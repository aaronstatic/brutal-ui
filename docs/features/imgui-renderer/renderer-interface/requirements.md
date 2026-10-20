# Renderer Interface - Requirements

**Epic:** imgui-renderer
**Created:** 2026-10-20

## Overview

This feature defines how the element tree turns into draw calls. It owns the `IUiRenderer` interface, the value types it uses, the paint fields on `ComputedStyle`, and `UiContext.Draw`. It comes first in the imgui-renderer epic because every other feature calls the interface or the draw walk.

## Requirements

- `BrutalUi.Core` defines `IUiRenderer` with `Begin`, `PushClip`, `PopClip`, `Rect`, `Image`, `Text`, `MeasureText`, and `End`, as in section 8 of the technical design. The methods take spans and structs only. No strings, no arrays.
- `BrutalUi.Core` defines `Rgba8`, `Corners`, `FontHandle`, and `TextureHandle` as structs.
- `ComputedStyle` gains these paint fields: background color, border color, corner radius, opacity, and overflow clip. Code sets them inline in this epic (A2).
- `UiContext.Draw(IUiRenderer)` walks the roots in paint order. It pushes a clip rect for an element with overflow hidden. It emits one `Rect` for each element with a visible background or border. A hidden element draws nothing.
- The walk allocates nothing after warm-up.
- `BrutalUi.Core.Tests` tests the walk with a recording renderer that stores each call. The tests cover the paint order, the clip push and pop, and the skip of a hidden element.
- The draw allocation test extends `ThousandElementAllocationTests` with the recording renderer and asserts zero bytes for `Draw` on a warm tree (A6).
- All code uses `Brutal.Numerics` types, file-scoped namespaces, nullable on, and XML doc comments on public members.
- `BrutalUi.Core` keeps no ImGui reference (A9).

## Dependencies

- The core-tree epic: `Element`, `UiContext`, the dirty flags, and the layout rects.
- No sibling feature. This feature builds first.

## Out of Scope

- The ImGui implementation. imgui-draw-list owns it.
- `Label` and `Image`. label-and-image owns them.
- The style cascade. The style-system epic writes the paint fields later.
- Shadows, blur, gradients, and an `Effect` command.
- A dirty-paint cull. The walk draws the whole tree each frame in this epic.
