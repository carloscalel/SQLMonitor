using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;
using PivotSqlMonitor.Core.Services;

namespace PivotSqlMonitor.Infrastructure.Checks;

public sealed class DiskFreeCheck : IMonitorCheck
{
    public string Code => "DISK_FREE";

    private const string Query = @"
SELECT MIN(CAST(available_bytes AS decimal(18,2)) / NULLIF(total_bytes,0) * 100.0)
FROM sys.dm_os_volume_stats(NULL, NULL);";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            await using var connection = new SqlConnection(context.ConnectionString);
            var freePercent = await connection.ExecuteScalarAsync<decimal?>(new CommandDefinition(Query, cancellationToken: cancellationToken));
            var value = freePercent ?? 0m;
            var status = ThresholdEvaluator.EvaluateLowIsBad(value, context.Config.WarningThreshold, context.Config.CriticalThreshold);

            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = status,
                NumericValue = Math.Round(value, 2),
                Message = $"Disk free {value:N2}%",
                StartedAtUtc = startedAt,
                FinishedAtUtc = DateTime.UtcNow,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = Core.Enums.MonitorStatus.Error,
                Message = ex.Message,
                StartedAtUtc = startedAt,
                FinishedAtUtc = DateTime.UtcNow,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }
}
