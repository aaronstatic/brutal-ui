# state-and-commands/commands-and-events - Code Review

**Reviewed:** 2026-10-29
**Reviewer:** Agent (cross-phase review of phases 1 to 5)
**Rules Applied:** 1. One click, one invoke, 2. The command registry rules, 3. The event bus rules, 4. The service registry rules, 5. 0 bytes after warm-up, and a strong gate, 6. The Core and Framework split, 7. API clarity and correct remarks, 8. Consistent shape, 9. The seam for the later features, 10. Test quality, 11. The docs, 12. The project rules, 13. No change outside the scope. The Vercel React rules do not apply to C#.
**Status:** Resolved - 2 findings, 2 resolved (review groups 1 and 2)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 0 | - |
| LOW | 2 | CR-1, CR-2 |

The feature does what the plan asks. The three registries and the bind follow the Internal Shape of the plan. The probes found no fault in the code. `Unsubscribe` removes the first match, the last removal leaves a working channel, `Unregister` works on a full array, and a grow during an invoke is safe. The gate measures 0 bytes in Debug, and the per-call paths of `Commands/`, `Events/`, and `Services/` hold no box and no hidden delegate.

The two findings are gaps in the tests. Two plants pass each test of the feature. A reversed loop in `EventBus.Unsubscribe` removes the last match, and no test sees it (CR-1). A bound of `_count` in the shift loop of `CommandRegistry.Unregister` throws on a full array, and no test fills the array before an unregister (CR-2).

Rules 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 12, and 13 have no finding. Rule 10 has CR-1 and CR-2.

### Baseline

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2668, failed 0, succeeded 2666, skipped 2. The two skipped tests are the GLFW tests that need a display. The base total was 2624, and the five new classes add 44 tests.
- `BrutalUi.Framework.Tests` (`dotnet test --project`): total 93, failed 0. The base count was 49.
- The class counts: `CommandRegistryTests` 12, `CommandBindingTests` 10, `EventBusTests` 10, `ServiceRegistryTests` 7, and `CommandAndEventAllocationTests` 5. Each count matches the plan.
- Rule 13: the three `git diff 60c6950` commands print no line. The new files are untracked, and each new file is in `Commands/`, `Events/`, or `Services/` of the source and test projects.

### Probe method

Each probe ran in the Debug build, and one probe also ran in the Release build. The review copied each changed source file to the scratchpad first. After each run, it copied the file back and checked it with `cmp`. Each `cmp` found no difference. The review deleted the probe test file and its folder after the runs.

### Probe list

- **Probe test file in `BrutalUi.Framework.Tests` (5 tests, deleted after the runs).** The 5 tests passed in Debug and in Release.
  - P1: subscribe the pairs `a`, `b`, and `a`. Unsubscribe `a`, then publish. The order is `b`, `a`. Pass.
  - P2: subscribe a static method group, then unsubscribe it with a second method group conversion. The bus removes it. Pass.
  - P3: unsubscribe the last subscriber of a type. The count is 0, a publish calls nothing, and a new subscribe gets the next publish. Pass.
  - P4: register 8 commands (a full array), then unregister the last name and the first name. The other names invoke. Pass.
  - P5: register 8 commands. The handler of one command registers a ninth command (a grow from 8 to 16 slots), unregisters itself, and invokes another command. Pass.
- **Plant A in `EventBus.cs`: the loop of `Unsubscribe` runs from the last slot to the first slot.** **Plant B in `CommandRegistry.cs`: the shift loop of `Unregister` runs to `i < _count`.** The review ran plants A and B in the same run. `BrutalUi.Framework.Tests` gave total 98, failed 2. Only P1 and P4 failed. Each of the 93 tests of the feature passed (CR-1 and CR-2).
- **Plant C in `EventBus.cs`: `ReferenceEquals(old[i].Handler, handler)` in place of `==`.** Each of the 98 tests passed, also P2. The C# compiler caches the delegate of a static method group and of a static lambda. Thus a game that follows the handler rule cannot see the difference. This result is not a finding.
- **Release run.** `BrutalUi.Framework.Tests` with the probes gave total 98, failed 0 in Release. The review ran `UnregisterRemovesTheNameAndDropsTheTarget` three times in Debug and three times in Release. Each run passed. The `NoInlining` helper keeps the target out of each frame of the test, so the `WeakReference` check is deterministic.

---

## Findings

### CR-1: No test pins the first-match rule or the last removal of `Unsubscribe`

**Severity:** LOW
**Rule:** 10 (Test quality)
**File:** `tests/BrutalUi.Framework.Tests/Events/EventBusTests.cs`, lines 121 to 140 (`UnsubscribeRemovesOneSubscriber`) and lines 142 to 160 (`TheSamePairTwiceGetsTwoCalls`)

**Problem:** Decision 5, the remarks of `EventBus`, and section 10 state that `Unsubscribe` removes the first match. The test of the same pair subscribes two equal pairs next to each other:

```csharp
bus.Subscribe(s_record, log);
bus.Subscribe(s_record, log);
```

A removal of the first match and a removal of the last match give the same array. Thus the test cannot tell them apart. Plant A proved it: an `Unsubscribe` that scans from the last slot passes each test of the feature. With such a bus, a game that subscribes a pair twice, with another subscriber between them, gets a changed call order after one unsubscribe.

Rule 3 also names the removal of the last subscriber of a type. No test of `EventBusTests` removes the last subscriber and then publishes or subscribes again. The code is correct (probe P3), but no test guards it.

**Fix:** Keep the count at 10. In `TheSamePairTwiceGetsTwoCalls`, give `log` an order list and subscribe `log`, then `other`, then `log`. After the unsubscribe and the second publish, assert that the order list ends with `other`, `log`. A removal of the last match gives `log`, `other`. In `UnsubscribeRemovesOneSubscriber`, add a step at the end. Unsubscribe `second`, assert a count of 0, publish, and assert no new call. Then subscribe `second` again, publish, and assert one more call. Run plant A again and expect a failure.

**Effort:** Low

**Resolved:** 2026-10-29 - `TheSamePairTwiceGetsTwoCalls` now subscribes `log`, `other`, `log` and asserts the call order, `UnsubscribeRemovesOneSubscriber` now removes the last subscriber and then publishes and subscribes again, the count stays at 10, and plant A (reversed loop) now fails `TheSamePairTwiceGetsTwoCalls`.

### CR-2: No test runs `Unregister` on a full array or a grow during an invoke

**Severity:** LOW
**Rule:** 10 (Test quality)
**File:** `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs`, lines 161 to 199 (`UnregisterRemovesTheNameAndDropsTheTarget`) and lines 201 to 222 (`AHandlerCanRegisterUnregisterAndInvokeDuringAnInvoke`)

**Problem:** Rule 2 asks the review to look for an off-by-one in the grow and in `Unregister`. The code has no off-by-one. The tests cannot find one. The largest registry of `CommandRegistryTests` holds 4 commands, and the first array holds 8 slots. Thus no test of the class fills the array or grows it. A shift loop with the bound `i < _count` reads the slot after the last command. That slot holds `default` while the array has a free slot, so the result does not change. On a full array, the read throws `IndexOutOfRangeException`. Plant B proved it: the bound `i < _count` passes each test of the feature.

The gate covers the grow at build time only, because `BuildScene` registers 16 commands. The remarks of `Invoke` and section 10 state that a register in a handler can grow the array. No test grows the array during an invoke. The code is correct (probe P5), but no test guards it.

**Fix:** Keep the count at 12. In `UnregisterRemovesTheNameAndDropsTheTarget`, register 4 more commands (`f0` to `f3`) before the first unregister, so the array is full (8 of 8). Assert a count of 8, and change the later counts by 4. In `AHandlerCanRegisterUnregisterAndInvokeDuringAnInvoke`, register 7 more commands before the invoke. The register of `b` in the handler then grows the array from 8 to 16 slots. Run plant B again and expect a failure.

**Effort:** Low

**Resolved:** 2026-10-29 - Both tests now fill the first array (8 of 8 slots) and the second test grows it to 16 slots, the count stays at 12, and plant B (`i < _count`) now fails `UnregisterRemovesTheNameAndDropsTheTarget` with an `IndexOutOfRangeException`.

---

## What's Already Good

- `CommandRegistry.Invoke` copies the handler and the target to locals before the call. It holds no `throw`, and the miss path calls the slot with the caller span. `Unregister` writes `default` into the old last slot, and the `WeakReference` test proves the drop in Debug and in Release.
- `EventBus.Publish` reads the array one time into a local. Two tests prove the snapshot rule, and each of them fails for a publish that reads `Items` inside the loop. `Subscribe` and `Unsubscribe` write a new array. Only `Subscribe` creates a channel.
- `ServiceRegistry` keys on the exact type. `Resolve` and `TryResolve` throw nothing, and `NotNullWhen(true)` on the `out` value is correct for a `class` constraint. A duplicate register keeps the first service.
- `BindCommand` adds to `Clicked` with `+=`. The tests pin the order with a game handler, two binds on one button, a late register, the unknown-name slot, the copy of the name, and the Enter key. The Space key shares the `Click` step of `Button`, and `ButtonKeyTests` of Core covers it.
- The gate has a strong real-work test. Each of the four bodies has an assert of a fixed increment, so an empty body fails. No measured body holds a lambda, a string, a box, an array, or an `Assert`. `AllocationGate.ReleaseAllocationContext()` runs right before each measure.
- The XML doc comments state the build-time rule, the handler rule, the re-entry rule, the error rule, and the reference rule. Each remark matches the code. The ASD-STE100 lint of the doc comment text of the five source files gives 0.97.
- The new files are ASCII text with no BOM, use file-scoped namespaces, and hold their own `using` lines. The new source folders hold no `lock`, no `IDisposable`, no `INotify` type, no `event` keyword, and no `ToString(` call.
- `EventBus.Unsubscribe` has no null check on `handler`, but a `null` handler matches no subscriber and gives `false`. This result agrees with the remarks, so rule 8 has no finding.

---

## Follow-up Questions

1. **Two open panels that register the same command name (panels-and-modes).** `Register` throws for a duplicate name (Decision 8). A game can open two panels that each want `Cancel`. The plan does not say how panels-and-modes routes a name to the active panel.
   - (A) Each panel or mode owns a `CommandRegistry`, and the mode gives the active registry to the action map.
   - (B) One registry for the game, and each panel gives its commands a unique name, for example a panel prefix.
   - (C) A scope stack in `CommandRegistry`, so a name of the top scope hides the same name below it.
   - **Default:** (B) for this epic, because action-map calls one registry. The panels-and-modes plan decides between (A) and (C).
2. **A release of each command and each subscriber of one target.** A closed panel must call `Unregister` for each name and `Unsubscribe` for each type and handler. The API has no release by target.
   - (A) The panel keeps a list of its names and pairs, and it releases each one. The API does not change.
   - (B) Add `CommandRegistry.UnregisterTarget(object target)` and `EventBus.UnsubscribeTarget(object target)`.
   - **Default:** (A) until the panels-and-modes plan shows a need for (B).
3. **The mosaic-ui names (Conflicts and Gaps item 1 of the plan).** The plan asks the user to confirm the names or give the mosaic-ui source before action-map starts. `context.md` holds no answer.
   - (A) Keep `EventBus`, `ServiceRegistry`, `CommandRegistry`, and the verbs of the requirements.
   - (B) The user gives the source, and a rename runs before action-map.
   - **Default:** (A).

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-2 | Low | Low | A regression of the shift loop throws in the frame loop when a panel closes on a full registry. |
| 2 | CR-1 | Low | Low | A regression changes the call order only for a pair that subscribes twice with another subscriber between them. |

---

## Progress

- [x] CR-1: No test pins the first-match rule or the last removal of `Unsubscribe`
- [x] CR-2: No test runs `Unregister` on a full array or a grow during an invoke
