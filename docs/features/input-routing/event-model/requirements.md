# Event Model - Requirements

**Epic:** input-routing
**Created:** 2026-10-22

## Overview

This feature gives `BrutalUi.Core` the event structs, the handler delegates on `Element`, the event queue, and the dispatch walk. It is the first feature of the input-routing epic. Every later feature of the epic sends its events through this walk.

## Requirements

- An event is a struct. The pointer, scroll, key, and char kinds share one base with a type, a timestamp, and a target.
- `UiContext` holds a fixed ring buffer of events. A push after warm-up allocates 0 bytes. A full buffer drops the oldest event and counts the drop.
- `Element` exposes one delegate slot per event kind. Registration happens at build time. The walk calls a delegate with a `ref` event struct.
- The walk runs capture from the root to the target, then the target, then bubble from the target to the root.
- A handler can stop propagation. The walk then calls no later handler.
- The walk uses an explicit path buffer on `UiContext`, not recursion, and allocates 0 bytes after warm-up.
- A unit test drives the walk with hand-made events and no GLFW code.

## Dependencies

- core-tree: `Element`, `Element.Parent`, and `UiContext`.
- No sibling feature. This feature comes first.

## Out of Scope

- The hit test and the GLFW adapter. pointer-routing adds them.
- The focus owner. keyboard-focus adds it.
- Drop and content-scale events. They go to `UiContext` callbacks in pointer-routing, with no walk.
