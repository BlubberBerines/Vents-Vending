# Station foundation review — 10 November 2026

Scope: harden the existing Station Concourse Ring gameplay, not add systems or art.

## Fixes

- Reject expired, already-resolved or stale customer handoffs before removing stock or paying credits.
- Close modal screens before loading state so UI cannot keep references to replaced customers or vessels.
- Clear the current mixer when a loaded older save has no mixer record.
- Retain the whole mixer vessel when ingredients cannot all fit in pockets, rather than dropping unsaved overflow.
- Fit the player capsule and camera to destination headroom before enabling the controller after teleport/load.
- Preserve customer position and destination on save/load, rather than restarting an already-arrived visitor at the remote entrance. Optional fields remain compatible with older saves.
- Save player-created dropped stacks, including item ID, count, quality, position and rotation; repeated loads replace rather than duplicate them. Save format v8 migrates older saves with an empty drop list. Authored pickups remain separate.
- Validate numeric quality values instead of accepting undefined enum values from JSON.
- Detect crawl mouths beyond the capsule edge so a standing player can automatically lower before the lounge header blocks entry.
- Handle clock steps past an entire meeting as a miss without spawning a customer for an expired window.
- Remove per-frame order-list snapshots and allocation-heavy recipe matching from idle mixer updates.

## Evidence

- Final automated EditMode regression run: **269 passed, 0 failed, 0 skipped** (4.58 seconds of test execution).
- Three successful live integration cycles. Earlier diagnostic runs also missed deadlines while the game continued running during tool investigation; the final cycle paused between review commands, not during travel or processing, and completed with approximately 86 seconds remaining. No deadline tuning was applied.
- Each cycle uses PC accept/cart/checkout controls, the physical timed courier, CharacterController travel through the Den doors and maintenance ring into and out of the core, three timed mining strikes, the eight-second refinery and live mixer, sales UI, stock/payment/trust assertions, and save/load at multiple stages.
- The handoff test repositions the player to the customer to isolate transaction checks. These are integration cycles, **not three fully unassisted end-to-end playthroughs**. Public-access traversal is a separate check.
- 144 maintenance-ring samples: floor support and at least one clear walking lane at every sample. The parked courier occupies the centre line at 45 degrees but can be passed.
- 144 crawl-ring samples: no missing floor, missing ceiling, or crawl-capsule obstruction.
- Physical lounge route checked separately: both crate mantles, four opened hatches, crawl ramps into the lounge, then the complete return route. The return test found the probe/header issue and passed after the fix without a manual crawl toggle.
- Live edge checks passed: expired sale keeps stock/credits; full-pocket mixer empty keeps vessel; a saved crawl-space load immediately uses a 0.7m capsule and remains movable; dropped Premium stock reloads once with its original count/quality.
- Den door checks passed: proximity alone leaves it shut; interaction opens it; occupancy holds it open; clearing it lets it close. Fan coast-down remains blocked, a stopped fan is passable, its occupied-passage restart interlock works, and hub light isolation preserves Den lighting.
- No missing scripts/references or duplicate inventory save IDs found in the scene.
- 10,000 repeated recipe matches allocated zero managed bytes after warm-up in the measured call path.
- Five-second moving-player Editor frame sample: 522 frames, mean 9.57 ms, 95th percentile 11.72 ms, max 20.96 ms. This includes tool activity and is **not a standalone-build performance certification**.
- QA uses an isolated save directory; normal player saves are not loaded or overwritten by these tests.

## Performance scan execution checklist

Focused source set: `OrderBoard`, `DeliveryQueue`, `RecipeDefinition`. Unity runtime compatibility takes precedence over recommendations requiring newer .NET runtimes.

| Recipe | Exact hit count |
|---|---:|
| IndexOf literal; Substring; StartsWith/EndsWith literal; Contains literal | 0 each |
| ToLower/ToUpper; Replace | 0 each |
| params | 1 |
| LINQ projection/filter/group | 5 |
| Any/All | 2 |
| List/Dictionary construction | 10 |
| Static readonly dictionary | 0 |
| Compiled regex; regex construction; GeneratedRegex | 0 each |
| Unsealed public/internal class | 0 |
| Sealed class | 5 of 5 classes |
| IEquatable | 0 (no relevant value type in the focused set) |
| Sync-over-async; ValueTask; stackalloc | 0 each |
| JsonSerializer; HttpClient | 0 each |
| ContainsKey; string.Format; ToCharArray | 0 each |
| ToArray | 1 (delivery unload boundary, not per-frame) |
| ArrayPool/byte buffers | 0 |

The skill's topic-specific collection reference was unavailable at its expected path; inline recipes and the critical-pattern reference were used. Remaining allocations in the focused set occur at configuration, checkout, save/load, unload or UI-description boundaries; no blanket LINQ rewrite was applied. Sibling mixer match paths were checked against the detailed recipe-validation path, with a regression test comparing their results. No async/regex/HTTP/JSON-library paths were present in this focused set.

### Findings classification

Moderate, fixed: two gameplay update paths created avoidable temporary collections (order tick and idle recipe status). Reused snapshots retain the original event-safe traversal behaviour; exact recipe matching uses a non-allocating path. The measurements above validate the recipe path only, not all game allocations.

| Severity | Count | Top issue |
|---|---:|---|
| Critical performance findings | 0 | None identified in focused scan |
| Moderate performance findings | 2, fixed | Per-frame temporary collections |
| Informational deferred optimizations | 0 | No speculative modern-.NET substitutions |

> AI-assisted scans can miss issues or produce false positives. Treat these findings as scoped evidence, not a substitute for standalone-build profiling and human playtesting.

## Foundation gate

Keep the feature freeze until three consecutive **unassisted** complete playthroughs pass, including physical access to the lounge, and a standalone-build smoke test. The bot integration cycles and separate public-route test are useful regression evidence but do not replace that gate. Sampled geometry checks do not establish that every branch, hatch, steam room and side room has been exhaustively played. No new gameplay features, balance changes or art expansion were added in this pass.
