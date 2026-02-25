using System.Text.Json.Serialization;

namespace CrossCutting.CanonicalSignature;

public class ServiceResponse<TResponse> where TResponse : class
{
    public ServiceResponse()
    {
        Errors = new List<ProblemDetail>();
    }

    [JsonPropertyName("data")]
    public TResponse Data { get; set; }

    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; set; } = true;

    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; }

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; }

    [JsonPropertyName("errors")]
    public List<ProblemDetail> Errors { get; set; }
}
