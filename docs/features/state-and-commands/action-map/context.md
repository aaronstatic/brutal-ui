# state-and-commands/action-map - Context & Decisions

**Last Updated:** 2026-10-29 15:20
**Current Phase:** Complete (phases 1 to 7 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan (`implementation.md`). The user chose (A) for each of the four open scope questions on 2026-10-29.
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file)

- ✅ Phase 1 group A (tasks 1.1 to 1.5): the Core slot `UiContext.OnUnhandledInput`, the delegate `UiUnhandledInputHandler`, and `UnhandledInputTests` (9 passed).
- ✅ Phase 1 group B (tasks 1.6 to 1.9): `UiActions`, `UiActionMap` (the key path), and `UiActionMapTests` (8 passed). Phase 1 is done.

- ✅ Phase 2 (tasks 2.1 to 2.5): `FocusCommands`, `IsActive`, `Deactivate`, `FocusActionTests` (5 passed), and 3 one-map tests (`UiActionMapTests` 11 passed).

- ✅ Phase 3 (tasks 3.1 to 3.8): `UiGamepadButtons`, `PushGamepad`, `GlfwUiInput.PollGamepad`, `GamepadActionTests` (7 passed), and `GlfwUiInputGamepadTests` (6 passed under `xvfb-run`).

- ✅ Phase 4 (tasks 4.1 to 4.6): the press case of the slot, `BindMouseButton`, `PressMouseButton`, `UnhandledInputTests` (12 passed), and `UiActionMapTests` (15 passed).

- ✅ Phase 5 (tasks 5.1 to 5.7): `HelloState`, the state row, the poll and the push, line 6 `HelloPanel: state: adds 119, resets 30, versions 149.`, and the two plants (each `exit=1`).

- ✅ Phase 6 (tasks 6.1 to 6.3): the text guard, the build rules, the re-entry rule, and `UiActionMapTests` (23 passed).

- ✅ Phase 7 (tasks 7.1 to 7.7): `ActionMapAllocationTests` (the seventeenth gate, 5 passed, 0 bytes in Debug), the four plants (each `exit=2`), sections 3, 4, 9, 10, and 14 of the technical design, and the changelog.

- ✅ The cross-phase review (`code-review.md`): 6 findings (1 MEDIUM, 5 LOW), 6 fixed in review groups 1 to 4. The review verifier returns PASS. Each verifier of phases 1 to 7 returns PASS, with Demo: PASS.

**What's Next:**
- 📋 A person tests the lines of `## Needs Human Verification`.
- 📋 `/autorun-epic` commits the feature and closes the state-and-commands epic.

**Blockers:**
- None

---

## Base and Counts

- Base commit: `d8a1ccb948b98914885632c26c8b251fcca9e0cc` (`feat: state-and-commands/commands-and-events`). The commands-and-events commit exists.
- Expected counts before any code change (from the commands-and-events context): solution 2668 (2666 passed, 2 skipped: the GLFW tests, no display), `BrutalUi.Core.Tests` 1733, `BrutalUi.Framework.Tests` 93. Task 1.1 reads each count from a run and records the `BrutalUi.Glfw.Tests` count.
- Measured counts before any code change (task 1.1, 2026-10-29, `git log --oneline -3` shows `d8a1ccb` at the top): `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2668, failed 0, succeeded 2666, skipped 2. A run of each project gives `BrutalUi.Core.Tests` 1733 (1733 passed), `BrutalUi.Framework.Tests` 93 (93 passed), and `BrutalUi.Glfw.Tests` 21 (19 passed, 2 skipped: no display).

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs` - the slot delegate (phase 1, phase 4)
- `src/BrutalUi.Core/Tree/UiContext.cs` - `OnUnhandledInput` and the slot call in `DrainEvents` (phase 1, phase 4)
- `src/BrutalUi.Core/Input/UiEventEnums.cs` - `UiGamepadButtons` (phase 3)
- `src/BrutalUi.Framework/Actions/UiActions.cs` - the action names (phase 1)
- `src/BrutalUi.Framework/Actions/UiActionMap.cs` - the map (phases 1, 2, 3, 4, 6)
- `src/BrutalUi.Framework/Actions/FocusCommands.cs` - the focus commands (phase 2)
- `src/BrutalUi.Glfw/GlfwUiInput.cs` - `PollGamepad` (phase 3)
- `samples/HelloPanel/` - the state row, the poll, the push, and line 6 (phase 5)

### Tests
- `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs` - 9, then 12 tests (phases 1 and 4)
- `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs` - 8, 11, 15, then 23 tests (phases 1, 2, 4, 6)
- `tests/BrutalUi.Framework.Tests/Actions/FocusActionTests.cs` - 5 tests (phase 2)
- `tests/BrutalUi.Framework.Tests/Actions/GamepadActionTests.cs` - 7 tests (phase 3)
- `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs` - 6 tests (phase 3)
- `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` - the seventeenth allocation gate, 5 tests (phase 7)

### Related Files
- `docs/features/state-and-commands/action-map/implementation.md` - Implementation plan
- `docs/features/state-and-commands/epic-overview.md` - the epic rollup and the build order
- `docs/features/state-and-commands/commands-and-events/context.md` - `CommandRegistry`, the gate shape, and the Gotchas
- `docs/features/state-and-commands/element-bindings/context.md` - the Gotchas (the Debug box, the real-work test, the scoped grep)

---

## Important Decisions

The plan holds Decisions 1 to 12 and the Confirmed Assumptions. The user chose (A) for each of the four Notes questions: one slot `OnUnhandledInput` for a key and a press (Decision 1), the Core enum `UiGamepadButtons` with a push by the game (Decision 10), the text guard in the map (Decision 6), and the phase 1 demo on `UiActionMapTests`. This section records each decision that the run makes past the plan.

- **The Seam.** The map lives in Framework, and the input pass lives in Core. Core cannot name `UiActionMap`. The seam is one delegate slot on `UiContext`, in the shape of `OnFileDrop`: `OnUnhandledInput` of the type `UiUnhandledInputHandler`. The drain calls it for an input that no element took. `UiActionMap` creates one instance delegate of its handler in its constructor, and `Activate` writes it into the slot. The gamepad crosses as the Core flags enum `UiGamepadButtons` (bit `i` is the GLFW gamepad button `i`). `GlfwUiInput.PollGamepad` returns it, and the game pushes it into the map.
- **The Internal Shape.** The drain stores the Tab condition in a local `tabMove`. After the Tab branch, one block reads `OnUnhandledInput` into a local and calls it when the slot is set, `routed` is `true`, `stopped` is `false`, and either the entry is a key down that is not a Tab move (phase 1) or a press with no hit target (phase 4). The map holds `ActionEntry[]` (a name and a command name, both UTF-8 copies) and `InputBinding[]` (a kind, a code, the modifiers with the mask Shift, Control, Alt, Super, and the action index). Both arrays start empty, then hold 8 slots, then double. The resolve scans in the order of the bind and calls `CommandRegistry.Invoke`. `PushGamepad` computes `buttons & ~_gamepad`, writes `_gamepad` first, and invokes each new press in the order of the bind.
- **The real local names of the drain (task 1.3).** The names of the plan are the real names: `entry`, `target`, `routed`, and `stopped`. The new locals are `tabMove` and `handler`.
- **The shape of the slot block (task 1.3).** The outer `if` checks `handler is not null && routed && !stopped`. The inner `if` checks `entry.Header.Type == UiEventType.KeyDown && !tabMove`. Phase 4 adds case 2 as an `else if` on the inner `if`: `entry.Header.Type == UiEventType.PointerDown && target is null`.
- **No `+=` on the slot (task 1.3).** The remarks of `OnUnhandledInput` do not copy the `+=` sentence of `OnFileDrop`. They tell the reader to write the slot and not to use `+=`, because the phase 2 `IsActive` compares the slot with the delegate of the map. A combined delegate breaks that compare.
- **The parameter doc of `type` (task 1.2).** The doc states only `UiEventType.KeyDown`. Phase 4 (task 4.1) adds `PointerDown`.
- **The tests read the focus at the slot call (task 1.4).** The recorder also stores `FocusedElement` at each call. Thus the tests check the rule "the slot reads the focus of that point in the ring": after a Control+Tab, and with a focused text field.
- **A private exception type in the throw test (task 1.4).** The slot throws `SlotException`, so the test does not mix it up with the `InvalidOperationException` of the drain guard.
- **The empty arrays of the map (task 1.7).** Both arrays start as `[]` (the shared empty array). The first add or bind resizes to 8, and each later full array doubles. Thus the fields are never `null`, and the scans need no `null` check.
- **Phase 1 `AddAction` checks nothing (task 1.7).** It takes an empty name and a duplicate name. `InvokeAction` and each bind use the first action with the name. Phase 6 adds the name rules.
- **`PressKey` reads the action of the binding after `Find` (task 1.7).** `Find` copies the array and the count to locals and returns the binding index. `PressKey` then reads `_bindings[index].Action`. No code runs between the two reads, so the re-entry rule holds.
- **`InvokeAction` and `BindKey` share one private name scan (task 1.7).** `IndexOfAction` copies the action array and the count to locals and compares with `SequenceEqual`, as `CommandRegistry.IndexOf` does.
- **The constructor message (task 1.7).** "The Commands of the UiActionMap.CreateInfo must not be null.", in the shape of the message of `UiContext`.
- **`_context` is written and not read in phase 1 (task 1.7).** The build gives no CS0414 for it. Phase 2 (`IsActive`) and phase 6 (the text guard) read it.
- **The test file holds no `Pointer` helper (task 1.8).** No phase 1 test pushes a pointer, so the copy waits for phase 4 (task 4.5). The `Key` helper takes the modifiers and the repeat flag as optional parameters. A private `CreateMap` helper builds a registry with one counter command and a map on it.
- **Extra checks in the 8 tests (task 1.8).** The demo also reads `ActionCount`, `BindingCount`, the slot, no focus owner, and an empty ring. The lock test also binds Control+CapsLock+S (the bind drops the lock bit) and checks that Shift still blocks a match. The repeat test pushes a key down with no repeat after the repeat, so the test proves that the path works. The no-activate test checks that `PressKey` still works. The `InvokeAction` test checks that an unknown action does not call `UnknownCommand`, and that `cancel` does not match `Cancel`.
- **The Frame Order.** The order of the passes does not change. The key and press path runs inside the input pass, at the position of its entry in the ring, so a command that sets a store shows in the binding pass of the same `Update`. The game calls `PushGamepad` after `Glfw.PollEvents` and before `Update`.

---

## Gotchas & Learnings

Carried from the sibling features:
- **A Debug build keeps a box.** Write no `(object)` cast, no `is null` on a generic value, and no `Equals` call on a value in the resolve path.
- **A real-work test reads its expected values before the body runs.** Keep a fixed-increment assert for each measured body.
- **A box grep for `Equals\(` also matches `ReferenceEquals(`.** Scope the grep to `\.Equals\(`.
- **A `static readonly` handler field that reads a later `static readonly` field in its lambda gives CS8604.** Declare the later field first.
- **`UiContext.PushEvent(in ...)` needs a variable.** Hold each event of a scene in a field.

Found in this feature:
- **A routed key down to a lost owner reaches the slot.** A handler of an earlier entry of the same drain can remove the focus owner from the roots, or disable it. The next routed key down then gets no walk, and `stopped` stays `false`. The key reaches the slot, as a key with no owner does. This is correct: no element took the key.
- **The slot gets a key down that walked to an owner with no handler.** A walk alone does not take a key. Only a stop takes it.
- **A map that moves to a second context must clear its first slot.** CR-1: `Activate` on a new context left the delegate of the map in the old slot, and the text guard read the focus of the wrong context. A test with one context cannot see it. Test each slot rule with two contexts.
- **A re-entry test must reach the scan that goes on after a call.** CR-2: `PressKey` returns after `Find`, so a bind during a key press cannot test the local copy. Only the scan of `PushGamepad` calls a command and goes on.
- **A guard test must cover each modifier that lifts the guard.** CR-3: a test with Control only passes a guard that checks Control only.
- **A verify grep needs the scope of the feature.** A grep over `src/` also matches the `InternalsVisibleTo` line of a project file and the generated files of `obj/`. Scope it to `*.cs` outside `obj/`, and filter the `///` lines.

---

## API Integration

- `CommandRegistry.Invoke(ReadOnlySpan<byte>)` (commands-and-events) is the call of each action. `UiContext.FocusNext` and `UiContext.FocusPrevious` (input-routing) are the targets of the focus commands. `UiContext.OnUnhandledInput` is the new Core entry. panels-and-modes calls `Activate` for the map of a mode.

---

## State Management

- The game owns each map, each registry, and each store. The context holds one delegate of the active map in its slot. The map holds the context of the last `Activate` and the gamepad buttons of the last push.

---

## Testing Approach

### Unit Tests
- `UnhandledInputTests` (12) in Core.Tests. `UiActionMapTests` (23), `FocusActionTests` (5), `GamepadActionTests` (7) in Framework.Tests. `GlfwUiInputGamepadTests` (6) in Glfw.Tests.

### Allocation Tests
- `ActionMapAllocationTests` (5): the seventeenth gate. Four plants guard `PressKey`, `PushGamepad`, the slot call of the drain, and the key body.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 8 with `failed: 0`. The first test pushes an Escape key down with no focus owner, runs `Update`, and reads one call of the command of `Cancel`.
- [ ] Phase 2 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*FocusActionTests'` and reads a total of 5 with `failed: 0`. The first test binds Down (264) to `NavigateDown`, presses it one time with the focus on the first of three buttons, and reads the focus on the second button.
- [ ] Phase 3 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*GamepadActionTests'` and reads a total of 7 with `failed: 0`. The first test pushes `UiGamepadButtons.B` two times and reads one call of the command of `Cancel` and the return values 1 and 0.
- [ ] Phase 4 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 15 with `failed: 0`. The test `AMouseButtonOverNoElementInvokesItsAction` presses the right button outside the panel and reads one call of the command of `Cancel`.
- [ ] Phase 5 demo: a developer runs `xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"` and reads line 6, `HelloPanel: state: adds <A>, resets <R>, versions <V>.`, with three counts above 0 and `exit=0`.
- [ ] Phase 6 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 23 with `failed: 0`. The test `TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus` presses W (87) with the field focused and reads no call of the bound action and the char in the field.
- [ ] Phase 7 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ActionMapAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
- [ ] Windows run: a person runs `scripts/test-windows.ps1` on a Windows machine and reads its PASS line. The run checks the seventeenth gate on the Windows JIT. No verifier of this run had a Windows machine.
- [ ] HelloPanel by hand: a person runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` with no argument. Press Escape: the score label shows 0. Click `Add`: the score rises. Press Up and Down: the focus moves. Type a letter into the text field: no action fires.
- [ ] Real gamepad: a person connects a gamepad, runs HelloPanel, and checks that `PollGamepad` reads the buttons. No verifier had a gamepad, so only the conversion and a live poll with no device are tested.

---

## Next Steps

### Immediate (Next Session)
1. Test the lines of `## Needs Human Verification`: the HelloPanel run by hand, a real gamepad, and the Windows run.
2. Commit the feature (the caller `/autorun-epic` owns each git action).
3. Answer the open follow-up questions 2 to 4 of `code-review.md` in the plans of panels-and-modes and xml-markup, or in a later fix.

---

## Open Questions

- The cross-phase review left three follow-up questions (`code-review.md`, `## Follow-up Questions` 2 to 4). No task of this feature waits on them.
  - A dispatcher of panels-and-modes cannot reuse the private repeat rule and text guard. Default: a public handle method, in the plan of panels-and-modes.
  - The map has no unbind call and no read of the bindings for a rebind screen or xml-markup. Default: add them when a feature needs them.
  - `BindGamepadButton` takes a single bit above bit 14, which never fires. Default: reject it. No fix in this run, because the review did not report it as a finding.
- The mosaic-ui names (Conflicts and Gaps item 3 of the plan). Default: the words of the requirement as UTF-8 action names, the prefix `ui.` for the focus commands, and the type name `UiActionMap`.

---

## Session Notes

### 2026-10-29 11:59
- `/start-feature` (called by `/autorun-epic`) wrote `tasks.md`, `inbox.md`, and this file. The base commit is `d8a1ccb`.

### 2026-10-29 Phase 1 group A (tasks 1.1 to 1.5)
- Task 1.1 recorded the base counts in "Base and Counts".
- Tasks 1.2 to 1.4 added `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`, the slot and the slot block in `src/BrutalUi.Core/Tree/UiContext.cs`, and `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs`.
- The diff of `UiContext.cs` holds the property, the local `tabMove`, the slot block, and doc comment lines only. No other file of `src/BrutalUi.Core/` changed.
- Task 1.5 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2677, failed 0, succeeded 2675, skipped 2 (2668 + 9). `BrutalUi.Core.Tests` gives 1742 (1733 + 9), all passed. `BrutalUi.Framework.Tests` gives 93, all passed. `BrutalUi.Glfw.Tests` gives 21 (19 passed, 2 skipped: no display). `UnhandledInputTests` gives total 9, failed 0. Each existing test passes with no edit.
- A grep of `src/BrutalUi.Core/` for `BrutalUi.Framework` and for a GLFW type finds no new match. The prose name `UiActionMap.Activate` in a `<c>` tag of the remarks is text, not a reference.

### 2026-10-29 12:12 Phase 1 group B (tasks 1.6 to 1.9)
- Tasks 1.6 to 1.8 added `src/BrutalUi.Framework/Actions/UiActions.cs`, `src/BrutalUi.Framework/Actions/UiActionMap.cs`, and `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`. No other file of `src/` changed in this group, and no `.csproj` changed.
- `Activate` writes the slot with `=`, as the remarks of `UiContext.OnUnhandledInput` tell.
- Task 1.9 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2685, failed 0, succeeded 2683, skipped 2 (2677 + 8). `BrutalUi.Core.Tests` gives 1742, all passed. `BrutalUi.Framework.Tests` gives 101 (93 + 8), all passed. `BrutalUi.Glfw.Tests` gives 21 (19 passed, 2 skipped: no display). `UnhandledInputTests` gives total 9, failed 0. Each existing test passes with no edit.
- The demo: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` gives total 8, failed 0, succeeded 8. The first test `AnUnhandledKeyInvokesTheCommandOfItsAction` pushes an Escape key down with no focus owner, runs `Update`, and reads one call of the command of `Cancel`.
- A grep of `src/BrutalUi.Framework/Actions/` for `lock`, `IDisposable`, `event`, `ToString(`, `(object)`, and `.Equals(` in code finds no match. A grep of the `.cs` files of `src/BrutalUi.Core/` for `BrutalUi.Framework` and for a GLFW type in code finds no match.
- Follow-up: no test checks the `null` registry of the constructor or the `null` context of `Activate`. Phase 6 (the build rule tests) can add them.

### 2026-10-29 Phase 2 (tasks 2.1 to 2.5)
- Tasks 2.1 to 2.4 added `src/BrutalUi.Framework/Actions/FocusCommands.cs` and `tests/BrutalUi.Framework.Tests/Actions/FocusActionTests.cs`. They also edited `UiActionMap.cs` (`IsActive`, `Deactivate`, and the remarks of `Activate`) and `UiActionMapTests.cs` (3 tests). No other file of `src/` changed, and no `.csproj` changed.
- Decision: `Deactivate` sets `_context` to `null` also when the map is not active (a replaced map, or a map that was never active). The map then forgets its context, and `IsActive` stays `false` until the next `Activate`. `Deactivate` is safe to call two times.
- Decision: `IsActive` uses `c.OnUnhandledInput == _onInput`, the delegate `==` operator. It boxes nothing. `Deactivate` writes the slot through `_context!` after `IsActive` proved that `_context` is set.
- Decision: `FocusActionTests` builds the three buttons with `Button.CreateInfo { Text = ... }`, runs one `Update` in the scene factory, and has a private `Scene` class with `required` fields. The Up test also presses Up a second time to show the wrap from the first button to the last. The `Register` test also invokes `Previous` and checks the two names as bytes.
- Decision: the `RegisterRejectsANullRegistryOrContext` test also checks that the registry holds 0 commands after the two failed calls.
- Task 2.5 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2693, failed 0, succeeded 2691, skipped 2 (2685 + 8). `BrutalUi.Framework.Tests` gives 109 (101 + 8), all passed. `FocusActionTests` gives total 5, failed 0. `UiActionMapTests` gives total 11, failed 0. Each existing test passes with no edit.
- The demo: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*FocusActionTests'` gives total 5, failed 0. The first test `NavigateDownMovesTheFocusToTheNextElement` binds Down (264) to `NavigateDown`, presses it one time with the focus on the first button, and reads the focus on the second button.
- A grep of `src/BrutalUi.Framework/Actions/` for `lock`, `IDisposable`, `event`, `ToString(`, `(object)`, and `.Equals(` in code finds no match.

### 2026-10-29 12:20 (phase 1 close)
- The phase 1 verifier returns PASS: build 0 warnings and 0 errors, solution 2685 (2683 passed, 2 skipped) with 0 failed, `UnhandledInputTests` 9, `UiActionMapTests` 8, the Q4 grep clean, and only `UiContext.cs` changed in `src/` with the five new files. The diff of `UiContext.cs` holds the property, `tabMove`, one slot block, and doc lines. Demo: PASS.
- The literal Core grep of the verify brief also matched the `InternalsVisibleTo` line of `BrutalUi.Core.csproj` and the files in `bin/` and `obj/`. Those lines exist at the base. Later verify briefs scope the grep to `*.cs` files outside `bin/` and `obj/`.
- Process note: the group A agent changed the new test file with one `sed` command, against tool rule 1. The result is correct.

### 2026-10-29 12:30 (phase 2 close)
- The phase 2 verifier returns PASS: build 0 warnings and 0 errors, solution 2693 (2691 passed, 2 skipped) with 0 failed, `FocusActionTests` 5, `UiActionMapTests` 11, `UnhandledInputTests` 9, the Q4 grep clean, two `static readonly CommandHandler` fields with `static` lambdas, and no file outside the feature changed. Demo: PASS.

### 2026-10-29 Phase 3 group A (tasks 3.1 to 3.4)
- Task 3.1 added `UiGamepadButtons` (`[Flags]`, 15 buttons and `None`) to `src/BrutalUi.Core/Input/UiEventEnums.cs`, after `PointerButton`. Task 3.2 added `_gamepad`, `BindGamepadButton`, and `PushGamepad` to `UiActionMap.cs`. Task 3.3 added `GamepadActionTests.cs`. No other file of `src/` changed, and no `.csproj` changed.
- Decision: `BindGamepadButton` stores the code `(int)button` with `UiModifiers.None` and checks only that the action exists. It does not reject `None` or a multi-bit value. Phase 6 adds those checks.
- Decision: `PushGamepad` writes `_gamepad` before the first command call. It compares `((int)pressed & binding.Code) != 0` and uses no `HasFlag` and no box. It returns the count of commands that the registry ran, so a press of an action with an unknown command does not count.
- Decision: a binding with more than one bit (phase 6 rejects it) would match when any one of its bits is a new press. This is a known gap until phase 6.
- Decision: `TwoNewPressesInOnePushInvokeInTheOrderOfTheBind` binds `B` first and `A` second, so the log reads B then A. This proves the order of the bind and not the order of the bits. The test uses a `Log` array and two `Mark` targets with one shared `s_mark` handler.
- Decision: `ADpadDownBindingMovesTheFocus` presses `DpadDown`, then `None`, then `DpadDown`, and reads the focus on the first and then on the second button.
- Task 3.4 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2700, failed 0, succeeded 2698, skipped 2 (2693 + 7). `BrutalUi.Core.Tests` gives 1742 (no change), `BrutalUi.Framework.Tests` gives 116 (109 + 7), all passed. `GamepadActionTests` gives total 7, failed 0. Each existing test passes with no edit.
- The demo: `ANewPressInvokesItsActionOneTime` pushes `B` two times, reads the return values 1 and 0, and reads one call of the command of `Cancel`.
- The grep `Brutal\.GlfwApi|BrutalUi\.Glfw` over `*.cs` of Core and Framework finds only the existing doc line in `UiContext.cs` (line 802, `BrutalUi.Glfw.Tests`) and the generated `obj/` files. A grep of `src/BrutalUi.Framework/Actions/` for `lock`, `IDisposable`, `event`, `ToString(`, and `(object)` in code finds no match.

### 2026-10-29 Phase 3 group B (tasks 3.5 to 3.8)
- Task 3.5 (reflection probe in the scratchpad, package `Ahwoo.BRUTAL.Glfw` 2026.10.4): `Brutal.GlfwApi.GlfwGamepadState` is a public struct of 40 bytes with sequential layout and two public fixed buffers. `Buttons` is `fixed byte[15]` (element type `byte`) and `Axes` is `fixed float[6]`. `Glfw.GetGamepadState(int gamepadId, out GlfwGamepadState state)` returns `bool` and uses `out`. The enum `GlfwButtonAction` has `Release = 0` and `Press = 1` (underlying `int`). The constants are `Glfw.Constants.GLFW_GAMEPAD_BUTTON_A` (0), `_B`, `_X`, `_Y`, `_LEFT_BUMPER` (4), `_RIGHT_BUMPER`, `_BACK`, `_START`, `_GUIDE` (8), `_LEFT_THUMB`, `_RIGHT_THUMB`, `_DPAD_UP` (11), `_DPAD_RIGHT`, `_DPAD_DOWN`, `_DPAD_LEFT` (14), and `_LAST` (14). The aliases `_CROSS`, `_CIRCLE`, `_SQUARE`, and `_TRIANGLE` exist. `GLFW_JOYSTICK_1` (0) to `GLFW_JOYSTICK_16` (15) and `GLFW_JOYSTICK_LAST` (15) exist. An enum `GlfwGamepadButton` with the same indices also exists. The state is span-like (a fixed buffer that reads as a `ReadOnlySpan<byte>` with no allocation), so no escalation is needed.
- Task 3.6 edited `src/BrutalUi.Glfw/GlfwUiInput.cs` only: two private constants (`GamepadIdLast` 15, `GamepadButtonCount` 15), the public `PollGamepad(int gamepadId = 0)`, the internal static `GamepadButtons(ReadOnlySpan<byte>)`, and one remarks paragraph. No `.csproj` and no `Directory.Packages.props` changed. `BrutalUi.Glfw` still references Core only.
- Decision: the metadata fixed buffer needs an `unsafe` context (error CS0214 in a safe context, so the C# 12 safe conversion does not apply to it). `PollGamepad` therefore reads it in one `unsafe` block: `new ReadOnlySpan<byte>(state.Buttons, 15)`. The local `state` is a fixed variable, so the code needs no `fixed` pin. The project already sets `AllowUnsafeBlocks`.
- Decision (the order of the checks): `PollGamepad` checks the id first, then `_disposed`, then calls GLFW. An id outside 0 to 15 throws also on a disposed adapter (an argument error is never hidden). A disposed adapter with a valid id returns `None` before any GLFW call. Both seam tests thus need no window and no GLFW start.
- Decision: `GamepadButtons` reads at most the first 15 values of the span (a longer span is cut, a shorter span gives only the bits it holds). It compares each value with `(byte)GlfwButtonAction.Press` (1), so a value other than 1 gives no bit.
- Decision: `TheBitsFollowTheGlfwButtonIndices` checks the 15 members against the `Glfw.Constants.GLFW_GAMEPAD_BUTTON_*` values (not the `GlfwGamepadButton` enum), and also checks that `GLFW_GAMEPAD_BUTTON_LAST + 1` is 15 and that the enum has 16 members (15 buttons and `None`). `ALivePollAllocatesNothing` builds its adapter with the test seam (no window), as GLFW joystick state needs none.
- Finding (the Risk of the brief): the live poll allocates 0 bytes. `Glfw.GetGamepadState` in the binding allocates nothing over 100 polls after 2 warm-up polls, under `xvfb-run`. No workaround is needed.
- Task 3.8 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2706, failed 0, succeeded 2703, skipped 3 (2700 + 6; the new skip is `ALivePollAllocatesNothing`, no display). `BrutalUi.Glfw.Tests` gives 27 (21 + 6): with no display 24 passed and 3 skipped, and under `xvfb-run -a` 27 passed and 0 skipped. `GlfwUiInputGamepadTests` gives total 6: with no display 5 passed and 1 skipped, and under `xvfb-run -a` 6 passed and 0 skipped. Each existing test passes with no edit.
- The demo: `xvfb-run -a dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputGamepadTests'` gives total 6, failed 0, succeeded 6. The first test `EachPressedButtonGivesItsBit` presses each of the 15 button values in turn and reads the bit `1 << i`.
- The grep `Brutal\.GlfwApi|BrutalUi\.Glfw` over `*.cs` of Core and Framework (outside `bin/` and `obj/`) finds only the existing doc line `UiContext.cs:802`. `git status` shows no change in `BrutalUi.Glfw.csproj`, `BrutalUi.Framework.csproj`, or `Directory.Packages.props`.

### 2026-10-29 12:50 (phase 3 close)
- The phase 3 verifier returns PASS: build 0 warnings and 0 errors, solution 2706 (2703 passed, 3 skipped with no display) with 0 failed, `GamepadActionTests` 7, `UiActionMapTests` 11, `GlfwUiInputGamepadTests` 6 under `xvfb-run -a` (5 passed and 1 skipped with no display), the GLFW grep and the Q4 grep clean, and no project file changed. Demo: PASS.
- The skipped count of the solution with no display is now 3: the 2 old GLFW tests and `ALivePollAllocatesNothing`.

### 2026-10-29 Phase 4 (tasks 4.1 to 4.6)
- Tasks 4.1 to 4.5 edited `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs` (docs only), `src/BrutalUi.Core/Tree/UiContext.cs` (case 2 of the slot block and doc lines), `src/BrutalUi.Framework/Actions/UiActionMap.cs`, `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs` (3 tests), and `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs` (4 tests). No other file of `src/` changed, and no `.csproj` changed.
- The real names of the pointer payload are the names of the plan: `entry.Payload.Pointer.Button` and `entry.Payload.Pointer.Modifiers`. The drain read confirms the target rule. For a routed press, `target` is the return value of `RoutePointer`: the hit element or `null`. The capture branch of `RoutePointer` applies to a move only, so a press never gets the capture element as its target. A press with a target from the caller has `routed` `false`, so the outer `if` skips it. A press that hit an element outside the roots cannot occur, because the hit test walks the roots only. The code diff of `UiContext.cs` against the base holds the property, `tabMove`, and the slot block with its two cases. Each other line is a doc line or a comment.
- Decision: case 2 is an `else if` right after the closing brace of case 1. Its comment sits inside the branch, so no blank line splits the `if` and the `else if`.
- Decision (past the brief): task 4.2 also extends the remarks of `UiContext.OnUnhandledInput` with one sentence on the press. Without it, the remarks name the key down only and are wrong after phase 4.
- Decision (past the brief): the `code` doc of the `UiActionMap.HandleInput` slot handler, the `Code` doc of `InputBinding`, and the first paragraph of the class remarks (a binding maps a key, a mouse button, or a gamepad button) now name the mouse path. The class remarks hold a new paragraph "The mouse rule" with the layout-root rule.
- Decision: `BindMouseButton` checks only that the action exists, as the brief states. It stores `(int)button`, so a value outside the enum (for example `(PointerButton)7`) binds. Phase 6 adds the code check (a mouse button of -1 fails).
- Decision: `PressMouseButton` does not check that a press hit no element. It is the raw resolve, as `PressKey` is. The remarks state it.
- Decision: the 3 Core tests hold extra checks. `APressOverNoElementCallsTheSlotWithTheButton` starts with the focus on `Left` and checks that the slot reads no focus owner (the click focus rule runs first), and that `RoutingGate.PointerOverUi` is `false`. `APressOnAnElementDoesNotCallTheSlot` covers a press on `Left` with a handler that does not stop the walk, a press on the pickable root with no handler, and a press with a target from the caller at a point over no element. `AReleaseAMoveAndAScrollOverNoElementDoNotCallTheSlot` pushes a release with no earlier press.
- Decision: the 4 Framework tests hold extra checks. `AMouseButtonOverAnElementInvokesNothing` also makes the root pickable and checks that a press outside the panel invokes nothing (the layout-root rule). `PressMouseButtonMatchesTheButtonAndTheModifiers` binds the right button with Control and checks the left and the middle button, no modifier, Control+Shift, and Control+Caps Lock. `AKeyAndAMouseButtonWithTheSameCodeStayApart` checks the raw resolve and the slot path. The file has a private `Pointer` helper with an optional button and optional modifiers, and a private `CreatePanel` helper.
- Task 4.6 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2713, failed 0, succeeded 2710, skipped 3 (2706 + 7, no display). `BrutalUi.Core.Tests` gives 1745 (1742 + 3), all passed. `BrutalUi.Framework.Tests` gives 120 (116 + 4), all passed. `BrutalUi.Glfw.Tests` gives 27 (24 passed, 3 skipped: no display). `UnhandledInputTests` gives total 12, failed 0. `UiActionMapTests` gives total 15, failed 0. Each existing test passes with no edit.
- The demo: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` gives total 15, failed 0. The test `AMouseButtonOverNoElementInvokesItsAction` presses the right button at (300, 200), outside the panel, and reads one call of the command of `Cancel`.
- A grep of `UiActionMap.cs` for `(object)` and `.Equals(` finds no match. The press call casts the button enum to `int`, which boxes nothing.
- Follow-up for phase 7 (the phase 7 brief carries it): the gate `AMouseButtonOverNoElementAllocatesNothing` must use a root that is not pickable, as the demo does. A pickable root takes the press, and the gate then measures no map call.

### 2026-10-29 Phase 5 (tasks 5.1 to 5.7)
- Tasks 5.1 to 5.4 edited `samples/HelloPanel/HelloPanel.csproj` (one `ProjectReference` to `BrutalUi.Framework`), `HelloScreen.cs`, and `Program.cs`, and wrote `samples/HelloPanel/HelloState.cs`. No file of `src/`, no test file, no sheet, and no script changed. The two plants restored their files (`cmp` clean).
- Decision: `HelloScreen.Build` takes the `HelloState` as its second parameter, after `checker`. It throws `ArgumentNullException` for `null`. `RunFrames` and `PushSmokeInput` take the state too.
- Decision: the store starts at version 1 (`Store<T>` constructor), so `Versions` is `checked((int)(Score.Version - 1))`. The 120-frame run gives versions 149 = adds 119 + resets 30. Each add changes the score. Each reset changes it too, because an add in the same frame comes before the Escape.
- Decision: `HelloState` has the score selector in a `static readonly Func<int, int>` field, so the bind allocates no lambda per call. The score label is not pickable, as the other labels of the sample. `Adds` and `Resets` are auto properties with a private setter. The static handlers write them.
- Decision: the Escape key is pushed after the Tab and after the Add click. The text field holds the focus then. The run proves live that a focused text field passes a non-text key to the map (`resets` 30 in 120 frames: the frames with `frameIndex % 4 == 3`).
- Task 5.5 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2713, failed 0, succeeded 2710, skipped 3 (no change: the sample has no tests). `scripts/test-linux.sh; echo "exit=$?"` gives `test-linux: PASS build, test, gpu, smoke (120 frames) in 17 s` and `exit=0`.
- The demo `xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"` gives `exit=0` and these six lines:
  1. `HelloPanel: 120 frames rendered.`
  2. `HelloPanel: last frame: draw commands 7, vertices 1172.`
  3. `HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.`
  4. `HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.`
  5. `HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118, edits 182, submits 121.`
  6. `HelloPanel: state: adds 119, resets 30, versions 149.`
- Line 2 is new in its values: the state row adds draw data. I did not record the old values of line 2 before the change.
- Task 5.6, plant A (the Escape push removed from `PushSmokeInput`): exit code 1. Line 6 reads `adds 119, resets 0, versions 119`. Standard error holds `HelloPanel: the synthetic input did not reach the state row.` The widget check and the input check passed (the widget line 5 kept its counts). `Program.cs` restored from the scratchpad copy, `cmp` clean.
- Task 5.6, plant B (`GC.KeepAlive(new object());` as the first line of `UiActionMap.PressKey`): exit code 1. Line 3 reads `Update and Draw 720` (30 resets of 24 bytes in the measured frames). Standard error holds `HelloPanel: Update and Draw allocated 720 bytes after warm-up. The limit is 0 bytes.` `UiActionMap.cs` restored from the scratchpad copy, `cmp` clean. After both restores, the demo gives the six lines above and `exit=0`.
- Task 5.7: the command line contract in `docs/features/dev-ops/solution-skeleton/context.md` (line 138, "Standard output on success") lists one line only: `HelloPanel: <count> frames rendered.` It was already out of date at the base (five lines). Line 6 is new. That file stays unchanged (closed feature). Phase 7 writes line 6 into section 14 of the technical design.
- The Tab order of the sample now ends with the `Add` button (panel, `Click me`, toggle, slider, text field, `Add`).
- A real gamepad is not tested: `PollGamepad` returns `None` with no device, so `PushGamepad(None)` runs each frame and invokes nothing. The item stays in "Needs Human Verification".

### 2026-10-29 13:10 (phase 4 close)
- The phase 4 verifier returns PASS: build 0 warnings and 0 errors, solution 2713 (2710 passed, 3 skipped with no display) with 0 failed, `UnhandledInputTests` 12, `UiActionMapTests` 15, the Q4 grep clean, and no project file changed. The diff of `UiContext.cs` holds the property, `tabMove`, one slot block with the key case and the press case, and doc lines. Demo: PASS.

### 2026-10-29 Phase 6 (tasks 6.1 to 6.3)
- Tasks 6.1 and 6.2 edited `src/BrutalUi.Framework/Actions/UiActionMap.cs` and `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs` (8 tests). No file of `src/BrutalUi.Core/`, no `.csproj`, and no other test file changed.
- Decision: the text guard is the private method `IsTypedByTextField(key, modifiers)`, called by `HandleInput` for a key down. It tests `_context?.FocusedElement is not TextField` first, then Control, Alt, and Super, then the key ranges (`key < 256` or 320 to 336). The three numbers are private constants with the GLFW names in their comments. `PressKey` has no guard.
- Decision: `AddAction` also rejects an empty command name (`ArgumentException`, parameter `command`). The brief names the empty action name only. An empty command name never matches a command (`CommandRegistry.Register` rejects an empty name), so the action would be dead.
- Decision: the order of the checks in a bind is the code (key below 0, button below 0, or a gamepad value that is not one bit), then the action, then the duplicate. Each check comes before the array growth, so a rejected call changes no count. The duplicate test reuses the private `Find` with the stored modifiers (the lock bits drop), so Escape with Caps Lock is a duplicate of Escape. `Find` allocates nothing and boxes nothing.
- Decision: the gamepad code check is `BitOperations.PopCount((uint)button) != 1`. It rejects `None` and a multi-bit value with one message. It does not reject a bit above bit 14.
- Finding: the re-entry rule was complete from phases 1 to 4. `Find`, `PushGamepad`, and `IndexOfAction` copy the array and the count to locals, and `Invoke` copies the command name before the call. Phase 6 adds a doc sentence to `Invoke` only. `PressKey` reads `_bindings[index].Action` after `Find` with no code between, so the field read is safe.
- Finding: a throw in a command during `Update` leaves the drain with the later events in the queue. The next `Update` runs them (the Core test `UnhandledInputTests` shows the same). The test `ACommandThatThrowsLeavesTheMapUsable` reads this: the Enter key after the failed Escape key calls its action in the second `Update`.
- Decision: the Phase 1 follow-up (the `null` registry and the `null` context) sits in `AddActionRejectsAnEmptyOrDuplicateName` with no new test. The class count is 23.
- Decision: the Phase 3 gap is closed. `BindGamepadButton` rejects `None` and `A | B`.
- Decision: the test `TheTextGuardPassesANonTextKeyAndAShortcut` also checks that Shift+W (bound with Shift) calls nothing with the field in focus, and that the field keeps the focus. `TheTextGuardSkipsAKeypadKey` checks the edges: 320, 321, and 336 skip, and 319 and 337 pass. `ABindDuringAPressTakesEffectAtTheNextPress` fills 8 bindings first, so the bind in the command grows the array in the middle of the press.
- Task 6.3 results: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2721, failed 0, succeeded 2718, skipped 3 (2713 + 8, no display). `BrutalUi.Framework.Tests` gives 128 (120 + 8), failed 0. `UiActionMapTests` gives total 23, failed 0. Each existing test passes with no edit.
- The demo: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` gives total 23, failed 0. The test `TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus` pushes W (87) and the char `w` with a text field in focus, and reads 0 calls and the field text `w`. With the focus on a button, W calls one time.
- The `throw` grep `grep -nE 'throw ' src/BrutalUi.Framework/Actions/UiActionMap.cs` finds one remark line (line 63) and 12 `throw` statements: the constructor (119), `AddAction` (152, 157, 162), `BindKey` (193, 199, 204), `BindMouseButton` (243, 249, 254), and `BindGamepadButton` (291, 297, 302). `Activate` throws through `ArgumentNullException.ThrowIfNull`. `PressKey`, `PressMouseButton`, `PushGamepad`, `InvokeAction`, and `HandleInput` hold none.
- A grep of `src/BrutalUi.Framework/Actions/` for `lock`, `IDisposable`, `event`, `ToString(`, `(object)`, and `.Equals(` in code finds no match.
- The smoke run `xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120` gives `exit=0` and line 6 `HelloPanel: state: adds 119, resets 30, versions 149.`

### 2026-10-29 13:30 (phase 5 close)
- The phase 5 verifier returns PASS: build 0 warnings and 0 errors, solution 2713 (2710 passed, 3 skipped with no display) with 0 failed, `scripts/test-linux.sh` PASS with `exit=0`, the one project reference in `HelloPanel.csproj`, `PollGamepad` outside the measured UI region, and `PushGamepad` as its first line. Line 6 of the verifier run: `HelloPanel: state: adds 119, resets 30, versions 149.` Demo: PASS.

### 2026-10-29 Phase 7 group 1 (tasks 7.1 to 7.3)
- Task 7.1 wrote `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` (5 tests). No file of `src/`, `samples/`, or `scripts/` and no `.csproj` changed after the plants.
- Decision (past the brief): the key body runs an `Update` between the two groups. Events have no target, so they route to the focus owner at the drain. With one `Update` at the end, the button would never get a key. Thus the first group (button focused: Escape, Control+S, F12, repeat of Escape) drains with one `Update`, and the second group (field focused: W, Escape) drains with the final `Update` and `Draw`.
- Decision: the scene binds 12 keys in total (Escape, Control+S, W, Up, Down, F1, Shift+F2, Control+F3, F4, Control+Shift+F5, F6, F7), 3 mouse buttons (Right to `act-10`, Middle to `act-11`, Left to `act-12`), and 4 gamepad buttons (`A` to `act-13`, `B` to `act-14`, `DpadUp` and `DpadDown` to the focus actions). `act-15` has no binding. The gamepad body calls `InvokeAction` on it. The 16 actions `act-00` to `act-15` map to `cmd-00` to `cmd-15`, and `NavigateUp` and `NavigateDown` map to the two focus commands (18 actions).
- Decision: the real-work test sets the focus on the first button before the gamepad body and before the focus body, so the start is fixed. The expected owner is the second button after both bodies. After the key body the owner is the text field.
- Result (task 7.2): `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. The class gives total 5, failed 0 on the first run (0 bytes in all four gates). `dotnet test --solution brutal-ui.sln` gives total 2726, failed 0, succeeded 2723, skipped 3 (2721 + 5). `BrutalUi.Framework.Tests` gives 133 (128 + 5), all passed. No box was found in the path.
- Task 7.3 plants (each: `dotnet test --solution brutal-ui.sln`, exit code, failed tests, bytes; each file restored from the scratchpad copy, `cmp` clean):
  1. `GC.KeepAlive(new object());` first line of `UiActionMap.PressKey`: exit 2. Failed: `AnUnhandledKeyAllocatesNothing` (9600 bytes = 400 calls x 24) and `TheFocusActionsAllocateNothing` (7200 bytes = 300 calls x 24).
  2. The same line, first line of `UiActionMap.PushGamepad`: exit 2. Failed: `AGamepadPushAllocatesNothing` (4800 bytes = 200 calls x 24).
  3. The same line, first line inside the outer `if` of the slot block of `UiContext.DrainEvents`: exit 2. Failed: `AMouseButtonOverNoElementAllocatesNothing` (7200), `TheFocusActionsAllocateNothing` (7200), `AnUnhandledKeyAllocatesNothing` (14400). `AGamepadPushAllocatesNothing` passes, as the gamepad path skips the slot.
  4. Empty body of `KeyStep`: exit 2. Failed: `TheMeasuredBodyDoesRealWork` only (Expected 2, Actual 0 for the Escape command). The key gate still measures 0 bytes with an empty body, as the brief states.
- After the restores, the build gives 0 warnings and 0 errors, and `dotnet test --solution brutal-ui.sln` gives total 2726, failed 0. The next task is group 2 (task 7.4, the technical design section 14).

### 2026-10-29 Phase 7 group 2 (tasks 7.4 to 7.7)
- Tasks 7.4 to 7.6 edited `docs/technical-design.md` and `CHANGELOG.md` only. No file of `src/`, `tests/`, `samples/`, or `scripts/` changed. `docs/features/dev-ops/solution-skeleton/context.md` stays unchanged.
- Task 7.4: section 3 reads `Phase 3: one store, one command, one action`. Section 4 step 2 gets one sentence on `OnUnhandledInput` and the gamepad push. Section 9 gets Flow item 10 (the slot and its rules) and a new "Action map" text with the rules from the code: the actions, the bindings, the modifier rule, the repeat rule, the text guard, the mouse rule, the gamepad edge, `PollGamepad`, the one-map rule, `FocusCommands`, the build rules, the error rule, and the cost. Section 10 gets the row `UiActionMap` in the Components table.
- Task 7.5: the last gate bullet was "A sixteenth allocation gate", so the ordinal check passed. Section 14 gets the bullet "Action map tests" (counts 12, 23, 5, 7, 6) and the bullet "A seventeenth allocation gate" (the scene, the 4 test bodies, and the 4 plants of task 7.3). The bullet of the sample projects now says 6 lines, line 2 as `draw commands 7, vertices 1172` (was 1100), line 6, the new push order (the `Add` click and the Escape), the state check with its exit code and message, `versions` 149 = `adds` 119 + `resets` 30, and the two plants of task 5.6.
- Decision (past the brief): the old value of line 2 comes from the technical design (`vertices 1100`), because phase 5 did not record it. The text states the vertex change as 72 vertices and the draw commands as unchanged (7), from the old text and the smoke run.
- Decision (past the brief): the text of item 10 and of "Action map" follows the code where the plan was less exact. The slot gets a key repeat too (the map skips it, `ARepeatCallsTheSlotWithTheRepeatFlag`). `AddAction` also rejects an empty command name (phase 6). The constructor and `Activate` throw `ArgumentNullException`.
- Task 7.6: one line under `## [Unreleased]`, `### Added`, after the line of commands-and-events.
- Task 7.7: `scripts/test-linux.sh; echo "exit=$?"` gives `test-linux: PASS build, test, gpu, smoke (120 frames) in 17 s` and `exit=0`. `git status` shows no change in `scripts/`.
- Lint scores (`ste-lint.py --fail-over 2.5`, per 100 words), before then after my edits: `docs/technical-design.md` 1.52 then 1.51, `CHANGELOG.md` 1.67 then 1.66, `context.md` 1.83 then 1.83 (measured before this note), `implementation.md` 1.98 then 1.98 (not edited). All four files exit with 0, so no base file failed and no prose needed a fix. Final run with this note: `context.md` 1.85, and the other three files as above. All exit with 0.
- Counts for the record: the solution has 2726 tests (2723 passed, 3 skipped with no display), from the last record of group 1.

### 2026-10-29 13:50 (phase 6 close)
- The phase 6 verifier returns PASS: build 0 warnings and 0 errors, solution 2721 (2718 passed, 3 skipped with no display) with 0 failed, `UiActionMapTests` 23, the HelloPanel smoke run `exit=0` with line 6 `adds 119, resets 30, versions 149`, each `throw` of `UiActionMap.cs` in the constructor, `AddAction`, or a bind method, and the Q4 grep clean. Demo: PASS.

### 2026-10-29 14:20 (phase 7 close)
- The phase 7 verifier returns PASS: build 0 warnings and 0 errors, solution 2726 (2723 passed, 3 skipped with no display) with 0 failed, `ActionMapAllocationTests` 5 in Debug, `scripts/test-linux.sh` PASS with `exit=0`, the six expected changed files in `src/` and `samples/`, the one project reference, no changed test file of the base, the gate bullets 16 and 17 in section 14, and the lint exit 0 on the four docs (1.51, 1.66, 1.85, 1.98). Demo: PASS.
- Each phase of `tasks.md` is done (45/45). The cross-phase review comes next.

### 2026-10-29 Review group 1 (CR-1, CR-2, CR-3)
- CR-1: `Activate` clears the slot of the old context when the old context is another object and its slot holds the handler of this map. The test uses `ReferenceEquals` for the contexts and `==` for the delegates. A map is active on one context at most. The remarks of `Activate` and of the class state the rule. The step is in `ASecondActivateReplacesTheFirstMap`. Plant: the old `Activate` fails this test at the first `Assert.Null`.
- CR-2: `ABindDuringAPressTakesEffectAtTheNextPress` gets a gamepad step. It needs a second handler, `s_bindPadLater`, because `s_bindLater` binds a key. The step uses the action `NavigateUp` for the bind of A. Plant A (the scan reads the fields) fails the test with 2 calls in place of 1.
- CR-3: `TheTextGuardPassesANonTextKeyAndAShortcut` binds S with Alt and S with Super. Plant B (Control only) fails the test with 4 calls in place of 5.
- Each plant file is restored with `cp` and equals its copy (`cmp`).

### 2026-10-29 Review group 2 (CR-4)
- CR-4: `AKeyUpAndACharDoNotCallTheSlot` (renamed) pushes a key up and a char (`PointerTree.Char('a')`) with no focus owner, then again with the focus on `Left`. It asserts a slot count of 0 after each `Update`. The count of `UnhandledInputTests` stays at 12.
- Plant C (`Char` added to the key case of the slot block in `UiContext.cs`) fails the test at the first char push, with no focus owner. `UiContext.cs` is restored with `cp` and equals its copy (`cmp`).
- Decision: the plant fails at the no-focus char only. The char with the focus on `Left` has no `OnChar` handler on `Left`, so it also reaches the slot under the plant, but the first assert stops the test before that step.

### 2026-10-29 Review group 3 (CR-5)
- CR-5: `TheBitsFollowTheGlfwButtonIndices` reads the `FixedBufferAttribute` of the field `GlfwGamepadState.Buttons` with reflection. It asserts the length 15 and the element type `byte`. `ALivePollAllocatesNothing` finds the first id from 0 to 15 that `Glfw.JoystickPresent` reports as absent, after the measure. It asserts that `PollGamepad` gives `UiGamepadButtons.None` for that id. When all 16 ids are present, the assert is skipped with a comment. The skip on a failed `Glfw.Init` stays.
- The count of `GlfwUiInputGamepadTests` stays at 6. The solution has 2726 tests (2723 passed, 3 skipped with no display). The class gives 5 passed and 1 skipped with no display, and 6 passed under `xvfb-run -a`.
- Decision: the absent-id assert runs only when a display exists, because the test skips on a failed `Glfw.Init`. I did not run a plant for this finding, because the brief asks for none.

### 2026-10-29 Review group 4 (CR-6, CR-1 doc half)
- CR-6: The "Sample projects" bullet of section 14 now lists the gamepad push first in the UI region. A new sentence says the gamepad poll runs after `Glfw.PollEvents` and outside the UI region. `RunFrames` in `samples/HelloPanel/Program.cs` confirms both: `PollGamepad` runs before the frame region, and `state.Map.PushGamepad(pad)` is the first line of the UI region.
- CR-1 doc half: The one-map rule in section 9 now says a map is active on one context at most and the last `Activate` wins. `Activate` on a new context first clears the slot of the old context when the slot holds the handler of the map. `UiActionMap.Activate` confirms it.
- Lint: `ste-lint.py --fail-over 2.5 docs/technical-design.md` exits 0. The score is 1.52 (1.51 before).
- No decision past the brief. No open task.

### 2026-10-29 15:10 (the cross-phase review)
- `/review-feature` wrote `code-review.md`: 6 findings (1 MEDIUM, 5 LOW). CR-1 is a real fault: an `Activate` on a second context left the slot of the first context, so a typed letter in a text field of the first context fired an action. CR-2 to CR-5 are test gaps, and CR-6 is a doc gap.
- Review group 1 fixed CR-1 (follow-up question 1, option A: the last `Activate` wins), CR-2, and CR-3. Review group 2 fixed CR-4. Review group 3 fixed CR-5. Review group 4 fixed CR-6 and wrote the CR-1 rule into section 9. Each fix extends an existing test, so each class count stays as in the plan.
- The review verifier returns PASS: build 0 warnings and 0 errors, solution 2726 (2723 passed, 3 skipped with no display) with 0 failed, `scripts/test-linux.sh` PASS, and the lint of the technical design at 1.52.
- Follow-up questions 2, 3, and 4 of `code-review.md` stay open. Questions 2 and 3 belong to panels-and-modes and xml-markup. Question 4 (reject a gamepad bit above bit 14) is a small scope choice for the user.

### 2026-10-29 15:20 (/update-feature)
- Each task of `tasks.md` is done (45/45). The status of `implementation.md` and of this file is Ready for Review.
- The inbox holds no line. No bug is promoted, and no note or change line stays open.
- The solution holds 2726 tests (2723 passed, 3 skipped with no display, 0 failed): `BrutalUi.Core.Tests` 1745 (+12), `BrutalUi.Framework.Tests` 133 (+40), and `BrutalUi.Glfw.Tests` 27 (+6), as Q2 of the Definition of Done asks.
