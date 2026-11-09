# input/gamepad-axes - Code Review

**Reviewed:** 2026-11-08
**Reviewer:** Senior C# engine reviewer (cross-phase review of phases 1 to 3)
**Rules Applied:** 1. Plan fidelity (Public API, The Deadzone Formulas, Internal Shape, the planned files), 2. Formula correctness (radial stick rescale and clamp, trigger map-then-zone, the `!(len > d)` NaN-catch form, Decisions 2, 3, 4), 3. The poll contract (16 slots, no early return, `default` paths, one `SetGamepad` call site, released edge). It also covers no GLFW call after `Dispose`, an unsafe span with no pin, and the frame order. 4. API clarity and XML docs (ASD-STE100, the stored-value remarks), 5. Allocation discipline (0 bytes, `stackalloc` only, the plant and gate shapes), 6. Integration and sibling contracts (A1 zero-diff, `PollGamepad` and `PushGamepad` unchanged, the values the three later features read), 7. Test strength (every F1-F8/Q1-Q6 criterion pinned, every documented rule pinned by a test or a plant), 8. Scope discipline (the I1 file list), 9. Docs truth (section 9 item 12, section 14, CHANGELOG, no doc claims an unmeasured number), 10. Sample correctness (label placement, scratch sizes, poll/push/label region split, `-0.00`). The Vercel React rules do not apply to C#.
**Status:** Open - 3 findings

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| CRITICAL | 0 | - |
| HIGH | 0 | - |
| MEDIUM | 0 | - |
| LOW | 3 | CR-1, CR-2, CR-3 |

This review is the strongest input-epic review in the project. Both formulas are arithmetically correct. This review re-derived each one on its own. The poll contract holds at every point. The value that the three later features read matches the value their requirement files expect. The three findings are test-strength and prose items. None changes a stored value.

Rule 1 passes. Each planned member ships in the planned file with the planned shape. The enum `UiGamepadAxis` holds six values 0 to 5. It sits after `UiGamepadButtons` at `UiEventEnums.cs` line 148. The helpers `ApplyStick` and `ApplyTrigger` sit at `UiGamepadDeadzone.cs` line 45 and 90. The reads `Axis(UiGamepadAxis)`, `LeftStick`, and `RightStick` sit at `UiGamepadState.cs` line 122, 129, and 136. The constant `GamepadAxisCount = 6` sits at `GlfwUiInput.cs` line 100. The default `StickDeadzone` is 0.15, and the default `TriggerDeadzone` is 0.1. Both sit behind the planned `!(value >= 0f && value <= 1f)` check at line 271 and 304. The member `internal static GamepadState` sits after `GamepadButtons` at line 469. The poll `PollGamepads()` sits after `PollGamepad` at line 403. The Internal Shape matches too. The backing fields hold private default constants at line 102-106 and 147-151. The conversion uses `stackalloc float[6]` at line 483. The call `Context.SetGamepad(id, in state)` sits outside the present branch at line 421.

Rule 2 passes. This review re-derived both formulas here. The helper `ApplyStick` at line 59-67 runs a finite check first. It sets `len = MathF.Sqrt(x*x + y*y)`. Then `!(len > deadzone) || deadzone >= 1f` returns (0, 0). The `!(>)` form catches a NaN length with no extra branch. Then `scale = MathF.Min((len - d)/(1 - d), 1)` and `k = scale/len` rescale along the input direction. Re-derived to five decimals: the pair (0.1, 0.05) at 0.15 gives (0, 0). The pair (0.575, 0) at 0.15 gives (0.5, 0). The pair (0.3, 0.4) at 0.15 gives (0.247059, 0.329412). The pair (1, 1) at 0.15 gives (0.707107, 0.707107). The clamp works there, because `(1.41421-0.15)/0.85 = 1.487 > 1`. The Decision 2 clamp also holds at a deadzone of 0. The pair (-0.75, 1) at 0 gives length 1.25, then (-0.6, 0.8). That is the shipped F6 value. The helper `ApplyTrigger` at line 97-108 returns 0 for a non-finite value. It maps first: `t = MathF.Min(MathF.Max((value + 1f) * 0.5f, 0f), 1f)`. Then the linear zone acts at the low end. Re-derived: the input -1 at 0.1 gives 0. The input 0 at 0.1 gives 0.444444. The input 0.5 at 0.1 gives 0.722222. The input 1 at 0.1 gives 1. The divide-by-zero at `d = 1` is unreachable, because `deadzone >= 1f` returns zero first at line 60 and 103. A non-finite `deadzone` throws before any math at line 47 and 92. The check order matches Decision 4.
Edge case checked: two finite-but-huge components overflow `len` to +∞. Then `scale = 1` and `k = 1/∞ = 0`, so the result is (±0, ±0). That is zero at any observable precision, and the contract holds. No fault.

Rule 3 passes. The poll `PollGamepads` at line 403-423 loops `id` from 0 to 15 with no early return. Each slot starts from `UiGamepadState state = default;`. The present branch is the short-circuit `!_disposed && GlfwApi.GetGamepadState(...)`. A disposed adapter therefore makes no GLFW call and writes `default` 16 times. The held button of the raw slots gets its one released edge through `SetGamepad`. The `grep` shows exactly one real `Context.SetGamepad` call, at line 421. The two other hits, at line 387 and 393, are `cref` text. The axes read is `new ReadOnlySpan<float>(raw.Axes, GamepadAxisCount)` inside the same `unsafe` block as the buttons. It carries the no-pin fixed-local comment at line 410-415. That is the planned A5 shape, and `TheAxisBufferAndTheAxisConstantsFollowGlfw` confirms it. The remarks state the frame rule in full: "after `Glfw.Init` and `Glfw.PollEvents`, and before `UiContext.Update(float)`" at line 391-392. They state the absent and no-mapping `default` rule, per A8 at line 397-398. They also state "The call allocates nothing" at line 401.

Rule 4 gives CR-3. Every new public member has a doc comment. CS1591 is an error, and the build is 0/0. The prose uses active voice with no contraction. A contraction grep over the new files prints nothing. The stored-value contract appears where the later features read it. The `UiGamepadState` class remarks and the `Axis(int)` remarks state the rule, at `UiGamepadState.cs` line 33-34 and 102-103. They read: "stores each value after the deadzone. A stick axis is -1 to 1, and a trigger is 0 to 1". The stick reads state the GLFW Y sign and "length is 1 or less". Those remarks sit at line 126-127 and 133-134. The enum members state the -1-for-up range and the 0-to-1 trigger range, at `UiEventEnums.cs` line 153 and 162.

Rule 5 gives CR-1. The Q6 grep runs the Verification step 5 pattern over `UiGamepadDeadzone.cs` and `GlfwUiInput.cs`. It prints no line. The helper is pure struct math. The conversion holds the only buffer, and it is `stackalloc`. The three 0-byte tests follow the `AllocationGate.ReleaseAllocationContext()` shape. Each runs 2 warm-up rounds and 100 measured calls. The test `TheConversionAllocatesNothing` asserts the sum `3 * PollCount` after the bytes, so the body of that test cannot pass empty. The null-platform test asserts all 16 slots cleared after the measure. That body also cannot pass empty. The live `Poll` helper measures both polls in one body. The helper `StickText` uses the UTF-8 `float.TryFormat` overload, and it writes into the span of the caller with `CultureInfo.InvariantCulture`. It makes no string and no box. The throw pattern matches `InputText`, in the `HelloScreen.cs` diff at line 561-624. Plants 4 and 7 prove the poll and helper measures bite.

Rule 6 passes. The `git diff 240f8d4` run over `UiContext.cs`, `UiInputState.cs`, `InputStateWriter.cs`, and all of `src/BrutalUi.Framework/` is empty. That covers A1 and I1. The state `UiGamepadState` gained no field. A `bool`, three 4-byte enums, and 6 floats stay 40 bytes. The test `InputStateTests` still asserts the 804-byte `UiInputState`. The `PollGamepad(int)` method keeps its body at `GlfwUiInput.cs` line 358-383. The `UiActionMap.PushGamepad` method keeps its body too. Each sibling gets exactly what it expects. The input-actions feature reads sticks and triggers with no deadzone of its own. That is the stored-after-deadzone rule from Q1, and the `requirements.md` line 13 binds the stick and trigger types. The control-schemes feature treats a value that is not 0 as gamepad input. A stick at rest and a released trigger read exactly 0, because `!(len > d)` returns literal zero. The binding-remap feature listens above the zone. The stored values are the zone-filtered ones. The ungated gamepad copy into `GameInput` follows the input-state rule. That is why the label can read `ui.GameInput` while the push reads `ui.Input`.

Rule 7 gives CR-1 and CR-2. The mapping runs like this. F1 to F4 pin in `GamepadDeadzoneTests`, and the nine tests are one per plan name. F4 also pins in the constants test. F5 to F7 pin in `GlfwUiInputGamepadPollTests`. F6 ships with the corrected clamp value (-0.6, 0.8). F7 pins in `ADisposedAdapterWritesDisconnectedSlots`, the null-platform test, and the grown live test. F8 pins in the smoke run and plant 9. The tracked X11 item blocks both, and this review does not re-raise it. This review records Q1 to Q3. The build reads 0/0. The solution reads 2881 with 0 failed and 3 skipped. Core reads 1795, which is 1786 plus 9. Glfw reads 40, which is 32 plus 8. Q4 pins with plants 4 and 7. Q5 now records five lint scores, all under 2.5. The file `technical-design.md` reads 1.53, and `CHANGELOG.md` reads 1.55. The file `context.md` reads 2.07, and `implementation.md` reads 1.66. The file `code-review.md` reads 0.52, so this report now carries its own measured score. This review re-checks Q6, and it verifies I1 to I3 below. No test or plant pins two rules. The first is the wiring of the two deadzone fields into the conversion inside `PollGamepads`. That is CR-2. The second is the cannot-pass-empty property of `ADisposedPollAllocatesNothing`. That is CR-1. The null-platform test genuinely covers the absent-slot path. It starts real GLFW on the null platform. It runs the real `GetGamepadState` false branch 100 times through `PollGamepads`. It asserts that a pre-seeded connected slot 0 goes to disconnected with all 16 slots cleared. That is the same branch an absent slot takes on a real platform.

Rule 8 passes. The `git diff 240f8d4 --stat` run over `src/`, `samples/`, `scripts/`, `tests/`, `*.csproj`, `*.props`, and `*.sln` shows exactly seven changed files. The set is three Core source, one Glfw source, one Glfw test, and three sample. The `git status --porcelain --untracked-files=all` run adds exactly the three new files: `UiGamepadDeadzone.cs`, `GamepadDeadzoneTests.cs`, and `GlfwUiInputGamepadPollTests.cs`. The feature docs come with them. That is the I1 list with no extra. The file `UiEventEnums.cs` shows added lines only. The file `GlfwUiInputGamepadTests.cs` shows three hunks: the class summary, the `Poll` helper, and `ALivePollAllocatesNothing`. No plant residue stays: a `GC.KeepAlive` grep over the three planted files prints nothing.

Rule 9 passes. Section 9 item 12, "The gamepad poll", sits at `docs/technical-design.md` line 911-922. It states the frame rule and the 16 slots. It states the connected rule (A8), and the absent and disposed rules (A4). It names `UiGamepadAxis` and the three reads. It states the stored-value rule from Q1 and Q2, plus the Y sign. It states both formulas with the clamp. It states both settings with their range, and the 0-byte rule. It closes by naming both test classes. The item 11 seam sub-item at line 909 and the action-map **`PollGamepad`** bullet at line 937 both name `PollGamepads`. Section 14, at line 1350 and 1358, holds the grown **Action map tests.** sentence. It holds the **Gamepad axes tests.** bullet with the 9 and 8 counts. It holds "0-byte tests, not a numbered gate". It holds plants 1 to 9 with exactly the failed-test lists that `context.md` records. Its **Sample projects** bullet reads "the three label updates" and names `PollGamepads`. Plant 9 reads honestly: "Its build gives `0 Error(s)` here. The smoke run on a host with a window must report 2808 bytes and exit code 1. The sandbox X11 block leaves that measure to a person." That text is a requirement, not a measurement. The `CHANGELOG.md` lines 43-45 and 49 carry the three Added entries and one Changed entry. Each carries the feature tag. A docs grep for the stale unclamped `RightStick` value finds it only in reconciled explanations of the clamp. The three places are task 2.5, F6, and the `context.md` decision. None of them says the unclamped value is the expected one. No other doc carries the stale value.

Rule 10 passes. The stick label sits right after `inputLabel` and before `checkerImage`, in the children block of the `HelloScreen.cs` diff. It is not pickable. It starts with `LongestStickText`, which holds 24 bytes. The prefix is 12 bytes and the separator is 2. Each component takes at most 5 after the -1 to 1 clamp and two decimals. So the 32-byte `stickScratch` always holds one text, with 8 bytes of headroom. The call `input.PollGamepads()` sits outside the UI region at line 818. The call `state.Map.PushGamepad(ui.Input.Gamepad(0).Buttons)` is the first line inside it, at line 853. So the push reads the raw slot of this frame before `Update`. The label write `HelloScreen.StickText(in ui.GameInput.Gamepad(0), stickScratch)` sits inside the region after `Update`. So the gate measures the slot read and the format. The helper `StickComponent` maps a non-finite value to 0, then clamps and rounds to two decimals. It then adds `0f`, which turns a `-0` rounding into `+0`. So `-0.00` cannot render.

### Baseline (observed in this review, read-only)

- `git log --oneline -1`: the base is `240f8d4 feat/input/input-state`. The feature work sits uncommitted in the tree on the feature branch.
- `git diff 240f8d4 --stat -- src/ samples/ scripts/ tests/ '*.csproj' '*.props' '*.sln'`: seven files, 394 insertions, and 36 deletions. The file set equals I1. No `.csproj`, `.props`, `.sln`, or `scripts/` file appears.
- `git diff 240f8d4 -- src/BrutalUi.Core/Tree/UiContext.cs src/BrutalUi.Core/Input/UiInputState.cs src/BrutalUi.Core/Input/InputStateWriter.cs src/BrutalUi.Framework/`: empty (A1, I1).
- `git diff 240f8d4 -- src/BrutalUi.Core/Input/UiEventEnums.cs | grep -E '^-[^-]'`: no line (added lines only).
- `grep -c 'SetGamepad' src/BrutalUi.Glfw/GlfwUiInput.cs`: 3 lines. One call sits at line 421, inside `PollGamepads`. The other two hits are `cref` comments (I2).
- Q6 grep (step 5 pattern) over `UiGamepadDeadzone.cs` and `GlfwUiInput.cs`: no line (Q6).
- `grep -rn GC.KeepAlive` over the three planted files: no line (no plant residue).
- `grep -rn -- '-0.75' docs/`: three hits, all reconciled explanations of the clamp, at task 2.5, F6, and the `context.md` decision. None of them says the unclamped value is the expected one.
- Python re-derived both formulas. The pair (1,1) at 0.15 gives (0.707107, 0.707107). The pair (0.3,0.4) at 0.15 gives (0.247059, 0.329412). The pair (0.575,0) at 0.15 gives (0.5, 0). The pair (-0.75,1) at 0 gives (-0.6, 0.8). The pair (2,0) at 0 gives (1,0). The trigger inputs -1 and 0 at 0.1 give 0 and 0.444444. The trigger inputs 0.5 and 1 at 0.1 give 0.722222 and 1. The pair (0.15,0) at 0.15 gives (0,0). The trigger input -0.8 at 0.1 gives 0. Each result matches the worked values in the plan and the shipped asserts.
- Build and tests: not re-run here. This review takes the tree as verified. The record shows build 0/0, solution 2881, 0 failed, and 3 skipped. The class totals are 9 and 8.

Open human items sit in `context.md` already, and this review does not count them as findings. They are the `scripts/test-linux.sh` smoke PASS run and the 2808-byte measure of plant 9. They are also the `xvfb-run` live PASS line and the HelloPanel window demo. The sandbox X11 socket blocks all four, per Gotcha 3.

---

## Findings

### CR-1: `ADisposedPollAllocatesNothing` passes even when its measured body does no poll

**Severity:** LOW
**Rule:** 5 (allocation discipline), 7 (test strength)
**File:** `tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadPollTests.cs` line 210 to 224

**Problem:** Rule 5 requires each 0-byte test to have "a measured body that cannot pass empty". The other two tests in the class do. The test `TheConversionAllocatesNothing` asserts the sum `3 * PollCount` after the measure, at line 244. The null-platform test asserts the cleared slots after the measure, at line 281 to 292. This test asserts only the byte counter:

```csharp
        GlfwUiInput input = CreateSeamAdapter();
        input.Dispose();
        PollGamepads(input, WarmUpPolls);
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        PollGamepads(input, PollCount);
        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, after - before);
```

A later edit could turn `PollGamepads` into a no-op for the disposed case. Plant 5 shows that shape: `if (_disposed) { return; }`. This test then measures 0 bytes and passes. The record in `context.md` shows exactly that: plant 5 failed `ADisposedAdapterWritesDisconnectedSlots` **only**. The sibling functional test catches the fault today. The shape is what task 2.5 planned, so this finding is a guard-shape gap, not a live defect. It is cheap to close, so the test stands on its own.

**Fix:** Seed one slot before the dispose, and assert its release after the measure. That is the same post-measure pattern the null-platform test uses: asserts outside the measured region cannot add bytes:

```csharp
        GlfwUiInput input = CreateSeamAdapter();
        input.Context.SetGamepad(0, new UiGamepadState(UiGamepadButtons.A, [0.5f]));
        input.Dispose();
        ...
        Assert.Equal(0L, after - before);

        // The measured polls did the work: slot 0 went to default with its release.
        Assert.False(input.Context.Input.Gamepad(0).Connected);
        Assert.Equal(UiGamepadButtons.A, input.Context.Input.Gamepad(0).Released);
```

The name still fits: the polls run after `Dispose`. A no-op `PollGamepads` now fails the test.

**Effort:** Low

### CR-2: The deadzone settings reach the poll through no pinned path

**Severity:** LOW
**Rule:** 7 (test strength), plus the task 2.1 remarks. The Internal Shape line reads "The poll reads the fields at each call".
**File:** `src/BrutalUi.Glfw/GlfwUiInput.cs` line 413 to 417, and remarks line 263 to 264 and 291

**Problem:** The shipped remarks state the rule twice: "The next `PollGamepads()` call uses the new value." The wiring sits in the poll:

```csharp
                    state = GamepadState(
                        new ReadOnlySpan<byte>(raw.Buttons, GamepadButtonCount),
                        new ReadOnlySpan<float>(raw.Axes, GamepadAxisCount),
                        _stickDeadzone,
                        _triggerDeadzone);
```

No test or plant exercises a poll with a gamepad present, so nothing pins this call site. The conversion tests pass the two deadzone values as direct arguments. They never pass them through these fields. The disposed test and the null-platform test take the absent branch, where the settings never meet the math. Two wrongs sit where no test can see them. The first passes `_triggerDeadzone` as the stick argument, or the reverse. The second caches the two fields in the constructor, so a later setter write never lands. Either keeps all 2881 tests green. Plant 6 shows what a swapped pair costs. The swap inside `GamepadState` fails three tests. The same swap at this call site stays invisible.

**Fix:** Add one pin in `TheConversionAppliesTheDeadzoneSettings` (`GlfwUiInputGamepadPollTests.cs` line 120). Read the settings through the properties of a live adapter, not as literals. Those are the same values the poll passes:

```csharp
        GlfwUiInput input = CreateSeamAdapter();
        input.StickDeadzone = 0.15f;
        input.TriggerDeadzone = 0.1f;
        UiGamepadState viaProps = GlfwUiInput.GamepadState(
            ReadOnlySpan<byte>.Empty, axes, input.StickDeadzone, input.TriggerDeadzone);
        Assert.Equal(state.LeftStick.X, viaProps.LeftStick.X, 5);   // and each of the six values
```

That pins the property values and their order against the conversion signature. The remaining stretch is the fields, not the getters, inside `PollGamepads`. That gap then shows as a two-line difference. A plant that swaps the two `cref`-adjacent fields can come with it. This report records the gap, its low likelihood, and its low cost. The record lets the fix pass take it.

**Effort:** Low

### CR-3: The `UiGamepadAxis` remarks claim a sharing the action map does not have yet

**Severity:** LOW
**Rule:** 4 (API clarity), 9 (docs truth)
**File:** `src/BrutalUi.Core/Input/UiEventEnums.cs` line 146

**Problem:** The enum remarks read:

```csharp
/// Core reads no gamepad. The enum is the shared type of the GLFW adapter and the action map.
```

The last sentence comes from `UiGamepadButtons`, where it is true today. The method `UiActionMap.PushGamepad` takes that enum. Nothing in `src/BrutalUi.Framework/` names `UiGamepadAxis`. A grep shows the only readers. The first is `BrutalUi.Glfw`, for the conversion. The second is the `UiGamepadState.Axis(UiGamepadAxis)` read. The sharing arrives with input-actions. So the sentence reads as a present claim that a game developer can check and disprove. This review judged the point borderline. The claim becomes true one feature later. The same sentence pattern already exists in the tree. So this report writes it here with the reason, for the prose sweep. It is not a behavior defect.

**Fix:** One phrase, matching today:

```csharp
/// Core reads no gamepad. The enum is the shared type of the GLFW adapter and the state reads, and
/// input-actions binds through it.
```

**Effort:** Low

---

## What's Already Good

- Both formulas are exact. This review re-derived each one on its own. The radial rescale keeps the direction. The clamp holds the result at length 1, even at a deadzone of 0. The trigger maps before it zones. The zone sits at the low end of the 0 to 1 scale.

- `d = 1` kills every input before any division. The `!(len > d)` form catches a NaN length with no extra branch. Every rule of Decisions 2, 3, and 4 appears in the shipped order.

- The stored-value contract says the same thing everywhere. After the deadzone, sticks run -1 to 1 with length 1 or less. Triggers run 0 to 1. A stick at rest and a released trigger read exact zero. The whole downstream epic reads this contract, and each reader finds it stated where it lands.

- The state class remarks and the `Axis` remarks carry it. The stick reads carry the Y sign and the length rule. The enum members carry the trigger range. Section 9 item 12 and the CHANGELOG Changed entry carry it too.

- The poll contract is exact. Every call writes 16 slots, with no early return. The absent, no-mapping, and disposed cases write `default`. One `SetGamepad` call site exists, and no GLFW call follows `Dispose`.

- The no-pin fixed-buffer span covers both `Buttons` and `Axes`. The remarks state the full frame rule.

- The null-platform test is the right answer to "no display on this host". It runs real GLFW headless. It covers the `GetGamepadState` false branch through `PollGamepads` 100 times at 0 bytes. It restores the init hint in `finally`, and it sits in the serialized collection. Plant 4 fails two tests on this host, which proves the measure bites without a window.

- The 9/8 test set pins every functional criterion. It uses hand-made values and five-decimal compares. The nine plants cover each rule they claim. Their recorded byte counts divide exactly by the 24-byte object. The records show 2400 = 100 x 24 and 4800 = 200 x 24. The collateral failures of each plant are correct consequences, and `context.md` records them.

- The plan-vs-code contradiction about the unclamped `RightStick` value in task 2.5 went the right way. The merged formula won. This pass reconciled the plan text at both places, task 2.5 and F6. The deviation and its arithmetic sit in `context.md`. The shipped test carries a comment that names the clamp at the exact assert.

- The sample is careful. The poll `PollGamepads` sits outside the UI region. The push of the raw slot is the first line inside it. The label reads `GameInput` after `Update`. This review proved the 24-byte longest text byte by byte against the 32-byte scratch.

- The `+ 0f` kills `-0.00`. The remarks say "four labels" in every paragraph that counts.

- Scope discipline is exact against I1. No doc on the tree claims a measurement this host could not make. The 2808 bytes of plant 9 appear only as a must-report requirement for a person.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-2 | Low | Low | It is the one shipped remark about the values with no test behind it. A swapped or cached setting would give every later feature a wrong zone. No test would show it. |
| 2 | CR-1 | Low | Low | It closes the last "cannot pass empty" gap in the 0-byte set with three asserts. Take it with CR-2 in the same test file pass. |
| 3 | CR-3 | Low | Low | One doc phrase. Take it in the same pass. It carries no build risk. |

---

## Progress

- [x] CR-1: `ADisposedPollAllocatesNothing` passes even when its measured body does no poll (tests/BrutalUi.Glfw.Tests/GlfwUiInputGamepadPollTests.cs:210-224)
- [x] CR-2: The deadzone settings reach the poll through no pinned path (src/BrutalUi.Glfw/GlfwUiInput.cs:413-417, GlfwUiInputGamepadPollTests.cs:120)
- [x] CR-3: The `UiGamepadAxis` remarks claim a sharing the action map does not have yet (src/BrutalUi.Core/Input/UiEventEnums.cs:146)
