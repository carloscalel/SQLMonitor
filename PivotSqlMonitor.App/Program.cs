using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PivotSqlMonitor.App.Configuration;
using PivotSqlMonitor.App.Security;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;
using PivotSqlMonitor.Core.Services;
using PivotSqlMonitor.Infrastructure.Checks;
using PivotSqlMonitor.Infrastructure.Data;
using Serilog;

if (args.Length > 0 && args[0].Equals("--encrypt-credential", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.WriteLine("Uso: PivotSqlMonitor.App.exe --encrypt-credential <textoPlano> <entropia>");
        return 1;
    }

    Console.WriteLine(CredentialProtector.Protect(args[1], args[2]));
    return 0;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables(prefix: "PIVOT_")
    .Build();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var services = new ServiceCollection();
services.AddSingleton(configuration);

var adminConnectionString = configuration.GetConnectionString("AdminDb")
                            ?? configuration["ConnectionStrings:AdminDb"]
                            ?? string.Empty;

if (string.IsNullOrWhiteSpace(adminConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:AdminDb está vacío. Configura appsettings.json o la variable de entorno PIVOT_ConnectionStrings__AdminDb.");
}

var targetOptions = configuration.GetSection("TargetSql").Get<TargetSqlOptions>()
                   ?? throw new InvalidOperationException("Missing TargetSql section");

var credentialEntropy = configuration["CredentialEncryption:Entropy"]
                        ?? throw new InvalidOperationException("Missing CredentialEncryption:Entropy");

services.AddSingleton<IMonitorRepository>(_ => new SqlMonitorRepository(adminConnectionString));
services.AddSingleton<IMonitorCheck, PingCheck>();
services.AddSingleton<IMonitorCheck, TcpPortCheck>();
services.AddSingleton<IMonitorCheck, SqlLoginCheck>();
services.AddSingleton<IMonitorCheck, DiskFreeCheck>();
services.AddSingleton<IMonitorCheck, MongoPingCheck>();
services.AddSingleton<IMonitorCheck, MongoTcpPortCheck>();
services.AddSingleton<IMonitorCheck, MongoLoginCheck>();

services.AddSingleton<Func<MonitoredServer, string>>(_ => server =>
{
    var builder = new SqlConnectionStringBuilder
    {
        DataSource = $"{server.HostOrIp},{server.Port}",
        InitialCatalog = "master",
        ConnectTimeout = server.ConnectTimeoutSec,
        Encrypt = targetOptions.EncryptConnection,
        TrustServerCertificate = targetOptions.TrustServerCertificate
    };

    if (targetOptions.AuthMode.Equals("SqlLogin", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(targetOptions.UserNameEncrypted) || string.IsNullOrWhiteSpace(targetOptions.PasswordEncrypted))
        {
            throw new InvalidOperationException("TargetSql.UserNameEncrypted y TargetSql.PasswordEncrypted son requeridos para AuthMode=SqlLogin");
        }

        try
        {
            builder.IntegratedSecurity = false;
            builder.UserID = CredentialProtector.Unprotect(targetOptions.UserNameEncrypted, credentialEntropy);
            builder.Password = CredentialProtector.Unprotect(targetOptions.PasswordEncrypted, credentialEntropy);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "No se pudieron descifrar credenciales TargetSql. Verifica que fueron cifradas en la misma máquina y con la misma entropía.", ex);
        }
    }
    else
    {
        builder.IntegratedSecurity = true;
    }

    return builder.ConnectionString;
});

services.AddSingleton<MonitorOrchestrator>();

var provider = services.BuildServiceProvider();

try
{
    var orchestrator = provider.GetRequiredService<MonitorOrchestrator>();
    var summary = await orchestrator.ExecuteAsync(CancellationToken.None);

    Log.Information("Run {RunId}: total={Total} ok={Ok} warn={Warn} crit={Crit} err={Err}",
        summary.RunId, summary.TotalChecks, summary.SuccessCount, summary.WarningCount, summary.CriticalCount, summary.ErrorCount);
}
catch (Exception ex)
{
    Log.Error(ex, "Monitor execution failed");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
