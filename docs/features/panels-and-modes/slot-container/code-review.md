# panels-and-modes/slot-container - Code Review

**Reviewed:** 2026-10-30
**Reviewer:** Agent (cross-phase review of phase 1 and phase 2)
**Rules Applied:** 1. The slot name and the element name (Decisions 2 and 3), 2. The mount rule (Decisions 4 and 5), 3. The unmount rule (Decision 6), 4. The lookup rule (Decision 7), 5. The 0-byte rule (Decision 8), 6. The test strength, 7. The docs, 8. The integration with the siblings, 9. The project rules. The Vercel React rules do not apply to C#.
**Status:** Resolved - 4 findings, 4 resolved

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 0 | - |
| LOW | 4 | CR-1, CR-2, CR-3, CR-4 |

The feature does what the plan asks. `SlotContainer.cs` follows the Public API and the Internal Shape of the plan. Each throw of `Mount` comes before a change. The held root moves with `ElementList.Move` and does not join again. `Unmount` returns the result of `Children.Remove`. The walk of `Find` stops at each slot. The 0-byte test measures 0 bytes in Debug, and a plant in each of the three members fails it.

The code has one small fault. A bad index gives two different `ParamName` values, and one of them names a parameter that `Mount` does not have (CR-4). Two findings are gaps in the tests. Two plants pass each of the 23 slot tests: a `Find` that walks into the panels of a root slot (CR-1), and a `Mount` with one message for the two parent cases (CR-2). One finding is a gap in the docs. The keep rule does not tell what Core does to the focus, the hover, and the running tweens of an unmounted panel, but the plan states that the section 10 text tells it (CR-3).

Rules 1, 5, 8, and 9 have no finding. Rule 2 has CR-4. Rule 3 and rule 7 have CR-3. Rule 6 has CR-1 and CR-2. The gaps of rule 2 and rule 4 are under rule 6.

### Baseline

- `dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2773, failed 0, succeeded 2770, skipped 3. The three skipped tests are the GLFW tests that need a display.
- The class counts: `SlotContainerTests` 10, `SlotLookupTests` 8, and `SlotContainerTreeTests` 5. Each count matches the plan.
- Review plant R1 (one message for the two parent cases) and review plant R2 (a `Find` that checks a root slot and then walks its children) ran in one build with a probe test file. The build gave `0 Error(s)`. Each of the 23 slot tests passed. The probe of R2 failed, because `Find` gave the inner slot.
- The same probe file showed the `ParamName` of a bad index: `toIndex` for a held root and `index` for a new root.
- Review plant R3 put `GC.KeepAlive(new object());` as the first line of `Unmount`. The build gave `0 Error(s)`. `MountUnmountAndFindAllocateNothingAfterWarmUp` failed with 14400 bytes (600 calls of 24 bytes). The other 22 slot tests passed. Plants 3 and 4 of `context.md` cover `Find` and `Mount`, so a plant in each of the three members fails the test.
- After each plant, the reviewer restored `SlotContainer.cs` from the scratchpad copy, and `cmp` found no difference. The reviewer deleted the probe file. A rebuild and a second test run gave the same counts.

---

## Findings

### CR-1: No test pins that `Find` does not walk into the panels of a root slot

**Severity:** LOW
**Rule:** 6 (The test strength), for rule 4 (The lookup rule)
**File:** `tests/BrutalUi.Framework.Tests/Panels/SlotLookupTests.cs`, lines 105 to 112 (`FindChecksTheRootItself`) and lines 156 to 167 (`FindDoesNotEnterTheChildrenOfASlot`)

**Problem:** Rule 4 states that a slot that is the root and matches no name gives `null`. The walk must not go into its panels. `FindChecksTheRootItself` calls `Find(slot, "right"u8)` on a slot with no child. `FindDoesNotEnterTheChildrenOfASlot` calls `Find` on a plain layout root. No test calls `Find` on a root slot that holds a panel with a slot in it. Review plant R2 changed `Find` to this shape:

```csharp
if (root is SlotContainer rootSlot)
{
    if (rootSlot._slotName.AsSpan().SequenceEqual(name)) { return rootSlot; }
    foreach (Element child in root.Children) { if (Walk(child, name) is SlotContainer found) { return found; } }
    return null;
}
```

Each of the 23 slot tests passed. A layout factory can return one slot as its layout root. With this bug, the result of `Find` then depends on the mounted panels, against Decision 7.

**Fix:** In `FindChecksTheRootItself`, mount a panel root that holds a slot `inner` into the root slot. Then assert `Assert.Null(SlotContainer.Find(slot, "inner"u8))`. Plant R2 must then fail the test. The count of `SlotLookupTests` stays 8.

**Effort:** Low

### CR-2: No test pins the message of each parent case

**Severity:** LOW
**Rule:** 6 (The test strength), for rule 2 (The mount rule)
**File:** `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTests.cs`, line 192 (`AMountOfARootThatAnotherSlotHoldsThrows`) and line 208 (`AMountOfARootWithAnotherParentThrows`)

**Problem:** Rule 2 and Decision 5 state that the message names the case: another slot or another parent. `SlotContainer.cs` line 117 picks the message with `parent is SlotContainer ? HeldByOtherSlotMessage : OtherParentMessage`. The two tests call `Assert.Throws<InvalidOperationException>` and do not read the message. Review plant R1 used `OtherParentMessage` for both cases. Each of the 23 slot tests passed. The message tells mode-set and the game the next step, so a swap of the two texts sends the reader to the wrong fix.

**Fix:** Keep the exception of each `Assert.Throws` call. In the first test, assert `Assert.Contains("another slot", e.Message)`. In the second test, assert `Assert.Contains("another parent", e.Message)`. Plant R1 must then fail one test. The count stays 10.

**Effort:** Low

### CR-3: The keep rule does not tell what Core does to the focus, the hover, and the running tweens

**Severity:** LOW
**Rule:** 3 (The unmount rule) and 7 (The docs)
**File:** `docs/technical-design.md`, line 1035 (the keep rule), and `src/BrutalUi.Framework/Panels/SlotContainer.cs`, lines 126 to 129 (the remarks of `Unmount`). The two files carry one text, so one finding names both.

**Problem:** The two texts state: "The root keeps its subtree, its dirty bits, its classes, its inline style, its name, and its bindings." They do not tell what Core does to the other state of the subtree. The plan expects it. Lines 313 and 314 of `implementation.md` state "The section 10 text states it" for the focus, the hover, and the tweens. Core does this at the next `Update` of the context:

- `DropStalePointerState` (`UiContext.cs`, line 1645) clears `Hover` and `Active` on a stored element that left the roots.
- `DropStaleFocus` (`UiContext.cs`, line 1692) clears the focus owner when the owner left the roots.
- Loop 0 of `TransitionPool.Advance` (`TransitionPool.cs`, lines 284 to 293) releases the record of a running tween outside the roots, with no write. The element keeps the last value of the tween. The next mount joins the root, so the restyle starts no tween and writes the target value.

The risk row at line 314 of `implementation.md` also gives the wrong cause. It names "the join rule of Core" for the drop at a remove, but the leave rule of the transition pool drops the record.

**Fix:** Add one sentence to the keep rule and to the remarks of `Unmount`: "At the next `Update`, Core clears the hover, the active state, and the focus of the subtree, and drops each running tween with no write." Change "The join rule of Core drops a tween on a remove" at line 314 of `implementation.md` to "The leave rule of the transition pool drops a running tween at the next `Update`".

**Effort:** Low

### CR-4: A bad index gives two `ParamName` values, and one names no parameter of `Mount`

**Severity:** LOW
**Rule:** 2 (The mount rule)
**File:** `src/BrutalUi.Framework/Panels/SlotContainer.cs`, lines 108 to 113 (the held branch of `Mount`) and line 98 (the `ArgumentOutOfRangeException` doc)

**Problem:** The held branch calls `Children.Move(Children.IndexOf(root), index)`. `ElementStorage.Move` checks the index with `ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(toIndex, _count)`, so `ParamName` is `toIndex`. The new branch calls `Children.Insert(index, root)`, and `ParamName` is `index`. The review probe showed both values. `Mount` has no parameter `toIndex`. The tests pin `ParamName` for `root` and for `name`, so the feature treats `ParamName` as part of its contract. Code that reads `ParamName` gets two answers for one fault. The tests at lines 119, 234, and 235 of `SlotContainerTests.cs` do not read it.

**Fix:** In the held branch, check the index in the slot before the move:

```csharp
ArgumentOutOfRangeException.ThrowIfNegative(index);
ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Children.Count);
Children.Move(Children.IndexOf(root), index);
```

The check comes before a change, so the rule "each throw comes before a change" stays. Then assert `Assert.Equal("index", e.ParamName)` at line 119 and at line 235 of `SlotContainerTests.cs`. Core gets no change.

**Effort:** Low

---

## What's Already Good

- The constructor checks the name before the copy. It throws `ArgumentException` with `ParamName` `name`, and `ANewSlotHoldsACopyOfItsNameAndNoChild` changes the source after the build.
- `SlotName` returns a `ReadOnlySpan<byte>` over the copy. No public member gives the array. `Find` reads `_slotName` and not `Name`, and `FindReadsTheSlotNameAndNotTheElementName` pins it.
- The type id comes from one static field. `AStylesheetStylesASlotByTypeName` asserts that a plain sibling does not take the color, so a slot with the type id of `Element` fails the test.
- The parent rule of `Mount` reads `root.Parent` one time and throws before `Children.Insert`. `Insert` checks a context root, a cycle, and the index before a change. Each claim of the remarks matches the Core code of `ElementList.Insert`, `Move`, `Remove`, and `ValidateChild`.
- `AMountOfAHeldRootMovesItWithNoJoin` reads the `Style` bit of the moved root after an `Update`. A held branch with `Children.Insert` sets that bit, so the test catches the join. The test also pins that a move to the same index marks nothing, and that the held range ends at `Count` minus 1.
- The colors of `SlotContainerTreeTests` are not default values. Each style test asserts that a plain sibling does not take the color.
- Each test that interns a class name uses the test name as a prefix. No test depends on the order of the classes.
- The 0-byte test warms the largest child count in two rounds. It calls `AllocationGate.ReleaseAllocationContext()` right before the measure, and it asserts a fixed sum of 17 for each step. An empty step body gives a sum of 0 and fails the test. A plant in `Find`, `Mount`, or `Unmount` fails it.
- The API covers each step of mode-set with 0 bytes and with no Core change. Assume that mode-set unmounts each leaving root before the first mount. Then a loop of `Mount(root_i, i)` never asks for a held index past `Count` minus 1, and a new index never goes past `Count`. A layout swap works with an `Unmount` on the cached slot of the old layout. `Unmount` does not need a context.
- `SlotContainer.cs` holds no `lock`, no `IDisposable`, no `event`, no `ToString(` call, no `(object)` cast, no `new List`, and no `Stack<` or `Queue<`. `BrutalUi.Core` has no change. The test counts, the plant results, and the byte counts in section 14 match `context.md`. The docs do not call the 0-byte test a numbered gate.

---

## Follow-up Questions

1. **What does the constructor do with a slot name that is not valid UTF-8?** The plan does not state it. `Encoding.UTF8.GetString` puts U+FFFD in place of each bad sequence, so `Name` then differs from the bytes of `SlotName`. `Find` still finds the slot, but a name rule of a stylesheet does not match it.
   - (A) Keep the code. The xml-markup loader and the UTF-8 literals give valid UTF-8.
   - (B) Reject a name that `System.Text.Unicode.Utf8.IsValid` rejects, with `ArgumentException` and `ParamName` `name`. Add one test.
   - (C) Keep the code, and add one sentence to the constructor remarks that tells the U+FFFD rule.
   - **Default:** (C). It costs one sentence and tells the reader the limit.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-4 | Low | Low | It is the one fault in the code. It changes an observable value of the public API. |
| 2 | CR-1 | Low | Low | It pins a rule that the brief names. A one-slot layout uses this path. |
| 3 | CR-2 | Low | Low | It pins the message that tells mode-set the next step. |
| 4 | CR-3 | Low | Low | It is a doc change only. Do it in the same group as CR-4, because both touch `SlotContainer.cs`. |

---

## Progress

- [x] CR-1: No test pins that `Find` does not walk into the panels of a root slot (review group 3: `FindChecksTheRootItself` mounts a panel with the slot `inner` into the root slot and asserts `null`. Plant R2 fails it. The count stays 8.)
- [x] CR-2: No test pins the message of each parent case (review group 2: the two tests assert "another slot" and "another parent". Plant R1 fails one test. The count stays 10.)
- [x] CR-3: The keep rule does not tell what Core does to the focus, the hover, and the running tweens (review groups 1 and 4: the remarks of `Unmount` and the keep rule of section 10 state the Core rule. The risk row of `implementation.md` keeps its text, because the called mode keeps the plan. `context.md` records the correct cause.)
- [x] CR-4: A bad index gives two `ParamName` values, and one names no parameter of `Mount` (review group 1: the held branch checks the index before the move. Review group 2: three asserts read `ParamName` `index`. Plant R4 fails `AMountOfAHeldRootMovesItWithNoJoin`.)

The review verifier returns PASS, with Demo: PASS: 2773 tests, 0 failed, 3 skipped. `scripts/test-linux.sh` prints its PASS line.

Follow-up question 1 uses the default (C): the constructor remarks and section 10 state the U+FFFD rule (review group 1 and review group 4). No code changed for it.
