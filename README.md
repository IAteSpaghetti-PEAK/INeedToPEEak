# INeedToPEEak 🚽
[![Thunderstore Version](https://img.shields.io/thunderstore/v/IAteSpaghetti/INeedToPEEak?style=for-the-badge)](https://thunderstore.io/c/peak/p/IAteSpaghetti/INeedToPEEak/)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/IAteSpaghetti/INeedToPEEak?style=for-the-badge)](https://thunderstore.io/c/peak/p/IAteSpaghetti/INeedToPEEak/)
[![Thunderstore Likes](https://img.shields.io/thunderstore/likes/IAteSpaghetti/INeedToPEEak?style=for-the-badge)](https://thunderstore.io/c/peak/p/IAteSpaghetti/INeedToPEEak/)

Bathroom needs for **PEAK** (Landfall Games / Aggro Crab). Fully multiplayer-synced —
**every player in the lobby must have the mod installed.**

## Features

### 💩 Poo

![Building up and releasing Poo](https://raw.githubusercontent.com/IAteSpaghetti-PEAK/INeedToPEEak/main/assets/poo.png)

- Eating food adds **Poo** (brown segment with a poo icon) to your stamina bar —
  half of the hunger the food cured.
- At **1/3 bar or more** of Poo, **hold K** to poo. You're forced into a crouch and
  can still waddle around — but incredibly slowly (no sprinting). It takes longer
  the more you need: ~3s at 33%, 8.5s at 85% (`time = poo × 10s`). Jumping,
  climbing, or letting go of K cancels it.
- Pooping drops a real **Poo item** under you, sized by how much you had
  (a default 33% poo is about half a Bing Bong).

The poo item:
- **Slips** anyone who runs over it, like a banana peel.
- Makes you **Stink** (its own olive-green status, +10% per poo) while it's in your
  hands or any of your three main slots — gone as soon as you get rid of it.
- Can be **eaten**. Eating takes half as long as the pooing did, and poisons the
  eater by half of the poo's original amount. Bon appétit.

### 🧻 Dirty & Toilet Paper

![Getting Dirty and wiping with Toilet Paper](https://raw.githubusercontent.com/IAteSpaghetti-PEAK/INeedToPEEak/main/assets/toilet-paper.png)

- Pooping leaves you **5% Dirty** (grey segment). It never goes away on its own.
- **Toilet Paper** (5 uses, each removes 5% Dirty) is the only cure.
- One random player starts the run with a roll in their first slot.
- Rolls can otherwise only be found in **Big Luggage (33%)** and
  **Explorer's Luggage (50%)**.

### 💦 Pee

![Peeing and the slippery puddle it leaves](https://raw.githubusercontent.com/IAteSpaghetti-PEAK/INeedToPEEak/main/assets/pee.png)

- Drinking adds **Pee** (yellow segment). Drinks that cure hunger add half of that;
  other drinks add half of whatever they cure (capped), or a small fixed amount.
- At 1/3 bar or more, stand still and **hold L** to pee — no crouching, a stream
  arcs from your character, and the pee **drains gradually**; release L to stop
  mid-stream and keep the rest.
- Peeing forms a growing **puddle** (starts poo-sized, maxes out at jellyfish size)
  that slips anyone who runs through it — no poison, it's just pee.

## Install
1. Install [BepInEx 5 (BepInExPack PEAK)](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/).
2. Drop `INeedToPEEak.dll` into `PEAK/BepInEx/plugins/`.
3. Every player in the lobby needs the mod (statuses, items, and RPCs are custom).

## Configuration
Everything is tunable in `BepInEx/config/com.exoflex.ineedtopeeak.cfg`:
keybinds, difficulty, gain ratios, thresholds, timings, sizes, luggage chances, item IDs.

**Difficulty** (`Gentle` / `Normal` / `Rough` / `Brutal`) scales every effect the mod
applies to you — poo and pee build-up, dirtiness, stink, and poo poisoning. It's a
personal setting: everyone in a lobby can run a different one.

With [PEAKLib.ModConfig](https://thunderstore.io/c/peak/p/PEAKModding/PEAKLib_ModConfig/)
installed you get this in-game — the poo/pee keys become rebindable under **Mod Controls**,
and **Mod Settings** shows:

| Setting | What it does |
|---|---|
| Difficulty | Scales every effect (see above) |
| Enable Dirty | Turn off to skip the dirtiness/toilet-paper loop entirely |
| Enable Stink | Turn off to carry poos with no penalty |
| Chance Explorer's / Big Luggage | How often toilet paper turns up |
| Uses Per Roll | Wipes per roll — raise it for bigger groups |
| Give Starting Roll | Whether someone starts the run with one |
| Replaces A Luggage Item | Off by default. On, toilet paper takes an item's place instead of being added — this destroys that item, including items from other mods |

The remaining tuning values are deliberately hidden from that menu (they stay editable in
the config file). ModConfig is entirely optional — the mod neither requires nor references it.

## Building
```
dotnet build INeedToPEEak/INeedToPEEak.csproj -c Release
```
Set `GameDir` in the csproj (or pass `-p:GameDir=...`) to your PEAK install.
The built DLL is auto-copied into `BepInEx/plugins/INeedToPEEak`.

## How it works (for the curious)
- Poo/Pee/Dirty/Stink are extra `CharacterAfflictions.STATUSTYPE` slots claimed right
  after the vanilla ones (read from the enum at runtime, so game updates that add new
  afflictions shift the mod's slots instead of colliding with them) —
  the status arrays are enlarged via Harmony, so the values ride the game's own
  `SyncStatusesRPC` multiplayer sync and reduce max stamina exactly like vanilla
  afflictions. The stamina-bar UI segments are cloned from a vanilla `BarAffliction`.
- Items are code-built prefabs registered in the game's `ItemDatabase` and served
  by a wrapping Photon prefab pool, so `PhotonNetwork.Instantiate` resolves them
  on every client. Poo spawns as a master-client room object (it outlives the
  pooper); puddle size streams over its `PhotonView`.
- Slipping reuses the exact ragdoll impulses of the vanilla banana peel / jellyfish.
