namespace PivotSqlMonitor.Core.Models;

public sealed class MonitoredServer
{
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string HostOrIp { get; set; } = string.Empty;
    public string SqlInstance { get; set; } = "MSSQLSERVER";
    public int Port { get; set; } = 1433;
    public string Environment { get; set; } = "PROD";
    public bool IsEnabled { get; set; }
    public int ConnectTimeoutSec { get; set; } = 5;
    public int CommandTimeoutSec { get; set; } = 5;
}
