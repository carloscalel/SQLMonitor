using System.Text.Json;
using MongoDB.Driver;
using PivotSqlMonitor.Core.Models;

namespace PivotSqlMonitor.Infrastructure.Checks;

internal static class MongoConnectionFactory
{
    internal static MongoClient BuildClient(CheckExecutionContext context, out MongoSettings settings)
    {
        settings = ParseSettings(context.Config.ExtraConfigJson);
        var conn = BuildConnectionString(context.Server, settings);
        var mongoClientSettings = MongoClientSettings.FromConnectionString(conn);
        mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(context.Server.ConnectTimeoutSec);
        mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(context.Server.ConnectTimeoutSec);
        return new MongoClient(mongoClientSettings);
    }

    internal static MongoSettings ParseSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new MongoSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<MongoSettings>(json) ?? new MongoSettings();
        }
        catch
        {
            return new MongoSettings();
        }
    }

    private static string BuildConnectionString(MonitoredServer server, MongoSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            return settings.ConnectionString;
        }

        var host = server.HostOrIp;
        var port = server.Port == 0 ? 27017 : server.Port;
        var authDb = string.IsNullOrWhiteSpace(settings.AuthDatabase) ? "admin" : settings.AuthDatabase;

        if (!string.IsNullOrWhiteSpace(settings.Username) && !string.IsNullOrWhiteSpace(settings.Password))
        {
            var u = Uri.EscapeDataString(settings.Username);
            var p = Uri.EscapeDataString(settings.Password);
            return $"mongodb://{u}:{p}@{host}:{port}/?authSource={authDb}";
        }

        return $"mongodb://{host}:{port}";
    }

    internal sealed class MongoSettings
    {
        public string? ConnectionString { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? AuthDatabase { get; set; } = "admin";
    }
}
