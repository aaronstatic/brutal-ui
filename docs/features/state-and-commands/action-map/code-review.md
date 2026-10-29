# state-and-commands/action-map - Code Review

**Reviewed:** 2026-10-29
**Reviewer:** Agent (cross-phase review of phases 1 to 7)
**Rules Applied:** 1. The slot rules (Core), 2. One input, one action, 3. The text guard, 4. The gamepad edge and the poll, 5. The one-map rule (A8) and the focus actions, 6. The build rules, the re-entry rule, and the error rule, 7. 0 bytes after warm-up, and a strong gate, 8. The Core, Framework, and Glfw split, 9. API clarity and correct remarks, 10. The seam for the later features, 11. The sample, 12. Test quality, 13. The docs, 14. The project rules. The Vercel React rules do not apply to C#.
**Status:** Resolved - 6 findings, 6 resolved (review groups 1 to 4)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 1 | CR-1 |
| LOW | 5 | CR-2, CR-3, CR-4, CR-5, CR-6 |

The feature does what the plan asks. The slot block of the drain follows the Internal Shape of the plan, and no other line of the drain changed. The map, the gamepad edge, the poll, and the focus commands follow Decisions 1 to 12. The gate measures 0 bytes in Debug, and the per-call paths hold no box.

One finding is a fault in the code. A map that the game activates on a second context leaves its delegate in the slot of the first context. `Deactivate` cannot clear that slot, and the text guard then reads the focus of the wrong context. A probe proved that a typed `W` in a focused text field of the first context calls its action (CR-1).

Four findings are gaps in the tests. Three plants pass each test of the solution: a `PushGamepad` scan that reads the fields live (CR-2), a text guard that checks Control only (CR-3), and a slot block that also fires for a char (CR-4). No test guards the length of the fixed buffer that the unsafe read of `PollGamepad` assumes, and no test reads the `false` path (CR-5). One finding is a stale statement in section 14 of the technical design (CR-6).

Rules 1, 2, 3, 4, 6, 7, 8, 9, 10, 11, and 14 have no finding in the code. The test gaps of rules 1, 3, 4, and 6 are under rule 12. Rule 5 has CR-1. Rule 12 has CR-2, CR-3, CR-4, and CR-5. Rule 13 has CR-6.

### Baseline

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2726, failed 0, succeeded 2723, skipped 3. The three skipped tests are the GLFW tests that need a display: the two old tests and `ALivePollAllocatesNothing`.
- The class counts: `UnhandledInputTests` 12, `UiActionMapTests` 23, `FocusActionTests` 5, `GamepadActionTests` 7, `GlfwUiInputGamepadTests` 6, and `ActionMapAllocationTests` 5. Each count matches the plan.
- Rule 8: `git diff d8a1ccb --stat` names one project file, `samples/HelloPanel/HelloPanel.csproj`. No test file of the base and no file of `scripts/` changed. `BrutalUi.Glfw.csproj` and `BrutalUi.Framework.csproj` reference Core only.

### Probe method

The review copied `UiActionMap.cs` and `UiContext.cs` to the scratchpad. Then it made three plants with the edit tool and added one probe test file. One run of `dotnet test --solution brutal-ui.sln` gave total 2727, failed 0. The review then restored both files with `cp`, and `cmp` found no difference. It deleted the probe file. `git status --porcelain` matched the state before the plants. A new build and test run gave the baseline counts again.

### Probe list

- Plant A: `PushGamepad` reads `_bindings` and `_bindingCount` in the loop, with no local copy. Each test passes (CR-2).
- Plant B: the text guard checks `UiModifiers.Control` only, not Alt and Super. Each test passes (CR-3).
- Plant C: the slot block calls the slot for a `KeyDown` or a `Char` entry. Each test passes (CR-4).
- Probe D: one map, two contexts, a focused `TextField` in the first context, and a bind of `W`. The test passes with each of its asserts (CR-1).

---

## Findings

### CR-1: An `Activate` on a second context leaves the slot of the first context

**Severity:** MEDIUM
**Rule:** 5 (The one-map rule (A8) and the focus actions)
**File:** `src/BrutalUi.Framework/Actions/UiActionMap.cs`, lines 427 to 446 (`Activate`), lines 458 to 473 (`Deactivate`), and lines 511 to 524 (`IsTypedByTextField`)

**Problem:** `Activate` writes the slot of the new context and then overwrites `_context`:

```csharp
context.OnUnhandledInput = _onInput;
_context = context;
```

The method does not clear the slot of the old context. After `map.Activate(a)` and `map.Activate(b)`, both slots hold the delegate of the map. Then three faults occur:

1. `Deactivate` clears the slot of `b` only, and it sets `_context` to `null`. The slot of `a` keeps the map, and no public call of the map can clear it.
2. The text guard reads `_context?.FocusedElement`, which is the focus of `b`, or no focus after `Deactivate`. A key of `a` thus passes the guard while a text field of `a` holds the focus.
3. `IsActive` gives `false`, but the map still gets the keys of `a`.

Probe D proved it. A `W` key down in `a`, with the text field of `a` in focus, called the action one time before `Deactivate` and one more time after it. The quality bar names "a typed letter that fires an action" as a fault that breaks each game. A game with a screen context and a world-space context can activate one map on both.

**Fix:** At the start of `Activate`, when `_context` is another context and its slot holds `_onInput`, write `null` into that slot. A map is then active on one context at most, and `Deactivate` clears the only slot. State the rule in the remarks of `Activate` and in the one-map rule of section 9. Add one test to `UiActionMapTests`: activate one map on two contexts, and assert that the slot of the first context is `null`. Then push a `W` into a focused field of the first context, and assert no call. Follow-up question 1 holds the other option (a throw).

**Effort:** Low

### CR-2: The re-entry test cannot fail for the scan of `PushGamepad`

**Severity:** LOW
**Rule:** 12 (Test quality), for rule 6 (the re-entry rule)
**File:** `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`, lines 693 to 725 (`ABindDuringAPressTakesEffectAtTheNextPress`)

**Problem:** The re-entry rule states that each resolve copies the binding array and the count before the scan. The test binds a key during a `PressKey`. But `PressKey` calls `Find`, and `Find` returns before the command runs. Thus the test passes with or without a local copy. The only scan that calls a command and then goes on is the scan of `PushGamepad`. Plant A removed its local copy, and each test passed. With plant A, a command that binds a gamepad button during a push fires that button in the same push.

**Fix:** Keep the count at 23. Add a step to the test. Bind `A` to an action whose command binds `B` to the counter action. Call `PushGamepad(A | B)`, and assert the return value 1 and no call of the counter. Then push `None` and `B`, and assert one call. Run plant A again and expect a failure.

**Effort:** Low

### CR-3: No test shows that Alt or Super lifts the text guard

**Severity:** LOW
**Rule:** 12 (Test quality), for rule 3 (the text guard)
**File:** `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`, lines 520 to 559 (`TheTextGuardPassesANonTextKeyAndAShortcut`)

**Problem:** Decision 6, the remarks of `UiActionMap`, and section 9 state that a text key with Control, Alt, or Super passes the guard. The test uses Control+S only. Plant B changed the guard to check Control only, and each test passed. A shortcut such as Alt+S then calls nothing while a text field holds the focus.

**Fix:** Keep the count at 23. In the same test, bind S with Alt and S with Super to `Confirm`. Push each one with the field in focus, and assert one more call for each. Run plant B again and expect a failure.

**Effort:** Low

### CR-4: No Core test pushes a char with the slot set

**Severity:** LOW
**Rule:** 12 (Test quality), for rule 1 (the slot rules)
**File:** `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs`, lines 104 to 126 (`AKeyUpDoesNotCallTheSlot`)

**Problem:** Rule 1, the remarks of `UiUnhandledInputHandler`, and item 10 of section 9 state that the slot gets no char. No test of `UnhandledInputTests` pushes a `Char` entry. Plant C added `Char` to the key case of the slot block, and each test of the solution passed. The slot then gets a `KeyDown` call for each typed char, with a code from the char payload.

**Fix:** Keep the count at 12. Rename the test to `AKeyUpAndACharDoNotCallTheSlot`. Push a char with no focus owner, and push it again with the focus on `Left`. Assert a count of 0 after each update. Run plant C again and expect a failure.

**Effort:** Low

### CR-5: No test guards the unsafe read or the `false` path of `PollGamepad`

**Severity:** LOW
**Rule:** 12 (Test quality), for rule 4 (the gamepad edge and the poll)
**File:** `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs`, lines 77 to 100 (`TheBitsFollowTheGlfwButtonIndices`) and lines 125 to 152 (`ALivePollAllocatesNothing`)

**Problem:** `PollGamepad` in `src/BrutalUi.Glfw/GlfwUiInput.cs` (lines 263 to 272) reads the fixed buffer with a fixed length:

```csharp
return GamepadButtons(new ReadOnlySpan<byte>(state.Buttons, GamepadButtonCount));
```

The code is correct for `Ahwoo.BRUTAL.Glfw` 2026.10.4, where `Buttons` is `fixed byte[15]` (the probe of task 3.5). The tests check `GLFW_GAMEPAD_BUTTON_LAST + 1`, but no test reads the length of the buffer. A package update with a shorter buffer reads past the struct, and no test fails. Also, no test reads the `false` result of `GetGamepadState`. The live test asserts the bytes only, and it skips with no display.

**Fix:** Keep the count at 6. In `TheBitsFollowTheGlfwButtonIndices`, read the `FixedBufferAttribute` of the field `GlfwGamepadState.Buttons` with reflection. Assert the length 15 and the element type `byte`. In `ALivePollAllocatesNothing`, after the measure, find a joystick id that `Glfw.JoystickPresent` reports as absent. Assert that `PollGamepad` gives `None` for it.

**Effort:** Low

### CR-6: Section 14 omits the gamepad calls from the UI region of the sample

**Severity:** LOW
**Rule:** 13 (The docs)
**File:** `docs/technical-design.md`, line 1280 (the bullet "Sample projects")

**Problem:** The feature rewrote the bullet, but the gate sentence keeps the old list of the UI region:

> when the UI region (the smoke input, `ViewportSize`, `Update`, `Draw`, the gate read, and the label update) allocates after 3 warm-up frames.

`Program.cs` (lines 719 to 755) now calls `PollGamepad` before the region and `UiActionMap.PushGamepad` as the first line of the region. The bullet names neither call. Rule 11 and Decision 11 make this order a rule of the sample.

**Fix:** Change the list to "(the gamepad push, the smoke input, `ViewportSize`, `Update`, `Draw`, the gate read, and the label update)". Add one sentence: "The gamepad poll runs after `Glfw.PollEvents` and outside the UI region, so an allocation of the BRUTAL binding cannot fail the gate." Run the lint on the file.

**Effort:** Low

---

## What's Already Good

- The slot block reads `OnUnhandledInput` one time for each entry, after the walk, `FinishRelease`, and the Tab move. The diff of `UiContext.cs` holds the property, the local `tabMove`, the slot block, and doc lines only. A `null` slot skips the block, so each old gate keeps its count.
- The press case is correct for each change of the routing gate. The capture branch of `RoutePointer` applies to a move only. The hit test finds a disabled element, so a press on a disabled button never reaches the slot. The hit test walks the roots only, so a removed element cannot be the target of a press.
- The resolve is exact. `Find` masks the modifiers with Shift, Control, Alt, and Super and compares them for equality. `TheModifiersMustMatch` fails for a subset match. The kind field keeps a key and a mouse button with the same code apart.
- `PushGamepad` writes `_gamepad` before the first invoke and scans in the order of the bind. `ACommandThatThrowsLeavesTheMapUsable` proves that a held button does not fire again after a throw.
- `Activate` writes the slot with `=`. `IsActive` uses the delegate `==` operator, and `Deactivate` clears the slot of its own map only on one context.
- Each build method checks the code, then the action, then the duplicate, before the array grows. Each message is constant. The resolve methods hold no `throw`.
- The text guard constants match GLFW: Escape is 256, `KP_0` is 320, and `KP_EQUAL` is 336. A key of -1 (the GLFW unknown key) can never match, because `BindKey` rejects a key below 0.
- The unsafe read of `PollGamepad` is safe. The local `state` is a fixed variable, the span lives inside the call, and the read takes the 15 button bytes and no axis.
- `ActionMapAllocationTests` releases the allocation context right before each measure. Each measured body holds no lambda, no string, no box, no array, and no `Assert`. The real-work test fails for an empty body of each of the four steps, because each step has a fixed increment or a fixed focus owner.
- The per-call paths of `Actions/`, the slot block, and `PollGamepad` hold no `(object)` cast, no `Equals` or `HasFlag` call on a value, no enum `ToString`, no `params` array, and no closure. The handler delegate and the focus handlers come from build time.
- HelloPanel keeps the caret check, the gate check, and the widget check, and it adds the state check. The Escape push in frame 3 always falls in a run with 2 measured frames or more.
- The project rules hold. Each new file has file-scoped namespaces and its own `using` lines. `UiActionMap.CreateInfo` has a `required` member. `Actions/` holds no `ToString(`, `event`, `lock`, or `IDisposable` in code.

---

## Follow-up Questions

1. **The shape of the CR-1 fix.** A map can get an `Activate` on a second context while it is active on the first context.
   - (A) `Activate` clears the slot of the old context first, so the last `Activate` wins.
   - (B) `Activate` throws `InvalidOperationException` while the map is active on another context.
   - (C) The map holds a list of contexts, and the text guard reads the focus of the context that called it.
   - **Default:** (A). It matches the rule "a second `Activate` replaces the first map", and it adds no throw to a mode change.

2. **A dispatcher of panels-and-modes.** Decision 8 lets panels-and-modes put a dispatcher of its own in the slot. The repeat rule and the text guard live in the private `HandleInput`. A dispatcher that calls `PressKey` on each map of a mode thus loses both rules.
   - (A) Add a public method that applies the repeat rule and the text guard for a given context and returns `true` when a binding matched.
   - (B) The dispatcher copies the repeat rule and the guard.
   - (C) panels-and-modes calls `Activate` for one map of the mode only.
   - **Default:** (A), in the plan of panels-and-modes. This feature needs no change.

3. **A change of a binding.** Decision 3 names a rebind screen as a reason for the level of names. The map has no call that removes, replaces, or lists a binding. xml-markup also cannot read the table back.
   - (A) Add `Unbind` and a read of the bindings when a rebind screen or xml-markup needs them.
   - (B) Add them now.
   - (C) A rebind builds a new map.
   - **Default:** (A). No feature of this epic needs a rebind.

4. **A gamepad bit above bit 14.** `BindGamepadButton` takes a value with one bit, also a bit that `UiGamepadButtons` does not define, for example `1 << 20`. `PollGamepad` never sets that bit, so the binding never fires. The phase 6 notes record this choice.
   - (A) Reject a value outside the 15 buttons.
   - (B) Keep the current rule.
   - **Default:** (A). It costs one compare at build time and catches a cast error.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort |
|-------|---------|--------|--------|
| 1 | CR-1: An `Activate` on a second context leaves the slot of the first context | Medium: a typed letter can fire an action, and `Deactivate` cannot clear the slot | Low |
| 2 | CR-2: The re-entry test cannot fail for the scan of `PushGamepad` | Low: a regression of the re-entry rule stays hidden | Low |
| 3 | CR-3: No test shows that Alt or Super lifts the text guard | Low: a regression of the guard stays hidden | Low |
| 4 | CR-4: No Core test pushes a char with the slot set | Low: a regression of the slot rules stays hidden | Low |
| 5 | CR-5: No test guards the unsafe read or the `false` path of `PollGamepad` | Low: a package update can read past the struct | Low |
| 6 | CR-6: Section 14 omits the gamepad calls from the UI region of the sample | Low: the doc does not match the sample | Low |

---

## Progress

- [x] CR-1: An `Activate` on a second context leaves the slot of the first context (review group 1: `Activate` clears the old slot first, follow-up question 1 option A. Review group 4: the rule in section 9.)
- [x] CR-2: The re-entry test cannot fail for the scan of `PushGamepad` (review group 1: a gamepad step in `ABindDuringAPressTakesEffectAtTheNextPress`. Plant A fails it.)
- [x] CR-3: No test shows that Alt or Super lifts the text guard (review group 1: Alt+S and Super+S in `TheTextGuardPassesANonTextKeyAndAShortcut`. Plant B fails it.)
- [x] CR-4: No Core test pushes a char with the slot set (review group 2: `AKeyUpAndACharDoNotCallTheSlot`. Plant C fails it.)
- [x] CR-5: No test guards the unsafe read or the `false` path of `PollGamepad` (review group 3: the `FixedBufferAttribute` check and the absent-id check)
- [x] CR-6: Section 14 omits the gamepad calls from the UI region of the sample (review group 4)

The review verifier returns PASS after the four groups: build 0 warnings and 0 errors, solution 2726 (2723 passed, 3 skipped with no display) with 0 failed, each class count of the plan, `scripts/test-linux.sh` PASS, and the lint of the technical design at 1.52. Each class count stays as in the plan, because each fix extends an existing test.
