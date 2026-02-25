using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.Business.Models.Jwks;
using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;
using Microsoft.Extensions.Logging;

namespace Atrox.Vectra.Authentication.Api.Application.Services;

public class AuthenticationExecutionService(
    IClientValidationService clientValidationService,
    ITokenService tokenService,
    IClientRepository clientRepository,
    ILogger<AuthenticationExecutionService> logger) : IExecutionService
{
    private readonly IClientValidationService _clientValidationService = clientValidationService ?? throw new ArgumentNullException(nameof(clientValidationService));
    private readonly ITokenService _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    private readonly IClientRepository _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
    private readonly ILogger<AuthenticationExecutionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<AuthResponse> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _clientValidationService.ValidateApiKeyAsync(request.ApiKey, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid || validation.Client is null)
        {
            _logger.LogWarning("Authentication rejected: {reason}", validation.Error);
            return new AuthResponse
            {
                Success = false,
                Error = validation.Error
            };
        }

        await _clientRepository.UpdateLastUsedAtAsync(validation.Client.ClientId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Authentication succeeded for client {clientId}.", validation.Client.ClientId);
        return _tokenService.GenerateToken(validation.Client);
    }

    public JwksDocument GetJwks()
    {
        return _tokenService.GetJwks();
    }
}
