using System.Diagnostics;
using Microsoft.Data.SqlClient;
using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Checks;

public sealed class SqlLoginCheck : IMonitorCheck
{
    public string Code => "SQL_LOGIN";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            await using var connection = new SqlConnection(context.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("SELECT 1", connection);
            var value = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);

            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = value == 1 ? MonitorStatus.Ok : MonitorStatus.Error,
                Message = "SQL login successful",
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
                Status = MonitorStatus.Error,
                Message = ex.Message,
                StartedAtUtc = startedAt,
                FinishedAtUtc = DateTime.UtcNow,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }
}
