# imgui-renderer/label-and-image - Task Checklist

**Last Updated:** 2026-10-20 22:13
**Progress:** 42/42 tasks complete (100%)
**Advanced phases:** Phases 1 to 6 (ADVANCED, opus). Group B of phase 6 is plain documentation work.

Source paths: `Core/` means `src/BrutalUi.Core/`. Test paths: `tests/Core/` means `tests/BrutalUi.Core.Tests/`, and `tests/ImGui/` means `tests/BrutalUi.ImGui.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "The Public API", "The Measure Route", "The Label Measure Rule", "The Label Draw Rule", "The Image Rules", "The Content Insets", "The Locale Refresh", and "The Owned Text Buffer" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines, outside the namespace (`.editorconfig`). The xUnit analyzers are errors. Use `TestContext.Current.TestOutputHelper?.WriteLine(...)` to print. A demo uses `dotnet run --project <test project> -- --filter-method '*Name' --show-live-output on`. The `dotnet test --project` form prints no output line. The ImGui test project needs the alias `using Gui = Brutal.ImGuiApi.ImGui;`. In a class that implements `IUiRenderer`, the simple name `Image` binds to the method group, so write `new BrutalUi.Core.Image(...)`. `BrutalUi.ImGui.Tests` sees the public API of Core only.

---

## Phase 1: A One-Line Label Through the Measurer Slot (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAOneLineLabel' --show-live-output on` and reads `rect 0,0,40,16`, then the three lines `Begin 400x300 scale 1`, `Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, and `End`.
**Needs:** none

- [x] **1.1 Record the baseline and read the code**
  - Description: Record the test total of the solution before the first edit in `context.md`. Confirm that the imgui-draw-list work is on the branch (the files `ImGuiRenderer.cs` and `tests/BrutalUi.ImGui.Tests/` exist, and `IUiRenderer.Text` takes `wrapWidth`). Read `.editorconfig` for the naming rule of a static field. Read the leaf path of `LayoutEngine.LayoutInternal` and `PaintContentTests.cs` for the test pattern of the hook.
  - File(s): `context.md`
  - Estimate: 6 steps

- [x] **1.2 Add the create-info and the measurer slot to `UiContext`**
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `CreateInfo` with `required IUiRenderer Measurer`, the constructor `UiContext(in CreateInfo info)` (a null measurer throws `ArgumentNullException`), the read-only `Measurer` property, the thread-static field, and `internal static IUiRenderer? CurrentMeasurer`. Keep the parameterless constructor. The class remarks state the measurer rule and the shared font ids.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] **1.3 Set the measure scope in `Update`**
  - Description: Edit `Update` in `UiContext.cs`: save the scope, set it to the measurer of the context, run the layout loop in `try`, and restore the scope in `finally`. The clear pass stays after the `finally`. The doc comment states that a label measures during this call only.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] **1.4 Write `Label.cs` with the owned buffer**
  - Description: Write `src/BrutalUi.Core/Elements/Label.cs`: `public sealed class Label : Element` with `CreateInfo` (`required Rgba8 Color`, `Font`, `Size`, `string? Text`), the constructor, the owned buffer of "The Owned Text Buffer", the `Text` getter, and the getters of `Font`, `Size`, and `Color`. The constructor sets `Layout.Measure` to a `private static readonly MeasureCallback` field. Phase 4 adds the setters.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 6 steps

- [x] **1.5 Add the label measure callback**
  - Description: Add the measure callback to `Label.cs`: read `UiContext.CurrentMeasurer`. A null measurer throws `InvalidOperationException` with the text "A Label needs a UiContext with a measurer. Create the context with UiContext.CreateInfo." Call `MeasureText(Font, Size, Text, 0)` and return the result. Phase 2 adds the wrap rule.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 4 steps

- [x] **1.6 Add the label `PaintContent` override**
  - Description: Add the `PaintContent` override to `Label.cs` per "The Label Draw Rule" with `content = borderBox` and the wrap width `content.Z` (no snap yet). The class remarks state: a leaf only, the font id rule, and that phase 5 adds the padding.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 4 steps

- [x] **1.7 Write `LabelTests.cs`**
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`:
    - `DrawsAOneLineLabel` (the demo): a `RecordingRenderer` as the measurer and the renderer, a viewport of 400 by 300, a root `Element` with `AlignItems = Align.FlexStart`, and a child label (`Hello`, size 16, color `(255, 255, 255)`). `Update`, then `Draw`. Print `rect <x,y,w,h>` with the invariant culture, then `Describe()`. Assert both.
    - `MeasuresThroughTheContextMeasurer` (the count goes up during `Update`), `ASecondUpdateOfACleanTreeMeasuresNothing`, `ThrowsWithNoMeasurer` (the parameterless context), `ThrowsForANullMeasurer`, `TheScopeIsEmptyAfterUpdate` (`CurrentMeasurer` is null after `Update` and after a throw), and `TwoContextsUseTheirOwnMeasurer` (two recording renderers, one label each, and each count goes up for its own context only).
    - `ScalesTheColorByTheOpacity` (an opacity of 0.5 gives alpha 128), `DrawsNothingForEmptyText`, and `DrawsNothingForATransparentColor`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`
  - Estimate: 10 steps

- [x] **1.8 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 2: Word Wrap at the Available Width (5/5 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*WrapsALabelAtItsWidth' --show-live-output on` and reads `label rect 0,0,63,26`, then one command line of the background list whose glyph vertices lie inside `0,0-64,27`.
**Needs:** Phase 1

Group A (the rule, 3 files):

- [x] **2.1 Add the wrap rule to `RecordingRenderer`**
  - Description: Before the edit, search the tests for callers of `RecordingRenderer.MeasureText` with a wrap width above 0. Record the result in `context.md`. Then edit `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs`. Keep the rule for no wrap: `(bytes * size * 0.5, size)`. Add the wrap rule for a finite wrap width above 0:
    - Each byte is `size * 0.5` wide. Words split at the space byte.
    - A word goes on the current line when the line width plus one space plus the word fits at or below the wrap width. Otherwise it starts a new line.
    - A word that is wider than the wrap width starts a new line. It is cut into parts of `max(1, floor(wrap / (size * 0.5)))` bytes, one part on each line. The next word follows the last part when it fits.
    - The width is the widest line, and the height is the line count times the size. Empty text gives `(0, size)`.
    - Add `LastMeasureWrapWidth`: the wrap width of the last `MeasureText` call with a wrap width above 0, or 0 before such a call.
    - The file stays free of test framework types (the benchmark links it). The remarks state: use a size of 16, so a byte is 8 pixels wide and no float noise occurs.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs`, `context.md`
  - Estimate: 8 steps

- [x] **2.2 Add the snap rule and the measure rule to `Label.cs`**
  - Description: Edit `Label.cs`: add `internal static float SnapWrapWidth(float width)` and the measure rule of "The Label Measure Rule". The draw uses `SnapWrapWidth(content.Z)`. The remarks state the rule and the reason.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 5 steps

- [x] **2.3 Write `LabelWrapTests.cs`**
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs`, with `Hello brutal world` at size 16 (144 px with no wrap):
    - The recording rule: wrap 100 gives `(96, 32)`, wrap 200 gives `(144, 16)`, wrap 0 and `NaN` give the no-wrap result, and `brutalui` at wrap 40 gives `(40, 32)`.
    - `SnapWrapWidth`: 0, -1, `NaN`, and infinity give 0. `95.999` gives 96. `70.3` gives `70.3`.
    - `WrapsAtTheAtMostLimit`: a root of width 100 with `AlignItems = Align.FlexStart` gives the rect `0,0,96,32`, `LastMeasureWrapWidth` 100, and a `Text` wrap of 96.
    - `WrapsAtTheExactWidth`: the stretched label in a root of width 100 gives the rect `0,0,100,32`. `LastMeasureWrapWidth` equals the `Text` wrap width (100).
    - `KeepsTheAtMostContract`: in a root of width 200, the rect equals the rect with a `NaN` viewport width, and `LastMeasureWrapWidth` stays 0 (no wrapped measure).
    - `DoesNotWrapWithAnUndefinedWidth` (a viewport with a `NaN` width), `MeasuresATallerLabelInANarrowerBox` (width 60 gives 3 lines: `Hello`, `brutal`, and `world`), and `ReturnsTheContentSizeOnly` (a minimum height of 50 gives a rect height of 50).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs`
  - Estimate: 10 steps

Group B (the real measure, 1 file):

- [x] **2.4 Write `ImGuiLabelTests.cs`**
  - Description: Write `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`. The `ImGuiRenderer` of a `HeadlessImGui` harness (400 by 300) is the measurer and the renderer. Each test runs `Update` before `BeginFrame`, so the measure runs outside the frame:
    - `WrapsALabelAtItsWidth` (the demo): a root of width 70 with `AlignItems = Align.FlexStart` and a label `Hello, brutal-ui` at size 13. Print `label rect ...` and the dump. Assert the rect `0,0,63,26` and that each glyph vertex lies inside the rect grown by 1 pixel. When ImGui gives another size, record the real values in `context.md`, and change the expected values only when the vertex rule still holds.
    - `WrapsAStretchedLabelAtTheBoxWidth`: the same text stretched in a root of width 70 gives the rect `0,0,70,26`, and the glyphs lie inside it.
    - `KeepsOneLineWhenTheTextFits`: a root of width 200 gives the rect `0,0,112,13`.
    - `KeepsTheLinesForEachWidthFromFortyToTwoHundred`: for each whole width from 40 to 200 (161 widths), a label with `AlignItems = Align.FlexStart` in a root of that width has a rect height equal to the vertex height of the draw, rounded up to whole lines. Print the first width that fails, if any.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`
  - Estimate: 12 steps

- [x] **2.5 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed lines in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 3: Image (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAnImageAtItsTextureSize' --show-live-output on` and reads `Image 0,0,64,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0`, then `Image 0,57,100,50 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0` for the fit image in a 100 by 100 box.
**Needs:** Phase 2

- [x] **3.1 Write `ImageScaleMode.cs`**
  - Description: Write `src/BrutalUi.Core/Elements/ImageScaleMode.cs`: the enum with a doc comment for each value per "The Image Rules".
  - File(s): `src/BrutalUi.Core/Elements/ImageScaleMode.cs`
  - Estimate: 2 steps

- [x] **3.2 Write `Image.cs`**
  - Description: Write `src/BrutalUi.Core/Elements/Image.cs`: `CreateInfo`, the constructor (the defaults of "The Public API"), the four properties with the dirty rules of "The Public API", the natural size, the static measure callback, and the internal static `FitRect` and `CropUv`. The setter of `Texture` compares the id and the size, because handle equality uses the id only.
  - File(s): `src/BrutalUi.Core/Elements/Image.cs`
  - Estimate: 8 steps

- [x] **3.3 Add the image `PaintContent` override**
  - Description: Add the `PaintContent` override to `Image.cs` per "The Image Rules", with `content = borderBox` until phase 5.
  - File(s): `src/BrutalUi.Core/Elements/Image.cs`
  - Estimate: 4 steps

- [x] **3.4 Write `ImageTests.cs`**
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ImageTests.cs`:
    - `DrawsAnImageAtItsTextureSize` (the demo): a root column with `AlignItems = Align.FlexStart` in a 400 by 300 viewport. Child 1: an image of `new TextureHandle(7, new int2(64, 32))`. Child 2: the same texture with `ScaleToFit`, width 100, and height 100. Print `Describe()` and assert the two image lines.
    - `TakesTheSizeOfTheUvRegion` (UV `0,0,0.5,0.5` gives `32x16`), `ShrinksToAnAtMostLimit` (a root of width 40 gives a width of 40), `KeepsTheAtMostContract`, `TakesNoSpaceForTheNoneHandle`, `DrawsNothingForTheNoneHandle`, and `ScalesTheTintByTheOpacity`.
    - The dirty rules: a new id with the same size marks `Paint` only. A new size marks `Layout` and `Paint`. The same handle marks nothing. `Uv` marks `Layout` and `Paint`. `Tint` and `ScaleMode` mark `Paint`.
    - The pure rules: `FitRect` for a wide and a tall image, `CropUv` for a wide and a tall box, a flipped UV, and the fall-back for a size of 0.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ImageTests.cs`
  - Estimate: 10 steps

- [x] **3.5 Write `ImGuiImageElementTests.cs`**
  - Description: Write `tests/BrutalUi.ImGui.Tests/ImGuiImageElementTests.cs`: `DrawsTheImageElementWithItsTexture`. Register the reference 4660 at 64 by 32 with the texture service. Add an image element with the returned handle. The dump holds `tex id 4660` and the vertex bounds `0,0-64,32`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiImageElementTests.cs`
  - Estimate: 6 steps

- [x] **3.6 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed lines in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 4: Text Changes and the Locale (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ShowsTheNewLocaleAfterARefresh' --show-live-output on` and reads `rect 0,0,40,16` and `dirty Layout, Paint`, then `rect 0,0,128,16` and `Text 0,0 font 0 size 16 #FFFFFFFF wrap 128 "Bonjour le monde"`.
**Needs:** Phase 3

- [x] **4.1 Probe `Brutal.Localization` 2026.10.2**
  - Description: Probe `Brutal.Localization` 2026.10.2 in a console project in the session scratch folder, not in the repository. Record in `context.md`: (a) the constructor forms of `LString` and whether the `Regex` argument has a default, (b) whether two reads of `Localized` give the same reference and allocate 0 bytes, (c) whether `Localize` gives a new reference, (d) whether `RemoveLocalization` gives back the `Template` reference, and (e) whether `LocalizedStrings.SetLocalizedStringProvider` calls `Localize` on each instance. When (b) fails, stop and record it (Risk 5).
  - File(s): `context.md` (the probe lives in the session scratch folder)
  - Estimate: 10 steps

- [x] **4.2 Add the text, font, size, and color setters**
  - Description: Edit `Label.cs`: add `SetText(ReadOnlySpan<byte>)`, `SetText(string?)`, and the setters of `Font`, `Size`, and `Color` with the dirty rules of "The Public API". Each one compares first. `Size` compares with `float.Equals`, so `NaN` to `NaN` marks nothing.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 6 steps

- [x] **4.3 Add the `LString` members to `Label.cs`**
  - Description: Edit `Label.cs`: add `CreateInfo.LocalizedText`, `LocalizedText`, `SetText(LString?)`, and the internal method `RefreshLocalized()` per "The Locale Refresh". `SetText(LString)` with null clears the text. The constructor throws `ArgumentException` when `Text` and `LocalizedText` are both set.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 6 steps

- [x] **4.4 Add `UiContext.RefreshLocalizedText()`**
  - Description: Edit `UiContext.cs`: add `RefreshLocalizedText()`. It walks each root and its subtree, calls `RefreshLocalized()` on each label, and marks nothing else. The doc comment states when to call it and the allocation rule.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] **4.5 Write `LabelTextTests.cs`**
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`:
    - `ShowsTheNewLocaleAfterARefresh` (the demo): a label with an `LString` of the template `Hello` (the constructor form of task 4.1) in the tree of the phase 1 demo. `Update`, then print the rect. Call `Localize("Bonjour le monde")` and `RefreshLocalizedText()`. Print `dirty <flags>` (the self bits of the label). `Update`, `Draw`, then print the rect and the text line. Use an `LString` instance, not the global `LStrings`, so parallel tests share no state.
    - The span, the string, the font, the size, and the color: a change marks the right bits. The same value marks nothing. After `Update`, the rect and the dump show the new value.
    - `SetTextFromASpanClearsTheLocalizedText`, `RefreshMarksNothingWhenTheTextIsTheSame` (the same content with a new reference), `RefreshReachesADeepLabel`, `RefreshWorksWithNoMeasurer`, `ThrowsForTwoTexts`, and `RemoveLocalizationShowsTheTemplate`.
    - `GrowsTheBufferForALongText` (a text of 100 bytes, then of 10, then of 100: the text reads back each time).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`
  - Estimate: 12 steps

- [x] **4.6 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed lines in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 5: Padding and Border (the Content Insets) (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsTheLabelInsideItsPadding' --show-live-output on` and reads `rect 0,0,64,40` and `Text 12,12 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, then `Text 40,40 font 0 size 16 #FFFFFFFF wrap 320 "Hello"` for the root label with a padding of 10 %.
**Needs:** Phase 4

Group A (the layout pass, 4 files):

- [x] **5.1 Read the padding width of the engine**
  - Description: Read `CalculateLayoutImpl` and the measure path of a leaf in `src/BrutalUi.Core/Layout/LayoutEngine.cs`. Record in `context.md` which width the engine uses for the padding of a node (the expected answer is `ownerWidth`) and the order of the sum of the inner width. When the engine uses another width, use that width in task 5.3 and record it.
  - File(s): `context.md`
  - Estimate: 5 steps

- [x] **5.2 Add `ContentInsets` and `ContentBox` to `LayoutNode`**
  - Description: Edit `src/BrutalUi.Core/Tree/LayoutNode.cs`: add `internal float4 ContentInsets` and `internal readonly float4 ContentBox(in float4 borderBox)` per "The Content Insets". The doc comment states the write point and that a cache hit writes the value too.
  - File(s): `src/BrutalUi.Core/Tree/LayoutNode.cs`
  - Estimate: 4 steps

- [x] **5.3 Write the insets in the layout pass**
  - Description: Edit `LayoutEngine.cs`: in the `if (performLayout)` block of `LayoutInternal`, write `ContentInsets` from the style and the width of task 5.1. Add no other change. The write allocates nothing.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 5 steps

- [x] **5.4 Write `ContentInsetsTests.cs`**
  - Description: Write `tests/BrutalUi.Core.Tests/Layout/ContentInsetsTests.cs`: pixel padding and border on each edge, a percent padding of a child (against the inner width of the parent), a percent padding of a root (against the viewport width), a `NaN` viewport width, a change of the parent width with a clean child (the insets follow on the next `Update`), and a layout cache hit after a measurement with another owner width (the insets keep the value of the final layout).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/ContentInsetsTests.cs`
  - Estimate: 10 steps

- [x] **5.5 Compare the insets in `LayoutDifferentialTests`**
  - Description: Edit `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs`: compare `ContentInsets` of each element between the incremental and the fresh layout, in the same way as the rects. Run each layout fixture. No expected rect changes.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs`
  - Estimate: 5 steps

Group B (the elements, 4 files):

- [x] **5.6 Draw the label and the image in the content box**
  - Description: Edit `Label.cs` and `Image.cs`: `PaintContent` uses `Layout.ContentBox(borderBox)`. The label computes the wrap width as `rect.Z - (insets.Left + insets.Right)` through `ContentBox`, in the order of the engine, then applies `SnapWrapWidth`. Remove the phase 1 remark on the padding.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`, `src/BrutalUi.Core/Elements/Image.cs`
  - Estimate: 5 steps

- [x] **5.7 Write `ContentBoxTests.cs`**
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ContentBoxTests.cs`:
    - `DrawsTheLabelInsideItsPadding` (the demo): context 1 is the phase 1 tree with a label padding of 10 px and a border of 2 px. Context 2 has a root label with a padding of 10 % in a 400 by 300 viewport. Print and assert the lines of the demo.
    - `KeepsTheSameWrapWidthWithAPercentPadding`: a stretched label with a padding of 7.3 % in a root of width 333. `LastMeasureWrapWidth` equals the `Text` wrap width.
    - `DrawsTheImageInsideItsBorder` and `FitsTheImageInsideItsPadding`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ContentBoxTests.cs`
  - Estimate: 8 steps

- [x] **5.8 Add the percent padding case to `ImGuiLabelTests.cs`**
  - Description: Add to `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`: `WrapsALabelWithAPercentPaddingAtItsWidth`. The text of phase 2 with a padding of 5 % in a root of width 150. The test computes the content box from the padding (7.5 px on each side). The glyph vertices lie inside that box grown by 1 pixel.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`
  - Estimate: 5 steps

- [x] **5.9 Run the build, the tests, the demo, and the facts**
  - Description: Run the build, the tests, and the demo. Run `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30` and record the new `bytes per element` value in `context.md` (the element grows by 16 bytes). Record the printed lines in `context.md`.
  - File(s): `context.md`
  - Estimate: 5 steps

---

## Phase 6: The Allocation Gate and the Record (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ElementAllocationTests'` and reads a total of 4 with `failed: 0`.
**Needs:** Phase 5

Group A (the gate, 2 files):

- [x] **6.1 Write `ElementAllocationTests.cs`**
  - Description: Write `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`, in the form of `ThousandElementAllocationTests`. The scene: a `RecordingRenderer` as the measurer and the renderer, a root column with 50 labels (padding 4, wrap in a width of 120) and 50 images, and one `LString` label. Run 3 warm-up frames. Then run 100 frames. In each frame, read `GC.GetAllocatedBytesForCurrentThread()` right before and right after the measured body, and add the difference.
    - `UpdateAndDrawAllocateNothingAfterWarmUp`: the body is `SetText` from one of two span buffers on 10 labels, a new tint on 10 images, `Update`, and `Draw`. Assert a sum of 0.
    - `SetTextFromAStringAllocatesNothingAfterWarmUp`: the body is `SetText` with one of two strings that the test creates before the loop, then `Update`. Assert 0.
    - `RefreshLocalizedTextAllocatesNothing`: the body is `RefreshLocalizedText()` with no locale change. Assert 0.
    - `TheMeasuredBodyDoesRealWork`: the measure count goes up in each frame of the first test, and the dump of one frame holds 51 `Text` lines and 50 `Image` lines.
  - File(s): `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`
  - Estimate: 10 steps

- [x] **6.2 Add the label allocation test to the ImGui project**
  - Description: Add `LabelUpdateAndDrawAllocateNothingAfterWarmUp` to `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`: the same body with `ImGuiRenderer` as the measurer and the renderer, `Update` outside the frame, and `Draw` between `BeginFrame` and `EndFrame`. Assert 0.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`
  - Estimate: 6 steps

- [x] **6.3 Prove that the gate blocks**
  - Description: Copy `src/BrutalUi.Core/Elements/Label.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of the measure callback. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Run the tests again and expect 0 failed. Record both exit codes in `context.md`.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs` (temporary, restored), `context.md`
  - Estimate: 6 steps

- [x] **6.4 Run the demo 5 times and the Linux script**
  - Description: Run the demo class 5 times and record the result in `context.md`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.
  - File(s): `context.md`
  - Estimate: 4 steps

Group B (the record, 4 files):

- [x] **6.5 Update `docs/technical-design.md`**
  - Description: Edit `docs/technical-design.md`. Section 5: the API of `Label` and `Image`, the create-info of `UiContext`, and `RefreshLocalizedText`. Section 6, "The measure callback": the measure scope of `Update`, the `ContentInsets` write point, and the 16 bytes for each element. Section 8: the measurer slot, the label measure rule, the draw rule, the snap rule, and the image scale modes. Section 14: add the element tests, the ImGui label tests, and the third allocation gate to "Test types".
  - File(s): `docs/technical-design.md`
  - Estimate: 8 steps

- [x] **6.6 Update `CHANGELOG.md`**
  - Description: Edit `CHANGELOG.md`: add one line under `### Added`: `Label`, `Image`, `ImageScaleMode`, the measurer slot of `UiContext`, `RefreshLocalizedText`, and the content insets of the layout pass (imgui-renderer/label-and-image).
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] **6.7 Add the label-and-image section to `human-testing.md`**
  - Description: Edit `docs/features/imgui-renderer/human-testing.md`: add a `## label-and-image` section in the form of the `## imgui-draw-list` section, with the 6 phase demos and the Windows run of `scripts\test-windows.ps1`.
  - File(s): `docs/features/imgui-renderer/human-testing.md`
  - Estimate: 3 steps

- [x] **6.8 Lint the docs and write the hand-offs**
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.
  - File(s): `context.md`, each changed doc
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- none

**Fixed Bugs:**
- none

---

## Technical Debt

- [ ] 💳 **The whole-pixel `MeasureText` contract** - Priority: Low
  - Description: a fractional measurer can still wrap a widest line within 1/64 pixel above a whole number (CR-1 limit).
  - Reason: the fix changes a public contract. `ImGuiRenderer` rounds each width up, so it is safe today.
  - Effort: Low
- [ ] 💳 **The open points of the hand-offs** - Priority: Low
  - Description: the image aspect with a style width, the `ScaleToFit` pixel snap, a label with a content width of 0, an empty label height, the `Size` check, and `RefreshLocalizedText` for a later text element. See "Hand-offs" in `context.md`.
  - Reason: each one needs a decision on the public API.
  - Effort: Medium

### Completed This Session (2026-10-20)
- ✅ Phases 1 to 6 (42 tasks) and the cross-phase review (9 findings, 9 resolved).

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
