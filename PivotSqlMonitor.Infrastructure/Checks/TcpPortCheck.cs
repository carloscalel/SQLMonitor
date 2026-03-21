using System.Diagnostics;
using System.Net.Sockets;
using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Checks;

public sealed class TcpPortCheck : IMonitorCheck
{
    public string Code => "TCP_PORT";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(context.Server.HostOrIp, context.Server.Port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(context.Server.ConnectTimeoutSec), cancellationToken);
            var completed = await Task.WhenAny(connectTask, timeoutTask);

            var isOpen = completed == connectTask && client.Connected;
            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = isOpen ? MonitorStatus.Ok : MonitorStatus.Unreachable,
                NumericValue = context.Server.Port,
                Message = isOpen ? "Port open" : "Port closed/timeout",
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
