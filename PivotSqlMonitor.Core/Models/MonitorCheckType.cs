namespace PivotSqlMonitor.Core.Models;

public sealed class MonitorCheckType
{
    public int CheckTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
