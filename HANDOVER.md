# RestedBrews — Handover

**Status:** Built. It compiles with 0 warnings and is packaged. **Not yet tested in game.**
**Date:** 2026-10-17
**Owner:** Syloreon Khan
**Framework:** BepInEx 5 + HarmonyX + Jotunn 2.30.2, C# (net472), built against `assembly_valheim.dll` **Valheim 1.0.20**. Installed via **Gale** (Thunderstore package format).

## 1. Original request (verbatim)

> develop a valheim addon where you can make potions that will provide rested bonus and there are different potion types depending on the biome you are in and uses that particular biomes items. do not use fish. use items in the biomes that are easy to get and each biome potion requires at least 2 ingredients and it should be made on the typical cooking for that biome. so cookfire or spit for the first biome, black forest use the next cooking utensil and so forth all the way to the end biome. the rested bonus timer will be equal to what someone could build for comfort for that biome level

## 2. Decisions (owner, during planning)

- **Four potions instead of eight.** Close tiers are merged: Black Forest; Swamp + Mountains; Plains + Mistlands; Ashlands + Deep North.
- **No Meadows potion.** A roof and a campfire already give Rested there.
- **Fermenter wherever it is available.** It is available from the Black Forest (bronze), so every potion is a Cauldron brew base aged in the Fermenter.
- **Gating.** Each tier is gated by its biome's newest Cauldron upgrade. Deep North has no new cooking station; its Cauldron upgrade, the Smoker, gates the top tier.
- **Timer equals that tier's max-comfort Rested.** The owner first accepted a bonus on top, then removed it.
- **Bones instead of Blueberries** for the Black Forest brew.
- **Fixed times for the upper tiers (after the first build):** Frostmarsh 25 min, Mistfield 30, Emberfrost 35. These are the `Minutes` config defaults. Forest keeps `Minutes = 0`, which means it follows the comfort formula at comfort 13 (20 min with vanilla values).

## 3. As built

| Tier | Prefabs | Cauldron level | Ingredients (default) | Duration |
|---|---|---|---|---|
| Forest Draught | `RestedBrew_forest[_Base]` | 1 | BoneFragments 5, Thistle 3 | comfort 13 (20 min) |
| Frostmarsh Draught | `RestedBrew_frostmarsh[_Base]` | 3 | Bloodbag 2, Turnip 3, Onion 3, FreezeGland 1 | 25 min |
| Mistfield Draught | `RestedBrew_mistfield[_Base]` | 5 | Cloudberry 5, Barley 3, RoyalJelly 2, MushroomJotunPuffs 2 | 30 min |
| Emberfrost Draught | `RestedBrew_emberfrost[_Base]` | 7 | Vineberry 3, Fiddleheadfern 2, MushroomSmokePuff 2, Lingonberry 5 | 35 min |

- **`src/BrewTiers.cs`:** the tier table and config binding. All entries are admin-only and server-synced by Jotunn.
- **`src/BrewRegistrar.cs`:** registration and live config.
  - On `PrefabManager.OnVanillaPrefabsAvailable` it clones `MeadBaseTasty` and `MeadTasty` for each tier, gives the clones copied, tinted materials, and renders an icon (falling back to the vanilla icon).
  - It adds the Cauldron recipe, a Fermenter conversion, the status effect and English text.
  - `ApplyConfig()` writes config into the live recipes. It runs on `ItemManager.OnItemsRegistered` and on `SynchronizationManager.OnConfigurationSynchronized`, rebuilding requirements and disabling any tier that has an unknown ingredient.
- **`src/SE_RestedBrew.cs`:** a 1 s, icon-less effect.
  - On `Setup` it adds the vanilla `Rested` effect and raises its timer to `BrewTier.DurationSeconds()`. That is the tier's `Minutes` setting if it is above 0. Otherwise it is `m_baseTTL + (comfort-1) * m_TTLPerComfortLevel`, read from the live `SE_Rested`. It never shortens an existing timer.
  - It sets the protected `StatusEffect.m_time` through `AccessTools.FieldRefAccess`.
- **`src/Patches.cs`:**
  - A `Player.CanConsumeItem` postfix refuses a brew in two cases. The first is a brew of the same or higher duration that is still behind the current Rested instance; that is tracked in `SE_RestedBrew`. The second is Rested already having at least that much time left.
  - A `SE_Rested.UpdateStatusEffect` postfix undoes each frame's countdown while a brew's Rested is active and the player has the vanilla `CampFire` status. This is config `PauseNearFire`, admin-only, on by default and added at the owner's request.

**Tested in game by the owner (2026-10-17):**
- The Forest and Frostmarsh effects work.
- The same-tier repeat is blocked after a fix.
- A higher tier is allowed.
- The fire pause works (E7).
- **Not tested; the owner chose to assume they pass:** Cauldron level gating (B1), the Mistfield and Emberfrost recipes, Fermenter yield (B3), visuals (B4), and the multiplayer checks (M3, M4).

## 4. Verified against Valheim 1.0.20 / Jotunn 2.30.2 (decompiled)

| API | Finding |
|---|---|
| `SE_Rested.UpdateTTL` | ttl = `m_baseTTL + (comfort-1)*m_TTLPerComfortLevel`, and it only extends. The code default base is 300 s; wikis say the prefab sets 480 s. The startup log prints the live value. |
| `CraftingStation.GetLevel` | 1 + attached extension count. |
| `Player.CanConsumeItem` / `ConsumeItem` | Applies `m_consumeStatusEffect`, and calls `EatFood` only if `m_food > 0`. |
| `SEMan.GetHUDStatusEffects` | Skips effects with no icon, so the helper effect stays hidden. |
| `Fermenter.ItemConversion.m_producedItems` | Default 4. |
| Jotunn `CustomItem`, `ItemConfig`, `FermenterConversionConfig`, `CustomStatusEffect`, `RenderManager.Render`, `NetworkCompatibility` | All present in 2.30.2. |
| Prefab names | All ingredient prefabs, `fermenter`, `piece_cauldron`, `MeadBaseTasty` and `MeadTasty` appear in `StreamingAssets\SoftRef\manifest`. |

## 5. Assumptions / open questions

- **Max comfort per tier** (13 / 17 / 20 / 22) is from the wiki's per-biome table, re-totalled. It only sets the duration when that tier's `Minutes` is 0; by default that is Forest only.
- **Cauldron level gating counts upgrades; it doesn't check which ones.** Every upgrade needs its biome's materials, so the count tracks progression.
- **A `PotionsPerBatch` change from a server sync** only reaches fermenters spawned after the sync.
- **Whether Rested survives logging out and back in** is untested (CRITERIA E-series).
- **The icon render** runs inside `OnVanillaPrefabsAvailable`. If it fails, the vanilla mead icon is used and a warning is logged.

## 6. Build, deploy, package

- **Build:** `dotnet build src\RestedBrews.csproj -c Release`. Set `VALHEIM_INSTALL` and `JOTUNN_DLL` to override the default paths.
- **Deploy:** set `RESTEDBREWS_DEPLOY` to a Gale profile's `BepInEx\plugins\Syloreon-RestedBrews` folder, and the build copies the DLL there.
- **Package:** `.\package.ps1` builds `dist\RestedBrews-<version>.zip`. Import it into Gale with "Import local mod".

## 7. Next steps

1. Install the package in a clean Gale profile and work through `CRITERIA.md`.
2. Check the startup log for the live Rested values and the per-tier minutes.
3. `git init` and the first commit, once the owner agrees.
