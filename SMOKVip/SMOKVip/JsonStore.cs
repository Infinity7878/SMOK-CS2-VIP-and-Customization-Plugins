using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SMOKVip;

public sealed class JsonStore
{
    private readonly ILogger _logger;
    private readonly string _configPath;
    private readonly string _databasePath;
    private readonly string _moduleDirectory;

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonStore(string moduleDirectory, ILogger logger)
    {
        _moduleDirectory = moduleDirectory;
        _logger = logger;
        Directory.CreateDirectory(moduleDirectory);
        _configPath = Path.Combine(moduleDirectory, "SMOKVip.json");
        _databasePath = Path.Combine(moduleDirectory, "vip_players.json");
    }

    public VipConfig LoadConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                var fresh = new VipConfig();
                SaveConfig(fresh);
                return fresh;
            }

            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<VipConfig>(json, _options) ?? new VipConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load SMOKVip config. Using safe defaults.");
            return new VipConfig();
        }
    }

    public void SaveConfig(VipConfig config)
    {
        try
        {
            File.WriteAllText(_configPath, JsonSerializer.Serialize(config, _options));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SMOKVip config.");
        }
    }

    public VipDatabase LoadDatabase()
    {
        try
        {
            if (!File.Exists(_databasePath))
            {
                var fresh = new VipDatabase();
                SaveDatabase(fresh);
                return fresh;
            }

            var json = File.ReadAllText(_databasePath);
            return JsonSerializer.Deserialize<VipDatabase>(json, _options) ?? new VipDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load VIP database. Starting empty.");
            return new VipDatabase();
        }
    }

    public void SaveDatabase(VipDatabase database)
    {
        try
        {
            File.WriteAllText(_databasePath, JsonSerializer.Serialize(database, _options));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save VIP database.");
        }
    }

    public void SaveExport(VipConfig config, VipDatabase database)
    {
        if (!config.WriteSharedExportFile) return;

        try
        {
            var now = DateTimeOffset.UtcNow;
            var tiers = config.Tiers.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
            var export = new VipExport { GeneratedAtUtc = now };

            foreach (var pair in database.Players)
            {
                var record = pair.Value;
                if (!record.IsActive(now)) continue;
                if (!tiers.TryGetValue(record.TierId, out var tier))
                    tier = config.Tiers.OrderByDescending(t => t.Priority).FirstOrDefault() ?? new VipTier();

                export.ActivePlayers[record.SteamId] = new VipExportRecord
                {
                    SteamId = record.SteamId,
                    TierId = record.TierId,
                    TierName = tier.DisplayName,
                    TierPriority = tier.Priority,
                    ExpiresAtUtc = record.ExpiresAtUtc
                };
            }

            var path = Path.Combine(_moduleDirectory, config.SharedExportFileName);
            File.WriteAllText(path, JsonSerializer.Serialize(export, _options));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write SMOKVip shared export file.");
        }
    }
}
