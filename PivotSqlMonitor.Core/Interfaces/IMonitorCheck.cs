using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Core.Interfaces;

public interface IMonitorCheck
{
    string Code { get; }
    Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken);
}
