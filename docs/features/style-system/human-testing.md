# Style System — Human Testing

**Epic:** style-system
**Last Updated:** 2026-10-22 00:55

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

## style-parser

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetPaintsAPanel' --show-live-output on` and reads `panel #1E222CFF` and `plain #00000000`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCellFromText' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after `panel.States = Hover`.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetLaysOutAPanel' --show-live-output on` and reads `panel 20,10,200,40`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AThemeVariableColorsTheButton' --show-live-output on` and reads `button #3060C0FF`, then `error 2:25 UnknownVariable` for a second sheet that reads `var(--missing)`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ASheetWithThreeMistakesReportsEachOne' --show-live-output on` and reads `error 2:3 UnknownProperty`, `error 3:10 InvalidValue`, `error 4:1 UnexpectedEnd`, and `sheet null`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATransitionDeclarationIsStoredInTheRule' --show-live-output on` and reads `BackgroundColor 0.2s EaseOut`, then `Opacity none`.
- [ ] Phase 7 demo: the developer runs `scripts/test-linux.sh; echo "exit=$?"` and reads `test-linux: PASS build, test, gpu, smoke (120 frames) in <S> s` and `exit=0`. Then `grep -c 'draw commands 2, vertices 128' artifacts/test-linux/smoke.log` prints `1`.
- [ ] Error path (style-parser): run `dotnet build brutal-ui.sln`, then write `.hello-panel { colour: #fff; }` into `samples/HelloPanel/bin/Debug/net10.0/HelloScreen.css`. Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 1; echo "exit=$?"`. Expect `exit=1` and the line `HelloScreen.css(1,16): UnknownProperty:` on standard error. Then run `touch samples/HelloPanel/HelloScreen.css` (or delete the bin copy) before the next build. `PreserveNewest` does not copy an older source over a newer copy.
- [ ] Windows run (style-parser): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
