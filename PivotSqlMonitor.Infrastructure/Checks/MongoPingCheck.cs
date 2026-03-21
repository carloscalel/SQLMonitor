using System.Diagnostics;
using MongoDB.Bson;
using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Checks;

public sealed class MongoPingCheck : IMonitorCheck
{
    public string Code => "MONGO_PING";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            var client = MongoConnectionFactory.BuildClient(context, out _);
            var db = client.GetDatabase("admin");
            var result = await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            var ok = result.GetValue("ok", 0).ToDouble() >= 1;

            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = ok ? MonitorStatus.Ok : MonitorStatus.Error,
                NumericValue = ok ? 1 : 0,
                Message = ok ? "Mongo ping OK" : "Mongo ping failed",
                RawPayloadJson = result.ToJson(),
                StartedAtUtc = startedAt,
                FinishedAtUtc = DateTime.UtcNow,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = MonitorStatus.Error,
                Message = ex.Message,
                StartedAtUtc = startedAt,
                FinishedAtUtc = DateTime.UtcNow,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }
}
