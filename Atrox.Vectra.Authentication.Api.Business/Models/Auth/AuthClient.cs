namespace Atrox.Vectra.Authentication.Api.Business.Models.Auth;

public class AuthClient
{
    public Guid ClientId { get; set; }
    public Guid CompanyId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
