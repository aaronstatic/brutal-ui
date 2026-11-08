# Binding Remap - Requirements

**Epic:** input
**Created:** 2026-11-07

## Overview

This feature lets a game rebind an action at run time and store the result as text. The map listens for the next input, replaces the binding, reports a conflict, and restores the defaults. Framework writes and parses the bindings as text lines. The game owns the file. It closes the epic. (Q5)

## Requirements

- `BeginRebind(action, scheme)` puts the map in a listen state. The next input of that scheme above the deadzone becomes the new binding, and the map leaves the listen state. `CancelRebind` leaves it with no change. Escape cancels by default, and a game can change the cancel key.
- During the listen state, the map gives no action value and invokes no command.
- A rebind that matches a binding of another action in the same scheme reports a conflict through a slot, with both action names. The game decides. The default keeps both.
- `ResetBindings` restores the bindings of the build. The map keeps a copy of the defaults from the build.
- `WriteBindings(Span<byte>)` writes one binding per line: the action, the scheme, the device, the code, and the modifiers. It returns the byte count, and a span that is too short returns the needed count.
- `ReadBindings(ReadOnlySpan<byte>)` parses the lines and replaces the bindings of each named action. An unknown action or a bad line skips with a count of the skipped lines. The format has a version line.
- The write and the parse run at load time and may allocate. The listen state and the conflict check in `Update` allocate nothing. (A3)
- Tests in `tests/BrutalUi.Framework.Tests/` cover the listen state, the cancel, the conflict, the reset, and a write and read round trip. HelloPanel rebinds one key through a button. Section 9 of `technical-design.md` gets the format. (A9)

## Dependencies

- input-actions is complete: the binding list.
- control-schemes is complete: the scheme of a rebind.

## Out of Scope

- File IO. The game reads and writes the file. (Q5)
- A rebind screen. The epic gives the API.
- An XML form of the bindings. The xml-markup epic can add one later.
- A rebind of a composite part by part. A composite rebinds as one binding.
