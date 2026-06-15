using System.Diagnostics;
using Prometheus;

namespace ErpBackend.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;

    private static readonly Counter RequestCounter = Metrics
        .CreateCounter("erp_api_requests_total", "Total API request count",
            new CounterConfiguration { LabelNames = new[] { "method", "endpoint", "status_code" } });

    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("erp_api_request_duration_seconds", "API request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint" },
                Buckets = new[] { 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 }
            });

    private static readonly Gauge ActiveRequests = Metrics
        .CreateGauge("erp_api_active_requests", "Currently active API requests");

    public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        ActiveRequests.Inc();
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            ActiveRequests.Dec();

            var method = context.Request.Method;
            var endpoint = NormalizeEndpoint(context.Request.Path);
            var statusCode = context.Response.StatusCode.ToString();

            RequestCounter.WithLabels(method, endpoint, statusCode).Inc();
            RequestDuration.WithLabels(method, endpoint).Observe(sw.Elapsed.TotalSeconds);
        }
    }

    private static string NormalizeEndpoint(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        if (segments.Length == 0) return "/";

        var normalized = "/" + segments[0];
        if (segments.Length > 1 && Guid.TryParse(segments[1], out _))
        {
            normalized += "/{id}";
        }
        else if (segments.Length > 1)
        {
            normalized += "/" + segments[1];
        }

        return normalized;
    }
}
