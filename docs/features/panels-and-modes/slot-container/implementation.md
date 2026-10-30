# Slot Container - Implementation Plan

**Epic:** panels-and-modes (feature 2 of 4)
**Status:** Planning
**Started:** 2026-10-30
**Target Completion:** TBD
**Last Updated:** 2026-10-30 11:30

---

## Quality Bar

**Target quality:** A library feature that sets the mount contract of the panels-and-modes epic. mode-set calls the mount, the unmount, and the lookup of this feature from its diff, in a frame. A wrong mount rule loses the state of a panel or puts one root in two slots. A hidden allocation in the mount costs bytes at each mode change, and the `ModeDiffAllocationTests` gate of panel-sample then fails.

- **Reliability:** A mount puts the root at the given index. A mount of a root that the slot holds moves the root and does not join it again, so the panel keeps its style and its running tweens. A mount of a root with another parent throws `InvalidOperationException`. An unmount detaches the root and keeps the root, its subtree, its classes, its inline style, and its bindings. Each throw leaves both slots and the root unchanged. A test pins each rule.
- **API clarity:** A game builds a slot with one constructor call and a UTF-8 literal, and adds it to the layout root as a plain element. mode-set calls three members: `Mount`, `Unmount`, and `SlotContainer.Find`. Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the order rule, the parent rule, the keep rule, the lookup rule, and the style rule.
- **0 bytes after warm-up:** `Mount`, `Unmount`, and `Find` allocate 0 bytes on a warm slot in the default Debug build. `Find` allocates nothing also with no cache. One test measures it, and two plants fail it.
- **No Core change:** `BrutalUi.Core` gets no change. No project file changes. Decision 9 holds the proof.

---

## Executive Summary

This feature adds one type to `BrutalUi.Framework`, in the folder `src/BrutalUi.Framework/Panels/` of panel-definition:

- `SlotContainer` (a sealed `Element` subtype). It holds a copy of its UTF-8 slot name, and its type name is `SlotContainer`. The constructor also sets `Element.Name` to the text of the slot name, so a name rule of a stylesheet styles the slot (Notes question 2).
- `Mount(Element root, int index)` attaches a panel root as a child at an index. It moves a root that the slot holds, and it throws `InvalidOperationException` for a root with another parent.
- `Unmount(Element root)` detaches a root and gives `true`, or gives `false` when the slot does not hold the root.
- `SlotContainer.Find(Element root, ReadOnlySpan<byte> name)` walks a layout root in tree order and gives the first slot of the name, or `null`.

The slot uses the child list of Core (`ElementList.Insert`, `Move`, and `Remove`), so the dirty bits, the parent links, and the binding marks follow the Core rules with no new code. The style pass and the layout pass see a plain `Element`.

The plan has 2 phases and 10 tasks. Phase 1 (4 tasks) delivers the slot with the mount, the unmount, and the parent rule. Phase 2 (6 tasks) delivers the lookup, the style and layout proof, the 0-byte test, and the docs. No phase needs an ADVANCED dev agent.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs, as in panel-definition.

**Target Users:**
- [x] **The game developer** (puts one `SlotContainer` for each slot into the layout factory, and styles each slot by type, class, or name)
- [x] **The sibling features** (mode-set finds each slot one time, caches it, and calls `Mount` and `Unmount` from the diff in a frame. panel-sample styles each slot by class. xml-markup later builds a slot from an XML element)
- [x] **The maintainer** (runs three more test classes through `scripts/test-linux.sh` with no script change)

**User Subsets Served:**
- [x] **Simple** - one layout root that is itself a slot, and one panel.
- [x] **Complex** - a layout with three slots at different depths, several panels in each slot, a panel that moves between slots, and a stylesheet with a descendant rule for each slot class.

**UX Approach:**
- The layout factory builds a slot as `new SlotContainer("left"u8)`, sets a class, and adds it as a child.
- mode-set calls `SlotContainer.Find(layoutRoot, "left"u8)` one time and keeps the result.
- No builder, no create-info, no `IDisposable`, no event keyword, and no closure in the frame path.

---

## Goals

### Primary Goals
1. Add `SlotContainer` as a sealed `Element` subtype with a `byte[]` copy of its slot name and the type name `SlotContainer` (requirements, A4 of the epic).
2. Add `Mount` and `Unmount` with the order rule, the parent rule, and the keep rule (A4, A5 of the epic, Q3 of the epic).
3. Add the static lookup `Find` with the tree order rule and the `null` result for a missing name (Q4 of the epic).
4. Prove that the style pass and the layout pass treat a slot as a plain `Element`.
5. Prove 0 bytes after warm-up for `Mount`, `Unmount`, and `Find`.
6. State the rules in section 10 and section 14 of the technical design and in the changelog.

### Success Criteria
- [ ] The phase 1 demo passes: `SlotContainerTests` reads a total of 10 with `failed: 0`.
- [ ] The phase 2 demo passes: `SlotLookupTests` reads a total of 8 and `SlotContainerTreeTests` reads a total of 5, each with `failed: 0`, and `scripts/test-linux.sh` prints its PASS line.
- [ ] `SlotLookupTests.MountUnmountAndFindAllocateNothingAfterWarmUp` measures 0 bytes in Debug, and the two allocation plants of task 2.4 fail it.
- [ ] Each existing test passes with no edit.
- [ ] `src/BrutalUi.Core/` and each project file have no diff.

---

## Architecture Overview

### The Place in the Epic

This feature owns no frame order change. The layout factory of a `LayoutDefinition` (panel-definition) builds the layout root and its `SlotContainer` elements at build time. mode-set adds the layout root to the `UiContext` (A4 of the epic). At build time or at the first diff, mode-set calls `Find` for each slot name of each mode and caches each result. In the diff, mode-set calls `Unmount` for each leaving root, and `Mount` for each root of the new mode in the order of the mode definition. Each root comes from `PanelController.EnsureRoot()` (panel-definition), so a root is created one time (Q3 of the epic). The next `Update` styles and lays out the changed slots through the Core passes.

```
Build time (panel-definition and the game)
  LayoutDefinition.CreateRoot(ctx) ──► layout root
                                         ├─ SlotContainer "left"   (class, name, inline style)
                                         └─ column
                                              └─ SlotContainer "right"
mode-set (later), at build time or at the first diff:
  SlotContainer.Find(layoutRoot, "left"u8) ──► cached SlotContainer reference
mode-set diff (later), in OnBeforeUpdate:
  slot.Unmount(leavingRoot)                 ──► false when the slot does not hold it
  slot.Mount(controller.EnsureRoot(), i)    ──► the root at index i, in the order of the mode
UiContext.Update (Core, no change):
  the binding pass, the style pass, and the layout pass see plain elements
```

### File Tree (end state)

```
src/BrutalUi.Framework/Panels/
└── SlotContainer.cs                # phase 1 (new), phase 2 (adds Find)
tests/BrutalUi.Framework.Tests/Panels/
├── SlotContainerTests.cs           # phase 1 (new): 10 tests
├── SlotLookupTests.cs              # phase 2 (new): 8 tests, with the 0-byte test
└── SlotContainerTreeTests.cs       # phase 2 (new): 5 tests
docs/technical-design.md            # phase 2: section 10 (new sub-section "Slot containers") and section 14 (one bullet)
CHANGELOG.md                        # phase 2: one line under Added
docs/features/panels-and-modes/slot-container/context.md   # each phase
```

These files do not change: each file of `src/BrutalUi.Core/`, each other file of `src/BrutalUi.Framework/` (also the six panel-definition files), each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file. The new test files compile in `BrutalUi.Framework.Tests` with no project edit. The source file uses the namespace `BrutalUi.Framework`, and the test files use `BrutalUi.Framework.Tests`.

### Public API (end state)

```csharp
namespace BrutalUi.Framework;

public sealed class SlotContainer : Element
{
    public SlotContainer(ReadOnlySpan<byte> name);              // ArgumentException for an empty name
    public ReadOnlySpan<byte> SlotName { get; }                  // a span over the copy
    public override ElementTypeId TypeId { get; }                // "SlotContainer"
    public void Mount(Element root, int index);                  // see the mount rule
    public bool Unmount(Element root);                           // false when the slot does not hold the root
    public static SlotContainer? Find(Element root, ReadOnlySpan<byte> name);   // null for a missing name
}
```

### Internal Shape

- **The type id.** `private static readonly ElementTypeId s_typeId = ElementType.Intern("SlotContainer");` and `public override ElementTypeId TypeId => s_typeId;`, as `Image` and `Label` do. A type rule of a stylesheet (`SlotContainer { ... }` in a text sheet, or `Selector.ForType(ElementType.Intern("SlotContainer"))`) matches it. The class is `sealed`.
- **The slot name (Decision 2).** The constructor checks that `name` is not empty (`ArgumentException`, constant message, parameter `name`). It copies the name with `name.ToArray()` into `private readonly byte[] _slotName`. `public ReadOnlySpan<byte> SlotName => _slotName;` reads it with 0 bytes. A change of the source array after the build changes nothing.
- **The element name (Decision 3, Notes question 2).** The constructor then sets `Name = Encoding.UTF8.GetString(name);`. A name rule (`#left`) then matches the slot. The string is a build-time allocation. `Find` reads `SlotName` only. A later set of `Name` changes the style match and does not change the lookup. The remarks state it.
- **`Mount` (Decisions 4 and 5).**
  ```
  ArgumentNullException.ThrowIfNull(root);
  Element? parent = root.Parent;
  if (ReferenceEquals(parent, this))
  {
      Children.Move(Children.IndexOf(root), index);  // checks the index (0 to Count - 1) before a change.
      return;                                        // The same index marks nothing. No join, no restyle.
  }
  if (parent is not null)
      throw new InvalidOperationException(parent is SlotContainer ? HeldByOtherSlotMessage : OtherParentMessage);
  Children.Insert(index, root);                      // Core checks a context root and a cycle, then the index
                                                     // (0 to Count), before a change.
  ```
  The messages are `private const string` fields: "The root is in another slot. Unmount it from that slot first." and "The root has another parent. Remove it from its parent first." Each throw comes before a change, so a failed mount leaves both slots and the root unchanged. A root that is a `UiContext` root, the slot itself, or an ancestor of the slot throws `InvalidOperationException` from `ElementList.Insert`.
- **`Unmount` (Decision 6).** `ArgumentNullException.ThrowIfNull(root); return Children.Remove(root);`. The slot drops its reference. The root keeps its subtree, its dirty bits, its classes, its inline style, its name, and its bindings (the Core rules of `ElementList.Remove` and the leave rule of the binding pass). The caller keeps the root alive (`PanelController`, Q3 of the epic). A root that another slot holds gives `false`, and that slot keeps it.
- **`Find` (Decision 7).** `ArgumentNullException.ThrowIfNull(root);` then a private static recursive walk in depth-first pre-order:
  ```
  if (element is SlotContainer slot)
      return slot._slotName.AsSpan().SequenceEqual(name) ? slot : null;   // no walk into a slot
  foreach (Element child in element.Children)                           // a span enumerator, 0 bytes
      if (Walk(child, name) is SlotContainer hit) return hit;
  return null;
  ```
  The walk checks the root itself first. It does not enter the children of a slot, so the mounted panels do not change the cost or the result. The first match in tree order wins. An empty name or a missing name gives `null`. The compare is by bytes, so `left` and `Left` are two names.
- **The direct child list.** The remarks of the class tell the game and mode-set to change the children of a slot through `Mount` and `Unmount` only. A direct `Children` call works, but it skips the parent rule.
- **The thread rule.** The slot runs on the UI thread and uses no lock, as `Element` does.

### Integration Points

| Sibling or caller | What it calls | Contract that this feature fixes |
|---|---|---|
| panel-definition (done) | `PanelController.EnsureRoot()`, `PanelInstance.Root` | The slot takes any `Element`. It does not call `EnsureRoot` (Notes question 3). The controller keeps the root, and the slot drops it at the unmount. |
| mode-set (`UiManager` lookup) | `SlotContainer.Find(layoutRoot, slotName)` | It allocates nothing, also with no cache. The result depends on the layout tree only, not on the mounted panels. A cached reference stays valid while the slot stays in the layout root. A missing name gives `null`, and mode-set decides the error. |
| mode-set (the diff) | `Unmount(root)`, then `Mount(root, i)` for `i` from 0 in the order of the mode slot | A mount of a held root moves it with no join, so a panel that stays (A5 of the epic) keeps its style and its tweens. A loop of `Mount(root_i, i)` over the panels of a slot gives the order of the mode for new and held roots. A root in another slot throws, so mode-set unmounts each leaving root first, also a root that moves to another slot (A5 of the epic). |
| mode-set (a layout swap) | `UiContext.RemoveRoot`, `AddRoot`, `Find` | The slots of the old layout keep their panels until mode-set unmounts them. A mount of such a root into a new slot throws until then. |
| panel-sample | The type name and the class of each slot | A class rule and a type rule match the slot. The layout pass gives the slot the rect of a plain element. |
| xml-markup (later epic) | The constructor | A loader calls the constructor with the bytes of the XML slot name. |

### Data Flow

```
Build time (allocates one time):
  left = new SlotContainer("left"u8)   ──► _slotName (copy), Name = "left"
  left.Classes.Add(slotClass); layoutRoot.Children.Add(left)
  ui.AddRoot(layoutRoot)

The first diff (mode-set, 0 bytes):
  cached = SlotContainer.Find(layoutRoot, "left"u8)   ──► left
  cached.Mount(inventoryRoot, 0); cached.Mount(statsRoot, 1)

A later diff (0 bytes):
  cached.Unmount(statsRoot)            ──► true, statsRoot.Parent is null, statsRoot keeps its state
  cached.Mount(mapRoot, 0)             ──► mapRoot, inventoryRoot
  cached.Mount(inventoryRoot, 0)       ──► inventoryRoot, mapRoot (a move, no join)

UiContext.Update (Core):
  the style pass restyles mapRoot (a join), the layout pass lays out left
```

---

## Implementation Phases

The feature has 10 tasks, above the "about 8 tasks" of Slice rule 7, so it has two slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea: a slot holds panel roots in order and lets them go with their state. Phase 2 adds the lookup, the proof of the plain element, the 0-byte test, and the docs.

Each verification command sets `MSBUILDDISABLENODEREUSE=1` and gives `-nodeReuse:false` to `dotnet build`, because stale MSBuild nodes on this machine hang the build. Do not run `dotnet build-server shutdown`. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `System.Text`, `BrutalUi.Core`), also for each namespace that a `cref` names. Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one class with `MSBUILDDISABLENODEREUSE=1 dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. When `dotnet run` hangs, run the built binary: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`.

The test files use unique class names for each test (`ClassName.Intern(nameof(TestName) + ".slot")`), because xUnit runs the classes in parallel and the class table is global. The type name `SlotContainer` is shared, and `ElementType.Intern` gives the same id each time.

### Phase 1: A Slot That Holds Panel Roots in Order
**Goal:** A game builds a `SlotContainer` with a UTF-8 name. A mount puts each panel root at its index, a mount of a held root moves it, and an unmount detaches a root and keeps its state. A mount of a root with another parent throws `InvalidOperationException` and changes nothing.
**Demo:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*SlotContainerTests'` after a build and reads a total of 10 with `failed: 0`.
**Needs:** none
**Agent tier:** STANDARD (sonnet).

**Tasks (one group):**
- [ ] 1.1 Create `docs/features/panels-and-modes/slot-container/context.md`. Check that the panel-definition files exist in `src/BrutalUi.Framework/Panels/` (six files). Record the base commit (`git rev-parse HEAD`), the test count of the solution (with the skipped count), and the count of `BrutalUi.Framework.Tests`. At planning time, the counts were 2750 (3 skipped) and 157. The panel-definition review fixes CR-1 to CR-4 keep these counts. Record the Internal Shape and the Integration Points of this plan.
- [ ] 1.2 Write `src/BrutalUi.Framework/Panels/SlotContainer.cs` with the constructor, `SlotName`, `TypeId`, the element name, `Mount`, and `Unmount` of the Public API and the Internal Shape. Do not add `Find` yet. The class remarks: the slot holds the panel roots of one slot as its children, in the order of the mode definition (A4 of the epic). The style pass and the layout pass treat it as a plain `Element`, and a stylesheet styles it by the type name `SlotContainer`, by class, or by name. The constructor sets `Name` to the text of the slot name, and a later set of `Name` does not change `SlotName`. Change the children through `Mount` and `Unmount` only. The slot runs on the UI thread. The remarks of `Mount`: the index range (0 to `Count` for a new root, 0 to `Count` minus 1 for a held root), the move with no join, the parent rule, each exception, and "each throw comes before a change". The remarks of `Unmount`: the keep rule and the `false` result. Each remark names the allocation rule: 0 bytes after the child array of the slot holds the largest count.
- [ ] 1.3 Write `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTests.cs` with 10 tests. Tests: `ANewSlotHoldsACopyOfItsNameAndNoChild` (build from a `byte[]` source, change a byte of the source, read the old name with `SequenceEqual`, `Children.Count` 0, `Parent` `null`), `TheConstructorRejectsAnEmptyName` (`ArgumentException` with `ParamName` `name`), `TheTypeIsSlotContainerAndTheNameIsTheTextOfTheSlotName` (`TypeId.ToString()` is `SlotContainer`, `TypeId` differs from `new Element().TypeId`, and `Name` is `left`), `MountPutsEachRootAtItsIndex` (mount `c` at 0, `a` at 0, `b` at 1, and read `a`, `b`, `c`. An append gives `c`, `a`, `b`, so the test sees an index that the mount drops. Each root has the slot as `Parent`), `AMountOfAHeldRootMovesItWithNoJoin` (a slot in a `UiContext` with a `RecordingRenderer` holds `a`, `b`, `c`. Run `Update(0f)`. `Mount(c, 0)` gives `c`, `a`, `b` and `Count` 3. `c.Dirty` holds no `DirtyFlags.Style` bit, and the slot holds `DirtyFlags.Layout`. Run `Update(0f)`. `Mount(c, 0)` again marks nothing on the slot. `Mount(a, 3)` throws `ArgumentOutOfRangeException` and changes nothing), `UnmountDetachesTheRootAndKeepsItsState` (a root with two children, a class, an inline width, and a name, in a slot of a context. After `Update(0f)`, `Unmount` gives `true`, `Parent` is `null`, the slot count falls by one, and the root keeps the same two children in order, the class, the inline width, and the name. A second mount at 0 puts the same root back), `UnmountOfARootThatTheSlotDoesNotHoldReturnsFalse` (a root with no parent, and a root of a second slot. Each gives `false`, and the second slot still holds its root), `AMountOfARootThatAnotherSlotHoldsThrows` (`InvalidOperationException`. The other slot still holds the root at the same index, and this slot keeps its count), `AMountOfARootWithAnotherParentThrows` (a plain `Element` parent. `InvalidOperationException`, and the parent still holds the root), and `TheMountRejectsANullRootABadIndexAContextRootAndACycle` (`ArgumentNullException` with `ParamName` `root` for `Mount` and `Unmount`, `ArgumentOutOfRangeException` for -1 and `Count + 1`, `InvalidOperationException` for a root of a `UiContext`, for the slot itself, and for an ancestor of the slot. Each case leaves the count of the slot the same).
- [ ] 1.4 Run the build, the tests, and `SlotContainerTests` (total 10). Expect `failed: 0`, and each other test passes with no edit. Run two plants. Copy `SlotContainer.cs` to the scratchpad first. Plant 1: remove the `throw` of the parent rule. Expect `AMountOfARootThatAnotherSlotHoldsThrows` and `AMountOfARootWithAnotherParentThrows` to fail. Plant 2: replace `Children.Insert(index, root)` with `Children.Add(root)`. Expect `MountPutsEachRootAtItsIndex` to fail. For each plant, run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file and check it with `cmp`. Run the tests again and expect 0 failed. Record the demo and the results in `context.md`.

**Estimated Steps:** about 25 (4 tasks, about six steps for one task)

**Files (one group, 3):** `SlotContainer.cs`, `SlotContainerTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `SlotContainerTests` pass. Each existing test passes with no edit.
- [ ] Both plants of task 1.4 give a non-zero exit code, and the restored file equals its copy.
- [ ] `src/BrutalUi.Core/`, each project file, and each existing test file have no diff.

### Phase 2: The Slot Lookup and the Plain Element Proof
**Goal:** mode-set finds a slot by name in a layout root with 0 bytes. The style pass and the layout pass treat a slot as a plain `Element`. A moved panel takes the style of its new slot, and an unmounted panel keeps its bindings. `Mount`, `Unmount`, and `Find` allocate 0 bytes after warm-up. The technical design and the changelog state the rules.
**Demo:** a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built binary reads a total of 8 for `--filter-class '*SlotLookupTests'` and 5 for `--filter-class '*SlotContainerTreeTests'`, each with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet).

**Tasks (one group):**
- [ ] 2.1 Add `Find` to `src/BrutalUi.Framework/Panels/SlotContainer.cs`: the public static method of the Public API and the private static walk of the Internal Shape. The remarks: the walk checks the root itself, then each child in depth-first pre-order. It does not enter the children of a slot, so the result depends on the layout tree only. The first match wins, and two slots with one name are a bug of the layout. The compare is by bytes, and `Find` reads `SlotName`, not `Name`. An empty name or a missing name gives `null`. The walk allocates nothing, so mode-set can call it at build time and at the first diff, and cache the result (Q4 of the epic). A `null` root throws `ArgumentNullException`.
- [ ] 2.2 Write `tests/BrutalUi.Framework.Tests/Panels/SlotLookupTests.cs` with 8 tests. Use a private helper that builds a layout root with a slot `left` under a column element and a slot `right` as a direct child. Tests: `FindReturnsTheSlotOfTheName` (both slots, `Assert.Same`), `FindChecksTheRootItself` (a root that is a slot), `FindReturnsNullForAMissingName` (`Left`, `lef`, `leftx`, an empty name, and a plain element with the `Name` `missing` and no slot of that name), `FindReadsTheSlotNameAndNotTheElementName` (set `Name` of the slot `left` to `other`. `Find` of `left` gives the slot, and `Find` of `other` gives `null`), `FindReturnsTheFirstSlotInTreeOrder` (two slots `dup`: one at depth 3 in the first branch and one at depth 1 in a later branch. The deep slot wins, so a breadth-first walk fails the test), `FindDoesNotEnterTheChildrenOfASlot` (a panel root in the slot `left` holds a slot `inner`. `Find(layoutRoot, "inner"u8)` gives `null`, and `Find(panelRoot, "inner"u8)` gives the inner slot), `FindRejectsANullRoot` (`ArgumentNullException`, `ParamName` `root`), and `MountUnmountAndFindAllocateNothingAfterWarmUp`. The last test builds a layout root with the slots `left` and `right` and three panel roots before the measure. A private static step method moves each root from `left` to `right` (`Unmount`, then `Mount` at index `i`), moves the roots back to `left` in the reverse order, mounts one held root at index 0 (a move), and calls `Find` for `left`, `right`, and one missing name. It adds each `Unmount` result, each `Children.Count`, and each hit to a local `int` and returns it. The test runs 2 warm-up rounds, calls `AllocationGate.ReleaseAllocationContext()` right before the measure, runs 100 iterations, and asserts `Assert.Equal(0L, after - before)` and a fixed sum after the measure. The measured body creates no lambda, no string, no array, and no `Assert`, as the rule of `CommandAndEventAllocationTests`.
- [ ] 2.3 Write `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTreeTests.cs` with 5 tests. Each test builds a `UiContext` with a `RecordingRenderer` and a viewport of 400 by 300, and a layout root of 400 by 300 with `FlexDirection.Column`. Tests: `AStylesheetStylesASlotByTypeName` (a rule `Selector.ForType(ElementType.Intern("SlotContainer"))` sets a background color. After `Update(0f)`, the slot has the color, and a plain sibling element does not), `AStylesheetStylesASlotByClassAndByName` (a class rule and a rule `Selector.ForName("left")` each set a value on the slot), `TheLayoutPassPlacesTheMountedRootsInTheirOrder` (a slot with `FlexDirection.Column` holds roots with the heights 20, 30, and 40. After `Update(0f)`, `Layout.Rect.Y` of each root is 0, 20, and 50. After an unmount of the middle root and `Update(0f)`, the last root is at 20. After a mount of the middle root at 0 and `Update(0f)`, the order is 30, 20, 40 with the Y values 0, 30, and 50), `APanelThatMovesToAnotherSlotTakesTheStyleOfTheNewSlot` (a descendant rule for each slot class sets a different color on a `Label` of a panel. Unmount the panel from `left` and mount it in `right`, run `Update(0f)`, and read the color of `right`. This is the leave and the enter of A5 of the epic), and `AnUnmountedPanelKeepsItsBindingAndAppliesTheNewValueAfterTheMount` (a `Store<int>` and a `Label` in the panel with `LabelBindings.BindText`. Mount and `Update(0f)` give `1`. Unmount, `Set(2)`, and `Update(0f)` keep `1`. Mount and `Update(0f)` give `2`. `BindingCount` of the label stays 1).
- [ ] 2.4 Run the build, the tests, `SlotLookupTests` (total 8), and `SlotContainerTreeTests` (total 5). Expect `failed: 0`. Run three plants. Copy `SlotContainer.cs` to the scratchpad first. Plant 3: `GC.KeepAlive(new object());` as the first line of `Find`. Expect `MountUnmountAndFindAllocateNothingAfterWarmUp` to fail. Plant 4: the same line as the first line of `Mount`. Expect the same test to fail. Plant 5: remove the early `return` of the slot branch in the walk, so the walk enters the children of a slot when the name does not match. Expect `FindDoesNotEnterTheChildrenOfASlot` to fail. For each plant, run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Record the failed tests and the measured bytes. Restore the file and check it with `cmp`. Run the tests again and expect 0 failed. Record the results in `context.md`.
- [ ] 2.5 Edit `docs/technical-design.md`. Section 10: add a sub-section `### Slot containers` after "Panel definitions" and before the `---` line. List the rules of this feature: the type and the type name, the slot name copy and the element name, the mount rule (the index, the move with no join, the parent rule, the throw before a change), the keep rule of the unmount, the lookup rule (the tree order, no walk into a slot, the byte compare, the `null` result), the direct child list rule, the thread rule, and the 0-byte rule. End with one sentence: `SlotContainerTests`, `SlotLookupTests`, and `SlotContainerTreeTests` guard the rules (section 14). Do not change the Components table: the row `UiManager`, `SlotContainer` stays correct. Section 14: add a bullet `**Slot tests.**` after the bullet "Panel tests" and before "Sample projects". Name the three classes, their counts, the 0-byte test, and the plants of tasks 1.4 and 2.4 with their results. Do not call the 0-byte test a numbered gate (Decision 8). Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line after the panel-definition line: `SlotContainer` with `Mount`, `Unmount`, and `SlotContainer.Find` (panels-and-modes/slot-container).
- [ ] 2.6 Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the demo commands of both phases and expect the totals 10, 8, and 5 with `failed: 0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes. Record the demo, the results, and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 40 (6 tasks, about six to seven steps for one task)

**Files (one group, 6):** `SlotContainer.cs`, `SlotLookupTests.cs`, `SlotContainerTreeTests.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `SlotLookupTests` and the 5 tests of `SlotContainerTreeTests` pass. The 10 tests of `SlotContainerTests` still pass. Each existing test passes with no edit.
- [ ] The three plants of task 2.4 give a non-zero exit code, and the restored file equals its copy.
- [ ] `scripts/test-linux.sh` prints its PASS line.
- [ ] `src/BrutalUi.Core/`, each project file, and each existing test file have no diff.

---

## Key Technical Decisions

### Confirmed Assumptions

None. This plan is a draft with no interview.

### Decision 1: A sealed `Element` subtype in `src/BrutalUi.Framework/Panels/` (the chosen approach)
**Choice:** `SlotContainer` is a `sealed class` that derives from `Element`, with its own interned type name `SlotContainer`. It lives in `src/BrutalUi.Framework/Panels/` with the types of panel-definition, in the flat namespace `BrutalUi.Framework`. The mount, the unmount, and the lookup are members of the slot, and they call the public child list of Core.
**Rationale:** The requirements ask for an `Element` subtype that a stylesheet styles by type name. The pattern of `Image` and `Label` (a static `ElementTypeId` field and a `TypeId` override) gives the type rule with no Core change. `Element` has a public constructor and a public virtual `TypeId`, so a Framework type can derive from it (A2 of the epic). One folder for the epic keeps the panel types together. mosaic-ui puts the slot in `Layout/`, but brutal-ui has no Framework folder of that name, and the technical design names only the mosaic-ui source.
**Alternatives:** (a) A slot flag or a name map in `UiManager` over plain elements: no type rule for a stylesheet, and the requirements name a subtype. (b) A slot type in `BrutalUi.Core`: Core then holds a Framework concept, against A2 and the no-Core-change rule. (c) An open class: YAGNI. No sibling needs a subclass, and `sealed` keeps the type check of the parent rule exact.

### Decision 2: The slot name is a `byte[]` copy with a span property
**Choice:** The constructor takes `ReadOnlySpan<byte> name`, rejects an empty name with `ArgumentException`, and copies it with `ToArray()`. `SlotName` returns a span over the copy.
**Rationale:** The requirements ask for a UTF-8 slot name. The copy matches `CommandRegistry.Register` and the records of panel-definition (Q4 of panel-definition). A UTF-8 literal at the call site allocates nothing. The property name `SlotName` keeps `Element.Name` (a `string?`) visible.
**Alternatives:** A `string` slot name: a UTF-16 compare in the lookup, against the UTF-8 rule.

### Decision 3: The constructor sets `Element.Name` to the text of the slot name
**Choice:** The constructor sets `Name = Encoding.UTF8.GetString(name)`. `Find` reads `SlotName` only.
**Rationale:** The requirements ask that a stylesheet styles a slot by name. A name rule of Core matches `Element.Name`, a string. With the set, `#left` styles the slot `left` with no extra line in each layout factory. The string costs one build-time allocation. `FindReadsTheSlotNameAndNotTheElementName` pins the split, so a later set of `Name` cannot break the lookup. Notes question 2 holds the open choice.
**Alternatives:** (a) Leave `Name` `null`, and the game sets it: one more line in each factory, and a name rule that misses a slot by default. (b) Find the slot by `Element.Name`: a UTF-16 compare, and a lookup that a style rename breaks.

### Decision 4: `Mount` takes an `Element` and an index
**Choice:** `Mount(Element root, int index)`. The caller gives the root. mode-set calls `controller.EnsureRoot()` and passes the result.
**Rationale:** The slot then knows no controller, and a test mounts a plain element. The controller already owns the one-time build (panel-definition, A2). The index matches A4 of the epic: mode-set mounts the panels of a slot in the order of the mode definition. Notes question 3 holds the open choice.
**Alternatives:** (a) `Mount(PanelController controller, int index)` that calls `EnsureRoot`: one less line in mode-set, but each test needs a definition, a context, and a controller. (b) `Mount(PanelInstance instance, int index)`: the instance can be unbuilt, so the slot needs one more error path. (c) An append-only `Mount(root)`: no reorder, and the order of A4 then depends on the call order of mode-set.

### Decision 5: The parent rule and the move of a held root
**Choice:** A mount of a root that the slot holds moves it with `ElementList.Move`. A move to the same index marks nothing. A mount of a root with another parent throws `InvalidOperationException` before a change. The message names the case: another slot or another parent.
**Rationale:** `ElementList.Insert` removes a held child and adds it again. The add calls `AttachToParent`, which marks the full subtree for style, resets `IsStyled`, and drops the running tweens of the subtree (the join rule of Core). A panel that stays in its slot (A5 of the epic) must keep its state, so a move is the right call: it marks the slot for layout only. A loop of `Mount(root_i, i)` over the panels of a slot then gives the order of the mode for new roots and for held roots, with no extra API. The requirements ask for a throw for a root that another slot holds. A root under a plain parent is the same bug: Core would take it from that parent with no error. Notes question 4 holds the open choice.
**Alternatives:** (a) A held root also throws: mode-set then needs an unmount and a mount to reorder, and each reorder restyles the panel and drops its tweens. (b) Only a root in another slot throws: a root under a plain parent leaves that parent with no error.

### Decision 6: `Unmount` gives `bool` and keeps the root as it is
**Choice:** `Unmount` calls `Children.Remove(root)` and returns its result. The slot touches no other state of the root.
**Rationale:** The requirements ask for a detach that keeps the root and its subtree, and for `false` when the slot does not hold the root. `ElementList.Remove` keeps the dirty bits, the classes, the inline style, and the children. The binding pass skips an element outside the roots of the context and applies a new version at the next join (the leave rule of section 10). The focus rule of Core clears a stale focus owner at the next `Update`. The slot thus needs no code of its own for these states.
**Alternatives:** An unmount that clears the focus, the hover, or the bindings: more code, and a change of state that the requirements do not ask for.

### Decision 7: `Find` walks in depth-first pre-order and does not enter a slot
**Choice:** `SlotContainer.Find(Element root, ReadOnlySpan<byte> name)` checks the root, then each child in depth-first pre-order with a private recursive method. At a slot, it compares the name and does not walk into the children of the slot. The first match wins. A missing name or an empty name gives `null`. A `null` root throws `ArgumentNullException`.
**Rationale:** The requirements allow a walk, because the lookup runs at build time and at the first diff, and mode-set caches each result. Recursion over the span enumerator of `Children` allocates nothing, so the lookup costs 0 bytes also with no cache. A walk that skips the children of a slot gives a result that depends on the layout tree only: the mounted panels change neither the cost nor the result, and a slot inside a panel cannot hide a layout slot. The pre-order matches the order of a layout file.
**Alternatives:** (a) A breadth-first walk: it needs a queue, which allocates or needs a pool. (b) A walk into the panels: the result then depends on the mounted panels. (c) A throw for two slots with one name: the walk then has to visit the full tree each time. mode-set can check the names at register time.

### Decision 8: The 0-byte check is a test in `SlotLookupTests`, not a numbered gate
**Choice:** `MountUnmountAndFindAllocateNothingAfterWarmUp` measures the mount, the unmount, and the lookup in the shape of a gate, inside `SlotLookupTests`.
**Rationale:** The path is small: a parent check, a child list call, and a tree walk. panel-sample owns the epic gate `ModeDiffAllocationTests` (A7 of the epic), and it measures the mount and the unmount inside `SetMode` plus `Update`. The ordinals of section 14 thus stay free for it, as Decision 9 of panel-definition states.
**Alternatives:** A class `SlotAllocationTests` as the eighteenth gate: one more file, and an ordinal that panel-sample then has to move.

### Decision 9: No Core change
**Choice:** `BrutalUi.Core` gets no change.
**Rationale:** Each Core member that the slot needs is public: the `Element` constructor, the virtual `TypeId`, `ElementType.Intern`, `Parent`, `Name`, and `ElementList.Insert`, `Move`, `Remove`, and `IndexOf`. `Element.IsRoot` is internal, but `ElementList.Insert` already throws `InvalidOperationException` for a context root and for a cycle, before a change. The dirty bits and the binding marks of an attach and a detach come from Core. The style pass matches a type rule by `TypeId`, so a Framework subtype needs no registration.
**Alternatives:** A Core hook for a slot (for example, a public `IsRoot`): not needed.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A mount of a held root joins the root again, restyles the panel, and drops its tweens. | Medium | Medium | Decision 5 moves a held root with `ElementList.Move`. `AMountOfAHeldRootMovesItWithNoJoin` checks the style bit. |
| The game or mode-set changes the child list of a slot directly and skips the parent rule. | Low | Medium | The class remarks tell the reader to use `Mount` and `Unmount` only. Section 10 states the rule. |
| `Name` and `SlotName` differ after a set of `Name`. | Low | Low | Decision 3. The remarks state the split. `FindReadsTheSlotNameAndNotTheElementName` pins it. |
| Two slots in one layout have the same name. | Low | Medium | `Find` gives the first slot in tree order (Decision 7). The remarks call it a bug of the layout. mode-set can check at register time (Notes, Conflicts and Gaps item 3). |
| A deep layout overflows the stack of the recursive walk. | Low | Low | A layout tree is shallow, and the walk does not enter the panels. The Core passes recurse in the same way. |
| An unmounted panel holds the focus or the hover. | Medium | Low | The stale rules of Core clear the focus owner and the hover at the next `Update` (`FocusStateEdgeTests`, `PointerStateEdgeTests`). The slot adds no code. |
| An unmount drops the running tweens of the panel. | Medium | Low | The join rule of Core drops a tween on a remove. A9 of the epic puts panel transitions out of scope. The section 10 text states it. |
| The child array of a slot grows in a frame and allocates. | Low | Medium | The array grows to the largest count, then it never shrinks. The 0-byte test warms the largest count first. panel-sample warms each mode. |
| Parallel test classes share the class table. | Medium | Low | Each test interns its class names with its own prefix, as the Core tests do. |
| `dotnet build`, `dotnet test`, or `dotnet run` hangs on stale MSBuild nodes. | High | Low | Each command sets `MSBUILDDISABLENODEREUSE=1` and `-nodeReuse:false`. The class runs use the built binary. No `dotnet build-server shutdown`. |
| The panel-definition review fixes (CR-1 to CR-4) land during this feature. | Medium | Low | They change no public API and no test count. Task 1.1 records the counts at the base. |

---

## Dependencies

### Internal Dependencies
- `panels-and-modes/panel-definition` (done, on the branch): `PanelController.EnsureRoot()` and `PanelInstance.Root`, the source of each root that mode-set mounts. This feature names no panel-definition type in code (Decision 4).
- `core-tree` (done): `Element`, `ElementList`, `ElementType`, `UiContext`, the dirty bits, and the join rule.
- `stylesheet-model` and `selector-matching` (done): `StyleSheet`, `Selector.ForType`, `ForClass`, `ForName`, and the descendant rule (tests only).
- `state-and-commands` (done): `Store<T>` and `LabelBindings.BindText` (tests only).
- The test helper `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs` (done).
- `dev-ops`: `brutal-ui.sln` and `scripts/test-linux.sh`.

### External Dependencies
- None. No new package and no native dependency.

### Sibling Features That Depend on This One
- mode-set (`Find`, `Mount`, `Unmount`).
- panel-sample (the type name and the class of each slot, and the mount path inside `ModeDiffAllocationTests`).

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. A `SlotContainer` holds a copy of its UTF-8 name. A change of the source bytes changes no name. An empty name throws `ArgumentException` with the parameter `name`. `TypeId` reads `SlotContainer`, and `Name` is the text of the slot name (`SlotContainerTests`).
- [ ] F2. `Mount` puts a new root at its index, and the children keep the order of the calls. A mount of a held root moves it, sets no style bit on it, and a move to the same index marks nothing (`SlotContainerTests`).
- [ ] F3. `Unmount` gives `true`, detaches the root, and keeps the root, its children, its class, its inline style, and its name. A second mount puts the same root back. `Unmount` gives `false` for a root that the slot does not hold, and changes nothing (`SlotContainerTests`).
- [ ] F4. A mount of a root that another slot holds, or of a root with a plain parent, throws `InvalidOperationException` and leaves both parents unchanged. A `null` root throws `ArgumentNullException`. A bad index throws `ArgumentOutOfRangeException`. A context root and a cycle throw `InvalidOperationException`. Each throw leaves the slot unchanged (`SlotContainerTests`).
- [ ] F5. `Find` gives the slot of a name, checks the root itself, gives the first slot in depth-first pre-order, does not enter the children of a slot, reads `SlotName` and not `Name`, gives `null` for a missing or empty name, and throws `ArgumentNullException` for a `null` root (`SlotLookupTests`).
- [ ] F6. A type rule, a class rule, and a name rule style a slot. The layout pass places the mounted roots in the order of the slot. A panel that moves to another slot takes the style of the new slot. An unmounted panel keeps its binding and applies the new value after the next mount (`SlotContainerTreeTests`).
- [ ] F7. `Mount`, `Unmount`, and `Find` allocate 0 bytes after warm-up in Debug. Plants 3 and 4 of task 2.4 fail the test (`SlotLookupTests`).

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln -nodeReuse:false` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with the skipped count of `<base>`. `SlotContainerTests` holds 10 tests, `SlotLookupTests` 8, and `SlotContainerTreeTests` 5. `BrutalUi.Framework.Tests` holds 23 more tests than at `<base>`.
- [ ] Q3. Each public member of `SlotContainer.cs` has an XML doc comment. The new doc comments and the changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q4. `SlotContainer.cs` holds no `lock`, no `IDisposable`, no `event` keyword, no `ToString(` call, no `(object)` cast, no `new List`, and no `Stack<` or `Queue<` in code.

### Integration Criteria
- [ ] I1. The only new source file is `src/BrutalUi.Framework/Panels/SlotContainer.cs`. No file of `src/BrutalUi.Core/`, no other file of `src/BrutalUi.Framework/`, no project file, no file of `scripts/` or `samples/`, and no test file that existed at `<base>` changed.
- [ ] I2. `SlotContainer` derives from `Element`, is `sealed`, uses the namespace `BrutalUi.Framework`, and interns the type name `SlotContainer`. `Mount` calls `Children.Insert` and `Children.Move`, and `Unmount` calls `Children.Remove`.
- [ ] I3. Section 10 of `docs/technical-design.md` holds the sub-section `### Slot containers`. Section 14 holds the bullet `**Slot tests.**`. `CHANGELOG.md` names `SlotContainer`.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` and the base counts from `docs/features/panels-and-modes/slot-container/context.md`. Do not run `dotnet build-server shutdown`.

1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and the skipped count of `context.md` (Q2).
3. Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*SlotContainerTests' 2>&1 | tail -n 5`. Expect a total of 10 and `failed: 0` (F1 to F4). Run the same command with `'*SlotLookupTests'` and expect a total of 8 (F5, F7), and with `'*SlotContainerTreeTests'` and expect a total of 5 (F6). Run the binary with no filter and compare the total with the base count plus 23 (Q2). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Framework/Panels/SlotContainer.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `Find`. Run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp`. Repeat with the same line as the first line of `Mount` (F7). Repeat with the `throw` of the parent rule removed (F4). Run step 2 again.
5. `grep -nE '\block\b|IDisposable|\bevent\b|ToString\(|\(object\)|new List|Stack<|Queue<' src/BrutalUi.Framework/Panels/SlotContainer.cs | grep -v '///'`. Expect no line (Q4).
6. `grep -nE 'sealed class SlotContainer : Element|namespace BrutalUi.Framework;|ElementType.Intern\("SlotContainer"\)|Children.Insert|Children.Move|Children.Remove' src/BrutalUi.Framework/Panels/SlotContainer.cs`. Expect at least one line for each pattern (I2).
7. `git diff <base> --name-only -- src/ samples/ scripts/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/ scripts/`. Expect only `src/BrutalUi.Framework/Panels/SlotContainer.cs`, plus the six panel-definition files when `<base>` comes before their commit. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no output (I1).
8. `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4).
9. Read section 10 and section 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I3). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q3).

---

## Testing Strategy

### Unit Tests
- `Framework.Tests/Panels/SlotContainerTests` (10): the name copy, the empty name, the type and the element name, the mount order, the move of a held root with no join, the unmount that keeps the state, the `false` result, the root of another slot, the root of a plain parent, and the argument, index, context root, and cycle checks.
- `Framework.Tests/Panels/SlotLookupTests` (7 plus the 0-byte test): the hit, the root itself, the missing names, the split of `SlotName` and `Name`, the tree order, no walk into a slot, and the `null` root.

### Integration Tests
- `Framework.Tests/Panels/SlotContainerTreeTests` (5): the type rule, the class rule and the name rule, the layout order after a mount, an unmount, and a move, the style of a moved panel (A5 of the epic), and the binding of an unmounted panel (the leave rule). Each test runs `UiContext.Update` through the Core passes.

### Allocation Tests
- `MountUnmountAndFindAllocateNothingAfterWarmUp` in `SlotLookupTests` (Decision 8): 2 warm-up rounds, a release of the allocation context, 100 measured iterations of unmounts, mounts, a move, and three lookups, and an assert of 0 bytes and of a fixed sum. Plants 3 and 4 of task 2.4 guard it. The epic gate `ModeDiffAllocationTests` of panel-sample measures the full mode change with `Update` later.

### Regression Tests
- Each existing test runs with no edit. No existing file changes, so the 17 allocation gates keep their scenes and their counts.

### Manual Testing
- None. The feature has no visual result. panel-sample shows the slots in HelloPanel.

---

## Related Documentation

- **Requirements:** `docs/features/panels-and-modes/slot-container/requirements.md`
- **Epic:** `docs/features/panels-and-modes/epic-overview.md`, `docs/features/panels-and-modes/epic-requirements.md`
- **Siblings:** `docs/features/panels-and-modes/panel-definition/implementation.md` (the format reference), its `context.md` and `code-review.md`, `docs/features/panels-and-modes/mode-set/requirements.md`, `docs/features/panels-and-modes/panel-sample/requirements.md`
- **Design:** `docs/technical-design.md` sections 3 (package layout), 4 (frame order, no change), 5 (element tree), 7 (style system, the join rule), 10 (Framework Layer, "Panel definitions"), and 14 (Testing). `docs/mission-statement.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Image.cs` (the subtype and the type id), `src/BrutalUi.Core/Tree/ElementList.cs` (`Insert`, `Move`, `Remove`, and the checks), `src/BrutalUi.Core/Tree/Element.cs` (`AttachToParent`, the join rule, `Name`), `src/BrutalUi.Framework/Commands/CommandRegistry.cs` (the name copy and the messages), `src/BrutalUi.Framework/Panels/PanelController.cs` (the remark style), `tests/BrutalUi.Framework.Tests/Panels/PanelControllerTests.cs` (the 0-byte test shape), `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs`, `tests/BrutalUi.Core.Tests/Style/TypeSelectorTests.cs` (a sheet in a test), `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs` (a bound label in a test)
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (A) of each question. A change before task 1.2 costs little.

1. **Which demo does phase 1 use?**
   - **(A) Recommended:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*SlotContainerTests'` after a build and reads a total of 10 with `failed: 0`.
   - (B) a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the `SlotContainerTests` run reads a total of 10 with `failed: 0`.
2. **Does the constructor set `Element.Name` to the text of the slot name?**
   - **(A) Recommended:** Yes. A name rule (`#left`) styles the slot with no extra line, and `Find` reads `SlotName` only (Decision 3).
   - (B) No. `Name` stays `null`, and the game sets it when it wants a name rule.
3. **Which argument does `Mount` take?**
   - **(A) Recommended:** an `Element` root. mode-set calls `PanelController.EnsureRoot()` and passes the result (Decision 4).
   - (B) a `PanelController`. The slot calls `EnsureRoot` itself.
   - (C) a `PanelInstance`. The slot throws for an unbuilt instance.
4. **What does a mount do with a root that has a parent?**
   - **(A) Recommended:** a root that the slot holds moves to the index with no join. A root with any other parent throws `InvalidOperationException` (Decision 5).
   - (B) a root that the slot holds also throws. mode-set reorders with an unmount and a mount, and the panel restyles.
   - (C) only a root in another slot throws. A root under a plain parent leaves that parent with no error, as in Core.

### Conflicts and Gaps

1. **The requirements name `PanelInstance.Root` as a dependency.** With Decision 4, the slot names no panel-definition type in code. mode-set reads `PanelInstance.Root` or `EnsureRoot()` and passes the root. The dependency stays in the build order only.
2. **"The diff caches each result".** The cache belongs to mode-set. This feature gives a lookup that allocates nothing also with no cache, so the requirement "the cached lookup allocates nothing" holds for each call. The 0-byte test measures the lookup with no cache.
3. **Two slots with one name.** The requirements do not state the rule. The plan gives the first slot in tree order and does not throw (Decision 7). mode-set can reject a duplicate slot name at register time, when it walks the layout one time.
4. **A slot inside a panel.** `Find` does not enter the children of a slot, so `Find` on a layout root does not find a slot inside a mounted panel. The epic does not ask for nested slots. A caller can call `Find` on a panel root.
5. **The move of a held root is more than the requirements.** The requirements name a mount at an index and a throw for a root of another slot. Decision 5 adds the move with no join, because A5 of the epic keeps a staying panel in place with its state.
6. **The parent rule is stricter than the requirements.** A root under a plain parent also throws (Decision 5, Notes question 4).
7. **No Core change is needed.** Decision 9 holds the proof. mode-set adds the one Core slot `UiContext.OnBeforeUpdate` of the epic.
8. **The panel-definition review is open.** `code-review.md` of panel-definition holds four open findings (CR-1 to CR-4). None changes a type that this feature reads. The follow-up question on `PanelDefinition.DefaultSlot` belongs to mode-set.
9. **The prose lint did not run.** The planner had no shell tool, so it did not run `ste-lint.py` on this file. Task 2.6 runs it on this plan and on the changed docs.
10. **The plan is not compiled.** The planner did not compile `SlotContainer`. Task 1.2 is the first compile. A Framework subtype of `Element` has no precedent, but each member that it needs is public (Decision 9).

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
