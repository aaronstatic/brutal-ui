# Panel Definition - Implementation Plan

**Epic:** panels-and-modes (feature 1 of 4)
**Status:** Ready for Review
**Started:** 2026-10-30
**Target Completion:** 2026-10-30
**Last Updated:** 2026-10-30 05:32

---

## Quality Bar

**Target quality:** A library feature that sets the data model of the panels-and-modes epic. slot-container, mode-set, panel-sample, and later the xml-markup loader read these types. A wrong shape here costs a change in three sibling features. A root that a controller builds two times loses the state of a panel. A hidden allocation in `EnsureRoot` costs bytes at each mode change.

- **Reliability:** `EnsureRoot` calls the factory one time. After that, it gives the same `Element` until the controller is dropped. A factory that throws or returns `null` leaves the controller unbuilt, and the next call tries again (Q3). A call of `EnsureRoot` from inside its own factory throws `InvalidOperationException` and does not overflow the stack. A test pins each rule.
- **API clarity:** A game builds each record with one constructor call and UTF-8 literals. Each record validates at build time (A5). Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the copy rule, the build rule, the equality rule, and the error rule.
- **0 bytes after warm-up:** `EnsureRoot` on a built controller, `IsBuilt`, `Root`, each name read, and each getter of `ModeDefinition` allocate 0 bytes in the default Debug build. One test measures it, and a planted allocation fails it.
- **No Core change:** `BrutalUi.Core` gets no change. No project file changes.

---

## Executive Summary

This feature adds the data model of the panels-and-modes epic to `BrutalUi.Framework`, in the new folder `src/BrutalUi.Framework/Panels/`. It adds three records, one input struct, and three classes:

- `PanelDefinition` (a sealed record): a panel name, a default slot name, and a root factory `Func<PanelContext, Element>` (Q1).
- `LayoutDefinition` (a sealed record): a layout name and a factory for the layout root. The layout root holds the `SlotContainer` elements of slot-container.
- `ModeDefinition` (a sealed record): a mode name, a layout name, an ordered array of slot entries, and one optional `UiActionMap` (A4). The game gives the panels as a flat list of `ModePanel` values (a slot name and a panel name), and the record groups them by slot.
- `PanelContext` (a sealed class): the `CommandRegistry`, the `EventBus`, and the `ServiceRegistry` as `required` init members (Q2).
- `PanelInstance` (a sealed class): `Definition`, `Root` (`null` until built), and `IsBuilt` (A3).
- `PanelController` (a sealed class): one `PanelInstance`, the `PanelContext`, and `EnsureRoot()` (A2).

Each record copies its UTF-8 names to `byte[]` at build time, as `CommandRegistry` does (Q4). The records and the controller allocate at build time only.

The plan has 1 phase and 13 tasks in two task groups (A6). Group A delivers the records and their tests. Group B delivers the controller, the instance, their tests with the 0-byte check, and the docs. No phase needs an ADVANCED dev agent.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (declares each panel, layout, and mode one time at build time with UTF-8 literals, and gets one root for each panel that keeps its state)
- [x] **The sibling features** (slot-container mounts `PanelInstance.Root`. mode-set reads the records, holds one controller for each panel, compares names with no allocation, and activates `ModeDefinition.ActionMap`. xml-markup builds the same records from XML)
- [x] **The maintainer** (runs one more test class pair through `scripts/test-linux.sh` with no script change)

**User Subsets Served:**
- [x] **Simple** - one panel, one layout with one slot, and one mode with no action map.
- [x] **Complex** - many panels with default slots, two layouts, several modes that list three panels in two slots each, one action map for each mode, and factories that read services from the context.

**UX Approach:**
- One constructor call for each record. The names are UTF-8 literals (`"inventory"u8`). The factory is a method group or a `static` lambda.
- The mode panel list is a collection expression: `[new("left"u8, "inventory"u8), new("right"u8, "map"u8)]`.
- No builder, no `IDisposable`, no event keyword, and no closure in the frame path.

---

## Goals

### Primary Goals
1. Add `PanelDefinition`, `LayoutDefinition`, and `ModeDefinition` as sealed records with `byte[]` name copies and build-time validation (Q4, A4, A5).
2. Add `PanelContext` with the three registries as `required` init members (Q2).
3. Add `PanelInstance` and `PanelController` with `EnsureRoot()`, which builds the root one time and keeps it (A2, A3, Q3).
4. Prove 0 bytes after warm-up for each read and for `EnsureRoot` on a built controller.
5. State the rules in section 10 and section 14 of the technical design and in the changelog.

### Success Criteria
- [ ] The phase 1 demo passes: `scripts/test-linux.sh` prints its PASS line, and the new Panels tests (`PanelRecordTests` and `PanelControllerTests`) pass.
- [ ] `PanelControllerTests.EnsureRootAndTheReadsAllocateNothingAfterTheBuild` measures 0 bytes in Debug, and the plant of task 1.11 fails it.
- [ ] Each existing test passes with no edit.
- [ ] `src/BrutalUi.Core/` and each project file have no diff.

---

## Architecture Overview

### The Place in the Epic

This feature owns no tree change and no frame order change. The game builds the records and the context at build time. mode-set builds one `PanelController` for each registered panel. At the first mount of a panel, slot-container or mode-set calls `EnsureRoot()` and attaches the result. Each later mount calls `EnsureRoot()` again and gets the same root. A leave detaches the root (slot-container), and the controller keeps the reference, so the root keeps its state and its bindings.

```
Game (build time)
  ├─ PanelContext { Commands, Events, Services }
  ├─ PanelDefinition ×N   ── name, default slot, root factory
  ├─ LayoutDefinition ×M  ── name, layout root factory
  └─ ModeDefinition ×K    ── name, layout name, slots [ slot name, panel names[] ], UiActionMap?
            │
            ▼
mode-set: UiManager (later)        ── one PanelController for each PanelDefinition
            │ at the first mount
            ▼
PanelController.EnsureRoot() ──► PanelDefinition.CreateRoot(PanelContext) ──► PanelInstance.Root
            │ each later mount: the same Element, 0 bytes
            ▼
slot-container: SlotContainer mount / unmount (later)
```

### File Tree (end state)

```
src/BrutalUi.Framework/Panels/
├── PanelContext.cs           # group A (new): sealed class, three required init registries
├── PanelDefinition.cs        # group A (new): sealed record
├── LayoutDefinition.cs       # group A (new): sealed record
├── ModeDefinition.cs         # group A (new): sealed record ModeDefinition and readonly struct ModePanel
├── PanelInstance.cs          # group B (new): sealed class
└── PanelController.cs        # group B (new): sealed class with EnsureRoot
tests/BrutalUi.Framework.Tests/Panels/
├── PanelRecordTests.cs       # group A (new): 14 tests
└── PanelControllerTests.cs   # group B (new): 10 tests
docs/technical-design.md      # group B: section 10 (new sub-section "Panel definitions") and section 14 (one bullet)
CHANGELOG.md                  # group B: one line under Added
docs/features/panels-and-modes/panel-definition/context.md   # each group
```

These files do not change: each file of `src/BrutalUi.Core/`, each other file of `src/BrutalUi.Framework/`, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file. The new test files compile in `BrutalUi.Framework.Tests` with no project edit. The namespaces stay flat: the source files use `BrutalUi.Framework`, and the test files use `BrutalUi.Framework.Tests`, as `Actions/` and `Commands/` do.

### Public API (end state)

```csharp
namespace BrutalUi.Framework;

public sealed class PanelContext
{
    public required CommandRegistry Commands { get; init; }   // ArgumentNullException for null
    public required EventBus Events { get; init; }             // ArgumentNullException for null
    public required ServiceRegistry Services { get; init; }    // ArgumentNullException for null
}

public sealed record PanelDefinition
{
    public PanelDefinition(ReadOnlySpan<byte> name, ReadOnlySpan<byte> defaultSlot, Func<PanelContext, Element> createRoot);
    public ReadOnlySpan<byte> Name { get; }
    public ReadOnlySpan<byte> DefaultSlot { get; }
    public Func<PanelContext, Element> CreateRoot { get; }
}

public sealed record LayoutDefinition
{
    public LayoutDefinition(ReadOnlySpan<byte> name, Func<PanelContext, Element> createRoot);
    public ReadOnlySpan<byte> Name { get; }
    public Func<PanelContext, Element> CreateRoot { get; }
}

public readonly struct ModePanel
{
    public ModePanel(ReadOnlySpan<byte> slot, ReadOnlySpan<byte> panel);
    public ReadOnlySpan<byte> Slot { get; }
    public ReadOnlySpan<byte> Panel { get; }
}

public sealed record ModeDefinition
{
    public ModeDefinition(ReadOnlySpan<byte> name, ReadOnlySpan<byte> layout, ReadOnlySpan<ModePanel> panels, UiActionMap? actionMap = null);
    public ReadOnlySpan<byte> Name { get; }
    public ReadOnlySpan<byte> Layout { get; }
    public UiActionMap? ActionMap { get; }
    public int SlotCount { get; }
    public ReadOnlySpan<byte> GetSlotName(int slot);
    public int GetPanelCount(int slot);
    public ReadOnlySpan<byte> GetPanelName(int slot, int index);
}

public sealed class PanelInstance
{
    // The constructor is internal. PanelController creates the one instance.
    public PanelDefinition Definition { get; }
    public Element? Root { get; }                                  // null until built
    [MemberNotNullWhen(true, nameof(Root))] public bool IsBuilt { get; }
}

public sealed class PanelController
{
    public PanelController(PanelDefinition definition, PanelContext context);
    public PanelDefinition Definition { get; }
    public PanelContext Context { get; }
    public PanelInstance Instance { get; }
    public Element EnsureRoot();
}
```

### Internal Shape

- **The name copy (Q4).** Each record constructor copies each name with `name.ToArray()` into a `private readonly byte[]` field. Each name property is an expression body over the field: `public ReadOnlySpan<byte> Name => _name;`. The property has no backing field of a span type, and a read allocates nothing. A change of the source array after the build changes nothing.
- **The validation (A5).** Each record checks its arguments before it copies. An empty name throws `ArgumentException` with a constant message and the parameter name (`name`, `defaultSlot`, `layout`, `slot`, `panel`, or `panels`). A `null` factory throws `ArgumentNullException` (`ArgumentNullException.ThrowIfNull(createRoot)`), a subtype of `ArgumentException`, as `CommandRegistry.Register` does for a `null` handler (Notes, Conflicts and Gaps item 1).
- **`PanelContext` (Q2).** Each member is `public required T X { get => _x; init { ArgumentNullException.ThrowIfNull(value, nameof(X)); _x = value; } }` with a `private readonly T _x = null!;` field. The class has no constructor other than the default one. The members are read-only after the initializer. The context holds no other state.
- **The record text.** Each record declares `private bool PrintMembers(StringBuilder builder)` (the signature for a sealed record). It writes the names as text with `Encoding.UTF8.GetString`. `PanelDefinition` writes `Name = inventory, DefaultSlot = left`. `LayoutDefinition` writes `Name = main`. `ModeDefinition` writes `Name = explore, Layout = main, SlotCount = 2`. The explicit method keeps the compiler from a synthesized `PrintMembers` over a `ReadOnlySpan<byte>` property (Risks), and it gives a readable `ToString`. `ToString` allocates. The remarks state that it is for a debugger and a log, not for a frame.
- **The record equality.** Each record keeps the equality that the compiler generates. It compares the `byte[]` copies by reference, so two records from two constructor calls are not equal, and a `with` copy equals its source. The remarks state the rule and tell the reader to compare names with `Name.SequenceEqual(other)` (Decision 6, Notes question 1). No record has an `init` member, so a `with` expression cannot change a member and cannot skip the validation.
- **`ModePanel`.** A `readonly struct` with two `private readonly byte[]? _slot, _panel` fields. The constructor checks both names (an empty name throws `ArgumentException` with the parameter `slot` or `panel`) and copies them. `Slot` and `Panel` return the span (empty for `default(ModePanel)`). `ModeDefinition` reads the arrays through two `internal` properties `SlotBytes` and `PanelBytes` and reuses them, because no code writes them.
- **`ModeDefinition` (A4).** `private readonly SlotEntry[] _slots;` with `private readonly struct SlotEntry { public readonly byte[] Name; public readonly byte[][] Panels; }`. The constructor walks `panels` one time in order. For each entry, it checks that the slot and the panel are not empty (a `default(ModePanel)` throws `ArgumentException` with the parameter `panels`), and that no earlier entry names the same panel (`ArgumentException`, parameter `panels`, Decision 5). It finds the slot by a byte compare and appends a new slot when no earlier entry names it. The slots thus keep the order of their first entry, and the panels of a slot keep the order of the list. The constructor builds the arrays with exact sizes. An empty list gives `SlotCount` 0. `GetSlotName`, `GetPanelCount`, and `GetPanelName` check each index with `ArgumentOutOfRangeException.ThrowIfNegative` and `ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual` (parameter `slot` or `index`), then read the arrays. The record does not check that the layout or a panel exists. mode-set checks the names at register time.
- **`PanelInstance` (A3).** `Definition` is set in the `internal` constructor. `Root` has a private setter. `internal void SetRoot(Element root)` writes it. `IsBuilt => Root is not null`, with `[MemberNotNullWhen(true, nameof(Root))]`, so `if (instance.IsBuilt) instance.Root.X` needs no `!`.
- **`PanelController` (A2, Q3).** The constructor checks both arguments with `ArgumentNullException.ThrowIfNull` and creates `Instance = new PanelInstance(definition)`. `private bool _building;` guards the re-entry. `EnsureRoot()`:
  ```
  Element? root = Instance.Root;
  if (root is not null) return root;                 // the warm path: one field read, 0 bytes
  if (_building) throw new InvalidOperationException(ReentryMessage);
  _building = true;
  try { root = Definition.CreateRoot(Context); }     // an exception of the factory leaves the call unchanged
  finally { _building = false; }
  if (root is null) throw new InvalidOperationException(NullRootMessage);
  Instance.SetRoot(root);
  return root;
  ```
  The messages are `private const string` fields. A factory exception leaves `Instance.Root` `null`, so the next call calls the factory again (Q3). The controller does not check the parent of the root. slot-container owns the mount rule.
- **The thread rule.** Each type runs on the UI thread and uses no lock, as the registries do.

### Integration Points

| Sibling or caller | What it reads | Contract that this feature fixes |
|---|---|---|
| slot-container | `PanelInstance.Root`, `PanelController.EnsureRoot()` | `Root` is `null` until the first `EnsureRoot`, then the same `Element` for the life of the controller. The controller never detaches or replaces it. |
| mode-set (`UiManager`) | `PanelDefinition.Name`, `DefaultSlot`, `LayoutDefinition.Name`, `CreateRoot`, `ModeDefinition` getters, `ActionMap`, `PanelController` | The names are spans over stable copies, so a compare with `SequenceEqual` allocates nothing. The slot order and the panel order of a mode are fixed at build time. No panel is in a mode two times. The manager calls `LayoutDefinition.CreateRoot` with the same `PanelContext`. |
| mode-set (map switch) | `ModeDefinition.ActionMap` | `null` means "no map", and the diff calls `Deactivate` on the old map (A6 of the epic). |
| panel-sample | Each type | The factory binds the root to a store through the existing binding pass (A7). |
| xml-markup (later epic) | The constructors | A loader calls the same constructors with the bytes of the XML names and gives a factory for each root. |

### Data Flow

```
Build time (allocates one time):
  ctx = new PanelContext { Commands = commands, Events = events, Services = services }
  inventory = new PanelDefinition("inventory"u8, "left"u8, BuildInventory)   ──► _name, _defaultSlot (copies)
  main = new LayoutDefinition("main"u8, BuildMainLayout)
  explore = new ModeDefinition("explore"u8, "main"u8,
      [new("left"u8, "inventory"u8), new("left"u8, "stats"u8), new("right"u8, "map"u8)], exploreMap)
                                                     ──► _slots = [ left: [inventory, stats], right: [map] ]
  controller = new PanelController(inventory, ctx)   ──► Instance (unbuilt)

The first mount (slot-container or mode-set):
  controller.EnsureRoot() ──► BuildInventory(ctx) ──► Instance.Root = root

Each later mount and each read (0 bytes):
  controller.EnsureRoot() ──► the same root
  explore.GetPanelName(0, 1) ──► "stats"u8
```

---

## Implementation Phases

The feature has one phase (A6). It is a slice: the path from a record to a built root, with a demo that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The phase has 13 tasks, above the 8 tasks of Slice rule 7. A6 confirms one phase, so the phase uses two task groups (Slice rule 3, Notes, Conflicts and Gaps item 4).

Each group ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member of `src/` needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `System.Text`, `System.Diagnostics.CodeAnalysis`, `BrutalUi.Core`), also for each namespace that a `cref` names. Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

### Phase 1: The Panel Data Model and the One-Time Root
**Goal:** A game declares a panel, a layout, and a mode as records with UTF-8 names, gives a `PanelContext`, and builds a `PanelController`. The first `EnsureRoot()` calls the factory with the context and keeps the root. Each later call gives the same root with 0 bytes.
**Demo:** a developer runs `scripts/test-linux.sh` and reads its PASS line, and the new Panels tests pass: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*PanelRecordTests'` reads a total of 14 and `--filter-class '*PanelControllerTests'` reads a total of 10, each with `failed: 0`.
**Needs:** none
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED only when the compiler rejects the record shape of Decision 2 and the fallback of the Risks table does not fix it.

**Tasks (group A: the records):**
- [ ] 1.1 Create `docs/features/panels-and-modes/panel-definition/context.md`. Check that the epic plan commit exists (`git log --oneline -3`, expect `43d3ea0` or later). Record the base commit (`git rev-parse HEAD`), the test count of the solution (with the skipped count), and the count of `BrutalUi.Framework.Tests`. Record the Internal Shape and the Integration Points of this plan.
- [ ] 1.2 Write `src/BrutalUi.Framework/Panels/PanelContext.cs`: the sealed class of the Public API with the three `required` init members and the null check of the Internal Shape. The class remarks: the game creates one context at build time and gives it to each controller (mode-set gives it to the manager). The factory of a panel or a layout reads the registries from it. The context holds no other state and owns no registry. It runs on the UI thread.
- [ ] 1.3 Write `src/BrutalUi.Framework/Panels/PanelDefinition.cs`: the sealed record of the Public API, with the name copy, the validation, the explicit `PrintMembers`, and the remarks. The remarks state: the record copies each name at build time. The factory runs one time for each controller, at the first mount, so a closure costs one build-time allocation only. The factory can bind the root to a store (A7). `DefaultSlot` names the slot of the panel when a mode does not name one (mode-set uses it). The equality rule and the text rule of the Internal Shape.
- [ ] 1.4 Write `src/BrutalUi.Framework/Panels/LayoutDefinition.cs`: the sealed record of the Public API, in the shape of task 1.3. The remarks: the root of the layout holds the `SlotContainer` elements of slot-container, and the manager finds each one by name (Q4 of the epic). The layout root is the one `UiContext` root (A4 of the epic). The factory gets the same `PanelContext` as the panels.
- [ ] 1.5 Write `src/BrutalUi.Framework/Panels/ModeDefinition.cs`: the readonly struct `ModePanel` and the sealed record `ModeDefinition` of the Public API and the Internal Shape (the grouping, the duplicate check, the index checks, `PrintMembers`). The remarks of `ModeDefinition`: the slot order is the order of the first entry of each slot. The panel order of a slot is the order of the list (A4). A panel is in one slot of a mode at most. `ActionMap` is optional, and mode-set activates it (A6 of the epic). The record checks no name against a registered panel or layout. Each getter allocates nothing. The remarks of `ModePanel`: one placement of a panel in a slot, an input of the `ModeDefinition` constructor. A UTF-8 literal is a span and cannot sit in an array, so the struct copies the two names.
- [ ] 1.6 Write `tests/BrutalUi.Framework.Tests/Panels/PanelRecordTests.cs` with 14 tests. Use a private static factory method `static Element NewRoot(PanelContext context) => new Element();` in a `static readonly Func<PanelContext, Element>` field, and a private helper that builds a `PanelContext` with three new registries. Tests: `PanelContextHoldsTheThreeRegistries`, `PanelContextRejectsANullRegistry` (each member, `ArgumentNullException`), `APanelDefinitionHoldsItsNamesAndItsFactory` (`SequenceEqual` of both names, `Assert.Same` of the factory), `APanelDefinitionCopiesItsNames` (build from two `byte[]` sources, change a byte of each source, read the old names), `APanelDefinitionRejectsAnEmptyNameAnEmptySlotAndANullFactory` (`ArgumentException` with `ParamName` `name` and `defaultSlot`, and `ArgumentNullException` with `createRoot`), `ALayoutDefinitionHoldsACopyOfItsNameAndItsFactory`, `ALayoutDefinitionRejectsAnEmptyNameAndANullFactory`, `AModeDefinitionGroupsThePanelsBySlotInTheOrderOfTheList` (entries left:a, right:c, left:b give `SlotCount` 2, slot 0 `left` with `a`, `b`, and slot 1 `right` with `c`), `AModeDefinitionHoldsItsLayoutAndItsOptionalActionMap` (`Assert.Same` of a map, and `null` with no map), `AModeDefinitionCopiesItsNames`, `AModeDefinitionWithNoPanelHoldsNoSlot`, `AModeDefinitionRejectsAnEmptyNameAnEmptyLayoutAnEmptyEntryAndAPanelThatItListsTwoTimes` (also `default(ModePanel)`, and one panel in the same slot and in two slots), `TheModeGettersRejectAnIndexOutsideTheRange` (-1 and the count, for `slot` and for `index`), and `ToStringShowsTheNamesAsText` (`PanelDefinition { Name = inventory, DefaultSlot = left }` and the two other formats of the Internal Shape).
- [ ] 1.7 Run the build, the tests, and `PanelRecordTests` (total 14). Expect `failed: 0`, and each other test passes with no edit. Record the results in `context.md`.

**Tasks (group B: the controller and the docs):**
- [ ] 1.8 Write `src/BrutalUi.Framework/Panels/PanelInstance.cs`: the sealed class of the Public API and the Internal Shape. The remarks: the controller creates the one instance and writes `Root` one time. `Root` is `null` until the first `EnsureRoot`. After that, it is the same `Element` for the life of the controller, also while the root is detached from each slot (Q3). slot-container mounts `Root`. After this task, record in `context.md` that the shape of `PanelInstance` is set, so slot-container can start (requirements, Dependencies).
- [ ] 1.9 Write `src/BrutalUi.Framework/Panels/PanelController.cs`: the sealed class of the Public API with `EnsureRoot` of the Internal Shape. The remarks state: the controller owns one `PanelInstance` and reaches the registries through `Context`. `EnsureRoot` calls the factory one time at the first call and gives the same root after that, with 0 bytes. A factory exception leaves the call, the controller stays unbuilt, and the next call tries again (Q3). A `null` root throws `InvalidOperationException`, and the controller stays unbuilt. A call from inside the factory throws `InvalidOperationException`. The controller keeps the root for its life and never detaches it. slot-container and mode-set call `EnsureRoot` at the mount (A2). The controller does not bind the root to a store. The factory does that (A7).
- [ ] 1.10 Write `tests/BrutalUi.Framework.Tests/Panels/PanelControllerTests.cs` with 10 tests. Use a private sealed `RootFactory` class with an `int Calls` field, a `PanelContext? Seen` field, a `bool Throw` field, and a `bool ReturnNull` field, and an instance method `Element Create(PanelContext context)` that the test passes as a method group at build time. Tests: `EnsureRootBuildsTheRootOneTime` (the demo test: the first call gives a root and `Calls` 1, `IsBuilt` is `true`, and `Instance.Root` is the same element. A second call gives the same element and `Calls` stays 1), `ANewControllerHoldsAnUnbuiltInstance` (`IsBuilt` `false`, `Root` `null`, `Definition` and `Instance.Definition` are the definition, and `Context` is the context), `TheFactoryGetsTheContextOfTheController` (`Seen` is the same context, and the factory reads `Commands`, `Events`, and `Services`), `AFactoryThatThrowsLeavesTheControllerUnbuiltAndTheNextCallTriesAgain` (the first call throws the exception of the factory with no wrap, and `IsBuilt` stays `false`. With `Throw` set to `false`, the next call builds, and `Calls` is 2), `AFactoryThatReturnsNullThrowsAndLeavesTheControllerUnbuilt` (`InvalidOperationException`), `ACallFromInsideTheFactoryThrows` (a factory that calls `EnsureRoot` of its own controller gets `InvalidOperationException`, stores it, and returns a root. The outer call builds), `TwoControllersOfOneDefinitionBuildTwoRoots`, `TheRootStaysTheSameAfterADetach` (`UiContext.AddRoot` of the root, then `RemoveRoot`. `EnsureRoot` gives the same element, and `Calls` stays 1), `TheConstructorRejectsANullDefinitionOrContext`, and `EnsureRootAndTheReadsAllocateNothingAfterTheBuild`. The last test builds a controller, a mode with three entries in two slots, and a layout before the measure. It runs 2 warm-up rounds, calls `AllocationGate.ReleaseAllocationContext()` right before the measure, and runs 100 iterations of: `EnsureRoot`, `IsBuilt`, `Instance.Root`, the length of each name of the panel and the layout, and each getter of the mode over each slot and panel. It adds each length and count to a local `int` and asserts `Assert.Equal(0L, after - before)` and the sum after the measure. The measured body creates no lambda, no string, no array, and no `Assert`, as the rule of `CommandAndEventAllocationTests`.
- [ ] 1.11 Run the build, the tests, and `PanelControllerTests` (total 10). Expect `failed: 0`. Run two plants. Copy `PanelController.cs` to the scratchpad first. Plant 1: `GC.KeepAlive(new object());` as the first line of `EnsureRoot`. Expect `EnsureRootAndTheReadsAllocateNothingAfterTheBuild` to fail. Plant 2: remove the early `return` of the warm path. Expect `EnsureRootBuildsTheRootOneTime` to fail. For each plant, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file and check it with `cmp`. Run the tests again and expect 0 failed. Record the results in `context.md`.
- [ ] 1.12 Edit `docs/technical-design.md`. Section 10: add a sub-section `### Panel definitions` after "Commands, events, and services" and before the `---` line. List the rules of this feature: the records and their names (Q4), the validation (A5), the equality rule and the text rule, the mode slot order (A4), the context (Q2), the controller and `EnsureRoot` (A2, Q3), the error rule, the thread rule, and the 0-byte rule. End with one sentence: `PanelRecordTests` and `PanelControllerTests` guard the rules (section 14). In the Components table, change the `ModeHistory` cell text only when it names a wrong feature (it reads "waits for panels-and-modes", which is correct, so expect no change). Section 14: add a bullet `**Panel tests.**` after the bullet "A seventeenth allocation gate" and before "Sample projects". Name the two classes, their counts, the 0-byte test, and the two plants of task 1.11. Do not call the 0-byte test a numbered gate (Decision 9). Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `ModePanel`, `PanelContext`, `PanelInstance`, and `PanelController` (panels-and-modes/panel-definition).
- [ ] 1.13 Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the two demo commands of the phase and expect the totals 14 and 10 with `failed: 0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes. Record the demo, the results, and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 80 (13 tasks, about six steps for one task)

**Files (group A, 6):** `PanelContext.cs`, `PanelDefinition.cs`, `LayoutDefinition.cs`, `ModeDefinition.cs`, `PanelRecordTests.cs`, `context.md`
**Files (group B, 6):** `PanelInstance.cs`, `PanelController.cs`, `PanelControllerTests.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 14 tests of `PanelRecordTests` and the 10 tests of `PanelControllerTests` pass. Each existing test passes with no edit.
- [ ] Both plants of task 1.11 give a non-zero exit code of `dotnet test`, and the restored file equals its copy.
- [ ] `scripts/test-linux.sh` prints its PASS line.
- [ ] `src/BrutalUi.Core/`, each project file, and each existing test file have no diff.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Root factory input: the root factory is `Func<PanelContext, Element>`.
- Q2 Registry handoff: one sealed `PanelContext` class holds the three registries as `required` init members.
- Q3 Factory throw: a factory throw leaves the call, and the controller stays unbuilt. The next mount tries again.
- Q4 Name storage: each record copies its UTF-8 names to `byte[]` at build time, as `CommandRegistry` does.
- A1 The types go in `src/BrutalUi.Framework/Panels/`. The tests go in `tests/BrutalUi.Framework.Tests/Panels/`.
- A2 `PanelController` gives `EnsureRoot()`. It builds the root on the first call, and it gives the same `Element` after that. slot-container and mode-set call it at the mount.
- A3 `PanelInstance` is a sealed class with `Definition`, `Root` (null until built), and `IsBuilt`.
- A4 `ModeDefinition` holds an ordered array of slot entries. Each entry holds a slot name and an ordered array of panel names.
- A5 A record validates at build time: an empty name or a null factory throws `ArgumentException`.
- A6 The feature has one phase. Its demo: `scripts/test-linux.sh` runs, and the new Panels tests pass.
- A7 No store binding API goes in this feature. The factory binds the root to a store through the existing binding pass.

### Decision 1: Sealed records with constructors and `byte[]` names (the chosen approach)
**Choice:** `PanelDefinition`, `LayoutDefinition`, and `ModeDefinition` are `sealed record` types with one explicit constructor each. The constructor takes `ReadOnlySpan<byte>` names and copies them to private `byte[]` fields. Each name is a `ReadOnlySpan<byte>` property with an expression body.
**Rationale:** The requirement asks for plain C# records with UTF-8 names (Q4). The span input takes a UTF-8 literal with no allocation at the call site, and the copy matches `CommandRegistry.Register`. A positional record cannot take a span, because a positional parameter becomes a property with a backing field, and a class cannot hold a span field. The constructor also runs the validation of A5 in one place.
**Alternatives:** (a) Interface-based definitions: rejected, YAGNI until the xml-markup epic. (b) A `required` create-info struct for each record: three records with two or three members each gain nothing from it, and a create-info struct cannot hold a span unless it is a `ref struct`.

### Decision 2: An explicit `PrintMembers` in each record
**Choice:** Each record declares `private bool PrintMembers(StringBuilder builder)` and writes its names as UTF-8 text.
**Rationale:** The compiler synthesizes `PrintMembers` over each public property. A `ReadOnlySpan<byte>` property cannot be boxed into the builder, and the planner cannot prove the compiler skips it (Risks). An explicit method removes the doubt. It also gives a useful `ToString` in a debugger: a synthesized text of a `byte[]` reads `System.Byte[]`.
**Alternatives:** Rely on the synthesized method: a possible compile error, and a useless text at best.

### Decision 3: The mode takes a flat list of `ModePanel` values and stores slot entries
**Choice:** The `ModeDefinition` constructor takes `ReadOnlySpan<ModePanel> panels`. Each `ModePanel` holds a copy of a slot name and a panel name. The constructor groups the list into the slot entries of A4: the slot order is the order of the first entry of each slot, and the panel order is the order of the list.
**Rationale:** A UTF-8 literal is a `ReadOnlySpan<byte>`, and a span cannot be an element of an array or of a span. A slot entry with an array of panel names thus has no literal syntax. A flat list of small structs works with a collection expression, keeps each record immutable, and stores the shape of A4. It also matches a future XML loader, which reads one element for each placement.
**Alternatives:** (a) A mutable slot builder (`new ModeSlot("left"u8).Add("a"u8)`): a mutable object inside a record, and a `with` copy that shares its arrays. (b) An `AddPanel` method on `ModeDefinition`: a mutable record. (c) Panel names as `byte[][]` input: `"a"u8.ToArray()` at each call site. (d) `PanelDefinition` references in place of names: the mode then needs the panels at build time, and A4 names panel names. Notes question 2 holds the open choice.

### Decision 4: A `null` factory throws `ArgumentNullException`
**Choice:** A `null` factory throws `ArgumentNullException`. An empty name throws `ArgumentException`.
**Rationale:** `ArgumentNullException` is a subtype of `ArgumentException`, so the rule of A5 holds. The codebase uses `ArgumentNullException` for each `null` argument (`CommandRegistry.Register`, `ServiceRegistry.Register`, `UiActionMap`). The tests assert the exact type and the parameter name.
**Alternatives:** `ArgumentException` for both: a break with the convention of the codebase.

### Decision 5: A mode rejects a panel that it lists two times
**Choice:** The `ModeDefinition` constructor throws `ArgumentException` (parameter `panels`) when two entries name the same panel, in one slot or in two slots. A slot name in two entries is valid and groups the panels.
**Rationale:** A panel has one root, and an `Element` has one parent. slot-container throws `InvalidOperationException` for a second mount. A build-time throw shows the bug at the declaration, not at the first mode change in a frame. A5 lists the minimum checks. This check adds one byte compare loop at build time.
**Alternatives:** No check: the bug shows as an exception inside the diff of mode-set.

### Decision 6: The records keep the equality of the compiler
**Choice:** No record overrides `Equals` or `GetHashCode`. The generated equality compares the `byte[]` copies by reference. The remarks state it, and they tell the reader to compare names with `SequenceEqual`.
**Rationale:** YAGNI. No sibling compares two records: mode-set keys each record by its name. A byte equality for `ModeDefinition` needs a deep compare of nested arrays, and an override pair adds code and tests for no use. Notes question 1 holds the open choice.
**Alternatives:** An override that compares the names by bytes: value equality for a record, at the cost of two methods in each record.

### Decision 7: `EnsureRoot` guards the re-entry and rejects a `null` root
**Choice:** A `_building` flag makes a call from inside the factory throw `InvalidOperationException`. A `null` root throws `InvalidOperationException`. Both leave the controller unbuilt.
**Rationale:** With no guard, a factory that calls its own controller recurses until the stack overflows, and a stack overflow ends the process with no catch. A `null` root would make `IsBuilt` stay `false` and the next mount call the factory again, against Q3. The flag resets in a `finally` block, so a factory exception keeps Q3: the next call tries again.
**Alternatives:** No guard: a crash of the process for a rare bug.

### Decision 8: One `PanelContext` for panels and layouts
**Choice:** `LayoutDefinition.CreateRoot` is a `Func<PanelContext, Element>`, the same type as the panel factory (Q1).
**Rationale:** A layout root can read a service (for example a theme) as a panel does. One delegate type and one context keep the manager simple: it holds one context and calls each factory with it.
**Alternatives:** `Func<Element>` for the layout: no access to the registries, and a second factory shape.

### Decision 9: The 0-byte check is a test in `PanelControllerTests`, not a numbered gate
**Choice:** `EnsureRootAndTheReadsAllocateNothingAfterTheBuild` measures the reads and the warm `EnsureRoot` in the shape of a gate, inside `PanelControllerTests`.
**Rationale:** The warm path is one field read. A full gate class with a scene and a real-work test is out of proportion. The precedent is `ALivePollAllocatesNothing` in `GlfwUiInputGamepadTests`. panel-sample owns the epic gate `ModeDiffAllocationTests` (A7 of the epic), so the gate ordinals of section 14 stay free for it.
**Alternatives:** A new class `PanelAllocationTests` as the eighteenth gate: one more file over the file limit of a task group, and an ordinal that panel-sample then has to move.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The compiler rejects a record with a `ReadOnlySpan<byte>` property (a synthesized member over the span). | Low | Medium | Decision 2 declares `PrintMembers`. The name properties have expression bodies and no backing field. When a synthesized member still fails, the fallback is a sealed class with the same public members, and a note in `context.md` (the agent tier line). |
| The generated record equality surprises a user (two records with the same names are not equal). | Medium | Low | Decision 6. The remarks state the rule. Notes question 1. |
| A sibling needs another shape of `PanelInstance` or of the mode getters. | Low | Medium | The Integration Points table fixes the contract. A3 and A4 set the shape. mode-set can add a lookup method to `ModeDefinition` with no break. |
| A factory calls `EnsureRoot` of its own controller and overflows the stack. | Low | High | Decision 7 and `ACallFromInsideTheFactoryThrows`. |
| A factory returns an element that has a parent or that another controller holds. | Low | Medium | slot-container throws `InvalidOperationException` at the mount. The remarks of `CreateRoot` state that the factory returns a new, detached element. |
| The warm path allocates in the Debug build (a boxed value or a closure). | Low | Medium | The warm path is a field read and a `null` test. The 0-byte test and plant 1 of task 1.11 guard it. |
| The `required` and `init` members with a null check do not compile as planned. | Low | Low | A `required` member can have an `init` accessor with a body (C# 11 and later). The fallback is a constructor check in `PanelController`, with a note in `context.md`. |
| The mosaic-ui names differ from the names of this plan. | Medium | Low | The names follow the requirements and section 10 of the technical design. A rename before slot-container costs little. |

---

## Dependencies

### Internal Dependencies
- `state-and-commands` (done): `CommandRegistry`, `EventBus`, `ServiceRegistry` (the members of `PanelContext`) and `UiActionMap` (`ModeDefinition.ActionMap`).
- `core-tree` (done): `Element` (the root type) and `UiContext.AddRoot` and `RemoveRoot` (the detach test).
- The test helper `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs` (done).
- `dev-ops`: `brutal-ui.sln` and `scripts/test-linux.sh`.

### External Dependencies
- None. No new package and no native dependency.

### Sibling Features That Depend on This One
- slot-container (`PanelInstance.Root`, `EnsureRoot`). It can start after task 1.8.
- mode-set (each type).
- panel-sample (each type, through mode-set).

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. `PanelContext` holds the three registries as `required` init members, and a `null` value for each member throws `ArgumentNullException` (`PanelRecordTests`).
- [ ] F2. `PanelDefinition` and `LayoutDefinition` hold copies of their names and their factory. A change of the source bytes after the build changes no name. An empty name throws `ArgumentException` with the parameter name, and a `null` factory throws `ArgumentNullException`. `ToString` shows the names as text (`PanelRecordTests`).
- [ ] F3. `ModeDefinition` holds its name, its layout name, and its optional `UiActionMap`. It groups the panels by slot, with the slots in the order of their first entry and the panels in the order of the list. It holds copies of each name. An empty name, an empty layout, an empty entry, and a panel that the list names two times throw `ArgumentException`. A getter index outside the range throws `ArgumentOutOfRangeException` (`PanelRecordTests`).
- [ ] F4. A new `PanelController` holds an unbuilt `PanelInstance`. The first `EnsureRoot` calls the factory one time with the context of the controller and keeps the root. Each later call, also after a detach, gives the same element and calls no factory. Two controllers of one definition build two roots (`PanelControllerTests`).
- [ ] F5. A factory exception leaves `EnsureRoot` with no wrap, the controller stays unbuilt, and the next call calls the factory again. A `null` root and a call from inside the factory throw `InvalidOperationException` and leave the controller unbuilt. A `null` definition or context throws `ArgumentNullException` (`PanelControllerTests`).
- [ ] F6. `EnsureRoot` on a built controller, `IsBuilt`, `Root`, each name read, and each mode getter allocate 0 bytes after warm-up in Debug. Plant 1 of task 1.11 fails the test (`PanelControllerTests`).

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with the skipped count of `<base>`. `PanelRecordTests` holds 14 tests and `PanelControllerTests` holds 10. `BrutalUi.Framework.Tests` holds 24 more tests than at `<base>`.
- [ ] Q3. Each public member of the six new source files has an XML doc comment. The new doc comments and the changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q4. `src/BrutalUi.Framework/Panels/` holds no `lock`, no `IDisposable`, no `event` keyword, no `ToString(` call, and no `(object)` cast in code.

### Integration Criteria
- [ ] I1. The only new source files are the six files of `src/BrutalUi.Framework/Panels/`. No file of `src/BrutalUi.Core/`, no project file, no file of `scripts/` or `samples/`, and no test file that existed at `<base>` changed.
- [ ] I2. The source files use the namespace `BrutalUi.Framework`. `PanelContext` names `CommandRegistry`, `EventBus`, and `ServiceRegistry`. `ModeDefinition.ActionMap` is a `UiActionMap?`. `PanelController.EnsureRoot` calls `Definition.CreateRoot(Context)`.
- [ ] I3. Section 10 of `docs/technical-design.md` holds the sub-section `### Panel definitions`. Section 14 holds the bullet `**Panel tests.**`. `CHANGELOG.md` names the seven new types.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` and the base counts from `docs/features/panels-and-modes/panel-definition/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and the skipped count of `context.md` (Q2).
3. Run `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*PanelRecordTests' 2>&1 | tail -n 5`. Expect a total of 14 and `failed: 0` (F1, F2, F3). Run the same command with `'*PanelControllerTests'`. Expect a total of 10 and `failed: 0` (F4, F5, F6). Run the project with no filter and compare the total with the base count plus 24 (Q2). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Framework/Panels/PanelController.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `EnsureRoot`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp`. Repeat with the early `return` of the warm path removed (F4, F6). Run step 2 again.
5. `grep -rnE '\block\b|IDisposable|\bevent\b|ToString\(|\(object\)' src/BrutalUi.Framework/Panels | grep -v '///'`. Expect no line (Q4).
6. `grep -n 'namespace BrutalUi.Framework;' src/BrutalUi.Framework/Panels/*.cs` gives six lines. `grep -n 'CreateRoot(Context)' src/BrutalUi.Framework/Panels/PanelController.cs` and `grep -n 'UiActionMap? ActionMap' src/BrutalUi.Framework/Panels/ModeDefinition.cs` each give at least one line (I2).
7. `git diff <base> --name-only -- src/ samples/ scripts/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/ scripts/`. Expect only the six new files of `src/BrutalUi.Framework/Panels/`. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no output (I1).
8. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4).
9. Read section 10 and section 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I3). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q3).

---

## Testing Strategy

### Unit Tests
- `Framework.Tests/Panels/PanelRecordTests` (14): the context and its null checks, the names and the factory of a panel and a layout, the name copy, the validation, the grouping and the order of a mode, the optional map, a mode with no panel, the duplicate panel, the index checks, and the record text.
- `Framework.Tests/Panels/PanelControllerTests` (10): the one-time build, the unbuilt start, the context of the factory, a throwing factory and the retry, a `null` root, the re-entry, two controllers, the root after a detach, the argument checks, and the 0-byte check.

### Allocation Tests
- `EnsureRootAndTheReadsAllocateNothingAfterTheBuild` in `PanelControllerTests` (Decision 9): 2 warm-up rounds, a release of the allocation context, 100 measured iterations, and an assert of 0 bytes and of the sum of the reads. Plant 1 of task 1.11 guards it. The epic gate `ModeDiffAllocationTests` of panel-sample measures the full mode change later.

### Regression Tests
- Each existing test runs with no edit. No existing file changes, so the 17 allocation gates keep their scenes and their counts.

### Integration Tests
- `TheRootStaysTheSameAfterADetach` runs the root through `UiContext.AddRoot` and `RemoveRoot`. The full mount path waits for slot-container and mode-set.

### Manual Testing
- None. The feature has no visual result. panel-sample shows the model in HelloPanel.

---

## Related Documentation

- **Requirements:** `docs/features/panels-and-modes/panel-definition/requirements.md`
- **Epic:** `docs/features/panels-and-modes/epic-overview.md`, `docs/features/panels-and-modes/epic-requirements.md`
- **Siblings:** `docs/features/panels-and-modes/slot-container/requirements.md`, `docs/features/panels-and-modes/mode-set/requirements.md`, `docs/features/panels-and-modes/panel-sample/requirements.md`
- **Design:** `docs/technical-design.md` sections 3 (package layout), 4 (frame order, no change), 10 (Framework Layer, the Components table), and 14 (Testing). `docs/mission-statement.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Format reference:** `docs/features/state-and-commands/action-map/implementation.md`
- **Code patterns:** `src/BrutalUi.Framework/Commands/CommandRegistry.cs` (the name copy, the validation messages, the XML doc style), `src/BrutalUi.Framework/Services/ServiceRegistry.cs` (the null check), `src/BrutalUi.Framework/Actions/UiActionMap.cs` (the create-info and the remarks), `tests/BrutalUi.Framework.Tests/Commands/CommandAndEventAllocationTests.cs` (the measure shape), `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs`, `tests/BrutalUi.Framework.Tests/Services/ServiceRegistryTests.cs` (the test style)
- **C# reference:** the record members and `PrintMembers`: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/record-structs, and the ref struct rules: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Questions

The plan uses the recommended option of each question. A change before task 1.3 costs little.

1. **The equality of the records.**
   - **(A) Recommended:** Keep the generated equality. It compares the name copies by reference. The remarks tell the reader to compare names with `SequenceEqual` (Decision 6).
   - (B) Override `Equals` and `GetHashCode` in each record to compare the names by bytes. Value equality, at the cost of two methods and two tests in each record.
2. **The input shape of the mode panels.**
   - **(A) Recommended:** A flat list of `ModePanel` values that the record groups by slot (Decision 3).
   - (B) A slot builder: `new ModeSlot("left"u8).Add("inventory"u8).Add("stats"u8)`, and the mode takes a list of slots. It reads like A4, but the builder is mutable.
   - (C) `PanelDefinition` references in place of panel names. Typed, but the mode needs the panels at build time.

### Conflicts and Gaps

1. **A5 says that a null factory throws `ArgumentException`.** The plan throws `ArgumentNullException`, a subtype of `ArgumentException`, as the codebase does for each `null` argument (Decision 4). The item holds as written, because each `ArgumentNullException` is an `ArgumentException`. A test with `Assert.Throws<ArgumentException>` needs `Assert.ThrowsAny` to pass.
2. **The epic requirements say that `LayoutDefinition` "names the slots of a layout".** Q4 of the epic replaced it with a factory for the layout root, and the feature requirements follow Q4. The plan follows Q4.
3. **The epic requirements say that the controller "binds the root to a store".** A7 moves the bind into the factory, through the existing binding pass. The plan follows A7. The controller has no store member.
4. **Slice rule 7 and A6.** The phase has 13 tasks, above the "about 8 tasks" of rule 7. A6 confirms one phase, so the plan keeps one phase in two task groups, each with 6 files or fewer (rule 3).
5. **`ModePanel` is a seventh public type.** The requirements name five types, and Q2 adds `PanelContext`. The input struct follows from the UTF-8 rule (Decision 3, Notes question 2).
6. **The duplicate panel check is more than A5.** A5 lists the minimum checks. Decision 5 adds one check that slot-container would otherwise hit inside a frame.
7. **The default slot.** The requirements give each panel a default slot, but no requirement says when mode-set uses it. This plan stores it and requires a non-empty name (A5). mode-set decides its use, for example for a mode entry with no slot.
8. **The record shape is not compiled yet.** The planner did not compile a sealed record with a `ReadOnlySpan<byte>` property and an explicit `PrintMembers`. Task 1.3 is the first compile. The Risks table holds the fallback.
9. **The prose lint.** The planner did not run `ste-lint.py` on this file. Task 1.13 runs it on this plan and on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
