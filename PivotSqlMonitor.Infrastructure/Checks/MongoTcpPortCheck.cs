using System.Diagnostics;
using System.Net.Sockets;
using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Checks;

public sealed class MongoTcpPortCheck : IMonitorCheck
{
    public string Code => "MONGO_TCP_PORT";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;
        var port = context.Server.Port == 0 ? 27017 : context.Server.Port;

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(context.Server.HostOrIp, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(context.Server.ConnectTimeoutSec), cancellationToken);
            var completed = await Task.WhenAny(connectTask, timeoutTask);
            var isOpen = completed == connectTask && client.Connected;

            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = isOpen ? MonitorStatus.Ok : MonitorStatus.Unreachable,
                NumericValue = port,
                Message = isOpen ? "Mongo port open" : "Mongo port closed/timeout",
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
