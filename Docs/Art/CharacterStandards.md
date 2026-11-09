# Character Production Standards

These are the working standards for every character built in Blender for *Vents & Vending*. Art direction comes from [ArtDirection.md](ArtDirection.md), and its §0 amendments take precedence. This file covers the *how*.

## 1. Files and folders

| What | Where |
|---|---|
| Shared material library | `Art/Blender/Library/VV_Materials.blend` (11 `VV_MAT_*` materials, fake-user) |
| Presentation scene | `Art/Blender/Library/VV_PresentationScene.blend` (scene `VV_Presentation`) |
| Base bodies | `Art/Blender/Base/VV_Human_Base_001.blend`, etc. |
| A character | `Art/Blender/Characters/<Name>/<AssetName>_v###.blend` |
| Preview renders | a `Renders/` folder next to the `.blend` |
| Unity import | `Assets/_Project/Art/Characters/<Name>/{Models,Materials,Textures,Animations,Prefabs}/` |

- **Versioning.** Save `_v001`, `_v002` … at every milestone and never overwrite an approved version. The approved file is also copied to the plain name (for example `NPC_Alien_Vendor_001.blend`) when its stage gate passes.
- **Backups.** Blender's `.blend1` auto-backups are git-ignored. `.blend` and `.fbx` files go through Git LFS.

## 2. Naming

| Thing | Pattern | Example |
|---|---|---|
| Asset | `<Role>_<Species/Kind>_<Variant>_###` | `NPC_Alien_Vendor_001` |
| Mesh object | `<PREFIX>_<Part>[_##]`, upper-case prefix per character | `VENDOR_Head`, `VENDOR_Tendril_02` |
| Shared material | `VV_MAT_<Name>` | `VV_MAT_AlienSkin` |
| Character-only material | `<PREFIX>_MAT_<Name>` | `VENDOR_MAT_Eyes` |
| Texture | `T_<Asset>_<Map>` with Map ∈ BaseColor, Normal, ORM, Emission, Mask | `T_NPC_Alien_Vendor_001_BaseColor.png` |
| Armature | `<PREFIX>_Rig` | `VENDOR_Rig` |
| Bones | Unity-Humanoid-friendly English names, with a side suffix `.L` / `.R` | `Hips`, `Spine`, `Chest`, `Neck`, `Head`, `UpperArm.L`, `LowerArm.L`, `Hand.L`, `UpperLeg.R`, `Foot.R`, `Toes.R`, `Tendril_01_A` |
| Shape keys | `EXP_<Expression>`, `MTH_<Shape>` | `EXP_Suspicious`, `MTH_Open` |
| Animation clip | `<PREFIX>_<Action>` | `VENDOR_Idle`, `VENDOR_HandOver` |
| Presentation helpers | `PRES_*`, `CAM_*`, `LGT_*`, `REF_*`, `SW_*` | never exported |

## 3. Scale, orientation and pivots

- 1 Blender unit = 1 m, with Metric units at unit scale 1.0.
- Humans are about 1.80 m tall. The alien vendor is 1.7–1.9 m including the head, but not the tendrils.
- Characters **face −Y** in Blender (the Front view, numpad 1, looks at their face). Up is +Z.
- Feet rest on Z = 0. The object origin sits at the world origin, between the feet.
- Apply rotation and scale before rigging and before export. Never leave negative scales.

## 4. Geometry

- The budget for the first vendor is **4,000–8,000 triangles**. Optional detail pieces are counted separately and reported.
- Use quad-dominant topology on anything that deforms, with edge loops at the shoulders, elbows, wrists, hips, knees, ankles, neck and around the eyes and mouth. Rigid accessories can be any clean topology.
- Use separate objects for interchangeable or moving parts: tendrils, apron, pads, belt, pouches and accessories.
- Hard surfaces get chunky bevels, using a Bevel modifier (applied before export) or modelled ones. Avoid bevelling small details into large polygon counts.
- Check for non-manifold edges, loose vertices and interpenetration before every gate.

## 5. Materials and textures

- Assign shared `VV_MAT_*` materials wherever they fit, and keep character-only materials to a minimum.
- The Blender materials are procedural. Before export, colour variation and wear on **skinned** meshes are baked to `T_*_BaseColor`, plus `T_*_Emission` where needed (see ArtDirection §0.4). Rigid props may rely on the Unity-side shader instead.
- Use UV channel 1 for baked textures, with no overlapping UVs on baked parts. Mirrored UVs are allowed only on parts with no unique detail.
- Texel density is about 512 px/m on the body and 1024 px/m on the face. Texture sizes are powers of two, 2048 at most for a hero character.

## 6. Rigging and animation (Stages 5–6)

- Use one armature per character. The core chain follows Unity Humanoid (Hips → Spine → Chest → Neck → Head, plus arms, legs, hands and feet). Tendrils and accessories use extra bones that Humanoid ignores and that the Generic or extra-transform setup animates.
- Use no IK in exported data. Bake animation to deform bones before export.
- Clips run at 30 fps. Loops have matching first and last frames. Walking is in place, with root motion off unless agreed.

## 7. FBX export (Stage 7)

| Setting | Value |
|---|---|
| Selection | Only the character's mesh objects and armature (no `PRES_` / `CAM_` / `LGT_` / `REF_` / `SW_`) |
| Apply Scalings | FBX Units Scale |
| Forward / Up | −Z Forward, Y Up |
| Apply Transform | on |
| Armature | Add Leaf Bones off, Primary Y, Secondary X |
| Mesh | Smoothing by Face, Apply Modifiers on, Tangent Space on |
| Animation | Baked, NLA strips / all actions as separate takes |
| Materials | Names only; textures exported separately into `Textures/` |

In Unity:
- Import with scale factor 1.
- Use Humanoid when the core chain maps cleanly, otherwise Generic.
- Use material remapping **by name** onto existing `VV_MAT_*` / `<PREFIX>_MAT_*` Unity materials.

## 8. Presentation and approval renders

Open `VV_PresentationScene.blend` and append or link the character into collection `VV_Reference`, parented to `PRES_Pivot`. Render through Render → Render Image, or the scripted helper, with these views:

| View | Camera | Pivot yaw |
|---|---|---|
| Front | `CAM_Turnaround` | 0° |
| Side | `CAM_Turnaround` | 90° |
| Rear | `CAM_Turnaround` | 180° |
| Three-quarter | `CAM_ThreeQuarter` | 0° |
| Face close-up | `CAM_FaceCloseup` | 0° |
| Material swatches | `CAM_Swatches` | collection `VV_Swatches` shown, 1600×900 |

Rotating the subject rather than moving cameras keeps the lighting identical across views. The rig uses:
- an amber key light
- a cool fill light
- a cyan rim light and a magenta rim light
- a dark purple-grey world
- EEVEE, with colour management set to Standard
- 1080×1350 for character shots

The 1.8 m height ruler (with a cyan cap) stands beside the turntable.

## 9. Stage gate checklist

Before asking for approval at any stage:
1. Save a new `_v###` file.
2. Render the stage's required views into `Renders/`.
3. Report the triangle count, object list, known issues and any deviations from the brief.
4. Wait for owner approval before starting the next stage.
