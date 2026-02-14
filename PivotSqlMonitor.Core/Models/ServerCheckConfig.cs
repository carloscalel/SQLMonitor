namespace PivotSqlMonitor.Core.Models;

public sealed class ServerCheckConfig
{
    public int ServerCheckConfigId { get; set; }
    public int ServerId { get; set; }
    public int CheckTypeId { get; set; }
    public string CheckCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public decimal? WarningThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public string? ExtraConfigJson { get; set; }
}
