# Changelog

This file lists each notable change of brutal-ui. The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The element tree: elements with child lists, dirty tracking, class lists, and element state (core-tree/element-tree).
- The flexbox layout engine: a port of the Yoga algorithm with a layout cache and a measure callback (core-tree/flexbox-layout).
- The allocation gate and the benchmarks: `ThousandElementAllocationTests` asserts zero allocated bytes in the update of a 1000-element tree. `TreeLayoutBenchmark` and `PercentChainBenchmark` measure the layout. The `--facts` mode prints four facts lines (core-tree/allocation-guard).
- The renderer interface `IUiRenderer`, the value types `Rgba8`, `Corners`, `FontHandle`, and `TextureHandle`, the paint members of `ComputedStyle` (`BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`), `UiContext.Draw`, and the draw allocation gate (imgui-renderer/renderer-interface).
- `ImGuiRenderer`, `ImGuiFontService`, `ImGuiTextureService`, the size of `TextureHandle`, the `wrapWidth` parameter of `IUiRenderer.Text`, and the headless test project (imgui-renderer/imgui-draw-list).
- `Label`, `Image`, `ImageScaleMode`, the measurer slot of `UiContext` (`UiContext.CreateInfo` and `UiContext.Measurer`), `UiContext.RefreshLocalizedText`, the content insets of the layout pass, and the element allocation gate `ElementAllocationTests` (imgui-renderer/label-and-image).
- The HelloPanel sample shows a static brutal-ui screen with a panel, a label, and a procedural image, with the sample allocation gate and `TreeDrawBenchmark` (imgui-renderer/hello-panel-sample).
- `StyleProperties`, `Selector`, `StyleRule`, `StyleSheet`, `InlineStyle` (`Element.Inline`), `ElementTypeId`, `ElementType`, `UiContext.AddSheet`, `UiContext.RemoveSheet`, the style pass with its cascade and text inheritance, and the text members `Color`, `Font`, and `FontSize` of `ComputedStyle` with the text defaults of `UiContext.CreateInfo` (style-system/stylesheet-model).
- `CompoundSelector`, `SelectorCombinator`, `Selector.For`, `Selector.Child`, `Selector.Descendant`, `Selector.ForName`, and the getters `Subject`, `Ancestor`, and `Combinator`. A rule now matches the pseudo-states, the name (ordinal), a child chain (`.a > .b`), and a descendant chain (`.a .b`). The descendant chain reads the parent and the grandparent only (style-system/selector-matching).
- `StyleSheetParser`, `StyleParseResult`, `StyleParseError`, and `StyleErrorKind` in `BrutalUi.Markup`. They read the stylesheet text format with theme variables (`:root` and `var()`) and the transition declaration. Each error has a position. `TransitionEasing`, `StyleTransition`, `StyleTransitions`, the `StyleRule.Transitions` field with a new `StyleRule` constructor, and a new `StyleSheet.Builder.Add` overload. The equality members (`IEquatable<T>`, `==`, `!=`, `GetHashCode`) and the CSS text form (`ToString`) of `CompoundSelector` and `Selector` (style-system/style-parser).
- The transitions of the five paint members `BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`, and `Color` in `UiContext.Update`. A rule with a transition fades a member from its visible value to its target over the duration with the CSS easing curves, and each tween advances by `deltaTime`. A `Color` tween also reaches each descendant that inherits the color. The first style of an element starts no tween. The transition pass marks `Paint` only and allocates nothing after warm-up. The new types and members are internal (style-system/transitions).
- The event structs (`UiEventHeader`, `PointerEvent`, `ScrollEvent`, `KeyEvent`, `CharEvent`), the 14 handler slots of `Element`, the event queue of `UiContext` (`PushEvent`, `EventQueueCapacity`, `DroppedEventCount`), and the capture, target, and bubble walk with `StopPropagation` in `BrutalUi.Core` (input-routing/event-model).
- The hit test with clip rects and `IsPickable`, the `Hover` and `Active` states, the scroll routing, the window callbacks `UiContext.OnFileDrop` and `UiContext.OnContentScale`, and the `BrutalUi.Glfw` package with `GlfwUiInput` (input-routing/pointer-routing).
- The focus owner (`UiContext.FocusedElement`, `SetFocus`, `ClearFocus`, `FocusNext`, `FocusPrevious`), `Element.IsFocusable`, focus on click, Tab, and Shift+Tab in tree order, the `Focus` state, the routing of key and char events to the focus owner, and the key and char events of `GlfwUiInput` with `KeyboardEnabled` (input-routing/keyboard-focus).
- `UiRoutingGate` (`PointerOverUi` and `KeyboardFocusInUi`), `UiContext.RoutingGate`, the allocation case of a full event queue in `ThousandElementAllocationTests`, and the input demo of the HelloPanel sample with its gate counts and its input check (input-routing/routing-gate).
- `Button` (with `Button.CreateInfo`, `Button.Clicked`, `Button.SetText`, and a text at the top-left corner of the content box) and `Toggle` (with `Toggle.CreateInfo`, `Toggle.Checked`, and `Toggle.CheckedChanged`). A left click, and an Enter or Space key on a button or a Space key on a toggle, call the sender delegate. The `:disabled` state blocks user input. `UiContext.RefreshLocalizedText` now knows `Button`. The widget allocation gate `WidgetAllocationTests` asserts zero allocated bytes for `Update`, `Draw`, and the widget input path. A `Button` measures its text, so its context needs a measurer unless its size is fixed (interactive-elements/button-and-toggle).
- `Slider` (with `Slider.CreateInfo`, `Slider.Value`, `Slider.Minimum`, `Slider.Maximum`, `Slider.Step`, `Slider.SetRange`, and `Slider.ValueChanged`). A left press, a drag, and the Left and Right keys move the value, and the value follows the range and the step. The box of the slider is the track, and the thumb draws in the `color` of the style. The `:disabled` state blocks user input. The slider allocation gate `SliderAllocationTests` asserts zero allocated bytes for `Update`, `Draw`, and the slider input path. A `Slider` has no own size, so a style rule or `Element.Inline` must give it one (interactive-elements/slider).
- `ScrollView` (with `ScrollView.CreateInfo`, `ScrollView.ScrollOffset`, `ScrollView.ScrollSpeed`, and `ScrollView.ScrollChanged`). A view clips its children and moves them by a scroll offset. The wheel over the view or over a child scrolls it, and a left press and a drag on the view itself scroll it too. The offset follows the content size and the clamp at use. The `:disabled` state blocks user input. The scroll view allocation gate `ScrollViewAllocationTests` asserts zero allocated bytes for `Update`, `Draw`, and the scroll view input path. A `ScrollView` has no own size, so a style rule or `Element.Inline` must give it one (interactive-elements/scroll-view).
- `TextField` (with `TextField.CreateInfo`, `TextField.Capacity`, `TextField.Text`, `TextField.GetText`, `TextField.SetText`, `TextField.TextChanged`, and `TextField.Submitted`). A field holds one line of UTF-8 text in a fixed buffer. A char inserts at the caret, the Left, Right, Home, and End keys move it, and `Shift` selects. Backspace and Delete remove the selection or one code point. A char replaces the selection. A left press places the caret, a drag selects, and Enter calls `Submitted`. Long text scrolls under the caret, and the caret blinks. The `:disabled` state blocks user input. The text field allocation gate `TextFieldAllocationTests` asserts zero allocated bytes for `Update`, `Draw`, and the text field input path. A `TextField` has no own size, so a style rule or `Element.Inline` must give it one (interactive-elements/text-field).
- Each widget in the HelloPanel sample: the `Click me` button with a disabled button, a toggle, a slider, a scroll view with 8 items, and a text field, each with a status label (`HelloWidgets`). The 31 widget rules of `HelloScreen.css` style them with a fill for hover and active, a border for focus, and a size for the toggle, the slider, the scroll view, and the text field. `HelloPanelSheetTests` (10 tests) checks that each rule reaches its widget. The thirteenth allocation gate, `ThousandWidgetAllocationTests` (4 tests), asserts zero allocated bytes for `Update`, `Draw`, and the input path on the 1000-element tree with each widget type (`ThousandWidgetTree`). `ThousandElementTree.Build` has an overload that takes a renderer. The runbooks `docs/testing-linux.md` and `docs/testing-windows.md` hold a widget checklist (interactive-elements/widget-sample).
- `IBindingSource` in Core, `Store<T>` and `StoreSubscription<T, TValue>` in Framework, and the Framework allocation gate (state-and-commands/store-and-subscription).
- The binding pass in `UiContext.Update` (after the input pass and before the style pass), `Element.AddBinding`, `Element.ClearBindings`, `BindingHandler`, `LabelBindings.BindText` (a number slice with the span formatter, and a string slice), `WidgetBindings` (`BindChecked`, `BindValue`, and `BindText` for a `TextField`), and the bound-tree allocation gate `BoundTreeAllocationTests`, the fifteenth gate. A store change now reaches the bound elements in the same `Update` (state-and-commands/element-bindings).
- `CommandRegistry`, `CommandHandler`, `UnknownCommandHandler`, `CommandBindings.BindCommand`, `EventBus`, `EventBusHandler<T>`, `ServiceRegistry`, and the command and event allocation gate `CommandAndEventAllocationTests`, the sixteenth gate. A command maps a UTF-8 name to a static handler and a target. A click on a bound button invokes the command, and an unknown name calls the `UnknownCommand` slot (state-and-commands/commands-and-events).
- `UiContext.OnUnhandledInput`, `UiUnhandledInputHandler`, `UiGamepadButtons`, `UiActionMap`, `UiActions`, `FocusCommands`, `GlfwUiInput.PollGamepad`, the HelloPanel state row, and the action map allocation gate (state-and-commands/action-map).
- `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `ModePanel`, `PanelContext`, `PanelInstance`, and `PanelController` (panels-and-modes/panel-definition).
- `SlotContainer` with `Mount`, `Unmount`, and `SlotContainer.Find` (panels-and-modes/slot-container).
- `UiContext.OnBeforeUpdate` and `UiBeforeUpdateHandler`, the slot that `Update` calls first (panels-and-modes/mode-set).
- `UiManager` with `Register`, `SetMode`, the deferred mode diff, the layout swap, and the action map switch, and `ModeHistory` with `Back` (panels-and-modes/mode-set).
- HelloPanel shows two modes with the layout `hello.layout`, the panels `hello.widgets`, `hello.state`, and `hello.info`, a `Mode (F1)` button, F1 for the switch, and F2 for the back step (panels-and-modes/panel-sample).
- The HelloPanel smoke run switches the mode in each frame and prints a seventh line with the mode counts (panels-and-modes/panel-sample).
- `ModeDiffAllocationTests`, the eighteenth allocation gate (panels-and-modes/panel-sample).
- `UiInputState`, `UiContext.Input`, and `UiContext.GameInput`, the per-frame input state and its gated view (input/input-state).
- `UiContext.ClearHeldKeys`, the request that clears the held keys at the end of the next drain (input/input-state).
- `UiGamepadState` and `UiContext.SetGamepad`, the gamepad slots and their write seam (input/input-state).
- `UiGamepadAxis` and `UiGamepadDeadzone`, the six axes and the deadzone helper (input/gamepad-axes).
- `UiGamepadState.Axis(UiGamepadAxis)`, `LeftStick`, and `RightStick` (input/gamepad-axes).
- `GlfwUiInput.PollGamepads`, `StickDeadzone`, and `TriggerDeadzone`, the poll of the 16 gamepad slots with the deadzones (input/gamepad-axes).
- `UiContext.OnInputRead` and `UiInputReadHandler`, the read slot that `Update` calls after the input pass and before the binding pass (input/input-actions).
- `UiActionType`, `UiGamepadStick`, and the typed `UiActionMap.AddAction(action, type)`, the three action types and the action with no command (input/input-actions).
- `UiActionMap.BindKeyVector`, `BindGamepadStick`, `BindGamepadAxis`, `BindKeyAxis`, `BindMouseDelta`, and `BindScroll`, the six new binds of the typed map (input/input-actions).
- `UiActionMap.GamepadSlot`, the gamepad slot that the frame read of an active map reads (input/input-actions).
- `UiActionMap.ReadVector`, `ReadAxis`, `WasPressed`, `IsHeld`, and `WasReleased`, the five queries of the typed map (input/input-actions).
- `UiScheme`, `UiSchemeChangedHandler`, `UiActionMap.AddScheme`, `SchemeName`, `ActiveScheme`, `LockedScheme`, `OnSchemeChanged`, and `SetScheme`, the named control schemes with the active device, its change slot, and its lock. Each of the nine binds takes one scheme overload, and the duplicate check is per scheme, so one input binds in two schemes. HelloPanel shows the active scheme in a sixth label (input/control-schemes).
- `UiActionMap.BeginRebind`, `CancelRebind`, `IsRebinding`, `RebindCancelKey`, `OnRebindResult`, `OnRebindConflict`, the three result codes `RebindBound`, `RebindCancelled`, and `RebindRejected`, `ResetBindings`, `WriteBindings`, `ReadBindings`, and `UiScheme.Default`. A listen takes the next fitting input and replaces the bindings of one action in one scheme. A conflicting rebind keeps both bindings, and `OnRebindConflict` names both actions. A listen frame writes no value and runs no command, and the device track keeps running. `ResetBindings` gives back the bindings of the build, from the snapshot of the first `Activate`. `WriteBindings` and `ReadBindings` ship the binding text format: one version line, one line per binding, the nine `device` numbers, and the merge and skip rules. HelloPanel rebinds the `Up` key through a `Rebind (Up)` button, and restores the build through a `Restore` button (input/binding-remap).

### Changed

- A locked `UiActionMap` ends a held binding of another scheme and reports one release for it in that frame, even though the player never released the input. A press of another scheme under a lock invokes no command and is not kept for the clear of the lock (input/control-schemes).
- An active `UiActionMap` reads the gamepad buttons of `GamepadSlot` in `UiContext.Update` and invokes their commands. A game drops its `PushGamepad` call for an active map, because the push would invoke each press a second time (input/input-actions).
- `BindKey`, `BindMouseButton`, and `BindGamepadButton` throw `ArgumentException` for an Axis or Vector2 action. Each bind needs an action of the type that its input fills (input/input-actions).
- The HelloPanel sample shows the `Move` vector in a fifth label, and the frame loop no longer pushes the gamepad. The `Move` action reads W, A, S, D and the left stick of slot 0 (input/input-actions).
- A `KeyAxis` pair and a `KeyVector` key set bound in any order is one binding, and a repeat throws. `BindKey` and `BindMouseButton` reject codes above the state range at bind time (input/input-actions).
- The slots of `UiInputState` hold the values after the deadzone when `PollGamepads` writes them, and a trigger holds 0 to 1 (input/gamepad-axes).
- `GlfwUiInput` subscribes `GlfwWindow.OnFocus` and clears the held keys on a focus loss. A `KeyboardEnabled` switch to `false` clears the held keys (input/input-state).
- `HelloScreen.Build` splits into four build methods, `UiManager` adds the root of the sample, and the class `hello-root` becomes `hello-slot-root` (panels-and-modes/panel-sample).
- The remark of `PanelDefinition.DefaultSlot` states that the manager does not read it (panels-and-modes/mode-set).
- The HelloPanel smoke run now drives each widget in each frame and prints a fifth line, `HelloPanel: widgets: clicks <C>, flips <T>, values <V>, scrolls <S>, edits <E>, submits <U>.` It exits with code 1 and writes `HelloPanel: the synthetic input did not reach each widget.` when a widget gets no change. The last frame now has 7 draw commands and 1100 vertices (interactive-elements/widget-sample).
- A chord now gives no click: a click needs the left press that set `Active`, with no other button down during the gesture, and a release of the left button that ends the press. A `PointerUp` with a target from the caller gives no click (CR-1 and CR-6 of button-and-toggle). `UiContext.CurrentPressTarget` holds the press target for one walk (interactive-elements/slider).
- A pointer move with no target now walks to a dragged `Slider` while it holds the left press, also outside its rect. A hidden slider captures no move. `Hover`, `PointerHit`, and the routing gate still follow the hit (interactive-elements/slider).
- The hit walk and the draw walk now move the children of a `ScrollView` by its offset. `WidgetInput.ViewportOrigin` subtracts the offset of each ancestor `ScrollView`. A tree with no scroll view does not change. A pointer move with no target now also walks to a dragged `ScrollView` while it holds the left press (interactive-elements/scroll-view).
- `UiContext.Update` now runs the caret pass for a focused `TextField`, after the layout pass and before the clear pass. The pass advances the blink, maps a press and a drag, and writes the text scroll. Each other focus owner costs one type test. A focus gain of a `TextField` restarts its blink. A left press on a `TextField` and each drag move now stop the walk, and a pointer move with no target now walks to a dragged `TextField` while it holds the left press. The main Enter key on a focused `TextField` no longer reaches an ancestor (interactive-elements/text-field).
- A `PointerUp` handler now reads `Active` on the pressed element. The release clears `Active` after its walk, and the `finally` block of the drain clears it too. The states after the drain do not change (interactive-elements/button-and-toggle).
- `Label` now uses an internal shared text type (`Utf8Text`). The behavior does not change (interactive-elements/button-and-toggle).
- A font or font size change measures a `Button` again, as it does a `Label` (interactive-elements/button-and-toggle).
- `Element.Style` is `ref readonly`, and the game writes `Element.Inline`. `Label.Color`, `Label.Font`, and `Label.Size`, and the matching members of `Label.CreateInfo`, go: the label reads its text style from `Element.Style` (style-system/stylesheet-model).
- `AddRoot` now marks the new root and each descendant with `Style` and `DescendantStyle`. An attach (`Children.Add` and `Children.Insert`) marks the moved element and each descendant with `Style`, and each descendant with a child also gets `DescendantStyle`. The expected `Dirty` of `DirtyTrackingTests.AddRootOfAChildMarksTheOldParentForLayout` changed for this reason (style-system/stylesheet-model).
- A label with no text value now draws in the default color of the context (opaque white). The old `Label.CreateInfo.Color` was `required`, and a game had to give a color (style-system/stylesheet-model).
- `Selector.Specificity` uses one byte for each tier: `(names << 16) | ((classes + states) << 8) | types`. A class rule gives 256 in place of 10, and a type and class rule gives 257 in place of 11. The order of the existing rules does not change. `ClassList.Add`, `ClassList.Remove`, and a new `Element.States` value mark the full subtree with `Style`. The old mark was on the element only. A new value of `Element.Name` now marks the element and its full subtree with `Style`. The old name setter marked nothing (style-system/selector-matching).
- The HelloPanel sample loads its sizes and colors from `HelloScreen.css` through `StyleSheetParser`, in place of inline values. A bad sheet gives exit code 1 before the window opens (style-system/style-parser).
- `UiContext.Update` now reads `deltaTime`: each running tween advances by this time, and a value of 0 or less advances no tween. A rule with a transition no longer gives its target in the same `Update`: the member reaches the target after the duration. A test that reads a target after `Update(0f)` on a sheet with transitions must pass a time of at least the duration (style-system/transitions).
- `UiContext.Update` drains the event ring before the style pass, and a handler that calls `Update` gets `InvalidOperationException` (input-routing/routing-gate).

### Fixed

- The zero-byte gates of `BrutalUi.Core.Tests` release the allocation context before each measure, so a background GC no longer fails them (imgui-renderer/hello-panel-sample).

### Benchmarks

One run of the default job on the machine of the author, on 2026-10-21, at commit `6e2b5a2` with the uncommitted hello-panel-sample work. No test asserts a time. A `-` in the `Allocated` column means 0 bytes. The settings panel is hidden, so the pass of `FullLayout` lays out 780 elements and sets the 220 elements of the panel to zero. `Draw` measures the draw walk of the 1000-element tree into a recording renderer. Its time holds the walk and the cost of the recording.

| Method                | Mean          | Error        | StdDev       | Allocated |
|---------------------- |--------------:|-------------:|-------------:|----------:|
| FullLayout            | 216,420.29 ns | 3,058.270 ns | 3,521.908 ns |         - |
| DirtySubtree          |  10,814.32 ns |   860.558 ns | 2,455.220 ns |         - |
| CleanUpdate           |      36.19 ns |     0.954 ns |     2.707 ns |         - |
| PaddingChainLeafMark  |      4.919 ms |    0.0948 ms |    0.0886 ms |         - |
| MaxWidthChainLeafMark |      3.812 ms |    0.0519 ms |    0.0460 ms |         - |
| Draw                  |      20.64 us |     0.158 us |     0.140 us |         - |

Machine line of BenchmarkDotNet:

```
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 (Resolute Raccoon)
Intel Core i7-6700K CPU 4.00GHz (Max: 0.80GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.113
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
```

Facts lines of the `--facts` mode:

```
facts: tree elements 1000, bytes 746528, bytes per element 746.5
facts: percent padding chain depth 20: 3031 callback calls for one leaf mark (budget 4000)
facts: percent max width chain depth 20: 5674 callback calls for one leaf mark (budget 7400)
facts: plain column chain on a 1 MB stack: 507 levels pass (Release build)
```

HelloPanel smoke run (120 frames): Update and Draw 0 bytes in 117 frames after warm-up.

Commands that make these numbers, from the repository root:

```
dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'
dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts
env -u WAYLAND_DISPLAY -u VK_DRIVER_FILES -u VK_ADD_DRIVER_FILES XDG_SESSION_TYPE=x11 VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 120
```
