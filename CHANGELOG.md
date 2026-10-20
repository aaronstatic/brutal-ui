# Changelog

This file lists each notable change of brutal-ui. The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The element tree: elements with child lists, dirty tracking, class lists, and element state (core-tree/element-tree).
- The flexbox layout engine: a port of the Yoga algorithm with a layout cache and a measure callback (core-tree/flexbox-layout).
- The allocation gate and the benchmarks: `ThousandElementAllocationTests` asserts zero allocated bytes in the update of a 1000-element tree. `TreeLayoutBenchmark` and `PercentChainBenchmark` measure the layout. The `--facts` mode prints four facts lines (core-tree/allocation-guard).
- The renderer interface `IUiRenderer`, the value types `Rgba8`, `Corners`, `FontHandle`, and `TextureHandle`, the paint members of `ComputedStyle` (`BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`), `UiContext.Draw`, and the draw allocation gate (imgui-renderer/renderer-interface).
- `ImGuiRenderer`, `ImGuiFontService`, `ImGuiTextureService`, the size of `TextureHandle`, the `wrapWidth` parameter of `IUiRenderer.Text`, and the headless test project (imgui-renderer/imgui-draw-list).

### Benchmarks

One run of the default job on the machine of the author, on 2026-10-18, at commit `a8e7846` with the uncommitted allocation-guard work. No test asserts a time. A `-` in the `Allocated` column means 0 bytes. The settings panel is hidden, so the pass of `FullLayout` lays out 780 elements and sets the 220 elements of the panel to zero.

| Method                | Mean          | Error        | StdDev       | Allocated |
|---------------------- |--------------:|-------------:|-------------:|----------:|
| FullLayout            | 206,445.66 ns | 2,220.564 ns | 2,077.117 ns |         - |
| DirtySubtree          |   8,573.94 ns |   105.449 ns |    98.637 ns |         - |
| CleanUpdate           |      23.04 ns |     0.219 ns |     0.204 ns |         - |
| PaddingChainLeafMark  |      4.893 ms |    0.0567 ms |    0.0530 ms |         - |
| MaxWidthChainLeafMark |      3.722 ms |    0.0355 ms |    0.0314 ms |         - |

Machine line of BenchmarkDotNet:

```
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 (Resolute Raccoon)
Intel Core i7-6700K CPU 4.00GHz (Max: 0.80GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
```

Facts lines of the `--facts` mode:

```
facts: tree elements 1000, bytes 696944, bytes per element 696.9
facts: percent padding chain depth 20: 3031 callback calls for one leaf mark (budget 4000)
facts: percent max width chain depth 20: 5674 callback calls for one leaf mark (budget 7400)
facts: plain column chain on a 1 MB stack: 519 levels pass (Release build)
```

Commands that make these numbers, from the repository root:

```
dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'
dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts
```
