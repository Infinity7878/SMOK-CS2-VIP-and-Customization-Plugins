using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace SMOKCustomization;

public sealed class SMOKCustomizationPlugin : BasePlugin
{
    public override string ModuleName => "SMOK Customization";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "SMOKNetwork / ChatGPT";
    public override string ModuleDescription => "Server-side player model and conservative weapon paint customization for CS2.";

    private JsonStore _store = null!;
    private PluginConfig _config = new();
    private Dictionary<ulong, PlayerPreferences> _preferences = new();
    private ulong _nextSyntheticItemId = 100_000_000_000UL;

    public override void Load(bool hotReload)
    {
        _store = new JsonStore(ModuleDirectory, Logger);
        ReloadFromDisk();

        AddCommand("css_models", "Open/list SMOK player models", OnModelsCommand);
        AddCommand("css_model", "Select a SMOK player model", OnModelCommand);
        AddCommand("css_modelreset", "Reset your SMOK player model", OnModelResetCommand);
        AddCommand("css_skins", "List SMOK skin presets", OnSkinsCommand);
        AddCommand("css_skin", "Set a SMOK skin for a weapon", OnSkinCommand);
        AddCommand("css_skinreset", "Reset SMOK skin selections", OnSkinResetCommand);
        AddCommand("css_wp", "Refresh SMOK weapon paints", OnRefreshPaintsCommand);
        AddCommand("css_smokcustom_reload", "Reload SMOKCustomization config", OnReloadCommand);

        RegisterListener<Listeners.OnServerPrecacheResources>(OnPrecacheResources);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        if (_config.ApplySkinsEverySecond)
        {
            AddTimer(1.0f, ApplyPaintsToAllPlayers, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        Logger.LogInformation("SMOK Customization loaded. Models: {ModelCount}, Paint presets: {PaintCount}",
            _config.PlayerModels.Count, _config.PaintPresets.Count);
    }

    public override void Unload(bool hotReload)
    {
        _store.SavePreferences(_preferences);
        RemoveListener<Listeners.OnServerPrecacheResources>(OnPrecacheResources);
        DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private void ReloadFromDisk()
    {
        _config = _store.LoadConfig();
        _preferences = _store.LoadPreferences();
        NormalizeConfig();
        _store.SaveConfig(_config);
    }

    private void NormalizeConfig()
    {
        _config.PlayerModels = _config.PlayerModels
            .Where(m => !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.ModelPath))
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        _config.PaintPresets = _config.PaintPresets
            .Where(p => !string.IsNullOrWhiteSpace(p.Id) && p.PaintKit > 0)
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        _config.WeaponPaintPermission = string.IsNullOrWhiteSpace(_config.WeaponPaintPermission)
            ? "@css/reservation"
            : _config.WeaponPaintPermission.Trim();
    }

    private void OnPrecacheResources(ResourceManifest manifest)
    {
        if (!_config.Enabled || !_config.EnablePlayerModels)
            return;

        foreach (var model in _config.PlayerModels.Where(m => m.Enabled))
        {
            try
            {
                manifest.AddResource(model.ModelPath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to precache model {ModelPath}", model.ModelPath);
            }
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsUsablePlayer(player))
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            ApplyModel(player!);
            ApplyPaints(player!);
        });

        AddTimer(0.25f, () =>
        {
            if (IsUsablePlayer(player))
            {
                ApplyModel(player!);
                ApplyPaints(player!);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private void OnModelsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;

        var team = GetModelTeam(player!);
        var models = _config.PlayerModels
            .Where(m => m.Enabled && IsModelAllowedForTeam(m, team))
            .OrderBy(m => m.Team)
            .ThenBy(m => m.DisplayName)
            .ToList();

        if (models.Count == 0)
        {
            Reply(player!, "No models are configured for your team.");
            return;
        }

        Reply(player!, "Available models:");
        foreach (var model in models)
        {
            Reply(player!, $"  !model {model.Id}  -  {model.DisplayName} [{model.Team}]");
        }

        Reply(player!, "Use !model <id>, !model t <id>, or !model ct <id>.");
    }

    private void OnModelCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;

        if (!_config.EnablePlayerModels)
        {
            Reply(player!, "Player models are disabled.");
            return;
        }

        if (command.ArgCount < 2)
        {
            Reply(player!, "Usage: !model <id> OR !model <t/ct/all> <id>. Use !models to list options.");
            return;
        }

        string first = command.ArgByIndex(1).Trim();
        string scope = "all";
        string modelId = first;

        if (first.Equals("t", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("ct", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (command.ArgCount < 3)
            {
                Reply(player!, "Usage: !model <t/ct/all> <id>.");
                return;
            }

            scope = first.ToLowerInvariant();
            modelId = command.ArgByIndex(2).Trim();
        }

        var model = _config.PlayerModels.FirstOrDefault(m =>
            m.Enabled && m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

        if (model == null)
        {
            Reply(player!, $"Unknown model '{modelId}'. Use !models.");
            return;
        }

        var pref = GetPrefs(player!);
        switch (scope)
        {
            case "t":
                if (!IsModelAllowedForTeam(model, ModelTeam.T))
                {
                    Reply(player!, "That model is not allowed for T.");
                    return;
                }
                pref.ModelT = model.Id;
                break;
            case "ct":
                if (!IsModelAllowedForTeam(model, ModelTeam.CT))
                {
                    Reply(player!, "That model is not allowed for CT.");
                    return;
                }
                pref.ModelCT = model.Id;
                break;
            default:
                pref.ModelAny = model.Id;
                break;
        }

        SaveAndApply(player!);
        Reply(player!, $"Model set to {model.DisplayName}.");
    }

    private void OnModelResetCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;

        var pref = GetPrefs(player!);
        pref.ModelAny = null;
        pref.ModelT = null;
        pref.ModelCT = null;
        SaveAndApply(player!);
        Reply(player!, "Model preference reset. It will fully return to default next spawn/map unless another plugin changes it.");
    }

    private void OnSkinsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;
        if (!RequireWeaponPaintAccess(player!)) return;

        Reply(player!, "Skin presets:");
        foreach (var preset in _config.PaintPresets.OrderBy(p => p.DisplayName))
        {
            Reply(player!, $"  !skin <weapon> {preset.Id}  -  {preset.DisplayName} / paint {preset.PaintKit}");
        }

        Reply(player!, "Examples: !skin ak47 redline | !skin awp 344 0.03 777 | !wp");
    }

    private void OnSkinCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;
        if (!RequireWeaponPaintAccess(player!)) return;

        if (!_config.EnableWeaponPaints)
        {
            Reply(player!, "Weapon paints are disabled.");
            return;
        }

        if (command.ArgCount < 3)
        {
            Reply(player!, "Usage: !skin <weapon> <preset-or-paintkit> [wear] [seed]. Example: !skin ak47 redline");
            return;
        }

        string weapon = NormalizeWeaponName(command.ArgByIndex(1));
        string paintArg = command.ArgByIndex(2).Trim();

        var selection = new WeaponPaintSelection();
        var preset = _config.PaintPresets.FirstOrDefault(p => p.Id.Equals(paintArg, StringComparison.OrdinalIgnoreCase));
        if (preset != null)
        {
            selection.PaintKit = preset.PaintKit;
            selection.Wear = preset.Wear;
            selection.Seed = preset.Seed;
            selection.PresetId = preset.Id;
        }
        else
        {
            if (!int.TryParse(paintArg, out var paintKit) || paintKit <= 0)
            {
                Reply(player!, "Invalid paint kit. Use !skins for presets, or pass a numeric paint kit id.");
                return;
            }

            selection.PaintKit = paintKit;
            selection.Wear = command.ArgCount >= 4 && float.TryParse(command.ArgByIndex(3), out var wear)
                ? Math.Clamp(wear, 0.0f, 1.0f)
                : 0.07f;
            selection.Seed = command.ArgCount >= 5 && int.TryParse(command.ArgByIndex(4), out var seed)
                ? Math.Clamp(seed, 0, 1000)
                : 0;
        }

        var prefs = GetPrefs(player!);
        prefs.WeaponPaints[weapon] = selection;
        SaveAndApply(player!);

        Reply(player!, $"Skin set for {weapon}: paint {selection.PaintKit}, wear {selection.Wear:0.###}, seed {selection.Seed}. Use !wp if it does not refresh instantly.");
    }

    private void OnSkinResetCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;
        if (!RequireWeaponPaintAccess(player!)) return;

        var prefs = GetPrefs(player!);
        if (command.ArgCount >= 2)
        {
            var weapon = NormalizeWeaponName(command.ArgByIndex(1));
            prefs.WeaponPaints.Remove(weapon);
            Reply(player!, $"Skin reset for {weapon}.");
        }
        else
        {
            prefs.WeaponPaints.Clear();
            Reply(player!, "All skin preferences reset.");
        }

        SaveAndApply(player!);
    }

    private void OnRefreshPaintsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;
        if (!RequireWeaponPaintAccess(player!)) return;

        ApplyPaints(player!);
        Reply(player!, "Weapon paints refreshed.");
    }

    private void OnReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null && !AdminManager.PlayerHasPermissions(player, _config.AdminReloadPermission))
        {
            Reply(player, "You do not have permission to reload this plugin.");
            return;
        }

        ReloadFromDisk();
        ApplyPaintsToAllPlayers();
        foreach (var online in Utilities.GetPlayers().Where(IsUsablePlayer))
        {
            ApplyModel(online);
        }

        if (player != null)
            Reply(player, "SMOK Customization config reloaded.");
        else
            Logger.LogInformation("SMOK Customization config reloaded from server console.");
    }

    private bool RequireWeaponPaintAccess(CCSPlayerController player)
    {
        if (HasWeaponPaintAccess(player))
            return true;

        Reply(player, _config.WeaponPaintNoPermissionMessage);
        return false;
    }

    private bool HasWeaponPaintAccess(CCSPlayerController player)
    {
        if (!_config.RequirePermissionForWeaponPaints)
            return true;

        if (string.IsNullOrWhiteSpace(_config.WeaponPaintPermission))
            return true;

        return AdminManager.PlayerHasPermissions(player, _config.WeaponPaintPermission) ||
               AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private void ApplyPaintsToAllPlayers()
    {
        if (!_config.Enabled || !_config.EnableWeaponPaints)
            return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (IsUsablePlayer(player))
                ApplyPaints(player);
        }
    }

    private void SaveAndApply(CCSPlayerController player)
    {
        _store.SavePreferences(_preferences);
        Server.NextFrame(() =>
        {
            ApplyModel(player);
            ApplyPaints(player);
        });
    }

    private void ApplyModel(CCSPlayerController player)
    {
        if (!_config.Enabled || !_config.EnablePlayerModels || !IsUsablePlayer(player))
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var prefs = GetPrefs(player);
        var team = GetModelTeam(player);
        string? desiredId = team == ModelTeam.CT ? prefs.ModelCT : team == ModelTeam.T ? prefs.ModelT : null;
        desiredId ??= prefs.ModelAny;

        if (string.IsNullOrWhiteSpace(desiredId))
            return;

        var model = _config.PlayerModels.FirstOrDefault(m =>
            m.Enabled && m.Id.Equals(desiredId, StringComparison.OrdinalIgnoreCase) && IsModelAllowedForTeam(m, team));

        if (model == null)
            return;

        try
        {
            pawn.SetModel(model.ModelPath);

            // Alpha 254 hides first-person legs on several model setups without fully hiding the player model.
            var old = pawn.Render;
            pawn.Render = Color.FromArgb(model.HideLegs ? 254 : 255, old.R, old.G, old.B);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to apply model {ModelId} to {PlayerName}", model.Id, player.PlayerName);
        }
    }

    private void ApplyPaints(CCSPlayerController player)
    {
        if (!_config.Enabled || !_config.EnableWeaponPaints || !IsUsablePlayer(player))
            return;

        if (!HasWeaponPaintAccess(player))
            return;

        var prefs = GetPrefs(player);
        if (prefs.WeaponPaints.Count == 0)
            return;

        var pawn = player.PlayerPawn.Value;
        var weapons = pawn?.WeaponServices?.MyWeapons;
        if (pawn == null || !pawn.IsValid || weapons == null || weapons.Count == 0)
            return;

        foreach (var handle in weapons)
        {
            try
            {
                if (!handle.IsValid || handle.Value == null || !handle.Value.IsValid)
                    continue;

                var weapon = handle.Value;
                string weaponName = NormalizeWeaponName(weapon.DesignerName);

                if (!prefs.WeaponPaints.TryGetValue(weaponName, out var selection))
                    continue;

                ApplyPaintToWeapon(player, weapon, selection);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to apply paint for player {PlayerName}", player.PlayerName);
            }
        }
    }

    private void ApplyPaintToWeapon(CCSPlayerController player, CBasePlayerWeapon weapon, WeaponPaintSelection selection)
    {
        if (selection.PaintKit <= 0)
            return;

        var item = weapon.AttributeManager.Item;
        var itemId = _nextSyntheticItemId++;

        item.ItemID = itemId;
        item.ItemIDLow = (uint)(itemId & 0xFFFFFFFF);
        item.ItemIDHigh = (uint)(itemId >> 32);
        item.AccountID = (uint)player.SteamID;

        item.AttributeList.Attributes.RemoveAll();
        item.NetworkedDynamicAttributes.Attributes.RemoveAll();

        weapon.FallbackPaintKit = selection.PaintKit;
        weapon.FallbackWear = Math.Clamp(selection.Wear, 0.0f, 1.0f);
        weapon.FallbackSeed = Math.Clamp(selection.Seed, 0, 1000);

        // These state changes are intentionally conservative. Full knife/glove/sticker support requires extra gamedata/signature
        // handling and is more likely to break across CS2 updates.
        Utilities.SetStateChanged(weapon, "CEconEntity", "m_AttributeManager");
    }

    private PlayerPreferences GetPrefs(CCSPlayerController player)
    {
        var steamId = player.SteamID;
        if (!_preferences.TryGetValue(steamId, out var prefs))
        {
            prefs = new PlayerPreferences();
            _preferences[steamId] = prefs;
        }
        return prefs;
    }

    private bool IsUsablePlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return false;

        if (!_config.AllowBots && player.IsBot)
            return false;

        return player.Connected == PlayerConnectedState.Connected && player.PlayerPawn.IsValid;
    }

    private static ModelTeam GetModelTeam(CCSPlayerController player)
    {
        var team = (CsTeam)player.TeamNum;
        return team switch
        {
            CsTeam.Terrorist => ModelTeam.T,
            CsTeam.CounterTerrorist => ModelTeam.CT,
            _ => ModelTeam.Any
        };
    }

    private static bool IsModelAllowedForTeam(PlayerModelEntry model, ModelTeam team)
    {
        return model.Team == ModelTeam.Any || team == ModelTeam.Any || model.Team == team;
    }

    private static string NormalizeWeaponName(string value)
    {
        value = value.Trim().ToLowerInvariant();
        if (value.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase))
            value = value[7..];

        // Common aliases players type.
        return value switch
        {
            "m4a1s" => "m4a1_silencer",
            "m4a1-s" => "m4a1_silencer",
            "usp" => "usp_silencer",
            "usps" => "usp_silencer",
            "usp-s" => "usp_silencer",
            "deagle" => "deagle",
            "ak" => "ak47",
            "ak-47" => "ak47",
            _ => value
        };
    }

    private void Reply(CCSPlayerController player, string message)
    {
        player.PrintToChat($" {_config.ChatPrefix} {message}");
    }
}
