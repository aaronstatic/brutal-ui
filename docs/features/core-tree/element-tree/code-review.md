# core-tree/element-tree - Code Review

**Reviewed:** 2026-10-16
**Reviewer:** Agent (cross-phase review)
**Rules Applied:** Tree correctness and the dirty-bit invariant, zero allocation after warm-up, API clarity and section 5 of the technical design, the sibling seams, `AGENTS.md`, ASD-STE100, test quality. The Vercel React rules do not apply to C#.
**Status:** Resolved - 11 findings, 11 resolved (verifier `review-verify.md` PASS on 2026-10-16)

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 1 |
| LOW | 10 |

The tree core is correct. A probe program in the scratch folder (outside the repository) ran 240,000 random public calls over 6 seeds, on 24 elements and 2 contexts. After each call, it checked the parent links, the child lists, the cycle rule, the root flags, and the dirty-bit invariant. It also compared the full state before and after each of the 90,609 failed calls. It found 0 failures. A second probe measured 0 bytes over 1000 warm runs of the reparent path, `AddRoot`, `RemoveRoot`, `Class(string)` on a known name, `foreach`, `IndexOf`, `Contains`, a visitor that marks another kind, and `Update`.

The findings are about the seam for the layout pass, the edges of the API, two doc lines, and gaps in the tests.

### Known items from the build

1. **`scripts/test-linux.sh` writes no name of a passed test.** Confirmed. `artifacts/test-linux/test.log` holds 0 matches for `ElementListTests` or `DirtyTrackingTests`. It holds only the counts (`total: 121`, `failed: 0`). The demo lines of phases 2 to 5 have the same gap, because they read the same `dotnet test` output. LOW, see CR-11. The script fix goes to a later dev-ops feature.
2. **`GenerateDocumentationFile` repeats in `BrutalUi.Core.csproj`.** Confirmed as a fact. `src/Directory.Build.props` sets it for each library, and the project file sets it again with a comment. Rejected as a finding. The repeat has no effect on the build, Decision 8 asks for it, and the comment gives the reason.
3. **Section 5 of the technical design omits `Class(ClassId)`.** Confirmed. See CR-6.
4. **The root guard.** Confirmed. `Element.IsRoot` is an internal `bool`, so the element holds no context reference, and A4 holds. `RemoveRoot` clears the flag at `UiContext.cs:69`, only when the context holds the root. The probe found the flag equal to "the element is in the `Roots` of a context" after each of the 240,000 calls. One edge stays open. See CR-3.

---

## Findings

### CR-1: The clear seam forbids the use that Plan Notes item 4 needs
**Severity:** MEDIUM
**Rule:** 4. The seams for the siblings
**File:** `src/BrutalUi.Core/Tree/Element.cs:190-200`
**Problem:** Plan Notes item 4 says: a layout pass that runs its own recursion must clear the bits that it skips. The internal surface has the tools for it: `ChildStorage`, `Dirty`, and `ClearDirty`. But the doc comment of `ClearDirty` forbids that use:

```csharp
/// Clears dirty bits on this element only. Only the dirty walk calls it.
/// A clear outside the walk can break the invariant. The walk clears an element after its subtree.
internal void ClearDirty(DirtyFlags bits)
```

The flexbox-layout agent then has two bad choices: break the documented rule, or put the pass on `DirtyWalk.Walk`. The walk does not fit a flex pass. `WalkElement` calls the visitor only for an element with a self bit (`DirtyWalk.cs:69-72`):

```csharp
if (self != DirtyFlags.None)
{
    visitor.Visit(element, self);
}
```

A parent with only `DescendantLayout` gets no visit. But a flex parent must distribute the space again when a child changes size. The visit is also pre-order only, and a visitor cannot tell the walk to skip the children. Thus a pass on the walk alone can give stale layout with no error. The Quality Bar names this risk first.

A second gap: `UiContext` holds no available size for a root, so the layout pass has no input rect. The flexbox-layout feature must add one. Decision 9 already names `UiContext.CreateInfo` for the first setting.
**Fix:** Rewrite the remarks of `ClearDirty` with the rule for a pass with its own recursion: "A pass with its own recursion can call this method. On each element that the pass enters, clear the self bit and the descendant bit of the walked kind, after the subtree of the element. Do not clear another kind." In `DirtyWalk.cs`, add one remark to `IDirtyVisitor`: "The walk calls the visitor only for an element with a self bit. A pass that must also run on an element with only a descendant bit uses its own recursion and `ClearDirty`, or calls `Walk` with `ClearVisitor` on the subtree after the pass." Record the root-size gap next to Notes item 4 for the flexbox-layout plan.
**Effort:** Low

### CR-2: A span from the child list can read the children of another element
**Severity:** LOW
**Rule:** 2. Zero allocation after warm-up (`ArrayPool` misuse: a rented array that leaks to a caller)
**File:** `src/BrutalUi.Core/Tree/ElementList.cs:13-16`
**Problem:** `GetEnumerator` (line 196) and `UiContext.Roots` give the caller a span on a rented array. A growth returns that array to `ArrayPool<Element>.Shared`. The next `Rent` of the same bucket on the same thread gets the same array back. The probe changed a 16-child list `a` during a `foreach`. It added one child to `a`, and then 4 children to an empty element `b`. The `foreach` then read `a0,b1,b2,b3,null,null`, so it read the children of `b`.

The Risk table of the plan states the opposite (`implementation.md:411`): "The span enumerator reads the old array, which `clearArray: true` sets to `null`, so a read gives `null`, not another tree." Notes item 9 (no version check) rests on that claim. The code doc says only "A change can move the items or replace the array." The change is a caller error, and the doc forbids it, so the severity is LOW. But the plan states a false fact.
**Fix:** In the remarks of `ElementList`, add: "After a growth, the enumerator can read null or the children of another element." Add the same sentence to the remarks of `UiContext.Roots`. In `implementation.md:411`, replace "so a read gives `null`, not another tree" with "so a read gives `null` or an element of another list that rents the same array". Keep the decision of Notes item 9 until a real defect shows.
**Effort:** Low

### CR-3: A root flag stays set after its context is gone
**Severity:** LOW
**Rule:** 1. Correctness of the tree (the root guard)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:8-15`
**Problem:** Only `RemoveRoot` clears `Element.IsRoot`, and a caller cannot read the flag, because it is internal. When a game drops a context and keeps its root element, the element can never join a child list or another context:

```csharp
UiContext dropped = new();
dropped.AddRoot(hud);
dropped = null;
new Element().Children.Add(hud);   // InvalidOperationException: "Remove it from its UiContext first."
```

The probe confirmed the throw. The message tells the caller to remove the element from its context, but the caller has no context to call. The class remarks say "Remove a root from its context before you add it to another context or to a child list." They do not say that the flag outlives the context.
**Fix:** Add to the class remarks of `UiContext`: "A root keeps its root flag until `RemoveRoot`. Call `RemoveRoot` on each root before you drop a context." Add the same rule to the `<exception cref="InvalidOperationException">` docs of `Add` and `Insert` in `ElementList.cs`. When a later epic gives `UiContext` a `Dispose` or a `ClearRoots` method, let that method clear the flag of each root.
**Effort:** Low

### CR-4: `ClassList.Add` accepts `ClassId.None`
**Severity:** LOW
**Rule:** 3. API clarity (a wrong call throws at once with the argument name)
**File:** `src/BrutalUi.Core/Tree/ClassList.cs:32-48`
**Problem:** `ClassId.None` is the default value, and it stands for no class. `Add` stores it and marks the owner for style:

```csharp
public bool Add(ClassId id)
{
    if (!_owner.ClassStorage.Add(id))
```

The probe got `true` from `Add(ClassId.None)`, with a count of 1. A `ClassId` field that the caller forgot to intern gives this result with no error. A later selector match can then match a class that has no name. The indexer (line 34) also has no `<exception>` doc, but the `ElementList` indexer has one for the same throw.
**Fix:** At the start of `Add`, add `if (id.IsNone) throw new ArgumentException("The id is ClassId.None. Get an id from ClassName.Intern.", nameof(id));`, with an `<exception cref="ArgumentException">` doc. Keep `Remove` and `Contains` as they are, because they return `false` for `None`. Add `<exception cref="ArgumentOutOfRangeException">` to the indexer. Add a test to `TreeGuardTests.cs` that checks the `ParamName` `id`.
**Effort:** Low

### CR-5: The builder gives a `NullReferenceException` for a null element
**Severity:** LOW
**Rule:** 3. API clarity (each exception names the bad argument)
**File:** `src/BrutalUi.Core/Tree/ElementBuilder.cs:17-46`
**Problem:** The three extension methods do not check `element`. The probe call `((Element)null).Class("x")` throws `NullReferenceException` with no parameter name. A null element is a likely result of a failed lookup in a chain. The methods also have no `<exception>` docs. But `Child` throws `ArgumentNullException` and `InvalidOperationException` through `Add`, and `Class(string)` throws `ArgumentNullException` and `ArgumentException` through `Intern`.
**Fix:** Add `ArgumentNullException.ThrowIfNull(element);` as the first line of each method. Add an `<exception>` doc for each exception that a method can throw. Add a test to `TreeGuardTests.cs` that checks the `ParamName` `element`.
**Effort:** Low

### CR-6: Section 5 of the technical design omits `Class(ClassId)`
**Severity:** LOW
**Rule:** 3. API clarity and section 5 of the technical design
**File:** `docs/technical-design.md:223`
**Problem:** The line says: "`ElementBuilder` adds the extension methods `Class(string)` and `Child(Element)` for a fluent build." The code ships three methods (`ElementBuilder.cs:17`, `:29`, and `:41`), and the Public API table of the plan lists all three. Integration criterion I4 asks section 5 to match that table.
**Fix:** Change the sentence to: "`ElementBuilder` adds the extension methods `Class(string)`, `Class(ClassId)`, and `Child(Element)` for a fluent build."
**Effort:** Low

### CR-7: No test covers a visitor that marks another kind or the invariant over many calls
**Severity:** LOW
**Rule:** 6. Test quality (a missing test for a rule of the plan)
**File:** `tests/BrutalUi.Core.Tests/DirtyTrackingTests.cs:197-289`
**Problem:** The section "Dirty Tracking" of the plan gives the visitor rule: "It can mark another kind." The style pass and the layout pass depend on that rule. The walk tests use only `RecordingVisitor`, which marks nothing. Also, each test checks the bits after one change. No test runs a long mixed sequence and checks the invariant after each call. The probe did both and found no failure, so the code is correct today. But a later change to the walk or to the reparent path has no guard.
**Fix:** Add a `MarkPaintVisitor` struct. Its `Visit` calls `element.MarkDirty(DirtyFlags.Paint)` and marks the first child for `Paint`. Walk `Layout` on a tree with a dirty leaf, and assert the exact flags of each element: no `Layout` bit stays, and each ancestor of a `Paint` element holds `DescendantPaint`. Then add an `AssertInvariant(Element root)` helper and one seeded test. The test runs about 10,000 random calls (add, insert, remove, move, `AddRoot`, `RemoveRoot`, the class calls, `States`, `MarkDirty`, and `Update`) and calls the helper after each call.
**Effort:** Medium

### CR-8: The allocation test does not measure the reparent path
**Severity:** LOW
**Rule:** 6. Test quality
**File:** `tests/BrutalUi.Core.Tests/TreeAllocationTests.cs:89-119`
**Problem:** The body removes a child and adds it back, so `Add` always gets a child with no parent. Thus `LeaveParent` and the reparent of A3 never run in the measured region. `AddRoot` and `RemoveRoot` do not run either, but they use the same `ElementStorage`. The Quality Bar names "add" with no limit, and A3 makes the reparent part of each add. The probe measured 0 bytes on these paths, so the gap is in the guard only.
**Fix:** In `Step`, take `Element moved = root.Children[2].Children[1];`. Call `root.Children[5].Children.Add(moved);` and then `root.Children[2].Children.Insert(1, moved);`. Add `AddRoot` and `RemoveRoot` of a spare root on the context. Keep each list inside its warm capacity. Add the matching count checks to `TheMeasuredBodyKeepsTheTreeShape`.
**Effort:** Low

### CR-9: The `ref` slot tests pass for one shared slot
**Severity:** LOW
**Rule:** 6. Test quality (a test that cannot fail)
**File:** `tests/BrutalUi.Core.Tests/ElementStateTests.cs:96-110`
**Problem:** Each test compares two refs from one element:

```csharp
Assert.True(Unsafe.AreSame(ref element.Style, ref element.Style));
```

A `Style` property that returns a ref to one static field for all elements also passes. The structs hold no field yet, so no other test can find that defect. The phase 4 acceptance criterion asks for a ref to the inline field of each element.
**Fix:** In both tests, make a second element. Add `Assert.False(Unsafe.AreSame(ref first.Style, ref second.Style));`, and the same line for `Layout`.
**Effort:** Low

### CR-10: The class table tests skip a parameter name and can hang on a cancel
**Severity:** LOW
**Rule:** 6. Test quality
**File:** `tests/BrutalUi.Core.Tests/ClassListTests.cs:59-104`
**Problem:** `GetNameThrowsForAnIdThatTheTableDoesNotHold` (lines 59 to 65) checks the exception type only. It does not check the `ParamName` `id`, but `TreeGuardTests` checks the name of each other throw. In the four-thread test (lines 86 to 104), `barrier.SignalAndWait()` has no token and no timeout. When the run cancels the token before a task starts, `StartNew` does not run that task, and the started tasks wait at the barrier with no end. A normal run does not flake, because `LongRunning` gives each task its own thread.
**Fix:** Assert `Assert.Equal("id", ex.ParamName)` for each of the three calls. Read `CancellationToken token = TestContext.Current.CancellationToken;` before the loop, and call `barrier.SignalAndWait(token)` in the task.
**Effort:** Low

### CR-11: The human demo lines name tests that the logs never print
**Severity:** LOW
**Rule:** 6. Test quality (the demo check)
**File:** `docs/features/core-tree/element-tree/context.md:161-165`
**Problem:** This is known item 1. The phase 1 line asks a person to see "the `ElementListTests` and `DirtyTrackingTests` results with 0 failed in `artifacts/test-linux/test.log`". `scripts/test-linux.sh:148` runs `dotnet test --solution brutal-ui.sln`, and Microsoft.Testing.Platform prints no name of a passed test. The current log holds 0 matches for the two class names. The lines of phases 2 to 5 read the same `dotnet test` output, so they have the same gap. `docs/features/core-tree/human-testing.md:10-14` holds the same five lines, so a person cannot pass them as written. The script is outside the scope of this feature.
**Fix:** Change each demo line to a check that a person can do. Read `failed: 0` in the output. Then run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class BrutalUi.Core.Tests.<ClassName>`, and see a count above 0 with 0 failed. Make the same change in `human-testing.md`. Record a note for a later dev-ops feature: give the test step of `scripts/test-linux.sh` a detailed output option.
**Effort:** Low

---

## What's Already Good

- The propagation sends the full set to each ancestor (`Element.cs:149-158`). A partial ancestor, with `DescendantStyle` but no `DescendantLayout`, does not stop a mark of another kind. `MarkOfAnotherKindOnASiblingLeafGoesUpToTheRoot` proves it.
- One path sends bits up. `MarkDirty` and `AttachToParent` both call `PropagateToAncestors`, and the reparent reuses `Children.Remove`. No second propagation path exists.
- Each throw runs before the first change. `ValidateChild` runs before `LeaveParent`, and `Insert` checks the index against the count after the remove. The probe confirmed the rule on 90,609 failed calls.
- The walk reads the flags one time before the visit and clears an element after its subtree. A visitor that marks another kind keeps the invariant (probe result).
- `Tree/` holds no boxing, no closure, no `params` array, and no `HasFlag`. The views are `readonly struct` types with one reference field, and the storage goes through `ref` returns, so no defensive copy exists. `ElementStorage` returns each old array one time, with a clear. `ClassSet` returns its old array with no clear, which is correct for a struct with no reference.
- `TreeAllocationTests` keeps each list inside its warm capacity, interns each name before the warm-up, and puts the `Assert` outside the measured region. The test rents no array in the body, so a pool trim cannot add a byte. No flaky risk shows.
- The tests assert exact `DirtyFlags` values and use no `HasFlag`. Two tests break the invariant on purpose with `ClearDirty`, so they prove the early stop and the skip, and not only the end state.
- `AGENTS.md`: each file has a file-scoped namespace, `BrutalUi.Core` has no ImGui reference and no new package, and Decision 9 covers the create-info rule. The prose lint gives 0.54 per 100 words on the source comments and 0.92 on the test comments (limit 2.5).
- The doc comments state each rule: no change during `foreach` (`ElementList`), main thread only (`Element`, `UiContext`), and the visitor rules (`IDirtyVisitor`).
- The seam comments in `UiContext.Update` match the plan word for word, with one clear pass after them.

---

## Recommended Fix Order

1. CR-1 (about 15 minutes). Do it before the flexbox-layout plan starts, because that plan must state its choice for Notes item 4.
2. CR-6 and CR-11 (about 10 minutes together). Each is a doc line.
3. CR-3, CR-4, and CR-5 (about 45 minutes together). Each is an API edge with a guard test.
4. CR-2 (about 10 minutes). It is a doc and plan correction.
5. CR-7 (about 1 hour), then CR-8, CR-9, and CR-10 (about 30 minutes together). Each adds or tightens a test.

---

## Progress

- [x] CR-1: The clear seam forbids the use that Plan Notes item 4 needs (`review-g1.md`. The root-size gap goes to the flexbox-layout plan, and `context.md` records it under Gotchas & Learnings.)
- [x] CR-2: A span from the child list can read the children of another element (`review-g6.md`, doc comments. The plan row `implementation.md:411` stays as written, because the called mode of `/autorun-feature` keeps the plan. `context.md` records the correction under Gotchas & Learnings.)
- [x] CR-3: A root flag stays set after its context is gone (`review-g3.md`)
- [x] CR-4: `ClassList.Add` accepts `ClassId.None` (`review-g4.md`)
- [x] CR-5: The builder gives a `NullReferenceException` for a null element (`review-g5.md`)
- [x] CR-6: Section 5 of the technical design omits `Class(ClassId)` (`review-g2.md`)
- [x] CR-7: No test covers a visitor that marks another kind or the invariant over many calls (`review-g7.md`)
- [x] CR-8: The allocation test does not measure the reparent path (`review-g8.md`)
- [x] CR-9: The `ref` slot tests pass for one shared slot (`review-g9.md`)
- [x] CR-10: The class table tests skip a parameter name and can hang on a cancel (`review-g10.md`)
- [x] CR-11: The human demo lines name tests that the logs never print (the orchestrator appended a "How to see the class names" line with the `--filter-class` check to `context.md` and `human-testing.md`. The demo lines stay word for word from the plan, and `human-testing.md` is append only. The script change goes to a later dev-ops feature.)

Verifier `review-verify.md`: PASS on 2026-10-16. Build 0 warnings, 126 tests pass (three runs), `scripts/test-linux.sh` PASS.
