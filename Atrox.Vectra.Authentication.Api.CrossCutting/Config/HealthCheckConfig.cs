namespace CrossCutting.Config;

using Microsoft.AspNetCore.Builder;

public static class HealthCheckConfig
{
    public static void AddRegistration(WebApplication app)
    {
        app.MapHealthChecks("/health");
    }
}
