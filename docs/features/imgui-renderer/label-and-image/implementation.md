# Label And Image - Implementation Plan

**Epic:** imgui-renderer (feature 3 of 4)
**Status:** Planning
**Started:** 2026-10-20
**Target Completion:** TBD
**Last Updated:** 2026-10-20 06:30

---

## Quality Bar

**Target quality:** A core library feature. `Label` and `Image` are the first element types of `BrutalUi.Core`. hello-panel-sample shows them on the screen. Each later element with text (`Button`, `TextField`, the framework layer) builds on the measure route, the wrap rule, and the content box of this feature. A wrong rule here gives wrong layouts in each later epic, and no pixel test catches it.

- **The measure contract:** Each measure callback follows `MeasureCallback.cs`. With `MeasureMode.AtMost`, it gives the result of `MeasureMode.Undefined` when that result fits the limit. It changes no tree state, marks no dirty bit, and allocates nothing. A test proves each rule.
- **The same lines in the measure and the draw (A4):** A wrapped label gives the same lines in `MeasureText` and in `Text`. A Core test proves the rule with `RecordingRenderer`. A headless ImGui test proves it with the real ImGui measure: the glyph vertices lie inside the layout rect of the label.
- **The dirty bits:** A change of the text, the font, or the size marks `Layout` and `Paint`. A change of a color marks `Paint` only. A set to the same value marks nothing.
- **Zero allocation:** `UiContext.Update` and `UiContext.Draw` allocate 0 bytes after warm-up on a tree with labels and images, also when a label gets new text from a span each frame. A planted allocation proves that the gate blocks.
- **The layout engine stays exact:** The new content insets do not change any layout result. Each existing layout fixture and `LayoutDifferentialTests` pass with no change of an expected value.
- **No new pin and no ImGui in Core:** `Directory.Packages.props` does not change. `BrutalUi.Core.csproj` keeps its 2 package references. No `.cs` file of `src/BrutalUi.Core` names ImGui outside a doc comment.

---

## Executive Summary

This feature adds the first two element types to `BrutalUi.Core`. `Label` holds owned UTF-8 text, a font handle, a size, and a color. It measures through `IUiRenderer.MeasureText` and draws through `IUiRenderer.Text`. `Image` holds a texture handle, a UV rect, a tint, and a scale mode. It measures from `TextureHandle.Size` and draws through `IUiRenderer.Image`.

`UiContext` gets a measurer slot, set at creation through a new `UiContext.CreateInfo`. An element holds no context reference, and `MeasureCallback` has no context argument. Thus `UiContext.Update` puts its measurer into a thread-static scope for the layout pass, and the static measure callback of `Label` reads it. The label wraps at the available width. It measures with no wrap first and wraps only when the text does not fit, so the `AtMost` rule of the contract holds. The draw wraps at the content width of the final layout. One snap rule goes to both widths, so the measure and the draw give the same lines.

`Label` accepts a `ReadOnlySpan<byte>`, a `string`, and an `LString` from `Brutal.Localization`. `Brutal.Localization` 2026.10.2 has no change event. Thus `UiContext.RefreshLocalizedText()` reads each `LString` again after a locale change, and it marks each label with new text. The layout pass stores the resolved padding and border of each element in `LayoutNode` (the content insets), so a label and an image draw inside their padding, also for a percent padding on a root (renderer-interface CR-10). A new allocation gate covers the label and the image. The sample does not change. hello-panel-sample owns it.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates the context with a measurer, adds labels and images, changes the text at run time, and calls `RefreshLocalizedText` after a locale change)
- [x] **The sibling feature agents** (hello-panel-sample builds a panel with a label and an image through the public API of this feature)
- [x] **The later epics** (the style-system epic writes the label color and the font from the cascade. The framework layer binds text to a label. The input epic reads the same rects)
- [ ] **The markup author** (not served. The markup comes in a later epic)

**User Subsets Served:**
- [x] **Simple** - `new UiContext(new UiContext.CreateInfo { Measurer = renderer })`, then `new Label(new Label.CreateInfo { Color = white, Text = "Hello" })` as a child of a panel. The label takes the size of its text.
- [x] **Complex** - Wrapped text in a fixed-width panel, text from a span each frame with no allocation, localized text, padding and borders, images from a texture atlas with a UV rect, and the fit and crop scale modes.

**UX Approach:**
- A `Label` in a context with no measurer throws `InvalidOperationException` from `Update`, with a message that names `UiContext.CreateInfo`. The bug shows at the first frame.
- An `Image` with the none handle or a size of 0 takes no space and draws nothing. It does not throw.
- Each setter marks the right dirty bits itself. The game calls no `MarkDirty` for a label or an image property.

---

## Goals

### Primary Goals
1. Put text and textures into the element tree as leaves that the layout pass sizes.
2. Wrap text at the available width, with the same lines in the measure and the draw (A4, A5).
3. Accept localized text and update it after a locale change (A7).
4. Keep `Update` and `Draw` free of managed allocation with labels and images.

### Success Criteria
- [ ] The phase 1 demo prints the rect `0,0,40,16` and the text line `Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`.
- [ ] A label of 13 px with `Hello, brutal-ui` in a column of 70 px gets the rect `0,0,63,26` with the real ImGui measure, and its glyph vertices lie inside that rect grown by 1 pixel.
- [ ] An image of 64 by 32 texels gets the rect `0,0,64,32`. In a 100 by 100 box with `ScaleToFit`, it draws at `0,57,100,50`.
- [ ] After `RefreshLocalizedText` with a new locale string, the label holds `Layout` and `Paint`, and the next `Update` gives the new size.
- [ ] A label with a padding of 10 px and a border of 2 px draws its text at `12,12`. A root label with a padding of 10 % in a 400 px viewport draws at `40,40` with a wrap width of `320`.
- [ ] `ElementAllocationTests` measures 0 bytes over 100 frames.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Elements/                                  # new folder, namespace BrutalUi.Core
│   ├── Label.cs                               # phase 1 (phase 2 wrap, phase 4 setters and LString, phase 5 content box)
│   ├── Image.cs                               # phase 3 (phase 5 content box)
│   └── ImageScaleMode.cs                      # phase 3
├── Tree/UiContext.cs                          # phase 1: CreateInfo, Measurer, the measure scope. Phase 4: RefreshLocalizedText
├── Tree/LayoutNode.cs                         # phase 5: ContentInsets, ContentBox
└── Layout/LayoutEngine.cs                     # phase 5: writes ContentInsets at each final layout
tests/BrutalUi.Core.Tests/
├── Shared/RecordingRenderer.cs                # phase 2: the wrap rule and LastMeasureWrapWidth
├── Elements/LabelTests.cs                     # phase 1
├── Elements/LabelWrapTests.cs                 # phase 2
├── Elements/ImageTests.cs                     # phase 3
├── Elements/LabelTextTests.cs                 # phase 4 (setters, dirty bits, LString, the locale refresh)
├── Elements/ContentBoxTests.cs                # phase 5
├── Layout/ContentInsetsTests.cs               # phase 5
├── Layout/LayoutDifferentialTests.cs          # phase 5: compares the insets too
└── ElementAllocationTests.cs                  # phase 6
tests/BrutalUi.ImGui.Tests/
├── ImGuiLabelTests.cs                         # phase 2 (phase 5 adds the percent padding case)
├── ImGuiImageElementTests.cs                  # phase 3
└── ImGuiRendererAllocationTests.cs            # phase 6: one label test
docs/technical-design.md                       # phase 6: sections 5, 6, 8, and 14
docs/features/imgui-renderer/human-testing.md  # phase 6: the label-and-image section
CHANGELOG.md                                   # phase 6: one Added line
```

`src/BrutalUi.ImGui/`, `samples/`, `Directory.Packages.props`, and each `.csproj` do not change.

### The Public API

All types live in `namespace BrutalUi.Core;`. Each public member has an XML doc comment (`GenerateDocumentationFile` is on, and a warning is an error).

```csharp
public sealed class UiContext
{
    public struct CreateInfo
    {
        public required IUiRenderer Measurer;      // the renderer that measures text for the layout pass
    }

    public UiContext();                            // unchanged: no measurer
    public UiContext(in CreateInfo info);          // ArgumentNullException for a null measurer
    public IUiRenderer? Measurer { get; }          // set at creation only (Confirmed Assumption 1)
    public void RefreshLocalizedText();            // phase 4: reads each LString of each label again
    // Roots, ViewportSize, AddRoot, RemoveRoot, Update, Draw: unchanged.
}

public sealed class Label : Element
{
    public struct CreateInfo
    {
        public required Rgba8 Color;               // no safe default: Transparent draws nothing
        public FontHandle Font;                    // default: id 0, the default font of the renderer
        public float Size;                         // default 0: the default size of the font
        public string? Text;                       // UTF-16, encoded to UTF-8 one time
        public LString? LocalizedText;             // phase 4. Text and LocalizedText together: ArgumentException
    }

    public Label(in CreateInfo info);
    public ReadOnlySpan<byte> Text { get; }        // the owned UTF-8 text. Valid until the next change.
    public LString? LocalizedText { get; }         // phase 4
    public void SetText(ReadOnlySpan<byte> utf8);  // phase 4: copies the bytes. Clears LocalizedText.
    public void SetText(string? text);             // phase 4: encodes to UTF-8. Clears LocalizedText.
    public void SetText(LString? text);            // phase 4: reads LString.Localized
    public FontHandle Font { get; set; }           // phase 4 setter: Layout | Paint
    public float Size { get; set; }                // phase 4 setter: Layout | Paint
    public Rgba8 Color { get; set; }               // phase 4 setter: Paint
}

public enum ImageScaleMode { StretchToFill, ScaleToFit, ScaleAndCrop }

public sealed class Image : Element
{
    public struct CreateInfo
    {
        public required TextureHandle Texture;     // the none handle is valid: no size, no draw
        public float4? Uv;                         // default (0, 0, 1, 1)
        public Rgba8? Tint;                        // default opaque white
        public ImageScaleMode ScaleMode;           // default StretchToFill
    }

    public Image(in CreateInfo info);
    public TextureHandle Texture { get; set; }     // a new id or size: Layout | Paint. Same id and size: no mark.
    public float4 Uv { get; set; }                 // Layout | Paint (the natural size uses the UV span)
    public Rgba8 Tint { get; set; }                // Paint
    public ImageScaleMode ScaleMode { get; set; }  // Paint
}
```

Each setter compares the old value first. A set to the same value marks nothing, as `Element.States` does. Both classes are `sealed`. `PaintContent` is `internal`, so an outside subclass cannot draw anyway. Both constructors set `Layout.Measure` to a cached static delegate, one time. Do not add children to a label or an image. The engine calls a measure callback for a leaf only (the doc comment states it).

**Gotcha (the name `Image`):** In a class that implements `IUiRenderer`, the simple name `Image` binds to the method group `Image`, not to the class. A test inside such a class writes `new BrutalUi.Core.Image(...)`. `RecordingRenderer` creates no element, so it needs no change.

**Gotcha (internal members in the ImGui tests):** `BrutalUi.Core` gives `InternalsVisibleTo` to `BrutalUi.Core.Tests` only. A test in `BrutalUi.ImGui.Tests` uses the public API. It computes a content box or a wrap width from the known style values, not from `ContentBox` or `SnapWrapWidth`.

### The Measure Route (Confirmed Assumption 1)

```
UiContext.Update(dt)
  └─ MeasureScope: previous = t_measurer; t_measurer = _measurer
     try   { LayoutEngine.Calculate(root, viewport) for each root }
     finally { t_measurer = previous }
        └─ Label.MeasureLabel(element, width, widthMode, height, heightMode)   (static)
             └─ UiContext.CurrentMeasurer ?? throw InvalidOperationException
             └─ measurer.MeasureText(font, size, text, wrap)
```

- `UiContext` holds `[ThreadStatic] private static IUiRenderer? t_measurer` and `internal static IUiRenderer? CurrentMeasurer => t_measurer`. Follow the field naming rule of `.editorconfig`. When it asks for `s_`, use `s_` and keep the attribute.
- The field is thread-static, not static. `BrutalUi.Core.Tests` runs test classes in parallel, and `LayoutEngine` already states that tests run trees on more than one thread (`s_generationCount`).
- `Update` saves and restores the old value, so a nested call keeps the outer value. The `try` and `finally` allocate nothing.
- The callback reads the scope only during `Update`. Outside `Update`, the scope is null.
- The measurer and the renderer of `Draw` must use the same font ids. The doc comment of `CreateInfo.Measurer` states it: pass the renderer that draws, or a renderer that shares its font service.

### The Label Measure Rule (phase 2, Decision 3)

```
free = MeasureText(font, size, text, 0)                 // no wrap
if widthMode == Undefined:            return free
limit = SnapWrapWidth(width)
if free.X <= limit:                   return free       // the AtMost contract, and Exactly with text that fits
return MeasureText(font, size, text, limit)             // wrap at the limit
```

- `SnapWrapWidth(float w)` (internal static): a value that is not finite, or 0 or less, gives 0 (no wrap). A value within 1/64 pixel of a whole number gives that whole number. Another value stays the same.
- The callback returns the content size. The engine adds the padding and the border, and it applies `Exactly`, the minimum, and the maximum. The callback does not clamp to the limit.
- A limit of 0 gives `MeasureText(..., 0)`, which is no wrap in the interface. The draw then gets a content width of 0, and the snap gives 0. Both sides use no wrap, so the lines stay the same.
- The label calls `MeasureText` once or twice in one callback. The layout cache stops a repeat of the same constraint. The label keeps no measure cache of its own (YAGNI, Notes gap 4).

### The Label Draw Rule

```
content = Layout.ContentBox(borderBox)                  // phase 5. Phases 1 to 4: content = borderBox
color = Color.ScaleAlpha(opacity)
if Text is empty or color.A == 0:  return
renderer.Text((content.X, content.Y), Font, Size, color, Text, SnapWrapWidth(content.Z))
```

**Why the lines stay the same (A4, Confirmed Assumption 3).** The final content width `W` of a leaf comes from the last layout of the leaf. Two cases exist.

1. `Exactly`: the engine gives `W` to the callback. The callback wraps at `SnapWrapWidth(W)`. The draw wraps at `SnapWrapWidth(W)`. The values are equal.
2. `AtMost` or `Undefined`: the callback measures at the limit `L` (or with no wrap) and gives a width `w`, the widest line rounded up. The engine sets `W = w`. The draw wraps at `w`. Each line of the measure fits in `w`. ImGui breaks a line only when the width of the line passes the wrap width, and a greedy break at `L >= w` gives the longest prefix that fits in `L`. That prefix also fits in `w`, so the greedy break at `w` takes the same prefix. Thus the lines are the same, and the values differ (see Notes, conflict 3).

The snap rule removes float noise. `(w + padding) - padding` can differ from `w` by one unit in the last place, and an exact whole-pixel line can then wrap. The draw computes the content width in the order of the engine: `rect.Z - (insets.Left + insets.Right)`.

### The Image Rules (phase 3)

- **Natural size:** `Texture.Size * (|u1 - u0|, |v1 - v0|)` in pixels, one pixel for each texel. The none handle or a size component of 0 gives `(0, 0)`. The UV span lets an atlas region take the size of the region (Decision 6).
- **Measure:** for each axis, `Undefined` gives the natural size, `AtMost` gives `min(natural, limit)`, and `Exactly` gives the limit. The `AtMost` rule keeps the contract: the natural size fits, so the result is the natural size.
- **Draw:** `content` as for the label. `tint = Tint.ScaleAlpha(opacity)`. The corner radius is square (`default(Corners)`, Notes gap 3).

| Scale mode | Draw rect | UV rect |
|------------|-----------|---------|
| `StretchToFill` (default) | the content box | `Uv` |
| `ScaleToFit` | the largest rect with the aspect of the natural size, centered in the content box | `Uv` |
| `ScaleAndCrop` | the content box | `Uv` cut around its center to the aspect of the content box |

- The aspect is `natural.X / natural.Y`. A natural size with a component of 0, or a content box with no area, falls back to `StretchToFill`. The renderer skips a rect with no area.
- A flipped UV (`u1 < u0`) keeps its direction. The crop scales the signed span.
- The rules are internal static functions (`Image.FitRect`, `Image.CropUv`), so a test checks them with no tree.

### The Content Insets (phase 5, Decision 5)

- `LayoutNode` gets `internal float4 ContentInsets`: the left, top, right, and bottom padding plus border in pixels, resolved by the engine.
- `LayoutEngine.LayoutInternal` writes it in its `if (performLayout)` block (the block that writes `ComputedRect.Z` and `ComputedRect.W`). The block runs for a computed layout and for a cache hit. The values come from `LayoutMath.LeadingPaddingAndBorder` and `LayoutMath.TrailingPaddingAndBorder` with the `ownerWidth` of the call, the width that the engine uses for the padding of the node. Task 5.1 checks this in `CalculateLayoutImpl`.
- A write at each final layout keeps the value fresh. A measurement with another owner width can run between two layouts, and a layout cache hit does not run `CalculateLayoutImpl`. Thus the cache-hit path must write the value too.
- `LayoutNode.ContentBox(in float4 borderBox)` (internal) gives `(x + left, y + top, max(w - (left + right), 0), max(h - (top + bottom), 0))`.
- The element grows by 16 bytes. The paint walk and the `PaintContent` signature do not change. The label reads its own `Layout`.

### The Locale Refresh (phase 4, Decision 4)

- `Label` keeps `_localized` (the `LString`) and `_lastLocalized` (the `string` that it encoded last).
- `UiContext.RefreshLocalizedText()` walks each root and each descendant (recursion: a rare call, and the tree stays under 200 levels per section 6). For each `Label` with an `LString`, it reads `LString.Localized`. When the reference differs from `_lastLocalized` and the content differs (`string.Equals`, ordinal), the label encodes the new text into its buffer and marks `Layout | Paint`.
- The call allocates nothing when each buffer is large enough. A text that grows a buffer allocates one time. The doc comment states it: call it after `SetLocalizedStringProvider` or `LString.Localize`, not in each frame.
- `Update` does not call it. Open scope question 2 lists the alternatives.

### The Owned Text Buffer

- `Label` holds `byte[] _utf8` (start length 32) and `int _length`. The buffer grows by doubling and never shrinks. A text change after warm-up allocates nothing.
- `SetText(ReadOnlySpan<byte>)` compares with `SequenceEqual` first. `SetText(string)` compares with the last string (ordinal), then encodes with `Encoding.UTF8.GetByteCount` and `Encoding.UTF8.GetBytes(string, Span<byte>)`.
- `LString.Localized` is shown as it is. An `LString` with replacement variables shows its template text. A game that formats the text calls `SetText` with the formatted span (Notes gap 5).

### Data Flow

```
Creation:    UiContext(new CreateInfo { Measurer = renderer }) ─► AddRoot(panel) ─► panel.Children.Add(label, image)
Frame:       label.SetText(span)  ─► MarkDirty(Layout | Paint)
             UiContext.Update(dt) ─► measure scope ─► LayoutEngine ─► Label.MeasureLabel ─► measurer.MeasureText
                                                                  ─► Image.MeasureImage ─► Texture.Size
                                                    ─► LayoutNode.ComputedRect and ContentInsets
             UiContext.Draw(renderer) ─► PaintWalk ─► Label.PaintContent ─► renderer.Text(content, wrap)
                                                  ─► Image.PaintContent ─► renderer.Image(fit rect, uv)
Locale:      LString.Localize(...) or SetLocalizedStringProvider(...) ─► context.RefreshLocalizedText()
Tests:       RecordingRenderer as the measurer and the renderer (Core). ImGuiRenderer on HeadlessImGui (ImGui).
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements leave the demo of phase 1 open, so Notes holds an open scope question (rule 12). Phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines, outside the namespace (`.editorconfig`). The xUnit analyzers are errors. Use `TestContext.Current.TestOutputHelper?.WriteLine(...)` to print. A demo uses `dotnet run --project <test project> -- --filter-method '*Name' --show-live-output on`. The `dotnet test --project` form prints no output line (renderer-interface `context.md`). The ImGui test project needs the alias `using Gui = Brutal.ImGuiApi.ImGui;` (imgui-draw-list gotcha).

### Phase 1: A One-Line Label Through the Measurer Slot
**Goal:** The smallest path that proves the idea: a context with a measurer, a label that takes the size of its text in the layout pass, and one `Text` call in the draw.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAOneLineLabel' --show-live-output on` and reads `rect 0,0,40,16`, then the three lines `Begin 400x300 scale 1`, `Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, and `End`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase fixes the measure route for each later text element: the thread-static scope, the create-info of `UiContext`, and the static callback. A wrong scope gives a race between parallel tests, or a measure with the measurer of another context.

**Tasks:**
- [ ] 1.1 Record the test total of the solution before the first edit in `context.md`. Confirm that the imgui-draw-list work is on the branch (the files `ImGuiRenderer.cs` and `tests/BrutalUi.ImGui.Tests/` exist, and `IUiRenderer.Text` takes `wrapWidth`). Read `.editorconfig` for the naming rule of a static field. Read the leaf path of `LayoutEngine.LayoutInternal` and `PaintContentTests.cs` for the test pattern of the hook.
- [ ] 1.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `CreateInfo` with `required IUiRenderer Measurer`, the constructor `UiContext(in CreateInfo info)` (a null measurer throws `ArgumentNullException`), the read-only `Measurer` property, the thread-static field, and `internal static IUiRenderer? CurrentMeasurer`. Keep the parameterless constructor. The class remarks state the measurer rule and the shared font ids.
- [ ] 1.3 Edit `Update` in `UiContext.cs`: save the scope, set it to the measurer of the context, run the layout loop in `try`, and restore the scope in `finally`. The clear pass stays after the `finally`. The doc comment states that a label measures during this call only.
- [ ] 1.4 Write `src/BrutalUi.Core/Elements/Label.cs`: `public sealed class Label : Element` with `CreateInfo` (`required Rgba8 Color`, `Font`, `Size`, `string? Text`), the constructor, the owned buffer of "The Owned Text Buffer", the `Text` getter, and the getters of `Font`, `Size`, and `Color`. The constructor sets `Layout.Measure` to a `private static readonly MeasureCallback` field. Phase 4 adds the setters.
- [ ] 1.5 Add the measure callback to `Label.cs`: read `UiContext.CurrentMeasurer`. A null measurer throws `InvalidOperationException` with the text "A Label needs a UiContext with a measurer. Create the context with UiContext.CreateInfo." Call `MeasureText(Font, Size, Text, 0)` and return the result. Phase 2 adds the wrap rule.
- [ ] 1.6 Add the `PaintContent` override to `Label.cs` per "The Label Draw Rule" with `content = borderBox` and the wrap width `content.Z` (no snap yet). The class remarks state: a leaf only, the font id rule, and that phase 5 adds the padding.
- [ ] 1.7 Write `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`:
  - `DrawsAOneLineLabel` (the demo): a `RecordingRenderer` as the measurer and the renderer, a viewport of 400 by 300, a root `Element` with `AlignItems = Align.FlexStart`, and a child label (`Hello`, size 16, color `(255, 255, 255)`). `Update`, then `Draw`. Print `rect <x,y,w,h>` with the invariant culture, then `Describe()`. Assert both.
  - `MeasuresThroughTheContextMeasurer` (the count goes up during `Update`), `ASecondUpdateOfACleanTreeMeasuresNothing`, `ThrowsWithNoMeasurer` (the parameterless context), `ThrowsForANullMeasurer`, `TheScopeIsEmptyAfterUpdate` (`CurrentMeasurer` is null after `Update` and after a throw), and `TwoContextsUseTheirOwnMeasurer` (two recording renderers, one label each, and each count goes up for its own context only).
  - `ScalesTheColorByTheOpacity` (an opacity of 0.5 gives alpha 128), `DrawsNothingForEmptyText`, and `DrawsNothingForATransparentColor`.
- [ ] 1.8 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (4):** `UiContext.cs`, `Label.cs`, `LabelTests.cs` (and `context.md`)

**Acceptance Criteria:**
- [ ] The demo prints `rect 0,0,40,16` and the three lines, and the test asserts them.
- [ ] Each existing test passes with no change. No existing test creates a context with the new constructor.
- [ ] `CurrentMeasurer` is null after each `Update`, also after an exception.

---

### Phase 2: Word Wrap at the Available Width
**Goal:** Wrap a label at the available width, keep the `AtMost` contract, and prove the same lines in the measure and the draw with the real ImGui measure (A4).
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*WrapsALabelAtItsWidth' --show-live-output on` and reads `label rect 0,0,63,26`, then one command line of the background list whose glyph vertices lie inside `0,0-64,27`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase holds the hardest part of the epic (A4). The proof of "The Label Draw Rule" rests on the break rule of ImGui. When the ImGui test finds another line count at the measured width, stop. Record the real result in `context.md`, and change the draw rule (for example a wrap width of `w + 1`) only with a test that shows the cause.

**Tasks:**

Group A (the rule, 3 files):
- [ ] 2.1 Before the edit, search the tests for callers of `RecordingRenderer.MeasureText` with a wrap width above 0. Record the result in `context.md`. Then edit `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs`. Keep the rule for no wrap: `(bytes * size * 0.5, size)`. Add the wrap rule for a finite wrap width above 0:
  - Each byte is `size * 0.5` wide. Words split at the space byte.
  - A word goes on the current line when the line width plus one space plus the word fits at or below the wrap width. Otherwise it starts a new line.
  - A word that is wider than the wrap width starts a new line. It is cut into parts of `max(1, floor(wrap / (size * 0.5)))` bytes, one part on each line. The next word follows the last part when it fits.
  - The width is the widest line, and the height is the line count times the size. Empty text gives `(0, size)`.
  - Add `LastMeasureWrapWidth`: the wrap width of the last `MeasureText` call with a wrap width above 0, or 0 before such a call.
  - The file stays free of test framework types (the benchmark links it). The remarks state: use a size of 16, so a byte is 8 pixels wide and no float noise occurs.
- [ ] 2.2 Edit `Label.cs`: add `internal static float SnapWrapWidth(float width)` and the measure rule of "The Label Measure Rule". The draw uses `SnapWrapWidth(content.Z)`. The remarks state the rule and the reason.
- [ ] 2.3 Write `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs`, with `Hello brutal world` at size 16 (144 px with no wrap):
  - The recording rule: wrap 100 gives `(96, 32)`, wrap 200 gives `(144, 16)`, wrap 0 and `NaN` give the no-wrap result, and `brutalui` at wrap 40 gives `(40, 32)`.
  - `SnapWrapWidth`: 0, -1, `NaN`, and infinity give 0. `95.999` gives 96. `70.3` gives `70.3`.
  - `WrapsAtTheAtMostLimit`: a root of width 100 with `AlignItems = Align.FlexStart` gives the rect `0,0,96,32`, `LastMeasureWrapWidth` 100, and a `Text` wrap of 96.
  - `WrapsAtTheExactWidth`: the stretched label in a root of width 100 gives the rect `0,0,100,32`. `LastMeasureWrapWidth` equals the `Text` wrap width (100).
  - `KeepsTheAtMostContract`: in a root of width 200, the rect equals the rect with a `NaN` viewport width, and `LastMeasureWrapWidth` stays 0 (no wrapped measure).
  - `DoesNotWrapWithAnUndefinedWidth` (a viewport with a `NaN` width), `MeasuresATallerLabelInANarrowerBox` (width 60 gives 3 lines: `Hello`, `brutal`, and `world`), and `ReturnsTheContentSizeOnly` (a minimum height of 50 gives a rect height of 50).

Group B (the real measure, 1 file):
- [ ] 2.4 Write `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`. The `ImGuiRenderer` of a `HeadlessImGui` harness (400 by 300) is the measurer and the renderer. Each test runs `Update` before `BeginFrame`, so the measure runs outside the frame:
  - `WrapsALabelAtItsWidth` (the demo): a root of width 70 with `AlignItems = Align.FlexStart` and a label `Hello, brutal-ui` at size 13. Print `label rect ...` and the dump. Assert the rect `0,0,63,26` and that each glyph vertex lies inside the rect grown by 1 pixel. When ImGui gives another size, record the real values in `context.md`, and change the expected values only when the vertex rule still holds.
  - `WrapsAStretchedLabelAtTheBoxWidth`: the same text stretched in a root of width 70 gives the rect `0,0,70,26`, and the glyphs lie inside it.
  - `KeepsOneLineWhenTheTextFits`: a root of width 200 gives the rect `0,0,112,13`.
  - `KeepsTheLinesForEachWidthFromFortyToTwoHundred`: for each whole width from 40 to 200 (161 widths), a label with `AlignItems = Align.FlexStart` in a root of that width has a rect height equal to the vertex height of the draw, rounded up to whole lines. Print the first width that fails, if any.
- [ ] 2.5 Run the build, the tests, and the demo. Record the printed lines in `context.md`.

**Estimated Steps:** about 36 (5 tasks, about seven steps for one task)

**Files (4):** Group A: `RecordingRenderer.cs`, `Label.cs`, `LabelWrapTests.cs`. Group B: `ImGuiLabelTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the rect and the dump. The vertex rule passes.
- [ ] `KeepsTheLinesForEachWidthFromFortyToTwoHundred` passes with no failed width.
- [ ] Each existing test of the three test projects passes with no change.

---

### Phase 3: Image
**Goal:** An image takes the size of its texture region in the layout pass and draws its texture with the UV rect, the tint, and the scale mode.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAnImageAtItsTextureSize' --show-live-output on` and reads `Image 0,0,64,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0`, then `Image 0,57,100,50 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0` for the fit image in a 100 by 100 box.
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)

**Tier note:** The image measure is a measure callback, and the caller marks each measure path as load-bearing. The rules are short. The risk is the `AtMost` contract and the fit and crop arithmetic.

**Tasks:**
- [ ] 3.1 Write `src/BrutalUi.Core/Elements/ImageScaleMode.cs`: the enum with a doc comment for each value per "The Image Rules".
- [ ] 3.2 Write `src/BrutalUi.Core/Elements/Image.cs`: `CreateInfo`, the constructor (the defaults of "The Public API"), the four properties with the dirty rules of "The Public API", the natural size, the static measure callback, and the internal static `FitRect` and `CropUv`. The setter of `Texture` compares the id and the size, because handle equality uses the id only.
- [ ] 3.3 Add the `PaintContent` override to `Image.cs` per "The Image Rules", with `content = borderBox` until phase 5.
- [ ] 3.4 Write `tests/BrutalUi.Core.Tests/Elements/ImageTests.cs`:
  - `DrawsAnImageAtItsTextureSize` (the demo): a root column with `AlignItems = Align.FlexStart` in a 400 by 300 viewport. Child 1: an image of `new TextureHandle(7, new int2(64, 32))`. Child 2: the same texture with `ScaleToFit`, width 100, and height 100. Print `Describe()` and assert the two image lines.
  - `TakesTheSizeOfTheUvRegion` (UV `0,0,0.5,0.5` gives `32x16`), `ShrinksToAnAtMostLimit` (a root of width 40 gives a width of 40), `KeepsTheAtMostContract`, `TakesNoSpaceForTheNoneHandle`, `DrawsNothingForTheNoneHandle`, and `ScalesTheTintByTheOpacity`.
  - The dirty rules: a new id with the same size marks `Paint` only. A new size marks `Layout` and `Paint`. The same handle marks nothing. `Uv` marks `Layout` and `Paint`. `Tint` and `ScaleMode` mark `Paint`.
  - The pure rules: `FitRect` for a wide and a tall image, `CropUv` for a wide and a tall box, a flipped UV, and the fall-back for a size of 0.
- [ ] 3.5 Write `tests/BrutalUi.ImGui.Tests/ImGuiImageElementTests.cs`: `DrawsTheImageElementWithItsTexture`. Register the reference 4660 at 64 by 32 with the texture service. Add an image element with the returned handle. The dump holds `tex id 4660` and the vertex bounds `0,0-64,32`.
- [ ] 3.6 Run the build, the tests, and the demo. Record the printed lines in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (4):** `ImageScaleMode.cs`, `Image.cs`, `ImageTests.cs`, `ImGuiImageElementTests.cs` (and `context.md`)

**Acceptance Criteria:**
- [ ] The demo prints the two image lines, and the test asserts them.
- [ ] Each scale mode and each dirty rule has one passing test or more.

---

### Phase 4: Text Changes and the Locale
**Goal:** The game changes the text, the font, the size, or the color of a label, or the locale of an `LString`, and the next `Update` and `Draw` show the change.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ShowsTheNewLocaleAfterARefresh' --show-live-output on` and reads `rect 0,0,40,16` and `dirty Layout, Paint`, then `rect 0,0,128,16` and `Text 0,0 font 0 size 16 #FFFFFFFF wrap 128 "Bonjour le monde"`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** The dirty bits drive each pass. A missing mark gives a stale layout, and an extra mark costs a layout each frame. The `LString` API has no change event, so the refresh rule rests on a probe.

**Tasks:**
- [ ] 4.1 Probe `Brutal.Localization` 2026.10.2 in a console project in the session scratch folder, not in the repository. Record in `context.md`: (a) the constructor forms of `LString` and whether the `Regex` argument has a default, (b) whether two reads of `Localized` give the same reference and allocate 0 bytes, (c) whether `Localize` gives a new reference, (d) whether `RemoveLocalization` gives back the `Template` reference, and (e) whether `LocalizedStrings.SetLocalizedStringProvider` calls `Localize` on each instance. When (b) fails, stop and record it (Risk 5).
- [ ] 4.2 Edit `Label.cs`: add `SetText(ReadOnlySpan<byte>)`, `SetText(string?)`, and the setters of `Font`, `Size`, and `Color` with the dirty rules of "The Public API". Each one compares first. `Size` compares with `float.Equals`, so `NaN` to `NaN` marks nothing.
- [ ] 4.3 Edit `Label.cs`: add `CreateInfo.LocalizedText`, `LocalizedText`, `SetText(LString?)`, and the internal method `RefreshLocalized()` per "The Locale Refresh". `SetText(LString)` with null clears the text. The constructor throws `ArgumentException` when `Text` and `LocalizedText` are both set.
- [ ] 4.4 Edit `UiContext.cs`: add `RefreshLocalizedText()`. It walks each root and its subtree, calls `RefreshLocalized()` on each label, and marks nothing else. The doc comment states when to call it and the allocation rule.
- [ ] 4.5 Write `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`:
  - `ShowsTheNewLocaleAfterARefresh` (the demo): a label with an `LString` of the template `Hello` (the constructor form of task 4.1) in the tree of the phase 1 demo. `Update`, then print the rect. Call `Localize("Bonjour le monde")` and `RefreshLocalizedText()`. Print `dirty <flags>` (the self bits of the label). `Update`, `Draw`, then print the rect and the text line. Use an `LString` instance, not the global `LStrings`, so parallel tests share no state.
  - The span, the string, the font, the size, and the color: a change marks the right bits. The same value marks nothing. After `Update`, the rect and the dump show the new value.
  - `SetTextFromASpanClearsTheLocalizedText`, `RefreshMarksNothingWhenTheTextIsTheSame` (the same content with a new reference), `RefreshReachesADeepLabel`, `RefreshWorksWithNoMeasurer`, `ThrowsForTwoTexts`, and `RemoveLocalizationShowsTheTemplate`.
  - `GrowsTheBufferForALongText` (a text of 100 bytes, then of 10, then of 100: the text reads back each time).
- [ ] 4.6 Run the build, the tests, and the demo. Record the printed lines in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (3):** `Label.cs`, `UiContext.cs`, `LabelTextTests.cs` (and `context.md`, and the scratch probe outside the repository)

**Acceptance Criteria:**
- [ ] The demo prints the four lines, and the test asserts them.
- [ ] Each setter has a test for a change and a test for the same value.
- [ ] No test uses the static `LStrings` class.

---

### Phase 5: Padding and Border (the Content Insets)
**Goal:** The layout pass stores the resolved padding and border of each element, and a label and an image draw inside their content box, also with a percent padding on a root (CR-10).
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsTheLabelInsideItsPadding' --show-live-output on` and reads `rect 0,0,64,40` and `Text 12,12 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, then `Text 40,40 font 0 size 16 #FFFFFFFF wrap 320 "Hello"` for the root label with a padding of 10 %.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** Group A changes the layout pass. A write in the wrong place gives a stale inset after a cache hit, and a wrong width gives other lines than the measure. The layout results must not change.

**Tasks:**

Group A (the layout pass, 4 files):
- [ ] 5.1 Read `CalculateLayoutImpl` and the measure path of a leaf in `src/BrutalUi.Core/Layout/LayoutEngine.cs`. Record in `context.md` which width the engine uses for the padding of a node (the expected answer is `ownerWidth`) and the order of the sum of the inner width. When the engine uses another width, use that width in task 5.3 and record it.
- [ ] 5.2 Edit `src/BrutalUi.Core/Tree/LayoutNode.cs`: add `internal float4 ContentInsets` and `internal readonly float4 ContentBox(in float4 borderBox)` per "The Content Insets". The doc comment states the write point and that a cache hit writes the value too.
- [ ] 5.3 Edit `LayoutEngine.cs`: in the `if (performLayout)` block of `LayoutInternal`, write `ContentInsets` from the style and the width of task 5.1. Add no other change. The write allocates nothing.
- [ ] 5.4 Write `tests/BrutalUi.Core.Tests/Layout/ContentInsetsTests.cs`: pixel padding and border on each edge, a percent padding of a child (against the inner width of the parent), a percent padding of a root (against the viewport width), a `NaN` viewport width, a change of the parent width with a clean child (the insets follow on the next `Update`), and a layout cache hit after a measurement with another owner width (the insets keep the value of the final layout).
- [ ] 5.5 Edit `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs`: compare `ContentInsets` of each element between the incremental and the fresh layout, in the same way as the rects. Run each layout fixture. No expected rect changes.

Group B (the elements, 4 files):
- [ ] 5.6 Edit `Label.cs` and `Image.cs`: `PaintContent` uses `Layout.ContentBox(borderBox)`. The label computes the wrap width as `rect.Z - (insets.Left + insets.Right)` through `ContentBox`, in the order of the engine, then applies `SnapWrapWidth`. Remove the phase 1 remark on the padding.
- [ ] 5.7 Write `tests/BrutalUi.Core.Tests/Elements/ContentBoxTests.cs`:
  - `DrawsTheLabelInsideItsPadding` (the demo): context 1 is the phase 1 tree with a label padding of 10 px and a border of 2 px. Context 2 has a root label with a padding of 10 % in a 400 by 300 viewport. Print and assert the lines of the demo.
  - `KeepsTheSameWrapWidthWithAPercentPadding`: a stretched label with a padding of 7.3 % in a root of width 333. `LastMeasureWrapWidth` equals the `Text` wrap width.
  - `DrawsTheImageInsideItsBorder` and `FitsTheImageInsideItsPadding`.
- [ ] 5.8 Add to `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`: `WrapsALabelWithAPercentPaddingAtItsWidth`. The text of phase 2 with a padding of 5 % in a root of width 150. The test computes the content box from the padding (7.5 px on each side). The glyph vertices lie inside that box grown by 1 pixel.
- [ ] 5.9 Run the build, the tests, and the demo. Run `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30` and record the new `bytes per element` value in `context.md` (the element grows by 16 bytes). Record the printed lines in `context.md`.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (8):** Group A: `LayoutNode.cs`, `LayoutEngine.cs`, `ContentInsetsTests.cs`, `LayoutDifferentialTests.cs`. Group B: `Label.cs`, `Image.cs`, `ContentBoxTests.cs`, `ImGuiLabelTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the three lines, and the test asserts them.
- [ ] Each layout fixture and `LayoutDifferentialTests` pass with no change of an expected rect.
- [ ] The phase 1 to 4 demos print the same lines as before.

---

### Phase 6: The Allocation Gate and the Record
**Goal:** Prove that `Update` and `Draw` allocate nothing with labels and images after warm-up, prove that the gate blocks, and put the rules into the design docs and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ElementAllocationTests'` and reads a total of 4 with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Tier note:** The allocation gate is load-bearing. When the gate measures bytes above 0, find the cause in `src/` before a change of the test. Group B is plain documentation work and needs no special care.

**Tasks:**

Group A (the gate, 2 files):
- [ ] 6.1 Write `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`, in the form of `ThousandElementAllocationTests`. The scene: a `RecordingRenderer` as the measurer and the renderer, a root column with 50 labels (padding 4, wrap in a width of 120) and 50 images, and one `LString` label. Run 3 warm-up frames. Then run 100 frames. In each frame, read `GC.GetAllocatedBytesForCurrentThread()` right before and right after the measured body, and add the difference.
  - `UpdateAndDrawAllocateNothingAfterWarmUp`: the body is `SetText` from one of two span buffers on 10 labels, a new tint on 10 images, `Update`, and `Draw`. Assert a sum of 0.
  - `SetTextFromAStringAllocatesNothingAfterWarmUp`: the body is `SetText` with one of two strings that the test creates before the loop, then `Update`. Assert 0.
  - `RefreshLocalizedTextAllocatesNothing`: the body is `RefreshLocalizedText()` with no locale change. Assert 0.
  - `TheMeasuredBodyDoesRealWork`: the measure count goes up in each frame of the first test, and the dump of one frame holds 51 `Text` lines and 50 `Image` lines.
- [ ] 6.2 Add `LabelUpdateAndDrawAllocateNothingAfterWarmUp` to `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`: the same body with `ImGuiRenderer` as the measurer and the renderer, `Update` outside the frame, and `Draw` between `BeginFrame` and `EndFrame`. Assert 0.
- [ ] 6.3 Prove that the gate blocks. Copy `src/BrutalUi.Core/Elements/Label.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of the measure callback. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Run the tests again and expect 0 failed. Record both exit codes in `context.md`.
- [ ] 6.4 Run the demo class 5 times and record the result in `context.md`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.

Group B (the record, 4 files):
- [ ] 6.5 Edit `docs/technical-design.md`. Section 5: the API of `Label` and `Image`, the create-info of `UiContext`, and `RefreshLocalizedText`. Section 6, "The measure callback": the measure scope of `Update`, the `ContentInsets` write point, and the 16 bytes for each element. Section 8: the measurer slot, the label measure rule, the draw rule, the snap rule, and the image scale modes. Section 14: add the element tests, the ImGui label tests, and the third allocation gate to "Test types".
- [ ] 6.6 Edit `CHANGELOG.md`: add one line under `### Added`: `Label`, `Image`, `ImageScaleMode`, the measurer slot of `UiContext`, `RefreshLocalizedText`, and the content insets of the layout pass (imgui-renderer/label-and-image).
- [ ] 6.7 Edit `docs/features/imgui-renderer/human-testing.md`: add a `## label-and-image` section in the form of the `## imgui-draw-list` section, with the 6 phase demos and the Windows run of `scripts\test-windows.ps1`.
- [ ] 6.8 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (6):** Group A: `ElementAllocationTests.cs`, `ImGuiRendererAllocationTests.cs` (and a temporary edit of `Label.cs`, restored). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/imgui-renderer/human-testing.md`, `context.md`.

**Acceptance Criteria:**
- [ ] The 4 tests of `ElementAllocationTests` and the new ImGui allocation test pass in 5 runs.
- [ ] The planted allocation gives a non-zero exit code, and the restored file is the same as the copy.
- [ ] The lint gives no hit above 2.5 for each changed doc.

---

## Key Technical Decisions

### Confirmed Assumptions

- `UiContext` gets a measurer slot, set at creation. The `Label` measure callback reads it during `Update` to call `MeasureText` (answer of 2026-10-20).
- `Image` reads its natural size from `TextureHandle.Size`, which imgui-draw-list adds (answer of 2026-10-20).
- A wrapped `Label` passes the same wrap width to `MeasureText` and to `IUiRenderer.Text` (answer of 2026-10-20).

The epic interview confirmed A4, A5, A7, and A9 (`epic-requirements.md`, `epic-overview.md`). The decisions below cite these labels.

### Decision 1: The callback reaches the slot through a thread-static scope of `Update`
**Choice:** `UiContext.Update` puts its measurer into a thread-static field for the layout loop and restores the old value in `finally`. The static label callback reads `UiContext.CurrentMeasurer`.
**Rationale:** Confirmed Assumption 1 puts the slot on `UiContext`. An element holds no context reference, and `MeasureCallback` has no context argument, so the callback cannot reach the slot directly. A scope changes no public signature and no layout fixture. Thread-static storage keeps parallel tests apart, as `LayoutEngine` already expects trees on more than one thread. The scope is internal, so a game sees only `CreateInfo.Measurer`.
**Alternatives:** (a) A measurer argument on `MeasureCallback`: a break of a public delegate and of each fixture that uses `MeasureStub` or `TextMeasure`. (b) The engine passes the measurer down its recursion to an internal second callback slot: each recursive signature of the engine changes. (c) The label stores the measurer at creation: against Confirmed Assumption 1. See open scope question 4.

### Decision 2: The slot type is `IUiRenderer`, and the parameterless constructor stays
**Choice:** `CreateInfo.Measurer` is an `IUiRenderer`. `new UiContext()` stays and gives a context with no measurer.
**Rationale:** `IUiRenderer.MeasureText` is the measure contract already, and the game passes its renderer. A narrow `ITextMeasurer` interface adds a type with no second user (YAGNI). About 120 sites in 18 test files create a context with the parameterless constructor. A context with no label needs no measurer, so the old constructor stays valid.
**Alternatives:** (a) A new interface `ITextMeasurer` that `IUiRenderer` extends: cleaner for a later measure-only service, but no user needs it now. (b) Remove the parameterless constructor: a change of each existing test with no gain.

### Decision 3: The label measures with no wrap first, and wraps only when the text does not fit
**Choice:** "The Label Measure Rule": one `MeasureText` call with no wrap, and a second call at the limit only when the text is wider than the limit. The draw wraps at the snapped content width.
**Rationale:** The rule keeps the `AtMost` contract by construction: a text that fits gives the `Undefined` result. The proof of "The Label Draw Rule" then gives the same lines in the measure and the draw (A4, Confirmed Assumption 3). The snap rule removes the float noise of the padding sum.
**Alternatives:** (a) Always wrap at the limit: one call, but a break of the contract can give a stale size from the layout cache. (b) The label stores the wrap width of its last callback for the draw: a cache hit skips the callback, so the stored value can belong to another constraint.

### Decision 4: An explicit `UiContext.RefreshLocalizedText()` for a locale change
**Choice:** The game calls `RefreshLocalizedText()` after a locale change. The call reads each `LString` again and marks each label with new text.
**Rationale:** `Brutal.Localization` 2026.10.2 has no change event (no `event` member in its XML doc). `SetLocalizedStringProvider` localizes each `LString` again in place. A locale change is rare, so one explicit full walk costs nothing in a normal frame. `Update` stays a dirty-only pass.
**Alternatives:** See open scope question 2.

### Decision 5: The layout pass stores the content insets (CR-10)
**Choice:** `LayoutNode.ContentInsets`, written at each final layout from the owner width of the engine, also on a cache hit.
**Rationale:** The label needs its content box for the text position and for the wrap width. A percent padding resolves against the inner width of the parent, and for a root against the viewport. Only the engine knows these widths. The inset that the engine writes is the inset that it used for the inner width of the measure, so the wrap widths agree. The `PaintContent` signature does not change.
**Alternatives:** See open scope question 3.

### Decision 6: The natural size of an image is the size of its UV region
**Choice:** `Texture.Size * |UV span|`.
**Rationale:** Confirmed Assumption 2 gives the texture size. A game draws icons from an atlas with a UV rect, and the icon then takes the size of its region, not of the whole atlas. The full UV rect `(0, 0, 1, 1)` gives the texture size exactly.
**Alternatives:** The full texture size for each UV rect: an atlas icon then takes the size of the atlas.

### Decision 7: Create-info structs with `required` only for a member with no safe default
**Choice:** `Label.CreateInfo.Color` and `Image.CreateInfo.Texture` are `required`. The other members have a safe default.
**Rationale:** AGENTS.md asks for `required` create-info structs. `default(Rgba8)` is transparent and draws nothing, which hides a bug. Font id 0 and a size of 0 are the defaults of the renderer. A `ReadOnlySpan<byte>` cannot be a field of a struct that is not a `ref struct`, so the create-info takes a `string`, and `SetText(span)` covers the span.
**Alternatives:** Constructors with arguments: against the project rule. All members `required`: noise for the safe defaults.

### Decision 8: Three scale modes with the names of Unity
**Choice:** `StretchToFill`, `ScaleToFit`, and `ScaleAndCrop`.
**Rationale:** The requirements name a scale mode and no values. Game developers know these three names, and they cover the cases of a game screen. No other source in the repository defines the set.
**Alternatives:** The CSS names `fill`, `contain`, and `cover`, or a `None` mode that draws at the natural size: no user needs it now.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The thread-static scope leaks a measurer, or two contexts on one thread mix their measurers. | A label measures with the wrong fonts. | `finally` restores the old value. Tests 1.7 prove the empty scope after `Update` and after a throw, and two contexts with their own counts. |
| 2 | The float noise of `(w + padding) - padding` gives a draw wrap width below `w`, and an exact whole-pixel line wraps. | One label line wraps in the draw only and overlaps the next element. | The snap rule and the sum order of the engine (task 5.6). Tests 2.4, 5.7, and 5.8 with fractional percent paddings and a sweep of widths. |
| 3 | ImGui breaks a line when the width equals the wrap width, so the draw at `w` gives one more line. | The proof of Decision 3 fails for the `AtMost` case. | The `MeasureText` width rounds up, so the real width is at or below `w`. Test 2.4 sweeps 161 widths with the real measure. The tier note of phase 2 states the stop rule. |
| 4 | A layout cache hit leaves a stale `ContentInsets` after a measurement with another owner width. | The text draws at a wrong position or wraps at another width. | The write in the `if (performLayout)` block runs on a hit too. Task 5.4 tests the case, and task 5.5 extends `LayoutDifferentialTests`. |
| 5 | `LString.Localized` allocates, or the `LString` constructor needs a `Regex`. | The refresh allocates in each call, or the demo code does not compile. | Probe 4.1 before the code. When `Localized` allocates, call the refresh only on a locale change (the doc states it) and remove `RefreshLocalizedTextAllocatesNothing` with a note in `context.md`. |
| 6 | The element grows by 16 bytes. | More memory for each element. | Task 5.9 records the new facts line. hello-panel-sample records the memory facts (renderer-interface hand-off). |
| 7 | The new wrap rule of `RecordingRenderer` changes a result that a linked project expects. | A test of the benchmark or of the ImGui project fails. | The rule for no wrap does not change. Task 2.1 searches the callers first. |
| 8 | A label in a context with no measurer throws in the middle of the layout pass. | The dirty bits stay, and each `Update` throws again. | The message names the fix. The doc comment of `Label` states the rule. The bug shows at the first frame. |

---

## Dependencies

**Internal (done):**
- core-tree: `Element`, `UiContext.Update`, the dirty flags, `LayoutNode.Measure`, and the layout cache.
- renderer-interface: `IUiRenderer`, `Rgba8.ScaleAlpha`, `FontHandle`, `TextureHandle`, `UiContext.Draw`, `PaintWalk`, and the `PaintContent` hook.
- imgui-draw-list: `TextureHandle.Size`, the `wrapWidth` argument of `IUiRenderer.Text`, `ImGuiRenderer.MeasureText` outside a frame, the texture service, and `HeadlessImGui`. This work is on the branch `feat/imgui-renderer/imgui-draw-list` and is not committed yet. Commit it before phase 1 (task 1.1 checks it).

**External (pinned, no change):**
- `Ahwoo.BRUTAL.Localization` 2026.10.2. `BrutalUi.Core.csproj` references it already. No new pin (A9).
- `Ahwoo.BRUTAL.Numerics` 2026.10.1, `Ahwoo.BRUTAL.ImGui` 2026.10.2 (the ImGui tests only), and `xunit.v3` 4.0.2.

**Downstream:**
- hello-panel-sample uses `UiContext.CreateInfo`, `Label`, and `Image`.

---

## Definition of Done

### Functional
- [ ] F1. `new UiContext(new UiContext.CreateInfo { Measurer = r })` gives `Measurer == r`. A null measurer throws `ArgumentNullException`.
- [ ] F2. A label measures through the measurer of its context during `Update` only. `UiContext.CurrentMeasurer` is null outside `Update`.
- [ ] F3. A label in a context with no measurer throws `InvalidOperationException` from `Update`.
- [ ] F4. A label wraps at an `AtMost` or `Exactly` width. A label with an undefined width does not wrap. A text that fits gives the result of no wrap.
- [ ] F5. In each Core wrap test with an `Exactly` width, `LastMeasureWrapWidth` equals the `Text` wrap width. In each ImGui label test, each glyph vertex lies inside the layout rect (or content box) grown by 1 pixel.
- [ ] F6. A change of the text, the font, or the size marks `Layout` and `Paint`. A change of the color marks `Paint`. A set to the same value marks nothing.
- [ ] F7. `RefreshLocalizedText()` updates each label with an `LString` whose localized text changed, and marks nothing else.
- [ ] F8. An image measures `Texture.Size` times the UV span, and draws with the three scale modes per "The Image Rules".
- [ ] F9. A label and an image draw inside their padding and border, also for a percent padding on a root.

### Quality
- [ ] Q1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30` prints `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` prints 0 failed. The total is the total of task 1.1 plus the new tests.
- [ ] Q3. The 4 tests of `ElementAllocationTests` and `LabelUpdateAndDrawAllocateNothingAfterWarmUp` measure 0 bytes. The planted allocation of task 6.3 gives a non-zero exit code.
- [ ] Q4. Each layout fixture and `LayoutDifferentialTests` pass with no change of an expected rect.
- [ ] Q5. Each public member of `Label`, `Image`, `ImageScaleMode`, and the new members of `UiContext` has an XML doc comment (the build enforces it).
- [ ] Q6. `ste-lint.py --fail-over 2.5` exits with 0 for each changed doc.

### Integration
- [ ] I1. `Directory.Packages.props`, `src/BrutalUi.Core/BrutalUi.Core.csproj`, `src/BrutalUi.ImGui/`, and `samples/` have no change from this feature: `git diff --quiet <imgui-draw-list commit> -- Directory.Packages.props src/BrutalUi.Core/BrutalUi.Core.csproj src/BrutalUi.ImGui samples; echo "exit=$?"` prints `exit=0`.
- [ ] I2. `grep -rn "ImGui" src/BrutalUi.Core --include=*.cs` finds doc comments only.
- [ ] I3. `scripts/test-linux.sh; echo "exit=$?"` prints the PASS line and `exit=0`.

### Verification Method
1. Run Q1 and Q2.
2. Run each phase demo command of "Implementation Phases" and compare the printed lines with the Demo line.
3. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ElementAllocationTests'` and `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-class '*ImGuiRendererAllocationTests'`. Expect `failed: 0`.
4. Run I1, I2, and I3.
5. Read `context.md` for the exit codes of task 6.3 and the facts line of task 5.9.

---

## Testing Strategy

- **Core unit tests (`BrutalUi.Core.Tests`, `RecordingRenderer`):** each measure rule, each draw rule, each dirty rule, the locale refresh, the content insets, and the pure image rules. `RecordingRenderer` is the measurer and the renderer. Its measure rule differs from ImGui, so no Core test expects an ImGui width. A size of 16 gives whole-pixel widths.
- **Headless ImGui tests (`BrutalUi.ImGui.Tests`):** the real measure and draw of a wrapped label, the sweep of widths, the percent padding case, and one image element. They read the background list through `HeadlessImGui.DescribeBackgroundList` and the vertex bounds. No test needs a GPU (A6).
- **Layout regression:** each fixture and `LayoutDifferentialTests`, with the insets in the comparison.
- **Allocation gates:** `ElementAllocationTests` (Core) and one test in `ImGuiRendererAllocationTests`. A planted allocation proves the gate.
- **Manual:** none in this feature. hello-panel-sample owns the visual check on a GPU.

---

## Related Documentation

- `docs/features/imgui-renderer/label-and-image/requirements.md`
- `docs/features/imgui-renderer/epic-overview.md` and `epic-requirements.md`
- `docs/features/imgui-renderer/renderer-interface/context.md` (hand-offs, CR-10)
- `docs/features/imgui-renderer/imgui-draw-list/context.md` (hand-offs, the `MeasureText` rules)
- `docs/features/imgui-renderer/imgui-draw-list/implementation.md` (the headless harness)
- `docs/technical-design.md` sections 5, 6, 8, and 14
- `src/BrutalUi.Core/Layout/MeasureCallback.cs` (the measure contract)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

### Open Scope Questions

**Question 1: What is the demo of phase 1? (rule 12)**
- **Recommended:** `**Demo:** the developer runs dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAOneLineLabel' --show-live-output on and reads rect 0,0,40,16, then Begin 400x300 scale 1, Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello", and End.` Reason: the smallest path that proves the measurer slot, the layout size, and the draw call, with no native library.
- Option B: the same label in the headless ImGui project, with the real measure and the background list dump. Reason: it proves the ImGui path one phase early, but it puts the ImGui harness into the first slice.
- Option C: a label and an image in one tree in phase 1. Reason: closer to the sample, but a larger first slice.

**Question 2: What starts the re-measure after a locale change?**
- **Recommended:** an explicit `UiContext.RefreshLocalizedText()` that the game calls after the change. Reason: `Brutal.Localization` has no change event, and the call costs nothing in a normal frame.
- Option B: `Update` compares the delegate of `LStrings.LocalizedStringProvider` with the last one each frame, and runs the refresh on a change. Reason: no call for the game, but it misses a private `LocalizedStrings` instance and a `Localize` call with the same provider.
- Option C: each `Update` walks each label with an `LString` and compares the `Localized` reference. Reason: automatic for each case, but a full walk in each frame.

**Question 3: How does a label find its content box (renderer-interface CR-10)?**
- **Recommended:** the layout pass stores the resolved padding and border in `LayoutNode.ContentInsets` (phase 5). Reason: exact for each padding, and the same inset as the measure, so the wrap widths agree.
- Option B: the paint walk resolves the insets top-down from the final rects and the viewport, and the hook gets the content box. Reason: no layout change, but a percent padding inside a parent with a fit-content width can differ from the engine value.
- Option C: document the limit. A label resolves a pixel padding only, and a percent padding counts as 0 for the draw. Reason: no new code, but a percent padding draws the text in the wrong place.

**Question 4: How does the label callback reach the measurer slot of `UiContext`?**
- **Recommended:** a thread-static scope that `Update` sets around the layout pass (Decision 1). Reason: no public signature changes, and parallel tests stay apart.
- Option B: a measurer argument on the public `MeasureCallback` delegate. Reason: explicit, but a break of a public type and of each layout fixture.
- Option C: the engine passes the measurer down its recursion to an internal second callback slot for the built-in elements. Reason: no static state, but each recursive signature of the engine changes.

### Conflicts

1. **Confirmed Assumption 1 and the codebase.** The item says that the label callback reads the slot during `Update`. The callback has no context argument, and an element holds no context reference (`UiContext` remarks). The plan keeps the item and adds a thread-static scope (Decision 1). The item also says "set at creation". The plan keeps the parameterless constructor, so a context with no measurer still exists (Decision 2).
2. **The requirement "re-measures when the locale changes" and `Brutal.Localization` 2026.10.2.** The package has no change event. The plan adds an explicit call (Decision 4, question 2).
3. **Confirmed Assumption 3 and the `AtMost` case.** The plan keeps the item. With an `Exactly` width, the measure and the draw get the same value. With an `AtMost` limit, the measure wraps at the limit, and the draw wraps at the measured width. The values differ, and the lines are the same (proof in "The Label Draw Rule"). A literal match in this case needs a third `MeasureText` call at the measured width, which gives the same result. The user can ask for it.
4. **The requirements name a scale mode and no values.** The plan picks three (Decision 8).

### Gaps (not in this feature)

1. No single-line mode. A label always wraps at a defined width. A later `TextWrap` style field can add it.
2. No text alignment (center, right) and no vertical alignment inside a taller box. The style-system epic can add them.
3. An image draws with square corners. The corner radius of the style applies to the border box, and the content box needs another radius.
4. The label keeps no measure cache. Thus a change of the font atlas needs no cache rule. Add a cache when a benchmark shows the cost.
5. An `LString` with replacement variables shows its localized template. The game formats the text and calls `SetText` with the span.
6. `RefreshLocalizedText` knows the `Label` type only. A later text element (for example `Button`) needs a virtual hook on `Element` or a type check.

### Hand-offs (for `context.md` at the end of phase 6)

**hello-panel-sample**
- Create the renderer first. Then create the context with `new UiContext(new UiContext.CreateInfo { Measurer = renderer })`.
- Call `Update` before `Draw`. The measure runs outside the ImGui frame too.
- A label needs `Color` (required). An image needs `Texture` (required) from `ImGuiTextureService.AddVulkan`, which carries the size.
- Record the new facts line: the element grows by 16 bytes (`ContentInsets`).

**The style-system epic**
- `Label.Color`, `Label.Font`, and `Label.Size` are properties on the element, not style fields. The cascade can write them, or a later change can move them into `ComputedStyle`.
