# input-routing/routing-gate - Task Checklist

**Last Updated:** 2026-10-22 23:51
**Progress:** 23/23 tasks complete (100%)
**Advanced phases:** Phases 1 and 2 (ADVANCED, opus). Phase 3 is STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. `Glfw/` means `src/BrutalUi.Glfw/`. "The plan" means `implementation.md` of this feature. Its section "Implementation Phases" holds the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment. Use the file-scoped `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the Core tests. Prose in comments follows ASD-STE100. Each phase checks that the SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` stays `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (Decision R3 of pointer-routing).

---

## Phase 1: The Input Pass and the Gate (7/7 complete)

**Demo:** a unit test marks `Right` of the pointer tree focusable, adds the hover sheet, pushes a pointer move over `Right` at (180, 80) and a Tab key down, both with no target, and calls `Update(0f)` only, with no `DrainEvents` call. It reads `QueuedEventCount` 0, `PseudoStates.Hover` and `PseudoStates.Focus` on `Right`, the hover fill in `Right.Style.BackgroundColor`, and `RoutingGate` with `PointerOverUi` and `KeyboardFocusInUi` both `true`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*RoutingGateTests'` and reads a total of 8 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes `UiContext.Update`, the method that each game calls each frame. Add one call, `DrainEvents();`, as the first statement. Read `_roots.AsSpan()` after the call, not before it: a handler can call `AddRoot` or `RemoveRoot` during the drain, and a span from before the drain then reads old storage. Do not change the body of `DrainEvents`, `RoutePointer`, the stale rules, or `EventWalk.cs`. Do not move the drain into the measure scope of the layout pass. The re-entrant guard exists already: the message of the `_draining` check names `Update`. After a handler exception, the `finally` block of the drain clears the flag, and `Update` ends before the style pass. Keep this behavior and document it.

- [x] ✅ **1.1 Write UiRoutingGate.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Input/UiRoutingGate.cs`: `public readonly struct UiRoutingGate` with `internal UiRoutingGate(bool pointerOverUi, bool keyboardFocusInUi)`, `public bool PointerOverUi { get; }`, and `public bool KeyboardFocusInUi { get; }`. The summary: the two values that tell the game when the UI owns the pointer or the keyboard. The remarks state: read the value from `UiContext.RoutingGate` after `UiContext.Update` and before the game handles its own input. The value is a copy, and a later change of the context does not change it. `default` gives two `false` values. `PointerOverUi` is true when the last pointer or scroll entry with no target hit an element. `KeyboardFocusInUi` is true when the context has a focus owner. The gate knows nothing of ImGui: combine `PointerOverUi` with `io.WantCaptureMouse`, and set `GlfwUiInput.KeyboardEnabled` from `io.WantCaptureKeyboard` (Decision 5). The layout-root rule: a root that covers the window makes `PointerOverUi` true over the full window. Set `IsPickable` to `false` on such a root (P6-8 of pointer-routing).
  - File(s): `src/BrutalUi.Core/Input/UiRoutingGate.cs`
  - Estimate: 5 steps

- [x] ✅ **1.2 Add UiContext.RoutingGate** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `public UiRoutingGate RoutingGate => new(_pointerHit is not null, _focusOwner is not null);` next to `FocusedElement`. The remarks state (Decision 3): the property reads the current state, so the value changes during a drain, at `SetFocus`, `ClearFocus`, `FocusNext`, and `FocusPrevious`, and at the stale rules of the next `Update`. The pointer value follows the pointer events only: a layout change under a still pointer, for example a panel that closes, keeps the value until the next pointer event or the next stale rule. A press, a release, a move, and a scroll each hit-test again before the game reads the gate. An entry with a target from the caller changes no value. The read allocates nothing.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **1.3 Call DrainEvents first in Update** - Completed 2026-10-22
  - Description: Edit `Update` in `UiContext.cs`: the first statement is `DrainEvents();` with the comment "The input pass (A5): the drain walks the queued events and changes the states before the style pass reads them." Move `ReadOnlySpan<Element> roots = _roots.AsSpan();` after the call. Change nothing else in `Update`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] ✅ **1.4 Edit the doc comments of the input pass** - Completed 2026-10-22
  - Description: Edit the doc comments in `UiContext.cs`. The class summary: `Update` runs the input pass, then the other passes. The remarks of `Update`: the frame order paragraph names the input pass first. Add a paragraph: the input pass drains each entry that the ring holds at the start of the call. The hit test reads the layout of the last `Update`. An event that a handler pushes waits for the next `Update`. A handler that calls `Update` gets `InvalidOperationException`. An exception from a handler stops the call before the style pass, the entries after it stay in the ring, and the next `Update` drains them. Read `RoutingGate` after the call. Add the second `<exception cref="InvalidOperationException">` case (a call from an event handler). In `DrainEvents`, replace the A5 paragraph ("routing-gate calls this method ... In this feature ... the tests call it directly") with: `Update` calls this method first. The tests also call it directly. In `PointerHit` and `FocusedElement`, replace "Routing-gate reads this member" with "`RoutingGate` reads this member". In the summaries of the four `PushEvent` overloads, "The next drain" becomes "The input pass of the next `Update`". Edit `src/BrutalUi.Glfw/GlfwUiInput.cs`: the class remarks paragraph that starts "The adapter only pushes the events" states that `UiContext.Update` drains the ring at its start.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`, `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 7 steps

- [x] ✅ **1.5 Write RoutingGateTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/RoutingGateTests.cs` with 8 tests. Each test uses `PointerTree` with a prefix from `nameof`, and each test calls `Update` and no `DrainEvents`, except where the test name says so:
    - `UpdateRunsTheInputPassBeforeTheStylePass` (the demo).
    - `TheGateIsFalseWithNoInput`: after `Settle()`, both values are `false`. `default(UiRoutingGate)` gives both `false`.
    - `PointerOverUiFollowsTheLastHit`: a move over `Right` gives `true`. A move to (500, 500) outside the root gives `false`. A move to (NaN, NaN), the cursor leave, gives `false`. A move with a target from the caller (`Left`) keeps the value of the last hit.
    - `ANonPickableRootPassesThePointerToTheGame`: `Root.IsPickable = false`. A move over the gap of the root at (300, 200) gives `false`. A move over `Left` at (50, 50) gives `true` (P6-8 of pointer-routing).
    - `KeyboardFocusInUiFollowsTheFocusOwner`: `Left` and `Right` are focusable. A Tab gives `true`. A press over the gap of the root (no focusable element) gives `false`. `SetFocus(Right)` gives `true` at once, with no `Update`. `ClearFocus()` gives `false` at once.
    - `TheStaleRulesClearTheGateAtTheNextUpdate`: a move over `Right` and `SetFocus(Right)`, then `Update`. Remove `Right` from the root. An `Update` with an empty queue gives both values `false`, and `Right` holds no `Hover` and no `Focus`.
    - `AnUpdateFromAHandlerThrowsAndTheContextStaysValid`: an `OnPointerMove` handler on `Left` calls `tree.Context.Update(0f)`. A move over `Left` and `Assert.Throws<InvalidOperationException>(() => tree.Context.Update(0f))`. Then `Left.OnPointerMove = null`, and the next `Update(0f)` does not throw and leaves `QueuedEventCount` 0.
    - `AnEventThatAHandlerPushesWaitsForTheNextUpdate`: an `OnPointerMove` handler on `Left` pushes a move over `Right` one time. After the first `Update`, `QueuedEventCount` is 1 and `PointerOverUi` is `true` with `Left` hovered. After the second `Update`, the queue is empty and `Right` holds `Hover`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/RoutingGateTests.cs`
  - Estimate: 10 steps

- [x] ✅ **1.6 Run the gate and check the existing tests** - Completed 2026-10-22
  - Description: Run the build and the full tests. Expect no failure in the existing classes: the existing tests push, call `DrainEvents()`, and then call `Settle()`, so the drain in `Update` finds an empty ring. When a test fails because `Update` now drains, read it. Record the test, the cause, and the fix in `context.md`. Change only the order of a push and a `Settle()` call, and change no assert. When a fix needs more, stop and report to the main thread. Run the demo command and the classes `FocusOwnerTests`, `ClickFocusTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `PointerStateEdgeTests`, `PointerAllocationTests`, `FocusAllocationTests`, and `ThousandElementAllocationTests` with no edit. Check the hash of `EventWalk.cs`. Run `git diff -- src/BrutalUi.Core/Tree/UiContext.cs` and check that the body of `DrainEvents` has no change.
  - File(s): none (verification), test files only when a push and `Settle()` order needs a change
  - Estimate: 6 steps

- [x] ✅ **1.7 Record the phase 1 results** - Completed 2026-10-22
  - Description: Record the test counts before and after, the hash, the result of the `DrainEvents` diff check, and each decision that the brief did not settle in `docs/features/input-routing/routing-gate/context.md`.
  - File(s): `docs/features/input-routing/routing-gate/context.md`
  - Estimate: 3 steps

---

## Phase 2: The HelloPanel Demo and the Smoke Gate (9/9 complete)

**Demo:** the developer runs `scripts/test-linux.sh; echo "exit=$?"` and reads `test-linux: PASS build, test, gpu, smoke (120 frames)` and `exit=0`. The file `artifacts/test-linux/smoke.log` holds `HelloPanel: bytes after warm-up: Update and Draw 0, ...` and the new line `HelloPanel: gate: pointer over UI in <P> frames, keyboard focus in UI in <K> frames, of 117 measured frames.` with `P` and `K` both above 0.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase touches four integration points at once: the adapter next to the ImGui `GlfwBackend`, the measured UI region of the sample, the sheet that a Markup test parses, and the output contract of the smoke run. Create `GlfwUiInput` after `GlfwBackend` and `VulkanBackend`, so a `using` declaration disposes it first (the creation order of the adapter remarks). Keep the UI region free of a hidden allocation: the synthetic events are structs, the gate text comes from four UTF-8 literals (`"..."u8`), and the gate label starts with its longest text, so its buffer never grows after warm-up. Put the gate label before the image, so the panel, the two labels, and the image keep 2 draw commands. When `Glfw` resolves to the namespace `BrutalUi.Glfw` in `Program.cs`, use an alias as `GlfwUiInput.cs` does. Do not change `scripts/`, the first three output lines, or the exit codes 0 and 2.

- [x] ✅ **2.1 Reference BrutalUi.Glfw from HelloPanel** - Completed 2026-10-22
  - Description: Edit `samples/HelloPanel/HelloPanel.csproj`: add `<ProjectReference Include="../../src/BrutalUi.Glfw/BrutalUi.Glfw.csproj" />` to the project reference group. Add no package. The sample references `Ahwoo.BRUTAL.Glfw` already, so the native GLFW library is not new.
  - File(s): `samples/HelloPanel/HelloPanel.csproj`
  - Estimate: 2 steps

- [x] ✅ **2.2 Add the :hover and :focus rules to HelloScreen.css** - Completed 2026-10-22
  - Description: Edit `samples/HelloPanel/HelloScreen.css`: add `--panel-hover: #283044;` and `--panel-focus: #f0c040;` to `:root`. Add the rule `.hello-panel:hover { background-color: var(--panel-hover); }` and the rule `.hello-panel:focus { border-color: var(--panel-focus); }` after the `.hello-panel` rule, each with a one-line comment. The two rules set different members, so hover and focus show at the same time.
  - File(s): `samples/HelloPanel/HelloScreen.css`
  - Estimate: 3 steps

- [x] ✅ **2.3 Make the panel focusable and add the gate label in HelloScreen.cs** - Completed 2026-10-22
  - Description: Edit `samples/HelloPanel/HelloScreen.cs`. `Build(TextureHandle checker)` becomes `Build(TextureHandle checker, out Element panel, out Label gateLabel)`. Set `root.IsPickable = false` (the layout-root rule), `panel.IsFocusable = true`, and `IsPickable = false` on the greeting, the gate label, and the image, so each point inside the panel hits the panel. Create the gate label with the longest gate text, and add it to the panel between the greeting and the image. Add `public static ReadOnlySpan<byte> GateText(UiRoutingGate gate)`: it returns one of four `u8` literals, `"Pointer over UI: no. Keyboard focus in UI: no."` to `"Pointer over UI: yes. Keyboard focus in UI: yes."`. Edit the class remarks: the screen reacts to the pointer and to Tab, the root passes the pointer to the game outside the panel, the two state rules, and the gate label.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 7 steps

- [x] ✅ **2.4 Create GlfwUiInput in RunImGui** - Completed 2026-10-22
  - Description: Edit `samples/HelloPanel/Program.cs`, `RunImGui`: after the `VulkanBackend` declaration, add `using GlfwUiInput input = new(new GlfwUiInput.CreateInfo { Window = window, Context = ui });`. Call `HelloScreen.Build(handle, out Element panel, out Label gateLabel)`, and pass `input`, `panel`, and `gateLabel` to `RunFrames`.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 4 steps

- [x] ✅ **2.5 Add the smoke input and the gate read to RunFrames** - Completed 2026-10-22
  - Description: Edit `RunFrames` in `Program.cs`. Before `Glfw.PollEvents()`, add `input.KeyboardEnabled = !io.WantCaptureKeyboard;`. At the start of the UI region, when the run has a frame limit (the smoke run), push one `PointerEvent` (`PointerMove`, `Glfw.Time`, a null target, `PointerButton.Left`, `UiModifiers.None`) and one Tab `KeyEvent` down and one up (`Key = (int)GlfwKey.Tab`, scan code 0, no repeat, a null target). The move goes to the center of the viewport rect of `panel` (the sum of the rects of the panel and the root) in an even frame, and to `(viewport.X - 8, viewport.Y - 8)` in an odd frame. A run with no frame limit pushes no synthetic event. After `ui.Draw(uiRenderer)` and still inside the UI region, read `UiRoutingGate gate = ui.RoutingGate;`, call `gateLabel.SetText(HelloScreen.GateText(gate));`, and count the measured frames with each value `true`. Add a comment at the gate read: the game handles the world input here, and with an ImGui window it uses `gate.PointerOverUi || io.WantCaptureMouse` (Decision 5).
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 8 steps

- [x] ✅ **2.6 Add the gate line and the no-work exit to Run** - Completed 2026-10-22
  - Description: Edit `Run` and `RunResult` in `Program.cs`: add `PointerOverUiFrames` and `KeyboardFocusInUiFrames` to `RunResult`. After line 3, print `HelloPanel: gate: pointer over UI in <P> frames, keyboard focus in UI in <K> frames, of <M> measured frames.` After the allocation check, when the run has a frame limit, 2 or more measured frames, and `P` or `K` is 0, write `HelloPanel: the synthetic input did not reach the UI.` to standard error and return `ExitFailure` (Decision 8). Edit the remarks of `Program`, `Run`, and `RunFrames`: the four output lines, the frame order with the input pass and the gate read, the smoke input, the `KeyboardEnabled` line, and the region that the gate measures.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 6 steps

- [x] ✅ **2.7 Check the two state rules in ParsedSheetAllocationTests** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`, `TheHelloPanelSheetGivesTheOldValues`: the rule count becomes 6. Add checks with `FindRule`: `.hello-panel:hover` sets only `BackgroundColor`, with the value `#283044`, and `.hello-panel:focus` sets only `BorderColor`, with the value `#f0c040`. Read the `ToString` text of a parsed state selector first, and use that text in `FindRule`. Keep each old check.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`
  - Estimate: 5 steps

- [x] ✅ **2.8 Run the gate and the smoke script** - Completed 2026-10-22
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Read `artifacts/test-linux/smoke.log`: the four lines, the draw command count (expect 2), the new vertex count, and the gate counts (expect `P` near 58 and `K` near 117 for 120 frames).
  - File(s): none (verification)
  - Estimate: 5 steps

- [x] ✅ **2.9 Record the phase 2 results** - Completed 2026-10-22
  - Description: Record in `context.md`: the native dependency note (AGENTS.md rule: HelloPanel now references `BrutalUi.Glfw`, which uses the native GLFW library of `Ahwoo.BRUTAL.Glfw`. The sample referenced that package already, so no native dependency is new), the four smoke lines, the new vertex count for section 14, the selector text of task 2.7, and each decision that the brief did not settle.
  - File(s): `docs/features/input-routing/routing-gate/context.md`
  - Estimate: 3 steps

---

## Phase 3: The Full-Queue Gate and the Record (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 14 with `failed: 0`. A planted allocation in the getter of `UiContext.RoutingGate` makes `dotnet test --solution brutal-ui.sln` exit non-zero and makes the HelloPanel smoke run exit with code 1.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)
**Tier note:** When the queue gate measures bytes above 0, find the cause before a change. Run the step with one event kind at a time in a scratch copy of the test. Expected suspects: a delegate that the step creates, a boxed enum, a growth of the hit, focus, or path stack after the warm-up, or a counter handler that allocates. Fix the cause in `src/` with no public API change. Do not add warm-up rounds to hide a growth. Do not edit `ThousandElementTree.cs`: the benchmark project links it and cannot read internal members.

Group A (the gate, 1 file and 2 temporary edits. Task 3.7 records the results):

- [x] ✅ **3.1 Add QueueScene, QueueCounter, and BuildQueueScene** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: add a private sealed class `QueueScene` with `required` members: the `Scene`, the slot point (the center of `PointerTree.ViewportRect(tree.GridSlot)` after the first `Update`), the stat row point (the center of stat row 0), and the 16 prepared events of one block, each with a null target: (1) a move at the slot point, (2) a move at the stat row point, (3) a scroll at the slot point, (4) a left down and (5) a left up at the slot point, (6) a key A (65) down and (7) up, (8) a char `'a'` (97), (9) Tab, (10) Shift+Tab, (11) a move, (12) a left down, and (13) a left up at (2000, 2000), (14) Tab, (15) a move at (NaN, NaN), and (16) a move at the slot point. Add a private sealed class `QueueCounter` with one count and one method for each of the 7 kinds. `BuildQueueScene()` builds the warm scene, sets `IsFocusable` on each of the 120 grid slots (`tree.GridSlot.Parent!.Children`) and on `tree.Settings.Children[0]`, and registers the 7 counter delegates on each grid slot one time, before any measure.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **3.2 Add QueueStep and UpdateDrainsAFullQueueWithNoAllocation** - Completed 2026-10-22
  - Description: Add `QueueStep(QueueScene scene)`: push the block `EventQueueCapacity / 16` times (16 blocks with the default ring, so the ring is full and drops nothing), then call `Step(scene.Scene)`, which runs the rotating change and `Update`, so `Update` drains the full ring. Then store `RoutingGate.PointerOverUi` and `KeyboardFocusInUi` in two `int` sums of the scene, so the compiler keeps the read. Add `[Fact] UpdateDrainsAFullQueueWithNoAllocation`: 2 warm-up rounds of `KindCount * 2` steps, `AllocationGate.ReleaseAllocationContext()`, then 100 measured steps. Assert `Assert.Equal(0L, after - before)`, the same `DroppedEventCount` before and after the measure, and `QueuedEventCount` 0 after it. The measured body holds no lambda, no boxing, no string, and no `Assert`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.3 Add TheQueueStepDoesRealWork and edit the class remarks** - Completed 2026-10-22
  - Description: Add `[Fact] TheQueueStepDoesRealWork`: from `scene.StepCount = LeafKind` (the settings panel is hidden), one queue step gives these counts on the slots: 16 times 2 moves, 1 scroll, 1 down, 1 up, 3 key downs (A, the Tab walk to slot 0, and the Shift+Tab walk to slot 1), 1 key up, and 1 char. After the step, `FocusedElement` is grid slot 0, both gate values are `true`, `QueuedEventCount` is 0, and `DroppedEventCount` is 0. When the real counts differ from this list, find the cause and record it in `context.md`. Do not change the expected values to match without a reason. Edit the class remarks: the class holds 14 tests, and a paragraph describes the queue case, the 16-event block, and the full ring.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.4 Prove that the gates block with two plants** - Completed 2026-10-22
  - Description: Prove that the gates block. Copy `src/BrutalUi.Core/Tree/UiContext.cs` to the session scratch folder. Plant 1: change the getter of `RoutingGate` into a block body with `GC.KeepAlive(new object());` as its first line. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit with `UpdateDrainsAFullQueueWithNoAllocation` as a failed gate. Run `dotnet build samples/HelloPanel/HelloPanel.csproj 2>&1 | tail -n 3`, then the smoke command of `scripts/test-linux.sh` with `--frames 120` (the `env`, `timeout`, and `xvfb-run -a` parts) followed by `; echo "exit=$?"`, and expect `exit=1` and the line `HelloPanel: Update and Draw allocated <B> bytes after warm-up.` on standard error. Plant 2: restore the file, then put the same line first in `DrainEvents`. Run the same two commands and expect a non-zero test exit and `exit=1` from the sample. Restore the file with `cp`, check it with `cmp`, and run the tests again: 0 failed. Report the exit codes and the names of the failed gates in the hand-back.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs` (temporary, restored)
  - Estimate: 8 steps

Group B (the record, 3 files):

- [x] ✅ **3.5 Record the feature in the technical design** - Completed 2026-10-22
  - Description: Edit `docs/technical-design.md`. Section 4 "Overview": add the input pass line under `UiContext.Update`, before the style pass. Section 4 "Frame order": item 2 names the input pass first. A new sentence states: the game reads `ui.RoutingGate` after `Update`. Section 5 "The drain": `Update` calls the drain first. Remove "The event-model feature does not call it". Section 9 "Flow": item 2 states that `Update` drains the ring at its start. Item 5 names HelloPanel as the example of the layout-root rule. Item 7, the sub-bullet "The pointer side belongs to the routing-gate feature" becomes the pointer rule of Decision 5. Item 8 describes `UiContext.RoutingGate`, the struct, the two rules, the live read, and the still-pointer limit. Section 14: `RoutingGateTests` (8 tests), the queue case of `ThousandElementAllocationTests` (14 tests) with the plant results of task 3.4, and the HelloPanel smoke run with four lines, the new vertex count from phase 2, the synthetic input, and the exit on no gate work.
  - File(s): `docs/technical-design.md`
  - Estimate: 8 steps

- [x] ✅ **3.6 Record the feature in the changelog** - Completed 2026-10-22
  - Description: Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`: one line for `UiRoutingGate`, `UiContext.RoutingGate`, the full-queue allocation case, and the HelloPanel input demo (input-routing/routing-gate). Under `### Changed`: one line, `UiContext.Update` drains the event ring before the style pass, and a handler that calls `Update` gets `InvalidOperationException`.
  - File(s): `CHANGELOG.md`
  - Estimate: 3 steps

- [x] ✅ **3.7 Run the final gate and record the results** - Completed 2026-10-22
  - Description: Run the build, the tests, the demo class 5 times, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the gate results, the plant results, and the final test counts in `context.md`.
  - File(s): `docs/features/input-routing/routing-gate/context.md`
  - Estimate: 6 steps

---

**Total:** 23 tasks over 3 phases, about 138 steps.
