# SMOKCustomization

A CounterStrikeSharp CS2 plugin for server-side player model selection and conservative weapon paint selection.

## Important limits

This is server-side only. It does **not** inject into clients, does **not** bypass VAC, and does **not** spoof inventory ownership.

Player model changing is the stable part. Weapon paint changing in CS2 is fragile because it touches econ item state. This plugin intentionally avoids knives, gloves, stickers, keychains, and client memory tricks. If you want full WeaponPaints-level behavior, use a dedicated weapon paints plugin/fork and test it carefully with your GSLT risk in mind.

## Commands

Players:

- `!models` - list configured player models
- `!model <id>` - set one model for any team
- `!model t <id>` - set T-side model
- `!model ct <id>` - set CT-side model
- `!modelreset` - clear model preference
- `!skins` - list configured paint presets
- `!skin <weapon> <preset>` - set preset paint, e.g. `!skin ak47 redline`
- `!skin <weapon> <paintkit> [wear] [seed]` - set direct paint values, e.g. `!skin awp 344 0.03 777`
- `!skinreset` - reset all paint choices
- `!skinreset <weapon>` - reset one weapon
- `!wp` - refresh current weapon paint state

Admin/server:

- `css_smokcustom_reload` - reload config and preferences from disk. Default required permission is `@css/config`.

## Install/build

1. Install the .NET 8 SDK on your PC.
2. From the `SMOKCustomization/SMOKCustomization` folder, run:

   ```bash
   dotnet restore
   dotnet publish -c Release
   ```

3. Upload the published files from:

   ```txt
   bin/Release/net8.0/publish/
   ```

   to:

   ```txt
   /game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/
   ```

4. Restart the server.
5. Check load status with:

   ```txt
   css_plugins list
   ```

6. The plugin creates this config after first load:

   ```txt
   /game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/SMOKCustomization.json
   ```

## Model paths

Use `.vmdl` paths, not `.vmdl_c`. Example:

```json
{
  "Id": "vip_ct",
  "DisplayName": "VIP CT",
  "ModelPath": "characters/models/example/vip_ct.vmdl",
  "Team": "CT",
  "Enabled": true,
  "HideLegs": false
}
```

Workshop/custom models must be available to clients through your server/workshop setup. For workshop model packs, you normally need the correct workshop addon handling and model precaching.

## Recommended testing order

1. Load the plugin with only the default config.
2. Test `!models` and `!model default_ct` / `!model default_t`.
3. Add one known-good custom model path.
4. Restart map/server and confirm precache works.
5. Test a simple weapon paint on AK: `!skin ak47 redline`, then respawn or use `!wp`.
6. Only after stable testing, add more models/presets.

## Troubleshooting

- If the server crashes or the model is invisible, the model path is bad or the content is not mounted/sent to clients.
- If paint does not visually refresh, respawn or use `!wp`. Some CS2 econ visuals are cached client-side.
- If the plugin fails to load, make sure your CounterStrikeSharp version supports the referenced API version and your server is using .NET 8-compatible CSS.


## Knife Changer Update

VIP players can use the knife changer when `RequirePermissionForKnifeChanger` is enabled and their active CounterStrikeSharp permissions include the configured `KnifeChangerPermission`.

Default permission:

```txt
@css/reservation
```

Commands:

```txt
!knives
!knife butterfly
!knife karambit
!knife m9
!knife bayonet
!knife flip
!knifereset
```

The knife changer removes the player's current knife and gives the selected knife weapon classname, such as `weapon_knife_butterfly`.

If your CS2 build or another plugin blocks custom knife classnames, leave `EnableKnifeChanger` set to `false`.
