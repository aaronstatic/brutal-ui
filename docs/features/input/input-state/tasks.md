# input/input-state - Task Checklist

**Last Updated:** 2026-11-08 16:25
**Progress:** 32/32 tasks complete (100%)

> Full task text lives in `implementation.md` (Implementation Phases). The phase brief copies it word for word. Estimate: about six steps for one task (`.beast-mode/phase-handoff.md` §1).

---

## Phase 1: The Pointer Delta and Its Gate (10/10 complete)

**Demo:** the user moves the mouse over the game area of HelloPanel, and a label shows the pointer delta. The label reads 0 over the panel.
**Needs:** none
**Agent tier:** ADVANCED (opus, at the preset effort) — the phase edits `DrainEvents`. Two groups: 1.1-1.6 (Core path), 1.7-1.10 (sample).

- [x] **1.1 Create/fill `context.md`: `git status` check for the panel-sample commit, record base commit, test counts, the Internal Shape, Integration Points, and the three hook places.** Files: `context.md`. Est: 6.
- [x] **1.2 `UiInputState.cs` (new): `UiInputState` + `UiPointerState` with `Position`, `Delta`, `Move`, `FinishFrame`, doc comments.** Files: `UiInputState.cs`. Est: 6.
- [x] **1.3 `InputStateWriter.cs` (new): fields, `Create`, `BeginFrame`, `Record` (pointer + scroll position), `EndFrame` (delta + pointer gate).** Files: `InputStateWriter.cs`. Est: 8.
- [x] **1.4 `UiContext.cs`: `_input` field, constructor `Create()` calls, three hooks in `DrainEvents`, `Input` + `GameInput` properties, doc remarks.** Files: `UiContext.cs`. Est: 8.
- [x] **1.5 `InputStateTests.cs` (new): the 8 phase-1 tests.** Files: `InputStateTests.cs`. Est: 8.
- [x] **1.6 Build + tests + `InputStateTests` (total 8, failed 0); plants 1-3 with restore + `cmp`; record.** Files: (verify), `context.md`. Est: 8.
- [x] **1.7 `HelloScreen.cs`: second `out` label from `BuildWidgetPanel`, `InputText(in UiInputState, Span<byte>)`.** Files: `HelloScreen.cs`. Est: 6.
- [x] **1.8 `HelloModes.cs`: `_inputLabel` + `InputLabel`.** Files: `HelloModes.cs`. Est: 4.
- [x] **1.9 `Program.cs`: `inputScratch`, pass `modes.InputLabel`, `SetText` line after the gate label.** Files: `Program.cs`. Est: 5.
- [x] **1.10 Build + tests + `scripts/test-linux.sh` PASS + demo check (or open manual record); record.** Files: (verify), `context.md`. Est: 6.

**Acceptance:** 8 tests pass; existing tests and gates pass with no edit; plants 1-3 exit non-zero and restored files equal their copies; `test-linux.sh` prints PASS; `UiContext.cs` diff adds lines only at the three hook places. Status: met except the `test-linux.sh` PASS line, which the agent sandbox blocks at the smoke step (see `context.md`, Needs Human Verification).

---

## Phase 2: The Buttons, the Press Ownership, and the Scroll (4/4 complete)

**Demo:** a developer runs `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*InputStateTests'` after a build and reads a total of 18 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet) — one group (2.1-2.4).

- [x] **2.1 `UiInputState.cs`: `UiPointerButtonState` (masks, queries, `Press`/`Release`/`BeginFrame`/`Mask`), `ScrollDelta` + `Scroll`, `Buttons` member.** Files: `UiInputState.cs`. Est: 7.
- [x] **2.2 `InputStateWriter.cs`: button cases + scroll sum in `Record`, `_uiPresses`/`_uiReleases`, `BeginFrame` clear, `EndFrame` button gate + ownership.** Files: `InputStateWriter.cs`. Est: 7.
- [x] **2.3 Add the 10 phase-2 tests to `InputStateTests.cs`.** Files: `InputStateTests.cs`. Est: 8.
- [x] **2.4 Build + tests + `InputStateTests` (total 18); plants 4-6 with restore; record demo.** Files: (verify), `context.md`. Est: 8.

**Acceptance:** 18 tests pass; existing tests pass; plants 4-6 exit non-zero and the restored file equals its copy; `UiContext.cs` does not change. Status: met.

---

## Phase 3: The Held Keys and the Clear Rules (8/8 complete)

**Demo:** the user holds W over the game area of HelloPanel, and the label shows `Held key: W`. The user clicks the text field and holds W, and the label shows `Held key: none`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet) — two groups: 3.1-3.5 (Core path), 3.6-3.8 (adapter + sample).

- [x] **3.1 `UiInputState.cs`: `KeyBits`, `UiKeyState` (`KeyCount`, `Modifiers`, queries, internal writers), `Keys` member.** Files: `UiInputState.cs`. Est: 8.
- [x] **3.2 `InputStateWriter.cs`: key cases of `Record`, key clear in `BeginFrame`, `RequestKeyClear`, key clear + key gate in `EndFrame`.** Files: `InputStateWriter.cs`. Est: 6.
- [x] **3.3 `UiContext.cs`: `ClearHeldKeys()` with the full remarks of Decision 10.** Files: `UiContext.cs`. Est: 5.
- [x] **3.4 Add the 8 phase-3 tests to `InputStateTests.cs`.** Files: `InputStateTests.cs`, `PointerTree.cs`. Est: 8.
- [x] **3.5 Build + tests + `InputStateTests` (total 26); plants 7-9 with restore; record.** Files: (verify), `context.md`. Est: 8.
- [x] **3.6 `GlfwUiInput.cs`: `_onFocus` handler, subscribe/unsubscribe, `PushFocus`, `KeyboardEnabled` backing field + switch-only clear; record the callback signature.** Files: `GlfwUiInput.cs`. Est: 8.
- [x] **3.7 `GlfwUiInputTests.cs` (`EventNames` line only) + `GlfwUiInputFocusTests.cs` (new, 5 tests).** Files: `GlfwUiInputTests.cs`, `GlfwUiInputFocusTests.cs`. Est: 7.
- [x] **3.8 `HelloScreen.cs`/`Program.cs`: held key in `InputText`, longest text; build + tests + focus tests (5) + `test-linux.sh`; plant 10; demo check; record.** Files: `HelloScreen.cs`, `Program.cs`, `context.md`. Est: 8.

**Acceptance:** 26 + 5 tests pass; `GlfwUiInputTests` keeps its count; plants 7-10 exit non-zero; `GlfwUiInputTests.cs` diff is one added line; `test-linux.sh` prints PASS. Status: met except the `test-linux.sh` PASS line: the smoke step stops on the known X11 sandbox blocker (build 0 Error, test 2855 with 0 failed, and gpu pass in the same run; see `context.md`, Phase 3 Group 2 Results).

---

## Phase 4: The Gamepad Slots and the Write Seam (4/4 complete)

**Demo:** a developer runs the built Core test binary with `--filter-class '*InputStateTests'` and reads a total of 32 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet) — one group (4.1-4.4).

- [x] **4.1 `UiGamepadState.cs` (new): `GamepadAxes`, `UiGamepadState`, constructor, queries; `UiInputState.cs`: `GamepadSlots`, `GamepadCount`, `Gamepad(int)`.** Files: `UiGamepadState.cs`, `UiInputState.cs`. Est: 8.
- [x] **4.2 `UiContext.cs` + `InputStateWriter.cs`: `SetGamepad` with the edge rule of the Internal Shape, writer method, remarks.** Files: `UiContext.cs`, `InputStateWriter.cs`. Est: 6.
- [x] **4.3 Add the 6 phase-4 tests to `InputStateTests.cs`.** Files: `InputStateTests.cs`. Est: 7.
- [x] **4.4 Build + tests + `InputStateTests` (total 32); plants 11-12 with restore; record struct size and demo.** Files: (verify), `context.md`. Est: 8.

**Acceptance:** 32 tests pass; existing tests pass; plants 11-12 exit non-zero; `context.md` records the size, at most 1024 bytes.

---

## Phase 5: The 0-Byte Gate and the Docs (6/6 complete)

**Demo:** a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built Core test binary reads a total of 3 for `--filter-class '*InputStateAllocationTests'` with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet) — two groups: 5.1-5.2 (gate), 5.3-5.6 (docs).

- [x] **5.1 `InputStateAllocationTests.cs` (new): the 3 gate tests in the shape of `PointerAllocationTests`.** Files: `InputStateAllocationTests.cs`. Est: 8.
- [x] **5.2 Build + tests + gate (total 3); plants 13-16 with restore; record failed tests and bytes.** Files: (verify), `context.md`. Est: 10.
- [x] **5.3 `docs/technical-design.md` section 9: item 11 "The input state" + the focus-clear sentences in items 1 and 7.** Files: `technical-design.md`. Est: 6.
- [x] **5.4 `docs/technical-design.md` section 14: the `**Input state tests.**` bullet + the nineteenth gate bullet.** Files: `technical-design.md`. Est: 5.
- [x] **5.5 `CHANGELOG.md`: the Added and Changed lines of task 5.5.** Files: `CHANGELOG.md`. Est: 4.
- [x] **5.6 `test-linux.sh` PASS + all phase demo totals (32/5/3) + ASD-STE100 lint on the changed docs and this plan; record the spelling note of gap 1.** Files: (verify), `context.md`. Est: 8.

**Acceptance:** 3 gate tests pass; plants 13-16 exit non-zero; `test-linux.sh` prints PASS; section 9 holds item 11 and section 14 holds the two bullets.

---

## Bugs & Issues

**Active Bugs:**
(none)

**Fixed Bugs:**
(none)

---

## Technical Debt

(none)

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they are discovered.*
