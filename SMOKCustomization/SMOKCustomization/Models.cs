using System.Text.Json.Serialization;

namespace SMOKCustomization;

public sealed class PluginConfig
{
    public bool Enabled { get; set; } = true;
    public string ChatPrefix { get; set; } = "\x04[SMOK]\x01";
    public bool EnablePlayerModels { get; set; } = true;
    public bool EnableWeaponPaints { get; set; } = true;
    public bool ApplySkinsEverySecond { get; set; } = true;
    public bool RequirePermissionForWeaponPaints { get; set; } = true;
    public string WeaponPaintPermission { get; set; } = "@css/reservation";
    public string WeaponPaintNoPermissionMessage { get; set; } = "Skin changer is a VIP perk. Buy VIP, then redeem your code with !redeem <code>.";
    public bool AllowBots { get; set; } = false;
    public string AdminReloadPermission { get; set; } = "@css/config";

    public List<PlayerModelEntry> PlayerModels { get; set; } = new()
    {
        new PlayerModelEntry
        {
            Id = "default_ct",
            DisplayName = "Default CT",
            Team = ModelTeam.CT,
            ModelPath = "characters/models/ctm_sas/ctm_sas.vmdl",
            Enabled = true
        },
        new PlayerModelEntry
        {
            Id = "default_t",
            DisplayName = "Default T",
            Team = ModelTeam.T,
            ModelPath = "characters/models/tm_phoenix/tm_phoenix.vmdl",
            Enabled = true
        }
    };

    public List<PaintPreset> PaintPresets { get; set; } = new()
    {
        new PaintPreset { Id = "redline", DisplayName = "Redline", PaintKit = 282, Wear = 0.07f, Seed = 0 },
        new PaintPreset { Id = "asiimov", DisplayName = "Asiimov", PaintKit = 279, Wear = 0.18f, Seed = 0 },
        new PaintPreset { Id = "fade", DisplayName = "Fade", PaintKit = 38, Wear = 0.01f, Seed = 661 },
        new PaintPreset { Id = "doppler", DisplayName = "Doppler", PaintKit = 415, Wear = 0.01f, Seed = 0 }
    };
}

public sealed class PlayerModelEntry
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public ModelTeam Team { get; set; } = ModelTeam.Any;
    public bool Enabled { get; set; } = true;
    public bool HideLegs { get; set; } = false;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModelTeam
{
    Any,
    T,
    CT
}

public sealed class PaintPreset
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int PaintKit { get; set; }
    public float Wear { get; set; } = 0.07f;
    public int Seed { get; set; } = 0;
}

public sealed class PlayerPreferences
{
    public string? ModelAny { get; set; }
    public string? ModelT { get; set; }
    public string? ModelCT { get; set; }

    // Key = weapon designer name without the weapon_ prefix. Example: ak47, m4a1_silencer, awp.
    public Dictionary<string, WeaponPaintSelection> WeaponPaints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class WeaponPaintSelection
{
    public int PaintKit { get; set; }
    public float Wear { get; set; } = 0.07f;
    public int Seed { get; set; } = 0;
    public string? PresetId { get; set; }
}
