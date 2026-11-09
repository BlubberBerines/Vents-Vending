# Art Direction — Vents & Vending

The production brief below (Parts 1–6) is the authoritative art direction, supplied by the project owner on 2026-11-09 and kept verbatim. Section 0 records the project decisions that adapt it to this repository. Where they conflict, Section 0 wins.

## 0. Project amendments (2026-11-09)

1. **Name.** The game is *Vents & Vending*. The brief's "Venting & Vending" refers to the same project.
2. **Asset locations.** First-party content stays under `Assets/_Project/` (see CLAUDE.md):
   - Unity side: `Assets/_Project/Art/Characters/AlienVendor/{Models,Materials,Textures,Animations,Prefabs}/`. The prefab is `NPC_Alien_Vendor_001.prefab`.
   - Blender sources: `Art/Blender/` at the repo root, *outside* `Assets/`, so Unity never auto-imports `.blend` files. The vendor lives in `Art/Blender/Characters/AlienVendor/`, the mannequin in `Art/Blender/Base/` and the shared material library in `Art/Blender/Library/`. Preview renders go in a `Renders/` folder next to each `.blend`.
   - Versioned saves (`_v001`, `_v002` …) serve as the brief's "backups before major changes". Blender's `.blend1` autosave backups are git-ignored.
3. **UI keeps the approved Ink & Hazard theme.** That means near-black ink, hazard yellow, cyan credits and green/red states. The brief's palette (§1.4) applies to the world, characters and props. Revisit if the two clash on screen.
4. **Shading bridge.** In Unity, world surfaces use the project's stylised shader `VentsAndVending/Grunge Triplanar` plus a fullscreen outline. FBX does not carry Blender shader graphs, so:
   - Every `VV_MAT_*` material in Blender has a same-named Unity material. The importer remaps by name, and the Unity material holds the real look.
   - Skinned characters cannot rely on world-space triplanar grunge, because it would swim as they move. Their colour variation and wear go into painted or baked textures.
   - Blender renders are for approval, not a pixel match. The Unity test scene is the final check.
5. **Tooling.** Blender 5.2 with the `blender-mcp` server (registered in `.mcp.json` as `blender`). The asset-generation add-ons (Poly Haven, Hyper3D, Sketchfab and similar) stay **off**, because assets are modelled here rather than sourced from marketplaces (brief §1.1).
6. **Stage gates are respected.** Each stage ends with renders and a report, and the next stage starts only after the owner approves.

---

# VENTING & VENDING
# ART DIRECTION & FIRST CHARACTER PRODUCTION TASK

**Document type:** AI Production Task Brief
**Project:** Venting & Vending
**Asset ID:** NPC-ALIEN-VENDOR-001
**First production target:** Purple Alien Shop Vendor
**Primary tools:** Claude Code, Blender, Blender MCP and Unity
**Rendering pipeline:** Unity URP

---

# PART 1 — PROJECT ART DIRECTION

## 1.1 Overall visual identity

Venting & Vending is a stylised, low-poly-inspired, first-person sci-fi management game.

Its visual identity combines:

- Exaggerated, chunky 3D forms.
- Angular geometry with softened bevels.
- Strong, immediately recognisable silhouettes.
- Saturated but controlled colours.
- Worn industrial surfaces.
- Improvised equipment and believable environmental clutter.
- Atmospheric lighting and contrasting emissive accents.
- Cartoon-like character proportions within a detailed industrial world.

The game should feel like a strange, colourful, grimy and inhabited space station.

It should not look photorealistic, overly clean, generic or like a collection of unrelated marketplace assets.

## 1.2 Core art direction principle

**The station was professionally manufactured. Its inhabitants have worn it down, repaired it, modified it and made it their own.**

This principle should influence every asset.

The station's infrastructure should feel standardised and industrial.

The player's equipment should feel improvised.

The alien population should feel colourful, strange and expressive.

Illegal production equipment should feel unusual, functional and slightly questionable.

Personal possessions should communicate personality and humour.

## 1.3 Visual categories

### Environment

- Modular industrial architecture.
- Chunky wall and floor panels.
- Exposed pipes, cables and ventilation.
- Large mechanical infrastructure.
- Worn paint and damaged surfaces.
- Strong environmental silhouettes.
- Distinct visual identities between station districts.

### Characters

- Oversized heads and expressive faces.
- Exaggerated body proportions.
- Distinct species silhouettes.
- Simple, readable facial features.
- Chunky clothing and accessories.
- Strong colour separation.
- Slightly imperfect and unusual anatomy.

### Machinery and props

- Separate, readable components.
- Bevelled edges.
- Visible bolts, seams and panels.
- Physical switches, displays and handles.
- Exposed pipes and cables.
- Scratched and worn materials.
- Functional-looking construction.

### Products and ingredients

- Floating, isolated visual presentation for catalogue references.
- Exaggerated organic and mineral shapes.
- Bold saturated colours.
- Angular low-poly-inspired geometry.
- Distinctive silhouettes.
- Stylised glow and unusual surface properties.
- Easily recognisable inventory-scale appearance.

### Tools and weapons

- Chunky, industrial construction.
- Improvised modifications.
- Distinctive colour accents.
- Visible mechanical components.
- Believable first-person proportions.
- Consistent materials and wear.

### User interface

- Industrial station technology.
- Dark purple-grey foundations.
- Orange and cyan indicators.
- Clear readable typography.
- Battered terminals and physical controls.
- Functional rather than decorative presentation.

## 1.4 Colour palette

Use this as the starting colour system:

| Colour | Purpose |
|---|---|
| Dark purple-grey | Primary structural materials |
| Industrial orange | Armour, machinery and hazard accents |
| Charcoal | Mechanical components and clothing |
| Cyan | Technology, displays and indicators |
| Magenta | Alien products and special effects |
| Muted beige | Fabric, packaging and aged surfaces |
| Red | Security warnings and emergencies |

These colours are a shared visual language, not rigid restrictions. Each district and species can have its own identity.

## 1.5 Geometry and modelling rules

- Prefer chunky, readable forms over intricate microgeometry.
- Use bevelled edges on hard-surface objects.
- Avoid excessively smooth, generic shapes.
- Use intentional asymmetry where appropriate.
- Keep silhouettes readable from normal gameplay distances.
- Use separate components for moving or interchangeable parts.
- Use modular geometry for environments.
- Avoid unnecessary polygon density.
- Do not confuse low polygon count with poor topology.

## 1.6 Material rules

Create a reusable material library.

Initial materials:

- VV_MAT_IndustrialMetal
- VV_MAT_WornPaint
- VV_MAT_OrangePaint
- VV_MAT_DarkMetal
- VV_MAT_CharcoalFabric
- VV_MAT_WornFabric
- VV_MAT_AlienSkin
- VV_MAT_Glass
- VV_MAT_EmissiveCyan
- VV_MAT_EmissiveMagenta
- VV_MAT_EmissiveRed

Materials should have appropriate roughness, colour variation and wear.

Avoid making everything equally glossy, metallic or emissive.

## 1.7 Lighting

Use lighting to reinforce the game's atmosphere.

- Strong directional lighting where appropriate.
- Coloured point lights.
- Soft but readable shadows.
- Controlled bloom.
- Cyan and magenta technology lighting.
- Amber industrial lighting.
- Red emergency lighting.
- Dark corners and atmospheric contrast.

Characters should remain readable against their environments.

---

# PART 2 — FIRST PRODUCTION CHARACTER

## 2.1 Asset overview

**Asset name:** NPC_Alien_Vendor_001

**Character role:** Commercial district shopkeeper.

**Personality:** Friendly, slightly suspicious, mischievous and commercially minded.

**Design objective:** Create a memorable alien merchant who looks like a believable inhabitant of the Venting & Vending universe.

## 2.2 Visual specification

### Body

- Approximately 1.7–1.9 metres tall.
- Oversized head.
- Broad upper torso.
- Slightly exaggerated arms.
- Large hands.
- Chunky legs and boots.
- Distinctive overall silhouette.

### Head

- Purple skin.
- Large yellow-orange eyes.
- Expressive eyebrows or eyelids.
- Wide, slightly mischievous mouth.
- Several thick, dangling head tendrils.
- Subtle darker purple markings.

### Clothing

- Dark purple-grey workwear.
- Worn beige merchant apron.
- Orange shoulder protection.
- Utility belt.
- Small storage pouches.
- Chunky boots.
- Dark gloves.

### Accessories

- Cyan illuminated chest device.
- Small product vials.
- Shopkeeping accessories.
- Belt-mounted equipment.
- Small identifying shop emblem.

### Character personality

The character should be recognisable even without clothing or accessories.

Its face should support several expressions:

- Friendly.
- Talking.
- Suspicious.
- Surprised.
- Pleased.

---

# PART 3 — PRODUCTION WORKFLOW

## Stage 1: Environment and project preparation

**Objective:** Prepare a controlled production workspace.

Tasks:

1. Verify Claude Code and Blender MCP connectivity.
2. Inspect the active Blender scene.
3. Create the character project directories.
4. Establish consistent naming conventions.
5. Create the neutral character presentation scene.
6. Establish the shared materials library.
7. Create the character standards documentation.

Deliverables:

- Working Blender project.
- Character folder structure.
- Art direction document.
- Material library.
- Presentation scene.

Approval gate: Do not begin character modelling until the workspace is confirmed.

## Stage 2: Base mannequin

**Objective:** Create the character's underlying body.

Tasks:

1. Create a neutral humanoid mannequin.
2. Establish body proportions.
3. Create head, torso, pelvis and limbs.
4. Establish hand and foot proportions.
5. Check front, side and rear silhouettes.
6. Check symmetry and scale.
7. Save a separate mannequin file.

Deliverables:

- VV_Human_Base_001.blend
- Front render.
- Side render.
- Rear render.
- Three-quarter render.

Approval gate: Approve proportions before adding character details.

## Stage 3: Character modelling

**Objective:** Transform the mannequin into the alien vendor.

Tasks:

1. Create the oversized alien head.
2. Create facial geometry.
3. Add eyes and mouth.
4. Create separate head tendrils.
5. Create clothing geometry.
6. Add apron and shoulder protection.
7. Add boots and gloves.
8. Add belt, pouches and accessories.
9. Add product vials and chest device.
10. Check intersections and silhouette.

Use separate named objects for major components.

Suggested object naming:

- VENDOR_Body
- VENDOR_Head
- VENDOR_Eyes
- VENDOR_Mouth
- VENDOR_Tendril_01
- VENDOR_Tendril_02
- VENDOR_Apron
- VENDOR_Jacket
- VENDOR_ShoulderPads
- VENDOR_Gloves
- VENDOR_Boots
- VENDOR_Belt
- VENDOR_Accessories

Deliverables:

- Complete static character model.
- Front, side, rear and three-quarter renders.
- Facial close-up.
- Separate component organisation.

Approval gate: Approve the static model before rigging.

## Stage 4: Materials and visual polish

**Objective:** Match the approved game art direction.

Tasks:

1. Apply purple alien skin material.
2. Add subtle skin colour variation.
3. Apply dark clothing materials.
4. Apply worn beige apron material.
5. Apply orange painted metal.
6. Apply dark mechanical metal.
7. Add cyan emissive elements.
8. Add surface wear and scratches.
9. Check roughness and reflections.
10. Render the character under approved lighting.

Deliverables:

- Completed material assignments.
- Reusable material references.
- Final character presentation renders.

Approval gate: Approve appearance before rigging.

## Stage 5: Rigging

**Objective:** Create a working character skeleton.

Tasks:

1. Create a humanoid armature.
2. Add hips, spine, chest, neck and head bones.
3. Add arm and leg chains.
4. Add hand and foot bones.
5. Add supplementary tendril bones.
6. Bind the character mesh.
7. Weight-paint and correct deformations.
8. Check clothing deformation.
9. Test shoulders, elbows, knees and hips.
10. Verify Unity Humanoid compatibility where possible.

Deliverables:

- Rigged Blender character.
- Working armature.
- Tested deformation poses.
- Rig validation renders.

Approval gate: Approve rigging before animation.

## Stage 6: Animation

**Objective:** Give the vendor basic personality and movement.

Create these initial animation clips:

1. Idle.
2. Look around.
3. Talking.
4. Happy reaction.
5. Suspicious reaction.
6. Handing over an item.
7. Walking.

Animation principles:

- Slightly exaggerated movement.
- Readable poses.
- Subtle idle movement.
- Gentle head and tendril movement.
- No unnecessary realism.
- No excessive animation complexity.

Deliverables:

- Individual animation clips.
- Looping idle animation.
- Tested walking animation.
- Animation preview renders.

## Stage 7: Unity integration

**Objective:** Turn the character into a usable game asset.

Tasks:

1. Export character as FBX.
2. Export associated textures.
3. Import into Unity.
4. Configure the Humanoid rig if compatible.
5. Configure Avatar.
6. Create Animator Controller.
7. Assign animation clips.
8. Add appropriate collider.
9. Create reusable prefab.
10. Place character in a test scene.
11. Check scale, orientation and materials.
12. Test animations.

Unity asset structure (amended, see §0.2): Assets/_Project/Art/Characters/AlienVendor/

- Models/
- Materials/
- Textures/
- Animations/
- Prefabs/

Final prefab:

NPC_Alien_Vendor_001.prefab

Final deliverables:

- NPC_Alien_Vendor_001.blend
- NPC_Alien_Vendor_001.fbx
- Textures.
- Materials.
- Animation clips.
- Animator Controller.
- Unity prefab.
- Final preview renders.
- Production notes.

---

# PART 4 — TECHNICAL STANDARDS

## Software

- Blender for modelling, materials and rigging.
- Claude Code for AI-assisted production.
- Blender MCP for scene interaction.
- Unity URP for game integration.
- FBX for model interchange.

## Scale

- 1 Blender unit = 1 metre.
- Human-sized characters approximately 1.8 metres.
- Apply transforms before export where appropriate.
- Use consistent forward direction and orientation.
- Ensure sensible origins and pivots.

## Geometry

For the first vendor, aim for approximately 4,000–8,000 triangles as an initial target, excluding any unusually detailed optional components.

This is a starting budget, not a hard limit.

Prioritise:
- Silhouette.
- Clean topology.
- Correct deformation.
- Readable details.
- Efficient geometry.

## Character reuse

Once approved, the vendor's workflow should inform three reusable character foundations:

1. Human humanoid.
2. Humanoid alien.
3. Non-humanoid creature.

Do not force every species to share the same body proportions or skeleton.

Reuse materials, clothing components, accessories and animations where appropriate.

---

# PART 5 — AI AGENT WORKING RULES

Claude Code must follow these rules throughout production.

1. Inspect the existing project before making changes.
2. Read the art direction document before creating assets.
3. Work in clearly defined stages.
4. Create backups before major changes.
5. Never delete unrelated project objects.
6. Never overwrite approved assets without permission.
7. Use descriptive object and file names.
8. Save working files at every major milestone.
9. Render previews for human approval.
10. Report problems rather than hiding them.
11. Test exported assets in Unity.
12. Do not declare an asset complete until its acceptance criteria are met.

## Iteration process

For every stage:

1. Read the task.
2. Inspect the current asset.
3. Make the requested changes.
4. Save the working file.
5. Render a preview.
6. Report completed work and outstanding issues.
7. Wait for approval before advancing to the next major stage.

The AI agent should not independently change the approved art direction.

---

# PART 6 — DEFINITION OF DONE

NPC_Alien_Vendor_001 is complete when:

- The model matches the approved visual reference.
- The silhouette is recognisable.
- The geometry is clean and efficient.
- Materials match the game art direction.
- The skeleton works.
- Facial and tendril details are functional.
- Basic animations work.
- FBX export is successful.
- Unity imports without missing assets.
- The Animator Controller works.
- The character prefab functions in a test scene.
- All project files are organised and documented.

**Final production objective:**

Create one high-quality, reusable alien shopkeeper that establishes the modelling, material, rigging, animation and Unity integration standards for the entire Venting & Vending character library.

This first character is the benchmark against which future characters will be evaluated.
