using System.Diagnostics;
using System.Net.NetworkInformation;
using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Checks;

public sealed class PingCheck : IMonitorCheck
{
    public string Code => "PING";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(context.Server.HostOrIp, context.Server.ConnectTimeoutSec * 1000);

            var status = reply.Status == IPStatus.Success ? MonitorStatus.Ok : MonitorStatus.Unreachable;
            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = status,
                NumericValue = reply.RoundtripTime,
                Message = reply.Status.ToString(),
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
