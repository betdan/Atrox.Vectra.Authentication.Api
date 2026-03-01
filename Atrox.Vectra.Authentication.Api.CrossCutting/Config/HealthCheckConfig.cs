namespace CrossCutting.Config;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

public static class HealthCheckConfig
{
    public static void AddRegistration(WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new
                {
                    status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy"
                });
                await context.Response.WriteAsync(payload);
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString().ToLowerInvariant(),
                    totalDurationMs = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString().ToLowerInvariant(),
                        durationMs = entry.Value.Duration.TotalMilliseconds,
                        description = entry.Value.Description
                    })
                });
                await context.Response.WriteAsync(payload);
            }
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });
    }
}
