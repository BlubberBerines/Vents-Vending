# VentNet: customer orders

## 0. Station-ring customer-flow amendment (10 November 2026)

The following user-requested station behaviour supersedes the original plaza flow below; the legacy greybox retains its earlier order source.

- Open **Tab** for Inventory, In-progress deals, and Customers. Storage interactions still use the transfer view. Deal entries hold the full order, quality requirement, location and chosen appointment; track exactly one active deal or leave all untracked.
- The compact top-right tracked card contains only the mugshot, name, ordered product/quantity and meeting location. Completion or a missed window clears tracking. The station clock is separate, top-left.
- New station games start with no known customers and no anonymous incoming orders. Talk to ambient passengers with **E**, then offer one carried finished product as a free sample. A good-quality liked sample recruits that named passenger as a regular. An unsuitable sample does not recruit; poor quality can be retried with a better batch. Repeated samples cannot farm trust.
- Each named contact has persistent species/appearance, trust, tried samples, likes and dislikes. Contacts who dislike the current product remain contacts, not regulars; the data supports offering another product later. Only recruited regulars request products they liked. Successful orders add 8 individual trust; missed appointments subtract 10. The PC's Rep meter is the separate existing global standing, not personal trust.
- The customer defines the location. The player books a future start time using the PC's **-30m / +30m** controls, up to one station day ahead. The customer appears during that window, then leaves after **150 real gameplay seconds** if the deal is not completed. Normal pause stops both the clock and meeting deadlines.
- A station day lasts **24 real minutes** (one real second per simulated minute), starting Day 1 at 08:00. Night shift is 20:00–06:00, with reduced lounge lighting; Den and emergency lighting remain independent. Timers explicitly distinguish real waiting duration from station time. This is station lighting, not an outdoor sun simulation.
- Save format v9 includes customer relationships and the tracked deal. The board's existing saved clock supplies station time. Older booked deals remain valid; new games require recruitment. Loading clears transient ambient passengers before rebuilding named visits, preventing duplicate identities.

Roadmap, not additional products delivered here: a broader catalogue, richer species/individual taste rules and dialogue, daily NPC routines, and deeper persuasion. Currently the existing Glarb-Juice is the available sample product; individual acceptance varies deterministically rather than rerolling on every visit.

Verification: 12 focused relationship/scheduling/tracking tests, plus the existing regression suite; live conversation/sample recruitment and rejection, PC booking, Tab tracking, saved relationship/clock restore, unique scheduled arrival, successful handoff and night-light changes were checked in isolated QA saves. Public drones were temporarily disabled for transaction/UI inspection and restored by ending that QA play session.

Expands **GDD §10.3 (Order System)** and **§10.1–10.2 (sales)**. Code is in `Assets/_Project/Scripts/Orders/`. The rules live in `OrderBoard` and are covered by EditMode tests in `OrderTests`.

## The loop

1. **A message arrives.** The pager chirps, a toast says "New VentNet message", and the HUD shows `[!] N new`. The den PC screen blinks.
2. **Read it on the den PC.** It's a beige CRT on the chamber's east wall. Each message gives:
   - the customer and species
   - the product, quantity and minimum quality
   - the price per unit
   - the meet spot and its risk
   - when they'll arrive (1:00–2:00 after you accept) and how long they'll wait (2:30)
3. **Accept or decline.** Declining costs nothing. Ignored messages expire after 2:00. You can run at most 2 deals at once, and only one per spot.
4. **The meet spot lights up** with a glowing ring and a light beam. The HUD tracks "Spot B · Glorbo · 2× Glarb-Juice · arrives in 0:45".
5. **The window opens.** The customer walks in through the plaza door to the spot and waits ("Psst. Got my order?").
6. **Hand-off.** It's fast, with no haggling. The price is agreed, and delivering better quality than asked pays that quality's price (GDD §10.2: "fast and satisfying").
7. **Miss the window** and they storm off. Trust drops.

## Meet spots (Central Plaza)

| Spot | Where | Risk | Why |
|---|---|---|---|
| A | Behind Glorp-Mart | Low | Behind the stall's back panel, out of the drone loop's sightlines |
| B | Under the Hologram | High | Dead centre, and the patrol drone passes overhead |
| C | Quiet Corner | Medium | Next to your vent; a pillar blocks part of the patrol |

Riskier spots pay more: +5% for medium, +10% for high.

## Numbers (`OrderRules`)

- **Timing:** first message after 12 s; then every 35–70 s, scaled by trust.
- **Limits:** up to 3 unread messages and 2 active deals.
- **Pay:** product sale price for the delivered quality × (1 + markup). The markup is about 80% of the customer's tolerance, plus the danger bonus, plus the trust bonus.
- **Trust:** starts at 50 and runs 0–100. A completed deal adds 6; a miss costs 12. Trust shifts pay by ±15% and message frequency by ±30%.

This stands in for the GDD's "reputation reward" and "failure penalty" until the full reputation system (GDD §9.3) exists.

## Decisions

- **The PC replaces a phone.** It's diegetic for a vent-dweller: a hacked maintenance terminal on a relay. Being away from the PC still gets you the pager chirp and HUD count, so you never miss that a message arrived.
- **Plaza customers come by appointment only.** The random plaza walk-in spawner is gone. The Concourse Lounge (behind the fan) keeps its walk-in VIPs for now.
- **Real-time seconds.** There's no station clock yet; when there is, windows map onto it.
- **Saved:** the inbox, deals, timers and trust are saved (save format v3). A customer who was waiting when you saved is waiting again when you load.

## Later

- VentNet orders for lounge clientele (richer, pickier, longer lead times).
- Repeat customers who remember your reliability.
- Bulk orders, and a delivery option once couriers exist (GDD §10, Blorb Runner).
- Hooking trust into the GDD §9.3 reputation layers.
