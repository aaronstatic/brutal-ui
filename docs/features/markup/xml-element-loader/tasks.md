# markup/xml-element-loader - Task Checklist

**Last Updated:** 2026-11-13 08:38 UTC
**Progress:** 38/38 tasks complete (100%)
**Agent tiers:** phase 1 is ADVANCED (opus) — it fixes the public API of the epic seam. Phases 2 to 5 are STANDARD (sonnet).

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (Public API, The Load Walk, The Written Table, The Value Forms, The Messages), "Key Technical Decisions", and "Notes" hold the rules that the tasks name. Each task's full text sits in the plan; the phase brief carries it word for word.

Shared rules for every group. Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Each group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Markup.Tests/bin/Debug/net10.0/BrutalUi.Markup.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. The test namespace is `BrutalUi.Markup.Tests.Template`. Prose in comments follows ASD-STE100; each public member has an XML doc comment (CS1591 is an error). Plants: copy the file to `src/BrutalUi.Markup/obj/plantscratch/`, change it, run the solution tests to a log, check the `error CS` count is 0 and the exit is non-zero, restore with `cp`, check with `cmp`, run again. A test host can exit 134 one time; run a second time before you treat it as a fault. Tests: pass a template as a `u8` literal to the span overload (`BRUTALCORE0011`); the `string` overload test passes a local. Each tree compare asserts `Assert.Null(TreeCompare.FirstDifference(expected, actual))`. Each error assert checks `Kind`, `Line`, `Column`, and `Contains` on the quoted text only.

**No-change rule:** `src/BrutalUi.Core/` (except nothing — it does not change), `src/BrutalUi.Framework/`, `src/BrutalUi.Markup/Style/`, `src/BrutalUi.Markup/BrutalUi.Markup.csproj`, `samples/`, `Directory.Packages.props`, and `brutal-ui.sln` do not change. Phase 5 edits `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` and `tests/BrutalUi.Core.Tests/Builder/TreeCompareTests.cs` only.

---

## Phase 1: The Section 11 Template (11/11 complete)

**Demo:** the developer runs the tests, and the section 11 template loads and equals the builder tree: `BrutalUi.Markup.Tests --filter-class '*SectionElevenTemplateTests'` reads a total of 6 with `failed: 0`, and `TheSectionElevenTemplateEqualsTheBuilderTree` is one of them.
**Needs:** none
**Agent tier:** ADVANCED (opus)

Two groups. Group 1: tasks 1.1 to 1.6 (6 files). Group 2: tasks 1.7 to 1.11 (6 files).

- [x] **1.1 Create context.md records: base commit, baseline counts, the recorded plan sections**
  - Description: Per plan task 1.1. Complete `context.md` if needed: run `git status --porcelain`; record the base commit, the solution total with the skipped count (3032 with 3 skipped at planning time), and the Markup.Tests and Core.Tests totals; record the Public API, The Load Walk, The Written Table, The Value Forms, and the Notes of the plan; record the tool notes.
  - File(s): `docs/features/markup/xml-element-loader/context.md`
  - Estimate: 6 steps

- [x] **1.2 Write TemplateError.cs**
  - Description: Per plan task 1.2. The 8-member `TemplateErrorKind : byte`, the `TemplateError` struct in the shape of `StyleParseError.cs`: internal constructor with checks, five properties, `ToString`, internal `GetKindName` switch, the column rule in the remarks.
  - File(s): `src/BrutalUi.Markup/Template/TemplateError.cs`
  - Estimate: 6 steps

- [x] **1.3 Write TemplateLoadResult.cs**
  - Description: Per plan task 1.3. In the shape of `StyleParseResult.cs`: `Root`, `Errors`, `Succeeded`, `GetRootOrThrow`, internal `Success`/`Failure`; the exception message joins `ToString` lines with LF.
  - File(s): `src/BrutalUi.Markup/Template/TemplateLoadResult.cs`
  - Estimate: 4 steps

- [x] **1.4 Write ElementTypeInfo.cs**
  - Description: Per plan task 1.4. `ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, `ElementFactory`, `AttributeSetter`, `AttributeHandler`, and internal `RegisteredType` with the row array, name-to-index dictionary, `CreateRequired` indices, and valid-names message text.
  - File(s): `src/BrutalUi.Markup/Template/ElementTypeInfo.cs`
  - Estimate: 8 steps

- [x] **1.5 Write AttributeContext.cs**
  - Description: Per plan task 1.5. The two `readonly ref struct` types with `Name`/`Value`/position/`AddError`/`TryGet` (the `TryRead*` methods come in phase 2); the doc states the ref-struct rule.
  - File(s): `src/BrutalUi.Markup/Template/AttributeContext.cs`
  - Estimate: 8 steps

- [x] **1.6 Markup tests csproj: the two Compile links**
  - Description: Per plan task 1.6. Add the `TreeCompare.cs` and `RecordingRenderer.cs` links after the `AllocationGate.cs` line. Build, and expect `0 Error(s)`.
  - File(s): `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`
  - Estimate: 3 steps

- [x] **1.7 Write TemplateLoader.cs**
  - Description: Per plan task 1.7. `Namespace`, `CreateInfo`, both constructors (texture map copy, `BuiltInElements.RegisterAll`), `Register`, `AddAttributeHandler`, both `Load` overloads; the class remarks.
  - File(s): `src/BrutalUi.Markup/Template/TemplateLoader.cs`
  - Estimate: 10 steps

- [x] **1.8 Write TemplateReader.cs**
  - Description: Per plan task 1.8. The walk of The Load Walk: settings, frame stack, steps 3, 4, 6, 7, 8, 9, the `XmlException` catch, `Success`/`Failure`; the attribute list with `IXmlLineInfo` positions.
  - File(s): `src/BrutalUi.Markup/Template/TemplateReader.cs`
  - Estimate: 12 steps

- [x] **1.9 Write BuiltInElements.cs (phase 1 rows)**
  - Description: Per plan task 1.9. `BaseRows` (`name`, `class`) and `RegisterAll` for `Element`, `Label` (leaf, `text` Create), `Button` (`text` Create); static lambdas; the written-table remarks.
  - File(s): `src/BrutalUi.Markup/Template/BuiltInElements.cs`
  - Estimate: 8 steps

- [x] **1.10 Write SectionElevenTemplateTests.cs with 6 tests**
  - Description: Per plan task 1.10. The six named tests; confirm the column 59 of `command` against `IXmlLineInfo` and record the column rule in `context.md` (Risk 1).
  - File(s): `tests/BrutalUi.Markup.Tests/Template/SectionElevenTemplateTests.cs`
  - Estimate: 12 steps

- [x] **1.11 Edit technical-design.md section 11; run plants 1 and 2**
  - Description: Per plan task 1.11. `<ui:Panel>` to `<ui:Element>`, the written-table sentence, the loader API block (phase 1 members), the binding-attributes sentence. Run build and tests (expect Markup +6); run plants 1 and 2; record counts, plants, and demo state in `context.md`.
  - File(s): `docs/technical-design.md`, `docs/features/markup/xml-element-loader/context.md`
  - Estimate: 10 steps

**Acceptance (plan phase 1):** the 6 new tests pass with no edit of an existing test; plants 1 and 2 fail as named; `grep -n "ui:Panel" docs/technical-design.md` is empty; the section 11 template equals the test template text character for character.

---

## Phase 2: The Eight Built-In Types (11/11 complete)

**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*EightTypeTreeTests'` and reads a total of 2 with `failed: 0`. The eight-type template equals the builder tree in two attribute orders.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

Two groups. Group 1: tasks 2.1 to 2.6 (6 files). Group 2: tasks 2.7 to 2.11 (6 files).

- [x] **2.1 Write TemplateValues.cs**
  - Description: Per plan task 2.1. `IsXmlWhitespace`, `Trim`, `TryParseBool`, `TryParseWholeNumber`, `TryParseNumber`, `TryParseNumbers`, `TryParseColor` on `ReadOnlySpan<char>`, plus `NextName` moved from the `class` setter; follow The Value Forms; allocate nothing.
  - File(s): `src/BrutalUi.Markup/Template/TemplateValues.cs`
  - Estimate: 10 steps

- [x] **2.2 AttributeContext.cs: the seven TryRead methods**
  - Description: Per plan task 2.2. Each reads `Value` through `TemplateValues` and adds `InvalidValue` (or `UnknownTexture`) on a fault; `TryReadTexture` reads the copied map.
  - File(s): `src/BrutalUi.Markup/Template/AttributeContext.cs`
  - Estimate: 8 steps

- [x] **2.3 TemplateLoader.cs: TryGetTexture, CreateRequired storage, doc**
  - Description: Per plan task 2.3. Add `internal bool TryGetTexture`; let `Register` store the `CreateRequired` indices; add the texture rule to the `CreateInfo.Textures` doc.
  - File(s): `src/BrutalUi.Markup/Template/TemplateLoader.cs`
  - Estimate: 5 steps

- [x] **2.4 TemplateReader.cs: step 5, the null-factory rule, subtree skip**
  - Description: Per plan task 2.4. `MissingAttribute` for each missing `CreateRequired` row in table order with no factory call; the `InvalidOperationException` rule for a null factory with no error; read a failed element's subtree with a null parent.
  - File(s): `src/BrutalUi.Markup/Template/TemplateReader.cs`
  - Estimate: 8 steps

- [x] **2.5 BuiltInElements.cs: the five remaining types**
  - Description: Per plan task 2.5. Add `pickable`/`focusable` to `BaseRows`; register `Toggle`, `Slider`, `TextField`, `ScrollView`, `Image` with the rows, leaf flags, range checks, and the explicit `scale-mode` switch of The Written Table.
  - File(s): `src/BrutalUi.Markup/Template/BuiltInElements.cs`
  - Estimate: 12 steps

- [x] **2.6 Scratch-template smoke run**
  - Description: Per plan task 2.6. Build; write a scratch template with each type and each row; load it in a quick local run of the test binary; check no exception leaves `Load` for each bad value; record in `context.md`.
  - File(s): `docs/features/markup/xml-element-loader/context.md`
  - Estimate: 6 steps

- [x] **2.7 Write BuiltInTypeTests.cs with 9 tests**
  - Description: Per plan task 2.7. The nine named row tests, including the slider range-before-value order and the texture-map copy rule.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/BuiltInTypeTests.cs`
  - Estimate: 10 steps

- [x] **2.8 Write TemplateValueTests.cs with 10 tests**
  - Description: Per plan task 2.8. The ten value-form and range-check tests; each fault asserts kind, line, column, and `Root` null; the culture test restores `CurrentCulture` in `finally`.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/TemplateValueTests.cs`
  - Estimate: 12 steps

- [x] **2.9 Write EightTypeTreeTests.cs with 2 tests**
  - Description: Per plan task 2.9. The eight-type tree against the builder tree, and the reversed attribute order.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/EightTypeTreeTests.cs`
  - Estimate: 10 steps

- [x] **2.10 Write LoadedTreeAllocationTests.cs with 2 tests**
  - Description: Per plan task 2.10. The 0-byte gate over 16 warm-up + 100 measured steps in the style of `ParsedSheetAllocationTests`, and the real-work test.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/LoadedTreeAllocationTests.cs`
  - Estimate: 12 steps

- [x] **2.11 Edit technical-design.md sections 11 and 14; run plants 3 to 5**
  - Description: Per plan task 2.11. The Written Table, The Value Forms, the phase 2 API members, the section 14 bullet; build and tests (expect Markup +23); plants 3, 4, 5 (plant 5 touches `Image.cs` only under the plant rule, restore and `git diff --stat` check); record in `context.md`.
  - File(s): `docs/technical-design.md`, `docs/features/markup/xml-element-loader/context.md`
  - Estimate: 10 steps

**Acceptance (plan phase 2):** the 23 new tests pass; plants 3, 4, 5 fail as named with `error CS` 0 and `cmp`-clean restores; `git diff --stat -- src/BrutalUi.Core` is empty after the restore; the allocation test measures 0 bytes.

---

## Phase 3: The Positioned Errors (6/6 complete)

**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*TemplateErrorTests'` and reads a total of 12 with `failed: 0`. A template with three faults gives three errors, each with its line and its column.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet)

One group, 6 tasks (5 files).

- [x] **3.1 TemplateReader.cs: steps 1 and 2, prefixed attributes, text nodes**
  - Description: Per plan task 3.1. Namespace rejection, leaf child rejection, prefixed-attribute `UnknownAttribute`, `UnexpectedText` for text and CDATA, no error for `SignificantWhitespace`.
  - File(s): `src/BrutalUi.Markup/Template/TemplateReader.cs`
  - Estimate: 8 steps

- [x] **3.2 TemplateReader.cs: position clamp, stable error sort, NotWellFormed message**
  - Description: Per plan task 3.2. Clamp the `XmlException` position to 1 or more; stable sort by line then column with an index tie-break; the `NotWellFormed` message form.
  - File(s): `src/BrutalUi.Markup/Template/TemplateReader.cs`
  - Estimate: 6 steps

- [x] **3.3 TemplateLoader.cs: TypeNames and the full messages**
  - Description: Per plan task 3.3. `internal string TypeNames` rebuilt after each `Register`; the `UnknownElement` and `UnknownAttribute` messages of The Messages.
  - File(s): `src/BrutalUi.Markup/Template/TemplateLoader.cs`
  - Estimate: 6 steps

- [x] **3.4 Write TemplateErrorTests.cs with 12 tests**
  - Description: Per plan task 3.4. The twelve named error tests, including the continue rule, the stop rule, the DTD rule, the default-namespace form, and `GetRootOrThrow`.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/TemplateErrorTests.cs`
  - Estimate: 12 steps

- [x] **3.5 Edit technical-design.md section 11: The error report**
  - Description: Per plan task 3.5. The continue rule, the position rule, the column rule, the namespace rule, the kind table, and one `ToString` example line.
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] **3.6 Run build, tests, plants 6 to 8; record**
  - Description: Per plan task 3.6. Expect Markup +12; plants 6, 7, 8 as named; record counts and plants in `context.md`.
  - File(s): `docs/features/markup/xml-element-loader/context.md`
  - Estimate: 8 steps

**Acceptance (plan phase 3):** the 12 new tests pass; plants 6, 7, 8 fail as named; each of the 8 kinds has a test asserting kind, line, and column.

---

## Phase 4: Custom Types and the Hook (5/5 complete)

**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*CustomTypeTests'` and reads a total of 6 with `failed: 0`. A template with a custom `Gauge` element loads and equals the builder tree.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet)

One group, 5 tasks (6 files).

- [x] **4.1 TemplateLoader.cs: the full Register checks**
  - Description: Per plan task 4.1. `XmlConvert.VerifyNCName`, null factory, row checks, duplicate checks, base-name clash, registered-name clash, all-or-nothing; `AddAttributeHandler` null check.
  - File(s): `src/BrutalUi.Markup/Template/TemplateLoader.cs`
  - Estimate: 8 steps

- [x] **4.2 AttributeContext.cs: TryGet rejects a set row**
  - Description: Per plan task 4.2. `ElementCreateArgs.TryGet` throws `ArgumentException` for a name that is not a create row of the type.
  - File(s): `src/BrutalUi.Markup/Template/AttributeContext.cs`
  - Estimate: 4 steps

- [x] **4.3 Write CustomTypeTests.cs with 6 tests**
  - Description: Per plan task 4.3. The `Gauge` type and the six named tests, including `RegisterChecksTheTypeInfo` and the per-loader registry.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/CustomTypeTests.cs`
  - Estimate: 10 steps

- [x] **4.4 Write AttributeHookTests.cs with 5 tests**
  - Description: Per plan task 4.4. The five named hook tests: order of add, position of a handler error, rows hide from the hook, null check.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/AttributeHookTests.cs`
  - Estimate: 8 steps

- [x] **4.5 Edit technical-design.md section 11; run plants 9 to 11; record**
  - Description: Per plan task 4.5. **Custom element types** and **The attribute hook** blocks; build and tests (expect Markup +11); plants 9, 10, 11 as named; record in `context.md`.
  - File(s): `docs/technical-design.md`, `docs/features/markup/xml-element-loader/context.md`
  - Estimate: 10 steps

**Acceptance (plan phase 4):** the 11 new tests pass; plants 9, 10, 11 fail as named; `BuiltInElements.cs` uses the public `Register`.

---

## Phase 5: Localized Text (5/5 complete)

**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*LocalizedTextTests'` and reads a total of 6 with `failed: 0`. `<ui:Label text-key="hud.score" text="Score" />` gives a label with the key `hud.score` and the default text `Score`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

Two groups. Group 1: tasks 5.1 to 5.4 (5 files). Group 2: task 5.5 (3 files).

- [x] **5.1 BuiltInElements.cs: the text-key rows and the join helper**
  - Description: Per plan task 5.1. `text-key` (Create) after `text` on `Label`, `Button`, `TextField`; the A4 helper; empty key gives `InvalidValue`; `TextField` uses `SetText(LString)`.
  - File(s): `src/BrutalUi.Markup/Template/BuiltInElements.cs`
  - Estimate: 8 steps

- [x] **5.2 TreeCompare.cs: compare LocalizedText by value**
  - Description: Per plan task 5.2. `LocalizedDiff` on `LString?`: same reference, or both set with the same id text and the same template (ordinal); the message; the remarks change.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs`
  - Estimate: 6 steps

- [x] **5.3 TreeCompareTests.cs: +1 localized test**
  - Description: Per plan task 5.3. `ALocalizedTextDifferenceGivesThePath`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/TreeCompareTests.cs`
  - Estimate: 4 steps

- [x] **5.4 Write LocalizedTextTests.cs with 6 tests**
  - Description: Per plan task 5.4. The six named tests, including the refresh test through `UiContext.RefreshLocalizedText()`.
  - File(s): `tests/BrutalUi.Markup.Tests/Template/LocalizedTextTests.cs`
  - Estimate: 10 steps

- [x] **5.5 Docs, changelog, close: plants 12 and 13, the DoD greps**
  - Description: Per plan task 5.5. Section 11 `text-key` rows and rule; the CHANGELOG line tagged `(markup/xml-element-loader)`; build and solution tests (expect Markup +6, Core +1, solution 3091 with 3 skipped from the `context.md` baseline); plants 12, 13; run the Verification Method greps; record in `context.md`.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/markup/xml-element-loader/context.md`
  - Estimate: 12 steps

**Acceptance (plan phase 5):** the 6 new Markup tests and the 1 new Core test pass; each Framework test that links `TreeCompare.cs` passes; plants 12 and 13 fail as named.

---

## Bugs & Issues

**Active Bugs:**
(none)

**Fixed Bugs:**
(none)

---

## Notes

- The plan's Verification Method (12 items) and Definition of Done run in full at the cross-phase review.
- Phase 2 plant 5 is the only change that touches `src/BrutalUi.Core/` (a scratch plant in `Image.cs`), restored right after; the no-change grep must read empty after the restore.

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done.*
