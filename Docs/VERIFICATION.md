# M1 verification record — 2026-10-13

**Status: M1 implementation and automated verification complete. On 2026-10-13 the user said "all good please proceed", authorizing M2 foundation work. This approval does not constitute new measured PC performance or physical mobile evidence; the outstanding checks below remain recorded. See [M2 progress](MILESTONE2.md).**

## Actual evidence so far

- Inspected workspace: existing Spring Potato Next.js repository, no Unity assets/project. Existing user change: `next-env.d.ts`; preserved outside game commits.
- Created `codex/rush-arena-m1` using the existing repository and remote. No remote push or new remote repository.
- Unity Hub installed successfully using the official package. Editor 6000.3.26f1 installed successfully (installer exit 0) after the user approved retrying Windows elevation and confirmed secure account/license activation.
- `dotnet run --project rush-arena/Tools/CoreChecks/CoreChecks.csproj -c Release`: **15/15 passed**, compiling the real pure movement source with .NET 8.0.420. This checks movement math, diagonal normalization, sprint, stopping, jump windows, no double jump, ceiling velocity, fall cap, yaw/pitch, frame-rate comparison and reset.
- Actual Unity import/compilation/configuration succeeded; URP 17.3.0, Input System 1.17.0 and the package lock resolved. An initial duplicate test-assembly reference failed import; it was removed and the corrected run succeeded.
- Actual Unity EditMode tests: **8/8 passed**, NUnit result `Passed`, no failures. Includes synthetic Input System multi-touch, cancellation and focus-loss checks. These are not physical device tests.
- Actual Unity PlayMode tests: **5/5 passed**, covering real ground/wall/ceiling collision, jump landing, step climbing and movement/look frame-rate behavior.
- Windows development player: **build succeeded** and **rendered scripted smoke passed** on Intel i5-12400F / AMD Radeon RX 6600, Direct3D 11 at 1280×720. Walk over roughly 1s: 6.674m, sprint over roughly 0.5s: 5.174m, jump apex: 1.406m, landed grounded, yaw: 90°. Rendered 921,600 visible pixels, zero magenta error-shader pixels. The actual URP offscreen screenshot was visually inspected: floor, walls and teal cover/platform rendered correctly. Overlay HUD is excluded from the capture.
- The initial hidden-window backbuffer screenshot was black even though motion passed. The probe was corrected to render through URP into a texture and validate pixels; the corrected run passed. Do not treat the earlier file-existence-only result as visual verification.
- Short scripted run after warmup: HUD reported **144 FPS**, mean **6.96ms**, p95 **7.01ms**, 394 samples. Last main-thread sample 7.04ms and last GC sample 0B. This is short hidden-window evidence, not a sustained performance, zero-allocation or hardware-input-latency acceptance claim.
- Android support, SDK/NDK/OpenJDK files installed and SDK licenses accepted. **Android ARM64 IL2CPP development build succeeded**, editor exit 0, producing `Builds/Android/RushArena.apk` (41,927,328 bytes). APK contents include an Android manifest, ARM64 `libunity.so` and `libil2cpp.so`; no ARMv7 libraries. This is build/package verification, not installation or a device gameplay test.
- Physical mobile testing: **pending by user choice**. No Android device was detected; user explicitly requested leaving this check pending.

Compact NUnit results, the Windows smoke/performance reports and the inspected screenshot are preserved in `Docs/Evidence`. Full logs and builds remain in ignored `TestResults` and `Builds`.

The project structure/meta GUID check passed, the verification PowerShell script parsed successfully, and Git ignore checks excluded generated files, builds, Photon account settings and signing keys. No populated sensitive fields were found in serialized project settings. Git whitespace checks allow Unity's native empty-field serialization while enforcing source-file whitespace.

## Automated Unity gate

`Tools/Verify-Unity.ps1` requires a licensed pinned editor and invokes `RushArena.Editor.MilestoneSetup.Configure`, EditMode tests, then PlayMode tests. Store editor logs and NUnit XML in ignored `TestResults`. A missing XML, no executed tests, nonzero exit, failed test or non-Passed test-run blocks acceptance. Tests exercise real CharacterController ground/wall/ceiling collisions, jump landing and rendered-frame movement/look behavior. No engine stubs count toward this gate.

## PC acceptance (pending)

- Build and launch Windows development player; note CPU/GPU/display resolution and refresh rate.
- Click capture and Esc release behave correctly; alt-tab stops motion, returning requires recapture with no jump/look burst.
- Mouse turn amount is consistent at 30, 60 and 144 FPS; no visible camera judder or added smoothing delay.
- WASD/diagonal speeds, immediate sprint transition, jump apex/landing, coyote/buffer behavior feel responsive.
- Arena wall collisions, 0.2m steps, ramp and low ceiling produce no clipping, jitter, stuck movement or unintended repeated jumping.
- After a 30-second warmup, run a 5-minute arena traversal and export reports. Record rolling p95 and profiler samples, not a made-up average. Target p95 <= 6.94ms on the chosen PC at 144Hz, subject to agreed hardware.

## Mobile acceptance (pending; physical device required)

- Build/install Android player and record device, OS, GPU, resolution and render scale. A simulated mouse touch is not a device test.
- Move/look simultaneously with two fingers; move/look/jump and held sprint with extra fingers. Verify controls do not swap roles across button boundaries.
- Cancel touches, background/resume, release all fingers, reconnect input and test screen cutouts/safe areas. No stuck sprint, movement or jump.
- Traverse continuously for 10 minutes to expose thermal throttling, report p95 <=16.67ms for the selected representative device at 60 FPS, and inspect Unity profiler for GC spikes. Document device limitations honestly.
- UI contrast, button reach, camera sensitivity and finger overlap need human acceptance. iOS remains a separate Mac/device setup decision.

## Next gate

The user has authorized M2 foundation work with physical mobile testing still deferred. Keep the pending interactive PC checklist and sustained performance check open; choose representative Android hardware when the user is ready. Open Unity Hub before command-line checks if licensing needs refresh. Photon account sign-in must occur in its secure provider UI.
