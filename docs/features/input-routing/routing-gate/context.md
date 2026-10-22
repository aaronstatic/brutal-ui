# input-routing/routing-gate - Context & Decisions

**Last Updated:** 2026-10-22 23:51
**Current Phase:** Done (phases 1 to 3 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The feature docs exist (`tasks.md`, `context.md`, `inbox.md`).
- ✅ event-model, pointer-routing, and keyboard-focus are committed on `main` (commits `1647228`, `9f006e9`, and `22f2597`). The branch `feat/input-routing/routing-gate` holds this feature.

- ✅ Phase 1, tasks 1.1 to 1.7: the gate struct, `RoutingGate`, the input pass in `Update`, the docs, and the 8 tests. The verifier gave PASS and Demo: PASS. Solution 2279 tests, 0 failed, 2 skipped (no display). See "Phase 1 Results".

- ✅ Phase 2, tasks 2.1 to 2.9: the HelloPanel state rules, the gate label, the smoke input, and the gate line. The verifier gave PASS and Demo: PASS. `scripts/test-linux.sh` PASS. Smoke: draw commands 2, vertices 288, `Update and Draw 0`, gate P 58 and K 117 of 117. See "Phase 2 Results".

- ✅ Phase 3, tasks 3.1 to 3.7: the full-queue gate (`ThousandElementAllocationTests` 14), the three plants, the technical design, and the changelog. The verifier gave PASS and Demo: PASS, with plant 1 in a scratch copy of the repository. Solution 2281 tests, 0 failed, 2 skipped (no display). See "Phase 3 Results".

- ✅ The cross-phase review (`code-review.md`) ran 21 mutation probes and found 8 findings (3 MEDIUM, 5 LOW). 7 are fixed, and CR-8 stays a follow-up with its reason. The verifier of the fixes gave PASS. See "Review Fixes".

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification".
- 📋 `/autorun-epic` commits the feature and runs the epic rollup.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Input/UiRoutingGate.cs` - The public gate struct (phase 1, new).
- `src/BrutalUi.Core/Tree/UiContext.cs` - `RoutingGate`, the drain call in `Update`, and the doc comments (phase 1).
- `src/BrutalUi.Glfw/GlfwUiInput.cs` - The class remarks only (phase 1).
- `samples/HelloPanel/` - The demo: the state rules, the gate label, and the smoke input (phase 2).

### Related Files
- `tests/BrutalUi.Core.Tests/Input/RoutingGateTests.cs` - The gate tests (phase 1).
- `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` - The queue case (phase 3).
- `docs/features/input-routing/routing-gate/implementation.md` - The implementation plan.

---

## Important Decisions

### Decision R1: The caller accepted option A of each open scope question
**Date:** 2026-10-22
**Context:** The Notes of the plan hold four open scope questions: the phase 1 demo, the time when the pointer value changes, the pointer share with an ImGui window, and the ring size.
**Decision:** The plan uses option A of each question. `/autorun-epic` states that the user confirmed the four answers.
**Rationale:** The user accepted the plan with these answers.
**Impact:** The phase 1 demo is the `RoutingGateTests` unit test. The pointer value is a live read of the last pointer hit. The adapter gets no pointer switch, and the game uses `gate.PointerOverUi || io.WantCaptureMouse`. The ring keeps 256 entries, and the docs name `EventQueueCapacity` (this closes CR-5 of pointer-routing).

### Decision R2: keyboard-focus is committed
**Date:** 2026-10-22
**Context:** Notes, Conflicts 1 of the plan says that keyboard-focus has no commit yet.
**Decision:** The conflict is closed. All three siblings are on `main`, and this branch starts from it.
**Impact:** Phase 1 starts with no wait.

### Decision R3: The hash of EventWalk.cs at the start
**Date:** 2026-10-22
**Context:** The plan asks each phase to check that `src/BrutalUi.Core/Input/EventWalk.cs` does not change.
**Decision:** The expected SHA-256 is `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (Decision R3 of pointer-routing).
**Impact:** Each verifier compares the hash with this value.

### Decision R4: The review fixes keep the class totals of the plan
**Date:** 2026-10-22
**Context:** CR-1 of the review asks for two new tests in `RoutingGateTests`. The demo line of phase 1 reads a total of 8, and Q2 of the Definition of Done counts 8 + 2 new Core tests. Decision R4 of keyboard-focus set the same rule for its review fixes.
**Decision:** The two CR-1 checks are blocks inside an existing test of `RoutingGateTests`. No fix adds a `[Fact]`, and no test changes its name.
**Rationale:** The demo lines, the human testing list, and the Verification Method keep their totals.
**Impact:** `RoutingGateTests` keeps 8 tests, and `BrutalUi.Core.Tests` keeps 1449.

### Decision R5: CR-8 of the review stays open as a follow-up
**Date:** 2026-10-22
**Context:** CR-8 of the review: the comment on line 14 of `src/BrutalUi.Core/BrutalUi.Core.csproj` says "DrainEvents stays internal until routing-gate". Criteria I1 and I2 of the plan say that the csproj does not change.
**Decision:** The csproj does not change in this feature. The next feature that can change the csproj fixes the comment.
**Impact:** One follow-up line under "Next Steps".

---

## Phase 1 Results

**Date:** 2026-10-22

### Test counts

| Project | Before (total / skipped) | After (total / skipped) |
|---------|--------------------------|-------------------------|
| BrutalUi.Core.Tests | 1439 / 0 | 1447 / 0 |
| BrutalUi.Framework.Tests | 1 / 0 | 1 / 0 |
| BrutalUi.Glfw.Tests | 20 / 2 | 20 / 2 |
| BrutalUi.ImGui.Tests | 243 / 0 | 243 / 0 |
| BrutalUi.Markup.Tests | 568 / 0 | 568 / 0 |
| Solution | 2271 / 2, failed 0 | 2279 / 2, failed 0 |

- The 2 skipped tests of `BrutalUi.Glfw.Tests` need a display. The machine has no display, before and after.
- The demo command (`--filter-class '*RoutingGateTests'`) gives a total of 8 and `failed: 0`.
- `FocusOwnerTests` 8, `ClickFocusTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `PointerStateEdgeTests` 8, `PointerAllocationTests` 3, `FocusAllocationTests` 3, and `ThousandElementAllocationTests` 12 pass with no edit.
- `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.

### Checks

- The SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` is `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`. It matches Decision R3.
- The `DrainEvents` diff check: `git diff -- src/BrutalUi.Core/Tree/UiContext.cs` changes only doc comments, the `DrainEvents();` call with its comment, and the `RoutingGate` property. The 120 lines of the method body are the same as in `HEAD`.
- `src/BrutalUi.Core/BrutalUi.Core.csproj` has no change. `BrutalUi.Core` has no `using` of a GLFW namespace.
- No existing test changed. The drain in `Update` finds an empty ring in each existing test, because each test drains before it calls `Settle()`.

### Blast radius

- `UiContext.Update` now drains the ring first. Each caller of `Update` gets the input pass: the tests, `samples/HelloPanel`, the debugger, and the games. The stale rules now run in each `Update`, also with an empty ring.
- A test or a game that pushes an event and then calls `Update` before `DrainEvents` now sees the drain in `Update`. No existing test does this.
- The re-entrant guard: a handler that calls `Update` gets `InvalidOperationException` from the existing `_draining` check. Before this phase, such a call ran the passes inside the drain.

### Decisions of phase 1 that the brief did not settle

- D1-1: The demo test calls `Settle()` (an `Update` with an empty ring) before the push. The hit test reads the layout of the last `Update`, so a tree with no layout hits nothing. The test calls no `DrainEvents`.
- D1-2: The remarks of `Update` also change the paragraph "The style pass runs first" to "The style pass runs after the input pass". The old sentence is false after this phase.
- D1-3: The remarks of `Update` hold the ring rule of Decision 4 in a separate paragraph: the input pass drains each frame, the default is `DefaultEventQueueCapacity`, and a game with long hitches sets `CreateInfo.EventQueueCapacity`. Task 1.4 does not list it, but Decision 4 asks for it.
- D1-4: The remarks of `Update` state that the stale rules run also with an empty ring. `TheStaleRulesClearTheGateAtTheNextUpdate` tests it.
- D1-5: The `GlfwUiInput` paragraph also states "Poll the events before the update". This gives the frame order of the adapter.
- D1-6: The remarks of `UiRoutingGate` name `GlfwUiInput.KeyboardEnabled`, `io.WantCaptureMouse`, and `io.WantCaptureKeyboard` in `<c>` tags, not in `cref`. Core has no reference to the GLFW adapter or to ImGui.
- D1-7: The tests make some cases stronger than the brief. `PointerOverUiFollowsTheLastHit` checks the cursor leave from a `true` value, and it checks the targeted move from a `false` value and from a `true` value. `ANonPickableRootPassesThePointerToTheGame` first shows that a pickable root gives `true` over its gap. `AnUpdateFromAHandlerThrowsAndTheContextStaysValid` pushes a second entry and checks that it stays in the ring, and that the style pass did not run. `TheStaleRulesClearTheGateAtTheNextUpdate` checks that the gate keeps the old values between the removal and the next `Update`.
- D1-8: The comment in `Update` wraps over two lines to stay inside 120 columns. The text is the text of task 1.3.

### Follow-up (outside the scope of phase 1)

- `UiContext.QueuedEventCount` still says "The next drain walks them". The sentence is true, but it can name the input pass of the next `Update`.
- The remarks of `PointerPosition` ("Routing-gate reads it (A6)") and `ActiveElement` ("routing-gate read this member") are stale. `RoutingGate` reads neither member.
- The comment in `src/BrutalUi.Core/BrutalUi.Core.csproj` line 14 says "DrainEvents stays internal until routing-gate". `DrainEvents` stays internal after routing-gate (Decision 2), so the comment is stale. The acceptance of phase 1 forbids a change of the csproj.

---

## Phase 2 Results

**Date:** 2026-10-22

### Native dependency note (AGENTS.md rule)

HelloPanel now references `BrutalUi.Glfw`. `BrutalUi.Glfw` uses the native GLFW library of `Ahwoo.BRUTAL.Glfw`. The sample referenced that package already, so no native dependency is new. The csproj gets one project reference and no package. `Directory.Packages.props` has no change.

### Gate results

- `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln` gives total 2279, failed 0, succeeded 2277, skipped 2 (no display). The totals are the same as after phase 1.
- `scripts/test-linux.sh; echo "exit=$?"` gives `test-linux: PASS build, test, gpu, smoke (120 frames) in 22 s` and `exit=0`.
- The four lines of `artifacts/test-linux/smoke.log`:
  - `HelloPanel: 120 frames rendered.`
  - `HelloPanel: last frame: draw commands 2, vertices 288.`
  - `HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.`
  - `HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.`
- The draw command count stays 2. The vertex count for section 14 is 288 (before: 128). The difference is 160 vertices: 40 glyphs of 4 vertices for `Pointer over UI: yes. Keyboard focus in UI: yes.`
- The SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` is `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`. No file under `src/` or `scripts/` changes in this phase.

### Edge runs (the smoke command of the script with another frame count)

| `--frames` | Line 2 | Line 3 | Line 4 | Exit |
|------------|--------|--------|--------|------|
| 4 | draw commands 2, vertices 288 | 0, frame 192, in 1 frames | pointer 0, focus 1, of 1 | 0 (the input check needs 2 measured frames) |
| 5 | draw commands 2, vertices 284 | 0, frame 384, in 2 frames | pointer 1, focus 2, of 2 | 0 |
| 121 | draw commands 2, vertices 284 | 0, frame 22656, in 118 frames | pointer 59, focus 118, of 118 | 0 |

### The selector text of task 2.7

`CompoundSelector.ToString` writes the class text and then the state text from its `StateTexts` table, with no space. The parsed state rules give `.hello-panel:hover` and `.hello-panel:focus`. `FindRule` finds exactly one rule for each text.

### Decisions of phase 2 that the brief did not settle

- D2-1: The smoke input lives in a private method `PushSmokeInput(UiContext, Element, int)`. `RunFrames` calls it as the first statement of the UI region, so the allocation gate measures it. The method adds the rectangle positions of each ancestor of the panel. For this tree, the sum is the panel plus the root, as the brief states.
- D2-2: The parity of the move uses `framesRendered` before the increment (the frame index). Frame 3, the first measured frame, is odd. Thus 120 frames give 58 even measured frames (4 to 118).
- D2-3: The three events of one frame share one `Glfw.Time` read.
- D2-4: The offset of the move in an odd frame is the constant `SmokeOffPanelOffset = 8f`.
- D2-5: The loop reads the gate in the UI region into a local. The measured block at the end of the frame adds the two counts, next to the other sums.
- D2-6: The gate label starts with an empty `Label.CreateInfo` and then `SetText(PointerYesFocusYes)`. Thus the longest text has one source, the `u8` literal. The buffer grows from 32 to 64 bytes in `Build`, before the first frame.
- D2-7: `IsPickable = false` and `IsFocusable = true` use object initializers.
- D2-8: `Program.cs` needs no alias for `Glfw`. The file lives in `namespace HelloPanel`, and `using BrutalUi.Glfw;` imports types only, not the nested namespace. Thus `Glfw` resolves to `Brutal.GlfwApi.Glfw`. The build gives no error.
- D2-9: The vertex count depends on the parity of the frame count. The last frame draws the gate of the frame before it. With an even frame count, that gate has the pointer over the panel (288 vertices). With an odd count, it has not (284 vertices, the 3 glyphs of `no.` in place of the 4 glyphs of `yes.`). Section 14 names 288 for `--frames 120`.
- D2-10: The test of task 2.7 also checks that the two state rules have no transition. The tree of the test keeps the old shape with one label. The gate label and the pickable flags do not change a style value, so the old checks stay valid.
- D2-11: The comment at the `KeyboardEnabled` line states that the flag of the last ImGui frame is the newest value before the poll.
- D2-12: The `Program` remarks add a paragraph on the real input and the smoke input. The exit code 1 now also covers a smoke run in which the synthetic input did not reach the UI. The text of the first three lines and of the allocation line has no change.

### Follow-up (outside the scope of phase 2)

- Phase 3 records the fourth line, the vertex count 288, and the input check in section 14 of the technical design.
- The input check has no plant proof in this phase. Phase 3 task 3.4 runs plants on the allocation gate only. By the code, a plant that removes the `PushSmokeInput` call must give `exit=1` and the input line. No run checked it.

---

## Phase 3 Results

### Group A

**Test counts.** The solution held 2279 tests before this group and holds 2281 after it (2279 passed, 0 failed, 2 skipped). `ThousandElementAllocationTests` holds 14 tests (was 12), with `failed: 0`. The queue gate `UpdateDrainsAFullQueueWithNoAllocation` measures 0 bytes, the same dropped count before and after, and `QueuedEventCount` 0.

**Real counts of `TheQueueStepDoesRealWork`** (one queue step from `LeafKind`, 16 blocks): 32 moves, 16 scrolls, 16 downs, 16 ups, 48 key downs, 16 key ups, and 16 chars on the grid slots. The counts equal the expected list with no change. After the step, `FocusedElement` is grid slot 0, both gate values are `true`, `QueuedEventCount` is 0, and `DroppedEventCount` is 0.

**Plant results.**

| Plant | Change | Test exit | Failed gates | Smoke exit and stderr |
|-------|--------|-----------|--------------|-----------------------|
| 1 | `GC.KeepAlive(new object());` first in the `RoutingGate` getter | 2 | `ThousandElementAllocationTests.UpdateDrainsAFullQueueWithNoAllocation` (only this test) | 1, `HelloPanel: Update and Draw allocated 2808 bytes after warm-up. The limit is 0 bytes.` |
| 2 | the same line first in `DrainEvents` | 2 | 16 tests in 3 projects: 14 in Core (all the allocation gates, including the 4 of `ThousandElementAllocationTests`), 1 in ImGui (`LabelUpdateAndDrawAllocateNothingAfterWarmUp`), and 1 in Markup (`AParsedSheetAllocatesNothingInUpdate`) | 1, `HelloPanel: Update and Draw allocated 2808 bytes after warm-up. The limit is 0 bytes.` |
| 3 | the `PushSmokeInput` call in `RunFrames` commented out | not run | not run | 1, `HelloPanel: the synthetic input did not reach the UI.` |

**`cmp` results.** `UiContext.cs` equals its copy after the restore of plant 1 and after the restore of plant 2. `Program.cs` equals its copy after the restore of plant 3. The `git diff --stat` of the two files equals the stat before the plants (`Program.cs` 242, `UiContext.cs` 90, 285 insertions, 47 deletions).

**End checks.** The build gives `0 Warning(s)` and `0 Error(s)`. `scripts/test-linux.sh` gives `test-linux: PASS build, test, gpu, smoke (120 frames) in 13 s` and `exit=0`. The SHA-256 of `EventWalk.cs` is unchanged (`e84ec520...6307d8`). `ThousandElementTree.cs` has no change.

### Decisions of phase 3 that the brief did not settle

- D3-1: `QueueScene` holds each event as a `required` field and keeps the slot and row points. The block pushes the same `MoveSlot` field for events 1 and 16, and the same `Tab` field for events 9 and 14.
- D3-2: The counter delegates cover the bubble handlers only (`OnPointerMove`, `OnPointerDown`, `OnPointerUp`, `OnScroll`, `OnKeyDown`, `OnKeyUp`, `OnChar`). No capture handler is set.
- D3-3: The move at the stat row point reaches no slot handler. The 2 moves per block come from events 1 and 16.
- D3-4: The brief reads "16 times 2 moves, 1 scroll, ..." as the counts of one block times the 16 blocks. The real totals confirm this reading.
- D3-5: Plant 1 fails one test only (exit 2). Plant 2 fails each gate, because each `Update` calls `DrainEvents`. The test exit code of `dotnet test` is 2 for a failed test.
- D3-6: The smoke stderr line of the allocation plants reads 2808 bytes in both plants, and it ends with `The limit is 0 bytes.`

### Group B

**Docs.** `docs/technical-design.md` changed in sections 4 (Overview diagram, Frame order), 5 (The drain), 9 (Flow items 2, 5, 7, 8), and 14 (the class count of `ThousandElementAllocationTests`, a new bullet for `RoutingGateTests`, a new bullet for the queue case with the three plants, and the HelloPanel smoke bullet with four lines). `CHANGELOG.md` has one line under `### Added` and one line under `### Changed`. The `grep` for "does not call the drain", "does not drain", and "does not call it" finds no line that says `Update` does not drain. `GlfwUiInput.cs` has no such line.

**The five demo runs** (`--filter-class '*ThousandElementAllocationTests'`, the last three lines of each run): `succeeded: 14` in all five runs (durations 6.1 s, 6.1 s, 8.7 s, 5.9 s, 6.1 s). A sixth run with a total filter gave `total: 14` and `failed: 0`.

**Final gates.**
- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2281, failed 0, succeeded 2279, skipped 2 (no display).
- `scripts/test-linux.sh; echo "exit=$?"`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 32 s` and `exit=0`. The smoke log holds the four expected lines (vertices 288, bytes 0, pointer 58, focus 117, of 117).
- The SHA-256 of `EventWalk.cs` is unchanged (`e84ec520...6307d8`). This group changed no source file.

**Lint scores** (per 100 words, limit 2.5): `technical-design.md` 1.52, `CHANGELOG.md` 1.76, `context.md` 1.57 (after the Group B text), `implementation.md` 1.73. No file needed a fix.

**Decisions of group B that the brief did not settle.**
- DB-1: Section 9 item 5 names HelloPanel in one added sentence. Section 14 gives the smoke input as "a synthetic pointer move and a Tab in each frame", with the move over the panel in even frames.
- DB-2: The new bullets of section 14 sit after the eighth allocation gate (focus) bullet. The old sentence "The class `ThousandElementAllocationTests` has 12 tests" now says 14 and points to the queue case.
- DB-3: The sentence "The game calls no drain" replaced "A game does not call it" in section 5, so that the phrase grep stays clean.
- DB-4: The still-pointer limit is a sub-bullet of item 8 of section 9. The pointer rule of Decision 5 is a sub-bullet of item 7.
- DB-5: The `test-linux.sh` smoke frame number differs from earlier runs (20928 now, 22464 before). The frame number is a report only.

---

## Review Fixes

The cross-phase review (`code-review.md`) ran 21 mutation probes and found 8 findings (3 MEDIUM, 5 LOW). Four fix groups ran, one file each. The verifier of the fixes (`review-verify.md`) gave PASS: build 0 warnings and 0 errors, 2281 tests with 0 failed and 2 skipped, `RoutingGateTests` 8, `ThousandElementAllocationTests` 14, `scripts/test-linux.sh` PASS, the hash of `EventWalk.cs` unchanged.

- CR-1 (MEDIUM, `review-g1.md`): two blocks in `AnEventThatAHandlerPushesWaitsForTheNextUpdate` guard the read of the `roots` span after the drain. A handler removes a root, and a handler adds a root. Under M04 the test fails with `NullReferenceException`. No new `[Fact]` (Decision R4).
- CR-2 (MEDIUM, `review-g2.md`): the HelloPanel smoke run also exits with code 1 when each measured frame reads the pointer over the UI. A pickable root (M21) now fails the smoke run.
- CR-3 (MEDIUM, `review-g3.md`): the remarks of `Update` state the limit of the stale rules between two contexts. The advice now reads "Call `Update` on the old context". The full fix is a follow-up.
- CR-4 and CR-5 (LOW, `review-g3.md`): stale doc texts in `UiContext.cs`, and no internal member in the public remarks of `RoutingGate`. Comment lines only.
- CR-6 and CR-7 (LOW, `review-g4.md`): sections 4 and 14 of the technical design.
- CR-8 (LOW): not fixed. Criteria I1 and I2 keep the csproj unchanged (Decision R5). A follow-up line under "Next Steps".

---

## Gotchas & Learnings

### 1. A smoke check that tests only for 0 cannot see a wrong habit
**Problem:** The smoke run failed only when a gate count was 0. A pickable root made the pointer count equal to the measured count, and the run still passed (CR-2).
**Solution:** The run also fails when each measured frame reads the pointer over the UI, because the odd frames move off the panel.
**Lesson:** When a demo has an "off" frame on purpose, check both ends of the count: above 0 and below the total.

### 2. A frame-order rule needs a handler that changes the tree
**Problem:** No test failed when the `roots` span moved before the drain (M04 of the review). Each test pushed events that only changed states.
**Solution:** A handler that removes a root and a handler that adds a root now guard the order (CR-1).
**Lesson:** When a pass moves in front of another pass, add a test where the first pass changes the input of the second pass.

### 3. A drain in each Update makes an old edge case reachable
**Problem:** The stale rule of one context can clear a state that another context set (CR-8 of pointer-routing). Before this feature, no library code ran the drain, so a game could not reach the case.
**Solution:** The remarks of `Update` state the limit and the advice. The full fix is a follow-up (CR-3 of the review).
**Lesson:** When a feature adds the first caller of a path, read the known limits of that path again.

---

## API Integration

- (see the section "Public API (end state)" of the plan)

---

## State Management

- `UiContext.RoutingGate` reads `_pointerHit` and `_focusOwner` at each read. The drain, the focus calls, and the stale rules write those two fields.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Input/RoutingGateTests.cs`
- The frame order of `Update`, the two gate values, the layout-root rule, the stale rules, the re-entrant `Update`, and a push from a handler.

### Allocation Tests
- The queue case of `ThousandElementAllocationTests` (phase 3).

### Sample and Smoke Tests
- `ParsedSheetAllocationTests.TheHelloPanelSheetGivesTheOldValues` and the HelloPanel smoke run of `scripts/test-linux.sh` (phase 2).

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a unit test marks `Right` of the pointer tree focusable, adds the hover sheet, pushes a pointer move over `Right` at (180, 80) and a Tab key down, both with no target, and calls `Update(0f)` only, with no `DrainEvents` call. It reads `QueuedEventCount` 0, `PseudoStates.Hover` and `PseudoStates.Focus` on `Right`, the hover fill in `Right.Style.BackgroundColor`, and `RoutingGate` with `PointerOverUi` and `KeyboardFocusInUi` both `true`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*RoutingGateTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 2 demo: the developer runs `scripts/test-linux.sh; echo "exit=$?"` and reads `test-linux: PASS build, test, gpu, smoke (120 frames)` and `exit=0`. The file `artifacts/test-linux/smoke.log` holds `HelloPanel: bytes after warm-up: Update and Draw 0, ...` and the new line `HelloPanel: gate: pointer over UI in <P> frames, keyboard focus in UI in <K> frames, of 117 measured frames.` with `P` and `K` both above 0.
- [ ] HelloPanel with a display (F9 of the plan): on a Linux or Windows desktop, run `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. Move the mouse over the panel: the panel fill changes, and the label reads "Pointer over UI: yes". Outside the panel, it reads "no". Press Tab: the panel border turns yellow, and the label reads "Keyboard focus in UI: yes". Click outside the panel: the focus clears.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 14 with `failed: 0`. A planted allocation in the getter of `UiContext.RoutingGate` makes `dotnet test --solution brutal-ui.sln` exit non-zero and makes the HelloPanel smoke run exit with code 1.
- [ ] Windows run (routing-gate, Q7 of the plan): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- The input pass and the gate read allocate 0 bytes after warm-up. The queue case and the smoke run guard it.

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the items of "Needs Human Verification", including the HelloPanel run with a display and the Windows run.
2. `/autorun-epic` commits the feature, merges it into `main`, and runs the epic rollup.

### Follow-ups (later features)
1. CR-8 of the review (Decision R5): the next feature that can change `src/BrutalUi.Core/BrutalUi.Core.csproj` replaces the comment on line 14 ("DrainEvents stays internal until routing-gate") with "The adapter tests of BrutalUi.Glfw call the internal drain and read the last hit."
2. CR-3 of the review and CR-8 of pointer-routing: the full fix of the cross-context stale rule needs the owner context on the element. Each `Update` now runs the stale rules, so a game with two contexts can reach the case. Give it priority in the next input feature.

---

## Open Questions

- (none open. See Decision R1.)

---

## Session Notes

### 2026-10-22 22:49
- `/autorun-epic` called `/autorun-feature input-routing/routing-gate`. The feature docs exist. Phase 1 starts.
- Phase 1: the work of tasks 1.1 to 1.7 is done. The build gives 0 warnings and 0 errors. The solution gives 2279 tests, 0 failed, and 2 skipped. No existing test changed.
- Phase 1 verify (`phase-1-verify.md`): PASS, Demo: PASS. Hash of `EventWalk.cs` matches. The `DrainEvents` body has no change. Inbox: no line.
- Phase 2: the work of tasks 2.1 to 2.9 is done. The smoke run gives `exit=0`, 0 bytes after warm-up, and the gate counts 58 and 117 of 117. See "Phase 2 Results".
- Phase 2 verify (`phase-2-verify.md`): PASS, Demo: PASS. `GlfwUiInput` comes after both ImGui backends, and `KeyboardEnabled` comes before `Glfw.PollEvents()`. Inbox: no line.
- Phase 2 follow-up: no run proves the "synthetic input did not reach the UI" exit yet. The phase 3 plant brief adds a third plant for it.
- Phase 3, groups A and B (`phase-3.md`, `phase-3-g2.md`): the queue case, three plants (plant 3 proves the no-work exit), the technical design, and the changelog. A group A agent created a wrong scratch path `/tmp/claude-1000/-home-aaronstatic/dev/brutal-ui` and removed it. The parent checked that no other folder changed.
- Phase 3 verify (`phase-3-verify.md`): PASS, Demo: PASS. Plant 1 in a scratch copy gave `dotnet test` exit 2 (one failed gate) and smoke exit 1. Inbox: no line.

### 2026-10-22 23:51 Cross-phase review and close
- The review (`review.md`) ran 21 mutation probes and wrote 8 findings (3 MEDIUM, 5 LOW) into `code-review.md`.
- Four fix groups ran (`review-g1.md` to `review-g4.md`). The verifier of the fixes (`review-verify.md`) gave PASS. See "Review Fixes".
- Decisions R4 (the review fixes keep the class totals) and R5 (CR-8 stays a follow-up) are new.
- The inbox holds no line. No bug is promoted.
- The next session: a person runs the items of "Needs Human Verification". `/autorun-epic` commits the feature and runs the epic rollup.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
