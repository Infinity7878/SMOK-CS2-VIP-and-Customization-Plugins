# Easy VIP Selling Setup for SMOKNetwork

## Best simple setup

Use a store page or Discord ticket system, then grant VIP with a console command.

Recommended products:

```txt
VIP - 30 Days - $4.99
VIP+ - 30 Days - $7.99
Lifetime VIP - $24.99
```

## Product description template

```txt
SMOKNetwork VIP - 30 Days

Includes:
- VIP player models
- VIP skin presets
- Armor + helmet on spawn
- VIP status in-game
- Discord VIP role if linked manually

After purchase, you will receive a redeem code. Join the server and type:
!redeem YOURCODE

Digital item. Redeemed codes are not refundable. Chargebacks or payment disputes may result in VIP removal and store ban.
```

## Delivery message template

```txt
Thanks for supporting SMOKNetwork.

Your VIP redeem code is:
CODEHERE

How to redeem:
1. Join the CS2 server.
2. Type !redeem CODEHERE in chat.
3. Type !vip to confirm your status.

If you have issues, open a Discord ticket.
```

## Manual command method

If you know the buyer's SteamID64:

```txt
css_vip_add 76561198000000000 30 vip Order #123
```

For lifetime:

```txt
css_vip_add 76561198000000000 lifetime vipplus Order #124
```

## Redeem code method

Create a one-use 30-day VIP code:

```txt
css_vip_code auto 30 vip 1
```

Create a custom code with 10 uses:

```txt
css_vip_code SMOKLAUNCH 30 vip 10
```

## Terms of service text

```txt
All purchases are digital perks for SMOKNetwork servers. VIP perks are not official Valve, Steam, or Counter-Strike items. Redeemed codes are non-refundable. VIP may be removed for chargebacks, payment disputes, exploiting, cheating, ban evasion, staff impersonation, or severe server rule violations. Server perks may change if CS2 or plugin updates break a feature.
```

## Recommended first launch offer

Run a launch promo instead of making VIP expensive immediately:

```txt
VIP - 30 Days: $3.99 launch price
VIP+ - 30 Days: $5.99 launch price
Lifetime VIP: $19.99 launch price
```

After you have active players, increase monthly prices slightly.
