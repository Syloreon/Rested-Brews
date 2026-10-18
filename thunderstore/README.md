# Rested Brews

Brew potions that make you **Rested** anywhere — out on a raft, in a cave, at a
forward camp — for as long as the best comfort you could build in that part of the
world would give you.

Each brew is made from that biome's easy-to-gather ingredients (no fish). You mix a
brew base at the **Cauldron**, then age it in the **Fermenter** to get the potions,
just like vanilla meads. The Cauldron upgrade level each brew needs comes from its
biome, so you can't make a late-game brew early.

| Brew | Biomes | Cauldron level | Ingredients | Rested for |
|---|---|---|---|---|
| Forest Draught | Black Forest | 1 (Cauldron) | 5 Bone Fragments, 3 Thistle | 20 min (comfort 13) |
| Frostmarsh Draught | Swamp + Mountains | 3 (+ Butcher's Table) | 2 Bloodbag, 3 Turnip, 3 Onion, 1 Freeze Gland | 25 min |
| Mistfield Draught | Plains + Mistlands | 5 (+ Mortar and Pestle) | 5 Cloudberries, 3 Barley, 2 Royal Jelly, 2 Jotun Puffs | 30 min |
| Emberfrost Draught | Ashlands + Deep North | 7 (+ Smoker) | 3 Vineberry Cluster, 2 Fiddlehead, 2 Smoke Puff, 5 Lingonberries | 35 min |

- One fermented base gives 4 potions.
- Forest Draught follows the game's own Rested formula at comfort 13, the best you
  can build in the Black Forest. That is 20 min with vanilla values (8 min at
  comfort 1, +1 min per level). The other three brews last a fixed time.
- A brew's Rested timer pauses while you are within a fire's warmth (config
  `PauseNearFire`, on by default).
- A brew never shortens Rested. If you are already Rested for longer than the brew
  would give, it won't let you drink it, so it isn't wasted.
- Cauldron level is 1 + the number of Cauldron upgrades placed next to it.

## Config

`BepInEx/config/com.syloreon.restedbrews.cfg` has a section for each brew:

- Enabled
- Ingredients, written as `Prefab:amount,Prefab:amount`
- CauldronLevel
- Minutes, a fixed duration (0 means use ComfortLevel instead)
- ComfortLevel, which sets the duration through the Rested formula when Minutes is 0
- PotionsPerBatch

All settings are admin-only and sync from the server.

## Multiplayer

The server and every player need the mod. It requires Jotunn.

## Development

Developed with AI assistance (Claude). This mod is tagged **AI Generated** on Thunderstore.
Source: https://github.com/Syloreon/Rested-Brews

## Credits

Syloreon Khan
