# Label And Image - Requirements

**Epic:** imgui-renderer
**Created:** 2026-10-20

## Overview

This feature adds the first two element types. `Label` shows UTF-8 text and `Image` shows a texture. Both set the measure callback slot of `LayoutNode`, so the layout pass sizes them. It holds the hardest part of the epic, text measure and wrap (A4).

## Requirements

- `Label` is an element that holds UTF-8 text, a font handle, a size, and a color. It draws through `IUiRenderer.Text`.
- `Label` sets the measure callback and measures through `IUiRenderer.MeasureText`. It does word wrap at the available width (A5).
- `Label` accepts an `LString` from `Brutal.Localization` and re-measures when the locale changes (A7).
- A text change marks the layout and paint dirty bits.
- `Image` is an element that holds a texture handle, a UV rect, a tint, and a scale mode. It draws through `IUiRenderer.Image`.
- `Image` sets the measure callback from the texture size.
- The measure callbacks allocate nothing and follow the contract in `MeasureCallback.cs`.
- `UiContext` gets a measurer slot, set at creation. The `Label` measure callback reads it during `Update` to call `MeasureText` (answer of 2026-10-20).
- `Image` reads its natural size from `TextureHandle.Size`, which imgui-draw-list adds (answer of 2026-10-20).
- A wrapped `Label` passes the same wrap width to `MeasureText` and to `IUiRenderer.Text` (answer of 2026-10-20).
- `BrutalUi.Core.Tests` tests both elements with the recording renderer: the measure result, the wrap, and the draw call.
- All code uses `Brutal.Numerics` types, UTF-8 text, file-scoped namespaces, nullable on, and XML doc comments on public members.

## Dependencies

- renderer-interface must be complete. It gives `MeasureText` and the paint walk.
- imgui-draw-list must be complete to show text and textures on screen.

## Out of Scope

- Rich text, kerning control, and bidirectional text (A5).
- Text selection and editing. The input epic and `TextField` own them.
- `Button`, `Toggle`, `ScrollView`, and the other interactive elements.
- Image loading from a file. The texture service accepts a registered texture only.
