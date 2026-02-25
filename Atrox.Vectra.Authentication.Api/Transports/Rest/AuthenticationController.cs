using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.Business.Models.Configuration;
using CrossCutting.CanonicalSignature;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Atrox.Vectra.Authentication.Api.Transports.Rest;

[ApiController]
[Route("api/v1/auth")]
public class AuthenticationController(IExecutionService executionService, IOptions<AuthenticationOptions> authOptions) : ControllerBase
{
    private readonly IExecutionService _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
    private readonly AuthenticationOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));

    [HttpPost]
    public async Task<IActionResult> Authenticate([FromBody] ServiceRequest<AuthRequest> request, CancellationToken cancellationToken)
    {
        var apiKey = ResolveApiKey(request?.Body?.ApiKey);
        var response = await _executionService.AuthenticateAsync(new AuthRequest { ApiKey = apiKey }, cancellationToken).ConfigureAwait(false);

        if (!response.Success)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [HttpGet("/.well-known/jwks.json")]
    public IActionResult GetJwks()
    {
        return Ok(_executionService.GetJwks());
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
