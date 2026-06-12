using System.Text.Json.Serialization;

namespace SMOKVip;

public sealed class VipConfig
{
    public bool Enabled { get; set; } = true;
    public string ChatPrefix { get; set; } = "\x04[SMOK VIP]\x01";
    public string AdminPermission { get; set; } = "@css/root";
    public bool AllowBots { get; set; } = false;
    public bool ApplyPerksOnSpawn { get; set; } = true;
    public bool AnnounceVipOnConnect { get; set; } = true;
    public bool SaveExpiredPlayersInDatabase { get; set; } = true;
    public int MaxDisplayedVipListEntries { get; set; } = 20;

    // When enabled, active VIP players receive temporary CounterStrikeSharp permissions while online.
    // This is what lets VIP unlock other plugins that check CSS permissions, such as trails.
    public bool GrantCounterStrikeSharpPermissions { get; set; } = true;
    public bool RemoveGrantedPermissionsWhenVipInactive { get; set; } = true;
    public List<string> PermissionsGrantedToActiveVip { get; set; } = new()
    {
        "@css/reservation",
        "@css/vip"
    };

    // If enabled, the plugin writes a simple file that other SMOK plugins can read.
    public bool WriteSharedExportFile { get; set; } = true;
    public string SharedExportFileName { get; set; } = "smok_vip_export.json";

    public List<VipTier> Tiers { get; set; } = new()
    {
        new VipTier
        {
            Id = "vip",
            DisplayName = "VIP",
            Priority = 10,
            SpawnHealth = 100,
            SpawnArmor = 100,
            GiveHelmet = true,
            SpawnMessage = "Your VIP perks are active."
        },
        new VipTier
        {
            Id = "vipplus",
            DisplayName = "VIP+",
            Priority = 20,
            SpawnHealth = 110,
            SpawnArmor = 100,
            GiveHelmet = true,
            SpawnMessage = "Your VIP+ perks are active."
        }
    };

    public List<string> BenefitLines { get; set; } = new()
    {
        "VIP access to the skin changer",
        "VIP access to trails through @css/reservation",
        "VIP player models / skins when used with SMOKCustomization",
        "Armor + helmet on spawn",
        "VIP status command",
        "One-time redeem codes for easy purchases"
    };
}

public sealed class VipTier
{
    public string Id { get; set; } = "vip";
    public string DisplayName { get; set; } = "VIP";
    public int Priority { get; set; } = 10;
    public int SpawnHealth { get; set; } = 100;
    public int SpawnArmor { get; set; } = 100;
    public bool GiveHelmet { get; set; } = true;
    public string SpawnMessage { get; set; } = "Your VIP perks are active.";
}

public sealed class VipDatabase
{
    public Dictionary<ulong, VipRecord> Players { get; set; } = new();
    public Dictionary<string, RedeemCodeRecord> RedeemCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class VipRecord
{
    public ulong SteamId { get; set; }
    public string TierId { get; set; } = "vip";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string Source { get; set; } = "manual";
    public string Note { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsLifetime => ExpiresAtUtc == null;

    public bool IsActive(DateTimeOffset nowUtc) => ExpiresAtUtc == null || ExpiresAtUtc > nowUtc;
}

public sealed class RedeemCodeRecord
{
    public string Code { get; set; } = string.Empty;
    public string TierId { get; set; } = "vip";
    public int Days { get; set; } = 30;
    public int UsesRemaining { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public List<ulong> RedeemedBy { get; set; } = new();

    public bool IsActive(DateTimeOffset nowUtc)
    {
        if (UsesRemaining <= 0) return false;
        if (ExpiresAtUtc != null && ExpiresAtUtc <= nowUtc) return false;
        return true;
    }
}

public sealed class VipExport
{
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<ulong, VipExportRecord> ActivePlayers { get; set; } = new();
}

public sealed class VipExportRecord
{
    public ulong SteamId { get; set; }
    public string TierId { get; set; } = "vip";
    public string TierName { get; set; } = "VIP";
    public int TierPriority { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
