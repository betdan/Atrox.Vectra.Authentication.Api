using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Grpc.Core;

namespace Atrox.Vectra.Authentication.Api.Transports.Grpc;

public class AuthenticationExecutionGrpcService(IExecutionService executionService, ILogger<AuthenticationExecutionGrpcService> logger)
    : AuthenticationExecution.AuthenticationExecutionBase
{
    private readonly IExecutionService _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
    private readonly ILogger<AuthenticationExecutionGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override async Task<AuthResponseGrpc> Authenticate(AuthRequestGrpc request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC authentication request received.");
        var response = await _executionService.AuthenticateAsync(new AuthRequest { ApiKey = request.ApiKey }, context.CancellationToken).ConfigureAwait(false);
        return new AuthResponseGrpc
        {
            Success = response.Success,
            AccessToken = response.AccessToken ?? string.Empty,
            TokenType = response.TokenType ?? "Bearer",
            ExpiresAtUtc = response.ExpiresAtUtc == default ? string.Empty : response.ExpiresAtUtc.ToString("O"),
            Error = response.Error ?? string.Empty
        };
    }

    public override Task<JwksResponseGrpc> GetJwks(JwksRequestGrpc request, ServerCallContext context)
    {
        var jwks = _executionService.GetJwks();
        var response = new JwksResponseGrpc();
        response.Keys.AddRange(jwks.Keys.Select(key => new JwksKeyGrpc
        {
            Kty = key.Kty,
            Use = key.Use,
            Alg = key.Alg,
            Kid = key.Kid,
            N = key.N,
            E = key.E
        }));
        return Task.FromResult(response);
    }
}
