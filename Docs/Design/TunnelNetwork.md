# Tunnel Network

Expands **GDD Part 4 (The Vent Network)** and **§19.1 Location 1 (Maintenance Network)**. Where this note and the GDD disagree, the GDD wins on scope (Part 27) and this note wins on vent layout and topology.

- Source: `TunnelNetwork/Venting_and_Vending_Tunnel_Network_Blueprint.docx` (original blueprint, kept verbatim).
- Diagrams: [master network](TunnelNetwork/master_network.png), [vertical planes](TunnelNetwork/vertical_planes.png), [progression gates](TunnelNetwork/progression_gates.png).
- Status: **adopted, with the amendments in §0.** §1 onward condenses the blueprint for quick reference.

> **North star:** the player should eventually know these tunnels like the back of their hand. The network starts as a confusing, hostile maze; through exploration, repairs and shortcuts it becomes a familiar toolset and finally the infrastructure of their growing business.

---

## 0. Decisions and amendments (2026-11-08)

1. **The fan does not gate the first sale.** The nook → plaza route never requires a puzzle: **01 → 02 → 03 → 05 Plaza Access** is open from the start. The fan at **04** instead blocks the way to a **richer customer base**: 04 → 06 Drop Hatch → **07 Plaza Service Hub → 08 Customer Vent**, which overlooks the wealthier mezzanine side of the plaza (bigger budgets, pickier about quality). 16 Power Distribution is reached from 03, so the breaker → fan lesson happens when the player chooses to go after better customers. Later, the same direction grows toward GDD Location 4 (Luxury District).
2. **Tiered scope.** Following GDD §4.4, launch has two or three hazards:
   - **Slice:** 01–08, 16, 17 and one secret dead-end. Hazards: fan only.
   - **Next:** 11 Old Pipe Gallery and 12 Condensation.
   - **Later (post vertical slice):** waste (13–15), cooling and production (18–20), security (21–24), legacy (25–28), cargo and hangar (29–30).
3. **District naming.** Tunnel districts are "the vents behind location X", not locations themselves. "Luxury Plaza Network" → **Plaza Network** (it sits behind the Central Plaza). The GDD's Industrial Sector and Luxury District stay public locations; their tunnel districts come with them.
4. **Crawling only in short ducts** (about 8 m or less). Main arteries are crouch- or stand-height service corridors like the current gallery. This answers the blueprint's open decision §12.2 and keeps the "not a walking simulator" check passing.
5. **Hazards block, they don't hurt.** There is no health system. A running fan is a solid, loud obstruction with visible blades. Steam and compactors (later) push back or pause, and never kill.
6. **Verticality cost.** Ladders and lifts need a new `Climbing` movement state. One-way drops only need a trigger and a safe landing. The slice uses one drop hatch (06) and no ladders.
7. **Data conventions follow the project**, not the blueprint's snippet: static `VentRoomDefinition` / `VentGateDefinition` ScriptableObjects with stable string IDs, runtime state kept separately, discovery and gate state saved with a version bump. Gate state is owned by a component (`BreakerSwitch`, `FanHazard`), never by a door mesh. Breaker IDs follow `BRK_FAN_04` controls `FAN_17`.
8. **Mapping to what is already built** (`PrototypeSceneBuilder.Hideout.cs`):

   | Room | Current geometry |
   |---|---|
   | 01 Starting Nook | Maintenance chamber and bed nook |
   | 02 Service Junction | Exit duct and the gallery start (D1). Becomes a proper three-way junction |
   | 03 Long Crawl | Gallery D1–D2 (crouch bundle, vault pipes) |
   | 05 Plaza Access | Gallery D3 and the plaza duct |
   | Secret dead-end | Side stash duct off D3 |

9. **Blueprint inconsistencies to resolve when those rooms are built:**
   - Overlapping boxes in the master diagram: 22/23, 29/30 and 26–28.
   - The vertical-planes chart shows links missing from the room index: 10↔11, 02↔P5 and 23↔09.
   - 25 is "found from 11" in the index, but the chart ties it to 02.
   - The index takes precedence.
10. **Maintenance Underpass (U01), 2026-11-08.** See `Docs/superpowers/specs/2026-11-08-maintenance-underpass-design.md`. A drone-patrolled maintenance sublevel (P3/P4, floor y −3.6) under the gallery, reached by a **crowbar-pried floor grate in 03 The Long Crawl** (`GATE_GRATE_03`, one-way drop). It leads upright to a push-bar exit in the plaza (`GATE_EXIT_U01`, inside only, never saved open), so it is an earned, faster, riskier alternative to 03 → 05. This amends the blueprint: the first patrolled corridor sits **below** the vents rather than in the P1 Security district (21–24), which stays planned. Inside it, being seen is suspicious even empty-handed (trespass), the supply closet is a guaranteed hiding spot, and a catch escorts you out to the plaza. Home is still vents-only.

---

## 1. Design principles

- **Fixed topology, authored surprises.** No random maze generation.
- **Readable navigation.** Each district has its own palette, soundscape, materials and landmark. Junction signage uses old station labels, not a GPS overlay.
- **Loops over dead ends.** A few strong arteries, optional branches and progression shortcuts. Unlocking a route should visibly cut travel time.
- **Every obstacle teaches a system:**
  - fans teach power isolation
  - cameras teach surveillance timing
  - valves teach observation
  - conveyors teach route manipulation
- **Fair traversal.** Hazards telegraph through sound, warning lights and visible moving parts. No precision platforming and no surprise damage.
- **Home stays reachable.** Gates never trap the player away from the nook or block the core craft/sell loop.

## 2. Route types

| Type | Use | Rule |
|---|---|---|
| Main artery | Nook ↔ commercial and utility hubs | Legible, fairly safe, landmark-rich |
| Secondary route | Narrow bypasses, service ducts, cross-links | Exploration, risk/reward, route choice |
| Locked shortcut | Hatch, lift, cleared blockage, repaired mechanism | Show the destination before it can be unlocked |
| Secret route | Concealed panel, crawlspace, old conduit | Hint with environmental clues, no pixel hunting |
| Dead-end branch | Short optional pocket | Usually a payoff; a few deliberately funny and empty |

### Major loops

| Loop | Connections | Purpose |
|---|---|---|
| Core maintenance | 01 → 02 → 03 → 04 → 16 → 17 → 18 → 07 → 06 → 05 → 04 | Home, industry and plaza; later a fast circuit |
| Old utility | 02 → 11 → 12 → 13 → 14 → 18 → 17 → 16 → 04 → 03 → 02 | Old pipes, waste resources, industrial cooling |
| Security bypass | 04 → 16 → 21 → 24 → 05 → 04 | Higher-risk bypass of the exposed plaza approach |
| Cargo / retail | 18 → 29 → 30, and 29 → 09 → 07 | Late-game logistics into shop backrooms |
| Legacy return | 02 → 11 → 25 → 26 → 28 → 02 | Exploration loop and return shortcut once the shaft is repaired |

## 3. Vertical planes

Broad elevation bands, not literal floors.

| Plane | Role | Spaces | Access |
|---|---|---|---|
| P1 Upper Service | Above ceiling voids | Security, cameras, drone dock, cargo, hangar approach | Ladders, lifts, restricted hatches; high surveillance |
| P2 Public Interface | Behind shop walls and plaza ceilings | Plaza access, drop hatch, service hub, customer vent, backrooms, ad crawlspace | Grilles, drop hatches, some one-way drops |
| P3 Core Maintenance | Around the nook | 01–04, 11, 16, 17 | Basic hatches and crawls; early hub |
| P4 Lower Utility | Below the main service level | Condensation, waste, organics, cooling, production | Ladders, pipe drops, pressure doors |
| P5 Legacy / Deep | Disused infrastructure | Conduit, old station, archive, escape shaft | Hidden access, old locks, repaired ladder |

Rules:
- Every drop, ladder or lift has a recognisable silhouette and sound.
- One-way drops only onto safe landings, with a return route that's discoverable or unlocks soon after.
- Show connections between planes through grilles, glass, open shafts and sound, so a room is recognisable from above or below before the player reaches it.
- Traversal stays simple and predictable.

## 4. Districts

| District | Rooms | Look and sound | Role |
|---|---|---|---|
| A. Old Maintenance | 01–04, 11–12 (P3) | Rusty yellow hazard paint, charcoal metal, pipe insulation, patched panels, warm flickering work lights. Metal knocks, distant fan rumble, pipe ticks | Safe-ish home territory, first junction choices, first fan, condensation resource. Set pieces: nook, map terminal, repairable hatch, loose-panel secret, small resident pipe creature |
| B. Plaza Network | 05–10 (P2) | Clean white surfaces seen through grilles, cyan/violet/orange ad light, muffled crowds | First commercial district: customer access, observation, social comedy. Tempting but exposed |
| C. Food & Waste | 12–15 (P4) | Green staining, damp metal, organic growth, amber lamps. Gurgling, compactors | Resources, salvage, pressure puzzle, creatures. Compactor never a surprise |
| D. Industrial | 16–20, 29–30 | Orange warning lights, huge fans, cable trays, vibrating floors | Power distribution, fan shutdown, cooling, logistics. Power routing readable from cable colour and labels |
| E. Security | 21–24 (P1) | Cold blue, neat panels, cameras, status LEDs, rotors, servos | Stealth: patrol timing, blind spots, camera sabotage. A readable risk layer, not a constant chase |
| F. Forgotten | 25–28 (P5) | Older construction, oxidised metal, obsolete labels, pools of light, unexplained knocks | Exploration, lore, collectibles. Suggestive mystery, no lore dump |

## 5. Room index

| ID | Room | Plane | Purpose | Access |
|---|---|---|---|---|
| 01 | Starting Nook | P3 | Home, storage, crafting, upgrade anchor | Always; spawn |
| 02 | Main Service Junction | P3 | Three-way hub, signage, visible locked branches | Open; later the legacy return shaft |
| 03 | The Long Crawl | P3 | Introductory corridor with cables, labels, sound cues | Basic movement |
| 04 | Fan Junction | P3 | First major obstruction; fan visible at the far end | Fan off via breaker in 16 **(amended: gates 06–08, not 05)** |
| 05 | Plaza Access | P2 | First glimpse of the plaza through a grille | **Open from the start (amended)** |
| 06 | Plaza Drop Hatch | P2/P3 | Drop to the service side of the plaza | One-way until the return hatch in 07 |
| 07 | Plaza Service Hub | P2 | Junction behind retail premises | Via 06; later a shortcut from 18 |
| 08 | Customer Access Vent | P2 | Hand-off and observation over the **wealthier mezzanine customers** | Via 07 |
| 09 | Shop Backrooms | P2 | Delivery, stash, staff-only route | Hatch from an adjacent maintenance panel |
| 10 | Advertiser's Crawlspace | P2 | Behind holo signage, colourful light spill | Optional, from 09 |
| 11 | Old Pipe Gallery | P3 | Big chamber with overhead pipe bridges | From 02 |
| 12 | Condensation Chamber | P3/P4 | Dripping pipes, condensation resource | From 11 |
| 13 | Waste Processing | P4 | Bins, valves, compactor | From 12; pressure sequence for side branch |
| 14 | Organic Collection | P4 | Organics, creature nests | From 13 |
| 15 | Blocked Waste Branch | P4 | Collapsed dead end with salvage | Clear later with a tool |
| 16 | Power Distribution | P3/P1 | Breaker wall, power routing hub | From 03/04; panel partly locked |
| 17 | Industrial Fan Room | P3 | Fan housing and maintenance lever | Fan power from 16 |
| 18 | Cooling Ducts | P4 | Hot/cold ducts, steam vents | Thermal mitigation |
| 19 | Production Access | P4 | Hatch toward later production/storage | Credential or repaired actuator |
| 20 | Hidden Lab Route | P4/P3 | Concealed route for lab expansion | Late nook upgrade or key |
| 21 | Security Backbone | P1 | Protected corridor | Panel bypass or stolen credential |
| 22 | Camera Control | P1 | Feeds, blind-spot control | Security panel access |
| 23 | Patrol Drone Dock | P1 | Drone charging and deployment | Observable early; entry needs bypass |
| 24 | Restricted Crawl | P1/P2 | Bypasses the camera corridor | After disabling a security lock |
| 25 | Forgotten Conduit | P5 | Old conduit behind a removable panel | From 11; graffiti / old-map clue |
| 26 | Old Station | P5 | Abandoned station, lore hub | Via 25 |
| 27 | Sealed Archive | P5 | Rare records, collectible trail | Power cell + latch sequence |
| 28 | Emergency Escape Shaft | P5/P3 | Vertical return route | Repair ladder/counterweight → 02 |
| 29 | Cargo Transfer Duct | P1 | Moving belt, concealment | Logistics upgrade |
| 30 | Hangar Access | P1 | Ship progression threshold | Late game; never required early |

## 6. Gates

| Gate | Requirement | Result | Rooms |
|---|---|---|---|
| Basic maintenance hatch | Interact | Open/close, no skill check | 01–12 |
| Fan isolation | Breaker in 16 off | Fan coasts to rest, blades stay visible and safe; far-side lever later restores control | 04 ↔ 17 / 06–08 |
| Plaza service hatch | Restore local actuator | Return route 07 → 06/05 | 06–09 |
| Waste pressure sequence | Read gauges and labels, operate valves | Pressure drops, compactor pauses for a window | 13–15 |
| Security panel bypass | Credential or camera control | Restricted crawl, camera manipulation | 21–24 |
| Cooling mitigation | Reroute heat or insulated gear | Hot duct traversable; shortcut to 07 | 18 ↔ 07 |
| Emergency shaft repair | Find part, repair ladder | Return 28 → 02 | 25–28 |
| Cargo conveyor control | Restore terminal | Stop/redirect belt; route to backrooms | 29 ↔ 09 |
| Hidden lab connection | Late nook upgrade | Advanced lab route back to the nook | 20 ↔ 01 |
| Hangar security | Late-game clearance | Ship threshold | 29–30 |

## 7. Set pieces (fairness notes)

| Challenge | Where | Player action | Feedback |
|---|---|---|---|
| Fan shutdown | 04 / 16 / 17 | Trace the labelled cable to the breaker, isolate it, crawl through the housing | Rumble winds down, warning light red → amber, blades coast to rest |
| Pressure sequence | 13 / 15 | Close/open valves in a logical order from pipe markings | Gauge needle, pipe vibration, steam plume |
| Camera blind spots | 21 / 22 / 24 | Time the sweep, or disable/redirect one camera | Visible cone, servo sound, indicator light |
| Drone dock | 23 | Enter in the departure window or cut charging | Dock lights, rotor audio, readable return path |
| Cargo conveyor | 29 | Stop, reverse or divert; cargo as moving cover | Warning light and clunk before movement |
| Steam duct | 18 | Time bursts or isolate the circuit | Hiss and light pulse before each burst |
| Archive latch | 27 | Restore low voltage, align three latches from clues in 26 | Distinct click and status light per latch |

## 8. Optional discoveries

- **Employee Shrine** (off 03 or 11): broken coffee machine, old staff badges, offerings. Collectible badge.
- **The Nest** (near 12/14): creatures steal dropped trinkets; the nest grows on return visits.
- **Forgotten Vending Machine** (26): takes obsolete tokens, dispenses a useless novelty.
- **Retired Maintenance Robot** (off 25): believes its shift ends in three minutes. Gives one clue once trusted.
- **Graffiti Wall** (25): past tunnel dwellers' messages; one hints at a removable panel.
- **Tiny Kingdom** (14): a throne and settlement built from packaging; expands over time.
- **The Watcher Feed** (22): one monitor shows the player's nook from an unknown angle. Never explained.
- **Staff Meeting** (10): a grille over a painfully dull meeting.
- **Empty Chair** (off 02): a chair, a cup, faint scratching. Nothing else. Use sparingly.

## 9. Navigation language

| District | Palette and materials | Landmark | Audio |
|---|---|---|---|
| Old Maintenance | Rust, dirty yellow, charcoal, patched metal | Hazard stripe, pipe bundles, battered signs | Knocks, pipe ticks, fan rumble |
| Plaza Network | White panels, cyan/violet/orange spill, gloss | Holo ad light through grilles | Muffled shoppers, announcements |
| Food & Waste | Green staining, wet metal, amber lamps | Labelled waste pipes, gauges, compactor | Gurgling, pressure pulses |
| Industrial | Orange warning light, cable trays, hot metal | Fan silhouette, breaker labels | Machinery rumble, hum |
| Security | Cold blue, clean panels | Camera clusters, drone dock | Rotors, servos, beeps |
| Forgotten | Oxidised metal, obsolete labels | Old station logo, map plates | Low hum, relays, knocks |

- Put signage at decision points only, using old sector numbers.
- Give every major junction a unique silhouette.
- Let players see into rooms they can't reach yet, through grilles and glass.
- An optional repairable map terminal in the nook reveals explored rooms. It supplements physical landmarks and never replaces them.

## 10. Build architecture

**Greybox order:**
1. P3 core (01–04, 11, 12, 16, 17)
2. P2 plaza interface (05–10)
3. P4 (13–15, 18–20)
4. P1 (21–24, 29–30)
5. P5 (25–28)

Final art only after greybox tests pass.

**Reusable modules:**
- tunnel segments (straight, L-turn, vertical elbow, crawl, damaged)
- junctions (T, four-way, offset, vertical)
- access modules (grille, hatch, pressure door, ladder, lift, one-way drop), each with open/locked/powered states
- hazard modules, with state logic separate from geometry
- secret modules
- infrastructure controls, each showing a state and the system it affects

**System rules:**
- Discovery and unlocks persist in saves.
- Gate state is owned by an infrastructure component.
- Stable IDs throughout.
- Deterministic, signalled hazard timing.
- Generous room-discovery triggers.
- A debug overlay showing room ID, connections, plane, gate and hazard state.

## 11. First-slice acceptance checks

- A new player can find the nook again without a full-screen map.
- The player understands why the fan blocks the route and can identify its breaker.
- The fan shutdown is satisfying, readable and safe; the player knows when crawling through is safe.
- The plaza route shows the contrast between public luxury and hidden grime.
- At least one optional branch rewards curiosity.
- Unlocking the plaza return hatch creates an obvious, appreciated shortcut.
- Nook → first customer is short enough that the loop isn't a walking simulator.
- **(Amended)** The first sale never requires solving a gate.

## 12. Still open

- Exact station dimensions and number of plaza levels.
- Whether room 20 returns directly to the nook or is an expansion room beside it.
- Whether hangar access is through the tunnels or a separate loading boundary.
- Final naming for sectors, doors and maintenance signage.
