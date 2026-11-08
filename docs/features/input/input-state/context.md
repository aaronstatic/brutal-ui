# input/input-state - Context & Decisions

**Last Updated:** 2026-11-08 16:40
**Current Phase:** Phase 5 group 2 complete (tasks 5.3-5.6, the docs, the changelog, and the final runs) - all 32 tasks done. Next: the phase 5 verifier, then `/update-feature` and `/evaluate-feature`.
**Status:** 🟡 In Progress

---

## Quick Status

**What's Done:**
- ✅ Plan approved (`implementation.md`, 5 phases, 32 tasks).
- ✅ Feature scaffolded by `/start-feature` (this file, `tasks.md`, `inbox.md`).
- ✅ panel-sample committed (`aec0341`) and fast-forward merged into `feat/input/input-state`, so the HelloPanel files match the plan (Notes, gap 6).
- ✅ Phase 1 group 1: tasks 1.1-1.6 (Core path). The 8 new tests pass, the three plants fail as expected, and the full suite is green.
- ✅ Phase 1 group 2: tasks 1.7-1.10 (HelloPanel input label). Build 0/0, solution 2832 with 0 failed.
- ✅ Phase 2 group 1: tasks 2.1-2.4 (buttons, press ownership, scroll). `InputStateTests` total 18 with 0 failed, solution 2842 with 0 failed, plants 4-6 each exit non-zero with their expected failure.
- ✅ Phase 3 group 1: tasks 3.1-3.5 (the held keys, the repeat rule, the modifiers, the clear rules). `InputStateTests` total 26 with 0 failed, solution 2850 with 0 failed, plants 7-9 each exit non-zero with their expected failure.
- ✅ Phase 3 group 2: tasks 3.6-3.8 (`GlfwUiInput` focus handler, `GlfwUiInputFocusTests`, HelloPanel held-key text). `GlfwUiInputFocusTests` total 5 with 0 failed, solution 2855 with 0 failed, plant 10 exits 2 with its expected failure.
- ✅ Phase 4: tasks 4.1-4.4 (`UiGamepadState`, 16 slots, `SetGamepad`, 6 tests, plants 11-12). `InputStateTests` total 32 with 0 failed, solution 2861 with 0 failed, `Unsafe.SizeOf<UiInputState>()` measures 804 bytes (A1 bound 1024).
- ✅ Phase 5 group 1: tasks 5.1-5.2 (`InputStateAllocationTests`, the nineteenth gate, 3 tests, 0 bytes). Plants 13-16 each exit 2 with `0 Error(s)` and fail their expected test with a non-zero measured byte count. Solution 2864 with 0 failed.
- ✅ Phase 5 group 2: tasks 5.3-5.6 (technical-design sections 9 and 14, the changelog, and the final runs). Demo totals 32, 5, and 3 with 0 failed. The ASD-STE100 lint passes on the four named files (1.55, 1.58, 2.0, and 1.96 per 100 words).

**What's Next:**
- ⏳ One person run of `scripts/test-linux.sh` outside the agent sandbox, and the HelloPanel window check.
- ⏳ The phase 5 verifier run, then `/update-feature` and `/evaluate-feature`.

**Blockers:**
- None.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Tree/UiContext.cs` - one field, two constructor lines, two properties, two methods, three hooks in `DrainEvents`.
- `src/BrutalUi.Core/Input/UiInputState.cs` - new: the state struct and its parts.
- `src/BrutalUi.Core/Input/InputStateWriter.cs` - new: the internal writer struct.
- `src/BrutalUi.Core/Input/UiGamepadState.cs` - new in phase 4: the gamepad slot type.
- `src/BrutalUi.Glfw/GlfwUiInput.cs` - phase 3: the focus handler and the `KeyboardEnabled` setter.

### Related Files
- `docs/features/input/input-state/implementation.md` - the plan. Each phase's full task text lives there. The briefs copy it word for word.
- `docs/features/input/epic-overview.md` - the epic's build order and decisions.

---

## Base State

- **Base commit:** `aec0341f0a4f2116b58ff3bfda6499124ebb8520` (feat/input/input-state, holds panel-sample).
- `git status --porcelain` at the start of task 1.1: clean except the four untracked feature docs. The panel-sample changes of HelloPanel are committed, so gap 6 is closed.
- Test counts at the base commit (Debug):
  - Solution: total 2824, failed 0, succeeded 2821, skipped 3.
  - `BrutalUi.Core.Tests`: total 1751, failed 0, skipped 0.
  - `BrutalUi.Glfw.Tests`: total 27, failed 0, skipped 3 (the three display-only tests).
- Build at the base commit: `0 Warning(s)`, `0 Error(s)`.
- End-state targets (Definition of Done): Core 1751 + 35 = 1786, Glfw 27 + 5 = 32, solution 2824 + 40 = 2864.

## Phase 1 Internal Shape, Integration Points, And Hook Places

Recorded from the plan (task 1.1). The plan's "Architecture Overview" stays the authority.

- **Internal Shape.** `internal struct InputStateWriter` in `src/BrutalUi.Core/Input/InputStateWriter.cs`: fields `Raw`, `Game` (both `UiInputState`), `_startPosition` (`float2`). Phase 1 keeps these only. `_uiPresses`, `_uiReleases`, and `_clearKeys` arrive with phases 2 and 3. `Create()` sets both positions and `_startPosition` to (NaN, NaN). `BeginFrame()` keeps the start position and clears the delta. `Record(in UiQueuedEvent entry, bool hitUi)` writes the pointer position for `PointerMove`, `PointerDown`, `PointerUp`, and the `Scroll` position. It reads no `hitUi` in phase 1. `EndFrame(UiRoutingGate gate)` writes the delta and copies `Raw` into `Game`, then empties the pointer part of `Game` when `gate.PointerOverUi` is `true`. The parts are structs with private fields and internal write methods: `UiPointerState` gets `Move(float2)` and `FinishFrame(float2)` in phase 1. `UiInputState` gives `Pointer` through a `[UnscopedRef] public readonly ref readonly` member.
- **Integration Points.** input-routing: the three hooks add writes only. The target step, the walk, `FinishRelease`, the Tab move, and the unhandled input step keep their order and result. routing-gate: hook 3 reads the same `RoutingGate` value that the game reads after `Update`. mode-set, state-and-commands, `GlfwUiInput`, `UiActionMap`, `UiUnhandledInputHandler`: no change in phase 1. Tests: `PointerTree` with a unique prefix. UI point (50, 50) on `Left`, game point (600, 400). Read state after `Update` only.
- **The three hook places in `DrainEvents`.**
  1. Hook 1 `_input.BeginFrame();` — after `DropStaleFocus()` and before the read of `_events.Count`.
  2. Hook 2 `_input.Record(in entry, target is not null);` — right after the target step (the `if`/`else if` that set `target`) and before the press target step.
  3. Hook 3 `_input.EndFrame(RoutingGate);` — in the `finally` block, after `FinishRelease()`.

## Phase 1 Group 1 Results (tasks 1.5 and 1.6)

- Build: `0 Warning(s)`, `0 Error(s)`, after the group and after each plant restore.
- Solution tests: total 2832 (base 2824 + 8), failed 0, skipped 3 (the display-only Glfw tests).
- `BrutalUi.Core.Tests`: total 1759 (base 1751 + 8), failed 0. `BrutalUi.Glfw.Tests`: total 27, failed 0, skipped 3.
- Class run: `--filter-class '*InputStateTests'` gives total 8, failed 0.
- Flake, per the plan rule: the first full-solution run after the new tests reported one failed Core test (the tail did not name it). The standalone Core run right after gave 0 failed, and the full run a second time gave 0 failed. Treated as the known flake. No test was edited.
- Plants (scratch and logs: `src/BrutalUi.Core/obj/plantscratch/`, git-ignored):
  - Plant 1: removed hook 3 from `UiContext.cs`. Exit 2, `0 Error(s)`. Failed: `TheDeltaIsThePositionOfThisUpdateMinusThePositionOfTheLastUpdate` (expected), plus `TheGameViewEqualsTheRawPointerOverTheGame`, `TheGameViewIsEmptyWhenThePointerIsOverTheUi`, and `TheGateOfTheGameViewComesFromTheEndOfTheDrain` (hook 3 also writes `Game`). Restored. The diff against the planted copy shows exactly the three re-added lines.
  - Plant 2: passed `default(UiRoutingGate)` to hook 3. Exit 2, `0 Error(s)`. Failed: `TheGameViewIsEmptyWhenThePointerIsOverTheUi` (expected), plus `TheGateOfTheGameViewComesFromTheEndOfTheDrain`. Restored. `cmp` identical to the copy.
  - Plant 3: removed the NaN check of the delta in `UiInputState.cs` (and the now-unused `IsFinite` helper). Exit 2, `0 Error(s)`. Failed: `APointerAtNaNGivesAZeroDelta` (expected), plus the four other delta-sensitive tests. Restored. `cmp` identical to the copy.
- `git diff` check: in `DrainEvents`, `UiContext.cs` adds lines only at the three hook places (comment plus call) and one doc paragraph. It changes no existing code line. The one removed line is the `<para>` of the `RoutingGate` remarks that task 1.4 edits by adding the `GameInput` sentence.
- `scripts/test-linux.sh`: steps preflight, build, test, gpu, and loader pass. The smoke step fails inside the agent's bash sandbox after two runs: `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109)`. Probe: a plain `xvfb-run` client cannot reach `/tmp/.X11-unix/X<display>` inside the sandbox (FileNotFoundError), so the X server and the client see different `/tmp` views. The failure is the sandbox, not this diff. Needs one run outside the sandbox (open question below).

## Phase 1 Group 2 Results (tasks 1.7-1.10)

- Files changed: `samples/HelloPanel/HelloScreen.cs`, `samples/HelloPanel/HelloModes.cs`, `samples/HelloPanel/Program.cs`. No file under `src/` changed.
- `BuildWidgetPanel` now takes `out Label gateLabel, out Label inputLabel, out HelloWidgets widgets`. The input label sits in the tree right after the gate label, is not pickable, and starts with `LongestInputText`.
- `InputText(in UiInputState game, Span<byte> scratch)`: writes `Game pointer delta: <x>, <y>`, returns the written part, and follows the `HelloWidgets.WriteCount` pattern (`TryCopyTo` for the two literals, `int.TryFormat` with `CultureInfo.InvariantCulture`, and one `InvalidOperationException` per failed write).
- Build: `0 Warning(s)`, `0 Error(s)`.
- Solution tests: total 2832, failed 0, succeeded 2829, skipped 3 (the display-only Glfw tests). `BrutalUi.Core.Tests`: 1759 with 0 failed, so `InputStateTests` keeps its 8 passes. No test changed.
- `scripts/test-linux.sh` run with `MSBUILDDISABLENODEREUSE=1` (task form adds `echo "exit=$?"`): exit 1, `test-linux: FAIL at smoke (exit 1)`, `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109)`. The same sandbox blocker as group 1: `/tmp/.X11-unix` does not exist for the client process here (probe in this session: `ls /tmp/.X11-unix/` → No such file or directory). Steps preflight, build (0 Error), test (2829 succeeded), and gpu all passed in that run. This agent session has approvals disabled, so the wide-access retry that the brief allows is not available here. The run still needs one pass outside the sandbox.
- No display exists on this host (`DISPLAY` unset), so the demo check is open: see Needs Human Verification.
- Buffer proof: the longest input text is 32 bytes (measured with `printf`), and `Utf8Text.InitialTextCapacity` is 32 with `EnsureCapacity` check `needed <= buffer.Length` (no terminator), so the label buffer never grows. `Program.cs` passes a 64-byte scratch, which always holds one input text.

## Phase 2 Results (tasks 2.1-2.4)

- Files changed: `src/BrutalUi.Core/Input/UiInputState.cs` (`UiPointerButtonState`, `Buttons`, `ScrollDelta`, `Scroll`, part-level `BeginFrame`), `src/BrutalUi.Core/Input/InputStateWriter.cs` (`_uiPresses`, `_uiReleases`, button cases, scroll sum, button gate, ownership mask), `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs` (10 new tests, `TargetedDown`, `GamePoint`). `UiContext.cs` did not change.
- Demo: `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests'` reads total 18, failed 0, succeeded 18, skipped 0.
- Build: `0 Warning(s)`, `0 Error(s)`, after each change and after each plant restore.
- Solution tests: total 2842 (base 2824 + 18), failed 0, succeeded 2839, skipped 3 (the display-only Glfw tests). All 8 existing `InputStateTests` pass with no edit. Every other existing test passes with no edit.
- Plants (scratch: `src/BrutalUi.Core/obj/plantscratch/`, git-ignored. Each run checked `0 Error(s)` before the exit code):
  - Plant 4: removed the `_uiReleases` mask from `EndFrame` (`Mask(_uiPresses, 0)`). Exit 2. Failed: `APressOverTheUiThatEndsOverTheGameGivesNoReleaseInTheGameView` (expected), plus `ANewPressOverTheGameTakesTheButtonBack` (same rule: its game view must hide a UI-press release). Restored. `cmp` identical to the copy.
  - Plant 5: skipped the button gate (removed `Game.ButtonState = default;` from the `PointerOverUi` branch). Exit 2. Failed: `TheButtonAndScrollPartsOfTheGameViewAreEmptyOverTheUi` (expected) only. Restored. `cmp` identical.
  - Plant 6: wrote the scroll with `=` in place of `+=`. The `+=` lives in `UiPointerState.Scroll` in `UiInputState.cs` (the writer calls it), so this plant edits that file, not `InputStateWriter.cs` as the brief words it. Exit 2. Failed: `TheScrollDeltaIsTheSumOfTheFrame` (expected) only. Restored. `cmp` identical.
- No flake run was needed (exit 134 and `TheCurvesAllocateNothing` did not appear).
- Notes: the 10 new tests push out-of-range buttons through the real drain. `PressButton` and `ReleaseButton` in `UiContext` guard the cast, so `(PointerButton)8` and `(PointerButton)(-1)` raise no exception.

## Phase 3 Group 1 Results (tasks 3.1-3.5)

- Files changed: `src/BrutalUi.Core/Input/UiInputState.cs` (`KeyBits`, `UiKeyState` with the three `KeyBits` sets and `_modifiers`, the three queries, the internal `KeyDown`/`KeyUp`/`BeginFrame`/`ReleaseAll`, the `Keys` member), `src/BrutalUi.Core/Input/InputStateWriter.cs` (the key cases of `Record`, `RequestKeyClear`, the key clear and key gate of `EndFrame`, `Raw.KeyState.BeginFrame()` in hook 1), `src/BrutalUi.Core/Tree/UiContext.cs` (`ClearHeldKeys` with the remarks, the only change of this group in that file), `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs` (8 new tests, `W`/`A` constants), `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` (`Key` gains an optional `repeat` parameter, Decision 15).
- `UiModifiers` check (brief line 29): the type is `public enum UiModifiers` with `[Flags]` in `src/BrutalUi.Core/Input/UiEventEnums.cs`, members `None = 0`, `Shift`, `Control`, `Alt`, `Super`, `CapsLock`, `NumLock`. Used as-is. `None` is the zero value.
- Demo: `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests'` reads total 26, failed 0, succeeded 26, skipped 0.
- Build: `0 Warning(s)`, `0 Error(s)`, after each change and after each plant restore.
- Solution tests: total 2850 (base 2824 + 26), failed 0, succeeded 2847, skipped 3 (the display-only Glfw tests). All 18 existing `InputStateTests` pass with no edit. Every other existing test passes with no edit.
- Test-host note for the next groups: `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln -nodeReuse:false` failed two runs in a row with "Zero tests ran", exit code 5 on every assembly (handshake failures, no test ran). The brief's exact run command has no `-nodeReuse:false` on `dotnet test`. That run passed. Do not pass `-nodeReuse:false` to `dotnet test`, only to `dotnet build`.
- Plants (scratch and logs: `src/BrutalUi.Core/obj/plantscratch/`, git-ignored. Each run checked `0 Error(s)` before the exit code):
  - Plant 7: `KeyDown` in `UiInputState.cs` sets the pressed bit also for a repeat (removed the `if (!repeat)` guard). Exit 2. Failed: `ARepeatSetsNoPressedFlag` (expected) only. Restored. `cmp` identical to the copy.
  - Plant 8: removed the key clear line `Raw.KeyState.ReleaseAll();` from `EndFrame` in `InputStateWriter.cs`, kept the flag clear so the field is still read and the build stays clean (Decision 16). Exit 2. Failed: `ClearHeldKeysReleasesEachHeldKeyAtTheNextUpdate` (expected), plus `ClearHeldKeysClearsAKeyDownOfTheSameFrame` (same rule: its released flag and cleared down flag also come from the key clear). Restored. `cmp` identical.
  - Plant 9: skipped the key gate (removed `if (gate.KeyboardFocusInUi) { Game.KeyState = default. }` from `EndFrame`). Exit 2. Failed: `TheKeyPartOfTheGameViewIsEmptyWhenTheUiHoldsTheFocus` (expected) only. Restored. `cmp` identical.
- `UiContext.cs` diff of this group: adds `ClearHeldKeys` with its remarks and nothing else. Hook 2 and hook 3 call sites did not change.

## Phase 3 Group 2 Results (tasks 3.6-3.8)

- Files changed: `src/BrutalUi.Glfw/GlfwUiInput.cs` (the `_onFocus` field, the subscribe line after `OnChar`, the unsubscribe line in `Dispose`, `internal void PushFocus(bool focused)` with remarks, `HandleFocus`, the `KeyboardEnabled` backing field with the switch-only clear and the new remarks paragraph, and the class remarks that now list nine events), `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs` (one added line in `EventNames`), `tests/BrutalUi.Glfw.Tests/GlfwUiInputFocusTests.cs` (new, 5 tests), `samples/HelloPanel/HelloScreen.cs` (the held-key part of `InputText`, `HeldKeyPrefix`, `HeldKeyNone`, `HeldKeyAsciiFirst`/`HeldKeyAsciiLast`, `LowestHeldKey`, and the grown `LongestInputText`). `samples/HelloPanel/Program.cs` did not change: the 64-byte scratch still holds each text, and the brief limits that file to a too-small buffer.
- **Focus callback signature (task 3.6).** `GlfwWindowFocusCallback` is `void (GlfwWindow window, GlfwFocusEvent focusEvent)`. The plan guessed `bool`. The build gave `error CS0123: No overload for 'HandleFocus' matches delegate 'GlfwWindowFocusCallback'`, which names the delegate but not the parameter type, so the type came from the assembly metadata and the package XML docs (`window`, `focusEvent`, "Enumerator describing the focus event"). `GlfwFocusEvent` members `GainFocus` and `LostFocus` live in the assembly. `HandleFocus` maps `GlfwFocusEvent.GainFocus` to `true` and every other value to `false`, and the mapping compiles with `0 Warning(s)`. The `bool` handler shape of the brief was wrong. Decision 19 records the adapter rule.
- Demo: `tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*GlfwUiInputFocusTests'` reads total 5, failed 0, succeeded 5, skipped 0. `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests'` keeps total 26, failed 0.
- Build: `0 Warning(s)`, `0 Error(s)`, after each change, after the plant restore, and inside `scripts/test-linux.sh` (its `artifacts/test-linux/build.log`).
- Solution tests: total 2855 (base 2824 + 31: 26 Core input tests + 5 Glfw focus tests), failed 0, succeeded 2852, skipped 3 (the display-only Glfw tests). `BrutalUi.Glfw.Tests` reads 32 (base 27 + 5). `GlfwUiInputTests` keeps its count and passes. Every other existing test passes with no edit.
- `git diff` check: `GlfwUiInputTests.cs` shows exactly one added line, `nameof(GlfwWindow.OnFocus)`, inside `EventNames`. The `ARealWindowSubscribesAndUnsubscribes` test (skipped with no display) now counts nine handlers on a real window.
- Plant 10 (scratch and log: `src/BrutalUi.Glfw/obj/plantscratch/`, git-ignored): the `KeyboardEnabled` setter dropped the same-value guard and the `!value` condition, so it called `Context.ClearHeldKeys()` for each write (`false`, `false` to `false`, and `true`). `grep error CS` of the run log: 0, so the plant compiled. Exit 2. Failed: `GlfwUiInputFocusTests.OnlyTheSwitchFromTrueToFalseClears` (expected. `Assert.True() Failure` at the same-value `true` write), 1 of 2855. Restored with `cp`. `cmp` says the file is identical to the copy. The rebuild and the focus class run (5, failed 0) confirm the restore.
- HelloPanel text (task 3.8): `. Held key: <key>` with no trailing period (Decision 20). The lowest code of 0 to 348 that `game.Keys.IsDown` gives. A code from 32 to 96 writes as one ASCII byte, another code writes with `int.TryFormat` (invariant culture), and `none` writes when no key is down. Measured longest text: `Game pointer delta: -9999, -9999. Held key: none` (48 bytes. The `none` part, 4 bytes, beats the longest code part, 3 bytes). `LongestInputText` holds it, and the 64-byte scratch in `Program.cs` always holds one input text (Decision 9 of group 1 re-checked: the label buffer and the scratch stay big enough). `LowestHeldKey` loops 349 `IsDown` reads per frame. Each read is a `ref readonly` bit test, so the loop allocates nothing.
- `scripts/test-linux.sh`: one run, exit 1, `test-linux: FAIL at smoke (exit 1)`. The exact smoke line: `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` The steps before it passed in the same run: preflight, build (`0 Error(s)`), test (total 2855, failed 0), gpu (`check-gpu: OK`). The brief allows one retry with the widest access. This session has approvals disabled and forbids the escalation flag, so the retry cannot run here. The known sandbox blocker (Gotcha 3), not a code fault: the PASS run stays open outside the sandbox.
- Demo check with a display: no display on this host (`DISPLAY` unset. The three skipped Glfw tests report `The machine has no display.`). The window check stays open in Needs Human Verification.
- Stale doc counts that this group could not touch: the `EventNames` doc comment in `GlfwUiInputTests.cs` and the summary of `ARealWindowSubscribesAndUnsubscribes` say "eight" (the brief fixes that file at one added line), and the scratch comment in `Program.cs` says "needs 32 bytes" (the brief limits that file to a buffer-size change). Open question for `/update-feature`: fix the three counts (Decision 21).

## Phase 4 Results (tasks 4.1-4.4)

- Files changed: `src/BrutalUi.Core/Input/UiGamepadState.cs` (new: `GamepadAxes` = `[InlineArray(6)]` of `float` with the `AxisCount = 6` const, `UiGamepadState` = 40 bytes, the public constructor, `Connected`, `Buttons`, `Pressed`, `Released`, `Axis(int)`, and the internal `WriteEdgesInto`), `src/BrutalUi.Core/Input/UiInputState.cs` (`GamepadCount = GamepadSlots.SlotCount`, `internal GamepadSlots GamepadState` with `[InlineArray(16)] internal struct GamepadSlots`, the `[UnscopedRef] public readonly ref readonly UiGamepadState Gamepad(int index)` member with the 0 to 15 check, and the grown class remarks), `src/BrutalUi.Core/Input/InputStateWriter.cs` (`SetGamepad(int, in UiGamepadState)` with the edge rule, and the BeginFrame and EndFrame remarks now name the slots), `src/BrutalUi.Core/Tree/UiContext.cs` (only `SetGamepad` with its remarks, after `ClearHeldKeys`), `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs` (6 tests, `AxisValues`, `AssertDisconnectedGamepad`, and the grown class summary), and `context.md`.
- Measured size: `Unsafe.SizeOf<UiInputState>()` = 804 bytes (A1 allows at most 1024. The plan estimated "about 800"). `Unsafe.SizeOf<UiGamepadState>()` = 40 bytes, exactly as the plan states. The measure ran through a temporary failing test that the task removed before the final gates. The committed test `TheStateStaysNearOneKilobyte` asserts the bound.
- Demo: `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests'` reads total 32, failed 0, succeeded 32, skipped 0.
- Build: `0 Warning(s)`, `0 Error(s)`, after each change and after each plant restore.
- Solution tests: total 2861 (base 2824 + 37, and 2855 + the 6 new tests), failed 0, succeeded 2858, skipped 3 (the display-only Glfw tests). All 26 existing `InputStateTests` pass with no edit. Every other existing test passes with no edit.
- Flake, per the plan rule: the first full-solution run after the final build died with `Fatal error. Internal CLR error. (0x80131506)` (the known host flake). The rerun gave total 2861 with 0 failed. No test was edited.
- Plants (scratch and logs: `src/BrutalUi.Core/obj/plantscratch/`, git-ignored. Each run checked `0 Error(s)` in the build log before the exit code):
  - Plant 11: `BeginFrame` clears the pressed edge of each slot, written with `new UiGamepadState(slot.Buttons, default).WriteEdgesInto(ref slot)` (Decision 25: the empty axes span is a plant-only side effect). Exit 2. Failed: `TheDrainKeepsTheGamepadEdges` (expected), plus `TheGameViewCopiesTheGamepadsWithNoGate` (the same clear runs before the copy, so the game view loses the pressed edge too). Restored. `cmp` identical to the copy.
  - Plant 12: `EndFrame` writes `default` into the game slots after the copy. Exit 2. Failed: `TheGameViewCopiesTheGamepadsWithNoGate` (expected), plus `TheDrainKeepsTheGamepadEdges` (its last check reads `GameInput.Gamepad(3).Pressed`, and the zeroed game slot fails it). Restored. `cmp` identical to the copy.
- `git diff` check: `UiContext.cs` adds `SetGamepad` with its remarks and nothing else. The three hook call sites and `BeginFrame` body did not change. `BeginFrame` touches no slot (Decision 11), and `EndFrame` needed no new line for the slots.
- Tooling fact (fixed the brief's hint): `Unsafe` lives in `System.Runtime.CompilerServices`, not in `System.Runtime.InteropServices`. `UiInputState.cs` already imported the right namespace, and the test file gained `using System.Runtime.CompilerServices;`. The brief's own check ("check which the existing `InlineArray` files use") settles it.
- Caught during 4.3: the first `SetGamepad` draft forwarded the index without the range check, so `SetGamepad(16, ...)` wrote past the slot array with no throw. `SetGamepadWritesTheSlotAndTheEdges` caught it, and the check now lives in `UiContext.SetGamepad` as the brief binds it. The fix landed before the phase ends, so no committed API shipped without the guard.

## Phase 5 Group 1 Results (tasks 5.1 and 5.2)

- Files changed: `tests/BrutalUi.Core.Tests/Input/InputStateAllocationTests.cs` (new, 3 tests), `docs/features/input/input-state/tasks.md`, and `context.md`. `src/` changed only through plants 13-16, then restored.
- Class remarks name the new class as the nineteenth allocation gate (the eighteenth is `ModeDiffAllocationTests`).
- Scene: `ThousandElementTree` after one `Update`. Slot point: grid slot 0 at its viewport rect corner plus (1, 1) - the pattern of `PointerAllocationTests`. Outside point: `ViewportWideWidth + 10` at mid height, outside the root, so it hits nothing and the gate stays open. One step: the nine events of the brief, `SetGamepad(0, ...)` with one of two hand-made states, `ClearHeldKeys` on even steps, and `Update(0.016f)`. Then the reads of both views into a local `int` sum.
- Gate run: `--filter-class '*InputStateAllocationTests'` gives total 3, failed 0, on the final build and on three more runs. `TheInputPassWithTheStateAllocatesNothingAfterWarmUp` (2 warm-up rounds, release, 100 steps) and `TheReadsOfTheStateAllocateNothing` (349 keys, 8 buttons, 16 slots with six axes, both views, 100 rounds) measure 0 bytes. `TheMeasuredBodyDoesRealWork` checks the raw and game flags, the press ownership in the game view, the key clear of a request in the same frame, the gamepad edges across the two states, the scroll sum, and the fixed sums 10 (step 0) and 16 (step 1).
- Build: `0 Warning(s)`, `0 Error(s)`, after each change and after each plant restore.
- Solution tests: total 2864 (base 2824 + 40), failed 0, succeeded 2861, skipped 3 (the display-only Glfw tests). `InputStateTests` keeps total 32 with 0 failed, also on two extra runs for the inbox note. No flake appeared.
- Measured bytes per plant (x64 `new object()` costs 24 bytes, so each count divides by 24 exactly):
  - Plant 13 (`GC.KeepAlive(new object());` first line of `InputStateWriter.BeginFrame`, with the `using System;` line the file needs): exit 2, `0 Error(s)`, failed 41. `TheInputPassWithTheStateAllocatesNothingAfterWarmUp` failed with 2400 bytes (100 steps x one drain x 24). Every gate that runs a drain in the measured region failed: `PointerAllocationTests.PointerRoutingAllocatesNothingAfterWarmUp` (4800: its step drains twice), all four `ThousandElementAllocationTests` gates (2400), and the update gates of `Core.Tests` (27), `Framework.Tests` (12), the ImGui renderer gate (1), and the Markup parsed-sheet gate (1). `TheReadsOfTheStateAllocateNothing` passed: its measure reads only. Full list: `obj/plantscratch/p13-names.txt`.
  - Plant 14 (the line in the `KeyDown` case of `Record`, with `using System;`): exit 2, `0 Error(s)`, failed 10. `TheInputPassWithTheStateAllocatesNothingAfterWarmUp` failed with 7200 bytes (3 key downs per step x 100 x 24). The nine other failures are exactly the gates that drain key events in the measured region: `FocusAllocationTests.KeyboardRoutingAllocatesNothingAfterWarmUp`, `EventAllocationTests.PushAndDrainAllocateNothingAfterWarmUp`, the four input-path tests of the widget, slider, text-field, and thousand-widget gates, `ThousandElementAllocationTests.UpdateDrainsAFullQueueWithNoAllocation`, and the two `ActionMapAllocationTests` key-path tests.
  - Plant 15 (the line first in `EndFrame`, with `using System;`): exit 2, `0 Error(s)`, failed 41 - the identical set of plant 13 (`diff` empty). `TheInputPassWithTheStateAllocatesNothingAfterWarmUp` failed with 2400 bytes.
  - Plant 16 (the line first in `UiKeyState.IsDown`, block body, Decision 28): exit 2, `0 Error(s)`, failed 2. `TheReadsOfTheStateAllocateNothing` failed with 1675200 bytes (349 codes x both views x 100 rounds x 24). `TheInputPassWithTheStateAllocatesNothingAfterWarmUp` failed too, with 4800 bytes: its step body reads `IsDown(W)` on both views (2 x 100 x 24). The other 2862 tests, value tests included, passed.
- Restore: each plant restored with `cp` from `src/BrutalUi.Core/obj/plantscratch/*.orig` (git-ignored). `cmp` reports the file identical to the copy after plants 13, 14, 15 (`InputStateWriter.cs`) and plant 16 (`UiInputState.cs`). The rebuild after the restores printed `0 Warning(s)`, `0 Error(s)`, and the final full run and the two class runs above ran on the restored tree.
- Acceptance of the group: the 3 tests pass with 0 bytes. The four plants give exit 2 with a non-zero measured value. `InputStateTests` keeps its 32. The existing tests keep passing.

## Phase 5 Group 2 Results (tasks 5.3-5.6)

- Files changed: `docs/technical-design.md`, `CHANGELOG.md`, `tasks.md`, and this file. No change under `src/`, `samples/`, `tests/`, or `scripts/`.
- Task 5.3, section 9: item 1 lists the focus event among the window events, and adds the sentence that a focus loss calls `UiContext.ClearHeldKeys`. Item 7, the bullet "A switch from `true` to `false`", now names the held-key clear of `UiContext.Input` and keeps the rule that an element gets no key up. New item 11 "The input state" states the two views (`UiContext.Input` and `UiContext.GameInput`, Q1), the parts and their sizes (804 bytes total: 24, 3, 136, and 16 slots of 40), the frame rule with the three hooks, the position rule and the NaN rule, the scroll sum, the button rule and the ownership rule, the key rule, the repeat rule and the modifier rule, the gate rule (A4), the clear rule, the gamepad seam, and the 0-byte rule. One closing sentence names `InputStateTests`, `GlfwUiInputFocusTests`, and the gate.
- Task 5.4, section 14: two bullets sit before the bullet "Sample projects". The bullet **Input state tests.** names `InputStateTests` (32), `GlfwUiInputFocusTests` (5), and `InputStateAllocationTests` (3), with the plants of tasks 1.6, 2.4, 3.5, 3.8, and 4.4 and their results. The bullet **A nineteenth allocation gate.** names `InputStateAllocationTests`, its scene, its step, and plants 13 to 16 of task 5.2 with the failed gates and the bytes. Ordinal check: the gate bullets of the section end at **An eighteenth allocation gate.** (`ModeDiffAllocationTests`), so "nineteenth" is right. The plan guessed it, and the count confirms it. The HelloPanel UI-region line now reads "the gate read, and the two label updates" (the follow-up of Migration Risks, applied in this task).
- Task 5.5, `CHANGELOG.md`: three entries under **Added** (`UiInputState` with `UiContext.Input` and `UiContext.GameInput`, then `UiContext.ClearHeldKeys`, then `UiGamepadState` with `UiContext.SetGamepad`) and one entry under **Changed** (the `GlfwUiInput` focus subscription and the `KeyboardEnabled` clear). Each entry ends with the tag (input/input-state).
- Task 5.6, script run: `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` gives exit 1 and the line `test-linux: FAIL at smoke (exit 1)`. The exact smoke line: `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` Steps preflight, build (`0 Warning(s)`, `0 Error(s)`), test (total 2864, succeeded 2861, failed 0, skipped 3), gpu, and loader pass. The brief allows one retry with the widest sandbox access. This session has approvals disabled, and the harness forbids the escalation flag here, so the retry cannot run. Record the blocker as the known Gotcha 3, not a code failure. The PASS run stays open outside the sandbox.
- Task 5.6, demo totals on the final build: `InputStateTests` total 32 with failed 0. `GlfwUiInputFocusTests` total 5 with failed 0. `InputStateAllocationTests` total 3 with failed 0.
- Task 5.6, ASD-STE100 lint (`ste-lint.py --fail-over 2.5`): `technical-design.md` 1.55, `CHANGELOG.md` 1.58, this file 2.0, and `implementation.md` 1.96 per 100 words. All four pass. The first run of this file gave 3.44. Decision 31 records the prose fix.
- Spelling note for `/update-epic` (Notes, gap 1 of the plan): Q1 changed `ui.Input.Game` to `ui.GameInput`. The old spelling stays in three places: `docs/features/input/epic-overview.md` (Integration & Architecture, "Within the epic"), `docs/features/input/input-state/requirements.md` (line 17), and `docs/features/input/input-actions/requirements.md` (lines 16 and 27). Run `/update-epic input` after this feature.
- The stale "eight" counts in `GlfwUiInputTests.cs` and the "needs 32 bytes" comment in `Program.cs` stay open (Decision 21). This group cannot touch those files (the brief's scope rule).
- Acceptance of the group: sections 9 and 14 hold the new items and bullets. `CHANGELOG.md` names the six new public members and the adapter change. The demo totals read 32, 5, and 3 with 0 failed. The lint passes on the four files. The script keeps the known sandbox blocker, recorded here.

## Review Group 2 Results (CR-3, CR-4)

- Files changed: `tests/BrutalUi.Core.Tests/Input/InputStateTests.cs` (two asserts added to `TheConstructorCopiesUpToSixAxes`), `tests/BrutalUi.Glfw.Tests/GlfwUiInputFocusTests.cs` (the setter phase added to `DisposeStopsTheFocusClear`), and this file. No new test method. No new file. The method counts stay 32 and 5.
- CR-3: `TheConstructorCopiesUpToSixAxes` now calls `Assert.Throws<ArgumentOutOfRangeException>` for `eight.Axis(-1)` and `eight.Axis(6)` on the connected slot. The test pins the documented 0 to 5 range guard of `UiGamepadState.Axis`.
- CR-4: `DisposeStopsTheFocusClear` now writes `KeyboardEnabled = false` after `Dispose` (the switch from the prior `true`), attempts a fresh W down, runs one `Update`, and asserts W not down and released. The setter path still clears after `Dispose`; the existing asserts still pin that `PushFocus` stops. The fresh push is a no-op in the adapter: `PushKey` returns after `Dispose`, and with the value `false`. The clear releases the held W of the earlier down at the end of the next drain. The run matches the `GlfwUiInput` remarks, so the finding found no contradiction.
- Gates: build 0 `Warning(s)` and 0 `Error(s)`. Solution total 2864, failed 0, succeeded 2861, skipped 3 (the display-only Glfw tests). Class runs on the built binaries: `InputStateTests` total 32 with failed 0, `GlfwUiInputFocusTests` total 5 with failed 0. No flake run was needed.

## Review Group 3 Results (CR-5, CR-6, CR-7)

- Files changed: comment-only edits in `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs` (two doc-comment lines), `samples/HelloPanel/Program.cs` (one comment line), `samples/HelloPanel/HelloScreen.cs` (one comment line), and `src/BrutalUi.Core/Tree/UiContext.cs` (the `GameInput` remarks paragraph). Plus this file. No behavior line changed.
- CR-5: The `EventNames` summary and the `ARealWindowSubscribesAndUnsubscribes` summary now read "nine". The scratch comment in `Program.cs` now reads "needs 48 bytes". Decision 21 closes with this group.
- CR-6: The inline comment in `BuildWidgetPanel` now reads "The three labels come before the image". It matches the tree, the class remarks, and its own second clause.
- CR-7: The `GameInput` pointer paragraph now names the scroll (0, 0) and the zero button masks over the UI. It adds the key half: the key part is empty when `KeyboardFocusInUi` was true, with no key bits and the `None` modifiers. It states that the gamepad slots get no gate.
- Gates: build 0 `Warning(s)` and 0 `Error(s)`. Solution total 2864, failed 0, succeeded 2861, skipped 3. No rerun was needed.

---

## Important Decisions

The plan holds Decisions 1-14 and Confirmed Assumptions A1-A9 and Q1-Q3 in `implementation.md` (Key Technical Decisions). This file records only decisions made during the build that the plan did not settle.

1. **Part field name.** The plan names the `ref readonly` members but not the internal part fields. `UiInputState` holds `internal UiPointerState PointerState;`. Phases 2 to 4 add `ButtonState`, `KeyState`, and the gamepad slots with the same pattern.
2. **The delta clear of `BeginFrame`.** Task 1.2 names only `Move` and `FinishFrame` as the phase-1 write methods, so `BeginFrame` clears the delta with `Raw.PointerState.FinishFrame(_startPosition)`: the start equals the position, and the delta rule gives (0, 0). Phase 2 may replace this with a part-level `BeginFrame` when it adds the button clear.
3. **The empty pointer part.** The plan pseudocode writes `Game.Pointer = empty`. Phase 1 composes the empty part with `Move((NaN, NaN))` and `FinishFrame((NaN, NaN))`, which gives the position (NaN, NaN) and the delta (0, 0) with the two named methods only.
4. **No `[UnscopedRef]` fallback.** Risk 3 did not materialize: the `[UnscopedRef] public readonly ref readonly` member and the `ref readonly` properties of `UiContext` compile on `net10.0` with no error.
5. **The clamp order of `InputText` (group 2).** Task 1.7 says "rounded to an `int` and clamped to -9999 to 9999". The helper `HelloScreen.DeltaComponent` clamps the `float` to -9999 to 9999 first, then casts the rounded value, so a delta too large for an `int` still gives a clamped result instead of an undefined cast. A component that is not finite gives 0. Phase 1 never writes one, so the check only guards a later change.
6. **The rounding rule of `InputText` (group 2).** `(int)MathF.Round(value)` uses `MidpointRounding.ToEven` at a midpoint. `HelloWidgets.WriteCount` uses the same call for the slider value, so the label matches the sample style.
7. **The out-parameter order (group 2).** The plan names the second out parameter but not its place. `BuildWidgetPanel` takes `out Label gateLabel, out Label inputLabel, out HelloWidgets widgets`, so the two labels sit together and the widgets keep the last place. `HelloModes.CreateWidgetPanel` and the `RunFrames` argument list follow the same order.
8. **Doc truth over the minimal diff (group 2).** The brief names only the remarks of `InputText` and `BuildWidgetPanel`. The third label also made the class remarks of `HelloScreen` false ("the two labels"), so group 2 updated those sentences too, and the `HelloModes` constructor exception text, the factory summary, and the `RunFrames` remarks that list the UI region.
9. **The scratch size of the sample (group 2).** Task 1.9 fixes `byte[] inputScratch = new byte[64]`. The longest input text needs 32 bytes, so the buffer holds each text with room to spare, and no code path can ask for a bigger one. Phase 3 adds the held-key text to the same method. The group records this so phase 3 can re-check the 64 bytes and the longest text of the label.
10. **The frame clear of the pointer part (phase 2).** Phase 1 Decision 2 cleared the delta with `FinishFrame(_startPosition)`, because task 1.2 named no clear method. Phase 2 adds `internal void BeginFrame()` to `UiPointerState`, which clears the delta and the scroll sum and keeps the position. The writer hook 1 calls it, then `Raw.ButtonState.BeginFrame()`, then clears `_uiReleases`. The brief left this replacement open. The phase took it.
11. **The empty pointer part, grown (phase 2).** Phase 1 Decision 3 composed the empty part with `Move((NaN, NaN))` plus `FinishFrame((NaN, NaN))`. With the scroll sum, the part-level composition would need three calls, so `EndFrame` now writes `Game.PointerState = default;` and `Game.PointerState.Move(noPosition);`. The result matches the rule of Decision 6: position (NaN, NaN), delta (0, 0), scroll (0, 0). `default` alone leaves the position (0, 0), so the `Move` still names the empty value in code.
12. **`UiPointerButtonState.BitOf` is internal (phase 2).** The writer needs the button bit to set `_uiPresses` and `_uiReleases` (a value outside 0 to 7 gives bit 0 and changes no mask). The Internal Shape names `Press`, `Release`, `BeginFrame`, and `Mask` as the internal methods, but does not settle how the writer computes the bit. The phase made the existing private helper `internal static` instead of repeating the range rule in the writer.
13. **The ownership write of an up (phase 2).** The plan pseudocode sets the released flag and the `_uiReleases` bit inside "when down holds the bit". The brief rules line reads as if `PointerUp` sets `_uiReleases` whenever `_uiPresses` holds the bit. The phase followed the pseudocode: `Record` checks `Raw.ButtonState.IsDown(button)` first, then updates `_uiReleases` with `_uiPresses & bit`, then calls `Release`. A stray up of a button that is not down changes no mask. The two readings differ only for an up of a button that is not down while `_uiPresses` holds its bit, and the added `_uiReleases` bit would hide no raw flag there, so the behavior is the same.
14. **The press owner survives the frame (phase 2).** `_uiPresses` keeps its bits across `BeginFrame`, because a press that went down over the UI must stay a UI press while the pointer drags outside (Decision 8). Only a new `PointerDown` entry rewrites the bit, from `hitUi`. The brief rules line states this ("`Record` now reads it"), and the phase records the field rule where the plan left the clear list open: hook 1 clears `_uiReleases` only.
15. **The `Key` helper gains a repeat parameter (phase 3).** `PointerTree.Key` could not carry a repeat, and the brief allows the extension. The method now takes `bool repeat = false` as the last parameter and writes it to `IsRepeat`. The default keeps every existing call and test unchanged.
16. **Plant 8 keeps the flag clear (phase 3).** "Remove the key clear of `EndFrame`" could delete the whole `if (_clearKeys)` block. That leaves `_clearKeys` written and never read, a CS0414 warning under `TreatWarningsAsErrors`, so the plant would not compile and the old binary would pass. The plant removes only the `Raw.KeyState.ReleaseAll();` line and keeps `_clearKeys = false;`. The guarded rule still fails, so the plant proves the rule.
17. **`Set` and `Clear` skip the range check (phase 3).** The brief binds `Holds` to the range rule ("a code outside that range reads `false`"). The two internal bit writers run only behind the `(uint)key < (uint)KeyCount` guard of `KeyDown` and `KeyUp`, so they take the code with no second check. They are static helpers on a `ref KeyBits`, because C# rejects an indexer write through an `in` receiver. `Holds` keeps the guard for its own reads.
18. **Word count lives on `KeyBits` (phase 3).** `KeyBits.WordCount = 11` is the single source for the attribute and for the `ReleaseAll` loop, in the `ClassIdBuffer` pattern of the inline-array const.
19. **The focus callback takes an enum, not a bool (phase 3 group 2).** `GlfwWindowFocusCallback` is `void (GlfwWindow window, GlfwFocusEvent focusEvent)`, so the brief's `HandleFocus(GlfwWindow, bool)` shape cannot subscribe. The handler converts the value: `PushFocus(focusEvent == GlfwFocusEvent.GainFocus)`. `LostFocus` exists in the assembly but `GlfwFocusEvent` has no XML docs, so the mapping names only the gain value and treats every other value as a loss. The seam rule is unchanged: `PushFocus` returns after `Dispose`, and a loss calls `Context.ClearHeldKeys()`.
20. **The held-key text ends with no period (phase 3 group 2).** Task 3.8 says `InputText` appends `. Held key: <key>`, and the Definition of Done F8 shows `Game pointer delta: <x>, <y>. Held key: <key>`. The period examples in the group brief sit inside prose sentences. The sample writes no trailing period, and the separator after the delta comes from the appended `. ` prefix.
21. **Locked stale doc counts (phase 3 group 2).** The brief fixes `GlfwUiInputTests.cs` at one added line and `Program.cs` at a buffer-size change, so the "eight" counts in that test file and the "needs 32 bytes" comment in `Program.cs` stay stale after this group. Unlike Decision 8 (where the brief allowed the remarks), here the scope rule wins. Open for `/update-feature` or phase 5: write "nine" twice in `GlfwUiInputTests.cs`, and "needs 48 bytes" in `Program.cs`. Closed by review group 3 (CR-5) on 2026-11-08. The review unlocked the counts, and the three comment lines now read "nine", "nine", and "needs 48 bytes".
22. **The lowest-code scan (phase 3 group 2).** The label shows one key, so `LowestHeldKey` scans codes 0 to 348 in order and stops at the first down. The plan names no helper. The scan lives in `HelloScreen` (private), not in Core, because the key part already answers `IsDown(int)` and the sample owns the display rule.
23. **The slots field and the internal first slot (phase 4).** The gamepad part of `UiInputState` follows Decision 1: `internal GamepadSlots GamepadState;`, with `[InlineArray(16)] internal struct GamepadSlots { internal UiGamepadState _slot. }`. The first-slot field is `internal`, not `private`, because `Gamepad(int)` and `InputStateWriter.SetGamepad` index the slots with `Unsafe.Add` from outside `GamepadSlots`. `KeyBits._word` stays private because its indexers live in the same file's owner type.
24. **The edge write lives on `UiGamepadState` (phase 4).** The `_pressed` and `_released` fields are private, so the writer cannot set them. `UiGamepadState` gains `internal readonly void WriteEdgesInto(ref UiGamepadState slot)`: it copies the connected flag, the buttons, and the six axes (a whole-struct `ref GamepadAxes` copy, no defensive copy), and sets `Pressed = new & ~old` and `Released = old & ~new` over the old slot buttons and the new buttons. `InputStateWriter.SetGamepad` computes the slot ref and calls it. The plan names the rule but not its owning type. This keeps the index rule in the writer and the field access in the slot type.
25. **Plant 11 writes through the public constructor (phase 4).** `BeginFrame` cannot clear a private edge field from `InputStateWriter`, and the brief plants the rule in `InputStateWriter.cs` only, so the plant rebuilds each slot with `new UiGamepadState(slot.Buttons, default)` and `WriteEdgesInto`: `Pressed` and `Released` both become `None`, and the axes become 0. The extra clear of `Released` and the axes changes no result: `TheDrainKeepsTheGamepadEdges` checks that the pressed edge survives (it must not), and no plant-11 check reads an axis. A real fix would add a `ClearEdges` method instead. The plant proves the guard, not the shape.
26. **`GamepadCount` reads through the slot type (phase 4).** `UiInputState.GamepadCount = GamepadSlots.SlotCount`, the Decision 18 pattern: the inline-array const is the single source for the attribute and for the range checks in `Gamepad(int)` and `UiContext.SetGamepad`. The public const value is 16.
27. **Plants in `InputStateWriter.cs` add the `using System;` line (phase 5).** The file imports no `System`, so the brief's plant line `GC.KeepAlive(new object());` cannot compile alone. The plant keeps the line of the brief word for word and adds the one using line. Writing `System.GC` would change the plant text. The restore (`cp` from the scratch copy) removes both lines, and `cmp` proves it. The same rule holds for `ClearHeldKeys` and `SetGamepad`: the group calls them through the public `UiContext` seam, never on the writer.
28. **Plant 16 converts `IsDown` to a block body (phase 5).** `UiKeyState.IsDown` is an expression-bodied member, and the brief plants the line as "the first line" of it. The plant rewrites the member as a block body with the keep-alive first and the same `return Holds(in _down, key);`. The behavior change is the allocation only, and the restore brings back the expression body (`cmp` clean).
29. **The step parity and the read body (phase 5).** "On every second step" binds `ClearHeldKeys` to the even step index. Odd steps hold no request, so one measured loop exercises both key paths (Decision 10 applies at the next drain). The same parity selects the gamepad state, so `SetGamepad` alternates its two hand-made values and both edges stay real in every round. The step body keeps the sum in a local `int` and writes one scene field after the reads. `TheMeasuredBodyDoesRealWork` derives its fixed sums from the read body: step 0 gives -2 (both scroll ints) + 2 (raw Left flags) + 4 (W flags on both views) + 6 (slot 0 buttons on both views) = 10, and step 1 gives 16 with the slot buttons 6 each.
30. **Reads take the view as an `in` parameter (phase 5).** `ReadView(in UiInputState state, int sum)` keeps the sweep body free of a view branch, and both call sites pass a `ref readonly` local through `in`, so the call copies nothing. The full sweep covers all three key flags for each of the 349 codes, all three button flags for each of the 8 buttons, and `Connected`, the three button sets, and the six axes for each of the 16 slots, on both views.
31. **The lint fix shape of this file (task 5.6).** The first lint run gave 3.44 per 100 words, and the semicolon count was 84. The fix replaced each prose semicolon with a period and a capital start. It changed no fact, number, file name, or command. Code spans keep their semicolons, and the lint strips them. The remaining flags are long record lines and possessive apostrophes like "the plan's". They stay: the record lines hold the measured results, and the scaffold headings `**What's Done:**` and `**What's Next:**` follow the Beast Mode template. The file now reads 2.0 per 100 words.

---

## Gotchas & Learnings

### 1. MSBuild nodes hang the build on this machine
**Problem:** `dotnet build` or `dotnet test` can hang on stale MSBuild nodes.
**Solution:** Every command sets `MSBUILDDISABLENODEREUSE=1`. Pass `-nodeReuse:false` to `dotnet build` only - on `dotnet test` it breaks the host handshake (exit 5, "Zero tests ran"). Never run `dotnet build-server shutdown`.
**Lesson:** Keep the pattern of the plan's Verification Method for every gate run.

### 2. Known test flakes
**Problem:** A test host can exit with 134 (Internal CLR error 0x80131506). `TheCurvesAllocateNothing` failed one time with 8128 bytes.
**Solution:** Run the tests a second time before treating either as a fault.
**Lesson:** A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before reading a plant result.

### 3. The agent bash sandbox isolates /tmp between processes
**Problem:** Inside the dsh agent sandbox, `xvfb-run` starts Xvfb, but the client process cannot open `/tmp/.X11-unix/X<n>`: the server and the client see different `/tmp` views. `scripts/test-linux.sh` therefore fails at its smoke step inside the sandbox, and a scratch folder under `/tmp` does not survive between tool calls.
**Solution:** Stage plant scratch under the workspace (`src/BrutalUi.Core/obj/plantscratch/`, git-ignored), and run the smoke script outside the sandbox for the final PASS check.
**Lesson:** A sandbox smoke failure is not a code result. Record it and hand it to a run with a normal shell.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the user moves the mouse over the game area of HelloPanel, and a label shows the pointer delta. The label reads 0 over the panel. (Verifier: Demo MANUAL - no usable display in the agent sandbox.)
- [ ] Phase 2 demo: a developer runs `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests'` after a build and reads a total of 18 with `failed: 0`. (Verifier ran it: PASS.)
- [ ] Phase 4 demo: a developer runs the built Core test binary with `--filter-class '*InputStateTests'` and reads a total of 32 with `failed: 0`. (Verifier ran it: PASS.)
- Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` outside the agent sandbox and expect its PASS line (the sandbox blocks the X11 socket of the smoke step. See Gotcha 3). Task 1.10 repeats it with the input label: the same smoke blocker appears in this session (see Phase 1 Group 2 Results), so the PASS run is still open.
- Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a host with a display. Move the pointer over the game area and read the third label: it shows a non-zero `Game pointer delta: <x>, <y>`. Move it over the panel and read 0, 0. No display exists on this agent host, so the group cannot run this check.
- Read line 3 of that run: `HelloPanel: bytes after warm-up: Update and Draw 0, ...`. The new label write sits inside the UI region, so a non-zero sum means the write allocates. The code review says it cannot: the scratch is fixed, `int.TryFormat` boxes nothing, and the label buffer starts at its longest text.
- Re-measure line 2 (`draw commands`, `vertices`) after the new label: one more label adds vertices to the shared font-atlas command. The technical-design counts are already marked pending.
- [ ] Phase 3 demo: the user holds W over the game area of HelloPanel, and the label shows `Held key: W`. The user clicks the text field and holds W, and the label shows `Held key: none`. (Verifier: Demo MANUAL - no display in the agent sandbox. Also press Alt+Tab with W held and read `Held key: none` after coming back.)
- [ ] Phase 5 demo: a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built Core test binary reads a total of 3 for `--filter-class '*InputStateAllocationTests'` with `failed: 0`. (Verifier: class-run part PASS. Script part Demo MANUAL - smoke step hits the sandbox X11 carve-out.)

---

## Migration Risks And Follow-Ups

- **Docs follow-up (phase 5).** Done in task 5.4: `docs/technical-design.md` now reads "the gate read, and the two label updates" for the HelloPanel UI region. The region holds the gate label update and the input label update.
- **Sample reuse risk.** A copy of `HelloScreen.BuildWidgetPanel` now needs three out arguments. The only other reference to the method is a comment in `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`, which builds its own tree, so nothing else breaks.
- **Phase 3 grows the same method.** Task 3.8 adds the held-key text to `InputText` and asks for the longest text again. The clamp, the separator literals, and `LongestInputText` live in `HelloScreen`, so phase 3 updates one place. Check the 64-byte scratch and the label buffer at that time (Decision 9).
- **No gate for the label text.** No test reads the text of the input label. The smoke run measures the allocation only, and the value check is the window run above. `InputStateTests` guards the delta that the text shows, so the group adds no sample test.

---

## Session Notes

### 2026-11-08 17:00
- `/autorun-feature input/input-state` run ended. All 32 tasks done, all five phase verifiers PASS, cross-phase review closed (7 findings, 7 fixed), and the post-review verifier returned PASS: build 0/0, solution 2864 / 0 failed / 3 skipped, class totals 32, 5, and 3, scope and Q4 greps clean. `implementation.md` Status: Ready for Review.
- Docs rolled up: `/update-epic input` refreshed the epic Features table (input-state Complete), the rollup, and the `ui.GameInput` spelling of Notes gap 1 (the same fix applied to `input-state/requirements.md` and `input-actions/requirements.md`). `/update-master` added the single `input (epic)` row to `docs/overview.md`, the v2.0 changelog entry, and the input-state integration bullet.
- Branch: `feat/input/input-state` (base `aec0341`). The run made no commit beyond the user-authorized panel-sample commit. The user commits this feature, pushes, and opens the PR.

### 2026-11-08 16:45
- Review group 3 (CR-5, CR-6, CR-7) applied. Comment-only edits in the four sites of the group brief; no behavior line changed. Decision 21 closes.
- Gates: build 0/0; solution 2864 with 0 failed and 3 skipped. No rerun was needed.
- Next: the verifier reruns the gates for these three findings, then `/update-feature`.

### 2026-11-08 16:40
- Review group 1 (CR-1, CR-2) applied. Doc-text fixes only; no code change.
- CR-1: `docs/technical-design.md` section 9 item 11 now states the ownership rule the right way round: the game view hides the down and pressed flags of a press that went down over the UI, and the released flag of an up of such a press, wherever the up lands. The raw state keeps every flag.
- CR-2: `implementation.md` I1 now names `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` as a second changed existing test file (the optional `repeat` parameter of Decision 15), and Verification step 7 now counts the three new test files that shipped.
- Next: the verifier reruns the gates for these two findings.

### 2026-11-08 16:35
- Phase 5 verifier returned PASS with Demo: MANUAL (script smoke part only. The class-run part verified PASS: gate 3/3, `InputStateTests` 32/32, focus tests 5/5, solution 2864 / 0 failed / 3 skipped, build 0/0, diff set exactly the plan's file list, docs and Q4 greps clean). All five phases are complete: 32/32 tasks.
- Next: cross-phase review (`/review-feature input/input-state`), then `/update-feature`, `/update-epic input`, and `/update-master`.

### 2026-11-08 16:25
- Phase 5 group 2 (tasks 5.3-5.6, the docs, the changelog, and the final runs) complete. Edited: `docs/technical-design.md` (section 9 items 1 and 7, new item 11, two new bullets in section 14, and the UI-region line), `CHANGELOG.md` (three Added entries and one Changed entry), `tasks.md`, and this file. The feature is at 32/32 tasks.
- Gates: demo totals 32 (`InputStateTests`), 5 (`GlfwUiInputFocusTests`), and 3 (`InputStateAllocationTests`), each with failed 0. The `scripts/test-linux.sh` test step reads total 2864, succeeded 2861, failed 0, skipped 3, and its build reads `0 Warning(s)`, `0 Error(s)`.
- `scripts/test-linux.sh` stops at the smoke step with the known sandbox X11 line (`HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`). The wide-access retry is unavailable here (approvals disabled, escalation flag forbidden). The PASS run stays open outside the sandbox.
- Lint (`--fail-over 2.5`): `technical-design.md` 1.55, `CHANGELOG.md` 1.58, this file 2.0, `implementation.md` 1.96. All four pass. Decision 31 records the prose fix of this file.
- Recorded the spelling note of Notes gap 1 for `/update-epic`, and the open stale counts of Decision 21.
- Inbox: the note "repeat 10 times" stays informational (phase 4 carry-over).
- Next: the phase 5 verifier, then `/update-feature` and `/evaluate-feature`.

### 2026-11-08 16:10
- Phase 5 group 1 (tasks 5.1-5.2, the 0-byte gate) complete. New file `InputStateAllocationTests.cs` (3 tests, the nineteenth gate). `tasks.md` and this file updated. No `src/` change survives: plants 13-16 restored, each `cmp` clean.
- Gates: build 0/0, solution 2864 with 0 failed and 3 skipped, `InputStateAllocationTests` total 3 with 0 failed and 0 bytes measured, `InputStateTests` keeps 32 with 0 failed. The inbox note (repeat): two extra `InputStateTests` runs and three extra gate-class runs, all clean, no flake.
- Plants 13 and 15 (writer hooks): exit 2, the same 41 gates fail. The new pass test measures 2400 bytes. Plant 14 (the `KeyDown` case): exit 2, 10 key-drain gates fail, 7200 bytes. Plant 16 (`UiKeyState.IsDown`): exit 2, the two state tests fail, 1675200 and 4800 bytes. Details in Phase 5 Group 1 Results.
- Decisions 27 to 30 record the plant using line, the block body of plant 16, the step parity and the read sums, and the `in` read view.
- Open (unchanged): the `scripts/test-linux.sh` smoke PASS run outside the sandbox and the HelloPanel window checks (Needs Human Verification).
- Next: phase 5 group 2 (tasks 5.3-5.6, the technical-design items, the changelog, and the final runs).

### 2026-11-08 16:00
- Phase 4 verifier returned PASS with Demo: PASS. Gates: build 0/0, solution 2861 / 0 failed / 3 skipped, `InputStateTests` 32 / 0 failed, `UiContext.cs` diff is the recorded set only, plant integrity clean (plants 11-12 restored `cmp`-identical), gamepad rules verified, struct size record present (804 bytes). Phase 4 closed at 26/32.
- Inbox: one open note "repeat 10 times" (reached the Phase 4 agent. Intent unclear). It rides the phase 5 briefs per section 12 rule 5.

### 2026-11-08 15:50

### 2026-11-08 15:50
- Phase 4 (tasks 4.1-4.4, the gamepad slots and the write seam) complete. New file `UiGamepadState.cs`. Edited `UiInputState.cs` (16 slots, `GamepadCount`, `Gamepad(int)`), `InputStateWriter.cs` (`SetGamepad` with the edge rule, slot remarks), `UiContext.cs` (`SetGamepad` with the seam remarks), and `InputStateTests.cs` (6 tests).
- Gates: build 0/0, solution 2861 with 0 failed and 3 skipped, `InputStateTests` total 32 with 0 failed. The known CLR flake took the first full-solution run. The rerun passed. `Unsafe.SizeOf<UiInputState>()` measures 804 bytes, and `UiGamepadState` measures 40 bytes (A1 bound 1024).
- Plants 11 and 12 each exit 2 with `0 Error(s)` and fail their expected test plus one edge-adjacent test. Each restored file equals its copy under `cmp`. Details in Phase 4 Results.
- Decisions 23 to 26 record the slots field, the edge-write owner, the plant-11 shape, and the `GamepadCount` source. The first `SetGamepad` draft missed the index guard. Test 4.3 caught it and the guard now sits in `UiContext.SetGamepad`.
- Open (unchanged): the `scripts/test-linux.sh` smoke PASS run outside the sandbox and the HelloPanel window checks (Needs Human Verification).
- Next: phase 5 (tasks 5.1-5.6, the 0-byte gate `InputStateAllocationTests`, plants 13-16, and the docs).

### 2026-11-08 15:45
- Phase 3 verifier returned PASS with Demo: MANUAL. Gates: build 0/0, solution 2855 / 0 failed / 3 skipped, `InputStateTests` 26 / 0 failed, `GlfwUiInputFocusTests` 5 / 0 failed, the one-line `EventNames` diff, adapter and plant integrity clean. Phase 3 closed at 22/32.

### 2026-11-08 15:35
- Phase 3 group 2 (tasks 3.6-3.8, the `GlfwUiInput` adapter and the HelloPanel held-key text) complete. Edited `GlfwUiInput.cs` (the `_onFocus` field, the subscribe and unsubscribe lines, `PushFocus`, `HandleFocus`, the `KeyboardEnabled` backing field with the switch-only clear, and the remarks), `GlfwUiInputTests.cs` (one `EventNames` line), and `HelloScreen.cs` (the held-key part, the scan helper, and the grown longest text). New file: `GlfwUiInputFocusTests.cs` (5 tests). `Program.cs` did not change (the 64-byte scratch still holds each 48-byte text).
- Gates: build 0/0, solution 2855 with 0 failed and 3 skipped, `GlfwUiInputFocusTests` 5 with 0 failed, `InputStateTests` keeps 26 with 0 failed. The 5 focus tests pass. Every other existing test passes with no edit.
- Plant 10 exits 2 with `0 Error(s)` in the run log and fails exactly `OnlyTheSwitchFromTrueToFalseClears`. The restored file equals its copy under `cmp`.
- The focus callback signature found: `void (GlfwWindow window, GlfwFocusEvent focusEvent)` — the plan's `bool` guess was wrong (Decision 19). Decisions 20 to 22 record the text rule, the locked stale counts, and the scan place.
- `scripts/test-linux.sh` stops at the smoke step with the known sandbox X11 line (`HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`). Preflight, build, test, and gpu pass in that run. The wide-access retry is unavailable here (approvals disabled). Open (unchanged): the smoke PASS run outside the sandbox, and the window checks of phase 1 and phase 3 (no display).
- Next: phase 4 (tasks 4.1-4.4, the gamepad slots and the write seam).

### 2026-11-08 15:20
- Phase 3 group 1 (tasks 3.1-3.5, the Core key path) complete. Edited `UiInputState.cs` (`KeyBits`, `UiKeyState`, the `Keys` member), `InputStateWriter.cs` (the key cases, `RequestKeyClear`, the key clear and key gate of `EndFrame`), `UiContext.cs` (`ClearHeldKeys`, the only change of this group there), `InputStateTests.cs` (8 tests), and `PointerTree.cs` (the optional `repeat` parameter).
- Gates: build 0/0, solution 2850 with 0 failed, `InputStateTests` total 26 with 0 failed. The 18 phase-1 and phase-2 tests pass with no edit.
- Plants 7 to 9 each exit 2 with `0 Error(s)` and fail their expected test. Each restored file equals its copy under `cmp`. Plant 8 also fails the same-frame clear test (same rule). Details in Phase 3 Group 1 Results.
- Decisions 15 to 18 record the helper extension, the plant 8 compile rule, the bit-writer guards, and the word-count const.
- New tool fact for later runs: do not pass `-nodeReuse:false` to `dotnet test`. Every assembly then exits 5 with "Zero tests ran" (see Phase 3 Group 1 Results).
- Open (unchanged): the `scripts/test-linux.sh` smoke PASS run outside the sandbox and the HelloPanel window checks (Needs Human Verification).
- Next: phase 3 group 2 (tasks 3.6-3.8, the `GlfwUiInput` adapter and the HelloPanel held-key text).

### 2026-11-08 15:05
- Phase 2 group 1 (tasks 2.1-2.4, the buttons, the press ownership, and the scroll) complete. Edited `UiInputState.cs` (`UiPointerButtonState` with the three byte masks and the three queries, `Press`, `Release`, `BeginFrame`, `Mask`. `UiPointerState.ScrollDelta`, `Scroll`, part-level `BeginFrame`. The `Buttons` member), `InputStateWriter.cs` (`_uiPresses`, `_uiReleases`, the button cases and the scroll sum of `Record`, the hook 1 clears, the button gate and the ownership mask of `EndFrame`), and `InputStateTests.cs` (10 tests, `TargetedDown`, `GamePoint`). `UiContext.cs` did not change.
- Gates: build 0/0, solution 2842 with 0 failed, `InputStateTests` total 18 with 0 failed. The 8 phase-1 tests pass with no edit.
- Plants 4 to 6 each exit 2 with `0 Error(s)` and fail their expected test. Each restored file equals its copy under `cmp`. Details and the two deviations (plant 6 edits `UiInputState.cs`. The up guard of Decision 13) are in Phase 2 Results.
- Decisions 10 to 14 record the frame clear, the empty part, the internal `BitOf`, the up guard, and the press-owner survival.
- Open (unchanged): the `scripts/test-linux.sh` smoke PASS run outside the sandbox and the HelloPanel window checks (Needs Human Verification).
- Next: phase 3 group 1 (tasks 3.1-3.5, the held keys, the repeat rule, the modifiers, and the clear rules).

### 2026-11-08 14:40
- Phase 1 verifier returned PASS with Demo: MANUAL. Gates: build 0/0, solution 2832 / 0 failed / 3 skipped, `InputStateTests` 8 / 0 failed, protected files clean, `src/` diff exactly the plan's Core set, plants 1-3 restored. The smoke step of `scripts/test-linux.sh` stops on the sandbox X11 blocker (carve-out, not a code fault). Phase 1 closed at 10/32.

### 2026-11-08 14:35
- Phase 1 group 2 (tasks 1.7-1.10, the HelloPanel label) complete. Edited `HelloScreen.cs` (`InputText`, `LongestInputText`, `DeltaComponent`, the second `out Label inputLabel`, and the remarks), `HelloModes.cs` (`_inputLabel`, `InputLabel`, the factory call, the exception text), and `Program.cs` (`inputScratch`, the `InputLabel` argument, the label write after the gate label, and the `RunFrames` remarks). No file under `src/` changed.
- End of group: build 0/0, solution 2832 with 0 failed, `InputStateTests` keeps its 8 passes. `scripts/test-linux.sh` stops at the smoke step with the known sandbox X11 blocker (exit 1). Steps preflight, build, test, and gpu pass. Details in Phase 1 Group 2 Results.
- Decisions 5 to 9 record the clamp order, the rounding rule, the out-parameter order, the doc scope, and the scratch size.
- Open: the smoke PASS run outside the sandbox, the window demo check (no display here), and the line 3 and line 2 re-measure. See Needs Human Verification and Migration Risks And Follow-Ups.
- Next: phase 2 group 1 (tasks 2.1-2.4, the buttons, the press ownership, and the scroll).

### 2026-11-08 14:20
- Phase 1 group 1 (tasks 1.1-1.6, Core path) complete. New files: `UiInputState.cs`, `InputStateWriter.cs`, `InputStateTests.cs` (8 tests). Edited: `UiContext.cs` (one field, two constructor lines, three hooks, `Input`, `GameInput`, and the doc remarks of task 1.4).
- Base state recorded in Base State: commit `aec0341f`, solution 2824 / Core 1751 / Glfw 27, skipped 3. `git status --porcelain` clean except the feature docs, so gap 6 stays closed.
- End of group: build 0/0, solution 2832 with 0 failed after the known-flake rerun, `InputStateTests` 8 with 0 failed. Plants 1 to 3 each exit 2 with `0 Error(s)` and fail their expected test. Restored files equal their copies. Details in Phase 1 Group 1 Results.
- Open: the `scripts/test-linux.sh` smoke step cannot run inside the agent sandbox (Gotcha 3 and Needs Human Verification).
- Next: phase 1 group 2 (tasks 1.7-1.10, the HelloPanel label).

### 2026-11-08 13:55
- `/autorun-feature input/input-state` started. Preconditions met. Plan exists, so no planning run.
- Committed panel-sample as `aec0341` (user permission) and fast-forward merged it into the new branch `feat/input/input-state` created from `main`. The plan's gap 6 (HelloPanel state) is closed.
- Scaffolded `tasks.md`, `context.md`, `inbox.md`. Status of `implementation.md` set to In Progress.
- Next: phase 1 group 1 on an ADVANCED agent per the plan's tier line.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
