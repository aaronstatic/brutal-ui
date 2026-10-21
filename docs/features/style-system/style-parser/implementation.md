# Style Parser - Implementation Plan

**Epic:** style-system (feature 3 of 4)
**Status:** Planning
**Started:** 2026-10-21
**Target Completion:** TBD
**Last Updated:** 2026-10-21 22:20

---

## Quality Bar

**Target quality:** A public library feature with a text format. A game author writes the format by hand, so each mistake must give an error at the right line and column, with a message that names the fix. The parser runs at load time only. The sheet that it gives must match the sheet that code gives through `StyleSheet.Builder`, so each test of stylesheet-model and selector-matching stays true for a parsed sheet. The transitions feature reads the transition data of this feature, so its shape must be stable.

- **A correct map:** Each of the 32 property names sets its own member of `StyleProperties` and no other member. Each selector form gives the selector that the factory of selector-matching gives. An edge set and a corner set follow the CSS order of one to four values. A test proves each form.
- **A correct error report:** A sheet with an error gives no `StyleSheet`. Each error holds a kind, a line, a column, a message, and the source name. The parser reports each error of a sheet, not only the first. No input makes `Parse` throw. A seeded random test proves it.
- **Theme variables at parse time (A9):** `var(--name)` reads a `--name` of a `:root` block of the same sheet. An unknown variable is an error at the position of its name. A variable of one sheet is not visible in a second sheet.
- **Zero allocation in the frame loop:** The parser can allocate. The frame loop runs no parser code. `UiContext.Update` allocates 0 bytes after warm-up with a parsed sheet that holds each selector form and a transition declaration. A planted allocation proves that the new gate blocks.
- **A visible result:** The HelloPanel sample loads `HelloScreen.css` and sets classes in place of inline values. The smoke run keeps its 3 lines (`draw commands 2, vertices 128`, and 0 bytes after warm-up) and its exit codes.
- **No new package and no reflection:** `BrutalUi.Markup` references Core only. `Directory.Packages.props` does not change. The parser uses explicit tables, not `Enum.Parse` and not reflection.

---

## Executive Summary

This feature adds the text format of a stylesheet and the parser that makes a `StyleSheet` from it. The format is the CSS subset of the technical design: a selector list, a block of `name: value;` declarations, and comments. The parser lives in `BrutalUi.Markup` and builds each sheet through the public API of Core (A1 of the epic requirements).

The parser has two layers. A tokenizer reads the UTF-8 bytes one time and gives a list of tokens, each with its line and column. A recursive descent parser then reads the list in two passes. Pass 1 collects the theme variables of each `:root` block. Pass 2 reads each rule, puts the variable values in place of each `var(--name)`, maps the selectors to `CompoundSelector` and the factories of selector-matching, and writes the values through the setters of `StyleProperties`. When an error occurs, the parser records it, skips to a safe point, and continues. Thus one parse reports each error. A sheet with an error gives no `StyleSheet`.

The feature also adds two parts to Core. `StyleTransitions` holds the `transition` declaration of a rule for the transitions feature, and `StyleRule` gets it through a new constructor overload. `CompoundSelector` and `Selector` get equality members and a CSS text form (CR-4 of selector-matching), so a parser test that compares two selectors shows the CSS text of each value.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The markup author** (writes `.hud-button:hover { background-color: var(--accent); }` in a file and reads an error with a line and a column)
- [x] **The game developer** (loads a sheet at startup, adds it to a `UiContext`, and sets classes, names, and states in code)
- [x] **The sibling feature agents** (transitions reads `StyleRule.Transitions`. The XML loader of Phase 3 loads a sheet next to a tree)
- [ ] **The end player** (not served directly. The player sees the result of the sheet)

**User Subsets Served:**
- [x] **Simple** - `new StyleSheetParser().Parse(File.ReadAllBytes(path), "hud.css").GetSheetOrThrow()` gives a sheet. A class rule with a color paints a panel.
- [x] **Complex** - A theme block of variables, a chain with a state on the ancestor, edge sets of four values, a font from a font map, and a list of transitions in one rule.

**UX Approach:**
- The format is CSS where CSS has the feature. The property names are the CSS names (`border-radius` for `CornerRadius`). The edge sets use the CSS order. A CSS author needs no new words.
- Each error names the problem and the fix. The text form is `HelloScreen.css(3,10): InvalidValue: <message>`, so an editor can jump to the position.
- The format rejects each CSS form that it does not support (`.a.b`, `*`, `+`, `~`, `[x]`, `:not`, `@import`, `!important`, `calc()`) with a positioned error. No CSS form gives a silent miss.

---

## Goals

### Primary Goals
1. Parse the CSS subset into a `StyleSheet` that matches a sheet built in code.
2. Accept each selector form of selector-matching and each of the 32 properties of `StyleProperties`, with the value forms of the requirements.
3. Resolve theme variables at parse time, for each sheet (A9).
4. Report each error with a kind, a line, a column, and a message. Give no sheet for a text with an error.
5. Store the `transition` declaration in the rule for the transitions feature.
6. Move the HelloPanel sample to a stylesheet with no change of its smoke output.

### Success Criteria
- [ ] The phase 1 demo prints `panel #1E222CFF` and `plain #00000000`.
- [ ] The phase 2 demo prints `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after the hover of the panel.
- [ ] The phase 3 demo prints `panel 20,10,200,40`.
- [ ] The phase 4 demo prints `button #3060C0FF`, then `error 2:25 UnknownVariable`.
- [ ] The phase 5 demo prints `error 2:3 UnknownProperty`, `error 3:10 InvalidValue`, `error 4:1 UnexpectedEnd`, and `sheet null`.
- [ ] The phase 6 demo prints `BackgroundColor 0.2s EaseOut`, then `Opacity none`.
- [ ] `scripts/test-linux.sh` prints the PASS line, and the smoke log holds `HelloPanel: last frame: draw commands 2, vertices 128.`

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/Style/
├── CompoundSelector.cs           # phase 2: IEquatable, ==, !=, GetHashCode, ToString (CR-4)
├── Selector.cs                   # phase 2: the same members (CR-4)
├── StyleTransitions.cs           # new. Phase 6: TransitionEasing, StyleTransition, StyleTransitions
├── StyleRule.cs                  # phase 6: the Transitions field and the new constructor
└── StyleSheet.cs                 # phase 6: the new Add overload
src/BrutalUi.Markup/
├── BrutalUi.Markup.csproj        # phase 1: InternalsVisibleTo BrutalUi.Markup.Tests
└── Style/
    ├── StyleParseError.cs        # new. Phase 1: StyleErrorKind and StyleParseError. Phase 5: the messages
    ├── StyleParseResult.cs       # new. Phase 1
    ├── StyleToken.cs             # new. Phase 1 (internal)
    ├── StyleTokenizer.cs         # new. Phase 1 (internal)
    ├── StyleSheetParser.cs       # new. Phase 1, then a part in each phase up to 6
    ├── StyleValues.cs            # new. Phase 1: colors. Phase 3: each form. Phase 5: the range checks. Phase 6: transitions
    ├── SelectorReader.cs         # new. Phase 2 (internal)
    ├── PropertyTable.cs          # new. Phase 3 (internal). Phase 6: the transition entry
    └── ThemeVariables.cs         # new. Phase 4 (internal)
samples/HelloPanel/
├── HelloScreen.css               # new. Phase 7
└── HelloScreen.cs, Program.cs, HelloPanel.csproj      # phase 7
tests/BrutalUi.Markup.Tests/
├── BrutalUi.Markup.Tests.csproj  # phase 7: links AllocationGate.cs and HelloScreen.css
├── PlaceholderTests.cs           # deleted in phase 1
└── Style/
    ├── StyleTokenizerTests.cs, TextSheetTests.cs                  # phase 1
    ├── SelectorFormTests.cs                                       # phase 2
    ├── PropertyFormTests.cs, EdgeAndCornerTests.cs                # phase 3
    ├── ThemeVariableTests.cs                                      # phase 4
    ├── ErrorReportTests.cs, ParserRobustnessTests.cs              # phase 5
    ├── TransitionDeclarationTests.cs                              # phase 6
    └── ParsedSheetAllocationTests.cs                              # phase 7
tests/BrutalUi.Core.Tests/Style/
├── SelectorEqualityTests.cs      # new. Phase 2
└── StyleTransitionsTests.cs      # new. Phase 6
docs/technical-design.md, CHANGELOG.md, docs/features/style-system/human-testing.md   # phase 7
```

`src/BrutalUi.ImGui/`, `Directory.Packages.props`, and the code of `StylePass.cs` do not change. Each new file holds its own `using` lines (no implicit usings).

### The Public API

```csharp
namespace BrutalUi.Markup;

public sealed class StyleSheetParser                      // phase 1
{
    public struct CreateInfo
    {
        public IReadOnlyDictionary<string, FontHandle>? Fonts;   // phase 3. null: only "font: default;" (question 4)
    }

    public StyleSheetParser();
    public StyleSheetParser(in CreateInfo info);
    public StyleParseResult Parse(ReadOnlySpan<byte> utf8, string sourceName = "stylesheet");
    public StyleParseResult Parse(string text, string sourceName = "stylesheet");   // encodes to UTF-8, then parses
}

public sealed class StyleParseResult                      // phase 1
{
    public StyleSheet? Sheet { get; }                     // null when Errors holds an error
    public IReadOnlyList<StyleParseError> Errors { get; } // in the order of the source. Empty on success
    public bool Succeeded { get; }                        // Sheet is not null
    public StyleSheet GetSheetOrThrow();                  // InvalidOperationException with one line for each error
}

public readonly struct StyleParseError                    // phase 1
{
    public StyleErrorKind Kind { get; }
    public int Line { get; }                              // from 1
    public int Column { get; }                            // from 1, in code points. A tab counts 1
    public string Message { get; }                        // the problem and the fix (ASD-STE100)
    public string SourceName { get; }
    public override string ToString();                    // "HelloScreen.css(3,10): InvalidValue: <message>"
}

public enum StyleErrorKind : byte                         // phase 1: all members. Each phase uses more of them
{
    UnexpectedCharacter, UnterminatedComment, UnexpectedToken, UnexpectedEnd,
    InvalidSelector, UnsupportedSelector, UnknownPseudoState,
    UnknownProperty, InvalidValue, UnknownFont,
    UnknownVariable, InvalidVariable, UnsupportedSyntax,
}
```

```csharp
namespace BrutalUi.Core;

public readonly struct CompoundSelector : IEquatable<CompoundSelector>   // phase 2 (CR-4)
{
    // The four init parts stay. New: Equals, ==, !=, GetHashCode, and ToString ("Label.title#score:hover").
}

public readonly struct Selector : IEquatable<Selector>                   // phase 2 (CR-4)
{
    // The members stay. New: Equals, ==, !=, GetHashCode, and ToString (".a:hover > .b", ".a .b", "(empty)").
}

public enum TransitionEasing : byte { Linear, EaseIn, EaseOut, EaseInOut }   // phase 6

public readonly struct StyleTransition                    // phase 6
{
    public StyleTransition(float duration, TransitionEasing easing);
    public float Duration { get; }                        // seconds. The model keeps each float (transitions applies its rule)
    public TransitionEasing Easing { get; }
}

public struct StyleTransitions                            // phase 6. A sparse set of 5 slots (question 3)
{
    public static bool CanTransition(StyleProperty property);   // BackgroundColor, BorderColor, CornerRadius, Opacity, Color
    public readonly bool IsEmpty { get; }
    public readonly bool IsSet(StyleProperty property);
    public readonly StyleTransition Get(StyleProperty property); // default(StyleTransition) for an unset member
    public void Set(StyleProperty property, StyleTransition transition);   // ArgumentOutOfRangeException outside the 5
    public void Clear(StyleProperty property);
}

public readonly struct StyleRule
{
    public readonly StyleTransitions Transitions;         // phase 6. Empty from the old constructor
    public StyleRule(in Selector selector, in StyleProperties properties);                                    // stays
    public StyleRule(in Selector selector, in StyleProperties properties, in StyleTransitions transitions);   // phase 6
}

public sealed class StyleSheet.Builder
{
    public Builder Add(in Selector selector, in StyleProperties properties);                                  // stays
    public Builder Add(in Selector selector, in StyleProperties properties, in StyleTransitions transitions); // phase 6
}
```

`Parse` holds no state between calls. Each call makes its own token list and its own variable map. `ToString` of the selector types allocates and reads the global name tables (`ElementType.GetName`, `ClassName.GetName`). It never throws: an id outside a table gives the text `<type 7>` or `.<class 7>`. Use it for test output and debug output only.

### The Tokenizer (phase 1)

The tokenizer reads the full UTF-8 span one time and gives a `List<StyleToken>`. Each token holds its kind, the byte offset, the byte length, the line, and the column. The parser reads the list, so it can look ahead and read the list two times.

| Kind | Text |
|------|------|
| `Ident` | `[A-Za-z_]`, or `-` and then `[A-Za-z_-]`, and then `[A-Za-z0-9_-]*`. Thus `--accent`, `flex-start`, and `-x` are identifiers |
| `Hash` | `#` and then one or more of `[A-Za-z0-9_-]`. A name selector or a color, by context |
| `Number`, `Dimension`, `Percentage` | An optional `-`, then digits with an optional `.digits`, or `.digits`. An identifier right after the number gives a `Dimension` (`12px`, `0.2s`). A `%` right after it gives a `Percentage` |
| `Colon`, `Semicolon`, `Comma`, `Dot`, `Greater`, `LeftBrace`, `RightBrace`, `LeftParen`, `RightParen` | `:` `;` `,` `.` `>` `{` `}` `(` `)` |
| `Whitespace` | One run of space, tab, CR, LF, and FF. A comment `/* ... */` joins the run. The selector reader reads it as the descendant combinator |
| `Delim` | Each other printable ASCII byte (`*`, `[`, `+`, `~`, `!`, `@`, `"`, and more). The parser gives a positioned error for it |
| `End` | The end of the text. It holds the position after the last byte |

- **The position.** The line starts at 1. LF ends a line. CRLF and a lone CR each count as one line break. The column starts at 1 and counts code points: each byte that is not a UTF-8 continuation byte (`10xxxxxx`) adds 1. A tab adds 1. A UTF-8 BOM at offset 0 adds no column.
- **ASCII only outside comments.** A byte of 0x80 or more outside a comment gives `UnexpectedCharacter` at its position. The tokenizer skips the full code point. A comment can hold each UTF-8 text.
- **No throw.** An unterminated comment gives `UnterminatedComment` at the `/*` and ends the token list. The tokenizer reads each byte at most one time.

### The Grammar

```
sheet        := ( root-block | rule )* End
root-block   := ':root' '{' ( '--name' ':' value-tokens ';' )* '}'          phase 4
rule         := selector ( ',' selector )* '{' declaration* '}'
selector     := compound ( combinator compound )?                          at most 2 compounds (A9)
combinator   := Whitespace | '>'                                           Descendant | Child
compound     := Ident? ( '.' Ident | Hash | ':' Ident )*                    at least one part
declaration  := Ident ':' value ( ';' | before '}' )                        the last ';' is optional
value        := one token or more. Each 'var' '(' '--name' ')' takes the tokens of the variable
```

**The selector map (the hand-off of selector-matching).** A leading identifier is the type: `ElementType.Intern(name)`. `.name` is the class: `ClassName.Intern(name)`, without the `.`. A `Hash` is the name: `Name = "title"`, without the `#`. `:hover`, `:active`, `:focus`, `:disabled`, and `:checked` each set their bit of `States`. A repeat of a state adds nothing. One compound gives `Selector.For(subject)`. `>` gives `Selector.Child(parent, subject)`. Whitespace between two compounds gives `Selector.Descendant(ancestor, subject)`. Whitespace around `>`, after `,`, and before `{` is not a combinator. Each name is case-sensitive.

**The selector rejects.** The parser checks each case before it calls a factory, so no `ArgumentException` leaves `Parse`. Each error points at the first byte of the token at fault.

| Text | Kind | Position |
|------|------|----------|
| `.a.b` (a second class), `#a#b` (a second name) | `UnsupportedSelector` | the second `.` or `#` |
| `.a .b .c`, `.a > .b > .c` (a third compound) | `UnsupportedSelector` | the start of the third compound |
| `*`, `[x]`, `.a + .b`, `.a ~ .b`, `::before`, `:not(.a)` | `UnsupportedSelector` | the `*`, `[`, `+`, `~`, the second `:`, or `not` |
| `:first-child` (an unknown state) | `UnknownPseudoState` | the identifier after `:` |
| `> .a`, `.a >`, `.a, {`, `{` with no selector (an empty compound) | `InvalidSelector` | the token where the compound must start |
| `:root .a`, `.a, :root` (`:root` in a chain or a list) | `InvalidSelector` | the `:` of `:root` |

An unknown type name is not an error. `ElementType.Intern` gives it a new id, and the rule matches no element until a type with that name exists (A3). A selector list gives one rule for each selector, in the order of the source, with the same values. A rule with no declaration gives no `StyleRule`.

### The Properties and the Value Forms (phase 3)

| Property names | Member type | Value |
|----------------|-------------|-------|
| `flex-direction` | `FlexDirection` | `column`, `column-reverse`, `row`, `row-reverse` |
| `width`, `height`, `flex-basis`, `min-width`, `min-height`, `max-width`, `max-height`, `left`, `top`, `right`, `bottom` | `Length` | a length |
| `margin` | `LengthEdges` | 1 to 4 lengths |
| `padding` | `LengthEdges` | 1 to 4 lengths, with no `auto` (phase 5 check) |
| `border-width` | `LengthEdges` | 1 to 4 lengths in `px` or `0` (Yoga has no percent border) |
| `flex-grow`, `flex-shrink` | `float` | a number, 0 or more |
| `justify-content` | `Justify` | `flex-start`, `center`, `flex-end`, `space-between`, `space-around`, `space-evenly` |
| `align-items`, `align-self`, `align-content` | `Align` | `auto`, `flex-start`, `center`, `flex-end`, `stretch`, `space-between`, `space-around`, `space-evenly` |
| `flex-wrap` | `FlexWrap` | `nowrap`, `wrap`, `wrap-reverse` |
| `display` | `Display` | `flex`, `none` |
| `position` | `PositionType` | `relative`, `absolute` |
| `overflow` | `Overflow` | `visible`, `hidden`, `scroll` |
| `background-color`, `border-color`, `color` | `Rgba8` | a color |
| `border-radius` | `Corners` | 1 to 4 values in `px` or `0` |
| `opacity` | `float` | a number from 0 to 1 |
| `font` | `FontHandle` | `default` (id 0), or a key of `CreateInfo.Fonts` (question 4) |
| `font-size` | `float` | a value in `px` or `0`, 0 or more |
| `transition` | `StyleTransitions` | `<property> <duration> <easing>`, and more of them in a comma list (phase 6) |

- **A length** is `<number>px` (`Length.Px`), `<number>%` (`Length.Percent`), `auto` (`Length.Auto`), or a bare `0` (`Length.Px(0)`). Another bare number gives `InvalidValue` with the fix "add px or %".
- **A number** reads with `float.TryParse` on the UTF-8 span with `CultureInfo.InvariantCulture`. A value that is not finite gives `InvalidValue` (phase 5).
- **A color** is a `Hash` of 3, 6, or 8 hex digits in any case. `#rgb` doubles each digit and sets the alpha to 255. `#rrggbb` sets the alpha to 255. `#rrggbbaa` holds the alpha.
- **A keyword** maps through an explicit table for each enum. No `Enum.Parse` and no reflection.
- **A later declaration** of the same property in one rule wins, as in CSS. The `StyleProperties` setter writes the value again.
- **The text members inherit.** `color`, `font`, and `font-size` inherit down the tree in the style pass. A rule that sets only `color` still lets `font` and `font-size` inherit. Section 11 of the design doc states it.

**The CSS order of an edge set and a corner set.** The constructors of Core use another order: `new LengthEdges(left, top, right, bottom)` and `new Corners(topLeft, topRight, bottomRight, bottomLeft)`. `StyleValues` maps the CSS order to them in one place.

| Values | `margin`, `padding`, `border-width` | `border-radius` |
|--------|-------------------------------------|-----------------|
| `a` | each edge `a` | each corner `a` |
| `a b` | top and bottom `a`, left and right `b` | top-left and bottom-right `a`, top-right and bottom-left `b` |
| `a b c` | top `a`, left and right `b`, bottom `c` | top-left `a`, top-right and bottom-left `b`, bottom-right `c` |
| `a b c d` | top `a`, right `b`, bottom `c`, left `d` | top-left `a`, top-right `b`, bottom-right `c`, bottom-left `d` |

A rule sets all four edges of an edge set at one time. The format has no longhand such as `margin-top` (out of scope).

### Theme Variables (phase 4, A9)

- **Pass 1** reads each `:root` block of the token list. Each declaration `--name: tokens;` stores the token range of its value, with no leading and no trailing whitespace. A later definition of the same name wins. A block can come before or after the rules that read it.
- **Pass 2** skips each `:root` block. In a declaration value, `var(--name)` takes the tokens of the variable. A value can mix variables and other tokens: `padding: var(--gap) 8px;`. A variable can hold more than one value: `--pad: 4px 8px;`.
- **The errors.** An unknown name gives `UnknownVariable` at the `--name` token inside `var(`. A `--name` declaration outside `:root`, a declaration in `:root` that is not a variable, an empty variable value, a `var()` inside a variable value, and a fallback (`var(--a, 4px)`) each give `InvalidVariable`. An error in a value that comes from a variable points at the `var` token of the declaration, and the message names the variable.
- **The scope.** The variable map lives in one `Parse` call. A second sheet does not see it (A9). Runtime theme variables are out of scope.

### The Error Report (phase 5)

- The parser records each error and continues. After an error in a declaration, it skips to the next `;` or `}` of the same block. After an error in a selector, it skips the full block of the rule. After an error at the top level, it skips to the next `}`. A missing `}` at the end gives `UnexpectedEnd` at the position of the `End` token.
- `Errors` holds the errors in the order of the source. A text with one error or more gives `Sheet == null`.
- An at-rule (`@import`, `@media`) gives `UnsupportedSyntax` at the `@`. `!important` gives `UnsupportedSyntax` at the `!`. A function other than `var` (`calc(`, `rgb(`) gives `UnsupportedSyntax` at its name.
- Each message names the problem and the fix in one or two short sentences, quotes the source text, and follows ASD-STE100. Example: `'colour' is not a property of the format. Check the spelling, for example 'color'.` The tests assert the kind, the line, and the column, and not the full message text.
- `ToString` of an error gives `<source>(<line>,<column>): <kind>: <message>`. `GetSheetOrThrow` throws `InvalidOperationException` with one such line for each error.

### Data Flow

```
Load:   File.ReadAllBytes("HelloScreen.css") ─► parser.Parse(utf8, "HelloScreen.css")
          ─► StyleTokenizer ─► List<StyleToken> (+ errors)
          ─► pass 1: ThemeVariables (each :root block)
          ─► pass 2: each rule ─► SelectorReader ─► Selector.For / Child / Descendant
                                 ─► each declaration ─► var() in place ─► PropertyTable ─► StyleValues
                                 ─► StyleProperties setters (+ StyleTransitions)
          ─► an error? ─► StyleParseResult { Sheet = null, Errors }
          ─► else StyleSheet.Builder.Add(selector, properties, transitions), in the order of the source ─► Build()
        context.AddSheet(result.GetSheetOrThrow())
Frame:  no parser code. UiContext.Update ─► StylePass (no change) ─► Selector.Matches ─► ApplyTo
```

---

## Implementation Phases

Each phase is a slice, per section 11 of `.beast-mode/phase-handoff.md`. No phase is a base phase. The CR-4 work rides with phase 2, because the selector tests of that phase need it. The Core types of the transition ride with phase 6. The requirements leave the demo of phase 1 open, so phase 1 uses the recommended line of open scope question 1 (rule 12).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The class table, the type table, and the names are global, and xUnit runs classes in parallel. Thus each test interns its names with the prefix `nameof(Test) + "-"`. The prefix uses `-`, because a `.` ends a class name in the format. A color prints as `#RRGGBBAA` (`Rgba8.ToString`). An error demo prints `error <line>:<column> <kind>`, so the demo does not depend on the message text.

### Phase 1: A Text Sheet Paints a Panel
**Goal:** The smallest path that proves the idea: the full tokenizer, the result types, and a parser for one class selector and the three color properties, through the builder and the style pass.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetPaintsAPanel' --show-live-output on` and reads `panel #1E222CFF` and `plain #00000000`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase fixes the public API of the parser, the token model, and the position rule. Each later phase builds on them. A wrong column rule or a tokenizer that can throw gives a defect in each error test.

**Tasks:**

Group A (the tokenizer, 5 files):
- [ ] 1.1 Record in `context.md`: the base commit (`git rev-parse HEAD`), the test total (expected 1238), the build result, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30`. Read `StyleSheet.cs`, `StyleProperties.cs`, `Rgba8.cs`, `ClassName.cs`, `ElementType.cs`, `src/BrutalUi.Markup/BrutalUi.Markup.csproj`, and `tests/BrutalUi.Markup.Tests/PlaceholderTests.cs`.
- [ ] 1.2 Edit `src/BrutalUi.Markup/BrutalUi.Markup.csproj`: add `<InternalsVisibleTo Include="BrutalUi.Markup.Tests" />`, as `BrutalUi.Core.csproj` does. Add no `PackageReference`.
- [ ] 1.3 Write `src/BrutalUi.Markup/Style/StyleParseError.cs`: `StyleErrorKind` with all 13 members per "The Public API" (each with a doc comment that names its cause), and `StyleParseError` with `ToString` per "The Error Report".
- [ ] 1.4 Write `src/BrutalUi.Markup/Style/StyleToken.cs` (the internal `StyleTokenKind` and `StyleToken`) and `src/BrutalUi.Markup/Style/StyleTokenizer.cs` (internal) per "The Tokenizer": each token kind, the position rule, the BOM, the ASCII rule, and the comment rule. The tokenizer adds its errors to a list that the parser gives it.
- [ ] 1.5 Write `tests/BrutalUi.Markup.Tests/Style/StyleTokenizerTests.cs`:
  - `EachTokenKindHasItsText` (a theory with one row for each kind, also `--accent`, `-4px`, `.5`, `50%`, `0.2s`, and `#1e222c`).
  - `ALineBreakStartsANewLine`: LF, CRLF, and a lone CR each give line 2, column 1 for the next token.
  - `AColumnCountsCodePoints`: `/* é */ .a` gives column 9 for the `.`. A tab counts 1.
  - `ABomAddsNoColumn`, `ACommentJoinsTheWhitespace`, and `ANonAsciiByteOutsideACommentIsAnError`.
  - `AnUnterminatedCommentIsAnError`: the position of the `/*`.
  - `TheEndTokenHoldsThePositionAfterTheText`: a text that ends with LF gives `End` at the next line, column 1.

Group B (the parser, 5 files):
- [ ] 1.6 Write `src/BrutalUi.Markup/Style/StyleParseResult.cs` per "The Public API", with `GetSheetOrThrow`.
- [ ] 1.7 Write `src/BrutalUi.Markup/Style/StyleValues.cs` (internal static): the color read of "The Properties and the Value Forms" (3, 6, or 8 hex digits). Another digit count gives `InvalidValue`.
- [ ] 1.8 Write `src/BrutalUi.Markup/Style/StyleSheetParser.cs` per "The Public API": the two `Parse` overloads, the empty `CreateInfo`, and the rule loop of this phase. A selector is one `.class` compound. A declaration is one of `background-color`, `border-color`, and `color` with one color. The last `;` is optional. Each other form gives an error at its position (`UnexpectedToken`, `UnknownProperty`, or `InvalidValue`), and the parse stops at the first error in this phase (phase 5 adds the recovery). The rules go to `StyleSheet.Builder.Add` in the order of the source. The doc comments state that the parser can allocate and that the sheet does not.
- [ ] 1.9 Delete `tests/BrutalUi.Markup.Tests/PlaceholderTests.cs`: the new tests load the assembly. Write `tests/BrutalUi.Markup.Tests/Style/TextSheetTests.cs`:
  - `ATextSheetPaintsAPanel` (the demo): `.<prefix>panel { background-color: #1e222c; }`. A panel with the class and a plain element with no class under one root. Print `panel <BackgroundColor>` and `plain <BackgroundColor>` after `Update`. Assert both lines.
  - `EachColorFormGivesTheColor` (a theory): `#fff`, `#FFF`, `#1e222c`, `#1E222C80`, and `#3060c0ff`.
  - `EachColorPropertySetsItsOwnMember`: the mask of each rule holds one bit only.
  - `CommentsAndWhitespaceChangeNothing`: comments between each token, CRLF line breaks, and a BOM give an equal sheet.
  - `TheLastSemicolonIsOptional`, and `ALaterRuleWinsAtEqualSpecificity`.
  - `AnErrorGivesNoSheet`: `.a { colour: #fff; }` gives `Sheet == null` and one `UnknownProperty` error at 1:6.
  - `GetSheetOrThrowNamesTheError`: the exception text holds `stylesheet(1,6): UnknownProperty:`.
- [ ] 1.10 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (10):** Group A: `BrutalUi.Markup.csproj`, `StyleParseError.cs`, `StyleToken.cs`, `StyleTokenizer.cs`, `StyleTokenizerTests.cs`. Group B: `StyleParseResult.cs`, `StyleValues.cs`, `StyleSheetParser.cs`, `PlaceholderTests.cs` (deleted), `TextSheetTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] Each existing test passes. The total is 1238, minus the placeholder test, plus the new tests.
- [ ] `grep -nE 'PackageReference' src/BrutalUi.Markup/BrutalUi.Markup.csproj` prints nothing.

---

### Phase 2: A Hover on a Panel Restyles Its Row and Its Cell from Text
**Goal:** Each selector form of selector-matching from text, the reject list with positions, and the equality members and the CSS text form of the selector types (CR-4).
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCellFromText' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after `panel.States = Hover`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** Group A adds public API to Core types that the cascade uses. A wrong `Equals` or `GetHashCode` breaks no match, but it gives wrong test results in each later phase. Group B holds the full selector grammar, the reject order, and the whitespace rule of the descendant combinator.

**Tasks:**

Group A (CR-4 in Core, 3 files):
- [ ] 2.1 Edit `src/BrutalUi.Core/Style/CompoundSelector.cs`: implement `IEquatable<CompoundSelector>`, `Equals(object?)`, `==`, `!=`, and `GetHashCode` over `Type`, `Class`, `States`, and `Name` (`string.Equals(..., StringComparison.Ordinal)`, and `StringComparer.Ordinal.GetHashCode` for the name, with 0 for `null`). Add `ToString`: the type name, `.class`, `#name`, and each state in the order `:hover:active:focus:disabled:checked`. An id outside its table gives `<type N>` or `.<class N>`. An empty compound gives `""`. The remarks state that `ToString` allocates and that the equality allocates nothing.
- [ ] 2.2 Edit `src/BrutalUi.Core/Style/Selector.cs`: the same members over the subject, the ancestor, and the combinator. `ToString` gives `subject`, `ancestor > subject`, or `ancestor subject`, and `(empty)` for `default(Selector)`. Replace each remark that says the type has no equality members.
- [ ] 2.3 Write `tests/BrutalUi.Core.Tests/Style/SelectorEqualityTests.cs`: equal and not equal for each part and for each combinator, two names in two string instances, the hash codes of equal values, `==` and `!=`, `ToString` of each form of the specificity table of section 7, the fallback text of an id outside the table, and `EqualsAllocatesNothing` (two selectors with names, 1000 calls after `AllocationGate.ReleaseAllocationContext()`, 0 bytes). `StyleSheetTests` passes with no change.

Group B (the selectors from text, 3 files):
- [ ] 2.4 Write `src/BrutalUi.Markup/Style/SelectorReader.cs` (internal) per "The Grammar", "The selector map", and "The selector rejects". It reads a selector list from the token list and gives the selectors, or one error. It checks each case before it calls a factory.
- [ ] 2.5 Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: read each rule with `SelectorReader`. A selector list gives one rule for each selector, in the order of the source. A rule with no declaration gives no rule.
- [ ] 2.6 Write `tests/BrutalUi.Markup.Tests/Style/SelectorFormTests.cs`:
  - `AHoverOnAPanelRestylesItsRowAndItsCellFromText` (the demo): `.<p>panel:hover > .<p>row { background-color: #3060c0; }` and `.<p>panel:hover .<p>cell { background-color: #ffcc00; }` on a tree of root, panel, row, and cell. Print after `Update`, then after `panel.States = PseudoStates.Hover` and `Update`. Assert both lines.
  - `EachSelectorFormGivesTheFactorySelector` (a theory): `Label`, `.a`, `#title`, each state alone, `Label.a#title:hover:checked`, `.a > .b`, `.a>.b`, `.a .b`, `.a  /* c */ .b`, `Label:hover > #title`, and `:focus .b`. Each row compares the parsed selector with the selector of the factories through `Assert.Equal`.
  - `ASelectorListGivesOneRuleForEachSelector`: `.a, .b > .c, #d { ... }` gives 3 rules with equal values, in the order of the source within equal specificity.
  - `TheTextFormRoundTrips`: for each row of the first theory, `Parse(selector.ToString())` gives an equal selector.
  - `EachRejectedFormGivesAPositionedError` (a theory): one row for each line of "The selector rejects", with the kind, the line, and the column.
  - `ATypeNameIsCaseSensitive`: `label` does not match a `Label`. `AnUnknownTypeMatchesNothingAndIsNoError`.
  - `ANameKeepsItsCase`, and `TheSpecificityComesFromTheSelector`: the rules hold the numbers of the tier table.
- [ ] 2.7 Run the build, the tests, and the demo. Run `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` and expect no output. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 50 (7 tasks, about seven steps for one task)

**Files (6):** Group A: `CompoundSelector.cs`, `Selector.cs`, `SelectorEqualityTests.cs`. Group B: `SelectorReader.cs`, `StyleSheetParser.cs`, `SelectorFormTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] Each test of `BrutalUi.Core.Tests` passes with no change of an assert.
- [ ] `EqualsAllocatesNothing`, `TheStylePassAllocatesNothingAfterWarmUp`, and `ThousandElementAllocationTests` pass with 0 bytes.
- [ ] `git diff -U0 <base commit> -- src/BrutalUi.Core/Style/Selector.cs src/BrutalUi.Core/Style/CompoundSelector.cs | grep -E '^-[^-]' | grep -vE '^-\s*///' | grep -E 'Matches|Specificity|ThrowIfWrong'` prints nothing: no code line of the match, the tiers, or the checks goes.

---

### Phase 3: A Text Sheet Lays Out a Panel
**Goal:** Each of the 32 properties and each value form: lengths, numbers, keywords, edge sets and corner sets of one to four values, the font map, and the font size.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetLaysOutAPanel' --show-live-output on` and reads `panel 20,10,200,40`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tier note:** The plan gives the full property table, the value forms, and the edge order. The work is table work in the parser. The one trap is the order of the `LengthEdges` and `Corners` constructors, and `EdgeAndCornerTests` checks each edge of each count.

**Tasks:**
- [ ] 3.1 Write `src/BrutalUi.Markup/Style/PropertyTable.cs` (internal static): one entry for each of the 32 names of "The Properties and the Value Forms", with its `StyleProperty` member and its value form. Add an explicit keyword table for each of the 7 enums. The table is built one time, at the first use, with `StringComparer.Ordinal`.
- [ ] 3.2 Edit `src/BrutalUi.Markup/Style/StyleValues.cs`: the length, the number (invariant culture), the keyword, the edge set and the corner set in the CSS order of the table, the `px` value of `font-size`, and the font. `font: default;` gives `new FontHandle(0)` when the map does not hold `default`. A key of `CreateInfo.Fonts` gives its handle. Another name gives `UnknownFont`. A wrong form or a wrong count gives `InvalidValue` at the first token at fault.
- [ ] 3.3 Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: `CreateInfo.Fonts`. The declaration loop finds the name in `PropertyTable`, reads the value with `StyleValues`, and writes it through the `StyleProperties` setter. An unknown name gives `UnknownProperty`. A later declaration of the same property wins.
- [ ] 3.4 Write `tests/BrutalUi.Markup.Tests/Style/PropertyFormTests.cs`:
  - `ATextSheetLaysOutAPanel` (the demo): a viewport of 400 by 300. `.<p>panel { width: 50%; height: 40px; margin: 10px 20px; }` on a child of a root with no style. Print `panel <x>,<y>,<width>,<height>` from `Layout.Rect` with the invariant culture. Assert the line.
  - `EachPropertyNameSetsItsOwnMember` (a theory with 32 rows): each row sets one name to a value that is not the default. The rule mask holds the bit of that member only, and the getter gives the value.
  - `EachKeywordGivesItsMember` (a theory with 28 rows: each keyword of each enum table).
  - `EachLengthFormGivesItsLength` (a theory): `12px`, `-4px`, `.5px`, `50%`, `1.25%`, `auto`, and `0`.
  - `ABareNumberForALengthIsAnError`: `width: 12;` gives `InvalidValue` at the `12`.
  - `NumbersReadInTheInvariantCulture`: with `CultureInfo.CurrentCulture` set to `de-DE` in the test (restored in `finally`), `opacity: 0.5;` gives 0.5.
  - `AFontNameReadsTheFontMap`, `DefaultGivesTheFontIdZero`, and `AnUnknownFontIsAnError`.
  - `ALaterDeclarationWins`, and `AColorRuleLetsTheFontInherit`: a parent rule sets `font-size: 24px`, a child rule sets only `color`, and the child gets size 24.
- [ ] 3.5 Write `tests/BrutalUi.Markup.Tests/Style/EdgeAndCornerTests.cs`: `EachEdgeCountFollowsTheCssOrder` (a theory: 1 to 4 values for `margin`, `padding`, and `border-width`, with four different values, and an assert of each of the four edges), `EachCornerCountFollowsTheCssOrder` (the same for `border-radius`), `AFifthValueIsAnError` (at the fifth value), and `AnEdgeSetWithMixedUnits` (`margin: 4px 10% auto 0`).
- [ ] 3.6 Run the build, the tests, and the demo. Record the printed line and the test total in `context.md`.

**Estimated Steps:** about 45 (6 tasks, about seven steps for one task)

**Files (5):** `PropertyTable.cs`, `StyleValues.cs`, `StyleSheetParser.cs`, `PropertyFormTests.cs`, `EdgeAndCornerTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the line, and the test asserts it.
- [ ] `EachPropertyNameSetsItsOwnMember` has 32 rows, and each `StyleProperty` member has one row.
- [ ] `grep -nE 'Enum\.(Parse|TryParse|GetNames|GetValues)|System\.Reflection|typeof\(' src/BrutalUi.Markup/Style/*.cs` prints nothing.

---

### Phase 4: A Theme Variable Colors a Button
**Goal:** Theme variables in `:root` blocks, resolved at parse time for each sheet (A9), with the variable errors.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AThemeVariableColorsTheButton' --show-live-output on` and reads `button #3060C0FF`, then `error 2:25 UnknownVariable` for a second sheet that reads `var(--missing)`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tier note:** "Theme Variables" gives the two passes, the token-range rule, and each error. The work stays inside the parser, and the value reader of phase 3 reads the tokens with no change.

**Tasks:**
- [ ] 4.1 Write `src/BrutalUi.Markup/Style/ThemeVariables.cs` (internal): pass 1 per "Theme Variables". It finds each `:root` block at the top level, stores each `--name` with the token range of its value, and gives `InvalidVariable` for a declaration that is not a variable, for an empty value, and for a `var(` inside a value.
- [ ] 4.2 Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: run pass 1 before the rule loop. Skip each `:root` block in pass 2. Before the value read, put the tokens of each `var(--name)` in place. Give `UnknownVariable` at the `--name` token, and `InvalidVariable` for a `--name` declaration outside `:root` and for a fallback. A value error inside a variable value points at the `var` token, and its message names the variable.
- [ ] 4.3 Write `tests/BrutalUi.Markup.Tests/Style/ThemeVariableTests.cs`:
  - `AThemeVariableColorsTheButton` (the demo): sheet 1 holds `:root { --accent: #3060c0; }` and `.<p>button { background-color: var(--accent); }`. Print `button <BackgroundColor>` after `Update`. Sheet 2 is `.<p>button {` + LF + `  background-color: var(--missing);` + LF + `}`. Print `error <line>:<column> <kind>` of its one error. Assert both lines.
  - `AVariableBeforeItsDefinitionResolves`, and `ALaterDefinitionWins`.
  - `AVariableHoldsMoreThanOneValue` (`--pad: 4px 8px;`), and `AVariableMixesWithOtherValues` (`padding: var(--gap) 8px;`).
  - `AVariableIsLocalToItsSheet`: a second `Parse` gives `UnknownVariable` for a name of the first text.
  - `VariableNamesAreCaseSensitive`.
  - `EachVariableErrorHasItsPosition` (a theory): a variable outside `:root`, a property in `:root`, an empty value, `var()` in a variable value, a fallback, and `:root .a`.
  - `AValueErrorFromAVariablePointsAtTheUse`: `--w: 12;` and `width: var(--w);` give `InvalidValue` at the `var` token.
- [ ] 4.4 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 30 (4 tasks, about seven steps for one task)

**Files (3):** `ThemeVariables.cs`, `StyleSheetParser.cs`, `ThemeVariableTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] Each test of phases 1 to 3 passes with no change.

---

### Phase 5: A Sheet with Three Mistakes Reports Each One
**Goal:** The full error report: the recovery, each error kind at its position, the range checks of the values, the unsupported CSS forms, the message text, and the proof that no input makes `Parse` throw.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ASheetWithThreeMistakesReportsEachOne' --show-live-output on` and reads `error 2:3 UnknownProperty`, `error 3:10 InvalidValue`, `error 4:1 UnexpectedEnd`, and `sheet null`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** The recovery changes the control flow of each read in the parser. A skip that stops at the wrong brace hides an error or reports a false one. A read past the end of the token list throws. The seeded random test is the guard, and a failure there needs a fix in `src/`, not in the test.

**Tasks:**
- [ ] 5.1 Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: the recovery of "The Error Report" in place of the stop at the first error. Add `UnexpectedEnd`, the at-rule check, the `!important` check, and the check of a function other than `var`. Each read checks the end of the token list.
- [ ] 5.2 Edit `src/BrutalUi.Markup/Style/StyleValues.cs`: the range checks. A negative value for `padding`, `border-width`, `border-radius`, `flex-grow`, `flex-shrink`, and `font-size`. `opacity` outside 0 to 1. `auto` for `padding`, `border-width`, `border-radius`, and `font-size`. `%` for `border-width`, `border-radius`, and `font-size`. A number that is not finite. An unknown unit (`12em`). Each check gives `InvalidValue` at the token at fault.
- [ ] 5.3 Edit `src/BrutalUi.Markup/Style/StyleParseError.cs` and each place that makes an error: the message rule of "The Error Report". Each message quotes the source text and names the fix.
- [ ] 5.4 Write `tests/BrutalUi.Markup.Tests/Style/ErrorReportTests.cs`:
  - `ASheetWithThreeMistakesReportsEachOne` (the demo): `.<p>panel {` + LF + `  colour: #fff;` + LF + `  width: 12em;` + LF. Print `error <line>:<column> <kind>` for each error, then `sheet null`. Assert the four lines.
  - `EachErrorKindHasItsPosition` (a theory with one row or more for each of the 13 kinds).
  - `EachRangeCheckGivesAnError` (a theory with one row for each check of task 5.2).
  - `TheParserFindsAnErrorAfterABadSelector`, and `TheParserFindsAnErrorAfterABadDeclaration`.
  - `TheErrorTextHasTheSourceAndThePosition`: `ToString` gives `hud.css(2,3): UnknownProperty: ` and then the message.
  - `EachMessageQuotesTheSourceText`: for each row of the kind theory, the message holds the text of the token at fault.
- [ ] 5.5 Write `tests/BrutalUi.Markup.Tests/Style/ParserRobustnessTests.cs`:
  - `NoRandomTextMakesTheParserThrow`: 2000 texts from `new Random(1234)`: random bytes, and random mixes of the tokens of the format. `Parse` gives a result for each text, and a result with no sheet has one error or more.
  - `EachCutOfAValidSheetGivesAnErrorOrASheet`: a valid sheet of each form, cut at each byte. No cut throws. A cut inside a rule gives an error.
- [ ] 5.6 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 45 (6 tasks, about seven steps for one task)

**Files (5):** `StyleSheetParser.cs`, `StyleValues.cs`, `StyleParseError.cs`, `ErrorReportTests.cs`, `ParserRobustnessTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the four lines, and the test asserts them.
- [ ] `EachErrorKindHasItsPosition` holds a row for each member of `StyleErrorKind`.
- [ ] `NoRandomTextMakesTheParserThrow` passes in 3 runs.

---

### Phase 6: A Transition Declaration Lives in the Rule
**Goal:** The Core model of a transition (`TransitionEasing`, `StyleTransition`, `StyleTransitions`), the new `StyleRule` constructor and `Add` overload, and the `transition` declaration of the format (Q3).
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATransitionDeclarationIsStoredInTheRule' --show-live-output on` and reads `BackgroundColor 0.2s EaseOut`, then `Opacity none`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Tier note:** Group A fixes a public Core model that the transitions feature reads, and that feature can run in parallel with this one. The new set grows `StyleRule`, which each cascade copies. Read the transitions folder first (task 6.1). Group B is table work.

**Tasks:**

Group A (the Core model, 4 files):
- [ ] 6.1 Read `docs/features/style-system/transitions/` and search `src/BrutalUi.Core` for an easing or transition type. When the transitions feature added one already, use it, record the change in `context.md`, and write no second type. Else write `src/BrutalUi.Core/Style/StyleTransitions.cs` per "The Public API". The set holds one slot for each member of Q3 and a mask. `Set` and `Get` throw `ArgumentOutOfRangeException` for each other member, with a message that names the five members. The remarks state Q3 and that the model keeps each duration (the transitions feature applies its rule for 0, `NaN`, and infinity).
- [ ] 6.2 Edit `src/BrutalUi.Core/Style/StyleRule.cs`: the `Transitions` field and the new constructor. The old constructor gives an empty set. Replace the remark "The style-parser feature adds the transition member to this struct later".
- [ ] 6.3 Edit `src/BrutalUi.Core/Style/StyleSheet.cs`: the new `Add` overload. The old `Add` calls it with an empty set. The sort and the copy do not change.
- [ ] 6.4 Write `tests/BrutalUi.Core.Tests/Style/StyleTransitionsTests.cs`: `CanTransition` for each of the 32 members (a theory), set, get, and clear for each of the five, the throw for each other member, `IsEmpty`, the copy into the rule, an empty set from the old constructor and from the old `Add`, the sort keeps the set with its rule, and `ARuleWithATransitionStylesTheSame`: the style pass gives the same `ComputedStyle` for a rule with a transition and with no transition.

Group B (the declaration, 4 files):
- [ ] 6.5 Edit `src/BrutalUi.Markup/Style/PropertyTable.cs`: the `transition` entry, the five member names (`background-color`, `border-color`, `color`, `border-radius`, `opacity`), and the four easing keywords (`linear`, `ease-in`, `ease-out`, `ease-in-out`).
- [ ] 6.6 Edit `src/BrutalUi.Markup/Style/StyleValues.cs`: `<property> <duration> <easing>`, and more of them in a comma list. A duration is `<number>s` or `<number>ms` (divided by 1000), 0 or more. All three parts are required. A member outside the five gives `InvalidValue` with a message that names the five. A wrong duration or a wrong easing gives `InvalidValue` at its token.
- [ ] 6.7 Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: each rule collects a `StyleTransitions` set and adds it through the new `Add` overload. A later `transition` declaration for the same member wins. A rule with only a transition declaration gives a rule.
- [ ] 6.8 Write `tests/BrutalUi.Markup.Tests/Style/TransitionDeclarationTests.cs`:
  - `ATransitionDeclarationIsStoredInTheRule` (the demo): `.<p>button { background-color: #1e222c; transition: background-color 0.2s ease-out; }`. Print `BackgroundColor <duration>s <easing>` and `Opacity none` from the rule. Assert both lines.
  - `EachMemberCanTransition` (5 rows), `EachEasingKeywordGivesItsMember` (4 rows), and `MillisecondsGiveSeconds` (`200ms` gives 0.2).
  - `ACommaListSetsEachMember`, `ALaterDeclarationWinsForTheSameMember`, and `AVariableCanHoldTheDuration`.
  - `EachWrongTransitionGivesAPositionedError` (a theory): `width 1s linear`, `opacity -1s linear`, `opacity 1px linear`, `opacity 1s ease`, `opacity 1s`, and `opacity`.
- [ ] 6.9 Run the build, the tests, and the demo. Run the facts line of task 1.1 and record `bytes per element`. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 55 (9 tasks, about six steps for one task)

**Files (8):** Group A: `StyleTransitions.cs`, `StyleRule.cs`, `StyleSheet.cs`, `StyleTransitionsTests.cs`. Group B: `PropertyTable.cs`, `StyleValues.cs`, `StyleSheetParser.cs`, `TransitionDeclarationTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] The code of `StylePass.cs` does not change (I4).
- [ ] `TheStylePassAllocatesNothingAfterWarmUp` and `ThousandElementAllocationTests` pass with 0 bytes.

---

### Phase 7: HelloPanel Draws from a Stylesheet
**Goal:** The sample loads `HelloScreen.css` and sets classes in place of inline values, with no change of its smoke output. A gate proves that a parsed sheet allocates nothing in `Update`. The format goes into the design doc, the changelog, and the hand-offs.
**Demo:** the developer runs `scripts/test-linux.sh; echo "exit=$?"` and reads `test-linux: PASS build, test, gpu, smoke (120 frames) in <S> s` and `exit=0`. Then `grep -c 'draw commands 2, vertices 128' artifacts/test-linux/smoke.log` prints `1`.
**Needs:** Phase 6
**Agent tier:** ADVANCED (opus)

**Tier note:** The smoke run and the allocation gate are load-bearing. A value of the sheet that differs from the old inline value changes the vertex count. When a gate measures bytes above 0, find the cause in `src/` before a change of the test. Group B is plain documentation work.

**Tasks:**

Group A (the sample and the gate, 6 files):
- [ ] 7.1 Write `samples/HelloPanel/HelloScreen.css`. A `:root` block holds `--panel-fill: #1e222c;`, `--panel-border: #608cdc;`, and `--text: #ffffff;`. `.hello-root` holds `align-items: flex-start; padding: 24px;`. `.hello-panel` holds `flex-direction: column; align-items: flex-start; padding: 16px; border-width: 2px; border-radius: 8px;` and the two colors through `var()`. `.hello-panel > Label` holds `color: var(--text); font-size: 26px;`. `.hello-panel > Image` holds `margin: 12px 0 0;`. Each value equals the old constant of `HelloScreen.cs`. A comment at the top names the file and the format section of the design doc.
- [ ] 7.2 Edit `samples/HelloPanel/HelloPanel.csproj`: a `ProjectReference` to `../../src/BrutalUi.Markup/BrutalUi.Markup.csproj`, and `<None Include="HelloScreen.css" CopyToOutputDirectory="PreserveNewest" />`.
- [ ] 7.3 Edit `samples/HelloPanel/HelloScreen.cs`: `Build` sets the classes `hello-root` and `hello-panel` (two `static readonly ClassId` fields) and no inline value. Remove the constants that moved to the sheet. Keep the checker constants and the image tint (a create-info value). Add `LoadSheet()`: it reads `Path.Combine(AppContext.BaseDirectory, "HelloScreen.css")` and returns `new StyleSheetParser().Parse(bytes, "HelloScreen.css").GetSheetOrThrow()`. Update the remarks: the sizes and the colors live in `HelloScreen.css`.
- [ ] 7.4 Edit `samples/HelloPanel/Program.cs`: call `HelloScreen.LoadSheet()` at the start of `Run`, before the window opens, so a bad sheet fails with no GPU. Give the sheet to `RunImGui` and call `ui.AddSheet(sheet)` before `ui.AddRoot`. A parse error goes to the existing `catch`, which writes the error lines to standard error and returns exit code 1. The 3 output lines and the exit codes do not change. Update the remarks.
- [ ] 7.5 Edit `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`: `<Compile Include="../BrutalUi.Core.Tests/Shared/AllocationGate.cs" LinkBase="Shared" />` and `<None Include="../../samples/HelloPanel/HelloScreen.css" Link="HelloScreen.css" CopyToOutputDirectory="PreserveNewest" />`.
- [ ] 7.6 Write `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`:
  - `AParsedSheetAllocatesNothingInUpdate`: one parsed text with a variable, each selector form, each value family, and a transition list. A tree of 50 elements with the classes and the names of the rules. Each step toggles `Hover` on a panel with children, toggles one class, and sets a name to one of two `const string` values, then calls `Update`. Run 16 warm-up steps, call `AllocationGate.ReleaseAllocationContext()`, measure 100 steps, and assert 0 bytes. Assert also that the last step gives the expected values, so the steps reach the rules.
  - `TheHelloPanelSheetGivesTheOldValues`: parse the linked `HelloScreen.css`. Assert success and each value of task 7.1 against the old constants (copied into the test, with a comment that names the source).
- [ ] 7.7 Prove that the gate blocks. Copy `src/BrutalUi.Core/Style/Selector.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Selector.Matches(Element, ElementTypeId)`. Run `dotnet test --solution brutal-ui.sln > <scratch>/plant.log 2>&1; echo "exit=$?"` and expect a value that is not 0, with `AParsedSheetAllocatesNothingInUpdate` in the failed list. Restore the file from the copy. Run `diff` on the two files and expect no output. Record both exit codes and the failed tests in `context.md`.
- [ ] 7.8 Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Read the 3 lines of `artifacts/test-linux/smoke.log`: `120 frames rendered`, `draw commands 2, vertices 128`, and `Update and Draw 0`. Then prove the error path: write `.hello-panel { colour: #fff; }` into `samples/HelloPanel/bin/Debug/net10.0/HelloScreen.css`, run `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 1; echo "exit=$?"`, and expect `HelloScreen.css(1,16): UnknownProperty:` on standard error and `exit=1`. Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30` to restore the copy. Record each result in `context.md`.

Group B (the record, 4 files):
- [ ] 7.9 Edit `docs/technical-design.md`. Section 11, "Stylesheet format": the grammar, the property table, the value forms, the edge order, the text members that inherit, theme variables, the transition declaration, the error report, the parser API, and the walk bound of `.a .b` (the parent and the grandparent only, which differs from CSS). Section 7: `StyleRule.Transitions`, the model of `StyleTransitions`, and the equality members of the selector types (in place of the design decision of DS1.7). Section 14: the test classes of `BrutalUi.Markup.Tests` and the new gate with the plant result of task 7.7. Update the cost line of section 7 with the facts of task 6.9 when the value changed.
- [ ] 7.10 Edit `CHANGELOG.md`: one line under `### Added` (`StyleSheetParser`, `StyleParseResult`, `StyleParseError`, `StyleErrorKind`, the text format with theme variables, `TransitionEasing`, `StyleTransition`, `StyleTransitions`, the new `StyleRule` constructor and `Add` overload, and the equality members and the text form of `CompoundSelector` and `Selector`) and one line under `### Changed` (the HelloPanel sample loads `HelloScreen.css`). Name the feature `style-system/style-parser`.
- [ ] 7.11 Edit `docs/features/style-system/human-testing.md`: add a `## style-parser` section with the 7 phase demos, the error path of task 7.8, and the Windows run of `.\scripts\test-windows.ps1`.
- [ ] 7.12 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.

**Estimated Steps:** about 70 (12 tasks, about six steps for one task)

**Files (10):** Group A: `HelloScreen.css`, `HelloPanel.csproj`, `HelloScreen.cs`, `Program.cs`, `BrutalUi.Markup.Tests.csproj`, `ParsedSheetAllocationTests.cs` (and a temporary edit of `Selector.cs`, restored). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/style-system/human-testing.md`, and `context.md`.

**Acceptance Criteria:**
- [ ] The smoke log holds the 3 lines with `draw commands 2, vertices 128` and `Update and Draw 0`, and `test-linux.sh` exits with 0.
- [ ] A bad sheet gives exit code 1 and the error line on standard error.
- [ ] The planted allocation fails `AParsedSheetAllocatesNothingInUpdate`, and the restored file is the same as the copy.
- [ ] `grep -n 'Inline\.' samples/HelloPanel/HelloScreen.cs` prints nothing.
- [ ] The lint gives no hit above 2.5 for each changed doc.

**Total estimate:** about 355 steps over 7 phases and 54 tasks.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q3 Transitions: colors, corner radius, and opacity only. A tween marks `Paint` only.
- A3 A type selector matches a type id that each element class declares. The match path uses no reflection.
- A9 Theme variables resolve at parse time, for each sheet. The selector depth is a constant of 2.

The epic interview confirmed these items (`epic-overview.md`). The decisions below cite them.

### Decision 1: A hand-written UTF-8 tokenizer and a recursive descent parser (the chosen approach)
**Choice:** `BrutalUi.Markup` holds its own tokenizer over `ReadOnlySpan<byte>` and a recursive descent parser over the token list. The parser builds the sheet through `StyleSheet.Builder` and the selector factories only.
**Rationale:** The format is a small subset. A hand-written reader gives exact positions, the reject list with a position for each form, the recovery of CSS, and the token-level variables of A9. It reads UTF-8 as the project rules ask, needs no package, and adds no pin. The parser uses only the public API of Core (A1).
**Alternatives:** (a) A CSS library package (for example ExCSS or AngleSharp.Css): a new pin and a new dependency of a published package, a full CSS model with more than the subset, and a second map from its model to the model of Core. Its error positions and its silent drops of unknown forms do not match the error rule. (b) A split on `{`, `}`, and `;` with string methods or regular expressions: little code, but no correct position after a comment, no clear place for the whitespace combinator, and no recovery.

### Decision 2: The tokenizer gives a full list first, and the parser reads it in two passes
**Choice:** One tokenizer run gives a `List<StyleToken>`. Pass 1 reads the `:root` blocks. Pass 2 reads the rules.
**Rationale:** A9 asks for variables at parse time for each sheet. With a list, a variable can come after the rule that reads it, as in CSS, and a substitution is a token range with its own positions. The list also gives the look-ahead that the selector reader needs for whitespace before `>`, `,`, and `{`. The parser runs at load time, so the list costs no frame time.
**Alternatives:** (a) A pull tokenizer with one pass: no list, but a variable must come before its use, and the look-ahead needs a buffer. (b) A text substitution of `var()` before the tokenizer: the positions of each later token move, so the error report gives wrong columns.

### Decision 3: The parser reports each error through a result, not an exception
**Choice:** `Parse` returns a `StyleParseResult` with `Sheet` or `Errors`. Each error holds a `StyleErrorKind`, a line, a column, a message, and the source name. The parser recovers at `;` and `}`. `GetSheetOrThrow` gives the throw for code that wants it.
**Rationale:** A markup author fixes all mistakes after one run. A result keeps the error path free of exceptions, so a fuzz test can prove that no input throws. The kind gives the tests a stable contract: they assert the kind, the line, and the column, and the message text can improve with no test change.
**Alternatives:** (a) A `StyleParseException` at the first error: less code, but one error for each run. (b) `bool TryParse(..., out StyleSheet?, out StyleParseError)` with one error: simple, but one error for each run, and no place for more errors later.

### Decision 4: The parser is an instance with a create-info, and it reads UTF-8
**Choice:** `new StyleSheetParser(new StyleSheetParser.CreateInfo { Fonts = map })`, then `Parse(ReadOnlySpan<byte>, sourceName)`. A `Parse(string, sourceName)` overload encodes the text first.
**Rationale:** The project uses create-info structs (`UiContext.CreateInfo`, `Label.CreateInfo`). One parser with one font map reads each sheet of a game. The UTF-8 input follows the project rules and reads a file with no decode. The string overload keeps the tests short, because the tests make names with a prefix.
**Alternatives:** A static `StyleSheetParser.Parse` with an optional map parameter: a shorter call, but a new parameter for each later option, and no place for the option pattern of the project.

### Decision 5: The property names and the value forms follow CSS, through explicit tables
**Choice:** The 32 names are the CSS names (`border-radius` for `CornerRadius`). The edge sets and the corner sets use the CSS order. The keywords are the CSS keywords (`nowrap`, not `no-wrap`). Each map is an explicit table.
**Rationale:** The design doc names a CSS subset, and its example uses `font-size` and `background-color`. A CSS author needs no new words. Explicit tables need no reflection and no `Enum.Parse`, and a test row exists for each entry. The order of the Core constructors differs from the CSS order, so one helper holds the map (Risk 1).
**Alternatives:** (a) The C# member names (`CornerRadius: 8px`): a direct map, but no CSS editor support and no CSS habit. (b) `Enum.TryParse` with a kebab-case convert: less code, but a culture and case risk, and a new enum member enters the format with no review.

### Decision 6: A transition lives in a sparse set of five slots in Core (question 3)
**Choice:** `StyleTransitions` holds one `StyleTransition` (a duration and an easing) for each member of Q3 and a mask. `StyleRule` gets it through a new constructor. `StyleSheet.Builder` gets a new `Add` overload. One declaration can hold a comma list. A later declaration for the same member wins.
**Rationale:** Q3 limits the transitions to five members, so five fixed slots hold each case with no heap array. The shape follows `StyleProperties`: a mask, a setter, and a clear. The transitions feature can merge the slots in the order of the cascade, as the pass merges the values. The old constructor and the old `Add` stay, as the stylesheet-model hand-off asks.
**Alternatives:** (a) One transition for each rule: the smallest model, but a button that fades its fill and its border needs two rules with the same selector. (b) A `StyleTransition[]` for each rule: any count, but a heap array for each rule and a pointer read for each match, for a count that is at most five.

### Decision 7: The selector types get equality members and a CSS text form (CR-4, question 2)
**Choice:** `CompoundSelector` and `Selector` implement `IEquatable<T>`, `==`, `!=`, and `GetHashCode`, and `ToString` gives the CSS form of this format.
**Rationale:** The parser tests compare each parsed selector with the selector of the factories. With the default members, a failed assert shows `BrutalUi.Core.Selector` for both values, and each compare allocates about 880 bytes through reflection. The new members allocate nothing, and the text form is the input form of the parser, so a round trip test proves both.
**Alternatives:** (a) A test helper that writes the CSS text of a selector, and tests that compare the text: no new public API, but the default `Equals` stays, and each test project needs its own helper. (b) No change: no work, but each failed parse test shows two equal type names.

### Decision 8: A font is a name of a font map in the create-info (question 4)
**Choice:** `font: <name>;` reads `CreateInfo.Fonts`. `font: default;` gives the id 0 with no map. An unknown name gives `UnknownFont`.
**Rationale:** A `FontHandle` id has a meaning for one renderer only, and its remarks say "Do not store them between runs". A sheet file lives between runs, so it names a font, and the game binds the name to a handle at load time.
**Alternatives:** (a) `font: 2;` for the id 2: no map, but the file stores a runtime id. (b) No `font` property in the format: no work, but the requirement asks for each property of `StyleProperties`.

### Decision 9: The sample reads its sheet from a file next to the program
**Choice:** `HelloScreen.css` copies to the output folder. `Run` loads it before the window opens. A parse error gives exit code 1 through the existing `catch`.
**Rationale:** A file shows the real load path of a game and gives the error text a file name. The load before the window lets a bad sheet fail with no GPU. Exit code 1 is the existing code for a runtime failure, so the contract of the smoke run does not change.
**Alternatives:** (a) A raw string constant in `HelloScreen.cs`: no file copy, but no file path in the error and no example of a load. (b) An embedded resource: no loose file, but a resource name in place of a file path, and more code in the sample.

### Decision 10: The parsed-sheet gate lives in `BrutalUi.Markup.Tests`
**Choice:** `ParsedSheetAllocationTests` links `AllocationGate.cs` from the shared folder and measures `Update` with a parsed sheet.
**Rationale:** `BrutalUi.Core.Tests` cannot reference Markup. The gate proves the requirement "the sheet allocates nothing in the frame loop" for a sheet that the parser made, with each selector form and a transition set. The ImGui tests and the benchmarks link the shared files in the same way.
**Alternatives:** Parse the text of the 1000-element tree in Core: not possible, because Core does not reference Markup.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The CSS order of the edges differs from the order of `new LengthEdges(left, top, right, bottom)` and `new Corners(...)`. | A margin or a radius on the wrong edge, with no error. | One helper in `StyleValues` holds the map. `EdgeAndCornerTests` checks each edge of each count with four different values. The sample test checks `margin: 12px 0 0`. |
| 2 | A number reads with the culture of the machine. | `0.5` fails or gives 5 on a German machine. | The invariant culture in each read. `NumbersReadInTheInvariantCulture` runs under `de-DE`. |
| 3 | A typo in a type name (`Lable`) interns a new id (A3). | The rule matches nothing, and no error shows it. | Section 11 states the rule. Gap 4 holds a later check of known types. The tests prove that the case matters. |
| 4 | The transitions feature runs in parallel and adds its own easing or transition type. | Two types for one idea, or a merge conflict in `StyleRule`. | Task 6.1 searches first and adopts an existing type. The hand-off names the types of this feature. |
| 5 | A read past the end or a factory throw leaves `Parse`. | A game crashes at load on a bad file, with no position. | The selector checks run before each factory. Each read checks the end. `ParserRobustnessTests` runs 2000 seeded texts and each cut of a valid sheet. |
| 6 | A value of `HelloScreen.css` differs from the old constant. | The vertex count of the smoke run changes. | `TheHelloPanelSheetGivesTheOldValues` checks each value. Task 7.8 reads the smoke log. |
| 7 | The sheet file is not in the output folder. | The smoke run fails at load with exit code 1. | `CopyToOutputDirectory`. `test-linux.sh` builds before the smoke step. Task 7.8 runs the full script. |
| 8 | `StyleRule` grows by the transition set (about 44 bytes). | Each cascade array is larger. No frame cost. | Task 6.9 records the facts line. The pass reads the rule through `ref readonly`, so the size adds no copy. |
| 9 | The column rule (code points) differs from the rule of an editor (UTF-16 units or bytes). | A column is off for a line with an emoji in a comment. | The format allows non-ASCII bytes in comments only. Section 11 states the rule. `AColumnCountsCodePoints` fixes it. |
| 10 | The new `Equals` gives another result than the default `Equals`. | `StyleSheetTests` or a later test gives a wrong result. | `SelectorEqualityTests` checks each part. Task 2.3 runs `StyleSheetTests` with no change. |

---

## Dependencies

**Internal (done):**
- stylesheet-model: `StyleProperties` with its setters, `StyleRule`, `StyleSheet.Builder`, `ElementType.Intern`, `ClassName.Intern`, `UiContext.AddSheet`, the style pass, and the text inheritance.
- selector-matching: `CompoundSelector`, `SelectorCombinator`, `Selector.For`, `Child`, `Descendant`, the byte tiers, the order of the factory checks, and the hand-off rules (the name without the `#`, the walk bound).
- core-tree and imgui-renderer: `Length`, `LengthEdges`, `Corners`, `Rgba8`, `FontHandle`, the layout enums, `Layout.Rect`, and `AllocationGate`.

**External (pinned, no change):**
- `Ahwoo.BRUTAL.Numerics`, `Ahwoo.BRUTAL.Localization`, the BRUTAL packages of the sample, and `xunit.v3`. No new package and no native dependency.

**Downstream:**
- transitions reads `StyleRule.Transitions` and uses `TransitionEasing`.
- The markup epic of Phase 3 loads a sheet next to an XML tree, with the same error form.

---

## Definition of Done

### Functional
- [ ] F1. Each phase demo prints its lines, and its test asserts them.
- [ ] F2. Each of the 32 property names sets its own member of `StyleProperties` and no other bit. `transition` sets `StyleRule.Transitions`.
- [ ] F3. The value forms work: `px`, `%`, `auto`, a bare `0`, a number, `#rgb`, `#rrggbb`, and `#rrggbbaa` in any case, each keyword of the 7 enum tables, and one to four values for `margin`, `padding`, `border-width`, and `border-radius` in the CSS order.
- [ ] F4. Each selector form of selector-matching from text equals the selector of the factories (`Assert.Equal`). Each form of "The selector rejects" gives its kind at its position.
- [ ] F5. `var(--name)` reads a variable of a `:root` block of the same text, also before the definition. An unknown name gives `UnknownVariable` at the `--name` token. A second text does not see the variables of the first.
- [ ] F6. A text with an error gives `Sheet == null`. Each error holds the kind, the line and the column from 1 (code points, CRLF as one break), the message, and the source name. One parse reports each error of the demo of phase 5. No input of `ParserRobustnessTests` makes `Parse` throw.
- [ ] F7. `transition: <property> <duration> <easing>` with a comma list stores one slot for each of the five members of Q3. The old `StyleRule` constructor and the old `Add` give an empty set.
- [ ] F8. `CompoundSelector` and `Selector` have `IEquatable<T>`, `==`, `!=`, `GetHashCode`, and a CSS `ToString`. `Parse(selector.ToString())` gives an equal selector. `Equals` allocates 0 bytes.
- [ ] F9. HelloPanel sets classes and no inline value, and it loads `HelloScreen.css`. The smoke run prints its 3 lines with `draw commands 2, vertices 128` and `Update and Draw 0`. A bad sheet gives exit code 1 and the error line.

### Quality
- [ ] Q1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30` prints `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` prints 0 failed. The total is 1238, minus 1 (the placeholder), plus the new tests.
- [ ] Q3. `AParsedSheetAllocatesNothingInUpdate`, `EqualsAllocatesNothing`, `TheStylePassAllocatesNothingAfterWarmUp`, and `ThousandElementAllocationTests` pass with 0 bytes. The plant of task 7.7 gives a non-zero exit code.
- [ ] Q4. Each public member of the new and the changed types has an XML doc comment (the build enforces it).
- [ ] Q5. `ste-lint.py --fail-over 2.5` exits with 0 for each changed doc.

### Integration
- [ ] I1. `git diff --quiet <base commit> -- Directory.Packages.props src/BrutalUi.ImGui; echo "exit=$?"` prints `exit=0`.
- [ ] I2. `grep -c 'ProjectReference' src/BrutalUi.Markup/BrutalUi.Markup.csproj` prints `1`, and `grep -c 'PackageReference' src/BrutalUi.Markup/BrutalUi.Markup.csproj` prints `0`.
- [ ] I3. `grep -rnE 'System\.Reflection|Enum\.(Parse|TryParse|GetNames|GetValues)|typeof\(|GetType\(\)' src/BrutalUi.Markup --include=*.cs` prints nothing. `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` prints nothing.
- [ ] I4. The code of `StylePass.cs` does not change: `git diff -U0 <base commit> -- src/BrutalUi.Core/Style/StylePass.cs | grep '^[-+]' | grep -v '^[-+][-+]' | grep -vE '^[-+]\s*///'` prints nothing.
- [ ] I5. `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs` prints nothing.
- [ ] I6. `scripts/test-linux.sh; echo "exit=$?"` prints the PASS line and `exit=0`.

### Verification Method
1. Run Q1 and Q2.
2. Run the demo command of each phase and compare the printed lines with the Demo line.
3. Run `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*ParserRobustnessTests'` and `--filter-class '*ParsedSheetAllocationTests'`, and expect `failed: 0`.
4. Run I1 to I6 with the base commit of task 1.1. Read `artifacts/test-linux/smoke.log` for the 3 lines.
5. Read `context.md` for the exit codes of tasks 7.7 and 7.8 and the facts lines of tasks 1.1 and 6.9.

---

## Testing Strategy

- **Tokenizer tests (`StyleTokenizerTests`):** each token kind, the line rule, the column rule, the BOM, the comment rule, and the ASCII rule. The tests read the internal tokenizer through `InternalsVisibleTo`.
- **Map tests (`TextSheetTests`, `SelectorFormTests`, `PropertyFormTests`, `EdgeAndCornerTests`, `TransitionDeclarationTests`):** a theory row for each property, each keyword, each selector form, and each edge count. A parsed selector compares with the selector of the factories. A parsed value compares with the value of the setter.
- **Cascade tests through the pass:** each demo parses a text, adds the sheet to a context, runs `Update`, and reads `Element.Style` or `Layout.Rect`. The paint values do not inherit, so a result comes from the match.
- **Error tests (`ErrorReportTests`, `ThemeVariableTests`):** each kind at its position, each range check, the recovery, and the text form. The tests assert the kind, the line, and the column.
- **Robustness (`ParserRobustnessTests`):** 2000 seeded random texts and each cut of a valid sheet. No throw.
- **Core tests (`SelectorEqualityTests`, `StyleTransitionsTests`):** the new members, with a 0-byte check of `Equals`. The existing style tests pass with no change.
- **Allocation gate (`ParsedSheetAllocationTests`):** a parsed sheet in `Update` with a state, a class, and a name change. The plant in `Selector.Matches` proves the gate.
- **Manual:** the smoke run of HelloPanel through `scripts/test-linux.sh`, and the error path of a bad sheet (task 7.8).

---

## Related Documentation

- `docs/features/style-system/style-parser/requirements.md`
- `docs/features/style-system/epic-overview.md` and `epic-requirements.md`
- `docs/features/style-system/stylesheet-model/context.md` (`## Hand-offs`, the style-parser part)
- `docs/features/style-system/selector-matching/implementation.md`, `context.md` (`## Hand-offs`, the style-parser part), and `code-review.md` (CR-4)
- `docs/features/style-system/transitions/requirements.md`
- `docs/technical-design.md` sections 2, 3, 7, 11, and 14
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

### Open Scope Questions

No interview ran for this draft. The plan uses the recommended option of each question.

**Question 1: Which demo does phase 1 use (slice rule 12)?**
- **Recommended:** `**Demo:** the developer runs dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetPaintsAPanel' --show-live-output on and reads panel #1E222CFF and plain #00000000`. Reason: the smallest path through the tokenizer, the parser, the builder, and the style pass, in the form of the first demo of stylesheet-model.
- Option B: `**Demo:** the developer runs the filter '*AnErrorGivesNoSheet' and reads error 1:6 UnknownProperty and sheet null`. Reason: it shows the error path first, but no cascade.
- Option C: `**Demo:** the developer runs scripts/test-linux.sh and the panel of HelloPanel takes its fill from a sheet`. Reason: a visible result on the GPU, but phase 1 then needs each property form of the sample and a GPU.

**Question 2: Which shape closes CR-4 (the selector types have no equality members and no text form)?**
- **Recommended:** `IEquatable<T>`, `==`, `!=`, `GetHashCode`, and a CSS `ToString` on `CompoundSelector` and `Selector` in Core. Reason: a failed parse test shows the CSS text of both values, the compare allocates nothing, and a round trip test proves the parser and the text form together.
- Option B: a test helper in `BrutalUi.Markup.Tests` that writes the CSS text of a selector, and tests that compare the text. Reason: no new public API in Core, but the default `Equals` stays (reflection, about 880 bytes for each call), and each test project needs its own helper.
- Option C: no change. Reason: no work, but a failed parse test shows `BrutalUi.Core.Selector` for both values.

**Question 3: How does a rule store its transitions?**
- **Recommended:** `StyleTransitions`, a sparse set of five slots in Core (one for each member of Q3) with a mask, and a comma list in one declaration. Reason: each case of Q3 fits with no heap array, and the shape follows `StyleProperties`, so the transitions feature can merge the slots in the order of the cascade.
- Option B: one transition for each rule (`property duration easing`), as the requirement text writes it. Reason: the smallest model, but a button that fades its fill and its border needs two rules with the same selector.
- Option C: a `StyleTransition[]` for each rule. Reason: any count, but a heap array for each rule, for a count that is at most five.

**Question 4: How does the format name a font?**
- **Recommended:** a name of a font map in `StyleSheetParser.CreateInfo.Fonts`, and `default` for the id 0. Reason: a `FontHandle` id has a meaning for one renderer and one run only, so the file names a font, and the game binds the name at load time.
- Option B: a whole number, `font: 2;`, for `new FontHandle(2)`. Reason: no map, but the file stores a runtime id against the rule of `FontHandle`.
- Option C: no `font` property in the format (the game sets the font in code). Reason: no work, but the requirement asks for each property of `StyleProperties`.

### Conflicts

1. **The transition form.** The requirements write one triple, `transition: <property> <duration> <easing>;`. The plan also accepts a comma list of triples (question 3). One triple stays valid.
2. **CSS and a later `transition` declaration.** In CSS, a later `transition` declaration replaces the full list. In this format, a later declaration replaces only the slots of its members, as each other property does. Section 11 states it.
3. **CSS and the descendant chain.** `.a .b` reads the parent and the grandparent only (question 2 of selector-matching). The format accepts the CSS text, and section 11 states the bound.
4. **CSS and the defaults.** `flex-direction` defaults to `column` (Yoga), not `row`. The format accepts no `#rgba` (four digits), no `transparent`, and no `none` for a maximum size. Section 11 states each one.
5. **The Confirmed Assumptions and the code.** No item conflicts with the code. Q3 names "colors", and the five members of the transitions requirements (`BackgroundColor`, `BorderColor`, `Color`, `CornerRadius`, `Opacity`) follow it.
6. **Section 13 of the design doc.** It says that `UiContext.CreateInfo` sets the default stylesheet. No such member exists, and this feature adds none (Gap 7).

### Gaps (not in this feature)

1. `.a.b`, `*`, and the other CSS selectors of "The selector rejects". Each gives a positioned error.
2. A variable that reads a variable, and a `var()` fallback. Each gives `InvalidVariable`.
3. `transition: none`, `transition: all`, a delay, and the CSS default easing `ease`.
4. A check of known type names, for example a list of type names in `CreateInfo`.
5. Longhands such as `margin-top`, `#rgba`, `transparent`, `rgb()`, `none` for a maximum size, and non-ASCII identifiers.
6. `@import`, `@media`, hot reload, runtime variables, and a cascade across sheets (out of scope).
7. A default stylesheet in `UiContext.CreateInfo` (section 13 of the design doc).

### Hand-offs (for `context.md` at the end of phase 7)

**transitions**
- Read `StyleRule.Transitions` (a `StyleTransitions` set). Use `TransitionEasing`, `StyleTransition`, and `StyleTransitions` of `src/BrutalUi.Core/Style/StyleTransitions.cs`. Do not add a second easing type.
- `StyleTransitions.CanTransition` gives the five members of Q3. A rule from the old constructor holds an empty set.
- The parser rejects a negative duration. The model keeps each float, so a sheet built in code can hold 0, a negative value, `NaN`, or infinity. Apply the rule of the transitions requirements to each one.
- A merge for each slot in the order of the cascade follows the model of `StyleProperties`: a later matched rule wins for each member. CSS takes the full list of the winning rule. Decide and document the rule.
- `StylePass.cs` does not change in this feature, so the start point of the stylesheet-model hand-off holds.

**markup epic (Phase 3)**
- Load a sheet with `StyleSheetParser` and `GetSheetOrThrow`, next to the XML tree. Use the form `<source>(<line>,<column>): <kind>: <message>` for the errors of the XML loader too.
- The tokenizer is internal to `BrutalUi.Markup`, so the XML loader can reuse the position rule.

**input and later features**
- A game binds its fonts by name through `CreateInfo.Fonts`. A font service can give the map.
