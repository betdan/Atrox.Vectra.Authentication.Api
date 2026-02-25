namespace Atrox.Vectra.Authentication.Api.Transports.WebSocket;

public class WebSocketTransportOptions
{
    public bool Enabled { get; set; }
    public string Path { get; set; } = "/ws/auth";
    public int KeepAliveSeconds { get; set; } = 120;
}
