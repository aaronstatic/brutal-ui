# markup/element-generator - Context & Decisions

**Last Updated:** 2026-11-17 00:20 UTC
**Current Phase:** Complete (phases 1 to 5, the cross-phase review, and the post-review gate are done)
**Status:** ✅ Ready for Review. Section 11 names **The element table** and carries **The diagnostics**, `CHANGELOG.md` has the feature line, and the post-review gate reads: build 0 warnings 0 errors, tests 3143 / 0 failed / 3 skipped.

---

## Quick Status

**What's Done:**
- ✅ The plan (`implementation.md`) is complete: 5 phases, 46 tasks. Phases 1, 2, and 5 are ADVANCED (opus); phases 3 and 4 are STANDARD (sonnet).
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file) stand ready. Base branch `main` at `ed767cc`; the run works on `feat/markup/element-generator`.
- ✅ Phase 1 group 1 (tasks 1.1 to 1.6) is complete: baseline recorded above; `Microsoft.CodeAnalysis.CSharp` 4.14.0 pinned; `BrutalUi.Generators` builds for `netstandard2.0` with 0 warnings and 0 errors, with `Polyfills.cs`, `Models.cs`, and `NameRules.cs`. The solution still builds clean and tests the baseline 3092 with 0 failed and 3 skipped.

- ✅ Phase 1 group 2 (tasks 1.7 to 1.11) is complete: `[TemplateElement]` exists; the generator, the writer, the Markup.Tests analyzer reference, and `GeneratedElementTests` (6 tests) exist; the phase 1 demo passes; plants 1 to 3 behave as the plan expects. Solution total 3098 (3092 + 6), 0 failed, 3 skipped.
- ✅ Phase 2 group 1 (tasks 2.1 to 2.5) is complete: Markup packs the analyzer into `analyzers/dotnet/cs`; `tests/BrutalUi.PackageTest/` consumes the package and prints `package-test: PASS Gauge level 0.5`; plant 4 fires `CS1061`; plant 5 as the plan words it cannot fire (see the phase 2 decisions and tasks.md), and the corrected probe fires `NU1101`. Solution build 0/0, tests 3098 / 0 / 3; pack still six packages.

- ✅ Phase 2 group 2 (tasks 2.6 to 2.8) is complete: both test scripts now pack and run the package test (`pack`, `package-restore`, `package`, with the build-server shutdown in the `package` step); `docs/technical-design.md` sections 3 and 14 name `BrutalUi.Generators`, the package test, and the new projects; both runbooks match the new PASS lines. The script passes every step to `loader`; `Xvfb` cannot start in this sandbox, so the `smoke` step and the full Windows run wait for a display host. Scratch PASS: `test-linux: PASS build, test, pack, package, gpu, smoke (120 frames) in 49 s`.

- ✅ Phase 3 group 1 (tasks 3.1 to 3.4) is complete: `TemplateElementAttribute` gains `Target`; `TemplateMemberAttributes.cs` holds the four member attributes; `TemplateValues.TryParseInteger`, `AttributeContext.TryReadInteger`, and `AttributeContext.TryReadKeyword` exist as additions only; `TemplateReadMethodTests` (4 tests) proves both reads through two hand-registered probe types. Solution build 0 warnings 0 errors (`-p:NuGetAudit=false`); tests 3102 / 0 failed / 3 skipped.

- ✅ Phase 3 group 2 (tasks 3.5 to 3.10) is complete: the generator reads `Target`, the `[TemplateFactory]` method, and its create rows; the `Integer` and `Keyword` read kinds work end to end; `[TemplateIgnore]` and `[TemplateRowName]` apply; `GeneratedAuthoringTests` (6 tests) proves the full surface; section 11 of the design doc gains **Generated element types**. Solution build 0 warnings 0 errors; tests 3108 / 0 failed / 3 skipped. Plants 6 and 8 fired exactly their named tests (plant 8 also fired the authoring int test, see the decisions); plant 7 fired its named test. All restores `cmp` clean.

- ✅ Phase 4 group 1 (tasks 4.1 to 4.4) is complete: `Diagnostics.cs` holds the six descriptors and the `LocationInfo`, `DiagnosticInfo`, and `ElementResult` records; the release-tracking files and the phase 4 item group are in place; the transform checks The Rules and returns `ElementResult`; the output reports the faults and rejects duplicate XML names with `BRUTALUI0002`. The scratch-type check printed exactly one `BRUTALUI0001` at the class name (9,23) with 0 `error CS`, then the file was deleted. Solution build 0 warnings 0 errors; tests stay 3108 / 0 failed / 3 skipped.

- ✅ Phase 4 group 2 (tasks 4.5 to 4.10) is complete: `tests/BrutalUi.Generators.Tests/` exists with `GeneratorHarness.cs`, `DiagnosticTests.cs` + `RowAndTypeDiagnosticTests.cs` (one filter, 8 tests), and `GeneratorOutputTests.cs` (8 tests); the harness compiles samples in memory with `CSharpGeneratorDriver` and no Roslyn testing package; `brutal-ui.sln` now names `BrutalUi.Generators` and `BrutalUi.Generators.Tests`, and not `BrutalUi.PackageTest`. Plants 9 and 10 each fired exactly their named test. Solution build 0 warnings 0 errors; tests 3124 / 0 failed / 3 skipped. Phase 4 is complete; the feature moves to phase 5.

- ✅ Phase 5 group 1 (tasks 5.1 to 5.4) is complete: `BuiltInRowListTests` (8 tests) pins each built-in row list and
  leaf flag, green before and after the replacement; `BaseRows.cs` holds the four base rows; `BuiltInElements.cs` holds
  eight `[TemplateElement(..., Target = ...)]` declarations with their factories and create rows; the loader
  constructor calls the generated `RegisterBrutalUiMarkupElements`. Solution build 0 warnings 0 errors; tests 3132 /
  0 failed / 3 skipped; Markup 661. Plants 11, 12, and 13 each fired their named test.
- ✅ Phase 5 group 2 (tasks 5.5 to 5.7) is complete: section 11 of `technical-design.md` renames **The written
  table** to **The element table**, says the generator writes the rows of the built-in types and of each marked game
  type, and carries **The diagnostics** with the six-id table; `CHANGELOG.md` has one `(markup/element-generator)`
  Added line; the close ran the build (0/0), the solution tests (3132 / 0 failed / 3 skipped), `scripts/test-linux.sh`
  (green through `loader`, environmental `FAIL at smoke`), and Verification Method items 1 to 16. The Windows run waits
  in Needs Human Verification. The feature is complete: 46/46.

- ✅ Cross-phase review: 5 findings (1 MEDIUM, 1 LOW-MEDIUM, 3 LOW), all 5 resolved in three fix groups. CR-1 made the walk resolve the visible (most-derived) property per name — kind, rename, and ignore follow the hider, the base place stays — pinned by `GeneratedHidingTests` (4) and `WalkRulesTests` (6). CR-3 closed with the real mechanism (`IMethodSymbol.IsInitOnly`, probed on 4.14). CR-2 ran the nested-in-generic check in both forms of `CheckElementType` (+1 case, `*DiagnosticTests` stays 8). CR-4 added `TemplateAttributeTests` (1). CR-5 restored the full kebab sentence, and the docs now state the plan Notes item 9 `required`-member limit. Post-review gate: PASS on all 6 items; the solution reads 3143 with 0 failed and 3 skipped.

**What's Next:**
- ✅ Nothing: the feature is complete (46/46 tasks, all five phases closed, the cross-phase review resolved 5/5, the post-review gate PASS at 3143 / 0 failed / 3 skipped).
- ⏳ A person tests the five demo lines and the two host runs under "Needs Human Verification", then commits, pushes, and opens the PR for `feat/markup/element-generator`. `/autorun-feature` makes no commit and no push.
- ⏳ The next feature of the epic is `markup/binding-attributes`. It registers `command` and `bind-text` on the attribute hook; this feature did not change the hook contract (`AddAttributeHandler`, `AttributeContext.AddError`, `TemplateError`), and `AttributeContext.cs` carries additions only.

**Blockers:**
- None for the code. The `smoke` step of `scripts/test-linux.sh` needs a host where `Xvfb` can open a display (see Gotchas); the Windows script needs a Windows host.

---

## Base and Counts

- Base commit: `ed767cc` (`ed767cccbe5ec1c3501bf093a3551f822de03e1e`), branch `feat/markup/element-generator`. `git status --porcelain` shows only the four feature docs (`context.md`, `implementation.md`, `inbox.md`, `tasks.md`). Matches the plan.
- Baseline, measured 2026-11-16 at `ed767cc` with the group 1 run:
  - Solution: total 3092, failed 0, skipped 3. The three skips are in `BrutalUi.Glfw.Tests` (no display on the machine). Equals the planned 3092 with 3 skipped.
  - `BrutalUi.Markup.Tests`: total 637, failed 0, skipped 0.
  - `BrutalUi.Core.Tests`: total 1840, failed 0, skipped 0.
- Baseline pack, `dotnet pack brutal-ui.sln --no-build --configuration Debug`, exit 0. `artifacts/packages/` holds six packages, all `0.1.0-alpha`: `BrutalUi.Core`, `BrutalUi.Debugger`, `BrutalUi.Framework`, `BrutalUi.Glfw`, `BrutalUi.ImGui`, `BrutalUi.Markup`. Phase 2 expects this list unchanged.
- New build-time package (requirements line 19): `Microsoft.CodeAnalysis.CSharp` 4.14.0. Managed only. The `BrutalUi.Generators` project uses it at build time; no project at run time. No native dependency.

---

## Plan Record (task 1.1)

Condensed from `implementation.md`. The plan stays the source of truth; this section is the working record.

### The Rules (condensed)

- Element type, game form: the marked class is the element type; it derives from `BrutalUi.Core.Element`, is not static, not generic, not nested in a generic type. Declaration form (`Target` set, phase 3): the marked class is static; `Target` is `Element` or a derived type, not an open generic. The marked class and each containing class are `internal` or `public`. The XML name is a valid XML local name.
- Factory (Q3): a `[TemplateFactory]` method is static, not generic, `internal` or `public`, takes one by-value `ElementCreateArgs`, returns `Element` or a derived type. One factory at most per class. `[TemplateCreateRow]` sits only on the factory; create rows follow source attribute order. With no factory, the generator calls the target's no-parameter constructor (`public`, or `internal` in the compiling assembly); the target must not be abstract. No factory and no constructor gives `BRUTALUI0001`.
- Property walk (Q2, A4 to A6): visits the target and each base type up to, but not including, `Element`. `Name`, `IsPickable`, `IsFocusable`, `States` give no row; base rows stay written code. A property gives a row when it is an instance property, not an indexer, `public`, with a `public` non-`init` setter, of a supported type, with no `[TemplateIgnore]`. Other types give no row and no diagnostic (A5); `[TemplateRowName]` on such a property gives `BRUTALUI0003`. A property that overrides or hides a base property keeps the base place and gives one row.
- Supported types and reads: `string` → `context.Value`; `float` → `TryReadNumber`; `int` → `TryReadInteger` (new); `bool` → `TryReadBool`; enum → `TryReadKeyword` (new) plus a `switch`; `float2` / `float4` (`Brutal.Numerics`, match by metadata name) → `TryReadNumbers`; `Rgba8` → `TryReadColor`; `TextureHandle` → `TryReadTexture`. `float?` gives no row; `string?` is `string`.
- Kebab rule: a hyphen goes before an upper-case letter that follows a lower-case letter or a digit, and before an upper-case letter that follows an upper-case letter and precedes a lower-case letter; then lower-case all letters. `Level` → `level`, `ScrollOffset` → `scroll-offset`, `Uv` → `uv`, `ScaleMode` → `scale-mode`, `HPBar` → `hp-bar`, `MaxHP` → `max-hp`, `Level2` → `level2`. Enum members follow the same rule.
- Row order: create rows first, in attribute order; then set rows, base-nearest-`Element` first, down to the target, each in declaration order. The order fixes the `UnknownAttribute` message text that tests read.
- Register call (A3): the assembly id keeps ASCII letters, digits, and `_` of the assembly name, drops the rest, and prefixes `_` when it would start with a digit. Generated class: `internal static class <Id>TemplateElements` in `BrutalUi.Markup`, with `internal static void Register<Id>Elements(this TemplateLoader loader)`. The id is in the method name to avoid CS0121 ambiguity in `BrutalUi.Markup.Tests`. No module initializer.

### The Diagnostics (condensed, phase 4)

Category `BrutalUi.Markup`, severity Error. A type with an error gets no code; other types keep theirs.

- `BRUTALUI0001` no usable constructor and no factory (also an abstract target) - class name.
- `BRUTALUI0002` duplicate XML name in one compilation (ordinal) - each name argument.
- `BRUTALUI0003` `[TemplateRowName]` on a property that gives no row - the attribute.
- `BRUTALUI0004` a factory that breaks a rule, two factories, or `[TemplateCreateRow]` without `[TemplateFactory]` - method name.
- `BRUTALUI0005` a row name that is invalid, duplicated, or taken by a base row (`name`, `class`, `pickable`, `focusable`) - the property or create-row attribute.
- `BRUTALUI0006` a marked class that breaks the element-type rules, or an invalid XML name - class name.
- `{1}` / `{2}` name the broken rule in one short clause. XML name checks use `XmlConvert.IsStartNCNameChar` and `IsNCNameChar` (present in `netstandard2.0`). A clash with a built-in name stays a run-time `ArgumentException` of `Register`.

### Notes (items 1 to 14, condensed)

1. `BrutalUi.Generators` targets `netstandard2.0` (exception to the .NET 10 rule; Roslyn loads only `netstandard2.0` analyzers). All other projects stay `net10.0`.
2. The plan accepts a same-assembly `internal` no-parameter constructor, not public only.
3. Base rows move to a new `BaseRows.cs` (A6/A7 reading); declarations and factories stay in `BuiltInElements.cs`.
4. `int` needs a new `TryReadInteger` (sign, digits, `int` range); `TryReadWholeNumber` reads 1 or more only.
5. The generated `scale-mode` message changes; tests read kind and position, so they pass.
6. The walk stops before `Element`, so `States` gives no row (Decision 4).
7. Built-in name clashes stay a run-time check in `Register` (YAGNI); `BRUTALUI0002` covers one compilation only.
8. Project-referenced analyzers do not flow on; HelloPanel (markup-sample) adds its own generator `ProjectReference` and calls `loader.RegisterHelloPanelElements()`. Package consumers need no line.
9. Known limit: a `required` member with no factory fails with CS9035; `BRUTALUI0001` does not detect it. Documented, not handled.
10. Known limit: a declaration cannot skip a property of another assembly's target; a new Core settable property then fails `BuiltInRowListTests` as the intended guard.
11. The generator project joins the solution in phase 4 with the test project; phases 1 to 3 build it through project references.
12. Roslyn 4.14.0 has no `LanguageVersion.CSharp14`; the generator test harness parses samples with `LanguageVersion.CSharp13`.
13. Planned totals come from the xml-element-loader close (3092 with 3 skipped at `ed767cc`); the measured baseline above equals them.
14. The plan's Last Updated stamp is an estimate; the planner had no clock.

### Tool notes

- Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in every command; give `-nodeReuse:false` to `dotnet build` only.
- Run one test class with the per-class binary: `tests/<Project>/bin/Debug/net10.0/<Project> --filter-class '*<Class>'`.
- Run `dotnet build-server shutdown` before a build after any generator change; the compiler server keeps the old analyzer.
- Plant check: `error CS` count must be 0 before you read failed test names, so a plant that does not compile never masquerades as a failed test.
- `/tmp` is not shared between tool calls; write logs inside the workspace (`artifacts/logs/`).
- Read generated source: build a consumer with `-p:EmitCompilerGeneratedFiles=true`, then read `obj/Debug/net10.0/generated/BrutalUi.Generators/BrutalUi.Generators.TemplateElementGenerator/<Id>TemplateElements.g.cs`.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Generators/` (new) - the incremental source generator.
- `src/BrutalUi.Markup/Template/` - the marker attributes, the two new reads, and the phase 5 replacement of the written table.

### Related Files
- `docs/features/markup/element-generator/implementation.md` - the plan and the source of truth.
- `docs/features/markup/xml-element-loader/context.md` - the table shape, the baseline, and review CR-1 (the `capacity` ceiling must survive).
- `docs/technical-design.md` - sections 3, 11, and 14.

---

## Important Decisions

The plan holds thirteen decisions and the confirmed assumptions (Q1 to Q5, A1 to A10). This section records only what a phase adds or changes.

### Phase 1 group 1 (tasks 1.1 to 1.6)

- Target-framework exception recorded (Note 1): `BrutalUi.Generators` is the only `netstandard2.0` project; every other project stays `net10.0`.
- The csproj copies "The Project Files" without the phase 4 item group (`AnalyzerReleases`, `InternalsVisibleTo` wait for phase 4). The project is not in the solution yet (Note 11); phase 4 adds it with the test project.
- `EquatableArray<T>` treats a default `ImmutableArray<T>` as empty in `Length`, `Equals`, `GetHashCode`, and `GetEnumerator`, so the default struct stays safe to cache and compare. The indexer passes through to the array. The hash is hand-made (seed 17, factor 31), because `System.HashCode` does not exist in `netstandard2.0`.
- `NameRules.ToIdentifier` returns `Assembly` for a `null` name, an empty name, and a name that holds no letter, digit, or `_` (all characters dropped). The plan names the first two; the third is the same shape and keeps the id a valid identifier. A leading digit gets the `_` prefix.
- All group 1 types are `internal`. The project has no public API and no `InternalsVisibleTo` yet, so nothing escapes the assembly.
- Prose checked against the kebab examples of the plan: `Level`, `ScrollOffset`, `Uv`, `ScaleMode`, `HPBar`, `MaxHP`, `Level2`, `StretchToFill`.

### Phase 1 group 2 (tasks 1.7 to 1.11)

- Metadata names confirmed (task 1.8): the `Brutal.Numerics` 2026.10.1 assembly holds the type names `float2` and `float4` exactly (one look at the assembly metadata strings). The generator matches them by metadata name plus the namespace `Brutal.Numerics`; `Rgba8` and `TextureHandle` by name plus the namespace `BrutalUi.Core`; `string`, `float`, and `bool` by `SpecialType`.
- `init` check: Roslyn 4.14 has no public init flag on `IMethodSymbol` (checked the public API docs of `Microsoft.CodeAnalysis.dll`). The transform reads the accessor syntax from `DeclaringSyntaxReferences` for `SyntaxKind.InitAccessorDeclaration`. A property from metadata has no syntax, so it counts as settable; the generated line for a metadata `init` property then fails to compile. Phase 1 marks game source types only, so the gap carries no risk; phase 4 diagnostics can revisit it.
- Override and hide dedup: the walk keeps a `HashSet` of visited property names and keeps the base-most place (The Rules row-order line, done early because it is one line).
- Deferred to phases 3 and 4 (per the brief): the constructor rule is not checked (a marked class with no parameterless constructor gets code that fails to compile until `BRUTALUI0001` lands); a class that does not derive from `Element` gives `null` from the transform, and the output skips it; no diagnostics are reported.
- The writer: plain `StringBuilder` lines that end with `'\n'` (RS1035 bans `Environment.NewLine`); blank lines carry no indent (the plan sample shows clean blank lines); the setter number is global across the file, in output order; the models sort by XML name ordinal before writing. `Quote` escapes `"` and `\` by hand; a valid XML name holds neither, so the escape stays a guard.
- `ArgumentNullException.ThrowIfNull(loader)` in the generated source compiles in the consumer (`net10.0`): confirmed by the passing tests. The generated `Create` lambda names the unused `args` parameter; the compiler gives no warning.
- The generated file for `BrutalUi.Markup.Tests` is `BrutalUiMarkupTestsTemplateElements.g.cs` with `RegisterBrutalUiMarkupTestsElements`, as the plan sample shows; read once with `-p:EmitCompilerGeneratedFiles=true`, it matches "The Generated Source" in shape (auto-generated header, `#nullable enable`, `global::` names, `Rows` omitted for a type with no own rows).
- Test types are three top-level `internal sealed` types in `BrutalUi.Markup.Tests.Generated`; the `APropertyWithNoRowIsAnUnknownAttribute` message check pins the row order: `name, class, pickable, focusable, caption, lit, offset, rect, color, icon`.

### Phase 2 group 1 (tasks 2.1 to 2.5)

- The Markup csproj carries the two ItemGroups of The Project Files verbatim: the analyzer `ProjectReference` (`OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all"`) and the `None` pack item at `../BrutalUi.Generators/bin/$(Configuration)/netstandard2.0/BrutalUi.Generators.dll` with `PackagePath="analyzers/dotnet/cs"`.
- Pack layout measured after the edit (task 2.1): `BrutalUi.Markup.0.1.0-alpha.nupkg` holds `lib/net10.0/BrutalUi.Markup.dll`, `lib/net10.0/BrutalUi.Markup.xml`, and `analyzers/dotnet/cs/BrutalUi.Generators.dll` — no other `Generators` entry; the `.nuspec` holds no `Generators` (grep exit 1); `artifacts/packages/` holds the same six `0.1.0-alpha` packages as task 1.1. The `dotnet pack` output repeats the benign "missing a readme" note for each package; it is pre-existing and not a warning.
- PackageTest stands outside the solution: `grep -c BrutalUi.PackageTest brutal-ui.sln` gives 0. The private restore folder `artifacts/package-test/packages/` holds the extracted `brutalui.markup` with its analyzer dll; the BRUTAL `Ahwoo.*` packages came from nuget.org (Risk 11 hit: restore needs the network; the private folder is deleted and re-restored on every run, so each run reads the new package).
- The generated register name equals the plan: assembly `BrutalUi.PackageTest` gives id `BrutalUiPackageTest` and the call `loader.RegisterBrutalUiPackageTestElements()`; the PASS run compiles the call, which proves the name.
- Plant 4 (drop the `None` pack item): pack exit 0, the nupkg holds no `Generators` entry, consumer restore exit 0, `dotnet run` exit 1 with `error CS1061: 'TemplateLoader' does not contain a definition for 'RegisterBrutalUiPackageTestElements'`. Restored with `cp`, `cmp` clean, repacked, and the run printed `package-test: PASS Gauge level 0.5`.
- Plant 4 gotcha: the first run still printed PASS, because the persistent `VBCSCompiler` kept the analyzer of the previous compile and the up-to-date check skipped the compile. A package-path plant needs `dotnet build-server shutdown` after the restore (and `dotnet run` after a plant needs the forced re-compile). The planned steps run in fresh shells, where the server is not warm; the scripts get a note (group 2).
- Plant 5 (drop `PrivateAssets="all"`) as the plan writes it cannot fire: the measured dependency flow of SDK 10.0.115 needs the reference in the restore graph, and two other filters remove it first. (a) `ReferenceOutputAssembly="false"` keeps the `ProjectReference` out of the assets graph (the assets file of Markup holds no `BrutalUi.Generators` library in any variant that keeps the attribute), so `_GetProjectReferenceVersions` never sees it. (b) A reference to a project with `IsPackable=false` also never joins the nuspec (probe: `ReferenceOutputAssembly` dropped, `IsPackable=true`, still no dependency). With a plain packable reference (`<ProjectReference Include=".../BrutalUi.Generators.csproj" />`, no metadata, `IsPackable=true`), the nuspec gains `<dependency id="BrutalUi.Generators" version="0.1.0-alpha" exclude="Build,Analyzers" />`, and the consumer restore fails exactly as the plan expects: exit 1, `error NU1101: Unable to find package BrutalUi.Generators. No packages exist with this id in source(s): local. PackageSourceMapping is enabled, the following source(s) were not considered: /usr/lib/dotnet/library-packs, nuget.org.` Restored both csproj files with `cp`, `cmp` clean, repacked, re-ran: PASS, exit 0.
- Reading of the plant 5 result: `PrivateAssets="all"` is still the correct guard to keep (it is the only one that acts when the reference is in the graph, e.g. after a later edit drops `ReferenceOutputAssembly="false"`), but it is not the reason the nuspec stays clean today. The real reason is `ReferenceOutputAssembly="false"` plus the non-packable generator project. Group 2 must not rely on plant 5 as the plan words it; the durable guard is the pack-layout check of task 2.1 (nupkg list plus nuspec grep).
- Deviation from the brief: the plant 5 variant also flipped `IsPackable` on the generator csproj (the probe above); the file is restored (`cmp` clean) and the flip is not in the shipped tree.
- Measured end state: solution build 0 warnings 0 errors; solution tests 3098 / 0 failed / 3 skipped (baseline holds); pack prints six `0.1.0-alpha` packages; the package path prints `package-test: PASS Gauge level 0.5` with exit 0.
- Open questions for group 2: none blocking. The scripts may add `dotnet build-server shutdown` before the `package` step for same-machine robustness (see the plant 4 gotcha); decide in 2.6/2.7.

### Phase 2 group 2 (tasks 2.6 to 2.8)

- Linux script: the new steps sit after `run_step test` and before the GPU steps, in the order of The Script Steps: `pack`, a plain `rm -rf -- artifacts/package-test`, `package-restore`, `package`. The delete is a plain command and not a `run_step`, because it writes no log. The header now names nine steps (0 `preflight` to 8 `smoke`), names the pack and the package path in the purpose, and names the new steps in the exit-code note.
- The `package` step runs `bash -c 'dotnet build-server shutdown && dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --no-restore'`. `run_step` takes one command and writes one log, so both commands share `package.log`, and the exit code of the run is the exit code of the step. One comment names the reason for the shutdown: the compiler server can keep an earlier analyzer set and skip the re-compile (the plant 4 fault).
- Windows script: the same four steps after `Invoke-Step test`. The delete is a guarded PowerShell pair, `Test-Path` then `Remove-Item -LiteralPath artifacts\package-test -Recurse -Force`, in the style of the log-folder reset in the preflight. The `package` step passes `'(dotnet build-server shutdown && dotnet run --project tests\BrutalUi.PackageTest\BrutalUi.PackageTest.csproj --no-restore)'` to `Invoke-Step`. The parentheses matter: `Invoke-Step` puts the log redirect at the end of the command string, so an ungrouped chain would send the shutdown text to the console and break the "one line on standard output" promise of the header. The gpu and smoke steps stay unchanged and run last.
- Both scripts keep the path style of their own file: forward slashes in the bash script, backslashes in the cmd.exe strings of the PowerShell script.
- Each header now also names the new steps under exit code 1 (`the package test reported package-test: FAIL`) and in the `package` step line (`dotnet build-server shutdown, then dotnet run ...`), so the header matches the body.
- PASS lines: `test-linux: PASS build, test, pack, package, gpu, smoke (<FRAMES> frames) in <S> s` and `test-windows: PASS build, test, pack, package, smoke (<FRAMES>) in <S> s`. The `loader` step stays out of both lines, as before, because it writes no log.
- `docs/technical-design.md` section 3: the tree gains `BrutalUi.Generators/` in `src/`, with the analyzer comment, and `BrutalUi.Generators.Tests/` (phase 4 creates it) plus `BrutalUi.PackageTest/` under `tests/`. The Directory Responsibilities gain a **`src/BrutalUi.Generators/`** paragraph (`netstandard2.0`, ships in `analyzers/dotnet/cs` of the Markup package, not a package, no run-time reference) and one clause in the `tests/` paragraph that names the outside-the-solution project. Section 14 gains one **Package test** bullet before the **Sample projects** bullet.
- Deviation, with reason: this group also edited `docs/testing-linux.md` and `docs/testing-windows.md`. Those runbooks print the exact PASS line and the step list of both scripts, so the script change makes them wrong. Each one got the purpose sentence, the step list, the exit-code note, the Success row, and three new failure rows: `FAIL at pack`, `FAIL at package-restore`, and `FAIL at package`. Feature `context.md` files that quote an old PASS line are historical records; this group changed none of them.
- Blast radius: `scripts/test-linux.sh`, `scripts/test-windows.ps1`, `docs/testing-linux.md`, `docs/testing-windows.md`, and `docs/technical-design.md` sections 3 and 14. No C# file changed, so no build or test behavior changed. The steps call the same commands that task 2.5 ran by hand.
- Full script run (task 2.8): `build`, `test`, `pack`, `package-restore`, `package`, `gpu`, and `loader` all passed. The run stopped at `smoke` with `exit=1` and `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109)`. The cause is the machine, not the code: `Xvfb` aborts with a segmentation fault inside the agent sandbox, so `xvfb-run` opens no display. Earlier features recorded the same limit. Logs: `artifacts/logs/phase2-g2-real-run/`.
- PASS line, measured with the blocked step removed: a scratch copy of the shipped script, with only the `run_step smoke` block taken out, ended with `test-linux: PASS build, test, pack, package, gpu, smoke (120 frames) in 49 s` and `exit=0`. Log: `artifacts/logs/phase2-g2-no-smoke.log`. The scratch file `scripts/.test-linux-no-smoke.sh` is deleted. The real `smoke` run on a host with the X11 path goes to Needs Human Verification.
- Counts from the same runs: solution tests total 3098, failed 0, skipped 3 (the feature baseline); `package.log` ends with `package-test: PASS Gauge level 0.5`; `pack` writes the same six `0.1.0-alpha` packages; `grep -c BrutalUi.PackageTest brutal-ui.sln` gives 0.
- Build noise: the script `build` step printed 30 `NU1900` warnings and 0 `warning CS`, 0 errors. NuGet cannot refresh the vulnerability index under `~/.local/share/NuGet/http-cache`, which the sandbox mounts read-only. `TreatWarningsAsErrors` does not escalate `NU1900`, so the step still passes. Clean proof: `dotnet build brutal-ui.sln -nodeReuse:false --no-incremental -p:NuGetAudit=false` gives `0 Warning(s)` and `0 Error(s)` in 46 s. See Gotchas.

### Phase 3 group 1 (tasks 3.1 to 3.4)

- `Target` is a plain settable `Type?` property, so a game writes it as a named argument: `[TemplateElement("Dial", Target = typeof(Dial))]`. The doc names the declaration form (the marked class is `static` and holds the factory) and the access rules. Nothing at run time reads the property; `TemplateElementGenerator` takes that in task 3.6.
- `TemplateMemberAttributes.cs` copies the Public API code block exactly: `TemplateFactoryAttribute` (Method, `AllowMultiple = false`) and `TemplateIgnoreAttribute` (Property, `AllowMultiple = false`) hold no member, so the implicit parameterless constructor stands; `TemplateCreateRowAttribute` (Method, `AllowMultiple = true`) and `TemplateRowNameAttribute` (Property, `AllowMultiple = false`) take the name and throw `ArgumentNullException` with `ArgumentNullException.ThrowIfNull`. Each doc names its rule of The Rules.
- `TryParseInteger` (TemplateValues): a hand loop over the span, no `int.TryParse`, so a success allocates nothing. One `long` accumulator; a negative value takes one more unit, so `-2147483648` reads and `2147483648` fails. An empty text, a bare sign, a point, an exponent, and a letter each fail on the first non-digit. A fault leaves `value` 0; the setter pattern (write only on `true`) keeps the element untouched.
- `TryReadInteger` and `TryReadKeyword` follow the shape of the existing reads: trim with `TemplateValues.Trim`, compare, then on a fault `AddError(TemplateErrorKind.InvalidValue, ...)` at the attribute. The message quotes the raw `Value`, like the other reads; the compare uses the trimmed span. `TryReadKeyword` success allocates nothing: `ReadOnlySpan<char>.Equals(string, StringComparison.Ordinal)` per keyword, first match wins, and the fault index is -1.
- The fault message form of the keyword read is `'{Value}' is not a value of {Name}. Write 'a', 'b', or 'c'.` A private static `JoinKeywords` in `AttributeContext.cs` builds the list with a `StringBuilder`, on the fault path only. Two keywords give `'a', or 'b'`, and one gives `'a'`; the plan fixes the three-keyword form.
- `AttributeContext.cs` took additions only: one new `using System.Text;` line, the two reads after `TryReadWholeNumber`, and the private helper after them. `git diff ed767cc -- src/BrutalUi.Markup/Template/AttributeContext.cs | grep -E '^-[^-]'` gives no output (grep exit 1). Sizes after the change: `AttributeContext.cs` 374 lines, `TemplateValues.cs` 348 — both under 400, so a further read must go to a new file.
- The probe types live as private nested classes of the test class and carry no `[TemplateElement]`, so the generator writes nothing for them. `Register` takes a hand-made `ElementTypeInfo` with one `AttributeRow` set row each; the names `ProbeInt` and `ProbeKeyword` hold no marked type and no built-in name.
- `XmlReader` keeps literal spaces inside an attribute value, so `count=" 42 "` and `mode=" round-dial "` reach the read with the spaces; the trim removes them. The two cases pin that rule for both new reads.
- Test style: every template starts as a raw string literal, because the position check reads the same text. The success loads and the fault loads pass it through the `Utf8` helper (`Encoding.UTF8.GetBytes`) to the span `Load` overload, as in `GeneratedElementTests`; a literal needs no interpolation only in `AnUnknownKeywordListsTheKeywords`, which still takes the helper for the shared `Fault` path. Fault asserts check `Kind`, `Line`, `Column`, and `Contains` on the quoted text; each also holds one short clause of the message ("is not an integer", "is not a value of mode").
- Measured: solution build `-p:NuGetAudit=false` 0 warnings 0 errors; `*TemplateReadMethodTests` total 4, failed 0; per-project binaries 1840 + 332 + 243 + 40 + 647 = 3102 (Markup 647 = 643 + 4), 0 failed, 3 skipped; `dotnet test --solution brutal-ui.sln` exit 0 with total 3102, failed 0, skipped 3.
- Deviation: none from the brief. No generator file changed, so no `dotnet build-server shutdown` was needed. No git commit and no push.
- Open questions for group 2: none blocking. Notes: an enum row calls `context.TryReadKeyword(Keywords, out int index)` with the keywords in enum declaration order, and writes the member from a `switch` on the index; plant 8 ("`TryParseInteger` rejects a sign") fires `AnIntegerTakesASignAndDigits` only, because `-3` and `+3` are the two cases that need the sign.

### Phase 3 group 2 (tasks 3.5 to 3.10)

- Generator split: `TemplateElementGenerator.cs` stands at 352 lines, so the member reads went to the new partial file `TemplateElementGenerator.Members.cs` (108 lines): `ReadFactories`, `GlobalFactoryCall`, `ReadCreateRows`, `FindAttribute`, `FullName`, and `AttributeStart`. The main file keeps the pipeline, the transform, the walk, and the type map. All files under `src/BrutalUi.Generators/` and the new test file stay under 400 lines.
- Models: `ReadKind` gains `Integer` and `Keyword` at the end, so the phase 1 numbers keep their meaning. `SetRowModel` gains `string EnumType = ""` and `EquatableArray<KeywordModel> Keywords = default`, so the constructor call in the walk stays short. `ElementModel` gains `string? FactoryCall` and `EquatableArray<CreateRowModel> CreateRows` between the target name and the set rows; the transform is the only construction site.
- Factory call shape: `FactoryCall` stores the full call text `global::Ns.Meter.Create(args)`, with the lambda parameter name. The writer picks `model.FactoryCall ?? "new <TargetType>()"` for the `Create` line, so the declaration form without a factory (`Dial`) calls the no-parameter constructor of the target, as the brief decides. The factory is read from the MARKED class in both forms; `DerivesFromElement` now also accepts a target that IS `Element` (phase 5 registers the `Element` declaration with `Target = typeof(Element)`).
- Create row order: `ReadCreateRows` sorts the attributes by `ApplicationSyntaxReference.Span.Start`, so the table follows the source order with one sort. `IsRequired` comes from the named argument and picks `CreateRequired` or `Create`.
- Enum rows: `KindOf` checks `TypeKind.Enum` before the metadata-name switch (an enum has no `SpecialType`). The keywords walk the constant fields (`IFieldSymbol.IsConst`) in `GetMembers` order (declaration order), so the array is in enum order and the group 1 `TryReadKeyword` message reads it correctly. `KeywordModel.MemberName` stores the full global member name (`global::Ns.MeterStyle.RoundDial`); the writer uses it directly in the `switch` and never calls `Enum.Parse`. `EnumType` carries the global enum name for the phase 4 diagnostics.
- `netstandard2.0` limit: a list pattern on `TypedConstant` (`is [{ Value: string name }]`) needs `System.Index`, which the reference assembly lacks: CS0518/CS0656. The transform and `ReadCreateRows` use explicit `ConstructorArguments.Length` checks instead. Expect the same limit in phase 4 code.
- `[TemplateIgnore]` removes the property before the kind check; `[TemplateRowName]` replaces the kebab name after it. Both match attributes by the full metadata name through `FindAttribute` (`AttributeClass.ToDisplayString()`).
- Deviation: `Meter.Style` carries the `new` modifier. `BrutalUi.Core.Element` holds `public ref readonly ComputedStyle Style` (line 562 of `Tree/Element.cs`), so the planned `MeterStyle Style` hides it, and the build treats the CS0108 warning as an error. The hiding is intended: the walk stops before `Element`, so only the game property gives the `style` row.
- Deviation: the `Meter` templates of tests 3, 4, and 5 carry `segments="1"`. `segments` is `CreateRequired`, so every `Meter` load without it stops with `MissingAttribute` before the row under test; the one-line test shapes in the plan omitted the attribute. `AFactoryTypeLoadsWithItsCreateRows` keeps `segments="4"`, and `AMissingRequiredCreateRowIsAnError` keeps it out on purpose.
- Deviation: plant 8 fires two tests, not one. `AnIntegerTakesASignAndDigits` (the named one) and `AnIntRowTakesZeroAndANegativeNumber` (its `offset="-3"` case reads the same `TryParseInteger` through the generated row). The group 1 note that it "fires only" the probe test predates the authoring tests; the side effect is the same shape as phase 1 plant 1. Plants 6 and 7 fired exactly their named test.
- Plant results (each: full solution run to a log in `artifacts/logs/`, `error CS` count checked first, restore with `cp` and `cmp` clean):
  - Plant 6 (writer ignores `IsRequired`): exit 2, `error CS` 0, total 3108, failed 1: `AMissingRequiredCreateRowIsAnError`.
  - Plant 7 (keyword = member name lower-cased, no hyphen): exit 2, `error CS` 0, total 3108, failed 1: `AnEnumRowTakesAKebabKeyword`. The build printed one transient `Internal CLR error (0x80131506)` line with no failing project; the run rebuilt clean and the test numbers are valid (the sibling of the 134 test-host fault in the Gotchas).
  - Plant 8 (`TryParseInteger` rejects a sign): exit 2, `error CS` 0, total 3108, failed 2: `AnIntegerTakesASignAndDigits` (named) and `AnIntRowTakesZeroAndANegativeNumber` (same read, above).
- Measured end state: solution build `-p:NuGetAudit=false` 0 warnings 0 errors; `dotnet test --solution brutal-ui.sln` total 3108, failed 0, skipped 3, exit 0; `*GeneratedAuthoringTests` 6/0, `*GeneratedElementTests` 6/0, `*TemplateReadMethodTests` 4/0; `git diff ed767cc -- src/BrutalUi.Markup/Template/AttributeContext.cs | grep -E '^-[^-]'` gives no output. The generated file matches the brief shapes: create rows before set rows, `Create = static (args) => global::...Meter.Create(args)`, `Keywords3 = new string[] { "bar", "round-dial" }`, a `switch` on `keywordIndex`, the renamed `lvl` row, no `hidden` row, and `Dial` created by `new`.
- Docs: the new section 11 block (**Generated element types**, both forms, the register call, The rules, the supported types table, the kebab rule, the row order, the manual `Register` stays public) stands after **Custom element types** and before **The attribute hook**. `ste-lint.py` is not in the repository (phase 2 ran it from outside; `artifacts/ste-lint.txt` reads 1.55 per 100 words for the whole file); the block follows the sentence style of its siblings: short sentences, active voice, no contractions.
- Open questions: none blocking. Phase 4 note: a game property that hides an `Element` member (`Style`, `Name`, ...) needs the C# `new` in game source, or the consumer build stops with CS0108; a phase 4 doc line or a diagnostic would close the trap for games.

### Phase 4 group 1 (tasks 4.1 to 4.4)

- Risk 3, measured: `dotnet list src/BrutalUi.Generators package --include-transitive` shows `Microsoft.CodeAnalysis.Analyzers 3.11.0` in the transitive list. It arrives through `Microsoft.CodeAnalysis.CSharp`, so the RS rules check the descriptors; no new pin. The release-tracking files close RS2008; the writer already uses `"\n"` (RS1035).
- Deviation: RS1032 (message format) fights the plan text of BRUTALUI0006. The plan message is one sentence with a trailing period (ASD-STE100); RS1032 wants no trailing period on a one-sentence message, and `TreatWarningsAsErrors` makes it an error. The descriptor keeps the plan text under a scoped `#pragma warning disable RS1032` with a comment. The text stays word for word, because the group 2 tests assert the quoted message.
- Deviation: the phase 4 checks moved to a new fourth partial file `TemplateElementGenerator.Checks.cs` (254 lines), not into `Diagnostics.cs` (the brief's named place). With the checks, `Diagnostics.cs` would grow past 400 lines. `Checks.cs` holds `ReadNamedArguments`, `NameArgumentLocation`, `IsValidXmlName`, `CheckFactory`, `HasCreateArgsParameter`, `HasUsableConstructor`, `CheckRowNames`, `IsBaseRowName`, and the `RowSource` struct. Final line counts: main 372, Members 396, Checks 254, Diagnostics 229. Every generator file is under 400 lines.
- The cache rule holds: the transform turns every `Location` into a `LocationInfo` in place, so no model holds an `ISymbol`, `SyntaxNode`, `Location`, or `Compilation`. `ToDiagnostic()` rebuilds the `Location` in the output step. `LocationInfo.From` gives `null` for a non-source location, and `ToDiagnostic` falls back to `Location.None`.
- Message argument choices (group 2 asserts these texts): `{0}` of 0001, 0003, and 0006 and the type list of 0002 use `SymbolDisplayFormat.CSharpErrorMessageFormat` (`Ns.Gauge`), not the `global::` name. `{0}` of 0004 is the method name. 0005: `{0}` the row name, `{1}` the property name or the factory method name, `{2}` the cause clause (`it is not a valid XML local name`, `a base row takes it`, `another row takes it`). The 0004 clauses: `it is not static`, `it is a generic method`, `it is not internal or public`, `it does not have one parameter of type ElementCreateArgs`, `it does not return Element or a type derived from Element`, `the class holds more than one factory`, `it has [TemplateCreateRow] but no [TemplateFactory]`. The 0006 clauses: `the XML name is not a valid XML local name`, `it is a static class`, `it is a generic type`, `it does not derive from BrutalUi.Core.Element`, `it is not a static class`, `the Target type is an open generic type`, `the Target type does not derive from BrutalUi.Core.Element`, `it or a containing class is not internal or public`.
- Check order in the transform: the element type first (BRUTALUI0006); on a fault the type stops there, so a class that does not derive from `Element` gives 0006 alone, not a 0001 cascade. Then the create kind (0004 and 0001); on a fault the type stops before the walk. Then the walk (0003) and the row names (0005). A type with any fault keeps `Model = null`, so it gets no code; the other types keep their code.
- Count choices: two factories give one BRUTALUI0004 on each method past the first, in declaration order. Two rows with one name give BRUTALUI0005 on each row of the group. Group 2 may adjust the test counts to these shapes.
- Location choices, measured: 0001 and 0006 sit on the class name identifier token (the scratch file gave (9,23): the identifier start). 0002 sits on the first argument node of the `[TemplateElement]` application, so its span holds the `name: ` text for a named argument. 0003 and 0005 (create rows) sit on the attribute application node. 0005 (set rows) sits on the property symbol location (its identifier).
- Constructor rule (Notes item 2 option a): an `internal` parameterless constructor counts when the element type sits in the compiling assembly. The check compares the `ContainingAssembly` of the element type with the marked class: the marked class is source, so it is in the compiling assembly, and the transform needs no `Compilation` read.
- `HasReachableAccess` reads `DeclaredAccessibility` on the marked class and each containing class. `ISymbol.EffectiveAccessibility()` is not in the Roslyn 4.14 public API of the core reference (CS1061 on the first build).
- XML names use `XmlConvert.IsStartNCNameChar` and `XmlConvert.IsNCNameChar`, as the brief decides. A name with one character outside the basic multilingual plane fails, because each lone surrogate is not an NCName character. Game names stay inside the plane.
- Small deviation: a missing or non-string name argument now gives BRUTALUI0006 (`the XML name is not a valid XML local name`), where phases 1 to 3 skipped the class silently. The C# compiler cannot reach that shape (the attribute constructor takes a non-nullable `string`), but the check keeps Risk 7: the transform never throws.
- Scratch-type check (task 4.4): `tests/BrutalUi.Markup.Tests/Generated/ScratchNoCtor.cs` with `[TemplateElement("Scratch")] internal sealed class ScratchNoCtor : Element { public ScratchNoCtor(int x) { } }`. Exact error line: `ScratchNoCtor.cs(9,23): error BRUTALUI0001: 'BrutalUi.Markup.Tests.Generated.ScratchNoCtor' has no usable constructor and no factory. Add a public constructor with no parameters, or mark a static method with [TemplateFactory].` The build gave 1 `Error(s)` and `error CS` count 0. The file was deleted, then `dotnet build-server shutdown`, the solution rebuilt at 0 warnings 0 errors, and `dotnet test --solution brutal-ui.sln` stayed 3108 / 0 failed / 3 skipped.
- Measured: generator project build 0 warnings 0 errors with `EnforceExtendedAnalyzerRules` and both release files in place; solution build 0/0 with `-p:NuGetAudit=false`; solution tests 3108 / 0 / 3 with no test edited. The generator project stays outside the solution; group 2 adds it in task 4.5.
- Open questions for group 2: the fault counts for two factories and for duplicate row names, and the 0002 span of a named argument, above. Assert the ids, severities, and texts against this implementation, not only the plan prose.

### Phase 4 group 2 (tasks 4.5 to 4.10)

- Test project (4.6): `tests/BrutalUi.Generators.Tests/BrutalUi.Generators.Tests.csproj` copies the Markup.Tests xunit.v3 wiring, adds `Microsoft.CodeAnalysis.CSharp` with no version (central package management), and takes NORMAL `ProjectReference`s to `BrutalUi.Generators` (in process, with the group 1 `InternalsVisibleTo`) and `BrutalUi.Markup` (samples compile against the real Markup, Core, and Numerics assemblies). No Roslyn testing package (Decision 13).
- Roslyn 4.14 API facts, measured: `CSharpGeneratorDriver.Create` has no `IEnumerable<IIncrementalGenerator>` overload with driver options. The harness wraps the generator with the public `AsSourceGenerator()` extension and calls `Create(generators, additionalTexts, parseOptions, optionsProvider, driverOptions)` with `trackIncrementalGeneratorSteps: true`. Tracked outputs are boxed `ValueTuple<object, IncrementalStepRunReason>`; the reason is `Item2`. There is no public `IncrementalStep` type; the steps read through `GeneratorRunResult.TrackedSteps` (by tracking name) and `TrackedOutputSteps` (the source output).
- Harness (4.7): `GeneratorHarness.Run(source, assemblyName = "Sample")` builds the in-memory `CSharpCompilation` (`LanguageVersion.CSharp13`, `NullableContextOptions.Enable`, references from `TRUSTED_PLATFORM_ASSEMBLIES` plus the three project assemblies). `GeneratorRun` carries the generated texts by hint name, the generator diagnostics, the output-compilation errors and warnings, the input compilation, and the warm driver. Helpers: `Rerun`, `WithTree`, `StepReasons`, `SourceOutputReasons`, `Position` (line and column from 1), and `AssertDiagnostic` (id, severity Error, line, column, full message).
- Group 1 open questions, measured: two factories give exactly ONE `BRUTALUI0004` (on the second method); duplicate row names give one `BRUTALUI0005` per row of the group; `BRUTALUI0003` sits on the attribute NODE, which starts at the name token — the `[` belongs to the `AttributeListSyntax` — so the column is 6 for a 4-space-indented attribute, not 5. `BRUTALUI0004` sits on the method name identifier, as planned.
- Raw-string gotcha, found by the first run: a multi-line raw string ends WITHOUT a newline before its closing delimiter. The factory samples therefore join a plain-`"\n"` head and tail (`FactoryHead + member + "\n" + FactoryTail`); with raw literals there, the member line landed on line 9 and every expected line number shifted by one.
- Known trap hit in a sample: a game property named `Style` hides `BrutalUi.Core.Element.Style` (phase 3 open question). The valid sample names the enum property `Face`, so the output compilation holds 0 warnings.
- File split: `DiagnosticTests.cs` reached 492 lines, over the 400 rule. The `0005`, `0006`, and valid-sample tests moved to `RowAndTypeDiagnosticTests.cs` (class `RowAndTypeDiagnosticTests`, matched by the `*DiagnosticTests` filter, which still reads total 8). Final sizes: DiagnosticTests 294, RowAndTypeDiagnosticTests 196, GeneratorHarness 247, GeneratorOutputTests 236.
- Golden test: the pinned text equals the writer output for the Gauge sample, verified against "The Generated Source" (blank lines carry no spaces; `Rows` present; setter `Set0`). The cache tests: an added unrelated tree keeps `TemplateElementModels` and `TemplateElementSet` `Cached` or `Unchanged` and the source output `Cached`; `Level`→`Power` gives `TemplateElementModels` `Modified` and the row `power` in the text.
- Plants (each: solution build first with 0 `error CS`, full `dotnet test --solution` to `artifacts/logs/p4g2-plant<N>-test.log`, restore `cp` + `cmp` clean):
  - Plant 9 (output step skips the duplicate check; `if (end - start <= 1)` keeps only the pass-through branch): exit 2, 0 `error CS`, total 3124, failed 1: `TwoTypesWithOneXmlNameGiveBRUTALUI0002`. Restore `cmp` clean.
  - Plant 10 (`ElementModel.SetRows` is `ImmutableArray<SetRowModel>`, replaced at the model site only): exit 2, 0 `error CS`, total 3124, failed 1: `AnUnrelatedEditKeepsEachStepCached` ("TemplateElementModels: unexpected reason Modified"). Two small additions made it compile: an `Array` accessor on `EquatableArray`, and `System.Array.Empty<T>()` qualified because the new property shadows the type name. Restore `cmp` clean.
- Measured end state: `dotnet build brutal-ui.sln -nodeReuse:false -p:NuGetAudit=false` 0 warnings 0 errors; `dotnet test --solution brutal-ui.sln` exit 0, total 3124, failed 0, skipped 3; `*DiagnosticTests` 8/0, `*GeneratorOutputTests` 8/0; Markup.Tests 653/0, Core.Tests 1840/0; `dotnet sln brutal-ui.sln list` names `BrutalUi.Generators` and `BrutalUi.Generators.Tests` and no `BrutalUi.PackageTest`. No existing test changed.
- Work order: 4.6 (csproj) and the 4.7 to 4.9 files ran before 4.5 (sln add), as the brief directs. No deviation from the brief. No git commit and no push.
- Open questions for phase 5: none. The phase 3 note stands: a declaration cannot skip a property of another assembly's target, and `BuiltInRowListTests` is the guard.

### Phase 5 group 1 (tasks 5.1 to 5.4)

- 5.1 measured against the WRITTEN table, before any `src/` change: `*BuiltInRowListTests` total 8, failed 0;
  `BrutalUi.Markup.Tests` total 661 (653 + 8); solution total 3132, failed 0, skipped 3, exit 0
  (`artifacts/logs/p5g1-t51-test.log`). Build 0 warnings 0 errors (`-p:NuGetAudit=false`).
- Row lists pinned by the 8 tests, read from the `UnknownAttribute` message `The attributes of <Type> are <list>.`:
  `Element` `name, class, pickable, focusable`; `Label` and `Button` add `text, text-key`; `Toggle` adds `checked`;
  `Slider` adds `min, max, step, value`; `TextField` adds `capacity, text, text-key`; `ScrollView` adds
  `scroll-speed, scroll-offset`; `Image` adds `texture, uv, tint, scale-mode`. Each equals section 11 of
  `technical-design.md` word for word.
- Child rule in the same tests: `Label` and `Image` give `UnexpectedChild` at the `<ui:Element />` name; the other
  six load with the child attached. Each test gives its type the required create attribute (`max` for `Slider`,
  `capacity` for `TextField`), because the loader reports an unknown attribute only after the factory path runs.
- The test file reuses the position helpers of `GeneratedElementTests` (`Fault`, `Bytes`, `AssertAttribute`,
  `AssertElement`, `AssertPosition`), because the row list and the leaf flag are the only new facts.
- 5.2 measured: `BaseRows.cs` (67 lines) holds `internal static readonly AttributeRow[] All` plus `SetPickable`,
  `SetFocusable`, and `AddClasses`, moved word for word from `BuiltInElements.cs`. `TemplateLoader.cs` reads
  `BaseRows.All` in both places (the base-row name check in `Register`, and the `RegisteredType` constructor).
  Markup build 0 warnings 0 errors.
- 5.3 shape: one nested `internal static class <Type>Declaration` per type, each with
  `[TemplateElement("<Type>", Target = typeof(<Type>))]` and one `[TemplateFactory] internal static Element? Create(ElementCreateArgs args)`.
  `Label` and `Image` carry `IsLeaf = true`. The create rows sit on the factory in table order: `text`, `text-key`
  (`Label`, `Button`); `min`, `max` (`IsRequired = true`), `step` (`Slider`); `capacity` (`IsRequired = true`), `text`,
  `text-key` (`TextField`); `scroll-speed` (`ScrollView`). `Element`, `Toggle`, and `Image` have no create rows. The
  five moved factory bodies are unchanged, `CheckCapacity` included. `RegisterAll`, the seven set-row setters, and the
  base rows are gone: `BuiltInElements.cs` is 268 lines.
- The walk reproduced The Written Table with no rename map and no `MetadataToken` sort (Risk 1 did not fire). Each
  built-in type sits one level below `Element`, so the chain holds the type alone, and the metadata property order of
  `Image` (`Texture`, `Uv`, `Tint`, `ScaleMode`) equals its declaration order. The extra public properties give no row
  for the right reason: `Minimum`, `Maximum`, `Step`, `Capacity`, `ScrollSpeed`, and `Text` are get-only, and
  `ValueChanged`, `CheckedChanged`, `TextChanged`, `Submitted`, `ScrollChanged`, and `Clicked` are `Action` types.
- 5.4 generated file (`BrutalUiMarkupTemplateElements.g.cs`, 177 lines) checked against section 11, row by row: the
  eight names, `IsLeaf = true` on `Label` and `Image` only, create rows before set rows, `max` and `capacity` as
  `CreateRequired`, and the setters `Set0` to `Set6`: `texture` (TextureHandle), `uv` (float4), `tint` (Rgba8),
  `scale-mode` (`Keywords3 = "stretch-to-fill", "scale-to-fit", "scale-and-crop"` with the index switch), `value`
  (number), `checked` (bool). `Element` gets no `Rows` line. Registration order is XML name ordinal: Button, Element,
  Image, Label, ScrollView, Slider, TextField, Toggle.
- Deviation, with reason: the brief lists the ordinal order with `Button` last. A true ordinal sort puts `Button` first
  (`'B'` < `'E'`), and that is what the generator writes. No behavior and no test depends on it: `Register` builds its
  own sorted `TypeNames` text, and `BuiltInRowListTests` reads a per-type list. The plan text needs no change.
- Note 5 confirmed: the `scale-mode` fault now reads `'<x>' is not a value of scale-mode. Write 'stretch-to-fill',
  'scale-to-fit', or 'scale-and-crop'.` `AScaleModeTakesTheThreeKeywords` reads kind and position only, and passes
  unchanged.
- Measured after the replacement: solution build 0 warnings 0 errors; `dotnet test --solution brutal-ui.sln` exit 0,
  total 3132, failed 0, skipped 3 (`artifacts/logs/p5g1-t54-test.log`); `BrutalUi.Markup.Tests` 661 / 0 (653 + 8);
  `*BuiltInRowListTests` 8/0, `*GeneratedElementTests` 6/0, `*GeneratedAuthoringTests` 6/0,
  `*TemplateReadMethodTests` 4/0. Acceptance greps: `Register(` 0, `TemplateElement(` 8, `CheckCapacity` one line
  (173). `git diff --stat ed767cc -- tests/BrutalUi.Markup.Tests/Template src/BrutalUi.Core src/BrutalUi.Framework
  src/BrutalUi.Markup/Style tests/BrutalUi.Core.Tests samples` gives no output. The generated files hold no
  `System.Reflection`, `Activator`, `Enum.Parse`, `GetType()`, or `typeof(`.
- Plants, each with a full solution run to `artifacts/logs/p5g1-plant<N>-test.log`, the `error CS` count checked
  first, and a `cp` restore that `cmp` reports clean:
  - Plant 11 (`TextField` factory drops `CheckCapacity`): exit 2, `error CS` 0, total 3132, failed 1:
    `ACapacityIsAWholeNumberAboveZero`.
  - Plant 12 (`Image` declaration drops `IsLeaf = true`): exit 2, `error CS` 0, total 3132, failed 2:
    `AChildInsideALabelOrAnImageIsAnError` and `TheRowsOfImageStayTheSame`.
  - Plant 13 (the generator sorts the set rows by name in `WalkProperties`): first run aborted with
    `Internal CLR error (0x80131506)`, exit 134, with 0 `error CS` — the known one-time test-host fault; the second run
    gave exit 2, total 3132, failed 3: `TheRowsOfImageStayTheSame` (named), plus
    `APropertyWithNoRowIsAnUnknownAttribute` and `ARenamedRowAndAnIgnoredPropertyFollowTheirAttributes`, which pin the
    same row-order rule on the marked test types. Same shape as plants 1 and 8. Restore `cmp` clean, then
    `dotnet build-server shutdown`, rebuild, and the suite is green again (3132 / 0 / 3), and the regenerated Markup
    file `cmp`-equals the checked copy.
- Blast radius of the group: `BuiltInElements.cs`, `BaseRows.cs` (new), `TemplateLoader.cs`, and the new test file. No
  change to `BrutalUi.Core`, `BrutalUi.Framework`, `BrutalUi.Markup/Style`, the loader tests, `samples/`, or the
  generator (plant 13 touched it only inside its own scratch cycle). `LoadedTreeAllocationTests` stays green: the
  generated setters are static lambdas over static methods, the shape the written table used.
- Open questions for group 2: none. Section 11 of `technical-design.md` still calls the table **The written table** and
  still says the generator replaces it later; task 5.5 fixes both lines.

---

### Phase 5 group 2 (tasks 5.5 to 5.7)

- 5.5, `docs/technical-design.md` section 11: the loader paragraph now reads "The generator writes the rows of the
  built-in types and of each marked game type, with no reflection." and names the row source **the element table**.
  **The written table** is **The element table**, and the text states that `BuiltInElements` declares each built-in
  type with its factory and its create rows, and that the generator writes the set rows from the properties. The
  **Custom element types** paragraph names the element table. **The diagnostics** block sits after **The rules** and
  before **The attribute hook**: the category, the severity, the six-id table (id, cause, location, message) word for
  word from the plan, and the `{1}`/`{2}` clause note with the `XmlConvert` check. The row tables stay, because the
  rows do not change. `grep -n 'written table' docs/technical-design.md` gives no output. The planning-time sentence
  was found by its text at line 1199, not by the planned line 1194 (brief decision).
- 5.6, `CHANGELOG.md`: exactly one `(markup/element-generator)` line under `## [Unreleased]` `### Added` (line 55). It
  names the generator inside the Markup package, `[TemplateElement]` and the four member attributes, the generated
  register call, `TryReadInteger` and `TryReadKeyword`, the six diagnostics, the generated rows of the eight built-in
  types, and the package test in both scripts. The xml-element-loader line keeps its historical wording; a changelog
  record does not change after the fact.
- 5.7, Verification Method, measured:
  - Item 1: `dotnet build-server shutdown`, then `dotnet build brutal-ui.sln -nodeReuse:false -p:NuGetAudit=false` →
    `Build succeeded. 0 Warning(s) 0 Error(s)` in 40 s.
  - Item 2: `dotnet test --solution brutal-ui.sln` → total 3132, failed 0, succeeded 3129, skipped 3, exit 0
    (`artifacts/logs/p5g2-t57-test.log`). The expected 3132 with 3 skipped.
  - Item 3 (per-class binaries): `*GeneratedElementTests` 6, `*TemplateReadMethodTests` 4, `*GeneratedAuthoringTests`
    6, `*BuiltInRowListTests` 8; each failed 0.
  - Item 4: `*DiagnosticTests` 8, `*GeneratorOutputTests` 8; each failed 0.
  - Item 5: `git diff ed767cc -- src/BrutalUi.Core src/BrutalUi.Framework src/BrutalUi.Markup/Style
    tests/BrutalUi.Markup.Tests/Template tests/BrutalUi.Core.Tests samples` gives no output.
  - Item 6: pack exit 0; `unzip -l` names exactly one Generators line, `analyzers/dotnet/cs/BrutalUi.Generators.dll`;
    the nuspec grep for `BrutalUi.Generators` counts 0; `artifacts/packages/` holds the same six `0.1.0-alpha` packages.
  - Item 7: delete `artifacts/package-test`, `dotnet restore --force`, `dotnet build-server shutdown`, `dotnet run
    --no-restore` → `package-test: PASS Gauge level 0.5`, exit 0.
  - Item 8: `scripts/test-linux.sh` → exit 1, `test-linux: FAIL at smoke (exit 1)`, `HelloPanel: GLFW init failed:
    PlatformUnavailable (X11: Failed to open display :109)` (`artifacts/logs/p5g2-t57-linuxsh.log`). Steps `build`,
    `test` (3129 succeeded, 3 skipped), `pack`, `package-restore`, `package` (PASS line), `gpu` (`check-gpu: OK`), and
    `loader` all passed first. Environmental; the script stays as shipped. The full PASS line waits in Needs Human
    Verification.
  - Item 9: the two `EmitCompilerGeneratedFiles` builds pass; the reflection grep over both
    `obj/Debug/net10.0/generated` folders gives no output (grep exit 1). `BrutalUiMarkupTemplateElements.g.cs`
    (177 lines) spot-check: the section 11 row names all appear, `IsLeaf = true` exactly 2 (`Label`, `Image`), and 8
    `Register` calls. Group 1 task 5.4 checked the file row by row.
  - Item 10: `Register(` 0; `TemplateElement(` 8; `CheckCapacity` line 173 in `BuiltInElements.cs`.
  - Item 11: `Directory.Packages.props` line 15 carries `Version="4.14.0"`; the Generators csproj carries the four
    flag lines (`netstandard2.0`, `IsPackable`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`; a comment repeats
    `netstandard2.0`); `Microsoft.CodeAnalysis.CSharp` appears in this file.
  - Item 12: the corrected diff grep gives exactly the five planned public attribute lines (TemplateElementAttribute,
    TemplateFactoryAttribute, TemplateCreateRowAttribute, TemplateRowNameAttribute, TemplateIgnoreAttribute) — see the
    deviation. The `AttributeContext.cs` diff gives no `^-` line (additions only, grep exit 1).
  - Item 13: `wc -l` over every new or changed `.cs` under `src/` and `tests/` (explicit list: `git diff
    --name-only` plus `git ls-files --others`): the largest is 396 lines
    (`TemplateElementGenerator.Members.cs`). Each file stays under 400.
  - Item 14: `dotnet sln brutal-ui.sln list` names `BrutalUi.Generators` and `BrutalUi.Generators.Tests`; no
    `BrutalUi.PackageTest`.
  - Item 15: `grep -n 'markup/element-generator' CHANGELOG.md` gives one line; `grep -n 'BrutalUi.Generators'
    docs/technical-design.md` gives lines in section 3 (96, 107, 129) and section 11 (1437).
  - Item 16: this file records every plant; group 1 kept all three fired.
  - Item 17 (Windows): no Windows host here. Added to Needs Human Verification.
- Deviations, with reasons:
  - The brief names `DOTNET_CLI_HOME=/home/aaronstatic/dui`; the sandbox holds the home file system read-only and the
    directory does not exist. The run used `/home/aaronstatic/dev/brutal-ui/artifacts/dotnet-cli-home`, with the same
    `DOTNET_NOLOGO=1` and `MSBUILDDISABLENODEREUSE=1`. No behavior change.
  - Item 12: `git diff ed767cc -- <path>` cannot list the feature source files, because they are untracked — the run
    makes no git commit and no push. The plan regex also lacks the `sealed ` alternative, and every template attribute
    class is `public sealed class`. The group emulated the diff with `git diff --no-index /dev/null <file>` on the two
    new attribute files and a `sealed ` alternative, and got the planned five lines. The plan text stays unchanged.
  - The script `pack.log` is 0 bytes: `dotnet pack --no-build` found the six nupkg up to date (item 6 packed them
    minutes earlier from the same tree), so it printed nothing and still exit 0. The fresh pack output stands in item 6.
- Blast radius: `docs/technical-design.md` (section 11), `CHANGELOG.md` (one Added line), the two feature docs, and the
  run logs. No `src/` or `tests/` code changed in group 2, so the build and test behavior stays the group 1 end state.
- Open questions: none. The feature is complete: 46/46 tasks. Follow-up work sits in Needs Human Verification (the full
  Linux `smoke` run and the Windows script run).

---

## Gotchas & Learnings

- Note 1 of the plan: `BrutalUi.Generators` targets `netstandard2.0`, against the general "Target .NET 10" rule of `AGENTS.md`. The Roslyn host loads only `netstandard2.0` analyzers (epic A2). Task 1.1 records this exception.
- The `netstandard2.0` reference assembly holds no nullable annotations. `string.IsNullOrEmpty` gives no null test, so the compiler keeps the maybe-null flow and reports CS8602. The code stores the checked value in a local with `!` (see `NameRules.ToIdentifier`). Expect the same pattern in the phase 1 generator transform.
- The first generator build prints the .NET first-run banner. It is noise; the warning and error counts stay the check.
- `VBCSCompiler` survives `dotnet build` calls in a machine session (`MSBUILDDISABLENODEREUSE` covers MSBuild nodes only). A plant that changes the analyzer set through a package needs `dotnet build-server shutdown` and a forced re-compile, or the old analyzer keeps the test green. Measured in plant 4.
- `dotnet pack --no-build` implies `--no-restore`. A csproj edit to a `ProjectReference` line changes the nuspec only after an explicit `dotnet restore` of that project. Measured in plant 5.
- Pack turns a `ProjectReference` into a nuspec dependency only when the reference enters the restore graph: `ReferenceOutputAssembly="false"` or a non-packable target keeps it out. `PrivateAssets="all"` acts inside the graph, not as the first filter. Measured on SDK 10.0.115 in the plant 5 probes.
- `Xvfb` aborts with a segmentation fault inside the agent sandbox: it prints `Caught signal 11 (Segmentation fault). Server aborting` and creates no socket in `/tmp/.X11-unix`. `xvfb-run` still exits 0, so the `smoke` step of `scripts/test-linux.sh` fails later with `GLFW init failed: PlatformUnavailable (X11: Failed to open display <N>)`. The failure is the machine, not the code. Measure each script step up to `loader` here, and take the full run to a host with a display.
- `Invoke-Step` (Windows) puts `> <log> 2>&1` at the end of the command string. In one cmd.exe chain `A && B > log`, cmd binds the redirect to `B` only, so `A` writes to the console. Put the chain in parentheses, `(A && B)`, so the redirect covers both.
- `run_step` (Linux) takes one command and its arguments. A two-command step needs a sub-shell: `run_step package bash -c 'A && B'`. The step keeps one log, and the exit code of the step is the exit code of the last command.
- The `build` step prints 30 `NU1900` warnings on this machine: NuGet cannot write the vulnerability index under `~/.local/share/NuGet/http-cache`, because the sandbox mounts that tree read-only. `TreatWarningsAsErrors` does not escalate `NU1900`, and `warning CS` stays at 0. Judge the build by the `warning CS` count here, or add `-p:NuGetAudit=false` to a `dotnet build` call to drop the audit and read `0 Warning(s)`.
- `dotnet test` takes `--solution`, not a positional solution path: `dotnet test brutal-ui.sln` ends with "Specifying a solution for 'dotnet test' should be via '--solution'". Pass `-p:NuGetAudit=false` to `dotnet build` only. The same flags on `dotnet test --solution brutal-ui.sln -nodeReuse:false -p:NuGetAudit=false` break the Microsoft Testing Platform handshake: each assembly reports `Zero tests ran`, `Exit code: 5`, and the run reports `error: 5` with `total: 0`. Measured in phase 3 group 1. The plain `DOTNET_NOLOGO=1 dotnet test --solution brutal-ui.sln` runs the whole suite (3102 / 0 failed / 3 skipped, exit 0), and the per-class binaries give the same numbers. So: audit off for the build, plain flags for the test, and expect `NU1900` in the test output as machine noise.
- A fresh `DOTNET_CLI_HOME` under `/tmp` prints the .NET first-run banner on the first call, because `/tmp` is not shared between tool calls. Export `DOTNET_NOLOGO=1` next to `DOTNET_CLI_HOME` to keep the log clean.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `BrutalUi.Markup.Tests --filter-class '*GeneratedElementTests'` and reads a total of 6 with `failed: 0`. In `AMarkedGaugeLoadsWithItsLevel`, the test marks a `Gauge` type, calls `loader.RegisterBrutalUiMarkupTestsElements()`, loads `<ui:Gauge level="0.5"/>`, and reads `Level` 0.5.
- [ ] Phase 2 demo: the developer runs `dotnet pack brutal-ui.sln --no-build --configuration Debug`, then `dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj`, and reads `package-test: PASS Gauge level 0.5`.
- [ ] Phase 3 demo: the developer runs `BrutalUi.Markup.Tests --filter-class '*GeneratedAuthoringTests'` and reads a total of 6 with `failed: 0`. A `Meter` with a factory, a required `segments` create row, an `int`, an enum keyword, a renamed row, and an ignored property loads from one template.
- [ ] Phase 4 demo: the developer runs `BrutalUi.Generators.Tests --filter-class '*DiagnosticTests'` and reads a total of 8 with `failed: 0`. Each of the six ids fires on its sample at the expected line and column.
- [ ] Phase 5 demo: the developer runs the solution tests (total 3132, 0 failed, 3 skipped), and each xml-element-loader test passes unchanged while `BuiltInElements.cs` holds no `Register` call. `BrutalUi.Markup.Tests --filter-class '*BuiltInRowListTests'` reads a total of 8 with `failed: 0`.
- [ ] Phase 2 full Linux run on a host where `Xvfb` opens a display: run `scripts/test-linux.sh; echo "exit=$?"` and read `test-linux: PASS build, test, pack, package, gpu, smoke (120 frames) in <S> s` with `exit=0`. The group 2 run reached `FAIL at smoke (exit 1)` with `GLFW init failed: PlatformUnavailable (X11: Failed to open display :109)`, because `Xvfb` segfaults in the agent sandbox; every earlier step passed.
- [ ] Phase 2 Windows run: on Windows, run `.\scripts\test-windows.ps1` and read `test-windows: PASS build, test, pack, package, smoke (<FRAMES>) in <S> s` with `$LASTEXITCODE` 0. The new `pack`, `package-restore`, and `package` steps need the same local-feed behavior; `pwsh` does not exist on the author's Linux machine, so no agent checked the file.
- [ ] Phase 5 close, item 17 of the Verification Method: on Windows, run `scripts/test-windows.ps1` against the final
  tree. Expect exit code 0 and the PASS line `test-windows: PASS build, test, pack, package, smoke (<FRAMES>) in <S> s`.
  The `pack`, `package-restore`, and `package` steps must name the local feed. The run also proves the phase 5 tree on
  Windows, where `pwsh` exists.

---

## Session Notes

### 2026-11-16 18:52
- `/autorun-feature markup/element-generator` started. Branch `feat/markup/element-generator` created from `main` at `ed767cc`. Dev docs scaffolded by `/start-feature`. Phase 1 build begins.

### 2026-11-16 19:32
- Phase 1 group 1 (ADVANCED brief `phase-1-g1.md`) complete. Tasks 1.1 to 1.6 marked `[x]`.
- Baseline measured: solution 3092 total, 0 failed, 3 skipped (Glfw, no display); Markup.Tests 637; Core.Tests 1840; pack list six `0.1.0-alpha` packages. All equal the planned values.
- Added: `Directory.Packages.props` pin; `src/BrutalUi.Generators/` with `BrutalUi.Generators.csproj` (netstandard2.0, no phase 4 item group), `Polyfills.cs`, `Models.cs`, `NameRules.cs`. The project is outside the solution by design (Note 11).
- Verified: generator build 0 warnings, 0 errors; `bin/Debug/netstandard2.0/BrutalUi.Generators.dll` exists; no `net10.0` folder in the generator bin; solution build 0 warnings, 0 errors; solution tests 3092 / 0 failed / 3 skipped.
- One fix during the run: CS8602 in `NameRules.ToIdentifier` (see Gotchas). Group 2 inherits the same pattern.
- Open questions for group 2: none. The `float2` / `float4` metadata names still need the one confirmation against `Brutal.Numerics` (task 1.8).

### 2026-11-16 19:48
- Phase 1 group 2 (ADVANCED brief `phase-1-g2.md`) complete. Tasks 1.7 to 1.11 marked `[x]`. Phase 1 is complete; the feature moves to phase 2.- Added: `src/BrutalUi.Markup/Template/TemplateElementAttribute.cs`, `src/BrutalUi.Generators/TemplateElementGenerator.cs`, `src/BrutalUi.Generators/ElementSourceWriter.cs`, the analyzer `ProjectReference` in `BrutalUi.Markup.Tests.csproj`, and `tests/BrutalUi.Markup.Tests/Generated/GeneratedElementTests.cs` (three marked types, six tests).
- Measured counts: solution build 0 warnings, 0 errors. Solution tests: total 3098, failed 0, skipped 3 (= baseline 3092 + 6). `BrutalUi.Markup.Tests`: total 643 (= 637 + 6), 0 failed. `*GeneratedElementTests`: total 6, failed 0. The phase 1 demo `AMarkedGaugeLoadsWithItsLevel` passes (Q5 state: green). `src/BrutalUi.Generators/bin/Debug/` holds `netstandard2.0` only; no `net10.0` folder exists. The generated source matches The Generated Source in shape.
- Plant 1 (writer omits the assignment line of a guarded setter): exit 2, 0 `error CS`, failed 2: `AMarkedGaugeLoadsWithItsLevel` (the expected fail) and `EachReadTypeGivesARow` (the same writer path breaks every guarded read; expected side effect). Restored, `cmp` clean.
- Plant 2 (walk does not stop before `Element`): exit 2, 0 `error CS`, failed 6: every `GeneratedElementTests` test (the register call throws `ArgumentException` for the `name` row, as the plan expects). Restored, `cmp` clean.
- Plant 3 (writer always writes `IsLeaf = false`): exit 2, 0 `error CS`, failed 1: `ALeafTypeRejectsAChild` (the expected fail). Restored, `cmp` clean; the restored run is green (3098 / 0 / 3).
- Deviations: none from the brief. One fix during the run: `IMethodSymbol` has no public `IsInitAccessor` in Roslyn 4.14, so `init` detection reads the accessor syntax (see the group 2 decisions).
- Open questions: none. Follow-up for phase 3: `TemplateMemberAttributes.cs` (`Target`, factory, create rows, `int`, enum) extends the same model shape; `ElementModel` gains the create kind and create rows then.

### 2026-11-16 20:10
- Phase 2 group 1 (ADVANCED brief `phase-2-g1.md`) complete. Tasks 2.1 to 2.5 marked `[x]`.
- Added: the analyzer `ProjectReference` and the `None` pack item in `BrutalUi.Markup.csproj`; `tests/BrutalUi.PackageTest/` with `BrutalUi.PackageTest.csproj`, `nuget.config`, and `Program.cs` (outside the solution, `grep -c` 0).
- Measured: the nupkg holds `analyzers/dotnet/cs/BrutalUi.Generators.dll` and `lib/net10.0/BrutalUi.Markup.dll`, with no other `Generators` entry; the nuspec is clean; the feed holds the same six `0.1.0-alpha` packages; the three script steps print `package-test: PASS Gauge level 0.5` with exit 0; solution build 0/0; solution tests 3098 / 0 failed / 3 skipped.
- Plants: plant 4 fired (`run` exit 1, `error CS1061` on `RegisterBrutalUiPackageTestElements`), after a first false PASS from the warm `VBCSCompiler` (Gotchas). Plant 5 cannot fire as the plan words it: `ReferenceOutputAssembly="false"` (and the non-packable target) keep the reference out of the restore graph before `PrivateAssets` acts. With a plain packable reference the nuspec gains the `BrutalUi.Generators` dependency, and the consumer restore fires `error NU1101` with exit 1, so the planned symptom exists — behind two more flips. All planted files are restored, `cmp` clean; the final re-run prints PASS with exit 0.
- Deviations: the plant 5 probe (recorded above and in `tasks.md`); no other deviation. No git commit, no push.

### 2026-11-16 20:05
- Phase 2 group 2 (ADVANCED brief `phase-2-g2.md`) complete. Tasks 2.6 to 2.8 marked `[x]`. Phase 2 is complete; the feature moves to phase 3.
- Edited: `scripts/test-linux.sh` (three new steps, the header, the PASS line), `scripts/test-windows.ps1` (the same steps, the header, the PASS line), `docs/technical-design.md` (section 3 tree, the `src/BrutalUi.Generators/` paragraph, the `tests/` clause, one section 14 bullet), `docs/testing-linux.md`, and `docs/testing-windows.md` (deviation, with the reason in the phase 2 group 2 decisions). No `src/` or `tests/` C# file changed. No git commit, no push.
- Verified: `bash -n` on the Linux script. Solution build 0 `warning CS`, 0 errors, 30 machine-only `NU1900` (Gotchas). The full script passed `build`, `test` (3098 total, 0 failed, 3 skipped), `pack` (six `0.1.0-alpha` packages), `package-restore`, `package` (`package-test: PASS Gauge level 0.5`), `gpu` (`check-gpu: OK`), and `loader`; it stopped at `smoke` on the sandbox `Xvfb` fault. Scratch run with only the `smoke` step removed: `test-linux: PASS build, test, pack, package, gpu, smoke (120 frames) in 49 s`, `exit=0`; the scratch file is deleted. `grep -c BrutalUi.PackageTest brutal-ui.sln` gives 0.
- Docs lint: `ste-lint.py --fail-over 2.5` on `docs/testing-linux.md` (0.68 per 100 words) and `docs/testing-windows.md` (0.68).
- Open questions for phase 3: none. Follow-up work: the real `smoke` PASS line and the Windows script run sit in Needs Human Verification above.

### 2026-11-16 20:22
- Phase 3 group 1 (STANDARD brief `phase-3-g1.md`) complete. Tasks 3.1 to 3.4 marked `[x]`.
- Added: `src/BrutalUi.Markup/Template/TemplateMemberAttributes.cs` (four public attributes) and `tests/BrutalUi.Markup.Tests/Generated/TemplateReadMethodTests.cs` (4 tests, two hand-registered probe types). Edited: `TemplateElementAttribute.cs` (`Target`), `TemplateValues.cs` (`TryParseInteger`), `AttributeContext.cs` (`TryReadInteger`, `TryReadKeyword`, `JoinKeywords`, additions only).
- Verified: build 0 warnings 0 errors (`-p:NuGetAudit=false`); `*TemplateReadMethodTests` total 4, failed 0; `dotnet test --solution brutal-ui.sln` total 3102, failed 0, skipped 3, exit 0; per-project totals 1840 / 332 / 243 / 40 / 647; `git diff ed767cc -- src/BrutalUi.Markup/Template/AttributeContext.cs | grep -E '^-[^-]'` gives no output. No existing test needed an edit. No generator file changed; no commit and no push.
- Deviations: none from the brief. See the phase 3 group 1 decisions for the two message forms, the integer range rule, and the keyword join.
- Open questions for group 2: none. Group 2 must keep the keyword array in enum declaration order and call `TryReadKeyword`, so the messages of `AnEnumRowTakesAKebabKeyword` come from this group's implementation.

### 2026-11-16 20:52
- Phase 3 group 2 (STANDARD brief `phase-3-g2.md`) complete. Tasks 3.5 to 3.10 marked `[x]`. Phase 3 is complete; the feature moves to phase 4.
- Added: `src/BrutalUi.Generators/TemplateElementGenerator.Members.cs` (108 lines, the member reads) and `tests/BrutalUi.Markup.Tests/Generated/GeneratedAuthoringTests.cs` (6 tests, types `MeterStyle`, `Meter`, `Dial`, `DialDeclaration`). Edited: `Models.cs` (`CreateRowModel`, `KeywordModel`, `Integer`, `Keyword`, the two added `SetRowModel` fields, the two added `ElementModel` fields), `TemplateElementGenerator.cs` (352 lines: `Target`, the factory, the walk with `int`, enum, ignore, rename), `ElementSourceWriter.cs` (235 lines: factory `Create`, create rows, `TryReadInteger`, keyword array and `switch`), and `docs/technical-design.md` section 11 (**Generated element types** + **The rules**). No git commit and no push.
- Measured: build 0 warnings 0 errors (`-p:NuGetAudit=false`); solution tests total 3108, failed 0, skipped 3, exit 0 (3102 + 6); `*GeneratedAuthoringTests` 6/0; `*GeneratedElementTests` 6/0 and `*TemplateReadMethodTests` 4/0 with no edit to either; the `AttributeContext.cs` diff stays additions-only.
- Plants: 6 exit 2, 1 failed (`AMissingRequiredCreateRowIsAnError`); 7 exit 2, 1 failed (`AnEnumRowTakesAKebabKeyword`); 8 exit 2, 2 failed (`AnIntegerTakesASignAndDigits` named, plus `AnIntRowTakesZeroAndANegativeNumber` through the same read). All `error CS` counts 0; all restores `cmp` clean; the final full run is green. Details and the three deviations in the phase 3 group 2 decisions above.
- Open questions: none. Phase 4 takes the six diagnostics, the `CS0108` hiding note, and the `System.Index` limit note above.

### 2026-11-16 21:30
- Phase 4 group 1 (STANDARD brief `phase-4-g1.md`) complete. Tasks 4.1 to 4.4 marked `[x]`.
- Added: `Diagnostics.cs` (229 lines: six descriptors, `LocationInfo`, `DiagnosticInfo`, `ElementResult`), `AnalyzerReleases.Shipped.md`, `AnalyzerReleases.Unshipped.md`, `TemplateElementGenerator.Checks.cs` (254 lines, the moved check helpers), and the phase 4 item group in `BrutalUi.Generators.csproj`. Edited: `TemplateElementGenerator.cs` (372 lines: the pipeline returns `ElementResult`, the transform runs checks 0006/0001/0004, `Emit` reports the faults and rejects duplicate XML names with BRUTALUI0002) and `TemplateElementGenerator.Members.cs` (396 lines: `CheckCreate`, the walk with BRUTALUI0003, `RowSource` wiring for BRUTALUI0005).
- Measured: `Microsoft.CodeAnalysis.Analyzers` 3.11.0 arrives transitively (Risk 3, recorded above); generator project build 0 warnings 0 errors; solution build 0/0 (`-p:NuGetAudit=false`); `dotnet test --solution brutal-ui.sln` 3108 / 0 failed / 3 skipped, before the scratch check and after it. The scratch check printed exactly one `BRUTALUI0001` at (9,23) with 0 `error CS` lines and 1 `Error(s)`; the scratch file is deleted. No git commit and no push.
- Deviations: the scoped `#pragma warning disable RS1032` on the BRUTALUI0006 descriptor (the plan text must stay word for word); the check helpers went to a fourth partial file, not to `Diagnostics.cs`, to keep every file under 400 lines. Both carry the reason in the phase 4 group 1 decisions above.
- Open questions for group 2: the fault counts for two factories (one per extra method) and duplicate row names (one per row of the group), and the 0002 span of a named argument. Group 2 writes the 16 tests against this implementation.

### 2026-11-16 22:05
- Phase 4 group 2 (STANDARD brief `phase-4-g2.md`) complete. Tasks 4.5 to 4.10 marked `[x]`. Phase 4 is complete; the feature moves to phase 5.
- Added: `tests/BrutalUi.Generators.Tests/` with `BrutalUi.Generators.Tests.csproj`, `GeneratorHarness.cs` (247 lines), `DiagnosticTests.cs` (294 lines, tests 0001 to 0004 and the no-code rule), `RowAndTypeDiagnosticTests.cs` (196 lines, tests 0005 and 0006 and the valid sample; one `*DiagnosticTests` filter, total 8), and `GeneratorOutputTests.cs` (236 lines, golden text, name rules, order, empty output, cache). Edited: `brutal-ui.sln` (both projects added through `dotnet sln add`, folders `src` and `tests`). No `src/` file changed in the final state.
- Measured: solution build 0 warnings 0 errors (`-p:NuGetAudit=false`); `dotnet test --solution brutal-ui.sln` total 3124, failed 0, skipped 3, exit 0 (3108 + 16); `*DiagnosticTests` 8/0; `*GeneratorOutputTests` 8/0; Markup.Tests 653/0; Core.Tests 1840/0; `dotnet sln brutal-ui.sln list` names `BrutalUi.Generators` and `BrutalUi.Generators.Tests` and no `BrutalUi.PackageTest`. No existing test changed. No git commit and no push.
- Plants: 9 exit 2, 0 `error CS`, total 3124, failed 1: `TwoTypesWithOneXmlNameGiveBRUTALUI0002`. 10 exit 2, 0 `error CS`, total 3124, failed 1: `AnUnrelatedEditKeepsEachStepCached`. Both restores `cmp` clean; the logs sit in `artifacts/logs/p4g2-*.log`.
- Deviations: none from the brief. The `DiagnosticTests` split into a second file follows the brief's own under-400 rule; the class name keeps the `*DiagnosticTests` filter at total 8. Details in the phase 4 group 2 decisions above.
- Open questions: none. Phase 5 replaces the written table; the 16 generator tests read the current diagnostics and generated text, so phase 5 keeps them green by contract.

### 2026-11-16 22:40
- Phase 5 group 1 (ADVANCED brief `phase-5-g1.md`) complete. Tasks 5.1 to 5.4 marked `[x]`. Group 2 (5.5 to 5.7) stays open.
- Added: `tests/BrutalUi.Markup.Tests/Generated/BuiltInRowListTests.cs` (185 lines, 8 tests) and
  `src/BrutalUi.Markup/Template/BaseRows.cs` (67 lines). Rewrote `src/BrutalUi.Markup/Template/BuiltInElements.cs`
  (268 lines): eight `[TemplateElement("<Type>", Target = typeof(<Type>))]` declarations with their
  `[TemplateFactory]` methods and `[TemplateCreateRow]` attributes; `RegisterAll`, the seven set-row setters, the base
  rows, and the four base setters are gone. Edited `src/BrutalUi.Markup/Template/TemplateLoader.cs` (324 lines): the
  constructor calls `this.RegisterBrutalUiMarkupElements();`, the base rows come from `BaseRows.All` in both places,
  and the constructor doc names the generated call. No `src/BrutalUi.Core`, `src/BrutalUi.Framework`,
  `src/BrutalUi.Markup/Style`, loader-test, Core-test, generator, or sample file changed.
- Measured: 5.1 ran green against the WRITTEN table first (`*BuiltInRowListTests` 8/0, solution 3132 / 0 / 3,
  Markup 661), then green again after the replacement with no test edit. Final: solution build 0 warnings 0 errors;
  `dotnet test --solution brutal-ui.sln` exit 0, total 3132, failed 0, skipped 3; Markup 661 / 0 (653 + 8, the +24 the
  feature plans). Greps: `Register(` 0, `TemplateElement(` 8, `CheckCapacity` one line. Risk 1 did not fire: the walk
  reproduced every row list, so no `MetadataToken` sort went into the generator.
- Generated-file check: `BrutalUiMarkupTemplateElements.g.cs` (177 lines) matches section 11 on every row name, kind,
  order, and leaf flag; `Set0` to `Set6` and `Keywords3` hold the four `Image` rows, `scroll-offset`, `value`, and
  `checked`; the `Element` entry has no `Rows`. Logs: `artifacts/logs/p5g1-t51-test.log`,
  `artifacts/logs/p5g1-t54-test.log`, `artifacts/logs/p5g1-plant11-test.log`, `artifacts/logs/p5g1-plant12-test.log`,
  `artifacts/logs/p5g1-plant13-test2.log`, `artifacts/logs/p5g1-restore-test.log`.
- Plants: 11 exit 2, failed 1 (`ACapacityIsAWholeNumberAboveZero`); 12 exit 2, failed 2
  (`AChildInsideALabelOrAnImageIsAnError`, `TheRowsOfImageStayTheSame`); 13 exit 2 after one host abort (0 `error CS`
  each time), failed 3 (`TheRowsOfImageStayTheSame` named, plus the two generated-list tests that read the same rule).
  All restores `cmp` clean; the generator restore needed `dotnet build-server shutdown` before the green re-run.
- Deviations: the ordinal registration order puts `Button` first, where the brief lists it last (no behavior or test
  depends on it); plant 13 fired two extra list tests, the same shape as plants 1 and 8. Reasons sit in the phase 5
  group 1 decisions above. No git commit and no push.
- Open questions for group 2: none. Task 5.5 must rename **The written table** and fix the line that says the generator
  replaces the table later; task 5.7 runs the scripts and the remaining Verification Method steps.

### 2026-11-16 23:04
- Phase 5 group 2 (ADVANCED brief `phase-5-g2.md`) complete. Tasks 5.5 to 5.7 marked `[x]`. The feature is complete:
  46/46 tasks (100%).
- Edited: `docs/technical-design.md` (section 11: **The element table** rename, the generator sentence, the
  **Custom element types** wording, and the new **The diagnostics** block with the six-id table), `CHANGELOG.md` (one
  `(markup/element-generator)` Added line), `tasks.md` (46/46, 100%), and this file. No `src/` or `tests/` code
  changed. No git commit and no push.
- Measured: build 0 warnings 0 errors; solution tests 3132 / 0 failed / 3 skipped; per-class 6/4/6/8 (Markup) and 8/8
  (Generators); pack layout clean (one analyzer line, nuspec grep 0, six packages); `package-test: PASS Gauge level
  0.5` exit 0; `scripts/test-linux.sh` green through `loader`, then environmental `FAIL at smoke (exit 1)`;
  Verification Method items 1 to 16 recorded in the group 2 decisions above. The Windows run (item 17) and the full
  Linux `smoke` run wait in Needs Human Verification.
- Deviations: `DOTNET_CLI_HOME` moved into the workspace (read-only home); item 12 ran through `git diff --no-index`
  with a `sealed ` alternative, because the feature files stay untracked; the script `pack.log` is empty because the
  packages were up to date. All reasons in the group 2 decisions above.

### 2026-11-16 23:58
- Review group g1 (ADVANCED brief `review-g1.md`) complete: CR-1 and CR-3 are fixed and pinned, and marked
  `- [x]` in `code-review.md`. CR-2, CR-4, and CR-5 stay open. The feature code stays at 46/46 tasks; these are
  review fixes.
- Edited: `src/BrutalUi.Generators/TemplateElementGenerator.Members.cs` (379 lines) and
  `TemplateElementGenerator.Checks.cs` (297 lines). `ElementSourceWriter.cs` did not change: the cast keeps its
  shape, because the walk now models the property that the cast binds to.
  CR-1, `WalkProperties`: one slot per row place, plus a `Dictionary<string, int>` that maps a property name to its
  place. The base-most declaration opens the place; every further declaration of the name takes over the row in
  that place — read kind, row name, keyword set, property name, and the BRUTALUI0005 source. A visible property
  that gives no row (ignore, no public setter, `init`, or an unsupported type) clears the place, so the name gives
  no row at all. A `new` hide of another type no longer keeps the base read kind, and `[TemplateIgnore]` or
  `[TemplateRowName]` on the hider no longer goes missing. The slots compact at the end, so the row order keeps the
  base place. To keep both files under 400 lines, the four attribute reads (`FindAttribute`, `FullName`,
  `AttributeStart`, `AttributeLocation`) moved to the Checks partial, the file that already holds the attribute
  reads. CR-3, `IsInit`: it reads `IMethodSymbol.IsInitOnly` first, and keeps the accessor-syntax route.
- Added tests: `tests/BrutalUi.Markup.Tests/Generated/GeneratedHidingTests.cs` (217 lines, 4 tests; the loader path
  through the generator of this assembly) and `tests/BrutalUi.Generators.Tests/WalkRulesTests.cs` (225 lines, 6
  tests; the driver path, which asserts that the generated text holds no `CS` error). Edited
  `tests/BrutalUi.Generators.Tests/GeneratorHarness.cs` (289 lines): `Run` takes an optional
  `extraReferences`, and `EmitMetadata` compiles a sample assembly in memory, so a walk can read a property from
  metadata with no declaring syntax. Additions only; no existing harness call changed. I chose a new class in each
  project, not an extension of an existing one: the walk rules are their own surface, the `*GeneratedElementTests`
  and `*GeneratorOutputTests` filters keep their counts, and the XML names of the new samples stay unique —
  `Hider`, `HiderName`, `HiderIgnore`, `HiderOverride`.
- CR-3 decision: **closed with the mechanism**, not documented as a limit. Evidence, from the scratch probe in
  `artifacts/probe-cr3` (a console over the pinned `Microsoft.CodeAnalysis` and `Microsoft.CodeAnalysis.CSharp`
  4.14.0; `artifacts/` is gitignored):
  1. The shipped XML doc holds `P:Microsoft.CodeAnalysis.IMethodSymbol.IsInitOnly` — "Returns true for 'init' set
     accessors, and false otherwise." Phase 1 had recorded the wrong flag; the accessor flag is public here.
  2. For a property of an emitted assembly referenced as metadata (`DeclaringSyntaxReferences.Length == 0`):
     `InitAmount { get; init; }` gives `setter.IsInitOnly = True`, and `PlainAmount { get; set; }` gives `False`.
  3. The same two properties read from source (one declaring syntax) give the same answers, so the flag needs no
     syntax and the syntax route stays as the second read.
  4. `IMethodSymbol.MethodKind` stays `PropertySet` for both, and the setter holds no attribute that marks `init`:
     the modreq is visible only through `IsInitOnly`. So the section 11 sentence and the `Target` doc of
     `TemplateElementAttribute.cs` that the review's option (b) asked for are not needed.
- Measured: solution build 0 warnings 0 errors (`-p:NuGetAudit=false`); `dotnet test --solution brutal-ui.sln`
  exit 0, total 3142, failed 0, succeeded 3139, skipped 3 (3132 + 4 + 6); `*GeneratedHidingTests` 4/0;
  `*WalkRulesTests` 6/0; `*GeneratorOutputTests` 8/0 and `*DiagnosticTests` 8/0 with no golden-literal edit, and
  `*BuiltInRowListTests` 8/0. The emission of the shipped table did not move: with the fixed generator, the fresh
  emit of `BrutalUiMarkupTemplateElements.g.cs`
  (`-p:CompilerGeneratedFilesOutputPath=artifacts/logs/g1/genmarkup`) is `cmp`-identical to the phase 5 snapshot.
  No file over 400 lines; no `src/BrutalUi.Core`, `src/BrutalUi.Framework`, `src/BrutalUi.Markup/Style`,
  `tests/BrutalUi.Markup.Tests/Template`, `samples`, or `AttributeContext.cs` change. No git commit and no push.
- Plants: both fixes reverted to the shapes the review found (keep the base row for a repeated name; read the
  accessor syntax only) — `*WalkRulesTests` 6 total, failed 5, passed 1. `AHidingPropertyTakesTheDerivedReadKind`
  failed with `error CS0266: Cannot implicitly convert type 'float' to 'int'` at
  `SampleTemplateElements.g.cs(28,60)`, and `AnInitPropertyFromMetadataGivesNoRow` failed with `error CS8852:
  Init-only property or indexer 'Widget.Count' can only be assigned in an object initializer...` at
  `SampleTemplateElements.g.cs(37,17)` — the two symptom classes, inside the generated file. The override pin
  passes both ways; it is the regression guard. With the reverted walk left in place, the new sample types also
  broke the `BrutalUi.Markup.Tests` build with two `CS0266` errors at
  `BrutalUiMarkupTestsTemplateElements.g.cs(138,82)` and `(154,87)` — CR-1 in a real consumer build. The restore
  ran `dotnet build-server shutdown` first, then recompiled green. Logs and snapshots: `artifacts/logs/g1/`.
- Deviations: the brief put the three CR-1 pins in `tests/BrutalUi.Markup.Tests/Generated/`; I wrote four there
  (the rename face of CR-1 needs its own pin) and six driver pins in the generator tests, because the "no `CS`
  error inside the generated file" half of CR-1 and the metadata half of CR-3 are visible only through the driver,
  and the metadata case needs an assembly without syntax. The probe under `artifacts/probe-cr3` stays as evidence.
- Open questions: (1) the same 4.14 surface exposes `IPropertySymbol.IsRequired`; a `required` property of a target
  type still gives a row, and the generated `new T()` then meets `CS9035`. Outside CR-1 and CR-3, so nothing
  changed here; it is a candidate for the next review round. (2) The `obj/**/generated/` copy of a generated file
  does not refresh under an incremental build, so the byte-identity check used an explicit output path.

### 2026-11-17 00:20
- Review group g2 (brief `review-g2.md`) complete. CR-2 fixed; `- [x] CR-2` marked in `code-review.md`.
- Change: `TemplateElementGenerator.cs` `CheckElementType` — moved the `classSymbol.IsGenericType || IsNestedInGenericType(classSymbol)`
  check out of the `target is null` branch, so both forms run it before `HasReachableAccess`, with the existing clause
  "it is a generic type". A declaration class inside a generic class now gives `BRUTALUI0006` and no code, instead of
  emitting `global::Ns.Holder<T>.Decl.Create(args)` and a `CS0246` in the register file. No descriptor or message change.
- Pin: extended `RowAndTypeDiagnosticTests.ABadElementTypeGivesBRUTALUI0006` with `internal static class GenericHolder<T>`
  holding a marked declaration class (`Target = typeof(Dial)`): case list 5 → 6, one added assert at (36, 27), the "1x"
  case renumbered to line 42. No new test method; the `*DiagnosticTests` total stays 8.
- Measured: `*DiagnosticTests` 8 / 0 failed; full suite `dotnet test --solution brutal-ui.sln` total 3142, failed 0,
  succeeded 3139, skipped 3 — the g1 total, unchanged. Solution build 0 warnings, 0 errors.
- Plant (the pre-fix shape, check back inside the `target is null` branch): the pin failed with
  `Assert.Equal() Expected: 6, Actual: 5` — the nested declaration class escaped `BRUTALUI0006`, the CR-2 symptom.
  Restored; the run above is the restored green.
- Deviations: the brief names `TemplateElementGenerator.Checks.cs`; the live `CheckElementType` sits in
  `TemplateElementGenerator.cs:237-299` (the review cites the same lines), so the fix landed there. No plants: the
  brief asks for the fix plus a pin only. Protected dirs untouched; no git commit, no push.
- Open questions: none.

### 2026-11-17 00:12
- Review group g3 (brief `review-g3.md`) complete. CR-4 and CR-5 fixed; `- [x] CR-4` and `- [x] CR-5` marked in
  `code-review.md`.
- CR-4: new test class `tests/BrutalUi.Markup.Tests/Generated/TemplateAttributeTests.cs` — one test
  `ANullNameThrowsInEachNamedConstructor` with three asserts: `Assert.Throws<ArgumentNullException>` for
  `new TemplateElementAttribute(null!)`, `new TemplateCreateRowAttribute(null!)`, and
  `new TemplateRowNameAttribute(null!)`. A new class, so the six planned Generated class counts stay exact.
  Per-class totals now: `*BuiltInRowListTests` 8, `*GeneratedAuthoringTests` 6, `*GeneratedElementTests` 6,
  `*GeneratedHidingTests` 4, `*TemplateAttributeTests` 1, `*TemplateReadMethodTests` 4.
- CR-5: `docs/technical-design.md` kebab sentence (section 11) — restored the dropped clause: "and before an
  upper-case letter that follows an upper-case letter and comes before a lower-case letter". It now matches
  The kebab rule of `implementation.md` word for word. `NameRules.cs` unchanged.
- Carry-over (plan Notes item 9, option a): added two sentences under **Generated element types** in
  `docs/technical-design.md`: a game type with a `required` property and no factory fails with `CS9035` in the
  generated code, `BRUTALUI0001` does not detect it, and such a type needs a `[TemplateFactory]`.
- Measured: `*TemplateAttributeTests` 1 / 0 failed; Markup project 666; full suite
  `dotnet test --solution brutal-ui.sln` total 3143, failed 0, succeeded 3140, skipped 3 — the g2 total + 1.
  Solution build 0 warnings, 0 errors.
- Deviations: none. No generator source change; protected dirs untouched; no git commit, no push.
- Open questions: none.

### 2026-11-17 00:22 - run close (/autorun-feature markup/element-generator)
- The autonomous run is complete: 5 phases, 46/46 tasks, all 13 plan plants plus the g1/g2 review plants recorded, the cross-phase review resolved 5/5, and the post-review gate PASS: build 0 warnings / 0 errors, solution tests 3143 / 0 failed / 3 skipped, package path green (`package-test: PASS Gauge level 0.5`, one `analyzers/dotnet/cs` line), blast radius empty, `code-review.md` 5/5 checked.
- Inbox: no lines were written during the run; nothing to promote.
- `implementation.md` `Status:` set to Ready for Review. Next feature of the epic: `markup/binding-attributes` (the hook contract is unchanged, so it can build now).
- Human steps: the five phase demos and the two host runs (Linux `smoke`, Windows script) under Needs Human Verification; then commit, push, and open the PR for `feat/markup/element-generator`.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
