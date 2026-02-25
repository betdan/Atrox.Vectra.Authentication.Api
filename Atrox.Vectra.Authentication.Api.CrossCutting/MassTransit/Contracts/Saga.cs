using System.Text.Json.Serialization;
using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using MassTransit;

namespace Atrox.Vectra.Authentication.Api.MassTransit.Contracts;

[JsonSerializable(typeof(AuthResponse))]
public record MtEvent : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; init; }
    public DateTime RequestDateUtc { get; init; }
    public AuthRequest Request { get; init; } = new();

    [JsonConstructor]
    public MtEvent(Guid correlationId, DateTime requestDateUtc, AuthRequest request)
    {
        CorrelationId = correlationId;
        RequestDateUtc = requestDateUtc;
        Request = request;
    }
}

[JsonSerializable(typeof(MtEvent))]
public record MtResultEvent : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; init; }
    public DateTime TimestampUtc { get; init; }
    public AuthResponse Response { get; init; } = new();

    [JsonConstructor]
    public MtResultEvent(Guid correlationId, DateTime timestampUtc, AuthResponse response)
    {
        CorrelationId = correlationId;
        TimestampUtc = timestampUtc;
        Response = response;
    }
}
