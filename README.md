# brutal-ui

A UI framework for the [BRUTAL](https://github.com/ahwootech/brutal) C# framework. Retained tree, flexbox, styles, data binding. Zero allocations per frame.

brutal-ui gives you an element tree, a flexbox layout engine, a stylesheet with a CSS-subset parser, data binding, and a panel and mode model. It renders through the Dear ImGui draw list that BRUTAL already ships. It allocates nothing in the frame loop after warm-up, and a test proves it. The XML markup loader is planned.

> **Status: early alpha, public.** The repository is public. Nothing is on NuGet yet. The element tree, the flexbox engine, the allocation gate, the ImGui renderer, the style system with a CSS-subset parser, the widgets, input routing, the gamepad state, the typed action map, and the framework layer (stores, bindings, commands, panels, modes) are in place and tested: the suite runs 2921 tests. The XML markup loader and the debugger window are not written yet, and the input epic has two features left. Expect the API to change before the first public release.

## Why brutal-ui

BRUTAL gives you Vulkan, OpenGL, GLFW, a Dear ImGui binding, textures, and localization. It gives you no player-facing UI. ImGui is immediate mode and is the right tool for debug panels. A game menu, a HUD, or an inventory screen needs persistent state, transitions, and data binding. brutal-ui fills that gap.

- **Zero allocation in the frame loop.** Layout, input, and draw allocate no managed memory after warm-up. A test on a 1000-element tree fails when they do.
- **One package reference.** brutal-ui draws into `ImDrawList`. The BRUTAL Vulkan and OpenGL backends render it. No native dependency, no second render path.
- **Declare the screen.** A mode is a set of named panels in named slots. You declare the next mode, and the framework computes the diff.
- **Style it, and read the input like a game.** Style the tree with a small CSS subset: classes, names, and pseudo-states. Bind keys, mouse, sticks, and triggers to typed actions, and read one value per frame. A planned XML loader brings the same tree from markup.
- **Familiar to Unity developers.** The concepts of UI Toolkit, UXML, and USS carry over under new names. A port is a translation, not a redesign.
- **BRUTAL conventions.** `Brutal.Numerics` vector types, UTF-8 text, `required` create-info structs, file-scoped namespaces.

## Packages

| Package | Contents | References |
|---------|----------|------------|
| `BrutalUi.Core` | The element tree, the flexbox engine, the style system, the widgets, input, focus, and the `IUiRenderer` interface | `Brutal.Numerics`, `Brutal.Localization` |
| `BrutalUi.ImGui` | `IUiRenderer` on `ImDrawList`, fonts through the ImGui atlas, textures through the BRUTAL backends, render to texture | `Brutal.ImGui`, `Brutal.ImGui.Backends` |
| `BrutalUi.Glfw` | The GLFW input adapter `GlfwUiInput`, which pushes window events and the gamepad state to a `UiContext` | `Brutal.Glfw`, `BrutalUi.Core` |
| `BrutalUi.Framework` | Stores, panels, modes, slots, windows, commands, the typed action map, an event bus, and a service registry | `BrutalUi.Core` |
| `BrutalUi.Markup` | The CSS-subset stylesheet parser. The XML element loader is planned | `BrutalUi.Core` |
| `BrutalUi.Debugger` | A state, composition, and event inspector window. Planned | `BrutalUi.Framework` |

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
var renderer = new ImGuiRenderer(new ImGuiRenderer.CreateInfo   // BrutalUi.ImGui
{
    Fonts = new ImGuiFontService(),
    Textures = new ImGuiTextureService(),
});
var ui = new UiContext(new UiContext.CreateInfo { Measurer = renderer });

// Each frame.
glfwBackend.NewFrame();
ImGui.NewFrame();

ui.Update(deltaTime);                 // input, action reads, bindings, style, layout
ui.Draw(renderer);                    // walk the tree into the ImDrawList

ImGui.Render();
vulkanBackend.RenderDrawData(ImGui.GetDrawData(), commandBuffer);
```

### An element tree in C#

```csharp
var score = new Label(new Label.CreateInfo()) { Name = "score" }.Class("hud-value");
score.SetText("0");

var pause = new Button(new Button.CreateInfo()) { Name = "pause" }.Class("hud-button");
pause.SetText("Pause");
pause.BindCommand(registry, "game.pause"u8);   // BrutalUi.Framework

var hud = new Element().Class("hud").Child(score).Child(pause);
ui.AddRoot(hud);
```

### The same tree in XML (planned)

The XML element loader is not written yet. The target shape:

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

```csharp
var result = new StyleSheetParser().Parse(cssUtf8, "screen.css");   // BrutalUi.Markup
ui.AddSheet(result.GetSheetOrThrow());
```

### A store and a mode

```csharp
var score = new Store<int>(new Store<int>.CreateInfo { Initial = 0 });
scoreLabel.BindText(score, static (int value) => $"{value}");

var manager = new UiManager(new UiManager.CreateInfo { Context = ui, Panels = panels });
manager.Register(new ModeDefinition("gameplay"u8, "main"u8, [new ModePanel("top"u8, "hud"u8)]));
manager.SetMode("gameplay"u8);
```

The panel and layout records that `UiManager` holds are plain definitions; `samples/HelloPanel/HelloModes.cs` registers them. The sample runs the full frame: widgets, a stylesheet, stores, two modes, and a typed action map that moves a `Move` vector with W, A, S, D or the left stick.

### Typed actions

```csharp
var map = new UiActionMap(new UiActionMap.CreateInfo { Commands = commands });
map.AddAction("Move"u8, UiActionType.Vector2);
map.BindKeyVector("Move"u8, W, A, S, D);
map.BindGamepadStick("Move"u8, UiGamepadStick.Left);

// After ui.Update, each frame:
float2 move = map.ReadVector("Move"u8);
```

## Repository layout

```
src/        the six packages
samples/    HelloPanel (widgets, a store, two modes, typed actions) and WorldSpace (a panel on a quad)
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
