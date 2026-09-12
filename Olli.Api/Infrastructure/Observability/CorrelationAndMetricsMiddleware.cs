using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Serilog.Context;

namespace Olli.Api.Infrastructure.Observability;

public sealed class CorrelationAndMetricsMiddleware(
    RequestDelegate next,
    ILogger<CorrelationAndMetricsMiddleware> logger)
{
    private const int MaxCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.Response.Headers["X-Correlation-ID"] = correlationId;
        var stopwatch = Stopwatch.StartNew();
        var unhandledException = false;
        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
        using var activity = Telemetry.ActivitySource.StartActivity(
            $"HTTP {context.Request.Method} {context.Request.Path}",
            ActivityKind.Server);

        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("http.request.method", context.Request.Method);
        activity?.SetTag("url.path", context.Request.Path.ToString());

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            unhandledException = true;
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogError(exception, "Erro nao tratado durante a requisicao {Method} {Path}", context.Request.Method, context.Request.Path);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = unhandledException ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
            var duration = stopwatch.Elapsed.TotalMilliseconds;

            logger.LogInformation(
                "Requisicao HTTP concluida: {Method} {Path} => {StatusCode} em {DurationMs} ms",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                Math.Round(duration, 2));

            activity?.SetTag("http.response.status_code", statusCode);
            if (statusCode >= 500)
            {
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            Telemetry.RecordRequest(
                context.Request.Method,
                GetMetricPath(context),
                statusCode,
                duration);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var requestedId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(requestedId)
            && requestedId.Length <= MaxCorrelationIdLength
            && requestedId.All(character => (character is >= 'A' and <= 'Z')
                || (character is >= 'a' and <= 'z')
                || (character is >= '0' and <= '9')
                || character is '-' or '_' or '.' or ':'))
        {
            return requestedId;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static string GetMetricPath(HttpContext context) =>
        (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
        ?? context.Request.Path.ToString();
}
