# Art Style Guide — "Stylized Grunge"

Source: GDD Part 15 (Art Direction & Visual Bible). This guide turns it into concrete, reusable rules.
Decision (2026-11-08): **stylized grunge** look; **procedural grime + AI-generated base textures**.

## 1. The look in one paragraph

Simple, chunky low-poly shapes carry **detailed material work**: worn paint, rust, grime and chipped edges that
read clearly from a few metres away. Wear is **bold and legible, never photoreal noise**. Lighting and colour do the
mood — the **maintenance network is dark, warm and dirty**; the **public plaza is cleaner, polished and neon**. That
contrast is the game's visual identity (GDD §28).

## 2. Palettes (GDD §15.4)

Material base colours (linear-ish sRGB, as set in `ArtLibrary.cs`):

| Area | Role | Colour | Used by |
|---|---|---|---|
| Maintenance | Industrial blue-grey | `#3D4D5C` (0.24, 0.30, 0.36) | Walls — `M_PaintedMetal_Wall` |
| Maintenance | Dark grey | `#333331` → `#575754` | Concrete floor, floor plate |
| Maintenance | Rust brown | `#704F38` / `#5C3D2B` | Ducts, rusty metal |
| Maintenance | Dirty yellow | `#A88A29` | Hazard paint, trims, hatch frames |
| Maintenance | Muted orange | `#A35C29` | Workshop props, lockers |
| Maintenance | Desaturated green | `#4A5E4A` | Secondary panels, tunnels |
| Plaza | Deep violet panel | `#4D3D73` | Walls — `M_PlazaPanel` |
| Plaza | Cool polished grey | `#40424F` | Floor — `M_PlazaTile` |
| Plaza | Metallic silver | `#6B707A` | Counters, benches — `M_BrushedSilver` |
| Plaza | Neon cyan / magenta / warm orange | emissive | Trim, signage, holograms |
| Products | Each product owns one colour (Glarb-Juice = neon green) | emissive | Liquids, packaging |

Rules: keep large surfaces **desaturated**; save saturated colour for **light sources, products, signage and
characters** so they pop. Never use pure black or pure white on surfaces.

## 3. Materials

All surfaces use **`VentsAndVending/Grunge Triplanar`** (`Assets/_Project/Art/Shaders/GrungeTriplanar.shader`):

- **World-space triplanar mapping** — no UVs needed; scaled primitives and modular pieces never stretch.
- **Base texture** = neutral grey detail map (mean 0.5) × **base colour** → one texture serves many palettes.
- **Procedural layers** (per-material sliders): panel seams, floor-level grime + patchy dirt, rust patches
  (biased low and to edges), chipped paint on box edges (bare metal shows through).

The library lives in `Assets/_Project/Art/Materials/Library/` and is defined in code (`Scripts/Editor/ArtLibrary.cs`).
**Tune presets there**, then run *Vents & Vending → Art → Update Material Library*. The level builder maps greybox
surface names to library materials (`LibraryRemap` in `PrototypeSceneBuilder.cs`).

| Material | Character |
|---|---|
| `M_PaintedMetal_Wall` | Blue-grey painted panels, 1.25 m seams, heavy floor grime, chipped edges, light rust |
| `M_FloorPlate` | Diamond tread steel, dirt patches |
| `M_Concrete_Dirty` | 3 m slabs, stains, heavy grime |
| `M_DuctMetal` / `M_RustyMetal` | Brown painted metal → heavily rusted |
| `M_HazardPaint` / `M_WorkshopOrange` / `M_GreenPanel` | Painted accents, strong edge wear |
| `M_DarkMetal` | Machinery bodies, drone shells |
| `M_PlazaTile` | Polished terrazzo, 1 m tiles, almost no grime |
| `M_PlazaPanel` | Clean violet cladding, 2 m panels |
| `M_BrushedSilver` | Counters, benches, fittings |

Emissive materials (neon, screens, liquids, lamps) and characters stay on **URP Lit** for now.

### Wear budget

| Area | Grime | Rust | Edge wear | Seams |
|---|---|---|---|---|
| Vents & hideout | High | Medium | High | Strong |
| Industrial (future) | Medium-high | Medium | Medium | Strong |
| Plaza | Very low | None | Very low | Soft |
| Luxury (future) | None | None | None | Fine |

## 4. Textures

- **Texel density:** base textures tile every **2 m** (concrete 3 m) at 1024 px → ~512 px/m. Don't exceed 1024 px
  for tiling surfaces; props may use 512 px.
- **Authoring:** tileable, **neutral grey**, flat-lit, head-on, no baked shadows/perspective, no text.
  Process with *Vents & Vending → Art → Process Source Textures* (desaturate, mean-normalise, derive normal map).
- **Generating with SpriteCook:** project "Vents & Vending"; model `gpt-image-2.5-flare`, `mode=texture`,
  `pixel=false`, 1K, medium quality; **always pass the style anchor** `f09cb59e-9306-4587-92fb-d2bd684f5bf7`
  (T_PaintedMetal) in `style_asset_ids`. Prompt template:
  > *Seamless tileable square texture of …, viewed perfectly flat and head-on (orthographic). [wear details].
  > Even, flat, shadowless lighting. Neutral desaturated mid-grey (no dominant colour), medium contrast.
  > Tiles seamlessly on all sides. No text, no borders, no seams.*
  Style: *"stylized grunge game texture for a low-poly sci-fi game, clean readable wear, not photoreal, not painterly"*.
  Record every asset ID in `spritecook-assets.json`.

## 5. Geometry (GDD §15.2)

Angular silhouettes, chunky props, exaggerated proportions. Bevel nothing smaller than ~2 cm; let the shader's edge
wear sell the edges. Detail where the player looks (eye height, work surfaces); keep ceilings and far walls simple.

## 6. Lighting (GDD §15.5)

- Maintenance: dim warm key, cold low fill, **pools of light** from work lamps; darkness is a feature.
- Plaza: coloured neon point lights (cyan, magenta, violet, amber) plus a soft cool fill; brighter overall.
- Emissive strips mark paths, hatches and points of interest. Bloom + tonemapping via `GlobalVolumeProfile`.
- Realtime shadows only from the key light and a few hero lights.

## 7. Characters

Readable silhouettes and exaggerated heads first (Blorb = jelly blob with three eyes; Gromflomite = insect clerk
with an oversized badge). Each species owns a colour family; keep them more saturated than their surroundings.

## 8. Review process

Build the look-dev scene (*Vents & Vending → Art → Build Look-Dev Scene*) — swatches of every material under both
maintenance and plaza lighting. Check new materials there **before** using them in levels.
