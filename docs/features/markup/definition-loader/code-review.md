# markup/definition-loader - Code Review

**Reviewed:** 2026-11-18
**Reviewer:** Agent (cross-phase review, phases 1 to 3)
**Skills Applied:** None. The `vercel-react-best-practices` and `vercel-composition-patterns` rules apply to React and JavaScript. This feature is C# on .NET 10, so no Vercel rule applies. The review uses the project rules of `AGENTS.md` and the quality bar of `implementation.md` instead.
**Status:** Resolved - 5 findings, 5 resolved (2026-11-18, review-fix run: groups 1 and 2, verifier PASS)

---

## Summary

The review reads each source file of the feature: `DefinitionLoader.cs`, `DefinitionReader.cs`, `DefinitionReader.Elements.cs`, `DefinitionReader.Resolve.cs`, `DefinitionLoadResult.cs`, `DefinitionError.cs`, and `SlotContainerTemplate.cs` under `src/BrutalUi.Framework/Templates/`. It also reads the related code in `UiManager.cs`, `ModeDefinition.cs`, `TemplateLoader.cs`, and `TemplateReader.cs`, and the error tests.

The code is sound. Each record constructor guard has a matching load check, so a load does not throw for the text. The error kinds, positions, and messages follow the plan. Two findings break a quality bar item: "one load gives each error" (CR-1) and "one fault gives one error" (CR-2). One finding is a plan choice that conflicts with the quality bar (CR-3). Two findings are small.

| Severity | Count |
| --- | --- |
| CRITICAL | 0 |
| HIGH | 0 |
| MEDIUM | 3 |
| LOW | 2 |

---

## Findings

### CR-1: A mode with a fault in its own values hides the faults of its placements

**Severity:** MEDIUM
**Rule:** Quality bar, "one load gives each error" (The Two Passes, resolve pass)
**File:** `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs:141-171`

**Problem:** Step 4 stops the work on a mode at the first fault of the mode itself. An `UnknownLayout` or an `UnknownActionMap` runs `continue` before the placement loop. A mode with no `name`, no `layout`, or a duplicate name also skips the loop. The placement checks `UnknownPanel` and the panel two times in one mode (`DuplicateName`) do not depend on the layout or the map, so their faults stay hidden. The user fixes one error, loads again, and gets a new error.

```csharp
if (!layoutNames.TryGetValue(layout, out int layoutIndex))
{
    AddError(DefinitionErrorKind.UnknownLayout, ...);
    continue;                       // the PanelRef names are never checked
}
...
if (!_loader.TryGetActionMap(map, out actionMap))
{
    AddError(DefinitionErrorKind.UnknownActionMap, ...);
    continue;                       // the same
}
```

Repro: `<ui:Mode name="m" layout="gam"><ui:PanelRef name="hdu" /></ui:Mode>` gives one error (`UnknownLayout`), not two. The test `AnUnknownLayoutOrPanelNameIsAnError` (`tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs:71`) puts the two faults in two modes, so it does not catch this case.

The same pattern applies in a smaller form to steps 2 and 3. A `Panel` with no `slot` skips the template lookup, so an unknown `template` on the same element gives no error.

**Fix:** Keep a `faulty` flag for the mode. Record each fault of the mode values, set the flag, and still run the placement loop. Without a known layout, set `checkSlots = false`. Build the `ModeDefinition` only when the flag is clear. In steps 2 and 3, look up a present template name also when another value of the entry has a fault.

```csharp
bool faulty = !entry.Name.HasName || !IsFirstName(modeNames, entry.Name, i);
int layoutIndex = -1;
if (entry.Layout.HasName && !layoutNames.TryGetValue(entry.Layout.Text!, out layoutIndex))
{
    AddError(DefinitionErrorKind.UnknownLayout, ...);
    faulty = true;
}
else if (!entry.Layout.HasName)
{
    faulty = true;
}

// the actions lookup sets faulty = true on a miss, with no continue
bool checkSlots = layoutIndex >= 0 && !layoutFaulty[layoutIndex];
// ... the placement loop as today ...
if (!faulty)
{
    modes.Add(new ModeDefinition(...));
}
```

Add one test: one mode with an unknown layout and an unknown panel gives two errors.

**Effort:** Low

---

### CR-2: An element outside the namespace under a failed element gives a follow-on error

**Severity:** MEDIUM
**Rule:** Quality bar, "one fault gives one error" (Decision 7)
**File:** `src/BrutalUi.Framework/Templates/DefinitionReader.Elements.cs:23-43`

**Problem:** `ReadElement` runs the namespace check before the failed parent check. A child of a failed element must give no error, but a child outside the `brutal-ui` namespace gives `UnknownElement`.

```csharp
// Check 1. runs first
if (xml.NamespaceURI != TemplateLoader.Namespace)
{
    AddError(DefinitionErrorKind.UnknownElement, ...);   // also under a failed parent
    PushFrame(Kind.Failed, -1, default, isEmpty);
    return;
}

Kind parent = _frames.Count == 0 ? Kind.Root : _frames.Peek().Kind;
if (parent == Kind.Failed) { ... return; }              // too late for check 1
```

Repro: `<ui:Definitions xmlns:ui="brutal-ui"><ui:Window><Panel /></ui:Window></ui:Definitions>` gives two `UnknownElement` errors. The second error points at a child of the real fault.

**Fix:** Move the failed parent check above check 1.

```csharp
Kind parent = _frames.Count == 0 ? Kind.Root : _frames.Peek().Kind;
if (parent == Kind.Failed)
{
    PushFrame(Kind.Failed, -1, default, isEmpty);
    return;
}

// Check 1. The namespace.
if (xml.NamespaceURI != TemplateLoader.Namespace) { ... }
```

Add one case to `DefinitionErrorTests`: a child outside the namespace under a `Window` gives one error.

**Effort:** Low

---

### CR-3: An unused layout with a bad template passes the load and throws in `RegisterTo`

**Severity:** MEDIUM
**Rule:** Quality bar, "a result with no error registers in `UiManager` with no throw from a fault of the text"
**File:** `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs:108-135`, `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs:116-120`

**Problem:** Step 3b loads only the layouts that a mode names (Q3). A layout that no mode names, with a template that does not load, gives a successful result. `UiManager.Register(LayoutDefinition)` calls the factory at once (`UiManager.cs:376`), so `RegisterTo` throws `InvalidOperationException` from `GetRootOrThrow`. `RegisterTo` is not atomic, so the panels and the layouts before the bad one stay registered. The XML doc of `RegisterTo` lists no `InvalidOperationException` for a successful result, so a caller does not expect this throw.

The plan records this as Notes item 6 and keeps Q3. The behavior still conflicts with the Reliability item of the quality bar, which says that the loader "checks each used layout template" and that a success result registers with no throw from a fault of the text.

**Fix:** Take the option of Notes item 6: run `CheckLayout` for each layout that is not faulty, also for a layout that no mode names. This adds one load for each unused layout, at build time only. If the team keeps Q3, add the throw to the `<exception>` list of `RegisterTo` and to the remarks of `DefinitionLoader`, and change the quality bar sentence to "each layout that a mode names".

```csharp
for (int i = 0; i < _layoutEntries.Count; i++)
{
    if (layoutFaulty[i])
    {
        continue;
    }

    if (CheckLayout(_layoutEntries[i]) is string[] slots) { layoutSlots[i] = slots; }
    else { layoutFaulty[i] = true; }
}
```

**Effort:** Low

---

### CR-4: The root message writes the namespace as a literal

**Severity:** LOW
**Rule:** Single source of a constant
**File:** `src/BrutalUi.Framework/Templates/DefinitionReader.Elements.cs:258-259`

**Problem:** Check 1 builds its message from `TemplateLoader.Namespace`. `PlaceMessage` writes `xmlns:ui=\"brutal-ui\"` as a literal. A change of the namespace makes the root message give the wrong fix.

```csharp
return $"'{localName}' is not the root of a definition text. "
    + "Write <ui:Definitions xmlns:ui=\"brutal-ui\"> as the root.";
```

**Fix:** Use the constant.

```csharp
return $"'{localName}' is not the root of a definition text. "
    + $"Write <ui:Definitions xmlns:ui=\"{TemplateLoader.Namespace}\"> as the root.";
```

**Effort:** Low

---

### CR-5: The read pass allocates the attribute tables and the joined name list for each element

**Severity:** LOW
**Rule:** Avoidable load-time allocation (the zero-allocation rule covers update and draw only, so this is not a rule break)
**File:** `src/BrutalUi.Framework/Templates/DefinitionReader.Elements.cs:142-143, 199, 303-325`; `src/BrutalUi.Framework/Templates/DefinitionReader.cs:94`

**Problem:** `AllowedNames` and `RequiredNames` use collection expressions in a switch, so each call allocates a new array. `ReadAttributes` calls both for each element and calls `string.Join` for the message of check 3b also when no attribute is unknown. `Load` also copies the whole input span to a new array before it wraps it in a `MemoryStream`. The cost is small and runs at build time, but a large definition text pays it for each element.

```csharp
string[] allowed = AllowedNames(kind);       // new array for each element
string names = string.Join(", ", allowed);   // new string for each element, used only on an error
```

**Fix:** Keep the tables in `static readonly string[]` fields, and build the joined list only in the error branch. With `AllowUnsafeBlocks` on, `Load` can read the span through an `UnmanagedMemoryStream` inside a `fixed` block and skip the copy.

```csharp
private static readonly string[] PanelNames = ["name", "slot", "template"];
...
if (Array.IndexOf(allowed, name) < 0)
{
    AddError(..., $"... The attributes of {elementName} are {string.Join(", ", allowed)}.", ...);
}
```

**Effort:** Low

---

## What's Already Good

- **No throw for the text.** Each guard of the `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, and `ModePanel` constructors has a load check before it: empty names, empty slots, and a panel two times in one mode. Each check of `UiManager.Register(ModeDefinition)` (unknown layout, unknown slot, unknown panel) has a load check too.
- **XML safety.** The reader settings match `TemplateReader`: DTD prohibited, no resolver, and `ConformanceLevel.Document`. An `XmlException` becomes one `NotWellFormed` error with its position.
- **The follow-on rule.** The name tables keep a faulty entry, so a reference to it gives no false `UnknownPanel` or `UnknownLayout`. A placement of a faulty panel gives no slot error.
- **The slot walk.** `CollectSlots` reads `SlotName`, the same value that `SlotContainer.Find` reads in `UiManager`, so the load check and the register agree when a template also sets `name`.
- **A stable error sort.** `SortErrors` uses the original index as the tie-break, so the order of two errors at one position does not change between runs.
- **Clear API checks.** `AddTemplate` copies the text, and both `Add*` calls check the name before the add, so a throw leaves the table unchanged. `DefinitionLoadResult` exposes read-only lists and has private construction paths.
- **Factories.** Each factory captures the text copy and the template name, and loads a new root at each call (A4). Plant 1 guards it.
- **File size and docs.** Each source file stays under 400 lines, and each member has an XML doc comment.

## Recommended Fix Order

| Order | Finding | Severity | Effort | Why this order |
| --- | --- | --- | --- | --- |
| 1 | CR-2: An element outside the namespace under a failed element gives a follow-on error | MEDIUM | Low | One moved block and one test. It restores a quality bar item. |
| 2 | CR-1: A mode with a fault in its own values hides the faults of its placements | MEDIUM | Low | A small change in step 4 and one test. It restores a quality bar item. |
| 3 | CR-3: An unused layout with a bad template passes the load and throws in `RegisterTo` | MEDIUM | Low | Needs a team choice between the code fix and the doc fix. |
| 4 | CR-4: The root message writes the namespace as a literal | LOW | Low | One line. |
| 5 | CR-5: The read pass allocates the attribute tables and the joined name list for each element | LOW | Low | Build time only. |

## Progress

- [x] CR-1: A mode with a fault in its own values hides the faults of its placements
- [x] CR-2: An element outside the namespace under a failed element gives a follow-on error
- [x] CR-3: An unused layout with a bad template passes the load and throws in `RegisterTo`
- [x] CR-4: The root message writes the namespace as a literal
- [x] CR-5: The read pass allocates the attribute tables and the joined name list for each element
