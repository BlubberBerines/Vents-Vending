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
- [ ] Art reference library (`Docs/Reference/ArtReference/`)
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
13. [~] Entering and exiting vents (`VentHatch` open/close; grate drop-down exits still to do)
14. [x] Basic mixing machine (`MixingStation` prefab: vessel, stirrer, gauge, status lamp, overheat smoke; `MixingPanel`)
15. [x] First ingredient ScriptableObjects (Glarb Extract, Lumina Gel, Stabiliser Gel — generic `ItemDefinition` for now)
16. [x] First RecipeDefinition (`Recipe_GlarbJuice`: Glarb Extract → Lumina Gel → Stabiliser Gel)
17. [x] Ingredient consumption (loaded from pockets; Empty vessel returns them)
18. [x] Product generation (`ProductDefinition` Glarb-Juice, 4 quality tiers from mix timing + order; 27 EditMode tests)
19. [ ] Basic customer
20. [ ] One complete sale
21. [ ] Cosmic Credits
22. [ ] First upgrade
23. [ ] Connect hideout to the alien plaza
24. [ ] Simple security drone
25. [ ] First playable build
