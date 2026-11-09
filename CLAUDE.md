# Vents & Vending — agent notes

Unity 6.3 LTS (6000.3.28f1) / URP / C# first-person business sim. The full design is `# VENTS & VENDING.txt` at the repo root and is authoritative. Read the relevant PART before implementing a system. Scope rules: PART 27. First tasks: Appendix B, tracked in `Docs/Production/Backlog.md`. Vent layout and tunnel progression: `Docs/Design/TunnelNetwork.md` (§0 holds agreed amendments). Art direction and the character pipeline (Blender → Unity): `Docs/Art/ArtDirection.md` (§0 holds project amendments; stage gates need owner approval). Blender sources live in `Art/Blender/` (outside `Assets/`).

- Unity project = repo root. First-party content only under `Assets/_Project/`.
- Runtime code: `Assets/_Project/Scripts/<System>/`, namespace `VentsAndVending.<System>`, assembly `VentsAndVending`.
- Editor code: `Assets/_Project/Scripts/Editor/` (assembly `VentsAndVending.Editor`).
- Tests: `Assets/_Project/Tests/{EditMode,PlayMode}`. Prefer EditMode tests for pure logic (inventory, recipes, economy, save data).
- Static data = ScriptableObject `*Definition`; runtime state separate; save via JSON with stable string IDs + version number.
- Input: Input System only (no legacy `UnityEngine.Input`).
- Movement: `CharacterController`, central state enum (GDD §3.2 tuning table).
- Never hand-edit `.meta` GUIDs; when moving assets outside Unity, move the `.meta` with them.
- Unity MCP: Ivan Murzak's Unity-MCP plugin (`com.ivanmurzak.unity.mcp`, OpenUPM) in **Custom/local** mode, serving HTTP on `localhost:22462`; registered in `.mcp.json` as `unity`. Unity must be open with the plugin's server running. Prefer `rql-query` for reading the scene/console and `tests-run` to verify code. Keep Cloud mode off.
- Unity Hub CLI: VS Code sets `ELECTRON_RUN_AS_NODE`, so unset it before running `Unity Hub.exe -- --headless …`.
