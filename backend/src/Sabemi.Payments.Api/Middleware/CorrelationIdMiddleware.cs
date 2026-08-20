namespace Sabemi.Payments.Api.Middleware;

public static class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() is { Length: > 0 } incoming
                ? incoming
                : context.TraceIdentifier;

            context.Response.Headers[HeaderName] = correlationId;

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Sabemi.Payments.Api.Request");

            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await next();
            }
        });
}
