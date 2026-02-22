namespace PivotSqlMonitor.Core.Models;

public sealed class RunSummary
{
    public int RunId { get; set; }
    public int TotalChecks { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
}
