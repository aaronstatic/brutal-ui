# style-system/style-parser - Code Review

**Reviewed:** 2026-10-22
**Reviewer:** Agent (cross-phase review of phases 1 to 7)
**Rules Applied:** 1. A correct map, 2. A correct error report, 3. Theme variables at parse time (A9), 4. Zero allocation in the frame loop, 5. API clarity and the hand-off to transitions, 6. No regression, 7. Test quality, `AGENTS.md`, and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 8 findings, 8 resolved

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 2 |
| LOW | 6 |

The parser gives the right sheet for each probe. Each of the 32 names sets its own member, the edge sets and the corner sets follow the CSS order, and each selector form gives the selector of the factories. No input of the probes makes `Parse` throw. A parsed sheet allocates nothing in `Update`, and a plant in `Selector.Matches` or in the match branch of `StylePass` fails the new gate. A shipped test kills 21 of the 22 mutations of the map, the position rule, the selector rejects, the variables, the recovery, the merge, and the allocation paths.

The two MEDIUM findings are both in the error report. The drop check of DS5.5 is quadratic, so a text with many non-ASCII bytes takes seconds to parse (CR-1). An error in a variable definition gives a second error at each use, with a message that names the wrong fault (CR-2). The LOW findings are a column rule for a stray UTF-8 byte that hides an error (CR-3), a test gap of the nested-brace skip (CR-4), the exception catch of `ToString` (CR-5), an unused variable with no value check and no doc line (CR-6), doc text that does not match the code (CR-7), and the size and the load cost of the larger `StyleRule` (CR-8).

All probes ran in the scratch copy `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/1041e7b1-7770-4054-8b67-c6d8d936fcf3/scratchpad/cr-style-parser/repo`. The copy holds a switch class `src/BrutalUi.Core/ReviewMutation.cs`, which reads the variable `CR_M`, and one guarded mutation at each probe site. The probe class `tests/BrutalUi.Markup.Tests/ReviewProbes.cs` writes its lines to `../probes.txt`, and each mutation run writes its output to `../mut/<name>.txt`. The mutation runs use the Markup test project (547 tests and the 3 probes), except M15, which uses the Core test project (1232 tests). No file of the repository changed, except this file.

1. **Baseline.** `dotnet build` gives 0 warnings and 0 errors. `dotnet test` gives total 2023, failed 0. The switch run M0 (no mutation) gives total 550, failed 0.
2. **The edge map and the corner map (rule 1).** M1, `left` and `top` swapped in `MapEdges`: 19 failures (for example `TheHelloPanelSheetGivesTheOldValues`, `ALaterDeclarationWins`, `AVariableMixesWithOtherValues`). M2, `topLeft` and `topRight` swapped in `MapCorners`: 5 failures (`EachCornerCountFollowsTheCssOrder` 3 rows, `EachPropertyNameSetsItsOwnMember`, `AParsedSheetAllocatesNothingInUpdate`).
3. **The other map checks (rule 1).** M16, the current culture in `TryReadNumber`: 1 failure (`NumbersReadInTheInvariantCulture`). M19, `color` stored as `BorderColor` in a transition: 5 failures. M20, `ease-in` stored as `EaseOut`: 9 failures. M21, `left` written to `Right`: 1 failure (`EachPropertyNameSetsItsOwnMember`). M22, `font: default` gives the id 1: 2 failures (`DefaultGivesTheFontIdZero`, `AParsedSheetAllocatesNothingInUpdate`). M23, a later `transition` declaration replaces the full set (CSS): 1 failure (`ALaterDeclarationWinsForTheSameMember`), so a test pins Conflict 2.
4. **The position rule (rule 2).** M3, a tab counts 4: 1 failure (`AColumnCountsCodePoints`). M4, CRLF counts 2 lines: 3 failures (`ALineBreakStartsANewLine`, `EachLineBreakCountsOneTime`, `TheEndTokenHoldsThePositionAfterTheText`). M5, a continuation byte counts 1 column: 5 failures.
5. **The selector rejects (rule 1).** M6, no check of a second class: 5 failures. M7, no check of a third compound: 4 failures. M8, no check of `:not(`: 2 failures. In each run, no test output holds an exception, so `Parse` does not throw with the check removed.
6. **The variables (rule 3).** M9, one static variable map for each process: 2 failures (`AVariableIsLocalToItsSheet`, `VariableNamesAreCaseSensitive`). M10, a value error from a variable points at the definition token, not at `var`: 3 failures (`AValueErrorFromAVariablePointsAtTheUse`, `AValueErrorInAMixedValuePointsAtTheUse`, `AWrongDurationFromAVariablePointsAtTheUse`).
7. **The recovery and the merge (rule 2).** M11, a declaration skip with no brace count (it stops at the first `}`): **0 failures** (see CR-4). M12, a declaration skip and a block skip that read past the End token: 4 failures, with `ArgumentOutOfRangeException` in `NoRandomTextMakesTheParserThrow` and `EachCutOfAValidSheetGivesAnErrorOrASheet`. M17, no drop of the declaration errors at a token error (DS5.5): 2 failures. M18, no drop of a copy in the merge: 2 failures.
8. **The planted allocations (rule 4).** M13, `GC.KeepAlive(new object());` at the start of `Selector.Matches(Element, ElementTypeId)`: 1 failure of 550 (`AParsedSheetAllocatesNothingInUpdate`). Task 7.7 records the 4 Core failures of the same plant. M14, the same plant in the match branch of `StylePass.Restyle`: 1 failure (`AParsedSheetAllocatesNothingInUpdate`). M15, the same plant in `Selector.EqualParts`: 1 failure of 1232 (`EqualsAllocatesNothing`).
9. **The drop check of DS5.5 (known item 1).** 20000 copies of `.a{cé:1px;}` (240 KB): 2913 ms. 50000 copies (600 KB): 17784 ms. 20000 copies of `.a:hé{}`: 2921 ms. The valid sheet of 20000 copies of `.a{color:#fff;}` (300 KB): 64 ms, and of 50000 copies (750 KB): 146 ms. 50000 copies of `.a{colr:#fff;}` (rule errors and no token error): 92 ms. Thus 2.5 times the text takes 6.1 times the time (see CR-1).
10. **A large valid sheet.** 20000 rules of two selectors each (`.crbigN > Label:hover, #nN`), each rule with an edge list, a variable list, a variable color, a corner list, a percent, and a transition list (3.8 MB): 398 ms, 193 MiB allocated, 40000 rules in the sheet. The 50000-rule sheet of probe 9 allocates 134 MiB. `StyleRule` is 368 bytes, `StyleTransitions` is 44 bytes, and `Selector` is 56 bytes (see CR-8).
11. **The round trip of `ToString`.** 20 forms give an equal selector after `Parse`: each form of section 7, all five states, a chain with a state, `my-widget`, `my-widget.my-class`, `-w`, `my-widget > -w`, `.my-class`, `.-x`, `.--y`, `#my-name`, `#-n`, `#1n`, and `#--n`. The class `1a` gives `.1a`, which the parser reads as a number (`UnexpectedToken` at 1:1). The name `a b` gives `#a b`, which reads back as a descendant chain (see CR-7).
12. **Edge cases.** CRLF, a lone CR, a tab, and a comment with two `é` before the token give the right line and column. `margin: 1px/**/2px` gives `InvalidValue` "touches" at 1:18 (see CR-7). An invalid variable definition gives a second `UnknownVariable` error at each use (see CR-2). The bytes `80 C3 A9 78` give one `UnexpectedCharacter` at 1:1, and the error of `é` is lost (see CR-3). A text of 200000 open braces parses in 14 ms with 2 errors. A font map with the key `default` gives its own id (5). Two `transition` declarations for two members keep both slots (Conflict 2).
13. **Rule checks.** `git diff --quiet c2af581 -- Directory.Packages.props src/BrutalUi.ImGui src/BrutalUi.Core/Style/StylePass.cs` gives exit 0. The Core diff holds only `CompoundSelector.cs`, `Selector.cs`, `StyleRule.cs`, and `StyleSheet.cs`, and no Core test file changed. The reflection grep of `src/BrutalUi.Markup/Style/*.cs` gives no hit (the hits of `src/BrutalUi.Markup` come from the generated `obj` files only). `BrutalUi.Markup.csproj` holds one `ProjectReference` and no `PackageReference`.
14. **R2 and the analyzer.** A call of `Parse(string)` and of `Parse(string, string)` with a local text and a local name compiles with no `BRUTALCORE0011` hit, also with the default source name. Only a constant string argument gives the error. R2 holds.
15. **The sample error path.** In the scratch copy, a bin copy of `HelloScreen.css` with three faults gives exit 1, no line on standard output, and the error lines on standard error: `HelloScreen.css(1,16): UnknownProperty: ...`, then the two lines of CR-2. The GPU smoke run did not run, because the brief starts no service. Task 7.8 records `draw commands 2, vertices 128` and exit 0.

### Known items from the build

1. **The drop check of DS5.5 is quadratic.** The item still holds. Probe 9 measures 2.9 s for 20000 copies and 17.8 s for 50000 copies. See CR-1.
2. **DS2.2: `CompoundSelector.ToString` catches `ArgumentOutOfRangeException` of the name tables.** The item still holds. See CR-5.
3. **DS4.9: a variable that no rule reads gets no value check.** The item still holds. `:root { --a: banana; } .x { color: #fff; }` gives a sheet. Section 11 does not state the rule. See CR-6.
4. **Bytes per element grew from 1046.6 to 1049.5 (Risk 8).** The item still holds, and section 7 states the new value and the cause. `StyleRule` grows from 320 to 368 bytes, and no doc gives that size or the load cost of the parse. See CR-8.
5. **The descendant walk bound and Conflict 2 differ from CSS on purpose.** Section 11 states both ("The walk bound of `.a .b`" and the `transition` bullets), and M23 proves that a test pins Conflict 2. Not reported.
6. **R1 and R2.** The four answers of R1 match the code: the phase 1 demo, the equality members of CR-4, the five slots, and the font map. R2 holds (probe 14). Not reported.

---

## Findings

### CR-1: The drop check of DS5.5 is quadratic

**Severity:** MEDIUM
**Rule:** 2. A correct error report (the load cost of the report)
**File:** `src/BrutalUi.Markup/Style/StyleSheetParser.cs` lines 706 to 725 (`DropDeclarationErrorsAtTokenError`), lines 731 to 754 (`DropSelectorErrorAtTokenError`), and lines 760 to 771 (`DropAtTokenError`)

**Problem:** Each declaration or selector with an error calls `DropAtTokenError`, and the method reads each token error of the text:

```csharp
foreach (StyleParseError tokenError in _tokenErrors)
{
    if (ComparePosition(tokenError.Line, tokenError.Column, lowerLine, lowerColumn) >= 0
        && ComparePosition(tokenError.Line, tokenError.Column, upperLine, upperColumn) < 0)
```

Thus the time grows with the count of token errors times the count of rule errors. Probe 9 measures 2.9 s for 20000 copies of `.a{cé:1px;}` and 17.8 s for 50000 copies, against 146 ms for a valid sheet of 750 KB. A file in the wrong encoding (for example UTF-16, where each second byte is 0x00) gives one token error for each character and many rule errors, so a game can hang at load for seconds before it shows the error. The parser runs at load time only, so no frame cost exists.

**Fix:** The tokenizer adds the token errors in the order of the source, so `_tokenErrors` is sorted by line and column. In `DropAtTokenError`, find the first token error at or after the lower position with a binary search, and test only that error against the upper position. An alternative is one moving index, because the rule loop also reads the text in order. Add a row to `ParserRobustnessTests` with 50000 copies of `.a{cé:1px;}` that asserts 50000 errors. Add a wide time bound (for example 2 s), or record the probe time in `context.md`. Remove the line of the drop check from `## Hand-offs` and from the follow-up of phase 5.

**Effort:** Low

**Resolved:** 2026-10-22 - `DropAtTokenError` finds the first token error at or after the lower position with a binary search, and 50000 copies of `.a{cé:1px;}` parse in 403 ms, not 20130 ms (DR2.1, DR2.2).

### CR-2: An error in a variable definition gives a second error at each use

**Severity:** MEDIUM
**Rule:** 2. A correct error report, and 3. Theme variables at parse time
**File:** `src/BrutalUi.Markup/Style/ThemeVariables.cs` lines 242 to 258 (a rejected value stays undefined), `src/BrutalUi.Markup/Style/StyleSheetParser.cs` lines 659 to 666 (`UnknownVariable` in `ExpandVariable`), and `docs/technical-design.md` line 904

**Problem:** Pass 1 records the error of a bad definition, and the variable stays undefined. Pass 2 then gives `UnknownVariable` at each use. The message says that the variable is not defined and asks for a definition that the text already holds. Probe 12:

```
p.css(1,12): UnsupportedSyntax: 'calc' is a function. ...
p.css(1,37): UnknownVariable: The variable '--a' is not defined. Define it in a ':root' block of this text, ...
p.css(1,64): UnknownVariable: The variable '--a' is not defined. ...
```

The same occurs for an empty value (`--a: ;`) and for a value that reads a variable (`--a: var(--b);`). The sample error path of probe 15 shows the same pair. A second case: `:root{--é:1px;}` gives `InvalidVariable` at 1:7 for the cut name `--` and `UnexpectedCharacter` at 1:9. DS5.5 drops such a result error in pass 2, but its rules do not apply to pass 1. Rule 2 asks for a message that names the problem and the fix, and DS5.5 says that "each error that stays names a real second fault". These errors name no second fault.

**Fix:** In `ThemeVariables`, keep a `HashSet<string>` of each name with a rejected definition, and add `IsRejected(string name)`. In `ExpandVariable`, return `false` with no new error for such a name, because the definition error names the fault. For the second case, apply the declaration rule of DS5.5 to pass 1: drop the error of a `:root` declaration when a token error lies inside that declaration. Add rows to `ThemeVariableTests` for `calc(`, an empty value, and `var(` in a definition with one use each (one error each), and a row for `:root{--é:1px;}` (one error). Update DS5.5 and the `UnknownVariable` row of section 11.

**Effort:** Low

**Resolved:** 2026-10-22 - `ThemeVariables` keeps a rejected set (`IsRejected`), `ExpandVariable` adds no error for a rejected name, and pass 1 drops the errors of a declaration with a token error (DR1.1 to DR1.4).

### CR-3: A stray continuation byte takes no column, and the merge drops the next error

**Severity:** LOW
**Rule:** 2. A correct error report
**File:** `src/BrutalUi.Markup/Style/StyleTokenizer.cs` lines 263 to 280 (`SkipNonAscii`) and lines 380 to 405 (`Cursor.Advance`), and `src/BrutalUi.Markup/Style/StyleSheetParser.cs` lines 222 to 236 (the copy drop of the merge)

**Problem:** `Advance` adds no column for a byte `10xxxxxx`. That rule is right inside a valid code point. A stray continuation byte is not part of a code point, but it also adds no column. Thus the next character gets the same column. The merge then drops the error of that character as a copy, because the line, the column, and the kind are equal. Probe 12: the bytes `80 C3 A9 78` give `UnexpectedCharacter` at 1:1 for `0x80` only. The error for `é` is lost, and `x` is at column 2. An editor shows the stray byte as one replacement character (U+FFFD), so the editor column of each later token on the line is one higher than the reported column.

**Fix:** In `SkipNonAscii`, read the column before the skip, and set `cursor.Column = column + 1` after the skip. Thus each rejected sequence counts one column, as the replacement character of an editor does. A valid code point keeps its column. Add the rule to the remarks of `StyleTokenizer`, to section 11, and to `StyleParseError.Column`. Add a row to `AnInvalidUtf8ByteIsAnError` with `80 C3 A9`, and expect two errors at 1:1 and 1:2.

**Effort:** Low

**Resolved:** 2026-10-22 - `SkipNonAscii` sets the column after each rejected sequence to the start column plus 1, so a stray continuation byte takes one column, and `80 C3 A9` gives errors at 1:1 and 1:2 (DR2.3, DR2.4).

### CR-4: No test proves the brace count of the declaration skip

**Severity:** LOW
**Rule:** 7. Test quality
**File:** `src/BrutalUi.Markup/Style/TokenSkip.cs` lines 68 to 103 (`Declaration`), and `tests/BrutalUi.Markup.Tests/Style/ErrorReportTests.cs` (the recovery tests)

**Problem:** Section 11 and the remarks of `StyleSheetParser` say "Each skip counts the nested braces". Mutation M11 removes the `depth++` of the declaration skip, and the Markup tests give 0 failures of 550. The probe text `.a { color: { x } ; width: 1px; }` gives one error (`InvalidValue` at 1:13) with the shipped code. With M11, the skip stops at the inner `}`, the rule ends there, and the text gives a second error (`UnexpectedToken` at the `;`, 1:19). Thus the brace count is right now, but a later change can break it with no failed test.

**Fix:** Add a test to `ErrorReportTests` (next to `TheParserFindsAnErrorAfterABadDeclaration`): `.a { color: { x } ; width: 1px; }` gives exactly one error, `InvalidValue` at 1:13. Add a second row with a nested block that holds a `;` (`.a { color: { x; y } ; width: 1px; }`), so the `;` rule of the depth check is also proved. Run M11 again, and expect a failure.

**Effort:** Low

**Resolved:** 2026-10-22 - `TheDeclarationSkipCountsTheNestedBraces` (2 rows) proves one `InvalidValue` at 1:13 for both texts, and M11 (no `depth++` in `TokenSkip.Declaration`) fails both rows in a scratch copy (DR3.1).

### CR-5: `CompoundSelector.ToString` uses an exception for the fallback text

**Severity:** LOW
**Rule:** 5. API clarity
**File:** `src/BrutalUi.Core/Style/CompoundSelector.cs` lines 234 to 262 (`TypeText` and `ClassText`), `src/BrutalUi.Core/Tree/ElementType.cs` line 55, and `src/BrutalUi.Core/Tree/ClassName.cs` line 55

**Problem:** DS2.2 catches the throw of the name tables:

```csharp
try
{
    return ElementType.GetName(type);
}
catch (ArgumentOutOfRangeException)
{
    return "<type " + type.Value.ToString(CultureInfo.InvariantCulture) + ">";
}
```

`ToString` runs in test output, in assert messages, and in the debugger display. A debugger that stops on each thrown exception stops in `ToString` for an id outside a table. An exception is also the slow path of .NET. The two tables are in Core, in the same assembly as the selector, so the reason of DS2.2 ("Core has no `TryGetName`") is a small gap, not a limit.

**Fix:** Add `internal static bool TryGetName(ElementTypeId id, [NotNullWhen(true)] out string? name)` to `ElementType` and the same method for `ClassId` to `ClassName`. Each method takes the lock and checks the bounds. Let `GetName` call it and throw on `false`. Replace the two `try` blocks with calls of `TryGetName`. `AStateBitOutsideTheEnumGivesTheFallbackText` and the fallback rows of `SelectorEqualityTests` keep their text. Update DS2.2.

**Effort:** Low

**Resolved:** 2026-10-22 - `ElementType.TryGetName` and `ClassName.TryGetName` (internal) replace the two `try` blocks of `CompoundSelector.ToString`, and `GetName` throws on `false` (DR4.1).

### CR-6: A variable that no rule reads gets no value check, and the docs do not say it

**Severity:** LOW
**Rule:** 3. Theme variables at parse time (and 7. the docs)
**File:** `src/BrutalUi.Markup/Style/ThemeVariables.cs` lines 250 to 258, `src/BrutalUi.Markup/Style/StyleSheetParser.cs` lines 37 to 43 (the remarks), and `docs/technical-design.md` line 873 (**Theme variables.**)

**Problem:** Pass 1 checks a variable value only for `var(`, a function, and `!important` (DS4.9, DS5.9). The value readers check the value at each use. Thus `:root { --a: banana; } .x { color: #fff; }` gives a sheet with no error (probe 12). CSS has the same rule, so the behavior is acceptable. But an author who reads section 11 can expect a check of each variable, and a typo in an unused theme value stays hidden until a rule reads it. No doc states the rule, only DS4.9 of `context.md`.

**Fix:** Add one sentence to the **Theme variables.** paragraph of section 11 and to the remarks of `StyleSheetParser`: "The parser checks the value of a variable at each use, with the form of the property that reads it. A variable that no rule reads gets no value check, as in CSS." Add a row to `ThemeVariableTests` that asserts a sheet for this text, so the rule has a test.

**Effort:** Low

**Resolved:** 2026-10-22 - Section 11 and the remarks of `StyleSheetParser` state the rule, and `AVariableThatNoRuleReadsGetsNoValueCheck` asserts the sheet (DR1.5).

### CR-7: Doc text that does not match the code

**Severity:** LOW
**Rule:** 7. The docs
**File:** `docs/technical-design.md` lines 809 and 855, `src/BrutalUi.Markup/Style/StyleSheetParser.cs` line 33, `src/BrutalUi.Core/Style/CompoundSelector.cs` line 174, `src/BrutalUi.Core/Style/Selector.cs` line 369, and `src/BrutalUi.Markup/Style/PropertyTable.cs` line 59

**Problem:** Three texts state a rule that the code does not follow:

1. Section 11 says "A list of edges or corners needs whitespace or a comment between two values", and line 809 and the remarks of `StyleSheetParser` say "Whitespace and comments can come between each two tokens outside a compound". A comment alone gives no token (DS1.1), so `margin: 1px/**/2px` gives `InvalidValue` "'2px' touches the value before it" at 1:18 (probe 12). The same occurs for the parts of a transition (`opacity/**/1s`). The test `TheValuesAreSeparatedByWhitespaceAndComments` uses `2px /* c */ 3px`, with whitespace on both sides of the comment.
2. The remarks of `CompoundSelector.ToString` and `Selector.ToString` say "the parser reads the text back to an equal compound" with no limit. `ClassName.Intern` and `ElementType.Intern` take each text. Probe 11: the class `1a` gives `.1a`, which the parser reads as a number, and the name `a b` gives `#a b`, which reads back as a chain. The remarks state the limit for the name only ("no escape").
3. `ValueForm.Opacity` says "The range check comes in phase 5." Phase 5 added the check.

**Fix:** (1) Change the doc: "A list needs whitespace between two values. A comment alone does not separate two values." Put the same words in the transition bullet of section 11 and in the remarks of `StyleSheetParser`, and add the row `margin: 1px/**/2px` with `InvalidValue` at 1:18 to `AWrongValueInAListPointsAtThatValue`. If the CSS form must work, change `TryReadList` and `TryNextTransitionPart` to accept a byte gap between two tokens of the source, and keep the "touches" error for two tokens of one variable. (2) Change both remarks to "for each name that the grammar of section 11 can read (an identifier for a type or a class, `[A-Za-z0-9_-]+` for a name). Another name gives a text that the parser does not read back." (3) Change the summary to "One number from 0 to 1."

**Effort:** Low

**Resolved:** 2026-10-22 - The docs state that a comment alone does not separate two list values (section 11 and `StyleSheetParser`), the row `margin: 1px/**/2px` proves it, both `ToString` remarks and section 7 state the name limit, and the `ValueForm.Opacity` summary says "One number from 0 to 1." (DR4.2, DR4.3, DR4.4).

### CR-8: The size of `StyleRule` and the load cost of a parse have no record

**Severity:** LOW
**Rule:** 5. API clarity (Risk 8), and 7. the docs
**File:** `src/BrutalUi.Core/Style/StyleRule.cs` lines 26 to 30 (`Transitions`), `docs/technical-design.md` line 543 (**The cost.**), and `docs/features/style-system/style-parser/context.md` (`## Performance Considerations`)

**Problem:** Known item 4 still holds. The new `Transitions` field grows `StyleRule` from 320 to 368 bytes (probe 10). The field is 44 bytes, and the alignment adds 4. Section 7 gives the new bytes per element (1049.5) and names the 44-byte set, but no doc gives the rule size. The selector-matching review records 320 bytes, so a reader has no current value. The parse also allocates a large amount for each rule: 134 MiB for 50000 one-line rules (about 2.8 KB for each rule), and 193 MiB for the 20000-rule sheet of probe 10. Each `PendingRule` holds a copy of `StyleProperties` and of `StyleTransitions`, and the lists and the builder hold more copies. The parser runs at load time, so this is a fact to record, not a defect.

**Fix:** Add the rule size to the cost line of section 7: "`StyleRule` is 368 bytes (320 before style-parser)". Add one line to `## Performance Considerations` of `context.md`: "A parse allocates about 2.8 KB for each one-line rule, and a 3.8 MB sheet of 20000 rules parses in about 0.4 s." If a later feature loads very large sheets, it can build `StyleRule` values in place and skip the `PendingRule` copy.

**Effort:** Low

**Resolved:** 2026-10-22 - Section 7, the remarks of `StyleRule.Transitions`, and `## Performance Considerations` of `context.md` state the rule size (368 bytes, measured) and the parse cost.

---

## What's Already Good

- **No input throws.** Each skip and each look-ahead stops at the End token. M12 proves that the robustness tests catch a read past the End token. A text of 200000 open braces parses in 14 ms, because each skip is a loop and not a recursion.
- **A failed parse changes no global table.** The rule loop keeps token indexes, and `SelectorReader.Build` interns the names only after a parse with no error (DS5.11).
- **The map has one home for each order.** `MapEdges` and `MapCorners` hold the CSS order, and each mutation of a swap fails 5 or more tests.
- **Strong tests.** A shipped test kills 21 of the 22 mutations. The selector rejects, the position rule, the variable scope, the `var` position, the culture, each keyword table, the font id, and Conflict 2 each have a test that fails when the rule goes.
- **The frame loop is clean.** The parser code never runs in `Update`. The gate of a parsed sheet fails for a plant in `Selector.Matches` and in the match branch of `StylePass`, and `EqualsAllocatesNothing` fails for a plant in `Selector.EqualParts`.
- **The round trip holds for each name of the grammar.** `ToString` gives a text that parses to an equal selector for 20 forms, also names with `-`, `--`, and a leading digit after `#`.
- **The hand-off to transitions matches the code.** The five slots, the size of 44 bytes, the throw of `Set` and `Get`, the no-op `Clear`, the empty set of the old constructor, and the duration rule of the parser each match the source and the tests.
- **The sample fails early.** A bad sheet gives exit code 1 and positioned error lines before the window opens, with no GPU.

---

## Recommended Fix Order

| Order | Finding | Reason |
|-------|---------|--------|
| 1 | CR-2 | The message of a real error report is wrong. The fix touches pass 1 and pass 2, so do it before CR-1. |
| 2 | CR-1 | The only performance defect. It changes the drop code that CR-2 and CR-3 read. |
| 3 | CR-3 | The column rule changes a test row and section 11. |
| 4 | CR-4 | A test only. Do it after CR-1, so the new test runs on the new drop code. |
| 5 | CR-5 | A small Core change with no public API change. |
| 6 | CR-7 | Doc text, and an optional code change of the list read. |
| 7 | CR-6 | Doc text and one test row. |
| 8 | CR-8 | Doc text only. |

---

## Progress

- [x] CR-1: The drop check of DS5.5 is quadratic
- [x] CR-2: An error in a variable definition gives a second error at each use
- [x] CR-3: A stray continuation byte takes no column, and the merge drops the next error
- [x] CR-4: No test proves the brace count of the declaration skip
- [x] CR-5: `CompoundSelector.ToString` uses an exception for the fallback text
- [x] CR-6: A variable that no rule reads gets no value check, and the docs do not say it
- [x] CR-7: Doc text that does not match the code
- [x] CR-8: The size of `StyleRule` and the load cost of a parse have no record
