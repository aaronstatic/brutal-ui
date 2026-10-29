# Element Bindings - Implementation Plan

**Epic:** state-and-commands (feature 2 of 4)
**Status:** Planning
**Started:** 2026-10-29
**Target Completion:** TBD
**Last Updated:** 2026-10-29

---

## Quality Bar

**Target quality:** A library feature of the frame loop. It adds the one change to the frame order of the epic: a binding pass in `UiContext.Update` between the input pass and the style pass. Each bound element of each game runs through this pass in each frame. A lost change, a false apply, a loop through a widget delegate, a hidden allocation, or a cost on a tree with no binding breaks each game that uses the UI.

- **The frame order is exact:** `Update` runs the input pass, the binding pass, the style pass, the transition pass, the layout pass, the caret pass, and the clear pass, in this order. A store change from an input handler shows in the same `Update`. A value that the binding pass writes reaches the style pass of the same `Update`. A test pins each rule.
- **One apply for each version change:** A binding applies at the first `Update`, and after that one time for each new version of its source. Two `Set` calls before one `Update` give one apply with the last value.
- **No loop:** A value from the store goes through the code setter of the widget. A code setter calls no delegate slot, so a store change never calls `CheckedChanged`, `ValueChanged`, or `TextChanged`. A user change that writes the store back raises the version one time only.
- **0 bytes after warm-up:** `BoundTreeAllocationTests` runs `Update` and `Draw` on a 1000-element tree with 40 bound labels and three bound widgets and a store that changes in each frame. It measures 0 bytes. A planted allocation in the binding pass, and a `ToString` in place of the span formatter, each fail the gate (A3, A4).
- **No cost for a tree with no binding:** The pass skips each subtree with no binding. A tree with no binding costs one flag check for each root. Each existing gate passes with no edit.
- **The Core and Framework split:** `BrutalUi.Core` names no Framework type. `Store<T>` and the typed bind methods live in `BrutalUi.Framework`.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the frame order, the no-loop rule, the leave rule, the error rule, and the selector rule.

---

## Executive Summary

This feature binds an element to a store slice. A game writes `label.BindText(scoreStore, static s => s.Score)` at build time. Each later `UiContext.Update` reads the version of the store. When the version changed, the binding reads the slice, formats the number into the UTF-8 buffer of the label through `IUtf8SpanFormattable.TryFormat`, and calls `Label.SetText(ReadOnlySpan<byte>)`. The setter compares the bytes and marks `Layout` and `Paint` on a change only. No `ToString` runs. The game calls no apply method (Q2).

The seam follows the event slots. Core gets a public delegate `BindingHandler(Element element, object? target)`, an internal struct `ElementBinding` (an `IBindingSource`, the applied version, a static handler, and a target object), a pooled array of these structs on `Element`, and a public build-time method `Element.AddBinding`. Core also gets `BindingPass`, which `UiContext.Update` runs after the input pass and before the style pass. `BrutalUi.Framework` gets the typed bind methods as extension methods: `Label.BindText` (a number slice and a string slice), `Toggle.BindChecked`, `Slider.BindValue`, and `TextField.BindText`. Each method creates one small target object at build time. The target holds the `Store<T>` and the selector. A static handler for each closed generic type reads the slice and writes it through the code setter of the element.

The plan has 4 phases and 30 tasks. Phase 1 delivers the Core list, the pass, and the number text of a label. Phase 2 delivers the widget value bindings, the string text, and the no-loop proof. Phase 3 delivers the leave rule, the error rule, and the format options. Phase 4 delivers the 0-byte gate on the 1000-element tree and the docs.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (binds a label, a toggle, a slider, or a text field to a store slice at build time, and calls only `Store<T>.Set` after that)
- [x] **The sibling feature agents** (action-map binds the label of the HelloPanel sample. panels-and-modes later binds a panel root. xml-markup later calls the same bind methods from XML)
- [x] **The maintainer** (runs one more 0-byte gate in `scripts/test-linux.sh` and `scripts/test-windows.ps1`, with no script change)
- [ ] **The markup author** (not served. The xml-markup epic binds from XML)

**User Subsets Served:**
- [x] **Simple** - `new Label(new Label.CreateInfo()).BindText(score, static s => s);` on a `Store<int>`.
- [x] **Complex** - a `record struct HudState` store, 40 labels on two slices, a toggle whose `CheckedChanged` writes the store back, a slider, and a text field, with no allocation in the frame loop.

**UX Approach:**
- One bind call at build time. No apply call, no unsubscribe call, and no event handler.
- The call returns the element, so it chains with `ElementBuilder.Class` and `ElementBuilder.Child`.

---

## Goals

### Primary Goals
1. Add the pooled binding list to `Element` and the public build-time method `Element.AddBinding`.
2. Add the binding pass to `UiContext.Update`, after the input pass and before the style pass.
3. Add `Label.BindText` for a number slice (span formatter) and a string slice in `BrutalUi.Framework`.
4. Add `Toggle.BindChecked`, `Slider.BindValue`, and `TextField.BindText`, with no loop through the delegate slots.
5. Define the leave rule and the error rule of a binding.
6. Prove 0 bytes after warm-up with `BoundTreeAllocationTests` on a 1000-element tree with a changing store.

### Success Criteria
- [ ] The phase 1 demo passes: `LabelBindingTests` reads `42` from a label bound to a store score of 42 after one `Update`.
- [ ] `BindingPassTests`, `BindingLifecycleTests`, `LabelBindingTests`, `WidgetBindingTests`, and `BoundTreeAllocationTests` pass.
- [ ] `BoundTreeAllocationTests` measures 0 bytes. A planted allocation in `BindingPass` and a `ToString` plant in the number handler each fail it.
- [ ] Each existing test passes with no edit.
- [ ] `src/BrutalUi.Core/` names no Framework type.

---

## Architecture Overview

### The Seam

`Element` and `Label` live in Core. Core does not reference Framework, so the binding list on `Element` cannot name `Store<T>`. The list thus holds a non-generic struct: the source as an `IBindingSource`, the applied version, a static handler, and a target object. The Core pass reads `IBindingSource.Version` and compares it with the applied version. On a change only, it calls the handler. The typed part lives in Framework: the target object holds the `Store<T>` and the selector, and the static handler casts the target and the element. This is the shape of the event slots and of the command handler of commands-and-events (a static handler plus a target). Decision 1 records the choice and the alternatives.

### File Tree (end state)

```
src/BrutalUi.Core/
├── Binding/IBindingSource.cs                    # no change (store-and-subscription)
├── Binding/ElementBinding.cs                    # phase 1: public delegate BindingHandler, internal struct ElementBinding
├── Binding/BindingPass.cs                       # phase 1: internal static class BindingPass
├── Tree/Element.cs                              # phase 1: the list, AddBinding, BindingCount, the subtree flag. phase 3: ClearBindings
└── Tree/UiContext.cs                            # phase 1: UpdateBindings and the frame order. phase 3: the re-entry guard
src/BrutalUi.Framework/
├── Binding/LabelBindings.cs                     # phase 1: the number overload. phase 2: the string overload
├── Binding/StoreSlice.cs                        # phase 2: internal target class (store + selector)
└── Binding/WidgetBindings.cs                    # phase 2: BindChecked, BindValue, BindText (TextField)
tests/BrutalUi.Core.Tests/
├── Binding/BindingPassTests.cs                  # phase 1 (10 tests)
└── Binding/BindingLifecycleTests.cs             # phase 3 (8 tests)
tests/BrutalUi.Framework.Tests/
├── BrutalUi.Framework.Tests.csproj              # phase 1: link RecordingRenderer.cs. phase 4: link the tree files
├── Binding/LabelBindingTests.cs                 # phase 1 (6), phase 2 (+2), phase 3 (+4) = 12 tests
├── Binding/WidgetBindingTests.cs                # phase 2 (10 tests)
├── Binding/BoundTreeAllocationTests.cs          # phase 4 (4 tests)
└── Shared/BoundTree.cs                          # phase 4: the bound 1000-element scene
docs/technical-design.md                         # phase 4: sections 4, 10, and 14
CHANGELOG.md                                     # phase 4
docs/features/state-and-commands/element-bindings/context.md   # each phase
```

These files do not change: `IBindingSource.cs`, each file in `src/BrutalUi.Framework/State/`, `Label.cs`, `Toggle.cs`, `Slider.cs`, `TextField.cs`, `ElementList.cs`, each `.csproj` other than the Framework test project, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file. Namespaces stay flat: `BrutalUi.Core`, `BrutalUi.Framework`, `BrutalUi.Core.Tests`, and `BrutalUi.Framework.Tests`.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public delegate void BindingHandler(Element element, object? target);         // phase 1

public class Element
{
    public void AddBinding(IBindingSource source, BindingHandler handler, object? target); // phase 1. Throws ArgumentNullException
    public int BindingCount { get; }                                                       // phase 1
    public void ClearBindings();                                                           // phase 3
}
```

```csharp
namespace BrutalUi.Framework;

public static class LabelBindings
{
    public static Label BindText<T, TValue>(this Label label, Store<T> store, Func<T, TValue> selector,
        string? format = null, IFormatProvider? provider = null)
        where TValue : IUtf8SpanFormattable;                                       // phase 1
    public static Label BindText<T>(this Label label, Store<T> store, Func<T, string?> selector); // phase 2
}

public static class WidgetBindings
{
    public static Toggle BindChecked<T>(this Toggle toggle, Store<T> store, Func<T, bool> selector);        // phase 2
    public static Slider BindValue<T>(this Slider slider, Store<T> store, Func<T, float> selector);         // phase 2
    public static TextField BindText<T>(this TextField field, Store<T> store, Func<T, string?> selector);   // phase 2
}
```

### Internal Shape

- `ElementBinding` (internal struct): `IBindingSource Source`, `ulong AppliedVersion` (0 at the add, so the first pass applies, because a source starts at 1), `BindingHandler Handler`, `object? Target`.
- `Element` fields: `private ElementBinding[]? _bindings;` (null until the first add, then 2 slots, then doubling) and `private int _bindingCount;`. `internal ReadOnlySpan<ElementBinding> Bindings` serves the Core tests. `internal bool HasBoundSubtree { get; set; }` is the subtree flag (Decision 3).
- `AddBinding`: check the source and the handler for `null`, grow the array when it is full, write the slot, then set `HasBoundSubtree` on the element and on each ancestor up to the first ancestor that holds it.
- `AttachToParent` (existing): after the existing marks, when `HasBoundSubtree` is `true`, set it on each new ancestor up to the first ancestor that holds it. `UiContext.AddRoot` needs no change: the pass reads the flag of each root.
- `BindingPass.Run(ReadOnlySpan<Element> roots)`: for each root, `Visit(root)`. `Visit(element)`: return when `HasBoundSubtree` is `false`. For each slot `i` from 0 to `_bindingCount - 1`: take `ref ElementBinding b = ref _bindings[i]`, read `ulong v = b.Source.Version`, skip when `v == b.AppliedVersion`, else write `b.AppliedVersion = v` and call `b.Handler(element, b.Target)`. Then visit each child, and set `HasBoundSubtree = _bindingCount > 0 || any child flag`. The last step clears a stale flag (Decision 3). The recursion depth is the depth of the tree, as in `StylePass`.
- `UiContext.UpdateBindings()` (internal, as `UpdateStyles`): `BindingPass.Run(_roots.AsSpan())`. `Update` calls it right after `DrainEvents()` and before `UpdateStyles()`.
- Framework target classes: `NumberText<T, TValue>` (a private nested class of `LabelBindings`: the store, the selector, the format, the provider, and a `byte[]` scratch of 32 bytes) and `StoreSlice<T, TValue>` (internal: the store and the selector, with `TValue Read() => Selector(Store.Value)`). Each closed generic handler class holds `private static readonly BindingHandler s_apply = Apply;`, as `Toggle.s_pointerUp` does.
- The number handler: read the slice, call `value.TryFormat(scratch, out int written, format, provider)`. On `false`, double the scratch and try again. Above 4096 bytes, throw `InvalidOperationException`. Then call `((Label)element).SetText(scratch.AsSpan(0, written))`. The constrained call on `TValue` boxes nothing.

### Data Flow

```
Build time (allocates one time):
  label.BindText(store, static s => s.Score)
    new NumberText<HudState, int>(store, selector, null, null) ──► label.AddBinding(store, s_apply, target)
      _bindings[n] = { Source = store, AppliedVersion = 0, Handler = s_apply, Target = target }
      HasBoundSubtree = true on the label and up the parent chain

Game code: store.Set(state with { Score = 10 })  ──► Version 1 → 2 (no event, no call)

UiContext.Update(dt):
  1. Input pass (DrainEvents) ── a handler can call store.Set
  2. Binding pass (BindingPass.Run) ── skip each subtree with HasBoundSubtree false
       b.Source.Version (2) != b.AppliedVersion (1) ──► AppliedVersion = 2 ──► s_apply(label, target)
         selector(store.Value) ──► TryFormat into scratch ──► label.SetText(span) ──► MarkDirty(Layout | Paint) on a change
  3. Style pass ── 4. Transition pass ── 5. Layout pass (measures the new text) ── 6. Caret pass ── 7. Clear pass
```

### The No-Loop Rule

Each widget already calls its delegate slot from user input only: `Toggle.Checked` (Q4 of button-and-toggle), `Slider.Value` (Decision 5 of slider), and `TextField.SetText` (Decision 7 of text-field). The binding writes through these code setters. A store change thus never calls a delegate. A user change calls the delegate one time, the game writes the store, the version rises one time, and the next binding pass writes the same value through the code setter. The setter compares and changes nothing. Phase 2 pins the full round trip for each widget.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The Confirmed Assumptions do not name the demo of phase 1, so Notes question 3 holds it (Slice rule 12).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/<Project>/<Project>.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. `UiContext.cs` and `Element.cs` have more than 300 lines: read them in ranges (tool rule 2). A doc comment in Core must not use `<see cref>` to a Framework type.

### Phase 1: The Pass and the Number Text of a Label
**Goal:** The smallest path that proves the idea: a game binds a label to an `int` slice of a store, and each `Update` after a change shows the new number, formatted into the label buffer with no `ToString`. The pass runs after the input pass and before the style pass, and it skips each subtree with no binding.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*LabelBindingTests'` and reads a total of 6 with `failed: 0`. The first test binds a label to a store score of 42 and reads the text `42` after one `Update`.
**Needs:** none
**Agent tier:** ADVANCED (opus). The phase changes the frame order of `UiContext.Update`, adds fields and a public method to `Element`, edits `AttachToParent`, and sets the public seam that panels-and-modes and xml-markup build on.

**Tasks (group A: the Core list and the pass):**
- [ ] 1.1 Create `docs/features/state-and-commands/element-bindings/context.md`. Record the base commit (`git rev-parse HEAD`), and the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Framework.Tests`. Record the Seam, the Internal Shape, and the No-Loop Rule of the Architecture Overview. Record that commands-and-events can run in parallel, so each later count check uses the counts of the new classes.
- [ ] 1.2 Write `src/BrutalUi.Core/Binding/ElementBinding.cs` with two types. (1) `public delegate void BindingHandler(Element element, object? target);`. The summary: the apply handler of a binding. The remarks: the binding pass calls it one time for each new version of the source, before the style pass. Pass a static method in a `static readonly` field, so the add allocates no delegate. The handler writes a value of its element through a setter that marks the dirty bits of the change. It must not add or remove an element, and it must not add or clear a binding. (2) `internal struct ElementBinding` with the four fields of the Internal Shape and a doc comment on each field.
- [ ] 1.3 Edit `src/BrutalUi.Core/Tree/Element.cs`: add the two fields, `internal ReadOnlySpan<ElementBinding> Bindings`, `internal bool HasBoundSubtree`, `public int BindingCount`, and `public void AddBinding(IBindingSource source, BindingHandler handler, object? target)` (the Internal Shape). The `AddBinding` remarks state: add a binding at build time. The list grows by doubling and never shrinks, so the add can allocate. The first `Update` applies the binding, and each later `Update` applies it one time for each new version. The element holds the source, and the source holds no reference to the element. Edit `AttachToParent`: send the flag up the new parent chain (the Internal Shape). Add one paragraph to the class remarks: the binding list and the binding pass.
- [ ] 1.4 Write `src/BrutalUi.Core/Binding/BindingPass.cs`: `internal static class BindingPass` with `Run(ReadOnlySpan<Element> roots)` and a private recursive `Visit` (the Internal Shape). Read the slot again by index after each handler call, and write `AppliedVersion` before the call (Decision 7). The class remarks state the skip rule, the order (a parent before its children, the bindings of one element in the order of add), and "the pass allocates nothing".
- [ ] 1.5 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `internal void UpdateBindings()` next to `UpdateStyles()`, with remarks in the style of `UpdateStyles`. In `Update`, call `UpdateBindings();` right after `DrainEvents();`, with the comment `// The binding pass. It applies each new store version before the style pass reads the elements.` Edit the remarks of `Update`: the frame order is the input pass, the binding pass, the style pass, the transition pass, the layout pass, the caret pass, and the clear pass. A value from an input handler reaches the binding pass of the same call. A tree with no binding costs one flag check for each root.
- [ ] 1.6 Write `tests/BrutalUi.Core.Tests/Binding/BindingPassTests.cs` with 10 tests. Use a private sealed `TestSource : IBindingSource` with a settable `Version` (start 1), a private sealed `ApplyLog` target that counts calls and records the order, and a static handler in a `static readonly` field. Tests: `TheFirstUpdateAppliesEachBinding`, `AnUpdateWithNoNewVersionAppliesNothing`, `TwoVersionsBeforeOneUpdateGiveOneApply`, `TheBindingPassRunsBeforeTheStylePass` (the handler writes `element.Inline.Color`, and `element.Style.Color` holds the new color after the same `Update`), `AVersionFromAnInputHandlerAppliesInTheSameUpdate` (a `PointerDown` with the element as the caller target. Its handler raises the version), `TwoBindingsOnOneElementApplyInTheOrderOfAdd`, `AddBindingRejectsANullSourceOrHandler` (`ArgumentNullException` with the parameter name for each), `AddBindingMarksTheBoundPathOnly` (`HasBoundSubtree` is `true` on the element and each ancestor and `false` on a sibling branch), `AnAttachOfABoundSubtreeMarksTheNewAncestors`, and `ABranchWithNoBindingLosesItsMarkAtTheNextUpdate` (remove the bound child, run `Update`, and read `false` on the old parent).
- [ ] 1.7 Run the build and the tests. Run the class of task 1.6 and expect a total of 10 with `failed: 0`. Run `ThousandElementAllocationTests` and `ThousandWidgetAllocationTests` and expect `failed: 0` (no cost for a tree with no binding). Record the results in `context.md`.

**Tasks (group B: the number text of a label):**
- [ ] 1.8 Edit `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`: add `<Compile Include="../BrutalUi.Core.Tests/Shared/RecordingRenderer.cs" LinkBase="Shared" />` (Decision 9). Link each file by name. Do not use `*.cs`, because the project holds its own copy of `AllocationGate`. When the linked file needs another file of `Shared/`, link that file too, except `AllocationGate.cs`, and record it in `context.md`.
- [ ] 1.9 Write `src/BrutalUi.Framework/Binding/LabelBindings.cs`: `public static class LabelBindings` with the number overload of the Public API and the private nested `NumberText<T, TValue>` target (the Internal Shape). Check the label, the store, and the selector with `ArgumentNullException.ThrowIfNull`. Return the label. The remarks state: the binding formats the slice with `IUtf8SpanFormattable.TryFormat` into a buffer of the binding, and it runs no `ToString` (A4). `null` for the provider gives the current culture, as `ToString` does. The scratch grows by doubling and never shrinks, so a text that is longer than each earlier text allocates one time. Pass a `static` lambda or a cached method group. A selector must not allocate. A `SetText` from code on a bound label shows until the next new version of the store. The binding clears `Label.LocalizedText`. The extension form exists because `Label` lives in Core and cannot name `Store<T>`.
- [ ] 1.10 Write `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs` with 6 tests on a private `record struct HudState(int Score, int Lives)` and a context with a `RecordingRenderer` measurer: `ABoundLabelShowsTheScoreAfterTheFirstUpdate` (the demo: the store score 42, one `Update`, the text `"42"u8`, and `Assert.Same` for the return value), `ASetOfANewScoreChangesTheTextAtTheNextUpdate`, `TwoSetsBeforeOneUpdateShowTheLastScore`, `AChangeOfAnotherSliceKeepsTheText`, `TwoLabelsOnOneStoreShowTheirOwnSlices`, and `BindTextRejectsANullStoreOrSelector`. Each selector is a `static` lambda. Run the build, the tests, and the class. Expect a total of 6 with `failed: 0`. Record the demo result in `context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (group A, 6):** `ElementBinding.cs`, `BindingPass.cs`, `Element.cs`, `UiContext.cs`, `BindingPassTests.cs`, `context.md`
**Files (group B, 4):** `BrutalUi.Framework.Tests.csproj`, `LabelBindings.cs`, `LabelBindingTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `BindingPassTests` and the 6 tests of `LabelBindingTests` pass.
- [ ] In `UiContext.Update`, the line `UpdateBindings();` comes after `DrainEvents();` and before `UpdateStyles();`.
- [ ] Each existing allocation gate passes with no edit.
- [ ] `src/BrutalUi.Core/` names no Framework type.

---

### Phase 2: The Widget Values and the String Text
**Goal:** A game binds a toggle, a slider, and a text field to a store slice, and a label to a string slice. A store change moves each widget and calls no delegate. A user change that writes the store back does not loop.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*WidgetBindingTests'` and reads a total of 10 with `failed: 0`. The test `AUserFlipWritesTheStoreOneTimeWithNoLoop` shows one delegate call and one new store version for one click.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Write `src/BrutalUi.Framework/Binding/StoreSlice.cs`: `internal sealed class StoreSlice<T, TValue>` with the store, the selector, and `Read()` (the Internal Shape). The class remarks state: one instance for each binding, created at build time.
- [ ] 2.2 Write `src/BrutalUi.Framework/Binding/WidgetBindings.cs`: `public static class WidgetBindings` with `BindChecked`, `BindValue`, and `BindText` (TextField) of the Public API. Each method checks its arguments with `ArgumentNullException.ThrowIfNull`, creates a `StoreSlice`, calls `AddBinding` with a static handler in a `static readonly` field, and returns the element. The handlers write `toggle.Checked = slice.Read()`, `slider.Value = slice.Read()`, and `field.SetText(slice.Read())`. The remarks of each method state: the code setter calls no delegate, so a store change calls no `CheckedChanged`, `ValueChanged`, or `TextChanged` (the No-Loop Rule). A user change still calls the delegate, and the game writes the store there (the epic has no two-way helper). The slider applies its value rule, so the slider can show a value that differs from the store (a clamp or a grid step). A new text from the store puts the caret of the field at the end and clears the selection. The field cuts a text at its capacity.
- [ ] 2.3 Edit `src/BrutalUi.Framework/Binding/LabelBindings.cs`: add the string overload of the Public API. Its handler calls `Label.SetText(string?)`, which encodes with no allocation when the buffer is large enough. `null` gives an empty text.
- [ ] 2.4 Write `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` with 10 tests on a private `record struct FormState(bool Muted, float Volume, string Name)`. Give each widget an inline size, and give each a counter delegate. Tests: `ABoundToggleTakesTheSliceAtTheFirstUpdate`, `AStoreChangeFlipsTheToggleAndCallsNoDelegate`, `ABoundSliderTakesTheSliceThroughTheValueRule` (range 0 to 100, step 1, store value 50.4, slider value 50), `AStoreChangeMovesTheSliderAndCallsNoDelegate`, `ABoundTextFieldTakesTheStringSlice`, `AStoreChangeSetsTheFieldTextAndCallsNoDelegate`, `AUserFlipWritesTheStoreOneTimeWithNoLoop` (a press and a release at the center of the toggle. `CheckedChanged` writes `Muted` back. After two `Update` calls: one delegate call, the version raised by one, and the toggle checked), `AUserKeyOnTheSliderWritesTheStoreOneTimeWithNoLoop` (focus the slider, push a Right key down with the key code 262, the GLFW value of Right), `AUserCharInTheFieldWritesTheStoreOneTimeWithNoLoop` (focus the field, push the char `a`. `TextChanged` writes the UTF-8 text back as a string. The caret stays at the end), and `EachWidgetBindMethodRejectsANullStoreOrSelector`. The first, third, and fifth tests also check the return value with `Assert.Same`.
- [ ] 2.5 Edit `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs`: add 2 tests, `ALabelShowsAStringSlice` and `ANullStringSliceGivesAnEmptyText`, on a store of a `record struct` with a `string? Title`.
- [ ] 2.6 Run the build, the tests, `WidgetBindingTests` (total 10), and `LabelBindingTests` (total 8). Expect `failed: 0`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 35 (6 tasks, about six steps for one task)

**Files (6):** `StoreSlice.cs`, `WidgetBindings.cs`, `LabelBindings.cs`, `WidgetBindingTests.cs`, `LabelBindingTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `WidgetBindingTests` and the 8 tests of `LabelBindingTests` pass.
- [ ] No file of `src/BrutalUi.Core/Elements/` has a diff against the base commit.
- [ ] The three round-trip tests each read one delegate call and one new version for one user change.

---

### Phase 3: The Leave Rule, the Error Rule, and the Format
**Goal:** A bound element that leaves the tree applies nothing, and it applies the newest value when it joins again. `ClearBindings` drops each binding. A store keeps no reference to a bound element. A handler that throws stops the `Update` one time. A number slice takes a format string and a format provider.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*BindingLifecycleTests'` and reads a total of 8 with `failed: 0`. The same command on the Framework test project with `'*LabelBindingTests'` reads a total of 12 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Edit `src/BrutalUi.Core/Tree/Element.cs`: add `public void ClearBindings()`. It writes `default` into each used slot, so the element drops its references to each source and each target, and it sets the count to 0. It keeps the array. It does not change `HasBoundSubtree`: the next pass clears a stale flag. The remarks state the leave rule (Decision 6): an element outside the roots of a context applies nothing. It keeps its bindings, and the first `Update` after a join applies a new version. Call `ClearBindings` to drop the bindings for good. A store holds no reference to an element, so a dropped element needs no unsubscribe.
- [ ] 3.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add a `private bool _binding;` flag. `UpdateBindings` sets it in a `try` and clears it in a `finally`. `Update` throws `InvalidOperationException` ("A binding handler must not call Update.") when the flag is set. Extend the remarks of `Update`: an exception from a binding handler stops the call before the style pass. That binding keeps the new version, so it does not apply again until the next new version. The bindings after it apply at the next `Update` (Decision 7).
- [ ] 3.3 Write `tests/BrutalUi.Core.Tests/Binding/BindingLifecycleTests.cs` with 8 tests and the helpers of `BindingPassTests` (copy the private helpers, do not share them): `ARemovedElementGetsNoApply`, `AnElementThatJoinsAgainAppliesTheNewVersion`, `AnElementThatJoinsAgainWithNoNewVersionAppliesNothing`, `ARemovedRootGetsNoApply`, `AMoveToAnotherContextAppliesInTheNewContext`, `ClearBindingsDropsEachBindingAndItsReferences` (`BindingCount` 0, each slot of `Bindings` is `default`, and no apply after a new version), `AHandlerThatThrowsUsesUpItsChangeAndStopsTheUpdate` (the exception leaves `Update`, the element keeps its `Style` bit, the next `Update` applies the second binding and does not call the first handler again), and `AHandlerThatCallsUpdateGetsAnException`.
- [ ] 3.4 Edit `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs`: add 4 tests. `AFormatStringFormatsTheNumber` (a `float` 3.14159, the format `F2`, `CultureInfo.InvariantCulture`, the text `3.14`), `AProviderFormatsTheNumber` (a clone of the invariant `NumberFormatInfo` with the decimal separator `,`, the text `3,14`), `ALongTextGrowsTheBuffer` (the format `D40`, a text of 40 bytes, above the 32 bytes of the start scratch), and `AStoreKeepsNoReferenceToADroppedLabel` (a `[MethodImpl(MethodImplOptions.NoInlining)]` helper builds a context with a bound label, runs one `Update`, removes the root, and returns a `WeakReference` to the label. Then `GC.Collect()`, `GC.WaitForPendingFinalizers()`, and `GC.Collect()`. The weak reference is dead, and the store is alive through `GC.KeepAlive`).
- [ ] 3.5 Run the build, the tests, `BindingLifecycleTests` (total 8), `BindingPassTests` (total 10), and `LabelBindingTests` (total 12). Expect `failed: 0`. Run `AStoreKeepsNoReferenceToADroppedLabel` five times with `--filter-method` and expect 5 passes.
- [ ] 3.6 Record the results, the leave rule, and the error rule in `context.md`.

**Estimated Steps:** about 40 (6 tasks, about six to seven steps for one task)

**Files (5):** `Element.cs`, `UiContext.cs`, `BindingLifecycleTests.cs`, `LabelBindingTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `BindingLifecycleTests` and the 12 tests of `LabelBindingTests` pass, and `BindingPassTests` still passes.
- [ ] The GC test passes five times in a row.
- [ ] Each existing test of `RoutingGateTests` passes with no edit (the re-entry rule of the input pass does not change).

---

### Phase 4: The 0-Byte Gate on the Bound Tree and the Docs
**Goal:** `BoundTreeAllocationTests` proves 0 bytes for `Update` and `Draw` on a 1000-element tree with a changing store, 40 bound labels, and three bound widgets (A3). The technical design and the changelog state the binding pass and the bind methods.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*BoundTreeAllocationTests'` and reads a total of 4 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the gate measures bytes that the Risks table does not explain.

**Tasks (group A: the gate):**
- [ ] 4.1 Edit `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`: link `ThousandElementTree.cs`, `ThousandWidgetTree.cs`, and `TextMeasure.cs` of `tests/BrutalUi.Core.Tests/Shared/` by name, as in task 1.8. The linked types are `internal` in `namespace BrutalUi.Core.Tests`, so each test file that uses them adds `using BrutalUi.Core.Tests;`.
- [ ] 4.2 Write `tests/BrutalUi.Framework.Tests/Shared/BoundTree.cs`: `internal sealed class BoundTree` with `static BoundTree Build()`. It calls `ThousandWidgetTree.Build()`. It creates a `Store<HudState>` with a private `record struct HudState(int Score, int Lives, float Volume, bool Muted, string Name)`. In each of the 40 stat rows (the children of `StatRow.Parent` with the class `row`), it replaces the child at the index of `StatName` in `StatRow` with a new `Label` through `Children.RemoveAt` and `Children.Insert`. Each label starts with a text of 8 ASCII bytes, so its buffer holds each later text. It binds rows 0 to 37 in turn to `Score` and `Lives`, row 38 to `Volume` with the format `F1` and the invariant culture, and row 39 to `Name` (the string overload). It binds the toggle to `Muted`, the slider to `Volume`, and the text field to `Name`. The toggle `CheckedChanged` writes `Muted` back to the store. The slider `ValueChanged` and the field `TextChanged` only count. It throws `InvalidOperationException` when `ThousandWidgetTree.CountSubtree(Root)` is not 1000 or when it found not 40 rows. `static HudState Next(int round)` gives `Score = 10000 + round`, `Lives = 10 + round % 10`, `Volume = 10 + round % 80`, `Muted = round % 2 == 1`, and `Name` from two strings that the build creates. Each text thus keeps a fixed width for each round. The remarks state the scene and the width rule.
- [ ] 4.3 Write `tests/BrutalUi.Framework.Tests/Binding/BoundTreeAllocationTests.cs` with 4 tests in the shape of `ThousandWidgetAllocationTests` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, 100 measured iterations, `Assert.Equal(0L, ...)`): `UpdateAndDrawWithAChangingStoreAllocateNothing` (each frame sets `Next(round)`, so each slice changes, and runs `Update` and `Draw`), `AChangeOfOneSliceAllocatesNothing` (each frame changes `Lives` only. Each binding applies, and the labels of the other slices compare the same bytes and mark nothing), `AUserFlipWithAWriteBackAllocatesNothing` (each frame pushes a press and a release at `ToggleCenter` with no target, then runs `Update` and `Draw`), and `TheMeasuredBodyDoesRealWork` (after one frame of each body: the text of row 0 is the UTF-8 form of `10000 + round`, the toggle and the slider and the field hold the slices of the store, `ValueChanged` and `TextChanged` counts are 0, one flip gives one `CheckedChanged` call and one new version). The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, and no `Assert`.
- [ ] 4.4 Run the build, the tests, and the class of task 4.3. Plant `GC.KeepAlive(new object());` as the first line of the slot loop in `BindingPass.Visit`, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`, and expect a value that is not 0. Record which gates fail and the bytes. Restore the file from a copy in the scratchpad and check it with `cmp`. Plant `label.SetText(value.ToString());` in place of the `TryFormat` path of `NumberText.Apply`, run the same command, expect a value that is not 0, record the result, and restore. Run the tests again and expect 0 failed.

**Tasks (group B: the docs):**
- [ ] 4.5 Edit `docs/technical-design.md` section 4: in the overview diagram, replace the line `Framework: mode diff, store notifications, bindings` with `Binding pass: apply each new store version to its bound elements`. In "Frame order" item 2, name the binding pass after the input pass, and add a paragraph "The binding pass at the seam" in the style of the style pass paragraph (the skip flag, the order, the code setters, the error rule, and the cost of a tree with no binding).
- [ ] 4.6 Edit section 10 of `docs/technical-design.md`, "Data binding": add the binding rules (the struct list on `Element`, `AddBinding` at build time, the bind methods in Framework, the span formatter, the no-loop rule, the leave rule, and the error rule). Edit section 14: add a bullet for the binding tests (`BindingPassTests`, `BindingLifecycleTests`, `LabelBindingTests`, `WidgetBindingTests`) and a bullet "A fifteenth allocation gate" for `BoundTreeAllocationTests` with the scene and the plant results of task 4.4.
- [ ] 4.7 Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: the binding pass in `UiContext.Update`, `Element.AddBinding`, `Element.ClearBindings`, `BindingHandler`, `LabelBindings.BindText`, `WidgetBindings`, and the bound-tree allocation gate (state-and-commands/element-bindings).
- [ ] 4.8 Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `grep -n 'dotnet test --solution brutal-ui.sln' scripts/test-linux.sh scripts/test-windows.ps1` and expect one line for each script (A3: the scripts run the gate with no change). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on the changed docs. Record the results and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 45 (8 tasks, about five to six steps for one task)

**Files (group A, 4):** `BrutalUi.Framework.Tests.csproj`, `BoundTree.cs`, `BoundTreeAllocationTests.cs`, `context.md`
**Files (group B, 3):** `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 4 tests of `BoundTreeAllocationTests` pass, and the two plants each give a non-zero exit code of `dotnet test`.
- [ ] The scene holds 1000 elements, 40 bound labels, and three bound widgets.
- [ ] `scripts/test-linux.sh` prints its PASS line, and no file of `scripts/` changed.
- [ ] Sections 4, 10, and 14 of the technical design and the changelog name the binding pass and the gate.

---

## Key Technical Decisions

### Confirmed Assumptions

This feature had no feature interview. The items below come from the epic interview of `/plan-epic state-and-commands` (see `docs/features/state-and-commands/epic-overview.md`), and the user confirmed them there:
- Q1 (epic) `IBindingSource` lives in Core, and `Store<T>` lives in Framework.
- Q2 (epic) A binding pass in `UiContext.Update` pulls the store version before the style pass.
- A3 (epic) A new gate `BoundTreeAllocationTests` puts a 1000-element tree with a changing store into the 0-byte check.
- A4 (epic) A text binding formats a number into the label buffer through a span formatter, with no `ToString` in the frame loop.
- A2 (epic) `tests/BrutalUi.Framework.Tests/` exists in the solution. The epic adds tests there and makes no new project.

### Decision 1: The seam is a Core struct with a static handler plus a target, and Framework extension methods
**Choice:** Options (a) and (c) of Notes item 1 of store-and-subscription. Core holds a non-generic struct `ElementBinding` (an `IBindingSource`, the applied version, a static `BindingHandler`, and a target object) in a pooled array on `Element`, and a public `Element.AddBinding`. Framework holds the typed bind methods as extension methods, and each creates one target object at build time. Notes question 1 holds the open choice, with this option as the recommended option.
**Rationale:** The epic rule: a binding is a struct in a pooled list. The struct names no Framework type. The shape is the shape of the event slots and of the command handler (Q3 of the epic): a static handler plus a target. The Core pass checks the version with one interface call and calls the handler on a change only. An extension method gives the call form of the requirement, `label.BindText(store, selector)`, and it follows `ElementBuilder` in Core. The target allocates one time at build time (A5 of the epic allows a build-time allocation).
**Alternatives:** (b) A Core interface `IElementBinding` and a sealed generic Framework class that implements it: the list holds references, against the struct rule, and each frame makes one virtual call for each binding. (a) with an internal `AddBinding` and `InternalsVisibleTo("BrutalUi.Framework")`: see Decision 8.

### Decision 2: One version counter for each binding, in the Core struct
**Choice:** The applied version lives in `ElementBinding`. The Framework target holds the `Store<T>` and the selector, not a `StoreSubscription<T, TValue>`.
**Rationale:** The pass must check the version in Core to skip the handler call. A subscription in the target holds a second counter that must stay equal to the first. One counter has one truth, and a forced re-apply (a later need) writes one field. `StoreSubscription` stays the pull tool for game code and is unchanged.
**Alternatives:** A `StoreSubscription` in the target: two counters for each binding, and a second version check in each apply.

### Decision 3: A subtree flag skips each branch with no binding
**Choice:** `Element.HasBoundSubtree` is `true` when the element or a descendant can hold a binding. `AddBinding` and `AttachToParent` set it up the parent chain. The pass skips a branch with the flag `false`, and after each visit it writes the flag again from the own count and the flags of the children. A remove leaves a stale `true` flag, and the next pass clears it.
**Rationale:** Section 4 of the technical design: the passes visit dirty subtrees only, and a static screen costs one tree walk for the draw. A full walk for each frame adds a second walk of each tree, also for each game with no binding. The flag is a hint that can be stale `true` but never stale `false`, so no bound element is ever skipped. The flag is a private member, so the public `DirtyFlags` enum does not change.
**Alternatives:** (a) A full walk in each frame: simple, and 0 bytes, but it costs each existing tree. (b) A new bit in `DirtyFlags`: a change of a public enum, and the clear pass and `DirtyWalk` must learn to keep it. (c) A list of bound elements on `UiContext`: an element holds no context reference, so a remove and a join must update the list.

### Decision 4: The handler writes through the code setter of the element
**Choice:** Each Framework handler writes through a public setter: `Label.SetText`, `Toggle.Checked`, `Slider.Value`, and `TextField.SetText`. The pass marks no dirty bit itself.
**Rationale:** Each setter compares the old value and marks the bits that its change needs: `Layout` and `Paint` for a label text, `Style` for a toggle state, and `Paint` for a slider value and a field text. A same value marks nothing. Each code setter calls no delegate slot, so the no-loop rule holds with no new code in the widgets. This reads the requirement "marks the element dirty" as "the setter marks it".
**Alternatives:** A `MarkDirty` call in the pass for each apply: a wrong kind for some elements, and a mark for a same value.

### Decision 5: The number text uses `IUtf8SpanFormattable.TryFormat` into a buffer of the binding
**Choice:** The number overload has the constraint `where TValue : IUtf8SpanFormattable`. The target owns a `byte[]` scratch of 32 bytes. On `false`, it doubles the scratch and tries again, up to 4096 bytes. Then it calls `Label.SetText(ReadOnlySpan<byte>)`.
**Rationale:** A4. `int`, `long`, `float`, `double`, and `decimal` implement the interface. The constrained call boxes no struct. The UTF-8 form needs no encode step. The label compares the bytes, so a same text marks nothing. A stack buffer does not work for a text above its size, and the scratch grows one time only.
**Alternatives:** (a) `ISpanFormattable` into a `char` buffer and then `Label.SetText`: no char overload exists, and an encode step costs time. (b) A format directly into the label buffer: `Label` has no API for it, and the compare rule of `SetText` then needs a second buffer.

### Decision 6: The leave rule keeps the list
**Choice:** An element outside the roots of a context applies nothing, because the pass walks from the roots only. The element keeps its bindings, and the first `Update` after a join applies a new version. `ClearBindings` drops the bindings for good. The store holds no reference to the element (Decision 1 of store-and-subscription), so a dropped element needs no unsubscribe, and the GC collects it. Notes question 2 holds the open choice.
**Rationale:** The requirement: an element that leaves the tree drops its bindings, and a store keeps no reference to the element. A panel leaves and joins the tree often (panels-and-modes). A clear at each remove loses its bindings, and a remove then costs a walk of the subtree. The pass never reaches an element outside the tree, so the bindings have no effect there.
**Alternatives:** A remove (`ElementList.RemoveAt` and `UiContext.RemoveRoot`) clears the bindings of the subtree.

### Decision 7: The pass writes the version before the call, and a throw stops the update
**Choice:** The pass writes `AppliedVersion` before it calls the handler, as `StoreSubscription.TryGetChanged` does (follow-up 1 of store-and-subscription). An exception leaves `Update` before the style pass. A handler that calls `Update` gets `InvalidOperationException`.
**Rationale:** The rule of the input pass: an exception from a handler stops the call before the style pass, and the next `Update` continues. A version that the call used up does not throw again in each frame, so one bad value does not stop each later frame. The same order in the pass and in the subscription gives one rule for the epic.
**Alternatives:** Write the version after the call: a bad selector throws in each frame until the game fixes it.

### Decision 8: `Element.AddBinding` is public
**Choice:** `AddBinding`, `BindingCount`, `ClearBindings`, and `BindingHandler` are public in Core.
**Rationale:** `IBindingSource` is public, so a game can write its own source. xml-markup references Core, and it can call the same method. A public method needs no change of the Core project file.
**Alternatives:** An internal method and `InternalsVisibleTo("BrutalUi.Framework")`: a smaller public surface, but a project file change, and each later package that binds needs one more friend assembly.

### Decision 9: The gate lives in the Framework tests and links the shared tree
**Choice:** `BoundTreeAllocationTests` lives in `tests/BrutalUi.Framework.Tests/`, because it needs `Store<T>`. The project links `RecordingRenderer.cs`, `ThousandElementTree.cs`, `ThousandWidgetTree.cs`, and `TextMeasure.cs` from `tests/BrutalUi.Core.Tests/Shared/` by name.
**Rationale:** A2 of the epic: no new project. The benchmark project links the same files, and each file states that it uses no internal member of Core. `BrutalUi.Markup.Tests` links a shared file in the same way. A copy of 1000 lines drifts. The link names each file, so the copy of `AllocationGate` of store-and-subscription does not meet a second `AllocationGate` type.
**Alternatives:** (a) A copy of the tree files: two trees to keep the same. (b) A project reference to `BrutalUi.Core.Tests`: a test project that references a test project, against A6 of store-and-subscription.

### Decision 10: Each bind method returns its element
**Choice:** `BindText`, `BindChecked`, and `BindValue` return the element.
**Rationale:** The methods of `ElementBuilder` return the element, so a build chain reads `new Label(...).Class("score").BindText(store, static s => s.Score)`.
**Alternatives:** A `void` return, or a handle for one unbind: YAGNI. `ClearBindings` drops all.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The subtree flag is `false` on a branch with a binding, and the binding never applies. | Low | High | Decision 3: the flag is set at each add and each attach, and only the pass clears it, after it saw each child. Tests 8 to 10 of `BindingPassTests` and the join tests of `BindingLifecycleTests`. |
| The pass adds cost to each tree with no binding. | Low | Medium | Decision 3: one flag check for each root. Task 1.7 runs the existing 1000-element gates. |
| A label buffer or the scratch grows in the measure, and the gate sees bytes. | Medium | Medium | Task 4.2: each label starts with 8 bytes, and each round keeps a fixed text width. The warm-up rounds use the same widths. |
| The current culture read allocates for a `null` provider. | Low | Medium | `NumberFormatInfo.CurrentInfo` reads a cached culture. The warm-up covers the first read. The gate uses a `null` provider for the score labels. |
| A selector allocates (a capturing lambda, a boxed slice, a string build). | Medium | High | The remarks state the selector rule. The gate uses `static` lambdas, and the gate catches a selector that allocates. |
| A handler changes the tree during the pass. | Low | High | The `BindingHandler` remarks forbid it. The Framework handlers write one value only. The pass reads each slot by index after each call. |
| A store change resets the caret of a text field while the user types. | Medium | Low | The `BindText` (TextField) remarks state it. A write-back of the same text changes no byte, so the caret stays (`AUserCharInTheFieldWritesTheStoreOneTimeWithNoLoop`). |
| The GC test is flaky in Debug, because the JIT keeps a local alive. | Medium | Low | Task 3.4 builds the label in a `NoInlining` helper and collects twice. Task 3.5 runs the test five times. |
| A linked shared file needs another file or clashes with the copy of `AllocationGate`. | Low | Low | Decision 9 and task 1.8: each link names one file. `context.md` records each extra link. |
| commands-and-events runs in parallel and edits `CHANGELOG.md`, `technical-design.md`, and the Framework test project. | Medium | Low | Each phase checks the counts of its own classes. Phase 4 edits the docs last. A merge conflict is a text conflict only. |
| The reading of "drops its bindings" differs from the intent of the user. | Medium | Medium | Notes question 2. The change to option (b) is one call in `ElementList.RemoveAt` and in `UiContext.RemoveRoot`. |

---

## Dependencies

### Internal Dependencies
- `state-and-commands/store-and-subscription` (done): `IBindingSource`, `Store<T>` (`Value`, `Version` from 1, `Set`), the Framework test project, and its copy of `AllocationGate`.
- `interactive-elements`: `Label.SetText` (UTF-8 span and string), `Toggle.Checked`, `Slider.Value`, `TextField.SetText(string?)`, and the rule that a code setter calls no delegate.
- `core-tree`: `Element`, `AttachToParent`, the dirty bits, `UiContext.Update`, and the shared 1000-element tree.
- `input-routing`: the input pass (`DrainEvents`) and the caller target of a pointer event.
- `dev-ops`: `brutal-ui.sln`, `scripts/test-linux.sh`, and `scripts/test-windows.ps1` (both run `dotnet test --solution brutal-ui.sln`).
- Sibling: commands-and-events has no dependency on this feature. action-map uses `Label.BindText` for the HelloPanel sample.

### External Dependencies
- None. No new package and no native dependency. `System.IUtf8SpanFormattable` and `System.Globalization.NumberFormatInfo` come from the .NET 10 base library.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. A label bound to a number slice shows the slice after the first `Update`, and after each later `Update` that follows a new version. Two sets before one `Update` show the last value. A change of another slice keeps the text (`LabelBindingTests`).
- [ ] F2. The binding pass runs after the input pass and before the style pass: a value from an input handler applies in the same `Update`, and a value that a handler writes reaches the style of the same `Update` (`BindingPassTests`).
- [ ] F3. A store change moves a bound toggle, slider, and text field and calls no delegate slot. A user change that writes the store back gives one delegate call and one new version (`WidgetBindingTests`).
- [ ] F4. An element outside the tree applies nothing, and it applies a new version when it joins again. `ClearBindings` drops each binding. A dropped label is collected while its store lives. A throwing handler stops one `Update`, and a nested `Update` throws (`BindingLifecycleTests`, `LabelBindingTests`).
- [ ] F5. A format string and a provider format the number, and a text of 40 bytes grows the buffer (`LabelBindingTests`).
- [ ] F6. `BoundTreeAllocationTests` measures 0 bytes for the changing store, for a change of one slice, and for a user flip with a write-back, and its real-work test passes. The plant in `BindingPass` and the `ToString` plant each give a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. The new classes hold these counts: `BindingPassTests` 10, `BindingLifecycleTests` 8, `LabelBindingTests` 12, `WidgetBindingTests` 10, and `BoundTreeAllocationTests` 4.
- [ ] Q3. Each public member of the new and changed source files has an XML doc comment. The new doc comments and the changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q4. `src/BrutalUi.Framework/Binding/` holds no `ToString(` call, no `event`, and no `lock` in code.

### Integration Criteria
- [ ] I1. `src/BrutalUi.Core/` names no Framework type in code (no `BrutalUi.Framework`, no `Store<`).
- [ ] I2. In `UiContext.Update`, `UpdateBindings();` comes after `DrainEvents();` and before `UpdateStyles();`.
- [ ] I3. The only changed project file is `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`. No file of `scripts/`, `samples/`, `src/BrutalUi.Framework/State/`, or `src/BrutalUi.Core/Elements/` has a diff against `<base>`, and no existing test file changed.
- [ ] I4. Sections 4, 10, and 14 of `docs/technical-design.md` state the binding pass, the binding rules, and the gate. `CHANGELOG.md` names the binding pass and the bind methods.
- [ ] I5. `scripts/test-linux.sh` prints its PASS line, and both test scripts run `dotnet test --solution brutal-ui.sln` (A3).

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` from `docs/features/state-and-commands/element-bindings/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed (Q2).
3. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5` for `BindingPassTests` (10, F2) and `BindingLifecycleTests` (8, F4). Run `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5` for `LabelBindingTests` (12, F1, F4, F5), `WidgetBindingTests` (10, F3), and `BoundTreeAllocationTests` (4, F6). Expect `failed: 0` and these totals. Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Core/Binding/BindingPass.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of the slot loop in `Visit`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file, check it with `cmp`, and run step 2 again (F6).
5. Copy `src/BrutalUi.Framework/Binding/LabelBindings.cs` to `<scratch>`. In the apply method of the number target, replace the `TryFormat` path with `label.SetText(value.ToString());`. Run the command of step 4. Expect a value that is not 0. Restore the file, check it with `cmp`, and run step 2 again (F6, A4).
6. `grep -n 'DrainEvents();\|UpdateBindings();\|UpdateStyles();' src/BrutalUi.Core/Tree/UiContext.cs`. In the body of `Update`, expect the three lines in this order (I2).
7. `grep -rnE 'BrutalUi\.Framework|Store<' src/BrutalUi.Core/ --include=*.cs | grep -v '///'`. Expect no line (I1). `grep -rnE 'ToString\(|\bevent\b|\block\b' src/BrutalUi.Framework/Binding/ | grep -v '///'`. Expect no line (Q4).
8. `git diff <base> --name-only -- '*.csproj'`. Expect `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj` only. `git diff <base> --stat -- scripts/ samples/ src/BrutalUi.Framework/State/ src/BrutalUi.Core/Elements/`. Expect no output. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect the Framework test project file and `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs` at most, and no file that existed at `<base>` other than the project file (I3).
9. `grep -n 'dotnet test --solution brutal-ui.sln' scripts/test-linux.sh scripts/test-windows.ps1`. Expect a match in each file. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I5).
10. Read section 4 ("Frame order"), section 10 ("Data binding"), and section 14 of `docs/technical-design.md`, and the `## [Unreleased]` part of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q3).

---

## Testing Strategy

### Unit Tests
- `Core.Tests/Binding/BindingPassTests` (10): the first apply, no new version, two versions in one frame, the order before the style pass, the order after the input pass, the order of add, the null checks, and the three subtree flag rules.
- `Core.Tests/Binding/BindingLifecycleTests` (8): a remove, a join with and without a new version, a removed root, a move to another context, `ClearBindings`, a throwing handler, and a nested `Update`.
- `Framework.Tests/Binding/LabelBindingTests` (12): the number text, the slices, the null checks, the string text, the format, the provider, the buffer growth, and the GC rule.
- `Framework.Tests/Binding/WidgetBindingTests` (10): each widget takes its slice, a store change calls no delegate, the three round trips, and the null checks.

### Allocation Tests
- `Framework.Tests/Binding/BoundTreeAllocationTests` (4): the changing store, a change of one slice, a user flip with a write-back, and the real-work check, on the 1000-element widget tree with 40 bound labels and three bound widgets. Two plants guard the pass and the span formatter.

### Regression Tests
- Each existing test runs with no edit. The 13 allocation gates of Core, ImGui, and Markup, and `StoreAllocationTests`, prove that a tree with no binding costs no allocation.

### Integration Tests
- `scripts/test-linux.sh` runs the full suite and the HelloPanel smoke run. HelloPanel does not change. action-map adds the bound label to the sample.

### Manual Testing
- None in this feature. action-map adds the visible demo of a bound label in HelloPanel.

---

## Related Documentation

- **Requirements:** `docs/features/state-and-commands/element-bindings/requirements.md`
- **Epic:** `docs/features/state-and-commands/epic-overview.md`, `docs/features/state-and-commands/epic-requirements.md`
- **Siblings:** `docs/features/state-and-commands/store-and-subscription/implementation.md` (Notes item 1, the seam options), `store-and-subscription/context.md`, `store-and-subscription/code-review.md` (follow-ups 1 and 2), `commands-and-events/requirements.md`, `action-map/requirements.md`
- **Design:** `docs/technical-design.md` sections 4 (frame order), 5 (element tree), 10 (Framework Layer, Data binding), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Toggle.cs` (a static handler in a `static readonly` field, a code setter that calls no delegate), `src/BrutalUi.Core/Input/ElementHandlers.cs` (the slots of an element), `src/BrutalUi.Core/Tree/ElementBuilder.cs` (an extension method that returns the element), `src/BrutalUi.Core/Style/StylePass.cs` (a pass with its own recursion), `src/BrutalUi.Core/Tree/UiContext.cs` (`UpdateStyles` and the re-entry rule of the drain), `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs` (the gate shape), and `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj` (a linked shared file)
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

This plan is a draft with no interview. Each question below has a recommended option, and the plan uses it.

1. **The seam for the binding list.** `Element` lives in Core and cannot name `Store<T>`.
   - **(A) Recommended:** A Core struct `ElementBinding` (an `IBindingSource`, the applied version, a static `BindingHandler`, and a target) in a pooled array on `Element`, a public `Element.AddBinding`, and the typed bind methods as Framework extension methods (options (a) and (c) of store-and-subscription). Decisions 1, 2, and 8.
   - (B) A Core interface `IElementBinding` and a sealed generic Framework class for each binding (option (b)). The list holds references, against the struct rule of the epic.
   - (C) Option (A) with an internal `AddBinding` and `InternalsVisibleTo("BrutalUi.Framework")` in the Core project. A game and xml-markup then cannot add a binding of their own.
2. **The meaning of "an element that leaves the tree drops its bindings".**
   - **(A) Recommended:** The element keeps its list, and the pass skips it outside the tree. The first `Update` after a join applies a new version. `ClearBindings` drops the list for good. The store keeps no reference to the element (Decision 6).
   - (B) A remove from a child list and `UiContext.RemoveRoot` clear the bindings of the subtree. A panel that leaves and joins the tree then needs a new bind.
3. **The demo of phase 1 (Slice rule 12).**
   - **(A) Recommended:** `**Demo:** a developer runs dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*LabelBindingTests' and reads a total of 6 with failed: 0. The first test binds a label to a store score of 42 and reads the text 42 after one Update.`
   - (B) `**Demo:** a person runs scripts/test-linux.sh, reads its PASS line, and finds the Framework test assembly with 0 failed in artifacts/test-linux/test.log.`
   - (C) `**Demo:** a developer runs the one test ABoundLabelShowsTheScoreAfterTheFirstUpdate with --filter-method and reads 1 passed.`
4. **The culture of a number text with no provider.**
   - **(A) Recommended:** `null` gives the current culture, as `ToString` and `TryFormat` do. A game passes `CultureInfo.InvariantCulture` for a fixed form.
   - (B) `null` gives the invariant culture, so a number looks the same on each machine. A game passes a culture for a local form.

### Conflicts and Gaps

1. **The store-and-subscription plan expected a `StoreSubscription` for each binding.** This plan keeps the applied version in the Core struct and keeps no subscription in the target (Decision 2). `StoreSubscription` does not change and stays the pull tool for game code.
2. **"Marks the element dirty".** The pass marks no bit itself. Each code setter marks the bits of its change and marks nothing for a same value (Decision 4).
3. **A project file changes.** `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj` gets four `Compile` links (Decision 9). store-and-subscription kept each project file the same. This feature needs a measurer and the 1000-element tree in the Framework tests, and A2 of the epic forbids a new project.
4. **The shared tree holds no `Label`.** `ThousandElementTree` holds text leaves with a measure callback, not labels. The gate scene replaces the 40 name leaves of the stat rows with labels and keeps 1000 elements (task 4.2). The `All` array of `ThousandWidgetTree` then holds the old leaves at those 40 slots, so the gate does not read `All`.
5. **The technical design names "store notifications".** Section 4 states that the framework applies "pending mode changes and store notifications" during `Update`. The store raises no notification (pull model). Task 4.5 replaces the text with the binding pass. The mode diff of panels-and-modes stays out of scope.
6. **The test scripts (A3).** The requirement states that the test scripts run the gate. Both scripts run `dotnet test --solution brutal-ui.sln`, so they run each test of the Framework test project with no change. Task 4.8 checks it.
7. **The base commit.** HEAD at the time of the plan is `2ac76be`, and the files of store-and-subscription are not committed yet. Task 1.1 records the base after that commit.
8. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 4.8 runs it on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
