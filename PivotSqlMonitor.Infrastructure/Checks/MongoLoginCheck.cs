using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using PivotSqlMonitor.Core.Enums;
using PivotSqlMonitor.Core.Interfaces;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Checks;

public sealed class MongoLoginCheck : IMonitorCheck
{
    public string Code => "MONGO_LOGIN";

    public async Task<CheckExecutionResult> ExecuteAsync(CheckExecutionContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            var client = MongoConnectionFactory.BuildClient(context, out var settings);

            var hasUserInConfig = !string.IsNullOrWhiteSpace(settings.Username) && !string.IsNullOrWhiteSpace(settings.Password);
            var hasUserInConn = !string.IsNullOrWhiteSpace(settings.ConnectionString)
                                && MongoUrl.Create(settings.ConnectionString).Username is not null;

            if (!hasUserInConfig && !hasUserInConn)
            {
                return new CheckExecutionResult
                {
                    CheckCode = Code,
                    Status = MonitorStatus.Warning,
                    Message = "MONGO_LOGIN sin credenciales. Agrega username/password o connectionString en ExtraConfigJson.",
                    StartedAtUtc = startedAt,
                    FinishedAtUtc = DateTime.UtcNow,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            var db = client.GetDatabase(string.IsNullOrWhiteSpace(settings.AuthDatabase) ? "admin" : settings.AuthDatabase);
            var result = await db.RunCommandAsync<BsonDocument>(new BsonDocument("connectionStatus", 1), cancellationToken: cancellationToken);
            var authInfo = result.GetValue("authInfo", new BsonDocument()).AsBsonDocument;
            var users = authInfo.Contains("authenticatedUsers") ? authInfo["authenticatedUsers"] : new BsonArray();
            var ok = users.AsBsonArray.Count > 0;

            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = ok ? MonitorStatus.Ok : MonitorStatus.Error,
                NumericValue = users.AsBsonArray.Count,
                Message = ok ? "Mongo login successful" : "Mongo login failed (no authenticated users)",
                RawPayloadJson = result.ToJson(),
                StartedAtUtc = startedAt,
                FinishedAtUtc = DateTime.UtcNow,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (MongoAuthenticationException ex)
        {
            return new CheckExecutionResult
            {
                CheckCode = Code,
                Status = MonitorStatus.Error,
                Message = $"Mongo auth failed: {ex.Message}",
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
