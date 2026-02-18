using Dapper;
using Microsoft.Data.SqlClient;
using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Data;

public sealed class SqlMonitorRepository : IMonitorRepository
{
    private readonly string _adminConnectionString;

    public SqlMonitorRepository(string adminConnectionString)
    {
        _adminConnectionString = adminConnectionString;
    }

    public async Task<int> StartRunAsync(string triggeredBy, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO dbo.MonitorRun(StartedAtUtc, Status, TriggeredBy)
OUTPUT INSERTED.RunId
VALUES (SYSUTCDATETIME(), 'RUNNING', @triggeredBy);";

        await using var connection = new SqlConnection(_adminConnectionString);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { triggeredBy }, cancellationToken: cancellationToken));
    }

    public async Task CompleteRunAsync(RunSummary summary, CancellationToken cancellationToken)
    {
        const string sql = @"
UPDATE dbo.MonitorRun
SET FinishedAtUtc = SYSUTCDATETIME(),
    Status = 'COMPLETED',
    TotalChecks = @TotalChecks,
    SuccessCount = @SuccessCount,
    WarningCount = @WarningCount,
    CriticalCount = @CriticalCount,
    ErrorCount = @ErrorCount
WHERE RunId = @RunId;";

        await using var connection = new SqlConnection(_adminConnectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, summary, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<MonitoredServer>> GetEnabledServersAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT ServerId, ServerName, HostOrIp, SqlInstance, Port, Environment, IsEnabled, ConnectTimeoutSec, CommandTimeoutSec
FROM dbo.MonitoredServer
WHERE IsEnabled = 1;";

        await using var connection = new SqlConnection(_adminConnectionString);
        var rows = await connection.QueryAsync<MonitoredServer>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ServerCheckConfig>> GetCheckConfigsAsync(int serverId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT scc.ServerCheckConfigId, scc.ServerId, scc.CheckTypeId, ct.Code AS CheckCode,
       scc.IsEnabled, scc.WarningThreshold, scc.CriticalThreshold, scc.ExtraConfigJson
FROM dbo.ServerCheckConfig scc
JOIN dbo.MonitorCheckType ct ON ct.CheckTypeId = scc.CheckTypeId
WHERE scc.ServerId = @serverId
  AND scc.IsEnabled = 1
  AND ct.IsEnabled = 1;";

        await using var connection = new SqlConnection(_adminConnectionString);
        var rows = await connection.QueryAsync<ServerCheckConfig>(new CommandDefinition(sql, new { serverId }, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task SaveResultAsync(int runId, int serverId, int checkTypeId, CheckExecutionResult result, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO dbo.MonitorResult
(RunId, ServerId, CheckTypeId, StartedAtUtc, FinishedAtUtc, DurationMs, Status, MetricValue, MetricText, RawPayloadJson)
VALUES
(@runId, @serverId, @checkTypeId, @startedAt, @finishedAt, @durationMs, @status, @metricValue, @metricText, @rawPayloadJson);";

        await using var connection = new SqlConnection(_adminConnectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            runId,
            serverId,
            checkTypeId,
            startedAt = result.StartedAtUtc,
            finishedAt = result.FinishedAtUtc,
            durationMs = result.DurationMs,
            status = Enum.GetName(typeof(MonitorStatus), result.Status)?.ToUpperInvariant(),
            metricValue = result.NumericValue,
            metricText = result.Message,
            rawPayloadJson = result.RawPayloadJson
        }, cancellationToken: cancellationToken));
    }
}
