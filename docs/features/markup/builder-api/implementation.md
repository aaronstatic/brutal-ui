# Builder API - Implementation Plan

**Epic:** markup (feature 1 of 6)
**Status:** Ready for Review
**Started:** 2026-11-12
**Target Completion:** TBD
**Last Updated:** 2026-11-12 10:35

---

## Quality Bar

**Target quality:** A public C# API that sets the property surface of the markup epic. The XML attributes of xml-element-loader follow this surface row by row, so a bad name or a missing method here moves into five later features. This is an API feature, so six things carry it.

- **A consistent surface:** Each fluent method returns the element of the call with its own type. A method on a base `Element` member is generic (`where T : Element`), as `Class` and `Child` are today. A method on a widget member takes the sealed widget type. Each method checks the element for `null` with `ArgumentNullException` (parameter name `element`, or the receiver name of the widget method).
- **A 1 to 1 map from a method to a property:** Each value method writes one property or calls one setter method, and does nothing else. `Text` calls `SetText`, and `Range` calls `SetRange`. Each delegate method adds one handler to one slot with `+=` (A7). No method adds a rule that the property does not have.
- **A 0-byte query:** `Element.Find` and `Element.Find<T>` allocate 0 bytes in the default Debug build. A new gate test measures it, and a plant fails the gate.
- **No break of the shipped surface:** `Class<T>(string)`, `Class<T>(ClassId)`, `Child<T>(Element)`, and `BindCommand` keep their exact text. `SlotContainer.Find` does not change. No new public type appears (the Chosen Approach).
- **Small files:** Each `ElementBuilder` partial file stays under 300 lines.
- **Accurate docs:** The section 11 example compiles as written, and a test runs it. Section 5 and section 11 of `technical-design.md` name each method. Each public member has an XML doc comment in ASD-STE100 prose (CS1591 is an error).

---

## Executive Summary

This feature completes the C# builder API of section 11 of `technical-design.md`. Today `ElementBuilder` holds `Class` and `Child` only. A game builds a screen with object initializers and property writes.

- `ElementBuilder` becomes a `static partial class` in four files under `src/BrutalUi.Core/Tree/`: the element group, the text group, the widget group, and the event group. The feature adds 40 fluent methods in `BrutalUi.Core`: 20 value methods (12 names) and 20 delegate methods (Q2, A1, A2, A3).
- `CommandBindings` in `BrutalUi.Framework` gains `Command(registry, name)`, which calls `BindCommand` (Q1, A9).
- `Element` becomes `partial` and gains `Find(ReadOnlySpan<byte>)` and `Find<T>(ReadOnlySpan<byte>)` in a new file. The walk checks the root first, walks in tree order, and compares the UTF-8 bytes with the `string` name through the internal `Utf8Text.Utf8Equals`. It allocates nothing (A8).
- A test helper `TreeCompare` compares two trees and gives the first difference. It lives in `tests/BrutalUi.Core.Tests/Shared/`, so Framework tests and Markup tests can link it (A10).
- The section 11 example changes to a form that compiles, and a Framework test builds it and compares it with an initializer tree (Q5, A4).

The plan has 4 phases and 33 tasks, about 240 steps. Phase 1 (11 tasks, two groups) delivers `Name`, the `Text` methods of `Label` and `Button`, `Command`, `TreeCompare`, and the test of the section 11 example. Phase 2 (8 tasks, two groups) delivers the other value methods and an eight-type tree compare. Phase 3 (6 tasks) delivers the query by name and its 0-byte gate. Phase 4 (8 tasks, two groups) delivers the 20 delegate methods and the changelog line. No phase needs an advanced dev agent.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The game developer** builds a screen in one C# expression and finds an element by name in a frame with no allocation.
- [ ] **The markup author** (through xml-element-loader) writes an attribute that maps to one builder method.
- [ ] **The framework contributor** of the later markup features uses the tree compare, the `Command` method, and the query.

**User Subsets Served:**
- [ ] **Simple** - a game writes `new Label(default).Name("score").Class("hud-value").Text("0"u8)` and adds it with `Child`.
- [ ] **Complex** - a game sets the range, the value, and the change handler of a slider in one chain, adds a key handler with `KeyDown`, binds a command with `Command`, and later calls `root.Find<Label>("score"u8)` in its frame code.

**UX Approach:**
- Build time: the fluent methods run when the game builds the tree. A delegate method can allocate the handler side object one time, as `+=` on the slot does today.
- Run time: `Find` and `Find<T>` run in a frame and allocate nothing.
- No inline style method (Q4), no XML, and no selector query.

---

## Goals

### Primary Goals
1. Add one fluent method for each settable property of the eight built-in types, in the scope of A2, A3, and A6 (requirements line 12).
2. Make the section 11 example compile and pass a test (requirements line 13, Q5, A4).
3. Put the Core methods in `BrutalUi.Core` and `Command` in `BrutalUi.Framework` (requirements line 14, A1, A9).
4. Add `Find` and `Find<T>` on `Element` with 0 bytes for each call (requirements lines 15 and 16, Q3, A8).
5. Prove that a fluent build and an initializer build give the same tree (requirements line 17, A10).
6. Cover each method and the query with tests, and put the full method list into section 11 (requirements line 18).

### Success Criteria
- [ ] The phase 1 demo passes: `CommandBuilderTests` reads `failed: 0`, and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` passes.
- [ ] The phase 2 demo passes: `BuilderTreeTests` reads `failed: 0`.
- [ ] The phase 3 demo passes: `ElementFindTests` and `ElementFindAllocationTests` read `failed: 0`, and the measured bytes are 0.
- [ ] The phase 4 demo passes: `ElementBuilderEventTests` reads `failed: 0`, and a handler that the builder adds runs on a routed event.
- [ ] Each existing test passes with no edit. The planned totals at the end are Core 1839 (1801 + 38), Framework 332 (326 + 6), and the solution 3032 (2988 + 44) with 3 skipped. Task 1.1 records the real baseline, and the deltas count from it.

---

## Architecture Overview

### The Place in the Epic

builder-api is feature 1 of 6. It sets the property surface. xml-element-loader writes one table row for each value method of this feature and reuses `TreeCompare` in `tests/BrutalUi.Markup.Tests/`. element-generator later writes those rows. binding-attributes calls `Command` (or `BindCommand`) from the `command` attribute and uses `Find` in panel code. definition-loader and markup-sample do not touch this surface. HelloPanel does not change in this feature (A10).

```
BrutalUi.Core                                   BrutalUi.Framework
  Tree/ElementBuilder.cs         (element)        Commands/CommandBindings.cs
  Tree/ElementBuilder.Text.cs    (text)             BindCommand (shipped)
  Tree/ElementBuilder.Widgets.cs (widgets)          Command  -> BindCommand   (NEW)
  Tree/ElementBuilder.Events.cs  (events)
  Tree/Element.cs                (partial, NEW modifier)
  Tree/Element.Find.cs           (Find, Find<T>)  -> Elements/Utf8Text.Utf8Equals (internal, shipped)

tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs   <- linked by BrutalUi.Framework.Tests (this feature)
                                                  <- linked by BrutalUi.Markup.Tests (xml-element-loader)
```

### File Tree (end state)

```
src/BrutalUi.Core/Tree/
├── ElementBuilder.cs            # phase 1: static partial, the class remarks, Name. phase 2: Pickable, Focusable
├── ElementBuilder.Text.cs       # phase 1 (new): Text of Label and Button. phase 2: Text of TextField
├── ElementBuilder.Widgets.cs    # phase 2 (new): the 8 widget value methods. phase 4: the 6 widget delegate methods
├── ElementBuilder.Events.cs     # phase 4 (new): the 14 event methods
├── Element.cs                   # phase 3: the partial modifier and one remarks sentence
└── Element.Find.cs              # phase 3 (new): Find, Find<T>, the private walks
src/BrutalUi.Framework/Commands/
└── CommandBindings.cs           # phase 1: Command
tests/BrutalUi.Core.Tests/
├── Shared/TreeCompare.cs        # phase 1 (new): the tree compare helper, no test framework type
└── Builder/                     # new folder
    ├── TreeCompareTests.cs          # phase 1 (new): 5 tests
    ├── ElementBuilderElementTests.cs# phase 1 (new): 3 tests. phase 2: +1
    ├── ElementBuilderTextTests.cs   # phase 1 (new): 4 tests. phase 2: +1
    ├── ElementBuilderWidgetTests.cs # phase 2 (new): 6 tests. phase 4: +2
    ├── BuilderTreeTests.cs          # phase 2 (new): 2 tests. phase 4: +1
    ├── ElementFindTests.cs          # phase 3 (new): 7 tests
    ├── ElementFindAllocationTests.cs# phase 3 (new): 2 tests
    └── ElementBuilderEventTests.cs  # phase 4 (new): 4 tests
tests/BrutalUi.Framework.Tests/
├── BrutalUi.Framework.Tests.csproj  # phase 1: one Compile link to Shared/TreeCompare.cs
└── Commands/CommandBuilderTests.cs  # phase 1 (new): 5 tests. phase 4: +1
docs/technical-design.md         # phases 1 to 4: section 5 line 301, the section 5 Element block, section 11, section 14
CHANGELOG.md                     # phase 4
docs/features/markup/builder-api/context.md   # each phase
```

These files do not change: `src/BrutalUi.Markup/`, `src/BrutalUi.Glfw/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Debugger/`, `samples/`, `src/BrutalUi.Framework/Panels/SlotContainer.cs`, each widget file under `src/BrutalUi.Core/Elements/`, `Directory.Packages.props`, `brutal-ui.sln`, and `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public static partial class ElementBuilder
{
    // ElementBuilder.cs, the element group
    public static T Class<T>(this T element, string name) where T : Element;      // shipped, text unchanged
    public static T Class<T>(this T element, ClassId id) where T : Element;       // shipped, text unchanged
    public static T Child<T>(this T element, Element child) where T : Element;    // shipped, text unchanged
    public static T Name<T>(this T element, string? name) where T : Element;      // Element.Name (A5)
    public static T Pickable<T>(this T element, bool value) where T : Element;    // Element.IsPickable
    public static T Focusable<T>(this T element, bool value) where T : Element;   // Element.IsFocusable

    // ElementBuilder.Text.cs, the text group (A3)
    public static Label Text(this Label label, ReadOnlySpan<byte> utf8);          // Label.SetText(ReadOnlySpan<byte>)
    public static Label Text(this Label label, string? text);                     // Label.SetText(string?)
    public static Label Text(this Label label, LString? text);                    // Label.SetText(LString?)
    public static Button Text(this Button button, ReadOnlySpan<byte> utf8);
    public static Button Text(this Button button, string? text);
    public static Button Text(this Button button, LString? text);
    public static TextField Text(this TextField field, ReadOnlySpan<byte> utf8);
    public static TextField Text(this TextField field, string? text);
    public static TextField Text(this TextField field, LString? text);

    // ElementBuilder.Widgets.cs, the widget group
    public static Toggle Checked(this Toggle toggle, bool value);                 // Toggle.Checked
    public static Slider Value(this Slider slider, float value);                  // Slider.Value
    public static Slider Range(this Slider slider, float minimum, float maximum, float step);  // Slider.SetRange (A6)
    public static Image Texture(this Image image, TextureHandle texture);         // Image.Texture
    public static Image Uv(this Image image, float4 uv);                          // Image.Uv
    public static Image Tint(this Image image, Rgba8 tint);                       // Image.Tint
    public static Image ScaleMode(this Image image, ImageScaleMode mode);         // Image.ScaleMode
    public static ScrollView ScrollOffset(this ScrollView view, float2 offset);   // ScrollView.ScrollOffset
    public static Button OnClicked(this Button button, Action<Button> handler);           // Clicked +=
    public static Toggle OnCheckedChanged(this Toggle toggle, Action<Toggle> handler);    // CheckedChanged +=
    public static Slider OnValueChanged(this Slider slider, Action<Slider> handler);      // ValueChanged +=
    public static TextField OnTextChanged(this TextField field, Action<TextField> handler);  // TextChanged +=
    public static TextField OnSubmitted(this TextField field, Action<TextField> handler);    // Submitted +=
    public static ScrollView OnScrollChanged(this ScrollView view, Action<ScrollView> handler); // ScrollChanged +=

    // ElementBuilder.Events.cs, the event group. Each one: "where T : Element", slot += handler
    public static T PointerMove<T>(this T element, UiEventHandler<PointerEvent> handler);         // OnPointerMove
    public static T PointerMoveCapture<T>(this T element, UiEventHandler<PointerEvent> handler);  // OnPointerMoveCapture
    public static T PointerDown<T>(this T element, UiEventHandler<PointerEvent> handler);
    public static T PointerDownCapture<T>(this T element, UiEventHandler<PointerEvent> handler);
    public static T PointerUp<T>(this T element, UiEventHandler<PointerEvent> handler);
    public static T PointerUpCapture<T>(this T element, UiEventHandler<PointerEvent> handler);
    public static T Scroll<T>(this T element, UiEventHandler<ScrollEvent> handler);
    public static T ScrollCapture<T>(this T element, UiEventHandler<ScrollEvent> handler);
    public static T KeyDown<T>(this T element, UiEventHandler<KeyEvent> handler);
    public static T KeyDownCapture<T>(this T element, UiEventHandler<KeyEvent> handler);
    public static T KeyUp<T>(this T element, UiEventHandler<KeyEvent> handler);
    public static T KeyUpCapture<T>(this T element, UiEventHandler<KeyEvent> handler);
    public static T Char<T>(this T element, UiEventHandler<CharEvent> handler);
    public static T CharCapture<T>(this T element, UiEventHandler<CharEvent> handler);
}

public partial class Element
{
    public Element? Find(ReadOnlySpan<byte> name);                       // A8
    public T? Find<T>(ReadOnlySpan<byte> name) where T : Element;        // Q3
}
```

```csharp
namespace BrutalUi.Framework;

public static class CommandBindings
{
    public static Button BindCommand(this Button button, CommandRegistry registry, ReadOnlySpan<byte> name);  // shipped
    public static Button Command(this Button button, CommandRegistry registry, ReadOnlySpan<byte> name);      // NEW (Q1, A9)
}
```

The count: 43 `public static` methods in the four `ElementBuilder` files (3 shipped, 40 new), 2 `Find` methods, and 1 `Command` method.

### The Section 11 Example (end state)

```csharp
var panel = new Element().Class("hud")
    .Child(new Label(default).Name("score").Class("hud-value").Text("0"u8))
    .Child(new Button(default).Name("pause").Class("hud-button").Text("Pause"u8).Command(registry, "game.pause"u8));
```

`registry` is the `CommandRegistry` of the game. The old text `new Label { Name = "score" }` does not compile, because `Label` and `Button` have only the constructor with `in CreateInfo` (Notes item 1). The old text `.Text("0")` does not compile, because `BRUTALCORE0011` rejects a string literal when a span overload exists (A4).

### The Query

```
Find(name):           if name.IsEmpty -> null; else Walk(this, name)
Walk(e, name):        if Matches(e, name) -> e
                      for i in 0..e.Children.Count: hit = Walk(e.Children[i], name); if hit -> hit
                      null
Find<T>(name):        same walk, with Matches(e, name) && e is T
Matches(e, name):     e.Name is string s
                      && s.Length <= name.Length && name.Length <= 3 * s.Length     (a fast reject)
                      && Utf8Text.Utf8Equals(name, s)                                (no allocation)
```

- The walk is depth-first pre-order, the same order as `SlotContainer.Walk`. It checks the root first (A8). It enters the children of each element, also of a `ScrollView`.
- The compare is ordinal on the UTF-8 form of the name. `Utf8Text.Utf8Equals` (`src/BrutalUi.Core/Elements/Utf8Text.cs:373`) encodes the `string` in chunks into a stack buffer and compares the bytes. It allocates nothing. The fast reject is valid because one UTF-16 unit gives 1 to 3 UTF-8 bytes.
- An empty name gives `null`, as `SlotContainer.Find` does. An element with a `null` name never matches. An element with an empty name never matches, because an empty query returns before the walk.
- `Find<T>` skips an element of another type and walks on (Q3). It does not stop at the first name hit.
- The walk is recursive, as `SlotContainer.Walk` is. It reads `Children.Count` and the indexer, so it uses no enumerator.

### The Delegate Method Names

A delegate property is invocable. C# binds `button.Clicked(h)` to a call of the delegate in the property, and it does not try the extension method. Thus a delegate method cannot have the name of its slot. A value property is not invocable, so `label.Text("0"u8)` and `slider.Value(5f)` reach the extension method, as `list.Count()` does.

The rule: **a delegate method has the name of its slot with the `On` prefix toggled.** A widget slot has no `On`, so the method adds it: `Clicked` gives `OnClicked`. An `Element` slot has `On`, so the method removes it: `OnPointerDown` gives `PointerDown`. Each delegate method throws `ArgumentNullException` for a `null` handler.

### The Tree Compare

`tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` holds `internal static class TreeCompare` in the namespace `BrutalUi.Core.Tests`, with one method:

```csharp
internal static string? FirstDifference(Element expected, Element actual);   // null when the trees are equal
```

- It uses public Core members and `System` only, and no test framework type, so each test project can link it as `AllocationGate.cs` is linked today. A test writes `Assert.Null(TreeCompare.FirstDifference(expected, actual));`.
- The result names the path and the member, for example `root/1/0: Name differs (expected 'score', actual 'scor')`.
- For each element it compares: the runtime type, `TypeId`, `Name` (ordinal), `IsPickable`, `IsFocusable`, `States`, the class count and each `ClassId` in order, the child count, and the invocation count of the 14 event slots (0 for `null`).
- For each widget it also compares: `Label` and `Button` `Text` bytes and `LocalizedText` (reference). `Button.Clicked` count. `Toggle.CheckedChanged` count (the checked state is in `States`). `Slider` `Minimum`, `Maximum`, `Step`, `Value` (with `float.Equals`), and the `ValueChanged` count. `TextField` `Capacity`, `Text` bytes, and the `TextChanged` and `Submitted` counts. `Image` `Texture.Id`, `Texture.Size`, `Uv`, `Tint`, and `ScaleMode`. `ScrollView` `ScrollOffset`, `ScrollSpeed`, and the `ScrollChanged` count.
- It compares the delegate counts and not the delegate identity. `Command` and `BindCommand` each make a new target, so two equal builds have equal counts but two different delegates. A loaded tree of binding-attributes has the same property.
- It does not compare `Inline`, `Layout`, `Style`, `Dirty`, or `BindingCount`. The builder sets none of them (Q4). xml-element-loader extends the helper when a template sets more.
- `BrutalUi.Framework.Tests.csproj` links it in phase 1. xml-element-loader adds the same link line to `BrutalUi.Markup.Tests.csproj`: `<Compile Include="../BrutalUi.Core.Tests/Shared/TreeCompare.cs" LinkBase="Shared" />`.

---

## Implementation Phases

The feature has 33 tasks, so it has four slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea, and rule 11 binds it to the demo of Q5: the test of the section 11 example passes. The other value methods complete the surface that xml-element-loader reads next, so they come second. The query is the second headline of the requirements, and binding-attributes needs it, so it comes third. The delegate methods have no XML attribute and no sibling that waits on them, so they come last.

**The agent tier.** Each phase is STANDARD (sonnet). The feature adds extension methods, two instance methods, and tests. No phase changes a frame path, a shipped signature, or a project reference. The one risk with a compile effect, the delegate names, has a written rule above.

**The commands.** Each verification command sets `MSBUILDDISABLENODEREUSE=1`. Give `-nodeReuse:false` to `dotnet build` only, because on `dotnet test` it breaks the host handshake. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, or the same path under `BrutalUi.Framework.Tests`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces.

**The plants.** For each plant, copy the file to `src/BrutalUi.Core/obj/plantscratch/` (git-ignored), make the change, and run the solution tests with `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Expect `0 Error(s)` and an exit code that is not 0. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A plant that does not compile leaves the old binary, and the old binary passes, so check `0 Error(s)` first. A test host can exit with 134 one time. Run a second time before you treat it as a fault.

**The tests.** Follow the style of `TreeGuardTests` (the null checks with `ParamName`) and `LabelTextTests`. A test that needs a string text passes a local, because `BRUTALCORE0011` rejects a literal argument of `Text(string?)`. A bare `Text(null)` is ambiguous between the three overloads, so cast it: `Text((string?)null)`. Create a widget with `new Label(default(Label.CreateInfo))` as the Framework tests do, or with a create-info that sets a field. A create-info field takes a string literal with no analyzer error.

### Phase 1: The Section 11 Example
**Goal:** `ElementBuilder` becomes partial. `Name` and the `Text` methods of `Label` and `Button` exist in Core, and `Command` exists in Framework. `TreeCompare` exists, and a Framework test builds the section 11 example and compares it with an initializer tree.
**Demo:** the developer runs the tests, and the test of the section 11 example passes: `BrutalUi.Framework.Tests --filter-class '*CommandBuilderTests'` reads a total of 5 with `failed: 0`, and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` is one of them.
**Needs:** none
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the code and the helper, 6 files):**
- [ ] 1.1 Create `docs/features/markup/builder-api/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `386793e` at planning time), the solution total with the skipped count (2988 with 3 skipped at planning time), and the totals of `BrutalUi.Core.Tests` (1801) and `BrutalUi.Framework.Tests` (326). Record the Public API, The Query, The Delegate Method Names, The Tree Compare, and the Notes of this plan.
- [ ] 1.2 Edit `src/BrutalUi.Core/Tree/ElementBuilder.cs`. Change `public static class ElementBuilder` to `public static partial class ElementBuilder`. Replace the remark "The builder holds `Class` and `Child` only." with three sentences: the builder has one file for each group (element, text, widgets, events); a value method writes one property or calls one setter method; a delegate method adds one handler with `+=`. Add `public static T Name<T>(this T element, string? name) where T : Element`: `ArgumentNullException.ThrowIfNull(element)`, `element.Name = name`, `return element`. Doc: `null` clears the name, an empty name is valid and matches no name selector, and the set marks the subtree for style as the property does (A5). Keep the text of `Class`, `Class`, and `Child` word for word.
- [ ] 1.3 Write `src/BrutalUi.Core/Tree/ElementBuilder.Text.cs` as `public static partial class ElementBuilder`. Add the six `Text` overloads of `Label` and `Button` from the Public API. Each one checks the receiver for `null`, calls the `SetText` overload with the same parameter type, and returns the receiver. Doc: the overload calls `SetText`, so the same text marks nothing; the analyzer `BRUTALCORE0011` rejects a string literal, so write `"..."u8`; a bare `null` is ambiguous, so cast it; `null` of `string?` or `LString?` gives an empty text.
- [ ] 1.4 Edit `src/BrutalUi.Framework/Commands/CommandBindings.cs`. Add `public static Button Command(this Button button, CommandRegistry registry, ReadOnlySpan<byte> name) => BindCommand(button, registry, name);`. Doc: `Command` is the builder name of `BindCommand`, with the same checks, the same `+=` rule, and the same one allocation at build time; use it in a fluent chain. Copy the three exception lines of `BindCommand`. Change the class summary to "The bind methods". Keep the text of `BindCommand` word for word.
- [ ] 1.5 Write `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` as The Tree Compare describes. Use `System`, `System.Text` (for the bytes in a message), and Core types only. Add class remarks: the helper uses no test framework type, Framework tests link it, and xml-element-loader links it into `BrutalUi.Markup.Tests`.
- [ ] 1.6 Edit `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`. Add `<Compile Include="../BrutalUi.Core.Tests/Shared/TreeCompare.cs" LinkBase="Shared" />` after the `TextMeasure.cs` line. Build, and expect `0 Error(s)`.

**Tasks (group 2, the tests and the docs, 6 files):**
- [ ] 1.7 Write `tests/BrutalUi.Core.Tests/Builder/TreeCompareTests.cs` with 5 tests. `TwoEqualTreesGiveNoDifference` (a tree with one element of each of the eight types, two levels deep, built two times by the same local function, gives `null`). `ANameOrAClassDifferenceGivesThePath` (one name differs, then one class differs, then the class order differs: each result is not `null` and holds the path and the member name). `ATypeOrAChildCountDifferenceGivesThePath` (an `Element` against a `Label`, and two children against three). `AWidgetValueDifferenceGivesThePath` (in turn: the label text, the slider value, the image tint, the image texture size with the same id, the scroll offset, and the field capacity). `ADelegateCountDifferenceGivesThePath` (one `OnKeyDown` handler against none, and one `Clicked` handler against two).
- [ ] 1.8 Write `tests/BrutalUi.Core.Tests/Builder/ElementBuilderElementTests.cs` with 3 tests. `NameSetsTheNameAndReturnsTheSameElementWithItsType` (`Label label = new Label(default(Label.CreateInfo)).Name("score");` compiles with a literal, `label.Name` is `"score"`, and `ReferenceEquals` holds for the returned element). `NameWithNullClearsTheNameAndTheSameNameMarksNothing` (after an `Update` that clears the dirty bits, `Name` with the same name leaves `Dirty` at none, and `Name((string?)null)` clears the name and marks the style). `TheElementMethodsThrowForANullElement` (`Name` with a `null` element throws `ArgumentNullException` with `ParamName` `element`. Phase 2 adds `Pickable` and `Focusable` to this test).
- [ ] 1.9 Write `tests/BrutalUi.Core.Tests/Builder/ElementBuilderTextTests.cs` with 4 tests. `TextOfALabelSetsTheBytesOfEachOverload` (the span overload sets the bytes, the string overload with a local sets the bytes and clears `LocalizedText`, and the `LString` overload sets `LocalizedText` and the bytes of its localized text). `TextOfAButtonSetsTheBytesOfEachOverload` (the same three checks on a `Button`). `TextReturnsTheSameElementWithItsType` (a chain `new Button(...).Text("Go"u8).Class("b")` gives a `Button` variable with no cast, and the reference is the same). `TheTextMethodsThrowForANullElement` (each of the six overloads with a `null` receiver throws `ArgumentNullException`).
- [ ] 1.10 Write `tests/BrutalUi.Framework.Tests/Commands/CommandBuilderTests.cs` with 5 tests in the style of `CommandBindingTests`. `CommandInvokesTheCommandOnAClick` (one click invokes the registered command one time). `CommandKeepsAHandlerThatTheGameSetBefore` (`Clicked = h` first, then `Command`: one click runs `h`, then the command). `CommandChecksTheButtonTheRegistryAndTheName` (a `null` button and a `null` registry throw `ArgumentNullException` with the parameter names `button` and `registry`. An empty name throws `ArgumentException` with `name`). `CommandReturnsTheSameButton`. `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` (build the example of this plan word for word. Build the reference tree with no builder method: `new Element()`, `Classes.Add(ClassName.Intern("hud"))`, `new Label(new Label.CreateInfo { Text = "0" }) { Name = "score" }`, `new Button(new Button.CreateInfo { Text = "Pause" }) { Name = "pause" }`, `Classes.Add`, `Children.Add`, and `BindCommand(registry, "game.pause"u8)`. Assert `Assert.Null(TreeCompare.FirstDifference(reference, panel))`. Then a click on the `pause` button of the fluent tree invokes `game.pause` one time).
- [ ] 1.11 Edit `docs/technical-design.md`. In section 11, replace the Builder API code block with the example of this plan, and add one sentence on `registry`. Add one sentence: the string literal of `Text` needs `u8`, because `BRUTALCORE0011` rejects a literal when a span overload exists. On line 301 of section 5, add `Name(string?)` to the list of `ElementBuilder` methods. Run the build and the tests, and expect Core 1813 (+12) and Framework 331 (+5). Run two plants. Plant 1: `TreeCompare` skips the `Name` check. Expect `ANameOrAClassDifferenceGivesThePath` to fail. Plant 2: `Command` returns `button` and does not call `BindCommand`. Expect `CommandInvokesTheCommandOnAClick` and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` to fail. Record the counts, the plants, and the demo state in `context.md`.

**Estimated Steps:** about 85 (11 tasks, about eight steps for one task)

**Files (group 1, 6):** `context.md`, `ElementBuilder.cs`, `ElementBuilder.Text.cs`, `CommandBindings.cs`, `TreeCompare.cs`, `BrutalUi.Framework.Tests.csproj`
**Files (group 2, 6):** `TreeCompareTests.cs`, `ElementBuilderElementTests.cs`, `ElementBuilderTextTests.cs`, `CommandBuilderTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 12 new Core tests and the 5 new Framework tests pass, and each existing test passes with no edit.
- [ ] Plants 1 and 2 each give a non-zero exit code with `0 Error(s)`, and each restored file equals its copy under `cmp`.
- [ ] `git diff -U0 src/BrutalUi.Core/Tree/ElementBuilder.cs` shows no removed line inside the bodies or the signatures of `Class`, `Class`, and `Child`.
- [ ] The section 11 code block equals the fluent expression of the test, character for character.

### Phase 2: The Other Value Methods
**Goal:** The value surface of A2 is complete: `Pickable`, `Focusable`, the `Text` methods of `TextField`, and the eight widget value methods. A tree with one element of each of the eight types, built with the fluent methods, equals the same tree built with initializers.
**Demo:** the developer runs `BrutalUi.Core.Tests --filter-class '*BuilderTreeTests'` and reads a total of 2 with `failed: 0`. The fluent eight-type tree equals the initializer tree.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the code and the unit tests, 6 files):**
- [ ] 2.1 Edit `src/BrutalUi.Core/Tree/ElementBuilder.cs`. Add `Pickable<T>(this T element, bool value)` and `Focusable<T>(this T element, bool value)`, each with the `null` check and one write of `IsPickable` or `IsFocusable`. Doc: the default of `IsPickable` is `true`, the default of `IsFocusable` is `false`, and a change sets no dirty bit, as the property states.
- [ ] 2.2 Edit `src/BrutalUi.Core/Tree/ElementBuilder.Text.cs`. Add the three `Text` overloads of `TextField`. Doc: `SetText` of the field applies the capacity rule of the field, so a long text gets the rule of `SetText`.
- [ ] 2.3 Write `src/BrutalUi.Core/Tree/ElementBuilder.Widgets.cs` as `public static partial class ElementBuilder`. Add `Checked`, `Value`, `Range`, `Texture`, `Uv`, `Tint`, `ScaleMode`, and `ScrollOffset` from the Public API. Each one checks the receiver for `null`, writes one property or calls `SetRange`, and returns the receiver. Doc of `Checked`: a set from code calls no `CheckedChanged`. Doc of `Value`: the value rule of the slider applies (a NaN gives the minimum, then the clamp and the grid), and a set from code calls no `ValueChanged`. Doc of `Range`: it calls `SetRange`, it throws `ArgumentOutOfRangeException` for a bad range and changes nothing, and a chain sets the range before the value, because `Value` clamps to the current range. Add no method for `Capacity` and `ScrollSpeed` (A6). Add the file remarks with a pointer to phase 4 for the delegate methods.
- [ ] 2.4 Edit `tests/BrutalUi.Core.Tests/Builder/ElementBuilderElementTests.cs`. Add `PickableAndFocusableSetTheFlagsAndReturnTheSameElement` (+1). Add `Pickable` and `Focusable` to `TheElementMethodsThrowForANullElement`.
- [ ] 2.5 Edit `tests/BrutalUi.Core.Tests/Builder/ElementBuilderTextTests.cs`. Add `TextOfATextFieldSetsTheBytesOfEachOverload` (+1). Add the three `TextField` overloads to `TheTextMethodsThrowForANullElement`.
- [ ] 2.6 Write `tests/BrutalUi.Core.Tests/Builder/ElementBuilderWidgetTests.cs` with 6 tests. `CheckedSetsTheStateAndCallsNoDelegate` (`Checked(true)` sets `PseudoStates.Checked`, and a `CheckedChanged` counter stays 0). `ValueAppliesTheValueRuleAndCallsNoDelegate` (a value above the maximum clamps, a NaN gives the minimum, a value off the grid rounds, and a `ValueChanged` counter stays 0). `RangeCallsSetRangeAndThrowsForABadRange` (`Range(0, 10, 1)` sets the three values. `Range(5, 1, 0)` throws `ArgumentOutOfRangeException` and leaves the old range). `TheImageMethodsSetTextureUvTintAndScaleMode`. `ScrollOffsetSetsTheOffset` (the offset follows the clamp rule of the property, so the test gives the view a content that is larger than the view, as `ScrollViewTree` does). `EachWidgetValueMethodThrowsForANullElement` (the eight methods).

**Tasks (group 2, the tree test and the docs, 3 files):**
- [ ] 2.7 Write `tests/BrutalUi.Core.Tests/Builder/BuilderTreeTests.cs` with 2 tests. `AnEightTypeTreeFromTheFluentMethodsEqualsTheInitializerTree` (a root `Element` with a name, two classes, `Pickable(false)`, and `Focusable(true)`. Under it one `Label`, `Button`, `Toggle`, `Slider`, `TextField`, `Image`, and `ScrollView`, each with a name and each value method of its type. The reference uses create-info fields, object initializers, and direct property writes only. Assert that `TreeCompare.FirstDifference` gives `null`). `ValueBeforeRangeClampsToTheOldRange` (`new Slider(new Slider.CreateInfo { Maximum = 1f }).Value(5f).Range(0f, 10f, 0f)` gives `Value` 1, and the order `Range` then `Value` gives 5. The test pins the doc rule of task 2.3).
- [ ] 2.8 Edit `docs/technical-design.md`. In section 11, under the example, add a method list for the element group, the text group, and the widget values, in the format of the Public API of this plan. On line 301 of section 5, add `Pickable(bool)` and `Focusable(bool)`, and add one sentence that points to the full list in section 11. Run the build and the tests, and expect Core 1823 (+10). Run two plants. Plant 3: `Range` calls `SetRange(minimum, maximum, 0f)`. Expect `RangeCallsSetRangeAndThrowsForABadRange` and `AnEightTypeTreeFromTheFluentMethodsEqualsTheInitializerTree` to fail. Plant 4: `Value` returns the slider and writes nothing. Expect `ValueAppliesTheValueRuleAndCallsNoDelegate` and the eight-type test to fail. Record the counts and the plants in `context.md`.

**Estimated Steps:** about 60 (8 tasks, about seven steps for one task)

**Files (group 1, 6):** `ElementBuilder.cs`, `ElementBuilder.Text.cs`, `ElementBuilder.Widgets.cs`, `ElementBuilderElementTests.cs`, `ElementBuilderTextTests.cs`, `ElementBuilderWidgetTests.cs`
**Files (group 2, 3):** `BuilderTreeTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 new Core tests pass, and each existing test passes with no edit.
- [ ] Plants 3 and 4 each give a non-zero exit code with `0 Error(s)`, and each restored file equals its copy under `cmp`.
- [ ] `wc -l src/BrutalUi.Core/Tree/ElementBuilder*.cs` shows each file under 300 lines.
- [ ] `git diff --stat -- src/BrutalUi.Core/Elements/` shows no output.

### Phase 3: The Query by Name
**Goal:** `Element.Find(name)` and `Element.Find<T>(name)` find the first element with a UTF-8 name in the subtree of the element, the element included, in tree order, with an ordinal compare. `Find<T>` skips an element of another type (Q3). Both allocate 0 bytes, and a gate proves it.
**Demo:** the developer runs `BrutalUi.Core.Tests --filter-class '*ElementFind*'` and reads a total of 9 with `failed: 0`. The allocation test measures 0 bytes for 1000 rounds of a hit, a miss, and a typed hit.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet).

**Tasks (one group, 6 files):**
- [ ] 3.1 Edit `src/BrutalUi.Core/Tree/Element.cs`. Change `public class Element` to `public partial class Element`. Add one sentence to the class remarks: `Find` and `Find<T>` in `Element.Find.cs` give the first element with a name. Change no member.
- [ ] 3.2 Write `src/BrutalUi.Core/Tree/Element.Find.cs` as `public partial class Element` with `Find`, `Find<T>`, and two private static walks, in the shape of The Query. Use the index loop over `Children` and `Utf8Text.Utf8Equals`. Doc of both: the root first, depth-first pre-order, the first hit wins, an ordinal compare of the UTF-8 form of `Name` (so `Score` and `score` are two names), an empty name gives `null`, an element with no name never matches, the walk does not look above the element, and the call allocates 0 bytes, so a game can call it in a frame. Doc of `Find<T>`: it skips an element of another type and walks on (Q3). Write no cref to a Framework type, because Core cannot name it.
- [ ] 3.3 Write `tests/BrutalUi.Core.Tests/Builder/ElementFindTests.cs` with 7 tests. `FindChecksTheRootFirst` (the root and a child both have the name `a`, and the call gives the root). `FindWalksInTreeOrderAndGivesTheFirstHit` (two elements named `a` in two branches, and the call gives the one in the first branch, also when the second one is less deep). `FindComparesOrdinal` (`Score` does not match `score`. A name `Größe` matches `"Größe"u8`. A name `a` does not match `"ab"u8`). `FindGivesNullForAMissAnEmptyNameAndAnUnnamedTree` (a miss, `""u8` on a tree with an element whose name is empty, and a tree with no name). `FindDoesNotLookAboveTheElement` (a call on a child does not find its parent or a sibling). `FindTGivesTheElementWithItsType` (`root.Find<Label>("score"u8)` gives a `Label` variable with no cast). `FindTSkipsAnElementOfAnotherType` (an `Element` named `x` comes first in tree order, and a `Label` named `x` comes later. `Find<Label>` gives the label, and `Find<Button>` gives `null`).
- [ ] 3.4 Write `tests/BrutalUi.Core.Tests/Builder/ElementFindAllocationTests.cs` with 2 tests in the style of `ElementAllocationTests`. Build a tree of about 200 elements with names, three levels deep, and put the target last in tree order. `FindAndFindTAllocateNothingAfterWarmUp` (warm up with one call of each, call `AllocationGate.ReleaseAllocationContext()`, read `GC.GetAllocatedBytesForCurrentThread()`, run 1000 rounds of a hit, a miss, and a typed hit, and assert a difference of 0). `TheMeasuredFindDoesRealWork` (the same three calls give the target, `null`, and the typed target, and a counter of the rounds reads 1000, so the measured body cannot be empty).
- [ ] 3.5 Edit `docs/technical-design.md`. In the `Element` code block of section 5, add `Find` and `Find<T>` with a short comment each. In section 11, add `Find` and `Find<T>` under the method list with the five rules of The Query. In section 14, add one bullet for `ElementFindAllocationTests` in the style of the other allocation bullets.
- [ ] 3.6 Run the build and the tests, and expect Core 1832 (+9). Check that each `SlotContainer` test and each `UiManager` test passes with no edit, because `SlotContainer` now inherits an instance `Find`. Run three plants. Plant 5: `Walk` checks the children before the root. Expect `FindChecksTheRootFirst` to fail. Plant 6: `GC.KeepAlive(new object());` as the first line of the untyped walk. Expect `FindAndFindTAllocateNothingAfterWarmUp` to fail. Plant 7: the typed walk returns `null` at the first name hit of another type. Expect `FindTSkipsAnElementOfAnotherType` to fail. Record the counts and the plants in `context.md`.

**Estimated Steps:** about 45 (6 tasks, about eight steps for one task)

**Files (6):** `Element.cs`, `Element.Find.cs`, `ElementFindTests.cs`, `ElementFindAllocationTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 new Core tests pass, and each existing test passes with no edit.
- [ ] Plants 5, 6, and 7 each give a non-zero exit code with `0 Error(s)`, and each restored file equals its copy under `cmp`.
- [ ] `git diff src/BrutalUi.Core/Tree/Element.cs` shows the `partial` modifier and the remarks sentence only.
- [ ] `git diff --stat -- src/BrutalUi.Framework/Panels/` shows no output.

### Phase 4: The Delegate Methods
**Goal:** The builder has a method for each of the 6 widget delegate slots and the 14 event slots of `Element` (Q2). Each method adds the handler with `+=`, so a handler that the game set before stays, and a command from `Command` stays (A7). The changelog gets one line.
**Demo:** the developer runs `BrutalUi.Core.Tests --filter-class '*ElementBuilderEventTests'` and reads a total of 4 with `failed: 0`. A `PointerDown` handler that the builder adds runs on a routed pointer press.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the code and the unit tests, 5 files):**
- [ ] 4.1 Edit `src/BrutalUi.Core/Tree/ElementBuilder.Widgets.cs`. Add `OnClicked`, `OnCheckedChanged`, `OnValueChanged`, `OnTextChanged`, `OnSubmitted`, and `OnScrollChanged` from the Public API. Each one checks the receiver and the handler with `ArgumentNullException.ThrowIfNull`, runs `slot += handler`, and returns the receiver. Doc: the name rule of The Delegate Method Names, the `+=` rule (a handler that the game set before stays, and two calls add two handlers in the order of the call), and the build-time rule (create the delegate one time).
- [ ] 4.2 Write `src/BrutalUi.Core/Tree/ElementBuilder.Events.cs` as `public static partial class ElementBuilder`. Add the 14 generic event methods from the Public API, each with `where T : Element`, the two `null` checks, `element.OnX += handler`, and `return element`. Doc: the name rule, the `+=` rule, the side object rule (the first handler on the element creates the side object of the slots one time, as `+=` on the property does), and the phase of the slot (bubble and target, or capture). Write the file remarks one time and keep each method doc short, so the file stays under 300 lines.
- [ ] 4.3 Edit `tests/BrutalUi.Core.Tests/Builder/ElementBuilderWidgetTests.cs`. Add `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` (for each of the 6 slots: a handler that the test set with `=` first stays, and two builder calls give an invocation count of 3 in the order of add) and `AWidgetSlotMethodThrowsForANullReceiverOrANullHandler` (+2).
- [ ] 4.4 Write `tests/BrutalUi.Core.Tests/Builder/ElementBuilderEventTests.cs` with 4 tests. `EachEventMethodAddsTheHandlerToItsSlot` (for each of the 14 methods on a new element: its slot holds the handler, and each of the other 13 slots stays `null`). `AnEventMethodKeepsAHandlerThatTheGameSetBefore` (`OnKeyDown = h1`, then `.KeyDown(h2)`: the slot has an invocation count of 2). `AnEventMethodThrowsForANullElementOrANullHandler` (all 14, with `ParamName` `element` and `handler`). `ABuilderHandlerRunsOnARoutedEvent` (a pickable element with `.PointerDown(h)` in a context, one pointer press over it in the pattern of `EventWalkTests`, and `h` runs one time).
- [ ] 4.5 Run the build and the Core tests. Check the name rule at compile time: `button.OnClicked(h)` and `element.PointerDown(h)` compile, and no method has the name of its slot. `grep -nE 'public static \w+ (Clicked|CheckedChanged|ValueChanged|TextChanged|Submitted|ScrollChanged|On(PointerMove|PointerDown|PointerUp|Scroll|KeyDown|KeyUp|Char)(Capture)?)\b' src/BrutalUi.Core/Tree/ElementBuilder*.cs` gives no output. Record the result in `context.md`.

**Tasks (group 2, the Framework test, the tree test, and the docs, 5 files):**
- [ ] 4.6 Edit `tests/BrutalUi.Framework.Tests/Commands/CommandBuilderTests.cs`. Add `CommandAndOnClickedKeepBothDelegatesInEachOrder` (+1): `.OnClicked(h).Command(r, n)` and `.Command(r, n).OnClicked(h)` each run `h` and the command on one click, in the order of the chain (A7).
- [ ] 4.7 Edit `tests/BrutalUi.Core.Tests/Builder/BuilderTreeTests.cs`. Add `ATreeWithHandlersFromTheFluentMethodsEqualsTheInitializerTree` (+1): the eight-type tree of phase 2 with one handler on each of the 6 widget slots and on 3 event slots, against a reference that sets the same `static` handlers with `=` and `+=`. `TreeCompare` gives `null`.
- [ ] 4.8 Edit `docs/technical-design.md` and `CHANGELOG.md`. In section 11, add the 20 delegate methods to the method list, and add the name rule in two sentences. In `CHANGELOG.md`, under `## [Unreleased]`, add one line tagged `(markup/builder-api)`: the fluent methods for the eight built-in types, `Command`, and `Find` and `Find<T>`. Run the build and the solution tests, and expect Core 1839 (+7), Framework 332 (+1), and the solution 3032 with 3 skipped. Run two plants. Plant 8: `OnClicked` writes `button.Clicked = handler`. Expect `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` and `CommandAndOnClickedKeepBothDelegatesInEachOrder` to fail. Plant 9: `KeyUpCapture` writes `OnKeyUp`. Expect `EachEventMethodAddsTheHandlerToItsSlot` to fail. Run the greps of the Verification Method of the Definition of Done. Record the counts, the plants, and the grep results in `context.md`.

**Estimated Steps:** about 50 (8 tasks, about six steps for one task)

**Files (group 1, 5):** `ElementBuilder.Widgets.cs`, `ElementBuilder.Events.cs`, `ElementBuilderWidgetTests.cs`, `ElementBuilderEventTests.cs`, `context.md`
**Files (group 2, 5):** `CommandBuilderTests.cs`, `BuilderTreeTests.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 7 new Core tests and the 1 new Framework test pass, and each existing test passes with no edit.
- [ ] Plants 8 and 9 each give a non-zero exit code with `0 Error(s)`, and each restored file equals its copy under `cmp`.
- [ ] `grep -c 'public static' src/BrutalUi.Core/Tree/ElementBuilder*.cs` gives a sum of 43, and each file is under 300 lines.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 The form of the `Command` method: The `Command` method takes the registry: `.Command(registry, "game.pause"u8)`.
- Q2 Fluent methods for delegate slots: The builder gets a fluent method for all delegate slots: the 6 widget slots and the 14 event slots of `Element`.
- Q3 The typed query on a type mismatch: `Find<T>(name)` skips an element of another type and returns the first element with the name and the type `T`.
- Q4 Inline style methods: Inline style methods wait for a later feature.
- Q5 The demo of phase 1: the developer runs the tests, and the test of the section 11 example passes.
- A1 Each fluent method is an extension method in `ElementBuilder` in `BrutalUi.Core`, and it returns the element with its own type.
- A2 The value methods are `Name`, `Pickable`, `Focusable`, `Text`, `Checked`, `Value`, `Range`, `Texture`, `Uv`, `Tint`, `ScaleMode`, and `ScrollOffset`. `States` gets no method.
- A3 `Text` covers `Label`, `Button`, and `TextField`, each with a UTF-8 span, a `string`, and an `LString` overload.
- A4 The analyzer `BRUTALCORE0011` rejects a string literal there, so the section 11 example changes to `.Text("0"u8)`.
- A5 `Element.Name` stays a `string`, and `Name(string)` sets it.
- A6 A create-time value gets no method: `Capacity` of `TextField`, `ScrollSpeed` of `ScrollView`. `Range` calls `Slider.SetRange`.
- A7 A delegate method adds the handler with `+=`, so a command from `BindCommand` stays.
- A8 The query is `Find(ReadOnlySpan<byte> name)` and `Find<T>` on `Element`. It checks the root first, walks in tree order, compares ordinal, and returns `null` for no hit.
- A9 `Command` is in `CommandBindings` in `BrutalUi.Framework`, on `Button` only.
- A10 The tree compare is a test helper, not a public API. HelloPanel does not change.

### Decision 1: Four partial files, explicit overloads, no new public type
**Choice:** `ElementBuilder` becomes `static partial` in four files: element, text, widgets, events. Each widget method takes the sealed widget type. Each `Element` method is generic with `where T : Element`.
**Rationale:** This is the Chosen Approach. Each file stays under 300 lines. Each value method maps to one table row of xml-element-loader. The user rejected one large file and a new `ITextElement` interface. The widgets are sealed, so a generic form gives nothing there.
**Alternatives:** One file (the user rejected it). A shared interface for text (the user rejected it, and it adds a public type).

### Decision 2: The delegate method name toggles the `On` prefix
**Choice:** `Clicked` gives `OnClicked`. `OnPointerDown` gives `PointerDown`. The full rule is in The Delegate Method Names.
**Rationale:** A delegate property is invocable, so a method with the slot name is never reached: C# binds the call to the delegate in the property. One mechanical rule covers all 20 slots, and each name is short. A value property is not invocable, so the value methods keep the property name (A2).
**Alternatives:** The same name as the slot (does not compile in use). A common prefix such as `Handle` or `When` (longer, and it adds a word that no other API of the project uses).

### Decision 3: The query compares through `Utf8Text.Utf8Equals`
**Choice:** `Find` compares the UTF-8 query with the UTF-8 form of `Element.Name` through the shipped internal `Utf8Text.Utf8Equals`, after a length check.
**Rationale:** `Name` stays a `string` (A5), and the query takes bytes (A8). The helper already encodes in chunks into a stack buffer, allocates nothing, and has the ordinal byte rule. A second compare would duplicate it. The length check (1 to 3 bytes for each UTF-16 unit) rejects most misses before the encode.
**Alternatives:** `Encoding.UTF8.GetString` of the query (it allocates). A rune-by-rune decode (new code for the same result).

### Decision 4: `Element` becomes partial, and the query has its own file
**Choice:** Add the `partial` modifier to `Element` and write `Element.Find.cs`.
**Rationale:** A8 puts the query on `Element`. `Element.cs` has 971 lines. A partial file keeps the query and its walks together, and the modifier does not break a caller.
**Alternatives:** An extension method in `ElementBuilder` (A8 says on `Element`). The code in `Element.cs` (a large file grows).

### Decision 5: Empty and `null` arguments
**Choice:** Each method throws `ArgumentNullException` for a `null` receiver. Each delegate method throws `ArgumentNullException` for a `null` handler. `Name(null)` clears the name, and `Name("")` sets an empty name, as the property does. `Text` with a `null` `string` or `LString` gives an empty text, as `SetText` does. `Range` passes the checks of `SetRange` through. `Find` with an empty name gives `null`.
**Rationale:** A value method maps 1 to 1 to its property, so it keeps the rules of the property. A `null` handler in a fluent chain is a bug, and `+= null` hides it, so the method throws. The empty-name rule copies `SlotContainer.Find`.

### Decision 6: `Command` calls `BindCommand`
**Choice:** `Command` is one line that calls `BindCommand`. Both stay public.
**Rationale:** A9 puts `Command` in `CommandBindings`. One body gives one behavior, so the remarks of `BindCommand` apply to both. `BindCommand` is shipped, so it stays.

### Decision 7: The tree compare gives a string and compares delegate counts
**Choice:** `TreeCompare.FirstDifference` returns `null` or a message with the path. It compares the delegate invocation counts, not the delegate identity.
**Rationale:** A message names the first difference, so a failed test shows the cause. The helper uses no test framework type, so each test project can link it. Two equal builds with `Command` make two different targets, so an identity compare fails on equal trees.

### Decision 8: The section 11 example uses `new Label(default)`
**Choice:** The example creates each widget with `new Label(default)` and `new Button(default)`, and sets the name with `.Name(...)`.
**Rationale:** The old object initializer form does not compile (Notes item 1). The requirement asks for `Name`, `Class`, `Text`, `Child`, and `Command` in one expression. When the compiler rejects `default` there, the test and the doc use `default(Label.CreateInfo)`, and `context.md` records it.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | `BRUTALCORE0011` rejects `Name("score")`. | The section 11 example does not compile. | Low. `Name` has no span overload, and `.Class("a")` with a literal already compiles in `ElementStateTests.cs:140`. Task 1.8 compiles a literal. When it fails, `context.md` records it, and the example passes a local. |
| 2 | A delegate method has the name of its slot. | A call does not compile, or it calls the delegate. | Decision 2 and the grep of task 4.5. |
| 3 | `SlotContainer` inherits an instance `Find` next to its static `Find(Element, ReadOnlySpan<byte>)`. | An ambiguous call or a cref warning. | The parameter counts differ, so each call has one match. Task 3.6 runs the `SlotContainer` and `UiManager` tests with no edit. A warning fails the build, so a cref fault shows. |
| 4 | `Value` before `Range` clamps to the old range. | A chain gives a value that the author did not expect. | The doc of `Range` and `Value` states the order, and `ValueBeforeRangeClampsToTheOldRange` pins it. xml-element-loader applies `Range` before `Value` (Notes item 4). |
| 5 | The allocation gate reads stray bytes from a background GC. | A false failure. | The gate calls `AllocationGate.ReleaseAllocationContext()` right before the measured region. A failure runs a second time before it counts. |
| 6 | The tree compare misses a member that a later template sets. | A loaded tree passes the compare but differs. | The helper states what it does not compare. xml-element-loader extends it (Notes item 3). |
| 7 | A recursive walk on a very deep tree. | A stack overflow. | The walk has the same shape as the shipped `SlotContainer.Walk`. A UI tree is not deep enough for this. The feature adds no depth limit (YAGNI). |

---

## Dependencies

- core-tree gives `Element`, `ElementBuilder`, `ClassName`, and `ElementList`. interactive-elements gives the seven widget types and their setters. state-and-commands gives `CommandRegistry` and `BindCommand`. All are complete.
- `Utf8Text.Utf8Equals` (internal, `src/BrutalUi.Core/Elements/Utf8Text.cs:373`) gives the compare of the query.
- No new package, no native dependency, and no project reference. One test project file gets one `Compile` link.
- No sibling feature comes before this one. xml-element-loader, element-generator, and binding-attributes depend on it.

---

## Definition of Done

### Functional Criteria
- [ ] `ElementBuilder` is `public static partial class` in four files under `src/BrutalUi.Core/Tree/`: `ElementBuilder.cs`, `ElementBuilder.Text.cs`, `ElementBuilder.Widgets.cs`, and `ElementBuilder.Events.cs`. Together they hold the 43 public static methods of the Public API of this plan, with the same names and parameter types.
- [ ] Each fluent method returns the same instance that it got, with the static type of the receiver. A chain `new Button(...).Text("Go"u8).Class("b").Name("n")` gives a `Button` with no cast.
- [ ] Each value method sets the property that the Public API names, and a test reads the property back. `Range` calls `SetRange`. No method exists for `States`, `Capacity`, `ScrollSpeed`, `Inline`, or `Layout`.
- [ ] Each of the 20 delegate methods adds its handler to the named slot with `+=`. A handler that the game set before stays. `.OnClicked(h).Command(r, n)` and `.Command(r, n).OnClicked(h)` each run both delegates on one click.
- [ ] `CommandBindings.Command(this Button, CommandRegistry, ReadOnlySpan<byte>)` exists in `BrutalUi.Framework` and binds the command as `BindCommand` does.
- [ ] `Element.Find(ReadOnlySpan<byte>)` gives the first element with the name in depth-first pre-order, the root first, with an ordinal compare. It gives `null` for a miss and for an empty name. `Find<T>` skips an element of another type and gives the first element with the name and the type `T`.
- [ ] The section 11 Builder API code block in `docs/technical-design.md` compiles as part of `TheSectionElevenExampleBuildsTheSameTreeAsInitializers`, and the test passes.
- [ ] A fluent build of a tree with all eight types equals an initializer build of it under `TreeCompare.FirstDifference`.

### Quality Criteria
- [ ] Each fluent method throws `ArgumentNullException` for a `null` receiver. Each delegate method throws `ArgumentNullException` with `ParamName` `handler` for a `null` handler. `Command` throws `ArgumentException` for an empty name. A test covers each.
- [ ] `Find` and `Find<T>` allocate 0 bytes over 1000 rounds of a hit, a miss, and a typed hit after warm-up (`FindAndFindTAllocateNothingAfterWarmUp`), and plant 6 fails that test.
- [ ] `Class<T>(this T element, string name)`, `Class<T>(this T element, ClassId id)`, `Child<T>(this T element, Element child)`, and `BindCommand` keep their exact signature and body.
- [ ] Each existing test passes with no edit. The totals grow by 38 in Core and 6 in Framework from the baseline that `context.md` records.
- [ ] The build gives `0 Warning(s)` and `0 Error(s)`. Each new public member has an XML doc comment.
- [ ] Each `ElementBuilder` file has fewer than 300 lines.

### Integration Criteria
- [ ] `BrutalUi.Core` gets no reference to Framework or Markup, and `BrutalUi.Markup` does not change.
- [ ] `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` uses no test framework type, and `BrutalUi.Framework.Tests.csproj` links it. Its remarks name the link line for `BrutalUi.Markup.Tests`.
- [ ] `SlotContainer.cs`, `UiManager.cs`, each widget file under `src/BrutalUi.Core/Elements/`, and `samples/` do not change.
- [ ] Section 5 line 301 and section 11 of `technical-design.md` name each method. Section 14 names the new gate. `CHANGELOG.md` has one line tagged `(markup/builder-api)`.

### Verification Method
1. Run `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)`.
2. Run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and 3 skipped.
3. Run `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*ElementBuilder*' 2>&1 | tail -n 5`. Expect a total of 21 with `failed: 0` (`ElementBuilderElementTests` 4, `ElementBuilderTextTests` 5, `ElementBuilderWidgetTests` 8, `ElementBuilderEventTests` 4). Run the same binary with `'*BuilderTreeTests'`, `'*ElementFind*'`, and `'*TreeCompareTests'`. Expect the totals 3, 9, and 5 with `failed: 0`.
4. Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*CommandBuilderTests' 2>&1 | tail -n 5`. Expect a total of 6 with `failed: 0`.
5. Run `grep -c 'public static' src/BrutalUi.Core/Tree/ElementBuilder*.cs` and add the counts. Expect 43. Run `wc -l src/BrutalUi.Core/Tree/ElementBuilder*.cs`. Expect each file under 300.
6. Run `git diff 386793e -- src/BrutalUi.Core/Tree/ElementBuilder.cs | grep '^-[^-]'`. Expect only the old class line and the old remarks line.
7. Run `git diff --stat 386793e -- src/BrutalUi.Markup src/BrutalUi.Core/Elements src/BrutalUi.Framework/Panels samples`. Expect no output.
8. Run `git diff 386793e -- src | grep -E '^\+.*public (sealed |static |abstract )*(partial )*(class|struct|interface|enum|record|delegate) '`. Expect only `public static partial class ElementBuilder` and `public partial class Element`.
9. Run the grep of task 4.5. Expect no output.
10. Compare the section 11 code block of `docs/technical-design.md` with the fluent expression in `TheSectionElevenExampleBuildsTheSameTreeAsInitializers`. Expect the same text.
11. Read `context.md`. Expect plants 1 to 9, each with a non-zero exit code, `0 Error(s)`, and the expected failed test names.

---

## Testing Strategy

- **Unit tests (Core, 38):** one test class for each builder file, the tree compare, the query, and the query gate. Each method has a read-back assert, a same-instance assert, and a `null` check.
- **Tree tests (Core 3, Framework 1):** `BuilderTreeTests` and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` compare a fluent tree with an initializer tree through `TreeCompare`. They prove requirements line 17.
- **Framework tests (6):** `CommandBuilderTests` covers `Command`, its checks, the order with `OnClicked`, and the section 11 example.
- **Allocation gate (2):** `ElementFindAllocationTests` measures `Find` and `Find<T>`, with a real-work test so the measured body cannot be empty.
- **Plants (9):** each phase plants a fault and proves that a named test fails. The plants guard the compare helper, `Command`, `Range`, `Value`, the root-first rule, the 0-byte rule, the type skip, the `+=` rule, and the slot map.
- **No regressions:** each existing test passes with no edit. The `SlotContainer` tests guard the inherited `Find`.
- **No manual check:** no window, no sample change, and no Needs Human Verification item.

| Class | Project | Phase | Tests |
|---|---|---|---|
| `TreeCompareTests` | Core | 1 | 5 |
| `ElementBuilderElementTests` | Core | 1, 2 | 3 + 1 |
| `ElementBuilderTextTests` | Core | 1, 2 | 4 + 1 |
| `CommandBuilderTests` | Framework | 1, 4 | 5 + 1 |
| `ElementBuilderWidgetTests` | Core | 2, 4 | 6 + 2 |
| `BuilderTreeTests` | Core | 2, 4 | 2 + 1 |
| `ElementFindTests` | Core | 3 | 7 |
| `ElementFindAllocationTests` | Core | 3 | 2 |
| `ElementBuilderEventTests` | Core | 4 | 4 |

---

## Related Documentation

- `docs/features/markup/builder-api/requirements.md`
- `docs/features/markup/epic-overview.md` and `docs/features/markup/epic-requirements.md`
- `docs/features/markup/xml-element-loader/requirements.md` (the table rows and the tree compare)
- `docs/features/markup/binding-attributes/requirements.md` (the `Command` method and the query)
- `docs/technical-design.md` section 5 (the element tree, line 301) and section 11 (the Builder API, about line 1366)
- `docs/features/imgui-renderer/label-and-image/context.md` (the `BRUTALCORE0011` rule for `SetText`)
- `src/BrutalUi.Framework/Panels/SlotContainer.cs:177` (the model walk) and `src/BrutalUi.Core/Elements/Utf8Text.cs:373` (the compare)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

1. **A conflict in the design doc, not in a confirmed item.** The section 11 example uses `new Label { Name = "score" }` and `new Button { Name = "pause" }`. `Label` and `Button` have only the constructor `(in CreateInfo info)`, so this form does not compile. The plan changes the example to `new Label(default).Name("score")` (Decision 8). A4 covers only the `Text` part of the change.
2. **A constraint on Q2, not a conflict.** Q2 asks for a method for each delegate slot. C# does not reach an extension method that has the name of an invocable property, so the 20 methods cannot use the slot names. Decision 2 gives the names. No confirmed item names them.
3. **For xml-element-loader.** Link `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` into `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj` with `<Compile Include="../BrutalUi.Core.Tests/Shared/TreeCompare.cs" LinkBase="Shared" />`. When a template sets a member that the helper does not compare (for example an inline style), extend the helper there. The 12 value names of A2 are the table rows. The 20 delegate methods have no row, because XML cannot hold a delegate.
4. **For xml-element-loader.** `Value` clamps to the current range. A row for `value` must run after the rows for the range, or the loader must read the range from the attributes first.
5. **For binding-attributes.** The `command` attribute can call `Command` or `BindCommand`, because both have one body. A bound tree and a builder tree compare equal under `TreeCompare`, because the helper compares delegate counts.
6. **A small mismatch in the requirements.** Requirements line 15 tags the query with "(Q4)". In the confirmed block, Q4 is the inline style item, and A8 and Q3 cover the query. The plan follows A8 and Q3.
7. **The word "settable".** Requirements line 12 asks for a method for each settable property. `States` (A2), the inline style (Q4), `Capacity` and `ScrollSpeed` (A6), and `Element.Layout` (a `ref` return for the layout node) get no method. No confirmed item names `Layout`. The plan keeps it out of scope, because it is not a template property and the requirements add no element property.
8. **A1 and the two non-builder members.** A1 says that each fluent method is an extension method in `ElementBuilder`. `Command` (A9) is in `CommandBindings`, and `Find` (A8) is an instance method on `Element`. The plan reads A1 as the rule for the Core fluent methods. `Command` and `Find` follow their own confirmed items.
9. **The planned totals** come from the binding-remap close: Core 1801, Framework 326, and the solution 2988 with 3 skipped. Task 1.1 records the real baseline, and the acceptance criteria count the deltas from it.
