# Gamepad Axes - Requirements

**Epic:** input
**Created:** 2026-11-07

## Overview

This feature extends the gamepad poll of `BrutalUi.Glfw` from the buttons to the full state: the buttons, six axes with a radial deadzone, and a connected flag, for each of the 16 GLFW slots. The poll writes the state into the gamepad part of `UiInputState`. Core gets the types, and `BrutalUi.Glfw` fills them.

## Requirements

- Core gets `UiGamepadState`: the `UiGamepadButtons` flags, six axes as `float` in -1 to 1, and a connected flag. Core names no GLFW type. (A2)
- `UiGamepadAxis` names the six GLFW axes: left stick X and Y, right stick X and Y, left trigger, and right trigger. The value of an axis is the GLFW value.
- `GlfwUiInput.PollGamepads()` reads each of the 16 slots one time and writes the gamepad part of `UiContext.Input`. The game calls it after `Glfw.PollEvents` and before `Update`. (A6)
- The stick axes apply a radial deadzone, with the default 0.15 and a setting on `GlfwUiInput`. A trigger applies a linear deadzone. The value outside the deadzone scales to 0 to 1.
- An absent joystick, a joystick with no gamepad mapping, and a disposed adapter give a state with the connected flag `false` and no button or axis. No hot-plug event. (Q3)
- `PollGamepad(int)` stays and returns the buttons of one slot, as today. `UiActionMap.PushGamepad` stays.
- The poll allocates nothing. The allocation gate covers it. (A3)
- Tests in `tests/BrutalUi.Core.Tests/` cover the deadzone math on a hand-made state. A Glfw test covers the absent slot. HelloPanel shows the left stick in a label. (A9)

## Dependencies

- input-state is complete: the gamepad slots of `UiInputState`.
- `Ahwoo.Brutal.Glfw` pinned in `Directory.Packages.props`.

## Out of Scope

- Rumble and a hot-plug event. (Q3, A8)
- A binding of an axis to an action. input-actions adds it.
- A background poll thread. (A6)
- An SDL adapter. (A8)
