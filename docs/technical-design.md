# brutal-ui

## Technical Design Document

**A companion to the Mission Statement. This document guides all technical decisions for the project, from the package layout to the data formats.**

A solo developer builds this project on top of BRUTAL NuGet packages. The project optimizes for zero allocation in the frame loop first and for API clarity second. The design here is the plan at the start. Sections change as code lands.

---

## Table of Contents

1. Stack & Environment
2. Prior Art and Constraints
3. Project Structure
4. Architecture Overview
5. Element Tree
6. Layout Engine
7. Style System
8. Renderer
9. Input
10. Framework Layer
11. Markup
12. World-Space UI
13. Configuration
14. Testing Strategy
15. Development Principles
16. Phase Boundaries

---

## 1. Stack & Environment

### C#

BRUTAL is C# on .NET 10. brutal-ui is a C# class library set that targets the same runtime.

### Core Technologies

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| Language | C# 14 on .NET 10 | The BRUTAL target. `AllowUnsafeBlocks` on, nullable on. |
| Package Manager | NuGet with central package management | One `Directory.Packages.props` pins every `Ahwoo.Brutal.*` version. |
| Draw API | `Ahwoo.Brutal.ImGui` 1.92.2 binding | `ImDrawList` gives rects, text, images, polylines, clip rects, and draw callbacks. |
| Render backends | `Ahwoo.Brutal.ImGui.Backends` | Vulkan and OpenGL renderers for `ImDrawData`. |
| Fonts | `Ahwoo.Brutal.ImGui.FreeType` | Better glyph quality than the stb loader. |
| Windowing and input | `Ahwoo.Brutal.Glfw` | C# events on `GlfwWindow` for keys, mouse, gamepad, drop, content scale. |
| Textures | `Ahwoo.Brutal.Texture` | PNG, KTX, DDS loading for sprites and icons. |
| Math | `Ahwoo.Brutal.Numerics` | `float2`, `int2`, `float4`. Matches BRUTAL signatures. |
| Localization | `Ahwoo.Brutal.Localization` | Text elements accept `LString`. |
| Testing | xUnit, BenchmarkDotNet | Standard .NET tools. An allocation test guards the frame loop. |

### Why This Stack

BRUTAL ships the hard low-level parts. The single most important factor is that brutal-ui adds no native dependency and no second render path. Everything the framework draws goes through the ImGui draw list that BRUTAL already renders.

---

## 2. Prior Art and Constraints

### What We Adopt

- **mosaic-ui.** Stores with selector subscriptions, panels in slots, modes as panel sets, a window manager, a command registry, an event bus, a service registry. The runtime is 4363 lines, and the non-Unity parts copy with no change.
- **Unity UI Toolkit.** The element tree with classes and names, flexbox layout, a stylesheet with pseudo-states, a markup format. brutal-ui keeps the concepts under new names.
- **Dear ImGui 1.92.** The dynamic font system (`ImFontBaked`, `PushFont(font, size)`), `ImDrawList`, `ImDrawDataBuilder`, and the texture registration on the backends.

### What We Avoid

- **Immediate mode for the player UI.** ImGui rebuilds the UI each frame from code. Game UI needs persistent state, transitions, and data binding. A retained tree is the right model.
- **A native layout library.** Yoga through `Brutal.Interop` is possible. A C# flexbox keeps the allocation rule in our hands and avoids a native binary for each platform.
- **Full CSS.** The style system supports type, class, name, and pseudo-state selectors only. A selector holds one compound, or a child chain (`.a > .b`) or a descendant chain (`.a .b`) of depth 2. A descendant chain reads the parent and the grandparent only. No sibling, attribute, universal, or `:not` selectors in the first release.
- **ScriptableObjects.** mosaic-ui used them for definitions. brutal-ui uses plain C# records and an optional file loader.

### BRUTAL constraints

- The BRUTAL repository ships no project files. Build on the NuGet packages and pin each version.
- The `VulkanBackend` binds one render pass at construction. World-space UI needs a second backend instance or a per-call render pass.
- `ImString` lives for one frame. Retained text must copy into owned UTF-8 storage.
- The BRUTAL maintainers reject UI additions. brutal-ui stays a separate package.

---

## 3. Project Structure

```
brutal-ui/
├── Directory.Build.props
├── Directory.Packages.props
├── brutal-ui.sln
├── src/
│   ├── BrutalUi.Core/          # element tree, layout, style, input, IUiRenderer
│   ├── BrutalUi.ImGui/         # IUiRenderer on ImDrawList, fonts, textures
│   ├── BrutalUi.Glfw/          # GlfwUiInput: GLFW window events to UiContext.PushEvent
│   ├── BrutalUi.Framework/     # stores, panels, modes, windows, commands, events
│   ├── BrutalUi.Markup/        # XML loader, stylesheet parser
│   └── BrutalUi.Debugger/      # state, composition, and event inspector window
├── samples/
│   ├── HelloPanel/             # Phase 1: a static screen. Phase 2: each widget of the epic. Phase 3: one store, one mode
│   └── WorldSpace/             # panel on a quad
├── tests/
│   ├── BrutalUi.Core.Tests/
│   ├── BrutalUi.ImGui.Tests/
│   ├── BrutalUi.Glfw.Tests/
│   ├── BrutalUi.Framework.Tests/
│   ├── BrutalUi.Markup.Tests/
│   └── BrutalUi.Benchmarks/
└── docs/
    ├── feasibility.md
    ├── mission-statement.md
    ├── technical-design.md
    └── features/
```

### Directory Responsibilities

**`src/BrutalUi.Core/`** holds the element tree, the flexbox engine, the style system, input routing, and the `IUiRenderer` interface. It references `Brutal.Numerics` and `Brutal.Localization` only. No ImGui reference.

**`src/BrutalUi.ImGui/`** holds the ImGui renderer, the font service, the texture service, and the render-to-texture path. It references `Brutal.ImGui` and `Brutal.ImGui.Backends`.

**`src/BrutalUi.Glfw/`** holds `GlfwUiInput`, the GLFW input adapter. It subscribes to the events of a `GlfwWindow` and pushes them to a `UiContext` with `PushEvent`. It references `Brutal.Glfw` and `BrutalUi.Core`. The package brings the native GLFW library through `Ahwoo.BRUTAL.Glfw`. `BrutalUi.Core` stays free of GLFW and of native code.

**`src/BrutalUi.Framework/`** holds the mosaic-ui layer. It references Core only.

**`src/BrutalUi.Markup/`** holds the XML element loader and the stylesheet parser. It references Core only.

**`tests/`** holds xUnit tests for each package and a BenchmarkDotNet project with the allocation guard. `BrutalUi.ImGui.Tests` runs the ImGui renderer headless: it creates an ImGui context with no window and no GPU, and it reads the background draw list.

---

## 4. Architecture Overview

### Overview

```
 Game loop
   │
   ├─ GlfwWindow events ──► GlfwUiInput ──► UiContext.PushEvent ──► Element tree (focus, hover, capture)
   │
   ├─ UiContext.Update(deltaTime)
   │     ├─ Input pass: drain the event ring, route the events, and move the states
   │     ├─ Framework: mode diff, store notifications, bindings
   │     ├─ Style pass: cascade the sheet rules and the inline set for dirty elements
   │     ├─ Transition pass: advance the running tweens and write them over the targets
   │     └─ Layout pass: flexbox on dirty subtrees
   │
   ├─ UiContext.Draw(IUiRenderer)
   │     └─ PaintWalk (frame stack) ──► IUiRenderer ──► ImGuiRenderer ──► ImDrawList
   │
   └─ VulkanBackend / OpenGlBackend render ImDrawData
```

### Frame order

1. The game calls `GlfwBackend.NewFrame()` and `ImGui.NewFrame()` as it does today.
2. The game sets `ViewportSize` from `io.DisplaySize`. Then it calls `UiContext.Update(deltaTime)`. The framework applies pending mode changes and store notifications. The input pass runs first: it drains the event ring and routes each event. The style pass runs after it, then the transition pass, then the layout pass. The style pass and the layout pass run on dirty subtrees. The transition pass runs on the running tweens only. The caret pass runs after the layout pass, for a focused `TextField` only. The game reads `ui.RoutingGate` after `Update`.

**The style pass at the seam.** `UiContext.Update` calls the internal `UiContext.UpdateStyles` after the input pass and before the layout pass. The pass restyles each element with the `Style` bit or the `DescendantStyle` bit, and it marks `Layout` or `Paint` when the result changes. The layout pass then reads the new `ComputedStyle`. The pass has its own recursion, because it marks the children of an element during the visit, and the visitor rules of `DirtyWalk` forbid a mark of the walked kind. The pass clears the two style bits of an element after its subtree. It never clears `Layout` or `Paint`.

**The transition pass at the seam.** `UiContext.Update` calls the internal `UiContext.UpdateTransitions(deltaTime)` after the style pass and before the layout pass. The style pass writes the target of each member, and it starts, keeps, turns, or ends the tweens of the restyled elements. The transition pass then advances each running tween by `deltaTime`, also a tween that the style pass started in the same `Update`. It writes the value of each tween over the target, and it marks `Paint` only. A tween changes a color, the corner radius, or the opacity, so the layout pass reads no value of a tween. With no running tween, the pass is one count check. Section 7 holds the rules.

**The caret pass at the seam.** `UiContext.Update` runs the caret pass after the layout pass and before the clear pass. The pass acts on the focus owner only, and only when the focus owner is a `TextField`. Other elements cost one type test in each frame. The pass calls the internal `TextField.AdvanceCaret(deltaTime, measurer)`. The call advances the blink timer, maps the press and drag positions that the input pass stored, and writes the text scroll. The pass reads the measurer of the context, not the measure scope of the layout pass. It marks `Paint` for a flip of the blink, a caret move, or a new scroll. A context with no measurer advances the blink, drops the stored positions, and keeps the scroll. The clear pass of the same `Update` clears these marks, and `Draw` reads the field in each frame. Section 5 holds the rules.
3. The game calls `UiContext.Draw(IUiRenderer)`. `PaintWalk` visits the tree with an explicit frame stack and calls the renderer for each painted element. It builds no command list.
4. The game renders the ImGui frame as it does today.

### Dirty tracking

Each element carries dirty flags for style, layout, and paint. A change marks the element and its ancestors. A class, state, or name change, an attach, and a sheet change also mark the full subtree of the element with `Style` (section 7). The passes visit dirty subtrees only. A static screen costs one tree walk for the draw.

---

## 5. Element Tree

### Purpose

The retained tree that every other layer reads and writes.

### Shape

```csharp
public class Element
{
    public Element();
    public string? Name { get; set; }          // a new name (ordinal) marks the subtree Style
    public Element? Parent { get; }
    public ElementList Children { get; }       // view on a pooled array, no allocation
    public ClassList Classes { get; }          // view on an inline set of 8, then a pooled array
    public PseudoStates States { get; set; }   // Hover, Active, Focus, Disabled, Checked. A new value marks the subtree Style
    public bool IsPickable { get; set; }       // default true
    public bool IsFocusable { get; set; }      // default false: the element can take the keyboard focus
    public virtual ElementTypeId TypeId { get; }  // the type id that a type selector matches
    public ref readonly ComputedStyle Style { get; }  // resolved style, written by the style pass only
    public ref readonly InlineStyle Inline { get; }   // the write API of the game, wins over each rule
    public ref LayoutNode Layout { get; }      // the output rect, the measure callback, and the layout cache
    public DirtyFlags Dirty { get; }           // self bits and descendant bits
    public void MarkDirty(DirtyFlags kinds);
}

public readonly struct ElementList             // view on the children of one element
{
    public int Count { get; }
    public Element this[int index] { get; }
    public void Add(Element child);            // a child with a parent leaves it first
    public void Insert(int index, Element child);
    public bool Remove(Element child);
    public void RemoveAt(int index);
    public void Move(int fromIndex, int toIndex);
    public int IndexOf(Element child);
    public bool Contains(Element child);
    public ReadOnlySpan<Element>.Enumerator GetEnumerator();
}

public readonly struct ClassList               // view on the classes of one element
{
    public int Count { get; }
    public ClassId this[int index] { get; }
    public bool Add(ClassId id);               // false for a duplicate. A new class marks the subtree Style
    public bool Remove(ClassId id);            // false when absent. A removal marks the subtree Style
    public bool Contains(ClassId id);
}

public readonly struct ClassId;                // small integer id of an interned class name

public static class ClassName
{
    public static ClassId Intern(string name); // thread-safe, call it before the hot path
    public static string GetName(ClassId id);
}

public readonly struct ElementTypeId;          // small integer id of an interned type name

public static class ElementType
{
    public static ElementTypeId Intern(string name);  // thread-safe, call it before the hot path
    public static string GetName(ElementTypeId id);
}

public sealed class UiContext
{
    public struct CreateInfo
    {
        public required IUiRenderer Measurer;  // measures text for the layout pass, not null
        public Rgba8? DefaultColor;            // text color of a root, default null: opaque white
        public FontHandle DefaultFont;         // font of a root, default id 0
        public float DefaultFontSize;          // font size of a root, default 0: the default size of the font
    }

    public UiContext();                        // no measurer, the same text defaults
    public UiContext(in CreateInfo info);      // ArgumentNullException for a null measurer
    public IUiRenderer? Measurer { get; }      // set at creation only
    public ReadOnlySpan<Element> Roots { get; }
    public void AddRoot(Element root);         // marks the root and each descendant Style
    public bool RemoveRoot(Element root);
    public void AddSheet(StyleSheet sheet);    // allocates, call it in load code
    public bool RemoveSheet(StyleSheet sheet); // allocates, call it in load code
    public float2 ViewportSize { get; set; }   // available size of each root, default (0, 0)
    public void Update(float deltaTime);
    public void Draw(IUiRenderer renderer);    // reads the tree only, call it after Update
    public void RefreshLocalizedText();        // reads the LString of each label again
}

public sealed class Label : Element
{
    public struct CreateInfo                   // no color, font, or size: the label reads them from its style
    {
        public string? Text;                   // encoded to UTF-8 one time
        public LString? LocalizedText;         // Text and LocalizedText together: ArgumentException
    }

    public Label(in CreateInfo info);
    public ReadOnlySpan<byte> Text { get; }    // the owned UTF-8 text, valid until the next change
    public LString? LocalizedText { get; }
    public void SetText(ReadOnlySpan<byte> utf8);  // copies the bytes, clears LocalizedText
    public void SetText(string? text);         // encodes to UTF-8, clears LocalizedText
    public void SetText(LString? text);        // shows LString.Localized as it is
}

public enum ImageScaleMode { StretchToFill, ScaleToFit, ScaleAndCrop }

public sealed class Image : Element
{
    public struct CreateInfo
    {
        public required TextureHandle Texture; // the none handle is valid: no size, no draw
        public float4? Uv;                     // default (0, 0, 1, 1)
        public Rgba8? Tint;                    // default opaque white
        public ImageScaleMode ScaleMode;       // default StretchToFill
    }

    public Image(in CreateInfo info);
    public TextureHandle Texture { get; set; } // see the dirty rule below
    public float4 Uv { get; set; }             // a change marks Layout and Paint
    public Rgba8 Tint { get; set; }            // a change marks Paint
    public ImageScaleMode ScaleMode { get; set; }  // a change marks Paint
}
```

`DirtyFlags` has the kinds `Style`, `Layout`, and `Paint`, and one descendant bit for each kind. `ElementBuilder` adds the extension methods `Class(string)`, `Class(ClassId)`, and `Child(Element)` for a fluent build.

**The style members.** `Element.Style` is a `ref readonly` to the computed style. Only the style pass writes it, so game code cannot write a computed value by mistake. Before the first `Update`, it holds the defaults. The game writes `Element.Inline`: `panel.Inline.Width = 200;`. A set to a new value marks `Style`, and the next `Update` writes the result to `Style`. A set to the same value marks nothing. `Inline` is `public ref readonly InlineStyle`, not a property that returns the struct by value. C# accepts a property set on a variable only, and a `ref` return is a variable. A by-value return gives error CS1612 on `el.Inline.Width = 200`. The element creates the view one time in its constructor, so it costs 8 bytes and allocates nothing. A nested write such as `el.Inline.Margin.Top = 12` also gives CS1612. Write the whole value: `el.Inline.Margin = el.Inline.Margin with { Top = 12 };`. `InlineStyle` has one property for each member of `ComputedStyle` (32 members), `IsSet(StyleProperty)`, and `Clear(StyleProperty)`.

**The name, the states, and the classes.** A chain rule of a descendant can read the name, the states, and the classes of an element. Thus each of these changes marks the element and its full subtree with `Style`: a new `Name` (ordinal compare), a new `States` value, and a `ClassList.Add` or `ClassList.Remove` that changes the list. Each element of the subtree with a child also gets `DescendantStyle`, and each ancestor gets `DescendantStyle`. A set to the same value, a duplicate add, and the removal of an absent class mark nothing. The mark reads no sheet and allocates nothing. A state change on a high ancestor restyles a large subtree on the next `Update` (section 7).

**The type id.** `Element.TypeId` is virtual. `Element` returns the id of the name `Element`. `Label` and `Image` override it with their own ids. A subclass with no override has the id of its base class, and a type rule for the base class matches it. A type rule does not match a subclass with its own id. The id comes from `ElementType.Intern(name)`. The type table and the class table are separate.

**The measurer of the context.** A `Label` or a `Button` measures its text with `UiContext.Measurer` during `Update`. A `Button` with an empty text also needs the measurer, unless its width and its height are fixed. Create the context with `new UiContext(new UiContext.CreateInfo { Measurer = renderer })` for a tree with a label or a button. A context from `UiContext()` has no measurer. Its `Update` throws `InvalidOperationException` when a label or a button needs a measure. An `Image` needs no measurer. The measurer and the renderer of `Draw` must use the same font ids. Pass the renderer that draws, or a renderer that shares its font service.

**Label and image.** Both classes are leaves, and both have the `sealed` modifier. Do not add children to them. The engine calls a measure callback for an element with no children only. Each constructor sets a cached static measure callback one time. Each setter and each `SetText` overload compares the old value first. A set to the same value marks nothing, so the game calls no `MarkDirty` for these properties. The `Image.Texture` setter compares the id and the size. A new size, or a change to or from the none handle, marks `Layout` and `Paint`. A new id with the same size marks `Paint` only.

**Widgets.** `Button` and `Toggle` are the first widgets. A widget is an element that reacts to user input. Ten rules give each widget the same pattern:

1. A widget is a `public sealed class` in `src/BrutalUi.Core/Elements/`. Its type id comes from `ElementType.Intern`.
2. A nested `CreateInfo` struct holds the start values. A member is `required` only when it has no safe default.
3. One cached `static readonly` handler delegate serves each slot. The constructor writes it with `=`, so it allocates no delegate.
4. A handler casts `e.Header.CurrentTarget` with `is` and runs the check. It then calls `StopPropagation` and runs the own step.
5. The checks live in the internal `WidgetInput`: `IsClick`, `IsKeyActivation`, `IsDragStart`, `IsDragMove`, `IsArrowKey`, `IsScroll`, `IsCharInput`, `IsEditKey`, and `ViewportOrigin`. Each check returns `false` for `Disabled`. `IsScroll` also reads the event type (`Scroll`). `ViewportOrigin` subtracts the offset of each ancestor `ScrollView`, never the offset of the element itself. Each pointer check reads `UiContext.CurrentPressTarget`, the element of the left press with no chord, for the walk of the entry. `IsClick`, `IsDragStart`, and `IsDragMove` also read the event type (`PointerUp`, `PointerDown`, and `PointerMove`), so a copy of the handler in another slot does nothing. `IsCharInput` and `IsEditKey` serve a widget that takes text. They read no press target, and they need the widget as the event target. `IsCharInput` reads the event type (`Char`). `IsEditKey` reads the event type (`KeyDown`) and returns `false` for `Control`, `Alt`, or `Super`. A widget with another rule writes its own check, and that check also returns `false` for `Disabled`.
6. A pseudo-state lives in `States` only. A widget holds no second copy.
7. A change delegate is an `Action<T>?` that gets the widget as the sender. Only user input calls it, after the state change.
8. The constructor sets `IsFocusable = true`.
9. `PaintContent` draws the content. It makes no tree change, no dirty mark, and no allocation.
10. Each slice has a test class, a disabled test, and a case in an allocation gate (`WidgetAllocationTests`).

The click rule: a click is a left release that ends the press, while the widget is the press target (`UiContext.CurrentPressTarget`). `IsClick` also reads the event type: it must be `PointerUp`. `Active` stays on the pressed element through the walk of the release. A release over another element gives no click, and so does a press on another element. The chord rule (Decision 13 of slider): a click and a drag need the left press that set `Active`, with no other button down during the gesture. A chord gives no click. A release with a target from the caller gives no click, because the press target of that entry is null. `UiContext.CurrentPressTarget` holds the press target for one walk. The drain sets it from the element of the left press, and a press, a move, and a release that ends the press get it. A key, a scroll, and an entry with a caller target get null. The key rule: a `KeyDown` with the widget as the target, no repeat, and no `Control`, `Alt`, or `Super`. `Button` takes Enter and Space. `Toggle` takes Space only. A key that bubbles from a child gives no activation. The `Checked` rule: `Toggle.Checked` reads and writes `PseudoStates.Checked`. A set from code restyles the toggle and calls no delegate. The disabled rule: a widget with `Disabled` gives no click and no flip, and it does not stop the walk. The event goes on to the ancestors. The sender delegates are `Button.Clicked` and `Toggle.CheckedChanged`. Each gets the widget, so one delegate can serve many widgets.

A `Button` draws one text at the top-left corner of its content box. It follows the `SetText` rules of `Label` and uses the same internal struct `Utf8Text`. A button with an auto size takes the size of its text plus the padding and the border.

**Slider.** `Slider` is the first widget with a drag. It holds a number in a range and follows the ten widget rules.

- The value rule: a NaN gives `Minimum`. The number is clamped to the range. When `Step` is above 0, the number is rounded to the grid `Minimum + k * Step`, and a midpoint rounds away from zero. The number is then clamped again. Each path that writes the value uses this rule: the code set, the create-info, `SetRange`, the press, the drag, and the keys. A step of 0 means no grid.
- The map of the thumb center: the thumb is as wide as the smaller of the content height and the content width, and as tall as the content box. It is square when the content box is at least as wide as tall. A slider narrower than tall has no travel, so a press and a drag give `Minimum`. The center of the thumb follows the pointer. The ratio is `(x - content.X - thumb / 2) / (content.Width - thumb)`, clamped to 0 and 1. It is 0 when the travel is 0 or less. The value is `Minimum` plus the ratio times the range. The map rounds one time to `float`, so a pixel center gives the exact value.
- The press: a left press with no chord moves the value to the pointer, stops the walk, and focuses the slider. A right press, a chord press, and a press on a child give no move.
- The drag rule: a pointer move moves the value while the slider holds the left press. The checks are `WidgetInput.IsDragStart` for the press and `WidgetInput.IsDragMove` for the move. The state is the press target only. The slider holds no drag flag. Each drag move stops the walk, also when the value does not change. The position is `WidgetInput.ViewportOrigin` with `Layout.ContentBox`. This is the origin rule of the hit walk and of the paint walk. Inside a `ScrollView`, `WidgetInput.ViewportOrigin` subtracts the offset of each ancestor view, so the three rules stay the same (the Offset Rule of the Walks, below). `ScrollViewSliderTests` checks a slider in a scrolled view.
- The capture: `IDragCapture` is an internal interface with the member `CapturesMoves`. `Slider` implements it. While the slider holds the left press with no chord, a pointer move with no target walks to the slider, also when the pointer is outside its rect. The slider must be in the roots and shown: a slider with `Display.None` on it or on an ancestor, or with an opacity product of 0 or less, captures no move, and the move walks to the hit. `Hover`, `PointerHit`, and the routing gate still follow the hit. The release of the last button ends the capture. A disabled slider captures no move. This amends Decision 8 of pointer-routing for a captured move only.
- The arrow keys: a Left or Right key down moves the value by `Step`, or by one percent of the range when `Step` is 0. The slider must be the target of the key. `Control`, `Alt`, and `Super` block the key, and a repeat moves again. The slider ignores the other keys.
- The draw: the box of the slider is the track, so the background, the border, and the corner radius of the style paint it. The thumb is one rect in the content box in the `color` of the style. The rules `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled` style the thumb. A change of the value marks `Paint` only.
- The disabled rule: a disabled slider takes no press, no drag, and no key. It does not stop the walk, and it captures no move. The code set and `SetRange` still work.
- `ValueChanged`: an `Action<Slider>?` that gets the slider as the sender. A press, a drag move, and an arrow key call it one time for a change. A code set, the create-info, and `SetRange` call nothing.

**ScrollView.** `ScrollView` shows a part of its children and moves them by a scroll offset. It follows the widget rules that fit it. It takes no focus and no key, and it has no scrollbar. It has no own size, so a style rule or `Element.Inline` must give it one. A `flex-grow` alone does not limit the view. Use a size, a maximum size, or a percent size. The view shows content from the leading edge only. A `*-reverse` direction or a `justify-content` other than `flex-start` on the scroll axis puts content out of reach.

- The offset: `ScrollOffset` is a `float2` in pixels, how far the content moves left and up. A set clamps the value and calls nothing. The get, the hit walk, the draw walk, and `WidgetInput.ViewportOrigin` read `AppliedOffset()`: the stored offset clamped by the layout of the last `Update` (the clamp at use).
- The clip: the constructor sets `Inline.Overflow = Overflow.Scroll`, so the existing clip of both walks clips the children to the padding box. An inline value wins over each rule, so a rule cannot remove the clip. A write of `Inline.Overflow` from code removes the clip.
- The content size: on each axis, the far edge of the direct children (their border boxes) plus the trailing padding and the trailing border. No margin counts. An absolute child counts. A hidden child has a rect of 0. The maximum offset on each axis is the content size minus the view size, and never below 0.
- The clamp at use: a content shrink shows the end of the new content at once, and it calls `ScrollChanged` never. The stored offset stays, so a content growth brings the old offset back. A user input or a set from code starts from the shown offset and stores the clamped result. A NaN gives 0 on its axis.
- The scroll rule: a scroll on the view or on a child moves the offset by the delta times `ScrollSpeed` (the default is 40 pixels for one unit of the delta). A positive delta scrolls toward the start, as in GLFW. A delta component that is not finite counts as 0.
- The stop rule: a scroll stops the walk only when the offset changes. A view at the end of its content passes the wheel to an outer view. Each drag move stops the walk, also when the offset does not change.
- The drag: only a left press with no chord on the view itself starts a drag. The view itself is its padding, a gap between the children, or a child with `IsPickable = false`. A press on a pickable child gives no drag, so a button in the view still clicks. The press stores the pointer position. Each move adds the difference of the pointer to the offset and stops the walk. A move that is not a drag move clears the stored position. After a press that an ancestor stops, the first drag move stores the position and moves nothing. A drag move calls `ScrollChanged` only when the shown offset changes, as the wheel does.
- The capture: `ScrollView` implements `IDragCapture`. While the view holds the left press with no chord, a pointer move with no target walks to the view, also outside its rect. The release of the last button ends the capture.
- The disabled rule: a disabled view takes no scroll, no drag, and no capture, and it does not stop the walk. The walks still apply its offset, so the content does not jump. A set from code still works.
- `ScrollChanged`: an `Action<ScrollView>?` that gets the view as the sender. User input calls it one time for each change, after the change. A set from code and a clamp at use call nothing.

**TextField.** `TextField` holds one line of UTF-8 text that the user types. It follows the widget rules that fit it. It has no own size, so a style rule or `Element.Inline` must give it one of at least one line of text. It is the first widget that changes `UiContext.Update` (the caret pass, section 4).

- The buffer rule: the constructor allocates one `byte[]` of `Capacity` bytes, and the buffer never grows. `Capacity` has no default, because a silent limit is a limit that a game finds late. An input that does not fit changes nothing. The caret and the anchor sit on a code point boundary after each path. A step is one code point, not one grapheme.
- The char rule: `WidgetInput.IsCharInput` needs the field as the target of the char. The key route gives a char with no target to the focus owner. A code point that is not a Unicode scalar value, or that is a control character, is not text, and it walks on. Each other char stops the walk, also when the field drops it. The field inserts the UTF-8 form at the caret.
- The drop rule: the fit check comes before each change. A char that does not fit changes nothing, keeps the selection, and calls nothing.
- The key rule: `WidgetInput.IsEditKey` needs the field as the target of a `KeyDown`, and no `Control`, `Alt`, or `Super`. A repeat counts, and `Shift` does not block. Left, Right, Home, and End move the caret. Backspace and Delete remove the selection or one code point. Each of these six keys stops the walk, also when it changes nothing. Tab, Up, Down, Escape, the keypad Enter key, and each letter key walk on.
- The selection: the field holds an anchor and the caret. The keys with `Shift` move the caret and keep the anchor. Left or Right with a selection and no `Shift` collapses it to its start or its end. A char, Backspace, or Delete removes the selection. A focused field draws the selection behind the text in `Color` at 35 % of the alpha, and it keeps the selection after a focus loss.
- The press and drag rule: a left press with no chord puts the caret at the code point boundary nearest to the pointer (a tie goes to the lower index), and the anchor follows. A press with `Shift` keeps the anchor. A drag move moves the caret and keeps the anchor. The field implements `IDragCapture`, so a drag past the edge still selects. The press and each drag move stop the walk. The handlers have no measurer, so they store the x in text space (`_pendingX` for the caret and `_pendingAnchorX` for the anchor). The caret pass maps them in the same `Update`. A later change of the text or the caret, and each focus gain, drop the stored x.
- The Enter rule: the main Enter key with no repeat stops the walk and calls `Submitted`, also with `Shift`. The field keeps the focus. Enter changes nothing else.
- The cut rule of `SetText` and `GetText`: each `SetText` overload copies the longest prefix that fits `Capacity` and ends on a code point boundary. `GetText(Span<byte>)` copies the text and cuts at a boundary of the span length. No overload throws or allocates. A new text puts the caret and the anchor at the end, sets the scroll to 0, and restarts the blink. The same text changes nothing. A text from code keeps a control character. The `LString` overload reads the text one time and keeps no reference. The string overload compares the cut form through the private `EqualsText`, so the same long text marks nothing.
- The clip: the constructor sets `Inline.Overflow = Overflow.Hidden`. The existing clip of both walks clips the text to the padding box, so `HitWalk` and `PaintWalk` have no change. The field moves its text in `PaintContent`.
- The caret pass: for the focus owner only. The pass advances the blink, maps the stored x with the measurer of the context, and writes the text scroll. The draw reads the scroll and writes nothing. `IndexAtX` takes the `FontHandle` of the style.
- The scroll rule: the text moves left by the smallest distance that keeps the caret and its width of 1 pixel inside the content box. The scroll stays from 0 to the text width plus the caret width minus the content width. A field that is not the focus owner keeps its scroll.
- The blink: the caret shows for 0.5 second and hides for 0.5 second. An edit, a caret move, and each focus gain restart the timer. A key that changes nothing does not. A time step of 0 or less, or one that is not finite, advances nothing.
- The disabled rule: a disabled field takes no char, no key, and no press. It does not stop the walk, and it captures no move. It does not accept the focus, so the next drain drops the focus, and the field draws no caret. A set from code still works.
- `TextChanged`: an `Action<TextField>?` that gets the field as the sender. A char, a remove, and a replace call it one time for each change, after the change. A caret move, a dropped char, a `SetText`, and the start text call nothing. `Submitted`: an `Action<TextField>?` for Enter, with the field as the sender.

**The widget sheet.** `samples/HelloPanel/HelloScreen.css` styles each widget of the epic, and `samples/HelloPanel/HelloWidgets.cs` builds them. The sheet holds 31 rules. The rule matrix in prose:

- The bits: `Button` gets `:hover`, `:active`, `:focus`, and `:disabled`. `Toggle` gets `:hover`, `:active`, `:focus`, and `:checked`. `Slider` gets `:hover`, `:active`, and `:focus`. `ScrollView` gets `:hover` and `:active`, and it takes no focus. `TextField` gets `:hover` and `:focus`.
- The size rule: `Toggle`, `Slider`, `ScrollView`, and `TextField` have no own size, so each needs a size rule. A widget with no size takes no space and catches no input. `Button` takes the size of its text plus the padding and the border. A `ScrollView` needs a fixed height, because it grows to its content without one and then has nothing to scroll. A `TextField` needs a border-box height that leaves a content box of at least one line.
- The fill rule and the border rule: a `:hover` rule and an `:active` rule set the fill only (`background-color`). A `:focus` rule sets the border color only. The members are different, so the hover and the focus show at the same time. `Slider` is the exception for the fill, because its box is the track. Its hover and active rules set `color`, the color of the thumb. `ScrollView:hover` sets the border color, because a hover fill is too close to the base fill.
- The order rule: two rules with the same specificity win by their place, and the later rule wins. `Button:active` follows `Button:hover`. `Button:disabled` follows `Button:hover`, `Button:active`, and `Button:focus`, so a disabled button keeps its fill under the pointer. The game sets the `Disabled` bit, not the widget. `Toggle:checked` follows `Toggle:hover` and `Toggle:active`, so a checked toggle keeps its fill under the pointer and under a press. A checked toggle with the focus shows the checked fill and the focus border.
- The hover rule of the hit element: `Hover` is on the hit element only, not on its ancestors. A widget under the pointer takes the hover from the panel, so the panel loses `.hello-panel:hover` while the pointer is over a widget. `ScrollView:hover` matches only over the view itself: its padding, a gap, or a child with `IsPickable = false`. The labels of the sample view are not pickable, so the hover and the wheel reach the view.
- The status label rule: each status label starts with its longest text, so its buffer never grows after the build. A `u8` prefix and `int.TryFormat` write the number into one fixed 64-byte buffer, and `Label.SetText(ReadOnlySpan<byte>)` takes the result. The code uses no string, no `$"`, and no lambda. A text that does not fit throws at build time.

**The Offset Rule of the Walks.** The draw walk, the hit walk, and `WidgetInput.ViewportOrigin` use one rule. The view itself, its clip, its `PaintContent`, and its own hit test use its origin with no offset. A child adds its rect position to the origin of the view minus `AppliedOffset()`. The clip of the view does not move. Nested views add up their offsets, and their clips intersect. `ViewportOrigin` subtracts the offset of each ancestor view, and never the offset of the element itself.

**The text of a label.** `SetText(ReadOnlySpan<byte>)` compares the bytes. `SetText(string)` and `SetText(LString)` compare the string (ordinal) with the last string. After a text from a span, they compare the UTF-8 bytes with no allocation. The BRUTAL analyzer `BRUTALCORE0011` rejects `SetText("Hello")` with a literal, because the span overload exists. Write `SetText("Hello"u8)`. A bare `SetText(null)` is ambiguous. Cast it to `string?` or `LString?`. An `LString` with replacement variables shows its template text. To show a formatted text, format it and call the span overload. A `Button` has the same `SetText` rules.

**The locale refresh.** `Brutal.Localization` has no change event. Call `UiContext.RefreshLocalizedText()` after `SetLocalizedStringProvider`, `LString.Localize`, or `LString.RemoveLocalization`. Do not call it in each frame, and `Update` does not call it. The method walks each root in tree order. For each label or button with an `LString`, it reads `LString.Localized`. The same reference returns at once. A new reference with new content (ordinal) encodes the text and marks `Layout` and `Paint`. A new reference with the same content marks nothing. The method needs no measurer, and it allocates nothing when each buffer is large enough. It knows `Label` and `Button`.

Built-in element types: `Element`, `Label`, `Image`, `Button`, `Toggle`, `Slider`, `TextField`, `ScrollView`, `ListView`, `WindowChrome`.

### Events

An event is a struct that walks the tree in three phases: capture, target, and bubble. The model lives in `BrutalUi.Core/Input/`. It has no ImGui reference.

**The structs.** `UiEventHeader` is the first member of each event. It holds the type, the timestamp in seconds, the target, the phase, the current target, and the stop flag. Four public structs carry the payload: `PointerEvent`, `ScrollEvent`, `KeyEvent`, and `CharEvent`. There are seven kinds: pointer move, pointer down, pointer up, scroll, key down, key up, and char. There is no enter event and no leave event.

**The slots.** An `Element` has two slots for each kind. That makes 14 slots, for example `OnPointerDown` and `OnPointerDownCapture`. The slots live in a side object, and the first registration creates it. An element with no handler holds one null reference. A handler is a `UiEventHandler<T>(ref T e)`. Register it at build time. A delegate is a method plus a target, so it fits the rule that a handler is a static method plus a target object. A closure allocates when the code creates it, not when the walk calls it. A slot can hold a multicast delegate.

**The queue.** `UiContext` holds a fixed ring. `CreateInfo.EventQueueCapacity` sets its size, and the default is 256 entries. An entry has 64 bytes (a header of 40 bytes and a payload of 20 bytes), so the default ring uses 16 KB for each context. A full ring drops the oldest entry and counts it in `DroppedEventCount`. `PushEvent` has four public overloads, one for each struct. A push with a type that the struct cannot carry throws `ArgumentException`. The ring copies the event, so a push allocates nothing.

**The drain.** The internal `DrainEvents` pops each queued entry and walks it. `UiContext.Update` calls it first, before the style pass. The game calls no drain. The drain skips an entry with a null target after the pointer step and the keyboard step, and an entry with a target outside the roots of the context. A call of the drain from a handler throws `InvalidOperationException`.

**The pointer step of the drain.** The step runs for each entry. It changes the state before the walk, so a handler reads the new state. The release is the exception: it clears `Active` after its walk, so a `PointerUp` handler reads `Active` on the pressed element. At the start of the drain, a stored element that left the roots loses its `Hover` and `Active` bits. A pointer or scroll entry with a null target then gets the hit element as its target:

1. `HitWalk.Find` hit-tests the position of the entry (section 9, "Flow"). The context stores the position and the hit element. The property `PointerHit` returns the hit element for the routing gate.
2. `Hover` moves to the hit element only, not to its ancestors. The old owner loses the bit. Each change marks the subtree for `Style`.
3. A pointer down sets `Active` on the hit element when no element holds it. The first press target keeps `Active` until the last button goes up. A pointer up clears the bit on the stored element after its walk, also when the pointer is over another element. The `finally` block of the drain clears it too, so a handler that throws leaves no `Active` bit. The states after the drain stay the same.
4. A move and a scroll leave `Active` as it is. A move with no target walks to the element of the left press when that element implements `IDragCapture`, captures its moves, and is shown (Decision 3 of slider). `Hover`, `PointerHit`, and the routing gate still follow the hit.

A target from the caller skips the hit test and changes no state. The states follow the pointer events only. A layout change under a still pointer changes no state until the next pointer event.

**The keyboard step of the drain.** The context holds one focus owner. `UiContext.FocusedElement` returns it, and the routing gate reads it. The owner has the `Focus` bit. The step runs for each entry, after the pointer step and before the walk:

1. Stale focus. At the start of each drain, after the stale pointer rule, an owner loses the focus when it does not accept focus (`IsFocusable` is false or `Disabled` is set), when it is not in the roots, or when it is hidden (`Display.None`, or an opacity of 0 or less on it or an ancestor). The rule runs at the start of a drain only, with two additions for a key or char entry with no target. When a handler disables the owner or clears `IsFocusable`, the drain drops the owner at the next such entry of the same drain, and that entry gets no walk. A removed owner gets no walk. A style change that hides the owner takes effect after the next `Update`, because the hidden check reads the style of the last `Update`. When the owner stays, the start of the drain sets the `Focus` bit on it again. Code can clear `Focus` through `States` on the owner, but the owner keeps the keys. The bit comes back at the next drain or at a `SetFocus` call on the owner.
2. Key and char target. A `KeyDown`, `KeyUp`, or `Char` entry with no target gets the focus owner as its target. With no owner, the entry gets no walk. A target from the caller keeps its target.
3. Click focus. A `PointerDown` with no target moves the focus to the hit element, or to its nearest ancestor with `IsFocusable`. This happens after `Active` and before the walk, so a `PointerDown` handler reads `Focus`. When that element does not accept focus, or when the press hits nothing, the focus clears. `Disabled` does not pass to the children. A focusable child of a disabled element still takes the focus. A press on a disabled focusable element clears the focus and does not give it to a focusable ancestor.
4. Tab. A Tab `KeyDown` with no target walks to the old owner first. After the walk, the drain moves the focus when no handler stopped the walk. The move goes to the next element that accepts focus, in paint order (the roots in the order of add, a parent before its children). The search skips `Display.None`, an opacity product of 0 or less, and their subtrees, and it wraps at the end. Shift moves back. `Control`, `Alt`, or `Super` stops the move. A repeat moves. A key up does not move.

`SetFocus`, `ClearFocus`, `FocusNext`, and `FocusPrevious` change the owner at once, also from a handler. A caller target on a press gives no click focus, so the game calls `SetFocus`.

`SetFocus` returns `false` when the element does not accept focus, when it is not in the roots, or when it is hidden. A failed call for another element changes nothing. A failed call for the current owner clears the focus, so the result and the state agree. `SetFocus` reads the computed style of the last `Update`. A panel that was `Display.None` stays hidden for `SetFocus` until the next `Update`, also when the game shows the panel in the same frame. Call `SetFocus` after that `Update`.

The internal `FocusWalk` finds the next element with an explicit stack and no recursion.

**The walk.** The walk copies the path from the target to the top root at its start, so a tree change during the walk does not change the path. It runs in this order:

1. Capture: the capture slot of each element from the top root down to the parent of the target.
2. Target: the capture slot of the target, then the bubble slot of the target. The phase is `Target` for both.
3. Bubble: the bubble slot of each element from the parent of the target up to the top root.

A handler calls `StopPropagation()` on the event to stop the walk. The stop ends the walk at once. It also skips the later delegates of the same multicast slot, and it skips the bubble slot of the target after a stop in the capture slot of the target. The event stops for its own walk only. The next event starts with no stop.

### Design Decisions

- `ComputedStyle` and `LayoutNode` are structs stored inline in the element. The layout engine reads them through `ref`. No per-node heap object beyond the element itself.
- Each dirty kind has a self bit and a descendant bit. A mark sets the self bit and sends the descendant bit up to the root, and it stops at the first ancestor that holds the bit. The walk enters only the subtrees that have a descendant bit.
- Text lives in owned UTF-8 buffers on the element. A `Label` accepts `ReadOnlySpan<byte>`, `string`, and `LString`. The buffer starts at 32 bytes, grows by doubling, and never shrinks. A text change after warm-up allocates nothing.
- A create-info member is `required` only when it has no safe default: `Image.CreateInfo.Texture` and `UiContext.CreateInfo.Measurer`. `Label.CreateInfo` has no `required` member, because the color, the font, and the size come from the style.

---

## 6. Layout Engine

### Purpose

Flexbox layout on the element tree with the Yoga subset that games use.

### Supported properties

`display`, `position` (relative, absolute), `flex-direction`, `flex-wrap`, `justify-content`, `align-items`, `align-self`, `align-content`, `flex-grow`, `flex-shrink`, `flex-basis`, `width`, `height`, `min-*`, `max-*`, `margin`, `padding`, `border-width`, `left`, `top`, `right`, `bottom`, `overflow`. Length units: pixels, percent, auto.

### Inputs

The layout inputs are 25 public fields of `ComputedStyle`: `FlexDirection`, `Width`, `Height`, `Margin`, `Padding`, `BorderWidth`, `FlexGrow`, `FlexShrink`, `FlexBasis`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight`, `JustifyContent`, `AlignItems`, `AlignSelf`, `FlexWrap`, `AlignContent`, `Display`, `Position`, `Left`, `Top`, `Right`, `Bottom`, and `Overflow`. The fields are public so that a read of a nested value, such as `Style.Margin.Left`, compiles. Game code does not write them. It writes `Element.Inline`, and the style pass sets `Layout` when the computed value changes (section 7). The enums are `FlexDirection`, `FlexWrap`, `Justify`, `Align`, `Display`, `PositionType`, and `Overflow`.

A `Length` holds a value and a `LengthUnit`: `Undefined`, `Pixel`, `Percent`, or `Auto`. `Length.Px(10)`, `Length.Percent(50)`, and `Length.Auto` make the values. A `float` converts to pixels. A percent resolves against the size of the parent content box on the same axis. The exceptions are a margin and a padding, which resolve against the parent width on all four edges. `LengthEdges` holds a `Length` for `Left`, `Top`, `Right`, and `Bottom`. The engine has no start or end edge.

`default(ComputedStyle)` gives the Yoga defaults: a column direction, grow 0, shrink 0, auto basis, auto sizes, no minimum, no maximum, stretch alignment, flex-start content, relative position, and `Display.Flex`. `Length.Undefined` means the default of the property. `AlignItems` auto acts as stretch. `AlignContent` auto acts as flex-start. A callback on `LayoutNode.Measure` supplies the content size of a leaf (see "The measure callback").

`UiContext.ViewportSize` is the available size of each root. It starts at `(0, 0)`. A `NaN` component means no limit. A negative or infinite component throws `ArgumentOutOfRangeException`. A root with an auto size takes the viewport size. A root with a style size keeps it. A new `ViewportSize` and `AddRoot` mark nothing. The next `Update` uses the new size.

### Output

The output is `LayoutNode.Rect`, a `float4` in pixels. X and Y are the position of the border box relative to the border box of the parent. Z is the width. W is the height. The X and Y of a root hold its margin and its offsets. To get a position in the viewport, the reader adds the positions of the ancestors on the walk down. A `Display.None` element and its subtree have rects of zero. The rects keep float values. The engine does not round to the pixel grid. The renderer can snap at draw time.

### Algorithm

The engine is a structural port of Yoga `v3.2.1` (MIT). It follows the Yoga steps: measure, main-axis line breaking, main-axis distribution, cross-axis alignment, wrapped lines, and absolute children. It runs on `ref LayoutNode` structs and writes `float4` rects. `UiContext.Update` runs the layout pass on each root against `ViewportSize`, then clears the layout bits. The engine allocates nothing after warm-up. `LayoutAllocationTests` guards this.

### The measure callback

A `MeasureCallback` gives the content size of an element with no children, for example text or an image. A callback on an element with children has no effect. The callback gets the available inner size and a `MeasureMode` for each axis: `Undefined`, `Exactly`, or `AtMost`. It returns the size of the content only. The engine adds padding and border and applies the minimum and the maximum. The callback must not change the tree, mark a bit, or allocate. With `AtMost`, it must give the result of the `Undefined` size when that result fits the limit. A callback that breaks this contract can get a stale size from the layout cache.

**The measure scope.** A callback has no context argument, and an element holds no context reference. Thus `UiContext.Update` puts the measurer of the context into a thread-static scope (the internal `UiContext.CurrentMeasurer`) for the layout pass. It saves the old value first and restores it in a `finally` block, also after an exception. A nested `Update` of another context from a callback thus keeps the outer scope. Outside `Update`, the scope is null. The field is thread-static and not static, because the tests lay out trees on more than one thread. The save, the set, and the restore allocate nothing. The label callback throws `InvalidOperationException` when the scope is null. The image callback reads no scope.

**The content insets.** `LayoutNode.ContentInsets` (an internal `float4`) holds the padding plus the border of each edge in pixels. The components are left (X), top (Y), right (Z), and bottom (W). Each side is `padding + border`. `LayoutInternal` writes the insets in its `if (performLayout)` block, after the size of `ComputedRect`. The block runs for a computed layout and for a layout cache hit, so the value stays fresh. A measurement does not write the insets. The engine resolves the padding against the `ownerWidth` of the call. The measure uses the same width for its inner size. Thus a percent padding gives the same inset in the measure and in the draw. A percent padding with an undefined owner width gives 0, so the insets are never `NaN`. `ZeroOutLayoutRecursively` sets the insets of a hidden subtree to zero, as it does for the rect. `LayoutDifferentialTests` compares the insets of each element after the rect.

`LayoutNode.ContentBox(in float4 borderBox)` (internal) gives `(x + left, y + top, max(w - (left + right), 0), max(h - (top + bottom), 0))`. The sum of the two insets comes first, in the order of the engine for the inner width. Thus the content width of the draw equals the width that the callback got for an `Exactly` width. The label and the image read the content box in `PaintContent`. `ContentInsets` and `ContentBox` are internal. A public content box for game code is an open question.

The insets add 16 bytes to each element. The facts line goes from 730.5 to 746.5 bytes per element (730520 to 746528 bytes for 1000 elements).

### The dirty-bit rule and the cache

An element is dirty for layout when it holds `Layout` or `DescendantLayout`. The pass reads these bits and does not change them while it computes. Each pass takes a new generation number. On the first visit of a dirty element in a generation, the engine resets the cache of the element. On each visit, the engine reads the cache first. A hit sets the measured size and does not enter the subtree. A miss runs the flex steps and stores the result. After the pass, a clear walk removes the layout bits. A clean root with the same viewport costs one check. A deep change lays out again from the root, and each clean sibling subtree returns its cached size.

Each element holds one layout entry and eight measurement entries. The cache is stricter than the Yoga cache. It compares inner sizes, it uses an exact key for a callback leaf with a limit, and it adds the owner size to the key of each entry when the element or a child holds a percent. A hit then needs the same owner width and owner height. Thus the entries for two owner sizes stay valid together. This costs some cache hits and removes stale rects. `LayoutDifferentialTests` compares the incremental result with a fresh layout.

The engine recurses about 4 KB of stack for each level in a Debug build and about 2 KB in a Release build. A plain column chain passes 292 levels (Debug) and 507 levels (Release) on a 1 MB stack, the default for the main thread on Windows (measured by `--facts`). The Release value went from 519 to 507 levels with the content insets. The probable cause is a larger frame of `LayoutInternal`. On an 8 MB stack, it passes 2378 levels (Debug) and 4219 levels (Release). A stack overflow ends the process, and code cannot catch it. Keep the tree under 200 levels on a 1 MB stack. A wrapped container or a percent can use larger frames, so the limits are an upper bound.

### Differences from Yoga

The port drops these Yoga parts: `direction` and right-to-left layout, start and end edges, `gap`, `aspect-ratio`, baseline alignment, `position: static` and the containing-block search, `box-sizing`, `display: contents`, errata and experimental flags, the config object, the pixel-grid rounding, node cloning and owners, `hasNewLayout`, the dirtied callback, logging, the edge shorthands, the `flex` shorthand, and the reuse of a flex basis across passes.

The engine follows CSS in place of Yoga in these cases:

- The three space values on `AlignItems` and `AlignSelf` act as flex-start.
- A relative offset moves the child in its physical direction, also on a reverse axis and in a wrapped container.
- A percent margin of an absolute child resolves against the width.
- Step 8 places each item of a wrapped line by its margin box. It stretches the item to its line only.
- When the minimum or maximum freezes each growing item, the total grow factor is 0 or a small rounding value. Then each item keeps its bound flex basis. Yoga divides by that total and gives an infinite or very large size.
- A `WrapReverse` container mirrors the margin box of each item inside its content box. Thus the first line starts inside the padding and the border of the flex-end edge, and the trailing cross margin of the item comes first. Yoga mirrors the border box against the full cross size and uses the padding and the margins of the wrong edge.

### Design Decisions

- C# over a Yoga binding to keep the allocation rule and avoid a native binary per platform.
- No grid in the first release. Absolute position and flex cover game screens.

---

## 7. Style System

### Purpose

Classes, pseudo-states, and a cascade on the element tree.

### Model

```csharp
public readonly struct CompoundSelector : IEquatable<CompoundSelector>   // one compound, as Label.title:hover is in CSS
{
    public ElementTypeId Type { get; init; }    // none, or the type id that the element must have
    public ClassId Class { get; init; }         // none, or the one class that the element must hold
    public PseudoStates States { get; init; }   // none, or each state that the element must hold
    public string? Name { get; init; }          // null, or the name that the element must have (ordinal)
    public bool Equals(CompoundSelector other); // also Equals(object?), ==, !=, and GetHashCode
    public override string ToString();          // "Label.title#score:hover", "" for an empty compound
}

public enum SelectorCombinator : byte { None = 0, Child = 1, Descendant = 2 }

public readonly struct Selector : IEquatable<Selector>
{
    public bool Equals(Selector other);         // also Equals(object?), ==, !=, and GetHashCode
    public override string ToString();          // ".a:hover > .b", ".a .b", "(empty)" for default(Selector)
    public CompoundSelector Subject { get; }    // the compound that the element itself must match
    public CompoundSelector Ancestor { get; }   // default when Combinator is None
    public SelectorCombinator Combinator { get; }
    public ClassId Class { get; }               // the class of Subject
    public ElementTypeId Type { get; }          // the type of Subject
    public int Specificity { get; }             // (names << 16) | ((classes + states) << 8) | types
    public static Selector For(in CompoundSelector subject);
    public static Selector Child(in CompoundSelector parent, in CompoundSelector subject);           // .a > .b
    public static Selector Descendant(in CompoundSelector ancestor, in CompoundSelector subject);    // .a .b
    public static Selector ForClass(ClassId id);
    public static Selector ForName(string name);
    public static Selector ForType(ElementTypeId type);
    public static Selector ForTypeAndClass(ElementTypeId type, ClassId id);
}

public struct StyleProperties                   // sparse set: a mask and one value for each member
{
    public bool IsEmpty { get; }
    public bool IsSet(StyleProperty property);
    public void Clear(StyleProperty property);
    public Length Width { get; set; }           // one property for each member of ComputedStyle (32)
    // ...
}

public enum TransitionEasing : byte { Linear, EaseIn, EaseOut, EaseInOut }

public readonly struct StyleTransition          // 8 bytes
{
    public StyleTransition(float duration, TransitionEasing easing);   // throws for an easing that is not a member
    public float Duration { get; }              // seconds. The model keeps each float
    public TransitionEasing Easing { get; }
}

public struct StyleTransitions                  // sparse set: five fixed slots and a mask. 44 bytes, no heap object
{
    public static bool CanTransition(StyleProperty property);   // BackgroundColor, BorderColor, CornerRadius, Opacity, Color
    public readonly bool IsEmpty { get; }
    public readonly bool IsSet(StyleProperty property);
    public readonly StyleTransition Get(StyleProperty property); // default(StyleTransition) for an unset member
    public void Set(StyleProperty property, StyleTransition transition);   // ArgumentOutOfRangeException outside the five
    public void Clear(StyleProperty property);  // a member outside the five changes nothing
}

public readonly struct StyleRule
{
    public readonly Selector Selector;
    public readonly StyleProperties Properties; // a copy of the set at creation
    public readonly StyleTransitions Transitions; // a copy of the set at creation. Empty from the first constructor
    public readonly int Specificity;            // the value of the selector at creation
    public StyleRule(in Selector selector, in StyleProperties properties);
    public StyleRule(in Selector selector, in StyleProperties properties, in StyleTransitions transitions);
}

public sealed class StyleSheet                  // immutable
{
    public ReadOnlySpan<StyleRule> Rules { get; }   // sorted by specificity, then by the order of add

    public sealed class Builder
    {
        public Builder Add(in Selector selector, in StyleProperties properties);  // default(Selector) throws
        public Builder Add(in Selector selector, in StyleProperties properties, in StyleTransitions transitions);
        public StyleSheet Build();              // copies the rules, so a later Add changes no built sheet
    }
}

public readonly struct InlineStyle              // the view behind Element.Inline
{
    public Length Width { get; set; }           // a set to a new value marks Style
    // ... one property for each member of ComputedStyle
    public bool IsSet(StyleProperty property);
    public void Clear(StyleProperty property);  // a clear of an unset member marks nothing
}
```

A `StyleSheet` is an immutable, sorted rule list. `StyleSheet.Builder` copies each rule at `Build`. The context merges the rules of all its sheets into one cascade. The style pass matches the cascade against a dirty element and writes `ComputedStyle`. `StyleProperties` is a struct with a mask and one field for each member (about 250 bytes), so a rule and an inline set need no allocation per property. A set to the default value still sets the bit, so a rule can put a member back to its default. `Clear` is the only way to remove a value.

**The transition set.** `StyleRule.Transitions` holds the transitions of a rule in a `StyleTransitions` set. The set has five fixed slots, one for each member that can transition: `BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`, and `Color`. A change of these members changes the draw only. A mask holds one bit for each slot. `Set` writes a slot and its bit, and a second `Set` of the same member replaces the slot. `Set` and `Get` throw `ArgumentOutOfRangeException` for each other member, with a message that names the five. `CanTransition` gives the five, so a caller checks before the call. `IsSet` gives `false` and `Clear` changes nothing for a member outside the five. The model keeps each duration as it is (0, a negative value, `NaN`, and infinity). The parser rejects a negative duration (section 11). The set holds no heap object, so a copy allocates nothing, and `default(StyleTransitions)` is the empty set. The first `StyleRule` constructor and the first `Builder.Add` give an empty set. The sort of the builder copies each rule as a whole, and so does the merge of the cascade. Thus the set stays with its rule. The style pass merges the sets of the matched rules for each slot and gives the merged set to the transition pool (see "Transitions" below). The set does not change the target that the style pass writes.

`ComputedStyle` holds 32 members. They are the 25 layout inputs (section 6), four paint members, and three text members. The paint members are `BackgroundColor` (`Rgba8`), `BorderColor` (`Rgba8`), `CornerRadius` (`Corners`), and `Opacity` (`float`, default 1). The text members are `Color` (`Rgba8`), `Font` (`FontHandle`), and `FontSize` (`float`). `Label` reads the three text members from its style (section 8).

**The cascade order.** The style pass builds the style of a dirty element from `default(ComputedStyle)` in these steps:

1. The pass applies each matching rule in the order of the cascade. The order is the specificity (see the tiers below), then the order of `AddSheet`, then the rule order in the sheet. A later rule wins.
2. The pass applies the inline set last. An inline member always wins over a rule.
3. A text member that no rule and no inline value sets takes the value of the parent. A root takes the text default of the context.

**The selectors.** A selector holds a subject compound. A chain also holds an ancestor compound and a combinator. Set the parts of a compound with an object initializer, and give the compound to `Selector.For`, `Selector.Child`, or `Selector.Descendant`. The shortcuts `ForClass`, `ForName`, `ForType`, and `ForTypeAndClass` create a selector with one compound. Each factory throws `ArgumentException` for a wrong compound. A wrong compound holds no part, an empty name, or a state bit that is not a member of `PseudoStates`. `Child` and `Descendant` check the ancestor slot first, and the parameter name of the exception tells the slot. A `default(Selector)` matches nothing, and the builder rejects it.

**The equality and the text form.** `CompoundSelector` and `Selector` implement `IEquatable<T>`, `Equals(object?)`, `==`, `!=`, and `GetHashCode`. The compare reads each part: the type, the class, the states, and the name of each compound, and the combinator. The name compare is ordinal, and the hash of a `null` name is 0. The operators take `in` parameters. The compare allocates nothing. `ToString` gives the CSS text of section 11. A compound gives the type, `.class`, `#name`, and the states in the order `:hover:active:focus:disabled:checked`. A chain gives `ancestor > subject` or `ancestor subject`, and `default(Selector)` gives `(empty)`. `ToString` reads the global name tables and allocates, so use it for test output and debug output only. It never throws: an id outside a table gives `<type 7>` or `.<class 7>`, and a state bit outside `PseudoStates` gives `:<state N>`. A parsed selector equals the selector of the factories. For each form of the grammar, `Parse(selector.ToString())` gives an equal selector, for each name that the grammar of section 11 can read (an identifier for a type or a class, `[A-Za-z0-9_-]+` for a name). Another name gives a text that the parser does not read back.

**The specificity tiers.** `Selector.Specificity` is the one place that holds the weights. Each tier holds one byte of the value: `(names << 16) | ((classes + states) << 8) | types`. The name tier counts each compound with a name. The class tier counts each class and each pseudo-state. The type tier counts each compound with a type. The value is the sum over the subject and the ancestor. A count of a higher tier wins over each count of a lower tier while each count stays below 256. The largest selector of the grammar holds 2 names, 12 units of the class tier, and 2 types. Thus no count comes near 256.

| Selector | Specificity |
|----------|-------------|
| `Label` | 1 |
| `.a` or `:hover` | 256 |
| `Label.a` | 257 |
| `.a:hover` or `.a > .b` | 512 |
| `.a:hover:active:focus:disabled:checked .b:hover:active:focus:disabled:checked` | 3072 |
| `#title` | 65536 |

The stylesheet-model feature used the weights class 10 and type 1. The byte tiers change each number: 10 becomes 256, and 11 becomes 257. The order of each pair of the old rules does not change. `StyleRule` stores the value at creation.

**The match.** A compound matches an element when the element holds each part of the compound:

- The states: the element holds each state bit of the compound (`(element.States & States) == States`). The element can hold more bits. A compound with only states, as `:hover` is, matches each element in the state.
- The type: the id of `Element.TypeId` equals the type of the compound. The comparison is exact (see section 5 for a subclass).
- The class: the element holds the one class of the compound. A compound holds at most one class.
- The name: `Element.Name` equals the name of the compound with an ordinal, case-sensitive compare. An element with a `null` name matches no name part.

The match tests the parts in this order, the cheapest part first. A chain tests the subject first, because most rules miss there. A child chain then reads the parent only. A descendant chain reads the parent and then the grandparent, and the walk stops there. Thus each chain rule reads at most two ancestors, and its cost stays constant. This bound differs from CSS: in CSS, `.a .b` also matches when `.a` is above the grandparent. A root has no parent, so it matches no chain, and an element is not its own ancestor. A chain reads the tree: the type, the class, the name, and the states of the ancestor. It does not read the computed style of the ancestor. Thus the order of the style pass does not change the result. The match allocates nothing.

**The dirty rule for a class, a state, and a name.** A chain rule of a descendant can read the class, the states, and the name of an element. Thus each of these changes marks the element and its full subtree with `Style`: `ClassList.Add`, `ClassList.Remove`, a new `Element.States` value, and a new `Element.Name` (ordinal). Each element of the subtree with a child also gets `DescendantStyle`, and each ancestor gets `DescendantStyle`. A change to the same value marks nothing. The mark reads no sheet. With the walk bound, only the children and the grandchildren can change their match. Thus a deeper mark costs a restyle with no change of the result. A hover on a high ancestor restyles the full subtree on the next `Update`. An attach marks the full subtree also.

**The inheritance.** `Color`, `Font`, and `FontSize` inherit. The other members do not. When a text member of an element changes, the pass marks each child with `Style` during the visit, and the visit reads the bits again after the restyle. Thus a new value flows to the descendants in the same `Update`. A child with its own value for a member keeps it. A child that sets only `Color` still inherits `Font` and `FontSize`. A move to a new parent marks the full subtree of the moved element with `Style`, so a move into another context restyles each descendant with the sheets of that context. `AddRoot` marks the full subtree of the new root.

**The context defaults.** `UiContext.CreateInfo` holds `DefaultColor`, `DefaultFont`, and `DefaultFontSize`. A `null` color gives opaque white (255, 255, 255, 255), because `default(Rgba8)` is transparent and draws no text. The default font is the id 0, the default font of the renderer. The default font size is 0, the default size of the font. The create-info checks no value of the font size. A context from `UiContext()` has the same defaults.

**The dirty rule.** The pass compares the old style with the new style member by member and marks the kinds that changed:

| Changed member | Bit |
|----------------|-----|
| A layout member: the 25 inputs, with `Overflow`, `Display`, and `BorderWidth` | `Layout` |
| A paint member, or `Color` | `Paint` |
| `Font` or `FontSize` | `Paint`, and `Layout` also on a `Label` |
| No member (the same value, also `NaN` to `NaN`) | no bit |

`Overflow`, `Display`, and `BorderWidth` count as layout members, because the draw walk reads them and the layout engine reads them. A change of a text member also marks each child with `Style` (the inheritance). A plain element and an `Image` get `Paint` for a new font size, because only a `Label` or a `Button` measures text. A `Label` or a `Button` gets `Layout` for a new font or font size. The check is a type check in the pass.

**The write rule.** Game code writes `Element.Inline` or a rule. It does not write `Element.Style`. The style pass owns the style, so it rebuilds the style of a dirty element from the defaults. A direct write would not survive the next restyle.

**The sheets of a context.** `AddSheet` and `RemoveSheet` build the cascade again and mark each element of each root with `Style`. Both allocate, so call them in load code. `AddSheet` throws `InvalidOperationException` for a sheet that the context holds already. `RemoveSheet` returns `false` for an absent sheet. A sheet can be shared between contexts.

**The cost.** The inline set lives in each element, also when empty. It adds about 250 bytes to each element (`StyleProperties`) and 8 bytes for the view. The `--facts` line shows 1049.6 bytes for each element in the 1000-element tree with its 16-rule sheet (`bytes 1049648`). The value was 1049.5 before transitions and 1046.6 before style-parser. The two flags of transitions (`IsStyled` and `FollowsColor`) fit in the padding of `Element`. The 128 more bytes in the total do not come from each element. Each rule now holds a `StyleTransitions` set of 44 bytes. `StyleRule` is 368 bytes (320 before style-parser). The sheet and the cascade hold copies of the rules. The sheet holds a state rule, a child rule, a descendant rule, and a name rule. The style step of the allocation gate sets and clears a hover state. `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp` and the style step of `ThousandElementAllocationTests` guard the pass (section 14). The rules have no index yet, so each restyled element scans all rules of the cascade. A rule index waits for a benchmark.

### Transitions

A property transition has a duration and an easing. The `transition` declaration (section 11) goes into `StyleRule.Transitions` (see the transition set above). Five members can transition: `BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`, and `Color`. A change of these members changes the draw only. The style pass writes the target of each member. The transition pass of `UiContext.Update` then writes the value of each running tween over the target and marks `Paint` only. Each new type and each new member is `internal`. The tests reach `UiContext.UpdateTransitions` and `UiContext.Transitions` (the pool, with `ActiveCount` and `Capacity`) through `InternalsVisibleTo`.

**The record model.** Each `UiContext` owns one `TransitionPool`. The pool holds one `TransitionRecord` for each element with a running tween:

```
internal struct TweenSlot            // From and To (float4), Elapsed and Duration (seconds), Easing
internal struct TweenSlots           // [InlineArray(5)]: BackgroundColor 0, BorderColor 1, CornerRadius 2, Opacity 3, Color 4
internal struct TransitionRecord     // Element, byte Mask (one bit for each running slot), bool Follow, TweenSlots Slots
internal sealed class TransitionPool // TransitionRecord[] (dense), Dictionary<Element, int> (ReferenceEqualityComparer)
```

The records 0 to `ActiveCount - 1` run. A release moves the last record into the freed index and clears the last index, so the pool keeps no reference to a released element. A value goes into four floats: a color as R, G, B, and A (0 to 255), the corners as top-left, top-right, bottom-right, and bottom-left, and the opacity in the first float. The opacity reads the getter, so an unset opacity reads 1. `Follow` is the follow mark of `Advance` (see the transition pass below). It fits in the padding after `Mask`, so a record is 232 bytes, with or without the field.

**The merge rule.** `Restyle` merges the transition sets of the matched rules in the order of the cascade. For each slot, the last matched rule that holds the slot wins. The merge runs in the same loop as the properties, so the rule that wins a member also wins its slot. The set of the new style decides the duration and the easing: the hover in uses the hover rule, and the hover out uses the base rule. This differs from the CSS `transition` property, which takes the full list of the last rule. A hover rule that names one member thus keeps the transitions of the base rule for the other members. The parser merges the items of one rule in the same way (section 11).

**The restyle hook.** `Restyle` calls the internal `TransitionPool.Track` before it writes the target, when the merged set holds a slot or the pool holds a record. `Track` reads the old style (the visible value) and the new style (the target), and it changes neither. Then `Restyle` writes the target, sets `Element.IsStyled`, and writes `Element.FollowsColor`. `Track` runs these steps:

1. It finds the record of the element in the dictionary, only when the pool holds a record.
2. The first-style rule: when `IsStyled` is false, `Track` drops the record and returns. The first style of an element in its tree starts no tween, so a new screen does not fade in from its defaults.
3. For each slot, the first branch that applies wins:
   - **Keep:** the slot runs, the target equals its `To`, the merged set of the new style holds the slot with a valid duration (see the duration rule), and the inline set does not hold the member. The tween keeps its start value, its elapsed time, and its duration. A restyle with the same target thus does not restart a fade. A hover on an ancestor restyles the full subtree and keeps the matched rules, so each tween below it keeps running. The set of the new style decides: a new style with no valid slot for the member skips this branch, and the slot ends in the next branches, as in step 4 of the CSS start algorithm. Thus a class swap to a rule with the same target and no transition ends the tween at once.
   - **No change:** the target equals the visible value. A running slot ends.
   - **Start or turn:** the merged set holds the slot with a valid duration, the visible value and the target are finite, and the inline set does not hold the member. The tween starts from the visible value with the full duration and the easing of the merged set. A running tween with a new target turns here. The first start of an element rents a record.
   - **Else:** a running slot ends. The style pass writes the target at once.
4. It releases the record when no slot runs.

**The turn.** A new target during a fade starts a new tween from the visible value of the last `Update`, with the full duration. This differs from CSS. CSS shortens a transition that turns back to its start value (the reversing shortening factor). brutal-ui does not shorten it. Thus a hover out at the midpoint of a 0.2 s fade takes 0.2 s to go back.

**The inline rule.** A member of the inline set never fades. An inline write changes the member at once and ends its running slot. A member that leaves the inline set follows the start rule: the old inline value is the visible value, and the value of the rules is the target. The member fades when the merged set holds the slot.

**The duration rule.** A valid duration is finite and more than 0. A tween starts only for a valid duration. A duration of 0, -0, a negative value, `NaN`, or an infinity gives the target at once and no record. Such a slot acts as no transition for the member (Decision R3 of the feature context): it starts no tween, it keeps no tween, and it does not stop the follow of `Color`. One internal check, `TransitionPool.CanRun`, holds the rule for the start branch, the keep branch, and the write of `FollowsColor`, so the three rules cannot drift. The parser rejects a negative duration already. The model keeps each value.

**The `deltaTime` rule.** Each `Update` advances each running tween by `deltaTime`, also in the `Update` that starts it. A progress of 1 or more writes the exact target and ends the slot. Thus each member ends on the value of the cascade. A `deltaTime` of 0 or less, also `NaN`, advances nothing. The pass still writes the visible value of each running slot. An infinite `deltaTime` ends each tween at its target. Pass the real frame time to `Update`.

**The curves.** `Linear` gives the progress. The other three easings use the CSS cubic Bezier curves, with the first and the last control points at (0, 0) and (1, 1):

| Easing | Control points | Value at 0.5 |
|--------|----------------|--------------|
| `EaseIn` | (0.42, 0) and (1, 1) | 0.31536 |
| `EaseOut` | (0, 0) and (0.58, 1) | 0.68464 |
| `EaseInOut` | (0.42, 0) and (0.58, 1) | 0.5 |

The solver finds the curve parameter for the progress with up to 8 Newton steps from `t = progress`. It stops at an x error below 1e-6. When the slope is below 1e-6 or a step leaves 0 to 1, the solver uses up to 32 bisection steps on 0 to 1 with the same tolerance. A progress of 0 or less, also `NaN`, gives 0. A progress of 1 or more gives 1. The solver uses no table and allocates nothing.

**The color step.** A color slot steps in premultiplied alpha, as CSS Color 4 does. The new alpha is `a = Lerp(a0, a1, t)`, with each alpha from 0 to 255. Each RGB channel is `Lerp(c0 * a0, c1 * a1, t) / a`. With a new alpha of 0 or less, the RGB channels take a straight step. Each channel rounds half up and clamps to 0 to 255, and `NaN` gives 0. Thus a fade from transparent black to red gives red with a growing alpha, not a dark red: `#00000000` to `#FF0000FF` at 0.5 gives `#FF000080`. For two opaque colors, the result equals a straight step: `#1E222CFF` to `#3060C0FF` at 0.5 gives `#274176FF`. The corners and the opacity take a linear step for each float and write the exact floats.

**The transition pass.** `UpdateTransitions` calls `TransitionPool.Advance(deltaTime, roots)` only when a tween runs. `Advance` runs three loops. Each loop visits the records from the last to the first, so a release skips no record.

1. Loop 0, the tree check: for each record, the pass walks up the parents to the top element. Then it compares the top element with each root of the context by reference. When no root matches, the element left the tree, and the pass releases the record with no write and no mark.
2. Loop 1, the step and the write: for each running slot, the pass adds the step to the elapsed time and computes the value. It writes the value only when it differs from the member, and it marks `Paint` on an element with a changed member. A record whose `Color` slot runs at the start of the step gets the follow mark: the pass sets `TransitionRecord.Follow` before the step, because the last step clears the slot bit. Bits 0 to 4 of `Mask` are the slot bits, and no other bit of `Mask` has a meaning.
3. Loop 2, the follow walks and the releases: for each record with the follow mark, the pass clears `Follow` and runs the follow walk. Then it releases each record with no running slot. No record holds the mark after the call.

**The follow rule of `Color`.** `Restyle` writes `FollowsColor` on each element: `true` when no matching rule and no inline value sets `Color`, and the merged set holds no `Color` slot with a valid duration. After each step of a running `Color` slot, the pass writes the visible color into each child with `FollowsColor`, and then into the children of that child, down the tree. This also happens at the last step and when the bytes did not change, because the style pass can write the target into a child in the same `Update`. The walk skips a child with `FollowsColor` false and its subtree. A child with its own `Color` transition thus runs its own tween and never follows (Decision R2 of the feature context). It ends on the same target as its parent, so its color does not jump back when its own tween ends. A child with a `Color` transition of 0 s (or another duration that is not valid) follows, as in CSS (Decision R3). The walk also skips a child with a running `Color` slot and its subtree. This skip is a guard only: the keep rule and the flag use the same check, so a running `Color` slot comes with `FollowsColor` false. A child with a record and no running `Color` slot (for example, a fill tween) follows. The walk writes a color only when it differs, and it marks `Paint` only. The walks run in loop 2, after each step of loop 1, so the order of the records does not change the result.

**The leave and join rules.** The pass drops the record of an element outside the tree of its context, with no write and no mark (loop 0). An element has no context reference, so the check goes from the element up. A write at the drop could write over the style that a second context gave the element after a move. An attach (`Children.Add` and `Children.Insert`) and `AddRoot` set `IsStyled` to false on the full subtree (`Element.MarkSubtreeJoined`). The next restyle then drops each record of the subtree in `Track` and starts no tween. Thus a move into another context starts no tween. `ElementList.Move` does not attach, so a move inside one child list keeps each running tween. A `Remove` and then an `Insert` end the tweens of the element. An element outside each tree keeps the last value of its tween (for example, the midpoint) until a join restyles it to the target.

**The pool growth.** The record array starts empty. The first rent grows it to 4 records, and each later growth doubles it. The array and the dictionary never shrink. Thus the pool allocates only while it grows to the peak count of the game. After this warm-up, `Track` and `Advance` allocate no managed memory.

**The cost.** With no running tween, `Update` pays one count check for the transition pass, and `Restyle` calls `Track` only when a matched rule holds a transition. `Track` skips the dictionary lookup while the pool is empty. While a tween runs, each restyled element costs one `Track` call with one dictionary lookup. `Advance` costs, for each record, one walk to the top of the tree and one compare with each root, then the step of each running slot. Each running `Color` slot adds one walk of the inheriting part of its subtree, with one dictionary lookup for each visited child. A restyle of an element with a running tween compares the visible value with the target, so `Compare` marks `Paint`. The transition pass marks `Paint` in the same `Update` anyway. A probe of the review measured the 1000-element tree in a Release build (see `## Performance Considerations` of the feature context). The worst case is a root hover toggle in each `Update` with a tween on each element. It adds about 0.36 ms to each `Update` with 5 slots on each element, and about 0.39 ms with 4 slots and a root `Color` tween that 886 elements follow. `Advance` alone takes about 0.22 ms of it. `Update` allocates 0 bytes.

### Design Decisions

- Pseudo-states: `:hover`, `:active`, `:focus`, `:disabled`, `:checked`. A compound holds them as the bits of `PseudoStates`, and the match is bit math.
- Selector depth 2 (`.a > .b` and `.a .b`) in the first release. Depth is a constant, not a list. A selector holds one subject compound and one ancestor compound by value. Its only heap reference is the name string of a compound.
- A descendant chain walks to the grandparent only. Each chain rule has a constant cost, and the result differs from CSS for a deeper ancestor.
- One class for each compound. `.a.b` is not in the first release. The byte tiers already hold a larger count.
- A name rule compares `Element.Name` with an ordinal compare. No intern table and no name id: the compare allocates nothing, and a name set takes no lock.
- One byte for each specificity tier. The CSS order holds for each count below 256, with no proof that depends on the grammar.
- A class change, a state change, and a name change mark the full subtree, with no check of the sheets. The mark is simple and correct. A sheet-aware mark waits for a benchmark.
- `CompoundSelector` and `Selector` have their own equality members and a CSS `ToString`. The compare is ordinal and allocates nothing, and no call reaches the reflection compare in `ValueType.Equals`. The hot path compares no selector. A parser test compares a parsed selector with the selector of the factories. A failed test shows the CSS text of both values.
- A rule stores its transitions in five fixed slots and a mask, with no heap array. A comma list in one declaration sets more than one slot.
- The running tweens live in a pool of the context: one record for each element with a running tween, in a dense array, with a reference dictionary from the element to its record. The element does not grow by a slot set, and the transition pass loops over the running records only, with no tree walk. The pool grows during warm-up and never shrinks.
- A `Color` tween reaches the inheriting descendants through a follow walk of the transition pass, with no record and no restyle for each child. The walks run in their own loop after each step, so the order of the records does not change the result.
- The style pass has its own recursion and not `DirtyWalk`, because it marks children during the visit.
- `StyleRule` stores the specificity at creation, so a change of the weights touches `Selector.Specificity` only.
- Theme variables (`--name`) resolve at parse time per stylesheet, not at runtime.

---

## 8. Renderer

### Purpose

Turn the element tree into pixels through an interface.

### Interface

```csharp
public interface IUiRenderer
{
    void Begin(in float2 displaySize, float scale);
    void PushClip(in float4 rect);
    void PopClip();
    void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor);
    void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius);
    void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8, float wrapWidth);
    float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth);
    void End();
}
```

`Text` and `MeasureText` use the same wrap rule: a wrap width of 0 or less, or `NaN`, means no wrap, and the same wrap width gives the same lines.

### ImGui implementation

In this epic, `ImGuiRenderer` writes into the background draw list of the main viewport (Q2). The list is under all ImGui windows, so a debug window shows on top of the UI. A private list waits for the world-space epic. Then `Begin` gets a second draw-list source. `Begin` reads the list in one place, and both renderers share the two services.

**Services.** `ImGuiFontService` maps a `FontHandle` to an `ImFontPtr` of `io.Fonts`. Id 0 is the default font: `io.FontDefault`, else the first font of the atlas, else one `AddFontDefault()` call. `Add` registers a font that the game loaded, and it gives ids from 1 that never repeat. `ImGuiTextureService` maps a `TextureHandle` to an `ImTextureRef`. `Add` takes a reference that the game registered. `AddVulkan` calls `VulkanBackend.AddTexture` (the OpenGL equivalent waits). `AddVulkan` checks the size, the sampler, the image view, and the backend before it calls the backend. The handle holds the size of the texture. The service owns the registration only, and the game owns the image, the view, and the sampler. `Remove` makes the id unknown for ever, and for a Vulkan texture it calls `RemoveTexture`. Call `Remove` after `vulkan.WaitIdle()` and before the backend is disposed. The font service needs a current ImGui context. The texture service reads no ImGui state.

**Handle errors (Decision 7).** An unknown font id, an unknown texture id, a removed texture, and the none handle in a lookup throw `ArgumentOutOfRangeException`. The none handle in `Image` draws nothing, but a stale id throws, also for a tint alpha of 0 or a rect with no area. A draw call before `Begin` throws `InvalidOperationException`. So do a `PopClip` with no open clip, a `PopClip` when the clip stack of the list holds only the viewport clip (a frame with no `End` in an earlier ImGui frame), and a `Begin`, `Text`, or `MeasureText` with no current context. Each check runs before the native call, because a native ImGui assert does not stop the process. `Begin` does not detect a call outside an ImGui frame. The game calls `Draw` between `ImGui.NewFrame` and `ImGui.Render`. `Update` can run before `ImGui.NewFrame` (see "Label and image").

**Explicit font and size (Decision 2).** `Text` and `MeasureText` pass the font and the size to each ImGui call (`AddText` with a font and a size, `ImFont.CalcTextSizeA`). They push no font and use no font stack of the frame, so `MeasureText` works before the first frame, inside a frame, and between frames. The dynamic fonts of ImGui 1.92 bake each size on demand. A size of 0 or less, `NaN`, or infinity gives the size of the font (the size of its `AddFont` call). A resolved size of 0 or less, or not finite, draws nothing and measures `(0, 0)`, because ImGui stops the process on a negative size.

**Size and wrap rules.** `MeasureText` resolves the font before the empty-text check, so an unknown id throws also for empty text. Empty text gives `(0, size)`. The width rounds up to a whole pixel with `floor(width + 0.99999)` in single precision, as `ImGui.CalcTextSize` does. A whole width of 256 or more gives the width plus 1, as ImGui does. The height is the line count times the size, and a line feed at the end adds no line. A wrap width of 0 or less, `NaN`, or infinity means no wrap. With a wrap, a line breaks after a blank or after one of the marks `. , ; ! ? "`. ImGui cuts a word that is wider than the wrap width inside the word, so the width stays at or below the wrap width, except when the wrap width is below one glyph. `Text` skips an empty span, an alpha of 0, a resolved size that is not drawable, and a position that is not finite.

**Corners (Decision 4).** ImGui rounds a rect with one radius and a corner mask. Each radius of 0 or less, or `NaN`, becomes 0, and each radius is limited to half the shorter side. The rounding is the largest radius, and the mask names each corner with a radius above 0. With no such corner, the mask is `RoundCornersNone`, because ImGui reads a mask with no corner bit as "all corners". The result is exact when the radii above 0 are equal. Mixed radii above 0 draw with the largest one. An exact path for mixed radii is a later change. `Image` uses the same rule with `AddImageRounded`. A rounded fill and a rounded image draw an anti-alias fringe of 0.5 pixel.

**Border (Decision 5).** The border stays inside the box. The stroke rect is the rect inset by `borderWidth / 2 - 0.5`, and the stroke radius is `max(radius - borderWidth / 2, 0)`, so the outer edge of the stroke follows the radius of the fill. The anti-alias fringe of the stroke reaches 1 pixel outside the rect. A border below 1 pixel draws as 1 pixel: ImGui draws a thin stroke as 1 pixel, so the inset uses `max(borderWidth, 1)` and is never below 0. A border of `borderWidth * 2 >= min(width, height)` covers the box. It draws as one fill in the border color with no stroke, and an opaque border skips the hidden fill. A rect draws nothing when its width or height is 0 or less, or when a component is not finite. **The finite bounds rule:** a rect or an image also draws nothing when a bound in viewport pixels is not finite, because two finite values can add up to infinity (`3e38 + 3e38`).

**Viewport (Decision 6).** A viewport pixel is one ImGui display unit. The renderer adds the position of the main viewport to each position. It uses neither the `scale` argument nor the `displaySize` argument of `Begin`, and the ImGui backend applies the framebuffer scale. The game sets `UiContext.ViewportSize` equal to `io.DisplaySize`. `PushClip` intersects with the current clip, so a finite clip stays in the viewport. A clip with an area of 0 is valid, and the Vulkan backend skips its draw commands. **The non-finite clip rule:** when a bound of the clip is not finite (a `NaN` or infinite component, or an overflow of `x + width`), `PushClip` pushes a clip with an area of 0. A coordinate of the top-left point that is not finite becomes the origin, and the bottom-right point equals the top-left point. The push still happens, so the stack stays balanced. ImGui keeps a `NaN` and an infinite min in its intersect, and the Vulkan backend casts the clip to an integer scissor. `End` pops each clip that is still open.

**Allocation.** `Begin`, the draw calls, `MeasureText`, and `End` allocate no managed memory after warm-up. A warm-up frame must use each font and size, because ImGui bakes a size on its first use.

### The draw walk

`UiContext.Draw(IUiRenderer)` calls `Begin(ViewportSize, 1f)`, runs `PaintWalk` on the roots in the order of add, and calls `End()`. `PaintWalk` is an internal static class with an explicit frame stack. The rules follow.

**Rules of the walk**

- **Rect form (Q1).** The walk passes x, y, w, h in viewport pixels to `Rect`, `Image`, and `PushClip`. The walk adds the position of each parent on the way down. A `ScrollView` moves its children by its offset: they use the origin of the view minus `AppliedOffset()`. The view, its clip, and its `PaintContent` use the origin with no offset, so the clip does not move (section 5, the Offset Rule of the Walks).
- **Hidden (Q2).** `Display.None` or an opacity of 0 hides an element and its subtree. The walk emits no call and pushes no clip for it. A root with `Display.None` draws nothing.
- **Opacity (Q3).** Opacity multiplies down the subtree into the alpha of every color. `Opacity` is a property with a default of 1. The walk clamps the value to 0 to 1, and `NaN` counts as 1.
- **Clip (Q4).** `Overflow.Hidden` clips to the padding box. The walk intersects it with the parent clip. `Overflow.Scroll` also clips.
- **Paint order (A1).** The order is tree order: the `Rect` of the parent first, then the children in child order. This epic has no z-index.
- **Read only (A2).** `Draw` reads the tree only. It changes no dirty bit, because `Update` already clears the paint bits. A renderer or a hook must not change the tree or call `Draw` during a draw.
- **Scale (A3).** `Draw(IUiRenderer)` calls `Begin(ViewportSize, 1f)`. A scale parameter waits for a later epic.
- **Border (A4).** `Rect` takes one border width. The walk uses the left border width of `Style.BorderWidth` (points only). Per-side borders wait. When the border is not visible, the walk passes width 0 and a transparent border color. The walk emits a `Rect` only for a visible fill or a visible border.
- **Padding box (A7).** The walk resolves the padding box from `Style.BorderWidth` at draw time. `LayoutNode.ContentInsets` holds the padding plus the border, not the border alone, so the walk does not read it.
- **Stack (A8).** The walk uses an explicit stack, not recursion, so a deep tree does not overflow the call stack. The stack starts at 32 frames, grows by doubling, and never shrinks. The walk allocates only during warm-up.

**Order per element:** the hidden check, `Rect`, `PushClip`, `PaintContent`, the children, `PopClip`.

**The paint hook.** `Element.PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` is an internal virtual method with an empty body. The walk calls it after the `Rect` and the clip push, and before the children. Thus the clip of an element also clips its own content. The border box is in viewport pixels. The `opacity` argument is the product down the tree, so an override scales its own colors with it. The hook changes no tree, sets no mark, allocates nothing, and does not call `Draw`. The label and image elements override it. A `TextField` also overrides it. It draws its selection, its text, and its caret, in this order. The text and the caret move left by the text scroll, and the clip of the walk cuts them at the padding box. The field measures the text before the caret and the selection with the renderer of the draw, which is also the measurer of the context in the ImGui path. The hook writes no scroll. The caret pass writes it (section 4).

**The value types.**

- `Rgba8` holds four bytes: R, G, B, A (A5). Its memory layout matches `ImU32` on a little-endian CPU, so ImGui casts it at no cost.
- `Corners` holds four floats in this order: top-left, top-right, bottom-right, bottom-left (A6).
- `FontHandle` and `TextureHandle` hold an integer id. The id 0 of a font is the default font of the renderer. The id 0 of a texture means no texture, and `Image` draws nothing for it.

### Label and image

**The measurer slot.** `UiContext.CreateInfo.Measurer` holds the `IUiRenderer` that measures text for the layout pass. The label callback reaches it through the measure scope of `Update` (section 6). `ImGuiRenderer.MeasureText` pushes no font, so the measure also runs outside the ImGui frame. It needs a current ImGui context only. Thus `Update` can run before `ImGui.NewFrame`, and `Draw` runs between `ImGui.NewFrame` and `ImGui.Render`. Create the renderer first, then the context, and call `Update` before `Draw`.

**The label measure rule.** The callback gives the content size of the text:

```
free = MeasureText(font, size, text, 0)                 // no wrap
if widthMode == Undefined:            return free
limit = SnapWrapWidth(width)
if limit <= 0 or free.X <= limit:     return free       // the AtMost contract, and Exactly with text that fits
return MeasureText(font, size, text, limit)             // wrap at the limit
```

The callback calls the measurer one or two times. It sets each component of a measurer result that is not finite, or that is below 0, to 0, so the engine always gets a finite size. It does not clamp to the limit. The engine adds the padding and the border, and it applies `Exactly`, the minimum, and the maximum. The layout cache stops a repeat of the same constraint, so the label keeps no measure cache. An empty text measures as one line: `(0, size)`.

**The label draw rule.** The label draws its text at the top-left corner of its content box. The color is `Style.Color` scaled by the opacity. An empty text, or a color with an alpha of 0 after the scale, draws nothing. The call is `Text((content.X, content.Y), Style.Font, Style.FontSize, color, Text, DrawWrapWidth(content.Z, borderBox.Z))`. The measure and the draw give the same lines, in the limits of the snap rule. With `Exactly`, both sides wrap at the same snapped width. With `AtMost`, the measure wraps at the limit and gives the widest line, and the draw wraps at that width. A greedy break at the limit takes the longest prefix that fits. That prefix also fits the widest line, so the draw takes the same prefix. The values differ, and the lines stay the same.

**The snap rule.** `Label.SnapWrapWidth` (internal) gives the wrap width of the measure and of the draw. A value that is not finite, or 0 or less, gives 0 (no wrap). A value within 1/64 pixel of a whole number gives that whole number. Another value stays the same. The snap removes float noise near a whole number. The value `(w + insets) - insets` can differ from `w` by one unit in the last place of the box width. A line that fits exactly can then wrap. A content width of 0 thus draws with no wrap, and the text extends past the box. `Overflow.Hidden` clips it.

**The draw raise.** `Label.DrawWrapWidth` (internal) gives the wrap width of the draw. It snaps the content width first. A result of 0 or a whole number stays the same, so a whole-pixel measurer gets the exact snapped width. A fractional result gets `2 * (BitIncrement(borderBox.Z) - borderBox.Z)` on top. The draw width is then never below the measured width `w`, and the widest line does not wrap. The rule has two limits for a measurer with fractional widths. (1) With `Exactly`, the draw wraps a little above the measure limit. A break can differ only when a line fills the limit to within this noise. (2) A measured width within 1/64 pixel above a whole number snaps down in the draw, and the widest line can wrap. `ImGuiRenderer` rounds each width up to a whole pixel, so it meets neither limit. `MeasureText` does not ask for whole-pixel widths. A contract change would close limit (2), and it is open work.

**The image rules.** The natural size of an image is `Texture.Size` times the absolute UV span, one pixel for each texel. Thus a region of an atlas takes the size of the region. The none handle, or a texture size with a component of 0 or less, gives `(0, 0)`. Such an image takes no space and draws nothing. A UV span that is not finite gives 0 on its axis. A UV component that is not finite draws nothing in each scale mode. The measure works on each axis alone. `Undefined` gives the natural size. `AtMost` gives the smaller value of the natural size and the limit. `Exactly` gives the limit. A limit that is not finite gives the natural size. A style size overrides the natural size. The draw scales the tint by the opacity, and a tint alpha of 0 draws nothing. The image draws with square corners.

| Scale mode | Draw rect | UV rect |
|------------|-----------|---------|
| `StretchToFill` (default) | the content box | `Uv` |
| `ScaleToFit` | the largest rect with the aspect of the natural size, in the center of the content box | `Uv` |
| `ScaleAndCrop` | the content box | `Uv` cut around its center to the aspect of the content box |

The aspect is `natural.X / natural.Y`. A natural size with a component of 0, or a content box with no area, uses the `StretchToFill` rule. The image then still sends the call, and the renderer skips a rect with no area. A flipped UV rect keeps its direction, because the crop scales the signed span. An unknown `ImageScaleMode` value draws as `StretchToFill`. The rules are internal static functions (`Image.NaturalSize`, `Image.MeasureAxis`, `Image.FitRect`, and `Image.CropUv`), so a test checks them with no tree.

**Limits.** An image with a style width and an auto height keeps its natural height, so its aspect changes. The `ScaleToFit` rect can have fractional pixels, and the image does not snap it. A minimum width above the available width gives the box the lines of the available width. The draw wraps at the box width and can use fewer lines. A label has no single-line mode and no text alignment.

**The text style.** `Label` has no color, font, or size member. It reads `Style.Color`, `Style.Font`, and `Style.FontSize`, which a rule, an inline value, or the parent sets (section 7). Write them with `label.Inline.Color`, `label.Inline.Font`, and `label.Inline.FontSize`, or set them on an ancestor. The measure callback and the draw take `ref readonly ComputedStyle style = ref Style` one time, so they copy no struct. A label with no value draws in the default color of the context (opaque white by default). A change of `Font` or `FontSize` marks `Layout` and `Paint`, and a change of `Color` marks `Paint` only.

### Design Decisions

- The interface takes spans and structs only. No strings, no arrays.
- Shadows and blur are out of scope for the first release. The interface gains an `Effect` command later, implemented through `ImDrawList.AddCallback`.

---

## 9. Input

### Purpose

Route GLFW events to the element tree and expose a gate for the game.

### Flow

1. `GlfwUiInput` (package `BrutalUi.Glfw`) subscribes to the events of a `GlfwWindow`: cursor position, mouse button, scroll, cursor enter, drop, content scale, key, and char. The key event carries the key, the scan code, the action (press, repeat, release), and the modifiers. A char event carries a Unicode scalar value, and the adapter drops any other value. The property `KeyboardEnabled` (default `true`) switches the key and char events off. A cursor leave pushes a pointer move at (NaN, NaN), so the pointer hits nothing and `Hover` clears. The positions stay in GLFW screen coordinates. Create the window, then `GlfwBackend`, then `GlfwUiInput`. Dispose in the opposite order.
2. The adapter pushes each event to the ring of the context with `UiContext.PushEvent`. The internal `DrainEvents` walks each entry (section 5, "Events"). `UiContext.Update` drains the ring at its start, before the style pass. The game calls no drain. A handler that calls `Update` gets `InvalidOperationException`. The ring keeps 256 entries by default. A game with long hitches sets `CreateInfo.EventQueueCapacity`.
3. A pointer or scroll event with no target hit-tests the tree. The internal `HitWalk` uses an explicit stack on `UiContext` and no recursion. The hit rules are:
   - The walk visits the last root first, the last child first, and a descendant before its ancestor.
   - The border box of an element is its hit area. The edges are half-open: the left and top edges hit, the right and bottom edges do not. A NaN point hits nothing.
   - An element with an overflow other than `Visible` clips to its padding box, intersected with the clip of its parent (`PaintWalk.PaddingBounds` and `PaintWalk.Intersect`, shared with the draw). A point outside the clip skips the subtree.
   - `Display.None` and an opacity product of 0 or less skip the subtree.
   - `IsPickable` set to false skips the element only. Its children stay pickable.
   - A `ScrollView` moves the hit area of its children by its offset, as the draw does: they use the origin of the view minus `AppliedOffset()`. The view and its clip use the origin with no offset. The walk of a hit and the walk of a draw thus agree on each point (section 5, the Offset Rule of the Walks).
4. The state rules are: `Hover` goes to the hit element only. `Active` goes to the first press target and stays until the last button goes up. The release order: the press sets `Active` before its walk, and the release clears `Active` after its walk (Decision 2 of button-and-toggle). A `PointerUp` handler then reads `Active` on the pressed element. This amends Decision 9 of pointer-routing for the release only. A move with no target walks to the element of the left press when that element captures the moves (`IDragCapture`, section 5, "Slider") and is shown. A hidden element (`Display.None` on it or on an ancestor) captures no move. The hit element still gets `Hover` and sets `PointerHit`. The states follow the pointer events only, and a stored element that leaves the roots loses its state at the start of the next drain.
5. A layout root sets `IsPickable` to false. Then an empty area of the root passes the pointer to the game, because the hit test finds no element there. The routing gate reads `PointerHit` for this. HelloPanel is the example: its root covers the window and sets `IsPickable = false`, so the gate reads `false` outside the panel.
6. The event walks capture, target, bubble. A handler can stop propagation.
7. Focus moves on click, Tab, and Shift+Tab, in tree order (section 5, "Events"). `UiContext.FocusNext` and `UiContext.FocusPrevious` are the slots for gamepad navigation. The action map binds a gamepad button to them (Q2 of the epic). Key and char events go to the focus owner. With no owner, they get no walk. A focused `TextField` takes the char and the edit keys (Left, Right, Home, End, Backspace, Delete, and Enter) and stops their walk. Tab walks on, so the focus moves on.
   - The ImGui rule: both the ImGui `GlfwBackend` and `GlfwUiInput` get each key and char event. The game sets `input.KeyboardEnabled = !io.WantCaptureKeyboard;` before `Glfw.PollEvents()`. Without it, a Tab moves both focuses.
   - The pointer rule (Decision 5 of routing-gate): `GlfwUiInput` has no pointer switch. With an ImGui window next to the UI, the game uses `ui.RoutingGate.PointerOverUi || io.WantCaptureMouse` for its own pointer input.
   - A switch from `true` to `false` can leave a key down with no key up. An element that tracks a held key must handle a focus loss.
   - `Disabled` does not pass to the children. A game that disables a panel also disables each focusable descendant.
8. `UiContext.RoutingGate` tells the game to stand down. It returns a `UiRoutingGate`, a `readonly struct` with two values: `PointerOverUi` and `KeyboardFocusInUi`. The game reads `ui.RoutingGate.PointerOverUi` after `Update` and before it handles its own input. The property computes the value at each read from `PointerHit` and `FocusedElement` (a live read), allocates nothing, and has no event. A code focus call shows in the gate at once.
   - The pointer rule: `PointerOverUi` is `true` when the last pointer or scroll entry with no target hit an element. An entry with a target from the caller changes nothing.
   - The keyboard rule: `KeyboardFocusInUi` is `true` when the context has a focus owner.
   - The still-pointer limit: a layout change under a still pointer, for example a panel that closes, keeps `PointerOverUi` until the next pointer event or the next stale rule.
9. Two window events skip the queue and the routing. `UiContext.OnFileDrop` and `UiContext.OnContentScale` are slots for a handler. `RaiseFileDrop` and `RaiseContentScale` call them at once, and the adapter calls the Raise methods from the GLFW events. A null slot does nothing.

### Action map

A `UiActionMap` names actions (`Confirm`, `Cancel`, `NavigateUp`) and binds keys, mouse buttons, and gamepad buttons through `Glfw.GetGamepadState`. A mode lists its active maps. The map calls the same command registry as a button click. This replaces the Unity Input System layer from mosaic-ui.

---

## 10. Framework Layer

### Purpose

The mosaic-ui model on the brutal-ui tree.

### Components

| Component | Source in mosaic-ui | Change |
|-----------|---------------------|--------|
| `Store<T>`, `StoreSubscription` | `State/` | Replace `INotifyBindablePropertyChanged` with `IBindingSource`. |
| `EventBus`, `ServiceRegistry`, `CommandRegistry`, `ModeHistory` | `Core/`, `Interaction/`, `Modes/` | Copy. |
| `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `WindowDefinition` | `Panels/`, `Modes/`, `Windows/` | Records. A loader reads them from XML. |
| `PanelController`, `PanelInstance` | `Panels/` | Root becomes `Element`. |
| `UiManager`, `SlotContainer` | `Layout/` | Same diff logic on `Element`. |
| `WindowChrome`, `WindowManager`, `WindowPersistence` | `Windows/` | Chrome is an element. Persistence writes a file next to the executable. |
| `DataList` | `Components/` | Wraps the virtualized `ListView`. |
| `WorldFeature`, `WorldController` | `WorldSpace/` | Interfaces the game implements. |

### Data binding

`Label.BindText(store, selector)` and friends subscribe to a store slice. A change marks the element dirty. A binding is a struct in a pooled list on the element.

---

## 11. Markup

### XML element format

```xml
<ui:Panel class="hud" xmlns:ui="brutal-ui">
  <ui:Label name="score" class="hud-value" text="0" />
  <ui:Button name="pause" class="hud-button" text="Pause" command="game.pause" />
</ui:Panel>
```

The loader maps element names to registered types and attributes to properties through a generated table, with no reflection in the loaded path. A game registers custom element types by name.

### Stylesheet format

The `BrutalUi.Markup` package reads a stylesheet text into a `StyleSheet`. The sheet is the same type that `StyleSheet.Builder` makes (section 7). The format is a small part of CSS. `samples/HelloPanel/HelloScreen.css` is a full example.

```css
:root { --panel-fill: #1e222c; --text: #ffffff; }
.hello-panel { flex-direction: column; padding: 16px; border-radius: 8px; background-color: var(--panel-fill); }
.hello-panel > Label { color: var(--text); font-size: 26px; }
.button:hover { background-color: #3060c0; transition: background-color 0.2s ease-out, opacity 150ms linear; }
```

**The parser API.**

```csharp
namespace BrutalUi.Markup;

public sealed class StyleSheetParser
{
    public struct CreateInfo
    {
        public IReadOnlyDictionary<string, FontHandle>? Fonts;   // null: only "font: default;"
    }

    public StyleSheetParser();
    public StyleSheetParser(in CreateInfo info);          // copies the font map
    public StyleParseResult Parse(ReadOnlySpan<byte> utf8, string sourceName = "stylesheet");
    public StyleParseResult Parse(string text, string sourceName = "stylesheet");   // encodes to UTF-8, then parses
}

public sealed class StyleParseResult
{
    public StyleSheet? Sheet { get; }                     // null when Errors holds an error
    public IReadOnlyList<StyleParseError> Errors { get; } // in the order of the source. Empty on success
    public bool Succeeded { get; }                        // Sheet is not null
    public StyleSheet GetSheetOrThrow();                  // InvalidOperationException with one line for each error
}

public readonly struct StyleParseError
{
    public StyleErrorKind Kind { get; }
    public int Line { get; }                              // from 1
    public int Column { get; }                            // from 1, in code points. A tab counts 1
    public string Message { get; }                        // the problem and the fix
    public string SourceName { get; }
    public override string ToString();                    // "HelloScreen.css(3,10): InvalidValue: <message>"
}

public enum StyleErrorKind : byte { /* 13 members, see the error report */ }
```

The parser runs at load time, and it allocates: the UTF-8 copy of a `string` text, the token list, the names, the messages, and the sheet. The sheet allocates nothing in `Update` (section 14). `Parse` holds no state between calls. Each call makes its own token list and its own variable map, so one parser can parse more than one text, also on more than one thread. `Parse` never throws for the text. It throws `ArgumentNullException` for a `null` source name and for a `null` text.

The BRUTAL analyzer `BRUTALCORE0011` rejects a constant string argument in a call to `Parse(string, string)`: a text literal, a source name literal, and `null`. Thus `parser.Parse(".a { color: #fff; }", "hud.css")` does not compile. Use the byte overload with a `u8` literal (`parser.Parse(".a { color: #fff; }"u8, "hud.css")`) or with the bytes of a file read. The HelloPanel sample calls `File.ReadAllBytes`, then `Parse(bytes, "HelloScreen.css").GetSheetOrThrow()`. It loads the sheet before the window opens, so a bad sheet stops the run with no GPU.

**The tokens and the position.** The text is UTF-8. A BOM at the start adds no column. LF ends a line, and CRLF and a lone CR each count as one line break. The line and the column start at 1. The column counts code points, not bytes, and a tab counts 1. Outside a comment, each byte sequence that is not valid UTF-8 counts 1, as the replacement character of an editor does. Thus a stray continuation byte takes one column, and the bytes `80 C3 A9` give two errors at columns 1 and 2. Outside a comment, the text holds printable ASCII and whitespace only. A non-ASCII byte is valid only in a comment. A non-ASCII byte or a control byte outside a comment gives `UnexpectedCharacter`. A comment `/* ... */` joins the whitespace around it. A comment with no whitespace around it gives no token, so `./**/a` reads as `.a`. A comment with no `*/` gives `UnterminatedComment` at its `/*`. A number has an optional `-`, digits, and an optional `.digits`, or only `.digits`. The format has no exponent.

**The grammar.**

```
sheet        := ( root-block | rule )* End
root-block   := ':root' '{' ( '--name' ':' value-tokens ';' )* '}'
rule         := selector ( ',' selector )* '{' declaration* '}'
selector     := compound ( combinator compound )?                          at most 2 compounds
combinator   := Whitespace | '>'                                           Descendant | Child
compound     := Ident? ( '.' Ident | Hash | ':' Ident )*                    at least one part
declaration  := Ident ':' value ( ';' | before '}' )                        the last ';' is optional
value        := one token or more. Each 'var' '(' '--name' ')' takes the tokens of the variable
```

Whitespace and comments can come between each two tokens outside a compound. A comment alone gives no token, so it does not separate two values of a list (see **A list** in the value forms). The parts of a compound touch, and they can come in each order (`#title.a:checked`). Whitespace around `>`, after `,`, and before `{` is not a combinator.

**The selectors.** A leading identifier is the type (`ElementType.Intern`). `.name` is the class (`ClassName.Intern`). `#name` is the name of the element. `:hover`, `:active`, `:focus`, `:disabled`, and `:checked` each set their bit of `States`, and a repeat adds nothing. One compound gives `Selector.For`. `>` gives `Selector.Child`, and whitespace gives `Selector.Descendant`. Each name is case-sensitive, so `label` does not match a `Label`. A type name that no element declares interns a new id and matches nothing, with no error. The rule matches when an element type with that name exists. A selector list gives one rule for each selector, with the same values, in the order of the source. A rule with no declaration gives no rule. The sheet sorts the rules by the specificity, so a later rule wins at equal specificity.

**The walk bound of `.a .b`.** A descendant chain reads the parent and the grandparent of the element only (section 7). This differs from CSS: in CSS, `.a .b` also matches when `.a` is above the grandparent. The format accepts the CSS text, and the bound applies.

**The selector rejects.** The parser checks each case before it calls a factory, so no `ArgumentException` leaves `Parse`. Each error points at the first byte of the token at fault.

| Text | Kind | Position |
|------|------|----------|
| `.a.b` (a second class), `#a#b` (a second name) | `UnsupportedSelector` | the second `.` or `#` |
| `.a .b .c`, `.a > .b > .c` (a third compound) | `UnsupportedSelector` | the start of the third compound |
| `*`, `[x]`, `.a + .b`, `.a ~ .b`, `::before`, `:not(.a)` | `UnsupportedSelector` | the `*`, `[`, `+`, `~`, the second `:`, or `not` |
| `:first-child` (an unknown state) | `UnknownPseudoState` | the identifier after `:` |
| `> .a`, `.a >`, `.a, {`, `{` with no selector (an empty compound) | `InvalidSelector` | the token where the compound must start |
| `:root .a`, `.a, :root`, `.a:root` (`:root` outside a block of its own) | `InvalidSelector` | the `:` of `:root` |

**The properties.** Each of the 32 members of `StyleProperty` has one name. The names are case-sensitive. The format has no longhand such as `margin-top`.

| Property names | Member type | Value |
|----------------|-------------|-------|
| `flex-direction` | `FlexDirection` | `column`, `column-reverse`, `row`, `row-reverse` |
| `width`, `height`, `flex-basis`, `min-width`, `min-height`, `max-width`, `max-height`, `left`, `top`, `right`, `bottom` | `Length` | a length. A negative value is valid |
| `margin` | `LengthEdges` | 1 to 4 lengths. A negative value is valid |
| `padding` | `LengthEdges` | 1 to 4 values in `px`, `%`, or `0`, each 0 or more. No `auto` |
| `border-width` | `LengthEdges` | 1 to 4 values in `px` or `0`, each 0 or more (Yoga has no percent border) |
| `flex-grow`, `flex-shrink` | `float` | a number, 0 or more |
| `justify-content` | `Justify` | `flex-start`, `center`, `flex-end`, `space-between`, `space-around`, `space-evenly` |
| `align-items`, `align-self`, `align-content` | `Align` | `auto`, `flex-start`, `center`, `flex-end`, `stretch`, `space-between`, `space-around`, `space-evenly` |
| `flex-wrap` | `FlexWrap` | `nowrap`, `wrap`, `wrap-reverse` |
| `display` | `Display` | `flex`, `none` |
| `position` | `PositionType` | `relative`, `absolute` |
| `overflow` | `Overflow` | `visible`, `hidden`, `scroll` |
| `background-color`, `border-color`, `color` | `Rgba8` | a color |
| `border-radius` | `Corners` | 1 to 4 values in `px` or `0`, each 0 or more |
| `opacity` | `float` | a number from 0 to 1 |
| `font` | `FontHandle` | `default` (the id 0), or a key of `CreateInfo.Fonts` |
| `font-size` | `float` | a value in `px` or `0`, 0 or more |
| `transition` | `StyleTransitions` | `<property> <duration> <easing>`, and more of them in a comma list |

**The value forms.**

- **A length** is `<number>px` (`Length.Px`), `<number>%` (`Length.Percent`), `auto` (`Length.Auto`), or a bare `0` (`Length.Px(0)`). Another bare number gives `InvalidValue` with the fix "add px or %". A unit is case-sensitive. An unknown unit (`12em`) gives `InvalidValue`.
- **A number** reads with `CultureInfo.InvariantCulture`, so `0.5` reads the same in each culture. A number that a `float` cannot hold gives `InvalidValue`. `flex-grow`, `flex-shrink`, and `opacity` take a bare number only.
- **A color** is `#` and 3, 6, or 8 hex digits in each case. `#rgb` doubles each digit and sets the alpha to 255. `#rrggbb` sets the alpha to 255. `#rrggbbaa` holds the alpha.
- **A keyword** maps through an explicit table for each enum, with no `Enum.Parse` and no reflection.
- **A list** needs whitespace between two values. A comment alone does not separate two values. A fifth value gives `InvalidValue` at that value.
- **A later declaration** of the same property in one rule wins, as in CSS.

**The edge order.** An edge set and a corner set follow the CSS order. The constructors of Core use another order: `new LengthEdges(left, top, right, bottom)` and `new Corners(topLeft, topRight, bottomRight, bottomLeft)`. The parser maps the CSS order to them in one place. A rule sets all four edges of an edge set at one time. Thus `margin: 12px 0 0` sets the right, bottom, and left edges to `0px`.

| Values | `margin`, `padding`, `border-width` | `border-radius` |
|--------|-------------------------------------|-----------------|
| `a` | each edge `a` | each corner `a` |
| `a b` | top and bottom `a`, left and right `b` | top-left and bottom-right `a`, top-right and bottom-left `b` |
| `a b c` | top `a`, left and right `b`, bottom `c` | top-left `a`, top-right and bottom-left `b`, bottom-right `c` |
| `a b c d` | top `a`, right `b`, bottom `c`, left `d` | top-left `a`, top-right `b`, bottom-right `c`, bottom-left `d` |

**The differences from CSS.** `flex-direction` defaults to `column` (Yoga), not `row`. The format has no `#rgba` (four digits), no `transparent`, no `rgb()`, and no `none` for a maximum size.

**The text members inherit.** `color`, `font`, and `font-size` inherit down the tree in the style pass (section 7). The other members do not inherit. A rule that sets only `color` still lets `font` and `font-size` inherit.

**The fonts.** `font: <name>` reads the font map `StyleSheetParser.CreateInfo.Fonts` with an ordinal compare. `default` gives the id 0, the default font of the renderer. A key `default` in the map wins over the id 0. Another name gives `UnknownFont`. A `FontHandle` id has a meaning for one renderer and one run only. Thus the file names a font, and the game binds the name at load time. The parser copies the map at creation, so a later change of the map does not change the parser.

**Theme variables.** A `:root` block holds variables: `:root { --accent: #3060c0; }`. A value reads a variable with `var(--accent)`, and the tokens of the variable take the place of the use. The parser reads the variables in a first pass. Thus a block can come before or after the rules that read it. When a text defines a name two times, the later value wins. A variable can hold more than one value (`--pad: 4px 8px;`), and a value can mix variables with other values (`padding: var(--gap) 8px;`). A variable name is case-sensitive. The variables of a text live in one `Parse` call, and a second call does not see them. The parser checks the value of a variable at each use, with the form of the property that reads it. A variable that no rule reads gets no value check, as in CSS. Runtime theme variables are out of scope.

- An unknown name gives `UnknownVariable` at the `--name` token of the use.
- A definition with an error gives one error at the definition. A use of that name gives no second error. A token error inside a declaration of a `:root` block replaces each other error of that declaration.
- A `--name` declaration outside `:root` gives `InvalidVariable`. So does a declaration in `:root` that is not a variable, and an empty variable value.
- A variable cannot read a variable, and `var()` takes no fallback. Thus `--a: var(--b);` and `var(--a, 4px)` give `InvalidVariable`.
- An error in a value that comes from a variable points at the `var` token of the use. The message names the variable.
- A `:root` block ends at its first `}`. It gives no rule.

**The transition declaration.** `transition: <property> <duration> <easing>` sets one transition for one member. A comma list sets more than one member: `transition: background-color 0.2s ease-out, opacity 150ms linear;`. The rule stores the set in `StyleRule.Transitions` (section 7).

- The property is `background-color`, `border-color`, `color`, `border-radius`, or `opacity`. Each other name gives `InvalidValue` with a message that names the five.
- The duration is `<number>s` or `<number>ms` (divided by 1000), 0 or more. A bare `0` gives 0 seconds.
- The easing is `linear`, `ease-in`, `ease-out`, or `ease-in-out`.
- The three parts need whitespace between them. A comment alone does not separate two parts.
- All three parts are necessary. The format has no `none`, no `all`, no delay, and no CSS easing `ease`.
- A later item for the same member wins, also in one list. A later `transition` declaration replaces only the slots of its own members. This differs from CSS: in CSS, a later declaration replaces the full list.
- A wrong value changes no slot. A variable can hold a duration or a full list. A rule with only a `transition` declaration gives a rule.

**The error report.** The parser records each error and continues. After an error in a declaration, it skips to the next `;` or `}` of the same block. After an error in a selector, it skips the full block of the rule. An at-rule skips to its `;` or to the end of its block. Each skip counts the nested braces. A missing `}` at the end gives `UnexpectedEnd` at the position after the last byte. `Errors` holds the errors of the tokens, the variable pass, and the rule pass. They come in the order of the source (line, then column). A text with one error or more gives `Sheet == null` and adds no name to the global name tables.

| Kind | Cause |
|------|-------|
| `UnexpectedCharacter` | A byte that starts no token, a control byte, or a non-ASCII byte outside a comment |
| `UnterminatedComment` | A `/*` with no `*/` |
| `UnexpectedToken` | A token in a place where the grammar wants another token |
| `UnexpectedEnd` | The text ends inside a rule, a `:root` block, an at-rule, or a `var(` |
| `InvalidSelector` | An empty compound, or `:root` outside a block of its own |
| `UnsupportedSelector` | `.a.b`, `*`, `[x]`, `+`, `~`, `::before`, `:not`, or a third compound |
| `UnknownPseudoState` | A state other than the five states |
| `UnknownProperty` | A property name that the format does not know |
| `InvalidValue` | A wrong form, a wrong count, an unknown unit, or a number out of range |
| `UnknownFont` | A font name that the font map does not hold |
| `UnknownVariable` | A `var(--name)` with no definition in the same text. A name with a wrong definition gives the error of the definition only. |
| `InvalidVariable` | A wrong variable declaration or a wrong `var()` use |
| `UnsupportedSyntax` | An at-rule (at the `@`), `!important` (at the `!`), or a function other than `var` (at its name) |

Each message names the problem and the fix in one or two short sentences. It quotes the source text at fault. `ToString` of an error gives `<source>(<line>,<column>): <kind>: <message>`. `GetSheetOrThrow` throws `InvalidOperationException` with one such line for each error. Example: `HelloScreen.css(1,16): UnknownProperty: 'colour' is not a property of the format. Check the spelling, for example 'color'.`

### Builder API

```csharp
var panel = new Element().Class("hud")
    .Child(new Label { Name = "score" }.Class("hud-value").Text("0"))
    .Child(new Button { Name = "pause" }.Class("hud-button").Text("Pause").Command("game.pause"));
```

Both paths produce the same tree. A test loads the XML and builds the C# and compares the trees.

---

## 12. World-Space UI

### Flow

1. A `WorldPanel` owns a private `ImDrawList` and an offscreen `ImageEx` render target at the panel's pixel size.
2. Each frame with a dirty panel, the renderer draws the tree into the list, wraps it with `ImDrawDataBuilder`, and calls `RenderDrawData` into the offscreen target.
3. The game samples the target image on its own mesh.
4. For input, the game raycasts the mesh, converts the hit UV to panel pixels, and calls `panel.Pointer(...)`.

### Open issue

The Vulkan backend binds one render pass at construction. Phase 5 tests a second backend instance first. A per-call render pass needs a change in BRUTAL, filed as an issue upstream.

---

## 13. Configuration

- `UiContext.CreateInfo` sets the display scale, the default stylesheet, the font set, and the renderer.
- Mode, panel, and window definitions load from XML files or from C# records.
- Window positions persist to a JSON file next to the executable.

---

## 14. Testing Strategy

### Test types

- **Unit tests** on the layout engine with fixtures from the Yoga test suite, ported by hand.
- **Unit tests** on the XML loader, the stylesheet parser, and the framework diff.
- **Style tests** in `BrutalUi.Core.Tests/Style/`. `StylePropertiesTests` checks the sparse set, the masks, and the throw for a value that is not a member. `StyleSheetTests` checks the builder, the sort, and the rule copy. `StylePassTests` checks the cascade, the compare of each member (a theory over all 32 members gives the kind of bit for each), and the dirty rule table of section 7. `InlineStyleTests` checks the view: a theory over all members checks that each set writes its own bit, that the same value marks nothing, and that a clear gives the default back. `TextInheritanceTests` checks the inheritance, the child marks, and the context defaults. `TypeSelectorTests` and `ElementTypeTests` check the type id and the type match. `MultiSheetTests` checks the merge of more than one sheet, `RemoveSheet`, and the mark of `AddRoot`. `PseudoStateSelectorTests`, `ChainSelectorTests`, and `NameSelectorTests` check each part of a compound, each chain form, the walk bound, the tiers, and the factory checks. `SubtreeMarkTests` checks the subtree mark of a class, state, and name change. `SelectorEqualityTests` checks the equality members and the hash code of both selector types for each part and each combinator. It also checks the CSS text of each form and the fallback text. Its gate `EqualsAllocatesNothing` asserts zero bytes for the compare. `StyleTransitionsTests` checks `CanTransition` for each of the 32 members, and the set, the get, the clear, and the throw. It also checks the empty set of the first constructor and the first `Add`, and the sort with the set. `ARuleWithATransitionStylesTheSame` checks that a transition does not change the computed style. The layout tests and the Yoga fixtures write through `Inline`, and no expected rect changed. `LayoutDifferentialTests` compares 200 random trees with a fresh layout.
- **Transition tests** in `BrutalUi.Core.Tests/Style/`. Five classes hold 70 tests. `TransitionPassTests` (12 tests) checks each of the five members at the midpoint and at the end, the first-style rule, the merge of the slots, the premultiplied color step, and the `Paint` bit of the pass alone through `UpdateTransitions`. `TransitionRetargetTests` (9 tests) checks the turn from the visible value, the keep rule under a subtree mark, the full duration of a turn, the set of the new style (also for the same target), and the inline rules. `TransitionCurveTests` (28 tests) checks each easing at 0, 0.5, and 1, the range and the rise of each curve, the clamp, each bad duration, each bad `deltaTime`, and a 0-byte gate of the curves (`TheCurvesAllocateNothing`). `TransitionInheritanceTests` (12 tests) checks the follow walk of `Color`, the stop at an own color and at an inline color, the own tween of a child with no jump back (Decision R2), the follow of a child with a `Color` transition of 0 s (Decision R3), a child added during the fade, and the `Paint` bit of a follower. `TransitionLifetimeTests` (9 tests) checks the drop on a remove of the element, of an ancestor, and of a root, a move to another parent in the tree (the move ends the tween), a move into another context, `AddRoot`, a fade after a join, the growth rule, and a 0-byte gate of the record reuse (`ThePoolReusesItsRecords`).
- **Allocation tests** that run update and draw on a warm 1000-element tree and assert zero bytes allocated. `ThousandElementAllocationTests` is the gate. It covers the update and the draw, and it holds a style step (see below). The draw test runs full frames (`Update`, then `Draw` into a `RecordingRenderer`) after a warm-up. Each zero-byte gate of `BrutalUi.Core.Tests` calls `AllocationGate.ReleaseAllocationContext()` right before its measured region. A background GC can add the free part of the allocation context to the counter, and the release stops this. `BrutalUi.ImGui.Tests` runs no tests in parallel, so its gates do not call the helper.
- **The style step of the allocation gate.** The 1000-element tree holds a sheet of 16 rules, added with `AddSheet`: type rules, class rules, and rules with a type and a class. Four rules use the forms of selector-matching: a state rule (`.slot:hover`), a child rule (`.slot:hover > Element`), a descendant rule (`.row:hover Element`), and a name rule (`#header`). The frame body adds and removes two classes (`selected` on a grid slot and `muted` on the sidebar). It sets and clears `Hover` on a grid slot and on a stat row, and it changes the name of the header between two constant strings. It then marks `Style` on each element. Each round runs a full restyle of 1000 elements, with a match of the 16 rules, a merge, and the inheritance. The gate asserts zero allocated bytes. `TheStyleStepRestylesTheTree` checks that the step changes the slot fill, the label color, and the inherited values. `TheStyleStepMatchesEachSelectorForm` checks that each of the four new forms changes the value of its target, and that a miss keeps the old value. `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp` is a smaller gate on 50 elements and 4 rules. `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing` is a gate on a chain scene. The class `ThousandElementAllocationTests` has 14 tests (see the queue case below). The 2 tests of transitions add `ThousandElementTree.BuildTransitionSheet`, a second sheet of three rules with transitions only: `.slot` (the fill 0.1 s `EaseOut` and the border color 0.5 s `EaseInOut`), `.muted` (`Color` 0.1 s `Linear`), and `#header` (the border color 0.3 s `EaseIn`). The style step then starts, turns, and ends tweens, and the sidebar color runs the follow walk. `UpdateWithRunningTransitionsAllocatesNothing` asserts zero bytes with running tweens. It also asserts the same `Capacity` of the pool before and after the measure, and at least one running record after it. `TheTransitionStepRunsTweens` checks that the step runs the tweens: 2 records after one style step (one record holds two tweens), each value strictly between its old value and its target, and the targets and the turned border after 9 more steps. Two plants guard the transition code. A planted `GC.KeepAlive(new object())` in loop 1 of `TransitionPool.Advance` gave exit code 2 and failed 3 gates: `UpdateWithRunningTransitionsAllocatesNothing`, `TransitionLifetimeTests.ThePoolReusesItsRecords`, and `ParsedSheetAllocationTests.AParsedSheetAllocatesNothingInUpdate`. The same plant at the start of `TransitionPool.Track` failed the same 3 gates with exit code 2. The gates with no transition sheet pass with both plants, because their measured bodies call neither `Track` nor `Advance`. Three plants show which gates guard the loops. A planted `GC.KeepAlive(new object())` in `Restyle` failed 7 gates in stylesheet-model: both gates of `ThousandElementAllocationTests` and one gate in each of 5 other classes. The same plant in `Selector.Matches` fails 4 Core gates: `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp`, `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing`, and both gates of `ThousandElementAllocationTests`. It fails no ImGui gate, because the ImGui gates measure `Draw` only, the match runs in `Update`, and the ImGui label scene adds no sheet. The same plant in the `Name` setter fails the two gates of `ThousandElementAllocationTests`, because only the 1000-element step changes a name.
- **Stylesheet parser tests** in `BrutalUi.Markup.Tests/Style/`. `StyleTokenizerTests` checks each token kind, the line and the column rules, the BOM, the comments, and the non-ASCII rule. `TextSheetTests` checks a text sheet end to end: the colors, the comments, the optional last `;`, and the result shape. `SelectorFormTests` checks each selector form against the factories, and the round trip of `ToString`. It also checks the selector lists, each reject with its position, and the specificity. `PropertyFormTests` checks each of the 32 names, each keyword, each length form, the invariant culture, and the fonts. `EdgeAndCornerTests` checks the CSS order of each count. `ThemeVariableTests` checks the variables and each variable error. `ErrorReportTests` checks a position for each of the 13 kinds, each range check, and the recovery. It also checks the error text, and the quote of the source text in each message. `ParserRobustnessTests` parses 2000 random texts and each cut of a valid sheet. No text makes the parser throw. `TransitionDeclarationTests` checks the transition declaration and each wrong form. The name tables are global, and xUnit runs the classes in parallel. Thus each test interns its names with its own prefix.
- **A fourth allocation gate.** `ParsedSheetAllocationTests.AParsedSheetAllocatesNothingInUpdate` parses one text with a variable, each selector form, each value family, and a transition list. It runs `Update` on a tree of 50 elements. Each step toggles `Hover` on a panel, toggles one class, and sets a name to one of two constant strings. After 16 warm-up steps, the test releases the allocation context, measures 100 steps, and asserts zero bytes. It also checks that the steps reach the rules. With transitions, each step runs `Update(0.016f)`, so the measured steps also run the transition pass and the turns of the tweens. Before each of the two checks of the rules, a settle step runs `Update(1f)` outside the measure, so each member holds its target. No assert changed. The test project links `AllocationGate.cs` of `BrutalUi.Core.Tests`. The plant of style-parser put `GC.KeepAlive(new object())` at the start of `Selector.Matches(Element, ElementTypeId)`. It failed 5 gates: this gate (0 bytes expected, 208800 bytes measured), `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp`, `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing`, and both gates of `ThousandElementAllocationTests`. `TheHelloPanelSheetGivesTheOldValues` parses `HelloScreen.css` and checks each value against the old constants of the sample. `HelloPanelSheetTests` (10 tests) parses the same file and checks that each widget rule reaches its widget on a small tree: the 31 rules, the fill and the border of each state, the sizes of the four widgets that need a size rule, the order of `Button:disabled` and `Toggle:checked`, and the status label rules.
- **Headless ImGui tests** in `BrutalUi.ImGui.Tests`. A harness creates an ImGui context with no window and no GPU, runs frames, and reads the background draw list as text lines (clip, texture, element count, vertex bounds, colors). The tests need no `VulkanBackend`. `dotnet test` loads the native ImGui library of the pinned `Ahwoo.BRUTAL.ImGui` package.
- **A second allocation gate.** `ImGuiRendererAllocationTests` runs full frames through `ImGuiRenderer` on the 1000-element tree and through the direct calls (rounded rect with a border, square and rounded image, wrapped text, and a measure outside a frame). It asserts zero allocated bytes after warm-up. A test also checks that the measured draw does real work, so a draw that does nothing cannot pass.
- **Element tests** in `BrutalUi.Core.Tests/Elements/`: `LabelTests`, `LabelWrapTests`, `LabelTextTests`, `ImageTests`, and `ContentBoxTests`. `ContentInsetsTests` in `Layout/` checks the insets of the engine. `RecordingRenderer` is the measurer and the renderer. Its wrap rule counts bytes, and each byte is `size * 0.5` pixels wide. It breaks a line at a blank and cuts a word that is wider than the wrap width. The tests check the measure, draw, snap, and dirty rules. They also check the locale refresh, the scale modes, and the content box. A theory checks each width from 8 to 200, and from 20 to 400 with a percent padding. At each width, the text at the draw wrap width has the height of the measured box.
- **ImGui label tests.** `ImGuiLabelTests` and `ImGuiImageElementTests` in `BrutalUi.ImGui.Tests` run a `Label` and an `Image` through `ImGuiRenderer` on the headless context. They use the real ImGui measure and read the vertices of the background list. A sweep checks each width from 40 to 200, with no padding and with a padding of 5 %. At each width, the measure and the draw give the same line count. Each glyph lies inside the label box grown by 1 pixel.
- **A third allocation gate.** `ElementAllocationTests` runs 100 frames on a tree of 50 labels with wrapped text, 50 images with a width of 32, and one label with an `LString`. Each frame changes 10 labels and 10 images. On a label, it sets the text and the inline color, size, and font. On an image, it sets the tint, the UV rect, the texture, and the scale mode. A new UV span runs the image measure. The frame also sets one of two `LString` instances on the last label. Then it runs `Update` and `Draw` into a `RecordingRenderer`. The tests assert zero allocated bytes after warm-up for the span path, the string path, and `RefreshLocalizedText`. Before each measured refresh, the test changes the locale string, so each refresh takes the slow path. `TheMeasuredBodyDoesRealWork` checks that each frame measures and draws the new values. `ImGuiRendererAllocationTests.LabelUpdateAndDrawAllocateNothingAfterWarmUp` runs the same scene through `ImGuiRenderer`. The harness has one font, so that test does not change the font. A planted allocation in the label measure makes three of these tests fail. A planted allocation in the image measure, in the `Uv`, `Color`, or `Size` setter, in `SetText(LString)`, or in the slow path of the refresh makes one test or more fail.
- **Event tests** in `BrutalUi.Core.Tests/Input/`. `EventWalkTests` checks the capture, target, and bubble order and the stop rule. `EventKindTests` checks that each of the 7 kinds reaches only its own slots and that each payload survives the queue. `EventQueueTests` checks the ring, the drop of the oldest entry, the capacity rules, and the exceptions of a wrong type. `EventWalkEdgeTests` checks a deep chain, a handler that throws, a nested drain, and a target outside the tree. `EventRecorder` is the shared test helper that records each call.
- **A fifth allocation gate.** `EventAllocationTests` builds a chain of 40 levels under the root, with all 14 slots registered, and a multicast slot at level 10. Each step pushes 11 events of the 4 kinds into a ring of 9, including a stopped walk, a target outside the tree, and a null target. It drains the ring and asserts zero allocated bytes over 100 steps (1100 pushes, 100 drains, 700 walks). `APushToAFullQueueAllocatesNothing` asserts zero bytes for 1000 pushes into a full ring. `TheMeasuredBodyDoesRealWork` checks that each step calls 438 handlers and that the ring drops entries. A planted `GC.KeepAlive(new object());` as the first line of `EventWalk.Dispatch` gave exit code 2 and failed one gate: `PushAndDrainAllocateNothingAfterWarmUp`. After the restore, the run gave exit code 0.
- **Pointer tests** in `BrutalUi.Core.Tests/Input/`. `PointerTree` is the shared small scene. `HitTestTests` (6 tests) checks the order of the walk, the half-open edges, and the NaN point. `HitTestRuleTests` (11 tests) checks the clip of an overflow, the nested clip, `Display.None`, the opacity product, and `IsPickable`. `PointerHoverTests` (5 tests) checks that `Hover` moves to the hit element only and restyles it. `PointerPressTests` (8 tests) checks `Active` with the button mask and the bubble from the hit element. It also checks a release over no element and at a NaN point. `ScrollRoutingTests` (4 tests) checks that a scroll reaches the hit element. `PointerStateEdgeTests` (8 tests) checks the stale state rule, the target from the caller, and the state after a removal. It also checks that the stale rule keeps the button mask. `WindowCallbackTests` (4 tests) checks the two window callbacks. `BrutalUi.Glfw.Tests` has 12 tests: `GlfwUiInputTests` (10) and `GlfwUiInputAllocationTests` (2). Most tests run through an internal test seam of the adapter, with no window. Two tests create a hidden window and skip when the machine has no display. They run under `xvfb-run -a`. `dotnet test` loads the native GLFW library of the pinned `Ahwoo.BRUTAL.Glfw` package.
- **A sixth and a seventh allocation gate.** `PointerAllocationTests` (3 tests) builds the 1000-element tree (`ThousandElementTree`). Each step pushes 6 pointer and scroll events and drains them, with 5 hit tests and 5 walks. `TheHitTestAllocatesNothingOnAWarmTree` and `PointerRoutingAllocatesNothingAfterWarmUp` assert zero allocated bytes. `TheMeasuredBodyDoesRealWork` checks that the hit tests find elements, because `HitRound` stores a hit count that the compiler cannot drop. `GlfwUiInputAllocationTests.TheAdapterHandlersAllocateNothing` pushes 8 events of each kind through the adapter into a context with `RecordingRenderer` and a ring of 8, and it asserts zero bytes. A second test, `TheRealHandlersAllocateNothing`, creates a hidden window, reads the handler delegates of `OnCursorPos`, `OnScroll`, and `OnMouseButton`, and calls each one 1000 times. It runs the real `GetKey`, `Glfw.Time`, and `Log.Trace` path, and it measures zero bytes. It skips when the machine has no display. The gate leaves out the drop and the content scale, because a drop allocates in the binding. A planted `GC.KeepAlive(new object());` as the first line of `HitWalk.Find` gave exit code 2 and failed two gates: `TheHitTestAllocatesNothingOnAWarmTree` and `PointerRoutingAllocatesNothingAfterWarmUp`. After the restore, the file hash was equal and the run gave exit code 0 with 0 failed.
- **Focus tests** in `BrutalUi.Core.Tests/Input/`. `FocusOwnerTests` (8 tests) checks `SetFocus`, `ClearFocus`, the `Focus` bit, and the route of a key and a char to the owner. `ClickFocusTests` (8 tests) checks the click focus, the nearest focusable ancestor, the clear on a miss, the caller target, and the events that keep the focus. `TabNavigationTests` (12 tests) checks Tab and Shift+Tab in tree order, the wrap, the hidden subtrees, the stop by a handler, the modifier, repeat, and key up rules, the caller target, and a deep tree. `FocusStateEdgeTests` (8 tests) checks `Disabled`, the stale focus rule, a removed owner, and a key from a handler. `BrutalUi.Glfw.Tests` now has 20 tests. `GlfwUiInputKeyTests` (8 tests) checks the key and char conversion, the scalar value check, `KeyboardEnabled`, `Dispose`, and Tab through the adapter. `GlfwUiInputTests` also counts the eight window events on a real window.
- **An eighth allocation gate.** `FocusAllocationTests` (3 tests) builds the 1000-element tree. A step holds two drains. The first drain starts with no owner, and the second starts with an owner, so `DropStaleFocus` and `FocusWalk.IsShown` run under the counter. A step gives 3 key downs, 1 key up, and 1 char, a Tab search, a click focus, and 7 walks, and a press outside the root gets no walk. `KeyboardRoutingAllocatesNothingAfterWarmUp` and `TheTabSearchAllocatesNothingOnAWarmTree` assert zero allocated bytes. `TheMeasuredBodyDoesRealWork` checks that the handlers run and that the focus moves. The extended `GlfwUiInputAllocationTests` stays the seventh gate. Its adapter test pushes key and char events, and the real-handler test calls `HandleKey` and `HandleChar` 1000 times each under `xvfb-run -a` with zero bytes. Five runs of the new class gave 3 tests with 0 failed. Two plants guard the focus code. A planted `GC.KeepAlive(new object());` as the first line of `FocusWalk.Find` gave exit code 2 and failed `KeyboardRoutingAllocatesNothingAfterWarmUp` (14248 bytes) and `TheTabSearchAllocatesNothingOnAWarmTree` (12000 bytes). The same line as the first line of `SetFocusOwner` gave exit code 2 and failed the same two gates and `PointerAllocationTests.PointerRoutingAllocatesNothingAfterWarmUp`, because each press runs the click focus. After each restore, `cmp` found no difference.
- **Widget tests** in `BrutalUi.Core.Tests/Elements/` and `Input/`. `ButtonClickTests` (8 tests) checks the click rule: a left press and release, a release over another element, a press on another element, the other buttons, the stop of the walk, and the sender. `ButtonKeyTests` (9 tests) checks Enter and Space, the repeat, the modifiers, the keypad Enter key, and a key from a child. `ButtonTextTests` (9 tests) checks the create-info, the `SetText` overloads, the locale refresh, and the draw at the top-left corner of the content box. `ToggleTests` (10 tests) checks the flip by click and by Space, the `:checked` rule, the code set that calls nothing, and the start value. `WidgetDisabledTests` (7 tests) checks that a disabled button or toggle gives no click and no flip and does not stop the walk. `ReleaseOrderTests` (4 tests) in `Input/` checks that a `PointerUp` handler reads `Active` on the pressed element and that the states after the drain stay the same. `WidgetTree` is the shared scene: a root, a button, a plain element, and a toggle.
- **A ninth allocation gate.** `WidgetAllocationTests` (4 tests) builds a column root with 20 buttons that have a text, 20 toggles, and a sheet with `Button:hover`, `Button:active`, `Toggle:checked`, and `:disabled`. `UpdateAndDrawAllocateNothingWithWidgets` runs 100 frames. `TheWidgetInputPathAllocatesNothing` runs 100 steps. A step pushes a press and a release over a button, an Enter key to the focused button, a press and a release over a toggle, and a Space key to the focused toggle. It then runs `Update` and `Draw`. `SetTextOnAButtonAllocatesNothing` sets two texts of the same length in turn. `TheMeasuredBodyDoesRealWork` checks 2 clicks, 2 flips, and 20 text draws after one step. A planted `GC.KeepAlive(new object());` as the first line of `Button.HandlePointerUp` gave exit code 2 and failed one gate: `TheWidgetInputPathAllocatesNothing`. After the restore, `cmp` found no difference.
- **Slider tests** in `BrutalUi.Core.Tests/Elements/` and `Input/`. `ChordClickTests` (8 tests) checks the chord rule: a chord gives no click, and a release with a caller target gives no click. `SliderPressTests` (10 tests) checks the create-info, the map of the thumb center, the press, and `ValueChanged`. `SliderDragTests` (9 tests) checks the drag rule: a move while the slider holds the left press, a move with no press, and the press button rule. `DragCaptureTests` (9 tests) in `Input/` checks the capture of a move outside the rect, the end of the capture at the release, and that `Hover` and `PointerHit` follow the hit. `SliderStepTests` (9 tests) checks the value rule, the grid, and `SetRange`. `SliderKeyTests` (9 tests) checks the arrow keys, the modifiers, and the repeat. `SliderDrawTests` (6 tests) checks the track, the thumb, and the style rules. `SliderDisabledTests` (6 tests) checks that a disabled slider gives no press, no drag, no key, and no capture. `SliderTree` is the shared scene: a root, a plain element, a slider, and a second plain element in a row.
- **A tenth allocation gate.** `SliderAllocationTests` (3 tests) builds a column root with 20 sliders of 200 x 20 (the range 0 to 100, the step 0), the last one in `Disabled`, and a sheet with `Slider`, `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled`. `UpdateAndDrawAllocateNothingWithSliders` runs 100 frames. `TheSliderInputPathAllocatesNothing` runs 100 steps. A step pushes a press at 25, a move to 60, a move past the right end of the root, a release, a Left key, and a Right key to the first slider. It then runs `Update` and `Draw`. The move past the root walks to the slider through the capture, so the gate measures the capture branch of the pointer step. `TheMeasuredBodyDoesRealWork` checks 5 `ValueChanged` calls, the value 100, and 20 thumb rects after one step. A planted `GC.KeepAlive(new object());` as the first line of `Slider.HandlePointerMove` gave exit code 2 and failed one gate: `TheSliderInputPathAllocatesNothing`. After the restore, `cmp` found no difference.
- **ScrollView tests** in `BrutalUi.Core.Tests/Elements/`. `ScrollViewScrollTests` (10 tests) checks the scroll rule, the sign, the speed, the stop rule, and `ScrollChanged`. `ScrollViewHitTests` (6 tests) checks that the hit walk follows the offset and that the clip does not move. `ScrollViewSliderTests` (6 tests) checks a slider in a scrolled view: the press, the drag, the capture, and the wheel map through the offset. `ScrollViewDragTests` (9 tests) checks the drag on the view itself, the press button rule, and the capture outside the view. `ScrollViewClampTests` (10 tests) checks the content size, the clamp at use, and the NaN rule. `ScrollViewNestedTests` (6 tests) checks that the offsets add up, that the clips intersect, and that an inner view at its end passes the wheel. `ScrollViewDisabledTests` (5 tests) checks that a disabled view takes no scroll, no drag, and no capture. `ScrollViewTree` is the shared scene: a root, a view with five children (one is a slider), and a plain element.
- **An eleventh allocation gate.** `ScrollViewAllocationTests` (3 tests) builds a column root with 10 scroll views of 200 x 100, each with 20 rows of 300 x 40 (the odd rows not pickable), a slider in the first view, the last view in `Disabled`, and a sheet with `ScrollView`, `ScrollView:hover`, `ScrollView:active`, and `:disabled`. `UpdateAndDrawAllocateNothingWithScrollViews` runs 100 frames. `TheScrollViewInputPathAllocatesNothing` runs 100 steps. A step sets the offset of the first view to (0, 0) and pushes a scroll down, a scroll up, a press on a row that is not pickable, a move, a move outside the view, a release, a press and a move on the slider at the offset (0, 40), and a release. It then runs `Update` and `Draw`. The move outside the view walks to the view through the capture. `TheMeasuredBodyDoesRealWork` checks the `ScrollChanged` calls, the `ValueChanged` calls, an offset above 0 on the first view, and 10 `PushClip` calls in the frame. A planted `GC.KeepAlive(new object());` as the first line of `ScrollView.HandleScroll` gave exit code 2 and failed one gate: `TheScrollViewInputPathAllocatesNothing`. After the restore, `cmp` found no difference.
- **TextField tests** in `BrutalUi.Core.Tests/Elements/`. `TextFieldTypeTests` (10 tests) checks the start text, a multi-byte code point, the capacity drop, a code point that does not fit, a control char, the stop, and the caller target. `TextFieldCaretTests` (10 tests) checks the insert at the caret, Home and End, the code point step, the keys that walk on, the modifiers, the repeat, and the caret draw. `TextFieldDeleteTests` (8 tests) checks Backspace and Delete on code points of 1 to 4 bytes. `TextFieldSubmitTests` (6 tests) checks Enter, the stop, the repeat, and the keypad Enter key. `TextFieldTextTests` (10 tests) checks `SetText`, `GetText`, the cut rule, and the `LString` overload. `TextFieldScrollTests` (9 tests) checks the scroll rule, a resize, and the padding and the border. `TextFieldBlinkTests` (6 tests) checks the cycle, the restarts, a bad time step, and a context with no measurer. `TextFieldSelectionTests` (10 tests) checks `Shift` with the caret keys, the collapse, the replace, the flip over the anchor, and the draw order. `TextFieldPointerTests` (10 tests) checks the press, the nearest boundary, the tie, the drag, `Shift` with a press, and a scrolled field. `TextFieldDisabledTests` (5 tests) checks that a disabled field takes no char, no key, and no press, that it loses the focus and the caret, and that a set from code works. `TextFieldKeyCodeTests` (1 test) in `BrutalUi.Glfw.Tests` checks that the key codes of Core equal the `GlfwKey` values. `TextFieldTree` is the shared scene: a root, a field of 81 x 16 with the font size 16, and a plain focusable element.
- **A twelfth allocation gate.** `TextFieldAllocationTests` (3 tests) builds a column root with 20 text fields of 81 x 16 (the capacity 32 and the start text `field`), the last one in `Disabled`, and a sheet with `TextField`, `TextField:focus`, and `:disabled`. The first field holds the focus. `UpdateAndDrawAllocateNothingWithTextFields` runs 100 frames, so the caret pass and the blink run. `TheTextFieldInputPathAllocatesNothing` runs 100 steps. A step sets the text of the first field, pushes three chars, Left, Shift+Left, a char that replaces the selection, Backspace, Delete, a press, a drag move, and a release, and runs one `Update` that maps the press. It then pushes Home, Shift+End, Delete, End, Enter, and a char past the capacity to a full second field, and it runs `GetText`, `Update`, and `Draw`. `TheMeasuredBodyDoesRealWork` checks 7 `TextChanged` calls, 1 `Submitted` call, the empty text, the dropped char, 19 text calls, and the caret rect of the blink phase. A planted `GC.KeepAlive(new object());` as the first line of `TextField.HandleChar` gave exit code 2 and failed one gate: `TheTextFieldInputPathAllocatesNothing`. After the restore, `cmp` found no difference.
- **Routing gate tests** in `BrutalUi.Core.Tests/Input/`. `RoutingGateTests` (8 tests) checks that `Update` runs the input pass before the style pass, that the gate is `false` with no input, and that `PointerOverUi` follows the last hit (also the cursor leave and a targeted move). It checks that a non-pickable root passes the pointer to the game, that `KeyboardFocusInUi` follows the focus owner, and that the stale rules clear the gate at the next `Update`. It checks that an `Update` from a handler throws `InvalidOperationException` and leaves the context valid, and that an event that a handler pushes waits for the next `Update`. One test also checks that a handler that adds or removes a root during the drain gets the layout of the new roots in the same `Update`.
- **The queue case of the allocation gate.** `ThousandElementAllocationTests` (14 tests) adds the input pass to the measure. `UpdateDrainsAFullQueueWithNoAllocation` pushes a block of 16 events with a null target 16 times in each step, so the ring of 256 entries is full and drops nothing. The block holds moves, a scroll, a press, a release, key down and key up, a char, two Tab keys, a Shift+Tab key, and a move at (NaN, NaN). Each of the 120 grid slots accepts focus and holds 7 counter handlers. Each step then makes the rotating change, calls `Update`, which drains the full ring, and reads `ui.RoutingGate`. The gate asserts zero allocated bytes, the same `DroppedEventCount` before and after, and `QueuedEventCount` 0. `TheQueueStepDoesRealWork` checks 32 moves, 16 scrolls, 16 presses, 16 releases, 48 key downs, 16 key ups, and 16 chars on the slots, the focus owner at slot 0, and both gate values `true`. Three plants guard the code. A planted `GC.KeepAlive(new object());` as the first line of the `RoutingGate` getter gave exit code 2 and failed one gate: `UpdateDrainsAFullQueueWithNoAllocation`. The same line as the first line of `DrainEvents` gave exit code 2 and failed 16 allocation gates (14 in Core, 1 in ImGui, 1 in Markup), because each `Update` calls the drain. A third plant removed the `PushSmokeInput` call of HelloPanel and gave exit code 1 with the line `HelloPanel: the synthetic input did not reach the UI.` After each restore, `cmp` found no difference.
- **Benchmarks** with BenchmarkDotNet for layout and draw on that tree. `TreeLayoutBenchmark` measures the full layout, a dirty subtree, and a clean update on the 1000-element tree. The settings panel is hidden, so the full layout pass lays out 780 elements and sets the 220 elements of the panel to zero. `PercentChainBenchmark` measures a leaf mark under a chain of percent sizes. `TreeDrawBenchmark` measures `Draw` on the 1000-element tree into `RecordingRenderer`. The time holds the walk and the cost of the recording.
- **Facts report.** The `--facts` mode of the benchmark project prints four `facts:` lines: the size of the tree, the callback calls of the two percent chains, and the number of levels that pass on a 1 MB stack.
- **A thirteenth allocation gate.** `ThousandWidgetAllocationTests` (4 tests) runs the 1000-element tree with each widget type (`ThousandWidgetTree`). The build swaps the last 3 slots of the footer (9 elements) for 9 widget elements: the button `Play`, the disabled button `Off`, a toggle of 20 x 20, a slider of 120 x 20, a text field of 120 x 16, and a scroll view of 80 x 48 with 3 rows. The tree keeps 1000 elements, and its context has a measurer. The tree adds a widget sheet of 7 rules after the base sheet: `Button:hover`, `Button:active`, `Button:disabled`, `Toggle:checked`, `Slider:focus`, `ScrollView:hover`, and `TextField:focus`. Each rule sets only a fill or a border color, so a state change changes no rect. `UpdateAndDrawAllocateNothingWithEachWidget` gives the focus to the field, marks `Style` on each element, and runs 100 frames, so the style pass restyles 1000 elements against both sheets, and the caret pass and the blink run. `TheWidgetInputPathAllocatesNothing` runs 100 steps. A step sets the view offset to (0, 0) and pushes 35 events in 7 groups. The step pushes the groups in the order 1, 2, 3, 4, 6, 7, 5. Group 1 is a click, a scroll, a char, and an Enter at the button. Group 2 is the same 4 events at the disabled button. Group 3 is the same at the toggle with a Space in place of the Enter. Group 4 is a drag of the slider from 25 % to 75 % with a scroll, a char, and a Right. Group 6 is a scroll at the view with a drag of 20 pixels. Group 7 is a Tab and an Enter. The Tab finds no focus owner, so it moves the focus to the button `Play`, and the Enter clicks it. Group 5 comes last. It is a click at the field with a scroll, a char, a Backspace, and an Enter, and then a second press and release inside the text. The step ends with the focus on the field and with a pending press x. The step then runs `Update` and `Draw`. The caret pass maps the second press with `IndexAtX`, so the input gate measures the press map, and the frame gate measures the blink. `TheMeasuredBodyDoesRealWork` checks 3 clicks, 0 clicks of the disabled button, 3 footer presses, 2 flips, 3 value changes, 2 scroll changes, 2 text changes, 1 submit, and a `PushClip` for the view and for the field. The 3 footer presses are the presses at the button, the disabled button, and the toggle, because those presses walk on to the footer. The slider, the view, and the field stop their presses. The test also checks the field as the focus owner and the caret at 2 after the step. The caret starts at 5, the end of the text, so only the press map can move it to 2. With the step order above (group 5 last), a planted `GC.KeepAlive(new object());` as the first line of `TextField.AdvanceCaret` gives exit code 2 and fails 4 gates: `ThousandWidgetAllocationTests.UpdateAndDrawAllocateNothingWithEachWidget` (10576 bytes), `ThousandWidgetAllocationTests.TheWidgetInputPathAllocatesNothing` (2400 bytes), `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing` (4800 bytes), and `TextFieldAllocationTests.UpdateAndDrawAllocateNothingWithTextFields` (2400 bytes). The same line as the first line of `TextField.IndexAtX` now fails `ThousandWidgetAllocationTests.TheWidgetInputPathAllocatesNothing` and `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`. After each restore, `cmp` found no difference.
- **Sample projects** as the integration test, built in CI. The HelloPanel smoke run (`--frames 120`) prints 5 lines: `HelloPanel: 120 frames rendered.`, `HelloPanel: last frame: draw commands 7, vertices 1100.`, `HelloPanel: bytes after warm-up: Update and Draw 0, frame <F>, in 117 frames.`, `HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.`, and `HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118, edits 182, submits 121.` The vertex count was 128 before the input demo. The gate label adds 160 vertices, and the widget section adds 812 more (288 to 1100 in total), with 5 more draw commands, because the view and the field clip their content. The last frame is odd, so the text of the field is empty in it. In each frame, the run pushes a synthetic input into the ring, so the gate counts show the input pass at work. The order of the push is: the button click, the toggle click, the slider click (25 % in an even frame and 75 % in an odd frame), the scroll at the center of the view ((0, -1) in an even frame and (0, 1) in an odd frame), the Tab down and up, the char `a` (even frame) or the Backspace (odd frame) and the Enter with the field as the caller target, the press and the release at the center of the field with no target, and the pointer move last. No focus change comes after the field press, so the field holds the focus at the caret pass. The presses also give the focus, so the keyboard count of line 4 does not prove the Tab. The Tab moves the focus from the slider to the field, and a Tab after the field press would move the focus to the panel. `FocusAllocationTests` and the queue case of `ThousandElementAllocationTests` guard the Tab. The first measured frame (frame 3) adds a burst after the Tab. The burst holds 32 chars of `W`, one Enter, and 32 Backspace keys, each with the field as the caller target. The field drops the 32nd char, so the text has 32 bytes. The text label then shows 38 bytes and the submit label 43 bytes. These are the longest texts of the two text labels, and both are longer than the start buffer of a label. The field is empty again at the end of the frame. The move comes last, so it decides the gate of line 4. The move is over the panel in the even frames and off the panel in the odd frames. The run exits with code 1 when it measured 2 frames or more and a gate count is 0. It then writes `HelloPanel: the synthetic input did not reach the UI.` to standard error. It also exits with code 1 when each measured frame reads the pointer over the UI, because the odd frames move off the panel. It then writes `HelloPanel: the pointer read over the UI in each measured frame. The off-panel frames did not reach the game.` to standard error. It also exits with code 1 when one of the 6 widget counts of line 5 is 0 in a run of 2 measured frames or more. It also exits with code 1 when the field never holds the focus after `Update` in a measured frame (the caret check). Line 5 does not show the caret count. In both cases it writes `HelloPanel: the synthetic input did not reach each widget.` to standard error. The run is also a gate. It exits with code 1 and writes `HelloPanel: Update and Draw allocated <B> bytes after warm-up. The limit is 0 bytes.` to standard error when the UI region (the smoke input, `ViewportSize`, `Update`, `Draw`, the gate read, and the label update) allocates after 3 warm-up frames. The frame number is a report only. In a measured frame in which the thread allocated since the last release, the sample releases the allocation context before the UI region and outside it, for the same cause as the test gates. The sample loads `HelloScreen.css` before the window opens. A bad sheet gives exit code 1 and the error lines on standard error, with no GPU. Five plants guard the widget script. Plant A puts `GC.KeepAlive(new object());` as the first line of `HelloWidgets.OnClicked`. The smoke run then exits with code 1 and writes `HelloPanel: Update and Draw allocated 2808 bytes after warm-up. The limit is 0 bytes.` (117 measured frames times 24 bytes). Plant C does the same in `HelloWidgets.OnTextChanged` and gives the same 2808 bytes. Plant B removes the click of the button from `PushSmokeInput`. The run then exits with code 1 and writes `HelloPanel: the synthetic input did not reach each widget.`, and line 5 shows `clicks 0`. Plant M03 removes the `WriteText` call for the text label from the constructor of `HelloWidgets`, so the label does not hold the longest text before the measure. The run exits with code 1 and writes `HelloPanel: Update and Draw allocated 88 bytes after warm-up. The limit is 0 bytes.` Plant M09 moves the Tab after the pointer move (the order of the plan). The run exits with code 1 and writes `HelloPanel: the synthetic input did not reach each widget.` because the field never holds the focus after `Update`. Line 2 then reads `draw commands 5, vertices 1096`, and lines 3 to 5 do not change. After each restore, `cmp` found no difference. `scripts/test-linux.sh` reads the exit code only, so the script has no change.

Run the benchmarks and the facts report from the repository root:

```
dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'
dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts
```

The tests assert no time. The recorded times come from one run on the machine of the author. `CHANGELOG.md` holds them.

### What is not tested

Pixel output. The walk tests assert the call list of `RecordingRenderer`, not the image.

---

## 15. Development Principles

1. **Zero allocation after warm-up.** The allocation test runs in CI. A failure blocks the merge.
2. **Core has no ImGui reference.** The compiler enforces it through the project references.
3. **Markup equals code.** Every element feature works from XML and from C#. A feature that works in one only is not done.
4. **Match BRUTAL style.** File-scoped namespaces, `required` create-info structs, `Brutal.Numerics` types, UTF-8 text, XML doc comments on public members.
5. **Pin BRUTAL versions.** A BRUTAL update is its own change with its own test run.

---

## 16. Phase Boundaries

### Phase 1: skeleton and tree (2 weeks)

Solution, package pins, `Element`, flexbox, `IUiRenderer`, `ImGuiRenderer`, `Label`, `Image`, the HelloPanel sample with a static screen. The feature hello-panel-sample closes this phase.

### Phase 2: style and input (3 weeks)

Stylesheet model and parser, pseudo-states, transitions, pointer and keyboard routing, focus, `Button`, `Toggle`, `Slider`, `ScrollView`, `TextField`. The feature widget-sample closes this phase: the HelloPanel sample shows each widget, and the smoke run drives each one.

### Phase 3: framework (2 weeks)

Stores, bindings, panels, modes, slots, commands, event bus, services, the action map, the XML element loader.

### Phase 4: windows and lists (2 weeks)

`WindowChrome`, `WindowManager`, persistence, virtualized `ListView`, `DataList`, the debugger window.

### Phase 5: world space (2 weeks)

`WorldPanel`, offscreen render target, the WorldSpace sample, the upstream issue if the render pass needs a change.

---

*This is a living document. Update it as technical decisions land, patterns emerge, and the architecture evolves.*
