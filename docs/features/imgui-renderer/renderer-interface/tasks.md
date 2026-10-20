# imgui-renderer/renderer-interface - Task Checklist

**Last Updated:** 2026-10-20 04:25
**Progress:** 32/32 tasks complete (100%)
**Advanced phases:** Phase 1 (ADVANCED, opus). Phases 2, 3, and 4 are STANDARD (sonnet). Phase 3 escalates to ADVANCED when the gate measures bytes above 0 and the cause is in `src/`.

Source paths: `Rendering/` means `src/BrutalUi.Core/Rendering/` and `Tree/` means `src/BrutalUi.Core/Tree/`. Test paths: `tests/` means `tests/BrutalUi.Core.Tests/`.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The build compiles `Shared/RecordingRenderer.cs` into the benchmark project too, so a solution build is required. The projects have no implicit usings, so each new file holds its own `using` lines. The xUnit analyzers are errors. Use `TestContext.Current.TestOutputHelper?.WriteLine(...)` to print.

---

## Phase 1: The Walk, the Rect, and the Clip (11/11 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads 7 call lines: Begin, the root Rect, PushClip 2,2,196,96, two child Rects, PopClip, and End.
**Needs:** none

Group A (the contract, 4 files):

- [x] ✅ **1.1 Write `Rendering/Rgba8.cs`**
  - Description: per the value type table of `implementation.md`. The summary states the byte order and that the memory layout matches `ImU32` (`IM_COL32`) on a little-endian CPU (A5). Doc comments name each byte and the default.
  - File(s): `src/BrutalUi.Core/Rendering/Rgba8.cs`
  - Estimate: 5 steps

- [x] ✅ **1.2 Write `Rendering/Corners.cs`**
  - Description: per the value type table (A6), in the doc style of `LengthEdges`. Mutable public fields `TopLeft`, `TopRight`, `BottomRight`, `BottomLeft`, a constructor in that order, and `static Corners All(float radius)`.
  - File(s): `src/BrutalUi.Core/Rendering/Corners.cs`
  - Estimate: 4 steps

- [x] ✅ **1.3 Write `Rendering/RenderHandles.cs`**
  - Description: `FontHandle` and `TextureHandle` per the table. The doc comment of each states the meaning of id 0.
  - File(s): `src/BrutalUi.Core/Rendering/RenderHandles.cs`
  - Estimate: 4 steps

- [x] ✅ **1.4 Write `Rendering/IUiRenderer.cs`**
  - Description: the eight members of section 8 of the technical design, unchanged, and the contract table of "The Interface" in the doc comments. The summary states: spans and structs only, viewport pixels, main thread, and no allocation in an implementation after warm-up.
  - File(s): `src/BrutalUi.Core/Rendering/IUiRenderer.cs`
  - Estimate: 5 steps

Group B (the walk and its tests, 6 files):

- [x] ✅ **1.5 Add `BackgroundColor` to `Tree/ComputedStyle.cs`**
  - Description: add the `BackgroundColor` field with its doc comment. Change the last paragraph of the class remarks: this epic adds the paint fields, code sets them inline, and the style-system epic writes them from the cascade later. After a write of a paint member, call `MarkDirty(DirtyFlags.Paint)`. The walk of this release draws the whole tree each frame, so the mark has no effect yet.
  - File(s): `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 4 steps

- [x] ✅ **1.6 Write `Rendering/PaintWalk.cs`**
  - Description: `PaintFrame`, `PaintWalk.InitialStackSize`, and `internal static void Walk(ReadOnlySpan<Element> roots, IUiRenderer renderer, ref PaintFrame[] stack)` per "The Draw Walk", steps 3, 4 (fill only), 5, 6, and 8, and the loop. Write the clip math as private static helpers (`PaddingBounds`, `Intersect`, `ToRect`). No LINQ, no lambda, no `foreach` over an interface. No recursion. Mind the gotcha: no `ref PaintFrame` after a stack growth.
  - File(s): `src/BrutalUi.Core/Rendering/PaintWalk.cs`
  - Estimate: 10 steps

- [x] ✅ **1.7 Add `Draw` to `Tree/UiContext.cs`**
  - Description: add `_paintStack`, `_drawing`, and `public void Draw(IUiRenderer renderer)` per the four steps. The doc comment states the order (`Update` first, then `Draw`), that `Draw` reads the tree only (A2), and that a renderer or a hook must not change the tree or call `Draw` during a draw. Update the class summary to name `Draw`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **1.8 Write `tests/Shared/RecordingRenderer.cs`**
  - Description: per "The Recording Renderer", with all eight members (phase 4 and label-and-image use `Text`, `Image`, and `MeasureText`). No xUnit type and no internal member of `BrutalUi.Core`.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs`
  - Estimate: 8 steps

- [x] ✅ **1.9 Write `tests/Rendering/RenderValueTests.cs`**
  - Description: `Unsafe.SizeOf<Rgba8>()` is 4. On a little-endian CPU, `Unsafe.As<Rgba8, uint>` of `(1, 2, 3, 4)` is `0x04030201`. `default(Rgba8)` equals `Rgba8.Transparent`. The constructor default alpha is 255. `ToString` of `(10, 20, 30, 255)` is `#0A141EFF`. `Corners.All(4)` sets four fields. The handle equality and the id 0 meaning.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`
  - Estimate: 5 steps

- [x] ✅ **1.10 Write `tests/Rendering/PaintWalkTests.cs`**
  - Description: `DrawsThreeElementTreeWithOneClip` (the demo, A9, golden 7-line list, printed), `AddsTheParentPositionsOnTheWayDown`, `IntersectsANestedClipWithTheParentClip`, `PopsEachClipAfterTheSubtree`, `SkipsTheRectOfATransparentElementAndDrawsItsChildren`, `DrawsTheRootsInTheOrderOfAdd`, `DrawsOnlyBeginAndEndWithNoRoots`, `ThrowsForANullRenderer`, and `ThrowsForADrawDuringADraw`. Task 1.10 of `implementation.md` holds the tree of each test.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs`
  - Estimate: 10 steps

- [x] ✅ **1.11 Run the build, the tests, and the demo**
  - Description: record the test total of the solution before the first edit in `context.md`. Run the build and the tests. Run the demo command. When `--show-live-output on` is not valid, run the test program with `--help`, use the listed form, and record it. Also try `dotnet test --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on`, and record which form prints the list. Record the printed list in `context.md`.
  - File(s): `docs/features/imgui-renderer/renderer-interface/context.md`
  - Estimate: 6 steps

---

## Phase 2: Hidden Elements, Opacity, Borders, and Corners (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsHiddenAndFadedTree' --show-live-output on` and reads a call list with no Rect for the hidden child, alpha `80` (128) in the fill of the half-opacity panel, and alpha `40` (64) in the fill of its half-opacity child.
**Needs:** Phase 1

- [x] ✅ **2.1 Add `BorderColor`, `CornerRadius`, and `Opacity` to `ComputedStyle.cs`**
  - Description: per "The Paint Members of `ComputedStyle`". The `Opacity` getter is `readonly`, because the engine and the walk read the style through `ref readonly`. The remarks of `Opacity` state why it is a property (Decision 3), the clamp, the `NaN` rule, and the hidden rule.
  - File(s): `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 5 steps

- [x] ✅ **2.2 Add `ScaleAlpha` to `Rgba8.cs`**
  - Description: `internal Rgba8 ScaleAlpha(float factor)` gives `new Rgba8(R, G, B, (byte)(A * factor + 0.5f))`. A factor of 1 keeps the alpha exactly.
  - File(s): `src/BrutalUi.Core/Rendering/Rgba8.cs`
  - Estimate: 3 steps

- [x] ✅ **2.3 Add the hidden, opacity, border, radius, and scroll rules to `PaintWalk.cs`**
  - Description: steps 1 and 2 of "Enter", the stroke and the border rule of step 4, the radius of step 5, and `Overflow.Scroll` in step 6. Keep the frame shape of phase 1.
  - File(s): `src/BrutalUi.Core/Rendering/PaintWalk.cs`
  - Estimate: 6 steps

- [x] ✅ **2.4 Add the value tests to `RenderValueTests.cs`**
  - Description: `default(ComputedStyle).Opacity` is 1, a new `Element` has opacity 1, a set of 0.3 reads back exactly 0.3, a set of 0 reads back 0. `ScaleAlpha(1)` keeps the alpha, `ScaleAlpha(0.5f)` of 255 gives 128, `ScaleAlpha(0)` gives 0.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`
  - Estimate: 4 steps

- [x] ✅ **2.5 Add the hidden tests to `PaintWalkTests.cs`**
  - Description: `SkipsADisplayNoneSubtreeAndDrawsTheNextSibling`, `SkipsAZeroOpacitySubtree`, `SkipsAHiddenRoot`, `TreatsANegativeOpacityAsHidden`, and `TreatsANaNOpacityAsOne`. Each checks that the hidden subtree pushes no clip.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs`
  - Estimate: 6 steps

- [x] ✅ **2.6 Add the opacity and border tests to `PaintWalkTests.cs`**
  - Description: `MultipliesOpacityDownTheSubtree` (0.5 and 0.5 give 64 for an alpha of 255, also on the border color), `DrawsABorderOnlyElement`, `UsesTheLeftBorderWidth` (left 3, top 1 give 3), `IgnoresAPercentBorderWidth`, `SkipsAnInvisibleBorder` (width 2 with a transparent color gives no Rect when the fill is transparent, and width 0 in the Rect when the fill is visible), `PassesTheCornerRadius`, and `ClipsAScrollContainer`.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs`
  - Estimate: 7 steps

- [x] ✅ **2.7 Add the dirty bit and deep tree tests to `PaintWalkTests.cs`**
  - Description: `DrawChangesNoDirtyBit` (A2), `DrawsAChainOf5000LevelsWithNoOverflow` (A8: no `Update`, expect 5000 Rect calls), and `KeepsTheOrderWhenTheStackGrows` (a chain of 100 levels as the first child and a leaf as the second child. The leaf Rect comes after the last chain Rect, and each clip pops).
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs`
  - Estimate: 6 steps

- [x] ✅ **2.8 Add the demo test `DrawsHiddenAndFadedTree` and run the checks**
  - Description: a root, a hidden child (`Display.None`) with its own child, a panel with opacity 0.5, and a child of the panel with opacity 0.5. Assert the golden list and print it. Run the build, the tests, and the demo. Record the list in `context.md`.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs`, `docs/features/imgui-renderer/renderer-interface/context.md`
  - Estimate: 6 steps

---

## Phase 3: The Draw Allocation Gate (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 2

- [x] ✅ **3.1 Set paint members in `Shared/ThousandElementTree.cs`**
  - Description: task 3.1 of `implementation.md` lists each member and each element. No `BorderWidth` outside the footer keys. Use named color constants. Update the remarks: the paint members change no layout.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`
  - Estimate: 8 steps

- [x] ✅ **3.2 Add the draw gate to `ThousandElementAllocationTests.cs`**
  - Description: the `Scene` gets `required RecordingRenderer Renderer`. Add `static void Frame(Scene)` (`Step`, then `Context.Draw(Renderer)`) and `RunFrames(Scene, int)`. Add `[Fact] DrawAllocatesNothingAfterWarmUp` per task 3.2 of `implementation.md`. `UpdateAllocatesNothingAfterWarmUp` does not change. Update the class summary and remarks.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.3 Extend `TheTreeHoldsTheRequiredMix`**
  - Description: count a visible background, a visible border, a clip, an opacity below 1, and a corner radius. Assert one or more of each. Keep each existing assert.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 3 steps

- [x] ✅ **3.4 Add `[Fact] TheDrawStepEmitsEachPaintedElement`**
  - Description: per task 3.4 of `implementation.md`. The visible frame holds 74 more Rect calls than the hidden frame. The `PushClip` and `PopClip` counts are equal.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 5 steps

- [x] ✅ **3.5 Run the build, the tests, the demo, and the Linux script**
  - Description: run the class 5 times and record the byte result and the run time in `context.md`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.
  - File(s): `docs/features/imgui-renderer/renderer-interface/context.md`
  - Estimate: 6 steps

- [x] ✅ **3.6 Prove that the gate blocks**
  - Description: copy `ThousandElementAllocationTests.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` after the `Draw` call in `Frame`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, run the tests again, and expect 0 failed. Record both exit codes in `context.md`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` (temporary edit, restored), `docs/features/imgui-renderer/renderer-interface/context.md`
  - Estimate: 5 steps

---

## Phase 4: The Paint Hook and the Record (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsContentBetweenTheBackgroundAndTheChildren' --show-live-output on` and reads, for the probe element, the Rect line, the PushClip line, a `Text` line, the Rect line of its child, and the PopClip line, in that order.
**Needs:** Phase 3

Group A (the hook, 3 files):

- [x] ✅ **4.1 Add the `PaintContent` hook to `Tree/Element.cs`**
  - Description: `internal virtual void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` with an empty body. The doc comment states when the walk calls it (after the Rect and the clip push, before the children), the units, that `opacity` is the product down the tree, and the rules: no tree change, no mark, no allocation, no `Draw` call.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 3 steps

- [x] ✅ **4.2 Call the hook in `PaintWalk.cs`**
  - Description: step 7 of "Enter".
  - File(s): `src/BrutalUi.Core/Rendering/PaintWalk.cs`
  - Estimate: 2 steps

- [x] ✅ **4.3 Write `tests/Rendering/PaintContentTests.cs`**
  - Description: a private `ContentProbe : Element` that overrides `PaintContent` and calls `renderer.Text` with `"hi"u8` and its color scaled by the opacity. Tests: `DrawsContentBetweenTheBackgroundAndTheChildren` (the demo, golden list, printed), `CallsTheHookWithTheViewportBorderBox`, `PassesTheOpacityProduct`, `CallsNoHookForAHiddenElement`, and `RecordsTheTextBytes`.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintContentTests.cs`
  - Estimate: 7 steps

Group B (the record, 4 files):

- [x] ✅ **4.4 Edit `docs/technical-design.md`**
  - Description: sections 4, 5, 7, 8, and 14 per task 4.4 of `implementation.md`. Section 8 gets a subsection "The draw walk".
  - File(s): `docs/technical-design.md`
  - Estimate: 7 steps

- [x] ✅ **4.5 Edit `CHANGELOG.md`**
  - Description: one line under `### Added`: the renderer interface, the value types, the paint members, `UiContext.Draw`, and the draw gate (imgui-renderer/renderer-interface). No benchmark number.
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] ✅ **4.6 Write or edit `docs/features/imgui-renderer/human-testing.md`**
  - Description: a `## renderer-interface` section in the form of `docs/features/core-tree/human-testing.md`, with the 4 phase demos and the Windows run of `scripts\test-windows.ps1`.
  - File(s): `docs/features/imgui-renderer/human-testing.md`
  - Estimate: 3 steps

- [x] ✅ **4.7 Run the build, the tests, the demo, the Linux script, and the prose lint**
  - Description: run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on `implementation.md`, and fix each hit. Record the results and the hand-offs of Notes in `context.md`.
  - File(s): `docs/features/imgui-renderer/renderer-interface/context.md`
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

### Completed This Session (2026-10-20)
- Phase 1, tasks 1.1 to 1.11: the contract, the walk, `UiContext.Draw`, the recording renderer, and 18 tests.
- Phase 2, tasks 2.1 to 2.8: the paint members, the hidden, opacity, border, radius, and scroll rules, and 20 tests.
- Phase 3, tasks 3.1 to 3.6: the draw gate on the 1000-element tree (0 bytes in 5 runs) and the proof that it blocks.
- Phase 4, tasks 4.1 to 4.7: the `PaintContent` hook, 5 tests, the technical design, `CHANGELOG.md`, and the human-testing lines.
- Cross-phase review: 11 findings. Review groups 1 to 4 fixed CR-1, CR-3 to CR-9. The orchestrator fixed CR-11. CR-2 goes to a user decision, and CR-10 goes to the label-and-image hand-off. Verifier `review-verify.md`: PASS (623 tests).
- The draft plan of imgui-draw-list (`docs/features/imgui-renderer/imgui-draw-list/implementation.md`).

### Discovered New Tasks
- none in this feature (CR-2 and the measure route wait for a user decision, see Open Questions of `context.md`)

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
