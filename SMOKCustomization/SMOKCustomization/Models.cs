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
    public bool EnableKnifeChanger { get; set; } = true;
    public bool RequirePermissionForKnifeChanger { get; set; } = true;
    public string KnifeChangerPermission { get; set; } = "@css/reservation";
    public string KnifeNoPermissionMessage { get; set; } = "Knife changer is a VIP perk. Buy VIP, then redeem your code with !redeem <code>.";
    // Uses CS2 ChangeSubclass on the player's existing knife. This avoids spawning invalid weapon_knife_* entities.
    public bool EnableKnifeSubclassChanger { get; set; } = true;
    public bool ApplyKnifeImmediatelyOnCommand { get; set; } = true;

    public bool EnableGloveChanger { get; set; } = true;
    public bool RequirePermissionForGloveChanger { get; set; } = true;
    public string GloveChangerPermission { get; set; } = "@css/reservation";
    public string GloveNoPermissionMessage { get; set; } = "Glove changer is a VIP perk. Buy VIP, then redeem your code with !redeem <code>.";
    public bool ApplyGlovesImmediatelyOnCommand { get; set; } = true;

    // Legacy/unsafe entity-replacement settings are intentionally kept for config compatibility only.
    // They are no longer used by the stable knife changer.
    public bool EnableExperimentalKnifeEntityReplacement { get; set; } = false;
    public bool RemoveExistingKnifeBeforeGiving { get; set; } = false;
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

    public List<KnifeEntry> Knives { get; set; } = new()
    {
        new KnifeEntry { Id = "bayonet", DisplayName = "Bayonet", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 500, Enabled = true },
        new KnifeEntry { Id = "classic", DisplayName = "Classic Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 503, Enabled = true },
        new KnifeEntry { Id = "flip", DisplayName = "Flip Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 505, Enabled = true },
        new KnifeEntry { Id = "gut", DisplayName = "Gut Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 506, Enabled = true },
        new KnifeEntry { Id = "karambit", DisplayName = "Karambit", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 507, Enabled = true },
        new KnifeEntry { Id = "m9", DisplayName = "M9 Bayonet", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 508, Enabled = true },
        new KnifeEntry { Id = "huntsman", DisplayName = "Huntsman Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 509, Enabled = true },
        new KnifeEntry { Id = "falchion", DisplayName = "Falchion Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 512, Enabled = true },
        new KnifeEntry { Id = "bowie", DisplayName = "Bowie Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 514, Enabled = true },
        new KnifeEntry { Id = "butterfly", DisplayName = "Butterfly Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 515, Enabled = true },
        new KnifeEntry { Id = "shadow_daggers", DisplayName = "Shadow Daggers", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 516, Enabled = true },
        new KnifeEntry { Id = "paracord", DisplayName = "Paracord Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 517, Enabled = true },
        new KnifeEntry { Id = "survival", DisplayName = "Survival Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 518, Enabled = true },
        new KnifeEntry { Id = "ursus", DisplayName = "Ursus Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 519, Enabled = true },
        new KnifeEntry { Id = "navaja", DisplayName = "Navaja Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 520, Enabled = true },
        new KnifeEntry { Id = "nomad", DisplayName = "Nomad Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 521, Enabled = true },
        new KnifeEntry { Id = "stiletto", DisplayName = "Stiletto Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 522, Enabled = true },
        new KnifeEntry { Id = "talon", DisplayName = "Talon Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 523, Enabled = true },
        new KnifeEntry { Id = "skeleton", DisplayName = "Skeleton Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 525, Enabled = true },
        new KnifeEntry { Id = "kukri", DisplayName = "Kukri Knife", WeaponClassName = "weapon_knife", ItemDefinitionIndex = 526, Enabled = true }
    };

    public List<GloveEntry> Gloves { get; set; } = new()
    {
        new GloveEntry { Id = "sport_vice", DisplayName = "Sport Gloves | Vice", ItemDefinitionIndex = 5030, PaintKit = 10048, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "sport_pandora", DisplayName = "Sport Gloves | Pandora's Box", ItemDefinitionIndex = 5030, PaintKit = 10037, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "sport_hedge", DisplayName = "Sport Gloves | Hedge Maze", ItemDefinitionIndex = 5030, PaintKit = 10038, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "sport_superconductor", DisplayName = "Sport Gloves | Superconductor", ItemDefinitionIndex = 5030, PaintKit = 10018, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "sport_amphibious", DisplayName = "Sport Gloves | Amphibious", ItemDefinitionIndex = 5030, PaintKit = 10045, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "specialist_crimson_kimono", DisplayName = "Specialist Gloves | Crimson Kimono", ItemDefinitionIndex = 5034, PaintKit = 10033, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "specialist_fade", DisplayName = "Specialist Gloves | Fade", ItemDefinitionIndex = 5034, PaintKit = 10063, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "specialist_emerald_web", DisplayName = "Specialist Gloves | Emerald Web", ItemDefinitionIndex = 5034, PaintKit = 10034, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "driver_king_snake", DisplayName = "Driver Gloves | King Snake", ItemDefinitionIndex = 5031, PaintKit = 10041, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "driver_imperial_plaid", DisplayName = "Driver Gloves | Imperial Plaid", ItemDefinitionIndex = 5031, PaintKit = 10042, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "handwraps_cobalt_skulls", DisplayName = "Hand Wraps | Cobalt Skulls", ItemDefinitionIndex = 5032, PaintKit = 10053, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "handwraps_overprint", DisplayName = "Hand Wraps | Overprint", ItemDefinitionIndex = 5032, PaintKit = 10054, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "moto_spearmint", DisplayName = "Moto Gloves | Spearmint", ItemDefinitionIndex = 5033, PaintKit = 10026, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "moto_pow", DisplayName = "Moto Gloves | POW!", ItemDefinitionIndex = 5033, PaintKit = 10049, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "bloodhound_charred", DisplayName = "Bloodhound Gloves | Charred", ItemDefinitionIndex = 5027, PaintKit = 10006, Wear = 0.15f, Seed = 0, Enabled = true },
        new GloveEntry { Id = "hydra_case_hardened", DisplayName = "Hydra Gloves | Case Hardened", ItemDefinitionIndex = 5035, PaintKit = 10060, Wear = 0.15f, Seed = 0, Enabled = true }
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

public sealed class KnifeEntry
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Kept for older configs. Stable CS2 knife changing uses ItemDefinitionIndex + ChangeSubclass on the existing knife,
    // not direct weapon_knife_* entity spawning.
    public string WeaponClassName { get; set; } = "weapon_knife";

    // CS2 knife item definition index. Examples: 505 flip, 507 karambit, 508 M9, 515 butterfly.
    public int ItemDefinitionIndex { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class GloveEntry
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // CS2 glove item definition index. Examples: 5030 Sport, 5031 Driver, 5032 Hand Wraps, 5033 Moto, 5034 Specialist.
    public int ItemDefinitionIndex { get; set; }

    // Glove finish/paint kit. Examples: 10048 Vice, 10037 Pandora's Box, 10018 Superconductor.
    public int PaintKit { get; set; }
    public float Wear { get; set; } = 0.15f;
    public int Seed { get; set; } = 0;
    public bool Enabled { get; set; } = true;
}

public sealed class PlayerPreferences
{
    public string? ModelAny { get; set; }
    public string? ModelT { get; set; }
    public string? ModelCT { get; set; }
    public string? SelectedKnife { get; set; }
    public string? SelectedGlove { get; set; }

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
