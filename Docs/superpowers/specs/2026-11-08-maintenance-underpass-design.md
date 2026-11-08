# Maintenance Underpass — design

Status: approved by the user (2026-11-08). Amends `Docs/Design/TunnelNetwork.md` (§0 gets a new item when this ships).

## 1. Intent

Give the player a faster, **walk-upright** route from the den to the plaza that has to be earned and carries real risk. The vents remain the first, slow, safe way. Later in a day, with a full backpack, the player chooses: the long safe crawl, or the quick drop into a patrolled maintenance corridor.

The corridor is also a **hook for later content**: its many side rooms will become storerooms and other things.

## 2. Decided with the user

- **Underpass:** a maintenance corridor under the vents: pipes, cable trays, numbered doors, **lots of side rooms**. Most side rooms start empty or locked and are placeholders for later.
- **One-way drop:** the player drops down through the grate and **cannot climb back up** that way.
- **Patrols:** drones patrol it now. Humanoid security staff come later and aren't built in this pass.
- **Unlock:** a **floor grate removed with a crowbar**.
- **Getting the crowbar:** one crowbar is **lying around in the tunnels**, and one can be **bought on the den PC (VentNet) and delivered**.
- **Security:** being spotted uses the existing security system: suspicion, then pursuit, then catch, then search. Getting home uses the vents from the plaza end. The den stays reachable by vents only.
- **Placement:** the underpass is a new lower space, not the blueprint's upper Security district (P1).

## 3. Goals, non-goals, anti-goals

**Goals**
- A route that is noticeably faster than the vents (it skips gallery D2, D3 and the 10 m plaza crawl).
- Readable risk: you can see patrols coming, side rooms let you break line of sight, and every catch is fair.
- The destination is visible before the unlock: from day one the player can look through the grate and see the lit corridor and a drone light passing below.
- The grate's open state and the crowbar are saved and never reset or duplicate.

**Non-goals (this pass)**
- Humanoid guards, cameras, keycards.
- Contents for side rooms beyond a couple of dressed ones.
- Physical shops (the clothing shop comes later).
- Other crowbar uses (blocked waste room 15 isn't built yet). The crowbar is designed so they can be added.
- Climbing or ladders (no `Climbing` state).

**Anti-goals (a failure even if it technically works)**
- A constant chase. It should be a timing and observation route, not a gauntlet.
- Making the vents pointless. The underpass must stay riskier, especially when you're carrying contraband.
- Trapping the player. Once down, the only way is forward, so the exit must **always** be open from the inside.
- Instant, unfair catches (a drone appearing round a corner and detaining you immediately).

## 4. The player's journey

1. **Discovery (day one).** In the Long Crawl (room 03, gallery D1 at about (0.3, 0, 0.5), north of the crouch pipe bundle and just before the turn into D2) there is a heavy, rusted, see-through floor grate. Below: cold fluorescent light, pipes, and every so often a drone beam sliding past. Interacting without a crowbar shows: *"Rusted solid. You'd need something to lever it."*
2. **Getting the crowbar**, either way:
   - **Found:** lying in the secret stash duct off D3 (room X01, near the existing loot at about (−3.5, 0.02, 10)).
   - **Bought:** a new **Supplies** tab on the VentNet PC lists *Crowbar, 60 CC*. Buying it charges the wallet and delivers the crowbar into the **storage locker** in the den, with a toast: *"Package dropped in your locker."*
3. **Prying.** With a crowbar in your pockets the prompt reads **"Pry open grate"**. A short pry (about 1.2 s, with a loud screech and a clank) and the grate comes off and leans against the wall. It is open for good (saved).
4. **The drop.** Step in and fall about 3.6 m onto a safe landing (a crate stack or a mattress pile, in keeping with the less-perfect props). There's no fall damage. The hole above is out of reach.
5. **The underpass.** An upright corridor with an L-shaped dog-leg. Drones patrol it. Doors line both sides: some open (somewhere to hide and break line of sight), most locked placeholders. Signage uses the Industrial sign plates (for example "SUBLEVEL B — MAINTENANCE", door numbers, "AUTHORISED PERSONNEL ONLY").
6. **The exit.** A ramp or stair up to an **emergency exit door** in the plaza's south wall. It is push-bar, **open from the inside only**: from the plaza side it's a plain locked staff door. You come out into the plaza on foot, upright, at a spot that doesn't look suspicious.
7. **Going home:** the normal plaza vent grate at (−8, 0, 25).

## 5. Security in the underpass

- **Trespass suspicion.** The underpass is a *restricted zone*. While inside it the player gains a base suspiciousness (proposed **+0.6**) on top of contraband, dealing and sprinting. Today a player with no contraband is never noticed, which would make the route risk-free when empty-handed. Visibility modifiers still apply: crouching (0.6) makes you harder to notice, and inside an open side room drones lose line of sight.
- **Catch consequence (decided with the user): escorted out.** A catch inside the underpass runs the usual `SecurityManager.HandleCaught`: contraband confiscated, fine of 15 + 5 per unit capped at balance, vigilance up, "DETAINED!" toast. **Then the player is moved out through the emergency exit into the plaza**, just outside the door, with a short fade and a toast such as *"Escorted off the premises."* This ends the visit, so repeated catches in a space you can't leave are impossible. Underpass drones then reset to their patrols. Catches in the plaza are unchanged. The crowbar is **not contraband** and is never confiscated.
- **Hiding (decided with the user): mixed.**
  - The **supply closet is a guaranteed hiding spot**: while inside its volume the player's visibility is 0, so no drone can notice or keep tracking them.
  - The **other open side rooms can be swept**: patrol detours may look or enter into them, and those rooms carry a clear visual tell (door propped wide, light spilling out) so the risk is readable.
  - Locked rooms are scenery.
- **Patrols:** two drones.
  - **Drone A** runs the long leg, with a stop just under the grate so it's visible from above on day one.
  - **Drone B** covers the dog-leg and the exit ramp.
  - Their patrol timing leaves gaps a careful player can read, with stops that sweep into one or two side rooms (using the existing detour and sweep features).
- **Required engine fix:** drones currently treat altitude as an absolute world y and sample the NavMesh at y = 0. An underground drone would path on the gallery floor above. Drones need their altitude **relative to their zone's floor**, and NavMesh sampling at the zone's floor height. The plaza drones must keep behaving exactly as today.

## 6. Geometry and look

- **Position:** under the gallery and hideout footprint (that space is currently empty). Corridor floor at about y −3.6, ceiling at about −0.6 (stays under the gallery's floor slab, y −0.5 to 0). The exit ramp rises to y 0 before the plaza slab, which starts at z 20.
- **Size:** main corridor about 3 m wide and 3 m tall, about 28–35 m long including the dog-leg, with 6–8 side doors.
- **Side rooms:**
  - 2–3 open and dressed: a supply closet with shelves (a hiding spot), a pump room with a noisy pipe, and a break room with a vending machine (a nod to the title).
  - The rest locked with door plates such as "STORE B-04", as future storerooms.
- **District look:** colder and cleaner than the vents, but still worn. Flickering tube lights, painted pipe runs (colour-coded), cable trays, floor hazard lines, puddles.
  - It needs its own post-processing volume (the hideout volume stops at y −0.5).
  - Audio: room tone, a pipe hum and distant drone buzzes, kept at the reduced tunnel ambience level.
- **Tunnel graph:** a new room (proposed ID `U01` "Maintenance Underpass"), with connections `03 → U01` (one-way, gated by `GATE_GRATE_03`) and `U01 → plaza` (one-way). It's added to discovery like other rooms.

## 7. Data and saving

- **Crowbar:** a new `ItemDefinition`, category `Equipment`, not contraband, max stack 1, with its own world model (not the vial prefab). It's registered in the `GameDatabase` so it survives saves.
- **Grate:** a `TunnelState` switch `GATE_GRATE_03`, following the `LatchDoor` pattern (snap on `Restored`). No save version bump.
- **The found crowbar must not respawn:** the world pickup is tied to a switch (for example `PICKUP_CROWBAR_X01`), set when picked up. A pickup with that switch set is removed on load.
- **Purchase:** the VentNet Supplies listing charges via `Wallet.TrySpend` and adds the crowbar to the locker inventory (`hideout.locker`). The listing remains buyable. Buying a second one is harmless, because the crowbar is a sellable or storable item. **If the locker has no room, the purchase is refused before charging**, with the message *"Locker's full. Make room for the delivery."* (my call).

## 8. Builder's calls (left to the implementer)

- Exact corridor layout, door count, prop dressing and patrol stops, within the ranges above.
- The pry interaction's presentation (hold versus a single press with a short lockout).
- Supplies tab layout, in the existing Ink & Hazard VentNet style.
- The crowbar model (simple primitives in the project's style are fine).
- Tuning: the trespass value, drone speed and the 60 CC price may change after a play-test.

## 9. Calls I made (not discussed; flagged for review)

- The grate is in gallery D1 just past the crouch bundle (inside room 03's volume).
- The crowbar costs 60 CC, delivery is instant into the den locker, and the listing stays available.
- The crowbar is a legal item and is never confiscated.
- The exit is a push-bar door, open from the inside only, so the underpass can't be used as an entrance from the plaza.
- Trespass suspicion exists, so the route is risky even without contraband.
- There are two drones and 2–3 dressed side rooms in this pass.
- The landing is a soft prop pile, with no fall damage (consistent with "hazards block, they don't hurt").
