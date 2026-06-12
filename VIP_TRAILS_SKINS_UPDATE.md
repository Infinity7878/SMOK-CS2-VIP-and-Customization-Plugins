# SMOK VIP Trails + Skin Changer Update

This build wires SMOKVip into CounterStrikeSharp permissions and makes SMOKCustomization's weapon skin changer require VIP/reservation access by default.

## What changed

### SMOKVip

Active VIP players now receive temporary CounterStrikeSharp permissions while they are connected:

```json
"GrantCounterStrikeSharpPermissions": true,
"RemoveGrantedPermissionsWhenVipInactive": true,
"PermissionsGrantedToActiveVip": [
  "@css/reservation",
  "@css/vip"
]
```

The important flag for your trails plugin is:

```txt
@css/reservation
```

This is granted when:

- a player connects and has active VIP
- a player spawns and has active VIP
- a player redeems a VIP code with `!redeem`
- an admin manually gives VIP with `css_vip_add`
- `css_vip_reload` is run

The permission is removed when:

- `css_vip_remove <steamid64>` is run for an online player
- VIP expires while the player is online and the sync timer runs
- the VIP plugin unloads/reloads

The plugin tracks only the permissions it added at runtime, so it should not remove permanent permissions that came from `admins.json` or `admin_groups.json`.

### SMOKCustomization

Weapon skins now require this config by default:

```json
"RequirePermissionForWeaponPaints": true,
"WeaponPaintPermission": "@css/reservation",
"WeaponPaintNoPermissionMessage": "Skin changer is a VIP perk. Buy VIP, then redeem your code with !redeem <code>."
```

This affects:

- `!skins`
- `!skin`
- `!skinreset`
- `!wp`
- automatic weapon paint application

Player model commands are still public unless you ask for them to be VIP-only too.

## Install steps

1. Upload the new source to GitHub.
2. Run the existing GitHub Actions build workflow.
3. Download the compiled artifact.
4. Replace these plugin folders on the server:

```txt
game/csgo/addons/counterstrikesharp/plugins/SMOKVip/
game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/
```

5. Restart the server.
6. Run:

```txt
css_plugins list
css_vip_reload
css_smokcustom_reload
```

## Test flow

Generate a test code:

```txt
css_vip_code auto 30 vip 1
```

Join the server and redeem it:

```txt
!redeem CODEHERE
```

Then test:

```txt
!vipstatus
!skins
!skin ak47 redline
!wp
```

Then test your trails command. Since your trails plugin checks `@css/reservation`, the VIP player should have access after redeeming VIP.
