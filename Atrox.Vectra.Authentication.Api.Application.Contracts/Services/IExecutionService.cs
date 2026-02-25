using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.Business.Models.Jwks;

namespace Atrox.Vectra.Authentication.Api.Application.Contracts.Services;

public interface IExecutionService
{
    Task<AuthResponse> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default);
    JwksDocument GetJwks();
}
