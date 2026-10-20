# Renderer Interface - Implementation Plan

**Epic:** imgui-renderer (feature 1 of 4)
**Status:** Ready for Review
**Started:** 2026-10-20
**Target Completion:** TBD
**Last Updated:** 2026-10-20 04:25

---

## Quality Bar

**Target quality:** A core library contract. Three later features and two later epics build on the interface, the value types, and the walk. A public signature is hard to change after imgui-draw-list and label-and-image consume it. A wrong walk draws wrong pixels in each later sample, and no pixel test catches it.

- **Correctness:** The walk emits the exact call list that the rules give: tree order, viewport pixels, the padding-box clip intersected with the parent clip, the skip of a hidden subtree, and the opacity product in each alpha. A golden call list test proves each rule.
- **Zero allocation:** `UiContext.Draw` allocates 0 bytes after warm-up on the 1000-element tree. `ThousandElementAllocationTests` gates it in `dotnet test`. The proof with a temporary allocation shows that the gate blocks.
- **API clarity:** Each public member has an XML doc comment that states the units, the default, and the contract. `IUiRenderer` takes spans and structs only. `default(ComputedStyle)` stays a visible, valid style.
- **No stack overflow:** The walk uses an explicit frame stack. A chain of 5000 levels draws with no overflow.
- **No ImGui reference:** `BrutalUi.Core` keeps its two package references.

---

## Executive Summary

This feature adds the draw half of the frame. `BrutalUi.Core` gets `IUiRenderer` (section 8 of the technical design), four value types (`Rgba8`, `Corners`, `FontHandle`, `TextureHandle`), four paint members on `ComputedStyle` (`BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`), and `UiContext.Draw(IUiRenderer)`. An internal static `PaintWalk` class walks each root with an explicit frame stack that `UiContext` owns. The walk emits one `Rect` for each element with a visible background or border, pushes a clip for an element with overflow hidden, skips a hidden subtree, and multiplies the opacity down the tree. An internal virtual hook, `Element.PaintContent`, lets label-and-image emit text and images between the background and the children. A `RecordingRenderer` in `tests/BrutalUi.Core.Tests/Shared/` stores each call. The walk tests and the later label-and-image tests use it. The allocation gate gets a draw test on the 1000-element tree. No package, no native dependency, and no ImGui reference change.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (sets paint fields inline and calls `Draw` each frame after `Update`)
- [x] **The renderer author** (imgui-draw-list now, a Vulkan renderer later: implements `IUiRenderer` from its doc comments alone)
- [x] **The sibling feature agents** (label-and-image overrides the paint hook and reuses the recording renderer, hello-panel-sample calls `Draw`)
- [ ] **The markup author** (not served. The style cascade and the markup come in later epics)

**User Subsets Served:**
- [x] **Simple** - Set `element.Style.BackgroundColor = new Rgba8(30, 30, 40)`, call `context.Update(dt)` and `context.Draw(renderer)`.
- [x] **Complex** - Nested clips, opacity on a subtree, a hidden overlay, and a tree deeper than the call stack allows.

**UX Approach:**
- A paint field write is a plain field or property write through `ref ComputedStyle`, the same as a layout field.
- `Draw` throws `ArgumentNullException` for a null renderer and `InvalidOperationException` for a call during a draw. It throws nothing else of its own.

---

## Goals

### Primary Goals
1. Define the renderer contract in `BrutalUi.Core` with no ImGui reference, so imgui-draw-list implements it with no change.
2. Ship `UiContext.Draw` with the confirmed rules: tree order, viewport pixels, the padding-box clip, the hidden skip, and the opacity product.
3. Keep the frame loop free of managed allocation: gate `Draw` on the 1000-element tree.
4. Give label-and-image a paint hook and a reusable recording renderer.

### Success Criteria
- [ ] The golden call list of the three-element demo tree matches, and the test prints it.
- [ ] The walk tests cover the paint order, the offsets, the clip push and pop, the clip intersection, the hidden skip, the opacity product, the border rule, the dirty bits, and a 5000-level chain.
- [ ] `DrawAllocatesNothingAfterWarmUp` measures 0 bytes over 100 frames.
- [ ] `BrutalUi.Core.csproj` still holds 2 `PackageReference` items and no ImGui text.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Rendering/                                   # new folder, namespace BrutalUi.Core
│   ├── IUiRenderer.cs                           # phase 1: the interface of section 8
│   ├── Rgba8.cs                                 # phase 1 (phase 2 adds ScaleAlpha)
│   ├── Corners.cs                               # phase 1
│   ├── RenderHandles.cs                         # phase 1: FontHandle and TextureHandle
│   └── PaintWalk.cs                             # phase 1 (phase 2 adds hidden and opacity, phase 4 the hook)
└── Tree/
    ├── ComputedStyle.cs                         # phase 1: BackgroundColor. Phase 2: BorderColor, CornerRadius, Opacity
    ├── Element.cs                               # phase 4: PaintContent hook
    └── UiContext.cs                             # phase 1: Draw and the frame stack
tests/BrutalUi.Core.Tests/
├── Shared/RecordingRenderer.cs                  # phase 1: compiled by the tests, linked by the benchmarks
├── Shared/ThousandElementTree.cs                # phase 3: paint fields on the sections
├── ThousandElementAllocationTests.cs            # phase 3: the draw gate
└── Rendering/
    ├── RenderValueTests.cs                      # phase 1 (phase 2 adds the style defaults)
    ├── PaintWalkTests.cs                        # phases 1 and 2
    └── PaintContentTests.cs                     # phase 4
docs/technical-design.md                         # phase 4: sections 4, 5, 7, 8, and 14
docs/features/imgui-renderer/human-testing.md    # phase 4: the renderer-interface section
CHANGELOG.md                                     # phase 4: one Added line
```

### The Interface (public API)

`IUiRenderer` copies section 8 of the technical design with no change of a signature:

```csharp
public interface IUiRenderer
{
    void Begin(in float2 displaySize, float scale);
    void PushClip(in float4 rect);
    void PopClip();
    void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor);
    void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius);
    void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8);
    float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth);
    void End();
}
```

The doc comments hold the contract that imgui-draw-list implements:

| Member | Contract |
|--------|----------|
| `Begin` | Starts a frame. `displaySize` is `UiContext.ViewportSize`. A `NaN` component means no limit. `scale` is the display scale (1 in this epic, A3). One `Begin` comes before each other call of a frame. |
| `PushClip` | `rect` is x, y, w, h in viewport pixels (Q1). The caller intersects it with the current clip already (Q4). The width or the height can be 0. The clips form a stack. |
| `PopClip` | Removes the last clip. Each `PushClip` gets one `PopClip` before `End`. |
| `Rect` | `rect` is the border box: x, y, w, h in viewport pixels. The renderer fills the rect with `color` when its alpha is above 0. It draws the border inside the rect when `borderWidth` is above 0 and the alpha of `borderColor` is above 0. `radius` applies to the fill and the border. |
| `Image` | `rect` as for `Rect`. `uv` is u0, v0, u1, v1. `tint` multiplies the texels. A `TextureHandle` of id 0 draws nothing. |
| `Text` | `pos` is the top-left of the first line in viewport pixels. `size` is the font size in pixels. `utf8` is valid for the call only. |
| `MeasureText` | Gives the width and the height of the text in pixels. A `wrapWidth` of 0 or less, or `NaN`, means no wrap. The call draws nothing. A `FontHandle` of id 0 is the default font of the renderer. |
| `End` | Ends the frame. |

Value types (all in `namespace BrutalUi.Core;`):

| Type | Shape |
|------|-------|
| `Rgba8` | `[StructLayout(LayoutKind.Sequential)] public readonly struct Rgba8 : IEquatable<Rgba8>`. Public readonly fields `R`, `G`, `B`, `A` (bytes, in this order, A5). Constructor `Rgba8(byte r, byte g, byte b, byte a = 255)`. `static Rgba8 Transparent => default`. `==`, `!=`, `Equals`, `GetHashCode`, `ToString()` as `#RRGGBBAA` (allocates, for test output only). Phase 2 adds `internal Rgba8 ScaleAlpha(float factor)`. |
| `Corners` | `public struct Corners`. Public fields `TopLeft`, `TopRight`, `BottomRight`, `BottomLeft` (floats, pixels, A6). Constructor with the four values in that order. `static Corners All(float radius)`. Mutable, as `LengthEdges`, so `element.Style.CornerRadius.TopLeft = 4f;` compiles. |
| `FontHandle` | `public readonly struct FontHandle : IEquatable<FontHandle>`. `public FontHandle(int id)`, `int Id`, equality members. Id 0 (the default) is the default font of the renderer. A font service in `BrutalUi.ImGui` gives the other ids. |
| `TextureHandle` | `public readonly struct TextureHandle : IEquatable<TextureHandle>`. `public TextureHandle(int id)`, `int Id`, `bool IsNone => Id == 0`, equality members. Id 0 (the default) is no texture. |

### The Paint Members of `ComputedStyle`

| Member | Type | Default | Rule |
|--------|------|---------|------|
| `BackgroundColor` | `Rgba8` field | transparent | The fill of the border box. |
| `BorderColor` | `Rgba8` field | transparent | The color of the border. The walk uses the left edge of `BorderWidth` as the width (A4). |
| `CornerRadius` | `Corners` field | all 0 | The radius of each corner of the fill and the border. |
| `Opacity` | `float` property | 1 | 0 to 1. The walk clamps a value below 0 to 0 and above 1 to 1. `NaN` counts as 1. 0 hides the element and its subtree (Q2). |

`Overflow` exists already. The walk clips for `Overflow.Hidden` and `Overflow.Scroll` (Decision 6). No new overflow field.

`Opacity` is a property and not a field (Decision 3). A float field gives 0 for `default(ComputedStyle)`, and Q2 hides an element with opacity 0. Thus a new element would draw nothing. The property stores a private `float _opacity` and a private `bool _hasOpacity`. The getter is `readonly get => _hasOpacity ? _opacity : 1f`. The setter writes both. The value comes back exactly, and `default(ComputedStyle).Opacity` is 1. The property has no nested-write problem, because a `float` has no fields.

### The Draw Walk

`UiContext.Draw(IUiRenderer renderer)`:
1. Throw `ArgumentNullException` for a null renderer. Throw `InvalidOperationException` when a draw of this context runs already (a private `_drawing` flag, reset in a `finally`).
2. Call `renderer.Begin(ViewportSize, 1f)` (A3).
3. Call `PaintWalk.Walk(roots, renderer, ref _paintStack)`. The walk handles the roots in the order of add.
4. Call `renderer.End()`.

`Draw` reads the tree only. It sets and clears no dirty bit (A2).

`PaintWalk` (internal static class) and `PaintFrame` (internal struct) live in `Rendering/PaintWalk.cs`. A frame holds:

| Field | Meaning |
|-------|---------|
| `Element Element` | The element of the frame. |
| `float2 Origin` | The viewport position of the border box of the element. |
| `float Opacity` | The product of the opacities from the root to the element. |
| `float4 ClipBounds` | The clip for the children: min x, min y, max x, max y (not x, y, w, h). A root starts with negative infinity and positive infinity, which means no clip. |
| `bool PushedClip` | True when the walk pushed a clip for the element. |
| `int NextChild` | The index of the next child to visit. |

`UiContext` holds `private PaintFrame[] _paintStack = new PaintFrame[PaintWalk.InitialStackSize];` with `InitialStackSize = 32`. A full stack grows with `Array.Resize` to two times its length. The stack never shrinks, so the walk allocates only during warm-up (A8).

**Enter** (one element, with the values of the parent frame, or the root values `(0, 0)`, `1f`, and no clip):
1. When `style.Display` is `Display.None`, return. The element and its subtree draw nothing (Q2). This applies to a root too (Decision 7).
2. Compute `opacity = parentOpacity * Clamp01(style.Opacity)`. When `opacity <= 0`, return (Q2).
3. Read `float4 r = element.Layout.Rect`. Compute `origin = parentOrigin + (r.X, r.Y)` and `borderBox = (origin.X, origin.Y, r.Z, r.W)` (Q1).
4. Compute `fill = style.BackgroundColor.ScaleAlpha(opacity)` and `stroke = style.BorderColor.ScaleAlpha(opacity)` (Q3). Compute `bw = LayoutMath.LeadingBorder(in style, FlexDirection.Row)`, the resolved left border (pixels only, A4). The border is visible when `bw > 0` and `stroke.A > 0`.
5. When `fill.A > 0` or the border is visible, call `renderer.Rect(borderBox, fill, style.CornerRadius, visible ? bw : 0f, visible ? stroke : Rgba8.Transparent)`.
6. When `style.Overflow` is not `Overflow.Visible`, compute the padding box from the border box and the four resolved borders (A7): left and right from `LayoutMath.LeadingBorder`/`TrailingBorder` on `FlexDirection.Row`, top and bottom on `FlexDirection.Column`. Intersect its bounds with the parent clip bounds (Q4). Clamp the max to the min when the result is empty. Call `renderer.PushClip` with x, y, w, h of the result. Set `PushedClip`.
7. (Phase 4) Call `element.PaintContent(renderer, borderBox, opacity)`.
8. Push a frame with `NextChild = 0`. Grow the stack first when it is full.

**Loop:** while the depth is above 0, take the top frame. When `NextChild` is less than the child count (`Element.ChildStorage.AsSpan()`), read the child, add 1 to `NextChild`, then enter the child. Else call `PopClip` when `PushedClip` is set, write `default` into the slot (no stale reference), and lower the depth.

**Gotcha:** step 8 can grow the array. A `ref PaintFrame` into the old array is then stale. Copy `Origin`, `Opacity`, and `ClipBounds` into locals and add 1 to `NextChild` before the call to enter. Do not use the `ref` after the call.

`ScaleAlpha(factor)` gives `new Rgba8(R, G, B, (byte)(A * factor + 0.5f))` for a factor from 0 to 1. A factor of 1 keeps the alpha exactly.

### The Paint Hook (phase 4)

`Element` gets `internal virtual void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` with an empty body. The walk calls it after the clip push and before the children. Thus the clip of an element also clips its own content, as in CSS. `borderBox` is in viewport pixels. `opacity` is the product of step 2, so the override multiplies its own colors. label-and-image overrides it in `Label` and `Image`, which live in `BrutalUi.Core`. The member is internal, so no public API commits to it before the custom element story of a later epic (Decision 8).

### The Recording Renderer

`tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs`: an `internal sealed class RecordingRenderer : IUiRenderer`. The benchmark project links `Shared/*.cs`, so the file uses no xUnit type and no internal member of `BrutalUi.Core`.

- `internal enum DrawCallKind { Begin, PushClip, PopClip, Rect, Image, Text, End }`.
- `internal struct DrawCall` with `Kind` and the fields of each kind: `Rect` (float4), `Position` (float2), `Color`, `BorderColor`, `Tint` (Rgba8), `Radius` (Corners), `BorderWidth`, `Size`, `Scale` (float), `Texture`, `Font`, `Uv`, `TextStart`, `TextLength`.
- A `DrawCall[]` list with a start capacity of 256 and a byte buffer for text, both grown by doubling. `Begin` sets both counts to 0, so one instance records one frame and stays at its peak size.
- `ReadOnlySpan<DrawCall> Calls`, `ReadOnlySpan<byte> GetText(in DrawCall call)`, `int MeasureCount`, `int ClipDepth`.
- `PopClip` at depth 0 and `End` at a depth above 0 throw `InvalidOperationException`. A walk bug then fails the test that finds it.
- `MeasureText` adds 1 to `MeasureCount` and returns `(utf8.Length * size * 0.5f, size)`. label-and-image can change this rule for its wrap tests.
- `string Describe()` gives one line for each call, with `CultureInfo.InvariantCulture` and the format below. It allocates, so the measured body never calls it.

```
Begin 400x300 scale 1
Rect 0,0,200,100 fill #0A141EFF border 0 #00000000 radius 0,0,0,0
PushClip 2,2,196,96
Rect 12,12,50,20 fill #FF0000FF border 0 #00000000 radius 0,0,0,0
Rect 12,32,300,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0
PopClip
End
```

### Data Flow

```
UiContext.Update ──► LayoutNode.Rect (relative border boxes), dirty bits clear
UiContext.Draw(renderer)
  └─ Begin(ViewportSize, 1)
  └─ PaintWalk.Walk(roots, renderer, ref _paintStack)
       per element: hidden? ─► Rect ─► PushClip ─► PaintContent ─► children ─► PopClip
  └─ End()
renderer = RecordingRenderer (tests) | ImGuiRenderer (imgui-draw-list)
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The confirmed assumption A9 names the demo of phase 1.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The build compiles `Shared/RecordingRenderer.cs` into the benchmark project too, so a solution build is required. The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `Brutal.Numerics`, `System.Runtime.InteropServices`, `System.Globalization` as needed). The xUnit analyzers are errors. Use `TestContext.Current.TestOutputHelper?.WriteLine(...)` to print.

### Phase 1: The Walk, the Rect, and the Clip
**Goal:** The smallest path that proves the idea: the interface, the value types, a background color, and a walk that emits rects in tree order with one clip, checked by a recording renderer.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads 7 call lines: Begin, the root Rect, PushClip 2,2,196,96, two child Rects, PopClip, and End.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase fixes the public API that three features consume, and the frame-stack walk. The walk must keep the enter and exit order exact with no recursion and no stale `ref` after a growth (see the gotcha in "The Draw Walk"). Write the walk in its end-state shape (the frame fields of the table), so phases 2 and 4 add branches and not a new structure. Phase 1 sets `Opacity` to `1f` in each frame and passes `default(Corners)`, 0, and `Rgba8.Transparent` for the border arguments.

**Tasks:**

Group A (the contract, 4 files):
- [ ] 1.1 Write `src/BrutalUi.Core/Rendering/Rgba8.cs` per the value type table. The summary states the byte order and that the memory layout matches `ImU32` (`IM_COL32`) on a little-endian CPU (A5). Doc comments name each byte and the default.
- [ ] 1.2 Write `src/BrutalUi.Core/Rendering/Corners.cs` per the table (A6), in the doc style of `LengthEdges`.
- [ ] 1.3 Write `src/BrutalUi.Core/Rendering/RenderHandles.cs` with `FontHandle` and `TextureHandle` per the table. The doc comment of each states the meaning of id 0.
- [ ] 1.4 Write `src/BrutalUi.Core/Rendering/IUiRenderer.cs` with the eight members of section 8, unchanged, and the contract table of "The Interface" in the doc comments. The summary states: spans and structs only, viewport pixels, main thread, and no allocation in an implementation after warm-up.

Group B (the walk and its tests, 6 files):
- [ ] 1.5 Edit `src/BrutalUi.Core/Tree/ComputedStyle.cs`: add the `BackgroundColor` field with its doc comment. Change the last paragraph of the class remarks: this epic adds the paint fields, code sets them inline, and the style-system epic writes them from the cascade later. After a write of a paint member, call `MarkDirty(DirtyFlags.Paint)`. The walk of this release draws the whole tree each frame, so the mark has no effect yet.
- [ ] 1.6 Write `src/BrutalUi.Core/Rendering/PaintWalk.cs`: `PaintFrame`, `PaintWalk.InitialStackSize`, and `internal static void Walk(ReadOnlySpan<Element> roots, IUiRenderer renderer, ref PaintFrame[] stack)` per "The Draw Walk", steps 3, 4 (fill only), 5, 6, and 8, and the loop. Write the clip math as private static helpers (`PaddingBounds`, `Intersect`, `ToRect`). No LINQ, no lambda, no `foreach` over an interface.
- [ ] 1.7 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `_paintStack`, `_drawing`, and `public void Draw(IUiRenderer renderer)` per the four steps. The doc comment states the order (`Update` first, then `Draw`), that `Draw` reads the tree only (A2), and that a renderer or a hook must not change the tree or call `Draw` during a draw. Update the class summary to name `Draw`.
- [ ] 1.8 Write `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs` per "The Recording Renderer", with all eight members (phase 4 and label-and-image use `Text`, `Image`, and `MeasureText`).
- [ ] 1.9 Write `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`: `Unsafe.SizeOf<Rgba8>()` is 4. On a little-endian CPU, `Unsafe.As<Rgba8, uint>` of `(1, 2, 3, 4)` is `0x04030201`. `default(Rgba8)` equals `Rgba8.Transparent`. The constructor default alpha is 255. `ToString` of `(10, 20, 30, 255)` is `#0A141EFF`. `Corners.All(4)` sets four fields. The handle equality and the id 0 meaning.
- [ ] 1.10 Write `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs` with these tests:
  - `DrawsThreeElementTreeWithOneClip` (the demo, A9): viewport (400, 300). The root: column, width 200, height 100, padding 10, border width 2 on all edges, `Overflow.Hidden`, background `(10, 20, 30)`. Child A: 50 by 20, red. Child B: 300 by 20, green (it overflows the root). Run `Update`, then `Draw`. Print `Describe()` and assert it equals the 7 lines of "The Recording Renderer".
  - `AddsTheParentPositionsOnTheWayDown`: three nested levels with margins and padding. The Rect of the leaf holds the sum of the relative positions.
  - `IntersectsANestedClipWithTheParentClip`: a clipped child that overflows its clipped parent gets the intersection. A clipped element outside the parent clip gets a width or height of 0, not a negative value.
  - `PopsEachClipAfterTheSubtree`: a clipped parent with two children and a sibling after it. The `PopClip` comes after the last descendant and before the sibling Rect.
  - `SkipsTheRectOfATransparentElementAndDrawsItsChildren`.
  - `DrawsTheRootsInTheOrderOfAdd` and `DrawsOnlyBeginAndEndWithNoRoots`.
  - `ThrowsForANullRenderer` and `ThrowsForADrawDuringADraw` (a renderer that calls `Draw` from `Begin`).
- [ ] 1.11 Record the test total of the solution before the first edit in `context.md`. Run the build and the tests. Run the demo command. When `--show-live-output on` is not valid, run the test program with `--help`, use the listed form, and record it. Also try `dotnet test --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on`, and record which form prints the list. Record the printed list in `context.md`.

**Estimated Steps:** about 66 (11 tasks, about six steps for one task)

**Files (10):** Group A: `Rgba8.cs`, `Corners.cs`, `RenderHandles.cs`, `IUiRenderer.cs`. Group B: `ComputedStyle.cs`, `PaintWalk.cs`, `UiContext.cs`, `Shared/RecordingRenderer.cs`, `Rendering/RenderValueTests.cs`, `Rendering/PaintWalkTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the 7 lines, and the test asserts them.
- [ ] Each test of `RenderValueTests` and `PaintWalkTests` passes.
- [ ] `PaintWalk.cs` holds no recursive call. It has no method that calls itself.
- [ ] The benchmark project builds with the linked recording renderer and 0 warnings.

---

### Phase 2: Hidden Elements, Opacity, Borders, and Corners
**Goal:** Add the other paint members and the visibility rules: the hidden skip (Q2), the opacity product (Q3), the border rule (A4), and the corner radius. Prove that `Draw` changes no dirty bit and survives a deep tree.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsHiddenAndFadedTree' --show-live-output on` and reads a call list with no Rect for the hidden child, alpha `80` (128) in the fill of the half-opacity panel, and alpha `40` (64) in the fill of its half-opacity child.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Edit `ComputedStyle.cs`: add the `BorderColor` and `CornerRadius` fields and the `Opacity` property per "The Paint Members of `ComputedStyle`". The getter is `readonly`, because the engine and the walk read the style through `ref readonly`. The remarks of `Opacity` state why it is a property (Decision 3), the clamp, the `NaN` rule, and the hidden rule.
- [ ] 2.2 Edit `Rgba8.cs`: add `internal Rgba8 ScaleAlpha(float factor)`.
- [ ] 2.3 Edit `PaintWalk.cs`: add steps 1 and 2 of "Enter", the stroke and the border rule of step 4, the radius of step 5, and `Overflow.Scroll` in step 6. Keep the frame shape of phase 1.
- [ ] 2.4 Add to `RenderValueTests.cs`: `default(ComputedStyle).Opacity` is 1, a new `Element` has opacity 1, a set of 0.3 reads back exactly 0.3, a set of 0 reads back 0. `ScaleAlpha(1)` keeps the alpha, `ScaleAlpha(0.5f)` of 255 gives 128, `ScaleAlpha(0)` gives 0.
- [ ] 2.5 Add to `PaintWalkTests.cs` the hidden tests: `SkipsADisplayNoneSubtreeAndDrawsTheNextSibling`, `SkipsAZeroOpacitySubtree`, `SkipsAHiddenRoot`, `TreatsANegativeOpacityAsHidden`, and `TreatsANaNOpacityAsOne`. Each checks that the hidden subtree pushes no clip.
- [ ] 2.6 Add the opacity and border tests: `MultipliesOpacityDownTheSubtree` (0.5 and 0.5 give 64 for an alpha of 255, also on the border color), `DrawsABorderOnlyElement` (a transparent fill and a visible border give one Rect), `UsesTheLeftBorderWidth` (left 3, top 1 give 3), `IgnoresAPercentBorderWidth`, `SkipsAnInvisibleBorder` (width 2 with a transparent color gives no Rect when the fill is transparent, and width 0 in the Rect when the fill is visible), `PassesTheCornerRadius`, and `ClipsAScrollContainer`.
- [ ] 2.7 Add `DrawChangesNoDirtyBit` (A2: mark a child for paint and layout after `Update`, run `Draw`, and compare the `Dirty` value of each element before and after) and `DrawsAChainOf5000LevelsWithNoOverflow` (A8: a chain of 5000 elements with a background each and no `Update`, so the layout recursion does not run. Expect 5000 Rect calls). Add `KeepsTheOrderWhenTheStackGrows`: a root with a chain of 100 levels as its first child and a leaf as its second child. The leaf Rect comes after the last chain Rect, and each clip pops.
- [ ] 2.8 Add the demo test `DrawsHiddenAndFadedTree`: a root, a hidden child (`Display.None`) with its own child, a panel with opacity 0.5, and a child of the panel with opacity 0.5. Assert the golden list and print it. Run the build, the tests, and the demo. Record the list in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (5):** `ComputedStyle.cs`, `Rgba8.cs`, `PaintWalk.cs`, `Rendering/RenderValueTests.cs`, `Rendering/PaintWalkTests.cs` (and `context.md`)

**Acceptance Criteria:**
- [ ] Each rule of Q2, Q3, Q4, A2, A4, and A8 has one passing test or more.
- [ ] Each layout test of the suite still passes. The paint members change no layout result.
- [ ] `default(ComputedStyle)` gives a visible element.

---

### Phase 3: The Draw Allocation Gate
**Goal:** Gate `Draw` on the warm 1000-element tree with the recording renderer, and prove that the gate blocks.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the gate measures bytes above 0 and the cause is in `src/`.

**Tasks:**
- [ ] 3.1 Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: set paint members inline. Add no `BorderWidth` outside the footer keys, because a border changes the layout. Backgrounds on the root, the header, the body, each sidebar item, each grid slot, each stat row, each footer slot, the tooltip, the modal, the settings panel, and each settings row. `Overflow.Hidden` on the sidebar, the grid, and the modal (the layout is the same as for `Visible`). `Opacity` 0.5 on the tooltip and 0.9 on the modal. `BorderWidth = LengthEdges.All(1f)` and a `BorderColor` on each footer key (a 12 by 12 leaf, so its rect does not change). `CornerRadius` 4 on each grid slot. Use named color constants. Update the remarks: the paint members and that they change no layout.
- [ ] 3.2 Edit `ThousandElementAllocationTests.cs`: the `Scene` gets `required RecordingRenderer Renderer`. Add `static void Frame(Scene)` (`Step`, then `Context.Draw(Renderer)`) and `RunFrames(Scene, int)`. Add `[Fact] DrawAllocatesNothingAfterWarmUp`: build the warm scene, run one `Draw`, run `RunFrames(scene, KindCount * 2)` two times, then measure 100 frames and `Assert.Equal(0L, after - before)`. `UpdateAllocatesNothingAfterWarmUp` does not change. Update the class summary and remarks to name the draw.
- [ ] 3.3 Extend `TheTreeHoldsTheRequiredMix`: count a visible background, a visible border, a clip, an opacity below 1, and a corner radius. Assert one or more of each. Keep each existing assert.
- [ ] 3.4 Add `[Fact] TheDrawStepEmitsEachPaintedElement`: after a frame with the settings panel hidden and after a frame with it visible, count the `Rect` calls. Compare each count with a count from `All`: the elements with a visible fill or a visible border, and with no `Display.None` on the element or an ancestor. Assert that the visible frame holds 74 more Rect calls than the hidden frame (the settings panel and its 73 rows, per task 3.1). Assert that the `PushClip` and `PopClip` counts are equal.
- [ ] 3.5 Run the build and the tests. Run the demo. Run the class 5 times and record the byte result and the run time in `context.md`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.
- [ ] 3.6 Prove that the gate blocks. Copy `ThousandElementAllocationTests.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` after the `Draw` call in `Frame`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, run the tests again, and expect 0 failed. Record both exit codes in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (3):** `Shared/ThousandElementTree.cs`, `ThousandElementAllocationTests.cs`, `context.md` (and a temporary edit of the test file, restored)

**Acceptance Criteria:**
- [ ] The 8 tests of `ThousandElementAllocationTests` pass. The draw gate reads 0 bytes in 5 runs.
- [ ] The temporary allocation gives a non-zero exit code.
- [ ] Each existing assert of `TheMeasuredBodyChangesEachPart` passes with no change of a constant.

---

### Phase 4: The Paint Hook and the Record
**Goal:** Give label-and-image its extension point, and put the walk rules into the design docs and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsContentBetweenTheBackgroundAndTheChildren' --show-live-output on` and reads, for the probe element, the Rect line, the PushClip line, a `Text` line, the Rect line of its child, and the PopClip line, in that order.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**

Group A (the hook, 3 files):
- [ ] 4.1 Edit `src/BrutalUi.Core/Tree/Element.cs`: add `internal virtual void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` with an empty body. The doc comment states when the walk calls it (after the Rect and the clip push, before the children), the units, that `opacity` is the product down the tree, and the rules: no tree change, no mark, no allocation, no `Draw` call.
- [ ] 4.2 Edit `PaintWalk.cs`: call the hook in step 7 of "Enter".
- [ ] 4.3 Write `tests/BrutalUi.Core.Tests/Rendering/PaintContentTests.cs` with a private `ContentProbe : Element` that overrides `PaintContent` (`internal override`, through `InternalsVisibleTo`) and calls `renderer.Text` with a UTF-8 literal (`"hi"u8`) and its color scaled by the opacity. Tests: `DrawsContentBetweenTheBackgroundAndTheChildren` (the demo, golden list, printed), `CallsTheHookWithTheViewportBorderBox`, `PassesTheOpacityProduct`, `CallsNoHookForAHiddenElement`, and `RecordsTheTextBytes` (`GetText` returns the bytes).

Group B (the record, 4 files):
- [ ] 4.4 Edit `docs/technical-design.md`. Section 4: the draw step names `PaintWalk` and the frame stack, with no `DrawCommand` list. Section 5: add `public void Draw(IUiRenderer renderer);` to the `UiContext` shape. Section 7: name the four paint members and the inline write of this epic. Section 8: add a subsection "The draw walk" with the rules of Q1 to Q4 and A1 to A8, the hook, `Overflow.Scroll`, the hidden root, and the value types. Section 14: the allocation gate now covers the update and the draw.
- [ ] 4.5 Edit `CHANGELOG.md`: add one line under `### Added`: the renderer interface, the value types, the paint members, `UiContext.Draw`, and the draw gate (imgui-renderer/renderer-interface). Add no benchmark number (hello-panel-sample records them).
- [ ] 4.6 Write or edit `docs/features/imgui-renderer/human-testing.md`: add a `## renderer-interface` section in the form of `docs/features/core-tree/human-testing.md`, with the 4 phase demos and the Windows run of `scripts\test-windows.ps1`.
- [ ] 4.7 Run the build, the tests, the demo, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the results and the hand-offs of Notes in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (7):** Group A: `Element.cs`, `PaintWalk.cs`, `Rendering/PaintContentTests.cs`. Group B: `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/imgui-renderer/human-testing.md`, `context.md`.

**Acceptance Criteria:**
- [ ] The probe test shows the order Rect, PushClip, Text, child Rect, PopClip.
- [ ] The draw gate of phase 3 still reads 0 bytes with the hook call in the walk.
- [ ] Section 8 of the technical design states each rule of the walk. The lint gives no hit above 2.5.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Rect form: the walk passes x,y,w,h in viewport pixels to `Rect`, `Image`, and `PushClip`.
- Q2 Hidden: `Display.None` or opacity 0 hides an element and its subtree.
- Q3 Opacity: opacity multiplies down the subtree into the alpha of every color.
- Q4 Clip: overflow hidden clips to the padding box, and the walk intersects it with the parent clip.
- A1 Paint order is tree order: the parent `Rect` first, then the children in child order. No z-index in this epic.
- A2 `Draw` reads the tree only. It changes no dirty bit, because `Update` already clears the paint bits.
- A3 `Draw(IUiRenderer)` calls `Begin(ViewportSize, 1f)`. A scale parameter waits for a later epic.
- A4 `Rect` takes one border width. The walk uses the left border width of `Style.BorderWidth` (points only). Per-side borders wait.
- A5 `Rgba8` holds four bytes R, G, B, A. Its memory layout matches `ImU32` on little endian, so ImGui casts it for free.
- A6 `Corners` holds four floats: top-left, top-right, bottom-right, bottom-left.
- A7 The walk resolves the padding box from `Style.BorderWidth` at draw time. `LayoutNode` stores no resolved border today.
- A8 The walk uses an explicit stack with a fixed start size, not recursion, so a deep tree does not overflow the stack.
- A9 Phase 1 demo: `dotnet test` runs a recording renderer test that prints the call list of a three-element tree with one clip.

### Decision 1: An internal static `PaintWalk` with an explicit frame stack
**Choice:** The chosen approach of the user. `PaintWalk.Walk` runs the enter and exit steps on a `PaintFrame[]` that `UiContext` owns and grows by doubling.
**Rationale:** The walk is testable alone, has no recursion (A8), and has a clean exit step for the clip pop. Each frame carries the opacity and the clip, so the exit restores them with no undo math. The stack lives on the context, so two contexts never share it.
**Alternatives:** (a) The `DirtyWalk` visitor: it has no exit hook, and it enters dirty subtrees only. Rejected by the user. (b) The walk inline in `UiContext`: harder to test and to read. Rejected by the user. (c) Recursion: a deep tree ends the process (section 6 of the technical design).

### Decision 2: The paint members are fields on `ComputedStyle`, the colors are `Rgba8`
**Choice:** `BackgroundColor`, `BorderColor`, and `CornerRadius` are public fields, as the layout fields.
**Rationale:** The style-system epic writes the same struct from the cascade. A field takes a nested write (`element.Style.CornerRadius.TopLeft = 4f`). `Rgba8` is the type of the interface, so the walk passes it with no conversion.
**Alternatives:** A separate paint struct on `Element`: a second `ref` slot and a second place for the cascade to write.

### Decision 3: `Opacity` is a property with a default of 1
**Choice:** A private value and a private "set" flag. The getter gives 1 when the flag is clear.
**Rationale:** Q2 hides an element with opacity 0, and a float field is 0 in `default(ComputedStyle)`. A field would hide each new element. The flag keeps an exact read-back, which a stored `1 - value` does not.
**Alternatives:** (a) A parameterless struct constructor: `default(ComputedStyle)` and the field of a new `Element` skip it. (b) A field named `Transparency`: the requirements name opacity, and the cascade and the markup use `opacity`. (c) An XOR of the bits with the bits of 1: exact and with no flag byte, but hard to read.

### Decision 4: The walk sends a clean border to the renderer
**Choice:** When the border is not visible, the walk passes width 0 and a transparent border color. It emits a Rect only for a visible fill or a visible border.
**Rationale:** The renderer then needs no visibility rule of its own, and the call list in a test shows the visible result only.
**Alternatives:** Pass the raw values and let each renderer decide: two renderers can then disagree.

### Decision 5: The walk computes the clip in min-max bounds and passes x, y, w, h
**Choice:** A frame holds `ClipBounds` as min x, min y, max x, max y. The root bounds are infinite. `PushClip` gets x, y, w, h (Q1) with a width and a height of 0 or more.
**Rationale:** An intersection in min-max form is two `Max` and two `Min` calls. Infinite root bounds remove the special case of "no parent clip" and work with a `NaN` viewport.
**Alternatives:** Start with the viewport rect: a `NaN` viewport component gives a `NaN` clip.

### Decision 6: `Overflow.Scroll` also clips
**Choice:** The walk pushes a clip for each value that is not `Overflow.Visible`.
**Rationale:** The doc comment of the `Overflow` enum already says that the renderer clips `Hidden` and `Scroll`. A scroll container that draws outside its box is a bug. The scroll offset waits for `ScrollView` in Phase 2 of the design.
**Alternatives:** Clip `Hidden` only: the enum doc and the walk then disagree.

### Decision 7: A root with `Display.None` draws nothing
**Choice:** The hidden rule of Q2 applies to a root too.
**Rationale:** Q2 states no exception. A game can then hide a root screen with no remove and no new layout.
**Alternatives:** Follow the layout, which ignores `Display` on a root: a hidden root then draws.

### Decision 8: An internal virtual paint hook on `Element`
**Choice:** `internal virtual void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)`, called after the clip push and before the children.
**Rationale:** `Label` and `Image` live in `BrutalUi.Core` and need one call per frame between their background and their children. A virtual call allocates nothing. After the clip push, overflow hidden also clips the own text of an element. Internal keeps the public API free until a later epic designs custom elements.
**Alternatives:** (a) A type switch in the walk: the walk then knows each element type. (b) A `protected` hook: public API before the custom element design. (c) A delegate slot: an allocation per element and a null check.

### Decision 9: The recording renderer lives in `Shared/` and keeps the shared rules
**Choice:** `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs` with no xUnit type and no internal member of `BrutalUi.Core`.
**Rationale:** The brief puts it there for reuse by label-and-image. The benchmark project links `Shared/*.cs`, so a later draw benchmark reuses it with no new file.
**Alternatives:** A file in `Rendering/`: the benchmark cannot link it with the existing glob.

### Decision 10: The draw gate measures full frames
**Choice:** `DrawAllocatesNothingAfterWarmUp` measures `Step` plus `Draw` for 100 frames. `UpdateAllocatesNothingAfterWarmUp` stays and measures the update alone.
**Rationale:** The update gate proves 0 bytes for `Step`, so 0 bytes for the frame proves 0 bytes for `Draw`. The rotation shows and hides the settings panel, so the draw walks two tree sizes and the recording renderer reaches its peak in the warm-up.
**Alternatives:** Measure `Draw` alone on a static tree: one tree size, and no hidden toggle in the measured region.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A `ref PaintFrame` stays in use after the stack grows, and the walk writes into the old array. | Medium | High | The gotcha in "The Draw Walk". `KeepsTheOrderWhenTheStackGrows` and the 5000-level test force a growth with a sibling after the deep child. Phase 1 is ADVANCED. |
| `Opacity` as a field gives 0 by default, and each new element hides. | High (if a field) | High | Decision 3. A test asserts `default(ComputedStyle).Opacity == 1`. |
| The paint members on the 1000-element tree change a layout value, and an existing assert fails. | Low | Medium | Only the footer keys get a border (12 by 12 leaves). `Overflow.Hidden` gives the layout of `Visible`. Phase 3 acceptance keeps each constant. On a failure, move the border to another fixed-size leaf. |
| The recording renderer grows in the measured region. | Low | High | `Begin` resets the counts. The warm-up runs each kind in both directions two times, so the peak call count comes before the measure. |
| An `internal override` from the test assembly does not compile or load. | Low | Low | `InternalsVisibleTo("BrutalUi.Core.Tests")` exists. Fallback: make the hook `protected internal virtual`, override it as `protected override`, and record the change in `context.md`. |
| `--show-live-output on` has another spelling under Microsoft.Testing.Platform. | Low | Low | Task 1.11 checks `--help`. Each demo test also asserts its golden list, so `failed: 0` proves the list. |
| The struct growth of `ComputedStyle` (about 32 bytes) raises the memory per element of the facts report. | High | Low | Expected. hello-panel-sample records the new numbers in `CHANGELOG.md`. |
| A renderer throws during a walk. The frame stack keeps stale element references until the next `Draw`. | Low | Low | `_drawing` resets in a `finally`. The next `Draw` overwrites the frames. The doc comment of `Draw` states that the renderer state is undefined after an exception. |

---

## Dependencies

### Internal Dependencies
- `core-tree/element-tree` (done): `Element`, `ElementStorage.AsSpan`, `UiContext`, `DirtyFlags`, and `InternalsVisibleTo("BrutalUi.Core.Tests")`.
- `core-tree/flexbox-layout` (done): `LayoutNode.Rect` (relative border boxes), `LayoutMath.LeadingBorder` and `TrailingBorder` (the pixel-only border rule), `Overflow`, and `Display`.
- `core-tree/allocation-guard` (done): `ThousandElementTree`, `ThousandElementAllocationTests`, and the `Shared/*.cs` link of the benchmark project.

### External Dependencies
- `Ahwoo.BRUTAL.Numerics` (`float2`, `float4`), pinned. No new package. No native dependency, so `context.md` needs no native note.

### Downstream Consumers
- imgui-draw-list: implements `IUiRenderer`, maps `Rgba8` to `ImU32`, `Corners` to the rounding flags, and the handles to its services.
- label-and-image: overrides `PaintContent`, calls `MeasureText`, and reuses `RecordingRenderer`.
- hello-panel-sample: calls `Draw` in the frame loop.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. `IUiRenderer` has the eight members of section 8 of the technical design with the same signatures. No member takes a `string` or an array.
- [ ] F2. `Rgba8` is 4 bytes in the order R, G, B, A. `Corners` holds `TopLeft`, `TopRight`, `BottomRight`, `BottomLeft`. `FontHandle` and `TextureHandle` are structs.
- [ ] F3. `ComputedStyle` has `BackgroundColor`, `BorderColor`, `CornerRadius`, and `Opacity`. `default(ComputedStyle).Opacity` is 1.
- [ ] F4. The demo test `DrawsThreeElementTreeWithOneClip` asserts and prints the 7-line call list.
- [ ] F5. `PaintWalkTests` covers the tree order, the offsets, the clip push and pop, the clip intersection, the hidden skip (`Display.None`, opacity 0, a hidden root), the opacity product, the border rule, the corner radius, the scroll clip, no dirty bit change, and a 5000-level chain.
- [ ] F6. `PaintContentTests` shows the hook between the clip push and the children, with the viewport border box and the opacity product.
- [ ] F7. `DrawAllocatesNothingAfterWarmUp` measures 0 bytes over 100 frames. A temporary allocation after `Draw` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`. The build includes the benchmark project with the linked recording renderer.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with more tests than the baseline in `context.md`.
- [ ] Q3. `PaintWalk.cs` holds no recursive call.
- [ ] Q4. No file of `tests/BrutalUi.Core.Tests/Shared/` holds `Xunit`, `Assert`, `PaintWalk`, `ChildStorage`, or `Generation`.
- [ ] Q5. Each public member of the new files has an XML doc comment (the build enforces it).
- [ ] Q6. The changed docs use ASD-STE100 prose. The lint gives no hit above 2.5.
- [ ] Q7. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows.

### Integration Criteria
- [ ] I1. `BrutalUi.Core.csproj` holds 2 `PackageReference` items, and no `.cs` file of `src/BrutalUi.Core/` has a `using` line with `ImGui`.
- [ ] I2. `Directory.Packages.props` and `scripts/` do not change.
- [ ] I3. `UpdateAllocatesNothingAfterWarmUp` and each other existing test pass with no change of a constant.
- [ ] I4. Section 8 of `docs/technical-design.md` states the walk rules, and `CHANGELOG.md` holds the Added line.

### Verification Method

Run steps 1 to 10 from `/home/aaronstatic/dev/brutal-ui` on Linux. Step 11 is MANUAL.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1, Q5).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and a total above the baseline of `context.md` (Q2, I3).
3. `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on 2>&1 | tail -n 20`. Expect the 7 lines of "The Recording Renderer" and `failed: 0` (F4).
4. `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PaintWalkTests' '*RenderValueTests' '*PaintContentTests' 2>&1 | tail -n 5`. Expect `failed: 0`. Read the test names in the three files and match them to F2, F3, F5, and F6.
5. `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests' 2>&1 | tail -n 5`. Expect a total of 8 and `failed: 0` (F7).
6. Copy `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` to the scratch folder. Add `GC.KeepAlive(new object());` after the `Draw` call in `Frame`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0 (F7). Restore the file from the copy. Run step 2 again and expect 0 failed.
7. `grep -v '^\s*///' src/BrutalUi.Core/Rendering/IUiRenderer.cs | grep -nE '\bstring\b|\[\]'`. Expect no output (F1). Read the file and compare the eight signatures with section 8.
8. `grep -nE 'Walk\(|Enter' src/BrutalUi.Core/Rendering/PaintWalk.cs`. Read each match and confirm that no method calls itself (Q3). `grep -nE 'Xunit|Assert|PaintWalk|ChildStorage|Generation' tests/BrutalUi.Core.Tests/Shared/*.cs`. Expect no output (Q4).
9. `grep -c PackageReference src/BrutalUi.Core/BrutalUi.Core.csproj; grep -rn 'using .*ImGui' src/BrutalUi.Core --include=*.cs; git diff main --stat -- Directory.Packages.props scripts/`. Expect `2`, no output, and no output (I1, I2).
10. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Read section 8 of `docs/technical-design.md` and the Added list of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).
11. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0 (Q7).

---

## Testing Strategy

### Unit Tests
- `RenderValueTests`: the layout and the byte order of `Rgba8`, `ScaleAlpha`, `Corners`, the handles, and the opacity default and read-back.
- `PaintWalkTests`: golden call lists for each rule of the walk. Each test builds a small tree, runs `Update` (except the 5000-level chain), runs `Draw` with a `RecordingRenderer`, and compares `Describe()` or single calls. Two demo tests print their list.
- `PaintContentTests`: the hook order, its arguments, and the text record.

### Allocation Test
- `ThousandElementAllocationTests.DrawAllocatesNothingAfterWarmUp` (Decision 10), with `TheDrawStepEmitsEachPaintedElement` as the proof that the measured draw does real work.

### Integration Tests
- `scripts/test-linux.sh` runs each test through `dotnet test`. Verification step 6 proves that the draw gate blocks.
- The GPU path waits for hello-panel-sample. No test of this feature needs a GPU.

### Manual Testing
- (MANUAL) `scripts\test-windows.ps1` on Windows.

---

## Related Documentation

- **Requirements:** `docs/features/imgui-renderer/renderer-interface/requirements.md`
- **Epic:** `docs/features/imgui-renderer/epic-overview.md`, `docs/features/imgui-renderer/epic-requirements.md`
- **Siblings:** `docs/features/imgui-renderer/imgui-draw-list/requirements.md`, `docs/features/imgui-renderer/label-and-image/requirements.md`, `docs/features/imgui-renderer/hello-panel-sample/requirements.md`
- **Previous plans:** `docs/features/core-tree/allocation-guard/implementation.md` (the gate pattern and the shared builder rules)
- **Design:** `docs/technical-design.md` sections 4, 5, 6, 8, and 14. `docs/mission-statement.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Tree/DirtyWalk.cs` (walk docs), `src/BrutalUi.Core/Layout/LengthEdges.cs` (a mutable edge struct), `src/BrutalUi.Core/Layout/LayoutMath.cs` (`LeadingBorder`, `TrailingBorder`), `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
- **External:** [xUnit.net v3 on Microsoft Testing Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) (`--filter-method`, `--show-live-output on|off`), [Capturing output](https://xunit.net/docs/capturing-output)

---

## Notes

### Conflicts and Gaps

1. **The opacity default.** A float field is 0 in `default(ComputedStyle)`, and Q2 hides an element with opacity 0. The plan keeps Q2 and makes `Opacity` a property with a default of 1 (Decision 3). This is the only property among the public fields of `ComputedStyle`. The class remarks say why.
2. **`Overflow.Scroll`.** Q4 names overflow hidden only. The doc comment of the `Overflow` enum says that the renderer clips `Hidden` and `Scroll`. The plan keeps Q4 and also clips `Scroll` (Decision 6).
3. **A hidden root.** The layout ignores `Display` on a root. Q2 states no exception, so the walk hides a root with `Display.None` (Decision 7). The layout still gives the root its viewport rect.
4. **The A9 command.** A9 says `dotnet test`. Microsoft.Testing.Platform shows no output of a passed test by default, so the demo uses `dotnet run` on the test project with `--filter-method` and `--show-live-output on`, the form of the earlier demos. It runs the same test host. Task 1.11 also tries the `dotnet test --project` form and records the result.
5. **The owner of the draw allocation test.** `epic-requirements.md` and `epic-overview.md` put "the draw allocation test" in hello-panel-sample. The requirements of this feature put it here. The plan follows the feature requirements (phase 3). hello-panel-sample keeps the frame loop of the sample and the changelog numbers. `/update-epic imgui-renderer` can align the epic table.
6. **"Overflow clip" as a new field.** The requirements list an overflow clip among the new paint fields. `ComputedStyle.Overflow` exists, so the plan adds no field.
7. **The `DrawCommand` list of section 4.** The diagram of section 4 shows a `DrawCommand` list. The walk calls the renderer directly, with no list. Task 4.4 changes the diagram.
8. **No clip cull and no paint cull.** The walk does not skip a subtree with an empty clip, and it draws the whole tree each frame (out of scope). A later epic can add both. Each needs no change of the interface.
9. **A clip on a leaf.** The walk pushes a clip for each clipped element, also for a leaf with no content. In ImGui, each clip change can split a draw command. A later change can skip the push for a leaf. The call list then changes, so the change needs new golden lists.

### Hand-offs

- **imgui-draw-list:** `Begin` can get a `NaN` component (a viewport with no limit). `PushClip` gets an intersected rect, so `PushClipRect(min, max, false)` or `true` both work. `Rgba8` casts to `ImU32` with `Unsafe.As`. `Corners` maps to four rounding values, and ImGui rounds all corners with one radius, so the renderer must pick a rule (for example the largest radius with the corner flags of the non-zero corners). `MeasureText` treats a wrap width of 0 or less, or `NaN`, as no wrap.
- **label-and-image:** override `PaintContent` and scale each color with the opacity argument. The hook gets the border box only, so the label resolves its own padding and border for the text position. The way a measure callback reaches `MeasureText` during `Update` is open. Options: a measurer reference on the label at creation, or a renderer slot on `UiContext` that the label reads through its root. This feature exposes the interface only. `RecordingRenderer.MeasureText` returns `(length * size * 0.5, size)` and can change for the wrap tests.
- **hello-panel-sample:** record the new memory facts line (the paint members grow `ComputedStyle`), and decide on a draw benchmark. `RecordingRenderer` is already linked into the benchmark project, so a `Draw` benchmark on `ThousandElementTree` needs one new class.

### Prose lint

The planner had no shell, so it did not run `ste-lint.py` on this file. Task 4.7 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
