# RUSH Arena

Unity **6000.3.26f1 (6.3 LTS)**, URP **17.3.0**, Input System **1.17.0**, Photon Fusion **2.1.3**, and Simple KCC **2.1.1**. Responsive PC/mobile FPS prototype. M1 is implemented and the first [Shared Mode foundation](Docs/MILESTONE2.md) is verified with two Windows clients. Physical mobile testing remains deferred.

This is a separate Unity project inside the existing Spring Potato repository. Open **this directory** in Unity Hub. The neighboring web app is not part of the game.

## First run

1. Install the pinned editor through Unity Hub, sign in through Unity's own UI, and activate an eligible license. Do not select a paid license without approval.
2. Add Android Build Support, OpenJDK, and Android SDK/NDK modules through Hub for Android builds. Windows Mono builds use the editor's existing Windows support. iOS builds need a Mac with Xcode and are outside this Windows setup.
3. Open the project and let Unity resolve its packages. Run **RUSH Arena > Configure Milestone 1** once. It creates the URP assets, sets Input System handling, configures low-cost rendering, and saves `Assets/RushArena/Scenes/ArenaM1.unity` as the build scene. Restart the editor if Unity requests it for input handling.
4. Open the scene and press Play. Click the Game view to capture the cursor. **WASD** move, **Shift** sprint, **Space** jump, **Esc** release. **F8** exports a diagnostic report to Unity's persistent data directory.
5. On mobile: left-side drag moves, right-side drag looks, hold **SPRINT**, tap **JUMP**. Diagnostic reports export locally every 60 seconds and when the app is backgrounded. In the scene's bootstrap inspector, `Editor Touch Preview` allows a single simulated touch with the mouse; it cannot verify simultaneous real fingers.

## Verification

Run the engine-independent checks:

```powershell
dotnet run --project Tools/CoreChecks/CoreChecks.csproj -c Release
```

With a licensed editor installed:

```powershell
./Tools/Verify-Unity.ps1 -UnityPath 'C:\path\to\Editor\Unity.exe'
```

This runs configuration/import, EditMode tests, and PlayMode tests and fails if results are missing or contain failures. Logs/results go to ignored `TestResults`. Run **RUSH Arena > Build Windows Development** and **Build Android Development** for actual builds. Physical device and human gameplay checks remain required; automated tests cannot certify mouse feel or touch comfort.

Read [verification and acceptance](Docs/VERIFICATION.md), [architecture](Docs/ARCHITECTURE.md), and [full game vision](Docs/VISION.md). Consult the recorded results before treating M1 as complete.

## Current local outputs

- `Builds/Windows/RushArena.exe`: verified development player. Launch normally to play; click its window to capture the mouse.
- `Builds/Android/RushArena.apk`: verified ARM64 IL2CPP build, Android API 26+; physical-device testing is pending.
- [Actual rendered arena capture](Docs/Evidence/ArenaM1-PC.png): Windows URP scene capture, overlay HUD excluded.
- [Recorded results](Docs/VERIFICATION.md): 15 pure movement checks, 8 EditMode tests and 5 PlayMode tests passed. Interactive mouse feel and sustained PC/mobile performance remain acceptance tasks.

`ArenaM1` remains the local CharacterController baseline. `ArenaShared` uses Photon Shared Mode, the tested frame-to-tick input buffer, and Simple KCC. A private named-room two-client smoke test passes; public matchmaking and match gameplay remain later work. See [M2 results and open verification](Docs/MILESTONE2.md).
