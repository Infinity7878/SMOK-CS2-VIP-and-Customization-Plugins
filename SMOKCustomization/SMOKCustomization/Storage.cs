using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SMOKCustomization;

public sealed class JsonStore
{
    private readonly string _configPath;
    private readonly string _prefsPath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonStore(string moduleDirectory, ILogger logger)
    {
        _logger = logger;
        Directory.CreateDirectory(moduleDirectory);
        _configPath = Path.Combine(moduleDirectory, "SMOKCustomization.json");
        _prefsPath = Path.Combine(moduleDirectory, "player_preferences.json");
    }

    public PluginConfig LoadConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                var fresh = new PluginConfig();
                SaveConfig(fresh);
                return fresh;
            }

            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<PluginConfig>(json, _jsonOptions) ?? new PluginConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read SMOKCustomization config. Loading safe defaults.");
            return new PluginConfig();
        }
    }

    public void SaveConfig(PluginConfig config)
    {
        try
        {
            File.WriteAllText(_configPath, JsonSerializer.Serialize(config, _jsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write SMOKCustomization config.");
        }
    }

    public Dictionary<ulong, PlayerPreferences> LoadPreferences()
    {
        try
        {
            if (!File.Exists(_prefsPath))
            {
                SavePreferences(new Dictionary<ulong, PlayerPreferences>());
                return new Dictionary<ulong, PlayerPreferences>();
            }

            var json = File.ReadAllText(_prefsPath);
            return JsonSerializer.Deserialize<Dictionary<ulong, PlayerPreferences>>(json, _jsonOptions)
                   ?? new Dictionary<ulong, PlayerPreferences>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read player preferences. Starting with empty preferences.");
            return new Dictionary<ulong, PlayerPreferences>();
        }
    }

    public void SavePreferences(Dictionary<ulong, PlayerPreferences> preferences)
    {
        try
        {
            File.WriteAllText(_prefsPath, JsonSerializer.Serialize(preferences, _jsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write player preferences.");
        }
    }
}
