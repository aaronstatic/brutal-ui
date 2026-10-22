# input-routing/routing-gate - Code Review

**Reviewed:** 2026-10-22
**Reviewer:** Agent (cross-phase review of phases 1 to 3)
**Rules Applied:** 1. The exact frame order, 2. The gate agrees with the states, 3. No new path in the drain, 4. Zero allocation after warm-up, 5. A visible demo, 6. No GLFW in Core, 7. A small public API, 8. Re-entrancy and exceptions, 9. Test quality, 10. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 8 findings, 8 resolved (7 fixed, CR-8 kept as a follow-up with its reason. The verifier of the fixes gave PASS: `review-verify.md`)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 3 | CR-1, CR-2, CR-3 |
| LOW | 5 | CR-4, CR-5, CR-6, CR-7, CR-8 |

The feature is in good shape. `Update` runs the drain as its first statement, and the roots span comes after it. The gate reads the two fields that hold `Hover` and `Focus`. The body of `DrainEvents` has no change, and the `_draining` guard throws for a re-entrant `Update`. The queue gate measures 0 bytes on a full ring. No probe found a HIGH problem. The two MEDIUM test gaps are a rule with no test (the roots span after the drain) and a smoke check that accepts a pointer count equal to the measured count. The third MEDIUM finding is CR-8 of pointer-routing. The drain in each `Update` makes that case reachable in each game with two contexts. A probe shows a gate that reads `true` while the hit element has no `Hover`, and no public doc states the limit. The LOW findings are doc drift.

**Baseline (repository, before the probes).**
- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2281, failed 0, succeeded 2279, skipped 2 (the two GLFW window tests, no display).
- The SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` is `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`. It matches Decision R3.
- `grep -rn 'using .*Glfw' src/BrutalUi.Core` gives no line. `git diff main --stat` lists no `.csproj` under `src/` or `tests/`.
- `git diff main` of `UiContext.cs` changes code in two places only: the `DrainEvents();` call in `Update` and the `RoutingGate` property. Each other change is a doc comment.

**Probe method.** The reviewer copied the repository to a scratch folder with `rsync` (no `bin`, `obj`, `.git`, or `artifacts`) and saved each original file in `../orig/`. The copy appended `$(Mut)` to `DefineConstants` in `Directory.Build.props`. Each mutation sat behind its own `#if Mnn` symbol. Each run built the touched project with `-p:Mut=Mnn -p:TreatWarningsAsErrors=false` and ran its tests with `--no-build`. The CSS probes used an edit and a restore with `cp`. The sample probes built HelloPanel and ran the smoke command of the brief from the copy root (`--frames 120`, `xvfb-run`). A scratch test class (`ReviewRootProbeTests`, 3 tests) gave the evidence for CR-1 and CR-3. The reviewer removed it before the final diff. The Core run of the copy with no symbol gave total 1451 (the 1449 real tests and 2 scratch tests) and failed 0. The third scratch test (E1) came later and passed alone (total 3, failed 0). The smoke run of the copy with no symbol gave exit 0 and the four expected lines (vertices 288, bytes 0, gate `58 / 117 / 117`).

**Probe list.** "Core" means `BrutalUi.Core.Tests` (1451 tests in the copy). Each test name drops the namespace.

| # | Mutation | Result |
|---|----------|--------|
| M01 | `Update`: the `DrainEvents()` call removed | Core exit 2, 9 real tests failed: `RoutingGateTests` (7 of 8: all but `TheGateIsFalseWithNoInput`), `ThousandElementAllocationTests.TheQueueStepDoesRealWork`, `ThousandElementAllocationTests.UpdateDrainsAFullQueueWithNoAllocation` |
| M02 | `Update`: the drain moved after the style pass | Core exit 2: `RoutingGateTests.UpdateRunsTheInputPassBeforeTheStylePass`, `RoutingGateTests.AnUpdateFromAHandlerThrowsAndTheContextStaysValid` |
| M03 | `Update`: the drain moved after the layout pass | Core exit 2: the same 2 tests as M02 |
| M04 | `Update`: the `roots` span read moved before the drain | Core 0 failed. Solution 0 failed of 2281 real tests (finding CR-1). Only the 2 scratch probes failed: a handler that calls `RemoveRoot` gives `NullReferenceException` in the layout pass, and a handler that calls `AddRoot` leaves the new root with a width of 0 |
| M05 | `RoutingGate`: the two values swapped | Core exit 2: `PointerOverUiFollowsTheLastHit`, `ANonPickableRootPassesThePointerToTheGame`, `KeyboardFocusInUiFollowsTheFocusOwner`, `AnUpdateFromAHandlerThrowsAndTheContextStaysValid`, `AnEventThatAHandlerPushesWaitsForTheNextUpdate` |
| M06 | `RoutingGate`: `PointerOverUi` always `true` | Core exit 2: `TheGateIsFalseWithNoInput`, `PointerOverUiFollowsTheLastHit`, `ANonPickableRootPassesThePointerToTheGame`, `TheStaleRulesClearTheGateAtTheNextUpdate` |
| M07 | `RoutingGate`: `PointerOverUi` from `_hasPointerPosition` | Core exit 2: `PointerOverUiFollowsTheLastHit`, `ANonPickableRootPassesThePointerToTheGame`, `TheStaleRulesClearTheGateAtTheNextUpdate` |
| M08 | `RoutingGate`: `KeyboardFocusInUi` always `false` | Core exit 2: `UpdateRunsTheInputPassBeforeTheStylePass`, `KeyboardFocusInUiFollowsTheFocusOwner`, `TheStaleRulesClearTheGateAtTheNextUpdate`, `ThousandElementAllocationTests.TheQueueStepDoesRealWork` |
| M09 | `UiRoutingGate`: the constructor swaps its two arguments | Core exit 2: the same 5 tests as M05 |
| M10 | `DrainEvents`: the `_draining` check removed | Core exit 2: `RoutingGateTests.AnUpdateFromAHandlerThrowsAndTheContextStaysValid`, `EventWalkEdgeTests.ANestedDrainThrows` |
| M11 | Queue block: the first Tab and the Shift+Tab removed | Core exit 2: `ThousandElementAllocationTests.TheQueueStepDoesRealWork` |
| M12 | Queue block: all three Tab entries removed | Core exit 2: `ThousandElementAllocationTests.TheQueueStepDoesRealWork` |
| M13 | `QueueStep`: the push loop removed (the ring stays empty) | Core exit 2: `ThousandElementAllocationTests.TheQueueStepDoesRealWork` |
| M14 | `QueueStep`: the gate read removed | Core exit 2: `ThousandElementAllocationTests.TheQueueStepDoesRealWork` (the 0-byte gate cannot fail, because the mutation removes work) |
| M21 | HelloPanel: `root.IsPickable = true` | Smoke exit 0, `gate: pointer over UI in 117 frames, keyboard focus in UI in 117 frames, of 117 measured frames.` (finding CR-2) |
| M22 | HelloPanel: `panel.IsFocusable = false` | Smoke exit 1, `keyboard focus in UI in 0 frames`, and `HelloPanel: the synthetic input did not reach the UI.` on standard error |
| M23 | HelloPanel: the `KeyboardEnabled` line removed | Smoke exit 0, gate line `58 / 117 / 117`. Equivalent mutant: HelloPanel opens no ImGui window, so `io.WantCaptureKeyboard` stays `false`, and `KeyboardEnabled` keeps its default `true`. The grep of Verification step 5 guards the line (I4) |
| M24 | HelloPanel: the odd-frame move goes over the panel | Smoke exit 0, gate line `117 / 117 / 117` (finding CR-2) |
| M25 | HelloPanel: the no-work exit check removed | Smoke exit 0, gate line `58 / 117 / 117`. Equivalent for a correct run: the check acts only when a second fault stops the input. Plant 3 of task 3.4 proves that it acts. The reviewer did not repeat that plant |
| M26 | `HelloScreen.css`: the `:focus` rule removed | Markup exit 2: `ParsedSheetAllocationTests.TheHelloPanelSheetGivesTheOldValues` |
| M27 | `HelloScreen.css`: the `:hover` value changed to `#283045` | Markup exit 2: `ParsedSheetAllocationTests.TheHelloPanelSheetGivesTheOldValues` (expected `#283044FF`, actual `#283045FF`) |
| E1 | Evidence probe, no mutation: an element moves from context A to context B, B hovers it, then A updates | Passes as written: `B.RoutingGate.PointerOverUi` is `true`, `B.PointerHit` is the element, and the element has no `Hover`, also after one more move over it (finding CR-3) |

21 mutations ran. 3 survived with no test or smoke failure (M04, M21, M24). 2 more are equivalent mutants with a reason (M23, M25).

**The test matrix of `RoutingGateTests` (rule 9).** Each test fails under at least one mutation, so no test is dead.

| Test | Failed under |
|------|--------------|
| `UpdateRunsTheInputPassBeforeTheStylePass` | M01, M02, M03, M08 |
| `TheGateIsFalseWithNoInput` | M06 |
| `PointerOverUiFollowsTheLastHit` | M01, M05, M06, M07, M09 |
| `ANonPickableRootPassesThePointerToTheGame` | M01, M05, M06, M07, M09 |
| `KeyboardFocusInUiFollowsTheFocusOwner` | M01, M05, M08, M09 |
| `TheStaleRulesClearTheGateAtTheNextUpdate` | M01, M06, M07, M08 |
| `AnUpdateFromAHandlerThrowsAndTheContextStaysValid` | M01, M02, M03, M05, M09, M10 |
| `AnEventThatAHandlerPushesWaitsForTheNextUpdate` | M01, M05, M09 |

---

## Findings

### CR-1: No test guards the read of the roots span after the drain
**Severity:** MEDIUM
**Rule:** 1. The exact frame order (and 9. Test quality)
**File:** `tests/BrutalUi.Core.Tests/Input/RoutingGateTests.cs` (the rule lives in `src/BrutalUi.Core/Tree/UiContext.cs:578-580`)
**Problem:** Decision 2 and the Data Flow of the plan put the `roots` span read after the drain, because a handler can add or remove a root. Mutation M04 moved the read before the drain:
```csharp
ReadOnlySpan<Element> roots = _roots.AsSpan();   // M04: before the drain
DrainEvents();
```
All 2281 tests of the solution passed. The mutant is not equivalent. `ElementStorage.RemoveAt` writes `null` into the last slot, so a stale span holds a null root, and the layout pass throws `NullReferenceException`. A root that a handler adds gets no layout in that `Update`, so `Draw` draws it with a rect of 0. Two scratch tests showed both results.
**Fix:** Add two tests to `RoutingGateTests` (the class then holds 10 tests. Update the count in section 14 under CR-7):
```csharp
[Fact]
public void AHandlerThatRemovesARootRunsTheLayoutOnTheNewRoots()
{
    PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(AHandlerThatRemovesARootRunsTheLayoutOnTheNewRoots));
    Element second = new();
    second.Inline.Width = 40;
    second.Inline.Height = 40;
    tree.Context.AddRoot(second);
    tree.Settle();

    tree.Left.OnPointerMove = (ref PointerEvent e) => tree.Context.RemoveRoot(second);
    tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
    tree.Context.Update(0f);   // a stale span throws NullReferenceException here

    Assert.False(second.IsRoot);
}

[Fact]
public void AHandlerThatAddsARootGetsItsLayoutInTheSameUpdate()
{
    // The same scene. The handler calls AddRoot(added), and the test asserts added.Layout.Rect.Z == 40f.
}
```
**Effort:** Low

### CR-2: The smoke input check accepts a pointer count equal to the measured count
**Severity:** MEDIUM
**Rule:** 5. A visible demo (and 9. Test quality)
**File:** `samples/HelloPanel/Program.cs:259-268`
**Problem:** Decision 7 and Decision 8 make the root not pickable and put the move off the panel in each odd frame, so the gate reads `false` outside the panel. Section 9 item 5 of the technical design names HelloPanel as the example of the layout-root rule. The check fails only on a count of 0:
```csharp
if (frameLimit != NoFrameLimit
    && result.MeasuredFrames >= 2
    && (result.PointerOverUiFrames == 0 || result.KeyboardFocusInUiFrames == 0))
```
M21 (`root.IsPickable = true`) and M24 (each move over the panel) both gave exit 0 with `pointer over UI in 117 frames ... of 117 measured frames`. The smoke run thus cannot see the wrong habit that Decision 7 rejects, and the unit test `ANonPickableRootPassesThePointerToTheGame` does not cover the sample tree.
**Fix:** Fail also when each measured frame reads the pointer over the UI. With 2 measured frames or more, at least one frame is odd, and an odd frame moves off the panel. Update the `Run` remarks and the comment with the same rule:
```csharp
if (frameLimit != NoFrameLimit
    && result.MeasuredFrames >= 2
    && (result.PointerOverUiFrames == 0
        || result.PointerOverUiFrames == result.MeasuredFrames
        || result.KeyboardFocusInUiFrames == 0))
```
Optional: run the focus part from 1 measured frame. The Tab of frame 0 gives the focus, so `--frames 4` can check it too (open risk 6). Section 14 then needs the new sentence (CR-7).
**Effort:** Low

### CR-3: The cross-context stale rule now runs in each Update, and no public doc states it
**Severity:** MEDIUM
**Rule:** 2. The gate agrees with the states (and 10. The docs)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:524-536` (the remarks of `Update`), with the advice text at lines 1211-1213, 1359-1360, and 1400-1401
**Problem:** Before this feature, no library code called the drain, so a game never ran the stale rules. Now each `Update` runs them. Evidence probe E1: an element moves from context A to context B. B hovers it. Then A updates, and the stale rule of A clears the `Hover` bit that B set. B keeps the element in `_pointerHit`, so `B.RoutingGate.PointerOverUi` reads `true` while the element has no `Hover`. A new move over the same element does not set the bit again, because `SetHover` returns early for the same element. The gate and the state disagree until the pointer leaves the element. The full fix needs the owner context on the element and is out of scope (CR-8 of pointer-routing). But the limit appears only in the internal remarks of `DrainEvents`, `DropStalePointerState`, and `DropStaleFocus`. Their advice is "Drain the old context after you remove an element", and a game cannot call the internal drain.
**Fix:** Add one paragraph to the remarks of `Update`, and change the three advice sentences from "Drain the old context" to "Call `Update` on the old context":
```csharp
/// <para>
/// Limit of the stale rules (CR-8 of pointer-routing): the input pass of one context can clear
/// <see cref="PseudoStates.Hover"/>, <see cref="PseudoStates.Active"/>, or <see cref="PseudoStates.Focus"/> that
/// another context set. When you move an element to another context, call <see cref="Update(float)"/> on the old
/// context before the new context updates. Otherwise <see cref="RoutingGate"/> of the new context can read
/// <see langword="true"/> while the element has no <see cref="PseudoStates.Hover"/>.
/// </para>
```
Add a follow-up line for the full fix to `context.md`, so that the next input feature gives it priority.
**Effort:** Low

### CR-4: Four doc texts of UiContext still describe the old drain or a routing-gate reader
**Severity:** LOW
**Rule:** 10. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:662, 720, 735-737, 1206-1208`
**Problem:** Open risk 1 lists three of these texts. The review found one more pair.
- Line 662, a public summary: "The next drain walks them." A game does not call a drain.
- Line 720: "Routing-gate reads it (A6)." `RoutingGate` does not read `PointerPosition`.
- Line 735: "Only the tests of `BrutalUi.Core.Tests` and routing-gate read this member." `RoutingGate` does not read `ActiveElement`.
- Lines 737 and 1207-1208: "Drain the queue each frame." A game cannot call the drain. It calls `Update`.
**Fix:**
```csharp
/// <summary>Gets the number of events in the event ring. The input pass of the next <see cref="Update(float)"/> walks them.</summary>
/// <remarks>The value follows the pointer events only.</remarks>                       // PointerPosition
/// Only the tests of <c>BrutalUi.Core.Tests</c> read this member. ...                  // ActiveElement
/// ... Call <see cref="Update(float)"/> each frame to keep the ring below its capacity. // lines 737 and 1207
```
**Effort:** Low

### CR-5: The public remarks of RoutingGate point to an internal member
**Severity:** LOW
**Rule:** 10. `AGENTS.md` and the docs (and 7. A small public API)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:784-786`
**Problem:** The public property says "`PointerOverUi` comes from `<see cref="PointerHit"/>`". `PointerHit` is internal, so the reader of the package docs gets a link to a member that does not exist for them. The summary of `UiRoutingGate.PointerOverUi` already states the rule with no internal name.
**Fix:**
```csharp
/// <see cref="UiRoutingGate.PointerOverUi"/> comes from the hit of the last pointer or scroll entry with no target,
/// the element with <see cref="PseudoStates.Hover"/>. <see cref="UiRoutingGate.KeyboardFocusInUi"/> comes from
/// <see cref="FocusedElement"/>.
```
**Effort:** Low

### CR-6: Section 4 of the technical design still says that the style pass runs first
**Severity:** LOW
**Rule:** 1. The exact frame order (and 10. The docs)
**File:** `docs/technical-design.md:140-141, 157`
**Problem:** Line 157: "`UiContext.Update` calls the internal `UiContext.UpdateStyles` first, before the layout pass." After this feature, the input pass runs first. Line 141 says that the input pass will "update the routing gate", but the gate is a live read (Decision 3), and no pass updates it. Line 140 puts the framework step above the input pass, but item 2 of "Frame order" and the code run the input pass first.
**Fix:** Line 157: "`UiContext.Update` calls the internal `UiContext.UpdateStyles` after the input pass and before the layout pass." Line 141: "Input pass: drain the event ring, route the events, and move the states". Line 140: move the framework line below the input line, or add "(Phase 3, not built yet)" so that the order does not read as a claim.
**Effort:** Low

### CR-7: Section 14 names an old UI region of the HelloPanel gate
**Severity:** LOW
**Rule:** 10. `AGENTS.md` and the docs
**File:** `docs/technical-design.md:1095`
**Problem:** The bullet says that the gate fails "when the UI region (`ViewportSize`, `Update`, and `Draw`) allocates". The region now also holds the smoke push, the gate read, and the label update (Decision 8 and the `RunFrames` remarks). With CR-1 and CR-2, the test count and the text of the input check also change.
**Fix:** "when the UI region (the smoke input, `ViewportSize`, `Update`, `Draw`, the gate read, and the label update) allocates". After CR-2, add: "It also exits with code 1 when each measured frame reads the pointer over the UI, because the odd frames move off the panel." After CR-1, change "`RoutingGateTests` (8 tests)" to 10.
**Effort:** Low

### CR-8: The csproj comment of BrutalUi.Core still says that DrainEvents stays internal until routing-gate
**Severity:** LOW
**Rule:** 10. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/BrutalUi.Core.csproj:14`
**Problem:** "DrainEvents stays internal until routing-gate." Decision 2 keeps `DrainEvents` internal after routing-gate, so the comment is false. Criteria I1 and I2 forbid a change of the csproj in this feature.
**Fix:** Do not change the csproj in this feature. Add a follow-up line to `docs/features/input-routing/routing-gate/context.md`. The next feature that can change the csproj writes: "The adapter tests of BrutalUi.Glfw call the internal drain and read the last hit." Alternatively, the caller records a comment-only exception of I1 in `context.md` and makes the change now.
**Effort:** Low

---

## What's Already Good

- **The frame order (rule 1).** `DrainEvents();` is the first statement of `Update`. The roots span comes after it. The style, transition, layout, and clear passes keep their order. M01, M02, and M03 each fail the demo test and the re-entrancy test.
- **The gate agrees with the states (rule 2).** `RoutingGate` reads `_pointerHit` and `_focusOwner`, the same two fields that `SetHover` and `SetFocusOwner` write. M05 to M09 each fail 3 to 5 tests. The only disagreement is the cross-context case of CR-3.
- **No new path in the drain (rule 3).** The hash of `EventWalk.cs` matches. The diff of `UiContext.cs` has no code change in `DrainEvents`.
- **Zero allocation (rule 4).** The queue gate drains 100 full rings of 256 entries with 0 bytes, no drop, and an empty ring at the end. The block covers each event kind, a click focus, Tab, Shift+Tab, a Tab with no owner, a press over nothing, and a cursor leave. The counter delegates exist before the measure. The measured body has no lambda, no boxing, and no string. `UiRoutingGate` is a two-`bool` struct, and `GateText` returns `u8` literals. The smoke run measures 0 bytes in the full UI region.
- **Re-entrancy and exceptions (rule 8).** M10 fails `AnUpdateFromAHandlerThrowsAndTheContextStaysValid` and `ANestedDrainThrows`. The guard message names `Update`. The `finally` block clears `_draining`, and the test shows the next `Update` drains the rest.
- **No GLFW in Core (rule 6) and a small API (rule 7).** Core has no GLFW `using` and no csproj change. `UiRoutingGate` has two public members and an internal constructor. `RoutingGate` is the only new public member of `UiContext`. `DrainEvents` stays internal, and `PushEvent` keeps four overloads.
- **The demo (rule 5).** The two state rules set different members, so the hover and the focus show at the same time. M26 and M27 fail `TheHelloPanelSheetGivesTheOldValues`. M22 makes the smoke run exit 1.
- **Test quality (rule 9).** Each of the 8 tests of `RoutingGateTests` fails under at least one mutation. Each of M11 to M14 fails `TheQueueStepDoesRealWork`.
- **Open risk 3 (the still pointer).** Not a finding. Decision 3 accepts it. The remarks of `RoutingGate` and item 8 of section 9 state the limit.
- **Open risk 4 (an exception stops `Update` before the style pass).** Not a finding. The remarks of `Update` state it, the entries after the thrown entry stay in the ring, and the test proves that the next `Update` runs the full frame. The plan rates it Low and Medium, and the game sees the exception.
- **Open risk 6 (`--frames 4` skips the input check).** Not a separate finding. Decision 8 names the 2-frame limit, the `Run` remarks and section 14 state it, and `scripts/test-linux.sh` runs 120 frames. CR-2 gives an optional split of the check.
- **Open risk 7 (the vertex count follows the parity of the frame count).** Not a finding. D2-9 explains it, and section 14 gives 288 for an even count and 284 for an odd count.
- **Open risk 8 (the label shows the gate of the frame before).** Not a finding. Decision 9 accepts it, and the comment at the `SetText` call states it.
- **Open risk 9 (the gate label buffer).** Not a finding. `Label.EnsureCapacity` only grows the buffer. `Build` sets the 48-byte longest text, so the buffer grows from 32 to 64 bytes before the first frame. Each later text fits. The smoke run measures 0 bytes in 117 frames.
- **M23 and M25.** Equivalent mutants with the reasons in the probe list. They are not findings.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Fix group (file) |
|-------|---------|--------|--------|------------------|
| 1 | CR-1 | MEDIUM: a frame-order rule with no test, and a crash under the mutant | Low | `tests/BrutalUi.Core.Tests/Input/RoutingGateTests.cs` |
| 2 | CR-2 | MEDIUM: the smoke run cannot see the layout-root rule of the sample | Low | `samples/HelloPanel/Program.cs` |
| 3 | CR-3 | MEDIUM: a gate that disagrees with the state in a reachable case, with no public doc | Low | `src/BrutalUi.Core/Tree/UiContext.cs` |
| 4 | CR-4 | LOW: stale texts | Low | `src/BrutalUi.Core/Tree/UiContext.cs` |
| 5 | CR-5 | LOW: a public doc link to an internal member | Low | `src/BrutalUi.Core/Tree/UiContext.cs` |
| 6 | CR-6 | LOW: the frame order text of section 4 | Low | `docs/technical-design.md` |
| 7 | CR-7 | LOW: the section 14 text (do it after CR-1 and CR-2) | Low | `docs/technical-design.md` |
| 8 | CR-8 | LOW: a stale csproj comment (follow-up, I1) | Low | `docs/features/input-routing/routing-gate/context.md` |

---

## Progress

- [x] CR-1: No test guards the read of the roots span after the drain (`review-g1.md`: two blocks inside `AnEventThatAHandlerPushesWaitsForTheNextUpdate`, a handler that removes a root and a handler that adds a root. No new `[Fact]`, per Decision R4 in `context.md`, so `RoutingGateTests` keeps 8 tests. Under M04 the host test fails with `NullReferenceException` in `LayoutEngine.Calculate`.)
- [x] CR-2: The smoke input check accepts a pointer count equal to the measured count (`review-g2.md`: `Run` also fails when `PointerOverUiFrames` equals `MeasuredFrames`, with the stderr line "HelloPanel: the pointer read over the UI in each measured frame. The off-panel frames did not reach the game." M21 now gives exit 1. The optional focus check from 1 measured frame is not done: Decision 8 keeps the 2-frame rule.)
- [x] CR-3: The cross-context stale rule now runs in each Update, and no public doc states it (`review-g3.md`: the remarks of `Update` hold the limit paragraph. The advice of `DrainEvents`, `DropStalePointerState`, and `DropStaleFocus` reads "Call `Update` on the old context". The full fix is a follow-up in `context.md`.)
- [x] CR-4: Four doc texts of UiContext still describe the old drain or a routing-gate reader (`review-g3.md`: the `QueuedEventCount` summary, the `PointerPosition` and `ActiveElement` reader sentences, and the two "Drain the queue each frame" sentences. Comment lines only.)
- [x] CR-5: The public remarks of RoutingGate point to an internal member (`review-g3.md`: the remarks name the hit of the last pointer or scroll entry with no target, and no internal member.)
- [x] CR-6: Section 4 of the technical design still says that the style pass runs first (`review-g4.md`: the overview lists the input pass first, and `UpdateStyles` runs "after the input pass and before the layout pass".)
- [x] CR-7: Section 14 names an old UI region of the HelloPanel gate (`review-g4.md`: the full UI region, the CR-2 exit rule, and the root check of CR-1. `RoutingGateTests` stays at 8 tests, per Decision R4.)
- [x] CR-8: The csproj comment of BrutalUi.Core still says that DrainEvents stays internal until routing-gate (not fixed in this feature: criteria I1 and I2 of the plan say that the csproj does not change. Decision R5 in `context.md` and a follow-up line under "Next Steps" give the fix to the next feature that can change the csproj.)
