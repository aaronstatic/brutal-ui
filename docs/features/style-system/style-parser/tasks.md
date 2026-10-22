# style-system/style-parser - Task Checklist

**Last Updated:** 2026-10-22 00:55
**Progress:** 54/54 tasks complete (100%)
**Advanced phases:** Phases 1, 2, 5, 6, and 7 (ADVANCED, opus). Phases 3 and 4 are STANDARD (sonnet). Group B of phase 7 is plain documentation work.

Source paths: `Core/` means `src/BrutalUi.Core/`, and `Markup/` means `src/BrutalUi.Markup/`. Test paths: `tests/Core/` means `tests/BrutalUi.Core.Tests/`, and `tests/Markup/` means `tests/BrutalUi.Markup.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "The Public API", "The Tokenizer", "The Grammar", "The selector map", "The selector rejects", "The Properties and the Value Forms", "Theme Variables", and "The Error Report" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The class table, the type table, and the names are global, and xUnit runs classes in parallel, so each test interns its names with the prefix `nameof(Test) + "-"`. A color prints as `#RRGGBBAA`. An error demo prints `error <line>:<column> <kind>`. The projects have no implicit usings, so each new file holds its own `using` lines.

---

## Phase 1: A Text Sheet Paints a Panel (10/10 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetPaintsAPanel' --show-live-output on` and reads `panel #1E222CFF` and `plain #00000000`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Group A (the tokenizer, 5 files):**

- [x] ✅ **1.1 Record the baseline and read the code**
  - Description: Record in `context.md`: the base commit (`git rev-parse HEAD`), the test total (expected 1238), the build result, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30`. Read `StyleSheet.cs`, `StyleProperties.cs`, `Rgba8.cs`, `ClassName.cs`, `ElementType.cs`, `src/BrutalUi.Markup/BrutalUi.Markup.csproj`, and `tests/BrutalUi.Markup.Tests/PlaceholderTests.cs`.
  - File(s): `context.md`
  - Estimate: 6 steps

- [x] ✅ **1.2 Let the test project read the internals**
  - Description: Edit `src/BrutalUi.Markup/BrutalUi.Markup.csproj`: add `<InternalsVisibleTo Include="BrutalUi.Markup.Tests" />`, as `BrutalUi.Core.csproj` does. Add no `PackageReference`.
  - File(s): `src/BrutalUi.Markup/BrutalUi.Markup.csproj`
  - Estimate: 2 steps

- [x] ✅ **1.3 Write StyleParseError.cs**
  - Description: Write `src/BrutalUi.Markup/Style/StyleParseError.cs`: `StyleErrorKind` with all 13 members per "The Public API" (each with a doc comment that names its cause), and `StyleParseError` with `ToString` per "The Error Report".
  - File(s): `src/BrutalUi.Markup/Style/StyleParseError.cs`
  - Estimate: 5 steps

- [x] ✅ **1.4 Write the token types and the tokenizer**
  - Description: Write `src/BrutalUi.Markup/Style/StyleToken.cs` (the internal `StyleTokenKind` and `StyleToken`) and `src/BrutalUi.Markup/Style/StyleTokenizer.cs` (internal) per "The Tokenizer": each token kind, the position rule, the BOM, the ASCII rule, and the comment rule. The tokenizer adds its errors to a list that the parser gives it.
  - File(s): `src/BrutalUi.Markup/Style/StyleToken.cs`, `src/BrutalUi.Markup/Style/StyleTokenizer.cs`
  - Estimate: 10 steps

- [x] ✅ **1.5 Write StyleTokenizerTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/StyleTokenizerTests.cs`:
    - `EachTokenKindHasItsText` (a theory with one row for each kind, also `--accent`, `-4px`, `.5`, `50%`, `0.2s`, and `#1e222c`).
    - `ALineBreakStartsANewLine`: LF, CRLF, and a lone CR each give line 2, column 1 for the next token.
    - `AColumnCountsCodePoints`: `/* é */ .a` gives column 9 for the `.`. A tab counts 1.
    - `ABomAddsNoColumn`, `ACommentJoinsTheWhitespace`, and `ANonAsciiByteOutsideACommentIsAnError`.
    - `AnUnterminatedCommentIsAnError`: the position of the `/*`.
    - `TheEndTokenHoldsThePositionAfterTheText`: a text that ends with LF gives `End` at the next line, column 1.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/StyleTokenizerTests.cs`
  - Estimate: 8 steps

**Group B (the parser, 5 files):**

- [x] ✅ **1.6 Write StyleParseResult.cs**
  - Description: Write `src/BrutalUi.Markup/Style/StyleParseResult.cs` per "The Public API", with `GetSheetOrThrow`.
  - File(s): `src/BrutalUi.Markup/Style/StyleParseResult.cs`
  - Estimate: 4 steps

- [x] ✅ **1.7 Write StyleValues.cs with the color read**
  - Description: Write `src/BrutalUi.Markup/Style/StyleValues.cs` (internal static): the color read of "The Properties and the Value Forms" (3, 6, or 8 hex digits). Another digit count gives `InvalidValue`.
  - File(s): `src/BrutalUi.Markup/Style/StyleValues.cs`
  - Estimate: 5 steps

- [x] ✅ **1.8 Write StyleSheetParser.cs with the rule loop of phase 1**
  - Description: Write `src/BrutalUi.Markup/Style/StyleSheetParser.cs` per "The Public API": the two `Parse` overloads, the empty `CreateInfo`, and the rule loop of this phase. A selector is one `.class` compound. A declaration is one of `background-color`, `border-color`, and `color` with one color. The last `;` is optional. Each other form gives an error at its position (`UnexpectedToken`, `UnknownProperty`, or `InvalidValue`), and the parse stops at the first error in this phase (phase 5 adds the recovery). The rules go to `StyleSheet.Builder.Add` in the order of the source. The doc comments state that the parser can allocate and that the sheet does not.
  - File(s): `src/BrutalUi.Markup/Style/StyleSheetParser.cs`
  - Estimate: 10 steps

- [x] ✅ **1.9 Replace the placeholder test with TextSheetTests.cs**
  - Description: Delete `tests/BrutalUi.Markup.Tests/PlaceholderTests.cs`: the new tests load the assembly. Write `tests/BrutalUi.Markup.Tests/Style/TextSheetTests.cs`:
    - `ATextSheetPaintsAPanel` (the demo): `.<prefix>panel { background-color: #1e222c; }`. A panel with the class and a plain element with no class under one root. Print `panel <BackgroundColor>` and `plain <BackgroundColor>` after `Update`. Assert both lines.
    - `EachColorFormGivesTheColor` (a theory): `#fff`, `#FFF`, `#1e222c`, `#1E222C80`, and `#3060c0ff`.
    - `EachColorPropertySetsItsOwnMember`: the mask of each rule holds one bit only.
    - `CommentsAndWhitespaceChangeNothing`: comments between each token, CRLF line breaks, and a BOM give an equal sheet.
    - `TheLastSemicolonIsOptional`, and `ALaterRuleWinsAtEqualSpecificity`.
    - `AnErrorGivesNoSheet`: `.a { colour: #fff; }` gives `Sheet == null` and one `UnknownProperty` error at 1:6.
    - `GetSheetOrThrowNamesTheError`: the exception text holds `stylesheet(1,6): UnknownProperty:`.
  - File(s): `tests/BrutalUi.Markup.Tests/PlaceholderTests.cs` (deleted), `tests/BrutalUi.Markup.Tests/Style/TextSheetTests.cs`
  - Estimate: 10 steps

- [x] ✅ **1.10 Run the gate and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 2: A Hover on a Panel Restyles Its Row and Its Cell from Text (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCellFromText' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after `panel.States = Hover`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Group A (CR-4 in Core, 3 files):**

- [x] ✅ **2.1 Add the equality members and the text form to CompoundSelector**
  - Description: Edit `src/BrutalUi.Core/Style/CompoundSelector.cs`: implement `IEquatable<CompoundSelector>`, `Equals(object?)`, `==`, `!=`, and `GetHashCode` over `Type`, `Class`, `States`, and `Name` (`string.Equals(..., StringComparison.Ordinal)`, and `StringComparer.Ordinal.GetHashCode` for the name, with 0 for `null`). Add `ToString`: the type name, `.class`, `#name`, and each state in the order `:hover:active:focus:disabled:checked`. An id outside its table gives `<type N>` or `.<class N>`. An empty compound gives `""`. The remarks state that `ToString` allocates and that the equality allocates nothing.
  - File(s): `src/BrutalUi.Core/Style/CompoundSelector.cs`
  - Estimate: 7 steps

- [x] ✅ **2.2 Add the same members to Selector**
  - Description: Edit `src/BrutalUi.Core/Style/Selector.cs`: the same members over the subject, the ancestor, and the combinator. `ToString` gives `subject`, `ancestor > subject`, or `ancestor subject`, and `(empty)` for `default(Selector)`. Replace each remark that says the type has no equality members.
  - File(s): `src/BrutalUi.Core/Style/Selector.cs`
  - Estimate: 6 steps

- [x] ✅ **2.3 Write SelectorEqualityTests.cs**
  - Description: Write `tests/BrutalUi.Core.Tests/Style/SelectorEqualityTests.cs`: equal and not equal for each part and for each combinator, two names in two string instances, the hash codes of equal values, `==` and `!=`, `ToString` of each form of the specificity table of section 7, the fallback text of an id outside the table, and `EqualsAllocatesNothing` (two selectors with names, 1000 calls after `AllocationGate.ReleaseAllocationContext()`, 0 bytes). `StyleSheetTests` passes with no change.
  - File(s): `tests/BrutalUi.Core.Tests/Style/SelectorEqualityTests.cs`
  - Estimate: 8 steps

**Group B (the selectors from text, 3 files):**

- [x] ✅ **2.4 Write SelectorReader.cs**
  - Description: Write `src/BrutalUi.Markup/Style/SelectorReader.cs` (internal) per "The Grammar", "The selector map", and "The selector rejects". It reads a selector list from the token list and gives the selectors, or one error. It checks each case before it calls a factory.
  - File(s): `src/BrutalUi.Markup/Style/SelectorReader.cs`
  - Estimate: 10 steps

- [x] ✅ **2.5 Read each rule with SelectorReader**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: read each rule with `SelectorReader`. A selector list gives one rule for each selector, in the order of the source. A rule with no declaration gives no rule.
  - File(s): `src/BrutalUi.Markup/Style/StyleSheetParser.cs`
  - Estimate: 5 steps

- [x] ✅ **2.6 Write SelectorFormTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/SelectorFormTests.cs`:
    - `AHoverOnAPanelRestylesItsRowAndItsCellFromText` (the demo): `.<p>panel:hover > .<p>row { background-color: #3060c0; }` and `.<p>panel:hover .<p>cell { background-color: #ffcc00; }` on a tree of root, panel, row, and cell. Print after `Update`, then after `panel.States = PseudoStates.Hover` and `Update`. Assert both lines.
    - `EachSelectorFormGivesTheFactorySelector` (a theory): `Label`, `.a`, `#title`, each state alone, `Label.a#title:hover:checked`, `.a > .b`, `.a>.b`, `.a .b`, `.a  /* c */ .b`, `Label:hover > #title`, and `:focus .b`. Each row compares the parsed selector with the selector of the factories through `Assert.Equal`.
    - `ASelectorListGivesOneRuleForEachSelector`: `.a, .b > .c, #d { ... }` gives 3 rules with equal values, in the order of the source within equal specificity.
    - `TheTextFormRoundTrips`: for each row of the first theory, `Parse(selector.ToString())` gives an equal selector.
    - `EachRejectedFormGivesAPositionedError` (a theory): one row for each line of "The selector rejects", with the kind, the line, and the column.
    - `ATypeNameIsCaseSensitive`: `label` does not match a `Label`. `AnUnknownTypeMatchesNothingAndIsNoError`.
    - `ANameKeepsItsCase`, and `TheSpecificityComesFromTheSelector`: the rules hold the numbers of the tier table.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/SelectorFormTests.cs`
  - Estimate: 12 steps

- [x] ✅ **2.7 Run the gate, the demo, and the grep**
  - Description: Run the build, the tests, and the demo. Run `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` and expect no output. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 3: A Text Sheet Lays Out a Panel (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetLaysOutAPanel' --show-live-output on` and reads `panel 20,10,200,40`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 Write PropertyTable.cs**
  - Description: Write `src/BrutalUi.Markup/Style/PropertyTable.cs` (internal static): one entry for each of the 32 names of "The Properties and the Value Forms", with its `StyleProperty` member and its value form. Add an explicit keyword table for each of the 7 enums. The table is built one time, at the first use, with `StringComparer.Ordinal`.
  - File(s): `src/BrutalUi.Markup/Style/PropertyTable.cs`
  - Estimate: 8 steps

- [x] ✅ **3.2 Read each value form in StyleValues.cs**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleValues.cs`: the length, the number (invariant culture), the keyword, the edge set and the corner set in the CSS order of the table, the `px` value of `font-size`, and the font. `font: default;` gives `new FontHandle(0)` when the map does not hold `default`. A key of `CreateInfo.Fonts` gives its handle. Another name gives `UnknownFont`. A wrong form or a wrong count gives `InvalidValue` at the first token at fault.
  - File(s): `src/BrutalUi.Markup/Style/StyleValues.cs`
  - Estimate: 10 steps

- [x] ✅ **3.3 Read each property in the declaration loop**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: `CreateInfo.Fonts`. The declaration loop finds the name in `PropertyTable`, reads the value with `StyleValues`, and writes it through the `StyleProperties` setter. An unknown name gives `UnknownProperty`. A later declaration of the same property wins.
  - File(s): `src/BrutalUi.Markup/Style/StyleSheetParser.cs`
  - Estimate: 6 steps

- [x] ✅ **3.4 Write PropertyFormTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/PropertyFormTests.cs`:
    - `ATextSheetLaysOutAPanel` (the demo): a viewport of 400 by 300. `.<p>panel { width: 50%; height: 40px; margin: 10px 20px; }` on a child of a root with no style. Print `panel <x>,<y>,<width>,<height>` from `Layout.Rect` with the invariant culture. Assert the line.
    - `EachPropertyNameSetsItsOwnMember` (a theory with 32 rows): each row sets one name to a value that is not the default. The rule mask holds the bit of that member only, and the getter gives the value.
    - `EachKeywordGivesItsMember` (a theory with 28 rows: each keyword of each enum table).
    - `EachLengthFormGivesItsLength` (a theory): `12px`, `-4px`, `.5px`, `50%`, `1.25%`, `auto`, and `0`.
    - `ABareNumberForALengthIsAnError`: `width: 12;` gives `InvalidValue` at the `12`.
    - `NumbersReadInTheInvariantCulture`: with `CultureInfo.CurrentCulture` set to `de-DE` in the test (restored in `finally`), `opacity: 0.5;` gives 0.5.
    - `AFontNameReadsTheFontMap`, `DefaultGivesTheFontIdZero`, and `AnUnknownFontIsAnError`.
    - `ALaterDeclarationWins`, and `AColorRuleLetsTheFontInherit`: a parent rule sets `font-size: 24px`, a child rule sets only `color`, and the child gets size 24.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/PropertyFormTests.cs`
  - Estimate: 12 steps

- [x] ✅ **3.5 Write EdgeAndCornerTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/EdgeAndCornerTests.cs`: `EachEdgeCountFollowsTheCssOrder` (a theory: 1 to 4 values for `margin`, `padding`, and `border-width`, with four different values, and an assert of each of the four edges), `EachCornerCountFollowsTheCssOrder` (the same for `border-radius`), `AFifthValueIsAnError` (at the fifth value), and `AnEdgeSetWithMixedUnits` (`margin: 4px 10% auto 0`).
  - File(s): `tests/BrutalUi.Markup.Tests/Style/EdgeAndCornerTests.cs`
  - Estimate: 8 steps

- [x] ✅ **3.6 Run the gate and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed line and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 4: A Theme Variable Colors a Button (4/4 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AThemeVariableColorsTheButton' --show-live-output on` and reads `button #3060C0FF`, then `error 2:25 UnknownVariable` for a second sheet that reads `var(--missing)`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **4.1 Write ThemeVariables.cs**
  - Description: Write `src/BrutalUi.Markup/Style/ThemeVariables.cs` (internal): pass 1 per "Theme Variables". It finds each `:root` block at the top level, stores each `--name` with the token range of its value, and gives `InvalidVariable` for a declaration that is not a variable, for an empty value, and for a `var(` inside a value.
  - File(s): `src/BrutalUi.Markup/Style/ThemeVariables.cs`
  - Estimate: 8 steps

- [x] ✅ **4.2 Put the variables in place in the parser**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: run pass 1 before the rule loop. Skip each `:root` block in pass 2. Before the value read, put the tokens of each `var(--name)` in place. Give `UnknownVariable` at the `--name` token, and `InvalidVariable` for a `--name` declaration outside `:root` and for a fallback. A value error inside a variable value points at the `var` token, and its message names the variable.
  - File(s): `src/BrutalUi.Markup/Style/StyleSheetParser.cs`
  - Estimate: 8 steps

- [x] ✅ **4.3 Write ThemeVariableTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/ThemeVariableTests.cs`:
    - `AThemeVariableColorsTheButton` (the demo): sheet 1 holds `:root { --accent: #3060c0; }` and `.<p>button { background-color: var(--accent); }`. Print `button <BackgroundColor>` after `Update`. Sheet 2 is `.<p>button {` + LF + `  background-color: var(--missing);` + LF + `}`. Print `error <line>:<column> <kind>` of its one error. Assert both lines.
    - `AVariableBeforeItsDefinitionResolves`, and `ALaterDefinitionWins`.
    - `AVariableHoldsMoreThanOneValue` (`--pad: 4px 8px;`), and `AVariableMixesWithOtherValues` (`padding: var(--gap) 8px;`).
    - `AVariableIsLocalToItsSheet`: a second `Parse` gives `UnknownVariable` for a name of the first text.
    - `VariableNamesAreCaseSensitive`.
    - `EachVariableErrorHasItsPosition` (a theory): a variable outside `:root`, a property in `:root`, an empty value, `var()` in a variable value, a fallback, and `:root .a`.
    - `AValueErrorFromAVariablePointsAtTheUse`: `--w: 12;` and `width: var(--w);` give `InvalidValue` at the `var` token.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/ThemeVariableTests.cs`
  - Estimate: 10 steps

- [x] ✅ **4.4 Run the gate and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 5: A Sheet with Three Mistakes Reports Each One (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ASheetWithThreeMistakesReportsEachOne' --show-live-output on` and reads `error 2:3 UnknownProperty`, `error 3:10 InvalidValue`, `error 4:1 UnexpectedEnd`, and `sheet null`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

- [x] ✅ **5.1 Add the recovery and the unsupported forms to the parser**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: the recovery of "The Error Report" in place of the stop at the first error. Add `UnexpectedEnd`, the at-rule check, the `!important` check, and the check of a function other than `var`. Each read checks the end of the token list.
  - File(s): `src/BrutalUi.Markup/Style/StyleSheetParser.cs`
  - Estimate: 10 steps

- [x] ✅ **5.2 Add the range checks to StyleValues.cs**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleValues.cs`: the range checks. A negative value for `padding`, `border-width`, `border-radius`, `flex-grow`, `flex-shrink`, and `font-size`. `opacity` outside 0 to 1. `auto` for `padding`, `border-width`, `border-radius`, and `font-size`. `%` for `border-width`, `border-radius`, and `font-size`. A number that is not finite. An unknown unit (`12em`). Each check gives `InvalidValue` at the token at fault.
  - File(s): `src/BrutalUi.Markup/Style/StyleValues.cs`
  - Estimate: 8 steps

- [x] ✅ **5.3 Write the error messages**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleParseError.cs` and each place that makes an error: the message rule of "The Error Report". Each message quotes the source text and names the fix.
  - File(s): `src/BrutalUi.Markup/Style/StyleParseError.cs`, `src/BrutalUi.Markup/Style/StyleSheetParser.cs`, `src/BrutalUi.Markup/Style/StyleValues.cs`, `src/BrutalUi.Markup/Style/SelectorReader.cs`, `src/BrutalUi.Markup/Style/ThemeVariables.cs`, `src/BrutalUi.Markup/Style/StyleTokenizer.cs`
  - Estimate: 8 steps

- [x] ✅ **5.4 Write ErrorReportTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/ErrorReportTests.cs`:
    - `ASheetWithThreeMistakesReportsEachOne` (the demo): `.<p>panel {` + LF + `  colour: #fff;` + LF + `  width: 12em;` + LF. Print `error <line>:<column> <kind>` for each error, then `sheet null`. Assert the four lines.
    - `EachErrorKindHasItsPosition` (a theory with one row or more for each of the 13 kinds).
    - `EachRangeCheckGivesAnError` (a theory with one row for each check of task 5.2).
    - `TheParserFindsAnErrorAfterABadSelector`, and `TheParserFindsAnErrorAfterABadDeclaration`.
    - `TheErrorTextHasTheSourceAndThePosition`: `ToString` gives `hud.css(2,3): UnknownProperty: ` and then the message.
    - `EachMessageQuotesTheSourceText`: for each row of the kind theory, the message holds the text of the token at fault.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/ErrorReportTests.cs`
  - Estimate: 10 steps

- [x] ✅ **5.5 Write ParserRobustnessTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/ParserRobustnessTests.cs`:
    - `NoRandomTextMakesTheParserThrow`: 2000 texts from `new Random(1234)`: random bytes, and random mixes of the tokens of the format. `Parse` gives a result for each text, and a result with no sheet has one error or more.
    - `EachCutOfAValidSheetGivesAnErrorOrASheet`: a valid sheet of each form, cut at each byte. No cut throws. A cut inside a rule gives an error.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/ParserRobustnessTests.cs`
  - Estimate: 8 steps

- [x] ✅ **5.6 Run the gate and the demo**
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 6: A Transition Declaration Lives in the Rule (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATransitionDeclarationIsStoredInTheRule' --show-live-output on` and reads `BackgroundColor 0.2s EaseOut`, then `Opacity none`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Group A (the Core model, 4 files):**

- [x] ✅ **6.1 Write StyleTransitions.cs**
  - Description: Read `docs/features/style-system/transitions/` and search `src/BrutalUi.Core` for an easing or transition type. When the transitions feature added one already, use it, record the change in `context.md`, and write no second type. Else write `src/BrutalUi.Core/Style/StyleTransitions.cs` per "The Public API". The set holds one slot for each member of Q3 and a mask. `Set` and `Get` throw `ArgumentOutOfRangeException` for each other member, with a message that names the five members. The remarks state Q3 and that the model keeps each duration (the transitions feature applies its rule for 0, `NaN`, and infinity).
  - File(s): `src/BrutalUi.Core/Style/StyleTransitions.cs`
  - Estimate: 8 steps

- [x] ✅ **6.2 Add the Transitions field to StyleRule**
  - Description: Edit `src/BrutalUi.Core/Style/StyleRule.cs`: the `Transitions` field and the new constructor. The old constructor gives an empty set. Replace the remark "The style-parser feature adds the transition member to this struct later".
  - File(s): `src/BrutalUi.Core/Style/StyleRule.cs`
  - Estimate: 4 steps

- [x] ✅ **6.3 Add the Add overload to StyleSheet.Builder**
  - Description: Edit `src/BrutalUi.Core/Style/StyleSheet.cs`: the new `Add` overload. The old `Add` calls it with an empty set. The sort and the copy do not change.
  - File(s): `src/BrutalUi.Core/Style/StyleSheet.cs`
  - Estimate: 4 steps

- [x] ✅ **6.4 Write StyleTransitionsTests.cs**
  - Description: Write `tests/BrutalUi.Core.Tests/Style/StyleTransitionsTests.cs`: `CanTransition` for each of the 32 members (a theory), set, get, and clear for each of the five, the throw for each other member, `IsEmpty`, the copy into the rule, an empty set from the old constructor and from the old `Add`, the sort keeps the set with its rule, and `ARuleWithATransitionStylesTheSame`: the style pass gives the same `ComputedStyle` for a rule with a transition and with no transition.
  - File(s): `tests/BrutalUi.Core.Tests/Style/StyleTransitionsTests.cs`
  - Estimate: 8 steps

**Group B (the declaration, 4 files):**

- [x] ✅ **6.5 Add the transition entry to PropertyTable.cs**
  - Description: Edit `src/BrutalUi.Markup/Style/PropertyTable.cs`: the `transition` entry, the five member names (`background-color`, `border-color`, `color`, `border-radius`, `opacity`), and the four easing keywords (`linear`, `ease-in`, `ease-out`, `ease-in-out`).
  - File(s): `src/BrutalUi.Markup/Style/PropertyTable.cs`
  - Estimate: 4 steps

- [x] ✅ **6.6 Read the transition value in StyleValues.cs**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleValues.cs`: `<property> <duration> <easing>`, and more of them in a comma list. A duration is `<number>s` or `<number>ms` (divided by 1000), 0 or more. All three parts are required. A member outside the five gives `InvalidValue` with a message that names the five. A wrong duration or a wrong easing gives `InvalidValue` at its token.
  - File(s): `src/BrutalUi.Markup/Style/StyleValues.cs`
  - Estimate: 6 steps

- [x] ✅ **6.7 Collect the transitions of each rule in the parser**
  - Description: Edit `src/BrutalUi.Markup/Style/StyleSheetParser.cs`: each rule collects a `StyleTransitions` set and adds it through the new `Add` overload. A later `transition` declaration for the same member wins. A rule with only a transition declaration gives a rule.
  - File(s): `src/BrutalUi.Markup/Style/StyleSheetParser.cs`
  - Estimate: 5 steps

- [x] ✅ **6.8 Write TransitionDeclarationTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/TransitionDeclarationTests.cs`:
    - `ATransitionDeclarationIsStoredInTheRule` (the demo): `.<p>button { background-color: #1e222c; transition: background-color 0.2s ease-out; }`. Print `BackgroundColor <duration>s <easing>` and `Opacity none` from the rule. Assert both lines.
    - `EachMemberCanTransition` (5 rows), `EachEasingKeywordGivesItsMember` (4 rows), and `MillisecondsGiveSeconds` (`200ms` gives 0.2).
    - `ACommaListSetsEachMember`, `ALaterDeclarationWinsForTheSameMember`, and `AVariableCanHoldTheDuration`.
    - `EachWrongTransitionGivesAPositionedError` (a theory): `width 1s linear`, `opacity -1s linear`, `opacity 1px linear`, `opacity 1s ease`, `opacity 1s`, and `opacity`.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/TransitionDeclarationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **6.9 Run the gate, the demo, and the facts line**
  - Description: Run the build, the tests, and the demo. Run the facts line of task 1.1 and record `bytes per element`. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 5 steps

---

## Phase 7: HelloPanel Draws from a Stylesheet (12/12 complete)

**Demo:** the developer runs `scripts/test-linux.sh; echo "exit=$?"` and reads `test-linux: PASS build, test, gpu, smoke (120 frames) in <S> s` and `exit=0`. Then `grep -c 'draw commands 2, vertices 128' artifacts/test-linux/smoke.log` prints `1`.
**Needs:** Phase 6
**Agent tier:** ADVANCED (opus)

**Group A (the sample and the gate, 6 files):**

- [x] ✅ **7.1 Write HelloScreen.css**
  - Description: Write `samples/HelloPanel/HelloScreen.css`. A `:root` block holds `--panel-fill: #1e222c;`, `--panel-border: #608cdc;`, and `--text: #ffffff;`. `.hello-root` holds `align-items: flex-start; padding: 24px;`. `.hello-panel` holds `flex-direction: column; align-items: flex-start; padding: 16px; border-width: 2px; border-radius: 8px;` and the two colors through `var()`. `.hello-panel > Label` holds `color: var(--text); font-size: 26px;`. `.hello-panel > Image` holds `margin: 12px 0 0;`. Each value equals the old constant of `HelloScreen.cs`. A comment at the top names the file and the format section of the design doc.
  - File(s): `samples/HelloPanel/HelloScreen.css`
  - Estimate: 5 steps

- [x] ✅ **7.2 Reference Markup and copy the sheet in HelloPanel.csproj**
  - Description: Edit `samples/HelloPanel/HelloPanel.csproj`: a `ProjectReference` to `../../src/BrutalUi.Markup/BrutalUi.Markup.csproj`, and `<None Include="HelloScreen.css" CopyToOutputDirectory="PreserveNewest" />`.
  - File(s): `samples/HelloPanel/HelloPanel.csproj`
  - Estimate: 2 steps

- [x] ✅ **7.3 Set classes in HelloScreen.cs and add LoadSheet**
  - Description: Edit `samples/HelloPanel/HelloScreen.cs`: `Build` sets the classes `hello-root` and `hello-panel` (two `static readonly ClassId` fields) and no inline value. Remove the constants that moved to the sheet. Keep the checker constants and the image tint (a create-info value). Add `LoadSheet()`: it reads `Path.Combine(AppContext.BaseDirectory, "HelloScreen.css")` and returns `new StyleSheetParser().Parse(bytes, "HelloScreen.css").GetSheetOrThrow()`. Update the remarks: the sizes and the colors live in `HelloScreen.css`.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 6 steps

- [x] ✅ **7.4 Load the sheet in Program.cs**
  - Description: Edit `samples/HelloPanel/Program.cs`: call `HelloScreen.LoadSheet()` at the start of `Run`, before the window opens, so a bad sheet fails with no GPU. Give the sheet to `RunImGui` and call `ui.AddSheet(sheet)` before `ui.AddRoot`. A parse error goes to the existing `catch`, which writes the error lines to standard error and returns exit code 1. The 3 output lines and the exit codes do not change. Update the remarks.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 5 steps

- [x] ✅ **7.5 Link the gate and the sheet into the Markup tests**
  - Description: Edit `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`: `<Compile Include="../BrutalUi.Core.Tests/Shared/AllocationGate.cs" LinkBase="Shared" />` and `<None Include="../../samples/HelloPanel/HelloScreen.css" Link="HelloScreen.css" CopyToOutputDirectory="PreserveNewest" />`.
  - File(s): `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`
  - Estimate: 2 steps

- [x] ✅ **7.6 Write ParsedSheetAllocationTests.cs**
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`:
    - `AParsedSheetAllocatesNothingInUpdate`: one parsed text with a variable, each selector form, each value family, and a transition list. A tree of 50 elements with the classes and the names of the rules. Each step toggles `Hover` on a panel with children, toggles one class, and sets a name to one of two `const string` values, then calls `Update`. Run 16 warm-up steps, call `AllocationGate.ReleaseAllocationContext()`, measure 100 steps, and assert 0 bytes. Assert also that the last step gives the expected values, so the steps reach the rules.
    - `TheHelloPanelSheetGivesTheOldValues`: parse the linked `HelloScreen.css`. Assert success and each value of task 7.1 against the old constants (copied into the test, with a comment that names the source).
  - File(s): `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **7.7 Prove that the gate blocks**
  - Description: Copy `src/BrutalUi.Core/Style/Selector.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Selector.Matches(Element, ElementTypeId)`. Run `dotnet test --solution brutal-ui.sln > <scratch>/plant.log 2>&1; echo "exit=$?"` and expect a value that is not 0, with `AParsedSheetAllocatesNothingInUpdate` in the failed list. Restore the file from the copy. Run `diff` on the two files and expect no output. Record both exit codes and the failed tests in `context.md`.
  - File(s): `src/BrutalUi.Core/Style/Selector.cs` (temporary, restored), `context.md`
  - Estimate: 5 steps

- [x] ✅ **7.8 Run the smoke script and the error path**
  - Description: Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Read the 3 lines of `artifacts/test-linux/smoke.log`: `120 frames rendered`, `draw commands 2, vertices 128`, and `Update and Draw 0`. Then prove the error path: write `.hello-panel { colour: #fff; }` into `samples/HelloPanel/bin/Debug/net10.0/HelloScreen.css`, run `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 1; echo "exit=$?"`, and expect `HelloScreen.css(1,16): UnknownProperty:` on standard error and `exit=1`. Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30` to restore the copy. Record each result in `context.md`.
  - File(s): `context.md`
  - Estimate: 6 steps

**Group B (the record, 4 files):**

- [x] ✅ **7.9 Write the format into the design doc**
  - Description: Edit `docs/technical-design.md`. Section 11, "Stylesheet format": the grammar, the property table, the value forms, the edge order, the text members that inherit, theme variables, the transition declaration, the error report, the parser API, and the walk bound of `.a .b` (the parent and the grandparent only, which differs from CSS). Section 7: `StyleRule.Transitions`, the model of `StyleTransitions`, and the equality members of the selector types (in place of the design decision of DS1.7). Section 14: the test classes of `BrutalUi.Markup.Tests` and the new gate with the plant result of task 7.7. Update the cost line of section 7 with the facts of task 6.9 when the value changed.
  - File(s): `docs/technical-design.md`
  - Estimate: 10 steps

- [x] ✅ **7.10 Add the changelog lines**
  - Description: Edit `CHANGELOG.md`: one line under `### Added` (`StyleSheetParser`, `StyleParseResult`, `StyleParseError`, `StyleErrorKind`, the text format with theme variables, `TransitionEasing`, `StyleTransition`, `StyleTransitions`, the new `StyleRule` constructor and `Add` overload, and the equality members and the text form of `CompoundSelector` and `Selector`) and one line under `### Changed` (the HelloPanel sample loads `HelloScreen.css`). Name the feature `style-system/style-parser`.
  - File(s): `CHANGELOG.md`
  - Estimate: 3 steps

- [x] ✅ **7.11 Add the human testing section**
  - Description: Edit `docs/features/style-system/human-testing.md`: add a `## style-parser` section with the 7 phase demos, the error path of task 7.8, and the Windows run of `.\scripts\test-windows.ps1`.
  - File(s): `docs/features/style-system/human-testing.md`
  - Estimate: 3 steps

- [x] ✅ **7.12 Lint the docs and write the hand-offs**
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/style-system/human-testing.md`, `docs/features/style-system/style-parser/implementation.md`, `context.md`
  - Estimate: 8 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- None

---

## Technical Debt

- None yet

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
