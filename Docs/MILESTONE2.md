# Milestone 2 — Photon Shared Mode foundation

## Status — verified 2026-10-14

The first multiplayer foundation is implemented on `codex/rush-arena-m2`. The offline M1 scene remains available. Physical Android testing is still deferred by user choice, and no claim is made about human gameplay feel or sustained performance.

Installed official dependencies:

- Photon Fusion **2.1.3 Stable Build 2390**
- Photon Simple KCC **2.1.1 build 1272**
- Unity Physics 2D module required by the imported Fusion SDK

The downloaded package checksums are recorded locally as:

- Fusion: `375e21bc771ce5f7cea942b5f804e7c28c8f4d858f7e9aab96725214564bc52e`
- Simple KCC: `3396fd71d9e2dc55bbbd431a0d6a5117dc456d2b193dcd024106ca360065525e`

The Photon application identifier lives only in the ignored local `PhotonAppSettings.asset`. Never commit or print it. The configured application uses Photon’s free development tier; no purchase was made.

## Implemented

- Separate `ArenaShared` scene and multiplayer assembly, preserving `ArenaM1`.
- Dual weapon systems: Tactical Assault Rifle (hitscan, high-capacity, crisp muzzle flash) and Plasma Cannon (heavy energy bolts, projectile tracer), switchable via `Q`, `1`, `2` or HUD mobile buttons.
- Procedural weapon sway, recoil kickback, dynamic crosshair bloom, hit markers with audio chirp (2.1 kHz), and zero-dependency procedural audio synthesis (shots, hits, pickups, jump pads).
- Stylized 3D combatant avatars replacing the capsule placeholder (torso armor, glowing visor helmet, thruster backpack, articulated animated limbs, pitch-aim replication, and overhead nameplates/health bars).
- Multi-level tactical sci-fi arena (54m×54m) featuring a central raised command platform (3.5m high), access ramps, catwalk bridges, jump pad launchers, and tactical cover pillars.
- Interactive Jump Pads (14 m/s vertical + 8 m/s forward impulse with neon ring and audio whoosh).
- Three floating tactical power-ups on glowing pedestals with respawn countdowns: Rapid Fire (2x fire rate), Shield Boost (+50 shield), and Super Speed (+40% speed).
- Synchronized 120-second match loop with countdown timer, kill feed, elimination sequence with 3s respawn shield, and end-of-match victory banner.
- Modern cross-platform Combat HUD supporting keyboard/mouse and multi-touch mobile input.
- Shared Mode room flow with ten-player capacity, leave action, and command-line smoke test integration.
- Baked concrete URP Lit material assets preventing WebGL AOT shader stripping (resolving the magenta rifle defect).

## Verification

- **Pure logic**: `dotnet run --project Tools/CoreChecks/CoreChecks.csproj -c Release` -> **15/15 passed**.
- **EditMode tests**: **16/16 passed** (including synthetic multi-touch and input buffer tests).
- **PlayMode tests**: **5/5 passed** (ground/wall/ceiling collisions, steps, landing).
- **Windows development build**: **Succeeded** (`Builds/WindowsShared/RushArenaShared.exe`).
- **WebGL development build**: **Succeeded** (`Builds/WebGLShared/index.html`) with zero shader compilation errors.
- **Two-client Photon Cloud Shared Mode smoke test**: **Passed** on 2026-10-14. Both Client 1 and Client 2 connected to Photon Cloud, joined the same session, and verified `players=2;avatars=2`.

## How to Test

1. **Windows Standalone**:
   - Run `Builds/WindowsShared/RushArenaShared.exe`.
   - Click **Join or create named room** in the developer menu.
   - Use `WASD` to move, `Shift` to sprint, `Space` to jump, `Left Mouse` to fire, `Right Mouse` to aim down sights, `Q` / `1` / `2` to switch weapons, `R` to reload, and `Tab` to view scores.
   - Step onto neon jump pads to launch onto catwalks; collect power-ups from pedestals.
   - Run a second instance of `RushArenaShared.exe` to see full 3D avatar replication and PvP combat.
2. **WebGL Browser**:
   - Open `http://localhost:8080/index.html` in Chrome, Edge, or Firefox.
   - Enjoy the full game directly in your browser with zero magenta shader defects.

