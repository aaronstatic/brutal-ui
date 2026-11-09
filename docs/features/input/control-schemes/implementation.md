# Control Schemes - Implementation Plan

**Epic:** input (feature 4 of 5)
**Status:** Planning
**Started:** 2026-11-09
**Target Completion:** TBD
**Last Updated:** 2026-11-09 19:55

---

## Quality Bar

**Target quality:** A scheme layer on `UiActionMap` that a game builds once and reads in each frame. spacegame map-controls and any pad-first game get one name per device group, the active device for the prompt glyphs, and a lock for a menu that only takes the pad. control-schemes is the base that binding-remap writes into each text line. A wrong device report shows the wrong glyph. A missed change call leaves a stale glyph on screen. A scheme filter that misses one path lets a locked menu still run a keyboard bind. A hidden allocation in the read or the change check fails the HelloPanel gate in each frame.

- **One id, one name:** `AddScheme` gives one int id per UTF-8 name. Two calls with the same bytes give one id, and `SchemeName` gives the bytes back. A name of `Gamepad` and a name of `gamepad` are two names. The map holds `KeyboardMouse` and `Gamepad` from construction, so a game needs no add for the two standard names.
- **A device report that a player can read:** `ActiveScheme` follows the input, not the tag. A key, a mouse button, the pointer delta, a scroll, and a key composite give `KeyboardMouse`. A gamepad stick, a gamepad axis, and a gamepad button give `Gamepad`. A value of exactly 0 gives no value, so the stick at rest and a released trigger change nothing.
- **One call per change:** `OnSchemeChanged` runs inside `Update`, after the frame read, and only when the reported scheme changed. A frame with the same device calls nothing. A frame that flips twice in the read calls one time with the last value.
- **A lock that holds on every path:** `SetScheme` filters the frame read, the key and mouse press path, and the gamepad command invoke, including `PushGamepad`. A binding of the default scheme reads under every lock. A locked map reports the locked scheme.
- **No break of the shipped surface:** The eleven current public build signatures keep their shape and bind into the default scheme. The five queries, `GamepadSlot`, `Activate`, `IsActive`, `Deactivate`, `PushGamepad`, and `InvokeAction` keep their signatures and their tested behavior. The switch rule and the `IsActive` pair test of input-actions keep passing.
- **0 bytes after warm-up:** The scheme filter, the device track, the change check, the slot call, `ActiveScheme`, `LockedScheme`, and `SchemeName` allocate 0 bytes in the default Debug build. `ActionMapAllocationTests` grows with a measured body that drives a device switch, and a plant fails it.
- **A small named diff:** `BrutalUi.Core` and `BrutalUi.Glfw` do not change. Three new files and four edited files in `src/BrutalUi.Framework/Actions/` hold the whole feature. HelloPanel gains one label.
- **Accurate docs:** Section 9 of the technical design gains one item for the schemes, and the map-wide duplicate sentence becomes the per-scheme rule. `CHANGELOG.md` gains one line. Each public member has an XML doc comment in ASD-STE100 prose.

---

## Executive Summary

This feature adds named control schemes to `UiActionMap`. A scheme is one int id that resolves back to a UTF-8 name. Each binding carries one scheme id, and id 0 is the default scheme that belongs to every scheme. The map reports the active device from the last binding that gave a value, calls a slot when that report changes, and can lock to one scheme so a binding of another scheme gives no value.

- Framework gets `UiScheme` with the standard names `KeyboardMouse` and `Gamepad`, and `UiSchemeChangedHandler`. Core and `BrutalUi.Glfw` do not change (A1).
- `UiActionMap` gets `AddScheme(ReadOnlySpan<byte>)`, `SchemeName(int)`, `ActiveScheme`, `LockedScheme`, `OnSchemeChanged`, `SetScheme(int)`, and the private scheme table in the new partial file `UiActionMap.Schemes.cs`.
- `InputBinding` gains one `int Scheme` field. Each of the nine public binds gains one scheme overload with a trailing scheme id. The eleven current signatures stay and forward with id 0 (CR-4).
- `Find` and `FindValue` take the scheme. The duplicate rule becomes per scheme, on the set rule that CR-3 shipped (`UiActionMap.Bindings.cs:779-813`).
- `ReadFrame` filters each binding by the lock, tracks the device of each binding that gave a value, and calls `OnSchemeChanged` at the end of the read on a change only.
- HelloPanel gains a sixth label that shows the active scheme, and its six standard binds gain the two scheme tags.
- binding-remap inherits the id that resolves to a UTF-8 name, the per-scheme duplicate rule, and the lock. `FindBindingIndex` and the read-one-binding hook stay there (A4, CR-5).

The plan has 5 phases and 34 tasks, about 245 steps. Phase 1 (11 tasks) delivers the scheme table, `ActiveScheme`, and the HelloPanel label, which is the demo of the confirmed Q4. Phase 2 (7 tasks, ADVANCED) delivers the scheme tag, the nine overloads, and the per-scheme duplicate rule. Phase 3 (5 tasks) delivers the `OnSchemeChanged` slot. Phase 4 (5 tasks, ADVANCED) delivers the lock on every path. Phase 5 (6 tasks) grows the allocation gate and updates the docs.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The game developer** (adds two schemes at build time, tags each bind, and sets the prompt glyph from `map.ActiveScheme` in a slot that runs only on a change)
- [ ] **The pad-first game** (locks the map to `Gamepad` in a menu, so a stray key gives no value, and clears the lock on the way out)
- [ ] **The sibling features** (binding-remap writes the scheme name of one binding in each text line and rebinds inside one scheme)

**User Subsets Served:**
- [ ] **Simple** - a game calls `AddScheme(UiScheme.Gamepad)`, tags its pad binds, and swaps one sprite on `OnSchemeChanged`.
- [ ] **Complex** - a game adds a third scheme for a flight stick, binds the same key in two schemes for two actions, locks the map per mode, and reads the name bytes of `ActiveScheme` for its glyph table.

**UX Approach:**
- Build time: `AddScheme`, then the binds with a scheme id. A bind with no id lands in the default scheme.
- Frame time: nothing new to call. The read inside `Update` tracks the device and calls the slot.
- No name lookup in the frame path: one int per binding, one compare per path.
- No glyph table, no persistence, no per-player scheme, no rebind screen, and no file IO (Out of Scope).

---

## Goals

### Primary Goals
1. Add `UiScheme`, the scheme table, `AddScheme`, and `SchemeName` (requirements lines 12 and 13).
2. Report `ActiveScheme` from the device of the last binding that gave a value (requirements line 14, Q1, Q3).
3. Widen the bind surface with the nine scheme overloads and the per-scheme duplicate rule (CR-4).
4. Call `OnSchemeChanged` in `Update`, after the read, on a change only (requirements line 15).
5. Lock the map with `SetScheme` on all three paths (requirements line 16, Q2).
6. Prove 0 bytes for the read, the device track, and the change check (requirements line 17, A3, A5).
7. Show the active scheme in HelloPanel, and state the rules in section 9 and the changelog (requirements line 18, A6, A9 of the epic).

### Success Criteria
- [ ] The phase 1 demo passes: HelloPanel reads `Scheme: Keyboard / Mouse`, and a stick move or a pad press flips it to `Scheme: Gamepad`. On a host with no window, `ActiveSchemeTests` (9) reads `failed: 0` in place of the live check.
- [ ] The phase 2 demo passes: `SchemeGroupTests` reads a total of 8 with `failed: 0`.
- [ ] The phase 3 demo passes: `SchemeChangedSlotTests` reads a total of 6 with `failed: 0`.
- [ ] The phase 4 demo passes: `SchemeLockTests` reads a total of 7 with `failed: 0`.
- [ ] The phase 5 demo passes: `ActionMapAllocationTests` reads a total of 7 with `failed: 0`.
- [ ] Each existing test passes. The one edited existing test class is `ActionMapAllocationTests` (the scene, `SchemeStep`, and `TheMeasuredBodyDoesRealWork`).

---

## Architecture Overview

### The Place in the Epic

input-state gives `GameInput` and the gate. gamepad-axes stores each gamepad value after the deadzone, so a stored value that is not exactly 0 is input above the deadzone (A2, `docs/features/input/gamepad-axes/implementation.md:209`). input-actions gives the typed binds, the frame read, and the slot pair. This feature builds on input-actions and reads no raw state of its own (`docs/features/input/input-state/implementation.md:82`). binding-remap edits the tagged bindings and writes the scheme name in each line.

```
Frame N (0 bytes after warm-up)
  Glfw.PollEvents()
  input.PollGamepads()                         gamepad-axes: SetGamepad x16, the deadzone baked in
  ui.Update(dt)
    OnBeforeUpdate                             UiManager: the mode diff, map.Activate (unchanged)
    DrainEvents()                              input-state: raw state, then GameInput at the end
      OnUnhandledInput (per entry)             UiActionMap.HandleInput: scheme filter (phase 4)
    OnInputRead(this)                          UiActionMap.ReadFrame (input-actions)
      filter each binding by the lock          NEW (phase 4)
      read each binding, track the device      NEW (phase 1, 4)
      finish each action                       unchanged
      invoke the gamepad press commands        scheme filter (phase 4)
      OnSchemeChanged on a change              NEW (phase 3)
    UpdateBindings, styles, transitions, layout, caret, clear
  the game reads map.ActiveScheme, map.SchemeName(map.ActiveScheme)
```

### File Tree (end state)

```
src/BrutalUi.Framework/Actions/
├── UiScheme.cs                        # phase 1 (new): the two standard names
├── UiSchemeChangedHandler.cs          # phase 3 (new): the delegate type of the slot
├── UiActionMap.Schemes.cs             # phase 1 (new): the table, AddScheme, SchemeName. phase 4: SetScheme
├── UiActionMap.cs                     # phase 1: the ctor seed, ResetFrame, the class remarks. phase 3: OnSchemeChanged
├── UiActionMap.Bindings.cs            # phase 2: the Scheme field, the nine overloads, Find and FindValue with the scheme
└── UiActionMap.Read.cs                # phase 1: the device track. phase 3: the change call. phase 4: the lock filter
samples/HelloPanel/
├── HelloScreen.cs                     # phase 1: the sixth label, SchemeText, LongestSchemeText
├── HelloModes.cs                      # phase 1: SchemeLabel
├── Program.cs                         # phase 1: schemeScratch, the label write
└── HelloState.cs                      # phase 2: the two scheme ids, the six tagged binds
tests/BrutalUi.Framework.Tests/Actions/
├── ActiveSchemeTests.cs               # phase 1 (new): 9 tests
├── SchemeGroupTests.cs                # phase 2 (new): 8 tests
├── SchemeChangedSlotTests.cs          # phase 3 (new): 6 tests
├── SchemeLockTests.cs                 # phase 4 (new): 7 tests
└── ActionMapAllocationTests.cs        # phase 5: the scene, SchemeStep, one new test, TheMeasuredBodyDoesRealWork
docs/technical-design.md               # phase 5: section 9 item 14, section 14
CHANGELOG.md                           # phase 5
docs/features/input/control-schemes/context.md   # each phase
```

These files do not change: every file of `src/BrutalUi.Core/`, every file of `src/BrutalUi.Glfw/`, `src/BrutalUi.Framework/Panels/UiManager.cs`, `UiActions.cs`, `UiActionEnums.cs`, `UiContext.cs`, and `UiInputReadHandler.cs` (A1). The eight existing classes under `tests/BrutalUi.Framework.Tests/Actions/` change none but `ActionMapAllocationTests`. The Core test project does not change. No `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, or `scripts/` file changes.

### Public API (end state)

```csharp
namespace BrutalUi.Framework;

public static class UiScheme                                            // UiScheme.cs (new)
{
    public static ReadOnlySpan<byte> KeyboardMouse => "KeyboardMouse"u8;
    public static ReadOnlySpan<byte> Gamepad => "Gamepad"u8;
}

public delegate void UiSchemeChangedHandler(UiActionMap map, int scheme);  // UiSchemeChangedHandler.cs (new)

public sealed partial class UiActionMap
{
    // existing and unchanged signatures (eleven: two adds, nine binds)
    public void AddAction(ReadOnlySpan<byte> action, ReadOnlySpan<byte> command);
    public void AddAction(ReadOnlySpan<byte> action, UiActionType type);
    public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers = UiModifiers.None);
    public void BindMouseButton(ReadOnlySpan<byte> action, PointerButton button, UiModifiers modifiers = UiModifiers.None);
    public void BindGamepadButton(ReadOnlySpan<byte> action, UiGamepadButtons button);
    public void BindKeyVector(ReadOnlySpan<byte> action, int up, int left, int down, int right);
    public void BindKeyAxis(ReadOnlySpan<byte> action, int negative, int positive);
    public void BindGamepadStick(ReadOnlySpan<byte> action, UiGamepadStick stick);
    public void BindGamepadAxis(ReadOnlySpan<byte> action, UiGamepadAxis axis, float scale = 1f);
    public void BindMouseDelta(ReadOnlySpan<byte> action, float scale = 1f);
    public void BindScroll(ReadOnlySpan<byte> action, float scale = 1f);
    // WasPressed, IsHeld, WasReleased, ReadAxis, ReadVector, PushGamepad, InvokeAction,
    // PressKey, PressMouseButton, Activate, IsActive, Deactivate, GamepadSlot, Commands,
    // ActionCount, BindingCount, CreateInfo: unchanged signatures

    // new: the group (phase 1, 2)
    public int AddScheme(ReadOnlySpan<byte> name);                                    // interns, returns one id
    public ReadOnlySpan<byte> SchemeName(int scheme);                                 // id 0 gives an empty span
    public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers, int scheme);
    public void BindMouseButton(ReadOnlySpan<byte> action, PointerButton button, UiModifiers modifiers, int scheme);
    public void BindGamepadButton(ReadOnlySpan<byte> action, UiGamepadButtons button, int scheme);
    public void BindKeyVector(ReadOnlySpan<byte> action, int up, int left, int down, int right, int scheme);
    public void BindKeyAxis(ReadOnlySpan<byte> action, int negative, int positive, int scheme);
    public void BindGamepadStick(ReadOnlySpan<byte> action, UiGamepadStick stick, int scheme);
    public void BindGamepadAxis(ReadOnlySpan<byte> action, UiGamepadAxis axis, float scale, int scheme);
    public void BindMouseDelta(ReadOnlySpan<byte> action, float scale, int scheme);
    public void BindScroll(ReadOnlySpan<byte> action, float scale, int scheme);

    // new: the active device, the slot, and the lock (phase 1, 3, 4)
    public int ActiveScheme { get; }
    public int LockedScheme { get; }                                                  // 0 when not locked
    public UiSchemeChangedHandler? OnSchemeChanged { get; set; }                       // write with =, never +=
    public void SetScheme(int scheme);                                                // 0 clears the lock
}
```

No public member changes the shape of `InputKind`, `UiActionType`, `UiGamepadStick`, or the five queries. There is no scheme list read, no binding handle, and no id type (A4).

### The Scheme Table and the Ids

```
id 0          the default scheme. A binding with it belongs to each scheme. Not in the table.
id 1, 2       the standard names KeyboardMouse and Gamepad, seeded by the constructor.
id 3 and up   a name that the game added.
```

- `AddScheme` scans the table with `SequenceEqual`, in the style of `IndexOfAction` (`UiActionMap.Bindings.cs:704`). A match gives the id, so the call interns. No match appends a copy with `ToArray()` and gives the new id. The array starts at `InitialCapacity` 8 and doubles, like the other two arrays (`UiActionMap.Bindings.cs:833-842`).
- The two standard ids are an internal invariant of the seed order. A game asks for an id with `AddScheme(UiScheme.KeyboardMouse)` and never hard-codes a number. `SchemeName` resolves any id, so `ActiveScheme` reaches its bytes through one call.
- `SchemeName(0)` gives an empty span: the default scheme has no name, and it means "each scheme". A negative id and an id at or above the count throw `ArgumentOutOfRangeException` with the parameter name `scheme`.
- An empty name in `AddScheme` throws `ArgumentException` with the parameter name `name`. The parameter name sits in one private constant, in the style of `ActionParameter` (`UiActionMap.cs:155`).

### The Device Rule (A2, Q1)

`device` holds the id of the scheme that the map reports. `pad` is `input.Gamepad(GamepadSlot)`. A kind below "gave a value" when its read of the frame is not nothing.

| Kind | Gave a value when | Device |
|---|---|---|
| `Key` | the modifiers match and (`Keys.IsDown(code)` or `Keys.WasPressed(code)`) | KeyboardMouse |
| `MouseButton` | the modifiers match and (`Buttons.IsDown` or `Buttons.WasPressed`) | KeyboardMouse |
| `KeyVector` | the four-key sum is not (0, 0) | KeyboardMouse |
| `KeyAxis` | the two-key difference is not 0 | KeyboardMouse |
| `MouseDelta` | `Pointer.Delta` is not (0, 0) | KeyboardMouse |
| `Scroll` | `Pointer.ScrollDelta.Y` is not 0 | KeyboardMouse |
| `GamepadButton` | the bit is set in `pad.Buttons` or in `pad.Pressed` | Gamepad |
| `GamepadStick` | the stick is not (0, 0) | Gamepad |
| `GamepadAxis` | `pad.Axis(code)` is not exactly 0 | Gamepad |

The rule reads the stored value, because the poll baked the deadzone in (A2, `src/BrutalUi.Glfw/GlfwUiInput.cs:103`). The map adds no deadzone. The tag of a binding never sets the device, so a gamepad bind tagged with a custom name still reports `Gamepad` (Q1). A device write costs one int store in the case that already ran the read.

Worked values for the tests. W on a WASD composite gives (0, 1), so KeyboardMouse. W and S together give (0, 0), so no device write. A stick at (0.4, 0) gives Gamepad, and a stick at (0, 0) gives nothing. The left trigger at exactly 0 gives nothing; at 0.3 it gives Gamepad. A pointer move of (40, -30) gives KeyboardMouse, and a gated move of (0, 0) gives nothing. Escape down with pad B pressed, bound in that order, gives Gamepad, because the pad bind is the later one. A held W re-asserts KeyboardMouse in each frame, so W held with the stick at rest gives KeyboardMouse.

### The Lock Filter (Q2)

```
SchemeAllows(binding)  =>  _lockedScheme == 0 || binding.Scheme == 0 || binding.Scheme == _lockedScheme
```

- Unlocked (`_lockedScheme == 0`), every binding reads, whatever its tag. The lock is the only filter. This keeps the auto-switch alive: a bind tagged `Gamepad` must still read while the map reports KeyboardMouse, or it could never set Gamepad. It also keeps the requirement's words, which put the "gives no value" rule on `SetScheme` only.
- The filter applies to the binding loop, the gamepad invoke pass of `ReadFrame`, `PushGamepad` (the gamepad invoke path of an inactive map), and the press path through `Find` in `PressKey` and `PressMouseButton`. `InvokeAction` names an action, not an input, so the lock does not touch it.
- The conflict rule is different, and has its own helper:

```
SchemeClashes(existingScheme, newScheme)  =>  newScheme == 0 || existingScheme == 0 || newScheme == existingScheme
```

The default scheme belongs to each scheme, so a default bind clashes with a tagged bind of the same input, in either order. Two tags that differ do not clash, so one key can bind two actions in two schemes.

### The Frame Read, With the Scheme Pass

```
ReadFrame(UiContext context)
  unchanged test: context.OnUnhandledInput != _onInput => return
  locals: input, pad, mods, pressed, actions, actionCount, bindings, bindingCount     (the re-entry rule)
  int kb = _keyboardScheme;  int gp = _gamepadScheme;                                  NEW (fields, no lookup)
  int device = _activeScheme;                                                          NEW: carries across frames
  clear the scratch of each action
  for each binding, in the order of the bind:
    NEW: if (!SchemeAllows(binding)) continue;
    switch (kind):  the nine cases of input-actions, each with NEW: device = kb or gp when it gave a value
  NEW: _activeScheme = device;
  finish each action
  invoke pass: same loop, NEW: the SchemeAllows test beside the kind test
  NEW: int reported = _lockedScheme == 0 ? _activeScheme : _lockedScheme;
  NEW: if (reported != _reportedScheme) { _reportedScheme = reported; OnSchemeChanged?.Invoke(this, reported); }
```

The call sits at the end of the read, so the handler sees the finished values, the invoked commands, and the new `ActiveScheme`. The handler runs inside `UiContext.Update`, so the re-entry rule of input-actions applies: the read copied the arrays and the counts to locals, so a bind or a `SetScheme` in the handler takes effect at the next frame (A3). A handler that calls `Update` throws `InvalidOperationException` in Core, as it does for `OnInputRead`. The map calls the slot one time per `Update` at most.

### Internal Shape

- **`InputBinding` (private, `UiActionMap.cs:640`).** One new field, `int Scheme`, after `Modifiers`. `Scheme` holds 0 for a bind of the eleven current signatures. Each bind body writes it. The struct stays a plain value type, and one int keeps it aligned for the frame read.
- **The scheme fields.** `_schemes` (`byte[][]`), `_schemeCount`, `_keyboardScheme`, `_gamepadScheme`, `_activeScheme`, `_lockedScheme`, and `_reportedScheme`, all in `UiActionMap.Schemes.cs`. `SeedSchemes()` fills the two standard names and sets `_activeScheme` and `_reportedScheme` to `_keyboardScheme` (Q3). The constructor calls it after `_onRead = ReadFrame;` (`UiActionMap.cs:207`).
- **`ResetFrame()` (`UiActionMap.cs:536`).** It also sets `_activeScheme` and `_reportedScheme` to `_keyboardScheme`. `Activate` and `Deactivate` call it as they do today, so Q3 holds after `Activate` with no new call site. The method leaves `_lockedScheme` alone: the lock is a build setting of the map, not a frame value.
- **The binds (phase 2).** One body per kind, in `UiActionMap.Bindings.cs`. The scheme overload holds the body and takes a required `int scheme` last. The current signature becomes a one-line forward with `scheme: 0`. No new overload takes an optional parameter, so every call resolves by arity and no existing call site changes. `BindKeyVector` and `BindKeyAxis` keep their four- and three-code stores.
- **The check order.** Each bind checks: its input arguments, the action name, the action type, the scheme id, then a duplicate. The scheme check throws `ArgumentOutOfRangeException` with the parameter name `scheme`, and a rejected call changes no count.
- **`Find` and `FindValue`.** Each gains one `int scheme` parameter and tests `SchemeClashes` on the same kind, code, and modifier rules as today. The composite helpers `SamePair`, `SameSet`, and `HasCode` (`UiActionMap.Bindings.cs:779-813`) keep their set compare, so the duplicate rule stays set-based inside one scheme.
- **The slot.** `public UiSchemeChangedHandler? OnSchemeChanged { get; set; }`, one auto-property written with `=` and never `+=`, in the pattern of `UiContext.OnInputRead` and `CommandRegistry.UnknownCommand`. The map holds no list of handlers, so no call combines delegates.
- **The class remarks.** Three new paragraphs in `UiActionMap.cs`: the group rule (the table, the ids, the default scheme, the per-scheme duplicate), the active rule (the device table, the carry across frames, the seed of Q3), and the lock rule (the three paths, `PushGamepad`, and the report of the locked scheme).

### Integration Points

| Sibling or caller | What this feature calls or gives | Contract |
|---|---|---|
| input-actions (done) | `ReadFrame`, the nine kinds, the slot pair, the switch rule | This feature adds work inside the four existing passes and changes no signature. `IsActive` still needs both slots. `ActionMapSwitchTests` (6) passes with no edit. |
| input-state (done) | `context.GameInput`, `Keys`, `Buttons`, `Pointer` | Reads only. The gate empties a key or a pointer value, so a gated read gives no value and sets no device. No Core file changes (A1). |
| gamepad-axes (done) | `pad.Buttons`, `pad.Pressed`, `LeftStick`, `RightStick`, `Axis(int)` | The stored value is after the deadzone, so "not exactly 0" is the above-deadzone test (A2). The map adds no deadzone. |
| state-and-commands (`OnUnhandledInput`, `PushGamepad`, done) | the press path and the manual pad path | Both gain the lock filter. The repeat rule, the text guard, and the body of `PushGamepad` for an unlocked map do not change. |
| panels-and-modes/mode-set (done) | `Activate` and `Deactivate` only | `UiManager.cs` does not change. `Activate` resets the device and the reported scheme through `ResetFrame`; it keeps the lock. |
| binding-remap (next) | `AddScheme`, `SchemeName`, `InputBinding.Scheme`, `SchemeClashes`, `LockedScheme` | One int per binding that resolves back to UTF-8 bytes; id 0 means the default scheme, and `SchemeName(0)` is empty. The conflict report is per scheme. The binding handle and `FindBindingIndex` stay in binding-remap (A4, CR-5). |
| HelloPanel (sample) | `AddScheme`, `SchemeName`, `ActiveScheme` | The label write sits inside the UI region, so the sample gate of 0 bytes covers the report and the name read. The sample owns its display text (Out of Scope: no glyph table). |
| spacegame map-controls (outside this repo) | the eleven current signatures | spacegame moved its submodule to input-actions. No signature and no query changes shape, so its binds keep compiling and keep landing in the default scheme. |

---

## Implementation Phases

The feature has 34 tasks, so it has five slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea, and it is the demo that the confirmed Q4 names: the scheme table, the active device, and the HelloPanel label. The tag on a binding is not needed for that demo, because the device comes from the kind (Q1), so it lands in phase 2. The lock lands in phase 4, because it is the only work that touches all three frame paths at once. Phase 5 grows the gate and writes the docs.

Each verification command sets `MSBUILDDISABLENODEREUSE=1`. Give `-nodeReuse:false` to `dotnet build` only. On `dotnet test` it breaks the host handshake (Gotcha 1 of input-state). Do not run `dotnet build-server shutdown`. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `BrutalUi.Core` where a cref names a Core type). Run one Framework class with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5` after a build.

Two flakes are known. A test host can exit with 134 (Internal CLR error 0x80131506), and the Core test `TheCurvesAllocateNothing` failed one time with 8128 bytes. Run the tests a second time before you treat either as a fault. A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant. For each plant, copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored, Gotcha 3 of input-state), run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`, expect a value that is not 0, read the failed test names from the log, restore the file with `cp`, check it with `cmp`, and run the tests again. A plant line `GC.KeepAlive(new object());` needs `using System;` in its file. Keep the plant text word for word (Decision 27 of input-state).

The sandbox blocks the X11 socket of `scripts/test-linux.sh` (Gotcha 3 of input-state), and the three display-host checks of input-actions are still open on this branch. The phase 1 demo needs a live window. Run the smoke step and the label check on a host with a display, or under `xvfb-run -a` in a normal shell. When the sandbox blocks it, record the exact smoke line and hand the run to Needs Human Verification in `context.md`. Each phase pins the same behavior in a headless test, so the gate does not depend on a window.

The tests follow the style of `ActionQueryTests`: a context with a `RecordingRenderer` measurer and a viewport of 400 by 300, a root that is not pickable for the pointer tests, a pad built by hand with `context.SetGamepad(0, new UiGamepadState(buttons, stackalloc float[] { ... }))` before `Update` (Framework references no Glfw, A2), events built by hand because the Core factories are internal, and one static handler field per gate. xUnit runs test classes in parallel, so each class keeps its own static counter field. A handler slot takes no target, so a slot test uses a `static readonly` handler over a private static int, in the pattern of `InputReadTests.s_gateHandler`. GLFW codes: W 87, A 65, S 83, D 68, Space 32, Escape 256, F1 290, F2 291, F3 292.

### Phase 1: The Scheme Table, the Active Device, and the HelloPanel Label
**Goal:** `UiScheme` names the two standard schemes. `UiActionMap` holds the scheme table with `AddScheme` and `SchemeName`, reports `ActiveScheme` from the device of the last binding that gave a value, and starts at KeyboardMouse at construction and after `Activate`. HelloPanel shows the active scheme in a sixth label.
**Demo:** a developer runs HelloPanel on a host with a window, reads `Scheme: Keyboard / Mouse`, pushes the left stick or presses B, and reads `Scheme: Gamepad`. On this host, where the sandbox blocks the window, the same check runs headless: `ActiveSchemeTests` reads a total of 9 with `failed: 0`.
**Needs:** none
**Agent tier:** STANDARD (sonnet). The device rule is one int store per case, and this plan writes each case out. No signature changes, and Core does not change.

**Tasks (group 1, the Framework):**
- [ ] 1.1 Create `docs/features/input/control-schemes/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`), the solution total with the skipped count, and the totals of the two test projects. At planning time they read 2921 with 3 skipped (the three skipped tests sit in the Core and Glfw display checks), `BrutalUi.Core.Tests` 1801, and `BrutalUi.Framework.Tests` 259. The planner measured the Framework binary in place: 259, 0 failed, 0 skipped, exit 0, 615 ms. Record the Scheme Table, the Device Rule, the Lock Filter, and the Internal Shape of this plan.
- [ ] 1.2 Write `src/BrutalUi.Framework/Actions/UiScheme.cs` with `public static class UiScheme` and the two `static ReadOnlySpan<byte>` properties `KeyboardMouse` (`"KeyboardMouse"u8`) and `Gamepad` (`"Gamepad"u8`), in the shape of `UiActions.cs:20-32`. The summary: the names of the standard control schemes of a `UiActionMap`. The remarks: a name is UTF-8 and the map compares two names by their bytes, so `Gamepad` and `gamepad` are two names; the map holds both names in its table from construction, so `AddScheme` gives their id with no add; a game can add other names; a custom name groups bindings only, because `ActiveScheme` reports the device of the input (Q1); each property gives a span of constant data, so a read allocates nothing. File-scoped namespace, `using System;`, one doc comment per member.
- [ ] 1.3 Write `src/BrutalUi.Framework/Actions/UiActionMap.Schemes.cs` as `public sealed partial class UiActionMap` (file-scoped namespace `BrutalUi.Framework`, `using System;`). Add the private constant `SchemeParameter = "scheme"` and the private fields `_schemes` (`byte[][]`, `[]`), `_schemeCount`, `_keyboardScheme`, `_gamepadScheme`, `_activeScheme`, `_lockedScheme`, and `_reportedScheme`. Add `private void SeedSchemes()`: it appends `UiScheme.KeyboardMouse` and `UiScheme.Gamepad` with `ToArray()`, keeps their ids in `_keyboardScheme` and `_gamepadScheme`, and sets `_activeScheme` and `_reportedScheme` to `_keyboardScheme` (Q3). Add `public int AddScheme(ReadOnlySpan<byte> name)`: an empty span throws `ArgumentException` with the parameter name `name`; a scan with `SequenceEqual` returns the id of a name the table holds, so the call interns; else it grows the array (empty gives `InitialCapacity`, else double) and returns the new id. Remarks: the build-time rule (the method copies the name and can grow an array), the intern rule, and the two standard names. Add `public ReadOnlySpan<byte> SchemeName(int scheme)`: id 0 gives an empty span (the default scheme has no name and means each scheme); a negative id and an id at or above `_schemeCount` throw `ArgumentOutOfRangeException` with the parameter name `scheme`; the read copies no array and allocates nothing. Add `private int IndexOfScheme(ReadOnlySpan<byte> name)` for the scan. Add `public int ActiveScheme => _lockedScheme == 0 ? _activeScheme : _lockedScheme;` with remarks for the device rule (the table of "The Device Rule"), the carry across frames, the seed of Q3, the locked report of Q2, and the read of `SchemeName`. The `_lockedScheme` field stays 0 until phase 4, so the getter reads `_activeScheme` in this phase. Add the class-level file remarks at the top of the file in the style of the other partial files.
- [ ] 1.4 Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`. Call `SeedSchemes()` in the constructor after `_onRead = ReadFrame;` (`:207`). In `ResetFrame()` (`:536`), add `_activeScheme = _keyboardScheme;` and `_reportedScheme = _keyboardScheme;`, and state in its remarks that the method resets the scheme report too and leaves `_lockedScheme` alone. Add three paragraphs to the class remarks: the group rule (a scheme is one int id that resolves to a UTF-8 name; the table holds `KeyboardMouse` and `Gamepad` from construction; a binding with the default id belongs to each scheme; `UiScheme` names the standard names), the active rule (`ActiveScheme` reports the device of the last binding that gave a value; a key, a mouse button, a key composite, the pointer delta, and a scroll give KeyboardMouse; a stick, an axis, and a gamepad button give Gamepad; a stored value of exactly 0 gives no value; the report carries from frame to frame; it starts at KeyboardMouse at construction and after `Activate`), and the lock rule, written in phase 4. Update the type cref list in the remarks so `UiScheme` and `SchemeName` appear. Change no other member.
- [ ] 1.5 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs`. In `ReadFrame` (`:210`), after the existing locals, add `int kb = _keyboardScheme;`, `int gp = _gamepadScheme;`, and `int device = _activeScheme;`. In each of the nine cases, write the device when the case gave a value, by the table of "The Device Rule": the `Key` and `MouseButton` cases write `kb` inside the existing modifier test when `a.Down` or `a.Press` is set; the `KeyVector` case writes `kb` when `x != 0f || y != 0f`; the `KeyAxis` case writes `kb` when `value != 0f`; the `GamepadButton` case writes `gp` when the bit is set in `pad.Buttons` or in `pressed`; the `GamepadStick` case writes `gp` when the stick is not (0, 0); the `GamepadAxis` case writes `gp` when `pad.Axis(binding.Code) != 0f`; the `MouseDelta` case writes `kb` when `delta` is not (0, 0); and the `Scroll` case writes `kb` when `value != 0f`. After the loop, before the finish pass, add `_activeScheme = device;`. Update the `ReadFrame` remarks with one paragraph for the device track: the rule, the carry across frames (a frame with no input keeps the last report), the exact-zero test of A2, and the words that the tag of a binding never sets the device (Q1). Add no allocation, and change no other pass yet.
- [ ] 1.6 Write `tests/BrutalUi.Framework.Tests/Actions/ActiveSchemeTests.cs` with 9 tests. `ANewMapAndAFreshActivateReportKeyboardMouse` (a map reports `AddScheme(UiScheme.KeyboardMouse)`; `Deactivate`, then `Activate`, then `Update` with a pad at rest still reports KeyboardMouse). `AKeyBindingSetsTheKeyboardScheme`: Escape down with `Cancel` bound invokes the command and `ActiveScheme` reads the KeyboardMouse id. `APassivePointerReadsSetTheKeyboardScheme` (a pointer move of (40, -30) outside the panel with `Look` bound sets KeyboardMouse; a scroll of (0, 3) with `Zoom` bound sets KeyboardMouse). `AStickAboveRestSetsTheGamepadScheme` (`Move` with the WASD composite and the left stick: the stick axes (0.4, -0.2) give Gamepad; the stick (0, 0) after it, with no key, keeps Gamepad). `AGamepadButtonSetsTheGamepadScheme` (pad A down with `Fire` bound gives Gamepad; the left trigger 0.3 gives Gamepad; the left trigger exactly 0 gives no change). `TheLastBindingThatGaveAValueWins` (two maps: bind Escape to `Cancel` then B to `Cancel`, and press both in one frame, gives Gamepad; the reverse bind order gives KeyboardMouse). `ADeadValueSetsNoScheme` (W and S together on `Move` give (0, 0) and no change; a gated key with a focused `Button` gives no change; a pointer move over a pickable root gives no change; each starts from a KeyboardMouse report). `AHeldInputKeepsItsSchemeEachFrame` (W held and the stick at rest gives KeyboardMouse in each of three `Update` calls; the stick held at (0.5, 0) with no key gives Gamepad; W held with the stick at (0.5, 0) gives the report of the later binding in bind order). `TheReportCarriesWithNoInput` (a stick move sets Gamepad; two frames with no `SetGamepad` edge and no key keep Gamepad). Use one static readonly handler only where a slot needs one; this class needs none.
- [ ] 1.7 Run the build and the tests. Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*ActiveSchemeTests' 2>&1 | tail -n 5` and expect a total of 9 with `failed: 0`. Check that `UiActionMapTests` (23), `GamepadActionTests` (7), `FocusActionTests` (5), `TypedActionMapTests` (9), `GamepadValueActionTests` (8), `ActionQueryTests` (10), `ActionMapSwitchTests` (6), `ActionMapAllocationTests` (6), and `ModeActionMapTests` (5) pass with no edit, and that the Framework total reads 268. Run three plants. Plant 1: the `GamepadStick` case writes `kb` in place of `gp`. Expect `AStickAboveRestSetsTheGamepadScheme` to fail. Plant 2: `int device = _keyboardScheme;` in place of `int device = _activeScheme;`. Expect `TheReportCarriesWithNoInput` and `AHeldInputKeepsItsSchemeEachFrame` to fail. Plant 3: the `KeyVector` case writes `kb` whatever the sum. Expect `ADeadValueSetsNoScheme` to fail. Record the counts, the demo state, and the failed tests in `context.md`.

**Tasks (group 2, the sample):**
- [ ] 1.8 Edit `samples/HelloPanel/HelloScreen.cs`. `BuildWidgetPanel` (`:269`) gets `out Label schemeLabel` after `moveLabel`. The label is not pickable, comes right after the move label, and starts with `LongestSchemeText`. Add `public static ReadOnlySpan<byte> SchemeText(ReadOnlySpan<byte> scheme, Span<byte> scratch)`: it copies `SchemePrefix` (`"Scheme: "u8`), then copies `"Keyboard / Mouse"u8` when `scheme.SequenceEqual(UiScheme.KeyboardMouse)`, `"Gamepad"u8` when it equals `UiScheme.Gamepad`, else `scheme` itself, and returns the written part. Add the three private spans, and `LongestSchemeText => "Scheme: Keyboard / Mouse"u8` (24 bytes). Remarks in the style of `MoveText` (`:660-725`): the sample owns the display text, because a scheme to art map is the game's (Out of Scope of the requirements); the longest text needs 24 bytes, so a scratch of 32 bytes holds each standard name; a custom name above 24 bytes does not fit, and the method throws `InvalidOperationException` like the other formatters; read the name with `UiActionMap.SchemeName` of `UiActionMap.ActiveScheme` after `UiContext.Update(float)`. Update the class remarks to six labels, and the `BuildWidgetPanel` doc comments.
- [ ] 1.9 Edit `samples/HelloPanel/HelloModes.cs`. Add the field `_schemeLabel` after `_moveLabel` (`:112`), the `SchemeLabel` property after `MoveLabel` (`:224`), the constructor check after the move check (`:181`), the `out _schemeLabel` argument in `CreateWidgetPanel` (`:243`), and the label list in the constructor remarks (`:132-134`).
- [ ] 1.10 Edit `samples/HelloPanel/Program.cs`. Add the parameter `Label schemeLabel` to `RunFrames` after `moveLabel` (`:785`), pass `modes.SchemeLabel` at the call site, create `byte[] schemeScratch = new byte[32];` next to `moveScratch` (`:819`) with the same style of comment (24 bytes needed), and add `schemeLabel.SetText(HelloScreen.SchemeText(state.Map.SchemeName(state.Map.ActiveScheme), schemeScratch));` right after the move label line (`:912`), inside the UI region. Update the remarks and the parameter docs of `RunFrames`.
- [ ] 1.11 Run the build and the tests. Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run plant 4: `GC.KeepAlive(new object());` as the first line of `HelloScreen.SchemeText`. Check `0 Error(s)`. On a host with a window, expect a non-zero byte count on smoke line 3 and exit code 1. Restore with `cmp`. On a host with a window, run the demo: `dotnet run --project samples/HelloPanel/HelloPanel.csproj`, read `Scheme: Keyboard / Mouse`, press W and read the same, push the left stick and read `Scheme: Gamepad`, press B and read the same, release the stick and read the same, then hold W and read `Scheme: Keyboard / Mouse`. When the sandbox blocks the smoke step, record the exact line and hand the PASS run, plant 4, and the demo to Needs Human Verification in `context.md`.

**Estimated Steps:** about 78 (11 tasks, about seven steps for one task)

**Files (group 1, 6):** `context.md`, `UiScheme.cs`, `UiActionMap.Schemes.cs`, `UiActionMap.cs`, `UiActionMap.Read.cs`, `ActiveSchemeTests.cs`
**Files (group 2, 4):** `HelloScreen.cs`, `HelloModes.cs`, `Program.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `ActiveSchemeTests` pass. Each existing test passes with no edit. The Framework total reads 268 (259 + 9), and the solution reads 2930 with 3 skipped.
- [ ] Plants 1 to 4 give a non-zero exit code, and each restored file equals its copy under `cmp`.
- [ ] `git diff` of `src/BrutalUi.Core/` and `src/BrutalUi.Glfw/` shows no output (A1).
- [ ] HelloPanel shows the sixth label, or `context.md` records the smoke blocker and hands the live check to a person.

### Phase 2: The Scheme Tag, the Nine Overloads, and the Per-Scheme Duplicate Rule
**Goal:** Each binding holds one scheme id. Each of the nine public binds gains one scheme overload with a trailing required scheme id, and the eleven current signatures forward with id 0. `Find` and `FindValue` take the scheme, so the duplicate rule is per scheme and the same input can bind in two schemes. HelloPanel tags its six standard binds with the two scheme ids.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*SchemeGroupTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus, at the preset effort). This is the widening that CR-4 names: nine bodies, two duplicate finds, eleven signatures that spacegame and the shipped tests already call, and the check order of the throws. A wrong optional parameter or an ambiguous overload breaks a game at compile time.

**Tasks (group 1, the map):**
- [ ] 2.1 Edit `src/BrutalUi.Framework/Actions/UiActionMap.cs`. Add `public int Scheme;` to `InputBinding` after `Modifiers` (`:671`), with a doc comment: the scheme id of the bind, 0 for the default scheme that belongs to each scheme. Each bind body writes it, and the eleven current signatures write 0. Edit `src/BrutalUi.Framework/Actions/UiActionMap.Schemes.cs`: add `private static bool SchemeClashes(int existingScheme, int newScheme)` with the rule of "The Lock Filter" and its doc comment, and add `private bool SchemeAllows(in InputBinding binding)` with the allows rule and its doc comment (phase 4 calls it).
- [ ] 2.2 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs`. Give `Find` (`:684`) a fourth parameter `int scheme`, and add the test `SchemeClashes(binding.Scheme, scheme)` beside the kind, code, and modifier tests. Rewrite `BindKey` (`:121`) as `BindKey(action, key, UiModifiers modifiers, int scheme)` with the body and `Scheme = scheme` in the `Append`, and add `public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers = UiModifiers.None) => BindKey(action, key, modifiers, 0);`. Do the same for `BindMouseButton` (`:174`) and `BindGamepadButton` (`:222`). Add the scheme id check in each body, after the type check and before the duplicate check: `(uint)scheme >= (uint)_schemeCount` throws `ArgumentOutOfRangeException` with the parameter name `scheme`. Move the doc comment of each signature so the remarks state the scheme rule (a bind of the default scheme belongs to each scheme, and the duplicate check is per scheme), and keep every other sentence.
- [ ] 2.3 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs`. Give `FindValue` (`:730`) a sixth parameter `int scheme` with the same `SchemeClashes` test, and keep `SamePair`, `SameSet`, and `HasCode` (`:779-813`) as they are. Rewrite `BindKeyVector` (`:289`), `BindKeyAxis` (`:513`), `BindGamepadStick` (`:380`), and `BindGamepadAxis` (`:445`) as the scheme bodies with the required `int scheme` last, and add the four current signatures as one-line forwards with `scheme: 0`. `BindGamepadAxis` keeps its `scale = 1f` default only on the current signature, so the new one reads `(action, axis, scale, scheme)`. Add the scheme id check to each body.
- [ ] 2.4 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs`. Do the same for `BindMouseDelta` (`:586`) and `BindScroll` (`:647`). Update the class remarks in `UiActionMap.cs` for the build rules: add the scheme id to the check order (the input arguments, the action name, the type, the scheme id, then a duplicate), state that the duplicate check is per scheme with the clash rule of the default scheme, and state that two tags that differ are not a duplicate. Change no line of `AddAction` in either overload (A1 of the notes: a scheme groups bindings, not actions).
- [ ] 2.5 Write `tests/BrutalUi.Framework.Tests/Actions/SchemeGroupTests.cs` with 8 tests. `AddSchemeGivesOneIdPerName` (two calls with `UiScheme.KeyboardMouse` give one id; `AddScheme("Gamepad"u8)` after `AddScheme(UiScheme.Gamepad)` gives the same id; a name of `"gamepad"u8` gives a third id; `SchemeName` of each id gives the bytes back). `TheStandardNamesStartInTheTable` (a new map resolves both standard names with no add; a custom name appends after them; `SchemeName(0)` is empty). `SchemeNameChecksTheId` (-1 and an id at or above the count throw `ArgumentOutOfRangeException` with `ParamName` `scheme`; a valid id never throws). `AddSchemeRejectsAnEmptyName` (an empty span throws `ArgumentException` with `ParamName` `name`; `ActionCount` and `BindingCount` do not change). `ASecondBindOfTheSameKeyInTwoSchemesBinds` (`BindKey(Fire, 32, None, kb)` then `BindKey(Jump, 32, None, gp)` binds, `BindingCount` rises by 1, and the same key twice in `kb` throws `ArgumentException` with `ParamName` `action`). `TheDefaultSchemeClashesWithEveryScheme` (a default `BindKey(Fire, 32)` then `BindKey(Jump, 32, None, gp)` throws, and the reverse order throws too; each rejected call changes no count). `ACompositeDuplicateIsPerScheme` (`BindKeyVector(Move, 87, 65, 83, 68, kb)` then the same four keys as W, D, S, A in `kb` throws, and the same four in `gp` binds; `BindKeyAxis(Throttle, 83, 87, kb)` then (87, 83) in `kb` throws, and in `gp` binds; `BindGamepadStick(Move, Left, kb)` then `(Move, Left, gp)` binds). `TheOldSignaturesBindIntoTheDefaultScheme` (each of the eleven current signatures compiles and lands in the default scheme: after `BindKey(Fire, 32)` a tagged `BindKey(Jump, 32, None, gp)` throws, and after a tagged `BindGamepadButton(Fire, A, gp)` a plain `BindGamepadButton(Jump, A)` throws; a scheme id of -1 or 99 on `BindKey`, `BindScroll`, and `BindKeyVector` throws `ArgumentOutOfRangeException` with `ParamName` `scheme`; a scheme bind of the wrong action type still throws with `ParamName` `action`; each call above changes no count).
- [ ] 2.6 Run the build, the tests, and `SchemeGroupTests` (total 8). Check that the 268 tests of phase 1 and the eight existing Actions classes pass with no edit. Run three plants. Plant 5: `Find` drops the scheme test. Expect `ASecondBindOfTheSameKeyInTwoSchemesBinds` to fail. Plant 6: `SchemeClashes` returns `existingScheme == newScheme`. Expect `TheDefaultSchemeClashesWithEveryScheme` to fail. Plant 7: `FindValue` keeps the map-wide compare for a `KeyVector`. Expect `ACompositeDuplicateIsPerScheme` to fail. Record the counts and the failed tests in `context.md`.

**Tasks (group 2, the sample):**
- [ ] 2.7 Edit `samples/HelloPanel/HelloState.cs`. Add the two private int fields `_keyboardScheme` and `_gamepadScheme`, set from `Map.AddScheme(UiScheme.KeyboardMouse)` and `Map.AddScheme(UiScheme.Gamepad)` before the first add, and pass them as the trailing scheme of the six binds at `:90-98`: the three key binds (Escape, Up, Down) with `_keyboardScheme`, the three gamepad button binds (B, DpadUp, DpadDown) with `_gamepadScheme`, `BindKeyVector(MoveAction, W, A, S, D, _keyboardScheme)`, and `BindGamepadStick(MoveAction, Left, _gamepadScheme)`. The F1 and F2 binds of `HelloModes.cs:150-151` stay untagged, so the sample also shows the default scheme. Update the remarks: an unlocked map reads each binding whatever its tag, so the sample behavior does not change (the group rule). Run the build and the tests, and check the phase 1 label demo still reads the same.

**Estimated Steps:** about 55 (7 tasks, about eight steps for one task)

**Files (group 1, 5):** `UiActionMap.cs`, `UiActionMap.Schemes.cs`, `UiActionMap.Bindings.cs`, `SchemeGroupTests.cs`, `context.md`
**Files (group 2, 2):** `HelloState.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `SchemeGroupTests` pass. Each existing test passes with no edit. The Framework total reads 276 (268 + 8), and the solution reads 2938 with 3 skipped.
- [ ] `git diff` of `UiActionMap.Bindings.cs` shows each of the eleven current signatures unchanged, each with one new forward.
- [ ] Plants 5 to 7 give a non-zero exit code, and each restored file equals its copy under `cmp`.

### Phase 3: The OnSchemeChanged Slot
**Goal:** `OnSchemeChanged` is one slot on the map. The map calls it in `Update`, at the end of the read, with the map and the new scheme, on a change only. `Activate` and `Deactivate` reset the reported scheme, so the first frame of a new map reports a change.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*SchemeChangedSlotTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet). One field, one compare, one call at the end of a pass that already exists, and this plan fixes the place.

**Tasks (one group):**
- [ ] 3.1 Write `src/BrutalUi.Framework/Actions/UiSchemeChangedHandler.cs` with `public delegate void UiSchemeChangedHandler(UiActionMap map, int scheme);`, in the doc shape of `src/BrutalUi.Core/Input/UiInputReadHandler.cs`. The summary: a handler that the map calls when the active scheme changes. The remarks: the slot rule (`UiActionMap.OnSchemeChanged`, written with `=`), the place rule (the map calls it in `UiContext.Update(float)`, at the end of the frame read, after the values and the gamepad invoke pass), the change rule (a call comes only when the reported scheme changed, and one call comes per `Update` at most, so two flips inside the read give one call with the last value), the re-entry rule (a handler is code inside the read, so a bind or a `SetScheme` in it takes effect at the next frame, and a call of `Update` throws `InvalidOperationException`), the exception rule (an exception of the handler leaves `Update` before the binding pass, and the map keeps the values of the frame), and the build-time rule (create the delegate one time; a new delegate in each frame allocates). Parameters: the map that reports the change, and the id of the new scheme, which the map reads back through `UiActionMap.SchemeName`.
- [ ] 3.2 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Schemes.cs`. Add `public UiSchemeChangedHandler? OnSchemeChanged { get; set; }` with remarks for the five rules of the delegate, and the words that the map holds one handler, not a list, so a write with `+=` is a mistake. Add the private helper `private void ReportSchemeChange()` that computes `int reported = _lockedScheme == 0 ? _activeScheme : _lockedScheme;`, and when `reported != _reportedScheme` sets `_reportedScheme = reported` and calls `OnSchemeChanged?.Invoke(this, reported)`. Add the doc: one compare and one call, no allocation. Do not touch `_lockedScheme` here beyond the read in the helper: `SetScheme` arrives in phase 4.
- [ ] 3.3 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs`. Call `ReportSchemeChange()` as the last statement of `ReadFrame`, after the invoke pass. Add the change paragraph to the `ReadFrame` remarks (the place after the finish and the invoke pass, the one call per change, the report of the locked id under a lock, and the fact that an early return of the pair test calls nothing). Add the slot rule and the exception rule to the class remarks of `UiActionMap.cs`.
- [ ] 3.4 Write `tests/BrutalUi.Framework.Tests/Actions/SchemeChangedSlotTests.cs` with 6 tests. `TheSlotRunsAfterTheReadInEachUpdate` (a handler records `map.ActiveScheme`, `ReadVector(Move)`, and `ReadAxis(Zoom)` inside the call: with the stick at (0.5, 0) and W down in one frame, the recorded value is the finished value of that frame, so the call runs after the finish). `TheSlotCallsOneTimePerChange` (a stick move gives one call with the Gamepad id; the same stick value in three more frames gives no further call). `TheSlotDoesNotCallWhenTheSchemeDoesNotChange` (a plain map with no handler, then a handler set to `null`, gives no call and no failure; a key-only frame gives no call after the seed). `TheSlotTakesOneHandlerWrittenWithEquals` (handler A then handler B: only B runs, and each call gets the same map instance and the new id; assert both parameters). `AHandlerChangeTakesEffectNextFrame` (the handler calls `SetScheme(gp)` on the first change, and records what it read: the frame that fired the call keeps the value that the unlocked read produced, and the next frame drops the keyboard value of a held W, so the lock applies at the next frame; the same shape for a bind added in the handler). `AFirstUpdateAfterActivateCallsWithTheLockedScheme` (a locked map: `Activate` resets the report, so the first `Update` calls one time with the locked id, and a second `Update` calls nothing; after `SetScheme(0)` the next change calls with the device again).
- [ ] 3.5 Run the build, the tests, and `SchemeChangedSlotTests` (total 6). Check that `ActionMapSwitchTests` (6) and `ModeActionMapTests` (5) pass with no edit, so the switch rule and the `IsActive` pair test still hold. Run two plants. Plant 8: `ReportSchemeChange` calls on every frame, whatever the report. Expect `TheSlotDoesNotCallWhenTheSchemeDoesNotChange` and `TheSlotCallsOneTimePerChange` to fail. Plant 9: the call runs before the binding loop in place of after the invoke pass. Expect `TheSlotRunsAfterTheReadInEachUpdate` to fail. Record the counts and the failed tests in `context.md`.

**Estimated Steps:** about 36 (5 tasks, about seven steps for one task)

**Files (one group, 5):** `UiSchemeChangedHandler.cs`, `UiActionMap.Schemes.cs`, `UiActionMap.Read.cs`, `SchemeChangedSlotTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `SchemeChangedSlotTests` pass. Each existing test passes with no edit. The Framework total reads 282 (276 + 6), and the solution reads 2944 with 3 skipped.
- [ ] Plants 8 and 9 give a non-zero exit code, and each restored file equals its copy under `cmp`.
- [ ] `grep -n 'OnSchemeChanged' src/BrutalUi.Framework/Actions/UiActionMap*.cs` shows the property, the one call, and the docs only. No file of `src/BrutalUi.Core/` changes (A1).

### Phase 4: The Lock on Every Path
**Goal:** `SetScheme` locks the map to one scheme. A binding of another scheme gives no value on the frame read, the key and mouse press path, and the gamepad command invoke, including `PushGamepad`. A binding of the default scheme reads under every lock. `ActiveScheme` reports the locked scheme while locked. `SetScheme(0)` clears the lock.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*SchemeLockTests'` and reads a total of 7 with `failed: 0`.
**Needs:** Phase 2, Phase 3
**Agent tier:** ADVANCED (opus, at the preset effort). Q2 asks for a filter on every path, and the paths sit in four methods across two passes plus the two public resolves and `PushGamepad`. A missed path lets a locked menu run a foreign bind, and a wrong filter on the press path breaks the shipped `UiActionMapTests`.

**Tasks (one group):**
- [ ] 4.1 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Schemes.cs`. Add `public void SetScheme(int scheme)`: a negative id and an id at or above `_schemeCount` throw `ArgumentOutOfRangeException` with the parameter name `scheme` and keep the old lock; `0` clears the lock; any other id stores it. Remarks: the filter rule (a binding of another scheme gives no value on the frame read, the press path, and the gamepad invoke, including `PushGamepad`), the default rule (a binding of the default scheme reads under every lock), the report rule (`ActiveScheme` reads the locked id while locked, and the map calls `OnSchemeChanged` at the next `Update` with it), the reset rule (`Activate` and `Deactivate` keep the lock, because it is a build setting of the map), and the cost rule (the method allocates nothing and takes effect at the next frame when a change handler calls it). Add `public int LockedScheme => _lockedScheme;` with a short doc.
- [ ] 4.2 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs`. In `ReadFrame`, add `if (!SchemeAllows(binding)) continue;` as the first test of the binding loop, and the same test as the first condition of the invoke pass (`:355-372`). In `PushGamepad` (`:29-51`), add `SchemeAllows(binding)` to the existing condition of the loop, and add one remark line: a locked map takes no binding of another scheme, and an unlocked map keeps the body of input-actions. Change no other line of `PushGamepad`.
- [ ] 4.3 Edit `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs` and `UiActionMap.Bindings.cs`. In `PressKey` (`:88`) and `PressMouseButton` (`:115`), pass `_lockedScheme` to `Find`, so the press path takes the lock. Keep both signatures, the raw resolve note, and the text guard of `HandleInput` (`:131-151`) unchanged. In `UiActionMap.Bindings.cs`, `Find` needs two different scheme tests, so give it one `bool lockMode` argument beside `scheme`: with `lockMode` `true` it applies `SchemeAllows` (the press path), and with `lockMode` `false` it applies `SchemeClashes` (the duplicate check). Each bind calls it with `false`, and the two press methods call it with `true`. Keep the kind, code, and modifier tests as they are. Update the remarks of `PressKey`, `PressMouseButton`, and `HandleInput` for the lock, and add the lock paragraph to the class remarks of `UiActionMap.cs` (the three paths, `PushGamepad`, and the untouched `InvokeAction`).
- [ ] 4.4 Write `tests/BrutalUi.Framework.Tests/Actions/SchemeLockTests.cs` with 7 tests. `SetSchemeChecksTheIdAndKeepsTheOldLock` (-1 and an id at or above the count throw `ArgumentOutOfRangeException` with `ParamName` `scheme` and keep the lock; `SetScheme(0)` clears it; `LockedScheme` reads it; a map that was never locked reports the device). `ALockEmptiesTheForeignFrameRead` (`Move` with the WASD composite tagged KeyboardMouse and the left stick tagged Gamepad: locked to Gamepad, W reads (0, 0) and the stick (0.5, 0) reads (0.5, 0); locked to KeyboardMouse the two flip). `ALockEmptiesTheForeignButtonFlags` (locked to Gamepad: Escape with `Cancel` gives `WasPressed` `false`, `IsHeld` `false`, and no command from the frame read; pad B gives `WasPressed` `true`). `ALockEmptiesThePressPath` (locked to Gamepad: `PressKey(32, None)` on a KeyboardMouse-tagged `Fire` gives `false` and runs no command, and `PressMouseButton(PointerButton.Right, None)` does the same; the same call with a default-scheme bind gives `true`). `ALockEmptiesTheGamepadInvoke` (locked to KeyboardMouse: a pad A press invokes nothing from the frame read and nothing from `PushGamepad`; locked to Gamepad both invoke one time). `ADefaultBindingReadsUnderEveryLock` (`BindKey(Fire, 32)` with no scheme reads held, pressed, and the command under a KeyboardMouse lock, under a Gamepad lock, and unlocked). `ALockReportsTheLockedScheme` (locked to Gamepad with a held W: `ActiveScheme` reads the Gamepad id whatever the keyboard reads; the label path `SchemeName(ActiveScheme)` reads `"Gamepad"u8`; after `SetScheme(0)` the next `Update` reports the device again).
- [ ] 4.5 Run the build, the tests, and `SchemeLockTests` (total 7). Check that `UiActionMapTests` (23) and `GamepadActionTests` (7) pass with no edit, so the press path and `PushGamepad` keep their unlocked behavior. Run three plants. Plant 10: `ReadFrame` drops the loop filter. Expect `ALockEmptiesTheForeignFrameRead` to fail. Plant 11: `PressKey` passes 0 to `Find`. Expect `ALockEmptiesThePressPath` to fail. Plant 12: the invoke pass and `PushGamepad` drop the filter. Expect `ALockEmptiesTheGamepadInvoke` to fail. Record the counts and the failed tests in `context.md`.

**Estimated Steps:** about 40 (5 tasks, about eight steps for one task)

**Files (one group, 5):** `UiActionMap.Schemes.cs`, `UiActionMap.Read.cs`, `UiActionMap.Bindings.cs`, `UiActionMap.cs`, `SchemeLockTests.cs` (+ `context.md`)

**Acceptance Criteria:**
- [ ] The 7 tests of `SchemeLockTests` pass. Each existing test passes with no edit. The Framework total reads 289 (282 + 7), and the solution reads 2951 with 3 skipped.
- [ ] Plants 10 to 12 give a non-zero exit code, and each restored file equals its copy under `cmp`.
- [ ] `grep -n 'SchemeAllows' src/BrutalUi.Framework/Actions/*.cs` shows the helper, the binding loop, the invoke pass, `PushGamepad`, and `Find` in lock mode. `git diff` of `src/BrutalUi.Core/` shows no output.

### Phase 5: The Allocation Gate and the Docs
**Goal:** `ActionMapAllocationTests` grows with a measured body that drives a device switch and calls the change slot, and keeps the fixed-increment real-work assert. Section 9 of the technical design gains the scheme item, section 14 gains the new classes, and `CHANGELOG.md` gains one line.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*ActionMapAllocationTests'` and reads a total of 7 with `failed: 0`.
**Needs:** Phase 1, Phase 2, Phase 3, Phase 4
**Agent tier:** STANDARD (sonnet). The gate copies the shape of `QueryStep`, and the doc edits are named line by line.

**Tasks (group 1, the gate):**
- [ ] 5.1 Grow `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs`. In `BuildScene`, add `int kbScheme = map.AddScheme(UiScheme.KeyboardMouse);` and `int gpScheme = map.AddScheme(UiScheme.Gamepad);` before the adds, store them in two new `required int` fields of `Scene`, and set `map.OnSchemeChanged = s_schemeHandler;` after `map.Activate(context)`. Add the static handler and its counter in the pattern of `InputReadTests.s_gateHandler`: `private static readonly UiSchemeChangedHandler s_schemeHandler = static (map, scheme) => s_schemeCalls++;` with `private static int s_schemeCalls;`. Add three actions and four bindings, each on a fresh input so no existing clash rule trips: `Kick` (Button, no command) with `BindKey(Kick, 297, UiModifiers.None, kbScheme)` and `BindGamepadButton(Kick, UiGamepadButtons.Y, gpScheme)`, `Turn` (Vector2) with `BindGamepadStick(Turn, UiGamepadStick.Right, gpScheme)`, and `Aim` (Axis) with `BindGamepadAxis(Aim, UiGamepadAxis.LeftStickX, 1f, gpScheme)`. Key 297 is free (the scene binds 290 to 296 and 301), `UiGamepadButtons.Y` is free (the scene binds A, B, X, DpadUp, and DpadDown), the right stick is free (the left belongs to `Move`), and a `GamepadAxis` read of `LeftStickX` is a different kind from the `GamepadStick` read of `Move`, so no clash throws at scene build. Add two pad states as scene fields: `PadIdle` (no button, six axes of 0) and `PadRight` (button Y, axes `{ 0.3f, 0f, 0.6f, -0.2f, 0f, 0f }`). Add the kick-key down and release events as fields, with the const `KickKey = 297` (GLFW F8; the scene binds 290 to 296 and 301). Add the int field `SchemeSum` to the scene. Add `SchemeStep(Scene scene)`: frame 1 `ClearFocus()`, `SetGamepad(0, in scene.PadIdle)`, push the kick key down, `Update`, `ReadScheme(scene)`, `Draw`; frame 2 push the kick-key release, `SetGamepad(0, in scene.PadRight)`, `Update`, `ReadScheme(scene)`, `Draw`. Add `ReadScheme(Scene scene)`: `int id = map.ActiveScheme; float2 turn = map.ReadVector(TurnAction); scene.SchemeSum += id + map.SchemeName(id).Length + Count(map.WasPressed(KickAction)); scene.ValueSum += turn.X + turn.Y + map.ReadAxis(AimAction);`. Add `RunSchemeSteps(scene, steps)`. Add the test `TheSchemeReadAndTheChangeSlotAllocateNothing` (BuildScene, 2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, 100 rounds of `SchemeStep`, `Assert.Equal(0L, after - before)`, then `Assert.True(s_schemeCalls > 0)`). In `TheMeasuredBodyDoesRealWork`, after the query step read `s_schemeCalls` into a local with `SchemeSum` and `ValueSum`, run one `SchemeStep`, and assert the fixed increments: the handler ran exactly one time (frame 1 keeps KeyboardMouse, because the query step ends on KeyboardMouse and the kick-key bind gives no change; frame 2 gives Gamepad), `SchemeSum` grew by 24 (frame 1: id 1 + the 13 bytes of `KeyboardMouse` + 1 press; frame 2: id 2 + the 6 bytes of `Gamepad` + 1 press), and `ValueSum` grew by 1.1 (turn (0.6, 0.2) plus aim 0.3, each with five decimals). Record the numbers that the run gives in `context.md`, and keep them fixed. Update the class summary and remarks (26 actions, 32 bindings, the scheme table with its two seeded ids, the new step).
- [ ] 5.2 Run the build, the tests, and `ActionMapAllocationTests` (total 7). Run plant 13: `GC.KeepAlive(new object());` as the last line of `ReadFrame`, after `ReportSchemeChange()`. Expect `TheSchemeReadAndTheChangeSlotAllocateNothing` to fail, and record the byte count and the other failed gates in `context.md`. Restore with `cmp`, then run the whole solution again and expect 0 failed.

**Tasks (group 2, the docs):**
- [ ] 5.3 Edit `docs/technical-design.md`, section 9 `### Flow`. Add item 14 **The control schemes** after item 13, and state in it: the scheme table with one int id per UTF-8 name and id 0 for the default scheme that belongs to each scheme; `UiScheme` with the two standard names held from construction; `AddScheme`, `SchemeName`, and `SchemeName(0)`; the tag on each binding and the nine scheme overloads, with the eleven current signatures bound into the default scheme; the per-scheme duplicate rule with the clash rule of the default scheme and the set compare of a composite; the device table of "The Device Rule" and the carry across frames; the exact-zero test of the stored gamepad value (item 12); the lock rule on the frame read, the press path, the gamepad invoke, and `PushGamepad`, with the locked report of `ActiveScheme`; the slot rule for `UiSchemeChangedHandler` (in `Update`, after the read, one call per change, written with `=`); and the cost rule with `ActionMapAllocationTests`. Do not restructure the `### Action map` bullets, except one sentence: the build rule that reads "The duplicate check is map-wide" becomes the per-scheme rule (A6 names one item, and the wrong sentence must not stay). Name `SchemeGroupTests`, `ActiveSchemeTests`, `SchemeChangedSlotTests`, and `SchemeLockTests`.
- [ ] 5.4 Edit `docs/technical-design.md`, section 14. Add one bullet **Scheme tests.** with the four new classes, the growth of the seventeenth gate with the scheme step, and the count of new tests (31). One line beyond the item that A6 names, in the habit of the sibling features; record it in `context.md`.
- [ ] 5.5 Edit `CHANGELOG.md`, under `## [Unreleased]` `### Added`: one line for `UiScheme`, `UiSchemeChangedHandler`, `UiActionMap.AddScheme`, `SchemeName`, `ActiveScheme`, `LockedScheme`, `OnSchemeChanged`, `SetScheme`, the nine scheme overloads of the binds, the per-scheme duplicate rule, and the HelloPanel scheme label (input/control-schemes).
- [ ] 5.6 Run the build, the tests, and the demo commands of phases 1 to 4. Run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` and expect 2952 with 3 skipped, 0 failed. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes. Record in `context.md` the final counts, the follow-up for binding-remap (the id that resolves to a UTF-8 name, the empty name of the default scheme, the per-scheme clash rule, and the binding handle that CR-5 leaves there), and each decision that this plan did not settle.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (group 1, 2):** `ActionMapAllocationTests.cs`, `context.md`
**Files (group 2, 4):** `technical-design.md`, `CHANGELOG.md`, `context.md`, `implementation.md`

**Acceptance Criteria:**
- [ ] `ActionMapAllocationTests` reads a total of 7 with `failed: 0`, and plant 13 gives a non-zero exit code with the file restored equal under `cmp`.
- [ ] The solution reads 2952 (2921 + 31) with 0 failed and 3 skipped. The Core total stays 1801.
- [ ] Section 9 holds item 14 and the corrected duplicate sentence. Section 14 holds the bullet **Scheme tests.**. `CHANGELOG.md` holds one new line.
- [ ] The prose lint gives no score above 2.5 on each changed file.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 ActiveScheme reports the device of the input: a key, mouse, or pointer input gives KeyboardMouse, a gamepad input gives Gamepad; custom scheme names group bindings only.
- Q2 `SetScheme` filters every path — the frame read, the key and mouse press path, and the gamepad command invoke — and ActiveScheme reports the locked scheme while locked.
- Q3 ActiveScheme starts at KeyboardMouse, at construction and after `Activate`.
- Q4 Phase 1 demo: HelloPanel shows `Scheme: Keyboard / Mouse`, and a stick move or a gamepad button press flips the label to `Scheme: Gamepad`.
- A1 Core and `BrutalUi.Glfw` get no change. `UiScheme`, the scheme table, the tag, the active read, the slot, and the lock all live in `BrutalUi.Framework` (`docs/features/input/epic-requirements.md:24`, A2 of the epic).
- A2 A gamepad input "above the deadzone" means a stored value that is not exactly 0, read through `context.GameInput.Gamepad(GamepadSlot)`; the 0.15 and 0.1 settings live in `src/BrutalUi.Glfw/GlfwUiInput.cs:103` and `BrutalUi.Framework` references no Glfw (`docs/features/input/gamepad-axes/implementation.md:209`).
- A3 A scheme reaches a binding as one int id that resolves back to its UTF-8 name, because `binding-remap` writes the scheme name in each text line (`docs/features/input/binding-remap/requirements.md`); the frame read copies its arrays and counts to locals first, so a bind inside the change slot takes effect at the next frame.
- A4 This feature ships no scheme list read and no binding handle. `FindBindingIndex` and the read-one-binding hook stay in `binding-remap` (CR-5, `docs/features/input/input-actions/code-review.md:135`).
- A5 The allocation gate grows: `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` adds a measured body that drives a device switch and keeps the fixed-increment real-work assert (A3 of the epic).
- A6 HelloPanel gains a sixth label for the active scheme, seeded with its longest text and formatted into a fixed byte scratch like the Move label, and the feature adds one item to section 9 of `docs/technical-design.md` and one CHANGELOG line (A9 of the epic).

### Decision 1: One int scheme id, interned in a table, with 0 for the default scheme
**Choice:** `AddScheme(ReadOnlySpan<byte>)` scans a `byte[][]` table with `SequenceEqual` and returns one id: the existing id for a name it holds, else a new id. Id 0 is the default scheme, is not in the table, and belongs to each scheme. The constructor seeds ids 1 and 2 with `UiScheme.KeyboardMouse` and `UiScheme.Gamepad`. `SchemeName(int)` resolves an id, and `SchemeName(0)` gives an empty span.
**Rationale:** A3 needs the id to reach a UTF-8 name for the text line of binding-remap. An int in `InputBinding` keeps the struct aligned and makes the per-frame filter and the report compare cost nothing (A5). Interning makes `AddScheme(UiScheme.Gamepad)` idempotent, so two files of a game can ask for the same scheme. The scan style already exists in `IndexOfAction` (`UiActionMap.Bindings.cs:704`), and there is no hash, no `Utf8Span`, and no id type anywhere in `src/`.
**Alternatives:** (a) A name per binding (`byte[]` on `InputBinding`): a per-frame `SequenceEqual` on a heap array, and the report needs a copy. (b) A public `UiSchemeId` struct: one more type for no gain, and binding-remap writes an int either way.

### Decision 2: A scheme overload per bind, with the scheme id last and required (CR-4)
**Choice:** Each of the nine public binds gains one overload whose last parameter is a required `int scheme`. On `BindKey` and `BindMouseButton` the pattern is a trailing `UiModifiers modifiers` plus the scheme id; on the two scale binds it is `scale` plus the scheme id; on the rest it is the codes plus the scheme id. The eleven current signatures become one-line forwards with `scheme: 0`. No new overload takes an optional parameter.
**Rationale:** It is the shape that CR-4 names (`docs/features/input/input-actions/code-review.md:125`), it keeps every shipped call site of input-actions and spacegame working, and an optional scheme parameter cannot fit: `BindKey` and `BindMouseButton` already end with an optional `modifiers`, and `BindGamepadAxis`, `BindMouseDelta`, and `BindScroll` end with an optional `scale` (`UiActionMap.Bindings.cs:121`, `:174`, `:445`, `:586`, `:647`). With no optional parameter on the new side, each call resolves by arity, so no overload is ambiguous.
**Alternatives:** (a) A trailing optional `int scheme = 0` on all eleven binds: a caller can pass the scheme by position, but the existing `modifiers` and `scale` defaults sit in the way, and every current signature changes its metadata. The user rejected it. (b) An implicit current scheme at build time (`map.BeginScheme(gp)`): one hidden mutable build cursor, no per-call clarity, and it cannot express a default-scheme bind in a build that already chose a scheme. The user rejected it.

### Decision 3: The duplicate rule is a scheme-set intersection
**Choice:** `SchemeClashes(existing, new)` is true when either side is 0 or the two ids are equal. `Find` and `FindValue` take the new scheme and test it beside the kind, code, and modifier tests. The composite set compare of CR-3 (`SamePair`, `SameSet`, `HasCode` at `UiActionMap.Bindings.cs:779-813`) stays, so a clash test is set-based within one scheme.
**Rationale:** A default binding belongs to each scheme, so a default bind and a tagged bind of one input are two bindings of that input in the tag's scheme; the read would take both, and the two paths of input-actions (the slot resolve and the frame read) could name two actions for one press. Two tags that differ are not a clash, which is exactly what "the same input can bind in two schemes" needs. binding-remap gets one conflict rule per scheme, as its line 14 asks.
**Alternatives:** (a) Equality only: a default `BindKey(Fire, Space)` plus a tagged `BindKey(Jump, Space, gp)` binds, and one press resolves to two actions. (b) Map-wide as today: no game can bind one key per scheme, which kills the feature.

### Decision 4: The active scheme is the device of the input, not the tag of the binding
**Choice:** The frame read maps each kind to one of the two standard ids (`Key`, `MouseButton`, `KeyVector`, `KeyAxis`, `MouseDelta`, `Scroll` to KeyboardMouse; `GamepadButton`, `GamepadStick`, `GamepadAxis` to Gamepad) and keeps the value of the last binding in bind order that gave a value. A custom tag never appears in `ActiveScheme`, unless the game locks to it.
**Rationale:** Q1. A glyph follows the device, and a game that groups flight binds under a custom name still shows the pad glyph. The device is a property of the kind, so the rule costs one int store in a case that already ran.
**Alternatives:** Report `binding.Scheme` of the last binding that gave a value: a stick bound in a custom scheme reports that custom scheme, and the sample needs a device anyway. It also breaks the "a gamepad input above the deadzone sets Gamepad" sentence of the requirement.

### Decision 5: The lock is the only filter; an unlocked map reads every binding
**Choice:** `SchemeAllows` returns true when the map is unlocked, whatever the tag. A foreign binding gives no value only under a lock.
**Rationale:** The requirement puts "gives no value" on `SetScheme` alone. And the auto-switch needs it: at construction the map reports KeyboardMouse, so a `Gamepad`-tagged stick bind must still read, or it could never set Gamepad and the Q4 demo could never flip. Notes conflict 3 records the one case this leaves open.
**Alternatives:** Read the default scheme plus the active scheme when unlocked: the device can never move to a scheme that no binding of it reads, and the flip rule of Q4 breaks.

### Decision 6: A held input re-asserts its device in each frame
**Choice:** A binding "gave a value" when its read of the frame is not nothing, which includes a held key, a held mouse button, a stick away from rest, a trigger above 0, and a nonzero pointer delta or scroll. A frame with no input changes nothing, because the device local starts from the last report.
**Rationale:** It is the literal reading of "the scheme of the last binding that gave a value", and it matches the store rule of A2 (a stick at rest is exactly 0). It gives the sample its expected behavior: W held over a resting stick reads KeyboardMouse, and a resting pad after a pad move keeps Gamepad.
**Alternatives:** Press edges only: a held stick stops updating, so a pad player who moves the stick after a key press would not flip the glyph until they re-pressed a button. A per-frame reset to KeyboardMouse: the label flips back with no input at all.

### Decision 7: `OnSchemeChanged` is one slot on the map, called at the end of the read
**Choice:** `public UiSchemeChangedHandler? OnSchemeChanged { get; set; }`, written with `=` and never `+=`, one delegate held, and called at the end of `ReadFrame` after the finish pass and the gamepad invoke pass, on a change of the reported value only. `_reportedScheme` is the last value that the map reported, and `ResetFrame` sets it with `_activeScheme` (Q3).
**Rationale:** The requirement names a slot on the map called in `Update` after the read, and the repo pattern for a slot is one nullable delegate auto-property (`UiContext.OnInputRead`, `CommandRegistry.UnknownCommand`). The end of `ReadFrame` is the last point inside `Update` that the map owns, so the handler sees the finished values and the invoked commands. Resetting the report in `ResetFrame` reuses the call sites of the switch rule, so `Activate` needs no new statement and Q3 holds.
**Alternatives:** (a) An `event`: a combined delegate and an allocation at add time. (b) A slot on `UiContext`: Core changes, against A1. (c) A call before the invoke pass: a gamepad command would run after a glyph change of the frame that caused it, and "after the read" would be false.

### Decision 8: The lock filter covers `PushGamepad`, and not `InvokeAction`
**Choice:** The filter applies to the binding loop and the gamepad invoke pass of `ReadFrame`, to `Find` in lock mode from `PressKey` and `PressMouseButton`, and to the loop of `PushGamepad`. `InvokeAction` and the five queries keep their behavior: the lock already emptied the values.
**Rationale:** Q2 says every path, and `PushGamepad` is the gamepad command invoke of a map that is not active (`UiActionMap.Read.cs:29-51`); a locked map that a game still pushes must not run a foreign pad bind. `InvokeAction` names an action, not an input, so it is not a device path. The body of `PushGamepad` for an unlocked map does not change, so `GamepadActionTests` (7) and the `GamepadStep` gate keep passing.
**Alternatives:** Leave `PushGamepad` unfiltered: a locked gamepad menu still runs a keyboard-only or pad-only bind that the game pushed in, which contradicts the requirement.

### Decision 9: Three new files in the Actions folder, and the tag threads through the existing bodies
**Choice:** `UiScheme.cs`, `UiSchemeChangedHandler.cs`, and `UiActionMap.Schemes.cs` are new. The tag, the nine overloads, and the two finds stay in `UiActionMap.Bindings.cs`, because each bind keeps one body there. `UiActionMap.cs` gains the `Scheme` field, the seed call, the `ResetFrame` lines, and the class remarks. `UiActionMap.Read.cs` gains the device track, the change call, and the filters.
**Rationale:** The scheme state, the table, and the lock form one group, so they get one partial file, in the habit of the three-file split of input-actions. Moving nine bind bodies to a new file would split each kind's read from its bind, and would leave `Bindings.cs` with forwards only.
**Alternatives:** One bigger `UiActionMap.Bindings.cs` (843 lines today) that also holds the table: the duplicate rule and the frame filter then live apart from each other.

### Decision 10: HelloPanel maps a scheme id to its own display text
**Choice:** `HelloScreen.SchemeText(ReadOnlySpan<byte> scheme, Span<byte> scratch)` writes `Scheme: Keyboard / Mouse` for the `KeyboardMouse` name, `Scheme: Gamepad` for the `Gamepad` name, and the name itself for any other. The label seeds with `LongestSchemeText` (24 bytes) and formats into a fixed 32-byte scratch, like the Move label.
**Rationale:** Q4 names the two label texts, and `UiScheme` holds the bytes `KeyboardMouse`. The requirement puts a glyph table and a scheme to art map out of scope, so the sample owns the words, which is also what a real game does with sprites.
**Alternatives:** Print `SchemeName(ActiveScheme)` whole: the demo would read `Scheme: KeyboardMouse`, not the confirmed text.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A new overload is ambiguous with an existing signature, and a shipped call site fails to compile. | Medium | High | Decision 2: no optional parameter on any new overload, so each call resolves by arity. `TheOldSignaturesBindIntoTheDefaultScheme` compiles all eleven forms, and the eight existing Actions classes plus `ModeActionMapTests` pass with no edit. |
| A locked menu still runs a foreign bind, because one path missed the filter. | Medium | High | Q2 as three paths plus `PushGamepad`. `SchemeLockTests` pins each path, and plants 10 to 12 each break one of them. |
| A held key pulls the report back to KeyboardMouse while a player uses the pad, so a glyph flickers. | Medium | Low | Decision 6 states the rule, and `AHeldInputKeepsItsSchemeEachFrame` pins it. Section 9 states it, so a game that wants press-only can lock the scheme. |
| The same input bound in two schemes reads twice while the map is unlocked. | Medium | Medium | Decision 5 is deliberate and stated. The clash rule blocks the default-versus-tag case. Section 9 and `ActiveSchemeTests` state that a game that separates the two must lock. |
| The change slot fires many times in a row when a player alternates devices, and a game rebuilds glyphs each time. | Low | Low | One call per change, and none without a change (`TheSlotCallsOneTimePerChange`, plant 8). |
| A handler allocates, because a game writes a new lambda in each frame. | Low | Medium | The build-time rule in the delegate remarks and the slot docs, and the gate scene sets a `static readonly` handler in `BuildScene`. |
| The device track or the change check allocates in the frame path. | Low | High | One int store per case and one compare per frame. `TheSchemeReadAndTheChangeSlotAllocateNothing` at 0 bytes, and plant 13 guards it. |
| The gate scene's new binds clash with the existing 28 bindings. | Medium | Medium | Task 5.1 uses fresh inputs (keys 297 and 298, `UiGamepadButtons.Y`, the right stick, `LeftStickX` as an axis), and the check order makes a clash throw at scene build, so a mistake shows at once. |
| `TheMeasuredBodyDoesRealWork` fixed increments move when the scene grows. | Medium | Medium | New values reach new actions only, so the four existing asserts (500.25, 3, and the counter deltas) hold. The new asserts read the counter immediately before the step. Record the measured numbers in `context.md`. |
| The HelloPanel demo cannot run: the sandbox blocks the X11 socket. | High | Medium | Gotcha 3 of input-state, already open for three display checks. Phase 1 pins the same behavior headless in `ActiveSchemeTests`, and task 1.11 hands the smoke PASS run, plant 4, and the live check to Needs Human Verification. |
| The lock breaks the switch rule or the `IsActive` pair test of input-actions. | Low | High | `Activate` and `Deactivate` change nothing but `ResetFrame`. Phase 3 and phase 4 each re-run `ActionMapSwitchTests` (6), `ModeActionMapTests` (5), and `ModeDiffTests` with no edit. |
| A doc comment or a cref for a scheme member is missing, and the build fails. | Low | Low | `TreatWarningsAsErrors` with CS1591 makes it a build error, and each task lists the members it docs. |
| The test host exits with 134, or `TheCurvesAllocateNothing` fails one time. | Medium | Low | Run the tests a second time. |
| `dotnet build` or `dotnet test` hangs on stale MSBuild nodes. | High | Low | Each command sets `MSBUILDDISABLENODEREUSE=1`, and `dotnet build` gets `-nodeReuse:false`. No `dotnet build-server shutdown`. |

---

## Dependencies

### Internal Dependencies
- input-actions (done): the nine binds, `InputBinding`, `ActionEntry`, `ReadFrame` with its four passes, the slot pair, `ResetFrame`, the switch rule, the set-based duplicate rule of CR-3, and the gate class that this feature grows.
- input-state (done): `UiContext.GameInput`, `UiKeyState`, `UiPointerButtonState`, `UiPointerState`, `UiContext.SetGamepad`, and the gate. Read only.
- gamepad-axes (done): `UiGamepadState.Buttons`, `.Pressed`, `LeftStick`, `RightStick`, `Axis(int)`, and the deadzone baked into the stored value (A2).
- state-and-commands/action-map (done): `OnUnhandledInput`, `PressKey`, `PressMouseButton`, `PushGamepad`, and `CommandRegistry`.
- panels-and-modes/mode-set (done): `Activate` and `Deactivate` only. `UiManager.cs` does not change.
- The test helpers `AllocationGate.cs` (`tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs`) and `RecordingRenderer.cs` (done).

### External Dependencies
- None new. No package and no native dependency. Framework references `BrutalUi.Core` only (`src/BrutalUi.Framework/BrutalUi.Framework.csproj`), which is what makes A2's "not exactly 0" test the right rule (no deadzone constant reaches Framework).

### Sibling Features That Depend on This One
- binding-remap (next): `AddScheme`, `SchemeName` for the scheme name of each text line (id 0 gives the empty name that means each scheme), `InputBinding.Scheme`, `SchemeClashes` for the per-scheme conflict report, `LockedScheme`, and `BeginRebind(action, scheme)` on the id that this feature defines.
- spacegame map-controls (outside this repo): keeps the eleven signatures, and can tag its binds when it wants a pad-only camera.
- No feature before this one changes.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. `AddScheme(UiScheme.KeyboardMouse)` called twice gives one id, `AddScheme("gamepad"u8)` gives another than `UiScheme.Gamepad`, `SchemeName(id)` gives the name bytes back, `SchemeName(0)` is empty, an empty name throws `ArgumentException` with `ParamName` `name`, and `SchemeName` of a negative id or an id at or above the table count throws `ArgumentOutOfRangeException` with `ParamName` `scheme`. A new map resolves both standard names with no add (`SchemeGroupTests`).
- [ ] F2. Each of the eleven current build signatures compiles unchanged and binds into the default scheme. Each of the nine scheme overloads binds into the given scheme. A bind with a bad scheme id throws `ArgumentOutOfRangeException` with `ParamName` `scheme` and changes `ActionCount` and `BindingCount` (`SchemeGroupTests`).
- [ ] F3. `BindKey(Fire, 32, None, kb)` then `BindKey(Jump, 32, None, gp)` binds. `BindKey(Fire, 32)` (default) then any tagged bind of the same key throws, and the reverse order throws. A `KeyAxis` pair and a `KeyVector` key set clash in any order inside one scheme, and bind across two schemes (`SchemeGroupTests`).
- [ ] F4. `ActiveScheme` reads the KeyboardMouse id at construction and after `Activate`. W on a WASD composite reads KeyboardMouse. The pointer delta and the scroll read KeyboardMouse. A stick at (0.4, 0) and a gamepad button down read Gamepad. A stick at (0, 0), a trigger at exactly 0, W and S together, and a gated pointer read change nothing. With both a key and a pad bit live in one frame, the later binding in bind order decides. A held input keeps its device in each frame, and a frame with no input keeps the last report (`ActiveSchemeTests`).
- [ ] F5. `OnSchemeChanged` runs inside `UiContext.Update(float)`, at the end of the frame read, with the map and the new scheme, and it sees the finished values of that frame. One change gives one call. Three frames with the same device give no further call. A write with `=` replaces the handler, and a `null` slot calls nothing. A `SetScheme` or a bind made inside the handler takes effect at the next frame (`SchemeChangedSlotTests`).
- [ ] F6. `SetScheme(gp)` makes a key, mouse, composite, delta, or scroll binding of another scheme give no value and no command on all three paths: the frame read (values, flags, and the gamepad invoke pass), `PressKey` and `PressMouseButton`, and `PushGamepad`. A binding with no scheme gives its value under every lock. `ActiveScheme` reads the locked id while locked, and the device again after `SetScheme(0)`. A bad id throws and keeps the old lock. `Activate` and `Deactivate` keep the lock (`SchemeLockTests`).
- [ ] F7. HelloPanel shows a sixth label with `Scheme: Keyboard / Mouse` at rest and after a held W, and `Scheme: Gamepad` after a stick move or a pad button press, and it returns to `Scheme: Keyboard / Mouse` when W is held again with the stick at rest.

### Quality Criteria
- [ ] Q1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Framework.Tests` holds 31 more tests than at `<base>` (9, 8, 6, 7, and 1), reading 290; the solution holds 31 more, reading 2952, with 3 skipped. `BrutalUi.Core.Tests` stays 1801.
- [ ] Q3. `UiActionMapTests`, `GamepadActionTests`, `FocusActionTests`, `TypedActionMapTests`, `GamepadValueActionTests`, `ActionQueryTests`, `ActionMapSwitchTests`, and each test under `tests/BrutalUi.Framework.Tests/Panels/` pass with no edit. The one edited existing test class is `ActionMapAllocationTests`.
- [ ] Q4. `TheSchemeReadAndTheChangeSlotAllocateNothing` asserts 0 bytes over 100 rounds of `SchemeStep`, and plant 13 fails it. `TheMeasuredBodyDoesRealWork` keeps a fixed-increment assert for every measured body, including the scheme step, so an empty body cannot pass.
- [ ] Q5. Each new public member (`UiScheme`, `UiSchemeChangedHandler`, `AddScheme`, `SchemeName`, `ActiveScheme`, `LockedScheme`, `OnSchemeChanged`, `SetScheme`, and the nine overloads) has an XML doc comment in ASD-STE100 prose, and the lint gives no score above 2.5 on the changed files.
- [ ] Q6. `SchemeAllows`, `SchemeClashes`, `AddScheme` outside its name copy, `SchemeName`, `SetScheme`, `ActiveScheme`, `LockedScheme`, `ReportSchemeChange`, the device track, and the filter in `ReadFrame`, the invoke pass, `PushGamepad`, and `Find` in lock mode hold no array creation, no `lock`, no `event` keyword, no `ToString(` call, no `(object)` cast, no lambda, and no `List<`.

### Integration Criteria
- [ ] I1. `git diff <base> -- src/BrutalUi.Core/ src/BrutalUi.Glfw/` shows no output (A1). `UiContext.cs`, `UiInputReadHandler.cs`, `UiInputState.cs`, `UiGamepadState.cs`, `UiActionEnums.cs`, `UiActions.cs`, and `src/BrutalUi.Framework/Panels/UiManager.cs` show no diff.
- [ ] I2. `UiActionMap` stays one class in namespace `BrutalUi.Framework`, now split over four files: `UiActionMap.cs`, `UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, and `UiActionMap.Schemes.cs`. `grep -nE 'Scheme|UiScheme' src/BrutalUi.Core/ src/BrutalUi.Glfw/` shows no hit.
- [ ] I3. `samples/HelloPanel/HelloScreen.cs` holds `SchemeText` and `LongestSchemeText`, `HelloModes.cs` holds `SchemeLabel`, `Program.cs` holds `schemeScratch` and one `SchemeText` call inside the UI region, and `HelloState.cs` tags its six standard binds with the two scheme ids. The five existing labels and their texts do not change.
- [ ] I4. Section 9 of `docs/technical-design.md` holds one new item for the control schemes and the corrected per-scheme duplicate sentence. Section 14 holds the bullet **Scheme tests.**. `CHANGELOG.md` holds one new line that names each new public member.
- [ ] I5. The plan states what binding-remap inherits: one int scheme id per binding that resolves back to UTF-8 bytes through `SchemeName`, `SchemeName(0)` empty for the default scheme, the `SchemeClashes` rule as the conflict rule of one scheme, and the lock of `BeginRebind(action, scheme)`. This feature ships no `FindBindingIndex`, no read-one-binding hook, and no scheme list read (A4, CR-5).
- [ ] I6. `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` prints its PASS line on a host with a working display. On a host without one, `context.md` records the exact smoke line and hands the run to a person, as the three open display-host checks of input-actions already do.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. Read `<base>` and the base counts from `docs/features/input/control-schemes/context.md`. Do not run `dotnet build-server shutdown`. When a run exits with 134 or `TheCurvesAllocateNothing` fails, run it a second time.

1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed, a total 31 above `<base>`, and the skipped count of `<base>` (Q2).
3. Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5` for `ActiveSchemeTests` (9), `SchemeGroupTests` (8), `SchemeChangedSlotTests` (6), `SchemeLockTests` (7), and `ActionMapAllocationTests` (7). Expect `failed: 0` for each (F1 to F6, Q4). Run the same binary for `*UiActionMapTests`, `*GamepadActionTests`, `*FocusActionTests`, `*TypedActionMapTests`, `*GamepadValueActionTests`, `*ActionQueryTests`, `*ActionMapSwitchTests`, and `*ModeActionMapTests`, and compare with the base counts (Q3). Read the test names and match them to the criteria.
4. Signatures: `git diff <base> -- src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs | grep '^-' | grep 'public void Bind\|public void AddAction'`. Expect no output, so none of the eleven current signatures changed (F2). `grep -c 'public void Bind' src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` reads 18 (F2, I2).
5. Plants, as a re-run of four phase plants. Copy `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs` to `src/BrutalUi.Framework/obj/plantscratch/`. Plant 1 (the device of a kind, F4): write `kb` in the `GamepadStick` case, run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > <scratch>/p1.log 2>&1; echo "exit=$?"`, expect a value that is not 0 and `AStickAboveRestSetsTheGamepadScheme` in the failed list, restore with `cp`, and check with `cmp`. Plant 11 (the lock on the press path, F6): pass 0 to `Find` in `PressKey`, expect `ALockEmptiesThePressPath` to fail. Plant 8 (one call per change, F5): drop the compare in `ReportSchemeChange`, expect `TheSlotDoesNotCallWhenTheSchemeDoesNotChange` to fail. Plant 13 (allocation, Q4): add `GC.KeepAlive(new object());` after `ReportSchemeChange()`, expect `TheSchemeReadAndTheChangeSlotAllocateNothing` to fail. Check `0 Error(s)` for each plant, and run step 2 after the restores (Q4, F5, F6).
6. `grep -nE 'new [A-Za-z0-9_]+\[|\block\b|\bevent\b|ToString\(|\(object\)|=>|List<' src/BrutalUi.Framework/Actions/UiActionMap.Schemes.cs src/BrutalUi.Framework/Actions/UiActionMap.Read.cs | grep -v '///'`. Read each hit: the only array creation in the frame path may sit in `AddScheme`, and none may sit in `SchemeName`, `SetScheme`, `ReportSchemeChange`, or `ReadFrame` (Q6).
7. `git diff <base> --name-only -- src/ samples/ scripts/ tests/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/ tests/`. Expect only the files of the File Tree (I2, I3). `git diff <base> -- src/BrutalUi.Core/ src/BrutalUi.Glfw/ src/BrutalUi.Framework/Panels/UiManager.cs`. Expect no output (I1).
8. `grep -n 'SchemeText\|schemeScratch\|SchemeLabel' samples/HelloPanel/HelloScreen.cs samples/HelloPanel/HelloModes.cs samples/HelloPanel/Program.cs` (I3). Then `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I6). In a sandbox that blocks the X11 socket, the smoke step fails with `GLFW init failed: PlatformUnavailable`; run it on a host with a working display, or under `xvfb-run -a` in a normal shell.
9. The manual check of HelloPanel (F7) on a host with a window: `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. Read `Scheme: Keyboard / Mouse` at rest. Press W and read the same. Push the left stick and read `Scheme: Gamepad`. Release the stick and read `Scheme: Gamepad`. Press B and read `Scheme: Gamepad`. Hold W with the stick at rest and read `Scheme: Keyboard / Mouse`. Click the text field, move the pointer, and read `Scheme: Gamepad` unchanged (the gate empties the pointer part). The sample's own allocation line must still read 0 bytes.
10. Read section 9 and section 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I4, I5). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q5).

---

## Testing Strategy

### Unit Tests
- `Framework.Tests/Actions/ActiveSchemeTests` (9): the seed at construction and after `Activate`, the six keyboard kinds, the three gamepad kinds, the exact-zero rule of A2, the bind order, the dead value, the held re-assert, and the carry across frames.
- `Framework.Tests/Actions/SchemeGroupTests` (8): the intern rule, the two standard names in the table, `SchemeName` and its id check, the empty name, the two-scheme bind, the default clash in both directions, the composite set rule per scheme, and the eleven current signatures.
- `Framework.Tests/Actions/SchemeChangedSlotTests` (6): the place after the read, one call per change, no call without a change, one handler written with `=` and both parameters, a handler change that lands next frame, and the locked report after `Activate`.
- `Framework.Tests/Actions/SchemeLockTests` (7): the id check and the kept lock, the frame read, the Button flags, the press path, the gamepad invoke and `PushGamepad`, the default binding under every lock, and the report of the locked scheme.

### Integration Tests
- The same four classes run through `UiContext.Update(float)` with `Activate`, `Deactivate`, `SetGamepad`, and hand-built events, so the scheme pass is checked inside the real frame order of `UiContext.cs:746-765`.
- `ActionMapSwitchTests` (6) and `ModeActionMapTests` (5) run with no edit, so the scheme reset does not break the switch rule or the `IsActive` pair test (A4 of the epic).
- The HelloPanel smoke run of `scripts/test-linux.sh` drives the scheme label in each frame, so the sample gate of 0 bytes covers `ActiveScheme`, `SchemeName`, and `SchemeText`.

### Allocation Tests
- `ActionMapAllocationTests` (the seventeenth gate) grows with `SchemeStep`: a keyboard frame, then a gamepad frame, the change slot handler, `ActiveScheme`, and `SchemeName`. `TheSchemeReadAndTheChangeSlotAllocateNothing` asserts 0 bytes, and plant 13 guards it (A5).
- The four existing gates keep passing with the change slot set in `BuildScene`, so the change check is inside each of them.

### Regression Tests
- Each existing test runs with no edit, except `ActionMapAllocationTests`. `UiActionMapTests` and `GamepadActionTests` guard the press path and `PushGamepad` on an unlocked map. `TypedActionMapTests`, `GamepadValueActionTests`, and `ActionQueryTests` guard the values that the device rule reads.
- `SchemeGroupTests` and `ActiveSchemeTests` guard the shipped spacegame surface: the eleven signatures and the five queries.

### Manual Testing
- Step 9 of the Verification Method: the two label texts, the stick, the B button, the held W pull-back, and the gate. It needs a host with a window, and `context.md` tracks it as an open display-host check.

---

## Related Documentation

- **Requirements:** `docs/features/input/control-schemes/requirements.md`
- **Epic:** `docs/features/input/epic-overview.md` (`epic-overview.md:63`, a scheme is a tag on each binding), `docs/features/input/epic-requirements.md` (`:20` the epic-level line, `:24` A2, `:25` A9), `.beast-mode/interview.md` (Q4, Q7, A2, A3, A9)
- **Siblings:** `docs/features/input/input-actions/implementation.md` and its `code-review.md` (CR-4 at `:112-131`, CR-5 at `:135-143`), `docs/features/input/gamepad-axes/implementation.md` (`:209` the above-deadzone rule), `docs/features/input/input-state/implementation.md` (`:82` control-schemes does not read the raw state), `docs/features/input/binding-remap/requirements.md` (the scheme name in each line, `BeginRebind(action, scheme)`)
- **Design:** `docs/technical-design.md` sections 4 (Frame order), 9 (Input and Action map, items 11 to 13), and 14 (Testing). `docs/mission-statement.md`, `docs/feasibility.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Framework/Actions/UiActions.cs:20-32` (the standard-name class), `src/BrutalUi.Framework/Actions/UiActionMap.cs:155` (the throw-name constant), `:230-244` (`GamepadSlot` and the keep-the-old-value rule), `:206-207` and `:454-497` (the cached delegates and the two slots), `:536-551` (`ResetFrame`), `:640-678` (`InputBinding`), `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:684-813` (`Find`, `FindValue`, and the set helpers), `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs:210-373` (`ReadFrame`), `src/BrutalUi.Core/Input/UiInputReadHandler.cs` (the delegate doc shape), `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` and `Shared/AllocationGate.cs` (the gate), `tests/BrutalUi.Core.Tests/Input/InputReadTests.cs` (a static slot handler), `samples/HelloPanel/HelloScreen.cs:660-725` (`MoveText` and the seeded label)
- **Skills:** `game-dev` for the standard phases, `game-dev-advanced` for phases 2 and 4. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (A) of each question. A change before the phase that the question names costs little.

1. **`SetScheme` shape** (before phase 4)
   - **(A) Recommended:** the method `SetScheme(int scheme)` that the requirement names, `0` clears the lock, and a read-only `LockedScheme` property gives the current lock.
   - (B) A property `Scheme { get; set; }` with `value = 0` to clear, in the style of `GamepadSlot`. Fewer members, and it renames nothing, but the requirement's `SetScheme` disappears.
2. **The name of the default scheme** (before phase 1)
   - **(A) Recommended:** `SchemeName(0)` gives an empty span, and the docs say the default scheme has no name and means each scheme.
   - (B) A stored name such as `Any`, so binding-remap writes one word per line. One more table entry, and a game could bind a scheme named `Any`.
   - (C) Throw for id 0. Then every writer must special-case the default.
3. **`AddScheme` on a name the table holds** (before phase 1)
   - **(A) Recommended:** intern and return the existing id, so two build sites can ask for one scheme.
   - (B) Throw, in the style of `AddAction`. A game then needs a find-or-add dance for a name it uses twice.
4. **The device rule for a held value** (before phase 1)
   - **(A) Recommended:** a held input re-asserts its device in each frame (Decision 6), which is what the Q4 demo reads as.
   - (B) Press edges only: the label flips only on a fresh press, and a pad player who holds the stick after a key press sees a stale glyph.
5. **A tagged bind and a default bind of one input** (before phase 2)
   - **(A) Recommended:** throw (Decision 3), because the default binding already lives in the tagged scheme.
   - (B) Allow it: one press then feeds two actions whenever the map is unlocked, which is the case Notes conflict 7 already leaves open.

### Conflicts and Gaps

1. **"Each bind takes an optional scheme name" and the chosen overloads.** Requirements line 12 asks for an optional parameter on each bind. A `ReadOnlySpan<byte>` cannot be an optional parameter, and CR-4 (`input-actions/code-review.md:118`) names the same block. The plan keeps the intent of the line — every bind can name a scheme, and a bind with no scheme belongs to each scheme — and lands it as one scheme overload per bind plus eleven unchanged signatures that bind into the default scheme (Decision 2). The scheme reaches a bind as an id, not as a name, per A3.
2. **`AddScheme` names a scheme, and a bind carries an id.** Requirements lines 12 and 13 speak of names on the binds. A3 and the binding-remap line format need one int that resolves back to UTF-8 bytes, so the public surface takes an id and gives `SchemeName` back. The name stays the source of truth: `UiScheme` holds the standard names, and a custom name reaches the table through `AddScheme`.
3. **"The scheme of the last binding that gave a value" versus Q1 and Q2.** Read literally, a binding tagged `FlightStick` would make `ActiveScheme` report `FlightStick`, and a locked map would have to report a device. Q1 says the report is the device and custom names group bindings only, and Q2 says a locked map reports the locked scheme. The plan keeps Q1 and Q2 and states the difference in the class remarks and section 9: unlocked, `ActiveScheme` reports the device of the last binding that gave a value; locked, it reports the locked scheme (Decision 4, Decision 5).
4. **The Phase 1 demo needs a live window.** Q4 names a HelloPanel check, and rule 11 of the slice rules binds phase 1 to it. The branch already carries three open display-host checks from input-actions (the `test-linux.sh` smoke PASS, plants 10 and 20, and the Move-label demo), and Gotcha 3 of input-state says the sandbox blocks the X11 socket. Phase 1 states the demo as written, pins the same behavior headless in `ActiveSchemeTests`, and hands the live run, plant 4, and the label check to Needs Human Verification in `context.md`.
5. **Q2 "every path" and `PushGamepad`.** The three paths that CR-4 and the frame read name are the binding loop, the press path, and the invoke pass. `PushGamepad` is the gamepad command invoke of a map that is not active, so the plan filters it too (Decision 8). input-actions Decision 9 keeps its body for an unlocked map, and `GamepadActionTests` and the `GamepadStep` gate pass with no edit.
6. **Section 9 stays one item, and one old sentence is now wrong.** A6 names one item in section 9 and one CHANGELOG line. The build rule at `docs/technical-design.md:974` reads "The duplicate check is map-wide", which phase 2 makes untrue, so task 5.3 corrects that one sentence inside the item's scope. Task 5.4 adds one bullet to section 14 for the four new classes, in the habit of the sibling features. That line sits beyond the letter of A6; record it in `context.md`.
7. **An unlocked map reads every binding.** Decision 5 gives the reason, and the case it leaves open is a game that binds one input in two schemes and never locks: both bindings then read, and the press path resolves the first one. Section 9 and the `ActiveScheme` remarks state the rule, and `TheOldSignaturesBindIntoTheDefaultScheme` and `ASecondBindOfTheSameKeyInTwoSchemesBinds` pin the shape. A later review can weigh a stricter rule if a consumer needs one.
8. **The plan is not compiled, and the prose lint did not run.** The planner built no new code. It read the source, and it ran the built `BrutalUi.Framework.Tests` binary in place: 259 tests, 0 failed, 0 skipped, exit 0, 615 ms. The solution total of 2921 with 3 skipped comes from `docs/features/input/input-actions/context.md`, because the three skipped tests sit in the Core and Glfw projects. Task 1.7 is the first compile of new code, and task 5.6 runs the lint on this plan and on each changed doc.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
