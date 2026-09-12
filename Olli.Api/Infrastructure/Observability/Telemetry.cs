using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;

namespace Olli.Api.Infrastructure.Observability;

public static class Telemetry
{
    public const string ActivitySourceName = "Olli.Api";
    public const string MeterName = "Olli.Api";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "olli_api_requests_total",
        unit: "{request}",
        description: "Quantidade total de requisicoes HTTP processadas.");
    public static readonly Counter<long> Errors = Meter.CreateCounter<long>(
        "olli_api_errors_total",
        unit: "{error}",
        description: "Quantidade de requisicoes HTTP com erro.");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "olli_api_request_duration_ms",
        unit: "ms",
        description: "Duracao das requisicoes HTTP em milissegundos.");

    private static readonly double[] DurationBucketLimits = [10, 50, 100, 250, 500, 1000, 2500, 5000, 10000];
    private static readonly long[] DurationBucketCounts = new long[DurationBucketLimits.Length];
    private static long _requestCount;
    private static long _errorCount;
    private static long _durationMicroseconds;

    public static void RecordRequest(string method, string path, int statusCode, double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "http.request.method", method },
            { "url.path", path },
            { "http.response.status_code", statusCode }
        };

        Requests.Add(1, tags);
        RequestDuration.Record(durationMilliseconds, tags);
        Interlocked.Increment(ref _requestCount);
        Interlocked.Add(ref _durationMicroseconds, (long)(durationMilliseconds * 1000));

        for (var index = 0; index < DurationBucketLimits.Length; index++)
        {
            if (durationMilliseconds <= DurationBucketLimits[index])
                Interlocked.Increment(ref DurationBucketCounts[index]);
        }

        if (statusCode >= 400)
        {
            Errors.Add(1, tags);
            Interlocked.Increment(ref _errorCount);
        }
    }

    public static string ExportPrometheus()
    {
        var requestCount = Interlocked.Read(ref _requestCount);
        var errorCount = Interlocked.Read(ref _errorCount);
        var durationMicroseconds = Interlocked.Read(ref _durationMicroseconds);
        var durationMilliseconds = durationMicroseconds / 1000d;

        var output = new StringBuilder();
        output.AppendLine("# HELP olli_api_requests_total Total de requisicoes HTTP.");
        output.AppendLine("# TYPE olli_api_requests_total counter");
        output.AppendLine($"olli_api_requests_total {requestCount.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine("# HELP olli_api_errors_total Total de requisicoes HTTP com status 4xx ou 5xx.");
        output.AppendLine("# TYPE olli_api_errors_total counter");
        output.AppendLine($"olli_api_errors_total {errorCount.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine("# HELP olli_api_request_duration_ms Duracao das requisicoes HTTP em milissegundos.");
        output.AppendLine("# TYPE olli_api_request_duration_ms histogram");
        for (var index = 0; index < DurationBucketLimits.Length; index++)
        {
            output.AppendLine($"olli_api_request_duration_ms_bucket{{le=\"{DurationBucketLimits[index].ToString(CultureInfo.InvariantCulture)}\"}} {Interlocked.Read(ref DurationBucketCounts[index])}");
        }
        output.AppendLine($"olli_api_request_duration_ms_bucket{{le=\"+Inf\"}} {requestCount.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine($"olli_api_request_duration_ms_sum {durationMilliseconds.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine($"olli_api_request_duration_ms_count {requestCount.ToString(CultureInfo.InvariantCulture)}");
        return output.ToString();
    }
}
