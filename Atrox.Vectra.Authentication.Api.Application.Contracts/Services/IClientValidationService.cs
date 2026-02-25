using Atrox.Vectra.Authentication.Api.Business.Models.Auth;

namespace Atrox.Vectra.Authentication.Api.Application.Contracts.Services;

public interface IClientValidationService
{
    Task<(bool IsValid, AuthClient Client, string Error)> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}
