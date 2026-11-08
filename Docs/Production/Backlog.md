# Production Backlog

Source: GDD Appendix B. Current phase: **Phase 1 — Pre-production** → Phase 2 Core Prototype.

> The first production milestone is not a beautiful game. It is a complete, functioning business loop.

## Phase 1 — Pre-production

- [x] Unity Hub + Unity 6.3 LTS installed, version pinned
- [x] Folder structure per GDD §18.2
- [x] Git repository + LFS rules
- [x] Assembly definitions (runtime / editor / tests)
- [x] Unity Personal licence activated
- [x] Project opened once in the editor and settings committed
- [ ] (Before first IL2CPP release build) Visual Studio Build Tools with "Desktop development with C++"
- [x] Art direction defined (`Docs/Design/ArtStyleGuide.md`, look-dev scene, material library)
- [x] Initial greybox level (`Scenes/Prototype/Greybox_Movement`, built by menu Vents & Vending → Prototype)
- [ ] Core gameplay prototype plan

## Phase 2 — Core prototype (Appendix B)

1. [x] Create and configure the Unity project
2. [x] Configure URP and Input System (URP 17.3, Input System only, project-wide actions + Crawl action)
3. [x] First-person controller (`Scripts/Player/`, CharacterController + central state enum)
4. [x] Camera movement (mouse + gamepad, pitch clamp, cursor lock)
5. [x] Crouching (toggle or hold, headroom check)
6. [x] Crawling (manual + forced inside `VentZone`)
7. [x] Interaction interface (`IInteractable`, `InteractionController`)
8. [x] Raycast interactions + prompt UI (`HudController`)
9. [x] Pickupable object (`PickupItem`, physics vial prefabs, drop from inventory)
10. [x] Inventory (`InventoryModel` + `InventoryComponent`, Tab screen, 12 EditMode tests)
11. [x] Basic storage container (`StorageContainer`, two-column transfer view)
12. [~] First ventilation section (greybox L-duct in `Greybox_Movement` — no modular prefabs yet)
13. [x] Entering and exiting vents (`VentHatch` hatches/grates at both ends of the hideout↔plaza duct)
14. [x] Basic mixing machine (`MixingStation` prefab: vessel, stirrer, gauge, status lamp, overheat smoke; `MixingPanel`)
15. [x] First ingredient ScriptableObjects (Glarb Extract, Lumina Gel, Stabiliser Gel — generic `ItemDefinition` for now)
16. [x] First RecipeDefinition (`Recipe_GlarbJuice`: Glarb Extract → Lumina Gel → Stabiliser Gel)
17. [x] Ingredient consumption (loaded from pockets; Empty vessel returns them)
18. [x] Product generation (`ProductDefinition` Glarb-Juice, 4 quality tiers from mix timing + order; 27 EditMode tests)
19. [x] Basic customer (`CustomerDefinition` data, NavMesh `Customer` lifecycle, Blorb + Gromflomite, `CustomerSpawner`, speech bubbles)
20. [x] One complete sale (`SalePanel`: pick quality, set price, haggle via `SaleNegotiation`)
21. [x] Cosmic Credits (`Wallet` + `PlayerWallet`, HUD counter; start 20 CC)
22. [x] First upgrade (6 `UpgradeDefinition`s, `ProgressionManager` + `UpgradeLedger`, Upgrade Terminal + shop panel; effects: lighting, pockets, locker, mixer window, settle speed)
23. [x] Connect hideout to the alien plaza (Central Plaza reachable only via vent duct; customers moved to plaza)
24. [x] Simple security drone (`SecurityDrone` patrol/suspicion/search/pursuit, `SecurityManager` confiscation + fines, HUD alert; 16 tests)
25. [x] First playable build (Windows Mono, main menu + pause + settings + save/load; `-smoketest` passes)

## Milestone 1 reached — complete, functioning business loop ✅

Next candidates (GDD Phase 3 Vertical Slice): audio pass, packaging machine + second product,
reputation (GDD 9.3), customer orders, Federation Inspector, production state in saves, art pass on the greybox.

## Tunnel slice (Docs/Design/TunnelNetwork.md §0)

- [x] Hideout rebuilt as a cramped maintenance chamber + service gallery (rooms 01, 02/03, 05, secret stash)
- [x] Less-perfect prop pass (wonky meshes, careless placement, wear, sagging cables)
- [x] Tunnel data: `VentRoomDefinition` / connections / gates (stable IDs), room-discovery triggers, discovery + gate state in saves
- [x] Debug overlay (F3): room ID, plane, connections, gate and hazard state
- [x] 02 Service Junction as a real three-way hub with old-station signage
- [x] 16 Power Distribution (breaker wall, `BRK_FAN_04`) + 17 Industrial Fan Room + 04 Fan Junction (`FAN_17` blocks until isolated)
- [x] 06 Drop Hatch → 07 Plaza Service Hub → 08 Customer Vent over the richer mezzanine customers; staff-door latch (07 → plaza) is the shortcut home
- [x] Richer customer tier (Gromflomite Executive, Blorb Socialite in the Concourse Lounge) (bigger budgets, higher quality expectations) served only via 08
- [x] Pass 2 hideout dressing: clutter, idle motion (wall fan, drips, swaying + flickering bulbs), procedural placeholder audio, master volume setting

## Orders and plaza (Docs/Design/VentNet.md)

- [x] VentNet: customer messages on a den PC, accept/decline, meeting windows, trust; save v3
- [x] Three meet spots (A low / B high / C medium risk) with floor tags and booked beacons; HUD deal tracker
- [x] Deal hand-off in the sale panel (agreed price, better quality pays more)
- [x] Central Plaza refresh: clean panels, lit ceiling with vent grilles, ads, planters, vending machine, kiosk, crowd / music / PA audio
- [ ] Play-test a full lounge sale (Executive + Socialite) and tune their prices vs product value
