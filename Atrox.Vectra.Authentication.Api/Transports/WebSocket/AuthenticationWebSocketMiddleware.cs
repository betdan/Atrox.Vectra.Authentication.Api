using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using CrossCutting.CanonicalSignature;
using Microsoft.Extensions.Options;

namespace Atrox.Vectra.Authentication.Api.Transports.WebSocket;

public class AuthenticationWebSocketMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, IOptions<WebSocketTransportOptions> transportOptions, ILogger<AuthenticationWebSocketMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly WebSocketTransportOptions _transportOptions = transportOptions?.Value ?? throw new ArgumentNullException(nameof(transportOptions));
    private readonly ILogger<AuthenticationWebSocketMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_transportOptions.Enabled || !context.Request.Path.Equals(_transportOptions.Path, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket request expected.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        _logger.LogInformation("WebSocket auth client connected from {remoteIp}.", context.Connection.RemoteIpAddress);
        await HandleConnectionAsync(socket, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task HandleConnectionAsync(System.Net.WebSockets.WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4 * 1024];

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var messageBuilder = new StringBuilder();
            WebSocketReceiveResult receiveResult;

            do
            {
                receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client.", cancellationToken).ConfigureAwait(false);
                    return;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
            }
            while (!receiveResult.EndOfMessage);

            var responsePayload = await ProcessMessageAsync(messageBuilder.ToString(), cancellationToken).ConfigureAwait(false);
            var responseBytes = Encoding.UTF8.GetBytes(responsePayload);
            await socket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> ProcessMessageAsync(string requestPayload, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("WebSocket auth request: {request}", requestPayload);
            var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var serviceRequest = JsonSerializer.Deserialize<ServiceRequest<AuthRequest>>(requestPayload, serializerOptions);
            var executionRequest = serviceRequest?.Body ?? JsonSerializer.Deserialize<AuthRequest>(requestPayload, serializerOptions) ?? new AuthRequest();

            using var scope = _scopeFactory.CreateScope();
            var executionService = scope.ServiceProvider.GetRequiredService<IExecutionService>();
            var response = await executionService.AuthenticateAsync(executionRequest, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("WebSocket auth response: {response}", Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented));
            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket auth message processing failed.");
            return JsonSerializer.Serialize(new AuthResponse { Success = false, Error = ex.Message });
        }
    }
}
