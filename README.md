<h1 align="center">SMOK CS2 Plugins</h1>

<p align="center">
  CounterStrikeSharp plugins for CS2 servers that add VIP memberships, redeemable VIP codes,
  VIP permission syncing, player model customization, VIP-locked weapon skins, VIP-locked knife selection,
  and trails permission integration. Interested in an automated store to purchase VIP all from discord with no external api? Join the Discord now!
</p>

<p align="center">
  <a href="https://discord.gg/zB7NgPBzBA">
    <img alt="Join Discord" src="https://img.shields.io/badge/Discord-Join%20Support-5865F2?style=for-the-badge&logo=discord&logoColor=white">
  </a>
  <a href="LICENSE">
    <img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge">
  </a>
  <img alt="Built for CounterStrikeSharp" src="https://img.shields.io/badge/Built%20For-CounterStrikeSharp-blue?style=for-the-badge">
</p>

---

## Support

Need help installing, configuring, or troubleshooting the plugins?

Join the SMOKNetwork Discord for setup help, bug reports, feature requests, and CS2 server discussion.

<p>
  <a href="https://discord.gg/zB7NgPBzBA">
    <img alt="Join the SMOKNetwork Discord" src="https://img.shields.io/badge/Join%20the%20SMOKNetwork%20Discord-Support%20%26%20Updates-5865F2?style=for-the-badge&logo=discord&logoColor=white">
  </a>
</p>

---

## Included Plugins

### SMOKVip

A VIP membership plugin with manual VIP management, redeem codes, expiration tracking, and runtime permission syncing.

**Features**

- Timed VIP memberships
- Lifetime VIP memberships
- VIP and VIP+ tiers
- Redeemable one-time or multi-use VIP codes
- Manual VIP add/remove commands
- VIP status command
- VIP benefits command
- Spawn perks such as armor, helmet, and optional bonus HP
- Runtime CounterStrikeSharp permissions for active VIP players
- `@css/reservation` support for trails, reserved-slot style plugins, skins, and knives
- JSON-based storage
- Export file for other plugins to read active VIPs

### SMOKCustomization

A customization plugin for player models, weapon skin presets, and knife selection.

**Features**

- Weapon skin preset selection
- Weapon skin reset
- Weapon paint refresh command
- Knife selection
- Knife reset
- JSON-based player preferences
- Optional VIP/permission lock for the skin changer
- Optional VIP/permission lock for the knife changer
- Designed to work alongside SMOKVip

---

## Requirements

- A working CS2 dedicated server
- Metamod installed
- CounterStrikeSharp installed
- .NET-compatible CounterStrikeSharp runtime
- Server file access through FTP, SFTP, panel file manager, or direct filesystem access

---

## Installation

Upload the compiled plugin folders to:

```txt
game/csgo/addons/counterstrikesharp/plugins/
```

Your final plugin folders should look like this:

```txt
game/csgo/addons/counterstrikesharp/plugins/SMOKVip/
game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/
```

Restart the server, then check that both plugins loaded:

```txt
css_plugins list
```

You should see both plugins listed as loaded.

---

## Online Build With GitHub Actions

If you cannot compile locally, you can use GitHub Actions.

1. Create a GitHub repository.
2. Upload the plugin source files.
3. Open the **Actions** tab.
4. Run the build workflow.
5. Download the compiled artifact.
6. Upload the compiled plugin folders to your server.

The workflow should produce a downloadable artifact containing the compiled plugin files.

---

## VIP Commands

### Player Commands

| Command | Description |
| --- | --- |
| `!vip` | Shows general VIP information |
| `!vipstatus` | Shows your current VIP status |
| `!vipbenefits` | Shows VIP benefit information |
| `!redeem <code>` | Redeems a VIP code |

### Admin / Server Commands

| Command | Description |
| --- | --- |
| `css_vip_add <steamid64> <days\|lifetime> [tier] [note]` | Manually gives a player VIP |
| `css_vip_remove <steamid64>` | Removes VIP from a player |
| `css_vip_code <code\|auto> <days\|lifetime> [tier] [uses]` | Creates a VIP redeem code |
| `css_vip_list` | Lists active VIP players |
| `css_vip_reload` | Reloads VIP config and data |

---

## VIP Examples

Give a player 30 days of regular VIP:

```txt
css_vip_add 76561198000000000 30 vip
```

Give a player 30 days of VIP+:

```txt
css_vip_add 76561198000000000 30 vipplus
```

Give a player lifetime VIP:

```txt
css_vip_add 76561198000000000 lifetime vip
```

Generate a single-use 30-day VIP code:

```txt
css_vip_code auto 30 vip 1
```

Generate a single-use 30-day VIP+ code:

```txt
css_vip_code auto 30 vipplus 1
```

Generate a single-use lifetime VIP code:

```txt
css_vip_code auto lifetime vip 1
```

Players redeem codes in-game:

```txt
!redeem CODEHERE
```

---

## VIP Tiers

The default tiers are:

| Tier | Description |
| --- | --- |
| `vip` | Standard VIP tier |
| `vipplus` | Higher VIP tier with stronger perks |

Example tier behavior:

- VIP can receive armor and helmet on spawn.
- VIP+ can receive armor, helmet, and optional bonus health.
- Both tiers can be synced with CounterStrikeSharp permissions.
- Both tiers can be used to unlock VIP-only features in other plugins.

---

## Permission Integration

SMOKVip can grant CounterStrikeSharp permissions to active VIP players at runtime.

The default permission used by the SMOK plugin setup is:

```txt
@css/reservation
```

This permission can be used by other CounterStrikeSharp plugins to check whether a player should have VIP-only access.

Common uses:

- Trails plugin access
- Skin changer access
- Knife changer access
- Reserved-slot style access
- VIP-only cosmetic features

This lets VIP players access supported perks without manually adding every VIP to your CounterStrikeSharp admin files.

---

## Trails Plugin Integration

If your trails plugin requires:

```txt
@css/reservation
```

SMOKVip can automatically give active VIP players that permission.

After a player redeems VIP, the trails plugin should treat them as allowed.

If trails do not unlock immediately, try:

```txt
css_vip_reload
```

Then have the player reconnect or respawn.

---

## Skin Changer VIP Lock

SMOKCustomization can require a permission before allowing players to use weapon skin commands.

Default VIP permission:

```txt
@css/reservation
```

When enabled, non-VIP players are blocked from commands such as:

```txt
!skins
!skin
!skinreset
!wp
```

This lets you make the skin changer a VIP-only perk.

---

## Knife Changer VIP Lock

SMOKCustomization can also require a permission before allowing players to use knife changer commands.

Default VIP permission:

```txt
@css/reservation
```

When enabled, non-VIP players are blocked from commands such as:

```txt
!knives
!knife
!knifereset
```

This lets you make knife selection a VIP-only perk.

### Knife Commands

| Command | Description |
| --- | --- |
| `!knives` | Shows available knives |
| `!knife <id>` | Selects a knife |
| `!knifereset` | Resets your selected knife |

### Knife Examples

Select a butterfly knife:

```txt
!knife butterfly
```

Select a karambit:

```txt
!knife karambit
```

Select an M9 bayonet:

```txt
!knife m9
```

Reset your knife:

```txt
!knifereset
```

### Default Knife Options

The knife changer update includes common knife IDs such as:

| ID | Knife |
| --- | --- |
| `butterfly` | Butterfly Knife |
| `karambit` | Karambit |
| `m9` | M9 Bayonet |
| `bayonet` | Bayonet |
| `flip` | Flip Knife |

Knife changing is more sensitive than simple chat commands or model selection. Test it on your server before advertising it as a paid feature.

---

## Customization Commands

| Command | Description |
| --- | --- |
| `!skins` | Shows available weapon skin presets |
| `!skin <weapon> <preset>` | Selects a weapon skin preset |
| `!skinreset` | Resets your selected weapon skins |
| `!wp` | Refreshes weapon paints |
| `!knives` | Shows available knives |
| `!knife <id>` | Selects a knife |
| `!knifereset` | Resets your selected knife |
| `css_smokcustom_reload` | Reloads customization config |

---

## Selling VIP

The easiest workflow is redeem codes.

1. Customer buys VIP through your store or Discord.
2. You generate a single-use code.
3. You send the customer the code.
4. The customer joins the server.
5. The customer types `!redeem CODEHERE`.
6. VIP is automatically attached to their SteamID64.

Suggested products:

| Product | Example Price | Command |
| --- | --- | --- |
| VIP - 30 Days | `$4.99` | `css_vip_code auto 30 vip 1` |
| VIP+ - 30 Days | `$7.99` | `css_vip_code auto 30 vipplus 1` |
| Lifetime VIP | `$19.99-$29.99` | `css_vip_code auto lifetime vip 1` |
| Lifetime VIP+ | `$34.99-$49.99` | `css_vip_code auto lifetime vipplus 1` |

Suggested VIP perk list:

- VIP player models
- VIP weapon skins
- VIP knife selection
- Trails access
- Armor and helmet on spawn
- VIP status command
- Discord VIP role, if manually synced

---

## Config Files

The plugins generate JSON files after they are loaded.

Common generated files:

```txt
SMOKVip.json
vip_players.json
smok_vip_export.json
SMOKCustomization.json
player_preferences.json
```

These files are server-specific and may contain player data, VIP codes, SteamIDs, and private settings.

Do not publish your live generated config or database files.

---

## Security Notice

Before making your repository public, make sure you do not upload:

```txt
SMOKVip.json
vip_players.json
smok_vip_export.json
SMOKCustomization.json
player_preferences.json
bin/
obj/
*.dll
*.pdb
*.zip
```

Use example config files instead of real server files.

---

## Troubleshooting

### Plugins do not load

Run:

```txt
css_plugins list
```

Check your server console for plugin load errors.

### VIP code command works, but players cannot use VIP perks

Make sure the player redeemed the code in-game:

```txt
!redeem CODEHERE
```

Then check:

```txt
!vipstatus
```

### Trails do not unlock for VIP players

Confirm the trails plugin checks for:

```txt
@css/reservation
```

Then reload VIP:

```txt
css_vip_reload
```

### Skin changer is not VIP locked

Check the SMOKCustomization config and make sure the weapon paint permission lock is enabled.

### Knife changer is not VIP locked

Check the SMOKCustomization config and make sure the knife permission lock is enabled.

### Butterfly knife does not appear

Confirm the player has VIP, then try:

```txt
!vipstatus
!knives
!knife butterfly
```

If the command works but the knife does not change, reconnect or respawn and test again.

---

## License

This project is released under the MIT License.

See the `LICENSE` file for details.
