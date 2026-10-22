# Input Routing — Human Testing

**Epic:** input-routing
**Last Updated:** 2026-10-22 08:27

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## event-model

- [ ] Phase 1 demo: a unit test pushes a pointer down at the leaf of a 3-deep tree (root, middle, leaf), drains the queue, and asserts capture (root, middle), target (leaf), and bubble (middle, root). The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 5 with `failed: 0`.
- [ ] Phase 2 demo: a unit test registers a capture handler on the middle element that calls `e.Header.StopPropagation()`. After the drain, the recorder holds root capture and middle capture only, and a second queued event still walks all six calls. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 3 demo: a unit test pushes a key down at the leaf of the 3-deep tree and reads the six calls of `OnKeyDownCapture` and `OnKeyDown` in capture, target, and bubble order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventKindTests'` and reads a total of 12 with `failed: 0`.
- [ ] Phase 4 demo: a unit test creates a context with `EventQueueCapacity = 4`, pushes 6 pointer events, and reads `QueuedEventCount` 4 and `DroppedEventCount` 2. The drain walks events 3 to 6 in order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventQueueTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `EventWalk.Dispatch` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
- Note (event-model): the cross-phase review added tests. `EventWalkTests` now gives a total of 13 (the Phase 1 and Phase 2 lines say 5 and 10), and `EventQueueTests` gives 9 (the Phase 4 line says 8). Expect `failed: 0` in each run.
- [ ] Windows run (event-model): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.
