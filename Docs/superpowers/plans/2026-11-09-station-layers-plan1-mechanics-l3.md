# Station Layers, Plan 1: Layer Mechanics + L3 Test Bay — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the three mechanics the layered station needs (climb onto crates / into head-height vent mouths, the full-size sliding den door, pushable crates), the kit pieces they need, and a playable L3 maintenance test bay that proves den door → corridor → crate → vent mouth → riser → L2 crawl duct.

**Architecture:** Pure rules live in static logic classes (EditMode-tested, like `PlayerStateLogic`); thin MonoBehaviours apply them. Kit pieces are built in Blender by `vv_build_env_kit.py`, exported as FBX, and turned into prefabs (with their components wired) by `EnvironmentKitSetup.cs`. The test bay is a separate scene built by a new `PrototypeSceneBuilder` partial, using the real player (`CreatePlayer`) and HUD, so the existing `Greybox_Movement` map stays untouched until the full assembly (Plans 2–3).

**Tech Stack:** Unity 6.3 (6000.3.28f1) URP, C#, Input System, NUnit EditMode tests run through the Unity MCP `tests-run` tool; Blender 5.2 headless Python for kit modules.

**Spec:** `Docs/superpowers/specs/2026-11-09-station-ring-layers-design.md` (approved 2026-11-09). Read §3 (layers), §3.2 (connectors), §6 (mechanics), §7 (anti-goals), §8 (kit).

## Global Constraints

- **No git commits** in this plan (user, 2026-11-09: nothing is committed until a full assembly replaces the existing map). Where a task would commit, run the full EditMode suite instead and leave the work uncommitted.
- Runtime code: `Assets/_Project/Scripts/<System>/`, namespace `VentsAndVending.<System>`, assembly `VentsAndVending`. Editor code: `Assets/_Project/Scripts/Editor/`, namespace `VentsAndVending.Editor`. Tests: `Assets/_Project/Tests/EditMode/`, namespace `VentsAndVending.Tests`.
- Input System only (no `UnityEngine.Input`). Movement stays on `CharacterController`; the state enum is `PlayerMovementState`.
- Layer heights (spec §3): **L1 floor +4.6, L2 duct floor +3.3 (clear 1.0 m), L3 floor 0 (3 m walls), L4 floor −3.6.** Vent mouth bottom edge **1.8 m** above the L3 floor.
- Climbable ledge (spec §6.1): crates ≤ **1.1 m**; a vent mouth is climbable when its sill is within **~0.9 m** of the surface the player stands on. Climb takes **~0.4–0.6 s**, has no stamina and cannot fail. No ladders, no free climbing.
- Kit grid: 2 m modules; wall pivot = bottom centre of the room-facing surface (room side +Z after import); floors top at y 0; ceilings underside at y 0, placed at 3 m.
- No fall damage anywhere; dropping out of a vent mouth is always safe (TunnelNetwork §0.5).
- Den → first customer stays short: ≤ ~15 m of crawl (spec §7 anti-goals).
- Keep the Blender live session untouched: kit builds run headless (`blender -b --factory-startup -P …`).

## Review Focus

1. **Climbing into a vent mouth while standing** — the capsule is 1.8 m and the duct is 1.0 m: the climb must resolve to `Crawling` *before* the body moves in, and the player must end inside the `VentZone` crawling, never stuck in the duct ceiling. (Task 1 test `VentMouthLedge_ResolvesCrawl`; Task 2 play check.)
2. **Climb refused where it doesn't fit** — ledge too high/low, no clearance above (crate under a low pipe), too shallow a top: no teleport, normal jump happens instead. (Task 1 tests `TooHigh_Refused`, `NoClearance_Refused`, `TooShallow_Refused`.)
3. **The door closing on the player** — standing in the doorway must keep it open; a locked door never opens and says why. (Task 3 tests `Occupied_StaysOpen`, `Locked_NeverOpens`.)
4. **A crate pushed into a useless or blocking spot** — pushes into walls, off ledges or onto other crates are refused; the fixed crate under the main vent mouth can't be pushed at all. (Task 4 tests `Blocked_Refused`, `NoGround_Refused`; fixed crate has no `PushableCrate`.)
5. **Climbing while carrying / while interacting / mid-air** — climbing only starts grounded, upright or crouched, not while `IsInteracting`; jumping never happens in the same press as a climb. (Task 1 test `CanStartClimb_*`.)

---

## File Structure

| File | Responsibility |
|---|---|
| `Scripts/Player/PlayerMovementState.cs` (modify) | add `Climbing` |
| `Scripts/Player/ClimbLogic.cs` (create) | pure ledge rules: can start, resolve ledge → posture, duration |
| `Scripts/Player/PlayerStateLogic.cs` (modify) | `IsClimbing` + `ClimbPosture` inputs |
| `Scripts/Player/PlayerTuning.cs` (modify) | climb tuning values |
| `Scripts/Player/PlayerClimb.cs` (create) | probes the world, runs the mantle |
| `Scripts/Player/PlayerStateController.cs`, `PlayerMovement.cs` (modify) | honour climbing |
| `Scripts/World/SlidingDoorLogic.cs`, `Scripts/World/SlidingDoor.cs` (create) | door rules + component |
| `Scripts/World/CratePushLogic.cs`, `Scripts/World/PushableCrate.cs` (create) | push rules + component |
| `Tests/EditMode/ClimbLogicTests.cs`, `SlidingDoorTests.cs`, `CratePushTests.cs` (create) | EditMode tests |
| `Art/Blender/Scripts/vv_build_env_kit.py` (modify) | door, vent mouth, duct modules; multi-object export |
| `Scripts/Editor/EnvironmentKitSetup.cs` (modify) | wire components on the new prefabs |
| `Scripts/Editor/PrototypeSceneBuilder.StationTestBay.cs` (create) | L3 test-bay scene |

---

### Task 1: Climb rules (pure)

**Files:**
- Modify: `Assets/_Project/Scripts/Player/PlayerMovementState.cs`, `PlayerStateLogic.cs`, `PlayerTuning.cs`
- Create: `Assets/_Project/Scripts/Player/ClimbLogic.cs`
- Test: `Assets/_Project/Tests/EditMode/ClimbLogicTests.cs`, add to `PlayerStateLogicTests.cs`

**Interfaces:**
- Produces:
  - `PlayerMovementState.Climbing` (appended after `Interacting`).
  - `PlayerTuning`: `float MinClimbHeight = 0.35f, MaxClimbHeight = 1.15f, MinClimbDepth = 0.65f, ClimbBaseTime = 0.35f, ClimbTimePerMetre = 0.2f, ClimbProbeDistance = 0.8f`; `SpeedFor(Climbing) == 0`.
  - `static class ClimbLogic` (namespace `VentsAndVending.Player`):
    - `bool CanStartClimb(PlayerMovementState state, bool grounded, bool interacting)` — true only when grounded, not interacting, and state is `Normal`, `Sprinting` or `Crouching`.
    - `bool TryResolve(float ledgeHeight, float clearance, float depth, PlayerTuning tuning, out PlayerMovementState posture)` — `ledgeHeight` = ledge top above the feet; `clearance` = free height above the ledge top; `depth` = free depth on top. Climbable iff `MinClimbHeight ≤ ledgeHeight ≤ MaxClimbHeight`, `clearance ≥ CrawlCapsuleHeight`, `depth ≥ MinClimbDepth`. Posture = `Normal` if `clearance ≥ NormalCapsuleHeight`, else `Crouching` if `≥ CrouchCapsuleHeight`, else `Crawling`.
    - `float Duration(float ledgeHeight, PlayerTuning tuning)` = `ClimbBaseTime + ClimbTimePerMetre * ledgeHeight`, clamped to [0.4, 0.6].
  - `PlayerStateInput`: `bool IsClimbing; PlayerMovementState ClimbPosture;` — when `IsClimbing`, `ResolvePosture` returns `ClimbPosture` and `Resolve` returns `Climbing` (overrides `IsInteracting`).

- [ ] **Step 1: Write the failing tests** in `ClimbLogicTests.cs` (default `new PlayerTuning()`):

```csharp
[Test] public void CrateHeight_StandsOnTop() { Assert.IsTrue(ClimbLogic.TryResolve(1.0f, 3f, 1f, T, out var p)); Assert.AreEqual(PlayerMovementState.Normal, p); }
[Test] public void VentMouthLedge_ResolvesCrawl() { Assert.IsTrue(ClimbLogic.TryResolve(0.79f, 0.9f, 2f, T, out var p)); Assert.AreEqual(PlayerMovementState.Crawling, p); }
[Test] public void LowCeilingLedge_ResolvesCrouch() { Assert.IsTrue(ClimbLogic.TryResolve(0.8f, 1.4f, 1f, T, out var p)); Assert.AreEqual(PlayerMovementState.Crouching, p); }
[Test] public void TooHigh_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(1.8f, 3f, 1f, T, out _));   // vent mouth from the floor
[Test] public void TooLow_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(0.2f, 3f, 1f, T, out _));
[Test] public void NoClearance_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(1.0f, 0.5f, 1f, T, out _));
[Test] public void TooShallow_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(1.0f, 3f, 0.3f, T, out _));
[Test] public void Duration_InRange() { Assert.AreEqual(0.4f, ClimbLogic.Duration(0.2f, T), 1e-4); Assert.AreEqual(0.55f, ClimbLogic.Duration(1.0f, T), 1e-4); Assert.AreEqual(0.6f, ClimbLogic.Duration(5f, T), 1e-4); }
[Test] public void CanStartClimb_Grounded_Upright() => Assert.IsTrue(ClimbLogic.CanStartClimb(PlayerMovementState.Normal, true, false));
[Test] public void CanStartClimb_Crouched() => Assert.IsTrue(ClimbLogic.CanStartClimb(PlayerMovementState.Crouching, true, false));
[Test] public void CanStartClimb_NotInAir() => Assert.IsFalse(ClimbLogic.CanStartClimb(PlayerMovementState.Normal, false, false));
[Test] public void CanStartClimb_NotCrawlingOrInteracting() { Assert.IsFalse(ClimbLogic.CanStartClimb(PlayerMovementState.Crawling, true, false)); Assert.IsFalse(ClimbLogic.CanStartClimb(PlayerMovementState.Normal, true, true)); }
```
and in `PlayerStateLogicTests.cs`:
```csharp
[Test] public void Climbing_OverridesInteracting_AndForcesPosture() { var i = Input(); i.IsClimbing = true; i.ClimbPosture = PlayerMovementState.Crawling; i.IsInteracting = true;
  Assert.AreEqual(PlayerMovementState.Climbing, PlayerStateLogic.Resolve(i)); Assert.AreEqual(PlayerMovementState.Crawling, PlayerStateLogic.ResolvePosture(i)); }
[Test] public void CannotJump_WhileClimbing() => Assert.IsFalse(PlayerStateLogic.CanJump(PlayerMovementState.Climbing, true));
```
And in `PlayerTuningTests.cs`: `Assert.AreEqual(0f, new PlayerTuning().SpeedFor(PlayerMovementState.Climbing));`

- [ ] **Step 2: Run, expect compile failure / FAIL.** Unity MCP `tests-run` with `{"testMode":"EditMode","testClass":"ClimbLogicTests"}` → fails (`ClimbLogic` not defined).
- [ ] **Step 3: Implement** the enum value, tuning fields, `PlayerStateInput` fields + the two `PlayerStateLogic` branches, and `ClimbLogic` per the Interfaces block.
- [ ] **Step 4: Run** `tests-run {"testMode":"EditMode"}` → all EditMode tests PASS (existing ones included).

### Task 2: PlayerClimb component (the mantle)

**Files:**
- Create: `Assets/_Project/Scripts/Player/PlayerClimb.cs`
- Modify: `PlayerStateController.cs` (feed `IsClimbing`/`ClimbPosture`), `PlayerMovement.cs` (skip movement and jump while climbing), `Scripts/Editor/PrototypeSceneBuilder.cs` `CreatePlayer` (add the component)

**Interfaces:**
- Consumes: `ClimbLogic.*`, `PlayerTuning` climb fields (Task 1).
- Produces: `sealed class PlayerClimb : MonoBehaviour` with `[DefaultExecutionOrder(-45)]` (after `PlayerStateController` at −50, before `PlayerMovement`), properties `bool IsClimbing`, `PlayerMovementState ClimbPosture`, `bool JumpConsumedThisFrame`, and `public bool TryClimb()` (also used by play checks). `PlayerStateController` reads `IsClimbing`/`ClimbPosture`; `PlayerMovement` returns early while `IsClimbing` and skips its jump when `JumpConsumedThisFrame`.

- [ ] **Step 1: Implement `TryClimb()`** — on `JumpPressed`, if `ClimbLogic.CanStartClimb(...)` and forward input > 0.3 (or always when called directly):
  1. Capsule-cast forward from the feet + 0.4 m up to `ClimbProbeDistance` to find a face (ignore triggers and the player).
  2. From `hit.point + forward * 0.35` at height `feet + MaxClimbHeight + 0.1`, raycast down to find the ledge top → `ledgeHeight`.
  3. From the ledge top + 0.02, raycast up (max 2 m) → `clearance`; raycast forward along the top at +0.3 m (max 1.5 m) → `depth`.
  4. If `ClimbLogic.TryResolve(...)`: set `IsClimbing`, `ClimbPosture`, `JumpConsumedThisFrame`; disable the `CharacterController`; over `Duration` move up to `ledgeTop + 0.02` (first 60% of the time), then forward to the edge + `max(0.4, radius + 0.1)` (last 40%); re-enable the controller; clear `IsClimbing`.
  - Keep the forced posture applied for the whole move, so `PlayerCrouch` shrinks the capsule before the body enters a duct.
- [ ] **Step 2: Wire it in.** Add `player.AddComponent<PlayerClimb>()` in `CreatePlayer` (after `PlayerMovement`), and make `PlayerStateController` and `PlayerMovement` honour it per the Interfaces block.
- [ ] **Step 3: Run** `tests-run {"testMode":"EditMode"}` → all PASS (no regressions).
- [ ] **Step 4: Play check (after Task 6 exists, revisit here).** In the test bay, enter Play mode and run via MCP `script-execute`: put the player 0.6 m in front of the fixed crate facing it, call `TryClimb()`, wait 1 s, and assert the player's feet are at the crate top (± 0.05). Repeat on the crate facing the vent mouth: assert `VentCrawler.InVent == true` and `PlayerStateController.Current == Crawling` after 1 s.

### Task 3: Sliding door

**Files:**
- Create: `Assets/_Project/Scripts/World/SlidingDoorLogic.cs`, `Assets/_Project/Scripts/World/SlidingDoor.cs`
- Test: `Assets/_Project/Tests/EditMode/SlidingDoorTests.cs`

**Interfaces:**
- Produces:
  - `static class SlidingDoorLogic` (namespace `VentsAndVending.World`):
    - `bool WantsOpen(bool locked, bool playerNear, bool occupied)` → `!locked && (playerNear || occupied)`.
    - `float Step(float openAmount, bool open, float openSeconds, float dt)` → moves 0..1 toward the target at `1/openSeconds` per second, clamped.
  - `sealed class SlidingDoor : MonoBehaviour, IInteractable` with serialized `Transform[] _leaves`, `Vector3[] _slideOffsets` (local offset per leaf when fully open), `float _openSeconds = 0.6f`, `float _senseRadius = 2.2f`, `Vector3 _doorwaySize = (1.4, 2.4, 1.0)` (centred on the door's local origin + 1.2 up), `bool _locked`, `string _lockedMessage = "Sealed. This sector is closed."`, open/close `AudioClip`s; `public bool Locked { get; set; }`; `public float OpenAmount { get; }`; `internal void Configure(Transform[] leaves, Vector3[] slideOffsets)` for the kit setup. `GetPrompt` returns `_lockedMessage` when locked (as an info prompt; `Interact` does nothing) and null otherwise (the door is automatic).
  - Not saved: the den door is automatic; sector bulkheads are permanently `Locked` in this plan.

- [ ] **Step 1: Write the failing tests:**

```csharp
[Test] public void Near_Opens() => Assert.IsTrue(SlidingDoorLogic.WantsOpen(false, true, false));
[Test] public void Away_Closes() => Assert.IsFalse(SlidingDoorLogic.WantsOpen(false, false, false));
[Test] public void Occupied_StaysOpen() => Assert.IsTrue(SlidingDoorLogic.WantsOpen(false, false, true));
[Test] public void Locked_NeverOpens() => Assert.IsFalse(SlidingDoorLogic.WantsOpen(true, true, true));
[Test] public void Step_ReachesOpenInOpenSeconds() { float a = 0f; for (int i = 0; i < 60; i++) a = SlidingDoorLogic.Step(a, true, 0.6f, 0.01f); Assert.AreEqual(1f, a, 1e-4); }
[Test] public void Step_Clamped() { Assert.AreEqual(0f, SlidingDoorLogic.Step(0f, false, 0.6f, 1f)); Assert.AreEqual(1f, SlidingDoorLogic.Step(1f, true, 0.6f, 1f)); }
```
- [ ] **Step 2: Run** `tests-run {"testMode":"EditMode","testClass":"SlidingDoorTests"}` → FAIL (not defined).
- [ ] **Step 3: Implement both classes.** `SlidingDoor.Update`:
  - `playerNear` = the "Player"-tagged object within `_senseRadius` of the door origin.
  - `occupied` = the player's position inside the `_doorwaySize` box.
  - Step `OpenAmount`, then set each leaf's local position to `closedLocal + _slideOffsets[i] * smoothstep(OpenAmount)`, caching `closedLocal` in `Awake`.
  - Play the open sound when the target turns open and the close sound when it turns closed.
  - Leaf colliders move with the leaves, so a closed door blocks and an open one doesn't.
- [ ] **Step 4: Run** `tests-run {"testMode":"EditMode"}` → all PASS.

### Task 4: Pushable crate

**Files:**
- Create: `Assets/_Project/Scripts/World/CratePushLogic.cs`, `Assets/_Project/Scripts/World/PushableCrate.cs`
- Test: `Assets/_Project/Tests/EditMode/CratePushTests.cs`

**Interfaces:**
- Produces:
  - `static class CratePushLogic`:
    - `Vector3 PushAxis(Vector3 playerForward)` → the world cardinal axis (±X or ±Z, y = 0) closest to the flattened forward.
    - `bool CanPush(bool destinationOverlaps, bool groundUnderDestination)` → `!destinationOverlaps && groundUnderDestination`.
  - `sealed class PushableCrate : MonoBehaviour, IInteractable`: `float _step = 0.5f`, `float _moveSeconds = 0.25f`, scrape `AudioClip`.
    - `GetPrompt` → `"Push crate"` (null while moving).
    - `Interact` → computes the axis from the interactor's camera forward, then checks the destination: an `OverlapBox` of the crate's collider bounds shrunk by 0.02 at `pos + axis * _step`, ignoring the crate itself and triggers; and a down-ray at the destination centre hitting ground within 0.15 m of the crate's bottom.
    - Then slides there over `_moveSeconds` or shows the toast *"It won't budge that way."*
  - Crate positions are **not saved** (reset on load). The fixed crate under the main vent mouth is a plain crate without this component.

- [ ] **Step 1: Write the failing tests:**

```csharp
[Test] public void Axis_SnapsToZ() => Assert.AreEqual(Vector3.forward, CratePushLogic.PushAxis(new Vector3(0.3f, -0.5f, 0.9f)));
[Test] public void Axis_SnapsToNegX() => Assert.AreEqual(Vector3.left, CratePushLogic.PushAxis(new Vector3(-0.8f, 0.2f, 0.4f)));
[Test] public void Blocked_Refused() => Assert.IsFalse(CratePushLogic.CanPush(true, true));
[Test] public void NoGround_Refused() => Assert.IsFalse(CratePushLogic.CanPush(false, false));
[Test] public void Clear_Allowed() => Assert.IsTrue(CratePushLogic.CanPush(false, true));
```
- [ ] **Step 2: Run** `tests-run {"testMode":"EditMode","testClass":"CratePushTests"}` → FAIL.
- [ ] **Step 3: Implement** per the Interfaces block. Use the existing toast/HUD path that `CrowbarGrate` uses for its "Rusted solid" message.
- [ ] **Step 4: Run** `tests-run {"testMode":"EditMode"}` → all PASS.

### Task 5: Kit pieces in Blender (door, vent mouth, ducts)

**Files:**
- Modify: `Art/Blender/Scripts/vv_build_env_kit.py`

**Interfaces:**
- Produces FBX files under `Assets/_Project/Art/Environment/Kit/`:
  - `Walls/ENV_Wall_DoorSliding_A.fbx` — objects `ENV_Wall_DoorSliding_A` (frame), `Leaf_L`, `Leaf_R` (children).
    - Opening 1.3 × 2.4 m, centred.
    - Leaves 0.66 × 2.42 × 0.08, sitting in the frame at y 0.12–0.20 behind the face.
    - Each leaf slides 0.68 m sideways into the wall (±X in Blender).
    - Leaf dressing: orange hazard band, centre diamond, cyan status lamp in the header.
  - `Walls/ENV_Wall_VentMouth_A.fbx` — objects `ENV_Wall_VentMouth_A` (wall with an opening 0.9 w × 0.85 h, sill at **z 1.8**, hazard-striped lip proud of the face by 0.06) and `Grille` (child; hinged at the top edge, modelled swung open 70° up).
    - Behind the opening: a 1.0 × 1.0 duct stub, 0.35 m deep, flush with the wall back (y 0.30).
  - `Ducts/ENV_Duct_Straight_A.fbx` — 2.0 m long along X; inner 1.0 w × 1.0 h; floor at z 0 (placed at y 3.3); walls 0.06 thick; ribs every 0.5 m; pivot centre of the floor.
  - `Ducts/ENV_Duct_Corner_A.fbx` — 90° turn, 2 × 2 footprint, same section.
  - `Ducts/ENV_Duct_Riser_A.fbx` — climbs 1.5 m over 2.0 m along +Y (Blender; from the mouth stub at z 1.8 to the L2 floor at z 3.3). Floor slope 36.9°, inner height 1.0 measured vertically. Pivot = bottom inner floor at the mouth end.
  - AC dressing (`VV_MAT_FrameDark` shell, `VV_MAT_StationPurple` panels, orange flange bands).
- Export change: `export_fbx` exports the module object **and its children** (select the hierarchy). Builders may return `(mesh, {child_name: mesh})`; children are parented to the module object at their modelled positions.

- [ ] **Step 1: Add the builders and the multi-object export.** Register them in `MODULES` with sub-folders `Walls` / `Ducts`. Add the ducts and the vent mouth to the Walls render as a row behind the existing walls.
- [ ] **Step 2: Run** `blender -b --factory-startup -P Art/Blender/Scripts/vv_build_env_kit.py` (`VV_SAMPLES=64`). Expected:
  - `@@` lines for the 5 new modules, each with dims matching the Interfaces block (±0.05).
  - Riser dims ≈ (1.12, 2.0, 2.6).
  - Renders written to `Art/Blender/Environment/Kit/review/`.
- [ ] **Step 3: Look at the renders** (`ENV_Kit_Walls.png`). Check:
  - the door leaves sit flush in the frame;
  - the vent-mouth lip reads as climbable (hazard stripe, grille up);
  - the duct ends are open and line up end to end.

  Fix anything off and re-run.

### Task 6: Prefab wiring + L3 test bay scene

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/EnvironmentKitSetup.cs`
- Create: `Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.StationTestBay.cs`

**Interfaces:**
- Consumes: `SlidingDoor.Configure(...)` (Task 3), `PushableCrate` (Task 4), `PlayerClimb` (Task 2), `VentsAndVending.World.VentZone`, `CreatePlayer`, `CreateHud`, `BuildLighting`, `EnvironmentKitSetup.ConfigureAll()`.
- Produces: menu item **Vents & Vending → Prototype → Build Station Test Bay**; `public static string BuildStationTestBay()` returning `Assets/_Project/Scenes/LookDev/Station_TestBay.unity`.
  - The scene is **not** added to build settings.

- [ ] **Step 1: Prefab wiring in `MakePrefab`**, by name:
  - **`ENV_Wall_DoorSliding_*`:** add `SlidingDoor`; `Configure(leaves: [Leaf_L, Leaf_R], offsets: [(-0.68,0,0), (+0.68,0,0)])` (in the prefab's local space after import, so use the leaves' own local axes). Add a `BoxCollider` to each leaf. The wall collider becomes two jamb boxes (x ±[0.65, 1.0]) and a header (y 2.4–3.0), so the opening is passable.
  - **`ENV_Wall_VentMouth_*`:** split the colliders the same way around the 0.9 × 0.85 opening, plus a sill box (top at 1.8) the climb probe can land on. Add a trigger `VentZone` (section `"L2"`) filling the stub behind the opening.
  - **`ENV_Duct_*`:** a `MeshCollider` from the mesh (non-convex, static), plus a trigger `VentZone` (section `"L2"`) filling the inner volume. The riser's zone is a box rotated with the slope.
  - Also create a **`ENV_Crate_Pushable_A` prefab** from `Assets/ThirdParty/LowPolySciFiPropsPack/FBX/crate.fbx` (`BoxCollider` + `PushableCrate`) and a **`ENV_Crate_Fixed_A`** from the same model (`BoxCollider` only).
- [ ] **Step 2: Build the test bay** (L3 at y 0, kit pieces only, lighting as in `EnvironmentKitSetup.Lighting()`):
  - **Den:** a 6 × 6 m room (kit walls, floors, ceilings) at x[−3,3] z[−6,0], with `ENV_Wall_DoorSliding_A` in its north wall at (0,0,0) and the player spawn inside facing the door.
  - **Corridor:** a maintenance corridor 4 m wide running east from x −3 to x 13 (inner faces z 0…4, the den door opening onto it), kit walls both sides, 2 kit ceiling lights.
  - **Vent mouth A:** on the corridor's north wall at x 4, with `ENV_Crate_Fixed_A` 0.55 m in front of it (centre z 3.45).
  - **Vent mouth B:** at x 10, with `ENV_Crate_Pushable_A` at (8, 0, 2) (two pushes east and one north put it under the mouth).
  - **Ducts:** a riser from each mouth north (z 4.3 → 6.3, rising 1.8 → 3.3). Then an L2 run on the duct floor y 3.3: straights and a corner joining both risers along z 6.8, then a 6 m straight north (≈ 10–12 m of crawl from mouth A).
  - **Concourse stub:** the run ends at a grille window (an `ENV_Wall_Window_A` facing south at z 13, its sill level with the duct) into a dummy L1 room (floor y 4.6, 8 × 8 m, lit cyan/magenta, one customer placeholder capsule).
  - Room discovery isn't needed here.
- [ ] **Step 3: Run** via MCP `script-execute`: `EnvironmentKitSetup.ConfigureAll()` then `PrototypeSceneBuilder.BuildStationTestBay()`. Expected: returns the scene path; no errors in the console (`rql-query` the console for errors).
- [ ] **Step 4: Play checks** (MCP `script-execute` during Play mode; also Task 2 Step 4):
  1. Teleport the player to (0, 0, −1.5) facing north; after 1 s `SlidingDoor.OpenAmount == 1`. Teleport to (0, 0, −5); after 1.5 s `OpenAmount == 0`.
  2. Fixed crate → climb → vent mouth A climb → `Crawling` inside a `VentZone` (Task 2 Step 4).
  3. Call `PushableCrate.Interact` with the player behind it facing east: the crate moves 0.5 m east. Facing the corridor wall: the crate doesn't move and a toast shows.
  4. Teleport into the L2 duct at the far end: `VentCrawler.InVent == true` and the player can't stand (`PlayerStateController.Posture == Crawling`).
- [ ] **Step 5: Capture** four screenshots (den door, crate + vent mouth, inside the riser, duct → concourse grille) to `Docs/Art/Review/EnvKit/TestBay_*.png` with the Capture pattern from `EnvironmentKitSetup.Capture()`. Look at them.
- [ ] **Step 6: Run** `tests-run {"testMode":"EditMode"}` → all PASS. Leave everything uncommitted.

---

## Roadmap (separate plans, written when Plan 1 is accepted)

- **Plan 2: Station assembly (L3 + L2 + L1).** A new `StationBuilder` scene (one ring arc, Plaza + Lounge, 3–4 sectors) built from kit pieces. It carries over the hideout contents (VentNet desk, mixer, locker, bed), tunnel rooms and gates (IDs unchanged, layer moved), customers and the plaza (raised to +4.6, customer vents fed from L2), fan in an L2 trunk with the breaker in L3, the security systems and the save system. New kit: duct T/cross/customer grille/fan housing, garbage-chute dressing, concourse walls and storefronts, sector joints.
- **Plan 3: Basement relink + bulkheads + swap.** The L4 underpass is placed under the new L3. The crowbar grate is kept and the exit becomes the emergency stairwell to L1. Sector bulkheads (locked `SlidingDoor`s) seal every layer. The TunnelNetwork §11 and spec §7 checks run as a full play-test, then the new scene replaces `Greybox_Movement` in build settings, and then (and only then) the commit.
