using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.Business.Models.Configuration;
using CrossCutting.CanonicalSignature;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atrox.Vectra.Authentication.Api.Transports.Rest;

[ApiController]
[Route("api/v1/auth")]
public class AuthenticationController(IExecutionService executionService, IOptions<AuthenticationOptions> authOptions, ILogger<AuthenticationController> logger) : ControllerBase
{
    private readonly IExecutionService _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
    private readonly AuthenticationOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly ILogger<AuthenticationController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpPost]
    public async Task<IActionResult> Authenticate([FromBody] ServiceRequest<AuthRequest> request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("REST auth request: {request}", JsonConvert.SerializeObject(request, Formatting.Indented));

        var apiKey = ResolveApiKey(request?.Body?.ApiKey);
        var response = await _executionService.AuthenticateAsync(new AuthRequest { ApiKey = apiKey }, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("REST auth response: {response}", JsonConvert.SerializeObject(response, Formatting.Indented));

        if (!response.Success)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [HttpGet("/.well-known/jwks.json")]
    public IActionResult GetJwks()
    {
        _logger.LogDebug("REST JWKS request received.");
        var response = _executionService.GetJwks();
        _logger.LogDebug("REST JWKS response: {response}", JsonConvert.SerializeObject(response, Formatting.Indented));
        return Ok(response);
    }

    private string ResolveApiKey(string apiKeyInBody)
    {
        if (string.Equals(_authOptions.ApiKey.Source, "Header", StringComparison.OrdinalIgnoreCase))
        {
            return Request.Headers[_authOptions.ApiKey.HeaderName].ToString();
        }

        return apiKeyInBody ?? string.Empty;
    }
}
