# Build fix notes

This version targets .NET 10 because the current CounterStrikeSharp.API package version 1.0.369 targets `net10.0`.

Changes made:

- `SMOKVip.csproj`: `<TargetFramework>net10.0</TargetFramework>`
- `SMOKCustomization.csproj`: `<TargetFramework>net10.0</TargetFramework>`
- GitHub Actions workflow: `dotnet-version: 10.0.x`
- GitHub Actions workflow keeps `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true`

If your CS2 server is running an older CounterStrikeSharp build that still expects .NET 8, use a matching older CounterStrikeSharp.API package version instead.
