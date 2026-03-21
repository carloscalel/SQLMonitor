using System.Diagnostics;
using System.Text.Json;
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
SELECT DISTINCT
    vs.volume_mount_point AS VolumeMountPoint,
    vs.logical_volume_name AS LogicalVolumeName,
    CAST(vs.available_bytes AS decimal(18,2)) / NULLIF(vs.total_bytes, 0) * 100.0 AS FreePct
FROM sys.master_files mf
CROSS APPLY sys.dm_os_volume_stats(mf.database_id, mf.file_id) vs
ORDER BY vs.volume_mount_point;";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            await using var connection = new SqlConnection(context.ConnectionString);
            var rows = (await connection.QueryAsync<DiskVolumeRow>(new CommandDefinition(Query, cancellationToken: cancellationToken))).ToList();

            if (rows.Count == 0)
            {
                return new CheckExecutionResult
                {
                    CheckCode = Code,
                    Status = Core.Enums.MonitorStatus.Error,
                    Message = "No se encontraron volúmenes de disco.",
                    StartedAtUtc = startedAt,
                    FinishedAtUtc = DateTime.UtcNow,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            var minFreePct = rows.Min(x => x.FreePct ?? 0m);
            var status = ThresholdEvaluator.EvaluateLowIsBad(minFreePct, context.Config.WarningThreshold, context.Config.CriticalThreshold);

            var detail = string.Join("; ", rows.Select(x =>
                $"{(string.IsNullOrWhiteSpace(x.VolumeMountPoint) ? "?" : x.VolumeMountPoint)} " +
                $"{(string.IsNullOrWhiteSpace(x.LogicalVolumeName) ? "" : $"({x.LogicalVolumeName}) ")}" +
                $"{(x.FreePct ?? 0m):N2}%"));

            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = status,
                NumericValue = Math.Round(minFreePct, 2),
                Message = detail,
                RawPayloadJson = JsonSerializer.Serialize(rows),
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

    private sealed class DiskVolumeRow
    {
        public string? VolumeMountPoint { get; set; }
        public string? LogicalVolumeName { get; set; }
        public decimal? FreePct { get; set; }
    }
}
