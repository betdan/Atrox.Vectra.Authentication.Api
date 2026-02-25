using CrossCutting.CanonicalSignature;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CrossCutting.Middlewares;

public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<ExceptionHandlerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing authentication request.");

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ServiceResponse<object>
            {
                Succeeded = false,
                TransactionId = context.Request.Headers["x-TransactionId"].ToString(),
                SessionId = context.Request.Headers["x-SessionId"].ToString(),
                Errors = new List<ProblemDetail>
                {
                    new()
                    {
                        Code = "INTERNAL_SERVER_ERROR",
                        Message = "An unexpected error occurred."
                    }
                }
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
