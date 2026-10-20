# ImGui Renderer - Human Testing

**Epic:** imgui-renderer
**Last Updated:** 2026-10-20 21:25

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

## label-and-image

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAOneLineLabel' --show-live-output on` and reads `rect 0,0,40,16`, then the three lines `Begin 400x300 scale 1`, `Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, and `End`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*WrapsALabelAtItsWidth' --show-live-output on` and reads `label rect 0,0,63,26`, then one command line of the background list whose glyph vertices lie inside `0,0-64,27`.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAnImageAtItsTextureSize' --show-live-output on` and reads `Image 0,0,64,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0`, then `Image 0,57,100,50 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0` for the fit image in a 100 by 100 box.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ShowsTheNewLocaleAfterARefresh' --show-live-output on` and reads `rect 0,0,40,16` and `dirty Layout, Paint`, then `rect 0,0,128,16` and `Text 0,0 font 0 size 16 #FFFFFFFF wrap 128 "Bonjour le monde"`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsTheLabelInsideItsPadding' --show-live-output on` and reads `rect 0,0,64,40` and `Text 12,12 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, then `Text 40,40 font 0 size 16 #FFFFFFFF wrap 320 "Hello"` for the root label with a padding of 10 %.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ElementAllocationTests'` and reads a total of 4 with `failed: 0`. The wildcard also matches `ThousandElementAllocationTests`, so this command reads a total of 12. The full name `--filter-class 'BrutalUi.Core.Tests.ElementAllocationTests'` reads a total of 4.
- [ ] Windows run (label-and-image): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
