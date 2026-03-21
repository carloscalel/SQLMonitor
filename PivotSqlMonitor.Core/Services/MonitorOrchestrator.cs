using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Core.Services;

public sealed class MonitorOrchestrator
{
    private readonly IMonitorRepository _repository;
    private readonly IReadOnlyDictionary<string, IMonitorCheck> _checks;
    private readonly Func<MonitoredServer, string> _connectionStringFactory;

    public MonitorOrchestrator(
        IMonitorRepository repository,
        IEnumerable<IMonitorCheck> checks,
        Func<MonitoredServer, string> connectionStringFactory)
    {
        _repository = repository;
        _checks = checks.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        _connectionStringFactory = connectionStringFactory;
    }

    public async Task<RunSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        var runId = await _repository.StartRunAsync("SQLAgent", cancellationToken);
        var summary = new RunSummary { RunId = runId };

        var servers = await _repository.GetEnabledServersAsync(cancellationToken);
        foreach (var server in servers)
        {
            var configs = await _repository.GetCheckConfigsAsync(server.ServerId, cancellationToken);

            foreach (var config in configs.Where(c => c.IsEnabled))
            {
                summary.TotalChecks++;
                if (!_checks.TryGetValue(config.CheckCode, out var check))
                {
                    summary.ErrorCount++;
                    continue;
                }

                var context = new CheckExecutionContext
                {
                    Server = server,
                    Config = config,
                    ConnectionString = _connectionStringFactory(server)
                };

                var result = await check.ExecuteAsync(context, cancellationToken);
                await _repository.SaveResultAsync(runId, server.ServerId, config.CheckTypeId, result, cancellationToken);

                switch (result.Status)
                {
                    case MonitorStatus.Ok:
                        summary.SuccessCount++;
                        break;
                    case MonitorStatus.Warning:
                        summary.WarningCount++;
                        break;
                    case MonitorStatus.Critical:
                        summary.CriticalCount++;
                        break;
                    default:
                        summary.ErrorCount++;
                        break;
                }
            }
        }

        await _repository.CompleteRunAsync(summary, cancellationToken);
        return summary;
    }
}
