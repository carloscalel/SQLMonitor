using PivotSqlMonitor.Core.Enums;

namespace PivotSqlMonitor.Core.Models;

public sealed class CheckExecutionResult
{
    public string CheckCode { get; set; } = string.Empty;
    public MonitorStatus Status { get; set; }
    public decimal? NumericValue { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RawPayloadJson { get; set; }
    public long DurationMs { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }
}
