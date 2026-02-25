using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;

namespace Atrox.Vectra.Authentication.Api.Application.Services;

public class ClientValidationService(IClientRepository repository) : IClientValidationService
{
    private readonly IClientRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<(bool IsValid, AuthClient Client, string Error)> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, null, "API key is required.");
        }

        if (apiKey.Length < 16)
        {
            return (false, null, "API key format is invalid.");
        }

        var apiKeyHash = ApiKeyHashService.ComputeSha256(apiKey);
        var client = await _repository.GetByApiKeyHashAsync(apiKeyHash, cancellationToken).ConfigureAwait(false);

        if (client is null)
        {
            return (false, null, "API key does not exist.");
        }

        if (!client.IsActive)
        {
            return (false, null, "API key is inactive.");
        }

        if (client.ExpiresAt.HasValue && client.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return (false, null, "API key is expired.");
        }

        return (true, client, string.Empty);
    }
}
