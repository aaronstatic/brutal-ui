# Input Actions - Implementation Plan

**Epic:** input (feature 3 of 5)
**Status:** Ready for Review
**Started:** 2026-11-09
**Target Completion:** TBD
**Last Updated:** 2026-11-09 17:50

---

## Quality Bar

**Target quality:** A framework input API that a game reads in each frame. spacegame map-controls pans the map with W, A, S, D and a stick, zooms with the scroll and the triggers, and drags with a mouse button. control-schemes tags each binding of this list, and binding-remap writes one binding to one text line. A wrong value rule moves a camera the wrong way. A second invoke of one press runs a command two times. A hidden allocation fails the HelloPanel gate in each frame.

- **Correct value rules:** Each bind kind has one read rule, and a test on hand-made input pins each rule: the key composite of four keys and of two keys, the sum and the clamp to length 1, the opposite keys that give 0, the stick with up positive, the stick axis and the trigger with a scale, the mouse delta and the scroll with no clamp, the gate, and the Button latch. A plant fails each rule test.
- **One press, one command:** A press of a key, a mouse button, or a gamepad button invokes the command one time, and `WasPressed` reads `true` in that same frame only. The frame read never invokes a command for a key or a mouse binding, so the slot path and the frame read never both invoke one press. Tests pin the agreement for each of the three button kinds.
- **Clear exceptions at build time:** A Button bind on an Axis or Vector2 action, and a value bind on the wrong type, throw `ArgumentException` with the parameter name `action`. A query of the wrong type throws the same. A rejected call changes no count. A name that the map does not hold reads `false` or zero.
- **No stale flag across a switch:** A mode switch mid-hold gives no held flag and no released flag in the next map. A replaced map and a deactivated map read `false` and zero.
- **0 bytes after warm-up:** The frame read, the invoke pass, and each query allocate 0 bytes in the default Debug build. `ActionMapAllocationTests` grows to measure them, and a plant fails it. The new Core slot allocates 0 bytes, and a Core test measures it.
- **A small named diff:** Core gets one delegate file and one slot with one call in `UiContext.Update`. `UiInputState.cs`, `InputStateWriter.cs`, and each Glfw file do not change. `UiManager.cs` does not change. `UiActionMap` changes in place and splits into three partial files.
- **Accurate docs:** Section 9 and section 4 of the technical design state the typed map, the read slot, and the frame order. Each public member has an XML doc comment in ASD-STE100 prose. The class remarks of `UiActionMap` keep the style of named rules.

---

## Executive Summary

This feature turns `UiActionMap` into a typed action map. An action is a Button, an Axis, or a Vector2. The map reads `UiContext.GameInput` one time in each `Update`, after the drain, through a new Core slot `OnInputRead`. It stores the value of each action for the frame, and the game reads the values with five queries by action name.

- Core gets `UiInputReadHandler` and the slot `UiContext.OnInputRead`. `Update` calls the slot right after the drain and before the binding pass.
- Framework gets the public enums `UiActionType` (Button, Axis, Vector2) and `UiGamepadStick` (Left, Right).
- `UiActionMap` gets `AddAction(action, type)`, six new binds (`BindKeyVector`, `BindKeyAxis`, `BindGamepadStick`, `BindGamepadAxis`, `BindMouseDelta`, `BindScroll`), `GamepadSlot`, and the queries `WasPressed`, `IsHeld`, `WasReleased`, `ReadAxis`, and `ReadVector`. `AddAction(action, command)`, the three old binds, `PushGamepad`, `PressKey`, `PressMouseButton`, `InvokeAction`, `Activate`, `IsActive`, and `Deactivate` keep their signatures.
- `Activate` writes both slots, and `Deactivate` clears both slots and resets the frame values. An active map invokes the command of a gamepad button from `Gamepad(GamepadSlot).Pressed` in the frame read (Q4), so HelloPanel drops its `PushGamepad` call.
- HelloPanel adds a `Move` Vector2 action with W, A, S, D and the left stick, and shows `ReadVector` in a fifth label.

The plan has 5 phases and 41 tasks, about 290 steps. Phase 1 (12 tasks, ADVANCED) delivers the Core slot, the action type, the four-key composite, and `ReadVector`. Phase 2 (9 tasks) delivers the gamepad values and the gamepad press. Phase 3 (7 tasks) delivers the mouse delta, the scroll, the two-key composite, and the Button queries. Phase 4 (5 tasks, ADVANCED) delivers the switch rule and the allocation gate. Phase 5 (8 tasks) delivers the HelloPanel label, the docs, and the changelog.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The game developer** (builds a map at build time with typed actions, and reads `map.ReadVector("Move"u8)` after `Update` with no per-device code)
- [ ] **The spacegame map-controls consumer** (pans the map with W, A, S, D and a stick, zooms with the scroll and the triggers, and reads a drag button with `IsHeld`)
- [ ] **The sibling features** (control-schemes adds a scheme name to each binding of the list, and binding-remap writes and reads one binding for each text line: a kind, the codes, the modifiers, a scale, and the action)

**User Subsets Served:**
- [ ] **Simple** - a game adds one Vector2 action, binds W, A, S, D, and reads (0, 1) when W is down.
- [ ] **Complex** - a game binds a stick and a key composite to one action, reads gamepad slot 2, mixes a trigger and the scroll into one zoom axis, and switches maps through modes with a key held.

**UX Approach:**
- Build time: `AddAction`, then the binds. A wrong bind throws at once with the parameter name.
- Frame time: `Glfw.PollEvents(); input.PollGamepads(); ui.Update(dt);` and then the queries. No call to the map in the frame loop before `Update`.
- No event, no builder, no handle, and no allocation in the frame path.

---

## Goals

### Primary Goals
1. Add the Core slot `OnInputRead` and call it after the drain (Q1).
2. Add the action type and the typed binds, with the type checks at build time (requirements lines 12 to 14).
3. Read `GameInput` in the frame read, sum and clamp the values, and keep the Button flags (A1, A2, A3, Q3).
4. Invoke a gamepad command from the frame read, and keep one command and one press flag for each press (Q4, A4).
5. Add the five queries by UTF-8 name (Q5, A5).
6. Reset each frame value at a switch, so the next map gets no stale flag (A6).
7. Prove 0 bytes for the frame read and the queries (requirements line 21).
8. Show the `Move` vector in HelloPanel, and state the rules in section 9, section 4, section 14, and the changelog (A8, A9 of the epic).

### Success Criteria
- [ ] The phase 1 demo passes: `TypedActionMapTests` reads a total of 9 with `failed: 0`.
- [ ] The phase 2 demo passes: `GamepadValueActionTests` reads a total of 8 with `failed: 0`.
- [ ] The phase 3 demo passes: `ActionQueryTests` reads a total of 10 with `failed: 0`.
- [ ] The phase 4 demo passes: `ActionMapSwitchTests` reads 6 and `ActionMapAllocationTests` reads 6, each with `failed: 0`.
- [ ] The phase 5 demo passes on a host with a window: W reads `Move: 0.00, 1.00`.
- [ ] Each existing test passes. The one edited existing test class is `ActionMapAllocationTests` (the scene and `TheMeasuredBodyDoesRealWork`).

---

## Architecture Overview

### The Place in the Epic

input-state gives `GameInput`, the edges, and the gate. gamepad-axes fills the 16 slots with values after the deadzone. This feature reads both in one loop. control-schemes and binding-remap read the binding list of this feature.

```
Frame N (0 bytes after warm-up)
  Glfw.PollEvents()
  input.PollGamepads()                         gamepad-axes: SetGamepad x16, edges in Core
  ui.Update(dt)
    OnBeforeUpdate                             UiManager: the mode diff, map.Activate (unchanged)
    DrainEvents()                              input-state: raw state, then GameInput at the end
      OnUnhandledInput (per entry)             UiActionMap.HandleInput: key and mouse commands (unchanged)
    OnInputRead(this)                          NEW (phase 1): UiActionMap.ReadFrame
      read GameInput, fill each action         values, Button flags (phases 1 to 3)
      invoke the gamepad press commands        Q4 (phase 2)
    UpdateBindings, styles, transitions, layout, caret, clear
  the game reads map.ReadVector("Move"u8), map.IsHeld("Fire"u8), ...
```

### File Tree (end state)

```
src/BrutalUi.Core/Input/
└── UiInputReadHandler.cs              # phase 1 (new): the delegate type of the slot
src/BrutalUi.Core/Tree/
└── UiContext.cs                       # phase 1: OnInputRead, the call in Update, the re-entry flag, the remarks
src/BrutalUi.Framework/Actions/
├── UiActionEnums.cs                   # phase 1 (new): UiActionType. phase 2: UiGamepadStick
├── UiActionMap.cs                     # phase 1: split. remarks, fields, queries, Activate, Deactivate, private types
├── UiActionMap.Bindings.cs            # phase 1 (new, split): AddAction, each bind, Find, IndexOfAction
└── UiActionMap.Read.cs                # phase 1 (new, split): HandleInput, ReadFrame, PushGamepad, the resolves, Invoke
samples/HelloPanel/
├── HelloState.cs                      # phase 5: the Move action
├── HelloScreen.cs                     # phase 5: the move label, MoveText
├── HelloModes.cs                      # phase 5: MoveLabel
└── Program.cs                         # phase 2: drop PushGamepad. phase 5: the move label write
tests/BrutalUi.Core.Tests/Input/
└── InputReadTests.cs                  # phase 1 (new): 6 tests
tests/BrutalUi.Framework.Tests/Actions/
├── TypedActionMapTests.cs             # phase 1 (new): 9 tests
├── GamepadValueActionTests.cs         # phase 2 (new): 8 tests
├── ActionQueryTests.cs                # phase 3 (new): 10 tests
├── ActionMapSwitchTests.cs            # phase 4 (new): 6 tests
└── ActionMapAllocationTests.cs        # phase 4: the scene, QueryStep, one new test, TheMeasuredBodyDoesRealWork
docs/technical-design.md               # phase 5: section 4, section 9, section 14
CHANGELOG.md                           # phase 5
docs/features/input/input-actions/context.md   # each phase
```

These files do not change: `UiInputState.cs`, `InputStateWriter.cs`, `UiGamepadState.cs`, `UiGamepadDeadzone.cs`, `UiEventEnums.cs`, `UiUnhandledInputHandler.cs`, each file of `src/BrutalUi.Glfw/`, `src/BrutalUi.Framework/Panels/UiManager.cs`, `UiActions.cs`, `UiActionMapTests.cs`, `GamepadActionTests.cs`, `FocusActionTests.cs`, each test of `tests/BrutalUi.Framework.Tests/Panels/`, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, and `scripts/`. One exception stands: `src/BrutalUi.Framework/Actions/UiActions.cs` holds one doc comment line for the cref split of the two `AddAction` overloads. No code line changed there (deviation decision 10 in `context.md`).

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public delegate void UiInputReadHandler(UiContext context);    // UiInputReadHandler.cs

public sealed class UiContext
{
    public UiInputReadHandler? OnInputRead { get; set; }       // Update calls it after the drain, before the binding pass
}
```

```csharp
namespace BrutalUi.Framework;

public enum UiActionType : byte { Button = 0, Axis = 1, Vector2 = 2 }
public enum UiGamepadStick : byte { Left = 0, Right = 1 }

public sealed partial class UiActionMap
{
    // existing and unchanged signatures
    public void AddAction(ReadOnlySpan<byte> action, ReadOnlySpan<byte> command);     // a Button with a command
    public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers = UiModifiers.None);   // Button only
    public void BindMouseButton(ReadOnlySpan<byte> action, PointerButton button, UiModifiers modifiers = UiModifiers.None);  // Button only
    public void BindGamepadButton(ReadOnlySpan<byte> action, UiGamepadButtons button);  // Button only
    // PushGamepad, InvokeAction, PressKey, PressMouseButton, Activate, IsActive, Deactivate, Commands,
    // ActionCount, BindingCount, CreateInfo: unchanged signatures

    // new
    public void AddAction(ReadOnlySpan<byte> action, UiActionType type);              // no command
    public void BindKeyVector(ReadOnlySpan<byte> action, int up, int left, int down, int right);   // Vector2
    public void BindKeyAxis(ReadOnlySpan<byte> action, int negative, int positive);   // Axis
    public void BindGamepadStick(ReadOnlySpan<byte> action, UiGamepadStick stick);    // Vector2
    public void BindGamepadAxis(ReadOnlySpan<byte> action, UiGamepadAxis axis, float scale = 1f);  // Axis
    public void BindMouseDelta(ReadOnlySpan<byte> action, float scale = 1f);          // Vector2
    public void BindScroll(ReadOnlySpan<byte> action, float scale = 1f);              // Axis
    public int GamepadSlot { get; set; }                                              // 0 to 15, default 0
    public bool WasPressed(ReadOnlySpan<byte> action);                                // Button
    public bool IsHeld(ReadOnlySpan<byte> action);                                    // Button
    public bool WasReleased(ReadOnlySpan<byte> action);                               // Button
    public float ReadAxis(ReadOnlySpan<byte> action);                                 // Axis
    public float2 ReadVector(ReadOnlySpan<byte> action);                              // Vector2
}
```

### The Value Rules

`input` is `context.GameInput`. `pad` is `input.Gamepad(GamepadSlot)`. `mods` is `input.Keys.Modifiers & MatchMask`. `Down(k)` is 1 when `input.Keys.IsDown(k)` and 0 otherwise.

| Bind | Action type | Read in each frame | Group |
|---|---|---|---|
| `BindKey` | Button | when `mods == Modifiers`: down `Keys.IsDown(code)`, press `Keys.WasPressed(code)` | flags |
| `BindMouseButton` | Button | when `mods == Modifiers`: down `Buttons.IsDown`, press `Buttons.WasPressed` | flags |
| `BindGamepadButton` | Button | down `pad.Buttons & bit`, press `pad.Pressed & bit`. The press invokes the command (Q4) | flags |
| `BindGamepadStick` | Vector2 | `(s.X, -s.Y)` of `pad.LeftStick` or `pad.RightStick` | clamped |
| `BindGamepadAxis` | Axis | `pad.Axis(axis) * scale`, negated for `LeftStickY` and `RightStickY` | clamped |
| `BindKeyAxis` | Axis | `Down(positive) - Down(negative)` | clamped |
| `BindKeyVector` | Vector2 | `(Down(right) - Down(left), Down(up) - Down(down))` | clamped |
| `BindMouseDelta` | Vector2 | `input.Pointer.Delta * scale` | free |
| `BindScroll` | Axis | `input.Pointer.ScrollDelta.Y * scale` | free |

The finish of each action, after the loop over the bindings:

```
Axis:     value = (Clamp(clampedSum.X, -1, 1) + freeSum.X, 0)
Vector2:  value = ClampLength(clampedSum, 1) + freeSum               // ClampLength scales by 1 / len when len > 1
Button:   start    = oldHeld || press                                // oldHeld is Held of the last frame read
          Pressed  = press
          Held     = start && down
          Released = start && !Held
```

Worked values for the tests: W gives (0, 1). W and D give (0.707107, 0.707107). W and S give (0, 0). The stick (0.5, -0.25) gives (0.5, 0.25). The stick (1, 0) and W give (0.707107, 0.707107). The stick (0, -1) and W give (0, 1). The right trigger 0.75 with scale 2 gives 1. The left stick Y -0.5 gives 0.5. A pointer move from (100, 100) to (140, 70) with scale 0.5 gives (20, -15). A scroll of (0, 3) with scale 2 gives 6. The right trigger 1 and a scroll of 3 in one Axis give 4.

### The Frame Read

```
ReadFrame(UiContext context)                         the OnInputRead handler, created one time in the constructor
  ref readonly UiInputState input = ref context.GameInput
  ref readonly UiGamepadState pad = ref input.Gamepad(_gamepadSlot)
  copy _actions, _actionCount, _bindings, _bindingCount to locals                (the re-entry rule)
  for each action: Sum = (0, 0), Free = (0, 0), Down = false, Press = false
  for each binding, in the order of the bind: add its read to its action        (the table above, one switch by kind)
  for each action: the finish                                                     (the formulas above)
  when pad.Pressed is not None (phase 2):
    for each GamepadButton binding with (pad.Pressed & Code) != 0, in the order of the bind:
      stop when context.OnInputRead is not _onRead                              (a command deactivated or replaced the map)
      Invoke(binding.Action)                                                     (an action with no command invokes nothing)
```

The loop reads `GameInput` only (A1). The finish runs before the invoke pass, so a command that throws leaves the values of the frame in the map.

### Internal Shape

- **The partial files (phase 1).** `public sealed partial class UiActionMap` in three files. `UiActionMap.cs` holds the class remarks, the constants, the fields, the constructor, `Commands`, the counts, `GamepadSlot`, the queries, `Activate`, `IsActive`, `Deactivate`, the reset, `InputKind`, `ActionEntry`, `InputBinding`, and `CreateInfo`. `UiActionMap.Bindings.cs` holds both `AddAction` overloads, each bind, `Find`, the value find, `IndexOfAction`, the type check, and the append. `UiActionMap.Read.cs` holds `HandleInput`, `IsTypedByTextField`, `ReadFrame`, `PushGamepad`, `InvokeAction`, `PressKey`, `PressMouseButton`, and `Invoke`.
- **`InputKind` (private).** `Key`, `MouseButton`, `GamepadButton` stay with their values. New: `GamepadStick`, `GamepadAxis`, `KeyAxis`, `KeyVector`, `MouseDelta`, `Scroll`. One `GamepadAxis` kind covers the trigger and the stick axis, because `UiGamepadAxis` names both.
- **`InputBinding` (private).** `Kind`, `Code`, `Code1`, `Code2`, `Code3`, `Modifiers`, `Scale`, `Action`. `Code` holds the key, the button, the bit, the stick, or the axis. A `KeyAxis` holds the negative key in `Code` and the positive key in `Code1`. A `KeyVector` holds up, left, down, and right in `Code` to `Code3`. An unused code is -1. Each old bind writes `Scale = 1`. The shape gives one text line for each binding (binding-remap).
- **`ActionEntry` (private).** `Name`, `Command` (an empty array for an action with no command), `Type`, `Value` (`float2`), `Pressed`, `Held`, `Released`, and the frame scratch `Sum`, `Free`, `Down`, `Press`. The read writes each entry through `ref ActionEntry a = ref actions[i]`.
- **The type check.** A private helper takes the action index and the type that the bind needs. It throws `ArgumentException(message, "action")` on a mismatch. Each bind checks the arguments first (`ArgumentOutOfRangeException`), then the action name, then the type, then a duplicate. A rejected call changes no count.
- **The slot handlers.** `_onInput` stays. `_onRead = ReadFrame` is new, with the same build-time rule.
- **The queries.** Each query finds the index with `IndexOfAction`. A name that the map does not hold returns `false` or zero. A wrong type throws `ArgumentException` with the parameter name `action`, also on a map that is not active. From phase 4, a map that is not active returns `false` or zero.
- **`float2` math.** The code uses `new float2(x, y)`, `.X`, `.Y`, `+`, and `MathF.Sqrt`. Each float operation works on the components.

### Integration Points

| Sibling or caller | What this feature calls or gives | Contract |
|---|---|---|
| input-state (`GameInput`, done) | the read of keys, buttons, pointer, and pads | The read runs after the end of the drain, so the gate of this frame applies. The gamepad part has no gate. `UiInputState.cs` and `InputStateWriter.cs` do not change. |
| gamepad-axes (`PollGamepads`, `UiGamepadAxis`, done) | `LeftStick`, `RightStick`, `Axis(UiGamepadAxis)` | The values are after the deadzone. The map adds no deadzone. The map flips the GLFW Y of a stick read, so up is positive (Decision 6). |
| Core `UiContext.Update` | the new slot `OnInputRead` | One call after `DrainEvents` and before `UpdateBindings`. A null slot does nothing. A call of `Update` from the handler throws. |
| state-and-commands (`OnUnhandledInput`, `PushGamepad`, done) | unchanged slot path, unchanged `PushGamepad` | The slot path keeps the repeat rule and the text guard. `PushGamepad` keeps its body. An active map also reads the pad, so a game that keeps `PushGamepad` on an active map gets two invokes for each press (Decision 9). |
| panels-and-modes (`UiManager`, done) | `Activate` and `Deactivate` only | The diff calls `Activate` on the map of the new mode, and `Deactivate` only when the new mode has no map. The switch rule covers both paths (Decision 11). `UiManager.cs` does not change. |
| control-schemes, binding-remap (later) | the binding list | Each binding holds a kind, up to four codes, the modifiers, a scale, and an action index. A scheme name and a text line fit this shape. |
| HelloPanel (sample) | the frame read, `ReadVector` | The read runs inside `Update`, inside the UI region, so the sample gate of 0 bytes covers it. |

---

## Implementation Phases

The feature has 41 tasks, so it has five slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea: the Core slot, one Vector2 action, one composite, and one query, checked headless (Q6). Phase 2 adds the gamepad, which the spacegame camera needs first. Phase 3 adds the other binds and the Button queries. Phase 4 adds the switch rule and the allocation gate. Phase 5 adds the sample and the docs.

Each verification command sets `MSBUILDDISABLENODEREUSE=1`. Give `-nodeReuse:false` to `dotnet build` only. On `dotnet test` it breaks the host handshake (Gotcha 1 of input-state). Do not run `dotnet build-server shutdown`. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `Brutal.Numerics`, `BrutalUi.Core`), also for each namespace that a `cref` names. Run one Core class with `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, and one Framework class with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, after a build.

Two flakes are known. A test host can exit with 134 (Internal CLR error 0x80131506), and the Core test `TheCurvesAllocateNothing` failed one time with 8128 bytes. Run the tests a second time before you treat either as a fault. A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant. For each plant, copy the file to `src/<Project>/obj/plantscratch/` (git-ignored, Gotcha 3 of input-state), run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`, expect a value that is not 0, read the failed test names from the log, restore the file with `cp`, check it with `cmp`, and run the tests again. A plant line `GC.KeepAlive(new object());` needs `using System;` in its file. Keep the plant text word for word (Decision 27 of input-state).

The tests follow the style of `UiActionMapTests`: a static handler in a `static readonly` field, a `Counter` target, events built by hand (the Core event factories are internal), a context with a `RecordingRenderer` measurer and a viewport of 400 by 300, and a root that is not pickable for the mouse tests. A test writes a pad with `context.SetGamepad(0, new UiGamepadState(buttons, axes))` before `Update`. Float asserts use `Assert.Equal(expected, actual, 5)` for each component. A test compares no `float2` value with `==`. The GLFW key codes: W 87, A 65, S 83, D 68, Space 32, Escape 256, Up 265, Down 264.

### Phase 1: The Read Slot and the Vector2 Composite
**Goal:** Core holds the slot `OnInputRead`, and `Update` calls it after the drain. `UiActionMap` holds the action type, the four-key composite, the frame read of `GameInput` through the slot, and `ReadVector`. `Activate` writes the slot and `Deactivate` clears it.
**Demo:** a developer runs the Framework test binary with a filter on the typed map tests and reads a total with `failed: 0`. The tests push W on a W,A,S,D Vector2 and read (0, 1) through the new Core slot.
**Needs:** none
**Agent tier:** ADVANCED (opus, at the preset effort). The phase changes `UiContext.Update` and the order around the drain. A wrong place of the call reads the state of the last frame or skips the gate.

**Tasks (group 1, Core):**
- [ ] 1.1 Create `docs/features/input/input-actions/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`), the solution total with the skipped count, and the totals of `BrutalUi.Core.Tests` and `BrutalUi.Framework.Tests`. Record the Value Rules, the Frame Read, and the Internal Shape of this plan.
- [ ] 1.2 Write `src/BrutalUi.Core/Input/UiInputReadHandler.cs` with `public delegate void UiInputReadHandler(UiContext context);`, in the style of `UiBeforeUpdateHandler.cs`. The summary: a handler that reads the input state of the frame. `Update` calls it after the drain. The remarks: Q1 of input-actions (the seam of the typed action map, Core names no type of that layer), the frame rule (the handler reads `context.GameInput` and `context.Input` with the values of this frame, after the gate), the order rule (after the before-update slot and the drain, before the binding pass, so a store write reaches the binding pass of the same call), the re-entry rule (a call of `Update` from the handler throws `InvalidOperationException`), the exception rule (an exception leaves `Update` before the binding pass, the state of the frame stays, and the next `Update` calls the handler again), and the build-time rule. The parameter doc: the context that calls the handler.
- [ ] 1.3 Edit `src/BrutalUi.Core/Tree/UiContext.cs`. Add `public UiInputReadHandler? OnInputRead { get; set; }` right after `OnBeforeUpdate` (about line 1415), with remarks in the style of `OnUnhandledInput`: Q1 of input-actions, `UiActionMap.Activate` writes the slot, a write with `=` replaces the delegate, do not use `+=` (the map reads the slot to know if it is active), and a null slot does nothing. Add a private flag `_inputRead` next to `_beforeUpdate`. In `Update`, add a check after the before-update check: `if (_inputRead) throw new InvalidOperationException("An input-read handler must not call Update.");`. Right after `DrainEvents();` and before `UpdateBindings();`, read the slot to a local, and when it is not null, set the flag, call `inputRead(this)` in `try`, and clear the flag in `finally`. Add a comment that names Q1 of input-actions. In the remarks of `Update`, add one paragraph for the read slot, change the frame order sentence to "the input pass, the read slot, the binding pass, ...", and add one `<exception>` line for a call from the read slot. Change nothing in `DrainEvents`.
- [ ] 1.4 Write `tests/BrutalUi.Core.Tests/Input/InputReadTests.cs` with 6 tests. `TheSlotRunsOneTimeAfterTheDrain` (push a key down of W with no target, `Update`: the handler runs one time, gets the same context, and reads `context.GameInput.Keys.WasPressed(87)` as `true` inside the call. A second `Update` with no event runs it again and reads `false`). `TheSlotRunsAfterTheDrainSlotAndTheBeforeUpdateSlot` (three handlers write 0, 1, and 2 into a log: `OnBeforeUpdate`, `OnUnhandledInput` for the W key, and `OnInputRead`. One `Update` gives the log 0, 1, 2). `ANullSlotDoesNothing` (`Update` with no slot works. A slot set to `null` after one call gets no second call). `AnUpdateFromTheSlotThrows` (the handler calls `context.Update(0f)`. `Update` throws `InvalidOperationException`. A next `Update` with a counting handler works, so the flag cleared). `AnExceptionOfTheSlotLeavesUpdate` (the handler throws a test exception. `Update` throws it. `GameInput.Keys.WasPressed(87)` reads `true` after the throw. The next `Update` calls the handler again). `TheSlotAllocatesNothing` (a static handler in a `static readonly` field adds to a counter. 2 warm-up updates, `AllocationGate.ReleaseAllocationContext()`, 100 updates, `Assert.Equal(0L, after - before)`, then assert the counter).
- [ ] 1.5 Run the build, the tests, and `InputReadTests` (total 6). Check that `BeforeUpdateTests` and `InputStateTests` pass with no edit. Run two plants. Plant 1: `Update` calls the read slot before `DrainEvents();`. Expect `TheSlotRunsOneTimeAfterTheDrain` to fail. Plant 2: `GC.KeepAlive(new object());` right before the slot call in `Update`. Expect `TheSlotAllocatesNothing` to fail. Record the counts and the failed tests in `context.md`.

**Tasks (group 2, Framework):**
- [ ] 1.6 Split `src/BrutalUi.Framework/Actions/UiActionMap.cs` into the three partial files of the Internal Shape with no change of behavior. Move each member as it is, with its doc comment. The class remarks stay in `UiActionMap.cs`. Run the build and the Framework tests, and check the same totals as task 1.1.
- [ ] 1.7 Write `src/BrutalUi.Framework/Actions/UiActionEnums.cs` with `public enum UiActionType : byte` (Button 0, Axis 1, Vector2 2), with a doc comment for each member that names its queries. Extend `ActionEntry` and `InputBinding` as the Internal Shape states, and add the new `InputKind` members `KeyVector` (this phase) with a doc comment. Add `AddAction(ReadOnlySpan<byte> action, UiActionType type)`: an undefined type throws `ArgumentOutOfRangeException` with the parameter name `type`, an empty name and a held name throw as today, and the entry gets an empty command. `AddAction(action, command)` writes `Type = UiActionType.Button`. `Invoke` returns `false` for an empty command and makes no registry call, so a Button with no command never calls `UnknownCommand`. Update the remarks of `InvokeAction`, `PressKey`, and `PressMouseButton` for an action with no command.
- [ ] 1.8 Add the type check to `BindKey`, `BindMouseButton`, and `BindGamepadButton`: an action that is not a Button throws `ArgumentException` with the parameter name `action`. Add `BindKeyVector(action, up, left, down, right)`: each key outside 0 to `UiKeyState.KeyCount - 1` throws `ArgumentOutOfRangeException` with the parameter name of that key, two equal keys throw `ArgumentException` with the parameter name of the later key, an action that is not a Vector2 throws, and the same four keys bound already throw. The binding stores no modifiers. The remarks: the direction of each key, up is +Y (Decision 6), two opposite keys give 0, the composite reads the keys with no modifier check (Decision 7), and the sum and the clamp to length 1.
- [ ] 1.9 Add `_onRead = ReadFrame` in the constructor. `Activate` writes `context.OnInputRead = _onRead` next to the old slot, and on a move to a new context it clears `OnInputRead` of the old context when that slot holds `_onRead`. `Deactivate` clears `OnInputRead` of its context when the slot holds `_onRead`, also when another map replaced the unhandled slot. Write `ReadFrame(UiContext context)` in `UiActionMap.Read.cs` with the loop of "The Frame Read", with the `KeyVector` case and the Vector2 finish. Update the slot rule of the class remarks: `Activate` writes two slots, and `Deactivate` clears two slots.
- [ ] 1.10 Add `public float2 ReadVector(ReadOnlySpan<byte> action)` in `UiActionMap.cs`. A name that the map does not hold gives (0, 0). An action that is not a Vector2 throws `ArgumentException` with the parameter name `action`. The remarks: the value of the last frame read, fixed until the next `Update`, and 0 bytes. Add the type rule and the value rule to the class remarks.
- [ ] 1.11 Write `tests/BrutalUi.Framework.Tests/Actions/TypedActionMapTests.cs` with 9 tests. `WOnAWasdVectorReadsZeroOne` (the demo: `Move` Vector2, `BindKeyVector(Move, 87, 65, 83, 68)`, `Activate`, `OnInputRead` is not null, a key down of W, `Update`, `ReadVector` gives (0, 1)). `EachKeyOfTheCompositeGivesItsDirection` (A gives (-1, 0), S gives (0, -1), D gives (1, 0), W and D give (0.707107, 0.707107), W and S give (0, 0), W, A, and S give (-1, 0)). `AKeyUpEndsTheValue` (W down gives (0, 1), an `Update` with no event keeps (0, 1), W up gives (0, 0)). `TheGateEmptiesTheVector` (a focusable `Button` holds the focus, W down gives (0, 0). With no focus owner, Shift+W gives (0, 1)). `TheBuildChecksTheActionType` (`BindKey`, `BindMouseButton`, and `BindGamepadButton` on a Vector2 action, and `BindKeyVector` on a Button action, throw `ArgumentException` with `ParamName` `action`. `AddAction(name, (UiActionType)3)` throws `ArgumentOutOfRangeException` with `ParamName` `type`. `AddAction(Move, UiActionType.Vector2)` a second time throws. `ActionCount` and `BindingCount` do not change). `BindKeyVectorChecksTheKeys` (-1 and 349 throw `ArgumentOutOfRangeException` with `ParamName` `up`, `left`, `down`, or `right`. W, A, W, D throws `ArgumentException` with `ParamName` `down`. The same four keys a second time throw. The count does not change). `ReadVectorChecksTheType` (on a Button action it throws `ArgumentException` with `ParamName` `action`. An unknown name gives (0, 0)). `ActivateWritesTheReadSlotAndDeactivateClearsIt` (after `Activate`, the slot is not null. `Activate` on a second context clears the slot of the first context and writes the second. A second map that activates on the context replaces the slot, and `Deactivate` of the first map leaves it. `Deactivate` of the second map clears it). `ACommandlessButtonInvokesNothing` (`AddAction(Fire, UiActionType.Button)`, `BindKey(Fire, 32)`, `PressKey(32, None)` gives `false`, `InvokeAction(Fire)` gives `false`, and a counting `UnknownCommand` handler of the registry reads 0).
- [ ] 1.12 Run the build, the tests, and the demo: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*TypedActionMapTests' 2>&1 | tail -n 5`. Expect a total of 9 with `failed: 0`. Check that `UiActionMapTests`, `GamepadActionTests`, `FocusActionTests`, `ActionMapAllocationTests`, and `ModeActionMapTests` pass with no edit. Run three plants. Plant 3: `ReadFrame` skips the clamp of the Vector2 finish. Expect `EachKeyOfTheCompositeGivesItsDirection` to fail. Plant 4: `ReadFrame` reads `context.Input` in place of `context.GameInput`. Expect `TheGateEmptiesTheVector` to fail. Plant 5: `Deactivate` leaves `OnInputRead`. Expect `ActivateWritesTheReadSlotAndDeactivateClearsIt` to fail. Record the counts, the demo, and the failed tests in `context.md`.

**Estimated Steps:** about 85 (12 tasks, about seven steps for one task)

**Files (group 1, 4):** `context.md`, `UiInputReadHandler.cs`, `UiContext.cs`, `InputReadTests.cs`
**Files (group 2, 6):** `UiActionMap.cs`, `UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, `UiActionEnums.cs`, `TypedActionMapTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `InputReadTests` and the 9 tests of `TypedActionMapTests` pass. Each existing test passes with no edit.
- [ ] The five plants give a non-zero exit code, and each restored file equals its copy under `cmp`.
- [ ] `git diff` of `UiContext.cs` shows the slot, the flag, the call in `Update`, and remarks only. `DrainEvents` shows no diff.

### Phase 2: The Gamepad Values and the Gamepad Press
**Goal:** A stick fills a Vector2, a trigger or a stick axis fills an Axis, and `GamepadSlot` selects the pad. The frame read of an active map invokes the command of each gamepad button press. HelloPanel drops its `PushGamepad` call.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*GamepadValueActionTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet). The read cases follow the table of this plan. The invoke pass copies the loop of `PushGamepad`, and this plan spells out the stop rule.

**Tasks (group 1, the map):**
- [ ] 2.1 Add `public enum UiGamepadStick : byte` (Left 0, Right 1) to `UiActionEnums.cs`. Add `BindGamepadStick(action, stick)`: an undefined stick throws `ArgumentOutOfRangeException` with the parameter name `stick`, an action that is not a Vector2 throws `ArgumentException` with the parameter name `action`, and the same stick bound already throws. The remarks: the read of `LeftStick` or `RightStick` of `GamepadSlot`, the values after the deadzone of `PollGamepads`, the flip of the GLFW Y so up is +Y (Decision 6), and the clamp.
- [ ] 2.2 Add `BindGamepadAxis(action, axis, scale = 1f)`: an axis outside 0 to 5 throws `ArgumentOutOfRangeException` with the parameter name `axis`, a scale that is not finite throws `ArgumentOutOfRangeException` with the parameter name `scale`, an action that is not an Axis throws, and the same axis bound already throws. The remarks: a trigger reads 0 to 1, a stick axis reads -1 to 1, a Y axis flips so up is positive, the scale multiplies, a negative scale turns a trigger into the negative half of a zoom axis, and the sum clamps to -1 to 1.
- [ ] 2.3 Add `public int GamepadSlot { get; set; }` with a backing field `_gamepadSlot` (default 0). A value outside 0 to 15 throws `ArgumentOutOfRangeException` with the parameter name `value` and keeps the old value. The remarks: the next frame read uses the new slot. `PushGamepad` does not read it.
- [ ] 2.4 Add the `GamepadStick` and `GamepadAxis` cases to `ReadFrame`, and the Axis finish. Add `public float ReadAxis(ReadOnlySpan<byte> action)` with the rules of `ReadVector` for an Axis action.
- [ ] 2.5 Add the invoke pass of "The Frame Read" at the end of `ReadFrame`. Update the class remarks: the gamepad rule (an active map invokes from `Pressed` of `GamepadSlot`, in the order of the bind, and a command that deactivates or replaces the map stops the pass), the error rule (an exception of a command leaves `Update`, and the map keeps the values of the frame), and the re-entry rule (the read copies the arrays first). Add to the remarks of `PushGamepad`: the method works on each map, an active map also reads the pad in `Update`, so do not push the same pad into an active map (Decision 9). Change no line of the body of `PushGamepad`.
- [ ] 2.6 Write `tests/BrutalUi.Framework.Tests/Actions/GamepadValueActionTests.cs` with 8 tests. `TheLeftStickFillsAVector2WithUpPositive` (left stick axes (0.5, -0.25) give (0.5, 0.25). A right stick bind reads axes 2 and 3 the same way. A disconnected slot gives (0, 0)). `AStickAndAKeyCompositeSumAndClamp` (WASD and the left stick in one action: the stick (1, 0) and W give (0.707107, 0.707107), the stick (0, -1) and W give (0, 1), the stick (0, 1) and W give (0, 0)). `ATriggerFillsAnAxis` (the right trigger 0.5 gives 0.5. The left trigger with scale -1 at 0.75 gives -0.75. Both, at 1 and 0.25, give 0.75. The right trigger 0.75 with scale 2 gives 1). `AStickAxisFillsAnAxisWithUpPositive` (`LeftStickX` 0.5 gives 0.5. `LeftStickY` -0.5 gives 0.5). `TheGamepadSlotSelectsThePad` (the default is 0. With `GamepadSlot = 2`, a stick in slot 0 gives (0, 0) and the same stick in slot 2 gives its value. -1 and 16 throw `ArgumentOutOfRangeException` with `ParamName` `value` and keep 2). `AGamepadPressInvokesTheCommandFromTheFrameRead` (an active map with `Cancel` on B: B down and `Update` give 1 call, B held and `Update` give 1, B up then B down give 2. An inactive map with B down invokes nothing. B and A bound in that order and pressed in one frame invoke in the order of the bind). `TheInvokePassStopsAndKeepsTheValues` (the command of B calls `Deactivate`, and the command of A, bound after B, does not run. A command that throws leaves `Update`, and `ReadAxis` of a trigger action in the same map gives the value of that frame). `TheBuildChecksTheGamepadValueBinds` (`BindGamepadStick` on an Axis action and on a Button action, and `BindGamepadAxis` on a Vector2 action, throw `ArgumentException` with `ParamName` `action`. `(UiGamepadStick)2` throws with `ParamName` `stick`. `(UiGamepadAxis)6` throws with `ParamName` `axis`. A scale of `float.NaN` or `float.PositiveInfinity` throws with `ParamName` `scale`. A duplicate stick and a duplicate axis throw. `ReadAxis` on a Vector2 action throws `ArgumentException` with `ParamName` `action`, and an unknown name gives 0).
- [ ] 2.7 Run the build, the tests, and `GamepadValueActionTests` (total 8). Check that `GamepadActionTests` and `ActionMapAllocationTests` pass with no edit. Run four plants. Plant 6: the stick case keeps the GLFW Y. Expect `TheLeftStickFillsAVector2WithUpPositive` to fail. Plant 7: `ReadFrame` reads `input.Gamepad(0)`. Expect `TheGamepadSlotSelectsThePad` to fail. Plant 8: the invoke pass tests `pad.Buttons` in place of `pad.Pressed`. Expect `AGamepadPressInvokesTheCommandFromTheFrameRead` to fail. Plant 9: the Axis finish skips the clamp. Expect `ATriggerFillsAnAxis` to fail. Record the results in `context.md`.

**Tasks (group 2, the sample):**
- [ ] 2.8 Edit `samples/HelloPanel/Program.cs`. Remove `state.Map.PushGamepad(ui.Input.Gamepad(0).Buttons);` at the start of the UI region (Q4). The active map now reads slot 0 in `Update`. Change the comment before `input.PollGamepads()` ("The push of slot 0 into the map is the first line of the UI region") and the comment at the start of the UI region to say that `Update` reads the pad through the map. Update the remarks of `RunFrames` where they name the push.
- [ ] 2.9 Run the build and the tests. Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run plant 10: `GC.KeepAlive(new object());` as the first line of `ReadFrame`. Check `0 Error(s)`. On a host with a window, expect a non-zero byte count on smoke line 3 and exit code 1. Restore with `cmp`. When the sandbox blocks the smoke step, record the exact line and hand the PASS run and plant 10 to Needs Human Verification in `context.md`.

**Estimated Steps:** about 60 (9 tasks, about six to seven steps for one task)

**Files (group 1, 6):** `UiActionEnums.cs`, `UiActionMap.cs`, `UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, `GamepadValueActionTests.cs`, `context.md`
**Files (group 2, 2):** `Program.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `GamepadValueActionTests` pass. Each existing test passes with no edit.
- [ ] Plants 6 to 9 give a non-zero exit code. Plant 10 builds with `0 Error(s)`. Each restored file equals its copy under `cmp`.
- [ ] `grep -n 'PushGamepad' samples/HelloPanel/Program.cs` shows no call. The body of `PushGamepad` shows no diff.

### Phase 3: The Pointer Values, the Two-Key Axis, and the Button Queries
**Goal:** The mouse delta fills a Vector2 and the scroll fills an Axis, each with a scale and no clamp. A two-key composite fills an Axis. The Button actions keep the pressed, held, and released flags, and the three Button queries read them. The queries and the commands agree on one press.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*ActionQueryTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet). The Button latch and the free sum have their formulas in "The Value Rules".

**Tasks (one group):**
- [ ] 3.1 Add `BindMouseDelta(action, scale = 1f)` (Vector2) and `BindScroll(action, scale = 1f)` (Axis). A scale that is not finite throws `ArgumentOutOfRangeException` with the parameter name `scale`. A wrong type throws `ArgumentException` with the parameter name `action`. A second mouse delta bind or a second scroll bind on the map throws. The remarks: the delta keeps the pointer coordinates of the context with Y down (Decision 6), the scroll reads the vertical part, the gate empties both over the UI, and there is no clamp (Q3).
- [ ] 3.2 Add `BindKeyAxis(action, negative, positive)` (Axis), with the key checks of `BindKeyVector` and the parameter names `negative` and `positive`. The remarks: the positive key gives 1, the negative key gives -1, both give 0 (A2), and the clamp.
- [ ] 3.3 Add the `MouseDelta`, `Scroll`, and `KeyAxis` cases to `ReadFrame`. The free sum adds after the clamp in both finishes.
- [ ] 3.4 Add the `Key`, `MouseButton`, and `GamepadButton` cases to `ReadFrame` (down and press, with the modifier match of A3 for the key and the mouse button), and the Button finish with the latch of "The Value Rules".
- [ ] 3.5 Add `WasPressed`, `IsHeld`, and `WasReleased` in `UiActionMap.cs`. An action that is not a Button throws `ArgumentException` with the parameter name `action`. A name that the map does not hold gives `false`. Add the press rule to the class remarks: a press reads the edge of the state in the frame of the slot call (A4), the frame read never invokes a key or mouse command, the gate applies to the queries and the text guard to the slot path only (A1), and the gate limit of Notes conflict 2. Add the latch to the remarks of `IsHeld`: an action becomes held only through a press while the map reads the input.
- [ ] 3.6 Write `tests/BrutalUi.Framework.Tests/Actions/ActionQueryTests.cs` with 10 tests. `AKeyPressInvokesOneTimeAndReadsOnePress` (Escape on `Cancel` with a command: down gives 1 call, `WasPressed` and `IsHeld` `true`. The next `Update` gives 1 call, `WasPressed` `false`, `IsHeld` `true`. A repeat gives 1 call and `WasPressed` `false`. Up gives `WasReleased` `true` and `IsHeld` `false`. The next `Update` gives `WasReleased` `false`). `AGamepadPressInvokesOneTimeAndReadsOnePress` (the same steps with B through `SetGamepad`). `AMouseButtonOverNoElementInvokesOneTimeAndReadsOnePress` (a right press outside each element gives 1 call and `WasPressed` `true`. The release gives `WasReleased` `true`. A press on a button gives no call and `WasPressed` `false`). `TwoBindingsHoldOneButton` (`Fire` with no command on Space and on pad A: Space down gives held. A down gives `WasPressed` `true` again. Space up keeps held with no release. A up gives `WasReleased` `true`). `APressAndAReleaseInOneFrameSetBothFlags` (Space down and up in one `Update`: `WasPressed` `true`, `IsHeld` `false`, `WasReleased` `true`). `TheModifierRuleAppliesToTheQueries` (`Save` on Control+S: S with Control gives `WasPressed` `true`. S alone gives `false`). `TheGateAndTheTextGuardAgree` (a focused `TextField`: W on a Button with a command gives 0 calls and `WasPressed` `false`. A focused `Button`: Escape on `Cancel` gives 1 call and `WasPressed` `false`, the gate limit of A1). `TheMouseDeltaAndTheScrollHaveNoClamp` (a move from (100, 100) to (140, 70) with scale 0.5 gives (20, -15). A scroll of (0, 3) with scale 2 gives 6. The right trigger 1 and a scroll of 3 in one Axis give 4. A move over a pickable element gives (0, 0)). `ATwoKeyAxisReadsMinusOneToOne` (`BindKeyAxis(Throttle, 83, 87)`: W gives 1, S gives -1, both give 0, none gives 0). `TheButtonQueriesCheckTheType` (the three Button queries on an Axis action and on a Vector2 action, and `ReadAxis` on a Button action, throw `ArgumentException` with `ParamName` `action`. An unknown name gives `false` or 0. `BindMouseDelta` on an Axis action, `BindScroll` on a Vector2 action, and `BindKeyAxis` on a Vector2 action throw. A scale of NaN throws with `ParamName` `scale`. `BindKeyAxis(Throttle, 87, 87)` throws with `ParamName` `positive`).
- [ ] 3.7 Run the build, the tests, and `ActionQueryTests` (total 10). Check that `UiActionMapTests` and `GamepadActionTests` pass with no edit. Run four plants. Plant 11: the Button finish sets `Released = oldHeld && !Held`. Expect `APressAndAReleaseInOneFrameSetBothFlags` to fail. Plant 12: the key case skips the modifier match. Expect `TheModifierRuleAppliesToTheQueries` to fail. Plant 13: the mouse delta adds into the clamped sum. Expect `TheMouseDeltaAndTheScrollHaveNoClamp` to fail. Plant 14: the key case sets the press from `Keys.IsDown`. Expect `AKeyPressInvokesOneTimeAndReadsOnePress` to fail. Record the results in `context.md`.

**Estimated Steps:** about 55 (7 tasks, about eight steps for one task)

**Files (one group, 5):** `UiActionMap.cs`, `UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, `ActionQueryTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `ActionQueryTests` pass. Each existing test passes with no edit.
- [ ] Plants 11 to 14 give a non-zero exit code, and each restored file equals its copy under `cmp`.

### Phase 4: The Switch Rule and the Allocation Gate
**Goal:** `Deactivate` resets each frame value. `Activate` of a map that is not active on the context resets each frame value. A query of a map that is not active reads `false` or zero. A mode switch mid-hold gives no stale flag. The frame read and each query allocate 0 bytes.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*ActionMapSwitchTests'` and reads a total of 6 with `failed: 0`, then with `--filter-class '*ActionMapAllocationTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus, at the preset effort). The phase owns the `Deactivate` release across a mode switch. `UiManager` replaces a map with no `Deactivate`, so the reset depends on the order of two slot writes and the latch.

**Tasks (one group):**
- [ ] 4.1 Add a private `ResetFrame()` that sets `Value`, `Pressed`, `Held`, `Released`, and the scratch of each action to their defaults, with no allocation. `Deactivate` calls it at the end, also on a map that another map replaced. `Activate` reads `bool wasActive = ReferenceEquals(_context, context) && IsActive` before the writes, and calls `ResetFrame()` after the writes when `wasActive` is `false`. A second `Activate` on the same context keeps each value.
- [ ] 4.2 Each of the five queries returns `false` or zero when `IsActive` is `false`, after the type check. Add the switch rule to the class remarks: `Deactivate` releases each action (A6), a fresh `Activate` starts with no value, a replaced map reads `false` and zero, the same map in two modes keeps its values, a Button needs a new press in the next map (the latch), and an Axis or a Vector2 reads a held input in the next map at once (Decision 12). Update the remarks of `Activate`, `IsActive`, and `Deactivate`.
- [ ] 4.3 Write `tests/BrutalUi.Framework.Tests/Actions/ActionMapSwitchTests.cs` with 6 tests. `DeactivateReleasesEachHeldAction` (Space held on `Fire` and W held on `Move`, then `Deactivate`: `IsHeld` `false`, `WasReleased` `false`, `ReadVector` (0, 0), also after two more `Update` calls). `ANewMapMidHoldSeesNoStaleFlag` (map A `Fire` on Space, map B `Jump` on Space. Space down and `Update`: A held. `A.Deactivate()`, `B.Activate(context)`, `Update`: B `IsHeld` and `WasPressed` `false`. Space up and `Update`: B `WasReleased` `false`. Space down: B `WasPressed` `true`). `AModeSwitchThroughTheManagerGivesNoStaleFlag` (a `UiManager` with two modes and two maps, in the style of `ModeActionMapTests`. Space held across `SetMode`. After the switch `Update`: A and B read `IsHeld` `false`. Space up: B `WasReleased` `false`). `TheSameMapInTwoModesKeepsAHeldAction` (one map in both modes, Space held across `SetMode`: `IsHeld` stays `true` and `WasReleased` stays `false`). `AReplacedMapReadsZero` (`B.Activate(context)` with no `Deactivate` of A: A reads `false` and (0, 0) in the next frame. `A.Activate(context)` again with Space still down: A `IsHeld` `false` until a new press). `AValueActionReadsAHeldInputInTheNextMap` (`Move` on W in A and B, W held across the switch: B `ReadVector` gives (0, 1) in the first frame after the switch).
- [ ] 4.4 Grow `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs`. Add to the scene: `Move` (Vector2: W, A, S, D and the left stick), `Look` (Vector2: the mouse delta), `Zoom` (Axis: the scroll, the right trigger, and the left trigger with scale -1), `Throttle` (Axis: S and W), and `Fire` (Button with no command: Space and pad X). Add the events and two pad states as scene fields. Add `QueryStep`: `ClearFocus()`, frame 1 writes slot 0 with X and A down and stick values, pushes W down, Space down, a pointer move, and a scroll outside the panel, and runs `Update`. Then it calls each of the five queries on each new action and adds the results to an `int` or `float` field of the scene. Frame 2 writes slot 0 with no button, pushes W up and Space up, runs `Update`, reads the queries again, and runs `Draw`. Add the test `TheTypedReadAndTheQueriesAllocateNothing` (2 warm-up rounds, 100 rounds, 0 bytes). Extend `TheMeasuredBodyDoesRealWork` with asserts after one `QueryStep`: the command of pad A runs one time through the frame read, and the sum field changes by a fixed amount. Update the class summary and remarks (the typed actions, the frame read, the queries, the new counts of actions and bindings).
- [ ] 4.5 Run the build, the tests, and both demo filters. Check that `ModeActionMapTests` and `ModeDiffTests` pass with no edit. Run five plants. Plant 15: `Deactivate` skips `ResetFrame()`. Expect `DeactivateReleasesEachHeldAction` to fail. Plant 16: the queries skip the `IsActive` check. Expect `AReplacedMapReadsZero` or `AModeSwitchThroughTheManagerGivesNoStaleFlag` to fail. Plant 17: `Activate` resets also when `wasActive` is `true`. Expect `TheSameMapInTwoModesKeepsAHeldAction` to fail. Plant 18: the Button finish sets `Held = down`. Expect `ANewMapMidHoldSeesNoStaleFlag` to fail. Plant 19: `GC.KeepAlive(new object());` as the first line of `ReadFrame`. Expect `TheTypedReadAndTheQueriesAllocateNothing` to fail. Record the failed tests and the bytes of plant 19 in `context.md`.

**Estimated Steps:** about 45 (5 tasks, about nine steps for one task)

**Files (one group, 5):** `UiActionMap.cs`, `UiActionMap.Read.cs`, `ActionMapSwitchTests.cs`, `ActionMapAllocationTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `ActionMapSwitchTests` and the 6 tests of `ActionMapAllocationTests` pass. Each other existing test passes with no edit.
- [ ] Plants 15 to 19 give a non-zero exit code, and each restored file equals its copy under `cmp`.
- [ ] `git diff` of `src/BrutalUi.Framework/Panels/UiManager.cs` shows no output.

### Phase 5: The HelloPanel Move Label and the Docs
**Goal:** HelloPanel holds a `Move` Vector2 action with W, A, S, D and the left stick of slot 0, and shows `ReadVector` in a fifth label. Section 4, section 9, and section 14 of the technical design and the changelog state the typed map.
**Demo:** a developer runs HelloPanel on a host with a window, holds W, and reads `Move: 0.00, 1.00`. With W and D the label reads `Move: 0.71, 0.71`. With a gamepad in slot 0 and the left stick full up, it reads `Move: 0.00, 1.00`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet). The label copies the pattern of the stick label.

**Tasks (group 1, the sample):**
- [ ] 5.1 Edit `samples/HelloPanel/HelloState.cs`. Add `public static ReadOnlySpan<byte> MoveAction => "Move"u8;` with a doc comment. In the constructor, after the three old actions: `Map.AddAction(MoveAction, UiActionType.Vector2)`, `Map.BindKeyVector(MoveAction, (int)GlfwKey.W, (int)GlfwKey.A, (int)GlfwKey.S, (int)GlfwKey.D)`, and `Map.BindGamepadStick(MoveAction, UiGamepadStick.Left)`. Use the `GlfwKey` names that compile. Keep the three old Button actions (A8). Update the remarks.
- [ ] 5.2 Edit `samples/HelloPanel/HelloScreen.cs`. `BuildWidgetPanel` gets `out Label moveLabel` after `stickLabel`. The label is not pickable, comes right after the stick label, and starts with `LongestMoveText`. Add `public static ReadOnlySpan<byte> MoveText(float2 value, Span<byte> scratch)`. It writes `Move: <x>, <y>` through `StickComponent` and returns the written part. `LongestMoveText` is `"Move: -1.00, -1.00"u8` (18 bytes). Update the class remarks (five labels). Edit `samples/HelloPanel/HelloModes.cs`: store the label in `_moveLabel`, expose `MoveLabel`, and add it to the constructor check, in the pattern of `StickLabel`.
- [ ] 5.3 Edit `samples/HelloPanel/Program.cs`. Add the parameter `Label moveLabel` to `RunFrames` after `stickLabel`, pass `modes.MoveLabel`, create `byte[] moveScratch = new byte[32];` next to `stickScratch`, and add `moveLabel.SetText(HelloScreen.MoveText(state.Map.ReadVector(HelloState.MoveAction), moveScratch));` right after the stick label line, inside the UI region. Update the remarks of `RunFrames`.
- [ ] 5.4 Run the build and the tests. Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run plant 20: `GC.KeepAlive(new object());` as the first line of `HelloScreen.MoveText`. Check `0 Error(s)`. On a host with a window, expect a non-zero byte count on smoke line 3 and exit code 1. Restore with `cmp`. When the sandbox blocks the smoke step, record the exact line and hand the PASS run, plant 20, and the demo to Needs Human Verification in `context.md`.

**Tasks (group 2, the docs):**
- [ ] 5.5 Edit `docs/technical-design.md`, section 9. In `### Flow`, add item 13 "The read slot": `OnInputRead`, the place after the drain, the order, the re-entry rule, and the exception rule. Rewrite `### Action map` for the typed map: the action types, the binds with the table of "The Value Rules", the sum and the clamp, the free sum (Q3), the Y-up rule, the Button latch, the press rule and the gate limit, the gamepad rule (`GamepadSlot`, the invoke from the frame read, `PushGamepad` for an inactive map), the queries, the switch rule, the build rules with the type checks, the error rule, and the cost. Keep each old rule that still holds. Name `TypedActionMapTests`, `GamepadValueActionTests`, `ActionQueryTests`, and `ActionMapSwitchTests`.
- [ ] 5.6 Edit `docs/technical-design.md`, section 4 `### Frame order` item 2: the read slot runs after the input pass and before the binding pass, and the active map reads the pad there, so the game does not push the gamepad before `Update`. Edit section 14: the bullet **Action map tests.** names the typed read and the growth of the seventeenth gate. Add the bullet **Typed action tests.** with the four new classes, `InputReadTests`, and plants 1 to 20. In the bullet for the sample projects, name the move label and the drop of the push.
- [ ] 5.7 Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`: `UiContext.OnInputRead` and `UiInputReadHandler` (input/input-actions). `UiActionType`, `UiGamepadStick`, `UiActionMap.AddAction(action, type)`, the six new binds, `GamepadSlot`, and the five queries (input/input-actions). Under `### Changed`: an active `UiActionMap` reads the gamepad buttons of `GamepadSlot` in `Update` and invokes their commands, so a game drops its `PushGamepad` call for an active map. `BindKey`, `BindMouseButton`, and `BindGamepadButton` throw `ArgumentException` for an Axis or Vector2 action (input/input-actions). HelloPanel shows the `Move` vector and no longer pushes the gamepad.
- [ ] 5.8 Run the build, the tests, and the demo commands of phases 1 to 4. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes. Record in `context.md` the results, the follow-up for spacegame (move the submodule, continue map-controls), and each decision that this plan did not settle.

**Estimated Steps:** about 45 (8 tasks, about five to six steps for one task)

**Files (group 1, 5):** `HelloState.cs`, `HelloScreen.cs`, `HelloModes.cs`, `Program.cs`, `context.md`
**Files (group 2, 4):** `technical-design.md`, `CHANGELOG.md`, `context.md`, `implementation.md`

**Acceptance Criteria:**
- [ ] The build gives `0 Warning(s)` and `0 Error(s)`, and each test passes.
- [ ] `scripts/test-linux.sh` prints its PASS line, or `context.md` records the sandbox line and hands the run to a person.
- [ ] Plant 20 builds with `0 Error(s)` and restores equal under `cmp`.
- [ ] Section 9 holds item 13 and the typed action map. Section 4 names the read slot. `CHANGELOG.md` names each new public member.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Frame read hook: a new Core slot `OnInputRead`, called after the drain. `Activate` writes it and `Deactivate` clears it.
- Q2 Gamepad slot: a `GamepadSlot` property on the map, 0 to 15, default 0.
- Q3 Mouse delta and scroll: a `scale` parameter, default 1, and no clamp. The clamp to length 1 applies to a stick and a key composite only.
- Q4 Gamepad buttons: the frame read of an active map invokes the command from `Gamepad(GamepadSlot).Pressed`. `PushGamepad` stays for an inactive map. HelloPanel drops its `PushGamepad` call.
- Q5 Query API: a UTF-8 name only. No action handle.
- Q6 Demo of phase 1: a developer runs the Framework test binary with a filter on the typed map tests and reads a total with `failed: 0`. The tests push W on a W,A,S,D Vector2 and read (0, 1) through the new Core slot.
- A1 The map reads `UiContext.GameInput` only, never `Input`. The gate rule of input-state applies to each query. The text guard stays on the slot path only. (`src/BrutalUi.Core/Tree/UiContext.cs:1040`)
- A2 A key composite holds two keys for an Axis (negative, positive) or four keys for a Vector2 (up, left, down, right), with no modifiers. Two opposite keys give 0.
- A3 A Button action is held when one binding holds it. A key binding with modifiers matches only in a frame where `Keys.Modifiers` holds the same Shift, Control, Alt, and Super bits.
- A4 A Button query reads the press edge of the state in the same frame that the slot invoked the command. The map keeps no press count of its own, so the queries and the commands agree on one press.
- A5 A query of a wrong type throws `ArgumentException` with the action parameter name. A name the map does not hold returns `false` or zero and throws nothing.
- A6 `Deactivate` clears the new slot and resets each held value and each edge of the map. `Activate` on a second context moves the new slot as it moves `OnUnhandledInput` today.
- A7 The scope stays as in Out of Scope of the requirements: no scheme tag, no rebind, no hold time, no tap count, no chord.
- A8 HelloPanel adds one `Move` Vector2 action in `HelloState` with W,A,S,D and the left stick of slot 0, and shows `ReadVector` in a label. The three old Button actions stay.

### Decision 1: One binding struct and one frame read loop (the chosen approach)
**Choice:** `InputBinding` gets a kind for each new bind, three more codes, and a scale. `ActionEntry` gets the type and the frame values. One loop in `ReadFrame` reads `GameInput` and dispatches with one switch by kind. The class splits into three partial files.
**Rationale:** The map changes in place, so `HelloPanel`, the action-map tests, and the mode switch keep working. No new type beyond two public enums. No virtual call and no allocation. control-schemes and binding-remap learn one binding shape.
**Alternatives:** A reader object for each kind, and a second map class. The user rejected both.

### Decision 2: The read slot gets the context as its one parameter
**Choice:** `public delegate void UiInputReadHandler(UiContext context);`. `Update` calls `inputRead(this)` right after `DrainEvents()` and before `UpdateBindings()`, outside the drain, with a re-entry flag of its own.
**Rationale:** The handler reads `context.GameInput` with no null check of a stored field. The call sits outside the `finally` of the drain, so a handler exception in the drain never runs a command during the unwind. A command that writes a store reaches the binding pass of the same `Update`, as a command from the slot path does. The flag gives the same `InvalidOperationException` rule as the before-update slot.
**Alternatives:** (a) A call inside the `finally` of the drain: a command would run while an exception unwinds. (b) A delegate with no parameter: the map then needs `_context!`.

### Decision 3: `AddAction(action, type)` adds an action with no command, also for a Button
**Choice:** The new overload adds an action of any type with an empty command. `AddAction(action, command)` stays and adds a Button with a command. `Invoke` skips an empty command and makes no registry call.
**Rationale:** The old overload keeps each caller. A game queries a Button such as `Fire` or a drag button with `IsHeld` and needs no command for it. The skip keeps `UnknownCommand` for a real typo of a command name.
**Alternatives:** (a) One overload with a type and an optional command: an Axis with a command makes no sense, and the old callers need an edit. (b) No Button without a command: a game registers empty commands for each query action.

### Decision 4: The free sum and the clamped sum
**Choice:** A stick, a stick axis, a trigger, and a key composite add into the clamped sum. The mouse delta and the scroll add into the free sum. The finish clamps the clamped sum (length 1 for a Vector2, -1 to 1 for an Axis) and then adds the free sum.
**Rationale:** Q3 says that the mouse delta and the scroll have no clamp and that the clamp applies to a stick and a key composite. The two sums keep both rules true in one action, for example a zoom axis from a trigger and the scroll. The requirement "a Vector2 from two bindings sums and clamps to length 1" holds for each stick and composite pair.
**Alternatives:** One sum with a clamp: the scroll caps at 1. One sum with no clamp: W and D give a length of 1.414.

### Decision 5: The Button latch
**Choice:** `Held = (oldHeld || press) && down`. `Released = (oldHeld || press) && !Held`. `Pressed = press`. The press is the press edge of one binding in the state (A4).
**Rationale:** An action becomes held only through a press while the map reads. A key that is down at a switch then gives no held flag and no released flag in the next map (requirement line 20). A press and a release in one frame give both flags, as the state does. A second binding that is pressed while the first holds gives a new press, and the slot path also invokes a second time for it, so the two paths agree. A release of one of two held bindings gives no release.
**Alternatives:** `Held = down` with an edge from the held flag: a key held across a switch reads as held in the next map, and a press and a release in one frame give no press.

### Decision 6: Up is +Y for a stick and a key composite, and the pointer keeps its coordinates
**Choice:** A key composite gives +1 for up (Q6). A stick bind and a Y stick-axis bind negate the GLFW Y, so up is +1. The mouse delta keeps the pointer coordinates of the context, with Y down.
**Rationale:** A8 sums W, A, S, D and the left stick in one action. Without the flip, W and the stick up cancel. gamepad-axes Decision 11 leaves the flip to a binding of this feature. The mouse delta pans a map with the pointer, so the pointer sign is the natural one. A negative scale flips both axes of the delta. Notes question 2 holds the open choice.
**Alternatives:** (a) No flip: the sum of A8 breaks. (b) Flip the delta too: a drag pan moves the wrong way.

### Decision 7: A key composite reads its keys with no modifier check
**Choice:** A composite stores no modifiers and reads `Keys.IsDown` of each key in each frame.
**Rationale:** A2 says "with no modifiers". A player who holds Shift to sprint still moves with W. A Button key binding keeps the exact modifier match of A3, so Control+S still does not fire `S`.
**Alternatives:** The exact match for a composite too: Shift+W stops the move. Notes question 3 holds the choice.

### Decision 8: The modifier source of the frame read is `Keys.Modifiers`
**Choice:** A key binding and a mouse binding match when `GameInput.Keys.Modifiers & MatchMask` equals the stored modifiers (A3).
**Rationale:** The state stores no modifiers for a pointer entry. `Keys.Modifiers` holds the modifiers of the last key event (A6 of input-state). In a frame with one key event, the slot path and the frame read see the same modifiers.
**Alternatives:** Store the modifiers of each press in the map: a press count of its own, against A4.

### Decision 9: `PushGamepad` keeps its body on each map
**Choice:** `PushGamepad` does not change. Its remarks warn that an active map also reads the pad, so a push into an active map invokes each press a second time.
**Rationale:** `ActionMapAllocationTests.GamepadStep` pushes into an active map and asserts one call, and the requirements keep the action-map tests working. A guard in `PushGamepad` would break that test. HelloPanel drops the push (Q4).
**Alternatives:** `PushGamepad` returns 0 on an active map: it changes a tested behavior. Notes conflict 3 holds the details.

### Decision 10: The gamepad press reads `Pressed` of the slot, and the invoke pass runs after the values
**Choice:** The invoke pass reads `pad.Pressed` (Q4), runs after the finish of each action, and stops when the slot no longer holds `_onRead`.
**Rationale:** `WasPressed` of a gamepad Button reads the same bit, so the command and the query agree in each frame. The values are complete before a command runs, so a command that reads the map sees this frame. A command that switches the map stops the pass, so a deactivated map invokes nothing more.
**Alternatives:** An edge of the map from the buttons of the last read: the query and the command could disagree with `Pressed` of the state.

### Decision 11: The switch rule covers a replace with no `Deactivate`
**Choice:** `Deactivate` resets each frame value. `Activate` resets each frame value when the map was not active on that context. A query of a map that is not active reads `false` or zero.
**Rationale:** `UiManager` calls `Activate` on the map of the new mode and calls `Deactivate` only when the new mode has no map (`UiManager.cs` lines 719 to 728). A direct switch from map A to map B thus never deactivates A. The `IsActive` check gives A no stale value, and the reset at a fresh `Activate` gives A no stale latch when it comes back. The same map in two modes keeps its values, because its `Activate` changes nothing. `UiManager.cs` does not change.
**Alternatives:** Ask `UiManager` to call `Deactivate` on the old map: the requirements forbid a change of mode-set.

### Decision 12: An Axis or a Vector2 has no latch
**Choice:** A value action reads the current state in each frame, also in the first frame of the next map.
**Rationale:** A held stick or a held W moves the camera with no gap at a mode switch. The requirement names a stale held or released flag, which is a Button concept. A value has no edge to go stale.
**Alternatives:** A latch for values: a player must release and press W again after each mode switch.

### Decision 13: A duplicate input on the map throws for each kind
**Choice:** Each bind throws `ArgumentException` for an input that the map binds already: the same kind, the same codes, and the same modifiers. A second mouse delta bind and a second scroll bind on one map throw.
**Rationale:** It extends the build rule of action-map with one rule for each kind. binding-remap gets a clear conflict rule. A mode with another map can bind the same input again.
**Alternatives:** Allow a value input on two actions: the conflict rule of binding-remap then needs exceptions.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A game keeps `PushGamepad` on an active map, and each pad press invokes two times. | Medium | Medium | Decision 9. The remarks of `PushGamepad`, section 9, and the changelog say it. HelloPanel drops the push in phase 2. |
| A frame with no `SetGamepad` keeps the gamepad edge of the last poll, so the frame read invokes again and `WasPressed` reads `true` again. | Low | Medium | The contract of `SetGamepad` is one call for each slot in each frame, and `PollGamepads` does it. The remarks of the gamepad rule state it. The query and the command still agree (Decision 10). |
| A focused element that is not a text field closes the key gate, but an unhandled key still reaches the slot, so the command runs and `WasPressed` reads `false`. | Medium | Low | A1 is confirmed. `TheGateAndTheTextGuardAgree` pins the rule. Section 9 states the limit. Notes conflict 2 holds the options. |
| `Keys.Modifiers` holds the modifiers of the last key event. On X11, GLFW can report the modifiers before the event, so a held S can read as Control+S after Control goes up. | Low | Low | Decision 8. The press itself carries the right modifiers. Section 9 states the limit. |
| The new call in `Update` changes the order of a pass, and a Core test fails. | Low | High | A null slot does nothing, so each existing context keeps its behavior. Phase 1 runs on an ADVANCED agent. Plant 1 proves the order test. |
| An exception of a command in the read slot skips the binding pass of that frame. | Low | Low | The same rule as an exception in the drain. The delegate remarks state it. `TheInvokePassStopsAndKeepsTheValues` covers the kept values. |
| The split into partial files loses a member or a doc comment. | Low | Medium | Task 1.6 moves the members with no change and runs the same totals before any new code. |
| A mode switch through `UiManager` leaves a stale value in the replaced map. | Medium | High | Decision 11. `AModeSwitchThroughTheManagerGivesNoStaleFlag` and `AReplacedMapReadsZero`. Phase 4 runs on an ADVANCED agent. |
| A query throws on the frame path and allocates. | Low | Low | A query throws only for a wrong type, which is a bug at build time. The allocation gate calls each query with the right type. |
| A float assert fails on the last bit. | Medium | Low | Each float assert uses five decimal places. The worked values are exact or have six decimals. |
| The sandbox blocks the smoke step. | High | Low | Gotcha 3 of input-state. `context.md` hands the smoke PASS run, plants 10 and 20, and the demo of phase 5 to a person. |
| The test host exits with 134, or `TheCurvesAllocateNothing` fails one time. | Medium | Low | Run the tests a second time. |
| `dotnet build` or `dotnet test` hangs on stale MSBuild nodes. | High | Low | Each command sets `MSBUILDDISABLENODEREUSE=1`, and `dotnet build` gets `-nodeReuse:false`. No `dotnet build-server shutdown`. |

---

## Dependencies

### Internal Dependencies
- input-state (done): `UiContext.GameInput`, `UiInputState`, the gate, the edges, `SetGamepad`, `ClearFocus`.
- gamepad-axes (done): `UiGamepadAxis`, `LeftStick`, `RightStick`, `Axis(UiGamepadAxis)`, `PollGamepads`.
- state-and-commands/action-map (done): `UiActionMap`, `CommandRegistry`, `OnUnhandledInput`, `UiActions`, `FocusCommands`.
- panels-and-modes/mode-set (done): `UiManager`, `ModeDefinition.ActionMap` (no change).
- The test helpers `AllocationGate.cs` and `RecordingRenderer.cs` in `BrutalUi.Core.Tests` (done).

### External Dependencies
- None new. No package and no native dependency.

### Sibling Features That Depend on This One
- control-schemes (the binding list and the typed binds, with a scheme name on each binding).
- binding-remap (one binding for each text line: kind, codes, modifiers, scale, action).
- spacegame map-controls (outside this repo): spacegame moves its submodule to the commit of this feature and continues map-controls.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. `UiContext.OnInputRead` runs one time in each `Update`, after the drain, with the context as its argument, and reads the `GameInput` of that frame. It runs after `OnBeforeUpdate` and after each `OnUnhandledInput` call of the frame. A call of `Update` from it throws `InvalidOperationException` (`InputReadTests`).
- [ ] F2. A Vector2 action with `BindKeyVector(W, A, S, D)` on an active map reads (0, 1) for W, (-1, 0) for A, (0, -1) for S, (1, 0) for D, (0.707107, 0.707107) for W and D, and (0, 0) for W and S. With a focused `Button`, W reads (0, 0) (`TypedActionMapTests`).
- [ ] F3. A stick bind reads (0.5, 0.25) for the GLFW stick (0.5, -0.25). The stick (1, 0) and W in one action read (0.707107, 0.707107). A trigger reads its value times the scale, clamped to -1 to 1. `LeftStickY` -0.5 reads 0.5. `GamepadSlot = 2` reads slot 2 only (`GamepadValueActionTests`).
- [ ] F4. On an active map, a gamepad button press invokes its command one time from the frame read. A held button invokes nothing more. An inactive map invokes nothing from the pad. Two presses in one frame invoke in the order of the bind (`GamepadValueActionTests`).
- [ ] F5. The mouse delta of a move from (100, 100) to (140, 70) with scale 0.5 reads (20, -15). A scroll of (0, 3) with scale 2 reads 6. The right trigger 1 and a scroll of 3 in one Axis read 4. A two-key axis reads 1, -1, and 0 (`ActionQueryTests`).
- [ ] F6. A key press, a gamepad press, and a mouse press over no element each invoke the command one time and set `WasPressed` in that frame only. `IsHeld` stays `true` while one binding holds the action. `WasReleased` reads `true` in the frame of the last release. A press and a release in one frame set `WasPressed` and `WasReleased` (`ActionQueryTests`).
- [ ] F7. Each bind of the wrong type and each query of the wrong type throw `ArgumentException` with `ParamName` `action`. A bad key, axis, stick, scale, and type throw `ArgumentOutOfRangeException` with the parameter name of that argument. A rejected call changes no count. A name that the map does not hold reads `false`, 0, or (0, 0) (`TypedActionMapTests`, `GamepadValueActionTests`, `ActionQueryTests`).
- [ ] F8. `Deactivate` sets each flag to `false` and each value to zero. A map that another map replaced reads `false` and zero. A mode switch through `UiManager` with Space held gives the next map no held flag and no released flag. The same map in two modes keeps a held action (`ActionMapSwitchTests`).
- [ ] F9. HelloPanel shows `Move: <x>, <y>` in a fifth label. It reads `Move: 0.00, 0.00` at rest and `Move: 0.00, 1.00` with W held.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln -nodeReuse:false` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` holds 6 more tests than at `<base>`. `BrutalUi.Framework.Tests` holds 34 more tests than at `<base>` (9, 8, 10, 6, and 1 in `ActionMapAllocationTests`). The skipped count equals the count of `<base>`.
- [ ] Q3. `UiActionMapTests`, `GamepadActionTests`, `FocusActionTests`, each test under `tests/BrutalUi.Framework.Tests/Panels/`, `BeforeUpdateTests`, and `InputStateTests` pass with no edit. The one edited existing test class is `ActionMapAllocationTests`.
- [ ] Q4. `TheTypedReadAndTheQueriesAllocateNothing` and `TheSlotAllocatesNothing` assert 0 bytes. Plants 2 and 19 fail them.
- [ ] Q5. Each new public member has an XML doc comment. The changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q6. `ReadFrame` and the five queries hold no array creation, no `lock`, no `event` keyword, no `ToString(` call, no `(object)` cast, no lambda, and no `List<`.

### Integration Criteria
- [ ] I1. The Core diff from `<base>` holds only the new `src/BrutalUi.Core/Input/UiInputReadHandler.cs` and `src/BrutalUi.Core/Tree/UiContext.cs`. `UiInputState.cs`, `InputStateWriter.cs`, `UiGamepadState.cs`, and each file of `src/BrutalUi.Glfw/` show no diff.
- [ ] I2. `src/BrutalUi.Framework/Panels/UiManager.cs` shows no diff. The mode switch calls `Activate` and `Deactivate` only.
- [ ] I3. `UiActionMap` stays one class in namespace `BrutalUi.Framework`, split into `UiActionMap.cs`, `UiActionMap.Bindings.cs`, and `UiActionMap.Read.cs`. `grep -n 'GameInput\|\.Input\b' src/BrutalUi.Framework/Actions/UiActionMap*.cs` shows reads of `GameInput` only.
- [ ] I4. `samples/HelloPanel/Program.cs` holds no `PushGamepad` call and one `ReadVector` call inside the UI region. `scripts/test-linux.sh` prints its PASS line on a host with a working display.
- [ ] I5. Section 9 of `docs/technical-design.md` holds item 13 and the typed action map. Section 4 names the read slot. Section 14 holds the bullet **Typed action tests.**. `CHANGELOG.md` names each new public member and the `PushGamepad` change.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. Read `<base>` and the base counts from `docs/features/input/input-actions/context.md`. Do not run `dotnet build-server shutdown`. When a run exits with 134 or `TheCurvesAllocateNothing` fails, run it a second time.

1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and the skipped count of `<base>` (Q2).
3. Run `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputReadTests' 2>&1 | tail -n 5` and expect 6 with `failed: 0` (F1). Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5` for `TypedActionMapTests` (9), `GamepadValueActionTests` (8), `ActionQueryTests` (10), `ActionMapSwitchTests` (6), and `ActionMapAllocationTests` (6). Expect `failed: 0` for each (F2 to F8, Q4). Run the Framework binary with `'*UiActionMapTests'`, `'*GamepadActionTests'`, `'*FocusActionTests'`, and `'*ModeActionMapTests'`, and compare with the base counts (Q3). Read the test names and match them to the criteria.
4. Copy `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs` to a scratch folder under `src/BrutalUi.Framework/obj/`. Add `GC.KeepAlive(new object());` as the first line of `ReadFrame`. Run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp` (Q4). Repeat with the stick case that keeps the GLFW Y (F3). Copy `src/BrutalUi.Framework/Actions/UiActionMap.cs`, remove the `IsActive` check from `IsHeld`, run the same command, expect a value that is not 0, and restore it with `cmp` (F8). Run step 2 again.
5. `grep -nE 'new [A-Za-z0-9_]+\[|\block\b|\bevent\b|ToString\(|\(object\)|=>|List<' src/BrutalUi.Framework/Actions/UiActionMap.Read.cs | grep -v '///'`. Read each hit and check that none sits in `ReadFrame`. Read the five queries in `UiActionMap.cs` and check the same rule (Q6).
6. `git diff <base> --name-only -- src/ samples/ scripts/ tests/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/ tests/`. Expect only the files of the File Tree (I1, I3). `git diff <base> -- src/BrutalUi.Core/Input/UiInputState.cs src/BrutalUi.Core/Input/InputStateWriter.cs src/BrutalUi.Glfw/ src/BrutalUi.Framework/Panels/UiManager.cs`. Expect no output (I1, I2).
7. `grep -n 'PushGamepad\|ReadVector' samples/HelloPanel/Program.cs`. Expect no `PushGamepad` and one `ReadVector` (I4).
8. `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4). In a sandbox that blocks the X11 socket, the smoke step fails with `GLFW init failed: PlatformUnavailable`. Then run the step on a host with a working display, or under `xvfb-run -a` in a normal shell.
9. The manual check of HelloPanel (F9) on a host with a window: run `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. Read `Move: 0.00, 0.00` at rest. Hold W and read `Move: 0.00, 1.00`. Hold W and D and read `Move: 0.71, 0.71`. Hold W and S and read `Move: 0.00, 0.00`. Click the text field, hold W, and read `Move: 0.00, 0.00` (the gate). With a gamepad in slot 0, push the left stick full up and read `Move: 0.00, 1.00`, while the stick label reads `Left stick: 0.00, -1.00`. Press B one time and see the score reset one time.
10. Read section 4, section 9, and section 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I5). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q5).

---

## Testing Strategy

### Unit Tests
- `Core.Tests/Input/InputReadTests` (6): the place after the drain, the order of the three slots, the null slot, the re-entry, the exception, and 0 bytes.
- `Framework.Tests/Actions/TypedActionMapTests` (9): the four-key composite, each direction, the key up, the gate, the type checks, the key checks, `ReadVector`, the two slots of `Activate` and `Deactivate`, and the Button with no command.
- `Framework.Tests/Actions/GamepadValueActionTests` (8): the stick, the sum with a composite, the trigger, the stick axis, `GamepadSlot`, the press invoke, the stop and the kept values, and the build checks.
- `Framework.Tests/Actions/ActionQueryTests` (10): the press agreement for three button kinds, two bindings, one-frame press and release, the modifier rule, the gate and the text guard, the delta and the scroll, the two-key axis, and the type checks.

### Integration Tests
- `Framework.Tests/Actions/ActionMapSwitchTests` (6): `Deactivate`, a direct switch, a switch through `UiManager`, the same map in two modes, a replaced map, and a value action across a switch.
- The HelloPanel smoke run of `scripts/test-linux.sh` runs the frame read and the move label in each frame. The sample check of 0 bytes covers them.

### Allocation Tests
- `ActionMapAllocationTests` (the seventeenth gate) grows with `QueryStep` and `TheTypedReadAndTheQueriesAllocateNothing`. Each existing measured body now also runs the frame read inside `Update`. Plant 19 guards it.
- `InputReadTests.TheSlotAllocatesNothing` guards the Core call. Plant 2 guards it.

### Regression Tests
- Each existing test runs with no edit, except `ActionMapAllocationTests`. `UiActionMapTests` guards the slot path, the repeat rule, and the text guard. `GamepadActionTests` guards `PushGamepad`. `ModeActionMapTests` guards the mode switch.

### Manual Testing
- Step 9 of the Verification Method: the keys, the diagonal, the opposite keys, the gate, the stick, and the B button.

---

## Related Documentation

- **Requirements:** `docs/features/input/input-actions/requirements.md`
- **Epic:** `docs/features/input/epic-overview.md`, `docs/features/input/epic-requirements.md`, `.beast-mode/interview.md`
- **Siblings:** `docs/features/input/input-state/implementation.md` and `context.md` (Gotchas 1 to 3), `docs/features/input/gamepad-axes/implementation.md` (Decision 11, the Y sign), `docs/features/input/control-schemes/requirements.md`, `docs/features/input/binding-remap/requirements.md`
- **Design:** `docs/technical-design.md` sections 4 (Frame order), 9 (Input and Action map), 10 (Mode set), and 14 (Testing). `docs/mission-statement.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Framework/Actions/UiActionMap.cs` (the remarks, the build rules, `PushGamepad`), `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs` and `UiContext.Update` (the slot and the flag), `src/BrutalUi.Core/Input/UiInputState.cs` (the reads), `src/BrutalUi.Framework/Panels/UiManager.cs` lines 719 to 728 (the map step), `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` (the gate), `tests/BrutalUi.Framework.Tests/Panels/ModeActionMapTests.cs` (a manager in a test), `samples/HelloPanel/HelloScreen.cs` (`StickText`, `StickComponent`)
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (A) of each question. A change before the phase that the question names costs little.

1. **The gate limit of a focused element** (before phase 3)
   - **(A) Recommended:** keep A1. The query reads the gated state, the slot path keeps the text guard only, and section 9 states the limit.
   - (B) The slot path also skips a key when the key gate is closed: the paths agree always, but `TheTextGuardPassesANonTextKeyAndAShortcut` and the key step of `ActionMapAllocationTests` change.
   - (C) The Button flags of a key binding come from the slot path: against A1 and A4.
2. **The Y sign of the mouse delta** (before phase 3)
   - **(A) Recommended:** the pointer coordinates, Y down (Decision 6).
   - (B) Up is +Y, as for a stick: a drag pan needs a negative scale on Y only, which one scale cannot give.
3. **The modifier rule of a key composite** (before phase 1)
   - **(A) Recommended:** no modifier check, so Shift+W still moves (Decision 7).
   - (B) The composite needs no modifier down: Shift+W stops the move.
4. **The scroll axis** (before phase 3)
   - **(A) Recommended:** `BindScroll` reads the vertical part only.
   - (B) A parameter for the horizontal part: one more argument for a trackpad case that no consumer asks for.

### Conflicts and Gaps

1. **The mode diff and `Deactivate`.** The prompt says that the mode diff calls `Deactivate()` on the old map. `UiManager.cs` lines 719 to 728 call `Activate` on the map of the new mode and call `Deactivate` only when the new mode has no map. A direct switch from map A to map B never deactivates A. The plan keeps A6 and adds Decision 11, so the switch has no stale flag on both paths and `UiManager.cs` does not change.
2. **A1 and "the queries and the commands agree on one press".** A1 gates each query and keeps the text guard on the slot path only. A focused element that is not a text field closes the key gate, but an unhandled key still reaches the slot (`TheTextGuardPassesANonTextKeyAndAShortcut` and the key step of `ActionMapAllocationTests` rely on it). The command then runs, and `WasPressed` reads `false`. The plan keeps A1, pins the rule in `TheGateAndTheTextGuardAgree`, and states the limit. The two paths never both invoke one press, and they agree when the gate is open. Notes question 1 holds the options.
3. **Q4 and `PushGamepad` on an active map.** Q4 says "`PushGamepad` stays for an inactive map". `ActionMapAllocationTests.GamepadStep` pushes into an active map and asserts one call. The plan keeps the body of `PushGamepad` for each map and warns in the remarks (Decision 9).
4. **Q6 and the GLFW Y sign.** Q6 gives (0, 1) for W, so up is +Y. GLFW gives -1 for a stick up (gamepad-axes Decision 11). A8 sums the two in one action. The plan flips the stick Y in the map (Decision 6).
5. **The two gamepad-axis kinds of the chosen approach.** The approach names a kind for the stick axis and a kind for the trigger. `UiGamepadAxis` names both, so the plan uses one kind `GamepadAxis` with one bind `BindGamepadAxis`. The behavior is the same.
6. **A Button with no command.** Requirements line 12 says "A Button action keeps its command". The plan also allows a Button with no command through `AddAction(action, UiActionType.Button)` (Decision 3). A game needs it for a query such as `IsHeld("Fire"u8)`.
7. **The gamepad edge with no poll.** The state keeps the gamepad edges until the next `SetGamepad`. A frame with no `SetGamepad` repeats the press. The plan follows Q4 and the contract of `SetGamepad`, and states it.
8. **The plan is not compiled, and the prose lint did not run.** The planner had no shell tool. Task 1.6 is the first compile, and task 5.8 runs the lint on this plan and on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
