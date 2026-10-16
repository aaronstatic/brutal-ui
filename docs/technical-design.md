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
- **Full CSS.** The style system supports class, name, and pseudo-state selectors only. No descendant selectors in the first release.
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
│   ├── HelloPanel/             # one panel, one store, one mode
│   └── WorldSpace/             # panel on a quad
├── tests/
│   ├── BrutalUi.Core.Tests/
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

**`tests/`** holds xUnit tests for each package and a BenchmarkDotNet project with the allocation guard.

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
   │     ├─ Style pass: resolve classes and pseudo-states for dirty elements
   │     └─ Layout pass: flexbox on dirty subtrees
   │
   ├─ UiContext.Draw(IUiRenderer)
   │     └─ Tree walk ──► DrawCommand list ──► ImGuiRenderer ──► ImDrawList
   │
   └─ VulkanBackend / OpenGlBackend render ImDrawData
```

### Frame order

1. The game calls `GlfwBackend.NewFrame()` and `ImGui.NewFrame()` as it does today.
2. The game calls `UiContext.Update(deltaTime)`. The framework applies pending mode changes and store notifications. The style and layout passes run on dirty subtrees.
3. The game calls `UiContext.Draw()`. The tree walk emits draw commands into the renderer.
4. The game renders the ImGui frame as it does today.

### Dirty tracking

Each element carries dirty flags for style, layout, and paint. A change marks the element and its ancestors. The passes visit dirty subtrees only. A static screen costs one tree walk for the draw.

---

## 5. Element Tree

### Purpose

The retained tree that every other layer reads and writes.

### Shape

```csharp
public class Element
{
    public Element();
    public string? Name { get; set; }
    public Element? Parent { get; }
    public ElementList Children { get; }       // view on a pooled array, no allocation
    public ClassList Classes { get; }          // view on an inline set of 8, then a pooled array
    public PseudoStates States { get; set; }   // Hover, Active, Focus, Disabled, Checked
    public bool IsPickable { get; set; }       // default true
    public ref ComputedStyle Style { get; }    // resolved style, struct
    public ref LayoutNode Layout { get; }      // flexbox input and output, struct
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
    public bool Add(ClassId id);               // false for a duplicate
    public bool Remove(ClassId id);            // false when absent
    public bool Contains(ClassId id);
}

public readonly struct ClassId;                // small integer id of an interned class name

public static class ClassName
{
    public static ClassId Intern(string name); // thread-safe, call it before the hot path
    public static string GetName(ClassId id);
}

public sealed class UiContext
{
    public UiContext();
    public ReadOnlySpan<Element> Roots { get; }
    public void AddRoot(Element root);
    public bool RemoveRoot(Element root);
    public void Update(float deltaTime);
}
```

`DirtyFlags` has the kinds `Style`, `Layout`, and `Paint`, and one descendant bit for each kind. `ElementBuilder` adds the extension methods `Class(string)`, `Class(ClassId)`, and `Child(Element)` for a fluent build.

Built-in element types: `Element`, `Label`, `Image`, `Button`, `Toggle`, `Slider`, `TextField`, `ScrollView`, `ListView`, `WindowChrome`.

### Events

A struct event with a phase (capture, target, bubble) walks the tree. Handlers register on an element by event type. No closures in the hot path. A handler is a static method plus a target object, or a command id.

### Design Decisions

- `ComputedStyle` and `LayoutNode` are structs stored inline in the element. The layout engine reads them through `ref`. No per-node heap object beyond the element itself.
- Each dirty kind has a self bit and a descendant bit. A mark sets the self bit and sends the descendant bit up to the root, and it stops at the first ancestor that holds the bit. The walk enters only the subtrees that have a descendant bit.
- Text lives in owned UTF-8 buffers on the element. A `Label` accepts `ReadOnlySpan<byte>`, `string`, and `LString`.

---

## 6. Layout Engine

### Purpose

Flexbox layout on the element tree with the Yoga subset that games use.

### Supported properties

`display`, `position` (relative, absolute), `flex-direction`, `flex-wrap`, `justify-content`, `align-items`, `align-self`, `align-content`, `flex-grow`, `flex-shrink`, `flex-basis`, `width`, `height`, `min-*`, `max-*`, `margin`, `padding`, `border-width`, `left`, `top`, `right`, `bottom`, `overflow`. Length units: pixels, percent, auto.

### Algorithm

The engine follows the Yoga algorithm structure: measure, main-axis line breaking, main-axis distribution, cross-axis alignment, absolute children. It runs on `ref LayoutNode` structs and writes `float4` rects. A measure callback on an element returns intrinsic size for text and images.

### Design Decisions

- C# over a Yoga binding to keep the allocation rule and avoid a native binary per platform.
- No grid in the first release. Absolute position and flex cover game screens.

---

## 7. Style System

### Purpose

Classes, pseudo-states, and a cascade on the element tree.

### Model

```csharp
public struct StyleRule
{
    public Selector Selector;        // class, name, type, pseudo-state, and a parent-child chain of depth 2
    public StyleProperties Props;    // sparse set of property values
    public int Specificity;
}
```

A `StyleSheet` is a sorted rule list. The style pass matches rules against a dirty element, merges by specificity, and writes `ComputedStyle`. Inline style on an element wins.

### Transitions

A property transition has a duration and an easing. The style pass writes the target value. A transition system interpolates per frame into the computed style. No allocation after the transition pool fills.

### Design Decisions

- Pseudo-states: `:hover`, `:active`, `:focus`, `:disabled`, `:checked`.
- Selector depth 2 (`.a > .b` and `.a .b`) in the first release. Depth is a constant, not a list.
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
    void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8);
    float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth);
    void End();
}
```

### ImGui implementation

`ImGuiRenderer` writes into a private `ImDrawList` owned by the context, or into `ImGui.GetBackgroundDrawList()` when the game asks for that. `FontHandle` maps to an `ImFontPtr`. `TextureHandle` maps to an `ImTextureRef` registered through `VulkanBackend.AddTexture` or the OpenGL equivalent.

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
- **Unit tests** on the style cascade, the XML loader, the stylesheet parser, and the framework diff.
- **Allocation tests** that run update and draw on a warm 1000-element tree and assert zero bytes allocated.
- **Benchmarks** with BenchmarkDotNet for layout and draw on that tree.
- **Sample projects** as the integration test, built in CI.

### What is not tested

Pixel output. The renderer tests assert the draw command list, not the image.

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

Solution, package pins, `Element`, flexbox, `IUiRenderer`, `ImGuiRenderer`, `Label`, `Image`, the HelloPanel sample with a static screen.

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
