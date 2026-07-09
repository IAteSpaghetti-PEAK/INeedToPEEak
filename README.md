# INeedToPEEak 🚽

Bathroom needs for **PEAK** (Landfall Games / Aggro Crab). Fully multiplayer-synced —
**every player in the lobby must have the mod installed.**

## Features

### 💩 Poo
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
- Pooping leaves you **5% Dirty** (grey segment). It never goes away on its own.
- **Toilet Paper** (5 uses, each removes 5% Dirty) is the only cure.
- One random player starts the run with a roll in their first slot.
- Rolls can otherwise only be found in **Big Luggage (33%)** and
  **Explorer's Luggage (50%)**.

### 💦 Pee
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
keybinds, gain ratios, thresholds, timings, sizes, luggage chances, item IDs.

## Building
```
dotnet build INeedToPEEak/INeedToPEEak.csproj -c Release
```
Set `GameDir` in the csproj (or pass `-p:GameDir=...`) to your PEAK install.
The built DLL is auto-copied into `BepInEx/plugins/INeedToPEEak`.

## How it works (for the curious)
- Poo/Pee/Dirty are extra `CharacterAfflictions.STATUSTYPE` slots (12/13/14) —
  the status arrays are enlarged via Harmony, so the values ride the game's own
  `SyncStatusesRPC` multiplayer sync and reduce max stamina exactly like vanilla
  afflictions. The stamina-bar UI segments are cloned from a vanilla `BarAffliction`.
- Items are code-built prefabs registered in the game's `ItemDatabase` and served
  by a wrapping Photon prefab pool, so `PhotonNetwork.Instantiate` resolves them
  on every client. Poo spawns as a master-client room object (it outlives the
  pooper); puddle size streams over its `PhotonView`.
- Slipping reuses the exact ragdoll impulses of the vanilla banana peel / jellyfish.
