# Element Generator - Implementation Plan

**Epic:** markup (feature 3 of 6)
**Status:** Ready for Review
**Started:** 2026-11-16
**Target Completion:** TBD
**Last Updated:** 2026-11-17 00:22

---

## Quality Bar

**Target quality:** A build-time library feature. A Roslyn source generator writes the element table of the XML loader, for a game type and for the eight built-in types. The output runs in each game build, so a fault in the generator becomes a fault in each consumer. Six things carry the feature.

- **Correct generated code:** The generated rows have the same names, kinds, order, and leaf flags as the written table of xml-element-loader. A golden test pins the full output for a sample type. The output compiles with 0 warnings under `TreatWarningsAsErrors` in Markup, in `BrutalUi.Markup.Tests`, and in a project that references the package.
- **Clear diagnostics:** Six error diagnostics (`BRUTALUI0001` to `BRUTALUI0006`) stop the build before a bad type reaches the generated code. Each one sits on the code at fault, quotes it, and names the fix in ASD-STE100 prose. A type with an error gets no generated code, so the build shows the diagnostic and no follow-up `CS` error.
- **Incremental behavior:** The pipeline models are value-equatable and hold no `ISymbol`, `SyntaxNode`, `Location`, or `Compilation`. A test proves that an unrelated edit leaves each tracked step `Cached` or `Unchanged`, and a plant with a non-equatable array fails it.
- **The pack layout:** `dotnet pack` puts `BrutalUi.Generators.dll` in `analyzers/dotnet/cs` of the Markup package, and nowhere else. No `BrutalUi.Generators` package exists, and the Markup package lists no dependency on it. Both test scripts pack, then build and run a project outside the solution that gets the generated rows from the package.
- **No change in the loaded tree:** Each test of xml-element-loader passes with no edit (`git diff` of `tests/BrutalUi.Markup.Tests/Template/` is empty). `BuiltInRowListTests` pins the row list and the leaf flag of each built-in type, before and after the replacement. The 0-byte gate stays green. The generated path uses no reflection.
- **Tests with plants and accurate docs:** Each phase plants a fault and proves that a named test fails. Section 3, 11, and 14 of `technical-design.md` name the new project, the attributes, the rules, the diagnostics, and the package test. Each new public member has an XML doc comment in ASD-STE100 prose. Each new or changed source file stays under 400 lines.

---

## Executive Summary

This feature adds a new project, `BrutalUi.Generators`, with one incremental source generator. The generator ships inside the `BrutalUi.Markup` package as an analyzer (epic A4).

- A game puts `[TemplateElement("Gauge")]` on its element class. The generator writes one `ElementTypeInfo` for it: a factory and one set row for each public settable property of a supported type, with a kebab-case name (Q2, A4). One generated extension method, for example `loader.RegisterHelloPanelElements()`, registers all marked types of the assembly (A3). The manual `TemplateLoader.Register` call stays public (epic A5).
- A game type can mark a static factory method with `[TemplateFactory]` and its create rows with `[TemplateCreateRow]` (Q3). `[TemplateIgnore]` skips a property, and `[TemplateRowName]` gives a row another name (Q2).
- The same attribute sits on a static declaration class with `Target = typeof(Slider)`. Markup declares each Core type that way, with the written factory and its create rows (Q1, A7). One `ForAttributeWithMetadataName` pipeline covers both forms (the Chosen Approach). The written set rows and `RegisterAll` go away. The four base rows stay written code (A6).
- Two new public reads on `AttributeContext`, `TryReadInteger` and `TryReadKeyword`, serve the `int` rows and the enum rows, because generated code in a game assembly cannot call the internal `TemplateValues`.
- Six error diagnostics cover a type with no usable constructor and no factory, a duplicate XML name, a row-name attribute on a property with no row (A5), a bad factory, a bad row name, and a bad element type.
- A new test project, `tests/BrutalUi.Generators.Tests`, compiles sample source in memory (A8). A console project outside the solution, `tests/BrutalUi.PackageTest`, references the packed Markup package from `artifacts/packages` (Q4).

The plan has 5 phases and 46 tasks, about 405 steps. Phase 1 (11 tasks) proves the idea with the confirmed demo: a marked `Gauge` loads with `Level` 0.5 (Q5). Phase 2 (8 tasks) ships the generator in the package and adds the package test to both scripts. Phase 3 (10 tasks) adds the full authoring surface for a game type. Phase 4 (10 tasks) adds the diagnostics and the generator test project. Phase 5 (7 tasks) replaces the written table of the built-in types. Phases 1, 2, and 5 need an advanced dev agent.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The game developer** marks a custom element class with one attribute, calls one register method, and uses the element in a template. A mistake in the class gives a build error at the class, not an exception at load time.
- [ ] **The markup author** uses a custom element with the same attribute forms and the same positioned errors as a built-in element.
- [ ] **The framework contributor** of markup-sample and the Phase 4 epics (`ListView`, `DataList`, `WindowChrome`) adds an element type with one declaration and gets the rows from the generator.

**User Subsets Served:**
- [ ] **Simple** - `[TemplateElement("Gauge")] sealed class Gauge : Element { public float Level { get; set; } }`, then `loader.RegisterMyGameElements()`, then `<ui:Gauge level="0.5"/>`.
- [ ] **Complex** - a game type with a factory, a required create row, an enum keyword, an `int`, a renamed row, an ignored property, and a declaration class for a type of another assembly.

**UX Approach:**
- Build time: the generator runs in each build and in the IDE. A fault gives one error diagnostic with a fix.
- Load time: the generated register call and the generated setters run before the frame loop. They use no reflection.
- Run time: no change. A loaded tree allocates nothing in `Update` and `Draw`.

---

## Goals

### Primary Goals
1. Add `BrutalUi.Generators` with an incremental source generator, and ship it in `analyzers/dotnet/cs` of the Markup package (requirements lines 12 and 20).
2. Write the rows of a marked game type from its public settable properties of a supported type (lines 13 and 14, Q2, A4, A5).
3. Report a diagnostic for a type with no usable constructor and no factory, and for a duplicate XML name (line 15, Q3).
4. Register the marked types of an assembly with one generated call, and keep `Register` public (line 16, A3).
5. Write the rows of the eight built-in types, remove the written table, and keep each xml-element-loader test unchanged (line 17, Q1, A6, A7).
6. Use no reflection at run time on the generated path (line 18).
7. Pin `Microsoft.CodeAnalysis.CSharp`, add no native dependency, and prove the package path in both scripts (lines 19 to 21, Q4).

### Success Criteria
- [ ] The phase 1 demo passes: `GeneratedElementTests` reads a total of 6 with `failed: 0`, and `AMarkedGaugeLoadsWithItsLevel` is one of them.
- [ ] The phase 2 demo passes: the package test prints `package-test: PASS Gauge level 0.5`, and `scripts/test-linux.sh` prints a PASS line that names `pack` and `package`.
- [ ] The phase 3 demo passes: `GeneratedAuthoringTests` reads 6 and `TemplateReadMethodTests` reads 4, each with `failed: 0`.
- [ ] The phase 4 demo passes: `DiagnosticTests` reads 8 and `GeneratorOutputTests` reads 8, each with `failed: 0`.
- [ ] The phase 5 demo passes: `BuiltInRowListTests` reads 8 with `failed: 0`, and `BuiltInElements.cs` holds no `Register` call.
- [ ] Each existing test passes with no edit. The planned delta is Markup +24 and Generators +16, so the solution grows from 3092 to 3132 with 3 skipped. Task 1.1 records the real baseline, and the deltas count from it.

---

## Architecture Overview

### The Place in the Epic

element-generator is feature 3 of 6. It reads the table shape of xml-element-loader (`ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, the public `Register`, the `TryRead*` reads) and replaces `BuiltInElements.cs`. It does not change the hook contract (`AddAttributeHandler`, `AttributeHandler`, `AttributeContext.AddError`), so binding-attributes can build in parallel. markup-sample adds one marked type to HelloPanel later (A10). The Phase 4 epics declare their types the same way.

```
BrutalUi.Generators (netstandard2.0, build time only, not a package)
  TemplateElementGenerator  -- ForAttributeWithMetadataName("BrutalUi.Markup.TemplateElementAttribute")
        |  finds the attributes by full name (A1). No reference to Markup or Core
        v
  <AssemblyId>TemplateElements.g.cs  -- internal static class in namespace BrutalUi.Markup
        |  calls the public API of Markup
        v
BrutalUi.Markup (net10.0, references BrutalUi.Core only at run time)
  Template/TemplateElementAttribute.cs, TemplateMemberAttributes.cs   public marker attributes (A1)
  Template/AttributeContext.cs       + TryReadInteger, + TryReadKeyword
  Template/BaseRows.cs               the four written base rows (A6)
  Template/BuiltInElements.cs        8 declarations + the written factories (Q1, A7)
  Template/TemplateLoader.cs         the constructor calls the generated RegisterBrutalUiMarkupElements()
  BrutalUi.Markup.csproj             analyzer ProjectReference + pack item analyzers/dotnet/cs
```

### File Tree (end state)

```
Directory.Packages.props                              # phase 1: pin Microsoft.CodeAnalysis.CSharp 4.14.0
brutal-ui.sln                                         # phase 4: add BrutalUi.Generators and BrutalUi.Generators.Tests
src/BrutalUi.Generators/                              # new project
├── BrutalUi.Generators.csproj                        # phase 1. phase 4: release files, InternalsVisibleTo
├── Polyfills.cs                                      # phase 1: IsExternalInit for the record models
├── Models.cs                                         # phase 1: EquatableArray<T>, ElementModel, SetRowModel. phase 3: create rows, keywords
├── NameRules.cs                                      # phase 1: kebab-case, assembly id
├── TemplateElementGenerator.cs                       # phase 1: pipeline and transform. phase 3: factory, target, int, enum. phase 4: checks
├── ElementSourceWriter.cs                            # phase 1. phase 3: factory, create rows, keywords
├── Diagnostics.cs                                    # phase 4: 6 descriptors, LocationInfo, DiagnosticInfo, ElementResult
├── AnalyzerReleases.Shipped.md                       # phase 4
└── AnalyzerReleases.Unshipped.md                     # phase 4
src/BrutalUi.Markup/
├── BrutalUi.Markup.csproj                            # phase 2: analyzer ProjectReference, pack item
└── Template/
    ├── TemplateElementAttribute.cs                   # phase 1: Name, IsLeaf. phase 3: Target
    ├── TemplateMemberAttributes.cs                   # phase 3: TemplateFactory, TemplateCreateRow, TemplateRowName, TemplateIgnore
    ├── AttributeContext.cs                           # phase 3: TryReadInteger, TryReadKeyword (additions only)
    ├── TemplateValues.cs                             # phase 3: TryParseInteger
    ├── BaseRows.cs                                   # phase 5: the base rows move here from BuiltInElements.cs
    ├── BuiltInElements.cs                            # phase 5: declarations and factories only
    └── TemplateLoader.cs                             # phase 5: the generated register call, BaseRows.All
tests/BrutalUi.Markup.Tests/
├── BrutalUi.Markup.Tests.csproj                      # phase 1: analyzer ProjectReference
└── Generated/                                        # new folder. Template/ does not change
    ├── GeneratedElementTests.cs                      # phase 1: 6 tests
    ├── TemplateReadMethodTests.cs                    # phase 3: 4 tests
    ├── GeneratedAuthoringTests.cs                    # phase 3: 6 tests
    └── BuiltInRowListTests.cs                        # phase 5: 8 tests
tests/BrutalUi.Generators.Tests/                      # new project, phase 4
├── BrutalUi.Generators.Tests.csproj
├── GeneratorHarness.cs
├── DiagnosticTests.cs                                # 8 tests
└── GeneratorOutputTests.cs                           # 8 tests
tests/BrutalUi.PackageTest/                           # new project outside the solution, phase 2
├── BrutalUi.PackageTest.csproj
├── nuget.config
└── Program.cs
scripts/test-linux.sh, scripts/test-windows.ps1       # phase 2: pack and package steps
docs/technical-design.md                              # phase 2: sections 3 and 14. phase 3 and 5: section 11
CHANGELOG.md                                          # phase 5
docs/features/markup/element-generator/context.md     # each phase
```

These files do not change: `src/BrutalUi.Core/`, `src/BrutalUi.Framework/`, `src/BrutalUi.Markup/Style/`, `tests/BrutalUi.Markup.Tests/Template/`, `tests/BrutalUi.Core.Tests/`, and `samples/` (A10).

### Public API (end state)

All new types are in the namespace `BrutalUi.Markup` (A1).

```csharp
namespace BrutalUi.Markup;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TemplateElementAttribute : Attribute
{
    public TemplateElementAttribute(string name);    // the XML local name, for example "Gauge"
    public string Name { get; }
    public bool IsLeaf { get; set; }                 // A9. The default is false
    public Type? Target { get; set; }                // phase 3. null: the marked class is the element type.
                                                     // Set: the marked class is a static declaration class for Target
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TemplateFactoryAttribute : Attribute { }          // static Element? M(ElementCreateArgs args)

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TemplateCreateRowAttribute : Attribute
{
    public TemplateCreateRowAttribute(string name);  // a create row that the factory reads with TryGet
    public string Name { get; }
    public bool IsRequired { get; set; }             // true: AttributeRowKind.CreateRequired
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class TemplateRowNameAttribute : Attribute
{
    public TemplateRowNameAttribute(string name);    // another row name for the property (Q2)
    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class TemplateIgnoreAttribute : Attribute { }           // the property gives no row (Q2)

public readonly ref struct AttributeContext                           // existing type, two additions
{
    public bool TryReadInteger(out int value);                        // an optional sign and digits, in the int range
    public bool TryReadKeyword(ReadOnlySpan<string> keywords, out int index);  // ordinal, after the trim
}
```

The count: 5 new public types and 2 new public methods. `TemplateLoader`, `ElementTypeInfo`, `AttributeRow`, the delegates, and the hook do not change.

### The Generator Pipeline

```
Initialize(context):
  models = context.SyntaxProvider.ForAttributeWithMetadataName(
               "BrutalUi.Markup.TemplateElementAttribute",
               predicate: node is ClassDeclarationSyntax,
               transform: ReadElement(ctx, ct) -> ElementResult)       // value-equatable
           .WithTrackingName("TemplateElementModels")
  assemblyId = context.CompilationProvider
               .Select((c, _) => NameRules.ToIdentifier(c.AssemblyName))
               .WithTrackingName("TemplateElementAssemblyId")            // a string, so an edit gives Unchanged
  all = models.Collect().Combine(assemblyId).WithTrackingName("TemplateElementSet")
  context.RegisterSourceOutput(all, Emit)

Emit(spc, (results, id)):
  report each DiagnosticInfo of each result (phase 4)
  group the valid models by XML name. A name on more than one model -> BRUTALUI0002 on each (phase 4)
  no valid model -> add no source
  sort the valid models by XML name (ordinal), write "<id>TemplateElements.g.cs"
```

- **The models.** `ElementResult` holds an `ElementModel?` and an `EquatableArray<DiagnosticInfo>`. `ElementModel` holds strings, flags, and `EquatableArray<T>` values only: the XML name, the leaf flag, the global target type name, the create kind (constructor or factory), the global factory call, the create rows, and the set rows. A `SetRowModel` holds the row name, the property name, the read kind, and for an enum the keywords with the global member names. A `DiagnosticInfo` holds the descriptor id, a `LocationInfo` (file path, `TextSpan`, `LinePositionSpan`), and the message arguments.
- **`EquatableArray<T>`** wraps an `ImmutableArray<T>` with element-wise `Equals` and a hash that combines the element hashes by hand, because `System.HashCode` is not in `netstandard2.0`.
- **The assembly id** comes from `Select` on the compilation, never from `Combine` with the compilation itself. A `Combine` with the compilation runs the output on each keystroke.

### The Rules

**The element type (both forms).**
- **Game form** (`Target` is `null`): the marked class is the element type. It derives from `BrutalUi.Core.Element`, is not static, is not generic, and is not nested in a generic type.
- **Declaration form** (`Target` is set): the marked class is static. `Target` is `BrutalUi.Core.Element` or a type derived from it, and it is not an open generic type.
- The marked class and each containing class have `internal` or `public` access, so the generated class can reach them. The XML name is a valid XML local name.

**The factory and the constructor (Q3).**
- A `[TemplateFactory]` method on the marked class makes the element. It is static, not generic, `internal` or `public`, has one by-value parameter of type `ElementCreateArgs`, and returns `Element` or a type derived from it. A class holds one factory at most. `[TemplateCreateRow]` sits only on the factory. The create rows follow the order of the attributes in the source.
- With no factory, the generator calls the constructor with no parameters of the target type. The constructor is `public`, or `internal` when the target is in the compiling assembly. The target type is not abstract.
- No factory and no such constructor gives `BRUTALUI0001`.

**The property walk (Q2, A4, A5, A6).**
- The walk visits the target type and each base type up to `BrutalUi.Core.Element`. It never visits the members of `Element`, so `Name`, `IsPickable`, `IsFocusable`, and `States` give no row. The base rows stay written code (A6).
- A property gives a row when it is an instance property, not an indexer, `public`, with a `public` set accessor that is not `init`, of a supported type, and with no `[TemplateIgnore]`.
- A property of another type gives no row and no diagnostic (A5). `[TemplateRowName]` on a property that gives no row gives `BRUTALUI0003`.
- A property that overrides or hides a base property keeps the place of the base property and gives one row.

**The supported types (A4).**

| Property type | Match | The generated read | Value form |
|---|---|---|---|
| `string` | `SpecialType.System_String` | `= context.Value` | text, as is |
| `float` | `SpecialType.System_Single` | `context.TryReadNumber(out float value)` | number |
| `int` | `SpecialType.System_Int32` | `context.TryReadInteger(out int value)` (new) | integer |
| `bool` | `SpecialType.System_Boolean` | `context.TryReadBool(out bool value)` | bool |
| an enum | `TypeKind.Enum` | `context.TryReadKeyword(Keywords3, out int index)` (new), then a `switch` | kebab-case keyword |
| `float2` | `Brutal.Numerics.float2` | `context.TryReadNumbers(out float2 value)` | 2 numbers |
| `float4` | `Brutal.Numerics.float4` | `context.TryReadNumbers(out float4 value)` | 4 numbers |
| `Rgba8` | `BrutalUi.Core.Rgba8` | `context.TryReadColor(out Rgba8 value)` | color |
| `TextureHandle` | `BrutalUi.Core.TextureHandle` | `context.TryReadTexture(out TextureHandle value)` | texture name |

Task 1.8 confirms the metadata names of `float2` and `float4` against the `Brutal.Numerics` assembly. A nullable value type (`float?`) is another type and gives no row. `string?` is `string`.

**The kebab rule.** A hyphen goes before an upper-case letter that follows a lower-case letter or a digit, and before an upper-case letter that follows an upper-case letter and comes before a lower-case letter. Then each letter becomes lower case. `Level` gives `level`. `ScrollOffset` gives `scroll-offset`. `Uv` gives `uv`. `ScaleMode` gives `scale-mode`. `HPBar` gives `hp-bar`. `MaxHP` gives `max-hp`. `Level2` gives `level2`. An enum member follows the same rule: `StretchToFill` gives `stretch-to-fill`.

**The row order.** The create rows come first, in the order of the `[TemplateCreateRow]` attributes. The set rows follow: the rows of the base type nearest to `Element` first, then each type down to the target, each in declaration order. The loader runs the factory before the set rows anyway, so `value` of a `Slider` runs after `min`, `max`, and `step`. The order also fixes the text of the `UnknownAttribute` message, which the tests read.

**The register call (A3).** The assembly id keeps the ASCII letters, the digits, and `_` of the assembly name, and drops each other character. An id that starts with a digit gets a `_` before it. The generated class is `internal static class <Id>TemplateElements` in the namespace `BrutalUi.Markup`, with one method, `internal static void Register<Id>Elements(this TemplateLoader loader)`. A game file needs `using BrutalUi.Markup;` for `TemplateLoader` already, so the method is visible with no other `using`. The id is in the method name, because `BrutalUi.Markup.Tests` sees the internals of Markup: two methods with one name would be ambiguous (CS0121). No module initializer registers a type (A3).

### The Generated Source

For the phase 1 sample in `BrutalUi.Markup.Tests`, the output looks like this. The golden test of task 4.9 pins the exact text of its own sample.

```csharp
// <auto-generated/>
#nullable enable

namespace BrutalUi.Markup
{
    internal static class BrutalUiMarkupTestsTemplateElements
    {
        internal static void RegisterBrutalUiMarkupTestsElements(this global::BrutalUi.Markup.TemplateLoader loader)
        {
            global::System.ArgumentNullException.ThrowIfNull(loader);

            loader.Register(new global::BrutalUi.Markup.ElementTypeInfo
            {
                Name = "Gauge",
                Create = static (global::BrutalUi.Markup.ElementCreateArgs args) => new global::BrutalUi.Markup.Tests.Generated.Gauge(),
                Rows = new global::BrutalUi.Markup.AttributeRow[]
                {
                    new global::BrutalUi.Markup.AttributeRow("level", static (global::BrutalUi.Core.Element element, global::BrutalUi.Markup.AttributeContext context) => Set0(element, context)),
                },
                IsLeaf = false,
            });
        }

        private static void Set0(global::BrutalUi.Core.Element element, global::BrutalUi.Markup.AttributeContext context)
        {
            if (context.TryReadNumber(out float value))
            {
                ((global::BrutalUi.Markup.Tests.Generated.Gauge)element).Level = value;
            }
        }
    }
}
```

- Each type name is `global::` qualified. The source uses `"\n"` line ends, no collection expression, and no C# 14 feature, so it compiles in a consumer with C# 11 or later (the `required` members of `ElementTypeInfo` need C# 11).
- A factory type gives `Create = static (... args) => global::Ns.Decl.Create(args)`, and its create rows give `new AttributeRow("max", AttributeRowKind.CreateRequired)`.
- An enum row gives one `private static readonly string[] Keywords<n>` and a `switch` on the index with the `global::` member names. It uses no `Enum.Parse`.

### The Diagnostics (phase 4)

Each diagnostic has the category `BrutalUi.Markup` and the severity `Error`. A type with an error gets no generated code. The other types of the assembly keep their code, so the register call still compiles.

| Id | Cause | Location | Message (ASD-STE100) |
|---|---|---|---|
| `BRUTALUI0001` | No factory, and no usable constructor with no parameters (also an abstract target) | the class name | `'{0}' has no usable constructor and no factory. Add a public constructor with no parameters, or mark a static method with [TemplateFactory].` |
| `BRUTALUI0002` | Two marked classes of one compilation share one XML name (ordinal) | the name argument of each attribute | `The XML name '{0}' is on more than one element type: {1}. Give each type its own name.` |
| `BRUTALUI0003` | `[TemplateRowName]` on a property that gives no row (A5) | the attribute | `[TemplateRowName] is on '{0}', but '{0}' gives no row. Use a public settable property of a supported type, or remove the attribute.` |
| `BRUTALUI0004` | A factory that breaks a rule of The Rules, two factories, or `[TemplateCreateRow]` on a method with no `[TemplateFactory]` | the method name | `'{0}' is not a valid factory: {1}. Write 'static Element? Create(ElementCreateArgs args)' with internal or public access.` |
| `BRUTALUI0005` | A row name that is not a valid XML local name, that two rows share, or that a base row takes (`name`, `class`, `pickable`, `focusable`) | the property or the create-row attribute | `The row name '{0}' of '{1}' is not valid: {2}. Give each row a valid XML name that no other row and no base row takes.` |
| `BRUTALUI0006` | A marked class that breaks a rule of The element type, or an XML name that is not valid | the class name | `'{0}' is not a valid element type: {1}.` |

`{1}` and `{2}` of 0004, 0005, and 0006 name the broken rule in one short clause, for example `it is not static`. The XML name check uses `XmlConvert.IsStartNCNameChar` and `XmlConvert.IsNCNameChar`, which `netstandard2.0` holds. A clash with a built-in name (for example a game type `Label`) is a run-time `ArgumentException` of `Register`, as today (Notes item 7).

### The Project Files

`src/BrutalUi.Generators/BrutalUi.Generators.csproj` (phase 1, the release files in phase 4):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- The Roslyn host loads an analyzer for netstandard2.0 only. The root sets net10.0. -->
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <Description>The source generator of the brutal-ui element table. It ships inside the BrutalUi.Markup package.</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
  </ItemGroup>

  <!-- Phase 4. -->
  <ItemGroup>
    <AdditionalFiles Include="AnalyzerReleases.Shipped.md" />
    <AdditionalFiles Include="AnalyzerReleases.Unshipped.md" />
    <InternalsVisibleTo Include="BrutalUi.Generators.Tests" />
  </ItemGroup>

</Project>
```

- The project keeps `LangVersion` 14 and `Nullable` from the root. `LangVersion` is a compiler setting and is valid for each target. `Polyfills.cs` adds `System.Runtime.CompilerServices.IsExternalInit`, so the models can be `record` types. The code uses no API that `netstandard2.0` does not hold (no `System.HashCode`, no `string.Join(char, ...)`).
- `IsPackable` `false` and `GenerateDocumentationFile` `false` override `src/Directory.Build.props`. The project is not a package, and it has no public API.
- `EnforceExtendedAnalyzerRules` turns on the RS rules for a compiler extension. RS1035 bans some APIs, for example `Environment.NewLine`, so the writer uses `"\n"`.

`src/BrutalUi.Markup/BrutalUi.Markup.csproj` (phase 2):

```xml
<!-- The generator runs on the declarations of Markup, and the pack puts it in analyzers/dotnet/cs. -->
<ItemGroup>
  <ProjectReference Include="../BrutalUi.Generators/BrutalUi.Generators.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />
</ItemGroup>
<ItemGroup>
  <None Include="../BrutalUi.Generators/bin/$(Configuration)/netstandard2.0/BrutalUi.Generators.dll"
        Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
</ItemGroup>
```

- `ReferenceOutputAssembly="false"` keeps the generator out of the compile references and out of `lib/net10.0`. `PrivateAssets="all"` keeps it out of the package dependencies.
- An analyzer from a `ProjectReference` does not flow to the next project. `BrutalUi.Markup.Tests` (phase 1) and HelloPanel (markup-sample, Notes item 8) each add the same `ProjectReference` line, with no `PrivateAssets`. A package consumer gets the analyzer from the package with no extra line.

`tests/BrutalUi.PackageTest/` (phase 2, outside the solution):

- `BrutalUi.PackageTest.csproj`: `OutputType` `Exe`. `<PackageReference Include="BrutalUi.Markup" VersionOverride="$(Version)" />` (central package management stays on, and the root gives `$(Version)`). `<RestorePackagesPath>$(MSBuildThisFileDirectory)../../artifacts/package-test/packages/</RestorePackagesPath>`, so a new pack of the same version never meets a stale copy in the global packages folder. No `ProjectReference`.
- `nuget.config`: `<clear />`, a source `local` at `../../artifacts/packages`, the source `nuget.org`, and `<packageSourceMapping>` with `BrutalUi.*` on `local` and `*` on `nuget.org`. The mapping stops NU1507 (central package management with two sources), which `TreatWarningsAsErrors` makes an error.
- `Program.cs`: marks `[TemplateElement("Gauge")] internal sealed class Gauge : Element { public float Level { get; set; } }`, calls `loader.RegisterBrutalUiPackageTestElements()`, loads `<ui:Gauge xmlns:ui="brutal-ui" level="0.5" />` through the `u8` span overload, prints `package-test: PASS Gauge level 0.5`, and returns 0. Another level, or an error, prints `package-test: FAIL ...` to standard error and returns 1.

### The Script Steps (phase 2)

Both scripts get three steps after `test` and before the GPU steps:

1. `pack` - `dotnet pack brutal-ui.sln --no-build --configuration Debug`. `dotnet pack` builds Release by default, and the build step builds Debug, so the step names the configuration.
2. Delete `artifacts/package-test`. Then `package-restore` - `dotnet restore tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --force`.
3. `package` - `dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --no-restore`.

The PASS line names the new steps, for example `test-linux: PASS build, test, pack, package, gpu, smoke (120 frames) in 95 s`.

---

## Implementation Phases

The feature has 46 tasks, so it has five slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Rule 11 binds phase 1 to the demo of Q5: a marked `Gauge` loads with `Level` 0.5. The package comes second, because a game uses the generator only through the package. The full authoring surface comes third, because each real game type needs a factory, an enum, or an `int`. The diagnostics come fourth: they guard a path that works already. The built-in replacement comes last: it gives the user no new path, and it needs the factory, the declaration form, the enum rows, and the diagnostics of phases 3 and 4.

**The agent tier.** Phase 1 is ADVANCED (opus). It sets the new project, the incremental pipeline, and the model shape that each later phase extends. Phase 2 is ADVANCED (opus). It touches the pack layout, NuGet restore, the local package source, and both test scripts. Phase 5 is ADVANCED (opus). It replaces the table that the loader and 59 tests read, and it touches `TemplateLoader`. Phases 3 and 4 are STANDARD (sonnet). They extend the pipeline and the writer in the shape that phase 1 fixes, and they add tests.

**The commands.** Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. Each task group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class with `tests/<Project>/bin/Debug/net10.0/<Project> --filter-class '*<Class>' 2>&1 | tail -n 5`. To read the generated source, build a consumer with `-p:EmitCompilerGeneratedFiles=true` and read `obj/Debug/net10.0/generated/BrutalUi.Generators/BrutalUi.Generators.TemplateElementGenerator/<Id>TemplateElements.g.cs`. After each change of the generator, run `dotnet build-server shutdown` before the build, because the compiler server can keep the old analyzer. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces.

**The plants.** For each plant, copy the file to `<project>/obj/plantscratch/` (git-ignored), make the change, run `dotnet build-server shutdown`, and run the solution tests with `dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Check the `error CS` count first: it must be 0 for a plant that expects a failed test, because a plant that does not compile leaves the old binary. Expect an exit code that is not 0. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time. Run a second time before you treat it as a fault.

**The tests.** Follow the style of `CustomTypeTests` and `TemplateValueTests`. Pass a template as a `u8` literal to the span overload, because `BRUTALCORE0011` rejects a string literal for `Load(string, string)`. Each error assert checks `Kind`, `Line`, and `Column`, and checks the message with `Contains` on the quoted text only. The test namespace is `BrutalUi.Markup.Tests.Generated` and `BrutalUi.Generators.Tests`. Each XML name of a marked test type is unique in `BrutalUi.Markup.Tests`: `Gauge`, `Readout`, `Lamp`, `Meter`, `Dial`.

### Phase 1: A Marked Gauge
**Goal:** `BrutalUi.Generators` exists and builds for `netstandard2.0`. `[TemplateElement]` exists in Markup with `Name` and `IsLeaf`. The generator reads a marked class with a constructor with no parameters, walks its properties, writes a set row for each property of the seven types that the existing reads cover (`string`, `float`, `bool`, `float2`, `float4`, `Rgba8`, `TextureHandle`), and writes the register call. `BrutalUi.Markup.Tests` uses the generator as an analyzer.
**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*GeneratedElementTests'` and reads a total of 6 with `failed: 0`. In `AMarkedGaugeLoadsWithItsLevel`, the test marks a `Gauge` type, calls `loader.RegisterBrutalUiMarkupTestsElements()`, loads `<ui:Gauge level="0.5"/>`, and reads `Level` 0.5.
**Needs:** none
**Agent tier:** ADVANCED (opus). This phase sets the project, the pipeline, and the model shape.

**Tasks (group 1, the project, 6 files):**
- [ ] 1.1 Create `docs/features/markup/element-generator/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `ed767cc` at planning time), the solution total with the skipped count (3092 with 3 skipped at planning time), and the totals of `BrutalUi.Markup.Tests` and `BrutalUi.Core.Tests`. Run `dotnet pack brutal-ui.sln --no-build --configuration Debug` after the build, and record the package list of `artifacts/packages/`. Record the new build-time package (`Microsoft.CodeAnalysis.CSharp` 4.14.0, managed, generator project only, no native dependency), as requirements line 19 asks. Record The Rules, The Diagnostics, and the Notes of this plan. Record the tool notes: `DOTNET_CLI_HOME`, the per-class binary, `dotnet build-server shutdown`, and the `error CS` check of a plant.
- [ ] 1.2 Edit `Directory.Packages.props`. Add `<PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />` after the BRUTAL pins, with a comment: the generator project uses it at build time only (A2).
- [ ] 1.3 Write `src/BrutalUi.Generators/BrutalUi.Generators.csproj` as The Project Files shows, without the phase 4 item group. Do not add the project to the solution yet (phase 4 adds it with the test project). Run `dotnet build src/BrutalUi.Generators/BrutalUi.Generators.csproj -nodeReuse:false 2>&1 | tail -n 30`, and expect `0 Warning(s)`.
- [ ] 1.4 Write `src/BrutalUi.Generators/Polyfills.cs` with `internal static class IsExternalInit` in the namespace `System.Runtime.CompilerServices`, and one comment line that names the reason (`netstandard2.0` does not hold the type).
- [ ] 1.5 Write `src/BrutalUi.Generators/Models.cs`: `readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>` over `ImmutableArray<T>` (element-wise `Equals`, a hand-made hash, `Length`, the indexer, an empty default), `enum ReadKind` (`Text`, `Number`, `Bool`, `Numbers2`, `Numbers4`, `Color`, `Texture`), `sealed record SetRowModel(string RowName, string PropertyName, ReadKind Kind)`, and `sealed record ElementModel(string XmlName, bool IsLeaf, string TargetType, EquatableArray<SetRowModel> SetRows)`. The doc comment of the file states the cache rule: a model holds no `ISymbol`, `SyntaxNode`, `Location`, or `Compilation`.
- [ ] 1.6 Write `src/BrutalUi.Generators/NameRules.cs`: `internal static class NameRules` with `ToKebabCase(string)` by The kebab rule and `ToIdentifier(string?)` by The register call (a `null` or empty name gives `Assembly`). Build the project, and expect `0 Warning(s)`.

**Tasks (group 2, the generator and the demo, 6 files):**
- [ ] 1.7 Write `src/BrutalUi.Markup/Template/TemplateElementAttribute.cs` with the constructor, `Name`, and `IsLeaf` of the Public API (no `Target` yet). The constructor throws `ArgumentNullException` for a `null` name. The doc states the game form, the register call, and the rule that the attribute does nothing at run time.
- [ ] 1.8 Write `src/BrutalUi.Generators/TemplateElementGenerator.cs`: `[Generator(LanguageNames.CSharp)] public sealed class TemplateElementGenerator : IIncrementalGenerator` with the pipeline of The Generator Pipeline (with the three tracking names) and the transform `ReadElement`. The transform reads the name and `IsLeaf` from `ctx.Attributes[0]`, reads the class symbol, and runs The property walk with the seven read kinds of this phase. It matches `float2` and `float4` by metadata name, and it confirms the two names against `Brutal.Numerics` with one look at the assembly (record the names in `context.md`). The transform checks `ct.ThrowIfCancellationRequested()` in the walk. In this phase the transform returns `null` for a class that does not derive from `Element`, and the output skips it.
- [ ] 1.9 Write `src/BrutalUi.Generators/ElementSourceWriter.cs`: `internal static class ElementSourceWriter` with `Write(string assemblyId, IReadOnlyList<ElementModel> models)`, which returns the text of The Generated Source. Use a `StringBuilder` with `"\n"`. Name the setters `Set0`, `Set1`, and on in output order. The output adds no source for an empty list. The hint name is `TemplateElements.g.cs`.
- [ ] 1.10 Edit `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`. Add `<ProjectReference Include="../../src/BrutalUi.Generators/BrutalUi.Generators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`. Write `tests/BrutalUi.Markup.Tests/Generated/GeneratedElementTests.cs` with three top-level `internal sealed` types and 6 tests. The types: `[TemplateElement("Gauge")] Gauge : Element` with `float Level`. `[TemplateElement("Readout")] Readout : Element` with `string? Caption`, `bool Lit`, `float2 Offset`, `float4 Rect`, `Rgba8 Color`, `TextureHandle Icon`, an `Action<Readout>? Changed` property, and a get-only `int Count`. `[TemplateElement("Lamp", IsLeaf = true)] Lamp : Element`. The tests: `AMarkedGaugeLoadsWithItsLevel` (the demo of Q5). `TheGaugeTakesTheBaseRowsAndEqualsTheBuilderTree` (`<ui:Gauge name="g" class="hud" level="0.5" />` against `new Gauge { Level = 0.5f }.Name("g").Class("hud")` under `TreeCompare`). `ABadLevelIsAnInvalidValueAtTheAttribute` (`level="x"`: one `InvalidValue` at `level`, `Root` `null`). `EachReadTypeGivesARow` (a `Readout` with `caption`, `lit`, `offset`, `rect`, `color`, and `icon` from a texture map reads each value). `APropertyWithNoRowIsAnUnknownAttribute` (`changed`, `count`, and `states` each give `UnknownAttribute`, and the message lists `name, class, pickable, focusable, caption, lit, offset, rect, color, icon`). `ALeafTypeRejectsAChild` (`UnexpectedChild` at the child name).
- [ ] 1.11 Run the build and the tests, and expect Markup +6. Read the generated source once with `-p:EmitCompilerGeneratedFiles=true`, and check it against The Generated Source. Run three plants. Plant 1: the writer omits the assignment line of a setter. Expect `AMarkedGaugeLoadsWithItsLevel` to fail. Plant 2: the walk does not stop before `Element`. Expect each `GeneratedElementTests` test to fail (the register call throws for the `name` row). Plant 3: the writer always writes `IsLeaf = false`. Expect `ALeafTypeRejectsAChild` to fail. Record the counts, the plants, and the demo state in `context.md`.

**Estimated Steps:** about 100 (11 tasks, about nine steps for one task)

**Files (group 1, 6):** `context.md`, `Directory.Packages.props`, `BrutalUi.Generators.csproj`, `Polyfills.cs`, `Models.cs`, `NameRules.cs`
**Files (group 2, 6):** `TemplateElementAttribute.cs`, `TemplateElementGenerator.cs`, `ElementSourceWriter.cs`, `BrutalUi.Markup.Tests.csproj`, `GeneratedElementTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 new Markup tests pass, and each existing test passes with no edit.
- [ ] Plants 1, 2, and 3 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] `src/BrutalUi.Generators/bin/Debug/netstandard2.0/BrutalUi.Generators.dll` exists, and no `bin/Debug/net10.0` folder exists in the generator project.

### Phase 2: The Package
**Goal:** Markup uses the generator as an analyzer, and `dotnet pack` puts it in `analyzers/dotnet/cs` of the Markup package. A console project outside the solution references the packed package from `artifacts/packages` and gets the generated rows. Both test scripts pack and run it (Q4).
**Demo:** the developer runs `dotnet pack brutal-ui.sln --no-build --configuration Debug`, then `dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj`, and reads `package-test: PASS Gauge level 0.5`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus). This phase touches the pack layout, NuGet restore, and both scripts.

**Tasks (group 1, the pack and the project, 5 files):**
- [ ] 2.1 Edit `src/BrutalUi.Markup/BrutalUi.Markup.csproj` as The Project Files shows: the analyzer `ProjectReference` with `PrivateAssets="all"`, and the `None` pack item. Build, pack, and check the layout: `unzip -l artifacts/packages/BrutalUi.Markup.0.1.0-alpha.nupkg` lists `analyzers/dotnet/cs/BrutalUi.Generators.dll` and `lib/net10.0/BrutalUi.Markup.dll`, and no other `Generators` entry. `unzip -p` of the `.nuspec` holds no `Generators`. The package list equals the list of task 1.1. Record the output in `context.md`.
- [ ] 2.2 Write `tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj` as The Project Files shows. Do not add it to the solution.
- [ ] 2.3 Write `tests/BrutalUi.PackageTest/nuget.config` as The Project Files shows, with one comment line for each source and for the mapping.
- [ ] 2.4 Write `tests/BrutalUi.PackageTest/Program.cs` as The Project Files shows: top-level statements, then the `Gauge` type.
- [ ] 2.5 Run the three steps of The Script Steps by hand, and expect the PASS line. Run two plants on the package path (here a plant expects a failed step, not a failed test). Plant 4: remove the `None` pack item, pack, and run. Expect a non-zero exit code with `error CS1061` for `RegisterBrutalUiPackageTestElements`. Plant 5: remove `PrivateAssets="all"` from the analyzer reference, pack, and restore. Expect a non-zero exit code with `NU1101` for `BrutalUi.Generators`. Restore each file, pack, and run again. Record the outputs in `context.md`.

**Tasks (group 2, the scripts and the docs, 4 files):**
- [ ] 2.6 Edit `scripts/test-linux.sh`. Add the steps of The Script Steps after `run_step test`: `run_step pack ...`, `rm -rf -- artifacts/package-test`, `run_step package-restore ...`, and `run_step package ...`. Update the header: the step list, the purpose, and the PASS line.
- [ ] 2.7 Edit `scripts/test-windows.ps1`. Add the same steps after `Invoke-Step test`, with `Remove-Item -LiteralPath artifacts\package-test -Recurse -Force` inside a `Test-Path` check. Update the header: the step list and the PASS line. Keep the cmd.exe rules of `Invoke-Step`.
- [ ] 2.8 Edit `docs/technical-design.md`. In section 3, add `BrutalUi.Generators/` to the tree with the comment `# source generator of the element table (analyzer, netstandard2.0)`, add `BrutalUi.Generators.Tests/` and `BrutalUi.PackageTest/` (outside the solution) under `tests/`, and add a **`src/BrutalUi.Generators/`** paragraph: it targets `netstandard2.0`, ships inside the Markup package in `analyzers/dotnet/cs`, is not a package, and adds no run-time reference. In section 14, add one bullet for the package test. Run `scripts/test-linux.sh`, and expect the PASS line with `pack` and `package`. Record the line in `context.md`.

**Estimated Steps:** about 70 (8 tasks, about nine steps for one task)

**Files (group 1, 5):** `BrutalUi.Markup.csproj`, `BrutalUi.PackageTest.csproj`, `nuget.config`, `Program.cs`, `context.md`
**Files (group 2, 4):** `test-linux.sh`, `test-windows.ps1`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The package test prints `package-test: PASS Gauge level 0.5` and exits with 0.
- [ ] `scripts/test-linux.sh` exits with 0 and prints a PASS line that names `pack` and `package`.
- [ ] Plants 4 and 5 each give a non-zero exit code at the expected step, and each restored file equals its copy under `cmp`.
- [ ] `grep -c BrutalUi.PackageTest brutal-ui.sln` gives 0.

### Phase 3: The Authoring Surface
**Goal:** A game type can name a static factory and its create rows (Q3), use `int` and enum properties (A4), rename a row, and skip a property (Q2). A static declaration class with `Target` gives the rows of a type that it does not own. `AttributeContext` gets the two reads that the new rows need.
**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*GeneratedAuthoringTests'` and reads a total of 6 with `failed: 0`. A `Meter` with a factory, a required `segments` create row, an `int`, an enum keyword, a renamed row, and an ignored property loads from one template.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the Markup side, 6 files):**
- [ ] 3.1 Edit `src/BrutalUi.Markup/Template/TemplateElementAttribute.cs`. Add `public Type? Target { get; set; }` with a doc that states the declaration form: the marked class is static, and it holds the factory of the target type.
- [ ] 3.2 Write `src/BrutalUi.Markup/Template/TemplateMemberAttributes.cs` with the four attributes of the Public API: `TemplateFactoryAttribute`, `TemplateCreateRowAttribute`, `TemplateRowNameAttribute`, and `TemplateIgnoreAttribute`. Each constructor with a name throws `ArgumentNullException` for `null`. Each doc names its rule of The Rules.
- [ ] 3.3 Edit `src/BrutalUi.Markup/Template/TemplateValues.cs` and `AttributeContext.cs`. Add `internal static bool TryParseInteger(ReadOnlySpan<char>, out int)`: an optional `+` or `-`, then ASCII digits, in the `int` range. Add `TryReadInteger(out int value)` with the message `'{Value}' is not an integer. Write digits with an optional sign, for example '-3'.` Add `TryReadKeyword(ReadOnlySpan<string> keywords, out int index)`: it trims the value, compares each keyword ordinal, and on a fault adds `InvalidValue` with the message `'{Value}' is not a value of {Name}. Write 'a', 'b', or 'c'.` (the keywords in order). Each read allocates nothing on a success. Change no existing member.
- [ ] 3.4 Write `tests/BrutalUi.Markup.Tests/Generated/TemplateReadMethodTests.cs` with 4 tests. Each test registers a probe type by hand with `Register` and a set row that calls the new read. `AnIntegerTakesASignAndDigits` (`0`, `-3`, `+3`, and ` 42 ` read). `AnIntegerFaultIsAnInvalidValue` (`1.5`, `x`, `-`, empty, `1e3`, and `2147483648` each give `InvalidValue` at the attribute with the quoted value). `AKeywordGivesItsIndex` (three keywords give 0, 1, 2, and the value with whitespace at both ends reads). `AnUnknownKeywordListsTheKeywords` (`InvalidValue` at the attribute, and the message holds the quoted value and each keyword). Run the build and the tests, and expect Markup +4.

**Tasks (group 2, the generator side, 6 files):**
- [ ] 3.5 Edit `src/BrutalUi.Generators/Models.cs`. Add `sealed record CreateRowModel(string Name, bool IsRequired)`, `sealed record KeywordModel(string Keyword, string MemberName)`, the read kinds `Integer` and `Keyword`, `EquatableArray<KeywordModel> Keywords` and `string EnumType` on `SetRowModel`, and `string? FactoryCall` and `EquatableArray<CreateRowModel> CreateRows` on `ElementModel`.
- [ ] 3.6 Edit `src/BrutalUi.Generators/TemplateElementGenerator.cs`. Read `Target`: with a value, walk the target type and look for the factory on the marked class. Find the `[TemplateFactory]` method, and read its `[TemplateCreateRow]` attributes in source order. Add the `int` and enum read kinds. For an enum, read each constant field in declaration order and give it a kebab keyword. Apply `[TemplateIgnore]` and `[TemplateRowName]`. Match each attribute by its full metadata name.
- [ ] 3.7 Edit `src/BrutalUi.Generators/ElementSourceWriter.cs`. A factory model writes `Create` with the global factory call. The create rows come before the set rows, with `AttributeRowKind.Create` or `AttributeRowKind.CreateRequired`. An `int` row calls `TryReadInteger`. An enum row writes its keyword array and a `switch` on the index.
- [ ] 3.8 Write `tests/BrutalUi.Markup.Tests/Generated/GeneratedAuthoringTests.cs` with 6 tests and these types: `enum MeterStyle { Bar, RoundDial }`. `[TemplateElement("Meter")] Meter : Element` with a constructor that takes the segment count, a `[TemplateFactory]` method with `[TemplateCreateRow("segments", IsRequired = true)]` that reads `segments` with `TryReadWholeNumber`, `int Offset`, `MeterStyle Style`, `[TemplateRowName("lvl")] float Level`, and `[TemplateIgnore] float Hidden`. `Dial : Element` with no marker and `float Angle`, and `[TemplateElement("Dial", Target = typeof(Dial))] internal static class DialDeclaration` with no factory. The tests: `AFactoryTypeLoadsWithItsCreateRows` (`<ui:Meter lvl="0.5" segments="4" />` gives 4 segments and the level, with the set attribute first in the source). `AMissingRequiredCreateRowIsAnError` (`MissingAttribute` at the element name, and the message holds `'segments'`). `AnIntRowTakesZeroAndANegativeNumber`. `AnEnumRowTakesAKebabKeyword` (`round-dial` gives `RoundDial`, and `RoundDial` gives `InvalidValue`). `ARenamedRowAndAnIgnoredPropertyFollowTheirAttributes` (`level` and `hidden` each give `UnknownAttribute`). `ADeclarationClassGivesTheRowsOfItsTarget` (`<ui:Dial angle="90" />` gives a `Dial` with the angle).
- [ ] 3.9 Edit `docs/technical-design.md` section 11. Add **Generated element types** after **Custom element types**: the game form with the `Gauge` example, the register call, The Rules (both forms, the factory, the constructor, the walk, the supported types table, the kebab rule, the row order), and the line that the manual `Register` call stays public.
- [ ] 3.10 Run the build and the tests, and expect Markup +6. Run three plants. Plant 6: the writer ignores `IsRequired`. Expect `AMissingRequiredCreateRowIsAnError` to fail. Plant 7: an enum keyword is the member name in lower case with no hyphen. Expect `AnEnumRowTakesAKebabKeyword` to fail. Plant 8: `TryParseInteger` rejects a sign. Expect `AnIntegerTakesASignAndDigits` to fail. Record the counts and the plants in `context.md`.

**Estimated Steps:** about 85 (10 tasks, about eight steps for one task)

**Files (group 1, 6):** `TemplateElementAttribute.cs`, `TemplateMemberAttributes.cs`, `AttributeContext.cs`, `TemplateValues.cs`, `TemplateReadMethodTests.cs`, `context.md`
**Files (group 2, 6):** `Models.cs`, `TemplateElementGenerator.cs`, `ElementSourceWriter.cs`, `GeneratedAuthoringTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 new Markup tests pass, and each existing test passes with no edit.
- [ ] Plants 6, 7, and 8 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] `git diff <base> -- src/BrutalUi.Markup/Template/AttributeContext.cs | grep -E '^-[^-]'` gives no output (additions only).

### Phase 4: The Diagnostics
**Goal:** The generator reports the six diagnostics of The Diagnostics, and a type with an error gets no code. A new test project compiles sample source in memory and checks each diagnostic, the golden output of a sample type, the name rules, and the incremental cache (A8).
**Demo:** the developer runs `BrutalUi.Generators.Tests --filter-class '*DiagnosticTests'` and reads a total of 8 with `failed: 0`. Each of the six ids fires on its sample at the expected line and column.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the checks, 6 files):**
- [ ] 4.1 Write `src/BrutalUi.Generators/Diagnostics.cs`: the six `DiagnosticDescriptor` values of The Diagnostics, `sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)` with `ToLocation()` and `From(Location?)`, `sealed record DiagnosticInfo(string Id, LocationInfo? Location, EquatableArray<string> Arguments)` with `ToDiagnostic()`, and `sealed record ElementResult(ElementModel? Model, EquatableArray<DiagnosticInfo> Diagnostics, LocationInfo? NameLocation, string TypeName)`.
- [ ] 4.2 Write `src/BrutalUi.Generators/AnalyzerReleases.Shipped.md` (the header line only) and `AnalyzerReleases.Unshipped.md` (a `### New Rules` table with the six ids, the category, the severity, and the notes). Add the phase 4 item group of The Project Files to `BrutalUi.Generators.csproj`. Check whether `Microsoft.CodeAnalysis.Analyzers` arrives through `Microsoft.CodeAnalysis.CSharp` (`dotnet list src/BrutalUi.Generators package --include-transitive`), and record the result in `context.md` (Risk 3).
- [ ] 4.3 Edit `src/BrutalUi.Generators/TemplateElementGenerator.cs`. The transform returns an `ElementResult`. It runs the checks of `BRUTALUI0001`, `0003`, `0004`, `0005`, and `0006` by The Rules, adds a `DiagnosticInfo` for each fault, and keeps `Model` `null` when the type has an error. Keep the file under 400 lines. When it grows past that, move the check helpers to `Diagnostics.cs`.
- [ ] 4.4 Edit the output step in the same file. Report each `DiagnosticInfo`. Group the valid models by XML name with an ordinal compare, report `BRUTALUI0002` on each model of a group with more than one, and write no code for them. Build. Add a scratch type with no usable constructor to `BrutalUi.Markup.Tests`, build, check that the build stops with `BRUTALUI0001` at the class name and with no other error, then delete the scratch type. Record the output in `context.md`.

**Tasks (group 2, the test project, 6 files):**
- [ ] 4.5 Edit `brutal-ui.sln` with `dotnet sln brutal-ui.sln add src/BrutalUi.Generators/BrutalUi.Generators.csproj --solution-folder src` and `dotnet sln brutal-ui.sln add tests/BrutalUi.Generators.Tests/BrutalUi.Generators.Tests.csproj --solution-folder tests` after task 4.6.
- [ ] 4.6 Write `tests/BrutalUi.Generators.Tests/BrutalUi.Generators.Tests.csproj`: `OutputType` `Exe`, `PackageReference` `xunit.v3` and `Microsoft.CodeAnalysis.CSharp`, a normal `ProjectReference` to `BrutalUi.Generators` and to `BrutalUi.Markup`. Add no testing package of Roslyn (Decision 13).
- [ ] 4.7 Write `tests/BrutalUi.Generators.Tests/GeneratorHarness.cs`: `Run(string source, string assemblyName = "Sample")` makes a `CSharpCompilation` with `LanguageVersion.CSharp13` and the references of `TRUSTED_PLATFORM_ASSEMBLIES`, runs a `CSharpGeneratorDriver` with `trackIncrementalGeneratorSteps: true`, and gives the generated text, the generator diagnostics, the errors and warnings of the output compilation, and the driver. Add a helper that gives the line and the column (from 1) of a diagnostic.
- [ ] 4.8 Write `tests/BrutalUi.Generators.Tests/DiagnosticTests.cs` with 8 tests. Each asserts the id, `DiagnosticSeverity.Error`, the line and the column, and the quoted text. `ATypeWithNoConstructorAndNoFactoryGivesBRUTALUI0001` (a constructor with a parameter, and an abstract class, each give it. The same class with a factory gives none, Q3). `TwoTypesWithOneXmlNameGiveBRUTALUI0002` (one diagnostic on each attribute, and no code for either). `ARowNameOnAPropertyWithNoRowGivesBRUTALUI0003` (an `Action` property and a get-only property). `ABadFactoryGivesBRUTALUI0004` (an instance method, a private method, two parameters, a `string` return, two factories, and `[TemplateCreateRow]` with no factory). `ABadRowNameGivesBRUTALUI0005` (two `[TemplateRowName("a")]`, `[TemplateRowName("class")]`, and `[TemplateRowName("a b")]`). `ABadElementTypeGivesBRUTALUI0006` (a class that does not derive from `Element`, a generic class, a private nested class, `Target` on a class that is not static, and the XML name `"1x"`). `ATypeWithAnErrorGetsNoCodeAndTheOtherTypesStay` (one bad type and one good type: the output holds the good type only, and the output compilation has no error). `AValidSampleGivesNoDiagnostic` (a sample with each supported type, a factory, a declaration class, a renamed row, an ignored property, and an `Action` property gives no diagnostic, A5).
- [ ] 4.9 Write `tests/BrutalUi.Generators.Tests/GeneratorOutputTests.cs` with 8 tests. `TheGaugeSampleGivesTheExpectedSource` (the full text equals a raw string literal). `TheKebabRuleSplitsWordsAndAcronyms` (a table loop over the examples of The kebab rule). `TheRegisterNameComesFromTheAssemblyName` (`My-Game.UI` gives `MyGameUITemplateElements` and `RegisterMyGameUIElements`, and `3d.Ui` gives `_3dUi`). `TheTypesRegisterInTheOrderOfTheirXmlNames`. `TheCreateRowsComeBeforeTheSetRows`. `AnAssemblyWithNoMarkedTypeGetsNoSource`. `AnUnrelatedEditKeepsEachStepCached` (run, add an unrelated syntax tree, run again: each output of `TemplateElementModels` and `TemplateElementSet` is `Cached` or `Unchanged`, and the source output is `Cached`). `AnEditOfAMarkedTypeRunsTheTransformAgain` (rename a property: `TemplateElementModels` gives `Modified`, and the new row name is in the text).
- [ ] 4.10 Run the build and the tests, and expect Generators +16. Run two plants. Plant 9: the output step does not check duplicate names. Expect `TwoTypesWithOneXmlNameGiveBRUTALUI0002` to fail. Plant 10: `ElementModel.SetRows` is an `ImmutableArray<SetRowModel>`. Expect `AnUnrelatedEditKeepsEachStepCached` to fail. Record the counts and the plants in `context.md`.

**Estimated Steps:** about 90 (10 tasks, about nine steps for one task)

**Files (group 1, 6):** `Diagnostics.cs`, `AnalyzerReleases.Shipped.md`, `AnalyzerReleases.Unshipped.md`, `BrutalUi.Generators.csproj`, `TemplateElementGenerator.cs`, `context.md`
**Files (group 2, 6):** `brutal-ui.sln`, `BrutalUi.Generators.Tests.csproj`, `GeneratorHarness.cs`, `DiagnosticTests.cs`, `GeneratorOutputTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 16 new Generators tests pass, and each existing test passes with no edit.
- [ ] Plants 9 and 10 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] `dotnet sln brutal-ui.sln list` names `BrutalUi.Generators` and `BrutalUi.Generators.Tests`, and not `BrutalUi.PackageTest`.

### Phase 5: The Built-In Types
**Goal:** Markup declares each of the eight Core types with `[TemplateElement(..., Target = typeof(...))]`, the leaf flag, and the written factory with its create rows (Q1). The generator writes the set rows and the register call. The written set rows and `RegisterAll` go away, the base rows move to `BaseRows.cs` (A6), and the factories keep each check, the `capacity` ceiling included (A7, CR-1).
**Demo:** the developer runs the solution tests, and each xml-element-loader test passes unchanged while `BuiltInElements.cs` holds no `Register` call. `BrutalUi.Markup.Tests --filter-class '*BuiltInRowListTests'` reads a total of 8 with `failed: 0`.
**Needs:** Phase 3, Phase 4
**Agent tier:** ADVANCED (opus). This phase replaces the table that the loader and 59 tests read.

**Tasks (group 1, the replacement, 5 files):**
- [ ] 5.1 Write `tests/BrutalUi.Markup.Tests/Generated/BuiltInRowListTests.cs` with 8 tests, one for each type: `TheRowsOfElementStayTheSame`, `...Label...`, `...Button...`, `...Toggle...`, `...Slider...`, `...TextField...`, `...ScrollView...`, `...Image...`. Each test loads the type with the attribute `probe` and asserts that the `UnknownAttribute` message holds `The attributes of <Type> are <list>.` with the exact list of The Written Table of `technical-design.md` (for example `name, class, pickable, focusable, texture, uv, tint, scale-mode` for `Image`). Each test also loads the type with one `<ui:Element />` child: `Label` and `Image` give `UnexpectedChild`, and the other six load. Give each type its required attributes. Run the tests against the written table first, and expect +8 with `failed: 0`. Record the result in `context.md`.
- [ ] 5.2 Write `src/BrutalUi.Markup/Template/BaseRows.cs`: `internal static class BaseRows` with `internal static readonly AttributeRow[] All` and the setters of the four base rows, moved from `BuiltInElements.cs` with no change of behavior. Edit `TemplateLoader.cs`: replace each `BuiltInElements.BaseRows` with `BaseRows.All`.
- [ ] 5.3 Edit `src/BrutalUi.Markup/Template/BuiltInElements.cs`. Keep `internal static class BuiltInElements` with one nested `internal static class <Type>Declaration` for each of the eight types, each with `[TemplateElement("<Type>", Target = typeof(<Type>))]` (`IsLeaf = true` for `Label` and `Image`) and one `[TemplateFactory] internal static Element? Create(ElementCreateArgs args)` with its `[TemplateCreateRow]` attributes in the order of The Written Table. `Element` gets the factory `new Element()`, `Toggle` gets `new Toggle(default)`, and `Image` gets `new Image(new Image.CreateInfo { Texture = default })` (Q1). Move the bodies of `CreateSlider`, `CreateTextField`, `CreateScrollView`, `CreateLabel`, and `CreateButton` into the declarations with no change. Keep `TryReadText` as a private helper of the outer class. Keep `TemplateValues.CheckCapacity` in the `TextField` factory. Remove `RegisterAll` and the seven set-row setters. Update the class remarks: the file holds the declarations and the factories, and the generator writes the set rows and the register call.
- [ ] 5.4 Edit `src/BrutalUi.Markup/Template/TemplateLoader.cs`. The constructor calls `this.RegisterBrutalUiMarkupElements();` in place of `BuiltInElements.RegisterAll(this);`. Update the constructor docs. Build with `-p:EmitCompilerGeneratedFiles=true`, read the generated file of Markup, and check each row name, kind, order, and leaf flag against The Written Table. Run the build and the tests: each existing test passes, and Markup stays +24 from the baseline. Run three plants. Plant 11: the `TextField` factory drops `CheckCapacity`. Expect `ACapacityIsAWholeNumberAboveZero` to fail. Plant 12: the `Image` declaration drops `IsLeaf = true`. Expect `AChildInsideALabelOrAnImageIsAnError` and `TheRowsOfImageStayTheSame` to fail. Plant 13: the generator sorts the set rows by name. Expect `TheRowsOfImageStayTheSame` to fail. Record the counts, the generated file check, and the plants in `context.md`.

**Tasks (group 2, the docs and the close, 3 files):**
- [ ] 5.5 Edit `docs/technical-design.md` section 11. Change the sentence of line 1194: the generator writes the rows of the built-in types and of each marked game type, with no reflection. Rename **The written table** to **The element table**, and state that `BuiltInElements.cs` declares each type with its factory and its create rows, and that the generator writes the set rows from the properties. Add **The diagnostics** with the table of The Diagnostics. Keep the row tables, because the rows do not change.
- [ ] 5.6 Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`, add one line tagged `(markup/element-generator)`: the source generator `BrutalUi.Generators` inside the Markup package, `[TemplateElement]` and its four member attributes, the generated register call, `TryReadInteger` and `TryReadKeyword`, the six diagnostics, the generated rows of the built-in types, and the package test in both scripts.
- [ ] 5.7 Run the build, the solution tests (expect 3132 with 3 skipped from the baseline of `context.md`), and `scripts/test-linux.sh`. Run the greps of the Verification Method of the Definition of Done. Record the counts, the PASS line, and the grep results in `context.md`. Add the Windows run of `scripts/test-windows.ps1` to the Needs Human Verification list of `context.md`.

**Estimated Steps:** about 60 (7 tasks, about nine steps for one task)

**Files (group 1, 5):** `BuiltInRowListTests.cs`, `BaseRows.cs`, `BuiltInElements.cs`, `TemplateLoader.cs`, `context.md`
**Files (group 2, 3):** `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 new Markup tests pass before and after the replacement, and each existing test passes with no edit.
- [ ] Plants 11, 12, and 13 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] `grep -c 'Register(' src/BrutalUi.Markup/Template/BuiltInElements.cs` gives 0, and `grep -c 'TemplateElement(' src/BrutalUi.Markup/Template/BuiltInElements.cs` gives 8.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Built-ins: Markup declares each Core type with the XML name, the leaf flag, and a written factory with its create rows. The generator writes the set rows and the register call.
- Q2 Rows: each public settable property of a supported type becomes a row with a kebab-case name. One attribute skips a property, and one attribute gives another name.
- Q3 Factory: a game type can name a static factory method and its create rows. The constructor diagnostic fires only when the type has no such constructor and no factory.
- Q4 Pack test: the scripts pack to `artifacts/packages`, then build and run one project outside the solution that references the package from that folder.
- Q5 Demo: a test marks a `Gauge` type, calls the one generated register call, loads `<ui:Gauge level="0.5"/>`, and reads `Level` 0.5.
- A1 The marker attributes are public types in `BrutalUi.Markup`. The generator finds them by full name.
- A2 `BrutalUi.Generators` targets `netstandard2.0`, with `Microsoft.CodeAnalysis.CSharp` pinned at 4.14.0. The compiler of SDK 10.0.115 loads that version.
- A3 The generated register call is an explicit extension method on `TemplateLoader`, one for each assembly. No module initializer registers a type.
- A4 The supported property types are `string`, `float`, `int`, `bool`, an enum, `float2`, `float4`, `Rgba8`, and `TextureHandle`. An enum value reads as a kebab-case keyword.
- A5 A property of another type gives no row and no diagnostic. A name attribute on such a property gives an error diagnostic.
- A6 The four base rows (`name`, `class`, `pickable`, `focusable`) stay written code in Markup. The generator writes no base row.
- A7 The written factories and their checks stay in Markup, the `capacity` ceiling included. `BuiltInElements.cs` keeps only the declarations and the factories.
- A8 The generator tests live in a new project, `tests/BrutalUi.Generators.Tests`, and compile sample source in memory.
- A9 The leaf flag is a property of the marker attribute. The default is `false`.
- A10 The feature changes no sample. markup-sample adds the custom type to HelloPanel.

### Decision 1: One class attribute and one pipeline
**Choice:** `[TemplateElement]` always sits on a class. On a game element class it marks the type itself. On a static declaration class with `Target` it marks a type of another assembly. One `ForAttributeWithMetadataName` pipeline reads both forms.
**Rationale:** This is the Chosen Approach. `ForAttributeWithMetadataName` gives the best incremental cache of the Roslyn API. The built-in types run the same generator path as a game type, so the built-in tests prove the game path too.
**Alternatives:** Assembly-level attributes for the Core types (a second pipeline and a weaker cache). A partial method on a table class (two code paths).

### Decision 2: The factory and its create rows are attributes on the method
**Choice:** `[TemplateFactory]` marks the factory method. Each `[TemplateCreateRow("name", IsRequired = ...)]` on the same method gives one create row, in source order.
**Rationale:** Q3 lets a game type name a factory and its create rows. An attribute on the method names it with no string method name, and the compiler checks the name. Source order keeps the row order of The Written Table (`min`, `max`, `step`). `IsRequired` maps to `AttributeRowKind.CreateRequired`.
**Alternatives:** A `Factory = "Create"` string on the class attribute (no compiler check, a rename breaks it). One array of names with a second array of required names (the order of a mixed list gets lost).

### Decision 3: The register call is an internal extension method with the assembly id in its name
**Choice:** `internal static class <Id>TemplateElements` in the namespace `BrutalUi.Markup`, with `internal static void Register<Id>Elements(this TemplateLoader loader)`.
**Rationale:** A3 asks for an explicit extension method for each assembly. `internal` keeps the generated class out of the public API of Markup and of each game. The namespace `BrutalUi.Markup` makes the method visible where `TemplateLoader` is visible. The id in the method name stops CS0121 in a project that sees the internals of another generated assembly, as `BrutalUi.Markup.Tests` does.
**Alternatives:** One name `RegisterTemplateElements` (ambiguous under `InternalsVisibleTo`). A module initializer (A3 rejects it, and it hides the order of the registry).

### Decision 4: The walk stops before `Element`
**Choice:** The walk visits the target type and its base types, and never the members of `BrutalUi.Core.Element`.
**Rationale:** A6 keeps the four base rows written. `Element.Name`, `IsPickable`, and `IsFocusable` would give `name`, `is-pickable`, and `is-focusable`, and `Register` rejects a row with a base name. `Element.States` is a public settable enum property, and a `states` row would change each `UnknownAttribute` message that the tests read.

### Decision 5: Two new public reads on `AttributeContext`
**Choice:** `TryReadInteger(out int)` and `TryReadKeyword(ReadOnlySpan<string>, out int)`. The generated code calls only public members of Markup.
**Rationale:** Generated code in a game assembly cannot call the internal `TemplateValues`. `TryReadWholeNumber` reads 1 or more only, so an `int` property could not read 0 or a negative number (Notes item 4). An enum needs a trim and an ordinal compare with an error that lists the keywords. Both reads follow the shape of the existing `TryRead*` methods.
**Alternatives:** Map `int` to `TryReadWholeNumber` (no new member, but 0 is an error). Generate the trim and the compare in each assembly (each game gets a copy of the whitespace rule).

### Decision 6: Value-equatable models with `EquatableArray<T>` and `LocationInfo`
**Choice:** The transform gives `record` models with strings, flags, and `EquatableArray<T>`. A diagnostic travels as a `DiagnosticInfo` with a `LocationInfo`, and the output step makes the `Location`.
**Rationale:** The incremental cache compares each step output with `Equals`. An `ImmutableArray<T>` compares by reference, and an `ISymbol` or a `Location` holds the old compilation in memory. Plant 10 proves the rule.

### Decision 7: The generator project settings
**Choice:** `netstandard2.0`, `IsPackable` `false`, `GenerateDocumentationFile` `false`, `IsRoslynComponent` `true`, `EnforceExtendedAnalyzerRules` `true`, and `LangVersion` 14 and `Nullable` from the root. `Polyfills.cs` adds `IsExternalInit`.
**Rationale:** A2 sets the target, because the Roslyn host loads `netstandard2.0` analyzers. The project is not a package (requirements line 12), and it has no public API to document. `EnforceExtendedAnalyzerRules` turns on the RS rules that catch a banned API in a compiler extension. `LangVersion` is a compiler setting, so the root value works for `netstandard2.0`.

### Decision 8: The pack puts the generator in the Markup package
**Choice:** An analyzer `ProjectReference` with `ReferenceOutputAssembly="false"` and `PrivateAssets="all"`, and a `None` item with `PackagePath="analyzers/dotnet/cs"`.
**Rationale:** Epic A4 ships the generator inside the Markup package. The `ProjectReference` builds the generator before Markup and runs it on the declarations of Markup. `PrivateAssets="all"` keeps a `BrutalUi.Generators` dependency out of the `.nuspec`, and plant 5 proves it. The pack path uses `$(Configuration)`, so the scripts pack Debug with `--no-build`.

### Decision 9: The package test is a console project with its own restore folder
**Choice:** `tests/BrutalUi.PackageTest/` with `VersionOverride="$(Version)"`, a private `RestorePackagesPath` under `artifacts/package-test/`, a `nuget.config` with package source mapping, and a `Program.cs` that returns 0 or 1.
**Rationale:** Q4 asks for a project outside the solution that references the package from `artifacts/packages`. Each pack writes the same version, so the global packages folder would keep the first copy. The private folder, which the scripts delete, makes each run read the new package. The mapping keeps `BrutalUi.*` on the local source and stops NU1507. A console project needs no test runner in the script.

### Decision 10: Six error diagnostics, and no code for a type with an error
**Choice:** `BRUTALUI0001` to `BRUTALUI0006`, each with the severity `Error`. The generator writes no code for a type with an error, and keeps the code of the other types.
**Rationale:** Requirements line 15 names two diagnostics, and A5 names a third. The other three stop a fault that would show as a `CS` error inside the generated file (a bad factory, a bad row name, a bad element type), or as an `ArgumentException` of `Register` at load time. The `BRUTALUI` prefix follows the `BRUTALCORE0011` style of the BRUTAL analyzers. With no code for the bad type, the build shows one clear error.

### Decision 11: The built-in declarations are nested in `BuiltInElements`, and the base rows move out
**Choice:** Eight `internal static class <Type>Declaration` types nested in `BuiltInElements`, each with a written factory. The base rows and their setters move to `BaseRows.cs`.
**Rationale:** Q1 gives each Core type a written factory, also `Element`, `Toggle`, and `Image`. A7 keeps only the declarations and the factories in `BuiltInElements.cs`. A6 keeps the base rows written, so they need another file (Notes item 3). A nested declaration reaches the private `TryReadText` helper of its outer class.

### Decision 12: The generated source is plain, deterministic C#
**Choice:** `global::` names, `"\n"` line ends, no collection expressions, no C# 14 feature, models sorted by XML name, setters named by output order, and no `GeneratedCode` attribute.
**Rationale:** A consumer can use an older language version than C# 14, and the golden test needs the same text on each run and each machine. A version string in the output would break the golden test at each version change.

### Decision 13: An own test harness, with no Roslyn testing package
**Choice:** `GeneratorHarness` builds a `CSharpCompilation` from the trusted platform assemblies and runs `CSharpGeneratorDriver` directly.
**Rationale:** The Roslyn testing packages pull an old xunit and a long dependency list. The driver API gives the generated text, the diagnostics, and the tracked steps, which the tests need. The harness adds no package beyond the pinned `Microsoft.CodeAnalysis.CSharp`.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The member order of a type from metadata (a Core type in the Markup build) differs from its declaration order. | The set rows of `Image` change order, and the `UnknownAttribute` message changes. | Task 5.1 pins each row list before the replacement. When the order differs, sort the set rows of a metadata type by `ISymbol.MetadataToken`, which follows the declaration order of the C# compiler. Record the choice in `context.md`. |
| 2 | The compiler server keeps the old analyzer after a change of the generator. | A plant passes by mistake, or a build uses old generated code. | Run `dotnet build-server shutdown` after each change of the generator (The commands, The plants). |
| 3 | An RS rule gives a warning, and `TreatWarningsAsErrors` makes it an error (RS2008 release tracking, RS1035 banned API, RS1036). | The generator project does not build. | Task 4.2 adds the release files and records whether `Microsoft.CodeAnalysis.Analyzers` arrives. The writer uses `"\n"`, not `Environment.NewLine`. Phase 1 has no descriptor, so RS2008 cannot fire before phase 4. |
| 4 | The global packages folder serves a stale `BrutalUi.Markup` of the same version. | The package test passes with an old package. | The private `RestorePackagesPath`, the delete before each run, and `restore --force` (Decision 9). |
| 5 | NU1507 or another restore warning in the package test. | The `package-restore` step fails. | The package source mapping in `nuget.config`. Task 2.5 runs the steps by hand first. |
| 6 | `dotnet pack` builds Release by default, and the pack item reads the Debug folder, or the reverse. | The generator is not in the package. | The scripts pass `--no-build --configuration Debug`. The pack item uses `$(Configuration)`. Task 2.1 and the Verification Method check the layout with `unzip -l`. |
| 7 | The generator throws on an odd input, or another analyzer flags the generated code. | CS8785 ("generator failed") becomes an error in each consumer. | The transform checks each symbol for `null` and each attribute argument for its type. `DiagnosticTests` feeds bad samples. The generated code uses the same forms as the written table, which the BRUTAL analyzers accept. |
| 8 | A test of xml-element-loader reads a text that the generated path changes. | The tests do not pass unchanged. | The plan checked that the `scale-mode` test reads the kind and the position only (`TemplateValueTests.cs` lines 141 to 147). The factories move with no change. `BuiltInRowListTests` pins each list before the change. |
| 9 | A file grows past 400 lines (`TemplateElementGenerator.cs`). | The DoD fails. | Task 4.3 moves the check helpers to `Diagnostics.cs` when the file grows. |
| 10 | The Windows script cannot run on the Linux dev machine. | The Windows path stays unproven. | Task 2.7 keeps the cmd.exe rules of `Invoke-Step`. Task 5.7 adds the Windows run to Needs Human Verification. |
| 11 | The package test needs nuget.org for the BRUTAL packages, because its restore folder is private. | The step fails with no network. | The solution restore needs nuget.org already. The HTTP cache of NuGet serves a known package again. |

---

## Dependencies

- xml-element-loader (complete): `ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, `ElementFactory`, `AttributeSetter`, the public `Register` with its checks, the `TryRead*` reads, `BuiltInElements.cs`, and the 59 tests that must pass unchanged.
- builder-api (complete): the builder methods and `TreeCompare` for the tree tests.
- dev-ops/solution-skeleton (complete): the pack metadata in the root `Directory.Build.props`, `artifacts/packages/`, and the two test scripts.
- `Microsoft.CodeAnalysis.CSharp` 4.14.0: a managed, build-time package, used by the generator project and the generator test project only. It adds no native dependency and no run-time reference.
- .NET SDK 10.0.115: its compiler loads an analyzer that references Roslyn 4.14.0 (A2).
- nuget.org: the package test restores the BRUTAL packages from it.
- binding-attributes builds in parallel and does not depend on this feature. markup-sample depends on the public attribute surface of this feature.

---

## Definition of Done

### Functional Criteria
- [ ] A class `[TemplateElement("Gauge")] sealed class Gauge : Element { public float Level { get; set; } }` in a project with the generator gives an internal extension method `Register<Id>Elements` on `TemplateLoader`. After the call, `<ui:Gauge level="0.5"/>` loads, and `Level` reads 0.5.
- [ ] Each public settable instance property of `string`, `float`, `int`, `bool`, an enum, `float2`, `float4`, `Rgba8`, or `TextureHandle` gives one set row with a kebab-case name. A property of another type gives no row and no diagnostic. `[TemplateIgnore]` removes a row, and `[TemplateRowName("x")]` renames it.
- [ ] An enum row reads a kebab-case keyword (`round-dial` gives `RoundDial`). An `int` row reads 0, a negative number, and a positive number.
- [ ] A `[TemplateFactory]` method with `[TemplateCreateRow]` attributes gives a factory type with create rows in source order. `IsRequired = true` gives `MissingAttribute` when the attribute is absent.
- [ ] A static class with `[TemplateElement("Dial", Target = typeof(Dial))]` gives the rows of `Dial`.
- [ ] `IsLeaf = true` gives `UnexpectedChild` for a child element.
- [ ] `new TemplateLoader()` registers the eight built-in types through the generated `RegisterBrutalUiMarkupElements`. Each type has the same rows, kinds, order, and leaf flag as before.
- [ ] `TemplateLoader.Register` stays public and unchanged in signature and checks.

### Quality Criteria
- [ ] Each of `BRUTALUI0001` to `BRUTALUI0006` has a test that asserts the id, the severity `Error`, the line, the column, and the quoted text. A type with no usable constructor and with a factory gives no diagnostic (Q3).
- [ ] A type with an error gets no generated code, and the other types of the assembly keep their code.
- [ ] An unrelated edit leaves the tracked steps `TemplateElementModels` and `TemplateElementSet` `Cached` or `Unchanged`. Plant 10 fails the test.
- [ ] The golden test pins the full generated text of one sample type.
- [ ] Each existing test passes with no edit. The totals grow by 24 in Markup and 16 in the new Generators project from the baseline of `context.md`.
- [ ] The build gives `0 Warning(s)` and `0 Error(s)`. Each new public member has an XML doc comment. Each new or changed file under `src/` and `tests/` has fewer than 400 lines.
- [ ] The generated source holds no `System.Reflection`, `Activator`, `Enum.Parse`, `GetType()`, or `typeof(`.

### Integration Criteria
- [ ] `src/BrutalUi.Markup/BrutalUi.Markup.csproj` references `BrutalUi.Core` and, as an analyzer only, `BrutalUi.Generators`. The Markup package holds `analyzers/dotnet/cs/BrutalUi.Generators.dll`, has no other `Generators` entry, and lists no `BrutalUi.Generators` dependency. No `BrutalUi.Generators` package exists.
- [ ] `src/BrutalUi.Generators/BrutalUi.Generators.csproj` targets `netstandard2.0` and sets `IsPackable` `false`, `IsRoslynComponent` `true`, and `EnforceExtendedAnalyzerRules` `true`. `Directory.Packages.props` pins `Microsoft.CodeAnalysis.CSharp` at `4.14.0`. `context.md` notes the build-time package.
- [ ] `AttributeContext.cs` has additions only. `AddAttributeHandler`, `AttributeHandler`, `AttributeContext.AddError`, and `TemplateError` do not change, so binding-attributes keeps its contract.
- [ ] `BuiltInElements.cs` holds eight declarations with factories and no `Register` call. The `TextField` factory calls `TemplateValues.CheckCapacity`.
- [ ] `src/BrutalUi.Core/`, `src/BrutalUi.Framework/`, `src/BrutalUi.Markup/Style/`, `tests/BrutalUi.Markup.Tests/Template/`, `tests/BrutalUi.Core.Tests/`, and `samples/` do not change.
- [ ] Both scripts pack, restore, and run `tests/BrutalUi.PackageTest`, which is not in the solution.
- [ ] Sections 3, 11, and 14 of `technical-design.md` name the generator project, the attributes, the rules, the diagnostics, and the package test. `CHANGELOG.md` has one line tagged `(markup/element-generator)`.

### Verification Method
1. Run `export DOTNET_CLI_HOME=/tmp/dui-cli-home MSBUILDDISABLENODEREUSE=1; dotnet build-server shutdown; dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)`.
2. Run `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and 3 skipped, and a total 40 above the baseline of `context.md` (3132 at planning time).
3. Run `tests/BrutalUi.Markup.Tests/bin/Debug/net10.0/BrutalUi.Markup.Tests --filter-class '<Class>' 2>&1 | tail -n 5` for each class. Expect `failed: 0` and these totals: `*GeneratedElementTests` 6, `*TemplateReadMethodTests` 4, `*GeneratedAuthoringTests` 6, `*BuiltInRowListTests` 8.
4. Run `tests/BrutalUi.Generators.Tests/bin/Debug/net10.0/BrutalUi.Generators.Tests --filter-class '<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `*DiagnosticTests` 8, `*GeneratorOutputTests` 8.
5. Run `git diff --stat <base> -- src/BrutalUi.Core src/BrutalUi.Framework src/BrutalUi.Markup/Style tests/BrutalUi.Markup.Tests/Template tests/BrutalUi.Core.Tests samples`. Expect no output. `<base>` is the commit of `context.md`.
6. Run `dotnet pack brutal-ui.sln --no-build --configuration Debug 2>&1 | tail -n 5`. Run `unzip -l artifacts/packages/BrutalUi.Markup.0.1.0-alpha.nupkg | grep Generators`. Expect one line, `analyzers/dotnet/cs/BrutalUi.Generators.dll`. Run `unzip -p artifacts/packages/BrutalUi.Markup.0.1.0-alpha.nupkg BrutalUi.Markup.nuspec | grep -c Generators`. Expect 0. Run `ls artifacts/packages | grep -c Generators`. Expect 0.
7. Run `rm -rf artifacts/package-test && dotnet restore tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --force && dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --no-restore`. Expect `package-test: PASS Gauge level 0.5` and exit code 0.
8. Run `scripts/test-linux.sh`. Expect exit code 0 and a PASS line that names `pack` and `package`.
9. Run `dotnet build src/BrutalUi.Markup -p:EmitCompilerGeneratedFiles=true -nodeReuse:false` and `dotnet build tests/BrutalUi.Markup.Tests -p:EmitCompilerGeneratedFiles=true -nodeReuse:false`. Run `grep -rnE 'System\.Reflection|Activator|Enum\.Parse|GetType\(\)|typeof\(' src/BrutalUi.Markup/obj/Debug/net10.0/generated tests/BrutalUi.Markup.Tests/obj/Debug/net10.0/generated`. Expect no output. Read the Markup file, and expect the eight types with the rows of section 11.
10. Run `grep -c 'Register(' src/BrutalUi.Markup/Template/BuiltInElements.cs`. Expect 0. Run `grep -c 'TemplateElement(' src/BrutalUi.Markup/Template/BuiltInElements.cs`. Expect 8. Run `grep -n CheckCapacity src/BrutalUi.Markup/Template/BuiltInElements.cs`. Expect one line.
11. Run `grep -n 'Microsoft.CodeAnalysis.CSharp' Directory.Packages.props`. Expect `Version="4.14.0"`. Run `grep -nE 'netstandard2.0|IsPackable>false|IsRoslynComponent>true|EnforceExtendedAnalyzerRules>true' src/BrutalUi.Generators/BrutalUi.Generators.csproj`. Expect four lines. Run `grep -n 'Microsoft.CodeAnalysis.CSharp' docs/features/markup/element-generator/context.md`. Expect a line.
12. Run `git diff <base> -- src | grep -E '^\+.*public (sealed |static |readonly |abstract )*(partial )*(class|struct|interface|enum|record|delegate) '`. Expect 5 lines: `TemplateElementAttribute`, `TemplateFactoryAttribute`, `TemplateCreateRowAttribute`, `TemplateRowNameAttribute`, and `TemplateIgnoreAttribute`. Run `git diff <base> -- src/BrutalUi.Markup/Template/AttributeContext.cs | grep -E '^-[^-]'`. Expect no output.
13. Run `wc -l` on each new or changed `.cs` file under `src/` and `tests/`. Expect each under 400 lines.
14. Run `dotnet sln brutal-ui.sln list`. Expect `BrutalUi.Generators` and `BrutalUi.Generators.Tests`, and no `BrutalUi.PackageTest`.
15. Run `grep -n 'markup/element-generator' CHANGELOG.md`. Expect one line. Run `grep -n 'BrutalUi.Generators' docs/technical-design.md`. Expect lines in section 3 and section 11.
16. Read `context.md`. Expect plants 1 to 13, each with a non-zero exit code and the expected failed test or failed step.
17. Human check: run `scripts/test-windows.ps1` on Windows. Expect exit code 0 and a PASS line that names `pack` and `package`.

---

## Testing Strategy

- **End-to-end tests (Markup, 16):** `GeneratedElementTests` and `GeneratedAuthoringTests` compile marked types with the real generator in `BrutalUi.Markup.Tests`, call the generated register call, and load templates. They prove the game path, the factory, the declaration form, and each read kind.
- **Read tests (Markup, 4):** `TemplateReadMethodTests` covers `TryReadInteger` and `TryReadKeyword` through a type that a test registers by hand.
- **Row list tests (Markup, 8):** `BuiltInRowListTests` pins the row list and the leaf flag of each built-in type. It runs before and after the replacement.
- **Generator tests (Generators, 16):** `DiagnosticTests` covers each diagnostic and the no-code rule. `GeneratorOutputTests` covers the golden text, the name rules, the order, the empty output, and the incremental cache.
- **Package test (outside the solution):** `tests/BrutalUi.PackageTest` proves the pack layout from a real package reference, in both scripts.
- **Plants (13):** each phase plants a fault and proves that a named test or a named step fails. The plants guard the setter, the walk stop, the leaf flag, the pack item, the private assets, the required flag, the enum keyword, the integer sign, the duplicate check, the cache, the capacity ceiling, the leaf flag of `Image`, and the row order.
- **No regressions:** each existing test passes with no edit. The 59 xml-element-loader tests and the 0-byte gate guard the loaded tree.
- **Manual check:** the Windows run of `scripts/test-windows.ps1` only.

| Class | Project | Phase | Tests |
|---|---|---|---|
| `GeneratedElementTests` | Markup | 1 | 6 |
| `TemplateReadMethodTests` | Markup | 3 | 4 |
| `GeneratedAuthoringTests` | Markup | 3 | 6 |
| `DiagnosticTests` | Generators | 4 | 8 |
| `GeneratorOutputTests` | Generators | 4 | 8 |
| `BuiltInRowListTests` | Markup | 5 | 8 |

---

## Related Documentation

- `docs/features/markup/element-generator/requirements.md`
- `docs/features/markup/epic-overview.md` and `docs/features/markup/epic-requirements.md`
- `docs/features/markup/xml-element-loader/implementation.md` (the table shape, The Written Table, Notes item 10) and `context.md` (the baseline, review CR-1)
- `docs/features/markup/binding-attributes/requirements.md` (the hook contract that this feature does not change)
- `docs/features/markup/markup-sample/requirements.md` (the custom type in HelloPanel)
- `docs/technical-design.md` section 3 (the project tree), section 11 (the markup format, line 1183), and section 14 (the tests)
- `src/BrutalUi.Markup/Template/BuiltInElements.cs`, `ElementTypeInfo.cs`, `AttributeContext.cs`, `TemplateValues.cs`, `TemplateLoader.cs`
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

1. **Conflict: the target framework rule.** `AGENTS.md` says "Target .NET 10". A2 sets `netstandard2.0` for `BrutalUi.Generators`, because the Roslyn host loads only `netstandard2.0` analyzers. The plan follows A2 for the generator project only. Every other project, the generator test project included, stays on `net10.0`. Task 1.1 records the exception in `context.md`.
2. **A reading: the constructor rule.** Requirements line 15 names "no public constructor without parameters". Q3 adds the factory. The plan also accepts an `internal` constructor when the target is in the compiling assembly, because the generated code can call it. Options: (a) public or same-assembly internal (recommended, the plan). (b) Public only.
3. **A reading: A6 and A7.** A6 keeps the base rows written in Markup. A7 says `BuiltInElements.cs` keeps only the declarations and the factories. The plan moves the base rows and their setters to a new file, `BaseRows.cs`. Options: (a) a new `BaseRows.cs` (recommended, the plan). (b) Keep the base rows in `BuiltInElements.cs` and read A7 as "no set rows".
4. **Open point: the `int` read.** A4 lists `int`. The existing `TryReadWholeNumber` reads 1 or more only. The plan adds `TryReadInteger` (an optional sign, digits, the `int` range). Options: (a) a new `TryReadInteger` (recommended, the plan). (b) Map `int` to `TryReadWholeNumber`, add no member, and accept that 0 is an error.
5. **The `scale-mode` message changes.** The generated enum row reports `'<value>' is not a value of scale-mode. Write 'stretch-to-fill', 'scale-to-fit', or 'scale-and-crop'.` in place of `... is not a scale mode ...`. The tests read the kind and the position only (`TemplateValueTests.cs` lines 141 to 147), so they pass unchanged. The keywords stay the same.
6. **A reading: Q2 and `Element.States`.** Q2 makes each public settable property of a supported type a row. `Element.States` is a public settable enum property. A6 keeps the base rows written, and the tests read each row list. The plan stops the walk before `Element` (Decision 4), so `States` gives no row.
7. **Open point: a clash with a built-in name.** `BRUTALUI0002` checks the names inside one compilation only. A game type named `Label` gives the existing `ArgumentException` of `Register` at load time. Options: (a) keep the run-time check (recommended, the plan, YAGNI). (b) Add a check against the eight built-in names in the generator, with a list that must follow the declarations.
8. **Hand-off to markup-sample.** An analyzer from a `ProjectReference` does not flow to the next project. HelloPanel references Markup as a project, so markup-sample adds `<ProjectReference Include="../../src/BrutalUi.Generators/BrutalUi.Generators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />` to `HelloPanel.csproj`, as task 1.10 does for the test project. HelloPanel then calls `loader.RegisterHelloPanelElements()`. A package consumer needs no extra line.
9. **A known limit: required members and the constructor path.** A game type with a `required` property and no factory gives `new T()` in the generated code, and that fails with CS9035. `BRUTALUI0001` does not detect it. Options: (a) accept the limit and state it in the docs (recommended). (b) Treat a type with a `required` member as a type with no usable constructor.
10. **A known limit: a declaration cannot skip a property of its target.** `[TemplateIgnore]` sits on the property, so a declaration of a type of another assembly cannot skip a property. When Core adds a public settable property of a supported type, each built-in type gets a new row. `BuiltInRowListTests` then fails, and the developer decides. This is the intended guard.
11. **The generator project joins the solution in phase 4.** Phases 1 to 3 build it through the `ProjectReference` of `BrutalUi.Markup.Tests` and Markup. The six-file rule of the slice puts the solution edit in phase 4, with the test project.
12. **The language version of the generator tests.** Roslyn 4.14.0 knows no `LanguageVersion.CSharp14`. The harness parses the samples with `LanguageVersion.CSharp13`. The generated code uses no C# 14 feature (Decision 12), and the real builds run the compiler of SDK 10.0.115.
13. **The planned totals** come from the xml-element-loader close: the solution 3092 with 3 skipped at `ed767cc`. Task 1.1 records the real baseline and the Markup and Core totals, and the acceptance criteria count the deltas from it.
14. **The time stamp.** The planner had no clock. The time of **Last Updated** is an estimate.
