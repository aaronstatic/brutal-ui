# panels-and-modes/mode-set - Code Review

**Reviewed:** 2026-11-08
**Reviewer:** Senior C# engine reviewer (cross-phase review of phases 1 to 4)
**Rules Applied:** 1. The Core slot (call order, re-entry guard, exception rule, `=` rule, null-slot cost — Decision 2), 2. The diff (mark, leave, layout, enter, map, history, real-slot read, stay with no join, move, slot order — Decisions 6 and 7, A4 and A5), 3. The defer rule and SetMode semantics (Decision 3, Q1), 4. The register contract (Decision 4 and 5), 5. The history ring (Decision 9, A3), 6. The error rule and guards (Decision 8), 7. The 0-byte rule (A7, Decision 10), 8. Integration with siblings (SlotContainer, PanelController.EnsureRoot, UiActionMap, game roots), 9. API clarity and docs, 10. Test strength (a rule pinned by a test AND a plant). The Vercel React rules do not apply to C#.
**Status:** Resolved - 2 findings, 2 resolved (2026-11-08: CR-2 added the `SetMode`-then-`Back` case to `ASetModeAfterBackInOneFrameWins` and a plant on the `_targetFromBack` line of `SetMode` failed exactly it; CR-1 reworded the line comment of `UiManager.cs` and the Q4 gate grep now prints no line)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 0 | - |
| LOW | 2 | CR-1, CR-2 |

The feature does what the plan asks. The Core slot (`OnBeforeUpdate`) runs first in `Update`, inside the `_beforeUpdate` guard, with a `finally` clear, before `DrainEvents()`; a nested `Update` throws; an exception leaves `Update` before the input pass and keeps the ring. `UiManager.Apply` runs the five steps of Decision 6 in order (mark, leave, layout, enter, map) then history, and the leave pass reads the real slot of each panel (`e.Slot`), so a diff after a failed diff removes each stale panel. The stay path skips the unmount when `ReferenceEquals(slot, e.Next)`, so a panel in the same slot joins nothing. The layout swap removes and adds only the manager's own roots; game roots stay in place (line 700 to 707). The map switch activates always and deactivates only when the target has no map. `ModeHistory` is a fixed `int[]` ring with drop-oldest and no forward step. Every register check comes before a change, and the `_applying` guard covers the whole diff and is cleared in `finally`. The 0-byte tests measure `SetMode`, the diff, the back step, and an idle frame at 0 bytes with a fixed sum assert. Rules 1 to 9 have no finding; the feature is genuinely clean. Two LOW findings remain: a gate-vs-comment mismatch (CR-1) and one untested frame order (CR-2).

### Baseline (observed, not copied from context.md)

- `dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2821, failed 0, succeeded 2818, skipped 3 (the three GLFW display tests).
- Class counts (built binaries, `--filter-class`): `BeforeUpdateTests` 6, `UiManagerTests` 6, `ModeDiffTests` 9, `LayoutSwapTests` 5, `ModeActionMapTests` 5, `ModeHistoryTests` 9, `UiManagerRegisterTests` 8. Feature adds 48 tests. Each `failed: 0`.
- I1 diff scope vs base `7c80ca5`: Core holds only `UiContext.cs` (changed) and `UiBeforeUpdateHandler.cs` (new); Framework holds `UiManager.cs` (new), `ModeHistory.cs` (new), `PanelDefinition.cs` (non-doc diff lines: none). No project file, no `scripts/`, no `samples/`, no existing test file changed.
- I2 order: `UiContext.cs` reads `OnBeforeUpdate` at line 699, before `DrainEvents();` at line 715. `SlotContainer.Find` appears only at line 452 (inside `Register(ModeDefinition)`) and line 566 (inside `HoldsDuplicateSlot`, called from `Register(LayoutDefinition)`).
- Q4 forbidden tokens: none in code. One false positive on the gate grep (see CR-1).
- STE lint (`--fail-over 2.5`): `technical-design.md` 1.53, `CHANGELOG.md` 1.62, `implementation.md` 1.96, `context.md` 1.16. All pass.
- Section 4 (lines 140, 156), section 10 (`### Mode set`, lines 1044 to 1060), section 14 (`**Mode tests.**`), and the `CHANGELOG.md` `## [Unreleased]` Added and Changed lines are present and correct; the `ModeHistory waits` sentence is gone.
- 14 plants recorded in `context.md` (tasks 1.6, 2.7, 3.4, 4.4); each shows 0 `error CS` lines before the result and a non-zero exit with the named tests failing.
- Blocker (not a code finding): the `scripts/test-linux.sh` smoke step fails on a host Xvfb/NVIDIA-EGL segfault. The build, test, gpu, and loader steps pass; the owner accepted the split evidence. No repo file causes it.

---

## Findings

### CR-1: The Q4 gate grep prints a benign comment line, so Verification Method step 5 is not empty

**Severity:** LOW
**Rule:** 9 (API clarity and docs) — the DoD gate does not match the code text
**File:** `src/BrutalUi.Framework/Panels/UiManager.cs`, line 258

**Problem:** Definition of Done Q4 forbids `lock`, `IDisposable`, `event`, `ToString(`, `(object)`, `new List`, `Stack<`, and `Queue<` in *code*. Verification Method step 5 runs the gate as `grep -nE '...|\bevent\b|...' UiManager.cs ModeHistory.cs | grep -v '///'` and says "Expect no line (Q4)." That filter drops doc comments (`///`) but not line comments (`//`). The comment at line 258 contains the bare word "event":

```csharp
// The ring rejects a capacity of 0, so map 0 to the default first, as UiContext maps it for the event ring.
```

The code is clean; only a prose word trips the literal grep. A verifier following the method gets a non-empty result and must stop to judge it benign. This is a small process fault, not a behavior fault.

**Fix:** Reword the comment to avoid the word, or widen the gate. Reword (preferred, one line, prose only):

```csharp
// The ring rejects a capacity of 0, so map 0 to the default first, as UiContext maps it for its queue.
```

No code line changes. (Alternatively, extend the step-5 filter to `| grep -vE '///|//'`, but then the word still sits in the source.)

**Effort:** Low

### CR-2: No test pins `Back()` after `SetMode()` in one frame

**Severity:** LOW
**Rule:** 10 (test strength), for rule 5 (the history ring) and rule 3 (SetMode semantics)
**File:** `tests/BrutalUi.Framework.Tests/Panels/ModeHistoryTests.cs`; the path lives at `UiManager.cs` lines 505 to 530 (`SetMode`, `SetBackTarget`) and `ModeHistory.cs` lines 84 to 99 (`Back`)

**Problem:** The plan and its tests pin the order "`SetMode` after `Back()` in one frame" (`ASetModeAfterBackInOneFrameWins`): `SetMode` clears `_targetFromBack`, so the back entry stays dropped and the apply pushes. The mirror case — `Back()` first, then `SetMode()` of a different mode, in the same frame, before the `Update` — has no test. `SetBackTarget` sets `_target` and `_targetFromBack = true` with no check of a pending target, and `SetMode` overwrites `_target` and clears the flag. So "`Back(); SetMode(X); Update`" must apply X, push the old current mode, and leave the popped entry dropped. A regression that made `SetMode` keep a stale `_targetFromBack` (so the apply of X added no history entry) would hide in this untested path. The reverse ordering is the one a HUD can hit (a menu-back key and a switch command in the same frame).

**Fix:** Add one assertion to `ASetModeAfterBackInOneFrameWins` or a small sibling case: start at C with history [A, B]; call `Back()` (target B, back flag set), then `SetMode(A)` (target A, flag cleared), then `Update`. Assert `CurrentMode` is A, `TargetMode` null, and `History.Count` is 2 (B dropped, C pushed by the non-back apply). Plant: remove the `_targetFromBack = false;` line in `SetMode` (line 519); the new case must fail with `Count` 1. Keep the class at 9 by folding it into `ASetModeAfterBackInOneFrameWins`.

**Effort:** Low

---

## What's Already Good

- The Core slot follows the `_binding` guard pattern exactly: check flag, throw on re-entry, read the delegate into a local, set, `try`, `finally` clear. `Update` reads `_roots` after the drain and the binding pass, so a root the handler adds is styled and laid out in the same call — `ARootThatTheSlotAddsGetsItsStyleAndLayoutInTheSameUpdate` pins it, plant 2 pins the order against `DrainEvents`.
- The leave pass reads the real slot (`e.Slot`), not the old placements, and the stay test reads `DirtyFlags.Style` on the staying root after the slot call only — plant 5 (unmount a staying panel) fails exactly that test. The move test confirms the unmount-then-mount order; plant 4 (drop the unmount) makes the moves throw and fails 11 tests, proving leave-before-enter is the only guard of the move path.
- Error recovery is genuinely pinned: `AFactoryExceptionLeavesUpdateAndTheNextUpdateFinishesTheDiff` proves `CurrentMode`/`TargetMode` stay, `_applying` clears through the `finally` (the second `Update` is not blocked by re-entry), and the retry finishes the tree; `ANewTargetAfterAFailedDiffShowsOnlyItsOwnPanels` proves the stale panel from a half diff unmounts.
- The ring is correct: drop-oldest (`_entries[_head] = mode; _head++`) is pinned by `AFullHistoryDropsTheOldestMode` and plant 9 (overwrite the newest) fails only that test; no-forward-step is pinned by `TheApplyOfABackStepAddsNoEntry` and plant 8 (push on a back step) fails four tests. The 0-byte cycle runs past capacity and asserts `History.Count` equals `DefaultHistoryCapacity`, proving a full ring adds nothing.
- The two 0-byte tests warm 2 rounds, call `AllocationGate.ReleaseAllocationContext()` right before the measure, run 100 iterations, assert 0 bytes AND a fixed sum (`CycleSum` 17, `IdleFrameSum` 3), so an empty body cannot pass; the measured body uses only `u8` literals, `ReferenceEquals`, and `Children.Count` — no lambda, string, array, or `Assert`. Plants 10 to 13 put a `GC.KeepAlive(new object())` at each of `Apply`, the handler, `SetMode`, and `Back()` and fail the right test each time.
- Every register check comes before a change: `Register(LayoutDefinition)` rejects null, applying, and a duplicate name before it calls the factory, and checks the built root (null, parent, duplicate slot) before the table write, so a rejected layout leaves the name free; `HoldsDuplicateSlot` walks pre-order and stops at each slot, relying on `SlotContainer.Find` for identity.
- `ModeDefinition` (panel-definition) already rejects a panel named twice in a mode, so the mark pass's one-`Next`-per-panel invariant holds and the diff cannot put a panel in two slots. Integration is clean: the manager adds and removes only its layout roots (`TheManagerKeepsTheRootsOfTheGame` keeps a game root at `Roots[0]` across two swaps), calls `EnsureRoot` per mount (factory runs once across A,B,A,B), and switches the map before the input pass (`AKeyInTheFrameOfTheChangeReachesTheNewMap`).
- Docs: all twelve remark topics appear in the `UiManager` class remarks and section 10; `OnBeforeUpdate` has its `=` rule, null-slot note, and the frame-order sentence is correct; the `DefaultSlot` remark now states the loader use and that `UiManager` does not read it, and the `PanelDefinition.cs` diff is doc lines only.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-2 | Low | Low | It pins a documented frame order (rule 3 and rule 5) that a bug could hide in; do it in `ModeHistoryTests`, no source change. |
| 2 | CR-1 | Low | Low | Prose-only; it clears the DoD step-5 gate so a verifier reads a clean "no line." |

---

## Progress

- [x] CR-1: The Q4 gate grep prints a benign comment line, so Verification Method step 5 is not empty (UiManager.cs line 258)
- [x] CR-2: No test pins `Back()` after `SetMode()` in one frame (ModeHistoryTests.cs; UiManager.cs lines 505 to 530)
