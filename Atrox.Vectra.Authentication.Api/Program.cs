using Atrox.Vectra.Authentication.Api.Installers;
using Atrox.Vectra.Authentication.Api.Transports.Grpc;
using Atrox.Vectra.Authentication.Api.Transports.WebSocket;
using CrossCutting.Config;
using CrossCutting.Middlewares;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

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
        options.ListenAnyIP(grpcOptions.Port, listenOptions => { listenOptions.Protocols = HttpProtocols.Http1AndHttp2; });
    });
}

builder.Services.AddControllers();
builder.Services.RegisterServices(configuration);
builder.Services.RegisterMassTransit(configuration);
builder.Services.RegisterHealthChecks();
ServiceExtensions.RegisterSwagger(builder.Services);

var app = builder.Build();
app.UseHttpsRedirection();
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
HealthCheckConfig.AddRegistration(app);
app.MapControllers();

if (grpcOptions.Enabled)
{
    app.MapGrpcService<AuthenticationExecutionGrpcService>();
}

await app.RunAsync();
