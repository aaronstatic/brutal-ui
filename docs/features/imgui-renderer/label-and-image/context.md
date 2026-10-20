# imgui-renderer/label-and-image - Context & Decisions

**Last Updated:** 2026-10-20 22:13
**Current Phase:** Done (phases 1 to 6 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The user picked the recommended option of each of the four open scope questions (2026-10-20).
- ✅ imgui-draw-list is committed on `main` as `8af276f`.
- ✅ Phase 1: the measurer slot of `UiContext`, the thread-static measure scope of `Update`, `Label` with one line, and `LabelTests`. Tests 864 to 877, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 2: the wrap rule of `RecordingRenderer`, `SnapWrapWidth`, the label measure rule, `LabelWrapTests`, and `ImGuiLabelTests`. The real ImGui sweep of 161 widths gives the same lines in the measure and the draw. Tests 877 to 901, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 3: `ImageScaleMode`, `Image` (natural size, measure, the three scale modes, the dirty rules), `ImageTests`, and `ImGuiImageElementTests`. Tests 901 to 929, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 4: the `Brutal.Localization` probe (Risk 5 does not occur), the label setters, the `LString` members, `UiContext.RefreshLocalizedText()`, and `LabelTextTests`. Tests 929 to 963, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 5: `LayoutNode.ContentInsets` and `ContentBox`, the write in the `if (performLayout)` block (and the zero-out of a hidden subtree), `ContentInsetsTests`, the insets in `LayoutDifferentialTests`, the content box in the label and image draw, `ContentBoxTests`, and the ImGui percent padding tests. Tests 963 to 992, 0 failed. Bytes per element 730.5 to 746.5. The verifier gave PASS and Demo: PASS.
- ✅ Phase 6: `ElementAllocationTests` (0 bytes over 100 frames), the ImGui label allocation test, the planted allocation (exit 2, then exit 0 after the restore), `scripts/test-linux.sh` PASS, `docs/technical-design.md`, `CHANGELOG.md`, `human-testing.md`, and the hand-offs. Tests 992 to 997, 0 failed. The verifier gave PASS and Demo: PASS (the wildcard filter reads 12, the full class name reads 4).
- ✅ The cross-phase review: 9 findings (3 MEDIUM, 6 LOW), 9 resolved by review groups 1 to 5. Tests 997 to 1013, 0 failed. The verifier `review-verify.md` gave PASS, and `scripts/test-linux.sh` passes.
- ✅ The draft plan of hello-panel-sample: `docs/features/imgui-renderer/hello-panel-sample/implementation.md` (4 phases, 27 tasks, 4 open scope questions).

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification".
- 📋 `/autorun-epic` commits the feature, then builds hello-panel-sample after the user answers its open scope questions.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Elements/Label.cs` - the label element (phase 1, then phases 2, 4, and 5)
- `src/BrutalUi.Core/Elements/Image.cs` - the image element (phase 3, then phase 5)
- `src/BrutalUi.Core/Elements/ImageScaleMode.cs` - the three scale modes (phase 3)
- `src/BrutalUi.Core/Tree/UiContext.cs` - the create-info, the measurer slot, the measure scope, and `RefreshLocalizedText`
- `src/BrutalUi.Core/Tree/LayoutNode.cs` - `ContentInsets` and `ContentBox` (phase 5)
- `src/BrutalUi.Core/Layout/LayoutEngine.cs` - the write of `ContentInsets` (phase 5)

### Related Files
- `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs` - the wrap rule and `LastMeasureWrapWidth` (phase 2)
- `src/BrutalUi.Core/Layout/MeasureCallback.cs` - the measure contract
- `docs/features/imgui-renderer/label-and-image/implementation.md` - Implementation plan

---

## Important Decisions

- The run is the called mode of `/autorun-feature`, from `/autorun-epic`. The orchestrator makes no git write. The caller commits.
- The answers of the four open scope questions (2026-10-20): 1 the Core `*DrawsAOneLineLabel` demo, 2 `UiContext.RefreshLocalizedText()`, 3 `LayoutNode.ContentInsets`, 4 the thread-static measurer scope.
- The changes from the plan, each one recorded in its phase section: the no-wrap return for a limit of 0 (phase 2), the `Texture` dirty rule of task 3.4 and the none handle rule (phase 3), the UTF-8 byte compare after a span text (phase 4), and the zero-out of `ContentInsets` for a hidden subtree (phase 5).
- The cross-phase review used seven C# rules in place of the Vercel React rules, as the sibling reviews did. Each probe ran in a scratch copy of the repository.
- Review group 1 (CR-1) raises a fractional draw wrap width by the float noise of the padding sum (`Label.DrawWrapWidth`). A whole-pixel width does not change, so each demo line stays the same. The whole-pixel `MeasureText` contract stays open (see "Open Questions").
- Review group 4 (CR-6) skips the draw of an image with a UV component that is not finite. The setter does not throw, so the public API stays the same.
- The demo filter of phase 6 stays as the plan states. `human-testing.md` gives both totals.

---

## Phase 1: Decisions

- Task 1.1: the test total of the solution before the first edit is **864** (0 failed). The imgui-draw-list work is on the branch: `src/BrutalUi.ImGui/ImGuiRenderer.cs` and `tests/BrutalUi.ImGui.Tests/` exist, and `IUiRenderer.Text` takes `wrapWidth`.
- `.editorconfig` has no naming rule for a static field. Thus the thread-static field keeps the name of the plan, `t_measurer`, with `[ThreadStatic]`. The static delegate field of `Label` is `s_measure`, as the mutable and reference static fields of the code (`s_lock`, `s_generationCount`).
- The leaf path of `LayoutEngine.LayoutInternal` calls `MeasureNodeWithMeasureFunc` only for a leaf with a callback. That method skips the callback when both modes are `Exactly`. Thus a label that is a root with an auto size, in a finite viewport, never measures. The tests put each label in a root with `AlignItems = Align.FlexStart`.
- The engine is re-entrant for two trees: its only static state is the atomic `s_generationCount`. Thus a nested `Update` of another context from a measure callback is safe. The test `ANestedUpdateKeepsTheOuterScope` proves that the outer scope comes back.
- `UiContext(in CreateInfo)` throws `ArgumentNullException` with the parameter name `info` for a null measurer and for `default(CreateInfo)`.
- The measure callback casts the element with `(Label)element`. Code that copies the callback to another element gets `InvalidCastException`.
- Blast radius of the `UiContext` change: `Update` only. The layout loop runs in `try` and `finally`. The clear pass and `Draw` do not change. No existing test and no sample uses the new constructor. Each existing test passes with no change.
- Additions to the test list of the brief: `ANestedUpdateKeepsTheOuterScope`, `EncodesTheTextToUtf8` (a text over 32 bytes, a multi-byte character, and the defaults of the create-info). `DrawsNothingForEmptyText` is a theory for a null text and an empty text. `TheScopeIsEmptyAfterUpdate` also checks that the scope holds the measurer during the measure.
- Task 1.8: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution is **877** (864 + 13 in `LabelTests`), 0 failed. The demo prints these lines:
  ```
  rect 0,0,40,16
  Begin 400x300 scale 1
  Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello"
  End
  ```

### Phase 1: Follow-up and open points

- An empty label still measures: `RecordingRenderer` gives `(0, size)`, so an empty label has the height of one line. ImGui `CalcTextSize` also gives the line height for an empty text. Phase 2 or phase 4 can decide if an empty label has a height of 0.
- The label does not check `Size`. A negative or `NaN` size goes to the renderer as it is. The plan states no rule. A rule needs a decision on the public API.
- In phase 1 the draw wraps at `content.Z` and the measure uses no wrap. With a real font, float noise can wrap a line that fits. Phase 2 adds `SnapWrapWidth` and the wrap rule.

---

## Phase 2 Group 1: Decisions

- Task 2.1, the search before the edit: no caller sends a wrap width above 0 to `RecordingRenderer.MeasureText`. Before the edit, the only caller was the `Label` callback, with a wrap width of 0. The `MeasureText` calls in `tests/BrutalUi.ImGui.Tests/` go to `ImGuiRenderer`. The ImGui project uses `RecordingRenderer` for one draw only (`ImGuiRendererAllocationTests.TheMeasuredDrawDoesRealWork`). The benchmark calls no `MeasureText` and has no label. Thus Risk 7 does not occur. The rule for no wrap keeps the same expression, `utf8.Length * size * 0.5f`.
- The wrap rule of `RecordingRenderer` counts bytes, not pixels. It multiplies by `size * 0.5` only for a compare and for the result, so no float noise adds up. A private struct `WrapState` holds the line count, the bytes of the current line, the widest line, and a flag for a used line. The rule allocates nothing.
- The edge cases of the recording rule that the brief does not name:
  - An infinite wrap width gives no wrap. It sets `LastMeasureWrapWidth`, because the value is above 0. `NaN` and a value of 0 or less set nothing.
  - Two spaces in a row give an empty word with a width of 0. A leading space gives an empty first word. Thus a text that fits keeps the width with no wrap (`"a  b"` at wrap 100 gives `(32, 16)`).
  - The rule keeps a space at a line break as an empty word. ImGui drops the spaces at a break. Thus `"Hello "` at wrap 40 gives 2 lines in `RecordingRenderer` and 1 line in ImGui. No test uses a space at the end of a wrapped text.
  - A part of a long word is `max(1, floor(wrap / (size * 0.5)))` bytes. A guard loop makes the part 1 byte smaller while the part is wider than the wrap width, against float noise in the division. A wrap width below one byte gives parts of 1 byte (`"ab"` at wrap 4 gives `(8, 32)`).
- Deviation from the pseudo-code of "The Label Measure Rule": the callback returns `free` for a limit of 0, and sends no second `MeasureText`. The result is the same, because `MeasureText(..., 0)` is no wrap. The callback thus calls the measurer one time for a limit of 0.
- `SnapWrapWidth` snaps a value when the distance to the nearest whole number is 1/64 pixel or less (the private constant `WrapSnapTolerance`). `96.01` gives 96, and `96.02` stays.
- The engine path of `WrapsAtTheAtMostLimit`: the last wrapped measure is at 100, not at 96. The final layout of the label uses the `AtMost` constraint of 100 again (a cache hit or a new measure), not `Exactly` 96. The draw wraps at 96.
- Additions to the test list of the brief: `TheRecordingRuleHandlesTheEdgeCases` (empty text, an infinite width, a long word after a word, the next word after the last part, a width below one byte, and spaces), more `SnapWrapWidth` cases (negative infinity, `96.01`, `96.02`, and 100), and `KeepsTheLinesForEachWidth` (a theory for `FlexStart` and `Stretch`, each whole width from 1 to 200). That test measures at the wrap width of the draw and expects the height of the rect. It checks the width from 8 pixels up only: a stretched box narrower than one byte cannot hold a glyph.
- The results: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution goes from **877** to **897** (20 new tests in `LabelWrapTests`), 0 failed. The phase 1 demo prints the same lines:
  ```
  rect 0,0,40,16
  Begin 400x300 scale 1
  Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello"
  End
  ```
- Blast radius: `Label.MeasureLabel`, `Label.PaintContent`, and `RecordingRenderer.MeasureText`. The no-wrap path of `RecordingRenderer` does not change. Each existing test passes with no change.

### Phase 2 Group 1: Follow-up and open points

- A label in a box narrower than one glyph gives lines that are wider than the box. The label does not clamp. ImGui puts at least one glyph on each line too. Group 2 can confirm this with the real measure.
- The space rule of `RecordingRenderer` differs from ImGui at a line break (see above). A Core test with a space at the end of a wrapped text can thus differ from the ImGui result.

---

## Phase 2 Group 2: Decisions

- Task 2.4: the test total of the solution before the first edit is **897** (0 failed).
- The real ImGui measure keeps the draw rule of group 1. The measure and the draw give the same lines at each width from 40 to 200. No change in `src/` occurs, and the draw rule does not change (no wrap width of `w + 1`).
- The root of each test gets its width from `UiContext.ViewportSize` (the width by 300), as in `LabelWrapTests`. The harness display stays 400 by 300. `ImGuiRenderer.Begin` pushes no clip for the viewport, so the dump shows the clip `0,0,400,300`.
- The label has no padding and no border in phase 2. Thus the content box is the rect, and the tests read no `ContentBox` and no `SnapWrapWidth`. Each glyph is 7 pixels wide, so each measured width is a whole number, and the snap does not change a value in these tests.
- The line count of the draw is `ceil((maxY - rect.Y - 0.01) / 13)`, from the raw vertex buffer of the background list. The glyph quads of a line span y from 2 to 13 below the line top, so a line with a comma ends exactly on the line bottom. The slack of 0.01 keeps that line from a count of one more line.
- The vertex rule grows the rect by 1 pixel and the slack of 0.01 on each side, as `AssertInsideBox` of `ImGuiRendererTextTests`. The acceptance bound `0,0-64,27` also holds: the demo vertices are `1,2-60,24`.
- The helpers `ReadVertexBounds` and `VertexBounds` of `ImGuiRendererTextTests` are private. The new file holds its own copy, so no existing test file changes.
- Additions to the test list of the brief:
  - The demo and the stretched test also assert the glyph count (15 glyphs, 60 vertices), 2 drawn lines, and the exact dump `cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 1,2-60,24 colors #FFFFFFFF`. The stretched label at 70 draws the same lines as the label at 63.
  - `KeepsOneLineWhenTheTextFits` also asserts 1 drawn line and the vertex rule.
  - The sweep also checks the glyph count and the vertex rule at each width, and it prints the count of widths for each line count. It asserts that the sweep has widths with 1, 2, and 3 lines, so it covers the cut inside a word, the break after a mark and a blank, and one line.
- Task 2.5, the results: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution goes from **897** to **901** (4 new tests in `ImGuiLabelTests`), 0 failed. The demo prints these lines:
  ```
  label rect 0,0,63,26
  cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 1,2-60,24 colors #FFFFFFFF
  ```
- The sweep prints these lines:
  ```
  checked 161 widths, first failed width none
  1 drawn lines: 89 widths
  2 drawn lines: 49 widths
  3 drawn lines: 21 widths
  4 drawn lines: 2 widths
  ```
  The 89 widths with one line are 112 to 200. A width of 112 fits the text exactly (`free.X <= limit`).
- Blast radius: one new test file. No file in `src/` and no existing test changes.

### Phase 2 Group 2: Follow-up and open points

- The default font is monospace with whole advances. The sweep thus does not test a font with fractional advances, where the round-up of the measured width matters. Phase 5 or a later font test can add a sweep with a second font (for example `GlyphExtraAdvanceX` of 0.5) and a size such as 13.5.
- The sweep checks `Align.FlexStart` only, as the brief states. The stretched path wraps at the exact box width in the measure and the draw, so it has the same wrap width on both sides.

---

## Phase 3: Decisions

- The test total of the solution before the first edit is **901** (0 failed), the total at the end of phase 2.
- Deviation from the brief (the dirty rule of `Texture`): a change from the none handle, or to the none handle, marks `Layout` and `Paint` also when the size stays the same. The none handle gives a natural size of `(0, 0)`, so the change from `TextureHandle(0, 64x32)` to `TextureHandle(7, 64x32)` changes the natural size. With `Paint` only, the image keeps a size of `0x0`. The test `AChangeFromTheNoneHandleMarksLayout` proves the rule. The rules of the brief stay true: a new id with the same size (no none handle) marks `Paint` only, a new size marks `Layout` and `Paint`, and the same id and size marks nothing.
- The plan text of "The Public API" states "a new id or size: Layout | Paint". The brief states "a new id with the same size: Paint". The code follows the brief.
- A texture size with a component of 0 or less draws nothing, also in a box with a style size (the UX rule "a size of 0 ... draws nothing"). A zero UV span with a real texture is not a size of 0. It takes no space by default. In a box with a style size, it draws with the fall-back of `StretchToFill` (`DrawsAZeroUvSpanWithTheStretchRule`).
- A tint with an alpha of 0 after the scale draws nothing, as the label does for its color (`DrawsNothingForATransparentTint`).
- The measure rule is the internal static `Image.MeasureAxis`. A limit that is not finite gives the natural size, so the callback never returns `NaN` or infinity. The natural size is the internal static `Image.NaturalSize`. A UV span that is not finite gives 0 on its axis.
- An unknown `ImageScaleMode` value draws as `StretchToFill` (the theory row `(ImageScaleMode)99`).
- The setter of `Uv` compares each component with `float.Equals`, so a set of `NaN` to `NaN` marks nothing. The setter of `Texture` compares the id and the two size components.
- `FitRect` compares the height of the full-width rect with the box height, then centers the rect. `CropUv` scales the signed half span around the center, so a flipped UV keeps its direction and an atlas region stays inside itself.
- Additions to the test list of the brief: `UsesTheDefaultsOfTheCreateInfo`, `KeepsTheFlipOfTheUvRect`, `MeasuresTheNaturalSizeForALimitThatIsNotFinite`, `DrawsNothingForATransparentTint`, `DrawsEachScaleMode` (a theory with 4 rows), `AChangeFromTheNoneHandleMarksLayout`, and `DrawsAZeroUvSpanWithTheStretchRule`. `KeepsTheAtMostContract` calls the callback directly and also compares a root of width 200 with a `NaN` viewport width. The demo context has no measurer, so the demo also proves that an image needs no measurer.
- Blast radius: two new files in `src/` (`ImageScaleMode.cs`, `Image.cs`) and two new test files. No existing file in `src/` and no existing test changes. `Image` reads `TextureHandle.Size` and calls `IUiRenderer.Image` only.
- Task 3.6, the results: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution goes from **901** to **929** (27 in `ImageTests` and 1 in `ImGuiImageElementTests`), 0 failed. The demo prints these lines:
  ```
  Begin 400x300 scale 1
  Image 0,0,64,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0
  Image 0,57,100,50 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0
  End
  ```
- The ImGui test prints `cmd 0 clip 0,0,400,300 tex id 4660 elems 6 verts 0,0-64,32 colors #FFFFFFFF`.

### Phase 3: Follow-up and open points

- The measure rule works on each axis alone. An image with a style width and an auto height keeps its natural height, so the aspect changes. An aspect rule for the measure (for example a height from the width) needs a decision on the public API.
- `ScaleToFit` can give a draw rect with fractional pixels (for example a 3 by 1 texture in a box of 100 by 100). The rect is not snapped. A pixel snap rule for images needs a decision.

---

## Phase 4: Decisions

- The test total of the solution before the first edit is **929** (0 failed).
- Task 4.1, the probe of `Brutal.Localization` 2026.10.2 (a console project in the session scratch folder, `localization-probe/`, Release):
  - (a) `LString(LStringId id, string templateString, Regex replacementPattern = null)` and `LString(string id, string templateString, Regex replacementPattern = null)`. The `Regex` argument has the default `null` (then `DefaultOptions.ReplacementPattern`). Thus `new LString("id", "Hello")` compiles. An empty id throws `ArgumentException`. `LocalizedStrings(Regex replacementPattern = null)` has the same default.
  - (b) Yes. Two reads of `Localized` give the same reference and allocate 0 bytes, before and after `Localize`. Risk 5 does not occur. Phase 6 keeps `RefreshLocalizedTextAllocatesNothing`.
  - (c) Yes. `Localize(s)` gives a new reference: `Localized` is the reference of the argument `s`. A second `Localize` with the same content and a new string instance also gives a new reference. Thus the refresh needs the ordinal content compare after the reference compare.
  - (d) Yes. After `RemoveLocalization`, `Localized` is the `Template` reference. Before the first `Localize`, `Localized` is also the `Template` reference.
  - (e) Yes, for each instance that the `LocalizedStrings` instance created (`Create`, `GetOrCreate`). The provider ran one time for the one created instance. A manual `new LString(...)` is not in `LStringInstances` and keeps its text. `RemoveLocalizedStringProvider` also gives back the `Template` reference of each created instance. A provider that returns `false` leaves the template.
  - Extra: an `LString` with a replacement variable (`"Hi {Name}"`) shows the template text with the variable as it is, before and after `Localize("Salut {Name}")`.
- `Label` keeps `_lastText`: the string that the buffer holds, or `null` after a text from a span. `ApplyText(string)` is the one compare path of `SetText(string)`, `SetText(LString)`, and `RefreshLocalized()`. The compare is ordinal with `_lastText`, and else a compare of the UTF-8 bytes (`Label.Utf8Equals`). Thus "the same value marks nothing" also holds after a text from a span. `Utf8Equals` encodes in chunks of 256 bytes into a stack buffer with `Utf8.FromUtf16`, so it allocates nothing. It replaces a lone surrogate with U+FFFD, as `Encoding.UTF8` does.
- Deviation from the plan text "SetText(string) compares with the last string (ordinal)": the method also falls back to the byte compare. The plan rule stays true when `_lastText` is set. The fallback removes an extra mark after a text from a span.
- `SetText(ReadOnlySpan<byte>)` and `SetText(string)` clear `LocalizedText` also when the text stays the same. The clear marks nothing. `SetText(LString)` with the same content sets `LocalizedText` and marks nothing.
- `SetText(ReadOnlySpan<byte>)` accepts a part of `Label.Text` as the source. A part is never longer than the buffer, so no growth occurs, and `Span.CopyTo` handles the overlap.
- `RefreshLocalized()` returns at once when `Localized` has the reference of `_lastLocalized`. Otherwise it stores the new reference and calls `ApplyText`. A new reference with the same content thus marks nothing, and the next refresh takes the fast path.
- The constructor throws `ArgumentException` (parameter name `info`) when `Text` and `LocalizedText` are both not null. `Text = ""` with an `LString` also throws.
- `EnsureCapacity` doubles the length and caps the growth at the needed length near `int.MaxValue`.
- `UiContext.RefreshLocalizedText()` walks each root with a private static recursive method in tree order. It reads `ChildStorage.AsSpan()`, so it allocates nothing. It has no guard against a call during `Draw`. The `Draw` doc forbids a tree change from a renderer or a hook already, and a refresh changes dirty bits only.
- The BRUTAL analyzer `BRUTALCORE0011` ("Prefer using a UTF8 string") is an error at a call of `SetText` with a string literal, because the `ReadOnlySpan<byte>` overload exists. The game code thus writes `label.SetText("Hello"u8)` for a literal. The tests that check the string path pass a string variable. A bare `SetText(null)` is ambiguous between the three overloads. The tests cast (`(string?)null`, `(LString?)null`).
- The provider delegate of `LocalizedStrings` has `[NotNullWhen(true)] out string?`. A lambda needs the same attribute, or CS8622 occurs.
- Additions to the test list of the brief: `SetTextFromAPartOfTheTextCopiesTheBytes`, `SetTextFromANullStringGivesAnEmptyText`, `TheConstructorReadsTheLocalizedText`, `SetTextFromAStringClearsTheLocalizedText`, `SetTextFromAnLStringShowsTheLocalizedText`, `RefreshOfAnEmptyContextDoesNothing`, `RefreshShowsTheTextOfANewProvider` (the probe point (e) on a private `LocalizedStrings` instance), `Utf8EqualsMatchesTheEncoder` (a theory with 8 rows), and `Utf8EqualsComparesALongText`. `RefreshReachesADeepLabel` uses a chain of 50 elements, and it checks that the refresh marks no element of the chain and no label with a plain text.
- Blast radius: `Label` (new setters, new members, and a private compare path) and `UiContext` (one new public method). The getters of `Font`, `Size`, and `Color` keep their names and types, so `MeasureLabel` and `PaintContent` do not change. No existing test changes.
- Task 4.6, the results: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution goes from **929** to **963** (34 in `LabelTextTests`), 0 failed. The demo prints these lines:
  ```
  rect 0,0,40,16
  dirty Layout, Paint
  rect 0,0,128,16
  Text 0,0 font 0 size 16 #FFFFFFFF wrap 128 "Bonjour le monde"
  ```

### Phase 4: Follow-up and open points

- `RefreshLocalizedText` knows the `Label` type only (plan note 6). A later text element needs a virtual hook on `Element` or a type check.
- The label does not check the UTF-8 bytes of `SetText(ReadOnlySpan<byte>)`. An invalid sequence goes to the renderer as it is.
- Phase 5 adds the content box. The setters of phase 4 do not depend on it.

## Phase 5 Group 1: Decisions

- The test total of the solution before the first edit is **963** (0 failed).
- Task 5.1, the padding width. The engine resolves the padding of a node against the `ownerWidth` argument of `LayoutInternal`, on each edge and on both axes. `CalculateLayoutImpl` passes the same `ownerWidth` to `PaddingAndBorderForAxis` and `LeadingPaddingAndBorder` (step 1), to `MeasureNodeWithMeasureFunc`, and to `MeasureNodeWithoutChildren`. Task 5.3 thus uses `ownerWidth`, as the plan expects. The owner width of each call site:
  - A root: the viewport width (the `availableWidth` of `Calculate`).
  - An in-flow child: the `availableInnerWidth` of the parent (the flex basis, step 6, and the stretch layout of step 7).
  - An absolute child: the padding box width of the parent (`containingBlockWidth` = the measured width minus the two borders).
- Task 5.1, the sum order of the inner width. `PaddingAndBorderForAxis(Row)` is `LeadingPaddingAndBorder(Row) + TrailingPaddingAndBorder(Row)`. Each side is `padding + border`, with the padding first. Thus `paddingAndBorderAxisRow = (leftPadding + leftBorder) + (rightPadding + rightBorder)`. The engine subtracts that sum in one step: `availableInnerWidth = (availableWidth - marginAxisRow) - paddingAndBorderAxisRow`, and the measure callback gets `max(0, (availableWidth - marginAxisRow) - paddingAndBorderAxisRow)`. `ContentInsets.X` holds `leftPadding + leftBorder`, and `ContentInsets.Z` holds `rightPadding + rightBorder`. Thus `rect.Z - (insets.X + insets.Z)` gives the same float as the engine. Group 2 adds the two insets first and then subtracts the sum from the width. The height uses the same order with Y and W.
- Group 2 note: the engine does not round to the pixel grid, so `rect.Z` is `MeasuredWidth`. When the final layout of a leaf has an exact width with no minimum or maximum clamp, `rect.Z - (insets.X + insets.Z)` equals the callback width bit for bit. For an `AtMost` width, the callback gets the limit, and the border box is the content width plus the insets. The draw width is then `(content + sum) - sum`. That value can differ from the callback result by one rounding step.
- `LayoutNode.ContentInsets` (internal `float4`) lies after `ComputedRect`. X is left, Y is top, Z is right, and W is bottom. `ContentBox(in float4)` adds X and Y to the position, and gives `max(w - (X + Z), 0)` and `max(h - (Y + W), 0)` with `MathF.Max`. A `NaN` rect gives a `NaN` size. The layout never gives a `NaN` rect.
- `LayoutInternal` writes the insets in its `if (performLayout)` block, after the size of `ComputedRect`. It uses the `in` overloads of `LayoutMath` and a `new float4`, so it allocates nothing. A measurement does not write the insets.
- Deviation from task 5.3 ("Add no other change"): `ZeroOutLayoutRecursively` also sets `ContentInsets` to `default`. Without it, a hidden element keeps the insets of its last visible layout, but a fresh copy has zeros. The differential test then fails, because a promote can hide a root and a later demote moves it into a tree. A mutation check confirmed it: with no zero-out, `HiddenElementGetsZeroInsets` and `LayoutDifferentialTests` fail. The zero-out matches the rule of the rect.
- A percent padding with an undefined owner width gives 0 (`ResolvePadding` uses `MaxOrDefined(NaN, 0)`). The insets are thus never `NaN`.
- The cache key includes the owner size for an element with a percent padding (`UsesOwnerSize`). A layout cache hit thus has the same owner width as the computed layout, and the hit write gives the same value. A pixel padding does not depend on the owner width. The hit write therefore keeps the value fresh, and it does not change a value in the current cases. It guards a later change of the cache key.
- `ContentInsetsTests` has 13 tests: the pixel padding and border on each edge, `ContentBox` (a layout case, a clamp case, and a border box smaller than the padding), a percent padding of a child, of an absolute child (against the padding box, 210 for a parent of 220 with two borders of 5), and of a root (each edge against the viewport width, also the top edge), a `NaN` viewport width, a new parent width with a clean child, a new viewport at a clean root, a layout cache hit after a measurement with the owner width 300 (the child keeps 10, the value of the final layout with the owner width 100), a hidden element and its subtree (zeros, then the insets come back), and an element before the first layout. Several tests compare the left inset with the position of a child at the flex-start edge.
- Mutation check of the cache-hit test: a write at the start of `CalculateLayoutImpl` for each call (also a measurement) makes `LayoutCacheHitAfterAMeasurementWithAnotherOwnerWidthKeepsTheFinalInsets` and `LayoutDifferentialTests` fail. Both checks ran in the working tree, and the code is back to the final state.
- `LayoutDifferentialTests.Compare` compares the insets of each element after the rect, with the same tolerance. A rect difference stays the first report of an element. The message now says "elements differ" in place of "rects differ". No test reads the message text.
- Blast radius: `LayoutNode` (one internal field and one internal method, 16 more bytes for each element), `LayoutEngine.LayoutInternal` (one write in the layout block), and `LayoutEngine.ZeroOutLayoutRecursively` (one write). No public API changes. The paint walk and `PaintContent` do not change. No expected rect of a fixture changes.
- Task results: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution goes from **963** to **976** (13 in `ContentInsetsTests`), 0 failed. `ThousandElementAllocationTests` and each layout fixture pass.

### Phase 5 Group 1: Follow-up and open points

- Group 2 reads `ContentInsets` through `ContentBox` in `Label.PaintContent` and `Image.PaintContent`, and uses the sum order above for the wrap width.
- `LayoutNode.Rect` is public, but `ContentInsets` and `ContentBox` are internal. A public content box for game code is an open API question for a later phase.

## Phase 5 Group 2: Decisions

- The test total of the solution before the first edit is **976** (0 failed), the total at the end of group 1.
- Task 5.6, the check of `ContentBox(in float4)`: the width is `borderBox.Z - (insets.X + insets.Z)` and the height is `borderBox.W - (insets.Y + insets.W)`. The sum comes first, in the order of the engine. No change was necessary.
- `Label.PaintContent` and `Image.PaintContent` now read `Layout.ContentBox(in borderBox)`. The label sends `SnapWrapWidth(content.Z)`. The phase 1 remarks on the padding are gone from the class remarks and the method comments. The doc comments now name the content box.
- The image keeps its fall-back for a content box with no area: `FitRect` and `CropUv` give the box and the UV rect with no change. The image still sends the call with the empty rect, and the renderer skips it. `DrawsAnEmptyContentBoxWithTheStretchRule` proves it for each scale mode (`Image 8,8,0,0 ... uv 0,0,1,1`).
- Context 2 of the demo: the root label has the exact viewport size on both axes. The engine thus skips its measure callback (`MeasureCount` is 0). The draw still gives `wrap 320`. The test asserts both facts and the rect `0,0,400,300`.
- Mutation check of the sum order: a change of `ContentBox` to `borderBox.Z - insets.X - insets.Z` makes `KeepsTheSameWrapWidthWithAPercentPadding` fail (7.3 % of 333 gives 284.382, and the other order differs by one unit in the last place). The code is back to the final state.
- Task 5.8: at a root width of 150, the content box is 112 by 13 at (7.5, 7.5). The text is 112 pixels wide, so it fits and keeps one line. The test name of the brief stays. The draw still wraps at the content width. ImGui truncates the text position to whole pixels: the glyph vertices are `8,9-116,20`, inside the content box grown by 1 pixel.
- Additions to the test list of the brief:
  - Core: `KeepsTheLinesWithAPercentPaddingForEachWidth` (a theory for `FlexStart` and `Stretch`, each root width from 20 to 400, a padding of 7.3 %). The text at the draw wrap width has the height of the content box and fits its width. `DrawsEachScaleModeInsideItsPadding` (a padding of 10, 20, 30, and 40, so a mix-up of the left and right edges fails). `DrawsAnEmptyContentBoxWithTheStretchRule`. `AddsTheInsetsOfTheParentAndTheLabel` (a padded root with a border and a label with a left and top padding: `Text 9,10 ... wrap 40`).
  - ImGui: `KeepsTheLinesWithAPercentPaddingForEachWidth` (a theory for `FlexStart` and `Stretch`, each root width from 40 to 200, a padding of 5 %). It prints `FlexStart: 161 widths, 85 wrapped, 0 failed` and `Stretch: 161 widths, 85 wrapped, 0 failed`. The ImGui tests compute the content box from the style values (`width * 0.05` on each edge), as the brief states.
- Task 5.9, the results: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution goes from **976** to **992** (13 in `ContentBoxTests` and 3 in `ImGuiLabelTests`), 0 failed. The demo prints these lines:
  ```
  rect 0,0,64,40
  Text 12,12 font 0 size 16 #FFFFFFFF wrap 40 "Hello"
  Text 40,40 font 0 size 16 #FFFFFFFF wrap 320 "Hello"
  ```
- The phase 1 to 4 demos print the same lines as before (`rect 0,0,40,16` and `Text 0,0 ... wrap 40 "Hello"`, `label rect 0,0,63,26` with the verts `1,2-60,24`, the two image lines, and `Text 0,0 ... wrap 128 "Bonjour le monde"`). Each layout fixture and `LayoutDifferentialTests` pass with no change.
- The facts run (Release) prints `facts: tree elements 1000, bytes 746528, bytes per element 746.5`. The same run on an export of `HEAD` (before group 1, in the session scratch folder) prints `bytes 730520, bytes per element 730.5`. The element thus grows by exactly 16 bytes. The callback counts do not change (3031 and 5674).
- Blast radius: `Label.PaintContent` and `Image.PaintContent` only. An element with no padding and no border has zero insets, so its content box equals its border box, and each earlier draw stays the same.

### Phase 5 Group 2: Follow-up and open points

- The facts line of the plain column chain on a 1 MB stack goes from 519 levels (`HEAD`) to 507 levels. The cause is probably the larger stack frame of `LayoutInternal` after the write of group 1. The budget of the stack test is not in the facts report. The phase 5 verifier or phase 6 can decide if the value needs a note.
- A label with a content width of 0 (a padding that fills the box) gets the wrap width 0 from `SnapWrapWidth`, so it draws with no wrap. The measure also gives the size with no wrap for a limit of 0. The two sides agree, but the text then extends past the box. A clip (`Overflow.Hidden`) hides it. A rule for this case needs a decision on the public API.

## Phase 6 Group 1: Decisions

- The test total of the solution before the first edit is **992** (0 failed).
- Task 6.1, the scene of `ElementAllocationTests`: a `RecordingRenderer` is the measurer and the renderer. The viewport is 400 by 3000, so each element lies inside it. The root is a column with `AlignItems = Align.FlexStart`. Each of the 50 labels has a style width of 120 and a padding of 4, so its text wraps in a content box of 112 pixels (14 bytes at size 16). Each of the 50 images has the texture `TextureHandle(7, 16x16)`. The last child is a label with an `LString`.
- The two span texts are `"The first text of a label"u8` (25 bytes) and `"A second text that wraps"u8` (24 bytes). Each one gives two lines at 112. Even frames take the first text and the first tint, odd frames the second, so each frame is a real change. Both texts fit the start buffer of 32 bytes of a label. The recording renderer holds 103 calls (below 256) and about 1200 text bytes. The text buffer thus grows from 1024 to 2048 bytes in the first warm-up frame only. The 3 warm-up frames use both values.
- The string test creates its two strings with `Encoding.UTF8.GetString` of the two span texts, before the loop. This passes a string variable, so `BRUTALCORE0011` does not occur. The test also asserts that the measured frames call the measurer 10 times or more for each frame.
- `RefreshLocalizedTextAllocatesNothing` runs one `Update`, then `Localize("Bonjour")` on the `LString` before the warm-up. The first warm-up refresh shows the new text (the test asserts it). Each measured refresh then takes the reference fast path. The body has no locale change.
- `TheMeasuredBodyDoesRealWork` asserts for each of the 100 frames that the measure count grows by 10 or more, that the first `Text` call holds the text of the frame, and that the first `Image` call has the tint of the frame. The dump of the last frame holds 51 `Text` lines and 50 `Image` lines, and its line 1 is `Text 4,4 font 0 size 16 #FFFFFFFF wrap 112 "The first text of a label"`. The first label is 120 by 40. The span test prints `frames 100, bytes 0, measures 2000` (2 measure calls for each changed label).
- Task 6.2: `LabelUpdateAndDrawAllocateNothingAfterWarmUp` uses the same scene with `ImGuiRenderer` and size 13 (the size of the default font of the harness, 16 glyphs in 112 pixels). The harness display and the viewport are 400 by 2600. The images use one texture of the texture service (`TestTexture(4660)`, 16 by 16). Each frame measures two regions: the changes and `Update` before `BeginFrame`, and `Draw` between `BeginFrame` and `EndFrame`. It prints `frames 100, bytes 0`. The test also asserts the text and the tint of the last frame and the label height of 34 (two lines of 13 and the padding). The scene helpers of the ImGui test have the prefix `LabelScene` and write `BrutalUi.Core.Image` in full.
- Task 6.3, the planted allocation: the copy is `Label.cs.orig` in the session scratch folder. `diff` of the copy gave no output before the edit. With `GC.KeepAlive(new object());` at the start of `MeasureLabel`, `dotnet test --solution brutal-ui.sln` gave **exit=2**: 3 failed of 997 (`UpdateAndDrawAllocateNothingAfterWarmUp`, `SetTextFromAStringAllocatesNothingAfterWarmUp`, and the ImGui `LabelUpdateAndDrawAllocateNothingAfterWarmUp`). `ThousandElementAllocationTests` does not fail, because its text leaves use `TextMeasure`, not `Label`. After the copy back, `diff` gave no output, and the tests gave **exit=0** (997 passed, 0 failed).
- Task 6.4, 5 runs: each run of `--filter-class 'BrutalUi.Core.Tests.ElementAllocationTests'` gave `total: 4, failed: 0`, and each run of `--filter-method '*LabelUpdateAndDrawAllocateNothingAfterWarmUp'` gave `total: 1, failed: 0`. The demo command as written (`--filter-class '*ElementAllocationTests'`) runs `ElementAllocationTests` (4) and `ThousandElementAllocationTests` (8): `total: 12, failed: 0`. No class got a new name.
- `scripts/test-linux.sh` printed `test-linux: PASS build, test, gpu, smoke (120 frames) in 13 s` and `exit=0`.
- The results: the build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution goes from **992** to **997** (4 in `ElementAllocationTests` and 1 in `ImGuiRendererAllocationTests`), 0 failed.
- Blast radius: two test files. No file in `src/` changes. `Label.cs` is the same as the copy.

### Phase 6 Group 1: Follow-up and open points

- The demo filter `*ElementAllocationTests` also matches `ThousandElementAllocationTests`, so the demo total is 12, not 4. The verifier and `human-testing.md` can use the full class name `BrutalUi.Core.Tests.ElementAllocationTests` for a total of 4.

## Phase 6 Group 2: Decisions

- The group changes docs only. No file in `src/` and no test changes.
- Task 6.5, `docs/technical-design.md`. Section 5 holds the shapes of `UiContext` (the create-info, `Measurer`, and `RefreshLocalizedText`), `Label`, `ImageScaleMode`, and `Image`. Four paragraphs follow: the measurer of the context, the dirty rules of the label and the image, the text of a label (`BRUTALCORE0011` and the ambiguous `SetText(null)`), and the locale refresh. The design decisions name the buffer growth and the `required` rule of the create-info structs. Section 6, "The measure callback", holds the measure scope, the write point of `ContentInsets`, `ContentBox`, and the 16 bytes (730.5 to 746.5 bytes per element). Section 8 has a new subsection "Label and image": the measurer slot, the label measure rule, the draw rule, the snap rule, the image rules with the scale mode table, and the limits. Section 14 has three new bullets: the element tests, the ImGui label tests, and the third allocation gate.
- The code is the source of truth. The section 5 shape lists the members of the code. The `Image.Texture` dirty rule follows the code (a new id with the same size marks `Paint` only), not the plan text "a new id or size: Layout | Paint".
- Two edits outside the four named points, to keep the design doc true:
  - Section 6, the stack paragraph: the Release value of the plain column chain goes from 519 to 507 levels (the phase 5 facts run). The Debug value 292 stays, because no run measured it again.
  - Section 8, rule A7 of the draw walk: the old text "`LayoutNode` stores no resolved border today" is no longer true. The new text states that `ContentInsets` holds the padding plus the border, so the walk does not read it.
- Section 8, "The measurer slot": the measure runs outside the ImGui frame, so `Update` can run before `ImGui.NewFrame`. The paragraph states that `Draw` runs between `ImGui.NewFrame` and `ImGui.Render`. The "Handle errors" paragraph still says "The game calls `Update` and `Draw` between `ImGui.NewFrame` and `ImGui.Render`". Both texts allow the order of the ImGui allocation test. The paragraph of the imgui-draw-list feature does not change.
- Task 6.6: one `### Added` line in `CHANGELOG.md`. It also names `ElementAllocationTests`, as the renderer-interface line names its draw gate. The `### Benchmarks` facts block keeps the run of 2026-10-18 (696.9 bytes per element and 519 levels). A new benchmark run is the work of the hello-panel-sample hand-off.
- Task 6.7: the `## label-and-image` section of `human-testing.md` holds the phase 1 to 6 demo lines, each one time, and the Windows line.
- Task 6.8: `ste-lint.py --fail-over 2.5` gives exit 0 for each changed doc and for `implementation.md` (see the results below). `implementation.md` gets no edit.
- The results: the lint gives `technical-design.md` 1.35, `CHANGELOG.md` 1.79, `human-testing.md` 1.51, `context.md` 1.41, and `implementation.md` 1.47 (each exit 0). The build prints `0 Warning(s)` and `0 Error(s)`. The test total of the solution stays **997**, 0 failed.

### Native Dependencies

None. This feature adds no package, no `Directory.Packages.props` line, and no native file.

### Hand-offs

**hello-panel-sample**
- Create the renderer first. Then create the context with `new UiContext(new UiContext.CreateInfo { Measurer = renderer })`. The measurer and the renderer of `Draw` must use the same font ids. Pass the renderer that draws.
- Call `Update` before `Draw`. The rule for the frame: call `Draw` between `ImGui.NewFrame` and `ImGui.Render`. `Update` can run before `ImGui.NewFrame`, because the measure needs a current ImGui context only. `LabelUpdateAndDrawAllocateNothingAfterWarmUp` runs `Update` before `BeginFrame` and `Draw` inside the frame.
- A label needs `Color` (required). An image needs `Texture` (required) from `ImGuiTextureService.AddVulkan`, which carries the size. The none handle is valid: the image then takes no space and draws nothing.
- Write a literal text as `label.SetText("Hello"u8)`. The analyzer `BRUTALCORE0011` rejects a string literal. Cast a null text to `string?` or `LString?`.
- After a locale change, call `context.RefreshLocalizedText()`. `Update` does not call it.
- A context from `new UiContext()` has no measurer. Its `Update` throws `InvalidOperationException` when a label needs a measure.
- Record the new facts lines. The element grows by 16 bytes (`ContentInsets`): `facts: tree elements 1000, bytes 746528, bytes per element 746.5` (`HEAD` before phase 5: 730.5). The plain column chain on a 1 MB stack passes 507 levels in Release (519 before phase 5). The callback counts stay 3031 and 5674. The `### Benchmarks` block of `CHANGELOG.md` still shows the run of 2026-10-18 (696.9 bytes per element).

**The style-system epic**
- `Label.Color`, `Label.Font`, and `Label.Size` are properties on the element, not style fields. The cascade can write them, or a later change can move them into `ComputedStyle`. Each setter compares the old value and marks the dirty bits itself (`Font` and `Size`: `Layout | Paint`, `Color`: `Paint`).
- A label has no text alignment and no vertical alignment inside a taller box (plan gap 2).

**Open points for a later epic**
- An image with a style width and an auto height keeps its natural height, so its aspect changes. An aspect rule for the measure needs a decision on the public API (phase 3).
- The `ScaleToFit` rect can have fractional pixels, and the image does not snap it. A pixel snap rule for images needs a decision (phase 3).
- An image draws with square corners. The content box needs its own radius (plan gap 3).
- A label with a content width of 0 draws with no wrap, and the text extends past the box. A rule needs a decision on the public API (phase 5).
- An empty label measures as one line: `(0, size)`. A height of 0 for an empty label needs a decision (phase 1).
- The label does not check `Size` (a negative or `NaN` size goes to the renderer) and does not check the UTF-8 bytes of `SetText(ReadOnlySpan<byte>)` (phases 1 and 4).
- `RefreshLocalizedText` knows the `Label` type only. A later text element (for example `Button`) needs a virtual hook on `Element` or a type check (phase 4, plan gap 6).
- `ContentInsets` and `ContentBox` are internal. A public content box for game code is an open API question (phase 5).
- The space rule of `RecordingRenderer` differs from ImGui at a line break. A Core test with a space at the end of a wrapped text can thus differ from the ImGui result (phase 2).
- The ImGui sweeps use the monospace default font. A sweep with a font with fractional advances (for example `GlyphExtraAdvanceX` of 0.5) and a size such as 13.5 can follow (phase 2).
- No single-line mode, no label measure cache, and no formatted `LString` (plan gaps 1, 4, and 5).
- The demo filter `*ElementAllocationTests` also matches `ThousandElementAllocationTests` (phase 6).

---

## Review Group 1: Decisions

The group fixes CR-1, CR-5, and CR-7 of `code-review.md`.

- Blast radius. The change touches `Label.PaintContent` and `Label.MeasureLabel` only. `PaintWalk` calls `PaintContent` with the same border box as before. No public member changes. The new `Label.DrawWrapWidth` is internal. `ImGuiRenderer`, the layout engine, and `RecordingRenderer` do not change.
- CR-1, the draw raise. `Label.DrawWrapWidth(contentWidth, borderBoxWidth)` snaps the content width with `SnapWrapWidth`. A result of 0 or a whole number stays the same. A fractional result gets `2 * (MathF.BitIncrement(borderBoxWidth) - borderBoxWidth)` on top. This follows the orchestrator rule: each whole-pixel wrap width stays exact, so the demo lines `wrap 40`, `wrap 96`, `wrap 128`, and `wrap 320` do not change.
- CR-1, the bound. The engine gives the box `R = w + s`, with `s = leading + trailing` in the same order as `insets.X + insets.Z`. The draw computes `c = R - s`. Each operation adds at most half a unit in the last place of `R`, so `|c - w|` is at most one unit. The raise of two units keeps the draw width at `w` or above.
- CR-1, an infinite raise. `BitIncrement(float.MaxValue)` gives infinity. A raise that is not finite gives the snapped width, so the renderer never gets an infinite wrap width.
- CR-1, the contract change of the review (whole-pixel widths from `MeasureText`) is not taken. It changes the public interface.
- CR-1, the theory `KeepsTheLinesWithAFractionalMeasuredWidth` in `ContentBoxTests` uses size 13.5, a padding of 7.3 %, and each root width from 20 to 400. With the raise removed, the `FlexStart` row fails at 9 widths (checked one time, then restored). The `Stretch` row passes with and without the raise, because the measure and the draw get the same bits under `Exactly`. A fact `RaisesAFractionalDrawWrapWidthOnly` pins the helper.
- CR-1, `KeepsTheSameWrapWidthWithAPercentPadding` now asserts that the draw width is in the range from the measure width to the measure width plus the noise. It also asserts that the draw raises the width.
- CR-5. `MeasureLabel` passes the result with no wrap and the wrapped result through the private `FiniteSize`. A component that is not finite, or that is below 0, becomes 0. The check runs before the compare `free.X <= limit`, so a `NaN` width no longer sends a wrapped measure. The theory `GivesAFiniteSizeForABadMeasurerResult` in `LabelTests` uses a stub measurer (`FixedMeasurer`) with six rows: the path with no wrap under `AtMost` and under `Undefined`, and the wrapped path.
- CR-7. `KeepsTheAtMostContractAtAnExactFit` in `LabelWrapTests` wraps the measure callback of the label to count the calls. It asserts the rect `0,0,144,16`, an `AtMost` call at the limit 144, a `LastMeasureWrapWidth` of 0, and one `MeasureText` call for each callback.
- The test total goes from 997 to 1007 (2 + 1 rows for CR-1, 6 rows for CR-5, 1 for CR-7).

### Follow-up work

- Limit (2) of the draw raise stays open. For a measurer with fractional widths, a measured width within 1/64 pixel above a whole number snaps down in the draw, and the widest line can wrap. Example: a widest line of 80.01 pixels under a limit of 96 draws at a wrap width of 80. The draw cannot tell `AtMost` from `Exactly`, so a fix in `PaintContent` alone breaks the `Exactly` case. Two fixes are possible: a snap that rounds down only by a few units in the last place, or the contract change of the review. Both change the snap rule, so they need a decision. `ImGuiRenderer` rounds each width up, so the shipped pair is safe. `RecordingRenderer` at size 13.5 gives widths with the fractions .25, .5, and .75 only, so the new theory does not reach this limit.

## Review Group 2: Decisions

The group fixes CR-4, CR-8, and CR-9 of `code-review.md`. It changes docs, the `Label` class remarks, and one test.

- CR-4. The class remarks of `Label` have a new paragraph with the limit. The "Limits" paragraph of section 8 has the same sentences. The fact `PinsTheMinimumWidthAboveTheAvailableWidth` in `LabelWrapTests` pins the probe: a root of width 100, `MinWidth` 150, and `Align.FlexStart` give the rect `0,0,150,32`. The measure wraps at 100. The draw wraps at 150 and gives one line (`144x16`). The code does not change.
- CR-8. The "Handle errors" paragraph now states the rule: the game calls `Draw` between `ImGui.NewFrame` and `ImGui.Render`, and `Update` can run before `ImGui.NewFrame`. The hand-off of `hello-panel-sample` above states the same rule. The note in "Phase 6 Group 2: Decisions" is a record of an earlier state, and it stays as it is.
- CR-9. Section 14 now states "the text of 10 labels and the tint of 10 images". The `Label` remarks now say "when the label needs a measure". Review group 5 (CR-3) updates the section 14 text again when it widens the gate.
- The test total goes from 1007 to 1008.

## Review Group 3: Decisions

The group fixes CR-2 of `code-review.md`. It adds two tests to `LabelTests`. The code does not change.

- Blast radius. The change touches `LabelTests` only. `UiContext.cs` changed for the proof only, and it is the same as before (checked with `diff` against a copy in the session scratch folder).
- CR-2, test 1. `TheScopeIsEmptyAfterAnExceptionWithAMeasurer` uses a context with a `RecordingRenderer` measurer and a `ThrowingProbe` leaf. After `Assert.Throws`, `UiContext.CurrentMeasurer` is null.
- CR-2, test 2. `ANestedUpdateThatThrowsKeepsTheOuterScope` uses an outer tree with a `CatchingProbe` and a later label. The probe runs `Update` of an inner context in a `try` and `catch`, and the inner tree holds a `ThrowingProbe`. The test asserts that the probe caught the exception, that the probe reads the outer measurer after the catch, that the later label measures with the outer measurer (rect width 40), that the inner measurer measures nothing, and that the scope is null after the outer update.
- The probes throw a private `MeasureProbeException`. No other code throws it, so `Assert.Throws` and the `catch` cannot get an exception from another source.
- The summary of `TheScopeIsEmptyAfterUpdate` now states that its exception case uses a context with no measurer, and it points to the new test.
- The proof. With no `try` and no `finally` in `Update` (the restore after the loop stays), both new tests fail. `ANestedUpdateKeepsTheOuterScope` also fails in that run. It reads the measurer that leaked from an earlier test on the same thread. Thus a missing `finally` also leaks the scope into later tests of the same thread.
- The test total goes from 1008 to 1010.

## Review Group 4: Decisions

The group fixes CR-6 of `code-review.md`. An image with a UV component that is not finite now draws nothing.

- Blast radius. The change touches `Image.PaintContent`, the doc comments of `Image.Uv`, `Image.CreateInfo.Uv`, and `PaintContent`, `ImageTests`, and one sentence in the image rules of section 8. No public signature changes. The measure does not change: a span that is not finite still gives 0 on its axis.
- CR-6. `PaintContent` checks each UV component with `float.IsFinite` after the texture check and before the tint, the content box, and the scale mode rules. The check allocates nothing.
- The setter does not throw. A throw changes the public API, so the group does not take that option.
- The theory `DrawsNothingForAUvThatIsNotFinite` has one row for each scale mode: `NaN` for u0 with `StretchToFill`, positive infinity for u1 with `ScaleToFit`, and negative infinity for v0 with `NaN` for v1 with `ScaleAndCrop`. Each row uses a style size of 100 by 50, as probe 8 does. Each row also sets the UV back to (0, 0, 1, 1) and asserts that the image draws again.
- The phase 3 demo prints the same two image lines as before.
- The test total goes from 1010 to 1013.

## Review Group 5: Decisions

The group fixes CR-3 of `code-review.md`. It widens the two allocation gates. The code of `src/` does not change.

- Blast radius. The change touches `ElementAllocationTests`, `ImGuiRendererAllocationTests`, and the gate sentence of section 14. `Label.cs` and `Image.cs` changed for the plants only. After each plant, a copy in the session scratch folder restored the file, and `diff` with the copy gave no output. The SHA-256 of each file is the same as before the plants.
- The Core frame. On 10 labels, the frame sets the text, the color, the size (16 and 14), and the font (id 0 and id 1). On 10 images, it sets the tint, the UV rect, the texture (id 7 and id 8, both 16 by 16), and the scale mode (`ScaleToFit` and `ScaleAndCrop`). On the last label, it sets one of two `LString` instances ("Hello" and "World", the same length). The even frames take the first values, the odd frames the second values. The warm-up of 3 frames uses both values.
- The image width. Each image of both scenes now has a style width of 32. The image measure gives the height: 16 for the UV rect (0, 0, 1, 1), 8 for the UV rect (0, 0, 1, 0.5). Thus a test can see that the measure ran. The box is wider than the image, so the two scale modes give two different draws: `ScaleToFit` draws a rect of 16 by 16 in the center, and `ScaleAndCrop` draws the full box with the UV rect (0, 0.125, 1, 0.375).
- The two sizes. At size 14, `RecordingRenderer` puts 16 bytes on a line of 112 pixels. Both texts wrap to two lines at size 16 and at size 14. The ImGui gate uses size 13 and size 12. Both texts wrap to two lines at both sizes, and the box height is `2 * size + 8` (checked in each measured frame).
- The ImGui gate has one font, so it does not change the font. It registers a second texture of the same size (`TestTexture(4661)`).
- The refresh test. Two strings of the same length ("Bonjour" and "Bonsoir") exist before the loop. Before each refresh, the test calls `Localize` with the other string, outside the measured region. Each measured refresh then finds a new reference with a new content, so it takes the slow path. After each refresh, the test checks the new text, runs `Update` outside the measured region, and checks that the layout pass measures again.
- Real work. `TheMeasuredBodyDoesRealWork` checks in each measured frame: the text, the color, the size, the font, and the wrap width of the first text call, the label height for the size, the text of the `LString` label, the tint and the texture id of the first image call, the image height for the UV span, and the rect and the UV rect of each scale mode. The ImGui gate checks the element values, the label height, and the image height in each measured frame. The checks run outside the measured regions.
- The plants. Each plant was `GC.KeepAlive(new object());`, one path at a time. Each run used the Core class `ElementAllocationTests` and the ImGui class `ImGuiRendererAllocationTests`.
  - `Image.MeasureImage`: 1 Core failure (`UpdateAndDrawAllocateNothingAfterWarmUp`) and 1 ImGui failure (`LabelUpdateAndDrawAllocateNothingAfterWarmUp`).
  - The `Image.Uv` setter: 1 Core failure and 1 ImGui failure, the same tests.
  - The `Label.Color` setter: 1 Core failure and 1 ImGui failure, the same tests.
  - The `Label.Size` setter: 1 Core failure and 1 ImGui failure, the same tests.
  - `SetText(LString)`: 1 Core failure and 1 ImGui failure, the same tests.
  - The slow path of `Label.RefreshLocalized`: 1 Core failure (`RefreshLocalizedTextAllocatesNothing`) and 0 ImGui failures. The ImGui gate does not call `RefreshLocalizedText`.
- The brief names six plants. The `Texture`, `ScaleMode`, and `Font` setters had no plant. Each runs in a measured frame, and the real-work checks show that each value changes in each frame.
- The test total stays at 1013. The group adds no test. It widens four existing tests.

---

## Gotchas & Learnings

- **The engine skips a measure for `Exactly` on both axes.** Problem: a root label of the viewport size never measures. Solution: each measure test puts the label in a root with `AlignItems = Align.FlexStart`. Lesson: check the measure count, not only the rect.
- **`BRUTALCORE0011` rejects a string literal for `SetText`.** Problem: the span overload exists, so the BRUTAL analyzer gives an error for `SetText("Hello")`. Solution: game code writes `SetText("Hello"u8)`, and a test passes a string variable. Lesson: a bare `SetText(null)` is ambiguous too. Cast it.
- **A wildcard class filter matches a longer class name.** Problem: `--filter-class '*ElementAllocationTests'` also runs `ThousandElementAllocationTests`. Solution: use the full name `BrutalUi.Core.Tests.ElementAllocationTests`. Lesson: give a demo the full class name.
- **A missing `finally` leaks the measure scope into later tests.** Problem: with no `finally`, a test on the same thread read a measurer of an earlier test. Solution: the `finally` of `Update` and the two CR-2 tests. Lesson: a thread-static scope needs a test with a measurer that throws.
- **The float noise of `(w + sum) - sum`.** Problem: the draw width can be one unit in the last place below the measured width. Solution: the snap rule for a whole-pixel width, and the raise of `DrawWrapWidth` for a fractional width. Lesson: compute the content width in the order of the engine.

---

## API Integration

- `Label` calls `IUiRenderer.MeasureText` through the measurer slot of `UiContext`, and `IUiRenderer.Text` in the draw. `Image` reads `TextureHandle.Size` and calls `IUiRenderer.Image`.

---

## State Management

- `UiContext` owns the measurer slot. `Update` puts it into a thread-static scope for the layout pass. `Label` owns a UTF-8 buffer that grows and never shrinks.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/*.cs`, `tests/BrutalUi.Core.Tests/Layout/ContentInsetsTests.cs`, `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`
- The measure rules, the draw rules, the dirty rules, the locale refresh, the content insets, and the allocation gate, with `RecordingRenderer`.

### Integration Tests
**Files:** `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`, `ImGuiImageElementTests.cs`, `ImGuiRendererAllocationTests.cs`
- The real ImGui measure and draw on a headless context.

### Manual Testing Checklist
- [ ] (MANUAL) `scripts\test-windows.ps1` on Windows.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAOneLineLabel' --show-live-output on` and reads `rect 0,0,40,16`, then the three lines `Begin 400x300 scale 1`, `Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, and `End`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj -- --filter-method '*WrapsALabelAtItsWidth' --show-live-output on` and reads `label rect 0,0,63,26`, then one command line of the background list whose glyph vertices lie inside `0,0-64,27`.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsAnImageAtItsTextureSize' --show-live-output on` and reads `Image 0,0,64,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0`, then `Image 0,57,100,50 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0` for the fit image in a 100 by 100 box.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ShowsTheNewLocaleAfterARefresh' --show-live-output on` and reads `rect 0,0,40,16` and `dirty Layout, Paint`, then `rect 0,0,128,16` and `Text 0,0 font 0 size 16 #FFFFFFFF wrap 128 "Bonjour le monde"`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsTheLabelInsideItsPadding' --show-live-output on` and reads `rect 0,0,64,40` and `Text 12,12 font 0 size 16 #FFFFFFFF wrap 40 "Hello"`, then `Text 40,40 font 0 size 16 #FFFFFFFF wrap 320 "Hello"` for the root label with a padding of 10 %.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ElementAllocationTests'` and reads a total of 4 with `failed: 0`. The wildcard also matches `ThousandElementAllocationTests`, so this command reads a total of 12. The full name `--filter-class 'BrutalUi.Core.Tests.ElementAllocationTests'` reads a total of 4.
- [ ] Windows run (label-and-image): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- `Update` and `Draw` must allocate 0 bytes after warm-up with labels and images. Phase 6 gates it.
- The element grows by 16 bytes for `ContentInsets` (phase 5).

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the 6 phase demos and the Windows run of "Needs Human Verification".
2. `/autorun-epic` commits the feature and goes on to hello-panel-sample from its draft plan.

### Short Term
1. hello-panel-sample uses the hand-offs of the section "Hand-offs".
2. `/review-epic imgui-renderer` reads the open points of the hand-offs.

---

## Open Questions

- [ ] **Q:** Should `IUiRenderer.MeasureText` give whole-pixel widths? A fractional measurer can still wrap a widest line within 1/64 pixel above a whole number (CR-1 limit, `## Review Group 1: Decisions`). `ImGuiRenderer` rounds each width up, so it is safe today. The answer changes a public contract. Open for a later renderer.
- [ ] **Q:** Should a minimum width above the available width give the label the lines of the box? Today the box gets the lines of the available width, and the draw can use fewer lines (CR-4, pinned by a test). Open.

---

## Session Notes

### 2026-10-20 20:24
- `/start-feature` scaffolded `context.md`, `tasks.md` (42 tasks in 6 phases), and `inbox.md`.

### 2026-10-20 20:32 (phase 1)
- Phase 1 done in one group (`game-dev-advanced`). The verifier gave PASS: build 0 warnings and 0 errors, 877 tests and 0 failed, no ImGui reference in Core code, no diff in the pins, `src/BrutalUi.ImGui`, `samples`, or `scripts`. Demo: PASS.
- The inbox has no line.

### 2026-10-20 20:43 (phase 2)
- Phase 2 done in two groups (`game-dev-advanced`). The verifier gave PASS: build 0 warnings and 0 errors, 901 tests and 0 failed, the benchmark builds, the phase 1 demo prints the same lines, and the sweep has no failed width. Demo: PASS (`label rect 0,0,63,26`, glyph vertices `1,2-60,24`).
- The inbox has no line.

### 2026-10-20 20:49 (phase 3)
- Phase 3 done in one group (`game-dev-advanced`). The verifier gave PASS: build 0 warnings and 0 errors, 929 tests and 0 failed, the ImGui image dump holds `tex id 4660` and `0,0-64,32`. Demo: PASS.
- The plan has two texts for the `Texture` dirty rule. "The Public API" says "a new id or size: Layout | Paint". Task 3.4 says "a new id with the same size marks `Paint` only". The code follows task 3.4, and a change to or from the none handle marks `Layout | Paint` (see `## Phase 3: Decisions`).
- The inbox has no line.

### 2026-10-20 20:57 (phase 4)
- Phase 4 done in one group (`game-dev-advanced`). The verifier gave PASS: build 0 warnings and 0 errors, 963 tests and 0 failed, no use of the static `LStrings` class in the element tests, and no probe file in the repository. Demo: PASS.
- The probe lives in the session scratch folder, outside the repository. `LString.Localized` allocates nothing, so phase 6 keeps `RefreshLocalizedTextAllocatesNothing`.
- The BRUTAL analyzer `BRUTALCORE0011` rejects `SetText("literal")`, because the span overload exists. Game code writes `SetText("Hello"u8)`. A bare `SetText(null)` is ambiguous. The cross-phase review looks at the overload set.
- The inbox has no line.

### 2026-10-20 21:13 (phase 5)
- Phase 5 done in two groups (`game-dev-advanced`). The verifier gave PASS: build 0 warnings and 0 errors, 992 tests and 0 failed, the phase 1 to 4 demos print the same lines, the layout diff adds only the inset compares, and the write sits in the `if (performLayout)` block. Demo: PASS.
- Group 1 also zeroes `ContentInsets` in `ZeroOutLayoutRecursively`, so a hidden element matches a fresh layout. A mutation check proved the need.
- The facts line of the plain column chain went from 519 to 507 levels (see `## Phase 5 Group 2: Decisions`).
- The inbox has no line.

### 2026-10-20 21:26 (phase 6)
- Phase 6 done in two groups (`game-dev-advanced`). The verifier gave PASS: build 0 warnings and 0 errors, 997 tests and 0 failed, `ImGuiRendererAllocationTests` 4 of 4, `scripts/test-linux.sh` PASS, each lint exit 0, I1 exit 0, and no planted line left in `Label.cs`. Demo: PASS.
- The demo filter of the plan, `*ElementAllocationTests`, also matches `ThousandElementAllocationTests` and reads 12. The plan keeps its text. The demo line in `human-testing.md` and in this file states both totals.
- Group 2 put the phase 6 demo line and the Windows line into `human-testing.md` and into this file.
- The inbox has no line.

### 2026-10-20 22:13 (`/update-feature`)
- Progress: 42/42 tasks, all 6 phases, and the cross-phase review (9 findings, 9 resolved). The total is 1013 tests, 0 failed, 0 warnings. `scripts/test-linux.sh` passes.
- Decisions: the changes from the plan, the review rules, and the fixes of CR-1 and CR-6 are in "Important Decisions".
- The draft plan of hello-panel-sample goes to `docs/features/imgui-renderer/hello-panel-sample/implementation.md` (the `solution-architect` agent). The lint gives 1.53 per 100 words.
- Inbox: the file holds no line. No line is open, and no bug is promoted.
- Blockers: none.
- The next session: a person runs "Needs Human Verification". `/autorun-epic` commits the feature and goes on to hello-panel-sample.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
