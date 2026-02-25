namespace CrossCutting.Config;

using Microsoft.AspNetCore.Builder;

public static class SwaggerConfig
{
    public static void AddRegistration(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
}
