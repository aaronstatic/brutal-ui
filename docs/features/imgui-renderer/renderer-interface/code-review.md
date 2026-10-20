# imgui-renderer/renderer-interface - Code Review

**Reviewed:** 2026-10-20
**Reviewer:** Agent (cross-phase review of phases 1 to 4)
**Rules Applied:** 1. Correctness of the walk, 2. Zero allocation, 3. API clarity, 4. The seams, 5. Test quality, 6. `AGENTS.md`, 7. Docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 11 findings, 11 resolved (8 fixed in review groups 1 to 4, CR-11 fixed by the orchestrator, CR-2 routed to a user decision, CR-10 routed to the label-and-image hand-off). Verifier `review-verify.md`: PASS (623 tests, 0 failed, 0 warnings) (2026-10-20).

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 2 |
| LOW | 9 |

No probe found a bug in the walk. The two MEDIUM findings are contract gaps that a sibling feature hits. The LOW findings are test gaps, stale doc lines, and open contract points.

All probes ran in a scratch copy of the repository at `/tmp/claude-1000/cr-renderer-interface/repo`. No file of the repository changed, except this file.

1. **Baseline.** The scratch copy builds with 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 619, failed 0.
2. **Walk probes (a scratch test class `ReviewProbes`).** All six passed.
   - A child with an opacity of 2 under a parent of 0.5 gets alpha `80` (128). A child with an opacity of `NaN` under the same parent also gets `80`. The clamp works.
   - A `NaN` viewport with a percent root and an auto root gives finite rects and clips (`Rect 0,0,30,20`, `PushClip 0,0,30,20`). No `NaN` reaches the renderer.
   - A renderer that throws from the 35th `Rect` of a 41-level clipped chain leaves 34 clips with no pop. After the throw, the frame stack (length 64) holds no element reference. The next `Draw` gives the full list and a clip depth of 0.
   - A hook that throws after the clip push leaves a clip depth of 2 in `RecordingRenderer`. The next `Draw` gives a correct list, because `Begin` resets the depth.
   - A border of 8 on a 10 by 10 box gives `Rect 0,0,16,16` and `PushClip 8,8,0,0`. No clip size is negative.
   - An opacity of `1e-6` gives no `Rect`, but the walk still pushes the clip and calls the hook.
3. **Mutation of the walk (shipped tests only, 617 tests of `BrutalUi.Core.Tests`).** One build removes the clamp above 1, the `Array.Clear` of the `finally`, and the `top = default` of the pop. Each shipped test passes (failed 0). See CR-4 and CR-5.
4. **The gate is real.** A planted `GC.KeepAlive(new object())` in `PaintWalk.Enter`, for the elements named `settings-row` only, fails `DrawAllocatesNothingAfterWarmUp` with `Actual: 85848`. The measured region thus walks the visible settings panel. The restore gives a file that is the same as the original (`diff` gives no output).
5. **Recursion probe.** A recursive copy of `Enter` draws a chain of 5000 levels on the test thread in Debug and in Release with no overflow. On a thread with a 64 KB stack, the same recursive walk stops the process with `Stack overflow.` (exit 134). `UiContext.Draw` on a thread with a 64 KB stack draws the 5000-level chain with no overflow. See CR-3.
6. **Rule checks.** The grep of Q4 on `tests/BrutalUi.Core.Tests/Shared/*.cs` finds no hit. `BrutalUi.Core.csproj` holds 2 `PackageReference` items. No `.cs` source file of `src/BrutalUi.Core` names ImGui outside a doc comment. `Directory.Packages.props`, `scripts/`, and each `.csproj` file do not differ from `main`. The STE lint gives 1.15 for `docs/technical-design.md`, 1.61 for `CHANGELOG.md`, and 1.91 for `human-testing.md` (each below 2.5).

### Known items from the build

1. **Section 14 names a "draw command list".** Confirmed at `docs/technical-design.md:518`. The walk builds no list. See CR-6.
2. **Gaps 8 and 9 of the plan (no clip cull, a clip push on a leaf).** Out of scope. The tiny-opacity probe shows one more case of the same kind (a clip push and a hook call for an element with no visible pixel). Not reported.

---

## Findings

### CR-1: The contract does not say when a caller can call `MeasureText`
**Severity:** MEDIUM
**Rule:** 4. The seams (and 3. API clarity)
**File:** `src/BrutalUi.Core/Rendering/IUiRenderer.cs:24-36, 91-99`
**Problem:** The summary says "One frame is a `Begin` call, other calls, and an `End` call". The `Begin` doc says "One `Begin` call comes before each other call of a frame". label-and-image calls `MeasureText` from the measure callback of `Label`, and the callback runs in `UiContext.Update`. `Update` runs before `Draw`, so the call comes before `Begin` and outside each frame. imgui-draw-list uses `PushFont(font, size)` of the dynamic font system for `MeasureText`. Thus imgui-draw-list does not know from the doc comments if `MeasureText` must work outside a `Begin` and `End` pair. Also, no API gives the label a renderer during `Update`. `MeasureCallback` gets the `Element` only, and `Update(float)` takes no renderer. The plan names this route as an open hand-off, but `context.md` lists no open question (CR-11).
**Fix:** In the doc comment of `MeasureText`, state the contract: "The caller can call this method at any time on the main thread, also outside a `Begin` and `End` pair, for example from a measure callback during `UiContext.Update`. The call does not depend on the clip stack." Change the `Begin` sentence to "One `Begin` call comes before each draw call of a frame" and name `MeasureText` as the exception. Record the route question (a measurer reference on the label, or a renderer slot on `UiContext`) in the Open Questions of `context.md` for a user decision before label-and-image starts.
**Effort:** Low

### CR-2: `TextureHandle` carries no size, and `IUiRenderer` has no size query
**Severity:** MEDIUM
**Rule:** 4. The seams (and 3. API clarity)
**File:** `src/BrutalUi.Core/Rendering/RenderHandles.cs:51-98`
**Problem:** The label-and-image requirements say "`Image` sets the measure callback from the texture size". `Image` lives in `BrutalUi.Core`. It holds a `TextureHandle`, which is one `int` id. The texture service that knows the size lives in `BrutalUi.ImGui`, and `BrutalUi.Core` cannot reference it. `IUiRenderer` has no member that gives the size of a texture. Thus `Image` cannot get its natural size from the public contract. The fix changes a public struct or the interface, and the plan says that a public signature is hard to change after the siblings consume it.
**Fix:** Get a user decision before label-and-image starts, and record it in `context.md`. Option (a): `TextureHandle` gets an `int2 Size` (a constructor `TextureHandle(int id, int2 size)`), and the texture service returns a handle with its size. `Image` then reads the size with no renderer reference. Option (b): `Image` takes an explicit natural size from the caller, and the handle does not change. Option (c): `IUiRenderer` gets `int2 GetTextureSize(TextureHandle texture)`, with the same route problem as CR-1. Option (a) keeps the interface the same and adds no route.
**Effort:** Low

### CR-3: The 5000-level test does not prove that the walk has no recursion
**Severity:** LOW
**Rule:** 5. Test quality
**File:** `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs:673-700`
**Problem:** `DrawsAChainOf5000LevelsWithNoOverflow` runs on a test thread with the default stack. The recursion probe shows that a recursive copy of `Enter` also draws 5000 levels on that thread, in Debug and in Release. Thus the test passes for a recursive walk too, and only the grep of Q3 guards A8.
**Fix:** Run the draw on a thread with a small stack, for example `new Thread(() => context.Draw(renderer), 64 * 1024)`, then `Start` and `Join`, and assert 5000 `Rect` calls after the join. The probe shows that the explicit walk passes on a 64 KB stack, and that a recursive walk stops the process. A stop of the test host is a failed run, so the test then guards A8.
**Effort:** Low

### CR-4: No test covers the opacity clamp above 1
**Severity:** LOW
**Rule:** 5. Test quality (and 1. Correctness of the walk)
**File:** `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs:417-490`
**Problem:** Rule 1 names the clamp. The tests cover a negative opacity and a `NaN` opacity on a root only. No test sets a value above 1. The mutation probe removes `|| own > 1f` from `PaintWalk.Enter`, and each shipped test passes. With that mutation, a child with an opacity of 2 under a parent of 0.5 draws with alpha 255 in place of 128. No test sets `NaN` under a faded parent either.
**Fix:** Add `ClampsAnOpacityAboveOne`: a root with an opacity of 0.5, a child with an opacity of 2, and a second child with an opacity of `NaN`. Assert the golden list with fill alpha `80` on the root and on each child (the probe gives these lines).
**Effort:** Low

### CR-5: No test checks the exception path or the release of the frame references
**Severity:** LOW
**Rule:** 5. Test quality (and 1. Correctness of the walk)
**File:** `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs:296-320`
**Problem:** `ThrowsForADrawDuringADraw` covers a throw from `Begin` only, before the walk starts. No test throws from the renderer at a depth above 0, or from a hook after the clip push. No test checks that the stack keeps no element reference after a draw. The mutation probe removes `Array.Clear` from the `finally` of `PaintWalk.Walk` (line 120) and `top = default` from the pop (line 109), and each shipped test passes. With the mutation, the stack keeps a removed root and its subtree alive until a later draw writes over the slots. The scratch probes show that the code is correct today.
**Fix:** Add `RecoversAfterTheRendererThrowsInTheWalk`: a 40-level clipped chain, a renderer that throws from the 35th `Rect`, then a `Draw` with a `RecordingRenderer`. Assert the full golden count and a clip depth of 0. Add `RecoversAfterAHookThrows` with a probe element that throws from `PaintContent`. For the reference rule, draw a root, remove it, drop the local, draw again, run `GC.Collect()`, and assert that a `WeakReference` to the root is dead. The test must keep the root out of each other reference (the element creation in a separate method with `[MethodImpl(MethodImplOptions.NoInlining)]`).
**Effort:** Medium

### CR-6: Section 14 says that the renderer tests assert a draw command list
**Severity:** LOW
**Rule:** 7. Docs
**File:** `docs/technical-design.md:518`
**Problem:** "What is not tested" says: "Pixel output. The renderer tests assert the draw command list, not the image." The walk builds no command list. Section 4 now says so (line 149: "It builds no command list"). The tests assert the call list of `RecordingRenderer`. The line contradicts section 4.
**Fix:** Change the line to: "Pixel output. The walk tests assert the call list of `RecordingRenderer`, not the image."
**Effort:** Low

### CR-7: Sections 6, 7, and 8 of the technical design state three wrong facts
**Severity:** LOW
**Rule:** 7. Docs
**File:** `docs/technical-design.md:253, 320, 368`
**Problem:**
- Line 320 says "A write calls `MarkDirty(DirtyFlags.Paint)`". A write of a paint member calls nothing. The doc of `ComputedStyle` (line 24) says that the caller calls `MarkDirty` after the write. A reader of section 7 expects an automatic mark.
- Line 253 says "The inputs are the 25 public fields of `ComputedStyle`". `ComputedStyle` now has 28 public fields and the `Opacity` property. The 25 fields are still the layout inputs, but the sentence counts all public fields.
- Line 368 says "`Opacity` is a property with a default of 1. It clamps to 0 to 1". The property does not clamp. A set value reads back exactly (`OpacityReadsBackTheSetValue`). The walk clamps.
**Fix:** Line 320: "A write sets no dirty bit. After a write, the caller calls `MarkDirty(DirtyFlags.Paint)`." Line 253: "The layout inputs are 25 public fields of `ComputedStyle`". Line 368: "The walk clamps the value to 0 to 1, and `NaN` counts as 1."
**Effort:** Low

### CR-8: Three doc comments name a stale or partial clip rule
**Severity:** LOW
**Rule:** 7. Docs (and 3. API clarity)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:169-170`
**Problem:**
- The remarks of `UiContext.Draw` say "An element with `Overflow.Hidden` clips its children to its padding box". `Overflow.Scroll` also clips (Decision 6), and the clip also applies to the content of the hook.
- `ComputedStyle.cs:276`, the remarks of `Overflow`, say "The renderer epic clips the content." This feature is that work. The draw walk now clips to the padding box.
- `ComputedStyle.cs:4`, the class summary, says that the struct "holds the layout inputs". It now also holds the four paint members.
**Fix:** `UiContext.cs:169-170`: "An element with an overflow other than `Overflow.Visible` clips its content and its children to its padding box and to the clip of its parent." `ComputedStyle.cs:276`: "The draw walk clips the content of `Hidden` and `Scroll` to the padding box." `ComputedStyle.cs:4`: "holds the layout inputs and the paint members."
**Effort:** Low

### CR-9: Four points of the renderer contract are open for imgui-draw-list
**Severity:** LOW
**Rule:** 4. The seams (and 3. API clarity)
**File:** `src/BrutalUi.Core/Rendering/IUiRenderer.cs:20-36, 61-79`
**Problem:** The plan says that imgui-draw-list implements the interface from its doc comments alone. Four points are open:
- **Begin after an aborted frame.** `UiContext.Draw` says "the next call starts a new frame" after an exception. The probes leave 34 and 2 clips with no pop and no `End`. The `Begin` doc does not say that `Begin` resets the clip stack and the frame state.
- **Scale.** The `scale` doc says "The display scale. The value is 1 in this release." It does not say if the rects are already in framebuffer pixels or if the renderer multiplies them by `scale`.
- **Radius and rect ranges.** The walk cleans the border width (0 or more, pixels only) and the opacity, but it passes `CornerRadius` with no change. The doc does not say how a renderer treats a negative radius, a `NaN` radius, or a radius above half the side. The `Image` doc calls `rect` "The border box", but label-and-image draws an image in a rect that its scale mode gives, which is not always the border box.
- **Allocation.** The summary says "The allocation test of the draw pass guards it". The gate covers `RecordingRenderer` only. imgui-draw-list has no test that needs no GPU (A6), so no test guards the ImGui renderer.
**Fix:** Add to the `Begin` doc: "`Begin` starts a clean frame. It clears each clip of an earlier frame that had no `End`." Add to the `scale` doc: "The rects are in viewport pixels. A renderer multiplies them by `scale` to get framebuffer pixels." (or the other rule, by a user decision). Add to the `radius` docs: "A value of 0 or less, or `NaN`, gives a square corner. The renderer limits a radius to half the shorter side." Change the `Image` rect doc to "The rect of the image: x, y, width, and height in viewport pixels." Change the allocation sentence to "`ThousandElementAllocationTests` guards the walk with `RecordingRenderer`. Each other implementation needs its own check."
**Effort:** Low

### CR-10: The paint hook gets the border box only, and a label cannot always find its content box
**Severity:** LOW
**Rule:** 4. The seams
**File:** `src/BrutalUi.Core/Tree/Element.cs:208-234`
**Problem:** `PaintContent` gets the border box. The hand-off says that the label resolves its own padding and border for the text position. A percent padding resolves against the inner width of the owner (`LayoutMath.LeadingPadding(style, axis, widthSize)`). For a root, the owner is the viewport. An element holds no context reference, and `LayoutNode` stores `Rect` only. Thus a root `Label` with a percent padding cannot find its content box in the hook. A child label must walk up and resolve the padding and border of its parent too. The hook is internal, so a later change is cheap.
**Fix:** Let the layout pass store the resolved padding and border of each element in `LayoutNode` (Yoga keeps both in its layout), and pass the content box to the hook as a second argument: `PaintContent(IUiRenderer renderer, in float4 borderBox, in float4 contentBox, float opacity)`. Else, record the limit (no percent padding on a root label) in the label-and-image hand-off.
**Effort:** Medium

### CR-11: `context.md` shows a stale state and no open question
**Severity:** LOW
**Rule:** 7. Docs
**File:** `docs/features/imgui-renderer/renderer-interface/context.md:3-5, 140-152`
**Problem:** The header says "Last Updated: 2026-10-20 03:10", "Current Phase: Phase 1", and "Status: In Progress". The Quick Status shows the four phases as done. "Next Steps" lists phases 1 to 4 as immediate work. "Open Questions" says "none", but the hand-offs hold an open public API question (the measure route of CR-1), and CR-2 adds one more. `AGENTS.md` and the agent rules ask for open questions in `context.md`.
**Fix:** Set the header to the review state (all four phases done, the review open). Replace "Next Steps" with the fix of this review. Add CR-1 (the measure route) and CR-2 (the texture size) to "Open Questions".
**Effort:** Low

---

## What's Already Good

- The walk follows the plan step by step. The enter order is the hidden check, the opacity, the `Rect`, the clip, the hook, and the frame push. The code copies the values of the frame and moves `NextChild` before each call to `Enter`, so no stale `ref` survives a growth.
- The clip math keeps min-max bounds and clamps an empty result to a size of 0. Infinite root bounds make a `NaN` viewport safe (probe 2).
- `default(ComputedStyle)` is visible. The `Opacity` property with a flag gives a default of 1 and an exact read-back, and its `readonly get` avoids a defensive copy through `ref readonly`.
- The equality members of `Rgba8`, `FontHandle`, and `TextureHandle` are consistent: `==`, `!=`, `Equals`, and `GetHashCode` use the same value. `Corners` has no equality members, the same as `LengthEdges`.
- The gate is real. A planted allocation in the walk for the settings rows fails it with 85,848 bytes, so the measured region walks the visible panel. `TheDrawStepEmitsEachPaintedElement` proves the real work with an independent count.
- The golden lists are exact strings. No test depends on another test. Each test builds its own context and renderer.
- `RecordingRenderer` checks the clip balance in `PopClip` and `End`, so a walk bug fails the test that finds it. It holds no test framework type and no internal member, and the benchmark project builds with it.
- `BrutalUi.Core` keeps 2 package references and no ImGui reference. No package, script, or project file changed.

## Recommended Fix Order

| Order | Finding | File | Severity | Effort |
|-------|---------|------|----------|--------|
| 1 | CR-2: `TextureHandle` carries no size | `src/BrutalUi.Core/Rendering/RenderHandles.cs` | MEDIUM | Low |
| 2 | CR-1: The call window of `MeasureText` | `src/BrutalUi.Core/Rendering/IUiRenderer.cs` | MEDIUM | Low |
| 3 | CR-11: Stale `context.md` and no open question | `docs/features/imgui-renderer/renderer-interface/context.md` | LOW | Low |
| 4 | CR-9: Four open contract points | `src/BrutalUi.Core/Rendering/IUiRenderer.cs` | LOW | Low |
| 5 | CR-10: The hook gets the border box only | `src/BrutalUi.Core/Tree/Element.cs` | LOW | Medium |
| 6 | CR-3: The 5000-level test does not prove A8 | `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs` | LOW | Low |
| 7 | CR-4: No test of the clamp above 1 | `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs` | LOW | Low |
| 8 | CR-5: No test of the exception path and the frame release | `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs` | LOW | Medium |
| 9 | CR-6: Section 14 names a command list | `docs/technical-design.md` | LOW | Low |
| 10 | CR-7: Three wrong facts in sections 6, 7, and 8 | `docs/technical-design.md` | LOW | Low |
| 11 | CR-8: Stale clip rule in three doc comments | `src/BrutalUi.Core/Tree/UiContext.cs` | LOW | Low |

CR-2 and CR-1 come first, because they need a user decision before label-and-image and imgui-draw-list start. CR-11 records those decisions.

## Progress

- [x] CR-1: The contract does not say when a caller can call `MeasureText`. Fixed in review group 1 (the doc contract). The route question is open in `context.md`.
- [x] CR-2: `TextureHandle` carries no size, and `IUiRenderer` has no size query. Not fixed: each option changes a public value type or the interface of the accepted plan, and the run cannot ask the user. Routed to a user decision: Open Questions of `context.md` and the open scope questions of the epic run. The draft plan of imgui-draw-list names the option that it assumes.
- [x] CR-3: The 5000-level test does not prove that the walk has no recursion. Fixed in review group 2.
- [x] CR-4: No test covers the opacity clamp above 1. Fixed in review group 2.
- [x] CR-5: No test checks the exception path or the release of the frame references. Fixed in review group 2.
- [x] CR-6: Section 14 says that the renderer tests assert a draw command list. Fixed in review group 3.
- [x] CR-7: Sections 6, 7, and 8 of the technical design state three wrong facts. Fixed in review group 3.
- [x] CR-8: Three doc comments name a stale or partial clip rule. Fixed in review group 4.
- [x] CR-9: Four points of the renderer contract are open for imgui-draw-list. Fixed in review group 1 (the `Begin`, `scale`, `radius`, `Image` rect, and allocation docs).
- [x] CR-10: The paint hook gets the border box only, and a label cannot always find its content box. Not fixed: out of scope. The fix changes the layout pass and the internal hook, and only `Label` of label-and-image needs it. The limit and both options are in the label-and-image hand-off of `context.md`.
- [x] CR-11: `context.md` shows a stale state and no open question. Fixed by the orchestrator: the header, the Next Steps, and two Open Questions (CR-1 route, CR-2).
