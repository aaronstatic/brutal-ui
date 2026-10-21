# imgui-renderer/hello-panel-sample - Inbox

> The loop adds one line for each item that reaches a run at any point: a bug, a change, or a note. It acts on the open lines after each phase. A done line stays. Section 12 of `.beast-mode/phase-handoff.md` holds the rules and the line shape.

## Items

- [x] bug 2026-10-21 01:55 | Verifier FAIL: the allocation tests of `ElementAllocationTests` fail at random, about 1 run in 10 to 20. `UpdateAndDrawAllocateNothingAfterWarmUp` measured 5968 bytes in one `scripts/test-linux.sh` run (exit 2). `RefreshLocalizedTextAllocatesNothing` (ElementAllocationTests.cs:205) failed in 1 of 20 runs of `BrutalUi.Core.Tests`. No file of `src/` or `tests/` had a change. | done: phase-3-fix1.md, phase-3-fix2.md
- [x] bug 2026-10-21 02:40 | Verifier FAIL: the other allocation gates of `BrutalUi.Core.Tests` have the same background GC cause as `ElementAllocationTests`. Under a load of 8 parallel runs, `LayoutAllocationTests.LayoutStepAllocatesNothingAfterWarmUp` failed 1 time and `TreeAllocationTests.TreeChangesAndUpdateAllocateNothingAfterWarmUp` failed 3 times in 480 runs. `ThousandElementAllocationTests` has the same exposure. | done: phase-3-fix2.md
