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
│   ├── BrutalUi.Framework/     # stores, panels, modes, windows, commands, events
│   ├── BrutalUi.Markup/        # XML loader, stylesheet parser
│   └── BrutalUi.Debugger/      # state, composition, and event inspector window
├── samples/
│   ├── HelloPanel/             # Phase 1: a static screen. Phase 3: one store, one mode
│   └── WorldSpace/             # panel on a quad
├── tests/
│   ├── BrutalUi.Core.Tests/
│   ├── BrutalUi.ImGui.Tests/
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

**`src/BrutalUi.Framework/`** holds the mosaic-ui layer. It references Core only.

**`src/BrutalUi.Markup/`** holds the XML element loader and the stylesheet parser. It references Core only.

**`tests/`** holds xUnit tests for each package and a BenchmarkDotNet project with the allocation guard. `BrutalUi.ImGui.Tests` runs the ImGui renderer headless: it creates an ImGui context with no window and no GPU, and it reads the background draw list.

---

## 4. Architecture Overview

### Overview

```
 Game loop
   │
   ├─ GlfwWindow events ──► UiInput ──► Element tree (focus, hover, capture)
   │
   ├─ UiContext.Update()
   │     ├─ Framework: mode diff, store notifications, bindings
   │     ├─ Style pass: cascade the sheet rules and the inline set for dirty elements
   │     └─ Layout pass: flexbox on dirty subtrees
   │
   ├─ UiContext.Draw(IUiRenderer)
   │     └─ PaintWalk (frame stack) ──► IUiRenderer ──► ImGuiRenderer ──► ImDrawList
   │
   └─ VulkanBackend / OpenGlBackend render ImDrawData
```

### Frame order

1. The game calls `GlfwBackend.NewFrame()` and `ImGui.NewFrame()` as it does today.
2. The game sets `ViewportSize` from `io.DisplaySize`. Then it calls `UiContext.Update(deltaTime)`. The framework applies pending mode changes and store notifications. The style pass runs first, then the layout pass. Both run on dirty subtrees.

**The style pass at the seam.** `UiContext.Update` calls the internal `UiContext.UpdateStyles` first, before the layout pass. The pass restyles each element with the `Style` bit or the `DescendantStyle` bit, and it marks `Layout` or `Paint` when the result changes. The layout pass then reads the new `ComputedStyle`. The pass has its own recursion, because it marks the children of an element during the visit, and the visitor rules of `DirtyWalk` forbid a mark of the walked kind. The pass clears the two style bits of an element after its subtree. It never clears `Layout` or `Paint`.
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

**The measurer of the context.** A `Label` measures its text with `UiContext.Measurer` during `Update`. Create the context with `new UiContext(new UiContext.CreateInfo { Measurer = renderer })` for a tree with a label. A context from `UiContext()` has no measurer. Its `Update` throws `InvalidOperationException` when a label needs a measure. An `Image` needs no measurer. The measurer and the renderer of `Draw` must use the same font ids. Pass the renderer that draws, or a renderer that shares its font service.

**Label and image.** Both classes are leaves, and both have the `sealed` modifier. Do not add children to them. The engine calls a measure callback for an element with no children only. Each constructor sets a cached static measure callback one time. Each setter and each `SetText` overload compares the old value first. A set to the same value marks nothing, so the game calls no `MarkDirty` for these properties. The `Image.Texture` setter compares the id and the size. A new size, or a change to or from the none handle, marks `Layout` and `Paint`. A new id with the same size marks `Paint` only.

**The text of a label.** `SetText(ReadOnlySpan<byte>)` compares the bytes. `SetText(string)` and `SetText(LString)` compare the string (ordinal) with the last string. After a text from a span, they compare the UTF-8 bytes with no allocation. The BRUTAL analyzer `BRUTALCORE0011` rejects `SetText("Hello")` with a literal, because the span overload exists. Write `SetText("Hello"u8)`. A bare `SetText(null)` is ambiguous. Cast it to `string?` or `LString?`. An `LString` with replacement variables shows its template text. To show a formatted text, format it and call the span overload.

**The locale refresh.** `Brutal.Localization` has no change event. Call `UiContext.RefreshLocalizedText()` after `SetLocalizedStringProvider`, `LString.Localize`, or `LString.RemoveLocalization`. Do not call it in each frame, and `Update` does not call it. The method walks each root in tree order. For each label with an `LString`, it reads `LString.Localized`. The same reference returns at once. A new reference with new content (ordinal) encodes the text and marks `Layout` and `Paint`. A new reference with the same content marks nothing. The method needs no measurer, and it allocates nothing when each buffer is large enough. It knows the `Label` type only.

Built-in element types: `Element`, `Label`, `Image`, `Button`, `Toggle`, `Slider`, `TextField`, `ScrollView`, `ListView`, `WindowChrome`.

### Events

A struct event with a phase (capture, target, bubble) walks the tree. Handlers register on an element by event type. No closures in the hot path. A handler is a static method plus a target object, or a command id.

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
public readonly struct CompoundSelector         // one compound, as Label.title:hover is in CSS
{
    public ElementTypeId Type { get; init; }    // none, or the type id that the element must have
    public ClassId Class { get; init; }         // none, or the one class that the element must hold
    public PseudoStates States { get; init; }   // none, or each state that the element must hold
    public string? Name { get; init; }          // null, or the name that the element must have (ordinal)
}

public enum SelectorCombinator : byte { None = 0, Child = 1, Descendant = 2 }

public readonly struct Selector
{
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

public readonly struct StyleRule
{
    public readonly Selector Selector;
    public readonly StyleProperties Properties; // a copy of the set at creation
    public readonly int Specificity;            // the value of the selector at creation
    public StyleRule(in Selector selector, in StyleProperties properties);
}

public sealed class StyleSheet                  // immutable
{
    public ReadOnlySpan<StyleRule> Rules { get; }   // sorted by specificity, then by the order of add

    public sealed class Builder
    {
        public Builder Add(in Selector selector, in StyleProperties properties);  // default(Selector) throws
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

`ComputedStyle` holds 32 members. They are the 25 layout inputs (section 6), four paint members, and three text members. The paint members are `BackgroundColor` (`Rgba8`), `BorderColor` (`Rgba8`), `CornerRadius` (`Corners`), and `Opacity` (`float`, default 1). The text members are `Color` (`Rgba8`), `Font` (`FontHandle`), and `FontSize` (`float`). `Label` reads the three text members from its style (section 8).

**The cascade order.** The style pass builds the style of a dirty element from `default(ComputedStyle)` in these steps:

1. The pass applies each matching rule in the order of the cascade. The order is the specificity (see the tiers below), then the order of `AddSheet`, then the rule order in the sheet. A later rule wins.
2. The pass applies the inline set last. An inline member always wins over a rule.
3. A text member that no rule and no inline value sets takes the value of the parent. A root takes the text default of the context.

**The selectors.** A selector holds a subject compound. A chain also holds an ancestor compound and a combinator. Set the parts of a compound with an object initializer, and give the compound to `Selector.For`, `Selector.Child`, or `Selector.Descendant`. The shortcuts `ForClass`, `ForName`, `ForType`, and `ForTypeAndClass` create a selector with one compound. Each factory throws `ArgumentException` for a wrong compound. A wrong compound holds no part, an empty name, or a state bit that is not a member of `PseudoStates`. `Child` and `Descendant` check the ancestor slot first, and the parameter name of the exception tells the slot. A `default(Selector)` matches nothing, and the builder rejects it.

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

`Overflow`, `Display`, and `BorderWidth` count as layout members, because the draw walk reads them and the layout engine reads them. A change of a text member also marks each child with `Style` (the inheritance). A plain element and an `Image` get `Paint` for a new font size, because only a `Label` measures text. The `Label` check is a type check in the pass.

**The write rule.** Game code writes `Element.Inline` or a rule. It does not write `Element.Style`. The style pass owns the style, so it rebuilds the style of a dirty element from the defaults. A direct write would not survive the next restyle.

**The sheets of a context.** `AddSheet` and `RemoveSheet` build the cascade again and mark each element of each root with `Style`. Both allocate, so call them in load code. `AddSheet` throws `InvalidOperationException` for a sheet that the context holds already. `RemoveSheet` returns `false` for an absent sheet. A sheet can be shared between contexts.

**The cost.** The inline set lives in each element, also when empty. It adds about 250 bytes to each element (`StyleProperties`) and 8 bytes for the view. The `--facts` line shows 1046.6 bytes for each element in the 1000-element tree with its 16-rule sheet. The sheet holds a state rule, a child rule, a descendant rule, and a name rule. The style step of the allocation gate sets and clears a hover state. `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp` and the style step of `ThousandElementAllocationTests` guard the pass (section 14). The rules have no index yet, so each restyled element scans all rules of the cascade. A rule index waits for a benchmark.

### Transitions

A property transition has a duration and an easing. The style pass writes the target value. A transition system interpolates per frame into the computed style. No allocation after the transition pool fills.

### Design Decisions

- Pseudo-states: `:hover`, `:active`, `:focus`, `:disabled`, `:checked`. A compound holds them as the bits of `PseudoStates`, and the match is bit math.
- Selector depth 2 (`.a > .b` and `.a .b`) in the first release. Depth is a constant, not a list. A selector holds one subject compound and one ancestor compound by value. Its only heap reference is the name string of a compound.
- A descendant chain walks to the grandparent only. Each chain rule has a constant cost, and the result differs from CSS for a deeper ancestor.
- One class for each compound. `.a.b` is not in the first release. The byte tiers already hold a larger count.
- A name rule compares `Element.Name` with an ordinal compare. No intern table and no name id: the compare allocates nothing, and a name set takes no lock.
- One byte for each specificity tier. The CSS order holds for each count below 256, with no proof that depends on the grammar.
- A class change, a state change, and a name change mark the full subtree, with no check of the sheets. The mark is simple and correct. A sheet-aware mark waits for a benchmark.
- `CompoundSelector` has no equality members. The default `ValueType.Equals` uses reflection for the `string?` name, so do not compare selectors in the hot path. The hot path compares no selector.
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

- **Rect form (Q1).** The walk passes x, y, w, h in viewport pixels to `Rect`, `Image`, and `PushClip`. The walk adds the position of each parent on the way down.
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

**The paint hook.** `Element.PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` is an internal virtual method with an empty body. The walk calls it after the `Rect` and the clip push, and before the children. Thus the clip of an element also clips its own content. The border box is in viewport pixels. The `opacity` argument is the product down the tree, so an override scales its own colors with it. The hook changes no tree, sets no mark, allocates nothing, and does not call `Draw`. The label and image elements override it.

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

1. `UiInput` subscribes to `GlfwWindow` events: cursor position, mouse button, scroll, key, char, drop, content scale.
2. A pointer event hit-tests the tree from the top-most root down, with `PickingEnabled` and clip rects.
3. The event walks capture, target, bubble. A handler can stop propagation.
4. Focus moves on click, Tab, and gamepad navigation.
5. `UiRoutingGate.PointerOverUi` and `KeyboardFocusInUi` tell the game to stand down.

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

```css
.hud-value { font-size: 24px; color: #ffffff; }
.hud-button:hover { background-color: #333333; }
```

The parser produces a `StyleSheet`. Parsing happens at load time, not per frame.

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
- **Style tests** in `BrutalUi.Core.Tests/Style/`. `StylePropertiesTests` checks the sparse set, the masks, and the throw for a value that is not a member. `StyleSheetTests` checks the builder, the sort, and the rule copy. `StylePassTests` checks the cascade, the compare of each member (a theory over all 32 members gives the kind of bit for each), and the dirty rule table of section 7. `InlineStyleTests` checks the view: a theory over all members checks that each set writes its own bit, that the same value marks nothing, and that a clear gives the default back. `TextInheritanceTests` checks the inheritance, the child marks, and the context defaults. `TypeSelectorTests` and `ElementTypeTests` check the type id and the type match. `MultiSheetTests` checks the merge of more than one sheet, `RemoveSheet`, and the mark of `AddRoot`. `PseudoStateSelectorTests`, `ChainSelectorTests`, and `NameSelectorTests` check each part of a compound, each chain form, the walk bound, the tiers, and the factory checks. `SubtreeMarkTests` checks the subtree mark of a class, state, and name change. The layout tests and the Yoga fixtures write through `Inline`, and no expected rect changed. `LayoutDifferentialTests` compares 200 random trees with a fresh layout.
- **Allocation tests** that run update and draw on a warm 1000-element tree and assert zero bytes allocated. `ThousandElementAllocationTests` is the gate. It covers the update and the draw, and it holds a style step (see below). The draw test runs full frames (`Update`, then `Draw` into a `RecordingRenderer`) after a warm-up. Each zero-byte gate of `BrutalUi.Core.Tests` calls `AllocationGate.ReleaseAllocationContext()` right before its measured region. A background GC can add the free part of the allocation context to the counter, and the release stops this. `BrutalUi.ImGui.Tests` runs no tests in parallel, so its gates do not call the helper.
- **The style step of the allocation gate.** The 1000-element tree holds a sheet of 16 rules, added with `AddSheet`: type rules, class rules, and rules with a type and a class. Four rules use the forms of selector-matching: a state rule (`.slot:hover`), a child rule (`.slot:hover > Element`), a descendant rule (`.row:hover Element`), and a name rule (`#header`). The frame body adds and removes two classes (`selected` on a grid slot and `muted` on the sidebar). It sets and clears `Hover` on a grid slot and on a stat row, and it changes the name of the header between two constant strings. It then marks `Style` on each element. Each round runs a full restyle of 1000 elements, with a match of the 16 rules, a merge, and the inheritance. The gate asserts zero allocated bytes. `TheStyleStepRestylesTheTree` checks that the step changes the slot fill, the label color, and the inherited values. `TheStyleStepMatchesEachSelectorForm` checks that each of the four new forms changes the value of its target, and that a miss keeps the old value. `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp` is a smaller gate on 50 elements and 4 rules. `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing` is a gate on a chain scene. The class `ThousandElementAllocationTests` has 10 tests. Three plants show which gates guard the loops. A planted `GC.KeepAlive(new object())` in `Restyle` failed 7 gates in stylesheet-model: both gates of `ThousandElementAllocationTests` and one gate in each of 5 other classes. The same plant in `Selector.Matches` fails 4 Core gates: `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp`, `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing`, and both gates of `ThousandElementAllocationTests`. It fails no ImGui gate, because the ImGui gates measure `Draw` only, the match runs in `Update`, and the ImGui label scene adds no sheet. The same plant in the `Name` setter fails the two gates of `ThousandElementAllocationTests`, because only the 1000-element step changes a name.
- **Headless ImGui tests** in `BrutalUi.ImGui.Tests`. A harness creates an ImGui context with no window and no GPU, runs frames, and reads the background draw list as text lines (clip, texture, element count, vertex bounds, colors). The tests need no `VulkanBackend`. `dotnet test` loads the native ImGui library of the pinned `Ahwoo.BRUTAL.ImGui` package.
- **A second allocation gate.** `ImGuiRendererAllocationTests` runs full frames through `ImGuiRenderer` on the 1000-element tree and through the direct calls (rounded rect with a border, square and rounded image, wrapped text, and a measure outside a frame). It asserts zero allocated bytes after warm-up. A test also checks that the measured draw does real work, so a draw that does nothing cannot pass.
- **Element tests** in `BrutalUi.Core.Tests/Elements/`: `LabelTests`, `LabelWrapTests`, `LabelTextTests`, `ImageTests`, and `ContentBoxTests`. `ContentInsetsTests` in `Layout/` checks the insets of the engine. `RecordingRenderer` is the measurer and the renderer. Its wrap rule counts bytes, and each byte is `size * 0.5` pixels wide. It breaks a line at a blank and cuts a word that is wider than the wrap width. The tests check the measure, draw, snap, and dirty rules. They also check the locale refresh, the scale modes, and the content box. A theory checks each width from 8 to 200, and from 20 to 400 with a percent padding. At each width, the text at the draw wrap width has the height of the measured box.
- **ImGui label tests.** `ImGuiLabelTests` and `ImGuiImageElementTests` in `BrutalUi.ImGui.Tests` run a `Label` and an `Image` through `ImGuiRenderer` on the headless context. They use the real ImGui measure and read the vertices of the background list. A sweep checks each width from 40 to 200, with no padding and with a padding of 5 %. At each width, the measure and the draw give the same line count. Each glyph lies inside the label box grown by 1 pixel.
- **A third allocation gate.** `ElementAllocationTests` runs 100 frames on a tree of 50 labels with wrapped text, 50 images with a width of 32, and one label with an `LString`. Each frame changes 10 labels and 10 images. On a label, it sets the text and the inline color, size, and font. On an image, it sets the tint, the UV rect, the texture, and the scale mode. A new UV span runs the image measure. The frame also sets one of two `LString` instances on the last label. Then it runs `Update` and `Draw` into a `RecordingRenderer`. The tests assert zero allocated bytes after warm-up for the span path, the string path, and `RefreshLocalizedText`. Before each measured refresh, the test changes the locale string, so each refresh takes the slow path. `TheMeasuredBodyDoesRealWork` checks that each frame measures and draws the new values. `ImGuiRendererAllocationTests.LabelUpdateAndDrawAllocateNothingAfterWarmUp` runs the same scene through `ImGuiRenderer`. The harness has one font, so that test does not change the font. A planted allocation in the label measure makes three of these tests fail. A planted allocation in the image measure, in the `Uv`, `Color`, or `Size` setter, in `SetText(LString)`, or in the slow path of the refresh makes one test or more fail.
- **Benchmarks** with BenchmarkDotNet for layout and draw on that tree. `TreeLayoutBenchmark` measures the full layout, a dirty subtree, and a clean update on the 1000-element tree. The settings panel is hidden, so the full layout pass lays out 780 elements and sets the 220 elements of the panel to zero. `PercentChainBenchmark` measures a leaf mark under a chain of percent sizes. `TreeDrawBenchmark` measures `Draw` on the 1000-element tree into `RecordingRenderer`. The time holds the walk and the cost of the recording.
- **Facts report.** The `--facts` mode of the benchmark project prints four `facts:` lines: the size of the tree, the callback calls of the two percent chains, and the number of levels that pass on a 1 MB stack.
- **Sample projects** as the integration test, built in CI. The HelloPanel smoke run (`--frames 120`) prints 3 lines: `HelloPanel: 120 frames rendered.`, `HelloPanel: last frame: draw commands 2, vertices 128.`, and `HelloPanel: bytes after warm-up: Update and Draw 0, frame <F>, in 117 frames.` The run is also a gate. It exits with code 1 and writes `HelloPanel: Update and Draw allocated <B> bytes after warm-up. The limit is 0 bytes.` to standard error when the UI region (`ViewportSize`, `Update`, and `Draw`) allocates after 3 warm-up frames. The frame number is a report only. In a measured frame in which the thread allocated since the last release, the sample releases the allocation context before the UI region and outside it, for the same cause as the test gates.

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

Stylesheet model and parser, pseudo-states, transitions, pointer and keyboard routing, focus, `Button`, `Toggle`, `ScrollView`, `TextField`.

### Phase 3: framework (2 weeks)

Stores, bindings, panels, modes, slots, commands, event bus, services, the action map, the XML element loader.

### Phase 4: windows and lists (2 weeks)

`WindowChrome`, `WindowManager`, persistence, virtualized `ListView`, `DataList`, the debugger window.

### Phase 5: world space (2 weeks)

`WorldPanel`, offscreen render target, the WorldSpace sample, the upstream issue if the render pass needs a change.

---

*This is a living document. Update it as technical decisions land, patterns emerge, and the architecture evolves.*
