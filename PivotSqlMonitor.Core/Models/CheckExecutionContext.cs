namespace PivotSqlMonitor.Core.Models;

public sealed class CheckExecutionContext
{
    public required MonitoredServer Server { get; init; }
    public required ServerCheckConfig Config { get; init; }
    public required string ConnectionString { get; init; }
}
