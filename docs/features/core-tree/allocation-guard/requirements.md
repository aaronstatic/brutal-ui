# Allocation Guard - Requirements

**Epic:** core-tree
**Created:** 2026-10-16

## Overview

This feature delivers the test and the benchmark that enforce the zero-allocation rule on the frame loop. It owns the 1000-element tree builder, the xUnit allocation test, and the BenchmarkDotNet layout benchmark. It is the last feature of the core-tree epic, so it measures the complete layout pass.

## Requirements

- A test helper builds a 1000-element tree with a mix of rows, columns, wrap, grow, absolute children, and percent sizes.
- An xUnit test warms the tree with one `UiContext.Update`, marks a subtree dirty, reads `GC.GetAllocatedBytesForCurrentThread()` before and after a second `Update`, and asserts zero bytes (A5).
- The test runs under `dotnet test` in `scripts/test-linux.sh` and `scripts/test-windows.ps1` with no new step in either script.
- A BenchmarkDotNet benchmark in `BrutalUi.Benchmarks` measures a full layout pass and a one-subtree dirty pass on the same tree. It replaces the placeholder benchmark.
- The benchmark runs on demand with `dotnet run -c Release`, not in the test scripts (A6).
- The benchmark numbers go in the feature docs and the changelog, per the mission statement.
- A failed allocation test exits non-zero and blocks a merge.

## Dependencies

- `core-tree/flexbox-layout` must be complete. The test measures its layout pass.
- The `BenchmarkDotNet` 0.15.8 pin and the `BrutalUi.Benchmarks` project from the dev-ops epic.

## Out of Scope

- An allocation test on `Draw`. The imgui-renderer epic adds it to the same helper.
- A GC settings change, server GC, or `GC.TryStartNoGCRegion` in the library code.
- A CI runner. A later `github-ci` epic runs the same scripts on hosted runners.
