namespace CrossCutting.CanonicalSignature;

public class ServiceRequest<TRequest> where TRequest : class
{
    public TRequest Body { get; set; }
}
