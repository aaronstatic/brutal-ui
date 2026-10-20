# ImGui Draw List - Implementation Plan

**Epic:** imgui-renderer (feature 2 of 4)
**Status:** Planning
**Started:** 2026-10-20
**Target Completion:** TBD
**Last Updated:** 2026-10-20 04:25

---

## Quality Bar

**Target quality:** A renderer behind a core contract. label-and-image calls `MeasureText` for each label layout (A4). hello-panel-sample puts the result on the screen, and the world-space epic adds a second draw list on the same services. A wrong mapping draws wrong pixels, and no pixel test catches it.

- **Correctness of each mapped call:** Each `IUiRenderer` member has one mapping rule (see "The Mapping Table"). A headless test reads the ImGui draw list (commands, clip rects, textures, vertex bounds, and vertex colors) and proves each rule. No test needs a GPU (A6).
- **Zero allocation:** `Begin`, the draw calls, `MeasureText`, and `End` allocate 0 bytes after warm-up. A gate test measures `UiContext.Draw` with `ImGuiRenderer` on the warm 1000-element tree and the direct calls of each member. A planted allocation proves that the gate blocks.
- **The contract of `IUiRenderer`:** `MeasureText` works outside a `Begin` and `End` pair and outside an ImGui frame. `Begin` clears the clips of an aborted frame. A radius of 0 or less, or `NaN`, gives a square corner. The border stays inside the rect.
- **No GPU in a unit test:** The tests create an ImGui context with no backend. No test creates a `VulkanBackend`, a window, or a device.
- **No new pin:** `BrutalUi.ImGui.csproj` keeps its 2 package references (A9). `Directory.Packages.props` does not change.

---

## Executive Summary

This feature ships the first renderer. `BrutalUi.ImGui` gets `ImGuiRenderer`, which implements `IUiRenderer` on the background draw list of the main viewport (Q2). It also gets two services. `ImGuiFontService` maps a `FontHandle` to an `ImFontPtr` of the ImGui font atlas, with the default stb loader of ImGui (Q3). `ImGuiTextureService` maps a `TextureHandle` to an `ImTextureRef` and a size. It registers a Vulkan texture through `VulkanBackend.AddTexture` (Q6), or it accepts a texture reference that the game registered. A `required` create-info struct gives the renderer both services. An internal static class `ImGuiMapping` holds the pure rules (the color cast, the corner rule, the border inset, the size rule, and the wrap rule), so a test checks them with no native call. Text uses the explicit font and size calls of the ImGui 1.92 dynamic font system (`ImDrawList.AddText(font, size, ...)` and `ImFont.CalcTextSizeA(size, ...)`), so `MeasureText` needs no frame and no font stack. A new test project, `tests/BrutalUi.ImGui.Tests`, runs a headless ImGui context with no GPU and reads the draw list after each frame. The plan assumes option (a) of CR-2: `TextureHandle` gets a size. The sample does not change. hello-panel-sample owns it.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates the two services and the renderer once, registers fonts and textures, and calls `UiContext.Draw(renderer)` between `ImGui.NewFrame` and `ImGui.Render`)
- [x] **The sibling feature agents** (label-and-image calls `MeasureText` and draws text and images, hello-panel-sample registers a procedural Vulkan texture)
- [x] **The world-space epic** (shares the font service and the texture service with a second renderer on a private draw list)
- [ ] **The markup author** (not served. The markup comes in a later epic)

**User Subsets Served:**
- [x] **Simple** - Create `new ImGuiRenderer(new ImGuiRenderer.CreateInfo { Fonts = new ImGuiFontService(), Textures = new ImGuiTextureService() })` and call `context.Draw(renderer)` each frame. Font id 0 is the ImGui default font.
- [x] **Complex** - Several fonts at any size, user textures with UV rects and tints, nested clips, rounded corners, borders, and measure calls from layout outside the frame.

**UX Approach:**
- The renderer throws `InvalidOperationException` for a draw call before `Begin`, for a `PopClip` with no open clip, and for a call with no current ImGui context. It throws `ArgumentOutOfRangeException` for an unknown font id or texture id. It throws nothing else of its own.
- The services allocate at registration only. A frame allocates nothing.

---

## Goals

### Primary Goals
1. Put the element tree into the ImGui background draw list with no change of the interface.
2. Ship `MeasureText` with a wrap width that works at any time on the main thread, so label-and-image only calls it (A4).
3. Map font handles and texture handles to ImGui objects through two shareable services.
4. Keep the draw free of managed allocation, and prove it with no GPU.

### Success Criteria
- [ ] The headless demo of phase 1 prints the draw commands of the three-element tree: the root fill, then the two child fills under the clip `2,2,198,98`.
- [ ] `MeasureText` gives a height of one line for no wrap, two lines at a narrow wrap width, and the same result before the first frame, inside a frame, and between frames.
- [ ] An image draw command carries the texture of its handle, and the next rect draws with the font atlas again.
- [ ] `DrawAllocatesNothingAfterWarmUp` and `DirectCallsAllocateNothingAfterWarmUp` measure 0 bytes over 100 frames.
- [ ] `BrutalUi.ImGui.csproj` still holds 2 `PackageReference` items.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.ImGui/
├── BrutalUi.ImGui.csproj                    # phase 1: InternalsVisibleTo the test project
├── ImGuiRenderer.cs                         # phase 1 (phase 2 text, phase 3 image, phase 4 corners and borders)
├── ImGuiMapping.cs                          # phase 1: internal static rules (phases 2 and 4 add rules)
├── ImGuiFontService.cs                      # phase 1 shell, phase 2 body
└── ImGuiTextureService.cs                   # phase 1 shell, phase 3 body
src/BrutalUi.Core/Rendering/RenderHandles.cs # phase 3: TextureHandle gets a size (CR-2 option a)
tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs  # phase 3: the handle size tests
tests/BrutalUi.ImGui.Tests/                  # new project, phase 1
├── BrutalUi.ImGui.Tests.csproj              # links ../BrutalUi.Core.Tests/Shared/*.cs
├── AssemblyInfo.cs                          # no parallel tests: ImGui has one current context
├── HeadlessImGui.cs                         # the headless context and the draw list dump
├── ImGuiMappingTests.cs                     # phases 1, 2, and 4
├── ImGuiRendererRectTests.cs                # phases 1 and 4
├── ImGuiFontServiceTests.cs                 # phase 2
├── ImGuiRendererTextTests.cs                # phase 2
├── ImGuiTextureServiceTests.cs              # phase 3
├── ImGuiRendererImageTests.cs               # phases 3 and 4
└── ImGuiRendererAllocationTests.cs          # phase 5
brutal-ui.sln                                # phase 1: the test project under the tests folder
docs/technical-design.md                     # phase 5: sections 3, 8, and 14
docs/features/imgui-renderer/human-testing.md # phase 5: the imgui-draw-list section
CHANGELOG.md                                 # phase 5: one Added line
```

`samples/HelloPanel/` does not change. hello-panel-sample owns it (A1).

### The Public API

All types live in `namespace BrutalUi.ImGui;`. Each public member has an XML doc comment (`src/Directory.Build.props` sets `GenerateDocumentationFile`, and a warning is an error).

```csharp
public sealed class ImGuiRenderer : IUiRenderer
{
    public struct CreateInfo
    {
        public required ImGuiFontService Fonts;
        public required ImGuiTextureService Textures;
    }

    public ImGuiRenderer(in CreateInfo info);      // ArgumentNullException for a null service
    public ImGuiFontService Fonts { get; }
    public ImGuiTextureService Textures { get; }
    // The eight members of IUiRenderer.
}

public sealed class ImGuiFontService
{
    public ImGuiFontService();
    public FontHandle Add(ImFontPtr font);          // a font of ImGui.GetIO().Fonts. Ids start at 1.
    public ImFontPtr GetFont(FontHandle font);      // id 0: the default font. Unknown id: ArgumentOutOfRangeException.
}

public sealed class ImGuiTextureService
{
    public ImGuiTextureService();
    public TextureHandle Add(ImTextureRef texture, int2 size);   // a texture that the game registered
    public TextureHandle AddVulkan(VulkanBackend backend, VkSampler sampler, VkImageView imageView,
                                   VkImageLayout layout, int2 size);  // calls backend.AddTexture (Q6)
    public void Remove(TextureHandle texture);      // calls backend.RemoveTexture for a Vulkan texture
    public ImTextureRef GetTexture(TextureHandle texture);
    public int2 GetSize(TextureHandle texture);
}
```

The Vulkan parameter types and the return type of `VulkanBackend.AddTexture` come from the binding probe of task 1.1. The names above follow `docs/feasibility.md` (`AddTexture(sampler, imageView, layout)`). The Vulkan types come through `Ahwoo.BRUTAL.ImGui.Backends`, which depends on `Ahwoo.BRUTAL.Vulkan.Abstractions`. No new reference is necessary.

**Gotcha (the name `ImGui`):** In `namespace BrutalUi.ImGui;` the simple name `ImGui` binds to the namespace `BrutalUi.ImGui`, not to the class `Brutal.ImGuiApi.ImGui`. `.editorconfig` puts `using` directives outside the namespace (`csharp_using_directive_placement = outside_namespace:warning`), so a `using` inside the namespace fails the build. Use an alias with another name in each file that calls the class: `using Gui = Brutal.ImGuiApi.ImGui;`. The test project has the same problem in `namespace BrutalUi.ImGui.Tests;`.

### The Mapping Table

The renderer keeps four private fields for a frame: the draw list (`ImDrawListPtr`, unset outside a frame), the origin (`float2`, the position of the main viewport), the ImGui frame count of `Begin`, and the clip depth (`int`). "Origin" below means the stored origin.

| Member | ImGui calls | Rules |
|--------|-------------|-------|
| `Begin(displaySize, scale)` | `Gui.GetCurrentContext()`, `Gui.GetMainViewport()`, `Gui.GetBackgroundDrawList(viewport)`, `Gui.GetFrameCount()` | No context: throw `InvalidOperationException`. When a frame of the same ImGui frame count and the same list has open clips (no `End`), call `PopClipRect` for each, then set the depth to 0 (the `Begin` contract). Store the list, `viewport.Pos` as the origin, and the frame count. `displaySize` and `scale` are not used (Decision 6). |
| `PushClip(rect)` | `drawList.PushClipRect(origin + (x, y), origin + (x + w, y + h), true)` | Add 1 to the depth. The walk intersects already. The `true` keeps the clip inside the viewport. A rect with an area of 0 is valid. |
| `PopClip()` | `drawList.PopClipRect()` | Depth 0: throw `InvalidOperationException` before the native call. Else subtract 1. |
| `Rect(rect, color, radius, borderWidth, borderColor)` | `AddRectFilled(min, max, col, rounding, flags)`, then `AddRect(min + d, max - d, borderCol, borderRounding, flags, borderWidth)`, or a second `AddRectFilled` with the border color | No area, or a `NaN` component: draw nothing. The fill draws when its alpha is above 0. The border draws when `borderWidth > 0` and its alpha is above 0. The corner rule gives `rounding` and `flags`. The border rules give `d`, `borderRounding`, and the full-cover case. The fill comes first. |
| `Image(rect, texture, uv, tint, radius)` | `AddImage(tex, min, max, (u0, v0), (u1, v1), col)`, or `AddImageRounded(..., col, rounding, flags)` when `rounding > 0` | `texture.IsNone`, a tint alpha of 0, or no area: draw nothing. An unknown id: `ArgumentOutOfRangeException` from the service. ImGui pushes and pops the texture itself. |
| `Text(pos, font, size, color, utf8)` | `AddText(fontPtr, fontSize, origin + pos, col, begin, end, 0, null)` | An empty span or an alpha of 0: draw nothing. The size rule gives `fontSize`. `fixed` pins the span. `end` is given, so ImGui needs no zero byte. |
| `MeasureText(font, size, utf8, wrapWidth)` | `fontPtr.CalcTextSizeA(fontSize, float.MaxValue, wrap, begin, end, null)` | No draw list and no frame needed. No context: throw `InvalidOperationException`. An empty span gives `(0, fontSize)` with no native call. The wrap rule gives `wrap`. The width rounds up to a whole pixel, as `ImGui.CalcTextSize` does (`MathF.Floor(width + 0.99999f)`). The height is the line count times `fontSize`. ImGui breaks lines after a blank or after the punctuation `. , ; ! ? "`. It cuts a word that is wider than the wrap width inside the word (`ImFont.CalcWordWrapPosition`), so the width stays at or below the wrap width, except for a wrap width below one glyph. |
| `End()` | `PopClipRect` for each open clip | Pop the clips that are still open, so the ImGui list stays balanced. Unset the list. |

Each draw call (`PushClip`, `PopClip`, `Rect`, `Image`, `Text`) throws `InvalidOperationException` when no `Begin` started the frame.

**Gotcha (the empty span):** `fixed (byte* p = utf8)` gives a null pointer for an empty span. ImGui reads a null `text_end` as "use `strlen`", and a null `text_begin` then crashes the process. The empty-span check comes before each `fixed`.

### The Pure Rules (`ImGuiMapping`, internal static)

| Rule | Function | Result |
|------|----------|--------|
| Color | `uint ToImU32(in Rgba8 color)` | `Unsafe.As<Rgba8, uint>(ref Unsafe.AsRef(in color))`. The value equals `IM_COL32(r, g, b, a)` on a little-endian CPU (renderer-interface A5). |
| Area | `bool HasArea(in float4 rect)` | `X` and `Y` are finite, and `Z > 0` and `W > 0`. `NaN` fails each compare. |
| Corners | `ImDrawFlags ResolveCorners(in Corners radius, float width, float height, out float rounding)` | (1) A radius of 0 or less, or `NaN`, becomes 0. (2) Each radius is limited to half the shorter side. (3) `rounding` is the largest radius. (4) The flags name each corner with a radius above 0. (5) No corner above 0: `rounding = 0` and `ImDrawFlags.RoundCornersNone`. ImGui reads flags with no corner bit as "all corners", so rule 5 matters. The result is exact when the radii above 0 are equal. Mixed radii above 0 draw with the largest one (Decision 4). |
| Border inset | `float BorderInset(float borderWidth)` | `borderWidth * 0.5f - 0.5f`. `AddRect` insets its path by 0.5 and strokes on the center of the path, so this inset puts the stroke exactly on `[0, borderWidth]` inside the rect. |
| Border rounding | `float BorderRounding(float rounding, float borderWidth)` | `MathF.Max(rounding - borderWidth * 0.5f, 0f)`. The outer edge of the stroke then follows the fill radius. A result of 0 uses `RoundCornersNone`. |
| Full cover | `bool BorderCoversBox(float borderWidth, float width, float height)` | `borderWidth * 2 >= MathF.Min(width, height)`. The border then fills the box: one `AddRectFilled` with the border color and the fill radius, and no stroke. |
| Font size | `float FontSize(float size, float defaultSize)` | `size` when it is above 0 and finite, else `defaultSize` (the `LegacySize` of the font, the size of its `AddFont` call). |
| Wrap | `float WrapWidth(float wrapWidth)` | `wrapWidth` when it is above 0, else 0 (the ImGui value for no wrap). `NaN` gives 0. |
| Width | `float RoundUpWidth(float width)` | `MathF.Floor(width + 0.99999f)`. |

### The Font Service

- A private `ImFontPtr[]` table, start size 4, grown by doubling in `Add` only. Index 0 is not used. `Add` gives the next index as the id. Ids never repeat.
- `Add(ImFontPtr font)`: a null font throws `ArgumentNullException`. A font of another atlas (`font.ContainerAtlas` differs from `Gui.GetIO().Fonts`) throws `ArgumentException`. The background draw list binds the atlas texture of `io.Fonts`, so a font of another atlas draws wrong glyphs.
- `GetFont(FontHandle font)`: id 0 gives the default font: `io.FontDefault` when it is set, else the first font of `io.Fonts`, else the result of one `io.Fonts.AddFontDefault()` call (the ImGui default loader, Q3). The method reads `io` at each call, so a later `io.FontDefault` write takes effect. An id above the count, or below 0, throws `ArgumentOutOfRangeException`.
- The doc comment shows how to load a TTF file with the default loader: `io.Fonts.AddFontFromFileTTF(...)`, then `fonts.Add(result)`. The service loads no file itself (YAGNI, Notes gap 8).
- The service uses the current ImGui context. Create it after `ImGui.CreateContext()`. One context per process (the doc states it).

### The Texture Service

- A private `Entry[]` table, start size 4, grown by doubling in `Add` and `AddVulkan` only. An entry holds the `ImTextureRef`, the `int2` size, the `VulkanBackend?` owner, the backend value that `RemoveTexture` takes, and a removed flag. Ids start at 1 and never repeat, so a stale handle never names a new texture.
- `Add(ImTextureRef texture, int2 size)`: a size with a component of 0 or less throws `ArgumentOutOfRangeException`. The caller owns the registration.
- `AddVulkan(...)`: a null backend throws `ArgumentNullException`. It calls `backend.AddTexture(sampler, imageView, layout)`, builds the `ImTextureRef` from the result, and stores the backend for `Remove`. The game owns the image, the view, and the sampler. The service owns the registration only.
- `Remove(TextureHandle texture)`: an unknown, removed, or none handle throws `ArgumentOutOfRangeException`. For a Vulkan entry, it calls `backend.RemoveTexture(...)`. The doc comment states the lifetime: call it when no frame in flight uses the texture (for example after `vkDeviceWaitIdle`), and before the backend is disposed. A backend dispose frees all its descriptor sets, so a game that ends needs no `Remove` call.
- `GetTexture` and `GetSize`: an unknown or removed id throws `ArgumentOutOfRangeException`.
- Assumed CR-2 option (a): `Add` and `AddVulkan` return `new TextureHandle(id, size)`, so `Image` reads `handle.Size` with no renderer. `GetSize` stays for options (b) and (c).

### The Headless Harness (`tests/BrutalUi.ImGui.Tests/HeadlessImGui.cs`)

`internal sealed class HeadlessImGui : IDisposable`. Each test creates one and disposes it. No GPU, no window, no backend.

1. The constructor calls `Gui.CreateContext()`, sets `io.IniFilename` to none, sets `io.DisplaySize` (a constructor argument, default 400 by 300), sets `io.DeltaTime` to 1/60, and adds `ImGuiBackendFlags.RendererHasTextures` to `io.BackendFlags`. The flag lets the 1.92 atlas bake glyphs on demand with no built texture.
2. `BeginFrame()` calls `Gui.NewFrame()`.
3. `EndFrame()` calls `Gui.Render()`, then sets the status of each texture of `Gui.GetPlatformIO().Textures` that wants a create or an update to OK, as a null backend does. Then the next `NewFrame` finds no pending texture.
4. `string DescribeBackgroundList()` reads `Gui.GetBackgroundDrawList(Gui.GetMainViewport())` after `EndFrame` and gives one line for each draw command with `CultureInfo.InvariantCulture`: `cmd <i> clip <x0>,<y0>,<x1>,<y1> tex <atlas|id N> elems <n> verts <minx>,<miny>-<maxx>,<maxy> colors <#RRGGBBAA ...>`. The clip is in min-max form, as ImGui stores it. `verts` are the bounds of the vertices that the indices of the command use. `colors` lists the distinct vertex colors in first-seen order. It allocates, so a measured region never calls it.
5. `Dispose` calls `Gui.Render()` when a frame is open, then `Gui.DestroyContext(context)`.

`AssemblyInfo.cs` holds `[assembly: CollectionBehavior(DisableTestParallelization = true)]`, because ImGui has one current context for the process.

### Data Flow

```
Game frame:  GlfwBackend.NewFrame ─► VulkanBackend.NewFrame ─► ImGui.NewFrame
             UiContext.Update(dt) ─► Label measure callback ─► renderer.MeasureText   (label-and-image)
             UiContext.Draw(renderer)
               └─ Begin ─► background list of the main viewport
               └─ PaintWalk ─► PushClip / Rect / PaintContent (Text, Image) / PopClip
               └─ End
             ImGui.Render ─► VulkanBackend.RenderDrawData   (hello-panel-sample)
Tests:       HeadlessImGui.BeginFrame ─► UiContext.Draw(renderer) ─► EndFrame ─► DescribeBackgroundList
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements leave the demo of phase 1 open, so Notes holds an open scope question (rule 12). Phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. The xUnit analyzers are errors. Use `TestContext.Current.TestOutputHelper?.WriteLine(...)` to print. A demo uses `dotnet run --project <test project> -- --filter-method '*Name' --show-live-output on`. The `dotnet test --project` form prints no output line (renderer-interface `context.md`).

### Phase 1: Rects and Clips on the Background List
**Goal:** The smallest path that proves the idea: a headless ImGui frame, `UiContext.Draw` with `ImGuiRenderer`, and square fills under one clip in the background draw list.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads two command lines: the root fill `0,0-200,100` in `#0A141EFF` under the clip `0,0,400,300`, then the two child fills in `#FF0000FF` and `#00FF00FF` under the clip `2,2,198,98`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase fixes the binding call form for the whole feature, the test harness that each later phase uses, and the frame state of the renderer. The binding names are not verified (Risks 1 to 3). Task 1.1 settles them before any code. Write `ImGuiRenderer` in its end-state shape (the four frame fields and the `Begin` contract), so phases 2 to 4 fill in members and not a new structure.

**Tasks:**

Group A (the harness, 5 files and `context.md`):
- [ ] 1.1 Record the test total of the solution before the first edit in `context.md`. Then run a binding probe. Write a console project in the session scratch folder, not in the repository, with `PackageReference` items for `Ahwoo.BRUTAL.ImGui` and `Ahwoo.BRUTAL.ImGui.Backends` at the pinned versions. Print through reflection: (a) whether `Brutal.ImGuiApi.ImGui.PInvoke` and its members `ImDrawList_AddRectFilled`, `ImDrawList_AddRect`, `ImDrawList_AddText_1`, `ImDrawList_AddImage`, `ImDrawList_AddImageRounded`, `ImDrawList_PushClipRect`, `ImDrawList_PopClipRect`, and `ImFont_CalcTextSizeA` are public, (b) the raw pointer member of `ImDrawListPtr` and `ImFontPtr`, (c) the fields of `ImColor8`, `ImTextureRef`, and `ImTextureID`, (d) the signatures of `VulkanBackend.AddTexture` and `VulkanBackend.RemoveTexture`, (e) the status members of `ImTextureDataPtr` and the type of `ImGuiPlatformIOPtr.Textures`, (f) the names of the `ImDrawFlags` corner values, and (g) the members of `ImGuiViewportPtr` for the position. Then, in the same program, create a context with the flags of "The Headless Harness", call `CalcTextSizeA` on the default font before the first `NewFrame`, run one frame, call it again after `Render`, and measure `GC.GetAllocatedBytesForCurrentThread()` around 1000 `AddRectFilled` and 1000 `CalcTextSizeA` calls in the chosen form. Record each answer in `context.md`. Choose the call form: the raw `PInvoke` members with pointers when they are public (preferred, no `ImString` arena), else the extension methods. Record the choice. When a call before the first frame stops the process, record it, and follow the mitigation of Risk 2.
- [ ] 1.2 Write `tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj` in the form of `tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj`: `OutputType` Exe, `PackageReference` `xunit.v3`, `ProjectReference` `../../src/BrutalUi.ImGui/BrutalUi.ImGui.csproj`, and `<Compile Include="../BrutalUi.Core.Tests/Shared/*.cs" LinkBase="Shared" />` as in the benchmark project. Run `dotnet sln brutal-ui.sln add tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj --solution-folder tests`. Read `brutal-ui.sln` and confirm one `tests` folder that holds the new project.
- [ ] 1.3 Write `tests/BrutalUi.ImGui.Tests/AssemblyInfo.cs` with the attribute of "The Headless Harness" and a comment that states the reason.
- [ ] 1.4 Edit `src/BrutalUi.ImGui/BrutalUi.ImGui.csproj`: add `<InternalsVisibleTo Include="BrutalUi.ImGui.Tests" />` in the form of `BrutalUi.Core.csproj`. Add no `PackageReference`.
- [ ] 1.5 Write `tests/BrutalUi.ImGui.Tests/HeadlessImGui.cs` per "The Headless Harness". Use the alias `Gui` (see the gotcha of "The Public API").

Group B (the renderer, 6 files):
- [ ] 1.6 Write `src/BrutalUi.ImGui/ImGuiMapping.cs`: `internal static class ImGuiMapping` with `ToImU32` and `HasArea` per "The Pure Rules".
- [ ] 1.7 Write `src/BrutalUi.ImGui/ImGuiFontService.cs` and `src/BrutalUi.ImGui/ImGuiTextureService.cs` as `public sealed` classes with a public constructor and the class summary of "The Font Service" and "The Texture Service". Phases 2 and 3 add the members.
- [ ] 1.8 Write `src/BrutalUi.ImGui/ImGuiRenderer.cs`: `CreateInfo`, the constructor (a null service throws `ArgumentNullException`), `Fonts`, `Textures`, the four frame fields, and `Begin`, `PushClip`, `PopClip`, `End`, and `Rect` (the fill only, `rounding` 0 and `RoundCornersNone`) per "The Mapping Table". `Image`, `Text`, and `MeasureText` throw `NotSupportedException` with the text "imgui-draw-list phase 2 or 3 adds this member." until those phases. The class summary states: the background list of the main viewport (Q2), the call window (`Draw` between `ImGui.NewFrame` and `ImGui.Render`), the main thread, one ImGui context, and the viewport rule of Decision 6.
- [ ] 1.9 Write `tests/BrutalUi.ImGui.Tests/ImGuiMappingTests.cs`: `ToImU32` of `(1, 2, 3, 4)` is `0x04030201` (skip on a big-endian CPU, as `RenderValueTests` does). `HasArea` is false for a width or height of 0, a negative value, and `NaN` in each component.
- [ ] 1.10 Write `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs`:
  - `DrawsThreeElementTreeWithOneClip` (the demo): the tree of the renderer-interface demo (`PaintWalkTests.DrawsThreeElementTreeWithOneClip`) in a 400 by 300 harness. `Update`, `BeginFrame`, `Draw`, `EndFrame`. Print the dump and assert it equals these two lines: `cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 0,0-200,100 colors #0A141EFF` and `cmd 1 clip 2,2,198,98 tex atlas elems 12 verts 12,12-312,52 colors #FF0000FF #00FF00FF`. When ImGui splits or merges the commands in another way, record the real dump in `context.md`. Change the expected lines only when each fill, color, and clip value still matches.
  - `ThrowsForADrawCallBeforeBegin`, `ThrowsForAPopClipWithNoPush`, `ThrowsForANullService`.
  - `EndPopsAnOpenClip`: `PushClip` with no `PopClip`, then `End`. A later `Begin` and `Rect` in the same ImGui frame draw under the viewport clip.
  - `BeginClearsTheClipsOfAnAbortedFrame`: `Begin`, `PushClip`, no `End`, then `Begin` and `Rect` in the same ImGui frame. The rect draws under the viewport clip.
  - `SkipsAnEmptyRect` (width 0, height 0, `NaN`) and `SkipsATransparentFill`.
- [ ] 1.11 Run the build, the tests, and the demo. Record the printed dump and the test total in `context.md`.

**Estimated Steps:** about 66 (11 tasks, about six steps for one task)

**Files (10):** Group A: `BrutalUi.ImGui.Tests.csproj`, `brutal-ui.sln`, `AssemblyInfo.cs`, `BrutalUi.ImGui.csproj`, `HeadlessImGui.cs` (and `context.md`). Group B: `ImGuiMapping.cs`, `ImGuiFontService.cs`, `ImGuiTextureService.cs`, `ImGuiRenderer.cs`, `ImGuiMappingTests.cs`, `ImGuiRendererRectTests.cs`.

**Acceptance Criteria:**
- [ ] The demo prints the two command lines, and the test asserts them.
- [ ] `dotnet test --solution brutal-ui.sln` runs the new project, and no test of it touches `VulkanBackend`.
- [ ] `context.md` holds each answer of the binding probe and the chosen call form.

---

### Phase 2: Text and Measure
**Goal:** Draw and measure UTF-8 text through the font service with the explicit font and size calls of the dynamic font system, inside and outside a frame (A4).
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*MeasuresAndDrawsText' --show-live-output on` and reads three measure lines for `Hello, brutal-ui` (13 px with height 13, 26 px with height 26, and 13 px at a wrap width of 70 with height 26), then one command line whose glyph vertices lie inside the measured box.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** label-and-image builds its layout on this measure (A4). The calls run outside `NewFrame` and `Render`, which the binding probe checks first. A wrong rule here gives wrong layouts in each later sample.

**Tasks:**
- [ ] 2.1 Edit `ImGuiFontService.cs`: the table, `Add`, and `GetFont` per "The Font Service". The context check throws `InvalidOperationException` when `Gui.GetCurrentContext()` has no value.
- [ ] 2.2 Edit `ImGuiMapping.cs`: add `FontSize`, `WrapWidth`, and `RoundUpWidth` per "The Pure Rules".
- [ ] 2.3 Edit `ImGuiRenderer.cs`: `Text` and `MeasureText` per "The Mapping Table", in the call form of task 1.1. The empty-span check comes before `fixed` (the gotcha). The `MeasureText` remarks state each rule of the table row: the frame independence, the width rounding, the height, the break rules, the empty text, and the default size. Write the text draw as a private method with a wrap argument (`DrawText(..., float wrapWidth)`) that `Text` calls with 0. Option (a) of open scope question 3 then costs one line.
- [ ] 2.4 Add to `ImGuiMappingTests.cs`: `FontSize` of 0, -1, `NaN`, and positive infinity gives the default, and 20 gives 20. `WrapWidth` of 0, -5, and `NaN` gives 0, and 70 gives 70. `RoundUpWidth` of 112 gives 112, and of 112.2 gives 113.
- [ ] 2.5 Write `tests/BrutalUi.ImGui.Tests/ImGuiFontServiceTests.cs`: `ReturnsTheDefaultFontForIdZero` (an empty atlas gets one default font, and a second call gives the same font), `PrefersTheIoDefaultFont`, `GivesIdsFromOne` (two `io.Fonts.AddFontDefault()` fonts give ids 1 and 2, and `GetFont` returns each), `ThrowsForAnUnknownId` (3 and -1), `ThrowsForANullFont`, and `ThrowsWithNoContext`.
- [ ] 2.6 Write `tests/BrutalUi.ImGui.Tests/ImGuiRendererTextTests.cs` with the measure tests: `MeasuresOneLineWithTheSizeAsHeight`, `DoublesTheHeightForTwiceTheSize`, `WrapsAtANarrowWidth` (two lines at 70), `TreatsAZeroNegativeOrNaNWrapAsNoWrap` (equal to the no-wrap result), `CutsAWordThatIsWiderThanTheWrapWidth` (`brutal-ui` at a wrap width of 40 gives more than one line and a width at or below 40. When ImGui gives another result, record it in `context.md` and assert the real rule), `MeasuresEmptyTextAsZeroWidthAndOneLine`, `UsesTheFontSizeForASizeOfZero`, `MeasuresTheSameBeforeInsideAndBetweenFrames` (before the first `NewFrame`, inside a frame, and after `Render`, unless task 1.1 found a limit, see Risk 2), `MeasureDrawsNothing` (the vertex count of the list does not change), `MeasuresWithASecondFont`, and `ThrowsForAnUnknownFont`.
- [ ] 2.7 Add the draw tests to `ImGuiRendererTextTests.cs`: `DrawsGlyphsInsideTheMeasuredBox` (each vertex lies inside the box from `pos` to `pos + MeasureText(...)`, grown by 1 pixel), `DrawsTheTextColor`, `DrawsNothingForEmptyText`, `DrawsNothingForAZeroAlpha`, and `ClipsTextUnderAPushedClip` (the text command carries the clip).
- [ ] 2.8 Add the demo test `MeasuresAndDrawsText`: print the three measure results as `measure <size> wrap <w> -> <width>x<height>` and the dump of a frame that draws the 13 px text at (10, 10). Assert the heights 13, 26, and 26, the larger width at 26 px, and the vertex bounds inside the measured box. The wrap width 70 puts `Hello,` on line 1 and `brutal-ui` on line 2, because `brutal-ui` is one word (`-` is no break point) and fits in 70.
- [ ] 2.9 Run the build, the tests, and the demo. Record the printed lines in `context.md`.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (6):** `ImGuiFontService.cs`, `ImGuiMapping.cs`, `ImGuiRenderer.cs`, `ImGuiMappingTests.cs`, `ImGuiFontServiceTests.cs`, `ImGuiRendererTextTests.cs` (and `context.md`)

**Acceptance Criteria:**
- [ ] Each rule of the `MeasureText` row of "The Mapping Table" has one passing test or more.
- [ ] `MeasureText` gives the same result before the first frame, inside a frame, and between frames, or `context.md` and the doc comment state the limit that task 1.1 found.
- [ ] The renderer calls no `PushFont` and no `ImGui.CalcTextSize` (Decision 2).

---

### Phase 3: Textures and Images
**Goal:** Register textures, give each handle its size (CR-2 option a), and draw an image with its texture, UV rect, and tint.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsAnImageWithItsTexture' --show-live-output on` and reads `size 64x32` for the handle, then a command line with `tex id 4660` and the image bounds `20,20-84,52`, then a command line with `tex atlas` for the rect that follows.
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)

**Tier note:** Group A changes a public struct of `BrutalUi.Core`, and group B wraps the Vulkan backend, which no test can run. Each is an integration point. When the user picks option (b) or (c) of open scope question 2 before this phase, follow the swap in Notes, "Hand-offs".

**Tasks:**

Group A (CR-2 option a, 2 files):
- [ ] 3.1 Edit `src/BrutalUi.Core/Rendering/RenderHandles.cs`: `TextureHandle` gets `public int2 Size { get; }` and `public TextureHandle(int id, int2 size)`. The `TextureHandle(int id)` constructor stays and sets the size to `(0, 0)`. Equality and the hash code stay on the id only. The doc comment states why: one renderer gives one size for each id. The summary states that the size is in texels and that `Image` reads it for its natural size.
- [ ] 3.2 Add to `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`: the size reads back, the one-argument constructor gives `(0, 0)`, and two handles with the same id and another size are equal. Run the Core tests. `PaintWalk.cs` and `RecordingRenderer.cs` do not change.

Group B (the service and the image, 4 files):
- [ ] 3.3 Edit `ImGuiTextureService.cs`: the table and the five members per "The Texture Service", with the Vulkan types and the return value of task 1.1.
- [ ] 3.4 Edit `ImGuiRenderer.cs`: `Image` per "The Mapping Table", with `AddImage` only. Phase 4 adds the rounded form.
- [ ] 3.5 Write `tests/BrutalUi.ImGui.Tests/ImGuiTextureServiceTests.cs`: `GivesIdsFromOneWithTheSize` (a reference with the id 4660 and the size 64 by 32), `GetTextureReturnsTheReference`, `RemoveMakesTheHandleUnknown`, `NeverReusesAnId`, `ThrowsForANoneHandle`, `ThrowsForASizeOfZero`, and `AddVulkanThrowsForANullBackend` (no GPU, the check comes before the backend call).
- [ ] 3.6 Write `tests/BrutalUi.ImGui.Tests/ImGuiRendererImageTests.cs`: `DrawsAnImageWithItsTexture` (the demo: register 4660 at 64 by 32, draw it at (20, 20, 64, 32), then draw a rect. Print `size 64x32` and the dump), `MapsTheUvRectToTheCorners` (the UV of the top-left and bottom-right vertices equal `(u0, v0)` and `(u1, v1)`), `DrawsTheTint`, `DrawsNothingForTheNoneHandle`, `DrawsNothingForAZeroTint`, and `ThrowsForARemovedTexture`.
- [ ] 3.7 Run the build, the tests, and the demo. Record the dump and the Vulkan signature in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (6):** Group A: `RenderHandles.cs`, `RenderValueTests.cs`. Group B: `ImGuiTextureService.cs`, `ImGuiRenderer.cs`, `ImGuiTextureServiceTests.cs`, `ImGuiRendererImageTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The image command carries `tex id 4660`, and the next rect command carries `tex atlas`.
- [ ] Each existing test of `BrutalUi.Core.Tests` passes with no change.
- [ ] No test file of `tests/BrutalUi.ImGui.Tests` creates a `VulkanBackend`.

---

### Phase 4: Corners, Borders, and the Edge Rules
**Goal:** Draw the corner radius and the border by the contract: square for 0 or `NaN`, limited to half the shorter side, the border inside the rect, and the rounded image.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsARoundedPanelWithABorder' --show-live-output on` and reads the resolved rules `corners 8,8,0,0 -> rounding 8 flags TopLeft|TopRight` and `border 2 -> inset 0.5 rounding 7`, then a command line whose vertices lie inside `0,0-200,100` grown by 1 pixel, in the fill color and the border color.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 4.1 Edit `ImGuiMapping.cs`: add `ResolveCorners`, `BorderInset`, `BorderRounding`, and `BorderCoversBox` per "The Pure Rules".
- [ ] 4.2 Edit `ImGuiRenderer.cs`: `Rect` uses the corner rule for the fill and the border rules for the stroke and the full-cover case. `Image` uses `AddImageRounded` when `rounding > 0`. The remarks of the class state the corner rule and the limit of Decision 4.
- [ ] 4.3 Add to `ImGuiMappingTests.cs`, on a 100 by 100 rect unless stated: all 0 gives `RoundCornersNone`, `NaN` and -3 give 0, `All(4)` gives 4 with four flags, `(4, 4, 0, 0)` gives `TopLeft|TopRight`, `(2, 6, 0, 0)` gives 6 with two flags, and 100 on a 40 by 20 rect gives 10. The inset: 1 gives 0, 2 gives 0.5, 4 gives 1.5. The border rounding: 8 and 2 give 7, 1 and 4 give 0. The full cover: 2 on a 4 by 10 box is true, 1 on it is false.
- [ ] 4.4 Add to `ImGuiRendererRectTests.cs`: `DrawsARoundedPanelWithABorder` (the demo: a 200 by 100 rect, radius `(8, 8, 0, 0)`, border 2. Print the two rule lines and the dump), `DrawsTheBorderAfterTheFill` (the fill vertices come before the border vertices), `KeepsTheBorderInsideTheRect` (border 3 on a square rect: each border vertex lies inside the rect grown by 1 pixel for the anti-alias fringe), `FillsTheBoxForAWideBorder` (one fill in the border color and no stroke), `DrawsASquareRectForANaNRadius` (6 elements, as phase 1), and `DrawsABorderOnlyRect`.
- [ ] 4.5 Add to `ImGuiRendererImageTests.cs`: `DrawsARoundedImage` (more than 6 elements, and the texture of the handle).
- [ ] 4.6 Add the edge tests: `BeginThrowsWithNoContext` and `MeasureTextThrowsWithNoContext` (no harness, so no current context). Run the build, the tests, and the demo. Record the demo lines in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (5):** `ImGuiMapping.cs`, `ImGuiRenderer.cs`, `ImGuiMappingTests.cs`, `ImGuiRendererRectTests.cs`, `ImGuiRendererImageTests.cs` (and `context.md`)

**Acceptance Criteria:**
- [ ] Each rule of the corner, inset, and cover functions has one passing test or more.
- [ ] No border vertex lies outside the rect grown by 1 pixel.
- [ ] The demo dump of phase 1 does not change.

---

### Phase 5: The Allocation Gate and the Record
**Goal:** Prove that the renderer allocates nothing after warm-up, prove that the gate blocks, and put the renderer rules into the design docs and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-class '*ImGuiRendererAllocationTests'` and reads a total of 3 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when a gate measures bytes above 0 and the cause is in `src/` or in the binding.

**Tasks:**

Group A (the gate, 2 files):
- [ ] 5.1 Write `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`:
  - `DrawAllocatesNothingAfterWarmUp`: a harness of 1280 by 720 (the viewport of `ThousandElementTree`), `ThousandElementTree.Build()`, one `Update`. Run 3 warm-up frames. Then run 100 frames. In each frame, read `GC.GetAllocatedBytesForCurrentThread()` right before and right after `tree.Context.Draw(renderer)`, inside `BeginFrame` and `EndFrame`, and add the difference. Assert a sum of 0.
  - `DirectCallsAllocateNothingAfterWarmUp`: register a second font and a texture. In each frame, call `Begin`, `PushClip`, `Rect` (a fill, a rounded fill with a border, and a full-cover border), `Image` (square and rounded), `Text`, `MeasureText` (no wrap and wrap), `PopClip`, and `End`. Call `MeasureText` once more after `EndFrame`, outside the frame. Measure the same way and assert 0.
  - `TheMeasuredDrawDoesRealWork`: the same tree drawn once with `RecordingRenderer` gives the count of `Rect` calls. The background list of one `ImGuiRenderer` frame holds 4 vertices or more for each of them.
- [ ] 5.2 Prove that the gate blocks. Copy `src/BrutalUi.ImGui/ImGuiRenderer.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Rect`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Run the tests again and expect 0 failed. Record both exit codes in `context.md`.
- [ ] 5.3 Run the demo class 5 times and record the result in `context.md`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.

Group B (the record, 4 files):
- [ ] 5.4 Edit `docs/technical-design.md`. Section 3: add `BrutalUi.ImGui.Tests/` to the tree and one sentence on the headless ImGui tests to the responsibilities of `tests/`. Section 8, "ImGui implementation": in this epic, `ImGuiRenderer` writes into the background list of the main viewport (Q2). A private list waits for the world-space epic. Name the two services, the explicit font and size calls (Decision 2), the corner rule and its limit (Decision 4), the border inset (Decision 5), the size and wrap rules, the viewport rule (Decision 6), and the handle errors (Decision 7). Section 14: add the headless ImGui tests and the second allocation gate to "Test types".
- [ ] 5.5 Edit `CHANGELOG.md`: add one line under `### Added`: `ImGuiRenderer`, `ImGuiFontService`, `ImGuiTextureService`, the size of `TextureHandle`, and the headless test project (imgui-renderer/imgui-draw-list).
- [ ] 5.6 Edit `docs/features/imgui-renderer/human-testing.md`: add a `## imgui-draw-list` section in the form of the `## renderer-interface` section, with the 5 phase demos and the Windows run of `scripts\test-windows.ps1`.
- [ ] 5.7 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. In `context.md`, add a "Native Dependencies" note: `dotnet test` now loads `libimgui.so` (Linux) and `imgui.dll` (Windows) from the pinned `Ahwoo.BRUTAL.ImGui` package. It is not a new dependency. Add the hand-offs of Notes. Run the build and the tests.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (5):** Group A: `ImGuiRendererAllocationTests.cs` (and a temporary edit of `ImGuiRenderer.cs`, restored). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/imgui-renderer/human-testing.md`, `context.md`.

**Acceptance Criteria:**
- [ ] The 3 tests of `ImGuiRendererAllocationTests` pass in 5 runs.
- [ ] The planted allocation gives a non-zero exit code, and the restored file is the same as the copy.
- [ ] Section 8 of the technical design states each rule of "The Mapping Table". The lint gives no hit above 2.5.

---

## Key Technical Decisions

The epic interview confirmed Q2, Q3, Q5, Q6, A1, A3, A4, A6, A8, and A9 (`epic-requirements.md`, `epic-overview.md`). This feature has no feature interview, so it holds no Confirmed Assumptions block of its own. The decisions below cite the epic labels.

### Decision 1: One renderer class and two shared services, joined by a `required` create-info
**Choice:** `ImGuiRenderer` takes `ImGuiFontService` and `ImGuiTextureService` through `CreateInfo` with two `required` members.
**Rationale:** The requirements ask for a font service, a texture service, and a create-info with `required` members. The world-space epic adds a second renderer on a private list. It must resolve the same handles, so the services live outside the renderer. A handle then has one meaning for the game.
**Alternatives:** (a) The renderer owns both services: a second renderer then has other ids for the same font. (b) One combined resource service: one class with two jobs, and the Vulkan part enters the font code.

### Decision 2: Text uses the explicit font and size calls, not `PushFont`
**Choice:** `Text` calls `ImDrawList.AddText(font, size, ...)`. `MeasureText` calls `ImFont.CalcTextSizeA(size, ...)`. Both use the dynamic font system of ImGui 1.92: `ImFont.GetFontBaked(size)` bakes each size on demand.
**Rationale:** `PushFont` and `ImGui.CalcTextSize` work on the font stack of the current frame, so a measure from `UiContext.Update` outside a frame is not safe. `PushFont` also applies the global scale factors (`style.FontScaleMain`, `style.FontScaleDpi`, per the binding doc), so the size is then not in pixels. It also needs a `PopFont` on each path. The explicit calls take the pixel size directly and need no frame. See Notes, conflict 1.
**Alternatives:** (a) `PushFont(font, size)`, `CalcTextSize`, and `PopFont`, as the requirement text names: frame-bound and scaled. (b) Bake one size per font: no dynamic sizes, against the epic.

### Decision 3: A headless test project with no GPU
**Choice:** `tests/BrutalUi.ImGui.Tests` creates an ImGui context with no backend, sets `RendererHasTextures`, runs `NewFrame` and `Render`, and reads the background list. It sets each pending texture to OK, as a null backend does.
**Rationale:** A6 forbids a unit test that needs a GPU. It does not forbid a test that needs the CPU part of ImGui. The draw list is plain CPU data, so a test proves each mapping rule and the allocation rule. CR-9 of renderer-interface asks for an allocation check for each implementation. With no test, no check guards the ImGui renderer until the GPU sample runs. Open scope question 5 asks the user to confirm this reading.
**Alternatives:** (a) No ImGui test project, with the sample as the only check: no guard for the mapping or the allocation. (b) Put the tests into `BrutalUi.Core.Tests`: the Core test project then loads ImGui.

### Decision 4: The corner rule takes the largest radius with the corner flags
**Choice:** `ResolveCorners` cleans each radius, limits it to half the shorter side, takes the largest, and sets the flag of each corner above 0. No corner above 0 gives `RoundCornersNone`.
**Rationale:** ImGui rounds with one radius and a corner mask. The rule is exact for equal radii, and for a subset of corners with one radius, which covers each style of this epic. It keeps the fill on the fast `AddRectFilled` path (4 vertices and no fringe for a square rect). It is the rule of the renderer-interface hand-off.
**Alternatives:** An exact path with four `PathArcToFast` calls and `PathFillConvex`: exact for mixed radii, but a second draw path, an anti-alias fringe on square corners, and more vertices. A later change can add it with no change of the interface (Notes, gap 5).

### Decision 5: The border stays inside the rect
**Choice:** The stroke rect is the rect inset by `borderWidth / 2 - 0.5`, the stroke radius is `max(rounding - borderWidth / 2, 0)`, and a border of half the shorter side or more becomes one fill in the border color.
**Rationale:** The contract says "inside the rect". `AddRect` strokes on the center of a path that it insets by 0.5, so a width above 1 spills out of the rect with no inset. The full-cover case avoids a stroke that crosses itself.
**Alternatives:** Pass the rect to `AddRect` unchanged: the border grows outside the layout box and overlaps the next sibling.

### Decision 6: Viewport pixels are ImGui display units, and the main viewport position is the origin
**Choice:** The renderer adds `viewport.Pos` of the main viewport to each position. It does not multiply by `scale`. `displaySize` is not used.
**Rationale:** ImGui draw lists use display units, and the backend applies `io.DisplayFramebufferScale` for the framebuffer. A3 fixes `scale` at 1. The game sets `UiContext.ViewportSize` to `io.DisplaySize`. With multi-viewports on, the main viewport position is the window position, so the origin keeps the tree on the main window. With multi-viewports off, the origin is `(0, 0)`. The binding doc of `GetBackgroundDrawList(viewport = NULL)` names "the viewport associated to the current window" for no argument, so `Begin` passes the main viewport.
**Alternatives:** (a) Multiply by `scale`: wrong for ImGui, because the backend scales again. (b) No origin: the tree moves to the screen origin with multi-viewports on.

### Decision 7: An unknown handle throws, the none handle draws nothing
**Choice:** Font id 0 is the default font. Texture id 0 draws nothing (the contract). Another unknown or removed id throws `ArgumentOutOfRangeException`. Ids never repeat.
**Rationale:** A stale handle is a bug in the game. A throw shows it at once, and it allocates on the error path only. Ids that never repeat stop a stale handle from drawing a new texture.
**Alternatives:** Fall back to the default font or to no texture: no frame stops, but the bug hides.

### Decision 8: The raw binding calls with pinned spans
**Choice:** When task 1.1 finds the `PInvoke` members public, the renderer calls them with `fixed` pointers and `text_end`. Else it uses the extension methods.
**Rationale:** The extension methods take `ImString`, an arena string with a life of one frame. `MeasureText` runs outside a frame, so an arena that resets in `NewFrame` can grow with each measure. A pinned span allocates nothing and needs no zero byte.
**Alternatives:** The `ImString` wrappers in all calls: simpler code, an unknown arena rule outside a frame.

### Decision 9: The plan assumes option (a) of CR-2, isolated in one task group
**Choice:** `TextureHandle` gets `int2 Size`. The texture service returns a handle with its size. Group A of phase 3 holds the change.
**Rationale:** `Image` then sizes itself with no renderer route (CR-1 stays a text question only). The service keeps `GetSize` for each option, so options (b) and (c) cost one group swap (Notes, "Hand-offs").
**Alternatives:** Options (b) and (c) of open scope question 2.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| 1. The `PInvoke` members are internal, or `ImDrawListPtr` exposes no raw pointer. | Medium | Medium | Task 1.1 checks it first. Fallback: the extension methods with `ImString` (Decision 8). The allocation gate of phase 5 then checks the arena. |
| 2. `CalcTextSizeA` before the first `NewFrame` fails, because the atlas builder or the font loader starts in `NewFrame`. | Medium | High | Task 1.1 runs the call in a scratch process, so a crash stops no test run. When it fails: `GetFont` calls `io.Fonts.AddFontDefault()` first (an `AddFont` call can start the builder), and the probe runs again. When it still fails, the doc comment states "call `MeasureText` after the first `ImGui.NewFrame`", the test of task 2.6 starts after one frame, and Notes gives the limit to label-and-image. The game calls `Update` after `NewFrame` in the frame order of section 4, so the sample still works. |
| 3. The headless context asserts on a texture with no backend. | Medium | High | The harness sets `RendererHasTextures` and moves each pending texture to OK after `Render` (as `example_null` and imgui issue 9525 do). Task 1.1 runs one frame in a scratch process first. |
| 4. A native `IM_ASSERT` stops the test host. | Low | Medium | The renderer checks each misuse (no `Begin`, depth 0, an empty span, an unknown id) before the native call. A stopped host is a failed run, not a false pass. |
| 5. The test project runs tests in parallel and two tests share the global ImGui context. | High (if not set) | High | `AssemblyInfo.cs` turns off parallel tests (task 1.3). Each test owns one harness. |
| 6. `AddRectFilled` or the clip commands split in another way than the expected dump. | Medium | Low | Task 1.10 records the real dump and changes the expected lines only when each value still matches. |
| 7. The `VulkanBackend.AddTexture` signature or return value differs from the feasibility report. | Medium | Medium | Task 1.1 records the signature. `AddVulkan` adapts to it. hello-panel-sample runs the GPU path. |
| 8. The sample descriptor pool (size 8) runs out of sets. | Low | Medium | Each `AddTexture` takes one set, and the font atlas takes one. hello-panel-sample registers one texture. Hand-off to hello-panel-sample. |
| 9. The `TextureHandle` change breaks a Core test. | Low | Low | The one-argument constructor stays, and equality stays on the id. Task 3.2 runs the Core tests. |
| 10. Glyph baking in a measured frame allocates managed memory. | Low | High | The warm-up frames use each font and size of the measured frames. Baking uses native memory. The gate finds any managed byte. |
| 11. The wrap rule of ImGui differs from the plan text (the break characters, the cut of a long word). | Low | Low | The plan text comes from the ImGui source of `CalcWordWrapPosition` from memory. Task 2.6 records the real result and asserts it. The demo of phase 2 uses a wrap width (70) that breaks only at the blank. |

---

## Dependencies

### Internal Dependencies
- `imgui-renderer/renderer-interface` (done): `IUiRenderer`, `Rgba8`, `Corners`, `FontHandle`, `TextureHandle`, `UiContext.Draw`, `RecordingRenderer`, and the paint walk.
- `core-tree/allocation-guard` (done): `ThousandElementTree` in `tests/BrutalUi.Core.Tests/Shared/`, linked as the benchmark project links it.
- `dev-ops` (done): the `BrutalUi.ImGui` project, the pins, the solution, and both test scripts.

### External Dependencies
- `Ahwoo.BRUTAL.ImGui` 2026.10.2 (Dear ImGui 1.92.2 docking, `Brutal.ImGuiApi`) and `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2 (`VulkanBackend`, which brings `Ahwoo.BRUTAL.Vulkan.Abstractions`). Both are pinned. No new package and no new native dependency (A9).
- `xunit.v3` 4.0.2, pinned.

### Downstream Consumers
- label-and-image: calls `MeasureText`, `Text`, and `Image`, and reads `TextureHandle.Size` (option a).
- hello-panel-sample: creates the services and the renderer, registers the procedural texture with `AddVulkan`, and calls `Draw` in the frame loop.
- The world-space epic: a second renderer on a private list, with the same services.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. `ImGuiRenderer` implements `IUiRenderer` and has a nested `CreateInfo` struct with the two `required` members `Fonts` and `Textures`. `Begin` reads the background list of the main viewport.
- [ ] F2. The demo `DrawsThreeElementTreeWithOneClip` asserts and prints the two command lines of phase 1.
- [ ] F3. `ImGuiRendererTextTests` covers one line, twice the size, the wrap, the no-wrap values, the cut of a long word, the empty text, the default size, the measure outside a frame, the draw inside the measured box, and the unknown font.
- [ ] F4. `ImGuiFontServiceTests` covers the default font of id 0, the ids from 1, the unknown id, and the null font.
- [ ] F5. `ImGuiTextureServiceTests` and `ImGuiRendererImageTests` cover the size, the remove, the none handle, the UV corners, the tint, and the texture of the image command.
- [ ] F6. `ImGuiMappingTests` covers the color cast, the area rule, the corner rule, the border inset, the border rounding, the full cover, the size rule, the wrap rule, and the width rounding.
- [ ] F7. `TextureHandle` has `Size` and the two-argument constructor (CR-2 option a), or `context.md` records the option that the user chose and its swap.
- [ ] F8. `DrawAllocatesNothingAfterWarmUp` and `DirectCallsAllocateNothingAfterWarmUp` pass. A planted allocation in `Rect` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with more tests than the baseline in `context.md`.
- [ ] Q3. No file of `tests/BrutalUi.ImGui.Tests/` creates a `VulkanBackend`, a `GlfwWindow`, or a Vulkan device.
- [ ] Q4. The renderer calls no `PushFont` and no `CalcTextSize` of the `ImGui` class.
- [ ] Q5. Each public member of `src/BrutalUi.ImGui/` has an XML doc comment (the build enforces it).
- [ ] Q6. The changed docs use ASD-STE100 prose. The lint gives no hit above 2.5.
- [ ] Q7. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows, and the new test project runs there.

### Integration Criteria
- [ ] I1. `BrutalUi.ImGui.csproj` holds 2 `PackageReference` items. `BrutalUi.Core.csproj` holds 2 and no ImGui text.
- [ ] I2. `Directory.Packages.props`, `scripts/`, and `samples/` do not change.
- [ ] I3. Each existing test of `BrutalUi.Core.Tests` passes, with no change of a constant.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line, so the HelloPanel smoke run still passes.
- [ ] I5. Section 8 of `docs/technical-design.md` states the renderer rules, and `CHANGELOG.md` holds the Added line.

### Verification Method

Run steps 1 to 10 from `/home/aaronstatic/dev/brutal-ui` on Linux. Step 11 is MANUAL.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1, Q5).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and a total above the baseline of `context.md` (Q2, I3).
3. `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on 2>&1 | tail -n 20`. Expect the two command lines of phase 1 and `failed: 0` (F1, F2).
4. Run the same command three times, with `'*MeasuresAndDrawsText'`, `'*DrawsAnImageWithItsTexture'`, and `'*DrawsARoundedPanelWithABorder'` as the filter. Expect the lines of the demos of phases 2, 3, and 4 and `failed: 0` each time.
5. `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj 2>&1 | tail -n 5`. Expect `failed: 0`. Read the test names of the test files and match them to F3 to F6.
6. `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-class '*ImGuiRendererAllocationTests' 2>&1 | tail -n 5`. Expect a total of 3 and `failed: 0` (F8).
7. Copy `src/BrutalUi.ImGui/ImGuiRenderer.cs` to a scratch folder. Add `GC.KeepAlive(new object());` at the start of `Rect`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0 (F8). Restore the file from the copy, and run step 2 again. Expect 0 failed.
8. `grep -rnE 'new VulkanBackend|GlfwWindow|CreateDevice' tests/BrutalUi.ImGui.Tests --include=*.cs` and `grep -rnE 'PushFont|CalcTextSize\(' src/BrutalUi.ImGui --include=*.cs`. Expect no output (Q3, Q4). Read `src/BrutalUi.Core/Rendering/RenderHandles.cs` or the CR-2 entry of `context.md` (F7).
9. `grep -c PackageReference src/BrutalUi.ImGui/BrutalUi.ImGui.csproj src/BrutalUi.Core/BrutalUi.Core.csproj; grep -rn 'using .*ImGui' src/BrutalUi.Core --include=*.cs; git diff main --stat -- Directory.Packages.props scripts/ samples/`. Expect `2` for each project, no output, and no output (I1, I2).
10. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4). Read section 8 of `docs/technical-design.md` and the Added list of `CHANGELOG.md` (I5). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).
11. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0 (Q7).

---

## Testing Strategy

### Unit Tests (no native call)
- `ImGuiMappingTests`: each pure rule of `ImGuiMapping`, with exact values.

### Headless ImGui Tests (native `libimgui`, no GPU)
- `ImGuiRendererRectTests`, `ImGuiRendererTextTests`, `ImGuiRendererImageTests`: each test owns a `HeadlessImGui`, runs one or more frames, and reads the background list through `DescribeBackgroundList` or the raw buffers. Four demo tests print their dump.
- `ImGuiFontServiceTests`, `ImGuiTextureServiceTests`: the tables and the errors. `AddVulkan` is tested for its argument check only.

### Allocation Test
- `ImGuiRendererAllocationTests` (phase 5): `UiContext.Draw` on the warm 1000-element tree, and the direct calls of each member, including `MeasureText` outside a frame. `TheMeasuredDrawDoesRealWork` proves that the measured draw fills the list.

### Integration Tests
- `scripts/test-linux.sh` runs the new project through `dotnet test` and the unchanged HelloPanel smoke run. Verification step 7 proves that the gate blocks.
- The GPU path (`VulkanBackend.AddTexture`, the real upload of glyphs, and the pixels) waits for hello-panel-sample (A6).

### Manual Testing
- (MANUAL) `scripts\test-windows.ps1` on Windows.

---

## Related Documentation

- **Requirements:** `docs/features/imgui-renderer/imgui-draw-list/requirements.md`
- **Epic:** `docs/features/imgui-renderer/epic-overview.md`, `docs/features/imgui-renderer/epic-requirements.md`
- **Previous feature:** `docs/features/imgui-renderer/renderer-interface/implementation.md`, `context.md` (hand-offs, open questions), `code-review.md` (CR-1, CR-2, CR-9)
- **Siblings:** `docs/features/imgui-renderer/label-and-image/requirements.md`, `docs/features/imgui-renderer/hello-panel-sample/requirements.md`
- **Design:** `docs/technical-design.md` sections 3, 4, 8, 12, 13, and 14. `docs/feasibility.md` (the ImGui binding and `VulkanBackend.AddTexture`). `docs/mission-statement.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`, `.editorconfig` (the `using` placement)
- **Code patterns:** `src/BrutalUi.Core/Rendering/IUiRenderer.cs` (the contract), `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs`, `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs` (the demo tree), `tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj` (the `Shared/*.cs` link), `samples/HelloPanel/Program.cs` (the ImGui context and the backend order)
- **Binding docs:** `~/.nuget/packages/ahwoo.brutal.imgui/2026.10.2/lib/net10.0/Brutal.ImGui.xml` (`ImDrawListExtensions`, `ImFontExtensions.CalcTextSizeA`, `ImGui.PInvoke`, `ImTextureRef`, the `ImGui.PushFont` remarks)
- **External:** [Dear ImGui example_null (1.92)](https://skia.googlesource.com/external/github.com/ocornut/imgui/+/17b50dcdd3b1cd5935653663c7bbff090a13370a/examples/example_null/main.cpp), [imgui issue 9525: font textures in headless builds](https://upd.dev/ocornut/imgui/issues/9525), [imgui issue 8745: ImTextureID_Invalid](https://upd.dev/ocornut/imgui/issues/8745)

---

## Notes

### Conflicts and Gaps

1. **`PushFont(font, size)` in the requirements.** The requirement names `PushFont` for `Text` and `MeasureText`. The plan uses the explicit font and size calls of the same dynamic font system (Decision 2), because `PushFont` is frame-bound and applies the global scale factors. The result meets the intent: each size bakes on demand, and no FreeType pin enters (Q3). Verification step 8 checks that no `PushFont` call exists.
2. **Section 8 of the technical design.** It says that `ImGuiRenderer` writes into a private list by default, and into the background list on request. Q2 puts the background list in this epic. Task 5.4 changes the paragraph.
3. **The `scale` and `displaySize` arguments.** The renderer uses neither (Decision 6). The game must set `UiContext.ViewportSize` to `io.DisplaySize`. A later epic that sets a scale must define the unit again.
4. **External unknowns.** The planner read the binding XML docs, but it did not run the binding. Task 1.1 settles these unknowns on the real package: the visibility of `ImGui.PInvoke`, the raw pointer of `ImDrawListPtr` and `ImFontPtr`, the `VulkanBackend.AddTexture` and `RemoveTexture` signatures, the texture status members, the `ImDrawFlags` names, and `CalcTextSizeA` before the first frame. The main thread does not need to run research for them, because the probe answers each one.
5. **Mixed corner radii.** Decision 4 draws mixed radii above 0 with the largest one. The contract names four radii. The class remarks and section 8 state the limit. An exact path is a later change.
6. **`Text` has no wrap width.** `MeasureText` can wrap, but `IUiRenderer.Text` takes no wrap width. A wrapped `Label` cannot draw its lines through ImGui with the same breaks. Open scope question 3 asks for the route. The private `DrawText` takes a wrap width already (task 2.3).
7. **A clip with an area of 0.** The renderer still draws into a command with an empty clip. The Vulkan backend skips such a command. A later change can skip the draw calls while the clip is empty.
8. **No font file loader.** The service registers fonts of `io.Fonts` and loads no file. The game loads a TTF with `io.Fonts.AddFontFromFileTTF` (the default loader, Q3). A test of a file loader needs a font file in the repository, and no feature of this epic needs one.
9. **The epic text "an `ImFontPtr` and a size".** The service maps a handle to an `ImFontPtr`. The size comes with each call, and the size rule takes the `LegacySize` of the font for a size of 0 or less.
10. **A native dependency note.** `AGENTS.md` asks for a note for each native dependency. The test project loads `libimgui` from a pinned package, which the sample loads already. Task 5.7 records it.

### Open Scope Questions

1. **The demo of phase 1 (rule 12).** Recommended first.
   - (a) **Recommended.** `**Demo:** the developer runs dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on and reads two command lines: the root fill under the viewport clip, then the two child fills under the clip 2,2,198,98.` Reason: it runs on each machine with no GPU and checks each value.
   - (b) `**Demo:** the developer runs the HelloPanel sample and sees the three rects in the window (MANUAL).` Reason against: it changes the sample, which hello-panel-sample owns.
   - (c) `**Demo:** the developer runs the test class and reads failed: 0.` Reason against: a person sees no value.
2. **The texture size (CR-2).** The plan assumes (a).
   - (a) **Recommended.** `TextureHandle` gets `int2 Size`, and the texture service returns a handle with its size. Reason: `Image` sizes itself with no renderer route, and the change is one small struct edit.
   - (b) `Image` takes an explicit natural size from the caller, and the handle does not change. Reason against: the caller repeats a value that the service knows.
   - (c) `IUiRenderer` gets `int2 GetTextureSize(TextureHandle texture)`. Reason against: `Image` then needs a renderer during `Update`, the same route problem as CR-1.
3. **Wrapped text draw (new, from gap 6).** The plan works with each option.
   - (a) **Recommended.** `IUiRenderer.Text` gets a `float wrapWidth` parameter. Reason: the measure and the draw then use the same ImGui wrap, so the lines always match.
   - (b) `Label` breaks the lines itself with no-wrap `MeasureText` calls and draws each line with `Text`. Reason against: more label code, and a second wrap rule beside the ImGui rule.
   - (c) `IUiRenderer` gets a wrap position query (ImGui has `ImFont.CalcWordWrapPosition`). Reason against: a third text member for one caller.
4. **The measure route (CR-1).** label-and-image owns the answer. This plan builds no route. `MeasureText` works outside a frame for each option (task 2.6).
   - (a) **Recommended.** A measurer slot on `UiContext`, set at creation, that the label reads during `Update`. Reason: section 13 already puts the renderer into `UiContext.CreateInfo`.
   - (b) A measurer reference on each label at creation. Reason against: each label stores one more reference, and markup must supply it.
   - (c) A text measure interface in `BrutalUi.Core` that `ImGuiRenderer` also implements. Reason against: one more public type. It still needs (a) or (b) to reach the label.
5. **The headless test project (the reading of A6).** The plan assumes (a).
   - (a) **Recommended.** `tests/BrutalUi.ImGui.Tests` with a headless ImGui context and no GPU. Reason: it checks each mapping and the allocation rule, and A6 forbids a GPU only.
   - (b) No ImGui test project. The sample is the only check. Reason against: no guard for the mapping or the allocation of this renderer (CR-9).
   - (c) The headless tests in `BrutalUi.Core.Tests` with a reference to `BrutalUi.ImGui`. Reason against: the Core test project then loads ImGui.

### Hand-offs

- **label-and-image:**
  - `MeasureText` rules: the width rounds up to a whole pixel, the height is the line count times the size, and empty text gives `(0, size)`. ImGui breaks after a blank or after `. , ; ! ? "`, and it cuts a word that is wider than the wrap width. A wrap width of 0 or less, or `NaN`, means no wrap. A size of 0 or less uses the default size of the font. It works outside a frame, and it needs a current ImGui context (or the limit of Risk 2, when task 1.1 finds one).
  - Font id 0 is the ImGui default font. An unknown id throws.
  - With option (a) of question 2, `Image` reads `handle.Size` for its natural size.
  - `Text` draws one line with no wrap until question 3 has an answer.
  - The `RecordingRenderer` measure rule (`length * size * 0.5`, `size`) differs from ImGui. The label tests in `BrutalUi.Core.Tests` stay on the recording renderer. A label test in `BrutalUi.ImGui.Tests` can use the real measure.
- **hello-panel-sample:**
  - Create order: `ImGui.CreateContext`, then the services and the renderer, then the backends. Register the procedural texture with `AddVulkan` after the upload.
  - Call `UiContext.Update` and `UiContext.Draw(renderer)` between `ImGui.NewFrame` and `ImGui.Render`. Set `UiContext.ViewportSize` to `io.DisplaySize`.
  - The descriptor pool of the sample has 8 sets. Each `AddVulkan` takes one, and the font atlas takes one.
  - Call `Remove` after `vulkan.WaitIdle()` and before the backend disposes, or skip it at the end of the run.
  - The GPU check of this renderer is the visual check of the sample: rounded corners, the border inside the box, the text, and the image (MANUAL).
- **The world-space epic:** a private list needs a second draw-list source. `Begin` reads the list in one place, so a create-info option or a subclass can supply another list. Share the two services.
- **The swap for question 2:** for (b), drop group A of phase 3 and keep `GetSize` for the caller. For (c), replace group A with an `int2 GetTextureSize(TextureHandle)` member on `IUiRenderer`, implement it in `RecordingRenderer` and in `ImGuiRenderer` through `Textures.GetSize`, and add a walk-free test.

### Prose lint

The planner had no shell, so it did not run `ste-lint.py` on this file. Task 5.7 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
