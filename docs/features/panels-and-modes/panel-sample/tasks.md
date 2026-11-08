# panels-and-modes/panel-sample - Task Checklist

**Last Updated:** 2026-11-08 11:42
**Progress:** 25/25 tasks complete (100%)

> Full task text lives in `implementation.md` (Implementation Phases). The phase brief copies it word for word. Estimate: about six steps for one task (`.beast-mode/phase-handoff.md` §1).

---

## Phase 1: Two Modes in the Window (9/9 complete)

**Demo:** a person presses F1 and sees the state row leave and the info panel enter. F2 brings it back.
**Needs:** none
**Agent tier:** STANDARD (sonnet) — one group.

- [x] **1.1 Fill in `context.md`, verify mode-set src files, record the `src/` snapshot + base commit + test counts.** Files: `context.md`. Est: 6.
- [x] **1.2 `HelloScreen.cs`: replace `Build` with `BuildWidgetPanel(checker, modeButton, out gateLabel, out widgets)` + mode row.** Files: `HelloScreen.cs`. Est: 6.
- [x] **1.3 `HelloScreen.cs`: add `BuildStatePanel`, `BuildInfoPanel`, `BuildLayout`; swap `RootClass`→`SlotRootClass` + `PanelRootClass`; rewrite remarks.** Files: `HelloScreen.cs`. Est: 6.
- [x] **1.4 `HelloScreen.css`: rename `.hello-root`→`.hello-slot-root`, add `SlotContainer` and `.hello-panel-root`, 31→33 rules.** Files: `HelloScreen.css`. Est: 6.
- [x] **1.5 `HelloModes.cs` part 1: class, names, `s_mode`/`s_back` handlers, ctor registration + actions + keys + button.** Files: `HelloModes.cs`. Est: 6.
- [x] **1.6 `HelloModes.cs` part 2: `PanelContext`, `UiManager`, register 3 panels + `EnsureRoot()` + layout + 2 modes, copy out refs.** Files: `HelloModes.cs`. Est: 6.
- [x] **1.7 `Program.cs` `RunImGui`: create `HelloModes`, `SetMode(hello.main)`, drop `AddRoot`/`Activate`, pass mode refs to `RunFrames`.** Files: `Program.cs`. Est: 6.
- [x] **1.8 `HelloState.cs` + `Program.cs` doc comments: map rule, `Row` summary, `panel`/`PushSmokeInput` params (no code change in `HelloState.cs`).** Files: `HelloState.cs`, `Program.cs`. Est: 6.
- [x] **1.9 Build + tests (expect 2821/3 skipped/0 failed, no count change) + smoke run six lines; record demo.** Files: (verify only), `context.md`. Est: 6.

**Acceptance:** build 0/0; solution 2821 (3 skipped) 0 failed; smoke prints six lines with `Update and Draw 0` exit 0 (or a Needs Human Verification line); F1/F2/`Mode (F1)` change panels; `HelloState.cs` shows changed `///` lines only.

---

## Phase 2: The Mode Switch in the Smoke Run (8/8 complete)

**Demo:** a developer runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120` and reads the seventh line `HelloPanel: modes: switches 60, backs 59, main frames 59, detail frames 58.` and the exit code 0.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet) — one group. Care with Decisions 5 and 6.

- [x] **2.1 `Program.cs` `RunResult`: add `Switches`, `Backs`, `MainFrames`, `DetailFrames` after `Versions` with `<param>` docs.** Files: `Program.cs`. Est: 5.
- [x] **2.2 `PushSmokeInput`: add `float2 addPoint`; push the `Add` click at it; push F1/F2 by parity before the field press; add "The mode script" remark.** Files: `Program.cs`. Est: 6.
- [x] **2.3 `RunFrames`: add `HelloModes modes` param; locals; cache `addPoint` after a `hello.main` Update; pass to `PushSmokeInput`.** Files: `Program.cs`. Est: 6.
- [x] **2.4 `RunFrames`: count `mainFrames`/`detailFrames` in the measured block; return the four fields; pass `modes`; update remarks.** Files: `Program.cs`. Est: 6.
- [x] **2.5 `Run`: print line 7 with the mode counts; add the mode check (any 0 count → stderr line + ExitFailure); add "The mode check" remark.** Files: `Program.cs`. Est: 6.
- [x] **2.6 `docs/overview.md`: add the Phase 3 GPU smoke contract (modes) line; change no other line.** Files: `overview.md`. Est: 5.
- [x] **2.7 Build + tests (2821, 3 skipped, 0 failed) + smoke run seven lines (line 7 counts, line 3 `Update and Draw 0`, exit 0); record lines.** Files: (verify), `context.md`. Est: 6.
- [x] **2.8 Two plants on `HelloModes.cs` (backs removed → `backs 0` exit 1; `GC.KeepAlive(new object())` in `s_mode` → line 3 bytes exit 1); restore + `cmp`; record or Needs Human Verification.** Files: `HelloModes.cs` (plants), `context.md`. Est: 6.

**Acceptance:** smoke prints seven lines with the demo counts, `Update and Draw 0`, exit 0; both plants exit 1 and the restored file equals its copy; `overview.md` holds only the new contract line.

---

## Phase 3: The Gate, the Docs, and the Full Gate (8/8 complete)

**Demo:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*ModeDiffAllocationTests'` after a build and reads a total of 3 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet) — two groups. Most care: hidden allocations, the empty-body plant, the right-test failures.

### Group 1 — the gate (tasks 3.1–3.3)

- [x] **3.1 `ModeDiffAllocationTests.cs` part 1: class/remarks, consts, `s_count`/`s_score`, `Counter`/`Factory`/`Scene`, `BuildScene` with real widgets, two SlotContainers, two modes.** Files: `ModeDiffAllocationTests.cs`. Est: 7.
- [x] **3.2 part 2: `ToggleStep`/`IdleStep` and the 3 tests (toggle 0-byte, idle 0-byte, real-work). Measured bodies hold no lambda/string/box/array/Assert.** Files: `ModeDiffAllocationTests.cs`. Est: 7.
- [x] **3.3 Build + tests (expect 2824/3 skipped/0 failed, Framework 225) + class run (total 3); plants 3–5 (`UiManager.Apply`, the `OnBeforeUpdate` handler, empty `ToggleStep`); restore + `cmp` + snapshot check; record.** Files: `UiManager.cs` (plants), `context.md`. Est: 7.

### Group 2 — the docs and the full gate (tasks 3.4–3.8)

- [x] **3.4 `docs/technical-design.md` section 14: past-tense the mode-tests bullet, add "An eighteenth allocation gate." bullet, extend the sample bullet with the modes/keys/line 7/check + plants 1–2.** Files: `technical-design.md`. Est: 7.
- [x] **3.5 `CHANGELOG.md`: 3 Added lines + 1 Changed line under [Unreleased].** Files: `CHANGELOG.md`. Est: 5.
- [x] **3.6 Run `MSBUILDDISABLENODEREUSE=1 timeout 600 scripts/test-linux.sh`; expect PASS or record split evidence + Needs Human Verification (Xvfb) + Windows-script line.** Files: `context.md`. Est: 5.
- [x] **3.7 Re-run each phase demo (smoke line 7 + `ModeDiffAllocationTests` total 3); check `src/` snapshot (both hashes equal); STE lint `--fail-over 2.5` on each changed doc + plan.** Files: `context.md`. Est: 6.
- [x] **3.8 Record demos, results, Needs Human Verification items, and any unsettled decision in `context.md`.** Files: `context.md`. Est: 4.

**Acceptance:** the 3 gate tests pass; solution 2824 (3 skipped) 0 failed; plants 3–5 fail the named tests and restores equal; `scripts/test-linux.sh` PASS or split evidence + Needs Human Verification line; each changed doc passes STE lint.

---

## Bugs & Issues

**Active Bugs:**
- (none)

---

## Review Fixes (cross-phase review, 2026-11-08)

- [x] ✅ **CR-3: section 14 sample bullet — stale line 2 and line 6 smoke counts** - Completed 2026-11-08
  - Fix: counts marked pending a windowed re-measure; causes named; Needs Human Verification line added to `context.md`.
- [x] ✅ **CR-1: five flattened-negation prose lines** - Completed 2026-11-08
  - Fix: all five sentences reworded to clear prohibitions (`HelloModes.cs`, `HelloState.cs`, `HelloScreen.cs`, `Program.cs`; comment-only).
- [x] ✅ **CR-2: unused `HelloState state` parameter of `PushSmokeInput`** - Completed 2026-11-08
  - Fix: parameter dropped, note folded into the `addPoint` doc, call site updated.

Review-fix verifier: PASS (build 0/0; solution 2824, 3 skipped, 0 failed; gate class 3/0; `src/` snapshot holds; STE lint under 2.5).

---

## Technical Debt

- (none)

---

## Notes

An independent evaluator runs the Verification Method (implementation.md) from the repo root on Linux. `<base>` = the base commit and `src/` snapshot that task 1.1 records.

*Update this file as tasks are completed.*
