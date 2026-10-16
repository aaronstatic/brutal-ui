# brutal-ui feasibility report

Date: 2026-10-16
BRUTAL commit examined: `a25cdf80` (2026-10-16)
mosaic-ui version examined: 0.4.0

## Verdict

The idea is feasible. BRUTAL has no retained-mode UI, no layout engine, no stylesheet, and no markup format. It does have a complete Dear ImGui 1.92.2 binding with Vulkan and OpenGL renderer backends. That binding gives brutal-ui a vector draw API, a font atlas, texture upload, and clip rects at no cost. brutal-ui can build a retained element tree, a flexbox layout engine, a style system, and the mosaic-ui panel and mode model on top of that draw API. World-space UI is feasible through render-to-texture.

Estimated effort for a first usable release: 8 to 12 weeks for one developer.

## What BRUTAL has today

The repository holds 47 libraries and 5585 C# files. The table lists each library that touches UI.

| Library | What it gives brutal-ui | Notes |
| --- | --- | --- |
| `Brutal.ImGui` | Dear ImGui 1.92.2, docking branch. `ImDrawList` with rects, rounded rects, text, images, polylines, arcs, clip rects, draw callbacks. Dynamic font system (`ImFontBaked`, `PushFont(font, size)`). | 1138 files, generated bindings. `ImString` is an arena UTF-8 string with a one-frame life. |
| `Brutal.ImGui.Backends` | `GlfwBackend` (input, cursors, clipboard, multi-viewport). `VulkanBackend` and `OpenGlBackend` that render `ImDrawData`. `VulkanBackend.AddTexture(sampler, imageView, layout)` registers any user texture for the draw list. | The interfaces `IPlatformBackend` and `IRendererBackend` expose only `NewFrame()`. |
| `Brutal.ImGui.Abstractions` | `ConsoleWindow`, a terminal and log window. | The only managed UI component in the repository. It is an example of a class that sits on the ImGui binding. |
| `Brutal.ImGui.Extensions` | Generic helpers for `Combo`, `DragScalar`, `SliderScalar`, enum flags. | Debug UI helpers only. |
| `Brutal.ImGui.FreeType` | FreeType font loader for the ImGui atlas. | Better glyph quality than the stb loader. |
| `Brutal.FreeType` | Native FreeType binary only. | No C# source in the repository. |
| `Brutal.Sdl.Ttf` | SDL3_ttf bindings, with the GPU text engine enums. | An alternative text path if the project does not use ImGui. |
| `Brutal.Glfw` | Window, cursor, keyboard, mouse, gamepad, drop, content scale events as C# events on `GlfwWindow`. | The natural input source for brutal-ui. |
| `Brutal.Sdl` | SDL3 window and input. | A second platform target. |
| `Brutal.Texture` | Loads PNG, KTX, DDS through stb, KTX and GLI. | Image loading for UI sprites and icons. |
| `Brutal.Vulkan.Abstractions` | `DeviceEx`, `ImageEx`, `BufferEx`, `VertexInput`, staging pool, descriptor helpers. | Needed for the offscreen render target in world-space UI. |
| `Brutal.Localization` | `LString`, ResX reader and writer, allocation-free format. | brutal-ui text elements can accept `LString` directly. |
| `Brutal.NotCurses` | Terminal UI. | Not relevant to game UI. |

## What BRUTAL does not have

- No retained element tree. ImGui is immediate mode. Each frame rebuilds the UI from code.
- No layout engine. ImGui places widgets in a cursor flow. There is no flexbox, no grid, no anchors.
- No stylesheet or theme file. ImGui has a style struct with fixed slots.
- No markup format. There is no equivalent of UXML.
- No data binding.
- No world-space UI. The search for "world space" in the source returns only Jolt physics files.
- No project files. The README says the `.csproj` files, the solution, and the build scripts are not in the repository yet. Users build on the NuGet packages, published under the `Ahwoo` profile.
- No UI library from the community yet. The BRUTAL forum has a "Tools and Libraries" section. The one UI-related post there is an ImGui code editor component.

The CONTRIBUTING file says the maintainers reject engine-style features and abstractions over the native APIs. A UI framework belongs in a separate package, which is what this project is. The forum asks authors to post such packages in the "Tools and Libraries" section.

## Design options for the renderer

### Option A: render through ImDrawList (recommended)

brutal-ui keeps its own element tree, layout, and style. At the end of the frame, it walks the tree and emits commands into an `ImDrawList`. The existing `VulkanBackend` or `OpenGlBackend` renders that list.

Pros:

- Both Vulkan and OpenGL work on day one.
- Fonts, glyph baking, atlas upload, texture registration, and clipping already exist.
- The ImGui debug UI and brutal-ui share one render path and one input path.
- A project that already uses ImGui adds brutal-ui with no new native dependency.

Cons:

- `ImDrawList` has no gradient fills beyond `AddRectFilledMultiColor`, no shadows, no blur. Those need shader effects through `AddCallback` or a second pass.
- `ImDrawVert` uses 32-bit color and 16-bit indices by default. Large lists need `VtxOffset`, which the Vulkan backend supports.
- The project depends on the ImGui context life cycle.

### Option B: own Vulkan renderer

brutal-ui owns a pipeline, vertex buffers, a glyph atlas, and a texture table on top of `Brutal.Vulkan.Abstractions`.

Pros: full control over effects, batching, and the vertex format.

Cons: three to five more weeks of work, a second path for OpenGL, and a text stack to write from `Brutal.FreeType` or `Brutal.Sdl.Ttf`.

### Recommendation

Start with Option A behind an `IUiRenderer` interface. The interface receives a flat list of draw commands from the tree walk. Option B becomes a second implementation later, with no change to the element tree, the layout, or the mosaic-ui layer.

## Screen-space UI

The screen-space path on Option A:

1. The game calls `UiContext.NewFrame()` after `GlfwBackend.NewFrame()`.
2. brutal-ui runs dirty-layout passes on the element tree.
3. brutal-ui emits into `ImGui.GetBackgroundDrawList()` or into a private `ImDrawList` that it appends to the frame.
4. The existing backend renders the frame.

Input comes from the `GlfwWindow` events, not from the ImGui IO struct, so brutal-ui can run without ImGui widgets on screen. The `UIRoutingGate` concept from mosaic-ui ports directly: brutal-ui reports if the pointer is over a panel, and the game stands down.

## World-space UI

BRUTAL has no scene graph, so a world-space panel is a texture the game draws on a quad or a mesh. The path:

1. brutal-ui builds a private `ImDrawList` for the panel in the panel's own pixel space.
2. brutal-ui wraps it in an `ImDrawData` with `ImDrawDataBuilder` and calls `VulkanBackend.RenderDrawData` into an offscreen `ImageEx` render target.
3. The game samples that image on a mesh with its own renderer.
4. For input, the game raycasts the mesh, converts the hit UV to panel pixels, and calls `panel.PointerEvent(...)`.

The Vulkan backend `RenderDrawData` takes a render pass at construction time. The world-space path needs a second `VulkanBackend` instance, or a small change to pass a render pass per call. This is the one place where Option A touches BRUTAL internals. Interactive 3D "WorldFeature" objects from mosaic-ui stay as interfaces that the game implements.

## Mapping mosaic-ui to brutal-ui

mosaic-ui Runtime is 4363 lines. The table shows the port cost for each part.

| mosaic-ui part | Unity dependency | Port cost | Plan |
| --- | --- | --- | --- |
| `Store`, `StoreSubscription` | `INotifyBindablePropertyChanged` | Low | Replace with a brutal-ui binding interface. |
| `EventBus`, `ServiceRegistry`, `CommandRegistry`, `ModeHistory`, `CallbackDisposable` | None | None | Copy. |
| `PanelController`, `PanelInstance` | `VisualElement` root | Medium | Root becomes a brutal-ui `Element`. |
| `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `WindowDefinition` | ScriptableObject | Low | Plain C# records. Optional JSON loader later. |
| `MosaicUIManager`, `SlotContainer` | UI Toolkit tree, `UIDocument` | Medium | Same diff logic on the brutal-ui tree. |
| `WindowChrome`, `WindowManager`, `WindowPersistence` | UI Toolkit, PlayerPrefs | Medium | Chrome is a brutal-ui element. Persistence writes a file. |
| `DataList` | `ListView` | Medium | Needs a virtualized list element in brutal-ui. |
| `InputService`, `InputBindingExtensions` | Input System asset | High | Replace with a GLFW action map: keys, mouse, gamepad, control schemes. |
| `UIRoutingGate` | Runtime panel | Low | Hit test on the brutal-ui tree. |
| `WorldFeature`, `WorldController` | MonoBehaviour, camera | Low | Become interfaces with the same life cycle. |
| `Inspection/*`, Editor debugger | Editor window | Medium | Render the debugger as a brutal-ui window or an ImGui window. |
| UXML templates | UI Toolkit | High | See the next section. |
| USS styles | UI Toolkit | High | See the next section. |

## The UI Toolkit gap

Unity UI Toolkit gives mosaic-ui four things that BRUTAL lacks. All four are in scope for brutal-ui.

1. **Element tree.** Classes, names, parent and child relations, hit testing, focus, event bubbling. About 1500 lines.
2. **Layout engine.** UI Toolkit uses Yoga. A C# flexbox with the Yoga subset that games use is about 2000 lines. Writing it in C# avoids a new native dependency. The `Brutal.Interop` layer makes a Yoga binding possible later.
3. **Style system.** Style classes, a cascade with pseudo-states like hover and active, transitions. A C# style object model first, a text stylesheet format second. About 1500 lines plus a parser.
4. **Markup.** A UXML-like XML format with a loader. The first release can use a C# builder API instead. A parser adds about 800 lines.

A minimum widget set: Label, Image, Button, Toggle, Slider, TextField, ScrollView, ListView, and a Window chrome. TextField needs IME and clipboard support from GLFW, which `GlfwBackend` already shows.

## Risks

- **No project files in the BRUTAL repository.** brutal-ui must build on the NuGet packages. A package version pin is required, and a BRUTAL update can change generated signatures.
- **ImGui context coupling.** Option A requires an ImGui context, even for a game with no debug UI. The `IUiRenderer` interface limits the blast radius.
- **Text layout.** ImGui text has no rich text, no kerning control, and no bidirectional text. Rich text needs a custom text layout on top of `ImFontBaked` glyph data.
- **World-space render pass.** The Vulkan backend binds one render pass at construction. Test a second backend instance early.
- **Allocation policy.** BRUTAL code avoids allocations in hot paths. brutal-ui must do the same in layout and draw, or users will see it as foreign.
- **Licence.** The BRUTAL licence is MIT with an attribution clause. brutal-ui can use MIT on its own. A game still must show the BRUTAL logo and notice.

## Package layout

| Package | Content |
| --- | --- |
| `BrutalUi.Core` | Element tree, layout, style, input, renderer interface. No ImGui reference. |
| `BrutalUi.ImGui` | `IUiRenderer` on `ImDrawList`, text through the ImGui font atlas, texture registration through the backend. |
| `BrutalUi.Framework` | The mosaic-ui layer: stores, panels, modes, windows, commands, event bus, services. |
| `BrutalUi.Debugger` | The state, composition, and event inspector as a window. |

## Phases

| Phase | Scope | Estimate |
| --- | --- | --- |
| 1 | Project skeleton, NuGet pins, `Element`, flexbox layout, `IUiRenderer`, ImGui renderer, Label and Image. | 2 weeks |
| 2 | Style system, pointer and keyboard input, focus, Button, Toggle, ScrollView, TextField. | 3 weeks |
| 3 | Port the mosaic-ui framework layer and the GLFW action map. | 2 weeks |
| 4 | Windows, DataList, debugger window. | 2 weeks |
| 5 | World-space render target and sample. XML markup and stylesheet loader. | 2 weeks |

## Open questions for the author

1. Does the first release require a text markup format, or is a C# builder API enough?
2. Is OpenGL a required target, or is Vulkan only acceptable? This decides if Option B stays open.
3. Should the GLFW action map support SDL as well?
