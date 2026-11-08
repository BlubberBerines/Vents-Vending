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
7. [ ] Interaction interface (`IInteractable`)
8. [ ] Raycast interactions + prompt UI
9. [ ] Pickupable object
10. [ ] Inventory
11. [ ] Basic storage container
12. [~] First ventilation section (greybox L-duct in `Greybox_Movement` — no modular prefabs yet)
13. [ ] Entering and exiting vents
14. [ ] Basic mixing machine
15. [ ] First ingredient ScriptableObjects
16. [ ] First RecipeDefinition
17. [ ] Ingredient consumption
18. [ ] Product generation
19. [ ] Basic customer
20. [ ] One complete sale
21. [ ] Cosmic Credits
22. [ ] First upgrade
23. [ ] Connect hideout to the alien plaza
24. [ ] Simple security drone
25. [ ] First playable build
