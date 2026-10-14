# Project architecture

`Assets/RushArena/Core` owns movement tuning, commands, jump windows, acceleration and look bounds. It has no engine or network references. The same actual source is compiled by .NET checks and Unity tests.

`Runtime/Input` converts keyboard/mouse or EnhancedTouch into `PlayerCommand` through `IPlayerInput`. Mouse input uses degrees per pixel without frame-time multiplication. Touch look is normalized to the shorter safe-area dimension. Each finger owns its control from begin until end/cancel. Jump is an edge; sprint is held. Focus/pause clears controls and touch waits for all fingers to release before accepting new gestures.

`Runtime/Player` handles Unity collisions and the camera. CharacterController is an offline implementation for M1. Movement runs from rendered Update with <= 1/120-second collision substeps and one look update per rendered frame. A stall simulates at most 100 ms; frame diagnostics retain the full actual frame duration. A future Fusion Simple KCC implementation belongs here, behind the input/command seam, after M1 acceptance.

The starting capsule is 1.8m tall and 0.35m radius. Walk is 7m/s; sprint is 10.5m/s; acceleration is 65m/s² grounded and 18m/s² airborne. Jump is 1.4m with 25m/s² gravity, 100ms coyote time and 120ms buffering. These are tuning starting points, not measured acceptance results.

`Runtime/Arena` builds a small primitive arena once at scene startup, with shared materials, flat lighting, lanes, cover, a ramp, 0.2m steps, jump platforms and a low-ceiling check. Geometry is runtime-created for rapid iteration; artists can later replace the bootstrap without entangling movement logic.

`Runtime/Diagnostics` excludes a three-second startup warmup, then uses fixed 600-sample arrays for rolling mean and p95 frame duration and shows available Unity profiler counters. Counters unavailable on a platform are labeled unavailable. The HUD refreshes 4Hz, so its strings/canvas rebuilding allocate at that rate. It is not a zero-allocation benchmark or an input-latency/GPU-time measurement. F8 writes a local report with Unity/device/resolution metadata; mobile exports automatically every minute and on backgrounding.

The development-only `-rush-smoke` argument drives a built player without hardware input and writes a motion result plus an actual URP offscreen scene capture. It checks visible pixels, rejects uniform/black images and magenta error shaders, and requires manual screenshot inspection. The overlay HUD is excluded from this capture. This short hidden-window check does not replace interactive PC or physical mobile acceptance.

`Editor` configures serialized project/URP settings and builds development players. `Tests` has independent EditMode and PlayMode assemblies. `Tools` provides a dependency-free movement check runner, structural verifier, and Unity verification script.

Keep URP forward rendering, HDR/shadows/additional lights/depth/opaque textures disabled, SRP Batcher enabled, and a short far plane. Start at render scale 1, no MSAA; lower scale only after measured device GPU constraints. Desired sustained targets: 144 FPS PC and 60 FPS representative mobile. Hardware must be selected before these can become acceptance claims.
