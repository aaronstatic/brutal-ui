# imgui-renderer/label-and-image - Code Review

**Reviewed:** 2026-10-20
**Reviewer:** Agent (cross-phase review of phases 1 to 6)
**Rules Applied:** 1. The measure and draw rules, 2. Zero allocation, 3. The dirty bits, 4. The measure scope, 5. API clarity, 6. The layout engine stays exact, 7. Test quality, `AGENTS.md`, and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 9 findings, 9 resolved. Review groups 1 to 5 fixed each finding. CR-1 keeps one documented limit for a fractional measurer (see `## Review Group 1: Decisions` of `context.md`). Verifier `review-verify.md`: PASS (1013 tests, 0 failed, 0 warnings, `scripts/test-linux.sh` PASS) (2026-10-20).

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 3 |
| LOW | 6 |

The label and the image follow their rules for each normal input. The dirty bits are right, and a shipped test kills each mutation of a dirty bit. The content insets are exact, also for a cache hit and a hidden subtree. The ImGui pair (`ImGuiRenderer` as the measurer and the renderer) keeps the line rule, because ImGui rounds each width up to a whole pixel.

The three MEDIUM findings are these. A renderer that measures a fractional width loses the line rule when the label has a percent padding (CR-1). No shipped test fails when the `finally` of the measure scope goes away (CR-2). The allocation gates do not reach the image measure, five setters, and the slow path of the locale refresh (CR-3). The LOW findings are edge values, one test gap, and two doc gaps.

All probes ran in a scratch copy of the repository at `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/3877ec01-edbe-4549-8fe2-adfb5451e35e/scratchpad/cr-label-and-image/repo`. The copy has a build property `ReviewMutant` in `Directory.Build.props`, so one build switches on one `#if` mutation. Two scratch test classes hold the probes: `ReviewProbeTests` (Core) and `ReviewProbeImGuiTests` (ImGui). No file of the repository changed, except this file.

1. **Baseline.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 997, failed 0.
2. **Mutations that a shipped test kills.** Each mutation had its own build. The full suite then ran (1011 tests with the probes).
   - Always wrap at the limit (no `free.X <= limit` return): `LabelWrapTests.KeepsTheAtMostContract` fails.
   - No snap in `SnapWrapWidth`: 4 failures in 3 tests (the Core percent sweep, the `SnapWrapWidth` theory, and the ImGui percent sweep).
   - `Size` marks `Paint` only: 2 failures. `Font` marks `Paint` only: 1 failure.
   - `ApplyText` with no UTF-8 byte compare: `SetTextFromTheSameStringMarksNothing` fails.
   - `MeasureAxis` gives the limit for `AtMost`: 13 failures.
   - `CropUv` with the inverse ratio on the u span: 4 failures. `CropUv` with no ratio on the v span: 2 failures (the unit tests only).
   - `Uv` marks `Paint` only: 1 failure. A new texture size marks `Paint` only: 1 failure. `FitRect` with no horizontal center: 1 failure.
3. **Mutations that no shipped test kills.** `free.X < limit` in place of `free.X <= limit`: failed 0 (see CR-7). `Update` with no `try` and `finally` (the restore after the loop only): failed 0 for the shipped tests, and the two scope probes fail (see CR-2). One full run of this mutation also had one failure of `TreeAllocationTests.TreeChangesAndUpdateAllocateNothingAfterWarmUp`. Three runs of that class on the same build and three on the baseline pass. The mutation removes no allocation, so the failure looks flaky. It is outside this feature and is not reported.
4. **The measure scope after an exception and in a nested update.** A callback that throws in a context with a measurer: after the catch, `UiContext.CurrentMeasurer` is null. An outer callback runs `Update` of a second context, the inner callback throws, and the outer callback catches it: the outer scope comes back, and a later label of the outer tree measures with the outer measurer. The code is right.
5. **A fractional measured width under `AtMost` with a percent padding.** `RecordingRenderer`, size 13.5 (6.75 pixels for each byte), each root width from 20 to 400. `FlexStart` with a padding of 7.3 %: 9 of 381 widths draw more lines than the box holds. The first is width 135: content `114.74999` by 27 (2 lines), draw wrap `114.74999`, drawn `81` by `40.5` (3 lines). `Stretch` with 7.3 %: 0 failures. A pixel padding of 3.3: 0 failures for both. See CR-1.
6. **A minimum width above the `AtMost` limit.** A root of width 100, a label with `MinWidth = 150` and a text of 144 pixels: the rect is `0,0,150,32` (two lines), and the draw at 150 gives one line (`144x16`). See CR-4.
7. **A label size of `NaN`, a negative size, or infinity.** `RecordingRenderer`: `NaN` and `-16` give the rect `0,0,0,0`. Infinity gives the rect `0,0,Infinity,0`. `ImGuiRenderer`: `NaN`, `-16`, infinity, and 0 use the default size of the font, and each gives the rect `0,0,63,26` and the dump of the phase 2 demo. See CR-5.
8. **Image edge values.** A `NaN` or infinite UV component gives 0 on its axis of the natural size. With a style size of 100 by 50, each scale mode sends the `NaN` or infinite UV to the renderer, and ImGui writes a quad (`tex id 4660 elems 6 verts 0,0-100,50`). See CR-6. A texture size of `int.MaxValue` gives finite values: the flex pass clamps the box to the viewport, and `ScaleToFit` draws `0,50,400,200` for the UV `-1,0,1,1`. A UV span of `1e-30` gives a `ScaleToFit` rect with a height of about `1e-28`, and the renderer skips it. A content box with no area gives `Image 8,8,0,0 texture 7 uv 0,0,1,1` in each mode. No probe gave an exception or a `NaN` rect.
9. **`RefreshLocalizedText` on a root label and on a deep chain.** A label as a root gets `Layout, Paint`, and the draw is `Text 0,0 font 0 size 16 #FFFFFFFF wrap 400 "Bonjour le monde"`. A chain of 500 and of 5000 elements on a thread with a 1 MB stack: the refresh reaches the leaf label. The probe did not run 50000 levels, because a stack overflow ends the process. The layout stops near 507 levels, so the refresh is not the depth limit.
10. **The gates with planted allocations.** Each plant had its own build, and the full suite ran.
    - `GC.KeepAlive(new object())` in `Image.PaintContent`: 2 failures (`ElementAllocationTests.UpdateAndDrawAllocateNothingAfterWarmUp` and the ImGui `LabelUpdateAndDrawAllocateNothingAfterWarmUp`).
    - The same plant in `SetText(string)`: 1 failure. At the start of `RefreshLocalizedText`: 1 failure.
    - The same plant in the slow path of `Label.RefreshLocalized`, in `Image.MeasureImage`, in the `Image.Uv` setter, and in the `Label.Color` setter: failed 0 for each. See CR-3.
11. **Rule checks.** `BrutalUi.Core.csproj` references `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization` only. The feature changes no `.csproj` and no line of `Directory.Packages.props`. No new file of `src/BrutalUi.Core` names ImGui. Each new file uses a file-scoped namespace, `Brutal.Numerics` types, and UTF-8 text. Section 5 of `docs/technical-design.md` lists the members of the code. `LayoutDifferentialTests.Compare` compares the insets after the rect.

### Known items from the build

1. **An empty label measures to the height of one line.** Not reported.
2. **The label does not check `Size`.** Probe 7 adds a new angle: the callback gives the engine a result that is not finite, against the contract of `MeasureCallback`, and the label gets an infinite rect. The fault does not need a bad `Size`: any measurer result that is not finite does the same. See CR-5.
3. **A label with a content width of 0 draws with no wrap.** Not reported.
4. **The image measure treats each axis on its own.** Not reported.
5. **A `ScaleToFit` rect has no snap to whole pixels.** Not reported.
6. **`SetText("literal")` gives `BRUTALCORE0011`, and a bare `SetText(null)` is ambiguous.** Not reported.
7. **The two plan texts for the `Texture` dirty rule.** The code follows task 3.4. Section 5 and the doc comment follow the code. Not reported.
8. **`RefreshLocalizedText` knows the `Label` type only, and it has no guard against a call during `Draw`.** Not reported.
9. **No single-line mode and no text alignment.** Not reported.
10. **The facts line of the plain column chain went from 519 to 507 levels.** Section 6 states it. Not reported.
11. **The callback casts with `(Label)element`.** Not reported.
12. **The demo filter `*ElementAllocationTests` reads 12, not 4.** `human-testing.md` and `context.md` state it. Not reported.
13. **The "Handle errors" paragraph of section 8 and the new `Update` text.** The brief asks for a check. The two texts give two different rules. See CR-8.

---

## Findings

### CR-1: A fractional measured width loses the line rule when the padding sum has float noise
**Severity:** MEDIUM
**Rule:** 1. The measure and draw rules
**File:** `src/BrutalUi.Core/Elements/Label.cs:303-315, 317-342`, `docs/technical-design.md:488-490`
**Problem:** The draw wraps at `SnapWrapWidth(content.Z)`. The content width is `rect.Z - (insets.X + insets.Z)`, and `rect.Z` is the measured width `w` plus the same sum. Thus the draw width can be one unit in the last place below `w`. The snap removes this noise only when `w` is within 1/64 pixel of a whole number:

```csharp
float whole = MathF.Round(width);
return MathF.Abs(width - whole) <= WrapSnapTolerance ? whole : width;
```

`IUiRenderer.MeasureText` does not ask for whole-pixel widths. A renderer that gives a fractional width (probe 5 uses `RecordingRenderer` at size 13.5) gets a draw width below the widest line. The widest line then wraps in the draw. At width 135, the box holds 2 lines, and the draw gives 3 lines. The doc comment of `SnapWrapWidth` states "The snap gives the same value on both sides", and section 8 states that the measure and the draw give the same lines. Both statements are true for a whole-pixel measurer only. `ImGuiRenderer` rounds each width up (`ImGuiMapping.RoundUpWidth`), so the shipped pair is safe. A later renderer with fractional advances is not.
**Fix:** (1) In `PaintContent`, raise a wrap width above 0 by the float noise of the subtraction: `wrap += 2 * (MathF.BitIncrement(borderBox.Z) - borderBox.Z)`. The draw width is then never below `w`. A break can only differ when the widest line fills the limit to within this noise. (2) Change the doc comment of `SnapWrapWidth`, the class remarks, and section 8 to state the rule and its limit. (3) Add a Core theory: size 13.5, a padding of 7.3 %, `FlexStart` and `Stretch`, each root width from 20 to 400. Assert that the draw gives the line count of the box. (4) `KeepsTheSameWrapWidthWithAPercentPadding` asserts a bit-for-bit equal wrap width. Change it to a tolerance of the same noise. Another fix is a contract change: `MeasureText` gives whole-pixel widths, and `RecordingRenderer` rounds up. That fix needs a decision in `context.md`.
**Effort:** Medium
**Resolution:** `Label.DrawWrapWidth` raises a fractional draw wrap width by two units in the last place of the box width, and a whole-pixel width stays exact. The docs state the rule and its two limits, the new theory `KeepsTheLinesWithAFractionalMeasuredWidth` covers widths 20 to 400, and the old test uses a tolerance.

### CR-2: No shipped test fails when the measure scope loses its `finally`
**Severity:** MEDIUM
**Rule:** 4. The measure scope (and 7. Test quality)
**File:** `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs:110-133`, `src/BrutalUi.Core/Tree/UiContext.cs:217-230`
**Problem:** The summary of `TheScopeIsEmptyAfterUpdate` states that the scope is null "also after an exception". The exception case uses a context with no measurer:

```csharp
UiContext noMeasurer = new() { ViewportSize = new float2(400, 300) };
AddLabel(noMeasurer, "Hello");
Assert.Throws<InvalidOperationException>(() => noMeasurer.Update(0f));
Assert.Null(UiContext.CurrentMeasurer);
```

`Update` sets `t_measurer` to the null measurer of that context before the throw. Thus the scope is null with no `finally` too. Probe 3 removes the `try` and the `finally` and keeps the restore after the loop: each shipped test passes. The code is right today, but no test guards the rule of the brief.
**Fix:** Add two tests to `LabelTests`. (1) A context with a `RecordingRenderer` measurer and a leaf whose static callback throws. After `Assert.Throws`, assert that `UiContext.CurrentMeasurer` is null. (2) An outer context with a probe leaf whose callback runs `Update` of an inner context in a `try` and `catch`. The inner tree holds a leaf that throws. Assert that the probe reads the outer measurer after the catch. Both tests fail with no `finally`.
**Effort:** Low
**Resolution:** The tests `TheScopeIsEmptyAfterAnExceptionWithAMeasurer` and `ANestedUpdateThatThrowsKeepsTheOuterScope` in `LabelTests` pass, and both fail with no `finally` (checked one time, then restored).

### CR-3: The allocation gates do not reach the image measure, five setters, and the slow path of the refresh
**Severity:** MEDIUM
**Rule:** 2. Zero allocation
**File:** `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs:133-156, 263-311`, `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs` (`LabelFrame`)
**Problem:** The measured body changes the text of 10 labels and the tint of 10 images:

```csharp
labels[i].SetText(text);
images[i].Tint = tint;
```

A tint change marks `Paint` only, so no image measure runs in a measured frame. The setters `Image.Uv`, `Image.Texture`, `Image.ScaleMode`, `Label.Font`, `Label.Size`, `Label.Color`, and `SetText(LString)` never run in a measured frame. `RefreshLocalizedTextAllocatesNothing` changes the locale before the warm-up only, so each measured refresh takes the reference fast path. Probe 10 plants an allocation in `Image.MeasureImage`, in the `Uv` setter, in the `Color` setter, and in the slow path of `RefreshLocalized`. All 1011 tests pass for each plant. An atlas animation that sets `Uv` in each frame runs the image measure in each frame, so this path is hot.
**Fix:** (1) In `Frame`, alternate `images[i].Uv` between two UV rects with different spans, so the image measure runs. Also alternate `Texture` between two ids with the same size, and alternate `ScaleMode`. (2) On the changed labels, alternate `Color` and `Size` (two sizes that both wrap). In the Core gate, also alternate `Font` between id 0 and id 1, because `RecordingRenderer` measures each id the same. The ImGui gate has one font, so it alternates the color and the size only. (3) Alternate `SetText(LString)` between two `LString` instances on the localized label. (4) In the refresh test, create two strings of the same length before the loop. Before each measured refresh, call `Localize` with the other string outside the measured region. Each measured refresh then takes the slow path. (5) Assert that each new path does real work, as `TheMeasuredBodyDoesRealWork` does. Then plant an allocation in each path and confirm one failure.
**Effort:** Low
**Resolution:** The two gates now change the UV rect, the texture, the scale mode, the color, the size, the font (Core only), and the `LString` in each frame, and each refresh takes the slow path. A plant in each of the six paths makes one gate test or more fail.

### CR-4: A minimum width above the `AtMost` limit gives a box with more lines than the draw
**Severity:** LOW
**Rule:** 1. The measure and draw rules
**File:** `src/BrutalUi.Core/Elements/Label.cs:359-364, 377-402`
**Problem:** The engine gives the callback the `AtMost` limit of the parent, not the minimum width. The label wraps at the limit, and the engine then clamps the width up to `MinWidth`. The draw wraps at the wider box. Probe 6: the rect is `0,0,150,32` (two lines), and the draw gives one line (`144x16`). The text shows one line in a box of two lines. Yoga gives the same layout. The doc comments state that the measure and the draw give the same lines, with no exception.
**Fix:** State the limit in the class remarks of `Label` and in the "Limits" paragraph of section 8: "A minimum width above the available width gives the box the lines of the available width. The draw wraps at the box width and can use fewer lines." Add a test that pins the current result, so a later fix sees the change.
**Effort:** Low
**Resolution:** The `Label` remarks and the "Limits" paragraph of section 8 state the limit. The test `PinsTheMinimumWidthAboveTheAvailableWidth` in `LabelWrapTests` pins the rect `0,0,150,32` and the one-line draw.

### CR-5: The label callback gives the engine a result that is not finite
**Severity:** LOW
**Rule:** 1. The measure and draw rules
**File:** `src/BrutalUi.Core/Elements/Label.cs:384-401`
**Problem:** The callback returns the result of the measurer with no check:

```csharp
float2 free = measurer.MeasureText(label.Font, label.Size, label.Text, 0f);
if (widthMode == MeasureMode.Undefined)
{
    return free;
}
```

The contract of `MeasureCallback` states "An infinite component gives an infinite size. Return a finite size." Probe 7 gives the label a size of infinity with `RecordingRenderer`, and the label gets the rect `0,0,Infinity,0`. The known item names the missing check of `Size`. The new angle: the callback breaks the contract for any measurer result that is not finite, also with a valid `Size`. `ImGuiRenderer` maps a bad size to the default size, so the ImGui pair is safe.
**Fix:** In `MeasureLabel`, pass each result through a private static helper that gives 0 for a component that is not finite or is below 0. Use it for `free` and for the wrapped result. Add a test with a stub measurer that returns `(float.PositiveInfinity, float.NaN)`. Assert a finite rect.
**Effort:** Low
**Resolution:** `MeasureLabel` passes both measurer results through `FiniteSize`, and the theory `GivesAFiniteSizeForABadMeasurerResult` checks six bad results.

### CR-6: A UV component that is not finite reaches the renderer
**Severity:** LOW
**Rule:** 1. The measure and draw rules (and 5. API clarity)
**File:** `src/BrutalUi.Core/Elements/Image.cs:96-120, 177-207, 217-227`
**Problem:** `NaturalSize` gives 0 for a UV span that is not finite, so an image with an auto size takes no space. With a style size, `PaintContent` sends the UV as it is in each scale mode. `FitRect` and `CropUv` fall back to the stretch rule and keep the bad UV. Probe 8 sends `uv NaN,0,1,1` and `uv 0,0,Infinity,1`, and ImGui writes a quad of 100 by 50 with these texture coordinates. The GPU then samples an undefined texel. The `Uv` doc comment names no rule for a value that is not finite.
**Fix:** In `PaintContent`, return before the draw when a component of `_uv` is not finite (`float.IsFinite`). State the rule in the doc comment of `Uv` and of `CreateInfo.Uv`: "A component that is not finite draws nothing." Add a theory row for each scale mode. A throw in the setter is the other option. It changes the public API, so write it in `context.md` first.
**Effort:** Low
**Resolution:** `PaintContent` returns before the scale mode rules when a UV component is not finite, and the theory `DrawsNothingForAUvThatIsNotFinite` in `ImageTests` has one row for each scale mode.

### CR-7: No test pins the `AtMost` return for a text that fits the limit exactly
**Severity:** LOW
**Rule:** 7. Test quality (and 1. The measure and draw rules)
**File:** `src/BrutalUi.Core/Elements/Label.cs:395-399`, `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs:127-144`
**Problem:** The rule returns the free size when the text fits:

```csharp
if (limit <= 0f || free.X <= limit)
```

Probe 3 changes `<=` to `<`. Each shipped test passes. With the mutation, a text with a width equal to the limit sends a second `MeasureText` call at the limit. The result then depends on the line break rule of the renderer at an exact fit. `KeepsTheAtMostContract` uses a limit of 200 for a text of 144, so it does not test the edge.
**Fix:** Add a test with a root width of 144 and the sentence of 144 pixels, with `Align.FlexStart`. Assert the rect `0,0,144,16`, `LastMeasureWrapWidth` of 0 (no wrapped call), and one measure call for each callback.
**Effort:** Low
**Resolution:** The test `KeepsTheAtMostContractAtAnExactFit` in `LabelWrapTests` pins the exact fit at the limit 144.

### CR-8: Section 8 gives two rules for the call order of `Update`
**Severity:** LOW
**Rule:** 7. The docs
**File:** `docs/technical-design.md:431, 474`
**Problem:** The "Handle errors" paragraph of the imgui-draw-list feature states a rule:

> The game calls `Update` and `Draw` between `ImGui.NewFrame` and `ImGui.Render`.

The new paragraph "The measurer slot" states another rule:

> Thus `Update` can run before `ImGui.NewFrame`, and `Draw` runs between `ImGui.NewFrame` and `ImGui.Render`.

A reader of the first paragraph puts `Update` inside the frame. The ImGui label tests and the ImGui allocation gate run `Update` before `BeginFrame`. The two texts allow the same order, but the first text reads as a requirement.
**Fix:** Change the last sentence of "Handle errors" to: "The game calls `Draw` between `ImGui.NewFrame` and `ImGui.Render`. `Update` can run before `ImGui.NewFrame` (see "Label and image")." Keep one rule in the hand-off of `context.md` too.
**Effort:** Low
**Resolution:** "Handle errors" now states that `Draw` runs between `ImGui.NewFrame` and `ImGui.Render` and that `Update` can run before `ImGui.NewFrame`. The `context.md` hand-off states the same rule.

### CR-9: Two doc texts do not match the code
**Severity:** LOW
**Rule:** 7. The docs
**File:** `docs/technical-design.md:624`, `src/BrutalUi.Core/Elements/Label.cs:19-23`
**Problem:** (1) Section 14 states that the gate "changes each text and each tint" in each frame. The code changes 10 of 50 labels and 10 of 50 images (`ChangedCount = 10`). (2) The class remarks of `Label` state: "A context with no measurer throws `InvalidOperationException` from `UiContext.Update`." The throw occurs only when a label needs a measure. A root label in a finite viewport gets `Exactly` on both axes, the engine skips its callback, and `Update` does not throw. The remarks of `UiContext` and section 5 state the condition correctly.
**Fix:** (1) In section 14, write "Each frame changes the text of 10 labels and the tint of 10 images". (2) In the `Label` remarks, add "when the label needs a measure", as the `UiContext` remarks state.
**Effort:** Low
**Resolution:** Section 14 states "the text of 10 labels and the tint of 10 images", and the `Label` remarks say "when the label needs a measure".

---

## What's Already Good

- The measure scope saves the old value and restores it in `finally`. Probe 4 proves the restore after an exception and in a nested update that throws. The scope adds no allocation and no public member.
- The label measure rule keeps the `AtMost` contract by construction. The two-step measure is simple, and the doc comments explain why the lines stay the same.
- `Utf8Equals` compares a string with the owned bytes in chunks on the stack, with no allocation. It matches the replacement rule of `Encoding.UTF8`, and a theory proves it.
- `ContentInsets` is written in the `performLayout` block, so a cache hit writes it too. `ZeroOutLayoutRecursively` zeros it, and `LayoutDifferentialTests` compares it. No expected rect of a fixture changed.
- A shipped test kills each mutation of a dirty bit, of the fit and the crop arithmetic, of the snap, and of the image measure (probe 2).
- The image rules never give a `NaN` rect or an exception for an edge value (probe 8).
- The ImGui sweeps check the real line breaks from the vertex buffer at 161 widths, with no padding and with a percent padding.
- The gates catch a planted allocation in the label measure, in `Image.PaintContent`, in `SetText(string)`, and in `RefreshLocalizedText`.
- Section 5 of `docs/technical-design.md` lists the members of the code, and the `Texture` dirty rule follows the code.

---

## Recommended Fix Order

| Order | Finding | File | Severity | Effort |
|-------|---------|------|----------|--------|
| 1 | CR-1: The fractional measured width and the line rule | `src/BrutalUi.Core/Elements/Label.cs` | MEDIUM | Medium |
| 2 | CR-2: The scope test with no `finally` | `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs` | MEDIUM | Low |
| 3 | CR-3: The gate paths | `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs` | MEDIUM | Low |
| 4 | CR-5: The result that is not finite | `src/BrutalUi.Core/Elements/Label.cs` | LOW | Low |
| 5 | CR-7: The exact fit at the limit | `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs` | LOW | Low |
| 6 | CR-6: The UV that is not finite | `src/BrutalUi.Core/Elements/Image.cs` | LOW | Low |
| 7 | CR-4: The minimum width above the limit | `src/BrutalUi.Core/Elements/Label.cs` | LOW | Low |
| 8 | CR-8: The call order of `Update` | `docs/technical-design.md` | LOW | Low |
| 9 | CR-9: The gate text and the `Label` remarks | `docs/technical-design.md` | LOW | Low |

CR-1 comes first, because it changes `PaintContent` and one existing test. CR-5 and CR-7 also change `Label.MeasureLabel` or its tests, so run them after CR-1. CR-4 and CR-9 change the `Label` remarks, so one fix agent can do both with CR-1. CR-3 adds paths to the gate after the code changes, so its plants test the final code.

---

## Progress

- [x] CR-1: A fractional measured width loses the line rule when the padding sum has float noise
- [x] CR-2: No shipped test fails when the measure scope loses its `finally`
- [x] CR-3: The allocation gates do not reach the image measure, five setters, and the slow path of the refresh
- [x] CR-4: A minimum width above the `AtMost` limit gives a box with more lines than the draw
- [x] CR-5: The label callback gives the engine a result that is not finite
- [x] CR-6: A UV component that is not finite reaches the renderer
- [x] CR-7: No test pins the `AtMost` return for a text that fits the limit exactly
- [x] CR-8: Section 8 gives two rules for the call order of `Update`
- [x] CR-9: Two doc texts do not match the code
