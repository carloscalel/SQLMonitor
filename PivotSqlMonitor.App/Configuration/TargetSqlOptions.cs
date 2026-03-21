namespace PivotSqlMonitor.App.Configuration;

public sealed class TargetSqlOptions
{
    public string AuthMode { get; set; } = "Windows";
    public bool EncryptConnection { get; set; } = true;
    public bool TrustServerCertificate { get; set; } = true;
    public string? UserNameEncrypted { get; set; }
    public string? PasswordEncrypted { get; set; }
}
