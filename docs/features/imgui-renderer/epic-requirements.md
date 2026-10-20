# ImGui Renderer - Epic Requirements

**Created:** 2026-10-18
**Phase:** Phase 1 (skeleton and tree)

> Epic-level requirements for the whole epic. Each child feature has its own `requirements.md` that `/plan-feature imgui-renderer/<feature-name>` consumes.

## Overview

This epic puts the element tree on screen. It owns the `IUiRenderer` interface in `BrutalUi.Core`, the draw pass in `UiContext`, and the first renderer in `BrutalUi.ImGui`, which writes into an `ImDrawList`. It adds the `Label` and `Image` elements and the HelloPanel sample with a static screen. The features belong together because each one turns a part of the tree walk into pixels, and the sample proves the full path. This epic closes Phase 1 of the technical design.

## Requirements

- `BrutalUi.Core` defines `IUiRenderer` with `Begin`, `PushClip`, `PopClip`, `Rect`, `Image`, `Text`, `MeasureText`, and `End`. The methods take spans and structs only. No strings, no arrays.
- `BrutalUi.Core` defines the value types the interface uses: `Rgba8`, `Corners`, `FontHandle`, and `TextureHandle`. `BrutalUi.Core` keeps no ImGui reference.
- `ComputedStyle` gains the paint fields the draw pass reads: background color, border color, corner radius, opacity, and overflow clip. The style-system epic writes them later from the cascade. In this epic, code sets them inline.
- `UiContext.Draw(IUiRenderer)` walks the tree in paint order, pushes a clip rect for an element with overflow hidden, and emits one `Rect` for each element with a visible background or border. The walk allocates nothing after warm-up.
- `BrutalUi.ImGui` ships `ImGuiRenderer`, which implements `IUiRenderer` on an `ImDrawList`. It writes into `ImGui.GetBackgroundDrawList()` (Q2). A private draw list waits for the world-space epic.
- `BrutalUi.ImGui` ships a font service that maps a `FontHandle` to an `ImFontPtr` and a size, and a texture service that maps a `TextureHandle` to an `ImTextureRef` registered through the Vulkan backend. Vulkan only in this epic (Q6).
- `Label` is an element that holds UTF-8 text, a font handle, a size, and a color. It sets the measure callback and measures through `IUiRenderer.MeasureText`. It supports word wrap at the available width.
- `Label` accepts an `LString` from `Brutal.Localization` and re-measures when the locale changes.
- `Image` is an element that holds a texture handle, a UV rect, a tint, and a scale mode. It sets the measure callback from the texture size.
- The HelloPanel sample shows a static screen with a panel, a label, and an image through the Vulkan backend. The brutal-ui tree only, the ImGui text window goes (Q5). The image is a procedural texture that the sample fills in code (Q4). It accepts `--frames N` and exits with a code, so both test scripts run it as the GPU smoke run.
- The allocation test extends to `UiContext.Draw` on a warm 1000-element tree with a fake renderer. It asserts zero bytes.
- `BrutalUi.Core.Tests` tests the draw walk against a recording renderer. `BrutalUi.ImGui` has no unit test that needs a GPU.
- All code uses `Brutal.Numerics` types, UTF-8 text, file-scoped namespaces, nullable on, `required` create-info structs, and XML doc comments on public members.
- No new native dependency and no new package pin (A9). The `Ahwoo.BRUTAL.ImGui` and `Ahwoo.BRUTAL.ImGui.Backends` pins cover the epic. The font service uses the ImGui default loader (Q3).

## Planned Features

1. **renderer-interface**. `IUiRenderer`, the handle and color structs, the paint fields on `ComputedStyle`, and `UiContext.Draw` with the clip and rect walk, tested with a recording renderer. It comes first because every other feature calls the interface or the walk.
2. **imgui-draw-list**. `ImGuiRenderer` on `ImDrawList`, the font service, and the texture service in `BrutalUi.ImGui`. It depends on feature 1 for the interface and the handles.
3. **label-and-image**. The `Label` and `Image` elements, the measure callback caller, word wrap, and the `LString` hook. It depends on feature 1 for `MeasureText` and on feature 2 to show text and textures on screen.
4. **hello-panel-sample**. The HelloPanel sample with a static screen, the `--frames N` contract, the draw allocation test, and the test script update. It comes last because it uses every feature of the epic and closes Phase 1.

## Dependencies

- The `dev-ops` epic. The `BrutalUi.Core`, `BrutalUi.ImGui`, and sample projects, the package pins, and both test scripts exist.
- The `core-tree` epic. `Element`, `UiContext.Update`, the dirty flags, the layout rects, and the measure callback slot exist.
- Dear ImGui 1.92.2 through `Ahwoo.BRUTAL.ImGui` and the backends, with the dynamic font system and `VulkanBackend.AddTexture`.

## Out of Scope

- The style cascade, selectors, pseudo-state rules, and transitions. The style-system epic owns them. This epic sets paint fields inline.
- Pointer and keyboard routing, hit testing, and focus. The input epic owns them.
- `Button`, `Toggle`, `ScrollView`, `TextField`, and the other interactive elements. They need input and style.
- Shadows, blur, gradients, and other effects through `ImDrawList.AddCallback`.
- Rich text, kerning control, and bidirectional text.
- The render-to-texture path and the WorldSpace sample. The world-space epic owns them.
- A second renderer on `Brutal.Vulkan.Abstractions`. The interface permits it. This epic ships the ImGui implementation only.
- The XML element loader and the stylesheet parser.
- FreeType font loading (Q3).
- The OpenGL backend texture registration (Q6).
- A WorldSpace sample scene. It stays a stub (A8).

---

*Consumed by `/plan-epic imgui-renderer`. After planning, run `/plan-feature imgui-renderer/<feature-name>` per feature in the recommended order.*
