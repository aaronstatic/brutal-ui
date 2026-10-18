# Core Tree — Human Testing

**Epic:** core-tree
**Last Updated:** 2026-10-18 04:25

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## element-tree

- [ ] Phase 1 demo: the developer runs `scripts/test-linux.sh`, sees the line `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s`, and sees the `ElementListTests` and `DirtyTrackingTests` results with 0 failed in `artifacts/test-linux/test.log`.
- [ ] Phase 2 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the reorder and reparent tests of `ElementListTests` and `DirtyTrackingTests` pass with 0 failed.
- [ ] Phase 3 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ClassListTests` pass with 0 failed, with the test that adds a ninth class and reads all nine back.
- [ ] Phase 4 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ElementStateTests` pass with 0 failed, with the test that builds a three-level tree in one builder expression.
- [ ] Phase 5 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees `TreeGuardTests` and `TreeAllocationTests` pass with 0 failed.
- [ ] How to see the class names of the demos (CR-11): `dotnet test` and `artifacts/test-linux/test.log` print no name of a passed test, only `total` and `failed: 0`. For each class of a demo line, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<ClassName>'`, for example `'*ElementListTests'`. Expect a count above 0 and `failed: 0`.
- [ ] Windows run (Q7): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

## flexbox-layout

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexDirectionFixtureTests'` and reads a total of 6 or more with `failed: 0`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*MarginFixtureTests' --filter-class '*PaddingFixtureTests' --filter-class '*BorderFixtureTests'` and reads a total of 10 or more with `failed: 0`.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexFixtureTests' --filter-class '*JustifyContentFixtureTests'` and reads a total of 18 or more with `failed: 0`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*AlignItemsFixtureTests' --filter-class '*AlignSelfFixtureTests'` and reads a total of 10 or more with `failed: 0`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*AbsolutePositionFixtureTests' --filter-class '*DisplayFixtureTests'` and reads a total of 13 or more with `failed: 0`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PercentageFixtureTests'` and reads a total of 10 or more with `failed: 0`.
- [ ] Phase 7 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexWrapFixtureTests' --filter-class '*AlignContentFixtureTests'` and reads a total of 16 or more with `failed: 0`.
- [ ] Phase 8 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*MeasureFixtureTests'` and reads a total of 6 or more with `failed: 0`.
- [ ] Phase 9 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*LayoutCacheTests'` and reads `failed: 0`, with the test that runs a second `Update` with no change and counts 0 measure calls.
- [ ] Phase 10 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*LayoutAllocationTests'` and reads `failed: 0`.
- [ ] Windows run (flexbox-layout Q6): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
