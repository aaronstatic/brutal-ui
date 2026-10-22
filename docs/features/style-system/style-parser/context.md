# style-system/style-parser - Context & Decisions

**Last Updated:** 2026-10-22 00:55
**Current Phase:** Done (phases 1 to 7 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan exists: `implementation.md` (7 phases, 54 tasks). The user accepted the recommended answer of each of the four open scope questions.
- ✅ `/start-feature` created `context.md`, `tasks.md`, and `inbox.md`.
- ✅ Phase 1, group 1 (tasks 1.1 to 1.5): the baseline, `InternalsVisibleTo`, `StyleParseError`, the tokens, the tokenizer, and the tokenizer tests. 1330 tests pass.
- ✅ Phase 1, group 2 (tasks 1.6 to 1.10): `StyleParseResult`, the color read, the phase 1 parser, and `TextSheetTests`. 1385 tests pass. The demo prints `panel #1E222CFF` and `plain #00000000`.

- ✅ Phase 2, group 1 (tasks 2.1 to 2.3, CR-4): the equality members and the CSS text form of `CompoundSelector` and `Selector`, and `SelectorEqualityTests`. 1428 tests pass.

- ✅ Phase 2, group 2 (tasks 2.4 to 2.7): `SelectorReader`, the selector lists in the parser, and `SelectorFormTests`. 1529 tests pass. The demo prints `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF`.

- ✅ Phase 3 (tasks 3.1 to 3.6): `PropertyTable`, the value forms, `CreateInfo.Fonts`, `PropertyFormTests`, and `EdgeAndCornerTests`. 1663 tests pass. The demo prints `panel 20,10,200,40`.

- ✅ Phase 4 (tasks 4.1 to 4.4): `ThemeVariables`, the `var(--name)` read in the parser, `ThemeVariableTests`, and the `:root` skip (DS2.12). 1695 tests pass. The demo prints `button #3060C0FF`, then `error 2:25 UnknownVariable`.

- ✅ Phase 5, group 1 (tasks 5.1 to 5.3): the recovery, the merge of the three error lists, the at-rule, `!important`, and function checks, the range checks, and the message rule. 1695 tests pass. The demo text gives `UnknownProperty` 2:3, `InvalidValue` 3:10, and `UnexpectedEnd` 4:1.

- ✅ Phase 5, group 2 (tasks 5.4 to 5.6): `ErrorReportTests`, `ParserRobustnessTests`, and the gate. 1770 tests pass. The demo prints `error 2:3 UnknownProperty`, `error 3:10 InvalidValue`, `error 4:1 UnexpectedEnd`, and `sheet null`.

- ✅ Phase 6, group 1 (tasks 6.1 to 6.4): `TransitionEasing`, `StyleTransition`, `StyleTransitions`, `StyleRule.Transitions`, the new `Add` overload, and `StyleTransitionsTests`. 1966 tests pass.

- ✅ Phase 6, group 2 (tasks 6.5 to 6.9): the `transition` entry, the transition read, the collect step of the parser, and `TransitionDeclarationTests`. 2021 tests pass. The demo prints `BackgroundColor 0.2s EaseOut`, then `Opacity none`.

- ✅ Phase 7, group 1 (tasks 7.1 to 7.8): `HelloScreen.css`, the sample loads the sheet, `ParsedSheetAllocationTests`, the plant, the smoke run, and the error path. 2023 tests pass. The smoke log holds `draw commands 2, vertices 128` and `Update and Draw 0`.

- ✅ Phase 7, group 2 (tasks 7.9 to 7.12): sections 7, 11, and 14 of the design doc, the two changelog lines, the two human test lines, the lint fixes, and `## Hand-offs`. 2023 tests pass.

- ✅ Phase 7 verified: the verifier gave PASS and Demo: PASS (the smoke run with `draw commands 2, vertices 128`).

- ✅ The cross-phase review: 8 findings (2 MEDIUM, 6 LOW). Review groups 1 to 5 fixed all 8. 2044 tests pass. The verifier `review-verify.md` gave PASS, the six test demos pass, and the smoke run passes.

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification" (the 7 slice demos, the error path, and the Windows run).
- 📋 `/autorun-epic` commits the feature, then builds style-system/transitions after the user answers the open scope questions of its draft plan (`docs/features/style-system/transitions/implementation.md`).

**Blockers:**
- None. Decision R2 settles DS1.20 (`BRUTALCORE0011` and the text overload): the plan stays.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Markup/Style/StyleSheetParser.cs` - the public parser (new)
- `src/BrutalUi.Markup/Style/StyleTokenizer.cs` - the UTF-8 tokenizer (new, internal)
- `src/BrutalUi.Markup/Style/SelectorReader.cs` - the selector grammar and the rejects (new, internal)
- `src/BrutalUi.Markup/Style/PropertyTable.cs` and `StyleValues.cs` - the 32 names and the value forms (new, internal)
- `src/BrutalUi.Markup/Style/ThemeVariables.cs` - pass 1 of the `:root` blocks (new, internal)
- `src/BrutalUi.Markup/Style/TokenSkip.cs` - the skip rules of the error recovery for both passes (new in phase 5, internal)
- `src/BrutalUi.Core/Style/StyleTransitions.cs` - the transition model of a rule (new)
- `src/BrutalUi.Core/Style/CompoundSelector.cs` and `Selector.cs` - the equality members and the CSS text form (CR-4)

### Related Files
- `docs/features/style-system/style-parser/implementation.md` - the implementation plan
- `docs/features/style-system/stylesheet-model/context.md` and `docs/features/style-system/selector-matching/context.md` - the hand-offs to this feature
- `samples/HelloPanel/HelloScreen.css` - the sample sheet (new in phase 7)

---

## Important Decisions

The plan holds Decisions 1 to 10 and the Confirmed Assumptions Q3, A3, and A9. This section records the decisions of the run.

### Decision R1: The user accepted the recommended answer of each open scope question
**Date:** 2026-10-21
**Context:** The draft plan lists four open scope questions. `/autorun-epic` passed the answers of the user.
**Decision:** Question 1: phase 1 uses the demo `ATextSheetPaintsAPanel`. Question 2: `CompoundSelector` and `Selector` get `IEquatable<T>`, `==`, `!=`, `GetHashCode`, and a CSS `ToString` in Core (CR-4). Question 3: `StyleTransitions` is a sparse set of five slots with a mask. Question 4: a font is a name of a font map in `StyleSheetParser.CreateInfo.Fonts`, and `default` gives the id 0.
**Rationale:** Each answer is the recommended option of the plan, so the plan does not change.
**Impact:** None beyond the plan.

### Decision R2: Keep the overload pair of `Parse` (DS1.20)
**Date:** 2026-10-21
**Context:** The BRUTAL analyzer `BRUTALCORE0011` gives an error for a constant string argument of a call that binds to `Parse(string, string)`. A call with a byte span as the first argument takes a literal source name with no error.
**Decision:** Keep `Parse(ReadOnlySpan<byte>, string)` and `Parse(string, string)` as the plan states. Do not rename the text overload.
**Rationale:** The plan is the source of truth, and the user accepted it. The analyzer steers a caller to the UTF-8 overload, and the project rules ask for UTF-8 text. The sample of phase 7 uses the byte overload. The run asks nothing, so the orchestrator takes the default that keeps the plan. The report names the choice, so the user can change it later.
**Impact:** A test passes a `u8` literal or a local. Section 11 of the design doc (task 7.9) states the analyzer rule and the `u8` form.

---

## Phase 1: Decisions

### Baseline (task 1.1)
- Base commit: `c2af58122c154b02e281ce94050c32da9f30d97e`.
- Build: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1238`, `failed: 0`, `succeeded: 1238`, `skipped: 0`.
- Facts: `facts: tree elements 1000, bytes 1046640, bytes per element 1046.6`.

### Group 1 result (tasks 1.1 to 1.5)
- Build: `0 Warning(s)`, `0 Error(s)`. Tests: `total: 1330`, `failed: 0` (1238 and 92 new rows in `StyleTokenizerTests`).
- `grep -nE 'PackageReference' src/BrutalUi.Markup/BrutalUi.Markup.csproj` prints nothing.
- Blast radius: new files in `BrutalUi.Markup` only, and one `InternalsVisibleTo` line. No Core file changes. No public type other than `StyleParseError` and `StyleErrorKind`.

### Decisions that the brief did not state (group 1)
- **DS1.1 A comment alone gives no token.** One run of whitespace bytes and comments gives one `Whitespace` token only when the run holds one whitespace byte or more. A run of comments only gives no token, as in CSS. Thus `./**/a` gives `Dot Ident`, and `a/**/b` gives `Ident Ident`. The token starts at the first byte of the run, which can be the `/*`. Reason: task 1.9 wants "comments between each token change nothing". With a `Whitespace` token for each comment, `./**/a` and `.a/**/.b` change their meaning.
- **DS1.2 The control byte rule.** Each byte from 0x00 to 0x1F other than tab, LF, FF, and CR, and the byte 0x7F, gives `UnexpectedCharacter`. Vertical tab (0x0B) is a control byte, not whitespace. FF is whitespace and does not end a line (the brief names only LF, CRLF, and CR).
- **DS1.3 A rejected byte gives no token.** The tokenizer records the error and skips the code point. Thus two `Whitespace` tokens can follow each other after an error (`a é b`). Each tokenizer error gives no sheet, so the parser never builds rules from such a list.
- **DS1.4 Invalid UTF-8.** The skip length is the `bytesConsumed` of `Rune.DecodeFromUtf8` (the bad subsequence, 1 byte or more, never an ASCII byte). The message names the byte in hex. A valid code point gets a message that quotes the character and its `U+` value. A BOM after offset 0 is an error.
- **DS1.5 The token text helpers.** `StyleToken` holds `NumberLength`, the byte length of the number part of a numeric token. `GetText`, `GetNumber`, and `GetUnit` slice the source span. `GetUnit` gives `%` for a percentage.
- **DS1.6 The error type.** The internal constructor throws for a `null` text and for a line or a column below 1. `Message` and `SourceName` give `""` for `default(StyleParseError)`. `ToString` reads the kind name from an explicit table (`StyleParseError.GetKindName`, no `Enum.ToString`). A value outside the enum gives its number.
- **DS1.7 The tokenizer API.** `internal static class StyleTokenizer` with `List<StyleToken> Tokenize(ReadOnlySpan<byte> utf8, string sourceName, List<StyleParseError> errors)`. The parser gives the source name, because each error holds it.
- **DS1.8 The number edge cases.** `1.` gives `Number Dot`. `1.5.5` gives two numbers. `1-2` gives two numbers. The format has no exponent: `1e3` is a `Dimension` with the unit `e3`. `1--x` is a `Dimension` with the unit `--x`. `#`, `-`, and `/` alone each give a `Delim`. `--` alone is an `Ident`.
- **DS1.9 An unterminated comment after whitespace.** The `Whitespace` token of the run runs to the end of the text. The `End` token follows it.
- **DS1.10 More tests than the brief.** `StyleTokenizerTests` also holds `ANumericTokenSplitsTheNumberAndTheUnit`, `EachTextGivesItsTokenList`, `EachLineBreakCountsOneTime`, `ABomAfterTheStartIsAnError`, `ACommentAloneGivesNoToken`, `AnInvalidUtf8ByteIsAnError`, `AControlByteOutsideACommentIsAnError`, `ACommentCanHoldEachByte`, `NoInputMakesTheTokenizerThrow` (2000 texts from `new Random(1234)`), `AnErrorTextHasTheSourceAndThePosition`, and `EachErrorKindHasItsName`. The last two give the public `StyleParseError` its main path test.

### Group 2 result (tasks 1.6 to 1.10)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1385`, `failed: 0`, `succeeded: 1385`, `skipped: 0`. The count is 1330, minus the placeholder test, plus 56 rows in `TextSheetTests`.
- Demo: `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetPaintsAPanel' --show-live-output on` prints these lines:
  - `panel #1E222CFF`
  - `plain #00000000`
- `grep -nE 'PackageReference' src/BrutalUi.Markup/BrutalUi.Markup.csproj` prints nothing.
- Blast radius: three new files in `BrutalUi.Markup` (`StyleParseResult.cs`, `StyleValues.cs`, `StyleSheetParser.cs`), one new test file, and the deleted `PlaceholderTests.cs`. No Core file and no group 1 file changes.

### Decisions that the brief did not state (group 2)
- **DS1.11 A token error skips the rule loop.** When the tokenizer reports an error, `Parse` gives the token errors only and does not read the rules. Reason: the errors stay in the order of the source, and the rule loop never reads a token list with a gap (DS1.3). Phase 5 can run the loop on such a list and merge the two error lists by position.
- **DS1.12 The End token gives `UnexpectedEnd` in phase 1.** The brief names `UnexpectedToken`, `UnknownProperty`, and `InvalidValue`. A text that ends inside a rule gives `UnexpectedEnd` at the `End` token in place of `UnexpectedToken`. Reason: the kind exists, its doc names this cause, and the phase 5 demo expects it at the `End` token. One helper (`RuleReader.Unexpected`) picks the kind.
- **DS1.13 The value errors.** An empty value (`color: ;` or `color: }`) gives `InvalidValue` at the `;` or the `}`. A second value token (`color: #fff #000;`) gives `InvalidValue` at the second token (a wrong count). A missing `:` gives `UnexpectedToken` at the token after the name. A token other than an identifier or `}` in a block gives `UnexpectedToken`. The name check comes before the `:` check, so `.a { colour #fff }` gives `UnknownProperty`.
- **DS1.14 The property names are case-sensitive.** The compare is ordinal, as the `PropertyTable` of phase 3 plans (`StringComparer.Ordinal`). `Color` gives `UnknownProperty`. The name map is `StyleSheetParser.TryGetProperty`, and the setter is `StyleSheetParser.SetColor`. Phase 3 replaces both with `PropertyTable`.
- **DS1.15 A class name goes into the global table only for a complete rule with a value.** The parser interns the name after the `}` of a rule that sets a value. Thus a failed parse or an empty rule adds no name to the process-wide class table.
- **DS1.16 The token text of a message.** `StyleValues.Describe` gives the token text in single quotes, `whitespace` for a whitespace token, or `the end of the text` for the `End` token. Each phase 1 message is a draft. Phase 5 writes the final text.
- **DS1.17 The result shape.** `StyleParseResult` has no public constructor. The internal factories `Success` and `Failure` keep the rule "a sheet or one error or more". `Errors` is a `ReadOnlyCollection` over a copy, so a cast cannot change it. `GetSheetOrThrow` throws a message that holds only the error lines, separated by LF, with no header line.
- **DS1.18 The text overload.** `Parse(string)` encodes with `Encoding.UTF8.GetBytes`. A `U+FEFF` at the start becomes the BOM bytes, and the tokenizer skips it. A lone surrogate becomes `EF BF BD` and gives `UnexpectedCharacter`. Both overloads throw `ArgumentNullException` for a `null` source name, and `Parse(string)` also for a `null` text.
- **DS1.19 `CreateInfo` holds nothing in phase 1.** The constructor with `in CreateInfo` stores nothing. Phase 3 adds `Fonts` and the copy.
- **DS1.20 `BRUTALCORE0011` and the string overload (open question).** The BRUTAL analyzer gives an error for each constant string argument of a call that binds to `Parse(string, string)`: the text literal, the source name literal, and `null`. A call with a byte span as the first argument takes a literal source name with no error. Thus game code that writes `parser.Parse(text, "hud.css")` does not compile, and it must write `parser.Parse(Encoding.UTF8.GetBytes(text), "hud.css")` or use a `u8` literal. The tests use `u8` literals or locals. Phase 7 uses the byte overload, so the sample does not see the error. Open question for the user: keep the overload pair as the plan states, or rename the text overload (for example `ParseText`). The API stays as the plan states until the user answers.
- **DS1.21 More tests than the brief.** `TextSheetTests` also holds `EachWrongColorIsAnInvalidValueAtItsToken` (13 rows), `GetSheetOrThrowGivesOneLineForEachError`, `ASuccessHasASheetAndNoError`, `AnEmptyTextGivesAnEmptySheet` (4 rows), `ARuleWithNoDeclarationGivesNoRule`, `TheTwoOverloadsGiveTheSameSheet`, `EachWrongFormGivesOnePositionedError` (18 rows), and `ANullArgumentThrows`. `EachColorFormGivesTheColor` has two more rows (`#a1B` and `#00000000`). `CommentsAndWhitespaceChangeNothing` also checks a byte text with a BOM through the span overload.

---

## Phase 2: Decisions

### Group 1 result (tasks 2.1 to 2.3, CR-4)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1428`, `failed: 0`, `succeeded: 1428`, `skipped: 0`. The count is 1385 and 43 new rows in `SelectorEqualityTests`. `StyleSheetTests` has no change and passes.
- Allocation gates: `EqualsAllocatesNothing` and `TheStylePassAllocatesNothingAfterWarmUp` pass (2 of 2), and `ThousandElementAllocationTests` passes (10 of 10). Each asserts 0 bytes.
- The acceptance grep of the match, the tiers, and the checks prints nothing. The two removed code lines are the two type declarations, which now name `IEquatable<T>`. The reflection grep prints nothing.
- Blast radius: two Core files get new public members only. No member, no line of `Matches`, `Specificity`, `Tiers`, or `ThrowIfWrong`, and no line of `StylePass.cs` changes. The default `ValueType.Equals` no longer runs for a selector, so each existing `Assert.Equal` of two selectors now uses the new ordinal equality. The result of each such compare stays the same, because the old reflection compare also used `string.Equals` for the name. `docs/technical-design.md` line 528 still says that `CompoundSelector` has no equality members. Task 7.9 replaces that line.

### Decisions that the brief did not state (group 1)
- **DS2.1 The operators take `in` parameters.** `==` and `!=` of both types take `in` parameters, because each value holds 24 bytes or more. A private `EqualParts(in T)` holds the compare, and `Equals(T)`, `Equals(object?)`, `==`, and `!=` call it. The name `EqualParts` avoids an overload of `Equals` that differs only by `in`.
- **DS2.2 The fallback text of an id outside a table.** `ElementType.GetName` and `ClassName.GetName` throw `ArgumentOutOfRangeException` for such an id. `ToString` catches that exception in two private helpers (`TypeText` and `ClassText`) and gives `<type N>` or `.<class N>`. Reason: the brief keeps the change inside the two selector files, and Core has no `TryGetName`. `ToString` runs for test output and debug output only, so the cost of the exception does not matter. A later `TryGetName` in the two tables can replace the `catch`.
  - Changed by CR-5: `ElementType` and `ClassName` have an `internal static bool TryGetName(id, [NotNullWhen(true)] out string? name)`. It takes the lock and checks the bounds. `GetName` calls it and throws on `false`. `TypeText` and `ClassText` call it, and the `catch` is gone (DR4.1).
- **DS2.3 The text of an unknown state bit.** The brief names no text for a state bit that is not a member of `PseudoStates`. An object initializer can set such a bit, and the factories reject it. `ToString` writes the known states first, in their order, and then one `:<state N>` with the value of all unknown bits for N (for example `.a:hover:checked:<state 32>`). Thus `ToString` never throws and never hides a bit.
- **DS2.4 The name has no escape.** `ToString` writes the name as it is after `#`. A name that the grammar cannot read (for example one with a space) gives a text that the parser does not read back. The round trip of group 2 uses names of the grammar only.
- **DS2.5 The hash code.** `CompoundSelector.GetHashCode` gives `HashCode.Combine(Type.Value, Class.Value, (int)States, nameHash)`, with the ordinal hash of the name or 0. `Selector.GetHashCode` gives `HashCode.Combine` of the two compound hashes and `(int)Combinator`. Each part is an `int`, so no call boxes.
- **DS2.6 `(empty)` follows `IsEmpty`.** `Selector.ToString` gives `(empty)` when the subject is empty, which is the rule of `Matches`. The public API can make no selector with an empty subject other than `default(Selector)`. An empty `CompoundSelector` gives `""`, as the brief states.
- **DS2.7 More tests than the brief.** `SelectorEqualityTests` also holds `EmptyCompoundsAreEqual`, `TheFactoryShortcutsGiveEqualSelectors`, `EqualsWithAnObjectChecksTheType`, `ACompoundGivesItsOwnText`, and `AStateBitOutsideTheEnumGivesTheFallbackText`. `ToStringGivesTheCssTextOfEachForm` has 15 rows: the 8 forms of section 7, `Label .b`, `.a:hover .b:focus`, `.a:hover > .b`, `Label.title#score:hover`, all five states, all parts, and `(empty)`. Each row also asserts the specificity of the form. `EqualsAllocatesNothing` measures `Equals(Selector)`, `CompoundSelector.Equals`, `==`, `!=`, and `GetHashCode`, 1000 times each, after 16 warm-up calls.

### Group 2 result (tasks 2.4 to 2.7)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1529`, `failed: 0`, `succeeded: 1529`, `skipped: 0`. The count is 1428 and 101 new rows in `SelectorFormTests`. `TextSheetTests` keeps its row count (DS2.8).
- Demo: `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCellFromText' --show-live-output on` prints these lines:
  - `row #00000000 cell #00000000`
  - `row #3060C0FF cell #FFCC00FF`
- Allocation gates: `EqualsAllocatesNothing` (1 of 1), `TheStylePassAllocatesNothingAfterWarmUp` (1 of 1), and `ThousandElementAllocationTests` (10 of 10) pass with 0 bytes.
- `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` prints nothing (exit 1).
- Blast radius: one new file in `BrutalUi.Markup` (`SelectorReader.cs`, internal), the rule read of `StyleSheetParser.cs`, one new test file, and two rows of `TextSheetTests.cs` (DS2.8). No Core file, no public API, and no project file changes.

### Decisions that the brief did not state (group 2)
- **DS2.8 Two rows of a phase 1 test change.** The full grammar gives a new meaning to two rows of `TextSheetTests.EachWrongFormGivesOnePositionedError`. `a { color: #fff; }` is now a valid type selector, so the row becomes `1 { color: #fff; }` (still `UnexpectedToken` at 1:1). `.a color: #fff; }` is now the descendant chain `.a color`, and the error moves to the whitespace after the `:` (`UnexpectedToken` at 1:10 in place of 1:4). A comment in the test names the change. Each other row and each other test of phase 1 passes with no change.
- **DS2.9 The kind of a token that cannot start a compound.** The reject table names `InvalidSelector` for an empty compound. The reader gives `InvalidSelector` when the token is `{`, `,`, or `>`, because these tokens end or link a selector. The `End` token gives `UnexpectedEnd` (DS1.12). `*`, `[`, `+`, and `~` give `UnsupportedSelector`. Each other token (a number, `;`, `}`, `)`, a lone `#`, `@`, and each other `Delim`) gives `UnexpectedToken`. Thus `.1 {` and a stray `}` keep the kind of phase 1. Phase 5 can add the at-rule check (`UnsupportedSyntax`) before this rule.
- **DS2.10 Parts that must touch.** A `.` with no identifier right after it, and a `:` with no identifier right after it, give `UnexpectedToken` at the next token (`UnexpectedEnd` at the `End` token). Examples: `. a` (1:2), `: hover` (1:2), `.a:#b` (1:4). A token after a compound that is not `{`, `,`, `>`, whitespace and a compound start, or one of the four unsupported bytes gives `UnexpectedToken` (`.a ;`, `.a )`).
- **DS2.11 The order of the state checks.** After `:`, the reader checks in this order: a second `:` (`UnsupportedSelector` at the second `:`), a token that is not an identifier (DS2.10), an identifier with `(` right after it (`UnsupportedSelector` at the identifier, also for `:hover(x)`), `root` (`InvalidSelector` at the `:`), and an unknown name (`UnknownPseudoState` at the identifier). The state compare is ordinal, so `:Hover` is unknown.
- **DS2.12 `:root` alone gives `UnsupportedSyntax` in this phase.** The brief asks for an error of my choice at the `:`. `StyleSheetParser` checks for `':' 'root'`, optional whitespace, and `'{'` at the start of a rule, before the selector read, and gives `UnsupportedSyntax` at the `:`. Each other `:root` goes to `SelectorReader`, which gives `InvalidSelector` at the `:`: in a chain, in a list, and also in a compound with other parts (`.a:root`, `:root:hover`). `:root` at the end of the text (no `{`) also gives `InvalidSelector`. Phase 4 replaces the check of `StartsRootBlock` with the skip of the block. The rows `:root { ... }` and `:root/* x */{ ... }` of `EachRejectedFormGivesAPositionedError` then change (hand-off to phase 4).
- **DS2.13 The third compound.** The error points at the first token of the third compound, also after a second `>` (`.a > .b > .c` gives 1:11, not the `>`). When that token cannot start a compound, the start rule of DS2.9 applies (`.a .b > {` gives `InvalidSelector` at the `{`). A `+`, `~`, or `*` in the place of the third compound gives `UnsupportedSelector` at that byte.
- **DS2.14 The reader gives token indexes, and the parser interns later.** `SelectorReader.ReadList` gives `SelectorSyntax` values (the token index of the type, the class, and the name, and the state bits). `SelectorReader.Build` interns the type names and the class names and calls the factories. The parser calls `Build` only after the `}` of a rule with a value. Thus DS1.15 holds for each name of a selector: a failed rule and an empty rule add no type name and no class name. A rule before the first error of a text can intern its names, as in phase 1. The ancestor names go into the tables before the subject names (the order of the source).
- **DS2.15 The reader shape.** `SelectorReader` is an internal `ref struct` that holds the source span, the token list, the source name, the error list, and its own index. The parser gives it the index and reads `Index` back after a successful read. The read stops at the `{` and does not read it. `RuleReader` keeps one `List<SelectorSyntax>` and clears it for each rule. The state name map is `SelectorReader.GetState` (ordinal, no `Enum.Parse`). The three name parts of a compound touch, so the reader never skips whitespace inside a compound.
- **DS2.16 More tests than the brief.** `SelectorFormTests` also holds `AListKeepsTheOrderOfTheSourceAtEqualSpecificity` (three classes in a list, written with no space and with a line break), and `AListWithNoDeclarationGivesNoRule`. `EachSelectorFormGivesTheFactorySelector` and `TheTextFormRoundTrips` have three more rows: `.a` LF `>` tab `.b`, `:hover:hover` (a repeat adds nothing), and `#title.a:checked` (the order of the parts is free). `EachRejectedFormGivesAPositionedError` has 46 rows: each line of the reject table and the extra cases of DS2.9 to DS2.13. `TheSpecificityComesFromTheSelector` has 12 rows: the 9 forms of the tier table, `:hover:hover`, all parts with all five states (65536 + 6 × 256 + 1), and `#a > #b` (2 × 65536). The demo also checks that the colors go back after the hover ends, with no third printed line.
- **DS2.17 A Label test needs a measurer.** A `Label` throws in the layout pass with no measurer. `SelectorFormTests` holds a private `FixedMeasurer : IUiRenderer` that draws nothing and measures each text as 10 by 10, so the Markup tests need no link to `RecordingRenderer` of `BrutalUi.Core.Tests`. `AnUnknownTypeMatchesNothingAndIsNoError` uses a private `Widget : Element` with the prefixed type name: the rule styles no `Element` and no `Label`, and it styles the `Widget`.

---

## Phase 3: Decisions

### Result (tasks 3.1 to 3.6)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1663`, `failed: 0`, `succeeded: 1663`, `skipped: 0`. The count is 1529 and 134 new rows (`PropertyFormTests` and `EdgeAndCornerTests`). One row of `TextSheetTests` changed (DS3.8).
- Demo: `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetLaysOutAPanel' --show-live-output on` prints `panel 20,10,200,40`.
- `grep -nE 'Enum\.(Parse|TryParse|GetNames|GetValues)|System\.Reflection|typeof\(' src/BrutalUi.Markup/Style/*.cs` prints nothing (exit 1).
- Blast radius: two new internal files in `BrutalUi.Markup` (`PropertyTable.cs`, `ValueError.cs`), the value reads of `StyleValues.cs`, the declaration loop and `CreateInfo.Fonts` of `StyleSheetParser.cs`, two new test files, and one row of `TextSheetTests.cs`. No Core file, no project file, and no package changes.

### Decisions that the brief did not state
- **DS3.1 The font map copy.** The parser copies `CreateInfo.Fonts` into an ordinal `Dictionary<string, FontHandle>` in both constructors. A later change of the caller map does not change the parser (a test proves it). A key `default` in the map wins over the built-in id 0, as the brief states. `default(CreateInfo)` and the parameterless constructor give an empty map.
- **DS3.2 The read contract and `ValueError`.** `StyleValues.TryRead(source, tokens, ref index, in entry, fonts, ref properties, out ValueError error)` reads the whole value of one declaration and writes it through the setter. `ValueError` is a new internal record struct in `ValueError.cs` (kind, token index, message), so the caller makes the error at the first token at fault. A one-value form moves `index` one token. A list form moves `index` to the `;`, the `}`, or the End token. The parser then skips whitespace and checks the end as in phase 1, so a second token after a one-value form gives `InvalidValue` at that token ("one value too many").
- **DS3.3 Forms that are narrower than a length.** `border-width`, `border-radius`, and `font-size` accept `px` and a bare `0` only in this phase. `%` and `auto` give `InvalidValue` now, because the form does not allow them. `margin` and `padding` accept `px`, `%`, `auto`, and `0`. A negative value, `padding: auto`, and the range of `opacity` and `flex-grow` stay open for phase 5 (task 5.2), as the brief states. Phase 5 adds the `padding: auto` check and the negative checks only.
- **DS3.4 A number that is not finite.** The read gives `InvalidValue` when a number does not fit a float (for example a text of 50 digits that gives infinity). Reason: `Length.Px(infinity)` gives `Length.Undefined` and would hide the error. Phase 5 can keep the check or move it.
- **DS3.5 The values of a list.** Whitespace (or a comment) must separate the values. `margin: 1px 2px3px` gives `InvalidValue` at `2px3px` (a unit `px3px` that the format does not know), and `50%10px` gives `InvalidValue` at the second value ("touches the value before it"). The fifth value gives `InvalidValue` at the fifth token with the text "takes 1 to 4 values".
- **DS3.6 A number form takes a bare number.** `flex-grow`, `flex-shrink`, and `opacity` accept a `Number` token only. `1px`, `50%`, and `auto` give `InvalidValue`. A bare number for a length (other than `0`) gives the message "has no unit. Add px or %".
- **DS3.7 The names.** The 32 names map one to one to the 32 members. `border-radius` maps to `StyleProperty.CornerRadius` and `font` to `StyleProperty.Font`. No name has no member, and no member has no name (a test proves it). A name is case-sensitive.
- **DS3.8 One row of a phase 1 test changes.** `TextSheetTests.EachWrongFormGivesOnePositionedError` used `width` as the unknown property. The format knows `width` now, so the row uses `size`. The kind, the line, and the column stay the same.
- **DS3.9 The reads allocate a string.** `PropertyTable.TryGet` and `TryFind` decode the token bytes to a `string` for the ordinal dictionary and the keyword compare. The parser runs at load time, so this is allowed. The sheet allocates nothing.
- **DS3.10 The font read.** A token that is not an identifier gives `InvalidValue`. An identifier that is not in the map and is not `default` gives `UnknownFont` at that token, and the message names the name and `StyleSheetParser.CreateInfo.Fonts`.
- **DS3.11 Hand-off to phase 6.** `ValueForm` is an internal enum and `StyleValues.TryRead` switches on it. Phase 6 adds the `Transition` member to the enum, a row to `PropertyTable.Entries` (the row ignores `Property`), and a case to the switch. The method `PropertyTable.TryFind` and `PropertyTable.List` work for the easing keywords too. The parser has no `RuleReader` state for a value that writes no member yet, so task 6.7 adds it.
- **DS3.12 More tests than the brief.** `PropertyFormTests` also holds `EachMemberHasOneRowAndOneName`, `TheKeywordTablesHaveTheirWords`, `AlignSelfAndAlignContentTakeTheAlignWords` (2 rows), `EachWrongFormIsAnInvalidValueAtTheFirstTokenAtFault` (23 rows), `ADeclarationWithNoValueIsAnError`, `AnUnknownPropertyIsStillAnError`, and `ThePropertyNamesAreCaseSensitive`. `NumbersReadInTheInvariantCulture` also reads `1.5px` and `2.25`. `EdgeAndCornerTests` also holds `TheValuesAreSeparatedByWhitespaceAndComments` (2 rows), `AnEdgeSetSetsAllFourEdgesAtOneTime`, and `AWrongValueInAListPointsAtThatValue` (4 rows). `AFifthValueIsAnError` has 5 rows.

---

## Phase 4: Decisions

### Result (tasks 4.1 to 4.4, and the follow-up of DS2.12)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1695`, `failed: 0`, `succeeded: 1695`, `skipped: 0`. The count is 1663 and 32 new rows in `ThemeVariableTests`. Two rows of `SelectorFormTests` changed (DS4.1).
- Demo: `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AThemeVariableColorsTheButton' --show-live-output on` prints these lines:
  - `button #3060C0FF`
  - `error 2:25 UnknownVariable`
- `grep -nE 'Enum\.(Parse|TryParse|GetNames|GetValues)|System\.Reflection|typeof\(' src/BrutalUi.Markup/Style/*.cs` prints nothing (exit 1).
- Blast radius: one new internal file in `BrutalUi.Markup` (`ThemeVariables.cs`), the value path and the `:root` skip of `StyleSheetParser.cs` (the `StartsRootBlock` check moved to `ThemeVariables`), one new test file, and two rows of `SelectorFormTests.cs`. No Core file, no project file, no package, and no `StyleValues.cs` change.

### Decisions that the brief did not state
- **DS4.1 The two `:root {` rows of `SelectorFormTests`.** The follow-up of DS2.12 says to change the kind. A block that holds a property is a variable error now, so the rows keep their text and give `InvalidVariable` at the property name: `:root { color: #fff; }` at 1:9, and `:root/* x */{ color: #fff; }` at 1:15. The comment above the rows names the new meaning.
- **DS4.2 Where a block starts.** Pass 1 walks the token list with a brace depth. A `:root` block starts at depth 0 when the token before it is the start of the text or a `}` (a rule start), and when `:root`, optional whitespace, and `{` follow. The same check (`ThemeVariables.StartsRootBlock`) runs in pass 2 at the start of each rule. A `:root` in any other place goes to `SelectorReader` and gives `InvalidSelector` at the `:` (phase 2 rule).
- **DS4.3 The value list.** For each declaration, `RuleReader.BuildValue` copies the tokens of the value into a reused `List<StyleToken>` up to the `;`, the `}`, or the End token, and puts the tokens of each `var(--name)` in place. The list ends with the terminator and the End token. A second reused list (`_map`) holds, for each entry, the index of the token that an error points at (the token itself, or the `var` token for a token from a variable). A third list (`_useName`) holds the index of the `--name` token of the use, or -1. `StyleValues` has no change: `TryRead` reads the value list, and the end check of the declaration reads it too. The loop index goes back to the token list through the map.
- **DS4.4 The message of a value error from a variable.** `FailValue` appends ` The value comes from the variable '--name'.` to the message of the reader. This also covers the "one value too many" error.
- **DS4.5 The positions of the variable errors.** `UnknownVariable` points at the `--name` token. `InvalidVariable` points at: the name of a `--name` declaration outside `:root`; the name of a declaration in `:root` that is not a variable (also a non-identifier token); the `;` or the `}` of an empty variable value; the `var` token of a `var(` inside a variable value; the `,` of a fallback; the token that is not a variable name in `var(` (also `var()`, which points at the `)`); and the token that is not `)` after the name. The End token inside `var(` or inside a block gives `UnexpectedEnd`. A missing `:` after a variable name gives `UnexpectedToken`.
- **DS4.6 A variable name needs a byte after `--`.** `IsVariableName` accepts an identifier that starts with `--` and has a length above 2. A lone `--` is not a variable name. In a declaration it gives `UnknownProperty`, as before.
- **DS4.7 Pass 1 keeps the first error and goes on.** A bad declaration in a block gives no variable. Pass 1 skips to the next `;` or `}` and goes on, so the map stays complete for the correct declarations. `ThemeVariables.Error` holds the first error only, as the rule loop does in this phase. `Parse` runs pass 2 in every case, and then reports the one error that comes first in the source (line, then column). Equal positions give the pass 1 error. Thus a text with a bad rule at line 1 and a bad block at line 10 reports line 1. Phase 5 replaces this with the merge of two error lists.
- **DS4.8 The skip of a block.** Pass 2 moves from the `:` to the first `}` (a block has no nested braces) and past it. A block that ends at the End token gives `UnexpectedEnd` at the End token. A `:root` block adds no rule and interns no name.
- **DS4.9 A variable that holds a variable.** `--a: var(--b);` gives `InvalidVariable`, as the brief states. A variable value is not checked for any other form in pass 1. The value readers check it at the use. A variable that no rule reads gives no error, also when its value is not a valid value.
- **DS4.10 `var` needs `(` right after it.** `var (--a)` is not a variable use. The value reader then sees the identifier `var` and gives its normal `InvalidValue`.
- **DS4.11 More tests than the brief.** `ThemeVariableTests` also holds `AValueErrorInAMixedValuePointsAtTheUse`, `ARootBlockGivesNoRule`, `TheLastSemicolonOfAVariableIsOptional`, `AVariableValueKeepsItsKeywordAndNumberForms`, and `TheFirstErrorOfTheSourceWins` (2 rows). `EachVariableErrorHasItsPosition` has 18 rows: the 6 forms of the brief and the extra cases of DS4.5. Three tests use a `u8` literal (R2).

---

## Phase 5: Decisions

### Group 1 result (tasks 5.1 to 5.3)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1695`, `failed: 0`, `succeeded: 1695`, `skipped: 0`. The count does not change. One test of phase 4 changed (see "Changed test rows").
- Demo text (a temporary local test, deleted after the run): `.p5panel {` + LF + `  colour: #fff;` + LF + `  width: 12em;` + LF gives these errors, and `Sheet == null`:
  - `hud.css(2,3): UnknownProperty: 'colour' is not a property of the format. Check the spelling, for example 'color'.`
  - `hud.css(3,10): InvalidValue: '12em' has a unit that the format does not know. Use px or % for width.`
  - `hud.css(4,1): UnexpectedEnd: The rule has no '}' before the end of the text. Write '}' to end the rule.`
- The temporary local test also ran 20000 texts from `new Random(1234)` (random bytes, and random mixes of 46 format pieces) and each cut of a valid sheet with a variable, a chain, a list, and each value family. No text threw. Each result with no sheet had one error or more, and the errors kept the order of the source.
- `grep -nE 'Enum\.(Parse|TryParse|GetNames|GetValues)|System\.Reflection|typeof\(' src/BrutalUi.Markup/Style/*.cs` prints nothing (exit 1).
- Blast radius: `BrutalUi.Markup` only. `StyleSheetParser.cs` (the recovery, the merge, the at-rule check, the deferred build), `ThemeVariables.cs` (each error, the statement walk, the checks of a variable value), `StyleValues.cs` (the range checks and `IsUnsupportedSyntax`), `SelectorReader.cs` and `StyleTokenizer.cs` (messages only), `StyleParseError.cs` (doc comments only), and the new internal file `TokenSkip.cs`. No public signature changes. The internal `ThemeVariables.Error` becomes `ThemeVariables.Errors`, and the internal `ThemeVariables.FindBlockEnd` becomes `TokenSkip.RootBlock`. No Core file, no project file, and no package changes.

### Changed test rows
- `ThemeVariableTests.TheFirstErrorOfTheSourceWins` (2 rows) becomes `TheErrorsOfBothPassesKeepTheOrderOfTheSource` (2 rows). Each row keeps the kind, the line, and the column of its first error, and asserts the second error too: row 1 gives `UnknownProperty` 1:6 and `InvalidVariable` 2:9, and row 2 gives `InvalidVariable` 1:9 and `UnknownProperty` 2:6. A comment names DS5.4. No other test of phases 1 to 4 changed.

### Decisions that the brief did not state
- **DS5.1 One set of skip rules for both passes.** The new internal file `TokenSkip.cs` holds `Block`, `Declaration`, `AtRule`, `RootBlock`, and `Statement`. Each skip counts the braces and stops at the End token. Pass 1 walks the top-level statements with `TokenSkip.Statement`. Pass 2 ends each statement at the same index, also after an error. Thus both passes find the same `:root` blocks. This replaces the depth walk of DS4.2. Example: `@import "a"; :root { --a: 1px; }` defines `--a`, because the at-rule ends at its `;` in both passes.
- **DS5.2 DS4.8 stays.** A `:root` block ends at its first `}` in both passes, with no nested skip. Pass 1 reports each error of a block, also `UnexpectedEnd`. Pass 2 skips the block and reports nothing for it. Inside a block, the declaration skip of pass 1 counts no braces. A `@` at the start of a declaration in a block gives `UnsupportedSyntax` (DS5.6).
- **DS5.3 `UnexpectedEnd` after a skip.** A selector-error skip or an at-rule skip that stops at the End token with an open `{` level gives `UnexpectedEnd` at the End token. A skip at the End token with no open level reports nothing more (for example `.a .b .c` with no `{`). A declaration skip that stops at the End token leaves the error to the block loop. The merge drops a second `UnexpectedEnd` at the same position (DS5.4), so one End token gives one `UnexpectedEnd`, with the message of the first error.
- **DS5.4 The merge.** `Parse` puts the token errors, the errors of pass 1, and the errors of pass 2 in one list in that order. A stable sort orders them by line, then column. The list order breaks a tie. The merge drops an error with the same line, column, and kind as the error before it (a copy). This replaces DS1.11 (the rule loop runs after a token error too) and DS4.7 (each error of each pass goes into the report).
- **DS5.5 The rule for a token error (the brief asks for a rule).** A rejected byte gives no token (DS1.3), so the rule loop can see a cut token. Three rules drop such a result error:
  - An `UnterminatedComment` drops each `UnexpectedEnd` of pass 1 and of pass 2. The comment runs to the end of the text, so each `}` after it is inside the comment.
  - A token error inside a declaration drops each rule-loop error of that declaration. The range starts at the first token of the declaration and ends after its `;`, or at the `}` or the End token that ends it. Example: `.a { colé: 1px; }` gives `UnexpectedCharacter` only, not `UnknownProperty` for `col`.
  - A token error from the first token of a rule up to the token after the selector error drops the selector error. Example: `.a:hové {` gives `UnexpectedCharacter` only, not `UnknownPseudoState` for `hov`. `.a.b é {` keeps both errors, because the `é` comes after the token after the error.
  - The rules do not apply to the errors of pass 1 and to an at-rule error. Each error that stays names a real second fault.
  - Changed by CR-2: the declaration rule now applies to pass 1 too. A token error inside a declaration of a `:root` block drops each pass 1 error of that declaration, and the definition is rejected (DR1.2). Pass 2 gives no `UnknownVariable` for a name with a rejected definition (DR1.1). The other rules do not apply to pass 1.
- **DS5.6 Where an at-rule is checked.** A `@` at the start of a top-level statement, at the start of a declaration in a rule block, and at the start of a declaration in a `:root` block gives `UnsupportedSyntax` at the `@`. A top-level at-rule skips with `TokenSkip.AtRule`. In a block, the declaration skip follows. The message names the at-rule (`'@' starts the at-rule '@media'.`) when an identifier touches the `@`. A `@` in a value or in a selector keeps its old kind (`InvalidValue` or `UnexpectedToken`).
- **DS5.7 `!important`.** A `!`, optional whitespace, and the identifier `important` in a value or in a variable value give `UnsupportedSyntax` at the `!`. Another `!` in a value keeps its value error. A `!` in a selector keeps `UnexpectedToken`.
- **DS5.8 A function other than `var`.** An identifier other than `var` with `(` right after it, in a value or in a variable value, gives `UnsupportedSyntax` at the identifier (`calc`, `rgb`, `rgba`, `url`, and each other name). In a selector, `:not(` keeps `UnsupportedSelector` (DS2.11), and `x(` keeps `UnexpectedToken` at the `(`. The check runs in `BuildValue`, before the value read, so each declaration gives one error: `width: 12em calc(1px);` reports `calc`, not `12em`.
- **DS5.9 Pass 1 checks the unsupported forms of a variable value.** This extends DS4.9. `--a: calc(1px);` and `--b: #fff !important;` give `UnsupportedSyntax` in pass 1, also when no rule reads the variable. The first `var(`, function, or `!important` of the value gives the one error, and the variable stays undefined.
- **DS5.10 The range checks.** The sign check uses `< 0`, so `-0px` and `-0` are correct. `margin` and the 11 length properties (`width` to `bottom`) keep their negative values. `padding` takes `px`, `%`, and `0`, with no `auto` and no negative value. `border-width`, `border-radius`, and `font-size` take `px` and `0`, with no negative value. `flex-grow` and `flex-shrink` take a number of 0 or more. For `opacity`, the range check (0 to 1) runs first, so `-0.1` gives the range message. `auto` where the form does not allow it gives `'auto' is not allowed for <name>.` The checks of DS3.3 and DS3.4 (the `%` of the narrow forms, a number that is not finite, and an unknown unit) stay.
- **DS5.11 A failed parse adds no name to the global tables.** The rule loop keeps each rule as a range of `SelectorSyntax` values and its `StyleProperties`. `Parse` builds the sheet and interns the names only when the merged list is empty. This replaces the note of DS2.14 that a rule before the first error can intern its names. DS1.15 still holds.
- **DS5.12 The message rule.** Each message holds `StyleValues.Describe` of the token at fault: the token text in single quotes, or `the end of the text` for the End token. Each `UnexpectedEnd` message holds `the end of the text`. Exceptions: a control byte and a byte that is not UTF-8 quote the byte in hex (`'0x01'`), and a character that is not ASCII quotes the character and its `U+` value. A value error from a variable points at the `var` token, and its message ends with `The value comes from 'var(--name)'.` (in place of the text of DS4.4). `UnknownVariable` quotes `'--name'`. The `:root` reject in a selector quotes `':'`. The second `.` of a compound, the `::`, the fallback `,`, the empty value, and the third compound got the quote in this phase. The text of an earlier message can change. The tests assert the kind, the line, and the column, as the brief states.
- **DS5.13 The reads stay inside the token list.** Each loop stops at the End token, and each look-ahead (`index + 1`, `index + 2`) starts at a token that is not the End token. The skips move at least one token when the start token is not a stop token, so the statement loop and the block loop always end.

### Hand-off to group 2 (tasks 5.4 to 5.6)
- The temporary test found no defect. Group 2 can use the rows of the local run: `@import "a";` (1:1), `@media x { .a {` (1:1 and `UnexpectedEnd` 1:16), `.a { color: #fff !important; }` (1:18), `.a { width: calc(1px); }` (1:13), `.a { color: rgb(1,2,3); }` (1:13), and `:root { --a: calc(1px); }` (1:14), each `UnsupportedSyntax`.
- For `EachMessageQuotesTheSourceText`, assert `Assert.Contains(text, message)` with the token text, or `the end of the text` for the End token (DS5.12).

### Group 2 result (tasks 5.4 to 5.6)
- Build: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1770`, `failed: 0`, `succeeded: 1770`, `skipped: 0`. The count is 1695 and 75 new rows: 73 in `ErrorReportTests` and 2 in `ParserRobustnessTests`. No earlier test changed.
- Demo: `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ASheetWithThreeMistakesReportsEachOne' --show-live-output on` prints these lines:
  - `error 2:3 UnknownProperty`
  - `error 3:10 InvalidValue`
  - `error 4:1 UnexpectedEnd`
  - `sheet null`
- `ParserRobustnessTests` passed in 3 runs of `--filter-class '*ParserRobustnessTests'` (2 of 2, 0 failed each run). Each run prints `texts 2000, sheets 31` and `cuts 1136, errors 1115, sheets 21`.
- `grep -nE 'Enum\.(Parse|TryParse|GetNames|GetValues)|System\.Reflection|typeof\(' src/BrutalUi.Markup/Style/*.cs` prints nothing (exit 1).
- Blast radius: two new test files, and one message line of `StyleTokenizer.cs` (DS5.16). No public signature, no Core file, no project file, and no package changes.

### Decisions that the brief did not state (group 2)
- **DS5.14 One row table for the two kind theories.** `ErrorReportTests` holds one private row table (the text, the kind, the line, the column, and the quote). The public `KindRows` gives it to `EachErrorKindHasItsPosition` and to `EachMessageQuotesTheSourceText`. Each row gives exactly one error (`Assert.Single`). The quote holds the single quotes (`'colour'`), or `the end of the text`. The table has 25 rows: 13 kinds, with 2 or more rows for `UnexpectedCharacter`, `UnexpectedToken`, `UnexpectedEnd`, `InvalidSelector`, `UnsupportedSelector`, `UnknownProperty` (also one on line 2), `InvalidValue`, and `UnsupportedSyntax` (`@import`, `@media` with a block, `!important`, `calc(` in a rule, and `calc(` in a `:root` block).
- **DS5.15 A guard for a new kind.** `TheKindRowsHoldEachKind` walks the values 0 to `UnsupportedSyntax` and asserts a row for each. It also asserts that `StyleParseError.GetKindName` gives `13` for the next value, so a new member of the enum fails the test until it gets a name and a row. No reflection.
- **DS5.16 Defect: the message of a byte that is not UTF-8 had no quotes.** DS5.12 states that the message quotes the byte in hex, but `StyleTokenizer.SkipNonAscii` wrote `The byte 0xFF is not valid ...`. The new test `EachMessageQuotesAByteThatIsNotUtf8` gives the bytes (a string row cannot hold them) and failed. The fix writes `The byte '0xFF' is not valid ...`. `StyleTokenizerTests.AnInvalidUtf8ByteIsAnError` asserts `0xFF` only and still passes.
- **DS5.17 The range theory.** `EachRangeCheckGivesAnError` has 17 rows. `opacity` gets two rows (above 1 and below 0) for its one check. The row for a number that is not finite uses `width` with 51 digits and `px`. Each row also asserts that the message quotes the token.
- **DS5.18 The recovery tests.** `TheParserFindsAnErrorAfterABadSelector` also checks that the skipped block of a bad rule gives no error, a bad selector in a list, and a bad state in an empty rule. `TheParserFindsAnErrorAfterABadDeclaration` also checks a bad last declaration with no `;` before the `}`.
- **DS5.19 The random texts.** Text `i` is random bytes (0 to 95 bytes, each of the 256 values) when `i % 3` is 0, a mix of 0 to 47 of 71 format pieces when it is 1, and such a mix with one random byte at a random place when it is 2. The check also asserts that a result never holds a sheet and an error, that each error has a line and a column of 1 or more, a message, the source name, and a kind of the enum, and that the errors keep the order of the source. A failure prints `seed 1234, text <i>` and the text with each byte outside printable ASCII as `\xHH`.
- **DS5.20 The cut rule.** The valid sheet has 10 units: a `:root` block, a top-level comment with `é`, and 8 rules with each selector form and each value family (also a variable use, a comment inside a rule, and a list of 3 selectors). A cut strictly inside a unit gives an error. A cut at the start or the end of a unit, or after the LF between two units, gives a sheet with the rules of the complete units. Thus the test expects 21 sheet cuts (2 for each unit and the cut after the last LF).

---

## Phase 6: Decisions

### Group 1 result (tasks 6.1 to 6.4)
- Task 6.1 search: `docs/features/style-system/transitions/` holds `requirements.md` only. `src/BrutalUi.Core` held no easing type and no transition type (the only match was a doc comment of `UiContext.Update` about `deltaTime`). Thus this group wrote the new file `src/BrutalUi.Core/Style/StyleTransitions.cs` with the three types.
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 1966`, `failed: 0`, `succeeded: 1966`, `skipped: 0`. `StyleTransitionsTests` holds 196 rows. No earlier test changed.
- Allocation: `TheStylePassAllocatesNothingAfterWarmUp` passes (1 of 1). `ThousandElementAllocationTests` passes (10 of 10). The new `TheSetCallsAllocateNothing` asserts 0 bytes for 1000 rounds of `Set`, `Get`, `IsSet`, `Clear`, and `CanTransition`.
- I4: the diff command of the brief on `StylePass.cs` prints nothing. The reflection grep on `src/BrutalUi.Core/Style/*.cs` prints nothing.
- Blast radius: `StyleRule` (one new field, one new constructor, and the old constructor calls the new one), `StyleSheet.Builder` (one new overload, and the old `Add` calls it), and the new file. `UiContext.RebuildCascade` and `StyleSheet.Builder.Build` copy whole `StyleRule` values and sort them with `Array.Sort(keys, rules)`, so the set travels with its rule and no sort key changes. `StylePass.cs` reads each rule through `ref readonly`. The parser of `BrutalUi.Markup` calls the old `Add` and does not change in this group. No project file and no package changes.

### Decisions that the brief did not state
- **DS6.1 `Clear` of a member outside the five is a no-op.** It does not throw. The set never holds a transition for such a member, and `IsSet` gives `false` for it, so the clear follows the rule "a clear of an unset member changes nothing" of `StyleProperties.Clear`. A caller can clear each member of a loop with no check. The doc comment of `Clear` states it.
- **DS6.2 A value that is not a member of `StyleProperty` follows the rule of a member outside the five.** `CanTransition` and `IsSet` give `false`, `Clear` changes nothing, and `Set` and `Get` throw `ArgumentOutOfRangeException` with the message of the five members. The tests use `(StyleProperty)32` and `(StyleProperty)255`.
- **DS6.3 The `StyleTransition` constructor throws for an easing that is not a member.** `(TransitionEasing)4` gives `ArgumentOutOfRangeException` with a message that names the four members. Thus the transitions feature can map each easing with a switch and no fallback branch. `default(StyleTransition)` holds 0 seconds and `Linear`. The constructor keeps each duration (0, a negative value, `NaN`, and infinity).
- **DS6.4 The mask bit of a slot.** The slot order is `BackgroundColor` 0, `BorderColor` 1, `CornerRadius` 2, `Opacity` 3, and `Color` 4, the order of the enum. One private `SlotOf` switch gives the slot. `Get` and `Set` use their own switch on the member and throw in the default branch. The order is private, and no public member shows it.
- **DS6.5 The size.** `StyleTransition` is 8 bytes (a float, a byte, and the padding). `StyleTransitions` is 44 bytes (five slots, one mask byte, and the padding). `TheSetHoldsNoHeapObject` asserts the size and `RuntimeHelpers.IsReferenceOrContainsReferences` (no reflection). A new slot changes the size and fails the test on purpose.
- **DS6.6 `StyleTransition` uses get-only auto-properties.** The brief names `Duration` and `Easing` as properties. `StyleRule.Transitions` is a `public readonly` field, as the other members of the rule. Each query member of `StyleTransitions` is `readonly`, so a read through the readonly field makes no defensive copy.
- **DS6.7 No equality members.** No test needs them. The tests compare `Duration` and `Easing`.
- **DS6.8 The guard of the theory.** `TheTheoryCoversEachMemberOfTheEnum` asserts that `StyleProperty.FontSize` is 31. A new member of the enum fails it until the 32-row theory gets a row for the new member.
- **DS6.9 The style test.** `ARuleWithATransitionStylesTheSame` styles a tree of four elements (a root, a panel, a label, and a plain child that inherits the color) with two sheets. The panel rule sets each of the five members and a transition for each one. The label rule sets a transition for `Color`. A private helper compares the 32 members of `ComputedStyle` one by one, with `object.Equals`, and names the element and the member in the failure message.
- **DS6.10 The sort test uses no type selector.** `CompoundSelector` has one class only, and a type id needs a control. The test adds a name rule, a class-and-hover rule, and three class rules (one with no transition). The sort gives the order 3, 4, 5, 2, 1, and each rule keeps its own duration.

### Hand-off to group 2 (tasks 6.5 to 6.9)
- Build each set with `default(StyleTransitions)` and `Set`. A later `Set` of the same member replaces the slot, so "a later declaration wins" needs no extra code.
- Call `StyleSheet.Builder.Add(in Selector, in StyleProperties, in StyleTransitions)`. The old `Add` gives an empty set.
- `Set` throws for a member outside the five. Check `StyleTransitions.CanTransition` before the call, and report `InvalidValue`.

### Group 2 result (tasks 6.5 to 6.9)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 2021`, `failed: 0`, `succeeded: 2021`, `skipped: 0`. The count is 1966 and 55 new rows in `TransitionDeclarationTests`. No earlier test changed.
- Demo: `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATransitionDeclarationIsStoredInTheRule' --show-live-output on` prints these lines:
  - `BackgroundColor 0.2s EaseOut`
  - `Opacity none`
- Allocation: `TheStylePassAllocatesNothingAfterWarmUp` passes (1 of 1). `ThousandElementAllocationTests` passes (10 of 10). Each asserts 0 bytes.
- Facts (task 1.1 line, Release): `facts: tree elements 1000, bytes 1049520, bytes per element 1049.5`. The baseline was 1046.6, so the value grows by 2.9 bytes per element. This group changes no Core type, so the growth comes from the larger `StyleRule` of group 1 (Risk 8).
- I4: `git diff --stat -- src/BrutalUi.Core/Style/StylePass.cs` prints nothing. The reflection grep on `src/BrutalUi.Markup/Style/*.cs` prints nothing (exit 1).
- Blast radius: `BrutalUi.Markup` only. `PropertyTable.cs` (the `Transition` member of `ValueForm`, the `TransitionEntry` row, and two word tables), `StyleValues.cs` (one new `ref` parameter of the internal `TryRead`, and the transition read), `StyleSheetParser.cs` (the set of each rule, `PendingRule.Transitions`, and the new `Add` overload), and one new test file. No public signature, no Core file, no project file, and no package changes. `TryRead` has one caller (`RuleReader.ReadDeclaration`).

### Decisions that the brief did not state (group 2)
- **DS6.11 The `transition` row is not a row of `Entries`.** `PropertyTable.TransitionEntry` is a separate static row, and `BuildIndex` adds it to the name map, so `TryGet` finds `transition` as each other name. `Entries` keeps one row for each of the 32 members, so `PropertyFormTests.EachMemberHasOneRowAndOneName` stays true with no change. This differs from the note "a row to `PropertyTable.Entries`" of DS3.11. The row holds `default(StyleProperty)` and the read ignores it.
- **DS6.12 `TryRead` gets a `ref StyleTransitions` parameter.** Only the `Transition` form writes it. The parser keeps one set for each rule, next to its `StyleProperties`, and `PendingRule` holds both. A rule with properties or transitions (or both) gives a rule.
- **DS6.13 A wrong value changes no slot.** The read fills a local copy of the set and writes it back only when the full list is correct, as the list forms of phase 3 do. Within one list, a later item for the same member replaces the slot. A later declaration replaces only the slots of its own members.
- **DS6.14 The duration forms.** `<number>s` and `<number>ms` (divided by 1000). A bare `0`, also `0.0` and `-0`, gives 0 seconds, as the brief states. `-0s` passes the sign check (DS5.10 uses `< 0`), and the slot gets +0, so the value prints as `0`. The unit is case-sensitive, as `px` is, so `1S` gives `InvalidValue`. A bare number other than 0 gives "has no unit". A percentage or an identifier gives "is not a duration". A number that a float cannot hold gives the float message (DS3.4).
- **DS6.15 The positions of a missing part.** The read skips whitespace after a part. When the next token is `;`, `}`, the End token, or `,`, the error points at that token (`opacity 1s;` at the `;`, `opacity, color 1s linear` at the `,`, `opacity 1s }` at the `}`). A part that touches the part before it gives `InvalidValue` at the touching token (DS3.5). After the easing, a `,` starts the next item, and `;`, `}`, or the End token ends the value. Each other token gives "one part too many" at that token (this covers a delay such as `0.5s`), or "touches the easing before it" when no whitespace comes before it. An empty item after a comma (`opacity 1s linear,` or `, ,`) gives `InvalidValue` at the first token after the comma and its whitespace.
- **DS6.16 The out-of-scope words.** `none`, `all`, `width`, and a name in another case (`Opacity`) give the member error at the token, with the message that names the five. `ease` and `EaseOut` give the easing error, with the message that names the four. A delay gives "one part too many".
- **DS6.17 More tests than the brief.** `TransitionDeclarationTests` also holds `EachDurationFormGivesItsSeconds` (7 rows: `0.2s`, `2s`, `0s`, `0`, `0.0`, `-0s`, `-0`, each also asserts +0), `TheRulesOfAListGetTheSameTransitions`, `ARuleWithNoTransitionHasAnEmptySet`, `AMemberOutsideTheFiveGivesAMessageThatNamesTheFive`, `AMissingPartBeforeTheBracePointsAtTheBrace`, and `AWrongDurationFromAVariablePointsAtTheUse` (the error points at the `var` token, DS5.12). `MillisecondsGiveSeconds` has 4 rows (`200ms`, `1500ms`, `0ms`, `0.5ms`). `ALaterDeclarationWinsForTheSameMember` also checks a repeat inside one list. `AVariableCanHoldTheDuration` also reads a full list from a variable that the text defines after the use. `EachWrongTransitionGivesAPositionedError` has 26 rows: the 6 rows of the brief and the cases of DS6.14 to DS6.16. It prints each error line.

---

## Phase 7: Decisions

### Group 1 result (tasks 7.1 to 7.8)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 2023`, `failed: 0`, `succeeded: 2023`, `skipped: 0`. The count is 2021 and the 2 tests of `ParsedSheetAllocationTests`. No earlier test changed. The new class passed in 3 runs of `--filter-class '*ParsedSheetAllocationTests'` (2 of 2 each run).
- `grep -n 'Inline\.' samples/HelloPanel/HelloScreen.cs` prints nothing.
- The sheet values: each value of `HelloScreen.css` equals the old constant of `HelloScreen.cs` (24, 16, 2, 8, 26, 12, `#1e222c` = 30,34,44, `#608cdc` = 96,140,220, `#ffffff`). No value differs from the text of task 7.1. One edge difference: see DS7.1.
- Blast radius: the sample (`HelloScreen.css` new, `HelloPanel.csproj`, `HelloScreen.cs`, `Program.cs`), the Markup test project file, and one new test file. The sample now references `BrutalUi.Markup`, so its output holds `BrutalUi.Markup.dll` and `HelloScreen.css`. No `src/` file changes (the plant of task 7.7 is restored). No package and no `Directory.Packages.props` change.

### The plant (task 7.7)
- The copy: `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/1041e7b1-7770-4054-8b67-c6d8d936fcf3/scratchpad/Selector.cs`. The plant: `GC.KeepAlive(new object());` as the first line of `Selector.Matches(Element, ElementTypeId)`.
- Planted run: `exit=2`. `total: 2023`, `failed: 5`. The failed tests:
  - `BrutalUi.Markup.Tests.Style.ParsedSheetAllocationTests.AParsedSheetAllocatesNothingInUpdate` (Expected 0, Actual 208800)
  - `BrutalUi.Core.Tests.StylePassTests.TheStylePassAllocatesNothingAfterWarmUp`
  - `BrutalUi.Core.Tests.SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing`
  - `BrutalUi.Core.Tests.ThousandElementAllocationTests.UpdateAllocatesNothingAfterWarmUp`
  - `BrutalUi.Core.Tests.ThousandElementAllocationTests.DrawAllocatesNothingAfterWarmUp`
- Restore: `cp` from the copy. `diff` of the copy and the file prints nothing (exit 0). The build after the restore gives `0 Warning(s)`, `0 Error(s)`. The test run after the restore gives `exit=0`, `total: 2023`, `failed: 0`.

### The smoke run and the error path (task 7.8)
- `scripts/test-linux.sh; echo "exit=$?"` prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 14 s` and `exit=0`.
- `artifacts/test-linux/smoke.log`:
  - `HelloPanel: 120 frames rendered.`
  - `HelloPanel: last frame: draw commands 2, vertices 128.`
  - `HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.`
- Error path: the bin copy holds `.hello-panel { colour: #fff; }`. `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 1` gives `exit=1`, nothing on standard output, and 5 lines on standard error. Line 1: `HelloPanel: System.InvalidOperationException: HelloScreen.css(1,16): UnknownProperty: 'colour' is not a property of the format. Check the spelling, for example 'color'.` Lines 2 to 5 are the stack trace (`GetSheetOrThrow`, `LoadSheet`, `Run`, `Main`).
- Restore: `dotnet build brutal-ui.sln` alone did not restore the copy (see DS7.6). After `touch samples/HelloPanel/HelloScreen.css` and the build, `cmp` of the two files prints nothing (exit 0).

### Decisions that the brief did not state
- **DS7.1 The three other edges of the image margin.** `margin: 12px 0 0` gives `12px` on the top edge and `0px` on the right, bottom, and left edges. The old inline write (`Margin with { Top = 12 }`) left those three edges at `Length.Undefined`. The layout reads an undefined margin as 0 (Yoga rule), so the position and the size do not change. The smoke run keeps `draw commands 2, vertices 128`. `TheHelloPanelSheetGivesTheOldValues` asserts `0px` on the three edges and names the difference in a comment.
- **DS7.2 The sheet loads before `Glfw.Init`.** `Run` calls `HelloScreen.LoadSheet()` as its first line, before the GLFW start. Thus a bad sheet needs no display and no Vulkan loader. `RunWindow` and `RunImGui` get a `StyleSheet` parameter. `RunImGui` calls `ui.AddSheet(sheet)` right before `ui.AddRoot`.
- **DS7.3 The error output keeps the existing `catch`.** The `catch` of `Main` writes `HelloPanel: {exception}`, so standard error holds the exception type, the error lines, and the stack trace. The brief asks for the existing `catch`, so the sample adds no new handler. A later change can write `exception.Message` only for an `InvalidOperationException` of the parser.
- **DS7.4 One constant for the file name.** `HelloScreen.SheetFileName` (`HelloScreen.css`) gives the file path and the source name of the parse. The two `ClassId` fields (`RootClass`, `PanelClass`) intern `hello-root` and `hello-panel` in their static initializers.
- **DS7.5 The gate scene.** The tree is a root, 7 panels, and 6 items under each panel (50 elements). The first item of each panel is a `Label`, so the type selector and the font family reach an element. The test holds a private `FixedMeasurer` (a copy of the one of `SelectorFormTests`, DS2.17). The text has 10 rules: a `:root` block with two variables (a color and a length), a list (`.root, .unused`), a class with an edge list, a corner list, a variable in a mixed margin, and a transition list, a state, a child chain with a state, a descendant chain, a type, a type with a class, a name, and a child chain with a name. The value families: keywords, `px`, `%`, `auto`, edges, corners, a number, a color, `font: default`, and a transition list. The named element is the second item of the first panel. Each step sets the name B on an even step and the name A on an odd step.
- **DS7.6 `PreserveNewest` does not restore a newer copy.** MSBuild copies the sheet only when the source is newer than the copy. The write of the error path makes the copy newer, so the build of task 7.8 left the bad copy in place (`cmp`: differ at byte 1). `touch samples/HelloPanel/HelloScreen.css` and a second build restored it. The project file keeps `PreserveNewest`, as the plan states. Hand-off to task 7.11: the human test of the error path must end with `touch samples/HelloPanel/HelloScreen.css` (or a delete of the bin copy) before the build.
- **DS7.7 The gate checks both name rules.** After the measured steps, the test checks the tree (the name A of step 115 gives `10px`). Then it runs one more step outside the measure (the name B) and checks again (`20%`). The extra step is not measured.
- **DS7.8 More checks than the brief.** `TheHelloPanelSheetGivesTheOldValues` also asserts that the sheet has 4 rules, that each rule sets exactly its members of task 7.1 (a loop over the 32 members), that the panel rule has no transition, and that a tree with the classes of `Build` gets each old value in its `ComputedStyle`.
- **DS7.9 The shared gate namespace.** `AllocationGate` is in the namespace `BrutalUi.Core.Tests`, so the new test file has `using BrutalUi.Core.Tests;`.

### Decisions that the brief did not state (group 2)
- **DS7.10 Section 11 holds the full format and the API.** Section 11 holds the public API of the parser as a code block, the 13 kinds as a table with their causes, and the selector reject table. The phase 2 rejects `.a:root` and `:root:hover` (DS2.12) join the `:root` row. The property table states the range of each name (DS3.3, DS5.10) in place of the phase notes of the plan.
- **DS7.11 Section 7 holds the full model of the new types.** The model block shows `TransitionEasing`, `StyleTransition`, `StyleTransitions`, the new `StyleRule` field and constructor, and the new `Add` overload. Two new paragraphs ("The equality and the text form", "The transition set") follow the model text. The design decision of DS1.7 of selector-matching becomes a line about the new equality members. A second new line names the sparse set of five slots.
- **DS7.12 The cost line names the cause of the growth.** The cost line of section 7 gives 1049.5 bytes per element (task 6.9) and the old value 1046.6. The growth comes from the 44-byte set in each rule and the rule copies of the sheet and the cascade (blast radius of phase 6, group 1). The facts command did not run again in this group, because group 2 changes no code.
- **DS7.13 Section 14 gets two new bullets and two sentences.** One bullet names the 10 test classes of `BrutalUi.Markup.Tests`, and one bullet holds the gate and the plant result of task 7.7. The style test bullet gets one sentence for each of `SelectorEqualityTests` and `StyleTransitionsTests`. The HelloPanel bullet gets one sentence for the error path (exit code 1 before the window opens).
- **DS7.14 The changelog `### Changed` line names the error path.** The line also states that a bad sheet gives exit code 1 before the window opens, because this is a new exit path of the sample.
- **DS7.15 The error path line of `human-testing.md` starts with a build.** The bin copy exists only after a build, so the line starts with `dotnet build brutal-ui.sln`. It ends with the `touch` step of DS7.6.
- **DS7.16 The lint hits that stay.** Each changed doc exits 0 with `--fail-over 2.5`. The em dash of the title of `human-testing.md` is an old line and stays. The heading "Definition of Done" of `implementation.md` is a fixed heading name of the plan template and stays. The plan got lint fixes in nine lines only (one passive, eight nominalizations). The task text of `tasks.md` for 7.11 and 7.12 stays as it is, and the brief of group 2 governs.

### Group 2 result (tasks 7.9 to 7.12)
- Build: `0 Warning(s)`, `0 Error(s)`. Tests: `total: 2023`, `failed: 0`, `succeeded: 2023`, `skipped: 0`.
- Lint (`--fail-over 2.5`, exit 0 each): `technical-design.md` 1.29, `CHANGELOG.md` 1.38, `human-testing.md` 0.95, `implementation.md` 1.44, `context.md` 2.04, `tasks.md` 2.06 (hits per 100 words).
- `human-testing.md` holds no duplicate line. `CHANGELOG.md` names `style-system/style-parser` in the two new lines.
- Blast radius: docs only. No code, project, or package file changes.

---

## Review Group 1: Decisions

### Result (CR-2 and CR-6)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 2037`, `failed: 0`, `succeeded: 2037`, `skipped: 0`. The count is 2023 and 14 new rows in `ThemeVariableTests`. `ParserRobustnessTests` passes (2 of 2).
- Lint (`--fail-over 2.5`, exit 0): `technical-design.md` 1.29 hits per 100 words.
- Blast radius: `ThemeVariables.cs` (the rejected set, `IsRejected`, the token error check, and the declaration read in its own method), `StyleSheetParser.cs` (one more argument of `ThemeVariables.Read`, the `IsRejected` check in `ExpandVariable`, `ComparePosition` becomes `internal`, and doc comments), `ThemeVariableTests.cs` (14 new rows), section 11 of `technical-design.md`, and `code-review.md`. `ThemeVariables.Read` has one caller (`Parse`). No public signature, no Core file, no project file, and no package changes. No existing test row changed: no row asserted a second `UnknownVariable`.

### Decisions
- **DR1.1 Which definitions reject a name.** Each error after a valid variable name rejects the name: a missing `:` (`UnexpectedToken`), the End token after the name or in the value (`UnexpectedEnd`), an empty value, a `var(`, a function, `!important`, and a token error inside the declaration. A use of a rejected name gives no error, and `ExpandVariable` returns `false`, so the declaration gives no value. The text has the definition error (or the token error), so it gives no sheet. A name with a rejected definition and a correct definition resolves to the correct value (`TryGet` runs first). Example: `:root { --a: ; --a: #fff; }` gives one error, and the use resolves.
- **DR1.2 The token error rule of pass 1.** The range is the range of the declaration rule of DS5.5: from the first token of the declaration up to its `;` (included), or up to the `}` or the End token that ends it. A token error in the range drops each pass 1 error of the declaration, and the definition is rejected also when it has no other error. Thus `:root { --a: #ffé; }` with a use gives one `UnexpectedCharacter` and no `InvalidValue` at the use for the cut value `#ff`. A cut name (`--é` gives the token `--`) is not a variable name, so nothing goes into the rejected set. A use `var(--é)` is cut too, and DS5.5 drops its pass 2 error.
- **DR1.3 The check of pass 1 is linear.** The declarations of the blocks come in the order of the source, and the tokenizer adds the token errors in the order of the source. Thus one moving index (`_tokenErrorIndex`) skips each token error before the declaration one time. The new check adds no quadratic path (compare CR-1). The pass 2 drop code does not change in this group.
- **DR1.4 The block end after a dropped declaration error.** A declaration that stops at the End token records its own `UnexpectedEnd`, and the block loop adds no copy. When a token error drops that `UnexpectedEnd`, the block loop records the `UnexpectedEnd` of the block, as the rule loop of pass 2 does. `:root { --a: 1pé` gives `UnexpectedCharacter` 1:16 and `UnexpectedEnd` 1:17 (the missing `}` is a real second fault).
- **DR1.5 The text of CR-6.** The two sentences of the brief go into the **Theme variables.** paragraph of section 11 and into the remarks of `StyleSheetParser`. The remarks also get one sentence for CR-2. Section 11 gets a bullet for CR-2 under the paragraph, next to the `UnknownVariable` bullet, and the `UnknownVariable` row of the kind table gets one sentence. `AVariableThatNoRuleReadsGetsNoValueCheck` also asserts that the same value gives `InvalidValue` at the use when a rule reads it.
- **DR1.6 Not changed: a `--name` declaration outside `:root`.** `.a { --x: 1px; } .b { color: var(--x); }` still gives `InvalidVariable` and then `UnknownVariable`. The second message names the right fix (define the name in a `:root` block), so it is not a result of the first error. The brief scope does not include it.

### New test rows
- `ADefinitionErrorGivesNoErrorAtTheUse` (8 rows): `calc(`, an empty value, `var(` (the three rows of the brief), `calc(` with three uses in two rules, a missing `:`, the End token in the value and after the name (with a use before the block), and a rejected definition before a correct one.
- `ATokenErrorInADefinitionGivesOneError` (4 rows): `:root{--é:1px;}` (the row of the brief, 1:9), a cut name after a correct definition, a cut color value, and a cut function name.
- `ATokenErrorInADefinitionKeepsTheErrorOfTheBlockEnd` (DR1.4) and `AVariableThatNoRuleReadsGetsNoValueCheck` (CR-6).

---

## Review Group 2: Decisions

### Result (CR-1 and CR-3)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 2041`, `failed: 0`, `succeeded: 2041`. The count is 2037 and 4 new rows: 2 in `ParserRobustnessTests` and 2 in `StyleTokenizerTests`. One existing row changed (see "Changed test rows").
- `ParserRobustnessTests` passed in 3 runs of `--filter-class '*ParserRobustnessTests'` (4 of 4, 0 failed each run).
- Lint (`--fail-over 2.5`, exit 0): `technical-design.md` 1.30 hits per 100 words.
- Blast radius: `StyleSheetParser.cs` (`DropAtTokenError` and the new private `FirstTokenErrorAtOrAfter`), `StyleTokenizer.cs` (`SkipNonAscii` and the remarks), `StyleParseError.cs` (the `Column` doc comment only), section 11 of `technical-design.md`, two test files, and `code-review.md`. `DropAtTokenError` has two callers (`DropDeclarationErrorsAtTokenError` and `DropSelectorErrorAtTokenError`), and their ranges do not change. The column change reaches each error and each token after a stray continuation byte on the same line, outside a comment. Pass 1 (`ThemeVariables.HasTokenError`) reads the same sorted token error list and needs no change. No public signature, no Core file, no project file, and no package changes.

### The time of 50000 copies (Debug build, this machine)
| Text | Before the fix | After the fix |
|------|----------------|---------------|
| 50000 copies of `.a{cé:1px;}` (600000 bytes, 50000 errors) | 20130 ms | 403 ms (then 172, 182, and 386 ms in the 3 runs) |
| 50000 copies of `.a:hé{}` (400000 bytes, 50000 errors) | 21609 ms | 272 ms (then 135, 158, and 293 ms in the 3 runs) |

### Decisions
- **DR2.1 A binary search, not a moving index.** `DropAtTokenError` finds the first token error at or after the lower position with a binary search (`FirstTokenErrorAtOrAfter`), and compares only that error with the upper position. The tokenizer adds the token errors in the order of the source, so the list is sorted. The result is the same as the old full walk: when a token error lies in the range, the first error at or after the lower position also lies in it. A binary search keeps the methods of `RuleReader` `readonly` and needs no state, so it stays correct if a later change reads a range out of order. Each call takes O(log n) time. The test asserts the error count and no time bound, as the brief prefers. The table above holds the times.
- **DR2.2 The new test rows of CR-1.** `ManyTokenErrorsParseInLinearTime` is a theory with two rows: `.a{cé:1px;}` (the declaration rule of DS5.5) and `.a:hé{}` (the selector rule). Each row parses 50000 copies, and asserts no sheet, 50000 errors, and the kind `UnexpectedCharacter` for each error. The test writes the time to the test output.
- **DR2.3 The column of a rejected sequence.** `SkipNonAscii` reads the column before the skip, and sets `cursor.Column = column + 1` after the skip. Thus each rejected sequence counts one column, as the replacement character (U+FFFD) of an editor does. A valid code point and a cut code point counted one column before, and they do not change. Only a stray continuation byte (and a run of them, because the decoder rejects each one alone) changes: it now takes one column. The skipped bytes are 0x80 or more, so the line does not change. In a comment, the rule does not apply, and a continuation byte adds no column, because the comment gives no error and no token position inside it.
- **DR2.4 The new test rows of CR-3.** `AnInvalidUtf8ByteIsAnError` becomes a theory. Row 2 is `80 C3 A9` (the row of the brief): two errors at 1:1 and 1:2, no token, and the End token at column 3. Row 3 adds `x` (the bytes of probe 12): `x` is at column 3. Each row also parses the bytes and asserts that the merge keeps each `UnexpectedCharacter` error. Row 3 also gives a selector error at the End token (1:4), a real second fault, so the parser check reads the `UnexpectedCharacter` errors only.

### Changed test rows
- `StyleTokenizerTests.AnInvalidUtf8ByteIsAnError` (a fact, now row 1 of the theory): the bytes `a FF 80 b C3` gave the columns 2, 3, and 4. They now give 2, 3, and 5, because the stray `0x80` takes column 3 and `b` moves to column 4. The row also asserts the End token at column 6. The assert of `0xFF` in the first message, the kinds `Ident Ident`, and the End offset 5 stay.

---

## Review Group 3: Decisions

### Result (CR-4)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 2043`, `failed: 0`, `succeeded: 2043`. The count is 2041 and 2 new rows in `ErrorReportTests`.
- Blast radius: one test file (`ErrorReportTests.cs`) and `code-review.md`. No change in `src/`.

### Decisions
- **DR3.1 The mutation result.** `TheDeclarationSkipCountsTheNestedBraces` is a theory with two rows: `.a { color: { x } ; width: 1px; }` and `.a { color: { x; y } ; width: 1px; }`. Each row gives one error, `InvalidValue` at 1:13, in the repository (the shipped code, after groups 1 and 2, gives the position and the count of the brief). In a scratch copy, M11 (the `depth++` of `TokenSkip.Declaration` commented out) fails both rows (2 of 2 failed). Row 2 fails because the `;` after `x` then ends the skip. The depth check of the `;` is proved by row 2 with the correct code: the `;` inside the block does not end the skip. The repository `TokenSkip.cs` did not change.

---

## Review Group 4: Decisions

### Result (CR-5, CR-7)
- Build: `0 Warning(s)`, `0 Error(s)`.
- Tests: `total: 2044`, `failed: 0`, `succeeded: 2044`. The count is 2043 and 1 new row in `AWrongValueInAListPointsAtThatValue`. `EqualsAllocatesNothing` and the Core allocation gates pass.
- `grep -n 'catch' CompoundSelector.cs` prints nothing. `StylePass.cs` did not change.

### Decisions
- **DR4.1 The internal `TryGetName`.** `ElementType.TryGetName(ElementTypeId, out string?)` and `ClassName.TryGetName(ClassId, out string?)` are `internal static`, with `[NotNullWhen(true)]`. Each takes the lock and checks the bounds `1 <= id < Count`. `GetName` calls it and throws the same `ArgumentOutOfRangeException` (same message) on `false`, so the public API does not change. `CompoundSelector.TypeText` and `ClassText` call it with no `try`. The fallback texts `<type N>` and `.<class N>` do not change. DS2.2 has a line "Changed by CR-5".
- **DR4.2 The list rule is a doc change.** The doc says "A list needs whitespace between two values. A comment alone does not separate two values." The text is in section 11 (the value forms, the transition bullet, and the token rule near the compound rule) and in the remarks of `StyleSheetParser`. The code of `TryReadList` and `TryNextTransitionPart` did not change. The row `margin: 1px/**/2px` of `AWrongValueInAListPointsAtThatValue` expects one `InvalidValue` at the column of `2px`. The test computes the column from the text (1:21 inside `.a { ... }`). The 1:18 of the finding is the column in the probe text, which has a shorter prefix.
- **DR4.3 The name limit.** The remarks of `CompoundSelector.ToString` and `Selector.ToString`, and the paragraph "The equality and the text form" of section 7, state that the parser reads the text back for each name that the grammar of section 11 can read (an identifier for a type or a class, `[A-Za-z0-9_-]+` for a name). Another name gives a text that the parser does not read back.
- **DR4.4 The opacity summary.** The summary of `ValueForm.Opacity` says "One number from 0 to 1."
- **DR5.1 The size record.** A scratch test (deleted) measured `Unsafe.SizeOf<StyleRule>()` as 368 bytes. Section 7, the remarks of `StyleRule.Transitions`, and `## Performance Considerations` state the value. The parse cost values (about 2.8 KB for each one-line rule, about 0.4 s for 20000 rules) stay "about" values from review probe 10.

---

## Hand-offs

The plan Notes "Hand-offs" are the base. Each line has a check against the code at the end of phase 7.

**transitions**
- Read `StyleRule.Transitions` (a `StyleTransitions` set). Use `TransitionEasing`, `StyleTransition`, and `StyleTransitions` of `src/BrutalUi.Core/Style/StyleTransitions.cs`. Do not add a second easing type.
- `StyleTransitions.CanTransition` gives the five members of Q3: `BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`, and `Color`. `Set` and `Get` throw `ArgumentOutOfRangeException` for each other member. `IsSet` gives `false` and `Clear` changes nothing for such a member (DS6.1, DS6.2).
- The first `StyleRule` constructor and the first `StyleSheet.Builder.Add` give an empty set. The sort of the builder and the merge of the cascade copy each rule as a whole, so the set stays with its rule.
- The `StyleTransition` constructor throws for an easing that is not a member (DS6.3). Thus a switch over the four easings needs no fallback branch.
- The parser rejects a negative duration and accepts 0 (`0`, `0s`, `0ms`, and `-0s` give +0, DS6.14). The model keeps each float, so a sheet from code can hold 0, a negative value, `NaN`, or infinity. Apply the rule of the transitions requirements to each one.
- The parser merges the slots inside one rule: a later item or declaration for the same member replaces that slot only (DS6.13). The merge across the matched rules is open. The model of `StyleProperties` gives a later matched rule the win for each member. CSS takes the full list of the winning rule. Decide and document the rule.
- `StylePass.cs` does not change in this feature (`git diff --stat` prints nothing), so the start point of the stylesheet-model hand-off holds. The style pass does not read the set.
- `StyleTransitionsTests.TheSetHoldsNoHeapObject` asserts 44 bytes. A new slot fails the test on purpose. Update the test and the size note of section 7 together.

**markup epic (Phase 3)**
- Load a sheet with `StyleSheetParser` and `GetSheetOrThrow`, next to the XML tree. Use the form `<source>(<line>,<column>): <kind>: <message>` for the errors of the XML loader too. `GetSheetOrThrow` throws `InvalidOperationException` with one error line for each error, separated by LF, with no header line (DS1.17).
- `StyleTokenizer` is an internal static class of `BrutalUi.Markup` (`Tokenize(ReadOnlySpan<byte>, string, List<StyleParseError>)`). An XML loader in the same assembly can reuse its position rule: the column counts code points, and a tab counts 1.
- The analyzer `BRUTALCORE0011` rejects a constant string argument of `Parse(string, string)` (R2). A loader passes the bytes of a file read to the byte overload.
- A sample file with `CopyToOutputDirectory="PreserveNewest"` does not get a new copy when the bin copy is newer (DS7.6).
- The column rule of a rejected byte (DR2.3): outside a comment, each byte sequence that is not valid UTF-8 counts one column. An XML loader that reuses the tokenizer gets the same rule.

**input and later features**
- A game binds its fonts by name through `StyleSheetParser.CreateInfo.Fonts`. A font service can give the map. The parser copies the map at creation, so create the parser after the fonts load.
- `CompoundSelector.ToString` and `Selector.ToString` give the CSS text. They allocate, so use them for debug output only. The equality members allocate nothing.

---

## Gotchas & Learnings

- A test method of a public test class cannot take an internal enum as a parameter (CS0051). `StyleTokenizerTests` gives the kind as `nameof(StyleTokenKind.X)` text and compares it with `Kind.ToString()`.
- `Rune.DecodeFromUtf8` gives `NeedMoreData` for a cut code point at the end of the text. The tokenizer reads it as an invalid byte, with the byte message.
- The BRUTAL analyzer `BRUTALCORE0011` rejects each constant string argument of `StyleSheetParser.Parse(string, string)`, also the source name and `null` (DS1.20). A test writes the text as a `u8` literal, or passes a local.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Markup.Tests/Style/*.cs`, `tests/BrutalUi.Core.Tests/Style/SelectorEqualityTests.cs`, `tests/BrutalUi.Core.Tests/Style/StyleTransitionsTests.cs`
- The tokenizer, each selector form, each property and value form, the variables, the error positions, and the transition declaration.

### Integration Tests
- Each demo parses a text, adds the sheet to a context, runs `Update`, and reads `Element.Style` or `Layout.Rect`.
- `ParsedSheetAllocationTests` measures `Update` with a parsed sheet. `ParserRobustnessTests` proves that no input throws.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetPaintsAPanel' --show-live-output on` and reads `panel #1E222CFF` and `plain #00000000`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCellFromText' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after `panel.States = Hover`.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATextSheetLaysOutAPanel' --show-live-output on` and reads `panel 20,10,200,40`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*AThemeVariableColorsTheButton' --show-live-output on` and reads `button #3060C0FF`, then `error 2:25 UnknownVariable` for a second sheet that reads `var(--missing)`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ASheetWithThreeMistakesReportsEachOne' --show-live-output on` and reads `error 2:3 UnknownProperty`, `error 3:10 InvalidValue`, `error 4:1 UnexpectedEnd`, and `sheet null`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-method '*ATransitionDeclarationIsStoredInTheRule' --show-live-output on` and reads `BackgroundColor 0.2s EaseOut`, then `Opacity none`.
- [ ] Phase 7 demo: the developer runs `scripts/test-linux.sh; echo "exit=$?"` and reads `test-linux: PASS build, test, gpu, smoke (120 frames) in <S> s` and `exit=0`. Then `grep -c 'draw commands 2, vertices 128' artifacts/test-linux/smoke.log` prints `1`.
- [ ] Error path (style-parser): run `dotnet build brutal-ui.sln`, then write `.hello-panel { colour: #fff; }` into `samples/HelloPanel/bin/Debug/net10.0/HelloScreen.css`. Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 1; echo "exit=$?"`. Expect `exit=1` and the line `HelloScreen.css(1,16): UnknownProperty:` on standard error. Then run `touch samples/HelloPanel/HelloScreen.css` (or delete the bin copy) before the next build.
- [ ] Windows run (style-parser): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- The parser runs at load time only and can allocate. The frame loop runs no parser code.
- `StyleRule` grows by the transition set (Risk 8). Task 6.9 records `bytes per element 1049.5` (baseline 1046.6). `StyleRule` is 368 bytes (320 before style-parser), measured with `Unsafe.SizeOf<StyleRule>()`.
- A parse allocates about 2.8 KB for each one-line rule, and a 3.8 MB sheet of 20000 rules parses in about 0.4 s. If a later feature loads very large sheets, it can build `StyleRule` values in place and skip the `PendingRule` copy.

---

## Native Dependencies

- None.

---

## Session Notes

### 2026-10-21 22:09
- `/autorun-epic` started the feature through `/autorun-feature` in the called mode. `/start-feature` created `context.md`, `tasks.md`, and `inbox.md`.
- The next session starts with phase 1, task 1.1.

### 2026-10-21 (phase 1, group 1)
- Tasks 1.1 to 1.5 are done. The decisions DS1.1 to DS1.10 are in `## Phase 1: Decisions`.
- The next session starts with phase 1, group 2, task 1.6.

### 2026-10-21 (phase 1, group 2)
- Tasks 1.6 to 1.10 are done. The decisions DS1.11 to DS1.21 are in `## Phase 1: Decisions`. Phase 1 is complete.
- The next session starts with phase 2, task 2.1.

### 2026-10-21 22:28
- Phase 1 is done in two groups (`phase-1.md`, `phase-1-g2.md`) at the ADVANCED tier. The verifier (`phase-1-verify.md`) gave PASS: 0 warnings, 1385 tests with 0 failed, no change of Core, ImGui, the samples, or `Directory.Packages.props`, no `PackageReference` in Markup, and no reflection. Demo: PASS.
- The orchestrator accepts DS1.1 to DS1.21. Decision R2 keeps the overload pair of `Parse` (DS1.20).
- The inbox holds no line.

### 2026-10-21 (phase 2, group 1)
- Tasks 2.1 to 2.3 are done. The decisions DS2.1 to DS2.7 are in `## Phase 2: Decisions`.
- The next session starts with phase 2, group 2, task 2.4.

### 2026-10-21 (phase 2, group 2)
- Tasks 2.4 to 2.7 are done. The decisions DS2.8 to DS2.17 are in `## Phase 2: Decisions`. Phase 2 is complete.
- The next session starts with phase 3, task 3.1.

### 2026-10-21 22:44
- Phase 2 is done in two groups (`phase-2.md`, `phase-2-g2.md`) at the ADVANCED tier. The verifier (`phase-2-verify.md`) gave PASS: 0 warnings, 1529 tests with 0 failed, 20 Core allocation tests with 0 failed (also `EqualsAllocatesNothing`), no match line of `Selector.cs` or `CompoundSelector.cs` removed, no reflection, and no change of `StylePass.cs`, ImGui, the samples, or `Directory.Packages.props`. Demo: PASS.
- The orchestrator accepts DS2.1 to DS2.17. DS2.8 (two rows of `TextSheetTests` change, because the grammar now accepts a type selector and a chain) is accepted. DS2.12 is a note for phase 4: the `:root {` check gives way to the skip of the block.
- CR-4 of selector-matching is closed by tasks 2.1 to 2.3.
- The inbox holds no line.

### 2026-10-21 22:52
- Phase 3 is done in one group (`phase-3.md`) at the STANDARD tier. The verifier (`phase-3-verify.md`) gave PASS: 0 warnings, 1663 tests with 0 failed, 32 rows in `EachPropertyNameSetsItsOwnMember`, 28 keywords in 7 explicit tables, the CSS order of the edges and the corners against the Core constructors, the invariant culture, and no reflection. Demo: PASS.
- The orchestrator accepts DS3.1 to DS3.12. DS3.8 (one row of `TextSheetTests` names `size` in place of `width` as the unknown property) is accepted.
- The orchestrator removed a second copy of the phase 2 demo line from `## Needs Human Verification`.
- The inbox holds no line.

### 2026-10-21 22:59
- Phase 4 is done in one group (`phase-4.md`) at the STANDARD tier, with the follow-up of DS2.12. The verifier (`phase-4-verify.md`) gave PASS: 0 warnings, 1695 tests with 0 failed, the variable map inside one `Parse` call, a variable after its use, `UnknownVariable` at the `--name` token, a value error from a variable at the `var` token, and the `:root` rules. Demo: PASS.
- The orchestrator accepts DS4.1 to DS4.11. DS4.7 (one error from pass 1 and pass 2) and DS4.8 (the skip to the first `}`) go into the brief of phase 5, group 1.
- The inbox holds no line.

### 2026-10-21 23:11 (phase 5, group 1)
- Tasks 5.1 to 5.3 are done. The decisions DS5.1 to DS5.13 are in `## Phase 5: Decisions`. One phase 4 test changed (DS5.4).
- The next session starts with phase 5, group 2, task 5.4.

### 2026-10-21 23:30 (phase 5, group 2)
- Tasks 5.4 to 5.6 are done. The decisions DS5.14 to DS5.20 and one follow-up are in `## Phase 5: Decisions`. One defect fix in `StyleTokenizer.cs` (DS5.16). Phase 5 is complete.
- The next session starts with phase 6, task 6.1.

### 2026-10-21 23:32
- Phase 5 is done in two groups (`phase-5.md`, `phase-5-g2.md`) at the ADVANCED tier. The verifier (`phase-5-verify.md`) gave PASS: 0 warnings, 1770 tests with 0 failed, 25 kind rows over the 13 kinds, 3 runs of `ParserRobustnessTests` with 0 failed, the recovery inside the token list, the three `UnsupportedSyntax` positions, and ASD-STE100 messages that quote the source text. Demo: PASS.
- The orchestrator accepts DS5.1 to DS5.20. The rename of `TheFirstErrorOfTheSourceWins` (DS5.4) is accepted.
- The quadratic drop check of DS5.5 goes to the cross-phase review as a known item.
- The inbox holds no line.

### 2026-10-21 23:45 (phase 6, group 2)
- Tasks 6.5 to 6.9 are done. The decisions DS6.11 to DS6.17 are in `## Phase 6: Decisions`. Phase 6 is complete.
- The next session starts with phase 7, task 7.1.

### 2026-10-21 23:36 (orchestrator, the phase 6 gate)
- Phase 6 is done in two groups (`phase-6.md`, `phase-6-g2.md`) at the ADVANCED tier. The verifier (`phase-6-verify.md`) gave PASS: 0 warnings, 2021 tests with 0 failed, no code change of `StylePass.cs` (I4), 20 Core allocation tests and `TheStylePassAllocatesNothingAfterWarmUp` with 0 failed, five fixed slots and a mask, the throw rule of `Set` and `Get`, the empty set of the old constructor and the old `Add`, and the duration read. Demo: PASS.
- Bytes per element grow from 1046.6 to 1049.5 (Risk 8 of the plan).
- The orchestrator accepts DS6.1 to DS6.17. DS6.11 (the `transition` row stays out of `Entries`) replaces the note of DS3.11.
- The inbox holds no line.

### 2026-10-21 23:43 (phase 7, group 1)
- Tasks 7.1 to 7.8 are done. The results and the decisions DS7.1 to DS7.9 are in `## Phase 7: Decisions`.
- The next session starts with phase 7, group 2, task 7.9.

### 2026-10-21 23:59 (phase 7, group 2)
- Tasks 7.9 to 7.12 are done. The decisions DS7.10 to DS7.16 and the group 2 result are in `## Phase 7: Decisions`.

### 2026-10-21 23:56 (orchestrator, the phase 7 gate)
- Phase 7 is done in two groups (`phase-7.md`, `phase-7-g2.md`) at the ADVANCED tier. The verifier (`phase-7-verify.md`) gave PASS: 0 warnings, 2023 tests with 0 failed, I1 to I5, no `Inline.` in the sample, `cmp=0` for the sheet copy, and the lint exit 0 for each changed doc. Demo: PASS (`test-linux: PASS ... in 17 s`, `exit=0`, and the 3 smoke lines with `draw commands 2, vertices 128` and `Update and Draw 0`).
- The plant of task 7.7 failed 5 gates (exit 2), also `AParsedSheetAllocatesNothingInUpdate` with 208800 bytes. The restore gave exit 0.
- The orchestrator accepts DS7.1 to DS7.16. All 7 phases are done. The inbox holds no line. `## Hand-offs` holds the checked hand-offs. Phase 7 is complete.

### 2026-10-22 00:33 (orchestrator, the cross-phase review)
- The cross-phase review (`review.md`) wrote `code-review.md` with 8 findings (2 MEDIUM, 6 LOW). A shipped test killed 21 of 22 mutations. Review groups 1 to 5 (`review-g1.md` to `review-g5.md`) fixed all 8 findings: CR-1 (a binary search in the drop check, 50000 bad copies from about 20 s to about 0.4 s), CR-2 (no second error at the use of a rejected variable), CR-3 (a stray continuation byte takes one column), CR-4 (a test of the nested brace count), CR-5 (an internal `TryGetName` in place of a `catch`), CR-6 (the doc of an unused variable), CR-7 (three doc texts), and CR-8 (the rule size of 368 bytes and the parse cost).
- The verifier (`review-verify.md`) gave PASS: 0 warnings, 2044 tests with 0 failed, I1 to I5, 3 runs of `ParserRobustnessTests`, 20 Core allocation tests and 2 parsed-sheet gates with 0 failed, the lint exit 0, the smoke run PASS, and the six phase demos. Demo: PASS.
- The inbox holds no line.

### 2026-10-22 00:55 (`/update-feature`)
- 54 of 54 tasks are done. `implementation.md` reads `Ready for Review`.
- The draft plan of style-system/transitions exists at `docs/features/style-system/transitions/implementation.md` (6 phases, 37 tasks, 4 open scope questions: the phase 1 demo, the merge of the transition sets, the easing curves, and the follow of a `Color` tween into the children). The draft names two conflicts with this feature: `ParsedSheetAllocationTests` asserts target values after `Update(0f)` on a sheet with transitions (its task 1.10 changes the step), and `StylePass.cs` changes in transitions. The architect had no shell, so the lint of the draft did not run. Its task 6.9 runs it.
- The inbox holds no line, so no line folds and no bug goes to `docs/bugs/`.
- Final state: 2044 tests with 0 failed, 0 build warnings, 0 bytes in each allocation gate, bytes per element 1049.5, `StyleRule` 368 bytes, and the smoke run PASS. The base commit is `c2af581`. The work is not committed (the caller owns each git action).

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
