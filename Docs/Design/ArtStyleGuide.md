# Art Style Guide — "Stylized Grunge"

Source: GDD Part 15 (Art Direction & Visual Bible). This guide turns it into concrete, reusable rules.
Decision (2026-11-08): **stylized grunge** look; **procedural grime + AI-generated base textures**.
Revision (2026-11-08): pushed **slightly cartoony** — toward Schedule I / Rick and Morty readability without becoming a full
cartoon: soft cel lighting with tinted shade, ink outlines, flatter cartoon textures, a little extra saturation.

## 1. The look in one paragraph

Simple, chunky low-poly shapes carry **detailed material work**: worn paint, rust, grime and chipped edges that
read clearly from a few metres away. Wear is **bold and legible, never photoreal noise**. Lighting and colour do the
mood — the **maintenance network is dark, warm and dirty**; the **public plaza is cleaner, polished and neon**. That
contrast is the game's visual identity (GDD §28).

## 1b. What makes it "slightly cartoony"

| Lever | Setting | Where |
|---|---|---|
| Soft cel lighting | 2-tone ramp (threshold 0.05, softness 0.12), crisp shadows, gently posterised point-light pools | `GrungeTriplanar.shader` (`VV_ToonLight`) |
| Tinted shade | Shadow tint lilac `(0.8, 0.76, 0.95)`, ambient strength 2.6, scene ambient `(0.24, 0.24, 0.30)` | shader + `ArtLibrary.ApplyToonLook`, scene builders |
| Highlights | Only glossy/metal surfaces get a small banded highlight; rough paint and concrete stay flat | shader |
| Rim light | Subtle (0.18) | shader |
| Ink outlines | Screen-space, colour `#0F0D1A`, 1.4 px at 1080p, fade 18–55 m | `OutlineFullscreen.shader`, "Toon Outline" renderer feature |
| Wear | Crispness 0.75 — grime is a wavy painted band and flat blotches; rust is two flat tones | shader `_WearCrispness` |
| Textures | Cartoon-style detail maps (flat tones, clean scratches, blobby stains), normal maps at 35 % | `_source/`, `ToonNormalScale` |
| Colour grade | Saturation +18, contrast +8 | `GlobalVolumeProfile` |
| One look everywhere | Characters, props, neon and screens all use the same shader (flat colour, no wear) | `GetOrCreateMaterial` |

Re-apply the renderer/volume part with *Vents & Vending → Art → Apply Toon Rendering Setup*.
Don't go further than this without a decision: thicker outlines, hard (0-softness) ramps or fully flat textures tip it into full cartoon.

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
  `pixel=false`, 1K, medium quality; **always pass the cartoon style anchor** `8df4b81e-9215-4db1-bfb2-95308f0a451d`
  (T_PaintedMetal cartoon) in `style_asset_ids`. Prompt template:
  > *Seamless tileable square texture of …, viewed perfectly flat and head-on (orthographic). Drawn in a simplified
  > cartoon style like an animated TV show background: [a few clean scratch lines, chunky crisp-edged flecks, two or
  > three big blobby stains]. Flat even lighting, no photographic noise or grain, limited tones. Neutral desaturated
  > mid-grey (no dominant colour). Tiles seamlessly on all sides. No text, no borders, no seams.*
  Style: *"stylized cartoon game texture, hand-drawn animated-series look, simplified shapes, clean readable wear,
  not photoreal, not painterly brushwork"*. The earlier realistic set is archived in `_source_realistic/`.
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
