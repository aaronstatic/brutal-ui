# ImGui Draw List - Requirements

**Epic:** imgui-renderer
**Created:** 2026-10-20

## Overview

This feature delivers the first renderer. `ImGuiRenderer` in `BrutalUi.ImGui` implements `IUiRenderer` on the ImGui background draw list, with a font service and a texture service. It depends on renderer-interface and gives label-and-image and the sample a path to the screen.

## Requirements

- `ImGuiRenderer` implements `IUiRenderer` and writes into `ImGui.GetBackgroundDrawList()` (Q2).
- `Rect` emits a filled rounded rect and a border through `ImDrawList`. `PushClip` and `PopClip` map to the draw list clip rect stack.
- `Text` and `MeasureText` use the ImGui dynamic font system with `PushFont(font, size)`. `MeasureText` supports a wrap width (A4).
- A font service maps a `FontHandle` to an `ImFontPtr`. It uses the ImGui default font loader. No FreeType pin (Q3).
- A texture service maps a `TextureHandle` to an `ImTextureRef` registered through `VulkanBackend.AddTexture`. Vulkan only (Q6).
- The renderer allocates nothing in `Begin`, the draw calls, or `End` after warm-up.
- A create-info struct with `required` members builds the renderer.
- `BrutalUi.ImGui` keeps the `Ahwoo.BRUTAL.ImGui` and `Ahwoo.BRUTAL.ImGui.Backends` references only (A9).
- `BrutalUi.ImGui` has no unit test that needs a GPU (A6). The sample in hello-panel-sample is the GPU check.

## Dependencies

- renderer-interface must be complete. It gives the interface and the handle structs.
- The ImGui context and the Vulkan backend of the game, as the HelloPanel sample creates them today.

## Out of Scope

- A private `ImDrawList` appended through `ImDrawDataBuilder`. The world-space epic owns it.
- The OpenGL backend texture registration.
- FreeType font loading.
- Draw callbacks for effects.
