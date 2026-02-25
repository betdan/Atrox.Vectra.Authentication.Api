namespace Atrox.Vectra.Authentication.Api.Business.Models.Auth;

public class AuthResponse
{
    public bool Success { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAtUtc { get; set; }
    public string Error { get; set; } = string.Empty;
}
