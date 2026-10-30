# panels-and-modes/panel-definition - Code Review

**Reviewed:** 2026-10-30
**Reviewer:** Agent (cross-phase review of phase 1)
**Rules Applied:** 1. The name copy and the reads (Q4), 2. The validation (A5, Decisions 4 and 5), 3. The mode grouping and the getters (A4), 4. The record rules (Decisions 1, 2, and 6), 5. `PanelContext` (Q2), 6. The controller and `EnsureRoot` (A2, A3, Q3, Decision 7), 7. 0 bytes after warm-up, and a strong test, 8. The seam for the later features, 9. API clarity and correct remarks, 10. Test quality, 11. The docs, 12. The project rules. The Vercel React rules do not apply to C#.
**Status:** Resolved - 4 findings, 4 resolved (review groups 1 to 3)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 1 | CR-1 |
| LOW | 3 | CR-2, CR-3, CR-4 |

The feature does what the plan asks. The six source files follow the Public API and the Internal Shape of the plan. Each record copies its names before it stores them, and each check runs before the copy. The mode groups its panels by a byte compare, and each getter checks its index. `EnsureRoot` follows Decision 7, and the `finally` block resets `_building` on each path. The 0-byte test measures 0 bytes in Debug, and no read path of `Panels/` holds a box.

The code has no fault. Three findings are gaps in the tests. Two plants pass each of the 24 Panels tests: a mode that sorts its slots and its panels by name (CR-1), and a `PanelDefinition` with value equality (CR-2). No test reads `ModePanel.Slot` or `ModePanel.Panel`, and the 0-byte test does not read them (CR-3). One finding is a remark of `PanelController` that tells the wrong result of a call from inside the factory (CR-4).

Rules 1, 2, 3, 4, 5, 6, 8, 11, and 12 have no finding. The test gap of rule 4 is under rule 10. Rule 7 has CR-3. Rule 9 has CR-4. Rule 10 has CR-1, CR-2, and CR-3.

### Baseline

- `dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2750, failed 0, succeeded 2747, skipped 3. The three skipped tests are the GLFW tests that need a display.
- The class counts: `PanelRecordTests` 14 and `PanelControllerTests` 10. Each count matches the plan.
- After each plant, `cmp` found no difference. A rebuild and a second test run gave the same counts.

---

## Findings

### CR-1: The grouping test passes with a mode that sorts its slots and its panels

**Severity:** MEDIUM
**Rule:** 10 (Test quality)
**File:** `tests/BrutalUi.Framework.Tests/Panels/PanelRecordTests.cs`, lines 39 to 50 (`NewMode`) and lines 153 to 166 (`AModeDefinitionGroupsThePanelsBySlotInTheOrderOfTheList`)

**Problem:** The test uses the entries `left:a`, `right:c`, and `left:b`. The order of the first entry gives `left` and then `right`. A byte sort of the slot names gives the same order. The panels of `left` are `a` and `b` in the list, and a sort gives the same order. Thus the test cannot see the difference between the order of the list and a sort.

A plant proved it. The plant added these lines after the second pass of the `ModeDefinition` constructor:

```csharp
Array.Sort(slots, static (x, y) => x.Name.AsSpan().SequenceCompareTo(y.Name));
for (int s = 0; s < slots.Length; s++)
{
    Array.Sort(slots[s].Panels, static (x, y) => x.AsSpan().SequenceCompareTo(y));
}
```

Each of the 24 Panels tests passed. The rule of A4 is the order of the list, and mode-set mounts the panels in that order. A later change that sorts the slots or the panels thus breaks the mount order of each mode with no failed test.

**Fix:** Use entries that a sort puts in a different order. For example, use `right:c`, `left:b`, and `right:a`. Assert `SlotCount` 2, slot 0 `right` with `c` and then `a`, and slot 1 `left` with `b`. Change `NewMode` of `PanelRecordTests` only, or add the case to the grouping test. The count of the class stays 14.

**Effort:** Low

### CR-2: No test pins the equality rule of the records

**Severity:** LOW
**Rule:** 10 (Test quality). The code of rule 4 has no fault.
**File:** `tests/BrutalUi.Framework.Tests/Panels/PanelRecordTests.cs` (no test). The rule is in `src/BrutalUi.Framework/Panels/PanelDefinition.cs` lines 20 to 25, `LayoutDefinition.cs` lines 17 to 20, and `ModeDefinition.cs` lines 70 to 73.

**Problem:** The remarks of each record and section 10 of the technical design state the equality rule of Decision 6. Two records from two constructor calls are not equal, and a `with` copy equals its source. No test asserts it. A plant added this pair to `PanelDefinition`:

```csharp
public bool Equals(PanelDefinition? other) => other is not null && _name.AsSpan().SequenceEqual(other._name) && _defaultSlot.AsSpan().SequenceEqual(other._defaultSlot);
public override int GetHashCode() => _name.Length;
```

The build passed, and each of the 24 Panels tests passed. The remarks then state a wrong rule, and no test shows it. The `with` rule has no test too: a `with { }` copy equals its source, and it holds the same names.

**Fix:** Extend `APanelDefinitionHoldsItsNamesAndItsFactory` and the two other "holds" tests with three asserts for each record. Two records from two calls with the same names are not equal. A `with { }` copy equals its source. The copy holds the same names. The count of the class stays 14.

**Effort:** Low

### CR-3: No test reads `ModePanel.Slot` or `ModePanel.Panel`

**Severity:** LOW
**Rule:** 7 (0 bytes after warm-up, and a strong test) and 10 (Test quality)
**File:** `tests/BrutalUi.Framework.Tests/Panels/PanelRecordTests.cs` (no test) and `tests/BrutalUi.Framework.Tests/Panels/PanelControllerTests.cs`, lines 121 to 157 (`ReadStep`). The members are in `src/BrutalUi.Framework/Panels/ModeDefinition.cs`, lines 43 to 47.

**Problem:** `ModePanel.Slot` and `ModePanel.Panel` are public name reads. A search of `tests/BrutalUi.Framework.Tests/Panels/` for `.Slot` and `.Panel` finds no line. Thus no test shows the following:

1. `Slot` returns the slot name and `Panel` returns the panel name. A swap of the two fields passes each test.
2. Each property returns an empty span for `default(ModePanel)`, as its XML doc states.
3. Each read allocates 0 bytes. F6 of the Definition of Done names "each name read", and `ReadStep` reads no `ModePanel`. `ReadStep` also does not read `ModeDefinition.ActionMap`.

Each property is a field read, so the risk is low. But F6 and the public docs name these reads.

**Fix:** In `PanelRecordTests`, extend `AModeDefinitionCopiesItsNames` or the validation test. Read `Slot` and `Panel` of one `ModePanel`, and assert that both spans of `default(ModePanel)` are empty. In `PanelControllerTests`, give `ReadStep` one `ModePanel` and add `Slot.Length` and `Panel.Length` to the sum. Also add 1 to the sum when `mode.ActionMap` is `null`. The class counts stay 14 and 10.

**Effort:** Low

### CR-4: A remark of `PanelController` tells a wrong result of a call from inside the factory

**Severity:** LOW
**Rule:** 9 (API clarity and correct remarks)
**File:** `src/BrutalUi.Framework/Panels/PanelController.cs`, lines 21 to 24

**Problem:** The remark reads:

> A factory that returns `null` makes `EnsureRoot` throw `InvalidOperationException`, and the controller stays unbuilt. A call from inside the factory throws `InvalidOperationException` in the same way.

"In the same way" tells the reader that the controller stays unbuilt. The code does not do that. Only the inner call throws. The outer call goes on. When the factory catches the exception and returns a root, the outer call keeps the root, and the controller is built. `ACallFromInsideTheFactoryThrows` asserts this result (`IsBuilt` is `true`). slot-container and mode-set read these remarks, so the remark can cause a wrong error path in a later feature.

**Fix:** Replace the last sentence with: "A call from inside the factory throws `InvalidOperationException`. Only the inner call throws. The outer call goes on, and it keeps the root that the factory returns."

**Effort:** Low

---

## What's Already Good

- Each record copies each name with `ToArray()` after its checks. Each name property is an expression body over a `private readonly byte[]` field. No public member gives a `byte[]` or a `byte[][]`. `SlotBytes` and `PanelBytes` are `internal`, and no code writes the arrays.
- Each `ParamName` matches the plan: `name`, `defaultSlot`, `layout`, `slot`, `panel`, `panels`, `createRoot`, `definition`, `context`, `slot`, and `index`. A rejected constructor gives no reference to a caller.
- The duplicate check and the slot match use `SequenceEqual` on the bytes. The test builds each entry with a new `ModePanel`, so a reference compare fails it.
- Each record has one explicit constructor, a `private bool PrintMembers(StringBuilder builder)`, and no `init` member. A `with` expression uses the copy constructor of the compiler and shares the immutable arrays, so it cannot skip the checks.
- `PanelContext` checks `null` in each `init` body. The `required` modifier makes the compiler reject a missing member, and the class cannot satisfy a `new()` constraint.
- `EnsureRoot` does what Decision 7 states on each path. A factory that catches the inner exception and returns a root builds the controller. A factory that throws after an inner call leaves the controller unbuilt, and `_building` resets in the `finally` block.
- The 0-byte test warms each path two times, calls `AllocationGate.ReleaseAllocationContext()` right before the measure, and holds no lambda, no string, no array, and no `Assert` in the body. A fixed sum makes an empty body fail.
- The project rules hold. The search for `lock`, `IDisposable`, `event`, `ToString(`, and `(object)` in `Panels/` finds no line. No file of `src/BrutalUi.Core/`, no project file, no file of `scripts/` or `samples/`, and no base test file changed.
- Section 10, section 14, and the changelog line match the code. Section 14 does not call the 0-byte test a numbered gate.

---

## Follow-up Questions

1. **When does mode-set use `PanelDefinition.DefaultSlot`?** The remark of `PanelDefinition` states that `DefaultSlot` "names the slot of the panel when a mode does not name one". But the `ModePanel` constructor rejects an empty slot, and `ModeDefinition` rejects `default(ModePanel)`. Thus no mode can omit the slot of a panel. The mode-set requirements do not name the default slot. Notes item 7 of the plan gives the choice to mode-set.
   - **(A) Default:** mode-set keeps the current API and does not use `DefaultSlot` in the diff. mode-set changes the remark of `PanelDefinition` to state the real use, or no use.
   - (B) mode-set adds a `ModePanel(ReadOnlySpan<byte> panel)` constructor with no slot. Then `ModeDefinition` must keep the entries with no slot until mode-set resolves them with the panel records. This option changes the `default(ModePanel)` check and the grouping.
   - (C) Remove `DefaultSlot` before mode-set starts. This option changes the public API and each test that builds a `PanelDefinition`.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort |
|-------|---------|--------|--------|
| 1 | CR-1 | Medium: guards the mount order that mode-set reads | Low |
| 2 | CR-4 | Low: a correct error path for slot-container and mode-set | Low |
| 3 | CR-2 | Low: guards the equality rule of the remarks | Low |
| 4 | CR-3 | Low: tests two public reads and closes F6 | Low |

---

## Progress

- [x] CR-1: The grouping test passes with a mode that sorts its slots and its panels (review group 1: `NewMode` lists `right:c`, `left:b`, `right:a`. The sort plant fails the grouping test.)
- [x] CR-2: No test pins the equality rule of the records (review group 1: the three "holds" tests assert the reference equality and the `with` copy. The value-equality plant fails a test.)
- [x] CR-3: No test reads `ModePanel.Slot` or `ModePanel.Panel` (review groups 1 and 2: `AModeDefinitionCopiesItsNames` reads both names and the empty spans of `default(ModePanel)`. The 0-byte test reads both names and `ActionMap`. An allocation plant in `Slot` fails the 0-byte test.)
- [x] CR-4: A remark of `PanelController` tells a wrong result of a call from inside the factory (review group 3: the remark states that only the inner call throws and that the outer call keeps the root.)

The review verifier returns PASS and Demo: PASS. The solution gives 2750 tests, 0 failed, 3 skipped. The class counts stay 14 and 10.
