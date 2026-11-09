# Visual References

The owner supplied seven reference images on 2026-11-09 as the target look for modelling and texturing. They sit alongside [ArtDirection.md](ArtDirection.md). Save the image files to `Docs/Reference/Art/` with the filenames below.

| # | File (suggested) | What it shows | Take from it |
|---|---|---|---|
| R1 | `R1_Human_Mechanic.png` | Human woman mechanic, full body, three-quarter | **Human humanoid base.** Stylised, slightly large head, natural limb lengths. Dark charcoal-purple workwear (cropped jacket over a beige tee, baggy cargo trousers tucked into boots). **Ochre/mustard** shoulder and collar stripes, light-grey reflective patches, fingerless gloves and wrist cuffs. Chunky boots with ochre toe and heel caps. A leg holster with a **cyan screen** and a wrench, a square belt buckle, and the station emblem on the chest. Painted dirt, scuffs and freckles. |
| R2 | `R2_Alien_Slug.png` | Green slug-like alien in a vest | **Non-humanoid / blob alien base.** Soft, rounded, glossy forms (*not* angular), eye stalks with heavy lids, wide toothy mouth, darker spot markings. An ochre vest with dark-brown trim, a diagonal belt with a steel buckle, chunky pouches, and a small amber-lit device. Proves that clothing and pouches can be reused across species. |
| R3 | `R3_Security_Drone.png` | Station security drone | **Replacement for the current drone.** Faceted, bevelled off-white armour panels with heavy edge chipping and a red stripe. "STATION SECURITY" text with the emblem, and a yellow/black hazard tag. A big **red** camera eye, two ring thrusters glowing red, an antenna with a red tip, an underslung spotlight on an arm casting a red cone, and a cable loop. |
| R4 | `R4_FirstPerson_Hands.png` | First-person view looking down | **Target for the player body.** Fingerless gloves with ochre and grey armour plates and chunky cuffs. A wrist device with a cyan emblem screen. Dark trousers with knee pads and grey reflective strips, boots with ochre toe caps, standing on a slotted floor grating. Warm amber light from one side. |
| R5 | `R5_Maintenance_Corridor.png` | Maintenance corridor environment | **Target for the tunnels and underpass.** Purple-grey panelled walls, ochre trim and hazard chevrons, a big "MAINTENANCE LEVEL M-2" wall sign. A hanging directional sign ("↑ VENTS · UTILITIES · RECYCLING" with icons). Warm amber tube lights with cyan accent lights, a fat pipe with ochre bands, magenta pipes behind grilles. Floor grates, puddles, steam, a hose, crates with ochre corners and cyan status lights, and a door with the emblem at the end. |
| R6 | `R6_Vent_Extract_Canister.png` | Product canister "VENT EXTRACT" | **Target for products and ingredients.** A glass cylinder of glowing purple goo with bubbles, chunky light-grey and charcoal caps, orange clips and cyan indicator strips. A worn paper label with the emblem and a barcode. |
| R7 | `R7_Prop_Kit_Sheet.png` | Prop sheet in nine categories | **Environment kit backlog:** Docking & Transport, Power & Utilities, Storage & Logistics, Safety & Security, Signage & Information, Interior Architecture, Sanitation & Crew Amenities, Lab/Processing/Product, Exterior & Station Shell. Purple-grey bodies with orange edges and wear, glowing accents. |

## Style rules distilled from the references

1. **Station emblem.** A diamond with an inset diamond, stencilled on uniforms, crates, doors, drones, labels and screens. It is the station's brand, and every faction-neutral asset can carry it.
2. **Two accent warms.** Characters' workwear uses **ochre/mustard** (about `#D9A23A`). Props, machinery and environment trims use **industrial orange** (about `#E0782A`). Both appear on the same screen, so `VV_MAT_OrangePaint` stays orange and an ochre variant is needed for clothing.
3. **Hand-painted wear.** Edges have *bright* chips (light paint showing through) and darker grime collects in recesses, together with painted scuffs and stains. That is edge-driven rather than random-noise wear, so skinned characters need a baked curvature/AO pass before export (CharacterStandards §5).
4. **Forms.** Chunky shapes with big bevels. Hard-surface pieces are faceted, while organic aliens can be very soft and rounded (R2). Heads are moderately oversized and silhouettes read at a glance.
5. **Light.** Warm amber keys, cyan and magenta accents, red for security, and dark purple-grey backgrounds (about `#2B2530`) used as the presentation backdrop.
6. **Glow is local.** Small emissive screens, strips and eyes on otherwise matte objects, never whole glowing surfaces.

## Follow-ups this implies (not yet scheduled)

- Add `VV_MAT_OchrePaint` (clothing accent) and `VV_MAT_LightArmour` (the drone's off-white panels) to the library.
- Bake an edge-wear mask (curvature plus AO) into character textures.
- The security drone, player hands and boots, corridor kit, canister and prop kit become production tasks after the vendor benchmark is approved.
