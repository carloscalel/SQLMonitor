using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;
using PivotSqlMonitor.Core.Services;
using PivotSqlMonitor.Infrastructure.Checks;
using PivotSqlMonitor.Infrastructure.Data;
using Serilog;

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
                            ?? throw new InvalidOperationException("Missing AdminDb connection string");

services.AddSingleton<IMonitorRepository>(_ => new SqlMonitorRepository(adminConnectionString));
services.AddSingleton<IMonitorCheck, PingCheck>();
services.AddSingleton<IMonitorCheck, TcpPortCheck>();
services.AddSingleton<IMonitorCheck, SqlLoginCheck>();
services.AddSingleton<IMonitorCheck, DiskFreeCheck>();

services.AddSingleton<Func<MonitoredServer, string>>(_ => server =>
    $"Server={server.HostOrIp},{server.Port};Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout={server.ConnectTimeoutSec};");

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
