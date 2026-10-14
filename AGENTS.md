# RUSH Arena development rules

These rules apply to this Unity project and its supporting tools.

- The user approved proceeding beyond M1 on 2026-10-13 ("all good please proceed"). M2 multiplayer foundation is authorized; physical mobile testing remains deferred by user choice and sustained performance checks remain unverified.
- Target Unity 6.3 LTS, URP, and later Photon Fusion 2 Shared Mode. Keep the full vision in `Docs/VISION.md`.
- Prioritize input latency, stable frame times and readable arenas over graphics.
- Separate pure movement rules, device input, Unity collision/presentation, diagnostics and editor tooling. Keep Core free of Unity and Photon dependencies.
- Use the Input System. Mouse delta is displacement, never multiplied by frame time. Touch controls must support simultaneous move/look/jump/sprint, safe areas, cancellation and focus loss.
- Commit Unity `.meta` files, force text serialization, and use assembly definitions. Never commit Library, builds, logs, credentials or Photon application settings.
- Keep the M1 offline scene as a regression baseline. M2 targets Fusion 2 Shared Mode and evaluates Simple KCC in a separate multiplayer assembly/scene after the real vendor SDK is installed. Do not infer SDK APIs or claim network tests from local simulations.
- Use official documentation and inspect installed package APIs before changing Unity APIs. Do not create fake Unity stubs and claim engine compilation.
- Verify pure logic with `Tools/CoreChecks`; verify collisions and integration with Unity EditMode/PlayMode tests. Keep real results and pending checks in `Docs/VERIFICATION.md`.
- Do not claim compilation, builds, frame rates or device tests without actual evidence. Do not store secrets in reports.
- Install only authorized tooling; account sign-in and eligibility declarations happen in provider UI. Never request passwords or secrets in chat. Never select paid services without approval.
- Preserve the neighboring Spring Potato app and pre-existing changes. Git commits for this task must include only RUSH Arena files.
