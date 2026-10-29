# state-and-commands/store-and-subscription - Code Review

**Reviewed:** 2026-10-29
**Reviewer:** Agent (cross-phase review of phases 1 and 2)
**Rules Applied:** 1. The version is exact, 2. A subscription sees each change one time, 3. 0 bytes after warm-up, and a strong gate, 4. No event in the frame loop, and the Core and Framework split, 5. API clarity and correct remarks, 6. The project rules, 7. The seam for element-bindings, 8. Test quality, 9. The shared file, 10. The docs, 11. No change outside the scope. The Vercel React rules do not apply to C#.
**Status:** Resolved - 3 findings, 3 fixed

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 0 | - |
| LOW | 3 | CR-1, CR-2, CR-3 |

The feature does what the plan asks. The store and the subscription follow the Internal Shape line by line. Each mutation of the review fails at least one test, and each allocation plant fails a gate. The three findings are doc comments that do not state what the code does (rule 5). Two doc comments of the gate scene give the wrong round for each string (CR-1). The `<returns>` tag of `TryGetChanged` does not include the first read (CR-2). The `Store` getter does not state that a default subscription gives `null` (CR-3).

Rules 1, 2, 3, 4, 6, 7, 8, 9, 10, and 11 have no finding.

### Baseline

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2580, failed 0, succeeded 2578, skipped 2. The two skipped tests are the GLFW tests that need a display.
- `BrutalUi.Framework.Tests`: total 23, failed 0. `StoreTests` 9, `StoreSubscriptionTests` 10, `StoreAllocationTests` 4.
- `BrutalUi.Core.Tests`: total 1715, failed 0. This is the count of the base commit in `context.md`.
- Rule 9: the `diff` of the two `AllocationGate.cs` files with no `///` line and no `namespace` line prints no line (exit 0).
- Rule 11: `git diff 2ac76be --stat` on the scope paths prints no line. `git status --porcelain -- src/BrutalUi.Core/` names only `src/BrutalUi.Core/Binding/`.
- Rule 4: `grep -rnE '\bevent\b|INotifyPropertyChanged|\block\b|Volatile'` on `State/` and `Binding/` finds no code line.

### Probe method

Before each probe, the review copied the file to the scratchpad folder. After each run, the review restored the file from the copy, and `cmp` found no difference. Each run used `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`.

### Probe list

| Probe | Change | Result |
|-------|--------|--------|
| P1 | `GC.KeepAlive(new object());` in the `Version` getter of `Store<T>`, with P2 | `AVersionReadAllocatesNothing` fails (7200 bytes). `TryGetChangedAllocatesNothing` fails (9600 bytes). |
| P2 | `GC.KeepAlive(new object());` as the first line of `TryGetChanged` | `TryGetChangedAllocatesNothing` fails (4800 bytes). |
| M3 | `Set` always raises the version (`... && _version == 0` on the equality check) | 8 tests fail: 6 of `StoreTests`, `AnEqualSetGivesNoChange`, and `TheMeasuredBodyDoesRealWork`. |
| M4 | `TryGetChanged` ignores `_applied` (`... && version == 0` on the check) | 7 tests fail: 6 of `StoreSubscriptionTests` and `TheMeasuredBodyDoesRealWork`. |
| M5 | The store starts at version 0 | 19 tests fail, also `ANewStoreHoldsTheInitialValueAtVersionOne` and `TheFirstReadAppliesTheCurrentValue`. |
| M6 | `ChangedStep` of the gate returns at its first line | `TheMeasuredBodyDoesRealWork` fails (`Hits` expected 2, actual 0). |

The plan records the plant in `Store<T>.Set` (14400 bytes and 2400 bytes). The review did not run it again.

---

## Findings

### CR-1: The gate scene gives the wrong round for each string
**Severity:** LOW
**Rule:** 5. API clarity and correct remarks
**File:** `tests/BrutalUi.Framework.Tests/State/StoreAllocationTests.cs:48-52`
**Problem:** The doc comment of `TextA` says "The string that the scene sets on odd rounds". The doc comment of `TextB` says "The string that the scene sets on even rounds". The code does the opposite. Line 118 is `string text = (round & 1) == 1 ? scene.TextB : scene.TextA;`, and the comment on line 117 also says "The odd rounds set TextB and the even rounds set TextA". Thus the file contradicts itself. The real-work test asserts `Assert.Same(scene.TextB, scene.TextStore.Value)` after 3 rounds, which agrees with the code. A reader who trusts the doc comments expects `TextA` there.
**Fix:** Swap the two round words:
```csharp
/// <summary>The string that the scene sets on even rounds. It is not equal to <see cref="TextB"/>.</summary>
public required string TextA { get; init; }

/// <summary>The string that the scene sets on odd rounds.</summary>
public required string TextB { get; init; }
```
**Effort:** Low
**Resolved:** 2026-10-29 - The doc comments of `TextA` and `TextB` now give even rounds and odd rounds, as line 118 does.

### CR-2: The `<returns>` tag of `TryGetChanged` does not include the first read
**Severity:** LOW
**Rule:** 5. API clarity and correct remarks
**File:** `src/BrutalUi.Framework/State/StoreSubscription.cs:73`
**Problem:** The tag says `<returns><see langword="true"/> when the store changed since the last applied version.</returns>`. The first read of a new subscription returns `true` also when the store did not change (rule 2 and `TheFirstReadAppliesTheCurrentValue`). Before the first read, there is no applied version. The summary on line 69 states the rule correctly ("when the store version differs from the applied version"). The `<returns>` tag is the text that a tooltip shows for the result, so it must agree with the summary.
**Fix:** State the version rule in the tag:
```csharp
/// <returns><see langword="true"/> when the store version differs from the applied version. The first read of a new
/// subscription always gives <see langword="true"/>.</returns>
```
**Effort:** Low
**Resolved:** 2026-10-29 - The `<returns>` tag now states the version rule and the first read.

### CR-3: The `Store` getter does not state that a default subscription gives `null`
**Severity:** LOW
**Rule:** 5. API clarity and correct remarks (also 7. The seam for element-bindings)
**File:** `src/BrutalUi.Framework/State/StoreSubscription.cs:58-61`
**Problem:** The getter is `public readonly Store<T> Store => _store;`, and its summary is "Gets the store that the subscription reads." The type has no `?`, so the nullable flow analysis tells a caller that the result is never `null`. A `default` subscription holds no store, so the getter gives `null`. Decision 7 states that a pooled array holds `default` slots past its count. `Store` is one of the three seam members that the plan gives to element-bindings. Thus a binding pass that reads `Store` from a wrong slot gets a `null` with no compiler warning. Then it gets a `NullReferenceException` later, in place of the clear exception of Decision 7. `TryGetChanged` throws on a default value, but the getter does not, and its doc comment does not say so.
**Fix:** Keep the type of the Public API, and add the default rule to the doc comment:
```csharp
/// <summary>
/// Gets the store that the subscription reads. A <see langword="default"/> value gives <see langword="null"/>.
/// </summary>
```
A change of the type to `Store<T>?` is a change of the Public API. Follow-up question 2 lists it.
**Effort:** Low
**Resolved:** 2026-10-29 - The summary of the `Store` getter now states that a default value gives `null`. The type did not change.

---

## What's Already Good

- The store and the subscription match the Internal Shape of the plan. The equality check, the write, and the version step are three lines. The first read applies, because the store starts at 1 and the subscription starts at 0.
- Each wrong implementation of rule 8 fails a test: a `Set` that always raises the version (M3), a `TryGetChanged` that ignores `_applied` (M4), and a store that starts at version 0 (M5).
- The gate is strong. Each measured body holds no lambda, no string build, no boxed value, and no `Assert`. Each test calls `ReleaseAllocationContext` right before its measure. The warm-up covers the first use of each comparer and of the static selector. An allocation in `Version` or in `TryGetChanged` fails a gate (P1, P2). An empty `ChangedStep` fails the real-work test (M6).
- `AVersionReadAllocatesNothing` raises the three versions to 2, 3, and 2 before the measure, so the sum check finds a missed store or a wrong store.
- `AnEqualStringKeepsTheVersionAndTheOldReference` builds the equal string at run time, so `Assert.Same` proves that the store keeps the old reference.
- `ASubscriptionInAnArraySlotKeepsItsVersion` fails when the subscription becomes a class, because the copy then reads the new version.
- `IBindingSource` holds one member and names no Framework type, also in its doc comments. `BrutalUi.Framework` references Core only.
- The copy of `AllocationGate` is the same in code, and its remarks name the source.
- Section 10, section 14, and the changelog line match the code. The section 14 bullet records the plant result, and the ordinal "fourteenth" follows the thirteenth gate.

---

## Follow-up Questions

1. **A selector that throws uses up the change.** `TryGetChanged` writes `_applied = version` before it calls the selector (line 89, then line 90). The Internal Shape of the plan gives this order, but it gives no reason. When the selector throws, the next read gives `false`, and the reader keeps the old slice until the next `Set`.
   - (a) Keep the order. Add one remark sentence: "When the selector throws, the subscription still applies the version."
   - (b) Call the selector into a local first, and then write `_applied`. A selector that throws then throws at each read until the bug is fixed.
   - **Default:** (a). element-bindings decides the error rule of the binding pass, and it can choose (b) then.
   - **Answered:** (a), by default in the called mode of /autorun-feature. Review group 1 added the remark.
2. **The seam: a re-apply and a `null` store.** element-bindings can need a forced re-apply, for example after `Label.SetText` writes over a bound text. The subscription has no `Reset` method and no `Selector` getter. Also `Store` gives `null` on a default value (CR-3).
   - (a) No API change. The Framework adapter keeps its selector and builds a new subscription from `Store` and the selector. A struct assignment allocates nothing.
   - (b) Add `Reset()`, which sets `_applied` to 0, and change `Store` to `Store<T>?`.
   - **Default:** (a). The plan keeps the Public API, and element-bindings owns the adapter shape (Notes item 1).
   - **Answered:** (a), by default. No API change. element-bindings owns the adapter shape.
3. **An edit in place of a class value.** The remarks state that a new instance with the same fields is a change. They do not state the other half. A game that edits a list in the stored object and calls `Set` with the same reference gets no change.
   - (a) Add one sentence to the `Store<T>` remarks and to section 10: "An edit in place of a reference value, followed by a `Set` of the same reference, is not a change."
   - (b) Keep the text. The reference rule implies it.
   - **Default:** (a). The sentence costs one line and prevents a silent stale binding.
   - **Answered:** (a), by default in the called mode of /autorun-feature. Review group 2 added the sentence to the Store<T> remarks and to section 10.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort |
|-------|---------|--------|--------|
| 1 | CR-3: The `Store` getter does not state that a default subscription gives `null` | Low (the seam of element-bindings) | Low |
| 2 | CR-2: The `<returns>` tag of `TryGetChanged` does not include the first read | Low (a public tooltip) | Low |
| 3 | CR-1: The gate scene gives the wrong round for each string | Low (a test doc comment) | Low |

The three fixes touch doc comments only. One fix group can do all three, with one build and one test run.

---

## Progress

- [x] CR-1: The gate scene gives the wrong round for each string
- [x] CR-2: The `<returns>` tag of `TryGetChanged` does not include the first read
- [x] CR-3: The `Store` getter does not state that a default subscription gives `null`
