using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Olli.Api.Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro interno ao processar {Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted)
            {
                throw;
            }

            var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            var problem = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = "Erro interno do servidor.",
                Detail = environment.IsDevelopment() ? exception.Message : "Ocorreu um erro inesperado.",
                Instance = context.Request.Path
            };
            problem.Extensions["correlationId"] = correlationId;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
