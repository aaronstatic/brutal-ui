# Keyboard Focus - Implementation Plan

**Epic:** input-routing (feature 3 of 4)
**Status:** Planning
**Started:** 2026-10-22
**Target Completion:** TBD
**Last Updated:** 2026-10-22 12:50

---

## Quality Bar

**Target quality:** A core library feature plus a small extension of the platform adapter. routing-gate reads the focus owner, and the interactive-elements epic builds `Button` and `TextField` on the focus and the key events. A wrong owner, a stuck `Focus` state, a key that goes to a hidden element, or a hidden allocation here breaks each consumer.

- **One owner:** `UiContext` holds one focus owner at most. Only the owner holds `PseudoStates.Focus` from the context. Each change clears the state on the old owner and sets it on the new owner.
- **The order is exact:** Tab and Shift+Tab follow the tree order: the roots in the order of add, a parent before its children, and the children in list order. This is the paint order. The move wraps at each end. A test asserts each rule on known trees.
- **The keys go to the owner:** A key or char event with no target walks to the focus owner through capture, target, and bubble. A target from the caller keeps its target and changes no focus.
- **The walk does not change:** The key and char events walk through the generic walk of event-model. `EventWalk.cs` does not change in this feature.
- **0 bytes after warm-up:** A focus move by click, by Tab, and by code allocates 0 bytes on a warm 1000-element tree. The full keyboard path (push, drain, focus move, walk, and `Update`) allocates 0 bytes. The adapter key and char handlers allocate 0 bytes. A planted allocation fails a gate.
- **No recursion and no list:** The Tab search walks the tree with an explicit stack on `UiContext`, as `HitWalk` does. It collects no list of candidates.
- **No GLFW in Core:** `BrutalUi.Core` gets no package and no `using` of GLFW. Only `BrutalUi.Glfw` converts the GLFW key and char events.

---

## Executive Summary

This feature adds the focus owner, the focus moves, and the keyboard path. `Element` gets `IsFocusable` (default `false`). `UiContext` gets the public `FocusedElement`, `SetFocus`, `ClearFocus`, `FocusNext`, and `FocusPrevious`. Each change moves `PseudoStates.Focus`, so a `:focus` rule restyles the owner on the next `Update`. The drain of `UiContext` gets three changes, all in the seams that pointer-routing left. In the target step, a key or char entry with no target gets the focus owner as its target. In the `PointerDown` branch of `RoutePointer`, a press moves the focus to the hit element or to its nearest focusable ancestor, and a press on no such element clears the focus. After the walk, a Tab key down that no handler stopped moves the focus to the next element that accepts focus, and Shift+Tab moves it to the previous one. The new internal `FocusWalk` finds that element with an explicit stack and no list. At the start of each drain, an owner that left the tree, turned hidden, turned unfocusable, or turned disabled loses the focus. `GlfwUiInput` subscribes to `OnKey` and `OnChar` with the internal `Push*` test seam, and a public `KeyboardEnabled` switch lets the game give the keyboard to ImGui. Unit tests drive the Core path with hand-made events. `UiContext.Update` does not call the drain yet. routing-gate wires it.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (sets `IsFocusable` on the elements that take keys, writes a `:focus` rule, and reads `FocusedElement`. Sets `KeyboardEnabled` from ImGui when a debug panel shows next to the UI)
- [x] **The sibling feature agents** (routing-gate reads `FocusedElement` for `UiRoutingGate.KeyboardFocusInUi`, wires the drain, and pushes a synthetic Tab in HelloPanel)
- [x] **The interactive-elements epic** (`Button` sets `IsFocusable` and reads `Focus`. `TextField` gets the char events at the owner, and it can stop a Tab to keep it)
- [x] **The Phase 3 framework epic** (the action map binds a gamepad button or an arrow key to `FocusNext` and `FocusPrevious`)
- [x] **The maintainer** (runs one more 0-byte gate and an extended adapter gate)
- [ ] **The markup author** (not served. The `:focus` selector exists already)

**User Subsets Served:**
- [x] **Simple** - `button.IsFocusable = true;` and a `.button:focus` rule. A click or a Tab focuses the button.
- [x] **Complex** - `ui.SetFocus(nameField)` when a dialog opens, a `KeyDown` capture handler on a panel, a handler that stops a Tab, and `input.KeyboardEnabled = !io.WantCaptureKeyboard;` before `Glfw.PollEvents()`.

**UX Approach:**
- A null target on a key or char event means "send it to the focus owner". A target from the caller keeps its target. A test or a synthetic event can still aim at one element.
- `SetFocus` returns `false` and changes nothing for an element that cannot take the focus. It throws only for a null element.

---

## Goals

### Primary Goals
1. Hold one focus owner on `UiContext` that code can set, clear, and read.
2. Let an element declare that it accepts focus. The default is no focus.
3. Move the focus on a click, on Tab, and on Shift+Tab, in tree order, with a wrap at each end (Q2).
4. Set `PseudoStates.Focus` on the owner only, so a `:focus` rule restyles it.
5. Send the key and char events of `GlfwWindow` to the owner through the walk of event-model (A1, A2, A5).
6. Leave a slot for gamepad navigation (`FocusNext` and `FocusPrevious`) with no binding.
7. Allocate 0 bytes for a focus move after warm-up. Use no recursion and no list in the Tab search.

### Success Criteria
- [ ] The phase 1 demo test passes: code focuses an element, a key with no target walks to it, and the `:focus` fill shows after `Update`.
- [ ] A press focuses the hit element or its nearest focusable ancestor. A press on no such element clears the focus.
- [ ] Tab and Shift+Tab move the focus in tree order and wrap. A handler that stops the Tab keeps the focus.
- [ ] `GlfwUiInput` pushes the key and char events. `KeyboardEnabled` set to `false` stops them.
- [ ] An owner that leaves the tree, turns hidden, turns unfocusable, or turns disabled loses the focus at the next drain.
- [ ] One new gate and the extended adapter gate measure 0 bytes, and a planted allocation fails a gate.
- [ ] `BrutalUi.Core` has no GLFW reference. `EventWalk.cs` keeps its hash.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Input/FocusWalk.cs                      # phase 3 (the Tab search), phase 5 (IsShown)
├── Input/UiEventHeader.cs                  # phase 1: the remarks of Target (a key or char with no target goes to the owner)
├── Tree/Element.cs                         # phase 1 (IsFocusable, AcceptsFocus), phase 5 (the Disabled rule)
└── Tree/UiContext.cs                       # phases 1, 2, 3, 5: the owner, the key target, the click focus, Tab, the stale rule
src/BrutalUi.Glfw/
└── GlfwUiInput.cs                          # phase 4: OnKey, OnChar, PushKey, PushChar, KeyboardEnabled
tests/BrutalUi.Core.Tests/Input/
├── PointerTree.cs                          # phases 1 and 3: the focus rule and the key factories (additions only)
├── FocusOwnerTests.cs                      # phase 1
├── ClickFocusTests.cs                      # phase 2
├── TabNavigationTests.cs                   # phase 3
├── FocusStateEdgeTests.cs                  # phase 5
└── FocusAllocationTests.cs                 # phase 6
tests/BrutalUi.Glfw.Tests/
├── GlfwUiInputTests.cs                     # phase 4: the real-window test also counts the OnKey and OnChar handlers
├── GlfwUiInputKeyTests.cs                  # phase 4
└── GlfwUiInputAllocationTests.cs           # phase 6: the key and char pushes and handlers
docs/technical-design.md                    # phase 6: sections 5, 9, 14
CHANGELOG.md                                # phase 6
```

`EventWalk.cs`, `HitWalk.cs`, `PaintWalk.cs`, `Directory.Packages.props`, the `.csproj` files, `ThousandElementAllocationTests.cs`, `README.md`, and `scripts/` do not change. `README.md` names "input, focus" for `BrutalUi.Core` already.

### Public API (end state)

Each public member gets an XML doc comment (CS1591 is an error).

```csharp
namespace BrutalUi.Core;

public class Element
{
    public bool IsFocusable { get; set; }          // phase 1: default false. A change sets no dirty bit
    internal bool AcceptsFocus { get; }            // phase 1: IsFocusable. Phase 5: and no PseudoStates.Disabled
}

public sealed class UiContext
{
    public Element? FocusedElement { get; }        // phase 1: the focus owner. routing-gate reads it (A6)
    public bool SetFocus(Element element);         // phase 1. Phase 5 adds the hidden rule
    public void ClearFocus();                      // phase 1
    public void FocusNext();                       // phase 3: the Tab move, and the slot for gamepad navigation
    public void FocusPrevious();                   // phase 3: the Shift+Tab move, and the slot for gamepad navigation

    internal const int TabKey = 258;               // phase 3: the GLFW value of GLFW_KEY_TAB
    internal int FocusStackLength { get; }         // phase 3, for the tests
    internal ReadOnlySpan<FocusFrame> FocusStackSlots { get; }   // phase 3, for the tests
}

namespace BrutalUi.Glfw;

public sealed class GlfwUiInput : IDisposable
{
    public bool KeyboardEnabled { get; set; }      // phase 4: default true (Notes Q2)

    // The GLFW handlers convert, then call these. The tests call them with no window.
    // After Dispose, or with KeyboardEnabled false, each method returns at once.
    internal void PushKey(int key, int scanCode, bool down, bool repeat, UiModifiers modifiers, double timestamp);
    internal void PushChar(uint codePoint, double timestamp);
}
```

`PushEvent` keeps its four public overloads. No new public event type, no new handler slot, and no new delegate type.

### Internal Types

- `FocusFrame`: an internal struct with `Element`, `Opacity` (`float`, the product from the root, after the clamp), and `NextChild` (`int`, counts up from 0). It holds 16 bytes.
- `FocusWalk`: an internal static class (the `HitWalk` pattern). `InitialStackSize = 32`. `Find(ReadOnlySpan<Element> roots, Element? current, bool backward, ref FocusFrame[] stack)` returns the next or the previous element that accepts focus, or null. Phase 5 adds `IsShown(Element element)`, a parent walk with the same hidden rule.

### Data Flow

```
Glfw.PollEvents() ──► GlfwUiInput (BrutalUi.Glfw)
   OnKey  ──► PushKey  ──► PushEvent(KeyEvent KeyDown | KeyUp, Target null)   (Repeat ──► KeyDown, IsRepeat true)
   OnChar ──► PushChar ──► PushEvent(CharEvent, Target null)                  (a value that is not a scalar value drops)
   (KeyboardEnabled false: OnKey and OnChar push nothing. The pointer events still push)
   ▼
UiContext.DrainEvents()   (internal. routing-gate calls it from Update before the style pass, A5)
   0. DropStalePointerState (pointer-routing). DropStaleFocus: an owner that cannot keep the focus loses it   (phase 5)
   for each entry (the count at the start):
     target step:
       Target null, pointer type or Scroll ──► RoutePointer(ref entry)                                  (pointer-routing)
           PointerDown: PressButton, then FocusOnPress(hit): the hit or its nearest focusable ancestor,
                        else no owner                                                                     (phase 2)
       Target null, KeyDown, KeyUp, or Char ──► Target = FocusedElement                                   (phase 1)
     walk: the target is not null and in the roots ──► stopped = DispatchQueued<T>   (EventWalk unchanged)
     default action: the drain gave the target, KeyDown, TabKey, no Control, Alt, or Super, not stopped
         ──► Shift ? FocusPrevious() : FocusNext()                                                         (phase 3)
               └─► FocusWalk.Find(roots, owner, backward, ref _focusStack) ──► SetFocusOwner(result)
   ▼
UiContext.Update: the style pass restyles the subtrees that the state change marked (:focus)

Code: SetFocus, ClearFocus, FocusNext, FocusPrevious ──► SetFocusOwner at once (no queue, also from a handler)
```

### FocusWalk.Find (end state)

```
first = last = before = after = null. seen = false. depth = 0
try:
  for r = 0 .. roots.Length - 1:                      (the roots in the order of add)
    if Enter(roots[r], opacity 1): Visit(roots[r])    (Visit can return at once, see below)
    while depth > 0:
      top = stack[depth - 1]
      if top.NextChild < top.Element.ChildStorage.Count:
        child = children[top.NextChild]. top.NextChild++. opacity = top.Opacity
        (copy before Enter: a growth of the stack makes the ref stale)
        if Enter(child, opacity): Visit(child)
      else: top = default. depth--
  return backward ? (before ?? last) : (after ?? first)
finally: clear the used frames (no element reference stays)

Enter(element, parentOpacity):
  Display.None, or parentOpacity * clamp(own opacity) <= 0: return false (skip the subtree, the HitWalk rule)
  grow the stack by doubling when it is full. Push {element, product, NextChild 0}. return true
Visit(element):                                         (a parent before its children: the paint order)
  if element is current: seen = true. backward and before is not null: return before
  if element.AcceptsFocus:
    first ??= element. last = element                   (current counts here, so a single candidate keeps the focus)
    if element is not current:
      not seen: before = element
      seen and after is null: after = element. not backward: return after
```

The walk visits each element at most one time, so a Tab costs one pass over the visible tree at most. A null `current`, or a `current` that the walk does not see (hidden or outside the roots), gives the first candidate for Tab and the last candidate for Shift+Tab. With no candidate, the result is null and the owner clears.

### The Seams for the Siblings

| Sibling | The seam | What the sibling adds |
|---------|----------|-----------------------|
| routing-gate | `public Element? FocusedElement` | `UiRoutingGate.KeyboardFocusInUi` (true when the value is not null). |
| routing-gate | `internal int DrainEvents()` (event-model) | The call from `Update`. A key entry in the queue case of `ThousandElementAllocationTests`. |
| routing-gate | HelloPanel | `IsFocusable = true` on one element, a `:focus` rule in `HelloScreen.css`, a synthetic Tab (`KeyEvent` with `Key = 258` and a null target), and `input.KeyboardEnabled = !io.WantCaptureKeyboard;` before `Glfw.PollEvents()`. |
| interactive-elements | `IsFocusable`, `PseudoStates.Focus`, `OnKeyDown`, `OnChar` | `Button` and `TextField`. A handler that stops a Tab keeps the focus (Decision 5). |
| Phase 3 framework | `FocusNext`, `FocusPrevious` | The action map binds gamepad and arrow keys. Spatial navigation adds its own method. |

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements leave the demo of phase 1 open, so Notes Q1 holds it, and phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment. Use the file-scoped `namespace BrutalUi.Core;` in Core, `namespace BrutalUi.Glfw;` in the adapter, and `namespace BrutalUi.Core.Tests;` or `namespace BrutalUi.Glfw.Tests;` in the tests. Prose in comments follows ASD-STE100. Each phase checks that the SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` stays `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (Decision R3 of pointer-routing). Each phase runs the five event-model classes (42 tests) and the pointer classes with no edit.

### Phase 1: The Focus Owner and the Key Route
**Goal:** The smallest path that proves the idea: code sets a focus owner, the owner gets `Focus`, and a key or char event with no target walks to the owner.
**Demo:** a unit test marks `Right` of the pointer tree focusable, adds a `:focus` rule, calls `SetFocus(Right)`, pushes a key down with no target, drains, and runs `Update`. It reads `PseudoStates.Focus` and the focus fill on `Right`. A recorder on the root and `Right` reads root capture, `Right` target capture, `Right` target bubble, and root bubble. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusOwnerTests'` and reads a total of 8 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes the target step of `DrainEvents`, the central method. Add the key and char branch next to the pointer branch only. Do not change the skip, the drop count, or `EventWalk.cs`. `SetFocusOwner` copies the pattern of `SetHover`: a return for the same element, so a repeated set marks nothing.

**Tasks:**
- [ ] 1.1 Edit `src/BrutalUi.Core/Tree/Element.cs`: add `public bool IsFocusable { get; set; }` after `IsPickable`, with the default `false`. The remarks state: a change sets no dirty bit. A click and a Tab read it. A drain drops the focus of an owner that turns unfocusable (phase 5). Add `internal bool AcceptsFocus => IsFocusable;` with a remark: phase 5 adds the `Disabled` rule here, so each focus rule reads one member.
- [ ] 1.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `private Element? _focusOwner;`, `public Element? FocusedElement => _focusOwner;`, and `private void SetFocusOwner(Element? next)`. The setter returns at once for the same element. Else it clears `PseudoStates.Focus` on the old owner with `old.States &= ~PseudoStates.Focus`, stores the new element, and sets `Focus` on it. The remarks of `FocusedElement` state: routing-gate reads it (A6). The owner holds `Focus` from this context. The value changes at a code call or during a drain.
- [ ] 1.3 Edit `UiContext.cs`: add `public bool SetFocus(Element element)` and `public void ClearFocus()`. `SetFocus` throws `ArgumentNullException` for null. It returns `false` and changes nothing when `element.AcceptsFocus` is false or when `IsInRootTree(element)` is false. Else it calls `SetFocusOwner(element)` and returns `true`. `ClearFocus` calls `SetFocusOwner(null)`. The remarks state: the call changes the state at once, also outside a drain and from a handler. The next `Update` restyles the two subtrees.
- [ ] 1.4 Edit `DrainEvents` in `UiContext.cs`: in the target step, when `entry.Header.Target` is null and the type is `KeyDown`, `KeyUp`, or `Char`, set `target = _focusOwner` and write it into `entry.Header.Target`. With no owner, the existing skip runs. A target from the caller keeps its target and changes no focus (Decision 3). Replace the comment "Keyboard-focus sets the target of a key entry here" with the rule.
- [ ] 1.5 Edit the doc comments: the remarks of `PushEvent(in KeyEvent)`, `PushEvent(in CharEvent)`, and `DrainEvents` in `UiContext.cs` (a key or char event with no target goes to the focus owner at the drain. With no owner, it gets no walk). Edit `src/BrutalUi.Core/Input/UiEventHeader.cs`: the remarks of the struct and of `Target` state the same rule. Remove the sentence "The drain skips an event of another type with no target".
- [ ] 1.6 Edit `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` with additions only: `public static readonly Rgba8 FocusFill = new(64, 160, 96, 255);`, `AddFocusSheet()` (a rule `.hoverable:focus` with `BackgroundColor = FocusFill`), `static KeyEvent Key(UiEventType type, int key, UiModifiers modifiers)` (scan code 0, no repeat, null target), and `static CharEvent Char(uint codePoint)` (null target). Extend the class summary: the shared small scene of the pointer and focus tests.
- [ ] 1.7 Write `tests/BrutalUi.Core.Tests/Input/FocusOwnerTests.cs` with 8 tests: `AKeyWithNoTargetWalksToTheFocusOwner` (the demo), `ACharWithNoTargetWalksToTheFocusOwner` (the code point survives), `AKeyWithNoOwnerGetsNoWalk` (`DrainEvents()` returns 0), `AKeyWithACallerTargetKeepsItsTarget` (the owner is `Right`, the target is `Left`, the walk goes to `Left`, and the owner stays), `SetFocusMovesTheFocusState` (`Left` loses `Focus` and `Right` gets it), `ClearFocusClearsTheState`, `SetFocusRejectsAnElementThatIsNotFocusable` (returns `false`, the default of `IsFocusable` is `false`, and the owner stays), and `SetFocusRejectsAnElementOutsideTheRoots` (an orphan and a root of another context).
- [ ] 1.8 Run the build and the tests. Run the demo command, the five event-model classes, and the pointer classes with no edit. Record the test counts before and after, the hash of `EventWalk.cs`, and each decision that the brief did not settle in `docs/features/input-routing/keyboard-focus/context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (6):** `Element.cs`, `UiContext.cs`, `UiEventHeader.cs`, `PointerTree.cs`, `FocusOwnerTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `FocusOwnerTests` pass.
- [ ] The 42 event-model tests and each pointer test pass with no edit.
- [ ] `EventWalk.cs` has the hash of Decision R3 of pointer-routing.

---

### Phase 2: Focus on Click
**Goal:** A press moves the focus to the hit element or to its nearest focusable ancestor. A press on no such element clears the focus.
**Demo:** a unit test marks `Right` focusable, adds the `:focus` rule, pushes a pointer down with no target over `Right` outside `Inner`, drains, and runs `Update`. It reads `Focus` and the focus fill on `Right`, and the `PointerDown` handler of `Right` reads `Focus` already. A press over the gap of the root then clears the focus. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ClickFocusTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes the `PointerDown` branch of `RoutePointer`, inside the drain. The focus move runs after `PressButton` and before the walk (Decision 9 of pointer-routing). Do not change the hit test, `Hover`, `Active`, or the button mask.

**Tasks:**
- [ ] 2.1 Edit `UiContext.cs`: add `private void FocusOnPress(Element? hit)`. It walks up from `hit` through `Parent` to the first element with `IsFocusable` (Notes Q3). When that element `AcceptsFocus`, it calls `SetFocusOwner` with it. Else, and for a null hit, it calls `SetFocusOwner(null)`. Call it in the `PointerDown` branch of `RoutePointer`, after `PressButton`. Each button runs the rule (Decision 4). Replace the comment "Keyboard-focus adds the focus move to the hit element in the PointerDown branch" with the rule.
- [ ] 2.2 Edit the doc comments: the summary and remarks of `RoutePointer`, the remarks of `DrainEvents` (the click focus rule), `PushEvent(in PointerEvent)`, and `FocusedElement`. A target from the caller skips the click focus, as it skips the states.
- [ ] 2.3 Write `tests/BrutalUi.Core.Tests/Input/ClickFocusTests.cs` with 8 tests: `APressOnAFocusableElementFocusesIt` (the demo), `APressOnAChildFocusesTheNearestFocusableAncestor` (`Inner` is not focusable and `Right` is, so `Right` gets the focus), `APressOnAFocusableChildFocusesTheChild` (`Inner` and `Right` are focusable, so `Inner` gets it), `APressOnNoFocusableElementClearsTheFocus` (the gap of the root), `APressOverNothingClearsTheFocus` (outside each root and at `(NaN, NaN)`), `AMoveAReleaseAndAScrollKeepTheFocus`, `APressWithACallerTargetKeepsTheFocus`, and `ARightButtonPressMovesTheFocus`.
- [ ] 2.4 Run the build and the tests. Run the demo command and the pointer classes with no edit (`PointerPressTests` 8, `PointerAllocationTests` 3, and `PointerStateEdgeTests` 8). Record the results and the decisions in `context.md`.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (3):** `UiContext.cs`, `ClickFocusTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `ClickFocusTests` pass, and the tests of phase 1 still pass.
- [ ] The pointer tests pass with no edit. No pointer element in them is focusable, so no state changes.
- [ ] `EventWalk.cs` has the hash of the start.

---

### Phase 3: Tab and Shift+Tab
**Goal:** A Tab key down with no target moves the focus to the next element that accepts focus in tree order. Shift+Tab moves it to the previous one. The move wraps. A handler that stops the Tab keeps the focus.
**Demo:** a unit test marks `Left`, `Right`, and `Inner` focusable, pushes four Tab key downs with no target, and drains after each. The owner goes `Left`, `Right`, `Inner`, and `Left` again. A Shift+Tab then gives `Inner`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TabNavigationTests'` and reads a total of 12 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase adds a third walk with an explicit stack and changes the loop of `DrainEvents`. Copy the stack rules of `HitWalk`: the growth by doubling, the copy of the frame values before `Enter` (a growth makes the `ref` stale), and the clear of the used frames in `finally`, also after an early return. `NextChild` counts up, not down. The skip of `DrainEvents` becomes an `if` block, so the step after the walk runs for each entry. Keep the drop count `remaining -= ...` around the walk only. `DispatchQueued` returns the stop flag. `EventWalk.cs` does not change.

**Tasks:**

Group A (the walk and the context, 2 files):
- [ ] 3.1 Write `src/BrutalUi.Core/Input/FocusWalk.cs`: the internal struct `FocusFrame` (`Element`, `Opacity`, `NextChild`) and `internal static class FocusWalk` with `internal const int InitialStackSize = 32` and `internal static Element? Find(ReadOnlySpan<Element> roots, Element? current, bool backward, ref FocusFrame[] stack)`. Follow the section "FocusWalk.Find". The hidden rule copies steps 1 and 2 of `HitWalk.Enter` (`Display.None`, and an opacity product of 0 or less after the clamp: below 0 counts as 0, above 1 as 1, NaN as 1). The walk reads no clip, no rect, and no `IsPickable`. The class remarks state the order (the paint order), the hidden rule, the wrap, the single-candidate rule, the result for a `current` that the walk does not see, and the `HitWalk` pattern. No method calls itself, and the walk keeps no list of candidates.
- [ ] 3.2 Edit `UiContext.cs`: add `private FocusFrame[] _focusStack = new FocusFrame[FocusWalk.InitialStackSize];`, `internal int FocusStackLength`, and `internal ReadOnlySpan<FocusFrame> FocusStackSlots`. Add `public void FocusNext()` and `public void FocusPrevious()`. Each calls `FocusWalk.Find(_roots.AsSpan(), _focusOwner, backward, ref _focusStack)` and then `SetFocusOwner` with the result. The remarks state: the methods are the Tab and Shift+Tab moves and the slot for gamepad navigation (Decision 8). With no element that accepts focus, the owner clears.
- [ ] 3.3 Edit `UiContext.cs`: add `internal const int TabKey = 258;` with a comment (the GLFW value of `GLFW_KEY_TAB`. Core has no GLFW reference. A test of `BrutalUi.Glfw.Tests` checks the value). Change `DispatchQueued<T>` to return `T.HeaderOf(ref e).IsPropagationStopped`. In `DrainEvents`, keep a local `routed` (the target was null at the pop). Change the skip into an `if` block around the walk. After the walk, when `routed` is true, the type is `KeyDown`, `entry.Payload.Key.Key == TabKey`, the modifiers hold no `Control`, `Alt`, or `Super`, and no handler stopped the walk, call `FocusPrevious()` for `Shift` and `FocusNext()` otherwise (Decision 5). A Tab with no owner gets no walk and still moves. A repeat moves. A key up does not move.
- [ ] 3.4 Edit the doc comments: the remarks of `DrainEvents` (the Tab rule, the stop rule, the repeat, the modifiers, and the caller target) and of `PushEvent(in KeyEvent)`. State that a handler that calls `SetFocus` during a Tab walk also stops the walk to keep its choice.

Group B (the tests, 3 files):
- [ ] 3.5 Edit `PointerTree.cs` with an addition only: `static KeyEvent Tab(bool shift)` (a `KeyDown` with `UiContext.TabKey`, `UiModifiers.Shift` when `shift` is true, and a null target).
- [ ] 3.6 Write `tests/BrutalUi.Core.Tests/Input/TabNavigationTests.cs` with 12 tests: `TabMovesTheFocusInTreeOrderAndWraps` (the demo), `ShiftTabMovesBackAndWraps` (from no owner to `Inner`, then `Right`, `Left`, and `Inner`), `TabSkipsAnElementThatIsNotFocusable`, `TabCrossesTheRootsInTheOrderOfAdd` (two roots), `TabSkipsAHiddenSubtree` (`Display.None` and an opacity of 0, written to `StyleStorage` after `Settle`, as P3-4 of pointer-routing), `AHandlerThatStopsTheTabKeepsTheFocus` (`EventRecorder.StopAt` on the old owner, and the recorder shows that the Tab walked to the old owner), `TabWithControlAltOrSuperDoesNotMove`, `ARepeatTabMovesAndAKeyUpDoesNot`, `TabWithOneOrNoFocusableElement` (one: the owner stays. None: the owner stays null), `FocusNextAndFocusPreviousMoveWithNoEvent` (`QueuedEventCount` stays 0), `ATabWithACallerTargetDoesNotMove`, and `ATreeDeeperThanTheStackFindsTheLeaf` (a chain of 40 levels with a focusable leaf, `FocusStackLength` of 41 or more, and each slot of `FocusStackSlots` holds a null element after the call).
- [ ] 3.7 Run the build and the tests. Run the demo command and the event-model and pointer classes with no edit. Check that no method of `FocusWalk` calls itself. Record the results, the hash, and the decisions in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (5):** Group A: `FocusWalk.cs`, `UiContext.cs`. Group B: `PointerTree.cs`, `TabNavigationTests.cs`, `context.md`.

**Acceptance Criteria:**
- [ ] The 12 tests of `TabNavigationTests` pass, and the tests of phases 1 and 2 still pass.
- [ ] No method of `FocusWalk` calls itself. `FocusWalk.cs` holds no `List<`, no `new Element[`, and no LINQ.
- [ ] `EventWalk.cs` has the hash of the start. `PushEvent` keeps four public overloads.

---

### Phase 4: The Adapter Keys and Chars
**Goal:** `GlfwUiInput` converts the key and char events of `GlfwWindow` and pushes them with no target. `KeyboardEnabled` lets the game give the keyboard to ImGui.
**Demo:** a unit test in `BrutalUi.Glfw.Tests` creates a `GlfwUiInput` with the test seam (no window), focuses `Right` of the pointer tree, calls the internal `PushKey` for the A key (65) and `PushChar` for `'a'` (97), and drains. A handler on `Right` reads a `KeyDown` with the key 65 and a `Char` with the code point 97. A `PushKey` of Tab (258) then moves the focus. The developer runs `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputKeyTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase adds two subscriptions to a third-party event source next to the ImGui `GlfwBackend`. Do task 4.1 before any code. The handler delegates live in fields that the seam constructor creates, so a subscription and an unsubscription use the same instance. Do not subscribe to `OnCharMods`. Do not change `Directory.Packages.props` or a `.csproj` file. When the real API differs from the research facts of this plan, stop and report to the main thread.

**Tasks:**
- [ ] 4.1 Confirm the API of `Ahwoo.BRUTAL.Glfw` 2026.10.4 against the facts of this plan: `GlfwKeyCallback(GlfwWindow window, GlfwKey key, int scancode, GlfwKeyAction action, GlfwModifier mods)`, `GlfwCharCallback(GlfwWindow window, uint codepoint)`, `GlfwKeyAction` (`Release` 0, `Press` 1, `Repeat` 2), `GlfwKey.Tab` 258, and `GlfwKey.Unknown`. Read `~/.nuget/packages/ahwoo.brutal.glfw/2026.10.4/lib/net10.0/Brutal.Glfw.xml`, or decompile `Brutal.Glfw.dll` with `ilspycmd` in the session scratch folder. Commit no decompiled file. Add the rows to the API table in `context.md`, with the `(ushort)point` cast of the native char callback.
- [ ] 4.2 Edit `src/BrutalUi.Glfw/GlfwUiInput.cs`: add the fields `_onKey` (`GlfwKeyCallback`) and `_onChar` (`GlfwCharCallback`). The seam constructor creates them. The public constructor subscribes them after the six existing handlers. `Dispose` unsubscribes them.
- [ ] 4.3 Add `public bool KeyboardEnabled { get; set; } = true;` with remarks (Notes Q2): with `false`, the adapter pushes no key and no char event. The pointer, drop, and content-scale events still go. Set the value before `Glfw.PollEvents()`, for example `input.KeyboardEnabled = !io.WantCaptureKeyboard;`. A key down before the switch and a key up after it give an element a down with no up.
- [ ] 4.4 Add `internal void PushKey(int key, int scanCode, bool down, bool repeat, UiModifiers modifiers, double timestamp)` and `internal void PushChar(uint codePoint, double timestamp)`. Each returns at once after `Dispose` or with `KeyboardEnabled` false. `PushKey` pushes a `KeyDown` for `down` and a `KeyUp` otherwise, with `IsRepeat = down && repeat` and a null target. `PushChar` returns at once when `System.Text.Rune.IsValid(codePoint)` is false (Decision 10). Else it pushes a `CharEvent` with a null target.
- [ ] 4.5 Add `HandleKey` and `HandleChar`. `HandleKey` calls `PushKey((int)key, scancode, action != GlfwKeyAction.Release, action == GlfwKeyAction.Repeat, ButtonModifiers(mods), GlfwApi.Time)`. The cast uses no table, also for `GlfwKey.Unknown` (Decision 9). `HandleChar` calls `PushChar(codepoint, GlfwApi.Time)`.
- [ ] 4.6 Edit the class remarks: the eight events, the ImGui rule (both handlers get each event, and `KeyboardEnabled` gives ImGui the keyboard), and the char limit of the binding (a code point above U+FFFF arrives cut, and the adapter cannot restore it). Update the summary.
- [ ] 4.7 Edit `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`: `ARealWindowSubscribesAndUnsubscribes` also counts the handlers with the adapter as the target on `OnKey` and `OnChar`, before and after `Dispose`. The class keeps 10 tests.
- [ ] 4.8 Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputKeyTests.cs` with 8 tests, each through the test seam: `AKeyAndACharReachTheFocusedElement` (the demo), `APressARepeatAndAReleaseGiveDownRepeatAndUp`, `TheKeyCarriesTheScanCodeTheModifiersAndTheTime`, `ATabPressMovesTheFocus` (and Shift+Tab moves back), `TheCoreTabKeyEqualsTheGlfwTabKey` (`(int)GlfwKey.Tab == UiContext.TabKey`), `ACharThatIsNotAScalarValueIsDropped` (`0xD800`, `0xDFFF`, and `0x110000` push nothing. `0x1F600` pushes, so a fix in BRUTAL needs no adapter change), `KeyboardEnabledFalseStopsTheKeyAndCharPushes` (a pointer push still goes), and `DisposeStopsTheKeyAndCharPushes`.
- [ ] 4.9 Run the build and the tests. Run the demo command. When `xvfb-run` exists, run `xvfb-run -a dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj 2>&1 | tail -n 5` and expect no skip. Run `grep -rn 'using .*Glfw' src/BrutalUi.Core` (no output). Write the issue text for BRUTAL in `context.md`: the native char callback casts the code point to `ushort`, a code point above U+FFFF arrives cut, and the fix passes the value with no cast. A person files it. Record the results and the decisions.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (4):** `GlfwUiInput.cs`, `GlfwUiInputTests.cs`, `GlfwUiInputKeyTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `GlfwUiInputKeyTests` pass. `GlfwUiInputTests` keeps 10 tests and passes, with one skip allowed with no display.
- [ ] `BrutalUi.Core` has no `using` of a GLFW namespace. No `.csproj` and no `Directory.Packages.props` change.
- [ ] `context.md` holds the API rows of task 4.1 and the issue text for BRUTAL.

---

### Phase 5: Disabled and Stale Focus
**Goal:** No element that cannot take the focus keeps it. A disabled element takes no focus. An owner that leaves the tree, turns hidden, turns unfocusable, or turns disabled loses the focus at the next drain.
**Demo:** a unit test focuses `Right`, removes it from the root, and drains an empty queue. It reads no `Focus` on `Right` and a null `FocusedElement`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusStateEdgeTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase adds one call at the start of `DrainEvents`, inside the `_draining` guard and before the count read, after `DropStalePointerState`. Change nothing else in the drain. `IsShown` shares the clamp of `FocusWalk.Enter` through one private helper, so the two cannot drift.

**Tasks:**
- [ ] 5.1 Edit `Element.cs`: `AcceptsFocus` returns `IsFocusable && (States & PseudoStates.Disabled) == 0`. Edit the remarks of `IsFocusable`: a disabled element takes no focus by click, by Tab, or by code.
- [ ] 5.2 Edit `FocusWalk.cs`: move the opacity clamp of `Enter` into a private static helper. Add `internal static bool IsShown(Element element)`: a parent walk that returns `false` when the element or an ancestor has `Display.None` or a clamped opacity of 0 or less. The loop allocates nothing and does not call itself.
- [ ] 5.3 Edit `UiContext.cs`: `SetFocus` also returns `false` when `FocusWalk.IsShown(element)` is false. Add `private void DropStaleFocus()`: when the owner is not null and `AcceptsFocus` is false, `IsInRootTree` is false, or `FocusWalk.IsShown` is false, call `SetFocusOwner(null)`. Call it in `DrainEvents` right after `DropStalePointerState()`.
- [ ] 5.4 Edit the doc comments: the remarks of `DrainEvents` (the stale focus rule, and the limit across two contexts, as CR-8 of pointer-routing), `FocusedElement`, `SetFocus`, and `IsFocusable`. The style is the style of the last `Update`.
- [ ] 5.5 Write `tests/BrutalUi.Core.Tests/Input/FocusStateEdgeTests.cs` with 8 tests: `AFocusedElementThatLeavesTheTreeLosesTheFocus` (the demo), `ARemovedRootLosesTheFocus` (`RemoveRoot`), `AnOwnerThatTurnsUnfocusableLosesTheFocus`, `AnOwnerThatGetsDisabledLosesTheFocus`, `AnOwnerUnderAHiddenAncestorLosesTheFocus` (`Display.None` on the parent through `StyleStorage` after `Settle`), `ADisabledElementTakesNoFocus` (`SetFocus` returns `false`, a press on it clears the focus, and a Tab skips it), `SetFocusOnTheOwnerMarksNothing` (after `Settle()`, no `Style` bit in `Dirty`), and `ASetFocusFromAHandlerRoutesTheNextKey` (a `KeyDown` handler on `Left` calls `SetFocus(Right)`, and the next key of the same drain walks to `Right`).
- [ ] 5.6 Run the build and the tests. Run the demo command. Record the results and the decisions in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (5):** `Element.cs`, `FocusWalk.cs`, `UiContext.cs`, `FocusStateEdgeTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `FocusStateEdgeTests` pass, and the tests of phases 1 to 4 still pass.
- [ ] After a drain, `FocusedElement` is null or an element that accepts focus, is shown, and is in the roots of the context.
- [ ] `EventWalk.cs` has the hash of the start.

---

### Phase 6: The Allocation Gates and the Record
**Goal:** Prove 0 bytes for a focus move on a warm 1000-element tree, for the full keyboard path, and for the adapter key and char handlers. Record the feature in the technical design and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FocusAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `FocusWalk.Find` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

**Tier note:** When a gate measures bytes above 0, find the cause before a change. Run the body with one event kind at a time in a scratch copy of the test. Expected suspects: a boxed `PseudoStates`, a delegate created in the body, a growth of the focus stack after the warm-up, or a `Rune` call that boxes. Fix the cause in `src/` with no public API change. Do not add warm-up rounds to hide a growth.

**Tasks:**

Group A (the gates, 2 files and 2 temporary edits. Task 6.8 records the results):
- [ ] 6.1 Write `tests/BrutalUi.Core.Tests/Input/FocusAllocationTests.cs` in the pattern of `PointerAllocationTests`: a private `Scene` from `ThousandElementTree.Build()` with one `Update(0f)`. Set `IsFocusable` on each child of the grid of `GridSlot` and on one element inside the hidden settings panel. Hold the prepared events as `required` fields: Tab, Shift+Tab, a key A down, a key A up, a char `'a'`, a down and an up at the slot point, and a down and an up outside the root. Add `[Fact] TheTabSearchAllocatesNothingOnAWarmTree`: 2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, then 100 rounds of `FocusNext` 3 times, `FocusPrevious`, `ClearFocus`, `FocusPrevious` (the wrap to the last slot), and `ClearFocus`. Assert `Assert.Equal(0L, after - before)`.
- [ ] 6.2 Add `[Fact] KeyboardRoutingAllocatesNothingAfterWarmUp`: each step pushes Tab, Tab, Shift+Tab, the key A down and up, the char, the down and up at the slot point, and the down and up outside the root. Then it calls `DrainEvents()` and `Update(0.016f)`. Measure 100 steps after 2 warm-up steps. The body holds no lambda, no boxing, no string, and no `Assert`.
- [ ] 6.3 Add `[Fact] TheMeasuredBodyDoesRealWork`: the Tab order gives slot 0, slot 1, and slot 0 again. `FocusPrevious` from no owner gives the last grid slot and never the element in the hidden panel. Counter handlers on the slots read the key and char walks of one step. After one full step, no element holds `Focus` and `FocusedElement` is null.
- [ ] 6.4 Edit `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs`: `Push` adds loops of `PushKey` (a press, a repeat, and a release in turn) and `PushChar`. The drop check becomes `6L * PushCount`. `CallHandlers` adds the delegates of `OnKey` and `OnChar` (read with `ReadEvent`), and the drop check becomes `5L * PushCount`. Update the remarks of the class.
- [ ] 6.5 Prove that the gates block. Copy `src/BrutalUi.Core/Input/FocusWalk.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Find`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file with `cp` and check it with `cmp`. Do the same with the first line of `SetFocusOwner` in `UiContext.cs`. Run the tests again and expect 0 failed. Report both exit codes and the names of the failed gates in the hand-back.

Group B (the record, 3 files):
- [ ] 6.6 Edit `docs/technical-design.md`. Section 5, "Shape": add `IsFocusable`. Section 5, "Events": add the keyboard step of the drain (the key and char target, the click focus, the Tab rule with the stop, and the stale focus rule), and change "an entry with a null target after the pointer step" to name the keyboard step too. Section 9, "Flow": item 1 adds the key and char events and `KeyboardEnabled`. Item 7 becomes: focus moves on click, Tab, and Shift+Tab in tree order, with the slot `FocusNext` and `FocusPrevious` for gamepad navigation (Q2 of the epic). Add the ImGui rule. Section 14: add the focus test classes and the eighth gate, with the plant results of task 6.5.
- [ ] 6.7 Edit `CHANGELOG.md`: add one line under `## [Unreleased]`, `### Added`: the focus owner, `IsFocusable`, focus on click, Tab, and Shift+Tab, the `Focus` state, the key and char routing, and the key and char events of `GlfwUiInput` with `KeyboardEnabled` (input-routing/keyboard-focus).
- [ ] 6.8 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the gate results of group A, the plant results, and the final test counts in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (7):** Group A: `FocusAllocationTests.cs`, `GlfwUiInputAllocationTests.cs`, and temporary edits of `FocusWalk.cs` and `UiContext.cs` (restored). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `context.md`.

**Acceptance Criteria:**
- [ ] The 3 tests of `FocusAllocationTests` pass with 0 bytes in 5 runs of the class. `GlfwUiInputAllocationTests` passes, with one skip allowed with no display.
- [ ] Each plant gives a non-zero exit code. The restored files give 0 failed.
- [ ] Sections 5, 9, and 14 of the technical design match the code. Section 9 no longer names gamepad navigation as a focus move of this epic.

---

**Total Estimated Steps:** about 252 (42 tasks over 6 phases)

---

## Key Technical Decisions

### Confirmed Assumptions

No feature-level interview ran: `/autorun-epic` drafts this plan in its called mode, and no person can answer. The items below are the epic-level items that the user confirmed:
- Q1 The GLFW adapter lives in a new `BrutalUi.Glfw` package. Core stays free of GLFW.
- Q2 Focus moves on click, Tab, and Shift+Tab. Gamepad waits for the action map.
- Q3 Build order: event-model, pointer-routing, keyboard-focus, routing-gate.
- A1 `BrutalUi.Core` gets no GLFW reference. The input types in Core accept plain structs, and `BrutalUi.Glfw` converts the `GlfwWindow` events.
- A2 A handler registers as a delegate per event kind on `Element` at build time, not in the frame loop. The walk calls the delegates with a `ref` event struct.
- A5 The adapter queues events in a fixed ring buffer. The input pass drains it in `UiContext.Update` before the style pass.
- A6 The gate reads the hit result of the last pointer position and the focus owner. The game polls it after `Update`.

### Decision 1: One focus owner on UiContext, with a public read and set
**Choice:** One field, `_focusOwner`. The public `FocusedElement` reads it. `SetFocus(Element)` sets it and returns `false` for an element that cannot take the focus. `ClearFocus()` clears it. Each change goes through `SetFocusOwner`, which copies `SetHover`: it clears `Focus` on the old owner and sets it on the new owner. `Focus` goes on the owner only.
**Rationale:** The requirement asks that code can set, clear, and read the owner, so the members are public. routing-gate reads the same member (A6). The `SetHover` pattern is known and tested, and a set of the same element marks nothing. One owner gives one `Focus` bit, as `Hover` has one owner.
**Alternatives:** (a) An internal owner with public methods only: a game cannot read the owner. (b) A `Focus` bit on each ancestor too (`:focus-within`): the requirement names the owner only. (c) `SetFocus` throws for a bad element: a game that focuses a closed panel then needs a try block. The `bool` result tells the same.

### Decision 2: IsFocusable on Element, and one accept rule
**Choice:** `Element.IsFocusable` (default `false`) declares the intent. A change sets no dirty bit, as `IsPickable`. The internal `AcceptsFocus` holds the full rule: `IsFocusable` and no `PseudoStates.Disabled` (phase 5). The click, the Tab search, `SetFocus`, and the stale rule read `AcceptsFocus` only.
**Rationale:** The requirement sets the default to no focus. The name follows `IsPickable`. One rule in one member keeps the four readers equal. `Disabled` is "the element does not accept input" in `PseudoStates`, so a disabled button takes no focus. The interactive-elements epic then needs no change of the focus rule.
**Alternatives:** (a) A `TabIndex` number as in HTML: no requirement asks for an order other than the tree order (YAGNI). (b) No `Disabled` rule: each widget of interactive-elements then repeats the check.

### Decision 3: A key or char event with no target goes to the owner
**Choice:** In the target step of the drain, a `KeyDown`, `KeyUp`, or `Char` entry with a null target gets the focus owner as its target. With no owner, the entry gets no walk. A target from the caller keeps its target and changes no focus.
**Rationale:** The event-model seam names this step, and pointer-routing uses the same step for the hit test. The rule mirrors Decision 2 of pointer-routing: a null target means "the drain routes it". A test or a synthetic event can still aim at one element, and the event-model tests keep their meaning. A key with no owner belongs to the game, so the UI walks nothing.
**Alternatives:** (a) The owner at the push: the push then reads a state that a handler of the same drain can change. (b) A key with no owner walks to the first root: then a root handler takes the keys of the game.

### Decision 4: The click focus runs in the PointerDown branch, before the walk
**Choice:** `RoutePointer` calls `FocusOnPress(hit)` after `PressButton`. The method walks up from the hit to the first element with `IsFocusable` (Notes Q3). When that element accepts focus, it gets the focus. Else, and for a press over nothing, the focus clears. Each button runs the rule. A target from the caller skips it.
**Rationale:** The requirement: a click on an element that accepts focus moves the focus to it, and a click on no such element clears it. A press over the game world clears the focus, so the game gets the keyboard back. The state changes before the walk, as Decision 9 of pointer-routing, so the `PointerDown` handler reads the new state. The ancestor rule lets a click on the label of a button focus the button. The walk up costs one parent walk with no allocation.
**Alternatives:** (a) The hit element only (Notes Q3, option B). (b) The left button only: a right click on a text field then keeps another owner. No requirement names a button.

### Decision 5: Tab is a default action after the walk, and a stop keeps the focus
**Choice:** A Tab `KeyDown` with no target walks to the old owner first. After the walk, the drain moves the focus when no handler stopped the walk. Shift moves back. A Tab with `Control`, `Alt`, or `Super` does not move. A repeat moves. A key up does not move. Core matches the key with `UiContext.TabKey = 258`, the GLFW value. `DispatchQueued` returns the stop flag of the walk. `EventWalk.cs` does not change.
**Rationale:** This is the default action of a browser: the key goes to the focused element, and the move follows unless a handler prevents it (Notes Q4). A `TextField` with tab stops can then keep the Tab. The stop flag exists in `UiEventHeader`, so no new member is necessary. `Ctrl+Tab` is a common tab switch of a game panel, so it does not move the focus. `KeyEvent.Key` holds the GLFW value (event-model), and Core has no GLFW reference, so an internal constant holds 258. A test of `BrutalUi.Glfw.Tests` checks it against `GlfwKey.Tab`.
**Alternatives:** (a) The move before the walk, and the Tab walks to the new owner (Notes Q4, option B): no element can keep the Tab. (b) A new "default prevented" flag in the header: a new public member for the same job as the stop.

### Decision 6: FocusWalk uses an explicit stack, one pass, and no list
**Choice:** An internal static class with an explicit stack of `FocusFrame` values on `UiContext`, in the `HitWalk` pattern: a start of 32 frames, a growth by doubling, no shrink, and a clear of the used frames in `finally`. One pre-order pass over the roots keeps four references (the first, the last, the last before the owner, and the first after the owner) and returns early when it can. It skips a hidden subtree with the hidden rule of `HitWalk`. It reads no clip, no rect, and no `IsPickable`.
**Rationale:** The requirement forbids recursion and a list. The walk collects no candidates. The stack is the same kind of frame buffer as the stacks of `HitWalk` and `PaintWalk`. The project knows and tests this pattern. One pass handles the wrap, a missing owner, a hidden owner, and one candidate with no special path. The pre-order is the paint order, so the Tab order is the order that the user sees. A hidden element is not on the screen, so it takes no focus. A clipped element can take the focus, as in a browser, so a later `ScrollView` can scroll to it.
**Alternatives:** (a) A parent and sibling walk with no stack: each step up needs `IndexOf` in the child list of the parent, each candidate needs a parent walk for the opacity product, and the reverse order needs a "last descendant" walk. It is harder to get right. (b) A list of focusable elements that the context keeps: against the requirement, and it needs a hook in each tree change. (c) A reuse of `HitFrame` and `_hitStack`: two walks with two meanings of `NextChild` on one type.

### Decision 7: The stale focus rule at the start of the drain
**Choice:** At the start of each drain, after the stale pointer rule, the context clears the focus when the owner does not accept focus, is not in its roots, or is hidden (`Display.None` or an opacity of 0 or less on the owner or an ancestor). `SetFocus` checks the same rule.
**Rationale:** A closed dialog must not keep the keys, and `KeyboardFocusInUi` must not stay true for a hidden panel. The cost is one parent walk per drain with no allocation. The rule follows Decision 10 of pointer-routing. routing-gate drains in each `Update`, so the rule runs in each frame. The style of the last `Update` decides "hidden", as the hit test does.
**Alternatives:** (a) A hook in `RemoveRoot`, the child list, and the style pass: more code paths for the same rule. (b) The tree rule only, as for `Hover`: a hidden dialog then keeps the keys until a click.

### Decision 8: The gamepad slot is FocusNext and FocusPrevious
**Choice:** The public `FocusNext()` and `FocusPrevious()` run the Tab search at once. The Tab default action calls them. This feature adds no gamepad and no arrow-key binding.
**Rationale:** The requirement asks for a slot and no binding (Q2). The Phase 3 action map binds a gamepad button to these methods with no change of Core. A game can also move the focus from its own code. Spatial navigation needs the rects and adds its own method later.
**Alternatives:** (a) An internal method only: the action map lives in the framework package and cannot call it. (b) A direction enum with up, down, left, and right now: no requirement asks for the spatial moves (YAGNI).

### Decision 9: The adapter keys and chars use the seam pattern of pointer-routing
**Choice:** `GlfwUiInput` subscribes to `OnKey` and `OnChar` with delegates in fields, created one time. `HandleKey` casts the key to `int` with no table, also for `GlfwKey.Unknown`. `Release` gives `KeyUp`, `Press` gives `KeyDown`, and `Repeat` gives `KeyDown` with `IsRepeat`. The modifiers use `ButtonModifiers` (the mask `0x3F`). The timestamp is `Glfw.Time`. `HandleChar` passes the code point. The internal `PushKey` and `PushChar` hold the logic, so the tests need no window. The adapter does not subscribe to `OnCharMods`.
**Rationale:** Decisions 12 and 14 of pointer-routing. `KeyEvent` documents a repeat as a `KeyDown` with `IsRepeat`. The GLFW key callback gives the modifiers, so no `GetKey` call runs. `CharEvent` has no modifiers, so `OnCharMods` adds nothing. An unknown key still carries its scan code.
**Alternatives:** (a) A key table to a brutal-ui key enum: event-model chose the GLFW values. (b) A filter of `GlfwKey.Unknown`: a handler loses a key that only the scan code names.

### Decision 10: A char outside the Basic Multilingual Plane
**Choice:** The adapter pushes the code point that it gets. It drops a value that is not a Unicode scalar value (`Rune.IsValid` is false: a surrogate or a value above U+10FFFF). The plan accepts the Basic Multilingual Plane only, until BRUTAL fixes its char callback. Task 4.9 writes the issue text for BRUTAL.
**Rationale:** The native char callback of `GlfwWindow` casts the code point to `ushort`, so the adapter cannot restore a code point above U+FFFF. A cut value can land in the surrogate range, and `CharEvent.CodePoint` is a scalar value, so the adapter drops that case. Any other cut value looks like a valid character, and no filter can find it. The adapter takes `uint` already, so a fix in BRUTAL needs no change here. No text field exists before the interactive-elements epic.
**Alternatives:** (a) A native char callback of our own: it replaces the callback of `GlfwWindow` and breaks the ImGui `GlfwBackend`. (b) A filter of the private use area: it drops icon-font characters and still misses other cut values.

### Decision 11: ImGui gets the keyboard through KeyboardEnabled
**Choice:** Both the ImGui `GlfwBackend` and `GlfwUiInput` get each key and char event. The binding has no consume rule. The public `GlfwUiInput.KeyboardEnabled` (default `true`) turns the key and char pushes off. The game sets it from `io.WantCaptureKeyboard` before `Glfw.PollEvents()`. The pointer events keep no switch (Notes Q2 and Conflicts 8).
**Rationale:** The mission keeps ImGui for debug panels next to the UI. With no rule, a Tab in an ImGui text field also moves the UI focus, and an Enter can press a focused UI button. The research and the pointer-routing hand-off give this rule to keyboard-focus. The switch costs one property and two checks, with no allocation. `BrutalUi.Glfw` stays free of ImGui.
**Alternatives:** See Notes Q2.

### Decision 12: A code call changes the focus at once
**Choice:** `SetFocus`, `ClearFocus`, `FocusNext`, and `FocusPrevious` change the owner and the state at once, with no queue. A handler can call them during a drain.
**Rationale:** A5 covers the events of the adapter. A code call is not an input event. The call marks two subtrees, and the next `Update` restyles them. A handler that focuses another element changes the target of the next key in the same drain, which is the expected order.
**Alternatives:** Queue a focus request for the next drain: a game that focuses a field and reads `FocusedElement` then reads the old value.

### Decision 13: The tests reuse PointerTree
**Choice:** `PointerTree` gets `FocusFill`, `AddFocusSheet`, `Key`, `Char`, and `Tab`, as additions only. Its tree order is root, `Left`, `Right`, `Inner`.
**Rationale:** The tree has the depth and the order that the click and Tab tests need. `BrutalUi.Glfw.Tests` links the file already, so the adapter tests need no project change.
**Alternatives:** A new `FocusTree`: a second scene and a new link in the Glfw test project.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The change of the skip in `DrainEvents` breaks the drop count or an event-model rule. | Medium | High | Tier note of phase 3. Each phase runs the 42 event-model tests and the pointer tests with no edit. `EventWalk.cs` keeps its hash. |
| A Tab in an ImGui text field also moves the UI focus. | Medium | Medium | Decision 11 and `KeyboardEnabled`. routing-gate shows the line in HelloPanel. |
| A hidden or closed panel keeps the keys. | Medium | High | Decision 7. `FocusStateEdgeTests` checks each case. |
| A code point above U+FFFF arrives cut from BRUTAL. | Medium | Low | Decision 10. The adapter drops a non-scalar value. Task 4.9 writes the issue text. No text field exists yet. |
| A handler that stops each key also stops Tab, so the focus cannot leave the element. | Low | Medium | The remarks of `DrainEvents` state the rule. interactive-elements stops a Tab only to keep it. |
| A handler sets the focus during a Tab walk with no stop, and the Tab then moves on from the new owner. | Low | Low | The remarks state: stop the walk to keep the choice. Test 5.5 covers a set from a handler. |
| The Tab search walks the full tree for each Tab. | Low | Low | One pass with an early return. A Tab is rare. The gate runs it on 1000 elements. |
| The ancestor rule of the click surprises an author who expects the hit only. | Medium | Low | Notes Q3. `IsFocusable` on the child keeps the focus on the child. |
| A press of any button clears the focus of a text field. | Low | Low | Decision 4. A later filter can name the buttons. |
| The stale rule of one context clears a `Focus` state that another context set. | Low | Low | The CR-8 limit of pointer-routing. The remarks state it. |
| A growth of the focus stack after the warm-up allocates in a gate. | Low | Medium | The warm-up runs the deepest walk. The tier note of phase 6 forbids extra warm-up rounds. |
| The OnKey or OnChar signature differs from the research. | Low | Medium | Task 4.1 runs first, and the tier note says to stop and report. |

---

## Dependencies

### Internal Dependencies
- `input-routing/event-model`: the structs, the ring, `PushEvent`, the internal `DrainEvents` and its target step, `IsInRootTree`, `DispatchQueued`, `EventWalk`, `UiEventHeader.IsPropagationStopped`, `EventRecorder`, and the gate pattern.
- `input-routing/pointer-routing`: `RoutePointer` and its `PointerDown` branch, `SetHover` (the pattern of `SetFocusOwner`), `DropStalePointerState`, `HitWalk` (the stack pattern and the hidden rule), `GlfwUiInput` and its test seam, `ButtonModifiers`, `PointerTree`, `PointerAllocationTests`, and `GlfwUiInputAllocationTests`. The work tree of this branch holds pointer-routing with no commit yet. Start this feature after that commit (Notes, Conflicts 1).
- `core-tree`: `Element`, `Parent`, `ChildStorage`, `IsPickable`, `UiContext`, and `AllocationGate`.
- `style-system`: `PseudoStates.Focus` and `Disabled`, `Element.States` (a change marks the subtree), and the `:focus` selector.
- `imgui-renderer`: `ThousandElementTree` and `RecordingRenderer`.

### External Dependencies
- `Ahwoo.BRUTAL.Glfw` 2026.10.4, pinned in `Directory.Packages.props`: `GlfwKeyCallback`, `GlfwCharCallback`, `GlfwKey`, and `GlfwKeyAction`. `BrutalUi.Glfw` references it already. No new package and no new native dependency, so no new note in `context.md` is necessary.
- `ilspycmd` in the session scratch folder for task 4.1 only, when the XML doc is not enough. No repository change.
- .NET 10 (`System.Text.Rune`).

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. Code focuses an element with `SetFocus`. The element holds `Focus`, and after `Update` it shows the fill of its `:focus` rule. A key with no target walks to it: root capture, target capture slot, target bubble slot, root bubble (`FocusOwnerTests.AKeyWithNoTargetWalksToTheFocusOwner`).
- [ ] F2. A char with no target walks to the owner. A key with no owner gets no walk. A key with a caller target walks to that target. `SetFocus` returns `false` for an element that is not focusable or not in the roots. `ClearFocus` clears the state (`FocusOwnerTests`, 8 tests).
- [ ] F3. A press focuses the hit element or its nearest focusable ancestor, before the walk. A press on no focusable element, or over nothing, clears the focus. A move, a release, a scroll, and a caller target keep the focus (`ClickFocusTests`, 8 tests).
- [ ] F4. Tab moves the focus in tree order and wraps. Shift+Tab moves back and wraps. The search skips an element that is not focusable and a hidden subtree, and it crosses the roots in the order of add. A handler that stops the Tab keeps the focus. `Control`, `Alt`, and `Super` stop the move. A repeat moves, and a key up does not (`TabNavigationTests`, 12 tests).
- [ ] F5. `FocusNext` and `FocusPrevious` move the focus with no event.
- [ ] F6. A disabled element takes no focus. An owner that leaves the tree, turns unfocusable, turns disabled, or gets a hidden ancestor loses the focus at the next drain (`FocusStateEdgeTests`, 8 tests).
- [ ] F7. `GlfwUiInput` pushes a key down, a repeat, a key up, and a char with the right key, scan code, modifiers, code point, and timestamp. A Tab through the adapter moves the focus. A non-scalar char drops. `KeyboardEnabled` set to `false` and `Dispose` stop the key and char pushes (`GlfwUiInputKeyTests`, 8 tests).
- [ ] F8. `FocusAllocationTests` measures 0 bytes for 100 rounds of the Tab search on the warm 1000-element tree, and for 100 steps of the full keyboard path with the click focus and `Update`. `GlfwUiInputAllocationTests` measures 0 bytes with the key and char pushes and handlers.
- [ ] F9. A planted allocation at the start of `FocusWalk.Find`, and one at the start of `SetFocusOwner`, each give a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` has 39 more tests than the count in `context.md` before phase 1 (8 + 8 + 12 + 8 + 3). `BrutalUi.Glfw.Tests` has 20 tests (12 + 8).
- [ ] Q3. No method of `FocusWalk` calls itself. `FocusWalk.cs` holds no `List<` and no LINQ.
- [ ] Q4. `FocusWalk`, `FocusFrame`, `AcceptsFocus`, `TabKey`, `FocusStackLength`, `FocusStackSlots`, `PushKey`, and `PushChar` are internal.
- [ ] Q5. The measured bodies of the gates hold no lambda, no `$"`, no `string`, and no `Assert`.
- [ ] Q6. The changed docs use ASD-STE100 prose. The lint gives no score above 2.5.
- [ ] Q7. `src/BrutalUi.Core/Input/EventWalk.cs` has the SHA-256 `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`.
- [ ] Q8. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows, and the Glfw test project runs with no skip there.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no `using` of a GLFW namespace and holds 2 `PackageReference` items.
- [ ] I2. `Directory.Packages.props`, each `.csproj` file, `brutal-ui.sln`, `ThousandElementAllocationTests.cs`, and `scripts/` do not change.
- [ ] I3. `UiContext.Update` does not call `DrainEvents` (routing-gate owns the call). `PushEvent` keeps four public overloads.
- [ ] I4. The drain changes only its target step, the `PointerDown` branch of `RoutePointer`, the step after the walk, and one call at its start.
- [ ] I5. `FocusedElement` is public, so routing-gate can read it for `KeyboardFocusInUi`.
- [ ] I6. Sections 5, 9, and 14 of `docs/technical-design.md` and `CHANGELOG.md` name the feature. Section 9 names `KeyboardEnabled`, `FocusNext`, and `FocusPrevious`.
- [ ] I7. `docs/features/input-routing/keyboard-focus/context.md` holds the API rows of task 4.1 and the issue text for BRUTAL.
- [ ] I8. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run steps 1 to 10 from `/home/aaronstatic/dev/brutal-ui` on Linux. Step 11 is MANUAL. `<scratch>` is any empty folder outside the repository.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the totals with the counts in `context.md` (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `FocusOwnerTests` 8 (F1, F2), `ClickFocusTests` 8 (F3), `TabNavigationTests` 12 (F4, F5), `FocusStateEdgeTests` 8 (F6), `FocusAllocationTests` 3 (F8), `PointerPressTests` 8, `PointerStateEdgeTests` 8, and `PointerAllocationTests` 3. Read the test names in each file and match them to the criteria.
4. Run `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj 2>&1 | tail -n 5`. Expect total 20 and `failed: 0`. Two skips are allowed with no display (F7, F8). When `xvfb-run` exists, run the same command under `xvfb-run -a` and expect no skip.
5. Copy `src/BrutalUi.Core/Input/FocusWalk.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `Find`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file from the copy and check it with `cmp` (F9).
6. Copy `src/BrutalUi.Core/Tree/UiContext.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `SetFocusOwner`. Run the command of step 5 and expect a value that is not 0. Restore the file and check it with `cmp`. Run step 2 again and expect 0 failed (F9).
7. `grep -rn 'using .*Glfw' src/BrutalUi.Core; grep -c PackageReference src/BrutalUi.Core/BrutalUi.Core.csproj; git diff main --stat -- Directory.Packages.props '*.csproj' brutal-ui.sln tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs scripts/ src/BrutalUi.Core/Input/EventWalk.cs`. Expect no line from the first grep, `2` from the second, and no diff (I1, I2). `sha256sum src/BrutalUi.Core/Input/EventWalk.cs` gives the value of Q7.
8. `grep -n 'DrainEvents' src/BrutalUi.Core/Tree/UiContext.cs`. Expect the declaration with `internal` and no call inside `Update` (I3). `grep -c 'public void PushEvent' src/BrutalUi.Core/Tree/UiContext.cs` gives `4` (I3). `grep -nE '^(public|internal).*(class|struct) ' src/BrutalUi.Core/Input/FocusWalk.cs` gives two `internal` lines (Q4). `grep -n 'Find(' src/BrutalUi.Core/Input/FocusWalk.cs` shows the declaration only (Q3). `grep -nE 'List<|System.Linq' src/BrutalUi.Core/Input/FocusWalk.cs` gives no line (Q3). `grep -n 'public Element? FocusedElement' src/BrutalUi.Core/Tree/UiContext.cs` gives one line (I5).
9. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I8).
10. Read sections 5, 9, and 14 of `docs/technical-design.md`, the `## [Unreleased]` part of `CHANGELOG.md`, and the API rows and the BRUTAL issue text of the feature `context.md` (I6, I7). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).
11. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0. Run step 4 and expect no skip (Q8).

---

## Testing Strategy

### Unit Tests (in `tests/BrutalUi.Core.Tests/Input/`)
- `FocusOwnerTests` (8): the demo, the char route, no owner, the caller target, the state move, the clear, and the two `SetFocus` rejects.
- `ClickFocusTests` (8): the focus of the hit, the nearest focusable ancestor, the focusable child, the clear on a gap and over nothing, the events that keep the focus, the caller target, and the right button.
- `TabNavigationTests` (12): the order and the wrap in both directions, the skip rules, two roots, the stop, the modifiers, the repeat and the key up, one and no candidate, the code moves, the caller target, and a 40-level chain with the stack growth and the clear.
- `FocusStateEdgeTests` (8): the stale rules, the disabled rule, the same-owner set, and a set from a handler.
- Each test uses `PointerTree` or its own small tree, pushes hand-made events with a null target, and calls the internal `DrainEvents()`. No Core test uses GLFW. Each test interns its class with a unique prefix, because xUnit runs the classes in parallel.

### Adapter Tests (in `tests/BrutalUi.Glfw.Tests/`)
- `GlfwUiInputKeyTests` (8): the conversions through the test seam and the internal `PushKey` and `PushChar`, the Tab constant, the char filter, `KeyboardEnabled`, and `Dispose`.
- `GlfwUiInputTests.ARealWindowSubscribesAndUnsubscribes`: also counts the `OnKey` and `OnChar` handlers. It skips with no display.

### Allocation Tests
- `FocusAllocationTests` (3): the Tab search gate on the 1000-element tree, the full keyboard path gate with the click focus and `Update`, and the real-work check.
- `GlfwUiInputAllocationTests` (2, extended): the key and char pushes and the real `OnKey` and `OnChar` handlers into a full ring.
- Each gate calls `AllocationGate.ReleaseAllocationContext()` before its measured region. routing-gate adds the queue case to `ThousandElementAllocationTests`.

### Integration Tests
- No HelloPanel change. routing-gate adds the visible demo with a `:focus` rule and a synthetic Tab.

### Manual Testing
- The Windows run of Q8. No visible output exists until routing-gate wires the drain.

---

## Related Documentation

- **Requirements:** `docs/features/input-routing/keyboard-focus/requirements.md`
- **Epic:** `docs/features/input-routing/epic-overview.md`, `docs/features/input-routing/epic-requirements.md`
- **Siblings:** `docs/features/input-routing/event-model/implementation.md` and `context.md`, `docs/features/input-routing/pointer-routing/implementation.md` and `context.md` (Decisions 2, 9, 10, 12, 14, R3, the Phase 4 API table, and the review follow-ups), `docs/features/input-routing/routing-gate/requirements.md`
- **Design:** `docs/technical-design.md` sections 5 (Shape, Events), 7 (the pseudo-states), 9 (Input), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Input/HitWalk.cs` (the stack, the hidden rule, the clear in `finally`), `src/BrutalUi.Core/Tree/UiContext.cs` (`DrainEvents`, `RoutePointer`, `SetHover`, `DropStalePointerState`, `IsInRootTree`), `src/BrutalUi.Glfw/GlfwUiInput.cs` (the seam and the `Push*` pattern), `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs` and `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs` (the gates)
- **External:** the GLFW input guide, [key input](https://www.glfw.org/docs/latest/input_guide.html#input_key) and [text input](https://www.glfw.org/docs/latest/input_guide.html#input_char). The Dear ImGui FAQ entry on `io.WantCaptureKeyboard`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (the first one) of each question.

**Q1. What is the demo of phase 1?**
- **A (recommended):** A unit test focuses `Right` with `SetFocus`, adds a `:focus` rule, pushes a key down with no target, drains, and runs `Update`. It reads `Focus`, the focus fill, and the walk order on `Right` (`--filter-class '*FocusOwnerTests'`, total 8). Reason: it proves the owner, the state, the restyle, and the key route with no click and no Tab.
- B: A unit test presses over `Right` and reads `Focus` (the click focus first). It proves no key route.
- C: A person presses Tab in HelloPanel and sees a focus color. It needs the drain in `Update`, which belongs to routing-gate.

**Q2. What is the rule between ImGui and the UI focus for the keyboard?**
- **A (recommended):** ImGui wins when it asks. A public `GlfwUiInput.KeyboardEnabled` (default `true`) turns the key and char pushes off. The game sets it from `io.WantCaptureKeyboard` before `Glfw.PollEvents()`. Reason: the mission keeps ImGui debug panels next to the UI, the pointer-routing hand-off gives this rule to keyboard-focus, and the switch costs one property with no allocation.
- B: No rule. Both handlers get each event. A Tab in an ImGui text field also moves the UI focus, and the game has no way to stop it.
- C: Option A plus a `PointerEnabled` switch from `io.WantCaptureMouse`. The pointer side belongs to the gate design of routing-gate (Conflicts 8).

**Q3. Which element gets the focus on a click?**
- **A (recommended):** The hit element, or its nearest ancestor with `IsFocusable`. Reason: a click on the label or the icon of a button focuses the button, as in a browser, at the cost of one parent walk.
- B: The hit element only, as `Hover` and `Active`. A label inside a focusable button must set `IsPickable = false`.

**Q4. Can a handler keep the Tab?**
- **A (recommended):** Yes. The Tab walks to the old owner first. When no handler stops the walk, the focus moves after the walk. Reason: a later `TextField` or a game panel can keep the Tab, as with the default action of a browser.
- B: No. The focus moves before the walk, as the pointer states do, and the Tab walks to the new owner.

### Conflicts and Gaps

1. **pointer-routing has no commit yet.** The git status shows its files as uncommitted on `feat/input-routing/pointer-routing`. Phase 1 starts after that commit. The hash of `EventWalk.cs` comes from Decision R3 of pointer-routing.
2. **Section 9 of the technical design** says "Focus moves on click, Tab, and gamepad navigation". The confirmed Q2 moves gamepad to the action map. Task 6.6 rewrites the item with the slot `FocusNext` and `FocusPrevious`.
3. **"No list" in the requirement.** `FocusWalk` keeps a frame stack (an array), as `HitWalk` and `PaintWalk` do, and it collects no candidates. When the requirement means no array at all, alternative (a) of Decision 6 applies, at the cost of the `IndexOf` steps.
4. **The char defect of BRUTAL.** `GlfwWindow` casts the code point to `ushort`. Text outside the Basic Multilingual Plane (for example an emoji) arrives wrong until BRUTAL fixes the cast. The `TextField` of interactive-elements has the same limit. Task 4.9 writes the issue text, and a person files it.
5. **The owner is public.** The epic context of this plan names "the internal focus owner that routing-gate reads". The requirement asks that code can read the owner, so `FocusedElement` is public. routing-gate reads the public member. No confirmed item conflicts.
6. **A5 and the code calls.** `SetFocus`, `ClearFocus`, `FocusNext`, and `FocusPrevious` change the state at once, outside the queue. A5 covers the events of the adapter only, so no confirmed item conflicts (Decision 12).
7. **No focus and blur events.** `UiEventType` has seven kinds and no focus or blur kind. The requirement asks for none. A handler reads `States`. When the `TextField` of interactive-elements needs a blur to commit its text, that feature adds a kind, a struct, and two slots. `EventWalk` stays generic.
8. **The pointer side of the ImGui rule.** pointer-routing has no rule between ImGui and the UI for the pointer. A click on an ImGui window over a UI element also moves the UI focus. Q2 adds the keyboard switch only. routing-gate decides the pointer side with its gate (`io.WantCaptureMouse`).
9. **The existing tests.** `IsFocusable` defaults to `false`, so the pointer tests and the event-model tests see no focus change. `EventQueueTests.ADrainSkipsAnEventWithNoTarget` and the null char of `EventAllocationTests` still skip, because no owner exists. No existing test needs an edit except the two adapter tests of tasks 4.7 and 6.4.
10. **`Disabled` and the hidden rule.** The requirements name neither. Decisions 2 and 7 add them, so a disabled or hidden element never holds the keys. Each is one check with no allocation.
11. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 6.8 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
