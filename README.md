# brutal-ui

A UI framework for the [BRUTAL](https://github.com/Ahwoo/BRUTAL) C# framework. Retained tree, flexbox, styles, markup, data binding. Zero allocations per frame.

brutal-ui gives you an element tree, a flexbox layout engine, a stylesheet, an XML markup format, data binding, and a panel and mode model. It renders through the Dear ImGui draw list that BRUTAL already ships. It allocates nothing in the frame loop after warm-up, and a test proves it.

> **Status: early alpha, private.** The element tree, the layout engine, the allocation gate, and the renderer interface are in place. The ImGui renderer, the style system, input, the framework layer, and markup are in progress. Expect the API to change before the first public release.

## Why brutal-ui

BRUTAL gives you Vulkan, OpenGL, GLFW, a Dear ImGui binding, textures, and localization. It gives you no player-facing UI. ImGui is immediate mode and is the right tool for debug panels. A game menu, a HUD, or an inventory screen needs persistent state, transitions, and data binding. brutal-ui fills that gap.

- **Zero allocation in the frame loop.** Layout, input, and draw allocate no managed memory after warm-up. A test on a 1000-element tree fails when they do.
- **One package reference.** brutal-ui draws into `ImDrawList`. The BRUTAL Vulkan and OpenGL backends render it. No native dependency, no second render path.
- **Declare the screen.** A mode is a set of named panels in named slots. You declare the next mode, and the framework computes the diff.
- **Markup and code are equal.** Build an element tree from C# or from XML. Style it with a small CSS subset: classes, names, and pseudo-states.
- **Familiar to Unity developers.** The concepts of UI Toolkit, UXML, and USS carry over under new names. A port is a translation, not a redesign.
- **BRUTAL conventions.** `Brutal.Numerics` vector types, UTF-8 text, `required` create-info structs, file-scoped namespaces.

## Packages

| Package | Contents | References |
|---------|----------|------------|
| `BrutalUi.Core` | The element tree, the flexbox engine, the style system, input, focus, and the `IUiRenderer` interface | `Brutal.Numerics`, `Brutal.Localization` |
| `BrutalUi.ImGui` | `IUiRenderer` on `ImDrawList`, fonts through the ImGui atlas, textures through the BRUTAL backends, render to texture | `Brutal.ImGui`, `Brutal.ImGui.Backends` |
| `BrutalUi.Framework` | Stores, panels, modes, slots, windows, commands, an event bus, and a service registry | `BrutalUi.Core` |
| `BrutalUi.Markup` | The XML element loader and the stylesheet parser | `BrutalUi.Core` |
| `BrutalUi.Debugger` | A state, composition, and event inspector window | `BrutalUi.Framework` |

`BrutalUi.Core` references no ImGui package. A second renderer is possible without a change to the tree.

## Requirements

- .NET 10 SDK and C# 14.
- A game that already runs BRUTAL with ImGui, on Vulkan or OpenGL.
- On Linux, the Vulkan loader package `libvulkan-dev`. On Windows, `vulkan-1.dll` from the graphics driver.

## Build

```
git clone https://github.com/aaronstatic/brutal-ui.git
cd brutal-ui
dotnet build brutal-ui.sln
dotnet test --solution brutal-ui.sln
```

The full check on Linux, with a GPU smoke run inside Xvfb:

```
scripts/test-linux.sh
```

The full check on Windows:

```
scripts/test-windows.ps1
```

The runbooks `docs/testing-linux.md` and `docs/testing-windows.md` list the one-time setup for each machine.

To pack the NuGet packages into `artifacts/packages/`:

```
dotnet pack brutal-ui.sln
```

## Usage

The packages are not on NuGet yet. Pack them, then add the folder `artifacts/packages/` as a package source in your `nuget.config`. Then reference the packages you need:

```xml
<ItemGroup>
  <PackageReference Include="BrutalUi.Core" Version="0.1.0-alpha" />
  <PackageReference Include="BrutalUi.ImGui" Version="0.1.0-alpha" />
</ItemGroup>
```

### The frame loop

brutal-ui adds two calls to the ImGui frame that your game already runs.

```csharp
// Once, at start.
var ui = new UiContext();
var renderer = new ImGuiRenderer();   // BrutalUi.ImGui

// Each frame.
glfwBackend.NewFrame();
ImGui.NewFrame();

ui.Update(deltaTime);                 // style and layout on dirty subtrees
ui.Draw(renderer);                    // walk the tree into the ImDrawList

ImGui.Render();
vulkanBackend.RenderDrawData(ImGui.GetDrawData(), commandBuffer);
```

### An element tree in C#

```csharp
var panel = new Element().Class("hud")
    .Child(new Label { Name = "score" }.Class("hud-value").Text("0"))
    .Child(new Button { Name = "pause" }.Class("hud-button").Text("Pause").Command("game.pause"));

ui.AddRoot(panel);
```

### The same tree in XML

```xml
<ui:Panel class="hud" xmlns:ui="brutal-ui">
  <ui:Label name="score" class="hud-value" text="0" />
  <ui:Button name="pause" class="hud-button" text="Pause" command="game.pause" />
</ui:Panel>
```

### A stylesheet

```css
.hud-value { font-size: 24px; color: #ffffff; }
.hud-button:hover { background-color: #333333; }
```

### A panel with a store and a mode

```csharp
var score = new Store<int>(0);

panel.Query<Label>("score").BindText(score, value => value);

ui.Modes.Register("gameplay", new ModeDefinition
{
    Panels = [new PanelPlacement { Panel = "hud", Slot = "top" }],
});

ui.Modes.Set("gameplay");
```

The builder, the XML loader, the stylesheet, the stores, and the modes are not released yet. The snippets above show the planned API from `docs/technical-design.md`. The sample `samples/HelloPanel` shows the frame loop that works today.

## Repository layout

```
src/        the five packages
samples/    HelloPanel (one panel, one store, one mode) and WorldSpace (a panel on a quad)
tests/      xUnit tests for each package, and the BenchmarkDotNet project with the allocation gate
scripts/    the Linux and Windows test scripts
docs/       the mission statement, the technical design, the feasibility study, and the feature plans
```

## Documentation

- `docs/mission-statement.md` for the goals and the boundaries.
- `docs/technical-design.md` for the package layout, the frame order, and the phase plan.
- `docs/feasibility.md` for what BRUTAL has and does not have.
- `docs/overview.md` for the feature status.
- `CHANGELOG.md` for each notable change and the benchmark numbers.

## License

MIT. See `LICENSE` and `THIRD-PARTY-NOTICES.md`.
