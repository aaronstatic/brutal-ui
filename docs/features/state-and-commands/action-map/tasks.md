# state-and-commands/action-map - Task Checklist

**Last Updated:** 2026-10-29 15:20
**Progress:** 45/45 tasks complete (100%)
**Advanced phases:** Phase 1 and Phase 4 are ADVANCED (opus): they change the drain of the input pass in `UiContext`. Phases 2, 3, 5, 6, and 7 are STANDARD (sonnet), with an escalation rule on phases 3, 5, and 7.

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (The Seam, Public API, Internal Shape, Data Flow, The Frame Order) and "Key Technical Decisions" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines, also for each namespace that a `cref` names. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces and the flat namespaces `BrutalUi.Core`, `BrutalUi.Framework`, `BrutalUi.Glfw`, and the matching `.Tests` namespaces. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/<Project>/<Project>.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. The Framework tests cannot use the internal event factories of `BrutalUi.Core.Tests`. Copy the private `Pointer`, `Key`, `CreateContext`, and `AddRoot` helpers of `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` into each Framework test file that needs them. `UiContext.PushEvent(in ...)` needs a variable, not a property. A `static readonly` handler field that reads a later `static readonly` field in its lambda gives CS8604, so declare the later field first. Write no `(object)` cast and no `Equals` call on a value in the resolve path.

**No-change rule:** these files do not change: each other file of `src/BrutalUi.Core/`, each file of `src/BrutalUi.Framework/State/`, `Binding/`, `Commands/`, `Events/`, and `Services/`, each `.csproj` other than `samples/HelloPanel/HelloPanel.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/HelloPanel/HelloScreen.css`, `samples/HelloPanel/HelloWidgets.cs`, and each existing test file.

---

## Phase 1: The Key Path (9/9 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 8 with `failed: 0`. The first test pushes an Escape key down with no focus owner, runs `Update`, and reads one call of the command of `Cancel`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

Group A: the Core slot (tasks 1.1 to 1.5).

- [x] **1.1 Record the base and the counts in context.md**
  - Description: In `docs/features/state-and-commands/action-map/context.md`, section "Base and Counts", check that the commands-and-events commit exists (`git log --oneline -3`). Record the test counts of the solution, of `BrutalUi.Core.Tests`, of `BrutalUi.Framework.Tests`, and of `BrutalUi.Glfw.Tests` before any code change (expected 2668 with 2 skipped, 1733, and 93. Read the Glfw count from a run of that project). Read each project count from a run of that project. The base commit, the Seam, the Internal Shape, and the Frame Order are already in the file.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

- [x] **1.2 Write UiUnhandledInputHandler.cs**
  - Description: Write `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`: `public delegate void UiUnhandledInputHandler(UiEventType type, int code, UiModifiers modifiers, bool isRepeat);`. The summary: a handler for an input that no element took. `UiContext` calls it from the drain. The parameter docs: `type` is `UiEventType.KeyDown` (phase 4 adds `PointerDown`). `code` is the GLFW key code of a key down. `isRepeat` is `true` for a key repeat. The remarks state the rules of the Internal Shape ("The drain call" and "The slot rules"), the A6 reason (the action map reads its input here, and Core names no Framework type), and the build-time rule (create the delegate one time, and set the slot at build time).
  - File(s): `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`
  - Estimate: 4 steps

- [x] **1.3 Add OnUnhandledInput and the key call of the drain to UiContext.cs**
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`. Add `public UiUnhandledInputHandler? OnUnhandledInput { get; set; }` after `OnContentScale`, with remarks in the shape of `OnFileDrop`: one slot, set at build time, a `null` slot does nothing, `UiActionMap.Activate` writes it. Add the key case of "The drain call" of the Internal Shape to `DrainEvents`: the local `tabMove`, then the slot block after the Tab branch. Extend the remarks of `DrainEvents` with a paragraph "The unhandled input rule (A6 of state-and-commands)", and add one sentence to the Tab rule paragraph of `PushEvent(in KeyEvent)`: a key down that no handler stopped and that is not a Tab move goes to `OnUnhandledInput` after its walk. Change no other line of the drain.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 8 steps

- [x] **1.4 Write UnhandledInputTests.cs (9 tests)**
  - Description: Write `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs` with 9 tests. Use `PointerTree` or the helpers of `TabNavigationTests` for the tree, and a private recorder target that stores each call (type, code, modifiers, repeat) in a fixed array. Tests: `AKeyDownWithNoFocusOwnerCallsTheSlot` (key 256 with Shift: one call with these values and `isRepeat` `false`), `AKeyDownThatNoHandlerStopsCallsTheSlotAfterTheWalk` (an owner with a `OnKeyDown` handler that records the order: the handler first, the slot second), `AKeyDownThatAHandlerStopsDoesNotCallTheSlot`, `AKeyUpDoesNotCallTheSlot`, `ATabMoveDoesNotCallTheSlotAndAControlTabDoes` (Tab and Shift+Tab move the focus and call nothing. Control+Tab calls the slot with key 258), `AKeyWithATargetFromTheCallerDoesNotCallTheSlot`, `ARepeatCallsTheSlotWithTheRepeatFlag`, `AFocusedTextFieldKeepsItsEditKeys` (with a focused `TextField`: Left, Backspace, and the main Enter call nothing. Escape (256) and Down (264) each call the slot one time), and `ASlotThatThrowsLeavesUpdateAndTheNextUpdateDrainsTheRest` (two key downs. The slot throws at the first. `Update` throws. The next `Update` calls the slot for the second key, and `QueuedEventCount` is 0).
  - File(s): `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs`
  - Estimate: 10 steps

- [x] **1.5 Run the gate of group A and record the results**
  - Description: Run the build, the tests, and `UnhandledInputTests` of `BrutalUi.Core.Tests` (total 9). Expect `failed: 0`, and each other test passes with no edit. Record the results in `context.md`.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

Group B: the map (tasks 1.6 to 1.9).

- [x] **1.6 Write UiActions.cs**
  - Description: Write `src/BrutalUi.Framework/Actions/UiActions.cs`: `public static class UiActions` with the four properties of the Public API (`Confirm`, `Cancel`, `NavigateUp`, `NavigateDown`), each `=> "<Name>"u8;`. The remarks: an action name is UTF-8 and compares by its bytes. A game can use other names. The names are the words of the requirement (Notes, Conflicts and Gaps item 3).
  - File(s): `src/BrutalUi.Framework/Actions/UiActions.cs`
  - Estimate: 3 steps

- [x] **1.7 Write UiActionMap.cs (the key path)**
  - Description: Write `src/BrutalUi.Framework/Actions/UiActionMap.cs` with the phase 1 members of the Public API and the Internal Shape: `CreateInfo`, the constructor (`ArgumentNullException` with the parameter name `info` and a constant message when `Commands` is `null`), `Commands`, `ActionCount`, `BindingCount`, `AddAction`, `BindKey`, `InvokeAction`, `PressKey`, `Activate`, the modifier rule, the resolve, and `HandleInput` with the repeat rule and the key branch. Phase 1 `AddAction` and `BindKey` check only that the action exists for a bind (`ArgumentException`). The class remarks state: an action maps a UTF-8 name to a command name. A binding maps a key with modifiers to an action. The map copies each name, and an add can grow an array, so add and bind at build time. A resolve scans the bindings in the order of the bind, allocates nothing, and throws nothing of its own. The map calls `CommandRegistry.Invoke`, so an unknown command calls `UnknownCommand` of the registry. An exception of a command leaves the call. `Activate` writes the slot `UiContext.OnUnhandledInput`, so one map is active on a context (A8). A repeat invokes nothing. The map runs on the UI thread and uses no lock.
  - File(s): `src/BrutalUi.Framework/Actions/UiActionMap.cs`
  - Estimate: 9 steps

- [x] **1.8 Write UiActionMapTests.cs (8 tests)**
  - Description: Write `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs` with 8 tests. Use a private sealed `Counter` target with an `int Calls` field and a static `CommandHandler` in a `static readonly` field that adds one to `((Counter)target!).Calls`. Use a context with a `RecordingRenderer` measurer and a viewport of 400 by 300. Tests: `AnUnhandledKeyInvokesTheCommandOfItsAction` (the demo: `Cancel` on Escape (256), `Activate`, a key down with no target and no focus owner, one `Update`. The counter reads 1), `PressKeyReturnsTrueForABoundKeyAndFalseForAnUnboundKey`, `TheModifiersMustMatch` (a bind of Control+S (83): S alone gives `false`, Control+S gives `true`, Control+Shift+S gives `false`), `CapsLockAndNumLockDoNotBlockAMatch`, `InvokeActionInvokesTheCommandOfTheAction` (also `false` for an unknown action), `AnActionWithAnUnknownCommandCallsTheUnknownCommandSlot` (`PressKey` gives `false`, and the slot of the registry gets the command name), `ARepeatInvokesNothing` (a key down with `IsRepeat` `true` through `Update`), and `AMapWithNoActivateInvokesNothing`.
  - File(s): `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`
  - Estimate: 9 steps

- [x] **1.9 Run the gate of phase 1 and record the results**
  - Description: Run the build, the tests, and `UiActionMapTests` (total 8). Expect `failed: 0`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

---

## Phase 2: The Focus Actions and One Active Map (5/5 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*FocusActionTests'` and reads a total of 5 with `failed: 0`. The first test binds Down (264) to `NavigateDown`, presses it one time with the focus on the first of three buttons, and reads the focus on the second button.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] **2.1 Write FocusCommands.cs**
  - Description: Write `src/BrutalUi.Framework/Actions/FocusCommands.cs`: `public static class FocusCommands` with `Next` (`"ui.focus-next"u8`), `Previous` (`"ui.focus-previous"u8`), `Register`, and the two static handlers of the Internal Shape (`static readonly CommandHandler` fields with `static` lambdas). `Register(registry, context)` checks both arguments with `ArgumentNullException.ThrowIfNull`, then registers both names with the context as the target. The remarks: the commands call `UiContext.FocusNext` and `UiContext.FocusPrevious` (the gamepad slots of Decision 8 of keyboard-focus), so their rules apply: the order is the paint order, the focus wraps, and with no owner the focus goes to the first or the last element. The registry holds the context until `Unregister` of both names. Map `NavigateDown` to `Next` and `NavigateUp` to `Previous`.
  - File(s): `src/BrutalUi.Framework/Actions/FocusCommands.cs`
  - Estimate: 5 steps

- [x] **2.2 Add IsActive and Deactivate to UiActionMap.cs**
  - Description: Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add `IsActive` and `Deactivate` (the Internal Shape: `IsActive` is `_context is { } c && c.OnUnhandledInput == _onInput`. `Deactivate` writes `null` into the slot when `IsActive`, then sets `_context` to `null`). Extend the remarks of `Activate`: the write replaces each delegate in the slot, also a delegate of the game. A mode list of maps waits for panels-and-modes (A8).
  - File(s): `src/BrutalUi.Framework/Actions/UiActionMap.cs`
  - Estimate: 4 steps

- [x] **2.3 Write FocusActionTests.cs (5 tests)**
  - Description: Write `tests/BrutalUi.Framework.Tests/Actions/FocusActionTests.cs` with 5 tests on a root with three focusable buttons. Tests: `NavigateDownMovesTheFocusToTheNextElement` (the demo), `NavigateUpMovesTheFocusToThePreviousElement` (Up (265)), `TheFocusActionsStartWithNoOwnerAndWrap` (with no owner, Down gives the first button. Down at the last button gives the first button), `RegisterAddsTwoCommandsWithTheContextAsTheTarget` (`Count` rises by 2, `Contains` of both names, and `Invoke(FocusCommands.Next)` moves the focus with no map), and `RegisterRejectsANullRegistryOrContext`.
  - File(s): `tests/BrutalUi.Framework.Tests/Actions/FocusActionTests.cs`
  - Estimate: 8 steps

- [x] **2.4 Add 3 one-map tests to UiActionMapTests.cs**
  - Description: Edit `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`: add 3 tests. `ASecondActivateReplacesTheFirstMap` (two maps on one context bind Escape to two counters. After the second `Activate`, an Escape adds 1 to the second counter only), `DeactivateClearsTheSlotOfItsOwnMapOnly` (`Deactivate` of the replaced map leaves the second map active), and `IsActiveFollowsTheSlot`.
  - File(s): `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`
  - Estimate: 5 steps

- [x] **2.5 Run the gate of phase 2 and record the results**
  - Description: Run the build, the tests, `FocusActionTests` (total 5), and `UiActionMapTests` (total 11). Expect `failed: 0`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

---

## Phase 3: The Gamepad Path (8/8 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*GamepadActionTests'` and reads a total of 7 with `failed: 0`. The first test pushes `UiGamepadButtons.B` two times and reads one call of the command of `Cancel` and the return values 1 and 0.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the shape of `GlfwGamepadState` does not fit the Internal Shape.

Group A: the map (tasks 3.1 to 3.4).

- [x] **3.1 Add UiGamepadButtons to UiEventEnums.cs**
  - Description: Edit `src/BrutalUi.Core/Input/UiEventEnums.cs`: add `UiGamepadButtons` of the Public API after `PointerButton`, with `[Flags]`. The summary of each member names its GLFW constant (for example `GLFW_GAMEPAD_BUTTON_A`, value 0). The remarks: bit `i` is the GLFW gamepad button `i`, so the adapter converts with no table. The names follow the Xbox layout of GLFW. Core reads no gamepad. The enum is the shared type of the Glfw adapter and the action map.
  - File(s): `src/BrutalUi.Core/Input/UiEventEnums.cs`
  - Estimate: 4 steps

- [x] **3.2 Add BindGamepadButton and PushGamepad to UiActionMap.cs**
  - Description: Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add `BindGamepadButton` and `PushGamepad` (the gamepad edge of the Internal Shape). Phase 3 `BindGamepadButton` checks only that the action exists. The remarks of `PushGamepad`: call it one time in each frame, after the poll and before `Update`. A press is a bit that is set now and was clear at the last push. A held button invokes nothing again. Two new presses in one push invoke in the order of the bind. The method works with no `Activate`. A new map starts with no button, so a button that is held at the first push counts as a press. The method allocates nothing.
  - File(s): `src/BrutalUi.Framework/Actions/UiActionMap.cs`
  - Estimate: 5 steps

- [x] **3.3 Write GamepadActionTests.cs (7 tests)**
  - Description: Write `tests/BrutalUi.Framework.Tests/Actions/GamepadActionTests.cs` with 7 tests. Tests: `ANewPressInvokesItsActionOneTime` (the demo), `AHeldButtonInvokesNothingAgain`, `AReleaseAndANewPressInvokeAgain` (`B`, `None`, `B` gives two calls), `TwoNewPressesInOnePushInvokeInTheOrderOfTheBind` (`A` and `B` in one push, with a target that records the order), `ADpadDownBindingMovesTheFocus` (with `FocusCommands` and three focusable buttons), `PushGamepadWorksWithNoActivate`, and `AnUnboundButtonInvokesNothing` (`X` gives 0).
  - File(s): `tests/BrutalUi.Framework.Tests/Actions/GamepadActionTests.cs`
  - Estimate: 8 steps

- [x] **3.4 Run the gate of group A and record the results**
  - Description: Run the build, the tests, and `GamepadActionTests` (total 7). Expect `failed: 0`. Record the results in `context.md`.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

Group B: the GLFW poll (tasks 3.5 to 3.8).

- [x] **3.5 Read the shape of GlfwGamepadState**
  - Description: Read the shape of `Brutal.GlfwApi.GlfwGamepadState` and of `Glfw.GetGamepadState` (`out` or `ref`) in the BRUTAL binding (package `Ahwoo.BRUTAL.Glfw`, the version of `Directory.Packages.props`). Use a reflection probe in the scratchpad, not in the repository. Record the button member, its element type, and the names of the GLFW gamepad constants (for example `Glfw.Constants.GLFW_GAMEPAD_BUTTON_A`, or a `GlfwGamepadButton` enum) in `context.md`. When the state holds no span-like button member, escalate (the agent tier line).
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 6 steps

- [x] **3.6 Add PollGamepad to GlfwUiInput.cs**
  - Description: Edit `src/BrutalUi.Glfw/GlfwUiInput.cs`: add `PollGamepad` and the internal static conversion of the Internal Shape. Name the conversion `GamepadButtons` and take the button values as a span of the element type of task 3.5. Return `UiGamepadButtons.None` after `Dispose`. Throw `ArgumentOutOfRangeException` for an id outside 0 to 15. A `false` result of `Glfw.GetGamepadState` gives `None`. Add a paragraph to the class remarks: the adapter reads one gamepad when the game calls `PollGamepad`, and it adds no handler for it. Call it one time in each frame, after `Glfw.PollEvents`. Give the result to `UiActionMap.PushGamepad`. The adapter reads no axis. The Guide button can be held by the system. GLFW joystick state is global, so the adapter needs no window for the poll.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 7 steps

- [x] **3.7 Write GlfwUiInputGamepadTests.cs (6 tests)**
  - Description: Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs` with 6 tests. Put the class in the collection `GlfwUiInputAllocationTests.CollectionName` (GLFW state is global). Tests: `EachPressedButtonGivesItsBit` (15 cases in one test: one pressed button gives one bit), `AReleasedButtonGivesNoBit`, `TheBitsFollowTheGlfwButtonIndices` (each member of `UiGamepadButtons` equals `1 << ` the GLFW constant of task 3.5, as the test of `UiContext.TabKey` checks the GLFW key enum), `PollGamepadRejectsAnIdOutsideTheRange` (-1 and 16, through the test seam with no window), `ADisposedAdapterPollsNoButton` (the test seam, no GLFW call), and `ALivePollAllocatesNothing` (start GLFW. On a failure of `Glfw.Init`, skip with the reason, as the second gate of `GlfwUiInputAllocationTests` does. Else poll 100 times after 2 warm-up polls and assert 0 bytes. Do not assert a button: the machine can have a gamepad).
  - File(s): `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs`
  - Estimate: 8 steps

- [x] **3.8 Run the gate of phase 3 and record the results**
  - Description: Run the build, the tests, and `GlfwUiInputGamepadTests` (total 6, with 1 skipped when no display exists). Run `xvfb-run -a dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputGamepadTests' 2>&1 | tail -n 5` and expect 6 passed. Record the results and the demo in `context.md`, with the new skipped count of the solution.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

---

## Phase 4: The Mouse Path (6/6 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 15 with `failed: 0`. The test `AMouseButtonOverNoElementInvokesItsAction` presses the right button outside the panel and reads one call of the command of `Cancel`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

- [x] **4.1 Extend the docs of UiUnhandledInputHandler.cs for a press**
  - Description: Edit `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`: extend the docs. `type` can be `UiEventType.PointerDown`, and `code` is then the GLFW mouse button value (`(int)PointerButton`). A press goes to the slot only when it hit no element: the routing gate then reads the pointer as a game pointer. A press on an element goes to the slot never, also when no handler stops it. The click focus rule runs first, so the press clears the focus before the slot call.
  - File(s): `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`
  - Estimate: 3 steps

- [x] **4.2 Add the press call of the drain to UiContext.cs**
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add case 2 of "The drain call" of the Internal Shape (the type is `UiEventType.PointerDown` and `target` is `null`: call `handler(UiEventType.PointerDown, (int)entry.Payload.Pointer.Button, entry.Payload.Pointer.Modifiers, false)`). Extend the paragraph "The unhandled input rule" of the `DrainEvents` remarks, and add one sentence to the click focus paragraph of `PushEvent(in PointerEvent)`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] **4.3 Add 3 press tests to UnhandledInputTests.cs**
  - Description: Edit `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs`: add 3 tests. `APressOverNoElementCallsTheSlotWithTheButton` (a root that is not pickable, a right press with Shift outside the child: one call with `PointerDown`, 1, Shift, and `false`), `APressOnAnElementDoesNotCallTheSlot` (also on a pickable element with no handler), and `AReleaseAMoveAndAScrollOverNoElementDoNotCallTheSlot`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs`
  - Estimate: 6 steps

- [x] **4.4 Add the mouse path to UiActionMap.cs**
  - Description: Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add `BindMouseButton`, `PressMouseButton`, and the `PointerDown` branch of `HandleInput`. The remarks: the map gets a press over no element only. A layout root that is pickable takes each press, so set `IsPickable` to `false` on a root that fills the window (the layout-root rule). A key binding and a mouse binding never match each other, also with the same code.
  - File(s): `src/BrutalUi.Framework/Actions/UiActionMap.cs`
  - Estimate: 5 steps

- [x] **4.5 Add 4 mouse tests to UiActionMapTests.cs**
  - Description: Edit `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`: add 4 tests. `AMouseButtonOverNoElementInvokesItsAction` (the demo: a root that is not pickable with a panel of 100 by 100, one `Update` for the layout, a right press and a release at (300, 200) with no target, one `Update`), `AMouseButtonOverAnElementInvokesNothing` (the same press inside the panel), `PressMouseButtonMatchesTheButtonAndTheModifiers`, and `AKeyAndAMouseButtonWithTheSameCodeStayApart` (a bind of key 1 and of mouse button 1 to two actions).
  - File(s): `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`
  - Estimate: 7 steps

- [x] **4.6 Run the gate of phase 4 and record the results**
  - Description: Run the build, the tests, `UnhandledInputTests` (total 12), and `UiActionMapTests` (total 15). Expect `failed: 0`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

---

## Phase 5: The HelloPanel Sample (7/7 complete)

**Demo:** a developer runs `xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"` and reads line 6, `HelloPanel: state: adds <A>, resets <R>, versions <V>.`, with three counts above 0 and `exit=0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the smoke run fails a check that the push order of task 5.4 does not explain.

- [x] **5.1 Add the Framework reference to HelloPanel.csproj**
  - Description: Edit `samples/HelloPanel/HelloPanel.csproj`: add `<ProjectReference Include="../../src/BrutalUi.Framework/BrutalUi.Framework.csproj" />`. Change no other line.
  - File(s): `samples/HelloPanel/HelloPanel.csproj`
  - Estimate: 2 steps

- [x] **5.2 Write HelloState.cs**
  - Description: Write `samples/HelloPanel/HelloState.cs`: `internal sealed class HelloState` with a constructor that takes the `UiContext`. It builds, one time: a `Store<int>` `Score` that starts at 0, a `CommandRegistry`, the commands `hello.add` (adds 1 to the score) and `hello.reset` (sets 0), each with a static handler in a `static readonly` field and the `HelloState` as the target, and each with a count field (`Adds`, `Resets`). It also builds `FocusCommands.Register(commands, context)`, and a `UiActionMap` with `Cancel` to `hello.reset` (Escape (256) and gamepad `B`), `NavigateUp` to `FocusCommands.Previous` (Up (265) and `DpadUp`), and `NavigateDown` to `FocusCommands.Next` (Down (264) and `DpadDown`). Use the GLFW key values of `GlfwKey` cast to `int`. The constructor also builds one row (class `hello-row`) with a button `Add` (bound with `BindCommand(commands, "hello.add"u8)`) and a label bound with `BindText(Score, static s => s)`. Expose `Row`, `AddButton`, `Map`, `Score`, `Adds`, and `Resets`. The remarks state the A7 demo, the build-time rule, and the label rule: the longest score text (`2147483647`, 10 bytes) fits the start buffer of a label (32 bytes), so the label never grows its buffer.
  - File(s): `samples/HelloPanel/HelloState.cs`
  - Estimate: 8 steps

- [x] **5.3 Add the state row to HelloScreen.cs**
  - Description: Edit `samples/HelloPanel/HelloScreen.cs`: `Build` takes the `HelloState` and adds `state.Row` as the last child of the panel, after the widget section. Update the class remarks: the panel holds the state row last. The `Add` button takes the focus, so the Tab order ends with it. The row uses the `hello-row` class, so the sheet needs no new rule.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 4 steps

- [x] **5.4 Add the poll, the push, the smoke input, and line 6 to Program.cs**
  - Description: Edit `samples/HelloPanel/Program.cs`. (1) Create the `HelloState` after the `UiContext`, pass it to `HelloScreen.Build`, and call `state.Map.Activate(ui)` before the frame loop. (2) In `RunFrames`, call `UiGamepadButtons pad = input.PollGamepad();` right after `Glfw.PollEvents()`, outside the UI region, and call `state.Map.PushGamepad(pad);` as the first line of the UI region, before the smoke input. (3) In `PushSmokeInput`, push a click on the `Add` button after the toggle click and before the slider click, so the slider press keeps the focus and the Tab still moves it from the slider to the text field. Push an Escape key down and key up with no target right after the Tab, in each frame with `frameIndex % 4 == 3`. (4) Add `Adds`, `Resets`, and `Versions` (`Score.Version - 1`) to `RunResult`. Print line 6 `HelloPanel: state: adds <A>, resets <R>, versions <V>.` after line 5. In the smoke check, after the widget check, write `HelloPanel: the synthetic input did not reach the state row.` to standard error and return `ExitFailure` when one of the three counts is 0. (5) Update the remarks of `Program`, `Run`, `RunFrames`, and `PushSmokeInput`: six lines, the poll and the push, the new push order, and the new check.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 10 steps

- [x] **5.5 Run the smoke run and record the six lines**
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Run the demo command and record the six lines in `context.md`. Line 2 changes, because the state row adds draw data. Record the new values.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

- [x] **5.6 Run the two plants of the sample**
  - Description: Copy each file to the scratchpad first. Plant A: remove the Escape push from `PushSmokeInput`. Expect exit code 1, `resets 0` in line 6, and the line `HelloPanel: the synthetic input did not reach the state row.` Plant B: put `GC.KeepAlive(new object());` as the first line of `UiActionMap.PressKey`. Expect exit code 1 and the allocation line. Restore each file and check it with `cmp`. Run the demo again and expect `exit=0`.
  - File(s): `samples/HelloPanel/Program.cs`, `src/BrutalUi.Framework/Actions/UiActionMap.cs` (both restored)
  - Estimate: 6 steps

- [x] **5.7 Record the command line contract and the plant results**
  - Description: Read the command line contract in `docs/features/dev-ops/solution-skeleton/context.md`. When it lists the output lines of HelloPanel, record in `context.md` that line 6 is new, and leave that file unchanged (it belongs to a closed feature). Phase 7 writes line 6 into section 14 of the technical design. Record the plant results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 3 steps

---

## Phase 6: The Text Guard and the Build Rules (3/3 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 23 with `failed: 0`. The test `TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus` presses W (87) with the field focused and reads no call of the bound action and the char in the field.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

- [x] **6.1 Add the text guard, the build rules, and the re-entry rule to UiActionMap.cs**
  - Description: Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add the text guard, the build rules, and the re-entry rule of the Internal Shape. Extend the remarks: the text guard and its key ranges, the reason (GLFW sends a key down and a char for a text key, and the field takes the char), the build rules, the re-entry rule, and the error rule (an exception of a command leaves `PressKey`, `PressMouseButton`, `PushGamepad`, or `Update`, and the map stays usable).
  - File(s): `src/BrutalUi.Framework/Actions/UiActionMap.cs`
  - Estimate: 8 steps

- [x] **6.2 Add 8 guard and rule tests to UiActionMapTests.cs**
  - Description: Edit `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`: add 8 tests. `TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus` (the demo: W bound to `Confirm`, a focused field, a key down of W and a char `w`, one `Update`. No call, and the field text is `"w"u8`. With the focus on a button, W calls one time), `TheTextGuardPassesANonTextKeyAndAShortcut` (Escape, Down, F1 (290), and Control+S each call one time with the field focused), `TheTextGuardSkipsAKeypadKey` (keypad 1 (321)), `AddActionRejectsAnEmptyOrDuplicateName`, `BindRejectsAnUnknownActionADuplicateInputAndABadCode` (an unknown action, a second bind of Escape with no modifier, key -1, mouse button -1, gamepad `None`, and `A | B`. The map keeps its counts), `AddActionAndBindKeyCopyTheNames` (change a byte of the source arrays after the add), `ABindDuringAPressTakesEffectAtTheNextPress` (a command that binds F2 during a press of F1), and `ACommandThatThrowsLeavesTheMapUsable` (through `Update` and through `PushGamepad`. The next input calls the other action, and a held button does not fire again).
  - File(s): `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`
  - Estimate: 12 steps

- [x] **6.3 Run the gate of phase 6 and record the results**
  - Description: Run the build, the tests, and `UiActionMapTests` (total 23). Expect `failed: 0`. Run `grep -nE 'throw ' src/BrutalUi.Framework/Actions/UiActionMap.cs` and check that each `throw` sits in the constructor, `AddAction`, a bind method, or `Activate`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

---

## Phase 7: The 0-Byte Gate and the Docs (7/7 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ActionMapAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
**Needs:** Phase 6
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the gate measures bytes that the Risks table does not explain.

Group A: the gate (tasks 7.1 to 7.3).

- [x] **7.1 Write ActionMapAllocationTests.cs (5 tests)**
  - Description: Write `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` with 5 tests in the shape of `CommandAndEventAllocationTests` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()` right before the measure, 100 measured iterations, `Assert.Equal(0L, after - before)`). Task 7.1 of the plan holds the scene and the five bodies word for word: `AnUnhandledKeyAllocatesNothing`, `AMouseButtonOverNoElementAllocatesNothing`, `AGamepadPushAllocatesNothing`, `TheFocusActionsAllocateNothing`, and `TheMeasuredBodyDoesRealWork`. The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, no array, and no `Assert`.
  - File(s): `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs`
  - Estimate: 12 steps

- [x] **7.2 Run the gate class in Debug**
  - Description: Run the build, the tests, and the class of task 7.1. Expect a total of 5 with `failed: 0` in the default Debug configuration. When a gate measures bytes, stop and escalate (the agent tier line).
  - File(s): `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 4 steps

- [x] **7.3 Run the four plants of the gate**
  - Description: Copy each file to the scratchpad first. Plant 1: `GC.KeepAlive(new object());` as the first line of `UiActionMap.PressKey`. Plant 2: the same line as the first line of `UiActionMap.PushGamepad`. Plant 3: the same line as the first line inside the slot block of `UiContext.DrainEvents`. Plant 4: an empty body of the key step of the gate. For each plant, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Record which tests fail and the bytes (plant 4 fails `TheMeasuredBodyDoesRealWork`). Restore each file and check it with `cmp`. Run the tests again and expect 0 failed.
  - File(s): `src/BrutalUi.Framework/Actions/UiActionMap.cs`, `src/BrutalUi.Core/Tree/UiContext.cs`, `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` (each restored), `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 10 steps

Group B: the docs (tasks 7.4 to 7.7).

- [x] **7.4 Edit sections 3, 4, 9, and 10 of the technical design**
  - Description: Edit `docs/technical-design.md`. Section 3: the HelloPanel comment reads `Phase 3: one store, one command, one action`. Section 4: add one sentence to step 2 of the frame order: the input pass calls `OnUnhandledInput` for an input that no element took, and the game pushes the gamepad before `Update`. Section 9: add Flow item 10 (the slot and its rules) and replace the text of "Action map" with the rules of this feature (the actions, the bindings, the modifier rule, the repeat rule, the text guard, the mouse rule, the gamepad edge, `PollGamepad`, the one-map rule, `FocusCommands`, the build rules, and the error rule). Section 10: add a row `UiActionMap` to the Components table (source: the Unity Input System layer of mosaic-ui. Change: the GLFW poll and the Core slot).
  - File(s): `docs/technical-design.md`
  - Estimate: 8 steps

- [x] **7.5 Edit section 14 of the technical design**
  - Description: Edit section 14 of `docs/technical-design.md`: add a bullet for the action tests (`UnhandledInputTests`, `UiActionMapTests`, `FocusActionTests`, `GamepadActionTests`, `GlfwUiInputGamepadTests`) and a bullet "A seventeenth allocation gate" for `ActionMapAllocationTests`, with the scene and the plant results of task 7.3. Check the ordinal first: the last gate bullet must be "A sixteenth allocation gate". In the bullet of the sample projects, add line 6, the new push order (the `Add` click and the Escape), the state check, the new values of line 2, and the plant results of task 5.6.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] **7.6 Edit CHANGELOG.md**
  - Description: Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `UiContext.OnUnhandledInput`, `UiUnhandledInputHandler`, `UiGamepadButtons`, `UiActionMap`, `UiActions`, `FocusCommands`, `GlfwUiInput.PollGamepad`, the HelloPanel state row, and the action map allocation gate (state-and-commands/action-map).
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] **7.7 Run the scripts and the prose lint**
  - Description: Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and `implementation.md`. Fix the prose until each file passes. Record the results and each decision that the plan did not settle in `context.md`.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/state-and-commands/action-map/implementation.md`, `docs/features/state-and-commands/action-map/context.md`
  - Estimate: 8 steps

---

## Bugs & Issues

**Active Bugs:**
- none

---

## Technical Debt

- none

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
