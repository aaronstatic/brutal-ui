# markup/element-generator - Code Review

**Reviewed:** 2026-11-16
**Reviewer:** Senior C# engine reviewer (cross-phase review of phases 1 to 5)
**Rules Applied:** 1. The Public API match (the five new public attribute types with their `AttributeUsage`, constructor null guards, and members, `AttributeContext.TryReadInteger` and `TryReadKeyword` against the plan's code block, an XML doc on each public member under `TreatWarningsAsErrors`), 2. The incremental pipeline (the three tracking names, `ForAttributeWithMetadataName` with the `ClassDeclarationSyntax` predicate, the assembly id from `Select` on the compilation and never from a `Combine` with it), 3. The cache rule (no `ISymbol`, `SyntaxNode`, `Location`, or `Compilation` in any cached model, `EquatableArray<T>` with element-wise `Equals` and a hand-made hash, plant 10), 4. The property walk (stops before `Element`, instance non-indexer public with a public non-`init` setter of a supported type, `[TemplateIgnore]` and `[TemplateRowName]`, a property that overrides or hides a base property keeps the base place and gives one row, `[TemplateRowName]` on a property with no row gives `BRUTALUI0003`), 5. The kebab rule and the row order (both hyphen clauses, the lower-case pass, the create rows first in attribute order, the set rows from the base nearest `Element` down in declaration order, the order fixes the `UnknownAttribute` text), 6. The supported-types table (the nine matches, the metadata-name matches for `float2`/`float4`/`Rgba8`/`TextureHandle`, `float?` gives no row), 7. The generated source shape (`global::` names, `"\n"`, no collection expression, no C# 14, no reflection, `Set<n>` and `Keywords<n>` numbering, `ThrowIfNull(loader)`, `<Id>TemplateElements` and `Register<Id>Elements`), 8. The diagnostics exactness (the six ids, the category, the severity, the six message texts word for word, the planned locations, no code for a type with an error, the other types keep their code), 9. The contract that must not move (`Register` signature and checks, `AddAttributeHandler`, `AttributeHandler`, `AttributeContext.AddError`, `TemplateError`, `AttributeContext.cs` additions only), 10. The loader guarantees (each xml-element-loader test passes with no edit, the 0-byte gate stays green, the generated path uses no reflection), 11. The pack layout and the scripts (`analyzers/dotnet/cs` only, no `Generators` in the nuspec, no Generators package, the six-package list, `PackageTest` outside the solution, the `pack`/`package-restore`/`package` steps in both scripts), 12. Test strength (plants 1 to 13 recorded with a non-zero exit and the named failures, the golden test pins the full text, a pin per rule, the plan totals 6/4/6/8/8/8), 13. The project settings (`netstandard2.0`, `IsPackable` false, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, the 4.14.0 pin, the release-tracking files), 14. Docs and hygiene (sections 3, 11, and 14 match the code, the CHANGELOG line, every changed or new file under 400 lines, ASD-STE100 prose, the `context.md` deviations, the untouched directories).
**Status:** Open - 5 findings

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| CRITICAL | 0 | - |
| HIGH | 0 | - |
| MEDIUM | 1 | CR-1 |
| LOW-MEDIUM | 1 | CR-2 |
| LOW | 3 | CR-3, CR-4, CR-5 |

The feature does what the plan asks. The pipeline has the three tracked steps and no model holds a symbol, node, location, or compilation. The six messages match the plan's table word for word, sit at the planned locations, and a type with a fault keeps its siblings' code. The generated file of Markup reproduces The Written Table row for row - I read `BrutalUiMarkupTemplateElements.g.cs` line by line against section 11 - and the package path works from a real consumer: I packed, restored into the private folder, and ran the package test myself, and it printed `package-test: PASS Gauge level 0.5`. The contract with binding-attributes did not move: the `TemplateLoader.cs` diff is the constructor call swap, the `BaseRows.All` rename, and doc lines; `AttributeContext.cs` is additions only. The loader tests, `samples/`, Core, and Framework are untouched, and the 0-byte gate stays green. I found three real holes in the walk and the checks, all in the same family the diagnostics exist to close (a shape passes the checks, and the consumer sees a `CS` error inside the generated file instead of a clean `BRUTALUI` fault): a `new` hiding property keeps the base row's read kind while the generated cast binds to the derived property (CR-1, observed), a declaration class nested in a generic class escapes `BRUTALUI0006` (CR-2, observed), and an `init` accessor on a metadata property counts as settable (CR-3, the phase 1 gap that phase 4 never closed). The two low findings are one unpin (the attribute constructor null guards) and one doc clause (the kebab sentence of section 11). None of the recorded deviations needs a rollback: the plant 5 correction, the checks-file split, the `RS1032` pragma, the `Button`-first order, and the runbook edits are all sound and recorded in `context.md`.

### Baseline (observed, not copied from context.md)

- Env `DOTNET_CLI_HOME=/tmp/dui-cli-home MSBUILDDISABLENODEREUSE=1 DOTNET_NOLOGO=1`; every run used them.
- `dotnet build brutal-ui.sln -nodeReuse:false -p:NuGetAudit=false`: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)` (12.39s warm; a second run also 0/0). The first chained run printed one `Fatal error. Internal CLR error. (0x80131506)` line - the known transient of the Gotchas - and the re-run was clean. A case-insensitive `error` grep of the build output prints only the `0 Error(s)` summary line.
- `dotnet test --solution brutal-ui.sln`: `total: 3132`, `failed: 0`, `succeeded: 3129`, `skipped: 3` (the three GLFW display tests). Equals the plan's close of `context.md`.
- Per-class, direct binaries, each `failed: 0`: `*GeneratedElementTests` 6, `*TemplateReadMethodTests` 4, `*GeneratedAuthoringTests` 6, `*BuiltInRowListTests` 8, `*DiagnosticTests` 8 (both files under the filter), `*GeneratorOutputTests` 8, `*LoadedTreeAllocationTests` 2.
- `dotnet pack brutal-ui.sln --no-build --configuration Debug`: six `0.1.0-alpha` packages. `unzip -l` of the Markup nupkg lists exactly one Generators line, `analyzers/dotnet/cs/BrutalUi.Generators.dll`; the nuspec grep for Generators counts 0.
- Package path, run by me end to end: `rm -rf artifacts/package-test`, `dotnet restore --force`, `dotnet build-server shutdown`, `dotnet run --no-restore` → `package-test: PASS Gauge level 0.5`, exit 0.
- `git status --porcelain`: 14 modified files + 11 untracked paths (the four feature docs, `src/BrutalUi.Generators/`, the two attribute files, `BaseRows.cs`, `tests/BrutalUi.Generators.Tests/`, `tests/BrutalUi.Markup.Tests/Generated/`, `tests/BrutalUi.PackageTest/`); 25 lines. `git diff --stat ed767cc`: 14 files, +536/-346 (tracked files only; the new sources are untracked - no commit, as the run records).
- `git diff ed767cc -- src/BrutalUi.Core src/BrutalUi.Framework src/BrutalUi.Markup/Style tests/BrutalUi.Markup.Tests/Template tests/BrutalUi.Core.Tests samples`: no output.
- `git diff ed767cc -- src/BrutalUi.Markup/Template/AttributeContext.cs | grep -E '^-[^-]'`: no output (additions only).
- `grep -rnE 'System\.Reflection|Activator|Enum\.Parse|GetType\(\)' src/BrutalUi.Markup/Template`: no output. The same grep over both `obj/Debug/net10.0/generated` folders, plus `typeof(`: no output.
- Acceptance greps: `grep -c 'Register(' BuiltInElements.cs` 0; `grep -c 'TemplateElement('` 8; `CheckCapacity` one line (173).
- `src/BrutalUi.Generators/bin/Debug/` holds `netstandard2.0` only. `dotnet sln brutal-ui.sln list` names `BrutalUi.Generators` and `BrutalUi.Generators.Tests`, and no `BrutalUi.PackageTest`.
- `wc -l` over every new or changed feature `.cs`: the largest is 396 (`TemplateElementGenerator.Members.cs`). All under 400.
- Plants 1 to 13: recorded in `context.md` with a non-zero exit, an `error CS` count of 0, the named failures, and a `cmp`-clean restore each. The logs of the later plants sit in `artifacts/logs/`.
- My own probes (public API only, in the git-ignored `src/BrutalUi.Markup/obj/plantscratch/review/hideprobe/` and `.../genericprobe/`, built with the generator as an analyzer): the quoted errors under CR-1 and CR-2. The sibling loader review's probe under `review/probe/` was left untouched.

---

## Findings

### CR-1: A `new` hiding property keeps the base row's read kind, and the generated cast binds to the derived property (MEDIUM)

**Rule:** 4 (the walk, "A property that overrides or hides a base property keeps the base place and gives one row") and 8 (the build must show a clean fault, not a `CS` error inside generated code)
**File:** `src/BrutalUi.Generators/TemplateElementGenerator.Members.cs:238-279` (the walk), with the binding at `src/BrutalUi.Generators/ElementSourceWriter.cs:170`

**Problem:** The walk visits the base-most property first and records its name in `seen`; when the derived property with the same name comes, the row is dropped at the `seen.Add` test:

```csharp
if (seen.Add(property.Name))
{
    rows.Add(new SetRowModel(rowName, property.Name, kind!.Value, enumType, keywords));
    setSources.Add(new RowSource(rowName, property.Name, LocationInfo.FromSymbol(property)));
}
```

The kept row carries the **base** property's read kind, but the writer emits the assignment through the **target** type:

```csharp
string cast = "((" + model.TargetType + ")element)." + row.PropertyName;
```

C# member lookup then binds that expression to the derived (hiding) property. Probe, observed: `internal class Base : Element { public float Amount { get; set; } }` and `[TemplateElement("Hide")] internal sealed class Derived : Base { public new int Amount { get; set; } }` give

```text
hideprobeTemplateElements.g.cs(28,64): error CS0266: Cannot implicitly convert type 'float' to 'int'.
```

with the generated `((global::Probe.Hide.Derived)element).Amount = value;` after `context.TryReadNumber(out float value)`. Two quieter faces of the same line: (a) `[TemplateIgnore]` on the **derived** property is skipped at `Members.cs:247-250` before the `seen` test, so the base row survives and its setter writes the ignored derived property - the ignore rule is silently lost whenever both properties share a type, and the build breaks when they do not; (b) `[TemplateRowName]` on the derived hider is dropped with no `BRUTALUI0003` (the property does give a row - the base's), so the rename is ignored in silence. The rule as written is about *place*; the implementation also freezes kind and attributes at the base, which is not what the generated code executes. No test covers `new` hiding or `override` dedup at all (I grepped `Generated/` and the generator tests: no sample declares a base property or a hiding property).

**Fix:** Keep the base *place*, take the row from the *visible* property, which is what the generated cast binds to. One pass before the emit: for each type in the chain, resolve one property symbol per name to the most-derived declaration, and let the row order come from the first (base-most) place that name appears. Sketch inside the current loop shape: keep `Dictionary<string, int>` name to row index instead of `HashSet<string> seen`; on a repeat name, overwrite `rows[i]`/`setSources[i]` in place with the derived property's kind, name, rename, and location. Then add three pins in `GeneratorOutputTests` (or one new `*WalkRulesTests` class and update the plan totals): a `new` hider of a different supported type reads through the derived kind (run through the driver with the harness's `CompilationDiagnostics` assert empty), `[TemplateIgnore]` on the hider removes the row, and an `override` keeps the base place with one row.

**Effort:** Medium

### CR-2: A declaration class nested in a generic class passes the checks and emits an uncompilable factory call (LOW-MEDIUM)

**Rule:** 8 (`BRUTALUI0006` covers "a marked class that breaks a rule of The element type"; the guarantee that a type with a fault gives no code and no `CS` follow-up)
**File:** `src/BrutalUi.Generators/TemplateElementGenerator.cs:252-291` (`CheckElementType`)

**Problem:** The `IsNestedInGenericType` test runs only in the game-form branch (`:260`). The declaration-form branch checks `classSymbol.IsStatic`, the target, and the access, but never the containing types. The generated file names the factory as `owner.ToDisplayString(GlobalFormat)` (`TemplateElementGenerator.Members.cs:112-116`), and the global display of a type nested in a generic class includes the outer type parameter - text that cannot compile in the register file. Probe, observed with `internal static class Holder<T> { [TemplateElement("Gadget", Target = typeof(Widget))] internal static class Declaration { [TemplateFactory] internal static Element? Create(...) } }`:

```text
genericprobeTemplateElements.g.cs(15,114): error CS0246: The type or namespace name 'T' could not be found
    Create = ... => global::Probe.Generic.Holder<T>.Declaration.Create(args),
```

No `BRUTALUI0006` fired, and the same nesting shape without a factory walks into the same wall through the `Register` call text of an unreachable class. The rule "The marked class and each containing class have `internal` or `public` access" exists because "the generated class can reach them"; a generic outer defeats that reach in both forms.

**Fix:** Move the containment test out of the `target is null` branch in `CheckElementType`, so both forms run `if (classSymbol.IsGenericType || IsNestedInGenericType(classSymbol))` with the clause `it is a generic type`. Add one sample to `RowAndTypeDiagnosticTests.ABadElementTypeGivesBRUTALUI0006` (the declaration form of a nested-in-generic class). No count moves (the test asserts its own list).

**Effort:** Low

### CR-3: An `init` accessor of a metadata property counts as settable, and no diagnostic or pin covers it (LOW)

**Rule:** 4 (the walk: "with a `public` set accessor that is not `init`") and 12 (a rule without a pin)
**File:** `src/BrutalUi.Generators/TemplateElementGenerator.Members.cs:306-331` (`IsInit`)

**Problem:** `IsInit` reads the accessor **syntax**, and a property from metadata has no declaring syntax, so it returns `false` and the walk emits the row. Phase 1 recorded this and deferred the revisit to phase 4; phase 4 never came back. For the declaration form - the exact shape the eight built-ins use - every walked property is metadata. The day Core adds `public float Foo { get; init; }` to a built-in type, the Markup build itself fails with `CS8852` inside `BrutalUiMarkupTemplateElements.g.cs` - a `CS` error in generated code, which is the failure class the whole diagnostics feature exists to replace, and `BuiltInRowListTests` (Note 10's guard) never runs because the build stops first.

**Fix:** Decide the rule once, in the walk: an accessor the writer cannot call gives no row. Two shapes, either acceptable: (a) check the `IsExternalInit` modreq on `property.SetMethod` through the public symbol API that Roslyn 4.14 offers (phase 1 recorded that the flag they tried was not public - re-check the return-type modifiers before falling back), or (b) keep source-only detection and add a cheap metadata guard: a target property from another assembly whose declaring type is not in the compiling assembly cannot have been read for `init` - document that limit in one line of section 11 and in the `Target` doc of `TemplateElementAttribute.cs`. Either way, pin (a) or state (b) with a sample in `GeneratorOutputTests`; today nothing at all references the case.

**Effort:** Low

### CR-4: The three named attribute constructors' `ArgumentNullException` guards carry no test (LOW)

**Rule:** 1 (the Public API match includes the guard line "Each constructor with a name throws `ArgumentNullException` for `null`") and 12 (test strength)
**File:** `src/BrutalUi.Markup/Template/TemplateElementAttribute.cs:46`, `src/BrutalUi.Markup/Template/TemplateMemberAttributes.cs:78,125`

**Problem:** The guards are there and correct, but no test in `tests/BrutalUi.Markup.Tests/Generated/` or the generator tests ever calls a constructor with `null`. The sibling feature set the precedent of pinning a constructor guard after review (its CR-2 stayed test-side with `TryGetRejectsANameThatIsNotACreateRow`). A later edit that drops `ArgumentNullException.ThrowIfNull` - for example a params-style ctor refactor - would pass the whole suite.

**Fix:** One small test class (or one test inside `TemplateReadMethodTests`) with three asserts: `Assert.Throws<ArgumentNullException>(() => new TemplateElementAttribute(null!))`, the same for `TemplateCreateRowAttribute` and `TemplateRowNameAttribute`. Public API only. Records one count move (solution 3132 → 3133, Markup 661 → 662) the same way the sibling review recorded its +1.

**Effort:** Low

### CR-5: Section 11 drops one clause of the kebab rule (LOW)

**Rule:** 14 (the docs match the code) and 5 (the kebab rule)
**File:** `docs/technical-design.md:1496-1497`

**Problem:** The doc reads "A hyphen goes before an upper-case letter that follows a lower-case letter or a digit, and before an upper-case letter that precedes a lower-case letter." The plan rule - and the implementation at `NameRules.cs:30-40` - requires the second clause to hold only when the upper-case letter **follows an upper-case letter**. For `HPBar` the doc's sentence predicts a hyphen before the `P` (it precedes nothing lower - fine), but for a name like `AB` the two readings agree only by accident; the real drift is that the printed rule no longer describes the code for an upper-case letter that follows a non-letter. `TheKebabRuleSplitsWordsAndAcronyms` pins the eight plan examples, so the implementation is right and the doc line is the only wrong artifact.

**Fix:** Restore the clause: "and before an upper-case letter that follows an upper-case letter and precedes a lower-case letter". One line.

**Effort:** Trivial

---

## What's Already Good

- The cached models are exactly the plan's shape. I read every field of `Models.cs` and `Diagnostics.cs`: strings, bools, `EquatableArray<T>`, and the value records `LocationInfo`/`DiagnosticInfo`/`ElementResult`. `EquatableArray` treats a default array as empty in `Equals`, the hash, `Length`, and `GetEnumerator`, and the hash is the hand-made seed-17/factor-31 the plan requires. Plant 10 proves the pin is real.
- The pipeline has all three tracking names, the id comes from `Select` on the compilation, and `AnUnrelatedEditKeepsEachStepCached` / `AnEditOfAMarkedTypeRunsTheTransformAgain` assert the reasons from the warm driver, not from a rebuild.
- The six messages match the plan's table word for word, including the `{1}`/`{2}` clause sets I spot-checked against `Checks.cs` and `Members.cs` (`it is not static`, `a base row takes it`, ...), with the one authorized `RS1032` pragma keeping BRUTALUI0006's period.
- The generated file of Markup matches The Written Table row for row under my own read: the eight names, the two leaves, `max` and `capacity` as `CreateRequired`, create rows before set rows, `Set0` to `Set6`, `Keywords3` with the three scale-mode keywords, no `Rows` for `Element`, ordinal registration order. The golden test pins the same shape for a sample.
- The contract that must not move did not: the `TemplateLoader.cs` diff is the constructor call, the two `BaseRows.All` reads, and doc lines; `AttributeContext.cs` is additions only; `ElementTypeInfo`, `AttributeRow`, `TemplateError`, and the hook are untouched.
- The pack layout, the nuspec, the six-package feed, the package consumer, and the solution list all verified clean under my own runs, not only under the recorded ones.
- Plants 1 to 13 are each recorded with a non-zero exit, `error CS` 0, the named failures, and a `cmp`-clean restore; the two plants that fired extra tests (8 and 13) record why.
- The authorized deviations are honest and recorded: the plant 5 analysis with its corrected probe, the `Checks.cs` file split, the runbook edits, the `Button`-first order, the `DOTNET_CLI_HOME` move, and the item-12 emulation. Each one names the reason and keeps the shipped tree clean.

## Recommended Fix Order

1. CR-1 (MEDIUM) - the walk binds one member and models another; it breaks the consumer build today for a legal game shape.
2. CR-2 (LOW-MEDIUM) - the one-line check move; same failure family, rarer shape.
3. CR-3 (LOW) - close or document the metadata `init` gap before Core adds an init property.
4. CR-4 (LOW) - three asserts; records a +1 test count like the sibling's CR-2.
5. CR-5 (LOW) - one doc line.

## Progress

- [x] CR-1
- [x] CR-2
- [x] CR-3
- [x] CR-4
- [x] CR-5
