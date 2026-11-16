using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Prometheus;

namespace ErpBackend.Data;

public sealed class DbConnectionPoolMetricsInterceptor : DbConnectionInterceptor
{
    private sealed record ConnectionLease(long StartedAt, double StartedAtUnixSeconds, string Source);

    private static readonly Gauge CheckedOutConnections = Metrics
        .CreateGauge("erp_db_connections_checked_out", "EF-managed database connections currently checked out.");
    private static readonly Gauge CheckedOutConnectionsBySource = Metrics
        .CreateGauge(
            "erp_db_connections_checked_out_by_source",
            "EF-managed database connections currently checked out, grouped by API endpoint or background worker.",
            new GaugeConfiguration { LabelNames = ["source"] });
    private static readonly Gauge ConnectionPoolLimit = Metrics
        .CreateGauge("erp_db_connection_pool_max", "Maximum Npgsql connections allowed for this backend instance.");
    private static readonly Gauge OldestConnectionLeaseStartedAt = Metrics
        .CreateGauge(
            "erp_db_connection_oldest_lease_started_timestamp_seconds",
            "Unix timestamp when the oldest currently checked-out EF database connection was opened, by source.",
            new GaugeConfiguration { LabelNames = ["source"] });
    private static readonly Histogram ConnectionLeaseDuration = Metrics
        .CreateHistogram(
            "erp_db_connection_lease_duration_seconds",
            "Duration that an EF-managed database connection remains checked out.",
            new HistogramConfiguration
            {
                Buckets = [0.01, 0.05, 0.1, 0.5, 1, 5, 15, 30, 60]
            });
    private static readonly Counter LongConnectionLeases = Metrics
        .CreateCounter(
            "erp_db_connection_long_leases_total",
            "Database connection leases lasting longer than 30 seconds.",
            new CounterConfiguration { LabelNames = ["source"] });

    private readonly ConcurrentDictionary<DbConnection, ConnectionLease> _leases =
        new(ReferenceEqualityComparer.Instance);
    private readonly object _leaseLock = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DbConnectionPoolMetricsInterceptor> _logger;

    public DbConnectionPoolMetricsInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<DbConnectionPoolMetricsInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public static void SetPoolLimit(int maxPoolSize) => ConnectionPoolLimit.Set(maxPoolSize);

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => TrackOpened(connection);

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        TrackOpened(connection);
        return Task.CompletedTask;
    }

    public override void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData)
        => TrackClosed(connection);

    public override Task ConnectionClosedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        TrackClosed(connection);
        return Task.CompletedTask;
    }

    private void TrackOpened(DbConnection connection)
    {
        var source = _httpContextAccessor.HttpContext?.GetEndpoint()?.DisplayName ?? "background";
        lock (_leaseLock)
        {
            if (!_leases.TryAdd(
                    connection,
                    new ConnectionLease(
                        Stopwatch.GetTimestamp(),
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d,
                        source)))
            {
                _logger.LogError("Database connection was opened while already tracked for {Source}.", source);
                return;
            }

            CheckedOutConnections.Inc();
            CheckedOutConnectionsBySource.WithLabels(source).Inc();
            UpdateOldestLeaseStartedAt(source);
        }
    }

    private void TrackClosed(DbConnection connection)
    {
        ConnectionLease lease;
        lock (_leaseLock)
        {
            if (!_leases.TryRemove(connection, out lease!))
                return;

            CheckedOutConnections.Dec();
            CheckedOutConnectionsBySource.WithLabels(lease.Source).Dec();
            UpdateOldestLeaseStartedAt(lease.Source);
        }

        var duration = Stopwatch.GetElapsedTime(lease.StartedAt);
        ConnectionLeaseDuration.Observe(duration.TotalSeconds);

        if (duration >= TimeSpan.FromSeconds(30))
        {
            LongConnectionLeases.WithLabels(lease.Source).Inc();
            _logger.LogWarning(
                "Database connection for {Source} was checked out for {DurationSeconds:F1} seconds.",
                lease.Source,
                duration.TotalSeconds);
        }
    }

    private void UpdateOldestLeaseStartedAt(string source)
    {
        var oldestStartedAt = _leases.Values
            .Where(lease => string.Equals(lease.Source, source, StringComparison.Ordinal))
            .Select(lease => lease.StartedAtUnixSeconds)
            .DefaultIfEmpty(0)
            .Min();
        OldestConnectionLeaseStartedAt.WithLabels(source).Set(oldestStartedAt);
    }
}
