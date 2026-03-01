using CrossCutting.CanonicalSignature;
using Microsoft.AspNetCore.Http;

namespace CrossCutting.Middlewares;

public class RequestHeaderValidationMiddleware(RequestDelegate next)
{
    private static readonly string[] RequiredHeaders =
    {
        "x-TransactionId",
        "x-SessionId",
        "x-ChannelId",
        "x-I18n"
    };

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private static readonly string[] ExcludedPathPrefixes =
    {
        "/health",
        "/swagger",
        "/.well-known"
    };

    public async Task Invoke(HttpContext context)
    {
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var missingHeaders = RequiredHeaders
            .Where(header => !context.Request.Headers.TryGetValue(header, out var value) || string.IsNullOrWhiteSpace(value.ToString()))
            .ToList();

        if (missingHeaders.Count == 0)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
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
                    Code = "HEADER_VALIDATION_ERROR",
                    Message = "Missing required headers"
                }
            }
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static bool IsExcludedPath(PathString requestPath)
    {
        var path = requestPath.Value ?? string.Empty;
        return ExcludedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
