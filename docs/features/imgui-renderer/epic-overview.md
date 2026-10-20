# ImGui Renderer - Epic Overview

**Epic:** imgui-renderer
**Status:** Planned
**Last Updated:** 2026-10-20 10:00

> **This file is the epic marker.** Its presence in `docs/features/imgui-renderer/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic puts the element tree on screen. It owns the `IUiRenderer` interface and the draw pass in `BrutalUi.Core`, and the first renderer in `BrutalUi.ImGui`, which writes into the ImGui background draw list. It adds the `Label` and `Image` elements and turns the HelloPanel sample into a static brutal-ui screen. The features belong together because each one turns a part of the tree walk into pixels, and the sample proves the full path from `Element` to `ImDrawList`.

The epic completes Phase 1 of the technical design. The dev-ops epic gave the skeleton and the core-tree epic gave the tree. After this epic, the style-system and input-routing epics of Phase 2 have a screen to show their results on.

---

## Features

The constituent features of this epic, in build order. Each feature is a subfolder under `docs/features/imgui-renderer/`.

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | renderer-interface | Planned | - | `IUiRenderer`, the handle and color structs, the paint fields on `ComputedStyle`, and `UiContext.Draw` with the clip and rect walk, tested with a recording renderer. |
| 2 | imgui-draw-list | Planned | - | `ImGuiRenderer` on the background `ImDrawList`, the font service on the ImGui default loader, and the texture service on the Vulkan backend. |
| 3 | label-and-image | Planned | - | The `Label` and `Image` elements, the measure callback caller, word wrap, and the `LString` hook. |
| 4 | hello-panel-sample | Planned | - | The HelloPanel sample as a static brutal-ui screen with a procedural texture, the draw allocation test, and the test script check. |

> Reference any feature with the slash form `imgui-renderer/<feature-name>`. Task counts come from each feature's own `tasks.md` and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **renderer-interface**. Every other feature calls the interface or the draw walk. It comes first. The recording renderer in `BrutalUi.Core.Tests` lets the walk ship with tests and no GPU.
2. **imgui-draw-list**. The first implementation of the interface. It depends on feature 1 for the interface and the handles. It ships `MeasureText` so feature 3 only calls it (A4).
3. **label-and-image**. The first two element types. It depends on feature 1 for `MeasureText` in the interface and on feature 2 to show text and textures on screen. It holds the hardest part of the epic, text measure and wrap through the ImGui dynamic font system (A4).
4. **hello-panel-sample**. The sample uses every feature of the epic. It comes last and closes Phase 1.

**Dependencies between features:**
- renderer-interface → imgui-draw-list (the interface, `Rgba8`, `Corners`, `FontHandle`, `TextureHandle`)
- renderer-interface → label-and-image (`MeasureText` and the paint walk)
- imgui-draw-list → label-and-image (text and textures on screen)
- label-and-image → hello-panel-sample (the label and the image in the sample)
- The four features build in series. No pair runs in parallel (A1).
- External: the dev-ops epic. The `BrutalUi.Core`, `BrutalUi.ImGui`, and HelloPanel projects, the pins, and both test scripts exist.
- External: the core-tree epic. `Element`, `UiContext.Update`, the dirty flags, the layout rects, and the measure callback slot exist.

**Confirmed interview items that shape the build order:**
- A1 Build order in series: renderer-interface, imgui-draw-list, label-and-image, hello-panel-sample. No pair runs in parallel.
- A3 No feature of `core-tree` or `dev-ops` moves into this epic.
- A4 The hardest part is text measure and wrap through the ImGui dynamic font system. It lands in label-and-image. imgui-draw-list ships `MeasureText` first so label-and-image only calls it.

---

## Integration & Architecture

- **Within the epic:** `UiContext.Draw(IUiRenderer)` walks the tree in paint order and reads the layout rect and the paint fields of each element. `ImGuiRenderer` turns each call into an `ImDrawList` call. `Label` and `Image` set the measure callback slot of `LayoutNode` and read `IUiRenderer.MeasureText` or the texture size. The sample builds the tree, calls `Update` and `Draw` in the frame loop, and lets the Vulkan backend render the ImGui frame.
- **With other epics / features:** The style-system epic writes the paint fields from the cascade. The input epic reads the same layout rects for hit testing. The world-space epic adds a private `ImDrawList` and the render-to-texture path on the same interface.
- **Key architectural decisions for the epic as a whole:**
  - `BrutalUi.Core` keeps no ImGui reference. The interface takes spans and structs only.
  - `ImGuiRenderer` writes into `ImGui.GetBackgroundDrawList()` in this epic (Q2).
  - The ImGui default font loader. No FreeType pin (Q3).
  - The texture service registers with the Vulkan backend only (Q6).
  - No managed allocation in `UiContext.Draw` after warm-up. The allocation test guards it.

---

## Tech Debt / Findings

- (none yet: `/review-epic` will surface cross-feature integration issues and per-feature tech debt here)

---

## Master Overview Rollup

- **Rollup status:** Planned (0/4 features)
- **One-line summary for master:** `IUiRenderer`, `UiContext.Draw`, the ImGui renderer, `Label`, `Image`, and the HelloPanel static screen. Closes Phase 1.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic imgui-renderer` after work on the features, and run `/review-epic imgui-renderer` to refresh the Tech Debt / Findings section.*
