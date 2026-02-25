using Atrox.Vectra.Authentication.Api.Business.Models.Auth;

namespace Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;

public interface IClientRepository
{
    Task<AuthClient> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default);
    Task UpdateLastUsedAtAsync(Guid clientId, CancellationToken cancellationToken = default);
}
