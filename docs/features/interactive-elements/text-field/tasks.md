# interactive-elements/text-field - Task Checklist

**Last Updated:** 2026-10-27 09:46
**Progress:** 38/38 tasks complete (100%)
**Advanced phases:** Phases 1, 2, 4, 5, and 6 (ADVANCED, opus). Phases 3 and 7 are STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "Architecture Overview", "Internal Types", "The Edit Rules", "The Caret Pass and the Scroll Rule", "The Draw", and "Key Technical Decisions" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `System.Text`, `System.Text.Unicode`, `Brutal.Localization`, `Brutal.Numerics` as needed). Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. Tests write UTF-8 literals with `"..."u8` (Finding 11). A test class that interns a class name uses a name prefix that is unique to the class, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

Each phase runs these classes with no edit (the regression set): the focus classes (`FocusOwnerTests`, `ClickFocusTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`), the walk classes (`PaintWalkTests`, `PaintContentTests`, `HitTestTests`), the button-and-toggle classes (`ButtonKeyTests`, `ButtonTextTests`, `ToggleTests`, `WidgetDisabledTests`, `WidgetAllocationTests`), the slider classes (`SliderKeyTests`, `SliderDisabledTests`, `SliderAllocationTests`), the scroll view classes (`ScrollViewScrollTests`, `ScrollViewSliderTests`, `ScrollViewAllocationTests`), and `ThousandElementAllocationTests`.

The test scene of each text field test is `TextFieldTree` (task 1.4), unless a task names another scene. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a column and is pickable. Its children are `Field` (a `TextField`, 81 x 16 at (0, 0), no padding, `Inline.FontSize = 16`, the default capacity 16) and `Other` (a plain element, 100 x 100 at (0, 16), `IsFocusable = true`). Each byte of the text is 8 pixels wide, so 10 bytes plus the caret fill the field exactly. A text of `n` bytes ends at x `8n`.

---

## Phase 1: A Typed Character Shows in the Field (6/6 complete)

**Demo:** a unit test settles the tree, focuses `Field`, pushes the chars `H` and `i` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads `Text` as `"Hi"u8`, two `TextChanged` calls with the field as the sender, and a `Text` call `"Hi"` at (0, 0) with the size 16 after `PushClip(0, 0, 81, 16)` and before its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldTypeTests'` and reads a total of 10 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase writes the fixed UTF-8 buffer and the fit rule (Edit Rule 2). The fit check comes before each change, so a dropped insert changes no byte. Encode the rune into `stackalloc byte[4]` with `Rune.EncodeToUtf8`. Insert with `Span<byte>.CopyTo` on overlapping ranges of the same array (it handles the overlap). The create-info text uses `Utf8.FromUtf16` into the buffer: on `DestinationTooSmall` it writes whole scalars only, so the cut is on a boundary. The phase 1 tests must not assert the absence of a `Rect` call in the frame, because phase 2 adds the caret rect.

- [x] ✅ **1.1 Record the base and the counts** - Completed 2026-10-27
  - Description: Record in `docs/features/interactive-elements/text-field/context.md`, before any change: the base commit (`git rev-parse HEAD`), and the result of `git ls-files src/BrutalUi.Core/Elements/ScrollView.cs` (expect one line) and `git status --porcelain -- src tests` (expect no line). When scroll-view is not in the base commit, stop and record it. Record the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Glfw.Tests`, and the counts of each class of the regression set. Record the Codebase Findings.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 6 steps

- [x] ✅ **1.2 Add IsCharInput to WidgetInput** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsCharInput(Element, in CharEvent)` (Internal Types). The doc comment states: the check reads the disabled bit, the type, and the target. The key route gives each char with no target to the focus owner, so a char that bubbles from a focusable child gives no input. It reads no press target and no modifier (a char has none). Extend the class remarks: `IsCharInput` serves each widget that takes text.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 4 steps

- [x] ✅ **1.3 Write TextField.cs (the buffer and the char path)** - Completed 2026-10-27
  - Description: Write `src/BrutalUi.Core/Elements/TextField.cs` by the widget pattern: `public sealed class TextField : Element`, the type id of `TextField`, `CreateInfo` (`required int Capacity`, `string? Text`), the constructor, `Capacity`, `Text`, `CaretIndex`, `TextChanged`, the static `HandleChar` with its cached `static readonly UiEventHandler<CharEvent>`, `Insert`, and `PaintContent` (step 4 of The Draw only, with `_scroll` absent: `x0 = content.X`). The constructor checks `Capacity > 0` first and throws `ArgumentOutOfRangeException` with `ParamName` `info` and a message that names `CreateInfo.Capacity`. It allocates `_buffer = new byte[info.Capacity]` (the only `new byte[]` of the file), encodes the start text (cut at the capacity), puts the caret at the end, sets `IsFocusable = true` and `Inline.Overflow = Overflow.Hidden`, and writes `OnChar = s_char`. The class remarks state: the buffer rule (A5), the char rule with the drop, the stop rule, the sender, "read `TextChanged`, not the slot", "a write of `=` to `OnChar` removes the typing", the clip through the inline overflow, and the field has no own size (Decision 15).
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs`
  - Estimate: 12 steps

- [x] ✅ **1.4 Write TextFieldTree.cs** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldTree.cs`: the scene of the phase intro. The constructor takes `in TextField.CreateInfo`, and a second constructor uses `Capacity = 16`. It holds the points `OverField` (40, 8), `OverOther` (50, 60), and `OverNothing` (600, 500), `Settle()` (`Update(0f)`), `Focus()` (`Settle()` then `Context.SetFocus(Field)`), `Type(string text)` (one `PointerTree.Char` push for each rune of the text), and `Frame()` (`Update(0f)` and `Draw` into the tree renderer, returns the renderer). The doc comment states the rects, the 8-pixel byte, and the clip (0, 0, 81, 16).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldTree.cs`
  - Estimate: 6 steps

- [x] ✅ **1.5 Write TextFieldTypeTests.cs (10 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldTypeTests.cs` with 10 tests: `ACharShowsInTheField` (the demo), `TheStartTextComesFirst` (start text `ab`, char `c`: `abc`, one call), `AMultiByteCodePointGoesInAsUtf8` (`é` then `😀`: the bytes `C3 A9 F0 9F 98 80`, the caret at 6), `AnInsertPastTheCapacityDrops` (16 chars `a`, then `b`: 16 bytes, 16 calls, a root bubble `OnChar` handler gets no char), `ACodePointThatDoesNotFitDrops` (15 chars `a`, then `€`: no change and no call. Then `b`: 16 bytes), `AControlCharWalksOn` (U+0008, U+007F, U+0085, U+D800, and U+110000: no change, no call, and the root bubble handler gets 5 chars), `ACharStopsTheWalk` (the root capture handler runs, the root bubble handler does not), `ACharWithACallerTargetInserts` (a char with `Field` as the caller target and no focus owner inserts. A char with no target and no owner changes nothing), `TheCreateInfoTheTypeAndTheOverflow` (the type id is `TextField`, `IsFocusable` is `true`, `Inline.Overflow` and `Style.Overflow` are `Hidden`, `Capacity` is 16, a capacity of 0 or -1 throws `ArgumentOutOfRangeException` with `ParamName` `info`, a start text `abc€` with capacity 4 gives `abc`, and a null start text gives an empty text with the caret at 0), and `ATypeRuleMatchesTheTextField` (a rule with `Type = ElementType.Intern("TextField")` sets a fill on the field and not on `Other`. A rule with `overflow: visible` on the type does not remove the clip).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldTypeTests.cs`
  - Estimate: 10 steps

- [x] ✅ **1.6 Run the phase 1 gate and record it** - Completed 2026-10-27
  - Description: Run the build, the tests, the demo class, and the regression set. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 5 steps

---

## Phase 2: The Caret Moves and Deletes by Code Point (6/6 complete)

**Demo:** a unit test creates the field with the start text `abc`, focuses it, pushes Left, Left, and the char `X`, and runs `Update` and `Draw`. It reads `Text` as `"aXbc"u8`, one `TextChanged` call, and a caret rect at (16, 0, 1, 16) in the text color after the `Text` call and before the `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldCaretTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*TextFieldDeleteTests'` reads a total of 8 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase writes the boundary steps and the key switch (Edit Rule 3). `PreviousBoundary` and `NextBoundary` use `Rune.DecodeLastFromUtf8` and `Rune.DecodeFromUtf8`, and step `Math.Max(1, bytesConsumed)`, so an invalid byte is one step and the caret never sticks. Do not read Shift in this phase: Shift with Left moves the caret as Left does. Phase 5 adds the selection. Each used key stops the walk also at an end. Tab must walk on. The caret draws when the field holds `Focus` (step 5 of The Draw, with no blink check yet).

- [x] ✅ **2.1 Add IsEditKey and four key codes to WidgetInput** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsEditKey(Element, in KeyEvent)` and the constants `BackspaceKey` (259), `DeleteKey` (261), `HomeKey` (268), and `EndKey` (269) with doc comments that name the GLFW constants (`GLFW_KEY_BACKSPACE` and so on). The doc comment of `IsEditKey` states the target rule, the modifier rule (`Control`, `Alt`, and `Super` block. `Shift`, `CapsLock`, and `NumLock` do not), and that a repeat passes.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 5 steps

- [x] ✅ **2.2 Add the boundary steps, the key handler, and the caret draw** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `NextBoundary`, `PreviousBoundary`, `Remove`, `MoveCaret`, and the static `HandleKeyDown` with its cached `static readonly UiEventHandler<KeyEvent>`. The constructor writes `OnKeyDown = s_keyDown`. The handler covers Left, Right, Home, End, Backspace, and Delete (Edit Rule 3, no Shift and no Enter yet). `HandleChar` inserts at the caret. Add step 5 of The Draw. Extend the remarks: the key rule, the stop rule at an end, the keys that walk on, "a write of `=` to `OnKeyDown` removes the keys", the caret draw, and the code point rule (a combining mark or an emoji sequence takes more than one step, Conflicts and Gaps item 4).
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs`
  - Estimate: 12 steps

- [x] ✅ **2.3 Write TextFieldCaretTests.cs (10 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldCaretTests.cs` with 10 tests: `ACharGoesInAtTheCaret` (the demo), `HomeAndEndMoveTheCaret` (`abc`: Home gives 0, End gives 3, and the caret rect moves to x 0 and x 24), `TheCaretStepsOverEachCodePoint` (`aé€😀`: Left from 10 gives 6, 3, 1, and 0. Right gives the reverse), `TheCaretStopsAtEachEnd` (Left at 0 and Right at the end change nothing, call nothing, and stop the walk: the root bubble `OnKeyDown` handler gets nothing), `TheOtherKeysWalkOn` (Up, Down, Escape, the keypad Enter (335), and the key A walk on to the root. A key up walks on. Tab walks on and moves the focus to `Other`), `TheModifiersBlockTheKey` (`Control`, `Alt`, and `Super` with Left: no move, and the key walks on. `CapsLock` and `NumLock` with Left move), `ARepeatMovesAgain` (two Left keys, the second with `IsRepeat = true`, give two steps), `ACaretMoveCallsNothing` (a move calls no `TextChanged` and marks `Paint`), `TheCaretDrawsOnlyWithFocus` (no caret rect with no focus. With the focus, one rect of width 1 and height 16 in `Style.Color`. With `Inline.Opacity = 0.5`, the caret alpha is the alpha of `Style.Color.ScaleAlpha(0.5f)`), and `TheKeyCodesAreTheGlfwValues` (`WidgetInput.BackspaceKey` is 259, `DeleteKey` 261, `HomeKey` 268, and `EndKey` 269).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldCaretTests.cs`
  - Estimate: 10 steps

- [x] ✅ **2.4 Write TextFieldDeleteTests.cs (8 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldDeleteTests.cs` with 8 tests: `BackspaceRemovesTheCodePointBeforeTheCaret` (`abc`, Left, Backspace: `ac`, caret 1, one call), `DeleteRemovesTheCodePointAfterTheCaret` (`abc`, Home, Delete: `bc`, caret 0), `BackspaceRemovesAWholeCodePoint` (`a😀`, Backspace: `a`, caret 1. `€b`, Home, Delete: `b`), `NothingToRemoveCallsNothing` (Backspace at 0 and Delete at the end: no change, no call, and the key stops the walk), `ARepeatRemovesAgain` (three Backspace keys, two with `IsRepeat`, remove three code points and give three calls), `ARemoveMakesRoomForAnInsert` (16 chars `a`, Backspace, then `€`: dropped. Then `b`: 16 bytes), `AnEditInTheMiddleKeepsTheRest` (`hello`, Left twice, Backspace, char `L`: `heLlo`), and `TheCaretDrawsAfterARemove` (`abc`, Backspace: the caret rect at (16, 0, 1, 16)).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldDeleteTests.cs`
  - Estimate: 8 steps

- [x] ✅ **2.5 Write TextFieldKeyCodeTests.cs (1 test)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Glfw.Tests/TextFieldKeyCodeTests.cs` with 1 test, `TheTextFieldKeyCodesEqualTheGlfwKeys`: `WidgetInput.BackspaceKey`, `DeleteKey`, `HomeKey`, `EndKey`, `EnterKey`, `LeftKey`, and `RightKey` equal the `GlfwKey` values (copy the shape of `GlfwUiInputKeyTests.TheCoreTabKeyEqualsTheGlfwTabKey`). Read the member names of `GlfwKey` in the binding first. When a name differs (for example `Backspace`), use the binding name and record it in `context.md`.
  - File(s): `tests/BrutalUi.Glfw.Tests/TextFieldKeyCodeTests.cs`
  - Estimate: 4 steps

- [x] ✅ **2.6 Run the phase 2 gate and record it** - Completed 2026-10-27
  - Description: Run the build, the tests, the two demo classes, the new Glfw class, and the regression set. Record the counts in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 5 steps

---

## Phase 3: Enter Submits and Code Reads and Writes the Text (5/5 complete)

**Demo:** a unit test focuses `Field`, types `ok`, pushes Enter, and runs `Update`. It reads one `Submitted` call with the field as the sender, two `TextChanged` calls, and `GetText` into a stack span of 16 bytes that returns 2 and holds `"ok"u8`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldSubmitTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*TextFieldTextTests'` reads a total of 10 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)
**Tier note:** Copy the `SetText` shape of `Button`: compare first, and mark nothing for the same text. The span overload compares the cut prefix with `Text`. The string overload compares with `Utf8Text.Utf8Equals(Text, text)` first, then encodes with `Utf8.FromUtf16(text, _buffer, out _, out int written, replaceInvalidSequences: true, isFinalBlock: true)`. The `LString` overload calls the string overload with `text?.Localized` one time. The field keeps no `LString`, and `RefreshLocalizedText` does not know the field. A set from code calls no delegate (rule 7 of the widget rules).

- [x] ✅ **3.1 Add Submitted, GetText, and the SetText overloads** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `Submitted` and the Enter branch of `HandleKeyDown` (main Enter only, no repeat, Decision 6). Add `CutAtBoundary`, `GetText(Span<byte>)`, and the three `SetText` overloads (Decision 8). A set that changes the bytes puts the caret at the end and marks `Paint`. The start text of the constructor uses the same string path. Extend the remarks: the Enter rule, the cut rule, "only user input calls `TextChanged`", the span of `Text` is valid until the next change, `SetText("x")` with a literal needs the `u8` form, and the field shows an `LString` as it is at the call only.
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs`
  - Estimate: 10 steps

- [x] ✅ **3.2 Write TextFieldSubmitTests.cs (6 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldSubmitTests.cs` with 6 tests: `EnterCallsSubmitted` (the demo), `EnterStopsTheWalk` (the root bubble handler gets no Enter. The text does not change), `ARepeatOfEnterWalksOn` (an Enter with `IsRepeat`: no call, and the root gets it), `TheKeypadEnterWalksOn` (key 335: no call), `TheModifiersAndEnter` (`Control` with Enter walks on and calls nothing. `Shift` with Enter calls `Submitted`), and `EnterWithNoDelegateStillStops`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldSubmitTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.3 Write TextFieldTextTests.cs (10 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldTextTests.cs` with 10 tests: `SetTextCopiesTheBytes` (`SetText("name"u8)`: `Text`, the caret at 4, `Paint` marked, no call), `SetTextEncodesAString` (`SetText((string?)"héllo")`: 6 bytes), `SetTextReadsTheLocalizedText` (an `LString` gives its localized text, and a later locale change does not change the field), `TheSameTextMarksNothing` (after a settle, the same span and the same string mark no `Paint`), `ASetPastTheCapacityCutsAtACodePoint` (capacity 4: `"abc€"u8` gives `abc`, and the string `"ab😀"` gives `ab`), `AnEmptySetClearsTheText` (an empty span and `(string?)null` give 0 bytes and the caret at 0), `GetTextCopiesTheText` (the count and the bytes. A span of 16 bytes for 2 bytes leaves the rest untouched), `GetTextIntoAShortSpanCutsAtACodePoint` (`a€` into 3 bytes gives 1), `TheTextIsUsableAfterASet` (`SetText`, then Left and Backspace and a char edit the new text), and `AnInvalidByteStepsAsOne` (`SetText([0x61, 0xFF, 0x62])`: Left from 3 gives 2, then 1. Backspace removes the byte `0xFF`).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldTextTests.cs`
  - Estimate: 10 steps

- [x] ✅ **3.4 Run the phase 3 gate** - Completed 2026-10-27
  - Description: Run the build, the tests, the two demo classes, the earlier text field classes, and the regression set. Record the counts in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 4 steps

- [x] ✅ **3.5 Record the cut rule and the LString rule** - Completed 2026-10-27
  - Description: Record in `context.md`: the cut rule (Notes Q3), the `LString` rule, and the test count of each new class.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 2 steps

---

## Phase 4: Long Text Scrolls Under the Caret, and the Caret Blinks (6/6 complete)

**Demo:** a unit test focuses `Field`, types 12 chars `a`, and runs `Update(0)` and `Draw`. It reads a `Text` call at (-16, 0) with 12 bytes and a caret rect at (80, 0, 1, 16), both after `PushClip(0, 0, 81, 16)` and before its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldScrollTests'` and reads a total of 9 with `failed: 0`, and the same command with `'*TextFieldBlinkTests'` reads a total of 6 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes `UiContext.cs`, the frame order of each context. Change only two places: (1) in `Update`, add the caret pass after the `finally` block of the layout pass and before the clear pass, and (2) in `SetFocusOwner`, call `RestartCaretBlink` on a new owner that is a `TextField`, in the change branch only (not in the repair branch). Edit the remarks of `Update` (the frame order and the caret pass) and of `SetFocusOwner`. Do not change `DrainEvents`, `RoutePointer`, `DropStaleFocus`, the focus rules, the measure scope, or `Draw`. The pass reads `_measurer`, not `t_measurer`. A context with no measurer still blinks and keeps the scroll. Prove it with `ThousandElementAllocationTests`, `FocusAllocationTests`, `RoutingGateTests`, and each focus class.

- [x] ✅ **4.1 Add the text scroll, the blink, and AdvanceCaret** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `_scroll`, `_blink`, `TextScroll`, `CaretVisible`, `CaretBlinkInterval`, `AdvanceCaret` (The Caret Pass and the Scroll Rule), and `RestartCaretBlink`. Each path that changes the text or the caret calls `RestartCaretBlink` (Edit Rule 6). `SetText` writes `_scroll = 0`. `PaintContent` uses `x0 = content.X - _scroll` and draws the caret only when `CaretVisible`. Extend the remarks: the caret pass, the scroll rule, the blink, a field that is not the focus owner keeps its scroll, and the next `Update` after a `SetText` scrolls to the caret.
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs`
  - Estimate: 10 steps

- [x] ✅ **4.2 Add the caret pass to UiContext** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs` by the tier note. Write the caret pass as `if (_focusOwner is TextField field) { field.AdvanceCaret(deltaTime, _measurer); }`, with a comment that names the pass. The doc comment of `Update` names the frame order: the input pass, the style pass, the transition pass, the layout pass, the caret pass, and the clear pass. It states that the caret pass reads `deltaTime` with the rule of the transition pass for a bad value.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **4.3 Write TextFieldScrollTests.cs (9 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldScrollTests.cs` with 9 tests: `LongTextScrollsUnderTheCaret` (the demo), `HomeScrollsBackToTheStart` (then Home: the text at (0, 0), the caret at (0, 0, 1, 16)), `AMoveInsideTheViewDoesNotScroll` (after the demo state, Left three times: the text stays at (-16, 0), the caret at (56, 0, 1, 16)), `AMovePastTheLeftEdgeScrolls` (Home, End, then Left to the byte 1: the text at (-8, 0), the caret at (0, 0, 1, 16)), `ARemoveAtTheEndPullsTheTextBack` (12 chars, Backspace: `TextScroll` 8, the text at (-8, 0), the caret at (80, 0, 1, 16)), `TextThatFitsDoesNotScroll` (10 chars: `TextScroll` 0, the caret at (80, 0, 1, 16)), `AResizeKeepsTheCaretVisible` (the demo state, then `Inline.Width = 41` and `Update(0)`: `TextScroll` 56, the caret at (40, 0, 1, 16)), `SetTextAndTheFocusOwnerRule` (`SetText` on the scrolled field gives `TextScroll` 0 at once. A field that is not the focus owner keeps its scroll through an `Update`. The next `Update` with the focus scrolls to the caret at the end), and `ThePaddingAndTheBorderCount` (width 91, height 26, padding 4, and border width 1 on each side: 12 chars give the clip (1, 1, 89, 24), the text at (-11, 5), and the caret at (85, 5, 1, 16)).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldScrollTests.cs`
  - Estimate: 10 steps

- [x] ✅ **4.4 Write TextFieldBlinkTests.cs (6 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldBlinkTests.cs` with 6 tests: `TheCaretBlinks` (focus, `Update(0)`: a caret rect. `Update(0.5f)`: no caret rect. `Update(0.5f)`: a caret rect), `AnEditRestartsTheBlink` (hidden after `Update(0.5f)`, then a char and `Update(0)`: a caret rect), `ACaretMoveRestartsTheBlink` (the same with Left), `AFocusGainRestartsTheBlink` (hidden after `Update(0.7f)`, `ClearFocus`, `SetFocus(Field)`, `Update(0)`: a caret rect. A Tab from `Other` to the field gives the same), `ABadDeltaAdvancesNothing` (NaN, -1, and positive infinity keep `CaretVisible` `true`), and `AContextWithNoMeasurerStillBlinks` (a field in a `UiContext()` with no measurer: `Update` does not throw, `TextScroll` stays 0 after 12 chars, and the blink still flips).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldBlinkTests.cs`
  - Estimate: 7 steps

- [x] ✅ **4.5 Run the phase 4 gate and check the UiContext diff** - Completed 2026-10-27
  - Description: Run the build, the tests, the two demo classes, each earlier text field class, and the regression set. Run `git diff <base> -- src/BrutalUi.Core/Tree/UiContext.cs` and check that only the two places and doc comments change. Record the diff summary in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 5 steps

- [x] ✅ **4.6 Run the Linux test script** - Completed 2026-10-27
  - Description: Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. The HelloPanel smoke run holds no field, so it must not change. Record the counts in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 3 steps

---

## Phase 5: Shift Selects, and Typing Replaces the Selection (4/4 complete)

**Demo:** a unit test creates the field with the start text `hello`, focuses it, pushes Shift with Left two times, and runs `Update` and `Draw`. It reads a selection rect at (24, 0, 16, 16) in `Style.Color.ScaleAlpha(SelectionAlpha)` before the `Text` call. It then pushes the char `p` and reads `Text` as `"help"u8`, one `TextChanged` call, the caret at 4, and no selection rect. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldSelectionTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase adds `_anchor` to each path of the buffer. Each path that writes `_caret` decides the anchor by Edit Rule 3: `Insert`, `Remove`, `MoveCaret(index, extend)`, `SetText`, and the constructor. The fit check of a char uses the length minus the selection, and a dropped char keeps the selection (Edit Rule 2). Left and Right with a selection and no Shift collapse with no step. The scroll rule follows the caret, not the anchor. The selection draws only with `Focus`.

- [x] ✅ **5.1 Add the anchor and the selection** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `_anchor`, `AnchorIndex`, `SelectionAlpha`, the Shift branch of Left, Right, Home, and End, the collapse rule, the selection remove of Backspace and Delete, the selection replace of `HandleChar`, and step 3 of The Draw. `SetText` and the constructor collapse the anchor to the caret. Extend the remarks: the selection rule, the replace rule with the fit check, the collapse rule, and the selection color (`Style.Color` at 35 % of its alpha, Conflicts and Gaps item 6).
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs`
  - Estimate: 10 steps

- [x] ✅ **5.2 Write TextFieldSelectionTests.cs (10 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldSelectionTests.cs` with 10 tests: `ACharReplacesTheSelection` (the demo), `ShiftHomeAndShiftEndSelect` (`hello`, Shift+Home: the selection 0 to 5. End, Left twice, Shift+End: 3 to 5), `LeftAndRightCollapseTheSelection` (with the selection 1 to 3: Left gives the caret 1 and no selection. Again with the selection: Right gives 3), `HomeAndEndCollapse`, `BackspaceAndDeleteRemoveTheSelection` (each removes the selection with one call, and the caret goes to the start), `ADroppedCharKeepsTheSelection` (16 chars `a`, Shift+Left, `€`: no change, no call, and the selection stays. Then `b`: the last byte becomes `b`), `TheSelectionFlipsOverTheAnchor` (the caret at 3, Shift+Right two times, Shift+Left four times: the anchor 3, the caret 1, the selection rect at (8, 0, 16, 16)), `TheSelectionDrawsBehindTheTextOnlyWithFocus` (the selection rect comes before the `Text` call. With no focus, no selection rect and no caret rect), `TheSelectionFollowsTheScroll` (12 chars, Shift+Left two times: the selection rect at (64, 0, 16, 16) with the text at (-16, 0)), and `SetTextClearsTheSelection` (a selection, then `SetText("new"u8)`: the anchor and the caret at 3, no selection rect).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldSelectionTests.cs`
  - Estimate: 10 steps

- [x] ✅ **5.3 Run the phase 5 gate** - Completed 2026-10-27
  - Description: Run the build, the tests, the demo class, each earlier text field class, and the regression set. Record the counts in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 4 steps

- [x] ✅ **5.4 Record the selection decisions** - Completed 2026-10-27
  - Description: Record in `context.md`: the collapse rule, the selection color, and each decision that this plan did not settle.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 2 steps

---

## Phase 6: A Press Places the Caret, and a Drag Selects (4/4 complete)

**Demo:** a unit test creates the field with the start text `hello`, pushes a left press at (21, 8) and a move to (37, 8), and runs `Update` and `Draw`. It reads `FocusedElement` as the field, the anchor at 3, the caret at 5, a selection rect at (24, 0, 16, 16), and no `TextChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldPointerTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)
**Tier note:** A handler has no measurer during the drain. Thus the press handler and the move handler store the pointer x in text space (`_pendingX`: the pointer x minus `WidgetInput.ViewportOrigin(this).X`, minus the left border and padding, plus `_scroll`) and the extend flag (`_pendingExtend`). The caret pass maps `_pendingX` with `IndexAtX` before the scroll rule, writes the caret through `MoveCaret(index, extend)`, restarts the blink, and sets `_pendingX` to NaN. A press without Shift collapses the anchor to the new caret. A drag move always extends. Use `WidgetInput.IsDragStart` and `WidgetInput.IsDragMove`, as `Slider` does, so a right press and a chord place nothing. Implement `IDragCapture` explicitly, so the drag goes on outside the rect. The click focus of the drain still gives the focus, and the caret pass runs for the new owner in the same `Update`. A context with no measurer drops `_pendingX` and keeps the caret. A double press, a word select, and a drag scroll timer are out of scope. Add no allocation: `IndexAtX` measures each prefix with the span of the buffer.

- [x] ✅ **6.1 Add the press, the drag, and IndexAtX** - Completed 2026-10-27
  - Description: Edit `src/BrutalUi.Core/Elements/TextField.cs`: add `_pendingX`, `_pendingExtend`, `IndexAtX`, the static `HandlePointerDown` and `HandlePointerMove` with their cached `static readonly UiEventHandler<PointerEvent>` fields, and the explicit `IDragCapture`. The constructor writes `OnPointerDown` and `OnPointerMove`. `AdvanceCaret` maps a pending x before the scroll rule. Extend the remarks: the press rule, the drag rule, the Shift rule, the pending map of the caret pass, the capture, "a write of `=` to `OnPointerDown` removes the press", and the no-measurer rule.
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs`
  - Estimate: 12 steps

- [x] ✅ **6.2 Write TextFieldPointerTests.cs (10 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldPointerTests.cs` with 10 tests: `APressAndADragSelect` (the demo), `APressPlacesTheCaretAtTheNearestBoundary` (`hello`: a press at x 11 gives 1, at x 13 gives 2, and at x 12, a tie, gives 1), `APressOutsideTheTextGoesToAnEnd` (a press at x 80 gives 5, and a press at x 0 gives 0), `APressStepsOverAWholeCodePoint` (`a€b`: the boundaries 0, 1, 4, and 5 at x 0, 8, 32, and 40. A press at x 19 gives 1, and a press at x 21 gives 4), `ShiftWithAPressExtendsTheSelection` (Home, Right, then a Shift press at x 37: the anchor 1, the caret 5), `APressInAScrolledFieldReadsTheScroll` (12 chars, `TextScroll` 16, a press at x 3: the caret at 2, and `TextScroll` stays 16), `ADragPastTheRightEdgeScrolls` (12 chars, Home, `Update(0)`, a press at x 2 and a move to x 200: the anchor 0, the caret 12, and `TextScroll` 16), `APressFocusesTheFieldAndShowsTheCaret` (`Other` holds the focus, then a press on the field: `FocusedElement` is the field, the caret moves, and `CaretVisible` is `true` after `Update(0)`), `ARightPressAndAChordPlaceNothing` (a right press and a left press during a right press keep the caret), and `AContextWithNoMeasurerKeepsTheCaret` (a field in a `UiContext()` with no measurer: a press does not throw and keeps the caret).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldPointerTests.cs`
  - Estimate: 10 steps

- [x] ✅ **6.3 Run the phase 6 gate** - Completed 2026-10-27
  - Description: Run the build, the tests, the demo class, each earlier text field class, and the regression set. Record the counts in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 4 steps

- [x] ✅ **6.4 Record the pointer decisions** - Completed 2026-10-27
  - Description: Record in `context.md`: the pending map, the tie rule, the Shift rule, and each decision that this plan did not settle.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 2 steps

---

## Phase 7: Disabled Fields, the Allocation Gate, and the Record (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldAllocationTests'` and reads a total of 3 with `failed: 0`, and the same command with `'*TextFieldDisabledTests'` reads a total of 5 with `failed: 0`. A planted allocation at the start of `TextField.HandleChar` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 6
**Agent tier:** STANDARD (sonnet)
**Tier note:** Copy the gate shape of `ScrollViewAllocationTests` and `SliderAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no new `string`, and no `Assert`. Write each text as a `u8` literal or a static `byte[]`. Do not add warm-up rounds to hide an allocation. Do not run a plant and its restore in one parallel tool block (a gotcha of button-and-toggle).

- [x] ✅ **7.1 Write TextFieldDisabledTests.cs (5 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldDisabledTests.cs` with 5 tests: `ADisabledFieldTakesNoChar` (a char with the disabled field as the caller target: no change, no call, and the root bubble handler gets it), `ADisabledFieldTakesNoKey` (Left, Backspace, and Enter with the field as the caller target: no change, no call, and the root gets each key), `ADisableDropsTheFocusAndTheCaret` (focus, `States |= Disabled`, `Update(0)`: `FocusedElement` is null and the frame has no caret rect), `APressOnADisabledFieldGivesNoFocus` (also no caret move and no selection), and `ACodeSetStillWorks` (`SetText` on a disabled field changes the text and calls nothing).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldDisabledTests.cs`
  - Estimate: 6 steps

- [x] ✅ **7.2 Write TextFieldAllocationTests.cs (3 tests)** - Completed 2026-10-27
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/TextFieldAllocationTests.cs` with 3 tests. The scene: a column root with 20 fields (81 x 16, capacity 32, the start text `field`, `FontSize` 16), the last one in `Disabled`, and a sheet with `TextField`, `TextField:focus`, and `:disabled` rules. The first field holds the focus. `UpdateAndDrawAllocateNothingWithTextFields` (100 rounds of `Update(0.016f)` and `Draw`, so the caret pass and the blink run). `TheTextFieldInputPathAllocatesNothing` (each step on the first field: `SetText("seed"u8)`, the chars `a`, `b`, and `€`, Left, Shift+Left, the char `c`, Backspace, Delete, Home, Shift+End, Delete, End, a left press, a drag move, and its release, Enter, and a char past the capacity on a full second field set with `SetText`, then `GetText` into a stack span, `Update(0.016f)`, and `Draw`). `TheMeasuredBodyDoesRealWork` (after one step: the `TextChanged` count, one `Submitted` call, the expected text, 19 text draws, and one caret rect or none by the blink phase, with the blink phase read from `CaretVisible`).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/TextFieldAllocationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **7.3 Plant an allocation, run the suite, and restore** - Completed 2026-10-27
  - Description: Copy `src/BrutalUi.Core/Elements/TextField.cs` to the scratchpad. Plant `GC.KeepAlive(new object());` as the first line of `TextField.HandleChar`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, in a later tool call, and check it with `cmp`. Record the result and the gates that failed in `context.md`.
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs` (plant and restore), `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 5 steps

- [x] ✅ **7.4 Record the text field in sections 4 and 5 of the technical design** - Completed 2026-10-27
  - Description: Edit `docs/technical-design.md` section 4 ("Frame order": the caret pass after the layout pass) and section 5: a "TextField" paragraph after the scroll view paragraph (the buffer rule, the char rule and the drop, the key rule, the selection, the press and drag rule, the Enter rule, the cut rule of `SetText`, `GetText`, the clip, the caret pass, the scroll rule, the blink, the disabled rule, `TextChanged`, and `Submitted`). Edit rule 5 of the widget rules (`WidgetInput` also holds `IsCharInput` and `IsEditKey`).
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] ✅ **7.5 Record sections 8, 9, and 14 and the changelog** - Completed 2026-10-27
  - Description: Edit `docs/technical-design.md` section 8 ("The paint hook": a `TextField` draws its selection, its text, and its caret, and it measures with the renderer of the draw), section 9, item 7 of the flow (a focused `TextField` takes the char and the edit keys, and Tab walks on), and section 14 (the text field test classes and the twelfth gate with the plant). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`TextField`) and "Changed" (`UiContext.Update` runs the caret pass for a focused `TextField`).
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`
  - Estimate: 6 steps

- [x] ✅ **7.6 Run the STE lint on the changed docs** - Completed 2026-10-27
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/interactive-elements/text-field/implementation.md`
  - Estimate: 4 steps

- [x] ✅ **7.7 Run the final gate and record the counts** - Completed 2026-10-27
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.
  - File(s): `docs/features/interactive-elements/text-field/context.md`
  - Estimate: 5 steps

---

## Progress Tracking

### Completed This Session (2026-10-27)
- Phases 1 to 7 (38 tasks), each with verifier PASS and Demo: PASS.
- The cross-phase review: 16 findings checked off in `code-review.md` (groups `review-g1.md` to `review-g9.md`, and CR-16 by the parent), verifier PASS.

### Discovered New Tasks
- None

### Blocked Items
- None

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
