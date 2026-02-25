using System.Security.Cryptography;
using System.Text;

namespace Atrox.Vectra.Authentication.Api.Application.Services;

public static class ApiKeyHashService
{
    public static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
