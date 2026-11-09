# brutal-ui

## Mission Statement

**brutal-ui gives BRUTAL developers a retained-mode game UI framework with a good developer experience and zero allocations in the frame loop.**

BRUTAL ships an interop layer, native bindings, and a Dear ImGui binding for debug UI. It ships no game UI: no element tree, no layout engine, no styles, no markup, no data binding. brutal-ui fills that gap as a separate NuGet package. It brings the panel, mode, store, and window model from mosaic-ui, which ran on Unity UI Toolkit, to BRUTAL without a Unity dependency.

---

## Core Philosophy

### The frame loop allocates nothing

BRUTAL users chose BRUTAL to escape garbage collector stalls and hidden costs. A UI framework that allocates on every frame betrays that choice. brutal-ui runs layout, input, and draw with no managed allocation after warm-up. The API accepts spans, structs, and arena strings. We trade some API convenience for that rule, and we measure it in tests.

### Declare the screen, do not manage it

A game screen is a composition of named panels in named slots. The developer declares what a mode looks like. The framework diffs the current mode against the next one and handles the transitions. Panels shared across modes stay alive. This model worked in mosaic-ui and carries over unchanged.

### Build on what BRUTAL gives, replace nothing

BRUTAL already has an ImGui draw list, font atlas, texture upload, Vulkan and OpenGL backends, GLFW input, and localized strings. brutal-ui renders through those parts. It adds no native dependency. A game that already runs ImGui adds brutal-ui with one package reference.

---

## What We Build

### The system

Four layers, each a package:

1. **Core.** An element tree, a flexbox layout engine, a style system with classes and pseudo-states, pointer and keyboard input, focus, and a renderer interface.
2. **ImGui renderer.** The renderer interface on `ImDrawList`, with text through the ImGui font atlas and textures through the BRUTAL backends. Screen-space and render-to-texture paths.
3. **Framework.** Stores, panels, modes, layouts, windows, commands, an event bus, and a service registry. The mosaic-ui model.
4. **Markup.** A C# builder API and an XML format for element trees, plus a stylesheet format. Both in the first release.

### The target outcome

A developer writes a store, a panel controller, and an XML template. The developer registers a mode. The screen shows the panel, with data bound to the store, at no allocation per frame.

---

## Who This Is For

### Solo and small indie teams on BRUTAL

The primary user. These teams want a game UI without an engine and without a UI programmer on staff. They want a clear API, a markup format, and sane defaults.

### Teams that port from Unity

A second audience. They know UI Toolkit, UXML, and USS. brutal-ui keeps those concepts under new names so the port is a translation, not a redesign.

---

## What brutal-ui Is Not

- **Not an engine.** No scene graph, no editor, no asset pipeline. World-space UI is a texture that the game draws with its own renderer.
- **Not a replacement for ImGui debug UI.** ImGui stays the right tool for tools and debug panels. brutal-ui is for the player-facing UI.
- **Not a web browser.** The style system is a small subset of CSS that games need. No cascade across documents, no selectors beyond class, name, and pseudo-state, no CSS layout modes beyond flexbox.
- **Not part of BRUTAL.** The BRUTAL maintainers reject UI additions to the framework. brutal-ui is an independent package and an independent repository.

---

## Design Pillars

1. **Zero allocation in the frame loop.** Layout, input, and draw allocate nothing after warm-up. A test fails when they do.
2. **One tree, two renderers.** The element tree never references a renderer. The ImGui renderer is the first implementation. An own Vulkan renderer is possible later.
3. **Declarative composition.** Modes, panels, and slots are data. The framework computes the diff.
4. **Markup and code are equal.** Every element tree is buildable from C# and from XML, with the same result.
5. **BRUTAL conventions.** File-scoped namespaces, `required` create-info structs, `Brutal.Numerics` vector types, UTF-8 strings, no abstraction for its own sake.

---

## Technical Direction

### Render through ImDrawList

The first renderer walks the element tree and emits into an `ImDrawList`. The existing BRUTAL Vulkan and OpenGL backends draw it. This gives both graphics APIs, fonts, textures, and clip rects on day one. Effects beyond the draw list, like shadows and blur, come later through draw callbacks.

### A C# flexbox engine

The layout engine is a C# implementation of the Yoga subset that games use. Writing it in C# avoids a native dependency and keeps the allocation rule under our control.

### Quality through measurement

Each release runs an allocation test on layout and draw, and a benchmark on a 1000-element tree. The numbers go in the changelog.

### Scale, drag, and shadow

Public alpha feedback named these three: UI scaling for HiDPI displays and a player scale setting, in-game drag and drop for inventory-like screens, and shadows for depth. The window content-scale event already reaches the `UiContext.OnContentScale` slot. The plan gives it a consumer, scales the layout, the style lengths, and the fonts, and inverse-transforms the pointer. Drag and drop builds on the widget capture rule; window dragging ships with the window model, and the general form follows. Shadows come through the paint members and the draw callbacks that the section above defers.

---

*This document is a living guide. It evolves as the project develops, but the core mission does not change.*
