# Commands and Events - Implementation Plan

**Epic:** state-and-commands (feature 3 of 4)
**Status:** Planning
**Started:** 2026-10-29
**Target Completion:** TBD
**Last Updated:** 2026-10-29

---

## Quality Bar

**Target quality:** A library feature of the frame loop. The three registries are the hub of each later Framework feature: action-map calls the command registry for each key, panels-and-modes publishes on the event bus and reads services, and xml-markup binds a `command="..."` attribute. A lost click, a wrong handler, an exception in a frame, a hidden allocation, or a leak through a held target breaks each game that uses them.

- **One click, one invoke:** A click or an Enter key on a bound button calls the handler of its command one time, with its target. A name that no command holds calls no handler, throws nothing, and reports the name. A test pins each rule.
- **One publish, one call for each subscriber:** A publish calls each subscriber of its event type one time, in the order of the subscribe. A subscribe or an unsubscribe during a publish takes effect at the next publish. A test pins each rule.
- **No exception in the frame loop:** `CommandRegistry.Invoke`, `EventBus.Publish`, `ServiceRegistry.Resolve`, and `ServiceRegistry.TryResolve` throw nothing of their own. Only a handler of the game can throw, and the exception leaves the call, as in the input pass. The build-time methods (`Register`, `Subscribe`, `BindCommand`) throw for a bad argument.
- **0 bytes after warm-up:** `CommandAndEventAllocationTests` (the sixteenth allocation gate) measures 0 bytes for an invoke by name, an unknown name, a click on a bound button through `Update` and `Draw`, a publish to many subscribers, and a resolve. The gate runs in the default Debug build. A planted allocation in `Invoke`, `Publish`, and `TryResolve` each fails it.
- **The Core and Framework split:** `BrutalUi.Core` does not change. Each new type lives in `BrutalUi.Framework`. No project file changes.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the build-time rule, the handler rule (a static method in a `static readonly` field), the re-entry rule, the error rule, and the reference rule (the registry holds the target until an unregister).

---

## Executive Summary

This feature adds the three registries of the mosaic-ui model to `BrutalUi.Framework`, and a command bind on `Button`. A game writes `registry.Register("save"u8, s_save, game)` and `button.BindCommand(registry, "save"u8)` at build time. A click on the button then calls `registry.Invoke("save"u8)` in the input pass of `UiContext.Update`. The registry finds the name by a byte compare and calls the static handler with its target. action-map later calls the same `Invoke` for a key (Q3, A1).

`CommandRegistry` holds a plain array of a private struct (the UTF-8 name, a static `CommandHandler`, and a target object), in the shape of `ElementBinding`. `Invoke` returns `false` for an unknown name and calls the slot `UnknownCommand` with the name, so a button click on a missing name also reports. `EventBus` holds one subscriber array for each event type. `Publish<T>(in T e)` reads the array and calls each static `EventBusHandler<T>` with the event and its target. A subscribe and an unsubscribe write a new array (copy on write), so a change during a publish cannot break the loop. `ServiceRegistry` maps an exact type to one service. `Resolve<T>` gives `null` for a missing service, and `TryResolve<T>` gives `false`.

The plan has 5 phases and 27 tasks. Phase 1 delivers the command registry and the button bind on the main path. Phase 2 delivers the unknown-name slot, the name rules, `Unregister`, and the re-entry rules. Phase 3 delivers the event bus. Phase 4 delivers the service registry. Phase 5 delivers the 0-byte gate and the docs. No phase changes `BrutalUi.Core`.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (registers a command, binds a button to it, subscribes to a typed event, and registers a service at build time, and pays no allocation in the frame after that)
- [x] **The sibling feature agents** (action-map invokes a command by name for each key, mouse button, and gamepad button. panels-and-modes publishes events, reads services, and unregisters the commands of a closed panel. xml-markup reads `command="..."` and calls `BindCommand`)
- [x] **The maintainer** (runs one more 0-byte gate through `scripts/test-linux.sh` and `scripts/test-windows.ps1`, with no script change)
- [ ] **The markup author** (not served. The xml-markup epic reads the `command` attribute)

**User Subsets Served:**
- [x] **Simple** - one command `save` with a static handler, and one button bound to it.
- [x] **Complex** - 16 commands, an unknown-name report, two buttons on one command, a command that writes a store that a bound label shows in the same `Update`, an event bus with 8 subscribers on one type, and a service registry that holds the bus and the command registry.

**UX Approach:**
- One register call and one bind call at build time. No closure, no `IDisposable`, and no event keyword.
- `BindCommand` returns the button, so it chains with `ElementBuilder.Class` and `ElementBuilder.Child`.

---

## Goals

### Primary Goals
1. Add `CommandRegistry` with a UTF-8 name, a static handler, and a target, and an `Invoke` that allocates nothing and throws nothing of its own.
2. Add `CommandBindings.BindCommand`, which connects `Button.Clicked` to a command name at build time.
3. Report an unknown name through the return value and through the slot `UnknownCommand`.
4. Add `EventBus` with a typed publish that allocates nothing after warm-up.
5. Add `ServiceRegistry` with a resolve by type that gives `null` or `false` for a missing service.
6. Prove 0 bytes after warm-up for invoke, click, publish, and resolve with the sixteenth allocation gate.

### Success Criteria
- [ ] The phase 1 demo passes: `CommandBindingTests` clicks a button bound to `save` and reads one call of the save handler with its target.
- [ ] `CommandRegistryTests`, `CommandBindingTests`, `EventBusTests`, `ServiceRegistryTests`, and `CommandAndEventAllocationTests` pass.
- [ ] `CommandAndEventAllocationTests` measures 0 bytes in Debug. A planted allocation in `Invoke`, `Publish`, and `TryResolve` each fails it.
- [ ] Each existing test passes with no edit.
- [ ] No file of `src/BrutalUi.Core/` and no project file changes.

---

## Architecture Overview

### The Seam

`Button` lives in Core, and Core does not reference Framework. The bind thus lives in Framework as an extension method, as `LabelBindings` and `WidgetBindings` do. `Button.Clicked` is an `Action<Button>`, a delegate with no target slot. The bind therefore creates one small target object (the registry and a copy of the name) and adds one instance delegate of that object to `Clicked`. This is the one-time build allocation that A5 allows. The click path then runs the delegate, the registry compares bytes, and the registry calls a static handler with its target. Nothing in that path allocates.

The registry itself follows Q3: a command is a static handler plus a target, as `BindingHandler` plus its target object in `ElementBinding`. The game passes a static method in a `static readonly` field, so the register allocates no delegate for each command.

### File Tree (end state)

```
src/BrutalUi.Framework/
├── Commands/CommandHandler.cs                   # phase 1: delegate CommandHandler. phase 2: delegate UnknownCommandHandler
├── Commands/CommandRegistry.cs                  # phase 1: Register, Invoke, Contains, Count. phase 2: the name rules, Unregister, UnknownCommand
├── Commands/CommandBindings.cs                  # phase 1: BindCommand. phase 2: the empty-name check and the remarks
├── Events/EventBus.cs                           # phase 3: delegate EventBusHandler<T>, class EventBus
└── Services/ServiceRegistry.cs                  # phase 4
tests/BrutalUi.Framework.Tests/
├── Commands/CommandRegistryTests.cs             # phase 1 (6), phase 2 (+6) = 12 tests
├── Commands/CommandBindingTests.cs              # phase 1 (5), phase 2 (+5) = 10 tests
├── Commands/CommandAndEventAllocationTests.cs   # phase 5 (5 tests)
├── Events/EventBusTests.cs                      # phase 3 (10 tests)
└── Services/ServiceRegistryTests.cs             # phase 4 (7 tests)
docs/technical-design.md                         # phase 5: sections 10 and 14
CHANGELOG.md                                     # phase 5
docs/features/state-and-commands/commands-and-events/context.md   # each phase
```

These files do not change: each file of `src/BrutalUi.Core/`, each file of `src/BrutalUi.Framework/State/` and `src/BrutalUi.Framework/Binding/`, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file. The Framework test project already links `RecordingRenderer.cs`, and it compiles each new test file with no edit. Namespaces stay flat: `BrutalUi.Framework` and `BrutalUi.Framework.Tests`.

### Public API (end state)

```csharp
namespace BrutalUi.Framework;

public delegate void CommandHandler(object? target);                          // phase 1
public delegate void UnknownCommandHandler(ReadOnlySpan<byte> name);           // phase 2

public sealed class CommandRegistry
{
    public CommandRegistry();
    public int Count { get; }                                                   // phase 1
    public void Register(ReadOnlySpan<byte> name, CommandHandler handler, object? target); // phase 1. phase 2: the empty and duplicate checks
    public bool Contains(ReadOnlySpan<byte> name);                              // phase 1
    public bool Invoke(ReadOnlySpan<byte> name);                                // phase 1. phase 2: calls UnknownCommand on a miss
    public bool Unregister(ReadOnlySpan<byte> name);                            // phase 2
    public UnknownCommandHandler? UnknownCommand { get; set; }                  // phase 2
}

public static class CommandBindings
{
    public static Button BindCommand(this Button button, CommandRegistry registry, ReadOnlySpan<byte> name); // phase 1
}

public delegate void EventBusHandler<T>(in T e, object? target);              // phase 3

public sealed class EventBus
{
    public EventBus();
    public void Subscribe<T>(EventBusHandler<T> handler, object? target);       // phase 3
    public bool Unsubscribe<T>(EventBusHandler<T> handler, object? target);     // phase 3
    public void Publish<T>(in T e);                                             // phase 3
    public int SubscriberCount<T>();                                            // phase 3
}

public sealed class ServiceRegistry
{
    public ServiceRegistry();
    public int Count { get; }                                                   // phase 4
    public void Register<T>(T service) where T : class;                         // phase 4
    public T? Resolve<T>() where T : class;                                     // phase 4
    public bool TryResolve<T>([NotNullWhen(true)] out T? service) where T : class; // phase 4
}
```

### Internal Shape

- `CommandRegistry`: `private CommandEntry[]? _entries;` (null until the first register, then 8 slots, then doubling) and `private int _count;`. `private struct CommandEntry` holds `byte[] Name` (a copy of the name), `CommandHandler Handler`, and `object? Target`. `private int IndexOf(ReadOnlySpan<byte> name)` scans from 0 to `_count - 1` and compares with `Name.AsSpan().SequenceEqual(name)`. It gives -1 for no match.
- `Invoke(name)`: `int i = IndexOf(name)`. On -1, call `UnknownCommand?.Invoke(name)` (phase 2) and return `false`. Else copy `Handler` and `Target` of the slot to locals, call `handler(target)`, and return `true`. The copy comes before the call, so a handler that registers or unregisters a command does not change the call.
- `Register`: `ArgumentNullException.ThrowIfNull(handler)`. Phase 2 adds: an empty name throws `ArgumentException`, and a name that the registry holds throws `ArgumentException`. Grow the array when it is full, then write the slot with `name.ToArray()`.
- `Unregister(name)` (phase 2): find the slot, move each later slot down by one, write `default` into the old last slot (so the registry drops the target), and lower the count. Return `false` for an unknown name.
- `CommandBindings.BindCommand`: check the button and the registry with `ArgumentNullException.ThrowIfNull`. Phase 2 adds: an empty name throws `ArgumentException`. Create `ButtonCommand target = new(registry, name.ToArray())`, then `button.Clicked += target.Run;`, and return the button. `private sealed class ButtonCommand` holds the registry and the name, and `internal void Run(Button button) => _registry.Invoke(_name);`.
- `EventBus`: `private readonly Dictionary<Type, object> _channels = new();`. `private sealed class Channel<T>` holds `internal Subscriber<T>[] Items = [];`. `private readonly struct Subscriber<T>` holds `EventBusHandler<T> Handler` and `object? Target`. Only `Subscribe` creates a channel. `Publish<T>`: `if (!_channels.TryGetValue(typeof(T), out object? c)) return;`, then `Subscriber<T>[] items = ((Channel<T>)c).Items;`, then call `items[i].Handler(in e, items[i].Target)` for each slot. The local `items` is the snapshot of the publish.
- `Subscribe<T>`: `ArgumentNullException.ThrowIfNull(handler)`. Get or create the channel. Write a new array of the old length plus one, with the new subscriber last. `Unsubscribe<T>`: find the first slot with `Handler == handler` (delegate equality) and `ReferenceEquals(Target, target)`. Write a new array without that slot. Return `false` when no slot matches. The code of `EventBus` holds no `is null`, `== null`, `(object)` cast, or `Equals` call on a value of type `T` (lesson 1 of element-bindings).
- `ServiceRegistry`: `private readonly Dictionary<Type, object> _services = new();`. `Register<T>`: `ArgumentNullException.ThrowIfNull(service)`. A type that the registry holds throws `ArgumentException`. Store under `typeof(T)`. `TryResolve<T>`: `TryGetValue(typeof(T), out object? s)`, then `service = (T)s` on a hit, and `service = null` on a miss. `Resolve<T>` calls `TryResolve<T>` and returns the service or `null`.

### Data Flow

```
Build time (allocates one time):
  registry.Register("save"u8, s_save, game)
    _entries[n] = { Name = copy of "save", Handler = s_save, Target = game }
  button.BindCommand(registry, "save"u8)
    new ButtonCommand(registry, copy of "save") ──► button.Clicked += target.Run
  bus.Subscribe<ScoreChanged>(s_onScore, hud)
    _channels[typeof(ScoreChanged)].Items = old items + { s_onScore, hud }
  services.Register<EventBus>(bus)
    _services[typeof(EventBus)] = bus

UiContext.Update(dt):
  1. Input pass (DrainEvents)
       PointerUp on the button ──► Button.HandlePointerUp ──► Click ──► Clicked(button)
         ──► ButtonCommand.Run ──► registry.Invoke("save")
               IndexOf: byte compare ──► hit: s_save(game) ──► true
                                     ──► miss: UnknownCommand?("save") ──► false
       (a handler can call store.Set or bus.Publish here)
  2. Binding pass ── a store that the command wrote shows in the same Update
  3. Style pass ── 4. Transition pass ── 5. Layout pass ── 6. Caret pass ── 7. Clear pass

Any time on the UI thread:
  bus.Publish(new ScoreChanged(10, 3)) ──► items snapshot ──► s_onScore(in e, hud) for each subscriber
  services.Resolve<EventBus>() ──► bus, or null

action-map (feature 4, later):
  key ──► UiActionMap ──► registry.Invoke(name)   (the same call as a click)
```

### The Frame Order

This feature does not change `UiContext.Update`. A button click runs in the input pass, so its command runs there. A command that writes a store shows in the binding pass of the same `Update` (element-bindings). Phase 2 pins this with a test. The event bus and the service registry have no pass. The game calls them from a handler or from its own frame code.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The Confirmed Assumptions do not name the demo of phase 1, so Notes question 3 holds it (Slice rule 12).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines, also for each namespace that a `cref` names. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. The Framework tests cannot use the internal event factories of `BrutalUi.Core.Tests`. Copy the private `Pointer` and `Key` helpers of `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` into each test file that needs them.

### Phase 1: The Command Registry and the Button Bind
**Goal:** The smallest path that proves the idea: a game registers a command with a static handler and a target, binds a button to the command name, and a click on the button calls the handler one time with its target. A direct `Invoke` of an unknown name returns `false` and throws nothing.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 5 with `failed: 0`. The first test clicks a button bound to `save` and reads one call of the save handler with its target.
**Needs:** none
**Agent tier:** STANDARD (sonnet). The phase adds new Framework files only and changes no Core file.

**Tasks:**
- [ ] 1.1 Create `docs/features/state-and-commands/commands-and-events/context.md`. Check that the element-bindings commit exists (`git log --oneline -3`). Record the base commit (`git rev-parse HEAD`), and the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Framework.Tests` (expected 2624, 1733, and 49 when element-bindings is the base). Record the Seam, the Internal Shape, and the Frame Order of the Architecture Overview.
- [ ] 1.2 Write `src/BrutalUi.Framework/Commands/CommandHandler.cs`: `public delegate void CommandHandler(object? target);`. The summary: the handler of a command. The remarks: `CommandRegistry.Invoke` calls it with the target of the register. Pass a static method in a `static readonly` field, so the register allocates no delegate (as `Toggle.s_pointerUp`). The handler runs on the UI thread, inside the input pass for a click.
- [ ] 1.3 Write `src/BrutalUi.Framework/Commands/CommandRegistry.cs`: `public sealed class CommandRegistry` with `Count`, `Register`, `Contains`, and `Invoke` of the Public API, and the private struct and `IndexOf` of the Internal Shape. Phase 1 `Register` checks the handler only. The class remarks state: a command maps a UTF-8 name to a static handler and a target (Q3). The names compare by their bytes, so `Save` and `save` are two names. `Register` copies the name and can grow the array, so register at build time. `Invoke` scans the names in the order of the register. It allocates nothing and throws nothing of its own. An exception of a handler leaves `Invoke`. The registry holds each target until the game drops the registry. The registry runs on the UI thread and uses no lock. The `Invoke` remarks state: an unknown name calls no handler and gives `false`.
- [ ] 1.4 Write `src/BrutalUi.Framework/Commands/CommandBindings.cs`: `public static class CommandBindings` with `BindCommand` and the private `ButtonCommand` of the Internal Shape. Return the button. The remarks state: the bind copies the name and adds one delegate to `Button.Clicked` with `+=`, so a handler that the game set before the bind still runs, before the command. The bind allocates one time, at build time (A5). A click or an Enter or Space key on the focused button invokes the command. The name needs no command at the time of the bind: the registry looks it up at each click. A later write of `=` to `Clicked` removes the command, so use `+=`. The button holds the registry, and the registry holds no reference to the button. The extension form exists because `Button` lives in Core and cannot name `CommandRegistry`.
- [ ] 1.5 Write `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs` with 6 tests. Use a private sealed `Counter` target with an `int Calls` field and a static handler in a `static readonly` field that adds one to `((Counter)target!).Calls`. Tests: `AnInvokeCallsTheHandlerWithItsTarget`, `AnInvokeReturnsTrueForARegisteredName`, `AnUnknownNameReturnsFalseAndCallsNoHandler`, `EachNameCallsItsOwnHandler` (two names, two counters), `NamesCompareByTheirBytes` (register `save`. `Save`, `sav`, and `saves` each give `false`), and `RegisterRejectsANullHandler` (`ArgumentNullException` with the parameter name `handler`, and `Count` stays 0). `AnInvokeCallsTheHandlerWithItsTarget` also checks `Count` 1 and `Contains("save"u8)`.
- [ ] 1.6 Write `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs` with 5 tests. Use a context with a `RecordingRenderer` measurer and a viewport of 400 by 300, a pickable root (the `AddRoot` helper of `WidgetBindingTests`), and a button of 80 by 24. Tests: `AClickOnABoundButtonInvokesTheCommand` (the demo: a press and a release at the center of the button, one `Update` before the click for the layout, then one `Update`. The counter reads 1, and `Assert.Same` checks the return value of `BindCommand`), `AnEnterOnAFocusedBoundButtonInvokesTheCommand` (`context.SetFocus(button)`, a key down with the GLFW key code 257), `AClickedHandlerOfTheGameStillRunsBeforeTheCommand` (set `Clicked` before the bind. The handler records the order. One click gives the game handler first and the command second), `ACommandThatIsRegisteredAfterTheBindRuns`, and `BindCommandRejectsANullButtonOrRegistry`.
- [ ] 1.7 Run the build, the tests, `CommandRegistryTests` (total 6), and `CommandBindingTests` (total 5). Expect `failed: 0`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 40 (7 tasks, about six steps for one task)

**Files (6):** `CommandHandler.cs`, `CommandRegistry.cs`, `CommandBindings.cs`, `CommandRegistryTests.cs`, `CommandBindingTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `CommandRegistryTests` and the 5 tests of `CommandBindingTests` pass.
- [ ] No file of `src/BrutalUi.Core/` and no project file has a diff against the base commit.
- [ ] `BindCommand` adds to `Clicked` with `+=` and does not replace it.

---

### Phase 2: The Unknown Name, the Name Rules, and the Re-Entry Rules
**Goal:** A click on a button bound to an unknown name reports the name through the slot `UnknownCommand`. A register rejects an empty name and a name that the registry holds. `Unregister` frees a name and drops its target. A handler can register, unregister, and invoke during an invoke. A command that writes a store shows in the same `Update`.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 10 with `failed: 0`. The test `AClickOnAnUnknownNameCallsTheUnknownCommandSlot` reads the name `load` in the slot and no handler call.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Edit `src/BrutalUi.Framework/Commands/CommandHandler.cs`: add `public delegate void UnknownCommandHandler(ReadOnlySpan<byte> name);`. The remarks state: the slot of the registry for a name that no command holds. The span is valid during the call only. Copy it to keep it. The slot follows `UiContext.OnFileDrop`: one delegate that the game sets one time.
- [ ] 2.2 Edit `src/BrutalUi.Framework/Commands/CommandRegistry.cs`: add the property `UnknownCommand`, and call it in `Invoke` on a miss (the Internal Shape). Add the empty-name and duplicate-name checks to `Register` (`ArgumentException` with the parameter name `name`, and a constant message with no interpolation). Add `Unregister` (the Internal Shape). Extend the remarks: a miss calls `UnknownCommand` with the name and gives `false`. A `null` slot reports through the return value only. The re-entry rule: `Invoke` copies the handler and the target before the call, so a handler can register, unregister, and invoke a command. A register in a handler can grow the array and allocate. The error rule: an exception of a handler or of the slot leaves `Invoke`, and the registry stays usable. Call `Unregister` when the target of a command closes, so the registry drops the target.
- [ ] 2.3 Edit `src/BrutalUi.Framework/Commands/CommandBindings.cs`: an empty name throws `ArgumentException` with the parameter name `name`. Extend the remarks: an unknown name at a click calls `UnknownCommand` of the registry. Two binds on one button invoke both commands, in the order of the bind. An `Unregister` of the name makes each later click a miss.
- [ ] 2.4 Edit `tests/BrutalUi.Framework.Tests/Commands/CommandRegistryTests.cs`: add 6 tests. `AnUnknownNameCallsTheUnknownCommandSlotWithTheName` (the slot copies the span to an array. Expect the bytes `load` and one slot call), `RegisterRejectsAnEmptyOrDuplicateName` (the second register of `save` throws, and the first handler still runs), `RegisterCopiesTheName` (register from a `byte[]`, change a byte of the array, and invoke the old name: `true`), `UnregisterRemovesTheNameAndDropsTheTarget` (`true`, then `false`, then `Invoke` gives `false`, `Count` drops by one, and the other names still invoke), `AHandlerCanRegisterUnregisterAndInvokeDuringAnInvoke` (the handler of `a` registers `b`, invokes `b`, and unregisters `a`. One invoke of `a` gives one call of each. A second invoke of `a` gives `false`), and `AHandlerThatThrowsLeavesTheRegistryUsable` (the exception leaves `Invoke`, and the next invoke of another name gives `true`).
- [ ] 2.5 Edit `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs`: add 5 tests. `AClickOnAnUnknownNameCallsTheUnknownCommandSlot` (the demo), `BindCommandCopiesTheName`, `BindCommandRejectsAnEmptyName`, `TwoBindsOnOneButtonInvokeBothCommandsInOrder`, and `ACommandThatSetsAStoreShowsInTheSameUpdate` (a `Store<int>`, a label bound with `LabelBindings.BindText`, and a command whose target is the store and whose handler sets the value 7. After one click and one `Update`, the label text is `"7"u8`).
- [ ] 2.6 Run the build, the tests, `CommandRegistryTests` (total 12), and `CommandBindingTests` (total 10). Expect `failed: 0`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 35 (6 tasks, about six steps for one task)

**Files (6):** `CommandHandler.cs`, `CommandRegistry.cs`, `CommandBindings.cs`, `CommandRegistryTests.cs`, `CommandBindingTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 12 tests of `CommandRegistryTests` and the 10 tests of `CommandBindingTests` pass.
- [ ] The body of `Invoke` holds no `throw` statement.
- [ ] `ACommandThatSetsAStoreShowsInTheSameUpdate` passes with no change to `UiContext`.

---

### Phase 3: The Event Bus
**Goal:** A game subscribes static handlers with targets to a typed event, and a publish calls each subscriber one time in the order of the subscribe. A subscribe or an unsubscribe during a publish takes effect at the next publish. A publish of a type with no subscriber does nothing.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*EventBusTests'` and reads a total of 10 with `failed: 0`. The first test publishes a `ScoreChanged` event to three subscribers and reads the score in each target, in the order of the subscribe.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Write `src/BrutalUi.Framework/Events/EventBus.cs` with two types. (1) `public delegate void EventBusHandler<T>(in T e, object? target);`. The remarks: pass a static method in a `static readonly` field, so the subscribe allocates no delegate. The event is a read-only reference, valid during the call only. (2) `public sealed class EventBus` with the Public API and the Internal Shape. The class remarks state: the bus keeps one subscriber list for each event type, and the type must match exactly. Use a `readonly record struct` for an event, so the publish copies no object and the caller allocates nothing. A class event allocates at each `new` of the caller. `Subscribe` and `Unsubscribe` write a new array, so call them at build time. `Publish` reads the list that holds at the start of the call: a subscribe during a publish gets the next publish, and an unsubscribe during a publish takes effect at the next publish. `Publish` allocates nothing and throws nothing of its own. An exception of a subscriber leaves `Publish`, and the later subscribers get no call for that publish. The same handler and target pair can subscribe twice and then gets two calls. `Unsubscribe` removes the first match. The bus holds each target until the unsubscribe. The bus runs on the UI thread and uses no lock.
- [ ] 3.2 Write `tests/BrutalUi.Framework.Tests/Events/EventBusTests.cs` with 10 tests on a private `readonly record struct ScoreChanged(int Score)` and a private `readonly record struct Paused(bool On)`. Use a private sealed `Log` target that records the score and the order. Tests: `APublishCallsEachSubscriberWithItsTargetInOrder` (the demo), `APublishWithNoSubscriberDoesNothing` (also `SubscriberCount<ScoreChanged>()` is 0), `ASubscriberOfAnotherTypeGetsNoCall`, `UnsubscribeRemovesOneSubscriber` (`true`, then `false`, and `SubscriberCount` drops by one), `TheSamePairTwiceGetsTwoCalls` (one unsubscribe leaves one call), `ASubscribeDuringAPublishGetsTheNextPublishOnly`, `AnUnsubscribeDuringAPublishTakesEffectAtTheNextPublish` (the first subscriber removes the second. The second still gets the current event, and no later event), `APublishInsideAHandlerReachesEachSubscriber` (a `ScoreChanged` handler publishes `Paused`), `ASubscriberThatThrowsStopsThePublish` (the exception leaves `Publish`, the later subscriber gets no call, and the next publish reaches each subscriber), and `SubscribeRejectsANullHandler`.
- [ ] 3.3 Run the build, the tests, and `EventBusTests` (total 10). Expect `failed: 0`. Run `grep -nE 'is null|== null|!= null|\(object\)|Equals\(' src/BrutalUi.Framework/Events/EventBus.cs | grep -v '///'` and expect no line.
- [ ] 3.4 Record the results, the demo, and the snapshot rule in `context.md`.

**Estimated Steps:** about 25 (4 tasks, about six steps for one task)

**Files (3):** `EventBus.cs`, `EventBusTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `EventBusTests` pass.
- [ ] `EventBus.cs` holds no null check, no `(object)` cast, and no `Equals` call on a value of type `T`.

---

### Phase 4: The Service Registry
**Goal:** A game registers a service under a type and resolves it by the same type. A missing service gives `null` from `Resolve` and `false` from `TryResolve`, with no exception.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ServiceRegistryTests'` and reads a total of 7 with `failed: 0`. The test `AMissingServiceGivesNullAndThrowsNothing` reads `null` for a type that no one registered.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 4.1 Write `src/BrutalUi.Framework/Services/ServiceRegistry.cs`: `public sealed class ServiceRegistry` with the Public API and the Internal Shape. The class remarks state: the registry maps an exact type to one service. `Register<IAudio>(audio)` resolves through `IAudio` only, not through the class of the service. Register at build time. `Resolve` and `TryResolve` allocate nothing and throw nothing. A missing service gives `null` or `false`. The registry holds each service until the game drops the registry. The game can register the `CommandRegistry` and the `EventBus` as services. The registry runs on the UI thread and uses no lock.
- [ ] 4.2 Write `tests/BrutalUi.Framework.Tests/Services/ServiceRegistryTests.cs` with 7 tests on a private interface `IAudio` and a private sealed class `Audio : IAudio`. Tests: `AResolveGivesTheRegisteredService` (`Assert.Same`), `AnInterfaceResolvesTheServiceThatWasRegisteredAsTheInterface`, `AMissingServiceGivesNullAndThrowsNothing` (the demo), `TryResolveGivesFalseAndNullForAMissingService`, `TheTypeMustMatchExactly` (register as `Audio`, and `Resolve<IAudio>()` gives `null`), `RegisterRejectsANullServiceOrADuplicateType` (`ArgumentNullException` and `ArgumentException`, and the first service stays), and `CountShowsEachRegisteredService`.
- [ ] 4.3 Run the build, the tests, and `ServiceRegistryTests` (total 7). Expect `failed: 0`. Record the results and the demo in `context.md`.

**Estimated Steps:** about 18 (3 tasks, about six steps for one task)

**Files (3):** `ServiceRegistry.cs`, `ServiceRegistryTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 7 tests of `ServiceRegistryTests` pass.
- [ ] `Resolve` and `TryResolve` hold no `throw` statement.

---

### Phase 5: The 0-Byte Gate and the Docs
**Goal:** `CommandAndEventAllocationTests` (the sixteenth allocation gate) proves 0 bytes after warm-up for an invoke by name, an unknown name, a click on a bound button through `Update` and `Draw`, a publish, and a resolve, in the Debug build. The technical design and the changelog state the three registries and the gate.
**Demo:** a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandAndEventAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet). Escalate to ADVANCED when the gate measures bytes that the Risks table does not explain (the element-bindings precedent: a Debug box in generic code).

**Tasks (group A: the gate):**
- [ ] 5.1 Write `tests/BrutalUi.Framework.Tests/Commands/CommandAndEventAllocationTests.cs` with 5 tests in the shape of `StoreAllocationTests` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()` right before the measure, 100 measured iterations, `Assert.Equal(0L, after - before)`). A private sealed `Scene` builds everything before the measure: a `CommandRegistry` with 16 commands `cmd-00` to `cmd-15` (each name a `byte[]` that the scene builds, each with its own `Counter` target and one shared static handler), the unknown name `missing` as a `byte[]`, an `UnknownCommand` slot that counts misses (an instance delegate that the scene creates one time), a context with a `RecordingRenderer` measurer and a viewport of 400 by 300, a pickable root, and a button of 80 by 24 bound to `cmd-07`. It also holds an `EventBus` with 8 subscribers of a `readonly record struct ScoreChanged(int Score)` (each with its own target that records the last score and a count), 1 subscriber of a `readonly record struct Paused(bool On)`, and no subscriber of a third type `Unheard`. It also holds a `ServiceRegistry` with 3 services and counters for the hits and the misses. Bodies: `InvokeAllocatesNothing` (each iteration invokes `cmd-00`, `cmd-15`, and `missing`), `AClickOnABoundButtonAllocatesNothing` (each frame pushes a press and a release at the center of the button with no target, then runs `Update` and `Draw`), `PublishAllocatesNothing` (each iteration publishes `ScoreChanged` with the round as the score, `Paused`, and `Unheard`), `ResolveAllocatesNothing` (each iteration calls `Resolve` and `TryResolve` of two registered types and `Resolve` of one missing type), and `TheMeasuredBodyDoesRealWork`. The real-work test reads each count and the round before each body, runs one step of each body, and asserts fixed increments after it: `cmd-00` +1, `cmd-15` +1, misses +1, `cmd-07` +1 for the click, each of the 8 score targets +1 with the score of the new round, `Paused` +1, resolve hits +4, and resolve misses +1 (lesson 3 of element-bindings). The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, no array, and no `Assert`.
- [ ] 5.2 Run the build, the tests, and the class of task 5.1. Expect a total of 5 with `failed: 0` in the default Debug configuration. When a gate measures bytes, stop and escalate (the agent tier line).
- [ ] 5.3 Run four plants. Copy each file to the scratchpad first. Plant 1: `GC.KeepAlive(new object());` as the first line of `CommandRegistry.Invoke`. Plant 2: the same line as the first line of `EventBus.Publish`. Plant 3: the same line as the first line of `ServiceRegistry.TryResolve`. Plant 4: an empty body of the click frame method of the gate. For each plant, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Record which tests fail and the bytes (plant 4 fails `TheMeasuredBodyDoesRealWork`). Restore each file and check it with `cmp`. Run the tests again and expect 0 failed.

**Tasks (group B: the docs):**
- [ ] 5.4 Edit section 10 of `docs/technical-design.md`. In the Components table, change the `Change` cell of the row `EventBus`, `ServiceRegistry`, `CommandRegistry`, `ModeHistory` to: copy, with a static handler plus a target for each command and each subscriber (Q3). `ModeHistory` waits for panels-and-modes. After "Data binding", add a subsection "Commands, events, and services" with the rules of this feature: the command registry (the UTF-8 name, the byte compare, the static handler plus target, `Invoke` and its result, the `UnknownCommand` slot, the name rules, `Unregister`, the re-entry rule, and the error rule), the button bind (`+=` on `Clicked`, the build-time allocation, and the late register), the event bus (the exact type, the snapshot rule, the copy on write, and the error rule), and the service registry (the exact type, `null` and `false` for a missing service).
- [ ] 5.5 Edit section 14 of `docs/technical-design.md`: add a bullet for the registry tests (`CommandRegistryTests`, `CommandBindingTests`, `EventBusTests`, `ServiceRegistryTests`) and a bullet "A sixteenth allocation gate" for `CommandAndEventAllocationTests`, with the scene and the plant results of task 5.3. Check the ordinal first: the last gate bullet must be "A fifteenth allocation gate".
- [ ] 5.6 Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `CommandRegistry`, `CommandHandler`, `UnknownCommandHandler`, `CommandBindings.BindCommand`, `EventBus`, `EventBusHandler<T>`, `ServiceRegistry`, and the command and event allocation gate (state-and-commands/commands-and-events).
- [ ] 5.7 Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, and `context.md`. Record the results and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 45 (7 tasks, about six to seven steps for one task)

**Files (group A, 2):** `CommandAndEventAllocationTests.cs`, `context.md` (the plants touch `CommandRegistry.cs`, `EventBus.cs`, and `ServiceRegistry.cs` and restore them)
**Files (group B, 3):** `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 5 tests of `CommandAndEventAllocationTests` pass in Debug, and each of the four plants gives a non-zero exit code of `dotnet test`.
- [ ] `scripts/test-linux.sh` prints its PASS line, and no file of `scripts/` changed.
- [ ] Sections 10 and 14 of the technical design and the changelog name the three registries, the bind, and the gate.

---

## Key Technical Decisions

### Confirmed Assumptions

This feature had no feature interview. The items below come from the epic interview of `/plan-epic state-and-commands` (see `docs/features/state-and-commands/epic-overview.md`), and the user confirmed them there:
- Q3 (epic) A command handler is a static method plus a target, like the event slots.
- Q4 (epic) One feature `commands-and-events` holds `EventBus`, `ServiceRegistry`, and `CommandRegistry`.
- A5 (epic) A command bind on a `Button` runs at build time and may allocate once. Only the invoke path is zero-alloc.
- A2 (epic) `tests/BrutalUi.Framework.Tests/` exists in the solution. The epic adds tests there and makes no new project.
- A1 (epic) Build order: store-and-subscription, element-bindings, commands-and-events, action-map.

### Decision 1: A command is a struct entry with a UTF-8 name, a static handler, and a target
**Choice:** `CommandRegistry` holds a plain array of a private struct (a copy of the name, a `CommandHandler`, and a target) that doubles when it is full. `CommandHandler` is `void (object? target)`.
**Rationale:** Q3 and the shape of `ElementBinding` of element-bindings: a struct in a plain array, a static handler, and a target object. A static handler in a `static readonly` field allocates no delegate for each register. The handler gets the target only, because a click and a key carry no payload for a command. A later need (a source or an argument) is a new delegate type, not a change of this one.
**Alternatives:** (a) `Action` closures (a closure for each register, and a capture can hide state). (b) An `ICommand` interface with `Execute` and `CanExecute` (one class for each command, and `CanExecute` is out of scope).

### Decision 2: The lookup is a scan with a byte compare
**Choice:** `Invoke` scans the entries in the order of the register and compares the bytes with `SequenceEqual`. No hash and no handle.
**Rationale:** YAGNI. A game holds tens of commands, and a click or a key is a rare event in a frame. `SequenceEqual` on a span is vectorized and allocates nothing. The API takes a name, so a hash table can replace the scan later with no API change.
**Alternatives:** (a) A hash table on the bytes: more code for no measured need. (b) A `CommandId` handle that the bind resolves one time: the command must exist at the bind, and the bind order then matters (Decision 4).

### Decision 3: An unknown name reports through the return value and a slot
**Choice:** `Invoke` returns `false` for an unknown name, and it calls the slot `UnknownCommand` with the name when the slot is set. Notes question 2 holds the open choice.
**Rationale:** The requirement allows a result value or a slot. A direct caller (action-map) reads the result. A button click has no caller that reads a result, so only a slot can report a click on a missing name. The slot follows `UiContext.OnFileDrop`: one delegate with a span argument, set one time. Neither path throws.
**Alternatives:** (a) The result only: a button click on a typo reports nothing. (b) A result enum: one more public type for two states.

### Decision 4: The bind copies the name and looks it up at each click
**Choice:** `BindCommand` copies the name to a `byte[]` and adds one instance delegate of a small target class to `Button.Clicked` with `+=`. The registry resolves the name at each click.
**Rationale:** `Button.Clicked` is an `Action<Button>` with no target slot, so a target needs an instance delegate. The allocation happens one time, at build time (A5). A late lookup lets the game bind the UI before it registers the commands, and it lets `Unregister` and a new `Register` change the command of a bound button. `+=` keeps a `Clicked` handler that the game set before the bind.
**Alternatives:** (a) `=` on `Clicked`: it removes the handler of the game. (b) A new `Command` property on `Button` in Core: a Core change for a Framework concept.

### Decision 5: The event bus calls a static handler with a target, and the arrays are copy on write
**Choice:** `EventBusHandler<T>` is `void (in T e, object? target)`. `Subscribe` and `Unsubscribe` write a new subscriber array. `Publish` reads the array once at the start. `Unsubscribe` takes the handler and the target, and removes the first match. Notes question 1 holds the open choice.
**Rationale:** Q3 gives one handler shape for the epic, and the `in` parameter copies no struct event. The copy on write makes each re-entry safe with no flag and no deferred list: a publish holds its own snapshot. The cost is an array for each subscribe and each unsubscribe, which is a build-time act. A removal by the pair needs no token type and no `IDisposable` object.
**Alternatives:** (a) The mosaic-ui shape as the docs name it: `Action<T>` and an `IDisposable` token (`CallbackDisposable`). It allocates a closure and a token for each subscribe and takes the event by value. (b) An in-place list with a publish depth and deferred removals: no allocation at an unsubscribe, but more state and more risk.

### Decision 6: A bus and a registry key on the exact type
**Choice:** `EventBus` and `ServiceRegistry` each hold a `Dictionary<Type, object>` keyed by `typeof(T)`. A publish of a derived type does not reach a subscriber of the base type. A service resolves through the type of its register only. Only `Subscribe` creates a channel, so a publish of a type with no subscriber allocates nothing, also at the first publish.
**Rationale:** A lookup by `typeof(T)` allocates nothing and boxes nothing. An exact type keeps the rule simple and the publish cost fixed. The `class` constraint of `ServiceRegistry` keeps a struct service out, so no service boxes.
**Alternatives:** (a) A walk of the base types at each publish: a cost for each publish and a surprise order. (b) A static generic slot for each type: one global list for each type, so two buses share their subscribers.

### Decision 7: The registries are plain instances with no create-info
**Choice:** Each registry is a `sealed class` with a public constructor and no parameters. The game owns each instance. The feature adds no static state.
**Rationale:** No registry has a setting. An empty `required` create-info adds noise. Instances keep each test apart and let a game hold two buses. The game can register the bus and the command registry in the `ServiceRegistry`.
**Alternatives:** (a) A static singleton for each registry (the tests share state, and a game cannot reset it). (b) An empty `CreateInfo` struct for a uniform shape.

### Decision 8: A duplicate register throws, and only commands can unregister
**Choice:** `CommandRegistry.Register` and `ServiceRegistry.Register` throw `ArgumentException` for a name or a type that they hold. `CommandRegistry.Unregister` and `EventBus.Unsubscribe` exist. `ServiceRegistry` has no unregister. Notes question 4 holds the open choice.
**Rationale:** A second register of a name is a build bug, and a throw at build time shows it. A command target and a subscriber target are often a panel that closes, so the registry must drop the target (the reference rule of element-bindings). A service lives as long as the game, so its unregister waits for a need.
**Alternatives:** (a) The last register replaces the first, with no throw and no unregister. (b) An unregister on each registry.

### Decision 9: The tests and the gate live in the Framework test project with no project change
**Choice:** Each new test class lives in a new folder of `tests/BrutalUi.Framework.Tests/` (`Commands/`, `Events/`, `Services/`). The gate uses the copy of `AllocationGate` and the linked `RecordingRenderer`. The test files copy the private event helpers of `WidgetBindingTests`.
**Rationale:** A2 forbids a new project. The project file already links each file that the tests need. The Framework tests cannot use the internal event factories of `BrutalUi.Core.Tests` (lesson 2 of element-bindings).
**Alternatives:** A shared test helper file for the event factories: a change of the project or a new shared file for 30 lines.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A Debug build boxes a generic value in `Publish<T>` or `TryResolve<T>` (the MinOpts JIT keeps a `box`). | Medium | Medium | The Internal Shape forbids `is null`, `== null`, an `(object)` cast, and `Equals` on a value of type `T`. Task 3.3 greps for them. The gate runs in Debug. Phase 5 escalates to ADVANCED when it measures bytes. |
| A subscribe or an unsubscribe during a publish breaks the loop or skips a subscriber. | Low | High | Decision 5: the publish holds its own snapshot. Two re-entry tests in phase 3. |
| A handler that registers or unregisters during an invoke calls the wrong handler. | Low | High | `Invoke` copies the handler and the target before the call. `AHandlerCanRegisterUnregisterAndInvokeDuringAnInvoke`. |
| A command target or a subscriber target keeps a closed panel alive. | Medium | Medium | `Unregister` and `Unsubscribe` drop the target. The remarks state the reference rule. `Unregister` writes `default` into the old last slot. |
| A game writes `button.Clicked = handler` after the bind and removes the command. | Medium | Low | The remarks of `BindCommand` state it. `AClickedHandlerOfTheGameStillRunsBeforeTheCommand` shows the `+=` order. |
| The click gate needs the button layout before the first click. | Low | Low | The warm-up rounds run `Update`. Task 1.6 runs one `Update` before the first click. |
| The scan grows slow for a large count of commands. | Low | Low | Decision 2. The remarks state the scan. A hash table can replace it with no API change. |
| The names of mosaic-ui differ from the names of this plan. | Medium | Low | Notes Conflicts and Gaps item 1. A rename before action-map costs little. |
| element-bindings is not committed when the feature starts. | Medium | Low | Task 1.1 checks the log and records the base and the counts. Each phase checks the counts of its own classes. |
| A test helper copies drift from `WidgetBindingTests`. | Low | Low | Each copy is about 30 lines of event construction. A drift fails a test, not the product. |

---

## Dependencies

### Internal Dependencies
- `state-and-commands/store-and-subscription` (done): the Framework test project, its copy of `AllocationGate`, the gate shape, and `Store<T>` for one integration test.
- `state-and-commands/element-bindings` (done, in the working tree): `LabelBindings.BindText` for `ACommandThatSetsAStoreShowsInTheSameUpdate`, the link of `RecordingRenderer.cs`, and the lessons of its `context.md`.
- `interactive-elements`: `Button.Clicked` (`Action<Button>`), the click rule, and the Enter and Space key rule.
- `input-routing`: the input pass, `UiContext.SetFocus`, and the pointer and key events.
- `dev-ops`: `brutal-ui.sln`, `scripts/test-linux.sh`, and `scripts/test-windows.ps1` (both run `dotnet test --solution brutal-ui.sln`).
- Sibling: action-map calls `CommandRegistry.Invoke` for each action. It can register `NavigateUp` and `NavigateDown` as commands whose target is the `UiContext` and whose static handlers call `FocusPrevious` and `FocusNext`.

### External Dependencies
- None. No new package and no native dependency. `Dictionary<TKey, TValue>`, `MemoryExtensions.SequenceEqual`, and `NotNullWhenAttribute` come from the .NET 10 base library.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. A registered command runs with its target at each `Invoke` of its name, and `Invoke` returns `true`. Names compare by their bytes. An unknown name returns `false`, calls no handler, calls `UnknownCommand` with the name, and throws nothing (`CommandRegistryTests`).
- [ ] F2. A click and an Enter key on a bound button each invoke the command one time. A `Clicked` handler of the game still runs, before the command. A command registered after the bind runs. A click on an unknown name calls `UnknownCommand`. A command that sets a store shows in the same `Update` (`CommandBindingTests`).
- [ ] F3. A register rejects a `null` handler, an empty name, and a duplicate name. `Unregister` frees a name. A handler can register, unregister, and invoke during an invoke. A throwing handler leaves the registry usable (`CommandRegistryTests`).
- [ ] F4. A publish calls each subscriber of its exact type one time with its target, in the order of the subscribe. A subscribe or an unsubscribe during a publish takes effect at the next publish. A throwing subscriber stops one publish (`EventBusTests`).
- [ ] F5. A service resolves by the exact type of its register. A missing service gives `null` and `false` and throws nothing. A `null` service and a duplicate type throw at the register (`ServiceRegistryTests`).
- [ ] F6. `CommandAndEventAllocationTests` measures 0 bytes in Debug for the invoke, the click, the publish, and the resolve bodies, and its real-work test passes. The plants in `Invoke`, `Publish`, `TryResolve`, and the click body each give a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. The new classes hold these counts: `CommandRegistryTests` 12, `CommandBindingTests` 10, `EventBusTests` 10, `ServiceRegistryTests` 7, and `CommandAndEventAllocationTests` 5. `BrutalUi.Framework.Tests` holds 44 more tests than at `<base>`.
- [ ] Q3. Each public member of the new source files has an XML doc comment. The new doc comments and the changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q4. The new source folders hold no `lock`, no `IDisposable`, no `INotify` type, no `event` keyword, and no `ToString(` call in code. `EventBus.cs` and `ServiceRegistry.cs` hold no `is null`, `== null`, `!= null`, or `(object)` in code.

### Integration Criteria
- [ ] I1. No file of `src/BrutalUi.Core/`, `src/BrutalUi.Framework/State/`, `src/BrutalUi.Framework/Binding/`, `scripts/`, `samples/`, or `tests/BrutalUi.Core.Tests/` has a diff against `<base>`. No `.csproj`, `.props`, or `.sln` file has a diff. No test file that existed at `<base>` changed.
- [ ] I2. `BindCommand` returns its button and adds to `Button.Clicked` with `+=`. `CommandRegistry.Invoke(ReadOnlySpan<byte>)` is public, so action-map can call it.
- [ ] I3. Section 10 of `docs/technical-design.md` states the rules of the three registries and the bind. Section 14 holds the bullet "A sixteenth allocation gate". `CHANGELOG.md` names the new types.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` and the base counts from `docs/features/state-and-commands/commands-and-events/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed (Q2).
3. Run `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5` for `CommandRegistryTests` (12, F1, F3), `CommandBindingTests` (10, F2), `EventBusTests` (10, F4), `ServiceRegistryTests` (7, F5), and `CommandAndEventAllocationTests` (5, F6). Expect `failed: 0` and these totals. Run `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj 2>&1 | tail -n 5` and expect the base count plus 44 (93 when the base count is 49). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Framework/Commands/CommandRegistry.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `Invoke`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp`. Repeat with the first line of `EventBus.Publish` in `src/BrutalUi.Framework/Events/EventBus.cs`, and with the first line of `ServiceRegistry.TryResolve` in `src/BrutalUi.Framework/Services/ServiceRegistry.cs` (F6).
5. Copy `tests/BrutalUi.Framework.Tests/Commands/CommandAndEventAllocationTests.cs` to `<scratch>`. Empty the body of the click frame method. Run the command of step 4. Expect a value that is not 0, with `TheMeasuredBodyDoesRealWork` among the failures. Restore the file, check it with `cmp`, and run step 2 again (F6).
6. `grep -rnE '\block\b|IDisposable|INotify|\bevent\b|ToString\(' src/BrutalUi.Framework/Commands src/BrutalUi.Framework/Events src/BrutalUi.Framework/Services | grep -v '///'`. Expect no line. `grep -nE 'is null|== null|!= null|\(object\)' src/BrutalUi.Framework/Events/EventBus.cs src/BrutalUi.Framework/Services/ServiceRegistry.cs | grep -v '///'`. Expect no line (Q4).
7. `grep -n 'Clicked +=' src/BrutalUi.Framework/Commands/CommandBindings.cs`. Expect one line. `grep -n 'Clicked =' src/BrutalUi.Framework/Commands/CommandBindings.cs`. Expect no line (I2).
8. `git diff <base> --stat -- src/BrutalUi.Core/ src/BrutalUi.Framework/State/ src/BrutalUi.Framework/Binding/ scripts/ samples/ tests/BrutalUi.Core.Tests/`. Expect no output. `git diff <base> --name-only -- '*.csproj' '*.props' '*.sln'`. Expect no output. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no output (I1).
9. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4).
10. Read section 10 ("Commands, events, and services") and section 14 of `docs/technical-design.md`, and the `## [Unreleased]` part of `CHANGELOG.md` (I3). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q3).

---

## Testing Strategy

### Unit Tests
- `Framework.Tests/Commands/CommandRegistryTests` (12): the invoke with the target, the result, the unknown name and the slot, the byte compare, the null, empty, and duplicate checks, the name copy, `Unregister`, the re-entry, and a throwing handler.
- `Framework.Tests/Commands/CommandBindingTests` (10): a click, an Enter key, the order with a game handler, a late register, an unknown name at a click, the name copy, the argument checks, two binds on one button, and a command that writes a store that a bound label shows in the same `Update`.
- `Framework.Tests/Events/EventBusTests` (10): the order and the target, no subscriber, another type, `Unsubscribe`, a pair twice, the two re-entry rules, a nested publish, a throwing subscriber, and the null check.
- `Framework.Tests/Services/ServiceRegistryTests` (7): the resolve, the interface register, the missing service through both methods, the exact type, the argument checks, and the count.

### Allocation Tests
- `Framework.Tests/Commands/CommandAndEventAllocationTests` (5, the sixteenth gate): an invoke of a first, a last, and an unknown name, a click on a bound button through `Update` and `Draw`, a publish to 8 subscribers plus a second type and a type with no subscriber, a resolve of present and missing services, and the real-work check. Four plants guard `Invoke`, `Publish`, `TryResolve`, and the click body.

### Regression Tests
- Each existing test runs with no edit. No Core file changes, so the 15 existing allocation gates keep their scenes.

### Integration Tests
- `ACommandThatSetsAStoreShowsInTheSameUpdate` runs the input pass, the command, the store, and the binding pass in one `Update`.
- `scripts/test-linux.sh` runs the full suite and the HelloPanel smoke run. HelloPanel does not change. action-map adds the command button and the key binding to the sample.

### Manual Testing
- None in this feature. action-map adds the visible demo of a command button in HelloPanel.

---

## Related Documentation

- **Requirements:** `docs/features/state-and-commands/commands-and-events/requirements.md`
- **Epic:** `docs/features/state-and-commands/epic-overview.md`, `docs/features/state-and-commands/epic-requirements.md`
- **Siblings:** `docs/features/state-and-commands/store-and-subscription/implementation.md` and `context.md`, `docs/features/state-and-commands/element-bindings/implementation.md`, `context.md` (the Important Decisions and the Gotchas), and `code-review.md` (CR-1 and the follow-up questions), `docs/features/state-and-commands/action-map/requirements.md`
- **Design:** `docs/technical-design.md` sections 4 (frame order), 9 (action map), 10 (Framework Layer), and 14 (Testing). `docs/feasibility.md` ("Mapping mosaic-ui to brutal-ui")
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Button.cs` (`Clicked`, the click and key rules), `src/BrutalUi.Core/Elements/Toggle.cs` (a static handler in a `static readonly` field), `src/BrutalUi.Core/Binding/ElementBinding.cs` (a static handler plus a target in a struct), `src/BrutalUi.Core/Input/UiWindowEvents.cs` (a delegate slot with a span argument), `src/BrutalUi.Framework/Binding/WidgetBindings.cs` (an extension bind that returns its element), `tests/BrutalUi.Framework.Tests/State/StoreAllocationTests.cs` (the gate shape), and `tests/BrutalUi.Framework.Tests/Binding/WidgetBindingTests.cs` (the event helpers)
- **mosaic-ui reference:** `docs/feasibility.md` (mosaic-ui 0.4.0: `EventBus`, `ServiceRegistry`, `CommandRegistry`, `ModeHistory`, and `CallbackDisposable` have no Unity dependency) and `docs/technical-design.md` section 10 (the source folders `Core/`, `Interaction/`, and `Modes/`). The source itself is not available (Conflicts and Gaps item 1).
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

This plan is a draft with no interview. Each question below has a recommended option, and the plan uses it.

1. **The handler shape of the event bus.**
   - **(A) Recommended:** A static handler plus a target, `EventBusHandler<T>(in T e, object? target)`, with `Unsubscribe` by the handler and target pair (Decision 5). One handler shape for the epic (Q3), and no allocation for each subscribe other than the array.
   - (B) The mosaic-ui shape as the docs name it: `Subscribe<T>(Action<T>)` returns an `IDisposable` token (`CallbackDisposable`). A closure and a token for each subscribe, and the event by value.
   - (C) Both forms: (A) as the main API and (B) as a convenience overload.
2. **The report of an unknown command name.**
   - **(A) Recommended:** `Invoke` returns `false`, and the slot `UnknownCommand` gets the name (Decision 3). A button click on a missing name then also reports.
   - (B) The return value only. A click on a missing name reports nothing.
   - (C) A `CommandResult` enum and the slot.
3. **The demo of phase 1 (Slice rule 12).**
   - **(A) Recommended:** `**Demo:** a developer runs dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests' and reads a total of 5 with failed: 0. The first test clicks a button bound to save and reads one call of the save handler with its target.`
   - (B) `**Demo:** a developer runs the one test AClickOnABoundButtonInvokesTheCommand with --filter-method and reads 1 passed.`
   - (C) `**Demo:** a developer runs dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandRegistryTests' and reads a total of 6 with failed: 0.` (the registry only, with no button)
4. **A second register of the same name or type.**
   - **(A) Recommended:** It throws `ArgumentException` at build time. `CommandRegistry.Unregister` frees a name, and `EventBus.Unsubscribe` drops a subscriber. `ServiceRegistry` has no unregister (Decision 8).
   - (B) The last register replaces the first, with no throw and no unregister. A closed panel then cannot drop its target.
   - (C) Option (A) plus `ServiceRegistry.Unregister<T>`.

### Conflicts and Gaps

1. **The mosaic-ui source is not available.** The requirement asks the copies to follow the mosaic-ui design and keep its names. The repository holds no mosaic-ui code. A search of `/home/aaronstatic` down to four folder levels found no `mosaic` file or folder, and a web search found no public mosaic-ui repository. The docs give only the type names (`EventBus`, `ServiceRegistry`, `CommandRegistry`, `ModeHistory`, `CallbackDisposable`), the version (0.4.0), and the source folders (`Core/`, `Interaction/`, `Modes/`). This plan keeps the three type names and takes each member name from the verbs of the requirements: `Subscribe`, `Publish`, `Register`, `Resolve`, and `Invoke`. It does not copy `CallbackDisposable` (Decision 5). Recommendation: the user gives the path of the mosaic-ui source, or confirms the names, before action-map starts. A rename then costs little.
2. **"With `INotifyBindablePropertyChanged` removed".** `docs/feasibility.md` lists no Unity dependency for the three registries, and only the store used that interface (store-and-subscription removed it). This feature has nothing to remove. Q4 of the verification greps for `INotify` to show it.
3. **"Like the event slots".** Q3 and the requirement compare a command handler with the event slots on `Element`. The event slots (`UiEventHandler<T>`) take `ref T e` and no target. The shape "a static handler plus a target" is the shape of `BindingHandler` plus the target of `ElementBinding`. This plan follows `BindingHandler` and keeps Q3 as written.
4. **"May allocate once" (A5).** `BindCommand` allocates a target object, a copy of the name, and a combined delegate: three objects, one time, at build time. The plan reads "once" as "one time, at the bind". The click path allocates nothing.
5. **`Button.Clicked` has no target slot.** It is an `Action<Button>`. The bind thus needs an instance delegate for each bound button. A static handler plus a target on the button itself would need a Core change, which this plan avoids (Decision 4).
6. **"The registry pattern" of store-and-subscription.** The epic overview states that commands-and-events depends on store-and-subscription for "the registry pattern". store-and-subscription made no registry. This plan reuses its test layout, its gate shape, and its copy of `AllocationGate`.
7. **The base commit.** HEAD at the time of the plan is `27f33c7`, and the files of element-bindings are not committed yet. Task 1.1 records the base after that commit.
8. **The xml-markup seam.** `BrutalUi.Markup` references Core only, and `BindCommand` lives in Framework. Follow-up question 3 of the element-bindings review holds the same open choice for the bind methods. The xml-markup plan decides. The public API of this feature serves each option.
9. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 5.7 runs it on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
