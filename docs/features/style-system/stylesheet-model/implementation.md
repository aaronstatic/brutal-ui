# Stylesheet Model - Implementation Plan

**Epic:** style-system (feature 1 of 4)
**Status:** Ready for Review
**Started:** 2026-10-21
**Target Completion:** TBD
**Last Updated:** 2026-10-21 17:34

---

## Quality Bar

**Target quality:** A core library feature. The cascade of this feature writes each `ComputedStyle` value that the layout pass and the draw walk read. Each later feature of the epic fills or reads its model: selector-matching extends the selector, style-parser writes the rules, and transitions interpolate the values. A wrong merge order, a missed member, or a wrong dirty bit gives a wrong screen, and no pixel test catches it.

- **A correct cascade:** For each element, the result is: the defaults, then each matched rule in the order of specificity and then rule order, then the inline set. An inline member always wins. A text member that no rule and no inline value sets takes the value of the parent, and a root takes the context default. A test proves each rule.
- **No missed member:** `StyleProperties`, `InlineStyle`, the apply step, and the compare step cover each member of `ComputedStyle`. A test sets each member to a value that is not the default and checks each field. A second test counts the members of `ComputedStyle` and of `StyleProperty`.
- **Deterministic dirty bits:** A changed layout member sets `Layout`. A changed paint member or text member sets `Paint`. A changed `Font` or `FontSize` on a `Label` also sets `Layout`. An unchanged value sets no bit. The pass visits only elements with `Style` or `DescendantStyle`, and it clears both bits.
- **Zero allocation:** `UiContext.Update` with an active sheet allocates 0 bytes after warm-up on the 1000-element tree, also with a class change and a full restyle in the measured body. A planted allocation proves that the gate blocks.
- **The layout stays exact:** The migration to the inline API changes no expected rect of any layout fixture and of `LayoutDifferentialTests`.
- **API clarity:** One write path for a game: `el.Inline.<Member> = value`. `Element.Style` is `ref readonly`. Each public member has an XML doc comment (the build makes a missing comment an error).
- **No new pin and no ImGui in Core:** `Directory.Packages.props` and each `.csproj` do not change. The match path uses no reflection.

---

## Executive Summary

This feature adds the data model of the style system and the style pass. `StyleProperties` is a value struct with one field for each member of `ComputedStyle` and a 64-bit set mask. `StyleRule` holds a `Selector`, a `StyleProperties` set, and a specificity. `StyleSheet` is an immutable list of rules that its builder sorts one time. `UiContext` holds zero or more sheets and merges their rules into one cascade list at `AddSheet`.

The style pass runs at the seam in `UiContext.Update`, before the layout pass. For each element with the `Style` bit, it builds a new `ComputedStyle` from the defaults, applies each matched rule (it skips each member of the inline mask), applies the inline set, and resolves the inherited text members. A compare of each field of the old and the new style gives the dirty bits. A selector of this feature matches a class, an element type, or a class on a type. Each element class gives its type through a virtual `TypeId` property, from an intern table.

Each element holds an inline `StyleProperties` set. The game writes it through `el.Inline.Width = x`. The setter sets the mask bit and marks `Style`. `Element.Style` becomes `ref readonly`. Thus every write of `Element.Style` in the sample and in the tests moves to `el.Inline` (about 3770 writes in 46 files). Four base phases carry this migration. `Color`, `Font`, and `FontSize` move into `ComputedStyle`. `Label` reads them, and `Label.Color`, `Label.Font`, and `Label.Size` go.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (builds a sheet in code, adds it to the context, sets classes on elements, and sets an inline value for a one-off change)
- [x] **The sibling feature agents** (selector-matching extends `Selector` and the specificity. style-parser fills `StyleSheet.Builder` from text. transitions read the old and the new paint values of the pass)
- [x] **The later epics** (input-routing sets `Element.States`. The framework layer sets classes from state. The markup epic loads a sheet next to an XML tree)
- [ ] **The markup author** (not served. The text format comes in style-parser)

**User Subsets Served:**
- [x] **Simple** - `StyleSheet sheet = new StyleSheet.Builder().Add(Selector.ForClass(ClassName.Intern("panel")), new StyleProperties { BackgroundColor = dark }).Build(); context.AddSheet(sheet);` and `panel.Class("panel")`.
- [x] **Complex** - Two sheets (a base and a theme), type rules and class-on-type rules, text values that inherit down a deep tree, and inline overrides that the game sets and clears at run time.

**UX Approach:**
- One write path: `el.Inline.X = value`. The game calls no `MarkDirty` for a style value.
- A wrong call fails fast: a null sheet, the same sheet two times, and a selector with no class and no type throw at once, with a message that names the fix.
- A `Label` with no rule and no inline value draws in the default color of the context (opaque white), so a new label is visible.

---

## Goals

### Primary Goals
1. Give the style system its model: `StyleProperties`, `Selector`, `StyleRule`, and `StyleSheet`.
2. Write `ComputedStyle` from the sheets and the inline set in the style pass, with deterministic dirty bits.
3. Make `el.Inline` the only write path, and move each existing write to it.
4. Move the text values into `ComputedStyle` with inheritance, and make `Label` read them.
5. Keep `Update` free of managed allocation with an active sheet.

### Success Criteria
- [ ] The phase 1 demo prints `panel BackgroundColor #1E222CFF` and `plain BackgroundColor #00000000`.
- [ ] An inline value wins over a rule, and a clear of the inline value gives back the rule value (phase 2 demo).
- [ ] A text color of a rule on a panel reaches a grandchild. A class removal on the panel gives the grandchild the context default on the next `Update` (phase 7 demo).
- [ ] A label under the panel draws in the inherited color and size, and a new font size re-measures it (phase 8 demo).
- [ ] `ThousandElementAllocationTests` measures 0 bytes over 100 steps with a sheet of 12 rules and a style step in the rotation.
- [ ] `grep` finds no write through `Element.Style` in `src`, `samples`, or `tests`.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Style/                                    # new folder, namespace BrutalUi.Core
│   ├── StyleProperties.cs                    # phase 1: StyleProperty enum, StyleMasks, StyleProperties. Phase 7: text members
│   ├── Selector.cs                           # phase 1: class selector. Phase 9: type and class-on-type
│   ├── StyleRule.cs                          # phase 1
│   ├── StyleSheet.cs                         # phase 1: StyleSheet and StyleSheet.Builder
│   ├── StylePass.cs                          # phase 1: the pass and the compare. Phases 2, 6, 7, 8, 9 extend it
│   └── InlineStyle.cs                        # phase 2: the view of el.Inline. Phase 7: text members
├── Tree/ElementTypeId.cs                     # phase 9
├── Tree/ElementType.cs                       # phase 9: the intern table of type names
├── Tree/Element.cs                           # phase 1: StyleStorage, MarkSubtreeStyle. Phase 2: Inline. Phase 6: ref readonly Style. Phase 9: TypeId
├── Tree/ComputedStyle.cs                     # phase 7: Color, Font, FontSize, and the new doc contract
├── Tree/UiContext.cs                         # phase 1: AddSheet, UpdateStyles. Phase 7: CreateInfo defaults. Phase 10: RemoveSheet, AddRoot mark
├── Elements/Label.cs                         # phase 8: reads Style. Phase 9: TypeId
└── Elements/Image.cs                         # phase 9: TypeId
tests/BrutalUi.Core.Tests/Style/
├── StylePropertiesTests.cs                   # phase 1
├── StyleSheetTests.cs                        # phase 1
├── StylePassTests.cs                         # phase 1
├── InlineStyleTests.cs                       # phase 2
├── TextInheritanceTests.cs                   # phase 7
├── ElementTypeTests.cs                       # phase 9
├── TypeSelectorTests.cs                      # phase 9
└── MultiSheetTests.cs                        # phase 10
tests/**  (45 files)                          # phases 3 to 6: the migration to el.Inline. Phase 8: the label files again
samples/HelloPanel/HelloScreen.cs             # phase 2: el.Inline. Phase 8: the label text values
docs/technical-design.md, CHANGELOG.md, docs/features/style-system/human-testing.md   # phase 11
```

`src/BrutalUi.ImGui/`, `src/BrutalUi.Markup/`, `Directory.Packages.props`, and each `.csproj` do not change.

### The Public API

All types live in `namespace BrutalUi.Core;`. Each new file holds its own `using` lines outside the namespace (`.editorconfig`, no implicit usings).

```csharp
public enum StyleProperty : byte            // one bit of the set mask for each member (A1). Order is the bit index.
{
    // Layout (25). A9: Overflow is a layout member.
    FlexDirection, Width, Height, Margin, Padding, BorderWidth, FlexGrow, FlexShrink, FlexBasis,
    MinWidth, MinHeight, MaxWidth, MaxHeight, JustifyContent, AlignItems, AlignSelf, AlignContent,
    FlexWrap, Display, Position, Left, Top, Right, Bottom, Overflow,
    // Paint (4)
    BackgroundColor, BorderColor, CornerRadius, Opacity,
    // Text (3), phase 7
    Color, Font, FontSize,
}

public struct StyleProperties                // a value struct. No heap object (A1).
{
    public Length Width { readonly get; set; }      // one get and set pair for each StyleProperty member.
    // ...                                          // The setter writes the value and sets the bit.
    public float Opacity { readonly get; set; }     // The getter of an unset member gives the ComputedStyle default (Opacity: 1).
    public readonly bool IsSet(StyleProperty property);
    public readonly bool IsEmpty { get; }
    public void Clear(StyleProperty property);      // clears the bit and puts back the default value
    internal readonly ulong SetMask { get; }
    internal readonly void ApplyTo(ref ComputedStyle target, ulong skip);   // writes each set member that skip does not hold
}

public readonly struct Selector
{
    public static Selector ForClass(ClassId id);                               // .panel (ArgumentException for None)
    public static Selector ForType(ElementTypeId type);                        // Label (phase 9)
    public static Selector ForTypeAndClass(ElementTypeId type, ClassId id);    // Label.title (phase 9)
    public ClassId Class { get; }
    public ElementTypeId Type { get; }                                         // phase 9
    public int Specificity { get; }                                            // A5: 10 for each class, 1 for each type
    internal bool Matches(Element element, ElementTypeId elementType);
}

public readonly struct StyleRule
{
    public StyleRule(in Selector selector, in StyleProperties properties);     // Specificity = selector.Specificity
    public readonly Selector Selector;
    public readonly StyleProperties Properties;
    public readonly int Specificity;
}

public sealed class StyleSheet                                                 // immutable after Build (A6)
{
    public ReadOnlySpan<StyleRule> Rules { get; }                              // sorted: specificity, then the order of Add
    public sealed class Builder
    {
        public Builder Add(in Selector selector, in StyleProperties properties);   // ArgumentException for default(Selector)
        public StyleSheet Build();                                                 // sorts one time. Each call gives a new sheet.
    }
}

public readonly struct InlineStyle                                             // phase 2: a view, like ClassList
{
    public Length Width { get; set; }          // one pair for each StyleProperty member. A set to a new value or to an
    // ...                                     // unset member sets the bit and marks Style. A set to the same value marks nothing.
    public bool IsSet(StyleProperty property);
    public void Clear(StyleProperty property); // marks Style only when the bit was set
}

public class Element
{
    public ref readonly ComputedStyle Style { get; }       // phase 6 (Q1). Phases 1 to 5: ref ComputedStyle.
    public InlineStyle Inline { get; }                     // phase 2 (Q1)
    public virtual ElementTypeId TypeId { get; }           // phase 9 (Q2): "Element"
    internal ref ComputedStyle StyleStorage { get; }       // phase 1: the pass writes through it
    internal ref StyleProperties InlineStorage { get; }    // phase 2
    internal void MarkSubtreeStyle();                      // phase 1: Style on each element, DescendantStyle on each parent
}

public readonly struct ElementTypeId : IEquatable<ElementTypeId>   // phase 9, the form of ClassId
public static class ElementType                                    // phase 9, the form of ClassName
{
    public static ElementTypeId Intern(string name);
    public static string GetName(ElementTypeId id);
}

public sealed class UiContext
{
    public struct CreateInfo
    {
        public required IUiRenderer Measurer;
        public Rgba8? DefaultColor;            // phase 7 (A2). Null gives opaque white.
        public FontHandle DefaultFont;         // phase 7 (A2). Default: id 0.
        public float DefaultFontSize;          // phase 7 (A2). Default: 0, the default size of the font.
    }
    public void AddSheet(StyleSheet sheet);    // phase 1. ArgumentNullException. InvalidOperationException for a sheet that the context holds.
    public bool RemoveSheet(StyleSheet sheet); // phase 10
    internal void UpdateStyles();              // the style pass alone. Update calls it. Tests call it to read the bits.
}

public struct ComputedStyle                    // phase 7 adds:
{
    public Rgba8 Color;                        // the text color. Inherits.
    public FontHandle Font;                    // the font id. Inherits.
    public float FontSize;                     // the font size in pixels. 0 is the default size. Inherits.
}

public sealed class Label : Element            // phase 8: Font, Size, and Color go. CreateInfo holds Text and LocalizedText.
```

**Gotcha (the nested write):** `el.Inline.Margin.Top = 12` does not compile (CS1612), because the view returns a copy of the edges. Write the whole value: `el.Inline.Margin = el.Inline.Margin with { Top = 12 };`, `new LengthEdges(l, t, r, b)`, or `LengthEdges.All(v)`. The same rule applies to `CornerRadius`.

**Gotcha (`Enum.HasFlag`):** Do not use `HasFlag` or LINQ in the pass. Use `ulong` bit math: `1UL << (int)property`.

### The Style Pass (Chosen Approach: mask struct, full rebuild)

```
UiContext.Update(dt)
  └─ UpdateStyles()                                         // the seam, before the layout pass
       for each root: Visit(root, in contextDefaults)
  └─ the layout pass (unchanged)
  └─ the clear pass (unchanged)

Visit(element, in parentText):                              // own recursion (A3 needs it, see Decision 3)
  flags = element.Dirty
  if (flags & Style) != 0:  Restyle(element, in parentText)
  flags = element.Dirty                                     // read again: Restyle can mark the children
  if (flags & DescendantStyle) != 0:
      for each child with Style or DescendantStyle:  Visit(child, in element.Style)
  element.ClearDirty(Style | DescendantStyle)               // after the subtree, as DirtyWalk does

Restyle(element, in parentText):                            // [MethodImpl(NoInlining)]: the 300-byte local stays out of the recursion frame
  next     = default(ComputedStyle)
  inline   = element.InlineStorage.SetMask
  applied  = inline
  type     = element.TypeId                                 // phase 9: one virtual call for each element
  for each rule in _cascade:                                // A7: a scan of each rule of each sheet
      if rule.Selector.Matches(element, type):
          rule.Properties.ApplyTo(ref next, skip: inline)
          applied |= rule.Properties.SetMask
  element.InlineStorage.ApplyTo(ref next, skip: 0)
  for Color, Font, FontSize not in applied: next.X = parentText.X      // phase 7, the inheritance
  bits = Compare(in element.Style, in next, out bool textChanged, out bool sizeTextChanged)
  if sizeTextChanged and element is Label: bits |= Layout              // phase 8
  element.StyleStorage = next
  if bits != None: element.MarkDirty(bits)
  if textChanged: each child.MarkDirty(Style)                          // A3
```

- **The compare** gives `Layout` for a changed layout member and `Paint` for a changed paint member or text member. `textChanged` covers the three text members. `sizeTextChanged` covers `Font` and `FontSize` (open scope question 1). Floats compare with `float.Equals`, so `NaN` to `NaN` is no change. `Length`, `Rgba8`, and `FontHandle` use their equality. `LengthEdges` and `Corners` have no equality, so the pass compares each part in a private helper. `Opacity` compares the getter values, so an unset value and a set 1 are the same.
- **The marks are safe.** The pass marks `Layout` and `Paint`, which it does not clear. A child mark of `Style` sends `DescendantStyle` up to the element, and the second read of `element.Dirty` sees it. The ancestors hold `DescendantStyle` already, because the pass entered them through it.
- **The cascade list.** `UiContext` holds `List<StyleSheet> _sheets` and `StyleRule[] _cascade`. `AddSheet` and `RemoveSheet` build `_cascade` again: the rules of each sheet in the order of add, sorted with the unique key `(Specificity, sheet index, rule index)`. Thus a later sheet wins at equal specificity (A4), and the order is deterministic. The rebuild allocates. It runs at load time, not in the frame loop.
- **A sheet change** calls `MarkSubtreeStyle()` on each root (A8). Each element gets `Style`, and each element with children gets `DescendantStyle`. The next `Update` restyles the full tree.

### The Transitional Gates (phases 1 to 6)

The pass rebuilds each restyled element from the defaults. An element that a test still writes through `Element.Style` loses that write. Two temporary gates keep the solution green until the migration ends.

| Phases | Gate | Where |
|--------|------|-------|
| 1 | Gate A: `UpdateStyles` returns at once when `_cascade` is empty. | `UiContext.UpdateStyles` |
| 2 to 6 | Gate B: `Restyle` runs only when `_cascade` is not empty or the inline mask of the element is not 0. Otherwise the pass skips the element and keeps its style. | `StylePass.Visit` |
| 6, end | No gate. The pass restyles each element with `Style`. | - |

A test that adds a sheet uses `el.Inline` only. Each migrated file moves completely, and a shared tree moves in the same group as each file that writes its elements. Phase 6 removes gate B, and the build proves that no direct write remains, because `Style` is `ref readonly`.

### The Migration Recipe (phases 3 to 6)

Apply these steps to each file, one file for each task.

1. Run one `Edit` with `replace_all` from `.Style.` to `.Inline.` on the file. Tool rule 1 forbids a script, and `replace_all` is one edit.
2. Build the test project. Fix each CS1612 (a nested write): four edge writes in a row become one `new LengthEdges(l, t, r, b)` or `LengthEdges.All(v)`. One edge write becomes `x.Inline.Margin = x.Inline.Margin with { Top = v };`. The same applies to `CornerRadius`.
3. A local `ref ComputedStyle s = ref e.Style;` that writes becomes `InlineStyle s = e.Inline;`. A local that only reads becomes `ref readonly`. A `ref ComputedStyle` parameter that only reads becomes `in ComputedStyle`.
4. A read through `ref readonly` of `Style` before the first `Update` now gives the defaults. Add an `Update` first, or read through `Inline`.
5. Keep each `MarkDirty` call. The inline write marks `Style`, and the pass sets `Layout` only for a new value. A test that writes the same value and marks `Layout` still needs its mark.
6. Run the tests of the file. When an exact assert of `Dirty` fails only because of the new `Style` bit, add `Style` to the expected value. When a test asserts the old contract ("a style write marks nothing"), rewrite it for the new contract. Record each such change in `context.md`.
7. Never change an expected rect. A changed rect is a bug in the pass or in the recipe. Stop, and record it in `context.md`.

### Data Flow

```
Load:      new StyleSheet.Builder().Add(selector, props)...Build() ─► context.AddSheet(sheet) ─► _cascade, MarkSubtreeStyle
Frame:     el.Classes.Add(id) ─► MarkDirty(Style)          el.Inline.Width = 120 ─► mask bit, MarkDirty(Style)
           UiContext.Update(dt) ─► UpdateStyles ─► Restyle ─► StyleStorage, MarkDirty(Layout | Paint), child Style (A3)
                                ─► LayoutEngine (reads Style) ─► Label.MeasureLabel (reads Style.Font, Style.FontSize)
           UiContext.Draw(r)    ─► PaintWalk (reads the paint members) ─► Label.PaintContent (reads Style.Color)
Tests:     context.UpdateStyles() alone, then read element.Dirty before the layout pass clears it.
```

---

## Implementation Phases

Each phase is a slice or a base phase, per section 11 of `.beast-mode/phase-handoff.md`. Confirmed Assumption Q4 gives the demo of phase 1 (rule 11). Phases 3 to 6 are base phases: a pure refactor of 45 test files. No slice can run on the text members until the migration ends, because gate B skips an element with no inline value.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project <test project> -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The xUnit analyzers are errors.

### Phase 1: A Class Rule Paints a Panel
**Goal:** The smallest path that proves the idea: a sheet built in code with one class rule, the style pass at the seam, and a paint member in `ComputedStyle` with its dirty bit.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*PaintsAPanelFromAClassRule' --show-live-output on` and reads `panel BackgroundColor #1E222CFF` and `plain BackgroundColor #00000000`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase fixes the model and the pass for each later feature: the bit layout, the apply step, the compare, the own recursion, and the cascade order. A missed member or a wrong clear order gives a silent cascade bug.

**Tasks:**

Group A (the model, 6 files):
- [ ] 1.1 Record in `context.md`: the test total of the solution, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30`. Read `.editorconfig` (field names, types in one file), `ClassName.cs`, `ClassSet.cs`, `ClassList.cs`, and `DirtyWalk.cs`.
- [ ] 1.2 Write `src/BrutalUi.Core/Style/StyleProperties.cs`: the `StyleProperty` enum with the 29 layout and paint members of "The Public API", an internal static `StyleMasks` class with `Layout` and `Paint` (each a `ulong`), and the `StyleProperties` struct. The struct holds one private field for each member, a `ulong` mask, `IsSet`, `IsEmpty`, `Clear`, the internal `SetMask`, and the internal `ApplyTo`. `ApplyTo` holds one `if` for each member. The remarks state the bit rule and that an unset getter gives the `ComputedStyle` default.
- [ ] 1.3 Write `src/BrutalUi.Core/Style/Selector.cs`: `ForClass`, `Class`, `Specificity` (10), and the internal `Matches` (`element.ClassStorage.Contains(Class)`). `ForClass(ClassId.None)` throws `ArgumentException`. `default(Selector)` matches nothing. The remarks state the slot for selector-matching: the name, the pseudo-states, and the parent chain join this struct later, and `Specificity` is the one place of the weights.
- [ ] 1.4 Write `src/BrutalUi.Core/Style/StyleRule.cs` per "The Public API". The remarks state that style-parser adds the transition member later.
- [ ] 1.5 Write `src/BrutalUi.Core/Style/StyleSheet.cs`: the sheet and its `Builder`. `Build` copies the rules and sorts them one time with the unique key `(Specificity, index of Add)`. `Add` throws `ArgumentException` for `default(Selector)`. The sheet exposes the rules as a span and has no write member.
- [ ] 1.6 Write `tests/BrutalUi.Core.Tests/Style/StylePropertiesTests.cs`:
  - `ApplyToWritesEachMember`: set each of the 29 members to a value that is not the default, apply to `default(ComputedStyle)`, and assert each field.
  - `ApplyToSkipsTheMaskedMembers`, `AnUnsetGetterGivesTheDefault` (Opacity gives 1), `ClearRemovesTheBitAndTheValue`, and `IsEmptyForANewSet`.
  - `EachMemberHasOneBitInOneMask`: each enum value is below 64, and its bit is in `Layout` or `Paint` and not in both.
  - `TheEnumCoversEachComputedStyleMember`: count the public instance fields of `ComputedStyle` plus the `Opacity` property with reflection in the test, and compare with the count of `StyleProperty`. The test lists the field names on a failure.
- [ ] 1.7 Write `tests/BrutalUi.Core.Tests/Style/StyleSheetTests.cs`: the rules keep the order of `Add` at equal specificity, `Add` throws for `default(Selector)`, two `Build` calls give two sheets with equal rules, and an `Add` after `Build` does not change the built sheet.

Group B (the pass, 4 files):
- [ ] 1.8 Edit `src/BrutalUi.Core/Tree/Element.cs`: add `internal ref ComputedStyle StyleStorage` and `internal void MarkSubtreeStyle()` (recursion: `Style` on each element, `DescendantStyle` on each element with children, then `PropagateToAncestors` from the start element). Keep `public ref ComputedStyle Style` until phase 6.
- [ ] 1.9 Write `src/BrutalUi.Core/Style/StylePass.cs`: an internal static class with `Run(ReadOnlySpan<Element> roots, ReadOnlySpan<StyleRule> cascade)`, `Visit`, `Restyle`, and `Compare` per "The Style Pass", with no inline set and no text members yet. `Restyle` has `[MethodImpl(MethodImplOptions.NoInlining)]`.
- [ ] 1.10 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `_sheets`, `_cascade`, `AddSheet` (the null check, the duplicate check, the cascade rebuild of "The Style Pass", and `MarkSubtreeStyle` on each root), and `internal void UpdateStyles()` with gate A. Call `UpdateStyles()` at the seam. The doc comment of `AddSheet` states that the call allocates and belongs in load code.
- [ ] 1.11 Write `tests/BrutalUi.Core.Tests/Style/StylePassTests.cs`:
  - `PaintsAPanelFromAClassRule` (the demo): a sheet with `.panel { BackgroundColor = (30, 34, 44, 255) }`. A root with two children: `panel` (class `panel`) and `plain`. `Update`, then print and assert both lines.
  - The dirty bits, read after `UpdateStyles()`: a new paint value gives `Paint` and no `Layout`. A new `Width` gives `Layout`. A second restyle with the same result (`MarkDirty(Style)`, then `UpdateStyles()`) gives no `Layout` and no `Paint`. The pass clears `Style` and `DescendantStyle` on each element.
  - `ALaterRuleWinsAtEqualSpecificity`: rules `.a { BackgroundColor = red; Width = 10 }` and `.b { BackgroundColor = blue }` on one element give blue and a width of 10.
  - `SkipsAnElementWithNoStyleBit`: after `Update`, add a class through `ClassStorage` (no mark), run `UpdateStyles()`, and the style stays the same.
  - `AddSheetAfterTheFirstUpdateRestylesEachElement` (A8), `AddSheetThrowsForNull`, and `AddSheetThrowsForTheSameSheetTwice`.
  - `TheStylePassAllocatesNothingAfterWarmUp`: a tree of 50 elements and a sheet of 4 rules. Each step toggles one class and calls `Update`. Run 16 warm-up steps, call `AllocationGate.ReleaseAllocationContext()`, measure 100 steps, and assert 0 bytes.
- [ ] 1.12 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 72 (12 tasks, about six steps for one task)

**Files (10):** Group A: `StyleProperties.cs`, `Selector.cs`, `StyleRule.cs`, `StyleSheet.cs`, `StylePropertiesTests.cs`, `StyleSheetTests.cs`. Group B: `Element.cs`, `StylePass.cs`, `UiContext.cs`, `StylePassTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] Each existing test passes with no change. No existing test adds a sheet, so gate A keeps their direct writes.
- [ ] `TheStylePassAllocatesNothingAfterWarmUp` measures 0 bytes.

---

### Phase 2: An Inline Value Wins Over a Rule
**Goal:** The game sets a value on one element through `el.Inline`, the value wins over each rule, and a clear gives back the rule value.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AnInlineValueWinsOverARule' --show-live-output on` and reads `BackgroundColor #3060C0FF width 200`, then `BackgroundColor #1E222CFF width 200` after the clear.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase sets the public write path of the library (Q1) and gate B. A wrong gate wipes the direct writes of the unmigrated tests, and a setter that does not mark gives a stale style.

**Tasks:**

Group A (the inline set, 5 files):
- [ ] 2.1 Write `src/BrutalUi.Core/Style/InlineStyle.cs`: a `readonly struct` view with an `Element` owner, in the form of `ClassList`. One get and set pair for each `StyleProperty` member. A setter returns at once when the bit is set and the value is equal. Otherwise it writes `_owner.InlineStorage` and calls `_owner.MarkDirty(DirtyFlags.Style)`. Add `IsSet` and `Clear`. The remarks state the nested-write gotcha and that a default instance throws.
- [ ] 2.2 Edit `Element.cs`: add the `_inline` field, `public InlineStyle Inline`, and `internal ref StyleProperties InlineStorage`. The class remarks state that the inline set lives in the element (A1).
- [ ] 2.3 Edit `StylePass.cs`: apply the rules with `skip: inline`, then apply the inline set. Add gate B in `Visit`. Its comment names phase 6 as the end of the gate.
- [ ] 2.4 Edit `UiContext.cs`: remove gate A. `UpdateStyles` runs in each `Update`.
- [ ] 2.5 Write `tests/BrutalUi.Core.Tests/Style/InlineStyleTests.cs`:
  - `AnInlineValueWinsOverARule` (the demo): a sheet with `.panel { BackgroundColor = (30, 34, 44, 255); Width = 200 }`, and `panel.Inline.BackgroundColor = (48, 96, 192, 255)`. Print after `Update`. Then `panel.Inline.Clear(StyleProperty.BackgroundColor)`, `Update`, and print again. Assert both lines.
  - `ASetMarksStyleAndTheAncestors`, `ASetToTheSameValueMarksNothing`, `AClearOfAnUnsetMemberMarksNothing`, `AnInlineValueReachesTheStyleWithNoSheet`, `AnInlineValueWinsOverEachRuleOrder`, and `ADefaultViewThrows`.
  - `AnInlineLayoutChangeSetsLayoutAfterThePass`: after `UpdateStyles()`, the element holds `Layout`.

Group B (the sample, 1 file):
- [ ] 2.6 Edit `samples/HelloPanel/HelloScreen.cs`: move the 10 writes to `Inline`. The nested `Margin.Top` write becomes a whole write. The label keeps its create-info until phase 8.
- [ ] 2.7 Run the build, the tests, the demo, and `scripts/test-linux.sh; echo "exit=$?"` (expect the PASS line and `exit=0`). Run the facts line of task 1.1 again, and record the growth of `bytes per element` in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (6):** Group A: `InlineStyle.cs`, `Element.cs`, `StylePass.cs`, `UiContext.cs`, `InlineStyleTests.cs`. Group B: `HelloScreen.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] Each existing test passes with no change (gate B).
- [ ] The smoke run of HelloPanel passes.

---

### Phase 3 (Base): Migrate the Shared Trees, the Allocation Gates, and the Render Tests
**Goal:** Move the shared test trees, the allocation tests, and the render tests to `el.Inline` with the migration recipe.
**Demo:** none (base phase: a pure refactor of test code to the inline API)
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)

**Tier note:** The shared trees compile into three projects (`BrutalUi.Core.Tests`, `BrutalUi.ImGui.Tests`, and `BrutalUi.Benchmarks` link `Shared/*.cs`), and the allocation gates are load-bearing. A tree and each file that writes its elements move in one group.

**Tasks:**

Group A (the shared trees and the gates, 6 files):
- [ ] 3.1 Record in `context.md` the count of `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` (expected: about 3770).
- [ ] 3.2 Apply the recipe to `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs` (124 writes). Use the public API only, because the linked projects have no internal access.
- [ ] 3.3 Apply the recipe to `tests/BrutalUi.Core.Tests/Shared/ChainTree.cs`.
- [ ] 3.4 Apply the recipe to `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`. `TheTreeHoldsTheRequiredMix` reads the style before the first `Update`: add `tree.Context.Update(0.016f)` first (recipe step 4). The helpers take `in ComputedStyle`.
- [ ] 3.5 Apply the recipe to `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`.
- [ ] 3.6 Apply the recipe to `tests/BrutalUi.Benchmarks/TreeLayoutBenchmark.cs`.
- [ ] 3.7 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs`.
- [ ] 3.8 Run the build and the tests. Run `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30` and expect a normal exit.

Group B (the render tests, 6 files):
- [ ] 3.9 Apply the recipe to `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs` (89 writes, 8 nested).
- [ ] 3.10 Apply the recipe to `tests/BrutalUi.Core.Tests/Rendering/PaintContentTests.cs`.
- [ ] 3.11 Apply the recipe to `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`.
- [ ] 3.12 Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs`.
- [ ] 3.13 Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiImageElementTests.cs`.
- [ ] 3.14 Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`.
- [ ] 3.15 Run the build and the tests. Record the new grep count and each recipe step 6 change in `context.md`.

**Estimated Steps:** about 60 (15 tasks, about four steps for one task)

**Files (12):** Group A: `ThousandElementTree.cs`, `ChainTree.cs`, `ThousandElementAllocationTests.cs`, `ElementAllocationTests.cs`, `TreeLayoutBenchmark.cs`, `LayoutAllocationTests.cs`. Group B: `PaintWalkTests.cs`, `PaintContentTests.cs`, `RenderValueTests.cs`, `ImGuiRendererRectTests.cs`, `ImGuiImageElementTests.cs`, `ImGuiRendererAllocationTests.cs`.

**Acceptance Criteria:**
- [ ] The 12 files hold no write through `Element.Style`.
- [ ] Each test passes. No expected rect and no expected draw call changed.
- [ ] The allocation tests measure 0 bytes.

---

### Phase 4 (Base): Migrate the Element Tests and the Layout Behavior Tests
**Goal:** Move the element tests and the layout tests that check the dirty bits, the cache, and the differential layout to `el.Inline`.
**Demo:** none (base phase: a pure refactor of test code to the inline API)
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** Group B holds the tests of the old contract. `LayoutCacheTests` and `LayoutPassTests` can assert that a style write marks nothing. `LayoutDifferentialTests` writes whole styles and changes random fields through `ref ComputedStyle`. A careless rewrite loses coverage.

**Tasks:**

Group A (the element tests, 6 files):
- [ ] 4.1 Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/ContentBoxTests.cs`.
- [ ] 4.2 Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`.
- [ ] 4.3 Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/ImageTests.cs`.
- [ ] 4.4 Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`.
- [ ] 4.5 Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs`.
- [ ] 4.6 Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`.
- [ ] 4.7 Run the build and the tests.

Group B (the layout behavior tests, 6 files):
- [ ] 4.8 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/LayoutPassTests.cs`. Search first for a test that writes a style with no mark and expects the old layout. Rewrite each such test for the new contract (the write marks `Style`, and the next `Update` lays out the new value). Record each one in `context.md`.
- [ ] 4.9 Apply the recipe and the search of task 4.8 to `tests/BrutalUi.Core.Tests/Layout/LayoutCacheTests.cs`.
- [ ] 4.10 Migrate `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs`: `RandomStyle` gives a `StyleProperties` and sets the same members with the same random calls in the same order. A whole copy becomes `copy.InlineStorage = source.InlineStorage; copy.MarkDirty(DirtyFlags.Style);`. `ChangeOneField` and `RemovePercents` take an `InlineStyle`. The seeds and the compare stay the same. Record the count of compared trees before and after in `context.md`. The counts must be equal.
- [ ] 4.11 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/ContentInsetsTests.cs` (22 nested writes).
- [ ] 4.12 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MeasureCacheFixtureTests.cs` and `tests/BrutalUi.Core.Tests/Layout/MeasureModeFixtureTests.cs`.
- [ ] 4.13 Run the build and the tests. Record the new grep count in `context.md`.

**Estimated Steps:** about 70 (13 tasks, about five steps for one task)

**Files (12):** Group A: `ContentBoxTests.cs`, `LabelTests.cs`, `ImageTests.cs`, `LabelTextTests.cs`, `LabelWrapTests.cs`, `ImGuiLabelTests.cs`. Group B: `LayoutPassTests.cs`, `LayoutCacheTests.cs`, `LayoutDifferentialTests.cs`, `ContentInsetsTests.cs`, `MeasureCacheFixtureTests.cs`, `MeasureModeFixtureTests.cs`.

**Acceptance Criteria:**
- [ ] The 12 files hold no write through `Element.Style`.
- [ ] `LayoutDifferentialTests` compares the same count of trees as before, with 0 failed.
- [ ] Each rewritten test of the old contract has a line in `context.md`.

---

### Phase 5 (Base): Migrate the First Half of the Layout Fixtures
**Goal:** Move twelve Yoga fixture files to `el.Inline` with the migration recipe.
**Demo:** none (base phase: a pure refactor of test code to the inline API)
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** The files are mechanical, but they are the proof of the layout engine. `AlignContentFixtureTests.cs` alone holds 753 writes and 97 nested writes. An edit that changes a value instead of the syntax breaks a fixture in a way that looks like an engine bug.

**Tasks:**

Group A (6 files):
- [ ] 5.1 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AlignContentFixtureTests.cs`.
- [ ] 5.2 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AlignItemsFixtureTests.cs`.
- [ ] 5.3 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AlignSelfFixtureTests.cs`.
- [ ] 5.4 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AutoFixtureTests.cs`.
- [ ] 5.5 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/BorderFixtureTests.cs`.
- [ ] 5.6 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/BoxModelTests.cs`.
- [ ] 5.7 Run the build and the tests.

Group B (6 files):
- [ ] 5.8 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/DimensionFixtureTests.cs`.
- [ ] 5.9 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/DisplayFixtureTests.cs`.
- [ ] 5.10 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/FlexDirectionFixtureTests.cs`.
- [ ] 5.11 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/FlexFixtureTests.cs`.
- [ ] 5.12 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/FlexWrapFixtureTests.cs`.
- [ ] 5.13 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/JustifyContentFixtureTests.cs`.
- [ ] 5.14 Run the build and the tests. Record the new grep count in `context.md`.

**Estimated Steps:** about 56 (14 tasks, about four steps for one task)

**Files (12):** Group A: `AlignContentFixtureTests.cs`, `AlignItemsFixtureTests.cs`, `AlignSelfFixtureTests.cs`, `AutoFixtureTests.cs`, `BorderFixtureTests.cs`, `BoxModelTests.cs`. Group B: `DimensionFixtureTests.cs`, `DisplayFixtureTests.cs`, `FlexDirectionFixtureTests.cs`, `FlexFixtureTests.cs`, `FlexWrapFixtureTests.cs`, `JustifyContentFixtureTests.cs`.

**Acceptance Criteria:**
- [ ] The 12 files hold no write through `Element.Style`.
- [ ] `git diff` of the 12 files changes no number in an `AssertRect` line: `git diff -U0 -- tests/BrutalUi.Core.Tests/Layout | grep -c '^[-+].*AssertRect'` prints `0`.

---

### Phase 6 (Base): Migrate the Second Half of the Layout Fixtures and Lock the Style
**Goal:** Move the last nine fixture files, make `Element.Style` `ref readonly`, and remove gate B.
**Demo:** none (base phase: a pure refactor that ends the migration)
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Tier note:** Group B ends the transitional state. After the lock, the pass restyles each element with `Style`. A missed direct write now fails the build, and a wrong pass now fails every layout test at once.

**Tasks:**

Group A (6 files):
- [ ] 6.1 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MarginFixtureTests.cs`.
- [ ] 6.2 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MeasureFixtureTests.cs`.
- [ ] 6.3 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MinMaxDimensionFixtureTests.cs`.
- [ ] 6.4 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/PaddingFixtureTests.cs`.
- [ ] 6.5 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/PercentageFixtureTests.cs`.
- [ ] 6.6 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/RelativePositionFixtureTests.cs`.
- [ ] 6.7 Run the build and the tests.

Group B (the lock, 6 files):
- [ ] 6.8 Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/RoundingFixtureTests.cs`, `SizeOverflowFixtureTests.cs`, and `AbsolutePositionFixtureTests.cs`.
- [ ] 6.9 Edit `tests/BrutalUi.Core.Tests/ElementStateTests.cs`: `StyleReturnsARefToTheSameField` uses `Unsafe.AsRef(in first.Style)`. `WriteThroughStyleOrLayoutMarksNothing` becomes `WriteThroughLayoutMarksNothing`. Add `StyleIsReadOnly`: the return parameter of the getter of `Element.Style` holds an attribute with the name `IsReadOnlyAttribute` (reflection in the test only).
- [ ] 6.10 Edit `Element.cs`: `public ref readonly ComputedStyle Style => ref _style;`. The doc comment states that the style pass writes the style and that the game writes `Inline`.
- [ ] 6.11 Edit `StylePass.cs`: remove gate B. Each element with `Style` gets a restyle.
- [ ] 6.12 Run `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs`. Expect the 3 doc comment lines of `ComputedStyle.cs`, `Corners.cs`, and `LengthEdges.cs` only. Record the output in `context.md`.
- [ ] 6.13 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the test total in `context.md`.

**Estimated Steps:** about 55 (13 tasks, about four steps for one task)

**Files (12):** Group A: `MarginFixtureTests.cs`, `MeasureFixtureTests.cs`, `MinMaxDimensionFixtureTests.cs`, `PaddingFixtureTests.cs`, `PercentageFixtureTests.cs`, `RelativePositionFixtureTests.cs`. Group B: `RoundingFixtureTests.cs`, `SizeOverflowFixtureTests.cs`, `AbsolutePositionFixtureTests.cs`, `ElementStateTests.cs`, `Element.cs`, `StylePass.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] `Element.Style` is `ref readonly`, and the build passes.
- [ ] The grep of task 6.12 finds the 3 doc comment lines only.
- [ ] Each test passes with no gate. No expected rect changed in the feature: `git diff -U0 main -- tests | grep -c '^[-+].*AssertRect'` prints `0`.

---

### Phase 7: Text Color Flows Down the Tree
**Goal:** `Color`, `Font`, and `FontSize` live in `ComputedStyle`, a rule or an inline value sets them, and an unset value comes from the parent or from the context defaults.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TextColorFlowsDownTheTree' --show-live-output on` and reads `root #FFFFFFFF`, `panel #FFCC00FF`, `child #FFCC00FF`, `badge #FF0000FF`, then `child #FFFFFFFF` after the class removal.
**Needs:** Phase 6
**Agent tier:** ADVANCED (opus)

**Tier note:** The inheritance adds a mark of the walked kind inside the walk (A3). A stale read of the dirty bits skips a child, and a child then keeps an old color until an unrelated change.

**Tasks:**
- [ ] 7.1 Edit `src/BrutalUi.Core/Tree/ComputedStyle.cs`: add `Color`, `Font`, and `FontSize` with doc comments (the units, the inheritance, and the font id rule that `Label` held). Rewrite the class remarks for the new contract: the style pass writes the fields, the game writes `Element.Inline`, and a write marks the dirty bits itself.
- [ ] 7.2 Edit `StyleProperties.cs`: add the 3 text members to the enum and the struct, `StyleMasks.Text`, and the branches of `ApplyTo`. Update `EachMemberHasOneBitInOneMask` to three masks.
- [ ] 7.3 Edit `InlineStyle.cs`: add the 3 text members.
- [ ] 7.4 Edit `UiContext.cs`: add `DefaultColor`, `DefaultFont`, and `DefaultFontSize` to `CreateInfo` (A2). Store the resolved defaults: a null color gives opaque white. The parameterless constructor uses white, id 0, and size 0. `UpdateStyles` passes the defaults to each root.
- [ ] 7.5 Edit `StylePass.cs`: the inheritance step, `textChanged` in `Compare` (a text change gives `Paint`), and the child marks of A3. Read `element.Dirty` again after `Restyle`.
- [ ] 7.6 Write `tests/BrutalUi.Core.Tests/Style/TextInheritanceTests.cs`:
  - `TextColorFlowsDownTheTree` (the demo): a context with no default color, a sheet with `.panel { Color = (255, 204, 0, 255) }`, and the tree root > panel > child, and panel > badge with `badge.Inline.Color = (255, 0, 0, 255)`. Print the four lines. Remove the class `panel`, `Update`, and print `child` again. Assert all lines.
  - `ARootTakesTheContextDefaults` (each of the 3 members from `CreateInfo`), `TheParameterlessContextUsesWhite`, `ARuleWinsOverTheParent`, `AnInlineValueWinsOverARule`, and `AChainOfSixInherits`.
  - `AParentChangeReachesAGrandchild` (A3: the grandchild has no own bit before the change), `AChildWithItsOwnValueKeepsIt`, `ATextChangeGivesPaint` (read after `UpdateStyles()` on a plain element), and `TheSameTextValueGivesNoBit`.
- [ ] 7.7 Run the build, the tests, and the demo. Record the printed lines in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (6):** `ComputedStyle.cs`, `StyleProperties.cs`, `InlineStyle.cs`, `UiContext.cs`, `StylePass.cs`, `TextInheritanceTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the five lines, and the test asserts them.
- [ ] `ApplyToWritesEachMember` and `TheEnumCoversEachComputedStyleMember` pass with 32 members.
- [ ] `Label` still uses its own fields. Each label test passes with no change.

---

### Phase 8: A Label Draws in the Inherited Text Style
**Goal:** `Label` reads `Color`, `Font`, and `FontSize` from `ComputedStyle`, its own text values go, and a new font or size re-measures it.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALabelDrawsInTheInheritedTextStyle' --show-live-output on` and reads `rect 0,0,40,16` and `Text 0,0 font 0 size 16 #FFCC00FF wrap 40 "Hello"`, then `rect 0,0,80,32` and `Text 0,0 font 0 size 32 #FFCC00FF wrap 80 "Hello"` after the new panel size.
**Needs:** Phase 7
**Agent tier:** ADVANCED (opus)

**Tier note:** The text move touches the measure callback, the draw hook, the sample, and nine test files in two projects. A missed `Layout` bit gives a label with the size of its old font.

**Tasks:**

Group A (the label, 6 files):
- [ ] 8.1 Edit `src/BrutalUi.Core/Elements/Label.cs`: remove `_font`, `_size`, `_color`, the properties `Font`, `Size`, and `Color`, and the create-info members `Color`, `Font`, and `Size` (Q3). `PaintContent` and `MeasureLabel` read `Style.Color`, `Style.Font`, and `Style.FontSize`. The class remarks state that the text values come from a rule, the inline set, or the parent.
- [ ] 8.2 Edit `StylePass.cs`: a change of `Font` or `FontSize` on a `Label` adds `Layout` (`element is Label`, in the form of `UiContext.RefreshSubtree`).
- [ ] 8.3 Edit `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`: each create-info color and size moves to `label.Inline.Color` and `label.Inline.FontSize`. Add `ALabelDrawsInTheInheritedTextStyle` (the demo): the tree of `DrawsAOneLineLabel`, a panel with the class `panel` between the root and the label, and a sheet with `.panel { Color = (255, 204, 0, 255); FontSize = 16 }`. Print and assert. Then `panel.Inline.FontSize = 32`, `Update`, `Draw`, print, and assert.
- [ ] 8.4 Edit `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`: remove the setter tests of `Font`, `Size`, and `Color`. Add: a new `FontSize` re-measures (`MeasureCount` goes up) and draws the new size. A new `Font` re-measures. A new `Color` does not re-measure and draws the new color. The same value does not re-measure. A label with no value takes the context defaults.
- [ ] 8.5 Edit `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs` and `ContentBoxTests.cs`: move the create-info text values to `Inline`.

Group B (the other users, 4 files):
- [ ] 8.6 Edit `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`: the labels get their color and size from a parent `Inline` set. The test still measures 0 bytes.
- [ ] 8.7 Edit `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs` and `ImGuiRendererAllocationTests.cs`: move the text values to `Inline`. Each expected rect and vertex bound stays.
- [ ] 8.8 Edit `samples/HelloPanel/HelloScreen.cs`: `greeting.Inline.Color = GreetingColor;` and `greeting.Inline.FontSize = GreetingFontSize;`. The remarks keep the font 0 rule.
- [ ] 8.9 Run the build, the tests, the demo, and `scripts/test-linux.sh; echo "exit=$?"`. Run `grep -rnE 'label\w*\.(Color|Font|Size)\b' src samples tests --include=*.cs` and expect no hit. Record the results in `context.md`.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (10):** Group A: `Label.cs`, `StylePass.cs`, `LabelTests.cs`, `LabelTextTests.cs`, `LabelWrapTests.cs`, `ContentBoxTests.cs`. Group B: `ElementAllocationTests.cs`, `ImGuiLabelTests.cs`, `ImGuiRendererAllocationTests.cs`, `HelloScreen.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the four lines, and the test asserts them.
- [ ] `Label` has no `Font`, `Size`, or `Color` member, and `Label.CreateInfo` holds `Text` and `LocalizedText` only.
- [ ] Each ImGui label test passes with the same rects and vertex bounds. The smoke run passes.

---

### Phase 9: A Type Selector Styles Each Label
**Goal:** A rule matches an element type or a class on a type, through a type id that each element class declares, with the specificity of A5.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ATypeSelectorStylesEachLabel' --show-live-output on` and reads `label #FFCC00FF size 0`, `title label #00FF00FF size 24`, and `title panel #00FF00FF size 0`.
**Needs:** Phase 8
**Agent tier:** STANDARD (sonnet)

**Tasks:**

Group A (the type id, 6 files):
- [ ] 9.1 Write `src/BrutalUi.Core/Tree/ElementTypeId.cs` in the form of `ClassId.cs`.
- [ ] 9.2 Write `src/BrutalUi.Core/Tree/ElementType.cs` in the form of `ClassName.cs`: a global table with one lock, `Intern`, and `GetName`. The comparison is ordinal.
- [ ] 9.3 Edit `Element.cs`: `public virtual ElementTypeId TypeId => s_typeId;` with `private static readonly ElementTypeId s_typeId = ElementType.Intern("Element");` (Q2). The doc states: a subclass overrides the property with its own name, and a subclass with no override has the id of its base class.
- [ ] 9.4 Edit `Label.cs` and `Image.cs`: override `TypeId` with `"Label"` and `"Image"`.
- [ ] 9.5 Write `tests/BrutalUi.Core.Tests/Style/ElementTypeTests.cs`: the same name gives the same id, `Element`, `Label`, and `Image` have three ids, `ElementType.Intern("Label")` equals `label.TypeId`, a test subclass with no override has the id of `Element`, a test subclass with an override has its own id, `GetName` gives the name back, and `Intern` throws for null and for an empty name.

Group B (the selector, 3 files):
- [ ] 9.6 Edit `Selector.cs`: add `Type`, `ForType`, and `ForTypeAndClass`. `Specificity` gives 1, 10, or 11 (A5). `Matches` compares `Type` with the type id of the element when `Type` is not none, and checks the class when `Class` is not none. Both none throws at creation.
- [ ] 9.7 Edit `StylePass.cs`: read `element.TypeId` one time in `Restyle` and pass it to `Matches`.
- [ ] 9.8 Write `tests/BrutalUi.Core.Tests/Style/TypeSelectorTests.cs`:
  - `ATypeSelectorStylesEachLabel` (the demo): a sheet with `Label { Color = (255, 204, 0, 255) }`, `.title { Color = (0, 255, 0, 255) }`, and `Label.title { FontSize = 24 }`, added in that order. A label, a label with `.title`, and a plain element with `.title`. Print and assert the three lines.
  - `TheSpecificityIsOneTenOrEleven`, `AClassRuleBeatsATypeRuleInEachOrder`, `TheBuilderSortsATypeRuleFirst`, `ATypeRuleDoesNotMatchASubclassWithItsOwnId`, and `ForTypeAndClassThrowsForTwoNones`.
- [ ] 9.9 Run the build, the tests, and the demo. Run `grep -nE 'System\.Reflection|GetType\(\)|typeof\(' src/BrutalUi.Core/Style/*.cs src/BrutalUi.Core/Tree/ElementType*.cs` and expect no hit. Record the results in `context.md`.

**Estimated Steps:** about 45 (9 tasks, about five steps for one task)

**Files (9):** Group A: `ElementTypeId.cs`, `ElementType.cs`, `Element.cs`, `Label.cs`, `Image.cs`, `ElementTypeTests.cs`. Group B: `Selector.cs`, `StylePass.cs`, `TypeSelectorTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the three lines, and the test asserts them.
- [ ] The grep of task 9.9 finds no reflection in the match path.

---

### Phase 10: Two Sheets and a Sheet Change
**Goal:** A context with two sheets merges their rules in the order of `AddSheet` (A4), and a sheet removal restyles the tree (A8).
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALaterSheetWinsAndARemoveRestyles' --show-live-output on` and reads `panel #3060C0FF`, then `panel #1E222CFF` after the removal of the theme sheet.
**Needs:** Phase 9
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 10.1 Edit `UiContext.cs`: add `RemoveSheet` (null throws, an absent sheet gives `false`, the cascade rebuild, and `MarkSubtreeStyle` on each root). `AddRoot` calls `MarkSubtreeStyle` on the new root, so a root from a context with other sheets gets a full restyle (open scope question 2). Update the doc comments of `AddRoot` and `RemoveRoot`.
- [ ] 10.2 Before the edit of task 10.1, search the tests for an assert of `Dirty` after `AddRoot`. Record each hit in `context.md`, and update each expectation after task 10.1.
- [ ] 10.3 Write `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs`:
  - `ALaterSheetWinsAndARemoveRestyles` (the demo): a base sheet with `.panel { BackgroundColor = (30, 34, 44, 255) }` and a theme sheet with `.panel { BackgroundColor = (48, 96, 192, 255) }`. Print after `Update`. `RemoveSheet(theme)`, `Update`, and print again.
  - `AHigherSpecificityInTheFirstSheetWins`, `ThreeSheetsMergeInTheOrderOfAdd`, `RemoveSheetGivesFalseForAnAbsentSheet`, `RemoveTheLastSheetKeepsTheInlineValues`, `AddSheetAfterARemoveRestyles`, and `ARootFromAnotherContextGetsTheNewSheets`.
- [ ] 10.4 Run the build, the tests, and the demo. Record the printed lines in `context.md`.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (2):** `UiContext.cs`, `MultiSheetTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] Each test of task 10.2 has its new expectation and a line in `context.md`.

---

### Phase 11: The Allocation Gate and the Record
**Goal:** Prove that `Update` allocates nothing with an active sheet on the 1000-element tree, prove that the gate blocks, and put the model into the design docs and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 10
**Agent tier:** ADVANCED (opus)

**Tier note:** The allocation gate is load-bearing. When the gate measures bytes above 0, find the cause in `src/` before a change of the test. Group B is plain documentation work.

**Tasks:**

Group A (the gate, 3 files):
- [ ] 11.1 Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: build a sheet of 12 rules (class, type, and class-on-type rules, among them `.selected` with a paint and a width value and `.muted` with a color) and add it to the context. Put classes on the rows and the sidebar items. Expose the sheet and the two class ids. Use the public API only.
- [ ] 11.2 Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: add `StyleKind = 8` and set `KindCount = 9`. The step toggles `.selected` on the grid slot and `.muted` on the sidebar, then marks `Style` on each element of `tree.All`. Add `TheStyleStepRestylesTheTree`: one style step flips the background of the grid slot and the inherited color of a sidebar label, and the next round flips them back. `TheTreeHoldsTheRequiredMix` asserts that the context holds the sheet.
- [ ] 11.3 Prove that the gate blocks. Copy `src/BrutalUi.Core/Style/StylePass.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Restyle`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Record both exit codes in `context.md`.
- [ ] 11.4 Run the demo class 5 times and record the results in `context.md`. Run the facts line of task 1.1 and record the final `bytes per element`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.

Group B (the record, 5 files):
- [ ] 11.5 Edit `docs/technical-design.md`. Section 4: the style pass at the seam and its own recursion. Section 5: `Inline`, `ref readonly Style`, and `TypeId`. Section 7: the model of "The Public API" (replace the sketch of `StyleRule`), the cascade order, the inline rule, the inheritance, the dirty rule table, and the defaults of `UiContext.CreateInfo`. Section 8: `Label` reads the text values from the style. Section 14: the style tests and the style step of the allocation gate.
- [ ] 11.6 Edit `CHANGELOG.md`: add one line under `### Added` (`StyleProperties`, `Selector`, `StyleRule`, `StyleSheet`, `InlineStyle`, `ElementTypeId`, the style pass, and the text members of `ComputedStyle`) and one line under `### Changed` (`Element.Style` is `ref readonly`, and `Label.Color`, `Label.Font`, and `Label.Size` go). Name the feature `style-system/stylesheet-model`.
- [ ] 11.7 Write `docs/features/style-system/human-testing.md` with a `## stylesheet-model` section in the form of `docs/features/imgui-renderer/human-testing.md`: the 7 phase demos and the Windows run of `scripts\test-windows.ps1`.
- [ ] 11.8 Edit the doc comments of `src/BrutalUi.Core/Layout/LengthEdges.cs` and `src/BrutalUi.Core/Rendering/Corners.cs`: the nested-write example uses a local variable, and the remarks name the whole-value write through `Inline`.
- [ ] 11.9 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build, the tests, and the grep of task 6.12 (expect no hit).

**Estimated Steps:** about 50 (9 tasks, about five to six steps for one task)

**Files (8):** Group A: `ThousandElementTree.cs`, `ThousandElementAllocationTests.cs` (and a temporary edit of `StylePass.cs`, restored). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/style-system/human-testing.md`, `LengthEdges.cs`, `Corners.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The 9 tests of `ThousandElementAllocationTests` pass in 5 runs, and both gates measure 0 bytes.
- [ ] The planted allocation gives a non-zero exit code, and the restored file is the same as the copy.
- [ ] The lint gives no hit above 2.5 for each changed doc.

**Total estimate:** about 570 steps over 11 phases and 112 tasks.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Inline API: Code sets an inline value with `el.Inline.Width = x`. The setter sets the mask bit and `Style` dirty. `Element.Style` becomes `ref readonly`.
- Q2 Type id: Each element class overrides a virtual `TypeId` property. It returns a static readonly id from an intern table.
- Q3 Label CreateInfo: `Label.CreateInfo` loses `Color`, `Font`, and `Size`. A label gets its text values from a rule, its inline set, or its parent.
- Q4 Phase 1 demo: The phase 1 demo is a unit test. It builds a sheet in code with a `.panel` rule, runs `Update`, and reads `BackgroundColor`.
- A1 `StyleProperties` is a value struct with a 64-bit set mask, one bit per `StyleProperty` enum member. It holds no heap object.
- A2 `UiContext.CreateInfo` gets `DefaultColor`, `DefaultFont`, and `DefaultFontSize`. A root inherits these values.
- A3 When the pass changes an inherited text value, it marks each child `Style` dirty. The child then inherits the new value.
- A4 With more than one sheet, rules merge in the order of `AddSheet`. A later sheet wins at equal specificity.
- A5 Specificity is a packed int: class = 10, type = 1. selector-matching adds the name and the pseudo-state.
- A6 A builder makes a `StyleSheet`, and the builder sorts the rules one time. The sheet does not change after the build.
- A7 The match scans each rule of each sheet for each dirty element. A rule index waits for a benchmark.
- A8 A sheet add or a sheet remove marks each root `Style` and `DescendantStyle`, so the next `Update` restyles the full tree.
- A9 `Overflow` counts as a layout property, and a change sets `Layout` only.

The epic interview confirmed the epic items Q1 to Q3, A2, A3, A8, and A9 (`epic-overview.md`). The decisions below cite the labels of the block above.

### Decision 1: A mask struct and a full rebuild for each dirty element (the chosen approach)
**Choice:** `StyleProperties` mirrors `ComputedStyle` with a `ulong` mask. The pass builds each restyled element from the defaults, applies the matched rules and the inline set, resolves the inheritance, and compares each field of the old and the new style.
**Rationale:** The rebuild has no reset path: a removed class, a removed sheet, and a cleared inline value all give the right result with no special case. The struct holds no heap object (A1), so the pass allocates nothing. The compare gives the dirty bits from the values, so an unchanged value sets no bit by construction. Each step is one testable function.
**Alternatives:** (a) A source generator for the member lists: a new build tool and a harder debug, for 32 members that change rarely. (b) A delta apply that writes only the members of the changed rules: it needs a reset path for each removed class and sheet, and it has more bug risk.

### Decision 2: The cascade list is one sorted array in the context
**Choice:** `AddSheet` and `RemoveSheet` build one `StyleRule[]` from all sheets, sorted with the unique key `(Specificity, sheet index, rule index)`. The pass scans this array in order and applies each matched rule at once.
**Rationale:** The order of A4 and of the requirements (specificity, then rule order) becomes the array order, so the pass needs no buffer and no sort for each element. The unique key makes the result of an unstable sort deterministic. The scan still visits each rule of each sheet (A7). The rebuild allocates at load time only.
**Alternatives:** (a) A scratch buffer of matched rules for each element and a sort: work in each frame and a pooled buffer. (b) A merge of the sorted sheets for each element: more code for the same result.

### Decision 3: The style pass uses its own recursion, not `DirtyWalk`
**Choice:** `StylePass.Visit` reads the bits, restyles, reads the bits again, enters the children with a style bit, and clears `Style` and `DescendantStyle` after the subtree.
**Rationale:** A3 marks each child `Style` during the pass. `DirtyWalk` forbids a visitor to mark the walked kind, and it reads the descendant bits before the visit, so it would skip the new marks. The `DirtyWalk` remarks name the own recursion with `ClearDirty` as the pattern for this case. The pass also needs the computed text values of the parent, which the walk does not pass.
**Alternatives:** A context flag that forces a restyle of each child: a second mechanism next to the dirty bits, and against the visit rule of the requirements.

### Decision 4: `InlineStyle` is a view struct with a setter for each member
**Choice:** `Element.Inline` gives a `readonly struct` view over the element, in the form of `ClassList`. Each setter writes the inline set, sets the bit, and marks `Style`. A set to the same value marks nothing. `Clear` removes a value.
**Rationale:** Q1 asks for a setter that marks. A `ref` to the inline set cannot reach the element to mark it. The view allocates nothing. The compare in the setter follows `Element.States` and the `Label` setters. `Clear` is the only way back to the rule value after an inline write, so the API needs it.
**Alternatives:** See open scope question 3 (the nested write).

### Decision 5: The specificity is a weighted sum in one int
**Choice:** `Selector.Specificity` gives 10 for each class and 1 for each type (A5): 1, 10, or 11 in this feature. The rule stores the value at creation.
**Rationale:** A5 gives the weights. The sum keeps the CSS order while each count stays below 10, and the depth constant of 2 keeps each count far below 10. `Selector.Specificity` is the one place that selector-matching changes.
**Alternatives:** Bit fields (for example 8 bits for each kind): the same order, but other numbers than the confirmed weights.

### Decision 6: The context defaults use a nullable color with white as the fallback
**Choice:** `CreateInfo.DefaultColor` is `Rgba8?`. Null gives opaque white. `DefaultFont` and `DefaultFontSize` are plain fields with the defaults id 0 and size 0.
**Rationale:** `default(Rgba8)` is transparent. A transparent default hides each label with no rule and no inline value, which hides a bug. `Image.CreateInfo.Tint` uses the same pattern. Font id 0 and size 0 are the defaults of the renderer. A `required` color would force a change of each `CreateInfo` site for no gain.
**Alternatives:** (a) A `required Rgba8 DefaultColor`: explicit, but noise at each site. (b) Transparent: each label vanishes until a sheet sets a color.

### Decision 7: The transitional gates and four base phases for the migration
**Choice:** Gate A (phase 1) and gate B (phases 2 to 6) of "The Transitional Gates", and the migration recipe in four base phases of 12 files each.
**Rationale:** The rebuild wipes each direct write of an unmigrated test. The gates keep the solution green after each group, so each phase ends with 0 failed. The slice rules allow 6 files for each group and 2 groups for each phase, so 45 test files need 4 phases. The lock at the end of phase 6 proves the migration with the compiler.
**Alternatives:** (a) One phase for the whole migration: against slice rule 3, and one broken file blocks the test run of all others. (b) A bridge that copies `Style` writes into the inline set: there is no hook for a write through a `ref`.

### Decision 8: A type id from a global intern table, in the form of `ClassName`
**Choice:** `ElementType.Intern(string)` gives an `ElementTypeId`. `Element.TypeId` is virtual (Q2). `Label` and `Image` override it with a static readonly id.
**Rationale:** The pattern exists already for classes, so the parser of style-parser maps a type name with one call. A virtual property is one call for each restyled element, with no reflection (A3 of the epic).
**Alternatives:** `GetType()` and a dictionary: reflection-adjacent, and a hash lookup for each element.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The migration is large: about 3770 writes, 321 of them nested, in 46 files. | The phases run long, and a wrong edit changes a fixture value. | The recipe with one `replace_all` for each file and the compiler for the nested writes. Four base phases. The `AssertRect` diff check of phases 5 and 6. |
| 2 | Gate B skips an element that a migrated test needs, or keeps a stale direct write. | A test fails for a reason outside its own change. | Each file moves completely. A shared tree moves with each file that writes its elements (phase 3 group A). Phase 6 removes the gate, and the compiler proves that no direct write remains. |
| 3 | `ApplyTo`, `InlineStyle`, or `Compare` misses a member. | A rule value never reaches the style, or a change sets no bit. | `ApplyToWritesEachMember`, `TheEnumCoversEachComputedStyleMember`, and an inline test for each member group. |
| 4 | The pass reads the dirty bits before the A3 child marks. | A child keeps an old inherited value. | The second read of `element.Dirty` after `Restyle`. `AParentChangeReachesAGrandchild` and `AChainOfSixInherits`. |
| 5 | The element grows by the size of `StyleProperties` (about 300 bytes) and of the text members. | About 300 KB more for 1000 elements. | A1 accepts the inline set. Tasks 1.1, 2.7, and 11.4 record the facts line. A later feature can move the inline set to a side table when a benchmark shows the cost. |
| 6 | A test asserts the old contract ("a style write marks nothing") or an exact `Dirty` value. | The test fails after the migration. | Recipe step 6 and tasks 4.8 and 4.9. Each rewrite has a line in `context.md`. |
| 7 | The recursion frame of the pass holds the 300-byte `ComputedStyle` local. | A deep tree uses much more stack. | `Restyle` has `NoInlining`, so the local lives in a leaf frame. The `StackProbe` benchmark covers the depth of the tree. |
| 8 | The `AddRoot` mark of phase 10 changes an existing assert of `Dirty`. | A tree test fails. | Task 10.2 searches first and records each change. |
| 9 | `Label` with no rule draws in an unexpected color in the sample. | The smoke run shows a wrong screen. | Decision 6 (white), and `HelloScreen` sets the color inline (task 8.8). |
| 10 | `AddSheet` and `RemoveSheet` allocate. | A game that swaps sheets each frame allocates. | The doc comment states that the calls belong in load code. The allocation gate does not swap sheets. |

---

## Dependencies

**Internal (done):**
- core-tree: `Element`, `ClassList`, `ClassSet`, `ClassName`, `DirtyFlags`, `ComputedStyle`, `DirtyWalk`, `UiContext.Update`, and the allocation gates.
- imgui-renderer: the paint members of `ComputedStyle`, `PaintWalk`, `Label`, `Image`, `RecordingRenderer`, and `UiContext.CreateInfo`.

**External (pinned, no change):**
- `Ahwoo.BRUTAL.Numerics`, `Ahwoo.BRUTAL.Localization`, `Ahwoo.BRUTAL.ImGui` (the ImGui tests only), and `xunit.v3`. No new package and no native dependency.

**Downstream:**
- selector-matching extends `Selector`, `Selector.Specificity`, and `Selector.Matches`, and marks `DescendantStyle` on a class, state, or name change.
- style-parser fills `StyleSheet.Builder`, maps type names through `ElementType.Intern`, and adds the transition member to `StyleRule`.
- transitions run after `UpdateStyles` and before the layout pass.

---

## Definition of Done

### Functional
- [ ] F1. A sheet with a class rule gives the matched element the rule values after `Update`. An element with no match keeps the defaults.
- [ ] F2. At equal specificity, a later rule wins, and a later sheet wins (A4). A rule with a higher specificity wins in each order.
- [ ] F3. An inline value wins over each rule. `Inline.Clear` gives back the rule value or the default on the next `Update`.
- [ ] F4. `Color`, `Font`, and `FontSize` inherit from the parent when no rule and no inline value sets them. A root takes the context defaults. The parameterless `UiContext` gives opaque white, id 0, and size 0.
- [ ] F5. A changed layout member gives `Layout`. A changed paint or text member gives `Paint`. A changed `Font` or `FontSize` on a `Label` also gives `Layout`. An unchanged value gives no bit. A changed inherited value marks each child `Style` (A3).
- [ ] F6. A type selector matches the type id of the element, and a class-on-type selector needs both. The specificities are 1, 10, and 11.
- [ ] F7. `AddSheet` and `RemoveSheet` restyle the full tree on the next `Update`. `AddSheet` throws for null and for a held sheet. `RemoveSheet` gives `false` for an absent sheet.
- [ ] F8. `Element.Style` is `ref readonly`. `Label` has no `Color`, `Font`, or `Size` member, and `Label.CreateInfo` holds `Text` and `LocalizedText` only.

### Quality
- [ ] Q1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30` prints `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` prints 0 failed. The total is at least the total of task 1.1 plus the new tests, less the removed `Label` setter tests (`context.md` lists them).
- [ ] Q3. `ThousandElementAllocationTests` (9 tests), `ElementAllocationTests`, and `TheStylePassAllocatesNothingAfterWarmUp` pass, with 0 bytes. The planted allocation of task 11.3 gives a non-zero exit code.
- [ ] Q4. No expected rect of a layout test changed: `git diff -U0 main -- tests | grep -c '^[-+].*AssertRect'` prints `0`.
- [ ] Q5. Each public member of the new types has an XML doc comment (the build enforces it).
- [ ] Q6. `ste-lint.py --fail-over 2.5` exits with 0 for each changed doc.

### Integration
- [ ] I1. `git diff --quiet main -- Directory.Packages.props src/BrutalUi.ImGui src/BrutalUi.Markup '*.csproj'; echo "exit=$?"` prints `exit=0`.
- [ ] I2. `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs` prints nothing.
- [ ] I3. `grep -nE 'System\.Reflection|GetType\(\)|typeof\(' src/BrutalUi.Core/Style/*.cs src/BrutalUi.Core/Tree/ElementType*.cs` prints nothing.
- [ ] I4. `grep -rn "ImGui" src/BrutalUi.Core --include=*.cs` finds doc comments only.
- [ ] I5. `scripts/test-linux.sh; echo "exit=$?"` prints the PASS line and `exit=0`.

### Verification Method
1. Run Q1 and Q2.
2. Run each demo command of the slice phases (1, 2, 7, 8, 9, 10, and 11) and compare the printed lines with the Demo line.
3. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ElementAllocationTests'`. Expect `failed: 0`.
4. Run Q4, I1, I2, I3, I4, and I5.
5. Read `context.md` for the exit codes of task 11.3, the facts lines of tasks 1.1, 2.7, and 11.4, and the list of rewritten tests.

---

## Testing Strategy

- **Model unit tests (`Style/StylePropertiesTests`, `StyleSheetTests`, `ElementTypeTests`):** each member through the apply step, the masks, the sort, and the intern table. They need no context.
- **Pass tests (`StylePassTests`, `InlineStyleTests`, `TextInheritanceTests`, `TypeSelectorTests`, `MultiSheetTests`):** a small tree in a context. A test reads the dirty bits after `UpdateStyles()`, before the layout pass clears them, and reads the values after `Update`.
- **Label tests:** `RecordingRenderer` as the measurer and the renderer. `MeasureCount` shows a re-measure. The dump shows the color and the size of the draw.
- **Layout regression:** each Yoga fixture and `LayoutDifferentialTests` on the inline API, with no change of an expected rect.
- **Allocation gates:** `TheStylePassAllocatesNothingAfterWarmUp` (phase 1, small tree) and the style step of `ThousandElementAllocationTests` (phase 11). A planted allocation proves the gate.
- **Headless ImGui tests:** the label tests keep their rects and vertex bounds with the text values from the style.
- **Manual:** the smoke run of HelloPanel through `scripts/test-linux.sh`. style-parser owns the visual check of a sheet on a GPU.

---

## Related Documentation

- `docs/features/style-system/stylesheet-model/requirements.md`
- `docs/features/style-system/epic-overview.md` and `epic-requirements.md`
- `docs/features/style-system/selector-matching/requirements.md`, `style-parser/requirements.md`, and `transitions/requirements.md`
- `docs/features/imgui-renderer/label-and-image/implementation.md` (the label rules and the hand-off to this epic)
- `docs/technical-design.md` sections 4, 5, 7, 8, and 14
- `src/BrutalUi.Core/Tree/DirtyWalk.cs` (the visitor rules and the own-recursion pattern)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

### Open Scope Questions

The plan uses the recommended option of each question.

**Question 1: Which text members set `Layout` on a `Label`?**
- **Recommended:** `Font` and `FontSize` only. `Color` sets `Paint` only. Reason: the color does not change the size, and the old `Label.Color` setter marked `Paint` only. A re-measure for each color change costs a layout.
- Option B: each of the three text members, as the requirement reads word for word. Reason: literal, but a color change then re-measures each label below it.

**Question 2: Does `AddRoot` restyle the subtree of the new root?**
- **Recommended:** yes. `AddRoot` calls `MarkSubtreeStyle` (phase 10). Reason: a root from another context holds the style of the other sheets, and no other bit restyles it.
- Option B: no. Document that a root keeps its style until a change. Reason: no change of `AddRoot`, but a stale style after a move between contexts.

**Question 3: How does code change one edge of an inline edge set?**
- **Recommended:** a whole-value write: `el.Inline.Margin = el.Inline.Margin with { Top = 12 };`. Reason: it keeps the setter of Q1, and the compiler finds each old nested write.
- Option B: `ref` members on the view that mark on each access. Reason: the nested write compiles, but a read also marks `Style`, and Q1 asks for a setter.
- Option C: one member for each edge (`MarginTop` and so on, 16 more). Reason: short writes, but 16 more members in three types, and the mask still holds one bit for the edge set.

### Conflicts

1. **Q1 and the nested writes.** `el.Inline.Margin.Top = 12` does not compile (CS1612). The codebase holds 321 nested writes in 19 files. The plan keeps the item. The recipe turns each nested write into a whole write (question 3).
2. **Q1 and the whole-style writes.** `LayoutDifferentialTests` writes whole styles (`copy.Style = source.Style`, `element.Style = RandomStyle(random)`), and `ElementStateTests` writes `element.Style = default`. The plan keeps the item. The differential test copies the internal inline set (task 4.10), and the state test changes (task 6.9).
3. **A3 and `DirtyWalk`.** A3 marks each child `Style` during the pass. The rules of `IDirtyVisitor` forbid a visitor to mark the walked kind. The plan keeps the item and uses its own recursion (Decision 3).
4. **A8 and the visit rule.** A mark of the roots alone does not reach a descendant, because the pass visits only elements with `Style` or `DescendantStyle`, and a child holds neither bit. The plan keeps the item: each root gets `Style` and `DescendantStyle`, and `MarkSubtreeStyle` also marks each descendant, so the next `Update` restyles the full tree.
5. **A9 and the draw walk.** `PaintWalk` clips on `Overflow` (`PaintWalk.cs`, step 6), so a change of `Overflow` changes the paint. The plan keeps the item: a change sets `Layout` only. The draw walk draws the whole tree each frame, so the missing `Paint` bit has no effect today. A later paint cache must read `Layout` too. The same holds for `Display` and `BorderWidth`, which the draw walk also reads.
6. **A5 "packed int".** The item gives decimal weights (class 10, type 1). The plan reads "packed" as one int that holds the weighted sum (Decision 5). The order holds while each count stays below 10.
7. **The requirement "a text member change also sets `Layout` on a `Label`".** The plan sets `Layout` for `Font` and `FontSize` only (question 1).
8. **Q3 and the `required` rule of AGENTS.md.** `Label.CreateInfo` keeps no `required` member, because `Text` and `LocalizedText` both have a safe default. The rule asks for create-info structs with `required` for a member with no safe default, so no member needs it now.
9. **Q4 and the full rebuild.** With an active sheet, the pass of phase 1 rebuilds each element from the defaults and wipes a direct write. Gate A runs the pass only with a sheet until phase 2. Gate B skips an element with no inline value and no sheet until phase 6. During phases 2 to 5, the pass thus visits a dirty element and skips its restyle, which bends the visit rule of the requirements for the length of the migration.
10. **The technical design sketch.** Section 7 shows a mutable `struct StyleRule` with a field `Props`. The plan uses a `readonly struct` with `Properties` (A6: no change after the build). Task 11.5 updates the section.

### Gaps (not in this feature)

1. No universal selector (`*`) and no compound of two classes (`.a.b`). selector-matching owns the grammar.
2. No rule index. A7 waits for a benchmark. The 12-rule sheet of the allocation gate records the cost.
3. The inline set lives in each element, also when it is empty (Risk 5).
4. The `Label` check of the pass is a type check, in the form of `UiContext.RefreshSubtree`. A later text element (for example `Button`) needs a virtual hook on `Element` or a second check.
5. No per-edge mask bit. A rule that sets `Margin` sets all four edges. A longhand such as `margin-top` in style-parser needs a per-edge bit or a merge rule.

### Hand-offs (for `context.md` at the end of phase 11)

**selector-matching**
- Extend `Selector` with the name, the pseudo-states, the parent compound, and the combinator. Change the weights in `Selector.Specificity` only. `StyleRule` stores the value at creation.
- The pass calls `Selector.Matches(element, type)` once for each rule and element. A descendant match walks at most two ancestors there.
- Mark `DescendantStyle` on a class, state, or name change. `Element.MarkSubtreeStyle()` exists for a full subtree mark.

**style-parser**
- Build a sheet with `StyleSheet.Builder`. Map a type name with `ElementType.Intern` and a class with `ClassName.Intern`. The names of the built-in types are `Element`, `Label`, and `Image`.
- Each `StyleProperty` member needs one property name of the format. The edge sets take one to four values.
- Add the transition member to `StyleRule` with a new constructor overload. The existing constructor stays.

**transitions**
- `Restyle` holds the old and the new `ComputedStyle` before the write. A tween starts there or right after `UpdateStyles`, before the layout pass.
- An inline value has no transition (out of scope of transitions).
