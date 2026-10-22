# input-routing/keyboard-focus - Context & Decisions

**Last Updated:** 2026-10-22 14:48
**Current Phase:** Done (phases 1 to 6 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The feature docs exist (`tasks.md`, `context.md`, `inbox.md`).
- ✅ event-model and pointer-routing are committed on `main` (commits `1647228` and `9f006e9`). The branch `feat/input-routing/keyboard-focus` holds this feature.

- ✅ Phase 1: the focus owner, `SetFocus`, `ClearFocus`, and the key and char route to the owner. The verifier gave PASS and Demo: PASS. Solution 2232 tests, 0 failed, 2 skipped (no display). See "Phase 1 Results".

- ✅ Phase 2: focus on click (`FocusOnPress` in the `PointerDown` branch of `RoutePointer`). The verifier gave PASS and Demo: PASS. Solution 2240 tests, 0 failed, 2 skipped (no display). See "Phase 2 Results".

- ✅ Phase 3: Tab and Shift+Tab (`FocusWalk`, `FocusNext`, `FocusPrevious`, and the Tab step after the walk). The verifier gave PASS and Demo: PASS. Solution 2252 tests, 0 failed, 2 skipped (no display). See "Phase 3 Results".

- ✅ Phase 4: the adapter keys and chars (`GlfwUiInput.PushKey`, `PushChar`, and `KeyboardEnabled`). The verifier gave PASS and Demo: PASS. Solution 2260 tests, 0 failed, 2 skipped (no display). Glfw.Tests 20 of 20 under `xvfb-run -a`. See "Phase 4 Results".

- ✅ Phase 5: disabled and stale focus (`AcceptsFocus` with `Disabled`, `FocusWalk.IsShown`, and `DropStaleFocus`). The verifier gave PASS and Demo: PASS. Solution 2268 tests, 0 failed, 2 skipped (no display). See "Phase 5 Results".

- ✅ Phase 6: the allocation gates (`FocusAllocationTests`, the extended `GlfwUiInputAllocationTests`), the plant check, and the docs. The verifier gave PASS and Demo: PASS. Solution 2271 tests, 0 failed, 2 skipped (no display). `scripts/test-linux.sh` PASS. See "Phase 6 Results".

- ✅ The cross-phase review (`code-review.md`) ran 58 mutation probes and found 6 findings (3 MEDIUM, 3 LOW). All 6 are resolved, and the verifier of the fixes gave PASS. See "Review Fixes".

- ✅ The draft plan of the next feature exists: `docs/features/input-routing/routing-gate/implementation.md` (3 phases, 23 tasks). It holds four open scope questions in its Notes.

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification", including the Windows run and the BRUTAL issue.
- 📋 `/autorun-epic` commits this feature and runs `input-routing/routing-gate`.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Tree/Element.cs` - `IsFocusable` and `AcceptsFocus` (phases 1 and 5).
- `src/BrutalUi.Core/Tree/UiContext.cs` - The focus owner, the key target, the click focus, Tab, and the stale rule (phases 1, 2, 3, and 5).
- `src/BrutalUi.Core/Input/FocusWalk.cs` - The Tab search with an explicit stack (phases 3 and 5).
- `src/BrutalUi.Glfw/GlfwUiInput.cs` - The key and char events and `KeyboardEnabled` (phase 4).

### Related Files
- `tests/BrutalUi.Core.Tests/Input/` - The focus test classes and the gate.
- `tests/BrutalUi.Glfw.Tests/` - The adapter key tests and the extended gate.
- `docs/features/input-routing/keyboard-focus/implementation.md` - The implementation plan.

---

## Important Decisions

### Decision R1: The caller accepted option A of each open scope question
**Date:** 2026-10-22
**Context:** The Notes of the plan hold four open scope questions: the phase 1 demo, the rule between ImGui and the UI focus for the keyboard, the element that gets the focus on a click, and whether a handler can keep the Tab.
**Decision:** The plan uses option A of each question. `/autorun-epic` states that the user confirmed the four answers.
**Rationale:** The user accepted the plan with these answers.
**Impact:** The phase 1 demo is the `SetFocus` and key unit test. A public `GlfwUiInput.KeyboardEnabled` switch exists, and the game sets it from `io.WantCaptureKeyboard`. A click focuses the hit element or its nearest ancestor that accepts focus. A Tab walks to the old owner first, and a stop prevents the focus move.

### Decision R2: event-model and pointer-routing are committed
**Date:** 2026-10-22
**Context:** Notes, Conflicts 1 of the plan says that pointer-routing has no commit yet.
**Decision:** The conflict is closed. Both features are on `main`, and this branch starts from it.
**Impact:** Phase 1 starts with no wait.

### Decision R3: The hash of EventWalk.cs at the start
**Date:** 2026-10-22
**Context:** The plan asks each phase to check that `src/BrutalUi.Core/Input/EventWalk.cs` does not change.
**Decision:** The expected SHA-256 is `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (Decision R3 of pointer-routing). Phase 1 records the value at its start.
**Impact:** Each verifier compares the hash with this value.

### Decision R4: The review fixes keep the class totals of the plan
**Date:** 2026-10-22
**Context:** The review findings CR-1 to CR-5 each asked for a guard test. The Definition of Done counts the tests of each class (Q2 of the plan).
**Decision:** Each guard check is a block inside an existing test of the same class. No fix adds a `[Fact]`.
**Rationale:** The demo lines, the human testing list, and the Verification Method keep their totals.
**Impact:** `BrutalUi.Core.Tests` has 1439 tests (1400 + 39), and `BrutalUi.Glfw.Tests` has 20, as the plan states.

### Decision R5: The owner gets its Focus bit back, and a disabled owner loses the keys in the same drain
**Date:** 2026-10-22
**Context:** CR-3 and CR-5 of the review. Code that writes `States` cleared `Focus` with no repair. A handler that disabled the owner left the owner for the later keys of the drain.
**Decision:** `DropStaleFocus` and the same-element path of `SetFocusOwner` set `Focus` on the owner again. The key and char branch of the target step drops an owner with `AcceptsFocus` false. A failed `SetFocus` on the owner clears the focus (CR-4).
**Rationale:** Each fix is one field check or one field write, with no allocation and no dirty bit when the state is right. The public API does not change.
**Impact:** The interactive-elements epic can write `States` on a focused widget. Decision 7 (the full stale rule at the start of the drain) stays as it is.

---

## Phase 1 Results

**Group 1 (the focus owner, the key route, and the tests):** `FocusOwnerTests.cs` is new. `Element.cs`, `UiContext.cs`, `UiEventHeader.cs`, and `PointerTree.cs` changed. `EventWalk.cs` did not change. No existing test file other than `PointerTree.cs` changed, and `PointerTree.cs` got additions only.

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` before phase 1 | total 2224, failed 0, succeeded 2222, skipped 2 (the two GLFW real-window tests, no display) |
| `dotnet test --solution brutal-ui.sln` after | total 2232, failed 0, succeeded 2230, skipped 2 |
| `BrutalUi.Core.Tests` before and after | total 1400, then total 1408. Failed 0 in each run |
| Demo: `--filter-class '*FocusOwnerTests'` | total 8, failed 0 |
| Event-model classes, no edit | `EventWalkTests` 13, `EventKindTests` 12, `EventQueueTests` 9, `EventWalkEdgeTests` 5, `EventAllocationTests` 3: 42 pass, 0 failed |
| Pointer and window classes, no edit | `HitTestTests` 6, `HitTestRuleTests` 11, `PointerHoverTests` 5, `PointerPressTests` 8, `PointerStateEdgeTests` 8, `ScrollRoutingTests` 4, `PointerAllocationTests` 3, `WindowCallbackTests` 4: 49 pass, 0 failed |
| SHA-256 of `EventWalk.cs` at the start and at the end | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (the value of Decision R3) |

**Blast radius (the shared code that this phase touches):**
- `UiContext.DrainEvents`: one new `else if` branch after the pointer branch of the target step. The pointer branch, the skip, the drop count, and the dispatch switch did not change. Each event of the drain goes through the step, so each routing test covers the change.
- The public API grows by four members: `Element.IsFocusable`, `UiContext.FocusedElement`, `UiContext.SetFocus(Element)`, and `UiContext.ClearFocus()`. No public member changed or went away.
- `PointerTree.cs` is linked by `BrutalUi.Glfw.Tests`. That project builds with 0 warnings and its tests pass.

**Group 1 decisions (the brief did not settle them):**
- P1-1: The key branch is an `else if` after the pointer branch, so the pointer branch keeps its text. The branch writes `entry.Header.Target` also when the owner is null. The write is harmless, because the existing skip then runs.
- P1-2: `SetFocus` checks `AcceptsFocus` before `IsInRootTree`, because the first check costs no parent walk. Both checks return `false` and change nothing. A call for the current owner returns `true` and marks nothing, because `SetFocusOwner` returns at once.
- P1-3: `SetFocusOwner` does not check the accept rule or the roots. Each caller checks them (`SetFocus` now, the click, Tab, and the stale rule in later phases). `ClearFocus` with no owner changes nothing.
- P1-4: The remarks of `PushEvent(in PointerEvent)` also changed. Its sentence "The drain skips an event of another type with no target" became false. The remarks of `PushEvent(in KeyEvent)` and `PushEvent(in CharEvent)` no longer say "The rules of `PushEvent(in PointerEvent)` apply", because those rules name the hit test. Each states its push rules and the focus rule itself.
- P1-5: The remarks of `DrainEvents` and `SetFocus` state a result of Decision 12: a handler that moves the focus changes the target of each later key or char entry of the same drain. Phase 1 has no test for it.
- P1-6: The tests use the key code 65 (the GLFW code of the key A) and the code point U+00E9. `SetFocusRejectsAnElementThatIsNotFocusable` also checks the `ArgumentNullException`. `SetFocusRejectsAnElementOutsideTheRoots` also checks a child of an orphan, a child of the root of another context, and that a rejected set keeps the stored owner. `SetFocusMovesTheFocusState` and `ClearFocusClearsTheState` check that a repeated call sets no `Style` dirty bit. `AKeyWithNoOwnerGetsNoWalk` pushes a key down, a key up, and a char.

**Risks and follow-up work that group 1 found:**
- Phase 5: an owner that leaves the roots keeps `Focus` and stays in `FocusedElement`. A key with no target then gets the owner as its target, and the existing `IsInRootTree` skip drops the entry with no walk. The stale rule of phase 5 must clear the owner.
- Phase 5: an owner that turns unfocusable keeps the focus until the stale rule runs. A set of `IsFocusable` sets no dirty bit and moves no focus.
- A caller that clears `PseudoStates.Focus` by hand through `States` leaves `FocusedElement` on the element. The next `SetFocus` of the same element returns at once and does not set the state again. This is the same limit as `Hover` (Phase 5 risk of pointer-routing). Keep it in the docs of phase 6.
- Phase 2: the click focus goes into the `PointerDown` branch of `RoutePointer`. The comment "Keyboard-focus adds the focus move to the hit element in the PointerDown branch" marks the place.

---

## Phase 2 Results

**Group 1 (the click focus, the doc comments, and the tests):** `ClickFocusTests.cs` is new. `UiContext.cs` changed. `EventWalk.cs` and `PointerTree.cs` did not change. No pointer test file changed.

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` before phase 2 | total 2232, failed 0, succeeded 2230, skipped 2 (Phase 1 Results) |
| `dotnet test --solution brutal-ui.sln` after | total 2240, failed 0, succeeded 2238, skipped 2 (the two GLFW real-window tests, no display) |
| `BrutalUi.Core.Tests` before and after | total 1408, then total 1416. Failed 0 in each run |
| Demo: `--filter-class '*ClickFocusTests'` | total 8, failed 0 |
| Phase 1 class, no edit | `FocusOwnerTests` 8: 8 pass, 0 failed |
| Pointer classes of the brief, no edit | `PointerPressTests` 8, `PointerAllocationTests` 3, `PointerStateEdgeTests` 8: 19 pass, 0 failed |
| Other pointer and window classes, no edit | `HitTestTests` 6, `HitTestRuleTests` 11, `PointerHoverTests` 5, `ScrollRoutingTests` 4, `WindowCallbackTests` 4: 30 pass, 0 failed |
| Event-model classes, no edit | `EventWalkTests` 13, `EventKindTests` 12, `EventQueueTests` 9, `EventWalkEdgeTests` 5, `EventAllocationTests` 3: 42 pass, 0 failed |
| SHA-256 of `EventWalk.cs` at the start and at the end | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (the value of Decision R3) |

**Blast radius (the shared code that this phase touches):**
- `UiContext.RoutePointer`: one call `FocusOnPress(hit)` in the `PointerDown` branch, after `PressButton`. The hit test, `SetHover`, `PressButton`, `ReleaseButton`, and the button mask did not change. Only an entry with no target reaches `RoutePointer`, so a caller target skips the click focus with no new check.
- Each `PointerDown` with no target now runs `FocusOnPress`. In the existing pointer tests no element is focusable and no owner exists, so `SetFocusOwner(null)` returns at once and changes no state. The pointer tests pass with no edit. `PointerAllocationTests` shows that the parent walk allocates nothing.
- No public member changed or went away. The doc comments of `RoutePointer`, `DrainEvents`, `PushEvent(in PointerEvent)`, and `FocusedElement` changed.

**Group 1 decisions (the brief did not settle them):**
- P2-1: `FocusOnPress` stops at the first element with `IsFocusable` and reads `AcceptsFocus` on that element only. When it does not accept the focus, the focus clears. The method does not look past it to a higher ancestor (Decision R1). The method makes one call `SetFocusOwner(candidate)`, so a press on the current owner returns at once and marks nothing.
- P2-2: The click focus does not depend on `Active`. A second button over another element moves the focus, while `Active` stays on the first element. `ARightButtonPressMovesTheFocus` checks it with the right and the middle button.
- P2-3: A button value above 31 also runs the rule, because `PressButton` skips only the mask bit for it. No test covers a value above 2.
- P2-4: The demo pushes a release over `Right` before the press over the gap, so the second press starts with no button down. The press over the gap hits the root, which is not focusable, so the focus clears. The root then holds `Hover` and `Active`.
- P2-5: Two more doc edits than the task names. The target-step paragraph of the `DrainEvents` remarks now says that a caller target "changes no state and no focus". The click focus paragraph of `DrainEvents` states that a press moves the target of each later key or char entry of the same drain (Decision 12). The remarks of `PushEvent(in ScrollEvent)` keep "The rules of `PushEvent(in PointerEvent)` apply", because the pointer remarks name the `PointerDown` type for the click focus.
- P2-6: Each test is a `[Fact]`, so the class total is 8. `APressOverNothingClearsTheFocus` checks the position outside the root and `(NaN, NaN)` in one test.
- P2-7: `APressOnAChildFocusesTheNearestFocusableAncestor` also marks the root focusable. The test then proves that the nearest focusable ancestor gets the focus, not a higher one. The handler of `Inner` reads `Right` as the owner during the walk.

**Risks and follow-up work that group 1 found:**
- Phase 5: when the `Disabled` rule goes into `AcceptsFocus`, a press on a disabled focusable element (or on its child) clears the focus. A disabled button inside a focusable panel does not give the focus to the panel (P2-1). `ADisabledElementTakesNoFocus` of phase 5 covers the press on the element.
- routing-gate (Conflicts 8): a press on an ImGui window over a UI element now also moves or clears the UI focus. The pointer side of the ImGui rule belongs to the gate.
- A game that pushes a press with a caller target gets no click focus. It calls `SetFocus` itself. The docs of phase 6 state this.
- A full ring that drops a `PointerDown` drops its focus move too. This is the same limit as `Active`.

---

## Phase 3 Results

**Group A (the focus walk, the stack, `FocusNext`, `FocusPrevious`, `TabKey`, and the Tab step):** `FocusWalk.cs` is new. `UiContext.cs` changed. `EventWalk.cs` did not change.

**Group B (the Tab tests and the record):** `TabNavigationTests.cs` is new. `PointerTree.cs` got one addition: `static KeyEvent Tab(bool shift)`. No existing member of `PointerTree.cs` changed. No event-model or pointer test file changed. No source file changed in group B.

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` before phase 3 | total 2240, failed 0, succeeded 2238, skipped 2 (Phase 2 Results and group A) |
| `dotnet test --solution brutal-ui.sln` after | total 2252, failed 0, succeeded 2250, skipped 2 (the two GLFW real-window tests, no display) |
| `BrutalUi.Core.Tests` before and after | total 1416, then total 1428. Failed 0 in each run |
| Demo: `--filter-class '*TabNavigationTests'` | total 12, failed 0 |
| Phase 1 and 2 classes, no edit | `FocusOwnerTests` 8, `ClickFocusTests` 8: 16 pass, 0 failed |
| Event-model classes, no edit | `EventWalkTests` 13, `EventKindTests` 12, `EventQueueTests` 9, `EventWalkEdgeTests` 5, `EventAllocationTests` 3: 42 pass, 0 failed |
| Pointer and window classes, no edit | `HitTestTests` 6, `HitTestRuleTests` 11, `PointerHoverTests` 5, `PointerPressTests` 8, `PointerStateEdgeTests` 8, `ScrollRoutingTests` 4, `PointerAllocationTests` 3, `WindowCallbackTests` 4: 49 pass, 0 failed |
| No method of `FocusWalk` calls itself | `Find` calls `Enter` and `Visit`. `Enter` and `Visit` call no method of `FocusWalk`. No `List<`, no `new Element[`, and no LINQ in `FocusWalk.cs` |
| `PushEvent` overloads | 4 public overloads (pointer, scroll, key, char) |
| SHA-256 of `EventWalk.cs` at the start and at the end | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (the value of Decision R3) |

**Blast radius (the shared code that this phase touches):**
- `UiContext.DrainEvents`: the walk step now sits inside `if (target is not null && IsInRootTree(target))`, and `DispatchQueued` returns the stop flag. The Tab step runs after the walk step for each entry. Each routing test of phases 1 and 2 and of pointer-routing covers the change and passes with no edit.
- The public API grows by two members: `UiContext.FocusNext()` and `UiContext.FocusPrevious()`. No public member changed or went away.
- `PointerTree.cs` is linked by `BrutalUi.Glfw.Tests`. `Tab` reads the internal `UiContext.TabKey`, which that project sees through `InternalsVisibleTo`. The project builds with 0 warnings and its tests pass.

**Group A decisions (the brief did not settle them):**
- P3-1: A private struct `FocusWalk.Candidates` holds the walk state (`First`, `Last`, `Before`, `After`, `Result`, and `Seen`). `Visit(Element, Element? current, bool backward, ref Candidates)` returns `bool`. When it returns `true`, `Find` returns `Result` at once. The code uses no local function and no closure.
- P3-2: The fields of `FocusFrame` are public fields, as in `HitFrame`, in this order: `Element`, `Opacity`, `NextChild`.
- P3-3: `Find` reads the child span again at each loop step. It copies `child` and `Opacity` and moves `NextChild` before `Enter`, as in `HitWalk`.
- P3-4: `routed` is `entry.Header.Target is null` at the pop, for each type. `stopped` is `false` when the entry gets no walk. A Tab that the drain routes to an owner outside the roots therefore still moves the focus, to the first candidate.
- P3-5: The Tab check reads `entry.Payload.Key.Key` and `entry.Payload.Key.Modifiers` from the popped local entry. The mask is `Control | Alt | Super`. `CapsLock` and `NumLock` do not stop a move.
- P3-6: The remarks of `FocusedElement` now name `FocusNext` and `FocusPrevious`, and state that a Tab key down moves the value.
- P3-7: `TabKey` sits next to the test members, with a summary and a remarks block that holds the GLFW note.
- P3-8: `Enter` in `FocusWalk` has its own copy of the opacity clamp. Phase 5 moves the clamp into a shared helper (task 5.2).
- P3-9: `FocusNext` and `FocusPrevious` each call `FocusWalk.Find` directly, with the literal `false` or `true`.

**Group B decisions (the brief did not settle them):**
- P3-10: `PointerTree.Tab` calls the existing `Key` method with `UiContext.TabKey`, so the scan code is 0 and `IsRepeat` is `false`. A test that needs a repeat or a caller target copies the struct and sets the field.
- P3-11: Each move check uses one helper, `AssertOwner`. It reads `FocusedElement` and checks that the owner holds `Focus` and that no other element of the tree holds it. The demo also checks the drain count: 0 for the first Tab (no owner, so no walk) and 1 for each later Tab.
- P3-12: `TabSkipsAHiddenSubtree` also covers an opacity of 0 on `Inner` only, a hidden owner (Tab gives the first candidate, Shift+Tab the last), and an element with `IsPickable` `false` that stays a candidate. Each style write comes after `Settle()`, and the test runs no other update.
- P3-13: `AHandlerThatStopsTheTabKeepsTheFocus` covers three cases: a stop in the target bubble slot of the old owner, a stop in the capture slot of the root with Shift+Tab, and a `StopAt` on an element off the path. The third case shows the full walk to the old owner (root capture, owner target twice, root bubble) and then the move.
- P3-14: `TabWithControlAltOrSuperDoesNotMove` checks six blocked sets, also with `Shift`. Each one gives one walk to the owner. `CapsLock` moves forward and `NumLock | Shift` moves back (P3-5).
- P3-15: `ARepeatTabMovesAndAKeyUpDoesNot` also checks a Shift+Tab key up and a char with the code point U+0009. Neither moves the focus. A repeat Shift+Tab moves back.
- P3-16: `ATabWithACallerTargetDoesNotMove` also covers a caller target that is the owner, a caller target outside the roots (no walk, no move), and a caller target with no owner (the owner stays null).
- P3-17: `ATreeDeeperThanTheStackFindsTheLeaf` marks the leaf and level 20 focusable in turn. It checks a stack length of 32 at the start, then a full pass, a single candidate, an early return of Shift+Tab at the leaf, an early return of `FocusNext` at the leaf, and a wrap. Each slot holds a null element after each call.

**Risks and follow-up work that group B found:**
- The hidden owner cases of `TabSkipsAHiddenSubtree` cannot tell "the walk does not see the owner" from "the owner is the last candidate", because the tree of `PointerTree` has no shown candidate after a hidden owner. A wider tree in phase 5 (the stale rule) can test the case where the two rules give different results.
- The group A hand-back gives a Core.Tests count of 1415 before the new tests. Phase 2 Results gives 1416, and the run after this phase gives 1428 (1416 + 12). The value 1416 is correct.

---

## Phase 4 Results

**Group 1 (the API check, the adapter keys and chars, `KeyboardEnabled`, the tests, and the record):** `GlfwUiInputKeyTests.cs` is new. `GlfwUiInput.cs` and `GlfwUiInputTests.cs` changed. No file of `BrutalUi.Core`, no `.csproj` file, `Directory.Packages.props`, `brutal-ui.sln`, `PointerTree.cs`, and `GlfwUiInputAllocationTests.cs` changed in this phase.

### API check of task 4.1

Source: `ilspycmd` 11.1.0 output of `Brutal.Glfw.dll` (`Ahwoo.BRUTAL.Glfw` 2026.10.4) in the session scratch folder, and `Brutal.Glfw.xml`. The repository holds no decompiled file. Each fact of the plan is correct.

| API | Real shape | Matches the plan |
|-----|------------|------------------|
| `GlfwKeyCallback` | `delegate void GlfwKeyCallback(GlfwWindow window, GlfwKey key, int scancode, GlfwKeyAction action, GlfwModifier mods)` | Yes |
| `GlfwCharCallback` | `delegate void GlfwCharCallback(GlfwWindow window, uint codepoint)` | Yes |
| `GlfwKeyAction` | `enum GlfwKeyAction { Release, Press, Repeat }`: the values 0, 1, and 2 | Yes |
| `GlfwKey.Tab` | `Tab = 258` | Yes |
| `GlfwKey.Unknown` | `Unknown = -1` | Yes |
| `GlfwWindow.OnKey` | `public event GlfwKeyCallback? OnKey`, a field-like event | Yes |
| `GlfwWindow.OnChar` | `public event GlfwCharCallback? OnChar`, a field-like event | Yes |
| Native key callback | `(GlfwWindowHandle* _, int key, int scanCode, int action, int mods)` calls `OnKey?.Invoke(this, (GlfwKey)key, scanCode, (GlfwKeyAction)action, (GlfwModifier)mods)` | Yes |
| Native char callback | `GlfwCharUnmanagedCallback(GlfwWindowHandle* window, uint codePoint)` calls `OnChar?.Invoke(this, (ushort)point)`. The native char-mods callback has the same `(ushort)point` cast | Yes (Decision 10) |
| `GlfwModifier` | `Shift = 1`, `Control = 2`, `Alt = 4`, `Super = 8`, `CapsLock = 0x10`, `NumLock = 0x20`. It has no `None` member | Yes (the tests use `(GlfwModifier)0`) |

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` before phase 4 | total 2252, failed 0, succeeded 2250, skipped 2 (Phase 3 Results) |
| `dotnet test --solution brutal-ui.sln` after | total 2260, failed 0, succeeded 2258, skipped 2 (the two GLFW real-window tests, no display) |
| `xvfb-run -a dotnet test --solution brutal-ui.sln` | total 2260, failed 0, succeeded 2260, skipped 0 |
| `BrutalUi.Glfw.Tests` with no display | total 20, failed 0, succeeded 18, skipped 2 |
| `xvfb-run -a dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj` | total 20, failed 0, succeeded 20, skipped 0 |
| Demo: `--filter-class '*GlfwUiInputKeyTests'` | total 8, failed 0 |
| `GlfwUiInputTests` | total 10, failed 0, skipped 1 with no display, skipped 0 with `xvfb-run` |
| `grep -rn 'using .*Glfw' src/BrutalUi.Core` | no output |
| SHA-256 of `EventWalk.cs` at the start and at the end | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (the value of Decision R3) |

**Blast radius (the shared code that this phase touches):**
- `GlfwUiInput` now adds two more handlers to each window: `OnKey` and `OnChar`. The ImGui `GlfwBackend` also adds handlers to these events. The events are multicast, so neither handler replaces the other (Decision 11). The handlers run in the order of the subscriptions.
- A game that creates `GlfwUiInput` now gets key and char events in its UI. A key or a char with no focus owner gets no walk, so a game with no focusable element sees no change. A Tab press with no owner and a focusable element now moves the UI focus. ImGui also gets that Tab. Phase 6 docs state the `KeyboardEnabled` rule.
- The public API grows by one member: `GlfwUiInput.KeyboardEnabled`. No public member changed or went away.
- `GlfwUiInputAllocationTests` passes with no edit, also under `xvfb-run`. Phase 6 adds the key and char handlers to it.

**Group 1 decisions (the brief did not settle them):**
- P4-1: `PushKey` and `PushChar` check `_disposed` and `KeyboardEnabled` in one `if`. `PushChar` adds `!Rune.IsValid(codePoint)` to the same `if`. The adapter reads `KeyboardEnabled` at each push, so a change between two events of one `Glfw.PollEvents()` call takes effect at once.
- P4-2: `KeyboardEnabled` is an auto property with no backing logic. A set after `Dispose` is legal and changes nothing, because `_disposed` is checked first.
- P4-3: The modifier mask constant and `ButtonModifiers` now name the key event in their summaries. The method keeps its name, so no test changed.
- P4-4: The class remarks got three new paragraphs at the top: the eight events (and no `OnCharMods`), the ImGui rule, and the char limit of the binding. The other paragraphs did not change.
- P4-5: The remarks of `KeyboardEnabled` also state the opposite case of the switch: a key down while the value is `false` and a key up after the value becomes `true` again give a key up with no key down.
- P4-6: `ARealWindowSubscribesAndUnsubscribes` counts the eight events through the shared `EventNames` array. It also calls the real `OnKey` delegate with a Tab press and the real `OnChar` delegate with `'a'`. This is the only test that runs `HandleKey`, `HandleChar`, and `GlfwApi.Time` through the key path. With `Right` as the only focusable element, the Tab gives a drain count of 0 and focuses `Right`, and the char then reaches `Right`. The class keeps 10 tests.
- P4-7: The demo marks `Left` and `Right` focusable, so the Tab from `Right` wraps to `Left` (`Inner` is not focusable). The demo also adds the focus sheet and checks the focus fill on `Right` and then on `Left`.
- P4-8: `ATabPressMovesTheFocus` checks the order `Left`, (key up: no move), (repeat: `Right`), `Inner`, (Control+Tab: no move), Shift+Tab `Right`, Shift+Tab `Left`. The first Tab has no owner and gives a drain count of 0.
- P4-9: `ACharThatIsNotAScalarValueIsDropped` also drops `0xDBFF`, `0xDC00`, and `uint.MaxValue`, and also pushes the edges `0xD7FF`, `0xE000`, `0xFFFF`, `0x10FFFF`, and `0`. U+0000 is a scalar value, so the adapter pushes it.
- P4-10: `TheKeyCarriesTheScanCodeTheModifiersAndTheTime` also pushes `GlfwKey.Unknown` (-1) as a key up and checks the key -1, and checks the timestamp of a char.
- P4-11: The `ilspycmd` tool is in the session scratch folder only. No tool manifest changed.

**Risks and follow-up work that group 1 found:**
- Phase 6: `GlfwUiInputAllocationTests` must cover `HandleKey` and `HandleChar`. `HandleKey` reads `GlfwApi.Time` and boxes nothing. The real-window test runs both handlers, but it does not measure allocation.
- Phase 6 docs: both ImGui and the UI get a Tab press. A game with ImGui text input and a focusable UI element must set `KeyboardEnabled` from `io.WantCaptureKeyboard`, or a Tab moves both focuses.
- The switch leaves a key down with no key up (P4-5). An element that tracks a held key must also handle a focus loss. Phase 5 or a later feature can add a key-state reset on a focus change.
- A code point above U+FFFF from GLFW arrives cut today. For example, U+1F600 arrives as U+F600, a private-use scalar value, and the adapter pushes it. The adapter cannot detect the cut. The fix belongs to BRUTAL (the issue text below).

### Issue text for BRUTAL

A person files this issue in the BRUTAL repository.

> **Title:** `GlfwWindow.OnChar` and `OnCharMods` cut a code point above U+FFFF
>
> **Package:** `Ahwoo.BRUTAL.Glfw` 2026.10.4.
>
> **Problem:** The native char callback of `GlfwWindow` gets the code point as `uint`. It then calls `OnChar?.Invoke(this, (ushort)point)`. The native char-mods callback calls `OnCharMods?.Invoke(this, (ushort)point, (GlfwModifier)mods)`. The `(ushort)` cast keeps the low 16 bits only. A code point above U+FFFF, for example an emoji or a CJK Extension B character, arrives cut. U+1F600 arrives as U+F600. A handler cannot restore the value.
>
> **Expected:** The handler gets the full code point, for example U+1F600.
>
> **Fix:** Pass the value with no cast: `OnChar?.Invoke(this, point)` and `OnCharMods?.Invoke(this, point, (GlfwModifier)mods)`. The delegates `GlfwCharCallback` and `GlfwCharModsCallback` already take `uint codepoint`, so the public API does not change.
>
> **Impact on brutal-ui:** None. `GlfwUiInput` pushes each Unicode scalar value that it gets, also a value above U+FFFF. A test (`ACharThatIsNotAScalarValueIsDropped`) pushes U+1F600 already.

---

## Phase 5 Results

**Group 1 (the `Disabled` rule, `IsShown`, the stale focus rule, the docs, the tests, and the record):** `FocusStateEdgeTests.cs` is new. `Element.cs`, `FocusWalk.cs`, and `UiContext.cs` changed. `EventWalk.cs`, `PointerTree.cs`, and each test file of phases 1 to 4 did not change.

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` before phase 5 | total 2260, failed 0, succeeded 2258, skipped 2 (Phase 4 Results) |
| `dotnet test --solution brutal-ui.sln` after | total 2268, failed 0, succeeded 2266, skipped 2 (the two GLFW real-window tests, no display) |
| `BrutalUi.Core.Tests` before and after | total 1428, then total 1436. Failed 0 in each run |
| Demo: `--filter-class '*FocusStateEdgeTests'` | total 8, failed 0 |
| Focus classes, no edit | `FocusOwnerTests` 8, `ClickFocusTests` 8, `TabNavigationTests` 12: 28 pass, 0 failed |
| Pointer and allocation classes, no edit | `PointerStateEdgeTests` 8, `HitTestRuleTests` 11, `EventAllocationTests` 3, `PointerAllocationTests` 3: 25 pass, 0 failed |
| No method of `FocusWalk` calls itself | `IsShown` and `Enter` call `ClampOpacity`. `ClampOpacity` calls no method of `FocusWalk`. `IsShown` is one `for` loop over `Parent` |
| SHA-256 of `EventWalk.cs` at the start and at the end | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (the value of Decision R3) |

**Blast radius (the shared code that this phase touches):**
- `Element.AcceptsFocus` now also reads `PseudoStates.Disabled`. Four callers read it: `SetFocus`, `FocusOnPress`, `FocusWalk.Visit` (Tab, Shift+Tab, `FocusNext`, and `FocusPrevious`), and the new `DropStaleFocus`. No other code reads it.
- `UiContext.SetFocus` now returns `false` for a disabled or hidden element. This is a behavior change of a public member. Its signature did not change. The tests of phases 1 to 4 pass with no edit.
- `UiContext.DrainEvents` got one call, `DropStaleFocus()`, right after `DropStalePointerState()`. It sits inside the `_draining` guard and before the count read. Nothing else in the drain changed.
- `FocusWalk.Enter` now calls `ClampOpacity`. The clamp is the same: below 0 counts as 0, above 1 as 1, and NaN as 1. `TabNavigationTests` and `HitTestRuleTests` pass with no edit.
- The public API did not grow. `IsShown` and `ClampOpacity` are not public.

**Group 1 decisions (the brief did not settle them):**
- P5-1: `ClampOpacity(float)` is a private static helper that returns the clamped value. `Enter` keeps the product and the `<= 0` check, and it calls the helper for the factor.
- P5-2: `IsShown` multiplies the product from the element up to the top. `Enter` multiplies from the root down. For normal values the two orders give the same answer to the check `<= 0`. Only a product near the float underflow limit (about 1e-45) can round to 0 in one order and not in the other. No real style gives such a product, so the method keeps the loop with no stack.
- P5-3: `IsShown` checks no root. `SetFocus` and `DropStaleFocus` check `AcceptsFocus`, then `IsInRootTree`, then `IsShown`. The cheap check runs first, and `IsShown` reads the style last.
- P5-4: `SetFocus` for the current owner that is now hidden or disabled returns `false` and does not clear the owner. The stale rule clears it at the next drain. `SetFocusOwner` still checks no rule (P1-3).
- P5-5: The summary of `AcceptsFocus` states the full rule. Its remarks state that it reads no style, so the hidden rule is a separate check (`FocusWalk.IsShown`).
- P5-6: The limit paragraph of `DrainEvents` now names the three states (`Hover`, `Active`, and `Focus`), and `DropStaleFocus` has its own limit paragraph (CR-8 of pointer-routing).
- P5-7: The remarks of `IsFocusable` state that a press on a disabled focusable element clears the focus and does not give it to a focusable ancestor (P2-1).
- P5-8: Three tests push a key or a char into the drain that runs the stale rule. Each one gets no walk (a drain count of 0), because the rule clears the owner before the target step. The same tests then check the way back: `AddRoot`, `IsFocusable = true`, or the removal of `Disabled` lets `SetFocus` succeed again, and no drain gives the focus back by itself.
- P5-9: `AnOwnerUnderAHiddenAncestorLosesTheFocus` also covers an opacity of 0 and of -3 on the parent (drop), NaN and 7 on the parent (keep), 0.5 on the parent and 0.5 on the owner (keep), and `Display.None` on the root (drop).
- P5-10: `ADisabledElementTakesNoFocus` marks `Root`, `Left`, `Right`, and `Inner` focusable and `Right` disabled. Tab from `Left` gives `Inner`, and Shift+Tab gives `Left` again. A press on `Right` clears the focus, and the focusable `Root` does not get it. A press on `Inner` then focuses `Inner`.
- P5-11: `SetFocusOnTheOwnerMarksNothing` also runs an empty drain with a valid owner after the second `SetFocus`. The stale rule then marks nothing either.
- P5-12: `ASetFocusFromAHandlerRoutesTheNextKey` uses the keys A (65) and B (66), not Tab, so no Tab move hides the result. The record reads `left 65 left` and then `right 66 right`.

**Risks and follow-up work that group 1 found:**
- `Disabled` is not inherited. A focusable child of a disabled element still takes the focus (P5-10 shows it). A game that disables a panel must also disable each focusable descendant. A later feature can add an inherited rule. Phase 6 docs must state the limit.
- The stale rule runs at the start of a drain only. A handler that disables, hides, or removes the owner during a drain leaves the owner in place for the later key and char entries of that drain. The next drain clears it.
- The hidden rule reads the style of the last `Update`. A sheet change that hides the owner takes effect for the stale rule after the next `Update` and then the next drain. `SetFocus` on an element that a new rule hides returns `true` until that `Update`.
- The phase 3 risk on a hidden owner is now moot for the drain: the stale rule clears a hidden owner before a Tab of the drain, so the Tab starts with no owner. A code call of `FocusNext` between two drains still sees a hidden owner and starts from the first candidate.
- Phase 6: `FocusAllocationTests` must cover a drain with an owner, so that `DropStaleFocus` and `IsShown` run under the allocation counter. The existing allocation classes pass, but none of them holds a focus owner.

---

## Phase 6 Results

**Group A (the allocation gates):** `FocusAllocationTests.cs` is new (3 tests). `GlfwUiInputAllocationTests.cs` got the key and char loops and the drop checks `6L` and `5L`. `src/` has no net change and no fix.

| Check | Result |
|-------|--------|
| 5 runs of `*FocusAllocationTests` | total 3, failed 0 in each run (0 bytes) |
| `GlfwUiInputAllocationTests`, no display | total 2, 1 passed, 1 skipped (`TheRealHandlersAllocateNothing`) |
| `GlfwUiInputAllocationTests` under `xvfb-run -a` | total 2, failed 0, skipped 0. The real `HandleKey` and `HandleChar` delegates allocate 0 bytes |
| Plant 1: `GC.KeepAlive(new object());` first in `FocusWalk.Find` | `exit=2`. Failed: `KeyboardRoutingAllocatesNothingAfterWarmUp` (14248 bytes) and `TheTabSearchAllocatesNothingOnAWarmTree` (12000 bytes). Restore with `cp`, `cmp` clean |
| Plant 2: the same line first in `SetFocusOwner` | `exit=2`. Failed: the same two gates, plus `PointerAllocationTests.PointerRoutingAllocatesNothingAfterWarmUp` (each press runs the click focus). Restore with `cp`, `cmp` clean |
| After the restores | build 0 warnings, solution total 2271, failed 0, skipped 2. The SHA-256 of `EventWalk.cs` is unchanged |

**Group A decisions:**
- P6-1: A step of task 6.2 has two drains. The first Tab goes to the first drain (no owner, so no walk). The other 9 events go to the second drain, which starts with the owner slot 0 and runs `DropStaleFocus` with `FocusWalk.IsShown`. The step ends with no owner. A step has 7 walks. The press outside the root gets no walk.
- P6-2: One `Counter` and one delegate for each event kind serve all 120 grid slots. The test installs them before the measure. A step gives 3 key downs, 1 key up, and 1 char (the last code point 97). `LastWalks` stays in the scene, so the compiler keeps the drain.
- P6-3: The hidden focusable element is `tree.Settings.Children[0]`. `FocusPrevious` from no owner gives the last grid slot, never this element.
- P6-4: In the Glfw `Push` and `CallHandlers`, the key loop uses `i % 3` for a press, a repeat, and a release, with the key 65 and the scan code 30.

**Group B (the docs and the gate):** `docs/technical-design.md`, `CHANGELOG.md`, and this file changed. `implementation.md` needed no lint fix.

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` | total 2271, failed 0, succeeded 2269, skipped 2 (the two GLFW real-window tests, no display). `BrutalUi.Core.Tests` 1439 |
| `scripts/test-linux.sh; echo "exit=$?"` | `test-linux: PASS build, test, gpu, smoke (120 frames) in 11 s`, `exit=0` |
| `ste-lint.py --fail-over 2.5` | `technical-design.md` 1.50, `CHANGELOG.md` 1.73, `implementation.md` 1.71, `context.md` 1.53. No hit above 2.5 |

**Sections of the technical design that changed:** section 5 "Shape" (`IsFocusable`), section 5 "Events" (the drain paragraph and the new keyboard step), section 9 "Flow" (items 1 and 7), and section 14 (the focus tests and the eighth gate).

**Group B decisions:**
- P6-5: Section 5 "Events" got a new paragraph, "The keyboard step of the drain", after the pointer step. It lists the stale focus, the key and char target, the click focus, and the Tab rule. It also states the `Disabled` limit and the stale rule limit.
- P6-6: Section 9 item 7 names `FocusNext` and `FocusPrevious` as the slots for gamepad navigation through the action map (Q2 of the epic). It no longer names gamepad navigation as a focus move of this epic. Item 1 names `KeyboardEnabled`, and item 7 holds the ImGui rule, the key down limit of the switch (P4-5), and the `Disabled` limit.
- P6-7: Section 14 has two new bullets after the seventh gate: the focus tests, and the eighth gate with the two plants. The sentence "BrutalUi.Glfw.Tests has 12 tests" in the pointer bullet stays as the count of that feature. The focus bullet gives the new count of 20.
- P6-8: The Shape block of the technical design got `IsFocusable` only. The members of `UiContext` for the focus are in the keyboard step paragraph, not in the Shape block.

---

## Review Fixes

The cross-phase review (`code-review.md`) ran 58 mutation probes. 52 failed one or more tests, 2 are equivalent mutants, and 4 became findings. The review found 6 findings (0 HIGH, 3 MEDIUM, 3 LOW). All 6 are resolved. The verifier of the fixes gave PASS: build 0 warnings, 2271 tests, 0 failed, 2 skipped with no display, the Glfw tests 20 of 20 under `xvfb-run -a`, and `scripts/test-linux.sh` PASS. The class totals of the plan did not change, because each guard check is a block inside an existing test.

- CR-1: the internal `GlfwUiInput.KeyArgs` holds the key action conversion. `APressARepeatAndAReleaseGiveDownRepeatAndUp` checks it with no window. `ARealWindowSubscribesAndUnsubscribes` checks a repeat, a release, and the `0xFF` mask through the real delegate. A `HandleKey` that skips `ButtonModifiers` fails only with a display.
- CR-2: `TabSkipsAHiddenSubtree` calls `FocusNext` and `FocusPrevious` with no drain on a hidden owner (the gamepad slot). The comment of the drain block states that the stale rule clears the hidden owner first.
- CR-3: `DropStaleFocus` and the early return of `SetFocusOwner` set `Focus` on the owner again. Code that clears the bit through `States` gets it back at the next drain or at a `SetFocus` on the owner.
- CR-4: a failed `SetFocus` on the current owner clears the focus, so the result and the state agree.
- CR-5: the key and char branch of the target step drops an owner with `AcceptsFocus` false. A handler that disables the owner stops the later keys of the same drain. Decision 7 stays as it is.
- CR-6: section 5 of `docs/technical-design.md` states the three rules and the `SetFocus` limit for a panel that the game shows in the same frame.

**Follow-ups for later features:**
- routing-gate: call `SetFocus` after the `Update` that shows a panel (CR-6). The pointer side of the ImGui rule (`io.WantCaptureMouse`) belongs to the gate design (Notes, Conflicts 8).
- interactive-elements: a widget can write `States`, and the drain gives the owner its `Focus` bit back (CR-3). `Disabled` does not pass to the children.

---

## Gotchas & Learnings

### 1. A diff against main cannot isolate one phase on an uncommitted branch
**Problem:** The phase 4 verify brief asked for no diff of `PointerTree.cs` against `main`. Phases 1 and 3 add to that file on purpose, and the branch holds no commit per phase, so the first verifier gave FAIL with no code fault.
**Solution:** The brief now checks the removed lines of the diff only, and it names the additions of the earlier phases.
**Lesson:** A verify brief that uses `git diff main` lists only the files that no phase of the feature changes. For a file that an earlier phase changed, check the content, not the diff.

### 2. A later rule can hide the case that an earlier test guards
**Problem:** The Tab test of a hidden owner went through the drain. Phase 5 added the stale rule at the start of the drain, so the test then checked the stale rule, and no test guarded the walk rule (CR-2).
**Solution:** A code call of `FocusNext` and `FocusPrevious` with no drain now guards the walk rule.
**Lesson:** When a phase adds a step in front of a path, read the tests of that path again. A test can still pass and guard a different rule.

### 3. A test with a window guards a conversion only where a display exists
**Problem:** The key action conversion of `HandleKey` ran only in the real-window test, which skips with no display (CR-1).
**Solution:** An internal static method holds the conversion, and a test with no window checks it.
**Lesson:** Move adapter logic out of a native callback into a method that a test can call with no window.

---

## API Integration

- (see the section "Public API (end state)" of the plan)

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Input/*.cs`
- The focus owner, the key route, the click focus, Tab and Shift+Tab, and the stale focus rules.

### Adapter Tests
**Files:** `tests/BrutalUi.Glfw.Tests/*.cs`
- The key and char conversions through the test seam, `KeyboardEnabled`, `Dispose`, and the real-window subscription count.

### Allocation Tests
- `FocusAllocationTests` and the extended `GlfwUiInputAllocationTests` (phase 6).

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a unit test marks `Right` of the pointer tree focusable, adds a `:focus` rule, calls `SetFocus(Right)`, pushes a key down with no target, drains, and runs `Update`. It reads `PseudoStates.Focus` and the focus fill on `Right`. A recorder on the root and `Right` reads root capture, `Right` target capture, `Right` target bubble, and root bubble. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusOwnerTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 2 demo: a unit test marks `Right` focusable, adds the `:focus` rule, pushes a pointer down with no target over `Right` outside `Inner`, drains, and runs `Update`. It reads `Focus` and the focus fill on `Right`, and the `PointerDown` handler of `Right` reads `Focus` already. A press over the gap of the root then clears the focus. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ClickFocusTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 3 demo: a unit test marks `Left`, `Right`, and `Inner` focusable, pushes four Tab key downs with no target, and drains after each. The owner goes `Left`, `Right`, `Inner`, and `Left` again. A Shift+Tab then gives `Inner`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TabNavigationTests'` and reads a total of 12 with `failed: 0`.
- [ ] Phase 4 demo: a unit test in `BrutalUi.Glfw.Tests` creates a `GlfwUiInput` with the test seam (no window), focuses `Right` of the pointer tree, calls the internal `PushKey` for the A key (65) and `PushChar` for `'a'` (97), and drains. A handler on `Right` reads a `KeyDown` with the key 65 and a `Char` with the code point 97. A `PushKey` of Tab (258) then moves the focus. The developer runs `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputKeyTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 5 demo: a unit test focuses `Right`, removes it from the root, and drains an empty queue. It reads no `Focus` on `Right` and a null `FocusedElement`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusStateEdgeTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `FocusWalk.Find` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
- [ ] Real-window tests (keyboard-focus): on a machine with a display, or under `xvfb-run -a`, run `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj`. Expect a total of 20 with `failed: 0` and no skip.
- [ ] Windows run (keyboard-focus, Q8 of the plan): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0. Then run the Glfw test project, and expect no skip.
- [ ] BRUTAL issue (keyboard-focus, phase 4): a person files the issue text of the section "Phase 4 Results" with BRUTAL. The native char callback of `GlfwWindow` casts the code point to `ushort`, so a code point above U+FFFF arrives cut.

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the items of "Needs Human Verification".
2. `/autorun-epic` commits the feature and merges it into `main`.

### Short Term (This Week)
1. `input-routing/routing-gate` reads `FocusedElement` for `KeyboardFocusInUi`, calls `DrainEvents` from `Update`, and adds the HelloPanel focus demo (a synthetic Tab and the `KeyboardEnabled` line).
2. A person files the BRUTAL issue of "Phase 4 Results".

---

## Open Questions

- (none open. See Decision R1.)

---

## Session Notes

### 2026-10-22 13:08
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`. The docs exist. Phase 1 is next.

### 2026-10-22 13:18 Phase 1
- Phase 1 is done (8 of 8 tasks). The verifier gave PASS and Demo: PASS.
- Build 0 warnings. Solution 2232 tests (2224 before), 0 failed, 2 skipped. Core.Tests 1408 (1400 before).
- The SHA-256 of `EventWalk.cs` stays the value of Decision R3. The inbox has no line.

### 2026-10-22 13:30 Phase 2
- Phase 2 is done (4 of 4 tasks). The verifier gave PASS and Demo: PASS.
- Build 0 warnings. Solution 2240 tests, 0 failed, 2 skipped. Core.Tests 1416.
- The SHA-256 of `EventWalk.cs` stays the value of Decision R3. The inbox has no line.

### 2026-10-22 13:33 Phase 3
- Phase 3 is done (7 of 7 tasks, two groups). The verifier gave PASS and Demo: PASS.
- Build 0 warnings. Solution 2252 tests, 0 failed, 2 skipped. Core.Tests 1428.
- The SHA-256 of `EventWalk.cs` stays the value of Decision R3. The inbox has no line.

### 2026-10-22 13:43 Phase 4
- Phase 4 is done (9 of 9 tasks). The API check of task 4.1 matched the plan.
- The first verifier gave FAIL on one check only: the verify brief compared `PointerTree.cs` with `main`, and phases 1 and 3 add to that file on purpose. The parent fixed the brief (the check now allows the additions of phases 1 and 3 only). The second verifier gave PASS and Demo: PASS. No code changed between the two runs.
- Build 0 warnings. Solution 2260 tests, 0 failed, 2 skipped. Glfw.Tests 20, and 20 of 20 under `xvfb-run -a`.
- The SHA-256 of `EventWalk.cs` stays the value of Decision R3. The inbox has no line.

### 2026-10-22 13:50 Phase 5
- Phase 5 is done (6 of 6 tasks). The verifier gave PASS and Demo: PASS.
- Build 0 warnings. Solution 2268 tests, 0 failed, 2 skipped. Core.Tests 1436.
- The SHA-256 of `EventWalk.cs` stays the value of Decision R3. The inbox has no line.
- The phase 6 brief adds one more drain to the keyboard gate, so a drain starts with an owner and the gate measures `DropStaleFocus` (phase 5 risk).

### 2026-10-22 14:02 Phase 6
- Phase 6 is done (8 of 8 tasks, two groups). The verifier gave PASS and Demo: PASS.
- Build 0 warnings. Solution 2271 tests, 0 failed, 2 skipped. Core.Tests 1439 (1400 + 39). Glfw.Tests 20, and 20 of 20 under `xvfb-run -a`. `scripts/test-linux.sh` PASS.
- Both plants gave `exit=2`. No `src/` fix was needed. The SHA-256 of `EventWalk.cs` stays the value of Decision R3. The inbox has no line.

### 2026-10-22 14:48 Cross-phase review and close
- The review ran 58 mutation probes and found 6 findings (0 HIGH, 3 MEDIUM, 3 LOW). Four fix groups (`review-g1.md` to `review-g4.md`) fixed all 6. The verifier of the fixes gave PASS: 2271 tests, 0 failed, 2 skipped with no display, the Glfw tests 20 of 20 under `xvfb-run -a`, and `scripts/test-linux.sh` PASS.
- Decisions R4 and R5 record the fix rules. Gotchas 1 to 3 record the lessons of the run.
- The solution architect wrote the draft plan of `input-routing/routing-gate` (3 phases, 23 tasks, four open scope questions).
- The inbox has no line, so no bug is promoted.
- Next: a person runs "Needs Human Verification". `/autorun-epic` commits the feature.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
