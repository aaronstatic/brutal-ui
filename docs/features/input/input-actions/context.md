# input/input-actions - Context & Decisions

**Last Updated:** 2026-11-09 17:45 UTC
**Current Phase:** Complete. All 5 phases (41/41 tasks). Cross-phase review closed: 13 findings, 11 fixed in four groups, 2 deferred with reason (CR-4, CR-5). Final verifier PASS after one prose-sweep round.
**Status:** 🟢 Ready for Review (window-host checks stay in Needs Human Verification)

---

## Quick Status

**Done:**

- The plan is approved. `implementation.md` holds 5 phases and 41 tasks.
- `/start-feature` made this file, `tasks.md`, and `inbox.md`.

- Phase 1 group 1 (tasks 1.1 to 1.5): `UiInputReadHandler`, the `UiContext.OnInputRead` slot and call, `InputReadTests` (6), plants 1 and 2.
- Phase 1 group 2 (tasks 1.6 to 1.12): the three partial files, `UiActionType`, `AddAction(action, type)`, the type checks, `BindKeyVector`, `ReadFrame` behind the Core slot, `ReadVector`, `TypedActionMapTests` (9), plants 3 to 5.
- Phase 1 verifier PASS. Demo PASS. Build 0/0. Solution 2896, 0 failed, 3 skipped.
- `InputReadTests` 6/6. `TypedActionMapTests` 9/9, with `WOnAWasdVectorReadsZeroOne` named.
- Each existing class passed with no edit. The scope held exactly the nine plan files.
- `DrainEvents` showed no diff. The protected files showed no diff. No plant residue stayed.
- Verifier note: six new files were untracked. Run `git add` before the commit, so the scope gate stays readable.

- Phase 2 group 1 (tasks 2.1 to 2.7, the map): `UiGamepadStick`, `BindGamepadStick`, `BindGamepadAxis`, `GamepadSlot`.
- The map also gained the `GamepadStick` and `GamepadAxis` read cases, the Axis finish, `ReadAxis`, and the invoke pass.
- `GamepadValueActionTests` 8/8, with `TheLeftStickFillsAVector2WithUpPositive` named. Solution 2904, 0 failed, 3 skipped.
- `GamepadActionTests` 7/7 and `ActionMapAllocationTests` 5/5 passed with no edit.
- Plants 6 to 9 each exited 2 with the named test failed. Each restore equalled its copy under `cmp`.
- The body of `PushGamepad` showed no change.

- Phase 2 group 2 (tasks 2.8 to 2.9, the sample): `samples/HelloPanel/Program.cs` drops the `PushGamepad` call (Q4).
- Three comment places name `Update` reading the pad through the map. `grep -n 'PushGamepad'` prints nothing.
- Build `0 Warning(s)`, `0 Error(s)`. Solution 2904, 0 failed, 3 skipped, exit 0.
- Smoke is blocked in this sandbox: `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`
- Plant 10 built with `0 Error(s)`. The `*ActionMapAllocationTests` run exited 2 with 4 failed. The restore was equal under `cmp`.
- Phase 2 verifier PASS. Demo PASS. Build 0/0. Solution 2904, 0 failed, 3 skipped.
- `GamepadValueActionTests` 8/8, with `TheLeftStickFillsAVector2WithUpPositive` named.
- The `PushGamepad` body matched `main` line by line. Only the remarks grew. The scope was exact.
- The protected files showed no diff, and no plant residue stayed.
- Smoke stopped only at the known X11 block. The Needs Human Verification line carries it.

- Phase 3 group 1 (tasks 3.1 to 3.7): `BindMouseDelta`, `BindScroll`, and `BindKeyAxis`.
- The frame read gained six new cases, the free sum, and the Button finish. The map gained `WasPressed`, `IsHeld`, and `WasReleased`.
- The press rule sits in the class remarks. `ActionQueryTests` 10/10 on the first run.
- Solution 2914, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 252 (242 + 10).
- These classes passed with no edit: `UiActionMapTests` 23, `GamepadActionTests` 7, `TypedActionMapTests` 9, `GamepadValueActionTests` 8, `ActionMapAllocationTests` 5, `FocusActionTests` 5, `ModeActionMapTests` 5.
- Plants 11 to 14 each built with `0 Error(s)` and exited 2 with the named test failed. Plant 14 failed one extra test.
- Each restore equalled its copy under `cmp`.
- Phase 3 verifier PASS, Demo PASS. The first pass FAILed only gate 6: the scope line of the brief ignored the approved phase 2 sample edit.
- Gates 6 and 7 were corrected, then the rerun passed. Build 0/0. Solution 2914, 0 failed, 3 skipped.
- `ActionQueryTests` 10/10, with `AKeyPressInvokesOneTimeAndReadsOnePress` and `TheMouseDeltaAndTheScrollHaveNoClamp` named.
- The I3 read-source gate and the Q6 frame-path purity gate were green. The scope was exact.
- The protected files showed no code diff, and no plant residue stayed. The inbox bug line closed with the rerun.

- Phase 4 group 1 (tasks 4.1 to 4.5, ADVANCED): `ResetFrame()` in `UiActionMap.cs`.
- `Deactivate` calls it at the end, also on a replaced map. `Activate` captures `wasActive` before the writes, and resets when it is `false`.
- Each of the five queries returns `false` or zero after the type check when `IsActive` is `false`.
- The class remarks hold the switch rule. The remarks of `Activate`, `IsActive`, and `Deactivate` grew.
- `ActionMapSwitchTests` (6) passed on the first run. `ActionMapAllocationTests` grew to 6.
- The scene holds the five typed actions: 23 actions, 28 bindings. `QueryStep` drives the frame read and the five queries.
- `TheTypedReadAndTheQueriesAllocateNothing` asserts 0 bytes. Build `0 Warning(s)`, `0 Error(s)`.
- Solution 2921 (2914 + 7), 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 259.
- `ModeActionMapTests` 5, `ModeDiffTests` 9, `ActionQueryTests` 10, and each other class passed with no edit.
- The diff of `UiManager.cs` is empty. Plants 16 to 19 exited 2 with the expected test failed, and each restore equalled its copy under `cmp`.
- Plant 15 exited 0. With the guard (4.2) and the fresh-`Activate` reset (4.1) in place, the `ResetFrame` call inside `Deactivate` is not observable through the public API.
- The change is defensive, and no test can fail it (decision 3).
- Phase 4 verifier PASS. Demo PASS. Build 0/0. Solution 2921, 0 failed, 3 skipped.
- `ActionMapSwitchTests` 6/6, with `AModeSwitchThroughTheManagerGivesNoStaleFlag` and `AReplacedMapReadsZero` named. `ActionMapAllocationTests` 6/6.
- The query order (type check, then `IsActive`) was confirmed by reading the five bodies. The scope was exact.
- `UiManager.cs` had an empty diff, and no plant residue stayed.
- The verifier independently confirmed the plant 15 analysis. `DeactivateReleasesEachHeldAction` passes without the `Deactivate`-side `ResetFrame()`.
- The call is defensive only. That deviation was accepted.

- Phase 5 group 1 (tasks 5.1 to 5.4, the HelloPanel move label): `HelloState` holds the `MoveAction` name and a Vector2 action.
- It binds `BindKeyVector` with W, A, S, D (the names compile) and `BindGamepadStick` with the left stick. The three old Button actions stay (A8).
- `HelloScreen.BuildWidgetPanel` takes `out Label moveLabel` after `stickLabel`. The label sits right after the stick label.
- Its start text is `LongestMoveText` (18 bytes). `MoveText(float2, Span<byte>)` copies the `StickText` pattern through `StickComponent`.
- `HelloModes` exposes `MoveLabel` in the pattern of `StickLabel`. `RunFrames` takes the label and holds `byte[] moveScratch = new byte[32];`.
- The write sits right after the stick line, inside the UI region. Build `0 Warning(s)`, `0 Error(s)`.
- Solution 2921, 0 failed, 2918 succeeded, 3 skipped, exit 0. No test changed.
- Smoke stopped at the known X11 block: `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, `test-linux: FAIL at smoke (exit 1)`, `exit=1` (Gotcha 3).
- Plant 20 put `GC.KeepAlive(new object());` as the first line of `MoveText`. It built with `0 Warning(s)`, `0 Error(s)`.
- The smoke attempt showed the same X11 line. The restore was equal under `cmp`, and the post-restore build was `0 Warning(s)`, `0 Error(s)`.
- The PASS run, the plant 20 byte measure, and the move-label demo go to Needs Human Verification.

- Phase 5 group 2 (tasks 5.5 to 5.8, the docs): section 9 of `docs/technical-design.md` holds the new item 13 "The read slot".
- `### Action map` is rewritten for the typed map, with the table of "The Value Rules".
- Section 4 names the read slot in the frame order and in the overview diagram.
- Section 14 names the typed read in the **Action map tests.** bullet, and adds the **Typed action tests.** bullet.
- The new bullet holds the four new classes, `InputReadTests`, and plants 1 to 20.
- The seventeenth gate bullet grew to 6 tests. The sample bullet names the move label and the drop of the push.
- `CHANGELOG.md` names each new public member under **Added**, and three behavior changes under **Changed**.
- The build gave `0 Warning(s)`, `0 Error(s)`. The solution gave 2921, 0 failed, 2918 succeeded, 3 skipped.
- Each demo class filter of phases 1 to 4 passed with `failed: 0`. The four lint files passed at `--fail-over 2.5`.
- This file also passed the lint after the prose pass.

**Next:**

- ✅ Cross-phase review closed on 2026-11-09 17:40. `code-review.md` holds 13 findings. Review group 1 fixed CR-1 and pinned CR-2 and CR-10 as stated limits. Group 2 fixed CR-3, CR-6, CR-11, CR-13. Group 3 pinned CR-7 and CR-9 in tests. Group 4 fixed the docs CR-8 and CR-12, plus the section 9 and CHANGELOG follow-ups. CR-4 and CR-5 are deferred with reason: their code belongs to control-schemes and binding-remap. The parent corrected the stale "code below 0" sentence in section 9 to the shipped state-range rule.
- ✅ Final verifier PASS after one fix round. Pass 1 failed gate 8 only: the prose of `code-review.md` read per100w=2.61 against the 2.5 bar. The sweep took it to 0.11 with every fact kept. The rerun passed all three checks: five lint scores (1.47, 1.47, 2.42, 1.77, 0.11), 13 ticked findings, 0 open.
- 🔄 `/update-feature`, `/update-epic input`, and `/update-master`. A person then tests the Needs Human Verification items, commits, pushes, and opens the PR.
- 🔎 Verifier note: plant 15 gives exit 0 (deviation, decision 3). A person may delete the `ResetFrame()` call in `Deactivate` to confirm the analysis.
- 🔎 Verifier note: group 1 changed two remark sentences that phase 2 left stale. They are the `PushGamepad` wording in `HelloState.cs` and in the `state` parameter of `RunFrames`. See the phase 5 group 1 decisions.
- 🔎 Follow-up outside this repo: spacegame moves its submodule to the commit of this feature. Then it continues its map-controls feature.

**Blockers:**

- None.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Input/UiInputReadHandler.cs` - phase 1 (new): the delegate type of the slot.
- `src/BrutalUi.Core/Tree/UiContext.cs` - phase 1: `OnInputRead`, the call in `Update`, the re-entry flag.
- `src/BrutalUi.Framework/Actions/UiActionMap.cs` + `.Bindings.cs` + `.Read.cs` - the typed map, split in phase 1.
- `src/BrutalUi.Framework/Actions/UiActionEnums.cs` - phase 1: `UiActionType`. Phase 2: `UiGamepadStick`.

### Related Files
- `docs/features/input/input-actions/implementation.md` - the plan. Each phase holds its full task text there and in `tasks.md`. The briefs copy it word for word.
- `docs/features/input/epic-overview.md` - the build order and the decisions of the epic. The `/update-epic` pass owns the rollup for phase 5.
- `docs/technical-design.md` - section 4 (frame order and the diagram), section 9 (item 13 and the typed action map), section 14 (the typed action tests and the grown seventeenth gate).
- `CHANGELOG.md` - the **Added** lines for the read slot and the typed map. The **Changed** lines for the pad read, the type checks, and the sample.
- `docs/features/input/input-state/context.md` - Gotchas 1 to 3 (build flags, test flakes, the sandbox X11 block).
- `docs/features/input/gamepad-axes/context.md` - Decision 11 (the stick Y sign that this feature flips).

---

## Important Decisions

The plan holds Confirmed Assumptions (Q1-Q6, A1-A8) and Decisions 1-13 in `implementation.md` (Key Technical Decisions). This file records only decisions made during the build that the plan did not settle.

### Phase 1 group 1 (Core read slot)
1. Style source path: the brief names the style file `UiBeforeUpdateHandler.cs`. That file lives at `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs`, not in `Input/`. The new `UiInputReadHandler.cs` follows its doc structure. It sits in `src/BrutalUi.Core/Input/` as the brief names, next to `UiUnhandledInputHandler.cs`. That file has the same shape (`using System;`, file-scoped `namespace BrutalUi.Core;`).
2. No `Counter` target in `InputReadTests`: all six tests push events with no target, so no element needs a handler target. The counting uses instance lambdas. The allocation gate uses the `static readonly UiInputReadHandler s_gateHandler` field with a static counter, in the pattern of `BeforeUpdateTests`.
3. The `OnInputRead` remarks keep the wording of the plan for the slot rules (Q1 seam, `UiActionMap.Activate` writes, `=` replaces, no `+=`, null does nothing). The slot property sits right after `OnBeforeUpdate`, as the brief names.
4. Plant gate: `dotnet test --solution brutal-ui.sln` prints no build summary line (`0 Error(s)`). So the group builds first with `dotnet build brutal-ui.sln -nodeReuse:false`, checks `0 Error(s)` there, then runs the tests. This satisfies Gotcha 2 for a plant that must compile before its result counts.

### Blast radius of the `UiContext.Update` change (advanced rule)
- New call site between `DrainEvents()` and `UpdateBindings()`. It runs only when `OnInputRead` is not null. At the base no code writes the slot, so no existing path changes.
- Re-entry guard `_inputRead` follows the `_beforeUpdate` pattern. The flag clears in `finally`, so a handler exception leaves no stuck flag (test `AnUpdateFromTheSlotThrows`).
- Doc changes: one remarks paragraph, the frame order sentence, one `<exception>` line. `DrainEvents` shows no diff. The diff of `UiContext.cs` holds only slot, flag, call, re-entry check, and remarks.
- Verified: full solution 2887 tests, 0 failed. `BeforeUpdateTests` (6) and `InputStateTests` (32) passed with no edit.

### Phase 1 group 2 (the typed map)
1. Split (1.6): each member moved as it is, with its doc comment. The class is now `public sealed partial class UiActionMap` in `UiActionMap.cs`, `UiActionMap.Bindings.cs`, and `UiActionMap.Read.cs`. The build gave `0 Warning(s)`, `0 Error(s)`. `BrutalUi.Framework.Tests` gave the same 225 total and 0 failed as task 1.1 before any new code.
2. Enum docs (1.7): the member docs of `UiActionType` name their queries with `<c>` markup, not `cref`. `WasPressed`, `IsHeld`, `WasReleased`, and `ReadAxis` do not exist in phase 1. An unresolvable `cref` is an error under `TreatWarningsAsErrors`. `BindKeyVector` and `ReadVector` use the same `<c>` style for a future query name.
3. The empty command (1.7): `AddAction(action, type)` writes `Command = []`. `Invoke` tests `command.Length != 0` before it calls the registry, so a commandless action never reaches `CommandRegistry.UnknownCommand`. The old overload writes `Type = UiActionType.Button`, so every existing map keeps its behavior.
4. The append (1.8): the Internal Shape names `the append` as a `Bindings.cs` member, so the four binds now call one private `Append(in InputBinding)`. The grow rule and the write order stay the same.
5. The value find (1.8): `FindValue(kind, code, code1, code2, code3)` compares all four codes. Each old bind writes -1 in each unused code and `Scale = 1`. So a later composite bind (phase 3 `BindKeyAxis`) reuses the same find and the same one-line text shape of a binding.
6. The order of the checks (1.8): one private helper `CheckActionType(index, type)` throws `ArgumentException(message, "action")`. Each bind checks the range or shape of its arguments, then the action name, then the type, then a duplicate. A rejected call changes no count.
7. The two slots (1.9): `Activate` handles each slot on its own. It clears a slot of the old context only when that slot holds the handler of this map. `Deactivate` also checks each slot against its own handler, and no longer uses `IsActive` as its guard. The case the brief names: a game replaces the unhandled slot after `Activate`. The map then still clears the read slot that it holds, and leaves the delegate of the game.
8. The frame read (1.9): `ReadFrame` holds `ref readonly UiInputState input = ref context.GameInput`, and takes `ref readonly UiKeyState keys = ref input.Keys` inside the `KeyVector` case. The code copies no bit set. The scratch pass and the finish pass run over the local copies of the two arrays and the two counts (the re-entry rule). `ClampLength` is a private static helper. The finish is `a.Value = ClampLength(a.Sum) + a.Free`. Phase 1 never writes `Free`.
9. `ReadVector` (1.10): it sits in `UiActionMap.cs` with the other queries. A name that the map does not hold gives `(0, 0)`. A wrong type throws `ArgumentException` with the parameter name `action`, also on a map that is not active. Phase 4 adds the inactive rule.
10. Deviation from the file list of the brief (1.7): `src/BrutalUi.Framework/Actions/UiActions.cs` is in the do-not-touch list. But the new `AddAction` overload made its `<see cref="UiActionMap.AddAction"/>` ambiguous (CS0419, an error here). The change is one doc line: the cref now names both overloads. No code line changed, and the diff shows 2 insertions and 1 deletion in that comment.
11. Plant gate (1.12): each plant builds with `-nodeReuse:false` first and checks `0 Error(s)`, then runs the class binary and reads its exit code, then restores with `cp` and checks `cmp`. `dotnet test --solution` prints no build summary line, so the gate stays on the separate build.

### Blast radius of the group 2 change (advanced rule)
- `UiActionMap` split: no behavior change. The same members, fields, and docs. All five existing action classes pass with no edit.
- `Activate` and `Deactivate`: two Core slots now, `OnUnhandledInput` and `OnInputRead`. Callers: the `UiManager` mode switch (`ModeActionMapTests`, 5), `HelloState.Map.Activate` in the sample, and the action tests. An active map now also reads the frame in `Update`.
- `Update` of every context with an active map runs `ReadFrame`: a new pass over the actions and bindings. It writes only Vector2 values. Phase 1 has no Vector2 binding in the sample or in an existing test, so no existing value changes. `ActionMapAllocationTests` (5) guards the cost at 0 bytes.
- `Invoke`: every resolve path goes through it (`PressKey`, `PressMouseButton`, `InvokeAction`, `PushGamepad`). Each command from the old `AddAction` is non-empty, so the new length test changes nothing for an existing map.
- The three Button binds: a new `ArgumentException` for a non-Button action. Every existing bind call in `src/`, `samples/`, and `tests/` binds an action made by `AddAction(action, command)`, which is a Button, so none breaks.
- `UiContext.cs`: untouched by group 2. Its diff stays the group 1 change (slot, flag, call, re-entry check, remarks), and `DrainEvents` shows no diff.
- `UiActions.cs`: doc cref only (see decision 10).

### Phase 2 group 1 (the gamepad values and the press)
1. The duplicate check of a value bind follows the KeyVector pattern: the map-wide `FindValue(kind, code, -1, -1, -1)` rejects the same stick or the same axis a second time, whatever the action and the scale. A bind rejected on any check changes no count (the order decision 6 of phase 1: argument checks, then the action name, then the type, then the duplicate).
2. A Y-axis flip needs two code paths, because two read shapes differ. The `GamepadStick` case flips the whole stick through the minus of `-stick.Y` inside `new float2(stick.X, -stick.Y)`. The `GamepadAxis` case negates the result of `pad.Axis(binding.Code) * binding.Scale` for the two codes `UiGamepadAxis.LeftStickY` and `UiGamepadAxis.RightStickY`. A trigger code keeps its sign.
3. The Axis read sums into the X component of the `float2` scratch (`a.Sum = a.Sum + new float2(value, 0f)`), so one finish code path serves a Vector2 and an Axis action. The Axis finish reads `new float2(ClampUnit(a.Sum.X) + a.Free.X, 0f)`, with the clamp before the free sum, per the value rule. `ClampUnit` is a new private static helper in `UiActionMap.Read.cs`.
4. The invoke pass runs after the finish pass, over the copied binding array (the re-entry rule). It tests the frame copy `pressed = pad.Pressed` before the loop. Then for each `GamepadButton` binding with the pressed bit set it checks `context.OnInputRead != _onRead` and breaks, so a command that deactivates the map or replaces the slot stops the pass. The pass then calls the private `Invoke(binding.Action)`, so an action with no command invokes nothing. A command exception leaves the method, and the values of the frame stay in the map, because the finish already ran. `PushGamepad` keeps its body line for line. Only the remarks grew the do-not-push note (Decision 9).
5. The new `GamepadSlot` set throws for a value outside 0 to 15 before it writes, so a wrong set keeps the old value and the next `ReadFrame` cannot go out of range. `UiInputState.GamepadCount` is 16, so the bound `input.Gamepad(_gamepadSlot)` inside `ReadFrame` is always valid for a map that is active.
6. Deviation inside the tests only (2.6): `ATriggerFillsAnAxis` in the plan names four trigger readings and one scale-2 reading. Because the duplicate rule is map-wide, the scale-2 reading of the right trigger needs a second map. That test builds one map for the three trigger readings (right scale 1, left scale -1) and one map for the clamped scale-2 reading, so the test keeps the worked values of the plan: 0.5, -0.75, 0.75, and the clamped 1.0.
7. The docs of `UiActionType` now carry cref links for `BindGamepadStick`, `BindGamepadAxis`, and `ReadAxis`, because this group added them. `BindKeyAxis`, `BindScroll`, `ReadVector`-free Button queries (`WasPressed`, `IsHeld`, `WasReleased`), and `BindMouseDelta` still use `<c>` markup, because they arrive in phase 3.

### Phase 2 group 2 (the sample)
1. Comment shape: the brief says the two body comments must say that `Update` reads the pad through the map. The comment before `input.PollGamepads()` now ends "Update reads the pad of slot 0 through the active map." The comment at the start of the UI region keeps the (A6) clause: the raw slot still holds the poll before `Update`, and the map now reads it there. That comment ends "so Update reads the pad of slot 0 through the active map."
2. Remarks of `RunFrames`: the step (3) list drops the gamepad push and starts with the smoke input. The (A6) note and the new-buttons reason move to a new sentence after the input-pass sentence. The read slot of the active map reads the new buttons of slot 0 and invokes their commands (Q4), so the UI region pushes no gamepad state. No other prose named the push. The smoke-input paragraph ("pushes, at the start of the UI region") stays true word for word.
3. `input.PollGamepads()` and the stick label write are unchanged. No other sample file changed. The gate still measures the map, because `ui.Update` runs inside the UI region and the frame read now runs there.
4. Plant 10 proof: the brief names `TheGamepadStepAllocatesNothing`; that test arrives with the growth of `ActionMapAllocationTests` in phase 4, so it does not exist today. The plant bites through the existing gates instead: the class run of `*ActionMapAllocationTests` exits 2 with 4 of 5 failed. The four are `AMouseButtonOverNoElementAllocatesNothing`, `TheFocusActionsAllocateNothing`, `AGamepadPushAllocatesNothing`, and `AnUnhandledKeyAllocatesNothing`, all of which run `Update` on an active map. This satisfies the "or the measured body's gate failing" clause of the brief.

### Blast radius of the group 2 change (standard rule)
- `samples/HelloPanel/Program.cs` only: one call line deleted, three comment places rewritten, no code path changed. The pad press of the live window now reaches the map through the read slot of `Update` instead of the push.
- `src/`, the other three HelloPanel files, each test, `scripts/`, and project files show no diff. The body of `PushGamepad` shows no diff: `UiActionMap.Read.cs` restores equal under `cmp` after plant 10.

### Blast radius of the group 1 change (standard rule)
- `UiActionEnums.cs`: one new public enum, `UiGamepadStick`. `UiActionType` member docs grew crefs.
- `UiActionMap.Bindings.cs`: two new public binds. No existing bind changed. The `Find` and `FindValue` helpers and the private `CheckActionType` and `Append` serve the new binds with no edit.
- `UiActionMap.cs`: the new `GamepadSlot` property with its backing field, the new `ReadAxis` query, the two new `InputKind` members `GamepadStick` and `GamepadAxis` after `KeyVector`, and the class remarks. `Activate`, `Deactivate`, and `IsActive` are unchanged.
- `UiActionMap.Read.cs`: the frame read grew the two read cases, the Axis finish, and the invoke pass. The remarks of `ReadFrame` and `PushGamepad` grew. `PushGamepad` body is unchanged line for line. `ClampUnit` is new.
- Existing behavior: an action with no gamepad value bind reads the same. The switch ignores the two new kinds when the map holds no such binding, and the invoke pass skips when the frame press set is empty. `GamepadActionTests` (7), `ActionMapAllocationTests` (5), `TypedActionMapTests` (9), `UiActionMapTests` (23), `FocusActionTests` (5), and `ModeActionMapTests` (5) all pass with no edit. The frame read allocates nothing: no new array, box, or closure, and `ActionMapAllocationTests` keeps 0 bytes.
- The phase-2 note of the phase-1 session: the scratch fields `Down`, `Press`, and `Free` and the Button flags stay unused. The `Key`, `MouseButton`, and `GamepadButton` read cases and the Button finish arrive in phase 3. Phase 3 writes them all.

### Phase 3 (the pointer values, the two-key axis, and the Button queries)
1. Two locals at the top of `ReadFrame`: the method computes `mods = input.Keys.Modifiers & MatchMask` and `pressed = pad.Pressed` before the passes, and the invoke pass reuses the local `pressed`. Moving the `pressed` line from after the finish to the top changes no value. The drain wrote the frame before the slot call, and the read writes no state.
2. The free sum needed no new finish code. Phases 1 and 2 already add `a.Free` after the clamp in both finishes, so task 3.3 only writes the three read cases. `MouseDelta` and `Scroll` add to `Free`, and `KeyAxis` adds to `Sum`. Plant 13 proves the split: an add into `Sum` clamps (20, -15) to (0.8, -0.6).
3. The two pointer binds hold no code. `BindMouseDelta` and `BindScroll` write -1 in all four codes, so `FindValue(kind, -1, -1, -1, -1)` gives the map-wide rule of the brief: one delta binding and one scroll binding per map, whatever the action and the scale. The scale stays out of the check, in the pattern of `BindGamepadAxis` (phase 2 decision 1).
4. The delta multiplies its components by hand: `new float2(delta.X * binding.Scale, delta.Y * binding.Scale)`. The Internal Shape names `new float2(x, y)`, `.X`, `.Y`, `+`, and `MathF.Sqrt`. This code base uses no `float2 * float` operator, so the read uses the named shape only.
5. The Button finish keeps the latch of the value rules word for word: `oldHeld`, `start = oldHeld || press`, `Pressed = press`, `Held = start && down`, `Released = start && !Held`. It runs in the same loop as the two value finishes, so one pass writes each action, and a Button action keeps `Value` at zero.
6. The three queries copy the shape of `ReadVector` and `ReadAxis`: `IndexOfAction`, then `false` for a name that the map does not hold, then `CheckActionType(index, UiActionType.Button)`, then the flag read. The type check runs before the flag read, so a wrong type throws also on a map that is not active. Phase 4 adds the inactive rule after the same check.
7. Doc fix in `InputBinding`: the comment of `Code1` named the second code of a key axis "the negative key". The Internal Shape and the read of this phase give `Code` the negative key and `Code1` the positive key, so the comment now names `Code1` the positive key. The comment of `Code` also names the -1 case of the two pointer binds.
8. Deviation inside the docs of one file (3.5): the file list of the brief omits `UiActionEnums.cs`. The three member docs of `UiActionType` moved from `<c>` markup to crefs, because `WasPressed`, `IsHeld`, `WasReleased`, `BindKeyAxis`, `BindScroll`, and `BindMouseDelta` now exist (the rule of phase 2 decision 7). No code line changed there.
9. Test geometry (3.6): the mouse test builds its tree by hand: a root that fills the window with `IsPickable = false`, and one `Button` child of 80 by 24 at the origin (the allocation-test geometry). (300, 200) then hits no element, and (40, 12) hits the button. Because the scroll rule is map-wide, the trigger-plus-scroll case uses a second map and a second context, in the pattern of phase 2 decision 6.

### Blast radius of the phase 3 change (standard rule)
- `UiActionEnums.cs`: three member docs only, no code line.
- `UiActionMap.Bindings.cs`: three new public binds. No existing bind changed. `Find`, `FindValue`, `CheckActionType`, `Append`, and `IndexOfAction` serve the new binds with no edit.
- `UiActionMap.cs`: three new public queries, three new `InputKind` members, the press rule paragraph, the grown value rule, the grown build rules, and the two `InputBinding` docs. `Activate`, `IsActive`, `Deactivate`, `ReadVector`, `ReadAxis`, and `GamepadSlot` are unchanged.
- `UiActionMap.Read.cs`: six new read cases, the Button finish, the two locals at the top of `ReadFrame`, and its remarks. `PushGamepad`, `HandleInput`, `IsTypedByTextField`, the three resolve methods, and `Invoke` are unchanged.
- Existing behavior: a map with none of the new bindings reads the same. The switch ignores a kind that the map holds no binding of, and the Button finish writes only the three flags of a Button action. `UiActionMapTests` (23), `GamepadActionTests` (7), `TypedActionMapTests` (9), `GamepadValueActionTests` (8), `ActionMapAllocationTests` (5), `FocusActionTests` (5), and `ModeActionMapTests` (5) pass with no edit. The frame read allocates nothing: no new array, box, or closure.
- Risk for phase 4: a Button action now keeps live flags, and `Deactivate` clears no flag, so a map that another map replaced keeps its held state until the next frame read of a map that is active. Phase 4 owns `ResetFrame` and the inactive rule of the five queries.

### Phase 4 (the switch rule and the allocation gate)
1. `ResetFrame()` sits in `UiActionMap.cs` with `Activate` and `Deactivate` (the Internal Shape names the reset as a member of that file). One plain loop over `_actions` up to `_actionCount` writes `Value`, the three flags, and the four scratch fields. No array creation, no lambda, 0 bytes.
2. The guard order in the five queries: index check, then `CheckActionType`, then `IsActive`. A wrong-type query still throws on a map that is not active, so `TheButtonQueriesCheckTheType`, `ReadVectorChecksTheType`, and `TheBuildChecksTheGamepadValueBinds` pass with no edit.
3. Plant 15 deviation: the brief expects `DeactivateReleasesEachHeldAction` to fail when `Deactivate` skips `ResetFrame()`. The plant builds with `0 Error(s)`, and all 259 Framework tests pass (exit 0). Reason: after `Deactivate`, `_context` is `null`, so `IsActive` is `false`, and the 4.2 guard gives every query its default value whatever the entries hold. The only way back to an active map is `Activate`, and the 4.1 rule resets there. No public read can observe the call inside `Deactivate`. The call is defensive, exactly the Decision 11 belt: the guard covers a replaced map, and the `Activate` reset covers a return. Plants 16 to 19 each exit 2 with the expected test failed.
4. `QueryStep` in the grown gate runs the same work each round: the writer seeds the pointer position with NaN, and `FinishFrame` compares the raw start and end positions, so the move to the fixed place (300, 250) reads delta (0, 0) after the first round. The scroll of (0, 3) and the pad edges repeat every round. Frame 1: Move (1, 0) (the stick plus W-minus-S keys, S still down from the key step), Look (0, 0) after the first call, Zoom 3.25 (0.25 + the free scroll 3), Throttle 0, Fire pressed and held. Frame 2: Move (0, -1), Throttle -1, Fire released.
5. `TheMeasuredBodyDoesRealWork` asserts the fixed amount 500.25 after one `QueryStep` in its own place, after the other bodies: the first move from the mouse step's raw place (40, 12) gives delta (260, 238), and the stale S of the key step makes frame 1 read Move 1 and Throttle 0, and frame 2 read Move -1 and Throttle -1: 1 + 498 + 3.25 + 0 - 1 + 0 + 0 - 1 = 500.25. Pad A runs its command one time through the frame read, and the flags add 3.
6. `Activate` computes `wasActive` before any write, and the reset runs after the writes, so a re-activated map reads a clean latch from the next frame, and the same map on the same context keeps its values (test 4 pins it, plant 17 kills it).
7. The `IsHeld` remarks of phase 3 said "the switch rule of a later phase". The phase landed, so the phrase now reads "the switch rule of this class".

### Blast radius of the phase 4 change (advanced rule)
- `UiActionMap.cs`: the five query bodies gained the `IsActive` guard and one remarks line each. `Activate` gained `wasActive` and the conditional reset. `Deactivate` gained the final `ResetFrame()`. `ResetFrame()` is new. The class remarks gained the switch rule paragraph. The remarks of `Activate`, `IsActive`, and `Deactivate` grew. `UiActionMap.Read.cs` shows no net change (plants 18 and 19 restored equal under `cmp`).
- `tests/BrutalUi.Framework.Tests/Actions/ActionMapSwitchTests.cs` is new (6 tests). `ActionMapAllocationTests.cs` grew to 6 tests and a 23-action, 28-binding scene.
- Callers of the map: the `UiManager` mode switch (no code change, the diff is empty) reads a replaced map at its defaults, and the same map in two modes keeps its values. `Activate` and `Deactivate` gained one O(actions) pass each, allocation-free. A game that queries a deactivated map now reads defaults instead of stale values: that is the requirement (Decision 11, F8).
- Existing behavior: `ModeActionMapTests` (5), `ModeDiffTests` (9), `ActionQueryTests` (10), `UiActionMapTests` (23), `GamepadActionTests` (7), `TypedActionMapTests` (9), `GamepadValueActionTests` (8), `FocusActionTests` (5) pass with no edit; solution 2921, 0 failed, 3 skipped. The frame read and the queries allocate nothing: plant 19 fails the grown gate with 4800 bytes over the 200 updates of `TheTypedReadAndTheQueriesAllocateNothing` (2400 in the four older gates).

### Phase 5 group 1 (the HelloPanel move label)
1. The render clamp of `MoveText`: the method sends each component through `StickComponent`, exactly as task 5.2 names. `StickComponent` clamps to -1 to 1, so the written text is never longer than `LongestMoveText` (18 bytes), and the 32-byte `moveScratch` and the label start text keep the no-growth invariant. The `Move` action of the sample holds only clamped bindings (a `KeyVector` and a stick), so its value is already inside that range, and no true value hides behind the clamp here. A bigger component, from a free-sum binding that a later sample might add, renders as `1.00` or `-1.00`. The remarks of `MoveText` say this in words. The alternative in the brief (a -9.99 to 9.99 clamp) needs a second component helper. The task names `StickComponent`, so the sample uses it.
2. Stale `PushGamepad` wording in remarks (a deviation inside the update-remarks scope): phase 2 deleted the push call from `Program.cs` but not the two sentences that describe it. Those are the build-time rule paragraph of `HelloState.cs` ("The frame loop only calls `UiActionMap.PushGamepad`") and the `state` parameter doc of `RunFrames` in `Program.cs` ("The loop pushes the gamepad buttons into its action map"). Both paragraphs are in the update-remarks scope of this group, so each now names the frame-read rule instead. The read slot of the active map reads the keys, the buttons, and the pad of slot 0 in `Update` (Q4). No behavior line changed.
3. `BindKeyVector(MoveAction, W, A, S, D)` maps to `(up, left, down, right)`, so W gives `Move: 0.00, 1.00` and W plus D give `Move: 0.71, 0.71` (the demo text of the plan). The `GlfwKey` names `W`, `A`, `S`, and `D` compile in this BRUTAL version. The sample build proves it.
4. The move label follows the stick label pattern member for member: `MovePrefix` (`"Move: "u8`), `MoveSeparator` (`", "u8`), `LongestMoveText` (`"Move: -1.00, -1.00"u8`), one `InvalidOperationException` guard per failed write, and a return of the written part. The class remarks of `HelloScreen` now say five labels in the pick rule, the draw-command rule, and the child-rule paragraph, and hold a new fifth-label paragraph. The factory of `HelloModes` writes five out fields.
5. The write line sits right after the stick label line, inside the UI region, and reads `state.Map.ReadVector(HelloState.MoveAction)` after `ui.Update`, so the map holds the value of this frame. `ReadVector` on the active map copies one `float2` and allocates nothing. The gate measures the line.

### Phase 5 group 2 (the docs)
1. Source of the section 9 rules: the class remarks of `UiActionMap` are the authority, as the brief names. The doc restates them in short named rules, and keeps the value table and the finish formulas of the plan in the "The Value Rules" shape. Where the remarks and the old doc disagreed, the remarks won. The old **The gamepad edge.** bullet described the `PushGamepad` edge only. The new **The gamepad rule.** names the frame read of `GamepadSlot`, the invoke pass, and the do-not-push warning for an active map.
2. Kept old rules: the modifier rule, the repeat rule, the text guard, the mouse rule, the one-map rule, `FocusCommands`, `PollGamepad`, the build rules, the error rule, and the cost all still hold, so section 9 keeps them with the typed additions. The old **The actions.** and **The bindings.** bullets were split into **The action types.**, **The binds.**, and **The queries.** to cover the new members. The one-map rule now names the second slot and the `UiManager` mode switch, because the "waits for panels-and-modes" note of the old text is stale.
3. The gate limit of the press rule stays in the doc (Notes conflict 2, option A): a focused element that is not a text field closes the key gate, and a key that no element took still reaches the slot, so its command runs while `WasPressed` reads `false`.
4. Section 4 scope: item 2 of `### Frame order` now names the read slot between the input pass and the binding pass, the pad read there, and the drop of the push. The overview diagram of the same section gained one line for the read slot, because item 2 and the diagram must agree. This is one line outside the words of the brief, and it stays inside section 4.
5. Section 14 counts: the gate counts of the brief match the recorded totals exactly (`InputReadTests` 6, `TypedActionMapTests` 9, `GamepadValueActionTests` 8, `ActionQueryTests` 10, `ActionMapSwitchTests` 6, `ActionMapAllocationTests` 6, plants 1 to 20). The seventeenth gate bullet was grown in place (5 tests to 6 tests, 18 actions to 23 actions, 19 bindings to 28 bindings, four plants to six plants), because the line "the growth of the seventeenth gate" in task 5.6 makes that bullet the target. Plant 15 is recorded as exit 0, the deviation of decision 3.
6. Sample bullet: "the three label updates" became "the four label updates" (the gate, input, and stick labels, plus the new move label), and the phrase "the gamepad push from the slot" left the UI-region list, because the sample dropped the push. The move label and plant 20 are named there.
7. `CHANGELOG.md` shape: five new **Added** bullets (the slot and delegate, the enums and typed add, the six binds, `GamepadSlot`, the five queries) and three new **Changed** bullets (the pad read and the push drop, the three type checks, the HelloPanel move label). Each names `(input/input-actions)`, in the pattern of the earlier features.
8. The prose pass of this file: the lint target of the brief (`--fail-over 2.5`) needed the earlier session notes rewritten. Every fact, number, file name, command, and test name is kept. Only the sentence shape changed: semicolons to periods, and long sentences split (the input-state Decision 31 pattern). No note was deleted.
9. `implementation.md` needed no edit: the Notes conflicts 1 to 8 are each resolved the way the plan chose (option A or the stated plan decision), so the plan body stays as is. This is the rule of the brief: record in `context.md` what a task does not name.

---

## Plan Reference (Phase 1 and Phase 2)

Copied from `implementation.md`, Architecture Overview. Sections: "The Value Rules", "The Frame Read", "The Internal Shape".

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

### The Internal Shape

- **The partial files (phase 1).** `public sealed partial class UiActionMap` in three files. `UiActionMap.cs` holds the class remarks, the constants, the fields, the constructor, `Commands`, the counts, `GamepadSlot`, the queries, `Activate`, `IsActive`, `Deactivate`, the reset, `InputKind`, `ActionEntry`, `InputBinding`, and `CreateInfo`. `UiActionMap.Bindings.cs` holds both `AddAction` overloads, each bind, `Find`, the value find, `IndexOfAction`, the type check, and the append. `UiActionMap.Read.cs` holds `HandleInput`, `IsTypedByTextField`, `ReadFrame`, `PushGamepad`, `InvokeAction`, `PressKey`, `PressMouseButton`, and `Invoke`.
- **`InputKind` (private).** `Key`, `MouseButton`, `GamepadButton` stay with their values. New: `GamepadStick`, `GamepadAxis`, `KeyAxis`, `KeyVector`, `MouseDelta`, `Scroll`. One `GamepadAxis` kind covers the trigger and the stick axis, because `UiGamepadAxis` names both.
- **`InputBinding` (private).** `Kind`, `Code`, `Code1`, `Code2`, `Code3`, `Modifiers`, `Scale`, `Action`. `Code` holds the key, the button, the bit, the stick, or the axis. A `KeyAxis` holds the negative key in `Code` and the positive key in `Code1`. A `KeyVector` holds up, left, down, and right in `Code` to `Code3`. An unused code is -1. Each old bind writes `Scale = 1`. The shape gives one text line for each binding (binding-remap).
- **`ActionEntry` (private).** `Name`, `Command` (an empty array for an action with no command), `Type`, `Value` (`float2`), `Pressed`, `Held`, `Released`, and the frame scratch `Sum`, `Free`, `Down`, `Press`. The read writes each entry through `ref ActionEntry a = ref actions[i]`.
- **The type check.** A private helper takes the action index and the type that the bind needs. It throws `ArgumentException(message, "action")` on a mismatch. Each bind checks the arguments first (`ArgumentOutOfRangeException`), then the action name, then the type, then a duplicate. A rejected call changes no count.
- **The slot handlers.** `_onInput` stays. `_onRead = ReadFrame` is new, with the same build-time rule.
- **The queries.** Each query finds the index with `IndexOfAction`. A name that the map does not hold returns `false` or zero. A wrong type throws `ArgumentException` with the parameter name `action`, also on a map that is not active. From phase 4, a map that is not active returns `false` or zero.
- **`float2` math.** The code uses `new float2(x, y)`, `.X`, `.Y`, `+`, and `MathF.Sqrt`. Each float operation works on the components.

---

## Gotchas & Learnings

### Inherited from input-state / gamepad-axes
1. Every build and test command sets `MSBUILDDISABLENODEREUSE=1`. Pass `-nodeReuse:false` to `dotnet build` only. Never pass it to `dotnet test` (exit 5, "Zero tests ran"). Never run `dotnet build-server shutdown`.
2. Known flakes: a test host can exit with 134 (Internal CLR error 0x80131506). `TheCurvesAllocateNothing` failed once with 8128 bytes. Rerun once before you treat either as a fault. A plant that does not compile leaves the old binary, which passes, so check `0 Error(s)` before you read a plant result.
3. The agent bash sandbox blocks the X11 socket. `scripts/test-linux.sh` fails at its smoke step with `HelloPanel: GLFW init failed: PlatformUnavailable`, and display-window demos cannot run here. Record the exact line, and hand the PASS runs and window demos to Needs Human Verification.
4. Plant copies live under `src/<Project>/obj/plantscratch/` (git-ignored). `GC.KeepAlive(new object());` needs `using System;` in its file. Restore with `cp`, and verify with `cmp`.

---

## Testing Approach

- `Core.Tests/Input/InputReadTests` (6), `Framework.Tests/Actions/TypedActionMapTests` (9), `GamepadValueActionTests` (8), `ActionQueryTests` (10), `ActionMapSwitchTests` (6), `ActionMapAllocationTests` grown to 6. Plants 1 to 20 guard each rule. Test style follows `UiActionMapTests`: static handler fields, hand-built events, a 400x300 viewport, and `Assert.Equal(expected, actual, 5)` per float component. GLFW codes: W 87, A 65, S 83, D 68, Space 32, Escape 256.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs the Framework test binary with a filter on the typed map tests and reads a total with `failed: 0`. The tests push W on a W,A,S,D Vector2 and read (0, 1) through the new Core slot.
- [ ] Phase 2 demo: a developer runs the built Framework test binary with `--filter-class '*GamepadValueActionTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 3 demo: a developer runs the built Framework test binary with `--filter-class '*ActionQueryTests'` and reads a total of 10 with `failed: 0`. The tests read `WasPressed`, `IsHeld`, and `WasReleased` after one key, pad, and mouse press, and read (20, -15) from a pointer move of (40, -30) with scale 0.5.
- [ ] Phase 4 demo: a developer runs the built Framework test binary with `--filter-class '*ActionMapSwitchTests'` and reads a total of 6 with `failed: 0`, then with `--filter-class '*ActionMapAllocationTests'` and reads a total of 6 with `failed: 0`.
- [ ] Phase 2 task 2.9 on a host with a window: a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads the PASS line with `exit=0`. The run of the agent failed at smoke with `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` and `exit=1`; this is Gotcha 3, not a code fault. With the same script, plant 10 (`GC.KeepAlive(new object());` as the first line of `ReadFrame`) must show a non-zero byte count on smoke line 3 and exit code 1. The sandbox proved the plant with the `*ActionMapAllocationTests` class run (exit 2, 4 failed). The smoke byte measure still needs the window host.
- [ ] Phase 5 task 5.4 on a host with a window: a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads the PASS line with `exit=0`. The clean run of the agent and the plant 20 run both stopped at smoke with `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` and `exit=1` (Gotcha 3). With the same script, plant 20 (`GC.KeepAlive(new object());` as the first line of `HelloScreen.MoveText`) must show a non-zero byte count on smoke line 3 and exit code 1. The plant builds `0 Error(s)` in the sandbox. The byte measure needs the window host.
- [ ] Phase 5 group 2 smoke rerun on a host with a window: a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` on the final docs tree and reads the PASS line with `exit=0`. The run of group 2 stopped at the same X11 line (Gotcha 3). Its first attempt also hit the known `TheCurvesAllocateNothing` flake (Gotcha 2) before a clean rerun.
- [ ] Phase 5 demo on a host with a window: a developer holds W and reads `Move: 0.00, 1.00` in the fifth label, holds W and D and reads `Move: 0.71, 0.71`, and pushes the left stick of slot 0 full up and reads `Move: 0.00, 1.00`.
- [ ] Follow-up outside this repo (spacegame): a person moves the spacegame submodule to the commit of this feature after the merge, then continues the map-controls feature of the spacegame map-view epic.

---

## Session Notes

### 2026-11-09 17:55 - Run end: review closed, final verifier PASS, docs rolled up
- `/autorun-feature input/input-actions` reached the end of its loop. 41/41 tasks. Five phase gates PASS. The cross-phase review closed: 13 findings, 11 fixed in four review groups, CR-4 and CR-5 deferred with written reason to control-schemes and binding-remap. The final verifier PASSED after one fix round on gate 8 (the prose of `code-review.md` read 2.61 against the 2.5 bar; the sweep took it to 0.11 with every fact kept).
- Final measured state: build `0 Warning(s)` / `0 Error(s)`. Solution 2921 total, 0 failed, 3 skipped (the three base display tests). `BrutalUi.Framework.Tests` 259 (base 225 + 34). `BrutalUi.Core.Tests` 1801 (base 1795 + 6). Class totals: 6, 9, 8, 10, 6, 6 for `InputReadTests`, `TypedActionMapTests`, `GamepadValueActionTests`, `ActionQueryTests`, `ActionMapSwitchTests`, `ActionMapAllocationTests`. Plants 1 to 20 landed; plant 15 exits 0 (accepted defensive-call deviation, decision 3). Lint exit 0 over five files: 1.47, 1.47, 2.42, 1.77, 0.11.
- Scope holds the plan: Core diff is `UiContext.cs` (56+/2-) plus the new delegate file; `UiInputState.cs`, `InputStateWriter.cs`, `UiGamepadState.cs`, `UiGamepadDeadzone.cs`, `UiEventEnums.cs`, `src/BrutalUi.Glfw/`, and `UiManager.cs` show no diff; `UiActionMap` splits into three partial files; the sample change is the push drop plus the Move label; no project file, no `scripts/` change, no native dependency, no plant residue.
- Deviations from the plan, all recorded: `UiActions.cs` one doc-comment line (decision 10, now an exception in the plan line 132); the `ATriggerFillsAnAxis` two-map adaptation (decision 6); plant 15 exit 0 (decision 3); the `ActionMapAllocationTests` real-work fixed amount 500.25 (decision 5); review groups 1 and 2 corrected two stale `PushGamepad` remarks and the class build-rules paragraph.
- Inbox: 1 line, closed (the phase 3 verifier gate 6 brief error). Inbox bug: Verifier FAIL phase-3 gate 6 (done: brief gates 6 and 7 corrected, rerun PASS). No open line, so no `docs/bugs/` promotion at the feature end.
- Not committed on purpose. The branch `feat/input/input-actions` (base `14c4b0c`) holds the work untracked-or-modified in the tree; the user runs `git add`, commits, pushes, and opens the PR.
- Next for a person: the Needs Human Verification list (five phase demos, the smoke PASS run, the plant 10 and plant 20 byte measures, the Move-label window demo), then `/evaluate-feature input/input-actions`.
- Next for the epic: `input/control-schemes` plans from the review notes (CR-4 scheme shape, CR-5 binding handle, and the set-based duplicate rule that binding-remap reuses). Spacegame moves its submodule to this commit and continues map-controls.

### 2026-11-09 17:20 UTC - Review group 4 (STANDARD, CR-8, CR-12, docs only)
- Base: the cross-phase review of `code-review.md`. Review groups 1 to 3 had landed. No earlier agent changed these docs for them.
- CR-8 (docs): the **Typed action tests.** bullet of section 14 of `docs/technical-design.md` now states what each class checks. The `ActionMapSwitchTests` sentence drops the wrong-type claim. `AReplacedMapReadsZero` is named as the check that a replaced map reads `false` or zero, also when a game clears the read slot by hand. The `ActionQueryTests` sentence adds the mouse limit in both directions, the modifier drop of a held plain-key bind, and the wrong-type throw on a map that is not active: the map of `TheButtonQueriesCheckTheType` never gets a context. `TypedActionMapTests.ReadVectorChecksTheType` is named for `ReadVector` in the same case.
- CR-8 (confirm): the body of `TheButtonQueriesCheckTheType` was re-read. It runs every wrong-type query on a never-activated map, so the type check before the inactive default is pinned for the four queries there. No test body was extended. The class stays 10, and the test gates of the brief (build 0/0, solution 2921) are not triggered.
- Section 9 added task: three rules review group 1 shipped now sit in the action map section. The press rule names the mouse limit next to the gate limit. The modifier rule states the drop: a modifier press during a held plain-key bind stops the match, the latch releases the action, and a fresh press is needed. The switch rule states the pair test: `IsActive` needs both slots, or the map is not active, and the frame read starts with the same test.
- CR-12 (docs): the "These files do not change" line of `implementation.md` now carries the recorded exception: `src/BrutalUi.Framework/Actions/UiActions.cs` holds one doc comment line for the cref split of the two `AddAction` overloads, with no code line changed (deviation decision 10).
- CHANGELOG added task: `## [Unreleased]` → `### Changed` gained one bullet: a `KeyAxis` pair and a `KeyVector` key set bound in any order is one binding and a repeat throws, and `BindKey` and `BindMouseButton` reject codes above the state range at bind time (input/input-actions).
- Gates: ste-lint `--fail-over 2.5` exit 0 on all three docs: technical-design.md 1.47 per 100 words, implementation.md 1.77, CHANGELOG.md 1.47. Sanity build `0 Warning(s)`, `0 Error(s)`. No tests were run: no test or source file changed.
- Files: `docs/technical-design.md`, `docs/features/input/input-actions/implementation.md`, `CHANGELOG.md`, `docs/features/input/input-actions/code-review.md` (two ticks, header 11 resolved), this file.
- Deviations: none from the brief. One note for a later group: the **The build rules.** bullet of section 9 still carries the pre-group-2 sentence "throw for a code below 0" for `BindKey` and `BindMouseButton`, and does not name the set compare of a composite. The group 2 follow-up of this brief owned the CHANGELOG line only.
- Tool use: about 15 tool calls, under the 100-call stop rule.
- Open questions: none. CR-4 and CR-5 stay open.

### 2026-11-09 17:00 UTC - Review group 3 (STANDARD, CR-7, CR-9)
- Base: the cross-phase review of `code-review.md`. Start totals: solution 2921, 0 failed, 3 skipped; review groups 1 and 2 had landed (pair guard, modifier pins, composite set rule, upper bounds).
- CR-7 (test): `AGamepadPressInvokesTheCommandFromTheFrameRead` now ends with the double-push frame. A clear frame (`SetPad(None)`, `Update`) resets the state edge, then in one frame `PushGamepad(B)` (asserted to run one command) and `SetGamepad(0, B)` with `Update`: the counter reads 4, the two calls of the double frame (once per path), and `WasPressed` reads `true` once. The test summary names the Decision 9 limit. No src change; the `PushGamepad` remarks already state it.
- CR-9 (test): `TheGamepadSlotSelectsThePad` now also binds `Cancel` on B (the map's `Commands.Register` after `CreateMap`) with a `Counter`, and with `GamepadSlot = 2`: a B press in slot 0 updates nothing (0 calls), a B press in slot 2 invokes once, and after both slots clear, a fresh slot-0 press still invokes nothing. The summary names the slot as the pad source of the invoke pass.
- Gates: build `0 Warning(s)`, `0 Error(s)`. Solution `dotnet test --solution brutal-ui.sln`: total 2921, failed 0, succeeded 2918, skipped 3, exit 0. `*GamepadValueActionTests`: total 8, failed 0, exit 0 (direct run of the built assembly).
- Files: `tests/BrutalUi.Framework.Tests/Actions/GamepadValueActionTests.cs`, `docs/features/input/input-actions/code-review.md` (two ticks, header 9 resolved), this file. No `src/` change, no method added, class stays 8.
- Deviations: CR-7's finding asks for a new test named in the `PushGamepad` remarks; the brief overrides both: the assert block extends the existing press test, and the remarks stay unchanged (no src edit in this group). The finding's "assert `WasPressed` reads true once" is pinned with the one `Assert.True`.
- Tool use: about 20 tool calls, under the 100-call stop rule. No plant was run for this group.
- Open questions: none. CR-4, CR-5, CR-8, and CR-12 stay open for the later review groups.

### 2026-11-09 16:55 UTC - Review group 2 (STANDARD, CR-3, CR-6, CR-11, CR-13)
- Base: the cross-phase review of `code-review.md`. Start totals: solution 2921, 0 failed, 3 skipped; review group 1 had landed (the `IsActive` pair test, the `ReadFrame` pair guard, and the modifier limit pins).
- CR-3 (fix): `FindValue` compares the codes of a composite as a set. `InputKind.KeyAxis` matches the two orders of one pair, and `InputKind.KeyVector` matches any order of the same four keys, through the new private static helpers `SamePair`, `SameSet`, and `HasCode` (no allocation, build-time path only). `Key`, `MouseButton`, `GamepadButton`, stick, axis, delta, and scroll keep the exact compare. The `FindValue` summary, and the `BindKeyVector` and `BindKeyAxis` exception docs, now state the set rule. The duplicate rule stays a throw, and the swapped `KeyAxis` throw still names the same parameter as before (`action`).
- CR-3 (tests): `TypedActionMapTests.BindKeyVectorChecksTheKeys` now also rejects (D, S, A, W) and (A, S, D, W) after the (W, A, S, D) bind, with the `action` parameter name, and still ends at 1 action and 1 binding. `ActionQueryTests.ATwoKeyAxisReadsMinusOneToOne` now throws on the swapped pair `BindKeyAxis(Throttle, W, S)` with the `action` name, and pins `ActionCount` 1 and `BindingCount` 1. No method added; the classes stay 9 and 10.
- CR-6 (fix): `BindKey` checks `(uint)key >= UiKeyState.KeyCount`, and `BindMouseButton` checks `(int)button < 0 || (int)button >= UiPointerButtonState.ButtonCount`, the same bound the state masks use. Each throws `ArgumentOutOfRangeException` with the parameter name of the key or the button. The two `<exception>` lines and the build-rules paragraph of the class remarks now state the two ranges.
- CR-6 (test): `TypedActionMapTests.TheBuildChecksTheActionType` now asserts the upper bound of both single binds (key 349 gives `key`, button value 8 gives `button`) before the type-check asserts, and its end still pins the unchanged counts.
- CR-11 (fix): one `private const string ActionParameter = "action";` sits next to `MatchMask` in `UiActionMap.cs`, with a remark that names it the public parameter name of every bind and query. `CheckActionType` throws with it, so no string literal is left in the helper.
- CR-13 (fix): each `AddAction` summary now names the `default` ambiguity and the way out: the command span adds a Button with a command, the `UiActionType` value adds an action with no command, and `UiActionType.Button` adds a Button with no command.
- Gates: build `0 Warning(s)`, `0 Error(s)`. `dotnet test --solution brutal-ui.sln`: total 2921, failed 0, succeeded 2918, skipped 3. Classes, each 0 failed: `*TypedActionMapTests` 9, `*ActionQueryTests` 10, `*UiActionMapTests` 23, `*GamepadActionTests` 7, `*ActionMapAllocationTests` 6.
- Files: `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs`, `src/BrutalUi.Framework/Actions/UiActionMap.cs`, `tests/BrutalUi.Framework.Tests/Actions/TypedActionMapTests.cs`, `tests/BrutalUi.Framework.Tests/Actions/ActionQueryTests.cs`, `docs/features/input/input-actions/code-review.md` (four ticks, header 7 resolved), this file.
- Deviations: the brief's Files list names `UiActionMap.Bindings.cs` only for the source, and this group also grew `UiActionMap.cs`, because CR-11's fix names that file for the constant and because the class remarks hold the two build rules that CR-3 and CR-6 changed. The `CHANGELOG.md` note that CR-6's fix asks for was skipped: the file is outside the brief's list, and no review group owns it. Class totals came from `dotnet test --project tests/BrutalUi.Framework.Tests --no-build --filter`, which ran clean here.
- Tool use: about 30 tool calls, under the 100-call stop rule. No plant was run for this group.
- Open questions: none. CR-4, CR-5, CR-7, CR-8, CR-9, and CR-12 stay open for the later review groups.

### 2026-11-09 16:36 UTC - Review group 1 (STANDARD, CR-1, CR-2, CR-10)
- Base: the cross-phase review of `code-review.md` (13 findings). Start totals: solution 2921, 0 failed, 3 skipped. No source file was changed for these findings before this group.
- CR-1 (fix): `IsActive` now tests the pair, `context.OnUnhandledInput == _onInput && context.OnInputRead == _onRead`. `ReadFrame` starts with the matching guard, `if (context.OnUnhandledInput != _onInput) return;`, one read and one compare, before the locals copy, so a broken pair gives no work, no values, and no invoke. The `IsActive` remarks state the pair, the switch-rule paragraph adds the pair test sentence ("a hand-written slot cannot leave a map active with frozen values"), and the `ReadFrame` remarks state that the read pass follows the unhandled slot.
- CR-1 (test): the body of `ActionMapSwitchTests.AReplacedMapReadsZero` now also clears `context.OnInputRead` by hand and keeps the unhandled slot; the map then reads `IsActive` false, each Button flag false, and `ReadVector` zero, before and after another `Update`. No test method was added; the class stays 6.
- CR-2 (decision: keep Decision 8): the press rule of the class remarks now names the mouse limit next to the gate limit, and `TheModifierRuleAppliesToTheQueries` pins both directions: a Ctrl+right press on a Control mouse bind runs the command and reads no press flag; a Ctrl+right press on a `None` mouse bind runs no command and reads the press. No test method was added; the class stays 10.
- CR-10 (decision: keep the rule): the `BindKey` remarks state that a modifier change during a held plain-key bind stops the match, the latch releases the action, and a fresh press is needed. `TheModifierRuleAppliesToTheQueries` holds Space on a `None` bind, presses Control, reads the release, releases Control, and reads that only a fresh Space press holds again.
- Test helper note: the `Pointer` helper of `ActionQueryTests` gained an optional `UiModifiers modifiers` parameter (default `UiModifiers.None`), and the class gained the `ControlKey = 341` const. No existing call changed.
- Gates: build `0 Warning(s)`, `0 Error(s)`. Solution `dotnet test --solution brutal-ui.sln`: total 2921, failed 0, succeeded 2918, skipped 3. Classes: `*ActionMapSwitchTests` 6, `*ActionQueryTests` 10, `*TypedActionMapTests` 9, `*ActionMapAllocationTests` 6, each 0 failed.
- Deviations: `dotnet test --project <csproj> --no-build` reported a testhost handshake failure (exit 5, zero tests ran) in this environment, so each class total came from a direct run of the built assembly (`dotnet <Tests.dll> --filter ...`). The brief's status header of `code-review.md` was set to "3 resolved" to match the three new ticks.
- Tool use: about 30 tool calls, under the 100-call stop rule.
- Open questions: none. CR-3 to CR-9 and CR-11 to CR-13 stay open for the later review groups.

### 2026-11-09 16:20 UTC - Phase 5 group 2 (STANDARD, the docs)
- Base: phases 1 to 4 complete and gate-verified, and phase 5 group 1 done. Start totals: solution 2921, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 259. No earlier agent changed a file for this group. The worktree held the phase 1 to 5 group 1 changes.
- Task 5.5 (section 9 of `docs/technical-design.md`): the new item 13 "The read slot" after item 12. It names the delegate type, the place after the input pass and before the binding pass, the `Activate` write, the `=` rule, the null rule, the re-entry throw, the exception rule, the build-time rule, and `InputReadTests`.
- `### Action map` was rewritten for the typed map: the action types, the binds with the value table and the finish formulas of the plan, the sum and the clamp, the free sum, the Y-up rule, the Button latch, the press rule with the gate limit, the gamepad rule (`GamepadSlot`, the invoke from the frame read, the do-not-push rule), `PollGamepad`, the modifier, repeat, text guard, and mouse rules, the five queries, the switch rule, the one-map rule with the two slots and the `UiManager` switch, `FocusCommands`, the build rules with the type checks, the error rule, and the cost.
- `TypedActionMapTests`, `GamepadValueActionTests`, `ActionQueryTests`, and `ActionMapSwitchTests` are named in the lead of the section.
- Task 5.6 (sections 4 and 14): item 2 of section 4 names the read slot after the input pass and before the binding pass, the pad read of an active map there, and the drop of the push. The overview diagram gained the matching "Read slot: OnInputRead" line (decision 4).
- Section 14: the **Action map tests.** bullet now names the typed read and the grown seventeenth gate. The new **Typed action tests.** bullet holds the four new classes, `InputReadTests`, and plants 1 to 20 with their recorded results.
- The seventeenth gate bullet grew (6 tests, 23 actions, 28 bindings, six plants). The sample bullet names the move label, the four label updates, and the drop of the push (decision 5 and 6).
- Task 5.7 (`CHANGELOG.md`): five **Added** bullets and three **Changed** bullets, each tagged `(input/input-actions)` (decision 7).
- Task 5.8 (the gates):
  - Build: `dotnet build brutal-ui.sln -nodeReuse:false` gave `0 Warning(s)`, `0 Error(s)`.
  - Solution: `dotnet test --solution brutal-ui.sln` gave total 2921, failed 0, succeeded 2918, skipped 3, exit 0. No test changed, as a docs group must do.
  - Demos (the built binaries, class filters): `*InputReadTests` 6, `*TypedActionMapTests` 9, `*GamepadValueActionTests` 8, `*ActionQueryTests` 10, `*ActionMapSwitchTests` 6, `*ActionMapAllocationTests` 6. Each gave `failed: 0`.
  - Smoke: `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` first stopped in the test step with the known flake `failed BrutalUi.Core.Tests.TransitionCurveTests.TheCurvesAllocateNothing` (Gotcha 2), so the group reran it once.
  - The rerun passed the build and test steps (total 2921, failed 0, succeeded 2918, skipped 3) and stopped at smoke with `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` and `test-linux: FAIL at smoke (exit 1)`, `exit=1`.
  - This is the known sandbox block (Gotcha 3), not a code fault. No escalation was asked. The PASS run goes to Needs Human Verification.
  - Lint `--fail-over 2.5`: technical-design.md, CHANGELOG.md, this file, and `implementation.md` each pass. This file needed the prose pass of decision 8 before it passed.
  - Scope: only `docs/technical-design.md`, `CHANGELOG.md`, this file, and `tasks.md` changed. `src/`, `samples/`, `tests/`, `scripts/`, project files, and `docs/features/input/epic-overview.md` show no diff from this group.
- Deviations from the brief: two, both small. The section 4 overview diagram gained the read-slot line (decision 4). The seventeenth gate bullet was grown in place (decision 5). `implementation.md` needed no edit (decision 9).
- Decisions that the plan did not settle, recorded here: the gate limit stays in the doc with A1 (Notes conflict 2, option A). The Y-up flip and the mouse Y-down stay as the class remarks state (Notes conflict 4, Decision 6).
- `PushGamepad` keeps its body for an inactive map, and the doc states the do-not-push rule (Notes conflict 3, Decision 9). A frame with no poll repeats the last press edge, and section 9 now states it (Notes conflict 7).
- One kind `GamepadAxis` covers the trigger and the stick axis (Notes conflict 5). A Button action may hold no command (Notes conflict 6).
- `UiManager` never calls `Deactivate` on a replaced map, and the switch rule covers it (Notes conflict 1, Decision 11). The plan compiled and the lint ran, so Notes conflict 8 is closed.
- Open questions: none. No public shape changed in this group.
- Migration risk and follow-up: none for the code. spacegame moves its submodule to the commit of this feature and continues map-controls. `/update-epic` and `/update-master` own the rollups.
- Tool use: about 60 tool calls, under the 100-call stop rule.

### 2026-11-09 16:10 UTC - Phase 5 group 1 (STANDARD, the HelloPanel move label)
- Base: phases 1 to 4 complete and gate-verified. Start totals: solution 2921, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 259. No earlier agent changed a file for phase 5. The worktree held the phase 1 to 4 changes.
- Task 5.1 (`HelloState.cs`): the `MoveAction` static name, the Vector2 action, `BindKeyVector` with `(int)GlfwKey.W`, `.A`, `.S`, `.D` (the names compile), and `BindGamepadStick` with `UiGamepadStick.Left`, right after the three old Button actions and their binds (A8). The class remarks gained the `Move` sentence, and the build-time rule paragraph now names the frame-read rule (decision 2). The sample project built `0 Warning(s)`, `0 Error(s)` after the edit.
- Task 5.2 (`HelloScreen.cs` and `HelloModes.cs`): `BuildWidgetPanel` gained `out Label moveLabel` after `stickLabel`, the label creation with `LongestMoveText` right after the stick label, and the child add right after the stick label. The new `MoveText(float2, Span<byte>)`, `MovePrefix`, `MoveSeparator`, and `LongestMoveText` copy the `StickText` pattern (decision 1 and 4). The class remarks say five labels in the pick rule, the draw-command rule, and the child-rule paragraph, and gained the fifth-label paragraph. `HelloModes` gained `_moveLabel`, `MoveLabel`, the constructor check, the exception-doc member, and "five out fields".
- Task 5.3 (`Program.cs`): the call site passes `modes.MoveLabel`; `RunFrames` gained the `Label moveLabel` parameter and doc after `stickLabel`; `byte[] moveScratch = new byte[32];` sits next to `stickScratch`. The write line sits right after the stick label line, inside the UI region (decision 5). The `RunFrames` remarks name the move label in the step (3) label list and in the gate-counts paragraph, and the `state` parameter doc now names the frame-read rule instead of the deleted push (decision 2).
- Task 5.4 (the gates):
  - Build: `dotnet build brutal-ui.sln -nodeReuse:false` gave `0 Warning(s)`, `0 Error(s)`.
  - Solution: `dotnet test --solution brutal-ui.sln` gave total 2921, failed 0, succeeded 2918, skipped 3, exit 0. No test changed. The sample is not a test project, so the total stays the phase 4 number.
  - Smoke: `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` printed `test-linux: FAIL at smoke (exit 1)`, `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, and `exit=1`. Known sandbox block (Gotcha 3); not a code fault; no escalation. The script passed its build and test steps before smoke. PASS run handed to Needs Human Verification.
  - Plant 20 (`GC.KeepAlive(new object());` as the first line of `HelloScreen.MoveText`; `using System;` already at line 1): copy to `samples/HelloPanel/obj/plantscratch/`, solution build `0 Warning(s)`, `0 Error(s)`, smoke attempt stopped at the same X11 line (the byte measure needs the window host), restore with `cp`, `cmp` equal, `grep -c GC.KeepAlive` gives 0. Post-restore build `0 Warning(s)`, `0 Error(s)`.
  - Scope: `git status --porcelain samples/` shows exactly the four sample files. `src/`, `tests/`, `scripts/`, and project files show no diff.
- Deviations from the brief: none beyond the smoke rule. Two remark sentences that phase 2 left stale (the `PushGamepad` wording) are inside the "update the remarks" scope of the tasks and now name the frame-read rule (decision 2). The smoke byte check of plant 20 is a window-host item (Needs Human Verification).
- Open questions: none. The public shape did not change. The sample uses only the phase 1 to 4 API.
- Migration risk and follow-up: group 2 wrote section 4, section 9, and section 14 of the technical design and the changelog. The switch rule and the value rules of the class remarks stayed the source text.
- Tool use: about 45 tool calls, under the 100-call stop rule. Plant scratch: `samples/HelloPanel/obj/plantscratch/HelloScreen.cs` (git-ignored).

### 2026-11-09 15:20 UTC - Phase 4 group 1 (ADVANCED, the switch rule and the allocation gate)
- Base: phases 1, 2, and 3 complete and gate-verified. Start totals: solution 2914, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 252; `ActionQueryTests` 10, `*ModeActionMapTests` 5. No earlier agent changed a file for phase 4. The worktree held the phase 1 to 3 changes.
- Tasks 4.1 and 4.2 (the map): `ResetFrame()` and the switch rule in `UiActionMap.cs` (decisions 1 to 3 and 6, 7). The build stayed `0 Warning(s)`, `0 Error(s)`, and the Framework run gave the same 252, 0 failed right after the map change: no existing test observed the guard or the reset as a break.
- Task 4.3 (`ActionMapSwitchTests`, 6 tests): the class passed on the first run (total 6, failed 0).
- Task 4.4 (`ActionMapAllocationTests` grown): the class ran 6 with one fix before green. The first `QueryStep` after the other bodies reads the move delta (260, 238) and the stale S of the key step, so the fixed amount is 500.25, not the resting-round value (decision 5).
- Task 4.5 (the gates and the plants):
  - Build: `0 Warning(s)`, `0 Error(s)`.
  - Demos: `--filter-class '*ActionMapSwitchTests'` total 6, failed 0, exit 0; `--filter-class '*ActionMapAllocationTests'` total 6, failed 0, exit 0.
  - Solution: `dotnet test --solution brutal-ui.sln` total 2921 (2914 + 7), failed 0, succeeded 2918, skipped 3, exit 0. `BrutalUi.Framework.Tests` 259 (252 + 7).
  - Unchanged classes: `ModeActionMapTests` 5, `ModeDiffTests` 9, `ActionQueryTests` 10, `UiActionMapTests` 23, `GamepadActionTests` 7, `TypedActionMapTests` 9, `GamepadValueActionTests` 8, `FocusActionTests` 5. Each passed with no edit.
  - Plant 15 (`Deactivate` skips `ResetFrame()`): build `0 Error(s)`, exit=0, total 259, failed 0. The planted change is not observable through the public API once the guard (4.2) and the fresh-`Activate` reset (4.1) land (decision 3). Deviation from the brief: the brief expects `DeactivateReleasesEachHeldAction` to fail. Restored, `cmp` equal.
  - Plant 16 (the five queries skip the `IsActive` check): build `0 Error(s)`, exit=2, total 259, failed 2 (`AReplacedMapReadsZero` and `AModeSwitchThroughTheManagerGivesNoStaleFlag`. The brief names either, and both fail). Restored, `cmp` equal.
  - Plant 17 (`Activate` resets also when `wasActive` is `true`): build `0 Error(s)`, exit=2, total 259, failed 1 (`TheSameMapInTwoModesKeepsAHeldAction`, as expected). Restored, `cmp` equal.
  - Plant 18 (the Button finish sets `Held = down`): build `0 Error(s)`, exit=2, total 259, failed 3: `ANewMapMidHoldSeesNoStaleFlag` (expected), plus `AModeSwitchThroughTheManagerGivesNoStaleFlag` and `AReplacedMapReadsZero` (both read a mid-hold latch). Restored, `cmp` equal.
  - Plant 19 (`GC.KeepAlive(new object());` as the first line of `ReadFrame`): build `0 Error(s)`, class run exit=2, total 6, failed 5: `TheTypedReadAndTheQueriesAllocateNothing` (expected, 4800 bytes over the 200 measured updates), plus `AMouseButtonOverNoElementAllocatesNothing` 2400, `TheFocusActionsAllocateNothing` 2400, `AGamepadPushAllocatesNothing` 2400, and `AnUnhandledKeyAllocatesNothing` 4800 (two updates per round in the key step). `TheMeasuredBodyDoesRealWork` does not measure and stayed green. Restored, `cmp` equal.
  - After each restore the build returned to `0 Warning(s)`, `0 Error(s)`. The final suite run above is the restored tree.
- Files: `src/BrutalUi.Framework/Actions/UiActionMap.cs`, new `tests/BrutalUi.Framework.Tests/Actions/ActionMapSwitchTests.cs`, grown `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs`, and these docs. `UiActionMap.Read.cs` restored equal under `cmp`: no net change. `git status` shows the phase 1 to 3 changes plus these three. `UiManager.cs`, `UiActions.cs` (no new change), `src/BrutalUi.Core/`, `src/BrutalUi.Glfw/`, `samples/`, and `scripts/` show no new diff.
- Deviations from the brief: one. Plant 15 exits 0 (decision 3): the expected failure of the brief cannot exist with the specified guard and reset, so acceptance criterion 2 holds for plants 16 to 19 only. `UiActionMap.Read.cs` is in the file list of the brief but needed no net change.
- Open questions: none. The public shape did not change; `ResetFrame` is private.
- Migration risk and follow-up: phase 5 wrote the HelloPanel move label and section 4, 9, and 14 of the technical design and the changelog. The switch rule text of the class remarks is the source for section 9. The verifier should note decision 3 for the plant 15 line of acceptance.
- Tool use: about 70 tool calls, under the 100-call stop rule. Plant scratch: `src/BrutalUi.Framework/obj/plantscratch/UiActionMap.cs`, `UiActionMap.Read.cs`, `p15.log` to `p19.log`, `p15-build.txt` to `p19-build.txt` (git-ignored).

### 2026-11-09 10:45 UTC - Phase 3 group 1 (STANDARD, the pointer values, the two-key axis, and the Button queries)
- Base: phases 1 and 2 complete and gate-verified. Start totals: solution 2904, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 242; `BrutalUi.Core.Tests` 1801.
- Tasks 3.1 to 3.5 (the map): `BindMouseDelta`, `BindScroll`, and `BindKeyAxis` in `UiActionMap.Bindings.cs`. The `KeyAxis`, `MouseDelta`, and `Scroll` `InputKind` members, the press rule, the grown value rule and build rules, and `WasPressed`, `IsHeld`, and `WasReleased` in `UiActionMap.cs`. The six new read cases, the Button finish, and the two locals in `UiActionMap.Read.cs`. The three cref docs in `UiActionEnums.cs` (decision 8). The build stayed at `0 Warning(s)`, `0 Error(s)` after each task.
- Task 3.6 (`ActionQueryTests`, 10 tests): the class passed on the first run (total 10, failed 0).
- Task 3.7 (the gates and the plants):
  - Build: `0 Warning(s)`, `0 Error(s)`.
  - Demo: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*ActionQueryTests'` gave total 10, failed 0, exit 0.
  - Solution: `dotnet test --solution brutal-ui.sln` gave total 2914 (2904 + 10), failed 0, succeeded 2911, skipped 3, exit 0. `BrutalUi.Framework.Tests` 252; `BrutalUi.Core.Tests` 1801.
  - Unchanged classes: `UiActionMapTests` 23, `GamepadActionTests` 7, `TypedActionMapTests` 9, `GamepadValueActionTests` 8, `ActionMapAllocationTests` 5, `FocusActionTests` 5, `ModeActionMapTests` 5. Each passed with no edit.
  - Plant 11 (`a.Released = oldHeld && !a.Held` in the Button finish): build `0 Error(s)`, exit=2, total 10, failed 1 (`APressAndAReleaseInOneFrameSetBothFlags`, expected. The frame holds no old held flag, so the released flag reads false). Restored, `cmp` equal.
  - Plant 12 (the `Key` case drops the `mods == binding.Modifiers` guard): build `0 Error(s)`, exit=2, total 10, failed 1 (`TheModifierRuleAppliesToTheQueries`, expected). Restored, `cmp` equal.
  - Plant 13 (the `MouseDelta` case adds into `Sum`, the clamped sum): build `0 Error(s)`, exit=2, total 10, failed 1 (`TheMouseDeltaAndTheScrollHaveNoClamp`, expected; (20, -15) clamps to (0.8, -0.6)). Restored, `cmp` equal.
  - Plant 14 (the `Key` case sets the press from `Keys.IsDown`): build `0 Error(s)`, exit=2, total 10, failed 2: `AKeyPressInvokesOneTimeAndReadsOnePress` (expected. The held frame reads a new press) and `APressAndAReleaseInOneFrameSetBothFlags` (the input is up at the end of the frame, so the pressed flag reads false). `TheModifierRuleAppliesToTheQueries` stays green, because its modifier guard rejects that frame first. Restored, `cmp` equal.
  - After each restore the build returned to `0 Warning(s)`, `0 Error(s)`. The final suite run above is the restored tree.
- Files: `src/BrutalUi.Framework/Actions/UiActionMap.cs`, `UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, `UiActionEnums.cs` (docs only, decision 8), new `tests/BrutalUi.Framework.Tests/Actions/ActionQueryTests.cs`, and these docs. `git status --porcelain` shows no other new change. `UiManager.cs`, `UiActions.cs`, `src/BrutalUi.Core/`, `src/BrutalUi.Glfw/`, `samples/`, `scripts/`, project files, and each existing test class show no new diff.
- Smoke: not run. Phase 3 touches no sample and no window path, so the X11 block of Gotcha 3 does not apply here. The phase 2 smoke item stays open for a person.
- Plant scratch: `src/BrutalUi.Framework/obj/plantscratch/UiActionMap.Read.cs`, `p11.log` to `p14.log`, `p11-build.txt` to `p14-build.txt` (git-ignored).
- Deviations from the brief: one, inside the docs. `UiActionEnums.cs` is not in the file list of the brief. The three `UiActionType` member docs moved from `<c>` to crefs now that the six new public members exist (decision 8). No code line changed there.
- Open questions: none. The public shape follows `implementation.md`: `BindMouseDelta(ReadOnlySpan<byte>, float)`, `BindScroll(ReadOnlySpan<byte>, float)`, `BindKeyAxis(ReadOnlySpan<byte>, int, int)`, `WasPressed(ReadOnlySpan<byte>)`, `IsHeld(ReadOnlySpan<byte>)`, `WasReleased(ReadOnlySpan<byte>)`.
- Migration risk and follow-up: phase 4 added `ResetFrame`, the inactive rule on the five queries, `ActionMapSwitchTests` (6), and the growth of `ActionMapAllocationTests` to 6; phase 5 wrote the docs of section 9 and the changelog.
- Tool use: about 92 tool calls, under the 100-call stop rule.

### 2026-11-09 10:05 UTC - Phase 2 group 2 (STANDARD, the sample)
- Base: phase 2 group 1 landed. Start totals: solution 2904, 0 failed, 3 skipped. `ActionMapAllocationTests` 5. No committed change since group 1: the worktree holds the phase 1 and phase 2 group 1 edits plus the `Program.cs` of this group.
- Task 2.8 (`samples/HelloPanel/Program.cs`): removed `state.Map.PushGamepad(ui.Input.Gamepad(0).Buttons);` (was the first line of the UI region). Rewrote the comment before `input.PollGamepads()`, the comment at the start of the UI region, and the `RunFrames` remarks where they named the push (decisions 1 and 2). `grep -n 'PushGamepad' samples/HelloPanel/Program.cs` prints nothing (exit 1). The diff is 1 deletion plus 3 comment places; `input.PollGamepads()`, the stick label write, and the other three HelloPanel files are untouched. Build after the edit: `0 Warning(s)`, `0 Error(s)`.
- Task 2.9 (the gates):
  - Build: `0 Warning(s)`, `0 Error(s)`.
  - Solution: `dotnet test --solution brutal-ui.sln` total 2904 (same as the group 1 end total; no test changed), failed 0, succeeded 2901, skipped 3, exit 0. `ActionMapAllocationTests` 5/5 and `GamepadValueActionTests` passed inside it with no edit.
  - Smoke: `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` printed `test-linux: FAIL at smoke (exit 1)`, `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, and `exit=1`. Known sandbox block (Gotcha 3); not a code fault; no escalation. PASS run and the window-host byte measure handed to Needs Human Verification.
  - Plant 10 (`GC.KeepAlive(new object());` as the first line of `ReadFrame` in `UiActionMap.Read.cs`; `using System;` already at line 1): copy to `src/BrutalUi.Framework/obj/plantscratch/`, build `0 Warning(s)`, `0 Error(s)`, then `BrutalUi.Framework.Tests --filter-class '*ActionMapAllocationTests'` exit=2, total 5, failed 4: `AMouseButtonOverNoElementAllocatesNothing`, `TheFocusActionsAllocateNothing`, `AGamepadPushAllocatesNothing`, `AnUnhandledKeyAllocatesNothing` (decision 4). Restored with `cp`, `cmp` equal. Post-restore build `0 Warning(s)`, `0 Error(s)`. The final suite run above is the restored tree.
- Files: `samples/HelloPanel/Program.cs`, this `context.md`, and `tasks.md` only. `git status` shows no other new change. The body of `PushGamepad` shows no diff.
- Deviations from the brief: none. The plant proof of the brief names `TheGamepadStepAllocatesNothing`, a phase 4 test. The plant bit through the four existing gates of the same class (decision 4).
- Migration risk and follow-up: phase 5 added the `Move` label write and re-grew `ActionMapAllocationTests`. The smoke line 3 byte measure for plant 10 waits for a window host (Needs Human Verification).
- Tool use: about 30 tool calls, under the 100-call stop rule.

### 2026-11-09 10:20 UTC - Phase 2 group 1 (STANDARD, the gamepad values and the press)
- Base: phase 1 complete and gate-verified. Start totals: solution 2896, 0 failed, 3 skipped. `BrutalUi.Core.Tests` 1801; `BrutalUi.Framework.Tests` 234.
- Tasks 2.1 to 2.5 (the map): `UiGamepadStick` in `UiActionEnums.cs`; `BindGamepadStick` and `BindGamepadAxis` in `UiActionMap.Bindings.cs`; `GamepadSlot`, `ReadAxis`, the `GamepadStick` and `GamepadAxis` `InputKind` members, and the class remarks in `UiActionMap.cs`. The two read cases, the Axis finish, the invoke pass, and the new remarks in `UiActionMap.Read.cs`. The build stayed at `0 Warning(s)`, `0 Error(s)` after each task.
- Task 2.6 (`GamepadValueActionTests`, 8 tests): one test-side fix before green. `ATriggerFillsAnAxis` first bound the right trigger twice in one map, and the map-wide duplicate rule rejected the scale-2 bind. Split into two maps (decision 6). The class then passed 8/8.
- Task 2.7 (the final run and the plants):
  - Build: `0 Warning(s)`, `0 Error(s)`.
  - Demo: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*GamepadValueActionTests'` gave total 8, failed 0, exit 0.
  - Solution: total 2904 (base 2896 + 8), failed 0, succeeded 2901, skipped 3, exit 0. `BrutalUi.Framework.Tests` 242 (234 + 8); `BrutalUi.Core.Tests` 1801.
  - Unchanged classes: `GamepadActionTests` 7, `ActionMapAllocationTests` 5, `TypedActionMapTests` 9, `UiActionMapTests` 23. Each passed with no edit.
  - Plant 6 (`GamepadStick` case keeps the GLFW Y: `new float2(stick.X, stick.Y)`): build `0 Error(s)`, exit=2, total 8, failed 3: `TheLeftStickFillsAVector2WithUpPositive` (expected), plus `AStickAndAKeyCompositeSumAndClamp` and `TheGamepadSlotSelectsThePad` (both read the flipped stick value too). Restored, `cmp` equal.
  - Plant 7 (`ReadFrame` reads `input.Gamepad(0)`): build `0 Error(s)`, exit=2, total 8, failed 1 (`TheGamepadSlotSelectsThePad`, expected). Restored, `cmp` equal.
  - Plant 8 (invoke pass tests `pad.Buttons` in place of `pad.Pressed`): build `0 Error(s)`, exit=2, total 8, failed 1 (`AGamepadPressInvokesTheCommandFromTheFrameRead`, expected. The held frame fired again). Restored, `cmp` equal.
  - Plant 9 (Axis finish skips the clamp: `new float2(a.Sum.X + a.Free.X, 0f)`): build `0 Error(s)`, exit=2, total 8, failed 1 (`ATriggerFillsAnAxis`, expected. The scale-2 trigger read 1.5). Restored, `cmp` equal.
  - After each restore the build returned to `0 Warning(s)`, `0 Error(s)`, and the final suite run above is the restored tree.
- Files: `src/BrutalUi.Framework/Actions/UiActionEnums.cs`, `UiActionMap.cs`, `UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, new `tests/BrutalUi.Framework.Tests/Actions/GamepadValueActionTests.cs`, and these docs. Nothing else changed: the diff against the phase 1 state shows these five files only.
- `PushGamepad`: remarks only; no body line changed. `UiActionMap.Read.cs` is untracked (new in phase 1, uncommitted), so a diff cannot show it. The body identity is backed by the edit history: only doc-comment and `ReadFrame` regions were edited, and the four plants restore equal under `cmp`.
- Sample note for group 2: `grep -n 'PushGamepad' samples/HelloPanel/Program.cs` still showed the call at line 853. Removing it is task 2.8, outside this group.
- Plant scratch: `src/BrutalUi.Framework/obj/plantscratch/UiActionMap.Read.cs`, `p6.log`, `p7.log`, `p8.log`, `p9.log`, `plant6.cs` (git-ignored).
- Migration risk and follow-up: the gamepad button read cases (`Key`, `MouseButton`, `GamepadButton` down and press) and the Button finish still arrived in phase 3, so `Down`, `Press`, `Free`, and the Button flags stayed unused for value actions. `IsActive` still read the unhandled slot only; phase 4 owned the switch rule.
- Open questions: none. The public shape follows `implementation.md`: `UiGamepadStick`, `BindGamepadStick(ReadOnlySpan<byte>, UiGamepadStick)`, `BindGamepadAxis(ReadOnlySpan<byte>, UiGamepadAxis, float)`, `GamepadSlot`, `ReadAxis(ReadOnlySpan<byte>)`.
- Tool use: about 55 tool calls, under the 100-call stop rule.

### 2026-11-09 10:05 UTC - Phase 1 group 2 (ADVANCED, the typed map)
- Base: group 1 landed on `14c4b0c` with the Core changes uncommitted in the worktree. Start totals: solution 2887, 0 failed, 3 skipped. `BrutalUi.Core.Tests` 1801; `BrutalUi.Framework.Tests` 225.
- Task 1.6 (split): build `0 Warning(s)`, `0 Error(s)`; Framework tests 225 total, 0 failed (the same as the base), so the split changed no behavior.
- Tasks 1.7 to 1.10 (the typed map): build stayed at `0 Warning(s)`, `0 Error(s)`, and Framework tests stayed at 225, 0 failed after each task. No existing test needed an edit for the new type checks.
- Task 1.11 (`TypedActionMapTests`, 9 tests): the class passed on the first run.
- Task 1.12 (the final run and the plants):
  - Build: `0 Warning(s)`, `0 Error(s)`.
  - Demo: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*TypedActionMapTests'` gave total 9, failed 0, exit 0.
  - Solution: total 2896 (base 2887 + 9), failed 0, succeeded 2893, skipped 3, exit 0. `BrutalUi.Framework.Tests` 234 (225 + 9); `BrutalUi.Core.Tests` 1801.
  - Unchanged classes: `UiActionMapTests` 23, `GamepadActionTests` 7, `FocusActionTests` 5, `ActionMapAllocationTests` 5, `ModeActionMapTests` 5, `InputReadTests` 6 (Core). Each passed with no edit.
  - Plant 3 (`ReadFrame` skips the clamp: `a.Value = a.Sum + a.Free`): build `0 Error(s)`, exit=2, total 9, failed 1 (`EachKeyOfTheCompositeGivesItsDirection`: W+D read (1, 1)). Restored, `cmp` equal.
  - Plant 4 (`ReadFrame` reads `context.Input`): build `0 Error(s)`, exit=2, total 9, failed 1 (`TheGateEmptiesTheVector`: the gated frame read the raw W). Restored, `cmp` equal.
  - Plant 5 (`Deactivate` leaves `OnInputRead`): build `0 Error(s)`, exit=2, total 9, failed 1 (`ActivateWritesTheReadSlotAndDeactivateClearsIt`). Restored, `cmp` equal.
  - After each restore the build returned to `0 Warning(s)`, `0 Error(s)`, and the final suite run above is the restored tree.
- Files: `src/BrutalUi.Framework/Actions/UiActionMap.cs`, `UiActionMap.Bindings.cs` (new), `UiActionMap.Read.cs` (new), `UiActionEnums.cs` (new), `tests/BrutalUi.Framework.Tests/Actions/TypedActionMapTests.cs` (new), `src/BrutalUi.Framework/Actions/UiActions.cs` (one doc cref line, decision 10), and these docs.
- Plant scratch: `src/BrutalUi.Framework/obj/plantscratch/UiActionMap.cs`, `UiActionMap.Read.cs` (git-ignored).
- Migration risk and follow-up: `ReadFrame` had one read case, so the scratch fields `Down`, `Press`, `Free`, and the Button flags stayed unused until phases 2 and 3 wrote them. The phase 2 pad cases needed the `_gamepadSlot` field and `GamepadSlot` property, which group 2 did not add. `IsActive` still read the unhandled slot only, so a game that wrote `OnInputRead` by hand outside `Activate` could leave the two slots apart. Phase 4 (the switch rule) owned that case.
- Open questions: none. The public shape follows `implementation.md`: `UiActionType`, `AddAction(ReadOnlySpan<byte>, UiActionType)`, `BindKeyVector(ReadOnlySpan<byte>, int, int, int, int)`, and `ReadVector(ReadOnlySpan<byte>)`.
- Tool use: about 85 tool calls, under the 100-call stop rule. No plant left a dirty file: `git status` after the last restore shows the six files above only.

### 2026-11-09 09:40 UTC - Phase 1 group 1 (ADVANCED, Core read slot)
- Base commit `14c4b0c6c6619258858c3c96fbbedc3b127bc1df` (branch `feat/input/input-actions`). `git status --porcelain` at the start: only the four untracked `docs/features/input/input-actions/` files.
- Baseline (before the change): build `0 Warning(s)`, `0 Error(s)`. Solution tests total 2881, failed 0, skipped 3 (the three `BrutalUi.Glfw.Tests` display tests). `BrutalUi.Core.Tests`: total 1795, 0 failed, 0 skipped. `BrutalUi.Framework.Tests`: total 225, 0 failed, 0 skipped.
- After the change: build `0 Warning(s)`, `0 Error(s)`. Solution tests total 2887 (+6), failed 0, skipped 3. `InputReadTests` 6/6. `BeforeUpdateTests` 6/6 and `InputStateTests` 32/32 passed with no edit.
- Files: new `src/BrutalUi.Core/Input/UiInputReadHandler.cs` (delegate, doc rules per the brief), new `tests/BrutalUi.Core.Tests/Input/InputReadTests.cs` (6 tests), edits to `src/BrutalUi.Core/Tree/UiContext.cs` (flag `_inputRead`, re-entry check, slot call between `DrainEvents()` and `UpdateBindings()`, property `OnInputRead` after `OnBeforeUpdate`, remarks paragraph, frame order sentence, `<exception>` line).
- Plant 1 (read slot before `DrainEvents();`): `dotnet test --solution` exit=2. Failed 3: `InputReadTests.TheSlotRunsOneTimeAfterTheDrain` (expected), plus `InputReadTests.TheSlotRunsAfterTheDrainSlotAndTheBeforeUpdateSlot` and `InputReadTests.AnExceptionOfTheSlotLeavesUpdate` (both read the frame state after the drain, so the wrong place fails them too). Restored, `cmp` equal, suite green again.
- Plant 2 (`GC.KeepAlive(new object());` right before `inputRead(this);` in `Update`): build checked first (`0 Error(s)`), `dotnet test --solution` exit=2. Failed 1: `InputReadTests.TheSlotAllocatesNothing`, 2400 bytes over the 100 iterations. Restored, `cmp` equal, suite green (total 2887, failed 0, skipped 3).
- Plant scratch: `src/BrutalUi.Core/obj/plantscratch/UiContext.cs`, `p1.log`, `p2.log` (git-ignored).
- Diff of `src/BrutalUi.Core/Tree/UiContext.cs`: 56 insertions, 2 deletions, seven hunks (flag, remarks paragraph, frame order sentence, exception line, re-entry check, slot call, slot property). No hunk touches `DrainEvents`.
- Deviations from the brief: none inside group 1. The `dotnet test --solution` log holds no build line, so each plant run builds first and gates on `0 Error(s)` (Decision 4 above).
- Migration risk and follow-up: none for group 1. The slot is additive, and no caller wrote it yet. Group 2 (tasks 1.6 to 1.12) wired `UiActionMap.ReadFrame` to it.
- Open questions: none. The public shape of group 1 follows `implementation.md` word for word.

### 2026-11-09 09:05 UTC
- `/autorun-feature input/input-actions` started. Preconditions met: plan and requirements exist. The dependencies input-state and gamepad-axes are complete; branch `feat/input/input-actions` was created from `main` (base `14c4b0c`, holds gamepad-axes).
- Scaffolded `tasks.md` (41 tasks, 5 phases; phases 1 and 4 ADVANCED, phases 2, 3, 5 STANDARD), this file, `inbox.md`. Set `implementation.md` Status: In Progress.
- Next: phase 1 group 1 on an ADVANCED dev agent.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
