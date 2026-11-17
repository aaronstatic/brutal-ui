# Binding Attributes - Code Review

**Reviewed:** 2026-11-17 06:39 UTC
**Reviewer:** Agent (project rules from AGENTS.md + the plan's Quality Bar)
**Skills applied:** `/review-feature` flow, C# library rule set instead of the Vercel React rules
**Status:** Resolved - 10 findings, 10 resolved (review pass closed 2026-11-17 07:20)

## Summary

Counts: CRITICAL 0, HIGH 0, MEDIUM 1, LOW-MEDIUM 5, LOW 4. Total 10.

The feature is sound. The architecture fits: one public class, one captured handler, one key table, the
existing bind methods under it, and 8 changed lines in Markup. The plan's decisions all read true in the
code, and the gate passed (solution 3166, failed 0, skipped 3, Framework 355, build 0 warnings). I also
re-checked the tree after the 06:15 duplicate-agent incident: `TemplateBindings.cs` is the clean file
(md5 `514499b699b2313b3c0b793b12fb528c`, 398 lines, the `AttachTo` loader check present).

One real defect: two of the four empty-value fault paths have no test, so a mutation there stays green
(CR-1). The rest are documentation accuracy (CR-2, CR-3, CR-8, CR-9, CR-10), one untested documented
decision (CR-4), one lifetime note on the API (CR-7), one prose error (CR-5), and one file at 398 of the
400-line cap (CR-6). No public-API change and no source behaviour change is required, except the optional
one in CR-6.

## Findings

### CR-1: Two empty-value fault paths have no test
**Severity:** MEDIUM
**Rule:** Quality Bar "Exact errors with positions" — "Each of the four fault cases (A2, A3, `UnknownBinding`,
`BindingTypeMismatch`) has a test that asserts the kind, the line, and the column"; Testing Strategy
"Error and API tests (Framework, 10)".
**File:** `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingErrorTests.cs:58-63,163-177` and
`src/BrutalUi.Framework/Templates/TemplateBindings.cs:340-345,371-376`
**Problem:** The code guards an empty value for each of the four names, but the test only loads two of them:

```xml
<ui:Button name="pause" text="Pause" command="" />
<ui:Label name="player" text="Ann" bind-text="" />
```

The `bind-checked` and `bind-value` branches never run with an empty value:

```csharp
if (value.Length == 0)
{
    context.AddError(TemplateErrorKind.InvalidValue,
        "'bind-checked' needs a key. Write a key that the panel code registers, for example 'hud.muted'.");
    return true;
}
```

Delete that block (or the `bind-value` one) and the key lookup takes the empty text, the load reports
`UnknownBinding` instead of `InvalidValue`, and the whole suite stays green. The same gap hides the
message text for those two attributes (see CR-2). No plant covered them either: the plan's plant 6 moved
the `bind-text` checks only.
**Fix:** Add the two names to `EmptyValueTemplate` and widen the assert:

```xml
<ui:Toggle name="mute" bind-checked="" />
<ui:Slider name="volume" max="100" bind-value="" />
```

```csharp
Assert.Equal(4, errors.Count);
AssertAttribute(errors[2], EmptyValueTemplate, "bind-checked", string.Empty);
AssertAttribute(errors[3], EmptyValueTemplate, "bind-value", string.Empty);
Assert.Contains("'bind-checked'", errors[2].Message);
Assert.Contains("'bind-value'", errors[3].Message);
```
**Effort:** Low

### CR-2: The message table names one example key for three different messages
**Severity:** LOW-MEDIUM
**Rule:** Brief angle 7 (doc drift) and task 3.6: "the four messages, quoted from `Handle`".
**File:** `docs/technical-design.md:1612` against `src/BrutalUi.Framework/Templates/TemplateBindings.cs:300,343,374`
**Problem:** The table says one text serves every bind attribute:

> Each bind attribute reads `'<name>' needs a key. Write a key that the panel code registers, for example 'hud.score'.`

The code uses a different example for two of them:

```csharp
"'bind-checked' needs a key. Write a key that the panel code registers, for example 'hud.muted'."
"'bind-value' needs a key. Write a key that the panel code registers, for example 'hud.volume'."
```

A developer who greps the doc text finds no match, and a future edit that copies the doc into the code
breaks nothing, because no test asserts those two texts (CR-1).
**Fix:** In the "An empty value" row, state the per-attribute example: `'hud.score'` for `bind-text`,
`'hud.muted'` for `bind-checked`, `'hud.volume'` for `bind-value`.
**Effort:** Low

### CR-3: The attribute-hook paragraph still names two attributes
**Severity:** LOW-MEDIUM
**Rule:** Brief angle 7 (doc drift); requirements line 19 "Sections 10 and 11 of `technical-design.md` get
the attributes"; Quality Bar "Each public member has an XML doc comment in ASD-STE100 prose" (the doc is
the surface a game developer reads first).
**File:** `docs/technical-design.md:1533`
**Problem:** The closing sentence of **The attribute hook.** reads:

> `TemplateBindings.AttachTo` registers `command` and `bind-text` here.

The same section's own table two paragraphs later lists four names (`command`, `bind-text`, `bind-checked`,
`bind-value`, lines 1572-1577), and `Handle` (TemplateBindings.cs:269-396) takes all four. Task 1.6 wrote
this line when only two names existed; task 3.6 did not revisit it.
**Fix:** Change the line to: "…registers `command`, `bind-text`, `bind-checked`, and `bind-value` here."
The same stale sentence sits in `src/BrutalUi.Markup/Template/TemplateLoader.cs:219`; leave it there in
this feature (A7 keeps the file unchanged, plan Notes item 10) and widen it in a later Markup pass.
**Effort:** Low

### CR-4: The no-trim decision is documented and untested
**Severity:** LOW-MEDIUM
**Rule:** Decision 5 and Notes item 6 ("The plan uses the value as is"); Quality Bar "Reliability".
**File:** `src/BrutalUi.Framework/Templates/TemplateBindings.cs:297-309` and `docs/technical-design.md:1567`
**Problem:** The handler compares the value with no trim:

```csharp
if (!_entries.TryGetValue(value, out TemplateBindingEntry? textEntry))
{
    context.AddError(TemplateErrorKind.UnknownBinding,
        $"'{value}' is not a bound key. Register it with TemplateBindings.AddText before the load.");
    return true;
}
```

`bind-text=" hud.name "` gives `UnknownBinding` with the spaces inside the quote, and
`command=" game.pause "` binds a name with the spaces and fails only at the click, through
`CommandRegistry.UnknownCommand`. No test uses a spaced value (`grep` of `tests/` finds none), so a later
change that adds a `Trim()` — the alternative that Notes item 6 lists — passes the suite and silently
changes a documented rule. Note that the rest of Markup does trim: `AttributeContext.TryRead*` calls
`TemplateValues.Trim(Value)` on every value read, so the binding attributes are the one exception.
**Fix:** Add one test that pins the rule, in `TemplateBindingErrorTests`: a template with
`bind-text=" hud.name "` on a `Label`, the key registered without the spaces, and the assert
`Assert.Contains("' hud.name '", errors[0].Message)`. Put the same sentence in the test's remarks so a
future editor sees the decision.
**Effort:** Low

### CR-5: "Trys" is a spelling error in four doc comments
**Severity:** LOW
**Rule:** AGENTS.md "Prose in docs and comments follows ASD-STE100"; the Quality Bar's ASD-STE100 line.
**File:** `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs:33,38,43,48`
**Problem:**

```csharp
/// <summary>Trys to bind the text of a label.</summary>
```

`Trys` is not an English word; the form is `Tries`. These four lines are the only `Trys` in `src/` and
`tests/` (grep: 4 hits, all in this file).
**Fix:** Replace each with `/// <summary>Tries to bind …</summary>`.
**Effort:** Low

### CR-6: The main file sits 2 lines under its own 400-line cap
**Severity:** LOW
**Rule:** Quality Bar "Each new source file stays under 400 lines"; Definition of Done item 11 of the
Verification Method.
**File:** `src/BrutalUi.Framework/Templates/TemplateBindings.cs:1-398`
**Problem:** The file is 398 lines. `Handle` alone runs lines 267-396 (130 lines), and it holds four
near-identical five-step blocks. Any next step — a fifth attribute, one more sentence in a message, the
Remarks growth that a review usually brings — breaks the rule and forces a restructure under time
pressure.
**Fix:** Make the class `partial` and move `Handle` with its `///` remarks to
`Templates/TemplateBindings.Handler.cs`. No behaviour change, no public-surface change; the Verification
Method item 11 check (`wc -l`) still passes with two files under 400 lines.
**Effort:** Medium

### CR-7: `AttachTo` retains every loader it takes, and nothing says so
**Severity:** LOW-MEDIUM
**Rule:** Brief angle 2 (Public API and DX: is anything surprising or missing?); the plan's "The object
keeps a small list of its loaders for the check".
**File:** `src/BrutalUi.Framework/Templates/TemplateBindings.cs:51,208-221`
**Problem:**

```csharp
/// <summary>The loaders that <see cref="AttachTo"/> took, for the check of a second attach.</summary>
private readonly List<TemplateLoader> _loaders = new();
...
_loaders.Add(loader);
loader.AddAttributeHandler(Handle);
```

The list only grows. `AttachTo` allows one object on many loaders, and the docs state it as a feature
(`technical-design.md:1619` "one object can attach to more than one loader"; the test
`TemplateBindingsApiTests.cs:157-160` proves it). Each loader also holds the delegate that captures this
object, so the two sides pin each other. A game that keeps one long-lived `TemplateBindings` and makes a
loader per panel instance holds every loader for the life of the object, and no public member removes one.
Neither the `AttachTo` doc (lines 198-207) nor section 11 warns about it.
**Fix:** Document the retention in the `AttachTo` remarks and in **The rules of the object.**: "The object
keeps a reference to each loader that it attaches to. Make one `TemplateBindings` object for each loader,
so the pair dies with the panel." Keep the list; the check is the point. If the many-loader case is not a
real need, replace the list with a single `TemplateLoader?` field and throw for any second loader — a
smaller API surface and no growth at all.
**Effort:** Low

### CR-8: The two setup faults report a message that does not name the cause
**Severity:** LOW-MEDIUM
**Rule:** Brief angle 4 (error quality) and A6; Quality Bar "a message that … names the fix".
**File:** `src/BrutalUi.Framework/Templates/TemplateBindings.cs:304-309`,
`tests/BrutalUi.Framework.Tests/Templates/TemplateBindingErrorTests.cs:184-200`
**Problem:** Both setup mistakes land on a message that points elsewhere.

1. A game that never calls `AttachTo` gets the loader's own fault, which lists the rows of the element and
   never names bindings — the behaviour that `ALoaderWithNoBindingsRejectsEachAttribute` pins (A6):
   `'bind-text' is not an attribute of Label. The attributes of Label are name, class, pickable, …`.
2. A game that attaches a second `TemplateBindings` set on the same loader gets `UnknownBinding` for a key
   that the second set *does* register:

```csharp
$"'{value}' is not a bound key. Register it with TemplateBindings.AddText before the load."
```

The advice is wrong in both cases, and case 2 is the trap that plan Risk 4 and Notes item 5 name. Neither
fix needs a code change, because A6 and A7 are settled decisions and a detection needs a Markup edit.
**Fix:** Add one sentence to **The rules of the object.** and to the class remarks: "A loader with no
attached bindings gives `UnknownAttribute` for the four names, with the element's own attribute list. A
second bindings set on one loader gives `UnknownBinding` for its keys, even when it registers them. Both
symptoms mean: make one loader for each bindings set, and call `AttachTo` before the load."
**Effort:** Low

### CR-9: The settled open point still reads open in the sibling plan
**Severity:** LOW
**Rule:** Brief angle 6 (epic integration: "Does the settled open point 9 of xml-element-loader read
correctly in the docs?").
**File:** `docs/features/markup/xml-element-loader/implementation.md:669`
**Problem:** The sibling plan still states the question as open and points the answer at this feature:

> **Open point for binding-attributes: the registry of the load.** … When that is not enough,
> binding-attributes can propose a state argument. Record the choice there.

This feature recorded the choice in `docs/features/markup/binding-attributes/implementation.md:290`
(Decision 1: "This settles open point 9 … the registry of the load is the registry of the attached
`TemplateBindings`") and in its `context.md` hand-off facts. The code matches: `AttachTo` captures the
object, `Load` takes no state argument, and the hook contract is unchanged. A reader of the sibling plan
alone cannot tell that it closed.
**Fix:** At `/update-feature` time, append one line to that Notes item: "Settled by markup/binding-attributes:
the registry of the load is the registry of the attached `TemplateBindings`; the hook contract did not
change." Do not rewrite the item.
**Effort:** Low

### CR-10: One slice on a slider and its readout label needs two keys, and no doc says why
**Severity:** LOW
**Rule:** Brief angle 2 (DX: anything surprising or unresolvable?); A4 and Q2 of the plan.
**File:** `tests/BrutalUi.Framework.Tests/Shared/BoundTemplate.cs:105-106`,
`src/BrutalUi.Framework/Templates/TemplateBindings.cs:239-244`
**Problem:** A4 makes a key hold one selector, so the commonest HUD pattern — a slider plus the number
that it shows — cannot use one key. The feature's own parity fixture invents a suffix to work around it:

```csharp
bindings.AddText("hud.volume-text", store, static s => s.Volume, "F1", CultureInfo.InvariantCulture);
bindings.AddValue("hud.volume", store, static s => s.Volume);
```

The duplicate throw tells the developer to "Use another key" but not that the readout wants a second key
with the format, so each team names the pattern its own way. The decision itself is right for the
framework: one key, one type, one clear `BindingTypeMismatch` message. The missing part is the guidance.
**Fix:** Add one sentence to **The four binding attributes.** in `docs/technical-design.md`: "One key holds
one selector, so a slider and the label that shows its value take two keys. Register the readout key with
the number overload of `AddText` and its format, for example `hud.volume` and `hud.volume-text`."
**Effort:** Low

## What's Already Good

1. **The package edge is exactly the epic rule.** `BrutalUi.Framework.csproj:8-9` references Core and
   Markup; `git diff --stat 5bbeec9 -- src/BrutalUi.Core src/BrutalUi.Framework/Panels samples
   tests/BrutalUi.Markup.Tests src/BrutalUi.Markup/BrutalUi.Markup.csproj Directory.Packages.props`
   prints nothing. Markup stays Core only (epic Q1), and the generator does not flow into Framework.
2. **The Markup blast radius is 8 lines in one file**, the two enum members with their doc lines and the
   two `GetKindName` arms (`TemplateError.cs:41-45,154-155`). Both members append at the end of a `byte`
   enum, so no existing value moves. A7 holds.
3. **The capture design fits the framework.** `AttachTo` adds the instance method `Handle` as one
   `AttributeHandler` (`TemplateBindings.cs:220`); no state argument, no static table, no change to the
   hook contract — the same shape as `CommandRegistry.Register`'s "one entry per registration".
4. **No reflection anywhere.** `grep -rnE 'System\.Reflection|Activator|MakeGenericType|GetType\(\)'
   src/BrutalUi.Framework/Templates` prints nothing. The generics close at the `Add*` call, and a virtual
   `TryBind` carries the fit test — Decision 2 delivered as planned.
5. **The handler matches D3 exactly**: element type, empty value, key, fit, bind, with one error per
   attribute and `return true` for all four names. It computes no line and no column; `AddError` forwards
   the position, so the errors cannot drift from the source text.
6. **The load path allocates at load time only.** `Encoding.UTF8.GetBytes(value)` and one binding per
   element; the entries hand the same store and selector to the same bind methods that C# code uses, so
   `Update` and `Draw` reuse the existing 0-byte binding pass. Both gates measure 100 frames at 0 bytes,
   and `TheMeasuredBodyDoesRealWork` stops an empty body from passing.
7. **The parity test pins behaviour, not shape.** `TemplateParityTests.cs:51-61` compares the two trees
   with `TreeCompare.FirstDifference` after each of 10 store versions, then asserts each element's
   `BindingCount` against an absolute count (`:103-109`) and the toggle's checked state (`:135-144`) —
   the two things the tree compare cannot see.
8. **The error tests read like the loader's own tests.** `AssertAttribute` recomputes each line and column
   from the template text (`TemplateBindingErrorTests.cs:239-258`), and each message assert checks the
   quoted text only, per the plan's test style.
9. **The argument checks follow the house convention.** Every `Add*` throws `ArgumentNullException` with
   the parameter name, then `CheckKey` throws `ArgumentException` with a constant message *before* the add,
   so the table keeps the first selector — the same fail-fast shape as `TemplateLoader.Register` and
   `CommandRegistry.Register`, each guarded by a test that reads `ParamName`.
10. **The docs landed where the plan said.** Section 3 line 125 ("It references Core and Markup"),
    section 10 lines 1097 and 1121, section 11 lines 1535-1628 (the block, the four-attribute table, the
    entry shape, the order of checks, the message table, the rules), section 14 line 1903 for the
    twenty-second gate, and one `CHANGELOG.md` line (56). The doc template (lines 1540-1543) equals the
    demo-test template (`TemplateBindingDemoTests.cs:35-38`).
11. **The house C# rules hold.** File-scoped namespaces, `BrutalUi.Framework` for the new folder, `u8`
    literals for every template, XML docs on each public member, and both new source files under 400 lines.
    The only new public type is `TemplateBindings`; the four entries and `TemplateBindingEntry` are
    `internal`.
12. **The 06:15 duplicate-agent incident is closed and provable.** The live file matches the clean hash
    `514499b699b2313b3c0b793b12fb528c` at 398 lines, with the `_loaders.Contains` check present
    (`TemplateBindings.cs:212`), and `context.md` records the recovery and the post-restore gate PASS.

## Recommended Fix Order

| # | CR-N | Severity | Effort | One-line action |
|---|------|----------|--------|-----------------|
| 1 | CR-5 | LOW | Low | Fix the four `Trys` to `Tries` in `TemplateBindingEntry.cs`. |
| 2 | CR-3 | LOW-MEDIUM | Low | Name all four attributes in `technical-design.md:1533`. |
| 3 | CR-2 | LOW-MEDIUM | Low | Give the empty-value row its three per-attribute example keys. |
| 4 | CR-1 | MEDIUM | Low | Add `bind-checked=""` and `bind-value=""` to `AnEmptyValueIsAnInvalidValueError`. |
| 5 | CR-4 | LOW-MEDIUM | Low | Add the spaced-key test that pins the no-trim rule. |
| 6 | CR-8 | LOW-MEDIUM | Low | State the two setup-fault symptoms in the class remarks and section 11. |
| 7 | CR-7 | LOW-MEDIUM | Low | Document the loader retention, or narrow `AttachTo` to one loader. |
| 8 | CR-10 | LOW | Low | Add the two-key readout pattern to section 11. |
| 9 | CR-9 | LOW | Low | Mark Notes item 9 of xml-element-loader as settled, at `/update-feature`. |
| 10 | CR-6 | LOW | Medium | Split `Handle` into a second partial file, before the next attribute. |

## Progress

- [x] CR-1: Two empty-value fault paths have no test
- [x] CR-2: The message table names one example key for three different messages
- [x] CR-3: The attribute-hook paragraph still names two attributes
- [x] CR-4: The no-trim decision is documented and untested
- [x] CR-5: "Trys" is a spelling error in four doc comments
- [x] CR-6: The main file sits 2 lines under its own 400-line cap
- [x] CR-7: `AttachTo` retains every loader it takes, and nothing says so
- [x] CR-8: The two setup faults report a message that does not name the cause
- [x] CR-9: The settled open point still reads open in the sibling plan
- [x] CR-10: One slice on a slider and its readout label needs two keys, and no doc says why
