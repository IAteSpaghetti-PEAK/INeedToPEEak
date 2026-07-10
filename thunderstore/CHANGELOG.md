# Changelog

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
