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

### Changed

- `Element.Style` is `ref readonly`, and the game writes `Element.Inline`. `Label.Color`, `Label.Font`, and `Label.Size`, and the matching members of `Label.CreateInfo`, go: the label reads its text style from `Element.Style` (style-system/stylesheet-model).
- `AddRoot` now marks the new root and each descendant with `Style` and `DescendantStyle`. An attach (`Children.Add` and `Children.Insert`) marks the moved element and each descendant with `Style`, and each descendant with a child also gets `DescendantStyle`. The expected `Dirty` of `DirtyTrackingTests.AddRootOfAChildMarksTheOldParentForLayout` changed for this reason (style-system/stylesheet-model).
- A label with no text value now draws in the default color of the context (opaque white). The old `Label.CreateInfo.Color` was `required`, and a game had to give a color (style-system/stylesheet-model).
- `Selector.Specificity` uses one byte for each tier: `(names << 16) | ((classes + states) << 8) | types`. A class rule gives 256 in place of 10, and a type and class rule gives 257 in place of 11. The order of the existing rules does not change. `ClassList.Add`, `ClassList.Remove`, and a new `Element.States` value mark the full subtree with `Style`. The old mark was on the element only. A new value of `Element.Name` now marks the element and its full subtree with `Style`. The old name setter marked nothing (style-system/selector-matching).
- The HelloPanel sample loads its sizes and colors from `HelloScreen.css` through `StyleSheetParser`, in place of inline values. A bad sheet gives exit code 1 before the window opens (style-system/style-parser).
- `UiContext.Update` now reads `deltaTime`: each running tween advances by this time, and a value of 0 or less advances no tween. A rule with a transition no longer gives its target in the same `Update`: the member reaches the target after the duration. A test that reads a target after `Update(0f)` on a sheet with transitions must pass a time of at least the duration (style-system/transitions).

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
