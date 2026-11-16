# markup/xml-element-loader - Context and Decisions

**Last Updated:** 2026-11-13 08:35 UTC
**Current Phase:** Complete (phases 1 to 5, the cross-phase review, and the post-review gate are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**

- ✅ The plan (`implementation.md`) is complete: 5 phases, 38 tasks. Phase 1 is ADVANCED (opus); phases 2 to 5 are STANDARD (sonnet).
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file) stand ready. Base branch `main` at `cf13f19`; the run works on `feat/markup/xml-element-loader`.
- ✅ Phase 1 group 1 (tasks 1.1 to 1.6): the template types exist. `TemplateError` with its 8 kinds, `TemplateLoadResult`, `ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, the three delegates, `RegisteredType`, `AttributeContext`, and `ElementCreateArgs`. The Markup test project links `TreeCompare.cs` and `RecordingRenderer.cs`. The solution builds with 0 warnings and 0 errors, and reads 3032 with 0 failed and 3 skipped. `TemplateReader.cs` was a placeholder stub for the contexts.
- ✅ Phase 1 group 2 (tasks 1.7 to 1.11): the loader works. `TemplateLoader` (registry, hook, both `Load` overloads), `TemplateReader` (the `XmlReader` walk), and `BuiltInElements` (the base rows `name` and `class`, and the types `Element`, `Label`, and `Button` with the `text` create row). `SectionElevenTemplateTests` reads 6 with `failed: 0`, so the phase 1 demo passes: the section 11 template loads and equals the builder tree. The solution reads 3038 with 0 failed and 3 skipped; Markup 584 (+6), Core 1839 (unchanged). Section 11 of `technical-design.md` shows `<ui:Element>` and the loader API. Plants 1 and 2 each failed the named test.
- ✅ Phase 2 group 1 (tasks 2.1 to 2.6): the value engine and the full table. `TemplateValues` holds the seven parse helpers plus `NextName`. `AttributeContext` holds the seven `TryRead*` methods. `TemplateLoader.TryGetTexture` reads the copied map; `TemplateLoader.Textures` is gone. `TemplateReader` runs step 5 (`MissingAttribute`, no factory call) before step 6. `BuiltInElements` holds all eight types and every row of The Written Table except `text-key`. The scratch smoke run reads `SMOKE OK`: no exception leaves `Load` for any bad value. The build reads 0 warnings and 0 errors; the solution stays 3038 with 0 failed and 3 skipped.

- ✅ Phase 2 group 2 (tasks 2.7 to 2.11): the 23 new Markup tests and the docs. `BuiltInTypeTests` 9, `TemplateValueTests` 10, `EightTypeTreeTests` 2, `LoadedTreeAllocationTests` 2, each with `failed: 0`, so the phase 2 demo passes. The build reads 0 warnings and 0 errors; the solution reads 3061 with 0 failed and 3 skipped; Markup 607 (+23). `ALoadedTreeAllocatesNothingInUpdateAndDraw` measures 0 bytes. Section 11 of `technical-design.md` holds The Written Table, The Value Forms, and the phase 2 API members; section 14 names the gate. Plants 3, 4, and 5 each failed the named test.

- ✅ Phase 3 group 1 (tasks 3.1 to 3.6): the positioned errors. Steps 1 and 2 of The Load Walk run (namespace rejection and leaf-child rejection), a prefixed attribute gives `UnknownAttribute`, text and CDATA give `UnexpectedText`, the `XmlException` message form and the stable sort are in, and the `UnknownElement` and `UnknownAttribute` messages list the valid names. `TemplateReader.cs` split into `TemplateReader.cs` (177) and `TemplateReader.Elements.cs` (323). `TemplateLoader.TypeNames` is new. `TemplateErrorTests` 12 with `failed: 0`, so the phase 3 demo passes. The build reads 0 warnings and 0 errors; the solution reads 3073 with 0 failed and 3 skipped; Markup 619 (+12). Section 11 holds **The error report**. Plants 6, 7, and 8 each failed the named test.

- ✅ Phase 4 group 1 (tasks 4.1 to 4.5): custom types and the hook. `Register` checks every entry and rejects a
  duplicate, all-or-nothing (318 lines). `ElementCreateArgs.TryGet` throws for a name that is not a create row.
  `CustomTypeTests` 6 and `AttributeHookTests` 5, each with `failed: 0`, so the phase 4 demo passes. The build
  reads 0 warnings and 0 errors; the solution reads 3084 with 0 failed and 3 skipped; Markup 630 (+11). Section 11
  holds **Custom element types** and **The attribute hook**. Plants 9, 10, and 11 each failed exactly the named
  test with no blast radius.

- ✅ Phase 5 group 1 (tasks 5.1 to 5.4): the localized text. `text-key` (Create) sits after `text` on `Label`,
  `Button`, and `TextField`; `TryReadText` joins the two attributes by the rule of A4, and an empty key gives
  `InvalidValue` at `text-key`. `TreeCompare.LocalizedDiff` compares `LString` values by id and template, and a
  loaded tree with `text-key` equals a builder tree. `LocalizedTextTests` reads 6 with `failed: 0`, and
  `*TreeCompareTests` reads 6 with `failed: 0`. The build reads 0 warnings and 0 errors; the solution reads 3091
  with 0 failed and 3 skipped; Markup 636 (+6), Core 1840 (+1). The Framework suite that links `TreeCompare.cs`
  reads 332 with `failed: 0`: no regression.

- ✅ Phase 5 group 2 (task 5.5): the docs and the close. Section 11 gained the `text-key` row on `Label`, `Button`,
  and `TextField`, the A4 `text-key` rule in The value forms, and the `text-key` name in the Whitespace rule.
  `CHANGELOG.md` gained one `(markup/xml-element-loader)` line under `## [Unreleased]`. The build reads 0 warnings and
  0 errors; the solution reads 3091 with 0 failed and 3 skipped (baseline 3032 + 59, as the DoD states). Plants 12 and
  13 each failed the named test with `error CS` 0 and a `cmp`-clean restore. All 12 Verification Method items ran and
  are recorded below. Task 5.5 added no test.

- ✅ Cross-phase review: 6 findings (1 MEDIUM, 1 LOW-MEDIUM, 4 LOW), all 6 resolved in four fix groups. CR-1 added the `capacity` ceiling 65536 in the `TextField` factory (`TemplateValues.MaxTextCapacity` and `CheckCapacity`), pinned in `ACapacityIsAWholeNumberAboveZero` with plant 14 — the only production change. CR-2 made `CustomTypeTests` public-API-only; CR-3 pinned the `NotWellFormed` position (3,3) and the UTF-16 column rule (measured 42); CR-4 added `TryGetRejectsANameThatIsNotACreateRow` (+1 test); CR-5 and CR-6 fixed two doc lines and one code doc line. Post-review gate: PASS on all 16 items; the solution reads 3092 with 0 failed and 3 skipped.

**What's Next:**

- ✅ Nothing: the feature is complete (38/38 tasks, all five phases closed, the cross-phase review resolved 6/6, the post-review gate PASS at 3092 / 0 failed / 3 skipped).
- ⏳ A person tests the five demo lines under "Needs Human Verification", then commits, pushes, and opens the PR for `feat/markup/xml-element-loader`. `/autorun-feature` makes no commit and no push.
- ⏳ The next feature of the epic is `markup/element-generator`. It replaces `BuiltInElements.cs` with generated rows in the shape of `AttributeRow` and keeps the public `Register` (see plan Notes item 10 and the review CR-1 note in `context.md`: the generated table must keep the `capacity` ceiling behavior — the row calls the same setter path).

**Blockers:**

- None.


---

## Base and Counts

Measured by phase 1 group 1 (task 1.1) on 2026-11-13 03:40 UTC.

- Base commit: `cf13f193015f644f33a534159bc50544eced2f38` (`cf13f19`). Branch: `feat/markup/xml-element-loader`.
- `git status --porcelain` at the start: only the four untracked feature docs (`context.md`, `implementation.md`, `inbox.md`, `tasks.md`). No source change.
- Solution total: **3032 tests, 0 failed, 3 skipped** (`dotnet test --solution brutal-ui.sln`, exit 0). The 3 skipped are the GLFW tests with no display.
- `BrutalUi.Markup.Tests`: **578 tests, 0 failed, 0 skipped**.
- `BrutalUi.Core.Tests`: **1839 tests, 0 failed, 0 skipped**.
- Planned close: Markup +58 (3032 to 3091 with 3 skipped). The deltas of every phase count from these numbers.

Tool notes:

- Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only.
- One test class after a build: `tests/BrutalUi.Markup.Tests/bin/Debug/net10.0/BrutalUi.Markup.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`.
- A plant: copy the file to `src/BrutalUi.Markup/obj/plantscratch/`, change it, run the solution tests to a log. Check the `error CS` count first: it must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect an exit code that is not 0. Restore with `cp`, check with `cmp`, run again. A test host can exit 134 one time; run a second time before you treat it as a fault.
- `/tmp` is not shared between the tool calls of this agent: write a plant log inside the workspace.

---

## Recorded Plan Sections

Condensed from `implementation.md`. The plan holds the full text and the exact signatures.

### Public API (end state, namespace `BrutalUi.Markup`)

- `TemplateLoader` (public sealed class): `const string Namespace = "brutal-ui"`; nested `public struct CreateInfo { IReadOnlyDictionary<string, TextureHandle>? Textures; }`; `TemplateLoader()` and `TemplateLoader(in CreateInfo)` (copies the map, registers the eight built-in types); `void Register(in ElementTypeInfo)` (copies the rows, `ArgumentException` for a bad or duplicate entry); `void AddAttributeHandler(AttributeHandler)`; `TemplateLoadResult Load(ReadOnlySpan<byte> utf8, string sourceName = "template")` and `Load(string text, string sourceName = "template")`.
- `TemplateLoadResult` (public sealed class): `Element? Root`, `IReadOnlyList<TemplateError> Errors`, `bool Succeeded`, `Element GetRootOrThrow()`. Internal `Success(Element)`, `Failure(List<TemplateError>)`.
- `TemplateError` (public readonly struct): `Kind`, `Line`, `Column`, `Message`, `SourceName`, `ToString()` → `<source>(<line>,<column>): <kind>: <message>`. Internal constructor with checks, internal `GetKindName`.
- `TemplateErrorKind : byte`: `NotWellFormed`, `UnknownElement`, `UnknownAttribute`, `InvalidValue`, `MissingAttribute`, `UnknownTexture`, `UnexpectedText`, `UnexpectedChild`.
- `ElementTypeInfo` (public struct): `required string Name`, `required ElementFactory Create`, `AttributeRow[]? Rows`, `bool IsLeaf`. The loader copies the rows.
- `AttributeRow` (public readonly struct): `AttributeRow(string, AttributeRowKind)` (Create or CreateRequired, `Set` gives `ArgumentException`), `AttributeRow(string, AttributeSetter)` (Set); `Name`, `Kind`, `AttributeSetter? Setter`.
- `AttributeRowKind : byte`: `Set`, `Create`, `CreateRequired`.
- Delegates: `Element? ElementFactory(ElementCreateArgs args)`; `void AttributeSetter(Element, AttributeContext)`; `bool AttributeHandler(Element, string name, string value, AttributeContext)` (true: handled).
- `AttributeContext` (public readonly ref struct): `Name`, `Value`, `Line`, `Column`, `SourceName`, `AddError(kind, message)`, the seven `TryRead*` (phase 2): `TryReadBool`, `TryReadWholeNumber(out int)`, `TryReadNumber(out float)`, `TryReadNumbers(out float2)`, `TryReadNumbers(out float4)`, `TryReadColor(out Rgba8)`, `TryReadTexture(out TextureHandle)`.
- `ElementCreateArgs` (public readonly ref struct): `ElementName`, `Line`, `Column`, `SourceName`, `bool TryGet(string, out AttributeContext)`, `AddError(kind, message)`.
- Internal: `TemplateReader`, `RegisteredType`, `TemplateValues`, `BuiltInElements`. 13 new public types.

### The Load Walk

- Settings: `DtdProcessing.Prohibit`, `XmlResolver = null`, `IgnoreComments`, `IgnoreProcessingInstructions`, `IgnoreWhitespace`, `ConformanceLevel.Document`, `CloseInput`. The span copies into a `MemoryStream`.
- Frame stack of `(Element? parent, bool isLeaf)`; a null parent: the subtree reports errors and attaches nothing.
- Node kinds: Element → `ReadElement`, and push the frame unless `IsEmptyElement`; EndElement → pop; Text and CDATA → `UnexpectedText` (phase 3); Whitespace, SignificantWhitespace, XmlDeclaration → skip.
- `catch XmlException` → `NotWellFormed` at `(max(1, ex.LineNumber), max(1, ex.LinePosition))`, stop the read (phase 3).
- Sort the errors by line, then column, stable (phase 3). Errors above 0 → `Failure`; else `Success(root)`.
- `ReadElement` steps: 1 outside the namespace → `UnknownElement` with a null-parent subtree; 2 a child of a leaf → `UnexpectedChild` with a null-parent subtree; 3 no registered type → `UnknownElement` with a null-parent subtree; 4 read each attribute in source order, skip a namespace declaration (`xmlns`, `xmlns:p`), a prefixed attribute → `UnknownAttribute` (phase 3), find the row (base rows, then the own rows), no row → the hook list; 5 a `CreateRequired` row with no attribute → `MissingAttribute` at the element, skip steps 6 to 8 (phase 2); 6 `element = type.Create(args)`, null with no new error → `InvalidOperationException`; 7 run each set row that has an attribute, in table order (base rows first); 8 the hook list in source order, handlers in the order of the add, the first `true` takes it, none → `UnknownAttribute` with the valid names; 9 attach to the parent of the frame, or keep as the root.
- Positions: an element error sits at the first character of the qualified name, after the `<`. An attribute error sits at the first character of the attribute name. A value has no position: a value error points at the name of its attribute.
- Step 7 runs `value` of a `Slider` after the factory made the range, so the source order of the attributes does not change the tree.
- An unprefixed attribute belongs to its element. A prefixed attribute is an error, also with the `brutal-ui` prefix, so each template uses one form.
- `Load` holds no state between calls. One loader loads on more than one thread when no `Register` and no `AddAttributeHandler` run at the same time.

### The Written Table

- Base rows, first for every type: `name` (Set, `Element.Name = value`, text as is), `class` (Set, `ClassName.Intern` per name, `Classes.Add` in order, class list), `pickable` (Set, `IsPickable`, bool), `focusable` (Set, `IsFocusable`, bool).
- Types (leaf, factory, own rows): `Element` (no, `new Element()`, none); `Label` (yes, `text` Create + `text-key` Create phase 5); `Button` (no, `text` Create + `text-key` Create phase 5); `Toggle` (no, `new Toggle(default)`, `checked` Set bool); `Slider` (no, `min` Create, `max` CreateRequired, `step` Create, `value` Set number, range checks in the factory); `TextField` (no, `capacity` CreateRequired, `text` Create, `text-key` Create phase 5); `ScrollView` (no, `scroll-speed` Create, `scroll-offset` Set 2 numbers); `Image` (yes, `texture` Set texture name, `uv` Set 4 numbers, `tint` Set color, `scale-mode` Set keyword).
- A missing `text` gives an empty text. A missing `min` or `step` gives 0. A missing `scroll-speed` gives 0, so the view uses `ScrollView.DefaultScrollSpeed` (40). A missing `texture` gives the none handle.
- `max` is required (`Slider.CreateInfo.Maximum` is `required`). `texture` is optional (the none handle is valid).
- `scroll-offset` writes `ScrollView.ScrollOffset`; a new tree keeps (0, 0) because the set clamps with the last layout, as the builder method does.

### The Value Forms

- A typed value ignores XML whitespace at both ends. `name`, `text`, and `text-key` keep the value as `XmlReader` gives it. XML normalizes a tab or a line break inside an attribute value to a space.
- Bool: `true` or `false`, case-sensitive. `True`, `1`, `yes` give `InvalidValue`.
- Whole number (`capacity`): ASCII digits only, 1 to `int.MaxValue`. A sign, a point, and 0 give `InvalidValue`.
- Number: `InvariantCulture`, `AllowLeadingSign | AllowDecimalPoint`, no exponent, no group separator. A non-finite result gives `InvalidValue`.
- List: numbers with XML whitespace between them. `uv` takes 4, `scroll-offset` takes 2. Another count gives `InvalidValue`.
- Class list: names with XML whitespace between them. An empty value adds none. A repeated name adds nothing.
- Color: `#` and 3, 6, or 8 hex digits in each case; `#rgb` doubles each digit with alpha 255; `#rrggbb` alpha 255; `#rrggbbaa` holds the alpha (the rules of `StyleValues.TryReadColor`).
- Keyword (`scale-mode`): `stretch-to-fill`, `scale-to-fit`, `scale-and-crop`, an explicit table, no `Enum.Parse`.
- Texture name: `CreateInfo.Textures`, ordinal. A name that the map does not hold gives `UnknownTexture`.
- Range checks: `min`, `max`, `step` finite; `max` below `min` gives `InvalidValue` at `max`; a non-finite width gives `InvalidValue` at `max`; a negative `step` gives `InvalidValue` at `step`; `scroll-speed` below 0 gives `InvalidValue`. The factory checks before the constructor, so no `ArgumentOutOfRangeException` leaves `Load`.
- `text-key` (phase 5): `text-key="k" text="t"` gives `new LString("k", "t")`; no `text` gives `new LString("k", "")`; an empty key gives `InvalidValue`.

### Notes (items 1 to 6 and 15 of the plan)

1. A8 vs an unprefixed attribute: the loader reads an unprefixed attribute as an attribute of its element, and rejects a prefixed attribute, also `ui:name`, so each template uses one form.
2. The Out of Scope line of `requirements.md` about `Capacity` and `ScrollSpeed` is stale. Q2 decides: both are create rows. The requirements file does not change.
3. Open point (the plan settles it): `max` required, `texture` optional.
4. The column unit: `IXmlLineInfo.LinePosition` counts UTF-16 units of the decoded text. A tab counts 1. A character outside the BMP counts 2. `StyleParseError` counts code points; this plan keeps the value of `XmlReader` and states the unit in the doc of `TemplateError.Column`.
5. The phase 1 demo adds a test handler for `command`, because binding-attributes registers it later. The builder tree adds the same handler, so `TreeCompare` compares the delegate counts.
6. Tech debt for `/review-epic`: `TemplateValues.TryParseColor` copies the digit rules of `StyleValues.TryReadColor` (Decision 7).
15. The planned baseline comes from the builder-api close: 3032 with 3 skipped at `cf13f19`. The real baseline above equals it.

---

## Key Files

### Core Implementation

- `src/BrutalUi.Markup/Template/` (new folder) - the loader: `TemplateLoader`, `TemplateReader`, `TemplateLoadResult`, `TemplateError`, `ElementTypeInfo`, `AttributeContext`, `TemplateValues`, `BuiltInElements`.

### Related Files

- `tests/BrutalUi.Markup.Tests/Template/` (new folder) - the nine test classes.
- `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` - the tree compare helper, linked into Markup tests in task 1.6; phase 5 extends it.
- `docs/technical-design.md` - section 11 (the markup format) and section 14 (the allocation gates).
- `docs/features/markup/xml-element-loader/implementation.md` - the plan and the source of truth.

---

## Important Decisions

The plan holds ten decisions (Decision 1 to 10) and ten confirmed assumption groups (Q1 to Q6, A1 to A10). This section records only what a phase adds or changes.

### Phase 1 group 1 (tasks 1.1 to 1.6)

- **Risk 2 is closed.** The `readonly ref struct` shape compiles: a delegate with a `readonly ref struct` parameter, a `static` lambda that infers it, and `ElementCreateArgs.TryGet(string, out AttributeContext)` all build clean. The probe file was compiled once inside `src/BrutalUi.Markup/Template/` and deleted. No fallback to `readonly struct` is needed.
- **`TemplateReader.cs` is a placeholder stub, not the walk.** `AttributeContext` and `ElementCreateArgs` hold a reference to it, so the build needed the type. The stub carries the state that the contexts read: `SourceName`, `Errors`, `Attributes` (the plan tuple `Name, Value, Line, Column, Row`), `ElementName`/`ElementLine`/`ElementColumn`, `CurrentType`, `AddError(kind, message, line, column)`, and `TryGetCreateAttribute(name, out index)`. Task 1.8 (group 2) replaces the file with the full walk and keeps the members that the contexts need. The `Files` list of the brief names five new files; the stub is the sixth, allowed by the brief.
- **`ElementCreateArgs` holds no attribute index.** The brief says each context type holds a reader reference and an index into the attribute list. `AttributeContext` does. `ElementCreateArgs` needs only the reader: the element position and the create-attribute lookup live on the reader (`ElementName`, `ElementLine`, `ElementColumn`, `TryGetCreateAttribute`). Deviation with reason: an index there would copy state that has no meaning before the attribute read.
- **A context with no reader throws, not a `NullReferenceException`.** Each struct keeps a nullable `TemplateReader?` field and reads it through a private `Reader` property that throws `InvalidOperationException` with a message that names the type. Reason: `default(AttributeContext)` and `default(ElementCreateArgs)` exist as values; the failure must state the rule.
- **`AttributeRow` does not check the setter for `null`.** Task 4.1 makes "a `Set` row has no setter" a `Register` fault with `ParamName` `info`. A constructor check would make such a row impossible to build, so the test could not reach `Register`. The constructor checks the name for `null` only, and the `AttributeRow(string, AttributeRowKind)` constructor throws `ArgumentException` with `ParamName` `kind` for `Set`.
- **`RegisteredType` merge rule.** The stored row array is the base rows, then the own rows, copied out of `ElementTypeInfo.Rows` (Decision: the loader copies the rows). The name-to-index map is ordinal and keeps the first row of a duplicate name, so a base row wins over an own row with the same name; task 4.1 rejects that duplicate. `RequiredIndices` holds the `CreateRequired` row indices in table order (task 2.4 consumes them). `AttributeNames` holds the names joined with `", "` in table order, for the `UnknownAttribute` message of task 3.3.
- **`TemplateError` uses the value of `IXmlLineInfo.LinePosition` unchanged**, and its docs state the UTF-16-unit rule (Notes item 4). Group 2 measured the rule: the value is the first character of the qualified element name and the first character of an attribute name, and `command` in the section 11 template sits at line 3, column 59. See the group 2 decisions (Risk 1 closed).
- **`TemplateLoadResult.Failure` does not sort.** The reader sorts by line, then column before it calls `Failure` (task 3.2). `Failure` copies the list, as `StyleParseResult.Failure` does.

### Phase 1 group 2 (tasks 1.7 to 1.11)

- **Risk 1 is closed: the measured column rule.** A scratch program read the section 11 template through `XmlReader.Create` and cast the reader to `IXmlLineInfo`. On an element node, `LinePosition` is the first character of the qualified name, after the `<`: `  <ui:Label` on line 2 gives line 2, column 4. On an attribute node it is the first character of the attribute name: `command` on line 3 gives line 3, **column 59**, exactly as the plan expects. The test asserts that pair, so the later tests compute each column from the same rule. `XmlException` uses the same convention and can point past the last line: an unterminated root gave line 4, column 1 with the message `Unexpected end of file has occurred. The following elements are not closed: ui:Element.`
- **The `XmlException` position is clamped in this group, not in phase 3.** `TemplateError` rejects a line or a column below 1, and `XmlReader` reports 0 and 0 for a text with no root element (`Root element is missing.`). Without the clamp, `Load` would throw for the text, which breaks the load rule of the quality bar. The reader clamps both values to 1. Task 3.2 still owns the message change (it appends `XmlException.Message`) and the stable sort.
- **The reader keeps the group 1 members and gains a loader reference.** The constructor is now `TemplateReader(TemplateLoader loader, string sourceName)`. `SourceName`, `Errors`, `Attributes`, `ElementName`/`ElementLine`/`ElementColumn`, `CurrentType`, `AddError(kind, message, line, column)`, and `TryGetCreateAttribute(name, out index)` stay as group 1 wrote them, so `AttributeContext` and `ElementCreateArgs` did not change. The loader exposes `internal IReadOnlyDictionary<string, RegisteredType> Types`, `internal IReadOnlyList<AttributeHandler> Handlers`, and `internal IReadOnlyDictionary<string, TextureHandle> Textures`. The walk reads the first two. `Textures` keeps the copied map read in this phase; task 2.3 replaces that access with `TryGetTexture`.
- **The attribute list stores the name as the source writes it.** `Name` holds `XmlReader.Name`, which is the qualified name for a prefixed attribute and the plain local name for the usual form. `AttributeContext.Name` documents "the attribute name, as the source writes it", and a row lookup on that name makes a prefixed attribute miss the table and land on the hook, so the phase 3 rule for a prefixed attribute (`UnknownAttribute`) follows with no extra check. `Row` holds the table row index, or -1. The reader resolves it during the read (step 3 runs before step 4), so `TryGetCreateAttribute` and step 7 need no second pass.
- **Namespace declarations are skipped by prefix.** The read skips an attribute whose `Prefix` is `xmlns` (`xmlns:ui`) or whose unprefixed `LocalName` is `xmlns` (the default declaration). `XmlReader` reports the namespace `http://www.w3.org/2000/xmlns/` for both.
- **A character reference to whitespace stays in an attribute value.** `class="a  b&#9;c"` gives the value `a  b<TAB>c`, because XML normalizes only literal whitespace in a value to a space. The split loop in `BuiltInElements.AddClasses` treats space, tab, CR, and LF as separators, so both forms give the same three names. `class=""` gives no name. Task 2.1 moves the loop to `TemplateValues.NextName`.
- **Steps 3 and 6 push a null-parent frame now (Decision 9, early).** After an unknown element, or after a factory that returns `null` with a new error, the walk pushes `(null, false)` for a non-empty element, so each child reports its own errors and attaches nothing. Steps 1 (namespace) and 2 (leaf child) stay for phase 3, and step 5 (`MissingAttribute`) stays for phase 2. The walk skips text nodes in this phase; phase 3 adds `UnexpectedText`.
- **The frame stack is a `Stack<(Element? Parent, bool IsLeaf)>`.** The plan names the frame as a `(parent, isLeaf)` pair. A tuple keeps `IsLeaf` stored without an unused private field, which the warning-as-error policy would reject.
- **A guard for a walk with no root and no error.** If the read ends with no error and no root, the reader records one `NotWellFormed` at (1,1). No template reaches that branch today: `ConformanceLevel.Document` throws for a missing root. The branch keeps `Load` from calling `Success(null)`, which would throw.
- **`BRUTALCORE0011` also flags the source-name literal at a call of `Load(string, string)`.** Measured: `fromText.Load(text, "hud.xml")` fails the build with the error reported on the `"hud.xml"` argument, while `loader.Load(bytes, "hud.xml")` (the span overload) is clean. Deviation from the brief wording, which named only the template argument: the `string`-overload test calls `Load(text)` and takes the default source name `template`. The other five tests pass a `u8` literal to the span overload, and the error test keeps the literal `"hud.xml"` there.
- **The hook tests use two delegate shapes.** `HandleCommand` is a `private static` method, added by method group, and it adds the `Clicked` handler through `OnClicked`, so `TreeCompare` compares equal invocation counts. `TheHookGetsTheElementTheNameAndTheValue` needs a capturing lambda, because it records the name, the value, and the position into locals; it names each parameter type. Both compile with the `readonly ref struct` parameter, so Risk 2 stays closed.
- **Doc placeholders switched.** `ElementTypeInfo.cs` now uses `<see cref="TemplateLoader.Register(in ElementTypeInfo)"/>` in two places, and `TemplateLoadResult.cs` uses `<see cref="TemplateLoader"/>`. A cref must carry the `in` modifier: `Register(ElementTypeInfo)` does not resolve (CS1574).
- **Section 11 of `technical-design.md`.** `<ui:Panel>` became `<ui:Element>` (Q1); the "through a generated table" sentence became the written-table sentence; the new **The loader API** block holds the phase 1 members; and one sentence states that binding-attributes registers `command` on the attribute hook. `diff` of the doc block (lines 1188 to 1191) against the test raw literal (its lines 27 to 30, minus the 8-space literal indent) gives no output, so the two texts are identical character for character.

**Counts after group 2 (measured 2026-11-13 04:19 UTC):** solution 3038, 0 failed, 3 skipped (3032 + 6); Markup.Tests 584 (578 + 6); Core.Tests 1839 (unchanged). `*SectionElevenTemplateTests` reads total 6, failed 0 — the phase 1 demo passes.

**Plants:**

- Plant 1 (`TemplateReader.RunHook`, step 8 calls no handler: the loop bound became `h < 0`): exit code 2, `error CS` count 0, failed 3 — `TheSectionElevenTemplateEqualsTheBuilderTree`, `TheHookGetsTheElementTheNameAndTheValue`, and `TheStringOverloadGivesTheSameTree` (its `GetRootOrThrow` throws on the new `UnknownAttribute` error). The two named tests failed as planned. Restored with `cp`, verified with `cmp`, and the run went green again (exit 0, failed 0).
- Plant 2 (`BuiltInElements.AddClasses` adds the first name only: a `break` after the first add): exit code 2, `error CS` count 0, failed 1 — `AClassListGivesEachNameInOrder`, exactly the named test. Restored with `cp`, verified with `cmp`.
- Logs and the original copies stay in the git-ignored `src/BrutalUi.Markup/obj/plantscratch/`: `TemplateReader.cs.p1.orig`, `BuiltInElements.cs.p2.orig`, `p1.log`, `p1-restore.log`, `p2.log`.

**Blast radius of group 2:** three new files under `src/BrutalUi.Markup/Template/`, and the replacement of the placeholder `TemplateReader.cs`, whose constructor signature changed. `AttributeContext.cs` and `ElementCreateArgs` (in `AttributeContext.cs`) did not change; they read the members that the new reader keeps. `ElementTypeInfo.cs` and `TemplateLoadResult.cs` changed only in doc comments. One new test file under `tests/BrutalUi.Markup.Tests/Template/`. `docs/technical-design.md` section 11 grew. Nothing in `src/BrutalUi.Core/`, `src/BrutalUi.Framework/`, `src/BrutalUi.Markup/Style/`, `src/BrutalUi.Markup/BrutalUi.Markup.csproj`, `samples/`, `Directory.Packages.props`, and `brutal-ui.sln` changed: `git diff --stat` over those paths gives no output.

**Migration risks and follow-up work:**

- `TemplateReader.cs` is 376 lines. Task 2.4 (step 5 and the null-factory rule) and task 3.1 to 3.2 add more. If the file passes 400 lines, move the value work out with task 2.1 (`TemplateValues.cs`) and split the walk so that the element read of one node sits in its own method; keep the public and internal API unchanged.
- `TemplateLoader.Textures` is an internal accessor that no phase 1 code reads. Task 2.3 should replace it with `TryGetTexture` and delete the accessor, or keep it as the read path of `TryReadTexture`.
- The short messages of step 3 (`UnknownElement`) and step 8 (`UnknownAttribute`) do not yet list the valid names. Task 3.3 builds `TemplateLoader.TypeNames` and switches both messages to the form of The Messages. `RegisteredType.AttributeNames` already holds the attribute list.
- `ApplyRows` scans the attribute list per set row (O(rows x attributes)). That is fine at load time. A later phase can store the attribute index in the row lookup if a large template shows a cost.
- The walk creates elements before it knows that a later node is not well formed, so a broken text can leave elements allocated and intern their class names (Risk 7, Notes item 11). No change in this group.
- `TemplateLoadResult.Errors` documents "sorted by line, then by column", and `TemplateReader` does not sort yet: this phase gives the load order. The current tests each hold one error, so no assert depends on the order. Task 3.2 adds the stable sort and closes the doc.

**Open questions:**

- `Register` in phase 1 replaces a type with the same name and checks only `Name` and `Create` for `null`. Task 4.1 owns the full checks. Recorded, not guessed.
- `ElementCreateArgs.TryGet` still returns `false` for a name that is not a create row. Task 4.2 decides the `ArgumentException` rule, as the plan states. Recorded, not guessed.
- No `IsLeaf` check exists yet, so a child of a `Label` loads today. Task 3.1 adds step 2. Recorded so that phase 3 does not lose it.

### Phase 2 group 1 (tasks 2.1 to 2.6)

- **`TryParseNumber` rejects a non-finite result, which covers three value-form faults in one guard.** `float.TryParse` with `AllowLeadingSign | AllowDecimalPoint` in `InvariantCulture` accepts the words `NaN` and `Infinity`, and a too-large magnitude rounds to `±float.MaxValue` or `±Infinity`. The method returns `false` when the read succeeds but `float.IsFinite` fails. That gives `InvalidValue` for `NaN`, `1e3` (the style rejects the exponent), and the 50-digit number, as The Value Forms ask, with no extra code.
- **`NextName` keeps the class split allocation-light.** `internal static bool NextName(ref ReadOnlySpan<char> rest, out ReadOnlySpan<char> name)` cuts the next name and moves the rest past it. `BuiltInElements.AddClasses` loops on it and interns one `name.ToString()` per name: one allocation per class name, at load time only. `IsXmlWhitespace` and the split loop left `BuiltInElements.cs` with it.
- **A failed `TryRead*` writes a safe default, never a half value, and each setter helper guards the write.** `SetPickable`, `SetFocusable`, `SetChecked`, `SetValue`, `SetScrollOffset`, `SetTexture`, `SetUv`, and `SetTint` write only when the read gives `true`, so a bad value adds one `InvalidValue` (or `UnknownTexture`) error and changes nothing. The helper methods are `private static`; each row lambda is `static` and one line, so the generated-table shape of Decision 1 stands.
- **Range and keyword messages follow The Messages style, but the plan fixes no words for them.** The Messages names exact forms only for `UnknownElement`, `UnknownAttribute`, `InvalidValue` (number), `MissingAttribute`, and `UnknownTexture`. The bool, whole-number, list, color, texture-name, `max`-below-`min`, infinite-width, negative-step, negative-speed, and scale-mode words were written to the same rule: quote the value, name the problem, name the fix. Group 2 tests must assert with `Contains` on the quoted text only, as the plan's test rule says. Recorded so group 2 does not pin a wrong word.
- **Step 5 sits in `TemplateReader.Create` before the factory call, and reuses the phase 1 null handling.** `RegisteredType.RequiredIndices` (built by `Register` in group 1 of phase 1 — task 2.3's storage needed no new code) is scanned in table order. Each missing row adds `MissingAttribute` at the element position; any miss returns `null` before `type.Create` runs. `ReadElement` already turns a `null` element into `PushFailedFrame` (the null-parent subtree), so the check did not duplicate the frame push. The step 6 `InvalidOperationException` rule also already existed from phase 1 group 2; it only gained a comment.
- **The width check of a `Slider` stays although the parser rejects non-finite bounds.** Two finite bounds can give an infinite width: `min="-340282346638528859811704183484516925440"` and the same positive value give `max - min = float.PositiveInfinity`. The factory reports that fault `InvalidValue` at `max`, as The Value Forms say. The probe covers it.
- **Texture reads route through `TryGetTexture`; the internal `Textures` accessor is deleted.** Task 2.3's either/or resolved to "route it": `TemplateLoader.TryGetTexture` owns the ordinal lookup, `TemplateReader.TryGetTexture` forwards, and `AttributeContext.TryReadTexture` calls the forward. A `null` map gives an empty copy, so every `texture` value is `UnknownTexture`, and the `CreateInfo.Textures` doc now states that.
- **A factory never sees a missing required attribute.** `CreateTextField` checks `capacity` defensively (`return null` with no new error cannot happen after step 5, and step 6's rule would name the type if it did). `Slider` reads `max` the same way. The optional reads use the plan's defaults: no `min`/`step` gives 0, no `scroll-speed` gives 0 (so the view uses `DefaultScrollSpeed` 40), no `texture` gives the none handle from `CreateInfo { Texture = default }`.
- **`scale-mode` trims with `TemplateValues.Trim` first.** The trimmed substring feeds the explicit `switch` (ordinal by the language rule); an unknown word adds `InvalidValue` at the attribute. This is the keyword rule of The Value Forms: typed values ignore XML whitespace at both ends.
- **Line budget of `TemplateReader.cs`: 398.** Task 2.4 added about 20 lines, so the file lost 4 elsewhere without an API change: one blank line in the `EndElement` case, the two-line default-case comment joined, the `XmlException` `AddError` rewrapped, and the `Create` summary joined. Phase 3 (tasks 3.1 and 3.2) still owns only about 15 lines here; if it needs more, split the element read of one node into its own file-level partial, as the phase 1 follow-up note says.
- **Smoke probe lives in `src/BrutalUi.Markup/obj/plantscratch/probe-p2/` (git-ignored).** One console project references `BrutalUi.Markup.csproj` and calls the public API only. The good template holds all eight types, every base row, every own row except `text-key`, and the whitespace forms (`value=" 7 "`, `scroll-offset=" 0 30 "`); the run asserts each written value, among them the (0, 0) clamp of `scroll-offset` on a new tree (Notes item 13). Twenty-seven bad templates plus a `null`-map texture read plus an empty text give one error each and no root. The result line reads `SMOKE OK`: no exception left `Load` for any bad value of The Value Forms. First run detail: one list entry was a wrong bad case — a `Slider` with `min` and `max` and a `Label` child is valid (Notes item 12) — so the probe was fixed, not the loader; the case now reads `min="5" max="1"` with the same child and reports `InvalidValue` at `max` with a null-parent subtree.
- **`git add -N` ran for the one new file under `src/` (`TemplateValues.cs`).** No commit, no push. The probe files sit in the git-ignored `obj/` and need no intent-to-add.

**Counts after group 1 (measured 2026-11-13 04:41 UTC):** build 0 warnings, 0 errors. Solution 3038, 0 failed, 3 skipped (unchanged: the new tests come in group 2). No test was added or edited in this group. Template file lines: `AttributeContext` 286, `BuiltInElements` 356, `ElementTypeInfo` 220, `TemplateError` 151, `TemplateLoader` 199, `TemplateLoadResult` 92, `TemplateReader` 398, `TemplateValues` 273 — eight files, each under 400.

**Open questions:**

- None blocking. The wording of the ten free-form messages (above) is the only point a group 2 test could pin wrongly; the plan's `Contains` rule protects it.

### Phase 2 group 2 (tasks 2.7 to 2.11)

- **Each position assert derives the line and the column from the template text that the test loads.**
  `TemplateValueTests` holds three helpers: `AssertAttribute` finds `name="` in the text, `AssertElement` finds the
  qualified name after the `<`, and `AssertPosition` turns the index into a line and a column (count `<` bytes
  before the index, then measure from the last line feed). Deviation with reason: the brief asked for a computed
  literal per column; a derivation from the same text cannot drift when a template line moves, and it still pins
  the rule of Risk 1 — the error sits at the first character of the name. Phase 3 should reuse the derivation.
- **The value tests pass `Encoding.UTF8.GetBytes(localString)` to the span overload, not a `u8` literal.** Each of
  those tests loops over a list of bad words, so the template text is composed. `Load(ReadOnlySpan<byte>, string)`
  stays the same call, and phase 1 recorded it analyzer-clean with a literal source name. The other three new
  classes keep the `u8` literals.
- **The invariant-culture test reads `scroll-speed`, not `Slider.value`.** `Slider.CreateInfo.Maximum` is
  `required`, so a `value` template needs a `max` too, and one slider fault then breaks the culture test as well.
  The first version of the test did exactly that, and plant 3 failed three tests instead of two. `scroll-speed`
  writes one number through the same `TryReadNumber` with no range check, so the test covers the culture only.
- **`AToggleTakesChecked` adds the counter after the load, then writes `Checked` from code twice.** A code set
  calls no `CheckedChanged` (Decision 5 of interactive-elements), so the counter stays 0. An assert right after the
  load would pass even with no handler at all.
- **The gate scene sets the inline fill of each panel after the load.** `PaintWalk.Enter` records a rect only for
  a visible fill or a visible border, and the plan asks the last frame for a rect call. Q5 binds the template
  format, not the panel code, and `ScrollViewAllocationTests` sets an inline fill the same way.
- **The gate class names carry the `loaded-` prefix**, because the class table is global and xUnit runs the test
  classes in parallel. The tree holds 55 elements: 1 root, 6 panels, 42 panel children, and 6 labels inside the
  views. The `RecordingRenderer` starts at 256 calls and 1024 text bytes, so one frame fits with no growth.
- **`git add -N` ran for the four new test files.** No commit, no push. `src/BrutalUi.Core/` keeps an empty diff
  after plant 5.

**Counts after group 2 (measured 2026-11-13 05:24 UTC):** build `0 Warning(s)`, `0 Error(s)`. Solution **3061, 0
failed, 3 skipped** (3038 + 23). `BrutalUi.Markup.Tests` **607** (+23); `BrutalUi.Core.Tests` 1839 (unchanged). Per
class, each with `failed: 0`: `*BuiltInTypeTests` 9, `*TemplateValueTests` 10, `*EightTypeTreeTests` 2,
`*LoadedTreeAllocationTests` 2. The phase 2 demo passes, and `ALoadedTreeAllocatesNothingInUpdateAndDraw` measures
0 bytes.

**Plants:**

- Plant 3 (`BuiltInElements.CreateSlider` passes `Maximum = min`): exit code 2, `error CS` count 0, failed 2 —
  `ASliderTakesTheRangeBeforeTheValue` and `AnEightTypeTemplateEqualsTheBuilderTree`, exactly the named tests. Log
  `src/BrutalUi.Markup/obj/plantscratch/p3c.log`; the original
  `plantscratch/BuiltInElements.cs.p3.orig`. An earlier run of this plant (`p3.log`, then `p3b.log` after the
  restore) failed two more tests, which led to the two test changes above.
- Plant 4 (`TemplateValues.TryParseNumber` uses `CultureInfo.CurrentCulture`): exit code 2, `error CS` count 0,
  failed 1 — `ANumberReadsInTheInvariantCulture`, the named test. Log `p4.log`; original `TemplateValues.cs.p4.orig`.
- Plant 5 (`GC.KeepAlive(new object());` as the first line of `Image.PaintContent`): exit code 2, `error CS` count
  0, failed 3 — `ALoadedTreeAllocatesNothingInUpdateAndDraw` (0 expected, 21616 measured) plus the two older image
  gates `ElementAllocationTests.UpdateAndDrawAllocateNothingAfterWarmUp` and
  `ImGuiRendererAllocationTests.LabelUpdateAndDrawAllocateNothingAfterWarmUp`, because the plant sits in the image
  paint path that all three frames reach. Log `p5.log`; original `Image.cs.p5.orig`.
- After each restore `cmp` found no difference. After plant 5, `git diff --stat -- src/BrutalUi.Core` gave no
  output, and the re-run read exit 0 with 3061, 0 failed, 3 skipped (`p5-restore.log`).

**Docs:** section 11 of `technical-design.md` now holds **The written table** (the base-row table and the eight-type
table, minus `text-key`, with the default, required, `scroll-offset` clamp, and factory-order notes), **The value
forms** (the ten rules of the plan, with the texture-copy rule), and the seven `TryRead*` members in the
**AttributeContext** block of **The loader API**. `TryGetTexture` stays internal, so the block does not list it.
Section 14 gained **A twenty-first allocation gate** for `LoadedTreeAllocationTests`, in the style of the other gate
bullets, with the plant 5 record.

**Open questions:**

- None blocking. `text-key` stays out of both the table and the docs until phase 5; the section 11 note says so.

**Blast radius of group 2:** four new files under `tests/BrutalUi.Markup.Tests/Template/`, and the two doc files. No
file under `src/` changed in this group: `git diff --stat -- src/BrutalUi.Core` gives no output after plant 5, and
the other `src/` files show only the group 1 work. `TemplateReader.cs` stays 398 lines.

### Phase 3 group 1 (tasks 3.1 to 3.6)

- **Split `TemplateReader.cs`.** The file sat at 398 lines, and steps 1, 2, the sort, and the text node add more. As
  the brief planned, the element walk moved to a second `internal sealed partial class` file,
  `TemplateReader.Elements.cs`. `TemplateReader.cs` (177 lines) keeps the state, the constructor, `Load`, `AddError`,
  `TryGetCreateAttribute`, and `TryGetTexture`. `TemplateReader.Elements.cs` (323 lines) keeps `ReadElement`,
  `ReadAttributes`, `Create`, `ApplyRows`, `RunHook`, `Attach`, `PushFailedFrame`, `FindAttributeOfRow`,
  `IsNamespaceDeclaration`, and the two new `AddUnexpectedText` and `SortErrors`. Behavior is unchanged, and each
  file stays under 400 lines.
- **The frame tuple gained a type name.** The plan names a frame as `(parent, isLeaf)`. Step 2's `UnexpectedChild`
  message names the parent type, so the tuple is now `(Element? Parent, string TypeName, bool IsLeaf)`. `ReadElement`
  pushes `type.Name`, and `PushFailedFrame` pushes `string.Empty`. `Attach` still reads only `.Parent`, so the extra
  field changes no existing behavior.
- **Namespace rule (Notes item 1) in the code.** Step 1 compares `xml.NamespaceURI` to `TemplateLoader.Namespace`, so
  the default-namespace form (`xmlns="brutal-ui"` on the root) passes: the reader reports the namespace URI, not the
  prefix. The message uses the qualified name `xml.Name`, for example `'x:Label'`, and names the fix
  `Add xmlns:ui="brutal-ui" and write <ui:Label>.` Step 4 gives `UnknownAttribute` for any attribute whose `Prefix` is
  not empty (the `brutal-ui` prefix included), with the fix `Write the attribute with no prefix.` The skip of a
  namespace declaration stays as phase 1 wrote it.
- **The `XmlException` clamp is complete.** Phase 1 group 2 added `Math.Max(1, ...)` for both line and position. This
  group keeps it, adds the `NotWellFormed` message form `The template is not well-formed XML. ` followed by
  `XmlException.Message`, and folds the old `NoRootElement` helper into the load tail (one `NotWellFormed` at (1, 1)
  when the read ends with no error and no root, for example an empty span).
- **The stable sort is a list with an index tie-break.** `SortErrors` copies the indices, sorts by `Line`, then
  `Column`, then the original index, and rebuilds the list. `List.Sort` is not stable, so the index tie-break makes the
  order deterministic and equal to the source order for two errors at one position. The sort runs once, at load time,
  after the walk and before `Failure`.
- **`TemplateLoader.TypeNames` is rebuilt after each `Register`.** The names are copied from the registry, sorted
  ordinal with `Array.Sort(names, StringComparer.Ordinal)`, and joined with `, `. The reader uses the text in the step
  3 `UnknownElement` message, and `RegisteredType.AttributeNames` (built by `Register` in phase 1) in the step 8
  `UnknownAttribute` message. A game that registers a type at runtime therefore extends the `UnknownElement` list with
  no extra code.
- **The 12 tests derive each position from the template text.** The new file copies the phase 2 helper style:
  `AssertAttribute`, `AssertPrefixedAttribute`, `AssertElement`, and `AssertPosition` turn the index of the fault text
  into a line and a column, so a moved line cannot drift. `AssertPrefixedAttribute` finds the qualified token
  (`x:name="`), because an element or attribute error sits at the first character of the qualified name.
- **`ADtdIsAnError` asserts the DTD line, not only the kind.** A document type with a prohibited read gives
  `NotWellFormed` on line 1, where the `<!DOCTYPE>` stands. With `DtdProcessing.Ignore` the reader skips the type and
  later faults on the undefined entity at line 2, so the test asserts `Assert.Equal(1, error.Line)`. Without that
  assert, plant 7 still gave one `NotWellFormed` and passed. The line-1 assert is the rule the plant turns off.
  Deviation with reason: the brief's task text named only the kind; the line assert proves the prohibition, not a
  deferred entity fault.
- **The `errors.xml` fault helper loads with the span overload and a literal source name.** The analyzer clean rule of
  phase 1 holds: a `u8` template plus a string-literal source name is clean on the span overload.

**Counts after group 1 (measured 2026-11-13 06:00 UTC):** build `0 Warning(s)`, `0 Error(s)`. Solution **3073, 0
failed, 3 skipped** (3061 + 12). `BrutalUi.Markup.Tests` **619** (+12); `BrutalUi.Core.Tests` 1839 (unchanged).
`*TemplateErrorTests` reads total 12, failed 0 — the phase 3 demo passes. Template file lines: `TemplateReader` 177,
`TemplateReader.Elements` 323, `TemplateLoader` 230, and the six other files unchanged, each under 400.

**Plants:**

- Plant 6 (continue rule off: a `break` after the walk records one or more errors): exit 2, `error CS` 0, failed 3 —
  `TheLoaderRecordsEachErrorAndContinues` (the named test), plus `XmlThatIsNotWellFormedStopsTheRead` and
  `AnErrorGivesItsTextAndGetRootOrThrowListsEachError`. The blast radius reaches both extra tests because each expects
  more than one error, and the return-after-first-error plant leaves only one. Log `p6.log`; original
  `TemplateReader.cs.p6.orig`.
- Plant 7 (`DtdProcessing.Ignore` in the settings): exit 2, `error CS` 0, failed 1 — `ADtdIsAnError`, the named test.
  The reader skips the type and faults on the entity on line 2, so the line-1 assert fails. Log `p7.log`. A `sed` one
  line toggle made the change; `cp` from `TemplateReader.cs.p6.orig` restored it and `cmp` was clean.
- Plant 8 (step 2 does not check the leaf flag: `_frames.Peek().IsLeaf` replaced with `false`): exit 2, `error CS` 0,
  failed 1 — `AChildInsideALabelOrAnImageIsAnError`, the named test. The child loads into the leaf, so no
  `UnexpectedChild`. Log `p8.log`; original `TemplateReader.Elements.cs.p8.orig`.
- After each restore `cmp` found no difference. After plant 8 the rebuild read 0 warnings, 0 errors, and the re-run of
  the solution read exit 0 with 3073, 0 failed, 3 skipped.

**Open questions:**

- None blocking. The wording of the `UnexpectedChild` and `UnexpectedText` messages is free (The Messages fixes no
  words for them), and the tests assert with `Contains` on the quoted name or text only.

### Phase 4 group 1 (tasks 4.1 to 4.5)

- **`Register` runs every check before it changes the registry (all-or-nothing).** The check order: `Name` null,
  `Name` a valid XML local name, `Create` null, each own row name valid, each `Set` row with a setter, no own row
  with a base row name, no two own rows with one name, the type name not registered. `IsValidXmlName` returns
  `false` for an invalid name, and each fault throws `ArgumentException` with `ParamName` `info` and a message
  that names the fault. The `null` `Name` and `null` `Create` faults keep the more exact
  `ArgumentNullException`, a subtype of `ArgumentException`, so the acceptance rule holds. The duplicate rule
  lives in `Register`, not in `RegisteredType`: `RegisteredType` still keeps the first row of a name (its phase 1
  comment), so a fault in `Register` can name the two rows. The method builds the `RegisteredType` fully and then
  inserts with `Dictionary.Add`; no path replaces a registered name. Phase 1 open question 1 closed.
- **`XmlConvert.VerifyNCName` throws two exception types: both mean invalid.** The method throws
  `XmlException` for a bad character (`'a b'`) and `ArgumentException` for the empty name. `IsValidXmlName`
  catches `XmlException or ArgumentException` with a filter, so `Register` turns both into one
  `ArgumentException` with `ParamName` `info`, as task 4.1 asks.
- **`ElementCreateArgs.TryGet` throws `ArgumentException` for a name that is not a create row (phase 1 open
  question 2 closed).** `TryGetCreateAttribute` gives the attribute of a create row; when it gives `false`,
  `TryGet` reads `Reader.CurrentType` directly: no row with the name, or a row of kind `Set`, throws with a
  message that names the attribute and the element type. `false` with a default context stays for a create row
  whose attribute is absent. The check lives in `AttributeContext.cs` with no new reader member.
- **`Gauge : Element` uses the public parameterless `Element()` constructor** — `Element` exposes it (no
  `in CreateInfo` form there), so the test class holds `private sealed class Gauge : Element` with
  `Gauge(int segments)`, a get-only `Segments`, and an ordinary `float Level` auto-property. `TreeCompare`
  compares the runtime type, the name, the classes, and the members it knows; it reads no custom member, so the
  gauge pair gives `null` and the test asserts `Segments` and `Level` directly.
- **`RegisterChecksTheTypeInfo` proves the all-or-nothing rule with the registry count.** Each fault asserts the
  `ArgumentException` family, `ParamName` `info`, a message that holds the quoted fault text, and that
  `loader.Types.Count` did not change; a failed `Register` therefore registered nothing. `Probe` names the fresh
  faults, `Label` names the type-exists fault, and `Gauge` twice names the duplicate after the good entry.
  `loader.Types` is internal, and `InternalsVisibleTo` for `BrutalUi.Markup.Tests` exists, so the test reads the
  registry through the same path the reader walks.
- **The handler-order test pins the "first true takes it" rule with a counter array.** Three handlers count their
  calls into `calls[0..2]`; the first returns `false`, the second returns `true`, and the third must stay 0, so
  the later handlers never run. Plant 9 flips this test.
- **`EachLoaderHasItsOwnRegistry` asserts both halves of A1 in one load.** Loader A registers `Gauge` and takes
  `probe` through a handler, so its tree equals nothing less than the gauge root's child. Loader B holds the
  same bytes and reports `UnknownAttribute` for `probe` and `UnknownElement` for `Gauge`, in source order after
  the phase 3 sort.
- **The three plants all compile, so none could land on an old binary.** Plant 9 and plant 11 each start from a
  `cmp`-verified copy of the phase 3 split `TemplateReader.Elements.cs`, and plant 10 edits `TemplateLoader.cs` in
  place; all three show `error CS` 0 in the log. Each change sits in one method body, so no signature moved and
  the restore needs only `cp`.

**Counts after group 1 (measured 2026-11-13 06:35 UTC):** build `0 Warning(s)`, `0 Error(s)`. Solution **3084, 0
failed, 3 skipped** (3073 + 11). `BrutalUi.Markup.Tests` **630** (+11); `BrutalUi.Core.Tests` 1839 (unchanged).
Per class, each with `failed: 0`: `*CustomTypeTests` 6, `*AttributeHookTests` 5. The phase 4 demo passes.
Template file lines: `TemplateLoader` 318, `AttributeContext` 296, and the seven other files unchanged, each
under 400.

**Plants:**

- Plant 9 (`TemplateReader.Elements.RunHook` iterates the handlers in reverse): exit 2, `error CS` 0, failed 1 —
  `TheHandlersRunInTheOrderOfTheAdd`, exactly the named test. Log `p9.log`; original
  `TemplateReader.Elements.cs.p9.orig`.
- Plant 10 (`TemplateLoader.Register` skips the duplicate-name check and replaces through the indexer): exit 2,
  `error CS` 0, failed 1 — `RegisterChecksTheTypeInfo`, exactly the named test; the second `Gauge` call then
  returns without a throw, so `Assert.ThrowsAny` fails. Log `p10.log`; original `TemplateLoader.cs.p10.orig`.
- Plant 11 (`ReadElement` runs step 8 before step 7): exit 2, `error CS` 0, failed 1 —
  `TheHookRunsAfterTheRowsOfTheElement`, exactly the named test; the handler then reads the name, the classes,
  and the value before the rows write them. Log `p11.log`; original `TemplateReader.Elements.cs.p11.orig`.
- After each restore `cmp` found no difference. After plant 11 the rebuild read 0 warnings, 0 errors, and the
  re-run of the solution read exit 0 with 3084, 0 failed, 3 skipped (`p11-restore.log`).

**Docs:** section 11 of `technical-design.md` now holds **Custom element types** (the `Gauge` `Register` call,
the base rows, the checks, and the copy rule) and **The attribute hook** (the order, the handled rule,
`AddError`, and the binding-attributes line for `command` and `bind-text`). `grep -n "Register("
BuiltInElements.cs` shows eight `loader.Register(new ElementTypeInfo` calls, so the built-in types still go
through the public `Register`.

**Open questions:**

- None blocking. The task text of the brief names the handler-position test
  `AHandlerErrorHasThePositionOfItsAttribute`, and the plan names it
  `AHandlerErrorHasThePositionOfTheAttribute`; the test follows the plan. Recorded, because the verifier could
  grep either name.

### Blast radius of group 1

`src/BrutalUi.Markup/Template/` is new. Nothing existing references it. `InternalsVisibleTo` for `BrutalUi.Markup.Tests` already exists in `BrutalUi.Markup.csproj`, so `RegisteredType` and `TemplateReader` are readable from the test project. The only shared file that changed is `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj` (two `Compile` links). `TreeCompare.cs` and `RecordingRenderer.cs` now compile into two assemblies: `BrutalUi.Core.Tests` and `BrutalUi.Markup.Tests`. Both files declare `namespace BrutalUi.Core.Tests;`, and a test file reads them through `using BrutalUi.Core.Tests;`, as `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs` does today. The link adds no new type to any referenced assembly, so no name clash appears.

### Migration risks and follow-up work

- `Rgba8` and `float2` appear in no phase 1 signature: the `TryRead*` methods arrive in task 2.2. Phase 2 must add the `using` lines then (`Brutal.Numerics`, and the `Rgba8` namespace), and add `using BrutalUi.Core;` to any file that names them.
- The `AttributeContext` name and value getters read `Reader.Attributes[_index]`, so a wrong index throws `ArgumentOutOfRangeException`, not a silent default. Group 2 fills that list in step 4 of `TemplateReader.ReadElement`, and `AttributeContext` never changes.
- Group 1 wrote the docs of `ElementTypeInfo` and `TemplateLoadResult` with `<c>TemplateLoader</c>` placeholders, because the type did not exist yet. Task 1.7 switched them to `<see cref>`.
- Closed: group 1 recorded that no test exercised the new types. `SectionElevenTemplateTests` now covers `TemplateLoader`, `TemplateLoadResult`, `TemplateError`, `ElementTypeInfo`, `AttributeRow`, the three delegates, `AttributeContext`, and `ElementCreateArgs` on their main paths.
- Open question: whether `ElementCreateArgs.TryGet` should also accept a `Set` row name in phase 1 (it returns `false` today) or reject it earlier. Task 4.2 decides, and the plan states the `ArgumentException` rule. Left as the plan says: no guess.

### Phase 5 group 1 (tasks 5.1 to 5.4)

**Decision: one join helper for the two text rows.** `BuiltInElements.TryReadText(ElementCreateArgs, out string?, out LString?)` reads `text` and `text-key` by the rule of A4. It returns `false` after an `AddError`, and the factory returns `null`. With `text-key` present, the helper builds `new LString(key, text ?? string.Empty)` and nulls the text out, so a create info never holds both, as the `Label` and `Button` constructors require. Without `text-key`, the plain `text` path stays byte-for-byte as before. The former `ReadCreateText` helper is gone; `CreateLabel` and `CreateButton` are two small factories that wrap the helper.

**Decision: the empty key check sits in the helper.** `LString` rejects an empty id with an `ArgumentException` at its constructor. The helper checks `Value.Length == 0` first and adds `InvalidValue` at the `text-key` position, so no exception leaves `Load` (A6, Risk 4). The message is `'' is an empty text key. Write a key with one or more characters.`

**Decision: the `TextField` localized path.** `CreateTextField` reads `capacity` first, so the `MissingAttribute` and bad-value rules do not change. It then calls `TryReadText`. With an `LString`, it makes the field with `Capacity` alone and calls `TextField.SetText(LString?)`, as `ElementBuilder.Text(field, LString?)` does: the field shows the localized text, cut at the capacity on a code point boundary, and keeps no `LString`. The test fixes `capacity="3" text="Score"` to the bytes `Sco`.

**Decision: `TreeCompare` value rule and message members.** `LocalizedDiff` takes `LString?` values. Two values are equal when they are the same reference, or when both are set with the same `Id.ToString()` and the same `Template` (ordinal). The message names the part: `LocalizedText key`, `LocalizedText template`, or a bare `LocalizedText` for a set-versus-none pair. The remarks of `FirstDifference` now state the rule.

**Test note: the template-difference case needs equal bytes.** `CompareWidget` runs `TextDiff` before `LocalizedDiff`, and a label encodes the localized bytes at construction, so two `LString` objects with different templates show different bytes and give `Text differs` first. The Core test localizes the second object to the first template text, so the bytes match and the `LocalizedText template` message shows. That is a test construction, not a rule change: a key difference and a set-versus-none pair already show equal bytes.

**Counts measured at the group close.** Solution 3091, 0 failed, 3 skipped. Markup 636 (baseline 578, +58 across five phases), Core 1840 (+1). The brief wrote "expect 3090 at the end of this group"; the brief also wrote that task 5.5 adds no test, and the plan's own close is 3091 (3032 + 58 + 1), so 3091 at the end of group 1 is the plan-consistent number. Recorded as measured. `BuiltInElements.cs` sits at 399 lines: the group trimmed its doc prose to keep the file under 400.

**Open questions:**

- None blocking. Group 2 owns task 5.5: section 11 rows and rule, the changelog line, plants 12 and 13, and the DoD greps.

### Phase 5 group 2 (task 5.5)

**Decision: section 11 additions match the code and The Written Table of the plan.** The `Label`, `Button`, and `TextField`
factory cells of The Written Table now read "or `LocalizedText` with `text-key` (A4)" and their own-rows cells list `text-key`
(Create) after `text`. The Value Forms gained one `text-key` bullet (A4): key + text gives `new LString("k", "t")`, key alone
gives `new LString("k", "")`, and an empty key gives `InvalidValue` at `text-key`; the factory sets one of `Text` or
`LocalizedText`. The Whitespace bullet now names `name`, `text`, and `text-key` as kept as-is, matching `BuiltInElements.TryReadText`,
which passes `textContext.Value` and `keyContext.Value` unchanged.

**Decision: one changelog line, modelled on the builder-api line.** `CHANGELOG.md` `## [Unreleased]` → `### Added` gained one
line tagged `(markup/xml-element-loader)` after the builder-api line. It names `TemplateLoader`, the per-loader type registry
(`Register`), the written table for the eight built-in types with the four base rows, the `text-key` join (A4), the attribute hook
(`AddAttributeHandler`), and the positioned error report (`TemplateError`, `TemplateErrorKind`) with the line and UTF-16 column.
`grep -n 'markup/xml-element-loader' CHANGELOG.md` gives exactly one line.

**Count note (honest reading, no fudge).** The build reads 0 warnings and 0 errors. The solution reads **3091, 0 failed, 3 skipped**.
The DoD item 2 states the target as "a total 59 above the baseline of `context.md` (3091 at planning time; 3032 + 59)". The measured
total equals that target exactly: baseline 3032 + 59 = 3091. The earlier group-1 note that "the brief expected 3090" is a brief-wording
slip, not a count deviation: the plan's own close and the DoD both say 3091, and task 5.5 adds no test, so 3091 with 3 skipped is the
correct closed number. Per-class and Core totals below confirm the split: Markup 636 (+58 over baseline 578), Core 1840 (+1).

**Plants (group 2):**

- Plant 12 (`BuiltInElements.TryReadText` ignores `text`: the helper builds `new LString(keyContext.Value, string.Empty)` instead of
  `new LString(keyContext.Value, text ?? string.Empty)`): exit code 2, `error CS` count 0, failed 3 —
  `TextKeyWithTextGivesALocalizedTextWithTheDefault` (the named test), plus `TextKeyWorksOnAButtonAndATextField` and
  `ALocalizedTemplateEqualsTheBuilderTree`. Blast radius, expected: the three tests all read the `text` default that the join helper
  drops, so each localized tree loses the template text and shows a difference. Log `src/BrutalUi.Markup/obj/plantscratch/p12.log`;
  original `plantscratch/BuiltInElements.cs.p12.orig`. Restored with `cp`, `cmp` clean, and the re-run read exit 0 with 3091, 0 failed,
  3 skipped (`p12-restore.log`).
- Plant 13 (`TreeCompare.LocalizedDiff` returns `null` always, with the parameters referenced by a discard): exit code 2, `error CS`
  count 0, failed 1 — `ALocalizedTextDifferenceGivesThePath`, exactly the named test, no blast radius (a null diff makes more pairs
  compare equal, so no other compare test breaks). Log `p13.log`; original `plantscratch/TreeCompare.cs.p13.orig`. Restored with `cp`,
  `cmp` clean, and the re-run read exit 0 with 3091, 0 failed, 3 skipped (`p13-restore.log`).

**Verification Method sweep (all 12 items, 2026-11-13 07:11 UTC):**

1. Build: `0 Warning(s)`, `0 Error(s)` — pass.
2. Solution: total 3091, failed 0, succeeded 3088, skipped 3 — pass (baseline 3032 + 59).
3. Per-class Markup totals, each `failed: 0`: `*SectionElevenTemplateTests` 6, `*BuiltInTypeTests` 9, `*TemplateValueTests` 10,
   `*EightTypeTreeTests` 2, `*LoadedTreeAllocationTests` 2, `*TemplateErrorTests` 12, `*CustomTypeTests` 6, `*AttributeHookTests` 5,
   `*LocalizedTextTests` 6 — pass.
4. Core `*TreeCompareTests`: total 6, `failed: 0` — pass.
5. `git diff --stat cf13f19 -- src/BrutalUi.Core src/BrutalUi.Framework src/BrutalUi.Markup/Style src/BrutalUi.Markup/BrutalUi.Markup.csproj samples Directory.Packages.props`: no output — pass.
6. `grep -rnE 'System\.Reflection|Activator|Enum\.Parse|GetType\(\)' src/BrutalUi.Markup/Template`: no output — pass.
7. `git diff cf13f19 -- src | grep -E '^\+.*public ... (class|struct|interface|enum|record|delegate) '`: 13 lines — pass (the 13 public
   types: `TemplateLoader`, its nested `CreateInfo`, `TemplateLoadResult`, `TemplateError`, `TemplateErrorKind`, `ElementTypeInfo`,
   `AttributeRow`, `AttributeRowKind`, `ElementFactory`, `AttributeSetter`, `AttributeHandler`, `AttributeContext`, `ElementCreateArgs`).
8. `wc -l src/BrutalUi.Markup/Template/*.cs`: 9 files (after the phase 3 `TemplateReader` split — the DoD "8 files" text predates the
   split), each under 400 (max `BuiltInElements` 399) — pass on the "each under 400" rule; deviation from "8 files" is the phase 3 split,
   already recorded.
9. `grep -n 'ui:Panel' docs/technical-design.md`: no output — pass. `grep -n 'LoadedTreeAllocationTests' docs/technical-design.md`:
   line 1711, the section 14 twenty-first gate bullet — pass. `grep -n 'markup/xml-element-loader' CHANGELOG.md`: one line (54) — pass.
10. Each of the 8 kinds `NotWellFormed UnknownElement UnknownAttribute InvalidValue MissingAttribute UnknownTexture UnexpectedText
    UnexpectedChild` has at least one test file with `TemplateErrorKind.<Kind>` — pass.
11. The section 11 doc template (lines 1188 to 1191) equals the `SectionElevenTemplate` raw literal (lines 27 to 30, minus the 8-space
    literal indent) character for character: `diff` gave no output — pass.
12. This `context.md` records plants 1 to 13, each with a non-zero exit code, 0 `error CS` lines, and the expected failed test names —
    pass (plants 1 to 11 from the earlier groups; 12 and 13 above).

**Open questions:**

- None. The feature closes at 38/38. `/update-feature` and `/update-master` run at the cross-phase review.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- None in phase 1 group 1: no window, no sample, and no manual step.
- None in phase 1 group 2: the demo is a test run, and `SectionElevenTemplateTests` covers it. No window, no sample, and no manual step.
- [ ] Phase 1 demo: the developer runs the tests, and the section 11 template loads and equals the builder tree: `BrutalUi.Markup.Tests --filter-class '*SectionElevenTemplateTests'` reads a total of 6 with `failed: 0`, and `TheSectionElevenTemplateEqualsTheBuilderTree` is one of them.
- [ ] Phase 2 demo: the developer runs `BrutalUi.Markup.Tests --filter-class '*EightTypeTreeTests'` and reads a total of 2 with `failed: 0`, so the eight-type template equals the builder tree in two attribute orders. No window, no sample, and no manual step: the gate `LoadedTreeAllocationTests` is a test run too.
- [ ] Phase 3 demo: the developer runs `BrutalUi.Markup.Tests --filter-class '*TemplateErrorTests'` and reads a total of 12 with `failed: 0`. A template with three faults gives three errors, each with its line and its column. No window, no sample, and no manual step.
- [ ] Phase 4 demo: the developer runs `BrutalUi.Markup.Tests --filter-class '*CustomTypeTests'` and reads a total of 6 with `failed: 0`. A template with a custom `Gauge` element loads and equals the builder tree. No window, no sample, and no manual step.

- [ ] Phase 5 demo: the developer runs `BrutalUi.Markup.Tests --filter-class '*LocalizedTextTests'` and reads a total of 6 with `failed: 0`. `<ui:Label text-key="hud.score" text="Score" />` gives a label with the key `hud.score` and the default text `Score`. No window, no sample, and no manual step.

---

## Session Notes

### 2026-11-13 03:33
- `/autorun-feature markup/xml-element-loader` started. Preconditions passed: epic `markup` (feature 2 of 6), the plan exists, builder-api is complete at `cf13f19` and gives the property surface and `TreeCompare`.
- Created the branch `feat/markup/xml-element-loader` from `main` at `cf13f19`. No commit, no push.
- `/start-feature` scaffolded `tasks.md`, `inbox.md`, and this file. Flipped `implementation.md` to In Progress.

### 2026-11-13 03:55 UTC - phase 1 group 1 (advanced agent)
- Task 1.1: recorded the real baseline in the "Base and Counts" section, the plan sections in "Recorded Plan Sections", and the tool notes.
- Tasks 1.2 to 1.5: wrote `TemplateError.cs`, `TemplateLoadResult.cs`, `ElementTypeInfo.cs`, `AttributeContext.cs`, and the placeholder `TemplateReader.cs`. See the decisions above.
- Task 1.6: added the two `Compile` links to `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` → total 3032, failed 0, succeeded 3029, skipped 3. Markup 578, Core 1839. No test changed and no test added in this group.
- The public type count of the DoD grep reads 11 for now: `TemplateLoader` and its nested `CreateInfo` come in task 1.7, which makes 13.
- Files: each new file is under 220 lines, so each stays under the 400-line rule.

### 2026-11-13 04:20 UTC - phase 1 group 2 (advanced agent)
- Task 1.7: wrote `TemplateLoader.cs` (186 lines): `Namespace`, `CreateInfo`, both constructors, `Register`, `AddAttributeHandler`, and both `Load` overloads. Switched the `<c>` doc placeholders to `<see cref>`.
- Task 1.8: replaced the placeholder `TemplateReader.cs` with the walk (376 lines): the settings, the frame stack, steps 3, 4, 6, 7, 8, and 9, the `XmlException` catch with the clamp, and `Success` or `Failure`.
- Task 1.9: wrote `BuiltInElements.cs` (116 lines): `BaseRows` (`name`, `class`), `RegisterAll` for `Element`, `Label` (leaf) and `Button`, each with the `text` create row, and the class-list split loop.
- Task 1.10: wrote `tests/BrutalUi.Markup.Tests/Template/SectionElevenTemplateTests.cs` (184 lines) with the six named tests. Measured Risk 1: `command` sits at line 3, column 59, and the element position is the first character of the qualified name.
- Task 1.11: edited `docs/technical-design.md` section 11 (`<ui:Element>`, the written-table sentence, the loader API block, the binding-attributes sentence). Ran the two plants, then restored both files and verified them with `cmp`.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` → total 3038, failed 0, succeeded 3035, skipped 3. Markup 584 (+6), Core 1839 (unchanged).
- Demo: `tests/BrutalUi.Markup.Tests/bin/Debug/net10.0/BrutalUi.Markup.Tests --filter-class '*SectionElevenTemplateTests'` → total 6, failed 0. Phase 1 acceptance met: no existing test needed an edit, `grep -n "ui:Panel" docs/technical-design.md` gives no output, and the doc template equals the test template character for character.
- Tool note: the probe projects lived in the git-ignored `src/BrutalUi.Markup/obj/plantscratch/probe/` and `probe2/`, and were removed after the measurement. Logs and the plant originals stay there.

### 2026-11-13 04:42 UTC - phase 2 group 1 (standard agent)
- Task 2.1: wrote `TemplateValues.cs` (273 lines): `IsXmlWhitespace`, `Trim`, `TryParseBool`, `TryParseWholeNumber`, `TryParseNumber`, `TryParseNumbers`, `TryParseColor`, `NextName`. See the decisions above.
- Task 2.2: added the seven `TryRead*` methods to `AttributeContext.cs` (144 to 286 lines).
- Task 2.3: `TemplateLoader.cs` gained `TryGetTexture`, the `CreateInfo.Textures` null rule in its doc, and lost the internal `Textures` accessor (186 to 199 lines). `Register` already stored the `CreateRequired` indices through `RegisteredType`.
- Task 2.4: `TemplateReader.cs` gained step 5 in `Create` and the texture forward (376 to 398 lines after the trims). Checked the phase 1 work: the null-parent frame and the step 6 throw existed; not duplicated.
- Task 2.5: `BuiltInElements.cs` gained `pickable`, `focusable`, and the five types with their rows, checks, and factories (116 to 356 lines).
- Task 2.6: built clean (`0 Warning(s)`, `0 Error(s)`), wrote the `probe-p2` smoke project, ran it: `SMOKE OK`. Ran the solution tests: 3038, 0 failed, 3 skipped. Marked tasks 2.1 to 2.6 in `tasks.md`.
- Deviation note: task 2.6 names "a quick local run of the test binary"; the smoke run used a scratch console project that calls the same public API, because the new test classes only exist in group 2. The check the task asks — no exception leaves `Load` for any bad value — is the one the probe makes.

### 2026-11-13 05:28 UTC - phase 2 group 2 (standard agent)
- Task 2.7: wrote `tests/BrutalUi.Markup.Tests/Template/BuiltInTypeTests.cs` (9 tests). All pass.
- Task 2.8: wrote `TemplateValueTests.cs` (10 tests) with the position helpers. All pass. Two test changes came out
  of plant 3: the culture test moved from `Slider.value` to `ScrollView.scroll-speed`, and
  `TheAttributeOrderOfTheSourceDoesNotChangeTheTree` now compares the two loaded trees only.
- Task 2.9: wrote `EightTypeTreeTests.cs` (2 tests). The phase 2 demo passes.
- Task 2.10: wrote `LoadedTreeAllocationTests.cs` (2 tests). The gate measures 0 bytes over 100 measured steps
  after 16 warm-up steps.
- Task 2.11: section 11 gained The written table, The value forms, and the seven `TryRead*` members; section 14
  gained the twenty-first gate bullet. Ran plants 3, 4, and 5, each with `error CS` 0 and the named failures, and
  restored each file with `cp` plus a `cmp` check.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` → total 3061, failed 0, succeeded 3058, skipped 3. Markup 607.
- Marked tasks 2.7 to 2.11 in `tasks.md`. Progress reads 22/38.
- Fix 1 (phase 2 verifier): the gate 7 docs grep failed on the forward-note bullet "- The localised text row
  `text-key` comes with the localized-text work." in `docs/technical-design.md`. The bullet is deleted; phase 5
  adds the `text-key` row and its rule. `grep -n "text-key" docs/technical-design.md` now gives no output.

### 2026-11-13 06:00 UTC - phase 3 group 1 (standard agent)
- Task 3.1: steps 1 and 2 in `ReadElement` (namespace `UnknownElement`, leaf-child `UnexpectedChild`, each with a
  null-parent subtree), the prefixed-attribute `UnknownAttribute` in `ReadAttributes` (step 4), and `UnexpectedText`
  for text and CDATA in the walk. `SignificantWhitespace` and skipped nodes give no error. Split
  `TemplateReader.cs` into `TemplateReader.cs` (177) + `TemplateReader.Elements.cs` (323) to keep each under 400.
- Task 3.2: checked the phase 1 `Math.Max(1, ...)` clamp (kept), added the `NotWellFormed` message form, and added the
  stable `SortErrors`. Folded `NoRootElement` into the load tail.
- Task 3.3: `TemplateLoader.TypeNames` (rebuilt after each `Register`), used in the step 3 `UnknownElement` message;
  `RegisteredType.AttributeNames` used in the step 8 `UnknownAttribute` message.
- Task 3.4: wrote `tests/BrutalUi.Markup.Tests/Template/TemplateErrorTests.cs` with the 12 named tests. Two fixes came
  out of the first run: the default-namespace root is an `Element`, not a `Label`; and `ToString` asserts with
  `StartsWith` on the full prefix. All 12 pass.
- Task 3.5: section 11 gained **The error report** (the continue, position, column, and namespace rules, the kind
  table, and one `ToString` example line).
- Task 3.6: build `0 Warning(s)`, `0 Error(s)`; solution 3073, 0 failed, 3 skipped; Markup 619. Ran plants 6, 7, and 8,
  each with `error CS` 0 and the named failure, restored with `cp`, and checked each with `cmp`. Marked tasks 3.1 to 3.6
  in `tasks.md`. Progress reads 28/38.

### 2026-11-13 06:35 UTC - phase 4 group 1 (standard agent)
- Task 4.1: `TemplateLoader.Register` runs the full checks of the entry, all-or-nothing (230 to 318 lines, `using
  System.Xml` new). `IsValidXmlName` wraps `XmlConvert.VerifyNCName` and turns a bad or empty name into one
  `ArgumentException` with `ParamName` `info`. The `AddAttributeHandler` null check was already in from phase 1.
- Task 4.2: `ElementCreateArgs.TryGet` throws `ArgumentException` for a name that is not a create row of the type
  (286 to 296 lines). `false` stays for a create row whose attribute is absent.
- Task 4.3: wrote `tests/BrutalUi.Markup.Tests/Template/CustomTypeTests.cs` (292 lines) with the six named tests and
  the `Gauge` class. All 6 pass. One fix came out of the first run: `VerifyNCName("")` throws `ArgumentException`,
  not `XmlException`, so the helper catches both (decision above).
- Task 4.4: wrote `AttributeHookTests.cs` (189 lines) with the five named tests. All 5 pass.
- Task 4.5: section 11 gained **Custom element types** and **The attribute hook**, and the `Register` and `TryGet`
  comment lines in the API block name the faults. Ran plants 9, 10, and 11, each with `error CS` 0 and exactly the
  named failure, restored with `cp`, and checked each with `cmp`. The `grep -n "Register("
  src/BrutalUi.Markup/Template/BuiltInElements.cs` check shows the eight built-in `loader.Register` calls.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` → total 3084, failed 0, succeeded 3081, skipped 3. Markup 630
  (+11), Core 1839 (unchanged). Demo: `*CustomTypeTests` total 6 and `*AttributeHookTests` total 5, each failed 0.
- Marked tasks 4.1 to 4.5 in `tasks.md`. Progress reads 33/38.
- `git add -N` ran for the two new files under `tests/`. No commit, no push.

### 2026-11-13 07:00 UTC - phase 5 group 1 (standard agent)
- Task 5.1: `BuiltInElements.cs` gained the `text-key` create row after `text` on `Label`, `Button`, and `TextField`,
  the `TryReadText` join helper of A4, and the `CreateLabel` and `CreateButton` factories; `TextField` takes the
  `SetText(LString?)` path. `ReadCreateText` is gone. The file sits at 399 lines (docs trimmed under the 400 rule).
- Task 5.2: `TreeCompare.LocalizedDiff` takes `LString?` values and compares by same reference, or same id text and
  same template (ordinal). The messages name the key or the template. The remarks line of `FirstDifference` states
  the value rule. `using Brutal.Localization;` is new; the helper still uses no test framework type.
- Task 5.3: `TreeCompareTests` gained `ALocalizedTextDifferenceGivesThePath` (+1). One fix came out of the first run:
  the template-difference labels need equal shown bytes, because `TextDiff` runs before `LocalizedDiff`, so the
  second `LString` is localized to the first template text (see the decision above).
- Task 5.4: wrote `tests/BrutalUi.Markup.Tests/Template/LocalizedTextTests.cs` (172 lines) with the six named tests.
  All 6 pass. One fix came out of the first build: the file needs `using BrutalUi.Core.Tests;` for `TreeCompare`
  and `RecordingRenderer`. `git add -N` ran for the new file. No commit, no push.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` → total 3091, failed 0, succeeded 3088, skipped 3. Markup 636
  (+6, +58 over the baseline 578), Core 1840 (+1). Framework 332 with `failed: 0`: the `TreeCompare.cs` change
  regresses no Framework test. Per class: `*LocalizedTextTests` 6 failed 0, `*TreeCompareTests` 6 failed 0.
- Count note: the brief expected 3090 at this point; 3091 is the plan's own close (3032 + 58 + 1), and task 5.5
  adds no test. Recorded as measured, per the brief's re-check rule.
- Marked tasks 5.1 to 5.4 in `tasks.md`. Progress reads 37/38. Task 5.5 (group 2) is open.

### 2026-11-13 07:11 UTC - phase 5 group 2 (standard agent)
- Task 5.5: `docs/technical-design.md` section 11 gained the `text-key` row on `Label`, `Button`, and `TextField` (The Written
  Table) and the A4 rule (The Value Forms), plus `text-key` in the Whitespace bullet. `CHANGELOG.md` gained one
  `(markup/xml-element-loader)` line under `## [Unreleased]`. No source, no test, and no public type changed.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` → total 3091, failed 0, succeeded 3088, skipped 3 (baseline 3032 + 59, the DoD
  target exactly).
- Plant 12 (`TryReadText` ignores `text`): exit 2, `error CS` 0, failed 3 — `TextKeyWithTextGivesALocalizedTextWithTheDefault` plus
  two expected localized-test breaks. Plant 13 (`LocalizedDiff` returns `null` always): exit 2, `error CS` 0, failed 1 —
  `ALocalizedTextDifferenceGivesThePath`. Each restored with `cp`, `cmp` clean, and the re-run read exit 0 with 3091, 0 failed, 3
  skipped.
- Verification Method: all 12 items ran; each result recorded in the "Phase 5 group 2" section. No fudge: item 2 reads the honest
  3032 + 59 = 3091, and item 8 reports the 9 files after the phase 3 split, each under 400.
- No new file, so no `git add -N` ran here. No commit, no push.
- Marked task 5.5 in `tasks.md`. Progress reads 38/38.

### 2026-11-13 07:43 UTC - review fix CR-1 (group 1, standard agent)
- Decision (the ceiling value): the limit stays 65536, as the brief records. One field of 65536 bytes is 2048 times the
  longest real capacity in the repo (the gates use 32, and the sample grid uses 81 x 16), and it sits far below the CLR
  array limit that `new byte[capacity]` trips. Nothing in the code argues for another number, so no smaller defensible
  value replaced it. `TemplateValues.MaxTextCapacity` holds the number in one place, and the section 11 bullet and the
  message both read it.
- Shape deviation (the reason): `BuiltInElements.cs` sits at 399 lines, and the rule is each source file under 400, so a
  separate `if` block for the new check did not fit. The check joins the existing condition chain of `CreateTextField`
  as `|| !TemplateValues.CheckCapacity(capacityContext, capacity)`, which keeps the file at 399 lines. The helper holds
  the message and calls `AddError` on the context, so the semantics equal the `Slider` and `ScrollView` shape: the
  factory rejects the value before the constructor, reports `InvalidValue` at `capacity`, and returns `null`. The file
  count of `TemplateValues.cs` rose to 302 lines, still under 400.
- Source: `TemplateValues.cs` gained `MaxTextCapacity` and `CheckCapacity`. `BuiltInElements.cs` gained the one
  condition. `TryReadWholeNumber`, the whole-number form, and `src/BrutalUi.Core/` did not change.
- Tests: `ACapacityIsAWholeNumberAboveZero` gained the over-limit list `65537`, `100000000`, `2147483647`, each with the
  kind, the line, the column of `capacity`, the null root, and `Contains` on the quoted value, plus the load of
  `capacity="65536"`. No new test method and no new class: `*TemplateValueTests` reads 10 with `failed: 0`, and the
  solution reads total 3091, failed 0, succeeded 3088, skipped 3. Build: `0 Warning(s)`, `0 Error(s)`.
- Plant 14 (`CreateTextField` drops the new upper-bound condition): exit code 2, `error CS` count 0, failed 1 —
  `ACapacityIsAWholeNumberAboveZero`, exactly the named test, no blast radius. Observed failure form: an assert
  failure, not a host crash — `Assert.Null()` found the root of `capacity="65537"` (`TextField { Capacity = 65537 }`),
  because that first over-limit value now loads. The run never reached `2147483647`, so the plant showed no
  `OutOfMemoryException` and no exit 134. Log `src/BrutalUi.Markup/obj/plantscratch/p14.log`; original
  `plantscratch/BuiltInElements.cs.p14.orig`. Restored with `cp`, `cmp` clean, and the re-run read exit 0 with 3091, 0
  failed, 3 skipped (`p14-restore.log`).
- Docs: `docs/technical-design.md` section 11, The value forms, gained the sentence about the `TextField` factory and
  the 65536 ceiling in the whole-number bullet. `code-review.md` Progress now reads CR-1 checked.
- No new file, so no `git add -N` ran here. No commit, no push.

### 2026-11-13 08:05 UTC - review fix CR-2 and CR-4 (group 2, standard agent)
- CR-2 (`CustomTypeTests.RegisterChecksTheTypeInfo`): the two absolute registry reads are gone. The test now proves
  both halves through the public load. After the failed `Register` calls, `<ui:Probe xmlns:ui="brutal-ui" />` gives
  one `UnknownElement` error: the type did not land. After the good `Gauge` registration, `<ui:Gauge ... />` loads:
  the type did land. Every throw assert of the test stays.
- Deviation (the reason): the CR-2 fix text keeps the internal before/after count compare of `AssertFault`, but the
  brief acceptance requires no reference to `loader.Types` or any other internal in the file. The compare therefore
  moved to the public API: `AssertFault` reads the `UnknownElement` message of a probe load of `<ui:NoSuchType ... />`
  before and after each bad `Register` call, and asserts the two messages are equal. The message lists the registered
  type names, and `TemplateLoader.Register` rebuilds that list only after a successful insert, so an unchanged message
  proves the registry did not change. The compare holds no table knowledge, and it is stronger than the old count
  compare: it names the types, not only their number.
- CR-4: one new test, `TryGetRejectsANameThatIsNotACreateRow`. It registers the gauge-like `ProbeRow` type: a
  `segments` `CreateRequired` row and a `level` `Set` row. The factory calls `args.TryGet` with `level` — the name of
  a set row of the type — for `segments="1"`, and with `ghost` — a name with no row — for `segments="2"`. Each call
  throws `ArgumentException` out of `Load`, with `ParamName` `name` and a message that quotes the attribute name, as
  the reviewer measured.
- Review delta (the count): one new test method. `*CustomTypeTests` now reads total 7, failed 0 — was 6. The solution
  now reads **3092, 0 failed, 3 skipped** — was 3091. The plan's tables and the phase 4 and phase 5 notes keep their
  recorded 6 and 3091; this delta came from the cross-phase review after the feature close, and the verifier and the
  epic rollup read it here. The phase 4 decision above that records the `loader.Types` reads in
  `RegisterChecksTheTypeInfo` is superseded by the CR-2 change.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`. Tests: `dotnet test --solution
  brutal-ui.sln` → total 3092, failed 0, succeeded 3089, skipped 3. One run; no exit 134.
- `CustomTypeTests.cs` is 366 lines, under the 400 rule, and holds no internal of the loader.
- Docs: `code-review.md` Progress now reads CR-2 and CR-4 checked.
- No new file, so no `git add -N` ran here. No commit, no push.

### 2026-11-13 08:11 UTC - review fix CR-3 (group 3, standard agent)
- CR-3 (a) `XmlThatIsNotWellFormedStopsTheRead`: after the kind and message asserts, two equality asserts pin the
  `NotWellFormed` position of the wrong end tag: line 3, column 3. The run measured both values; they pass.
- CR-3 (b) the UTF-16 column unit: no new test method. The host is `AnAttributeOfAnotherTypeIsAnUnknownAttribute`,
  the class's most fitting test: its fault shape equals the reviewer's probe — an unknown `nope` attribute on `Label`
  after a `text` value. The test gained a second template with the astral character `😀` in `text`, on the same line
  before the bad attribute. The asserts: the kind `UnknownAttribute`, the existing `AssertAttribute` helper (its
  `AssertPosition` counts UTF-16 string indices, the same unit `IXmlLineInfo` gives), and one explicit column number.
- Measurement (the column): the reader gave column 42 — the UTF-16 index of `nope` (41) plus 1, not the rune index plus
  1 (41). The reviewer's probe measured 54 with a longer value before the fault; the templates differ, the unit rule is
  the same, and the brief's decision says to trust and record our own number.
- Count delta: none. Both asserts sit inside existing tests, so no new method and no section 11 gate note.
  `*TemplateErrorTests` reads total 12, failed 0. The solution holds **3092, failed 0, succeeded 3089, skipped 3**.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`. Tests: `dotnet test --solution
  brutal-ui.sln` → total 3092, failed 0, one run; no exit 134. The per-class run used `dotnet test --project` with no
  `--no-build`: that flag breaks the .NET 10 CLI test path in this environment.
- No production code change, no new plant, and no new file, so no `git add -N` ran here. `code-review.md` Progress
  now reads CR-3 checked. No commit, no push.

### 2026-11-13 08:20 UTC - review fix CR-5 and CR-6 (group 4, standard agent)
- CR-5 (`docs/technical-design.md:1221`): the section 11 API-block comment on `TemplateLoadResult.Errors` now reads
  "sorted by line, then column. Empty on success". The old text said "in the order of the load", which phase 3's
  `SortErrors` superseded. The code XML doc in `TemplateLoadResult.cs:32` already reads "sorted by line, then by
  column", so it needed no change. `TemplateReader.cs:41` keeps "in the order of the load": that internal list is the
  pre-sort collection, so the wording is accurate there.
- CR-6 (two sites): both `UnknownAttribute` example messages gained the `text-key` row — `docs/technical-design.md:1385`
  and the `Message` remarks example in `src/BrutalUi.Markup/Template/TemplateError.cs:107`. Both now end "...pickable,
  focusable, text, text-key." like the shipped message the reviewer measured. Doc text only, no behavior change.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)` (the doc comments compile).
  Tests: `dotnet test --solution brutal-ui.sln` → total 3092, failed 0, succeeded 3089, skipped 3 — unchanged from
  groups 1 to 3. One run; no exit 134.
- DoD grep: the public-type diff grep still prints 13 lines; a comment change moves nothing. `TemplateError.cs` is 151
  lines, far under 400.
- `code-review.md` Progress now reads CR-5 and CR-6 checked. All six findings are now applied. No new file, so no
  `git add -N` ran here. No commit, no push.

### 2026-11-13 08:40 UTC - run close (parent)
- Post-review gate verifier: PASS on all 16 items. Demo: PASS (the suite is the demo: solution 3092, failed 0, skipped 3; every per-class count green, including `*CustomTypeTests` 7 after CR-4). `code-review.md` Status now reads Resolved - 6 findings, 6 resolved.
- Inbox: 2 lines, both open bugs, both done (gate 9 intent-to-add; gate 7 `text-key` doc line, fix brief phase-2-fix1). No open lines; no bug promotion needed.
- `implementation.md` Status: Ready for Review. The branch `feat/markup/xml-element-loader` holds the whole uncommitted feature; the person commits, pushes, and opens the PR.
- Next focus: `markup/element-generator` (the written table is one replaceable file; the public `Register` is stable; the CR-1 capacity ceiling lives in the `TextField` factory and must survive the replacement).

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
