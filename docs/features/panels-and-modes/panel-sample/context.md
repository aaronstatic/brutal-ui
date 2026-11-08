# panels-and-modes/panel-sample - Context and Decisions

**Last Updated:** 2026-11-08 11:42
**Current Phase:** Complete (phases 1 to 3 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**Done:**

- Phase 1 (tasks 1.1–1.9): `HelloModes.cs` owns the mode model. `HelloScreen` splits into four build methods. The sheet styles the layout root, the two slots, and each panel root. `Program.cs` creates `HelloModes` and calls `SetMode`. Build: 0 warnings, 0 errors. Tests: 2821 (3 skipped), 0 failed, no count change.

- Phase 2 (tasks 2.1–2.8): the smoke run presses F1 and F2 in turn. It caches the `Add` point per Decision 6, counts the mode frames, prints line 7, and exits 1 when a mode count is 0. `docs/overview.md` holds the modes smoke contract line. Build: 0 warnings, 0 errors. Tests: 2821 (3 skipped), 0 failed, no count change. Phase 2 adds no test. The smoke run and both plants need a window: Needs Human Verification (see below).

- Phase 3 group 1 (tasks 3.1–3.3): `ModeDiffAllocationTests` (3 tests, the eighteenth gate) passes headless. It shows 0 bytes for the 100-step toggle cycle and for the 100-step idle frame, plus the real-work tree check. Plants 3–5 each failed exactly the named tests and restored clean. Build: 0 warnings, 0 errors. Tests: 2824 (3 skipped), 0 failed. `BrutalUi.Framework.Tests` = 225. Class run total 3, failed 0.

- Phase 3 group 2 (tasks 3.4–3.8): section 14 of `docs/technical-design.md` holds the gate bullet and the extended sample bullet. `CHANGELOG.md` holds three Added lines and one Changed line. `scripts/test-linux.sh` passed the build, test, gpu, and loader steps and failed the smoke step on the host window fault (A8). Split evidence and the Needs Human Verification lines follow. Demos re-ran, the `src/` snapshot holds, and each changed doc passes the STE lint.

- Cross-phase review (`code-review.md`): 3 findings (1 MEDIUM, 2 LOW), all fixed and verified. CR-3 marked the stale line-2/line-6 smoke counts in section 14 as pending a windowed re-measure. CR-1 reworded five flattened-negation prose lines. CR-2 dropped the unused `HelloState state` parameter of `PushSmokeInput`. The review-fix verifier returned PASS with Demo PASS: build 0/0, solution 2824 (3 skipped) 0 failed, gate class 3/0, `src/` snapshot holds, STE lint passes.

**Next:**

- The person tests the "Needs Human Verification" items, then commits, pushes, and opens the PR for `feat/panels-and-modes/panel-sample`.
- The epic closes with `/update-epic panels-and-modes` and `/update-master`.

**Blockers:**

- None for the code.

---

## Base and Counts

The epic context that binds this feature. Read `.beast-mode/epic-resolution.md` §4 sources.

- **Epic:** panels-and-modes, feature 4 of 4 (the last). Depends on panel-definition, slot-container, and mode-set — all three are Complete on this branch.
- **Branch:** `feat/panels-and-modes/panel-sample`, created from `main` at base commit `a2a5184` (which already holds mode-set). The working tree is clean and `main` equals `origin/main`.
- **Base commit (recorded in task 1.1):** `a2a5184d39e5d80d5aa7dcd52b42c7dad0fc9290` (`git rev-parse HEAD`).
- **`src/` snapshot (task 1.1):** the mode-set files are committed at `a2a5184`, so `src/` is clean. `git diff HEAD -- src/ | sha256sum` and `git status --porcelain --untracked-files=all -- src/ | sha256sum` both give the hash of empty output: `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`. The end-state checks in tasks 3.3 and 3.7 must give these two hashes again.
- **Verified src files (task 1.1):** `src/BrutalUi.Framework/Panels/UiManager.cs`, `src/BrutalUi.Framework/Panels/ModeHistory.cs`, and `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs` all exist.
- **Solution test count at base (measured in task 1.1):** total 2821, skipped 3, failed 0. `BrutalUi.Framework.Tests` total: 222, failed 0. These match the planning numbers. This feature adds `ModeDiffAllocationTests` (3) in Phase 3 → target 2824 (3 skipped), Framework.Tests 225. Phase 1 must change no test count.

## Integration Points (from `implementation.md`)

| Point | Owner | Use in this feature | Rule that binds it |
|-------|-------|---------------------|--------------------|
| `UiManager.Register`, `SetMode`, `CurrentMode`, `TargetMode`, `History` | mode-set | `HelloModes` and the gate | Register the panels and the layout before the modes. `SetMode` from a command applies at the start of the next `Update`. |
| `ModeHistory.Back()` | mode-set | `hello.back` | Returns `false` for an empty history. The apply of a back step adds no entry. |
| `UiManager.Apply` step 5 (the map) | mode-set | Both modes name `HelloState.Map` | The diff calls `Activate` on each apply, also for the same map. `Activate` writes the same delegate again and allocates nothing. |
| `PanelController.EnsureRoot()` | panel-definition | `HelloModes` builds each root at build time | The manager mounts the built root. No factory runs in a frame. |
| `SlotContainer` type id `SlotContainer` | slot-container | The type rule of `HelloScreen.css` | A type selector matches each slot. |
| `UiActionMap.Commands`, `AddAction`, `BindKey` | state-and-commands | `hello.mode`, `hello.back`, F1, F2 | One registry for the map, the buttons, and the panel context. A focused text field passes a non-text key to the map. |
| `CommandBindings.BindCommand` | state-and-commands | `Mode (F1)` button | The bind reads the name at click time. |
| Smoke input and `ViewportCenter` | dev-ops, interactive-elements | The Add click and the F keys | `ViewportCenter` needs an element in the tree. The Add point comes from the last frame with the state panel in the tree (Decision 6). |
| `AllocationGate.ReleaseAllocationContext()` | state-and-commands | The gate | Call it right before each measure. |

---

## Environment / Host Rules

- **Build and test:** set `MSBUILDDISABLENODEREUSE=1` and pass `-nodeReuse:false`. Never run `dotnet build-server shutdown`. A warning fails the build (`TreatWarningsAsErrors`). Projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces.
- **Known flakes:** a test host can exit 134 (Internal CLR error 0x80131506). `TheCurvesAllocateNothing` failed once with 8128 bytes. Run the tests a second time before treating either as a fault.
- **GPU window / smoke run:** the HelloPanel smoke run (`dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames N`) opens a Vulkan window and needs a GPU desktop session. On the prior mode-set run, `scripts/test-linux.sh` failed at the smoke step because Xvfb segfaults inside the NVIDIA EGL stack during GLX init — a host fault, not a repo fault (A8). When no window opens: record the build result, the test result, and the split evidence in `context.md`, add a "Needs Human Verification" line, and go on. Do not change `src/` to force a window.
- **No browser tool:** this is a native game UI (Vulkan/GLFW), not a web app. There is no browser automation. UI verification is: the build + test gate, class-filtered test runs, and the windowed smoke run (MANUAL when the host cannot open a window).
- **Plants:** for each plant, copy the file to a scratch folder first, run the check, expect the failure, restore, verify with `cmp`, and re-run. A plant that does not compile leaves the old binary and the old binary passes — check `0 Error(s)` before reading the result. Run all plants of one phase inside one chained shell call (a scratch folder under `/tmp` does not persist between calls).

---

## Important Decisions

Carried from `implementation.md` (see Key Technical Decisions there). These bind the phases.

- **Decision 1:** a new `samples/HelloPanel/HelloModes.cs` owns the `PanelContext`, `UiManager`, the registers, the two commands, the two actions, the two keys, and the mode button. `HelloScreen` keeps the tree builders. `Program.cs` creates `HelloModes` and calls `SetMode`.
- **Decision 2:** one command registry (`state.Map.Commands`) serves the map, the buttons, and the `PanelContext`. No `HelloState` code change.
- **Decision 3:** `HelloModes` builds each panel root at build time through `EnsureRoot()`, so the smoke run has `WidgetPanel`, `GateLabel`, and `Widgets` before frame 0 and no factory runs in a frame.
- **Decision 4:** `hello.mode` toggles from `TargetMode ?? CurrentMode`. `Switches` counts each `hello.mode` run. `Backs` counts only a `Back()` that returns true.
- **Decision 5:** `PushSmokeInput` pushes F1 on even `(frameIndex - WarmUpFrames)` and F2 on odd, in every frame (warm-up included).
- **Decision 6:** the `Add` click aims at a point cached after each `hello.main` frame (`ViewportCenter` needs an element in the tree).
- **Decision 7:** the layout root and both slots set `IsPickable = false`.
- **Decision 8:** `HelloScreen.css` renames `.hello-root` to `.hello-slot-root` and adds a `SlotContainer` type rule and `.hello-panel-root`. The sheet goes from 31 to 33 rules.
- **Decision 9:** the gate uses a closed toggle cycle with a store change and `Draw` (see Phase 3).
- **Decision 10:** plants 3 and 4 edit `src/BrutalUi.Framework/Panels/UiManager.cs` for one run only and restore it. The `src/` snapshot of task 1.1 proves the end state (A1).

---

## Key Files (end state)

### Sample
- `samples/HelloPanel/HelloModes.cs` (new, phase 1): the mode model of the sample.
- `samples/HelloPanel/HelloScreen.cs` (phase 1): `Build` splits into four build methods.
- `samples/HelloPanel/HelloScreen.css` (phase 1): `.hello-slot-root`, `SlotContainer`, `.hello-panel-root`.
- `samples/HelloPanel/HelloState.cs` (phase 1): doc comments only.
- `samples/HelloPanel/Program.cs` (phase 1 wiring, phase 2 smoke run + line 7 + exit rule). 1166 lines — read in ranges only.

### Tests
- `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs` (new, phase 3): 3 tests, the eighteenth gate.

### Docs
- `docs/overview.md` (phase 2): the Phase 3 modes smoke-contract line.
- `docs/technical-design.md` (phase 3): section 14.
- `CHANGELOG.md` (phase 3): Added and Changed.

### Do not change
- Each file of `src/` in the end state, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `HelloWidgets.cs`, `SampledTexture.cs`, `VulkanContext.cs`, and each existing test file.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Needs Human Verification (Phase 1): the smoke run with `--frames 120` must print its lines with `Update and Draw 0` and `exit=0` on a host with a GPU window. On this headless host GLFW reports `PlatformUnavailable` and no window opens. Phase 2 now expects seven lines. See the Phase 2 line below.

- [ ] Needs Human Verification (Phase 1): the windowed demo. F1 removes the state row and shows the info panel. F2 brings the state row back, and a second F2 changes nothing. A click on `Mode (F1)` toggles. F1 works while the text field holds the focus.

- [ ] Needs Human Verification (Phase 2): the smoke run with `--frames 120` must print seven lines. Line 3 reads `Update and Draw 0`, and line 6 holds each count above 0. Line 7 reads `HelloPanel: modes: switches 60, backs 59, main frames 59, detail frames 58.`, and `exit=0`.

- [ ] Needs Human Verification (Phase 2): on this headless host `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120` prints `HelloPanel: GLFW init failed: PlatformUnavailable (Failed to detect any supported platform).` and exits 1 before any line. One `xvfb-run -a` attempt gives the same GLFW failure (A8).

- [ ] Needs Human Verification (Phase 2): both plants on `HelloModes.cs` must give exit 1 with their named failures.

- [ ] Plant 1 (remove the line `modes.Backs++;`): expect line 7 with `backs 0`, the line `HelloPanel: the synthetic input did not reach the mode switch.`, and `exit=1`. Plant 2 (`GC.KeepAlive(new object());` as the first line of `s_mode`): expect line 3 with bytes above 0 and `exit=1`. Both plants built with `0 Error(s)` here. The smoke run reached only the GLFW failure, so the exit codes cannot show on this host. Each plant file is back, `cmp` equal, and the final build reads 0 warnings, 0 errors.

- [ ] Orchestrator APPROVED deviation (2026-11-08): two existing Markup tests (`HelloPanelSheetTests`, `ParsedSheetAllocationTests`) pinned the old sample sheet. Phase 1 updated only their expected values (33 rules, `.hello-slot-root`, `SlotContainer`, `.hello-panel-root`). Decision 8 intentionally changes the linked sheet, so its pins must track it. No test was added or removed. (Markup holds 578, and the solution held 2821 before Phase 3.)

- [ ] Definition-of-Done I1 reads as: `ModeDiffAllocationTests.cs` is the only new test file, and these two Markup pins change as a direct consequence of Decision 8. Section 14 of `docs/technical-design.md` records this (done in task 3.4). The cross-phase review checks it.

- [ ] Needs Human Verification (Phase 3, gate): re-run `scripts/test-linux.sh` on a host with a repaired window step. On this host the script exits 1 at the smoke step, and the smoke log reads `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).` The build, test, gpu, and loader steps passed. Section 14 and the session note below hold the split evidence.

- [ ] Needs Human Verification (Phase 3, Windows): run `scripts/test-windows.ps1` on Windows. The script reads the exit code of the smoke run, so the new mode check runs there too.

- [ ] Needs Human Verification (review CR-3): re-measure the seven smoke lines on a windowed host and update the sample bullet of section 14 with the real line 2 and line 6 counts.

---

## Session Notes

### 2026-11-08 11:42 (feature close: review, fixes, and docs)
- Cross-phase review wrote `code-review.md`: 3 findings (CR-3 MEDIUM, CR-1 LOW, CR-2 LOW); rules 1 to 6, 8, and 9 passed. Review-fix group 1 fixed CR-3 (section 14 counts pending a windowed re-measure + a Needs Human Verification line). Review-fix group 2 fixed CR-1 (five prose lines) and CR-2 (dropped the unused `PushSmokeInput` `state` parameter).
- Review-fix verifier: PASS, Demo PASS. Observed: build 0/0; solution 2824 (3 skipped), 0 failed; `*ModeDiffAllocationTests` 3/0; both `src/` snapshot hashes equal the task 1.1 value; STE lint passes on the changed docs.
- Inbox folded: the one `note` line is done (section 14 records it; DoD I1 updated; the review confirmed the two Markup pins). No open bug line, so no `docs/bugs/` promotion.
- All 25 tasks are complete. `implementation.md` Status is Ready for Review. Next: the person runs the Needs Human Verification items (the windowed F1/F2 demo, the seven-line smoke, plants 1 and 2, the repaired-host `scripts/test-linux.sh`, and `scripts/test-windows.ps1`), then commits, pushes, and opens the PR.

### 2026-11-08 11:20 (Phase 3, group 2, tasks 3.4–3.8 — the docs and the full gate)
- Task 3.4: section 14 of `docs/technical-design.md`. The `**Mode tests.**` bullet now reads in the past: panel-sample
  planned the gate, and the gate exists. The new bullet `**An eighteenth allocation gate.**` follows the shape of the
  earlier gate bullets. It names `ModeDiffAllocationTests` (3 tests) in `tests/BrutalUi.Framework.Tests/Panels/`, the
  scene, the toggle step with its sum 1200, the idle step with its sum 300, the real-work checks, and plants 3–5 with
  their results and bytes (9600, 2400, and the sum-12 empty body).

- The `**Sample projects**` bullet now prints 7 lines, with line 7 `HelloPanel: modes: switches 60, backs 59, main
  frames 59, detail frames 58.` The bullet adds the two modes, the F1 and F2 script, the cached `Add` point, the mode
  check, and plants 1 and 2. Both plants built with `0 Error(s)` here and stopped at the window step, so the bullet
  states the expected results and says a person runs both on a host with a window.

- The fourth-gate bullet now reads the 33 rules of `HelloScreen.css`. Section 14 states that the Decision 8 sheet
  change moved the two Markup pins from 31 to 33 rules with value-only edits, and that `ModeDiffAllocationTests.cs`
  is the only new test file of the feature.

- Task 3.5: `CHANGELOG.md`. Under `### Added`, three new lines follow the mode-set lines: the HelloPanel modes, the
  smoke run's seventh line, and `ModeDiffAllocationTests`, the eighteenth allocation gate. Under `### Changed`, one
  new line first: the `HelloScreen.Build` split, the `UiManager` root of the sample, and the class rename.

- Task 3.6, the full gate: `MSBUILDDISABLENODEREUSE=1 timeout 600 scripts/test-linux.sh` gave exit 1. The script
  stopped at the smoke step. Split evidence: the build step passed with `0 Warning(s)` and `0 Error(s)`, and the test
  step passed with total 2824, failed 0, skipped 3. The gpu step passed: kernel module 595.99.02, user space library
  595.99.02, Vulkan ICD OK. The loader step passed: the script went past it to the smoke step. The smoke step failed
  with `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, the host A8 fault.
  This is a host fault, not a code fault. No `src/` file changed.

- Task 3.7, the demos again: the direct smoke run prints `HelloPanel: GLFW init failed: PlatformUnavailable (Failed
  to detect any supported platform).` and exits 1 before any line. The headless run cannot show the seven lines, so
  the Phase 2 Needs Human Verification line covers line 7 and the F1 and F2 demo. The class run
  `--filter-class '*ModeDiffAllocationTests'` gave total 3, failed 0, succeeded 3. Both `src/` snapshot hashes equal
  the task 1.1 value `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.

- Task 3.7, the STE lint at `--fail-over 2.5`: technical-design.md, overview.md, CHANGELOG.md, context.md, and
  implementation.md each pass. Group 1's numbers stay in this file unchanged, and no doc adds an invented count.

- Task 3.8: the Needs Human Verification section now holds ten lines. New lines: the smoke step on a repaired host,
  and `scripts/test-windows.ps1` on Windows. The F1 and F2 demo line and the plants 1–2 line stay from Phase 2.

- Decisions: none beyond the plan. The one open question from Phase 2 stays: the `HelloState state` parameter of
  `PushSmokeInput` is unused, and the Phase 3 review or the next edit removes it or keeps the brief's signature.

### 2026-11-08 11:06 (Phase 3, group 1, tasks 3.1–3.3, the gate)
- Tasks 3.1–3.2: new `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs` (3 tests, the eighteenth
  gate, headless, no window). Verified APIs against `src/`: `UiContext.CreateInfo { Measurer }` +
  `ViewportSize` + `SetFocus`, `CommandRegistry.Register(name, CommandHandler, object?)`,
  `UiActionMap.CreateInfo { Commands }` + `AddAction` + `BindKey` + `IsActive`, `Store<int>`, `Label.BindText`
  (number overload, `IUtf8SpanFormattable`), `Button` + `CommandBindings.BindCommand`, `TextField`
  (`CreateInfo.Capacity`, inline 120 by 24), `SlotContainer`, `UiManager`/`ModeDefinition`/`ModePanel`/
  `PanelDefinition`/`LayoutDefinition`/`PanelController.EnsureRoot`, `ModeHistory.Count`/`Back`, `AllocationGate`,
  and `RecordingRenderer` (linked file, namespace `BrutalUi.Core.Tests`).

- Gate result (clean tree): build 0 warnings, 0 errors. Solution: total 2824, skipped 3, failed 0.
  `BrutalUi.Framework.Tests`: total 225, failed 0. Class run (`--filter-class '*ModeDiffAllocationTests'`):
  total 3, failed 0. The known flakes did not show, and no rerun was needed.

- Plant 3 (`GC.KeepAlive(new object());` first line of `UiManager.Apply`, one run): the class run showed exactly one
  failure. `TheToggleCycleAllocatesNothingAfterWarmUp` failed at 9600 bytes (4 apply steps of 24 bytes, 100 rounds).
  `AnIdleFrameAllocatesNothing` passed, because there is no apply with no target. Full `dotnet test` exit=2.
  Restored, `cmp` equal, and both `src/` hashes `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.

- Plant 4 (same line first in `UiManager.OnBeforeUpdate`, before the target check): both 0-byte tests failed.
  `AnIdleFrameAllocatesNothing` failed at 2400 bytes (1 update of 24 bytes, 100 rounds), and
  `TheToggleCycleAllocatesNothingAfterWarmUp` failed at 9600 bytes (4 updates of 24 bytes, 100 rounds).
  The apply adds nothing without the target.
  `TheMeasuredBodyDoesRealWork` passed, because it reads the tree, not the bytes. Full `dotnet test` exit=2.
  Restored, `cmp` equal, and both hashes equal.

- Plant 5 (empty `ToggleStep` body that returns 12, in the test file): exactly one failure.
  `TheMeasuredBodyDoesRealWork` failed ("Expected: 2, Actual: 0"), because the history did not grow.
  `TheToggleCycleAllocatesNothingAfterWarmUp` passed at 0 bytes with the sum 12. An empty body allocates nothing,
  so the real-work test is the guard. `AnIdleFrameAllocatesNothing` passed. Full `dotnet test` exit=2.
  Restored, `cmp` equal, and `UiManager.cs` still `cmp` equal. Both `src/` hashes equal. The scratch copies sat in
  the workspace `.plant-scratch/` folder, because a workspace folder persists between shell calls. The run removed
  them after the final run.

- Decisions:

  - Measured-body literals: `ToggleStep` calls `SetMode(scene.Detail.Name)` and `SetMode(scene.Main.Name)`. The name
    spans come from the registered `ModeDefinition`, so the body holds no string literal, `u8` or otherwise.
  - Warm-sum asserts: the two 0-byte tests also assert `WarmUpRounds * 12` and `WarmUpRounds * 3`. This matches the
    shape of the two `ModeDiffTests` 0-byte tests. The brief pins the measured sum, and the warm sum pins the same
    rule one round earlier.
  - Factory shape: one `Factory` class records its calls and returns the one root it holds. `BuildScene` builds each
    root first and calls `EnsureRoot()` once per panel (Decision 3). So each factory runs exactly one time, and the
    real-work test asserts that.
  - `BuildScene` asserts `SetFocus(field)` is true. A silent focus loss would drop the caret pass from Decision 9,
    and the gate would measure a lighter body than the plan names.
  - Inline styles only, per the brief: the layout root takes width 800, height 600, and `IsPickable = false`. The
    field takes 120 by 24, and the `Add` button takes 80 by 24. The panel roots and the slots keep auto size.
    `FlexDirection.Column` is the Core default, so the column needs no inline write.

### 2026-11-08 10:48 (Phase 2, group 1, tasks 2.1–2.8)
- Tasks 2.1–2.6 done in `samples/HelloPanel/Program.cs` and `docs/overview.md`:
  - `RunResult`: four new fields after `Versions` (`Switches`, `Backs`, `MainFrames`, `DetailFrames`), each with a `<param>` doc.
  - `PushSmokeInput`: new `float2 addPoint` parameter, and the `Add` click now aims at `addPoint`. After the Escape block and before the longest text burst, the method pushes one key down and one key up with no target: F1 when `(frameIndex - WarmUpFrames) % 2 == 0`, F2 otherwise. The rule runs in every frame, warm-up included (Decision 5). New "The mode script" remark.
  - `RunFrames`: new `HelloModes modes` parameter and the locals `addPoint`, `mainFrames`, and `detailFrames`. After each `ui.Update` the loop reads `modes.Manager.CurrentMode` into a local, as the code reads `fieldFocused` (Decision 4). It caches `addPoint = ViewportCenter(state.AddButton)` in a `hello.main` frame (Decision 6) and counts the measured frames in each mode. The return carries the four new fields, and the remarks cover the mode counts and line 7.
  - `Run`: prints line 7 after the state line. Inside the `frameLimit != NoFrameLimit && result.MeasuredFrames >= 2` block, after the state check, the mode check writes `HelloPanel: the synthetic input did not reach the mode switch.` to standard error and returns `ExitFailure` when one of the four counts is 0. New "The mode check" remark.
  - `docs/overview.md`: exactly one new line, the `- **GPU smoke contract (Phase 3, modes):** ...` bullet after the Phase 3 bullet. `git diff` shows 1 added line and no other change.

- Decisions:
  - Key push: the sample pushes the mode keys as inline `KeyDown`/`KeyUp` `KeyEvent` structs with a `null` header target, like the Tab and the Escape. `PushKeyDown` takes a non-null `Element` caller target, so it cannot push a key with no target.
  - Parity check: the C# remainder keeps its sign, so `(0 - 3) % 2 = -1` (F2), `(1 - 3) % 2 = 0` (F1), and `(2 - 3) % 2 = -1` (F2). That matches Decision 5 word for word: F1 in frame 1, F2 in frames 0 and 2, and F1 again in the first measured frame 3. So `--frames 120` gives switches 60 (frames 1, 3, …, 119), backs 59 (frame 0 meets an empty history), main frames 59, and detail frames 58. This re-check agrees with the plan, and the run re-derived nothing away from it.
  - The `HelloState state` parameter of `PushSmokeInput` stays (brief 2.2 only adds `float2 addPoint`). The body does not use `state` now, and the parameter doc names the link between `addPoint` and the `Add` button. Open question for the Phase 3 review: remove the parameter or keep the brief's signature.
  - `Program.cs` adds `using BrutalUi.Framework;` for the `ModeDefinition?` local. The mode read, the point cache, and the counts stay inside the UI region and allocate nothing (only a reference compare and a `float2` store).
  - The `addPoint` cache runs in every frame, warm-up included. Frame 0 ends in `hello.main`, so the point is valid before the first measured frame. The hit test of frame N+1 reads the layout of frame N.

- Task 2.7: build `0 Warning(s)`, `0 Error(s)`. Tests: total 2821, failed 0, skipped 3, no count change (Phase 2 adds no test). The smoke run prints `GLFW init failed: PlatformUnavailable` and exits 1 before any line, and one `xvfb-run -a` attempt fails the same way (A8). The seven lines cannot print on this host, so see Needs Human Verification (Phase 2) above.
- Task 2.8 (plants on `HelloModes.cs`, scratch copy in the workspace before plant 1):
  - Plant 1: removed `modes.Backs++;`. Build: `0 Error(s)`. Smoke: GLFW failure only. The window never opens, so line 7, the stderr line, and the exit code cannot show here. Restored, and `cmp` equal.
  - Plant 2: added `GC.KeepAlive(new object());` as the first line of `s_mode`. Build: `0 Error(s)`. Smoke: GLFW failure only, same reason. Restored, `cmp` equal, and the run removed the scratch folder.
  - Final rebuild after the restore: `0 Warning(s)`, `0 Error(s)`. The post-restore smoke run again stops at the GLFW failure (`exit=1` from the missing window, not from a check). Both plants sit under Needs Human Verification (Phase 2) above.

### 2026-11-08 10:35 (Phase 1, group 1, tasks 1.2–1.9)
- Tasks 1.2–1.8 done: four build methods in `HelloScreen.cs`, new `HelloModes.cs`, CSS layout rules, `Program.cs`
  wiring (`HelloModes modes = new HelloModes(ui, state, handle);` + `modes.Manager.SetMode(HelloModes.MainName);`,
  no `AddRoot`, no `state.Map.Activate`), and doc comments in `HelloState.cs` (comment lines only).
- Task 1.9 build: `0 Warning(s)`, `0 Error(s)`. Test run: total 2821, failed 0, skipped 3 — no count change. The
  Framework.Tests count stays 222.
- Task 1.9 smoke run: `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120` prints
  `HelloPanel: GLFW init failed: PlatformUnavailable (Failed to detect any supported platform).` on this headless
  host. No window opens (A8), so the six lines and the F1/F2/`Mode (F1)` demo cannot run here. Needs Human
  Verification (see the section above).
- The I2 grep now reads `new HelloModes(` and one `SetMode(HelloModes.MainName)` in `Program.cs` (explicit type on
  the construction, so the verification step finds it). The F6 grep finds `.hello-slot-root`, `SlotContainer`, and
  `.hello-panel-root`, and no `.hello-root`.
- Decision (key codes): `GlfwKey.F1` (290) and `GlfwKey.F2` (291) exist in `Brutal.GlfwApi`, so `HelloModes` binds
  the enum, not raw codes.
- A person updated the two pinned Markup tests for the sheet change. See the task 1.4 deviation note.

### 2026-11-08 (Phase 1, group 1, task 1.4, deviation, recorded cause)
- Decision 8 renames `.hello-root` and adds two rules (31 → 33). Two existing Markup tests read the linked copy of
  `samples/HelloPanel/HelloScreen.css` and pin its old state. So the plan's rule "do not change existing test files"
  and its "0 failed" acceptance collide. `HelloPanelSheetTests.TheSheetHoldsEachRule` states the project convention in
  its own line: "When a phase adds a rule, update the count and record the cause in context.md." We followed it:
  - `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`: `RuleCount` went 31 → 33. The selector list now names
    `.hello-slot-root`, `SlotContainer`, and `.hello-panel-root` in sheet order. The test-tree class `hello-root` went
    to `hello-slot-root`. The remarks are new. No test method added or removed.
  - `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`: `TheHelloPanelSheetGivesTheOldValues` reads the
    `.hello-slot-root` rule (it now also sets `FlexDirection: column`) and interns `hello-slot-root` for its check
    tree. No test method added or removed.
- The test count is unchanged (578 Markup). Verification step 8 (I1) expects "only the new test file" among changed
  test paths. The Phase 3 docs read I1 as: `ModeDiffAllocationTests.cs` is the only new test file, and the two Markup
  pins change as a direct consequence of Decision 8. Orchestrator APPROVED this reading on 2026-11-08.

### 2026-11-08 10:11 (Phase 1, group 1, task 1.1)
- Task 1.1 done: `src/` files verified, snapshot hashes and base counts recorded above, Integration Points copied from the plan.
- Verified API shapes against `src/`: `UiManager.CreateInfo { required Context, required Panels, HistoryCapacity }`, `UiManager.Register(PanelDefinition|LayoutDefinition|ModeDefinition)`, `SetMode(ReadOnlySpan<byte>)`, `CurrentMode`/`TargetMode` (`ModeDefinition?`), `History` (`ModeHistory` with `bool Back()`), `PanelController.EnsureRoot()`, `PanelInstance.Root` (`Element?`), `PanelDefinition(name, defaultSlot, Func<PanelContext, Element>)`, `LayoutDefinition(name, Func<PanelContext, Element>)`, `ModeDefinition(name, layout, ReadOnlySpan<ModePanel>, UiActionMap?)`, `ModePanel(slot, panel)`, `SlotContainer(ReadOnlySpan<byte> name)`, `UiActionMap.AddAction(action, command)`, `BindKey(action, key, modifiers)`, `CommandRegistry.Register(name, CommandHandler, object? target)`, `CommandBindings.BindCommand(button, registry, name)` returns the button, `Button.CreateInfo { Text }`.
- **Decision (GlfwKey):** reflection on `Ahwoo.BRUTAL.Glfw` 2026.10.4 proves `Brutal.GlfwApi.GlfwKey` holds `F1` = 290 and `F2` = 291. The sample uses the enum, not the raw codes. No `GlfwKey.F1` fallback comment is needed.
- The `src/` snapshot is the hash of empty output, because the tree is clean at the base commit. The planning note "uncommitted on this branch" is stale. `a2a5184` already holds mode-set.

### 2026-11-08 10:09
- Feature started via `/start-feature`: status set to In Progress, `tasks.md`, `context.md`, and `inbox.md` created.
- Branch `feat/panels-and-modes/panel-sample` created from `main` (base `a2a5184`), clean tree. No commit, no push.
- Epic context loaded: epic-overview, epic-requirements, and mode-set context (the direct dependency). Host Xvfb fault and flake rules recorded above.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
