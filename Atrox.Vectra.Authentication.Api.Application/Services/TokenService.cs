using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.Business.Models.Configuration;
using Atrox.Vectra.Authentication.Api.Business.Models.Jwks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Atrox.Vectra.Authentication.Api.Application.Services;

public class TokenService(IOptions<AuthenticationOptions> options, ILogger<TokenService> logger) : ITokenService
{
    private readonly AuthenticationOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<TokenService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Lazy<(RsaSecurityKey SecurityKey, SigningCredentials SigningCredentials)> _keyMaterial = new(() => BuildKeyMaterial(options!.Value, logger));

    public AuthResponse GenerateToken(AuthClient client)
    {
        var now = DateTime.UtcNow;
        var expiration = now.AddMinutes(_options.TokenExpirationMinutes <= 0 ? 30 : _options.TokenExpirationMinutes);
        var keyMaterial = _keyMaterial.Value;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, client.ClientId.ToString()),
            new("company_id", client.CompanyId.ToString()),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiration,
            signingCredentials: keyMaterial.SigningCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponse
        {
            Success = true,
            AccessToken = accessToken,
            ExpiresAtUtc = expiration
        };
    }

    public JwksDocument GetJwks()
    {
        var rsa = _keyMaterial.Value.SecurityKey.Rsa ?? throw new InvalidOperationException("RSA key not loaded.");
        var parameters = rsa.ExportParameters(false);

        return new JwksDocument
        {
            Keys =
            [
                new JwksKey
                {
                    Kid = _options.Rsa.KeyId,
                    N = Base64UrlEncoder.Encode(parameters.Modulus),
                    E = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            ]
        };
    }

    private static (RsaSecurityKey SecurityKey, SigningCredentials SigningCredentials) BuildKeyMaterial(AuthenticationOptions options, ILogger<TokenService> logger)
    {
        if (string.IsNullOrWhiteSpace(options.Rsa.PrivateKey))
        {
            throw new InvalidOperationException("Authentication:Rsa:PrivateKey is required.");
        }

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(options.Rsa.PrivateKey);
            var securityKey = new RsaSecurityKey(rsa) { KeyId = options.Rsa.KeyId };
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
            return (securityKey, signingCredentials);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid RSA private key format in Authentication:Rsa:PrivateKey.");
            throw new InvalidOperationException("Invalid RSA private key format.", ex);
        }
    }
}
