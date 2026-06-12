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
    public override string ModuleVersion => "1.1.2";
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
        AddCommand("css_knives", "List SMOK knives", OnKnivesCommand);
        AddCommand("css_knife", "Select a SMOK knife", OnKnifeCommand);
        AddCommand("css_knifereset", "Reset your SMOK knife", OnKnifeResetCommand);
        AddCommand("css_smokcustom_reload", "Reload SMOKCustomization config", OnReloadCommand);

        RegisterListener<Listeners.OnServerPrecacheResources>(OnPrecacheResources);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        if (_config.ApplySkinsEverySecond)
        {
            AddTimer(1.0f, ApplyPaintsToAllPlayers, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        Logger.LogInformation("SMOK Customization loaded. Models: {ModelCount}, Paint presets: {PaintCount}, Knives: {KnifeCount}, KnifeSubclassChanger: {KnifeSubclassChanger}",
            _config.PlayerModels.Count, _config.PaintPresets.Count, _config.Knives.Count, _config.EnableKnifeSubclassChanger);
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

        _config.Knives = _config.Knives
            .Where(k => !string.IsNullOrWhiteSpace(k.Id))
            .GroupBy(k => k.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var knife = g.First();
                if (knife.ItemDefinitionIndex <= 0)
                    knife.ItemDefinitionIndex = GuessKnifeDefinitionIndex(knife);
                if (string.IsNullOrWhiteSpace(knife.WeaponClassName))
                    knife.WeaponClassName = "weapon_knife";
                return knife;
            })
            .Where(k => k.ItemDefinitionIndex > 0)
            .ToList();

        _config.WeaponPaintPermission = string.IsNullOrWhiteSpace(_config.WeaponPaintPermission)
            ? "@css/reservation"
            : _config.WeaponPaintPermission.Trim();

        _config.KnifeChangerPermission = string.IsNullOrWhiteSpace(_config.KnifeChangerPermission)
            ? "@css/reservation"
            : _config.KnifeChangerPermission.Trim();
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
            ApplyKnife(player!);
            ApplyPaints(player!);
        });

        AddTimer(0.25f, () =>
        {
            if (IsUsablePlayer(player))
            {
                ApplyModel(player!);
                ApplyKnife(player!);
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

    private void OnKnivesCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;
        if (!RequireKnifeChangerAccess(player!)) return;

        if (!_config.EnableKnifeChanger)
        {
            Reply(player!, "Knife changer is disabled.");
            return;
        }

        var knives = _config.Knives
            .Where(k => k.Enabled)
            .OrderBy(k => k.DisplayName)
            .ToList();

        if (knives.Count == 0)
        {
            Reply(player!, "No knives are configured.");
            return;
        }

        Reply(player!, "Available knives:");
        foreach (var knife in knives)
        {
            Reply(player!, $"  !knife {knife.Id}  -  {knife.DisplayName}");
        }

        Reply(player!, "Use !knife <id>. Example: !knife butterfly");
    }

    private void OnKnifeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;
        if (!RequireKnifeChangerAccess(player!)) return;

        if (!_config.EnableKnifeChanger)
        {
            Reply(player!, "Knife changer is disabled.");
            return;
        }

        if (command.ArgCount < 2)
        {
            Reply(player!, "Usage: !knife <id>. Use !knives to list available knives.");
            return;
        }

        string knifeId = command.ArgByIndex(1).Trim();
        var knife = _config.Knives.FirstOrDefault(k =>
            k.Enabled && k.Id.Equals(knifeId, StringComparison.OrdinalIgnoreCase));

        if (knife == null)
        {
            Reply(player!, $"Unknown knife '{knifeId}'. Use !knives.");
            return;
        }

        var prefs = GetPrefs(player!);
        prefs.SelectedKnife = knife.Id;
        _store.SavePreferences(_preferences);

        if (!_config.EnableKnifeSubclassChanger)
        {
            Reply(player!, $"Knife preference saved as {knife.DisplayName}, but the knife subclass changer is disabled in config.");
            return;
        }

        if (_config.ApplyKnifeImmediatelyOnCommand)
        {
            Server.NextFrame(() => ApplyKnife(player!));
            AddTimer(0.15f, () =>
            {
                if (IsUsablePlayer(player))
                    ApplyKnife(player!);
            }, TimerFlags.STOP_ON_MAPCHANGE);
            Reply(player!, $"Knife set to {knife.DisplayName}. It should apply now or after your next respawn.");
        }
        else
        {
            Reply(player!, $"Knife set to {knife.DisplayName}. It will apply on your next spawn.");
        }
    }

    private void OnKnifeResetCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsUsablePlayer(player)) return;
        if (!RequireKnifeChangerAccess(player!)) return;

        var prefs = GetPrefs(player!);
        prefs.SelectedKnife = null;
        _store.SavePreferences(_preferences);
        Reply(player!, "Knife preference reset.");
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
            ApplyKnife(online);
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

    private bool RequireKnifeChangerAccess(CCSPlayerController player)
    {
        if (HasKnifeChangerAccess(player))
            return true;

        Reply(player, _config.KnifeNoPermissionMessage);
        return false;
    }

    private bool HasKnifeChangerAccess(CCSPlayerController player)
    {
        if (!_config.RequirePermissionForKnifeChanger)
            return true;

        if (string.IsNullOrWhiteSpace(_config.KnifeChangerPermission))
            return true;

        return AdminManager.PlayerHasPermissions(player, _config.KnifeChangerPermission) ||
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
            ApplyKnife(player);
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

    private void ApplyKnife(CCSPlayerController player)
    {
        if (!_config.Enabled || !_config.EnableKnifeChanger || !_config.EnableKnifeSubclassChanger || !IsUsablePlayer(player))
            return;

        if (!HasKnifeChangerAccess(player))
            return;

        var prefs = GetPrefs(player);
        if (string.IsNullOrWhiteSpace(prefs.SelectedKnife))
            return;

        var knife = _config.Knives.FirstOrDefault(k =>
            k.Enabled && k.Id.Equals(prefs.SelectedKnife, StringComparison.OrdinalIgnoreCase));

        if (knife == null || knife.ItemDefinitionIndex <= 0)
            return;

        var weapon = FindCurrentKnife(player);
        if (weapon == null || !weapon.IsValid)
            return;

        try
        {
            ApplyKnifeSubclass(player, weapon, knife);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to apply knife {KnifeId} to {PlayerName}", knife.Id, player.PlayerName);
        }
    }

    private CBasePlayerWeapon? FindCurrentKnife(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        var weapons = pawn?.WeaponServices?.MyWeapons;
        if (pawn == null || !pawn.IsValid || weapons == null)
            return null;

        foreach (var handle in weapons.ToList())
        {
            if (!handle.IsValid || handle.Value == null || !handle.Value.IsValid)
                continue;

            var weapon = handle.Value;
            string normalized = NormalizeWeaponName(weapon.DesignerName);
            if (IsKnifeWeapon(normalized))
                return weapon;
        }

        return null;
    }

    private void ApplyKnifeSubclass(CCSPlayerController player, CBasePlayerWeapon weapon, KnifeEntry knife)
    {
        ushort definitionIndex = (ushort)knife.ItemDefinitionIndex;
        var item = weapon.AttributeManager.Item;

        // ChangeSubclass is the important part. It changes the subclass of the player's existing knife entity.
        // This avoids the crash-prone method of spawning weapon_knife_flip / weapon_knife_butterfly entities directly.
        weapon.AcceptInput("ChangeSubclass", value: definitionIndex.ToString());

        if (item.ItemDefinitionIndex != definitionIndex)
            item.ItemDefinitionIndex = definitionIndex;

        item.EntityQuality = 3;
        item.AccountID = (uint)player.SteamID;

        var itemId = _nextSyntheticItemId++;
        item.ItemID = itemId;
        item.ItemIDLow = (uint)(itemId & 0xFFFFFFFF);
        item.ItemIDHigh = (uint)(itemId >> 32);

        item.AttributeList.Attributes.RemoveAll();
        item.NetworkedDynamicAttributes.Attributes.RemoveAll();

        Utilities.SetStateChanged(weapon, "CEconEntity", "m_AttributeManager");

        try
        {
            player.ExecuteClientCommand("slot3");
        }
        catch
        {
            // Not critical. The knife will still apply on spawn or when the player switches weapons.
        }
    }

    private void RemoveCurrentKnives(CCSPlayerController player, string normalizedDesiredKnife)
    {
        var pawn = player.PlayerPawn.Value;
        var weapons = pawn?.WeaponServices?.MyWeapons;
        if (pawn == null || !pawn.IsValid || weapons == null)
            return;

        foreach (var handle in weapons.ToList())
        {
            try
            {
                if (!handle.IsValid || handle.Value == null || !handle.Value.IsValid)
                    continue;

                var weapon = handle.Value;
                string normalized = NormalizeWeaponName(weapon.DesignerName);
                if (!IsKnifeWeapon(normalized))
                    continue;

                if (normalized.Equals(normalizedDesiredKnife, StringComparison.OrdinalIgnoreCase))
                    continue;

                weapon.Remove();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to remove existing knife for {PlayerName}", player.PlayerName);
            }
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

    private static int GuessKnifeDefinitionIndex(KnifeEntry knife)
    {
        string combined = $"{knife.Id} {knife.DisplayName} {knife.WeaponClassName}".ToLowerInvariant();

        if (combined.Contains("butterfly")) return 515;
        if (combined.Contains("karambit")) return 507;
        if (combined.Contains("m9")) return 508;
        if (combined.Contains("bayonet") && !combined.Contains("m9")) return 500;
        if (combined.Contains("flip")) return 505;
        if (combined.Contains("gut")) return 506;
        if (combined.Contains("tactical") || combined.Contains("huntsman")) return 509;
        if (combined.Contains("falchion")) return 512;
        if (combined.Contains("bowie")) return 514;
        if (combined.Contains("shadow") || combined.Contains("dagger")) return 516;
        if (combined.Contains("ursus")) return 519;
        if (combined.Contains("navaja")) return 520;
        if (combined.Contains("stiletto")) return 522;
        if (combined.Contains("talon")) return 523;
        if (combined.Contains("classic")) return 503;
        if (combined.Contains("skeleton")) return 525;
        if (combined.Contains("survival")) return 518;
        if (combined.Contains("paracord")) return 517;
        if (combined.Contains("nomad")) return 521;
        if (combined.Contains("kukri")) return 526;

        return 0;
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
            "bfk" => "knife_butterfly",
            "butterfly" => "knife_butterfly",
            "karambit" => "knife_karambit",
            "m9" => "knife_m9_bayonet",
            "m9bayonet" => "knife_m9_bayonet",
            "flip" => "knife_flip",
            _ => value
        };
    }

    private static bool IsKnifeWeapon(string normalizedWeaponName)
    {
        return normalizedWeaponName.Equals("bayonet", StringComparison.OrdinalIgnoreCase) ||
               normalizedWeaponName.Equals("knife", StringComparison.OrdinalIgnoreCase) ||
               normalizedWeaponName.StartsWith("knife_", StringComparison.OrdinalIgnoreCase);
    }

    private void Reply(CCSPlayerController player, string message)
    {
        player.PrintToChat($" {_config.ChatPrefix} {message}");
    }
}
