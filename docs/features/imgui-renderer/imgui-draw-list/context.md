# imgui-renderer/imgui-draw-list - Context & Decisions

**Last Updated:** 2026-10-20 18:13
**Current Phase:** Done (phases 1 to 5 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Implementation plan (`implementation.md`), accepted. The user answered the open scope questions on 2026-10-20: 2 (a), 3 (a), 4 (a), 5 (a). Question 1 uses demo (a).
- ✅ Dev docs scaffolded (`context.md`, `tasks.md`, `inbox.md`)

- ✅ Phase 1 group A (tasks 1.1 to 1.5): the binding probe, the test project, `AssemblyInfo.cs`, `InternalsVisibleTo`, and the harness. The call form is the raw `PInvoke` members. The total is 632, 0 failed.

- ✅ Phase 1 group B (tasks 1.6 to 1.11): the mapping, the service shells, the renderer, and the rect and clip tests. The demo prints the two expected lines. The total is 667, 0 failed.
- ✅ Phase 1 verifier `phase-1-verify.md`: PASS (667 tests, 0 failed, 0 warnings, 2 package references in each project, no `VulkanBackend` in the tests, no diff in `Directory.Packages.props`, `scripts/`, or `samples/`). Demo: PASS.

- ✅ Phase 2 group 1 (tasks 2.1, 2.2, 2.4, and 2.5): the font service, the three text rules, and their tests. The total is 699, 0 failed.

- ✅ Phase 2 group 2 (task 2.3): `Text`, `MeasureText`, and the `wrapWidth` parameter of `IUiRenderer.Text`. The total is 703, 0 failed.

- ✅ Phase 2 group 3 (tasks 2.6 to 2.9): the renderer text tests and the demo. The total is 750, 0 failed.

- ✅ Phase 2 verifier `phase-2-verify.md`: PASS (750 tests, 0 failed, 0 warnings, no `PushFont` or `CalcTextSize(` in `src/BrutalUi.ImGui`, `wrapWidth` in both `Text` lines). Demo: PASS (`112x13`, `224x26`, `63x26`, glyphs at `11,12-119,23`).

- ✅ Phase 3 group 1 (tasks 3.1 and 3.2): `TextureHandle` gets `int2 Size`. The total is 753, 0 failed.

- ✅ Phase 3 group 2 (tasks 3.3 to 3.7): the texture service, `Image` with `AddImage`, and their tests. The demo prints `size 64x32`, a `tex id 4660` command, and a `tex atlas` command. The total is 786, 0 failed.

- ✅ Phase 3 (tasks 3.1 to 3.7): the size of `TextureHandle`, the texture service, `Image` with `AddImage`, and their tests. Verifier `phase-3-verify.md`: PASS (786 tests, 0 failed, 0 warnings, no `NotSupportedException` left in the renderer). Demo: PASS (`size 64x32`, `tex id 4660` at `20,20-84,52`, then `tex atlas`).

- ✅ Phase 4 (tasks 4.1 to 4.6): corners, borders, the rounded image, and the edge tests. The run stopped part-way in phase 4, and a resumed run finished it with the brief `phase-4-resume.md`. Verifier `phase-4-verify.md`: PASS (824 tests, 0 failed, 0 warnings, the phase 1 dump does not change, no debug code left). Demo: PASS (the two rule lines, then `elems 144` with the vertex bounds `-1.0003166,-1.000226-201.00032,101.00049`, inside the rect grown by 1 pixel with a float slack of 0.01 pixel).

- ✅ Phase 5 (tasks 5.1 to 5.7): the allocation gate (0 bytes for the tree draw and the direct calls over 100 frames, the planted allocation gives exit 2), the design doc, the changelog, the Windows run line, and the native dependency note. Verifier `phase-5-verify.md`: PASS (827 tests, 0 failed, 0 warnings, lint exit 0 on each changed doc, section 8 matches the source). Demo: PASS (`total: 3`, `failed: 0`).

- ✅ Cross-phase review (`code-review.md`): 10 findings (1 HIGH, 2 MEDIUM, 7 LOW). Review groups 1 to 6 fixed each finding. CR-10 came from review group 1. Two optional parts stay out of scope with a reason (CR-1 fix 2, CR-3 fix 3). Verifier `review-verify.md`: PASS (864 tests, 0 failed, 0 warnings, `scripts/test-linux.sh` exit 0, the phase 1 and phase 4 demos do not change, the gate total is 3).

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification", then `/autorun-epic` commits the feature and goes on to label-and-image.

**Blockers:**
- none

---

## Key Files

### Core Implementation
- `src/BrutalUi.ImGui/ImGuiRenderer.cs` - `IUiRenderer` on the background draw list of the main viewport
- `src/BrutalUi.ImGui/ImGuiMapping.cs` - The pure mapping rules (internal static)
- `src/BrutalUi.ImGui/ImGuiFontService.cs` - `FontHandle` to `ImFontPtr`
- `src/BrutalUi.ImGui/ImGuiTextureService.cs` - `TextureHandle` to `ImTextureRef` and a size
- `src/BrutalUi.Core/Rendering/RenderHandles.cs` - `TextureHandle` gets a size (phase 3)
- `src/BrutalUi.Core/Rendering/IUiRenderer.cs` - `Text` gets a `wrapWidth` parameter (phase 2, task 2.3)

### Related Files
- `tests/BrutalUi.ImGui.Tests/` - The headless ImGui test project (new)
- `docs/features/imgui-renderer/imgui-draw-list/implementation.md` - Implementation plan

---

## Important Decisions

The plan holds decisions 1 to 9. This section records the decisions made during the build.

- The run is the called mode of `/autorun-feature`, from `/autorun-epic`. The orchestrator makes no git write. The caller commits.
- The user answers of 2026-10-20: `TextureHandle` gets `int2 Size` (question 2 a). `IUiRenderer.Text` gets a `float wrapWidth` parameter, in task 2.3 (question 3 a). label-and-image builds a measurer slot on `UiContext` (question 4 a). The headless test project `tests/BrutalUi.ImGui.Tests` is in scope (question 5 a). Phase 1 uses demo (a).
- **The resumed run (2026-10-20).** Context: the user paused the run part-way in phase 4, so the phase 4 files can be half-written. Decision: the new orchestrator wrote a resume brief (`phase-4-resume.md`) that checks each task first and finishes only the open work. Rationale: the code in `src/` was right, and a redo would waste the work. Result: six tests had wrong expected values, and the rect tests held debug code. No change in `src/`.
- **The fill under an opaque covering border (kept).** Context: the plan row says that the fill draws when its alpha is above 0. Decision: `Rect` skips the fill when an opaque border (alpha 255) covers the box. Rationale: the border fill hides the fill, so the skip saves one draw with no visible change. A translucent covering border still draws the fill first.
- **The review scope (2026-10-20).** Context: two fixes of the cross-phase review reach outside this feature or change a design rule. Decision: CR-1 fix (2), a `catch` in `UiContext.Draw` that calls `End`, and CR-3 fix (3), an atlas check in `ImGuiTextureService.Add`, are out of scope. Rationale: renderer-interface owns `UiContext.Draw` and the frame contract. The service reads no ImGui state, so each member works with no context. The `PopClip` guard closes the native write of CR-1, and the doc comments state the rule of CR-3.
- **A non-finite clip clips everything (review group 1).** Context: a `NaN` or an infinite clip reached the Vulkan backend as a `NaN` scissor. Decision: `PushClip` turns a non-finite clip into a clip with an area of 0, and it still pushes. Rationale: the stack stays balanced, and the rule matches the rect rule (a non-finite rect draws nothing). A clip with an infinite width gave the viewport clip before. It now clips everything.

---

## Phase 1 Group A: Baseline and Binding Probe (task 1.1)

**Baseline (before the first edit, 2026-10-20):** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives a total of 623, 0 failed (Core 621, Framework 1, Markup 1).

**The probe:** a console project in the session scratch folder (`.../scratchpad/probe/`), with `Ahwoo.BRUTAL.ImGui` 2026.10.2 and `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2. The repository does not hold it. It runs in three modes: `pre` (measure before the first frame), `nopre`, and `assert`.

**The answers:**

- (a) `Brutal.ImGuiApi.ImGui.PInvoke` is a public nested static class. All eight members are `public static`:
  - `ImDrawList_AddRectFilled(ImDrawList* target, float2* p_min, float2* p_max, uint col, float rounding, ImDrawFlags flags)`
  - `ImDrawList_AddRect(ImDrawList* target, float2* p_min, float2* p_max, uint col, float rounding, ImDrawFlags flags, float thickness)`
  - `ImDrawList_AddText_1(ImDrawList* target, ImFont* font, float font_size, float2* pos, uint col, byte* text_begin, byte* text_end, float wrap_width, float4* cpu_fine_clip_rect)`
  - `ImDrawList_AddImage(ImDrawList* target, ImTextureRef tex_ref, float2* p_min, float2* p_max, float2* uv_min, float2* uv_max, uint col)`
  - `ImDrawList_AddImageRounded(ImDrawList* target, ImTextureRef tex_ref, float2* p_min, float2* p_max, float2* uv_min, float2* uv_max, uint col, float rounding, ImDrawFlags flags)`
  - `ImDrawList_PushClipRect(ImDrawList* target, float2* clip_rect_min, float2* clip_rect_max, Bool8 intersect_with_current_clip_rect)` (a C# `bool` converts to `Bool8` with no cast)
  - `ImDrawList_PopClipRect(ImDrawList* target)`
  - `float2 ImFont_CalcTextSizeA(ImFont* target, float size, float max_width, float wrap_width, byte* text_begin, byte* text_end, byte** remaining)`
  - Each point is a `float2*`, not a `float2` value. The color is a `uint` (`ImU32`), not `ImColor8`. The texture is an `ImTextureRef` by value.
- (b) `ImDrawListPtr.Ptr` is `ImDrawList*` and `ImFontPtr.Ptr` is `ImFont*` (public get-only properties). Both structs have `IsNull()`. `ImFontPtr` also has `ref float LegacySize` and `ref ImFontAtlasPtr ContainerAtlas`.
- (c) `ImColor8` is 4 bytes with a private `byte4 _value`, a public `(byte r, byte g, byte b, byte a = 255)` constructor, and `AsUint()`. `ImTextureRef` is 16 bytes with two public fields: `ImTextureDataPtr _TexData` and `ImTextureID _TexID`. `ImTextureID` is an 8-byte record struct with `IntPtr Value { get; set; }`, a public `(IntPtr Value)` constructor, and `static Invalid`.
- (d) `public ImTextureRef VulkanBackend.AddTexture(VkSampler sampler, VkImageView imageView, VkImageLayout imageLayout)` and `public void VulkanBackend.RemoveTexture(ImTextureRef texture)`. The service stores the returned `ImTextureRef` and gives the same value back to `RemoveTexture`. No descriptor set type shows in the public API.
- (e) `ImTextureDataPtr` has `ref ImTextureStatus Status`, `ref ImTextureID TexID`, `ref int UniqueID`, `ref int Width`, `ref int Height`, and `ref bool WantDestroyNextFrame`. `ImTextureStatus` is `OK = 0`, `Destroyed = 1`, `WantCreate = 2`, `WantUpdates = 3`, `WantDestroy = 4`. `ImGuiPlatformIOPtr.Textures` is `ref ImVector<ImTextureDataPtr>` (members `Count` and an indexer).
- (f) `ImDrawFlags`: `None = 0`, `Closed = 1`, `RoundCornersTopLeft = 16`, `RoundCornersTopRight = 32`, `RoundCornersTop = 48`, `RoundCornersBottomLeft = 64`, `RoundCornersLeft = 80`, `RoundCornersBottomRight = 128`, `RoundCornersRight = 160`, `RoundCornersBottom = 192`, `RoundCornersAll = 240`, `RoundCornersDefault = 240`, `RoundCornersNone = 256`, `RoundCornersMask = 496`.
- (g) `ImGuiViewportPtr` has `ref float2 Pos`, `ref float2 Size`, `ref float2 WorkPos`, `ref float2 WorkSize`, and `ref float2 FramebufferScale`. The origin is `viewport.Pos`. The headless viewport is `Pos 0,0`, `Size 400,300`.

**Other binding facts:**

- `ImDrawListPtr` has `ref ImVector<ImDrawCmd> CmdBuffer`, `ref ImVector<ushort> IdxBuffer`, and `ref ImVector<ImDrawVert> VtxBuffer`. `ImDrawCmd` has public fields `float4 ClipRect`, `ImTextureRef TexRef`, `uint VtxOffset`, `uint IdxOffset`, and `uint ElemCount`. `ImDrawVert` has `float2 pos`, `float2 uv`, and `uint col`.
- `ImFontAtlasPtr` has `ref ImTextureRef TexRef`, `ref ImTextureDataPtr TexData`, and `ref ImVector<ImFontPtr> Fonts`. `ImGuiIOPtr` has `ref` properties `BackendFlags`, `DisplaySize`, `DeltaTime`, `IniFilename` (a `Ptr<byte>`, so `default` gives none), `Fonts`, and `FontDefault`.
- The extension forms take `ImColor8` and `ImString` (`ImFontExtensions.CalcTextSizeA(ImFontPtr, float, float, float, ImString, Ptr<byte> remaining)`, `ImDrawListExtensions.AddText(..., ImString text, ...)`). The raw form needs no `ImString`.

**The measure before the first frame (Risk 2):** a new context has no font (`io.Fonts.Fonts.Count` is 0) before the first `NewFrame`. `io.Fonts.AddFontDefault()` gives a font with `LegacySize` 13. `ImFont_CalcTextSizeA` on it before the first `NewFrame` works, with no crash. The results of `Hello, brutal-ui` are equal before the first frame, inside frame 1, after `Render`, and between frames: size 13 gives `112x13`, size 26 gives `224x26`, and size 13 at a wrap width of 70 gives `63x26`. With no `AddFontDefault` call, the first `NewFrame` adds the default font itself. So the font service path "else one `io.Fonts.AddFontDefault()` call" makes `MeasureText` work before the first frame. No limit applies.

**The frame and the textures (Risk 3):** with `RendererHasTextures`, `Render` leaves one texture (the atlas, unique id 1, 512 by 128) in `WantCreate`, with `TexID` 0. The probe sets `WantCreate` and `WantUpdates` to `OK`, and `WantDestroy` to `Destroyed`. The next frames find no pending texture, and no assert shows. The atlas `TexID` stays 0. A command of the atlas has `TexRef._TexData.Ptr` equal to `io.Fonts.TexRef._TexData.Ptr`, and `_TexID.Value` 0.

**The background list:** ImGui resets the list at the first `GetBackgroundDrawList` call of a frame, not in `NewFrame`. A read after `Render` in the same frame gives the commands of the frame. The probe drew the root fill, a clip of `2,2`-`198,98`, and two fills. The list gave two commands: `cmd 0 clip 0,0,400,300 elems 6 verts 0,0-200,100` and `cmd 1 clip 2,2,198,98 elems 12`, both on the atlas. The clip does not cut the vertices (only the GPU scissor uses it), so `verts` can extend past the clip.

**ImGui asserts:** `Render` with no `NewFrame` prints `[imgui-error] ... Forgot to call ImGui::NewFrame()?` (with `Assert=1`) and returns. The process does not stop. So a native assert does not stop the process in this build. A misuse, for example a `PopClipRect` on an empty stack, can corrupt the list with no signal. The C# checks of the renderer (the clip depth, the frame state) are the only guard.

**The allocation numbers (raw form, after a warm-up of 20 calls, inside frame 2):** 3 by 1000 `ImDrawList_AddRectFilled` calls give 0 bytes. 3 by 1000 `ImFont_CalcTextSizeA` calls (wrap 70, `fixed` span) give 0 bytes. 3 by 1000 reads of the `Begin` calls (`GetCurrentContext`, `GetMainViewport`, `GetBackgroundDrawList`, `viewport.Pos`, `GetFrameCount`) give 0 bytes. 1000 `ImFont_CalcTextSizeA` calls between two frames give 0 bytes.

**The chosen call form:** the raw `Gui.PInvoke` members with pointers (Decision 8). The `PInvoke` members are public. The renderer takes `ImDrawListPtr.Ptr` and `ImFontPtr.Ptr`, puts each point in a `float2` local and passes its address, passes the color as the `uint` of `ImGuiMapping.ToImU32`, and pins the text with `fixed` after the empty-span check. No `ImString` arena is used.

## Phase 1 Group A: Decisions (tasks 1.2 to 1.5)

- **The parallel switch (a change from the plan):** xunit.v3 4.0.2 marks `CollectionBehavior(DisableTestParallelization = true)` obsolete with an error (CS0619). `AssemblyInfo.cs` uses the replacement `[assembly: Parallelization(Mode = ParallelMode.None)]` (namespaces `Xunit.v3` and `Xunit.Sdk`). The effect is the same: no test of the assembly runs in parallel. The comment states the reason (one current ImGui context for the process).
- **The harness constructor:** `HeadlessImGui(float width = 400f, float height = 300f)`. Phase 5 uses `new HeadlessImGui(1280, 720)`. The constructor throws `InvalidOperationException` when a context is current already. Reason: `ImGui.CreateContext` keeps the old context current, so a leaked harness makes the next harness set the wrong context with no signal.
- **The texture reset:** `EndFrame` sets `WantCreate` and `WantUpdates` to `OK`, and `WantDestroy` to `Destroyed`, as a null backend does. It sets no `TexID`. The probe and the smoke tests show no error with this reset.
- **The harness members:** `Context`, `FrameOpen`, `BeginFrame`, `EndFrame`, `DescribeBackgroundList`, `Dispose`, and two internal format helpers (`Format`, `FormatColor`). `BeginFrame` and `EndFrame` throw `InvalidOperationException` out of order. `DescribeBackgroundList` throws inside an open frame, because a first access of the list in a frame resets it. `Dispose` runs `Render` for an open frame on its own context, then `DestroyContext`. A second `Dispose` does nothing. A call after `Dispose` throws `ObjectDisposedException`.
- **The dump:** the texture is `atlas` when `TexRef._TexData.Ptr` equals `io.Fonts.TexRef._TexData.Ptr`, `id N` when `_TexData` is null (`N` is `_TexID.Value`), else `data N` with the unique id of the texture (an extra form that the plan does not name). A command with 0 elements prints `verts none`. A number prints with the invariant culture, and -0 prints as 0.
- **The smoke test (`HeadlessImGuiTests.cs`):** the brief allows it when it helps prove the harness. It holds 9 tests. They draw with the raw `PInvoke` calls, so they need no renderer. `DescribesTheBackgroundList` draws the fills and the clip of the phase 1 demo by hand and gets the two expected demo lines exactly.
- **The test project:** it compiles `../BrutalUi.Core.Tests/Shared/*.cs` (`RecordingRenderer`, `ThousandElementTree`, `ChainTree`, `TextMeasure`) as linked files, in the namespace `BrutalUi.Core.Tests`. A test file of group 2 adds `using BrutalUi.Core.Tests;` to use them.

**Blast radius:** `brutal-ui.sln` gets one project under the `tests` folder (`{616E76E8-2256-453C-9616-FECEFB69B2E6}`). `BrutalUi.ImGui.csproj` gets `InternalsVisibleTo` only, and no package. No file of `src/` other than that project file changes. No other test project changes. `dotnet test --solution` now loads `libimgui.so` from the pinned `Ahwoo.BRUTAL.ImGui` package. It is not a new dependency.

**The test totals:** the baseline is 623. After group A the total is 632 (623 and the 9 harness tests), 0 failed. The build gives 0 warnings and 0 errors.

**Notes for group 2 (the renderer, the mapping, the service shells, and the rect tests):**

- Use `Gui.PInvoke.*` with `list.Ptr`, `&local` points, and the `uint` color. Pass `ImDrawFlags.RoundCornersNone` for a square fill.
- The harness dump of a frame with no draw call is one line: `cmd 0 clip 0,0,400,300 tex atlas elems 0 verts none colors`. ImGui removes a trailing empty command only when a command is before it. So `SkipsAnEmptyRect` and `SkipsATransparentFill` can assert this exact line, or assert `elems 0`.
- After a `PopClipRect` that ends the frame, ImGui removes the trailing empty command at `Render`. The demo gives two lines, not three.
- `EndPopsAnOpenClip` and `BeginClearsTheClipsOfAnAbortedFrame` draw a second time in the same ImGui frame. The list does not reset in the same frame, so the dump also holds the commands of the first pass. Assert the clip of the last command.
- The native asserts do not stop the process. A `PopClipRect` with no push does not fail in ImGui. The depth check of the renderer must come before the native call.
- The main viewport position is `0,0` in the harness. A test that proves the origin rule must set `viewport.Pos` by hand.
- For `ThrowsWithNoContext` (phase 4), create no harness. `Gui.GetCurrentContext().IsNull()` is true after each harness `Dispose`.

## Phase 1 Group B: Decisions (tasks 1.6 to 1.11)

**The demo dump (task 1.11, `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on`):**

```
cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 0,0-200,100 colors #0A141EFF
cmd 1 clip 2,2,198,98 tex atlas elems 12 verts 12,12-312,52 colors #FF0000FF #00FF00FF
```

ImGui gives the two expected lines. The test asserts them with no change.

**The test totals:** after group B the total is 667, 0 failed (Core 621, Framework 1, Markup 1, ImGui 44). The ImGui project holds 9 harness tests, 16 mapping tests, and 19 rect tests. No test of the ImGui project logs an `[imgui-error]` line. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors.

**The decisions:**

- **The base clip of the list (a fact that the plan does not name):** ImGui pushes the viewport clip when it resets the background list. So `_ClipRectStack.Count` is 1 with no clip of the renderer open, not 0. The tests assert `ViewportClipCount` (1).
- **The pop guard (Risk 4):** `PopOpenClips` pops `Math.Min(depth, list._ClipRectStack.Count)` clips. A pop on an empty ImGui stack reads out of bounds with no signal. A pop to size 0 is safe in ImGui (the clip falls back to `ClipRectFullscreen`). The guard is for memory safety only. It does not fix a wrong depth.
- **The `Begin` contract:** `Begin` pops only when the depth is above 0, the new list pointer equals the stored pointer, and the frame count equals the stored count. A new ImGui frame resets the list, so a pop there corrupts it. `Begin` reads only the list that it gets from ImGui in the same call, so it never reads a freed list.
- **`End` checks the frame (an edge that the plan does not name):** `End` with no open frame does nothing, so `End` never hides an exception of `Begin`. With an open frame, `End` pops only when a context is current, the frame count is the same, and `GetBackgroundDrawList(GetMainViewport())` gives the stored pointer. The frame count check runs before the list call, because a first list access in a new frame resets the list. With no context, `End` makes no native call. The other three P/Invoke calls run once for each frame and allocate nothing.
- **`HasArea` (a change from "The Pure Rules"):** the rule also needs a finite width and a finite height. Reason: an infinite size gives infinite vertex positions, and ImGui does not cut vertices at the clip. The plan rule needs finite `X` and `Y` only. The tests cover `+Infinity` in each component.
- **`Rect` order:** the frame check runs first, so an empty rect before `Begin` also throws. Then the area check, then the fill. Phase 4 adds the border after the fill, inside the same area check.
- **`PopClip` order:** the depth check runs before the native call. The depth goes down before the call.
- **The constructor:** `ArgumentNullException.ThrowIfNull(info.Fonts, "info.Fonts")`. The parameter name tells which service is null.
- **The service shells:** each has an empty public constructor. The constructors read no ImGui state, so a test can create them with no context (`ThrowsForANullService`).
- **Extra tests (inside the brief files):** `AddsTheMainViewportPosition` (it sets `viewport.Pos` to `100,50` after `NewFrame`, and proves Decision 6: clip `102,52,298,148`, verts `110,60-130,80`), `BeginInANewImGuiFramePopsNothing`, `EndInALaterImGuiFramePopsNothing`, and `EndWithNoFrameDoesNothing`. No later task of `tasks.md` tests the origin rule.

**Blast radius:** four new files in `src/BrutalUi.ImGui/` and two new test files. No file of `BrutalUi.Core` changes. No package changes. `IUiRenderer` does not change.

**Notes for phase 2 (text and measure):**

- `Text` and `MeasureText` throw `NotSupportedException` with the constant `NotYetSupported`. Remove the constant when phase 3 replaces `Image`.
- `Text` must call `ThrowIfNoFrame()` first, as `Rect` does. `MeasureText` must not call it, because it runs outside a frame. `MeasureText` checks `Gui.GetCurrentContext().IsNull()` itself.
- The origin is `_origin`. `Text` adds it to `pos`, as `Rect` does.
- Task 2.3 adds `wrapWidth` to `IUiRenderer.Text`. The current `Text` signature in `ImGuiRenderer` has no `wrapWidth`, so the build breaks at the interface change until `ImGuiRenderer.Text` gets the parameter.
- The class is `unsafe`, so `fixed` and `&local` need no extra modifier.

**Open questions (no change made):**

- `Begin` outside an ImGui frame (before `NewFrame` or after `Render`) does not throw. The draw then goes to a list that ImGui resets at the next access, so the UI is lost with no signal. A check needs `g.WithinFrameScope` or a similar read. It changes the public contract, so it waits for a decision.
- A draw call between `Begin` and `End` does not check the context, for speed. A context destroyed inside a frame gives a dangling list. The class summary states one context for each process.

## Phase 2 Group 1: Decisions (tasks 2.1, 2.2, 2.4, and 2.5)

**The binding facts (from a reflection run of the probe, mode `fonts`):** `ImFontPtr` has a public constructor `ImFontPtr(ImFont*)`, `Ptr`, and `IsNull()`. `ImFontAtlasPtr` has `Ptr` (`ImFontAtlas*`) and `IsNull()`. `io.FontDefault` is `ref ImFontPtr`. `io.Fonts.Fonts` is `ref ImVector<ImFontPtr>` with `Count` and an indexer. The raw call `Gui.PInvoke.ImFontAtlas_AddFontDefault(ImFontAtlas*, ImFontConfig*)` gives `ImFont*`. The binding exposes no public constructor of `ImFontAtlas`.

**The decisions:**

- **The check order of `Add`:** the null check, then the context check, then the atlas check (`font.ContainerAtlas.Ptr != io.Fonts.Ptr`). The null check needs no native call. The context check comes before the service reads the font, so a font pointer with no context is never read.
- **The check order of `GetFont`:** the context check, then the id check, for each id (not for id 0 only). The renderer then never gets a font with no current context. The cost is one `GetCurrentContext` P/Invoke for each call. The probe measured 0 bytes for it.
- **The id check:** `(uint)id > (uint)_count` covers a negative id and an id above the last id in one compare. The exception has the parameter name `font` and the id as the actual value. A `[DoesNotReturn]` helper throws it, so the success path stays small.
- **The default loader result:** a null result of `ImFontAtlas_AddFontDefault` throws `InvalidOperationException`. The plan names no rule for it. The method then never gives a null font.
- **The same font two times:** `Add` gives a new id for each call, also for the same font. The plan says that ids never repeat. It names no duplicate check.
- **The table growth:** `Add` grows the table when the next id equals the length, so index 0 stays unused. A test adds 9 fonts (two growths).
- **`WrapWidth` (a change from "The Pure Rules"):** the rule also maps an infinite width (positive and negative) to 0. Reason: an infinite width wraps no line, so 0 gives the same lines, and ImGui then runs no wrap code on an infinite value. The plan rule maps only a width of 0 or less, and `NaN`. `HasArea` of phase 1 has the same finite check.
- **`FontSize`:** negative infinity also gives the default, because the rule needs a finite size above 0.
- **`RoundUpWidth` (a fact that the plan does not name):** the rule `MathF.Floor(width + 0.99999f)` is the rule of `ImGui::CalcTextSize`. In single precision, the sum rounds to the next whole number at 256 and above. So a whole width of 256 or more gives the width plus 1 (256 gives 257). A whole width of 255 or less stays. The doc comment states it, and the test `RoundUpWidthMatchesImGuiForAWideWholeWidth` pins it. The rule stays, because it matches the ImGui result.
- **Extra tests (inside the brief files):** `UsesTheFirstAtlasFontWithNoIoDefault`, `GrowsTheTableAndNeverRepeatsAnId`, `ThrowsForIdOneWithNoRegisteredFont`, `ThrowsForAFontOfAnotherAtlas` (a second context, made current for one `AddFontDefault` call, then destroyed in a `finally` block), and `GivesTheSameDefaultFontOutsideAndInsideAFrame`. `ThrowsWithNoContext` passes a fake pointer `(ImFont*)16` to `Add`. The context check throws before the service reads it.

**The test totals:** 699, 0 failed (Core 621, Framework 1, Markup 1, ImGui 76). The ImGui project now holds 37 mapping tests and 11 font service tests. No test of the ImGui project logs an `[imgui-error]` line. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors.

**Blast radius:** `ImGuiFontService.cs` and `ImGuiMapping.cs` change. `ImGuiRenderer` does not call the new members yet. No file of `BrutalUi.Core` changes. No package changes.

**Notes for group 2 (`Text`, `MeasureText`, and the `wrapWidth` parameter):**

- Call `Fonts.GetFont(font)` once for each call. It checks the context, so `MeasureText` gets an `InvalidOperationException` with no context from it. Call it after the empty-text check only when the empty text must work with no context. The plan does not say. Choose one order and record it.
- `FontSize(size, font.LegacySize)` gives the size. A font that a game loads with a size of 0 can have a `LegacySize` of 0. Then the size is 0. Check the ImGui result for a size of 0 (`CalcTextSizeA` and `AddText_1`), and record it.
- `WrapWidth` maps an infinite width to 0 (see above). Use it in `Text` and in `MeasureText`, so both give the same lines.
- `RoundUpWidth` adds 1 to a whole width of 256 or more. A test that compares a measured width with a vertex bound must allow it.
- The font table holds `ImFontPtr` values, not handles to a context. A font of a destroyed context is a dangling pointer. The class summary states one context for each process.

**Open questions (no change made):**

- `GetFont` does not check that a registered font still belongs to the current atlas. A game that calls `io.Fonts.Clear()` or `ClearFonts()` after `Add` gets a dangling font. A check costs a read of the font for each call. The plan names no such rule.

## Phase 2 Group 2: Decisions (task 2.3)

**The size-0 check (a temporary test in `tests/BrutalUi.ImGui.Tests`, removed before the end of the group):**

- `ImFont_CalcTextSizeA` at size 0 gives `0x0`, before the first frame and inside a frame. It does not crash.
- `ImFont_CalcTextSizeA` at size -5 stops the test process with a segmentation fault (exit code 139). No `[imgui-error]` line shows.
- `ImDrawList_AddText_1` at size 0 draws the text at the font size of the frame (13), not at size 0. So the draw and the measure do not agree at size 0.
- A write of 0 to `font.LegacySize` (a `ref float`) works, so a test can make a font with a size of 0.
- **The choice:** a guard. When the resolved size is not finite or is 0 or less, `Text` draws nothing and `MeasureText` returns `(0, 0)`. Neither makes a native call. The private helper `IsDrawableSize` holds the rule. `ImGuiMapping.FontSize` never gives a negative size from a call size, but a font with a negative or `NaN` `LegacySize` does. The guard also stops the crash of that case.

**The other facts of the check (the default font, size 13):**

- `"a\n"` measures `7x13`, `"\n"` measures `0x13`, and `"a\nb"` measures `7x26`. A line feed at the end adds no line. The height is the line count times the size, and ImGui gives it directly, so the renderer uses the ImGui height with no change.
- Size 13.5 at wrap 70 gives `69.42857x27`, so the height stays the line count times the size for a fraction size. The renderer rounds the width to 70.
- `"Hello"` at wrap 1 gives `7x65`: one glyph for each line, and the width is above the wrap width (the exception of the table row).
- `"Supercalifragilistic"` at wrap 20 gives `14x130`: ImGui cuts the word inside the word.
- `"Hello, brutal-ui "` (a blank at the end) at wrap 70 gives `70x26`. The blank at the end of the last line counts in the width. The width stays at or below the wrap width.
- Through the renderer: `112x13` at 13, `224x26` at 26, and `63x26` at the default size with wrap 70, before the first frame, inside a frame, and between frames. An empty span gives `(0, 13)`. `NaN` and positive infinity as the wrap width give `112x13`. The draw of `Hello, brutal-ui` at `10,10` with wrap 70 gives `elems 90 verts 11,12-70,34`, inside the measured box `10,10`-`73,36`. 1000 `MeasureText` calls and 1000 `Text` calls (alpha 0) inside a frame allocate 0 bytes.

**The decisions:**

- **The order of `MeasureText` (the orchestrator decision):** (1) the context check (`InvalidOperationException`), (2) `Fonts.GetFont(font)` (an unknown id throws `ArgumentOutOfRangeException`, also for empty text), (3) the size rule, (4) the size guard gives `(0, 0)`, (5) an empty span gives `(0, fontSize)`, (6) the measure. The size guard comes before the empty check, so a font with a negative size never gives a negative height. The remarks state the order.
- **The order of `Text` (a choice that the plan does not name):** (1) `ThrowIfNoFrame()`, (2) `Fonts.GetFont(font)`, (3) the size rule, (4) the skip rules. The font lookup comes before the skip rules, so an unknown id throws also for an empty text or an alpha of 0, the same as `MeasureText`. The cost is one `GetCurrentContext` P/Invoke for a skipped call. `GetFont` also checks the context, so `Text` with no context throws `InvalidOperationException`.
- **The skip rules of `Text`:** an empty span, an alpha of 0, a resolved size that fails the guard, or a position with a `NaN` or infinite component draws nothing. **The position check is a change from the table row.** Reason: ImGui does not cut vertices at the clip, and the `HasArea` rule of `Rect` skips a non-finite rect for the same reason.
- **The ImGui alpha check:** ImGui also skips an alpha of 0 in `AddText`. The renderer checks it first, so no pin and no native call happen.
- **`MeasureText` checks the context itself** before `GetFont`, with its own message, as the phase 1 notes ask. `GetFont` checks it again. The cost is one more P/Invoke, with 0 bytes.
- **The width rule:** `ImGuiMapping.RoundUpWidth` on the ImGui width. The remarks state the 256 rule. The height is the ImGui height with no change.
- **The interface doc:** `IUiRenderer.Text` gets remarks with the contract of renderer-interface (an empty span and an alpha of 0 draw nothing) and the `wrapWidth` parameter doc of the brief. The doc names no ImGui type. Section 8 of `docs/technical-design.md` gets the new `Text` line and one sentence on the shared wrap rule. The "ImGui implementation" paragraph waits for task 5.4.
- **`RecordingRenderer`:** `DrawCall.WrapWidth` holds the wrap width as the caller gave it, with no rule applied. `Describe` adds ` wrap <w>` before the text only when the width is above 0, so `NaN`, 0, and negative widths print as before. Each golden line stays the same.
- **The tests:** `PaintContentTests` gets `RecordsTheWrapWidth` (a fact) and `DescribesNoWrapForAWidthOfZeroOrLess` (a theory with 0, -5, and `NaN`). The probe gets a `WrapWidth` property with a default of 0. The renderer text tests wait for group 3.

**Blast radius of the interface change (`IUiRenderer.Text` gets `float wrapWidth`):**

- `src/BrutalUi.Core/Rendering/IUiRenderer.cs`: the signature and the doc.
- The implementations: `ImGuiRenderer` (src), `RecordingRenderer` (tests, linked into `BrutalUi.ImGui.Tests` and the benchmark project), and `ThrowingRenderer` and `ReentrantRenderer` (private classes in `PaintWalkTests.cs`).
- The callers: the `ContentProbe` hook in `PaintContentTests.cs` only. `PaintWalk.cs` calls no `Text`. `samples/HelloPanel/Program.cs` calls `ImGui.Text` of ImGui, not the interface, so it does not change.
- No other project in `src/` implements or calls `IUiRenderer.Text`. A game renderer outside this repository breaks at compile time. The package is not released yet, so no migration note is necessary.
- `docs/technical-design.md` section 8: the interface block.

**The test totals:** 703, 0 failed (the 699 of group 1 and 4 recorder tests). `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors.

**Notes for group 3 (the renderer text tests and the demo):**

- The probe numbers above are the expected values for the default font of the harness (`LegacySize` 13).
- A test of the size guard sets `font.LegacySize = 0f` and restores it in a `finally` block. Do not test a negative size with a direct native call: it stops the process.
- `Text` with an unknown id throws `ArgumentOutOfRangeException` also for an empty span or an alpha of 0.
- `Text` skips a position with a `NaN` or infinite component. Add a test for it.
- The draw of `Hello, brutal-ui` at `10,10` with wrap 70 gives `cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 11,12-70,34 colors #FFFFFFFF`. The blank makes no glyph, so 15 glyphs give 90 elements.
- `MeasureText` with no context throws `InvalidOperationException` from its own check. `Text` with no context and no frame throws from `ThrowIfNoFrame()` first.

## Phase 2 Group 3: Decisions (tasks 2.6 to 2.9)

**The demo (task 2.9, `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*MeasuresAndDrawsText' --show-live-output on`):**

```
measure 13 wrap 0 -> 112x13
measure 26 wrap 0 -> 224x26
measure 13 wrap 70 -> 63x26
cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 11,12-119,23 colors #FFFFFFFF
```

The demo draws the 13 px text with no wrap at `10,10`. The measured box is `10,10`-`122,23`, and the box grown by 1 pixel is `9,9`-`123,24`. The vertex bounds `11,12-119,23` lie inside it. The test asserts the four lines exactly.

**The test totals:** 750, 0 failed (Core 625, Framework 1, Markup 1, ImGui 123). The ImGui project gets 47 tests in `ImGuiRendererTextTests.cs` (the theories count each data row). No test of the ImGui project logs an `[imgui-error]` line. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors.

**The real results of ImGui (each one is now a test assert):**

- `CutsAWordThatIsWiderThanTheWrapWidth`: `brutal-ui` at size 13 and a wrap width of 40 gives `35x26`. ImGui cuts the word after 5 glyphs (`bruta` and `l-ui`). This is the rule of the brief: more than one line, and a width at or below 40. No change was necessary.
- The break rules at a wrap width of 50 (size 13): `aaaa bbbb` gives `28x26`, because the blank at a break does not count in the width. `aaaa.bbbb`, `aaaa,bbbb`, `aaaa;bbbb`, `aaaa!bbbb`, `aaaa?bbbb`, and `aaaa"bbbb` give `35x26`, because the mark stays on line 1. `aaaa-bbbb` gives `49x26`, because `-` is no break point and ImGui cuts the word after 7 glyphs.
- **The clip (a fact that the plan does not name):** for text, ImGui drops each glyph that lies fully outside the clip rect on the CPU. The text `Hello, brutal-ui` at `10,10` under the clip `5,5,50,20` gives `cmd 0 clip 5,5,55,25 tex atlas elems 36 verts 11,12-48,23 colors #FFFFFFFF`: 6 glyphs (`Hello,`), not 15. The rect fills of phase 1 have no such cut. It is not a fault of `src/`, because the GPU scissor cuts the same pixels.
- The second font: the ImGui default font with `SizePixels` 20 and `GlyphExtraAdvanceX` 1 measures `189x20` at size 0 (its own size) and `128x13` at size 13 (112 and 1 pixel for each of the 16 characters).
- A size of 13.5 with no wrap gives a raw width with a fraction, and `MeasureText` gives the next whole pixel. The height stays 13.5.

**The decisions:**

- **The demo draw has no wrap.** The brief says "the 13 px text", so the draw matches the first measure line. `DrawsWrappedTextInsideTheWrappedBox` covers the wrapped draw: it asserts `cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 11,12-70,34 colors #FFFFFFFF`, the measured box `63x26`, and vertex bounds on both sides of `y = 23` (two lines).
- **The second font (a test helper that the plan does not name):** the binding has no call to the native `ImFontConfig` constructor. The helper `AddWideFont` sets the defaults of that constructor by hand (`FontDataOwnedByAtlas`, `GlyphMaxAdvanceX = float.MaxValue`, `RasterizerMultiply` 1, `RasterizerDensity` 1), then `SizePixels` and `GlyphExtraAdvanceX`, and calls the raw `ImFontAtlas_AddFontDefault`. The extra advance makes the glyphs of the second font wider, so the test proves that the measure uses the second font. The test reads the default font first, so the second font does not become the font of id 0.
- **The size guard test:** `UsesNoNativeCallForAResolvedSizeOfZero` sets `LegacySize` to 0, -5, and `NaN` (a theory) after `NewFrame`, and restores it in a `finally` block before `Render`. A native draw at size 0 uses the frame font size, so an empty list proves that `Text` made no native call. The renderer guard stops each case before the native call, so the -5 case does not stop the process.
- **The vertex bounds:** the helper `ReadVertexBounds` reads the raw vertex buffer of the background list after the frame, as the brief allows. `DrawsGlyphsInsideTheMeasuredBox` also asserts 60 vertices (4 for each of the 15 glyphs) at sizes 13 and 26.
- **Extra tests (inside the brief file):** `BreaksAfterABlankOrAMark` (8 rows), `CountsALineFeedAsANewLine`, `RoundsTheWidthUpToAWholePixel`, `MeasureThrowsWithNoContext`, and `ThrowsForATextCallBeforeBegin`. With them, each rule of the `MeasureText` row of "The Mapping Table" has a test.

**Faults found in `src/`:** none.

**Notes for later phases:**

- Task 4.6 names `MeasureTextThrowsWithNoContext`. `ImGuiRendererTextTests.MeasureThrowsWithNoContext` covers it already. Task 4.6 can skip it or move it, but must not add a second copy of the same test.
- A text test under a clip must allow the CPU glyph cut of ImGui. Assert the glyphs that touch the clip, not all glyphs.

## Phase 3 Group 1: Decisions (tasks 3.1 and 3.2)

**The change:** `TextureHandle` gets `public int2 Size { get; }` and `public TextureHandle(int id, int2 size)`. The one-argument constructor stays and gives the size `(0, 0)`. `default(TextureHandle)` stays the none handle (id 0, size `(0, 0)`). `Id`, `IsNone`, the operators, `Equals`, and `GetHashCode` do not change. Equality and the hash code stay on the id only.

**The decisions:**

- **The doc comments:** the struct summary states that the handle holds the size in texels. The remarks state three facts: id 0 is no texture, an image element reads `Size` for its natural size, and equality uses the id only, because one renderer gives one size for each id. The doc text names no ImGui type. It says "an image element", not a type name, because label-and-image adds that type later.
- **The size is not checked:** the constructor accepts each `int2`, also a negative size or a size with an id of 0. The plan names no rule for it. The texture service of group 2 throws for a size of 0 (task 3.5 `ThrowsForASizeOfZero`), so the service is the guard. A check in Core would also stop a renderer in another assembly that has a reason for another rule.
- **`int2` equality in the tests:** the tests assert `Size.X` and `Size.Y`, not `Assert.Equal` on `int2`. Reason: the tests then do not depend on the equality members of `Brutal.Numerics.int2`.
- **The tests (`RenderValueTests.cs`):** `TextureHandleSizeReadsBack` (4660 at 64 by 32), `TextureHandleWithNoSizeHasSizeZero` (the one-argument constructor and the default handle), and `TextureHandleEqualityIgnoresTheSize` (the operators, both `Equals` forms, and the hash code, for another size and for no size).

**Blast radius (each use of `TextureHandle` in `src/` and `tests/`, traced before the change):**

- `src/BrutalUi.Core/Rendering/RenderHandles.cs`: the struct. It now imports `Brutal.Numerics`. Core uses that namespace already, so no package changes.
- `src/BrutalUi.Core/Rendering/IUiRenderer.cs`: `Image` takes the handle by value. No change. The size adds 8 bytes to the struct (12 bytes now), so the copy costs a little more. The value is still small.
- `src/BrutalUi.ImGui/ImGuiRenderer.cs` (`Image`, the `CreateInfo` doc) and `ImGuiTextureService.cs` (the class doc): no change. Group 2 creates the handle with its size.
- `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs` (`DrawCall.Texture`, `Image`) and the two private renderers of `PaintWalkTests.cs`: no change. `RecordingRenderer.Describe` prints no size, so each golden line stays the same.
- `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`: the existing tests use the one-argument constructor, and they pass with no change. 3 tests are new.
- `PaintWalk.cs` makes no `Image` call. No project in `samples/` or `benchmarks/` uses the handle.
- A renderer outside this repository compiles with no change, because the one-argument constructor stays. The package is not released yet, so no migration note is necessary.

**The test totals:** 753, 0 failed (the 750 of phase 2 and the 3 handle tests). `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors.

**Notes for group 2 (the texture service and `Image`):**

- Create the handle with `new TextureHandle(id, size)`. `GetSize` of the service gives the same size as `handle.Size` (Decision 9).
- A handle with the same id and another size is equal to the registered handle. The service must look up the id only, and it must not compare the size of the handle that the caller gives.

## Phase 3 Group 2: Decisions (tasks 3.3 to 3.7)

**The demo (task 3.7, `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsAnImageWithItsTexture' --show-live-output on`):**

```
size 64x32
cmd 0 clip 0,0,400,300 tex id 4660 elems 6 verts 20,20-84,52 colors #FFFFFFFF
cmd 1 clip 0,0,400,300 tex atlas elems 6 verts 100,20-130,50 colors #FF0000FF
```

The image command binds the texture of the handle. The rect after it binds the atlas again. ImGui gives the image to command 0, because the first command of the list has 0 elements, so ImGui changes its texture and adds no command. The test asserts the three values and the two lines exactly.

**The Vulkan signatures (task 1.1, used with no change):** `AddVulkan` calls `public ImTextureRef VulkanBackend.AddTexture(VkSampler sampler, VkImageView imageView, VkImageLayout imageLayout)` and stores the result. `Remove` calls `public void VulkanBackend.RemoveTexture(ImTextureRef texture)` with the stored result. `VulkanBackend` is in `Brutal.ImGuiApi.Backends`. `VkSampler`, `VkImageView`, and `VkImageLayout` are in `Brutal.VulkanApi`. The pinned `Ahwoo.BRUTAL.ImGui.Backends` package brings `Ahwoo.BRUTAL.Vulkan` through `Ahwoo.BRUTAL.Vulkan.Abstractions`, so `BrutalUi.ImGui.csproj` needs no new package reference.

**The test totals:** 786, 0 failed (Core 625, Framework 1, Markup 1, ImGui 156). The ImGui project gets 16 tests in `ImGuiTextureServiceTests.cs` and 17 tests in `ImGuiRendererImageTests.cs` (the theories count each data row). No test of the ImGui project logs an `[imgui-error]` line. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors.

**The decisions:**

- **The entry:** `ImTextureRef Texture`, `int2 Size`, `VulkanBackend? Owner`, and `bool Removed`. `RemoveTexture` takes the `ImTextureRef` that `AddTexture` gave, so the entry holds no other backend value. The plan names "the backend value that `RemoveTexture` takes" as a separate field. Here it is the same field.
- **The lookup:** one unsigned compare (`unchecked((uint)(id - 1)) >= (uint)_count`) covers id 0, a negative id, and an id above the last id. A removed entry throws with its own message. Both throws use the parameter name `texture` and the id as the actual value, through `[DoesNotReturn]` helpers. The lookup reads the id only, so a handle with another size names the same texture.
- **`Add` checks the reference (a check that the plan does not name):** a reference with no texture data and a texture id of 0 throws `ArgumentException`. It names no texture, and the backend fails on it at render time. The size check comes first.
- **The order of the `AddVulkan` checks (a change from the plan order):** (1) the size, (2) a null sampler, (3) a null image view, (4) a null backend, then the backend call. The null handle checks are new: the backend writes the handles into a descriptor set, and a null handle there fails on the GPU. The backend check comes last, so a test with a null backend can prove each check with no GPU. The order among argument errors does not change the contract. **Deviation from the brief:** `AddVulkanThrowsForANullBackend` passes fake handles that are not null (`new VkSampler(1)` and `new VkImageView(2)`), not `default`, because a `default` handle now throws `ArgumentException` first. `AddVulkanChecksTheArgumentsBeforeTheBackend` covers the other three checks.
- **The growth before the backend call:** `AddVulkan` grows the table before `backend.AddTexture`. A failed growth then leaks no descriptor set.
- **The layout is not checked:** many layouts are wrong for a sampled image. The service gives the layout to the backend with no check.
- **`Remove` marks the entry first:** the entry becomes removed (and drops the backend reference and the texture reference) before `backend.RemoveTexture`. When the backend call throws, the handle stays unknown, so no later draw uses a descriptor set that the backend can have freed. The service keeps no reference to the backend after `Remove`.
- **The service reads no ImGui state:** each member works with no context. Most service tests create no harness. `AcceptsAReferenceWithTextureData` runs one frame first, because the atlas gets its texture data at the first `NewFrame`.
- **The order of `Image` (a choice that the plan does not name):** (1) `ThrowIfNoFrame()`, (2) the none handle returns, (3) `Textures.GetTexture` (a stale or unknown id throws), (4) the skip rules (a tint alpha of 0, or `HasArea` false). This is the order of `Text`: a stale handle throws also for an alpha of 0 or an empty rect, so the game sees the bug at once. The lookup is a table read with no P/Invoke.
- **The UV:** `(uv.X, uv.Y)` is the top-left UV, and `(uv.Z, uv.W)` is the bottom-right UV, with no change. A flipped UV rect draws a mirrored image (`KeepsAFlippedUvRect`). A `NaN` UV is not checked: it gives no bad vertex position, only an undefined texel on the GPU.
- **`NotYetSupported` is gone:** `Image` was the last member that used it.
- **Extra tests (inside the brief files):** `LooksUpTheIdOnly`, `AcceptsAReferenceWithTextureData`, `AddThrowsForAnEmptyReference`, `AddVulkanChecksTheArgumentsBeforeTheBackend`, `LookupsAllocateNothing` (1000 `GetTexture` and `GetSize` calls give 0 bytes), `KeepsAFlippedUvRect`, `DrawsNothingForARectWithNoArea` (8 rows), `ThrowsForAnImageCallBeforeBegin`, and `DrawsUnderTheClipAtTheViewportPosition` (`cmd 0 clip 110,60,150,100 tex id 4660 elems 6 verts 120,70-184,102 colors #FFFFFFFF`). `ImGuiTextureServiceTests.TestTexture(int)` is `internal static`, and the image tests use it.

**Faults found in `src/`:** none.

**Blast radius:** `ImGuiTextureService.cs` and `ImGuiRenderer.cs` change. `ImGuiTextureService` now imports `Brutal.ImGuiApi.Backends` and `Brutal.VulkanApi`, so the public API of `BrutalUi.ImGui` names `VulkanBackend` and three Vulkan handle types. Both packages come from the pinned `Ahwoo.BRUTAL.ImGui.Backends` already. No file of `BrutalUi.Core` changes. No package changes. No test file creates a `VulkanBackend`.

**Notes for phase 4 (the rounded image, the corners, and the borders):**

- `Image` ignores `radius`. Task 4.2 adds `AddImageRounded` (signature in task 1.1 (a)) when the rounding of `ResolveCorners` is above 0. Use the rect width and height of the image for the corner rule. Keep the check order of `Image` above, and call `ResolveCorners` after the skip rules.
- `AddImageRounded` with `RoundCornersNone` or a rounding of 0 falls back to `AddImage` in ImGui. A test for `DrawsARoundedImage` can expect more than 6 elements and `tex id 4660` on the command.
- The image tests use `ImGuiTextureServiceTests.TestTexture(4660)` and the constants `DemoRect` (`20,20,64,32`) and `FullUv`. Task 4.5 adds its test to `ImGuiRendererImageTests.cs` and can reuse them.
- The `Image` summary says "This release draws a square image only." Task 4.2 must change that sentence.
- Phase 5 (`DirectCallsAllocateNothingAfterWarmUp`) can register a texture with `Add(TestTexture(...), size)` and needs no GPU. `GetTexture` allocates 0 bytes.

**Open issues (no change made):**

- `Remove` after the backend is disposed calls `RemoveTexture` on a disposed backend. The service cannot see the dispose. The doc comment states the rule: call `Remove` before the dispose.
- `AddVulkan` does not check the result of `backend.AddTexture`. An empty result would register a handle that names no texture. No test can reach that path with no GPU.

## Phase 4: Decisions (tasks 4.1 to 4.6)

**The resume state (task R.1):** the build was clean (0 warnings, 0 errors) and the test run had 6 failures out of 827. Tasks 4.1 and 4.2 (`ImGuiMapping.cs`, `ImGuiRenderer.cs`) were complete, and no change was necessary. Task 4.3 (`ImGuiMappingTests.cs`) was complete. Tasks 4.4 and 4.5 had all tests written, but five tests had wrong expected values, and `ImGuiRendererRectTests.cs` held debug code (a `ProbeTemp` theory and a `DBG` print loop). Task 4.6 was complete: `BeginThrowsWithNoContext` is in the rect tests, and `ImGuiRendererTextTests.MeasureThrowsWithNoContext` covers the measure (no copy exists in the rect tests, only a comment). All 6 failures were wrong test expectations. The rules in `src/` were right.

**The demo (task 4.6, `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsARoundedPanelWithABorder' --show-live-output on`):**

```
corners 8,8,0,0 -> rounding 8 flags TopLeft|TopRight
border 2 -> inset 0.5 rounding 7
cmd 0 clip 0,0,400,300 tex atlas elems 144 verts -1.0003166,-1.000226-201.00032,101.00049 colors #0A141EFF #0A141E00 #FF0000FF
```

**The test totals:** 824, 0 failed (Core 628, Framework 1, Markup 1, ImGui 194). The total after phase 3 was 786, so phase 4 adds 38 tests, all in the ImGui project (156 to 194). `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. The note "Core 625" in the brief is wrong: Core had 628 after phase 3.

**The real ImGui results (headless harness, `AntiAliasedLines`, `AntiAliasedLinesUseTex`, and `AntiAliasedFill` on):**

- **The border stays inside the rect plus the fringe.** A stroke on the rect `10,10-70,70` has 8 vertices (2 for each corner), all with alpha 255, and its bounds are `8.99999,8.99999-71.00001,71.00001` for a square rect and `8.999712-71.00029` for radius 8. A probe of the widths 1, 2, and 4 on a square rect gave the same bounds (`8.99996`, `8.99968`, `8.999268` to `71.00004`, `71.00032`, `71.00073`). The texture-based line puts the outer vertex exactly 1 pixel outside the rect for each width, because `BorderInset` shrinks the path as the width grows. The rule holds: no vertex lies outside the rect grown by 1 pixel, but the float error is up to 0.0008 pixel. The tests use a slack of 0.01 pixel (`FloatSlack`).
- **The demo list:** the fill and the border give 144 elements. The vertices lie in `-1.0003166,-1.000226` to `201.00032,101.00049`, inside `0,0-200,100` grown by 1 pixel (with the 0.01 slack). The colors are the fill `#0A141EFF`, the fill fringe `#0A141E00`, and the border `#FF0000FF`.
- **A rounded fill has an anti-alias fringe.** A square fill (rounding 0) keeps the fast path: 4 vertices, 6 elements, and vertices on the exact corner points. A rounded fill goes through `PathRect` and a convex fill, so its vertices lie within 0.5 pixel of the rect on each axis, and no vertex lies on a corner point. `AddImageRounded` is the same: the demo image (`20,20,64,32`, radius 8) gives 138 elements and the vertex bounds `19.499945,19.499945-84.50005,52.500057`.
- **The UV of a rounded image** follows the position of the vertex in the rect, clamped to the UV rect, because ImGui clamps the UV of a fringe vertex.

**The decisions and the changes from the plan:**

- **Decision 5 is right as written.** The inset `borderWidth / 2 - 0.5` and the stroke radius `max(rounding - borderWidth / 2, 0)` keep each border vertex inside the rect grown by 1 pixel. No change to `ImGuiMapping`.
- **Test fixes (no change in `src/`):** (1) `AssertColorInside`, `KeepsTheBorderInsideTheRect`, and `DrawsABorderOnlyRect` now allow a slack of 0.01 pixel for the float error of the fringe (the test asked for `>= 9`, and ImGui gives `8.99999`). `DrawsARoundedPanelWithABorder` failed for the same reason. (2) `RoundsOnlyTheNamedCorners` asked for a vertex on the exact corner point. A rounded fill has a fringe, so the test now uses `HasVertexNear` (0.6 pixel on each axis): the three square corners have a vertex, and the round corner has none. (3) `DrawsARoundedImage` asked for the text ` verts 20,20-84,52 `. It now checks that each vertex lies within 0.5 pixel of the rect, that no vertex lies within 1 pixel of the round corner, and that the clamped UV follows the position. (4) The `ProbeTemp` theory and the `DBG` print loop are removed. The bounds line of `KeepsTheBorderInsideTheRect` stays as test output.
- **The fill under a covering border (a choice of the earlier agent, kept):** when the border covers the box and its alpha is 255, `Rect` skips the fill, because the border fill hides it. This is a change from the table row of the brief ("the fill draws when its alpha is above 0"). It saves a draw with no visible change. A border with an alpha below 255 draws the fill first (`KeepsTheFillUnderATranslucentWideBorder`, 12 elements). `FillsTheBoxForAWideBorder` expects 6 elements with the border color only.
- **Task 4.6:** `MeasureTextThrowsWithNoContext` is not added again. `ImGuiRendererTextTests.MeasureThrowsWithNoContext` covers it (the brief allows this).
- **The class remarks** of `ImGuiRenderer` state the corner rule and the limit of Decision 4. The text "square image only" is gone.

**Faults found in `src/`:** none.

**Blast radius:** no file of `src/` changes in this resume. The test files `ImGuiRendererRectTests.cs` and `ImGuiRendererImageTests.cs` change. The phase 1 demo dump (`DrawsThreeElementTreeWithOneClip`) does not change: a square rect keeps 6 elements.

**Notes for phase 5:**

- A rounded fill and a rounded image draw an anti-alias fringe, so a test of their vertices needs a tolerance of 0.5 pixel plus 0.01 pixel. A test of a square fill can still expect the exact corner points.
- `DirectCallsAllocateNothingAfterWarmUp` should include a rounded `Rect` with a border and a rounded `Image`. These paths call `AddRect` and `AddImageRounded`. The code in `Rect` and `Image` uses only locals and the pure rules, so no allocation is expected, but no test proves it yet.
- Mixed radii above 0 draw with the largest one (Decision 4, gap 5). An exact path is a later change.

## Phase 5 Group 1: Decisions (tasks 5.1 to 5.3)

**Task 5.1: `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`.**

- **Gate form.** The class follows `ThousandElementAllocationTests`. Each frame calls `BeginFrame`, reads `GC.GetAllocatedBytesForCurrentThread()`, runs the measured calls, reads the counter again, and calls `EndFrame`. The harness calls stay outside the measured region. The test sums the 100 differences after 3 warm-up frames.
- **`DrawAllocatesNothingAfterWarmUp`.** A harness of 1280 by 720, `ThousandElementTree.Build()`, one `Update`, and `tree.Context.Draw(renderer)` in the measured region. Sum: 0 bytes.
- **`DirectCallsAllocateNothingAfterWarmUp`.** The test registers a second font (`AddFontDefault` with `GlyphExtraAdvanceX` 1, set by hand as in `ImGuiRendererTextTests`) and a texture (`TestTexture(4660)`). The measured region holds `Begin`, `PushClip`, 4 `Rect` calls, 2 `Image` calls, 3 `Text` calls, 2 `MeasureText` calls, `PopClip`, and `End`. The 4 `Rect` calls are: a square fill, a rounded fill with a translucent border (`AddRectFilled` and `AddRect`), a full-cover border with alpha 255 (the fill is skipped), and a full-cover border with alpha 200 (fill and border fill). The 2 `Image` calls are one square (`AddImage`) and one rounded (`AddImageRounded`). `Text` runs with the default font at 13 and at 26, and with the second font at 13 and a wrap of 70. A third `MeasureText` runs after `EndFrame`, outside the frame, and its bytes add to the sum. Sum: 0 bytes. The warm-up frames use each font and size of the measured frames (risk 10), so no bake happens in a measured frame.
- **`TheMeasuredDrawDoesRealWork`.** The tree draws once into `RecordingRenderer` and the test counts the `Rect` calls. The same tree draws once through `ImGuiRenderer`, and the test asserts that the vertex count of the raw buffer of the background list is at least 4 times that count. The vertex count is read outside the measured region, with no call of `DescribeBackgroundList`. The test writes both numbers to the test output.
- **Result:** the 3 tests pass, total 3, failed 0.

**Task 5.2: the gate blocks.**

- Copy: `scratchpad/ImGuiRenderer.cs.orig`. The planted line `GC.KeepAlive(new object());` stood at the start of `Rect`.
- `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` with the planted line: **exit=2**. The class run showed `DrawAllocatesNothingAfterWarmUp` with 564000 bytes and `DirectCallsAllocateNothingAfterWarmUp` with 9600 bytes. Both numbers match 24 bytes for each `Object` (23500 `Rect` calls for the tree, 400 for the direct frames), so the gate sees the plant and the cause is the plant.
- The edit tool removed the line. `diff` against the copy: **no output** (diff exit 0).
- The same command after the restore: **exit=0**.

**Task 5.3: the demo and the Linux script.**

- `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-class '*ImGuiRendererAllocationTests'`, 5 runs: each run read `total: 3` and `failed: 0`.
- `scripts/test-linux.sh; echo "exit=$?"`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 14 s`, **exit=0**.
- `dotnet build brutal-ui.sln`: 0 Warning(s), 0 Error(s). `dotnet test --solution brutal-ui.sln`: total 827, failed 0 (Core 628, Framework 1, Markup 1, ImGui 197).

**Faults found in `src/` or in the binding:** none. The renderer allocates 0 managed bytes on both paths, including the rounded rect with a border (`AddRect`), the rounded image (`AddImageRounded`), the wrapped text, and the measure outside a frame.

**Blast radius:** no file of `src/` changes (the plant is restored and `diff` is empty). One new test file.

## Phase 5 Group 2: Decisions (tasks 5.4 to 5.7)

**Task 5.4: `docs/technical-design.md`.**

- Section 3 gets `BrutalUi.ImGui.Tests/` in the tree and one sentence on the headless tests in the `tests/` paragraph.
- Section 8, "ImGui implementation", is rewritten as eight short paragraphs: the renderer and the list (Q2), the services, the handle errors (Decision 7), the explicit font and size calls (Decision 2), the size and wrap rules, the corner rule and its limit (Decision 4), the border (Decision 5), the viewport (Decision 6), and the allocation rule. Gap 2 is closed: the old text said "private list by default, background list on request". Q2 puts the background list in this epic, and the private list waits for the world-space epic.
- Section 14, "Test types", gets two lines: the headless ImGui tests and the second allocation gate (`ImGuiRendererAllocationTests`).
- The source differs from the plan in these facts. The doc states the source version.
  - `HasArea` also needs a finite width and a finite height.
  - A resolved font size of 0 or less, or not finite, draws nothing and measures `(0, 0)`.
  - `Text` skips a position that is not finite.
  - `MeasureText` resolves the font before the empty-text check, so an unknown id throws also for empty text.
  - `RoundUpWidth` gives the width plus 1 for a whole width of 256 or more, as ImGui does.
  - `WrapWidth` maps infinity to 0, like `NaN` and values of 0 or less.
  - `Begin` does not detect a call outside an ImGui frame.
  - `AddVulkan` checks the size, the sampler, the image view, and the backend before the backend call.
  - `Rect` skips the fill when an opaque border covers the box.
  - A rounded fill and a rounded image draw an anti-alias fringe of 0.5 pixel. The border stroke reaches 1 pixel outside the rect for the fringe.
  - A clip with an area of 0 still gets draw calls (gap 7). The Vulkan backend skips such a command.

**Task 5.5: `CHANGELOG.md`.** One line under `### Added`, as the brief words it. Group 1 recorded the allocation results, and the line does not repeat them.

**Task 5.6: `human-testing.md`.** The `## imgui-draw-list` section already held the demo lines of phases 1 to 4. It gets one line: the Windows run of `scripts\test-windows.ps1`, in the form of the `renderer-interface` line. The phase 5 demo line is for the orchestrator. The lint raised the file to 2.54 hits for each 100 words, so the long demo sentences of the `renderer-interface` phases 2 and 4 and of the `imgui-draw-list` phases 2 to 4 are split into short sentences. No fact changes.

**Task 5.7: the lint.** `ste-lint.py --fail-over 2.5` on `docs/technical-design.md` (1.36 before the lint pass), `CHANGELOG.md` (1.53), `human-testing.md` (1.56 after the split), `implementation.md` (1.78, no fix necessary), and this file (1.75). Each file is under 2.5, and the lint exits with 0. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 827, failed 0.

### Native Dependencies

`dotnet test` now loads `libimgui.so` (Linux) and `imgui.dll` (Windows) from the pinned `Ahwoo.BRUTAL.ImGui` package, because `BrutalUi.ImGui.Tests` creates a headless ImGui context. It is not a new dependency. The package is pinned in `Directory.Packages.props` already, and `samples/HelloPanel` loads the same library. No package, no `Directory.Packages.props` line, and no native file is added. The test project creates no `VulkanBackend`, so it needs no GPU and no Vulkan library.

### Hand-offs

**label-and-image**

- `MeasureText` rules: the width rounds up to a whole pixel (`floor(width + 0.99999)`, plus 1 for a whole width of 256 or more). The height is the line count times the size. Empty text gives `(0, size)`. A line breaks after a blank or after one of `. , ; ! ? "`. A word that is wider than the wrap width is cut inside the word, so the width stays at or below the wrap width, except when the wrap width is below one glyph. A wrap width of 0 or less, `NaN`, or infinity means no wrap. A size of 0 or less, `NaN`, or infinity gives the size of the font. A resolved size that is not drawable gives `(0, 0)`.
- `MeasureText` works outside a frame (before the first frame, inside a frame, and between frames). It needs a current ImGui context and throws `InvalidOperationException` without one.
- Font id 0 is the ImGui default font. An unknown id throws `ArgumentOutOfRangeException`, also for empty text.
- `Image` reads `handle.Size` for its natural size. The texture service gives the same size through `GetSize`.
- `IUiRenderer.Text` takes a `wrapWidth`. A wrapped `Label` must pass the same wrap width to `MeasureText` and to `Text`, so both give the same lines.
- The measure route is a measurer slot on `UiContext`, set at creation. label-and-image builds the slot.
- The `RecordingRenderer` measure rule (`length * size * 0.5` for the width, `size` for the height) differs from ImGui. A test with `RecordingRenderer` must not expect ImGui widths.

**hello-panel-sample**

- The create order: `ImGui.CreateContext`, then the font loads, then the font service, the texture service, and the renderer, then the backends. Load the fonts of the game (or set `io.FontDefault`) before the first `GetFont(0)` call. Else the ImGui default font becomes font 0 of the atlas.
- Call `AddVulkan` after the texture upload.
- Call `Update` and `Draw` between `ImGui.NewFrame` and `ImGui.Render`. `Begin` does not detect a call outside that window.
- Set `UiContext.ViewportSize` equal to `io.DisplaySize`. The renderer uses neither `scale` nor `displaySize`.
- The descriptor pool holds 8 sets. Each `AddVulkan` takes one, and the font atlas takes one.
- Call `Remove` after `vulkan.WaitIdle()` and before the backend is disposed.
- The visual check of the GPU path is MANUAL. No headless test draws on a GPU.

**The world-space epic**

- A private list needs a second draw-list source. `Begin` reads the list in one place.
- Share the two services between the two renderers.

## Review Group 1: Decisions (CR-1, CR-2, CR-4)

**Blast radius.** `ImGuiRenderer.PushClip`, `PopClip`, `Rect`, and `Image` change. `ImGuiMapping` gets one rule, `HasFiniteBounds`. No public signature changes. The callers are `PaintWalk` (through `IUiRenderer`) and the ImGui tests. No Core file changes. The draw paths get no P/Invoke and no allocation. The allocation tests pass.

**CR-1: the `PopClip` guard.**

- `PopClip` reads `_list._ClipRectStack.Count` after the depth check. When the size is 1 or less (`ViewportClipCount`), it calls the `[DoesNotReturn]` helper `ThrowNoClipOnList`, before the depth changes and before the native pop. The message is the text of the brief.
- The read is a field read through the pointer, with no P/Invoke.
- The new test `PopClipInALaterImGuiFrameThrows` runs frame 1 with `Begin`, two `PushClip` calls, and no `End`. In frame 2 it reads the list, so ImGui resets it. Two `PopClip` calls throw, and the stack keeps size 1. A `Begin` then draws under the viewport clip.
- Fix (2), the `catch` in `UiContext.Draw`, and the Core test are out of scope. They belong to renderer-interface.
- **Real ImGui result (probe, removed after the run).** ImGui does not reset the background list at `NewFrame`. It resets the list at the first `GetBackgroundDrawList` of the frame. The probe kept the list pointer of frame 1. Frame 1 with two pushes gives size 3, also after `Render`. In frame 2 with no access, the size stays 3. A `PopClip` then pops a stale clip (size 2) and does not throw. The first access gives size 1. The list pointer stays the same.
- So the guard closes the out-of-bounds write. It does not detect a stale frame on a list that ImGui has not reset yet. Such a pop stays in bounds, and the reset at the first access clears it. A `Rect` in that state also draws into the stale list. This is a follow-up (see below).

**CR-2: the non-finite clip rule.**

- **Real ImGui results** (viewport 0,0,400,300, the clip in min-max form, before the fix):

| Clip rect | ImGui clip |
|---|---|
| `NaN, 10, 50, 50` | `NaN,10,NaN,60` |
| `10, NaN, 50, 50` | `10,NaN,60,NaN` |
| `10, 10, NaN, 50` | `10,10,NaN,60` |
| `10, 10, 50, NaN` | `10,10,60,NaN` |
| `+inf, 10, 50, 50` | `Infinity,10,Infinity,60` |
| `10, +inf, 50, 50` | `10,Infinity,60,Infinity` |
| `-inf, 10, 50, 50` | `0,10,0,60` |
| `10, -inf, 50, 50` | `10,0,60,0` |
| `10, 10, +inf, 50` | `10,10,400,60` |
| `10, 10, 50, +inf` | `10,10,60,300` |
| `10, 10, -inf, 50` | `10,10,10,60` |
| `-inf, 10, +inf, 50` | `0,10,NaN,60` |
| `3e38, 0, 3e38, 10` | `3E+38,0,3E+38,10` |

- The intersect keeps a `NaN` and a min of `+inf`. `-inf + inf` gives `NaN`. So an infinite component can give a clip value that is not finite, and the brief rule for an infinite component applies.
- **Decision.** `PushClip` computes `min` and `max`. When `HasFiniteBounds(min, max)` is false, each coordinate of `min` that is not finite becomes the origin, and `max` equals `min`. One rule covers a `NaN` component, an infinite component, and an overflow of `x + width`. It matches the rect rule (a rect with an infinite size draws nothing).
- **Behavior change.** A clip with a width or a height of `+inf` gave the viewport clip before. Now it clips everything. The walk gives an infinite clip only when the layout gives an infinite size, and the `Rect` of that element draws nothing already. No test relied on the old result.
- The theory `PushClipWithANaNComponentClipsEverything` has 4 rows. The new theory `PushClipWithANonFiniteBoundClipsEverything` has 9 rows (the infinite rows and the overflow row). Each row asserts the exact dump clip, a finite clip, a width or a height of 0, and a balanced stack.
- Section 8 of `docs/technical-design.md` names the rule in the viewport paragraph.

**CR-4: the finite bounds rule.**

- `ImGuiMapping.HasFiniteBounds(in float2 min, in float2 max)` checks the four coordinates. The brief names `max` only. The rule also checks `min`, because the origin plus a large `x` can overflow too. For example, `x = -3.4e38` and `width = 3.4e38` with a negative origin give an infinite `min` and a finite `max`.
- `Rect` and `Image` return after the computation of `min` and `max` when the rule is false. `Image` keeps its check order: the frame, the none handle, the texture lookup, the skip rules. The new check is the last skip rule.
- The row `3e38, 0, 3e38, 10` is in `SkipsAnEmptyRect` and in `DrawsNothingForARectWithNoArea`.
- Section 8 names the finite bounds rule in the border paragraph. The handle-error paragraph names the new `PopClip` throw.

**Results.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 843, failed 0 (827 before, plus 16 new cases: 1 fact, 4 NaN rows, 9 non-finite rows, and 2 overflow rows).

**Follow-up work and migration risks.**

- A stale frame on a list that ImGui has not reset passes the frame check of each draw call. Fix (2) of CR-1 (an `End` call on the exception path of `UiContext.Draw`) closes most of it. A frame-count check in the draw calls costs a P/Invoke, so it needs a decision.
- `PopOpenClips` limits the count to the full stack size. When the size equals the depth, it also pops the viewport clip. In a valid frame the size is above the depth, so this does not happen today. A limit of `Count - ViewportClipCount` is safer.
- `HasFiniteBounds` has no direct test in `ImGuiMappingTests`. The renderer rows cover it.

## Review Group 2: Decisions (CR-5, CR-10)

**CR-5 (a border below 1 pixel).** Probe before the fix, on the rect `10,10-50,30`: a border of 0.25 gave the red bounds `8.6248,8.6249-51.3752,31.3751` (square) and a border of 0.5 with radius 8 gave `8.75,8.75-51.25,31.25`. Both lie outside the rect grown by 1 pixel. `AddRect` draws the thin stroke as 1 pixel, so the stroke does not shrink with the thickness. Fix: `Rect` computes the inset from `MathF.Max(borderWidth, 1f)`. The inset is then never below 0. The thickness that goes to `AddRect` stays the width of the caller (the thin stroke is 1 pixel anyway), so the rule is exact. `BorderInset`, `BorderRounding`, and `BorderCoversBox` stay the same: they take the width as the caller gave it, and no test showed a fault. The `Rect` remarks and section 8 of `technical-design.md` state that a border below 1 pixel draws as 1 pixel.

Real bounds after the fix (rect `10,10-50,30`, area `9,9-51,31` plus the slack of 0.01):

| Border | Radius | Border vertices |
| --- | --- | --- |
| 0.25 | 0 | `8.999857,8.999668-51.000145,31.000332` |
| 0.5 | 0 | `8.999857,8.999668-51.000145,31.000332` |
| 0.99 | 0 | `8.999857,8.999668-51.000145,31.000332` |
| 0.25 | 8 | `8.999775,8.999889-51.000225,31.00011` |
| 0.5 | 8 | `8.999995,9.000237-51.000004,30.999762` |
| 0.99 | 8 | `9.000244,9.0000305-50.999756,30.999971` |

The new test is `KeepsAThinBorderInsideTheRectGrownByOnePixel` (6 rows). Before the fix, the rows for 0.25 and 0.5 failed (4 of 6, the 0.99 rows passed). The demo `DrawsARoundedPanelWithABorder` still prints `border 2 -> inset 0.5 rounding 7`.

**CR-10 (`PopOpenClips`).** The count is now `Math.Max(Math.Min(depth, Count - ViewportClipCount), 0)`. Test `EndKeepsTheViewportClipWhenTheGamePoppedTheClipByHand` pushes a clip, pops it by hand with the native call, calls `End`, and asserts that the stack keeps size 1.

**F.1.** `ImGuiMappingTests` has two new theories for `HasFiniteBounds`: 3 rows for true, and 8 rows for false (`NaN` and infinity in each component of `min` and `max`).

**Results.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 861, failed 0 (843 before, plus 18 new cases: 6 + 1 + 11).

## Review Group 3: Decisions (CR-6)

**New tests (3).**

- `ImGuiRendererRectTests.PushClipStaysInsideTheParentClip`: pushes `-50, -50, 1000, 1000` and draws a rect. It asserts the clip `0,0,400,300`. Then it pushes `10, 10, 100, 100` and a larger `0, 0, 300, 300`, draws a rect, and asserts the first clip `10,10,110,110`. It also asserts the stack size of 3.
- `ImGuiRendererRectTests.BeginKeepsTheViewportClipWhenTheGamePoppedTheClipByHand`: pushes a clip, pops it by hand with the native call, and calls `Begin` (the aborted frame path). It asserts that the stack keeps the viewport clip (size 1), draws a rect, calls `End`, and asserts the dump of the red rect under the viewport clip. Group 2 covers `End`, so this test covers `Begin`.
- `ImGuiRendererTextTests.MeasuresAndDrawsASpanWithNoZeroByte`: slices `"Hello, brutal-ui"u8[..5]`. The measure equals the measure of `"Hello"u8` and equals `35x13`. The draw has 30 elements.

**Mutations** (the copy is `scratchpad/cr6-ImGuiRenderer.cs`; each mutation was made with the edit tool and restored with the edit tool).

| Mutation | Failed tests |
| --- | --- |
| `PushClipRect(..., false)` | `PushClipStaysInsideTheParentClip` |
| `PopOpenClips` with `count = depth` (no limit) | `BeginKeepsTheViewportClipWhenTheGamePoppedTheClipByHand`, `EndKeepsTheViewportClipWhenTheGamePoppedTheClipByHand` |
| `PopOpenClips` with the old limit `Math.Min(depth, Count)` | `BeginKeepsTheViewportClipWhenTheGamePoppedTheClipByHand`, `EndKeepsTheViewportClipWhenTheGamePoppedTheClipByHand` |
| null text end in `Text` and in `MeasureText` | `MeasuresAndDrawsASpanWithNoZeroByte` |
| null text end in `MeasureText` only | `MeasuresAndDrawsASpanWithNoZeroByte` |
| null text end in `Text` only | `MeasuresAndDrawsASpanWithNoZeroByte` |

**Restore.** `diff` of `src/BrutalUi.ImGui/ImGuiRenderer.cs` and the copy gives no output (exit 0). No line of `src/` changed.

**Results.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 864, failed 0 (861 before, plus 3 new tests).

## Review Group 4: Decisions (CR-7)

**New calls in the measured region of `DirectFrame`** (the class keeps 3 tests, and the warm-up frames run the same function):

- `Text` with an alpha of 0, and `Text` with an empty span.
- `MeasureText` with an empty span. The result joins the `GC.KeepAlive` expression.
- `Image` with the none handle.
- `Rect` with a width of 0.
- `PushClip` with a `NaN` x, and its `PopClip`.
- `Rect` and `Image` with the overflow rect `3e38, 0, 3e38, 10` (the bound `x + width` is infinite).
- `Rect` with a border of 0.5 (square) and with a border of 0.25 (rounded, opaque). Both are below 1 pixel.
- One `PushClip` with no `PopClip`. `End` pops it (the `End` pop path).

The `PopClip` with no open clip throws, so it stays out of the measured region.

**Plants** (the copy is `scratchpad/cr7-ImGuiRenderer.cs`; each plant was made and removed with the edit tool).

| Plant | Actual bytes | Gate |
| --- | --- | --- |
| `GC.KeepAlive(new object());` in the skip block of `Text` (the alpha 0 call and the empty-span call both reach it) | 4800 (2 calls x 24 bytes x 100 frames) | `DirectCallsAllocateNothingAfterWarmUp` failed |
| The same call in the `HasFiniteBounds` return of `Rect` | 2400 (1 call x 24 bytes x 100 frames) | `DirectCallsAllocateNothingAfterWarmUp` failed |

**Restore.** `diff` of `src/BrutalUi.ImGui/ImGuiRenderer.cs` and the copy gives no output (exit 0). No line of `src/` changed.

**Results.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 864, failed 0. The class run of `ImGuiRendererAllocationTests` gives total 3, failed 0.

## Review Group 5: Decisions (CR-8, CR-9)

- CR-8: The class remarks and the `GetFont` remarks of `ImGuiFontService` state the font load order. Load the fonts, or set `io.FontDefault`, before the first call with id 0.
- CR-8: The hand-off line of hello-panel-sample (Phase 5 Group 2) names the font load step in the create order.
- CR-9: The class remarks state the atlas clear rule. No `io.Fonts.Clear()`, `ClearFonts()`, or `RemoveFont` for a registered font. Id 0 works after a clear.
- Section 8 of `docs/technical-design.md` does not state the create order, so it did not change.
- No code changed. The runtime check of a cleared atlas stays out of scope (known item 2).

## Review Group 6: Decisions (CR-3)

- Fix (1): The class remarks and the `Add` remarks of `ImGuiTextureService` state the rule. A reference with texture data must stay valid until `Remove`. Do not register the font atlas, because ImGui replaces its texture data when the atlas grows. The `Add` remarks also say that the service cannot detect this case, because it reads no ImGui state.
- Fix (2): `AcceptsAReferenceWithTextureData` now uses texture data that the test owns. The binding allows it: `ImTextureData` is a struct, and `new ImTextureDataPtr(&owned)` wraps a local on the test stack. This needs no ImGui context and leaks no native memory. The test no longer uses the font atlas, and its summary names the atlas as a short-lived case.
- Fix (3) stays out of scope, as the brief says. `Add` still reads no ImGui state.
- The unused `Gui` alias is removed from the test file.
- Test totals: build 0 warnings and 0 errors. Tests: 864 total, 864 passed, 0 failed (Core 218, ImGui 234, Framework and Markup the rest).

## Gotchas & Learnings

- The `PInvoke` points are `float2*`. Take the address of a local (`&min`). A local is on the stack, so no `fixed` is necessary for it.
- `ImDrawFlags.RoundCornersDefault` equals `RoundCornersAll` (240). Flags with no corner bit mean "all corners" in ImGui, so a square rect passes `RoundCornersNone` (256).
- A native ImGui assert logs and returns. It does not stop the process (see the probe).
- Do not call `ImDrawCmd.GetTexID()` in a test. The atlas `TexID` stays 0 in the headless harness, and ImGui logs an error for a texture with no id. The harness dump reads the `TexRef` fields.
- **The fringe has a float error.** Problem: a test that asks for a border vertex at `>= 9` fails, because ImGui gives `8.99999`. Solution: the tests use a slack of 0.01 pixel (`FloatSlack`). Lesson: a bound test of an anti-aliased path needs a small slack.
- **A rounded path has a fringe.** Problem: a test that asks for a vertex on the exact corner of a rounded fill fails. Solution: `HasVertexNear` with 0.6 pixel on each axis. Lesson: only a square fill (4 vertices, no fringe) gives the exact corner points.
- **ImGui resets the background list at the first access of a frame, not at `NewFrame`.** Problem: a stale renderer with no `Begin` can pop a clip of the old frame. Solution: `PopClip` reads the stack size of the list and throws before the native pop when only the viewport clip is left. Lesson: never trust `_clipDepth` alone for a native pop.
- **A thin stroke draws as 1 pixel.** Problem: `AddRect` with a thickness below 1 still draws 1 pixel, so the inset of the plan put the stroke outside the rect. Solution: the inset uses `max(borderWidth, 1)`.
- **A stack local can be texture data in a test.** `ImTextureData` is a struct, so `new ImTextureDataPtr(&local)` gives a test its own texture data with no native memory.
- **A `u8` literal ends with a zero byte.** A test of the explicit text end must slice a span, else ImGui finds the same end with `strlen`.

---

## API Integration

- `ImGuiRenderer` implements `IUiRenderer` of renderer-interface. label-and-image calls `MeasureText`, `Text`, and `Image`. hello-panel-sample creates the services and the renderer.

---

## State Management

- The renderer keeps four frame fields: the draw list, the origin, the ImGui frame count of `Begin`, and the clip depth. The services own a handle table each. Ids start at 1 and never repeat.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.ImGui.Tests/ImGuiMappingTests.cs`
- Each pure rule of `ImGuiMapping`, with exact values.

### Integration Tests
**Files:** `tests/BrutalUi.ImGui.Tests/ImGuiRenderer*Tests.cs`, `ImGuiFontServiceTests.cs`, `ImGuiTextureServiceTests.cs`, `ImGuiRendererAllocationTests.cs`
- A headless ImGui context with no GPU. Each test reads the background draw list.

### Manual Testing Checklist
- [ ] (MANUAL) `scripts\test-windows.ps1` on Windows.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads two command lines: the root fill `0,0-200,100` in `#0A141EFF` under the clip `0,0,400,300`, then the two child fills in `#FF0000FF` and `#00FF00FF` under the clip `2,2,198,98`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*MeasuresAndDrawsText' --show-live-output on` and reads three measure lines for `Hello, brutal-ui` (13 px with height 13, 26 px with height 26, and 13 px at a wrap width of 70 with height 26), then one command line whose glyph vertices lie inside the measured box.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsAnImageWithItsTexture' --show-live-output on` and reads `size 64x32` for the handle, then a command line with `tex id 4660` and the image bounds `20,20-84,52`, then a command line with `tex atlas` for the rect that follows.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsARoundedPanelWithABorder' --show-live-output on` and reads the resolved rules `corners 8,8,0,0 -> rounding 8 flags TopLeft|TopRight` and `border 2 -> inset 0.5 rounding 7`, then a command line whose vertices lie inside `0,0-200,100` grown by 1 pixel, in the fill color and the border color.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-class '*ImGuiRendererAllocationTests'` and reads a total of 3 with `failed: 0`.
- [ ] Windows run (imgui-draw-list): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0. The test run loads `imgui.dll` from the pinned `Ahwoo.BRUTAL.ImGui` package.

---

## Performance Considerations

- `Begin`, the draw calls, `MeasureText`, and `End` allocate 0 bytes after warm-up. Phase 5 gates it.

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the 5 phase demos and the Windows run of "Needs Human Verification".
2. `/autorun-epic` commits the feature, then builds label-and-image from its draft plan (`docs/features/imgui-renderer/label-and-image/implementation.md`).

### Short Term
1. label-and-image uses the hand-offs of the section "Phase 5 Group 2: Decisions".
2. hello-panel-sample follows the create order of the same section, with the font loads before the services.

---

## Open Questions

- The user answered the five scope questions of the plan on 2026-10-20.
- [x] **Q:** Should `Begin` throw outside an ImGui frame? See "Phase 1 Group B: Decisions", open questions.
      **A:** No (the orchestrator, 2026-10-20). The plan names no such check, and ImGui exposes the frame scope (`g.WithinFrameScope`) through no public call. The doc comment of `Begin` and the class summary state the call window (between `ImGui.NewFrame` and `ImGui.Render`). The public contract does not change.
- [ ] **Q:** Should `UiContext.Draw` call `End` after an exception in the walk (CR-1 fix 2)? It changes the frame contract of renderer-interface. The `PopClip` guard closes the native write already. Open for `/review-epic`.
- [ ] **Q:** Should each draw call check the ImGui frame count? A stale frame on a list that ImGui has not reset yet passes the frame check. Its pops stay in bounds. The check costs one P/Invoke for each call. Open.

---

## Session Notes

### 2026-10-20 04:31
- `/start-feature` scaffolded `context.md`, `tasks.md` (40 tasks in 5 phases), and `inbox.md`. The plan status is In Progress.
- Phase 1 group A is done (tasks 1.1 to 1.5). The probe answers, the decisions, and the notes for group 2 are in the sections "Phase 1 Group A".
- Phase 1 group B is done (tasks 1.6 to 1.11). The demo dump, the decisions, and the notes for phase 2 are in the section "Phase 1 Group B: Decisions". Phase 1 is complete.
- Phase 1 verifier: PASS, Demo: PASS. The inbox holds no line.
- Outside the run: a commit `215459f docs: add README with the project tag-line, packages, and setup` landed on this branch during group 2. No agent of this run made it.
- Task 1.3 change: xunit.v3 4.0.2 rejects `CollectionBehavior(DisableTestParallelization = true)`. `AssemblyInfo.cs` holds `[assembly: Parallelization(Mode = ParallelMode.None)]`, with the same effect.
- Phase 2 group 1 is done (tasks 2.1, 2.2, 2.4, and 2.5). The decisions and the notes for group 2 are in the section "Phase 2 Group 1: Decisions".
- The orchestrator added the `wrapWidth` parameter to the Added line of task 5.5, and it changed task 5.6 to keep the demo lines that the run adds to `human-testing.md` after each slice (the called mode).
- Phase 2 group 2 is done (task 2.3). The size-0 check, the decisions, the blast radius, and the notes for group 3 are in the section "Phase 2 Group 2: Decisions".
- Phase 2 group 3 is done (tasks 2.6 to 2.9). The demo lines, the real ImGui results, and the decisions are in the section "Phase 2 Group 3: Decisions". No fault of `src/` showed.
- Phase 3 group 1 is done (tasks 3.1 and 3.2). The decisions, the blast radius, and the notes for group 2 are in the section "Phase 3 Group 1: Decisions".
- Phase 3 group 2 is done (tasks 3.3 to 3.7). The demo dump, the Vulkan signatures, the decisions, and the notes for phase 4 are in the section "Phase 3 Group 2: Decisions". Phase 3 is complete.

### 2026-10-20 07:20 (resumed run)
- The user paused the run part-way in phase 4. `/autorun-epic` started a new orchestrator on the same branch and the same working tree. It did not redo phases 1 to 3.
- The resume brief `phase-4-resume.md` checked each task of phase 4 first. The code in `src/` was complete and right. Six tests had wrong expected values, and the rect tests held debug code. The agent fixed the tests and removed the debug code. The decisions are in the section "Phase 4: Decisions".
- Phase 4 verifier: PASS, Demo: PASS. The inbox holds no line.
- Change from the plan, kept: `Rect` skips the fill under an opaque border that covers the box. The result on screen is the same.
- Phase 5 is done in two groups. The decisions are in the sections "Phase 5 Group 1: Decisions" and "Phase 5 Group 2: Decisions". Phase 5 verifier: PASS, Demo: PASS. The inbox holds no line. Each task of `tasks.md` is done.
- The cross-phase review found 9 findings. Review group 1 found CR-10. Six review groups fixed them, one agent at a time. The decisions are in the sections "Review Group 1" to "Review Group 6". The review verifier: PASS. The total is 864, 0 failed.
- Not fixed, with a reason in `code-review.md`: CR-1 fix (2), a `catch` in `UiContext.Draw` (renderer-interface owns the walk and the frame contract), and CR-3 fix (3), an atlas check in `Add` (the service reads no ImGui state).

### 2026-10-20 18:13 (`/update-feature`)
- Progress: 40/40 tasks, all 5 phases, and the cross-phase review (10 findings, 10 resolved). The total is 864 tests, 0 failed, 0 warnings. `scripts/test-linux.sh` passes.
- Decisions: the resumed run, the kept fill skip, the review scope, and the non-finite clip rule are in "Important Decisions".
- The draft plan of label-and-image goes to `docs/features/imgui-renderer/label-and-image/implementation.md` (the `solution-architect` agent).
- Inbox: the file holds no line. No line is open, and no bug is promoted.
- Blockers: none.
- The next session: a person runs "Needs Human Verification". `/autorun-epic` commits the feature and goes on to label-and-image.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
