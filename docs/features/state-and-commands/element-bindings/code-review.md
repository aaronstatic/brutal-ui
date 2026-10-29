# state-and-commands/element-bindings - Code Review

**Reviewed:** 2026-10-29
**Reviewer:** Agent (cross-phase review of phases 1 to 4)
**Rules Applied:** 1. The frame order is exact, 2. One apply for each version change, 3. The subtree flag is never stale `false`, 4. No loop, 5. 0 bytes after warm-up, and a strong gate, 6. The Core and Framework split, 7. API clarity and correct remarks, 8. Consistent shape, 9. The seam for the later features, 10. Test quality, 11. The docs, 12. The project rules, 13. No change outside the scope. The Vercel React rules do not apply to C#.
**Status:** Resolved - 5 findings, 5 resolved (review groups 1 to 5)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 1 | CR-1 |
| LOW | 4 | CR-2, CR-3, CR-4, CR-5 |

The feature does what the plan asks. The pass, the list, and the bind methods follow the Internal Shape. The probes found no path that leaves a bound element unreachable. Each legal attach path reaches the pass. The gate measures 0 bytes, and the per-frame code of `Binding/` has no box in the Debug JIT.

The findings are in the tests and in the remarks. The real-work test of the gate cannot fail when the body of the lives gate is empty (CR-1). The char round trip of the text field cannot see a caret reset (CR-2). Three rules of the number handler have no test (CR-3). Two Core remarks state more than the code does (CR-4). The growth remark of the scratch is wrong for a jump of two sizes or more (CR-5).

Rules 1, 2, 3, 4, 6, 8, 9, 11, 12, and 13 have no finding. Rule 5 has CR-1. Rule 7 has CR-4 and CR-5. Rule 10 has CR-1, CR-2, and CR-3.

### Baseline

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2624, failed 0, succeeded 2622, skipped 2. The two skipped tests are the GLFW tests that need a display. The base total was 2580, and the five new classes add 44 tests.
- `BrutalUi.Core.Tests` (`dotnet run`): total 1733, failed 0. The base count was 1715, and the two new classes add 18.
- `BrutalUi.Framework.Tests` (`dotnet run`): total 49, failed 0. The base count was 23, and the three new classes add 26.
- The class counts: `BindingPassTests` 10, `BindingLifecycleTests` 8, `LabelBindingTests` 12, `WidgetBindingTests` 10, and `BoundTreeAllocationTests` 4. Each count matches the plan.
- Rule 13: `git diff 27f33c7 --stat` on the scope paths prints no line. `git diff 27f33c7 --name-only -- '*.csproj'` names only `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`.
- Rules 6 and 12: `grep` for `BrutalUi.Framework`, `Store<`, `StoreSlice`, `LabelBindings`, and `WidgetBindings` in `src/BrutalUi.Core/` finds no line. `grep` for `ToString(`, `event`, and `lock` in `src/BrutalUi.Framework/Binding/` finds no line. Each new file is ASCII and has a file-scoped namespace.

### Probe method

Each probe ran in the Debug build. The review copied each changed file to the scratchpad first. After the run, it copied the file back and checked it with `cmp`. Each `cmp` found no difference. The review deleted the probe test file after the run.

### Probe list

- **Probe test file in `BrutalUi.Core.Tests` (6 tests, deleted after the run).**
  - P1: an `Insert` of a bound subtree into a root with the flag `false` applies at the next `Update`. Pass.
  - P2: an `Add` that moves a bound child from one parent to a child of another parent, with no `Remove` first, applies the new version. The old parent loses its flag at the next pass. Pass.
  - P3: a stale-flag clear, then a root add of the detached subtree in a second context, then an `Insert` back under the first context. Each step applies the new version. Pass.
  - P4: a handler that adds two bindings to its own element (a grow from 2 to 4 slots) during the pass. The pass applies each new slot in the same `Update`, writes version 1 into each slot, and skips no slot. Pass.
  - P5: a handler that adds a binding to an element of a branch that the pass visited already. The parent chain of the new binding gets the flag, and the next `Update` applies it. Pass.
  - P6: a handler that adds a bound element to an ancestor and clears its own bindings (two acts that the `BindingHandler` remarks forbid). The ancestor gets a stale `false`, and the new element never applies. Fail, as expected. Follow-up question 2 holds this result.
- **Plant A in `BoundTreeAllocationTests.cs`: an empty body of `LivesFrame`.** `BrutalUi.Framework.Tests` gives total 49, failed 0. Thus `TheMeasuredBodyDoesRealWork` passes with an empty lives body (CR-1).
- **Plant B in `WidgetBindings.cs`: `SetText((string?)null)` before the real `SetText` in `TextHandler<T>.Run`.** Each apply then moves the caret to the end. `BrutalUi.Framework.Tests` gives total 49, failed 0 (CR-2). The review ran plants A and B in the same run.

---

## Findings

### CR-1: The real-work test cannot fail when the lives body is empty

**Severity:** MEDIUM
**Rule:** 5 (0 bytes after warm-up, and a strong gate) and 10 (Test quality)
**File:** `tests/BrutalUi.Framework.Tests/Binding/BoundTreeAllocationTests.cs`, lines 87 to 95 (`LivesFrame`) and lines 222 to 227 (step 2 of `TheMeasuredBodyDoesRealWork`)

**Problem:** Step 2 of the real-work test calculates the expected value from `scene.Round`, and `LivesFrame` changes `scene.Round`:

```csharp
LivesFrame(scene);
Assert.Equal(10 + (scene.Round % 10), bound.Store.Value.Lives);
Assert.True(bound.Labels[1].Text.SequenceEqual(Encoding.UTF8.GetBytes(bound.Store.Value.Lives.ToString())));
```

After step 1, `scene.Round` is 1, and the store holds `Lives = 11`. An empty `LivesFrame` keeps `scene.Round` at 1, so the expected value is 11 again. The label shows `11` from step 1. Each assert of step 2 thus passes. Plant A proved it: the suite gives 49 passed with an empty `LivesFrame`. The gate `AChangeOfOneSliceAllocatesNothing` then measures no work, and no test fails. Rule 5 asks that the real-work test can fail when a body is empty.

**Fix:** Read the round and the version before the call, and assert fixed values after it:

```csharp
int round = scene.Round;
ulong version = bound.Store.Version;
LivesFrame(scene);
Assert.Equal(round + 1, scene.Round);
Assert.Equal(version + 1UL, bound.Store.Version);
Assert.Equal(10 + ((round + 1) % 10), bound.Store.Value.Lives);
Assert.True(bound.Labels[1].Text.SequenceEqual("12"u8));
```

Run plant A again and expect a failure of `TheMeasuredBodyDoesRealWork`.

**Effort:** Low

### CR-2: The char round trip cannot see a caret reset

**Severity:** LOW
**Rule:** 10 (Test quality)
**File:** `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs`, lines 218 to 256 (`AUserCharInTheFieldWritesTheStoreOneTimeWithNoLoop`)

**Problem:** The Risks table of the plan names this test as the guard for the caret: "A write-back of the same text changes no byte, so the caret stays". The test types at the end of the text only. A caret reset puts the caret at the end, and the caret is at the end already. Thus the test cannot tell a correct handler from a handler that resets the caret. Plant B proved it: a handler that calls `SetText((string?)null)` before the real `SetText` passes each test of the suite. With such a handler, a player who types in the middle of a bound field sees the caret jump to the end after each char.

**Fix:** Add a step with the caret in the middle. After the text `ab`, push the Left key (code 263), then the char `x`, then run two `Update` calls. Assert the text `axb`, the store text `axb`, and one more delegate call. Then push the char `y` and assert the text `axyb`. A caret reset gives `axby`. Keep the test count at 10, or add one test and update the counts in the plan and in section 14.

**Effort:** Low

### CR-3: Three rules of the number handler have no test

**Severity:** LOW
**Rule:** 10 (Test quality)
**File:** `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs` (the whole class). The rules live in `src/BrutalUi.Framework/Binding/LabelBindings.cs`, lines 184 to 203.

**Problem:** Decision 5 and the remarks of `BindText<T, TValue>` state three rules that no test pins:

1. A text over 4096 bytes throws `InvalidOperationException` in `Update`.
2. A `null` slice of a reference type gives an empty text. This is the branch of fix 1 (`s_isReferenceType && value is null`). A plant that removes the branch gives a `NullReferenceException` for a reference slice, and no test fails.
3. A `null` provider gives the current culture (Notes question 4). Each provider test passes a provider. A plant that gives `CultureInfo.InvariantCulture` for `null` passes each test.

**Fix:** Add three tests. (1) An `int` slice with the format `D5000`, and `Assert.Throws<InvalidOperationException>(() => context.Update(0f))`. (2) A test class `Score : IUtf8SpanFormattable` and a selector that gives `null`, then an empty text after `Update`. (3) A `float` slice with the format `F2` and no provider, inside a scope that sets `CultureInfo.CurrentCulture` to a clone of the invariant culture with the decimal separator `,`, and restores it in `finally`. Expect `3,14`. Update the count of 12 in the plan and in section 14.

**Effort:** Low

### CR-4: Two Core remarks state more than the code does

**Severity:** LOW
**Rule:** 7 (API clarity and correct remarks)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs`, lines 486 and 487 (the remarks of `UpdateBindings`), and `src/BrutalUi.Core/Binding/ElementBinding.cs`, lines 9 and 10 (the remarks of `BindingHandler`). One finding names both files, because both remarks describe the same pass.

**Problem:**

1. The `UpdateBindings` remarks state: "Tests call this method to read the values of the handlers before the style pass reads them." No test calls `UpdateBindings`. A `grep` for `UpdateBindings` in `tests/` and `src/` finds the definition and the call in `Update` only. The sentence copies the remark of `UpdateStyles`, which tests do call.
2. The `BindingHandler` remarks state: "The first update after the add always calls the handler." The pass walks from the roots only (the leave rule). An element outside the roots of the context gets no call at the first update. The word "always" thus contradicts the remarks of `ClearBindings`.

**Fix:** (1) Remove the sentence from the `UpdateBindings` remarks, or replace it with "Only `Update` calls this method." (2) Write "The first update after the add calls the handler when the element is in the tree of a root of the context."

**Effort:** Low

### CR-5: The growth remark of the scratch is wrong for a jump of two sizes or more

**Severity:** LOW
**Rule:** 7 (API clarity and correct remarks)
**File:** `src/BrutalUi.Framework/Binding/LabelBindings.cs`, lines 27 and 28 (the remarks of `BindText<T, TValue>`)

**Problem:** The remarks state: "It grows by doubling and never shrinks, so a text that is longer than each earlier text allocates one time." The loop at lines 194 to 203 allocates one new array for each `false` of `TryFormat`. A text of 100 bytes after a text of 10 bytes allocates an array of 64 bytes and an array of 128 bytes in one apply. The format `D100` shows it. The rationale of Decision 5 states the same wrong rule ("the scratch grows one time only"). Section 10 of the technical design states the rule correctly: "It doubles on each `false` of `TryFormat` and never shrinks."

**Fix:** Write "It doubles on each `false` of `TryFormat` and never shrinks. A text that is longer than each earlier text allocates one array for each doubling, in that update only." Correct the rationale of Decision 5 in `implementation.md` in the same change.

**Effort:** Low

---

## What's Already Good

- **The frame order (rule 1).** `Update` runs the guard, `DrainEvents`, `UpdateBindings`, `UpdateStyles`, the transitions, the layout, the caret pass, and the clear pass, in this order. `AVersionFromAnInputHandlerAppliesInTheSameUpdate` fails when the pass moves before the input pass. `TheBindingPassRunsBeforeTheStylePass` fails when the pass moves after the style pass, because the clear pass then drops the `Style` bit of the inline write.
- **One apply for each version (rule 2).** The pass reads the count and the slot again by index after each call, and it writes `AppliedVersion` before the call. Probe P4 grows the list of the own element during the pass, and the pass skips no slot and reads no old array.
- **The subtree flag (rule 3).** `ElementList.Add` and `ElementList.Insert` are the only callers of `AttachToParent`, and each attach sends the flag up the chain. The invariant holds after each pass, because the pass clears a flag only after it read the flag of each child. Probes P1 to P5 cover `Insert`, a move with no `Remove`, a root add, a move between contexts, and an add during the pass. Each one applies.
- **No loop (rule 4).** Each handler writes through a code setter. The toggle, the slider, and the text field compare the value first, and no code setter calls a delegate. The pass marks no dirty bit. `TextField.SetText(string?)` compares only the prefix that fits the capacity, so a long store text does not reset the field at each version.
- **The gate (rule 5).** Each measured body holds no lambda, no string build, no boxed value, and no `Assert`. `AllocationGate.ReleaseAllocationContext()` runs right before each measure. The width rule of `Next` keeps each buffer at its warm-up size. The constrained `TryFormat` call boxes nothing, and the `static readonly` flag keeps the `box` of `value is null` off the struct path. The plant numbers of section 14 match the scene: 100 frames, 43 bindings, and 24 bytes give 103200 bytes.
- **The split (rules 6 and 13).** Core names no Framework type, also in a `cref`. The Framework test project links four files by name, holds no reference to `BrutalUi.Core.Tests`, and keeps its own `AllocationGate`.
- **The shape (rule 8).** The nested `AttachTo` of `NumberText` and the generic holder classes both give one cached delegate for each closed type, so neither shape allocates a delegate for each bind. Each bind method checks each argument with `ArgumentNullException.ThrowIfNull` and returns its element.
- **The seam (rule 9).** commands-and-events binds `Button.Clicked` and needs nothing from this feature. action-map can bind its label through `LabelBindings.BindText`. A custom format with a quoted literal, for example `"'Score: '0"`, gives a prefix with no new API. panels-and-modes can write a custom handler through the public `Element.AddBinding`.
- **The docs (rule 11).** Sections 4, 10, and 14 and the changelog line match the code: the order, the skip flag, the error rule, the guard, the span formatter, the 4096-byte limit, the leave rule, and the counts.

---

## Follow-up Questions

1. **A widget binding writes at each new version of the store, also for a change of another slice.** A user change that the game does not write back is lost at the next change of any slice. The plan states that the game writes the store in the delegate, but the remarks of `WidgetBindings` do not state the loss.
   - (a) Keep the behavior, and add one sentence about the loss to the class remarks of `WidgetBindings`.
   - (b) Keep the last applied slice in the target, and skip the setter when the new slice is equal. This needs an equality check with no box for `bool`, `float`, and `string`.
   - (c) Give each slice its own version.
   - **Default:** (a) in this feature. A panel feature that needs a one-way field with no write-back can choose (b).
   - Answer: (a), applied in review-g2 (the called mode of /autorun-epic takes the default).
2. **The pass has no guard for the acts that the `BindingHandler` remarks forbid.** Probe P6 shows that a handler that adds a bound element to an ancestor and clears its own bindings leaves a stale `false`. The new element then never applies, also after a later legal `AddBinding` on it. No legal path gives this state.
   - (a) Keep the rule in the remarks only.
   - (b) Add a `[ThreadStatic]` flag of the pass and a `Debug.Assert` in `AddBinding`, `ClearBindings`, and `AttachToParent`.
   - (c) Throw `InvalidOperationException` from these methods during the pass.
   - **Default:** (a). Choose again when panels-and-modes adds a list binding that builds rows in a handler.
   - Answer: (a). The called mode of /autorun-epic takes the default. No code change.
3. **xml-markup and the bind methods.** `BrutalUi.Markup` references Core only, and the bind methods live in Framework. Requirement 9 states that xml-markup calls the same bind methods.
   - (a) `BrutalUi.Markup` references `BrutalUi.Framework`.
   - (b) Markup calls `Element.AddBinding` with its own static handlers.
   - (c) A small bridge package references both.
   - **Default:** the xml-markup plan decides. The public API of this feature serves (a), (b), and (c) with no change.
   - Answer: the default. The xml-markup plan decides. No code change.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-1 | MEDIUM | Low | The gate can lose one of its three bodies with no failure. |
| 2 | CR-2 | LOW | Low | The plan names this test as the caret guard, and the test cannot fail. |
| 3 | CR-3 | LOW | Low | The branch of fix 1 has no test. |
| 4 | CR-4 | LOW | Low | Two remark lines, no code change. |
| 5 | CR-5 | LOW | Low | One remark and one line of the plan, no code change. |

---

## Progress

- [x] CR-1: The real-work test cannot fail when the lives body is empty - Fixed 2026-10-29 (review-g1)
- [x] CR-2: The char round trip cannot see a caret reset - Fixed 2026-10-29 (review-g2)
- [x] CR-3: Three rules of the number handler have no test - Fixed 2026-10-29 (review-g3, new steps in three existing tests, count stays 12)
- [x] CR-4: Two Core remarks state more than the code does - Fixed 2026-10-29 (review-g4)
- [x] CR-5: The growth remark of the scratch is wrong for a jump of two sizes or more - Fixed 2026-10-29 (review-g5, the code remark; the plan line of Decision 5 stays, see context.md)
