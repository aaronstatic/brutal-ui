# core-tree/element-tree - Task Checklist

**Last Updated:** 2026-10-16 18:25
**Progress:** 35/35 tasks complete (100%)
**Advanced phases:** Phase 1 (ADVANCED, opus). Phases 2 to 5 are STANDARD (sonnet).

---

## Phase 1: The Tree, the Dirty Bits, and the Walk (10/10 complete)

**Demo:** the developer runs `scripts/test-linux.sh`, sees the line `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s`, and sees the `ElementListTests` and `DirtyTrackingTests` results with 0 failed in `artifacts/test-linux/test.log`.
**Needs:** none

Group A (src, 6 files):

- [x] ✅ **1.1 Write `Tree/ElementStorage.cs`**
  - Description: an internal struct with `Element[]? _items` and `int _count`. Members: `Count`, `AsSpan()` (a `ReadOnlySpan<Element>` of `_count` items, empty when `_items` is null), `Add(Element)`, `RemoveAt(int)` (shift down, set the free slot to `null`), `IndexOf(Element)` (reference compare). Growth per Key Components: `Rent(4)` on the first add, then `Rent(_items.Length * 2)`, copy, `Return(old, clearArray: true)`. No shrink.
  - File(s): `src/BrutalUi.Core/Tree/ElementStorage.cs`
  - Estimate: 6 steps

- [x] ✅ **1.2 Write `Tree/DirtyFlags.cs`**
  - Description: the public `[Flags]` enum of the Public API table, with a doc comment on each member. In the same file, add an internal static class `DirtyBits` with `const DirtyFlags SelfMask`, `const DirtyFlags DescendantMask`, and `static DirtyFlags ToDescendant(DirtyFlags self)` (shift left by 3).
  - File(s): `src/BrutalUi.Core/Tree/DirtyFlags.cs`
  - Estimate: 4 steps

- [x] ✅ **1.3 Write `Tree/Element.cs` (part 1)**
  - Description: the fields `ElementStorage _children`, `DirtyFlags _dirty`, `Element? _parent`. Members: `Element()` (sets `Style | Layout | Paint`), `Name`, `Parent`, `Children`, `Dirty`, and `MarkDirty(DirtyFlags)` per the section "Dirty Tracking". Add an internal method `PropagateToAncestors(DirtyFlags descendantBits)` that runs step 3 of `MarkDirty` from the parent up. `MarkDirty` and the add path both use it.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 6 steps

- [x] ✅ **1.4 Write `Tree/ElementList.cs` (part 1)**
  - Description: the public readonly struct with `Element _owner`. Members: `Count`, the indexer, `Add`, `Remove(Element)`, `RemoveAt(int)`, `IndexOf`, `Contains`, `GetEnumerator()` (returns `_owner` storage `AsSpan().GetEnumerator()`). `Add` throws `InvalidOperationException` when the child has a parent (phase 2 replaces this with the reparent). `Add` sets the parent and applies the "Add" row of the structural marks. `Remove` and `RemoveAt` clear the parent and mark the owner for layout. The doc comment says: do not change the list during a `foreach`.
  - File(s): `src/BrutalUi.Core/Tree/ElementList.cs`
  - Estimate: 6 steps

- [x] ✅ **1.5 Write `Tree/DirtyWalk.cs`**
  - Description: the internal `IDirtyVisitor` with `void Visit(Element element, DirtyFlags flags)`, the internal static `DirtyWalk.Walk<TVisitor>` per the section "Dirty Tracking", and the internal struct `ClearVisitor` with an empty `Visit`. Read the children through the storage span. Do not use `foreach` on `ElementList` inside the walk (direct span index, no extra copy).
  - File(s): `src/BrutalUi.Core/Tree/DirtyWalk.cs`
  - Estimate: 6 steps

- [x] ✅ **1.6 Write `Tree/UiContext.cs`**
  - Description: the public sealed class with `ElementStorage _roots`. Members: `UiContext()`, `Roots`, `AddRoot(Element)` (throws `InvalidOperationException` when the element has a parent, phase 2 replaces it), `RemoveRoot(Element)` (returns `false` when the element is not a root), and `Update(float)` with the exact seam comments of the section "The Pass Seam in `UiContext.Update`". The class doc comment says: the tree is main-thread only (A6), and an element is a root of one context at most (A4).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

Group B (project and tests, 4 files):

- [x] ✅ **1.7 Edit `BrutalUi.Core.csproj`**
  - Description: add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to the property group, and an item group with `<InternalsVisibleTo Include="BrutalUi.Core.Tests" />`. Add no `PackageReference`.
  - File(s): `src/BrutalUi.Core/BrutalUi.Core.csproj`
  - Estimate: 3 steps

- [x] ✅ **1.8 Delete `PlaceholderTests.cs` and write `ElementListTests.cs`**
  - Description: Delete `tests/BrutalUi.Core.Tests/PlaceholderTests.cs` (A9). Write `ElementListTests.cs`: add one child (count, index, parent), add three and read the order, remove by reference (count, parent is null, the order of the rest), `RemoveAt`, `Remove` of a non-child returns `false`, 40 adds keep the order (two growths past the 16-slot bucket), `foreach` visits each child in order, `IndexOf` and `Contains`.
  - File(s): `tests/BrutalUi.Core.Tests/PlaceholderTests.cs` (delete), `tests/BrutalUi.Core.Tests/ElementListTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.9 Write `DirtyTrackingTests.cs`**
  - Description: Use an internal test visitor struct that records each visited element in a `List<Element>` (test code can allocate). Tests: a new element has `Style | Layout | Paint`. A mark on a leaf at depth 4 sets the self bit on the leaf and the descendant bit on each ancestor up to the root, and nothing more. A second mark on a sibling leaf stops at the shared parent (the root flags stay the same). `UiContext.Update` clears each bit of each element. The walk visits the dirty leaf and its path, and visits no element of a clean sibling subtree (the recorder holds no element of that subtree). `Remove` marks the old parent for layout. A mark on an element of a removed subtree does not reach the old parent.
  - File(s): `tests/BrutalUi.Core.Tests/DirtyTrackingTests.cs`
  - Estimate: 8 steps

- [x] ✅ **1.10 Build, test, and run the demo**
  - Description: Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 warnings and 0 failed. Then run the demo with `scripts/test-linux.sh`, and `grep -E 'ElementListTests|DirtyTrackingTests|Passed|Failed' artifacts/test-linux/test.log`. Record the results in `context.md`.
  - File(s): `docs/features/core-tree/element-tree/context.md`
  - Estimate: 4 steps

---

## Phase 2: Reorder and Reparent (6/6 complete)

**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees the reorder and reparent tests of `ElementListTests` and `DirtyTrackingTests` pass with 0 failed.
**Needs:** Phase 1

- [x] ✅ **2.1 Add `Insert` and `Move` to `ElementStorage`**
  - Description: Add `Insert(int index, Element)` and `Move(int fromIndex, int toIndex)` to `ElementStorage`. Both shift in place with `Span<Element>.CopyTo` on the overlap-safe range. `Insert` grows per task 1.1.
  - File(s): `src/BrutalUi.Core/Tree/ElementStorage.cs`
  - Estimate: 5 steps

- [x] ✅ **2.2 Add `Insert` and `Move` to `ElementList`**
  - Description: Add `Insert` and `Move` to `ElementList`. `Move` marks the owner for layout. `Move` with the same two indexes changes nothing and marks nothing.
  - File(s): `src/BrutalUi.Core/Tree/ElementList.cs`
  - Estimate: 5 steps

- [x] ✅ **2.3 Reparent in `Add` and `Insert`**
  - Description: Replace the phase 1 throw in `Add` and `Insert` with the reparent of A3. When the child has a parent, remove it from that list first (the "Remove" row), then add it (the "Add" row). When the old parent is the owner, the index of `Insert` refers to the list after the remove (valid range 0 to `Count` after the remove). `Add` of a child of the same owner moves it to the end.
  - File(s): `src/BrutalUi.Core/Tree/ElementList.cs`
  - Estimate: 6 steps

- [x] ✅ **2.4 Reparent in `UiContext.AddRoot`**
  - Description: In `UiContext.AddRoot`, replace the phase 1 throw: an element with a parent leaves its parent first (the old parent gets a layout mark), then it becomes a root.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 3 steps

- [x] ✅ **2.5 Add the reorder and reparent tests**
  - Description: Add tests. `ElementListTests`: insert at 0, at the middle, and at `Count`. Move down, move up, move to the same index. Reparent from parent A to parent B (both counts, the new parent, A no longer lists the child). Add of an existing child of the same owner moves it to the end. Insert of an existing child of the same owner at index 0. `AddRoot` of a child removes it from its parent. `DirtyTrackingTests`: after a clean `Update`, a reparent marks A and B for layout, marks the child for style, and gives each ancestor of A and B the descendant layout bit. A dirty subtree that joins a clean tree gives the new ancestors its descendant bits. A move marks the owner for layout only.
  - File(s): `tests/BrutalUi.Core.Tests/ElementListTests.cs`, `tests/BrutalUi.Core.Tests/DirtyTrackingTests.cs`
  - Estimate: 10 steps

- [x] ✅ **2.6 Build and test**
  - Description: Run the build and the tests as in task 1.10 (no script run). Record the results in `context.md`.
  - File(s): `docs/features/core-tree/element-tree/context.md`
  - Estimate: 3 steps

---

## Phase 3: The Class List (7/7 complete)

**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ClassListTests` pass with 0 failed, with the test that adds a ninth class and reads all nine back.
**Needs:** Phase 1

- [x] ✅ **3.1 Write `Tree/ClassId.cs`**
  - Description: the public readonly struct of the Public API table. `ToString()` returns `ClassName.GetName(this)`, or `"(none)"` for `None`.
  - File(s): `src/BrutalUi.Core/Tree/ClassId.cs`
  - Estimate: 4 steps

- [x] ✅ **3.2 Write `Tree/ClassName.cs`**
  - Description: the public static class. Fields: `static readonly Lock s_lock`, `Dictionary<string, ClassId>` (ordinal compare), `List<string>` with a placeholder at index 0. `Intern` returns the existing id, or adds the name and returns the next id. `GetName` returns the name, and throws `ArgumentOutOfRangeException` for an id that the table does not hold. Both lock (A6).
  - File(s): `src/BrutalUi.Core/Tree/ClassName.cs`
  - Estimate: 5 steps

- [x] ✅ **3.3 Write `Tree/ClassSet.cs`**
  - Description: the internal `[InlineArray(8)] struct ClassIdBuffer` and the internal struct `ClassSet` with `ClassIdBuffer _inline`, `ClassId[]? _overflow`, `int _count`. Members: `Count`, an indexer, `Contains`, `Add` (returns `false` for a duplicate), `Remove` (shift down, keep the order, returns `false` when absent). The ninth add rents `ArrayPool<ClassId>.Shared.Rent(16)`, copies the 8 inline ids, and uses the array from then on. A full array grows per task 1.1 with `clearArray: false`. The set stays on the array after a remove below 8 (Decision 6).
  - File(s): `src/BrutalUi.Core/Tree/ClassSet.cs`
  - Estimate: 7 steps

- [x] ✅ **3.4 Write `Tree/ClassList.cs`**
  - Description: the public readonly struct view with `Element _owner`. `Add` and `Remove` mark the owner for style only when the set changes.
  - File(s): `src/BrutalUi.Core/Tree/ClassList.cs`
  - Estimate: 4 steps

- [x] ✅ **3.5 Add `Classes` to `Element`**
  - Description: Edit `Tree/Element.cs`: add the field `ClassSet _classes` and the property `ClassList Classes`.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 2 steps

- [x] ✅ **3.6 Write `ClassListTests.cs`**
  - Description: Use unique names for each test (for example a `nameof` prefix), because the table is global and xUnit runs test classes in parallel. Tests: `Intern` of the same name gives the same id, of two names gives two ids. `GetName` returns the name. `ClassId.None` is the default and `IsNone`. Four threads intern the same 100 names, and each name gets one id. Add, contains, remove. A duplicate add returns `false` and marks nothing (check after a clean `Update`). A remove of an absent id returns `false`. Nine adds keep all nine in order. A remove from the overflow keeps the order. A remove down to 3 and an add back to 12 keep the order.
  - File(s): `tests/BrutalUi.Core.Tests/ClassListTests.cs`
  - Estimate: 8 steps

- [x] ✅ **3.7 Build and test**
  - Description: Run the build and the tests as in task 2.6. Record the results in `context.md`.
  - File(s): `docs/features/core-tree/element-tree/context.md`
  - Estimate: 3 steps

---

## Phase 4: Element State, the Slots, and the Fluent Builder (6/6 complete)

**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ElementStateTests` pass with 0 failed, with the test that builds a three-level tree in one builder expression.
**Needs:** Phase 3

- [x] ✅ **4.1 Write `Tree/PseudoStates.cs`**
  - Description: the public `[Flags]` enum of the Public API table.
  - File(s): `src/BrutalUi.Core/Tree/PseudoStates.cs`
  - Estimate: 2 steps

- [x] ✅ **4.2 Write `Tree/ComputedStyle.cs` and `Tree/LayoutNode.cs`**
  - Description: two public structs with no fields (A7, A8). Each doc comment names the feature that adds the fields: core-tree/flexbox-layout for both, and the style-system epic for the paint fields of `ComputedStyle`.
  - File(s): `src/BrutalUi.Core/Tree/ComputedStyle.cs`, `src/BrutalUi.Core/Tree/LayoutNode.cs`
  - Estimate: 3 steps

- [x] ✅ **4.3 Add state and slots to `Element`**
  - Description: Edit `Tree/Element.cs`: add the fields `ComputedStyle _style`, `LayoutNode _layout`, `PseudoStates _states`. Add `ref ComputedStyle Style => ref _style;` and `ref LayoutNode Layout => ref _layout;`. Add `States` with a setter that marks style only when the value changes. Add `IsPickable` with the default `true` (A5) and no mark.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 4 steps

- [x] ✅ **4.4 Write `Tree/ElementBuilder.cs`**
  - Description: the public static class with the three generic extension methods of the Public API table. `Class(string)` calls `ClassName.Intern`, then `Classes.Add`. `Child` calls `Children.Add`. Each returns the element of the call, so a later derived type (`Label`) keeps its type in a chain.
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.cs`
  - Estimate: 4 steps

- [x] ✅ **4.5 Write `ElementStateTests.cs`**
  - Description: Tests: a new element has `States == None` and `IsPickable == true`. `States |= Hover` marks style after a clean `Update`. A set to the same value marks nothing. `IsPickable = false` marks nothing. `Unsafe.AreSame(ref e.Style, ref e.Style)` and the same for `Layout` are `true`. A builder expression `new Element { Name = "root" }.Class("a").Child(new Element().Class("b").Child(new Element())).Child(new Element())` gives the expected names, parents, classes, and counts.
  - File(s): `tests/BrutalUi.Core.Tests/ElementStateTests.cs`
  - Estimate: 6 steps

- [x] ✅ **4.6 Build and test**
  - Description: Run the build and the tests as in task 2.6. Record the results in `context.md`.
  - File(s): `docs/features/core-tree/element-tree/context.md`
  - Estimate: 3 steps

---

## Phase 5: Guards, the Allocation Check, and the Design Doc (6/6 complete)

**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees `TreeGuardTests` and `TreeAllocationTests` pass with 0 failed.
**Needs:** Phase 4

- [x] ✅ **5.1 Add the cycle guard**
  - Description: Add the cycle guard to `ElementList.Add` and `Insert`: walk up from the owner. When the child is the owner or an ancestor of the owner, throw `InvalidOperationException` before any change. Add the same guard to `ElementBuilder.Child` through `Add` (no extra code).
  - File(s): `src/BrutalUi.Core/Tree/ElementList.cs`
  - Estimate: 4 steps

- [x] ✅ **5.2 Add the argument checks**
  - Description: `ArgumentNullException.ThrowIfNull` for each `Element` argument of `ElementList` and `UiContext`, and for the name of `ClassName.Intern`. `ArgumentException` for an empty name. `ArgumentOutOfRangeException` (with the parameter name) for each index of the indexer, `RemoveAt`, `Insert`, and `Move`. `UiContext.AddRoot` of an element that is already a root of this context throws `InvalidOperationException`. Each check runs before any change.
  - File(s): `src/BrutalUi.Core/Tree/ElementList.cs`, `src/BrutalUi.Core/Tree/UiContext.cs`, `src/BrutalUi.Core/Tree/ClassName.cs`
  - Estimate: 6 steps

- [x] ✅ **5.3 Write `TreeGuardTests.cs`**
  - Description: add of the element to itself, add of the parent to its child, add of the root to a leaf at depth 3 (each throws, and the tree, the counts, and the flags do not change). Each null and each bad index throws the named exception with the expected `ParamName`. A duplicate `AddRoot` throws. `ClassName.Intern(null!)` and `Intern("")` throw.
  - File(s): `tests/BrutalUi.Core.Tests/TreeGuardTests.cs`
  - Estimate: 6 steps

- [x] ✅ **5.4 Write `TreeAllocationTests.cs`**
  - Description: Build a tree: one root, 8 children, each with 8 children. Give one element 12 classes (overflow) and one 4 classes (inline). Add the root to a `UiContext`. Warm up: run the measured body two times. Measure: read `GC.GetAllocatedBytesForCurrentThread()`, run the body 100 times, read again, and `Assert.Equal(0, after - before)` outside the measured region. The body: remove and add back a child, insert at 0 and remove at 0, move first to last and back, remove and add back one class on each class element, set and clear `Hover`, `MarkDirty(Layout)` on a leaf, and `Update(0.016f)`. Keep each list inside its warm capacity. Keep the test synchronous.
  - File(s): `tests/BrutalUi.Core.Tests/TreeAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **5.5 Update section 5 of the technical design**
  - Description: Edit section 5 of `docs/technical-design.md`: update the code block of the shape to the end-state API (`IsPickable` in place of `PickingEnabled`, `States { get; set; }`, `ElementList` and `ClassList` as views, `ClassId`, `MarkDirty`). Add one design decision line on the two-bit dirty flags. Use ASD-STE100 prose.
  - File(s): `docs/technical-design.md`
  - Estimate: 4 steps

- [x] ✅ **5.6 Final build, test, and verification**
  - Description: Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30`, `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`, and `scripts/test-linux.sh`. Run each non-MANUAL step of the Verification Method. Add the Windows run (Verification step 9) to "Needs Human Verification" in `context.md`.
  - File(s): `docs/features/core-tree/element-tree/context.md`
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- none

**Fixed Bugs:**
- none

---

## Technical Debt

- none

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Progress Tracking

### Completed This Session (2026-10-16)
- ✅ Dev docs scaffolded
- ✅ Phase 1 (tasks 1.1 to 1.10), verifier PASS, demo PASS
- ✅ Phase 2 (tasks 2.1 to 2.6), verifier PASS, demo PASS
- ✅ Phase 3 (tasks 3.1 to 3.7), verifier PASS, demo PASS
- ✅ Phase 4 (tasks 4.1 to 4.6), verifier PASS, demo PASS
- ✅ Phase 5 (tasks 5.1 to 5.6 and the root guard), verifier PASS, demo PASS
- ✅ Cross-phase review: 11 findings (1 MEDIUM, 10 LOW), all fixed, verifier `review-verify.md` PASS

### Discovered New Tasks
- The root guard (an internal `Element.IsRoot` flag), added to the phase 5 brief by the orchestrator from the phase 1 open question "A root that is also a child". Done in phase 5 group 1.

### Blocked Items
- none

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
