# markup/binding-attributes - Context & Decisions

**Epic:** markup (feature 4 of 6)
**Last Updated:** 2026-11-17 07:30
**Current Phase:** All three phases complete — cross-phase review closed, feature Ready for Review
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Plan approved (`implementation.md`, 3 phases, 18 tasks, 8 plants)
- ✅ Docs scaffolded by `/start-feature` (`tasks.md`, `context.md`, `inbox.md`)
- ✅ Branch `feat/markup/binding-attributes` created from `main` (base commit `5bbeec9`)
- ✅ Phase 1 group 1 complete (tasks 1.1 to 1.6): Framework references Markup, `TemplateBindings` + `NumberTextEntry` exist, `TemplateBindingDemoTests` 4/4, docs section 11 holds **The binding attributes.**, plants 1 and 2 fail the named tests. Solution 3147, Framework 336, build 0 warnings.
- ✅ Phase 1 gate verified by an independent verifier agent: `PASS` on all 13 steps, `Demo: PASS` (total 4, failed 0; the click runs `game.pause` once and the store write gives `1,500`).
- ✅ Phase 2 group 1 complete (tasks 2.1 to 2.3): `StringTextEntry<T>`, `CheckedEntry<T>`, and `ValueEntry<T>` exist; `AddText<T>` (string), `AddChecked<T>`, and `AddValue<T>` exist; `Handle` binds `bind-text` on a `TextField`, `bind-checked` on a `Toggle`, and `bind-value` on a `Slider`. `TemplateWidgetBindingTests` 4/4. Risk 3 confirmed (both `AddText` calls pick their intended overload). Plant 3 fails exactly `OneStringKeyServesALabelAndATextField`. Solution 3151, Framework 340, build 0 warnings.
- ✅ Phase 2 group 2 complete (tasks 2.4 to 2.6): `Shared/BoundTemplate.cs` holds the template and its C# twin; `TemplateParityTests` 2/2; `TemplateBindingAllocationTests` 3/3, both gates 0 bytes; `technical-design.md` sections 10, 11, and 14 hold the phase-2 text (the four-attribute table, the entry shape, the rules of the object, and the twenty-second gate). Plants 4 and 5 fail the named tests (plant 4 also fails `TemplateBindingAllocationTests.TheMeasuredBodyDoesRealWork`; see the 2026-11-17 05:00 phase 2 group 2 session note). Solution 3156, Framework 345, build 0 warnings.
- ✅ Phase 3 group 1 complete (tasks 3.1 to 3.4): `TemplateErrorKind` holds `UnknownBinding` and `BindingTypeMismatch`
  (with their two doc lines and two `GetKindName` arms), each entry gives `SelectorKind`, and `Handle` reports each
  fault in the order type → empty → key → fit → bind. `TemplateBindingErrorTests` 6/6. Plants 6 and 7 fail exactly the
  named tests. Solution 3162, Framework 351, Markup 666, build 0 warnings.
- ✅ Phase 3 group 2 complete (tasks 3.5 and 3.6): each `Add*` method checks its key (an empty key and a second key each
  throw `ArgumentException` before the add), and `AttachTo` keeps its loader list and throws for a second attach.
  `TemplateBindingsApiTests` 4/4. `technical-design.md` section 11 holds the order of the checks, the four messages, and
  the rules of A4 and the attach; the `TemplateErrorKind` block and the error table hold the two new kinds; section 10
  names the `command` attribute in the button bind rules. `CHANGELOG.md` holds the feature line. Plant 8 fails exactly
  `ASecondAttachToOnOneLoaderThrows`. The package test prints `package-test: PASS Gauge level 0.5`. Solution 3166,
  Framework 355, Markup 666, build 0 warnings.

- ✅ Phase 3 gate verified by an independent verifier agent after the recovery below: `PASS` on all 14 steps, `Demo: PASS`.
- ✅ Cross-phase review (`/review-feature`, C# library rule set): 10 findings (0 CRITICAL, 0 HIGH, 1 MEDIUM, 5 LOW-MEDIUM, 4 LOW), all 10 fixed and verified. `code-review.md` reads `Resolved - 10 findings, 10 resolved`.
- ✅ Review fix groups 1 to 5: the partial split of `TemplateBindings` (276 + 171 lines), the two setup-fault messages that now name both causes, the loader-retention docs, the missing empty-value coverage for `bind-checked` and `bind-value`, the new no-trim test for Decision 5, the docs that quote the shipped message text, the four "Tries to bind" comments, and the settled Notes item 9 in the xml-element-loader plan. Plants 9, 10, and 11 each fail exactly their named test.
- ✅ Review pass verified by one independent verifier: `PASS` on all 15 gates, `Demo: PASS`. Final state: build `0 Warning(s)` / `0 Error(s)`, solution **3167** / failed 0 / skipped 3, Framework **356** (+24 over the 332 baseline), Markup 666, `package-test: PASS Gauge level 0.5`, Markup blast radius `TemplateError.cs` alone (8 lines).

**What's Next:**
- 📋 A person tests the `## Needs Human Verification` items, then commits, pushes `feat/markup/binding-attributes`, and opens the PR. This run made no commit and no push on purpose.
- ⏳ `markup/definition-loader` (feature 5) builds on `TemplateBindings`, and `markup/markup-sample` (feature 6) closes the epic. See the two consumer notes in the plan's **What definition-loader inherits** and **What markup-sample inherits**.
- ⏳ Follow-up outside this feature, from Notes item 7: the packed Framework nuspec carries `exclude="Build,Analyzers"`, so markup-sample or the README must tell a game with its own `[TemplateElement]` types to reference `BrutalUi.Markup` too.

**Blockers:**
- None

---

## Baseline

- Base commit: `5bbeec9e242447efd81d82023a7e9202b34a6fa8` (`git rev-parse HEAD`, verified by task 1.1 at run time).
- `git status --porcelain` at start: only the four untracked docs files of this feature (`context.md`, `implementation.md`, `inbox.md`, `tasks.md`). The source tree is clean.
- Real baseline, run on the untouched tree (task 1.1, log `artifacts/logs/p0-baseline.log`): solution total 3143, failed 0, skipped 3 (Glfw, no display). `BrutalUi.Framework.Tests` total 332, failed 0. These match the planning baseline.
- Phase 1 expects Framework 336 and solution 3147 (+4). The deltas of the later phases count from 332.

## Plan digest for phase 1 (from `implementation.md`)

- **Public API (end state).** `public sealed class TemplateBindings` in `BrutalUi.Framework`: the constructor takes a `CommandRegistry` (throws `ArgumentNullException` for `null`); `AddText<T>(string, Store<T>, Func<T, string?>)`; `AddText<T, TValue>(string, Store<T>, Func<T, TValue>, string? format = null, IFormatProvider? provider = null) where TValue : IUtf8SpanFormattable`; `AddChecked<T>(string, Store<T>, Func<T, bool>)`; `AddValue<T>(string, Store<T>, Func<T, float>)`; `AttachTo(TemplateLoader)`. The `Add*` methods return `void`. The two `AddText` overloads copy the shape of the two `LabelBindings.BindText` overloads. Phase 1 writes the constructor, the number `AddText`, and `AttachTo` only.
- **The Entry Shape.** `TemplateBindingEntry` is internal abstract with four virtual `TryBind` overloads (`Label`, `TextField`, `Toggle`, `Slider`), each `false` in the base. A sealed generic entry overrides the targets it fits. The generic types close at the `Add*` call: no reflection, no `Activator`, no `MakeGenericType`, no `GetType()`. `NumberTextEntry<T, TValue>` overrides `TryBind(Label)` only (A5: a number key serves a Label only). `SelectorKind` comes in phase 3.
- **The Handler.** `AttachTo` adds the instance method `Handle` as one `AttributeHandler`; the method group captures the object. `Handle` switches on the name with an ordinal `switch`. Another name returns `false`. End state: the four names always return `true` and add at most one error, in the order type → empty → key → fit → bind. Phases 1 and 2 return `false` for each fault case instead, so the loader gives its own `UnknownAttribute`. `command` calls `button.BindCommand(_commands, Encoding.UTF8.GetBytes(value))` (D6). The registry finds the command at each click, so `command` has no key check (A1).
- **The Rules of the Object.** One handler per `AttachTo`; a second `AttachTo` on the same loader throws (phase 3 adds the loader list and the check). One bindings set per loader (the first handler takes the four names). An `Add*` call after `AttachTo` is valid: the handler reads the table at each load. One key holds one selector (phase 3 adds the throws). The bind and the UTF-8 copy allocate at load time only; `Update` and `Draw` allocate nothing.
- **Notes item 2.** Section 11 of `technical-design.md` keeps its FIRST template unchanged; the `bind-text` variant lives in the NEW **The binding attributes.** block, and the demo test copies that block character for character (LF line ends).
- **Notes item 3.** The plan also edits line 125 of `technical-design.md` (section 3), because epic Q1 makes the "references Core only" line wrong.
- **Notes item 7.** The default pack of a project reference can mark the dependency with `exclude="Build,Analyzers"`. Task 1.2 records the real attribute; this feature changes no pack setting.

## Tool notes (phase 1, with the orchestrator steer of 2026-11-17)

- Export `DOTNET_CLI_HOME=/tmp/dui-cli-home MSBUILDDISABLENODEREUSE=1 DOTNET_NOLOGO=1` in every chain.
- Build: `dotnet build brutal-ui.sln -nodeReuse:false -p:NuGetAudit=false 2>&1 | tail -n 30`. `NU1900` audit noise is machine-local; a warning still fails the gate.
- Test: `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` with NO msbuild flags (they break the MTP handshake: exit 5, "Zero tests ran").
- One class after a build: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`.
- Plants: copy the file to `src/BrutalUi.Framework/obj/plantscratch/`, change the working file, run the solution tests into a workspace log, check the `error CS` count is 0 first (a plant that does not compile leaves the old binary, and the old binary passes), expect a non-zero exit, restore with `cp`, check with `cmp`, rerun once on exit 134.
- Do not run `scripts/test-linux.sh`: `Xvfb` segfaults in this sandbox. Run pack steps by hand when a task needs them.
- **Untracked new files and the `git diff` checks of the Verification Method.** This run makes no commit, so `git diff 5bbeec9 -- src` shows nothing for the NEW files (`Templates/`, the new test files). Do not stage them: verify such a check by reading the new file directly (for example `grep -n 'public sealed class TemplateBindings' src/BrutalUi.Framework/Templates/TemplateBindings.cs`). A `git diff --stat 5bbeec9 -- src/BrutalUi.Markup` style check stays valid, because it targets tracked Markup files, so task 3.1's "one file only" check works as written.
- **Pack and `DOTNET_CLI_HOME` (phase 1 verifier proof).** With `DOTNET_CLI_HOME=/tmp/dui-cli-home`, the global-packages folder becomes `/tmp/dui-cli-home/.nuget/packages`, which holds no `Ahwoo.Brutal.*` packages, and `restore` writes that path into every `obj/project.assets.json`. Solution-level `dotnet pack brutal-ui.sln --no-build` then exits 1 with `NETSDK1064: Package Ahwoo.BRUTAL.Core, version 2026.10.1 was not found`, reported on `src/BrutalUi.Debugger/BrutalUi.Debugger.csproj` (Debugger reaches it through `BrutalUi.ImGui → Ahwoo.BRUTAL.ImGui`, a graph that already existed at base `5bbeec9`). It is not a fault of this feature. Fix for any later pack or PackageTest run: `dotnet restore brutal-ui.sln` WITHOUT the `DOTNET_CLI_HOME` override first (that repoints the assets to `~/.nuget/packages`, which holds the `Ahwoo` packages), then pack and run `tests/BrutalUi.PackageTest`. Task 3.6 must follow that order. State of the tree now: the verifier's plain `dotnet restore` already repointed the assets, and `artifacts/packages/` holds fresh `0.1.0-alpha` nupkgs for all six packages.

## Hand-off facts from the markup siblings (read-only survey, 2026-11-17)

- **Hook contract (xml-element-loader).** `AttributeHandler` is `delegate bool AttributeHandler(Element element, string name, string value, AttributeContext context)`. Handlers run AFTER the element's table rows, in add order; the first `true` takes the attribute; no handler takes it gives `UnknownAttribute` listing the valid names. `TemplateLoader.AddAttributeHandler(AttributeHandler)` is the only public hook entry.
- **`AttributeContext` is a `readonly ref struct`.** It cannot be stored in a field or captured in a closure; it is valid only during the handler call. It holds `Name`, `Value`, `Line`, `Column`, `SourceName`, `AddError(TemplateErrorKind, string)`, and the `TryRead*` reads (`TryReadInteger` and `TryReadKeyword` came from element-generator). `AddError` takes NO line or column: it forwards the attribute's own position (first character of the attribute name, UTF-16 columns).
- **`TemplateError`** is a public `readonly struct` with an INTERNAL constructor: outside Markup, errors go only through `AttributeContext.AddError`. `ToString()` prints `hud.xml(3,59): UnknownAttribute: <msg>`. `TemplateErrorKind : byte` order today: `NotWellFormed, UnknownElement, UnknownAttribute, InvalidValue, MissingAttribute, UnknownTexture, UnexpectedText, UnexpectedChild` — the two new members go after `UnexpectedChild`.
- **This feature settles xml-element-loader Notes item 9** ("the registry of the load"): the registry of the load is the registry of the attached `TemplateBindings`. No state argument on `Load`; the hook contract stays unchanged.
- **`TreeCompare.FirstDifference`** compares delegate invocation counts, not identity, and does NOT compare `BindingCount` — so task 2.4 asserts `BindingCount` by hand. A `bind-text` addition is invisible to a tree compare; a `command` handler counts like `OnClicked`.
- **Framework bind surface (as it really is in `src/`).** `CommandRegistry.Register(ReadOnlySpan<byte> name, CommandHandler handler, object? target)`; `CommandHandler` is `delegate void CommandHandler(object? target)`. `BindCommand(this Button, CommandRegistry, ReadOnlySpan<byte>)` throws `ArgumentNullException` for a null button/registry and `ArgumentException` for an empty name. `LabelBindings.BindText<T,TValue>(this Label, Store<T>, Func<T,TValue>, string? format = null, IFormatProvider? provider = null) where TValue : IUtf8SpanFormattable` and `LabelBindings.BindText<T>(this Label, Store<T>, Func<T,string?>)`; `TextFieldBindings`-style `BindText<T>(this TextField, Store<T>, Func<T,string?>)`; `WidgetBindings.BindChecked<T>(this Toggle, Store<T>, Func<T,bool>)`; `WidgetBindings.BindValue<T>(this Slider, Store<T>, Func<T,float>)`. No key→selector registry exists yet: this feature owns that layer.
- **Generator/pack state (element-generator).** `BuiltInElements.cs` (268 lines) holds the eight `[TemplateElement]` declarations plus factories, base rows moved to `BaseRows.cs`, and the loader constructor calls the generated `RegisterBrutalUiMarkupElements()`. An analyzer from a `ProjectReference` does NOT flow to the referencing project (Notes item 8), which is what task 1.2 checks. Six `0.1.0-alpha` packages; the PackageTest prints `package-test: PASS Gauge level 0.5`.
- **Command conventions that the plan does not state.** Build with `dotnet build brutal-ui.sln -nodeReuse:false -p:NuGetAudit=false 2>&1 | tail -n 30` (`NU1900` audit warnings are machine noise; a warning still fails the gate). NEVER pass `-nodeReuse:false` or `-p:NuGetAudit=false` to `dotnet test --solution brutal-ui.sln` — it breaks the MTP handshake (exit 5, "Zero tests ran"). `element-generator` moved the CLI home to `artifacts/dotnet-cli-home` with `DOTNET_NOLOGO=1`; either value of `DOTNET_CLI_HOME` works as long as one run is consistent. Run `dotnet build-server shutdown` after any generator-affecting change. Plants: `obj/plantscratch/` or `artifacts/logs/`, check the `error CS` count is 0 first, expect a non-zero exit, restore with `cp` and check with `cmp`, rerun once on exit 134.
- **Environment noise.** `Xvfb` segfaults in this sandbox, so `scripts/test-linux.sh` fails environmentally at the `smoke` step; that is not a fault of this feature. Run the `pack` / `package-restore` / `package` steps of that script by hand instead (task 3.6).
- **element-generator Notes item 10:** any NEW public settable property on a Core element adds a generated row and fails `BuiltInRowListTests` by design. This feature adds no Core property.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/BrutalUi.Framework.csproj` - gains the project reference to `BrutalUi.Markup` (task 1.2)
- `src/BrutalUi.Framework/Templates/TemplateBindings.cs` - the one new public type: constructor, `AddText` (2 overloads), `AddChecked`, `AddValue`, `AttachTo`, private `Handle`
- `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs` - internal abstract entry + 4 sealed generic entries
- `src/BrutalUi.Markup/Template/TemplateError.cs` - the only Markup edit: `UnknownBinding`, `BindingTypeMismatch`, two `GetKindName` arms

### Related Files
- `src/BrutalUi.Markup/Template/TemplateLoader.cs` - `AddAttributeHandler`, the hook contract (unchanged)
- `src/BrutalUi.Framework/Commands/CommandBindings.cs`, `src/BrutalUi.Framework/Binding/LabelBindings.cs`, `src/BrutalUi.Framework/Binding/WidgetBindings.cs` - the bind methods the entries call
- `tests/BrutalUi.Framework.Tests/Shared/BoundTree.cs` - `HudState`, `Next`, `ViewportCenter`
- `tests/BrutalUi.Framework.Tests/Binding/BoundTreeAllocationTests.cs` - the gate model
- `tests/BrutalUi.Markup.Tests/Template/AttributeHookTests.cs` - the column helper for error asserts
- `docs/technical-design.md` - sections 3, 10, 11, 14
- `docs/features/markup/binding-attributes/implementation.md` - the plan of record

---

## Important Decisions

Recorded in full in `implementation.md` (Decisions 1 to 7). The ones an agent must honor per phase:

### D1: `TemplateBindings` captures its state in the hook handler
**Decision:** one class holds the `CommandRegistry` and the key table; `AttachTo` adds its instance method `Handle` as one `AttributeHandler`.
**Impact:** the Markup hook contract does not change (A7); no state argument on `Load`.

### D2: Abstract entry with one virtual `TryBind` per target type
**Decision:** `TryBind(Label)`, `TryBind(TextField)`, `TryBind(Toggle)`, `TryBind(Slider)`, each `false` in the base; sealed generic entries override the targets they fit.
**Impact:** no reflection, no `Type` test; a `false` result is the `BindingTypeMismatch` case.

### D3: One handler, fixed order of checks
**Decision:** element type → empty value → key → fit → bind; at most one error per attribute.
**Impact:** phase 3 replaces the `false` returns of phases 1 and 2 with this order.

### D4: Second `AttachTo` on one loader throws; an add after `AttachTo` is valid
**Impact:** the object keeps a small `List<TemplateLoader>`; the handler reads the table at each load.

### D5: `string` key with `StringComparer.Ordinal`, value used as is
**Impact:** no trim; `bind-text=" k "` gives `UnknownBinding` with the spaces in the quote (Notes item 6).

### D6: `command` calls `button.BindCommand(_commands, Encoding.UTF8.GetBytes(value))`
**Impact:** load-time allocation only; the click path is the C# bind path (0 bytes).

### D7: Tests reuse `BoundTree.HudState` and a shared `Shared/BoundTemplate.cs`
**Impact:** one template serves the parity test and the allocation gate.

---

## Gotchas & Learnings

- `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` exported in every command; `-nodeReuse:false` on `dotnet build` only. A warning fails the build.
- Pass a template as a `u8` literal to `Load(ReadOnlySpan<byte>, string)`; `BRUTALCORE0011` rejects a `string` literal for `Load(string, string)`.
- Plants go in `obj/plantscratch/` copies, checked with `error CS` count 0 first; a test host can exit 134 once, rerun before treating it as a fault.
- `/tmp` is not shared between the tool calls of one agent: write plant logs inside the workspace.
- Only the new files hold both `using BrutalUi.Framework` and `using BrutalUi.Markup` (Risk 2, CS0104).
- The tests never change `CultureInfo.CurrentCulture`; they pass the provider to `AddText` (Risk 7).
- The two `AddText` overloads never collide: `string` does not implement `IUtf8SpanFormattable`, so a `string`
  selector leaves only the string overload, and an `int` selector leaves only the number overload. A 3-argument call
  therefore needs no cast and gives no `CS0121` (Risk 3, confirmed in phase 2 group 1).
- A template element needs its required create attributes before a binding attribute works: `Slider` requires `max`,
  and `TextField` requires `capacity`. A missing one fails the load for a reason unrelated to the binding.
- A store write of a `Muted` or `Volume` slice never calls `Toggle.CheckedChanged` or `Slider.ValueChanged`, because
  the binding uses the code setter. A test can attach a counting delegate to each slot and assert the count stays 0.
- **Orchestrator incident, 2026-11-17 06:15 — never run two group agents on one tree.** During a corrupted
  orchestrator turn the delegator dispatched four Phase-3 group-2 agents for the same brief. They raced on
  `src/BrutalUi.Framework/Templates/TemplateBindings.cs`: one applied plant 8 (it removes the `AttachTo` loader check)
  while another ran its close, and the last writer left the file in the PLANTED state (md5 `013fe903…`, 391 lines). A
  scratch backup taken during the race (`obj/plantscratch/p8v-TemplateBindings.orig.cs`) first held that planted file
  too, so a restore from it would have shipped the fault. All four agents were stopped, the clean file (md5
  `514499b699b2313b3c0b793b12fb528c`, 398 lines) was restored from `obj/plantscratch/p8-TemplateBindings.orig.cs` and
  confirmed with `cmp`, and the Phase-3 gate was then re-run by one verifier. Lesson: a plant edits real source, so the
  one-agent-at-a-time rule is a safety rule, not a style rule; after any concurrent write, check file identity by hash
  before trusting a close report, and treat logs written during the race (`artifacts/logs/p3-g2*`, `p3-plant8*`) as
  untrustworthy until a clean re-run confirms them.

---

## Testing Approach

### Unit / integration tests (Framework, 23 new)
- `TemplateBindingDemoTests` 4 (phase 1), `TemplateWidgetBindingTests` 4 + `TemplateParityTests` 2 + `TemplateBindingAllocationTests` 3 (phase 2), `TemplateBindingErrorTests` 6 + `TemplateBindingsApiTests` 4 (phase 3).
- 8 plants prove each named test fails for its fault.

### Manual Testing Checklist
- No manual check planned: no window, no sample change (plan Testing Strategy).

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Windows run of `scripts/test-windows.ps1` (the pack path), if a person has a Windows host.
- [ ] Phase 1 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateBindingDemoTests'` and reads a total of 4 with `failed: 0`. In `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore`, a click on `pause` runs `game.pause` one time, and a store write of the score 1500 changes the `score` label to `1,500`. (Verifier ran it green: total 4, failed 0.)
- [ ] Phase 2 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateParityTests'` and reads a total of 2 with `failed: 0`. The template tree and the C# tree show the same text, checked state, and value after each of 10 store versions. (Verifier ran it green: total 2, failed 0; `*TemplateBindingAllocationTests` also 3 / failed 0, both gates at 0 bytes.)
- [ ] Phase 3 demo (group 1 part): the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateBindingErrorTests'` and reads a total of 6 with `failed: 0`. A template with `bind-text="hud.nme"` gives one `UnknownBinding` error at the line and the column of `bind-text`, and no root. (Agent run green: total 6, failed 0; a verifier should rerun after group 2.)
- [ ] Phase 3 demo (group 2 part): the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateBindingsApiTests'` and reads a total of 4 with `failed: 0`. A second `Add*` call for one key throws, a second `AttachTo` on one loader throws, and a game handler added after `AttachTo` still takes its own attribute. The package run prints `package-test: PASS Gauge level 0.5`. (Agent run green: total 4, failed 0; solution 3166 / failed 0 / skipped 3.)

---

## Next Steps

### Immediate
1. Phase 3 group 2 (tasks 3.5 and 3.6), STANDARD agent: the argument checks and the duplicate rules on the `Add*`
   methods and `AttachTo`, `TemplateBindingsApiTests` (4), the section 10, 11, and `CHANGELOG.md` edits, plant 8, and
   the package step (restore with the machine cache first: see the `DOTNET_CLI_HOME` note above). Expect Framework +4
   (351 → 355) and solution 3166.
2. Remember the `DefaultFontSize = 16f` rule (Risk 6 learning) in any click test of a loaded tree. An error test needs
   no context at all (phase 3 group 1).

---

## Open Questions

- [x] **Q:** Notes item 7 — does the packed Framework nuspec mark the Markup dependency with `exclude="Build,Analyzers"`?
      **A:** Yes. Task 1.2 read line 15 of the packed `BrutalUi.Framework.nuspec`:
      `<dependency id="BrutalUi.Markup" version="0.1.0-alpha" exclude="Build,Analyzers" />`.
      A game that references only the `BrutalUi.Framework` package gets the Markup library with no generator. A game
      with its own `[TemplateElement]` types must reference `BrutalUi.Markup` too. markup-sample or the README states
      it (follow-up, not this feature). The pack setting did not change.
- [x] **Q:** Notes item 5 — two `TemplateBindings` sets on one loader. Plan: document the one-set rule.
      **A:** Option (a), as the plan chose. The class remarks of `TemplateBindings` and section 11 of
      `technical-design.md` state the rule, and a game makes one loader for each set. The loader has no public list of its
      handlers, and a static table would break the no-static-state rule, so the feature adds no detection and no merge
      API. `TemplateBindingsApiTests.ASecondAttachToOnOneLoaderThrows` guards the one-object case (Decision 4), and
      `TemplateBindingErrorTests.ALoaderWithNoBindingsRejectsEachAttribute` guards the no-set case (A6).

---

## Session Notes

### 2026-11-17 (phase 1 group 1, ADVANCED agent)
- 1.1: baseline recorded above (`artifacts/logs/p0-baseline.log`): solution 3143 / failed 0 / skipped 3; Framework.Tests 332 / failed 0. `git status --porcelain` showed only the four untracked feature docs. Base commit `5bbeec9e242447efd81d82023a7e9202b34a6fa8`.
- 1.2: added the Markup `ProjectReference` after the Core line in `BrutalUi.Framework.csproj`. Build: `0 Warning(s)`, `0 Error(s)`. Analyzer item check: `dotnet msbuild ... -getItem:Analyzer | grep -c 'BrutalUi.Generators'` gave `0` — the generator does not flow through the project reference. `dotnet pack --no-build` succeeded; the Framework nuspec holds one `BrutalUi.Markup` dependency line with `exclude="Build,Analyzers"` (see Open Questions). `src/BrutalUi.Markup/BrutalUi.Markup.csproj` did not change.
- 1.3: wrote `Templates/TemplateBindingEntry.cs` (103 lines): the internal abstract base with four virtual `TryBind` overloads, each `false`, and `NumberTextEntry<T, TValue>` with four `readonly` fields in the field style of `CommandBindings.ButtonCommand`. The class remarks state the entry rule.
- 1.4: wrote `Templates/TemplateBindings.cs` (155 lines): constructor, ordinal dictionary, `AddText<T, TValue>`, `AttachTo`, private `Handle`. `Handle` binds `command` on a `Button` with a non-empty value (D6: `Encoding.UTF8.GetBytes(value)` into `BindCommand`) and `bind-text` on a `Label` with a known key whose `TryBind` gives `true`; every other case returns `false`. Remarks state the capture, the load-time rule, the one-set rule, and the add-after-attach rule.
- 1.5: wrote `tests/.../Templates/TemplateBindingDemoTests.cs` (4 tests). All 4 pass: `--filter-class '*TemplateBindingDemoTests'` gives total 4, failed 0.
- Learning (Risk 6 confirmed): a loaded tree with no stylesheet lays out at font size 0, so text-measured leaves have height 0 and a click at their center misses. The fix is `UiContext.CreateInfo.DefaultFontSize = 16f` in the demo contexts — not a change to the template. A click test of a loaded tree MUST set the context font size or give elements inline sizes.

- 1.6: edited `docs/technical-design.md`. Section 3 line 125 now reads "It references Core and Markup." (Notes item 3). Section 11: the sentence after the first template and the last sentence of **The attribute hook.** now name `TemplateBindings.AttachTo`; the new **The binding attributes.** block follows it, and its template equals the test template character for character (`diff` clean, LF only, `artifacts/logs/doc.tpl` versus `artifacts/logs/test.tpl`). The first template stayed unchanged (Notes item 2).
- 1.6 counts: full build `0 Warning(s)`, `0 Error(s)`. Solution tests after the phase: total 3147, failed 0, skipped 3 (baseline 3143 +4). `BrutalUi.Framework.Tests` total 336 (baseline 332 +4). Demo line: `--filter-class '*TemplateBindingDemoTests'` reads total 4, failed 0.
- Plant 1 (`obj/plantscratch/p1-TemplateBindings.orig.cs`): the `command` branch returned `element is Button && value.Length > 0` with no `BindCommand` call. Exit code 2, 0 `error CS` lines. Failed exactly: `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore`, `ACommandThatTheGameRegistersAfterTheLoadRuns` (log `artifacts/logs/p1-plant1.log`). Restore passed `cmp`.
- Plant 2 (`obj/plantscratch/p2-TemplateBindingEntry.orig.cs`): `NumberTextEntry.TryBind(Label)` passed `null` for the format. Exit code 2, 0 `error CS` lines. Failed exactly: `TheFormatAndTheProviderComeFromTheRegisterCall`, `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore` (log `artifacts/logs/p1-plant2.log`). Restore passed `cmp`.
- Close check after the restores: build `0 Warning(s)` / `0 Error(s)`, solution 3147 / failed 0 / skipped 3, demo class 4 / failed 0 (log `artifacts/logs/p1-close.log`).
- Acceptance facts: `ProjectReference` count in Framework = 2; no `BrutalUi.Generators` in the Framework Analyzer list (0 hits); the packed nuspec holds the one `BrutalUi.Markup` dependency line; `git diff --stat 5bbeec9 -- src/BrutalUi.Markup` is empty; `src/BrutalUi.Framework/Templates/` uses no reflection; the only new public type is `TemplateBindings` (grep of the new files). File sizes: 97, 152, 223 lines. No commit and no push (branch `feat/markup/binding-attributes`).

### 2026-11-17 03:50
- `/autorun-feature markup/binding-attributes` started: preconditions checked, plan and epic context loaded, branch `feat/markup/binding-attributes` created from `main`, docs scaffolded from `implementation.md`.
- Phase 1 (ADVANCED) queued.

### 2026-11-17 04:30
- Phase 1 gate verified by an independent verifier agent: `PASS` on all 13 brief steps, `Demo: PASS`. No fix round was needed, and no `bug` line reached `inbox.md`.
- Verifier found one environmental trap and proved its cause: the `DOTNET_CLI_HOME` override makes solution-level `dotnet pack` fail with `NETSDK1064` on `BrutalUi.Debugger`. Recorded above under Tool notes; task 3.6 (the package step) must restore with the machine cache first.
- Two verification-method steps read a new file directly, not through `git diff`, because this run makes no commit. Recorded above.
- Phase 2 group 1 (tasks 2.1 to 2.3, STANDARD) started with brief `.beast-mode/briefs/markup/binding-attributes/phase-2-g1.md`. Phase 2 group 2 brief ready at `phase-2-g2.md`.

### 2026-11-17 04:42 (phase 2 group 1, STANDARD agent)
- 2.1: added three entries to `Templates/TemplateBindingEntry.cs` (226 lines). `StringTextEntry<T>` overrides
  `TryBind(Label)` with the string `LabelBindings.BindText` and `TryBind(TextField)` with `WidgetBindings.BindText`;
  `CheckedEntry<T>` overrides `TryBind(Toggle)` with `BindChecked`; `ValueEntry<T>` overrides `TryBind(Slider)` with
  `BindValue`. Each holds the store and the selector in `readonly` fields in the field style of task 1.3, and returns
  `true` after the bind call (D2). `NumberTextEntry` did NOT gain a `TryBind(TextField)` override (A5). `SelectorKind`
  stays out until phase 3.
- 2.2: added `AddText<T>` (string), `AddChecked<T>`, and `AddValue<T>` to `Templates/TemplateBindings.cs`
  (275 lines), each with `ArgumentNullException.ThrowIfNull` for the key, the store, and the selector, then
  `_entries.Add` (no duplicate or empty-key check yet: phase 3). `Handle` now takes `bind-text` on a `TextField`,
  `bind-checked` on a `Toggle`, and `bind-value` on a `Slider` through `TryBind`. Each case keeps the phase-1 shape:
  the element-type test first, then the key lookup, then the fit; each fault case still returns `false`, so the
  loader gives its own `UnknownAttribute` and this phase adds no `AddError` call (D3). No `Add*` call after
  `AttachTo` changed the table-reading rule.
- **Risk 3 confirmed.** `bindings.AddText("hud.name", store, static s => s.Name)` picks the STRING overload, and
  `bindings.AddText("hud.score", store, static s => s.Score)` picks the NUMBER overload. Proof: both three-argument
  calls compile in one file with `0 Warning(s)` / `0 Error(s)`, so resolution is not ambiguous (`CS0121` would
  appear if `string` implemented `IUtf8SpanFormattable`; it does not). A temporary probe test (removed before the
  close) then proved each pick at run time: a `TextField` with `bind-text="hud.name"` loads (`StringTextEntry`
  overrides `TryBind(TextField)`), and a `TextField` with `bind-text="hud.score"` fails the load with
  `UnknownAttribute` (`NumberTextEntry` does not). `OneStringKeyServesALabelAndATextField` keeps the first half of
  that proof in the permanent suite.
- 2.3: wrote `tests/BrutalUi.Framework.Tests/Templates/TemplateWidgetBindingTests.cs` (238 lines, 4 tests):
  `AStringKeyBindsALabel`, `OneStringKeyServesALabelAndATextField` (label and field each hold one binding, A5),
  `ACheckedKeyBindsAToggle` (`CheckedChanged` gets no call), `AValueKeyBindsASlider`
  (`<ui:Slider name="volume" max="100" bind-value="hud.volume" />`, `ValueChanged` gets no call). Each test follows
  the demo setup: a `Store<BoundTree.HudState>`, `u8` template literals, `root.Find<T>("name"u8)`, a context with a
  `RecordingRenderer` measurer and `DefaultFontSize = 16f` (the phase-1 learning, Risk 6), and inline size 400x300
  on the root. `Slider` needs `max`, and `TextField` needs `capacity`, or the load fails.
- Counts: build `0 Warning(s)` / `0 Error(s)`. Solution 3151 / failed 0 / skipped 3 (baseline 3147 +4). Framework
  class totals: `*TemplateWidgetBindingTests` 4 / failed 0, `*TemplateBindingDemoTests` 4 / failed 0; Framework
  project 340 (336 +4). Logs: `artifacts/logs/p2-g1-tests.log`, `artifacts/logs/p2-g1-close.log`.
- Plant 3 (`obj/plantscratch/p2-TemplateBindingEntry.orig.cs`): `StringTextEntry.TryBind(TextField)` returned
  `false`. Exit code 2, 0 `error CS` lines, failed exactly
  `BrutalUi.Framework.Tests.TemplateWidgetBindingTests.OneStringKeyServesALabelAndATextField`
  (log `artifacts/logs/p2-plant3.log`). Restore passed `cmp`; the class ran green again (4 / failed 0).
- No commit and no push (branch `feat/markup/binding-attributes`). `git status --porcelain` shows only the files of
  phase 1 plus this group's four files, `tasks.md`, `context.md`, and the `artifacts/logs/` files.

### 2026-11-17 05:00 (phase 2 group 2, STANDARD agent)
- 2.4: wrote `tests/BrutalUi.Framework.Tests/Shared/BoundTemplate.cs` as `internal static class BoundTemplate`. It holds
  the template text (root `hud`, the labels `score` (`hud.score`, `N0`, invariant), `volume` (`hud.volume-text`, `F1`,
  invariant), and `player` (`hud.name`), the field `name-field` (`capacity="16"`, `hud.name`), the toggle `mute`
  (`hud.muted`), the slider `volume-slider` (`max="100"`, `hud.volume`), and the button `pause` (`game.pause`)), the
  `Load(Store<BoundTree.HudState>, CommandRegistry)` path, and the `BuildCode(...)` C# twin built with the builder
  methods and the bind methods. Then wrote `tests/BrutalUi.Framework.Tests/Templates/TemplateParityTests.cs` (2 tests):
  `TheTemplateTreeEqualsTheCodeTreeAfterEachStoreVersion` (two contexts; for rounds 1 to 10 `Set(BoundTree.Next(round))`,
  `Update` both, assert `TreeCompare.FirstDifference` null and equal `BindingCount` on each bound element) and
  `AClickOnEachTreeRunsTheCommandOneTime`. Proof: the template tree equals the code tree after every store version, and
  one click on each `pause` runs the command once.
- 2.5: wrote `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingAllocationTests.cs` (3 tests) in the pattern of
  `BoundTreeAllocationTests`, with `AllocationGate.ReleaseAllocationContext()` before each measure.
  `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` (2 warm-up rounds of 9 frames, then 100 measured frames, 0 bytes);
  `AClickOnALoadedCommandButtonAllocatesNothing` (a press and a release on `pause` each frame, 0 bytes);
  `TheMeasuredBodyDoesRealWork` (after one changing frame each bound element shows the round state; after one click
  frame the counter grows by 1). Proof: a loaded bound tree allocates nothing in update and draw, and the measured body
  does real work.
- 2.6: edited `docs/technical-design.md`. Section 11 **The binding attributes.** now holds the four-attribute table
  (attribute, element types, `Add*` method, bind method), The Entry Shape in prose, and the rules of the object except
  the errors. Section 10 **Data binding** adds one sentence: a template binds through `TemplateBindings` (section 11)
  with the same bind methods, and `TemplateParityTests` guards the parity. Section 14 adds **A twenty-second allocation
  gate.** for `TemplateBindingAllocationTests` with its plant.
- Counts: build `0 Warning(s)` / `0 Error(s)`. Solution 3156 / failed 0 / skipped 3 (baseline 3151 +5 from this group's
  2 + 3 tests). Framework project 345 (340 +5 — the parity and allocation classes). Class totals:
  `*TemplateParityTests` 2 / failed 0, `*TemplateBindingAllocationTests` 3 / failed 0. Logs:
  `artifacts/logs/p2-g2-tests.log`, `artifacts/logs/p2-g2-close.log`.
- Plant 4 (`obj/plantscratch/p4-TemplateBindingEntry.orig.cs`): `CheckedEntry.TryBind(Toggle)` bound `s => !selector(s)`.
  Exit code 2; build `error CS` count 0 (`artifacts/logs/p2-plant4-build.log`). Failed exactly
  `BrutalUi.Framework.Tests.TemplateWidgetBindingTests.ACheckedKeyBindsAToggle`,
  `BrutalUi.Framework.Tests.TemplateParityTests.TheTemplateTreeEqualsTheCodeTreeAfterEachStoreVersion`, and
  `BrutalUi.Framework.Tests.TemplateBindingAllocationTests.TheMeasuredBodyDoesRealWork` (log `artifacts/logs/p2-plant4.log`).
  `cmp` of `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs` against the original passed clean.
- Plant 5 (`obj/plantscratch/p5-WidgetBindings.orig.cs`): `GC.KeepAlive(new object());` as the first line of
  `WidgetBindings.ValueHandler<T>.Run`. Exit code 2; build `error CS` count 0 (`artifacts/logs/p2-plant5-build.log`).
  Failed exactly `BrutalUi.Framework.Tests.TemplateBindingAllocationTests.UpdateAndDrawOnALoadedBoundTreeAllocateNothing`
  plus the three `BrutalUi.Framework.Tests.BoundTreeAllocationTests` gates `AUserFlipWithAWriteBackAllocatesNothing`,
  `AChangeOfOneSliceAllocatesNothing`, and `UpdateAndDrawWithAChangingStoreAllocateNothing` (Risk 5 of the plan names
  that extra fallout). Log `artifacts/logs/p2-plant5.log`. `cmp` of
  `src/BrutalUi.Framework/Binding/WidgetBindings.cs` against the original passed clean.
- Close state after the restores: build `0 Warning(s)` / `0 Error(s)`; solution 3156 / failed 0 / skipped 3;
  `BrutalUi.Framework.Tests` passed with `*TemplateParityTests` 2 / failed 0 and `*TemplateBindingAllocationTests`
  3 / failed 0 (logs `artifacts/logs/p2-g2-tests.log`, `artifacts/logs/p2-g2-close.log`).
- No commit and no push (branch `feat/markup/binding-attributes`).

### 2026-11-17 05:45 (phase 3 group 1, STANDARD agent)
- 3.1: `TemplateError.cs` gains `UnknownBinding` and `BindingTypeMismatch` after `UnexpectedChild`, each with a doc
  line that names its cause, plus the two `GetKindName` arms. The file diff against `5bbeec9` is 8 insertions in this
  one file — the whole A7 Markup edit. The kind grep of task 3.1 shows no code that counts the members, and the only
  `switch` on the kind (`GetKindName`) keeps its `_ =>` default. Markup tests after the change: 666 / failed 0.
- 3.2: `TemplateBindingEntry` gains `public abstract string SelectorKind { get; }` (247 lines now). The four entries
  answer `a string` (`StringTextEntry`), `a number` (`NumberTextEntry`), `a bool` (`CheckedEntry`), and `a float`
  (`ValueEntry`). The base remarks now state the selector-kind rule.
- 3.3: `Handle` is the end state of The Handler (351 lines in the file). Each of the four names returns `true`; another
  name returns `false`. Each fault adds at most one error through `context.AddError`, which forwards the position of
  the attribute name, so the handler computes no line and no column. The order inside each case is the element type,
  then an empty value, then the key, then the fit, then the bind (D3). The success path is unchanged: the same
  `TryBind` calls, and `command` still calls `BindCommand` with `Encoding.UTF8.GetBytes(value)`.
- **Message shapes, written here so group 2 copies them into section 11.** Wrong element:
  `'<name>' is not an attribute of this element. Write <name> on a Button only.` (a `bind-text` reads
  `on a Label or a TextField only`, `bind-checked` `on a Toggle only`, `bind-value` `on a Slider only`) — Notes item 4:
  the message names the valid element types, because the handler sees the `Element`, not the XML name. Empty value:
  `'command' needs a command name. Write the name of a registered command, for example 'game.pause'.`, and for each
  bind attribute `'<name>' needs a key. Write a key that the panel code registers, for example '<example key>'.` The
  example key is `'hud.score'`, `'hud.muted'`, and `'hud.volume'` for `bind-text`, `bind-checked`, and `bind-value`:
  the plan gives the first, and the other two follow the same form with the key of that attribute in section 11.
  Unknown key: `'<key>' is not a bound key. Register it with TemplateBindings.AddText before the load.` (`AddChecked`,
  `AddValue`). Mismatch: `'<key>' holds <SelectorKind> selector. bind-text on a Label needs a string or a number
  selector. Register the key with AddText.`, with `bind-text on a TextField needs a string selector`, `bind-checked on
  a Toggle needs a bool selector`, and `bind-value on a Slider needs a float selector`.
- 3.4: wrote `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingErrorTests.cs` (274 lines, 6 tests). Each test
  holds its template as a `const string` and passes `Encoding.UTF8.GetBytes(template)` to the span overload of `Load`,
  so one text serves the load and the position check; `BRUTALCORE0011` stays quiet, because no call takes a string
  literal. `AssertAttribute(error, template, attributeName, value)` follows `AttributeHookTests.AssertAttribute` and
  matches `attributeName="value"`, so two `bind-text` faults on one template each get their own column. No test needs
  a `UiContext`: a fault gives no root, so the load alone reports the errors, and the `DefaultFontSize = 16f` rule of
  Risk 6 does not apply here.
- **Message asserts stay fragments.** Each test checks `Kind`, the computed `Line` and `Column`, and `Contains` on the
  quoted key or attribute name. `ASelectorOfTheWrongTypeIsABindingTypeMismatchError` also checks
  `holds a bool selector.`, `holds a number selector.`, `holds a string selector.`, and `holds a float selector.`, so
  each `SelectorKind` value carries a guard.
- Counts: build `0 Warning(s)` / `0 Error(s)` (after `dotnet build-server shutdown`, because the Markup enum changed).
  Solution 3162 / failed 0 / skipped 3 (3156 +6). `BrutalUi.Framework.Tests` 351 (345 +6);
  `BrutalUi.Markup.Tests` 666 / failed 0. Class totals: `*TemplateBindingErrorTests` 6, `*TemplateBindingDemoTests` 4,
  `*TemplateWidgetBindingTests` 4, `*TemplateParityTests` 2, `*TemplateBindingAllocationTests` 3, each failed 0. No
  existing test needed an edit. Logs: `artifacts/logs/p3-g1-tests.log`, `artifacts/logs/p3-g1-close.log`,
  `artifacts/logs/p3-g1-framework.txt`, `artifacts/logs/p3-g1-markup.txt`.
- Plant 6 (`src/BrutalUi.Framework/obj/plantscratch/p6-TemplateBindings.orig.cs`): the `bind-text` case moved the
  empty-value check and the key check above the element-type check. Build `error CS` count 0
  (`artifacts/logs/p3-plant6-build.log`). Exit 2; failed exactly
  `BrutalUi.Framework.Tests.TemplateBindingErrorTests.AnAttributeOnTheWrongElementIsAnUnknownAttributeError`
  (log `artifacts/logs/p3-plant6.log`). The empty check travels with the key check on purpose: a plant that moves only
  the lookup would also fail `AnEmptyValueIsAnInvalidValueError`, because the lookup of an empty key misses. `cmp` of
  `src/BrutalUi.Framework/Templates/TemplateBindings.cs` against the original passed clean.
- Plant 7 (`src/BrutalUi.Markup/obj/plantscratch/p7-TemplateError.orig.cs`): `GetKindName` lost the `UnknownBinding`
  arm. Build `error CS` count 0 (`artifacts/logs/p3-plant7-build.log`), after `dotnet build-server shutdown`. Exit 2;
  failed exactly `BrutalUi.Framework.Tests.TemplateBindingErrorTests.TheErrorTextNamesTheNewKinds` (the three Glfw
  names in the log are the skipped tests). `ToString` fell back to the number `8`. `cmp` of
  `src/BrutalUi.Markup/Template/TemplateError.cs` against the original passed clean.
- Close state after both restores: build `0 Warning(s)` / `0 Error(s)`; solution 3162 / failed 0 / skipped 3; the new
  class 6 / failed 0 (log `artifacts/logs/p3-g1-close.log`).
- Acceptance checks: `git diff --stat 5bbeec9 -- src/BrutalUi.Markup` shows `Template/TemplateError.cs` only; the kind
  grep shows the two members after `UnexpectedChild` and the two arms; each of the four kinds appears in a
  `Templates/*.cs` test file; `Templates/` uses no reflection; each file stays under 400 lines; the only public type in
  the changed files is `TemplateBindings`. `obj/plantscratch/` stays out of `git status`. No commit and no push.
- Hand-off to group 2: section 11 of `technical-design.md` still ends **The rules of the object.** with "Each fault
  case of the four names returns `false`, so the loader gives its own `UnknownAttribute`. A later step adds the
  positioned binding errors." That sentence is now wrong; task 3.6 replaces it with the order of checks and the four
  messages, adds the two members to the `TemplateErrorKind` block, and adds the two rows to the table of
  **The error report.**

### 2026-11-17 06:05 (phase 3 group 2, STANDARD agent)
- 3.5: `Templates/TemplateBindings.cs` (398 lines) now checks each argument. A private `CheckKey` runs the two key rules
  of A4 in each `Add*` method, after the three `ArgumentNullException.ThrowIfNull` calls and before `_entries.Add`: an
  empty key throws `ArgumentException` (`"A binding key must not be empty. Write a key that the panel code registers,
  for example 'hud.score'."`), and a key that the table holds throws `ArgumentException` (`"One binding key holds one
  selector. Use another key, or register the key on another TemplateBindings object."`). Both messages are constant and
  both take the parameter name `key`; the check runs before the add, so the table keeps the first selector, also across
  two `Add*` methods. `AttachTo` keeps `private readonly List<TemplateLoader> _loaders` and throws `ArgumentException`
  with the parameter name `loader` (`"One TemplateBindings object attaches to one loader one time. Make another loader
  for another bindings set."`) before it adds a handler; one object can still attach to more than one loader. The class
  remarks gained the **One key holds one selector.** paragraph, and the order of `Handle` and the success path did not
  change. No lock: the doc comments already state that an `Add*` call and a `Load` call must not run at the same time.
- 3.5 tests: wrote `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingsApiTests.cs` (4 tests).
  `ASecondAddForOneKeyThrows` (a second `AddText`, the number `AddText`, and an `AddChecked` on one key each throw with
  `key`, and the first selector still binds: the label holds one binding and shows `Ann`),
  `TheMethodsCheckTheirArguments` (a `null` registry, loader, key, store, or selector throws `ArgumentNullException` with
  its parameter name; an empty key throws `ArgumentException` in each of the four methods),
  `ASecondAttachToOnOneLoaderThrows` (the second call throws with `loader`, a load after it binds the label one time, and
  a second loader still takes an attach), and `TheHandlerPassesAnotherAttributeToTheNextHandler` (a game handler that the
  game adds after `AttachTo` takes `probe`, the load reports no error, and the label holds one binding). No click test, so
  no context needs a size; the two tests that read the bound text use the `RecordingRenderer` context with
  `DefaultFontSize = 16f`. `Load` throws no exception for any template text in the class.
- 3.6 docs: `technical-design.md` section 11. The `TemplateErrorKind` block gained `UnknownBinding` and
  `BindingTypeMismatch` with their two comments, and the table of **The error report.** gained their two rows. The new
  **The order of the checks.** names the five steps (element type, empty value, key, fit, bind), the `false` return for
  another name, the position rule of `AddError`, and the rule that a fault gives no root and throws nothing. The new
  **The four messages.** table holds the four texts, quoted from `Handle`. **The rules of the object.** lost the stale
  phase-2 sentence and now holds the second-attach throw, the A4 duplicate rule, the empty key, and the
  `ArgumentNullException` parameter names, plus the guard classes. The entry-shape paragraph names the four
  `SelectorKind` values. Section 10 **The button bind rules** gained the line that the `command` attribute of a template
  calls `BindCommand` with the registry of the attached `TemplateBindings` (section 11). `CHANGELOG.md` gained one
  `### Added` line tagged `(markup/binding-attributes)` that names `TemplateBindings`, the four attributes, the typed
  selector registration, and the two new error kinds.
- Counts: build `0 Warning(s)` / `0 Error(s)`. Solution 3166 / failed 0 / skipped 3 (3162 +4). `BrutalUi.Framework.Tests`
  355 (351 +4, and 332 +23 for the feature); `BrutalUi.Markup.Tests` 666 / failed 0. Class totals:
  `*TemplateBindingsApiTests` 4, `*TemplateBindingErrorTests` 6, `*TemplateBindingDemoTests` 4,
  `*TemplateWidgetBindingTests` 4, `*TemplateParityTests` 2, `*TemplateBindingAllocationTests` 3, each `failed: 0`. No
  existing test needed an edit. Logs: `artifacts/logs/p3-g2-tests.log`, `artifacts/logs/p3-g2-close.log`,
  `artifacts/logs/p3-g2-framework.txt`, `artifacts/logs/p3-g2-markup.txt`.
- Plant 8 (`src/BrutalUi.Framework/obj/plantscratch/p8-TemplateBindings.orig.cs`): `AttachTo` lost the
  `_loaders.Contains` check and added its handler every call. Build `error CS` count 0
  (`artifacts/logs/p3-plant8-build.log`). Exit 2; Framework 355 / failed 1, and the one failure is exactly
  `BrutalUi.Framework.Tests.TemplateBindingsApiTests.ASecondAttachToOnOneLoaderThrows` with `Assert.Throws() Failure: No
  exception was thrown` (log `artifacts/logs/p3-plant8.log`). The label keeps one binding even with two handlers,
  because the first handler takes the attribute, so the throw is the only detector — as group 1 predicted. `cmp` of
  `src/BrutalUi.Framework/Templates/TemplateBindings.cs` against the original passed clean, and the class ran green
  again (4 / failed 0).
- Package path (task 3.6, the `DOTNET_CLI_HOME` note above): `dotnet restore brutal-ui.sln` with NO
  `DOTNET_CLI_HOME` override, then `dotnet pack brutal-ui.sln --no-build --configuration Debug` — success, no
  `NETSDK1064`. The packed `BrutalUi.Framework.nuspec` line 15 holds the one dependency
  `<dependency id="BrutalUi.Markup" version="0.1.0-alpha" exclude="Build,Analyzers" />` (Notes item 7 unchanged).
  `rm -rf artifacts/package-test`, `dotnet restore tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --force`
  (the `NU1900` read-only-http-cache lines are machine noise), `dotnet build-server shutdown`, then
  `dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --no-restore` printed
  **`package-test: PASS Gauge level 0.5`**. The whole `scripts/test-linux.sh` stayed unrun: its `smoke` step needs
  `Xvfb`, which segfaults on this host.
- Verification Method, items 1 to 15, all run by this group: 1 build `0 Warning(s)` / `0 Error(s)`; 2 solution 3166 /
  failed 0 / skipped 3; 3 the six class totals above, each `failed: 0`; 4 `ProjectReference` count 2 with the one Markup
  line at 9; 5 `git diff --stat 5bbeec9 -- src/BrutalUi.Markup` is `Template/TemplateError.cs | 8 ++++++++` only, and the
  grep shows the two members after `UnexpectedChild` and the two `GetKindName` arms; 6 the guarded paths show no diff;
  7 no reflection use in `Templates/`; 8 `TemplateBindings` is the only public type in `Templates/` (the run makes no
  commit, so the check reads the new files directly); 9 the Framework `Analyzer` item list holds 0 `BrutalUi.Generators`
  entries; 10 the pack, the nuspec line, and `package-test: PASS Gauge level 0.5` above; 11 `Templates/` holds 2 files,
  247 and 398 lines, each under 400; 12 `bind-checked` 4 hits, `TemplateBindingAllocationTests` 2 hits, section 3 line
  125 reads "It references Core and Markup.", and `CHANGELOG.md` line 56 holds the one feature line; 13 each of the four
  kinds appears in `TemplateBindingErrorTests.cs`; 14 the template of **The binding attributes.** equals the demo-test
  template `SectionElevenTemplate` with the C# indentation removed
  (`diff <(sed 's/^ *//' …) <(sed 's/^ *//' …)` clean; `artifacts/logs/p3-g2-doc.tpl`, `artifacts/logs/p3-g2-test.tpl`);
  15 this file records plants 1 to 8, each with a non-zero exit, 0 `error CS` lines, and the named failures.
- Close state: build `0 Warning(s)` / `0 Error(s)`; solution 3166 / failed 0 / skipped 3; Framework 355 / Markup 666.
  `git status --porcelain` shows the modified `CHANGELOG.md`, `docs/technical-design.md`,
  `src/BrutalUi.Framework/BrutalUi.Framework.csproj`, and `src/BrutalUi.Markup/Template/TemplateError.cs`, plus the
  untracked `src/BrutalUi.Framework/Templates/`, `tests/BrutalUi.Framework.Tests/Templates/`,
  `tests/BrutalUi.Framework.Tests/Shared/BoundTemplate.cs`, and the four feature docs. No commit and no push (branch
  `feat/markup/binding-attributes`).
- Open point for the caller: the feature stays uncommitted by design. `/update-feature` and `/update-master` did not run
  in this group.

### 2026-11-17 06:35 (phase 3 group 2, close re-verification pass)
- The tree already held all group-2 products when this pass started: the argument checks and the loader list in
  `TemplateBindings.cs` (398 lines), `TemplateBindingsApiTests.cs` (240 lines, 4 tests), the section 10 and 11 doc edits
  (the stale **The rules of the object.** sentence from the hand-off is gone; the section holds the order of the checks,
  the four messages, the A4 rule, and the attach rule), and the one `CHANGELOG.md` line. Tasks 3.5 and 3.6 and the four
  phase-3 acceptance boxes are ticked in `tasks.md`. This pass re-ran the Verification Method, plant 8, and the package
  steps to prove the recorded state.
- Item 1: build `0 Warning(s)` / `0 Error(s)`. Item 2: solution 3166 / failed 0 / skipped 3 (log
  `artifacts/logs/p3-g2r-tests.log`). Item 3: class totals `*TemplateBindingDemoTests` 4, `*TemplateWidgetBindingTests`
  4, `*TemplateParityTests` 2, `*TemplateBindingAllocationTests` 3, `*TemplateBindingErrorTests` 6,
  `*TemplateBindingsApiTests` 4, each `failed: 0`. Project totals: Framework 355, Markup 666 (logs
  `artifacts/logs/p3-g2r-framework.txt`, `artifacts/logs/p3-g2r-markup.txt`). Counts hold: this group +4 (351 → 355),
  phase +10, feature +23 over the 3143 baseline.
- Items 4 to 13 and 15 passed as recorded: `ProjectReference` 2 with the Markup line at 9; `git diff --stat 5bbeec9 --
  src/BrutalUi.Markup` is `Template/TemplateError.cs | 8 ++++++++` only with the two members and the two arms; the
  guarded paths show no diff; no reflection in `Templates/`; `TemplateBindings` is the only public type there (the run
  makes no commit, so the check reads the new files directly); the Framework `Analyzer` list holds 0
  `BrutalUi.Generators` entries; 247 and 398 lines; the doc greps and the one changelog line; all four kinds appear in
  `TemplateBindingErrorTests.cs`; this file records plants 1 to 8. Item 14: the section 11 template equals the demo-test
  `SectionElevenTemplate` with indentation removed (`diff` clean, `artifacts/logs/p3-g2r-item14.txt`).
- Plant 8 re-run (`obj/plantscratch/p8r-TemplateBindings.orig.cs`): `AttachTo` lost the `_loaders.Contains` check.
  Solution tests exited 2 with 0 `error CS` lines; Framework 355 / failed 1, exactly
  `BrutalUi.Framework.Tests.TemplateBindingsApiTests.ASecondAttachToOnOneLoaderThrows` (log
  `artifacts/logs/p3-g2r-plant8.log`). `cp` restore passed `cmp`; after a rebuild the class ran green (4 / failed 0).
- Package steps in the brief's corrected order: `dotnet restore brutal-ui.sln` with NO `DOTNET_CLI_HOME`, then
  `dotnet pack brutal-ui.sln --no-build --configuration Debug` — success, no `NETSDK1064`; the nuspec line 15 holds
  `<dependency id="BrutalUi.Markup" version="0.1.0-alpha" exclude="Build,Analyzers" />`; `rm -rf artifacts/package-test`
  + forced restore (only `NU1900` machine noise) + `dotnet build-server shutdown` + `dotnet run --no-restore` printed
  **`package-test: PASS Gauge level 0.5`**. `scripts/test-linux.sh` stayed unrun (its `smoke` step needs `Xvfb`, which
  segfaults on this host).
- Close state: build `0 Warning(s)` / `0 Error(s)`; solution 3166 / failed 0 / skipped 3; Framework 355 / Markup 666.
  Group 2 and phase 3 are verified closed. No commit and no push (branch `feat/markup/binding-attributes`).

### 2026-11-17 06:40 (orchestrator, duplicate-agent fault and Phase 3 gate)
- **Fault.** A corrupted orchestrator turn dispatched four duplicate Phase-3 group-2 agents for the same brief while
  the real group-2 run had already finished. They raced on `TemplateBindings.cs`, and the last writer left the file in
  the plant-8 state (md5 `013fe903…`, 391 lines, the `AttachTo` loader check gone). All four agents were stopped. The
  clean file was restored from `obj/plantscratch/p8-TemplateBindings.orig.cs` and confirmed with `cmp` and `md5sum`
  (`514499b699b2313b3c0b793b12fb528c`, 398 lines). The incident and the lesson are under Gotchas & Learnings, and the
  bug line is closed in `inbox.md`. See the 06:35 group-2 note above for the re-run of the close that this recovery
  depends on.
- **Phase 3 gate, independent verifier, post-restore: `PASS`, `Demo: PASS`.** Step 0 confirmed the tree identity
  before anything else (live file md5 `514499b6…`, 398 lines, `cmp`-identical to the `p8-` and `p8r-` backups, and all
  three `p8*` backups now hold the clean hash). Gates 1 to 14 all passed: build `0 Warning(s)` / `0 Error(s)`; solution
  3166 / failed 0 / skipped 3; the six class totals 4 / 4 / 2 / 3 / 6 / 4 each with `failed: 0`; Framework 355; Markup
  666; the Markup blast radius is `TemplateError.cs` alone, 8 lines; no reflection in `Templates/`; `TemplateBindings`
  is the only public type; `package-test: PASS Gauge level 0.5`; the docs and the changelog line; the section 11
  template equals the demo-test template; and `git status` shows no commit (HEAD `5bbeec9`).
- The verifier checked the plant logs against this file with the race in mind: plants 6 and 7 (05:35, 05:36) and the
  original plant 8 (05:52) predate the race window and match; the plant-8 re-run log (06:08) falls inside it, is
  redundant with the pre-race original, and needs no further rerun.
- **Next.** Cross-phase review under way (brief `.beast-mode/briefs/markup/binding-attributes/review.md`, report
  `code-review.md`), then the review fix groups, one final verification pass, `/update-feature`, `/update-epic markup`,
  and `/update-master`. All 18 tasks are built; no commit and no push, by design — the branch is the human's to ship.

### 2026-11-17 07:05 (review group 1, CR-6, CR-7 and CR-8)
- **CR-6, the partial split.** `TemplateBindings` is now `public sealed partial class` in two files.
  `Templates/TemplateBindings.cs` holds the class remarks, the state, the constructor, the four `Add*` methods,
  `CheckKey`, and `AttachTo` (276 lines). `Templates/TemplateBindings.Handler.cs` holds `Handle` with its `///`
  remarks (171 lines). The move carried no behavior change: `Handle` keeps the same order of checks and one error per
  attribute. `Templates/` now holds three `.cs` files, each well under the 400-line cap. The public surface is
  unchanged; the split added no member.
- **CR-8, part A, the unknown-key message.** Each bind attribute now builds one longer
  `TemplateErrorKind.UnknownBinding` message: `'<key>' is not a bound key. Register it with TemplateBindings.AddText
  on the TemplateBindings object that this loader attached first, before the load. Another TemplateBindings object on
  this loader can hold the key: only the first attached object receives the four names, so make one loader for each
  bindings set.` (`AddText`, `AddChecked`, `AddValue` per attribute.) The message names both causes, and the position
  still comes from `AddError`. No test assert changed: `TemplateBindingErrorTests` pins the quoted key and the
  `holds a … selector.` fragments only, and both stay present.
- **CR-8, part B, the unattached loader.** No Markup edit. The class remarks hold a new
  **The two setup faults.** paragraph and `AttachTo` holds a new **A loader needs one call.** paragraph: a loader that
  no `AttachTo` call took gives its own `UnknownAttribute` with the attributes of the element, never names a binding,
  and the fix is one `AttachTo` call on that loader. `ALoaderWithNoBindingsRejectsEachAttribute` passes unchanged.
- **CR-7, the retention rule.** No detach method, no weak reference, no new member. The `_loaders` field doc, a new
  **The object keeps each loader.** class-remarks paragraph, and a matching `AttachTo` paragraph state that the list
  only grows, that no detach method exists, and that a game makes one `TemplateBindings` per loader set and releases
  the two together.
- **Numbers.** Build `0 Warning(s)` / `0 Error(s)`. Solution 3166 / failed 0 / skipped 3. `*TemplateBindingErrorTests`
  6 / failed 0, `*TemplateBindingsApiTests` 4 / failed 0. `git diff --stat 5bbeec9 -- src/BrutalUi.Markup` still shows
  `Template/TemplateError.cs` alone. No commit and no push.

### 2026-11-17 07:06 (review group 2, CR-1 and CR-4)
- **CR-1, the empty-value test.** `EmptyValueTemplate` now loads four lines: `command=""` on the `Button`,
  `bind-text=""` on the `Label`, `bind-checked=""` on a `Toggle`, and `bind-value=""` on a `Slider`.
  `AnEmptyValueIsAnInvalidValueError` keeps its name, keeps the A2 rule, and now asserts four errors: each kind is
  `InvalidValue`, each position comes from `AssertAttribute` on its own attribute name, and each message holds its
  own quoted name (`'command'`, `'bind-text'`, `'bind-checked'`, `'bind-value'`). No other test body changed.
- **CR-4, the no-trim test.** New test `ASpacedValueIsUsedAsIsWithNoTrim` with a new `SpacedValueTemplate` that loads
  `bind-text=" hud.name "` on a `Label` while the register call holds `hud.name` with no spaces. It asserts one
  `UnknownBinding` error at the line and column of `bind-text`, `Assert.Contains("' hud.name '")` with the spaces
  inside the quote, and no root (the `LoadErrors` helper asserts that). The remarks name Decision 5, Notes item 6,
  and the fact that `AttributeContext.TryRead*` does trim, so the four binding attributes are the one exception.
- **The plants.** Scratch copies: `src/BrutalUi.Framework/obj/plantscratch/p9-TemplateBindings.Handler.orig.cs`,
  `p10-…orig.cs`, and `p11-…orig.cs` (the Handler file, because `Handle` lives there since the CR-6 split).

  | Plant | Change to `Handle` | Exit | `error CS` | Failed test |
  |---|---|---|---|---|
  | 9 | the empty-value guard of `bind-checked` removed | 2 | 0 | `AnEmptyValueIsAnInvalidValueError` |
  | 10 | the empty-value guard of `bind-value` removed | 2 | 0 | `AnEmptyValueIsAnInvalidValueError` |
  | 11 | `value = value.Trim();` added at the top of `Handle` | 2 | 0 | `ASpacedValueIsUsedAsIsWithNoTrim` |

  Logs: `artifacts/logs/p4-plant9.log`, `p4-plant10.log`, `p4-plant11.log`, with the build logs beside them. Each
  plant failed exactly one test, and the framework solution total held at 3167 in each run. Each restore passed
  `cmp` against its scratch copy, and the working Handler file returns to md5 `d341b4afb354f5d51891e91754df0dad`
  (171 lines). `TemplateBindings.cs` never opened: md5 `6f2d7c78a943799e92aa441b93e75564` (276 lines). No `src/`
  file is changed by this group.
- **Numbers.** Build `0 Warning(s)` / `0 Error(s)`. Solution 3167 / failed 0 / skipped 3 (3166 +1).
  `BrutalUi.Framework.Tests` 356 / failed 0 (355 +1); `BrutalUi.Markup.Tests` 666 / failed 0, unchanged.
  Class totals: `*TemplateBindingErrorTests` 7 / failed 0, `*TemplateBindingDemoTests` 4, `*TemplateWidgetBindingTests`
  4, `*TemplateParityTests` 2, `*TemplateBindingAllocationTests` 3, `*TemplateBindingsApiTests` 4, each `failed: 0`.
  Logs `artifacts/logs/p4-close-build.log` and `artifacts/logs/p4-close-tests.log`. No commit and no push.

### 2026-11-17 07:15 (review group 3, CR-2, CR-3, CR-10)
- **CR-3, the attribute-hook sentence.** **The attribute hook.** in section 11 of `docs/technical-design.md` now reads
  that `TemplateBindings.AttachTo` registers `command`, `bind-text`, `bind-checked`, and `bind-value` here. A grep of
  `command.*bind-text` finds no other sentence in the file that claims two names. No other doc file, no source file,
  and no test file changed.
- **CR-2, the message table.** The **The four messages.** table now quotes the shipped text. The empty-value row names
  the per-attribute example key: `'hud.score'` for `bind-text`, `'hud.muted'` for `bind-checked`, and `'hud.volume'`
  for `bind-value`, each copied from `TemplateBindings.Handler.cs`. The unknown-key row now holds the message that
  review group 1 shipped: `'<key>' is not a bound key. Register it with TemplateBindings.AddText on the
  TemplateBindings object that this loader attached first, before the load. Another TemplateBindings object on this
  loader can hold the key: only the first attached object receives the four names, so make one loader for each
  bindings set.`, and names `AddChecked` for `bind-checked` and `AddValue` for `bind-value`.
- **CR-10, the two-keys paragraph.** A new paragraph at the end of **The binding attributes.** states the pattern that
  A4 forces: one key holds one selector, so a `Slider` and the numeric `Label` that reads the same value need two
  keys, for example `hud.volume` for `bind-value` and `hud.volume-text` for `bind-text`, and points at
  `Shared/BoundTemplate.cs`.
- **Templates.** The section 11 first template is untouched, and the **The binding attributes.** template still equals
  the demo-test template (`SectionElevenTemplate` in `TemplateBindingDemoTests.cs`): both blocks read word for word
  equal after the edits. No fenced block changed.
- **Numbers.** Build `0 Warning(s)` / `0 Error(s)`. Solution 3167 / failed 0 / skipped 3, unchanged by the doc edits.
  `BrutalUi.Framework.Tests` 356, `BrutalUi.Markup.Tests` 666. `code-review.md` now ticks CR-2, CR-3, and CR-10: 8 of
  10 resolved, with CR-5 and CR-9 for later groups. No commit and no push.

### 2026-11-17 07:16 (review group 4, CR-5)
- **CR-5, the four doc comments.** `Templates/TemplateBindingEntry.cs` lines 33, 38, 43, and 48 now read
  `Tries to bind the text of a label.`, `Tries to bind the text of a text field.`, `Tries to bind the checked
  state of a toggle.`, and `Tries to bind the value of a slider.` Each sentence keeps its meaning, its parameter
  text, and its return text. No signature, name, field, message string, or behavior changed, in this file or in
  any other file.
- **The grep.** `grep -rn 'Trys' src tests --include=*.cs --exclude-dir=obj` prints nothing. The plain brief
  grep still shows 8 hits: four in each of the untracked plant backup copies
  `src/BrutalUi.Framework/obj/plantscratch/p2-TemplateBindingEntry.orig.cs` and
  `p4-TemplateBindingEntry.orig.cs`. `git check-ignore` proves they are ignored, the build compiles neither, and
  group 4 changes no file but the one source file, so the backups stay untouched.
- **Numbers.** Build `0 Warning(s)` / `0 Error(s)`. Solution 3167 / failed 0 / skipped 3, unchanged.
  `BrutalUi.Framework.Tests` 356 / failed 0; `BrutalUi.Markup.Tests` 666 / failed 0;
  `*TemplateBindingErrorTests` 7 / failed 0. Log `artifacts/logs/g4-close-tests.log`. No commit and no push.

### 2026-11-17 07:18 (review group 5, CR-9)
- **CR-9, the sibling plan Notes item 9.** The opening label of item 9 of
  `docs/features/markup/xml-element-loader/implementation.md` now reads **Settled open point for
  binding-attributes: the registry of the load.** One added sentence in the same item states the settlement:
  markup/binding-attributes settled this point in Decision 1 of
  `docs/features/markup/binding-attributes/implementation.md` — the registry of the load is the registry of the
  attached `TemplateBindings`, `AttachTo` adds its instance method as the `AttributeHandler` and so captures that
  registry, the hook contract did not change (A7), and no state argument reached `Load`. Every original word of
  the item stays below the new sentence, so the history reads on.
- **Footprint.** `git diff --stat 5bbeec9 -- docs/features/markup/xml-element-loader` shows one file and one
  changed line: `1 insertion(+), 1 deletion(-)`, the re-labelled line itself. No original word of the item is
  deleted, and no other sibling file changed. `src/BrutalUi.Markup/Template/TemplateLoader.cs` stayed untouched:
  its stale remark at line 219 stays protected by A7 and Notes item 10 of this feature's plan.
- **Runs.** Docs-only group, per the brief: no build and no test run. `code-review.md` now ticks CR-5 and CR-9,
  so all 10 findings read resolved. No commit and no push.

### 2026-11-17 07:30 (/autorun-feature close-out, /update-feature)
- The whole feature ran in one autonomous pass: plan read → docs scaffolded → Phase 1 (ADVANCED) → Phase 2 (2 groups) →
  Phase 3 (2 groups), each phase followed by an independent verifier agent, then `/review-feature` with five fix groups
  and a final verifier pass. 18 tasks, 11 plants, 10 review findings, all closed.
- **Deviation from the plan's numbers, on purpose.** The plan expected Framework +23 and solution 3166. Review finding
  CR-4 added one test (`ASpacedValueIsUsedAsIsWithNoTrim`, the Decision 5 no-trim guard), so the real close is Framework
  **+24** (332 → 356) and solution **3167** / failed 0 / skipped 3. Every other plan number held: each existing test
  passed with no edit, the Markup diff stayed `TemplateError.cs` (8 lines), `package-test: PASS Gauge level 0.5`,
  `TemplateBindings` stayed the only new public type, and no file exceeds the 400-line cap (247 / 276 / 171).
- Inbox folded: 2 lines, both closed.
  - Inbox bug: Verifier FAIL, Phase 2 gate 12 — `context.md` recorded only plant 3, not plants 4 and 5 (fixed by
    `phase-2-fix1.md`; the re-verify agent cross-checked the note against `artifacts/logs/p2-plant4.log` and
    `p2-plant5.log` and returned PASS).
  - Inbox bug: Orchestrator fault — four duplicate Phase-3 group-2 agents raced on the tree and one left
    `TemplateBindings.cs` in the plant-8 state (stopped, restored from `p8-TemplateBindings.orig.cs`, `cmp` clean, and
    proven by hash in the Phase-3 gate and again in the review-pass gate). See Gotchas & Learnings.
  - No open `note` or `change` line and no open `bug` line, so `/update-feature` promoted no bug to `docs/bugs/`.
- Needs Human Verification holds the three demo lines plus the Windows script run. No UI existed to browser-verify: this
  feature ships no screen, so every check ran through the test binaries, the greps, and the package test.
- Ship state for the human: branch `feat/markup/binding-attributes`, HEAD still `5bbeec9`, work uncommitted by design.
- `implementation.md` `Status:` set to Ready for Review. Next feature of the epic: `markup/definition-loader` (feature 5
  of 6), which builds on the attached `TemplateBindings` and may put its record loader in
  `src/BrutalUi.Framework/Templates/`.

