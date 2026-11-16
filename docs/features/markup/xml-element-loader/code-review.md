# markup/xml-element-loader - Code Review

**Reviewed:** 2026-11-13
**Reviewer:** Senior C# engine reviewer (cross-phase review of phases 1 to 5)
**Rules Applied:** 1. The Public API match (the 13 public types and every member against the plan's code block, names and signatures, an XML doc on each public member), 2. The Load Walk (the settings, the step 1 to 9 order, the required check before the factory, the set rows in table order after the factory, the hook after the rows in add order, the null-parent subtree rule, the no-state-between-calls rule, the thread rule), 3. The Written Table (every row, kind, leaf flag, and default against the plan's tables, the base-row order, the built-ins through the public `Register`), 4. The Value Forms (bool, whole number, number, list, color, keyword, texture exactness, the whitespace trim rule, the range checks in the factory, no exception leaves `Load` for the text), 5. The Messages and errors (kind, line, column, and source for the 8 kinds, the position rules — element name start, attribute name start, UTF-16 columns — the continue rule, the stable sort, the `ToString` form), 6. No reflection (no `System.Reflection`, `Activator`, `Enum.Parse`, `GetType()` under `Template/`), 7. The ref-struct contexts (`readonly ref struct`, no kept state, the `TryGet` rule, the `AddError` positions), 8. The `Register` checks (all-or-nothing, `ParamName`, the messages, duplicate handling, the `AddAttributeHandler` null check), 9. The 0-byte rule (`LoadedTreeAllocationTests` real, no allocation added to `Update` or `Draw` by this feature), 10. Test strength (a rule pinned by a test AND a plant where the plan says so, plants 1 to 13, public-API-only test usage), 11. Integration with the epic consumers (the hook shape for binding-attributes, the table shape for element-generator, `Load`/`GetRootOrThrow` for definition-loader, `TemplateError.ToString` for markup-sample, the `TreeCompare` change only adds equal cases), 12. Docs and hygiene (section 11 matches the code, the section 14 gate bullet, the CHANGELOG line, files under 400 lines, ASD-STE100 prose, the `context.md` deviations). The Vercel React rules do not apply to C#.
**Status:** Resolved - 6 findings, 6 resolved (2026-11-13, four fix groups: CR-1 added the `capacity` upper bound 65536 in the `TextField` factory with the constant and check in `TemplateValues`, pinned in `ACapacityIsAWholeNumberAboveZero`, plant 14; CR-2 and CR-4 stayed test-side — `CustomTypeTests` is public-API-only and gained `TryGetRejectsANameThatIsNotACreateRow` (+1 test, solution 3092); CR-3 pinned the `NotWellFormed` position (3,3) and the UTF-16 column rule in two existing tests; CR-5 and CR-6 fixed two doc lines and one code doc line. Only CR-1 changed production behavior.)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| CRITICAL | 0 | - |
| HIGH | 0 | - |
| MEDIUM | 1 | CR-1 |
| LOW-MEDIUM | 1 | CR-2 |
| LOW | 4 | CR-3, CR-4, CR-5, CR-6 |

The feature does what the plan asks. The 13 public types match the plan's code block name for name and signature for signature, and `src/Directory.Build.props` sets `GenerateDocumentationFile` with `TreatWarningsAsErrors`, so the build's `0 Warning(s)` proves every public member carries an XML doc. The Load Walk is faithful: the settings carry all seven planned values, the steps run 1 to 9 in order, the `CreateRequired` check sits in `Create` before `type.Create`, the set rows run in table order after the factory, the hook runs after the rows in the order of the add, a failed element pushes a null-parent frame, and `Load` builds a fresh `TemplateReader` per call. The written table equals the plan's two tables row for row, kind for kind, leaf flag for leaf flag, and the eight built-ins go through the public `Register` (eight `loader.Register` calls). No reflection appears under `Template/`. The gate is real: 16 warm-up steps, `AllocationGate.ReleaseAllocationContext()`, 100 measured steps, `0` bytes, and `TheMeasuredStepsDoRealWork` reads the 116 steps plus a text call, an image call with texture id 7, and a rect call. All 13 plants record a named test, and each named test asserts the exact clause its plant breaks — I read all nine test classes against the plant list. Rules 1, 3, 6, 7, 8, 9, and 11 have no finding. The six findings are one value-form hole that lets an exception leave `Load` (CR-1), three test-strength gaps (CR-2, CR-3, CR-4), and three doc lines that phase 3 and phase 5 left behind (CR-5, CR-6).

### Baseline (observed, not copied from context.md)

- `export DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` set first; every run used them.
- `dotnet build brutal-ui.sln -nodeReuse:false`: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)` (10.33s). A grep of the build output for `error` and `warning` printed nothing.
- `dotnet test --solution brutal-ui.sln`: `total: 3091`, `failed: 0`, `succeeded: 3088`, `skipped: 3` (the three GLFW display tests), duration 5.574s. No `error CS` line, no exit-134 host death; one run was enough.
- Per-assembly totals, run direct: `BrutalUi.Markup.Tests` 636 with 0 failed, `BrutalUi.Core.Tests` 1840 with 0 failed, `BrutalUi.Framework.Tests` 332 with 0 failed. Against the recorded baseline (Markup 578, Core 1839) that is Markup +58 and Core +1, exactly the plan's delta.
- Verification item 3, per class, each `failed: 0`: `*SectionElevenTemplateTests` 6, `*BuiltInTypeTests` 9, `*TemplateValueTests` 10, `*EightTypeTreeTests` 2, `*LoadedTreeAllocationTests` 2, `*TemplateErrorTests` 12, `*CustomTypeTests` 6, `*AttributeHookTests` 5, `*LocalizedTextTests` 6. Item 4: `*TreeCompareTests` 6, `failed: 0`.
- Item 5: `git diff --stat cf13f19 -- src/BrutalUi.Core src/BrutalUi.Framework src/BrutalUi.Markup/Style src/BrutalUi.Markup/BrutalUi.Markup.csproj samples Directory.Packages.props brutal-ui.sln` prints no output.
- Item 6: `grep -rnE 'System\.Reflection|Activator|Enum\.Parse|GetType\(\)' src/BrutalUi.Markup/Template` prints no output.
- Item 7: the public-type grep prints 13 lines: `TemplateLoader`, its nested `CreateInfo`, `TemplateLoadResult`, `TemplateError`, `TemplateErrorKind`, `ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, `ElementFactory`, `AttributeSetter`, `AttributeHandler`, `AttributeContext`, `ElementCreateArgs`.
- Item 8: `wc -l` over the nine `Template/` files: `TemplateError` 151, `TemplateLoadResult` 92, `TemplateReader` 177, `TemplateReader.Elements` 323, `ElementTypeInfo` 220, `AttributeContext` 296, `TemplateValues` 273, `TemplateLoader` 318, `BuiltInElements` 399. Each is under 400; `BuiltInElements.cs` has one line of room left.
- Item 9: `grep -n 'ui:Panel' docs/technical-design.md` prints nothing. `grep -n 'LoadedTreeAllocationTests' docs/technical-design.md` gives line 1710, the section 14 twenty-first gate bullet. `grep -n 'markup/xml-element-loader' CHANGELOG.md` gives one line (54).
- Item 10: each of the 8 kinds has a test file — `NotWellFormed` (SectionEleven, TemplateError), `UnknownElement` (CustomType, TemplateError), `UnknownAttribute` (CustomType, SectionEleven, TemplateError), `InvalidValue` (AttributeHook, CustomType, LocalizedText, TemplateError, TemplateValue), `MissingAttribute` (TemplateValue), `UnknownTexture` (BuiltInType, TemplateValue), `UnexpectedText` (TemplateError), `UnexpectedChild` (CustomType, TemplateError).
- Item 11: `diff` of `docs/technical-design.md` lines 1188 to 1191 against `SectionElevenTemplateTests.cs` lines 27 to 30 minus the 8-space literal indent prints no output — the two templates are identical character for character.
- Item 12: `context.md` records plants 1 to 13, each with a non-zero exit, `error CS` 0, and the named failures. The evidence is on disk: `src/BrutalUi.Markup/obj/plantscratch/` holds `p1.log` to `p13.log`, the restore logs, and 12 `.orig` copies.
- `git status --porcelain`: 22 intent-to-add (`A`) files (the four feature docs, the nine `Template/` sources, the nine test classes) and 5 modified (`M`) files (`CHANGELOG.md`, `docs/technical-design.md`, `TreeCompare.cs`, `TreeCompareTests.cs`, `BrutalUi.Markup.Tests.csproj`): 27 files, +6261/-6 against `cf13f19`. Branch `feat/markup/xml-element-loader` at `cf13f19`; the feature is uncommitted working-tree work. No commit, no push.
- Behavior probe (my own, in the git-ignored `src/BrutalUi.Markup/obj/plantscratch/review/probe/`): 15 cases through the public API only. Each case is quoted inside the findings below.

---

## Findings

### CR-1: `capacity` has no upper bound, so `Load` throws `OutOfMemoryException` for a legal template value

**Severity:** MEDIUM
**Rule:** 4 (The Value Forms, "no exception leaves `Load` for template text") and 2 (the factory range-check rule)
**File:** `src/BrutalUi.Markup/Template/BuiltInElements.cs:177-197` (`CreateTextField`), `src/BrutalUi.Markup/Template/TemplateValues.cs:78-110` (`TryParseWholeNumber`); the form is stated at `docs/technical-design.md:1331-1332`

**Problem:** The whole-number form accepts "from 1 to `int.MaxValue`", and the factory passes the value straight into the create info:

```csharp
if (!args.TryGet("capacity", out AttributeContext capacityContext)
    || !capacityContext.TryReadWholeNumber(out int capacity))
{
    return null;
}
...
TextField field = new(new TextField.CreateInfo { Capacity = capacity, Text = text });
```

`TextField` allocates the buffer from that number (`src/BrutalUi.Core/Elements/TextField.cs:320`, `_buffer = new byte[info.Capacity];`), and the CLR array limit sits below `int.MaxValue`. The probe measured it: `capacity="2147483647"` left `Load` with `OutOfMemoryException: Array dimensions exceeded supported range.` — not an error in the report. `capacity="100000000"` loaded clean and asked for 100 MB per field, and a template may hold many fields. This is exactly the case Risk 4 and the Quality Bar forbid ("`Load` never throws for the text of a template with the built-in types"), and it is the only built-in value with no bound on the memory it buys. `Slider` and `ScrollView` show the right shape one file up: each checks its range in the factory and reports `InvalidValue` before the constructor runs (`BuiltInElements.cs:146-168`, `:202-222`).

**Fix:** Give `capacity` a documented ceiling and check it in the factory, in the shape the `Slider` checks already use. `TextField` capacities in the repo run to 32 (`ScrollView`/`TextField` gates) and 81 x 16 fields, so a ceiling of 65536 bytes is far above any real use:

```csharp
private const int MaxCapacity = 65536;

if (!args.TryGet("capacity", out AttributeContext capacityContext)
    || !capacityContext.TryReadWholeNumber(out int capacity))
{
    return null;
}

if (capacity > MaxCapacity)
{
    capacityContext.AddError(
        TemplateErrorKind.InvalidValue,
        $"'{capacityContext.Value}' is above the largest capacity {MaxCapacity}. Write a smaller capacity.");
    return null;
}
```

Then one clause in The value forms of `implementation.md` (line 306) and the matching doc bullet at `technical-design.md:1331` ("from 1 to 65536"), and one entry in the `ACapacityIsAWholeNumberAboveZero` bad list (`TemplateValueTests.cs:156`, add `"2147483647"` and `"70000"`). No test count moves.

**Effort:** Low

### CR-2: `RegisterChecksTheTypeInfo` pins the internal registry count, which the class remarks call off-limits

**Severity:** LOW-MEDIUM
**Rule:** 10 (Test strength, the public-API-only clause)
**File:** `tests/BrutalUi.Markup.Tests/Template/CustomTypeTests.cs:75-78`, with the claim at `:15-16` and the helper at `:247-254`

**Problem:** The class remarks state "Each test uses the public API of `TemplateLoader` and `TreeCompare` only", and Quality Bar bullet 4 and Risk 3 give the reason: the tests must pass unchanged when element-generator replaces `BuiltInElements.cs`. Two lines read the internal registry instead, and one of them pins its size:

```csharp
Assert.False(loader.Types.ContainsKey("Probe"));

// The registry holds the eight built-in types and the one Gauge. The failed calls registered nothing.
Assert.Equal(9, loader.Types.Count);
```

`loader.Types` is `TemplateLoader.Types` (`TemplateLoader.cs:285`), internal and reachable through `InternalsVisibleTo`. The `Assert.Equal(9, ...)` value is not a rule of the loader: it is 8 (the count of the written table, the file element-generator owns) plus 1. A replaced table that registers one type more, or one less, fails a phase 4 test for a reason outside that test's rule. The all-or-nothing proof does not need the absolute count — `AssertFault` (`:249-253`) already compares the count before and after each bad call, and that comparison holds no table knowledge.

**Fix:** Keep the per-fault before/after compare, and replace the two absolute registry reads with a public proof of the same rule:

```csharp
// Register added nothing for the failed entries: Probe is still an unknown element.
TemplateLoadResult probe = loader.Load("""<ui:Probe xmlns:ui="brutal-ui" />"""u8, "gauge.xml");
Assert.Equal(TemplateErrorKind.UnknownElement, Assert.Single(probe.Errors).Kind);
```

Line 79 to 80 already prove the good `Gauge` loads, so the two asserts together cover both halves. `context.md`'s phase 4 decision then needs one line of update (it records reading `loader.Types`, which stays true for `AssertFault`). Class stays at 6 tests, count 3091 unchanged.

**Effort:** Low

### CR-3: Two position rules carry no assert: the exact `NotWellFormed` position, and the UTF-16 column unit

**Severity:** LOW
**Rule:** 5 (position rules) and 10 (test strength)
**File:** `tests/BrutalUi.Markup.Tests/Template/TemplateErrorTests.cs:160-177` and `:179-196` and `:198-209`; `tests/BrutalUi.Markup.Tests/Template/SectionElevenTemplateTests.cs:122-133`; the rules sit at `src/BrutalUi.Markup/Template/TemplateError.cs:94-99` and `docs/technical-design.md:1364-1366`

**Problem:** (a) `NotWellFormed` is the one kind whose line and column no test pins by equality. `XmlThatIsNotWellFormedStopsTheRead` asserts only the kind of the second error; the other three give `Assert.True(error.Line >= 1)` / `Assert.True(error.Column >= 1)`, and the DTD test pins the line but not the column. The plan's task text asked for that form, so the gap is not a mistake of the build — but the Quality Bar's "a test that asserts the kind, the line, and the column" is then unmet for that kind, and the `Math.Max(1, ...)` position path (`TemplateReader.cs:109-116`) has one guard only: the empty-input test fails with an exception if the clamp disappears. The probe shows the value is deterministic and cheap to pin: the wrong-end-tag case reads `probe.xml(3,3): NotWellFormed: The template is not well-formed XML. The 'ui:Element' start tag on line 1 position 2 does not match the end tag of 'ui:Wrongs'. Line 3, position 3.`
(b) The UTF-16 column unit — "a tab counts 1, a character outside the Basic Multilingual Plane counts 2", stated in `TemplateError.Column`, in the `AttributeContext.Column` doc, and in section 11 — has no test with any non-BMP character (`grep` over the nine test classes finds none). The probe confirms the loader is right today: with `text="😀"` before a bad attribute, the error reads line 1, column 54, which is the UTF-16 index 53 plus 1, and not the rune index 52 plus 1. A later refactor to `Rune`-based counting, or a change of the reader settings, moves that column with no test to say so.

**Fix:** In `XmlThatIsNotWellFormedStopsTheRead`, after the kind assert, add the two equality asserts:

```csharp
Assert.Equal(3, result.Errors[1].Line);
Assert.Equal(3, result.Errors[1].Column);
```

Then add one case to `TemplateErrorTests`, reusing the existing helpers, which pin the unit rule because `AssertPosition` counts UTF-16 units — the same unit `IXmlLineInfo` gives:

```csharp
/// <summary>The column counts UTF-16 units, so a character outside the BMP takes 2.</summary>
[Fact]
public void AColumnCountsUtf16Units()
{
    TemplateLoader loader = new();
    string template = """<ui:Label xmlns:ui="brutal-ui" text="X" nope="1" />""";
    TemplateError error = Fault(loader, template);
    AssertAttribute(error, template, "nope");
}
```

Write the `X` as the actual astral character, and put it in an attribute value that stands before the fault. Class grows from 12 to 13 tests, so the solution total becomes 3092 — record the new total in `context.md` and in the section 11 gate note, or fold the assert pair into the existing `TextBetweenElementsIsAnError` template line instead if the count must hold.

**Effort:** Low

### CR-4: The `TryGet` rule for a name that is not a create row has no test

**Severity:** LOW
**Rule:** 7 (the ref-struct contexts, the `TryGet` rule) and 10 (test strength)
**File:** `src/BrutalUi.Markup/Template/AttributeContext.cs:261-280`; the rule is stated at `docs/technical-design.md:1422-1423` and in the plan's Public API block (`implementation.md:229`, task 4.2); no test exists in `tests/BrutalUi.Markup.Tests/Template/`

**Problem:** The check is implemented and correct — the probe measured both halves: a factory that calls `args.TryGet("name", out _)` on a base **set** row throws `ArgumentException` with `ParamName` `name` and the message `'name' is not a create attribute of 'Probe2'. TryGet reads a create row of the type only.`, and an unknown name (`"ghost"`) throws the same. Nothing pins it: `grep -rn 'not a create' tests/` prints no output, and no custom factory in `CustomTypeTests` reads a set-row name. The rule is the one that protects a game author who types `args.TryGet("level")` for a set row and then reads a default value with no signal — the plan called it "a factory cannot read a set attribute". A refactor that returns `false` instead of throwing keeps all 3091 tests green, and the section 11 sentence then documents a rule the code dropped.

**Fix:** Add one test to `CustomTypeTests` that drives the throw through the public API:

```csharp
/// <summary>`TryGet` reads a create row of the type only: a set-row name throws.</summary>
[Fact]
public void TryGetRejectsANameThatIsNotACreateRow()
{
    TemplateLoader loader = new();
    loader.Register(new ElementTypeInfo
    {
        Name = "ProbeRow",
        Create = static (ElementCreateArgs args) =>
        {
            _ = args.TryGet("name", out _);
            return new Element();
        },
    });

    ArgumentException error = Assert.Throws<ArgumentException>(
        () => loader.Load("""<ui:ProbeRow xmlns:ui="brutal-ui" name="x" />"""u8, "gauge.xml"));
    Assert.Equal("name", error.ParamName);
}
```

The throw leaves `Load` by design (a factory bug, as step 6 states), so `Assert.Throws` wraps the call. Class grows 6 to 7, the solution 3091 to 3092; the same count note as CR-3 covers both.

**Effort:** Low

### CR-5: Section 11's API block still says `Errors` is in the order of the load

**Severity:** LOW
**Rule:** 12 (section 11 matches the code) and 5 (the sort)
**File:** `docs/technical-design.md:1221`

**Problem:** Phase 1 wrote the block before the sort existed; task 3.2 added `SortErrors` and closed the code doc, but the doc block line stayed:

```csharp
public IReadOnlyList<TemplateError> Errors { get; }              // in the order of the load. Empty on success
```

The code sorts by line, then column, with the index as tie-break (`TemplateReader.Elements.cs:285-322`, called from `TemplateReader.cs:123-127`), `TemplateLoadResult.Errors` documents "sorted by line, then by column" (`:32`), and the same section's own error-report bullet says the loader sorts (`:1358-1360`). The load order and the sorted order are not the same: the probe loaded `<ui:Image tint="zz" uv="0 0 0" />`, where the rows run in table order (uv first at column 42, then tint at column 32), and `Errors` came back as column 32 then 42. A reader of the block therefore copies the wrong contract, and the block is the part element-generator and definition-loader read.

**Fix:** One comment edit, to match the plan's own block wording:

```csharp
public IReadOnlyList<TemplateError> Errors { get; }              // sorted by line, then column. Empty on success
```

**Effort:** Low

### CR-6: The `UnknownAttribute` example lists the `Label` attributes without `text-key`, at two sites

**Severity:** LOW
**Rule:** 5 (The Messages) and 12 (docs and comments match the code)
**File:** `docs/technical-design.md:1384`; `src/BrutalUi.Markup/Template/TemplateError.cs:106-107`

**Problem:** Both examples were written in phase 3, before `text-key` became a row of `Label`. The message is built from `RegisteredType.AttributeNames` in table order, and phase 5 added the row, so the real text now ends with the key row. The probe: `<ui:Label ... nope="1" />` gives `probe.xml(1,54): UnknownAttribute: 'nope' is not an attribute of Label. The attributes of Label are name, class, pickable, focusable, text, text-key.` Both sites still read "…pickable, focusable, text." The plan's Messages section already carries the correct form (`implementation.md:322`), so the doc drifts from both the code and the plan. No test catches it: `AnErrorGivesItsTextAndGetRootOrThrowListsEachError` asserts `StartsWith` on the `hud.xml(2,14): UnknownAttribute: ` prefix only, which is the plan's rule (the message text may change).

**Fix:** Add `, text-key` to the attribute list in both examples — one line in `technical-design.md` (1384) and one in the `Message` remarks of `TemplateError.cs` (106-107). No code line, no test line.

**Effort:** Low

---

## What's Already Good

- The Public API is exact: the DoD grep prints the planned 13 lines and no extra; every signature in `TemplateLoader`, `TemplateLoadResult`, `TemplateError`, `TemplateErrorKind`, `ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, the three delegates, and the two context structs matches the plan's block, including the enum orders (`Set, Create, CreateRequired` and the 8 kinds in the plan's order). `src/Directory.Build.props:13` sets `GenerateDocumentationFile` and `TreatWarningsAsErrors` is on, so the build's `0 Warning(s)` is itself the proof that each public member is documented.
- The Load Walk is faithful and the plan's hard rules are visible in one read: all seven settings (`DtdProcessing.Prohibit`, `XmlResolver = null`, the three ignore flags, `ConformanceLevel.Document`, `CloseInput`, `TemplateReader.cs:67-76`), `IsEmptyElement` read before the attribute loop (`TemplateReader.Elements.cs:22`), steps 1 to 3 in order each with `PushFailedFrame`, the required check before `type.Create` (`:128-145`), the step 6 `InvalidOperationException` for a null factory result with no new error (`:155-160`), `ApplyRows` over `type.Rows` in table order with base rows first (`:168-184`), `RunHook` after it over the source-ordered list with handlers in add order and first-`true`-takes-it (`:189-219`), and a fresh `TemplateReader` per `Load` call (`TemplateLoader.cs:245`), so no state crosses calls.
- The written table equals the plan's tables cell for cell: base rows `name`, `class`, `pickable`, `focusable` in that order, leaf flags on `Label` and `Image` only, `max` and `capacity` the only `CreateRequired` rows, `min`/`step`/`scroll-speed`/`texture` optional with the documented defaults, and all eight types registered through the public `Register` — so element-generator can replace one file and keep the seam.
- The value forms are strict and measured: case-sensitive `true`/`false`; ASCII digits only with an overflow guard on a `long` accumulator; `InvariantCulture` with `AllowLeadingSign | AllowDecimalPoint`, no exponent, no group separator, plus a `float.IsFinite` reject that also kills the `NaN`/`Infinity` words; exact counts for the two and four number lists; `#` with 3, 6, or 8 hex digits in either case, mirroring `StyleValues`; the three `scale-mode` keywords through an explicit switch; an ordinal texture read of the map copied at construction. `TemplateValueTests` pins each fault path today with a bad-value list per form. My own probe added five forms that no test lists — `1.2.3`, `1 2`, `scroll-speed="5."`, `scroll-speed="+5"`, and `capacity="100000000"` — and each gave one `InvalidValue` with `Root` null, except the two accepted forms (`5.` reads 5, `+5` reads 5) and the large capacity, all with no exception.
- The trim rule is placed exactly as the plan states it: typed reads trim through `TemplateValues.Trim`, while `name`, `text`, `text-key`, and the `class` list use `context.Value` as `XmlReader` gives it — and `class="a  b&#9;c"` still gives three names because `NextName` treats tab as a separator (`SectionElevenTemplateTests.AClassListGivesEachNameInOrder`).
- The messages follow The Messages word for word where the plan fixed the words, and the free-form ones follow the same rule (quote the text, name the problem, name the fix). `ToString` gives `<source>(<line>,<column>): <kind>: <message>`, and `GetRootOrThrow` joins those lines with LF, asserted by splitting the thrown message and comparing each line to `ToString`.
- The sort is stable by construction, not by luck: `SortErrors` sorts an index array with the original index as tie-break (`TemplateReader.Elements.cs:285-322`), because `List.Sort` is not stable — and `TheLoaderRecordsEachErrorAndContinues` pins four errors in source order, one per fault.
- No reflection anywhere in the feature (`grep` clean), and the two context types are `readonly ref struct` with a reader reference and one index, so a handler cannot keep a context; each carries the doc that says why, and `default(...)` throws an `InvalidOperationException` that names the rule instead of a `NullReferenceException`.
- The gate is honest work: 16 warm-up steps, `AllocationGate.ReleaseAllocationContext()`, 100 measured steps, `Assert.Equal(0L, after - before)`, and `TheMeasuredStepsDoRealWork` reads 116 steps plus a text call, an image call with texture id 7, and a rect call — `RecordingRenderer.Begin` resets the call list per frame, so "the last frame" is literally what the asserts check. `git diff --stat` over `src/BrutalUi.Core` and `src/BrutalUi.Framework` is empty, so no update or draw path changed by this feature.
- The plant discipline holds. I read each of the 13 named tests against its plant, and each asserts the clause the plant breaks: plant 2 against the three ordered names, plant 3 against `Maximum` 10 and the eight-type tree, plant 4 against the `de-DE` read of `0.5`, plant 6 against the count 4, plant 7 against line 1 (recorded as a deviation with reason), plant 9 against `calls[2]` staying 0, plant 11 against the value the rows wrote, plant 12 against `Template` equal to `Score`, plant 13 against `AssertOneDifference`. Every position assert derives line and column from the template text that the test loads, so a moved line cannot drift.
- The epic seam is in the right shape: `AttributeHandler` carries the element, the name, and the value as arguments so binding-attributes can implement A9 against its own signature, and `AttributeContext.AddError` is public; `BuiltInElements.cs` is one file, `Register` is public, and `AttributeRowKind` is the row shape a generator writes; `Load` plus `GetRootOrThrow` is the whole definition-loader path; `TemplateError.ToString` is the whole markup-sample path. The `TreeCompare` change only adds equal cases (same reference, or the same id text and the same template), and `BrutalUi.Framework.Tests` stays 332 with 0 failed, so builder-api and the Framework suites read the new rule with no edit.
- Hygiene: nine source files, all under 400 lines; the untouched-path list holds its empty diff; the CHANGELOG carries exactly one `(markup/xml-element-loader)` line; and `context.md` records each deviation as a decision with a reason — the placeholder `TemplateReader`, the index-free `ElementCreateArgs`, the `ArgumentNullException` subtype choice, the early `XmlException` clamp, the phase 3 file split against the DoD's "8 files" text, and the honest 3032 + 59 = 3091 count note.
- Note for `/review-epic`, not a finding here: `TemplateValues.TryParseColor` copies the digit rules of `StyleValues.TryReadColor` (Decision 7, plan Notes item 6), which is recorded tech debt for a shared span reader.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-1 | Medium | Low | The only path where a template value that the docs call legal still throws out of `Load`. The factory range-check shape already exists twice in the same file, so the fix copies it. |
| 2 | CR-2 | Low-medium | Low | One assert ties the phase 4 suite to the size of the file element-generator replaces; the public proof is shorter than the registry read it replaces. |
| 3 | CR-4 | Low | Low | A documented rule of the seam has no test. One test, and the two count notes (CR-3, CR-4) land in one `context.md` line. |
| 4 | CR-3 | Low | Low | Pin the two position halves that only prose guards today: the `NotWellFormed` line and column, and the UTF-16 unit. Do it with CR-4 so the test-count change is one pass. |
| 5 | CR-5 | Low | Low | One comment word in `technical-design.md`; the block is what the three later features read. |
| 6 | CR-6 | Low | Low | Two example lines, one in the doc and one in the `Message` remarks; do it with CR-5 so the doc pass is one commit. |

---

## Progress

- [x] CR-1: `capacity` has no upper bound, so `Load` throws `OutOfMemoryException` for a legal template value (BuiltInElements.cs:177-197) — add the factory ceiling, the doc clause, and two entries in `ACapacityIsAWholeNumberAboveZero`. Fixed 2026-11-13: `TemplateValues.MaxTextCapacity` 65536 with `TemplateValues.CheckCapacity`, the `TextField` factory call, the section 11 sentence, the over-limit cases in the test, plant 14.
- [x] CR-2: `RegisterChecksTheTypeInfo` pins the internal registry count, which the class remarks call off-limits (CustomTypeTests.cs:75-78) — replace the two registry reads with the public `UnknownElement` probe, keep the per-fault before/after compare, update the remark note in `context.md`. Fixed 2026-11-13: the `<ui:Probe ... />` load gives one `UnknownElement` (the failed calls landed nothing), and the `<ui:Gauge ... />` load proves the good entry. `AssertFault` now compares the `UnknownElement` message of a probe load before and after each bad call, so the file holds no internal read at all. No test count moved.
- [x] CR-3: two position rules carry no assert: the exact `NotWellFormed` line and column, and the UTF-16 column unit (TemplateErrorTests.cs:160-209) — two asserts plus one `AColumnCountsUtf16Units` test, or fold both asserts into existing templates to hold the count. Fixed 2026-11-13: both asserts folded into existing tests; no new method, the class holds 12 and the solution 3092. `XmlThatIsNotWellFormedStopsTheRead` pins the wrong-end-tag position: line 3, column 3. `AnAttributeOfAnotherTypeIsAnUnknownAttribute` gained a second template with `text="😀"` before a bad `nope` attribute; the reader gives column 42 — the UTF-16 index plus 1, not the rune index plus 1 — derived by `AssertAttribute` and pinned by one explicit number.
- [x] CR-4: the `TryGet` rule for a name that is not a create row has no test (AttributeContext.cs:261-280) — one `TryGetRejectsANameThatIsNotACreateRow` test in `CustomTypeTests`. Fixed 2026-11-13: the test drives the `ProbeRow` type, whose factory calls `args.TryGet` with the `level` set-row name and with the `ghost` name that holds no row. Each call throws `ArgumentException` out of `Load`, with `ParamName` `name` and a message that quotes the attribute. Class 6 to 7; solution 3091 to 3092.
- [x] CR-5: section 11's API block still says `Errors` is in the order of the load (technical-design.md:1221) — one comment edit to "sorted by line, then column". Fixed 2026-11-13: the comment now reads "sorted by line, then column. Empty on success", matching `TemplateLoadResult.Errors` and `SortErrors`.
- [x] CR-6: the `UnknownAttribute` example lists the `Label` attributes without `text-key` (technical-design.md:1384, TemplateError.cs:106-107) — add `, text-key` to both. Fixed 2026-11-13: both examples end "...pickable, focusable, text, text-key." like the shipped message. Doc text only; no behavior or test change.
