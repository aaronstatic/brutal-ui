# XML Element Loader - Implementation Plan

**Epic:** markup (feature 2 of 6)
**Status:** Ready for Review
**Started:** 2026-11-13
**Target Completion:** 2026-11-13
**Last Updated:** 2026-11-13 08:38

---

## Quality Bar

**Target quality:** A public library API that turns a UTF-8 XML template into the same element tree as the builder API. Three later features build on its table shape, its hook, and its error type. A bad seam here moves into element-generator, binding-attributes, and definition-loader. Five things carry the feature.

- **Correct errors with positions:** Each fault of a template gives one `TemplateError` with the right kind, the line and the column of `IXmlLineInfo`, the source name, and a message that quotes the text at fault and names the fix. The loader records each error and continues (A6). `Load` never throws for the text of a template with the built-in types. Each of the 8 error kinds has a test that asserts the kind, the line, and the column.
- **An API that fits the builder surface:** Each attribute maps to one builder method or one create-info field (A2, Q2). A loaded tree and a builder tree compare equal under `TreeCompare`. The API follows `StyleSheetParser`: a `CreateInfo` with a copied name map, a span overload and a `string` overload, and a result with `Errors` and `GetRootOrThrow` (A10).
- **The 0-byte gate:** A loaded tree allocates 0 bytes in `Update` and `Draw` after warm-up. `LoadedTreeAllocationTests` measures it, and a plant fails the gate.
- **Tests with plants:** Each phase plants a fault and proves that a named test fails. The tests use the public API and `TreeCompare` only, so they pass unchanged when element-generator replaces the written table.
- **Accurate docs:** Section 11 of `technical-design.md` names each public type, each row, each value form, and each error kind. Each public member has an XML doc comment in ASD-STE100 prose (CS1591 is an error). Each new source file stays under 400 lines.

---

## Executive Summary

This feature adds the XML template loader to `BrutalUi.Markup`, next to the stylesheet parser. A new folder `src/BrutalUi.Markup/Template/` holds it.

- `TemplateLoader` is an instance class with a `CreateInfo` (A1). It reads a template with `System.Xml.XmlReader` (DTD prohibited, A7) and gives a `TemplateLoadResult`: the root element, or a list of `TemplateError` values (A10).
- The table is a row list for each type (the Chosen Approach). An `ElementTypeInfo` holds the XML name, an `ElementFactory`, and a list of `AttributeRow` values. A row is a create row (the factory reads it) or a set row (a static `AttributeSetter`). Four base rows (`name`, `class`, `pickable`, `focusable`) apply to each type.
- The written table for the eight built-in types lives in one file, `BuiltInElements.cs`. element-generator replaces that file later. `Register` stays public, so a game registers a custom type with the same call.
- An attribute with no row goes to the attribute hook. `AddAttributeHandler` adds a handler. An attribute that no handler takes gives `UnknownAttribute` (A9). binding-attributes registers `command` and `bind-text` on this hook.
- `AttributeContext` and `ElementCreateArgs` give a setter, a factory, and a handler the typed value reads and an `AddError` call with the position of the attribute.
- `text-key` gives a localized text for `Label`, `Button`, and `TextField` (Q4, A4). `texture="icon"` reads the texture map of the create info (Q3).

The plan has 5 phases and 38 tasks, about 325 steps. Phase 1 (11 tasks) loads the section 11 template with `Element`, `Label`, and `Button`, and it compares the result with the builder tree (Q6). Phase 2 (11 tasks) adds the other five types, the create rows, the value forms, and the 0-byte gate. Phase 3 (6 tasks) adds the error cases of A8 and the continue rule. Phase 4 (5 tasks) adds the checks of `Register` and the tests for a custom type and the hook. Phase 5 (5 tasks) adds `text-key` and the changelog line. Phase 1 needs an advanced dev agent, because it sets the public API that three later features build on. The other phases are STANDARD.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The markup author** writes a screen as an XML template and gets an error with a line, a column, and a fix for each fault.
- [ ] **The game developer** loads a template at start, finds an element with `Find`, adds the handlers in panel code (A3), and gets a tree with no allocation in a frame.
- [ ] **The framework contributor** of element-generator, binding-attributes, and definition-loader writes rows, registers a hook handler, and reports an error with a position.

**User Subsets Served:**
- [ ] **Simple** - a game loads the section 11 template with `new TemplateLoader().Load(bytes, "hud.xml").GetRootOrThrow()` and adds the root to its context.
- [ ] **Complex** - a game gives a texture map, registers a custom `Gauge` type with a factory and two rows, adds a hook handler for its own attribute, and prints each error of a bad template with `ToString`.

**UX Approach:**
- Load time: the loader runs before the frame loop and allocates. It never throws for the text.
- Run time: the loaded tree is a normal element tree. It allocates nothing in `Update` and `Draw`.
- No inline style (Q5), no `command` and no `bind-text` (binding-attributes), no hot reload, no include.

---

## Goals

### Primary Goals
1. Read a UTF-8 template with `XmlReader` and give the root or a list of errors (requirements line 12).
2. Map each element name in the `brutal-ui` namespace to a registered type, and each attribute to a row, with no reflection (requirements line 13).
3. Ship a written table for the eight built-in types with a row for each value method of the builder API (requirements line 14, A2).
4. Let a game register a custom type with a factory and its setters (requirements line 15).
5. Send each attribute with no row to the attribute hook (requirements line 16, A9).
6. Report each error with a kind, a line, a column, a message, and a source name (requirements line 18, A10).
7. Prove "markup equals code" with `TreeCompare`, and prove the 0-byte rule with a gate (requirements lines 19 and 20).

### Success Criteria
- [ ] The phase 1 demo passes: `SectionElevenTemplateTests` reads a total of 6 with `failed: 0`, and `TheSectionElevenTemplateEqualsTheBuilderTree` is one of them.
- [ ] The phase 2 demo passes: `EightTypeTreeTests` reads 2 with `failed: 0`, and `LoadedTreeAllocationTests` reads 2 with `failed: 0`.
- [ ] The phase 3 demo passes: `TemplateErrorTests` reads 12 with `failed: 0`.
- [ ] The phase 4 demo passes: `CustomTypeTests` reads 6 and `AttributeHookTests` reads 5, each with `failed: 0`.
- [ ] The phase 5 demo passes: `LocalizedTextTests` reads 6 with `failed: 0`.
- [ ] Each existing test passes with no edit. The planned delta is Markup +58 and Core +1, so the solution grows from 3032 to 3091 with 3 skipped. Task 1.1 records the real baseline, and the deltas count from it.

---

## Architecture Overview

### The Place in the Epic

xml-element-loader is feature 2 of 6. It reads the property surface of builder-api and reuses `TreeCompare`. element-generator writes the rows in the shape of `AttributeRow` and replaces `BuiltInElements.cs`. binding-attributes calls `AddAttributeHandler` from Framework and reports an error through `AttributeContext.AddError`. definition-loader loads a template through `TemplateLoader.Load`. markup-sample prints `TemplateError.ToString`.

```
BrutalUi.Markup (references BrutalUi.Core only)
  Style/                         (stylesheet parser, does not change)
  Template/
    TemplateLoader.cs            public: Namespace, CreateInfo, Register, AddAttributeHandler, Load
    TemplateReader.cs            internal: the XmlReader walk of one Load call
    TemplateLoadResult.cs        public: Root, Errors, Succeeded, GetRootOrThrow
    TemplateError.cs             public: TemplateError, TemplateErrorKind
    ElementTypeInfo.cs           public: ElementTypeInfo, AttributeRow, AttributeRowKind, 3 delegates. internal: RegisteredType
    AttributeContext.cs          public: AttributeContext, ElementCreateArgs (readonly ref structs)
    TemplateValues.cs            internal: the value forms (bool, whole number, number, list, color)
    BuiltInElements.cs           internal: the written table. element-generator replaces this file
        |
        +--> BrutalUi.Core: Element, Label, Button, Toggle, Slider, TextField, ScrollView, Image, ClassName, TextureHandle, LString
        +--> System.Xml: XmlReader, XmlReaderSettings, IXmlLineInfo, XmlException, XmlConvert
```

### File Tree (end state)

```
src/BrutalUi.Markup/Template/
├── TemplateError.cs        # phase 1: the 8 kinds, the struct, ToString, GetKindName
├── TemplateLoadResult.cs   # phase 1
├── ElementTypeInfo.cs      # phase 1: the table shape and RegisteredType
├── AttributeContext.cs     # phase 1: Name, Value, position, AddError, TryGet. phase 2: TryRead*. phase 4: the TryGet check
├── TemplateLoader.cs       # phase 1: the API. phase 2: the texture map, CreateRequired. phase 3: the messages. phase 4: the Register checks
├── TemplateReader.cs       # phase 1: the walk. phase 2: the required check, the null factory rule. phase 3: the A8 cases, the sort
├── TemplateValues.cs       # phase 2 (new)
└── BuiltInElements.cs      # phase 1: name, class, Element, Label, Button. phase 2: the rest. phase 5: text-key
tests/BrutalUi.Markup.Tests/
├── BrutalUi.Markup.Tests.csproj           # phase 1: two Compile links
└── Template/                              # new folder
    ├── SectionElevenTemplateTests.cs      # phase 1: 6 tests
    ├── BuiltInTypeTests.cs                # phase 2: 9 tests
    ├── TemplateValueTests.cs              # phase 2: 10 tests
    ├── EightTypeTreeTests.cs              # phase 2: 2 tests
    ├── LoadedTreeAllocationTests.cs       # phase 2: 2 tests
    ├── TemplateErrorTests.cs              # phase 3: 12 tests
    ├── CustomTypeTests.cs                 # phase 4: 6 tests
    ├── AttributeHookTests.cs              # phase 4: 5 tests
    └── LocalizedTextTests.cs              # phase 5: 6 tests
tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs        # phase 5: LocalizedText compares by value
tests/BrutalUi.Core.Tests/Builder/TreeCompareTests.cs  # phase 5: +1 test
docs/technical-design.md                   # each phase: section 11. phase 2: section 14
CHANGELOG.md                               # phase 5
docs/features/markup/xml-element-loader/context.md     # each phase
```

These files do not change: `src/BrutalUi.Core/`, `src/BrutalUi.Framework/`, `src/BrutalUi.Markup/Style/`, `src/BrutalUi.Markup/BrutalUi.Markup.csproj`, `samples/`, `Directory.Packages.props`, and `brutal-ui.sln`. `System.Xml` is part of the shared framework, so the feature adds no package and no native dependency.

### Public API (end state)

All types are in the namespace `BrutalUi.Markup`, as the stylesheet types are.

```csharp
namespace BrutalUi.Markup;

public sealed class TemplateLoader
{
    public const string Namespace = "brutal-ui";                     // the XML namespace of each element

    public struct CreateInfo
    {
        public IReadOnlyDictionary<string, TextureHandle>? Textures;  // null: no texture name is valid. Copied at creation
    }

    public TemplateLoader();                                         // registers the eight built-in types
    public TemplateLoader(in CreateInfo info);                       // copies the texture map, registers the eight built-in types
    public void Register(in ElementTypeInfo info);                   // ArgumentException for a bad or duplicate entry. Copies the rows
    public void AddAttributeHandler(AttributeHandler handler);       // the hook. Handlers run in the order of the add
    public TemplateLoadResult Load(ReadOnlySpan<byte> utf8, string sourceName = "template");
    public TemplateLoadResult Load(string text, string sourceName = "template");  // encodes to UTF-8, then loads
}

public sealed class TemplateLoadResult
{
    public Element? Root { get; }                                    // null when Errors holds an error
    public IReadOnlyList<TemplateError> Errors { get; }              // line, then column. Empty on success
    public bool Succeeded { get; }                                   // Root is not null
    public Element GetRootOrThrow();                                 // InvalidOperationException with one line for each error
}

public readonly struct TemplateError
{
    public TemplateErrorKind Kind { get; }
    public int Line { get; }                                         // from 1
    public int Column { get; }                                       // from 1, IXmlLineInfo.LinePosition
    public string Message { get; }
    public string SourceName { get; }
    public override string ToString();                               // "hud.xml(3,59): UnknownAttribute: <message>"
}

public enum TemplateErrorKind : byte
{
    NotWellFormed, UnknownElement, UnknownAttribute, InvalidValue,
    MissingAttribute, UnknownTexture, UnexpectedText, UnexpectedChild,
}

public struct ElementTypeInfo
{
    public required string Name;                                     // the XML local name, for example "Slider"
    public required ElementFactory Create;
    public AttributeRow[]? Rows;                                     // the own rows of the type. null: none
    public bool IsLeaf;                                              // true: a child element is an UnexpectedChild error
}

public readonly struct AttributeRow
{
    public AttributeRow(string name, AttributeRowKind kind);         // Create or CreateRequired. Set gives ArgumentException
    public AttributeRow(string name, AttributeSetter setter);        // Set
    public string Name { get; }
    public AttributeRowKind Kind { get; }
    public AttributeSetter? Setter { get; }                          // null for a create row
}

public enum AttributeRowKind : byte { Set, Create, CreateRequired }

public delegate Element? ElementFactory(ElementCreateArgs args);    // null only after an AddError
public delegate void AttributeSetter(Element element, AttributeContext context);
public delegate bool AttributeHandler(Element element, string name, string value, AttributeContext context);  // true: handled

public readonly ref struct AttributeContext
{
    public string Name { get; }                                      // the attribute name
    public string Value { get; }                                     // the attribute value as XmlReader gives it
    public int Line { get; }
    public int Column { get; }
    public string SourceName { get; }
    public void AddError(TemplateErrorKind kind, string message);    // at the position of the attribute
    public bool TryReadBool(out bool value);                         // each TryRead adds InvalidValue on a fault
    public bool TryReadWholeNumber(out int value);                   // 1 or more
    public bool TryReadNumber(out float value);
    public bool TryReadNumbers(out float2 value);
    public bool TryReadNumbers(out float4 value);
    public bool TryReadColor(out Rgba8 value);
    public bool TryReadTexture(out TextureHandle value);             // UnknownTexture for a name that the map does not hold
}

public readonly ref struct ElementCreateArgs
{
    public string ElementName { get; }
    public int Line { get; }                                         // the position of the element name
    public int Column { get; }
    public string SourceName { get; }
    public bool TryGet(string name, out AttributeContext context);   // a create row of the type only, else ArgumentException
    public void AddError(TemplateErrorKind kind, string message);    // at the position of the element
}
```

The count: 13 new public types (12 top-level types, 3 of them delegates, and the nested `CreateInfo`). The internal types are `TemplateReader`, `RegisteredType`, `TemplateValues`, and `BuiltInElements`.

### The Load Walk

```
Load(utf8, sourceName):
  settings: DtdProcessing.Prohibit, XmlResolver null, IgnoreComments, IgnoreProcessingInstructions,
            IgnoreWhitespace, ConformanceLevel.Document, CloseInput
  reader = XmlReader.Create(new MemoryStream(utf8.ToArray(), writable: false), settings)
  frames = stack of (Element? parent, bool isLeaf)          a null parent: the subtree reports errors but attaches nothing
  try
    while reader.Read():
      Element                -> ReadElement(). When not IsEmptyElement, push the frame of the element
      EndElement             -> pop
      Text, CDATA            -> UnexpectedText at the node (phase 3)
      Whitespace, SignificantWhitespace, XmlDeclaration -> skip
  catch XmlException ex     -> NotWellFormed at (max(1, ex.LineNumber), max(1, ex.LinePosition)). Stop the read
  sort the errors by line, then column, stable (phase 3)
  errors > 0 -> Failure(errors). Else Success(root)

ReadElement():
  1. The element is outside Namespace           -> UnknownElement. Read the subtree with a null parent
  2. The parent frame is a leaf                 -> UnexpectedChild. Read the subtree with a null parent
  3. No registered type for the local name      -> UnknownElement. Read the subtree with a null parent
  4. Read each attribute in source order: skip a namespace declaration (xmlns, xmlns:p).
     A prefixed attribute                       -> UnknownAttribute (phase 3)
     Find the row by name: base rows, then the own rows. No row -> the hook list
  5. A CreateRequired row with no attribute     -> MissingAttribute at the element. Skip steps 6 to 8
  6. element = type.Create(args). null with no new error -> InvalidOperationException (a bug of the factory)
  7. Run each set row that has an attribute, in table order (base rows first, then the own rows)
  8. For each attribute of the hook list, in source order: call the handlers in the order of the add.
     The first handler that returns true takes it. No handler takes it -> UnknownAttribute with the valid names
  9. Attach the element to the parent of the frame, or keep it as the root
```

- Step 7 runs `value` of a `Slider` after the factory made the range from `min`, `max`, and `step`. Thus the order of the attributes in the source does not change the tree.
- The position of an element error is the position that `IXmlLineInfo` gives for the element node: the first character of the qualified name, after the `<`. The position of an attribute error is the first character of the attribute name. A value has no position of its own, so a value error points at the name of its attribute.
- `LinePosition` counts UTF-16 units of the decoded text. A tab counts 1. A character outside the BMP counts 2. This differs from `StyleParseError`, which counts code points (Notes item 4).
- An unprefixed attribute is in no namespace in XML. The loader reads it as an attribute of its element. A prefixed attribute is an error, also with the `brutal-ui` namespace, so each template uses one form (Notes item 1).
- `Load` holds no state between calls. Each call makes its own reader, frame stack, and error list. One loader can load more than one text, also on more than one thread, when no `Register` and no `AddAttributeHandler` run at the same time.

### The Written Table

The base rows apply to each type, the built-in types and each custom type. They run first, in this order. Then the own rows of the type run in their order.

| Row | Kind | Effect | Value form |
|---|---|---|---|
| `name` | Set | `Element.Name = value` | text, as is |
| `class` | Set | `ClassName.Intern` for each name, then `Classes.Add` in order | class list |
| `pickable` | Set | `IsPickable` | bool |
| `focusable` | Set | `IsFocusable` | bool |

| Type | Leaf | Factory | Own rows (kind) |
|---|---|---|---|
| `Element` | no | `new Element()` | none |
| `Label` | yes | `new Label(new Label.CreateInfo { Text = text })`, or `LocalizedText` with `text-key` (A4) | `text` (Create), `text-key` (Create, phase 5) |
| `Button` | no | `new Button(new Button.CreateInfo { Text = text })`, or `LocalizedText` with `text-key` | `text` (Create), `text-key` (Create, phase 5) |
| `Toggle` | no | `new Toggle(default)` | `checked` (Set: `Checked`, bool) |
| `Slider` | no | `new Slider(new Slider.CreateInfo { Minimum, Maximum, Step })` after the range checks | `min` (Create), `max` (CreateRequired), `step` (Create), `value` (Set: `Value`, number) |
| `TextField` | no | `new TextField(new TextField.CreateInfo { Capacity, Text })`, then `SetText(LString)` with `text-key` | `capacity` (CreateRequired), `text` (Create), `text-key` (Create, phase 5) |
| `ScrollView` | no | `new ScrollView(new ScrollView.CreateInfo { ScrollSpeed })` after the speed check | `scroll-speed` (Create), `scroll-offset` (Set: `ScrollOffset`, 2 numbers) |
| `Image` | yes | `new Image(new Image.CreateInfo { Texture = default })` | `texture` (Set: `Texture`, texture name), `uv` (Set: `Uv`, 4 numbers), `tint` (Set: `Tint`, color), `scale-mode` (Set: `ScaleMode`, keyword) |

- The 12 value names of A2 each have a row. `Range` becomes `min`, `max`, and `step` (A2). `capacity` and `scroll-speed` are create rows (Q2). The 20 delegate methods have no row (A3).
- A missing `text` gives an empty text, as `default(Label.CreateInfo)` does. A missing `min` or `step` gives 0. A missing `scroll-speed` gives 0, so the view uses `ScrollView.DefaultScrollSpeed` (40). A missing `texture` gives the none handle.
- `max` is required, because `Slider.CreateInfo.Maximum` is `required` and a range of 0 to 0 has no use. `texture` is optional, because the none handle is valid and panel code can set the texture later (Notes item 3).
- `scroll-offset` writes `ScrollView.ScrollOffset`, as the builder method does. The set clamps with the layout of the last update, so on a new tree the offset stays (0, 0). The builder tree reads the same, so the trees compare equal (Notes item 13).

### The Value Forms

- **Whitespace.** A typed value (bool, whole number, number, list, color, keyword, texture name) ignores XML whitespace (space, tab, CR, LF) at both ends. `name`, `text`, and `text-key` keep the value as `XmlReader` gives it. XML normalizes a tab or a line break inside an attribute value to a space.
- **A bool** is `true` or `false`. The compare is case-sensitive. `True`, `1`, and `yes` give `InvalidValue`.
- **A whole number** (`capacity`) is ASCII digits only, from 1 to `int.MaxValue`. A sign, a point, and 0 give `InvalidValue`.
- **A number** reads with `CultureInfo.InvariantCulture` and `NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint`: an optional sign, digits, and an optional `.digits`, or `.digits` alone. The form has no exponent and no group separator, as the stylesheet number form. A number that is not finite after the read (`NaN`, a value too large for a `float`) gives `InvalidValue`.
- **A list** (`uv`, `scroll-offset`) holds numbers with XML whitespace between them (A5). `uv` takes 4 numbers (u0, v0, u1, v1). `scroll-offset` takes 2 numbers (x, y). Another count gives `InvalidValue`.
- **A class list** holds names with XML whitespace between them (A5). An empty value adds no class. A repeated name adds nothing, as `ClassList.Add` does.
- **A color** is `#` and 3, 6, or 8 hex digits in each case (A5). `#rgb` doubles each digit and sets the alpha to 255. `#rrggbb` sets the alpha to 255. `#rrggbbaa` holds the alpha. These are the rules of `StyleValues.TryReadColor`.
- **A keyword** (`scale-mode`) is `stretch-to-fill`, `scale-to-fit`, or `scale-and-crop`, through an explicit table with no `Enum.Parse`.
- **A texture name** reads `CreateInfo.Textures` with an ordinal compare (Q3). A name that the map does not hold gives `UnknownTexture`.
- **The range checks.** `min`, `max`, and `step` are each a finite number. `max` below `min` gives `InvalidValue` at `max`. A width `max - min` that is not finite gives `InvalidValue` at `max`. A negative `step` gives `InvalidValue` at `step`. `scroll-speed` below 0 gives `InvalidValue`. The factory checks each case before it calls the constructor, so no `ArgumentOutOfRangeException` leaves `Load`.
- **`text-key`** (phase 5, A4). `text-key="k"` with `text="t"` gives `new LString("k", "t")`. `text-key` with no `text` gives `new LString("k", "")`. An empty `text-key` gives `InvalidValue`, because `LString` rejects an empty id.

### The Messages

Each message quotes the text at fault in single quotes, names the problem, and names the fix, in one or two short sentences. Examples:

- `UnknownElement`: `'Panel' is not an element type. The types are Button, Element, Image, Label, ScrollView, Slider, TextField, Toggle.`
- `UnknownElement` (namespace): `'Label' is not in the namespace 'brutal-ui'. Add xmlns:ui="brutal-ui" and write <ui:Label>.`
- `UnknownAttribute`: `'colour' is not an attribute of Label. The attributes of Label are name, class, pickable, focusable, text, text-key.`
- `InvalidValue`: `'0,5' is not a number. Write a number such as '0.5'.`
- `MissingAttribute`: `TextField needs the attribute 'capacity'.`
- `UnknownTexture`: `'icon2' is not a texture name. Add it to TemplateLoader.CreateInfo.Textures.`

`ToString` gives `<source>(<line>,<column>): <kind>: <message>`. The text of a message can change in a later version. Code reads `Kind`, `Line`, and `Column`.

---

## Implementation Phases

The feature has 38 tasks, so it has five slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea, and rule 11 binds it to the demo of Q6: the section 11 template loads and equals the builder tree. The other five types come second, because each widget screen needs them, and the gate guards the loaded tree. The error cases come third, because the markup author needs each fault with a position. The custom type and the hook come fourth: the path already works for the built-in types, and phase 4 adds the checks and the proof for a game type. `text-key` comes last, because it is an addition to the text rows.

**The agent tier.** Phase 1 is ADVANCED (opus). It sets 13 public types that element-generator, binding-attributes, and definition-loader build on. It uses `readonly ref struct` parameters in delegates and the `XmlReader` line info. A wrong shape there moves into three features. Phases 2 to 5 are STANDARD (sonnet). They fill the table, the cases, and the tests in a shape that phase 1 fixes.

**The commands.** Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. Each task group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Markup.Tests/bin/Debug/net10.0/BrutalUi.Markup.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. The test namespace is `BrutalUi.Markup.Tests.Template`.

**The plants.** For each plant, copy the file to `src/BrutalUi.Markup/obj/plantscratch/` (git-ignored), make the change, and run the solution tests with `dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Check the `error CS` count first: it must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect an exit code that is not 0. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time. Run a second time before you treat it as a fault.

**The tests.** Follow the style of `ParsedSheetAllocationTests` and `ErrorReportTests`. Pass a template as a `u8` literal to the span overload, because `BRUTALCORE0011` rejects a string literal for `Load(string, string)`. A test of the `string` overload passes a local. Each test asserts `Assert.Null(TreeCompare.FirstDifference(expected, actual))` for a tree compare. Each error assert checks `Kind`, `Line`, and `Column`, and checks the message with `Contains` on the quoted text only. A test that changes the culture sets `CultureInfo.CurrentCulture` in a `try` block and restores it in `finally`.

### Phase 1: The Section 11 Template
**Goal:** The loader API exists with the error type, the result type, the table shape, and the two context structs. The written table holds the base rows `name` and `class` and the types `Element`, `Label`, and `Button` with `text`. The hook exists. A test loads the section 11 template and compares it with the builder tree.
**Demo:** the developer runs the tests, and the section 11 template loads and equals the builder tree: `BrutalUi.Markup.Tests --filter-class '*SectionElevenTemplateTests'` reads a total of 6 with `failed: 0`, and `TheSectionElevenTemplateEqualsTheBuilderTree` is one of them.
**Needs:** none
**Agent tier:** ADVANCED (opus). This phase fixes the public API of the epic seam.

**Tasks (group 1, the types, 6 files):**
- [ ] 1.1 Create `docs/features/markup/xml-element-loader/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `cf13f19` at planning time), the solution total with the skipped count (3032 with 3 skipped at planning time), and the totals of `BrutalUi.Markup.Tests` and `BrutalUi.Core.Tests`. Record the Public API, The Load Walk, The Written Table, The Value Forms, and the Notes of this plan. Record the tool notes: `DOTNET_CLI_HOME`, the per-class binary, and the `error CS` check of a plant.
- [ ] 1.2 Write `src/BrutalUi.Markup/Template/TemplateError.cs` in the shape of `StyleParseError.cs`. The enum `TemplateErrorKind : byte` has the 8 members of the Public API, each with a doc line that names its causes (the table of phase 3 in section 11). The struct `TemplateError` has an internal constructor with the same checks (a `null` message or source name, a line or a column below 1), the five properties, `ToString`, and an internal `GetKindName` with an explicit switch. The remarks state the column rule of The Load Walk.
- [ ] 1.3 Write `src/BrutalUi.Markup/Template/TemplateLoadResult.cs` in the shape of `StyleParseResult.cs`: `Root`, `Errors`, `Succeeded`, `GetRootOrThrow`, internal `Success(Element)`, and internal `Failure(List<TemplateError>)`. The exception message joins the `ToString` lines with LF.
- [ ] 1.4 Write `src/BrutalUi.Markup/Template/ElementTypeInfo.cs` with `ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, `ElementFactory`, `AttributeSetter`, and `AttributeHandler` from the Public API. The constructor `AttributeRow(string, AttributeRowKind)` throws `ArgumentException` for `Set`. Add `internal sealed class RegisteredType`: the name, the factory, the leaf flag, one array of rows (the base rows, then the own rows), a `Dictionary<string, int>` from the row name to the index (ordinal), the indices of the `CreateRequired` rows, and the text of the attribute names for a message. The doc of `ElementTypeInfo` states that the loader copies the rows, so a later change of the array changes nothing.
- [ ] 1.5 Write `src/BrutalUi.Markup/Template/AttributeContext.cs` with the two `readonly ref struct` types. Each holds a reference to the internal `TemplateReader` of the load and an index into its attribute list, so it is valid during one call of a factory, a setter, or a handler only. `AttributeContext` gives `Name`, `Value`, `Line`, `Column`, `SourceName`, and `AddError`. `ElementCreateArgs` gives `ElementName`, `Line`, `Column`, `SourceName`, `TryGet`, and `AddError`. `TryGet` gives `false` for a create row with no attribute. The doc states why the types are `ref struct`: a handler cannot keep a context after the load. The `TryRead*` methods come in phase 2.
- [ ] 1.6 Edit `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`. After the `AllocationGate.cs` line, add `<Compile Include="../BrutalUi.Core.Tests/Shared/TreeCompare.cs" LinkBase="Shared" />` and `<Compile Include="../BrutalUi.Core.Tests/Shared/RecordingRenderer.cs" LinkBase="Shared" />`. Build, and expect `0 Error(s)`.

**Tasks (group 2, the loader, the table, and the demo, 6 files):**
- [ ] 1.7 Write `src/BrutalUi.Markup/Template/TemplateLoader.cs` with `Namespace`, `CreateInfo`, both constructors, `Register`, `AddAttributeHandler`, and both `Load` overloads. The constructors copy the texture map into an ordinal `Dictionary<string, TextureHandle>` and call `BuiltInElements.RegisterAll(this)`. `Register` checks `Name` and `Create` for `null` (the full checks come in task 4.1), copies the rows into a `RegisteredType`, and adds it to an ordinal `Dictionary<string, RegisteredType>`. `AddAttributeHandler` appends to a `List<AttributeHandler>`. `Load(ReadOnlySpan<byte>, string)` throws `ArgumentNullException` for a `null` source name and runs a new `TemplateReader`. `Load(string, string)` checks both arguments, encodes with `Encoding.UTF8.GetBytes`, and calls the span overload. The class remarks state the load-time rule, the thread rule, and the namespace rule.
- [ ] 1.8 Write `src/BrutalUi.Markup/Template/TemplateReader.cs` as `internal sealed class TemplateReader` with the walk of The Load Walk: the settings, the frame stack, steps 3, 4, 6, 7, 8, and 9, the `XmlException` catch, and `Success` or `Failure`. Step 3 records a short `UnknownElement` (phase 3 adds the full message and steps 1 and 2). Step 4 skips each namespace declaration. Text nodes are skipped in this phase (phase 3 adds `UnexpectedText`). The attribute list is a `List<(string Name, string Value, int Line, int Column, int Row)>` of the current element, filled through `MoveToNextAttribute` and `IXmlLineInfo`, then `MoveToElement`. Read `IsEmptyElement` before the attribute loop.
- [ ] 1.9 Write `src/BrutalUi.Markup/Template/BuiltInElements.cs` as `internal static class BuiltInElements` with `internal static readonly AttributeRow[] BaseRows` (`name` and `class` in this phase) and `internal static void RegisterAll(TemplateLoader loader)`, which calls `loader.Register` for `Element`, `Label` (leaf, `text` Create), and `Button` (`text` Create). The class remarks state that this file is the written table and that element-generator replaces it. The `class` setter splits on XML whitespace with an index loop and calls `ClassName.Intern` and `Classes.Add` for each name. Each setter and each factory is a `static` lambda.
- [ ] 1.10 Write `tests/BrutalUi.Markup.Tests/Template/SectionElevenTemplateTests.cs` with 6 tests. `TheSectionElevenTemplateEqualsTheBuilderTree` (the template of section 11 after task 1.11, word for word with LF line ends. A test handler for `command` checks that the element is a `Button` and the value is `game.pause`, adds a `static` click handler with `OnClicked`, and returns `true`. The builder tree is `new Element().Class("hud").Child(new Label(default).Name("score").Class("hud-value").Text("0"u8)).Child(new Button(default).Name("pause").Class("hud-button").Text("Pause"u8).OnClicked(Clicked))`. `TreeCompare` gives `null`). `TheHookGetsTheElementTheNameAndTheValue` (a handler records its three arguments and `context.Line` and `context.Column`: line 3, column 59). `AnAttributeThatNoHandlerTakesIsAnUnknownAttributeError` (the same template with no handler: `Root` is `null`, one error, `UnknownAttribute`, line 3, column 59, the source name of the call). `AClassListGivesEachNameInOrder` (`class="a  b&#9;c"` gives three classes in order, and `class=""` gives none). `XmlThatIsNotWellFormedGivesOneErrorAndNoRoot` (a template with no end tag gives one `NotWellFormed` error with a line of 1 or more). `TheStringOverloadGivesTheSameTree` (a local string and its UTF-8 bytes give two trees that compare equal). Confirm the column 59 of `command` against `IXmlLineInfo` and record the column rule in `context.md` (Risk 1).
- [ ] 1.11 Edit `docs/technical-design.md` section 11. Replace `<ui:Panel ...>` and `</ui:Panel>` with `<ui:Element ...>` and `</ui:Element>` (Q1). Replace "through a generated table" with: the loader maps each element name to a registered type and each attribute to a row of a written table, with no reflection, and element-generator replaces the written table later. Add a **The loader API** block with the Public API of this plan (phase 1 members). Add one sentence: binding-attributes registers `command` on the attribute hook, so the template above needs that handler. Run the build and the tests, and expect Markup +6. Run two plants. Plant 1: step 8 records `UnknownAttribute` and calls no handler. Expect `TheSectionElevenTemplateEqualsTheBuilderTree` and `TheHookGetsTheElementTheNameAndTheValue` to fail. Plant 2: the `class` setter adds the first name only. Expect `AClassListGivesEachNameInOrder` to fail. Record the counts, the plants, and the demo state in `context.md`.

**Estimated Steps:** about 95 (11 tasks, about nine steps for one task)

**Files (group 1, 6):** `context.md`, `TemplateError.cs`, `TemplateLoadResult.cs`, `ElementTypeInfo.cs`, `AttributeContext.cs`, `BrutalUi.Markup.Tests.csproj`
**Files (group 2, 6):** `TemplateLoader.cs`, `TemplateReader.cs`, `BuiltInElements.cs`, `SectionElevenTemplateTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 new Markup tests pass, and each existing test passes with no edit.
- [ ] Plants 1 and 2 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] `grep -n "ui:Panel" docs/technical-design.md` gives no output.
- [ ] The section 11 template equals the template text of `TheSectionElevenTemplateEqualsTheBuilderTree`, character for character.

### Phase 2: The Eight Built-In Types
**Goal:** The written table holds all eight types and all rows of The Written Table except `text-key`. The value forms, the create rows, the required check, and the texture map work. A template with each type and each row equals the builder tree. A loaded tree allocates 0 bytes in `Update` and `Draw`.
**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*EightTypeTreeTests'` and reads a total of 2 with `failed: 0`. The eight-type template equals the builder tree in two attribute orders.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the code, 6 files):**
- [ ] 2.1 Write `src/BrutalUi.Markup/Template/TemplateValues.cs` as `internal static class TemplateValues` on `ReadOnlySpan<char>`: `IsXmlWhitespace`, `Trim`, `TryParseBool`, `TryParseWholeNumber`, `TryParseNumber`, `TryParseNumbers(ReadOnlySpan<char>, Span<float>)` (an exact count), and `TryParseColor`. Each method follows The Value Forms and allocates nothing. `TryParseColor` copies the digit rules of `StyleValues.TryReadColor` (Decision 7). Move the split loop of the `class` setter here as `NextName`.
- [ ] 2.2 Edit `src/BrutalUi.Markup/Template/AttributeContext.cs`. Add the seven `TryRead*` methods of the Public API. Each one reads `Value` through `TemplateValues`, and on a fault it adds `InvalidValue` (or `UnknownTexture`) with a message of The Messages and returns `false`. `TryReadTexture` reads the copied map of the loader.
- [ ] 2.3 Edit `src/BrutalUi.Markup/Template/TemplateLoader.cs`. Add `internal bool TryGetTexture(string name, out TextureHandle texture)`. Let `Register` store the `CreateRequired` indices. Add the texture rule to the doc of `CreateInfo.Textures`: the loader copies the map, the compare is ordinal, and `null` makes each `texture` value an `UnknownTexture` error.
- [ ] 2.4 Edit `src/BrutalUi.Markup/Template/TemplateReader.cs`. Add step 5 (`MissingAttribute` at the element for each missing `CreateRequired` row, in table order, and no factory call). Add the rule of step 6: a factory that returns `null` with no new error throws `InvalidOperationException` that names the type. After a failed element, read its subtree with a `null` parent.
- [ ] 2.5 Edit `src/BrutalUi.Markup/Template/BuiltInElements.cs`. Add `pickable` and `focusable` to `BaseRows`. Register `Toggle`, `Slider`, `TextField`, `ScrollView`, and `Image` with the rows, the leaf flags, and the factories of The Written Table. The `Slider` factory and the `ScrollView` factory run the range checks of The Value Forms and report each fault with `AddError` on the context of the attribute. They return `null` after an error. The `scale-mode` setter uses an explicit switch.
- [ ] 2.6 Run the build. Write a scratch template with each type and each row, load it in a quick local run of the test binary, and check that no exception leaves `Load` for each bad value of The Value Forms. Record the result in `context.md`.

**Tasks (group 2, the tests and the docs, 6 files):**
- [ ] 2.7 Write `tests/BrutalUi.Markup.Tests/Template/BuiltInTypeTests.cs` with 9 tests. `AnElementTakesNameClassPickableAndFocusable`. `ALabelTakesText`. `AButtonTakesText`. `AToggleTakesChecked` (`checked="true"` sets `PseudoStates.Checked`, and a `CheckedChanged` counter that the test adds after the load stays 0). `ASliderTakesTheRangeBeforeTheValue` (`<ui:Slider value="7" min="0" max="10" step="1" />` gives `Value` 7, and `value="15"` gives 10). `ATextFieldTakesCapacityAndText` (`capacity="8" text="abc"` gives `Capacity` 8 and the bytes `abc`). `AScrollViewTakesScrollSpeedAndScrollOffset` (`scroll-speed="60"` gives 60, no attribute gives 40, `scroll-speed="0"` gives 40, and `scroll-offset="0 30"` reads (0, 0) on a new tree, as `ScrollOffset(new float2(0, 30))` of the builder does). `AnImageTakesTextureUvTintAndScaleMode` (a map with `icon` = `new TextureHandle(7, new int2(16, 16))`, `uv="0 0 0.5 0.5"`, `tint="#ff000080"`, `scale-mode="scale-to-fit"`). `TheLoaderCopiesTheTextureMap` (a change of the map after the constructor changes no load).
- [ ] 2.8 Write `tests/BrutalUi.Markup.Tests/Template/TemplateValueTests.cs` with 10 tests. Each fault asserts the kind, the line, the column of the attribute name, and `Root` `null`. `ABooleanIsTrueOrFalseOnly`. `ANumberReadsInTheInvariantCulture` (under `de-DE`, `0.5` reads 0.5. `0,5`, `1e3`, `NaN`, and a 50-digit number give `InvalidValue`). `AListNeedsTheRightCount` (3 numbers in `uv` and 3 in `scroll-offset` give `InvalidValue`, and whitespace at both ends is valid). `AColorTakesThreeSixOrEightHexDigits` (`#fff`, `#1E222C`, and `#1e222c80` read. `#ffff`, `fff`, and `#ggg` give `InvalidValue`). `AScaleModeTakesTheThreeKeywords`. `ACapacityIsAWholeNumberAboveZero` (`0`, `-1`, `1.5`, and `x` give `InvalidValue`). `ASliderRangeIsChecked` (`min="5" max="1"` gives `InvalidValue` at `max`, and `step="-1"` gives it at `step`, with no exception). `AScrollSpeedIsZeroOrMore` (`-1` gives `InvalidValue`). `AnUnknownTextureNameIsAnError` (`UnknownTexture`, also with no map). `AMissingRequiredAttributeIsAnError` (a `TextField` with no `capacity` and a `Slider` with no `max` each give `MissingAttribute` at the element name).
- [ ] 2.9 Write `tests/BrutalUi.Markup.Tests/Template/EightTypeTreeTests.cs` with 2 tests. `AnEightTypeTemplateEqualsTheBuilderTree` (a root `Element` with each base row, and under it one element of each other type with each of its rows. The builder tree uses the fluent methods and create-info fields only. `TreeCompare` gives `null`). `TheAttributeOrderOfTheSourceDoesNotChangeTheTree` (the same template with the attributes of each element in reverse order gives a tree that equals the first).
- [ ] 2.10 Write `tests/BrutalUi.Markup.Tests/Template/LoadedTreeAllocationTests.cs` with 2 tests in the style of `ParsedSheetAllocationTests`. Load one template with a root and 6 panels. Each panel holds one `Label`, `Button`, `Toggle`, `Slider`, `TextField`, `Image` (texture `icon` from the map), and `ScrollView` with one `Label` child. Add the root to a `UiContext` with a `RecordingRenderer` as the measurer. The measured step toggles the hover of one panel, writes the value of one slider, sets the text of one label from one of two `u8` constants, runs `Update(0.016f)`, and runs `Draw(renderer)`. `ALoadedTreeAllocatesNothingInUpdateAndDraw` (16 warm-up steps, `AllocationGate.ReleaseAllocationContext()`, 100 measured steps, a difference of 0 bytes). `TheMeasuredStepsDoRealWork` (the step count reads 116, and the last frame holds a text call, an image call with the texture id 7, and a rect call).
- [ ] 2.11 Edit `docs/technical-design.md`. In section 11, add The Written Table and The Value Forms of this plan, and add the phase 2 members to the loader API block. In section 14, add one bullet for `LoadedTreeAllocationTests` in the style of the other allocation bullets. Run the build and the tests, and expect Markup +23. Run three plants. Plant 3: the `Slider` factory passes `Maximum = minimum`. Expect `ASliderTakesTheRangeBeforeTheValue` and `AnEightTypeTemplateEqualsTheBuilderTree` to fail. Plant 4: `TryParseNumber` uses `CultureInfo.CurrentCulture`. Expect `ANumberReadsInTheInvariantCulture` to fail. Plant 5: `GC.KeepAlive(new object());` as the first line of the paint method of `src/BrutalUi.Core/Elements/Image.cs` (line 221 area). Expect `ALoadedTreeAllocatesNothingInUpdateAndDraw` to fail. Record the counts and the plants in `context.md`.

**Estimated Steps:** about 95 (11 tasks, about nine steps for one task)

**Files (group 1, 6):** `TemplateValues.cs`, `AttributeContext.cs`, `TemplateLoader.cs`, `TemplateReader.cs`, `BuiltInElements.cs`, `context.md`
**Files (group 2, 6):** `BuiltInTypeTests.cs`, `TemplateValueTests.cs`, `EightTypeTreeTests.cs`, `LoadedTreeAllocationTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 23 new Markup tests pass, and each existing test passes with no edit.
- [ ] Plants 3, 4, and 5 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`. `git diff --stat -- src/BrutalUi.Core` gives no output after the restore.
- [ ] `ALoadedTreeAllocatesNothingInUpdateAndDraw` measures 0 bytes.

### Phase 3: The Positioned Errors
**Goal:** The loader reports each case of A8 with a position: an element or an attribute outside the namespace, text that is not whitespace, a child inside a leaf. The messages list the valid names. The loader records each error and continues, sorts the errors in source order, and stops at XML that is not well formed (A6).
**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*TemplateErrorTests'` and reads a total of 12 with `failed: 0`. A template with three faults gives three errors, each with its line and its column.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (one group, 5 files):**
- [ ] 3.1 Edit `src/BrutalUi.Markup/Template/TemplateReader.cs`. Add steps 1 and 2 of The Load Walk. In step 4, a prefixed attribute that is not a namespace declaration gives `UnknownAttribute` with the fix "write the attribute with no prefix". Text and CDATA nodes give `UnexpectedText` at the node. A `SignificantWhitespace` node gives no error.
- [ ] 3.2 Edit `src/BrutalUi.Markup/Template/TemplateReader.cs`. Clamp the position of an `XmlException` to 1 or more. Sort the errors at the end with a stable sort by line, then column (a `List` with an index tie-break). The message of `NotWellFormed` is `The template is not well-formed XML. ` followed by `XmlException.Message`.
- [ ] 3.3 Edit `src/BrutalUi.Markup/Template/TemplateLoader.cs`. Add `internal string TypeNames`: the registered names, sorted ordinal, joined with `, `, built again after each `Register`. The reader uses it in the `UnknownElement` message, and uses `RegisteredType` names in the `UnknownAttribute` message, as The Messages shows.
- [ ] 3.4 Write `tests/BrutalUi.Markup.Tests/Template/TemplateErrorTests.cs` with 12 tests. `AnUnknownElementIsAnErrorAtItsName` (`<ui:Panel>` gives `UnknownElement` at the `u` of `ui:Panel`, and the message holds `Element`). `AnElementOutsideTheNamespaceIsAnError` (`<Label>` with no namespace, and `<x:Label xmlns:x="other">`). `AnAttributeOutsideTheNamespaceIsAnError` (`x:name` with another namespace, and `ui:name`, each give `UnknownAttribute`). `AnAttributeOfAnotherTypeIsAnUnknownAttribute` (`<ui:Label checked="true" />`, and the message lists the attributes of `Label`). `TextBetweenElementsIsAnError` (text and CDATA give `UnexpectedText`. Whitespace, a comment, and a processing instruction give no error). `AChildInsideALabelOrAnImageIsAnError` (`UnexpectedChild` at the child name). `TheLoaderRecordsEachErrorAndContinues` (one template with an unknown attribute on line 2, a bad number on line 3, and an unknown element on line 4 with a bad child on line 5: four errors in source order, `Root` `null`). `XmlThatIsNotWellFormedStopsTheRead` (an unknown attribute on line 2 and a wrong end tag on line 3: two errors, the second `NotWellFormed`, and no error after it). `ADtdIsAnError` (a `<!DOCTYPE>` with an entity gives `NotWellFormed`, and no entity text reaches a label). `AnEmptyTemplateIsAnError` (an empty span gives one `NotWellFormed` at line 1 or more and column 1 or more). `TheDefaultNamespaceFormLoads` (`<Element xmlns="brutal-ui"><Label text="a" /></Element>` equals the prefixed form). `AnErrorGivesItsTextAndGetRootOrThrowListsEachError` (`ToString` gives `hud.xml(2,14): UnknownAttribute: ...`, and `GetRootOrThrow` throws `InvalidOperationException` with one line for each error, joined by LF).
- [ ] 3.5 Edit `docs/technical-design.md` section 11. Add **The error report**: the continue rule, the position rule, the column rule, the namespace rule, and a table with each kind and its causes. Add one example line of `ToString`.
- [ ] 3.6 Run the build and the tests, and expect Markup +12. Run three plants. Plant 6: the reader returns after the first error. Expect `TheLoaderRecordsEachErrorAndContinues` to fail. Plant 7: `DtdProcessing.Ignore` in the settings. Expect `ADtdIsAnError` to fail. Plant 8: step 2 does not check the leaf flag. Expect `AChildInsideALabelOrAnImageIsAnError` to fail. Record the counts and the plants in `context.md`.

**Estimated Steps:** about 50 (6 tasks, about eight steps for one task)

**Files (5):** `TemplateReader.cs`, `TemplateLoader.cs`, `TemplateErrorTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 12 new Markup tests pass, and each existing test passes with no edit.
- [ ] Plants 6, 7, and 8 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] Each of the 8 kinds of `TemplateErrorKind` has a test that asserts the kind, the line, and the column (`grep -c "TemplateErrorKind\.<Kind>" tests/BrutalUi.Markup.Tests/Template/*.cs` gives 1 or more for each kind).

### Phase 4: Custom Types and the Hook
**Goal:** `Register` checks each entry and rejects a duplicate. A game registers a custom type with a factory, a create row, and a set row, and the base rows apply to it. Each loader has its own registry and its own handlers (A1). The hook runs after the rows, in the order of the add, and a handler reports an error with the position of its attribute (A9).
**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*CustomTypeTests'` and reads a total of 6 with `failed: 0`. A template with a custom `Gauge` element loads and equals the builder tree.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (one group, 6 files):**
- [ ] 4.1 Edit `src/BrutalUi.Markup/Template/TemplateLoader.cs`. Complete the checks of `Register`: `Name` is a valid XML local name (`XmlConvert.VerifyNCName`, with the `XmlException` turned into `ArgumentException`), `Create` is not `null`, each row has a valid name, a `Set` row has a setter, no two rows have one name, no own row has the name of a base row, and no registered type has the name. Each fault throws `ArgumentException` with `ParamName` `info` and a message that names the fault, and registers nothing. `AddAttributeHandler` throws `ArgumentNullException` with `ParamName` `handler`.
- [ ] 4.2 Edit `src/BrutalUi.Markup/Template/AttributeContext.cs`. `ElementCreateArgs.TryGet` throws `ArgumentException` for a name that is not a create row of the type, so a factory cannot read a set attribute.
- [ ] 4.3 Write `tests/BrutalUi.Markup.Tests/Template/CustomTypeTests.cs` with 6 tests. Declare `private sealed class Gauge : Element` with a constructor that takes a segment count and a `float Level` property. `ACustomTypeLoadsWithItsFactoryAndItsRows` (`segments` CreateRequired, `level` Set. `<ui:Gauge name="g" class="hud" segments="4" level="0.5" />` gives a `Gauge` with each value, and `TreeCompare` with `new Gauge(4) { Level = 0.5f }.Name("g").Class("hud")` gives `null`). `RegisterChecksTheTypeInfo` (an empty name, `a b`, a `null` factory, a `Set` row with no setter, a duplicate row, a row named `class`, `Label` again, and `Gauge` two times: each throws, and a failed `Register` leaves no type). `EachLoaderHasItsOwnRegistry` (loader A has `Gauge` and a handler, and loader B gives `UnknownElement` and `UnknownAttribute`). `AFactoryErrorGoesToTheReport` (the factory reads `segments`, adds `InvalidValue` on its context for 0, and returns `null`: one error at the position of `segments`). `AFactoryThatReturnsNullWithNoErrorThrows` (`InvalidOperationException`). `ACustomLeafTypeRejectsAChild` (`IsLeaf = true` gives `UnexpectedChild`).
- [ ] 4.4 Write `tests/BrutalUi.Markup.Tests/Template/AttributeHookTests.cs` with 5 tests. `TheHookRunsAfterTheRowsOfTheElement` (a handler for `probe` on a `Slider` reads the name, the classes, and the value that the rows set). `TheHandlersRunInTheOrderOfTheAdd` (the first returns `false`, the second returns `true`, the third never runs). `AHandlerErrorHasThePositionOfTheAttribute` (a handler that calls `AddError(InvalidValue, ...)` and returns `true` gives one error at the position of its attribute, and `Root` `null`). `TheHookNeverSeesAnAttributeWithARow` (a handler that counts calls sees no `name` and no `text`). `AddAttributeHandlerChecksNull`.
- [ ] 4.5 Edit `docs/technical-design.md` section 11. Add **Custom element types** (the `Register` call with the `Gauge` example, the base rows, the checks, the copy rule) and **The attribute hook** (the order, the handled rule, `AddError`, and the line that binding-attributes registers `command` and `bind-text` here). Run the build and the tests, and expect Markup +11. Run three plants. Plant 9: the handlers run in reverse order. Expect `TheHandlersRunInTheOrderOfTheAdd` to fail. Plant 10: `Register` replaces a type with the same name. Expect `RegisterChecksTheTypeInfo` to fail. Plant 11: step 8 runs before step 7. Expect `TheHookRunsAfterTheRowsOfTheElement` to fail. Record the counts and the plants in `context.md`.

**Estimated Steps:** about 45 (5 tasks, about nine steps for one task)

**Files (6):** `TemplateLoader.cs`, `AttributeContext.cs`, `CustomTypeTests.cs`, `AttributeHookTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 11 new Markup tests pass, and each existing test passes with no edit.
- [ ] Plants 9, 10, and 11 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] `grep -n "Register(" src/BrutalUi.Markup/Template/BuiltInElements.cs` shows that the built-in types use the public `Register` call.

### Phase 5: Localized Text
**Goal:** `text-key` gives a localized text for `Label`, `Button`, and `TextField` by the rule of A4. `TreeCompare` compares `LocalizedText` by its id and its template text, so a loaded tree with `text-key` equals a builder tree with a new `LString`. The changelog gets one line.
**Demo:** the developer runs `BrutalUi.Markup.Tests --filter-class '*LocalizedTextTests'` and reads a total of 6 with `failed: 0`. `<ui:Label text-key="hud.score" text="Score" />` gives a label with the key `hud.score` and the default text `Score`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the code and the tests, 5 files):**
- [ ] 5.1 Edit `src/BrutalUi.Markup/Template/BuiltInElements.cs`. Add `text-key` (Create) to `Label`, `Button`, and `TextField` after `text`. Add one private helper that reads `text` and `text-key` from the create args by the rule of A4 and gives a `string?` or an `LString?`. An empty key gives `InvalidValue` at `text-key`. The `Label` and `Button` factories set `LocalizedText` and leave `Text` `null`, so the create info never holds both. The `TextField` factory makes the field with `Capacity` and calls `SetText(LString)`.
- [ ] 5.2 Edit `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs`. `LocalizedDiff` takes `LString?` values. Two values are equal when they are the same reference, or when both are set with the same `Id.ToString()` and the same `Template` (ordinal). The message names the key or the template that differs. Change the remarks line: the helper compares `LocalizedText` by its id and its template, because a loaded tree and a builder tree make two `LString` objects.
- [ ] 5.3 Edit `tests/BrutalUi.Core.Tests/Builder/TreeCompareTests.cs`. Add `ALocalizedTextDifferenceGivesThePath` (+1): two labels with `new LString("a", "t")` compare equal. A key difference, a template difference, and one `null` each give a message with the path.
- [ ] 5.4 Write `tests/BrutalUi.Markup.Tests/Template/LocalizedTextTests.cs` with 6 tests. `TextKeyWithTextGivesALocalizedTextWithTheDefault` (`LocalizedText.Id.ToString()` is `k`, `Template` is `t`, and the text bytes are `t`). `TextKeyWithNoTextGivesAnEmptyDefault`. `TextKeyWorksOnAButtonAndATextField` (the field text bytes are the template text, cut at the capacity). `AnEmptyTextKeyIsAnError` (`InvalidValue` at `text-key`). `ALocalizedTemplateEqualsTheBuilderTree` (a label and a button with `text-key` against `.Text(new LString("k", "t"))`. `TreeCompare` gives `null`). `ALoadedLocalizedTextFollowsARefresh` (after `label.LocalizedText.Localize("x")` and `UiContext.RefreshLocalizedText()`, the label bytes are `x`).

**Tasks (group 2, the docs and the close, 3 files):**
- [ ] 5.5 Edit `docs/technical-design.md` and `CHANGELOG.md`. In section 11, add `text-key` to The Written Table and the rule of A4 to The Value Forms. In `CHANGELOG.md`, under `## [Unreleased]`, add one line tagged `(markup/xml-element-loader)`: the XML template loader with the type registry, the written table for the eight built-in types, the attribute hook, `text-key`, and the errors with a line and a column. Run the build and the solution tests, and expect Markup +6, Core +1, and the solution 3091 with 3 skipped (from the baseline of `context.md`). Run two plants. Plant 12: the helper of task 5.1 ignores `text`. Expect `TextKeyWithTextGivesALocalizedTextWithTheDefault` to fail. Plant 13: `LocalizedDiff` returns `null` always. Expect `ALocalizedTextDifferenceGivesThePath` to fail. Run the greps of the Verification Method of the Definition of Done. Record the counts, the plants, and the grep results in `context.md`.

**Estimated Steps:** about 40 (5 tasks, about eight steps for one task)

**Files (group 1, 5):** `BuiltInElements.cs`, `TreeCompare.cs`, `TreeCompareTests.cs`, `LocalizedTextTests.cs`, `context.md`
**Files (group 2, 3):** `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 new Markup tests and the 1 new Core test pass, and each existing test passes with no edit. Each Framework test that links `TreeCompare.cs` passes.
- [ ] Plants 12 and 13 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 The XML name of the plain container: `<ui:Element>` only. Section 11 changes from `<ui:Panel>` to `<ui:Element>`.
- Q2 The create-time values: the factory of a type reads its create attributes before it makes the element. `capacity` is a required attribute of `TextField`, and `scroll-speed` is optional on `ScrollView`.
- Q3 The texture of an `Image`: the loader create info holds a map from a name to a `TextureHandle`. `texture="icon"` reads the map, and an unknown name is an error.
- Q4 Localized text: the feature adds `text-key` now, for `Label`, `Button`, and `TextField`.
- Q5 Inline style: a template sets no inline style in this feature.
- Q6 The demo of phase 1: the developer runs the tests, and the section 11 template loads and equals the builder tree.
- A1 The loader is an instance class with a `CreateInfo`. Each instance holds its own type registry and its own hook handlers, and it registers the eight built-in types itself.
- A2 An attribute name is the builder method name in kebab-case: `name`, `class`, `pickable`, `focusable`, `text`, `checked`, `value`, `texture`, `uv`, `tint`, `scale-mode`, `scroll-offset`. `Range` becomes `min`, `max`, and `step`.
- A3 The 20 delegate methods get no row. Panel code adds each handler after the load, with `Find`.
- A4 `text-key="k"` with `text="t"` gives `new LString("k", "t")`. `text-key` with no `text` uses an empty default text.
- A5 `class` holds a list with whitespace between the names. `uv` and `scroll-offset` hold numbers with whitespace between them. `tint` uses the `#rgb`, `#rrggbb`, and `#rrggbbaa` forms of the stylesheet.
- A6 The loader records each error and continues, in the manner of the stylesheet parser. XML that is not well formed stops the read with one error. One error or more gives a `null` root.
- A7 The line and the column come from `IXmlLineInfo` of `XmlReader`. The loader prohibits a DTD.
- A8 These cases are errors: an element or attribute outside the `brutal-ui` namespace, text between elements that is not whitespace, a child inside a `Label` or an `Image`, and a missing required create attribute.
- A9 A hook handler gets the element, the attribute name, and the value, and it returns handled or not handled. The loader calls the hook after the table rows of the element. An attribute that no handler takes is an `UnknownAttribute` error.
- A10 The result type and the error type follow `StyleParseResult` and `StyleParseError`: a new error struct and a new kind enum in `BrutalUi.Markup`, each error with a kind, a line, a column, a message, and a source name.

### Decision 1: A row list for each type, with create rows and set rows
**Choice:** `ElementTypeInfo` holds the name, an `ElementFactory`, and an `AttributeRow[]`. A row is `Set` (a static `AttributeSetter`), `Create`, or `CreateRequired` (the factory reads it through `ElementCreateArgs.TryGet`). The loader runs the factory, then the set rows in table order, then the hook.
**Rationale:** This is the Chosen Approach. The rows are plain data, so element-generator can write them and a game can write them by hand. The loader owns the order (`value` after the range), the required check, the duplicate checks, and the list of valid names for a message. The `CreateRequired` kind makes Q2 and A8 a data rule that the loader checks before the factory runs.
**Alternatives:** One apply method with a switch for each type (the user rejected it). A typed `ElementMarkup<T>` class for each type (the user rejected it).

### Decision 2: The name `TemplateLoader` and the `Template*` prefix
**Choice:** `TemplateLoader`, `TemplateLoadResult`, `TemplateError`, and `TemplateErrorKind`, in the namespace `BrutalUi.Markup`, in the folder `Template/`.
**Rationale:** The requirements and the sibling features call the input a template ("a panel points to a template by name"). The four names follow the four names of the stylesheet parser. The prefix does not clash with `ElementType`, `ElementBuilder`, or the record loader of definition-loader.
**Alternatives:** `ElementLoader` (close to the Core types `ElementType` and `ElementList`). `MarkupLoader` (definition-loader also loads markup).

### Decision 3: The context types are `readonly ref struct`, passed by value
**Choice:** `AttributeContext` and `ElementCreateArgs` are `readonly ref struct` types. Each delegate takes them by value. Each holds a reference to the reader of one load and an index.
**Rationale:** A setter, a factory, and a handler need the value, the position, the typed reads, and `AddError`. A `ref struct` cannot go into a field or a closure, so a handler cannot keep a context after its load ends. The struct holds no `ref` field and no span, so a factory can get one through `out` from `TryGet`. A delegate parameter of a `ref struct` type is valid C#, and a `static` lambda infers it.
**Alternatives:** A class that the loader reuses (a handler can keep it and write into a later load). A `ref` parameter as the sketch shows (the context holds no state to change, so `ref` adds nothing).

### Decision 4: The hook handler gets the name and the value as arguments
**Choice:** `AttributeHandler(Element element, string name, string value, AttributeContext context)` returns `bool`. A setter is `AttributeSetter(Element element, AttributeContext context)`.
**Rationale:** A9 gives the handler the element, the name, and the value. The arguments make that contract visible in the signature that binding-attributes implements. A setter belongs to one row, so it knows its name, and the context gives the value.

### Decision 5: Four base rows apply to each type
**Choice:** `name`, `class`, `pickable`, and `focusable` live in `BuiltInElements.BaseRows`. Each registered type gets them before its own rows. An own row with a base name is a `Register` error.
**Rationale:** Each element type is an `Element`, and the four builder methods are generic on `Element`. A custom type gets the four rows with no work. One place holds them, so element-generator replaces them with the rest of the written table.

### Decision 6: The text rows are create rows
**Choice:** `text` and `text-key` are `Create` rows of `Label`, `Button`, and `TextField`.
**Rationale:** A4 joins two attributes into one `LString`. A set row sees only its own value, so the join needs the factory. The create info of `Label` and `Button` holds `Text` and `LocalizedText`, and the factory sets one of them. The tree equals a builder tree with `Text(...)`, because `TreeCompare` compares the bytes.

### Decision 7: A small hex reader in `TemplateValues`, and no change to `Style/`
**Choice:** `TemplateValues.TryParseColor` reads a `ReadOnlySpan<char>` with the digit rules of `StyleValues.TryReadColor`.
**Rationale:** `StyleValues.TryReadColor` reads a `StyleToken` over UTF-8 bytes, and an attribute value is a `string`. A shared helper needs a refactor of the stylesheet parser and its tests. About 20 lines of copy keep `Style/` unchanged. The copy is a tech debt item for `/review-epic` (Notes item 6).
**Alternatives:** Tokenize the attribute value with `StyleTokenizer` (it reports errors with stylesheet positions and kinds).

### Decision 8: The reader settings
**Choice:** `DtdProcessing.Prohibit`, `XmlResolver = null`, `IgnoreComments`, `IgnoreProcessingInstructions`, `IgnoreWhitespace`, `ConformanceLevel.Document`. The span overload copies the bytes into a `MemoryStream`.
**Rationale:** A7 prohibits a DTD, so no entity expansion and no external read can run. `XmlReader` detects the encoding from the BOM or the declaration, and UTF-8 is its default. The copy is a load-time allocation (A10 of the epic). The `string` overload encodes to UTF-8, as `StyleSheetParser.Parse(string)` does, so both overloads run one path.

### Decision 9: A failed element still reads its subtree
**Choice:** After an unknown element, a child of a leaf, a missing required attribute, or a factory that returns `null`, the reader reads the subtree with a `null` parent. Each child reports its own errors and attaches nothing.
**Rationale:** A6 asks the loader to record each error and continue. A skip of the subtree hides the faults inside it, and the author then fixes one fault for each run.

### Decision 10: `TreeCompare` compares `LocalizedText` by value
**Choice:** Two `LocalizedText` values are equal when they are the same reference, or when both have the same id text and the same template text.
**Rationale:** A loaded tree and a builder tree each make their own `LString`, so a reference compare can never pass. The change only adds equal cases, so each builder-api test keeps its result. A new Core test pins the rule.
**Alternatives:** Assert the key and the template in each test and keep the reference compare (the tree compare then skips the text of a localized label).

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | `IXmlLineInfo` gives another column than the plan expects (for example the `<` and not the name). | The position asserts fail. | Task 1.10 checks one known position (line 3, column 59) and records the rule in `context.md`. The later tests compute each column from the same rule. The doc states the rule that the test proves. |
| 2 | A `ref struct` parameter in a delegate or an `out` of a `ref struct` from `TryGet` does not compile with an analyzer or a scope rule. | Phase 1 stops. | The struct holds no `ref` field and no span, so its scope is the caller. When the compiler rejects it, use a `readonly struct` with the same members and record the change in `context.md`. Phase 1 runs on an advanced agent. |
| 3 | element-generator replaces the table, and a test reads an internal of the table. | The tests do not pass unchanged. | The tests use the public API and `TreeCompare` only. The written table is one file, and `Register` stays public. |
| 4 | A built-in factory throws for a bad value. | `Load` throws for the text. | Each factory checks the range before it calls the constructor. `TemplateValueTests` loads each bad value and asserts an error and no exception. |
| 5 | An XML attack (an entity expansion or an external read). | Memory use or a file read at load time. | `DtdProcessing.Prohibit` and `XmlResolver = null`. `ADtdIsAnError` and plant 7 guard it. |
| 6 | The `TreeCompare` change breaks a builder-api test or a Framework test. | A regression. | The change only adds equal cases. Task 5.5 runs the full solution. |
| 7 | A failed load interns class names in the global class table. | The table grows by the names of a bad template. | The growth is at load time and small. The stylesheet parser avoids it, but the loader creates the elements during the walk. Notes item 11 records it. |
| 8 | `scroll-offset` has no effect on a new tree. | An author expects an offset. | The row follows the builder method (A2), and the doc states the clamp rule. |
| 9 | `BRUTALCORE0011` rejects a literal in `Load(string, string)`. | A test does not compile. | The tests pass `u8` to the span overload. The `string` test passes a local. |
| 10 | A test changes the culture, and xUnit runs classes in parallel. | Another test reads a wrong culture. | `CultureInfo.CurrentCulture` is local to the thread and the async flow. The test restores it in `finally`. |

---

## Dependencies

- builder-api (complete): the 43 builder methods, `Find`, and `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs`.
- style-system (complete): `StyleParseError` and `StyleParseResult` as the model, and `StyleValues.TryReadColor` as the color rule.
- core-tree and interactive-elements (complete): `Element`, the seven widget types with their create infos, `ClassName`, `TextureHandle`, and `UiContext`.
- `Ahwoo.BRUTAL.Localization` through Core: `LString(string id, string templateString)`, `Id`, and `Template`.
- `System.Xml` of the shared framework: `XmlReader`, `XmlReaderSettings`, `IXmlLineInfo`, `XmlException`, and `XmlConvert`. No new package, no native dependency, and no project reference.
- The test project gets two `Compile` links: `TreeCompare.cs` and `RecordingRenderer.cs`.
- element-generator, binding-attributes, and definition-loader depend on this feature.

---

## Definition of Done

### Functional Criteria
- [ ] `src/BrutalUi.Markup/Template/` holds the 8 files of the File Tree, and they declare the 13 public types of the Public API with the same names and member signatures.
- [ ] `new TemplateLoader().Load(bytes, "hud.xml")` on the section 11 template of `technical-design.md` with a hook handler for `command` gives a root that equals the builder tree under `TreeCompare.FirstDifference`.
- [ ] A template with one element of each of the eight built-in types and each row of The Written Table equals the builder tree. The order of the attributes in the source does not change the tree.
- [ ] `capacity` on `TextField` and `max` on `Slider` are required. A missing one gives `MissingAttribute` at the element name. `scroll-speed`, `min`, `step`, and `texture` are optional.
- [ ] `texture="icon"` reads `CreateInfo.Textures`. A name that the map does not hold gives `UnknownTexture`. A change of the map after the constructor changes nothing.
- [ ] `text-key="k" text="t"` gives `LocalizedText` with the id `k` and the template `t`. `text-key` alone gives the template `""`. This works on `Label`, `Button`, and `TextField`.
- [ ] A custom type that a game registers with `Register` loads with its factory, its create rows, its set rows, and the four base rows.
- [ ] An attribute with no row goes to the handlers of `AddAttributeHandler` in the order of the add, after the rows of the element. An attribute that no handler takes gives `UnknownAttribute`.

### Quality Criteria
- [ ] Each of the 8 kinds of `TemplateErrorKind` has at least one test that asserts the kind, the line, and the column.
- [ ] A template with more than one fault gives one error for each fault, sorted by line and column, and `Root` is `null`. XML that is not well formed adds one `NotWellFormed` error and stops the read.
- [ ] `Load` throws no exception for any template text in the tests with the built-in types. It throws `ArgumentNullException` for a `null` source name or text. `Register` throws `ArgumentException` for each fault of task 4.1.
- [ ] `ALoadedTreeAllocatesNothingInUpdateAndDraw` measures 0 bytes over 100 steps of `Update` and `Draw`, and plant 5 fails it.
- [ ] Each existing test passes with no edit. The totals grow by 58 in Markup and 1 in Core from the baseline of `context.md`.
- [ ] The build gives `0 Warning(s)` and `0 Error(s)`. Each new public member has an XML doc comment. Each file under `src/BrutalUi.Markup/Template/` has fewer than 400 lines.

### Integration Criteria
- [ ] `src/BrutalUi.Markup/BrutalUi.Markup.csproj` references `BrutalUi.Core` only, and does not change.
- [ ] `src/BrutalUi.Markup/Template/` uses no `System.Reflection`, no `Activator`, no `Enum.Parse`, and no `GetType()`.
- [ ] `src/BrutalUi.Core/`, `src/BrutalUi.Framework/`, `src/BrutalUi.Markup/Style/`, and `samples/` do not change.
- [ ] `BuiltInElements.cs` holds the whole written table and calls the public `Register`, so element-generator can replace the file. `AddAttributeHandler`, `AttributeContext.AddError`, and `TemplateError` are public, so binding-attributes can use them from Framework.
- [ ] Section 11 of `technical-design.md` shows `<ui:Element>` (no `<ui:Panel>`), the loader API, The Written Table, The Value Forms, the error table, custom types, and the hook. Section 14 names `LoadedTreeAllocationTests`. `CHANGELOG.md` has one line tagged `(markup/xml-element-loader)`.

### Verification Method
1. Run `export DOTNET_CLI_HOME=/tmp/dui-cli-home MSBUILDDISABLENODEREUSE=1; dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)`.
2. Run `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and 3 skipped, and a total 59 above the baseline of `context.md` (3091 at planning time).
3. Run `tests/BrutalUi.Markup.Tests/bin/Debug/net10.0/BrutalUi.Markup.Tests --filter-class '<Class>' 2>&1 | tail -n 5` for each class. Expect `failed: 0` and these totals: `*SectionElevenTemplateTests` 6, `*BuiltInTypeTests` 9, `*TemplateValueTests` 10, `*EightTypeTreeTests` 2, `*LoadedTreeAllocationTests` 2, `*TemplateErrorTests` 12, `*CustomTypeTests` 6, `*AttributeHookTests` 5, `*LocalizedTextTests` 6.
4. Run `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*TreeCompareTests' 2>&1 | tail -n 5`. Expect a total of 6 with `failed: 0`.
5. Run `git diff --stat cf13f19 -- src/BrutalUi.Core src/BrutalUi.Framework src/BrutalUi.Markup/Style src/BrutalUi.Markup/BrutalUi.Markup.csproj samples Directory.Packages.props`. Expect no output.
6. Run `grep -rnE 'System\.Reflection|Activator|Enum\.Parse|GetType\(\)' src/BrutalUi.Markup/Template`. Expect no output.
7. Run `git diff cf13f19 -- src | grep -E '^\+.*public (sealed |static |readonly |ref |abstract )*(partial )*(class|struct|interface|enum|record|delegate) '`. Expect 13 lines: `TemplateLoader`, `CreateInfo`, `TemplateLoadResult`, `TemplateError`, `TemplateErrorKind`, `ElementTypeInfo`, `AttributeRow`, `AttributeRowKind`, `ElementFactory`, `AttributeSetter`, `AttributeHandler`, `AttributeContext`, and `ElementCreateArgs`.
8. Run `wc -l src/BrutalUi.Markup/Template/*.cs`. Expect 8 files, each under 400 lines.
9. Run `grep -n 'ui:Panel' docs/technical-design.md`. Expect no output. Run `grep -n 'LoadedTreeAllocationTests' docs/technical-design.md`. Expect a line in section 14. Run `grep -n 'markup/xml-element-loader' CHANGELOG.md`. Expect one line.
10. For each kind in `NotWellFormed UnknownElement UnknownAttribute InvalidValue MissingAttribute UnknownTexture UnexpectedText UnexpectedChild`, run `grep -l "TemplateErrorKind.<Kind>" tests/BrutalUi.Markup.Tests/Template/*.cs`. Expect at least one file for each kind.
11. Compare the section 11 template of `docs/technical-design.md` with the template text of `TheSectionElevenTemplateEqualsTheBuilderTree`. Expect the same text.
12. Read `context.md`. Expect plants 1 to 13, each with a non-zero exit code, 0 `error CS` lines, and the expected failed test names.

---

## Testing Strategy

- **Tree tests (Markup, 5, inside the classes below):** `TheSectionElevenTemplateEqualsTheBuilderTree`, the two `EightTypeTreeTests`, `ACustomTypeLoadsWithItsFactoryAndItsRows`, and `ALocalizedTemplateEqualsTheBuilderTree` compare a loaded tree with a builder tree through `TreeCompare`. They prove requirements line 19.
- **Row tests (Markup, 15):** `BuiltInTypeTests` reads back each row of each type. `LocalizedTextTests` covers `text-key`.
- **Value and error tests (Markup, 22):** `TemplateValueTests` covers each value form and each range check. `TemplateErrorTests` covers each case of A6, A7, and A8, and the text form of an error.
- **API tests (Markup, 11):** `CustomTypeTests` and `AttributeHookTests` cover `Register`, the per-loader registry, the factory rules, and the hook order.
- **Allocation gate (Markup, 2):** `LoadedTreeAllocationTests` measures `Update` and `Draw` on a loaded tree with all eight types, with a real-work test so the measured body cannot be empty.
- **Core test (1):** `ALocalizedTextDifferenceGivesThePath` pins the new compare rule of `TreeCompare`.
- **Plants (13):** each phase plants a fault and proves that a named test fails. The plants guard the hook, the class list, the slider range, the culture, the gate, the continue rule, the DTD rule, the leaf rule, the handler order, the duplicate check, the hook order, the `text-key` join, and the localized compare.
- **No regressions:** each existing test passes with no edit. The stylesheet tests guard `Style/`.
- **No manual check:** no window, no sample change, and no Needs Human Verification item.

| Class | Project | Phase | Tests |
|---|---|---|---|
| `SectionElevenTemplateTests` | Markup | 1 | 6 |
| `BuiltInTypeTests` | Markup | 2 | 9 |
| `TemplateValueTests` | Markup | 2 | 10 |
| `EightTypeTreeTests` | Markup | 2 | 2 |
| `LoadedTreeAllocationTests` | Markup | 2 | 2 |
| `TemplateErrorTests` | Markup | 3 | 12 |
| `CustomTypeTests` | Markup | 4 | 6 |
| `AttributeHookTests` | Markup | 4 | 5 |
| `LocalizedTextTests` | Markup | 5 | 6 |
| `TreeCompareTests` | Core | 5 | +1 |

---

## Related Documentation

- `docs/features/markup/xml-element-loader/requirements.md`
- `docs/features/markup/epic-overview.md` and `docs/features/markup/epic-requirements.md`
- `docs/features/markup/builder-api/implementation.md` and `context.md` (the property surface, `TreeCompare`, and the hand-off notes)
- `docs/features/markup/element-generator/requirements.md` (the table shape and the public `Register`)
- `docs/features/markup/binding-attributes/requirements.md` (the hook and the error with a position)
- `docs/features/markup/definition-loader/requirements.md` (the reader and the template names)
- `docs/technical-design.md` section 11 (the markup format, line 1183) and section 14 (the allocation gates)
- `src/BrutalUi.Markup/Style/StyleParseError.cs`, `StyleParseResult.cs`, `StyleSheetParser.cs` (the model), and `StyleValues.cs:1058` (the color rule)
- `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs` (the gate model)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

1. **A8 and the unprefixed attribute (a reading, not a conflict).** A8 makes an attribute outside the `brutal-ui` namespace an error. In XML, an unprefixed attribute is in no namespace, and the section 11 template uses only unprefixed attributes. The plan reads an unprefixed attribute as an attribute of its element. It also rejects a prefixed attribute with the `brutal-ui` namespace (`ui:name`), so each template uses one form and no duplicate check is necessary. A8 does not say that `ui:name` is valid.
2. **A stale line in the requirements.** The Out of Scope list of `requirements.md` says that `Capacity` and `ScrollSpeed` have no table row and need a decision first. Q2 makes that decision: `capacity` and `scroll-speed` are create rows. The plan follows Q2. Task 1.1 can add a pointer to Q2 in `context.md`. The requirements file does not change in this plan.
3. **Open point: the required create rows.** Q2 names `capacity` as required and `scroll-speed` as optional. No confirmed item names `max` or `texture`. The plan makes `max` required, because `Slider.CreateInfo.Maximum` is `required`. It makes `texture` optional, because the none handle is valid. Options: (a) `max` required and `texture` optional (recommended, the plan). (b) Both optional, with `max` 0 and the none handle. (c) Both required.
4. **The column unit.** A7 takes the column from `IXmlLineInfo`. `LinePosition` counts UTF-16 units, so a character outside the BMP counts 2. `StyleParseError` counts code points. The plan keeps the value of `XmlReader` and states the unit in the doc of `TemplateError.Column`.
5. **The `command` attribute in the phase 1 demo.** The section 11 template holds `command="game.pause"`, and binding-attributes registers that attribute later. The demo test adds a test handler that adds a click handler with `OnClicked`, and the builder tree adds the same handler. `TreeCompare` compares the delegate counts, so the trees compare equal. This also proves the hook in phase 1.
6. **Tech debt for `/review-epic`.** `TemplateValues.TryParseColor` copies the digit rules of `StyleValues.TryReadColor` (Decision 7). A later refactor can share one span reader.
7. **Open point for definition-loader: the template registry.** The epic overview lists "the reader and the template registry" as the hand-off to definition-loader. The requirements of this feature ask for no registry, so the plan adds none. definition-loader states that the game registers each template by name in C#. That registry can live in Framework. Decide it in definition-loader.
8. **Open point for definition-loader: the error type.** `TemplateError` has an internal constructor, as `StyleParseError` has. Framework code adds an error through `AttributeContext.AddError` only. When definition-loader needs to make its own positioned errors, it needs its own error type or a public constructor here. Decide it in definition-loader.
9. **Open point for binding-attributes: the registry of the load.** `command` calls `BindCommand` "with the `CommandRegistry` of the load". The handler has no load argument. A handler can capture the registry when panel code adds it, and A1 lets a game make one loader for each registry. When that is not enough, binding-attributes can propose a state argument. Record the choice there.
10. **For element-generator.** The generator requirements write rows for "each public settable property". Several built-in rows have another source: `text` and `text-key` are create rows (Decision 6), `min`, `max`, and `step` are create rows for `SetRange`, `capacity` and `scroll-speed` are create-info fields, and the base rows use the A2 names (`pickable`, not `is-pickable`). The generator must write these rows with the same names and kinds, or keep them in a written file, so the tests of this feature pass unchanged.
11. **Class names of a failed load.** The loader interns each class name when it creates the element. A template with an error still adds its class names to the global table. The stylesheet parser adds no name for a sheet with an error. The plan accepts the difference, because the growth is small and at load time.
12. **Children of the other widgets.** A8 names `Label` and `Image` as leaf types. `Button`, `Toggle`, `Slider`, `TextField`, and `ScrollView` accept a child element, as the builder `Child` method does.
13. **`scroll-offset` on a new tree.** The row writes `ScrollView.ScrollOffset`. The set clamps with the layout of the last update, so a new tree keeps (0, 0). The builder method does the same, so the row keeps "markup equals code".
14. **No stream overload.** The epic requirements name "a read of a UTF-8 span or a stream". The feature requirements name a UTF-8 template only. The plan gives the span overload and the `string` overload, as `StyleSheetParser` does.
15. **The planned totals** come from the builder-api close: the solution 3032 with 3 skipped at `cf13f19`. Task 1.1 records the real baseline and the Markup and Core totals, and the acceptance criteria count the deltas from it.
