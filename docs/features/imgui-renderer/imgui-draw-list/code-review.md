# imgui-renderer/imgui-draw-list - Code Review

**Reviewed:** 2026-10-20
**Reviewer:** Agent (cross-phase review of phases 1 to 5)
**Rules Applied:** 1. Correctness of the mapping, 2. Zero allocation, 3. Native safety, 4. API clarity, 5. The seams, 6. Test quality, 7. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 10 findings, 10 resolved. Review groups 1 to 6 fixed each finding. Two optional parts stay out of scope with a reason (CR-1 fix 2, CR-3 fix 3). Review group 1 found CR-10 during its fix. Verifier `review-verify.md`: PASS (864 tests, 0 failed, 0 warnings, `scripts/test-linux.sh` exit 0) (2026-10-20).

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 1 |
| MEDIUM | 2 |
| LOW | 6 |

The mapping rules are right for each normal input, and the allocation gate is real for each main path. The HIGH finding is a frame state that stays open after an exception, so a later `PopClip` pops an empty ImGui clip stack. The two MEDIUM findings are native values that the renderer gives to ImGui with no check: a `NaN` clip rect, and a texture data pointer that ImGui frees. The LOW findings are edge values, test gaps, and doc gaps.

All probes ran in a scratch copy of the repository at `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/3877ec01-edbe-4549-8fe2-adfb5451e35e/scratchpad/cr-imgui-draw-list/repo`. A scratch test class `ReviewProbes` holds the probes. No file of the repository changed, except this file.

1. **Baseline.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 827, failed 0.
2. **Mutations that a shipped test kills.** One build holds three mutations: `BorderInset` without the `- 0.5f`, the `Image` texture lookup after the skip rules, and `EnsureCapacity` with `>` in place of `>=`. Six tests fail: three rows of `BorderInsetGivesHalfTheWidthMinusHalf`, `DrawsARoundedPanelWithABorder`, `ThrowsForARemovedTexture`, and `NeverReusesAnId`. Each mutation has a failed test.
3. **Mutations that no shipped test kills.** One build holds four mutations of `ImGuiRenderer.cs`: `PushClipRect` with `false` in place of `true`, `PopOpenClips` with no `Math.Min` guard, and a `null` text end in `Text` and in `MeasureText`. The 197 tests of `BrutalUi.ImGui.Tests` pass (failed 0). See CR-6.
4. **The clip depth after an exception.** In ImGui frame 1, `Begin`, two `PushClip` calls, and a `Text` call with an unknown font id (it throws). The frame has no `End`. In ImGui frame 2, with no `Begin`: a `Rect` call does not throw and draws `verts 10,10-30,30`. The first `PopClip` does not throw and pops the viewport clip (stack size 0). The second `PopClip` gives a stack size of `-1`. See CR-1.
5. **A rect with a very large or a negative size.** A negative size draws nothing (shipped tests). The rect `3e38, 0, 3e38, 10` passes `HasArea` and draws `verts 3E+38,0-Infinity,10`. See CR-4. The rect `0, 0, 1e30, 1e30` with a radius of 8 and a border of 2 draws a fringe at `-2.86` (float precision at `1e30`, not reported). A clip of `3e38, 0, 3e38, 10` gives a clip of area 0 (correct).
6. **A radius larger than the rect.** A radius of 1000 on a rect of 40 by 20 with a border of 4 gives the bounds `8.999762,8.99992-51.000237,31.00008`, inside the rect grown by 1 pixel. A radius of infinity gives the bounds `9.499959,9.499985-50.500042,30.500015`. Both are correct.
7. **A border width of `NaN` or infinity.** `NaN`, positive infinity, and negative infinity draw the fill only. `1e30` covers the box: one fill in the border color, and no fill under it. All are correct. A border of `0.25` gives the bounds `8.624996` to `51.375004` for the rect `10,10-50,30`. That is 1.375 pixels outside the rect. See CR-5.
8. **A clip with a `NaN` component.** `PushClip(NaN, 10, 50, 50)` gives the command clip `NaN,10,NaN,60`, and the rect under it draws. See CR-2.
9. **`MeasureText` after a clear of the font atlas.** After `ImFontAtlas_Clear`, `io.FontDefault` is null and the atlas holds no font. Id 0 then adds a new default font: the measure gives `35x13` before and after, and a draw works. A registered id 1 after the clear stops the process with exit code 139 (a segmentation fault). See CR-9.
10. **The font atlas as a registered texture.** The probe registers `io.Fonts.TexRef` with `Add`, then measures sizes 20 to 191 in 6 frames. ImGui grows the atlas and frees the old texture data. The stored reference then names the freed data (`stale True`, 1 texture left in the platform list). See CR-3.
11. **The default font before the game font.** A `MeasureText` call with id 0 on an empty atlas adds the ImGui default font. A game font that loads after it is not font 0 of the atlas, and id 0 does not give it. See CR-8.
12. **The gate with planted allocations.** One build plants `new object()` in `Image`, `new byte[1]` in `Text`, and `new long[2]` in `MeasureText`, each on the main path. `DirectCallsAllocateNothingAfterWarmUp` fails with `Actual: 26400`. That is 200 times 24, plus 300 times 32, plus 300 times 40, so the gate sees each plant. A second build plants `new object()` in five skip paths (the `Text` skip, the `MeasureText` empty text, the `Image` none handle, the `Rect` with no area, and `PopOpenClips`). The 3 gate tests pass. See CR-7.
13. **Rule checks.** `BrutalUi.Core.csproj` references `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization` only. No `.cs` file of `src/BrutalUi.Core` names ImGui outside a doc comment. `BrutalUi.ImGui.csproj` adds `InternalsVisibleTo` and no package. Each test creates its harness with `using`. The one test with a second context destroys it in a `finally` block.

### Known items from the build

1. **`Begin` outside an ImGui frame does not throw.** The orchestrator decided no (Open Questions of `context.md`). Not reported.
2. **`GetFont` does not check that a registered font still belongs to the current atlas.** Probe 9 adds a new angle: the effect is a stop of the process in `MeasureText`, and the doc comment names no atlas clear. See CR-9.
3. **`Remove` after the backend is disposed calls `RemoveTexture` on a disposed backend.** The doc comment states the order. Not reported.
4. **`AddVulkan` does not check the result of `backend.AddTexture`.** Not reported.
5. **A clip with an area of 0 still gets draw calls (gap 7 of the plan).** Not reported. A clip with a `NaN` component is a different fault: its area is not 0, and the Vulkan backend does not skip it. See CR-2.
6. **Mixed radii above 0 draw with the largest radius (Decision 4, gap 5).** Not reported.
7. **A draw call between `Begin` and `End` does not check the context, for speed.** Probe 4 adds a new angle: the frame state stays open after an exception, also with a live context. See CR-1.

---

## Findings

### CR-1: After an exception, the frame stays open, and `PopClip` pops an empty ImGui clip stack
**Severity:** HIGH
**Rule:** 3. Native safety (and 1. Correctness of the mapping)
**File:** `src/BrutalUi.ImGui/ImGuiRenderer.cs:154-164, 497-504`, `src/BrutalUi.Core/Tree/UiContext.cs:196-205`
**Problem:** `UiContext.Draw` calls `renderer.End()` on the normal path only. A throw inside the walk (for example `Text` with an unknown font id, or `Image` with a removed texture) leaves `_list` set and `_clipDepth` above 0. The next `Begin` clears this state, so `UiContext.Draw` alone is safe. But a direct call with no `Begin` in a later ImGui frame passes `ThrowIfNoFrame`, because the check reads `_list` only:

```csharp
private void ThrowIfNoFrame()
{
    if (_list.IsNull())
```

`PopClip` then trusts `_clipDepth` and calls the native pop with no check of the native stack:

```csharp
_clipDepth--;
Gui.PInvoke.ImDrawList_PopClipRect(_list.Ptr);
```

ImGui reset the list in the new frame, so the stack holds the viewport clip only. Probe 4 gives a stack size of 0 after the first pop and `-1` after the second pop. With a size of `-1`, ImGui reads `Data[-2]` for the current clip, and the next `PushClipRect` writes to `Data[-1]`. That is a heap write out of bounds with no signal. The class remarks and section 8 say that a draw call before `Begin` throws, but here a `Rect` draws into the new frame at the old origin. `PopOpenClips` has the `Math.Min` guard, but `PopClip` does not.
**Fix:** (1) In `PopClip`, after the depth check, read `_list._ClipRectStack.Count`. When it is 1 or less (only the viewport clip, or none), throw `InvalidOperationException` before the native call: "The clip stack of the draw list holds no clip of this renderer. Call Begin to start a frame." The read is a field read through the pointer, with no P/Invoke and no allocation. (2) In `UiContext.Draw`, call `renderer.End()` on the exception path too: a `catch` block that calls `End` and then rethrows with `throw;`. Then the stale state of each renderer ends with the failed frame. Keep `End` free of throws, so it does not hide the first exception. (3) Add a test in `ImGuiRendererRectTests`: frame 1 with `Begin`, two `PushClip` calls, and no `End`. In frame 2 with no `Begin`, `PopClip` must throw `InvalidOperationException`, and the clip stack must keep size 1. Add a Core test: a renderer that throws from `Rect` gets an `End` call after the throw.
**Effort:** Low

### CR-2: A clip rect with a `NaN` component gives a `NaN` clip to the Vulkan backend
**Severity:** MEDIUM
**Rule:** 3. Native safety (and 1. Correctness of the mapping)
**File:** `src/BrutalUi.ImGui/ImGuiRenderer.cs:140-148`, `docs/technical-design.md:375`
**Problem:** `PushClip` gives the rect to ImGui with no check:

```csharp
float2 min = new(_origin.X + rect.X, _origin.Y + rect.Y);
float2 max = new(_origin.X + (rect.X + rect.Z), _origin.Y + (rect.Y + rect.W));
Gui.PInvoke.ImDrawList_PushClipRect(_list.Ptr, &min, &max, true);
```

ImGui intersects with `if (cr.x < current.x)`. A `NaN` fails each compare, so the `NaN` stays. Probe 8 gives the command clip `NaN,10,NaN,60`. The Vulkan backend clamps the clip with the same kind of compare, and its skip test `clip_max.x <= clip_min.x` is false for `NaN`. It then casts `NaN` to `int32_t` for the scissor offset. That is undefined in C++, and on x86 it gives `INT_MIN`, a negative scissor offset that Vulkan forbids. The walk can give a `NaN` clip: `PaintWalk.Intersect` uses `Math.Max`, which keeps a `NaN` of the layout. `Rect` skips a `NaN` rect (`HasArea`), but `PushClip` does not. Section 8 says that the clip stays in the viewport, which is not true for `NaN`.
**Fix:** In `PushClip`, when a component of `rect` is `NaN`, push a clip with an area of 0: set `max` equal to `min`, and set a `NaN` coordinate of `min` to the origin. The push still happens, so the stack stays balanced, and the contract "an area of 0 draws nothing until the matching `PopClip`" applies. Add a theory `PushClipWithANaNComponentClipsEverything` with a `NaN` in each component: the dump clip has no `NaN`, and its width or height is 0. Change section 8 to name the rule.
**Effort:** Low

### CR-3: `Add` accepts texture data that ImGui frees, and a test registers the font atlas
**Severity:** MEDIUM
**Rule:** 3. Native safety (and 4. API clarity)
**File:** `src/BrutalUi.ImGui/ImGuiTextureService.cs:47-77`, `tests/BrutalUi.ImGui.Tests/ImGuiTextureServiceTests.cs:77-92`
**Problem:** `Add` stores an `ImTextureRef` with a `_TexData` pointer. The doc says "It names an ImGui texture data or holds a texture id that is not 0", and it states no lifetime for the data. `AcceptsAReferenceWithTextureData` registers `io.Fonts.TexRef` as the valid case. With the dynamic fonts of ImGui 1.92, a new font size bakes on demand, and the atlas can grow. A growth makes a new texture data and frees the old one. Probe 10 shows it: after 6 frames of measures, the stored pointer differs from the atlas pointer, and the platform list holds 1 texture. An `Image` with that handle then gives a freed `ImTextureData*` to ImGui and to the backend.
**Fix:** (1) In the `Add` doc and the class remarks, state the rule: "A reference with texture data must stay valid until `Remove`. Do not register the font atlas: ImGui replaces its texture data when the atlas grows." (2) Change `AcceptsAReferenceWithTextureData` to use texture data that the test owns, or keep the atlas and assert the rule in a comment that names it as a short-lived case. (3) Optional: reject a reference whose `_TexData` equals `ImGui.GetIO().Fonts.TexRef._TexData` when a context is current.
**Effort:** Low

### CR-4: An overflow of `x + width` passes `HasArea` and gives an infinite vertex
**Severity:** LOW
**Rule:** 1. Correctness of the mapping
**File:** `src/BrutalUi.ImGui/ImGuiMapping.cs:37-45`, `src/BrutalUi.ImGui/ImGuiRenderer.cs:195-201, 268-274`, `docs/technical-design.md:373`
**Problem:** `HasArea` checks that each component is finite. The renderer then adds the components: `_origin.X + (rect.X + rect.Z)`. Two finite values can add up to infinity. Probe 5 draws the rect `3e38, 0, 3e38, 10` with `verts 3E+38,0-Infinity,10`. The `context.md` decision for `HasArea` says that an infinite size gives infinite vertex positions, and the rule exists to stop them. Section 8 says that a rect draws nothing when a component is not finite.
**Fix:** In `Rect` and `Image`, after the computation of `min` and `max`, return when `max.X` or `max.Y` is not finite (`!float.IsFinite`). Or add a `HasFiniteBounds(in float2 min, in float2 max)` rule to `ImGuiMapping`. Add the row `3e38, 0, 3e38, 10` to `SkipsAnEmptyRect` and to `DrawsNothingForARectWithNoArea`.
**Effort:** Low

### CR-5: A border below 1 pixel spills up to 1.375 pixels outside the rect
**Severity:** LOW
**Rule:** 1. Correctness of the mapping
**File:** `src/BrutalUi.ImGui/ImGuiRenderer.cs:224-230`, `src/BrutalUi.ImGui/ImGuiMapping.cs:155-158`, `docs/technical-design.md:373`
**Problem:** `BorderInset` gives `borderWidth * 0.5f - 0.5f`, which is negative for a width below 1. The stroke rect then grows out of the box. ImGui draws a stroke of at least 1 pixel, so the stroke does not shrink to match. Probe 7 draws a border of `0.25` on the rect `10,10-50,30` with the bounds `8.624996` to `51.375004`: 1.375 pixels outside, and the rule of phase 4 allows 1 pixel. A border of 0.5 gives 1.25 pixels. Section 8 says that the fringe reaches 1 pixel outside the rect.
**Fix:** In `Rect`, compute the inset from `MathF.Max(borderWidth, 1f)`, so the inset is never below 0. ImGui draws the thin stroke as 1 pixel, so the stroke then covers `[0, 1]` inside the box. State in the `Rect` remarks that a border below 1 pixel draws as 1 pixel. Add the rows `0.25f` and `0.5f` to a border test that asserts the bounds inside the rect grown by 1 pixel.
**Effort:** Low

### CR-6: Four mutations of the renderer pass each shipped test
**Severity:** LOW
**Rule:** 6. Test quality
**File:** `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs`, `tests/BrutalUi.ImGui.Tests/ImGuiRendererTextTests.cs`
**Problem:** Probe 3 shows four gaps.
- `PushClipRect(..., false)` passes. No test pushes a clip that is larger than the viewport or the parent clip, so the rule "The `true` keeps the clip inside the viewport" has no proof.
- `PopOpenClips` with no `Math.Min` guard passes. No test makes the native stack smaller than `_clipDepth`.
- A `null` text end in `Text` and in `MeasureText` passes. Each test text is a `u8` literal, and the compiler puts a zero byte after each `u8` literal. So ImGui finds the same end with `strlen`. No test proves the plan rule "`end` is given, so ImGui needs no zero byte".
**Fix:** (1) Add `PushClipStaysInsideTheParentClip`: push `-50, -50, 1000, 1000` and assert the dump clip `0,0,400,300`. Push a second clip that is larger than the first and assert the first clip. (2) Add a test that pops a clip of the list by hand inside a frame, then calls `End` or `Begin`, and asserts that the stack size stays at 0 or more. (3) Add `MeasuresAndDrawsASpanWithNoZeroByte`: slice `"Hello, brutal-ui"u8[..5]`, assert that the measure equals the measure of `"Hello"u8` (`35x13` at size 13), and assert 30 elements for the draw.
**Effort:** Low

### CR-7: The allocation gate covers no skip path
**Severity:** LOW
**Rule:** 2. Zero allocation (and 6. Test quality)
**File:** `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs:139-174`
**Problem:** `DirectFrame` runs each member on its main path only. The 1000-element tree draws rects with an area, and it calls no `Text` or `Image`. Probe 12 plants an allocation in five skip paths: the `Text` skip, the `MeasureText` empty text, the `Image` none handle, the `Rect` with no area, and `PopOpenClips`. The 3 gate tests pass. A real UI takes these paths each frame: a label with no text, a faded label with an alpha of 0, an image with no texture, an element of size 0, and a frame that ends with an open clip.
**Fix:** Add these calls to the measured region of `DirectFrame`: `Text` with an alpha of 0, `Text` with an empty span, `MeasureText` with an empty span, `Image` with the none handle, `Rect` with a width of 0, and one `PushClip` with no `PopClip` before `End` (the `End` pop path). Keep the warm-up frames the same as the measured frames.
**Effort:** Low

### CR-8: `GetFont` adds the ImGui default font before the game loads its fonts
**Severity:** LOW
**Rule:** 5. The seams (and 4. API clarity)
**File:** `src/BrutalUi.ImGui/ImGuiFontService.cs:81-131`, `docs/features/imgui-renderer/imgui-draw-list/context.md:486`
**Problem:** Id 0 on an empty atlas calls `ImFontAtlas_AddFontDefault` (line 124). The new font becomes font 0 of `io.Fonts`. When the game loads its TTF after that, the game font is font 1. Then ImGui and id 0 use the ImGui default font, not the game font (probe 11). A measure from a markup load or a first `Update` before the font load causes it. The doc comment says that the method adds the default font, but not that the font stays the default for ImGui. The hand-off to hello-panel-sample gives a create order with no font step.
**Fix:** In the `GetFont` remarks and the class remarks, add: "Load the fonts of the game, or set `io.FontDefault`, before the first call with id 0. Else the ImGui default font becomes font 0 of the atlas, and it stays the default font of ImGui." Add the font step to the hand-off line of `context.md`: `ImGui.CreateContext`, the font loads, then the services and the renderer, then the backends.
**Effort:** Low

### CR-9: A registered font after an atlas clear stops the process, and the doc does not name the clear
**Severity:** LOW
**Rule:** 3. Native safety (and 4. API clarity)
**File:** `src/BrutalUi.ImGui/ImGuiFontService.cs:16-20, 98-110`
**Problem:** This is known item 2 with a new angle. Probe 9 shows that a `MeasureText` call with a registered id after `io.Fonts.Clear()` stops the test process with exit code 139. The effect is not a wrong glyph. It is a stop of the game with no managed exception. The class remarks name `ImGui.DestroyContext` only: "A font stays valid only while its context lives". They do not name `Clear`, `ClearFonts`, or `RemoveFont`. Id 0 recovers after a clear, because ImGui resets `io.FontDefault` and the method adds a new default font.
**Fix:** Add to the class remarks: "Do not call `io.Fonts.Clear()`, `ClearFonts()`, or `RemoveFont` for a font that the service holds. A registered id then names freed memory, and a call with it stops the process. Id 0 works after a clear." The runtime check stays out of scope (known item 2).
**Effort:** Low

### CR-10: `PopOpenClips` can pop the viewport clip
**Severity:** LOW
**Rule:** 3. Native safety (and 1. Correctness of the mapping)
**File:** `src/BrutalUi.ImGui/ImGuiRenderer.cs` (`PopOpenClips`)
**Problem:** Review group 1 found this while it fixed CR-1. `PopOpenClips` pops `Math.Min(_clipDepth, _ClipRectStack.Count)` clips. When the stack size equals the depth (for example, the game popped a clip of the list by hand), `End` also pops the viewport clip, which the renderer did not push. ImGui is safe at size 0 (the clip falls back to the full-screen clip), so this is no write out of bounds. But the list then loses its base clip for the rest of the frame.
**Fix:** Limit the pops to `Math.Min(_clipDepth, _ClipRectStack.Count - ViewportClipCount)`, and never below 0. Add a test: pop a clip of the list by hand inside a frame, call `End`, and assert that the stack keeps size 1.
**Effort:** Low

---

## What's Already Good

- Each draw path checks its misuse before the native call: the frame, the depth, the empty span, the size guard, and the unknown handle. The size guard stops the segmentation fault of a negative font size.
- `Begin` and `End` compare the list pointer and the ImGui frame count before a pop, and the frame count check runs before the list call that resets the list. `PopOpenClips` limits the pops to the native stack size.
- The rules of `ImGuiMapping` are pure, small, and tested with exact values, also for `NaN` and infinity. The edge values of the probes (a radius above the rect, a radius of infinity, a border of `NaN`, infinity, or `1e30`) give correct results.
- The allocation gate is real for each main path: a plant in `Image`, `Text`, and `MeasureText` gives exactly the expected bytes. `TheMeasuredDrawDoesRealWork` stops a gate that draws nothing.
- The harness checks a leaked context, renders an open frame before `DestroyContext`, and acts as a null texture backend. Each test disposes it with `using`.
- `Remove` marks the entry before the backend call, and ids never repeat, so a stale texture handle throws and never names a new texture.
- `BrutalUi.Core` keeps no ImGui reference. `TextureHandle` keeps the one-argument constructor and the id-only equality, so no Core golden line changed.
- Section 8, `CHANGELOG.md`, and the doc comments state the changes from the plan (`HasArea`, the size guard, the position skip, the covered fill, and the `AddVulkan` check order).

---

## Recommended Fix Order

| Order | Finding | File | Severity | Effort |
|-------|---------|------|----------|--------|
| 1 | CR-1: The open frame after an exception and the unguarded `PopClip` | `src/BrutalUi.ImGui/ImGuiRenderer.cs` | HIGH | Low |
| 2 | CR-2: A `NaN` clip reaches the Vulkan backend | `src/BrutalUi.ImGui/ImGuiRenderer.cs` | MEDIUM | Low |
| 3 | CR-3: Texture data that ImGui frees | `src/BrutalUi.ImGui/ImGuiTextureService.cs` | MEDIUM | Low |
| 4 | CR-4: The overflow of `x + width` | `src/BrutalUi.ImGui/ImGuiRenderer.cs` | LOW | Low |
| 5 | CR-5: A border below 1 pixel | `src/BrutalUi.ImGui/ImGuiRenderer.cs` | LOW | Low |
| 6 | CR-6: Four surviving mutations | `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs` | LOW | Low |
| 7 | CR-7: The gate covers no skip path | `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs` | LOW | Low |
| 8 | CR-8: The default font before the game fonts | `src/BrutalUi.ImGui/ImGuiFontService.cs` | LOW | Low |
| 9 | CR-9: The atlas clear in the font doc | `src/BrutalUi.ImGui/ImGuiFontService.cs` | LOW | Low |

CR-1 comes first, because it is a native write out of bounds. CR-6 adds the tests for the guards of CR-1 and CR-2, so run it after them. CR-1 also changes `UiContext.Draw` in `BrutalUi.Core`, so run the Core tests after it.

---

## Progress

- [x] CR-1: After an exception, the frame stays open, and `PopClip` pops an empty ImGui clip stack. Fixed in review group 1: `PopClip` throws before the native pop when the list holds only the viewport clip, and `PopClipInALaterImGuiFrameThrows` proves it. Fix (2), a `catch` in `UiContext.Draw` that calls `End`, is not done: out of scope. `UiContext.Draw` belongs to renderer-interface, the change alters the `IUiRenderer` frame contract, and the `PopClip` guard closes the native write. A stale frame on a list that ImGui has not reset yet still passes the frame check of a draw call. Its pops stay in bounds. A frame-count check costs one P/Invoke for each call.
- [x] CR-2: A clip rect with a `NaN` component gives a `NaN` clip to the Vulkan backend. Fixed in review group 1: a non-finite clip (`NaN`, infinity, or an overflow) becomes a clip with an area of 0. Section 8 names the rule.
- [x] CR-3: `Add` accepts texture data that ImGui frees, and a test registers the font atlas. Fixed in review group 6: the class remarks and the `Add` doc state the lifetime rule and name the font atlas, and the test uses texture data that it owns. Fix (3), a check against the atlas, is not done: out of scope. The service reads no ImGui state, so each member works with no context.
- [x] CR-4: An overflow of `x + width` passes `HasArea` and gives an infinite vertex. Fixed in review group 1: `ImGuiMapping.HasFiniteBounds` in `Rect` and `Image`.
- [x] CR-5: A border below 1 pixel spills up to 1.375 pixels outside the rect. Fixed in review group 2: the inset uses `max(borderWidth, 1)`.
- [x] CR-6: Four mutations of the renderer pass each shipped test. Fixed in review group 3: three new tests kill each mutation.
- [x] CR-7: The allocation gate covers no skip path. Fixed in review group 4: `DirectFrame` runs 11 skip paths, and a plant in a skip path fails the gate.
- [x] CR-8: `GetFont` adds the ImGui default font before the game loads its fonts. Fixed in review group 5: the doc comments and the hello-panel-sample hand-off state the font load order.
- [x] CR-9: A registered font after an atlas clear stops the process, and the doc does not name the clear. Fixed in review group 5: the class remarks state the atlas clear rule.
- [x] CR-10: `PopOpenClips` can pop the viewport clip (found by review group 1). Fixed in review group 2: `PopOpenClips` never pops the viewport clip, and two tests prove it.
