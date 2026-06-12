# SMOK VIP - CounterStrikeSharp VIP Plugin

A CS2 CounterStrikeSharp VIP plugin for SMOKNetwork.

## What it does

- Adds `!vip`, `!vipstatus`, `!vipbenefits`, and `!redeem <code>` player commands.
- Adds admin commands to grant/remove/list VIPs and generate redeem codes.
- Stores VIPs in `vip_players.json` by SteamID64.
- Supports expiring VIPs, lifetime VIPs, and multiple tiers.
- Applies configurable spawn perks such as armor, helmet, and health.
- Writes `smok_vip_export.json` so other SMOK plugins can read active VIP status.

## Player commands

```txt
!vip
!vipstatus
!vipbenefits
!redeem <code>
```

## Admin/server commands

Run from server console, RCON, or in-game with the configured admin permission.

```txt
css_vip_add <steamid64> <days|lifetime> [tier] [note]
css_vip_remove <steamid64>
css_vip_code <code|auto> <days|lifetime> [tier] [uses]
css_vip_list
css_vip_reload
```

Examples:

```txt
css_vip_add 76561198000000000 30 vip PayPal order 1001
css_vip_add 76561198000000000 lifetime vipplus Owner gifted
css_vip_code auto 30 vip 1
css_vip_code SMOK30 30 vip 10
css_vip_remove 76561198000000000
```

## Install

1. Build the plugin:

```bash
cd SMOKVip/SMOKVip
dotnet restore
dotnet publish -c Release
```

2. Upload the published output to:

```txt
game/csgo/addons/counterstrikesharp/plugins/SMOKVip/
```

3. Restart the server or reload CounterStrikeSharp.

4. Check it loaded:

```txt
css_plugins list
```

5. Generate a test code:

```txt
css_vip_code auto 30 vip 1
```

6. In game, redeem it:

```txt
!redeem CODEHERE
```

## Config

On first load, the plugin creates:

```txt
game/csgo/addons/counterstrikesharp/plugins/SMOKVip/SMOKVip.json
game/csgo/addons/counterstrikesharp/plugins/SMOKVip/vip_players.json
game/csgo/addons/counterstrikesharp/plugins/SMOKVip/smok_vip_export.json
```

Edit `SMOKVip.json`, then run:

```txt
css_vip_reload
```

## Recommended tiers

Start simple:

- VIP: $4.99/month
- VIP+: $7.99/month
- Lifetime VIP: $19.99-$29.99 one-time

Suggested VIP benefits:

- VIP models
- VIP skin presets
- armor + helmet on spawn
- Discord role
- queue/reserved slot later if you add a safe reservation plugin

## Selling flow

### Easiest manual flow

1. Create a product on your store/Discord: `VIP - 30 Days`.
2. Buyer pays.
3. Buyer sends SteamID64 or opens a ticket.
4. You run:

```txt
css_vip_add THEIR_STEAMID64 30 vip Order #123
```

### Better redeem-code flow

1. Generate codes in console:

```txt
css_vip_code auto 30 vip 1
```

2. Put one code in the customer's order/delivery message.
3. Customer joins the server and types:

```txt
!redeem CODE
```

This is easier because the plugin automatically attaches VIP to the player's SteamID64.

## Important notes

- This plugin does not process payments itself.
- Do not sell anything as an official Valve item or official CS2 skin.
- Keep VIP perks mostly cosmetic/QOL so players do not feel the server is pay-to-win.
- Keep clear terms: no refunds for redeemed digital codes, chargebacks remove VIP, abuse can revoke VIP.
