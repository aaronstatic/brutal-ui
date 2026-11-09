# input/gamepad-axes - Context & Decisions

**Last Updated:** 2026-11-08 19:48
**Current Phase:** Complete. All 3 phases built and gate-verified (22/22 tasks). Cross-phase review closed (3 findings, 3 fixed, 0 open). Post-review verifier PASS. `implementation.md` Status: Ready for Review.
**Status:** 🟢 Ready for Review (display-host checks stay in Needs Human Verification)

---

## Quick Status

**Done:**
- ✅ Plan approved (`implementation.md`, 3 phases, 22 tasks).
- ✅ Feature branch `feat/input/gamepad-axes` created from `main` (base `240f8d4`, holds merged input-state).
- ✅ Feature scaffolded by `/start-feature` (this file, `tasks.md`, `inbox.md`).
- ✅ **Phase 1 complete** (tasks 1.1-1.6): `UiGamepadAxis` enum, the pure `UiGamepadDeadzone` helper (`ApplyStick`, `ApplyTrigger`), and the `UiGamepadState` reads `Axis(UiGamepadAxis)`, `LeftStick`, `RightStick`. 9 `GamepadDeadzoneTests` pass. Three plants guard the rules. Each existing test passes with no edit. Build 0/0, solution 2873 (0 failed, 3 skipped).
- ✅ **Phase 2 complete** (tasks 2.1-2.8): `GlfwUiInput.PollGamepads()` (16 slots, `SetGamepad` per slot, `default` on absent/disposed), `StickDeadzone` (0.15) and `TriggerDeadzone` (0.1) with the range check, and the internal `GamepadState` conversion. New `GlfwUiInputGamepadPollTests` (8 tests, all run here — the null-platform test does not skip). The live `GlfwUiInputGamepadTests` poll helper now measures both polls (A7). Build 0/0, solution 2881 (0 failed, 3 skipped). Plants 4-8 each exit 2 with the named target failed. Each restored file equals its copy under `cmp`. Phase 2 verifier PASS, Demo PASS (all six gate steps green, write seam one `SetGamepad` call, Q6 grep clean, protected files no diff, no plant residue).

- ✅ **Phase 3 group 1 complete** (tasks 3.1-3.4): HelloPanel shows the left stick of slot 0 in a fourth label. `HelloScreen`: `out Label stickLabel` in `BuildWidgetPanel`, `StickText(in UiGamepadState, Span<byte>)`, `StickComponent`, `LongestStickText` (24 bytes). `HelloModes` has `StickLabel`. `Program.cs` has `input.PollGamepads()` outside the UI region and `stickScratch` (32 bytes). The first line of the UI region is `state.Map.PushGamepad(ui.Input.Gamepad(0).Buttons)`, and the stick label write follows `Update`. Build 0/0, solution 2881 (0 failed, 3 skipped), no test in this group. The smoke step and plant 9 hit the sandbox X11 block (Gotcha 3). The PASS run, the plant 9 byte count, and the window demo are Needs Human Verification.

- ✅ **Phase 3 group 2 complete** (tasks 3.5-3.8): section 9 of the technical design holds item 12 "The gamepad poll", section 14 holds the bullet **Gamepad axes tests.**, and `CHANGELOG.md` holds three Added entries and one Changed entry. Build 0 Warning/0 Error. Solution 2881 total (0 failed, 3 skipped). The demos read `GamepadDeadzoneTests` 9 and `GlfwUiInputGamepadPollTests` 8, each with `failed: 0`. The ASD-STE100 lint passes on the five named files. The `technical-design.md` score reads 1.53, and the `CHANGELOG.md` score reads 1.55. The `context.md` score reads 2.07, the `implementation.md` score reads 1.66, and the `code-review.md` score reads 0.52.
- ✅ **Phase 3 verifier PASS, Demo MANUAL.** All six gate steps green (build 0/0, solution 2881 / 0 failed / 3 skipped, demos 9 and 8, lint 1.53 / 1.55 / 1.80 / 1.66). Sample contract checks pass: one `PollGamepads` call outside the UI region before `Update`, the push uses `ui.Input.Gamepad(0).Buttons`, the label write sits inside the UI region after the input label, `stickScratch` 32 bytes against the 24-byte longest text, `-0.00` cannot render. Docs checks pass (section 9 item 12, section 14 bullet, CHANGELOG names all seven members); no doc claims plant 9's byte count was measured here. Scope: samples limited to three files, `src/` limited to the Phase 1/2 set, no project file, protected files zero-diff, no plant residue. `scripts/test-linux.sh` fails only at the smoke step with the known X11 block.

- ✅ **Cross-phase review complete and closed.** `code-review.md` holds 3 findings, all LOW, none CRITICAL/HIGH/MEDIUM: CR-1 a 0-byte test that passed even when the disposed poll did no work; CR-2 the deadzone settings reached the poll through no pinned path; CR-3 the `UiGamepadAxis` remarks claimed an action-map sharing that arrives with input-actions. All three fixed in three groups (CR-1 and CR-2 in the poll test file, CR-3 one comment sentence), each guard bite proved with a temporary plant and restored `cmp`-equal. The review re-derived both formulas independently and confirmed the shipped code, the stored-value contract, the poll contract, the allocation discipline, the I1 scope list, and the docs.
- ✅ **Post-review verifier PASS after one fix round.** Pass 1 failed gate 7 only (this report's own prose read 4.38 against the 2.5 ASD-STE100 bar). The sweep took it to 0.52 with every fact kept. Pass 2: build 0/0, solution 2881 / 0 failed / 3 skipped, class totals 8, 9, 32, 6 unchanged, lint exit 0 over five files (1.53, 1.55, 2.07, 1.66, 0.52), guard integrity real for all three findings, scope still exactly I1, protected files zero-diff, no plant residue. Demo: none (the fixes add no user-facing path).

**Next:**
- ⏭️ A person commits the branch, pushes, and opens the PR. The run makes no commit and no push.
- ⏭️ A person runs the four display-host checks in `## Needs Human Verification`: the `scripts/test-linux.sh` smoke PASS line, plant 9's 2808-byte measure with exit 1, the live `xvfb-run` gamepad class at 6 succeeded, and the HelloPanel stick label with a pad in slot 0.
- ⏭️ `/evaluate-feature input/gamepad-axes` runs the Definition of Done verification method. Its F, Q, and I boxes stay open for that independent evaluator; this run did not tick them.

**Blockers:**
- None. (The agent sandbox blocks the X11 socket, so the HelloPanel window demo and the `scripts/test-linux.sh` smoke PASS run are Needs Human Verification, not blockers.)

---

## Key Files

### Core Implementation (this feature)
- `src/BrutalUi.Core/Input/UiEventEnums.cs` - phase 1: add `UiGamepadAxis` after `UiGamepadButtons`.
- `src/BrutalUi.Core/Input/UiGamepadDeadzone.cs` - phase 1 (new): `ApplyStick`, `ApplyTrigger`.
- `src/BrutalUi.Core/Input/UiGamepadState.cs` - phase 1: `Axis(UiGamepadAxis)`, `LeftStick`, `RightStick`, remarks.
- `src/BrutalUi.Glfw/GlfwUiInput.cs` - phase 2: `StickDeadzone`, `TriggerDeadzone`, `GamepadState`, `PollGamepads`.

### Sample and Docs (this feature)
- `samples/HelloPanel/HelloScreen.cs` - phase 3: the stick label, `StickText`.
- `samples/HelloPanel/HelloModes.cs` - phase 3: `StickLabel`.
- `samples/HelloPanel/Program.cs` - phase 3: `PollGamepads`, the push from the slot, the label write.
- `docs/technical-design.md` - phase 3: section 9, section 14.
- `CHANGELOG.md` - phase 3.

### Files that must NOT change (A1)
- `src/BrutalUi.Core/Tree/UiContext.cs`, `src/BrutalUi.Core/Input/UiInputState.cs`, `src/BrutalUi.Core/Input/InputStateWriter.cs`, and all of `src/BrutalUi.Framework/` (also `UiActionMap.cs`).

### Related Files
- `docs/features/input/gamepad-axes/implementation.md` - the plan. The full task text of each phase lives there. The briefs copy it word for word.
- `docs/features/input/epic-overview.md` - the build order and the decisions of the epic.
- `docs/features/input/input-state/context.md` - Gotchas 1-3 (build flags, test flakes, the sandbox X11 block), Decisions 24-27 (the gamepad slot type, edges).

---

## Base State

- **Base commit:** `240f8d471c45332c20eee210a3546b4cf749f8dd` (branch `feat/input/gamepad-axes`, holds merged `feat/input/input-state`).
- **`git status --porcelain` at phase 1 start:** four untracked files, all under `docs/features/input/gamepad-axes/` (`context.md`, `implementation.md`, `inbox.md`, `tasks.md`), created by `/start-feature`. No tracked file is dirty.
- **Live baseline (task 1.1, Debug, this host):** solution `total: 2864`, `failed: 0`, `skipped: 3`. `BrutalUi.Core.Tests` `total: 1786`, `failed: 0`, `skipped: 0`. `BrutalUi.Glfw.Tests` `total: 32`, `failed: 0`, `skipped: 3` (the 3 skips are the display tests — `GLFW init failed: PlatformUnavailable`, no X11). These match the planning-time reference counts.
- **Skipped list:** `GlfwUiInputTests.ARealWindowSubscribesAndUnsubscribes`, `GlfwUiInputAllocationTests.TheRealHandlersAllocateNothing`, `GlfwUiInputGamepadTests.ALivePollAllocatesNothing`.
- **End-state targets (Definition of Done Q2):** Core 1786 + 9 = 1795, Glfw 32 + 8 = 40, solution 2864 + 17 = 2881. Skipped stays 3 unless the null-platform test skips on this host (then 4, recorded here).

---

## Important Decisions

The plan holds Confirmed Assumptions (Q1-Q3, A1-A8) and Decisions 1-11 in `implementation.md` (Key Technical Decisions). This file records only decisions made during the build that the plan did not settle.

### Phase 2 build decisions

- **A5 confirmed clean.** `GlfwGamepadState.Axes` is a fixed buffer of 6 floats. `new ReadOnlySpan<float>(raw.Axes, GamepadAxisCount)` compiles inside the same `unsafe` block as the buttons, with no pin, exactly as `PollGamepad` does for `Buttons`. No heap copy. No alternate access was needed. `TheAxisBufferAndTheAxisConstantsFollowGlfw` pins the length and element type.
- **The binding names the platform query `IsPlatformSupported`.** The brief and the plan write `GlfwApi.PlatformSupported(GlfwPlatform.Null)`. The member in `Ahwoo.BRUTAL.Glfw` 2026.10.4 is `GlfwApi.IsPlatformSupported(GlfwPlatform)`. The test uses the real name. Same meaning, no API choice.
- **The init-hint spelling that compiled** (task 2.6, the cast branch — the binding has no named `GlfwInitHint` member for the platform hint, and `GlfwApi.Constants` holds all three names):
  `GlfwApi.SetInitHint((GlfwInitHint)GlfwApi.Constants.GLFW_PLATFORM, GlfwApi.Constants.GLFW_PLATFORM_NULL);`
  The reset in `finally` sets `GlfwApi.Constants.GLFW_ANY_PLATFORM` back to the same hint. The cast to `(GlfwInitHint)` compiles, as the risk table of the plan predicted.
- **Deviation (plan text vs. plan formula).** `TheConversionMapsEachGlfwAxis` asserts RightStick `(−0.6, 0.8)`, not the `(−0.75, 1)` of the plan text. The worked values in task 2.5 contradict the Phase-1 formula and the public contract. `ApplyStick` clamps the result length to 1 also with a deadzone of 0 ("Result length 0 to 1"). `AStickAtFullTiltReadsLengthOne` pins this at base: with 0, (2, 0) gives (1, 0). The raw right stick (−0.75, 1) has length 1.25, so it reads (−0.6, 0.8). The helper lives in merged Core code, 9 tests guard it, and Phase 1 planted it, so the formula wins and the test value changed. The test exists to prove the axis-order mapping, and each of the six slots keeps its own value there.
- **Plan reconciled to the formula (parent, 2026-11-08 18:35).** The parent confirmed the arithmetic independently (`ApplyStick((-0.75, 1), 0)`: length 1.25 → `s = min(1.25, 1) = 1`, `k = 0.8` → `(-0.6, 0.8)`) and edited `implementation.md` in two places — the `TheConversionMapsEachGlfwAxis` values in task 2.5 and Definition of Done **F6** — so the plan now states `RightStick` (-0.6, 0.8) and names the Decision 2 clamp. The shipped test matches the corrected plan. No scope change: only the stale expected value was wrong. The formula and the public contract stand as planned.
- **`TriggerDeadzone` remarks state the no-trigger mapping case** (risk table: a pad with no trigger gives GLFW 0, and the default zone reads 0.444). The sentence lives in the property remarks.
- **The null-platform test runs on this host.** `IsPlatformSupported(Null)` is true and `Init` succeeds headless, so `GlfwUiInputGamepadPollTests` reads total 8 with 0 skipped. Skipped stays 3 solution-wide.

## Phase 1 Reference — Formulas and Internal Shape (from the plan)

**Deadzone formulas** (authority: `implementation.md`, "The Deadzone Formulas"). `d` is the deadzone. Both methods first check `d`: NaN or a value outside 0 to 1 throws `ArgumentOutOfRangeException` with the parameter name `deadzone`.

- `ApplyStick(stick, d)` (radial): if x or y is not finite, return (0, 0). `len = sqrt(x*x + y*y)`. If `!(len > d)` or `d >= 1`, return (0, 0) (the `!(>)` form also catches a NaN length). `s = min((len - d) / (1 - d), 1)`. `k = s / len`. Return `(x*k, y*k)` — same direction, length `s`.
- `ApplyTrigger(value, d)` (linear): if value is not finite, return 0. `t = clamp((value + 1) * 0.5, 0, 1)`. If `!(t > d)` or `d >= 1`, return 0. Return `(t - d) / (1 - d)` — the deadzone sits at the low end of the 0 to 1 scale (Q2).
- Worked values: `ApplyStick((0.1, 0.05), 0.15)`→(0,0). `ApplyStick((0.575, 0), 0.15)`→(0.5,0). `ApplyStick((0.3, 0.4), 0.15)`→(0.247059, 0.329412). `ApplyStick((1, 1), 0.15)`→(0.707107, 0.707107). `ApplyTrigger(-1, 0.1)`→0. `ApplyTrigger(0, 0.1)`→0.444444. `ApplyTrigger(0.5, 0.1)`→0.722222. `ApplyTrigger(1, 0.1)`→1.

**Internal Shape (phase 1):**

- `UiGamepadDeadzone` (new, `src/BrutalUi.Core/Input/UiGamepadDeadzone.cs`): `public static class`, two methods, no state, no field. It uses `float` components and `MathF.Sqrt`. It builds the result with `new float2(x, y)`. Uses no `float2` operator beyond what Core uses today (the components `.X`/`.Y` and the constructor).
- `UiGamepadAxis` (in `UiEventEnums.cs`, Decision 5): members `LeftStickX=0, LeftStickY=1, RightStickX=2, RightStickY=3, LeftTrigger=4, RightTrigger=5`. Value `i` is GLFW gamepad axis `i`.
- The reads (in `UiGamepadState.cs`): `Axis(UiGamepadAxis axis) => Axis((int)axis)` (keeps the range check and the `axis` parameter name in one place). `LeftStick => new(_axes[0], _axes[1])`. `RightStick => new(_axes[2], _axes[3])`. The struct keeps its fields and its 40 bytes.

---

## Phase 1 Build Log

### Result (tasks 1.2-1.6)

- Build: `0 Warning(s)`, `0 Error(s)` on `brutal-ui.sln` (Debug, `MSBUILDDISABLENODEREUSE=1`, `-nodeReuse:false` on build only).
- Solution tests: total 2873, failed 0, skipped 3. The 3 skips are the display tests at base (`ARealWindowSubscribesAndUnsubscribes`, `TheRealHandlersAllocateNothing`, `ALivePollAllocatesNothing`). This host has no X11, so they skip. Expected, not a fault.
- `BrutalUi.Core.Tests` total: 1795 (base 1786 + the 9 new `GamepadDeadzoneTests`).
- `GamepadDeadzoneTests` (`--filter-class`): total 9, failed 0. The phase 1 demo.
- `InputStateTests`: total 32, failed 0, all pass with no edit.
- `GlfwUiInputGamepadTests`: total 6, failed 0 (5 pass, 1 skip = `ALivePollAllocatesNothing`, as at base).
- Core diff shape: `UiGamepadState.cs` 32 insertions / 4 deletions (the three reads + the changed remarks). `UiGamepadAxis` in `UiEventEnums.cs` shows added lines only. `UiContext.cs`, `UiInputState.cs`, and `InputStateWriter.cs` show no diff.

### Plants (task 1.6)

Each plant: backup the file to `src/BrutalUi.Core/obj/plantscratch/`, edit in place, rebuild Core.Tests (confirmed `0 Error(s)` before reading the result), run `GamepadDeadzoneTests`, then restore with `cp` and confirm `cmp` equal. The scratch dir is git-ignored (`git check-ignore` confirmed).

| Plant | Change | Exit code | Failed test(s) | `cmp` after restore |
|-------|--------|-----------|----------------|-------------------|
| 1 | `ApplyStick` returns `stick` unchanged when the length is above the deadzone (drops the rescale + clamp) | 2 | `TheStickRescalesTheLengthAndKeepsTheDirection` (named target), and also `AStickAtFullTiltReadsLengthOne` (collateral: the raw stick also breaks the clamp) | equal |
| 2 | `ApplyTrigger` skips the map and uses `t = value` | 2 | `ATriggerMapsTheGlfwRangeToZeroToOne` (named target), and also `ATriggerAppliesTheLinearDeadzoneAtTheLowEnd` (collateral: the low-end zone reads the un-mapped value) | equal |
| 3 | `RightStick` reads axes 0 and 1 | 2 | `TheStateReadsTheSticksAndTheNamedAxes` (named target) only | equal |

Each plant gives a non-zero exit (2), so each rule test is guarded. The named target fails in every plant, as the brief requires.

### Deviations from the plan

- Plants 1 and 2 each fail two tests, not one. The brief names one expected test per plant ("expect X to fail"), and that test does fail. The extra failure is a correct consequence: plant 1 removes the clamp along with the rescale, and plant 2 removes the map before both trigger rules apply. This strengthens the guard, so no action is needed. I record it for the phase-2 doc line.

### Open questions

- None. The public API shape, the formulas, and the internal shape all followed the plan without ambiguity.

## Phase 2 Build Log

### Result (tasks 2.1-2.8)

- Build: `0 Warning(s)`, `0 Error(s)` on `brutal-ui.sln` (Debug, `MSBUILDDISABLENODEREUSE=1`, `-nodeReuse:false` on build only).
- Solution tests: total 2881, failed 0, skipped 3. The 3 skips are the base display tests. The null-platform test runs here, so skipped stays 3 (target met: 2873 + 8).
- `BrutalUi.Glfw.Tests` total: 40 (base 32 + the 8 new `GlfwUiInputGamepadPollTests`).
- `GlfwUiInputGamepadPollTests` (`--filter-class`): total 8, failed 0, succeeded 8, skipped 0. The phase 2 demo. `TheNullPlatformPollWritesSixteenAbsentSlots` RUNS on this host (null platform supported, init succeeds headless), it does not skip.
- `GlfwUiInputGamepadTests`: total 6, failed 0 (5 pass, 1 skip = `ALivePollAllocatesNothing`, as at base).
- `xvfb-run` (task 2.8, one run): `xvfb-run -a tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*GlfwUiInputGamepadTests'` → `total: 6, failed: 0, succeeded: 5, skipped: 1`. The sandbox blocks the X11 socket (Gotcha 3), so `ALivePollAllocatesNothing` still skips under `xvfb-run`. Expected, not a code fault. The PASS run (6 succeeded, 0 skipped) goes to Needs Human Verification.
- Diff shape: `GlfwUiInputGamepadTests.cs` shows three hunks only — the class summary, the `Poll` helper, and `ALivePollAllocatesNothing` (acceptance met). `UiContext.cs`, `UiInputState.cs`, and `InputStateWriter.cs` show no diff. No file under `src/BrutalUi.Framework/` changed. `UiGamepadDeadzone.cs` is byte-equal to its pre-plant backup (plant 7 restored, `cmp` equal).

### Plants (task 2.8)

Each plant: backup under `src/BrutalUi.Glfw/obj/plantscratch/` (plant 7 under `src/BrutalUi.Core/obj/plantscratch/`), edit in place, build (confirmed `0 Warning(s) 0 Error(s)` before reading the result), run the full solution test to a log, then restore with `cp` and confirm `cmp` equal.

| Plant | Change | Exit code | Failed test(s) | Measured bytes | `cmp` after restore |
|-------|--------|-----------|----------------|----------------|-------------------|
| 4 | `GC.KeepAlive(new object());` as the first line of `PollGamepads` | 2 | `ADisposedPollAllocatesNothing` and `TheNullPlatformPollWritesSixteenAbsentSlots` (both named as expected — the null-platform test runs here) | 2400 each (100 calls x 24 bytes) | equal |
| 5 | `if (_disposed) { return; }` as the first line of `PollGamepads` | 2 | `ADisposedAdapterWritesDisconnectedSlots` (named target) only | n/a (edge assert) | equal |
| 6 | `GamepadState` writes axis 5 to slot 4 and axis 4 to slot 5 (the two triggers swapped) | 2 | `TheConversionMapsEachGlfwAxis` (named target), plus `TheConversionAppliesTheDeadzoneSettings` and `TheConversionAllocatesNothing` (collateral: both read the trigger slots too) | sum 200 vs 300 on the allocation test | equal |
| 7 | `GC.KeepAlive(new object());` as the first line of `UiGamepadDeadzone.ApplyStick` | 2 | `TheConversionAllocatesNothing` (named target) only | 4800 (200 stick calls x 24 bytes) | equal |
| 8 | The `StickDeadzone` setter skips the range check | 2 | `TheDeadzoneSettingsCheckTheRange` (named target) only | n/a (throws assert) | equal |

Every plant gives a non-zero exit (2), and the named target fails in every plant, as the brief requires.

### Deviations from the plan (phase 2)

- **`TheConversionMapsEachGlfwAxis` asserts RightStick (−0.6, 0.8), not the (−0.75, 1) in the plan text.** See the Phase 2 build decision above: the worked value in the plan contradicts the formula in the plan (the length clamp applies at a deadzone of 0 too, pinned at base by `AStickAtFullTiltReadsLengthOne`). The merged Phase-1 helper wins. The axis-order mapping stays pinned.
- **The binding names the query `GlfwApi.IsPlatformSupported`,** not `PlatformSupported` as the brief writes. The test uses the real name.
- **Plants 6 collaterals:** plant 6 fails three tests, not one. The named target fails, and the two other conversion tests read the swapped trigger slots. Same note as phase 1 plants 1-2: the extra failures are correct consequences and strengthen the guard.

### Open questions (phase 2)

- None blocking. Notes question 2 of the plan (the no-trigger mapping reads 0.444) stays open as the plan left it. The `TriggerDeadzone` remarks state the case.

---

## Phase 3 Build Log (Group 1, tasks 3.1-3.4)

### Result

- Scope: the group edits only `samples/HelloPanel/HelloScreen.cs`, `samples/HelloPanel/HelloModes.cs`, `samples/HelloPanel/Program.cs`, and this file. The group adds no test. `git diff --stat` over `src/`, `scripts/`, `samples/HelloPanel/HelloState.cs`, `samples/HelloPanel/HelloScreen.css`, and the project files shows only the Phase 1 and Phase 2 changes. `UiContext.cs`, `UiInputState.cs`, and `InputStateWriter.cs` stay without a diff.
- Build: `0 Warning(s)`, `0 Error(s)` on `brutal-ui.sln` (Debug, `MSBUILDDISABLENODEREUSE=1`, `-nodeReuse:false` on build only).
- Solution tests: total 2881, failed 0, skipped 3. The suite equals the Phase 2 target (this group adds none). The 3 skips are the base display tests.
- Smoke run: `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` passes build, test, gpu, and loader, then prints exactly:
  `test-linux: FAIL at smoke (exit 1)` / `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` / `test-linux: full log: artifacts/test-linux/smoke.log` → `exit=1`.
  This is the expected sandbox line (Gotcha 3): the sandbox blocks the X11 socket, so the HelloPanel smoke step cannot run here. It is not a code fault. The PASS run goes to Needs Human Verification.
- Frame-order change: item (1) now calls `input.PollGamepads()` outside the UI region (in place of `UiGamepadButtons pad = input.PollGamepad();`). The first line of the UI region (item (3)) is `state.Map.PushGamepad(ui.Input.Gamepad(0).Buttons);` — the raw slot holds the poll of this frame before `Update` (A6). The stick write `stickLabel.SetText(HelloScreen.StickText(in ui.GameInput.Gamepad(0), stickScratch));` sits right after the input label line, inside the UI region, so the 0-byte gate measures the slot read, the format, and the label update.
- Buffer proof: the longest stick text is `LongestStickText` = `"Left stick: -1.00, -1.00"` = 24 bytes: the prefix takes 12 bytes, the separator takes 2, and each component takes at most 5 bytes (`-1.00`). `StickComponent` clamps to -1 to 1 and rounds to two decimals, so no component renders longer. The label starts with those 24 bytes, so its buffer never grows after the first frame, and the 32-byte `stickScratch` always holds one stick text (8 bytes of headroom).

### Plant 9 (task 3.4)

- Staged the backup at `samples/HelloPanel/obj/plantscratch/HelloScreen.cs.bak` (git-ignored, confirmed with `git check-ignore`). Plant text word for word: `GC.KeepAlive(new object());` as the first line of `HelloScreen.StickText`. `using System;` is already in the file.
- Build with the plant: `0 Warning(s)`, `0 Error(s)` — confirmed before any measurement attempt.
- Attempt on this host: `timeout 90 env -u WAYLAND_DISPLAY -u VK_DRIVER_FILES -u VK_ADD_DRIVER_FILES XDG_SESSION_TYPE=x11 xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 120` → exit 1 with `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` The sample dies at GLFW init, before the frame loop, so the gate cannot measure here. The un-planted run fails at the same line, so this exit 1 is the sandbox block, not gate proof.
- Left for a person: on a host with a working display, the smoke line 3 must read 2808 bytes (`WarmUpFrames = 3`, so 117 measured frames × one 24-byte `new object()` each) and the script must exit 1.
- Proven here: the plant compiles with `0 Error(s)`. Restored with `cp`, equal under `cmp`. A clean rebuild gives `0 Warning(s)` `0 Error(s)`. `grep` finds no `GC.KeepAlive` residue in the sample. The group deleted the scratch folder.

### Build decisions (group 1, not settled by the plan)

- `StickText` lives after the input-state helpers at the end of the class, with its own literals `StickPrefix` (`"Left stick: "u8`) and `StickSeparator` (`", "u8`). The method does not reuse `InputSeparator`: same bytes, but each text helper stays self-describing. The write pattern copies `InputText`: prefix copy, `TryFormat` per component, one `InvalidOperationException` guard per part, and the return of the written part.
- Added the private constant `StickClampLimit = 1f` for the -1 to 1 clamp in `StickComponent`, following the `DeltaClampLimit` pattern.
- The class remarks name "the gamepad poll" in plain prose. The file adds no cross-assembly cref to `GlfwUiInput`, so its doc references stay inside the assemblies it already uses.
- `BuildWidgetPanel` now wraps its parameter list over three lines. The added `out Label stickLabel` would otherwise pass the 120-column limit.

### Deviations (group 1)

- None. Tasks 3.1-3.4 follow the brief word for word. The blocked smoke measurement is the known sandbox limitation the brief anticipates, not a deviation.

### Open questions (group 1)

- None.

## Phase 3 Build Log (Group 2, tasks 3.5-3.8)

### Result

- Scope: docs only. The group edits `docs/technical-design.md` (sections 9 and 14), `CHANGELOG.md`, this file, and the phase-3 boxes and status lines of `implementation.md`. `git status` shows no change by this group under `src/`, `samples/`, `tests/`, or `scripts/`.
- Build: `0 Warning(s)`, `0 Error(s)` on `brutal-ui.sln` (Debug, `MSBUILDDISABLENODEREUSE=1`, `-nodeReuse:false` on build only).
- Solution tests: total 2881, failed 0, skipped 3. The 3 skips are the base display tests.
- Demo (phase 1): `GamepadDeadzoneTests` total 9, failed 0. Demo (phase 2): `GlfwUiInputGamepadPollTests` total 8, failed 0.
- Smoke: `scripts/test-linux.sh` still fails at its smoke step here (Gotcha 3, the sandbox X11 block). Group 1 ran it, recorded the exact line, and did not retry it with wider access. The PASS run stays in Needs Human Verification.
- Plant 9: the byte count is not measurable on this host. It stays open for a person. The docs name the expected 2808 bytes as the pending person-run result, not as a measurement.
- Section 9, task 3.5: the item 11 sub-item "The gamepad seam" now names `GlfwUiInput.PollGamepads` (item 12). New item 12 "The gamepad poll" holds the frame rule (A6), the 16 slots, the connected rule (A8), the absent rule and the disposed rule (A4), and `UiGamepadAxis` with the three reads. It states the stored value rule (Q1, Q2, the GLFW Y sign), the two formulas, the two settings and their range, and the 0-byte rule. One closing sentence names `GamepadDeadzoneTests` and `GlfwUiInputGamepadPollTests`. The **`PollGamepad`** bullet of the action map now says that `PollGamepads` (items 11 and 12) writes the full state, and a game can push `ui.Input.Gamepad(0).Buttons` in place of the result of `PollGamepad`.
- Section 14, task 3.6: the **Action map tests.** bullet now says the live poll measures `PollGamepads` too and asserts a disconnected absent slot. New bullet **Gamepad axes tests.** holds the two classes (9 and 8 tests), the three 0-byte tests (not a numbered gate, Decision 9), the null-platform test, and plants 1 to 9 with their results. Plant 9's byte measure is recorded as open for a person. The **Sample projects** bullet now reads "the three label updates", names the gamepad push from the slot, and names `PollGamepads` in the frame-order sentence.
- CHANGELOG, task 3.7: three Added entries (`UiGamepadAxis` and `UiGamepadDeadzone`; `UiGamepadState.Axis(UiGamepadAxis)`, `LeftStick`, and `RightStick`; `GlfwUiInput.PollGamepads`, `StickDeadzone`, and `TriggerDeadzone`) and one Changed entry (the slots hold the values after the deadzone when `PollGamepads` writes them, and a trigger holds 0 to 1). Each entry carries the input/gamepad-axes tag.
- Lint, task 3.8: `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` passes on the four files. Scores per 100 words: `docs/technical-design.md` 1.53, `CHANGELOG.md` 1.55, `context.md` 1.80, `implementation.md` 1.64.

### Epic spelling note (Notes gap 6, for `/update-epic input`)

- `docs/features/input/epic-overview.md` (Integration & Architecture, "Within the epic") says "`GlfwUiInput.PollGamepad` writes the gamepad state into the same struct". The real writer is `PollGamepads`. This group did not edit the epic overview. `/update-epic input` should fix the spelling.

### Build decisions (group 2, not settled by the plan)

- The item 12 sub-items follow the sub-item style of item 11 ("The frame rule.", "The 0-byte rule.", and so on). The doc states the Decision 2 clamp at a deadzone of 0, in line with the corrected plan. It restates no unclamped stick value.
- This file got the same lint fix as the input-state record before it: prose semicolons became periods, and the "What's Done" / "What's Next" headings became "Done" / "Next". Every fact, number, file name, and command stayed.

### Deviations (group 2)

- None. Tasks 3.5-3.8 follow the brief. The plant-9 byte count and the smoke PASS line stay for a person, as the brief directs.

### Open questions (group 2)

- None.

---

## Gotchas & Learnings
### 1. MSBuild nodes hang the build on this machine
**Solution:** Every command sets `MSBUILDDISABLENODEREUSE=1`. Pass `-nodeReuse:false` to `dotnet build` only — never to `dotnet test` (it breaks the host handshake, exit 5, "Zero tests ran"). Never run `dotnet build-server shutdown`. (Inherited from input-state Gotcha 1.)

### 2. Known test flakes
A test host can exit with 134 (Internal CLR error 0x80131506), and `TheCurvesAllocateNothing` failed once with 8128 bytes. Run the tests a second time before treating either as a fault. A plant that does not compile leaves the old test binary, so check `0 Error(s)` before reading a plant result. (Inherited from input-state Gotcha 2.)

### 3. The agent bash sandbox isolates /tmp — display tests fail here
Inside the dsh sandbox, `xvfb-run` starts Xvfb but the client cannot open `/tmp/.X11-unix/X<n>`, so `scripts/test-linux.sh` fails at its smoke step with `HelloPanel: GLFW init failed: PlatformUnavailable`, and a live-display test skips. Record it and hand the PASS run to a normal shell. This is not a code fault. (Inherited from input-state Gotcha 3.)

### 4. Plant scratch stays under the workspace
Stage plant copies under `src/BrutalUi.Core/obj/plantscratch/` or `src/BrutalUi.Glfw/obj/plantscratch/` (git-ignored). `GC.KeepAlive(new object());` needs `using System;` in its file. (Inherited from input-state.)

### 5. The dsh sandbox blocks the `dotnet run` file-based app (phase 2)
`dotnet run one.cs` wants `~/.local/share/dotnet/runfile` and `/run/user/<uid>/.dotnet/shm` — both read-only here, and a redirected `NUGET_PACKAGES` restore hits more read-only paths. Do not probe a binding API with a scratch app. Write the code with the cast fallback from the plan, and let the compiler confirm the spelling. (The compiled spelling for the platform hint: `(GlfwInitHint)GlfwApi.Constants.GLFW_PLATFORM` with the `GLFW_PLATFORM_NULL` / `GLFW_ANY_PLATFORM` constants.)

---

## Testing Approach

### Unit Tests
- `Core.Tests/Input/GamepadDeadzoneTests` (9): the stick zone, the full tilt and clamp, the rescale and kept direction, the trigger map, the trigger zone, the range check, the deadzone-of-1 and non-finite reads, the state reads, and the enum order.

### Glfw Tests
- `Glfw.Tests/GlfwUiInputGamepadPollTests` (8): the axis buffer and GLFW constants, the conversion order, the conversion with deadzones, the settings range, the disposed poll, two 0-byte tests, and the null-platform poll.
- `GlfwUiInputGamepadTests` (6): grows `Poll` + `ALivePollAllocatesNothing` to cover `PollGamepads` (A7).

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*GamepadDeadzoneTests'` after a build and reads a total of 9 with `failed: 0`. (Verifier ran it: PASS.)

- [ ] Phase 2 demo: a developer runs `tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*GlfwUiInputGamepadPollTests'` after a build and reads a total of 8 with `failed: 0`. (Verifier ran it: PASS. The null-platform test runs on this host, so 8 pass with 0 skipped.)

- [ ] Phase 2 live run: on a host with a working display, run `xvfb-run -a tests/BrutalUi.Glfw.Tests/bin/Debug/net10.0/BrutalUi.Glfw.Tests --filter-class '*GlfwUiInputGamepadTests'` after a build and read `total: 6, failed: 0, skipped: 0` — `ALivePollAllocatesNothing` now measures `PollGamepads` + `PollGamepad` and asserts the disconnected absent slot. In the agent sandbox the run gives `succeeded: 5, skipped: 1` (Gotcha 3), so only the PASS line needs a person.

- [ ] Phase 3 smoke run: on a host with a working display, run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and read `test-linux: PASS build, test, gpu, smoke (120 frames) in <n> s` with exit 0. The run covers the `PollGamepads` poll, the push of slot 0, and the stick label. In the agent sandbox the smoke step fails with `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` and exit 1 (Gotcha 3), so the PASS line needs a person.

- [ ] Phase 3 plant 9: add `GC.KeepAlive(new object());` as the first line of `HelloScreen.StickText` (the plant builds with `0 Error(s)` here), then run the smoke script on a host with a working display. Line 3 of the smoke output must read 2808 bytes (117 measured frames × 24) and the run must exit 1. Restore the file with `cp` and confirm it with `cmp`.

- [ ] Phase 3 demo: on a host with a window, run `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. The fourth label reads `Left stick: 0.00, 0.00` with no gamepad and at rest. A small tilt inside the 0.15 deadzone reads the same. The stick full right reads `Left stick: 1.00, 0.00`. Full up reads `Left stick: 0.00, -1.00`. Unplug the pad and the label reads `0.00, 0.00` again.

---

## Session Notes

### 2026-11-08 17:51
- `/autorun-feature input/gamepad-axes` started. Preconditions met: plan + requirements exist, dependency input-state complete and merged at base `240f8d4`, feature branch created, tree clean.
- Scaffolded `tasks.md` (22 tasks, 3 phases, all STANDARD tier), `context.md`, `inbox.md`. Set `implementation.md` Status: In Progress.
- Next: phase 1 group 1 on a STANDARD dev agent.

### 2026-11-08 18:06 (phase 1 build)
- Phase 1 group 1 complete on a STANDARD dev agent. The agent added `UiGamepadAxis` (six members, 0 to 5) to `UiEventEnums.cs`. It wrote `UiGamepadDeadzone.cs` (`ApplyStick` radial with rescale and clamp to length 1, `ApplyTrigger` map then low-end linear zone). It added `Axis(UiGamepadAxis)`, `LeftStick`, `RightStick`, and the changed remarks to `UiGamepadState.cs`. It wrote `GamepadDeadzoneTests.cs` (9 tests). Stayed inside the five-file scope.
- Build 0 Warning/0 Error. Solution 2873 total (0 failed, 3 skipped = the base display tests). Core.Tests 1795 (base 1786 + 9). `GamepadDeadzoneTests` 9/0. `InputStateTests` 32/0. `GlfwUiInputGamepadTests` 6 (5 pass, 1 skip, as at base).
- Plants 1-3 each exit 2 with the named target failed, and each restored file equals its copy under `cmp`. Plants 1 and 2 each fail one collateral rule test too (recorded in the Build Log). No plan ambiguity. No new decisions.
- Cleaned the `plantscratch` backups and the `.scratch` logs. Ready for phase 2.

### 2026-11-08 18:58 (phase 2 build)
- Phase 2 group 1 complete on a STANDARD dev agent. `GlfwUiInput.cs` gained `GamepadAxisCount = 6`, the two default constants, and the backing fields. It gained `StickDeadzone` + `TriggerDeadzone` with the 0 to 1 check. The setter works after `Dispose`. The file gained the internal `GamepadState` conversion and `PollGamepads()`. `PollGamepads()` covers 16 slots, the unsafe no-pin fixed-buffer read of `Buttons` and `Axes`, `Context.SetGamepad(id, in state)` per slot, `default` on absent and disposed, and no early return. Class and `PollGamepad` remarks updated (2.4). New `GlfwUiInputGamepadPollTests.cs` (8 tests, collection `GlfwUiInputAllocation`). `GlfwUiInputGamepadTests.cs` grew the `Poll` helper and `ALivePollAllocatesNothing` (A7) — three hunks only. Stayed inside the five-file scope. The agent touched `UiGamepadDeadzone.cs` for plant 7 only, then restored it (`cmp` equal).
- Build 0 Warning/0 Error. Solution 2881 total (0 failed, 3 skipped). `GlfwUiInputGamepadPollTests` 8/0 — the demo, and the null-platform test runs here (no skip). `GlfwUiInputGamepadTests` 6 (5 pass, 1 skip, as at base). `xvfb-run` once: `succeeded: 5, skipped: 1` (sandbox X11 block, Gotcha 3) — PASS line handed to Needs Human Verification.
- Plants 4-8 each exit 2 with the named target failed (plants 4 and 6 as noted in the Build Log). Each restored file equals its copy under `cmp`.
- Deviations: the right-stick expectation in the conversion test follows the merged clamp formula, not the plan text (see Deviations). The binding query is `IsPlatformSupported` (see Decisions).

### 2026-11-08 18:44 (phase 3 group 1 build)
- Phase 3 group 1 complete on a STANDARD dev agent (sample only. Tasks 3.5-3.8 stay open). `HelloScreen.cs` gained the fourth label in `BuildWidgetPanel`: `out Label stickLabel`, not pickable, right after the input label and before the image, starts with `LongestStickText`. The agent added `StickText(in UiGamepadState, Span<byte>)` and the private `StickComponent`. A non-finite value gives 0. `StickComponent` clamps to -1 to 1 and rounds to two decimals. `+ 0f` kills `-0.00`. `LongestStickText` holds 24 bytes. The class remarks now name four labels. `HelloModes.cs` gained `_stickLabel` / `StickLabel`, the constructor check, its exception doc, and the factory summary of the four out fields. `Program.cs` now calls `input.PollGamepads()` outside the UI region, and `state.Map.PushGamepad(ui.Input.Gamepad(0).Buttons);` is the first UI-region line (A6). The group added `Label stickLabel` after `inputLabel`, `byte[] stickScratch = new byte[32];`, and the stick label write inside the UI region. The agent updated the `RunFrames` remarks. Stayed inside the four-file scope. `git status` confirms it.
- Build 0 Warning/0 Error. Solution 2881 total (0 failed, 3 skipped) — no new test in this group, the Phase 2 target holds.
- Smoke: `scripts/test-linux.sh` fails at its smoke step with `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, exit 1 — the known sandbox X11 block (Gotcha 3). Plant 9 builds with 0 Error(s) and restores equal under `cmp`. Its byte measurement (expected line 3: 2808 bytes, exit 1) cannot run here because the sample dies at GLFW init before the frame loop. The PASS run, plant 9, and the window demo land in Needs Human Verification.

### 2026-11-08 19:05 (phase 3 group 2 build)
- Phase 3 group 2 complete on a STANDARD dev agent (docs only). Task 3.5: section 9 gained item 12 "The gamepad poll". The item 11 seam sub-item and the **`PollGamepad`** action-map bullet now name `PollGamepads`. Task 3.6: section 14 gained the bullet **Gamepad axes tests.**. The **Action map tests.** bullet grew, and the **Sample projects** bullet reads "the three label updates", names the push from the slot, and names `PollGamepads`. Task 3.7: `CHANGELOG.md` gained three Added entries and one Changed entry.
- Build 0 Warning/0 Error. Solution 2881 total (0 failed, 3 skipped). Demos: `GamepadDeadzoneTests` 9 with `failed: 0`, and `GlfwUiInputGamepadPollTests` 8 with `failed: 0`. The smoke step stays blocked here (Gotcha 3), and plant 9 stays open for a person.
- Task 3.8: the lint passes on the four named files. The record here kept every fact, number, file name, and command while the prose changed. The group recorded the epic spelling note for `/update-epic input`. No `src/`, `samples/`, `tests/`, or `scripts/` file changed in this group.

### 2026-11-08 19:21 (review fixes, group 1: CR-1, CR-2)
- Review fix group 1 complete on a STANDARD dev agent (tests only). The group edits `GlfwUiInputGamepadPollTests.cs`, this file, and the two CR-1/CR-2 ticks in `code-review.md`. No `src/` file changed: the two temporary guard plants restored byte-equal under `cmp`, and `git status` matches the pre-group state. No test method added; the class stays at 8 tests.
- CR-1: `ADisposedPollAllocatesNothing` seeds slot 0 with a held `UiGamepadButtons.A` and an axis of 0.5 before `Dispose`. Three asserts follow the measure: slot 0 reads disconnected, no button, and axis 0 of 0. A no-op disposed poll leaves the seed and fails the test. Deviation from the brief sketch: the sketch asserted `Released == A` after the measure. Each later `SetGamepad` write recomputes the edge and clears it, so the 100 measured polls end with no edge — `ADisposedAdapterWritesDisconnectedSlots` shows that. The stable-state asserts stand in place, the same shape the null-platform test already uses.
- CR-2: `TheConversionAppliesTheDeadzoneSettings` gained a property-reads-through pin. The test sets `StickDeadzone` 0.15 and `TriggerDeadzone` 0.1 on a live seam adapter, converts through the property reads, and compares all six stored values against the literal-deadzone result at five decimals. A swapped getter fails the pin. The call-site wiring inside `PollGamepads` stays the two-line visible difference the review records.
- Guard bites (temporary plants in `GlfwUiInput.cs`, staged under `src/BrutalUi.Glfw/obj/plantscratch/`, each restored with `cp`, `cmp` equal, scratch deleted):
  - Plant `if (_disposed) { return; }` as the first line of `PollGamepads`: `ADisposedPollAllocatesNothing` failed (the new guard bites — it passed under plant 5 before the fix), and `ADisposedAdapterWritesDisconnectedSlots` failed (recorded collateral). Plant build: `0 Warning(s)` `0 Error(s)`.
  - Plant the two deadzone getters swapped: `TheConversionAppliesTheDeadzoneSettings` failed (the new pin bites), and `TheDeadzoneSettingsCheckTheRange` failed (collateral: the defaults read back swapped). Plant build: `0 Warning(s)` `0 Error(s)`.
- Measured after both restorations: build `0 Warning(s)` / `0 Error(s)`; solution total 2881, failed 0, skipped 3; `GlfwUiInputGamepadPollTests` total 8, failed 0, skipped 0; `GamepadDeadzoneTests` total 9, failed 0. The two 0-byte tests still measure 0 bytes.

### 2026-11-08 19:30 (review fixes, group 2: CR-3)
- Review fix group 2 complete on a STANDARD dev agent (prose only). The group edits one comment sentence in `src/BrutalUi.Core/Input/UiEventEnums.cs`, this file, and the CR-3 tick in `code-review.md`. No code changed: no new member, no signature change, no behavior change. `UiGamepadButtons` keeps its own "shared type of the GLFW adapter and the action map" sentence — that claim is true today. The review group 1 changes in `GlfwUiInputGamepadPollTests.cs` stand untouched.
- CR-3: the `UiGamepadAxis` remarks said "The enum is the shared type of the GLFW adapter and the action map." Nothing in `src/BrutalUi.Framework/` names `UiGamepadAxis` today; the sharing arrives with input-actions. The sentence now reads "The enum is the shared type of the GLFW adapter and the state reads, and input-actions binds through it." — true today, and it names the coming use without claiming it exists. The remarks wrap over one added line.
- Measured: build `0 Warning(s)` / `0 Error(s)`; solution total 2881, failed 0, skipped 3; `GamepadDeadzoneTests` total 9, failed 0; `GlfwUiInputGamepadPollTests` total 8, failed 0, skipped 0. `git diff` of `UiEventEnums.cs` shows only the changed comment line (plus its wrap); the enum block keeps its added-lines-only shape, and no other type in the file changed.

### 2026-11-08 19:40 (review fix, group 3: Q5 lint sweep)
- Review fix group 3 complete on a STANDARD dev agent (docs only). The pass swept the prose of `code-review.md` for the Q5 gate. The score went from 4.38 to 0.52 hits per 100 words, under the 2.5 bar. Every finding, severity, file path, line number, test name, byte count, and CR tick stayed.
- The line 20 note in this file now names the five scored files. No `src/`, `samples/`, `tests/`, or `scripts/` file changed. Build 0 Warning/0 Error. Solution 2881 total, 0 failed, 3 skipped. The class totals stay 9 and 8.

### 2026-11-08 19:48 (run end: review verified, docs rolled up)
- `/autorun-feature input/gamepad-axes` reached the end of its loop. 22/22 tasks, three phase verifiers PASS, the cross-phase review closed (3 LOW findings, 3 fixed), and the post-review verifier PASS on its second pass after one fix round. `implementation.md` Status: Ready for Review.
- Final measured state: build `0 Warning(s)` / `0 Error(s)`. Solution 2881 total, 0 failed, 3 skipped (the three base display tests). `GamepadDeadzoneTests` 9, `GlfwUiInputGamepadPollTests` 8, `InputStateTests` 32, `GlfwUiInputGamepadTests` 6 (5 pass, 1 skip). Core.Tests 1795 (base 1786 + 9), Glfw.Tests 40 (base 32 + 8). ASD-STE100 lint exit 0 over five files: 1.53, 1.55, 2.07, 1.66, 0.52.
- Scope holds exactly I1: 7 changed files plus 3 new source and test files, protected files zero-diff, no project file, no `scripts/` change, no `src/BrutalUi.Framework/` change, no native dependency, no plant residue.
- One plan correction landed during the run: `implementation.md` task 2.5 and DoD F6 said `RightStick` (-0.75, 1) for an input of length 1.25 at a deadzone of 0, which contradicts the plan's own Decision 2 clamp. The parent confirmed the arithmetic and reconciled both places to (-0.6, 0.8). The shipped test matches the corrected plan.
- Inbox: 0 lines open.
- Inbox bug: Verifier FAIL: Q5 doc lint gate 7 - `code-review.md` reads per100w=4.38 (129 hits over 2948 words), over the 2.5 bar. Drivers: long_sentence(>20w)=50, semicolon=48, passive_voice=15, contraction=7, em_dash=31. The other four files pass (1.53, 1.55, 2.10, 1.66). (done: review-fix1 — prose sweep took it to 0.52, gate 7 exit 0 on the second pass)
- Not committed on purpose. The branch `feat/input/gamepad-axes` (base `240f8d4`) holds the work; the user commits, pushes, and opens the PR.
- Next for a person: the four display-host checks in Needs Human Verification, then `/evaluate-feature input/gamepad-axes`.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
