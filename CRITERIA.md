# RestedBrews — Success Criteria (QC source of truth)

Each criterion is pass/fail. All must pass before the mod is considered done. Test in single player first, then on a dedicated server with at least one other player. For the first pass, use a Gale profile containing only BepInExPack, Jotunn and this mod.

## Brewing

| ID | Criterion | How to verify |
|---|---|---|
| B1 | All four brew bases appear in the Cauldron's craft list only when the Cauldron reaches the tier's level (1 / 3 / 5 / 7). | Build a Cauldron with devcommands. Spawn each tier's ingredients. Add upgrades one at a time: the Forest base is available at level 1, Frostmarsh at 3, Mistfield at 5 and Emberfrost at 7, and not before. |
| B2 | Each recipe uses exactly the configured ingredients. No fish is used anywhere. | Compare the craft panel with the README table. |
| B3 | A brew base placed in a Fermenter produces 4 of the matching potion. | Add a base, run `skiptime 2400`, then tap the Fermenter. |
| B4 | Potions and bases have their own names, descriptions and tinted look. Vanilla Tasty Mead looks unchanged. | Inventory tooltip; drop items on the ground; spawn `MeadTasty` next to them. |

## Effect

| ID | Criterion | How to verify |
|---|---|---|
| E1 | Drinking a potion out in the open (no shelter, no fire) gives Rested with the tier's duration: Forest 20 min (comfort 13, vanilla values), Frostmarsh 25, Mistfield 30, Emberfrost 35. | Check the Rested tooltip time. "Rested for N min" shows centre screen. |
| E2 | A tier with `Minutes = 0` takes its duration from the live Rested effect at `ComfortLevel`, not hard-coded minutes. | Log line `Live Rested: base …` and the per-tier "(comfort N)" lines. |
| E3 | Drinking while already Rested for at least the brew's duration is refused with "You are already well rested", and no potion is used up. | Drink the Emberfrost, then try a Forest. |
| E4 | A higher-tier brew raises a shorter Rested to its own duration. | Drink the Forest, then the Emberfrost. |
| E5 | Sleeping or sitting by a fire afterwards never shortens a brew's Rested. | Drink the Emberfrost, then sit by a campfire in a comfort-1 shelter. |
| E7 | With `PauseNearFire` on, a brew's Rested timer stops counting while the vanilla CampFire (warmth) status is active, and resumes when you walk away. Vanilla Rested from a bed or fire is unaffected. | Drink a brew, stand by a campfire for 1 min and watch the timer hold, then walk off. |
| E6 | No stray status icon from the helper effect. | Status bar after drinking. |

## Config & multiplayer

| ID | Criterion | How to verify |
|---|---|---|
| M1 | Each tier's Enabled, Ingredients, CauldronLevel, Minutes, ComfortLevel and PotionsPerBatch take effect. | Edit the config, restart, re-check B1/B3/E1. |
| M2 | A bad ingredient name disables that tier's recipe with one warning, and no exception is thrown. | Set `Ingredients = Nope:1`. |
| M3 | On a dedicated server, the server's config overrides the client's. | Change the server's CauldronLevel; the client's craft list follows. |
| M4 | A client without the mod, or with a different minor version, is refused by a server running it. | Jotunn's version-mismatch dialog. |

## Packaging

| ID | Criterion | How to verify |
|---|---|---|
| P1 | `dotnet build -c Release` gives 0 warnings and 0 errors. | Build output. |
| P2 | `package.ps1` produces `dist\RestedBrews-1.0.0.zip`, which imports into Gale. | Gale → Import local mod. |
| P3 | No private-identity strings anywhere in the folder. | Scan with the patterns from `~/.claude/CLAUDE.md`, read at run time. |
