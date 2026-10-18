# Rested Brews

A Valheim mod (BepInEx + Jotunn). Brew biome potions at the Cauldron, ferment them, and
drink them to become Rested anywhere. Player-facing docs, including the potion table and
config, are in [thunderstore/README.md](thunderstore/README.md).

## Building

Requirements: .NET SDK, Valheim, and Jotunn 2.30.2.

```
dotnet build src\RestedBrews.csproj -c Release
```

- `VALHEIM_INSTALL` overrides the game path (default `F:\SteamLibrary\steamapps\common\Valheim`).
- `JOTUNN_DLL` overrides the path to `Jotunn.dll` (default: a Gale "Valheim 1.0" profile).
- `RESTEDBREWS_DEPLOY` copies the built DLL to a plugins folder after each build.

`package.ps1` builds the plugin and writes the Thunderstore zip to `dist\`.

## Development

Developed with AI assistance (Claude) by Syloreon Khan.
