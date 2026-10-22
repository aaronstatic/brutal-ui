# Routing Gate - Implementation Plan

**Epic:** input-routing (feature 4 of 4, the last one)
**Status:** Ready for Review
**Started:** 2026-10-22
**Target Completion:** TBD
**Last Updated:** 2026-10-22 23:51

---

## Quality Bar

**Target quality:** A core library feature plus a sample and a smoke gate. This feature closes the input-routing epic. It wires the drain of event-model into `UiContext.Update`, so each game frame now runs the input pass. A wrong frame order, a gate value that does not agree with the states, a stuck drain after an exception, or a hidden allocation in the input pass breaks each game that calls `Update`.

- **The frame order is exact:** `UiContext.Update` runs the input pass first, then the style pass, then the transition pass, then the layout pass, then the clear pass (A5). A test proves that one `Update` call turns a queued pointer move into a hover fill.
- **The gate agrees with the states:** `PointerOverUi` is true exactly when the last hit of pointer-routing is an element. `KeyboardFocusInUi` is true exactly when a focus owner exists (A6). The gate reads the same two fields that hold `Hover` and `Focus`, so the gate and the states cannot disagree.
- **No new path in the drain:** `DrainEvents` keeps its body and its order: the stale pointer rule, the stale focus rule, then each entry. `EventWalk.cs` keeps its SHA-256. This feature adds a caller, not a rule.
- **0 bytes after warm-up:** A full ring of 256 mixed events, drained by `Update` on the warm 1000-element tree, allocates 0 bytes. The gate read allocates 0 bytes. The HelloPanel smoke run measures the synthetic input, the input pass, the gate read, and the label update, and it exits with code 1 when that region allocates. A planted allocation fails a unit gate and the smoke run.
- **A visible demo:** HelloPanel shows a `:hover` rule, a `:focus` rule, and a label with the two gate values. The smoke run proves that the synthetic input reaches the UI, so the allocation gate of the sample cannot pass with no work.
- **No GLFW in Core:** `BrutalUi.Core` gets no package and no `using` of GLFW. Only the sample and `BrutalUi.Glfw` use GLFW.

---

## Executive Summary

This feature adds the public `UiRoutingGate` struct and the public `UiContext.RoutingGate` property. The game reads the property after `UiContext.Update`. `PointerOverUi` reads the internal last hit of pointer-routing (`PointerHit`), and `KeyboardFocusInUi` reads the focus owner of keyboard-focus (`FocusedElement`). The gate is a value with no event. `UiContext.Update` gets one new first step: it calls the internal `DrainEvents`, so the queued events walk the tree, move `Hover`, `Active`, and `Focus`, and the style pass of the same call restyles the changed elements. A handler that calls `Update` during the drain gets `InvalidOperationException` from the existing drain guard, and the plan documents the rule on `Update`. `ThousandElementAllocationTests` gets a queue case: each step fills the ring with 256 mixed pointer, scroll, key, char, and Tab events, then runs the rotating change and `Update`. The case measures 0 bytes. HelloPanel gets a `GlfwUiInput`, the `KeyboardEnabled` line, a focusable panel, a `:hover` rule and a `:focus` rule in `HelloScreen.css`, a label that shows the two gate values, and, in the smoke run, a synthetic pointer move and a synthetic Tab in each frame. `scripts/test-linux.sh` runs the sample, and a planted allocation in the input pass makes the sample exit with code 1.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (reads `ui.RoutingGate` after `Update` and stands down the camera, the world click, or the game keys while the UI owns the pointer or the keyboard. Copies the frame loop of HelloPanel)
- [x] **The interactive-elements epic** (`Button`, `Toggle`, `ScrollView`, and `TextField` get their events through `Update` with no extra call)
- [x] **The Phase 3 framework epic** (the action map reads the same gate to decide if a key belongs to the UI or to the game)
- [x] **The maintainer** (runs one more 0-byte case in the release gate and a smoke run that proves the input pass)
- [ ] **The markup author** (not served. The `:hover` and `:focus` selectors exist already)

**User Subsets Served:**
- [x] **Simple** - `ui.Update(dt); if (!ui.RoutingGate.PointerOverUi) { HandleWorldClick(); }`
- [x] **Complex** - `UiRoutingGate gate = ui.RoutingGate; bool uiOwnsPointer = gate.PointerOverUi || io.WantCaptureMouse;` with a debug ImGui panel next to the UI, `input.KeyboardEnabled = !io.WantCaptureKeyboard;` before `Glfw.PollEvents()`, and a layout root with `IsPickable = false`, so the empty area of the root passes the pointer to the game.

**UX Approach:**
- One read gives both values at the same time. The game reads the gate one time per frame, after `Update`, and before it handles its own input.
- The game calls only `Update`. `DrainEvents` stays internal, so no game can forget the drain or call it in the wrong order.

---

## Goals

### Primary Goals
1. Run the input pass at the start of each `UiContext.Update`, before the style pass (A5).
2. Tell the game when the UI owns the pointer and when it owns the keyboard, with a value that the game polls after `Update` (A6).
3. Prove 0 bytes for a full ring drained by `Update` on the warm 1000-element tree.
4. Show the hover state, the focus state, and the gate in HelloPanel, and guard the input pass in the smoke run.
5. Close the follow-ups of the siblings: the drain in each `Update` (CR-5 of pointer-routing), the layout-root rule (P6-8 of pointer-routing), the pointer side of the ImGui rule (Conflicts 8 of keyboard-focus), and the HelloPanel seams of keyboard-focus.

### Success Criteria
- [ ] The phase 1 demo test passes: one `Update` call with a queued move and a queued Tab gives the hover fill, the focus, and a gate with both values true.
- [ ] `RoutingGate` follows the last hit and the focus owner, also after a stale rule and after a code focus call.
- [ ] A handler that calls `Update` gets `InvalidOperationException`, and the next `Update` works.
- [ ] The queue case of `ThousandElementAllocationTests` measures 0 bytes, and a plant fails it.
- [ ] HelloPanel shows the two state rules and the gate label. The smoke run prints the gate line with both counts above 0, and it exits with code 1 under a plant.
- [ ] `scripts/test-linux.sh` passes. `scripts/test-windows.ps1` passes on Windows (manual).
- [ ] `EventWalk.cs` keeps its hash. `BrutalUi.Core` has no GLFW reference.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Input/UiRoutingGate.cs                  # phase 1: the public gate struct (new)
└── Tree/UiContext.cs                       # phase 1: RoutingGate, the drain call in Update, the doc comments
src/BrutalUi.Glfw/
└── GlfwUiInput.cs                          # phase 1: the class remarks only (Update drains now)
tests/BrutalUi.Core.Tests/
├── Input/RoutingGateTests.cs               # phase 1 (new)
└── ThousandElementAllocationTests.cs       # phase 3: the queue case (2 tests)
tests/BrutalUi.Markup.Tests/Style/
└── ParsedSheetAllocationTests.cs           # phase 2: the HelloPanel sheet test reads 6 rules
samples/HelloPanel/
├── HelloPanel.csproj                       # phase 2: a project reference to BrutalUi.Glfw
├── HelloScreen.css                         # phase 2: two variables, the :hover rule, the :focus rule
├── HelloScreen.cs                          # phase 2: the focusable panel, the pick rules, the gate label
└── Program.cs                              # phase 2: GlfwUiInput, KeyboardEnabled, the smoke input, the gate line
docs/technical-design.md                    # phase 3: sections 4, 5, 9, 14
CHANGELOG.md                                # phase 3
```

`EventWalk.cs`, `HitWalk.cs`, `FocusWalk.cs`, `PaintWalk.cs`, `Element.cs`, `Directory.Packages.props`, each `.csproj` under `src/` and `tests/`, `brutal-ui.sln`, `scripts/`, `README.md`, and `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs` do not change. The benchmark project links `ThousandElementTree.cs`, so the queue case sets `IsFocusable` in the test file and not in the tree.

### Public API (end state)

Each public member gets an XML doc comment (CS1591 is an error).

```csharp
namespace BrutalUi.Core;

/// The routing gate: a copy of two values that the game reads after UiContext.Update.
public readonly struct UiRoutingGate
{
    internal UiRoutingGate(bool pointerOverUi, bool keyboardFocusInUi);   // phase 1

    public bool PointerOverUi { get; }       // phase 1: the last hit of a pointer entry is an element
    public bool KeyboardFocusInUi { get; }   // phase 1: a focus owner exists
}

public sealed class UiContext
{
    public UiRoutingGate RoutingGate { get; }   // phase 1: new(_pointerHit is not null, _focusOwner is not null)
    public void Update(float deltaTime);        // phase 1: the input pass runs first. The signature does not change
}
```

`default(UiRoutingGate)` gives two `false` values. The struct has no other member: no `Equals` override, no `ToString` override, and no event. `PushEvent` keeps its four public overloads. `DrainEvents`, `PointerHit`, `PointerPosition`, and `HasPointerPosition` stay internal.

### Data Flow

```
Frame of the game
   input.KeyboardEnabled = !io.WantCaptureKeyboard;          (keyboard-focus, Decision 11)
   Glfw.PollEvents() ──► GlfwUiInput ──► UiContext.PushEvent ──► the ring (256 entries, drop-oldest)
   (smoke run of HelloPanel only: PushEvent(move), PushEvent(Tab down), PushEvent(Tab up))
   ▼
UiContext.Update(deltaTime)
   0. the input pass: DrainEvents()                          (phase 1, A5. The body does not change)
        DropStalePointerState ─► DropStaleFocus ─► for each entry (the count at the start):
          target step (hit test, Hover, Active, click focus, or the focus owner) ─► walk ─► Tab move
        (a handler that calls Update or the drain ─► InvalidOperationException from the _draining guard)
   1. roots = _roots.AsSpan()                                (read after the drain: a handler can add or remove a root)
   2. the style pass ─► 3. the transition pass ─► 4. the layout pass ─► 5. the clear pass
   ▼
UiRoutingGate gate = ui.RoutingGate;                          (phase 1, A6. A live read, a struct copy)
   PointerOverUi     = _pointerHit is not null
   KeyboardFocusInUi = _focusOwner is not null
   ▼
The game: when gate.PointerOverUi (or io.WantCaptureMouse) is true, the game ignores the world click.
          When gate.KeyboardFocusInUi is true, the game ignores its own keys.
ui.Draw(renderer)
```

### The Seams for Later Work

| Consumer | The seam | What the consumer adds |
|----------|----------|------------------------|
| interactive-elements | `Update` drains each frame | `Button`, `Toggle`, `ScrollView`, and `TextField` register handlers. They call nothing else. |
| Phase 3 framework | `UiContext.RoutingGate` | The action map reads the gate before it gives a key to the game. |
| A later input feature | `GlfwUiInput` | A pointer switch from `io.WantCaptureMouse` (Notes Q3, option B), when a real case needs it. |

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements leave the demo of phase 1 open, so Notes Q1 holds it, and phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment. Use the file-scoped `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the Core tests. Prose in comments follows ASD-STE100. Each phase checks that the SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` stays `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (Decision R3 of pointer-routing).

### Phase 1: The Input Pass and the Gate
**Goal:** The smallest path that proves the idea: `Update` drains the ring before the style pass, and the game reads both gate values after `Update`.
**Demo:** a unit test marks `Right` of the pointer tree focusable, adds the hover sheet, pushes a pointer move over `Right` at (180, 80) and a Tab key down, both with no target, and calls `Update(0f)` only, with no `DrainEvents` call. It reads `QueuedEventCount` 0, `PseudoStates.Hover` and `PseudoStates.Focus` on `Right`, the hover fill in `Right.Style.BackgroundColor`, and `RoutingGate` with `PointerOverUi` and `KeyboardFocusInUi` both `true`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*RoutingGateTests'` and reads a total of 8 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes `UiContext.Update`, the method that each game calls each frame. Add one call, `DrainEvents();`, as the first statement. Read `_roots.AsSpan()` after the call, not before it: a handler can call `AddRoot` or `RemoveRoot` during the drain, and a span from before the drain then reads old storage. Do not change the body of `DrainEvents`, `RoutePointer`, the stale rules, or `EventWalk.cs`. Do not move the drain into the measure scope of the layout pass. The re-entrant guard exists already: the message of the `_draining` check names `Update`. After a handler exception, the `finally` block of the drain clears the flag, and `Update` ends before the style pass. Keep this behavior and document it.

**Tasks:**
- [ ] 1.1 Write `src/BrutalUi.Core/Input/UiRoutingGate.cs`: `public readonly struct UiRoutingGate` with `internal UiRoutingGate(bool pointerOverUi, bool keyboardFocusInUi)`, `public bool PointerOverUi { get; }`, and `public bool KeyboardFocusInUi { get; }`. The summary: the two values that tell the game when the UI owns the pointer or the keyboard. The remarks state: read the value from `UiContext.RoutingGate` after `UiContext.Update` and before the game handles its own input. The value is a copy, and a later change of the context does not change it. `default` gives two `false` values. `PointerOverUi` is true when the last pointer or scroll entry with no target hit an element. `KeyboardFocusInUi` is true when the context has a focus owner. The gate knows nothing of ImGui: combine `PointerOverUi` with `io.WantCaptureMouse`, and set `GlfwUiInput.KeyboardEnabled` from `io.WantCaptureKeyboard` (Decision 5). The layout-root rule: a root that covers the window makes `PointerOverUi` true over the full window. Set `IsPickable` to `false` on such a root (P6-8 of pointer-routing).
- [ ] 1.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `public UiRoutingGate RoutingGate => new(_pointerHit is not null, _focusOwner is not null);` next to `FocusedElement`. The remarks state (Decision 3): the property reads the current state, so the value changes during a drain, at `SetFocus`, `ClearFocus`, `FocusNext`, and `FocusPrevious`, and at the stale rules of the next `Update`. The pointer value follows the pointer events only: a layout change under a still pointer, for example a panel that closes, keeps the value until the next pointer event or the next stale rule. A press, a release, a move, and a scroll each hit-test again before the game reads the gate. An entry with a target from the caller changes no value. The read allocates nothing.
- [ ] 1.3 Edit `Update` in `UiContext.cs`: the first statement is `DrainEvents();` with the comment "The input pass (A5): the drain walks the queued events and changes the states before the style pass reads them." Move `ReadOnlySpan<Element> roots = _roots.AsSpan();` after the call. Change nothing else in `Update`.
- [ ] 1.4 Edit the doc comments in `UiContext.cs`. The class summary: `Update` runs the input pass, then the other passes. The remarks of `Update`: the frame order paragraph names the input pass first. Add a paragraph: the input pass drains each entry that the ring holds at the start of the call. The hit test reads the layout of the last `Update`. An event that a handler pushes waits for the next `Update`. A handler that calls `Update` gets `InvalidOperationException`. An exception from a handler stops the call before the style pass, the entries after it stay in the ring, and the next `Update` drains them. Read `RoutingGate` after the call. Add the second `<exception cref="InvalidOperationException">` case (a call from an event handler). In `DrainEvents`, replace the A5 paragraph ("routing-gate calls this method ... In this feature ... the tests call it directly") with: `Update` calls this method first. The tests also call it directly. In `PointerHit` and `FocusedElement`, replace "Routing-gate reads this member" with "`RoutingGate` reads this member". In the summaries of the four `PushEvent` overloads, "The next drain" becomes "The input pass of the next `Update`". Edit `src/BrutalUi.Glfw/GlfwUiInput.cs`: the class remarks paragraph that starts "The adapter only pushes the events" states that `UiContext.Update` drains the ring at its start.
- [ ] 1.5 Write `tests/BrutalUi.Core.Tests/Input/RoutingGateTests.cs` with 8 tests. Each test uses `PointerTree` with a prefix from `nameof`, and each test calls `Update` and no `DrainEvents`, except where the test name says so:
  - `UpdateRunsTheInputPassBeforeTheStylePass` (the demo).
  - `TheGateIsFalseWithNoInput`: after `Settle()`, both values are `false`. `default(UiRoutingGate)` gives both `false`.
  - `PointerOverUiFollowsTheLastHit`: a move over `Right` gives `true`. A move to (500, 500) outside the root gives `false`. A move to (NaN, NaN), the cursor leave, gives `false`. A move with a target from the caller (`Left`) keeps the value of the last hit.
  - `ANonPickableRootPassesThePointerToTheGame`: `Root.IsPickable = false`. A move over the gap of the root at (300, 200) gives `false`. A move over `Left` at (50, 50) gives `true` (P6-8 of pointer-routing).
  - `KeyboardFocusInUiFollowsTheFocusOwner`: `Left` and `Right` are focusable. A Tab gives `true`. A press over the gap of the root (no focusable element) gives `false`. `SetFocus(Right)` gives `true` at once, with no `Update`. `ClearFocus()` gives `false` at once.
  - `TheStaleRulesClearTheGateAtTheNextUpdate`: a move over `Right` and `SetFocus(Right)`, then `Update`. Remove `Right` from the root. An `Update` with an empty queue gives both values `false`, and `Right` holds no `Hover` and no `Focus`.
  - `AnUpdateFromAHandlerThrowsAndTheContextStaysValid`: an `OnPointerMove` handler on `Left` calls `tree.Context.Update(0f)`. A move over `Left` and `Assert.Throws<InvalidOperationException>(() => tree.Context.Update(0f))`. Then `Left.OnPointerMove = null`, and the next `Update(0f)` does not throw and leaves `QueuedEventCount` 0.
  - `AnEventThatAHandlerPushesWaitsForTheNextUpdate`: an `OnPointerMove` handler on `Left` pushes a move over `Right` one time. After the first `Update`, `QueuedEventCount` is 1 and `PointerOverUi` is `true` with `Left` hovered. After the second `Update`, the queue is empty and `Right` holds `Hover`.
- [ ] 1.6 Run the build and the full tests. Expect no failure in the existing classes: the existing tests push, call `DrainEvents()`, and then call `Settle()`, so the drain in `Update` finds an empty ring. When a test fails because `Update` now drains, read it. Record the test, the cause, and the fix in `context.md`. Change only the order of a push and a `Settle()` call, and change no assert. When a fix needs more, stop and report to the main thread. Run the demo command and the classes `FocusOwnerTests`, `ClickFocusTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `PointerStateEdgeTests`, `PointerAllocationTests`, `FocusAllocationTests`, and `ThousandElementAllocationTests` with no edit. Check the hash of `EventWalk.cs`. Run `git diff -- src/BrutalUi.Core/Tree/UiContext.cs` and check that the body of `DrainEvents` has no change.
- [ ] 1.7 Record the test counts before and after, the hash, the result of the `DrainEvents` diff check, and each decision that the brief did not settle in `docs/features/input-routing/routing-gate/context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (5):** `UiRoutingGate.cs`, `UiContext.cs`, `GlfwUiInput.cs`, `RoutingGateTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `RoutingGateTests` pass.
- [ ] Each existing test passes. A changed test is in `context.md` with its cause.
- [ ] `EventWalk.cs` has the hash of Decision R3 of pointer-routing. The body of `DrainEvents` has no change.

---

### Phase 2: The HelloPanel Demo and the Smoke Gate
**Goal:** HelloPanel shows the hover state, the focus state, and the gate. The smoke run pushes a synthetic pointer move and a synthetic Tab, proves that they reach the UI, and fails when the input pass allocates.
**Demo:** the developer runs `scripts/test-linux.sh; echo "exit=$?"` and reads `test-linux: PASS build, test, gpu, smoke (120 frames)` and `exit=0`. The file `artifacts/test-linux/smoke.log` holds `HelloPanel: bytes after warm-up: Update and Draw 0, ...` and the new line `HelloPanel: gate: pointer over UI in <P> frames, keyboard focus in UI in <K> frames, of 117 measured frames.` with `P` and `K` both above 0.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase touches four integration points at once: the adapter next to the ImGui `GlfwBackend`, the measured UI region of the sample, the sheet that a Markup test parses, and the output contract of the smoke run. Create `GlfwUiInput` after `GlfwBackend` and `VulkanBackend`, so a `using` declaration disposes it first (the creation order of the adapter remarks). Keep the UI region free of a hidden allocation: the synthetic events are structs, the gate text comes from four UTF-8 literals (`"..."u8`), and the gate label starts with its longest text, so its buffer never grows after warm-up. Put the gate label before the image, so the panel, the two labels, and the image keep 2 draw commands. When `Glfw` resolves to the namespace `BrutalUi.Glfw` in `Program.cs`, use an alias as `GlfwUiInput.cs` does. Do not change `scripts/`, the first three output lines, or the exit codes 0 and 2.

**Tasks:**
- [ ] 2.1 Edit `samples/HelloPanel/HelloPanel.csproj`: add `<ProjectReference Include="../../src/BrutalUi.Glfw/BrutalUi.Glfw.csproj" />` to the project reference group. Add no package. The sample references `Ahwoo.BRUTAL.Glfw` already, so the native GLFW library is not new.
- [ ] 2.2 Edit `samples/HelloPanel/HelloScreen.css`: add `--panel-hover: #283044;` and `--panel-focus: #f0c040;` to `:root`. Add the rule `.hello-panel:hover { background-color: var(--panel-hover); }` and the rule `.hello-panel:focus { border-color: var(--panel-focus); }` after the `.hello-panel` rule, each with a one-line comment. The two rules set different members, so hover and focus show at the same time.
- [ ] 2.3 Edit `samples/HelloPanel/HelloScreen.cs`. `Build(TextureHandle checker)` becomes `Build(TextureHandle checker, out Element panel, out Label gateLabel)`. Set `root.IsPickable = false` (the layout-root rule), `panel.IsFocusable = true`, and `IsPickable = false` on the greeting, the gate label, and the image, so each point inside the panel hits the panel. Create the gate label with the longest gate text, and add it to the panel between the greeting and the image. Add `public static ReadOnlySpan<byte> GateText(UiRoutingGate gate)`: it returns one of four `u8` literals, `"Pointer over UI: no. Keyboard focus in UI: no."` to `"Pointer over UI: yes. Keyboard focus in UI: yes."`. Edit the class remarks: the screen reacts to the pointer and to Tab, the root passes the pointer to the game outside the panel, the two state rules, and the gate label.
- [ ] 2.4 Edit `samples/HelloPanel/Program.cs`, `RunImGui`: after the `VulkanBackend` declaration, add `using GlfwUiInput input = new(new GlfwUiInput.CreateInfo { Window = window, Context = ui });`. Call `HelloScreen.Build(handle, out Element panel, out Label gateLabel)`, and pass `input`, `panel`, and `gateLabel` to `RunFrames`.
- [ ] 2.5 Edit `RunFrames` in `Program.cs`. Before `Glfw.PollEvents()`, add `input.KeyboardEnabled = !io.WantCaptureKeyboard;`. At the start of the UI region, when the run has a frame limit (the smoke run), push one `PointerEvent` (`PointerMove`, `Glfw.Time`, a null target, `PointerButton.Left`, `UiModifiers.None`) and one Tab `KeyEvent` down and one up (`Key = (int)GlfwKey.Tab`, scan code 0, no repeat, a null target). The move goes to the center of the viewport rect of `panel` (the sum of the rects of the panel and the root) in an even frame, and to `(viewport.X - 8, viewport.Y - 8)` in an odd frame. A run with no frame limit pushes no synthetic event. After `ui.Draw(uiRenderer)` and still inside the UI region, read `UiRoutingGate gate = ui.RoutingGate;`, call `gateLabel.SetText(HelloScreen.GateText(gate));`, and count the measured frames with each value `true`. Add a comment at the gate read: the game handles the world input here, and with an ImGui window it uses `gate.PointerOverUi || io.WantCaptureMouse` (Decision 5).
- [ ] 2.6 Edit `Run` and `RunResult` in `Program.cs`: add `PointerOverUiFrames` and `KeyboardFocusInUiFrames` to `RunResult`. After line 3, print `HelloPanel: gate: pointer over UI in <P> frames, keyboard focus in UI in <K> frames, of <M> measured frames.` After the allocation check, when the run has a frame limit, 2 or more measured frames, and `P` or `K` is 0, write `HelloPanel: the synthetic input did not reach the UI.` to standard error and return `ExitFailure` (Decision 8). Edit the remarks of `Program`, `Run`, and `RunFrames`: the four output lines, the frame order with the input pass and the gate read, the smoke input, the `KeyboardEnabled` line, and the region that the gate measures.
- [ ] 2.7 Edit `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`, `TheHelloPanelSheetGivesTheOldValues`: the rule count becomes 6. Add checks with `FindRule`: `.hello-panel:hover` sets only `BackgroundColor`, with the value `#283044`, and `.hello-panel:focus` sets only `BorderColor`, with the value `#f0c040`. Read the `ToString` text of a parsed state selector first, and use that text in `FindRule`. Keep each old check.
- [ ] 2.8 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Read `artifacts/test-linux/smoke.log`: the four lines, the draw command count (expect 2), the new vertex count, and the gate counts (expect `P` near 58 and `K` near 117 for 120 frames).
- [ ] 2.9 Record in `context.md`: the native dependency note (AGENTS.md rule: HelloPanel now references `BrutalUi.Glfw`, which uses the native GLFW library of `Ahwoo.BRUTAL.Glfw`. The sample referenced that package already, so no native dependency is new), the four smoke lines, the new vertex count for section 14, the selector text of task 2.7, and each decision that the brief did not settle.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (6):** `HelloPanel.csproj`, `HelloScreen.css`, `HelloScreen.cs`, `Program.cs`, `ParsedSheetAllocationTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] `scripts/test-linux.sh` prints its PASS line, and the smoke log holds the gate line with `P` and `K` above 0.
- [ ] `HelloScreen.css` holds `.hello-panel:hover` and `.hello-panel:focus`. `TheHelloPanelSheetGivesTheOldValues` passes with 6 rules.
- [ ] `Program.cs` sets `input.KeyboardEnabled` before `Glfw.PollEvents()` and creates `GlfwUiInput` after both ImGui backends.

---

### Phase 3: The Full-Queue Gate and the Record
**Goal:** Prove 0 bytes for a full ring that `Update` drains on the warm 1000-element tree. Prove that a planted allocation in the input pass fails the unit gate and the smoke run. Record the feature in the technical design and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 14 with `failed: 0`. A planted allocation in the getter of `UiContext.RoutingGate` makes `dotnet test --solution brutal-ui.sln` exit non-zero and makes the HelloPanel smoke run exit with code 1.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tier note:** When the queue gate measures bytes above 0, find the cause before a change. Run the step with one event kind at a time in a scratch copy of the test. Expected suspects: a delegate that the step creates, a boxed enum, a growth of the hit, focus, or path stack after the warm-up, or a counter handler that allocates. Fix the cause in `src/` with no public API change. Do not add warm-up rounds to hide a growth. Do not edit `ThousandElementTree.cs`: the benchmark project links it and cannot read internal members.

**Tasks:**

Group A (the gate, 1 file and 2 temporary edits. Task 3.7 records the results):
- [ ] 3.1 Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: add a private sealed class `QueueScene` with `required` members: the `Scene`, the slot point (the center of `PointerTree.ViewportRect(tree.GridSlot)` after the first `Update`), the stat row point (the center of stat row 0), and the 16 prepared events of one block, each with a null target: (1) a move at the slot point, (2) a move at the stat row point, (3) a scroll at the slot point, (4) a left down and (5) a left up at the slot point, (6) a key A (65) down and (7) up, (8) a char `'a'` (97), (9) Tab, (10) Shift+Tab, (11) a move, (12) a left down, and (13) a left up at (2000, 2000), (14) Tab, (15) a move at (NaN, NaN), and (16) a move at the slot point. Add a private sealed class `QueueCounter` with one count and one method for each of the 7 kinds. `BuildQueueScene()` builds the warm scene, sets `IsFocusable` on each of the 120 grid slots (`tree.GridSlot.Parent!.Children`) and on `tree.Settings.Children[0]`, and registers the 7 counter delegates on each grid slot one time, before any measure.
- [ ] 3.2 Add `QueueStep(QueueScene scene)`: push the block `EventQueueCapacity / 16` times (16 blocks with the default ring, so the ring is full and drops nothing), then call `Step(scene.Scene)`, which runs the rotating change and `Update`, so `Update` drains the full ring. Then store `RoutingGate.PointerOverUi` and `KeyboardFocusInUi` in two `int` sums of the scene, so the compiler keeps the read. Add `[Fact] UpdateDrainsAFullQueueWithNoAllocation`: 2 warm-up rounds of `KindCount * 2` steps, `AllocationGate.ReleaseAllocationContext()`, then 100 measured steps. Assert `Assert.Equal(0L, after - before)`, the same `DroppedEventCount` before and after the measure, and `QueuedEventCount` 0 after it. The measured body holds no lambda, no boxing, no string, and no `Assert`.
- [ ] 3.3 Add `[Fact] TheQueueStepDoesRealWork`: from `scene.StepCount = LeafKind` (the settings panel is hidden), one queue step gives these counts on the slots: 16 times 2 moves, 1 scroll, 1 down, 1 up, 3 key downs (A, the Tab walk to slot 0, and the Shift+Tab walk to slot 1), 1 key up, and 1 char. After the step, `FocusedElement` is grid slot 0, both gate values are `true`, `QueuedEventCount` is 0, and `DroppedEventCount` is 0. When the real counts differ from this list, find the cause and record it in `context.md`. Do not change the expected values to match without a reason. Edit the class remarks: the class holds 14 tests, and a paragraph describes the queue case, the 16-event block, and the full ring.
- [ ] 3.4 Prove that the gates block. Copy `src/BrutalUi.Core/Tree/UiContext.cs` to the session scratch folder. Plant 1: change the getter of `RoutingGate` into a block body with `GC.KeepAlive(new object());` as its first line. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit with `UpdateDrainsAFullQueueWithNoAllocation` as a failed gate. Run `dotnet build samples/HelloPanel/HelloPanel.csproj 2>&1 | tail -n 3`, then the smoke command of `scripts/test-linux.sh` with `--frames 120` (the `env`, `timeout`, and `xvfb-run -a` parts) followed by `; echo "exit=$?"`, and expect `exit=1` and the line `HelloPanel: Update and Draw allocated <B> bytes after warm-up.` on standard error. Plant 2: restore the file, then put the same line first in `DrainEvents`. Run the same two commands and expect a non-zero test exit and `exit=1` from the sample. Restore the file with `cp`, check it with `cmp`, and run the tests again: 0 failed. Report the exit codes and the names of the failed gates in the hand-back.

Group B (the record, 3 files):
- [ ] 3.5 Edit `docs/technical-design.md`. Section 4 "Overview": add the input pass line under `UiContext.Update`, before the style pass. Section 4 "Frame order": item 2 names the input pass first. A new sentence states: the game reads `ui.RoutingGate` after `Update`. Section 5 "The drain": `Update` calls the drain first. Remove "The event-model feature does not call it". Section 9 "Flow": item 2 states that `Update` drains the ring at its start. Item 5 names HelloPanel as the example of the layout-root rule. Item 7, the sub-bullet "The pointer side belongs to the routing-gate feature" becomes the pointer rule of Decision 5. Item 8 describes `UiContext.RoutingGate`, the struct, the two rules, the live read, and the still-pointer limit. Section 14: `RoutingGateTests` (8 tests), the queue case of `ThousandElementAllocationTests` (14 tests) with the plant results of task 3.4, and the HelloPanel smoke run with four lines, the new vertex count from phase 2, the synthetic input, and the exit on no gate work.
- [ ] 3.6 Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`: one line for `UiRoutingGate`, `UiContext.RoutingGate`, the full-queue allocation case, and the HelloPanel input demo (input-routing/routing-gate). Under `### Changed`: one line, `UiContext.Update` drains the event ring before the style pass, and a handler that calls `Update` gets `InvalidOperationException`.
- [ ] 3.7 Run the build, the tests, the demo class 5 times, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the gate results, the plant results, and the final test counts in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (5):** Group A: `ThousandElementAllocationTests.cs` and temporary edits of `UiContext.cs` (restored). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `context.md`.

**Acceptance Criteria:**
- [ ] `ThousandElementAllocationTests` passes with 14 tests in 5 runs, and the queue gate measures 0 bytes.
- [ ] Each plant gives a non-zero test exit and the sample exit code 1. The restored file gives 0 failed.
- [ ] Sections 4, 5, 9, and 14 of the technical design match the code. No sentence says that `Update` does not drain.

---

**Total Estimated Steps:** about 138 (23 tasks over 3 phases)

---

## Key Technical Decisions

### Confirmed Assumptions

No feature-level interview ran: `/autorun-epic` drafts this plan in its called mode, and no person can answer. The items below are the epic-level items that the user confirmed:
- Q1 The GLFW adapter lives in a new `BrutalUi.Glfw` package. Core stays free of GLFW.
- Q2 Focus moves on click, Tab, and Shift+Tab. Gamepad waits for the action map.
- Q3 Build order: event-model, pointer-routing, keyboard-focus, routing-gate.
- Q4 Four features. routing-gate closes the epic.
- A1 `BrutalUi.Core` gets no GLFW reference. The input types in Core accept plain structs, and `BrutalUi.Glfw` converts the `GlfwWindow` events.
- A2 A handler registers as a delegate per event kind on `Element` at build time, not in the frame loop. The walk calls the delegates with a `ref` event struct.
- A3 The hit test uses the axis-aligned layout rects and the clip rects only. No transforms, no custom shapes.
- A4 Drop and content-scale events go to `UiContext` callbacks. They get no per-element routing.
- A5 The adapter queues events in a fixed ring buffer. The input pass drains it in `UiContext.Update` before the style pass.
- A6 The gate reads the hit result of the last pointer position and the focus owner. The game polls it after `Update`.

### Decision 1: The gate is a readonly struct that one property returns
**Choice:** `public readonly struct UiRoutingGate` with `PointerOverUi` and `KeyboardFocusInUi`. `UiContext.RoutingGate` returns a new value for each read. The constructor is internal.
**Rationale:** The requirement and section 9 of the technical design name `UiRoutingGate.PointerOverUi`. The requirement allows "a struct or a pair of properties on `UiContext`". One read gives a pair from the same moment. A struct copy allocates nothing. An internal constructor keeps the game from making a gate that the context did not give. The read needs no event (the requirement).
**Alternatives:** (a) Two `bool` properties on `UiContext` (`PointerOverUi`, `KeyboardFocusInUi`): the name `UiRoutingGate` of the requirement and the design then has no type. (b) A class that the context owns: a second object for two values. (c) A change event: the requirement forbids an event, and a game polls each frame anyway.

### Decision 2: Update calls the drain as its first statement
**Choice:** `Update` calls `DrainEvents()` first and reads the roots span after it. The body of `DrainEvents` does not change. `DrainEvents` stays internal.
**Rationale:** A5 and the requirement. The states change before the style pass, so a `:hover` or `:focus` rule restyles in the same `Update`. The drain guard exists already, and its message names `Update`, so a re-entrant `Update` throws with no new code. A handler can add or remove a root, so the span comes after the drain. An internal drain means that a game cannot call it at the wrong time.
**Alternatives:** (a) A public `ProcessInput()` that the game calls before `Update`: a game can forget it, and A5 names `Update`. (b) The drain after the style pass: a state change then shows one frame late. (c) A flag that turns the drain off: no requirement asks for it, and the tests call `DrainEvents` directly already.

### Decision 3: The gate reads the live state
**Choice:** `RoutingGate` computes the value from `_pointerHit` and `_focusOwner` at each read (Notes Q2).
**Rationale:** The requirement: "true when the last hit result of pointer-routing is an element" and "true when a focus owner exists". The live read uses the same two fields that hold `Hover` and `Focus`, so the gate and the states agree at each moment. A code focus call (`SetFocus` when a dialog opens) shows in the gate at once. Each pointer event hit-tests again in the drain, so a click after a panel closes reads the new layout before the game reads the gate.
**Alternatives:** (a) A hit test of the stored position after the layout pass (Notes Q2, option B): a gate that follows a layout change under a still pointer, at the cost of one hit test for each frame, and a gate that can disagree with `Hover`. (b) A snapshot at the end of `Update` (Notes Q2, option C): a `SetFocus` call after `Update` then shows one frame late.

### Decision 4: The ring keeps 256 entries, and the drain in each Update closes CR-5
**Choice:** `DefaultEventQueueCapacity` stays 256 (Notes Q4). The remarks of `Update` and section 9 state the rule: the input pass drains each frame, and a game with long hitches sets `CreateInfo.EventQueueCapacity`. No move merge.
**Rationale:** The main cause of CR-5 was a ring that no code drained. With the drain in each `Update`, the ring holds the events of one frame. 256 entries hold about 250 ms of a 1000 Hz mouse plus the keys. A bigger default costs 64 bytes for each entry in each context. A move merge changes the drop-oldest rule of event-model and needs its own decision (CR-5 of pointer-routing).
**Alternatives:** (a) 1024 entries (64 KB for each context). (b) A merge of the moves at the push when the ring is full.

### Decision 5: The pointer side of the ImGui rule is a rule for the game, not a switch
**Choice:** `GlfwUiInput` gets no pointer switch (Notes Q3). The remarks of `UiRoutingGate` and section 9 state the rule: with an ImGui window next to the UI, the game uses `gate.PointerOverUi || io.WantCaptureMouse` for its own input. The keyboard keeps `KeyboardEnabled` (Decision 11 of keyboard-focus). HelloPanel has no ImGui window, so it shows the rule as a comment at the gate read.
**Rationale:** The requirements name no switch. A pointer switch has a hard case: a press that goes to the UI and a release that ImGui takes leaves `Active` on the UI element, and the click focus moves in a half state. No interactive element exists yet, so no wrong click can occur. Conflicts 8 of keyboard-focus gives this choice to the gate design, and the gate design answers it with a combine rule that costs no code.
**Alternatives:** (a) `GlfwUiInput.PointerEnabled` from `!io.WantCaptureMouse`: the adapter pushes one leave, then drops the moves, the buttons, and the scrolls (Notes Q3, option B). (b) Option (a) plus a release of each pressed button at the switch (Notes Q3, option C).

### Decision 6: The queue case fills the ring, then runs the rotating step
**Choice:** Each queue step pushes a block of 16 events `EventQueueCapacity / 16` times (256 entries), then calls the existing `Step`, which makes one of the nine tree changes and calls `Update`. The block holds each event kind, a click focus, a Tab, a Shift+Tab, a Tab with no owner, a press over nothing, and a cursor leave. The step reads the gate.
**Rationale:** The requirement: "drains a full queue on a warm 1000-element tree and measures 0 bytes". The keyboard-focus seam asks for a key entry. The rotating step runs the drain under each kind of tree change, also with the settings panel shown, so the hit test and the Tab search see two tree sizes. A ring that is exactly full shows that a full ring with no drop costs nothing. The gate read in the measured body lets plant 1 prove that the read is measured.
**Alternatives:** (a) A separate scene with no tree change: the drain then sees one layout only. (b) A ring that overflows each step: the drop path has its own gate in `EventAllocationTests` already.

### Decision 7: HelloPanel makes the panel the focusable and hoverable element
**Choice:** The panel gets `IsFocusable = true`. The root, the two labels, and the image get `IsPickable = false`. The `:hover` rule changes the fill of the panel, and the `:focus` rule changes its border color. A second label shows the gate.
**Rationale:** `Hover` goes to the hit element only (pointer-routing), so non-pickable children let each point inside the panel hit the panel. A non-pickable root shows the layout-root rule (P6-8 of pointer-routing): the gate reads `false` outside the panel. One focusable element is the seam of keyboard-focus. A new button element belongs to interactive-elements. The gate label sits before the image, so the draw command count stays 2.
**Alternatives:** (a) A new box element as a "button": scope of interactive-elements. (b) A pickable root: the gate then reads `true` over the full window, and the demo shows the wrong habit.

### Decision 8: The smoke run pushes synthetic input and checks that it reaches the UI
**Choice:** A run with `--frames` pushes, in each frame, a move (over the panel in an even frame, over the empty root in an odd frame), a Tab down, and a Tab up. The pushes, `Update`, `Draw`, the gate read, and the label update stay in the measured UI region. A fourth output line gives the gate counts. The run exits with code 1 when it measured 2 frames or more and a count is 0. A run with no frame limit pushes nothing synthetic and uses the real `GlfwUiInput`.
**Rationale:** The requirement: the smoke run pushes a synthetic pointer move and a Tab, and it exits with code 1 when the input pass allocates. The xvfb display sends no real input, so with no synthetic event the input pass drains an empty ring and the gate proves little. The count check stops a gate that passes with no work (the pattern of `TheMeasuredBodyDoesRealWork`). The existing three lines and the exit codes keep their text, so `scripts/` needs no change. The sample cannot call the internal `Push*` methods of the adapter, so it uses the public `PushEvent`.
**Alternatives:** (a) The synthetic input in the warm-up frames only: the measured frames then drain an empty ring. (b) No count check: a broken push passes the smoke run. (c) A separate input region with its own sum: the drain runs inside `Update`, so the sample cannot measure it apart.

### Decision 9: The gate label uses four UTF-8 literals and starts with the longest text
**Choice:** `HelloScreen.GateText` returns one of four `u8` literals. The label starts with the longest one. The sample sets the text after `Draw`, so the label shows the gate of the last frame.
**Rationale:** `Label.SetText(ReadOnlySpan<byte>)` copies into a buffer that grows by doubling. A buffer that holds the longest text never grows after warm-up, so the UI region stays at 0 bytes. A `u8` literal allocates nothing. A text change after `Draw` keeps the layout and the draw of one frame equal.
**Alternatives:** (a) `SetText(string)` with interpolation: a string for each frame. (b) A text change before `Draw`: the label draws a new text in an old rect for one frame.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| An existing test pushes, calls `Update`, and then expects the events in the ring. | Low | Medium | Task 1.6. The planner checked the pattern of `PointerStateEdgeTests`: push, `DrainEvents`, then `Settle`. A changed test goes into `context.md`. |
| A handler adds or removes a root during the drain, and `Update` reads an old span. | Low | High | Decision 2 and the tier note of phase 1: the span comes after the drain. |
| An exception from a handler stops `Update` before the style pass, and a game catches it and goes on with an old style. | Low | Medium | The remarks of `Update` state the rule. The next `Update` drains the rest. Test 1.5 checks that the context stays valid. |
| The gate reads `true` over a panel that hides under a still pointer. | Medium | Low | Decision 3 and Notes Q2. A pointer event hit-tests again before the game reads the gate. The remarks state the limit. |
| A click on an ImGui window over the UI also reaches the UI. | Medium | Low | Decision 5. The game combines the gate with `io.WantCaptureMouse`. No interactive element exists yet. |
| The gate label buffer grows after warm-up, and the smoke run fails. | Low | Medium | Decision 9: the label starts with the longest text. |
| The new label adds a draw command, and a reader of section 14 sees an old count. | Low | Low | The tier note of phase 2 puts the label before the image. Task 3.5 writes the new vertex count. |
| The synthetic input does not reach the UI under xvfb, and the smoke gate passes with no work. | Low | Medium | Decision 8: the count check exits with code 1. |
| A long hitch fills the ring, and a dropped `PointerUp` keeps `Active`. | Low | Low | Decision 4 and Notes Q4. The remarks name `CreateInfo.EventQueueCapacity`. |
| The name `Glfw` in `Program.cs` resolves to the namespace `BrutalUi.Glfw`. | Low | Low | The tier note of phase 2: use an alias, as `GlfwUiInput.cs` does. |
| A plant in `DrainEvents` fails many gates, and the record lists them wrong. | Low | Low | Plant 1 in the `RoutingGate` getter fails only the queue gate and the sample. Task 3.4 records the names. |

---

## Dependencies

### Internal Dependencies
- `input-routing/event-model`: the ring, `PushEvent`, the internal `DrainEvents` and its `_draining` guard, `EventWalk`, and the gate pattern.
- `input-routing/pointer-routing`: `_pointerHit` (`PointerHit`), the stale pointer rule, `HitWalk`, `GlfwUiInput` and its creation order, `PointerTree`, and `PointerAllocationTests`.
- `input-routing/keyboard-focus`: `FocusedElement` and `_focusOwner`, `IsFocusable`, the Tab rule, the stale focus rule, `KeyboardEnabled`, and `FocusAllocationTests`. This branch holds keyboard-focus with no commit yet. Start this feature after that commit (Notes, Conflicts 1).
- `core-tree`: `Element`, `IsPickable`, `UiContext.Update`, and `AllocationGate`.
- `style-system`: `PseudoStates`, the `:hover` and `:focus` selectors, and `StyleSheetParser`.
- `imgui-renderer` and `dev-ops`: `ThousandElementTree`, `RecordingRenderer`, HelloPanel, and `scripts/test-linux.sh`.

### External Dependencies
- `Ahwoo.BRUTAL.Glfw` 2026.10.4, pinned in `Directory.Packages.props`. HelloPanel references it already. `GlfwKey.Tab` gives the Tab key in the sample.
- No new package and no new native dependency. Task 2.9 writes the note of the AGENTS.md rule.
- The smoke run needs the Linux test machine of `scripts/test-linux.sh`: `xvfb-run`, the NVIDIA Vulkan driver, and `libvulkan.so`.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs a person with a display or with Windows.

### Functional Criteria
- [ ] F1. One `Update` call drains the ring before the style pass. A queued move over `Right` and a queued Tab, with no `DrainEvents` call, give `Hover` and `Focus` on `Right`, the hover fill after that one call, an empty queue, and a gate with both values `true` (`RoutingGateTests.UpdateRunsTheInputPassBeforeTheStylePass`).
- [ ] F2. `UiContext.RoutingGate.PointerOverUi` is `true` after a pointer entry that hit an element. It is `false` with no input, after a move outside each root, after a cursor leave at (NaN, NaN), and over the empty area of a root with `IsPickable = false`. A caller target does not change it (`RoutingGateTests`).
- [ ] F3. `KeyboardFocusInUi` is `true` when `FocusedElement` is not null. `SetFocus` and `ClearFocus` change it at once, with no `Update`. A press on no focusable element makes it `false` (`RoutingGateTests`).
- [ ] F4. An `Update` with an empty queue clears both values when the hovered and focused element left the tree (`RoutingGateTests`).
- [ ] F5. A handler that calls `Update` gets `InvalidOperationException`. The next `Update` works. An event that a handler pushes waits for the next `Update` (`RoutingGateTests`, 8 tests in total).
- [ ] F6. `ThousandElementAllocationTests.UpdateDrainsAFullQueueWithNoAllocation` measures 0 bytes for 100 steps, each with 256 queued events and one `Update`, with no drop. `TheQueueStepDoesRealWork` reads the walks, the focus, and the gate of one step.
- [ ] F7. `samples/HelloPanel/HelloScreen.css` holds a `.hello-panel:hover` rule and a `.hello-panel:focus` rule. HelloPanel shows a second label with the two gate values. The smoke run pushes a pointer move and a Tab in each frame, and its log holds `HelloPanel: gate: pointer over UI in <P> frames, keyboard focus in UI in <K> frames, of <M> measured frames.` with `P` and `K` above 0.
- [ ] F8. A planted allocation in the `RoutingGate` getter, and one at the start of `DrainEvents`, each give a non-zero exit of `dotnet test` and the exit code 1 of the HelloPanel smoke run.
- [ ] F9. (MANUAL) In HelloPanel with a display, the panel fill changes under the mouse, and the label reads "Pointer over UI: yes". Outside the panel, it reads "no". Tab turns the panel border yellow, and the label reads "Keyboard focus in UI: yes". A click outside the panel clears the focus.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` has 10 more tests than the count in `context.md` before phase 1 (8 + 2). `BrutalUi.Markup.Tests` keeps its count.
- [ ] Q3. `UiRoutingGate` is a `public readonly struct` with exactly two public members, `PointerOverUi` and `KeyboardFocusInUi`, and an internal constructor. `DrainEvents` stays internal.
- [ ] Q4. The measured bodies of the queue gate hold no lambda, no `$"`, no `string`, and no `Assert`.
- [ ] Q5. The changed docs use ASD-STE100 prose. The lint gives no score above 2.5.
- [ ] Q6. `src/BrutalUi.Core/Input/EventWalk.cs` has the SHA-256 `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`.
- [ ] Q7. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no `using` of a GLFW namespace. `src/BrutalUi.Core/BrutalUi.Core.csproj` does not change.
- [ ] I2. `Directory.Packages.props`, each `.csproj` under `src/` and `tests/`, `brutal-ui.sln`, `scripts/`, `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`, and `src/BrutalUi.Core/Input/EventWalk.cs` do not change. `samples/HelloPanel/HelloPanel.csproj` gets one project reference to `BrutalUi.Glfw` and no package.
- [ ] I3. The first statement of `UiContext.Update` is the `DrainEvents()` call. The body of `DrainEvents` does not change. `PushEvent` keeps four public overloads.
- [ ] I4. `Program.cs` creates `GlfwUiInput` after `GlfwBackend` and `VulkanBackend`, and it sets `input.KeyboardEnabled = !io.WantCaptureKeyboard;` before `Glfw.PollEvents()`.
- [ ] I5. Sections 4, 5, 9, and 14 of `docs/technical-design.md` and `CHANGELOG.md` name the feature. No doc sentence says that `Update` does not call the drain.
- [ ] I6. `scripts/test-linux.sh` prints its PASS line.
- [ ] I7. `docs/features/input-routing/routing-gate/context.md` holds the native dependency note, the smoke lines, and the plant results.

### Verification Method

Run steps 1 to 10 from `/home/aaronstatic/dev/brutal-ui` on Linux. Steps 11 and 12 are MANUAL. `<scratch>` is any empty folder outside the repository. `<smoke>` is `env -u WAYLAND_DISPLAY -u VK_DRIVER_FILES -u VK_ADD_DRIVER_FILES XDG_SESSION_TYPE=x11 VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json timeout --kill-after=10 60 xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 120`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the totals with the counts in `context.md` (Q2).
3. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*RoutingGateTests' 2>&1 | tail -n 5` and expect a total of 8 with `failed: 0` (F1 to F5). Run the same with `'*ThousandElementAllocationTests'` and expect a total of 14 with `failed: 0` (F6). Read the test names and match them to the criteria.
4. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I6). `cat artifacts/test-linux/smoke.log`. Expect four `HelloPanel:` lines, `Update and Draw 0`, and the gate line with `P` and `K` above 0 (F7).
5. `grep -n 'hello-panel:hover\|hello-panel:focus' samples/HelloPanel/HelloScreen.css` gives two rule lines (F7). `grep -n 'KeyboardEnabled\|PollEvents\|new GlfwUiInput\|new GlfwBackend' samples/HelloPanel/Program.cs`. Expect `GlfwUiInput` after `GlfwBackend`, and the `KeyboardEnabled` line before `Glfw.PollEvents()` in the loop (I4).
6. Copy `src/BrutalUi.Core/Tree/UiContext.cs` to `<scratch>`. Change the getter of `RoutingGate` into a block body with `GC.KeepAlive(new object());` as its first line. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Run `dotnet build samples/HelloPanel/HelloPanel.csproj > /dev/null 2>&1; <smoke>; echo "exit=$?"` and expect `exit=1` and the "allocated" line on standard error. Restore the file from the copy and check it with `cmp` (F8).
7. Do step 6 again with the plant as the first line of `DrainEvents`. Expect the same results. Restore the file, check it with `cmp`, and run step 2 again: 0 failed (F8).
8. `grep -rn 'using .*Glfw' src/BrutalUi.Core; git diff main --stat -- Directory.Packages.props src tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs brutal-ui.sln scripts/`. Expect no line from the grep. The diff lists only `UiContext.cs`, `UiRoutingGate.cs`, and `GlfwUiInput.cs` under `src/`, and no other file (I1, I2). `git diff main -- samples/HelloPanel/HelloPanel.csproj` shows one added `ProjectReference` line (I2). `sha256sum src/BrutalUi.Core/Input/EventWalk.cs` gives the value of Q6.
9. Read `Update` in `src/BrutalUi.Core/Tree/UiContext.cs`: the first statement is `DrainEvents();` (I3). `git diff main -- src/BrutalUi.Core/Tree/UiContext.cs` shows no change inside the body of `DrainEvents` (I3). `grep -c 'public void PushEvent' src/BrutalUi.Core/Tree/UiContext.cs` gives `4` (I3). `grep -n 'public\|internal' src/BrutalUi.Core/Input/UiRoutingGate.cs` shows one `public readonly struct`, two `public bool` properties, and one `internal` constructor (Q3).
10. Read sections 4, 5, 9, and 14 of `docs/technical-design.md`, the `## [Unreleased]` part of `CHANGELOG.md`, and the feature `context.md` (I5, I7). `grep -n 'does not call the drain\|does not drain' docs/technical-design.md src/BrutalUi.Glfw/GlfwUiInput.cs` gives no line (I5). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q5).
11. (MANUAL) On a Linux or Windows desktop, run `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. Move the mouse over the panel and away from it, press Tab, and click outside the panel. Check each result of F9.
12. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0 (Q7).

---

## Testing Strategy

### Unit Tests (in `tests/BrutalUi.Core.Tests/Input/`)
- `RoutingGateTests` (8): the frame order of `Update`, the gate with no input, the pointer value, the layout-root rule, the keyboard value with the live read, the stale rules through `Update`, the re-entrant `Update`, and the push from a handler.
- Each test uses `PointerTree`, pushes hand-made events with a null target, and calls `Update` with no `DrainEvents` call. No Core test uses GLFW. Each test interns its class with a unique prefix, because xUnit runs the classes in parallel.

### Allocation Tests
- `ThousandElementAllocationTests` (12 to 14): `UpdateDrainsAFullQueueWithNoAllocation` and `TheQueueStepDoesRealWork`. The existing gates now also run an empty drain in each `Update`, so they also guard the stale rules.
- Each gate calls `AllocationGate.ReleaseAllocationContext()` right before its measured region.

### Sample and Smoke Tests
- `ParsedSheetAllocationTests.TheHelloPanelSheetGivesTheOldValues` reads 6 rules and checks the two state rules.
- The HelloPanel smoke run in `scripts/test-linux.sh`: the synthetic move and Tab, the 0-byte UI region, and the gate count check.

### Manual Testing
- F9 in HelloPanel with a display, and the Windows run of Q7.

---

## Related Documentation

- **Requirements:** `docs/features/input-routing/routing-gate/requirements.md`
- **Epic:** `docs/features/input-routing/epic-overview.md`, `docs/features/input-routing/epic-requirements.md`
- **Siblings:** `docs/features/input-routing/event-model/implementation.md` and `context.md` (Decision 6, the drain count), `docs/features/input-routing/pointer-routing/implementation.md` and `context.md` (Decisions 7, 10, R3, P6-8, and the follow-ups CR-5 and CR-8), `docs/features/input-routing/keyboard-focus/implementation.md` and `context.md` (Decision 11, "The Seams for the Siblings", and Conflicts 8)
- **Design:** `docs/technical-design.md` sections 4 (Frame order), 5 (Events), 9 (Input), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Tree/UiContext.cs` (`Update`, `DrainEvents`, `PointerHit`, `FocusedElement`), `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`, `tests/BrutalUi.Core.Tests/Input/FocusAllocationTests.cs` (the counters and the two-drain step), `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` (the rotating step), `samples/HelloPanel/Program.cs` (the measured UI region)
- **External:** the Dear ImGui FAQ entries on `io.WantCaptureMouse` and `io.WantCaptureKeyboard`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (the first one) of each question.

**Q1. What is the demo of phase 1?**
- **A (recommended):** A unit test queues a move over `Right` and a Tab, calls `Update(0f)` only, and reads the hover fill, the focus, an empty queue, and both gate values `true` (`--filter-class '*RoutingGateTests'`, total 8). Reason: it proves the frame order and both gate values with no GPU, and the slice touches only Core.
- B: The HelloPanel smoke log shows the gate line. It needs the sample work of phase 2 first.
- C: A person runs HelloPanel, moves the mouse over the panel, and sees the label change. It needs a display and the sample work.

**Q2. When does the pointer value of the gate change?**
- **A (recommended):** At each read, from the last hit of the drain (`PointerHit`). Reason: it matches the text of the requirement and the `Hover` state, and each pointer event hit-tests again before the game reads the gate.
- B: `Update` hit-tests the stored position again after the layout pass. The gate then follows a panel that closes under a still pointer, at the cost of one hit test for each frame, and the gate can disagree with `Hover`.
- C: A copy that `Update` takes at its end. A `SetFocus` call after `Update` then shows one frame late.

**Q3. How does the pointer share with an ImGui window?**
- **A (recommended):** No adapter switch. The game uses `gate.PointerOverUi || io.WantCaptureMouse`, and the docs state the rule. Reason: the requirements name no switch, a pointer switch can leave `Active` and the click focus in a half state, and no interactive element exists yet.
- B: `GlfwUiInput.PointerEnabled`, set from `!io.WantCaptureMouse` before `Glfw.PollEvents()`. With `false`, the adapter pushes one leave and then drops the moves, the buttons, and the scrolls.
- C: Option B plus a release of each pressed button at the switch, so `Active` cannot stay.

**Q4. What size does the ring need for a hitch (CR-5 of pointer-routing)?**
- **A (recommended):** Keep 256 entries and document the rule: `Update` drains each frame, and a game with long hitches sets `CreateInfo.EventQueueCapacity`. Reason: the drain in each `Update` removes the main cause, and 256 entries hold about 250 ms of a 1000 Hz mouse.
- B: Raise `DefaultEventQueueCapacity` to 1024 (64 KB for each context).
- C: Merge the moves at the push when the ring is full. It changes the drop-oldest rule of event-model and needs its own decision.

### Conflicts and Gaps

1. **keyboard-focus has no commit yet.** The git status shows its files as uncommitted on `feat/input-routing/keyboard-focus`. Phase 1 starts after that commit. The hash of `EventWalk.cs` comes from Decision R3 of pointer-routing.
2. **"A struct or a pair of properties".** The plan uses both: one struct with two properties, and one property on `UiContext` that returns it (Decision 1). The access path in game code is `ui.RoutingGate.PointerOverUi`. Section 9 item 8 of the technical design names `UiRoutingGate.PointerOverUi`, so task 3.5 adds the access path.
3. **The HelloPanel sheet test.** `ParsedSheetAllocationTests.TheHelloPanelSheetGivesTheOldValues` asserts 4 rules. Task 2.7 changes the count to 6 and checks the two new rules.
4. **The output contract of HelloPanel.** Section 14 of the technical design names three lines. The plan adds a fourth line and keeps the first three. The table in `docs/features/dev-ops/solution-skeleton/context.md` is a record of that feature, and it already names one line only. The plan does not edit it.
5. **The still pointer (A6).** "The hit result of the last pointer position" is the hit of the last pointer event, not a new hit test (Q2). A layout change under a still pointer keeps the value until the next pointer event.
6. **The pointer side of the ImGui rule.** Conflicts 8 of keyboard-focus gives it to this feature. Decision 5 answers with a rule for the game and no switch (Q3).
7. **The layout-root rule (P6-8 of pointer-routing).** The library keeps `IsPickable` true by default on each root. HelloPanel shows the rule with `root.IsPickable = false`, and the remarks of `UiRoutingGate` state it.
8. **The re-entrant Update.** The guard and its message exist in `DrainEvents` already. This feature adds the docs and a test, and no code.
9. **The draw count of HelloPanel.** The second label changes the vertex count of the last frame. Task 2.8 reads the new value, and task 3.5 writes it into section 14.
10. **The epic status.** This feature closes the epic. `/update-epic input-routing` updates `epic-overview.md` and the master overview after the commit. This plan does not edit them.
11. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 3.7 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
