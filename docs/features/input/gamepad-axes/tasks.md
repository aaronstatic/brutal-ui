# input/gamepad-axes - Task Checklist

**Last Updated:** 2026-11-08 19:48
**Progress:** 22/22 tasks complete (100%)

> Full task text lives in `implementation.md` (Implementation Phases). The phase brief copies it word for word. Estimate: about six steps for one task (`.beast-mode/phase-handoff.md` §1).

---

## Phase 1: The Deadzone Math and the Reads in Core (6/6 complete)

**Demo:** a developer runs the built Core test binary with `--filter-class '*GamepadDeadzoneTests'` on this host and reads a total of 9 with `failed: 0`.
**Needs:** none
**Agent tier:** STANDARD (sonnet) — one group (1.1-1.6). Pure math on value types, formulas written out in the plan.

- [x] **1.1 Create/fill `context.md`: `git status --porcelain`, record base commit + solution/Core/Glfw test totals and skipped counts (planning-time 2864 (3 skipped), 1786, 32), the formulas, and the Internal Shape.** Files: `context.md`. Est: 6.
- [x] **1.2 `UiEventEnums.cs`: add `public enum UiGamepadAxis` after `UiGamepadButtons`, six members 0-5 with the GLFW remarks (Y sign, trigger range).** Files: `UiEventEnums.cs`. Est: 5.
- [x] **1.3 `UiGamepadDeadzone.cs` (new): `ApplyStick` (radial, rescale+clamp) and `ApplyTrigger` (map then linear deadzone), with edge-case rules and doc comments.** Files: `UiGamepadDeadzone.cs`. Est: 7.
- [x] **1.4 `UiGamepadState.cs`: `using Brutal.Numerics;`, `Axis(UiGamepadAxis)`, `LeftStick`, `RightStick`, and the changed remarks.** Files: `UiGamepadState.cs`. Est: 6.
- [x] **1.5 `GamepadDeadzoneTests.cs` (new): the 9 tests, using the worked values.** Files: `GamepadDeadzoneTests.cs`. Est: 8.
- [x] **1.6 Build + tests + `GamepadDeadzoneTests` (total 9, failed 0); plants 1-3 with restore + `cmp`; record.** Files: (verify), `context.md`. Est: 8.

**Acceptance:** 9 tests pass; `InputStateTests` (32) and `GlfwUiInputGamepadTests` (6) pass with no edit; plants 1-3 exit non-zero and restored files equal their copies; `UiEventEnums.cs` diff is added lines only; `UiContext.cs`, `UiInputState.cs`, `InputStateWriter.cs` show no diff.

---

## Phase 2: The Poll of the 16 Slots (8/8 complete)

**Demo:** a developer runs the built Glfw test binary with `--filter-class '*GlfwUiInputGamepadPollTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet) — one group (2.1-2.8). The `unsafe` read copies the existing `PollGamepad` pattern; task 2.6 spells out the null-platform restore.

- [x] **2.1 `GlfwUiInput.cs`: `GamepadAxisCount = 6`, the two default constants, backing fields, `StickDeadzone` + `TriggerDeadzone` properties with the range check and remarks.** Files: `GlfwUiInput.cs`. Est: 6.
- [x] **2.2 `GlfwUiInput.cs`: `internal static UiGamepadState GamepadState(buttons, axes, stickDeadzone, triggerDeadzone)` after `GamepadButtons`.** Files: `GlfwUiInput.cs`. Est: 7.
- [x] **2.3 `GlfwUiInput.cs`: `public void PollGamepads()` — loop 16 slots, unsafe span read of buttons + axes, `GamepadState`, `Context.SetGamepad`; `default` on absent/disposed.** Files: `GlfwUiInput.cs`. Est: 8.
- [x] **2.4 `GlfwUiInput.cs`: update the class + `PollGamepad` remarks (remove "reads no axis" from class; see cref to `PollGamepads`).** Files: `GlfwUiInput.cs`. Est: 4.
- [x] **2.5 `GlfwUiInputGamepadPollTests.cs` (new): 7 tests (buffer/constants, conversion map, deadzone settings, settings range, disposed poll, two 0-byte tests).** Files: `GlfwUiInputGamepadPollTests.cs`. Est: 8.
- [x] **2.6 `GlfwUiInputGamepadPollTests.cs`: 8th test `TheNullPlatformPollWritesSixteenAbsentSlots` (GLFW null platform, 0-byte measure, hint reset in finally).** Files: `GlfwUiInputGamepadPollTests.cs`. Est: 8.
- [x] **2.7 `GlfwUiInputGamepadTests.cs` (A7): `Poll` helper calls `PollGamepads()`; `ALivePollAllocatesNothing` asserts a disconnected absent slot; update summaries.** Files: `GlfwUiInputGamepadTests.cs`. Est: 5.
- [x] **2.8 Build + tests + `GlfwUiInputGamepadPollTests` (8) + `GlfwUiInputGamepadTests` (6); `xvfb-run` once (may block); plants 4-8 with restore; record.** Files: (verify), `context.md`. Est: 8.

**Acceptance:** 8 poll tests pass (or 7 pass + null-platform skips with `context.md` note); 6 gamepad tests pass/skip as at base; plants 4-8 exit non-zero and restored files equal their copies; `GlfwUiInputGamepadTests.cs` diff limited to class summary + `Poll` helper + `ALivePollAllocatesNothing`.

---

## Phase 3: The HelloPanel Stick Label and the Docs (8/8 complete)

**Demo:** a developer runs HelloPanel on a host with a window and a gamepad in slot 0, tilts the left stick full right, and reads `Left stick: 1.00, 0.00`. At rest the label reads `Left stick: 0.00, 0.00`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet) — two groups: 3.1-3.4 (sample), 3.5-3.8 (docs). The label copies the pattern of the input label.

- [x] **3.1 `HelloScreen.cs`: `out Label stickLabel` in `BuildWidgetPanel`, `StickText(in UiGamepadState, Span<byte>)`, `StickComponent`, `LongestStickText`, updated remarks.** Files: `HelloScreen.cs`. Est: 7.
- [x] **3.2 `HelloModes.cs`: `_stickLabel` + `StickLabel`, constructor check + exception doc, factory summary.** Files: `HelloModes.cs`. Est: 4.
- [x] **3.3 `Program.cs`: replace `PollGamepad` with `PollGamepads()` (outside UI region), push `ui.Input.Gamepad(0).Buttons`, `stickLabel` param + scratch + label write, updated remarks.** Files: `Program.cs`. Est: 6.
- [x] **3.4 Build + tests + `scripts/test-linux.sh` (smoke PASS, may block) + plant 9 (`StickText` keep-alive, 2808 bytes on a window host); record.** Files: (verify), `context.md`. Est: 6.
- [x] **3.5 `docs/technical-design.md` section 9: sub-item gamepad seam (item 11) + new item 12 (the poll), PollGamepad bullet sentence.** Files: `technical-design.md`. Est: 6.
- [x] **3.6 `docs/technical-design.md` section 14: Action map tests bullet, new "Gamepad axes tests." bullet, Sample projects wording.** Files: `technical-design.md`. Est: 5.
- [x] **3.7 `CHANGELOG.md`: three Added entries + one Changed entry (input/gamepad-axes).** Files: `CHANGELOG.md`. Est: 4.
- [x] **3.8 Build + tests + phase 1/2 demo totals (9, 8); ASD-STE100 lint `--fail-over 2.5` on the four files; record the epic spelling note + decisions.** Files: (verify), `context.md`. Est: 6.

**Acceptance:** build 0/0 and each test passes; `test-linux.sh` PASS or `context.md` sandbox note; plant 9 builds 0 Error and restores equal under `cmp`; section 9 holds item 12 and section 14 holds "Gamepad axes tests."; `CHANGELOG.md` names each new public member; lint no score above 2.5.

---

## Bugs & Issues

**Active Bugs:**
- (none)

**Fixed Bugs:**
- [x] ✅ 🐛 **Q5 doc lint gate 7 fails on `code-review.md` (4.38 per 100 words, bar 2.5)** - Fixed 2026-11-08
  - Fix: prose sweep of the one file. 129 hits over 2948 words became 18 over 3482 words (0.52). Long sentences 50 to 0, semicolons 48 to 0, em dashes 31 to 0, passives 15 to 0. Every finding, severity, path, line number, and byte figure kept. The two stale "four files" lint-score claims in `code-review.md` and `context.md` now name the five files with measured scores. Verifier pass 2: gate 7 exit 0, all five files under the bar. From the inbox, line 19:27.

---

## Cross-Phase Review

`docs/features/input/gamepad-axes/code-review.md` - 3 findings, all LOW, all fixed and verified.

- [x] ✅ CR-1: `ADisposedPollAllocatesNothing` measured only the byte counter, so it passed even when the disposed poll did no work. Fixed: seed a slot before `Dispose`, assert the post-measure stable state. Guard bite proved with the plant 5 shape.
- [x] ✅ CR-2: the remark "the next `PollGamepads()` call uses the new value" had no test and no plant. Fixed: `TheConversionAppliesTheDeadzoneSettings` now calls the conversion through `input.StickDeadzone` and `input.TriggerDeadzone` and compares all six stored values. Guard bite proved with a swapped-getter plant.
- [x] ✅ CR-3: the `UiGamepadAxis` remarks claimed an action-map sharing that arrives with input-actions. Fixed: one comment sentence.

The review found no CRITICAL, HIGH, or MEDIUM item. It re-derived both formulas independently and confirmed the shipped code, the stored-value contract, the poll contract, the allocation discipline, the I1 scope list, and the docs.

---

## Technical Debt

(none)

---

## Notes

### Needs Human Verification
See `context.md`, `## Needs Human Verification`. The agent sandbox blocks the X11 socket (Gotcha 3 of input-state), so the HelloPanel window demo, plant 9's 2808-byte measure, the `scripts/test-linux.sh` smoke PASS run, and the live `xvfb-run` gamepad test stay for a person on a display host. No display exists on this host (`DISPLAY` unset), so no code path here could run them.
