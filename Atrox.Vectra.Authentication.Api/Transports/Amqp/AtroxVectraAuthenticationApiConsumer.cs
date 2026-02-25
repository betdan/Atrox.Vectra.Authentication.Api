using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.MassTransit.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Atrox.Vectra.Authentication.Api.Transports.Amqp;

public class AtroxVectraAuthenticationApiConsumer(ILogger<AtroxVectraAuthenticationApiConsumer> logger, IExecutionService executionService) : IConsumer<MtEvent>
{
    private readonly ILogger<AtroxVectraAuthenticationApiConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IExecutionService _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));

    public async Task Consume(ConsumeContext<MtEvent> context)
    {
        _logger.LogDebug("AMQP auth request: {request}", JsonConvert.SerializeObject(context.Message.Request, Formatting.Indented));
        var response = await _executionService.AuthenticateAsync(context.Message.Request, context.CancellationToken).ConfigureAwait(false);
        _logger.LogDebug("AMQP auth response: {response}", JsonConvert.SerializeObject(response, Formatting.Indented));

        var resultEvent = new MtResultEvent(context.Message.CorrelationId, DateTime.UtcNow, response);
        await context.RespondAsync(resultEvent).ConfigureAwait(false);
    }
}
