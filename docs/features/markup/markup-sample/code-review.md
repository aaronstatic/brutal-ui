# markup/markup-sample - Code Review

**Reviewed:** 2026-11-19
**Reviewer:** Agent (cross-phase review, Orca worker `task_91975c56455b`)
**Skills Applied:** `review-feature` workflow. The Vercel React rules do not apply to C#. The review uses the same priority scale against the project rules of `AGENTS.md` (allocation, API shape, docs), the plan decisions in `implementation.md`, and general C# correctness.
**Scope:** `git diff 7948a78 HEAD -- src samples tests` (phases 1 to 4). Phase 4 is docs only.
**Status:** Resolved - 0 findings open, 6 resolved (review-fix run 2026-11-19; see the Fix Record at the end)

---

## Summary

The feature is sound. The `AddFactory` hook is small, it keeps one name table, and its tests pin each rule of Decisions 1 to 4. The sample loads its state panel and its six records from XML, and the twenty-third gate proves 0 bytes on the loaded shape. The review found no allocation fault and no API break.

The one real gap is the open point of Notes item 5: a slot typo on the factory layout of `HelloDefinitions.xml` passes the load before the window and throws after the window opens. The sample then breaks its own A3 rule.

| Severity | Count |
|---|---|
| CRITICAL | 0 |
| HIGH | 0 |
| MEDIUM | 1 |
| LOW | 5 |

---

## Findings

### CR-1: A slot typo in HelloDefinitions.xml escapes the before-window check
**Severity:** MEDIUM
**Rule:** `correctness` (A3 of the plan, Decision 3, Notes item 5)
**File:** `samples/HelloPanel/HelloMarkup.cs:96-120`, `samples/HelloPanel/HelloModes.cs:216`, `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs:217`
**Problem:** `hello.layout` names a factory, so the resolve pass skips the slot check of `hello.main` and `hello.detail` (Decision 3). A typo such as `<ui:Slot name="botom">` gives a succeeded result. `HelloMarkup.Load` returns it, and `Run` calls `Glfw.Init`, creates the window, the Vulkan context, and the ImGui context. Then `Mount` calls `RegisterTo`, and `UiManager.Register(ModeDefinition)` throws `ArgumentException` ("A mode names a slot that its layout holds not."). The `catch` in `Main` prints `HelloPanel: System.ArgumentException: ...` with a stack trace and returns 1. A3 says that each definition fault prints one line with no prefix and gives exit code 1 with no GLFW call. The error has no file, line, or column. The phase 2 headless checks do not cover a slot fault, so no check shows the gap.

```csharp
// HelloMarkup.Load: the result is "succeeded" with a bad slot name on a factory layout.
DefinitionLoadResult result = definitions.Load(definitionsText, DefinitionsFileName);
...
return check.Succeeded && result.Succeeded ? result : null;
```

**Fix:** Check the slots in `HelloMarkup.Load`, before the window. `HelloScreen.BuildLayout()` needs no texture and no GPU, so the sample can build the layout root one time and test each placement slot of each loaded mode with `SlotContainer.Find`. Print one line per miss, in the form of the other lines, and return `null`.

```csharp
Element layout = HelloScreen.BuildLayout();
bool slotsOk = true;
foreach (ModeDefinition mode in result.Modes)
{
    for (int s = 0; s < mode.SlotCount; s++)
    {
        ReadOnlySpan<byte> slot = mode.GetSlotName(s);
        if (SlotContainer.Find(layout, slot) is null)
        {
            errors.WriteLine($"{DefinitionsFileName}: UnknownSlot: '{Encoding.UTF8.GetString(slot)}' is not a slot of '{LayoutFactoryName}'.");
            slotsOk = false;
        }
    }
}
```

Add a fifth headless check (slot typo: one line, no GLFW line, exit code 1). A Framework fix (an `AddFactory` overload with a slot list) stays out of scope (Decision 3 rejected it as YAGNI). Record the choice in `context.md`.
**Effort:** Low

### CR-2: The panel and layout resolve branches repeat the same lookup
**Severity:** LOW
**Rule:** `maintainability` (duplication)
**File:** `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs:72-93`, `:123-142`
**Problem:** Step 2 and step 3 hold the same 20-line block: the missing-name check, `TryGetTemplate`, `TryGetFactory`, and the `UnknownTemplate` error. Two branches have empty bodies that hold only a comment. A third record kind or a change of the lookup order must change both copies in the same way.

```csharp
else if (_loader.TryGetTemplate(entry.Template.Text!, out text))
{
    // The template text path.
}
else if (_loader.TryGetFactory(entry.Template.Text!, out factory))
{
    // The factory path. The load calls no factory (Decision 3).
}
else
{
    AddError(DefinitionErrorKind.UnknownTemplate, ...);
    faulty = true;
}
```

**Fix:** Extract one helper that returns `false` and adds the error for an unknown name.

```csharp
/// <summary>Looks up a template value: the text first, then the factory (Decision 1).</summary>
private bool TryResolveTemplate(DefinitionValue value, out byte[]? text, out Func<PanelContext, Element>? factory)
{
    factory = null;
    if (_loader.TryGetTemplate(value.Text!, out text) || _loader.TryGetFactory(value.Text!, out factory))
    {
        return true;
    }

    AddError(DefinitionErrorKind.UnknownTemplate, $"'{value.Text}{UnknownTemplateMessage}", value.Line, value.Column);
    return false;
}
```

**Effort:** Low

### CR-3: The gate copies the sample load flow by hand
**Severity:** LOW
**Rule:** `test-fidelity` (Decision 10)
**File:** `tests/BrutalUi.Framework.Tests/Templates/SampleMarkupAllocationScene.cs:131-175`, `samples/HelloPanel/HelloMarkup.cs:86-106`
**Problem:** `BuildScene` repeats the steps of `HelloMarkup.Load`: the bindings, `AttachTo`, the generated register call, the check load, `AddTemplate`, three `AddFactory` calls, and `AddActionMap`. The three factory names are string literals in the test and constants in the sample. The XML files are linked, so a change in a file still reaches the gate. A change of the C# flow does not reach it, for example a new step, a changed order, or a new binding key that only the sample adds. Thus the gate can pass while the sample flow differs.
**Fix:** Move the GPU-free part of the flow into one file with no reference to `HelloState` or `HelloModes`, for example `HelloMarkup.Configure(TemplateLoader, DefinitionLoader, CommandRegistry, Store<int>, UiActionMap, Func<PanelContext, Element> widgets, ... info, ... layout)`. Link that file into the test as `HelloRow.cs` is linked, and call it from both `HelloMarkup.Load` and `BuildScene`. Or, at minimum, link a small `HelloMarkupNames.cs` that holds the constants, so the names have one source.
**Effort:** Medium

### CR-4: A stale comment names the removed C# state row
**Severity:** LOW
**Rule:** `docs-accuracy`
**File:** `samples/HelloPanel/Program.cs:405-406`
**Problem:** The comment says "The state builds the store, the commands, the action map, and the state row one time, at build time." Phase 3 removed `HelloState.Row` and its buttons (A9). `HelloState.xml` now builds the state row inside `Mount`. A reader who copies the sample looks for a row that does not exist.
**Fix:** Change the sentence to "The state builds the store, the commands, and the action map one time, at build time. `HelloState.xml` builds the state row inside `HelloModes.Mount`."
**Effort:** Low

### CR-5: No test pins Decision 2 for a factory layout
**Severity:** LOW
**Rule:** `test-coverage`
**File:** `tests/BrutalUi.Framework.Tests/Templates/DefinitionFactoryTests.cs:107-111`
**Problem:** `AFactoryPanelAndAFactoryLayoutLoadAndMount` checks `Assert.Same(widgetsFactory, result.Panels[0].CreateRoot)` for the panel only. The layout path in step 3 has its own `factory ?? CreateFactory(...)` line. A plant that wraps the layout delegate keeps all five tests green, because the wrapper still gives a root with the two slots.
**Fix:** Add one line to the same test:

```csharp
Assert.Same(layoutFactory, result.Layouts[0].CreateRoot);
```

**Effort:** Low

### CR-6: HelloModes.Mount has no guard against a second call
**Severity:** LOW
**Rule:** `api-contract`
**File:** `samples/HelloPanel/HelloModes.cs:205-216`
**Problem:** A second `Mount` call registers the panels again, and `UiManager.Register(PanelDefinition)` throws `ArgumentException` after zero records. The message names a duplicate panel, not the real fault (a second mount). Also `Main` and `Detail` stay `null` until `Mount` runs, so a `hello.mode` run before `Mount` always picks `hello.detail`. The sample calls `Mount` one time, so no run fails today. But the class remarks say "Copy this file to start a screen with modes of your game", so the contract should be clear.
**Fix:** Add a private `bool _mounted`. Throw `InvalidOperationException("Mount runs one time.")` on a second call. State the one-call rule and the `Main`/`Detail` null state in the `<remarks>` of `Mount`.
**Effort:** Low

---

## What's Already Good

- **One name table, checks in parity.** `AddFactory` copies the checks of `AddTemplate` in the same order (`name`, `factory`, empty name, both tables), and each add checks both dictionaries before the write. So a throw leaves the tables unchanged (`DefinitionLoader.cs:145-159`).
- **The load runs no game code.** The resolve pass gives the record the delegate as is and never calls it. `Assert.Same` pins this for panels, and the call counter pins "no call at load" for layouts.
- **Source-compatible API change.** `RegisterTo` changed from `void` to `IReadOnlyList<PanelController>`. Each existing call compiles with no edit. The `ReadOnlyCollection` wrap stops a caller cast-and-write.
- **No allocation in a frame.** The bindings use a `static` selector field (`s_score`). The command handlers are static fields with a target. `Mount` calls `EnsureRoot` on each controller before the first frame, so no factory runs in a frame. The twenty-third gate measures 0 bytes for both a click and a mode toggle, and plants 4 and 5 prove the gate can fail.
- **The gate tests the real body.** `TheMeasuredBodyDoesRealWork` makes a no-op body fail. The toggle body alternates the mode by parity, so each measured frame runs one real diff.
- **The load runs before the window.** The managed objects move ahead of `Glfw.Init` (Decision 6), and the check load of `HelloState.xml` (Decision 7) catches template faults with a position, before any native call.
- **Non-null mount.** `HelloMount` uses `required` init members, so the nullable analysis needs no `null!`.
- **Error message in one place.** `UnknownTemplateMessage` holds the fixed text, and it names both add methods.
- **Linked files over a project reference.** The test links `HelloRow.cs` and the XML files and runs the generator on a second assembly (Decision 10), with no reference to the exe.

---

## Recommended Fix Order

| Order | Finding | Severity | Effort | Why this order |
|---|---|---|---|---|
| 1 | CR-1 | MEDIUM | Low | It closes a real gap in the A3 promise of the sample, with a few lines and one new headless check. |
| 2 | CR-5 | LOW | Low | One assert line closes a plant hole in the Framework API. |
| 3 | CR-4 | LOW | Low | One comment edit. The sample is the copy source for a game. |
| 4 | CR-2 | LOW | Low | A local refactor with full test cover. Do it before the next change of the resolve pass. |
| 5 | CR-6 | LOW | Low | A guard and two doc lines in the sample. |
| 6 | CR-3 | LOW | Medium | A file split in the sample and a link line in the test project. It has the most churn. |

---

## Progress

- [x] CR-1: A slot typo in HelloDefinitions.xml escapes the before-window check
- [x] CR-2: The panel and layout resolve branches repeat the same lookup
- [x] CR-3: The gate copies the sample load flow by hand
- [x] CR-4: A stale comment names the removed C# state row
- [x] CR-5: No test pins Decision 2 for a factory layout
- [x] CR-6: HelloModes.Mount has no guard against a second call

## Fix Record

**Fixed:** 2026-11-19, review-fix orchestrator (Orca worker `task_ff9747589df1`).

- Group 1 (CR-1, CR-2, CR-6): the sample moved the register step before the window —
  `HelloModes.Register(DefinitionLoadResult)` runs inside `HelloMarkup.Load` after both
  loads pass, so a slot typo on the factory layout prints one error line and exits 1 with
  no window (headless proof: `HelloDefinitions.xml: A mode names a slot that its layout
  holds not. (Parameter 'mode')`, exit 1); one `TryResolveTemplate` helper now serves
  both resolve loops; `Register` and `Mount` carry one-time guards that throw
  `InvalidOperationException` naming the double call. Brief:
  `.beast-mode/briefs/markup/markup-sample/review-fix-g1.md`.
- Group 2 (CR-3, CR-4, CR-5): new `samples/HelloPanel/HelloMarkupNames.cs` single-sources
  the shared names, linked into the Framework tests, and the scene and `HelloMarkup`
  reference it with no name literals left in either; the `Run` comment names the XML
  state row at `HelloModes.Mount`; the factory test now pins both records
  (`Assert.Same` on the panel and the layout delegate). Brief:
  `.beast-mode/briefs/markup/markup-sample/review-fix-g2.md`.
- Verifier (brief `.beast-mode/briefs/markup/markup-sample/review-fix-verify.md`): PASS,
  Demo: PASS. Build `0 Warning(s)`; suite total 3203, failed 0, skipped 3; per-class
  5/0, 3/0, 12/0; headless checks 1 and the typo check and the clean run give the
  expected lines and exit 1; `package-test: PASS Gauge level 0.5`; worktree at HEAD
  `de246bb` with no commit. Windowed smoke and the in-flow second-`Mount` demo stay
  pending on a host with a window.
- Open note for the coordinator (docs only, out of this run's edit scope): `README.md`
  line 155 and `docs/technical-design.md` section 11 (the sample-flow paragraph, lines
  1827-1828) should say the register step now also runs before `Glfw.Init`.
