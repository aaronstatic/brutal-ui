# Input State - Implementation Plan

**Epic:** input (feature 1 of 5)
**Status:** Ready for Review
**Started:** 2026-11-08
**Target Completion:** TBD
**Last Updated:** 2026-11-08 16:55

---

## Quality Bar

**Target quality:** A library Core feature that is the base of the input epic. Each later feature of the epic reads this state, and the spacegame map controls read it through input-actions. A wrong flag gives a stuck key, a lost release, or a pan of the map under a panel. A hidden allocation costs bytes in each `Update`, and each existing allocation gate then fails.

- **Reliability of the flags:** Each flag has one rule, and a test pins each rule. The down, pressed, and released flags of a button and of a key follow the entries of the ring in order. A repeat sets no pressed flag. A press belongs to the side that got the down. A focus loss and a `KeyboardEnabled` switch to `false` leave no key down. A plant fails each rule test.
- **API clarity:** The game reads `ui.Input` (the raw state) and `ui.GameInput` (the gated view) after `Update`. Both are `ref readonly`, so a read copies nothing and the game cannot write the state. Each part has a short query API: `Pointer.Delta`, `Buttons.WasReleased(PointerButton.Left)`, `Keys.IsDown(87)`. Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the frame rule, the gate rule, the ownership rule, the repeat rule, and the clear rule.
- **0 bytes after warm-up:** The write in the drain, `ClearHeldKeys`, `SetGamepad`, and each read allocate 0 bytes in the default Debug build. A new gate `InputStateAllocationTests` measures them, and four plants fail it. Each existing gate keeps 0 bytes with the three drain hooks in place.
- **A small named Core diff:** The Core diff is `UiContext.cs` (one field, two constructor lines, two properties, two methods, three hooks in `DrainEvents`) and three new files in `src/BrutalUi.Core/Input/`. `UiActionMap`, `OnUnhandledInput`, and `UiRoutingGate` do not change. No project file changes.

---

## Executive Summary

This feature adds a per-frame input state to `UiContext`. The drain of the input pass writes it, entry by entry, as each entry leaves the ring. The end of the drain writes a second copy that the routing gate filters. The game reads both copies after `Update`:

- `ui.Input` gives the raw `UiInputState` of the last `Update`: the pointer position, the pointer delta, the scroll delta, the down, pressed, and released flags of each mouse button and each GLFW key, the modifiers of the last key event, and 16 gamepad slots.
- `ui.GameInput` gives the same struct after the gate. The pointer, button, and scroll parts are empty when `PointerOverUi` is `true`. The key part is empty when `KeyboardFocusInUi` is `true`. A button press that went down over the UI never shows in the game view.
- `ui.ClearHeldKeys()` releases each held key at the end of the next drain. `GlfwUiInput` calls it on a window focus loss and on a `KeyboardEnabled` switch to `false`.
- `ui.SetGamepad(index, state)` is the write seam for gamepad-axes. This feature defines the slot and the seam, and it fills no gamepad value.

The write logic lives in a new internal struct `InputStateWriter`, a field of `UiContext` in the pattern of `EventQueue`. `UiContext.cs` grows by about 120 lines, most of it doc comments.

The plan has 5 phases and 32 tasks. Phase 1 (10 tasks in two groups) delivers the pointer position and delta, the pointer gate, and the HelloPanel label. Phase 2 (4 tasks) delivers the buttons, the press ownership, and the scroll. Phase 3 (8 tasks in two groups) delivers the keys, the repeat rule, the modifiers, and the clear rules with the adapter change. Phase 4 (4 tasks) delivers the gamepad slots and the write seam. Phase 5 (6 tasks in two groups) delivers the 0-byte gate, the docs, and the changelog. Phase 1 needs an ADVANCED dev agent, because it edits `DrainEvents`.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (reads the pointer delta, the release of a button, the scroll, and the held keys after `Update`, and gets the gate for free through `GameInput`)
- [x] **The sibling features** (gamepad-axes writes the gamepad slots through `SetGamepad`, input-actions reads `GameInput` in `UiActionMap.Update`, and the spacegame map controls read the actions)
- [x] **The maintainer** (runs the new test classes through `scripts/test-linux.sh` with no script change)

**User Subsets Served:**
- [x] **Simple** - a game reads `ui.GameInput.Pointer.Delta` and `ui.GameInput.Keys.IsDown(87)` in its frame loop to pan a camera.
- [x] **Complex** - a game drags a slider off a panel and releases the button over the world, holds W while a text field takes the focus, alt-tabs out with a key down, and switches the keyboard to ImGui. No stray release and no stuck key reach the game.

**UX Approach:**
- Build time: nothing. The context creates the state in its constructor.
- Frame time: `ui.Update(dt);` then `ref readonly UiInputState game = ref ui.GameInput;` and the queries. No new line in the frame loop of a game that uses `GlfwUiInput`.
- No builder, no `IDisposable`, no event keyword, no copy of the state, and no allocation in the frame path.

---

## Goals

### Primary Goals
1. Add `UiInputState` with its pointer, button, key, and gamepad parts, and the two `ref readonly` properties `Input` and `GameInput` (Q1, Q2 of the epic, A1, A2).
2. Write the raw state in the drain, entry by entry, and the game view at the end of the drain from the gate (A4, A5, A6).
3. Give each button press to the side that got the down (A3).
4. Clear each held key on a focus loss and on a `KeyboardEnabled` switch to `false` (Q2).
5. Define the gamepad slots and the write seam of gamepad-axes (A2).
6. Prove 0 bytes after warm-up, show the pointer delta and a held key in HelloPanel, and state the rules in section 9 of the technical design and in the changelog (A3 and A9 of the epic).

### Success Criteria
- [ ] The phase 1 demo passes: over the game area of HelloPanel, the label shows the pointer delta, and over the panel the label reads 0.
- [ ] The phase 2 demo passes: `InputStateTests` reads a total of 18 with `failed: 0`.
- [ ] The phase 3 demo passes: with W held over the game area, the label shows `Held key: W`. With the text field focused, it shows `Held key: none`.
- [ ] The phase 4 demo passes: `InputStateTests` reads a total of 32 with `failed: 0`.
- [ ] The phase 5 demo passes: `scripts/test-linux.sh` prints its PASS line, and `InputStateAllocationTests` reads a total of 3 with `failed: 0`.
- [ ] Each existing test passes, and each existing allocation gate keeps 0 bytes. The one edited existing test is the `EventNames` list of `GlfwUiInputTests`.
- [ ] The Core diff is `src/BrutalUi.Core/Tree/UiContext.cs` and the new files `UiInputState.cs`, `UiGamepadState.cs`, and `InputStateWriter.cs` in `src/BrutalUi.Core/Input/`.

---

## Architecture Overview

### The Place in the Epic

input-routing gives the ring, the drain, the focus owner, and `UiRoutingGate`. This feature adds the state that the drain writes. gamepad-axes (next) calls `SetGamepad` from `GlfwUiInput.PollGamepads()`. input-actions reads `GameInput` in `UiActionMap.Update`. control-schemes and binding-remap build on input-actions and do not read the state directly.

```
Frame N (0 bytes after warm-up)
  Glfw.PollEvents()
    GlfwUiInput pushes pointer, scroll, key, and char events into the ring
    OnFocus(lost)               ──► ui.ClearHeldKeys()      (phase 3: a pending flag only)
  input.KeyboardEnabled = false ──► ui.ClearHeldKeys()      (phase 3: on the switch from true only)
  ui.SetGamepad(i, state)       ──► Raw.Gamepad(i), with the edges (phase 4, gamepad-axes calls it)
  ui.Update(dt)
    OnBeforeUpdate(dt)                                      (mode-set, no change)
    DrainEvents()
      the stale rules (no change)
      hook 1: _input.BeginFrame()        clear the frame flags, keep the start position
      for each entry:
        the target step (RoutePointer or the focus owner, no change)
        hook 2: _input.Record(in entry, target is not null)   the raw write and the press owner
        the walk, FinishRelease, the Tab move, the unhandled input step (no change)
      finally:
        FinishRelease() (no change)
        hook 3: _input.EndFrame(RoutingGate)   the key clear, the delta, the game view
    the binding, style, transition, layout, caret, and clear passes (no change)
  the game reads ui.Input and ui.GameInput
```

### File Tree (end state)

```
src/BrutalUi.Core/Input/
├── UiInputState.cs                 # phase 1 (new): UiInputState, UiPointerState. Phase 2: UiPointerButtonState. Phase 3: UiKeyState, KeyBits. Phase 4: the gamepad slots
├── UiGamepadState.cs               # phase 4 (new): UiGamepadState, GamepadAxes
└── InputStateWriter.cs             # phase 1 (new), phases 2, 3, 4
src/BrutalUi.Core/Tree/
└── UiContext.cs                    # phase 1: the field, the properties, the hooks. Phase 3: ClearHeldKeys. Phase 4: SetGamepad
src/BrutalUi.Glfw/
└── GlfwUiInput.cs                  # phase 3: the OnFocus handler, PushFocus, the KeyboardEnabled setter
samples/HelloPanel/
├── HelloScreen.cs                  # phase 1: the input label and InputText. Phase 3: the held key
├── HelloModes.cs                   # phase 1: the InputLabel property
└── Program.cs                      # phase 1: the label write in the frame loop
tests/BrutalUi.Core.Tests/Input/
├── InputStateTests.cs              # phase 1 (new): 8 tests. Phase 2: 10 more. Phase 3: 8 more. Phase 4: 6 more (32)
└── InputStateAllocationTests.cs    # phase 5 (new): 3 tests
tests/BrutalUi.Glfw.Tests/
├── GlfwUiInputFocusTests.cs        # phase 3 (new): 5 tests
└── GlfwUiInputTests.cs             # phase 3: OnFocus in the EventNames list only
docs/technical-design.md            # phase 5: section 9, section 14
CHANGELOG.md                        # phase 5
docs/features/input/input-state/context.md   # each phase
```

These files do not change: each other file of `src/BrutalUi.Core/` (also `UiRoutingGate.cs`, `UiUnhandledInputHandler.cs`, `EventQueue.cs`, and `UiEventEnums.cs`), each file of `src/BrutalUi.Framework/` (also `UiActionMap.cs`), each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, and `scripts/`. The Core files use the namespace `BrutalUi.Core`. The test files use `BrutalUi.Core.Tests` and `BrutalUi.Glfw.Tests`.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public sealed class UiContext
{
    public ref readonly UiInputState Input { get; }        // the raw state of the last Update
    public ref readonly UiInputState GameInput { get; }    // the view that the routing gate filters
    public void ClearHeldKeys();                           // phase 3: applies at the end of the next drain
    public void SetGamepad(int index, in UiGamepadState state);   // phase 4: the write seam of gamepad-axes
}

public struct UiInputState                                 // about 800 bytes, at most 1024
{
    public const int GamepadCount = 16;
    [UnscopedRef] public readonly ref readonly UiPointerState Pointer { get; }
    [UnscopedRef] public readonly ref readonly UiPointerButtonState Buttons { get; }   // phase 2
    [UnscopedRef] public readonly ref readonly UiKeyState Keys { get; }                // phase 3
    [UnscopedRef] public readonly ref readonly UiGamepadState Gamepad(int index);      // phase 4, 0 to 15
}

public struct UiPointerState                               // 24 bytes
{
    public readonly float2 Position { get; }     // (NaN, NaN): no position yet, or the cursor left the window
    public readonly float2 Delta { get; }        // this Update minus the last Update. 0 when either is NaN
    public readonly float2 ScrollDelta { get; }  // phase 2: the sum of the scroll entries of the frame
}

public struct UiPointerButtonState                         // 3 bytes: three masks over the GLFW buttons 0 to 7
{
    public const int ButtonCount = 8;
    public readonly bool IsDown(PointerButton button);       // false for a value outside 0 to 7
    public readonly bool WasPressed(PointerButton button);
    public readonly bool WasReleased(PointerButton button);
}

public struct UiKeyState                                   // 136 bytes: three bitsets of 11 uint words, the modifiers
{
    public const int KeyCount = 349;                         // GLFW key codes 0 to 348 (GLFW_KEY_LAST)
    public readonly UiModifiers Modifiers { get; }           // from the last key event
    public readonly bool IsDown(int key);                    // false for a code outside 0 to 348, also -1
    public readonly bool WasPressed(int key);
    public readonly bool WasReleased(int key);
}

public struct UiGamepadState                               // 40 bytes
{
    public const int AxisCount = 6;
    public UiGamepadState(UiGamepadButtons buttons, ReadOnlySpan<float> axes);   // Connected is true
    public readonly bool Connected { get; }                  // default(UiGamepadState) is a disconnected slot
    public readonly UiGamepadButtons Buttons { get; }        // the buttons that are down
    public readonly UiGamepadButtons Pressed { get; }        // SetGamepad writes the edges
    public readonly UiGamepadButtons Released { get; }
    public readonly float Axis(int axis);                    // the GLFW axis index 0 to 5
}
```

The `{ get; }` lines show the shape only. Each property is an expression body over a private field.

### Internal Shape

- **The parts (Decision 3).** Each part is a struct with private fields, public `readonly` queries, and `internal` write methods. `UiPointerState` holds `_position`, `_delta`, and `_scroll`. `UiPointerButtonState` holds three `byte` masks: `_down`, `_pressed`, and `_released`. Bit `i` is the GLFW button `i`. `UiKeyState` holds three `KeyBits` values and `_modifiers`. `KeyBits` is `[InlineArray(11)] internal struct KeyBits { private uint _word; }`. 11 words hold 352 bits, and the codes 0 to 348 use 349 of them. `UiInputState` holds the four parts in internal fields and gives each through a `[UnscopedRef] public readonly ref readonly` member. The gamepad slots are `[InlineArray(16)] internal struct GamepadSlots { private UiGamepadState _slot; }`. `UiGamepadState` holds `_connected`, `_buttons`, `_pressed`, `_released`, and `GamepadAxes`, an `[InlineArray(6)]` of `float`.
- **The writer (Decision 2).** `internal struct InputStateWriter` in `src/BrutalUi.Core/Input/InputStateWriter.cs`. Fields: `internal UiInputState Raw`, `internal UiInputState Game`, `private byte _uiPresses` (bit `i`: the last down of button `i` went to the UI), `private byte _uiReleases` (bit `i`: an up of a UI press in this frame), `private float2 _startPosition`, and `private bool _clearKeys`. `UiContext` holds it in `private InputStateWriter _input;`, in the pattern of `_events`: a mutable struct that changes only through the field. `static InputStateWriter Create()` sets the raw and the game position and `_startPosition` to (NaN, NaN). Both `UiContext` constructors call it.
- **BeginFrame (hook 1).** `_startPosition = Raw.Pointer.Position`. Clear the delta, the scroll, the pressed and released masks of the buttons and the keys, and `_uiReleases`. Keep each down mask, the modifiers, the position, `_uiPresses`, and each gamepad slot. `SetGamepad` writes the gamepad edges before `Update`, so the drain must not clear them (Decision 11).
- **Record (hook 2).** `Record(in UiQueuedEvent entry, bool hitUi)` switches on `entry.Header.Type`. It reads the payload that the type names, because the payloads overlap.
  - `PointerMove`: the position.
  - `PointerDown`: the position. Then `down |= bit`, `pressed |= bit`. Then `_uiPresses` gets the bit when `hitUi` is `true`, and loses it when `hitUi` is `false` (phase 2).
  - `PointerUp`: the position. When `down` holds the bit: `released |= bit`, and `_uiReleases |= bit` when `_uiPresses` holds it. Then `down &= ~bit` (phase 2).
  - `Scroll`: the position from the scroll payload, and `ScrollDelta += Delta` (the sum is phase 2).
  - `KeyDown`: when the code is in 0 to 348, `down` gets the bit, and `pressed` gets it only when `IsRepeat` is `false`. The modifiers take the value of the entry for each code, also a code outside the range (phase 3).
  - `KeyUp`: when the code is in range and `down` holds the bit, `released` gets it and `down` loses it. The modifiers take the value of the entry (phase 3).
  - `Char` and each other type: nothing.
  A button outside 0 to 7 changes no mask. The position takes each value, also (NaN, NaN).
- **EndFrame (hook 3).** `EndFrame(UiRoutingGate gate)`:
  ```
  if (_clearKeys) { keys.released |= keys.down; keys.down = 0; keys.modifiers = None; _clearKeys = false; }   // phase 3
  delta = (start and position are both finite) ? position - start : (0, 0)
  Game = Raw                                                     // one struct copy, about 800 bytes
  if (gate.PointerOverUi) { Game.Pointer = empty; Game.Buttons = empty; }   // empty: position (NaN, NaN), zeros
  else { Game.down &= ~_uiPresses; Game.pressed &= ~_uiPresses; Game.released &= ~_uiReleases; }   // phase 2
  if (gate.KeyboardFocusInUi) { Game.Keys = default; }           // phase 3: no bit, modifiers None
  ```
  The gamepad slots pass to the game view with no gate.
- **The hooks in `DrainEvents` (Decision 4).** Hook 1 goes after `DropStaleFocus()` and before the read of `_events.Count`. Hook 2 goes right after the target step (the `if` and `else if` that set `target`) and before the press target step. Hook 3 goes in the `finally` block after `FinishRelease()`, as `_input.EndFrame(RoutingGate);`. The `_draining` check throws before the `try` block, so a nested drain writes nothing. An exception of a handler still runs hook 3, so the state holds each entry up to the throw.
- **ClearHeldKeys (phase 3).** `public void ClearHeldKeys() => _input.RequestKeyClear();`. The request sets `_clearKeys`. Hook 3 of the next drain applies it after the last entry (Decision 10).
- **SetGamepad (phase 4).** Checks the index (0 to 15, else `ArgumentOutOfRangeException` with the parameter `index`). Then `_input.SetGamepad(index, in state)`: `Pressed = new & ~old`, `Released = old & ~new`, over the `Buttons` of the old raw slot and of the new state. It copies `Connected`, `Buttons`, and the six axes. It writes the raw slot at once. Hook 3 of the next drain copies it to the game view.
- **The adapter (phase 3).** `GlfwUiInput` gets a seventh handler field `_onFocus` of the type `GlfwWindowFocusCallback`, the subscription `window.OnFocus += _onFocus;` after `OnChar`, and the unsubscribe in `Dispose`. The handler calls `internal void PushFocus(bool focused)`. The seam returns after `Dispose`, and else calls `Context.ClearHeldKeys()` for a loss. `KeyboardEnabled` gets a backing field. Its setter calls `Context.ClearHeldKeys()` when the value goes from `true` to `false`, also after `Dispose` (the call only sets a flag). A write of the same value does nothing, so the line `input.KeyboardEnabled = !io.WantCaptureKeyboard;` in each frame clears only on the switch.

### Integration Points

| Sibling or caller | What this feature calls or gives | Contract |
|---|---|---|
| input-routing (`UiContext.DrainEvents`, done) | three hooks, `RoutingGate` | The hooks add writes only. The target step, the walk, `FinishRelease`, the Tab move, and the unhandled input step keep their order and their result. Each existing input test passes with no edit. |
| routing-gate (`UiRoutingGate`, done) | `RoutingGate` at the end of the drain | Hook 3 reads the same value that the game reads after `Update`. A focus change from game code after `Update` changes `RoutingGate` and not `GameInput` until the next `Update`. |
| mode-set (`OnBeforeUpdate`, done) | none | The slot runs before the drain, so it reads the state of the last `Update`. A mode switch that calls `ActionMap.Deactivate` runs before the drain of the same `Update`. |
| state-and-commands (`UiActionMap`, `OnUnhandledInput`, done) | none | No change (A7). A press over no element still reaches the slot. The state records the same press. |
| `GlfwUiInput` (done) | `OnFocus`, `PushFocus`, the `KeyboardEnabled` setter | GLFW sends synthetic key releases after the focus callback. They walk as before and clear their down bits. The pending clear then finds no held key, so no key gets two releases. |
| gamepad-axes (next) | `SetGamepad`, `UiGamepadState`, `Gamepad(int)` | The poll builds each state with the public constructor and calls `SetGamepad` one time for each slot, after `Glfw.PollEvents` and before `Update`. gamepad-axes adds `UiGamepadAxis` with the values 0 to 5 and needs no `UiContext` change (Notes question 1). |
| input-actions (later) | `GameInput`, each query | The map reads `GameInput` in its `Update`, after `UiContext.Update`. Each name of the Public API stays. `Deactivate` releases the held actions of the map. The state does not change for it. |
| HelloPanel (sample) | `GameInput` | The loop writes the input label after the draw, inside the UI region, so the sample check of 0 bytes covers the reads. |

---

## Implementation Phases

The feature has 32 tasks, above the "about 8 tasks" of Slice rule 7, so it has five slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea: the drain writes the pointer position, the end of the drain writes the delta and the gated view, and a HelloPanel label shows the delta (Q3). Phase 2 adds the buttons, which the map controls need for a drag. Phase 3 adds the held keys and the clear rules. Phase 4 adds the gamepad slots. Phase 5 adds the gate and the docs.

Each verification command sets `MSBUILDDISABLENODEREUSE=1` and gives `-nodeReuse:false` to `dotnet build`, because stale MSBuild nodes on this machine hang the build. Do not run `dotnet build-server shutdown`. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `System.Diagnostics.CodeAnalysis` for `UnscopedRef`, `System.Runtime.CompilerServices` for `InlineArray`, `Brutal.Numerics`), also for each namespace that a `cref` names. Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one Core class with `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, and one Glfw class with `tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, after a build.

Two flakes are known. A test host can exit with 134 (Internal CLR error 0x80131506), and the Core test `TheCurvesAllocateNothing` failed one time with 8128 bytes. Run the tests a second time before you treat either as a fault. A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant. For each plant, copy the file to the scratchpad first, run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`, expect a value that is not 0, restore the file, check it with `cmp`, and run the tests again.

The tests use `PointerTree` (`tests/BrutalUi.Core.Tests/Input/PointerTree.cs`) with a unique prefix for each test: the root covers (0, 0, 400, 300) in a viewport of 800 by 600. A point over the UI is (50, 50), on `Left`. A point over the game is (600, 400), outside the root. Each test calls `tree.Settle()` first, pushes events with `PointerTree.Move`, `Down`, `Up`, `Scroll`, and `Key`, and calls `tree.Context.Update(0f)`. A test reads the state after `Update` only, and never calls `DrainEvents` directly, because a second drain starts a new frame.

### Phase 1: The Pointer Delta and Its Gate
**Goal:** The drain writes the pointer position of each pointer and scroll entry into `ui.Input`. The end of the drain writes the delta since the last `Update` and the game view. The pointer part of `ui.GameInput` is empty when the pointer is over the UI. A HelloPanel label shows the pointer delta of the game view.
**Demo:** the user moves the mouse over the game area of HelloPanel, and a label shows the pointer delta. The label reads 0 over the panel.
**Needs:** none
**Agent tier:** ADVANCED (opus, at the preset effort). The phase edits `DrainEvents`, the place where each input rule meets, and it sets all three hooks that the later phases use.

**Tasks (group 1, the Core path):**
- [ ] 1.1 Create `docs/features/input/input-state/context.md`. Run `git status --porcelain` and check that the panel-sample changes of HelloPanel are committed (Notes, gap 6). Record the base commit (`git rev-parse HEAD`), the test count of the solution with the skipped count, and the counts of `BrutalUi.Core.Tests` and `BrutalUi.Glfw.Tests`. Record the Internal Shape, the Integration Points, and the three hook places of this plan.
- [ ] 1.2 Write `src/BrutalUi.Core/Input/UiInputState.cs` with `UiInputState` (the constant `GamepadCount` waits for phase 4) and `UiPointerState` with `Position` and `Delta`. Add the internal write methods of the pointer part: `Move(float2 position)` and `FinishFrame(float2 start)`, which writes the delta. The doc comment of `UiInputState`: the drain of `Update` writes it, the game reads it after `Update` through `UiContext.Input` or `UiContext.GameInput`, and a read through the `ref readonly` property copies nothing. A copy of the struct is a snapshot. The doc comment of `Position`: (NaN, NaN) before the first pointer entry and after a cursor leave. The doc comment of `Delta`: the position of this `Update` minus the position of the last `Update`, and (0, 0) when either position holds NaN (A5).
- [ ] 1.3 Write `src/BrutalUi.Core/Input/InputStateWriter.cs` with the fields of the Internal Shape, `Create`, `BeginFrame`, `Record` for the pointer types and the scroll position, and `EndFrame` with the delta and the pointer gate. `Record` takes `bool hitUi` from the start, and phase 1 does not read it. The remarks: the mutable-struct rule of `EventQueue`, the frame rule (hook 1 clears the frame values, hook 2 writes each entry, hook 3 writes the delta and the game view), and the allocation rule (no field holds a reference, and no method allocates).
- [ ] 1.4 Edit `src/BrutalUi.Core/Tree/UiContext.cs`. Add `private InputStateWriter _input;` after `_events`, and `_input = InputStateWriter.Create();` in both constructors. Add the three hooks of the Internal Shape to `DrainEvents`, each with a one-line comment. Add `public ref readonly UiInputState Input => ref _input.Raw;` and `public ref readonly UiInputState GameInput => ref _input.Game;` after `RoutingGate`. The remarks of `Input`: the raw state of the last `Update`, the frame rule, and Q2 of the epic. The remarks of `GameInput`: the gate rule (A4: the gate of the end of the drain, the same value that `RoutingGate` gives after `Update`), the empty pointer part, and the rule that a focus change after `Update` waits for the next `Update`. Add one paragraph to the remarks of `DrainEvents`: the state write of the three hooks. Add one sentence to the remarks of `RoutingGate`: `GameInput` applies this value at the end of the drain. Change no other member.
- [ ] 1.5 Write `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs` with 8 tests. `TheStateStartsWithNoPointerAndAZeroDelta` (a new context: `Input.Pointer.Position` holds NaN in both components, and `Delta` is (0, 0). An `Update` with an empty ring keeps both). `ThePositionIsTheLastPointerEntryOfTheFrame` (moves to (500, 350) and (600, 400) in one frame give (600, 400). A scroll at (610, 410) after them gives (610, 410)). `TheDeltaIsThePositionOfThisUpdateMinusThePositionOfTheLastUpdate` (frame 1: a move to (500, 350). Frame 2: moves to (520, 340) and (530, 360) give (30, 10). Frame 3 with no entry gives (0, 0) and keeps the position). `APointerAtNaNGivesAZeroDelta` (a move to (500, 350), then a move to (NaN, NaN) gives (0, 0) and a NaN position. A move to (510, 350) in the next frame gives (0, 0) again). `TheGameViewIsEmptyWhenThePointerIsOverTheUi` (frame 1 over the game, frame 2 a move to (50, 50): `RoutingGate.PointerOverUi` is `true`, `Input.Pointer.Delta` is (-550, -350), `GameInput.Pointer.Delta` is (0, 0), and `GameInput.Pointer.Position` holds NaN). `TheGameViewEqualsTheRawPointerOverTheGame` (two frames over the game: the position and the delta of both views are equal). `TheGateOfTheGameViewComesFromTheEndOfTheDrain` (one frame with a move over the UI and then a move over the game gives an open game view. The reverse order gives an empty game view, A4). `AnEntryWithATargetFromTheCallerMovesTheRawPosition` (a move with the target `Left` at (700, 500) sets `Input.Pointer.Position` to (700, 500) and keeps `PointerHit` and `RoutingGate` as before).
- [ ] 1.6 Run the build, the tests, and `InputStateTests` (total 8). Expect `failed: 0`. Each existing test and each existing allocation gate passes with no edit. Run three plants. Plant 1: remove hook 3. Expect `TheDeltaIsThePositionOfThisUpdateMinusThePositionOfTheLastUpdate` to fail. Plant 2: pass `default(UiRoutingGate)` to hook 3. Expect `TheGameViewIsEmptyWhenThePointerIsOverTheUi` to fail. Plant 3: remove the NaN check of the delta. Expect `APointerAtNaNGivesAZeroDelta` to fail. Record the counts and the results in `context.md`.

**Tasks (group 2, the sample):**
- [ ] 1.7 Edit `samples/HelloPanel/HelloScreen.cs`. `BuildWidgetPanel` gets a second out parameter `out Label inputLabel`. The label is not pickable, comes right after the gate label, and starts with the longest input text, so its buffer never grows. Add `public static ReadOnlySpan<byte> InputText(in UiInputState game, Span<byte> scratch)`. It writes `Game pointer delta: <x>, <y>` into the scratch buffer and returns the written part. Each component is rounded to an `int` and clamped to -9999 to 9999, and `int.TryFormat` writes it with `CultureInfo.InvariantCulture` (the pattern of `HelloWidgets.WriteCount`). Update the remarks of the method and of `BuildWidgetPanel`.
- [ ] 1.8 Edit `samples/HelloPanel/HelloModes.cs`. Store the input label of the widget factory in `_inputLabel` and expose `InputLabel`, in the pattern of `GateLabel`.
- [ ] 1.9 Edit `samples/HelloPanel/Program.cs`. Create `byte[] inputScratch = new byte[64];` before the frame loop, pass `modes.InputLabel` to the loop method next to the gate label, and add `inputLabel.SetText(HelloScreen.InputText(in ui.GameInput, inputScratch));` right after the gate label line, inside the UI region. The comment: the label shows the game view of this frame in the next frame, and the reads allocate nothing.
- [ ] 1.10 Run the build, the tests, and `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`, so the smoke run keeps 0 bytes in the UI region. Run HelloPanel with a display (`dotnet run --project samples/HelloPanel/HelloPanel.csproj`) when one exists, and check the demo. Without a display, record the manual check as open in `context.md`.

**Estimated Steps:** about 70 (10 tasks, about seven steps for one task)

**Files (group 1, 5):** `UiInputState.cs`, `InputStateWriter.cs`, `UiContext.cs`, `InputStateTests.cs`, `context.md`
**Files (group 2, 4):** `HelloScreen.cs`, `HelloModes.cs`, `Program.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `InputStateTests` pass. Each existing test passes with no edit, and each existing allocation gate passes.
- [ ] The three plants of task 1.6 give a non-zero exit code, and each restored file equals its copy.
- [ ] `scripts/test-linux.sh` prints its PASS line.
- [ ] `git diff` of `UiContext.cs` adds lines in `DrainEvents` only at the three hook places, and changes no existing code line.

### Phase 2: The Buttons, the Press Ownership, and the Scroll
**Goal:** The button part holds the down, pressed, and released flags of each GLFW mouse button. A press belongs to the side that got the down: the game view never shows a press that went down over the UI. The pointer part holds the scroll sum of the frame. The game view empties the button and scroll parts over the UI.
**Demo:** a developer runs `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests'` after a build and reads a total of 18 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet). Hook 2 passes `hitUi` since phase 1, so the phase changes the writer and the types only.

**Tasks (one group):**
- [ ] 2.1 Edit `src/BrutalUi.Core/Input/UiInputState.cs`. Add `UiPointerButtonState` with `ButtonCount`, the three queries, and the internal methods `Press`, `Release`, `BeginFrame`, and `Mask(byte presses, byte releases)`. Add `ScrollDelta` and the internal `Scroll(float2 position, float2 delta)` to `UiPointerState`. Add the `Buttons` member to `UiInputState`. The remarks: the masks cover the GLFW buttons 0 to 7, and a value outside that range reads `false`. An up sets the released flag only for a button that is down. A press and a release in one frame set both flags and clear the down flag. The ownership rule (A3 and Decision 8).
- [ ] 2.2 Edit `src/BrutalUi.Core/Input/InputStateWriter.cs`. Add the button cases and the scroll sum of `Record`, the `_uiPresses` and `_uiReleases` masks, their clear in `BeginFrame`, and the button gate and the ownership mask of `EndFrame`.
- [ ] 2.3 Add 10 tests to `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs`. `APressSetsDownAndPressedForOneFrame` (a left press at (600, 400): frame 1 is down and pressed. Frame 2 is down and not pressed). `AReleaseSetsReleasedForOneFrameAndClearsDown` (the up gives released and not down, and the next frame gives neither. An up of a button that is not down gives no released flag). `APressAndAReleaseInOneFrameSetBothFlags` (pressed, released, and not down). `EachButtonHasItsOwnFlags` (`Right`, `Middle`, and `(PointerButton)7` each set only their own flags. `(PointerButton)8` and `(PointerButton)(-1)` change no mask and read `false` with no exception). `TheScrollDeltaIsTheSumOfTheFrame` (scrolls of (0, 1) and (0, 2) at (600, 400) give (0, 3). The next frame gives (0, 0)). `TheButtonAndScrollPartsOfTheGameViewAreEmptyOverTheUi` (a press and a scroll at (50, 50): the raw view holds both, and the game view holds neither). `APressOverTheUiThatEndsOverTheGameGivesNoReleaseInTheGameView` (frame 1: a press at (50, 50). Frame 2: a move to (600, 400). The raw view is down, and the game view is not down. Frame 3: an up at (600, 400). The gate is open, the raw view is released, and the game view is not released). `APressOverTheGameBelongsToTheGame` (a press, a held frame, and an up at (600, 400) show pressed, down, and released in the game view). `ANewPressOverTheGameTakesTheButtonBack` (frame 1: a press at (50, 50). Frame 2: an up and a press at (600, 400). The game view is pressed and down, and not released). `APressWithATargetFromTheCallerBelongsToTheUi` (a press with the target `Left` at (600, 400): the raw view is pressed, and the game view is not pressed, also with the gate open).
- [ ] 2.4 Run the build, the tests, and `InputStateTests` (total 18). Expect `failed: 0`. Run three plants on `InputStateWriter.cs`. Plant 4: remove the `_uiReleases` mask from `EndFrame`. Expect `APressOverTheUiThatEndsOverTheGameGivesNoReleaseInTheGameView` to fail. Plant 5: skip the button gate. Expect `TheButtonAndScrollPartsOfTheGameViewAreEmptyOverTheUi` to fail. Plant 6: write the scroll with `=` in place of `+=`. Expect `TheScrollDeltaIsTheSumOfTheFrame` to fail. Record the demo and the results in `context.md`.

**Estimated Steps:** about 30 (4 tasks, about seven steps for one task)

**Files (one group, 4):** `UiInputState.cs`, `InputStateWriter.cs`, `InputStateTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 18 tests of `InputStateTests` pass. Each existing test passes with no edit.
- [ ] The three plants of task 2.4 give a non-zero exit code, and the restored file equals its copy.
- [ ] `UiContext.cs` does not change in this phase.

### Phase 3: The Held Keys and the Clear Rules
**Goal:** The key part holds the down, pressed, and released flags of each GLFW key code and the modifiers of the last key event. A repeat sets no pressed flag. The game view empties the key part when the UI holds the focus. `ClearHeldKeys` releases each held key at the end of the next drain. `GlfwUiInput` calls it on a focus loss and on a `KeyboardEnabled` switch to `false`. The HelloPanel label shows a held key.
**Demo:** the user holds W over the game area of HelloPanel, and the label shows `Held key: W`. The user clicks the text field and holds W, and the label shows `Held key: none`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet). The drain hooks exist. The phase adds one `UiContext` method, writer code, and an adapter handler.

**Tasks (group 1, the Core path):**
- [ ] 3.1 Edit `src/BrutalUi.Core/Input/UiInputState.cs`. Add `KeyBits`, `UiKeyState` with `KeyCount`, `Modifiers`, the three queries, and the internal methods `KeyDown(int key, bool repeat, UiModifiers modifiers)`, `KeyUp(int key, UiModifiers modifiers)`, `BeginFrame`, and `ReleaseAll`. Add the `Keys` member to `UiInputState`. The remarks: the codes are the GLFW key codes 0 to 348 (A1), and a code outside that range, also -1, reads `false`. A repeat sets the down flag and no pressed flag. An up sets the released flag only for a key that is down. The modifiers come from the last key event and stay until the next key event (A6). `ReleaseAll` sets the released flag of each held key, clears each down flag, and sets the modifiers to `None`.
- [ ] 3.2 Edit `src/BrutalUi.Core/Input/InputStateWriter.cs`. Add the key cases of `Record`, the key clear of `BeginFrame`, `RequestKeyClear`, the key clear and the key gate of `EndFrame`.
- [ ] 3.3 Edit `src/BrutalUi.Core/Tree/UiContext.cs`. Add `public void ClearHeldKeys()` after `GameInput`. The remarks: Q2 of the epic. The call sets a request only. The end of the next drain applies it after the last entry, so a key down of the same frame that came before the call also clears. Each cleared key gets the released flag for that frame, and the modifiers become `None`. A key down after the call in the same frame also clears (Decision 10). The call does not touch the elements: an element that got a key down gets no key up. The call allocates nothing. `GlfwUiInput` calls it on a focus loss and on a `KeyboardEnabled` switch to `false`.
- [ ] 3.4 Add 8 tests to `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs`. `AKeyDownSetsDownAndPressedForOneFrame` (W, 87). `AKeyUpSetsReleasedAndClearsDown` (also: an up of a key that is not down gives no released flag). `ARepeatSetsNoPressedFlag` (a down, then a repeat in the next frame: down and not pressed. A repeat of a key that is not down sets the down flag and no pressed flag). `TheModifiersComeFromTheLastKeyEventOfTheFrame` (Left Shift (340) with `Shift`, then W with `Shift | Control`, give `Shift | Control`. The next frame with no key keeps the value). `AKeyOutsideTheRangeChangesNoBit` (-1, 349, and 1000 read `false` with no exception and still set the modifiers. 348 and 32 work). `TheKeyPartOfTheGameViewIsEmptyWhenTheUiHoldsTheFocus` (`Left.IsFocusable = true`, `SetFocus(Left)`, W down: the raw view is down, and the game view is not down and has `None` modifiers. After `ClearFocus` and an `Update`, the game view is down). `ClearHeldKeysReleasesEachHeldKeyAtTheNextUpdate` (W and A down, `Update`, `ClearHeldKeys`: the raw view still holds both. The next `Update` gives not down, released, and `None` modifiers. The `Update` after it gives no released flag). `ClearHeldKeysClearsAKeyDownOfTheSameFrame` (a pushed W down, `ClearHeldKeys`, and one `Update` give pressed, released, and not down).
- [ ] 3.5 Run the build, the tests, and `InputStateTests` (total 26). Expect `failed: 0`. Run three plants. Plant 7: a repeat sets the pressed flag. Expect `ARepeatSetsNoPressedFlag` to fail. Plant 8: remove the key clear of `EndFrame`. Expect `ClearHeldKeysReleasesEachHeldKeyAtTheNextUpdate` to fail. Plant 9: skip the key gate. Expect `TheKeyPartOfTheGameViewIsEmptyWhenTheUiHoldsTheFocus` to fail. Record the results in `context.md`.

**Tasks (group 2, the adapter and the sample):**
- [ ] 3.6 Edit `src/BrutalUi.Glfw/GlfwUiInput.cs` with the adapter change of the Internal Shape. Find the parameter types of `GlfwWindowFocusCallback` first: write the handler as `private void HandleFocus(GlfwWindow window, bool focused)`, build, and read the delegate signature from the CS0123 error when one appears. Record the signature in `context.md`. `HandleFocus` converts the value to `bool` and calls `PushFocus`. Update the remarks of `KeyboardEnabled`: the switch to `false` clears the held keys of `UiContext.Input`, and an element still gets a key down with no key up. Add the remarks of `PushFocus`. Update the class remarks that list the events.
- [ ] 3.7 Edit `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`: add `nameof(GlfwWindow.OnFocus)` to `EventNames` only. Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputFocusTests.cs` with 5 tests on the seam adapter (`new GlfwUiInput(tree.Context)`). `AFocusLossClearsTheHeldKeys` (W down through `PushKey`, `Update`, `PushFocus(false)`, `Update`: not down and released). `AFocusGainChangesNothing` (`PushFocus(true)` keeps W down). `KeyboardEnabledFalseClearsTheHeldKeys` (W down, `Update`, `KeyboardEnabled = false`, `Update`: not down). `OnlyTheSwitchFromTrueToFalseClears` (a write of `true` while `true` keeps W down. A write of `false`, `Update`, a write of `true`, a new W down, `Update`, a second write of `true`, and `Update` keep W down). `DisposeStopsTheFocusClear` (after `Dispose`, `PushFocus(false)` keeps W down).
- [ ] 3.8 Edit `samples/HelloPanel/HelloScreen.cs` and `samples/HelloPanel/Program.cs`. `InputText` appends `. Held key: <key>`: the lowest code of 0 to 348 that `game.Keys.IsDown` gives, as one ASCII character for a code from 32 to 96, as the number for another code, and `none` when no key is down. Update the longest text of the label. Program.cs needs a change only when the scratch buffer is too small. Then run the build, the tests, `GlfwUiInputFocusTests` (total 5), and `scripts/test-linux.sh` (PASS line). Run plant 10: the `KeyboardEnabled` setter calls `ClearHeldKeys` for each write of `false`, also `false` to `false`, and for each write of `true`. Expect `OnlyTheSwitchFromTrueToFalseClears` to fail. Check the demo with a display when one exists. Record the results in `context.md`.

**Estimated Steps:** about 60 (8 tasks, about seven to eight steps for one task)

**Files (group 1, 5):** `UiInputState.cs`, `InputStateWriter.cs`, `UiContext.cs`, `InputStateTests.cs`, `context.md`
**Files (group 2, 6):** `GlfwUiInput.cs`, `GlfwUiInputTests.cs`, `GlfwUiInputFocusTests.cs`, `HelloScreen.cs`, `Program.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 26 tests of `InputStateTests` and the 5 tests of `GlfwUiInputFocusTests` pass. `GlfwUiInputTests` keeps its count and passes. Each other existing test passes with no edit.
- [ ] The four plants of tasks 3.5 and 3.8 give a non-zero exit code, and each restored file equals its copy.
- [ ] `git diff` of `GlfwUiInputTests.cs` shows one added line in `EventNames`.
- [ ] `scripts/test-linux.sh` prints its PASS line.

### Phase 4: The Gamepad Slots and the Write Seam
**Goal:** `UiInputState` holds 16 `UiGamepadState` slots with the buttons, the pressed and released edges, six axes, and a connected flag. Each slot starts disconnected. `UiContext.SetGamepad` writes one slot and its edges. The drain keeps the gamepad edges, and the game view copies the slots with no gate. The struct stays at 1024 bytes or less.
**Demo:** a developer runs the built Core test binary with `--filter-class '*InputStateTests'` and reads a total of 32 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet).

**Tasks (one group):**
- [ ] 4.1 Write `src/BrutalUi.Core/Input/UiGamepadState.cs` with `GamepadAxes`, `UiGamepadState`, its public constructor, and its queries. Edit `src/BrutalUi.Core/Input/UiInputState.cs`: add `GamepadSlots`, `GamepadCount`, and `Gamepad(int index)`, which throws `ArgumentOutOfRangeException` with the parameter `index` for a value outside 0 to 15. The remarks of `UiGamepadState`: A2 of this feature and of the epic. Core names no GLFW type. Bit `i` of the buttons is the GLFW gamepad button `i`. `Axis(i)` gives the GLFW axis `i` with no deadzone (gamepad-axes applies it), and a value outside 0 to 5 throws `ArgumentOutOfRangeException`. The constructor copies at most six axes and fills the rest with 0. `default(UiGamepadState)` is a disconnected slot with no button and no axis. `SetGamepad` writes `Pressed` and `Released`, and the constructor leaves them `None`.
- [ ] 4.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs` and `src/BrutalUi.Core/Input/InputStateWriter.cs`. Add `public void SetGamepad(int index, in UiGamepadState state)` after `ClearHeldKeys`, and the writer method of the Internal Shape. `EndFrame` copies the slots through the copy of `Raw`, so it needs no new line. Check that `BeginFrame` does not touch the slots. The remarks of `SetGamepad`: the write seam of gamepad-axes (Notes question 1). Call it one time for each slot in each frame, after `Glfw.PollEvents` and before `Update`. A second call in one frame replaces the edges of the first. The raw slot changes at once, and the game view gets it at the end of the next drain. The gate does not apply to a gamepad. The call allocates nothing.
- [ ] 4.3 Add 6 tests to `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs`. `EachGamepadSlotStartsDisconnected` (the 16 slots of both views: not connected, `None` buttons and edges, and six axes of 0. `Gamepad(-1)` and `Gamepad(16)` throw `ArgumentOutOfRangeException`). `SetGamepadWritesTheSlotAndTheEdges` (`SetGamepad(3, new(A | B, axes))` gives connected, `A | B`, pressed `A | B`, and the six axes. A second call with `B` gives pressed `None` and released `A`. `SetGamepad(16, ...)` throws). `TheGameViewCopiesTheGamepadsWithNoGate` (after `Update`, `GameInput.Gamepad(3)` equals `Input.Gamepad(3)`, also with the pointer over the UI and the focus in the UI). `TheDrainKeepsTheGamepadEdges` (`SetGamepad` and then `Update`: `Input.Gamepad(3).Pressed` still holds the new buttons). `TheConstructorCopiesUpToSixAxes` (eight values give the first six. Two values give two and four zeros. `default` is not connected). `TheStateStaysNearOneKilobyte` (`Unsafe.SizeOf<UiInputState>()` is 1024 or less, A1).
- [ ] 4.4 Run the build, the tests, and `InputStateTests` (total 32). Expect `failed: 0`. Run two plants on `InputStateWriter.cs`. Plant 11: `BeginFrame` clears the pressed edge of each slot. Expect `TheDrainKeepsTheGamepadEdges` to fail. Plant 12: `EndFrame` writes `default` into the game slots. Expect `TheGameViewCopiesTheGamepadsWithNoGate` to fail. Record the measured size of the struct, the demo, and the results in `context.md`.

**Estimated Steps:** about 30 (4 tasks, about seven to eight steps for one task)

**Files (one group, 6):** `UiGamepadState.cs`, `UiInputState.cs`, `UiContext.cs`, `InputStateWriter.cs`, `InputStateTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 32 tests of `InputStateTests` pass. Each existing test passes with no edit.
- [ ] Both plants of task 4.4 give a non-zero exit code, and the restored file equals its copy.
- [ ] `context.md` records the size of `UiInputState`, at most 1024 bytes.

### Phase 5: The 0-Byte Gate and the Docs
**Goal:** A new gate proves that the input pass with the state, `ClearHeldKeys`, `SetGamepad`, and each read allocate 0 bytes after warm-up. Section 9 and section 14 of the technical design and the changelog state the rules.
**Demo:** a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built Core test binary reads a total of 3 for `--filter-class '*InputStateAllocationTests'` with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the gate):**
- [ ] 5.1 Write `tests/BrutalUi.Core.Tests/Input/InputStateAllocationTests.cs` with 3 tests, in the shape of `PointerAllocationTests`. The scene is `ThousandElementTree` after one `Update`, with the events built before the measure. Each step pushes a move to the slot point, a press, a move outside the root, a release, a scroll outside the root, a W down, a W repeat, a W up, and a Left Shift down. It calls `SetGamepad(0, ...)` with one of two states, calls `ClearHeldKeys` on every second step, and calls `Update(0.016f)`. It then reads `Input` and `GameInput` into a local `int` sum: the delta and the scroll as `int`, each button flag, the W flags, and the buttons of slot 0. `TheInputPassWithTheStateAllocatesNothingAfterWarmUp` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()` right before the measure, 100 steps, `Assert.Equal(0L, after - before)`). `TheReadsOfTheStateAllocateNothing` (each query over the 349 key codes, the 8 buttons, and the 16 slots with six axes, on both views, 100 rounds, 0 bytes). `TheMeasuredBodyDoesRealWork` (after one step: the expected flags of the raw view and of the game view, the expected gamepad edges, and a fixed sum). The measured bodies create no lambda, no string, no array, and no `Assert`.
- [ ] 5.2 Run the build, the tests, and `InputStateAllocationTests` (total 3). Run four plants. Plant 13: `GC.KeepAlive(new object());` as the first line of `InputStateWriter.BeginFrame`. Expect `TheInputPassWithTheStateAllocatesNothingAfterWarmUp` to fail, and the existing gates that run `Update` (for example `PointerAllocationTests` and `ThousandElementAllocationTests`) to fail too. Plant 14: the same line in the `KeyDown` case of `Record`. Expect the input pass test to fail. Plant 15: the same line as the first line of `EndFrame`. Expect the input pass test to fail. Plant 16: the same line as the first line of `UiKeyState.IsDown`. Expect `TheReadsOfTheStateAllocateNothing` to fail. Record the failed tests of each plant and the measured bytes in `context.md`.

**Tasks (group 2, the docs):**
- [ ] 5.3 Edit `docs/technical-design.md`, section 9. Item 1: add the focus event to the list of events, and one sentence: a focus loss calls `UiContext.ClearHeldKeys`. Item 7, the bullet "A switch from `true` to `false` can leave a key down with no key up": add that the switch clears the held keys of `UiContext.Input`, and that an element still gets no key up. Add item 11 "The input state": `UiContext.Input` and `UiContext.GameInput` (Q1), the parts and their sizes, the frame rule and the three hooks, the position rule and the NaN rule, the scroll sum, the button rule and the ownership rule, the key rule, the repeat rule and the modifier rule, the gate rule (A4), the clear rule, the gamepad seam, and the 0-byte rule. End the item with one sentence that names `InputStateTests`, `GlfwUiInputFocusTests`, and the gate.
- [ ] 5.4 Edit `docs/technical-design.md`, section 14. Add a bullet `**Input state tests.**` before the bullet "Sample projects". Name the classes, their counts (32, 5, and 3), and the plants of tasks 1.6, 2.4, 3.5, 3.8, 4.4, and 5.2 with their results. Add the gate bullet with the next ordinal after the last gate of the section (`**A nineteenth allocation gate.**` at planning time, after `ModeDiffAllocationTests`): `InputStateAllocationTests` (3 tests), its scene, its step, and plants 13 to 16 with the failed gates and the bytes.
- [ ] 5.5 Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`, add: `UiInputState`, `UiContext.Input`, and `UiContext.GameInput`, the per-frame input state and its gated view (input/input-state), `UiContext.ClearHeldKeys` (input/input-state), and `UiGamepadState` and `UiContext.SetGamepad`, the gamepad slots and their write seam (input/input-state). Under `### Changed`, add: `GlfwUiInput` subscribes `GlfwWindow.OnFocus` and clears the held keys on a focus loss, and a `KeyboardEnabled` switch to `false` clears the held keys (input/input-state).
- [ ] 5.6 Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the demo commands of each phase and expect the totals 32 (`InputStateTests`), 5 (`GlfwUiInputFocusTests`), and 3 (`InputStateAllocationTests`), each with `failed: 0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes. Record in `context.md` the spelling note of the epic (Notes, gap 1) for `/update-epic`, the demo, the results, and each decision that this plan did not settle.

**Estimated Steps:** about 40 (6 tasks, about six to seven steps for one task)

**Files (group 1, 4):** `InputStateAllocationTests.cs`, `InputStateWriter.cs` and `UiInputState.cs` (plants only, then restored), `context.md`
**Files (group 2, 4):** `technical-design.md`, `CHANGELOG.md`, `context.md`, `implementation.md`

**Acceptance Criteria:**
- [ ] The 3 tests of `InputStateAllocationTests` pass. Each earlier test of this feature still passes. Each existing test passes.
- [ ] The four plants of task 5.2 give a non-zero exit code, and the restored files equal their copies.
- [ ] `scripts/test-linux.sh` prints its PASS line.
- [ ] Section 9 holds item 11, and section 14 holds the bullet `**Input state tests.**` and the gate bullet.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Access: two `ref readonly` properties, `ui.Input` and `ui.GameInput`. The epic spelling `ui.Input.Game` changes to `ui.GameInput`.
- Q2 Focus loss: `UiContext.ClearHeldKeys()`. `GlfwUiInput` subscribes `GlfwWindow.OnFocus` and calls it on a loss. The `KeyboardEnabled` setter calls it on a switch to false.
- Q3 Demo of phase 1: the user moves the mouse over the game area, and a HelloPanel label shows the pointer delta. The label reads 0 over the panel.
- A1 The key part holds three fixed bitsets over the GLFW key codes 0 to 348, with no list. The whole struct stays near 1 KB.
- A2 `UiInputState` holds 16 `UiGamepadState` slots with the full shape: the buttons, six axes, and a connected flag. This feature writes none of them. gamepad-axes writes them.
- A3 The input pass records, for each button, if the down hit the UI. The game view drops the released flag of such a button. The raw state keeps it.
- A4 The game view applies the gate one time, at the end of the drain, from `PointerOverUi` and `KeyboardFocusInUi` at that point.
- A5 The scroll delta of a frame is the sum of the scroll events of the frame. The pointer delta is the position of this `Update` minus the position of the last `Update`.
- A6 The modifiers of the key part come from the last key event of the frame.
- A7 `UiActionMap`, `OnUnhandledInput`, and `UiRoutingGate` do not change in this feature.
- A8 The tests go to `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs` and `InputStateAllocationTests.cs`. The focus loss test goes to `tests/BrutalUi.Glfw.Tests/`.
- A9 Codebase fact: `GlfwUiInput` subscribes no focus event today. `Brutal.GlfwApi.GlfwWindow.OnFocus` exists in `Ahwoo.BRUTAL.Glfw` 2026.10.4, the pinned version.

### Decision 1: The drain writes the state entry by entry (the chosen approach)
**Choice:** `DrainEvents` writes the raw state as each entry passes, records the press owner from the target of the entry, and writes the game view at the end of the drain. The frame values clear at the start of each drain.
**Rationale:** The drain is the one place that knows both the entry and its hit. The ring pops each entry, so no second pass can read the entries after the drain. One pass gives exact ownership and 0 bytes. The value that the game reads after `Update` is the value that Q2 of the epic asks for: the state after the drain.
**Alternatives:** (a) A second pass over a copy of the ring: a second buffer and a second hit test. (b) A write at the push: the push knows no hit, so no ownership. (c) A read of the GLFW key state at `Update`: Core would name GLFW, against A2 of the epic.

### Decision 2: The writer is an internal struct field of `UiContext`, not a partial class
**Choice:** `internal struct InputStateWriter` in `src/BrutalUi.Core/Input/InputStateWriter.cs`. `UiContext` holds it in `_input` and calls it from three hooks.
**Rationale:** `UiContext` is not `partial` today, and `EventQueue` already sets the pattern of a mutable struct in a field that changes only through the field. The writer holds no reference, so a struct field costs no allocation and no indirection. `UiContext.cs` (about 2200 lines) grows by the hooks, two properties, two methods, and their doc comments. The writer can be tested through `UiContext` only, so no new seam is needed.
**Alternatives:** (a) A `UiContext` partial file: a new pattern for one feature. (b) A sealed class: one more object and one more indirection for each read.

### Decision 3: The parts are structs with private fields and `ref readonly` access
**Choice:** Each part has private fields, public `readonly` queries, and `internal` write methods. `UiInputState` gives each part through a `[UnscopedRef] public readonly ref readonly` member, and `UiContext` gives the state through a `ref readonly` property.
**Rationale:** A read such as `ui.GameInput.Keys.IsDown(87)` copies no byte of the 136-byte key part. The game cannot write the state through a `ref readonly`, and the private fields stop a write through a copy. The internal methods keep each bit rule next to its bits. The group names (`Pointer`, `Buttons`, `Keys`, `Gamepad`) match the parts of the requirements and the bind kinds of input-actions. `UnscopedRef` lets a `readonly` struct member return a reference to its own field, which C# 11 and later need.
**Alternatives:** (a) A flat struct with `IsKeyDown`, `PointerDelta`, and other members: one wide type, and the group names of the requirements go away. (b) Parts returned by value: a copy of 136 bytes for each key read. (c) Public fields: the game can write a copy, and the bit layout becomes an API.

### Decision 4: Three hooks in `DrainEvents`, and hook 3 reads `RoutingGate`
**Choice:** Hook 1 after the stale rules, hook 2 after the target step, and hook 3 in the `finally` block after `FinishRelease`. Hook 3 calls `_input.EndFrame(RoutingGate)`.
**Rationale:** After the stale rules, the stored hit and the focus owner are valid. After the target step, `target` holds the hit or the target from the caller, so hook 2 knows if a press went to the UI. In the `finally` block, hook 3 also runs after an exception of a handler, so the delta and the game view match the entries that ran. `RoutingGate` is the single source of the gate, so the game view and the value that the game reads after `Update` always agree (A4, A7: the gate type does not change).
**Alternatives:** (a) Hook 3 after the loop only: an exception leaves the delta and the game view of the last frame. (b) A gate for each entry: against A4, and a frame with a move over the UI and then over the game would be half gated.

### Decision 5: The raw state records each pointer, scroll, and key entry, also with a target from the caller
**Choice:** Hook 2 runs for each entry. A press with a target from the caller counts as a press of the UI.
**Rationale:** The raw state is the input that reached the context. A target from the caller sends the entry to an element, so the UI owns it, and the ownership rule hides it from the game view. One rule for all entries keeps the hook simple and testable (`AnEntryWithATargetFromTheCallerMovesTheRawPosition`, `APressWithATargetFromTheCallerBelongsToTheUi`).
**Alternatives:** Record only entries with no target: the raw state then misses an input that an element got.

### Decision 6: (NaN, NaN) is the "no position" value
**Choice:** `Position` starts at (NaN, NaN) and takes each value of the entries, also the (NaN, NaN) of a cursor leave. `Delta` is (0, 0) when the start or the end position holds NaN. The empty pointer part of the game view has the position (NaN, NaN) and zero deltas.
**Rationale:** The adapter already uses (NaN, NaN) for a cursor leave, and the hit test treats a NaN point as no hit. The requirement asks for a zero delta at (NaN, NaN). One value for "no position" needs no extra flag.
**Alternatives:** A `HasPosition` flag: one more member, and the leave still needs the NaN rule.

### Decision 7: The masks cover GLFW buttons 0 to 7 and keys 0 to 348, and an up releases only a held input
**Choice:** Three `byte` masks for the buttons and three bitsets of 11 `uint` words for the keys (A1). A value outside the range changes no bit and reads `false`. An up sets the released flag only when the down flag is set.
**Rationale:** `PointerButton` names three buttons, but the adapter casts each GLFW button (0 to 7) with no table, so a mask of 8 bits loses no button. GLFW key codes run from 32 to 348, and -1 is the unknown key. The released rule stops a stray release after a key down that `KeyboardEnabled` dropped, or after a clear. The struct stays at about 800 bytes.
**Alternatives:** (a) A mask over `PointerButton` only: buttons 3 to 7 vanish. (b) A list of held keys: against A1, and it can grow.

### Decision 8: A press belongs to the side that got the down, for all three flags
**Choice:** `_uiPresses` records the owner of the last down of each button. The game view drops the down and pressed flags of a button that the UI owns, and the released flag of each up of such a press in the frame (`_uiReleases`). The raw state keeps all flags.
**Rationale:** A3 asks for no released flag in the game view after a press on the UI. A down flag with no press would also tell the game that a UI drag is a held game button, and a map would pan under a slider drag. Thus the rule drops the down flag and the pressed flag too, which matches "a press belongs to the side that got the down". The second mask keeps the rule exact when one frame holds a UI release and a new game press of the same button.
**Alternatives:** (a) Drop the released flag only (the letter of A3): the game sees a held button for a UI drag. (b) One mask: a UI release and a game press in one frame show the UI release to the game.

### Decision 9: A repeat sets the down flag and no pressed flag, and the modifiers stay until the next key event
**Choice:** A key down with `IsRepeat` sets the down bit only. The modifiers take the value of each key event in order, also of a code outside the range, and keep it across frames with no key event.
**Rationale:** The requirement and the repeat rule of the action map ask for no pressed flag on a repeat. A repeat of a key that is not down (after a clear, while the key stays down) shows the key as held again, which matches the device. A6 names the last key event of the frame. A frame with no key event has no such event, so the last known value stays.
**Alternatives:** (a) A repeat changes nothing: a key that stays down after a clear never reads as held again. (b) Modifiers reset each frame: a held Shift reads `None` in a frame with no key event.

### Decision 10: `ClearHeldKeys` sets a request, and the end of the next drain applies it
**Choice:** The call sets `_clearKeys`. Hook 3 sets the released flag of each held key, clears each down flag, and sets the modifiers to `None`, before the delta and the game view. The adapter calls it from `OnFocus` on a loss and from the `KeyboardEnabled` setter on a switch from `true` to `false`.
**Rationale:** The adapter calls it during `Glfw.PollEvents`, between two `Update` calls. A key down that GLFW queued before the focus loss is still in the ring. An apply at the end of the drain clears that key too, so no key stays down. An apply at once would lose that key and lose the released flag at the next frame start. The released flag tells a consumer that waits for a release that the key is up. GLFW also sends synthetic key releases after the focus callback, and the released rule of Decision 7 stops a second release.
**Alternatives:** (a) An apply at once: a key down still in the ring stays held. (b) Push key up events for each held key: against "no new event type" in spirit, and up to 349 entries in the ring. (c) No released flag on a clear: Notes question 3.

### Decision 11: The gamepad slots, the edges, and the write seam
**Choice:** `UiGamepadState` holds `Connected`, `Buttons`, `Pressed`, `Released`, and six axes. `UiContext.SetGamepad(index, state)` writes the raw slot at once and computes the edges from the old buttons. The drain does not clear the gamepad edges, and the game view copies the slots with no gate.
**Rationale:** A2 asks for the full shape, and input-actions needs pressed and released flags for a gamepad button. The epic overview states that gamepad-axes changes `BrutalUi.Glfw` and the Core enum only, and `BrutalUi.Glfw` cannot write internal fields of Core. The seam keeps this feature the one `UiContext` change of the epic. The edges come from one place, so the adapter stays a plain poll. The gate does not apply, because the routing gate covers the pointer and the keyboard only. This feature writes no gamepad value itself (A2). The tests use hand-made states.
**Alternatives:** (a) gamepad-axes adds the seam: a second `UiContext` change in the epic. (b) The adapter computes the edges: each poll source repeats the rule. See Notes question 1.

### Decision 12: The game view is one copy of the raw state at the end of the drain
**Choice:** Hook 3 copies `Raw` into `Game` and then empties the parts that the gate closes.
**Rationale:** One copy of about 800 bytes in each `Update` costs less than a check in each read, and the game reads plain data. The view is fixed after `Update`, so two reads in one frame always agree.
**Alternatives:** A view struct that checks the gate at each read: each query pays a branch, and a focus change after `Update` changes the answer in the middle of a frame.

### Decision 13: The 0-byte proof is a new numbered gate, `InputStateAllocationTests`
**Choice:** A new class with 3 tests on `ThousandElementTree`, the next ordinal in section 14 (the nineteenth at planning time). Each existing gate also covers the hooks, because each one runs `Update`.
**Rationale:** A3 of the epic asks the gate to grow to cover the state. A8 names the file. A gate of its own gives the plants a clear target, and the existing gates show that the hooks cost nothing on each path.
**Alternatives:** A case in `PointerAllocationTests`: the key, the clear, and the gamepad paths do not fit its scene.

### Decision 14: The HelloPanel label shows the game view in the widget panel
**Choice:** One new label after the gate label. It shows `Game pointer delta: <x>, <y>. Held key: <key>` from `ui.GameInput`, written after the draw inside the UI region.
**Rationale:** Q3 asks for a label that reads 0 over the panel, and only the game view does that. The UI region of the sample measures 0 bytes, so the label write and the reads join the sample check with no new code. The gate label next to it explains the 0.
**Alternatives:** A label for the raw view too: two labels for one demo (YAGNI).

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A hook in `DrainEvents` changes an input rule (the focus, the Tab move, the release, or the unhandled input step). | Low | High | The hooks only write the writer. Each existing input test runs with no edit. Phase 1 runs on an ADVANCED agent, and its acceptance criteria check that no existing code line changes. |
| A hook or a read allocates, and each gate that runs `Update` fails. | Low | High | The writer and the parts hold no reference, and no method allocates. The existing gates run from phase 1 on. `InputStateAllocationTests` and plants 13 to 16 pin it. |
| `[UnscopedRef]` on a `readonly` member, or the `ref readonly` return of an inline array element, does not compile. | Low | Medium | C# 11 and later allow both. When the compiler rejects one, return that part by value (no allocation, a copy of at most 136 bytes), record the change in `context.md`, and keep the names. |
| The ownership rule hides or leaks a press in a corner case. | Medium | Medium | Two masks (Decision 8). Four tests pin the cases: a UI press that ends over the game, a game press, a new game press after a UI release in one frame, and a press with a target from the caller. |
| A game press dragged over the UI loses its release in the game view. | Medium | Medium | The gate empties the button part over the UI (the requirements and A4). The game sees the down flag go `false`. input-actions can treat that as a release. Notes question 2 holds the open choice. |
| A key down after a focus loss in the same frame clears too. | Low | Low | The clear applies at the end of the drain (Decision 10). The next key down sets the key again. The remarks state it. |
| GLFW synthetic releases and the clear give two releases. | Low | Low | An up releases only a held key (Decision 7). `AFocusLossClearsTheHeldKeys` covers the clear, and the remarks state the order. |
| The parameter type of `GlfwWindowFocusCallback` is not known at planning time. | Medium | Low | Task 3.6 reads it from the compiler error and records it. The XML docs name a "focus event" parameter. |
| HelloPanel sets `KeyboardEnabled` in each frame, and each write clears the keys. | Medium | Medium | The setter clears only on the switch from `true` to `false`. `OnlyTheSwitchFromTrueToFalseClears` and plant 10 pin it. |
| The panel-sample changes of HelloPanel are not committed at the start. | Medium | Medium | Task 1.1 checks `git status`. Start phase 1 only after the panel-sample commit, so the diff of this feature stays clean. |
| Parallel test classes share the class table. | Medium | Low | Each test builds its own `PointerTree` with a unique prefix. |
| The test host exits with 134, or `TheCurvesAllocateNothing` fails one time. | Medium | Low | Run the tests a second time. |
| `dotnet build` or `dotnet test` hangs on stale MSBuild nodes. | High | Low | Each command sets `MSBUILDDISABLENODEREUSE=1` and `-nodeReuse:false`. The class runs use the built binaries. No `dotnet build-server shutdown`. |

---

## Dependencies

### Internal Dependencies
- input-routing (done): the ring, `DrainEvents`, the target step, `RoutePointer`, the focus owner, `RoutingGate`, `PointerHit`, and `FocusedElement`.
- state-and-commands (done): `UiGamepadButtons`, `PollGamepad`, and `UiActionMap` (no change).
- panels-and-modes/mode-set (done): `OnBeforeUpdate` runs before the drain (no change).
- panels-and-modes/panel-sample (on the branch): the current HelloPanel files. It must be committed before phase 1.
- The test helpers `PointerTree.cs`, `ThousandElementTree.cs`, `AllocationGate.cs`, and `RecordingRenderer.cs` (done).
- dev-ops: `brutal-ui.sln` and `scripts/test-linux.sh`.

### External Dependencies
- `Ahwoo.BRUTAL.Glfw` 2026.10.4 (pinned): `GlfwWindow.OnFocus` and `GlfwWindowFocusCallback`. No new package and no native dependency.

### Sibling Features That Depend on This One
- gamepad-axes (`UiGamepadState`, `SetGamepad`, `Gamepad(int)`).
- input-actions (`GameInput` and each query of the Public API).
- control-schemes and binding-remap, through input-actions.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. `ui.Input.Pointer` holds the position of the last pointer or scroll entry, (NaN, NaN) before the first entry and after a leave, and the delta since the last `Update`. A NaN position gives a zero delta. An entry with a target from the caller moves the raw position (`InputStateTests`, phase 1 tests).
- [ ] F2. `ui.GameInput` has an empty pointer part when `RoutingGate.PointerOverUi` is `true` at the end of the drain, and equals the raw pointer part otherwise (`InputStateTests`).
- [ ] F3. The button part holds down, pressed, and released for the GLFW buttons 0 to 7. An up of a button that is not down sets no released flag. The scroll delta is the sum of the frame. The game view empties both over the UI. A press that went down over the UI shows no down, pressed, or released flag in the game view, and the raw view keeps them. A press with a target from the caller belongs to the UI (`InputStateTests`, phase 2 tests).
- [ ] F4. The key part holds down, pressed, and released for the codes 0 to 348. A repeat sets no pressed flag. The modifiers come from the last key event. A code outside the range reads `false`. The game view empties the key part when the UI holds the focus (`InputStateTests`, phase 3 tests).
- [ ] F5. `ClearHeldKeys` releases each held key at the next `Update`, also a key down of the same frame. A focus loss and a `KeyboardEnabled` switch from `true` to `false` call it. A focus gain, a write of the same value, and a call after `Dispose` of the adapter do not clear (`InputStateTests`, `GlfwUiInputFocusTests`).
- [ ] F6. `UiInputState` holds 16 disconnected gamepad slots. `SetGamepad` writes a slot and its edges, the drain keeps the edges, and the game view copies the slots with no gate. A bad index throws `ArgumentOutOfRangeException`. `Unsafe.SizeOf<UiInputState>()` is 1024 or less (`InputStateTests`, phase 4 tests).
- [ ] F7. The input pass with the state, `ClearHeldKeys`, `SetGamepad`, and each read allocate 0 bytes after warm-up in Debug. Plants 13 to 16 fail the gate (`InputStateAllocationTests`).
- [ ] F8. HelloPanel shows `Game pointer delta: <x>, <y>. Held key: <key>` in a label of the widget panel. Over the game area, a mouse move shows a delta that is not 0. Over the panel, the label reads `0, 0`. A held W over the game area shows `Held key: W`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln -nodeReuse:false` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with the skipped count of `<base>`. `InputStateTests` holds 32 tests, `InputStateAllocationTests` 3, and `GlfwUiInputFocusTests` 5. `BrutalUi.Core.Tests` holds 35 more tests than at `<base>`, and `BrutalUi.Glfw.Tests` holds 5 more.
- [ ] Q3. Each public member of the new types and the new `UiContext` members has an XML doc comment. The new doc comments and the changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q4. `InputStateWriter.cs`, `UiInputState.cs`, and `UiGamepadState.cs` hold no array creation, no `lock`, no `IDisposable`, no `event` keyword, no `ToString(` call, no `(object)` cast, and no `List<` in code.
- [ ] Q5. Each existing allocation gate passes with no edit.

### Integration Criteria
- [ ] I1. The Core diff from `<base>` holds only `src/BrutalUi.Core/Tree/UiContext.cs` (changed) and `src/BrutalUi.Core/Input/UiInputState.cs`, `UiGamepadState.cs`, and `InputStateWriter.cs` (new). The Glfw diff holds only `src/BrutalUi.Glfw/GlfwUiInput.cs`. The sample diff holds only `HelloScreen.cs`, `HelloModes.cs`, and `Program.cs`. The changed existing test files are `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`, with one added line, and `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`, with the optional `repeat` parameter of Decision 15. No project file and no file of `scripts/` changes.
- [ ] I2. `UiActionMap.cs`, `UiRoutingGate.cs`, and `UiUnhandledInputHandler.cs` do not change (A7). In `UiContext.cs`, the writer calls `BeginFrame`, `Record`, and `EndFrame` appear only in `DrainEvents`, `Create` only in the two constructors, `RequestKeyClear` only in `ClearHeldKeys`, and the writer `SetGamepad` only in `UiContext.SetGamepad`.
- [ ] I3. Section 9 of `docs/technical-design.md` holds item 11 with the input state, and items 1 and 7 name the focus clear. Section 14 holds the bullet `**Input state tests.**` and the gate bullet of `InputStateAllocationTests`. `CHANGELOG.md` names `UiInputState`, `UiContext.Input`, `UiContext.GameInput`, `ClearHeldKeys`, `UiGamepadState`, `SetGamepad`, and the `GlfwUiInput` change.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line, so the HelloPanel smoke run keeps 0 bytes in its UI region with the label write.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` and the base counts from `docs/features/input/input-state/context.md`. Do not run `dotnet build-server shutdown`. When a run exits with 134 or `TheCurvesAllocateNothing` fails, run it a second time.

1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and the skipped count of `context.md` (Q2, Q5).
3. Run `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests' 2>&1 | tail -n 5` and expect a total of 32 (F1 to F6). Run the same binary with `'*InputStateAllocationTests'` and expect 3 (F7). Run `tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*GlfwUiInputFocusTests' 2>&1 | tail -n 5` and expect 5 (F5). Each reads `failed: 0`. Run both binaries with no filter and compare the totals with the base counts plus 35 and plus 5 (Q2). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Core/Input/InputStateWriter.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `BeginFrame`. Run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp` (F7). Repeat with the `_uiReleases` mask removed from `EndFrame` (F3) and with the key clear of `EndFrame` removed (F5). Copy `src/BrutalUi.Glfw/GlfwUiInput.cs` to `<scratch>`, make the `KeyboardEnabled` setter call `ClearHeldKeys` for each write, run the same command, expect a value that is not 0, and restore it with `cmp` (F5). Run step 2 again.
5. `grep -nE 'new [A-Za-z0-9_]+\[|\block\b|IDisposable|\bevent\b|ToString\(|\(object\)|List<' src/BrutalUi.Core/Input/InputStateWriter.cs src/BrutalUi.Core/Input/UiInputState.cs src/BrutalUi.Core/Input/UiGamepadState.cs | grep -v '///'`. Expect no line (Q4).
6. `grep -n '_input\.' src/BrutalUi.Core/Tree/UiContext.cs`. Expect `BeginFrame`, `Record`, and `EndFrame` on lines inside `DrainEvents`, `Create` in the two constructors, `RequestKeyClear` in `ClearHeldKeys`, `SetGamepad` in `SetGamepad`, and `Raw` and `Game` in the two properties (I2).
7. `git diff <base> --name-only -- src/ samples/ scripts/ tests/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/ scripts/ tests/`. Expect only the files of I1, with `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` as the changed file that I1 names, and the three new test files `InputStateTests.cs`, `InputStateAllocationTests.cs`, and `GlfwUiInputFocusTests.cs`. `git diff <base> -- src/BrutalUi.Framework/ src/BrutalUi.Core/Input/UiRoutingGate.cs src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs`. Expect no output (I2). `git diff <base> -- tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs | grep -E '^[+-] '`. Expect one added line with `OnFocus` (I1).
8. `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4).
9. The manual check of HelloPanel (F8) on a machine with a display: run `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. Move the mouse in the empty area outside the panels, and read a delta that is not `0, 0` in the label under the gate label. Move the mouse over a panel, and read `0, 0` while the gate label reads `Pointer over UI: yes`. Hold W with the mouse over the empty area and no focused element, and read `Held key: W`. Click the text field, hold W, and read `Held key: none`. Press Alt+Tab with W held and come back, and read `Held key: none`.
10. Read section 9 and section 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I3). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q3).

---

## Testing Strategy

### Unit Tests
- `Core.Tests/Input/InputStateTests` (32): the pointer position, the delta, the NaN rule, the pointer gate and its timing, an entry with a target from the caller (phase 1, 8). The button flags, the ownership cases, and the scroll sum (phase 2, 10). The key flags, the repeat rule, the modifiers, the range, the key gate, and the clear rule (phase 3, 8). The gamepad slots, the edges, the copy with no gate, the constructor, and the size (phase 4, 6). Each test runs the real `UiContext.Update` on `PointerTree`, so it also covers the hooks.
- `Glfw.Tests/GlfwUiInputFocusTests` (5): the focus loss, the focus gain, the `KeyboardEnabled` switch, the edge rule of the setter, and `Dispose`, on the seam adapter.

### Integration Tests
- `GlfwUiInputTests.ARealWindowSubscribesAndUnsubscribes` counts the `OnFocus` handler with the other handlers on a hidden window, when a display exists.
- The HelloPanel smoke run of `scripts/test-linux.sh` runs the label write in each frame with the smoke input, and the sample check of 0 bytes covers it.

### Allocation Tests
- `InputStateAllocationTests` (3, the new gate): the input pass with each entry type, a clear, and a gamepad write, the reads of each query on both views, and a check of real work. Plants 13 to 16 guard it.
- Each existing gate that runs `Update` (for example `ThousandElementAllocationTests` and `PointerAllocationTests`) covers the three hooks from phase 1 on.

### Regression Tests
- Each existing test runs with no edit, except the one added line in `EventNames`. The input tests of input-routing (`RoutingGateTests`, `PointerPressTests`, `UnhandledInputTests`) guard the drain order.

### Manual Testing
- Step 9 of the Verification Method: the pointer delta over the game area and over the panel, a held key, a focused text field, and a focus loss.

---

## Related Documentation

- **Requirements:** `docs/features/input/input-state/requirements.md`
- **Epic:** `docs/features/input/epic-overview.md`, `docs/features/input/epic-requirements.md`, `.beast-mode/interview.md`
- **Siblings:** `docs/features/input/gamepad-axes/requirements.md`, `docs/features/input/input-actions/requirements.md`, `docs/features/input/control-schemes/requirements.md`, `docs/features/input/binding-remap/requirements.md`
- **Design:** `docs/technical-design.md` sections 4 (frame order), 5 ("Events"), 9 (Input and Action map), and 14 (Testing). `docs/mission-statement.md`
- **Format reference:** `docs/features/panels-and-modes/mode-set/implementation.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Tree/UiContext.cs` (`DrainEvents`, `RoutePointer`, `PressButton`, `RoutingGate`, `_events`), `src/BrutalUi.Core/Input/EventQueue.cs` (the mutable struct field, the payload overlap), `src/BrutalUi.Core/Input/UiRoutingGate.cs` (a value the game reads), `src/BrutalUi.Core/Tree/ClassSet.cs` and `src/BrutalUi.Core/Style/TransitionRecord.cs` (`InlineArray`), `src/BrutalUi.Glfw/GlfwUiInput.cs` (the handler fields, the seam methods, `Dispose`), `samples/HelloPanel/HelloWidgets.cs` (`WriteCount`, the scratch buffer), `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs` (the gate shape), `tests/BrutalUi.Glfw.Tests/GlfwUiInputKeyTests.cs` (the seam adapter in a test)
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (A) of each question. A change before the phase that the question names costs little.

1. **Who adds the write seam of the gamepad slots?** (before phase 4)
   - **(A) Recommended:** this feature adds `UiContext.SetGamepad` and the public constructor of `UiGamepadState`. gamepad-axes then changes `BrutalUi.Glfw` and adds the Core enum `UiGamepadAxis` only, as the epic overview states (Decision 11).
   - (B) this feature defines the slots with no write path, and gamepad-axes adds the seam: a second `UiContext` change in the epic.
2. **What does the game view show for a game press that the pointer drags over the UI?** (before phase 2)
   - **(A) Recommended:** the gate empties the button part over the UI, as the requirements and A4 state. The game sees the down flag go `false` with no released flag, and gets the pointer back when the pointer leaves the UI.
   - (B) the ownership rule keeps a game press in the game view over the UI, so a map drag continues across a panel. The pointer part then needs the same rule, and the gate of the requirements changes.
3. **Does `ClearHeldKeys` set the released flag?** (before phase 3)
   - **(A) Recommended:** yes. Each cleared key reads released for one frame, so a consumer that waits for a release sees one (Decision 10).
   - (B) no. The clear only drops the down flags.

### Conflicts and Gaps

1. **The spelling of the game view.** Q1 changes `ui.Input.Game` to `ui.GameInput`. The old spelling stays in `docs/features/input/epic-overview.md` (Integration & Architecture, "Within the epic"), in `docs/features/input/input-state/requirements.md` (line 17), and in `docs/features/input/input-actions/requirements.md` (lines 16 and 27). `/update-epic input` fixes them. Task 5.6 records the note in `context.md`.
2. **A2 and the write seam.** A2 says that this feature writes no gamepad slot. `SetGamepad` writes only the values that its caller gives, and this feature calls it only from tests. The plan keeps A2 and records the seam as Notes question 1.
3. **A2 and the gamepad edges.** A2 names the buttons, six axes, and a connected flag. The plan reads "the buttons" as the down, pressed, and released masks, because input-actions needs the edges of a gamepad button.
4. **A3 and the down flag.** A3 names the released flag. The plan also drops the down and pressed flags of a UI press in the game view (Decision 8), because a down flag with no press would show a UI drag as a held game button. The raw state keeps all flags, as A3 states.
5. **Q2 of the epic and the per-entry write.** The requirements say that the input pass writes the state after it drains the ring. The chosen approach writes inside the drain, entry by entry, and writes the game view after the last entry. The value that the game reads after `Update` is the same.
6. **panel-sample is not committed.** At planning time, the branch `feat/panels-and-modes/panel-sample` holds uncommitted changes of `HelloScreen.cs`, `HelloState.cs`, `Program.cs`, and the new `HelloModes.cs`. This plan builds on that state (`HelloModes.GateLabel`, the mode loop). Task 1.1 checks that the commit exists before phase 1 starts. The gate ordinal of task 5.4 (the nineteenth) also assumes that `ModeDiffAllocationTests` is the eighteenth.
7. **The focus callback signature.** The XML docs of `Ahwoo.BRUTAL.Glfw` 2026.10.4 name the parameters `window` and `focusEvent`, but not the type of `focusEvent`. Task 3.6 reads it from the compiler.
8. **`PointerButton` names three buttons.** The requirement says "for each `PointerButton`". The adapter casts each GLFW button with no table, so the plan covers 0 to 7 (Decision 7). The enum does not change.
9. **The Core test count is not known.** Task 1.1 records the counts of `BrutalUi.Core.Tests` and `BrutalUi.Glfw.Tests`. At the end, they read the base counts plus 35 and plus 5.
10. **The plan is not compiled, and the prose lint did not run.** The planner had no shell tool. Task 1.6 is the first compile, and task 5.6 runs the lint on this plan and on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
