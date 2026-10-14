# Local dependency setup

The user authorized installing required dependencies on 2026-10-13. No paid service, subscription, remote deployment or purchase is authorized.

## Tools

- Existing Git and .NET SDK 8.0.420 are sufficient for version control and pure logic checks.
- Unity Hub 3.22.2.65535 was installed using the official Windows package and its installer hash was verified by the package manager.
- Unity 6.3 LTS **6000.3.26f1**, revision **de8167e7440c**, was selected from Unity's official release API. The editor installer downloaded into the existing ignored `.local/rush-setup` directory outside the game folder. Its publisher checksum matched and Windows reported a valid Unity Technologies SF signature.
- Windows initially canceled the editor installer launch. The user authorized a retry and approved the Windows prompt; editor installation completed with exit 0. The user confirmed Unity sign-in and an eligible license activation through Hub.
- Hub's command-line registration did not register the manually installed editor, and its module command failed with `No editor found`. Android support, SDK/NDK and OpenJDK were installed from the exact module URLs in Unity's official pinned release metadata instead. The signed support installer and applicable published checksums were checked. Installed files include OpenJDK 17.0.18, Android NDK r27c, build tools 36.0.0, platform tools 37.0.1, command-line tools 16.0, CMake 3.22.1 and API platforms 34–37. Android SDK licenses were accepted through the SDK's own tool (exit 0). The complete toolchain successfully built an ARM64 IL2CPP APK targeting API 36 with minimum API 26.
- URP 17.3.0 is the Unity 6000.3 editor-matched core package; the editor supplies core package resolution. Input System 1.17.0 and Test Framework 1.4.6 are pinned registry packages. Actual Unity package import and configuration succeeded, producing `Packages/packages-lock.json` and serialized project/URP settings.

The Windows app environment redirected the logical editor install location into its local package cache. The actual executable is:

```text
C:\Users\balme\AppData\Local\Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local\Unity\Editors\6000.3.26f1\Editor\Unity.exe
```

If Hub does not list the editor, locate that executable in Hub. The project itself remains in the normal workspace `rush-arena` directory. The completed 4.2GB editor installer cache was removed after successful installation; installed tools were preserved.

## Secure account access

Sign in to Unity Hub directly and activate an eligible license under its Licenses settings. The agent cannot attest personal-license eligibility or choose a paid plan. No account credentials belong in chat, Git or logs shared with the user.

A later command-line run returned license exit 198 even after earlier successful tests/builds. Reopening Hub restored licensing and the configuration retry succeeded. If this recurs, open Hub first; do not copy account tokens or license files between applications.

Plugin discovery for Unity/Photon returned no relevant connector in the searched directory. More plugins may exist in the [plugin directory](https://chatgpt.com/plugins). GitHub tooling is already available, but local Git is sufficient for M1; no additional remote access is needed now.

The user has authorized starting the M2 foundation. Photon Fusion SDK and Simple KCC import/account configuration now await secure sign-in: both official downloads require an authenticated account. See [M2 setup and status](MILESTONE2.md). Use the [official SDK downloads](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download) and [Photon dashboard](https://dashboard.photonengine.com/) with secure sign-in. Do not buy a plan. Never request account secrets in chat.
