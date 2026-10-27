# Text Field - Implementation Plan

**Epic:** interactive-elements (feature 4 of 5)
**Status:** Ready for Review
**Started:** 2026-10-27
**Target Completion:** TBD
**Last Updated:** 2026-10-27 07:35

---

## Quality Bar

**Target quality:** A core library widget that edits bytes in place. `TextField` is the largest widget of the epic. It holds a UTF-8 buffer, a caret, and a selection, and it changes `UiContext.Update` in one place. A caret that splits a code point, a buffer that grows, an insert that half fits, a scroll that hides the caret, or an allocation per keystroke breaks each game that uses a text field. The framework epic binds the field to a store, so the change delegate must fire one time for each change.

- **The buffer never grows (A5):** The constructor allocates one `byte[]` of `Capacity` bytes. No later path allocates a buffer. An insert that does not fit drops the input and changes nothing.
- **The caret never splits a code point:** The caret and the selection anchor sit on a code point boundary after each path. A test types and removes 1-, 2-, 3-, and 4-byte code points.
- **One change, one call:** Each user edit that changes the text calls `TextChanged` one time, after the change. A caret move, a dropped insert, and a set from code call nothing.
- **The caret stays visible:** After each `Update`, the caret of the focused field lies inside the content box. The text scrolls by the smallest distance that does this.
- **The walks do not change:** `HitWalk.cs` and `PaintWalk.cs` have no diff. The field clips through `Overflow.Hidden` and moves its text in `PaintContent` (risk 12 of the scroll-view review).
- **Disabled means no effect:** A disabled field takes no char and no key, and it does not stop the walk. A set from code still works.
- **0 bytes after warm-up:** `Update`, `Draw`, each char, each key, `SetText`, and `GetText` allocate 0 bytes. A planted allocation fails a gate.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. Section 5 of the technical design states the text field rules.
- **No ImGui in Core:** `BrutalUi.Core` gets no package and no ImGui `using`.

---

## Executive Summary

This feature adds `TextField`, a single-line text editor. It is a sealed `Element` subclass in `src/BrutalUi.Core/Elements/`, with the type id `TextField` (epic A2). It follows the widget pattern of button-and-toggle: two cached `static readonly` handler delegates (`OnChar` and `OnKeyDown`), checks in `WidgetInput`, `IsFocusable = true`, and a change delegate with the field as the sender (epic Q3).

The text lives in one fixed `byte[]` of `CreateInfo.Capacity` bytes (A5). The caret and the selection anchor are byte offsets on code point boundaries. A char event inserts the UTF-8 form of its code point at the caret, or replaces the selection. Left, Right, Home, and End move the caret. Shift with a move extends the selection. Backspace and Delete remove one code point, or the selection. Enter calls `Submitted`. Each edit that changes the text calls `TextChanged`. `SetText` (span, string, and `LString`) and `GetText(Span<byte>)` read and write the buffer with no allocation. A set past the capacity cuts the text at the last whole code point that fits (Notes Q3). A left press places the caret at the nearest code point boundary, and a drag selects (Notes Q2).

The constructor sets `Inline.Overflow = Overflow.Hidden`. The existing draw walk thus clips the text to the padding box before it calls `PaintContent`, so the walks do not change. `PaintContent` draws the selection, the text at the content box minus the text scroll, and a caret of 1 pixel.

The draw reads the tree only, and a handler has no measurer during the drain. Thus `UiContext.Update` gets one new step, the caret pass, after the layout pass. When the focus owner is a `TextField`, the pass advances its blink timer and scrolls its text so the caret stays visible. `SetFocusOwner` restarts the blink of a field that gets the focus. These are the only changes outside the new file and `WidgetInput.cs`.

The plan has 7 phases and 38 tasks.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates a `TextField` with a capacity and a size, sets `TextChanged` and `Submitted`, reads the text with `GetText` or `Text`, sets it with `SetText`, and writes `TextField`, `:focus`, and `:disabled` rules)
- [x] **The player** (types a name, moves the caret, selects with Shift, deletes, and presses Enter)
- [x] **The sibling feature agents** (widget-sample puts a field in HelloPanel and adds it to the 1000-element gate)
- [x] **The framework epic** (binds `TextChanged` and `Submitted` to a store or a command)
- [x] **The maintainer** (runs one more 0-byte gate)
- [ ] **The markup author** (not served. The markup epic registers `TextField` by name)

**User Subsets Served:**
- [x] **Simple** - `var name = new TextField(new TextField.CreateInfo { Capacity = 64 }); name.Inline.Width = 200; name.Inline.Height = 20; name.Submitted = OnName;` and typed text.
- [x] **Complex** - a field with a start text, a capacity in bytes for a network packet, `SetText("Player"u8)` from a save, `GetText` into a stack buffer, Shift selection, `States |= PseudoStates.Disabled` during a cut scene, and one static method that serves many fields through the sender.

**UX Approach:**
- The game reads two delegates. It never reads the raw slots.
- The player edits as in a desktop text box: the caret steps by code point, the text scrolls under the caret, and the caret blinks.
- A full field drops a keystroke with no error.

---

## Goals

### Primary Goals
1. Add `TextField`: a focused field takes a typed code point and shows it, clipped to its padding box.
2. Move the caret by code point with Left, Right, Home, and End. Remove one code point with Backspace and Delete.
3. Report each change through `TextChanged`, and Enter through `Submitted`.
4. Read and write the text from code with no allocation.
5. Scroll the text so the caret stays visible, and blink the caret.
6. Select with Shift and a move. Replace and remove the selection.
7. Place the caret with a left press, and select with a drag.
8. Block each input on a disabled field.
9. Allocate 0 bytes in `Update`, `Draw`, and the input path after warm-up.

### Success Criteria
- [ ] The phase 1 demo passes: `TextFieldTypeTests` reads a total of 10 with `failed: 0`, and the demo test reads the text `Hi`, two `TextChanged` calls, and a `Text` call `"Hi"` at (0, 0) inside the clip (0, 0, 81, 16).
- [ ] The caret, delete, submit, text, scroll, blink, selection, pointer, and disabled test classes pass.
- [ ] Each existing test passes with no edit.
- [ ] `TextFieldAllocationTests` measures 0 bytes, and a planted allocation in `TextField.HandleChar` fails it.
- [ ] Section 4, 5, 8, 9, and 14 of the technical design hold the text field rules and the caret pass.

---

## Architecture Overview

### Codebase Findings

1. **The key and char route exists.** `UiContext.DrainEvents` gives each `KeyDown`, `KeyUp`, and `Char` entry with no target to the focus owner (`FocusedElement`). With no owner, the entry gets no walk. `CharEvent.CodePoint` is a Unicode scalar value, and `GlfwUiInput` drops each other value. `FocusOwnerTests` pins the route.
2. **Tab walks first.** A Tab `KeyDown` walks to the owner first. The drain moves the focus after the walk only when no handler stopped it. Thus the field must not stop Tab.
3. **The clip comes before the paint hook.** `PaintWalk.Enter` pushes the clip of the padding box in step 6 for an overflow other than `Visible`, and it calls `PaintContent` in step 7. The view itself, its clip, and its `PaintContent` use the origin with no offset (risk 12 of the scroll-view `code-review.md`). Thus `Overflow.Hidden` clips the text, and the field moves its text in `PaintContent`.
4. **The draw reads the tree only.** `UiContext.Draw` and rule 9 of the widget rules (section 5) forbid a tree change and a dirty mark in `PaintContent`. The text scroll is state, so `Update` must write it.
5. **No measurer during the drain.** `UiContext.Update` sets `t_measurer` around the layout pass only. A handler thus cannot measure text. The layout of the frame also comes after the drain. The text scroll needs both, so it runs after the layout pass.
6. **No per-element tick.** `Update(deltaTime)` gives the time to the transition pass only. The blink needs a new step that reads `deltaTime`.
7. **One funnel for the focus.** `UiContext.SetFocusOwner` is the only method that changes `_focusOwner`. `AcceptsFocus` is `IsFocusable` and not `Disabled`, and `DropStaleFocus` drops a disabled owner at the next drain.
8. **The measurers agree on one line.** `RecordingRenderer.MeasureText` gives `(bytes * size * 0.5, size)` with no wrap, and `(0, size)` for an empty text. `ImGuiRenderer.MeasureText` gives `(0, fontSize)` for an empty text. With a font size of 16, a test byte is 8 pixels wide.
9. **`Utf8Text` grows.** It doubles its buffer, so it breaks A5. The field owns its own fixed `byte[]`. The internal static `Utf8Text.Utf8Equals(ReadOnlySpan<byte>, ReadOnlySpan<char>)` compares a string with the buffer with no allocation, and the field reuses it.
10. **`WidgetInput` holds the key codes and the target rule.** `IsArrowKey` reads the target, the type, and the modifiers, and passes a repeat. `IsKeyActivation` rejects a repeat. `EnterKey` is 257 (the main Enter key). `SliderKeyTests` asserts the raw values. `GlfwUiInputKeyTests.TheCoreTabKeyEqualsTheGlfwTabKey` compares an internal Core value with `GlfwKey`.
11. **The analyzer.** `BRUTALCORE0011` rejects `SetText("x")` with a literal when a span overload exists. Tests write `SetText("x"u8)`. A bare `SetText(null)` is ambiguous.
12. **The handler shape.** Each widget handler is a static method with no target. It casts `e.Header.CurrentTarget` with `is` (rule 4 of the widget rules). The brief says "with the element as the target". The plan follows the code (Conflicts and Gaps item 1).

### File Tree (end state)

```
src/BrutalUi.Core/
├── Elements/TextField.cs           # phase 1 (buffer, char), 2 (keys, caret draw), 3 (Enter, SetText, GetText),
│                                   # 4 (caret pass, scroll, blink), 5 (selection), 6 (press, drag),
│                                   # 7 (plant and restore only)
├── Elements/WidgetInput.cs         # phase 1 (IsCharInput), 2 (IsEditKey and four key codes)
└── Tree/UiContext.cs               # phase 4 (the caret pass in Update, the blink restart in SetFocusOwner)
tests/BrutalUi.Core.Tests/
├── Elements/TextFieldTree.cs            # phase 1
├── Elements/TextFieldTypeTests.cs       # phase 1
├── Elements/TextFieldCaretTests.cs      # phase 2
├── Elements/TextFieldDeleteTests.cs     # phase 2
├── Elements/TextFieldSubmitTests.cs     # phase 3
├── Elements/TextFieldTextTests.cs       # phase 3
├── Elements/TextFieldScrollTests.cs     # phase 4
├── Elements/TextFieldBlinkTests.cs      # phase 4
├── Elements/TextFieldSelectionTests.cs  # phase 5
├── Elements/TextFieldPointerTests.cs    # phase 6
├── Elements/TextFieldDisabledTests.cs   # phase 7
└── Elements/TextFieldAllocationTests.cs # phase 7
tests/BrutalUi.Glfw.Tests/
└── TextFieldKeyCodeTests.cs        # phase 2
docs/technical-design.md            # phase 7: sections 4, 5, 8, 9, 14
CHANGELOG.md                        # phase 7
docs/features/interactive-elements/text-field/context.md   # each phase
```

These files do not change: `Element.cs`, `PseudoStates.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `FocusWalk.cs`, `HitWalk.cs`, `PaintWalk.cs`, `CharEvent.cs`, `KeyEvent.cs`, `IDragCapture.cs`, `LayoutNode.cs`, each file in `src/BrutalUi.Core/Layout/`, each file in `src/BrutalUi.Core/Style/`, `IUiRenderer.cs`, `Utf8Text.cs`, `Label.cs`, `Image.cs`, `Button.cs`, `Toggle.cs`, `Slider.cs`, `ScrollView.cs`, each `.csproj`, `Directory.Packages.props`, `samples/`, `scripts/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Glfw/`, and each existing test file.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public sealed class TextField : Element
{
    public TextField(in CreateInfo info);           // phase 1: ArgumentOutOfRangeException for a Capacity of 0 or less
    public override ElementTypeId TypeId { get; }   // phase 1: the id of "TextField"
    public int Capacity { get; }                    // phase 1: the buffer size in bytes
    public ReadOnlySpan<byte> Text { get; }         // phase 1: the UTF-8 text, valid until the next change
    public Action<TextField>? TextChanged { get; set; }  // phase 1 (Q3 of the epic)
    public Action<TextField>? Submitted { get; set; }    // phase 3
    public int GetText(Span<byte> destination);     // phase 3: copies, cut at a code point, returns the byte count
    public void SetText(ReadOnlySpan<byte> utf8);   // phase 3: copies, cut at the capacity (Notes Q3)
    public void SetText(string? text);              // phase 3: encodes, cut at the capacity
    public void SetText(LString? text);             // phase 3: reads LString.Localized one time

    public struct CreateInfo
    {
        public required int Capacity;               // phase 1: no safe default (A5)
        public string? Text;                        // phase 1: the start text, cut at the capacity
    }
}
```

### Internal Types

- `WidgetInput` (`src/BrutalUi.Core/Elements/WidgetInput.cs`), additions:
  - `internal static bool IsCharInput(Element widget, in CharEvent e)`: the widget does not hold `Disabled`, the type is `Char`, and the target is the widget. Phase 1.
  - `internal static bool IsEditKey(Element widget, in KeyEvent e)`: the widget does not hold `Disabled`, the type is `KeyDown`, the target is the widget, and no `Control`, `Alt`, or `Super` is down. A repeat passes. `Shift` passes. Phase 2.
  - `internal const int BackspaceKey = 259`, `DeleteKey = 261`, `HomeKey = 268`, `EndKey = 269` (the GLFW values). Phase 2.
- `TextField` internal members (tests read them):
  - `internal const float CaretWidth = 1f`, `internal const float CaretBlinkInterval = 0.5f` (phase 4), `internal const float SelectionAlpha = 0.35f` (phase 5).
  - `internal int CaretIndex { get; }` (phase 1), `internal int AnchorIndex { get; }` (phase 5), `internal float TextScroll { get; }` (phase 4), `internal bool CaretVisible { get; }` (phase 4).
  - `internal static int NextBoundary(ReadOnlySpan<byte> text, int index)` and `internal static int PreviousBoundary(ReadOnlySpan<byte> text, int index)`: one step by `Rune.DecodeFromUtf8` or `Rune.DecodeLastFromUtf8`. Each invalid sequence is one step of at least 1 byte. Phase 2.
  - `internal static int CutAtBoundary(ReadOnlySpan<byte> text, int maxBytes)`: the length of the longest prefix of at most `maxBytes` that ends on a boundary. Phase 3.
  - `internal void AdvanceCaret(float deltaTime, IUiRenderer? measurer)`: the caret pass (Decision 10). Phase 4.
  - `internal void RestartCaretBlink()`: sets the blink timer to 0. Phase 4.
  - `internal static int IndexAtX(ReadOnlySpan<byte> text, float x, IUiRenderer measurer, string? font, float fontSize)`: the code point boundary with the measured prefix width nearest to `x`. A tie goes to the lower index. An `x` at or below 0 gives 0, and an `x` past the text gives the length. Phase 6.
  - `TextField` implements `IDragCapture` explicitly. `CapturesMoves` is `true` when the field does not hold `Disabled`. Phase 6.
- `TextField` private members: `_buffer` (`byte[]`, `readonly`), `_length`, `_caret`, `_anchor` (phase 5), `_scroll` (phase 4), `_blink` (phase 4), `_pendingX` and `_pendingExtend` (phase 6), the static `HandleChar`, `HandleKeyDown`, `HandlePointerDown`, and `HandlePointerMove` (phase 6), `bool Insert(ReadOnlySpan<byte> utf8)`, `void Remove(int start, int end)`, `void MoveCaret(int index, bool extend)`, and `void WriteText(...)`.
- `UiContext` (phase 4): in `Update`, after the `finally` block of the layout pass and before the clear pass: `if (_focusOwner is TextField field) { field.AdvanceCaret(deltaTime, _measurer); }`. In `SetFocusOwner`, after `next.States |= PseudoStates.Focus` in the change branch: `if (next is TextField field) { field.RestartCaretBlink(); }`.

### The Edit Rules

1. **The buffer.** `_buffer[0.._length]` holds the text. `0 <= _anchor, _caret <= _length <= Capacity`. Both offsets sit on a boundary. The selection is `[min(_anchor, _caret), max(_anchor, _caret))`. It is empty when the two are equal.
2. **The char.** `HandleChar` runs `IsCharInput`. A code point that is not a valid scalar (`Rune.IsValid`) or that is a control character (`Rune.IsControl`: C0, DEL, and C1) returns with no stop, so it walks on. Else the handler stops the walk and encodes the rune into `stackalloc byte[4]`. The new length is `_length - selectionLength + runeLength`. Above `Capacity`, the handler returns: nothing changes, and nothing is called (requirement: "drops the input"). Else it removes the selection, inserts the bytes at the caret, moves the caret after them, collapses the anchor, and calls `TextChanged`.
3. **The keys.** `HandleKeyDown` runs `IsEditKey`, then switches on the key. Each key in this list stops the walk, also when nothing changes:
   - Left and Right: with a selection and no Shift, the caret goes to the start (Left) or the end (Right) of the selection, and the anchor follows. Else the caret steps one code point. With Shift, the anchor stays. Else the anchor follows.
   - Home and End: the caret goes to 0 or `_length`. With Shift, the anchor stays.
   - Backspace: with a selection, remove it. Else remove `[PreviousBoundary(_caret), _caret)` when `_caret > 0`.
   - Delete: with a selection, remove it. Else remove `[_caret, NextBoundary(_caret))` when `_caret < _length`.
   - Enter (`WidgetInput.EnterKey`, no repeat): stop and call `Submitted`. A repeat of Enter walks on.
   - Each other key (Tab, Up, Down, Escape, keypad Enter, and a letter) walks on.
4. **The calls.** A remove or an insert that changes the bytes calls `TextChanged` one time, after the change. A caret move calls nothing.
5. **The marks.** Each change of the text, the caret, the anchor, the scroll, or the shown caret marks `DirtyFlags.Paint`. The field has no own size, so no change marks `Layout`.
6. **The blink restart.** Each change of the text or the caret, and each focus gain, sets `_blink` to 0, so the caret shows at once (phase 4).

### The Caret Pass and the Scroll Rule

`UiContext.Update` order after this feature: the input pass, the style pass, the transition pass, the layout pass, **the caret pass**, and the clear pass. The caret pass runs for the focus owner only, and only when it is a `TextField`.

```
AdvanceCaret(deltaTime, measurer):
  if deltaTime is finite and > 0:
      _blink = (_blink + deltaTime) mod (2 * CaretBlinkInterval)   // the shown phase is _blink < CaretBlinkInterval
      mark Paint when the shown phase flips
  if measurer is null: return                                     // a context from UiContext() keeps the scroll
  width  = content width of Layout.Rect (Layout.ContentBox of (0, 0, rect.Z, rect.W)).Z
  caretX = measurer.MeasureText(Style.Font, Style.FontSize, Text[.._caret], 0).X
  textW  = measurer.MeasureText(Style.Font, Style.FontSize, Text, 0).X
  next = _scroll
  if caretX - next > width - CaretWidth: next = caretX - (width - CaretWidth)
  if caretX - next < 0:                  next = caretX
  next = clamp(next, 0, max(0, textW + CaretWidth - width))       // a value that is not finite gives 0
  if next != _scroll: _scroll = next, mark Paint
```

`SetText` writes `_scroll = 0`. A field that is not the focus owner keeps its scroll. The next caret pass with the focus scrolls to the caret.

### The Draw

`PaintContent(renderer, borderBox, opacity)`. The walk pushed the clip of the padding box first.

1. `content = Layout.ContentBox(in borderBox)`. Return when the content width or height is 0 or less.
2. `x0 = content.X - _scroll`. `focused = (States & PseudoStates.Focus) != 0`.
3. With `focused` and a selection: `a = MeasureText(Text[..start]).X`, `b = MeasureText(Text[..end]).X`. `Rect((x0 + a, content.Y, b - a, lineHeight), Style.Color.ScaleAlpha(opacity * SelectionAlpha), Style.CornerRadius = none, 0, Transparent)`. The selection draws before the text (phase 5).
4. `Text((x0, content.Y), Style.Font, Style.FontSize, Style.Color.ScaleAlpha(opacity), Text, 0)`. No wrap. An empty text or an alpha of 0 draws nothing.
5. With `focused` and `CaretVisible`: `m = MeasureText(Text[.._caret])`. `Rect((x0 + m.X, content.Y, CaretWidth, m.Y), Style.Color.ScaleAlpha(opacity), no radius, 0, Transparent)` (phase 2. Phase 4 adds the blink test).

`lineHeight` is the `Y` of the measure. Both measurers give the font size for one line. The draw makes no tree change, no mark, and no allocation.

### Data Flow

```
Game: ui.PushEvent(Char 'H') ... PushEvent(Key Left, Shift) ... PushEvent(Key Enter) ... ui.Update(dt) ... ui.Draw(r)
UiContext.DrainEvents
  Char, no target ──► target = FocusedElement (the field)
    walk: TextField.HandleChar ──► WidgetInput.IsCharInput (not Disabled, Char, target is the field)
          invalid or control ──► return (walks on)
          StopPropagation ──► encode the rune ──► fits? no ──► return (dropped, no call)
                                               └─► remove the selection, insert, caret += n, anchor = caret
                                                   MarkDirty(Paint), RestartCaretBlink, TextChanged(field)
  KeyDown Left + Shift ──► target = the field
    walk: TextField.HandleKeyDown ──► WidgetInput.IsEditKey ──► StopPropagation ──► caret = PreviousBoundary, anchor stays
  KeyDown Enter ──► StopPropagation ──► Submitted(field)
  KeyDown Tab ──► the field does not stop it ──► FocusNext ──► SetFocusOwner(next) ──► RestartCaretBlink when next is a TextField
UiContext.Update: style pass ──► transition pass ──► layout pass
  ──► caret pass: _focusOwner is TextField ──► AdvanceCaret(dt, _measurer): blink, scroll to the caret
  ──► clear pass
UiContext.Draw: PaintWalk: the field box ──► PushClip(padding box) ──► PaintContent: selection, text at content.X - scroll, caret ──► PopClip
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Notes Q1 names the demo of phase 1. No person answered it, so phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `System.Text`, `System.Text.Unicode`, `Brutal.Localization`, `Brutal.Numerics` as needed). Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. Tests write UTF-8 literals with `"..."u8` (Finding 11). A test class that interns a class name uses a name prefix that is unique to the class, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

Each phase runs these classes with no edit (the regression set): the focus classes (`FocusOwnerTests`, `ClickFocusTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`), the walk classes (`PaintWalkTests`, `PaintContentTests`, `HitTestTests`), the button-and-toggle classes (`ButtonKeyTests`, `ButtonTextTests`, `ToggleTests`, `WidgetDisabledTests`, `WidgetAllocationTests`), the slider classes (`SliderKeyTests`, `SliderDisabledTests`, `SliderAllocationTests`), the scroll view classes (`ScrollViewScrollTests`, `ScrollViewSliderTests`, `ScrollViewAllocationTests`), and `ThousandElementAllocationTests`.

The test scene of each text field test is `TextFieldTree` (task 1.4), unless a task names another scene. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a column and is pickable. Its children are `Field` (a `TextField`, 81 x 16 at (0, 0), no padding, `Inline.FontSize = 16`, the default capacity 16) and `Other` (a plain element, 100 x 100 at (0, 16), `IsFocusable = true`). Each byte of the text is 8 pixels wide, so 10 bytes plus the caret fill the field exactly. A text of `n` bytes ends at x `8n`.

### Phase 1: A Typed Character Shows in the Field
**Goal:** The smallest path that proves the idea: a char event to a focused `TextField` inserts the UTF-8 form of the code point into the fixed buffer, calls `TextChanged`, and the draw shows the text inside the clip of the field.
**Demo:** a unit test settles the tree, focuses `Field`, pushes the chars `H` and `i` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads `Text` as `"Hi"u8`, two `TextChanged` calls with the field as the sender, and a `Text` call `"Hi"` at (0, 0) with the size 16 after `PushClip(0, 0, 81, 16)` and before its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldTypeTests'` and reads a total of 10 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase writes the fixed UTF-8 buffer and the fit rule (Edit Rule 2). The fit check comes before each change, so a dropped insert changes no byte. Encode the rune into `stackalloc byte[4]` with `Rune.EncodeToUtf8`. Insert with `Span<byte>.CopyTo` on overlapping ranges of the same array (it handles the overlap). The create-info text uses `Utf8.FromUtf16` into the buffer: on `DestinationTooSmall` it writes whole scalars only, so the cut is on a boundary. The phase 1 tests must not assert the absence of a `Rect` call in the frame, because phase 2 adds the caret rect.

**Tasks:**
- [ ] 1.1 Record in `docs/features/interactive-elements/text-field/context.md`, before any change: the base commit (`git rev-parse HEAD`), and the result of `git ls-files src/BrutalUi.Core/Elements/ScrollView.cs` (expect one line) and `git status --porcelain -- src tests` (expect no line). When scroll-view is not in the base commit, stop and record it. Record the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Glfw.Tests`, and the counts of each class of the regression set. Record the Codebase Findings.
- [ ] 1.2 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsCharInput(Element, in CharEvent)` (Internal Types). The doc comment states: the check reads the disabled bit, the type, and the target. The key route gives each char with no target to the focus owner, so a char that bubbles from a focusable child gives no input. It reads no press target and no modifier (a char has none). Extend the class remarks: `IsCharInput` serves each widget that takes text.
- [ ] 1.3 Write `src/BrutalUi.Core/Elements/TextField.cs` by the widget pattern: `public sealed class TextField : Element`, the type id of `TextField`, `CreateInfo` (`required int Capacity`, `string? Text`), the constructor, `Capacity`, `Text`, `CaretIndex`, `TextChanged`, the static `HandleChar` with its cached `static readonly UiEventHandler<CharEvent>`, `Insert`, and `PaintContent` (step 4 of The Draw only, with `_scroll` absent: `x0 = content.X`). The constructor checks `Capacity > 0` first and throws `ArgumentOutOfRangeException` with `ParamName` `info` and a message that names `CreateInfo.Capacity`. It allocates `_buffer = new byte[info.Capacity]` (the only `new byte[]` of the file), encodes the start text (cut at the capacity), puts the caret at the end, sets `IsFocusable = true` and `Inline.Overflow = Overflow.Hidden`, and writes `OnChar = s_char`. The class remarks state: the buffer rule (A5), the char rule with the drop, the stop rule, the sender, "read `TextChanged`, not the slot", "a write of `=` to `OnChar` removes the typing", the clip through the inline overflow, and the field has no own size (Decision 15).
- [ ] 1.4 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldTree.cs`: the scene of the phase intro. The constructor takes `in TextField.CreateInfo`, and a second constructor uses `Capacity = 16`. It holds the points `OverField` (40, 8), `OverOther` (50, 60), and `OverNothing` (600, 500), `Settle()` (`Update(0f)`), `Focus()` (`Settle()` then `Context.SetFocus(Field)`), `Type(string text)` (one `PointerTree.Char` push for each rune of the text), and `Frame()` (`Update(0f)` and `Draw` into the tree renderer, returns the renderer). The doc comment states the rects, the 8-pixel byte, and the clip (0, 0, 81, 16).
- [ ] 1.5 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldTypeTests.cs` with 10 tests: `ACharShowsInTheField` (the demo), `TheStartTextComesFirst` (start text `ab`, char `c`: `abc`, one call), `AMultiByteCodePointGoesInAsUtf8` (`é` then `😀`: the bytes `C3 A9 F0 9F 98 80`, the caret at 6), `AnInsertPastTheCapacityDrops` (16 chars `a`, then `b`: 16 bytes, 16 calls, a root bubble `OnChar` handler gets no char), `ACodePointThatDoesNotFitDrops` (15 chars `a`, then `€`: no change and no call. Then `b`: 16 bytes), `AControlCharWalksOn` (U+0008, U+007F, U+0085, U+D800, and U+110000: no change, no call, and the root bubble handler gets 5 chars), `ACharStopsTheWalk` (the root capture handler runs, the root bubble handler does not), `ACharWithACallerTargetInserts` (a char with `Field` as the caller target and no focus owner inserts. A char with no target and no owner changes nothing), `TheCreateInfoTheTypeAndTheOverflow` (the type id is `TextField`, `IsFocusable` is `true`, `Inline.Overflow` and `Style.Overflow` are `Hidden`, `Capacity` is 16, a capacity of 0 or -1 throws `ArgumentOutOfRangeException` with `ParamName` `info`, a start text `abc€` with capacity 4 gives `abc`, and a null start text gives an empty text with the caret at 0), and `ATypeRuleMatchesTheTextField` (a rule with `Type = ElementType.Intern("TextField")` sets a fill on the field and not on `Other`. A rule with `overflow: visible` on the type does not remove the clip).
- [ ] 1.6 Run the build, the tests, the demo class, and the regression set. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 40 (6 tasks, about seven steps for one task)

**Files (5):** `WidgetInput.cs`, `TextField.cs`, `TextFieldTree.cs`, `TextFieldTypeTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `TextFieldTypeTests` pass.
- [ ] Each class of the regression set passes with no edit.
- [ ] `UiContext.cs`, `HitWalk.cs`, `PaintWalk.cs`, and `Element.cs` have no diff against the base commit.

---

### Phase 2: The Caret Moves and Deletes by Code Point
**Goal:** Left, Right, Home, and End move the caret by code point. Backspace and Delete remove one code point. A char goes in at the caret. The focused field draws the caret.
**Demo:** a unit test creates the field with the start text `abc`, focuses it, pushes Left, Left, and the char `X`, and runs `Update` and `Draw`. It reads `Text` as `"aXbc"u8`, one `TextChanged` call, and a caret rect at (16, 0, 1, 16) in the text color after the `Text` call and before the `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldCaretTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*TextFieldDeleteTests'` reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase writes the boundary steps and the key switch (Edit Rule 3). `PreviousBoundary` and `NextBoundary` use `Rune.DecodeLastFromUtf8` and `Rune.DecodeFromUtf8`, and step `Math.Max(1, bytesConsumed)`, so an invalid byte is one step and the caret never sticks. Do not read Shift in this phase: Shift with Left moves the caret as Left does. Phase 5 adds the selection. Each used key stops the walk also at an end. Tab must walk on. The caret draws when the field holds `Focus` (step 5 of The Draw, with no blink check yet).

**Tasks:**
- [ ] 2.1 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsEditKey(Element, in KeyEvent)` and the constants `BackspaceKey` (259), `DeleteKey` (261), `HomeKey` (268), and `EndKey` (269) with doc comments that name the GLFW constants (`GLFW_KEY_BACKSPACE` and so on). The doc comment of `IsEditKey` states the target rule, the modifier rule (`Control`, `Alt`, and `Super` block. `Shift`, `CapsLock`, and `NumLock` do not), and that a repeat passes.
- [ ] 2.2 Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `NextBoundary`, `PreviousBoundary`, `Remove`, `MoveCaret`, and the static `HandleKeyDown` with its cached `static readonly UiEventHandler<KeyEvent>`. The constructor writes `OnKeyDown = s_keyDown`. The handler covers Left, Right, Home, End, Backspace, and Delete (Edit Rule 3, no Shift and no Enter yet). `HandleChar` inserts at the caret. Add step 5 of The Draw. Extend the remarks: the key rule, the stop rule at an end, the keys that walk on, "a write of `=` to `OnKeyDown` removes the keys", the caret draw, and the code point rule (a combining mark or an emoji sequence takes more than one step, Conflicts and Gaps item 4).
- [ ] 2.3 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldCaretTests.cs` with 10 tests: `ACharGoesInAtTheCaret` (the demo), `HomeAndEndMoveTheCaret` (`abc`: Home gives 0, End gives 3, and the caret rect moves to x 0 and x 24), `TheCaretStepsOverEachCodePoint` (`aé€😀`: Left from 10 gives 6, 3, 1, and 0. Right gives the reverse), `TheCaretStopsAtEachEnd` (Left at 0 and Right at the end change nothing, call nothing, and stop the walk: the root bubble `OnKeyDown` handler gets nothing), `TheOtherKeysWalkOn` (Up, Down, Escape, the keypad Enter (335), and the key A walk on to the root. A key up walks on. Tab walks on and moves the focus to `Other`), `TheModifiersBlockTheKey` (`Control`, `Alt`, and `Super` with Left: no move, and the key walks on. `CapsLock` and `NumLock` with Left move), `ARepeatMovesAgain` (two Left keys, the second with `IsRepeat = true`, give two steps), `ACaretMoveCallsNothing` (a move calls no `TextChanged` and marks `Paint`), `TheCaretDrawsOnlyWithFocus` (no caret rect with no focus. With the focus, one rect of width 1 and height 16 in `Style.Color`. With `Inline.Opacity = 0.5`, the caret alpha is the alpha of `Style.Color.ScaleAlpha(0.5f)`), and `TheKeyCodesAreTheGlfwValues` (`WidgetInput.BackspaceKey` is 259, `DeleteKey` 261, `HomeKey` 268, and `EndKey` 269).
- [ ] 2.4 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldDeleteTests.cs` with 8 tests: `BackspaceRemovesTheCodePointBeforeTheCaret` (`abc`, Left, Backspace: `ac`, caret 1, one call), `DeleteRemovesTheCodePointAfterTheCaret` (`abc`, Home, Delete: `bc`, caret 0), `BackspaceRemovesAWholeCodePoint` (`a😀`, Backspace: `a`, caret 1. `€b`, Home, Delete: `b`), `NothingToRemoveCallsNothing` (Backspace at 0 and Delete at the end: no change, no call, and the key stops the walk), `ARepeatRemovesAgain` (three Backspace keys, two with `IsRepeat`, remove three code points and give three calls), `ARemoveMakesRoomForAnInsert` (16 chars `a`, Backspace, then `€`: dropped. Then `b`: 16 bytes), `AnEditInTheMiddleKeepsTheRest` (`hello`, Left twice, Backspace, char `L`: `heLlo`), and `TheCaretDrawsAfterARemove` (`abc`, Backspace: the caret rect at (16, 0, 1, 16)).
- [ ] 2.5 Write `tests/BrutalUi.Glfw.Tests/TextFieldKeyCodeTests.cs` with 1 test, `TheTextFieldKeyCodesEqualTheGlfwKeys`: `WidgetInput.BackspaceKey`, `DeleteKey`, `HomeKey`, `EndKey`, `EnterKey`, `LeftKey`, and `RightKey` equal the `GlfwKey` values (copy the shape of `GlfwUiInputKeyTests.TheCoreTabKeyEqualsTheGlfwTabKey`). Read the member names of `GlfwKey` in the binding first. When a name differs (for example `Backspace`), use the binding name and record it in `context.md`.
- [ ] 2.6 Run the build, the tests, the two demo classes, the new Glfw class, and the regression set. Record the counts in `context.md`.

**Estimated Steps:** about 40 (6 tasks, about seven steps for one task)

**Files (6):** `WidgetInput.cs`, `TextField.cs`, `TextFieldCaretTests.cs`, `TextFieldDeleteTests.cs`, `TextFieldKeyCodeTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `TextFieldCaretTests`, the 8 tests of `TextFieldDeleteTests`, and the test of `TextFieldKeyCodeTests` pass.
- [ ] `TextFieldTypeTests` passes with no edit.
- [ ] Each class of the regression set passes with no edit.

---

### Phase 3: Enter Submits and Code Reads and Writes the Text
**Goal:** Enter calls `Submitted`. The game reads the text with `GetText` and writes it with the three `SetText` overloads, with no allocation and with the cut rule at the capacity.
**Demo:** a unit test focuses `Field`, types `ok`, pushes Enter, and runs `Update`. It reads one `Submitted` call with the field as the sender, two `TextChanged` calls, and `GetText` into a stack span of 16 bytes that returns 2 and holds `"ok"u8`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldSubmitTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*TextFieldTextTests'` reads a total of 10 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tier note:** Copy the `SetText` shape of `Button`: compare first, and mark nothing for the same text. The span overload compares the cut prefix with `Text`. The string overload compares with `Utf8Text.Utf8Equals(Text, text)` first, then encodes with `Utf8.FromUtf16(text, _buffer, out _, out int written, replaceInvalidSequences: true, isFinalBlock: true)`. The `LString` overload calls the string overload with `text?.Localized` one time. The field keeps no `LString`, and `RefreshLocalizedText` does not know the field. A set from code calls no delegate (rule 7 of the widget rules).

**Tasks:**
- [ ] 3.1 Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `Submitted` and the Enter branch of `HandleKeyDown` (main Enter only, no repeat, Decision 6). Add `CutAtBoundary`, `GetText(Span<byte>)`, and the three `SetText` overloads (Decision 8). A set that changes the bytes puts the caret at the end and marks `Paint`. The start text of the constructor uses the same string path. Extend the remarks: the Enter rule, the cut rule, "only user input calls `TextChanged`", the span of `Text` is valid until the next change, `SetText("x")` with a literal needs the `u8` form, and the field shows an `LString` as it is at the call only.
- [ ] 3.2 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldSubmitTests.cs` with 6 tests: `EnterCallsSubmitted` (the demo), `EnterStopsTheWalk` (the root bubble handler gets no Enter. The text does not change), `ARepeatOfEnterWalksOn` (an Enter with `IsRepeat`: no call, and the root gets it), `TheKeypadEnterWalksOn` (key 335: no call), `TheModifiersAndEnter` (`Control` with Enter walks on and calls nothing. `Shift` with Enter calls `Submitted`), and `EnterWithNoDelegateStillStops`.
- [ ] 3.3 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldTextTests.cs` with 10 tests: `SetTextCopiesTheBytes` (`SetText("name"u8)`: `Text`, the caret at 4, `Paint` marked, no call), `SetTextEncodesAString` (`SetText((string?)"héllo")`: 6 bytes), `SetTextReadsTheLocalizedText` (an `LString` gives its localized text, and a later locale change does not change the field), `TheSameTextMarksNothing` (after a settle, the same span and the same string mark no `Paint`), `ASetPastTheCapacityCutsAtACodePoint` (capacity 4: `"abc€"u8` gives `abc`, and the string `"ab😀"` gives `ab`), `AnEmptySetClearsTheText` (an empty span and `(string?)null` give 0 bytes and the caret at 0), `GetTextCopiesTheText` (the count and the bytes. A span of 16 bytes for 2 bytes leaves the rest untouched), `GetTextIntoAShortSpanCutsAtACodePoint` (`a€` into 3 bytes gives 1), `TheTextIsUsableAfterASet` (`SetText`, then Left and Backspace and a char edit the new text), and `AnInvalidByteStepsAsOne` (`SetText([0x61, 0xFF, 0x62])`: Left from 3 gives 2, then 1. Backspace removes the byte `0xFF`).
- [ ] 3.4 Run the build, the tests, the two demo classes, the earlier text field classes, and the regression set. Record the counts in `context.md`.
- [ ] 3.5 Record in `context.md`: the cut rule (Notes Q3), the `LString` rule, and the test count of each new class.

**Estimated Steps:** about 32 (5 tasks, about six steps for one task)

**Files (4):** `TextField.cs`, `TextFieldSubmitTests.cs`, `TextFieldTextTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `TextFieldSubmitTests` and the 10 tests of `TextFieldTextTests` pass.
- [ ] The earlier text field classes pass with no edit.
- [ ] Each class of the regression set passes with no edit.

---

### Phase 4: Long Text Scrolls Under the Caret, and the Caret Blinks
**Goal:** The caret pass of `UiContext.Update` scrolls the text of the focused field so the caret stays visible, and it blinks the caret. A focus gain, an edit, and a caret move show the caret at once.
**Demo:** a unit test focuses `Field`, types 12 chars `a`, and runs `Update(0)` and `Draw`. It reads a `Text` call at (-16, 0) with 12 bytes and a caret rect at (80, 0, 1, 16), both after `PushClip(0, 0, 81, 16)` and before its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldScrollTests'` and reads a total of 9 with `failed: 0`, and the same command with `'*TextFieldBlinkTests'` reads a total of 6 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes `UiContext.cs`, the frame order of each context. Change only two places: (1) in `Update`, add the caret pass after the `finally` block of the layout pass and before the clear pass, and (2) in `SetFocusOwner`, call `RestartCaretBlink` on a new owner that is a `TextField`, in the change branch only (not in the repair branch). Edit the remarks of `Update` (the frame order and the caret pass) and of `SetFocusOwner`. Do not change `DrainEvents`, `RoutePointer`, `DropStaleFocus`, the focus rules, the measure scope, or `Draw`. The pass reads `_measurer`, not `t_measurer`. A context with no measurer still blinks and keeps the scroll. Prove it with `ThousandElementAllocationTests`, `FocusAllocationTests`, `RoutingGateTests`, and each focus class.

**Tasks:**
- [ ] 4.1 Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `_scroll`, `_blink`, `TextScroll`, `CaretVisible`, `CaretBlinkInterval`, `AdvanceCaret` (The Caret Pass and the Scroll Rule), and `RestartCaretBlink`. Each path that changes the text or the caret calls `RestartCaretBlink` (Edit Rule 6). `SetText` writes `_scroll = 0`. `PaintContent` uses `x0 = content.X - _scroll` and draws the caret only when `CaretVisible`. Extend the remarks: the caret pass, the scroll rule, the blink, a field that is not the focus owner keeps its scroll, and the next `Update` after a `SetText` scrolls to the caret.
- [ ] 4.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs` by the tier note. Write the caret pass as `if (_focusOwner is TextField field) { field.AdvanceCaret(deltaTime, _measurer); }`, with a comment that names the pass. The doc comment of `Update` names the frame order: the input pass, the style pass, the transition pass, the layout pass, the caret pass, and the clear pass. It states that the caret pass reads `deltaTime` with the rule of the transition pass for a bad value.
- [ ] 4.3 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldScrollTests.cs` with 9 tests: `LongTextScrollsUnderTheCaret` (the demo), `HomeScrollsBackToTheStart` (then Home: the text at (0, 0), the caret at (0, 0, 1, 16)), `AMoveInsideTheViewDoesNotScroll` (after the demo state, Left three times: the text stays at (-16, 0), the caret at (56, 0, 1, 16)), `AMovePastTheLeftEdgeScrolls` (Home, End, then Left to the byte 1: the text at (-8, 0), the caret at (0, 0, 1, 16)), `ARemoveAtTheEndPullsTheTextBack` (12 chars, Backspace: `TextScroll` 8, the text at (-8, 0), the caret at (80, 0, 1, 16)), `TextThatFitsDoesNotScroll` (10 chars: `TextScroll` 0, the caret at (80, 0, 1, 16)), `AResizeKeepsTheCaretVisible` (the demo state, then `Inline.Width = 41` and `Update(0)`: `TextScroll` 56, the caret at (40, 0, 1, 16)), `SetTextAndTheFocusOwnerRule` (`SetText` on the scrolled field gives `TextScroll` 0 at once. A field that is not the focus owner keeps its scroll through an `Update`. The next `Update` with the focus scrolls to the caret at the end), and `ThePaddingAndTheBorderCount` (width 91, height 26, padding 4, and border width 1 on each side: 12 chars give the clip (1, 1, 89, 24), the text at (-11, 5), and the caret at (85, 5, 1, 16)).
- [ ] 4.4 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldBlinkTests.cs` with 6 tests: `TheCaretBlinks` (focus, `Update(0)`: a caret rect. `Update(0.5f)`: no caret rect. `Update(0.5f)`: a caret rect), `AnEditRestartsTheBlink` (hidden after `Update(0.5f)`, then a char and `Update(0)`: a caret rect), `ACaretMoveRestartsTheBlink` (the same with Left), `AFocusGainRestartsTheBlink` (hidden after `Update(0.7f)`, `ClearFocus`, `SetFocus(Field)`, `Update(0)`: a caret rect. A Tab from `Other` to the field gives the same), `ABadDeltaAdvancesNothing` (NaN, -1, and positive infinity keep `CaretVisible` `true`), and `AContextWithNoMeasurerStillBlinks` (a field in a `UiContext()` with no measurer: `Update` does not throw, `TextScroll` stays 0 after 12 chars, and the blink still flips).
- [ ] 4.5 Run the build, the tests, the two demo classes, each earlier text field class, and the regression set. Run `git diff <base> -- src/BrutalUi.Core/Tree/UiContext.cs` and check that only the two places and doc comments change. Record the diff summary in `context.md`.
- [ ] 4.6 Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. The HelloPanel smoke run holds no field, so it must not change. Record the counts in `context.md`.

**Estimated Steps:** about 42 (6 tasks, about seven steps for one task)

**Files (5):** `TextField.cs`, `UiContext.cs`, `TextFieldScrollTests.cs`, `TextFieldBlinkTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `TextFieldScrollTests` and the 6 tests of `TextFieldBlinkTests` pass.
- [ ] The earlier text field classes pass with no edit. The phase 2 caret tests run `Update(0)`, so the caret stays shown.
- [ ] In `UiContext.cs`, only the caret pass in `Update`, the restart line in `SetFocusOwner`, and doc comments change.
- [ ] `ThousandElementAllocationTests`, `FocusAllocationTests`, and each focus class pass with no edit.

---

### Phase 5: Shift Selects, and Typing Replaces the Selection
**Goal:** Shift with Left, Right, Home, or End extends a selection from the anchor. A char replaces the selection. Backspace and Delete remove it. A move with no Shift collapses it. The focused field draws the selection behind the text.
**Demo:** a unit test creates the field with the start text `hello`, focuses it, pushes Shift with Left two times, and runs `Update` and `Draw`. It reads a selection rect at (24, 0, 16, 16) in `Style.Color.ScaleAlpha(SelectionAlpha)` before the `Text` call. It then pushes the char `p` and reads `Text` as `"help"u8`, one `TextChanged` call, the caret at 4, and no selection rect. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldSelectionTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase adds `_anchor` to each path of the buffer. Each path that writes `_caret` decides the anchor by Edit Rule 3: `Insert`, `Remove`, `MoveCaret(index, extend)`, `SetText`, and the constructor. The fit check of a char uses the length minus the selection, and a dropped char keeps the selection (Edit Rule 2). Left and Right with a selection and no Shift collapse with no step. The scroll rule follows the caret, not the anchor. The selection draws only with `Focus`.

**Tasks:**
- [ ] 5.1 Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `_anchor`, `AnchorIndex`, `SelectionAlpha`, the Shift branch of Left, Right, Home, and End, the collapse rule, the selection remove of Backspace and Delete, the selection replace of `HandleChar`, and step 3 of The Draw. `SetText` and the constructor collapse the anchor to the caret. Extend the remarks: the selection rule, the replace rule with the fit check, the collapse rule, and the selection color (`Style.Color` at 35 % of its alpha, Conflicts and Gaps item 6).
- [ ] 5.2 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldSelectionTests.cs` with 10 tests: `ACharReplacesTheSelection` (the demo), `ShiftHomeAndShiftEndSelect` (`hello`, Shift+Home: the selection 0 to 5. End, Left twice, Shift+End: 3 to 5), `LeftAndRightCollapseTheSelection` (with the selection 1 to 3: Left gives the caret 1 and no selection. Again with the selection: Right gives 3), `HomeAndEndCollapse`, `BackspaceAndDeleteRemoveTheSelection` (each removes the selection with one call, and the caret goes to the start), `ADroppedCharKeepsTheSelection` (16 chars `a`, Shift+Left, `€`: no change, no call, and the selection stays. Then `b`: the last byte becomes `b`), `TheSelectionFlipsOverTheAnchor` (the caret at 3, Shift+Right two times, Shift+Left four times: the anchor 3, the caret 1, the selection rect at (8, 0, 16, 16)), `TheSelectionDrawsBehindTheTextOnlyWithFocus` (the selection rect comes before the `Text` call. With no focus, no selection rect and no caret rect), `TheSelectionFollowsTheScroll` (12 chars, Shift+Left two times: the selection rect at (64, 0, 16, 16) with the text at (-16, 0)), and `SetTextClearsTheSelection` (a selection, then `SetText("new"u8)`: the anchor and the caret at 3, no selection rect).
- [ ] 5.3 Run the build, the tests, the demo class, each earlier text field class, and the regression set. Record the counts in `context.md`.
- [ ] 5.4 Record in `context.md`: the collapse rule, the selection color, and each decision that this plan did not settle.

**Estimated Steps:** about 26 (4 tasks, about six steps for one task)

**Files (3):** `TextField.cs`, `TextFieldSelectionTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `TextFieldSelectionTests` pass.
- [ ] The earlier text field classes pass with no edit. The phase 2 tests send no Shift, so they keep their result.
- [ ] Each class of the regression set passes with no edit.

---

### Phase 6: A Press Places the Caret, and a Drag Selects
**Goal:** A left press on the field places the caret at the nearest code point boundary under the pointer. A drag with the left button moves the caret and keeps the anchor at the press point, so the drag selects. Shift with a press keeps the old anchor and extends the selection.
**Demo:** a unit test creates the field with the start text `hello`, pushes a left press at (21, 8) and a move to (37, 8), and runs `Update` and `Draw`. It reads `FocusedElement` as the field, the anchor at 3, the caret at 5, a selection rect at (24, 0, 16, 16), and no `TextChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldPointerTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Tier note:** A handler has no measurer during the drain. Thus the press handler and the move handler store the pointer x in text space (`_pendingX`: the pointer x minus `WidgetInput.ViewportOrigin(this).X`, minus the left border and padding, plus `_scroll`) and the extend flag (`_pendingExtend`). The caret pass maps `_pendingX` with `IndexAtX` before the scroll rule, writes the caret through `MoveCaret(index, extend)`, restarts the blink, and sets `_pendingX` to NaN. A press without Shift collapses the anchor to the new caret. A drag move always extends. Use `WidgetInput.IsDragStart` and `WidgetInput.IsDragMove`, as `Slider` does, so a right press and a chord place nothing. Implement `IDragCapture` explicitly, so the drag goes on outside the rect. The click focus of the drain still gives the focus, and the caret pass runs for the new owner in the same `Update`. A context with no measurer drops `_pendingX` and keeps the caret. A double press, a word select, and a drag scroll timer are out of scope. Add no allocation: `IndexAtX` measures each prefix with the span of the buffer.

**Tasks:**
- [ ] 6.1 Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `_pendingX`, `_pendingExtend`, `IndexAtX`, the static `HandlePointerDown` and `HandlePointerMove` with their cached `static readonly UiEventHandler<PointerEvent>` fields, and the explicit `IDragCapture`. The constructor writes `OnPointerDown` and `OnPointerMove`. `AdvanceCaret` maps a pending x before the scroll rule. Extend the remarks: the press rule, the drag rule, the Shift rule, the pending map of the caret pass, the capture, "a write of `=` to `OnPointerDown` removes the press", and the no-measurer rule.
- [ ] 6.2 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldPointerTests.cs` with 10 tests: `APressAndADragSelect` (the demo), `APressPlacesTheCaretAtTheNearestBoundary` (`hello`: a press at x 11 gives 1, at x 13 gives 2, and at x 12, a tie, gives 1), `APressOutsideTheTextGoesToAnEnd` (a press at x 80 gives 5, and a press at x 0 gives 0), `APressStepsOverAWholeCodePoint` (`a€b`: the boundaries 0, 1, 4, and 5 at x 0, 8, 32, and 40. A press at x 19 gives 1, and a press at x 21 gives 4), `ShiftWithAPressExtendsTheSelection` (Home, Right, then a Shift press at x 37: the anchor 1, the caret 5), `APressInAScrolledFieldReadsTheScroll` (12 chars, `TextScroll` 16, a press at x 3: the caret at 2, and `TextScroll` stays 16), `ADragPastTheRightEdgeScrolls` (12 chars, Home, `Update(0)`, a press at x 2 and a move to x 200: the anchor 0, the caret 12, and `TextScroll` 16), `APressFocusesTheFieldAndShowsTheCaret` (`Other` holds the focus, then a press on the field: `FocusedElement` is the field, the caret moves, and `CaretVisible` is `true` after `Update(0)`), `ARightPressAndAChordPlaceNothing` (a right press and a left press during a right press keep the caret), and `AContextWithNoMeasurerKeepsTheCaret` (a field in a `UiContext()` with no measurer: a press does not throw and keeps the caret).
- [ ] 6.3 Run the build, the tests, the demo class, each earlier text field class, and the regression set. Record the counts in `context.md`.
- [ ] 6.4 Record in `context.md`: the pending map, the tie rule, the Shift rule, and each decision that this plan did not settle.

**Estimated Steps:** about 26 (4 tasks, about six steps for one task)

**Files (3):** `TextField.cs`, `TextFieldPointerTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `TextFieldPointerTests` pass.
- [ ] The earlier text field classes pass with no edit.
- [ ] Each class of the regression set passes with no edit. `SliderDragTests` and `ScrollViewDragTests` also pass with no edit.

---

### Phase 7: Disabled Fields, the Allocation Gate, and the Record
**Goal:** Prove that a disabled field takes no input. Prove 0 bytes for `Update`, `Draw`, and the full text field input path after warm-up. Record the text field rules and the caret pass in the technical design and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldAllocationTests'` and reads a total of 3 with `failed: 0`, and the same command with `'*TextFieldDisabledTests'` reads a total of 5 with `failed: 0`. A planted allocation at the start of `TextField.HandleChar` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 6
**Agent tier:** STANDARD (sonnet)

**Tier note:** Copy the gate shape of `ScrollViewAllocationTests` and `SliderAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no new `string`, and no `Assert`. Write each text as a `u8` literal or a static `byte[]`. Do not add warm-up rounds to hide an allocation. Do not run a plant and its restore in one parallel tool block (a gotcha of button-and-toggle).

**Tasks:**
- [ ] 7.1 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldDisabledTests.cs` with 5 tests: `ADisabledFieldTakesNoChar` (a char with the disabled field as the caller target: no change, no call, and the root bubble handler gets it), `ADisabledFieldTakesNoKey` (Left, Backspace, and Enter with the field as the caller target: no change, no call, and the root gets each key), `ADisableDropsTheFocusAndTheCaret` (focus, `States |= Disabled`, `Update(0)`: `FocusedElement` is null and the frame has no caret rect), `APressOnADisabledFieldGivesNoFocus` (also no caret move and no selection), and `ACodeSetStillWorks` (`SetText` on a disabled field changes the text and calls nothing).
- [ ] 7.2 Write `tests/BrutalUi.Core.Tests/Elements/TextFieldAllocationTests.cs` with 3 tests. The scene: a column root with 20 fields (81 x 16, capacity 32, the start text `field`, `FontSize` 16), the last one in `Disabled`, and a sheet with `TextField`, `TextField:focus`, and `:disabled` rules. The first field holds the focus. `UpdateAndDrawAllocateNothingWithTextFields` (100 rounds of `Update(0.016f)` and `Draw`, so the caret pass and the blink run). `TheTextFieldInputPathAllocatesNothing` (each step on the first field: `SetText("seed"u8)`, the chars `a`, `b`, and `€`, Left, Shift+Left, the char `c`, Backspace, Delete, Home, Shift+End, Delete, End, a left press, a drag move, and its release, Enter, and a char past the capacity on a full second field set with `SetText`, then `GetText` into a stack span, `Update(0.016f)`, and `Draw`). `TheMeasuredBodyDoesRealWork` (after one step: the `TextChanged` count, one `Submitted` call, the expected text, 19 text draws, and one caret rect or none by the blink phase, with the blink phase read from `CaretVisible`).
- [ ] 7.3 Copy `src/BrutalUi.Core/Elements/TextField.cs` to the scratchpad. Plant `GC.KeepAlive(new object());` as the first line of `TextField.HandleChar`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, in a later tool call, and check it with `cmp`. Record the result and the gates that failed in `context.md`.
- [ ] 7.4 Edit `docs/technical-design.md` section 4 ("Frame order": the caret pass after the layout pass) and section 5: a "TextField" paragraph after the scroll view paragraph (the buffer rule, the char rule and the drop, the key rule, the selection, the press and drag rule, the Enter rule, the cut rule of `SetText`, `GetText`, the clip, the caret pass, the scroll rule, the blink, the disabled rule, `TextChanged`, and `Submitted`). Edit rule 5 of the widget rules (`WidgetInput` also holds `IsCharInput` and `IsEditKey`).
- [ ] 7.5 Edit `docs/technical-design.md` section 8 ("The paint hook": a `TextField` draws its selection, its text, and its caret, and it measures with the renderer of the draw), section 9, item 7 of the flow (a focused `TextField` takes the char and the edit keys, and Tab walks on), and section 14 (the text field test classes and the twelfth gate with the plant). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`TextField`) and "Changed" (`UiContext.Update` runs the caret pass for a focused `TextField`).
- [ ] 7.6 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
- [ ] 7.7 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.

**Estimated Steps:** about 44 (7 tasks, about six steps for one task)

**Files (6):** `TextFieldDisabledTests.cs`, `TextFieldAllocationTests.cs`, `TextField.cs` (planted and restored), `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 5 tests of `TextFieldDisabledTests` and the 3 tests of `TextFieldAllocationTests` pass.
- [ ] The planted allocation fails the run, and `cmp` finds no difference after the restore.
- [ ] `scripts/test-linux.sh` prints its PASS line.

---

## Key Technical Decisions

### Confirmed Assumptions

None (a draft with no interview).

### Decision 1: A sealed `TextField` on the widget pattern
**Choice:** `TextField` derives from `Element` directly and follows the ten widget rules of section 5. The new checks `IsCharInput` and `IsEditKey` go into `WidgetInput`, next to `IsArrowKey`. The handlers are static methods with no target, cached in `static readonly` fields, and they cast `e.Header.CurrentTarget`.
**Rationale:** Epic A2 and Decision 1 of button-and-toggle. One place holds the disabled rule of each check. The handler shape is the shape of each sibling, so the constructor allocates no delegate.
**Alternatives:** (a) Private checks in `TextField`: the disabled rule drifts. (b) A delegate with the field as the target: one delegate allocation for each field, and a second handler shape in the epic.

### Decision 2: One fixed byte buffer with a required capacity (A5)
**Choice:** `CreateInfo.Capacity` is `required` and counts bytes. The constructor allocates `new byte[Capacity]` one time. A capacity of 0 or less throws `ArgumentOutOfRangeException` with `ParamName` `info`.
**Rationale:** A5: "the buffer never grows during typing". The capacity has no safe default, so it is `required` (rule 2 of the widget rules). Bytes match the UTF-8 buffer and a network or save limit. `Utf8Text` doubles its buffer, so the field cannot reuse it (Finding 9).
**Alternatives:** (a) A capacity in code points: the byte size is up to four times larger, and the fit check needs a count. (b) A default capacity: a silent limit that a game finds late. (c) `Utf8Text`: it grows, against A5.

### Decision 3: The caret and the anchor are byte offsets on code point boundaries
**Choice:** `_caret` and `_anchor` are byte offsets. `NextBoundary` and `PreviousBoundary` step with `Rune.DecodeFromUtf8` and `Rune.DecodeLastFromUtf8`. An invalid sequence is one step of at least 1 byte. The unit is the code point, not the grapheme cluster.
**Rationale:** The requirement says "a code point". A byte offset reads and slices the buffer with no search. `Rune` decodes with no allocation and never returns a step of 0, so a bad byte from `SetText(ReadOnlySpan<byte>)` cannot trap the caret.
**Alternatives:** (a) A code point index: each draw and each edit must convert it to bytes. (b) Grapheme clusters through `StringInfo`: it needs a string, so it allocates. (c) A UTF-8 check in `SetText`: a cost for each set, and `Label` checks nothing either.

### Decision 4: The char rule: a fit check before each change
**Choice:** Edit Rule 2. A control character or an invalid scalar walks on. Else the field stops the walk. When the new length passes the capacity, the field changes nothing and calls nothing. Else it replaces the selection and calls `TextChanged`.
**Rationale:** The requirement: "an insert past the capacity drops the input and raises nothing". The check before the remove keeps the selection of a dropped char, so the player loses no text. The field uses the char also when it drops it, so the stop keeps a game shortcut from firing on a typed letter. A control character is not text, so it walks on.
**Alternatives:** (a) Insert the part that fits: a code point cannot split. (b) Remove the selection and then drop: the player loses text. (c) No stop on a drop: a letter in a full field reaches a game handler.

### Decision 5: The key rule
**Choice:** Edit Rule 3. `IsEditKey` passes a repeat and `Shift`, and it blocks `Control`, `Alt`, and `Super`. Each listed key stops the walk, also when nothing changes. Tab, Up, Down, Escape, the keypad Enter, and the other keys walk on.
**Rationale:** The requirement lists the arrow keys, Home, End, Backspace, and Delete. A held key repeats, as in each text box. The stop at an end copies Decision 8 of slider. Tab must walk on, so the focus moves (Finding 2). The modifier rule copies `IsArrowKey`, and it leaves `Control` shortcuts (copy, word jump) to a later feature.
**Alternatives:** (a) A stop only on a change: a Left at 0 reaches a game handler. (b) Take Up and Down as Home and End: no requirement asks for it.

### Decision 6: Enter calls `Submitted`
**Choice:** The main Enter key (`WidgetInput.EnterKey`, 257), with no repeat, stops the walk and calls `Submitted` with the field as the sender. `Shift` does not block it. The keypad Enter key walks on. The field does not lose the focus.
**Rationale:** The requirement: "a submit on Enter goes to a second slot". The repeat rule and the keypad rule copy `Button` (`IsKeyActivation`), so the epic has one Enter rule. The game decides what a submit does, also a `ClearFocus`.
**Alternatives:** (a) The keypad Enter key too: a second Enter rule in the epic (Conflicts and Gaps item 5). (b) A submit that clears the focus: a game that keeps a chat field open must focus it again.

### Decision 7: Only user input calls `TextChanged`
**Choice:** A char and a remove that change the bytes call `TextChanged` one time, after the change and after the stop. A caret move, a selection change, a dropped char, `SetText`, and the start text call nothing.
**Rationale:** Rule 7 of the widget rules, Decision 5 of slider, and epic Q3. A store binding does not echo its own set.
**Alternatives:** A call for each code set: an echo loop for a two-way binding.

### Decision 8: `SetText` and `GetText` copy with a cut at a code point (Notes Q3)
**Choice:** The three `SetText` overloads copy the longest prefix that fits the capacity and ends on a boundary. The same text marks nothing. A new text puts the caret and the anchor at the end, writes the scroll 0, and marks `Paint`. `SetText(LString?)` reads `Localized` one time and keeps no `LString`. `GetText(Span<byte>)` copies the longest prefix that fits the destination and ends on a boundary, and it returns the byte count. `Text` gives the bytes with no copy.
**Rationale:** The requirement names the `Label` overloads and no allocation. A cut keeps the buffer valid with no exception in a hot path. An editable text cannot follow a locale refresh, because the refresh would overwrite the edits of the player. `Utf8.FromUtf16` writes whole scalars only, so the string path cuts on a boundary for free.
**Alternatives:** Notes Q3, options B and C.

### Decision 9: The field clips through the inline overflow
**Choice:** The constructor sets `Inline.Overflow = Overflow.Hidden`. `PaintContent` draws the text at `content.X - TextScroll`.
**Rationale:** The walk pushes the clip of the padding box before `PaintContent` (Finding 3), so the walks do not change. The requirement asks for the padding box. `Hidden` keeps the layout of `Visible`, so the field size does not change. An inline value wins over each rule, as Decision 6 of scroll-view.
**Alternatives:** (a) A `ScrollView` inside the field: a child element, a second offset, and a wheel that scrolls the text. (b) A `PushClip` in `PaintContent`: the hook must pop it, and the walks own the clip stack.

### Decision 10: The caret pass of `UiContext.Update`
**Choice:** `Update` calls `AdvanceCaret(deltaTime, _measurer)` on the focus owner when it is a `TextField`, after the layout pass and before the clear pass. The pass advances the blink and writes the text scroll.
**Rationale:** The draw reads the tree only (Finding 4), so it cannot write the scroll. A handler has no measurer and no new layout during the drain (Finding 5). After the layout pass, the text, the style, and the size of the frame are final, so one pass gives the right scroll also after a resize. Only the focus owner shows a caret, so one type test is the full cost for each frame. The type test copies Decision 3 of scroll-view.
**Alternatives:** (a) A scroll with no state at draw time: the caret sticks to the right edge, and the text jumps on each Left key. (b) A measurer scope for the drain: a change of the drain, and the layout of the frame is still old. (c) An internal virtual tick on `Element`: a call for each element of each frame. (d) A list of fields in the context: a registration on attach and detach.

### Decision 11: The caret blinks on the timer of the caret pass
**Choice:** `CaretBlinkInterval` is 0.5 s. The caret shows while the timer is below 0.5 s, in a cycle of 1 s. The timer restarts on each change of the text or the caret, and on each focus gain through one line in `SetFocusOwner`. A `deltaTime` that is not finite, or 0 or less, advances nothing.
**Rationale:** The requirement: "the caret blinks on a timer that `Update` advances". A restart shows the caret at once after each keystroke, as in each desktop text box. `SetFocusOwner` is the one funnel of each focus change (Finding 7). The bad-value rule matches the transition pass in spirit, and an infinite step cannot hide the caret for good.
**Alternatives:** (a) A blink interval in the create-info: no requirement asks for it. (b) A restart in the caret pass by a stored last owner: the context keeps a reference to a removed field.

### Decision 12: The scroll rule moves the text by the smallest distance
**Choice:** The Caret Pass and the Scroll Rule. The caret stays in `[0, width - CaretWidth]` of the content box, and the scroll stays in `[0, max(0, textWidth + CaretWidth - width)]`.
**Rationale:** The requirement: "scrolls the text so the caret stays visible". The smallest move keeps the text still while the caret moves inside the view. The upper clamp pulls the text back after a remove at the end, so no empty space shows past the text.
**Alternatives:** (a) Center the caret: the text jumps at each edge. (b) Scroll by a page: the caret jumps.

### Decision 13: The draw order is selection, text, and caret
**Choice:** The Draw. The selection rect uses `Style.Color` at `SelectionAlpha` (0.35) of the opacity product. The caret is a rect of 1 pixel by the line height in `Style.Color`. The selection and the caret draw only when the field holds `Focus`.
**Rationale:** The selection under the text keeps the glyphs readable. The style system has no selection color, and a new style member changes 32-member tables in three projects. The `color` of the style gives `TextField:focus` and `:disabled` rules a hook with no new member. Each measure call takes a span and allocates nothing.
**Alternatives:** (a) A new style member for the selection: a style-system change (Conflicts and Gaps item 6). (b) A selection color in the create-info: a second color source that a stylesheet cannot set.

### Decision 14: A disabled field takes no input and keeps its text
**Choice:** `IsCharInput` and `IsEditKey` return `false` for `Disabled`, so the handlers return with no stop. A disabled field does not accept the focus (`AcceptsFocus`), so `DropStaleFocus` drops it at the next drain, and it draws no caret. A set from code still works.
**Rationale:** Decision 6 of button-and-toggle and Decision 11 of slider. The existing focus rules do the rest, with no new code.
**Alternatives:** A disabled field that keeps the focus: it takes the keys of the player and does nothing.

### Decision 15: `TextField` has no own size (Notes Q4)
**Choice:** No measure callback and no inline size. The remarks advise a width and a height of at least one line.
**Rationale:** Decision 10 of slider and Decision 13 of scroll-view. The field needs no measurer in the layout pass, so a context with no measurer can hold a field. widget-sample adds the size rules.
**Alternatives:** Notes Q4, option B.

### Decision 16: A press places the caret, and a drag selects (Notes Q2)
**Choice:** A left press stores a pending x. The caret pass maps it to the nearest code point boundary with the measurer. A drag through `IDragCapture` moves the caret and keeps the anchor, so it selects. Shift with a press extends the selection.
**Rationale:** The user chose option C. A handler has no measurer during the drain, so the map waits for the caret pass, which already reads `_measurer` (Decision 10).
**Alternatives:** Notes Q2, options A and B.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The caret pass breaks the frame order or an existing gate. | Low | High | Two places in `UiContext.cs` only (tier note of phase 4). The pass runs for a focused `TextField` only. `ThousandElementAllocationTests`, `FocusAllocationTests`, `RoutingGateTests`, and each focus class run with no edit. |
| An insert or a remove splits a code point. | Medium | High | Each path steps through `NextBoundary` and `PreviousBoundary` (Decision 3). `TheCaretStepsOverEachCodePoint`, `BackspaceRemovesAWholeCodePoint`, and `AnInvalidByteStepsAsOne` pin it. |
| A dropped char changes the text or the selection. | Medium | Medium | The fit check comes before each change (Decision 4). `AnInsertPastTheCapacityDrops`, `ACodePointThatDoesNotFitDrops`, and `ADroppedCharKeepsTheSelection` pin it. |
| A path allocates (a string in `SetText`, a delegate, a `Rune` array). | Low | High | `Utf8.FromUtf16` into the buffer, `stackalloc byte[4]`, and cached delegates. `TextFieldAllocationTests` and the plant of task 6.3. |
| The caret sits up to 1 pixel off the glyph edge with `ImGuiRenderer`. | Medium | Low | `ImGuiRenderer` rounds each width up to a whole pixel, so the width of a prefix can differ from the glyph position. widget-sample checks the caret on screen. |
| The caret pass measures text in each frame. | High | Low | Two measure calls for one field (the focus owner). The draw adds one call for the caret and two for a selection, for the focused field only. |
| A code point is not a grapheme: an emoji sequence or a combining mark needs more than one Backspace. | High | Low | The requirement names the code point. The remarks state it. Conflicts and Gaps item 4. |
| A field that is not the focus owner shows an old scroll after a resize or a style change. | Low | Low | The next focus gain runs the caret pass. `SetText` writes the scroll 0. The remarks state it. |
| A set past the capacity cuts the text with no signal. | Medium | Medium | Notes Q3. The remarks state the cut. `Capacity` and `Text.Length` let the game check. |
| A field with no size takes no space and catches no press. | Medium | Low | Decision 15 and Notes Q4. The remarks state it. widget-sample adds the size rules. |
| A test writes `SetText("x")` and the analyzer rejects it. | Medium | Low | The phase intro asks for the `u8` form (Finding 11). |
| ImGui and the UI both take the keys. | Medium | Medium | Section 9 item 7: the game sets `input.KeyboardEnabled = !io.WantCaptureKeyboard`. widget-sample shows it. |
| A Glfw binding name differs from the guess in task 2.5. | Medium | Low | The task reads the names in the binding first and records each change. |

---

## Dependencies

### Internal Dependencies
- `interactive-elements/scroll-view`: the clip order of the draw walk (risk 12 of its review) and the gate shape of `ScrollViewAllocationTests`. The caller commits scroll-view before this feature starts. Task 1.1 checks it.
- `interactive-elements/slider`: `IsArrowKey` (the shape of `IsEditKey`), `LeftKey`, `RightKey`, and the key stop rule (Decision 8 of slider).
- `interactive-elements/button-and-toggle`: the widget pattern, `WidgetInput`, `EnterKey`, the `SetText` shape of `Button`, and the delegate slot shape (`21abb3c`).
- `input-routing`: `CharEvent`, `KeyEvent`, `OnChar`, `OnKeyDown`, the focus owner, the key and char route of `DrainEvents`, `SetFocus`, `SetFocusOwner`, `DropStaleFocus`, Tab, and `PointerTree.Char` and `PointerTree.Key`.
- `core-tree` and the layout engine: `Element`, `ElementType.Intern`, `MarkDirty`, `LayoutNode.Rect`, `LayoutNode.ContentBox`, `InlineStyle.Overflow`, and `Overflow.Hidden`.
- `imgui-renderer`: `IUiRenderer.MeasureText`, `Text`, `Rect`, and `RecordingRenderer` (8 pixels for each byte at size 16).
- `style-system`: the type selector, `Style.Color`, `Style.Font`, `Style.FontSize`, and `PseudoStates.Focus` and `Disabled`.
- `Utf8Text.Utf8Equals` (internal static, no change).

### External Dependencies
- `System.Text.Rune` and `System.Text.Unicode.Utf8` of .NET 10. No new package and no native dependency.
- The GLFW key values: `GLFW_KEY_ENTER` 257, `GLFW_KEY_BACKSPACE` 259, `GLFW_KEY_DELETE` 261, `GLFW_KEY_RIGHT` 262, `GLFW_KEY_LEFT` 263, `GLFW_KEY_HOME` 268, `GLFW_KEY_END` 269, and `GLFW_KEY_KP_ENTER` 335 ([GLFW keyboard keys](https://www.glfw.org/docs/latest/group__keys.html)). Task 2.5 checks them against the `GlfwKey` enum of the pinned binding.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. Two chars `H` and `i` to the focused `Field` of `TextFieldTree`, pushed with no target and followed by `Update` and `Draw`, give `Text` `"Hi"u8`, two `TextChanged` calls with the field as the sender, and a `Text` call `"Hi"` at (0, 0) after `PushClip(0, 0, 81, 16)` and before its `PopClip` (`TextFieldTypeTests.ACharShowsInTheField`).
- [ ] F2. The start text, a multi-byte code point, the capacity drop, a control char, the stop of the walk, a caller target, the create-info checks, the type id, the inline overflow, and a type rule hold (`TextFieldTypeTests`, 10 tests).
- [ ] F3. Left, Right, Home, and End move the caret by code point and stop at each end. Each used key stops the walk. Tab and the other keys walk on, and Tab moves the focus. `Control`, `Alt`, and `Super` block a key. A repeat moves again. A move calls nothing. The caret draws with the focus only. The key codes are the GLFW values (`TextFieldCaretTests`, 10 tests, and `TextFieldKeyCodeTests`, 1 test).
- [ ] F4. Backspace and Delete remove one whole code point, change nothing at an end, repeat, and make room for an insert (`TextFieldDeleteTests`, 8 tests).
- [ ] F5. Enter calls `Submitted` one time with the field as the sender and stops the walk. A repeat, the keypad Enter, and `Control` walk on. `Shift` submits (`TextFieldSubmitTests`, 6 tests).
- [ ] F6. The three `SetText` overloads copy, cut at a code point at the capacity, mark nothing for the same text, and call nothing. `GetText` copies and cuts at a code point. An invalid byte steps as one (`TextFieldTextTests`, 10 tests).
- [ ] F7. After `Update`, the caret of the focused field lies inside the content box. The text scrolls by the smallest distance, comes back after a remove at the end, and follows a resize, the padding, and the border (`TextFieldScrollTests`, 9 tests).
- [ ] F8. The caret blinks with a cycle of 1 s, restarts on an edit, a move, and a focus gain, ignores a bad delta, and blinks in a context with no measurer (`TextFieldBlinkTests`, 6 tests).
- [ ] F9. Shift with a move selects. A char replaces the selection, and a dropped char keeps it. Backspace and Delete remove it. A move with no Shift collapses it. The selection draws behind the text with the focus only and follows the scroll (`TextFieldSelectionTests`, 10 tests).
- [ ] F10. A disabled field takes no char and no key, does not stop the walk, loses the focus at the next `Update`, draws no caret, and takes a set from code (`TextFieldDisabledTests`, 5 tests).
- [ ] F11. `TextFieldAllocationTests` measures 0 bytes for `Update` and `Draw` and for the full input path, with the press and the drag (3 tests). A planted allocation in `TextField.HandleChar` gives a non-zero exit code of `dotnet test`.
- [ ] F12. A left press places the caret at the nearest code point boundary, with a tie to the lower index. A drag selects and scrolls. Shift with a press extends. A right press and a chord place nothing. A context with no measurer keeps the caret (`TextFieldPointerTests`, 10 tests).

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` has 87 more tests than the count of task 1.1 (10 + 10 + 8 + 6 + 10 + 9 + 6 + 10 + 10 + 5 + 3). `BrutalUi.Glfw.Tests` has 1 more.
- [ ] Q3. `TextField` is `public sealed`. `WidgetInput` stays internal. `TextField.CreateInfo.Capacity` is the only `required` member. `TextChanged` and `Submitted` are `Action<TextField>?`.
- [ ] Q4. `TextField.cs` holds four `static readonly UiEventHandler` fields, no lambda, and exactly one `new byte[`.
- [ ] Q5. No existing test file has a diff against `<base>`.
- [ ] Q6. The changed docs and the new doc comments use ASD-STE100 prose. The lint gives no score above 2.5.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no ImGui or GLFW `using`, and no `.csproj` has a diff.
- [ ] I2. These paths have no diff against `<base>`: `Element.cs`, `PseudoStates.cs`, `LayoutNode.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `FocusWalk.cs`, `HitWalk.cs`, `CharEvent.cs`, `KeyEvent.cs`, `IDragCapture.cs`, `PaintWalk.cs`, `IUiRenderer.cs`, `src/BrutalUi.Core/Layout/`, `src/BrutalUi.Core/Style/`, `Utf8Text.cs`, `Label.cs`, `Image.cs`, `Button.cs`, `Toggle.cs`, `Slider.cs`, `ScrollView.cs`, `Directory.Packages.props`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Glfw/`, `samples/`, and `scripts/`.
- [ ] I3. In `src/`, the feature adds `TextField.cs` and changes `WidgetInput.cs` and `UiContext.cs` only. In `UiContext.cs`, only the caret pass in `Update`, one restart line in `SetFocusOwner`, and doc comments change.
- [ ] I4. Section 4 of `docs/technical-design.md` names the caret pass. Section 5 holds the text field paragraph and the two new checks in rule 5. Section 8 ("The paint hook") and section 9 (item 7 of the flow) name the field. Section 14 names the text field classes and the twelfth gate. `CHANGELOG.md` names `TextField` and the caret pass.
- [ ] I5. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` from `docs/features/interactive-elements/text-field/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the Core total and the Glfw total with the counts of task 1.1 in `context.md` (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `TextFieldTypeTests` 10 (F1, F2), `TextFieldCaretTests` 10 (F3), `TextFieldDeleteTests` 8 (F4), `TextFieldSubmitTests` 6 (F5), `TextFieldTextTests` 10 (F6), `TextFieldScrollTests` 9 (F7), `TextFieldBlinkTests` 6 (F8), `TextFieldSelectionTests` 10 (F9), `TextFieldPointerTests` 10 (F12), `TextFieldDisabledTests` 5 (F10), and `TextFieldAllocationTests` 3 (F11). Run `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*TextFieldKeyCodeTests' 2>&1 | tail -n 5` and expect a total of 1 with `failed: 0` (F3). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Core/Elements/TextField.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `HandleChar`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file from the copy, and check it with `cmp`. Run step 2 again (F11).
5. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no line (Q5).
6. `git diff <base> --stat -- src/BrutalUi.Core/Tree/Element.cs src/BrutalUi.Core/Tree/PseudoStates.cs src/BrutalUi.Core/Tree/LayoutNode.cs src/BrutalUi.Core/Input/ElementHandlers.cs src/BrutalUi.Core/Input/EventWalk.cs src/BrutalUi.Core/Input/FocusWalk.cs src/BrutalUi.Core/Input/HitWalk.cs src/BrutalUi.Core/Input/CharEvent.cs src/BrutalUi.Core/Input/KeyEvent.cs src/BrutalUi.Core/Input/IDragCapture.cs src/BrutalUi.Core/Rendering/PaintWalk.cs src/BrutalUi.Core/Rendering/IUiRenderer.cs src/BrutalUi.Core/Layout/ src/BrutalUi.Core/Style/ src/BrutalUi.Core/Elements/Utf8Text.cs src/BrutalUi.Core/Elements/Label.cs src/BrutalUi.Core/Elements/Image.cs src/BrutalUi.Core/Elements/Button.cs src/BrutalUi.Core/Elements/Toggle.cs src/BrutalUi.Core/Elements/Slider.cs src/BrutalUi.Core/Elements/ScrollView.cs src/BrutalUi.ImGui/ src/BrutalUi.Glfw/ Directory.Packages.props '*.csproj' samples/ scripts/`. Expect no output (I1, I2). When a path of this list does not exist, find the file with `git ls-files` and use its path.
7. `git diff <base> --name-status -- src/` and `git status --porcelain -- src/`. Together they list only `src/BrutalUi.Core/Elements/TextField.cs` (added or untracked) and `WidgetInput.cs` and `UiContext.cs` (modified). Read `git diff <base> -- src/BrutalUi.Core/Tree/UiContext.cs`, and expect the parts of I3 only.
8. `grep -n '^public sealed class TextField : Element$' src/BrutalUi.Core/Elements/TextField.cs` gives one line. `grep -c 'static readonly UiEventHandler' src/BrutalUi.Core/Elements/TextField.cs` gives 4. `grep -c 'new byte\[' src/BrutalUi.Core/Elements/TextField.cs` gives 1. `grep -nE '\(ref [A-Za-z]+ e\) =>|new UiEventHandler' src/BrutalUi.Core/Elements/TextField.cs` gives no line. `grep -nE '^\s*public required' src/BrutalUi.Core/Elements/TextField.cs` gives one line with `int Capacity`. `grep -n 'Inline.Overflow = Overflow.Hidden' src/BrutalUi.Core/Elements/TextField.cs` gives one line. `grep -nE 'public Action<TextField>\? (TextChanged|Submitted)' src/BrutalUi.Core/Elements/TextField.cs` gives two lines. `grep -c 'is TextField' src/BrutalUi.Core/Tree/UiContext.cs` gives 2 (Q3, Q4).
9. `grep -rnE 'using .*(ImGui|Glfw)' src/BrutalUi.Core` gives no line (I1).
10. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I5).
11. Read section 4 ("Frame order"), section 5 (the text field paragraph and rule 5 of the widget rules), section 8 ("The paint hook"), section 9 (item 7 of the flow), and section 14 of `docs/technical-design.md`, and the `## [Unreleased]` part of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).

---

## Testing Strategy

### Unit Tests
- `Elements/TextFieldTypeTests` (10): the demo, the start text, a multi-byte code point, the capacity drop, a code point that does not fit, a control char, the stop, a caller target, the create-info and the overflow, and the type rule.
- `Elements/TextFieldCaretTests` (10): an insert at the caret, Home and End, the code point step, the ends, the keys that walk on and Tab, the modifiers, the repeat, a move that calls nothing, the caret draw, and the key codes.
- `Elements/TextFieldDeleteTests` (8): Backspace, Delete, a whole code point, the ends, the repeat, room for an insert, an edit in the middle, and the caret after a remove.
- `Elements/TextFieldSubmitTests` (6): the demo, the stop, the repeat, the keypad Enter, the modifiers, and no delegate.
- `Elements/TextFieldTextTests` (10): the span, the string, the `LString`, the same text, the cut, the empty set, `GetText`, the short span, an edit after a set, and an invalid byte.
- `Elements/TextFieldScrollTests` (9): the demo, Home, a move inside the view, a move past the left edge, a remove at the end, text that fits, a resize, `SetText` and the focus owner rule, and the padding and the border.
- `Elements/TextFieldBlinkTests` (6): the cycle, the restart on an edit, on a move, and on a focus gain, a bad delta, and no measurer.
- `Elements/TextFieldSelectionTests` (10): the demo, Shift+Home and Shift+End, the collapse of Left and Right, the collapse of Home and End, the remove, a dropped char, the flip over the anchor, the draw order and the focus rule, the scroll, and `SetText`.
- `Elements/TextFieldPointerTests` (10): the demo, the nearest boundary and the tie, the ends, a whole code point, Shift with a press, a scrolled field, a drag past the edge, the focus and the caret, a right press and a chord, and no measurer.
- `Elements/TextFieldDisabledTests` (5): the char, the keys, the focus drop and the caret, the press, and a code set.
- `Glfw.Tests/TextFieldKeyCodeTests` (1): the Core key codes equal the `GlfwKey` values.
- Each Core test uses `TextFieldTree`, except the no-measurer test and the gate. Each test pushes events through `UiContext.PushEvent` and calls `Update`. A test computes the expected rects by hand with 8 pixels for each byte.

### Allocation Tests
- `Elements/TextFieldAllocationTests` (3): `Update` and `Draw` with a focused field (the caret pass runs), the full input path (chars, the keys, the selection, the press, the drag, the drop, Enter, `SetText`, and `GetText`), and the real-work check. widget-sample adds the 1000-element gate with each widget (epic A9).

### Regression Tests
- The regression set of the phase intro with no edit in each phase, and each class in `tests/BrutalUi.Core.Tests/Input/` in phase 4.

### Integration Tests
- `scripts/test-linux.sh` runs the full suite and the HelloPanel smoke run. HelloPanel does not change. widget-sample adds the field to it.

### Manual Testing
- None in this feature. widget-sample adds the visible demo and the manual check of typing, the caret, the press, the drag, the selection, the scroll, and Enter on screen.

---

## Related Documentation

- **Requirements:** `docs/features/interactive-elements/text-field/requirements.md`
- **Epic:** `docs/features/interactive-elements/epic-overview.md`, `docs/features/interactive-elements/epic-requirements.md`
- **Pattern sources:** `docs/features/interactive-elements/button-and-toggle/implementation.md` ("The Widget Pattern"), `docs/features/interactive-elements/slider/implementation.md` (Decisions 5, 8, 10, and 11), `docs/features/interactive-elements/scroll-view/implementation.md` (Decisions 3, 6, and 13, and the gate shape), and risk 12 of `docs/features/interactive-elements/scroll-view/code-review.md`
- **Siblings:** `docs/features/interactive-elements/widget-sample/requirements.md` (adds the field to HelloPanel and to the 1000-element gate)
- **Design:** `docs/technical-design.md` sections 4 ("Frame order"), 5 (Element Tree, Events, the widget rules, "The keyboard step of the drain"), 8 ("The draw walk", "The paint hook"), 9 (Input, item 7), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Slider.cs` (the key handler and the create-info check), `src/BrutalUi.Core/Elements/Button.cs` (the `SetText` overloads), `src/BrutalUi.Core/Elements/ScrollView.cs` (the inline overflow), `src/BrutalUi.Core/Elements/WidgetInput.cs` (the checks and the key codes), `src/BrutalUi.Core/Elements/Utf8Text.cs` (`Utf8Equals`), `src/BrutalUi.Core/Tree/UiContext.cs` (`Update`, `SetFocusOwner`, `DrainEvents`), `src/BrutalUi.Core/Rendering/PaintWalk.cs` (steps 6 and 7 of `Enter`), `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs` (the measure rule), `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` (`Char` and `Key`), `tests/BrutalUi.Core.Tests/Elements/SliderTree.cs` (the tree shape), `tests/BrutalUi.Core.Tests/Elements/SliderKeyTests.cs` (the key tests), `tests/BrutalUi.Core.Tests/Elements/ScrollViewAllocationTests.cs` (the gate), and `tests/BrutalUi.Glfw.Tests/GlfwUiInputKeyTests.cs` (the key code check)
- **Skills:** no implementation skill in `.claude/skills/` applies. The skills there are workflow commands.

---

## Notes

### Open Scope Questions

The user answered these questions. Q1, Q3, and Q4 use the recommended option. Q2 uses option C.

**Q1. What is the demo of phase 1?**
- **A (recommended):** a unit test focuses the field, pushes the chars `H` and `i` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads the text `Hi`, two `TextChanged` calls, and a `Text` call `"Hi"` at (0, 0) inside the clip (0, 0, 81, 16). The developer runs the class filter `'*TextFieldTypeTests'` and reads a total of 10 with `failed: 0`. Reason: it shows the buffer, the delegate, and the clipped draw in one test, the form of the sibling demos.
- B: a person runs `scripts/test-linux.sh` and reads its PASS line, with the demo test in the run. Reason: the full suite and the smoke run, but the line does not show the text.
- C: a person types into a field in HelloPanel on screen. Reason: it moves widget-sample work into this feature and needs a GPU.

**Q2. Does a press place the caret?**
- A (recommended): no. A press focuses the field through the click focus of the drain, and the caret stays where it was. Reason: the requirement names no pointer rule, and the change needs no measurer in the drain.
- B: a left press places the caret at the nearest code point boundary under the pointer. The press handler stores the x, and the caret pass maps it with the measurer. Reason: the desktop habit, but one more phase with a hit map of each code point.
- **C (chosen):** B, and a drag selects (Decision 16, phase 6). Reason: full mouse editing, but it also needs `IDragCapture` and two more phases.

**Q3. What does a set past the capacity do?**
- **A (recommended):** `SetText` and the start text copy the longest prefix that fits and ends on a code point boundary (Decision 8). Reason: no exception in a hot path, and the buffer stays valid UTF-8 at each boundary.
- B: `SetText` throws `ArgumentException` and changes nothing. Reason: a loud signal, but a network text of unknown length needs a check before each call.
- C: `SetText` changes nothing and returns `false`. Reason: a quiet signal, but the signature differs from `Label.SetText`.

**Q4. Does the field size itself?**
- **A (recommended):** no. The field has no own size, as `Slider` and `ScrollView` (Decision 15). The game gives a width and a height with a rule or `Inline`. Reason: one widget rule for the epic, and no measurer in the layout pass.
- B: a measure callback gives the height of one line (the measure of an empty text) and a width of 0. Reason: a field fits its font with no height rule, but it needs the measurer of the context, as `Label` does.

### Conflicts and Gaps

1. **The handler target.** The brief says "`static readonly` delegates with the element as the target". Each sibling handler is a static method with no target that casts `e.Header.CurrentTarget` (rule 4 of the widget rules). The plan follows the code (Decision 1).
2. **The clip rule of scroll-view.** The epic says text-field depends on "the clip of content that is wider than the element" from scroll-view. The field uses no `ScrollView`. It reuses the clip order of the draw walk and the inline overflow of Decision 6 of scroll-view. The walks do not change.
3. **`UiContext.cs` changes.** The siblings left `UiContext.cs` alone. The blink and the scroll need a step after the layout pass (Decision 10). The change is two places, and phase 4 pins it.
4. **Code points, not graphemes.** The requirement says "a code point". An emoji with a skin tone, a flag, or a letter with a combining mark takes more than one step and more than one Backspace.
5. **The keypad Enter key.** The field follows the Enter rule of `Button`, so the keypad Enter key walks on. A person can ask for it later.
6. **No selection color in the style.** The selection uses `Style.Color` at 35 % of its alpha. A later style member can replace it.
7. **Out of scope by the requirement:** IME, the clipboard, undo (A6), multi-line text (A5), a placeholder, a password mask, validation, and a numeric mode. `Control` shortcuts (select all, word jump) walk on to the game.
8. **The selection after a focus loss.** The selection stays and draws again at the next focus gain. The field draws no selection with no focus.
9. **Known debt of the epic.** A press that a capture handler on an ancestor stops keeps the press target. `:disabled` on an ancestor does not block the field. `RoutingGate.PointerOverUi` follows the hit during a captured drag. This plan changes none of these rules.
10. **An exception in the caret pass.** `ImGuiRenderer.MeasureText` throws with no current ImGui context. The throw then comes after the layout pass and before the clear pass, as a measure throw of a `Label` does in the layout pass. The next `Update` clears the bits.
