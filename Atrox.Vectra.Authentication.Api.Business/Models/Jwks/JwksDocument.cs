using System.Text.Json.Serialization;

namespace Atrox.Vectra.Authentication.Api.Business.Models.Jwks;

public class JwksDocument
{
    [JsonPropertyName("keys")]
    public List<JwksKey> Keys { get; set; } = new();
}
