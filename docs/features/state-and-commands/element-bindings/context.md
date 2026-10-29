# state-and-commands/element-bindings - Context & Decisions

**Last Updated:** 2026-10-29 08:58
**Current Phase:** Complete (phases 1 to 4 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan (`implementation.md`). The user chose (A) for each of the four open scope questions on 2026-10-29.
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file)
- ✅ Phase 1 (tasks 1.1 to 1.10): the Core list, `AddBinding`, the subtree flag, the binding pass, and `Label.BindText` for a number slice. The verifier returns PASS. Demo: PASS.
- ✅ Phase 2 (tasks 2.1 to 2.6): `StoreSlice`, `WidgetBindings`, the string overload of `BindText`, and the no-loop round trips. The verifier returns PASS. Demo: PASS.
- ✅ Phase 3 (tasks 3.1 to 3.6): `ClearBindings`, the re-entry guard of `Update`, `BindingLifecycleTests` (8), and the format and GC tests (`LabelBindingTests` 12). The verifier returns PASS. Demo: PASS.
- ✅ Phase 4 (tasks 4.1 to 4.8): `BoundTree`, `BoundTreeAllocationTests` (4), the two plants in Debug, the Debug box fix in `NumberText.Apply` (fix 1, ADVANCED), sections 4, 10, and 14 of the technical design, and the changelog. The verifier returns PASS. Demo: PASS.
- ✅ The cross-phase review (`code-review.md`): 5 findings (1 MEDIUM, 4 LOW), 5 fixed in review groups 1 to 5. The review verifier returns PASS (round 2).
- ✅ The draft plan of the next feature: `docs/features/state-and-commands/commands-and-events/implementation.md` (the solution-architect writes it).

**What's Next:**
- 📋 A person tests the lines of `## Needs Human Verification`.
- 📋 `/autorun-epic` commits the feature and goes on to `state-and-commands/commands-and-events`.

**Blockers:**
- None

---

## Base and Counts

- Base commit: `27f33c79b9ecf097d180b5c30fe90dd908f88405` (`feat: state-and-commands/store-and-subscription`).
- Counts before any code change (task 1.1, 2026-10-29):
  - The solution (`dotnet test --solution brutal-ui.sln`): total 2580, succeeded 2578, skipped 2, failed 0.
  - `BrutalUi.Core.Tests` (`dotnet run --project`): total 1715, succeeded 1715, skipped 0, failed 0.
  - `BrutalUi.Framework.Tests` (`dotnet run --project`): total 23, succeeded 23, skipped 0, failed 0.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Binding/ElementBinding.cs` - the public delegate `BindingHandler` and the internal struct `ElementBinding` (phase 1)
- `src/BrutalUi.Core/Binding/BindingPass.cs` - the binding pass (phase 1)
- `src/BrutalUi.Core/Tree/Element.cs` - the binding list, `AddBinding`, `BindingCount`, the subtree flag (phase 1), `ClearBindings` (phase 3)
- `src/BrutalUi.Core/Tree/UiContext.cs` - `UpdateBindings` and the frame order (phase 1), the re-entry guard (phase 3)
- `src/BrutalUi.Framework/Binding/LabelBindings.cs` - `Label.BindText` for a number slice (phase 1) and a string slice (phase 2)
- `src/BrutalUi.Framework/Binding/StoreSlice.cs` - the target class: the store and the selector (phase 2)
- `src/BrutalUi.Framework/Binding/WidgetBindings.cs` - `BindChecked`, `BindValue`, `BindText` (TextField) (phase 2)

### Tests
- `tests/BrutalUi.Core.Tests/Binding/BindingPassTests.cs` - 10 tests (phase 1)
- `tests/BrutalUi.Core.Tests/Binding/BindingLifecycleTests.cs` - 8 tests (phase 3)
- `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs` - 6, then 8, then 12 tests (phases 1 to 3)
- `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` - 10 tests (phase 2)
- `tests/BrutalUi.Framework.Tests/Binding/BoundTreeAllocationTests.cs` - the 0-byte gate, 4 tests (phase 4)
- `tests/BrutalUi.Framework.Tests/Shared/BoundTree.cs` - the bound 1000-element scene (phase 4)

### Related Files
- `docs/features/state-and-commands/element-bindings/implementation.md` - Implementation plan
- `docs/features/state-and-commands/epic-overview.md` - the epic rollup and the build order
- `docs/features/state-and-commands/store-and-subscription/context.md` - the store, the pull model, and the seam

---

## Important Decisions

The plan holds Decisions 1 to 10 and the Confirmed Assumptions. The user chose (A) for each of the four Notes questions: the Core struct seam with a public `AddBinding` (Decisions 1, 2, 8), the leave rule that keeps the list (Decision 6), the phase 1 demo on `LabelBindingTests`, and the current culture for a `null` provider. This section records each decision that the run makes past the plan.

- **The Seam.** `Element` and `Label` live in Core, and Core does not reference Framework. The binding list on `Element` holds a non-generic struct `ElementBinding`: the source as an `IBindingSource`, the applied version, a static `BindingHandler`, and a target object. The Core pass reads `IBindingSource.Version` and calls the handler on a change only. The Framework target holds the `Store<T>` and the selector, and the static handler casts the target and the element.
- **The Internal Shape.** `Element` gets `private ElementBinding[]? _bindings;` (null until the first add, then 2 slots, then doubling) and `private int _bindingCount;`, `internal ReadOnlySpan<ElementBinding> Bindings`, and `internal bool HasBoundSubtree`. `AddBinding` and `AttachToParent` set the flag up the parent chain. `BindingPass.Visit` skips a branch with the flag `false`, writes `AppliedVersion` before the handler call, reads the slot again by index after each call, and writes the flag again after it visits the children. `UiContext.Update` calls `UpdateBindings()` right after `DrainEvents()` and before `UpdateStyles()`.
- **The No-Loop Rule.** Each binding writes through a code setter: `Label.SetText`, `Toggle.Checked`, `Slider.Value`, and `TextField.SetText`. A code setter calls no delegate slot, so a store change never calls `CheckedChanged`, `ValueChanged`, or `TextChanged`. A user change calls the delegate one time, the game writes the store, the version rises one time, and the next pass writes the same value. The setter compares and changes nothing.
- **A parallel sibling.** commands-and-events can run in parallel with this feature. Each later count check thus uses the counts of the new classes of this feature, not the solution total alone.
- **The write access of the pass (group A, past the plan).** The Internal Shape gives the pass only the read-only span `Bindings`, but the pass writes `AppliedVersion`. `Element` thus has `internal ref ElementBinding BindingAt(int index)` (a `Debug.Assert` checks the index). The pass gets the reference again for each slot, and the loop reads `BindingCount` again after each handler call. Thus the pass never holds a reference into an old array.
- **One chain mark (group A, past the plan).** `AddBinding` and `AttachToParent` share the private static `Element.MarkBoundChain(Element? start)`. It sets `HasBoundSubtree` from `start` up the parent chain and stops at the first element that holds it. The stop is safe because of the invariant: in a tree, each ancestor of a flagged element is flagged. The pass clears a flag only after it visits the children, so the invariant holds after each pass.
- **The attach method of the target (group B, past the plan).** The brief gives `NumberText<T, TValue>` a `private static readonly BindingHandler s_apply`. The outer class cannot read a private member of a nested class. `NumberText` thus has an instance method `AttachTo(Label label)` that calls `label.AddBinding(_store, s_apply, this)`. `BindText` creates the target and calls `AttachTo`. The field stays private, and the add allocates no delegate.
- **A null slice (group B, past the plan).** `TValue` can be a reference type that implements `IUtf8SpanFormattable`. A `null` slice thus gives an empty text through `SetText(ReadOnlySpan<byte>.Empty)`, not a `NullReferenceException`. Phase 4 fix 1 changes the check to `s_isReferenceType && value is null`, because the JIT of a Debug build keeps the `box` of `value is null` for a struct (see the phase 4 fix 1 notes).
- **The scratch limit (group B).** The scratch starts at 32 bytes and doubles on each `false` of `TryFormat`. When the next size is above 4096 bytes, the handler throws `InvalidOperationException` with a constant message. The message uses no string interpolation.
- **The extra check of `AChangeOfAnotherSliceKeepsTheText` (group B, past the plan).** The test checks `RecordingRenderer.MeasureCount`: a change of the other slice measures nothing again. A control step then changes the bound slice and checks that the count rises. Thus the test shows that the count reacts to a text change. The test also checks `BindingCount` after the bind (1) and after a rejected bind (0).
- **The test values (group B).** The tests use no negative number, because a culture can give another minus sign (for example U+2212), and a `null` provider gives the current culture.
- **The test shapes (group A, past the plan).** `TwoBindingsOnOneElementApplyInTheOrderOfAdd` adds three bindings, so it also covers the grow from 2 slots to 4 slots. `TheFirstUpdateAppliesEachBinding` also checks the order "a parent before its children". `ABranchWithNoBindingLosesItsMarkAtTheNextUpdate` keeps a second bound branch, so it shows that the root keeps its flag while the old parent loses it, and that the removed child keeps its own flag.

---

## Gotchas & Learnings

- **A Debug build keeps the box of `value is null` on a generic struct.** The C# compiler emits `box TValue` for `value is null` on an unconstrained (or interface-constrained) generic value. The optimizing JIT removes the box for a struct. A Debug assembly has `DebuggableAttribute(DisableOptimizations)`, so the JIT compiles in MinOpts, and the box stays: 24 bytes for an `int` or a `float`. `DOTNET_TieredCompilation=0` does not help. Guard each such check in a per-frame path with a `static readonly bool` that holds `!typeof(TValue).IsValueType`, tested first with `&&`. Do not use `EqualityComparer<TValue>.Default.Equals(value, default)`, because `0` equals `default(int)`.
- **A real-work test must read its expected values before the body runs.** CR-1: step 2 of `TheMeasuredBodyDoesRealWork` computed the expected value from `scene.Round`, which the body under test raises. An empty body then kept the round and the old label, and the test passed. Read the round and the version first, then assert fixed values.
- **A round-trip test at the end of a text cannot see a caret reset.** CR-2: type in the middle of the text (`axyb` against `axby`) to pin the caret rule.
- **A verify grep needs the scope of the feature.** The round 1 review verifier matched two sentences of the base commit. Scope a "text is gone" grep to the feature files or to the added lines of `git diff <base>`.

---

## API Integration

- `IBindingSource.Version` (Core, store-and-subscription) is the seam of the pass. `Element.AddBinding` is the public Core entry. `LabelBindings` and `WidgetBindings` in Framework are the typed entries.

---

## State Management

- `Store<T>` (Framework) owns the state and the version. Each `ElementBinding` owns its applied version. The element holds the source, and the source holds no reference to the element.

---

## Testing Approach

### Unit Tests
- `BindingPassTests` (10), `BindingLifecycleTests` (8) in Core.Tests. `LabelBindingTests` (12), `WidgetBindingTests` (10) in Framework.Tests.

### Allocation Tests
- `BoundTreeAllocationTests` (4): the bound 1000-element tree with a changing store. Two plants guard the pass and the span formatter.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*LabelBindingTests'` and reads a total of 6 with `failed: 0`. The first test binds a label to a store score of 42 and reads the text `42` after one `Update`.
- [ ] Phase 2 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*WidgetBindingTests'` and reads a total of 10 with `failed: 0`. The test `AUserFlipWritesTheStoreOneTimeWithNoLoop` shows one delegate call and one new store version for one click.
- [ ] Phase 3 demo: a developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*BindingLifecycleTests'` and reads a total of 8 with `failed: 0`. The same command on the Framework test project with `'*LabelBindingTests'` reads a total of 12 with `failed: 0`.
- [ ] Phase 4 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*BoundTreeAllocationTests'` and reads a total of 4 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
- [ ] Windows run: a person runs `scripts/test-windows.ps1` on a Windows machine and reads its PASS line. The run checks the bound-tree gate on the Windows JIT. No verifier of this run had a Windows machine.

---

## Next Steps

### Immediate (Next Session)
1. Test the lines of `## Needs Human Verification`.
2. Commit the feature (the caller `/autorun-epic` owns each git action).
3. Build `state-and-commands/commands-and-events` from its draft plan. Answer its open scope questions first.

---

## Open Questions

- none

---

## Session Notes

### 2026-10-29 07:40
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`. `/start-feature` created `tasks.md` (30 tasks in 4 phases), `inbox.md`, and this file. The plan status is In Progress.

### 2026-10-29 (phase 1 group A, tasks 1.1 to 1.7)
- Done: the counts (1.1), `ElementBinding.cs` (1.2), the list, `AddBinding`, `BindingCount`, and the subtree flag in `Element.cs` (1.3), `BindingPass.cs` (1.4), `UpdateBindings` and the frame order in `UiContext.cs` (1.5), `BindingPassTests.cs` (1.6).
- Gate results (1.7):
  - `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
  - `BindingPassTests`: total 10, failed 0.
  - `ThousandElementAllocationTests`: total 14, failed 0. `ThousandWidgetAllocationTests`: total 4, failed 0. No gate file changed.
  - The solution: total 2590, succeeded 2588, skipped 2, failed 0 (2580 at the base plus the 10 new tests).
  - In `UiContext.Update`, `UpdateBindings();` is at line 661, after `DrainEvents();` (658) and before `UpdateStyles();` (666).
  - A search of `src/BrutalUi.Core/` for `BrutalUi.Framework` and `Store<` finds nothing.
- The group A brief names no `tasks.md` in its file list, so this run did not mark the tasks there. The caller marks them.

### 2026-10-29 (phase 1 group B, tasks 1.8 to 1.10)
- Done: the link of `RecordingRenderer.cs` in the Framework test project (1.8), `src/BrutalUi.Framework/Binding/LabelBindings.cs` (1.9), `tests/BrutalUi.Framework.Tests/Binding/LabelBindingTests.cs` (1.10).
- Linked files (1.8): `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs` only. It needs no other file of `Shared/`. `AllocationGate.cs` is not linked.
- Demo result (1.10): `dotnet run --project tests/BrutalUi.Framework.Tests -- --filter-class BrutalUi.Framework.Tests.LabelBindingTests` gives total 6, succeeded 6, failed 0. `ABoundLabelShowsTheScoreAfterTheFirstUpdate` shows `"42"u8` after one `Update`.
- Gate results:
  - `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
  - `BindingPassTests`: total 10, failed 0.
  - The solution: total 2596, succeeded 2594, skipped 2, failed 0 (2590 after group A plus the 6 new tests).
  - A search of `src/BrutalUi.Framework/Binding/` for `ToString(` finds nothing. A search of `src/BrutalUi.Core/` for `BrutalUi.Framework` and `Store<` finds nothing.
- The xUnit v3 runner uses the Microsoft Testing Platform. A class filter is `--filter-class <full name>`, not `-class`.
- The group B brief names no `tasks.md` in its file list, so this run did not mark the tasks there. The caller marks them.

### 2026-10-29 07:53 (phase 1 close)
- Phase 1 done (game-dev-advanced, ADVANCED, two groups). The parent marked tasks 1.1 to 1.10 in `tasks.md`.
- Verifier (`phase-1-verify.md`): PASS. Build 0 warnings, 0 errors. Solution 2596 (2594 passed, 2 skipped, 0 failed). `BindingPassTests` 10, `ThousandElementAllocationTests` 14, `ThousandWidgetAllocationTests` 4, each with 0 failed. The order `DrainEvents` (658), `UpdateBindings` (661), `UpdateStyles` (666) holds. Both seam greps find no line. The only changed project file is the Framework test project. No diff in `scripts/`, `samples/`, `src/BrutalUi.Framework/State/`, `src/BrutalUi.Core/Elements/`, or `IBindingSource.cs`.
- Demo: PASS (`LabelBindingTests` 6 of 6, and the first test reads `42` after one `Update`). The `'*LabelBindingTests'` wildcard form of the demo command works.
- Inbox: 0 lines.

### 2026-10-29 (phase 2, tasks 2.1 to 2.6)
- Done: `StoreSlice.cs` (2.1), `WidgetBindings.cs` (2.2), the string overload in `LabelBindings.cs` (2.3), `WidgetBindingTests.cs` with 10 tests (2.4), 2 tests in `LabelBindingTests.cs` (2.5), the runs (2.6).
- Decisions past the brief:
  - The static handlers sit in private generic holder classes (`CheckedHandler<T>`, `ValueHandler<T>`, `TextHandler<T>` in `WidgetBindings`, `StringHandler<T>` in `LabelBindings`). Each holds `internal static readonly BindingHandler Apply`. The bind method passes `Holder<T>.Apply`, so the add allocates no delegate after the first use of a closed type. The holder removes the need for an `AttachTo` method.
  - `StoreSlice` also exposes `Store` and `Selector` as properties. The handlers use only `Read()`.
  - The tests build the pointer, key, and char events by hand. The factories of `PointerTree` are internal to `BrutalUi.Core.Tests`, and the brief allows no change to a `.csproj`.
  - `TextField.CaretIndex` is internal, so the Framework tests cannot read it. `AUserCharInTheFieldWritesTheStoreOneTimeWithNoLoop` shows the caret at the end in another way: a second char `b` after the round trip gives the text `ab`.
  - The test root uses `FlexDirection.Column`, `Align.FlexStart`, and `IsPickable = true`. The toggle is 20 by 20 and the press is at (10, 10).
- Overload check: `label.BindText(store, static s => s.Score)` (an `int`) still picks the number overload, and `label.BindText(store, static s => s.Title)` (a `string?`) picks the string overload. Both compile with no ambiguity. `LabelBindingTests` covers both (the six phase 1 tests use `int`, the two new tests use `string?`).
- Results:
  - `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
  - `WidgetBindingTests`: total 10, succeeded 10, failed 0. `AUserFlipWritesTheStoreOneTimeWithNoLoop`: total 1, failed 0.
  - `LabelBindingTests`: total 8, succeeded 8, failed 0.
  - The solution: total 2608, succeeded 2606, skipped 2, failed 0 (2596 after phase 1 plus 12 new tests).
  - No diff against `27f33c7` in `src/BrutalUi.Core/Elements/`. A search of `src/BrutalUi.Framework/Binding/` for `ToString(`, `event`, and `lock` finds nothing.

### 2026-10-29 07:58 (phase 2 close)
- Phase 2 done (game-dev, STANDARD, one group). The parent marked tasks 2.1 to 2.6 in `tasks.md`.
- Verifier (`phase-2-verify.md`): PASS. Build 0 warnings, 0 errors. Solution 2608 (2606 passed, 2 skipped, 0 failed). `LabelBindingTests` 8, `BindingPassTests` 10, each with 0 failed. Both seam greps find no line. The only changed project file is the Framework test project. No diff in `scripts/`, `samples/`, `src/BrutalUi.Framework/State/`, `src/BrutalUi.Core/Elements/`, or `IBindingSource.cs`. Each round-trip test asserts one delegate call and one new version.
- Demo: PASS (`WidgetBindingTests` 10 of 10).
- Note: phase 1 uses an `AttachTo` method on the nested `NumberText` target, and phase 2 uses generic holder classes for the static handler. Both add no delegate per bind. The cross-phase review can align the two forms.
- Inbox: 0 lines.

### 2026-10-29 (phase 3, tasks 3.1 to 3.6)
- Done: `Element.ClearBindings` (3.1), the `_binding` guard in `UiContext` (3.2), `BindingLifecycleTests.cs` with 8 tests (3.3), 4 tests in `LabelBindingTests.cs` (3.4), the runs (3.5).
- The leave rule (Decision 6): an element outside the roots of a context applies nothing, because the binding pass walks from the roots only. The element keeps its bindings, and the first `Update` after a join applies a new version only. `ClearBindings` drops the bindings for good: it writes `default` into each used slot, sets the count to 0, keeps the array, and leaves `HasBoundSubtree` to the next pass. A store holds no reference to an element, so a dropped element needs no unsubscribe. `ElementList.cs` is unchanged.
- The error rule (Decision 7): the pass writes `AppliedVersion` before the handler call. An exception leaves `Update` before the style pass. That binding keeps the new version, and the bindings after it apply at the next `Update`. A handler that calls `Update` gets `InvalidOperationException` ("A binding handler must not call Update."). `UpdateBindings` sets `_binding` in a `try` and clears it in a `finally`. `Update` checks the flag first, before `DrainEvents`, so a nested call changes no state.
- Decisions past the brief:
  - The check of `_binding` sits at the start of `Update`, before the drain. `DrainEvents` keeps its own `_draining` guard unchanged.
  - `ClearBindings` clears with `Array.Clear(_bindings, 0, _bindingCount)`.
  - `ClearBindingsDropsEachBindingAndItsReferences` takes the `Bindings` span before the clear. The span is a view of the array, so it shows the cleared slots.
  - `tasks.md` is outside the file list of the brief, so the parent marks the tasks.
- Results:
  - `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
  - The solution: total 2620, succeeded 2618, skipped 2, failed 0.
  - `BindingLifecycleTests` 8 of 8, `BindingPassTests` 10 of 10, `LabelBindingTests` 12 of 12, `RoutingGateTests` 8 of 8, each with 0 failed.
  - `AStoreKeepsNoReferenceToADroppedLabel` with `--filter-method`: 5 runs, 5 passes.

### 2026-10-29 08:03 (phase 3 close)
- Phase 3 done (game-dev, STANDARD, one group). The parent marked tasks 3.1 to 3.6 in `tasks.md`.
- Verifier (`phase-3-verify.md`): PASS. Build 0 warnings, 0 errors. Solution 2620 (2618 passed, 2 skipped, 0 failed). `BindingPassTests` 10, `RoutingGateTests` 8 (no edit), each with 0 failed. The GC test passes in five of five runs. The order `DrainEvents` (681), `UpdateBindings` (684), `UpdateStyles` (689) holds. Both seam greps find no line. No diff in `scripts/`, `samples/`, `src/BrutalUi.Framework/State/`, `src/BrutalUi.Core/Elements/`, `IBindingSource.cs`, or `ElementList.cs`.
- Demo: PASS (`BindingLifecycleTests` 8 of 8, `LabelBindingTests` 12 of 12).
- Inbox: 0 lines.

### 2026-10-29 (phase 4, tasks 4.1 to 4.4) - ESCALATION: the gate measures bytes in a Debug build (RESOLVED by phase 4 fix 1, below)
- Done: 4.1 (three linked files), 4.2 (`BoundTree.cs`), 4.3 (`BoundTreeAllocationTests.cs`, 4 tests). Open: 4.4 (see below), and 4.5 to 4.8 (not in this group).
- Linked files (task 4.1): `ThousandElementTree.cs`, `ThousandWidgetTree.cs`, `TextMeasure.cs`. No extra file was needed. `ThousandWidgetTree` does not expose `StatRow` and `StatName`, so `BoundTree.Build` finds the panel named `stats` in `ThousandWidgetTree.All` before the swap, and the name leaf by the name `stat-name`.
- Decisions past the brief: `HudState` is `internal`, not `private`, because `Next` and `Store` have an `internal` signature (a private type causes error CS0050). `Next` is `static`, and the two names are `static readonly` strings from `new string(...)`. `Counter` is a small class in `BoundTree.cs`. The tests build the pointer events by hand, because the event factories of the Core test project are internal.
- **Finding (Debug build, 24 bytes for each number binding and each apply).** In the Debug configuration, 3 of the 4 tests fail with the same value: `Actual: 93600` bytes for 100 iterations = 936 bytes for each frame = 39 x 24 bytes. The 39 are the number labels (38 `int` labels and the `float` label). The `Name` label, the toggle, the slider, and the field allocate nothing. The failing tests are `UpdateAndDrawWithAChangingStoreAllocateNothing`, `AChangeOfOneSliceAllocatesNothing`, and `AUserFlipWithAWriteBackAllocatesNothing`. `TheMeasuredBodyDoesRealWork` passes.
- The cause (checked with a throw-away test, now deleted): a single `Label` bound to an `int` allocates 24 bytes for each apply, with `null` or `"D5"` or `"F1"` formats and with `null` or invariant providers. The same scene in `-c Release` allocates 0 bytes (4 of 4 pass). `DOTNET_TieredCompilation=0` and `DOTNET_TC_QuickJit=0` change nothing in Debug, because a Debug assembly has `DebuggableAttribute(DisableOptimizations)`, which keeps the JIT in minopts. The probable line is the check `if (value is null)` in `NumberText<T, TValue>.Apply` (`src/BrutalUi.Framework/Binding/LabelBindings.cs`, line 175). The C# compiler emits `box TValue` for it. The optimizing JIT removes the box, and the minopts JIT keeps it. A boxed `int` or `float` is 24 bytes. This is not yet proven by an edit of `src/`, because the brief forbids it.
- Proposed fix for the advanced agent (not applied): remove the box in all configurations, for example `if (!typeof(TValue).IsValueType && value is null)` is not enough in minopts. A safe form is a static readonly bool `s_isReferenceType = !typeof(TValue).IsValueType` plus `if (s_isReferenceType && ((object?)value) is null)`, or `EqualityComparer<TValue>.Default.Equals(value, default)`. Test the form in the Debug build. The same check may exist in other generic code of the Framework layer.
- The plants (task 4.4) in the Debug build give a non-zero exit code, but the gate is already red, so the result shows nothing. I ran both plants in `-c Release`, where the gate is green:
  - Plant 1 (`GC.KeepAlive(new object());` first line of the slot loop in `BindingPass.Visit`): `dotnet test --solution brutal-ui.sln -c Release` gives `exit=2`. The three allocation gates fail with `Actual: 103200` bytes (100 x 43 x 24: 40 labels + 3 widgets). The 4th test passes. Restore: `cmp` identical (3511 bytes).
  - Plant 2 (`label.SetText(value.ToString());` in place of the `TryFormat` loop of `NumberText.Apply`): Release gives `exit=2`. The three allocation gates fail with `Actual: 64000` bytes, and `TheMeasuredBodyDoesRealWork` fails too (the `ToString` text of the volume is `11`, not `11.0`). Restore: `cmp` identical (9609 bytes).
  - After both restores, `git diff --stat -- src/` is the same as before (`Element.cs` 152, `UiContext.cs` 52). Debug build: `0 Warning(s)`, `0 Error(s)`. Debug solution: total 2624, succeeded 2619, skipped 2, failed 3 (the three gates). Release solution: total 2624, succeeded 2622, skipped 2, failed 0.
- Open for the parent: fix the boxing in `LabelBindings.cs` (an `src/` change outside this group), then run the Debug solution and expect 0 failed. Then redo the plants in Debug if the acceptance needs the default configuration.

### 2026-10-29 (phase 4 fix 1, ADVANCED) - the ESCALATION above is resolved
- **The cause (proven).** `if (value is null)` in `NumberText<T, TValue>.Apply` boxes the slice in a Debug build. The proof is a JIT disassembly: `DOTNET_JitDisasm='*NumberText*:Apply'` with `DOTNET_JitStdOutFile`, on the run of `BoundTreeAllocationTests` in Debug. The listing of `NumberText<HudState, int>.Apply` says `(MinOpts)`. Right after the `Func.Invoke` call it has `call CORINFO_HELP_NEWSFAST`, a store of the `int` at `[rax+0x08]`, and `cmp gword ptr [rbp-0x70], 0`. That is the box of the null check: one object of 24 bytes for each apply. The listing of the `float` handler has the same block. The pass, the store, `SetText`, and the draw are not the cause.
- **The fix** (`src/BrutalUi.Framework/Binding/LabelBindings.cs`). A new field with a remark that states why the guard exists, and the new check:
  - `private static readonly bool s_isReferenceType = !typeof(TValue).IsValueType;`
  - `if (s_isReferenceType && value is null)`
  - After the fix, the MinOpts listing reads the byte of the field (`movzx rax, byte ptr [...]`, `test eax, eax`, `je`) and jumps over the `NEWSFAST` block. The box stays in the code, but the runtime never runs it for a struct slice. The null rule of phase 1 holds: a throw-away probe (now deleted) bound a label to a reference `IUtf8SpanFormattable` slice and read `R`, then an empty text for `null`, then `R` again (1 of 1 passed).
- **The other handlers.** A search of `src/BrutalUi.Framework/Binding/` for `is null`, `is not null`, `== null`, `!= null`, `(object)`, `Equals`, and `ToString(` finds only the line of the fix. `StringHandler<T>` (`LabelBindings.cs`) reads a `string?` and calls `SetText(string?)`: no generic value, no box. `CheckedHandler<T>`, `ValueHandler<T>`, and `TextHandler<T>` (`WidgetBindings.cs`) read a `bool`, a `float`, and a `string?`: no generic value check. `StoreSlice<T, TValue>.Read` (`StoreSlice.cs`) calls the selector only. No change in `WidgetBindings.cs` or `StoreSlice.cs`.
- **The test step.** `ASetOfANewScoreChangesTheTextAtTheNextUpdate` (`LabelBindingTests.cs`) now also sets the score 0, runs one `Update`, and reads `"0"u8`. It guards the null rule against a fix that treats `default` as `null`. The class keeps 12 tests.
- **Results in Debug (the default configuration).**
  - `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
  - The solution: total 2624, succeeded 2622, skipped 2, failed 0.
  - `BoundTreeAllocationTests`: total 4, failed 0. `LabelBindingTests`: total 12, failed 0.
  - A control run in `-c Release`: build `0 Warning(s)`, `0 Error(s)`. The Framework tests: total 49, failed 0.
- **The plants in Debug (task 4.4).** Each file was copied to the scratchpad first (`BindingPass.cs.fix1`, `LabelBindings.cs.fix1`).
  - Plant 1 (`GC.KeepAlive(new object());` as the first line of the slot loop in `BindingPass.Visit`): `exit=2`. The three allocation gates fail with `Actual: 103200` bytes, the same as Release. `TheMeasuredBodyDoesRealWork` passes. The solution: total 2624, failed 3. Restore: `cmp` identical (3511 bytes). The file is also identical to the group 1 backup `BindingPass.cs.orig`.
  - Plant 2 (`label.SetText(value.ToString());` in place of the `TryFormat` loop and its `SetText`, after the fix): `exit=2`. The three allocation gates fail with `Actual: 64000` bytes, and `TheMeasuredBodyDoesRealWork` fails (`Expected: True`, `Actual: False`), the same as Release. `LabelBindingTests` also fails 3 tests (`ALongTextGrowsTheBuffer`, `AFormatStringFormatsTheNumber`, `AProviderFormatsTheNumber`). The solution: total 2624, failed 7. Restore: `cmp` identical (10472 bytes, the size after the fix).
  - After both restores, `git diff --stat -- src/` is the same as before the plants. The Debug solution gives total 2624, failed 0.
- No change to a `.csproj`, `scripts/`, `samples/`, `src/BrutalUi.Core/`, `src/BrutalUi.Framework/State/`, `BoundTree.cs`, or `BoundTreeAllocationTests.cs`. The test itself has no fault.

### 2026-10-29 (phase 4 group 2, tasks 4.5 to 4.8)
- Done: 4.5 (section 4 of `docs/technical-design.md`: the diagram line, frame order item 2, and the paragraph "The binding pass at the seam"), 4.6 (the binding rules in section 10, and two bullets in section 14: the binding tests and "A fifteenth allocation gate"), 4.7 (one line in `CHANGELOG.md`), 4.8 (the runs below).
- The ordinal: section 14 held gates up to "A fourteenth allocation gate" (store-and-subscription) and no sibling gate in between. The new gate is the fifteenth.
- Results:
  - `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 17 s`, `exit=0`.
  - `grep -n 'dotnet test --solution brutal-ui.sln' scripts/test-linux.sh scripts/test-windows.ps1` gives 4 lines, not 2: a comment line and the run line in each script (`test-linux.sh` lines 13 and 148, `test-windows.ps1` lines 15 and 278). Each script runs the gate with no change. `git status` shows no change in `scripts/`.
  - The lint (`ste-lint.py --fail-over 2.5`): `technical-design.md` 1.53, `CHANGELOG.md` 1.71, `context.md` 1.57.
- Decisions past the brief:
  - Frame order item 2 lost the text "pending mode changes". A new sentence says that the mode diff joins the order when the panel and mode features land (section 10). The order of the mode diff against the binding pass stays open for panels-and-modes.
  - The first sentence of the "Data binding" text said that a binding is a struct "in a pooled list". The code uses a plain array that doubles, so the text now says "a list on the element".
  - The binding rules also state two facts of the code that the brief did not list: the doubling list (null, then 2 slots) and the `static readonly` flag that guards the null check of a struct slice (Debug box, 24 bytes).
  - The gate bullet states the Debug fault that fix 1 found, and that both plants ran in Debug.
  - The plants were not run again. The bullet uses the numbers of the phase 4 fix 1 notes (103200 and 64000 bytes).
- No change in `src/`, `tests/`, `scripts/`, or `samples/`.

### 2026-10-29 08:23 (phase 4 close)
- Phase 4 done. Group A (tasks 4.1 to 4.3, game-dev, STANDARD) built the gate and found 936 bytes per frame in Debug (39 boxes of 24 bytes), which the Risks table does not explain. The parent escalated per the plan: fix 1 (`phase-4-fix1.md`, game-dev-advanced, ADVANCED) proved the box of `value is null` in `NumberText.Apply` with a MinOpts disassembly, added the `s_isReferenceType` guard, added a score-0 step to an existing label test, and ran task 4.4 in Debug. Group B (tasks 4.5 to 4.8, game-dev, STANDARD) wrote the docs. The parent marked tasks 4.1 to 4.8 in `tasks.md`.
- Verifier (`phase-4-verify.md`): PASS. Build 0 warnings, 0 errors. Solution 2624 (2622 passed, 2 skipped, 0 failed). `BindingPassTests` 10, `BindingLifecycleTests` 8, `LabelBindingTests` 12, `WidgetBindingTests` 10, `BoundTreeAllocationTests` 4, each with 0 failed. The order `DrainEvents` (681), `UpdateBindings` (684), `UpdateStyles` (689) holds. The seam, Q4, and plant greps find no line. The only changed project file is the Framework test project. `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 18 s`, `exit=0`. Lint: `technical-design.md` 1.53, `CHANGELOG.md` 1.71.
- Demo: PASS.
- Inbox: 0 lines.

### 2026-10-29 08:50 (the cross-phase review)
- The review (game-dev-advanced, `review.md`) wrote `code-review.md`: 5 findings, 0 HIGH, 1 MEDIUM, 4 LOW. Its probe tests P1 to P5 show that each legal attach path keeps a bound element reachable. Rules 1, 2, 3, 4, 6, 8, 9, 11, 12, and 13 have no finding.
- Review group 1 (`review-g1.md`, CR-1, MEDIUM): step 2 of `TheMeasuredBodyDoesRealWork` reads the round and the version before `LivesFrame`. A plant of an empty `LivesFrame` now fails the test.
- Review group 2 (`review-g2.md`, CR-2 and follow-up 1): the char round trip types in the middle of the text (`axyb`). A caret-reset plant now fails it. The class remarks of `WidgetBindings` state that a user change that the game does not write back is lost at the next store change (follow-up 1, answer (a)).
- Review group 3 (`review-g3.md`, CR-3): new steps in three existing label tests pin the 4096-byte limit, the `null` reference slice (the guard of fix 1), and the current culture for a `null` provider. Two plants each fail a changed test. The count of `LabelBindingTests` stays 12.
- Review group 4 (`review-g4.md`, CR-4): the remarks of `UpdateBindings` and `BindingHandler` state what the code does. Doc comments only.
- Review group 5 (`review-g5.md`, CR-5): the growth remark of the scratch in `LabelBindings.cs` states one array for each doubling. Doc comments only. The rationale of Decision 5 in `implementation.md` still states "the scratch grows one time only". The called mode keeps the plan, so this note records the correct rule: the scratch doubles on each `false` of `TryFormat`, and a long jump allocates one array for each doubling in that update.
- Follow-up 2 (a guard for the forbidden acts of a handler) and follow-up 3 (how xml-markup reaches the bind methods) take the default answers: no code change.
- The review verifier (`review-verify.md`) round 1 returned FAIL on two greps that matched base text (a brief scope fault, no code fault). The parent narrowed the two greps, and round 2 returns PASS: build 0 warnings, 0 errors, solution 2624 (2622 passed, 2 skipped, 0 failed), each class count of the plan, each grep empty, CR-1 to CR-5 checked, `scripts/test-linux.sh` PASS with `exit=0`, and the lint of `code-review.md` 1.60.
- Inbox: 1 line (the round 1 verifier FAIL), done.

### 2026-10-29 08:58 (/update-feature)
- `/update-feature`: 30 of 30 tasks done. `implementation.md` status is Ready for Review. Phase 1 ran on the advanced agent, as the plan states. No advanced agent was missing, so no fallback.
- The solution-architect wrote the draft plan of `state-and-commands/commands-and-events` (5 phases, 27 tasks, all STANDARD). Its Notes hold four open scope questions (the event handler shape, the unknown-name report, the phase 1 demo, a second register of the same name). It found no mosaic-ui source in the repository or on the web.
- Inbox bug: Verifier FAIL: review-verify greps `plant2_grep` and `cr4a_grep` print lines (`TextField.cs:423` remark, `UiContext.cs:507` and `:523` remarks of `UpdateStyles` and `UpdateTransitions`) (done: review-verify.md, fix round 1. The fault was in the brief. Both greps matched base text. Round 2 passes.)
- No open inbox line, so no bug file to promote.
- Next: a person tests the lines of `## Needs Human Verification`. `/autorun-epic` commits the work.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
