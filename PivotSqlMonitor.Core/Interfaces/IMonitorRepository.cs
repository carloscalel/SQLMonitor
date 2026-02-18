using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Core.Interfaces;

public interface IMonitorRepository
{
    Task<int> StartRunAsync(string triggeredBy, CancellationToken cancellationToken);
    Task CompleteRunAsync(RunSummary summary, CancellationToken cancellationToken);
    Task<IReadOnlyList<MonitoredServer>> GetEnabledServersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ServerCheckConfig>> GetCheckConfigsAsync(int serverId, CancellationToken cancellationToken);
    Task SaveResultAsync(int runId, int serverId, int checkTypeId, CheckExecutionResult result, CancellationToken cancellationToken);
}
