# Changelog

## 0.2.0

Compatibility update for **PEAK 2.0**. Earlier versions are broken on 2.0 — please update.

- Poo, Pee, Dirty and Stink now claim their stamina-bar slots dynamically. PEAK 2.0 added
  three new afflictions (Arrow, Petrify, FlyTrap) that took the exact slots the mod used,
  which scrambled them together; the mod now shifts around new vanilla statuses automatically.
- Fixed slipping on poos and pee puddles (the game's fall call changed and would have errored).
- Toilet paper can once again turn up in big luggage in the new biomes (Gloom, Citadel).
- You can no longer start a bathroom break while petrifying.

Fixes:

- **Toilet paper no longer deletes other items.** It used to take the place of one item the
  luggage rolled — which destroyed that item, and with food mods installed it was often
  one of theirs. Toilet paper is now simply added to the luggage and nothing is removed.
  (The old behaviour is still available as *Replaces A Luggage Item* if you'd rather keep
  luggage item counts unchanged.)

New in this version:

- **Difficulty setting** — Gentle, Normal, Rough or Brutal, scaling how hard everything hits
  you: poo and pee build-up, dirtiness, stink from carrying a poo, and poo poisoning.
  It's per-player, so you and your friends can each pick your own.
- **Dirty and Stink can each be turned off** — handy for big groups who don't want to ration
  toilet paper. Turning Dirty off also clears any you already have.
- **Toilet paper spawning is configurable** — per-luggage-type chances, wipes per roll, and
  whether someone starts the run with one.
- **Proper key rebinding** — the poo and pee keys are now real keybinds: click the setting,
  press a key. They also no longer fire while a menu or the item wheel is open.
- **In-game settings** — with [PEAKLib.ModConfig](https://thunderstore.io/c/peak/p/PEAKModding/PEAKLib_ModConfig/)
  installed, the poo/pee keys are rebindable under Mod Controls and the difficulty sits in
  Mod Settings. The other tuning values are hidden there to keep it tidy — they're all still
  in the config file. ModConfig is optional; without it nothing changes.

## 0.1.5

Optimization fixes:

- One central progress-bar UI instead of a GUI pass per character every frame.
- Pee puddles stop touching physics once they've finished growing.
- Removed per-frame component lookups in the movement and carry checks.
- Safety caps on lingering poos (40) and pee puddles (20) — oldest ground ones despawn past the cap; configurable, 0 disables.

## 0.1.4

- **Fixed:** toilet paper spawned as an extra item in luggage; it now *replaces* one of the luggage's items instead.
- Toilet paper chances adjusted: Explorer's Luggage 25% (was 50%), Big Luggage 3% (was 33%).

## 0.1.3

- Skeletons (revived with the Book of Bones) no longer build up Poo or Pee, just like they don't get hungry.
- Cure-All now removes Poo and Pee along with your other afflictions.
- Pandora's Lunchbox now clears Poo and Pee and can randomly re-roll them like other statuses.

## 0.1.2

- **Fixed:** a pooed item reverted to the default size when picked up and dropped — it now keeps its own size.
- **Fixed:** the starting toilet paper roll was only handed out on the first run of a session; it now appears every run.
- **Fixed:** pee puddles spawned near your feet instead of where the stream actually lands.
- Added screenshots to the mod page.

## 0.1.1

- New hand-drawn icon and updated project links. No gameplay changes.

## 0.1.0

First release.

- **Poo status**: eating food adds Poo (half the hunger cured) to your stamina bar.
- **Pooping**: at 1/3+ Poo, hold **K** — you crouch and waddle very slowly while it happens (3s at 33%, up to 10s at 100%), then leave a real poo behind and become 5% **Dirty**.
- **Poo item**: sized by how badly you needed to go. Slips players like a banana peel, gives the carrier **Stink** (10% per poo, hands or main slots), and can be eaten (poisons you, obviously).
- **Dirty & Toilet Paper**: Dirty never fades — wipe with Toilet Paper (5 uses). One random player starts with a roll; more can be found in Big Luggage (33%) and Explorer's Luggage (50%).
- **Pee status**: drinks fill your bladder. Hold **L** to relieve it gradually — release to stop mid-stream.
- **Pee puddles**: grow while you go (up to jellyfish size) and stay behind as slippery hazards.
- Everything is Photon-synced; every player in the lobby needs the mod. Keybinds, rates, sizes and chances configurable in `BepInEx/config/com.exoflex.ineedtopeeak.cfg`.
