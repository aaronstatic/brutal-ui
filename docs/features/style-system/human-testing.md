# Style System — Human Testing

**Epic:** style-system
**Last Updated:** 2026-10-21 21:18

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## stylesheet-model

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*PaintsAPanelFromAClassRule' --show-live-output on` and reads `panel BackgroundColor #1E222CFF` and `plain BackgroundColor #00000000`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AnInlineValueWinsOverARule' --show-live-output on` and reads `BackgroundColor #3060C0FF width 200`, then `BackgroundColor #1E222CFF width 200` after the clear.
- [ ] Phase 7 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TextColorFlowsDownTheTree' --show-live-output on` and reads `root #FFFFFFFF`, `panel #FFCC00FF`, `child #FFCC00FF`, `badge #FF0000FF`, then `child #FFFFFFFF` after the class removal.
- [ ] Phase 8 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALabelDrawsInTheInheritedTextStyle' --show-live-output on` and reads `rect 0,0,40,16` and `Text 0,0 font 0 size 16 #FFCC00FF wrap 40 "Hello"`, then `rect 0,0,80,32` and `Text 0,0 font 0 size 32 #FFCC00FF wrap 80 "Hello"` after the new panel size.
- [ ] Phase 9 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ATypeSelectorStylesEachLabel' --show-live-output on` and reads `label #FFCC00FF size 0`, `title label #00FF00FF size 24`, and `title panel #00FF00FF size 0`.
- [ ] Phase 10 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALaterSheetWinsAndARemoveRestyles' --show-live-output on` and reads `panel #3060C0FF`, then `panel #1E222CFF` after the removal of the theme sheet.
- [ ] Phase 11 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 9 with `failed: 0`.
- [ ] Windows run (stylesheet-model): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

## selector-matching

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverRuleRestylesTheButton' --show-live-output on` and reads `button #1E222CFF`, then `button hover #3060C0FF` after `States = Hover`, then `button #1E222CFF` after `States = None`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCell' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after the hover of the panel, then `row #00000000 cell #00000000` after the hover ends.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ANameRuleWinsOverAClassAndAState' --show-live-output on` and reads `title #00FF00FF`, then `subtitle #3060C0FF` after `Name = "subtitle"`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 10 with `failed: 0`.
- [ ] Windows run (selector-matching): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
