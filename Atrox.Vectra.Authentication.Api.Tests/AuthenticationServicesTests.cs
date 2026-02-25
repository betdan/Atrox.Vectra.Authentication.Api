using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Atrox.Vectra.Authentication.Api.Application.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.Business.Models.Configuration;
using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atrox.Vectra.Authentication.Api.Tests;

public class AuthenticationServicesTests
{
    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsError_WhenApiKeyDoesNotExist()
    {
        var sut = new ClientValidationService(new InMemoryClientRepository(null));
        var result = await sut.ValidateApiKeyAsync("valid-api-key-123456");
        Assert.False(result.IsValid);
        Assert.Equal("API key does not exist.", result.Error);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsError_WhenApiKeyInactive()
    {
        var client = new AuthClient
        {
            ClientId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            IsActive = false
        };
        var sut = new ClientValidationService(new InMemoryClientRepository(client));
        var result = await sut.ValidateApiKeyAsync("valid-api-key-123456");
        Assert.False(result.IsValid);
        Assert.Equal("API key is inactive.", result.Error);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsError_WhenApiKeyExpired()
    {
        var client = new AuthClient
        {
            ClientId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        var sut = new ClientValidationService(new InMemoryClientRepository(client));
        var result = await sut.ValidateApiKeyAsync("valid-api-key-123456");
        Assert.False(result.IsValid);
        Assert.Equal("API key is expired.", result.Error);
    }

    [Fact]
    public void GenerateToken_ReturnsSignedJwtWithExpectedClaims()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportRSAPrivateKeyPem();
        var options = Options.Create(new AuthenticationOptions
        {
            Issuer = "Atrox.Vectra.Authentication",
            Audience = "Atrox.Vectra.Runtime",
            TokenExpirationMinutes = 30,
            Rsa = new RsaOptions
            {
                KeyId = "test-kid",
                PrivateKey = privatePem
            }
        });

        var sut = new TokenService(options, NullLogger<TokenService>.Instance);
        var client = new AuthClient
        {
            ClientId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            IsActive = true
        };

        var tokenResult = sut.GenerateToken(client);

        Assert.True(tokenResult.Success);
        Assert.False(string.IsNullOrWhiteSpace(tokenResult.AccessToken));
        Assert.Equal("Bearer", tokenResult.TokenType);

        var parameters = rsa.ExportParameters(false);
        using var publicRsa = RSA.Create();
        publicRsa.ImportParameters(parameters);
        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(
            tokenResult.AccessToken,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "Atrox.Vectra.Authentication",
                ValidateAudience = true,
                ValidAudience = "Atrox.Vectra.Runtime",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(publicRsa),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5)
            },
            out _);

        var jwt = handler.ReadJwtToken(tokenResult.AccessToken);
        Assert.Equal(client.ClientId.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(client.CompanyId.ToString(), jwt.Claims.First(c => c.Type == "company_id").Value);
        Assert.False(string.IsNullOrWhiteSpace(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value));
    }

    [Fact]
    public void GetJwks_ReturnsConfiguredKid()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportRSAPrivateKeyPem();
        var options = Options.Create(new AuthenticationOptions
        {
            Rsa = new RsaOptions
            {
                KeyId = "test-kid",
                PrivateKey = privatePem
            }
        });

        var sut = new TokenService(options, NullLogger<TokenService>.Instance);
        var jwks = sut.GetJwks();

        Assert.Single(jwks.Keys);
        Assert.Equal("test-kid", jwks.Keys[0].Kid);
        Assert.Equal("RS256", jwks.Keys[0].Alg);
    }

    private sealed class InMemoryClientRepository(AuthClient authClient) : IClientRepository
    {
        public Task<AuthClient> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(authClient);
        }

        public Task UpdateLastUsedAtAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
