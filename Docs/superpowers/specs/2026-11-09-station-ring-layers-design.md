# Station Ring & Layers — design

Status: **approved by the user (2026-11-09).** Amends `Docs/Design/TunnelNetwork.md` (§0 item 11, §3) and gives GDD Part 19 locations a physical home. Where this spec and older notes disagree, this spec wins on station structure and layer rules; the GDD still wins on scope (Part 27).

## 1. Intent

Give the station one clear physical structure the player can learn: a **ring (a donut)** built in **stacked layers**. Public life is on top; the player's hidden world is underneath; each layer down is more hidden, more cramped or more dangerous, and each has its own way of moving. The current playable map is reorganised onto this structure and rebuilt from the new environment kit (one assembly that replaces the existing map, see §9).

## 2. Decided with the user

- **The station is entirely circular, like a donut, with layers.**
- **Layers, top to bottom:**
  1. **Concourse** — NPC/customer areas: the lounge, the plaza and the other public locations.
  2. **Crawlspace** — thin, **crawl-only** tunnels forming the vent maze. They carry the station's **air conditioning and garbage disposal**.
  3. **Maintenance tunnels** — standing height; **the den is here**. Built like what we already have, except:
     - **the den's entrance is a full-size sliding door**, and
     - **every ventilation entrance is at head height**, reached by **climbing on crates**.
  4. **Basement** — the current placeholder level below (the Maintenance Underpass) with its **drone patrol stays**.

## 3. The layers

Heights are proposals on the kit grid (2 m modules, 3 m walls), chosen so the existing den, gallery (y 0) and underpass (y −3.6) keep their heights.

| | Layer | Floor (y) | Clear height | How you move | Who's there | Role |
|---|---|---|---|---|---|---|
| L1 | **Concourse** | +4.6 | 5–10 m (public halls) | Walk, run | Customers, vendors, security, crowds | Selling, observing, shops, the economy's face |
| L2 | **Crawlspace** | +3.3 | ~1.0 m ducts | **Crawl only** | Vermin, small creatures; nobody official | The vent maze: AC trunks and garbage chutes; links the hidden world to the concourse |
| L3 | **Maintenance** | 0 | 3 m corridors | Walk, crouch; **climb onto crates** | Rarely anyone (staff-only, neglected) | Home layer: the den, workshops, storage, junctions, breakers |
| L4 | **Basement** | −3.6 | 3 m corridors | Walk, crouch | **Drone patrols** (humanoid guards later) | Risky fast routes, storerooms, deep infrastructure |

Slabs between layers are ~0.3 m (L3 ceiling at 3.0, crawl ducts 3.3–4.3, concourse floor 4.6). The concourse is the only layer with views of space and the ring's curve.

### 3.1 Layer rules

- **L1 Concourse.** Public. Being there is legal; dealing, sprinting and contraband raise suspicion as today. The player enters it on foot through staff doors and emergency exits, or interacts with customers through **customer vents** (grilles low in concourse walls) from L2.
- **L2 Crawlspace.** Crawl-only (`Crawling` state for the whole layer). It's the maze: tight turns, junction boxes, fans, junk. Two duct families with their own look and sound:
  - **AC trunks** (cool, humming, filters, fans): the network's spine around the ring.
  - **Garbage chutes** (stained, smelly, rattling bins of trash dropping past): the source of salvage and later the waste set pieces.
  - From L2 the player sees into the concourse through grilles and down into maintenance through vent mouths.
- **L3 Maintenance.** Standing height, unpatrolled in the slice (trespass free). The den sits off a maintenance corridor behind a **full-size sliding door** (opens for the player, closed otherwise; lockable later as a den upgrade). **Every way up into the vents is a vent mouth at head height** (bottom edge ~1.8 m): the player stacks or finds **crates** under it and climbs up, then crawls in.
- **L4 Basement.** The Maintenance Underpass as designed on 2026-11-08 (drones, trespass suspicion, supply-closet hiding, escort-out on catch). Reached from L3 by the crowbar floor grate (one-way drop). Its exit now has to reach the concourse two layers up, so it becomes an **emergency stairwell** that rises through L3 (no door into L3) to a push-bar door in the concourse.

### 3.2 Moving between layers

| From → to | Connector | Rule |
|---|---|---|
| L3 → L2 | **Vent mouth at head height + crates** | Climb a crate (≤ 1.1 m), then climb into the mouth. A short sloped **riser duct** (crawl) lifts the ~1.5 m to the crawl layer. |
| L2 → L3 | Same vent mouths (drop out) and **drop hatches** in duct floors | Dropping out onto the crate or floor is always safe (no fall damage). |
| L2 → L1 | **Customer vents** (grille in a concourse wall, low) | Hand-off and observation stay inside the vent; the player never needs to leave L2 to sell. Some grilles open (later) to let the player climb out into back corners of the concourse. |
| L1 → L2/L3 | **Staff doors / maintenance hatches** in the concourse | Locked or watched; for later progression. The den is never reachable from L1 without going through L2 or L3. |
| L3 → L4 | **Crowbar grate** (existing `GATE_GRATE_03`), later lifts/stairs | One-way drop onto a safe landing. |
| L4 → L1 | **Emergency stairwell** to a push-bar door | Inside-only exit, never saved open (as in the underpass spec). |

## 4. The ring

- **Shape.** A horizontal ring (the layers stack vertically; "down" is −Y everywhere). Proposed **radius ~80 m** on the concourse (circumference ~500 m). It's never all built at once.
- **Sectors.** The ring is divided into **24 sectors** (15° each, ~21 m of concourse arc), numbered like the station's old signage (`S01`–`S24`). Each sector is built from straight kit modules; the curve happens at **sector joints** (a short angled module or bulkhead every sector). Inside a sector the corridors are straight, which keeps the 2 m kit, NavMesh and collisions simple.
- **Inner and outer sides.** The **outer wall** of the concourse has windows to space and docked ships; the **inner side** faces the hub (spokes, lifts, transit). Every layer uses the same convention, so "outward/inward" is a navigation cue on every layer.
- **Locations as arcs.** GDD locations become arcs of the ring, each with all four layers under it:
  - Central Plaza + the Lounge (the slice), then Industrial Sector, Luxury District, Docking Sector.
  - Unbuilt arcs are sealed by **sector bulkheads** (closed doors with a status light) on every layer, so the ring reads as a ring before it's all built.
- **Loops come free.** Every layer can eventually go all the way round, so loops are natural (TunnelNetwork §1 "loops over dead ends").

## 5. What happens to what exists

| Current | Becomes |
|---|---|
| Den / hideout (01 Starting Nook), y 0 | L3, behind the new sliding door off a maintenance corridor |
| Gallery D1–D3 (03 Long Crawl, crouch bundle, vault pipes) | L3 maintenance corridors (standing; the crouch bundle stays as an L3 obstacle) |
| Exit duct, plaza duct, side stash duct (02, 05, X01) | L2 crawl ducts, entered from L3 vent mouths |
| Fan junction 04, breaker room 16, fan room 17 | Fan in an **L2 AC trunk** (it blocks a duct); breaker in **L3** (power lives where you can stand) |
| Plaza (y 0, z ≥ 20) | **L1 concourse**, raised to y +4.6; the customer vents become L1 wall grilles fed from L2 |
| Maintenance Underpass U01 (y −3.6) | **L4 basement**, unchanged inside; exit becomes the emergency stairwell to L1 |
| Blueprint planes P1–P5 | P2 → L1/L2, P3 → L3, P4 → L2 (garbage/AC ducts) + L4 (processing rooms), P5 → L4 (deepest, oldest), P1 security → L1/L4 (security offices up top, drone docks below) |

Room IDs and gate IDs stay; only their layer changes. `TunnelNetwork.md` §0.4 ("crawling only in short ducts") and §0.10's "home is vents-only" are **superseded** (see §7).

## 6. Mechanics this needs

1. **Climbing (mantle).** New `PlayerMovementState.Climbing`: hold forward + jump (or interact) at a climbable edge to pull up onto it.
   - **Climbable:** crates and crate stacks (≤ 1.1 m), ledges, and **vent mouths** whose bottom edge is within ~0.9 m above the surface the player stands on.
   - Climbing into a vent mouth ends in `Crawling` inside the duct.
   - Short, readable animation (~0.4–0.6 s), no stamina, can't fail. No free climbing, no ladders yet.
2. **Crates as tools.** Crates are physics-light props the player can **push or carry** a short distance (one crate at a time) to reach a vent mouth. In the slice, at least one fixed crate sits under the main vent mouth near the den so the route always works; pushable crates open optional vents.
3. **Sliding door.** A full-size powered sliding door (`SlidingDoor` component, state owned by the door, opens on approach for the player, closes behind). The den door is the first one; the same module is used for staff doors and sector bulkheads (locked state).
4. **Crawl-only layer.** The L2 volume forces `Crawling` (no standing up into the duct ceiling); the existing crawl speed applies. Riser ducts are sloped crawl.
5. **Layer-aware systems.**
   - Drones: altitude and NavMesh sampling relative to their zone's floor (already required by the underpass spec).
   - Room discovery and the debug overlay show the **layer and sector** (`L3 · S04`).
   - Audio and post-processing volumes per layer.

## 7. Goals, non-goals, anti-goals

**Goals**
- The player can always say which layer they're on from look and sound alone, and roughly where on the ring from sector signs and the inner/outer cue.
- Going down = more hidden; going up = more public. Selling happens from L2 into L1.
- The den feels like a real room in a real corridor (a door you walk through), and getting into the vents is a small physical act (climb the crates).

**Non-goals (first assembly)**
- Building the whole ring. One arc (Plaza + Lounge, 3–4 sectors) on all four layers, the rest sealed.
- Spin-gravity effects, a visible hub, transit between arcs.
- Ladders, lifts, free climbing; humanoid guards.

**Anti-goals**
- A walking simulator. Den → first customer must stay short: **den door → corridor → crates → vent mouth → ≤ ~15 m of crawl → customer vent** (the TunnelNetwork §11 check still applies).
- Crawl fatigue. Long crawl stretches in L2 are optional routes and shortcuts, never the only way home.
- Unclear climbing. Every climbable vent mouth and crate has the same readable silhouette (hazard-striped lip, grille swung open, a crate under it).
- Getting stuck. Every crate puzzle has a fixed fallback; nothing can be pushed into a state that blocks the den.

## 8. Kit implications

- The environment kit A (walls 2×3 m, floors, ceilings) is the **L3 maintenance** language. New modules needed, in rough order:
  1. **Sliding door** wall module (den door, staff doors, bulkhead variant).
  2. **Vent mouth** wall module (head-height opening with lip and hinged grille) + **riser duct**.
  3. **Crawl duct** set for L2 (1.0 × 1.0 m: straight, corner, T, cross, riser, drop hatch, customer grille, fan housing), in two dressings (AC / garbage).
  4. **Crate** set (fixed stack, pushable crate) using the props arsenal where possible.
  5. **Sector joint** modules (L3 corridor and L2 duct bends), **sector bulkheads**.
  6. Concourse (L1) kit: taller public walls, storefront frames, windows to space. Basement (L4) reuses L3 with a colder dressing.

## 9. Delivery

- Everything above lands as **one assembly** that replaces the current playable map (Greybox prototype scenes). No commits until that assembly is playable (user's instruction, 2026-11-09).
- Build order: L3 around the den (with the sliding door and one crate-climb vent mouth) → L2 duct maze to the customer vents → L1 plaza/lounge arc raised to +4.6 → L4 basement re-linked (stairwell exit) → sector bulkheads → greybox tests (§7 checks, TunnelNetwork §11) → art pass with the kit.

## 10. Calls I made (not discussed; flagged for review)

- **Ring orientation:** horizontal ring with layers stacked vertically (normal "down"), rather than spin gravity with layers stacked toward the rim.
- **Scale:** radius ~80 m, 24 sectors, straight modules with bends at sector joints.
- **Heights:** L1 +4.6, L2 +3.3 (1.0 m ducts), L3 0 (3 m), L4 −3.6, so existing den/gallery/underpass heights are kept.
- **Maintenance (L3) is unpatrolled and trespass-free** in the slice; danger lives in L4 (and later L1 security).
- **Selling stays inside the vents** (customer grilles from L2); climbing out into the concourse comes later.
- **Fans live in L2 ducts, breakers in L3.**
- **Climbing** is a mantle onto crates/ledges/vent mouths (no stamina, can't fail); one fixed crate always under the main vent near the den.
- **The basement exit** becomes an emergency stairwell to the concourse (inside-only push-bar door).
- **First assembly scope:** one arc (Plaza + Lounge, 3–4 sectors) on all four layers; the rest sealed by bulkheads.
