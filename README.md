# Vents & Vending

First-person sci-fi black-market business simulator. You live in the vents of Citadel-9 and build an intergalactic empire selling absurd alien products.

- **Design bible:** [`# VENTS & VENDING.txt`](./%23%20VENTS%20%26%20VENDING.txt)
- **Production tracker:** [`Docs/Production/Backlog.md`](Docs/Production/Backlog.md)

## Toolchain (pinned)

| Tool | Version | Notes |
|---|---|---|
| Unity Editor | **6000.3.28f1** (Unity 6.3 LTS) | Installed via Unity Hub to `F:\Unity\Editors` |
| Render pipeline | URP (bundled with the editor) | |
| Input | Input System package (new input handling only) | |
| Unity Hub | 3.22.6 | MSIX install |
| IDE | VS Code + Unity extension (`visualstudiotoolsforunity.vstuc`) + C# Dev Kit | Set as External Script Editor in Unity |
| VCS | Git 2.55 + Git LFS 3.7 | Binary assets go through LFS (see `.gitattributes`) |
| Packages | URP 17.3.0, Input System 1.20.1, AI Navigation 2.0.16, ProBuilder 6.1.4, Test Framework 1.6.0 | Pinned in `Packages/manifest.json` |
| AI editor bridge | Unity-MCP 1.8.0 (`com.ivanmurzak.unity.mcp`, server 9.4.2) | Local mode, `http://localhost:22462`, registered in `.mcp.json` |
| IL2CPP builds | Needs Visual Studio Build Tools (C++ workload) | Not installed yet; Mono builds work without it |

Project settings and URP assets come from Unity's Universal 3D template (`com.unity.template.3d-cross-platform`). Render assets: `Assets/_Project/Settings/Rendering` (`PC_RPAsset` is the default; bloom, tonemapping and vignette are in `GlobalVolumeProfile`).

Don't change the editor version without updating this table and `ProjectSettings/ProjectVersion.txt` in the same commit.

## Opening the project

1. Open Unity Hub, sign in, and activate a **Unity Personal** licence (Hub → Preferences → Licenses).
2. Hub → **Add → Add project from disk** → select this folder.
3. Open with **6000.3.28f1**.
4. In Unity: **Edit → Preferences → External Tools → External Script Editor → Visual Studio Code**.

## Repository layout

```text
Assets/
  _Project/            all first-party content (see GDD §18.2)
    Art/ Audio/ Animations/ Prefabs/ Scenes/
    Scripts/           runtime code → VentsAndVending.asmdef
      Editor/          editor tooling → VentsAndVending.Editor.asmdef
    ScriptableObjects/ data definitions (items, recipes, species …)
    Settings/          URP + Input assets
    Tests/             EditMode / PlayMode test assemblies
    UI/
  ThirdParty/          imported store/vendor assets (keep separate)
Packages/              Unity package manifest (pinned versions)
ProjectSettings/       Unity project settings
Docs/                  design notes, art reference, production docs
Tools/                 helper scripts outside Unity
Builds/                local build output (git-ignored)
```

## Conventions

- Namespace root `VentsAndVending`, mirroring the folder (`VentsAndVending.Player`, `VentsAndVending.Crafting`, …).
- Static data lives in ScriptableObjects (`*Definition`); runtime/save state is kept separately, keyed by stable string IDs (GDD §18.4–18.5).
- Third-party assets only under `Assets/ThirdParty/`.
- Commit `.meta` files together with their assets, always.

## Building & testing

- **Rebuild scenes:** Unity menu **Vents & Vending → Prototype → Build All Scenes** (regenerates `MainMenu` and `Greybox_Movement`; don't hand-edit them yet).
- **Windows player:** **Vents & Vending → Build → Windows Player (Mono)** → `Builds/Windows/VentsAndVending.exe` (git-ignored).
- **Smoke test a build:** `VentsAndVending.exe -batchmode -nographics -smoketest -logFile smoke.log` → look for `SMOKETEST PASS`.
- **Unit tests:** Window → General → Test Runner → EditMode.
- **Saves:** `%USERPROFILE%\AppData\LocalLow\VentsAndVending\Vents & Vending\Saves\save_0.json` (+ `.bak`).
