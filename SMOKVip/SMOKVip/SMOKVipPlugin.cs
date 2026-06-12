using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace SMOKVip;

public sealed class SMOKVipPlugin : BasePlugin
{
    public override string ModuleName => "SMOK VIP";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "SMOKNetwork / ChatGPT";
    public override string ModuleDescription => "VIP membership, redeem codes, and spawn perks for CS2 CounterStrikeSharp servers.";

    private JsonStore _store = null!;
    private VipConfig _config = new();
    private VipDatabase _database = new();

    // Tracks only permissions this plugin added at runtime so we do not accidentally remove
    // permissions that came from admins.json/admin_groups.json.
    private readonly Dictionary<ulong, HashSet<string>> _runtimeGrantedPermissions = new();

    public override void Load(bool hotReload)
    {
        _store = new JsonStore(ModuleDirectory, Logger);
        ReloadFromDisk();

        AddCommand("css_vip", "Show your VIP status", OnVipCommand);
        AddCommand("css_vipstatus", "Show VIP status", OnVipStatusCommand);
        AddCommand("css_vipbenefits", "List VIP benefits", OnVipBenefitsCommand);
        AddCommand("css_redeem", "Redeem a VIP code", OnRedeemCommand);

        AddCommand("css_vip_add", "Add VIP to a SteamID64", OnVipAddCommand);
        AddCommand("css_vip_remove", "Remove VIP from a SteamID64", OnVipRemoveCommand);
        AddCommand("css_vip_code", "Create a VIP redeem code", OnVipCodeCommand);
        AddCommand("css_vip_list", "List active VIPs", OnVipListCommand);
        AddCommand("css_vip_reload", "Reload SMOK VIP config/database", OnVipReloadCommand);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);

        AddTimer(1.0f, SyncVipPermissionsForOnlinePlayers, TimerFlags.STOP_ON_MAPCHANGE);
        AddTimer(60.0f, () =>
        {
            CleanupExpiredAndExport();
            SyncVipPermissionsForOnlinePlayers();
        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

        Logger.LogInformation("SMOK VIP loaded. Active VIPs: {Count}, redeem codes: {CodeCount}",
            CountActiveVips(), _database.RedeemCodes.Count);
    }

    public override void Unload(bool hotReload)
    {
        RemoveRuntimeVipPermissionsFromOnlinePlayers();
        _store.SaveDatabase(_database);
        _store.SaveExport(_config, _database);
        DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    }

    private void ReloadFromDisk()
    {
        _config = _store.LoadConfig();
        _database = _store.LoadDatabase();
        NormalizeConfig();
        _store.SaveConfig(_config);
        CleanupExpiredAndExport();
    }

    private void NormalizeConfig()
    {
        _config.Tiers = _config.Tiers
            .Where(t => !string.IsNullOrWhiteSpace(t.Id))
            .GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t.Priority)
            .ToList();

        if (_config.Tiers.Count == 0)
            _config.Tiers.Add(new VipTier());

        foreach (var record in _database.Players.Values)
        {
            if (record.SteamId == 0) continue;
            if (string.IsNullOrWhiteSpace(record.TierId))
                record.TierId = _config.Tiers[0].Id;
        }

        _config.PermissionsGrantedToActiveVip = _config.PermissionsGrantedToActiveVip
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Where(p => p.StartsWith('@'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsUsablePlayer(player)) return HookResult.Continue;
        if (!_config.Enabled) return HookResult.Continue;

        AddTimer(2.0f, () =>
        {
            if (!IsUsablePlayer(player)) return;
            ApplyVipPermissions(player!);
            var active = GetActiveVip(player!.SteamID);
            if (active.Record == null || active.Tier == null) return;

            if (_config.AnnounceVipOnConnect)
                Reply(player, $"Welcome back, {active.Tier.DisplayName}. VIP expires: {FormatExpiration(active.Record)}");
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsUsablePlayer(player)) return HookResult.Continue;
        if (!_config.Enabled || !_config.ApplyPerksOnSpawn) return HookResult.Continue;

        AddTimer(0.25f, () =>
        {
            if (IsUsablePlayer(player))
            {
                ApplyVipPermissions(player!);
                ApplyVipSpawnPerks(player!);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private void ApplyVipSpawnPerks(CCSPlayerController player)
    {
        var active = GetActiveVip(player.SteamID);
        if (active.Record == null || active.Tier == null) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        try
        {
            var tier = active.Tier;

            if (tier.GiveHelmet)
            {
                player.GiveNamedItem("item_assaultsuit");
            }

            if (tier.SpawnHealth > 0)
            {
                pawn.MaxHealth = Math.Max(100, tier.SpawnHealth);
                pawn.Health = Math.Max(1, tier.SpawnHealth);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }

            if (tier.SpawnArmor >= 0)
            {
                pawn.ArmorValue = Math.Clamp(tier.SpawnArmor, 0, 100);
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
            }

            if (!string.IsNullOrWhiteSpace(tier.SpawnMessage))
                Reply(player, tier.SpawnMessage);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to apply VIP spawn perks for {PlayerName}", player.PlayerName);
        }
    }

    private void OnVipCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;

        var active = GetActiveVip(player!.SteamID);
        if (active.Record == null || active.Tier == null)
        {
            Reply(player, "You do not have VIP active. Buy VIP, then redeem your code with !redeem <code>.");
            Reply(player, "Use !vipbenefits to see what VIP includes.");
            return;
        }

        Reply(player, $"Status: {active.Tier.DisplayName}. Expires: {FormatExpiration(active.Record)}");
    }

    private void OnVipBenefitsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;

        Reply(player!, "VIP benefits:");
        foreach (var line in _config.BenefitLines)
            Reply(player!, $"- {line}");
    }

    private void OnVipStatusCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            Logger.LogInformation("Use css_vip_list from server console to list VIPs.");
            return;
        }

        if (!IsUsablePlayer(player)) return;

        if (command.ArgCount >= 2)
        {
            if (!IsAdmin(player))
            {
                Reply(player, "You do not have permission to check another player's VIP status.");
                return;
            }

            if (!TryParseSteamId(command.ArgByIndex(1), out var steamId))
            {
                Reply(player, "Invalid SteamID64.");
                return;
            }

            SendVipStatus(player, steamId);
            return;
        }

        SendVipStatus(player, player.SteamID);
    }

    private void OnRedeemCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;

        if (command.ArgCount < 2)
        {
            Reply(player!, "Usage: !redeem <code>");
            return;
        }

        var code = NormalizeCode(command.ArgByIndex(1));
        if (!_database.RedeemCodes.TryGetValue(code, out var record) || !record.IsActive(DateTimeOffset.UtcNow))
        {
            Reply(player!, "That code is invalid, expired, or already used.");
            return;
        }

        if (record.RedeemedBy.Contains(player!.SteamID))
        {
            Reply(player, "You already redeemed that code.");
            return;
        }

        GrantVip(player.SteamID, record.Days, record.TierId, "redeem", $"Redeemed code {code}");
        ApplyVipPermissions(player);
        record.UsesRemaining--;
        record.RedeemedBy.Add(player.SteamID);

        _store.SaveDatabase(_database);
        _store.SaveExport(_config, _database);

        var active = GetActiveVip(player.SteamID);
        Reply(player, $"Code redeemed. You now have {active.Tier?.DisplayName ?? record.TierId}. Expires: {FormatExpiration(active.Record!)}");
    }

    private void OnVipAddCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CanRunAdminCommand(player)) return;

        if (command.ArgCount < 3)
        {
            AdminReply(player, "Usage: css_vip_add <steamid64> <days|lifetime> [tier] [note]");
            return;
        }

        if (!TryParseSteamId(command.ArgByIndex(1), out var steamId))
        {
            AdminReply(player, "Invalid SteamID64.");
            return;
        }

        int days = ParseDaysOrLifetime(command.ArgByIndex(2));
        if (days == 0)
        {
            AdminReply(player, "Invalid days. Use a positive number or lifetime.");
            return;
        }

        var tierId = command.ArgCount >= 4 ? command.ArgByIndex(3).Trim() : _config.Tiers[0].Id;
        if (!TierExists(tierId))
        {
            AdminReply(player, $"Unknown tier '{tierId}'. Valid tiers: {string.Join(", ", _config.Tiers.Select(t => t.Id))}");
            return;
        }

        var note = command.ArgCount >= 5 ? string.Join(" ", Enumerable.Range(4, command.ArgCount - 4).Select(i => command.ArgByIndex(i))) : string.Empty;
        GrantVip(steamId, days, tierId, "manual", note);
        ApplyVipPermissionsToSteamId(steamId);
        _store.SaveDatabase(_database);
        _store.SaveExport(_config, _database);

        var active = GetActiveVip(steamId);
        AdminReply(player, $"Granted {active.Tier?.DisplayName ?? tierId} to {steamId}. Expires: {FormatExpiration(active.Record!)}");
    }

    private void OnVipRemoveCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CanRunAdminCommand(player)) return;

        if (command.ArgCount < 2 || !TryParseSteamId(command.ArgByIndex(1), out var steamId))
        {
            AdminReply(player, "Usage: css_vip_remove <steamid64>");
            return;
        }

        if (_database.Players.Remove(steamId))
        {
            RemoveVipPermissionsFromSteamId(steamId);
            _store.SaveDatabase(_database);
            _store.SaveExport(_config, _database);
            AdminReply(player, $"Removed VIP from {steamId}.");
        }
        else
        {
            AdminReply(player, $"{steamId} does not have a VIP record.");
        }
    }

    private void OnVipCodeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CanRunAdminCommand(player)) return;

        if (command.ArgCount < 3)
        {
            AdminReply(player, "Usage: css_vip_code <code|auto> <days|lifetime> [tier] [uses]");
            return;
        }

        var codeInput = command.ArgByIndex(1);
        var code = codeInput.Equals("auto", StringComparison.OrdinalIgnoreCase)
            ? GenerateCode()
            : NormalizeCode(codeInput);

        if (code.Length < 4)
        {
            AdminReply(player, "Code must be at least 4 characters.");
            return;
        }

        int days = ParseDaysOrLifetime(command.ArgByIndex(2));
        if (days == 0)
        {
            AdminReply(player, "Invalid days. Use a positive number or lifetime.");
            return;
        }

        var tierId = command.ArgCount >= 4 ? command.ArgByIndex(3).Trim() : _config.Tiers[0].Id;
        if (!TierExists(tierId))
        {
            AdminReply(player, $"Unknown tier '{tierId}'. Valid tiers: {string.Join(", ", _config.Tiers.Select(t => t.Id))}");
            return;
        }

        int uses = command.ArgCount >= 5 && int.TryParse(command.ArgByIndex(4), out var parsedUses)
            ? Math.Max(1, parsedUses)
            : 1;

        _database.RedeemCodes[code] = new RedeemCodeRecord
        {
            Code = code,
            Days = days,
            TierId = tierId,
            UsesRemaining = uses,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _store.SaveDatabase(_database);
        AdminReply(player, $"Created VIP code: {code} | tier: {tierId} | days: {(days < 0 ? "lifetime" : days)} | uses: {uses}");
    }

    private void OnVipListCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CanRunAdminCommand(player)) return;

        var now = DateTimeOffset.UtcNow;
        var active = _database.Players.Values
            .Where(v => v.IsActive(now))
            .OrderBy(v => v.ExpiresAtUtc ?? DateTimeOffset.MaxValue)
            .Take(Math.Max(1, _config.MaxDisplayedVipListEntries))
            .ToList();

        if (active.Count == 0)
        {
            AdminReply(player, "No active VIPs.");
            return;
        }

        AdminReply(player, $"Active VIPs showing {active.Count}/{CountActiveVips()}:");
        foreach (var vip in active)
        {
            var tier = FindTier(vip.TierId);
            AdminReply(player, $"{vip.SteamId} | {tier?.DisplayName ?? vip.TierId} | expires {FormatExpiration(vip)}");
        }
    }

    private void OnVipReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CanRunAdminCommand(player)) return;

        ReloadFromDisk();
        SyncVipPermissionsForOnlinePlayers();
        AdminReply(player, "SMOK VIP config/database reloaded.");
    }

    private void SyncVipPermissionsForOnlinePlayers()
    {
        if (!_config.Enabled || !_config.GrantCounterStrikeSharpPermissions)
        {
            RemoveRuntimeVipPermissionsFromOnlinePlayers();
            return;
        }

        foreach (var player in Utilities.GetPlayers().Where(IsUsablePlayer))
            ApplyVipPermissions(player);
    }

    private void ApplyVipPermissionsToSteamId(ulong steamId)
    {
        var player = Utilities.GetPlayerFromSteamId64(steamId);
        if (IsUsablePlayer(player))
            ApplyVipPermissions(player!);
    }

    private void ApplyVipPermissions(CCSPlayerController player)
    {
        if (!_config.Enabled || !_config.GrantCounterStrikeSharpPermissions || !IsUsablePlayer(player))
            return;

        if (_config.PermissionsGrantedToActiveVip.Count == 0)
            return;

        var active = GetActiveVip(player.SteamID);
        if (active.Record == null || active.Tier == null)
        {
            if (_config.RemoveGrantedPermissionsWhenVipInactive)
                RemoveVipPermissions(player);
            return;
        }

        try
        {
            var missing = _config.PermissionsGrantedToActiveVip
                .Where(permission => !AdminManager.PlayerHasPermissions(player, permission))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (missing.Length == 0)
                return;

            AdminManager.AddPlayerPermissions(player.AuthorizedSteamID, missing);

            if (!_runtimeGrantedPermissions.TryGetValue(player.SteamID, out var granted))
            {
                granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _runtimeGrantedPermissions[player.SteamID] = granted;
            }

            foreach (var permission in missing)
                granted.Add(permission);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to apply VIP CSS permissions to {PlayerName} ({SteamId})", player.PlayerName, player.SteamID);
        }
    }

    private void RemoveVipPermissionsFromSteamId(ulong steamId)
    {
        var player = Utilities.GetPlayerFromSteamId64(steamId);
        if (IsUsablePlayer(player))
            RemoveVipPermissions(player!);
        else
            _runtimeGrantedPermissions.Remove(steamId);
    }

    private void RemoveRuntimeVipPermissionsFromOnlinePlayers()
    {
        foreach (var player in Utilities.GetPlayers().Where(IsUsablePlayer))
            RemoveVipPermissions(player);

        _runtimeGrantedPermissions.Clear();
    }

    private void RemoveVipPermissions(CCSPlayerController player)
    {
        if (!_runtimeGrantedPermissions.TryGetValue(player.SteamID, out var granted) || granted.Count == 0)
            return;

        try
        {
            AdminManager.RemovePlayerPermissions(player.AuthorizedSteamID, granted.ToArray());
            _runtimeGrantedPermissions.Remove(player.SteamID);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to remove VIP CSS permissions from {PlayerName} ({SteamId})", player.PlayerName, player.SteamID);
        }
    }

    private void SendVipStatus(CCSPlayerController player, ulong steamId)
    {
        var active = GetActiveVip(steamId);
        if (active.Record == null || active.Tier == null)
        {
            Reply(player, $"{steamId} does not have active VIP.");
            return;
        }

        Reply(player, $"{steamId}: {active.Tier.DisplayName}. Expires: {FormatExpiration(active.Record)}");
    }

    private void GrantVip(ulong steamId, int days, string tierId, string source, string note)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = _database.Players.TryGetValue(steamId, out var old) ? old : null;
        var startFrom = existing != null && existing.IsActive(now) && existing.ExpiresAtUtc != null
            ? existing.ExpiresAtUtc.Value
            : now;

        DateTimeOffset? expires = days < 0 ? null : startFrom.AddDays(days);

        _database.Players[steamId] = new VipRecord
        {
            SteamId = steamId,
            TierId = tierId,
            CreatedAtUtc = existing?.CreatedAtUtc ?? now,
            ExpiresAtUtc = expires,
            Source = source,
            Note = note
        };
    }

    private (VipRecord? Record, VipTier? Tier) GetActiveVip(ulong steamId)
    {
        if (!_config.Enabled) return (null, null);
        if (!_database.Players.TryGetValue(steamId, out var record)) return (null, null);
        if (!record.IsActive(DateTimeOffset.UtcNow)) return (null, null);
        return (record, FindTier(record.TierId) ?? _config.Tiers.OrderByDescending(t => t.Priority).FirstOrDefault());
    }

    private VipTier? FindTier(string tierId)
    {
        return _config.Tiers.FirstOrDefault(t => t.Id.Equals(tierId, StringComparison.OrdinalIgnoreCase));
    }

    private bool TierExists(string tierId) => FindTier(tierId) != null;

    private bool IsAdmin(CCSPlayerController player)
    {
        return AdminManager.PlayerHasPermissions(player, _config.AdminPermission) ||
               AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private bool CanRunAdminCommand(CCSPlayerController? player)
    {
        if (player == null) return true; // server console
        if (IsUsablePlayer(player) && IsAdmin(player)) return true;
        Reply(player, "You do not have permission to use this command.");
        return false;
    }

    private void CleanupExpiredAndExport()
    {
        if (!_config.SaveExpiredPlayersInDatabase)
        {
            var now = DateTimeOffset.UtcNow;
            var expired = _database.Players
                .Where(pair => !pair.Value.IsActive(now))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var steamId in expired)
                _database.Players.Remove(steamId);

            if (expired.Count > 0)
                _store.SaveDatabase(_database);
        }

        _store.SaveExport(_config, _database);
    }

    private int CountActiveVips()
    {
        var now = DateTimeOffset.UtcNow;
        return _database.Players.Values.Count(v => v.IsActive(now));
    }

    private bool IsUsablePlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid) return false;
        if (!_config.AllowBots && player.IsBot) return false;
        return player.Connected == PlayerConnectedState.Connected;
    }

    private static bool TryParseSteamId(string value, out ulong steamId)
    {
        value = value.Trim();
        return ulong.TryParse(value, out steamId) && steamId > 0;
    }

    // Returns -1 for lifetime, 0 for invalid, positive days otherwise.
    private static int ParseDaysOrLifetime(string value)
    {
        if (value.Equals("lifetime", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("permanent", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("perm", StringComparison.OrdinalIgnoreCase))
            return -1;

        return int.TryParse(value, out var days) && days > 0 ? days : 0;
    }

    private static string NormalizeCode(string value)
    {
        return new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

    private static string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return "SMOK-" + new string(Enumerable.Range(0, 10).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
    }

    private static string FormatExpiration(VipRecord record)
    {
        if (record.ExpiresAtUtc == null) return "lifetime";

        var remaining = record.ExpiresAtUtc.Value - DateTimeOffset.UtcNow;
        if (remaining.TotalSeconds <= 0) return "expired";

        if (remaining.TotalDays >= 1)
            return $"{record.ExpiresAtUtc.Value:yyyy-MM-dd} UTC ({Math.Ceiling(remaining.TotalDays)} days left)";

        return $"{record.ExpiresAtUtc.Value:yyyy-MM-dd HH:mm} UTC ({Math.Ceiling(remaining.TotalHours)} hours left)";
    }

    private void Reply(CCSPlayerController player, string message)
    {
        player.PrintToChat($" {_config.ChatPrefix} {message}");
    }

    private void AdminReply(CCSPlayerController? player, string message)
    {
        if (player == null)
            Logger.LogInformation(message);
        else
            Reply(player, message);
    }
}
