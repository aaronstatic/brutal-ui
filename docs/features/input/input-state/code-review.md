# input/input-state - Code Review

**Reviewed:** 2026-11-08
**Reviewer:** Senior C# engine reviewer (cross-phase review of phases 1 to 5)
**Rules Applied:** 1. Plan fidelity of the writer and the three hooks in `DrainEvents`, 2. Flag-rule correctness against Decisions 7, 8, 9, 10, 11 (range guards, ownership, repeat, clear, edges), 3. The gate rules (A4 gate at end of drain, ownership, NaN, press owner across frames), 4. API clarity and XML docs (ASD-STE100), 5. Allocation discipline (0 bytes, gate shape, plant coverage of each rule), 6. Integration with input-routing, action-map, mode-set, and the contracts for gamepad-axes and input-actions, 7. Test strength (each DoD criterion pinned by a test and a plant; list rules pinned by neither), 8. Scope discipline (the I1 file list, no framework/gate/queue changes), 9. Docs truth (section 9 item 11, section 14, CHANGELOG, the stale comments of Decision 21), 10. Sample correctness (label rules, scratch sizes, the phase-3 buffer re-check of Decision 9/20). The Vercel React rules do not apply to C#.
**Status:** Resolved - 7 findings, 7 resolved (2026-11-08: CR-1 ownership sentence rewritten in section 9 item 11; CR-2 plan I1 and step 7 updated to the shipped file set; CR-3 and CR-4 pinned by extended tests with the counts held at 32 and 5; CR-5, CR-6, CR-7 comment fixes. All groups verified by the review-fix verifier - see the session note in `context.md`.)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 2 | CR-1, CR-2 |
| LOW | 5 | CR-3, CR-4, CR-5, CR-6, CR-7 |

The engine code does what the plan asks. Rules 1, 2, 3, 5, and 6 pass with no finding. Rules 4, 7, 8, 9, and 10 give one or two findings each; every one is a doc sentence, a missing assert, or a file-list truth, not a behavior fault.

Rule 1 passes. `InputStateWriter` matches the Internal Shape field for field: `Raw`, `Game`, `_startPosition`, `_uiPresses`, `_uiReleases`, `_clearKeys`; `Create` sets both positions and the start to (NaN, NaN); `BeginFrame` reads `_startPosition` from `Raw.PointerState.Position`, clears the delta, the scroll, the button pressed and released masks, the key pressed and released bits, and `_uiReleases`, and keeps each down mask, the modifiers, the position, `_uiPresses`, and each slot (Decision 11, 14). `Record` switches on the entry type and reads only the payload the type names (`InputStateWriter.cs` line 94 to 144). `EndFrame` runs the key clear, the delta, the one copy, the pointer gate, the ownership mask, and the key gate in the plan order (line 202 to 228). The three hooks sit where Decision 4 binds them: hook 1 after `DropStaleFocus()` and before the read of `_events.Count` (`UiContext.cs` line 1632 to 1638), hook 2 right after the target step and before the press target step (line 1674), hook 3 in the `finally` block after `FinishRelease()` (line 1797). The `_draining` throw sits before the `try`, so a nested drain writes nothing, and an exception of a handler still runs hook 3. `git diff aec0341f --stat` on `UiContext.cs` gives 126 added and 1 deleted line; the deleted line is the `RoutingGate` `<para>` that task 1.4 rewrites by adding the `GameInput` sentence.

Rule 2 passes. The range guards use the `(uint)x < (uint)Count` shape, so -1 and a high value read `false` and change no bit (`UiInputState.cs` line 282, 377, 396, 435); `KeyBits.Set` and `Clear` skip the second check because both run behind the guard (Decision 17). An up releases only a held input (Decision 7): `UiPointerButtonState.Release` and `UiKeyState.KeyUp` test the down bit first. A repeat sets the down bit and no pressed bit, and a repeat of a key that is not down sets the down bit (Decision 9, line 375 to 387). The modifiers take the value of every key event, also an out-of-range code, and `ReleaseAll` gives `None` (Decision 10, line 420 to 430). `ClearHeldKeys` sets the request and the end of the next drain applies it after the last entry, so a key down of the same frame also clears (line 204 to 208; `UiContext.cs` line 1060). The gamepad edge rule (`Pressed = new & ~old`, `Released = old & ~new`) sits on the slot type (`UiGamepadState.WriteEdgesInto`, line 115 to 124), the drain keeps the edges, and the copy carries the slots ungated.

Rule 3 passes. Hook 3 takes `RoutingGate` at the end of the drain, so the game view and the value the game reads after `Update` agree (A4; `UiContext.cs` line 1797). The pointer gate empties the pointer and button parts; with an open gate the ownership rule drops the down and pressed bits of `_uiPresses` and the released bits of `_uiReleases` (`Mask`, `UiInputState.cs` line 268 to 273). `Record` updates `_uiPresses` only on a `PointerDown`, from `hitUi`, so the press owner survives the frame while the pointer drags outside the UI, and a new game press takes the button back (Decision 8, 14; `InputStateWriter.cs` line 102 to 123). The delta falls back to (0, 0) when the start or the end position is not finite (`FinishFrame`, `UiInputState.cs` line 171 to 179). The empty pointer part is (NaN, NaN) with zero deltas, composed as Decision 11 records (line 214 to 218).

Rule 4 gives CR-7. Every public member carries an XML doc (the build keeps CS1591 as an error at 0 warnings), the prose is active voice and short, and no contraction sits in the new comments.

Rule 5 passes. The Q4 grep over the three new Core files prints no line. `InputStateAllocationTests` follows the `PointerAllocationTests` shape: events built before the measure, `AllocationGate.ReleaseAllocationContext()` right before the read, 2 warm-up rounds, 100 steps, `Assert.Equal(0L, after - before)`, and a measured body with no lambda, string, array, or `Assert`. `TheMeasuredBodyDoesRealWork` pins both steps with fixed sums 10 and 16, so an empty body cannot pass. The step exercises every entry type, `ClearHeldKeys` on even steps, and an alternating `SetGamepad`, so the gamepad edges and the clear request stay live in the measured body. Plants 13 to 16 cover the three hooks and a read, with the byte counts recorded in `context.md` (2400, 7200, 2400, 1675200+4800). The read sweep covers all 349 codes, 8 buttons, and 16 slots with six axes on both views, so no read path hides behind a narrow one.

Rule 6 passes. `UiActionMap.cs`, `UiRoutingGate.cs`, and `UiUnhandledInputHandler.cs` show an empty diff against the base (I2). The `git diff aec0341f --name-only` set matches the plan file list except PointerTree.cs (CR-2). The seam names the later features rely on all ship in the planned shape: `SetGamepad`, the public `UiGamepadState` constructor, `Gamepad(int)`, and the query names `Pointer.Delta`, `Buttons.WasPressed`, `Keys.IsDown` for input-actions. The contract line for gamepad-axes holds: `Axis(i)` takes the plain index, so `UiGamepadAxis` needs no `UiContext` change. Core names no GLFW type; `UiGamepadButtons` is the Core enum. The adapter integration is the planned one: `OnFocus` subscribe and unsubscribe, `PushFocus` returning after `Dispose`, the `GlfwFocusEvent` to `bool` mapping of Decision 19, and the `KeyboardEnabled` setter that clears only on the switch and also after `Dispose` (`GlfwUiInput.cs` line 220 to 236, 517 to 525, 704 to 707).

Rule 7 gives CR-3 and CR-4. All 18 DoD criteria map: F1 to F6 sit in `InputStateTests` (32 tests, one per plan name), F5 also in `GlfwUiInputFocusTests` (5 tests), F7 in the gate, and the twelve value plants and four gate plants each fail their expected test. Rules pinned by neither a test nor a plant: the `UiGamepadState.Axis` range throw (CR-3) and the documented post-`Dispose` clear of the `KeyboardEnabled` setter (CR-4). F8 (the label text) has no automated pin by plan decision ("No gate for the label text", Migration Risks); its guard is the smoke run, which stays open outside the sandbox.

Rule 8 gives CR-2. `git diff` of `GlfwUiInputTests.cs` shows the one added `nameof(GlfwWindow.OnFocus)` line. No `.csproj`, `.props`, `.sln`, or `scripts/` file changes. The scope deviation: `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` changed (the optional `repeat` parameter, Decision 15) and I1 does not name it.

Rule 9 gives CR-1 and CR-5. Section 14 is true: the `**Input state tests.**` bullet counts the twelve plants and their failed-test counts exactly as `context.md` records, the `**A nineteenth allocation gate.**` bullet matches the gate class and the plant bytes, the sizes in item 11 (804, 24, 3, 136, 40) match the measured and asserted values, and the CHANGELOG holds the three Added and one Changed entries with the feature tag. The stale `ui.Input.Game` spelling stays in the three places that Notes gap 1 records for `/update-epic`; that is tracked, not a new finding. Item 11 has one false sentence (CR-1), and the three Decision 21 counts are still stale (CR-5).

Rule 10 gives CR-6. The label sits in the tree right after the gate label and is not pickable (`HelloScreen.cs` line 287 to 290). It starts with `LongestInputText` (48 bytes), the scratch stays 64, and the phase-3 re-check holds in the `InputText` remarks ("The longest text needs 48 bytes", line 416): `none` (4) beats the longest code part (3 digits at 348), each delta part is 5 bytes with the -9999 clamp, and `LowestHeldKey` scans 0 to 348 with `ref readonly` reads that allocate nothing. The label write sits after the draw inside the UI region (`Program.cs` line 873).

### Baseline (observed in this review, read-only)

- `git diff aec0341f --name-only` and `git status --porcelain --untracked-files=all`: the I1 set plus `PointerTree.cs`; the six new source and test files; the four feature docs. No other file.
- `git diff aec0341f -- src/BrutalUi.Framework/ src/BrutalUi.Core/Input/UiRoutingGate.cs src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`: empty (I2).
- Q4 grep (the Verification step 5 pattern) over the three new Core files: no line (Q4 holds).
- `grep -n '_input\.' src/BrutalUi.Core/Tree/UiContext.cs`: seven hits exactly where I2 binds them (`Raw` and `Game` in the properties, `RequestKeyClear` in `ClearHeldKeys`, `SetGamepad` in `SetGamepad`, `BeginFrame`/`Record`/`EndFrame` in `DrainEvents` lines 1635, 1674, 1797; `Create` is the field-initializing call shape `InputStateWriter.Create()` in both constructors at lines 259 and 292).
- `git diff` of `GlfwUiInputTests.cs`: the one added `EventNames` line.
- Build and tests: not re-run here. The tree is recorded verified (build 0/0, solution 2864, 0 failed, 3 skipped; class totals 32, 5, 3).

Open human items, already tracked in `context.md` and not counted as findings: one `scripts/test-linux.sh` PASS run outside the sandbox (I4, F8 smoke side), and the HelloPanel window checks (F8, F5 alt-tab).

---

## Findings

### CR-1: Section 9, item 11 states the ownership rule for the released flag wrongly

**Severity:** MEDIUM
**Rule:** 9 (docs truth); Decision 8, DoD F3
**File:** `docs/technical-design.md` line 904

**Problem:** The ownership bullet reads:

```markdown
- The ownership rule. A press belongs to the side that got the down. The game view hides the down and pressed flags of a UI press, and it hides the released flag of an up that ends over the UI.
```

The last clause states a different rule from the code and from Decision 8. The released flag is hidden when the up ends a press that went **down over the UI**, wherever the up lands — `_uiReleases |= _uiPresses & bit` inside `if (Raw.ButtonState.IsDown(button))` (`InputStateWriter.cs` line 116 to 120). The essential A3 case, and the case DoD F3 and the plant 4 test `APressOverTheUiThatEndsOverTheGameGivesNoReleaseInTheGameView` pin, is an up that ends **over the game**: there the gate is open and only the ownership mask hides the release. An up that ends over the UI needs no ownership rule at all, because the pointer gate already empties the whole button part in that frame (line 907 of the same item). As written, the doc teaches that a UI press released over the game shows its release to the game — the exact stuck-button-in-the-game behavior the rule exists to stop.

**Fix:** One sentence, matching Decision 8 and `Mask`:

```markdown
- The ownership rule. A press belongs to the side that got the down. The game view hides the down and pressed flags of a press that went down over the UI, and it hides the released flag of an up of such a press, wherever the up lands. The raw state keeps every flag.
```

**Effort:** Low

### CR-2: I1 does not fit the shipped diff: `PointerTree.cs` changed outside the named file list

**Severity:** MEDIUM
**Rule:** 8 (scope discipline); I1, Verification Method step 7
**File:** `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` line 170 to 184; `docs/features/input/input-state/implementation.md` line 531 (I1) and line 546 (step 7)

**Problem:** I1 states: "The only changed existing test file is `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`, with one added line." The shipped diff against `aec0341f` also changes `PointerTree.cs`:

```csharp
-    public static KeyEvent Key(UiEventType type, int key, UiModifiers modifiers)
+    public static KeyEvent Key(UiEventType type, int key, UiModifiers modifiers, bool repeat = false)
...
-            IsRepeat = false,
+            IsRepeat = repeat,
```

The change is additive, keeps every existing call unchanged, and `context.md` Decision 15 records it with the note that the phase brief allowed the extension. It is the right change; the plan text is what fails. Verification step 7 expects "only the files of I1 and the four new test files", and the independent evaluator runs that step from the plan text. Two counts are then wrong on the page: the tree holds three new test files (`InputStateTests.cs`, `InputStateAllocationTests.cs`, `GlfwUiInputFocusTests.cs`), not four, and one more changed existing test file than I1 allows, so a literal run of step 7 reads I1 as failed although nothing in `src/` is out of scope.

**Fix:** Keep the code. Update the plan text in the same place the review-fix pass or `/update-feature` runs: in I1 (line 531), replace the last sentence of the test-file rule with "The changed existing test files are `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`, with one added line, and `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`, with the optional `repeat` parameter of Decision 15." In Verification step 7 (line 546), change "the four new test files" to "the three new test files". Add `PointerTree.cs` to the Phase 3 group 1 file list and to the plan's File Tree (line 122 to 127).

**Effort:** Low

### CR-3: The `UiGamepadState.Axis` range throw is pinned by neither a test nor a plant

**Severity:** LOW
**Rule:** 7 (test strength); task 4.1 remarks, DoD F6
**File:** `src/BrutalUi.Core/Input/UiGamepadState.cs` line 96 to 108; `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs` line 737 to 752 and line 856 to 877

**Problem:** Task 4.1 binds the rule "a value outside 0 to 5 throws `ArgumentOutOfRangeException`", and the XML doc ships it (`UiGamepadState.Axis`, line 96 to 98). No test names it: `EachGamepadSlotStartsDisconnected` throws-checks `Gamepad(-1)` and `Gamepad(16)` only (line 748 to 751), `SetGamepadWritesTheSlotAndTheEdges` checks `SetGamepad` indexes (line 784 to 785), and `TheConstructorCopiesUpToSixAxes` reads axes inside the range. The grep for an out-of-range `Axis` assert over `tests/` prints nothing. No plant covers it either (plants 11 and 12 guard the drain rules). A later edit that drops the guard, for example a switch to a masked index, changes no test result. The read-sweep gate test (`TheReadsOfTheStateAllocateNothing`) reads only axes 0 to 5, so it cannot catch it either.

**Fix:** Two lines in `TheConstructorCopiesUpToSixAxes`, next to the existing `default` check:

```csharp
UiGamepadState empty = default;
Assert.False(empty.Connected);
Assert.Equal(UiGamepadButtons.None, empty.Buttons);
Assert.Throws<ArgumentOutOfRangeException>(() => eight.Axis(-1));
Assert.Throws<ArgumentOutOfRangeException>(() => eight.Axis(6));
```

**Effort:** Low

### CR-4: The documented rule "`KeyboardEnabled` also clears after `Dispose`" has no test

**Severity:** LOW
**Rule:** 7 (test strength); plan Internal Shape (the adapter), `GlfwUiInput` remarks
**File:** `src/BrutalUi.Glfw/GlfwUiInput.cs` line 216 to 217 and 223 to 235; `tests/BrutalUi.Glfw.Tests/GlfwUiInputFocusTests.cs` line 126 to 141

**Problem:** The setter remarks state: "The setter also calls after `Dispose`, because the call only sets a flag." The code matches (no `_disposed` guard in the setter, and the plan binds it). The dispose test covers only the seam that does stop:

```csharp
input.Dispose();
input.PushFocus(false);
tree.Context.Update(0f);
Assert.True(tree.Context.Input.Keys.IsDown(W));   // PushFocus stops after Dispose
```

No test writes `KeyboardEnabled = false` after `Dispose`. A later edit that adds a symmetric `if (_disposed) return;` to the setter — a shape that looks like a fix — breaks the documented rule and the stuck-key guarantee it exists for (a game that Disposes the adapter while ImGui holds the keyboard would keep held keys), and no test or plant changes color.

**Fix:** Three lines at the end of `DisposeStopsTheFocusClear`, without adding a test:

```csharp
        // The setter still clears after Dispose: the call only sets a flag.
        input.KeyboardEnabled = false;
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.IsDown(W));
```

**Effort:** Low

### CR-5: Three stale counts that Decision 21 locked during the phases stay in the tree

**Severity:** LOW
**Rule:** 9 (docs truth); Decision 21
**File:** `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs` line 25 and line 310; `samples/HelloPanel/Program.cs` line 786 to 787

**Problem:** The adapter now subscribes nine window events (`GlfwUiInput.cs` class remarks say nine), and the longest input text is 48 bytes (`HelloScreen.LongestInputText`). Three comments still print the pre-phase numbers, exactly as Decision 21 recorded for this pass:

```csharp
// GlfwUiInputTests.cs:25
/// <summary>The names of the eight window events that the adapter subscribes to.</summary>

// GlfwUiInputTests.cs:310
/// A real hidden window: the constructor adds one handler to each of the eight events, seeds the last cursor

// Program.cs:786
// The scratch buffer of the input label. The longest input text needs 32 bytes, so the buffer never needs a
// change of its size. The loop allocates nothing in it.
```

**Fix:** Write "nine" twice in `GlfwUiInputTests.cs` (lines 25 and 310), and "needs 48 bytes" in `Program.cs` (line 786). Decision 21 already gives the exact wording; the scope lock that held it is past. These are comment-only edits, so the one added `EventNames` line that I1 counts stays the only code-line change in that file.

**Effort:** Low

### CR-6: The draw-command comment in `BuildWidgetPanel` counts two labels where the tree holds three

**Severity:** LOW
**Rule:** 10 (sample correctness); Decision 8 (doc truth over the minimal diff)
**File:** `samples/HelloPanel/HelloScreen.cs` line 281 to 282

**Problem:** Phase 1 group 2 fixed the class remarks from "the two labels" to "the three labels" (Decision 8), and the line 77 remark now reads "The three labels come before the image." The inline comment in the same method got half of the same fix:

```csharp
        // The two labels come before the image, so the panel and the three labels share one draw command.
```

The first clause contradicts the tree (greeting, gate, and input labels all sit before `checkerImage`, line 287 to 290), the class remarks (line 77), and its own second clause. A reader takes "the two labels" as gate plus input and wonders which third label shares the command.

**Fix:** One word:

```csharp
        // The three labels come before the image, so the panel and the three labels share one draw command.
```

**Effort:** Low

### CR-7: The `GameInput` remarks state only the pointer half of the gate

**Severity:** LOW
**Rule:** 4 (API clarity); Quality Bar line 16 (the remarks state the gate rule)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs` line 1031 to 1036

**Problem:** The gate paragraph on `GameInput` was written in phase 1 and never grew with phases 2 and 3:

```csharp
    /// <para>
    /// The pointer part is empty when <see cref="UiRoutingGate.PointerOverUi"/> was <see langword="true"/> at the
    /// end of the last drain: the position holds (NaN, NaN) and the delta is (0, 0). The game then sees no pointer
    /// movement from the UI.
    /// </para>
```

The shipped gate also zeroes the scroll sum and the button masks over the UI, and empties the key part when `KeyboardFocusInUi` was true (`InputStateWriter.EndFrame` line 212 to 227; DoD F2, F3, F4). The property is the one place a game developer reads before touching `GameInput`, and the Quality Bar binds the gate rule to its remarks; the full rule today lives only on the part types. (Task 1.4 named only the pointer part because the phase knew only the pointer part; the finding is the missed growth, the same class as CR-6.)

**Fix:** Extend the paragraph with two sentences; docs only, no code line changes:

```csharp
    /// <para>
    /// The pointer part is empty when <see cref="UiRoutingGate.PointerOverUi"/> was <see langword="true"/> at the
    /// end of the last drain: the position holds (NaN, NaN), the delta is (0, 0), the scroll is (0, 0), and the
    /// button masks are zero. The game then sees no pointer input from the UI. The key part is empty when
    /// <see cref="UiRoutingGate.KeyboardFocusInUi"/> was <see langword="true"/> at that point. The gamepad slots
    /// get no gate.
    /// </para>
```

**Effort:** Low

---

## What's Already Good

- The hook contract is exact. The three `_input.` call sites sit in `DrainEvents` at the three places Decision 4 names, each with a one-line comment, and the diff changes no existing drain line. The `_draining` throw, the stale rules, the target step, the walk, `FinishRelease`, the Tab move, and the unhandled input step keep their order, and every existing input-routing test runs with no edit (Q5).
- The ownership model is the strong version of A3. Two masks keep press and release ownership separate, `_uiPresses` survives the frame so a drag out of the UI cannot hand a UI press to the game, and a new game down takes the button back in the same frame. The four tests and plant 4 pin the corner cases the risk table calls the highest-likelihood fault.
- The writer respects the mutable-struct rule and states it: `Create` returns the value, both constructors assign it to the field, every later change runs through the field, and the remarks name `EventQueue` as the pattern owner. No field holds a reference, the Q4 grep prints nothing, and the struct copies stay one `Game = Raw` per drain (Decision 12).
- The bit rules live on the part types, not in the writer: `Press`, `Release`, `Mask`, `BitOf`, `KeyDown`, `KeyUp`, `ReleaseAll`, and `WriteEdgesInto` each keep one rule next to its bits, and the guards are uniform (`(uint)code < (uint)Count`). `Set` and `Clear` skip the second check only behind a checked call, and Decision 17 records why.
- The allocation gate is a true proof, not a form: fixed sums 10 and 16 derived from the read body, a full query sweep over 349 keys, 8 buttons, and 16 slots on both views, both key paths inside one measured loop through the step parity, and four plants with byte counts that divide by the x64 object size exactly. Plant 16 shows the read gate bites a single allocation inside one query.
- The 804-byte shape matches the plan estimate, `TheStateStaysNearOneKilobyte` asserts the bound, and the seam names the two sibling features rely on (`SetGamepad`, the `UiGamepadState` constructor, `Gamepad(int)`, the query set) all ship with the planned signatures and range rules, so gamepad-axes needs no `UiContext` change and input-actions needs no rename.
- The adapter work turned the plan's wrong guess into a recorded rule instead of a silent fix: Decision 19 captured the `GlfwFocusEvent` signature from the CS0123 step the plan itself prescribed, and the five focus tests pin gain-no-clear, loss-clears, switch-clears, same-value-does-nothing, and Dispose-stops-the-seam through the seam adapter without a window.
- Every phase ran its plants with the compile check the plan demands (`0 Error(s)` before the exit code), restored with `cmp` proof, and recorded each expected-plus-collateral failure list in `context.md`. No plant result was taken on the old binary.
- The docs work is nearly complete truth: section 9 item 11 states the sizes, the hooks, and eight of the nine rules exactly as the code does, section 14 counts all sixteen plants with their failed-test lists and bytes, and the CHANGELOG names the six new public members and the adapter change with the feature tag.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-1 | Medium | Low | The technical design is the rulebook every later input feature reads. It currently states the ownership rule for releases backwards, the one case the map-controls bug class turns on. |
| 2 | CR-2 | Medium | Low | The independent evaluator runs Verification step 7 from the plan text and reads I1 as failed over a change the phases deliberately allowed. Fix the plan text before the evaluation pass. |
| 3 | CR-4 | Low | Low | The documented post-`Dispose` setter clear is the guarantee a disposed adapter still releases keys; one uncolored edit (a symmetric `_disposed` guard) would silently break it. |
| 4 | CR-3 | Low | Low | The `Axis` throw ships as public API behavior with no test behind it; two asserts close it. |
| 5 | CR-7 | Low | Low | The `GameInput` remarks are the property page a game reads; two sentences complete the gate rule there. |
| 6 | CR-5 | Low | Low | Three stale counts that Decision 21 deferred to exactly this pass. Comment-only edits. |
| 7 | CR-6 | Low | Low | One wrong number in a sample comment. Take it with CR-5 in the same prose sweep. |

---

## Progress

- [x] CR-1: Section 9, item 11 states the ownership rule for the released flag wrongly (docs/technical-design.md:904)
- [x] CR-2: I1 does not fit the shipped diff: `PointerTree.cs` changed outside the named file list (tests/BrutalUi.Core.Tests/Input/PointerTree.cs:170-184; implementation.md:531, 546)
- [x] CR-3: The `UiGamepadState.Axis` range throw is pinned by neither a test nor a plant (src/BrutalUi.Core/Input/UiGamepadState.cs:96-108; InputStateTests.cs:856-877)
- [x] CR-4: The documented rule "`KeyboardEnabled` also clears after `Dispose`" has no test (src/BrutalUi.Glfw/GlfwUiInput.cs:216-217; GlfwUiInputFocusTests.cs:126-141)
- [x] CR-5: Three stale counts that Decision 21 locked during the phases stay in the tree (GlfwUiInputTests.cs:25, 310; Program.cs:786-787)
- [x] CR-6: The draw-command comment in `BuildWidgetPanel` counts two labels where the tree holds three (samples/HelloPanel/HelloScreen.cs:281-282)
- [x] CR-7: The `GameInput` remarks state only the pointer half of the gate (src/BrutalUi.Core/Tree/UiContext.cs:1031-1036)
