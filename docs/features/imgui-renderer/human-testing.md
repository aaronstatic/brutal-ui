# ImGui Renderer - Human Testing

**Epic:** imgui-renderer
**Last Updated:** 2026-10-20 04:10

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## renderer-interface

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads 7 call lines: `Begin`, the root Rect, PushClip 2,2,196,96, two child Rects, PopClip, and End.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsHiddenAndFadedTree' --show-live-output on` and reads a call list with no Rect for the hidden child, alpha `80` (128) in the fill of the half-opacity panel, and alpha `40` (64) in the fill of its half-opacity child.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsContentBetweenTheBackgroundAndTheChildren' --show-live-output on` and reads, for the probe element, the Rect line, the PushClip line, a `Text` line, the Rect line of its child, and the PopClip line, in that order.
- [ ] Windows run (renderer-interface): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
