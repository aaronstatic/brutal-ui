# Keyboard Focus - Requirements

**Epic:** input-routing
**Created:** 2026-10-22

## Overview

This feature adds the focus owner and the keyboard path. It routes the key and char events of `GlfwWindow` to the focused element and moves focus on click, Tab, and Shift+Tab. It is the third feature of the input-routing epic.

## Requirements

- `UiContext` holds one focus owner. Code can set it, clear it, and read it.
- An element declares that it accepts focus. The default is no focus.
- A click on an element that accepts focus moves the focus to it. A click on no such element clears the focus.
- Tab moves the focus to the next element that accepts focus, in tree order. Shift+Tab moves it to the previous one. The move wraps at the ends.
- The focus owner gets `PseudoStates.Focus`. A focus change clears it on the last owner and sets it on the new one.
- The adapter in `BrutalUi.Glfw` pushes the key and char events into the queue. The walk sends them to the focus owner with capture and bubble.
- A focus move after warm-up allocates 0 bytes. The Tab search uses no recursion and no list.

## Dependencies

- event-model must be complete: the queue and the walk.
- pointer-routing must be complete: the click target for click focus and the adapter project.

## Out of Scope

- Arrow-key and spatial navigation. The Phase 3 action map adds it.
- Gamepad navigation. This feature leaves a slot and adds no binding.
- Text editing. `TextField` in the interactive-elements epic consumes the char events.
- The routing gate. routing-gate adds it.
