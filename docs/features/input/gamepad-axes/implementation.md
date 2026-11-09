# Gamepad Axes - Implementation Plan

**Epic:** input (feature 2 of 5)
**Status:** Ready for Review
**Started:** 2026-11-08
**Target Completion:** TBD
**Last Updated:** 2026-11-08 19:48

---

## Quality Bar

**Target quality:** An input adapter and a pure Core math helper. input-actions binds a stick and a trigger to these stored values with no deadzone of its own, control-schemes reads a value that is not 0 as gamepad input, and binding-remap listens for a value above the deadzone. A wrong formula gives a drift of the map camera or a dead trigger in each later feature. A hidden allocation fails the HelloPanel gate in each frame.

- **Correct math:** Each formula of the helper has one rule, and a test on hand-made values pins each rule: the zero inside the deadzone, the rescale outside it, the kept direction, the clamp to length 1, the map of the trigger from -1 to 1 onto 0 to 1, the range check, and the zero for an input that is not finite. A plant fails each rule test.
- **A stable contract:** The stored value of each axis is the value after the deadzone (Q1). The sticks hold -1 to 1 with a length of 1 or less. The triggers hold 0 to 1 (Q2). The Y sign is the GLFW sign. The remarks of `UiGamepadState` state each rule, so input-actions reads the values with no guess.
- **0 bytes after warm-up:** `PollGamepads`, the conversion, the helper, and the new reads allocate 0 bytes in the default Debug build. Three 0-byte tests in `BrutalUi.Glfw.Tests` measure them, and two plants fail them. The live poll test grows to cover `PollGamepads` (A7).
- **A small named diff:** Core gets one new file (`UiGamepadDeadzone.cs`), one new enum in `UiEventEnums.cs`, and three reads in `UiGamepadState.cs`. `UiContext.cs`, `UiInputState.cs`, and `InputStateWriter.cs` do not change (A1). `BrutalUi.Glfw` changes `GlfwUiInput.cs` only. No project file changes.
- **Accurate docs:** Section 9 of the technical design states the axes, the formulas, the settings, the absent rule, and the frame rule. Each public member has an XML doc comment in ASD-STE100 prose.

---

## Executive Summary

This feature fills the 16 gamepad slots that input-state added. `GlfwUiInput.PollGamepads()` reads each GLFW slot one time, applies the deadzones, and writes the result through `UiContext.SetGamepad`. The game calls it after `Glfw.PollEvents` and before `Update`:

- Core gets `UiGamepadAxis` (six values, 0 to 5) and `UiGamepadDeadzone`, a static helper with a radial deadzone for a stick and a linear deadzone for a trigger.
- `UiGamepadState` gets `Axis(UiGamepadAxis)`, `LeftStick`, and `RightStick`. `Axis(int)` stays.
- `GlfwUiInput` gets `StickDeadzone` (default 0.15), `TriggerDeadzone` (default 0.1), and `PollGamepads()`. An absent slot, a slot with no gamepad mapping, and a disposed adapter write `default`, so the connected flag is `false` and a held button gets its released edge.
- `PollGamepad(int)` and `UiActionMap.PushGamepad` stay. HelloPanel calls `PollGamepads()`, pushes `ui.Input.Gamepad(0).Buttons` into the map, and shows the left stick in a fourth label.

The plan has 3 phases and 22 tasks. Phase 1 (6 tasks) delivers the Core helper, the enum, the reads, and 9 Core tests. Phase 2 (8 tasks) delivers the poll, the settings, 8 Glfw tests, and the growth of the live test. Phase 3 (8 tasks in two groups) delivers the HelloPanel label, section 9 and section 14 of the technical design, and the changelog. Each phase runs on a STANDARD dev agent.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (calls `input.PollGamepads()` one time in each frame and reads `ui.GameInput.Gamepad(0).LeftStick` after `Update`, with no deadzone code of its own)
- [x] **The sibling features** (input-actions binds a stick to a Vector2 action and a trigger to an Axis action from the stored values, control-schemes treats a value that is not 0 as gamepad input, and binding-remap listens for the next value above 0)
- [x] **The spacegame map-controls consumer** (pans and zooms the map with the sticks through input-actions, with no drift at rest)

**User Subsets Served:**
- [x] **Simple** - a game reads `LeftStick` and moves a cursor. A stick at rest reads (0, 0).
- [x] **Complex** - a game sets `StickDeadzone` from an options screen, reads four pads, and sees a pad that a player unplugs go to `Connected == false` with a released edge for each held button.

**UX Approach:**
- Build time: nothing new. The adapter starts with the default deadzones.
- Frame time: `Glfw.PollEvents(); input.PollGamepads(); ui.Update(dt);` and then the reads. One new line in the frame loop.
- No event, no thread, no builder, and no allocation in the frame path.

---

## Goals

### Primary Goals
1. Add `UiGamepadAxis` and the pure helper `UiGamepadDeadzone` to Core (A1).
2. Add `Axis(UiGamepadAxis)`, `LeftStick`, and `RightStick` to `UiGamepadState`, and state the stored value rules (A2, Q1, Q2).
3. Add `PollGamepads()` and the two deadzone settings to `GlfwUiInput` (A3, A4, A5, A8).
4. Prove 0 bytes for the poll, and cover the absent slot with a Glfw test (A7).
5. Show the left stick in HelloPanel, and state the rules in section 9 of the technical design and in the changelog (A6, A9 of the epic).

### Success Criteria
- [x] The phase 1 demo passes: `GamepadDeadzoneTests` reads a total of 9 with `failed: 0`.
- [x] The phase 2 demo passes: `GlfwUiInputGamepadPollTests` reads a total of 8 with `failed: 0`.
- [ ] The phase 3 demo passes on a host with a window and a gamepad: the label reads `Left stick: 0.00, 0.00` at rest and `Left stick: 1.00, 0.00` with the stick full right. (Open: Needs Human Verification — the host has no display.)
- [x] Each existing test passes. The one edited existing test is `ALivePollAllocatesNothing` with its `Poll` helper (A7).
- [x] The Core diff is `UiEventEnums.cs` (one added enum), `UiGamepadState.cs`, and the new `UiGamepadDeadzone.cs`.

---

## Architecture Overview

### The Place in the Epic

input-state gives the 16 slots, `UiGamepadState`, its public constructor, and the write seam `UiContext.SetGamepad`. This feature writes the slots. input-actions reads `GameInput.Gamepad(i)` in `UiActionMap.Update` and uses the stored values as they are (Q1).

```
Frame N (0 bytes after warm-up)
  Glfw.PollEvents()
  input.PollGamepads()                         (phase 2)
    for id in 0..15:
      not disposed and GetGamepadState(id) is true:
        buttons = GamepadButtons(state.Buttons)          (existing conversion)
        axes    = state.Axes, six floats, read as a span (A5)
        sticks  = UiGamepadDeadzone.ApplyStick(..., StickDeadzone)      (phase 1 helper)
        triggers = UiGamepadDeadzone.ApplyTrigger(..., TriggerDeadzone)
        slot = new UiGamepadState(buttons, six values)   Connected = true
      else: slot = default                               Connected = false (A4, A8)
      ui.SetGamepad(id, in slot)               raw slot and edges at once (input-state)
  ui.Update(dt)                                the end of the drain copies the slots to GameInput
  the game reads ui.GameInput.Gamepad(0).LeftStick
```

### File Tree (end state)

```
src/BrutalUi.Core/Input/
├── UiEventEnums.cs                 # phase 1: UiGamepadAxis, after UiGamepadButtons
├── UiGamepadDeadzone.cs            # phase 1 (new): ApplyStick, ApplyTrigger
└── UiGamepadState.cs               # phase 1: Axis(UiGamepadAxis), LeftStick, RightStick, the remarks
src/BrutalUi.Glfw/
└── GlfwUiInput.cs                  # phase 2: StickDeadzone, TriggerDeadzone, PollGamepads, GamepadState, the remarks
samples/HelloPanel/
├── HelloScreen.cs                  # phase 3: the stick label, StickText
├── HelloModes.cs                   # phase 3: StickLabel
└── Program.cs                      # phase 3: PollGamepads, the push from the slot, the label write
tests/BrutalUi.Core.Tests/Input/
└── GamepadDeadzoneTests.cs         # phase 1 (new): 9 tests
tests/BrutalUi.Glfw.Tests/
├── GlfwUiInputGamepadPollTests.cs  # phase 2 (new): 8 tests
└── GlfwUiInputGamepadTests.cs      # phase 2: the Poll helper and ALivePollAllocatesNothing only (A7)
docs/technical-design.md            # phase 3: section 9, section 14
CHANGELOG.md                        # phase 3
docs/features/input/gamepad-axes/context.md   # each phase
```

These files do not change: `UiContext.cs`, `UiInputState.cs`, `InputStateWriter.cs` (A1), each other file of `src/BrutalUi.Core/`, each file of `src/BrutalUi.Framework/` (also `UiActionMap.cs`), `HelloState.cs`, `HelloScreen.css`, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, and `scripts/`.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public enum UiGamepadAxis                    // in UiEventEnums.cs. Value i is the GLFW gamepad axis i
{
    LeftStickX = 0,                          // GLFW_GAMEPAD_AXIS_LEFT_X
    LeftStickY = 1,                          // GLFW_GAMEPAD_AXIS_LEFT_Y. GLFW gives -1 for up
    RightStickX = 2,                         // GLFW_GAMEPAD_AXIS_RIGHT_X
    RightStickY = 3,                         // GLFW_GAMEPAD_AXIS_RIGHT_Y
    LeftTrigger = 4,                         // GLFW_GAMEPAD_AXIS_LEFT_TRIGGER
    RightTrigger = 5,                        // GLFW_GAMEPAD_AXIS_RIGHT_TRIGGER
}

public static class UiGamepadDeadzone
{
    public static float2 ApplyStick(float2 stick, float deadzone);   // radial. Result length 0 to 1
    public static float ApplyTrigger(float value, float deadzone);   // GLFW -1 to 1 in, 0 to 1 out
}

public struct UiGamepadState                 // stays 40 bytes. No new field
{
    // existing: AxisCount, the constructor, Connected, Buttons, Pressed, Released, Axis(int)
    public readonly float Axis(UiGamepadAxis axis);     // Axis((int)axis). Outside 0 to 5 throws, ParamName "axis"
    public readonly float2 LeftStick { get; }           // (Axis 0, Axis 1)
    public readonly float2 RightStick { get; }          // (Axis 2, Axis 3)
}
```

```csharp
namespace BrutalUi.Glfw;

public sealed class GlfwUiInput
{
    public float StickDeadzone { get; set; }      // default 0.15. NaN or outside 0 to 1 throws ArgumentOutOfRangeException, ParamName "value"
    public float TriggerDeadzone { get; set; }    // default 0.1. The same check
    public void PollGamepads();                   // reads the 16 slots and calls Context.SetGamepad 16 times
    // existing and unchanged: PollGamepad(int gamepadId = 0)

    internal static UiGamepadState GamepadState(
        ReadOnlySpan<byte> buttons, ReadOnlySpan<float> axes, float stickDeadzone, float triggerDeadzone);
}
```

The `{ get; }` lines show the shape only. Each property is an expression body over a private field or the axes.

### The Deadzone Formulas

`d` is the deadzone. Both methods first check `d`: NaN or a value outside 0 to 1 throws `ArgumentOutOfRangeException` with the parameter name `deadzone`.

**`ApplyStick(stick, d)`** (radial, Decision 2):
```
if x or y is not finite: return (0, 0)
len = sqrt(x*x + y*y)
if !(len > d) or d >= 1: return (0, 0)          // the "!(>)" form also catches a NaN
s = min((len - d) / (1 - d), 1)                  // the length outside the deadzone, scaled to 0 to 1
k = s / len
return (x * k, y * k)                            // the same direction, length s
```

**`ApplyTrigger(value, d)`** (linear, Decision 3):
```
if value is not finite: return 0
t = clamp((value + 1) * 0.5, 0, 1)               // GLFW -1 to 1 onto 0 to 1
if !(t > d) or d >= 1: return 0
return (t - d) / (1 - d)                         // 0 to 1. The deadzone sits at the low end (Q2)
```

Worked values for the tests: `ApplyStick((0.1, 0.05), 0.15)` gives (0, 0). `ApplyStick((0.575, 0), 0.15)` gives (0.5, 0). `ApplyStick((0.3, 0.4), 0.15)` gives (0.247059, 0.329412). `ApplyStick((1, 1), 0.15)` gives (0.707107, 0.707107). `ApplyTrigger(-1, 0.1)` gives 0. `ApplyTrigger(0, 0.1)` gives 0.444444. `ApplyTrigger(0.5, 0.1)` gives 0.722222. `ApplyTrigger(1, 0.1)` gives 1.

### Internal Shape

- **`UiGamepadDeadzone` (phase 1).** `public static class` in `src/BrutalUi.Core/Input/UiGamepadDeadzone.cs`. Two methods, no state, no field. The math uses `float` components and `MathF.Sqrt`, and builds the result with `new float2(x, y)`. It uses no operator of `float2` that the code does not use today.
- **The reads (phase 1).** `Axis(UiGamepadAxis axis) => Axis((int)axis)`, so the range check and the parameter name `axis` stay in one place. `LeftStick => new(_axes[0], _axes[1])`. `RightStick => new(_axes[2], _axes[3])`. The struct keeps its fields and its 40 bytes.
- **The conversion (phase 2).** `internal static UiGamepadState GamepadState(...)` in the pattern of `GamepadButtons`. It throws `ArgumentException` with the parameter name `axes` when `axes` holds fewer than six values. It calls `GamepadButtons(buttons)`, applies `ApplyStick` to (0, 1) and (2, 3) and `ApplyTrigger` to 4 and 5, writes the six results into `Span<float> values = stackalloc float[6]`, and returns `new UiGamepadState(buttons, values)`. The stack buffer is no managed allocation.
- **The poll (phase 2).** `PollGamepads()` loops `id` from 0 to `GamepadIdLast`. For each id it starts with `UiGamepadState state = default;`. When `_disposed` is `false` and `GlfwApi.GetGamepadState(id, out GlfwGamepadState raw)` gives `true`, it reads `raw.Buttons` and `raw.Axes` as spans inside `unsafe`, with no pin, as `PollGamepad` does today (A5), and calls `GamepadState`. Then it calls `Context.SetGamepad(id, in state)`. A new constant `GamepadAxisCount = 6` gives the span length. The method has no early return, so a disposed adapter writes `default` into each slot (A4).
- **The settings (phase 2).** Two backing fields `_stickDeadzone = 0.15f` and `_triggerDeadzone = 0.1f`, with private constants for the defaults. The setter checks `!(value >= 0f && value <= 1f)` and throws `ArgumentOutOfRangeException(nameof(value), value, "The deadzone must be from 0 to 1.")`. The setter works after `Dispose`, because it only sets a field. The poll reads the fields at each call.

### Integration Points

| Sibling or caller | What this feature calls or gives | Contract |
|---|---|---|
| input-state (`UiContext.SetGamepad`, done) | 16 calls in each `PollGamepads` | The one write path. The raw slot changes at once, and the edges come from the old buttons. The game view gets the slot at the end of the next drain. The gate does not apply. No change to `UiContext`, `UiInputState`, or `InputStateWriter` (A1). |
| input-state (`UiGamepadState`, done) | the public constructor, three new reads | The constructor copies six values and sets `Connected`. `default` is a disconnected slot. The 40-byte size and the 804-byte `UiInputState` stay. |
| state-and-commands (`PollGamepad`, `UiActionMap.PushGamepad`, done) | none | Both stay with no change. A game can keep `PollGamepad(0)`, or push `ui.Input.Gamepad(0).Buttons` after `PollGamepads()` (A6). The two give the same bits for slot 0. |
| input-actions (next) | the stored values, `UiGamepadAxis`, `LeftStick`, `RightStick` | The values are after the deadzone. Sticks -1 to 1, length 1 or less. Triggers 0 to 1. GLFW Y sign. The map adds no deadzone (Q1). |
| control-schemes, binding-remap (later) | the stored values | A value that is not 0 is input above the deadzone. A stick at rest and a released trigger read exactly 0. |
| HelloPanel (sample) | `PollGamepads`, `ui.Input.Gamepad(0)`, `ui.GameInput.Gamepad(0)` | The poll stays outside the UI region. The push and the label write sit inside it, so the sample check of 0 bytes covers the reads and the format. |

---

## Implementation Phases

The feature has 22 tasks, above the "about 8 tasks" of Slice rule 7, so it has three slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea: the deadzone math and the reads on a hand-made state, checked headless (Q3). Phase 2 adds the poll that writes the slots, so a Glfw regression shows in its own phase. Phase 3 adds the sample and the docs. The host has no display, so the live checks sit in phases 2 and 3.

Each verification command sets `MSBUILDDISABLENODEREUSE=1`. Give `-nodeReuse:false` to `dotnet build` only. On `dotnet test` it breaks the host handshake (Gotcha 1 of input-state). Do not run `dotnet build-server shutdown`. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `Brutal.Numerics`), also for each namespace that a `cref` names. Run one Core class with `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, and one Glfw class with `tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, after a build.

Two flakes are known. A test host can exit with 134 (Internal CLR error 0x80131506), and the Core test `TheCurvesAllocateNothing` failed one time with 8128 bytes. Run the tests a second time before you treat either as a fault. A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant. For each plant, copy the file to `src/BrutalUi.Core/obj/plantscratch/` or `src/BrutalUi.Glfw/obj/plantscratch/` (git-ignored, Gotcha 3 of input-state), run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`, expect a value that is not 0, read the failed test names from the log, restore the file with `cp`, check it with `cmp`, and run the tests again. A plant line `GC.KeepAlive(new object());` needs `using System;` in its file. Add the line and keep the plant text word for word (Decision 27 of input-state).

Float asserts use `Assert.Equal(expected, actual, 5)` (five decimal places) for each component. A test compares no `float2` value with `==`.

### Phase 1: The Deadzone Math and the Reads in Core
**Goal:** Core holds `UiGamepadAxis`, the pure helper `UiGamepadDeadzone`, and the reads `Axis(UiGamepadAxis)`, `LeftStick`, and `RightStick`. Tests on hand-made values and a hand-made state pin each formula.
**Demo:** a developer runs the built Core test binary with `--filter-class '*GamepadDeadzoneTests'` on this host and reads a total of 9 with `failed: 0`.
**Needs:** none
**Agent tier:** STANDARD (sonnet). Pure math on value types with the formulas written out in this plan.

**Tasks (one group):**
- [x] 1.1 Create `docs/features/input/gamepad-axes/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`), the solution total with the skipped count, and the totals of `BrutalUi.Core.Tests` and `BrutalUi.Glfw.Tests`. At planning time they read 2864 (3 skipped), 1786, and 32. Record the formulas and the Internal Shape of this plan.
- [x] 1.2 Edit `src/BrutalUi.Core/Input/UiEventEnums.cs`. Add `public enum UiGamepadAxis` right after `UiGamepadButtons`, with the six members and values of the Public API. The summary: the six axes of a gamepad. The remarks: value `i` is the GLFW gamepad axis `i`, so the adapter reads the GLFW axes with no table. Core reads no gamepad. Each member names its GLFW constant and value, in the style of `UiGamepadButtons`. The Y members state that GLFW gives -1 for up. The trigger members state that the stored value is 0 to 1. Change no other type in the file.
- [x] 1.3 Write `src/BrutalUi.Core/Input/UiGamepadDeadzone.cs` with `public static class UiGamepadDeadzone` and the two methods of "The Deadzone Formulas". The class remarks: the poll of `BrutalUi.Glfw` applies the helper, and the state stores the result (Q1). The helper is pure, has no state, and allocates nothing. `ApplyStick` remarks: the radial rule, the kept direction, the clamp to length 1 (a GLFW stick can give a length up to about 1.414 on a diagonal), the zero for a deadzone of 1, and the zero for a component that is not finite. `ApplyTrigger` remarks: the map of -1 to 1 onto 0 to 1, the deadzone on the 0 to 1 scale at the low end (Q2), the clamp, the zero for a deadzone of 1, and the zero for a value that is not finite. Both name the exception for a bad `deadzone`.
- [x] 1.4 Edit `src/BrutalUi.Core/Input/UiGamepadState.cs`. Add `using Brutal.Numerics;`. Add `Axis(UiGamepadAxis axis)`, `LeftStick`, and `RightStick` after `Axis(int)`, each with a doc comment. `LeftStick`: X is axis 0 and Y is axis 1, GLFW gives -1 for up, and the length is 1 or less after `PollGamepads`. Change the remarks of `Axis(int)`: the summary reads "Gets the stored value of one axis". The remark reads "`GlfwUiInput.PollGamepads` stores each value after the deadzone. A stick axis is -1 to 1, and a trigger is 0 to 1. A state from the public constructor holds the values that its caller gives." Change the class remarks paragraph on the deadzone in the same way, and name `UiGamepadAxis`. Change no field and no other member.
- [x] 1.5 Write `tests/BrutalUi.Core.Tests/Input/GamepadDeadzoneTests.cs` with 9 tests. Use the worked values of "The Deadzone Formulas". `AStickInsideTheDeadzoneReadsZero` ((0.1, 0.05), (0.15, 0), and (0, 0) with 0.15 each give (0, 0)). `AStickAtFullTiltReadsLengthOne` ((1, 0) gives (1, 0), (0, -1) gives (0, -1), and (1, 1) gives (0.707107, 0.707107), all with 0.15. With 0, (2, 0) gives (1, 0)). `TheStickRescalesTheLengthAndKeepsTheDirection` ((0.575, 0) gives (0.5, 0) and (0.3, 0.4) gives (0.247059, 0.329412), with 0.15. With 0, (0.6, 0.8) gives (0.6, 0.8)). `ATriggerMapsTheGlfwRangeToZeroToOne` (with 0: -1 gives 0, 0 gives 0.5, and 1 gives 1. 2 gives 1 and -2 gives 0). `ATriggerAppliesTheLinearDeadzoneAtTheLowEnd` (with 0.1: -1 gives 0, -0.9 gives 0, 0 gives 0.444444, 0.5 gives 0.722222, and 1 gives 1). `TheHelperRejectsADeadzoneOutsideTheRange` (-0.01, 1.01, and `float.NaN` throw `ArgumentOutOfRangeException` with `ParamName` `deadzone`, for both methods. 0 and 1 do not throw). `ADeadzoneOfOneAndANonFiniteInputReadZero` (with 1: (1, 1) gives (0, 0) and the trigger 1 gives 0. With 0.15: (NaN, 0) and (`float.PositiveInfinity`, 0) give (0, 0), and the trigger NaN and `float.PositiveInfinity` give 0). `TheStateReadsTheSticksAndTheNamedAxes` (`new UiGamepadState(UiGamepadButtons.None, [0.1f, -0.2f, 0.3f, -0.4f, 0.5f, 0.6f])` gives `LeftStick` (0.1, -0.2), `RightStick` (0.3, -0.4), `Axis(UiGamepadAxis.LeftTrigger)` 0.5, and `Axis(UiGamepadAxis.RightTrigger)` 0.6. For each enum value, `Axis(value)` equals `Axis((int)value)`. `Axis((UiGamepadAxis)6)` and `Axis((UiGamepadAxis)(-1))` throw `ArgumentOutOfRangeException` with `ParamName` `axis`. `default(UiGamepadState)` gives `LeftStick` and `RightStick` (0, 0)). `TheAxisEnumFollowsTheGlfwAxisOrder` (the six values are 0 to 5 in the order of the Public API, and `Enum.GetValues<UiGamepadAxis>().Length` equals `UiGamepadState.AxisCount`).
- [x] 1.6 Run the build, the tests, and `GamepadDeadzoneTests` (total 9). Expect `failed: 0`. Check that the 32 tests of `InputStateTests` and the 6 tests of `GlfwUiInputGamepadTests` pass with no edit. Run three plants. Plant 1: `ApplyStick` returns `stick` unchanged when the length is above the deadzone. Expect `TheStickRescalesTheLengthAndKeepsTheDirection` to fail. Plant 2: `ApplyTrigger` skips the map and uses `t = value`. Expect `ATriggerMapsTheGlfwRangeToZeroToOne` to fail. Plant 3: `RightStick` reads axes 0 and 1. Expect `TheStateReadsTheSticksAndTheNamedAxes` to fail. Record the counts, the demo, and the failed tests of each plant in `context.md`.

**Estimated Steps:** about 35 (6 tasks, about six steps for one task)

**Files (one group, 5):** `UiEventEnums.cs`, `UiGamepadDeadzone.cs`, `UiGamepadState.cs`, `GamepadDeadzoneTests.cs`, `context.md`

**Acceptance Criteria:**
- [x] The 9 tests of `GamepadDeadzoneTests` pass. Each existing test passes with no edit.
- [x] The three plants of task 1.6 give a non-zero exit code, and each restored file equals its copy under `cmp`.
- [x] `git diff` of `UiEventEnums.cs` shows added lines only. `UiContext.cs`, `UiInputState.cs`, and `InputStateWriter.cs` show no diff.

### Phase 2: The Poll of the 16 Slots
**Goal:** `GlfwUiInput.PollGamepads()` reads each of the 16 slots one time, applies the two deadzone settings, and writes each slot through `UiContext.SetGamepad`. An absent slot and a disposed adapter write `default`. The poll allocates 0 bytes.
**Demo:** a developer runs the built Glfw test binary with `--filter-class '*GlfwUiInputGamepadPollTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet). The `unsafe` read copies the existing `PollGamepad` pattern (a span over a fixed buffer of a local, with no pin). The one hazard is the global GLFW state of the null-platform test, and task 2.6 spells out the restore. An ADVANCED agent is not needed.

**Tasks (one group):**
- [x] 2.1 Edit `src/BrutalUi.Glfw/GlfwUiInput.cs`. Add the constant `GamepadAxisCount = 6` (`GLFW_GAMEPAD_AXIS_LAST` plus 1) next to `GamepadButtonCount`, the two default constants, the two backing fields, and the properties `StickDeadzone` and `TriggerDeadzone` with the check of the Internal Shape. The remarks of each: the default, the range, the rule that the next `PollGamepads` uses the new value, and the rule that the setter also works after `Dispose`. `StickDeadzone` names `UiGamepadDeadzone.ApplyStick`. `TriggerDeadzone` names `UiGamepadDeadzone.ApplyTrigger` and the 0 to 1 scale.
- [x] 2.2 Add `internal static UiGamepadState GamepadState(ReadOnlySpan<byte> buttons, ReadOnlySpan<float> axes, float stickDeadzone, float triggerDeadzone)` after `GamepadButtons`, as the Internal Shape states. The doc comment names the axis order of `UiGamepadAxis` and the `ArgumentException` for fewer than six axes.
- [x] 2.3 Add `public void PollGamepads()` after `PollGamepad`, as the Internal Shape states. Read the axes with `new ReadOnlySpan<float>(raw.Axes, GamepadAxisCount)` inside the same `unsafe` block as the buttons, with the comment of `PollGamepad` about the fixed local. When `raw.Axes` is not a fixed buffer of `float` (A5), use the access that compiles with no copy to the heap, and record the shape in `context.md`. The remarks: call it one time in each frame, on the main thread, after `Glfw.Init` and `Glfw.PollEvents`, and before `Update`. It reads each slot one time and calls `UiContext.SetGamepad` for each, so the edges come from the last poll. An absent joystick and a joystick with no gamepad mapping give `default` (A8). After `Dispose`, each call writes `default` into each slot and makes no GLFW call, so a held button gets one released edge. No hot-plug event. The call allocates nothing. It does not change `PollGamepad`.
- [x] 2.4 Edit the remarks. The gamepad paragraph of the class remarks (lines 69 to 74): `PollGamepads` writes the full state of the 16 slots into `UiContext.Input`, with the deadzones. `PollGamepad` still returns the buttons of one slot for `UiActionMap.PushGamepad`. Remove "The adapter reads no axis and applies no deadzone" from the class remarks. The remarks of `PollGamepad` keep "reads no axis" and add a `see cref` to `PollGamepads`. Keep the Guide button sentence and the no-window sentence.
- [x] 2.5 Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadPollTests.cs` with `[Collection(GlfwUiInputAllocationTests.CollectionName)]` and the seam helper of `GlfwUiInputGamepadTests`. Add 7 tests. `TheAxisBufferAndTheAxisConstantsFollowGlfw` (the `FixedBufferAttribute` of `GlfwGamepadState.Axes` has the length 6 and the element type `float`, in the pattern of the buttons check. `GlfwApi.Constants.GLFW_GAMEPAD_AXIS_LEFT_X` to `GLFW_GAMEPAD_AXIS_RIGHT_TRIGGER` equal the six `UiGamepadAxis` values, and `GLFW_GAMEPAD_AXIS_LAST + 1` is 6). `TheConversionMapsEachGlfwAxis` (deadzones 0 and 0: the axes [0.5, -0.25, -0.75, 1, -1, 1] give `LeftStick` (0.5, -0.25), `RightStick` (-0.6, 0.8), `LeftTrigger` 0, and `RightTrigger` 1. The `RightStick` value is the Decision 2 length clamp at work: (-0.75, 1) has length 1.25, so `ApplyStick` with a deadzone of 0 rescales it to length 1. The axes [0, 0, 0, 0, 0, -1] give `LeftTrigger` 0.5 and `RightTrigger` 0. The A and Start buttons pressed give `A | Start`. `Connected` is `true`). `TheConversionAppliesTheDeadzoneSettings` (deadzones 0.15 and 0.1: the axes [0.1, 0.05, 0.575, 0, 0, -1] give `LeftStick` (0, 0), `RightStick` (0.5, 0), `LeftTrigger` 0.444444, and `RightTrigger` 0. Five axes throw `ArgumentException` with `ParamName` `axes`). `TheDeadzoneSettingsCheckTheRange` (the defaults read 0.15 and 0.1. For each property, -0.01, 1.01, and `float.NaN` throw `ArgumentOutOfRangeException` with `ParamName` `value` and keep the old value. 0 and 1 set. A set after `Dispose` works). `ADisposedAdapterWritesDisconnectedSlots` (seam adapter: `Context.SetGamepad(3, new UiGamepadState(UiGamepadButtons.A, [0.5f]))` and `SetGamepad(15, ...)` first. Then `Dispose` and `PollGamepads`. Each of the 16 raw slots reads `Connected` `false`, `Buttons` `None`, and six axes of 0. Slot 3 reads `Released` `A`. A second `PollGamepads` gives `Released` `None`. The test makes no GLFW call). `ADisposedPollAllocatesNothing` (seam adapter, disposed. 2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, 100 calls of `PollGamepads`, `Assert.Equal(0L, after - before)`). `TheConversionAllocatesNothing` (the button and axis arrays exist before the measure. Each of 100 rounds calls `GamepadState` with the default deadzones, calls `Context.SetGamepad(round % 16, in state)`, and adds `LeftStick.X`, `RightStick.X`, and `Axis(UiGamepadAxis.LeftTrigger)` as a rounded `int` to a local sum. Assert 0 bytes, then assert the sum, so an empty body cannot pass). The measured bodies hold no lambda, no string, no array creation, and no `Assert`.
- [x] 2.6 Add the eighth test `TheNullPlatformPollWritesSixteenAbsentSlots`. It covers the absent slot on a host with no display. Steps: check `GlfwApi.PlatformSupported(GlfwPlatform.Null)` and skip with a message when it is `false`. Set the init hint `GLFW_PLATFORM` (0x00050003) to `GLFW_PLATFORM_NULL` (0x00060005) with `GlfwApi.SetInitHint`. Use the named enum members when the binding has them, and else cast the constants to `GlfwInitHint` and `int`. Call `GlfwApi.Init()`, and skip with the GLFW error when it fails. Write `A` into slot 0 through `SetGamepad` first. Run 2 warm-up calls of `PollGamepads`, release the allocation context, and measure 100 calls at 0 bytes. After the measure, assert that each of the 16 raw slots reads `Connected` `false` with no button and no axis, and that `PollGamepad(id)` gives `None` for each id. In `finally`: `input.Dispose()`, `GlfwApi.Terminate()`, and set the hint back to `GLFW_ANY_PLATFORM` (0x00060000), so the later GLFW tests of the collection start on the normal platform. Record in `context.md` the hint spelling that compiled and the result on this host (run or skip).
- [x] 2.7 Edit `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs` (A7). The `Poll` helper calls `input.PollGamepads()` and then `last = input.PollGamepad()` in each iteration. In `ALivePollAllocatesNothing`, after the existing absent-id assert, add: `input.PollGamepads()`, then `Assert.False(input.Context.Input.Gamepad(absentId).Connected)` and `Assert.Equal(UiGamepadButtons.None, input.Context.Input.Gamepad(absentId).Buttons)`. Update the summary of the class and of the helper: the live poll measures both polls. Change no other test.
- [x] 2.8 Run the build, the tests, and `GlfwUiInputGamepadPollTests` (total 8) and `GlfwUiInputGamepadTests` (total 6). Expect `failed: 0`. Run `xvfb-run -a tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*GlfwUiInputGamepadTests' 2>&1 | tail -n 5` one time, and record the result. The sandbox can block it (Gotcha 3 of input-state). Run five plants. Plant 4: `GC.KeepAlive(new object());` as the first line of `PollGamepads`. Expect `ADisposedPollAllocatesNothing` to fail, and `TheNullPlatformPollWritesSixteenAbsentSlots` too when it runs. Plant 5: `if (_disposed) { return; }` as the first line of `PollGamepads`. Expect `ADisposedAdapterWritesDisconnectedSlots` to fail. Plant 6: `GamepadState` passes the two triggers in the wrong order (axis 5 to slot 4, axis 4 to slot 5). Expect `TheConversionMapsEachGlfwAxis` to fail. Plant 7: `GC.KeepAlive(new object());` as the first line of `UiGamepadDeadzone.ApplyStick`. Expect `TheConversionAllocatesNothing` to fail. Plant 8: the `StickDeadzone` setter skips the range check. Expect `TheDeadzoneSettingsCheckTheRange` to fail. Record the failed tests and the measured bytes of each plant in `context.md`.

**Estimated Steps:** about 55 (8 tasks, about seven steps for one task)

**Files (one group, 5):** `GlfwUiInput.cs`, `GlfwUiInputGamepadPollTests.cs`, `GlfwUiInputGamepadTests.cs`, `context.md`, `UiGamepadDeadzone.cs` (plant 7 only, then restored)

**Acceptance Criteria:**
- [x] The 8 tests of `GlfwUiInputGamepadPollTests` pass, or 7 pass and the null-platform test skips with its message and `context.md` records why. The 6 tests of `GlfwUiInputGamepadTests` pass or skip as at the base. (8 passed with 0 skipped: the null-platform test runs on this host.)
- [x] The five plants of task 2.8 give a non-zero exit code, and each restored file equals its copy under `cmp`.
- [x] `git diff` of `GlfwUiInputGamepadTests.cs` shows changes only in the class summary, the `Poll` helper, and `ALivePollAllocatesNothing`.

### Phase 3: The HelloPanel Stick Label and the Docs
**Goal:** HelloPanel calls `PollGamepads()` one time in each frame, pushes `ui.Input.Gamepad(0).Buttons` into the map, and shows the left stick of slot 0 in a fourth label. Section 9 and section 14 of the technical design and the changelog state the rules.
**Demo:** a developer runs HelloPanel on a host with a window and a gamepad in slot 0, tilts the left stick full right, and reads `Left stick: 1.00, 0.00`. At rest the label reads `Left stick: 0.00, 0.00`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet). The label copies the pattern of the input label.

**Tasks (group 1, the sample):**
- [x] 3.1 Edit `samples/HelloPanel/HelloScreen.cs`. `BuildWidgetPanel` gets the out parameter `out Label stickLabel` after `inputLabel`. The label is not pickable, comes right after the input label and before the image, and starts with `LongestStickText`, so its buffer never grows. Add `public static ReadOnlySpan<byte> StickText(in UiGamepadState pad, Span<byte> scratch)`. It writes `Left stick: <x>, <y>` and returns the written part. Each component goes through a private `StickComponent(float value)`: a value that is not finite gives 0, then the value clamps to -1 to 1, then `MathF.Round(value, 2) + 0f`. The `+ 0f` turns -0 into +0, so the label never shows `-0.00`. `float.TryFormat` writes it with the format `"F2"` and `CultureInfo.InvariantCulture`, in the throw pattern of `InputText`. `LongestStickText` is `"Left stick: -1.00, -1.00"u8` (24 bytes). Update the class remarks (four labels, the fourth label is the stick label, the draw-command paragraph, and the child-rule paragraph) and the remarks of `BuildWidgetPanel`.
- [x] 3.2 Edit `samples/HelloPanel/HelloModes.cs`. Store the stick label of the widget factory in `_stickLabel` and expose `StickLabel`, in the pattern of `InputLabel`. Add it to the constructor check and its exception doc, and to the factory summary ("the four out fields").
- [x] 3.3 Edit `samples/HelloPanel/Program.cs`. Replace `UiGamepadButtons pad = input.PollGamepad();` with `input.PollGamepads();`, outside the UI region, with the comment updated. Replace the first line of the UI region with `state.Map.PushGamepad(ui.Input.Gamepad(0).Buttons);`. The raw slot holds the poll of this frame before `Update` (A6). Add the parameter `Label stickLabel` to `RunFrames` after `inputLabel`, pass `modes.StickLabel` at the call site, create `byte[] stickScratch = new byte[32];` next to `inputScratch`, and add `stickLabel.SetText(HelloScreen.StickText(in ui.GameInput.Gamepad(0), stickScratch));` right after the input label line, inside the UI region. Update the remarks of `RunFrames` (the frame order item 1 and item 3, the gate counts paragraph, and the parameter docs).
- [x] 3.4 Run the build and the tests. Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`, so the smoke run keeps 0 bytes in the UI region with the poll, the push, and the label. Run plant 9: `GC.KeepAlive(new object());` as the first line of `HelloScreen.StickText`. Check `0 Error(s)`. On a host with a window, expect smoke line 3 to read 2808 bytes (117 frames of 24 bytes) and exit code 1. Restore with `cmp`. When the sandbox blocks the smoke step, record the exact line and hand the PASS run, plant 9, and the demo to Needs Human Verification in `context.md`.

**Tasks (group 2, the docs):**
- [x] 3.5 Edit `docs/technical-design.md`, section 9. Change the sub-item "The gamepad seam" of item 11: `GlfwUiInput.PollGamepads` calls the seam (item 12). Add item 12 "The gamepad poll": `PollGamepads` and the frame rule (A6), the 16 slots, the connected rule (A8), the absent rule and the disposed rule (A4), `UiGamepadAxis`, the reads `LeftStick`, `RightStick`, and `Axis(UiGamepadAxis)`, the stored value rule (Q1, Q2, the GLFW Y sign), the two formulas, the two settings and their range, the 0-byte rule, and one closing sentence that names `GamepadDeadzoneTests` and `GlfwUiInputGamepadPollTests`. In the bullet **`PollGamepad`** of the action map, add one sentence: `PollGamepads` (item 11 and 12) writes the full state, and a game can push `ui.Input.Gamepad(0).Buttons` in place of the result of `PollGamepad`.
- [x] 3.6 Edit `docs/technical-design.md`, section 14. In the bullet **Action map tests.**, add that the live poll of `GlfwUiInputGamepadTests` also measures `PollGamepads` and asserts a disconnected absent slot. Add the bullet **Gamepad axes tests.** before "Sample projects": `GamepadDeadzoneTests` (9), `GlfwUiInputGamepadPollTests` (8), the three 0-byte tests (not a numbered gate, Decision 9), the null-platform test, and plants 1 to 9 with their results. In the bullet "Sample projects", change "the two label updates" to "the three label updates", name the gamepad push from the slot, and change "The gamepad poll runs after" to name `PollGamepads`.
- [x] 3.7 Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`: `UiGamepadAxis` and `UiGamepadDeadzone`, the six axes and the deadzone helper (input/gamepad-axes). `UiGamepadState.Axis(UiGamepadAxis)`, `LeftStick`, and `RightStick` (input/gamepad-axes). `GlfwUiInput.PollGamepads`, `StickDeadzone`, and `TriggerDeadzone`, the poll of the 16 gamepad slots with the deadzones (input/gamepad-axes). Under `### Changed`: the slots of `UiInputState` hold the values after the deadzone when `PollGamepads` writes them, and a trigger holds 0 to 1 (input/gamepad-axes).
- [x] 3.8 Run the build, the tests, and the demo commands of phases 1 and 2 (totals 9 and 8, each with `failed: 0`). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes. Record in `context.md` the epic spelling note of Notes gap 6 for `/update-epic`, the results, and each decision that this plan did not settle.

**Estimated Steps:** about 45 (8 tasks, about five to six steps for one task)

**Files (group 1, 4):** `HelloScreen.cs`, `HelloModes.cs`, `Program.cs`, `context.md`
**Files (group 2, 4):** `technical-design.md`, `CHANGELOG.md`, `context.md`, `implementation.md`

**Acceptance Criteria:**
- [x] The build gives `0 Warning(s)` and `0 Error(s)`, and each test passes.
- [x] `scripts/test-linux.sh` prints its PASS line, or `context.md` records the sandbox line and hands the run to a person.
- [x] Plant 9 builds with `0 Error(s)` and restores equal under `cmp`.
- [x] Section 9 holds item 12, and section 14 holds the bullet **Gamepad axes tests.**. `CHANGELOG.md` names each new public member.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Where the deadzone applies and what the state stores: The poll applies the deadzone. Core holds the pure helper, and the state stores the result.
- Q2 The trigger range after the poll: A trigger stores 0 to 1, with the linear deadzone at the low end.
- Q3 The demo line of phase 1: The phase 1 demo is the Core deadzone test filter with `failed: 0` on this host.
- A1 Core adds `UiGamepadAxis` (six values, 0 to 5) and `UiGamepadDeadzone`, a static helper. Core changes no other type.
- A2 `UiGamepadState` gets `Axis(UiGamepadAxis)`, and the `float2` reads `LeftStick` and `RightStick`. The `Axis(int)` read stays.
- A3 `GlfwUiInput` gets `StickDeadzone` (default 0.15) and `TriggerDeadzone` (default 0.1). A value outside 0 to 1 throws.
- A4 `PollGamepads()` calls `GetGamepadState` for each of the 16 slots and `SetGamepad` for each. An absent slot and a disposed adapter write `default`, so a held button gets its released edge.
- A5 `GlfwGamepadState` of `Ahwoo.BRUTAL.Glfw` 2026.10.4 holds `Axes` as a fixed buffer of six floats, next to `Buttons`. The poll reads it as a span, with no pin.
- A6 HelloPanel calls `PollGamepads()` one time and pushes `ui.Input.Gamepad(0).Buttons` into the map. It drops the `PollGamepad(0)` call.
- A7 The live poll test of `GlfwUiInputGamepadTests` grows to cover `PollGamepads`. The 0-byte gate stays in `BrutalUi.Glfw.Tests`, and a plant guards it.
- A8 The connected flag of each slot comes from the `GetGamepadState` result. A joystick with no gamepad mapping reads as not connected.

### Decision 1: The poll applies the deadzone, and the state stores the result (Q1)
**Choice:** `PollGamepads` calls the Core helper and stores the result in each slot. The settings live on `GlfwUiInput`.
**Rationale:** Each reader then sees one value. input-actions, control-schemes, and binding-remap read the slots with no setting of their own, and a zero always means no input. The helper is pure, so the Core tests prove the math with no GLFW.
**Alternatives:** (a) The state stores the raw value and the reads apply the deadzone: each reader needs the setting, and two readers can disagree. (b) The math lives in `BrutalUi.Glfw`: the Core tests cannot reach it, and a second adapter repeats it.

### Decision 2: The radial stick deadzone rescales the length and keeps the direction
**Choice:** The formula of "The Deadzone Formulas": zero at a length up to `d`, then `(len - d) / (1 - d)`, clamped to 1, along the input direction.
**Rationale:** A radial zone has no cross-shaped dead band, so a diagonal starts as smoothly as an axis. The rescale removes the jump at the edge of the zone. The clamp keeps the length at 1 or less, because a GLFW stick with a square gate gives a length up to about 1.414. A map camera then gets the same top speed in each direction.
**Alternatives:** (a) An axial deadzone for each component: a cross-shaped dead band and a slow start near an axis. (b) A radial zone with no rescale: a jump from 0 to `d` at the edge.

### Decision 3: The trigger maps to 0 to 1 first, and the linear deadzone applies on that scale (Q2)
**Choice:** `t = clamp((value + 1) / 2, 0, 1)`, then zero up to `d`, then `(t - d) / (1 - d)`.
**Rationale:** GLFW gives -1 for a released trigger and 1 for a full pull. A 0 to 1 value is what an Axis action wants, and a released trigger reads exactly 0. The deadzone on the 0 to 1 scale reads as a share of the pull: 0.1 is the first tenth.
**Alternatives:** A deadzone on the GLFW scale: 0.1 would then be a twentieth of the pull, and the setting would mean another thing than the stick setting.

### Decision 4: The edge rules of the helper
**Choice:** A deadzone of NaN or outside 0 to 1 throws `ArgumentOutOfRangeException` with the parameter `deadzone`. A deadzone of 1 gives 0 for each input. An input that is not finite gives 0.
**Rationale:** The helper is public, so it checks its argument as each public Core method does. A deadzone of 1 would divide by 0, and "the full range is dead" is the natural reading. GLFW never gives NaN, but a hand-made state can, and a zero keeps a NaN out of a camera. The `!(len > d)` form catches a NaN length with no extra branch.
**Alternatives:** Clamp a bad deadzone: it hides a bug in an options screen.

### Decision 5: `UiGamepadAxis` lives in `UiEventEnums.cs`, with the stick names
**Choice:** The enum goes right after `UiGamepadButtons`, with the names `LeftStickX`, `LeftStickY`, `RightStickX`, `RightStickY`, `LeftTrigger`, and `RightTrigger`.
**Rationale:** The epic overview calls this "the Core enum", and `UiEventEnums.cs` holds the other gamepad enum. The names follow the requirement ("left stick X and Y") and the GLFW order, so the adapter casts with no table. A Glfw test pins each value to its GLFW constant, because Core cannot reference GLFW.
**Alternatives:** A new file for the enum: one more file for 20 lines.

### Decision 6: The conversion is an internal static method of the adapter
**Choice:** `GlfwUiInput.GamepadState(buttons, axes, stickDeadzone, triggerDeadzone)` builds the slot from the GLFW values. `PollGamepads` calls it for each present slot.
**Rationale:** It follows `GamepadButtons`, which the Glfw tests already call with hand-made arrays. The axis order of GLFW (which index is a stick and which a trigger) is the main bug risk of the adapter, and this seam tests it with no gamepad and no display.
**Alternatives:** (a) A Core helper that takes six values: Core then knows the GLFW axis layout. (b) No seam: the order test needs a real gamepad.

### Decision 7: `PollGamepads` writes all 16 slots in each call, with no early return
**Choice:** Each call writes each slot. An absent slot and each slot of a disposed adapter get `default`.
**Rationale:** A4 asks for a released edge when a pad goes away or the adapter is disposed. One write for each slot in each frame also matches the contract of `SetGamepad` ("one time for each slot in each frame"), so the edges always describe one frame. The disposed path makes no GLFW call, so the seam tests run headless.
**Alternatives:** Skip the write for an absent slot: a pad that a player unplugs keeps its last buttons.

### Decision 8: The absent slot test uses the GLFW null platform
**Choice:** `TheNullPlatformPollWritesSixteenAbsentSlots` starts GLFW with the init hint `GLFW_PLATFORM_NULL`, polls, and resets the hint to `GLFW_ANY_PLATFORM` in `finally`. It skips when the platform is not supported or the init fails.
**Rationale:** The requirement asks for a Glfw test of the absent slot, and this host has no display, so the live test always skips here. The null platform of GLFW 3.4 needs no display and has no joystick, so each slot is absent and the GLFW `false` path runs. The test also measures the 0 bytes of the GLFW path headless, so plant 4 fails a test on this host. `GlfwPlatform.Null` exists in the binding ("useful for headless / testing builds"). The class sits in the collection with `DisableParallelization`, so no other GLFW test runs at the same time.
**Alternatives:** Only the live test: no proof of the absent slot and of the GLFW path on this host.

### Decision 9: The 0-byte tests are not a numbered gate
**Choice:** `ADisposedPollAllocatesNothing`, `TheConversionAllocatesNothing`, and the measure of the null-platform test are 0-byte tests inside `GlfwUiInputGamepadPollTests`. Section 14 names them as "0-byte tests, not a numbered gate".
**Rationale:** A7 keeps the gate in `BrutalUi.Glfw.Tests`, and the live poll test of the same project is a 0-byte test with no ordinal today. The panel and slot features use the same wording. Plants 4 and 7 guard them.
**Alternatives:** A twentieth gate class: one more file for three tests.

### Decision 10: HelloPanel shows the stick in a fourth label and reads the game view
**Choice:** A new label after the input label shows `Left stick: <x>, <y>` with two decimals, from `ui.GameInput.Gamepad(0).LeftStick`. The push into the map reads `ui.Input.Gamepad(0).Buttons` before `Update`.
**Rationale:** A label of its own keeps the input label text and its docs as input-state left them, and the stick text stays short (24 bytes). The game view is what input-actions reads, and the slots get no gate, so after `Update` both views hold the same values. Before `Update` only the raw slot holds the poll of this frame, so the push reads `ui.Input` (A6). Two decimals show the deadzone at work: a small tilt still reads `0.00`.
**Alternatives:** (a) Append the stick to the input label: a 74-byte line, a wider panel, and a change of the input-state text. (b) An integer scale: the label then hides values below 1.

### Decision 11: The Y sign stays the GLFW sign
**Choice:** The poll stores the GLFW Y value with no flip. The remarks state that GLFW gives -1 for up.
**Rationale:** The requirement says that the value is the GLFW value after the deadzone. A flip is a choice of the game or of an input-actions binding. One rule from GLFW to the state keeps the adapter a plain poll.
**Alternatives:** Flip Y in the poll: a hidden rule, and the raw GLFW docs no longer match.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| `GlfwGamepadState.Axes` is not a fixed buffer of six floats (A5). | Low | Medium | The DLL holds an `Axes` member, and the user confirmed A5. Task 2.3 uses the access that compiles with no heap copy and records it. `TheAxisBufferAndTheAxisConstantsFollowGlfw` pins the length, so a new package version that changes it fails a test. |
| The binding has no named member for the platform init hint. | Medium | Low | Task 2.6 casts the GLFW constants 0x00050003, 0x00060005, and 0x00060000. A cast to an enum always compiles. |
| The null-platform hint leaks into a later GLFW test. | Low | Medium | `finally` resets the hint to `GLFW_ANY_PLATFORM`. The collection runs with no parallel test. The live tests of the collection still run on a display host after it. |
| The null platform does not init on this host, so no test covers the GLFW path headless. | Low | Medium | The test skips with the GLFW error, and `context.md` records it. `ADisposedPollAllocatesNothing` still fails plant 4. The live test covers the GLFW path under `xvfb-run -a` on a host with a working display. |
| A pad mapping with no trigger gives the GLFW value 0, and the trigger reads 0.444 with the default deadzone. | Low | Low | GLFW reports 0 for an unavailable axis. Common SDL mappings hold both triggers. The remarks of `TriggerDeadzone` state the case. Notes question 2 holds the open choice. |
| A GLFW error on a call before `Glfw.Init` throws in the binding. | Low | Low | The remarks of `PollGamepads` name the init order, as `PollGamepad` does. The seam tests use the disposed path, which makes no GLFW call. |
| A float assert fails on the last bit. | Medium | Low | Each float assert uses five decimal places. The boundary values of the tests are exact in `float` (0.15 against 0.15, and -0.9 in place of -0.8 for the trigger). |
| `float.TryFormat` with `"F2"` allocates. | Low | Medium | The UTF-8 overload writes into the span with the invariant culture and boxes nothing. Plant 9 and the smoke check of line 3 catch an allocation on a host with a window. |
| The sandbox blocks the smoke step and the live test. | High | Low | Gotcha 3 of input-state. The null-platform test runs headless. `context.md` hands the smoke PASS run, plant 9, and the demo to a person. |
| The test host exits with 134, or `TheCurvesAllocateNothing` fails one time. | Medium | Low | Run the tests a second time. |
| `dotnet build` or `dotnet test` hangs on stale MSBuild nodes. | High | Low | Each command sets `MSBUILDDISABLENODEREUSE=1`, and `dotnet build` gets `-nodeReuse:false`. No `dotnet build-server shutdown`. |

---

## Dependencies

### Internal Dependencies
- input-state (done): `UiGamepadState`, its public constructor, the 16 slots, `UiInputState.Gamepad(int)`, and `UiContext.SetGamepad`.
- state-and-commands (done): `UiGamepadButtons`, `PollGamepad`, `GamepadButtons`, and `UiActionMap.PushGamepad` (no change).
- The test helpers `AllocationGate.cs` and `RecordingRenderer.cs` in `BrutalUi.Core.Tests`, and the collection `GlfwUiInputAllocationTests.CollectionName` (done).
- dev-ops: `brutal-ui.sln` and `scripts/test-linux.sh` (no change).

### External Dependencies
- `Ahwoo.BRUTAL.Glfw` 2026.10.4 (pinned): `Glfw.GetGamepadState`, `GlfwGamepadState.Buttons` and `Axes`, the `GLFW_GAMEPAD_AXIS_*` constants, `Glfw.SetInitHint`, `Glfw.PlatformSupported`, and `GlfwPlatform.Null`. No new package and no native dependency.

### Sibling Features That Depend on This One
- input-actions (the stored values, `UiGamepadAxis`, `LeftStick`, and `RightStick` for the stick and trigger bindings).
- control-schemes and binding-remap, through input-actions and the zero rule.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. `UiGamepadDeadzone.ApplyStick` with 0.15 gives (0, 0) for (0.1, 0.05) and for (0.15, 0), (1, 0) for (1, 0), (0, -1) for (0, -1), (0.5, 0) for (0.575, 0), (0.247059, 0.329412) for (0.3, 0.4), and (0.707107, 0.707107) for (1, 1), each to five decimals (`GamepadDeadzoneTests`).
- [ ] F2. `UiGamepadDeadzone.ApplyTrigger` with 0 gives 0 for -1, 0.5 for 0, and 1 for 1. With 0.1 it gives 0 for -1 and for -0.9, 0.444444 for 0, 0.722222 for 0.5, and 1 for 1 (`GamepadDeadzoneTests`).
- [ ] F3. Both helper methods throw `ArgumentOutOfRangeException` with `ParamName` `deadzone` for -0.01, 1.01, and NaN. A deadzone of 1 gives 0. A NaN or infinite input gives 0 (`GamepadDeadzoneTests`).
- [ ] F4. `UiGamepadAxis` holds six values, 0 to 5, equal to `GLFW_GAMEPAD_AXIS_LEFT_X` to `GLFW_GAMEPAD_AXIS_RIGHT_TRIGGER`. `UiGamepadState.LeftStick` reads axes 0 and 1, `RightStick` reads axes 2 and 3, and `Axis(UiGamepadAxis)` equals `Axis(int)`. A value outside 0 to 5 throws with `ParamName` `axis` (`GamepadDeadzoneTests`, `GlfwUiInputGamepadPollTests`).
- [ ] F5. `GlfwUiInput.StickDeadzone` reads 0.15 and `TriggerDeadzone` reads 0.1 on a new adapter. A set of -0.01, 1.01, or NaN throws `ArgumentOutOfRangeException` with `ParamName` `value` and keeps the old value (`GlfwUiInputGamepadPollTests`).
- [ ] F6. The conversion maps the GLFW axes [0.5, -0.25, -0.75, 1, -1, 1] with deadzones 0 to `LeftStick` (0.5, -0.25), `RightStick` (-0.6, 0.8) — the Decision 2 clamp to length 1 on an input of length 1.25, `LeftTrigger` 0, and `RightTrigger` 1, with `Connected` `true` (`GlfwUiInputGamepadPollTests`).
- [ ] F7. After `Dispose`, `PollGamepads` writes 16 disconnected slots with no button and no axis, a held button of a slot reads `Released` one time, and the call makes no GLFW call. On the GLFW null platform, or on a host with no gamepad, each absent slot reads `Connected` `false` after `PollGamepads` (`GlfwUiInputGamepadPollTests`, `GlfwUiInputGamepadTests`).
- [ ] F8. HelloPanel calls `PollGamepads()` one time in each frame, outside the UI region, and pushes `ui.Input.Gamepad(0).Buttons` as the first line of the UI region. A fourth label shows `Left stick: <x>, <y>` with two decimals. At rest it reads `Left stick: 0.00, 0.00`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln -nodeReuse:false` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `GamepadDeadzoneTests` holds 9 tests and `GlfwUiInputGamepadPollTests` 8. `BrutalUi.Core.Tests` holds 9 more tests than at `<base>`, and `BrutalUi.Glfw.Tests` holds 8 more. The skipped count is the count of `<base>`, plus 1 only when `context.md` records that the null-platform test skips on this host.
- [ ] Q3. The 32 tests of `InputStateTests`, the 3 tests of `InputStateAllocationTests`, and the 5 tests of `GlfwUiInputGamepadTests` other than `ALivePollAllocatesNothing` pass with no edit. Each existing allocation gate passes with no edit.
- [ ] Q4. `ADisposedPollAllocatesNothing`, `TheConversionAllocatesNothing`, and the null-platform measure (when it runs) assert 0 bytes. Plants 4 and 7 fail them.
- [ ] Q5. Each new public member has an XML doc comment. The changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q6. `UiGamepadDeadzone.cs` and the new code of `GlfwUiInput.cs` hold no array creation (the `stackalloc` is not one), no `lock`, no `event` keyword, no `ToString(` call, no `(object)` cast, and no `List<`.

### Integration Criteria
- [ ] I1. The Core diff from `<base>` holds only `src/BrutalUi.Core/Input/UiEventEnums.cs` (added lines only), `src/BrutalUi.Core/Input/UiGamepadState.cs`, and the new `src/BrutalUi.Core/Input/UiGamepadDeadzone.cs`. `UiContext.cs`, `UiInputState.cs`, and `InputStateWriter.cs` show no diff (A1). The Glfw diff holds only `src/BrutalUi.Glfw/GlfwUiInput.cs`. The sample diff holds only `HelloScreen.cs`, `HelloModes.cs`, and `Program.cs`. The test diff holds the two new files and `GlfwUiInputGamepadTests.cs`. No project file, no props file, and no file of `scripts/` changes.
- [ ] I2. `PollGamepads` writes each slot only through `Context.SetGamepad`. `grep -n 'SetGamepad' src/BrutalUi.Glfw/GlfwUiInput.cs` shows one call, inside `PollGamepads`. `PollGamepad(int)` keeps its body.
- [ ] I3. Section 9 of `docs/technical-design.md` holds item 12 with the poll, the axes, the formulas, the settings, and the absent rule. The **`PollGamepad`** bullet names `PollGamepads`. Section 14 holds the bullet **Gamepad axes tests.**. `CHANGELOG.md` names `UiGamepadAxis`, `UiGamepadDeadzone`, the three reads, `PollGamepads`, `StickDeadzone`, and `TriggerDeadzone`.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line on a host with a working display, so the smoke run keeps 0 bytes in the UI region with the push from the slot and the stick label.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. Read `<base>` and the base counts from `docs/features/input/gamepad-axes/context.md`. Do not run `dotnet build-server shutdown`. When a run exits with 134 or `TheCurvesAllocateNothing` fails, run it a second time.

1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and the skipped count of Q2.
3. `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*GamepadDeadzoneTests' 2>&1 | tail -n 5`. Expect a total of 9 with `failed: 0` (F1 to F4). `tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*GlfwUiInputGamepadPollTests' 2>&1 | tail -n 5`. Expect a total of 8 with `failed: 0` (F4 to F7). Run the Core binary with `'*InputStateTests'` (32) and the Glfw binary with `'*GlfwUiInputGamepadTests'` (6). Run both binaries with no filter and compare with the base counts plus 9 and plus 8 (Q2, Q3). Read the test names and match them to the criteria.
4. Copy `src/BrutalUi.Glfw/GlfwUiInput.cs` to a scratch folder under `src/BrutalUi.Glfw/obj/`. Add `GC.KeepAlive(new object());` as the first line of `PollGamepads`. Run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp` (Q4). Repeat with `if (_disposed) { return; }` as the first line of `PollGamepads` (F7). Copy `src/BrutalUi.Core/Input/UiGamepadDeadzone.cs`, make `ApplyStick` return `stick` for a length above the deadzone, run the same command, expect a value that is not 0, and restore it with `cmp` (F1). Run step 2 again.
5. `grep -nE 'new [A-Za-z0-9_]+\[|\block\b|\bevent\b|ToString\(|\(object\)|List<' src/BrutalUi.Core/Input/UiGamepadDeadzone.cs | grep -v '///'`. Expect no line. Read `PollGamepads` and `GamepadState` in `GlfwUiInput.cs` and check the same rule (Q6).
6. `grep -n 'SetGamepad\|PollGamepads' src/BrutalUi.Glfw/GlfwUiInput.cs samples/HelloPanel/Program.cs`. Expect one `SetGamepad` call inside `PollGamepads`, and one `PollGamepads()` call in `Program.cs` before the UI region (I2, F8). `grep -n 'PushGamepad' samples/HelloPanel/Program.cs` shows `ui.Input.Gamepad(0).Buttons`.
7. `git diff <base> --name-only -- src/ samples/ scripts/ tests/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/ tests/`. Expect only the files of I1. `git diff <base> -- src/BrutalUi.Core/Tree/UiContext.cs src/BrutalUi.Core/Input/UiInputState.cs src/BrutalUi.Core/Input/InputStateWriter.cs src/BrutalUi.Framework/`. Expect no output (I1). `git diff <base> -- src/BrutalUi.Core/Input/UiEventEnums.cs | grep -E '^-[^-]'`. Expect no line.
8. `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4). In a sandbox that blocks the X11 socket, the smoke step fails with `GLFW init failed: PlatformUnavailable`. Then run the step on a host with a working display, or under `xvfb-run -a` in a normal shell.
9. The manual check of HelloPanel (F8) on a host with a window: run `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. With no gamepad, read `Left stick: 0.00, 0.00` in the fourth label. With a gamepad in slot 0, leave the stick at rest and read `0.00, 0.00`. Tilt it a little and read `0.00, 0.00`. Push it full right and read `1.00, 0.00`. Push it full up and read `0.00, -1.00`. Press B and see the Cancel action reset the score, as before. Unplug the pad and read `0.00, 0.00`.
10. Read section 9 and section 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I3). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q5).

---

## Testing Strategy

### Unit Tests
- `Core.Tests/Input/GamepadDeadzoneTests` (9): the stick zone, the full tilt and the clamp, the rescale and the kept direction, the trigger map, the trigger zone, the range check, the deadzone of 1 and the input that is not finite, the reads on a hand-made state, and the enum order.
- `Glfw.Tests/GlfwUiInputGamepadPollTests` (8): the axis buffer and the GLFW constants, the conversion order, the conversion with the deadzones, the settings check, the disposed poll, two 0-byte tests, and the null-platform poll.

### Integration Tests
- `GlfwUiInputGamepadTests.ALivePollAllocatesNothing` measures `PollGamepads` and `PollGamepad` together on a real GLFW, and checks a disconnected absent slot. It skips with no display and runs under `xvfb-run -a`.
- The HelloPanel smoke run of `scripts/test-linux.sh` runs the poll, the push from the slot, and the stick label in each frame. The sample check of 0 bytes covers the push and the label.

### Allocation Tests
- `ADisposedPollAllocatesNothing`, `TheConversionAllocatesNothing`, and the measure of `TheNullPlatformPollWritesSixteenAbsentSlots`: 0-byte tests, not a numbered gate (Decision 9). Plants 4 and 7 guard them. `InputStateAllocationTests` keeps covering `SetGamepad`.

### Regression Tests
- Each existing test runs with no edit, except the `Poll` helper and `ALivePollAllocatesNothing` (A7). `InputStateTests` guards the slots and the edges, and `GamepadActionTests` guards `PushGamepad`.

### Manual Testing
- Step 9 of the Verification Method: the stick at rest, a small tilt, full right, full up, the B button, and an unplug.

---

## Related Documentation

- **Requirements:** `docs/features/input/gamepad-axes/requirements.md`
- **Epic:** `docs/features/input/epic-overview.md`, `docs/features/input/epic-requirements.md`, `.beast-mode/interview.md`
- **Siblings:** `docs/features/input/input-state/implementation.md` (Decision 11, Notes question 1) and `context.md` (Gotchas 1 to 3, Decisions 24 to 27), `docs/features/input/input-actions/requirements.md`
- **Design:** `docs/technical-design.md` sections 9 (Input and Action map) and 14 (Testing). `docs/mission-statement.md`
- **Format reference:** `docs/features/input/input-state/implementation.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Glfw/GlfwUiInput.cs` (`PollGamepad`, `GamepadButtons`, the seam constructor, `_disposed`), `src/BrutalUi.Core/Input/UiGamepadState.cs` (the axes, `Axis(int)`), `src/BrutalUi.Core/Input/UiEventEnums.cs` (`UiGamepadButtons`), `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadTests.cs` (the fixed buffer check, the live test), `samples/HelloPanel/HelloScreen.cs` (`InputText`, `LongestInputText`)
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (A) of each question. A change before the phase that the question names costs little.

1. **Where does HelloPanel show the left stick?** (before phase 3)
   - **(A) Recommended:** a fourth label `Left stick: <x>, <y>` after the input label (Decision 10).
   - (B) append `. Left stick: <x>, <y>` to the input label: two fewer sample edits, a 74-byte line, and a change of the input-state text and docs.
2. **What does a trigger read when the pad mapping has no trigger?** (before phase 2)
   - **(A) Recommended:** keep the formula. GLFW gives 0 for an unavailable axis, so the trigger reads about 0.444 with the default deadzone. The remarks of `TriggerDeadzone` state it.
   - (B) treat a raw 0 as released: a real half pull then reads 0.

### Conflicts and Gaps

1. **The axis range of the requirements.** Requirements line 12 says "six axes as `float` in -1 to 1". Q2 sets the trigger range to 0 to 1. The plan follows Q2, and the remarks of `UiGamepadState` and `UiGamepadAxis` state both ranges.
2. **"The value of an axis is the GLFW value."** Requirements line 13 reads as the raw value. Q1 and the prompt say the value after the deadzone. The plan stores the value after the deadzone and changes the remarks of `Axis(int)`, which said "with no deadzone applied".
3. **A1 and the new reads.** A1 says "Core changes no other type". A2 adds three reads to `UiGamepadState`. The plan reads A1 as no change to `UiContext`, `UiInputState`, and `InputStateWriter`, as the prompt states. `UiEventEnums.cs` gets a new enum and no change to an existing type.
4. **A7 and "the existing 6 gamepad tests pass with no edit".** A7 grows the live test. The plan keeps the other 5 tests and their count with no edit, and edits only `ALivePollAllocatesNothing`, its `Poll` helper, and the class summary.
5. **A5 and the package docs.** The XML docs of `Ahwoo.BRUTAL.Glfw` 2026.10.4 do not document `GlfwGamepadState.Axes`. The DLL holds the member name `Axes` and the `GLFW_GAMEPAD_AXIS_*` names. The plan keeps A5. Task 2.3 confirms the shape at the first compile, and a test pins it.
6. **The epic spelling of the poll.** `docs/features/input/epic-overview.md` (Integration & Architecture, "Within the epic") says "`GlfwUiInput.PollGamepad` writes the gamepad state into the same struct". The writer is `PollGamepads`. `/update-epic input` fixes it. Task 3.8 records the note in `context.md`.
7. **The platform init hint.** The XML docs name `GlfwPlatform.Null` and `SetInitHint(GlfwInitHint, int)`, but not the `GlfwInitHint` member for the platform. Task 2.6 casts the GLFW constant when no member exists.
8. **The plan is not compiled, and the prose lint did not run.** The planner had no shell tool. Task 1.6 is the first compile, and task 3.8 runs the lint on this plan and on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
