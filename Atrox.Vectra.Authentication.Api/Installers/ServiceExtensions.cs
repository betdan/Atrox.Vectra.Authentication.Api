using System.Text.Json;
using Atrox.Vectra.Authentication.Api.Application.Contracts.Services;
using Atrox.Vectra.Authentication.Api.Application.Services;
using Atrox.Vectra.Authentication.Api.Business.Models.Configuration;
using Atrox.Vectra.Authentication.Api.DataAccess.Connections;
using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Connections;
using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;
using Atrox.Vectra.Authentication.Api.DataAccess.Repositories;
using Atrox.Vectra.Authentication.Api.HealthChecks;
using Atrox.Vectra.Authentication.Api.Transports.Amqp;
using CrossCutting.Crypto;
using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

namespace Atrox.Vectra.Authentication.Api.Installers;

public static class ServiceExtensions
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseEngine = configuration["Database:Engine"];
        if (!string.Equals(databaseEngine, "SqlServer", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(databaseEngine, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid Database:Engine value. Allowed values: SqlServer, PostgreSql.");
        }

        services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));

        services.AddSingleton<ICrypto, Crypto>();
        services.AddScoped<IConnectionStringBuilder, ConnectionStringBuilder>();
        services.AddScoped<SqlServerClientRepository>();
        services.AddScoped<PostgreSqlClientRepository>();
        services.AddScoped<IClientRepositoryFactory, ClientRepositoryFactory>();
        services.AddScoped(sp => sp.GetRequiredService<IClientRepositoryFactory>().CreateRepository());
        services.AddScoped<IClientValidationService, ClientValidationService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IExecutionService, AuthenticationExecutionService>();
    }

    public static void RegisterHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", failureStatus: HealthStatus.Unhealthy, tags: ["ready"]);
    }

    public static void RegisterMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AtroxVectraAuthenticationApiConsumer>()
                .Endpoint(e => e.Name = configuration["RabbitMqQueueName:Atrox.Vectra.Authentication.Api"]);

            x.UsingRabbitMq((context, rabbitMqConfiguration) =>
            {
                rabbitMqConfiguration.ConfigureJsonSerializerOptions(_ => new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                rabbitMqConfiguration.UseRawJsonSerializer(RawSerializerOptions.AddTransportHeaders | RawSerializerOptions.CopyHeaders);
                rabbitMqConfiguration.Host(new Uri(configuration["RabbitMq:Hostname"]!), h =>
                {
                    h.Username(configuration["RabbitMq:UserName"]);
                    h.Password(configuration["RabbitMq:Password"]);
                });

                rabbitMqConfiguration.ConfigureEndpoints(context);
            });
        });
    }

    public static void RegisterSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Atrox.Vectra.Authentication.Api",
                Description = "Authentication API for token issuance and JWKS publication."
            });
        });
    }
}
