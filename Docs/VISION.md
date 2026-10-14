# RUSH Arena vision and milestone boundary

The full game remains a fast tactical FPS for PC and mobile with **10-player online rooms**, **120-second matches**, **special weapon power-ups**, **jump pads**, **public and private matchmaking**, and **global leaderboards**.

Milestone 1 contains only responsive first-person movement, mouse camera, sprint, jump, a basic arena, mobile input abstraction and performance diagnostics. It contains no weapons, matchmaking, timer, scoring, leaderboards, network sessions or jump-pad gameplay.

Later multiplayer technology: **Photon Fusion 2, Shared Mode**. Local movement acceptance must precede network work. Consider the separately distributed **Photon Simple KCC** addon as the network motor candidate. Its current documentation supports Shared Mode and mobile platforms; replacing this milestone's CharacterController should preserve the accepted feel and re-run its collision/latency checks.

Do not infer that the current pure movement model is deterministic across machines or ready for network prediction. Future Fusion state/tick ownership needs its own design. Shared Mode distributes authority to clients, so competitive leaderboard integrity will need a separate validated scoring design; that decision is deferred.

After M1 passes, request authorization for M2 and securely create/connect a Photon account and Fusion application through the official dashboard. Keep application configuration out of Git. Confirm applicable service plans and charges with the user before any purchase or paid deployment.

Primary references checked 2026-10-13:

- [Photon Simple KCC](https://doc.photonengine.com/fusion/v2/addons/simple-kcc)
- [Photon Shared Mode overview](https://doc.photonengine.com/fusion/v2/manual/shared-mode)
- [Unity 6000.3.26f1 release](https://unity.com/releases/editor/whats-new/6000.3.26f1)
- [Unity CharacterController.Move](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html)
- [URP 17.3 source and package APIs](https://github.com/Unity-Technologies/Graphics/tree/6000.3/staging/Packages/com.unity.render-pipelines.universal)
