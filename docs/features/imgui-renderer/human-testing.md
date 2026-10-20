# ImGui Renderer - Human Testing

**Epic:** imgui-renderer
**Last Updated:** 2026-10-20 04:10

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## renderer-interface

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads 7 call lines: `Begin`, the root Rect, PushClip 2,2,196,96, two child Rects, PopClip, and End.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsHiddenAndFadedTree' --show-live-output on` and reads a call list. It has no Rect for the hidden child. The fill of the half-opacity panel has alpha `80` (128). The fill of its half-opacity child has alpha `40` (64).
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsContentBetweenTheBackgroundAndTheChildren' --show-live-output on` and reads five lines for the probe element in this order. They are the Rect line, the PushClip line, a `Text` line, the Rect line of its child, and the PopClip line.
- [ ] Windows run (renderer-interface): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

## imgui-draw-list

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads two command lines: the root fill `0,0-200,100` in `#0A141EFF` under the clip `0,0,400,300`, then the two child fills in `#FF0000FF` and `#00FF00FF` under the clip `2,2,198,98`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*MeasuresAndDrawsText' --show-live-output on` and reads three measure lines for `Hello, brutal-ui`. They show 13 px with height 13, 26 px with height 26, and 13 px at a wrap width of 70 with height 26. Then one command line shows glyph vertices inside the measured box.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsAnImageWithItsTexture' --show-live-output on` and reads `size 64x32` for the handle. Then a command line shows `tex id 4660` and the image bounds `20,20-84,52`. A second command line shows `tex atlas` for the rect that follows.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*DrawsARoundedPanelWithABorder' --show-live-output on` and reads two rule lines: `corners 8,8,0,0 -> rounding 8 flags TopLeft|TopRight` and `border 2 -> inset 0.5 rounding 7`. Then a command line shows vertices inside `0,0-200,100` grown by 1 pixel. It shows the fill color and the border color.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-class '*ImGuiRendererAllocationTests'` and reads a total of 3 with `failed: 0`.
- [ ] Windows run (imgui-draw-list): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0. The test run loads `imgui.dll` from the pinned `Ahwoo.BRUTAL.ImGui` package.
