# Action Map - Implementation Plan

**Epic:** state-and-commands (feature 4 of 4)
**Status:** Ready for Review
**Started:** 2026-10-29
**Target Completion:** 2026-10-29
**Last Updated:** 2026-10-29 15:20

---

## Quality Bar

**Target quality:** A library feature of the input pass. The action map is the keyboard, mouse, and gamepad path of each game menu. The panels-and-modes epic lists the maps of a mode on top of it. A key that fires two actions, a typed letter that fires an action, a lost gamepad press, a focus move to the wrong element, or a hidden allocation in the frame breaks each game that uses it.

- **One input, one action:** A key down that no element handled, a press over no element, or a new gamepad press calls the command of its action one time. A key that a handler stopped, a Tab move, a key up, a repeat, and a held gamepad button call nothing. A test pins each rule.
- **The text field keeps its keys:** A focused `TextField` keeps its edit keys (the stop rule of the walk) and its text keys (the text guard of the map). Escape, the arrow keys Up and Down, the function keys, and a shortcut with Control, Alt, or Super still reach the map. A test pins each rule.
- **One small Core change:** `BrutalUi.Core` gets one slot on `UiContext` (`OnUnhandledInput`), its delegate type, one call in the drain, and the enum `UiGamepadButtons`. Core names no Framework type and no GLFW type. `BrutalUi.Framework` names no GLFW type.
- **0 bytes after warm-up:** `ActionMapAllocationTests` (the seventeenth allocation gate) measures 0 bytes for an unhandled key, a press over no element, a gamepad push, and the focus actions, through `Update` and `Draw`, in the default Debug build. A planted allocation in the resolve path, in the gamepad push, and at the slot call of the drain each fails it. The HelloPanel smoke run measures the same path in a real frame.
- **The sample proves the epic:** HelloPanel shows one store that drives a label, one command on a button, and one action on a key. The smoke run prints a count line and exits with code 1 on a count of 0.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the unhandled-input rule, the text guard, the modifier rule, the edge rule of the gamepad, the one-map rule (A8), and the error rule.

---

## Executive Summary

This feature adds `UiActionMap` to `BrutalUi.Framework`. A game names an action (`Confirm`, `Cancel`, `NavigateUp`, `NavigateDown`), maps it to a command name, and binds it to a key with modifiers, a mouse button, or a gamepad button, at build time. The map resolves an input to its action and calls `CommandRegistry.Invoke` with the command name, the same call as a button click (Q3, A1).

The map reads its input through one new slot on `UiContext`: `OnUnhandledInput` (A6). The drain of the input pass calls the slot for a key down that no handler stopped and that is not a Tab move, and for a press over no element. `UiActionMap.Activate(context)` puts the map in the slot, so one map is active on a context (A8). The Glfw adapter reads the gamepad one time in each frame through `Glfw.GetGamepadState` and gives the buttons as `UiGamepadButtons`. The game pushes them into the map with `map.PushGamepad(input.PollGamepad())`. The map finds each new press and calls its action. `FocusCommands` registers two commands whose target is the context and whose static handlers call `UiContext.FocusNext` and `UiContext.FocusPrevious`, so `NavigateDown` and `NavigateUp` move the focus.

The plan has 7 phases and 45 tasks. Phase 1 delivers the key path end to end, with the Core slot. Phase 2 delivers the focus actions and the one-map rule. Phase 3 delivers the gamepad path and the GLFW poll. Phase 4 delivers the mouse path. Phase 5 delivers the HelloPanel sample. Phase 6 delivers the text guard and the build-time rules. Phase 7 delivers the 0-byte gate and the docs. Phases 1 and 4 change the input pass of Core and need an ADVANCED dev agent.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (binds Escape and the gamepad button B to `Cancel`, the arrow keys and the D-pad to the focus actions, and a shortcut to a command, at build time, and pays no allocation in the frame after that)
- [x] **The player** (moves through a menu with a keyboard or a gamepad, and types into a text field with no stray action)
- [x] **The sibling epics** (panels-and-modes lists the active maps of a mode and calls `Activate`. xml-markup can read an action table later)
- [x] **The maintainer** (runs one more 0-byte gate and a smoke run with one more count line, through `scripts/test-linux.sh` and `scripts/test-windows.ps1`, with no script change)

**User Subsets Served:**
- [x] **Simple** - one action `Cancel` on Escape that calls one command.
- [x] **Complex** - 16 actions, keys with modifiers, three mouse buttons, four gamepad buttons, the focus actions on the arrow keys and the D-pad, a focused text field, and a command that writes a store that a bound label shows in the same `Update`.

**UX Approach:**
- One `AddAction` call and one bind call for each input, at build time. One `Activate` call. One `PushGamepad` call in each frame. No closure, no `IDisposable`, and no event keyword.
- The action names live in `UiActions`, and the focus command names live in `FocusCommands`, as UTF-8 properties.

---

## Goals

### Primary Goals
1. Add the slot `UiContext.OnUnhandledInput` and its call in the drain, with no Framework type in Core (A6).
2. Add `UiActionMap` with actions, a struct list of bindings that grows at build time only, the key, mouse, and gamepad resolve, and one active map on a context (A8).
3. Add `FocusCommands`, so `NavigateUp` and `NavigateDown` call `FocusPrevious` and `FocusNext`.
4. Add `UiGamepadButtons` to Core and `GlfwUiInput.PollGamepad` to the Glfw adapter (A6).
5. Keep the edit keys and the text keys of a focused `TextField` out of the map.
6. Show the store, the command, and the action in HelloPanel, with a count line and a failure on a count of 0 (A7).
7. Prove 0 bytes after warm-up for the resolve and invoke path with the seventeenth allocation gate.

### Success Criteria
- [ ] The phase 1 demo passes: `UiActionMapTests` pushes an Escape key down with no focus owner, runs `Update`, and reads one call of the command of `Cancel`.
- [ ] `UnhandledInputTests`, `UiActionMapTests`, `FocusActionTests`, `GamepadActionTests`, `GlfwUiInputGamepadTests`, and `ActionMapAllocationTests` pass.
- [ ] `ActionMapAllocationTests` measures 0 bytes in Debug. The plants of task 7.3 each fail it.
- [ ] The HelloPanel smoke run prints the line `HelloPanel: state: ...` with three counts above 0, and `scripts/test-linux.sh` prints its PASS line.
- [ ] Each existing test passes with no edit.

---

## Architecture Overview

### The Seam

The map lives in Framework, and the input pass lives in Core. Core cannot name `UiActionMap`. The seam is one delegate slot on `UiContext`, in the shape of `UiContext.OnFileDrop`: `OnUnhandledInput`, of the type `UiUnhandledInputHandler`. The drain calls it for an input that no element took. `UiActionMap` creates one instance delegate of its own handler in its constructor. `Activate` writes that delegate into the slot. The frame path then creates no delegate.

The gamepad has no event in GLFW. The adapter reads the state with `Glfw.GetGamepadState`. `BrutalUi.Glfw` and `BrutalUi.Framework` each reference Core only, so the shared type lives in Core: the flags enum `UiGamepadButtons`, next to `PointerButton` and `UiModifiers`. Its bit `i` is the GLFW gamepad button `i`, so the adapter converts with a shift and no table. The adapter returns the buttons, and the game gives them to the map (Notes question 2).

### File Tree (end state)

```
src/BrutalUi.Core/
├── Input/UiUnhandledInputHandler.cs             # phase 1 (new): delegate UiUnhandledInputHandler. phase 4: the press remarks
├── Input/UiEventEnums.cs                        # phase 3: enum UiGamepadButtons
└── Tree/UiContext.cs                            # phase 1: OnUnhandledInput and the key call. phase 4: the press call
src/BrutalUi.Framework/
├── Actions/UiActions.cs                         # phase 1 (new): the action names
├── Actions/UiActionMap.cs                       # phase 1 (new): actions, keys, Activate. phase 2: Deactivate, IsActive.
│                                                #   phase 3: the gamepad. phase 4: the mouse. phase 6: the guard and the rules
└── Actions/FocusCommands.cs                     # phase 2 (new)
src/BrutalUi.Glfw/GlfwUiInput.cs                 # phase 3: PollGamepad and the conversion
samples/HelloPanel/
├── HelloPanel.csproj                            # phase 5: the reference to BrutalUi.Framework
├── HelloState.cs                                # phase 5 (new): the store, the label, the command, the map
├── HelloScreen.cs                               # phase 5: the state row
└── Program.cs                                   # phase 5: the poll, the push, the smoke input, line 6
tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs           # phase 1 (9), phase 4 (+3) = 12 tests
tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs       # phase 1 (8), phase 2 (+3), phase 4 (+4), phase 6 (+8) = 23 tests
tests/BrutalUi.Framework.Tests/Actions/FocusActionTests.cs       # phase 2 (5 tests)
tests/BrutalUi.Framework.Tests/Actions/GamepadActionTests.cs     # phase 3 (7 tests)
tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs # phase 7 (5 tests)
tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs             # phase 3 (6 tests)
docs/technical-design.md                         # phase 7: sections 3, 4, 9, 10, and 14
CHANGELOG.md                                     # phase 7
docs/features/state-and-commands/action-map/context.md   # each phase
```

These files do not change: each other file of `src/BrutalUi.Core/`, each file of `src/BrutalUi.Framework/State/`, `Binding/`, `Commands/`, `Events/`, and `Services/`, each `.csproj` other than `HelloPanel.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/HelloPanel/HelloScreen.css`, `samples/HelloPanel/HelloWidgets.cs`, and each existing test file. The new test files compile in their projects with no project edit. Namespaces stay flat: `BrutalUi.Core`, `BrutalUi.Framework`, `BrutalUi.Glfw`, and the matching `.Tests` namespaces.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public delegate void UiUnhandledInputHandler(UiEventType type, int code, UiModifiers modifiers, bool isRepeat); // phase 1

public sealed class UiContext
{
    public UiUnhandledInputHandler? OnUnhandledInput { get; set; }              // phase 1 (key down). phase 4 (press)
}

[Flags]
public enum UiGamepadButtons                                                    // phase 3. Bit i = GLFW gamepad button i
{
    None = 0, A = 1 << 0, B = 1 << 1, X = 1 << 2, Y = 1 << 3,
    LeftBumper = 1 << 4, RightBumper = 1 << 5, Back = 1 << 6, Start = 1 << 7, Guide = 1 << 8,
    LeftThumb = 1 << 9, RightThumb = 1 << 10,
    DpadUp = 1 << 11, DpadRight = 1 << 12, DpadDown = 1 << 13, DpadLeft = 1 << 14,
}

namespace BrutalUi.Framework;

public static class UiActions                                                   // phase 1
{
    public static ReadOnlySpan<byte> Confirm { get; }                           // "Confirm"u8
    public static ReadOnlySpan<byte> Cancel { get; }                            // "Cancel"u8
    public static ReadOnlySpan<byte> NavigateUp { get; }                        // "NavigateUp"u8
    public static ReadOnlySpan<byte> NavigateDown { get; }                      // "NavigateDown"u8
}

public static class FocusCommands                                               // phase 2
{
    public static ReadOnlySpan<byte> Next { get; }                              // "ui.focus-next"u8
    public static ReadOnlySpan<byte> Previous { get; }                          // "ui.focus-previous"u8
    public static void Register(CommandRegistry registry, UiContext context);
}

public sealed class UiActionMap
{
    public struct CreateInfo { public required CommandRegistry Commands; }      // phase 1
    public UiActionMap(in CreateInfo info);                                     // phase 1
    public CommandRegistry Commands { get; }                                    // phase 1
    public int ActionCount { get; }                                             // phase 1
    public int BindingCount { get; }                                            // phase 1
    public void AddAction(ReadOnlySpan<byte> action, ReadOnlySpan<byte> command); // phase 1. phase 6: the name rules
    public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers = UiModifiers.None); // phase 1. phase 6: the rules
    public void BindGamepadButton(ReadOnlySpan<byte> action, UiGamepadButtons button);  // phase 3
    public void BindMouseButton(ReadOnlySpan<byte> action, PointerButton button, UiModifiers modifiers = UiModifiers.None); // phase 4
    public bool InvokeAction(ReadOnlySpan<byte> action);                        // phase 1
    public bool PressKey(int key, UiModifiers modifiers);                       // phase 1
    public int PushGamepad(UiGamepadButtons buttons);                           // phase 3
    public bool PressMouseButton(PointerButton button, UiModifiers modifiers);  // phase 4
    public void Activate(UiContext context);                                    // phase 1
    public void Deactivate();                                                   // phase 2
    public bool IsActive { get; }                                               // phase 2
}

namespace BrutalUi.Glfw;

public sealed class GlfwUiInput
{
    public UiGamepadButtons PollGamepad(int gamepadId = 0);                     // phase 3
}
```

### Internal Shape

- **The drain call (phase 1, Core).** In `UiContext.DrainEvents`, store the condition of the Tab move in a local `bool tabMove` and keep the Tab branch on it. Right after the Tab branch, add one block. Read `OnUnhandledInput` into a local `handler` one time for each entry. Call it when `handler` is not `null`, `routed` is `true`, `stopped` is `false`, and one of two cases holds. Case 1 (phase 1): the type is `UiEventType.KeyDown` and `tabMove` is `false`. Call `handler(UiEventType.KeyDown, entry.Payload.Key.Key, entry.Payload.Key.Modifiers, entry.Payload.Key.IsRepeat)`. Case 2 (phase 4): the type is `UiEventType.PointerDown` and `target` is `null` (the press hit no element). Call `handler(UiEventType.PointerDown, (int)entry.Payload.Pointer.Button, entry.Payload.Pointer.Modifiers, false)`. The call runs inside the drain, after the walk, after `FinishRelease`, and after the Tab move, so the slot reads the focus of that point in the ring. A key down with no focus owner has `routed` set and no walk, so it reaches the slot. That is the main case of a game with no UI focus.
- **The slot rules (Core).** The slot gets no key up, no char, no move, no release, no scroll, and no entry with a target from the caller. An exception of the slot leaves `Update` through the `finally` block of the drain, as an exception of a handler does. The entries after it stay in the ring for the next `Update`. The slot runs while the drain runs, so a call of `Update` from the slot throws `InvalidOperationException`. A call of `FocusNext`, `FocusPrevious`, or `SetFocus` from the slot is valid. An event that the slot pushes waits for the next drain.
- **`UiActionMap` (Framework).** `private ActionEntry[] _actions` and `private int _actionCount`. `private struct ActionEntry` holds `byte[] Name` and `byte[] Command` (copies). `private InputBinding[] _bindings` and `private int _bindingCount`. `private struct InputBinding` holds `InputKind Kind`, `int Code`, `UiModifiers Modifiers`, and `int Action` (the index of the action). `private enum InputKind : byte { Key, MouseButton, GamepadButton }`. Both arrays start empty, then hold 8 slots, then double. `private readonly UiUnhandledInputHandler _onInput` holds `HandleInput`, created one time in the constructor. `private UiContext? _context` holds the context of the last `Activate`. `private UiGamepadButtons _gamepad` holds the buttons of the last push.
- **The modifier rule.** `private const UiModifiers MatchMask = UiModifiers.Shift | UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super;`. A bind stores `modifiers & MatchMask`. A resolve compares `modifiers & MatchMask` with the stored value for equality. Caps Lock and Num Lock thus never block a match. `Control+S` does not match a bind of `S`, and `S` does not match a bind of `Control+S`.
- **The resolve.** `private int Find(InputKind kind, int code, UiModifiers modifiers)` scans the bindings in the order of the bind and returns the first index with the same kind, code, and modifiers, or -1. `PressKey` and `PressMouseButton` call `Find`, then `Invoke(binding.Action)`. `private bool Invoke(int action)` reads `byte[] command = _actions[action].Command` into a local and returns `Commands.Invoke(command)`. A miss returns `false`. The return value is `true` only when the registry holds the command and ran it. `InvokeAction(name)` scans the actions by a byte compare (`SequenceEqual`) and calls `Invoke`.
- **The gamepad edge (phase 3).** `PushGamepad(buttons)`: `UiGamepadButtons pressed = buttons & ~_gamepad; _gamepad = buttons;`. With `pressed == UiGamepadButtons.None`, return 0. Else copy `_bindings` and `_bindingCount` to locals, scan in the order of the bind, and for each gamepad binding with `((int)pressed & binding.Code) != 0`, call `Invoke` and count a `true`. Return the count. The method writes `_gamepad` before the first call, so a command that throws does not fire the same press again at the next push. `PushGamepad` does not need `Activate`: the game holds the map and pushes into it.
- **The slot handler.** `private void HandleInput(UiEventType type, int code, UiModifiers modifiers, bool isRepeat)`: return at once for a repeat. For `KeyDown`, apply the text guard (phase 6), then call `PressKey(code, modifiers)`. For `PointerDown` (phase 4), call `PressMouseButton((PointerButton)code, modifiers)`. Each other type does nothing.
- **The text guard (phase 6).** `HandleInput` skips a key down when `_context?.FocusedElement is TextField`, no Control, Alt, or Super is down, and the key is a text key. A text key is each key code below 256 (the printable keys and the world keys of GLFW) and each keypad key from 320 to 336. Each key from 256 to 319 (Escape, Enter, Tab, Backspace, Insert, Delete, the arrow keys, Page Up, Page Down, Home, End, the lock keys, Print Screen, Pause, and F1 to F25) passes. The guard holds the numbers as private constants with the GLFW names in their comments, as `UiContext.TabKey` does. The guard lives in `HandleInput` only. `PressKey` is the raw resolve and applies no guard.
- **One active map (phase 1, phase 2).** `Activate(context)`: check the context, then `context.OnUnhandledInput = _onInput; _context = context;`. The write replaces each delegate in the slot, also the delegate of another map. `IsActive` (phase 2): `_context is { } c && c.OnUnhandledInput == _onInput`. `Deactivate` (phase 2): when `IsActive`, write `null` into the slot. Then set `_context` to `null`. A map that another map replaced keeps its state and clears nothing.
- **The build rules (phase 6).** `AddAction` throws `ArgumentException` (parameter `action` or `command`) for an empty name and for an action name that the map holds. Each bind throws `ArgumentException` (parameter `action`) for an action that the map does not hold, and for an input that the map binds already (the same kind, code, and stored modifiers). `BindKey` throws `ArgumentOutOfRangeException` for a key below 0 (the GLFW unknown key is -1). `BindMouseButton` throws it for a button below 0. `BindGamepadButton` throws `ArgumentException` for `None` and for a value with more than one bit (`BitOperations.PopCount`). The messages are constant. A command needs no register at the time of the bind: the registry looks it up at each invoke.
- **The re-entry rule (phase 6).** Each resolve copies the binding array and the count to locals before the scan. `Invoke` copies the command array to a local before the call. A command that adds an action or a binding thus takes effect at the next input, and the array growth of the add does not break the scan.
- **`FocusCommands` (phase 2).** Two `static readonly CommandHandler` fields: `s_next = static target => ((UiContext)target!).FocusNext();` and `s_previous`, the same with `FocusPrevious`. `Register(registry, context)` checks both arguments with `ArgumentNullException.ThrowIfNull`, then calls `registry.Register(Next, s_next, context)` and `registry.Register(Previous, s_previous, context)`. The registry throws for a second register of the same names.
- **`GlfwUiInput.PollGamepad` (phase 3).** Return `UiGamepadButtons.None` after `Dispose`. Throw `ArgumentOutOfRangeException` for an id outside 0 to 15 (`GLFW_JOYSTICK_1` to `GLFW_JOYSTICK_LAST`). Call `Glfw.GetGamepadState(gamepadId, out GlfwGamepadState state)` (check `out` or `ref` in task 3.5). A `false` result (no joystick, or no gamepad mapping) gives `None`. Else convert the 15 button values with an internal static method: each button with the value `GlfwButtonAction.Press` sets the bit `1 << i`. The conversion takes the button values as a span, so a test fills it with no device. The method reads the state of one gamepad. It reads no axis and applies no deadzone.

### Data Flow

```
Build time (allocates one time):
  FocusCommands.Register(commands, ui)                       ──► "ui.focus-next" (target ui), "ui.focus-previous" (target ui)
  map = new UiActionMap(new() { Commands = commands })       ──► _onInput = HandleInput (one delegate)
  map.AddAction(UiActions.Cancel, "menu.close"u8)            ──► _actions[n] = { copy of the name, copy of the command }
  map.AddAction(UiActions.NavigateDown, FocusCommands.Next)
  map.BindKey(UiActions.Cancel, 256 /* Escape */)            ──► _bindings[n] = { Key, 256, None, action 0 }
  map.BindGamepadButton(UiActions.NavigateDown, UiGamepadButtons.DpadDown)
  map.Activate(ui)                                           ──► ui.OnUnhandledInput = _onInput

Each frame:
  Glfw.PollEvents() ──► GlfwUiInput ──► ui.PushEvent (key, pointer)
  pad = input.PollGamepad() ──► Glfw.GetGamepadState ──► UiGamepadButtons
  map.PushGamepad(pad) ──► new presses ──► commands.Invoke(name) ──► FocusNext() / game handler
  ui.Update(dt)
    1. Input pass (DrainEvents), for each entry:
         target step ──► walk ──► FinishRelease ──► Tab move
         ──► key down, routed, not stopped, no Tab move ──► OnUnhandledInput(KeyDown, key, mods, repeat)
         ──► press, routed, hit no element                ──► OnUnhandledInput(PointerDown, button, mods, false)
               ──► UiActionMap.HandleInput ──► repeat? stop ──► text guard ──► Find ──► commands.Invoke(name)
    2. Binding pass ── a store that a command wrote shows in the same Update
    3. Style pass ── 4. Transition pass ── 5. Layout pass ── 6. Caret pass ── 7. Clear pass
  ui.Draw(renderer)
```

### The Frame Order

The order of the passes does not change. The key and press path of the map runs inside the input pass, at the position of its entry in the ring, so a command that sets a store shows in the binding pass of the same `Update`. The gamepad path runs where the game calls `PushGamepad`. The recommended place is after `Glfw.PollEvents` and before `Update`, so a command of a gamepad press also shows in the same `Update`. A focus move from a gamepad press before `Update` changes the owner at once (Decision 12 of keyboard-focus), and the key entries of the same frame go to the new owner.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements do not name the demo of phase 1, so Notes question 4 holds it (Slice rule 12).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines, also for each namespace that a `cref` names. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/<Project>/<Project>.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. The Framework tests cannot use the internal event factories of `BrutalUi.Core.Tests`. Copy the private `Pointer`, `Key`, `CreateContext`, and `AddRoot` helpers of `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` into each Framework test file that needs them. `UiContext.PushEvent(in ...)` needs a variable, not a property. A `static readonly` handler field that reads a later `static readonly` field in its lambda gives CS8604, so declare the later field first. The map holds no generic code, so the Debug box of element-bindings does not apply. Still, write no `(object)` cast and no `Equals` call on a value in the resolve path.

### Phase 1: The Key Path
**Goal:** The smallest path that proves the idea: a game adds the action `Cancel` with a command, binds Escape to it, and activates the map. A key down of Escape that no element handled reaches the map through the new slot of `UiContext`, and the command runs one time.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 8 with `failed: 0`. The first test pushes an Escape key down with no focus owner, runs `Update`, and reads one call of the command of `Cancel`.
**Needs:** none
**Agent tier:** ADVANCED (opus). The phase changes the drain of the input pass in `UiContext`, a load-bearing Core path that 16 allocation gates measure.

**Tasks (group A: the Core slot):**
- [ ] 1.1 Create `docs/features/state-and-commands/action-map/context.md`. Check that the commands-and-events commit exists (`git log --oneline -3`). Record the base commit (`git rev-parse HEAD`) and the test counts of the solution, of `BrutalUi.Core.Tests`, of `BrutalUi.Framework.Tests`, and of `BrutalUi.Glfw.Tests` (expected 2668 with 2 skipped, 1733, and 93 when commands-and-events is the base. Read the Glfw count from a run of that project). Record the Seam, the Internal Shape, and the Frame Order of the Architecture Overview.
- [ ] 1.2 Write `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`: `public delegate void UiUnhandledInputHandler(UiEventType type, int code, UiModifiers modifiers, bool isRepeat);`. The summary: a handler for an input that no element took. `UiContext` calls it from the drain. The parameter docs: `type` is `UiEventType.KeyDown` (phase 4 adds `PointerDown`). `code` is the GLFW key code of a key down. `isRepeat` is `true` for a key repeat. The remarks state the rules of the Internal Shape ("The drain call" and "The slot rules"), the A6 reason (the action map reads its input here, and Core names no Framework type), and the build-time rule (create the delegate one time, and set the slot at build time).
- [ ] 1.3 Edit `src/BrutalUi.Core/Tree/UiContext.cs`. Add `public UiUnhandledInputHandler? OnUnhandledInput { get; set; }` after `OnContentScale`, with remarks in the shape of `OnFileDrop`: one slot, set at build time, a `null` slot does nothing, `UiActionMap.Activate` writes it. Add the key case of "The drain call" of the Internal Shape to `DrainEvents`: the local `tabMove`, then the slot block after the Tab branch. Extend the remarks of `DrainEvents` with a paragraph "The unhandled input rule (A6 of state-and-commands)", and add one sentence to the Tab rule paragraph of `PushEvent(in KeyEvent)`: a key down that no handler stopped and that is not a Tab move goes to `OnUnhandledInput` after its walk. Change no other line of the drain.
- [ ] 1.4 Write `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs` with 9 tests. Use `PointerTree` or the helpers of `TabNavigationTests` for the tree, and a private recorder target that stores each call (type, code, modifiers, repeat) in a fixed array. Tests: `AKeyDownWithNoFocusOwnerCallsTheSlot` (key 256 with Shift: one call with these values and `isRepeat` `false`), `AKeyDownThatNoHandlerStopsCallsTheSlotAfterTheWalk` (an owner with a `OnKeyDown` handler that records the order: the handler first, the slot second), `AKeyDownThatAHandlerStopsDoesNotCallTheSlot`, `AKeyUpDoesNotCallTheSlot`, `ATabMoveDoesNotCallTheSlotAndAControlTabDoes` (Tab and Shift+Tab move the focus and call nothing. Control+Tab calls the slot with key 258), `AKeyWithATargetFromTheCallerDoesNotCallTheSlot`, `ARepeatCallsTheSlotWithTheRepeatFlag`, `AFocusedTextFieldKeepsItsEditKeys` (with a focused `TextField`: Left, Backspace, and the main Enter call nothing. Escape (256) and Down (264) each call the slot one time), and `ASlotThatThrowsLeavesUpdateAndTheNextUpdateDrainsTheRest` (two key downs. The slot throws at the first. `Update` throws. The next `Update` calls the slot for the second key, and `QueuedEventCount` is 0).
- [ ] 1.5 Run the build, the tests, and `UnhandledInputTests` of `BrutalUi.Core.Tests` (total 9). Expect `failed: 0`, and each other test passes with no edit. Record the results in `context.md`.

**Tasks (group B: the map):**
- [ ] 1.6 Write `src/BrutalUi.Framework/Actions/UiActions.cs`: `public static class UiActions` with the four properties of the Public API, each `=> "<Name>"u8;`. The remarks: an action name is UTF-8 and compares by its bytes. A game can use other names. The names are the words of the requirement (Notes, Conflicts and Gaps item 3).
- [ ] 1.7 Write `src/BrutalUi.Framework/Actions/UiActionMap.cs` with the phase 1 members of the Public API and the Internal Shape: `CreateInfo`, the constructor (`ArgumentNullException` with the parameter name `info` and a constant message when `Commands` is `null`), `Commands`, `ActionCount`, `BindingCount`, `AddAction`, `BindKey`, `InvokeAction`, `PressKey`, `Activate`, the modifier rule, the resolve, and `HandleInput` with the repeat rule and the key branch. Phase 1 `AddAction` and `BindKey` check only that the action exists for a bind (`ArgumentException`). The class remarks state: an action maps a UTF-8 name to a command name. A binding maps a key with modifiers to an action. The map copies each name, and an add can grow an array, so add and bind at build time. A resolve scans the bindings in the order of the bind, allocates nothing, and throws nothing of its own. The map calls `CommandRegistry.Invoke`, so an unknown command calls `UnknownCommand` of the registry. An exception of a command leaves the call. `Activate` writes the slot `UiContext.OnUnhandledInput`, so one map is active on a context (A8). A repeat invokes nothing. The map runs on the UI thread and uses no lock.
- [ ] 1.8 Write `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs` with 8 tests. Use a private sealed `Counter` target with an `int Calls` field and a static `CommandHandler` in a `static readonly` field that adds one to `((Counter)target!).Calls`. Use a context with a `RecordingRenderer` measurer and a viewport of 400 by 300. Tests: `AnUnhandledKeyInvokesTheCommandOfItsAction` (the demo: `Cancel` on Escape (256), `Activate`, a key down with no target and no focus owner, one `Update`. The counter reads 1), `PressKeyReturnsTrueForABoundKeyAndFalseForAnUnboundKey`, `TheModifiersMustMatch` (a bind of Control+S (83): S alone gives `false`, Control+S gives `true`, Control+Shift+S gives `false`), `CapsLockAndNumLockDoNotBlockAMatch`, `InvokeActionInvokesTheCommandOfTheAction` (also `false` for an unknown action), `AnActionWithAnUnknownCommandCallsTheUnknownCommandSlot` (`PressKey` gives `false`, and the slot of the registry gets the command name), `ARepeatInvokesNothing` (a key down with `IsRepeat` `true` through `Update`), and `AMapWithNoActivateInvokesNothing`.
- [ ] 1.9 Run the build, the tests, and `UiActionMapTests` (total 8). Expect `failed: 0`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 60 (9 tasks, about six to seven steps for one task)

**Files (group A, 4):** `UiUnhandledInputHandler.cs`, `UiContext.cs`, `UnhandledInputTests.cs`, `context.md`
**Files (group B, 4):** `UiActions.cs`, `UiActionMap.cs`, `UiActionMapTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `UnhandledInputTests` and the 8 tests of `UiActionMapTests` pass. Each existing test passes with no edit.
- [ ] The diff of `UiContext.cs` holds the property, the local `tabMove`, the slot block, and doc comment lines only.
- [ ] No file of `src/BrutalUi.Core/` names `BrutalUi.Framework` or a GLFW type.

---

### Phase 2: The Focus Actions and One Active Map
**Goal:** `NavigateDown` and `NavigateUp` move the focus through two commands whose target is the context. A second `Activate` replaces the first map, and `Deactivate` clears the slot of its own map only.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*FocusActionTests'` and reads a total of 5 with `failed: 0`. The first test binds Down (264) to `NavigateDown`, presses it one time with the focus on the first of three buttons, and reads the focus on the second button.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Write `src/BrutalUi.Framework/Actions/FocusCommands.cs`: `public static class FocusCommands` with `Next`, `Previous`, `Register`, and the two static handlers of the Internal Shape. The remarks: the commands call `UiContext.FocusNext` and `UiContext.FocusPrevious` (the gamepad slots of Decision 8 of keyboard-focus), so their rules apply: the order is the paint order, the focus wraps, and with no owner the focus goes to the first or the last element. The registry holds the context until `Unregister` of both names. Map `NavigateDown` to `Next` and `NavigateUp` to `Previous`.
- [ ] 2.2 Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add `IsActive` and `Deactivate` (the Internal Shape). Extend the remarks of `Activate`: the write replaces each delegate in the slot, also a delegate of the game. A mode list of maps waits for panels-and-modes (A8).
- [ ] 2.3 Write `tests/BrutalUi.Framework.Tests/Actions/FocusActionTests.cs` with 5 tests on a root with three focusable buttons. Tests: `NavigateDownMovesTheFocusToTheNextElement` (the demo), `NavigateUpMovesTheFocusToThePreviousElement` (Up (265)), `TheFocusActionsStartWithNoOwnerAndWrap` (with no owner, Down gives the first button. Down at the last button gives the first button), `RegisterAddsTwoCommandsWithTheContextAsTheTarget` (`Count` rises by 2, `Contains` of both names, and `Invoke(FocusCommands.Next)` moves the focus with no map), and `RegisterRejectsANullRegistryOrContext`.
- [ ] 2.4 Edit `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`: add 3 tests. `ASecondActivateReplacesTheFirstMap` (two maps on one context bind Escape to two counters. After the second `Activate`, an Escape adds 1 to the second counter only), `DeactivateClearsTheSlotOfItsOwnMapOnly` (`Deactivate` of the replaced map leaves the second map active), and `IsActiveFollowsTheSlot`.
- [ ] 2.5 Run the build, the tests, `FocusActionTests` (total 5), and `UiActionMapTests` (total 11). Expect `failed: 0`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 30 (5 tasks, about six steps for one task)

**Files (5):** `FocusCommands.cs`, `UiActionMap.cs`, `FocusActionTests.cs`, `UiActionMapTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 5 tests of `FocusActionTests` and the 11 tests of `UiActionMapTests` pass.
- [ ] `FocusCommands` holds its two handlers in `static readonly` fields with `static` lambdas.

---

### Phase 3: The Gamepad Path
**Goal:** A game binds gamepad buttons to actions. The Glfw adapter reads the buttons of one gamepad, and a push into the map calls the action of each new press one time.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*GamepadActionTests'` and reads a total of 7 with `failed: 0`. The first test pushes `UiGamepadButtons.B` two times and reads one call of the command of `Cancel` and the return values 1 and 0.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the shape of `GlfwGamepadState` does not fit the Internal Shape.

**Tasks (group A: the map):**
- [ ] 3.1 Edit `src/BrutalUi.Core/Input/UiEventEnums.cs`: add `UiGamepadButtons` of the Public API after `PointerButton`, with `[Flags]`. The summary of each member names its GLFW constant (for example `GLFW_GAMEPAD_BUTTON_A`, value 0). The remarks: bit `i` is the GLFW gamepad button `i`, so the adapter converts with no table. The names follow the Xbox layout of GLFW. Core reads no gamepad. The enum is the shared type of the Glfw adapter and the action map.
- [ ] 3.2 Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add `BindGamepadButton` and `PushGamepad` (the gamepad edge of the Internal Shape). Phase 3 `BindGamepadButton` checks only that the action exists. The remarks of `PushGamepad`: call it one time in each frame, after the poll and before `Update`. A press is a bit that is set now and was clear at the last push. A held button invokes nothing again. Two new presses in one push invoke in the order of the bind. The method works with no `Activate`. A new map starts with no button, so a button that is held at the first push counts as a press. The method allocates nothing.
- [ ] 3.3 Write `tests/BrutalUi.Framework.Tests/Actions/GamepadActionTests.cs` with 7 tests. Tests: `ANewPressInvokesItsActionOneTime` (the demo), `AHeldButtonInvokesNothingAgain`, `AReleaseAndANewPressInvokeAgain` (`B`, `None`, `B` gives two calls), `TwoNewPressesInOnePushInvokeInTheOrderOfTheBind` (`A` and `B` in one push, with a target that records the order), `ADpadDownBindingMovesTheFocus` (with `FocusCommands` and three focusable buttons), `PushGamepadWorksWithNoActivate`, and `AnUnboundButtonInvokesNothing` (`X` gives 0).
- [ ] 3.4 Run the build, the tests, and `GamepadActionTests` (total 7). Expect `failed: 0`. Record the results in `context.md`.

**Tasks (group B: the GLFW poll):**
- [ ] 3.5 Read the shape of `Brutal.GlfwApi.GlfwGamepadState` and of `Glfw.GetGamepadState` (`out` or `ref`) in the BRUTAL binding (package `Ahwoo.BRUTAL.Glfw`, the version of `Directory.Packages.props`). Use a reflection probe in the scratchpad, not in the repository. Record the button member, its element type, and the names of the GLFW gamepad constants (for example `Glfw.Constants.GLFW_GAMEPAD_BUTTON_A`, or a `GlfwGamepadButton` enum) in `context.md`. When the state holds no span-like button member, escalate (the agent tier line).
- [ ] 3.6 Edit `src/BrutalUi.Glfw/GlfwUiInput.cs`: add `PollGamepad` and the internal static conversion of the Internal Shape. Name the conversion `GamepadButtons` and take the button values as a span of the element type of task 3.5. Add a paragraph to the class remarks: the adapter reads one gamepad when the game calls `PollGamepad`, and it adds no handler for it. Call it one time in each frame, after `Glfw.PollEvents`. Give the result to `UiActionMap.PushGamepad`. The adapter reads no axis. The Guide button can be held by the system. GLFW joystick state is global, so the adapter needs no window for the poll.
- [ ] 3.7 Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs` with 6 tests. Put the class in the collection `GlfwUiInputAllocationTests.CollectionName` (GLFW state is global). Tests: `EachPressedButtonGivesItsBit` (15 cases in one test: one pressed button gives one bit), `AReleasedButtonGivesNoBit`, `TheBitsFollowTheGlfwButtonIndices` (each member of `UiGamepadButtons` equals `1 << ` the GLFW constant of task 3.5, as the test of `UiContext.TabKey` checks the GLFW key enum), `PollGamepadRejectsAnIdOutsideTheRange` (-1 and 16, through the test seam with no window), `ADisposedAdapterPollsNoButton` (the test seam, no GLFW call), and `ALivePollAllocatesNothing` (start GLFW. On a failure of `Glfw.Init`, skip with the reason, as the second gate of `GlfwUiInputAllocationTests` does. Else poll 100 times after 2 warm-up polls and assert 0 bytes. Do not assert a button: the machine can have a gamepad).
- [ ] 3.8 Run the build, the tests, and `GlfwUiInputGamepadTests` (total 6, with 1 skipped when no display exists). Run `xvfb-run -a dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputGamepadTests' 2>&1 | tail -n 5` and expect 6 passed. Record the results and the demo in `context.md`, with the new skipped count of the solution.

**Estimated Steps:** about 50 (8 tasks, about six steps for one task)

**Files (group A, 4):** `UiEventEnums.cs`, `UiActionMap.cs`, `GamepadActionTests.cs`, `context.md`
**Files (group B, 3):** `GlfwUiInput.cs`, `GlfwUiInputGamepadTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 7 tests of `GamepadActionTests` pass, and the 6 tests of `GlfwUiInputGamepadTests` pass under `xvfb-run -a`.
- [ ] `grep -rnE 'Brutal\.GlfwApi|BrutalUi\.Glfw' src/BrutalUi.Framework src/BrutalUi.Core` gives no line.
- [ ] `BrutalUi.Glfw.csproj` and `BrutalUi.Framework.csproj` have no diff.

---

### Phase 4: The Mouse Path
**Goal:** A press over no element reaches the map through the same slot, and the map calls the action of its mouse button. A press on an element calls no action.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 15 with `failed: 0`. The test `AMouseButtonOverNoElementInvokesItsAction` presses the right button outside the panel and reads one call of the command of `Cancel`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus). The phase adds a second case to the slot call of the drain.

**Tasks:**
- [ ] 4.1 Edit `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`: extend the docs. `type` can be `UiEventType.PointerDown`, and `code` is then the GLFW mouse button value (`(int)PointerButton`). A press goes to the slot only when it hit no element: the routing gate then reads the pointer as a game pointer. A press on an element goes to the slot never, also when no handler stops it. The click focus rule runs first, so the press clears the focus before the slot call.
- [ ] 4.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add case 2 of "The drain call" of the Internal Shape. Extend the paragraph "The unhandled input rule" of the `DrainEvents` remarks, and add one sentence to the click focus paragraph of `PushEvent(in PointerEvent)`.
- [ ] 4.3 Edit `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs`: add 3 tests. `APressOverNoElementCallsTheSlotWithTheButton` (a root that is not pickable, a right press with Shift outside the child: one call with `PointerDown`, 1, Shift, and `false`), `APressOnAnElementDoesNotCallTheSlot` (also on a pickable element with no handler), and `AReleaseAMoveAndAScrollOverNoElementDoNotCallTheSlot`.
- [ ] 4.4 Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add `BindMouseButton`, `PressMouseButton`, and the `PointerDown` branch of `HandleInput`. The remarks: the map gets a press over no element only. A layout root that is pickable takes each press, so set `IsPickable` to `false` on a root that fills the window (the layout-root rule). A key binding and a mouse binding never match each other, also with the same code.
- [ ] 4.5 Edit `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`: add 4 tests. `AMouseButtonOverNoElementInvokesItsAction` (the demo: a root that is not pickable with a panel of 100 by 100, one `Update` for the layout, a right press and a release at (300, 200) with no target, one `Update`), `AMouseButtonOverAnElementInvokesNothing` (the same press inside the panel), `PressMouseButtonMatchesTheButtonAndTheModifiers`, and `AKeyAndAMouseButtonWithTheSameCodeStayApart` (a bind of key 1 and of mouse button 1 to two actions).
- [ ] 4.6 Run the build, the tests, `UnhandledInputTests` (total 12), and `UiActionMapTests` (total 15). Expect `failed: 0`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 40 (6 tasks, about six to seven steps for one task)

**Files (6):** `UiUnhandledInputHandler.cs`, `UiContext.cs`, `UnhandledInputTests.cs`, `UiActionMap.cs`, `UiActionMapTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 12 tests of `UnhandledInputTests` and the 15 tests of `UiActionMapTests` pass. Each existing test passes with no edit.
- [ ] The slot gets a press over no element only.

---

### Phase 5: The HelloPanel Sample
**Goal:** HelloPanel shows one store that drives a label, one command on a button, and one action on a key, and it feeds the gamepad into the map in each frame (A7). The smoke run drives each one, prints a count line, and exits with code 1 on a count of 0.
**Demo:** a developer runs `xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"` and reads line 6, `HelloPanel: state: adds <A>, resets <R>, versions <V>.`, with three counts above 0 and `exit=0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the smoke run fails a check that the push order of task 5.4 does not explain.

**Tasks:**
- [ ] 5.1 Edit `samples/HelloPanel/HelloPanel.csproj`: add `<ProjectReference Include="../../src/BrutalUi.Framework/BrutalUi.Framework.csproj" />`. Change no other line.
- [ ] 5.2 Write `samples/HelloPanel/HelloState.cs`: `internal sealed class HelloState` with a constructor that takes the `UiContext`. It builds, one time: a `Store<int>` `Score` that starts at 0, a `CommandRegistry`, the commands `hello.add` (adds 1 to the score) and `hello.reset` (sets 0), each with a static handler in a `static readonly` field and the `HelloState` as the target, and each with a count field (`Adds`, `Resets`). It also builds `FocusCommands.Register(commands, context)`, and a `UiActionMap` with `Cancel` to `hello.reset` (Escape (256) and gamepad `B`), `NavigateUp` to `FocusCommands.Previous` (Up (265) and `DpadUp`), and `NavigateDown` to `FocusCommands.Next` (Down (264) and `DpadDown`). Use the GLFW key values of `GlfwKey` cast to `int`. The constructor also builds one row (class `hello-row`) with a button `Add` (bound with `BindCommand(commands, "hello.add"u8)`) and a label bound with `BindText(Score, static s => s)`. Expose `Row`, `AddButton`, `Map`, `Score`, `Adds`, and `Resets`. The remarks state the A7 demo, the build-time rule, and the label rule: the longest score text (`2147483647`, 10 bytes) fits the start buffer of a label (32 bytes), so the label never grows its buffer.
- [ ] 5.3 Edit `samples/HelloPanel/HelloScreen.cs`: `Build` takes the `HelloState` and adds `state.Row` as the last child of the panel, after the widget section. Update the class remarks: the panel holds the state row last. The `Add` button takes the focus, so the Tab order ends with it. The row uses the `hello-row` class, so the sheet needs no new rule.
- [ ] 5.4 Edit `samples/HelloPanel/Program.cs`. (1) Create the `HelloState` after the `UiContext`, pass it to `HelloScreen.Build`, and call `state.Map.Activate(ui)` before the frame loop. (2) In `RunFrames`, call `UiGamepadButtons pad = input.PollGamepad();` right after `Glfw.PollEvents()`, outside the UI region, and call `state.Map.PushGamepad(pad);` as the first line of the UI region, before the smoke input. (3) In `PushSmokeInput`, push a click on the `Add` button after the toggle click and before the slider click, so the slider press keeps the focus and the Tab still moves it from the slider to the text field. Push an Escape key down and key up with no target right after the Tab, in each frame with `frameIndex % 4 == 3`. The text field then holds the focus, so the Escape also proves that a focused field passes a non-text key to the map. (4) Add `Adds`, `Resets`, and `Versions` (`Score.Version - 1`) to `RunResult`. Print line 6 `HelloPanel: state: adds <A>, resets <R>, versions <V>.` after line 5. In the smoke check, after the widget check, write `HelloPanel: the synthetic input did not reach the state row.` to standard error and return `ExitFailure` when one of the three counts is 0. (5) Update the remarks of `Program`, `Run`, `RunFrames`, and `PushSmokeInput`: six lines, the poll and the push, the new push order, and the new check.
- [ ] 5.5 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Run the demo command and record the six lines in `context.md`. Line 2 changes, because the state row adds draw data. Record the new values.
- [ ] 5.6 Run two plants. Copy each file to the scratchpad first. Plant A: remove the Escape push from `PushSmokeInput`. Expect exit code 1, `resets 0` in line 6, and the line `HelloPanel: the synthetic input did not reach the state row.` Plant B: put `GC.KeepAlive(new object());` as the first line of `UiActionMap.PressKey`. Expect exit code 1 and the allocation line (30 resets of 24 bytes in the measured frames, or the measured count). Restore each file and check it with `cmp`. Run the demo again and expect `exit=0`.
- [ ] 5.7 Read the command line contract in `docs/features/dev-ops/solution-skeleton/context.md`. When it lists the output lines of HelloPanel, record in `context.md` that line 6 is new, and leave that file unchanged (it belongs to a closed feature). Phase 7 writes line 6 into section 14 of the technical design. Record the plant results and the demo in `context.md`.

**Estimated Steps:** about 50 (7 tasks, about seven steps for one task)

**Files (5):** `HelloPanel.csproj`, `HelloState.cs`, `HelloScreen.cs`, `Program.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The smoke run prints line 6 with three counts above 0, and `scripts/test-linux.sh` prints its PASS line with no change to `scripts/`.
- [ ] Both plants give exit code 1, and the restored files equal their copies.
- [ ] The UI region of `RunFrames` holds the `PushGamepad` call, and the `PollGamepad` call sits outside it.

---

### Phase 6: The Text Guard and the Build Rules
**Goal:** A focused `TextField` keeps its text keys: a letter key types and calls no action. The build-time methods reject an empty name, a duplicate action, an unknown action, a duplicate input, and a bad code. A command that binds or throws during a press leaves the map usable.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 23 with `failed: 0`. The test `TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus` presses W (87) with the field focused and reads no call of the bound action and the char in the field.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 6.1 Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`: add the text guard, the build rules, and the re-entry rule of the Internal Shape. Extend the remarks: the text guard and its key ranges, the reason (GLFW sends a key down and a char for a text key, and the field takes the char), the build rules, the re-entry rule, and the error rule (an exception of a command leaves `PressKey`, `PressMouseButton`, `PushGamepad`, or `Update`, and the map stays usable).
- [ ] 6.2 Edit `tests/BrutalUi.Framework.Tests/Actions/UiActionMapTests.cs`: add 8 tests. `TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus` (the demo: W bound to `Confirm`, a focused field, a key down of W and a char `w`, one `Update`. No call, and the field text is `"w"u8`. With the focus on a button, W calls one time), `TheTextGuardPassesANonTextKeyAndAShortcut` (Escape, Down, F1 (290), and Control+S each call one time with the field focused), `TheTextGuardSkipsAKeypadKey` (keypad 1 (321)), `AddActionRejectsAnEmptyOrDuplicateName`, `BindRejectsAnUnknownActionADuplicateInputAndABadCode` (an unknown action, a second bind of Escape with no modifier, key -1, mouse button -1, gamepad `None`, and `A | B`. The map keeps its counts), `AddActionAndBindKeyCopyTheNames` (change a byte of the source arrays after the add), `ABindDuringAPressTakesEffectAtTheNextPress` (a command that binds F2 during a press of F1), and `ACommandThatThrowsLeavesTheMapUsable` (through `Update` and through `PushGamepad`. The next input calls the other action, and a held button does not fire again).
- [ ] 6.3 Run the build, the tests, and `UiActionMapTests` (total 23). Expect `failed: 0`. Run `grep -nE 'throw ' src/BrutalUi.Framework/Actions/UiActionMap.cs` and check that each `throw` sits in the constructor, `AddAction`, a bind method, or `Activate`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 25 (3 tasks, about eight steps for one task)

**Files (3):** `UiActionMap.cs`, `UiActionMapTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 23 tests of `UiActionMapTests` pass.
- [ ] `PressKey`, `PressMouseButton`, `PushGamepad`, `InvokeAction`, and `HandleInput` hold no `throw` statement.

---

### Phase 7: The 0-Byte Gate and the Docs
**Goal:** `ActionMapAllocationTests` (the seventeenth allocation gate) proves 0 bytes after warm-up for the key, mouse, gamepad, and focus paths through `Update` and `Draw`, in the Debug build. The technical design and the changelog state the slot, the map, the poll, the sample line, and the gate.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ActionMapAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
**Needs:** Phase 6
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the gate measures bytes that the Risks table does not explain.

**Tasks (group A: the gate):**
- [ ] 7.1 Write `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` with 5 tests in the shape of `CommandAndEventAllocationTests` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()` right before the measure, 100 measured iterations, `Assert.Equal(0L, after - before)`). A private sealed `Scene` builds everything before the measure: a context with a `RecordingRenderer` measurer and a viewport of 400 by 300, a root that is not pickable, a panel of 200 by 200 with three focusable buttons and a `TextField`, a `CommandRegistry` with 16 counted commands (`cmd-00` to `cmd-15`, each a `byte[]` with its own `Counter` target and one shared static handler) and the two focus commands, and a map with 18 actions and these bindings: 12 keys (some with Control or Shift), Escape, W, Up, Down, 3 mouse buttons, and 4 gamepad buttons (`A`, `B`, `DpadUp`, `DpadDown`). Each event is a field of the scene (lesson 5). Bodies: `AnUnhandledKeyAllocatesNothing` (each frame focuses the first button with `SetFocus`, pushes Escape, Control+S, an unbound key, and a repeat with no target, then focuses the field and pushes W and Escape, then runs `Update` and `Draw`), `AMouseButtonOverNoElementAllocatesNothing` (a right press and release outside the panel, and a left press and release on a button, then `Update` and `Draw`), `AGamepadPushAllocatesNothing` (each iteration pushes `A | DpadDown`, then `None`, then calls `InvokeAction` of one action, then runs `Update` and `Draw`), `TheFocusActionsAllocateNothing` (Down, Down, Up with no target, then `Update` and `Draw`), and `TheMeasuredBodyDoesRealWork`. The real-work test reads each count and the focus owner before each body, runs one step of each body, and asserts fixed increments: the Escape command +2 from the key step (the button and the field), Control+S +1, W +0, the right button +1, the left button command +0, the `A` command +1, the D-pad focus move to the next button, and the owner after the focus step. The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, no array, and no `Assert`.
- [ ] 7.2 Run the build, the tests, and the class of task 7.1. Expect a total of 5 with `failed: 0` in the default Debug configuration. When a gate measures bytes, stop and escalate (the agent tier line).
- [ ] 7.3 Run four plants. Copy each file to the scratchpad first. Plant 1: `GC.KeepAlive(new object());` as the first line of `UiActionMap.PressKey`. Plant 2: the same line as the first line of `UiActionMap.PushGamepad`. Plant 3: the same line as the first line inside the slot block of `UiContext.DrainEvents`. Plant 4: an empty body of the key step of the gate. For each plant, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Record which tests fail and the bytes (plant 4 fails `TheMeasuredBodyDoesRealWork`). Restore each file and check it with `cmp`. Run the tests again and expect 0 failed.

**Tasks (group B: the docs):**
- [ ] 7.4 Edit `docs/technical-design.md`. Section 3: the HelloPanel comment reads `Phase 3: one store, one command, one action`. Section 4: add one sentence to step 2 of the frame order: the input pass calls `OnUnhandledInput` for an input that no element took, and the game pushes the gamepad before `Update`. Section 9: add Flow item 10 (the slot and its rules) and replace the text of "Action map" with the rules of this feature (the actions, the bindings, the modifier rule, the repeat rule, the text guard, the mouse rule, the gamepad edge, `PollGamepad`, the one-map rule, `FocusCommands`, the build rules, and the error rule). Section 10: add a row `UiActionMap` to the Components table (source: the Unity Input System layer of mosaic-ui. Change: the GLFW poll and the Core slot).
- [ ] 7.5 Edit section 14 of `docs/technical-design.md`: add a bullet for the action tests (`UnhandledInputTests`, `UiActionMapTests`, `FocusActionTests`, `GamepadActionTests`, `GlfwUiInputGamepadTests`) and a bullet "A seventeenth allocation gate" for `ActionMapAllocationTests`, with the scene and the plant results of task 7.3. Check the ordinal first: the last gate bullet must be "A sixteenth allocation gate". In the bullet of the sample projects, add line 6, the new push order (the `Add` click and the Escape), the state check, the new values of line 2, and the plant results of task 5.6.
- [ ] 7.6 Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `UiContext.OnUnhandledInput`, `UiUnhandledInputHandler`, `UiGamepadButtons`, `UiActionMap`, `UiActions`, `FocusCommands`, `GlfwUiInput.PollGamepad`, the HelloPanel state row, and the action map allocation gate (state-and-commands/action-map).
- [ ] 7.7 Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan. Fix the prose until each file passes. Record the results and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 50 (7 tasks, about seven steps for one task)

**Files (group A, 2):** `ActionMapAllocationTests.cs`, `context.md` (the plants touch `UiActionMap.cs` and `UiContext.cs` and restore them)
**Files (group B, 4):** `technical-design.md`, `CHANGELOG.md`, `implementation.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 5 tests of `ActionMapAllocationTests` pass in Debug, and each of the four plants gives a non-zero exit code of `dotnet test`.
- [ ] `scripts/test-linux.sh` prints its PASS line, and no file of `scripts/` changed.
- [ ] Sections 3, 4, 9, 10, and 14 of the technical design and the changelog name the slot, the map, the poll, line 6, and the gate.

---

## Key Technical Decisions

### Confirmed Assumptions

This feature had no feature interview. The items below come from the epic interview of `/plan-epic state-and-commands` (see `docs/features/state-and-commands/epic-overview.md`), and the user confirmed them there:
- Q3 (epic) A command handler is a static method plus a target, like the event slots.
- A6 (epic) The action map reads a key that no element handled through one slot on `UiContext`. The Glfw adapter polls the gamepad once per frame and pushes the state into the map.
- A7 (epic) The HelloPanel sample changes live in action-map. No separate sample feature.
- A8 (epic) This epic sets one active map on the context. A mode list of maps waits for panels-and-modes.
- A2 (epic) `tests/BrutalUi.Framework.Tests/` exists in the solution. The epic adds tests there and makes no new project.
- A1 (epic) Build order: store-and-subscription, element-bindings, commands-and-events, action-map.

### Decision 1: One slot for each input that no element took
**Choice:** `UiContext.OnUnhandledInput` of the type `UiUnhandledInputHandler(UiEventType type, int code, UiModifiers modifiers, bool isRepeat)`. The drain calls it for a key down that no handler stopped and that is not a Tab move, and for a press that hit no element. Notes question 1 holds the open choice.
**Rationale:** A6 allows one slot. The map also needs the mouse buttons, and Framework has no other path to a press. "No element took it" has the same meaning for both: a stopped walk takes a key, and a hit takes a press. The test of a hit and not of a stop keeps a click on a button out of the map, because a button stops the release and not the press. The delegate takes four values and no struct, so Core adds no type for the payload. The shape follows `OnFileDrop`: one delegate that the game or the map sets one time.
**Alternatives:** (a) A key slot of `in KeyEvent` only, and a public `PressMouseButton` that the game calls. (b) Two slots, one for a key and one for a press. Both are in Notes question 1.

### Decision 2: The slot call runs inside the drain, after the Tab move
**Choice:** The drain calls the slot at the position of the entry in the ring, after the walk, `FinishRelease`, and the Tab move.
**Rationale:** A call at the position of the entry keeps the order of the input: a press that moves the focus before a key changes the owner that the text guard reads. A command that writes a store shows in the binding pass of the same `Update`. The Tab move is a built-in action of Core, so a Tab that moved the focus is a handled key. A Control+Tab is not a Tab move, so a game can bind it.
**Alternatives:** (a) A list of unhandled inputs that `Update` calls after the drain: a buffer, and a focus state that differs from the state at the entry. (b) A call before the walk: a stop of a handler cannot keep the key.

### Decision 3: An action maps a name to a command name, and a binding maps an input to an action
**Choice:** `UiActionMap` holds two plain arrays of private structs: the actions (a name and a command name, both UTF-8 copies) and the bindings (a kind, a code, the modifiers, and the index of the action). The resolve is a scan in the order of the bind. Each input has one binding at most.
**Rationale:** The requirement asks for named actions that map to command names, and for a struct list that grows at build time only. A level of names between the input and the command lets a mode or a rebind screen change a binding with no change to the commands. The scan follows Decision 2 of commands-and-events: a game binds tens of inputs, and the scan allocates nothing. One binding for each input gives one result for each input, so a build-time throw shows a bind conflict (Decision 7).
**Alternatives:** (a) A binding that holds the command name and no action: no name for a rebind or a mode list. (b) A dictionary keyed by the input: a boxed or a custom key, and more code for no measured need.

### Decision 4: The modifiers match exactly, and the lock keys do not count
**Choice:** A bind stores the modifiers with Shift, Control, Alt, and Super only. A resolve compares the same four bits for equality.
**Rationale:** An exact match keeps `S` and `Control+S` apart, so a shortcut does not also fire the plain key. Caps Lock and Num Lock are states, not chords, so a bind of `S` still works with Caps Lock on.
**Alternatives:** (a) A subset match (each bound modifier is down): `Control+S` then also fires a bind of `S`. (b) A match of all six bits: Caps Lock blocks each binding.

### Decision 5: A repeat calls no action, and a gamepad action fires on a new press only
**Choice:** The slot handler returns at once for a key repeat. `PushGamepad` invokes an action for a bit that is set now and was clear at the last push.
**Rationale:** One press, one action, as `Button` activates on a key down with no repeat. A held D-pad or a held arrow key thus moves the focus one time. A repeat rule for the focus actions is a later need (Notes, Conflicts and Gaps item 6).
**Alternatives:** A repeat flag for each binding: more state, and no requirement asks for it.

### Decision 6: The text guard lives in the map, and the edit keys stay in Core
**Choice:** Core keeps the stop rule: a `TextField` stops the walk of its edit keys, so they never reach the slot. The map adds the text guard: while a `TextField` holds the focus, it skips a text key with no Control, Alt, or Super. Notes question 3 holds the open choice.
**Rationale:** The requirement says that a focused `TextField` keeps its edit keys. The stop rule does that with no change. But GLFW sends a key down and a char for each text key, and the field takes only the char, so a letter key reaches the slot while the user types. A game that binds W to `NavigateUp` then moves the focus out of the field at each W. The guard in the map changes no rule of Core and no test of the field. The key ranges are fixed GLFW values, as `UiContext.TabKey` is.
**Alternatives:** (a) The stop rule only. (b) The field stops each text key down in Core: a change of the walk that an ancestor handler can see. (c) The map skips each key while a field holds the focus: Escape then cannot close a menu with a field.

### Decision 7: The build rules throw, and the resolve throws nothing
**Choice:** `AddAction` and the bind methods throw for an empty name, a duplicate action, an unknown action, a duplicate input, and a bad code, with constant messages. The resolve methods throw nothing of their own. An exception of a command leaves the call.
**Rationale:** The rule of commands-and-events (Decision 8 there): a build bug shows at build time, and the frame loop throws nothing of its own. A bind of an unknown action is a typo, so it throws. A command needs no register at the bind, because the registry looks it up at each invoke (Decision 4 of commands-and-events).
**Alternatives:** A last bind that replaces the first: a bind conflict stays hidden.

### Decision 8: One active map through the slot (A8)
**Choice:** `Activate(context)` writes the delegate of the map into `OnUnhandledInput`. A second `Activate` replaces it. `IsActive` compares the slot with the delegate of the map. `Deactivate` clears the slot of its own map only.
**Rationale:** A8 sets one active map. The slot holds one delegate, so the slot itself is the state, and Core holds no Framework type. panels-and-modes can call `Activate` for the map of a mode, or replace the slot with a dispatcher of its own.
**Alternatives:** (a) A Framework registry of the active map for each context: a static table or a weak map. (b) `+=` on the slot: two active maps.

### Decision 9: The focus actions are commands with the context as the target
**Choice:** `FocusCommands.Register(registry, context)` registers `ui.focus-next` and `ui.focus-previous` with static handlers and the context as the target. The game maps `NavigateDown` to `FocusCommands.Next` and `NavigateUp` to `FocusCommands.Previous`.
**Rationale:** Q3 and the epic integration point: each action calls a command, and a command is a static handler plus a target. The map thus has one path for each action. A game can bind the focus commands to a button too. The order of the focus follows the paint order of `FocusWalk`, so "down" is the next element in the tree. A spatial search is out of scope.
**Alternatives:** A built-in kind of action that calls the context with no command: a second path in the map.

### Decision 10: The gamepad state crosses as a Core enum, and the game makes the push
**Choice:** `UiGamepadButtons` lives in Core. `GlfwUiInput.PollGamepad(int gamepadId = 0)` returns it. The game writes `map.PushGamepad(input.PollGamepad())` one time in each frame. Notes question 2 holds the open choice.
**Rationale:** `BrutalUi.Glfw` and `BrutalUi.Framework` each reference Core only (section 3 of the technical design), so a shared type must live in Core. `PointerButton` and `UiModifiers` set the precedent: Core holds the input words, with the GLFW values. A return value adds no delegate, no slot, and no project reference. The game decides the map and the time of the push. A8 holds, because the game pushes into the active map.
**Alternatives:** (a) `BrutalUi.Glfw` references `BrutalUi.Framework`, and `PollGamepad(UiActionMap)` pushes: a new package dependency of the adapter. (b) A slot on the adapter that the map fills: one more delegate type in Glfw.

### Decision 11: The sample adds a row and keeps the old rows
**Choice:** `HelloState` builds one row with an `Add` button and a bound score label, at the end of the panel. The smoke run pushes a click on `Add` before the slider click, and an Escape after the Tab in each fourth frame. Line 6 prints three counts.
**Rationale:** A7 asks for one store, one command, and one action on a key. A new class keeps `HelloWidgets` unchanged. The click order keeps the Tab move from the slider to the field, so the caret check of the run still holds. The Escape reaches the map while the field holds the focus, so the run proves the text field rule live. A reset in each fourth frame makes the score text change, so the label binding shows real work.
**Alternatives:** (a) A new row inside `HelloWidgets`: a change of a large class and of its rules. (b) The action and the button on one command: then the counts cannot tell the two paths apart.

### Decision 12: The tests and the gate live in the existing test projects
**Choice:** The Core rule tests live in `tests/BrutalUi.Core.Tests/Input/`, the map tests in a new folder `tests/BrutalUi.Framework.Tests/Actions/`, and the poll tests in `tests/BrutalUi.Glfw.Tests/`. No project file changes.
**Rationale:** A2 forbids a new project. The Core rules need no Framework type, so they test in Core with the Core helpers. The Framework tests copy the private helpers of `WidgetBindingTests` (lesson 7).
**Alternatives:** A shared helper file for the event factories: a project change for about 30 lines.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The slot call changes an existing route, a count of a gate, or the order of the Tab move. | Low | High | Decision 2. The call is one block after the Tab branch, and a `null` slot skips it. Phases 1 and 4 run the full suite, with ADVANCED agents. Each existing test passes with no edit. |
| A typed letter fires an action while a text field holds the focus. | Medium | High | Decision 6 (the text guard). Three guard tests in phase 6, and a W key in the gate scene. |
| A click on a button also fires a mouse action. | Medium | Medium | Decision 1: only a press over no element reaches the slot. `APressOnAnElementDoesNotCallTheSlot` and `AMouseButtonOverAnElementInvokesNothing`. |
| A pickable root covers the window, so no press reaches the map. | Medium | Low | The remarks of `BindMouseButton` state the layout-root rule. HelloPanel uses a root that is not pickable. |
| The shape of `GlfwGamepadState` differs from the plan. | Medium | Low | Task 3.5 reads it first, and the conversion takes a span. The agent tier line of phase 3 escalates. |
| The live GLFW poll allocates in the BRUTAL binding. | Low | Medium | `ALivePollAllocatesNothing` measures it under `xvfb-run`. The sample calls the poll outside the UI region, so a binding allocation cannot fail the smoke gate. Record a finding when it allocates. |
| A command of the slot throws and leaves the ring half drained. | Low | Medium | The `finally` block of the drain keeps the context valid. `ASlotThatThrowsLeavesUpdateAndTheNextUpdateDrainsTheRest` and `ACommandThatThrowsLeavesTheMapUsable`. |
| A held gamepad button fires again after a map switch. | Medium | Low | The remarks of `PushGamepad` state the rule. panels-and-modes can push the state into a new map before it activates it. |
| The new sample row breaks the caret check or the gate check of the smoke run. | Medium | Medium | Decision 11 keeps the focus order. Task 5.5 runs the smoke run, and task 5.6 runs two plants. |
| The new live GLFW test raises the skipped count of the solution. | High | Low | Task 3.8 records the new count. The DoD reads the skipped count from `context.md`. |
| The mosaic-ui names differ from the names of this plan. | Medium | Low | Notes Conflicts and Gaps item 3. A rename before panels-and-modes costs little. |
| commands-and-events is not committed when the feature starts. | Medium | Low | Task 1.1 checks the log and records the base and the counts. |

---

## Dependencies

### Internal Dependencies
- `state-and-commands/commands-and-events` (done, in the working tree at the time of this plan): `CommandRegistry` (`Register`, `Invoke`, `Contains`, `UnknownCommand`), `CommandHandler`, and `CommandBindings.BindCommand` for the sample.
- `state-and-commands/element-bindings` (done): `LabelBindings.BindText` for the sample, and the binding pass that shows a command result in the same `Update`.
- `state-and-commands/store-and-subscription` (done): `Store<int>` for the sample, and the Framework copy of `AllocationGate`.
- `input-routing`: `UiContext.DrainEvents`, the Tab move, `FocusNext`, `FocusPrevious`, `SetFocus`, `FocusedElement`, the key and pointer events, `OnFileDrop` as the slot shape, and `GlfwUiInput`.
- `interactive-elements`: the stop rules of `Button` and `TextField`, and `WidgetInput.IsEditKey`.
- `dev-ops`: `brutal-ui.sln`, `scripts/test-linux.sh`, `scripts/test-windows.ps1`, and the GPU smoke contract of HelloPanel.

### External Dependencies
- `Ahwoo.BRUTAL.Glfw` (pinned in `Directory.Packages.props`): `Glfw.GetGamepadState` and `GlfwGamepadState`. No new package and no native dependency: the GLFW native library ships in the pinned package already.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. A key down that no handler stopped, that is not a Tab move, and that has no target from the caller calls `OnUnhandledInput` one time after its walk. A stopped key, a key up, a Tab move, and a key with a caller target call nothing. A repeat calls the slot with the repeat flag. A focused `TextField` keeps Left, Backspace, and Enter, and passes Escape and Down. A throwing slot leaves `Update`, and the next `Update` drains the rest (`UnhandledInputTests`).
- [ ] F2. A press over no element calls the slot with the button and the modifiers. A press on an element, a release, a move, and a scroll call nothing (`UnhandledInputTests`).
- [ ] F3. An active map calls the command of the action of an unhandled key, with an exact match of Shift, Control, Alt, and Super. Caps Lock and Num Lock do not block a match. A repeat and an inactive map call nothing. An unknown command calls `UnknownCommand` of the registry (`UiActionMapTests`).
- [ ] F4. `NavigateDown` and `NavigateUp` move the focus to the next and the previous element through `FocusCommands`. A second `Activate` replaces the first map, and `Deactivate` clears the slot of its own map only (`FocusActionTests`, `UiActionMapTests`).
- [ ] F5. `PushGamepad` calls the action of each new press one time, in the order of the bind, and calls nothing for a held button. `PollGamepad` converts each GLFW button to its bit, rejects an id outside 0 to 15, returns `None` after `Dispose`, and allocates nothing in a live poll (`GamepadActionTests`, `GlfwUiInputGamepadTests`).
- [ ] F6. A mouse binding fires for a press over no element only, and a key binding and a mouse binding with the same code stay apart (`UiActionMapTests`).
- [ ] F7. While a `TextField` holds the focus, a text key with no Control, Alt, or Super calls no action, and Escape, Down, F1, and Control+S each call their action. The build rules throw, and a command that binds or throws during a press leaves the map usable (`UiActionMapTests`).
- [ ] F8. `ActionMapAllocationTests` measures 0 bytes in Debug for the key, mouse, gamepad, and focus bodies, and its real-work test passes. The four plants of task 7.3 each give a non-zero exit code of `dotnet test`.
- [ ] F9. The HelloPanel smoke run prints `HelloPanel: state: adds <A>, resets <R>, versions <V>.` with three counts above 0 and exits with code 0. A run with no Escape push exits with code 1 and `resets 0`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. The new classes hold these counts: `UnhandledInputTests` 12, `UiActionMapTests` 23, `FocusActionTests` 5, `GamepadActionTests` 7, `ActionMapAllocationTests` 5, and `GlfwUiInputGamepadTests` 6. `BrutalUi.Core.Tests` holds 12 more tests than at `<base>`, `BrutalUi.Framework.Tests` 40 more, and `BrutalUi.Glfw.Tests` 6 more.
- [ ] Q3. Each public member of the new and changed source files has an XML doc comment. The new doc comments and the changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q4. `src/BrutalUi.Framework/Actions/` holds no `lock`, no `IDisposable`, no `event` keyword, no `ToString(` call, and no `(object)` cast in code. `src/BrutalUi.Framework/` and `src/BrutalUi.Core/` name no GLFW type.

### Integration Criteria
- [ ] I1. The only changed files of `src/BrutalUi.Core/` are `Input/UiEventEnums.cs` and `Tree/UiContext.cs`, and the only new file is `Input/UiUnhandledInputHandler.cs`. The only changed file of `src/BrutalUi.Glfw/` is `GlfwUiInput.cs`. The only changed project file is `samples/HelloPanel/HelloPanel.csproj`. No file of `scripts/` and no test file that existed at `<base>` changed.
- [ ] I2. `UiActionMap` calls `CommandRegistry.Invoke` for each action. `FocusCommands` calls `UiContext.FocusNext` and `UiContext.FocusPrevious`. `Activate` writes `UiContext.OnUnhandledInput`.
- [ ] I3. Sections 3, 4, 9, 10, and 14 of `docs/technical-design.md` state the slot, the map, the poll, line 6 of the sample, and the bullet "A seventeenth allocation gate". `CHANGELOG.md` names the new types.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` and the base counts from `docs/features/state-and-commands/action-map/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed, and the skipped count of `context.md` (Q2).
3. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*UnhandledInputTests' 2>&1 | tail -n 5` (12, F1, F2). Run `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5` for `UiActionMapTests` (23, F3, F4, F6, F7), `FocusActionTests` (5, F4), `GamepadActionTests` (7, F5), and `ActionMapAllocationTests` (5, F8). Run `xvfb-run -a dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputGamepadTests' 2>&1 | tail -n 5` (6, F5). Expect `failed: 0` and these totals. Run each of the three projects with no filter and compare the totals with the base counts plus 12, 40, and 6 (Q2). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Framework/Actions/UiActionMap.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `PressKey`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp`. Repeat with the first line of `PushGamepad`, and with the first line inside the slot block of `DrainEvents` in `src/BrutalUi.Core/Tree/UiContext.cs` (F8).
5. Copy `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` to `<scratch>`. Empty the body of the key step. Run the command of step 4. Expect a value that is not 0, with `TheMeasuredBodyDoesRealWork` among the failures. Restore the file, check it with `cmp`, and run step 2 again (F8).
6. `xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"`. Expect six lines, line 6 with three counts above 0, and `exit=0`. Copy `samples/HelloPanel/Program.cs` to `<scratch>`, remove the Escape push of `PushSmokeInput`, and run the command again. Expect `resets 0` and `exit=1`. Restore the file and check it with `cmp` (F9).
7. `grep -rnE '\block\b|IDisposable|\bevent\b|ToString\(|\(object\)' src/BrutalUi.Framework/Actions | grep -v '///'`. Expect no line. `grep -rnE 'Brutal\.GlfwApi|BrutalUi\.Glfw|GlfwKey' src/BrutalUi.Framework src/BrutalUi.Core | grep -v '///'`. Expect no line (Q4).
8. `grep -n 'OnUnhandledInput' src/BrutalUi.Framework/Actions/UiActionMap.cs`, `grep -n 'Commands.Invoke\|\.Invoke(' src/BrutalUi.Framework/Actions/UiActionMap.cs`, and `grep -nE 'FocusNext|FocusPrevious' src/BrutalUi.Framework/Actions/FocusCommands.cs`. Expect at least one line each (I2).
9. `git diff <base> --name-only -- src/ samples/ scripts/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/`. Expect only the files of I1 and the new files of the File Tree. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no output (I1).
10. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4).
11. Read sections 3, 4, 9, 10, and 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I3). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q3).

---

## Testing Strategy

### Unit Tests
- `Core.Tests/Input/UnhandledInputTests` (12): the key rule (no owner, a walk with no stop, a stop, a key up, the Tab move and Control+Tab, a caller target, a repeat, the edit keys of a focused field, a throwing slot) and the press rule (over no element, on an element, a release, a move, and a scroll).
- `Framework.Tests/Actions/UiActionMapTests` (23): the key resolve, the modifier rule, the lock keys, `InvokeAction`, an unknown command, the repeat rule, an inactive map, the one-map rule, the mouse resolve, the kind rule, the text guard, the build rules, the name copy, the re-entry rule, and a throwing command.
- `Framework.Tests/Actions/FocusActionTests` (5): `NavigateDown`, `NavigateUp`, the wrap, the register, and the argument checks.
- `Framework.Tests/Actions/GamepadActionTests` (7): a new press, a held button, a new press after a release, two presses in one push, a D-pad focus move, a push with no `Activate`, and an unbound button.
- `Glfw.Tests/GlfwUiInputGamepadTests` (6): the conversion of each button, a released button, the GLFW indices, the id range, a disposed adapter, and a live poll at 0 bytes.

### Allocation Tests
- `Framework.Tests/Actions/ActionMapAllocationTests` (5, the seventeenth gate): an unhandled key with a button and a field as the owner, a press over no element and a press on a button, a gamepad push with `InvokeAction`, the focus actions, and the real-work check. Four plants guard `PressKey`, `PushGamepad`, the slot call of the drain, and the key body.

### Regression Tests
- Each existing test runs with no edit. A `null` slot skips the new block, so the 16 existing allocation gates keep their scenes and their counts.

### Integration Tests
- The HelloPanel smoke run drives the button command, the Escape action with a focused field, and the bound label in each frame, inside the measured UI region. It prints line 6 and fails on a count of 0.
- `ADpadDownBindingMovesTheFocus` runs the gamepad push, the command, and the focus walk in one call.

### Manual Testing
- Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj` with no argument. Press Escape: the score label shows 0. Click `Add`: the score rises. Press Up and Down: the focus moves. With a gamepad: B resets the score, and the D-pad moves the focus. `/autorun-feature` adds this to the Needs Human Verification list of `context.md`.

---

## Related Documentation

- **Requirements:** `docs/features/state-and-commands/action-map/requirements.md`
- **Epic:** `docs/features/state-and-commands/epic-overview.md`, `docs/features/state-and-commands/epic-requirements.md`
- **Siblings:** `docs/features/state-and-commands/commands-and-events/implementation.md`, `context.md`, and `code-review.md` (the follow-up questions), `docs/features/state-and-commands/element-bindings/context.md` (the Gotchas), `docs/features/state-and-commands/store-and-subscription/context.md`
- **Design:** `docs/technical-design.md` sections 3 (package layout), 4 (frame order), 9 (input and action map), 10 (Framework Layer), and 14 (Testing). `docs/feasibility.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Tree/UiContext.cs` (`DrainEvents`, the Tab move, `OnFileDrop`, `FocusNext`, `FocusPrevious`, `TabKey`), `src/BrutalUi.Core/Input/UiWindowEvents.cs` (a slot delegate), `src/BrutalUi.Core/Input/UiEventEnums.cs` (`PointerButton` and `UiModifiers` with the GLFW values), `src/BrutalUi.Core/Elements/TextField.cs` (`HandleKeyDown`, the stop rule), `src/BrutalUi.Core/Elements/WidgetInput.cs` (the key constants), `src/BrutalUi.Glfw/GlfwUiInput.cs` (the test seam and the remarks), `src/BrutalUi.Framework/Commands/CommandRegistry.cs`, `tests/BrutalUi.Framework.Tests/Commands/CommandAndEventAllocationTests.cs` (the gate shape), `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` (the event helpers), `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs` (the GLFW skip and the collection), and `samples/HelloPanel/Program.cs` (the smoke contract)
- **GLFW reference:** the XML docs of `Brutal.GlfwApi.Glfw.GetGamepadState` in `Ahwoo.BRUTAL.Glfw` 2026.10.4, and https://www.glfw.org/docs/latest/input_guide.html#gamepad
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

This plan is a draft with no interview. Each question below has a recommended option, and the plan uses it.

**Answered 2026-10-29:** the user chose (A) for each of the four questions. The plan stays as written.

1. **The shape of the Core slot and the route of a mouse button.**
   - **(A) Recommended:** One slot `OnUnhandledInput(UiEventType type, int code, UiModifiers modifiers, bool isRepeat)` for a key down that no handler stopped and for a press over no element (Decision 1). One slot, as A6 asks, and the map gets the mouse buttons with no help from the game.
   - (B) A key slot only, `OnUnhandledKey(in KeyEvent e)`, as A6 names it. The map has a public `PressMouseButton`, and the game calls it from its own pointer code after it reads `RoutingGate.PointerOverUi`.
   - (C) Two slots: `OnUnhandledKey` and `OnUnhandledPress`. Typed events, but two slots against "one slot" of A6.
2. **The gamepad seam between `BrutalUi.Glfw` and the map.**
   - **(A) Recommended:** A Core enum `UiGamepadButtons`. `GlfwUiInput.PollGamepad()` returns it, and the game writes `map.PushGamepad(input.PollGamepad())` one time in each frame (Decision 10). No new project reference, and no delegate.
   - (B) `BrutalUi.Glfw` references `BrutalUi.Framework`, and `input.PollGamepad(map)` pushes into the map. A literal reading of A6, but each user of the adapter then gets the Framework package.
   - (C) A slot on the adapter, `input.OnGamepad = map.PushGamepad;`, and `input.PollGamepad()` calls it. The adapter pushes, but Glfw gets one more delegate type.
3. **The text guard for a focused `TextField`.**
   - **(A) Recommended:** The map skips a text key (below 256, or a keypad key from 320 to 336) with no Control, Alt, or Super while a `TextField` holds the focus. Escape, the arrow keys, the function keys, and a shortcut still reach the map (Decision 6).
   - (B) The stop rule only: the field keeps its seven edit keys, and a bound letter key fires its action while the user types.
   - (C) The map skips each key while a `TextField` holds the focus. Escape then cannot close a menu with a field.
4. **The demo of phase 1 (Slice rule 12).**
   - **(A) Recommended:** `**Demo:** a developer runs dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests' and reads a total of 8 with failed: 0. The first test pushes an Escape key down with no focus owner, runs Update, and reads one call of the command of Cancel.`
   - (B) `**Demo:** a developer runs dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*UnhandledInputTests' and reads a total of 9 with failed: 0.` (the Core slot only, with no map)
   - (C) `**Demo:** a developer runs HelloPanel, presses Escape, and sees the score label show 0.` (the sample moves into phase 1, and phase 1 grows past two task groups)

### Conflicts and Gaps

1. **A6 names "a key".** The recommended slot also carries a press over no element (question 1). A6 does not forbid it, and it keeps one slot. Option (B) of question 1 follows the words of A6 exactly.
2. **A6 says that the adapter "pushes the state into the map".** `BrutalUi.Glfw` cannot name `UiActionMap` with no new project reference. The recommended seam has the adapter read the state and the game make the push in one line (question 2). Option (B) follows the words of A6 exactly.
3. **The mosaic-ui names.** Conflicts and Gaps item 1 of the commands-and-events plan asks the user to confirm the mosaic-ui names before action-map starts. No answer exists, and the source is still not available. This plan uses the words of the requirement as UTF-8 action names (`Confirm`, `Cancel`, `NavigateUp`, `NavigateDown`), the prefix `ui.` for the focus commands, and the type name `UiActionMap` of the technical design. Recommendation: the user confirms the names or gives the mosaic-ui source before panels-and-modes starts. A rename costs little.
4. **The follow-up questions of the commands-and-events review.** Two panels that register the same command name, and a release of each command of one target, stay open. This plan keeps the defaults: unique names, and the caller keeps its names. The map adds no rule for them. `FocusCommands.Register` throws on a second register, because the registry throws for a duplicate name.
5. **A project file change.** HelloPanel needs `BrutalUi.Framework` for the store, the command, and the map (A7). Task 5.1 adds one project reference. The commands-and-events plan changed no project file. This plan changes one.
6. **The repeat rule.** The map ignores a key repeat, so a held arrow key moves the focus one time. The Tab move of Core repeats. A repeat rule for the focus actions waits for a need (Decision 5).
7. **"A store that drives a label" in the smoke count.** The run counts the store versions, not the label writes. The label binding has its own tests and the fifteenth gate. The reset in each fourth frame makes the label text change in the run.
8. **The shape of `GlfwGamepadState`.** The planner had no shell and could not read the struct members in the binding. The XML docs give `Glfw.GetGamepadState(int, ref or out GlfwGamepadState)` and button values of `GlfwButtonAction`. Task 3.5 reads the shape before any code.
9. **One gamepad.** `PollGamepad` reads one gamepad id, 0 by default. Two players, a hot-plug event, an axis, and a deadzone are out of scope.
10. **The base commit.** HEAD at the time of the plan is `60c6950`, and the files of commands-and-events are not committed yet. Task 1.1 records the base after that commit.
11. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 7.7 runs it on this plan and on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
