# Knife Changer Update

This update adds VIP-only knife changing to `SMOKCustomization`.

## New player commands

```txt
!knives
!knife <id>
!knifereset
```

Example:

```txt
!knife butterfly
```

## Default knives

- `butterfly` -> `weapon_knife_butterfly`
- `karambit` -> `weapon_knife_karambit`
- `m9` -> `weapon_knife_m9_bayonet`
- `bayonet` -> `weapon_bayonet`
- `flip` -> `weapon_knife_flip`

## VIP permission

By default, knife changing requires:

```txt
@css/reservation
```

This matches the SMOKVip update that grants active VIP players `@css/reservation` at runtime.

## After updating

Rebuild with GitHub Actions, replace the compiled `SMOKCustomization` plugin folder, restart the server, and run:

```txt
css_smokcustom_reload
```

Existing `SMOKCustomization.json` files will receive the new knife changer settings after reload/save.
