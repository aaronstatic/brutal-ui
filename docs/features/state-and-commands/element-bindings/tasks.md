# state-and-commands/element-bindings - Task Checklist

**Last Updated:** 2026-10-29 08:58
**Progress:** 30/30 tasks complete (100%)
**Advanced phases:** Phase 1 is ADVANCED (opus). Phases 2, 3, and 4 are STANDARD (sonnet). Phase 4 escalates to ADVANCED when the gate measures bytes that the Risks table does not explain.

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (The Seam, Public API, Internal Shape, Data Flow, The No-Loop Rule) and "Key Technical Decisions" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/<Project>/<Project>.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. `UiContext.cs` and `Element.cs` have more than 300 lines: read them in ranges (tool rule 2). A doc comment in Core must not use `<see cref>` to a Framework type.

**No-change rule:** these files do not change: `IBindingSource.cs`, each file in `src/BrutalUi.Framework/State/`, `Label.cs`, `Toggle.cs`, `Slider.cs`, `TextField.cs`, `ElementList.cs`, each `.csproj` other than `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file.

---

## Phase 1: The Pass and the Number Text of a Label (10/10 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*LabelBindingTests'` and reads a total of 6 with `failed: 0`. The first test binds a label to a store score of 42 and reads the text `42` after one `Update`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

*Group A (the Core list and the pass):*

- [x] ✅ **1.1 Record the counts in context.md** - Completed 2026-10-29
  - Description: In `docs/features/state-and-commands/element-bindings/context.md`, section "Base and Counts", record the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Framework.Tests` before any code change. The base commit is already there. Read each project count from a run of that project. The Seam, the Internal Shape, the No-Loop Rule, and the parallel-sibling note are already in "Important Decisions".
  - File(s): `docs/features/state-and-commands/element-bindings/context.md`
  - Estimate: 4 steps

- [x] ✅ **1.2 Write ElementBinding.cs** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Core/Binding/ElementBinding.cs` with two types. (1) `public delegate void BindingHandler(Element element, object? target);`. The summary: the apply handler of a binding. The remarks: the binding pass calls it one time for each new version of the source, before the style pass. Pass a static method in a `static readonly` field, so the add allocates no delegate. The handler writes a value of its element through a setter that marks the dirty bits of the change. It must not add or remove an element, and it must not add or clear a binding. (2) `internal struct ElementBinding` with the four fields of the Internal Shape (`IBindingSource Source`, `ulong AppliedVersion`, `BindingHandler Handler`, `object? Target`) and a doc comment on each field.
  - File(s): `src/BrutalUi.Core/Binding/ElementBinding.cs`
  - Estimate: 4 steps

- [x] ✅ **1.3 Edit Element.cs: the list, AddBinding, and the subtree flag** - Completed 2026-10-29
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: add the two fields, `internal ReadOnlySpan<ElementBinding> Bindings`, `internal bool HasBoundSubtree`, `public int BindingCount`, and `public void AddBinding(IBindingSource source, BindingHandler handler, object? target)` (the Internal Shape). The `AddBinding` remarks state: add a binding at build time. The list grows by doubling and never shrinks, so the add can allocate. The first `Update` applies the binding, and each later `Update` applies it one time for each new version. The element holds the source, and the source holds no reference to the element. Edit `AttachToParent`: send the flag up the new parent chain (the Internal Shape). Add one paragraph to the class remarks: the binding list and the binding pass.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 8 steps

- [x] ✅ **1.4 Write BindingPass.cs** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Core/Binding/BindingPass.cs`: `internal static class BindingPass` with `Run(ReadOnlySpan<Element> roots)` and a private recursive `Visit` (the Internal Shape). Read the slot again by index after each handler call, and write `AppliedVersion` before the call (Decision 7). The class remarks state the skip rule, the order (a parent before its children, the bindings of one element in the order of add), and "the pass allocates nothing".
  - File(s): `src/BrutalUi.Core/Binding/BindingPass.cs`
  - Estimate: 5 steps

- [x] ✅ **1.5 Edit UiContext.cs: UpdateBindings and the frame order** - Completed 2026-10-29
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `internal void UpdateBindings()` next to `UpdateStyles()`, with remarks in the style of `UpdateStyles`. In `Update`, call `UpdateBindings();` right after `DrainEvents();`, with the comment `// The binding pass. It applies each new store version before the style pass reads the elements.` Edit the remarks of `Update`: the frame order is the input pass, the binding pass, the style pass, the transition pass, the layout pass, the caret pass, and the clear pass. A value from an input handler reaches the binding pass of the same call. A tree with no binding costs one flag check for each root.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **1.6 Write BindingPassTests.cs (10 tests)** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Core.Tests/Binding/BindingPassTests.cs` with 10 tests. Use a private sealed `TestSource : IBindingSource` with a settable `Version` (start 1), a private sealed `ApplyLog` target that counts calls and records the order, and a static handler in a `static readonly` field. Tests: `TheFirstUpdateAppliesEachBinding`, `AnUpdateWithNoNewVersionAppliesNothing`, `TwoVersionsBeforeOneUpdateGiveOneApply`, `TheBindingPassRunsBeforeTheStylePass` (the handler writes `element.Inline.Color`, and `element.Style.Color` holds the new color after the same `Update`), `AVersionFromAnInputHandlerAppliesInTheSameUpdate` (a `PointerDown` with the element as the caller target. Its handler raises the version), `TwoBindingsOnOneElementApplyInTheOrderOfAdd`, `AddBindingRejectsANullSourceOrHandler` (`ArgumentNullException` with the parameter name for each), `AddBindingMarksTheBoundPathOnly` (`HasBoundSubtree` is `true` on the element and each ancestor and `false` on a sibling branch), `AnAttachOfABoundSubtreeMarksTheNewAncestors`, and `ABranchWithNoBindingLosesItsMarkAtTheNextUpdate` (remove the bound child, run `Update`, and read `false` on the old parent).
  - File(s): `tests/BrutalUi.Core.Tests/Binding/BindingPassTests.cs`
  - Estimate: 8 steps

- [x] ✅ **1.7 Run the gate of group A and record the results** - Completed 2026-10-29
  - Description: Run the build and the tests. Run `BindingPassTests` and expect a total of 10 with `failed: 0`. Run `ThousandElementAllocationTests` and `ThousandWidgetAllocationTests` and expect `failed: 0` (no cost for a tree with no binding). Record the results in `context.md`.
  - File(s): `docs/features/state-and-commands/element-bindings/context.md`
  - Estimate: 4 steps

*Group B (the number text of a label):*

- [x] ✅ **1.8 Link RecordingRenderer.cs into the Framework test project** - Completed 2026-10-29
  - Description: Edit `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`: add `<Compile Include="../BrutalUi.Core.Tests/Shared/RecordingRenderer.cs" LinkBase="Shared" />` (Decision 9). Link each file by name. Do not use `*.cs`, because the project holds its own copy of `AllocationGate`. When the linked file needs another file of `Shared/`, link that file too, except `AllocationGate.cs`, and record it in `context.md`.
  - File(s): `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`
  - Estimate: 3 steps

- [x] ✅ **1.9 Write LabelBindings.cs: the number overload** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Binding/LabelBindings.cs`: `public static class LabelBindings` with the number overload of the Public API (`BindText<T, TValue>(this Label label, Store<T> store, Func<T, TValue> selector, string? format = null, IFormatProvider? provider = null) where TValue : IUtf8SpanFormattable`) and the private nested `NumberText<T, TValue>` target (the Internal Shape: the store, the selector, the format, the provider, a `byte[]` scratch of 32 bytes, double on `false`, throw `InvalidOperationException` above 4096 bytes, then `((Label)element).SetText(scratch.AsSpan(0, written))`). Check the label, the store, and the selector with `ArgumentNullException.ThrowIfNull`. Return the label. The remarks state: the binding formats the slice with `IUtf8SpanFormattable.TryFormat` into a buffer of the binding, and it runs no `ToString` (A4). `null` for the provider gives the current culture, as `ToString` does. The scratch grows by doubling and never shrinks, so a text that is longer than each earlier text allocates one time. Pass a `static` lambda or a cached method group. A selector must not allocate. A `SetText` from code on a bound label shows until the next new version of the store. The binding clears `Label.LocalizedText`. The extension form exists because `Label` lives in Core and cannot name `Store<T>`.
  - File(s): `src/BrutalUi.Framework/Binding/LabelBindings.cs`
  - Estimate: 6 steps

- [x] ✅ **1.10 Write LabelBindingTests.cs (6 tests) and run the demo** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs` with 6 tests on a private `record struct HudState(int Score, int Lives)` and a context with a `RecordingRenderer` measurer: `ABoundLabelShowsTheScoreAfterTheFirstUpdate` (the demo: the store score 42, one `Update`, the text `"42"u8`, and `Assert.Same` for the return value), `ASetOfANewScoreChangesTheTextAtTheNextUpdate`, `TwoSetsBeforeOneUpdateShowTheLastScore`, `AChangeOfAnotherSliceKeepsTheText`, `TwoLabelsOnOneStoreShowTheirOwnSlices`, and `BindTextRejectsANullStoreOrSelector`. Each selector is a `static` lambda. Run the build, the tests, and the class. Expect a total of 6 with `failed: 0`. Record the demo result in `context.md`.
  - File(s): `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs`, `docs/features/state-and-commands/element-bindings/context.md`
  - Estimate: 7 steps

---

## Phase 2: The Widget Values and the String Text (6/6 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*WidgetBindingTests'` and reads a total of 10 with `failed: 0`. The test `AUserFlipWritesTheStoreOneTimeWithNoLoop` shows one delegate call and one new store version for one click.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **2.1 Write StoreSlice.cs** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Binding/StoreSlice.cs`: `internal sealed class StoreSlice<T, TValue>` with the store, the selector, and `Read()` (`TValue Read() => Selector(Store.Value)`, the Internal Shape). The class remarks state: one instance for each binding, created at build time.
  - File(s): `src/BrutalUi.Framework/Binding/StoreSlice.cs`
  - Estimate: 3 steps

- [x] ✅ **2.2 Write WidgetBindings.cs** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Binding/WidgetBindings.cs`: `public static class WidgetBindings` with `BindChecked`, `BindValue`, and `BindText` (TextField) of the Public API. Each method checks its arguments with `ArgumentNullException.ThrowIfNull`, creates a `StoreSlice`, calls `AddBinding` with a static handler in a `static readonly` field, and returns the element. The handlers write `toggle.Checked = slice.Read()`, `slider.Value = slice.Read()`, and `field.SetText(slice.Read())`. The remarks of each method state: the code setter calls no delegate, so a store change calls no `CheckedChanged`, `ValueChanged`, or `TextChanged` (the No-Loop Rule). A user change still calls the delegate, and the game writes the store there (the epic has no two-way helper). The slider applies its value rule, so the slider can show a value that differs from the store (a clamp or a grid step). A new text from the store puts the caret of the field at the end and clears the selection. The field cuts a text at its capacity.
  - File(s): `src/BrutalUi.Framework/Binding/WidgetBindings.cs`
  - Estimate: 6 steps

- [x] ✅ **2.3 Add the string overload to LabelBindings.cs** - Completed 2026-10-29
  - Description: Edit `src/BrutalUi.Framework/Binding/LabelBindings.cs`: add the string overload of the Public API (`BindText<T>(this Label label, Store<T> store, Func<T, string?> selector)`). Its handler calls `Label.SetText(string?)`, which encodes with no allocation when the buffer is large enough. `null` gives an empty text.
  - File(s): `src/BrutalUi.Framework/Binding/LabelBindings.cs`
  - Estimate: 4 steps

- [x] ✅ **2.4 Write WidgetBindingTests.cs (10 tests)** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` with 10 tests on a private `record struct FormState(bool Muted, float Volume, string Name)`. Give each widget an inline size, and give each a counter delegate. Tests: `ABoundToggleTakesTheSliceAtTheFirstUpdate`, `AStoreChangeFlipsTheToggleAndCallsNoDelegate`, `ABoundSliderTakesTheSliceThroughTheValueRule` (range 0 to 100, step 1, store value 50.4, slider value 50), `AStoreChangeMovesTheSliderAndCallsNoDelegate`, `ABoundTextFieldTakesTheStringSlice`, `AStoreChangeSetsTheFieldTextAndCallsNoDelegate`, `AUserFlipWritesTheStoreOneTimeWithNoLoop` (a press and a release at the center of the toggle. `CheckedChanged` writes `Muted` back. After two `Update` calls: one delegate call, the version raised by one, and the toggle checked), `AUserKeyOnTheSliderWritesTheStoreOneTimeWithNoLoop` (focus the slider, push a Right key down with the key code 262, the GLFW value of Right), `AUserCharInTheFieldWritesTheStoreOneTimeWithNoLoop` (focus the field, push the char `a`. `TextChanged` writes the UTF-8 text back as a string. The caret stays at the end), and `EachWidgetBindMethodRejectsANullStoreOrSelector`. The first, third, and fifth tests also check the return value with `Assert.Same`.
  - File(s): `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs`
  - Estimate: 9 steps

- [x] ✅ **2.5 Add 2 string tests to LabelBindingTests.cs** - Completed 2026-10-29
  - Description: Edit `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs`: add 2 tests, `ALabelShowsAStringSlice` and `ANullStringSliceGivesAnEmptyText`, on a store of a `record struct` with a `string? Title`.
  - File(s): `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs`
  - Estimate: 3 steps

- [x] ✅ **2.6 Run the gate of phase 2 and record the results** - Completed 2026-10-29
  - Description: Run the build, the tests, `WidgetBindingTests` (total 10), and `LabelBindingTests` (total 8). Expect `failed: 0`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/element-bindings/context.md`
  - Estimate: 4 steps

---

## Phase 3: The Leave Rule, the Error Rule, and the Format (6/6 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*BindingLifecycleTests'` and reads a total of 8 with `failed: 0`. The same command on the Framework test project with `'*LabelBindingTests'` reads a total of 12 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 Add ClearBindings to Element.cs** - Completed 2026-10-29
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: add `public void ClearBindings()`. It writes `default` into each used slot, so the element drops its references to each source and each target, and it sets the count to 0. It keeps the array. It does not change `HasBoundSubtree`: the next pass clears a stale flag. The remarks state the leave rule (Decision 6): an element outside the roots of a context applies nothing. It keeps its bindings, and the first `Update` after a join applies a new version. Call `ClearBindings` to drop the bindings for good. A store holds no reference to an element, so a dropped element needs no unsubscribe.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 4 steps

- [x] ✅ **3.2 Add the re-entry guard to UiContext.cs** - Completed 2026-10-29
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add a `private bool _binding;` flag. `UpdateBindings` sets it in a `try` and clears it in a `finally`. `Update` throws `InvalidOperationException` ("A binding handler must not call Update.") when the flag is set. Extend the remarks of `Update`: an exception from a binding handler stops the call before the style pass. That binding keeps the new version, so it does not apply again until the next new version. The bindings after it apply at the next `Update` (Decision 7).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] ✅ **3.3 Write BindingLifecycleTests.cs (8 tests)** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Core.Tests/Binding/BindingLifecycleTests.cs` with 8 tests and the helpers of `BindingPassTests` (copy the private helpers, do not share them): `ARemovedElementGetsNoApply`, `AnElementThatJoinsAgainAppliesTheNewVersion`, `AnElementThatJoinsAgainWithNoNewVersionAppliesNothing`, `ARemovedRootGetsNoApply`, `AMoveToAnotherContextAppliesInTheNewContext`, `ClearBindingsDropsEachBindingAndItsReferences` (`BindingCount` 0, each slot of `Bindings` is `default`, and no apply after a new version), `AHandlerThatThrowsUsesUpItsChangeAndStopsTheUpdate` (the exception leaves `Update`, the element keeps its `Style` bit, the next `Update` applies the second binding and does not call the first handler again), and `AHandlerThatCallsUpdateGetsAnException`.
  - File(s): `tests/BrutalUi.Core.Tests/Binding/BindingLifecycleTests.cs`
  - Estimate: 8 steps

- [x] ✅ **3.4 Add 4 format and GC tests to LabelBindingTests.cs** - Completed 2026-10-29
  - Description: Edit `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs`: add 4 tests. `AFormatStringFormatsTheNumber` (a `float` 3.14159, the format `F2`, `CultureInfo.InvariantCulture`, the text `3.14`), `AProviderFormatsTheNumber` (a clone of the invariant `NumberFormatInfo` with the decimal separator `,`, the text `3,14`), `ALongTextGrowsTheBuffer` (the format `D40`, a text of 40 bytes, above the 32 bytes of the start scratch), and `AStoreKeepsNoReferenceToADroppedLabel` (a `[MethodImpl(MethodImplOptions.NoInlining)]` helper builds a context with a bound label, runs one `Update`, removes the root, and returns a `WeakReference` to the label. Then `GC.Collect()`, `GC.WaitForPendingFinalizers()`, and `GC.Collect()`. The weak reference is dead, and the store is alive through `GC.KeepAlive`).
  - File(s): `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.5 Run the gate of phase 3** - Completed 2026-10-29
  - Description: Run the build, the tests, `BindingLifecycleTests` (total 8), `BindingPassTests` (total 10), and `LabelBindingTests` (total 12). Expect `failed: 0`. Run `AStoreKeepsNoReferenceToADroppedLabel` five times with `--filter-method` and expect 5 passes.
  - File(s): none (runs only)
  - Estimate: 5 steps

- [x] ✅ **3.6 Record the results, the leave rule, and the error rule** - Completed 2026-10-29
  - Description: Record the results, the leave rule, and the error rule in `context.md`.
  - File(s): `docs/features/state-and-commands/element-bindings/context.md`
  - Estimate: 2 steps

---

## Phase 4: The 0-Byte Gate on the Bound Tree and the Docs (8/8 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*BoundTreeAllocationTests'` and reads a total of 4 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the gate measures bytes that the Risks table does not explain.

*Group A (the gate):*

- [x] ✅ **4.1 Link the shared tree files into the Framework test project** - Completed 2026-10-29
  - Description: Edit `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`: link `ThousandElementTree.cs`, `ThousandWidgetTree.cs`, and `TextMeasure.cs` of `tests/BrutalUi.Core.Tests/Shared/` by name, as in task 1.8. The linked types are `internal` in `namespace BrutalUi.Core.Tests`, so each test file that uses them adds `using BrutalUi.Core.Tests;`.
  - File(s): `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`
  - Estimate: 3 steps

- [x] ✅ **4.2 Write BoundTree.cs** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Shared/BoundTree.cs`: `internal sealed class BoundTree` with `static BoundTree Build()`. It calls `ThousandWidgetTree.Build()`. It creates a `Store<HudState>` with a private `record struct HudState(int Score, int Lives, float Volume, bool Muted, string Name)`. In each of the 40 stat rows (the children of `StatRow.Parent` with the class `row`), it replaces the child at the index of `StatName` in `StatRow` with a new `Label` through `Children.RemoveAt` and `Children.Insert`. Each label starts with a text of 8 ASCII bytes, so its buffer holds each later text. It binds rows 0 to 37 in turn to `Score` and `Lives`, row 38 to `Volume` with the format `F1` and the invariant culture, and row 39 to `Name` (the string overload). It binds the toggle to `Muted`, the slider to `Volume`, and the text field to `Name`. The toggle `CheckedChanged` writes `Muted` back to the store. The slider `ValueChanged` and the field `TextChanged` only count. It throws `InvalidOperationException` when `ThousandWidgetTree.CountSubtree(Root)` is not 1000 or when it found not 40 rows. `static HudState Next(int round)` gives `Score = 10000 + round`, `Lives = 10 + round % 10`, `Volume = 10 + round % 80`, `Muted = round % 2 == 1`, and `Name` from two strings that the build creates. Each text thus keeps a fixed width for each round. The remarks state the scene and the width rule.
  - File(s): `tests/BrutalUi.Framework.Tests/Shared/BoundTree.cs`
  - Estimate: 9 steps

- [x] ✅ **4.3 Write BoundTreeAllocationTests.cs (4 tests)** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Binding/BoundTreeAllocationTests.cs` with 4 tests in the shape of `ThousandWidgetAllocationTests` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, 100 measured iterations, `Assert.Equal(0L, ...)`): `UpdateAndDrawWithAChangingStoreAllocateNothing` (each frame sets `Next(round)`, so each slice changes, and runs `Update` and `Draw`), `AChangeOfOneSliceAllocatesNothing` (each frame changes `Lives` only. Each binding applies, and the labels of the other slices compare the same bytes and mark nothing), `AUserFlipWithAWriteBackAllocatesNothing` (each frame pushes a press and a release at `ToggleCenter` with no target, then runs `Update` and `Draw`), and `TheMeasuredBodyDoesRealWork` (after one frame of each body: the text of row 0 is the UTF-8 form of `10000 + round`, the toggle and the slider and the field hold the slices of the store, `ValueChanged` and `TextChanged` counts are 0, one flip gives one `CheckedChanged` call and one new version). The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, and no `Assert`.
  - File(s): `tests/BrutalUi.Framework.Tests/Binding/BoundTreeAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **4.4 Run the gate and the two plants** - Completed 2026-10-29 (fix 1: the Debug box guard in `NumberText.Apply`)
  - Description: Run the build, the tests, and the class of task 4.3. Plant `GC.KeepAlive(new object());` as the first line of the slot loop in `BindingPass.Visit`, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`, and expect a value that is not 0. Record which gates fail and the bytes. Restore the file from a copy in the scratchpad and check it with `cmp`. Plant `label.SetText(value.ToString());` in place of the `TryFormat` path of `NumberText.Apply`, run the same command, expect a value that is not 0, record the result, and restore. Run the tests again and expect 0 failed.
  - File(s): `src/BrutalUi.Core/Binding/BindingPass.cs` (plant, then restore), `src/BrutalUi.Framework/Binding/LabelBindings.cs` (plant, then restore), `docs/features/state-and-commands/element-bindings/context.md`
  - Estimate: 8 steps

*Group B (the docs):*

- [x] ✅ **4.5 Edit section 4 of the technical design** - Completed 2026-10-29
  - Description: Edit `docs/technical-design.md` section 4: in the overview diagram, replace the line `Framework: mode diff, store notifications, bindings` with `Binding pass: apply each new store version to its bound elements`. In "Frame order" item 2, name the binding pass after the input pass, and add a paragraph "The binding pass at the seam" in the style of the style pass paragraph (the skip flag, the order, the code setters, the error rule, and the cost of a tree with no binding).
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] ✅ **4.6 Edit sections 10 and 14 of the technical design** - Completed 2026-10-29
  - Description: Edit section 10 of `docs/technical-design.md`, "Data binding": add the binding rules (the struct list on `Element`, `AddBinding` at build time, the bind methods in Framework, the span formatter, the no-loop rule, the leave rule, and the error rule). Edit section 14: add a bullet for the binding tests (`BindingPassTests`, `BindingLifecycleTests`, `LabelBindingTests`, `WidgetBindingTests`) and a bullet "A fifteenth allocation gate" for `BoundTreeAllocationTests` with the scene and the plant results of task 4.4.
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] ✅ **4.7 Edit CHANGELOG.md** - Completed 2026-10-29
  - Description: Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: the binding pass in `UiContext.Update`, `Element.AddBinding`, `Element.ClearBindings`, `BindingHandler`, `LabelBindings.BindText`, `WidgetBindings`, and the bound-tree allocation gate (state-and-commands/element-bindings).
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] ✅ **4.8 Run the scripts and the lint, and record the results** - Completed 2026-10-29
  - Description: Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `grep -n 'dotnet test --solution brutal-ui.sln' scripts/test-linux.sh scripts/test-windows.ps1` and expect one line for each script (A3: the scripts run the gate with no change). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on the changed docs. Record the results and each decision that this plan did not settle in `context.md`.
  - File(s): `docs/features/state-and-commands/element-bindings/context.md`
  - Estimate: 5 steps

---

## Cross-Phase Review

- ✅ **CR-1 to CR-5 of `code-review.md`** - Completed 2026-10-29 (not counted in the 30 tasks)
  - Fix groups `review-g1.md` (CR-1), `review-g2.md` (CR-2, follow-up 1), `review-g3.md` (CR-3), `review-g4.md` (CR-4), and `review-g5.md` (CR-5). The review verifier returns PASS (round 2).

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

## Progress Tracking

### Completed This Session (2026-10-29)
- ✅ Phase 1 (tasks 1.1 to 1.10), phase 2 (tasks 2.1 to 2.6), phase 3 (tasks 3.1 to 3.6), and phase 4 (tasks 4.1 to 4.8, with fix 1 for the Debug box)
- ✅ The cross-phase review: 5 findings (1 MEDIUM, 4 LOW), 5 fixed
- ✅ The draft plan of the next feature, `state-and-commands/commands-and-events/implementation.md`

### Discovered New Tasks
- none

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
