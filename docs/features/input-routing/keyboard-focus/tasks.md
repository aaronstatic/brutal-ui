# input-routing/keyboard-focus - Task Checklist

**Last Updated:** 2026-10-22 14:48
**Progress:** 42/42 tasks complete (100%)
**Advanced phases:** Phases 1 to 5 (ADVANCED, opus). Phase 6 is STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. `Glfw/` means `src/BrutalUi.Glfw/`. "The plan" means `implementation.md` of this feature. Its section "Implementation Phases" holds the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment. Use the file-scoped `namespace BrutalUi.Core;` in Core, `namespace BrutalUi.Glfw;` in the adapter, and `namespace BrutalUi.Core.Tests;` or `namespace BrutalUi.Glfw.Tests;` in the tests. Prose in comments follows ASD-STE100. Each phase checks that the SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` stays `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (Decision R3 of pointer-routing). Each phase runs the five event-model classes (42 tests) and the pointer classes with no edit.

---

## Phase 1: The Focus Owner and the Key Route (8/8 complete)

**Demo:** a unit test marks `Right` of the pointer tree focusable, adds a `:focus` rule, calls `SetFocus(Right)`, pushes a key down with no target, drains, and runs `Update`. It reads `PseudoStates.Focus` and the focus fill on `Right`. A recorder on the root and `Right` reads root capture, `Right` target capture, `Right` target bubble, and root bubble. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusOwnerTests'` and reads a total of 8 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes the target step of `DrainEvents`, the central method. Add the key and char branch next to the pointer branch only. Do not change the skip, the drop count, or `EventWalk.cs`. `SetFocusOwner` copies the pattern of `SetHover`: a return for the same element, so a repeated set marks nothing.

- [x] ✅ **1.1 Add IsFocusable and AcceptsFocus to Element.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: add `public bool IsFocusable { get; set; }` after `IsPickable`, with the default `false`. The remarks state: a change sets no dirty bit. A click and a Tab read it. A drain drops the focus of an owner that turns unfocusable (phase 5). Add `internal bool AcceptsFocus => IsFocusable;` with a remark: phase 5 adds the `Disabled` rule here, so each focus rule reads one member.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 5 steps

- [x] ✅ **1.2 Add the focus owner and SetFocusOwner to UiContext.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `private Element? _focusOwner;`, `public Element? FocusedElement => _focusOwner;`, and `private void SetFocusOwner(Element? next)`. The setter returns at once for the same element. Else it clears `PseudoStates.Focus` on the old owner with `old.States &= ~PseudoStates.Focus`, stores the new element, and sets `Focus` on it. The remarks of `FocusedElement` state: routing-gate reads it (A6). The owner holds `Focus` from this context. The value changes at a code call or during a drain.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **1.3 Add SetFocus and ClearFocus** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `public bool SetFocus(Element element)` and `public void ClearFocus()`. `SetFocus` throws `ArgumentNullException` for null. It returns `false` and changes nothing when `element.AcceptsFocus` is false or when `IsInRootTree(element)` is false. Else it calls `SetFocusOwner(element)` and returns `true`. `ClearFocus` calls `SetFocusOwner(null)`. The remarks state: the call changes the state at once, also outside a drain and from a handler. The next `Update` restyles the two subtrees.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **1.4 Route a key or char with no target to the owner in DrainEvents** - Completed 2026-10-22
  - Description: Edit `DrainEvents` in `UiContext.cs`: in the target step, when `entry.Header.Target` is null and the type is `KeyDown`, `KeyUp`, or `Char`, set `target = _focusOwner` and write it into `entry.Header.Target`. With no owner, the existing skip runs. A target from the caller keeps its target and changes no focus (Decision 3). Replace the comment "Keyboard-focus sets the target of a key entry here" with the rule.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **1.5 Edit the doc comments of the key target rule** - Completed 2026-10-22
  - Description: Edit the doc comments: the remarks of `PushEvent(in KeyEvent)`, `PushEvent(in CharEvent)`, and `DrainEvents` in `UiContext.cs` (a key or char event with no target goes to the focus owner at the drain. With no owner, it gets no walk). Edit `src/BrutalUi.Core/Input/UiEventHeader.cs`: the remarks of the struct and of `Target` state the same rule. Remove the sentence "The drain skips an event of another type with no target".
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`, `src/BrutalUi.Core/Input/UiEventHeader.cs`
  - Estimate: 5 steps

- [x] ✅ **1.6 Add the focus helpers to PointerTree.cs** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` with additions only: `public static readonly Rgba8 FocusFill = new(64, 160, 96, 255);`, `AddFocusSheet()` (a rule `.hoverable:focus` with `BackgroundColor = FocusFill`), `static KeyEvent Key(UiEventType type, int key, UiModifiers modifiers)` (scan code 0, no repeat, null target), and `static CharEvent Char(uint codePoint)` (null target). Extend the class summary: the shared small scene of the pointer and focus tests.
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`
  - Estimate: 5 steps

- [x] ✅ **1.7 Write FocusOwnerTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/FocusOwnerTests.cs` with 8 tests: `AKeyWithNoTargetWalksToTheFocusOwner` (the demo), `ACharWithNoTargetWalksToTheFocusOwner` (the code point survives), `AKeyWithNoOwnerGetsNoWalk` (`DrainEvents()` returns 0), `AKeyWithACallerTargetKeepsItsTarget` (the owner is `Right`, the target is `Left`, the walk goes to `Left`, and the owner stays), `SetFocusMovesTheFocusState` (`Left` loses `Focus` and `Right` gets it), `ClearFocusClearsTheState`, `SetFocusRejectsAnElementThatIsNotFocusable` (returns `false`, the default of `IsFocusable` is `false`, and the owner stays), and `SetFocusRejectsAnElementOutsideTheRoots` (an orphan and a root of another context).
  - File(s): `tests/BrutalUi.Core.Tests/Input/FocusOwnerTests.cs`
  - Estimate: 8 steps

- [x] ✅ **1.8 Run the gate and record the results** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command, the five event-model classes, and the pointer classes with no edit. Record the test counts before and after, the hash of `EventWalk.cs`, and each decision that the brief did not settle in `docs/features/input-routing/keyboard-focus/context.md`.
  - File(s): `docs/features/input-routing/keyboard-focus/context.md`
  - Estimate: 6 steps

---

## Phase 2: Focus on Click (4/4 complete)

**Demo:** a unit test marks `Right` focusable, adds the `:focus` rule, pushes a pointer down with no target over `Right` outside `Inner`, drains, and runs `Update`. It reads `Focus` and the focus fill on `Right`, and the `PointerDown` handler of `Right` reads `Focus` already. A press over the gap of the root then clears the focus. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ClickFocusTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes the `PointerDown` branch of `RoutePointer`, inside the drain. The focus move runs after `PressButton` and before the walk (Decision 9 of pointer-routing). Do not change the hit test, `Hover`, `Active`, or the button mask.

- [x] ✅ **2.1 Add FocusOnPress and call it in the PointerDown branch** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `private void FocusOnPress(Element? hit)`. It walks up from `hit` through `Parent` to the first element with `IsFocusable` (Notes Q3). When that element `AcceptsFocus`, it calls `SetFocusOwner` with it. Else, and for a null hit, it calls `SetFocusOwner(null)`. Call it in the `PointerDown` branch of `RoutePointer`, after `PressButton`. Each button runs the rule (Decision 4). Replace the comment "Keyboard-focus adds the focus move to the hit element in the PointerDown branch" with the rule.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **2.2 Edit the doc comments of the click focus rule** - Completed 2026-10-22
  - Description: Edit the doc comments: the summary and remarks of `RoutePointer`, the remarks of `DrainEvents` (the click focus rule), `PushEvent(in PointerEvent)`, and `FocusedElement`. A target from the caller skips the click focus, as it skips the states.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] ✅ **2.3 Write ClickFocusTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/ClickFocusTests.cs` with 8 tests: `APressOnAFocusableElementFocusesIt` (the demo), `APressOnAChildFocusesTheNearestFocusableAncestor` (`Inner` is not focusable and `Right` is, so `Right` gets the focus), `APressOnAFocusableChildFocusesTheChild` (`Inner` and `Right` are focusable, so `Inner` gets it), `APressOnNoFocusableElementClearsTheFocus` (the gap of the root), `APressOverNothingClearsTheFocus` (outside each root and at `(NaN, NaN)`), `AMoveAReleaseAndAScrollKeepTheFocus`, `APressWithACallerTargetKeepsTheFocus`, and `ARightButtonPressMovesTheFocus`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/ClickFocusTests.cs`
  - Estimate: 8 steps

- [x] ✅ **2.4 Run the gate and record the results** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command and the pointer classes with no edit (`PointerPressTests` 8, `PointerAllocationTests` 3, and `PointerStateEdgeTests` 8). Record the results and the decisions in `context.md`.
  - File(s): `docs/features/input-routing/keyboard-focus/context.md`
  - Estimate: 6 steps

---

## Phase 3: Tab and Shift+Tab (7/7 complete)

**Demo:** a unit test marks `Left`, `Right`, and `Inner` focusable, pushes four Tab key downs with no target, and drains after each. The owner goes `Left`, `Right`, `Inner`, and `Left` again. A Shift+Tab then gives `Inner`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TabNavigationTests'` and reads a total of 12 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase adds a third walk with an explicit stack and changes the loop of `DrainEvents`. Copy the stack rules of `HitWalk`: the growth by doubling, the copy of the frame values before `Enter` (a growth makes the `ref` stale), and the clear of the used frames in `finally`, also after an early return. `NextChild` counts up, not down. The skip of `DrainEvents` becomes an `if` block, so the step after the walk runs for each entry. Keep the drop count `remaining -= ...` around the walk only. `DispatchQueued` returns the stop flag. `EventWalk.cs` does not change.

**Group A (the walk and the context, 2 files):**

- [x] ✅ **3.1 Write FocusWalk.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Input/FocusWalk.cs`: the internal struct `FocusFrame` (`Element`, `Opacity`, `NextChild`) and `internal static class FocusWalk` with `internal const int InitialStackSize = 32` and `internal static Element? Find(ReadOnlySpan<Element> roots, Element? current, bool backward, ref FocusFrame[] stack)`. Follow the section "FocusWalk.Find". The hidden rule copies steps 1 and 2 of `HitWalk.Enter` (`Display.None`, and an opacity product of 0 or less after the clamp: below 0 counts as 0, above 1 as 1, NaN as 1). The walk reads no clip, no rect, and no `IsPickable`. The class remarks state the order (the paint order), the hidden rule, the wrap, the single-candidate rule, the result for a `current` that the walk does not see, and the `HitWalk` pattern. No method calls itself, and the walk keeps no list of candidates.
  - File(s): `src/BrutalUi.Core/Input/FocusWalk.cs`
  - Estimate: 10 steps

- [x] ✅ **3.2 Add the focus stack, FocusNext, and FocusPrevious** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `private FocusFrame[] _focusStack = new FocusFrame[FocusWalk.InitialStackSize];`, `internal int FocusStackLength`, and `internal ReadOnlySpan<FocusFrame> FocusStackSlots`. Add `public void FocusNext()` and `public void FocusPrevious()`. Each calls `FocusWalk.Find(_roots.AsSpan(), _focusOwner, backward, ref _focusStack)` and then `SetFocusOwner` with the result. The remarks state: the methods are the Tab and Shift+Tab moves and the slot for gamepad navigation (Decision 8). With no element that accepts focus, the owner clears.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **3.3 Add TabKey and the Tab default action after the walk** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `internal const int TabKey = 258;` with a comment (the GLFW value of `GLFW_KEY_TAB`. Core has no GLFW reference. A test of `BrutalUi.Glfw.Tests` checks the value). Change `DispatchQueued<T>` to return `T.HeaderOf(ref e).IsPropagationStopped`. In `DrainEvents`, keep a local `routed` (the target was null at the pop). Change the skip into an `if` block around the walk. After the walk, when `routed` is true, the type is `KeyDown`, `entry.Payload.Key.Key == TabKey`, the modifiers hold no `Control`, `Alt`, or `Super`, and no handler stopped the walk, call `FocusPrevious()` for `Shift` and `FocusNext()` otherwise (Decision 5). A Tab with no owner gets no walk and still moves. A repeat moves. A key up does not move.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 8 steps

- [x] ✅ **3.4 Edit the doc comments of the Tab rule** - Completed 2026-10-22
  - Description: Edit the doc comments: the remarks of `DrainEvents` (the Tab rule, the stop rule, the repeat, the modifiers, and the caller target) and of `PushEvent(in KeyEvent)`. State that a handler that calls `SetFocus` during a Tab walk also stops the walk to keep its choice.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

**Group B (the tests, 3 files):**

- [x] ✅ **3.5 Add the Tab factory to PointerTree.cs** - Completed 2026-10-22
  - Description: Edit `PointerTree.cs` with an addition only: `static KeyEvent Tab(bool shift)` (a `KeyDown` with `UiContext.TabKey`, `UiModifiers.Shift` when `shift` is true, and a null target).
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`
  - Estimate: 3 steps

- [x] ✅ **3.6 Write TabNavigationTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/TabNavigationTests.cs` with 12 tests: `TabMovesTheFocusInTreeOrderAndWraps` (the demo), `ShiftTabMovesBackAndWraps` (from no owner to `Inner`, then `Right`, `Left`, and `Inner`), `TabSkipsAnElementThatIsNotFocusable`, `TabCrossesTheRootsInTheOrderOfAdd` (two roots), `TabSkipsAHiddenSubtree` (`Display.None` and an opacity of 0, written to `StyleStorage` after `Settle`, as P3-4 of pointer-routing), `AHandlerThatStopsTheTabKeepsTheFocus` (`EventRecorder.StopAt` on the old owner, and the recorder shows that the Tab walked to the old owner), `TabWithControlAltOrSuperDoesNotMove`, `ARepeatTabMovesAndAKeyUpDoesNot`, `TabWithOneOrNoFocusableElement` (one: the owner stays. None: the owner stays null), `FocusNextAndFocusPreviousMoveWithNoEvent` (`QueuedEventCount` stays 0), `ATabWithACallerTargetDoesNotMove`, and `ATreeDeeperThanTheStackFindsTheLeaf` (a chain of 40 levels with a focusable leaf, `FocusStackLength` of 41 or more, and each slot of `FocusStackSlots` holds a null element after the call).
  - File(s): `tests/BrutalUi.Core.Tests/Input/TabNavigationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **3.7 Run the gate and record the results** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command and the event-model and pointer classes with no edit. Check that no method of `FocusWalk` calls itself. Record the results, the hash, and the decisions in `context.md`.
  - File(s): `docs/features/input-routing/keyboard-focus/context.md`
  - Estimate: 6 steps

---

## Phase 4: The Adapter Keys and Chars (9/9 complete)

**Demo:** a unit test in `BrutalUi.Glfw.Tests` creates a `GlfwUiInput` with the test seam (no window), focuses `Right` of the pointer tree, calls the internal `PushKey` for the A key (65) and `PushChar` for `'a'` (97), and drains. A handler on `Right` reads a `KeyDown` with the key 65 and a `Char` with the code point 97. A `PushKey` of Tab (258) then moves the focus. The developer runs `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputKeyTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase adds two subscriptions to a third-party event source next to the ImGui `GlfwBackend`. Do task 4.1 before any code. The handler delegates live in fields that the seam constructor creates, so a subscription and an unsubscription use the same instance. Do not subscribe to `OnCharMods`. Do not change `Directory.Packages.props` or a `.csproj` file. When the real API differs from the research facts of this plan, stop and report to the main thread.

- [x] ✅ **4.1 Confirm the key and char API of Ahwoo.BRUTAL.Glfw** - Completed 2026-10-22
  - Description: Confirm the API of `Ahwoo.BRUTAL.Glfw` 2026.10.4 against the facts of this plan: `GlfwKeyCallback(GlfwWindow window, GlfwKey key, int scancode, GlfwKeyAction action, GlfwModifier mods)`, `GlfwCharCallback(GlfwWindow window, uint codepoint)`, `GlfwKeyAction` (`Release` 0, `Press` 1, `Repeat` 2), `GlfwKey.Tab` 258, and `GlfwKey.Unknown`. Read `~/.nuget/packages/ahwoo.brutal.glfw/2026.10.4/lib/net10.0/Brutal.Glfw.xml`, or decompile `Brutal.Glfw.dll` with `ilspycmd` in the session scratch folder. Commit no decompiled file. Add the rows to the API table in `context.md`, with the `(ushort)point` cast of the native char callback.
  - File(s): `docs/features/input-routing/keyboard-focus/context.md`
  - Estimate: 6 steps

- [x] ✅ **4.2 Add the key and char handler fields and subscriptions** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Glfw/GlfwUiInput.cs`: add the fields `_onKey` (`GlfwKeyCallback`) and `_onChar` (`GlfwCharCallback`). The seam constructor creates them. The public constructor subscribes them after the six existing handlers. `Dispose` unsubscribes them.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 5 steps

- [x] ✅ **4.3 Add KeyboardEnabled** - Completed 2026-10-22
  - Description: Add `public bool KeyboardEnabled { get; set; } = true;` with remarks (Notes Q2): with `false`, the adapter pushes no key and no char event. The pointer, drop, and content-scale events still go. Set the value before `Glfw.PollEvents()`, for example `input.KeyboardEnabled = !io.WantCaptureKeyboard;`. A key down before the switch and a key up after it give an element a down with no up.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 3 steps

- [x] ✅ **4.4 Add PushKey and PushChar** - Completed 2026-10-22
  - Description: Add `internal void PushKey(int key, int scanCode, bool down, bool repeat, UiModifiers modifiers, double timestamp)` and `internal void PushChar(uint codePoint, double timestamp)`. Each returns at once after `Dispose` or with `KeyboardEnabled` false. `PushKey` pushes a `KeyDown` for `down` and a `KeyUp` otherwise, with `IsRepeat = down && repeat` and a null target. `PushChar` returns at once when `System.Text.Rune.IsValid(codePoint)` is false (Decision 10). Else it pushes a `CharEvent` with a null target.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 6 steps

- [x] ✅ **4.5 Add HandleKey and HandleChar** - Completed 2026-10-22
  - Description: Add `HandleKey` and `HandleChar`. `HandleKey` calls `PushKey((int)key, scancode, action != GlfwKeyAction.Release, action == GlfwKeyAction.Repeat, ButtonModifiers(mods), GlfwApi.Time)`. The cast uses no table, also for `GlfwKey.Unknown` (Decision 9). `HandleChar` calls `PushChar(codepoint, GlfwApi.Time)`.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 4 steps

- [x] ✅ **4.6 Edit the class remarks of GlfwUiInput** - Completed 2026-10-22
  - Description: Edit the class remarks: the eight events, the ImGui rule (both handlers get each event, and `KeyboardEnabled` gives ImGui the keyboard), and the char limit of the binding (a code point above U+FFFF arrives cut, and the adapter cannot restore it). Update the summary.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 3 steps

- [x] ✅ **4.7 Count the OnKey and OnChar handlers in the real-window test** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`: `ARealWindowSubscribesAndUnsubscribes` also counts the handlers with the adapter as the target on `OnKey` and `OnChar`, before and after `Dispose`. The class keeps 10 tests.
  - File(s): `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.8 Write GlfwUiInputKeyTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputKeyTests.cs` with 8 tests, each through the test seam: `AKeyAndACharReachTheFocusedElement` (the demo), `APressARepeatAndAReleaseGiveDownRepeatAndUp`, `TheKeyCarriesTheScanCodeTheModifiersAndTheTime`, `ATabPressMovesTheFocus` (and Shift+Tab moves back), `TheCoreTabKeyEqualsTheGlfwTabKey` (`(int)GlfwKey.Tab == UiContext.TabKey`), `ACharThatIsNotAScalarValueIsDropped` (`0xD800`, `0xDFFF`, and `0x110000` push nothing. `0x1F600` pushes, so a fix in BRUTAL needs no adapter change), `KeyboardEnabledFalseStopsTheKeyAndCharPushes` (a pointer push still goes), and `DisposeStopsTheKeyAndCharPushes`.
  - File(s): `tests/BrutalUi.Glfw.Tests/GlfwUiInputKeyTests.cs`
  - Estimate: 8 steps

- [x] ✅ **4.9 Run the gate, write the BRUTAL issue text, and record the results** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command. When `xvfb-run` exists, run `xvfb-run -a dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj 2>&1 | tail -n 5` and expect no skip. Run `grep -rn 'using .*Glfw' src/BrutalUi.Core` (no output). Write the issue text for BRUTAL in `context.md`: the native char callback casts the code point to `ushort`, a code point above U+FFFF arrives cut, and the fix passes the value with no cast. A person files it. Record the results and the decisions.
  - File(s): `docs/features/input-routing/keyboard-focus/context.md`
  - Estimate: 6 steps

---

## Phase 5: Disabled and Stale Focus (6/6 complete)

**Demo:** a unit test focuses `Right`, removes it from the root, and drains an empty queue. It reads no `Focus` on `Right` and a null `FocusedElement`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusStateEdgeTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase adds one call at the start of `DrainEvents`, inside the `_draining` guard and before the count read, after `DropStalePointerState`. Change nothing else in the drain. `IsShown` shares the clamp of `FocusWalk.Enter` through one private helper, so the two cannot drift.

- [x] ✅ **5.1 Add the Disabled rule to AcceptsFocus** - Completed 2026-10-22
  - Description: Edit `Element.cs`: `AcceptsFocus` returns `IsFocusable && (States & PseudoStates.Disabled) == 0`. Edit the remarks of `IsFocusable`: a disabled element takes no focus by click, by Tab, or by code.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 3 steps

- [x] ✅ **5.2 Add FocusWalk.IsShown with a shared clamp helper** - Completed 2026-10-22
  - Description: Edit `FocusWalk.cs`: move the opacity clamp of `Enter` into a private static helper. Add `internal static bool IsShown(Element element)`: a parent walk that returns `false` when the element or an ancestor has `Display.None` or a clamped opacity of 0 or less. The loop allocates nothing and does not call itself.
  - File(s): `src/BrutalUi.Core/Input/FocusWalk.cs`
  - Estimate: 5 steps

- [x] ✅ **5.3 Add the hidden rule to SetFocus and DropStaleFocus to the drain** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: `SetFocus` also returns `false` when `FocusWalk.IsShown(element)` is false. Add `private void DropStaleFocus()`: when the owner is not null and `AcceptsFocus` is false, `IsInRootTree` is false, or `FocusWalk.IsShown` is false, call `SetFocusOwner(null)`. Call it in `DrainEvents` right after `DropStalePointerState()`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **5.4 Edit the doc comments of the stale focus rule** - Completed 2026-10-22
  - Description: Edit the doc comments: the remarks of `DrainEvents` (the stale focus rule, and the limit across two contexts, as CR-8 of pointer-routing), `FocusedElement`, `SetFocus`, and `IsFocusable`. The style is the style of the last `Update`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`, `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 4 steps

- [x] ✅ **5.5 Write FocusStateEdgeTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/FocusStateEdgeTests.cs` with 8 tests: `AFocusedElementThatLeavesTheTreeLosesTheFocus` (the demo), `ARemovedRootLosesTheFocus` (`RemoveRoot`), `AnOwnerThatTurnsUnfocusableLosesTheFocus`, `AnOwnerThatGetsDisabledLosesTheFocus`, `AnOwnerUnderAHiddenAncestorLosesTheFocus` (`Display.None` on the parent through `StyleStorage` after `Settle`), `ADisabledElementTakesNoFocus` (`SetFocus` returns `false`, a press on it clears the focus, and a Tab skips it), `SetFocusOnTheOwnerMarksNothing` (after `Settle()`, no `Style` bit in `Dirty`), and `ASetFocusFromAHandlerRoutesTheNextKey` (a `KeyDown` handler on `Left` calls `SetFocus(Right)`, and the next key of the same drain walks to `Right`).
  - File(s): `tests/BrutalUi.Core.Tests/Input/FocusStateEdgeTests.cs`
  - Estimate: 8 steps

- [x] ✅ **5.6 Run the gate and record the results** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command. Record the results and the decisions in `context.md`.
  - File(s): `docs/features/input-routing/keyboard-focus/context.md`
  - Estimate: 5 steps

---

## Phase 6: The Allocation Gates and the Record (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `FocusWalk.Find` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)
**Tier note:** When a gate measures bytes above 0, find the cause before a change. Run the body with one event kind at a time in a scratch copy of the test. Expected suspects: a boxed `PseudoStates`, a delegate created in the body, a growth of the focus stack after the warm-up, or a `Rune` call that boxes. Fix the cause in `src/` with no public API change. Do not add warm-up rounds to hide a growth.

**Group A (the gates, 2 files and 2 temporary edits):**

- [x] ✅ **6.1 Write FocusAllocationTests.cs with the Tab search gate** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/FocusAllocationTests.cs` in the pattern of `PointerAllocationTests`: a private `Scene` from `ThousandElementTree.Build()` with one `Update(0f)`. Set `IsFocusable` on each child of the grid of `GridSlot` and on one element inside the hidden settings panel. Hold the prepared events as `required` fields: Tab, Shift+Tab, a key A down, a key A up, a char `'a'`, a down and an up at the slot point, and a down and an up outside the root. Add `[Fact] TheTabSearchAllocatesNothingOnAWarmTree`: 2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, then 100 rounds of `FocusNext` 3 times, `FocusPrevious`, `ClearFocus`, `FocusPrevious` (the wrap to the last slot), and `ClearFocus`. Assert `Assert.Equal(0L, after - before)`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/FocusAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **6.2 Add the keyboard routing gate** - Completed 2026-10-22
  - Description: Add `[Fact] KeyboardRoutingAllocatesNothingAfterWarmUp`: each step pushes Tab, Tab, Shift+Tab, the key A down and up, the char, the down and up at the slot point, and the down and up outside the root. Then it calls `DrainEvents()` and `Update(0.016f)`. Measure 100 steps after 2 warm-up steps. The body holds no lambda, no boxing, no string, and no `Assert`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/FocusAllocationTests.cs`
  - Estimate: 5 steps

- [x] ✅ **6.3 Add the real-work check** - Completed 2026-10-22
  - Description: Add `[Fact] TheMeasuredBodyDoesRealWork`: the Tab order gives slot 0, slot 1, and slot 0 again. `FocusPrevious` from no owner gives the last grid slot and never the element in the hidden panel. Counter handlers on the slots read the key and char walks of one step. After one full step, no element holds `Focus` and `FocusedElement` is null.
  - File(s): `tests/BrutalUi.Core.Tests/Input/FocusAllocationTests.cs`
  - Estimate: 5 steps

- [x] ✅ **6.4 Extend GlfwUiInputAllocationTests.cs with the key and char pushes** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs`: `Push` adds loops of `PushKey` (a press, a repeat, and a release in turn) and `PushChar`. The drop check becomes `6L * PushCount`. `CallHandlers` adds the delegates of `OnKey` and `OnChar` (read with `ReadEvent`), and the drop check becomes `5L * PushCount`. Update the remarks of the class.
  - File(s): `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **6.5 Prove that the gates block** - Completed 2026-10-22
  - Description: Prove that the gates block. Copy `src/BrutalUi.Core/Input/FocusWalk.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Find`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file with `cp` and check it with `cmp`. Do the same with the first line of `SetFocusOwner` in `UiContext.cs`. Run the tests again and expect 0 failed. Report both exit codes and the names of the failed gates in the hand-back.
  - File(s): `src/BrutalUi.Core/Input/FocusWalk.cs` (temporary), `src/BrutalUi.Core/Tree/UiContext.cs` (temporary)
  - Estimate: 6 steps

**Group B (the record, 3 files):**

- [x] ✅ **6.6 Edit the technical design** - Completed 2026-10-22
  - Description: Edit `docs/technical-design.md`. Section 5, "Shape": add `IsFocusable`. Section 5, "Events": add the keyboard step of the drain (the key and char target, the click focus, the Tab rule with the stop, and the stale focus rule), and change "an entry with a null target after the pointer step" to name the keyboard step too. Section 9, "Flow": item 1 adds the key and char events and `KeyboardEnabled`. Item 7 becomes: focus moves on click, Tab, and Shift+Tab in tree order, with the slot `FocusNext` and `FocusPrevious` for gamepad navigation (Q2 of the epic). Add the ImGui rule. Section 14: add the focus test classes and the eighth gate, with the plant results of task 6.5.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] ✅ **6.7 Edit the changelog** - Completed 2026-10-22
  - Description: Edit `CHANGELOG.md`: add one line under `## [Unreleased]`, `### Added`: the focus owner, `IsFocusable`, focus on click, Tab, and Shift+Tab, the `Focus` state, the key and char routing, and the key and char events of `GlfwUiInput` with `KeyboardEnabled` (input-routing/keyboard-focus).
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] ✅ **6.8 Run the full gate, the lint, and record the results** - Completed 2026-10-22
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the gate results of group A, the plant results, and the final test counts in `context.md`.
  - File(s): `docs/features/input-routing/keyboard-focus/context.md`
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- (none)

**Fixed Bugs:**
- (none)

---

## Technical Debt

- [ ] 💳 **A `HandleKey` that skips `ButtonModifiers` fails a test only with a display** - Priority: Low
  - Description: the real-window test catches it under `xvfb-run -a` and on Windows. A test with no window does not (CR-1 note).
  - Reason: `KeyArgs` returns the action conversion only, not the modifiers.
  - Effort: Low (let `KeyArgs` also return the masked modifiers).

---

## Progress Tracking

### Completed This Session (2026-10-22)
- ✅ Phases 1 to 6 (42 of 42 tasks), each with a verifier PASS and Demo: PASS.
- ✅ The cross-phase review: 6 findings (CR-1 to CR-6), all resolved, verifier PASS.
- ✅ The draft plan of `input-routing/routing-gate` (3 phases, 23 tasks).

### Discovered New Tasks
- (none. The review fixes went into existing tests, so the task list did not grow.)

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
