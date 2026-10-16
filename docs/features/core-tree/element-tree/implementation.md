# Element Tree - Implementation Plan

**Epic:** core-tree (feature 1 of 3)
**Status:** Ready for Review
**Started:** 2026-10-16
**Target Completion:** 2026-10-16
**Last Updated:** 2026-10-16 18:25

---

## Quality Bar

**Target quality:** Production-grade library code. Every later layer of brutal-ui reads and writes this tree: the layout pass, the style pass, the renderer, the input router, the framework, and the debugger. A defect here spreads to each of them. A silent defect in the dirty bits costs the most, because a skipped subtree shows stale layout with no error.

- **Reliability:** The tree never holds a cycle, a child with two parents, or a parent that does not list its child. A failed call changes nothing. The dirty-bit invariant (section "Dirty Tracking") holds after each public call.
- **Zero allocation:** After warm-up, add, insert, remove, move, class add, class remove, `MarkDirty`, and `UiContext.Update` allocate no managed memory. A test in this feature measures it. The allocation-guard feature measures `Update` on a 1000-element tree later.
- **API clarity:** The public shape matches section 5 of `docs/technical-design.md`. Each public member has an XML doc comment. The compiler enforces the doc comments (CS1591 is an error). An exception names the bad argument.
- **Seams for the siblings:** The flexbox-layout feature adds its pass to `UiContext.Update` with one call and no rewrite. It adds fields to `LayoutNode` and `ComputedStyle` with no change to `Element`.

---

## Executive Summary

This feature delivers the retained element tree of `BrutalUi.Core`: the `Element` class, a pooled child list, an inline class set with interned class ids, pseudo-states, a picking flag, and the inline `ComputedStyle` and `LayoutNode` slots. Each element carries two dirty bits for each kind (style, layout, paint): one for the element and one for a dirty descendant. A minimal `UiContext` owns the root list. `UiContext.Update` walks the dirty subtrees only and clears the bits. The flexbox-layout feature then plugs its layout pass into the same walk. The code is about 600 lines in `src/BrutalUi.Core/Tree/`, with xUnit tests in `tests/BrutalUi.Core.Tests/`.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (builds a screen in C#, adds and removes elements at run time, and needs no frame-time spike)
- [x] **The sibling feature agent** (the flexbox-layout and allocation-guard agents build on the tree, the dirty walk, and the slots)
- [ ] **The markup author** (not served yet. The markup epic builds elements from XML on the same API later)

**User Subsets Served:**
- [x] **Simple** - The fluent builder (`Class`, `Child`) builds a small tree in one expression.
- [x] **Complex** - Direct `ref` access to `ComputedStyle` and `LayoutNode`, an explicit `MarkDirty`, and the `Dirty` flags for a custom tool.

**UX Approach:**
- The developer writes `new Element { Name = "hud" }.Class("hud").Child(new Element())`.
- The developer calls `UiContext.Update(deltaTime)` each frame. A static screen costs one cheap check per root.
- A wrong call (a cycle, a bad index, a null) throws at once with the argument name.

---

## Goals

### Primary Goals
1. Ship `Element`, `ElementList`, `ClassList`, `ClassId`, `ClassName`, `PseudoStates`, `DirtyFlags`, `ComputedStyle`, `LayoutNode`, the fluent builder, and `UiContext` in `BrutalUi.Core`.
2. Make add, insert, remove, move, and the class operations allocation-free after warm-up.
3. Give the flexbox-layout feature a dirty walk and a pass seam in `UiContext.Update` that it uses with no rewrite.

### Success Criteria
- [ ] `dotnet test --solution brutal-ui.sln` passes, with tests for add, remove, reorder, reparent, the dirty mark up to the root, and the walk that skips clean subtrees.
- [ ] An allocation test measures 0 bytes over warm child-list, class-set, `MarkDirty`, and `Update` operations.
- [ ] `dotnet build brutal-ui.sln` gives 0 warnings with CS1591 active on `BrutalUi.Core`.
- [ ] `BrutalUi.Core.csproj` holds no new `PackageReference`, and `Directory.Packages.props` does not change.

---

## Architecture Overview

All types go into the `BrutalUi.Core` namespace, in the folder `src/BrutalUi.Core/Tree/` (A9). `Element` is a class. It stores its child list, its class set, its style slot, and its layout node inline as struct fields. `ElementList` and `ClassList` are small read-only struct views that hold the owner element. A call on a view mutates the storage of the owner and marks the owner dirty. The views keep the shape `ElementList Children { get; }` and `ClassList Classes { get; }` of section 5 of the technical design, and they allocate nothing.

### File Tree (end state)

```
src/BrutalUi.Core/
├── BrutalUi.Core.csproj          # phase 1: GenerateDocumentationFile, InternalsVisibleTo
└── Tree/
    ├── Element.cs                # phase 1, edits in phases 2 to 5
    ├── ElementStorage.cs         # phase 1, internal pooled array (children and roots)
    ├── ElementList.cs            # phase 1, edits in phases 2 and 5
    ├── DirtyFlags.cs             # phase 1
    ├── DirtyWalk.cs              # phase 1, internal: DirtyWalk, IDirtyVisitor, ClearVisitor
    ├── UiContext.cs              # phase 1, edits in phases 2 and 5
    ├── ClassId.cs                # phase 3
    ├── ClassName.cs              # phase 3
    ├── ClassSet.cs               # phase 3, internal: ClassSet, ClassIdBuffer
    ├── ClassList.cs              # phase 3
    ├── PseudoStates.cs           # phase 4
    ├── ComputedStyle.cs          # phase 4
    ├── LayoutNode.cs             # phase 4
    └── ElementBuilder.cs         # phase 4
tests/BrutalUi.Core.Tests/
├── PlaceholderTests.cs           # phase 1 deletes it
├── ElementListTests.cs           # phase 1, phase 2 adds tests
├── DirtyTrackingTests.cs         # phase 1, phase 2 adds tests
├── ClassListTests.cs             # phase 3
├── ElementStateTests.cs          # phase 4
├── TreeGuardTests.cs             # phase 5
└── TreeAllocationTests.cs        # phase 5
docs/technical-design.md          # phase 5: section 5 shape update
```

### Public API (end state)

| Type | Kind | Members |
|------|------|---------|
| `Element` | public class (not sealed) | `Element()`, `string? Name { get; set; }`, `Element? Parent { get; }`, `ElementList Children { get; }`, `ClassList Classes { get; }`, `PseudoStates States { get; set; }`, `bool IsPickable { get; set; }` (default `true`), `ref ComputedStyle Style { get; }`, `ref LayoutNode Layout { get; }`, `DirtyFlags Dirty { get; }`, `void MarkDirty(DirtyFlags kinds)` |
| `ElementList` | public readonly struct (view) | `int Count`, `Element this[int index]`, `void Add(Element child)`, `void Insert(int index, Element child)`, `bool Remove(Element child)`, `void RemoveAt(int index)`, `void Move(int fromIndex, int toIndex)`, `int IndexOf(Element child)`, `bool Contains(Element child)`, `ReadOnlySpan<Element>.Enumerator GetEnumerator()` |
| `ClassList` | public readonly struct (view) | `int Count`, `ClassId this[int index]`, `bool Add(ClassId id)`, `bool Remove(ClassId id)`, `bool Contains(ClassId id)` |
| `ClassId` | public readonly struct, `IEquatable<ClassId>` | `int Value`, `static ClassId None`, `bool IsNone`, `==`, `!=`, `Equals`, `GetHashCode`, `ToString()` (returns the name) |
| `ClassName` | public static class | `ClassId Intern(string name)`, `string GetName(ClassId id)` |
| `PseudoStates` | public `[Flags]` enum | `None = 0`, `Hover = 1`, `Active = 2`, `Focus = 4`, `Disabled = 8`, `Checked = 16` |
| `DirtyFlags` | public `[Flags]` enum | `None = 0`, `Style = 1`, `Layout = 2`, `Paint = 4`, `DescendantStyle = 8`, `DescendantLayout = 16`, `DescendantPaint = 32` |
| `ComputedStyle` | public struct, no fields (A8) | none |
| `LayoutNode` | public struct, no fields (A7) | none |
| `ElementBuilder` | public static class (extension methods) | `T Class<T>(this T element, string name) where T : Element`, `T Class<T>(this T element, ClassId id) where T : Element`, `T Child<T>(this T element, Element child) where T : Element` |
| `UiContext` | public sealed class | `UiContext()`, `ReadOnlySpan<Element> Roots { get; }`, `void AddRoot(Element root)`, `bool RemoveRoot(Element root)`, `void Update(float deltaTime)` |

Internal types: `ElementStorage` (struct), `ClassSet` (struct), `ClassIdBuffer` (`[InlineArray(8)]` struct), `IDirtyVisitor` (interface), `DirtyWalk` (static class), `ClearVisitor` (struct).

### Key Components
- **`ElementStorage`** - An internal mutable struct: `Element[]? _items` and `int _count`. The first add rents from `ArrayPool<Element>.Shared` (`Rent(4)`, the pool gives 16 slots). A full array rents an array of two times the length, copies, and returns the old array with `clearArray: true` (A1). A remove shifts the tail down and sets the free slot to `null`. The storage never shrinks. `Element` uses it for the children. `UiContext` uses it for the roots.
- **`ElementList`** - A view on the `ElementStorage` of its owner. It keeps the parent links and the dirty marks correct.
- **`ClassSet` and `ClassList`** - `ClassSet` holds 8 `ClassId` values inline in a `ClassIdBuffer`. The ninth id moves the set to an array from `ArrayPool<ClassId>.Shared` (A2 of the interview, Q2). The set keeps the order of add. `ClassList` is the public view. A change marks the owner for style.
- **`ClassName`** - One global table (Q1). A `Dictionary<string, ClassId>` and a `List<string>` behind one `System.Threading.Lock` (A6). Id 0 is `ClassId.None`. The first name gets id 1.
- **`DirtyWalk`** - `internal static void Walk<TVisitor>(Element root, DirtyFlags kinds, ref TVisitor visitor) where TVisitor : struct, IDirtyVisitor`. A generic struct visitor gives a direct call with no boxing and no delegate.
- **`UiContext`** - Owns the root `ElementStorage`. `Update` runs the passes in frame order. Each pass is one `DirtyWalk.Walk` call on each root.

### Dirty Tracking

Each kind has a self bit and a descendant bit (A2). The descendant bit of a kind is the self bit shifted left by 3.

**The invariant:** when an element has the self bit or the descendant bit of a kind, each ancestor has the descendant bit of that kind. The early stop of `MarkDirty` and the skip of the walk both depend on it.

**`MarkDirty(DirtyFlags kinds)`:**
1. Keep the self bits of `kinds` only. Ignore the descendant bits. Return when no bit stays.
2. Set the self bits on the element.
3. Compute `up = kinds << 3`. For each ancestor `p`, from the parent to the root: `add = up & ~p.flags`. Stop when `add` is 0. Otherwise set `add` on `p`.

The early stop makes a repeated mark O(1) after the first mark.

**`DirtyWalk.Walk(root, kinds, ref visitor)`** (recursive, depth first):
1. `self = e.flags & kinds`, `desc = e.flags & (kinds << 3)`.
2. When `self != 0`, call `visitor.Visit(e, self)`.
3. When `desc != 0`, walk each child. Skip a child with no bit of `kinds` and no bit of `kinds << 3`.
4. Clear `kinds | (kinds << 3)` on `e`.

A walk on a root with no bit of `kinds` returns at once. A clean subtree costs one flag check. Recursion uses the call stack, not a managed stack, so it allocates nothing. UI trees are shallow (under 100 levels).

**The rules for a visitor:** a visitor must not change the tree, and must not mark the kind that its walk clears. It can mark another kind.

**Structural marks:**

| Change | Marks |
|--------|-------|
| `new Element()` | The element starts with `Style | Layout | Paint` (self bits). |
| Add or insert a child | The new parent: `Layout`. The child: `Style` (its cascade changes). The bits of the child (self and descendant, as descendant bits) go up the new parent chain with the early stop. |
| Remove a child | The old parent: `Layout`. The child keeps its bits. A later add sends them up again. |
| Reparent (A3) | The remove row, then the add row. |
| Move a child in its list | The parent: `Layout`. |
| Class add or remove that changes the set | The owner: `Style`. |
| `States` set to a new value | The owner: `Style`. |
| `Name`, `IsPickable`, a write through `ref Style` or `ref Layout` | No mark. The caller calls `MarkDirty`. The flexbox-layout feature decides on its own setters. |

### The Pass Seam in `UiContext.Update`

```csharp
public void Update(float deltaTime)
{
    ReadOnlySpan<Element> roots = _roots.AsSpan();

    // Seam: the style pass of the style-system epic goes here.
    // Seam: the layout pass of core-tree/flexbox-layout goes here.
    //       It walks DirtyFlags.Layout and clears the layout bits.

    // The clear pass. It clears the bits that no earlier pass cleared.
    for (int i = 0; i < roots.Length; i++)
    {
        ClearVisitor clear = default;
        DirtyWalk.Walk(roots[i], DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint, ref clear);
    }
}
```

The flexbox-layout feature adds one loop with its own visitor (or its own recursion over the same bits) above the clear pass. The clear pass then finds no layout bit and costs one check per clean root. The imgui-renderer epic moves the paint kind out of the clear pass and into `Draw` (Notes item 5). `deltaTime` has no reader in this feature. The framework and the transitions read it later.

### Data Flow

The game builds the tree with `Element`, `Children`, `Classes`, and the builder. Each change sets dirty bits on the element and its ancestors. The game adds the top element with `UiContext.AddRoot`. Each frame, `UiContext.Update` walks the roots. It enters only the subtrees with a descendant bit, calls each pass visitor on the dirty elements, and clears the bits.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The Confirmed Assumptions block names the demo of phase 1 (Q4).

Each phase ends with `dotnet build brutal-ui.sln` and `dotnet test --solution brutal-ui.sln`, with the output filtered to the last 30 lines (tool rule 5). Each new public member gets an XML doc comment in the same task. CS1591 is an error from phase 1 on.

### Phase 1: The Tree, the Dirty Bits, and the Walk
**Goal:** The smallest path that proves the idea. An element tree with a pooled child list (add and remove), the dirty bits with the mark up to the root, the dirty walk, and a `UiContext` whose `Update` clears the bits and skips clean subtrees.
**Demo:** the developer runs `scripts/test-linux.sh`, sees the line `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s`, and sees the `ElementListTests` and `DirtyTrackingTests` results with 0 failed in `artifacts/test-linux/test.log`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase is load-bearing. The dirty-bit invariant, the early stop, the walk, and the pass seam are the base of the flexbox-layout feature and of each later pass. A defect here gives stale layout with no error. Follow the section "Dirty Tracking" exactly. When a rule of that section looks wrong, stop and report.

**Tasks:**

Group A (src, 6 files):
- [ ] 1.1 Write `Tree/ElementStorage.cs`: an internal struct with `Element[]? _items` and `int _count`. Members: `Count`, `AsSpan()` (a `ReadOnlySpan<Element>` of `_count` items, empty when `_items` is null), `Add(Element)`, `RemoveAt(int)` (shift down, set the free slot to `null`), `IndexOf(Element)` (reference compare). Growth per Key Components: `Rent(4)` on the first add, then `Rent(_items.Length * 2)`, copy, `Return(old, clearArray: true)`. No shrink.
- [ ] 1.2 Write `Tree/DirtyFlags.cs`: the public `[Flags]` enum of the Public API table, with a doc comment on each member. In the same file, add an internal static class `DirtyBits` with `const DirtyFlags SelfMask`, `const DirtyFlags DescendantMask`, and `static DirtyFlags ToDescendant(DirtyFlags self)` (shift left by 3).
- [ ] 1.3 Write `Tree/Element.cs` (part 1): the fields `ElementStorage _children`, `DirtyFlags _dirty`, `Element? _parent`. Members: `Element()` (sets `Style | Layout | Paint`), `Name`, `Parent`, `Children`, `Dirty`, and `MarkDirty(DirtyFlags)` per the section "Dirty Tracking". Add an internal method `PropagateToAncestors(DirtyFlags descendantBits)` that runs step 3 of `MarkDirty` from the parent up. `MarkDirty` and the add path both use it.
- [ ] 1.4 Write `Tree/ElementList.cs` (part 1): the public readonly struct with `Element _owner`. Members: `Count`, the indexer, `Add`, `Remove(Element)`, `RemoveAt(int)`, `IndexOf`, `Contains`, `GetEnumerator()` (returns `_owner` storage `AsSpan().GetEnumerator()`). `Add` throws `InvalidOperationException` when the child has a parent (phase 2 replaces this with the reparent). `Add` sets the parent and applies the "Add" row of the structural marks. `Remove` and `RemoveAt` clear the parent and mark the owner for layout. The doc comment says: do not change the list during a `foreach`.
- [ ] 1.5 Write `Tree/DirtyWalk.cs`: the internal `IDirtyVisitor` with `void Visit(Element element, DirtyFlags flags)`, the internal static `DirtyWalk.Walk<TVisitor>` per the section "Dirty Tracking", and the internal struct `ClearVisitor` with an empty `Visit`. Read the children through the storage span. Do not use `foreach` on `ElementList` inside the walk (direct span index, no extra copy).
- [ ] 1.6 Write `Tree/UiContext.cs`: the public sealed class with `ElementStorage _roots`. Members: `UiContext()`, `Roots`, `AddRoot(Element)` (throws `InvalidOperationException` when the element has a parent, phase 2 replaces it), `RemoveRoot(Element)` (returns `false` when the element is not a root), and `Update(float)` with the exact seam comments of the section "The Pass Seam in `UiContext.Update`". The class doc comment says: the tree is main-thread only (A6), and an element is a root of one context at most (A4).

Group B (project and tests, 4 files):
- [ ] 1.7 Edit `src/BrutalUi.Core/BrutalUi.Core.csproj`: add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to the property group, and an item group with `<InternalsVisibleTo Include="BrutalUi.Core.Tests" />`. Add no `PackageReference`.
- [ ] 1.8 Delete `tests/BrutalUi.Core.Tests/PlaceholderTests.cs` (A9). Write `ElementListTests.cs`: add one child (count, index, parent), add three and read the order, remove by reference (count, parent is null, the order of the rest), `RemoveAt`, `Remove` of a non-child returns `false`, 40 adds keep the order (two growths past the 16-slot bucket), `foreach` visits each child in order, `IndexOf` and `Contains`.
- [ ] 1.9 Write `DirtyTrackingTests.cs`. Use an internal test visitor struct that records each visited element in a `List<Element>` (test code can allocate). Tests: a new element has `Style | Layout | Paint`. A mark on a leaf at depth 4 sets the self bit on the leaf and the descendant bit on each ancestor up to the root, and nothing more. A second mark on a sibling leaf stops at the shared parent (the root flags stay the same). `UiContext.Update` clears each bit of each element. The walk visits the dirty leaf and its path, and visits no element of a clean sibling subtree (the recorder holds no element of that subtree). `Remove` marks the old parent for layout. A mark on an element of a removed subtree does not reach the old parent.
- [ ] 1.10 Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 warnings and 0 failed. Then run the demo with `scripts/test-linux.sh`, and `grep -E 'ElementListTests|DirtyTrackingTests|Passed|Failed' artifacts/test-linux/test.log`. Record the results in `context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (10):** Group A: `Tree/ElementStorage.cs`, `Tree/DirtyFlags.cs`, `Tree/Element.cs`, `Tree/ElementList.cs`, `Tree/DirtyWalk.cs`, `Tree/UiContext.cs`. Group B: `BrutalUi.Core.csproj`, `PlaceholderTests.cs` (delete), `ElementListTests.cs`, `DirtyTrackingTests.cs`.

**Acceptance Criteria:**
- [ ] The build gives 0 warnings with `GenerateDocumentationFile` on.
- [ ] Each test of tasks 1.8 and 1.9 passes.
- [ ] The walk test proves that no element of a clean subtree reaches the visitor.
- [ ] `UiContext.Update` holds the two seam comments and the clear pass.
- [ ] `scripts/test-linux.sh` prints its PASS line.

---

### Phase 2: Reorder and Reparent
**Goal:** Complete the child list: insert at an index, move inside the list, and reparent per A3 for `Add`, `Insert`, and `AddRoot`.
**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees the reorder and reparent tests of `ElementListTests` and `DirtyTrackingTests` pass with 0 failed.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Add `Insert(int index, Element)` and `Move(int fromIndex, int toIndex)` to `ElementStorage`. Both shift in place with `Span<Element>.CopyTo` on the overlap-safe range. `Insert` grows per task 1.1.
- [ ] 2.2 Add `Insert` and `Move` to `ElementList`. `Move` marks the owner for layout. `Move` with the same two indexes changes nothing and marks nothing.
- [ ] 2.3 Replace the phase 1 throw in `Add` and `Insert` with the reparent of A3. When the child has a parent, remove it from that list first (the "Remove" row), then add it (the "Add" row). When the old parent is the owner, the index of `Insert` refers to the list after the remove (valid range 0 to `Count` after the remove). `Add` of a child of the same owner moves it to the end.
- [ ] 2.4 In `UiContext.AddRoot`, replace the phase 1 throw: an element with a parent leaves its parent first (the old parent gets a layout mark), then it becomes a root.
- [ ] 2.5 Add tests. `ElementListTests`: insert at 0, at the middle, and at `Count`. Move down, move up, move to the same index. Reparent from parent A to parent B (both counts, the new parent, A no longer lists the child). Add of an existing child of the same owner moves it to the end. Insert of an existing child of the same owner at index 0. `AddRoot` of a child removes it from its parent. `DirtyTrackingTests`: after a clean `Update`, a reparent marks A and B for layout, marks the child for style, and gives each ancestor of A and B the descendant layout bit. A dirty subtree that joins a clean tree gives the new ancestors its descendant bits. A move marks the owner for layout only.
- [ ] 2.6 Run the build and the tests as in task 1.10 (no script run). Record the results in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (5):** `Tree/ElementStorage.cs`, `Tree/ElementList.cs`, `Tree/UiContext.cs`, `ElementListTests.cs`, `DirtyTrackingTests.cs`

**Acceptance Criteria:**
- [ ] After each reparent, the child has one parent, and only that parent lists it.
- [ ] Each mark of A3 is present after a reparent.
- [ ] The tests of task 2.5 pass.

---

### Phase 3: The Class List
**Goal:** Interned class ids, the global class table, and the inline class set with the pooled overflow.
**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ClassListTests` pass with 0 failed, with the test that adds a ninth class and reads all nine back.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Write `Tree/ClassId.cs`: the public readonly struct of the Public API table. `ToString()` returns `ClassName.GetName(this)`, or `"(none)"` for `None`.
- [ ] 3.2 Write `Tree/ClassName.cs`: the public static class. Fields: `static readonly Lock s_lock`, `Dictionary<string, ClassId>` (ordinal compare), `List<string>` with a placeholder at index 0. `Intern` returns the existing id, or adds the name and returns the next id. `GetName` returns the name, and throws `ArgumentOutOfRangeException` for an id that the table does not hold. Both lock (A6).
- [ ] 3.3 Write `Tree/ClassSet.cs`: the internal `[InlineArray(8)] struct ClassIdBuffer` and the internal struct `ClassSet` with `ClassIdBuffer _inline`, `ClassId[]? _overflow`, `int _count`. Members: `Count`, an indexer, `Contains`, `Add` (returns `false` for a duplicate), `Remove` (shift down, keep the order, returns `false` when absent). The ninth add rents `ArrayPool<ClassId>.Shared.Rent(16)`, copies the 8 inline ids, and uses the array from then on. A full array grows per task 1.1 with `clearArray: false`. The set stays on the array after a remove below 8 (Decision 6).
- [ ] 3.4 Write `Tree/ClassList.cs`: the public readonly struct view with `Element _owner`. `Add` and `Remove` mark the owner for style only when the set changes.
- [ ] 3.5 Edit `Tree/Element.cs`: add the field `ClassSet _classes` and the property `ClassList Classes`.
- [ ] 3.6 Write `ClassListTests.cs`. Use unique names for each test (for example a `nameof` prefix), because the table is global and xUnit runs test classes in parallel. Tests: `Intern` of the same name gives the same id, of two names gives two ids. `GetName` returns the name. `ClassId.None` is the default and `IsNone`. Four threads intern the same 100 names, and each name gets one id. Add, contains, remove. A duplicate add returns `false` and marks nothing (check after a clean `Update`). A remove of an absent id returns `false`. Nine adds keep all nine in order. A remove from the overflow keeps the order. A remove down to 3 and an add back to 12 keep the order.
- [ ] 3.7 Run the build and the tests as in task 2.6. Record the results in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (6):** `Tree/ClassId.cs`, `Tree/ClassName.cs`, `Tree/ClassSet.cs`, `Tree/ClassList.cs`, `Tree/Element.cs`, `ClassListTests.cs`

**Acceptance Criteria:**
- [ ] The hot path compares `ClassId` values only. `ClassSet` and `ClassList` hold no `string`.
- [ ] The ninth class moves the set to a pooled array, and no id goes missing.
- [ ] The tests of task 3.6 pass.

---

### Phase 4: Element State, the Slots, and the Fluent Builder
**Goal:** The pseudo-states, the picking flag, the `ref` slots for `ComputedStyle` and `LayoutNode`, and the fluent builder with `Class` and `Child` (Q3).
**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ElementStateTests` pass with 0 failed, with the test that builds a three-level tree in one builder expression.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 4.1 Write `Tree/PseudoStates.cs`: the public `[Flags]` enum of the Public API table.
- [ ] 4.2 Write `Tree/ComputedStyle.cs` and `Tree/LayoutNode.cs`: two public structs with no fields (A7, A8). Each doc comment names the feature that adds the fields: core-tree/flexbox-layout for both, and the style-system epic for the paint fields of `ComputedStyle`.
- [ ] 4.3 Edit `Tree/Element.cs`: add the fields `ComputedStyle _style`, `LayoutNode _layout`, `PseudoStates _states`. Add `ref ComputedStyle Style => ref _style;` and `ref LayoutNode Layout => ref _layout;`. Add `States` with a setter that marks style only when the value changes. Add `IsPickable` with the default `true` (A5) and no mark.
- [ ] 4.4 Write `Tree/ElementBuilder.cs`: the public static class with the three generic extension methods of the Public API table. `Class(string)` calls `ClassName.Intern`, then `Classes.Add`. `Child` calls `Children.Add`. Each returns the element of the call, so a later derived type (`Label`) keeps its type in a chain.
- [ ] 4.5 Write `ElementStateTests.cs`. Tests: a new element has `States == None` and `IsPickable == true`. `States |= Hover` marks style after a clean `Update`. A set to the same value marks nothing. `IsPickable = false` marks nothing. `Unsafe.AreSame(ref e.Style, ref e.Style)` and the same for `Layout` are `true`. A builder expression `new Element { Name = "root" }.Class("a").Child(new Element().Class("b").Child(new Element())).Child(new Element())` gives the expected names, parents, classes, and counts.
- [ ] 4.6 Run the build and the tests as in task 2.6. Record the results in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (6):** `Tree/PseudoStates.cs`, `Tree/ComputedStyle.cs`, `Tree/LayoutNode.cs`, `Tree/Element.cs`, `Tree/ElementBuilder.cs`, `ElementStateTests.cs`

**Acceptance Criteria:**
- [ ] `Style` and `Layout` return a `ref` to the inline field. No per-node heap object exists beyond the element and its pooled arrays.
- [ ] The builder ships `Class` and `Child` only.
- [ ] The tests of task 4.5 pass.

---

### Phase 5: Guards, the Allocation Check, and the Design Doc
**Goal:** Reject each bad call before any change, prove the zero-allocation claim of the requirements, and bring section 5 of the technical design in line with the code.
**Demo:** the developer runs `dotnet test --solution brutal-ui.sln` and sees `TreeGuardTests` and `TreeAllocationTests` pass with 0 failed.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 5.1 Add the cycle guard to `ElementList.Add` and `Insert`: walk up from the owner. When the child is the owner or an ancestor of the owner, throw `InvalidOperationException` before any change. Add the same guard to `ElementBuilder.Child` through `Add` (no extra code).
- [ ] 5.2 Add the argument checks. `ArgumentNullException.ThrowIfNull` for each `Element` argument of `ElementList` and `UiContext`, and for the name of `ClassName.Intern`. `ArgumentException` for an empty name. `ArgumentOutOfRangeException` (with the parameter name) for each index of the indexer, `RemoveAt`, `Insert`, and `Move`. `UiContext.AddRoot` of an element that is already a root of this context throws `InvalidOperationException`. Each check runs before any change.
- [ ] 5.3 Write `TreeGuardTests.cs`: add of the element to itself, add of the parent to its child, add of the root to a leaf at depth 3 (each throws, and the tree, the counts, and the flags do not change). Each null and each bad index throws the named exception with the expected `ParamName`. A duplicate `AddRoot` throws. `ClassName.Intern(null!)` and `Intern("")` throw.
- [ ] 5.4 Write `TreeAllocationTests.cs`. Build a tree: one root, 8 children, each with 8 children. Give one element 12 classes (overflow) and one 4 classes (inline). Add the root to a `UiContext`. Warm up: run the measured body two times. Measure: read `GC.GetAllocatedBytesForCurrentThread()`, run the body 100 times, read again, and `Assert.Equal(0, after - before)` outside the measured region. The body: remove and add back a child, insert at 0 and remove at 0, move first to last and back, remove and add back one class on each class element, set and clear `Hover`, `MarkDirty(Layout)` on a leaf, and `Update(0.016f)`. Keep each list inside its warm capacity. Keep the test synchronous.
- [ ] 5.5 Edit section 5 of `docs/technical-design.md`: update the code block of the shape to the end-state API (`IsPickable` in place of `PickingEnabled`, `States { get; set; }`, `ElementList` and `ClassList` as views, `ClassId`, `MarkDirty`). Add one design decision line on the two-bit dirty flags. Use ASD-STE100 prose.
- [ ] 5.6 Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30`, `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`, and `scripts/test-linux.sh`. Run each non-MANUAL step of the Verification Method. Add the Windows run (Verification step 9) to "Needs Human Verification" in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (6):** `Tree/ElementList.cs`, `Tree/UiContext.cs`, `Tree/ClassName.cs`, `TreeGuardTests.cs`, `TreeAllocationTests.cs`, `docs/technical-design.md`

**Acceptance Criteria:**
- [ ] No public call can make a cycle.
- [ ] A failed call leaves the tree, the counts, and the flags unchanged.
- [ ] The allocation test measures 0 bytes.
- [ ] Section 5 of the technical design matches the public API.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Class name table location: One global static class table: `ClassName.Intern("hud")` returns a `ClassId`.
- Q2 Class set overflow: The class set holds 8 ids inline. Above 8, it moves to a pooled array.
- Q3 Fluent builder: The fluent builder ships with `Class` and `Child` only.
- Q4 Phase 1 demo: Run `scripts/test-linux.sh` and see the tree tests pass.
- A1 The child list rents its array from `ArrayPool<Element>.Shared`, doubles on growth, and returns the old array.
- A2 Each dirty kind has two bits: one for the element and one for a dirty descendant. The walk uses the descendant bit to skip clean subtrees.
- A3 An add on a child that has a parent removes it from the old parent first. Both parents get a layout mark.
- A4 An element holds no reference to its `UiContext` in this feature.
- A5 The picking flag is `bool IsPickable`, with the default `true`.
- A6 The tree is main-thread only, with no locks. The class table is the one exception: it locks on intern.
- A7 `LayoutNode` is an empty struct slot. The flexbox-layout feature adds its fields and the measure callback.
- A8 `ComputedStyle` holds no fields yet. The flexbox-layout feature adds the layout fields.
- A9 All types go into the `BrutalUi.Core` namespace, in a `Tree/` folder. The tests replace `PlaceholderTests.cs` in `BrutalUi.Core.Tests`.

### Decision 1: `Element` is a class with inline struct slots
**Context:** The chosen approach and section 5 of the technical design.
**Decision:** `Element` is a non-sealed class. `ComputedStyle`, `LayoutNode`, the child storage, and the class set are struct fields of the element. `Style` and `Layout` return `ref` to the fields.
**Rationale:** It matches the documented API and the fluent builder. The layout engine reads the slots through `ref` with no copy. No per-node heap object exists beyond the element and its pooled arrays.
**Alternatives Considered:** Struct handles into structure-of-arrays storage in `UiContext`. Rejected: it breaks the documented API, and the markup and framework layers need object identity.

### Decision 2: `ElementList` and `ClassList` are struct views on the owner
**Context:** The documented shape is `ElementList Children { get; }` and `ClassList Classes { get; }`. A mutable struct returned by a getter is a copy, so a change on it is lost. A class wrapper is one more heap object for each element.
**Decision:** Each view is a readonly struct with one field, the owner element. A call on the view changes the internal storage of the owner and applies the dirty marks.
**Rationale:** It keeps the documented shape, it allocates nothing, and each change goes through one path that keeps the parent links and the dirty bits correct.
**Alternatives Considered:** `ref ElementList Children`. Rejected: a caller then changes the storage with no dirty mark and no parent link. Methods on `Element` (`AddChild`, `AddClass`). Rejected: it moves away from the documented shape.

### Decision 3: A generic struct visitor for the dirty walk
**Context:** The flexbox-layout feature plugs its pass into `Update` with no rewrite, and the walk must allocate nothing.
**Decision:** `DirtyWalk.Walk<TVisitor>(Element, DirtyFlags, ref TVisitor) where TVisitor : struct, IDirtyVisitor`. Each pass is one walk with its own kinds. A walk clears the kinds that it walks. `Update` runs the passes in frame order and ends with a clear pass. The types are internal.
**Rationale:** The JIT specializes the generic method for each struct visitor, so the call is direct and can inline. No delegate, no closure, no boxing. The test project reads the walk through `InternalsVisibleTo`, so a test proves the skip of a clean subtree.
**Alternatives Considered:** A `Func<Element, bool>` callback. Rejected: a delegate call, and a closure risk. A public pass interface on `UiContext`. Rejected: YAGNI, and the passes live inside `BrutalUi.Core`.

### Decision 4: `MarkDirty` and `Dirty` are public
**Context:** A write through `ref Layout` or `ref Style` sets no bit. The framework assembly (bindings) changes elements later.
**Decision:** `public void MarkDirty(DirtyFlags kinds)` marks self bits only. It ignores descendant bits. `public DirtyFlags Dirty { get; }` reads all bits. The clear stays internal to the walk.
**Rationale:** A caller outside `BrutalUi.Core` can mark a change. A caller cannot clear a bit and break the invariant.

### Decision 5: A reparent marks the child for style
**Context:** A3 fixes the layout marks of both parents. A moved subtree also gets a new cascade.
**Decision:** Each add and insert marks the child for style, and sends the bits of the child up the new parent chain.
**Rationale:** The style-system epic needs it. The cost is one bit and one short loop. A3 stays exact: both parents get a layout mark.

### Decision 6: The class set stays on the array after a remove
**Context:** Q2 moves the set to a pooled array above 8 ids. It does not say when the set moves back.
**Decision:** The set never moves back to the inline buffer.
**Rationale:** No thrash at the edge (8 to 9 to 8). An element with more than 8 classes keeps the array that it already holds.

### Decision 7: The child storage never shrinks
**Context:** A1 doubles on growth. A remove gives free slots.
**Decision:** No shrink. A remove sets the free slot to `null`, so the array holds no stale reference. The storage of an element that the GC collects goes with it. The pool does not need it back.
**Rationale:** A warm list never rents again. A shrink rule adds a rent at each edge for no gain.

### Decision 8: `GenerateDocumentationFile` on `BrutalUi.Core`
**Context:** The requirements ask for an XML doc comment on each public member. `TreatWarningsAsErrors` is on.
**Decision:** Set `GenerateDocumentationFile` to `true` in `BrutalUi.Core.csproj`, so CS1591 is a build error. Add `InternalsVisibleTo` for `BrutalUi.Core.Tests` in the same file.
**Rationale:** The compiler checks each later change. The NuGet package gets the XML file for IntelliSense. Neither item is a package reference, so A7 of the epic stays true.

### Decision 9: `UiContext` has a parameterless constructor
**Context:** The rules ask for `required` create-info structs. Section 13 of the technical design names `UiContext.CreateInfo` with the display scale, the stylesheet, the font set, and the renderer. None of these exist yet.
**Decision:** `UiContext()` with no create-info. The epic that adds the first setting adds `UiContext.CreateInfo`.
**Rationale:** An empty create-info struct is YAGNI. The package is `0.1.0-alpha`, so the later signature change is acceptable (Notes item 7).

### Decision 10: Update clears all three kinds in this feature
**Context:** The requirements say: in this feature the walk clears the flags. The paint pass is `Draw`, which does not exist yet.
**Decision:** The clear pass of `Update` walks `Style | Layout | Paint`. The imgui-renderer epic removes `Paint` from that mask and clears it in `Draw`.
**Rationale:** The bits never stay set forever, and the change later is one mask.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| A path breaks the dirty-bit invariant, so the early stop of a later mark hides a dirty element. Layout goes stale with no error. | Medium | High | One propagation method for all paths (task 1.3). Phase 1 runs on the advanced tier. Tests 1.9 and 2.5 check the bits after each structural change. A visitor must not mark its own kind (doc comment). |
| A change of a child list during a `foreach` reads a returned pool array. | Low | High | The doc comment forbids it. The span enumerator reads the old array, which `clearArray: true` sets to `null`, so a read gives `null`, not another tree. A version check is YAGNI (Notes item 9). |
| `ArrayPool.Shared` trims its buckets on a Gen 2 GC, so a later growth rents a new array. | Medium | Low | Only a growth rents. A warm list does not grow. The allocation test keeps each list inside its capacity. |
| The allocation test is flaky: JIT tiering or the test runner allocates on the test thread. | Low | Medium | Two warm-up runs. A synchronous test. The `Assert` sits outside the measured region. The allocation-guard feature uses the same method on a larger tree. |
| xUnit runs test classes in parallel, and the global class table gives different ids in different runs. | Medium | Low | No test asserts a fixed id value. Each test uses unique names (task 3.6). |
| Deep recursion in the walk overflows the stack. | Low | Medium | UI trees are under 100 levels. Notes item 10 records the limit. |
| The flexbox-layout pass needs to skip the walk into a subtree that it lays out itself. | Medium | Low | The visitor gets the element and its bits. The pass can run its own recursion over the same bits, and then clear them. Notes item 4 records the rule. |
| An element joins two contexts as a root. The tree has no way to see it (A4). | Low | Medium | The doc comment of `AddRoot` forbids it. The input epic can add a context reference later. |

---

## Dependencies

### Internal Dependencies
- The dev-ops epic (complete): `brutal-ui.sln`, `src/BrutalUi.Core/BrutalUi.Core.csproj`, `tests/BrutalUi.Core.Tests` (xUnit v3, an `Exe` project on Microsoft.Testing.Platform), `Directory.Build.props` (nullable, warnings as errors, code style in build), `.editorconfig` (file-scoped namespaces as a warning, `using` outside the namespace), and `scripts/test-linux.sh`.
- No sibling feature. This feature comes first.

### External Dependencies
- None new. The base class library gives `ArrayPool<T>`, `System.Threading.Lock` (.NET 9 and later), `InlineArrayAttribute` (.NET 8 and later), and `Unsafe.AreSame` (tests only).
- `Ahwoo.BRUTAL.Numerics` stays referenced. This feature uses no vector type, because `LayoutNode` has no field yet. The flexbox-layout feature uses `float4` for the rect.
- No native dependency.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. `Children.Add`, `Insert`, `Remove`, `RemoveAt`, and `Move` keep the order, the count, and the parent link correct. `ElementListTests` proves each one.
- [ ] F2. A reparent with `Add` or `Insert` removes the child from the old parent first, and both parents get the layout bit (A3). A test proves it.
- [ ] F3. `MarkDirty` on a leaf sets the self bit on the leaf and the descendant bit on each ancestor up to the root. A test proves it.
- [ ] F4. `UiContext.Update` clears each dirty bit of each element under its roots. The walk gives no element of a clean subtree to the visitor. A test proves both.
- [ ] F5. `ClassName.Intern` gives one id for one name, also under four threads. The class set holds 8 ids inline and keeps all ids after the ninth add. A test proves it.
- [ ] F6. `States`, `IsPickable` (default `true`), `ref Style`, and `ref Layout` work as the Public API table says. A test proves it.
- [ ] F7. The builder expression of task 4.5 builds the expected tree. The builder has `Class` and `Child` only.
- [ ] F8. Each guard of tasks 5.1 and 5.2 throws the named exception and changes nothing.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`, with `GenerateDocumentationFile` set in `BrutalUi.Core.csproj`.
- [ ] Q2. `TreeAllocationTests` measures 0 bytes over 100 runs of the warm body of task 5.4.
- [ ] Q3. Each file under `src/BrutalUi.Core/Tree/` holds `namespace BrutalUi.Core;` (file-scoped).
- [ ] Q4. No file under `src/BrutalUi.Core/Tree/` holds `new List<`, `new Dictionary<`, `Func<`, `Action<`, or a lambda, except `ClassName.cs` (the table).
- [ ] Q5. `PlaceholderTests.cs` does not exist in `tests/BrutalUi.Core.Tests/`.
- [ ] Q6. The doc comments use ASD-STE100 prose.
- [ ] Q7. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows.

### Integration Criteria
- [ ] I1. `UiContext.Update` holds the two seam comments of the section "The Pass Seam", and one clear pass with `DirtyWalk.Walk`.
- [ ] I2. `LayoutNode` and `ComputedStyle` are public structs with no fields, stored inline in `Element`, and returned by `ref`.
- [ ] I3. `BrutalUi.Core.csproj` holds the same two `PackageReference` items as before (`Ahwoo.BRUTAL.Numerics`, `Ahwoo.BRUTAL.Localization`). `Directory.Packages.props` does not change.
- [ ] I4. Section 5 of `docs/technical-design.md` matches the Public API table.

### Verification Method

Run steps 1 to 8 from `/home/aaronstatic/dev/brutal-ui` on Linux.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed for each test project (F1 to F8, Q2).
3. `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --list-tests 2>&1 | grep -E 'ElementListTests|DirtyTrackingTests|ClassListTests|ElementStateTests|TreeGuardTests|TreeAllocationTests' | wc -l`. Expect a number above 0 for each class name (run the `grep` one time for each name to see each count). Read the test names, and match them to F1 to F8.
4. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Then `grep -E 'Passed|Failed' artifacts/test-linux/test.log`. Expect 0 failed.
5. `grep -L 'namespace BrutalUi.Core;' src/BrutalUi.Core/Tree/*.cs`. Expect no output (Q3). `grep -nE 'new List<|new Dictionary<|Func<|Action<|=>\s*\{|\) =>' src/BrutalUi.Core/Tree/*.cs | grep -v ClassName.cs`. Expect no lambda and no collection (Q4). An expression-bodied member (`=> ref _style;`) is not a lambda. Read each hit.
6. `test ! -e tests/BrutalUi.Core.Tests/PlaceholderTests.cs && echo ok`. Expect `ok` (Q5).
7. `grep -c PackageReference src/BrutalUi.Core/BrutalUi.Core.csproj; git diff main -- Directory.Packages.props`. Expect `2` and no diff (I3). `grep -n 'GenerateDocumentationFile\|InternalsVisibleTo' src/BrutalUi.Core/BrutalUi.Core.csproj`. Expect both.
8. Read `src/BrutalUi.Core/Tree/UiContext.cs` for I1, `Element.cs`, `LayoutNode.cs`, and `ComputedStyle.cs` for I2, and section 5 of `docs/technical-design.md` for I4.
9. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0 (Q7).

---

## Testing Strategy

### Unit Tests
- `ElementListTests`: add, insert, remove, remove at, move, reparent, growth past the pool bucket, `foreach`.
- `DirtyTrackingTests`: the start bits, the mark up to the root, the early stop, the structural marks, the clear in `Update`, and the skip of a clean subtree with a recording visitor.
- `ClassListTests`: intern, the name lookup, the thread test, the inline set, the overflow, the order.
- `ElementStateTests`: the pseudo-states, the picking flag, the `ref` slots, the builder.
- `TreeGuardTests`: the cycle guard, the null checks, the index checks, the duplicate root.
- The tests reach the internal walk through `InternalsVisibleTo`. Each test builds its own tree. No test shares a tree.

### Allocation Test
- `TreeAllocationTests` measures a warm body with `GC.GetAllocatedBytesForCurrentThread()` (task 5.4). It covers the operations of the requirements. The allocation-guard feature adds the 1000-element test on `Update` with the layout pass.

### Integration Tests
- `scripts/test-linux.sh` runs the full solution build and the tests (the demo of phase 1).
- (MANUAL) `scripts\test-windows.ps1` on Windows.

### Manual Testing
- None on Linux. The feature has no visible output.

---

## Related Documentation

- **Requirements:** `docs/features/core-tree/element-tree/requirements.md`
- **Epic:** `docs/features/core-tree/epic-overview.md`, `docs/features/core-tree/epic-requirements.md`
- **Siblings:** `docs/features/core-tree/flexbox-layout/requirements.md`, `docs/features/core-tree/allocation-guard/requirements.md`
- **Design:** `docs/technical-design.md` sections 4 (dirty tracking, frame order), 5 (element tree), 13 (configuration), 15 (principles)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Format reference:** `docs/features/dev-ops/windows-test-path/implementation.md`

---

## Notes

### Conflicts and Gaps

1. **`IsPickable` against `PickingEnabled`.** A5 names `bool IsPickable`. Section 5 of `docs/technical-design.md` names `bool PickingEnabled`. The plan keeps A5. Task 5.5 updates the design doc.
2. **`ComputedStyle` fields.** The requirements say: `ComputedStyle` holds the layout fields only. A8 says: it holds no fields yet, and the flexbox-layout feature adds them. The plan keeps A8. The two statements agree at the end of the epic.
3. **`States` setter.** Section 5 of the technical design shows `PseudoStates States { get; }` with no setter. The plan adds a setter that marks style, because no other path sets a state in this epic. The input epic can make the setter internal later, if it routes all state changes.
4. **The flexbox-layout pass and the walk.** The walk always enters a subtree with a descendant bit. A layout pass that lays out a full subtree from one dirty element can run its own recursion over the same bits. When it does, it must clear the bits that it skips. Otherwise a stale descendant bit stops a later mark early, and a dirty element stays hidden. The flexbox-layout plan must state its choice.
5. **The paint kind.** Decision 10. The imgui-renderer epic moves the paint clear from `Update` to `Draw`.
6. **The demo needs the GPU steps.** `scripts/test-linux.sh` runs the `gpu`, `loader`, and `smoke` steps after the tests. The demo of phase 1 needs the NVIDIA driver and Xvfb of `docs/testing-linux.md`. When those steps fail, the `test` step result in `artifacts/test-linux/test.log` still shows the tree tests.
7. **`UiContext.CreateInfo`.** Decision 9. The later epic changes the constructor signature. The rule "required create-info structs" applies from that point.
8. **No user personas file.** `docs/user-personas.md` does not exist. The personas of this plan come from the mission statement and the epic docs.
9. **No version check on the child list.** A change during a `foreach` is a caller error. A version counter costs one field and one check for each read. Add it only when a real defect shows.
10. **The recursion depth.** The walk recurses once for each level. A tree deeper than several thousand levels overflows the stack. No game UI comes near it. An explicit stack needs a pooled array, so it waits for a real need.
11. **The epic overview.** After this feature, `/update-epic core-tree` records the pass seam and Notes item 4 under Integration & Architecture.
12. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. The dev agent can run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on the changed docs in task 5.6.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
