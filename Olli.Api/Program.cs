using System.Threading.RateLimiting;
using HealthChecks.UI.Client;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Core;
using IdempotentAPI.Extensions.DependencyInjection;
using IdempotentAPI.MinimalAPI;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Olli.Api.Application.Services;
using Olli.Api.Infrastructure.Observability;
using Olli.Api.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Olli.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "olli-.log"),
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(Telemetry.ActivitySourceName)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(Telemetry.MeterName)
        .AddConsoleExporter());

builder.Services.AddControllers();

#region Database
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "InMemory";
if (builder.Environment.IsEnvironment("Testing"))
{
    databaseProvider = "InMemory";
}

var isOracleProvider = databaseProvider.Equals("Oracle", StringComparison.OrdinalIgnoreCase);
var applyMigrationsOnStartup = builder.Configuration.GetValue<bool?>("ApplyMigrationsOnStartup") ?? false;
var seedDatabase = builder.Configuration.GetValue<bool?>("SeedDatabase") ?? !isOracleProvider;
if (builder.Environment.IsEnvironment("Testing"))
{
    seedDatabase = true;
}

var oracleConnectionString = builder.Configuration.GetConnectionString("FiapOracle");
if (isOracleProvider && string.IsNullOrWhiteSpace(oracleConnectionString))
{
    throw new InvalidOperationException("Connection string FiapOracle nao configurada.");
}

builder.Services.AddDbContext<OlliDb>(
    options =>
    {
        if (isOracleProvider)
        {
            options.UseOracle(oracleConnectionString);
            return;
        }

        var databaseName = builder.Configuration.GetValue<string>("DatabaseName") ?? "OlliDb";
        options.UseInMemoryDatabase(databaseName);
    });
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<ITutorService, TutorService>();
builder.Services.AddScoped<IPetService, PetService>();
#endregion

#region Idempotency
builder.Services.AddIdempotentAPI();
builder.Services.AddIdempotentMinimalAPI(new IdempotencyOptions());
builder.Services.AddDistributedMemoryCache();
builder.Services.AddIdempotentAPIUsingDistributedCache();
#endregion

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(1)
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain";
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync(
            "Muitas requisicoes. Por favor, tente novamente mais tarde.",
            cancellationToken);
    };
});

var healthChecks = builder.Services.AddHealthChecks();

healthChecks.AddCheck(
    "api",
    () => HealthCheckResult.Healthy("API Olli em execucao."),
    tags: ["live", "ready"]);

if (isOracleProvider)
{
    healthChecks.AddOracle(
        connectionString: oracleConnectionString!,
        name: "oracle-fiap",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "db", "oracle"],
        healthQuery: "SELECT 1 FROM DUAL",
        timeout: TimeSpan.FromSeconds(5)
    );
}
else
{
    healthChecks.AddCheck(
        "in-memory-db",
        () => HealthCheckResult.Healthy("Banco em memoria ativo para desenvolvimento local."),
        tags: ["ready", "db", "in-memory"]);
}

var externalServices = builder.Configuration
    .GetSection("ExternalServices:Urls")
    .GetChildren()
    .Where(section => Uri.TryCreate(section.Value, UriKind.Absolute, out _))
    .ToList();

if (builder.Environment.IsEnvironment("Testing"))
{
    externalServices.Clear();
}

if (externalServices.Count == 0)
{
    healthChecks.AddCheck(
        "external-services",
        () => HealthCheckResult.Healthy("Nenhum servico externo foi configurado."),
        tags: ["ready", "external"]);
}
else
{
    foreach (var service in externalServices)
    {
        healthChecks.AddUrlGroup(
            new Uri(service.Value!),
            name: $"external-{service.Key}",
            failureStatus: HealthStatus.Degraded,
            tags: ["ready", "external"]);
    }
}

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(150);
        options.MaximumHistoryEntriesPerEndpoint(5);
        options.SetApiMaxActiveRequests(1);
        options.AddHealthCheckEndpoint("api", "/health");
    }).AddInMemoryStorage();
}

#region OpenAPI / Swagger / Scalar
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo()
        {
            Title = "API Olli - Clyvo VET",
            Version = "1.0.0",
            Description = """
                          API para acompanhamento preventivo da saude pet.

                          A Olli conecta tutor, pet e clinica veterinaria em uma jornada continua.
                          A IA deste MVP gera recomendacoes preventivas, mas nao realiza diagnostico definitivo.

                          Boa vizinhanca:

                          Recomendamos no maximo 10 requisicoes por segundo.
                          Caso haja exagero de uso, a API retornara status code 429 (Too Many Requests).
                          """,
            License = new OpenApiLicense()
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/license/mit/")
            },
            Contact = new OpenApiContact()
            {
                Email = "contato@olli.pet",
                Name = "Equipe Olli",
                Url = new Uri("https://www.fiap.com.br")
            }
        };
        return Task.CompletedTask;
    });
});
#endregion

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OlliDb>();
    if (db.Database.IsRelational() && applyMigrationsOnStartup)
    {
        await db.Database.MigrateAsync();
    }

    if (seedDatabase)
    {
        await OlliDbSeed.SeedAsync(db);
    }
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/database", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/external", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("external"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapHealthChecksUI(options =>
    {
        options.UIPath = "/health-ui";
    });
}

app.UseMiddleware<CorrelationAndMetricsMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Response.Headers["X-Correlation-ID"].ToString());
        diagnosticContext.Set("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString());
    };
});
app.UseRateLimiter();

app.MapGet("/metrics", () => Results.Text(
    Telemetry.ExportPrometheus(),
    "text/plain; version=0.0.4; charset=utf-8"))
    .WithTags("observability")
    .ExcludeFromDescription();

app.MapGet("/", () => "Olli API - Clyvo VET")
    .WithTags("status")
    .ExcludeFromDescription();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

public partial class Program { }
