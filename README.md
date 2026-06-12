SMOK CS2 Plugins
CounterStrikeSharp plugins for CS2 servers that add a VIP membership system, redeemable VIP codes, VIP permission syncing, player model customization, and VIP-locked weapon skin selection.
This project is intended for server owners who want a simple VIP system that can integrate with other CounterStrikeSharp plugins, such as trails plugins that use `@css/reservation`.
---
<div align="center">
SMOKNetwork Support
Need help installing, configuring, or troubleshooting the plugins?
![Join Discord](https://img.shields.io/badge/Join%20Our%20Discord-Support%20%26%20Updates-5865F2?style=for-the-badge&logo=discord&logoColor=white)
Plugin support • Bug reports • Feature requests • CS2 server discussion
</div>
---

Included Plugins
SMOKVip
A VIP membership plugin with manual VIP management, redeem codes, expiration tracking, and runtime permission syncing.
Features:
Timed VIP memberships
Lifetime VIP memberships
VIP and VIP+ tiers
Redeemable one-time or multi-use VIP codes
Manual VIP add/remove commands
VIP status command
VIP benefits command
Spawn perks such as armor, helmet, and optional bonus HP
Runtime CounterStrikeSharp permissions for active VIP players
`@css/reservation` support for trails/reserved-slot style plugins
JSON-based storage
Export file for other plugins to read active VIPs
SMOKCustomization
A customization plugin for player models and weapon skin presets.
Features:
Player model selection
Player model reset
Weapon skin preset selection
Weapon skin reset
Weapon paint refresh command
JSON-based player preferences
Optional VIP/permission lock for the skin changer
Designed to work alongside SMOKVip
---
Requirements
A working CS2 dedicated server
CounterStrikeSharp installed
Metamod installed as required by CounterStrikeSharp
.NET-compatible CounterStrikeSharp runtime
Plugin projects currently target `.NET 10.0`
A CounterStrikeSharp version compatible with the `CounterStrikeSharp.API` package used by the project
If you use an older CounterStrikeSharp server build, you may need to update CounterStrikeSharp or adjust the plugin project to use the matching API package version.
---
Installation
Download or build the compiled plugin files, then upload the compiled plugin folders to:
```txt
/game/csgo/addons/counterstrikesharp/plugins/
```
Expected layout:
```txt
/game/csgo/addons/counterstrikesharp/plugins/SMOKVip/
/game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/
```
Restart the server after uploading the plugins.
To confirm they loaded, run:
```txt
css_plugins list
```
You should see both plugins listed as loaded.
---
Building Locally
From the repository root:
```bash
dotnet restore SMOKVip/SMOKVip/SMOKVip.csproj
dotnet publish SMOKVip/SMOKVip/SMOKVip.csproj -c Release

dotnet restore SMOKCustomization/SMOKCustomization/SMOKCustomization.csproj
dotnet publish SMOKCustomization/SMOKCustomization/SMOKCustomization.csproj -c Release
```
The compiled output will be inside each project’s `bin/Release/` publish folder.
---
Building Online With GitHub Actions
This repository can be built without installing .NET locally by using GitHub Actions.
Steps:
Fork or upload this repository to GitHub.
Go to the Actions tab.
Select the build workflow.
Click Run workflow.
Wait for the workflow to finish.
Download the artifact from the completed workflow run.
Upload the compiled plugin folders to your server.
If the build fails, open the failed workflow step and look for the first red `error CS...` or `error NU...` line.
---
Generated Files
The plugins generate JSON files after first load/reload.
SMOKVip generated files
```txt
SMOKVip.json
vip_players.json
smok_vip_export.json
```
SMOKCustomization generated files
```txt
SMOKCustomization.json
player_preferences.json
```
These files are normally generated inside the plugin folders.
Example:
```txt
/game/csgo/addons/counterstrikesharp/plugins/SMOKVip/SMOKVip.json
/game/csgo/addons/counterstrikesharp/plugins/SMOKVip/vip_players.json
/game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/SMOKCustomization.json
```
Reload commands:
```txt
css_vip_reload
css_smokcustom_reload
```
---
Security Notice
Do not upload your live generated config/database files publicly.
These files may contain SteamIDs, VIP codes, player preferences, or server-specific settings:
```txt
SMOKVip.json
vip_players.json
smok_vip_export.json
SMOKCustomization.json
player_preferences.json
```
Use example config files for public repositories instead of your real live server files.
Recommended `.gitignore` entries:
```gitignore
bin/
obj/
.vs/
.idea/
*.user
*.suo

SMOKVip.json
vip_players.json
smok_vip_export.json
SMOKCustomization.json
player_preferences.json

*.zip
*.dll
*.pdb
```
---
VIP Commands
Player Commands
Command	Description
`!vip`	Shows general VIP information.
`!vipstatus`	Shows the player’s current VIP status.
`!vipbenefits`	Shows configured VIP benefits.
`!redeem <code>`	Redeems a VIP code for the player’s SteamID64.
Players can usually use either `!command` or `/command` in chat depending on the server’s CounterStrikeSharp command handling.
---
VIP Admin Commands
Run these from server console or as an authorized admin.
Add VIP manually
```txt
css_vip_add <steamid64> <days|lifetime> [tier] [note]
```
Examples:
```txt
css_vip_add 76561198000000000 30 vip
css_vip_add 76561198000000000 30 vipplus
css_vip_add 76561198000000000 lifetime vip
css_vip_add 76561198000000000 lifetime vipplus
css_vip_add 76561198000000000 30 vip Order #1042
```
Remove VIP
```txt
css_vip_remove <steamid64>
```
Example:
```txt
css_vip_remove 76561198000000000
```
Generate redeem codes
```txt
css_vip_code <code|auto> <days|lifetime> [tier] [uses]
```
Examples:
```txt
css_vip_code auto 30 vip 1
css_vip_code auto 30 vipplus 1
css_vip_code auto lifetime vip 1
css_vip_code auto lifetime vipplus 1
css_vip_code SMOKVIP30 30 vip 10
```
For paid VIP, single-use codes are recommended.
List VIP players
```txt
css_vip_list
```
Reload VIP config/data
```txt
css_vip_reload
```
---
VIP Tiers
The default plugin includes two tiers:
```txt
vip
vipplus
```
Regular VIP is intended for basic paid benefits.
VIP+ is intended for higher-tier benefits such as bonus HP, extra cosmetics, or more exclusive models/skins.
Example tier behavior:
```txt
VIP    = armor + helmet
VIP+   = armor + helmet + optional bonus HP
```
Exact values can be changed in `SMOKVip.json`.
---
VIP Permissions
The VIP plugin can grant CounterStrikeSharp permissions to active VIP members at runtime.
The important default permission for trails integration is:
```txt
@css/reservation
```
If your trails plugin checks for `@css/reservation`, active VIP players should be allowed to use trails.
The plugin may also grant a VIP-specific permission such as:
```txt
@css/vip
```
Runtime permissions are applied when:
A player connects
A player spawns
A player redeems a VIP code
An admin manually adds VIP
The VIP plugin reloads
Runtime permissions can be removed when:
VIP is removed
VIP expires
The plugin unloads/reloads
These permissions are not the same as permanently editing `admins.json`.
---
Trails Plugin Integration
If your trails plugin uses:
```txt
@css/reservation
```
Then active VIP players should automatically receive access after their VIP is active.
Recommended setup:
Generate or manually add VIP.
Have the player redeem the code with `!redeem <code>`.
Player reconnects or respawns if needed.
Player uses the trails plugin command.
If trails still do not work, confirm the trails plugin is definitely checking `@css/reservation` and not a different custom permission.
---
Customization Commands
Player model commands
Command	Description
`!models`	Lists available player models.
`!model <id>`	Selects a player model.
`!modelreset`	Resets the player model preference.
Examples:
```txt
!models
!model default_ct
!modelreset
```
Weapon skin commands
Command	Description
`!skins`	Lists available weapon skin presets.
`!skin <weapon> <skin>`	Selects a weapon skin preset.
`!skinreset`	Resets selected weapon skin preferences.
`!wp`	Refreshes/reapplies weapon paint settings.
Examples:
```txt
!skins
!skin ak47 redline
!wp
!skinreset
```
---
VIP-Locked Skin Changer
The skin changer can be locked behind a permission.
Default intended setup:
```json
"RequirePermissionForWeaponPaints": true,
"WeaponPaintPermission": "@css/reservation",
"WeaponPaintNoPermissionMessage": "Skin changer is a VIP perk. Buy VIP, then redeem your code with !redeem <code>."
```
With this setup:
Non-VIP players cannot use the skin changer.
Active VIP players receive `@css/reservation`.
VIP players can use `!skins`, `!skin`, `!skinreset`, and `!wp`.
---
Adding Custom Player Models
Custom CS2 player models usually need a `.vmdl` model path.
Example config entry:
```json
{
  "Id": "vip_model_1",
  "Name": "VIP Custom Model",
  "Team": "Both",
  "ModelPath": "characters/models/example/model_name/model_name.vmdl",
  "Permission": ""
}
```
If a Workshop model path ends in `.vmdl_c`, use the same path but replace `.vmdl_c` with `.vmdl` in the config.
Example:
```txt
characters/models/example/model_name/model_name.vmdl_c
```
becomes:
```txt
characters/models/example/model_name/model_name.vmdl
```
The server and clients still need access to the Workshop/custom assets for the model to appear correctly.
---
Selling VIP
A simple selling flow:
Create a VIP product on your store or Discord.
When someone buys VIP, generate a single-use code.
Send the code to the customer.
Customer joins the server.
Customer types `!redeem CODEHERE`.
VIP activates on their SteamID64 automatically.
Example 30-day VIP code:
```txt
css_vip_code auto 30 vip 1
```
Example 30-day VIP+ code:
```txt
css_vip_code auto 30 vipplus 1
```
Example lifetime VIP code:
```txt
css_vip_code auto lifetime vip 1
```
Suggested product structure:
```txt
VIP - 30 Days
VIP+ - 30 Days
Lifetime VIP
Lifetime VIP+
```
Cosmetic perks are recommended for surf/community servers. Avoid heavy pay-to-win perks.
---
Recommended VIP Benefits
Good VIP benefits for a CS2 surf/community server:
VIP player models
VIP weapon skin presets
Trails access
VIP Discord role
Chat tag, if using a chat tag plugin
Armor/helmet on spawn if appropriate for your server
Small VIP+ HP bonus if it does not hurt gameplay balance
Avoid extreme gameplay advantages that make the server feel unfair.
---
Troubleshooting
Plugin does not appear in `css_plugins list`
Check that the compiled `.dll` files are inside the correct folder:
```txt
/game/csgo/addons/counterstrikesharp/plugins/SMOKVip/
/game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/
```
Restart the server and check the console for load errors.
Config files did not generate
Run:
```txt
css_vip_reload
css_smokcustom_reload
```
Then check the plugin folders again.
If files still do not appear, check server file permissions.
VIP code generation works but `!redeem` does not
Make sure you are testing in-game as a real connected player. The redeem command needs a valid player SteamID64.
Trails do not work for VIP
Check that:
The VIP plugin is loaded.
The player has active VIP.
The player has respawned or reconnected.
The trails plugin checks `@css/reservation`.
The trails plugin is loaded after/alongside CounterStrikeSharp permissions correctly.
Skin changer says no permission
Check that:
The player has active VIP.
VIP grants `@css/reservation`.
`WeaponPaintPermission` is set to `@css/reservation`.
`RequirePermissionForWeaponPaints` is set to `true`.
Build fails on GitHub Actions
Open the failed workflow run and look for the first red error line.
Common examples:
```txt
error NU1202
error CS0103
error CS0266
```
Copy the first few red error lines and fix the matching source/API issue.
---
Public Repository Warning
Before making your repository public, remove any live server data and generated files.
Do not publish:
```txt
SMOKVip.json
vip_players.json
smok_vip_export.json
SMOKCustomization.json
player_preferences.json
```
These may contain private codes, player SteamIDs, and server-specific information.
---
License
Choose a license before publishing publicly.
MIT is recommended if you want other server owners to freely use and modify the plugin.
Add a `LICENSE` file to the repository before making it public.
---
Disclaimer
This project is provided as-is for CounterStrikeSharp CS2 community servers.
CS2, CounterStrikeSharp, and plugin APIs may change over time. If CounterStrikeSharp updates its API or CS2 changes internal entity/econ behavior, the plugins may need updates.
Server owners are responsible for testing plugins on their own servers before public use.
