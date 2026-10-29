# state-and-commands/commands-and-events - Task Checklist

**Last Updated:** 2026-10-29 11:55
**Progress:** 27/27 tasks complete (100%)
**Advanced phases:** none. Phases 1 to 5 are STANDARD (sonnet). Phase 5 escalates to ADVANCED when the gate measures bytes that the Risks table does not explain.

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (The Seam, Public API, Internal Shape, Data Flow, The Frame Order) and "Key Technical Decisions" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines, also for each namespace that a `cref` names. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces and the flat namespaces `BrutalUi.Framework` and `BrutalUi.Framework.Tests`. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. The Framework tests cannot use the internal event factories of `BrutalUi.Core.Tests`. Copy the private `Pointer` and `Key` helpers of `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` into each test file that needs them.

**No-change rule:** these files do not change: each file of `src/BrutalUi.Core/`, each file of `src/BrutalUi.Framework/State/` and `src/BrutalUi.Framework/Binding/`, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file.

---

## Phase 1: The Command Registry and the Button Bind (7/7 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 5 with `failed: 0`. The first test clicks a button bound to `save` and reads one call of the save handler with its target.
**Needs:** none
**Agent tier:** STANDARD (sonnet)

- [x] **1.1 Record the counts in context.md** ✅ 2026-10-29
  - Description: In `docs/features/state-and-commands/commands-and-events/context.md`, section "Base and Counts", record the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Framework.Tests` before any code change (expected 2624, 1733, and 49). Read each project count from a run of that project. The base commit, the Seam, the Internal Shape, and the Frame Order are already in the file.
  - File(s): `docs/features/state-and-commands/commands-and-events/context.md`
  - Estimate: 4 steps

- [x] **1.2 Write CommandHandler.cs** ✅ 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Commands/CommandHandler.cs`: `public delegate void CommandHandler(object? target);`. The summary: the handler of a command. The remarks: `CommandRegistry.Invoke` calls it with the target of the register. Pass a static method in a `static readonly` field, so the register allocates no delegate (as `Toggle.s_pointerUp`). The handler runs on the UI thread, inside the input pass for a click.
  - File(s): `src/BrutalUi.Framework/Commands/CommandHandler.cs`
  - Estimate: 3 steps

- [x] **1.3 Write CommandRegistry.cs** ✅ 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Commands/CommandRegistry.cs`: `public sealed class CommandRegistry` with `Count`, `Register`, `Contains`, and `Invoke` of the Public API, and the private struct and `IndexOf` of the Internal Shape. Phase 1 `Register` checks the handler only. The class remarks state: a command maps a UTF-8 name to a static handler and a target (Q3). The names compare by their bytes, so `Save` and `save` are two names. `Register` copies the name and can grow the array, so register at build time. `Invoke` scans the names in the order of the register. It allocates nothing and throws nothing of its own. An exception of a handler leaves `Invoke`. The registry holds each target until the game drops the registry. The registry runs on the UI thread and uses no lock. The `Invoke` remarks state: an unknown name calls no handler and gives `false`.
  - File(s): `src/BrutalUi.Framework/Commands/CommandRegistry.cs`
  - Estimate: 7 steps

- [x] **1.4 Write CommandBindings.cs** ✅ 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Commands/CommandBindings.cs`: `public static class CommandBindings` with `BindCommand` and the private `ButtonCommand` of the Internal Shape. Return the button. The remarks state: the bind copies the name and adds one delegate to `Button.Clicked` with `+=`, so a handler that the game set before the bind still runs, before the command. The bind allocates one time, at build time (A5). A click or an Enter or Space key on the focused button invokes the command. The name needs no command at the time of the bind: the registry looks it up at each click. A later write of `=` to `Clicked` removes the command, so use `+=`. The button holds the registry, and the registry holds no reference to the button. The extension form exists because `Button` lives in Core and cannot name `CommandRegistry`.
  - File(s): `src/BrutalUi.Framework/Commands/CommandBindings.cs`
  - Estimate: 6 steps

- [x] **1.5 Write CommandRegistryTests.cs (6 tests)** ✅ 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs` with 6 tests. Use a private sealed `Counter` target with an `int Calls` field and a static handler in a `static readonly` field that adds one to `((Counter)target!).Calls`. Tests: `AnInvokeCallsTheHandlerWithItsTarget`, `AnInvokeReturnsTrueForARegisteredName`, `AnUnknownNameReturnsFalseAndCallsNoHandler`, `EachNameCallsItsOwnHandler` (two names, two counters), `NamesCompareByTheirBytes` (register `save`. `Save`, `sav`, and `saves` each give `false`), and `RegisterRejectsANullHandler` (`ArgumentNullException` with the parameter name `handler`, and `Count` stays 0). `AnInvokeCallsTheHandlerWithItsTarget` also checks `Count` 1 and `Contains("save"u8)`.
  - File(s): `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs`
  - Estimate: 6 steps

- [x] **1.6 Write CommandBindingTests.cs (5 tests)** ✅ 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs` with 5 tests. Use a context with a `RecordingRenderer` measurer and a viewport of 400 by 300, a pickable root (the `AddRoot` helper of `WidgetBindingTests`), and a button of 80 by 24. Tests: `AClickOnABoundButtonInvokesTheCommand` (the demo: a press and a release at the center of the button, one `Update` before the click for the layout, then one `Update`. The counter reads 1, and `Assert.Same` checks the return value of `BindCommand`), `AnEnterOnAFocusedBoundButtonInvokesTheCommand` (`context.SetFocus(button)`, a key down with the GLFW key code 257), `AClickedHandlerOfTheGameStillRunsBeforeTheCommand` (set `Clicked` before the bind. The handler records the order. One click gives the game handler first and the command second), `ACommandThatIsRegisteredAfterTheBindRuns`, and `BindCommandRejectsANullButtonOrRegistry`.
  - File(s): `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs`
  - Estimate: 8 steps

- [x] **1.7 Run the gate of phase 1 and record the results** ✅ 2026-10-29
  - Description: Run the build, the tests, `CommandRegistryTests` (total 6), and `CommandBindingTests` (total 5). Expect `failed: 0`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/commands-and-events/context.md`
  - Estimate: 4 steps

---

## Phase 2: The Unknown Name, the Name Rules, and the Re-Entry Rules (6/6 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 10 with `failed: 0`. The test `AClickOnAnUnknownNameCallsTheUnknownCommandSlot` reads the name `load` in the slot and no handler call.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] **2.1 Add UnknownCommandHandler to CommandHandler.cs** ✅ 2026-10-29
  - Description: Edit `src/BrutalUi.Framework/Commands/CommandHandler.cs`: add `public delegate void UnknownCommandHandler(ReadOnlySpan<byte> name);`. The remarks state: the slot of the registry for a name that no command holds. The span is valid during the call only. Copy it to keep it. The slot follows `UiContext.OnFileDrop`: one delegate that the game sets one time.
  - File(s): `src/BrutalUi.Framework/Commands/CommandHandler.cs`
  - Estimate: 3 steps

- [x] **2.2 Add UnknownCommand, the name rules, and Unregister to CommandRegistry.cs** ✅ 2026-10-29
  - Description: Edit `src/BrutalUi.Framework/Commands/CommandRegistry.cs`: add the property `UnknownCommand`, and call it in `Invoke` on a miss (the Internal Shape). Add the empty-name and duplicate-name checks to `Register` (`ArgumentException` with the parameter name `name`, and a constant message with no interpolation). Add `Unregister` (the Internal Shape). Extend the remarks: a miss calls `UnknownCommand` with the name and gives `false`. A `null` slot reports through the return value only. The re-entry rule: `Invoke` copies the handler and the target before the call, so a handler can register, unregister, and invoke a command. A register in a handler can grow the array and allocate. The error rule: an exception of a handler or of the slot leaves `Invoke`, and the registry stays usable. Call `Unregister` when the target of a command closes, so the registry drops the target.
  - File(s): `src/BrutalUi.Framework/Commands/CommandRegistry.cs`
  - Estimate: 7 steps

- [x] **2.3 Add the empty-name check to CommandBindings.cs** ✅ 2026-10-29
  - Description: Edit `src/BrutalUi.Framework/Commands/CommandBindings.cs`: an empty name throws `ArgumentException` with the parameter name `name`. Extend the remarks: an unknown name at a click calls `UnknownCommand` of the registry. Two binds on one button invoke both commands, in the order of the bind. An `Unregister` of the name makes each later click a miss.
  - File(s): `src/BrutalUi.Framework/Commands/CommandBindings.cs`
  - Estimate: 4 steps

- [x] **2.4 Add 6 tests to CommandRegistryTests.cs** ✅ 2026-10-29
  - Description: Edit `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs`: add 6 tests. `AnUnknownNameCallsTheUnknownCommandSlotWithTheName` (the slot copies the span to an array. Expect the bytes `load` and one slot call), `RegisterRejectsAnEmptyOrDuplicateName` (the second register of `save` throws, and the first handler still runs), `RegisterCopiesTheName` (register from a `byte[]`, change a byte of the array, and invoke the old name: `true`), `UnregisterRemovesTheNameAndDropsTheTarget` (`true`, then `false`, then `Invoke` gives `false`, `Count` drops by one, and the other names still invoke), `AHandlerCanRegisterUnregisterAndInvokeDuringAnInvoke` (the handler of `a` registers `b`, invokes `b`, and unregisters `a`. One invoke of `a` gives one call of each. A second invoke of `a` gives `false`), and `AHandlerThatThrowsLeavesTheRegistryUsable` (the exception leaves `Invoke`, and the next invoke of another name gives `true`).
  - File(s): `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs`
  - Estimate: 7 steps

- [x] **2.5 Add 5 tests to CommandBindingTests.cs** ✅ 2026-10-29
  - Description: Edit `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs`: add 5 tests. `AClickOnAnUnknownNameCallsTheUnknownCommandSlot` (the demo), `BindCommandCopiesTheName`, `BindCommandRejectsAnEmptyName`, `TwoBindsOnOneButtonInvokeBothCommandsInOrder`, and `ACommandThatSetsAStoreShowsInTheSameUpdate` (a `Store<int>`, a label bound with `LabelBindings.BindText`, and a command whose target is the store and whose handler sets the value 7. After one click and one `Update`, the label text is `"7"u8`).
  - File(s): `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs`
  - Estimate: 7 steps

- [x] **2.6 Run the gate of phase 2 and record the results** ✅ 2026-10-29
  - Description: Run the build, the tests, `CommandRegistryTests` (total 12), and `CommandBindingTests` (total 10). Expect `failed: 0`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/commands-and-events/context.md`
  - Estimate: 4 steps

---

## Phase 3: The Event Bus (4/4 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*EventBusTests'` and reads a total of 10 with `failed: 0`. The first test publishes a `ScoreChanged` event to three subscribers and reads the score in each target, in the order of the subscribe.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] **3.1 Write EventBus.cs** ✅ 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Events/EventBus.cs` with two types. (1) `public delegate void EventBusHandler<T>(in T e, object? target);`. The remarks: pass a static method in a `static readonly` field, so the subscribe allocates no delegate. The event is a read-only reference, valid during the call only. (2) `public sealed class EventBus` with the Public API and the Internal Shape. The class remarks state: the bus keeps one subscriber list for each event type, and the type must match exactly. Use a `readonly record struct` for an event, so the publish copies no object and the caller allocates nothing. A class event allocates at each `new` of the caller. `Subscribe` and `Unsubscribe` write a new array, so call them at build time. `Publish` reads the list that holds at the start of the call: a subscribe during a publish gets the next publish, and an unsubscribe during a publish takes effect at the next publish. `Publish` allocates nothing and throws nothing of its own. An exception of a subscriber leaves `Publish`, and the later subscribers get no call for that publish. The same handler and target pair can subscribe twice and then gets two calls. `Unsubscribe` removes the first match. The bus holds each target until the unsubscribe. The bus runs on the UI thread and uses no lock.
  - File(s): `src/BrutalUi.Framework/Events/EventBus.cs`
  - Estimate: 8 steps

- [x] **3.2 Write EventBusTests.cs (10 tests)** ✅ 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Events/EventBusTests.cs` with 10 tests on a private `readonly record struct ScoreChanged(int Score)` and a private `readonly record struct Paused(bool On)`. Use a private sealed `Log` target that records the score and the order. Tests: `APublishCallsEachSubscriberWithItsTargetInOrder` (the demo), `APublishWithNoSubscriberDoesNothing` (also `SubscriberCount<ScoreChanged>()` is 0), `ASubscriberOfAnotherTypeGetsNoCall`, `UnsubscribeRemovesOneSubscriber` (`true`, then `false`, and `SubscriberCount` drops by one), `TheSamePairTwiceGetsTwoCalls` (one unsubscribe leaves one call), `ASubscribeDuringAPublishGetsTheNextPublishOnly`, `AnUnsubscribeDuringAPublishTakesEffectAtTheNextPublish` (the first subscriber removes the second. The second still gets the current event, and no later event), `APublishInsideAHandlerReachesEachSubscriber` (a `ScoreChanged` handler publishes `Paused`), `ASubscriberThatThrowsStopsThePublish` (the exception leaves `Publish`, the later subscriber gets no call, and the next publish reaches each subscriber), and `SubscribeRejectsANullHandler`.
  - File(s): `tests/BrutalUi.Framework.Tests/Events/EventBusTests.cs`
  - Estimate: 8 steps

- [x] **3.3 Run the gate of phase 3 and the generic-value grep** ✅ 2026-10-29
  - Description: Run the build, the tests, and `EventBusTests` (total 10). Expect `failed: 0`. Run `grep -nE 'is null|== null|!= null|\(object\)|Equals\(' src/BrutalUi.Framework/Events/EventBus.cs | grep -v '///'` and expect no line.
  - File(s): none (a check)
  - Estimate: 3 steps

- [x] **3.4 Record the results of phase 3** ✅ 2026-10-29
  - Description: Record the results, the demo, and the snapshot rule in `context.md`.
  - File(s): `docs/features/state-and-commands/commands-and-events/context.md`
  - Estimate: 2 steps

---

## Phase 4: The Service Registry (3/3 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ServiceRegistryTests'` and reads a total of 7 with `failed: 0`. The test `AMissingServiceGivesNullAndThrowsNothing` reads `null` for a type that no one registered.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

- [x] **4.1 Write ServiceRegistry.cs** ✅ 2026-10-29
  - Description: Write `src/BrutalUi.Framework/Services/ServiceRegistry.cs`: `public sealed class ServiceRegistry` with the Public API and the Internal Shape. The class remarks state: the registry maps an exact type to one service. `Register<IAudio>(audio)` resolves through `IAudio` only, not through the class of the service. Register at build time. `Resolve` and `TryResolve` allocate nothing and throw nothing. A missing service gives `null` or `false`. The registry holds each service until the game drops the registry. The game can register the `CommandRegistry` and the `EventBus` as services. The registry runs on the UI thread and uses no lock.
  - File(s): `src/BrutalUi.Framework/Services/ServiceRegistry.cs`
  - Estimate: 5 steps

- [x] **4.2 Write ServiceRegistryTests.cs (7 tests)** ✅ 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Services/ServiceRegistryTests.cs` with 7 tests on a private interface `IAudio` and a private sealed class `Audio : IAudio`. Tests: `AResolveGivesTheRegisteredService` (`Assert.Same`), `AnInterfaceResolvesTheServiceThatWasRegisteredAsTheInterface`, `AMissingServiceGivesNullAndThrowsNothing` (the demo), `TryResolveGivesFalseAndNullForAMissingService`, `TheTypeMustMatchExactly` (register as `Audio`, and `Resolve<IAudio>()` gives `null`), `RegisterRejectsANullServiceOrADuplicateType` (`ArgumentNullException` and `ArgumentException`, and the first service stays), and `CountShowsEachRegisteredService`.
  - File(s): `tests/BrutalUi.Framework.Tests/Services/ServiceRegistryTests.cs`
  - Estimate: 6 steps

- [x] **4.3 Run the gate of phase 4 and record the results** ✅ 2026-10-29
  - Description: Run the build, the tests, and `ServiceRegistryTests` (total 7). Expect `failed: 0`. Record the results and the demo in `context.md`.
  - File(s): `docs/features/state-and-commands/commands-and-events/context.md`
  - Estimate: 4 steps

---

## Phase 5: The 0-Byte Gate and the Docs (7/7 complete)

**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandAndEventAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the gate measures bytes that the Risks table does not explain.

*Group A (the gate):*

- [x] **5.1 Write CommandAndEventAllocationTests.cs (5 tests)** ✅ 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Commands/CommandAndEventAllocationTests.cs` with 5 tests in the shape of `StoreAllocationTests` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()` right before the measure, 100 measured iterations, `Assert.Equal(0L, after - before)`). A private sealed `Scene` builds everything before the measure: a `CommandRegistry` with 16 commands `cmd-00` to `cmd-15` (each name a `byte[]` that the scene builds, each with its own `Counter` target and one shared static handler), the unknown name `missing` as a `byte[]`, an `UnknownCommand` slot that counts misses (an instance delegate that the scene creates one time), a context with a `RecordingRenderer` measurer and a viewport of 400 by 300, a pickable root, and a button of 80 by 24 bound to `cmd-07`. It also holds an `EventBus` with 8 subscribers of a `readonly record struct ScoreChanged(int Score)` (each with its own target that records the last score and a count), 1 subscriber of a `readonly record struct Paused(bool On)`, and no subscriber of a third type `Unheard`. It also holds a `ServiceRegistry` with 3 services and counters for the hits and the misses. Bodies: `InvokeAllocatesNothing` (each iteration invokes `cmd-00`, `cmd-15`, and `missing`), `AClickOnABoundButtonAllocatesNothing` (each frame pushes a press and a release at the center of the button with no target, then runs `Update` and `Draw`), `PublishAllocatesNothing` (each iteration publishes `ScoreChanged` with the round as the score, `Paused`, and `Unheard`), `ResolveAllocatesNothing` (each iteration calls `Resolve` and `TryResolve` of two registered types and `Resolve` of one missing type), and `TheMeasuredBodyDoesRealWork`. The real-work test reads each count and the round before each body, runs one step of each body, and asserts fixed increments after it: `cmd-00` +1, `cmd-15` +1, misses +1, `cmd-07` +1 for the click, each of the 8 score targets +1 with the score of the new round, `Paused` +1, resolve hits +4, and resolve misses +1 (lesson 3 of element-bindings). The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, no array, and no `Assert`.
  - File(s): `tests/BrutalUi.Framework.Tests/Commands/CommandAndEventAllocationTests.cs`
  - Estimate: 10 steps

- [x] **5.2 Run the gate class in Debug** ✅ 2026-10-29
  - Description: Run the build, the tests, and the class of task 5.1. Expect a total of 5 with `failed: 0` in the default Debug configuration. When a gate measures bytes, stop and escalate (the agent tier line).
  - File(s): none (a check)
  - Estimate: 3 steps

- [x] **5.3 Run the four plants** ✅ 2026-10-29
  - Description: Copy each file to the scratchpad first. Plant 1: `GC.KeepAlive(new object());` as the first line of `CommandRegistry.Invoke`. Plant 2: the same line as the first line of `EventBus.Publish`. Plant 3: the same line as the first line of `ServiceRegistry.TryResolve`. Plant 4: an empty body of the click frame method of the gate. For each plant, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Record which tests fail and the bytes (plant 4 fails `TheMeasuredBodyDoesRealWork`). Restore each file and check it with `cmp`. Run the tests again and expect 0 failed. Record the results in `context.md`.
  - File(s): `docs/features/state-and-commands/commands-and-events/context.md` (the plants touch `CommandRegistry.cs`, `EventBus.cs`, `ServiceRegistry.cs`, and `CommandAndEventAllocationTests.cs` and restore them)
  - Estimate: 10 steps

*Group B (the docs):*

- [x] **5.4 Edit section 10 of the technical design** ✅ 2026-10-29
  - Description: Edit section 10 of `docs/technical-design.md`. In the Components table, change the `Change` cell of the row `EventBus`, `ServiceRegistry`, `CommandRegistry`, `ModeHistory` to: copy, with a static handler plus a target for each command and each subscriber (Q3). `ModeHistory` waits for panels-and-modes. After "Data binding", add a subsection "Commands, events, and services" with the rules of this feature: the command registry (the UTF-8 name, the byte compare, the static handler plus target, `Invoke` and its result, the `UnknownCommand` slot, the name rules, `Unregister`, the re-entry rule, and the error rule), the button bind (`+=` on `Clicked`, the build-time allocation, and the late register), the event bus (the exact type, the snapshot rule, the copy on write, and the error rule), and the service registry (the exact type, `null` and `false` for a missing service).
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] **5.5 Edit section 14 of the technical design** ✅ 2026-10-29
  - Description: Edit section 14 of `docs/technical-design.md`: add a bullet for the registry tests (`CommandRegistryTests`, `CommandBindingTests`, `EventBusTests`, `ServiceRegistryTests`) and a bullet "A sixteenth allocation gate" for `CommandAndEventAllocationTests`, with the scene and the plant results of task 5.3. Check the ordinal first: the last gate bullet must be "A fifteenth allocation gate".
  - File(s): `docs/technical-design.md`
  - Estimate: 4 steps

- [x] **5.6 Edit CHANGELOG.md** ✅ 2026-10-29
  - Description: Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `CommandRegistry`, `CommandHandler`, `UnknownCommandHandler`, `CommandBindings.BindCommand`, `EventBus`, `EventBusHandler<T>`, `ServiceRegistry`, and the command and event allocation gate (state-and-commands/commands-and-events).
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] **5.7 Run the test script and the prose lint** ✅ 2026-10-29
  - Description: Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, and `context.md`. Record the results and each decision that this plan did not settle in `context.md`.
  - File(s): `docs/features/state-and-commands/commands-and-events/context.md`
  - Estimate: 5 steps

---

## Session Notes

**Completed This Session (2026-10-29):** tasks 1.1 to 5.7 (27 of 27), the cross-phase review (CR-1 and CR-2 fixed in review groups 1 and 2), and the draft plan of `state-and-commands/action-map`.

**Discovered New Tasks:** none. The two review findings changed two test files and kept each test count.

---

*Mark each task `[x]` with `✅` and the date when it is complete. Update the Progress line and the phase counts.*
