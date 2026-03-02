namespace Atrox.Vectra.Authentication.Api.Transports.Grpc;

public class GrpcTransportOptions
{
    public bool Enabled { get; set; }
    public int Port { get; set; } = 5005;
    public int TlsPort { get; set; }
}
