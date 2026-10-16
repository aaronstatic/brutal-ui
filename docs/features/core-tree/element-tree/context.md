# core-tree/element-tree - Context & Decisions

**Last Updated:** 2026-10-16 18:25
**Current Phase:** Complete (all five phases and the cross-phase review done)
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Implementation plan (`implementation.md`), accepted by the architect
- ✅ Dev docs scaffolded (`context.md`, `tasks.md`, `inbox.md`)
- ✅ Phase 1, tasks 1.1 to 1.10: the pooled child list, the dirty bits, the dirty walk, and `UiContext` with the pass seam (verifier PASS)
- ✅ Phase 2, tasks 2.1 to 2.6: insert, move, and the reparent (verifier PASS)
- ✅ Phase 3, tasks 3.1 to 3.7: `ClassId`, the global class table, and the inline class set with the pooled overflow (verifier PASS)
- ✅ Phase 4, tasks 4.1 to 4.6: pseudo-states, `IsPickable`, the `ref` slots, and the fluent builder (verifier PASS)
- ✅ Phase 5, tasks 5.1 to 5.6: the guards, the root guard, the allocation test (0 bytes), and section 5 of the technical design (verifier PASS)
- ✅ Cross-phase review: 11 findings (1 MEDIUM, 10 LOW), all fixed. Verifier `review-verify.md` PASS. 126 tests pass.
- ✅ The draft plan of the next feature: `docs/features/core-tree/flexbox-layout/implementation.md`

**What's Next:**
- 📋 A person runs the items of `## Needs Human Verification` (the Windows run first)
- 📋 The caller (`/autorun-epic`) commits the work and asks the open scope questions of the flexbox-layout draft

**Blockers:**
- none

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Tree/` - The element tree types (phases 1 to 5)
- `src/BrutalUi.Core/BrutalUi.Core.csproj` - `GenerateDocumentationFile` and `InternalsVisibleTo` (phase 1)

### Related Files
- `tests/BrutalUi.Core.Tests/` - The tree tests (phases 1 to 5)
- `docs/technical-design.md` - Section 5 update (phase 5)
- `docs/features/core-tree/element-tree/implementation.md` - Implementation plan
- `docs/features/core-tree/flexbox-layout/requirements.md` - The next feature. It plugs its layout pass into `UiContext.Update`.

---

## Important Decisions

The plan holds decisions 1 to 10 and the confirmed assumptions Q1 to Q4 and A1 to A9. This section records the decisions made during the build.

- The run is the called mode of `/autorun-feature`, from `/autorun-epic`. The orchestrator makes no git write. The caller commits.
- Plan Notes item 4 (stale descendant bits when the layout pass runs its own walk) is a hand-off item for the flexbox-layout draft plan.

### Phase 1 (2026-10-16)

- **Propagation carries the full set.** `PropagateToAncestors` computes `add = descendantBits & ~p.flags` at each ancestor with the full input set, not with the `add` of the step before. While the invariant holds, both give the same loop. The full set also repairs a bit that a defect lost.
- **One propagation method.** `MarkDirty` and the add path both call `PropagateToAncestors`. The add path lives in the internal `Element.AttachToParent(Element)`. It sets the parent, sets the `Style` self bit, and sends `ToDescendant(flags & SelfMask) | (flags & DescendantMask)` up from the new parent. `ElementList.Add` then calls `_owner.MarkDirty(DirtyFlags.Layout)`. `_dirty` and `_parent` stay private to `Element`.
- **Internal seams on `Element`.** `ref ElementStorage ChildStorage` (the view and the walk use it, no struct copy), `AttachToParent`, `DetachFromParent`, and `ClearDirty(DirtyFlags)`. Only the walk calls `ClearDirty`.
- **`ToDescendant` masks its input.** It shifts `self & SelfMask`. A descendant bit in the input cannot become an unknown bit 64 or higher.
- **Debug asserts.** `Walk` asserts self bits only in `kinds`. `PropagateToAncestors` asserts descendant bits only. `AttachToParent` asserts no parent. The asserts cost nothing in Release.
- **The walk early return.** `Walk` returns at once when the root has no walked bit. `WalkElement` reads the flags one time before the visit, and checks each child before the recursive call, so a clean child costs one flag check and no call.
- **`ElementStorage.RemoveAt` checks the index.** It throws `ArgumentOutOfRangeException`. Without the check, an index past the count but inside the array removes the last item with no error. `ElementList.RemoveAt` and the indexer read the span first, so a bad index throws `IndexOutOfRangeException` before any change. Phase 5 replaces it with `ArgumentOutOfRangeException` and the parameter name.
- **`IndexOf`, `Contains`, and `Remove` search the storage.** They do not use the parent link as a shortcut. They stay correct during the reparent of phase 2.
- **`deltaTime`.** The doc comment of `Update` names the parameter and its later readers. The build gives no unused-parameter warning, so the code has no suppression.
- **`ClearVisitor`** is a `readonly struct`. `ElementStorage.Count`, `AsSpan`, and `IndexOf` are `readonly` members, so a read through a `readonly` path makes no defensive copy.
- **Verification.** A scratch console program (outside the repository) ran 26 checks of the public API on the Debug build: the bits after each mark, add, remove, and update, the early stop, the 40-add order, and 0 bytes allocated over 100 warm loops of remove, add, mark, and update. All passed.
- **The `GenerateDocumentationFile` property repeats a value.** Task 1.7 adds it to `BrutalUi.Core.csproj` with an XML comment that says so. `src/Directory.Build.props` already sets it. The build found no missing doc comment in the files of group A, so no file of group A changed. The `InternalsVisibleTo` item has its own comment.
- **The placeholder test is gone (A9).** The deleted `PlaceholderTests.cs` of `BrutalUi.Core.Tests` was the only file of that project. The two other test projects keep their placeholders.
- **The recording visitor.** `RecordingVisitor` is an `internal struct` at the top of `DirtyTrackingTests.cs`. It holds two lists: the visited elements and the flags of each visit. `Visit` is a `readonly` member, because it changes only the list contents and not the fields.
- **Two tests break the invariant on purpose.** With the internal `ClearDirty`, the test removes one descendant bit, so the result shows the behavior. In `MarkOnASiblingLeafStopsAtTheSharedParent`, the root loses its bit, and the second mark must not put it back. In `WalkDoesNotEnterTheChildrenOfAnElementWithNoDescendantBit`, the top of subtree B loses its bit, and the walk must not visit the leaf below it. Without the broken invariant, a mark that goes to the root and a mark that stops give the same flags. The plain versions of both tests also exist and use the public API only.
- **The skip of a clean child has no visible effect.** The walk skips a child with no walked bit. A walk that enters that child also visits nothing and clears nothing. Thus the tests prove the skip through the descendant-bit test above and through the exact recorder list, not through the child check itself.
- **The clean step of the tests.** The helper classes `Chain` and `TwoSubtrees` clean a tree with a new `UiContext`, an `Update(0f)`, and a `RemoveRoot`. The helper asserts that each element is clean before the mark under test.
- **Extra tests in the scope of the brief.** `ElementListTests`: a new element has no children, `RemoveAt` of the last index, the remove of the only child and a new add, `foreach` on an empty list, `Remove` of a child of another owner. `DirtyTrackingTests`: a mark of another kind goes past the shared parent, a mark with no self bit changes nothing, `Update` with two roots, `AddRoot` keeps the bits and `RemoveRoot` takes the root out of the update, the depth-first visit order, the walk keeps the bits of the other kinds, the walk on a clean tree, the add marks, `RemoveAt` marks the old parent, and a removed child keeps its bits.
- **No test of the phase 1 throws.** No test calls `Add` or `AddRoot` with an element that has a parent. Phase 2 replaces both throws with the reparent, so such a test breaks in phase 2.
- **Task 1.10 results.**
  - `dotnet build brutal-ui.sln`: 0 warnings, 0 errors.
  - `dotnet test --solution brutal-ui.sln`: total 33, failed 0, succeeded 33, skipped 0. The core project alone: `ElementListTests` 12 of 12, `DirtyTrackingTests` 19 of 19 (from `--filter-class` on the test executable). The other 2 tests are the placeholders of the framework and markup projects.
  - `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 10 s`.
  - The `grep -E 'ElementListTests|DirtyTrackingTests|Passed|Failed' artifacts/test-linux/test.log` gives one line: `Test run summary: Passed!`. The log also holds `total: 33` and `failed: 0`. Microsoft.Testing.Platform does not write the name of a test that passes, so the log holds no class name. The demo line of phase 1 in `tasks.md` expects the class names. A later demo can add `--output detailed` to the test step of the script, or read the counts.

### Phase 2 (2026-10-16)

- **Task 2.6 results.**
  - `dotnet build brutal-ui.sln`: 0 warnings, 0 errors.
  - `dotnet test --solution brutal-ui.sln`: total 59, failed 0, succeeded 59, skipped 0. The core project holds 57 of them (26 new in phase 2). The two placeholders stay in the framework and markup projects.
- **One reparent path.** `ElementList.Add` and `Insert` call the private `LeaveParent` first. It calls `Children.Remove` on the old parent (the "Remove" row). Then the code runs the "Add" row as in phase 1. `UiContext.AddRoot` uses the same `Children.Remove` call. No second propagation path exists.
- **`Insert` checks the index before the remove.** When the child belongs to this owner, the valid range is 0 to `Count - 1`. Otherwise it is 0 to `Count`. A bad index throws `ArgumentOutOfRangeException` (with the name `index`) and changes nothing. Phase 5 task 5.2 can keep this code and only add the null checks.
- **`Move` checks both indexes in `ElementStorage.Move`.** A bad index throws before any change, also when both indexes are equal. `ElementList.Move` marks the owner for layout only when the two indexes differ.
- **Shifts use `Span<T>.CopyTo`.** The call handles an overlap of source and destination, so no temporary array exists. `Insert` shares the growth code with `Add`.
- **Decision 5 holds.** Each add and insert marks the child for style, also on a reparent and on an add of a child of the same owner. A test shows the exact flags for the same-owner case.
- **Failed insert with a parent that is not the owner.** The check happens before `LeaveParent`, so a bad index leaves the child with its old parent.
- **Open question unchanged.** `Add` of a root of a context still works (A4). Phase 5 owns the guard. No test covers it.

### Phase 3 (2026-10-16)

- **Task 3.7 results.**
  - `dotnet build brutal-ui.sln`: 0 warnings, 0 errors.
  - `dotnet test --solution brutal-ui.sln`: total 82, failed 0, succeeded 82, skipped 0. The phase adds 23 tests in `ClassListTests` (59 before).
- **Ref-return access, no copy.** `Element.ClassStorage` is an internal `ref ClassSet`, like `ChildStorage`. `ClassList` calls `Add`, `Remove`, and `Contains` on that ref, so the mutable struct is never copied.
- **`ClassName` table.** One `Lock`, one `Dictionary<string, ClassId>` with the ordinal comparer, and one `List<string>` with an empty placeholder at index 0. `GetName` throws `ArgumentOutOfRangeException` (parameter `id`) for `None`, for a negative value, and for a value past the table.
- **`ClassId.ToString()`** returns `(none)` for `None` without a table call. For any other id it calls `GetName`.
- **`ClassId` constructor is internal.** Only the table makes ids. The tests read the constructor through `InternalsVisibleTo` to build an unknown id.
- **Overflow capacity.** The ninth add rents 16. The code reads `_overflow.Length` for the capacity, because the pool can give a larger array. A full array rents two times the length and returns the old array with `clearArray: false`.
- **Free slots.** A remove writes `default` to the freed slot. It is not necessary for the pool, but the slots stay clean for a debugger view.
- **Set indexer checks the range.** `ClassSet.this[int]` throws `ArgumentOutOfRangeException` for an index outside 0 to `Count - 1`. Without it, a read past the count would give a stale inline slot. Phase 5 task 5.2 can add the parameter name if it needs one (the framework already sets `index`).
- **Search is linear.** `IndexOf` uses `ReadOnlySpan<ClassId>.IndexOf` (vectorized, `ClassId` is a `IEquatable` struct). The set holds a few ids in the normal case.
- **Thread test.** `FourThreadsThatInternTheSameNamesGetOneIdForEachName` is an `async Task` test, because the xUnit analyzers (errors in this build) reject `Task.WaitAll` (xUnit1031) and a call with no `TestContext.Current.CancellationToken` (xUnit1051). A `Barrier` starts the four threads together.
- **No fixed ids in tests.** Each test uses its own `nameof` prefix. No test asserts a number of an id.

### Phase 4 (2026-10-16)

- **Task 4.6 results.**
  - `dotnet build brutal-ui.sln`: 0 warnings, 0 errors.
  - `dotnet test --solution brutal-ui.sln`: total 95, failed 0, succeeded 95, skipped 0. The phase adds 13 tests in `ElementStateTests` (82 before).
- **Empty structs.** `ComputedStyle` and `LayoutNode` are plain (not `readonly`) structs with no field. No analyzer rule fired, so no suppression exists.
- **`States` setter.** It compares the value, stores it, and calls `MarkDirty(DirtyFlags.Style)`. A set to the same value returns before any mark. Plan Notes item 3 holds: the setter is public. Phase 5 updates the design doc.
- **`IsPickable`** is an auto property with the initializer `true`. It marks nothing. A write through `ref Style` or `ref Layout` marks nothing either. Tests cover both.
- **Builder.** `ElementBuilder` has `Class(string)`, `Class(ClassId)`, and `Child`, and no other method. `Child` returns the parent, so a chain continues on the parent. A test uses a private derived element to show that the type stays.
- **Extra tests.** The state change on a child gives the root `DescendantStyle`, a set to `None` marks style, and the `Class(ClassId)` overload works. The same-value test uses the internal `ClearDirty` to start from a clean element after the first set.
- **Rule check.** The brief rules held. The builder test uses `Class("a")` and `Class("b")` and compares ids with `ClassName.Intern`, with no fixed id value.

### Phase 5 (2026-10-16)

Group 1: tasks 5.1 to 5.3 and the root guard. Group 2: tasks 5.4 to 5.6 (done, see the end of this block).

- **Results.** `dotnet build brutal-ui.sln`: 0 warnings, 0 errors. `dotnet test --solution brutal-ui.sln`: total 119, failed 0, succeeded 119, skipped 0. The group adds 24 tests in `TreeGuardTests` (95 before).
- **Root guard.** `Element` has `internal bool IsRoot`. `UiContext.AddRoot` sets it after the add. `UiContext.RemoveRoot` clears it when it removes the element. `AddRoot` of an element with the flag throws `InvalidOperationException`. This covers the duplicate root and a root of a second context. `ElementList.Add` and `Insert` of an element with the flag throw `InvalidOperationException` ("remove it from its UiContext first"). The flag is a `bool`, so A4 holds. `RemoveRoot` on a context that does not hold the element returns `false` and keeps the flag.
- **Cycle guard.** `ElementList.ValidateChild` (private) walks up from the owner with `Parent`. It throws when the owner or an ancestor is the child. The cost is the depth of the owner. `ElementBuilder.Child` uses `Add`, so it has the guard with no extra code.
- **Check order.** `ValidateChild` runs first in `Add` and `Insert`: null, then root flag, then cycle. `Insert` checks the index after that. Each check runs before `LeaveParent`, so a failed call changes nothing.
- **Null checks.** Besides `Add` and `Insert`, `Remove`, `IndexOf`, and `Contains` of `ElementList` and `RemoveRoot` of `UiContext` also throw `ArgumentNullException` for null. The brief says "each `Element` argument". `ClassName.Intern` uses `ArgumentException.ThrowIfNullOrEmpty`: null gives `ArgumentNullException`, empty gives `ArgumentException`. Both name `name`.
- **Index checks.** The `ElementList` indexer and `RemoveAt` check the index and throw `ArgumentOutOfRangeException` named `index` (they threw `IndexOutOfRangeException` before). `Move` already gives `fromIndex` and `toIndex` through the storage. `ClassList` indexer already names `index`. Tests check each name.
- **Tests.** `TreeGuardTests` uses a private `Tree` helper. It cleans the tree with a `UiContext` (`AddRoot`, `Update(0f)`, `RemoveRoot`), so the root flag is not set and the cycle guard runs. The helper asserts the exact `Dirty` value (`None`), the parents, and the counts after a failed call. No existing test comment held the "no test may make a cycle" rule.

Group 2 (tasks 5.4 to 5.6):

- **Allocation result.** `TreeAllocationTests.TreeChangesAndUpdateAllocateNothingAfterWarmUp` measures 0 bytes for 100 runs of the body (`Assert.Equal(0L, after - before)`). Five more full test runs gave the same result, with no flake. No source file of `Tree/` allocates, so no fix brief is needed.
- **Test design.** A private `Scene` class holds the tree, the spare element, the leaf, the two class elements, and the two `ClassId` values. The measured body is the `static` method `Step(Scene)`. It has no lambda, no boxing, no string formatting, and no `Assert`. The test interns 12 names before the warm-up. The tree is a root, 8 children, and 8 children for each child (73 elements). One element holds 12 classes (overflow), and one holds 4 (inline). Two warm-up runs of one step each, then 100 measured steps. A second test, `TheMeasuredBodyKeepsTheTreeShape`, checks that the body keeps the counts, the classes, the state, and a clean root. It proves that the body does real work.
- **Test project usings.** The project has no implicit usings, so the test file has `using System;` for `GC`.
- **Design doc.** Section 5 "Shape" now shows `Element`, `ElementList`, `ClassList`, `ClassId`, `ClassName`, and `UiContext` as in the code, with `IsPickable`, `States { get; set; }`, `Dirty`, and `MarkDirty`. One sentence after the code block names `DirtyFlags` and `ElementBuilder`. One design decision line covers the two-bit dirty flags. The brief asked for one line, so the doc has no separate line on the views. The prose lint score of `docs/technical-design.md` is 0.84 per 100 words (limit 2.5).
- **Results (task 5.6).** `dotnet build brutal-ui.sln`: 0 warnings, 0 errors. `dotnet test --solution brutal-ui.sln`: total 121, failed 0, succeeded 121, skipped 0 (the group adds 2 tests). `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 10 s`, `exit=0`. `--list-tests` counts: `ElementListTests` 30, `DirtyTrackingTests` 27, `ClassListTests` 23, `ElementStateTests` 13, `TreeGuardTests` 24, `TreeAllocationTests` 2. Every `Tree/*.cs` file has `namespace BrutalUi.Core;`. The lambda and collection grep gives 8 hits, all expression-bodied members (`ClassId`, `ClassList.Contains`, `ElementList.GetEnumerator`), and no lambda. `PlaceholderTests.cs` is gone. `PackageReference` count is 2, `Directory.Packages.props` has no diff against `main`, and the csproj has `GenerateDocumentationFile` and `InternalsVisibleTo`. `UiContext.cs` has the two seam comments and one clear pass. `LayoutNode` and `ComputedStyle` are empty public structs, stored inline in `Element` and returned by `ref`.

---

## Gotchas & Learnings

- `src/Directory.Build.props` already sets `GenerateDocumentationFile` to `true` for each library project. CS1591 is an error before task 1.7. Task 1.7 can skip that property, or add it with no change in effect. The `InternalsVisibleTo` item is still necessary.
- The blast radius of group A is new files only. No shared code changed. No other project uses the new types yet.
- **The plan Risk row on a change during `foreach` states a false fact (CR-2).** `implementation.md` says that a read after a growth gives `null`, not another tree. The review probe showed that the next `Rent` of the same pool bucket on the same thread gets the old array back, so a read can give `null` or an element of another list. The doc comments of `ElementList` and `UiContext.Roots` now say so. The plan row stays as written, because the called mode keeps the plan. Plan Notes item 9 (no version check) still holds: the change is a caller error.
- **`test.log` holds no class names (CR-11).** Microsoft.Testing.Platform prints no name of a passed test, so the demo lines that expect class names in `artifacts/test-linux/test.log` need `--filter-class` (see "Needs Human Verification"). A later dev-ops feature can give the test step of `scripts/test-linux.sh` a detailed output option.
- **The layout pass needs its own seam (CR-1).** The walk calls the visitor only for an element with a self bit, and pre-order only. A flex parent with only `DescendantLayout` gets no visit. The flexbox-layout pass needs its own recursion with `ClearDirty` (plan Notes item 4), and `UiContext` needs an available size for each root. Both go to the flexbox-layout plan.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/*Tests.cs`
- `ElementListTests`, `DirtyTrackingTests`, `ClassListTests`, `ElementStateTests`, `TreeGuardTests`, `TreeAllocationTests`

### Integration Tests
- `scripts/test-linux.sh` runs the full build and the tests.
- (MANUAL) `scripts\test-windows.ps1` on Windows.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `scripts/test-linux.sh`, sees the line `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s`, and sees the `ElementListTests` and `DirtyTrackingTests` results with 0 failed in `artifacts/test-linux/test.log`.
- [ ] Phase 2 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the reorder and reparent tests of `ElementListTests` and `DirtyTrackingTests` pass with 0 failed.
- [ ] Phase 3 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ClassListTests` pass with 0 failed, with the test that adds a ninth class and reads all nine back.
- [ ] Phase 4 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ElementStateTests` pass with 0 failed, with the test that builds a three-level tree in one builder expression.
- [ ] Phase 5 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees `TreeGuardTests` and `TreeAllocationTests` pass with 0 failed.
- [ ] Windows run (Q7): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
- [ ] How to see the class names of the demos (CR-11): `dotnet test` and `artifacts/test-linux/test.log` print no name of a passed test, only `total` and `failed: 0`. For each class of a demo line, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<ClassName>'`, for example `'*ElementListTests'`. Expect a count above 0 and `failed: 0`.

---

## Performance Considerations

- No managed allocation after warm-up in add, insert, remove, move, the class operations, `MarkDirty`, and `UiContext.Update`. `TreeAllocationTests` (phase 5) measures it.

---

## Open Questions

- **RESOLVED in phase 5 (root flag, see the Phase 5 block). A root that is also a child.** `ElementList.Add` accepts a root of a context, because a root has no parent and an element holds no context reference (A4). The element is then a root and a child. `Update` walks it two times. The clear is safe, but a later layout pass can lay it out two times. No phase guards this case. Phase 2 (task 2.3) or phase 5 (task 5.2) must decide: reject it, or remove the root from the context. The second choice needs a context reference, which A4 rules out.
- **RESOLVED in phase 5 (task 5.1). A cycle before phase 5.** Until task 5.1, `a.Children.Add(a)` succeeds when `a` has no parent. The propagation stops, but the next `Update` recurses with no end and overflows the stack. Group B tests must not do this call before phase 5.

---

## Session Notes

### 2026-10-16 17:05
- Dev docs scaffolded from the accepted plan. 35 tasks across 5 phases.

### 2026-10-16 18:00
- Phase 1 group A done: tasks 1.1 to 1.6. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. Group B is open: tasks 1.7 to 1.10.

### 2026-10-16 19:00
- Phase 1 group B done: tasks 1.7 to 1.10. The build gives 0 warnings. 33 tests pass, 0 fail. `scripts/test-linux.sh` passes. Phase 1 is complete.

### 2026-10-16 17:40 (orchestrator)
- Phase 1 verifier (`phase-1-verify.md`): PASS. Demo: PASS. The PASS line printed with exit 0. `ElementListTests` 12 of 12 and `DirtyTrackingTests` 19 of 19 pass (from `--filter-class`, because the log names no passed test).
- The open question "A root that is also a child" goes to the phase 5 brief (the guards). The orchestrator default: an internal flag (not a context reference, so A4 holds) lets `Add` and `Insert` reject a root, and lets `AddRoot` reject an element that is a root already.

### 2026-10-16 17:55 (orchestrator)
- Phase 2 done: tasks 2.1 to 2.6. Verifier (`phase-2-verify.md`): PASS. Demo: PASS. 59 tests pass. `ElementListTests` 30 of 30, `DirtyTrackingTests` 27 of 27.

### 2026-10-16 18:10 (orchestrator)
- Phase 3 done: tasks 3.1 to 3.7. Verifier (`phase-3-verify.md`): PASS. Demo: PASS. 82 tests pass. `ClassListTests` 23 of 23, with `NineAddsKeepAllNineInOrder`.

### 2026-10-16 18:25 (orchestrator)
- Phase 4 done: tasks 4.1 to 4.6. Verifier (`phase-4-verify.md`): PASS. Demo: PASS. 95 tests pass. `ElementStateTests` 13 of 13, with `BuilderOneExpressionFromTheTaskGivesTheExpectedTree`.
- Phase 5 splits into two groups: group 1 (tasks 5.1 to 5.3 and the root guard, 5 files) and group 2 (tasks 5.4 to 5.6, 3 files).

### 2026-10-16 18:50 (orchestrator)
- Phase 5 done: tasks 5.1 to 5.6. Verifier (`phase-5-verify.md`): PASS. Demo: PASS. 121 tests pass. `TreeGuardTests` 24 of 24, `TreeAllocationTests` 2 of 2 (0 bytes over 100 runs). `scripts/test-linux.sh` PASS. The STE lint gives 1.39 for `implementation.md` and 1.48 for `context.md` (limit 2.5).
- Verifier LOW note: section 5 of `docs/technical-design.md` names `Class(string)` and `Child(Element)`, but not `Class(ClassId)`. It goes to the cross-phase review.

### 2026-10-16 18:25 (orchestrator, `/update-feature`)
- The session note times above are approximate. The system clock read 18:24 at this note.
- Cross-phase review (`code-review.md`): 11 findings, 1 MEDIUM (CR-1, the clear seam for the layout pass) and 10 LOW. Ten fix groups (`review-g1.md` to `review-g10.md`) fixed CR-1 to CR-10. The orchestrator fixed CR-11 in this file and in `docs/features/core-tree/human-testing.md`. Verifier `review-verify.md`: PASS, with 126 tests in three runs and `scripts/test-linux.sh` PASS.
- The review probe ran 240,000 random public calls and found no break of the tree rules or of the dirty-bit invariant. CR-7 added a seeded test of 10,000 calls (seed 20260416) to `DirtyTrackingTests`.
- The solution-architect wrote the draft plan `docs/features/core-tree/flexbox-layout/implementation.md` (10 phases, 88 tasks) with four open scope questions in its Notes. It received plan Notes item 4 and the root-size gap of CR-1.
- Inbox: no line in the run. Nothing to promote.
- All 35 tasks are done. Status: Ready for Review.

## Next Steps

1. A person runs the items of `## Needs Human Verification`. The Windows run (Q7) is the only check that no agent can run.
2. The caller commits the work as `feat: core-tree/element-tree`.
3. `/autorun-epic` asks the four open scope questions of the flexbox-layout draft, then builds core-tree/flexbox-layout.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
