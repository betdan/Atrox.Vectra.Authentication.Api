using Atrox.Vectra.Authentication.Api.Installers;
using Atrox.Vectra.Authentication.Api.Transports.Grpc;
using Atrox.Vectra.Authentication.Api.Transports.WebSocket;
using CrossCutting.Config;
using CrossCutting.Middlewares;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var configuredUrls = (configuration["ASPNETCORE_URLS"] ?? string.Empty)
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var grpcOptions = configuration.GetSection("Transports:Grpc").Get<GrpcTransportOptions>() ?? new GrpcTransportOptions();
var webSocketOptions = configuration.GetSection("Transports:WebSocket").Get<WebSocketTransportOptions>() ?? new WebSocketTransportOptions();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.Host.UseSerilog(logger);
builder.Services.Configure<WebSocketTransportOptions>(configuration.GetSection("Transports:WebSocket"));

if (grpcOptions.Enabled)
{
    builder.Services.AddGrpc();
    builder.WebHost.ConfigureKestrel(options =>
    {
        foreach (var url in configuredUrls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                options.ListenAnyIP(uri.Port, listenOptions =>
                {
                    listenOptions.UseHttps();
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
            }
            else if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                options.ListenAnyIP(uri.Port, listenOptions => { listenOptions.Protocols = HttpProtocols.Http1; });
            }
        }

        if (!configuredUrls.Any(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Port == grpcOptions.Port))
        {
            options.ListenAnyIP(grpcOptions.Port, listenOptions => { listenOptions.Protocols = HttpProtocols.Http2; });
        }
    });
}

builder.Services.AddControllers();
builder.Services.RegisterServices(configuration);
builder.Services.RegisterMassTransit(configuration);
builder.Services.RegisterHealthChecks();
ServiceExtensions.RegisterSwagger(builder.Services);

var app = builder.Build();
var hasHttpsUrlConfigured = configuredUrls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (hasHttpsUrlConfigured)
{
    app.UseHttpsRedirection();
}

SwaggerConfig.AddRegistration(app);
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<RequestHeaderValidationMiddleware>();

if (webSocketOptions.Enabled)
{
    app.UseWebSockets(new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(webSocketOptions.KeepAliveSeconds)
    });
    app.UseMiddleware<AuthenticationWebSocketMiddleware>();
}

app.UseRouting();
app.UseHttpMetrics();
HealthCheckConfig.AddRegistration(app);
app.MapMetrics();
app.MapControllers();

if (grpcOptions.Enabled)
{
    app.MapGrpcService<AuthenticationExecutionGrpcService>();
}

await app.RunAsync();
