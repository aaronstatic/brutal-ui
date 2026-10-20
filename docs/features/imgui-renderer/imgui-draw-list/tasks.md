# imgui-renderer/imgui-draw-list - Task Checklist

**Last Updated:** 2026-10-20 18:13
**Progress:** 40/40 tasks complete (100%)
**Advanced phases:** Phases 1, 2, and 3 (ADVANCED, opus). Phases 4 and 5 are STANDARD (sonnet). Phase 5 escalates to ADVANCED when a gate measures bytes above 0 and the cause is in `src/` or in the binding.

Source paths: `ImGui/` means `src/BrutalUi.ImGui/`. Test paths: `tests/ImGui/` means `tests/BrutalUi.ImGui.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "The Public API", "The Mapping Table", "The Pure Rules", "The Font Service", "The Texture Service", and "The Headless Harness" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. The xUnit analyzers are errors. Use `TestContext.Current.TestOutputHelper?.WriteLine(...)` to print. A demo uses `dotnet run --project <test project> -- --filter-method '*Name' --show-live-output on`. In `namespace BrutalUi.ImGui;` the simple name `ImGui` binds to the namespace, so each file that calls the class uses the alias `using Gui = Brutal.ImGuiApi.ImGui;` outside the namespace.

---

## Phase 1: Rects and Clips on the Background List (11/11 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads two command lines: the root fill `0,0-200,100` in `#0A141EFF` under the clip `0,0,400,300`, then the two child fills in `#FF0000FF` and `#00FF00FF` under the clip `2,2,198,98`.
**Needs:** none

Group A (the harness, 5 files and `context.md`):

- [x] **1.1 Record the baseline and run the binding probe**
  - Description: Record the test total of the solution before the first edit in `context.md`. Then write a console project in the session scratch folder, not in the repository, with `PackageReference` items for `Ahwoo.BRUTAL.ImGui` and `Ahwoo.BRUTAL.ImGui.Backends` at the pinned versions. Print through reflection: (a) whether `Brutal.ImGuiApi.ImGui.PInvoke` and its members `ImDrawList_AddRectFilled`, `ImDrawList_AddRect`, `ImDrawList_AddText_1`, `ImDrawList_AddImage`, `ImDrawList_AddImageRounded`, `ImDrawList_PushClipRect`, `ImDrawList_PopClipRect`, and `ImFont_CalcTextSizeA` are public, (b) the raw pointer member of `ImDrawListPtr` and `ImFontPtr`, (c) the fields of `ImColor8`, `ImTextureRef`, and `ImTextureID`, (d) the signatures of `VulkanBackend.AddTexture` and `VulkanBackend.RemoveTexture`, (e) the status members of `ImTextureDataPtr` and the type of `ImGuiPlatformIOPtr.Textures`, (f) the names of the `ImDrawFlags` corner values, and (g) the members of `ImGuiViewportPtr` for the position. Then, in the same program, create a context with the flags of "The Headless Harness", call `CalcTextSizeA` on the default font before the first `NewFrame`, run one frame, call it again after `Render`, and measure `GC.GetAllocatedBytesForCurrentThread()` around 1000 `AddRectFilled` and 1000 `CalcTextSizeA` calls in the chosen form. Record each answer in `context.md`. Choose the call form: the raw `PInvoke` members with pointers when they are public (preferred, no `ImString` arena), else the extension methods. Record the choice. When a call before the first frame stops the process, record it, and follow the mitigation of Risk 2.
  - File(s): `context.md` (the probe lives in the session scratch folder)
  - Estimate: 15 steps

- [x] **1.2 Write the test project and add it to the solution**
  - Description: Write `tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj` in the form of `tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj`: `OutputType` Exe, `PackageReference` `xunit.v3`, `ProjectReference` `../../src/BrutalUi.ImGui/BrutalUi.ImGui.csproj`, and `<Compile Include="../BrutalUi.Core.Tests/Shared/*.cs" LinkBase="Shared" />` as in the benchmark project. Run `dotnet sln brutal-ui.sln add tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj --solution-folder tests`. Read `brutal-ui.sln` and confirm one `tests` folder that holds the new project.
  - File(s): `tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj`, `brutal-ui.sln`
  - Estimate: 6 steps

- [x] **1.3 Write `tests/ImGui/AssemblyInfo.cs`**
  - Description: `[assembly: CollectionBehavior(DisableTestParallelization = true)]`, with a comment that states the reason: ImGui has one current context for the process.
  - File(s): `tests/BrutalUi.ImGui.Tests/AssemblyInfo.cs`
  - Estimate: 2 steps

- [x] **1.4 Add `InternalsVisibleTo` to `ImGui/BrutalUi.ImGui.csproj`**
  - Description: add `<InternalsVisibleTo Include="BrutalUi.ImGui.Tests" />` in the form of `BrutalUi.Core.csproj`. Add no `PackageReference`.
  - File(s): `src/BrutalUi.ImGui/BrutalUi.ImGui.csproj`
  - Estimate: 2 steps

- [x] **1.5 Write `tests/ImGui/HeadlessImGui.cs`**
  - Description: per "The Headless Harness" of the plan (the constructor flags, `BeginFrame`, `EndFrame` with the texture status reset, `DescribeBackgroundList`, and `Dispose`). Use the alias `Gui`.
  - File(s): `tests/BrutalUi.ImGui.Tests/HeadlessImGui.cs`
  - Estimate: 8 steps

Group B (the renderer, 6 files):

- [x] **1.6 Write `ImGui/ImGuiMapping.cs`**
  - Description: `internal static class ImGuiMapping` with `ToImU32` and `HasArea` per "The Pure Rules".
  - File(s): `src/BrutalUi.ImGui/ImGuiMapping.cs`
  - Estimate: 3 steps

- [x] **1.7 Write the two service shells**
  - Description: `src/BrutalUi.ImGui/ImGuiFontService.cs` and `src/BrutalUi.ImGui/ImGuiTextureService.cs` as `public sealed` classes with a public constructor and the class summary of "The Font Service" and "The Texture Service". Phases 2 and 3 add the members.
  - File(s): `src/BrutalUi.ImGui/ImGuiFontService.cs`, `src/BrutalUi.ImGui/ImGuiTextureService.cs`
  - Estimate: 4 steps

- [x] **1.8 Write `ImGui/ImGuiRenderer.cs`**
  - Description: `CreateInfo`, the constructor (a null service throws `ArgumentNullException`), `Fonts`, `Textures`, the four frame fields, and `Begin`, `PushClip`, `PopClip`, `End`, and `Rect` (the fill only, `rounding` 0 and `RoundCornersNone`) per "The Mapping Table". `Image`, `Text`, and `MeasureText` throw `NotSupportedException` with the text "imgui-draw-list phase 2 or 3 adds this member." until those phases. The class summary states: the background list of the main viewport (Q2), the call window (`Draw` between `ImGui.NewFrame` and `ImGui.Render`), the main thread, one ImGui context, and the viewport rule of Decision 6. Write the class in its end-state shape, so phases 2 to 4 fill in members and not a new structure.
  - File(s): `src/BrutalUi.ImGui/ImGuiRenderer.cs`
  - Estimate: 10 steps

- [x] **1.9 Write `tests/ImGui/ImGuiMappingTests.cs`**
  - Description: `ToImU32` of `(1, 2, 3, 4)` is `0x04030201` (skip on a big-endian CPU, as `RenderValueTests` does). `HasArea` is false for a width or height of 0, a negative value, and `NaN` in each component.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiMappingTests.cs`
  - Estimate: 4 steps

- [x] **1.10 Write `tests/ImGui/ImGuiRendererRectTests.cs`**
  - Description: `DrawsThreeElementTreeWithOneClip` (the demo): the tree of the renderer-interface demo (`PaintWalkTests.DrawsThreeElementTreeWithOneClip`) in a 400 by 300 harness. `Update`, `BeginFrame`, `Draw`, `EndFrame`. Print the dump and assert it equals these two lines: `cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 0,0-200,100 colors #0A141EFF` and `cmd 1 clip 2,2,198,98 tex atlas elems 12 verts 12,12-312,52 colors #FF0000FF #00FF00FF`. When ImGui splits or merges the commands in another way, record the real dump in `context.md`. Change the expected lines only when each fill, color, and clip value still matches. Also: `ThrowsForADrawCallBeforeBegin`, `ThrowsForAPopClipWithNoPush`, `ThrowsForANullService`, `EndPopsAnOpenClip` (`PushClip` with no `PopClip`, then `End`. A later `Begin` and `Rect` in the same ImGui frame draw under the viewport clip), `BeginClearsTheClipsOfAnAbortedFrame` (`Begin`, `PushClip`, no `End`, then `Begin` and `Rect` in the same ImGui frame. The rect draws under the viewport clip), `SkipsAnEmptyRect` (width 0, height 0, `NaN`), and `SkipsATransparentFill`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs`
  - Estimate: 10 steps

- [x] **1.11 Run the build, the tests, and the demo**
  - Description: record the printed dump and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 3 steps

---

## Phase 2: Text and Measure (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*MeasuresAndDrawsText' --show-live-output on` and reads three measure lines for `Hello, brutal-ui` (13 px with height 13, 26 px with height 26, and 13 px at a wrap width of 70 with height 26), then one command line whose glyph vertices lie inside the measured box.
**Needs:** Phase 1

- [x] **2.1 Fill `ImGui/ImGuiFontService.cs`**
  - Description: the table, `Add`, and `GetFont` per "The Font Service". The context check throws `InvalidOperationException` when `Gui.GetCurrentContext()` has no value.
  - File(s): `src/BrutalUi.ImGui/ImGuiFontService.cs`
  - Estimate: 6 steps

- [x] **2.2 Add the text rules to `ImGui/ImGuiMapping.cs`**
  - Description: add `FontSize`, `WrapWidth`, and `RoundUpWidth` per "The Pure Rules".
  - File(s): `src/BrutalUi.ImGui/ImGuiMapping.cs`
  - Estimate: 3 steps

- [x] **2.3 Add `Text` and `MeasureText`, and the `wrapWidth` parameter of `IUiRenderer.Text`**
  - Description: Edit `ImGuiRenderer.cs`: `Text` and `MeasureText` per "The Mapping Table", in the call form of task 1.1. The empty-span check comes before `fixed` (the gotcha). The `MeasureText` remarks state each rule of the table row: the frame independence, the width rounding, the height, the break rules, the empty text, and the default size. The user picked option (a) of open scope question 3. Add a `float wrapWidth` parameter after `utf8` to `IUiRenderer.Text` in `BrutalUi.Core`, with the same wrap rule as `MeasureText`. Update `RecordingRenderer` (record the wrap width), each caller in `BrutalUi.Core.Tests`, and the `IUiRenderer` doc comment and section 8 of `docs/technical-design.md`. `ImGuiRenderer.Text` passes the wrap width to the ImGui text draw.
  - File(s): `src/BrutalUi.ImGui/ImGuiRenderer.cs`, `src/BrutalUi.Core/Rendering/IUiRenderer.cs`, `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs`, the callers of `Text` in `tests/BrutalUi.Core.Tests/`, `docs/technical-design.md`
  - Estimate: 12 steps

- [x] **2.4 Add the text rule tests to `tests/ImGui/ImGuiMappingTests.cs`**
  - Description: `FontSize` of 0, -1, `NaN`, and positive infinity gives the default, and 20 gives 20. `WrapWidth` of 0, -5, and `NaN` gives 0, and 70 gives 70. `RoundUpWidth` of 112 gives 112, and of 112.2 gives 113.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiMappingTests.cs`
  - Estimate: 3 steps

- [x] **2.5 Write `tests/ImGui/ImGuiFontServiceTests.cs`**
  - Description: `ReturnsTheDefaultFontForIdZero` (an empty atlas gets one default font, and a second call gives the same font), `PrefersTheIoDefaultFont`, `GivesIdsFromOne` (two `io.Fonts.AddFontDefault()` fonts give ids 1 and 2, and `GetFont` returns each), `ThrowsForAnUnknownId` (3 and -1), `ThrowsForANullFont`, and `ThrowsWithNoContext`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiFontServiceTests.cs`
  - Estimate: 6 steps

- [x] **2.6 Write the measure tests in `tests/ImGui/ImGuiRendererTextTests.cs`**
  - Description: `MeasuresOneLineWithTheSizeAsHeight`, `DoublesTheHeightForTwiceTheSize`, `WrapsAtANarrowWidth` (two lines at 70), `TreatsAZeroNegativeOrNaNWrapAsNoWrap` (equal to the no-wrap result), `CutsAWordThatIsWiderThanTheWrapWidth` (`brutal-ui` at a wrap width of 40 gives more than one line and a width at or below 40. When ImGui gives another result, record it in `context.md` and assert the real rule), `MeasuresEmptyTextAsZeroWidthAndOneLine`, `UsesTheFontSizeForASizeOfZero`, `MeasuresTheSameBeforeInsideAndBetweenFrames` (before the first `NewFrame`, inside a frame, and after `Render`, unless task 1.1 found a limit, see Risk 2), `MeasureDrawsNothing` (the vertex count of the list does not change), `MeasuresWithASecondFont`, and `ThrowsForAnUnknownFont`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererTextTests.cs`
  - Estimate: 10 steps

- [x] **2.7 Add the draw tests to `tests/ImGui/ImGuiRendererTextTests.cs`**
  - Description: `DrawsGlyphsInsideTheMeasuredBox` (each vertex lies inside the box from `pos` to `pos + MeasureText(...)`, grown by 1 pixel), `DrawsTheTextColor`, `DrawsNothingForEmptyText`, `DrawsNothingForAZeroAlpha`, and `ClipsTextUnderAPushedClip` (the text command carries the clip).
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererTextTests.cs`
  - Estimate: 6 steps

- [x] **2.8 Add the demo test `MeasuresAndDrawsText`**
  - Description: print the three measure results as `measure <size> wrap <w> -> <width>x<height>` and the dump of a frame that draws the 13 px text at (10, 10). Assert the heights 13, 26, and 26, the larger width at 26 px, and the vertex bounds inside the measured box. The wrap width 70 puts `Hello,` on line 1 and `brutal-ui` on line 2, because `brutal-ui` is one word (`-` is no break point) and fits in 70.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererTextTests.cs`
  - Estimate: 4 steps

- [x] **2.9 Run the build, the tests, and the demo**
  - Description: record the printed lines in `context.md`.
  - File(s): `context.md`
  - Estimate: 3 steps

---

## Phase 3: Textures and Images (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsAnImageWithItsTexture' --show-live-output on` and reads `size 64x32` for the handle, then a command line with `tex id 4660` and the image bounds `20,20-84,52`, then a command line with `tex atlas` for the rect that follows.
**Needs:** Phase 2

Group A (CR-2 option a, 2 files):

- [x] **3.1 Add the size to `TextureHandle`**
  - Description: Edit `src/BrutalUi.Core/Rendering/RenderHandles.cs`: `TextureHandle` gets `public int2 Size { get; }` and `public TextureHandle(int id, int2 size)`. The `TextureHandle(int id)` constructor stays and sets the size to `(0, 0)`. Equality and the hash code stay on the id only. The doc comment states why: one renderer gives one size for each id. The summary states that the size is in texels and that `Image` reads it for its natural size.
  - File(s): `src/BrutalUi.Core/Rendering/RenderHandles.cs`
  - Estimate: 4 steps

- [x] **3.2 Add the handle size tests**
  - Description: Add to `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`: the size reads back, the one-argument constructor gives `(0, 0)`, and two handles with the same id and another size are equal. Run the Core tests. `PaintWalk.cs` and `RecordingRenderer.cs` do not change.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`
  - Estimate: 4 steps

Group B (the service and the image, 4 files):

- [x] **3.3 Fill `ImGui/ImGuiTextureService.cs`**
  - Description: the table and the five members per "The Texture Service", with the Vulkan types and the return value of task 1.1.
  - File(s): `src/BrutalUi.ImGui/ImGuiTextureService.cs`
  - Estimate: 8 steps

- [x] **3.4 Add `Image` to `ImGui/ImGuiRenderer.cs`**
  - Description: `Image` per "The Mapping Table", with `AddImage` only. Phase 4 adds the rounded form.
  - File(s): `src/BrutalUi.ImGui/ImGuiRenderer.cs`
  - Estimate: 4 steps

- [x] **3.5 Write `tests/ImGui/ImGuiTextureServiceTests.cs`**
  - Description: `GivesIdsFromOneWithTheSize` (a reference with the id 4660 and the size 64 by 32), `GetTextureReturnsTheReference`, `RemoveMakesTheHandleUnknown`, `NeverReusesAnId`, `ThrowsForANoneHandle`, `ThrowsForASizeOfZero`, and `AddVulkanThrowsForANullBackend` (no GPU, the check comes before the backend call).
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiTextureServiceTests.cs`
  - Estimate: 6 steps

- [x] **3.6 Write `tests/ImGui/ImGuiRendererImageTests.cs`**
  - Description: `DrawsAnImageWithItsTexture` (the demo: register 4660 at 64 by 32, draw it at (20, 20, 64, 32), then draw a rect. Print `size 64x32` and the dump), `MapsTheUvRectToTheCorners` (the UV of the top-left and bottom-right vertices equal `(u0, v0)` and `(u1, v1)`), `DrawsTheTint`, `DrawsNothingForTheNoneHandle`, `DrawsNothingForAZeroTint`, and `ThrowsForARemovedTexture`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererImageTests.cs`
  - Estimate: 8 steps

- [x] **3.7 Run the build, the tests, and the demo**
  - Description: record the dump and the Vulkan signature in `context.md`.
  - File(s): `context.md`
  - Estimate: 3 steps

---

## Phase 4: Corners, Borders, and the Edge Rules (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsARoundedPanelWithABorder' --show-live-output on` and reads the resolved rules `corners 8,8,0,0 -> rounding 8 flags TopLeft|TopRight` and `border 2 -> inset 0.5 rounding 7`, then a command line whose vertices lie inside `0,0-200,100` grown by 1 pixel, in the fill color and the border color.
**Needs:** Phase 3

- [x] **4.1 Add the corner and border rules to `ImGui/ImGuiMapping.cs`**
  - Description: add `ResolveCorners`, `BorderInset`, `BorderRounding`, and `BorderCoversBox` per "The Pure Rules".
  - File(s): `src/BrutalUi.ImGui/ImGuiMapping.cs`
  - Estimate: 5 steps

- [x] **4.2 Use the rules in `ImGui/ImGuiRenderer.cs`**
  - Description: `Rect` uses the corner rule for the fill and the border rules for the stroke and the full-cover case. `Image` uses `AddImageRounded` when `rounding > 0`. The remarks of the class state the corner rule and the limit of Decision 4.
  - File(s): `src/BrutalUi.ImGui/ImGuiRenderer.cs`
  - Estimate: 6 steps

- [x] **4.3 Add the corner and border rule tests to `tests/ImGui/ImGuiMappingTests.cs`**
  - Description: on a 100 by 100 rect unless stated: all 0 gives `RoundCornersNone`, `NaN` and -3 give 0, `All(4)` gives 4 with four flags, `(4, 4, 0, 0)` gives `TopLeft|TopRight`, `(2, 6, 0, 0)` gives 6 with two flags, and 100 on a 40 by 20 rect gives 10. The inset: 1 gives 0, 2 gives 0.5, 4 gives 1.5. The border rounding: 8 and 2 give 7, 1 and 4 give 0. The full cover: 2 on a 4 by 10 box is true, 1 on it is false.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiMappingTests.cs`
  - Estimate: 5 steps

- [x] **4.4 Add the rounded and border tests to `tests/ImGui/ImGuiRendererRectTests.cs`**
  - Description: `DrawsARoundedPanelWithABorder` (the demo: a 200 by 100 rect, radius `(8, 8, 0, 0)`, border 2. Print the two rule lines and the dump), `DrawsTheBorderAfterTheFill` (the fill vertices come before the border vertices), `KeepsTheBorderInsideTheRect` (border 3 on a square rect: each border vertex lies inside the rect grown by 1 pixel for the anti-alias fringe), `FillsTheBoxForAWideBorder` (one fill in the border color and no stroke), `DrawsASquareRectForANaNRadius` (6 elements, as phase 1), and `DrawsABorderOnlyRect`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs`
  - Estimate: 8 steps

- [x] **4.5 Add `DrawsARoundedImage` to `tests/ImGui/ImGuiRendererImageTests.cs`**
  - Description: more than 6 elements, and the texture of the handle.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererImageTests.cs`
  - Estimate: 3 steps

- [x] **4.6 Add the edge tests and run the gate**
  - Description: `BeginThrowsWithNoContext` and `MeasureTextThrowsWithNoContext` (no harness, so no current context). Run the build, the tests, and the demo. Record the demo lines in `context.md`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs` or `ImGuiRendererTextTests.cs`, `context.md`
  - Estimate: 5 steps

---

## Phase 5: The Allocation Gate and the Record (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-class '*ImGuiRendererAllocationTests'` and reads a total of 3 with `failed: 0`.
**Needs:** Phase 4

Group A (the gate, 2 files):

- [x] **5.1 Write `tests/ImGui/ImGuiRendererAllocationTests.cs`**
  - Description: `DrawAllocatesNothingAfterWarmUp`: a harness of 1280 by 720 (the viewport of `ThousandElementTree`), `ThousandElementTree.Build()`, one `Update`. Run 3 warm-up frames. Then run 100 frames. In each frame, read `GC.GetAllocatedBytesForCurrentThread()` right before and right after `tree.Context.Draw(renderer)`, inside `BeginFrame` and `EndFrame`, and add the difference. Assert a sum of 0. `DirectCallsAllocateNothingAfterWarmUp`: register a second font and a texture. In each frame, call `Begin`, `PushClip`, `Rect` (a fill, a rounded fill with a border, and a full-cover border), `Image` (square and rounded), `Text`, `MeasureText` (no wrap and wrap), `PopClip`, and `End`. Call `MeasureText` once more after `EndFrame`, outside the frame. Measure the same way and assert 0. `TheMeasuredDrawDoesRealWork`: the same tree drawn once with `RecordingRenderer` gives the count of `Rect` calls. The background list of one `ImGuiRenderer` frame holds 4 vertices or more for each of them.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`
  - Estimate: 10 steps

- [x] **5.2 Prove that the gate blocks**
  - Description: Copy `src/BrutalUi.ImGui/ImGuiRenderer.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Rect`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Run the tests again and expect 0 failed. Record both exit codes in `context.md`.
  - File(s): `src/BrutalUi.ImGui/ImGuiRenderer.cs` (temporary, restored), `context.md`
  - Estimate: 6 steps

- [x] **5.3 Run the demo class 5 times and the Linux test script**
  - Description: Run the demo class 5 times and record the result in `context.md`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.
  - File(s): `context.md`
  - Estimate: 4 steps

Group B (the record, 4 files):

- [x] **5.4 Edit `docs/technical-design.md`**
  - Description: Section 3: add `BrutalUi.ImGui.Tests/` to the tree and one sentence on the headless ImGui tests to the responsibilities of `tests/`. Section 8, "ImGui implementation": in this epic, `ImGuiRenderer` writes into the background list of the main viewport (Q2). A private list waits for the world-space epic. Name the two services, the explicit font and size calls (Decision 2), the corner rule and its limit (Decision 4), the border inset (Decision 5), the size and wrap rules, the viewport rule (Decision 6), and the handle errors (Decision 7). Section 14: add the headless ImGui tests and the second allocation gate to "Test types".
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] **5.5 Edit `CHANGELOG.md`**
  - Description: add one line under `### Added`: `ImGuiRenderer`, `ImGuiFontService`, `ImGuiTextureService`, the size of `TextureHandle`, the `wrapWidth` parameter of `IUiRenderer.Text`, and the headless test project (imgui-renderer/imgui-draw-list).
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] **5.6 Edit `docs/features/imgui-renderer/human-testing.md`**
  - Description: make sure the `## imgui-draw-list` section holds the 5 phase demos (the run adds each one as the slice lands) and add the Windows run of `scripts\test-windows.ps1`, in the form of the `## renderer-interface` section. Add no line that the section already holds.
  - File(s): `docs/features/imgui-renderer/human-testing.md`
  - Estimate: 2 steps

- [x] **5.7 Lint the docs and add the native dependency note**
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. In `context.md`, add a "Native Dependencies" note: `dotnet test` now loads `libimgui.so` (Linux) and `imgui.dll` (Windows) from the pinned `Ahwoo.BRUTAL.ImGui` package. It is not a new dependency. Add the hand-offs of Notes. Run the build and the tests.
  - File(s): `context.md`, the changed docs
  - Estimate: 6 steps

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
- Phase 1 group A (tasks 1.1 to 1.5): the binding probe, the headless test project, `AssemblyInfo.cs`, `InternalsVisibleTo`, and the harness with 9 smoke tests in `HeadlessImGuiTests.cs`.
- Phase 1 group B (tasks 1.6 to 1.11): `ImGuiMapping`, the two service shells, `ImGuiRenderer` (frame, clips, square fills), 16 mapping tests, and 19 rect tests. The demo prints the two expected lines. The total is 667, 0 failed.
- Phase 2 group 1 (tasks 2.1, 2.2, 2.4, and 2.5): the font service, `FontSize`, `WrapWidth`, `RoundUpWidth`, 21 mapping tests, and 11 font service tests. The total is 699, 0 failed.
- Phase 2 group 2 (task 2.3): `ImGuiRenderer.Text` and `MeasureText`, the `wrapWidth` parameter of `IUiRenderer.Text`, the wrap width in `RecordingRenderer`, and 4 recorder tests in `PaintContentTests.cs`. The total is 703, 0 failed.
- Phase 2 group 3 (tasks 2.6 to 2.9): 47 renderer text tests in `ImGuiRendererTextTests.cs`, with the demo `MeasuresAndDrawsText`. The total is 750, 0 failed.
- Phase 3 group 1 (tasks 3.1 and 3.2): `TextureHandle` gets `int2 Size` and a two-argument constructor. 3 handle tests in `RenderValueTests.cs`. The total is 753, 0 failed.
- Phase 3 group 2 (tasks 3.3 to 3.7): `ImGuiTextureService` (the table and the five members), `ImGuiRenderer.Image` with `AddImage`, 16 texture service tests, and 17 image tests with the demo `DrawsAnImageWithItsTexture`. The total is 786, 0 failed.
- Phase 4 (tasks 4.1 to 4.6): the corner and border rules, the rounded fill, the border stroke, the full-cover border, `AddImageRounded`, and 38 tests with the demo `DrawsARoundedPanelWithABorder`. The user paused the run part-way in phase 4. A resumed run checked the files, fixed 6 wrong test expectations, and removed debug code. No change in `src/`. The total is 824, 0 failed.
- Phase 5 group 1 (tasks 5.1 to 5.3): `ImGuiRendererAllocationTests` with 3 tests. The tree draw and the direct calls allocate 0 bytes over 100 frames. The planted allocation gives exit 2, and the restore gives exit 0. `scripts/test-linux.sh` passes. The total is 827, 0 failed.
- Phase 5 group 2 (tasks 5.4 to 5.7): sections 3, 8, and 14 of `docs/technical-design.md`, the `CHANGELOG.md` line, the Windows run line in `human-testing.md`, the native dependency note, and the hand-offs. Each changed doc passes the lint.
- The cross-phase review (`code-review.md`): 10 findings, fixed in 6 review groups. The review verifier: PASS. The total is 864, 0 failed.

### Discovered New Tasks
- none

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
