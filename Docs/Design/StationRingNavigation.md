# Station ring: navigable first-area assembly

This adapts [TunnelNetwork](TunnelNetwork.md) and the approved
[four-layer plan](../superpowers/specs/2026-11-09-station-ring-layers-design.md)
to the current complete-ring review scene. Existing room and gate IDs are retained;
the original Cartesian blueprint is not copied over the new ring.

## Implemented topology

### Ring logistics and distant core revision (current)

The core no longer branches from Supply Store. Its authored entrance is behind
the deep stores of the 242-degree Tool Store (persistent ID MAINT_ROOM_270),
at room-local (-1,0,-14). The existing winding descent/core chamber are rotated
and translated as one sealed branch behind that opening. Placement is fixed
across saves/rebuilds; it does not reshuffle the player's learned map. Deposit
IDs and the refinery save ID are unchanged. Supply Store is enclosed again and
its graph connection to the core is removed. The core graph now links to Tool
Store. The shortest maintenance-ring walk from the Den approach is about 164 m
before entering the room/descending branch, rather than a nearby cupboard trip.

The Den bed rotates 180 degrees only in the production scene. The refinery sits
at its actual foot end (opposite the pillow), Den-local (-2.3,0,-4.7), facing the
aisle. The corner crate stack is reduced/repositioned to clear that workspace.

Station PC Supplies now uses paid, queued physical deliveries, not the old
instant cupboard purchase path (retained only for the legacy greybox scene).
The marked bay is on the maintenance ring outside the Den culdesac, at radius
73.55 / angle 15 degrees. E opens its saved 24-slot collection crate; click a
parcel row to move it into pockets, then close normally. Goods do not enter the
Den cupboard automatically. All station supply listings, including the crowbar,
use this route. Starter cupboard stock is retained.

One wheeled courier parks at angle 45 and drives the 30-degree arc at radius
72.4 to the bay. A fresh run waits 25 s, travels for 18 s and unloads for 3 s
(nominal first delivery 46 s; PC advertises a 43-55 s window). Wheels roll and
cargo moves to the crate during unloading. The courier returns before another
dispatch. Purchases after departure join the following run with its longer
displayed window. Timers use game time and stop while paused. Traffic causes
the courier to wait; a full bay preserves the paid manifest until space clears.
No refused order charges credits. Space is reserved for the complete mixed-item
manifest, with at most 12 outstanding units.

Save version 7 includes courier phase, remaining route time and paid manifest,
while the collection crate uses the standard inventory save ID
station.delivery_bay. Restoring mid-transit does not deliver twice or restart
the charge. Unavailable saved goods are refunded. PC status updates do not
rebuild purchase buttons every second.

Validation: 252/252 EditMode tests passed, including 17 new delivery queue tests.
Live PC purchase charged 4 CC with zero instant stock in either crate/cupboard;
the courier drove, resumed a mid-route save, unloaded once, returned, and the
parcel was collected via the actual inventory row interaction. A movement-input
bot walked the maintenance ring from 12.5 degrees through 0 to 242, entered the
Tool Store and traversed its internal doors/new descent into the core chamber.
QA saves remained isolated under Logs/CourierReview/Saves.

### Material sourcing / first gathering loop

The Den PC already supports **Supplies**: Glarb Extract (5 CC), Lumina Gel
(4 CC), Stabiliser Gel (3 CC), and the crowbar (60 CC). Ingredient purchases
deliver one unit directly to the saved Den cupboard; refused purchases do not
charge credits. The starter cupboard stock remains unchanged for now.

The free alternative branches through the existing Supply Store door just
outside the Den. A second manual door at service-frame (10,0,-4) leads down a
sealed, winding 2.6 m corridor to a chamber at (24,-1.8,-23). Its corrupted
auxiliary reactor has six mineable growths, plus fused decorative growth on the
vessel. The reactor collider is solid; the surrounding floor is walkable.

Take the spare pickaxe beside the chamber entrance with E. Carrying it enables
three E strikes on a vein, separated by a 0.65 s strike cooldown. Harvesting
awards 3 Raw Core Crystals and shrinks the growth until it regrows after 180 s
of game time. A full inventory preserves the final strike/harvest. Pickaxes are
reusable and the loaner rack can replace a lost tool without duplicating one
already carried. Raw crystals are not valid mixer ingredients.

The compact Den Crystal Refinery stands beside the central aisle at Den-local
(1.05,0,-3.2), facing into the room. E loads 3 carried raw crystals; processing
takes 8 s, then E collects 2 Lumina Gel. A full inventory leaves the batch ready
in the machine. The player can walk away while it runs. Its live status panel
shows input/output instructions and remaining time. The gel is the existing
lumina_gel item used by the Glarb-Juice recipe, not a parallel crafting economy.

Save version 6 records all deposit strike progress/regrowth timers and refinery
batches, along with tool/material inventory. Old saves migrate to empty resource
state. Pausing freezes processing/regrowth; time offline does not advance them.

Live verification used an isolated Logs/CoreLoopReview/Saves directory: bought
gel through the PC Supplies Buy button (4 CC deducted, cupboard +1); movement
input bot traversed every bend and both doors in both directions; injected E
input acquired the axe, harvested a vein, started and collected a refinery batch.
A save/load resumed the machine at the identical remaining time, and its gel
was loaded into the existing mixer to produce one Glarb-Juice. All 50 sampled
access-route positions had floor and ceiling coverage. Six unique deposit save
IDs are present. Sixteen new resource-state tests cover tool requirements, full
inventory safety, cooldown/regrowth, input/output atomicity and save migration.
The complete EditMode suite passed: 235 tests, no failures or skips.

### Visual refinement pass

Corrected five locker placements that penetrated side walls by approximately
0.11 m, moved the shared desk/cart placement inward, and separated the lockers
from rear partitions. All 15 side-room prop meshes now fit inside their authored
room footprints. Added continuous foot seals, full-footprint ceiling kit coverage
for annexes/deep stores, and local painted-panel/deck/ceiling material variants.
The variants use the existing triplanar texture library with smaller wear/rust
patterns; no shared Den or imported-kit materials were replaced globally.

Service doors now have attached stiffeners and finer, less orange rust patches.
Ring wall panel runs split around actual door margins instead of omitting whole
bays. Fixed wall-run orientation detection to include collider transform scale
in both the Den approach and new skirting. Moved the lounge-crawl instructions
beside the hatch so the text no longer overlaps grille bars or the hinge sweep.

Validation: compared captured Play-mode views before/after; 144/144 ring samples
have standing clearance and floor support; all 1,092 sampled room positions have
floor/ceiling coverage. Movement-input bot traversed both expanded rooms and
the Tool Store rising crawl route into the AC ring. All 40 focused door, tunnel
and player-state tests passed. No runtime exceptions observed. QA saves remained
isolated under Logs/RefinementReview/Saves.

### Irregular maintenance-room revision

Entrance positions now follow an uneven sequence: 39, 58, 126, 193, 219,
242, 254 and 337 degrees. Short clusters alternate with long uninterrupted runs;
The entrance table below reflects this revision.
Persistent identities remain MAINT_ROOM_045/090/225/270/345, and the steam valve
remains STEAM_RING_225, independent of its new position at 219 degrees.

Pump Service (58 degrees) has a 6 x 9 m main space, a separately door-controlled
rear machinery bay and a 5 x 5 m lateral control annex. The Tool Store (242 degrees)
has a different L-shaped footprint: a calibration annex, a rear store behind a
sliding door, and a narrower deep-stock extension. These are ordinary navigation
spaces, not additional mandatory puzzles. Equipment stays out of the door lanes.

Each expanded room has a low, hinged side hatch in its rear bay. An enclosed,
automatically crawling approach and rising duct join the existing L2 AC ring,
with another hinged hatch at the ring end. The two new ring wall apertures are
authored with the ducts, not hidden behind solid collision. The risers reach
3.3 m before crossing the maintenance ring wall, preserving standing clearance
below. Graph connections are bidirectional and room discovery covers the annexes.

Closing any ModalScreen clears UI selection before restoring gameplay capture.
PlayerCamera retries capture briefly after the closing frame, cancels retries
when another menu opens, and retries on application focus return; it does not
continuously force-lock the cursor when the user releases it in the Editor.

This pass's checks: 40 focused door/tunnel/player-state tests passed. Live
movement traversed both rooms' internal doors, both rising ducts, the AC-ring
join, and the Tool Store return route. PC and cupboard open/close checks restored
Locked/hidden cursor state without mouse-click input. QA saves use an isolated
Logs/IrregularRoomsReview/Saves directory, not the player's save slots.

- **L1, +4.6 m:** public concourse and the first enclosed customer lounge. Appointment
  customers and ambient passengers share a baked NavMesh and the existing trading system.
- **L2, +3.3 m:** continuous AC crawl ring. A branched, enclosed riser junction
  connects the ring, maintenance crate mouth, and short lounge customer riser.
  Six hinged, manually operated grilles close the route endpoints. Hatches use
  the existing `VentHatch` behaviour from main, not automatic sliding doors.
- **L3, 0 m:** continuous standing-height maintenance ring, service junction (02),
  maintenance gallery (03), dressed Den (01), power room (16), and supply store (09).
  The fixed crate beneath the head-height vent mouth makes the route dependable.
- **L4, -3.6 m:** U01 shortcut below maintenance, visible through the crowbar grate
  (`GATE_GRATE_03`). Two existing security drones patrol an authored route; a supply
  closet is a hiding zone. A fully enclosed emergency stairwell reaches the lounge
  through an inside-only, automatically closing `GATE_EXIT_U01` push-bar door.

Primary loop: Den door -> junction -> crate -> hatch -> crawl riser -> customer
grille -> lounge/public concourse. The optional AC and maintenance ring loops remain
available around the whole station. The drop shortcut cannot strand the player:
its emergency exit and security escort endpoint both lead to the lounge.

## Enclosure and inspection grilles

Ring walls, decks and roofs use coincident wedge boundaries, not overlapping boxes
with small voids at the bends. Bent connectors use shared mitered cross-sections.
All crawl connectors have side walls, a floor, a roof, and crawl volumes.

Spaced floor inspection grilles provide views between L1/L2 and L2/L3. The grilles
have visible bars and a separate walkable blocker; the U01 grate is different:
its blocker is disabled by the crowbar interaction, revealing the lined drop shaft.

## Reserved expansion

The first power/fan challenge (04/16/17) is implemented as an optional cache branch
off Power Distribution. Further customer-area progression, old pipes/condensation
(11/12), waste processing and staff backrooms remain expansion work. Neither the
fan nor a full-ring crawl is required for the first customer.

## Source

- Scene: [Station_Concourse_Ring](../../Assets/_Project/Scenes/Districts/Station_Concourse_Ring.unity)
- Main builder: [ConcourseRing](../../Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.ConcourseRing.cs)
- Layout: [StationExpansion](../../Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.StationExpansion.cs)
- Geometry/hatches: [StationGeometry](../../Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.StationGeometry.cs)
- Lounge, shortcut and navigation: [StationAreas](../../Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.StationAreas.cs)

## Verification performed

- Full-circle clearance sampling: 144 positions each in L2 and L3; no blocked
  player capsules or missing floors. L2 also passed floor/roof/side boundary rays.
- All six hatches block their openings when closed and clear them when open.
- Existing mantle logic successfully climbs from the fixed crate into the riser.
  Input-driven crawling reaches the junction, customer grille and optional AC ring.
- Input-driven walking reaches the maintenance ring, power room, store and Den.
- Crowbar prerequisite: inspect prompt without tool, pry prompt with tool, then
  the grate opens and the player safely lands in U01 below.
- Input-driven walking traverses U01 and the emergency stairwell. The push-bar
  door opens from inside, closes afterwards, and is locked from the lounge side.
- Both drone patrol loops have complete NavMesh paths (4/4 legs each); both
  drones moved during runtime observation. The supply closet reports hidden.
- Lounge customers spawn on NavMesh, request Glarb-Juice, and can be targeted
  through the open grille. Simulated E input opens the existing trading UI.

## Den production and orders

The ring scene now starts inside the Den. The existing desk opens VentNet; the
south-wall mixing table uses the original mixing process, gauge and recipe panel.
The east-wall ingredient cupboard contains 30 each of Glarb Extract, Lumina Gel
and Stabiliser Gel, with stable inventory save ID `hideout.locker`. Transfers use
the normal inventory UI. The decorative tool bench and cable spool are omitted
in this scene to leave the production station and central aisle clear.

VentNet replaces the lounge's unsolicited walk-in spawner with appointment
customers. Accept an offer, collect ingredients, mix in recipe order, stop in the
green band, let the batch settle, collect, and carry the requested quantity to
the person named in the lounge order. The regular customer trade panel performs the agreed
hand-off, removes product, awards credits and trust, and sends the customer away.
There are no marked delivery spots: customers choose free positions around the room.
The PC map uses lounge-local coordinates rather than the original plaza map.
Supplies purchases deliver additional ingredients directly to the same cupboard.

Live Editor verification (2026-11-10):

- Cupboard UI and PC UI opened through their existing interaction events.
- Camera targeting resolved mixer, cupboard and computer prompts.
- Simulated W input moved the player 2.05 m; simulated E opened VentNet.
- Transferred ingredients from cupboard, crafted three Premium Glarb-Juice,
  accepted an offer, and observed its customer reach Waiting at the lounge.
- Invoked the real sale-panel hand-over button: order Completed, product count
  fell to zero, credits increased by 153 and trust by 6.
- Actual supply-panel purchase added one ingredient and charged 5 credits.
- Both delivery spots returned complete NavMesh paths from the customer entrance.
- Visually inspected Den and VentNet; corrected oversized terminal text and
  replaced the old plaza map. No runtime exceptions were logged during checks.

The integrated crafting test advanced the mixing and appointment clocks directly;
it is not a claim that a full timed, input-only delivery traversal was replayed.
Earlier tunnel traversal checks above still apply. Test inventory, money and
orders are reset by restarting Play Mode before hand-off.

## Atmosphere and person-based rendezvous

The maintenance ring has 24 wall-side service bays, each with three equipment
clusters: scaled machinery, lockers, crates, tool carts, overhead utilities,
hazard pads, warm work lights and service labels. Hub props add work/storage
detail without obstructing the crate, hatches, grate or Den doorway. The central
maintenance lane passed capsule-clearance checks at all 144 ring samples.

Public-level security now includes one lounge patrol (five stops) and one nearby
concourse patrol (eight stops), in addition to the two basement drones. All 21
patrol legs were checked at their proper floor height and returned complete paths;
both new drones moved during observation at y=6.85, above the public floor at 4.6.

The public NavMesh includes concourse and lounge geometry only, on the dedicated
PublicNavigation layer; this avoids accidentally routing people on lower tunnel
ceilings. Customer entry/exit is on the concourse, not next to a hand-off point.

Appointments say "Meet me in the lounge". Each arriving customer selects an
unoccupied NavMesh location inside the room. No floor tag, pinpoint map dot or
sales speech bubble identifies them remotely. Mugshots are rendered from the
actual character prefab using the same body tint as the order customer, displayed
in VentNet offers/booked deals and the persistent HUD. Aim at a person to read
their name; the existing interaction/trade panel handles the sale. Internal region
IDs retain save compatibility but are not displayed as physical meeting spots.
Room appointments allow two simultaneous orders without exclusive spot booking.

StationCrowd maintains at most 12 passengers circulating between two concourse
entrances and lounge/concourse pause points. Passengers share customer species and
looks, have distinct names from the order-customer name pool, dwell 12–35 seconds,
leave via the concourse and despawn; they cannot start a sale or consume an order.
This is traffic for the current service-concourse/lounging area, not a simulation
of every station district.

Additional live checks:

- Observed a customer walk from the distant entry and reach Waiting at a random
  room location, with other passengers present; no delivery-marker objects remain.
- Observed passenger Arriving, Waiting and Leaving states; nine originally
  observed passengers departed during a 44-simulated-second turnover check.
- Completed an unmarked customer sale through the real hand-over button:
  order Completed, two products removed, 122 credits awarded. Product was seeded
  for this focused test; the earlier production test covers actual crafting.
- Visually inspected generated mugshots, persistent identity HUD and occupied lounge.
- Board test: two room orders with the same region ID accepted, third blocked by
  the two-order limit; RoomMeeting survived JSON save round-trip.
- No runtime exceptions observed. Test state is reset before hand-off.

## Local controls and first fan challenge

The Den and the two hub side-room doors (Power Distribution and Supply Store)
open with E, not proximity, and close after four clear-doorway seconds. They stay
open while the player occupies the doorway. Other station doors retain their
existing automatic/locked behaviour. The Den's manual mode is a serialized prefab
override, verified after rebuilding and restarting Play Mode.

Switches beside the Den entrance, inside and outside, control seven nearby hub,
power-room/store and maintenance work lights under `STATION_DEN_APPROACH_LIGHTS`.
The Den, public lounge, fan warning and red emergency glow remain independent.
Light state is stored through the existing tunnel-switch save system.

Fan lesson: enter 16 Power Distribution, identify the orange cable and labelled
`BRK_FAN_04 / FAN 17` breaker, switch off with E, wait for the fan to coast to a
visible stop and its lamp to turn amber, then crouch/crawl through the housing.
The sealed branch bends toward a service cache with five of each recipe ingredient.
It returns by the same route; there is no one-way drop or first-sale gate.
The cache roof is 2.2 m high to fit safely underneath the existing emergency stairs.

The existing FanHazard and BreakerSwitch own the hazard and power state, with
stable `R04`, `R17`, `FAN_17` and `BRK_FAN_04` IDs. Running/coasting fans remain a
solid obstruction; stopped blades remain visible and collidable, leaving a low
central gap. There is no damage or health mechanic. A restart interlock refuses
power while the player occupies the fan passage.

Verification:

- Den: nearby with no interaction remained shut (0); E opened it (1); occupancy
  held it open beyond four seconds; stepping clear allowed closure (0).
- Both new side-room doors opened and use the same timed/occupancy behaviour.
- Simulated E targeted and operated both the Den door and local light switch.
- All seven controlled lights disabled; Den/lounge lights and emergency glow stayed on.
- Fan: Running -> WindingDown (still blocked) -> Stopped (passable, speed 0).
- Input-driven crouch traversal crossed the housing, reached the cache and returned
  through six successive movement goals without teleporting through geometry.
- Running-fan movement test stopped the player at local x=-11.41, before the fan.
- Restart while inside was refused; clearing the passage allowed restart.
- Tunnel save capture/restore restored both lights-off and fan-stopped states.
- Capsule clearance samples across the new branch and cache found no obstructions.
- Visually inspected the fan assembly; no runtime exceptions during verification.

## Maintenance architectural finish

The hub and full L3 maintenance ring now reuse the Den's actual wall and ceiling
panel kit, including its painted-purple, dark-frame and worn hazard-trim materials.
Plain shell walls no longer use the floor-metal material. Grouped wall-cell runs
preserve every authored opening instead of covering doors or the crate hatch.
Recessed ceiling shoulders, cross ribs, oxidised utility pipes and orange collars
give the corridors a lower, enclosed service-space silhouette without lowering
the navigable central headroom. Inspection-grille ceiling openings remain clear.

Architectural panels are visual layers over the original watertight collision
shell. Their prefab colliders are explicitly disabled and the overrides saved;
they cannot silently change the traversal clearance after a scene reload. New
panel emissives near the Den participate in the local maintenance-light circuit.
The Den and customer lounge themselves keep their existing finish.

Sliding leaves now unseal over the first 14% of the stroke, then follow a quintic
ease-in/ease-out slide with a slower closing stroke and a final latch sound.
The Den and hub doors use 0.85 s opening / 1.05 s closing; E interaction, four-second
clear-doorway hold, occupancy protection and exact closed positions are retained.
Side doors also have dark jambs and an overhead track.

The batch-capable [maintenance review](../../Assets/_Project/Scripts/Editor/StationMaintenanceReview.cs)
rebuilds the saved scene, captures hub/ring views, then enters actual Play Mode
to check manual operation, occupancy, closure, lighting, fan timing and clearances.
This pass completed those checks, including all 144 maintenance-ring samples and
the fan/cache passage samples. Hub/ring captures were visually inspected.

## Complete game flow and ring-room pass

New Game now enters Station_Concourse_Ring. MainMenu, the station and the legacy
greybox are enabled in build settings. Continue reads scene identity from the
save (including backup fallback); old saves without a scene name retain the
legacy greybox route. Build All Scenes now includes the station assembly.

The station has SaveManager, a full item/upgrade database and ProgressionManager.
Pause supports Save, Save & Main Menu and Save & Quit; a failed save refuses to
leave gameplay. Save v5 adds scene identity and mixer state: loaded ingredients,
recipe, phase, progress, settle time and product quality survive scene changes.
Credits, inventories, orders, appearance and infrastructure continue through
the existing save system. Invalid saved positions inside closed obstructions
fall back to the Den spawn rather than trapping the player. Steam isolation snaps
immediately on restore so a saved open passage is not momentarily treated as blocked.

A rusty two-leaf blast door separates the Den approach from the maintenance ring
at service-frame z=8. It opens with E, takes 1.25 s to open / 1.5 s to close and
holds for five clear-doorway seconds. The ring vault shoulders and side pipes sit
lower (side pipes at 2.32 m, cross ribs at 2.52 m), retaining standing clearance.

Eight inward-facing entrances are spread around the ring:

| Angle | Space | Access |
|---|---|---|
| 39° | Pipefitter break room | Door secured open; ordinary room |
| 58° | Pump service | Manual doors, machinery/control annex, AC crawl connection |
| 126° | Sealed relay vault | Permanently sealed, backed by the ring wall |
| 193° | Collapsed access | Locked facade obscured by fallen brace and debris |
| 219° | Steam service | One isolation-valve encounter, off the main route |
| 242° | Tool store | Manual doors, calibration/deep stores, AC crawl connection |
| 254° | Decommissioned | Permanently sealed, backed by the ring wall |
| 337° | Maintenance rest bay | Door secured open; ordinary room |

The five usable rooms have complete shells, short vestibules, panel ceilings,
lights, dressing, discovery volumes and graph connections. The three dead entries
are intentionally not claims of playable rooms behind their doors. Twelve other
small steam vents provide particles and positional hissing without collision or
tasks. Only the 219° room's jet blocks its vestibule; E on its nearby valve stops
the jet/hiss and saves isolation under STEAM_RING_225. No steam blocks the main lap.
Isolation is one-way for this pass; no restarting a jet onto someone in the doorway.

Verification in the live Editor:

- New Game UI -> station; saved -> Save & Main Menu -> Continue UI -> station.
- Credits, cupboard stock, an in-flight mixer batch, lights and steam state restored.
- Corrupted only an isolated test save: backup loaded, then Continue restored the
  correct station scene and 37-credit test balance. Real player saves were untouched.
- Simulated E opened the blast door; input-driven walking passed into the ring.
- Input-driven walking entered and returned from all five usable rooms.
- Simulated E isolated the blocking steam jet; its blocker cleared immediately.
- Main-ring clearance passed all 144 samples; no runtime exceptions observed.
- All 30 focused SaveTests, MixerPersistenceTests and SlidingDoorTests passed.

Further power-routing puzzles and additional districts remain expansion work.
