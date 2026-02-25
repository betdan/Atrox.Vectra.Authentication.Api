namespace Atrox.Vectra.Authentication.Api.Business.Models.Configuration;

public class AuthenticationOptions
{
    public string Issuer { get; set; } = "Atrox.Vectra.Authentication";
    public string Audience { get; set; } = "Atrox.Vectra.Runtime";
    public int TokenExpirationMinutes { get; set; } = 30;
    public ApiKeyOptions ApiKey { get; set; } = new();
    public RsaOptions Rsa { get; set; } = new();
}

public class ApiKeyOptions
{
    public string Source { get; set; } = "Header";
    public string HeaderName { get; set; } = "x-api-key";
}

public class RsaOptions
{
    public string PrivateKey { get; set; } = string.Empty;
    public string KeyId { get; set; } = "atrox-auth-rs256-k1";
}
