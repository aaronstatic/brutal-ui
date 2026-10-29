# state-and-commands/commands-and-events - Context & Decisions

**Last Updated:** 2026-10-29 11:55
**Current Phase:** Complete (phases 1 to 5 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan (`implementation.md`). The user chose (A) for each of the four open scope questions on 2026-10-29.
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file)
- ✅ Phase 1 (tasks 1.1 to 1.7): `CommandHandler`, `CommandRegistry`, `CommandBindings.BindCommand`, `CommandRegistryTests` (6), `CommandBindingTests` (5). The verifier returns PASS. Demo: PASS.
- ✅ Phase 2 (tasks 2.1 to 2.6): `UnknownCommand`, the name rules, `Unregister`, the re-entry rules, `CommandRegistryTests` (12), `CommandBindingTests` (10). The verifier returns PASS. Demo: PASS.
- ✅ Phase 3 (tasks 3.1 to 3.4): `EventBusHandler<T>`, `EventBus`, `EventBusTests` (10). The verifier returns PASS. Demo: PASS.
- ✅ Phase 4 (tasks 4.1 to 4.3): `ServiceRegistry`, `ServiceRegistryTests` (7). The verifier returns PASS. Demo: PASS.
- ✅ Phase 5 (tasks 5.1 to 5.7): `CommandAndEventAllocationTests` (5, 0 bytes in Debug), the four plants (each `exit=2`), sections 10 and 14 of the technical design, and the changelog. The verifier returns PASS. Demo: PASS.
- ✅ The cross-phase review (`code-review.md`): 2 LOW findings (CR-1, CR-2, both test gaps), 2 fixed in review groups 1 and 2. The review verifier returns PASS.
- ✅ The draft plan of the next feature: `docs/features/state-and-commands/action-map/implementation.md` (the solution-architect writes it, 7 phases, 45 tasks, 4 open scope questions).

**What's Next:**
- 📋 A person tests the lines of `## Needs Human Verification`.
- 📋 `/autorun-epic` commits the feature and goes on to `state-and-commands/action-map`. The user answers the four open scope questions of its draft plan first.

**Blockers:**
- None

---

## Base and Counts

- Base commit: `60c69501e4f58cb18892dcc3914a4f0205ba7ace` (`feat: state-and-commands/element-bindings`).
- Counts before any code change: solution 2624 (2622 passed, 2 skipped: the GLFW tests, no display), `BrutalUi.Core.Tests` 1733, `BrutalUi.Framework.Tests` 49.
- Counts after phase 1: solution 2635 (2633 passed, 2 skipped, 0 failed), `BrutalUi.Core.Tests` 1733, `BrutalUi.Framework.Tests` 60 (`CommandRegistryTests` 6, `CommandBindingTests` 5).

- Counts after phase 2: solution 2646 (2644 passed, 2 skipped, 0 failed), `BrutalUi.Core.Tests` 1733, `BrutalUi.Framework.Tests` 71 (`CommandRegistryTests` 12, `CommandBindingTests` 10).

- Counts after phase 3: solution 2656 (2654 passed, 2 skipped, 0 failed), `BrutalUi.Core.Tests` 1733, `BrutalUi.Framework.Tests` 81 (`CommandRegistryTests` 12, `CommandBindingTests` 10, `EventBusTests` 10).

- Counts after phase 4: solution 2663 (2661 passed, 2 skipped, 0 failed), `BrutalUi.Core.Tests` 1733, `BrutalUi.Framework.Tests` 88 (`CommandRegistryTests` 12, `CommandBindingTests` 10, `EventBusTests` 10, `ServiceRegistryTests` 7).

- Counts after phase 5 group 1: solution 2668 (2666 passed, 2 skipped, 0 failed), `BrutalUi.Core.Tests` 1733, `BrutalUi.Framework.Tests` 93 (`CommandRegistryTests` 12, `CommandBindingTests` 10, `EventBusTests` 10, `ServiceRegistryTests` 7, `CommandAndEventAllocationTests` 5).
- Counts at the end (after the review fixes): the same as after phase 5 group 1. The review fixes changed two tests and added none.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Commands/CommandHandler.cs` - `CommandHandler` (phase 1), `UnknownCommandHandler` (phase 2)
- `src/BrutalUi.Framework/Commands/CommandRegistry.cs` - the command registry (phases 1 and 2)
- `src/BrutalUi.Framework/Commands/CommandBindings.cs` - `BindCommand` on `Button` (phases 1 and 2)
- `src/BrutalUi.Framework/Events/EventBus.cs` - `EventBusHandler<T>` and `EventBus` (phase 3)
- `src/BrutalUi.Framework/Services/ServiceRegistry.cs` - the service registry (phase 4)

### Tests
- `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs` - 6, then 12 tests (phases 1 and 2)
- `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs` - 5, then 10 tests (phases 1 and 2)
- `tests/BrutalUi.Framework.Tests/Events/EventBusTests.cs` - 10 tests (phase 3)
- `tests/BrutalUi.Framework.Tests/Services/ServiceRegistryTests.cs` - 7 tests (phase 4)
- `tests/BrutalUi.Framework.Tests/Commands/CommandAndEventAllocationTests.cs` - the sixteenth allocation gate, 5 tests (phase 5)

### Related Files
- `docs/features/state-and-commands/commands-and-events/implementation.md` - Implementation plan
- `docs/features/state-and-commands/epic-overview.md` - the epic rollup and the build order
- `docs/features/state-and-commands/element-bindings/context.md` - the Gotchas (the Debug box, the real-work test, the scoped grep)

---

## Important Decisions

The plan holds Decisions 1 to 9 and the Confirmed Assumptions. The user chose (A) for each of the four Notes questions: the static handler plus a target for the event bus, the return value plus the `UnknownCommand` slot, the phase 1 demo on `CommandBindingTests`, and a throw for a duplicate register. This section records each decision that the run makes past the plan.

- **The Seam.** `Button` lives in Core, and Core does not reference Framework. The bind lives in Framework as an extension method, as `LabelBindings` and `WidgetBindings` do. `Button.Clicked` is an `Action<Button>` with no target slot. The bind creates one small target object (the registry and a copy of the name) and adds one instance delegate of that object to `Clicked` with `+=`. That is the one-time build allocation of A5. The click path runs the delegate, the registry compares bytes, and the registry calls a static handler with its target. Nothing in that path allocates.
- **The Internal Shape.** `CommandRegistry` holds `CommandEntry[]? _entries` (null until the first register, then 8 slots, then doubling) and `int _count`. `CommandEntry` holds a copy of the name, the `CommandHandler`, and the target. `IndexOf` scans in the order of the register with `SequenceEqual`. `Invoke` copies the handler and the target to locals before the call. `EventBus` holds `Dictionary<Type, object>` of `Channel<T>`, each with a copy-on-write `Subscriber<T>[]`. `Publish` reads the array once (the snapshot). `ServiceRegistry` holds `Dictionary<Type, object>` keyed by the exact type.
- **The Frame Order.** This feature does not change `UiContext.Update`. A button click runs in the input pass, so its command runs there. A command that writes a store shows in the binding pass of the same `Update`. The event bus and the service registry have no pass.
- **Lesson 1 of element-bindings.** The code of `EventBus` and `ServiceRegistry` holds no `is null`, `== null`, `(object)` cast, or `Equals` call on a value of type `T`. A Debug build keeps the box.
- **The target compare of `Unsubscribe` (phase 3, past the plan).** `Unsubscribe` compares the target with `old[i].Target == target`, a reference compare of two `object?` values. The brief named `ReferenceEquals`, but the box grep of task 3.3 matches `Equals(` inside `ReferenceEquals(`. The `==` on `object` is the same compare and boxes nothing.
- **The duplicate check of `ServiceRegistry.Register` (phase 4, past the plan).** `Register` checks `ContainsKey` and then calls `Add`, so the message of the `ArgumentException` stays constant. The parameter name is `service`.
- **The click scene of the gate (phase 5, past the plan).** `Press` and `Release` of the gate scene are `required` fields, not properties, because `UiContext.PushEvent(in ...)` needs a variable.
- **The review fixes keep the counts.** CR-1 and CR-2 extend four existing tests and add none, so each class count of the Definition of Done and of section 14 stays the same.

---

## Gotchas & Learnings

- **A box grep for `Equals\(` also matches `ReferenceEquals(`.** Use `==` on two `object?` values for a reference compare, or scope the grep to `\.Equals\(`.
- **A `static readonly` handler field that reads a later `static readonly` field in its lambda gives CS8604.** Declare the later field first.
- **`UiContext.PushEvent(in ...)` needs a variable.** A property gives CS8156. Hold each event of a scene in a field.
- **An empty click body still measures 0 bytes.** Plant 4 shows that only the real-work test catches an empty body of a gate. Keep a fixed-increment assert for each body.
- **A test registry that never fills its array cannot find an off-by-one in a shift.** CR-2: the first array holds 8 slots, so a test must register 8 commands before an `Unregister` to reach the last slot.
- **Two equal pairs next to each other cannot pin a first-match rule.** CR-1: put another subscriber between the two pairs, so a removal of the first match and of the last match give a different order.

---

## API Integration

- `CommandRegistry.Invoke(ReadOnlySpan<byte>)` is the entry of action-map. `CommandBindings.BindCommand` is the entry of xml-markup. panels-and-modes publishes on `EventBus` and reads `ServiceRegistry`.

---

## State Management

- The game owns each registry instance. No registry holds static state. Each registry holds its targets until an unregister, an unsubscribe, or the drop of the registry.

---

## Testing Approach

### Unit Tests
- `CommandRegistryTests` (12), `CommandBindingTests` (10), `EventBusTests` (10), `ServiceRegistryTests` (7) in `tests/BrutalUi.Framework.Tests/`.

### Allocation Tests
- `CommandAndEventAllocationTests` (5): the sixteenth gate. Four plants guard `Invoke`, `Publish`, `TryResolve`, and the click body.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 5 with `failed: 0`. The first test clicks a button bound to `save` and reads one call of the save handler with its target.
- [ ] Phase 2 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 10 with `failed: 0`. The test `AClickOnAnUnknownNameCallsTheUnknownCommandSlot` reads the name `load` in the slot and no handler call.
- [ ] Phase 3 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*EventBusTests'` and reads a total of 10 with `failed: 0`. The first test publishes a `ScoreChanged` event to three subscribers and reads the score in each target, in the order of the subscribe.
- [ ] Phase 4 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ServiceRegistryTests'` and reads a total of 7 with `failed: 0`. The test `AMissingServiceGivesNullAndThrowsNothing` reads `null` for a type that no one registered.
- [ ] Phase 5 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandAndEventAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
- [ ] Windows run: a person runs `scripts/test-windows.ps1` on a Windows machine and reads its PASS line. The run checks the sixteenth gate on the Windows JIT. No verifier of this run had a Windows machine.

---

## Next Steps

### Immediate (Next Session)
1. Test the lines of `## Needs Human Verification`.
2. Commit the feature (the caller `/autorun-epic` owns each git action).
3. Answer the four open scope questions of `docs/features/state-and-commands/action-map/implementation.md`, then build action-map.

---

## Open Questions

The cross-phase review left three follow-up questions (`code-review.md`, `## Follow-up Questions`). Each default keeps the current API, and no task of this feature waits on them.
- Two open panels that register the same command name. Default: unique names for this epic. The panels-and-modes plan picks a registry for each mode or a scope stack.
- A release of each command and each subscriber of one target. Default: the caller keeps its names and pairs.
- The mosaic-ui names (Conflicts and Gaps item 1 of the plan). Default: keep `EventBus`, `ServiceRegistry`, `CommandRegistry`, and the verbs of the requirements. The draft plan of action-map carries the same question.

---

## Session Notes

### 2026-10-29 10:54
- `/start-feature` (called by `/autorun-epic`) wrote `tasks.md`, `inbox.md`, and this file. The base commit is `60c6950`.

### 2026-10-29 (phase 1)
- Tasks 1.1 to 1.7 are done. `CommandHandler`, `CommandRegistry`, and `CommandBindings.BindCommand` are written, with 11 tests.
- Demo: a press and a release at the center of a bound button (one `Update` before the click, one after) call the handler one time with its target. A direct `Invoke` of an unknown name gives `false` and throws nothing.
- Build: 0 warnings, 0 errors. Tests: 0 failed. No file of Core and no project file changed.
- The test `ACommandThatIsRegisteredAfterTheBindRuns` first clicks with no command (no call, no throw), then registers and clicks again.

### 2026-10-29 11:00 (phase 1 close)
- The phase 1 verifier returns PASS: build 0 warnings and 0 errors, solution 2635 with 0 failed, `CommandRegistryTests` 6, `CommandBindingTests` 5, the Q4 grep and the `+=` grep clean, no diff in Core, State, Binding, scripts, samples, or a project file. Demo: PASS.

### 2026-10-29 (phase 2)
- Tasks 2.1 to 2.6 are done. `UnknownCommandHandler`, `CommandRegistry.UnknownCommand`, the empty-name and duplicate-name checks, `Unregister`, and the empty-name check of `BindCommand` are written, with 11 more tests.
- Demo: a click on a button bound to `load` (no command `load`) calls the slot one time with the bytes `load` and runs no handler. A command that sets a `Store<int>` to 7 shows `7` in a bound label after one click and one `Update`, with no change to `UiContext`.
- Build: 0 warnings, 0 errors. Tests: 0 failed. `CommandRegistryTests` 12, `CommandBindingTests` 10. The body of `Invoke` holds no `throw`. The grep for `lock`, `IDisposable`, `event`, and `ToString(` in `Commands/` is clean.
- Decision past the brief: `UnregisterRemovesTheNameAndDropsTheTarget` also checks the drop of the target with a `WeakReference` (a `NoInlining` helper registers a temporary target, and the test collects after the unregister). It also registers the name again after the unregister.
- Decision past the brief: `CommandHandler.cs` got `using System;` for `ReadOnlySpan<byte>` (no implicit usings).
- Gotcha: a static readonly handler field that reads a later static readonly field inside its lambda gives CS8604 (nullable). Declare the later field first.

### 2026-10-29 11:05 (phase 2 close)
- The phase 2 verifier returns PASS: build 0 warnings and 0 errors, solution 2646 with 0 failed, `CommandRegistryTests` 12, `CommandBindingTests` 10, the greps clean, no diff outside the feature files. `Invoke` holds no `throw`, and `Unregister` writes `default` into the old last slot. Demo: PASS.

### 2026-10-29 (phase 3)
- Tasks 3.1 to 3.4 are done. `EventBusHandler<T>`, `EventBus` (`Subscribe`, `Unsubscribe`, `Publish`, `SubscriberCount`), and `EventBusTests` (10) are written.
- Demo: a publish of `ScoreChanged(42)` to three subscribers calls each target one time, in the order of the subscribe (`first`, `second`, `third`), and each target reads the score 42.
- Snapshot rule: `Publish` reads `Items` into a local one time at the start. A subscribe or an unsubscribe during the publish writes a new array, so the running publish does not see it. A subscriber that the publish removed still gets the current event, and gets no later event.
- Build: 0 warnings, 0 errors. Tests: solution 2656, 0 failed. `EventBusTests` 10, 0 failed. The generic-value grep (`is null|== null|!= null|\(object\)|Equals\(`, without `///` lines) gives no line. The grep for `lock`, `IDisposable`, `event`, and `ToString(` is clean.
- Decision past the brief: `Unsubscribe` compares the target with `old[i].Target == target` (a reference compare of two `object?` values), not with `ReferenceEquals`. The grep of task 3.3 matches `Equals(` inside `ReferenceEquals(`, so the brief rules conflict. The `==` on `object` is the same reference compare and boxes nothing.
- Decision past the brief: the test `APublishInsideAHandlerReachesEachSubscriber` also checks that a `ScoreChanged` subscriber after the relay gets its call. The test `ASubscriberThatThrowsStopsThePublish` uses a thrower that throws one time, so the second publish reaches each subscriber.

### 2026-10-29 11:12 (phase 3 close)
- The phase 3 verifier returns PASS: build 0 warnings and 0 errors, solution 2656 with 0 failed, `EventBusTests` 10, the box grep and the Q4 grep clean, no diff outside the feature files. `Publish` reads the array one time and creates no channel. `Subscribe` and `Unsubscribe` write a new array. Demo: PASS.

### 2026-10-29 (phase 4)
- Tasks 4.1 to 4.3 are done. `ServiceRegistry` (`Count`, `Register<T>`, `Resolve<T>`, `TryResolve<T>`) and `ServiceRegistryTests` (7) are written.
- Demo: `Resolve<IAudio>()` on a registry with no service gives `null` and throws nothing. `TryResolve` gives `false` and `null`. A service that is registered as `Audio` does not resolve as `IAudio`.
- Build: 0 warnings, 0 errors. Tests: solution 2663, 0 failed. `ServiceRegistryTests` 7, 0 failed. The box grep (`is null|== null|!= null|\(object\)|Equals\(`, without `///` lines) gives no line. `Resolve` and `TryResolve` hold no `throw`. The grep for `lock`, `IDisposable`, `event`, and `ToString(` in code is clean.
- Decision past the brief: `Register` checks the duplicate with `ContainsKey` and then `Add`, so the duplicate message is constant. The `ArgumentException` uses the parameter name `service`.
- Decision past the brief: `CountShowsEachRegisteredService` also registers a `CommandRegistry` and an `EventBus`, to show that the game can register them as services.

### 2026-10-29 11:15 (phase 4 close)
- The phase 4 verifier returns PASS: build 0 warnings and 0 errors, solution 2663 with 0 failed, `ServiceRegistryTests` 7, the box grep and the Q4 grep clean, no diff outside the feature files. `Resolve` and `TryResolve` hold no `throw`. Demo: PASS.

### 2026-10-29 (phase 5 group 1)
- Tasks 5.1 to 5.3 are done. `CommandAndEventAllocationTests` (the sixteenth gate, 5 tests) is written. The gate measures 0 bytes in Debug for each of the four allocation tests. The class gives a total of 5 with 0 failed. Build: 0 warnings, 0 errors. Solution: 2668, 0 failed, 2 skipped.
- Plant 1 (`GC.KeepAlive(new object());` in `CommandRegistry.Invoke`): exit=2. `InvokeAllocatesNothing` fails with 7200 bytes (300 invokes of 24 bytes). `AClickOnABoundButtonAllocatesNothing` fails with 2400 bytes (100 clicks of 24 bytes).
- Plant 2 (the same line in `EventBus.Publish`): exit=2. `PublishAllocatesNothing` fails with 7200 bytes (300 publishes of 24 bytes).
- Plant 3 (the same line in `ServiceRegistry.TryResolve`): exit=2. `ResolveAllocatesNothing` fails with 12000 bytes (500 calls of 24 bytes, because `Resolve` calls `TryResolve`).
- Plant 4 (an empty body of `ClickStep`): exit=2. `TheMeasuredBodyDoesRealWork` fails (expected 1, actual 0 for the click). The gate `AClickOnABoundButtonAllocatesNothing` still measures 0 bytes, so only the real-work test catches an empty body.
- Restore: `cmp` gives 0 for `CommandRegistry.cs`, `EventBus.cs`, `ServiceRegistry.cs`, and the gate file. The tests after the restore give 0 failed.
- Decision past the brief: `Press` and `Release` are required fields of `Scene` (not properties), because `PushEvent(in ...)` needs a variable that C# can pass by reference.
- Decision past the brief: the unused `out` values in `ResolveStep` are named variables. The build gives 0 warnings.
- No debug box: the Debug build measured 0 bytes in `Publish<T>` and `TryResolve<T>`. The Risks table risk did not occur.

### 2026-10-29 (phase 5 group 2)
- Tasks 5.4 to 5.7 are done. Section 10 of `docs/technical-design.md` has the new row text and the subsection "Commands, events, and services". Section 14 has a bullet for the registry tests and the bullet "A sixteenth allocation gate", after the bullet "A fifteenth allocation gate". `CHANGELOG.md` has one new line.
- `scripts/test-linux.sh` prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 17 s` with `exit=0`.
- Lint (limit 2.5): `technical-design.md` 1.52, `CHANGELOG.md` 1.67, `context.md` 2.20 before this note. No file needed a prose fix.
- Decision past the brief: the subsection also states that a subscriber of a base type gets no event of a derived type, and that `Unregister` and `Unsubscribe` give `false` for no match. Both follow from the exact-type rule and the tests.
- Decision past the brief: the `CHANGELOG.md` line adds one sentence on the click path and the `UnknownCommand` slot.

### 2026-10-29 11:24 (phase 5 close)
- The phase 5 verifier returns PASS: build 0 warnings and 0 errors, solution 2668 with 0 failed, `CommandAndEventAllocationTests` 5, `BrutalUi.Framework.Tests` 93 (49 plus 44), `scripts/test-linux.sh` PASS with `exit=0`, the Q4, box, and plant greps clean, no diff outside the feature files, lint 1.52, 1.67, and 2.26. Demo: PASS.

### 2026-10-29 11:40 (the cross-phase review)
- The review (`code-review.md`, ADVANCED reviewer) checks 13 rules and finds 2 LOW test gaps. The code has no fault. Probes and plants restored with a `cmp` check.
- CR-2 (review group 1): `UnregisterRemovesTheNameAndDropsTheTarget` now fills the array (8 of 8) before the first unregister, and `AHandlerCanRegisterUnregisterAndInvokeDuringAnInvoke` grows the array from 8 to 16 inside the handler. Plant B (the shift bound `i < _count`) now fails with `IndexOutOfRangeException`. The count stays 12.
- CR-1 (review group 2): `TheSamePairTwiceGetsTwoCalls` subscribes `log`, `other`, `log` and pins the first-match rule. `UnsubscribeRemovesOneSubscriber` removes the last subscriber, publishes with no call, and subscribes again. Plant A (a scan from the last slot) now fails. The count stays 10.
- The review verifier returns PASS: build 0 warnings and 0 errors, solution 2668 with 0 failed, each class count as planned, `scripts/test-linux.sh` PASS, the planted files byte-equal to their copies, `code-review.md` lint 1.31.
- Follow-up questions of the review (for panels-and-modes, no action here): two panels with the same command name, a release of each command and subscriber of one target, and the mosaic-ui names (Conflicts and Gaps item 1 of the plan). The defaults keep the current API.

### 2026-10-29 11:55 (/update-feature)
- The solution-architect writes the draft plan of `state-and-commands/action-map`: 7 phases, 45 tasks, phases 1 and 4 ADVANCED. It holds four open scope questions (the shape of the Core slot, the gamepad seam, the text guard, and the phase 1 demo). The architect had no shell, so task 7.7 of that plan runs the prose lint.
- The inbox holds no line. No bug to promote.
- `implementation.md` status: Ready for Review. All 27 tasks are done.
- Next: a person tests `## Needs Human Verification`, and `/autorun-epic` commits the feature.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
