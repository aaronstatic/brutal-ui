# Core Tree — Human Testing

**Epic:** core-tree
**Last Updated:** 2026-10-16 18:25

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## element-tree

- [ ] Phase 1 demo: the developer runs `scripts/test-linux.sh`, sees the line `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s`, and sees the `ElementListTests` and `DirtyTrackingTests` results with 0 failed in `artifacts/test-linux/test.log`.
- [ ] Phase 2 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the reorder and reparent tests of `ElementListTests` and `DirtyTrackingTests` pass with 0 failed.
- [ ] Phase 3 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ClassListTests` pass with 0 failed, with the test that adds a ninth class and reads all nine back.
- [ ] Phase 4 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees the `ElementStateTests` pass with 0 failed, with the test that builds a three-level tree in one builder expression.
- [ ] Phase 5 demo: the developer runs `dotnet test --solution brutal-ui.sln` and sees `TreeGuardTests` and `TreeAllocationTests` pass with 0 failed.
- [ ] How to see the class names of the demos (CR-11): `dotnet test` and `artifacts/test-linux/test.log` print no name of a passed test, only `total` and `failed: 0`. For each class of a demo line, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<ClassName>'`, for example `'*ElementListTests'`. Expect a count above 0 and `failed: 0`.
- [ ] Windows run (Q7): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
