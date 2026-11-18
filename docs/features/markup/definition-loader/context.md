# markup/definition-loader - Context & Decisions

**Epic:** markup (feature 5 of 6)
**Last Updated:** 2026-11-18 21:30
**Current Phase:** Complete (phases 1 to 3, the cross-phase review, and the review fixes are done)
**Status:** ✅ Ready for Review. The docs step ran: 20/20 tasks, five review findings closed, one review verifier PASS, and `docs/technical-design.md` section 11 holds the format, the errors, and the rules of the object.

---

## Quick Status

**What's Done:**
- ✅ The plan interview: Q1 to Q7 and A1 to A9 are confirmed. `implementation.md` holds them under `### Confirmed Assumptions`.
- ✅ The plan: 3 slices, 20 tasks, about 190 steps.
- ✅ Phase 1 group 1 (tasks 1.1 to 1.5): the four public types, `SlotContainerTemplate`, and the `DefinitionReader` placeholder exist. The build has 0 warnings. The test totals equal the baseline below.
- ✅ Phase 1 group 2 (tasks 1.6 to 1.8): the read pass (`DefinitionReader.cs` and `DefinitionReader.Elements.cs`), the resolve pass (`DefinitionReader.Resolve.cs`), the four demo tests, plants 1 and 2, and the section 10 and 11 edits in `technical-design.md`. Build `0 Warning(s)`, `0 Error(s)`. Solution 3171 (0 failed, 3 skipped), Framework 360. Phase 1 acceptance holds.
- ✅ Phase 2 group 1 (tasks 2.1 to 2.3): the `actions` attribute, the direct `PanelRef` under `Mode`, the action map lookup and the default-slot placement rule in the resolve pass, and five tests in `DefinitionRecordTests.cs`. Plants 3 and 4 ran. Build `0 Warning(s)`, `0 Error(s)`. Solution 3176 (0 failed, 3 skipped), Framework 365. Demo class `*DefinitionLoaderDemoTests` still reads 4.

- ✅ Phase 2 group 2 (tasks 2.4 to 2.6): `DefinitionSet.cs`, `DefinitionParityTests.cs` with 2 tests, `ModeDiffAllocationTests.Loaded.cs` with 1 test, the `partial` edit of `ModeDiffAllocationTests.cs`, plant 5, and the section 10, 11, and 14 doc edits. Build `0 Warning(s)`, `0 Error(s)`. Solution 3179 (0 failed, 3 skipped), Framework 368. Phase 2 acceptance holds.

- ✅ Phase 3 group 1 (tasks 3.1 to 3.3): the full read-pass messages (the `Window` case, the empty-value guidance, and `UnexpectedText`), the resolve-pass duplicate check, the mode panel check, the check load of each used layout with the slot walk (Q3, A5; review group 2 widened it to each non-faulty layout, Decision 15), the slot check, the completed messages, and the follow-on rule. New `DefinitionErrorTests.cs` with 10 tests. Plants 6, 7, and 8 ran. One phase 2 test text needed one line (Decision 13). Build `0 Warning(s)`, `0 Error(s)`. Solution 3189 (0 failed, 3 skipped), Framework 378. Group 1 acceptance holds.

- ✅ Phase 3 group 2 (tasks 3.4 to 3.6): the empty-name and duplicate-add guards in `DefinitionLoader`, the constructor and `RegisterTo` remarks, new `DefinitionLoaderApiTests.cs` with 4 tests, plant 9, the section 11 error block, the changelog line, and the package run (`package-test: PASS Gauge level 0.5`). Build `0 Warning(s)`, `0 Error(s)`. Solution 3193 (0 failed, 3 skipped), Framework 382. Phase 3 acceptance holds.

- ✅ Cross-phase review (`/review-feature`, the C# library rule set of `AGENTS.md` and the quality bar): 5 findings (0 CRITICAL, 0 HIGH, 3 MEDIUM, 2 LOW), all 5 fixed. `code-review.md` reads `Resolved - 5 findings, 5 resolved`.

- ✅ Review-fix group 1 (CR-2, CR-4, CR-5): the failed-parent check moved before the namespace check, so one fault gives one error; `PlaceMessage` builds its fix sentence from `TemplateLoader.Namespace`; the attribute name tables are `static readonly string[]` fields, `string.Join` runs only in the error branch, and `Load` reads the input span in place through an `UnmanagedMemoryStream` in a `fixed` block. New `DefinitionReader.Tables.cs` (157 lines) and `DefinitionErrorTests.FollowOn.cs` (31). Solution 3194, Framework 383, `*DefinitionErrorTests` 11.

- ✅ Review-fix group 2 (CR-1, CR-3): the resolve pass marks a faulty entry instead of skipping it, so a faulty mode still reports `UnknownPanel` and the duplicate-panel faults, and a `Panel` with no `slot` and an unknown `template` reports both. Step 3b check-loads each non-faulty layout, also one that no mode names, so a successful result holds no factory that throws inside `RegisterTo` (Decision 15). New `DefinitionReader.Resolve.Checks.cs` (145) and `DefinitionErrorTests.Layouts.cs` (64); `DefinitionErrorTests.cs` gained the word `partial`. Solution 3195, Framework 384, `*DefinitionErrorTests` 12.

- ✅ Review fixes verified by one independent verifier: PASS with Demo: PASS. Final state: build `0 Warning(s)`, `0 Error(s)`; solution 3195 (0 failed, 3 skipped); `BrutalUi.Framework.Tests` 384 (356 baseline, +28); the protected-path diff against `9fa4b43` empty; every file under the 400-line cap.

- ✅ Docs step (`/update-feature markup/definition-loader`): `tasks.md`, this file, and `implementation.md` hold the close state; the `TemplateFault` row of section 11 and the `CHANGELOG.md` line now name each non-faulty layout, to match Decision 15. `inbox.md` holds no line.

**What's Next:**
- 📋 A person tests the three demo lines and the review-fix line under `## Needs Human Verification` below.
- 📋 The coordinator: merge `feat/markup/definition-loader` into the epic branch, copy the open human tests below to `docs/features/markup/human-testing.md`, and run `/update-epic markup` and `/update-master`.
- ⏳ `markup/markup-sample` (feature 6 of 6) loads the HelloPanel records through this API and closes the epic.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Templates/DefinitionLoader.cs` - The public loader. It holds the template table and the action map table, and it registers the `SlotContainer` type on the `TemplateLoader`.
- `src/BrutalUi.Framework/Templates/DefinitionReader.cs` - The read pass: the state, `Load`, `AddError`, the sort, and the entry structs.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Elements.cs` - The walk of the read pass: one element, its place, and its attributes. Group 2 added this partial file, because one file passed the 400-line cap.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Tables.cs` - The static name tables and message builders of the walk (`ResolveKind`, `PlaceMessage`, `EmptyValueMessage`, `AllowedNames`, `RequiredNames`). Review group 1 split this file, because the CR-5 fields block took `DefinitionReader.Elements.cs` past the 400-line cap.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs` - The resolve pass. It checks the names, check-loads each sound layout, and builds the three records.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.Checks.cs` - The resolve helpers: the check load, the slot walk, the name tables, the duplicate report, and the factory. Review group 2 split this file, because the step 4 rework pushed the pass toward the 400-line cap.
- `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs` - The records or the errors, and `RegisterTo(UiManager)`.
- `src/BrutalUi.Framework/Templates/DefinitionError.cs` - `DefinitionError` and `DefinitionErrorKind` (13 kinds).
- `src/BrutalUi.Framework/Templates/SlotContainerTemplate.cs` - The `SlotContainer` element type with the required `slot` attribute.

### Related Files
- `src/BrutalUi.Framework/Panels/PanelDefinition.cs`, `LayoutDefinition.cs`, `ModeDefinition.cs`, `UiManager.cs` - The records and the register calls. This feature does not change them.
- `src/BrutalUi.Markup/Template/TemplateLoader.cs`, `TemplateReader.cs` - The template loader and the read pattern. This feature does not change Markup (A8).
- `src/BrutalUi.Framework/Templates/TemplateBindings.cs` - The bindings that the game attaches to the `TemplateLoader`.
- `docs/features/markup/definition-loader/implementation.md` - The plan.

---

## Baseline Record (task 1.1)

- **Base commit:** `9f8f14e99daa35d27ff7d113bf8ca418f475c333` at the group start (planning-time reference: `9fa4b43`; the acceptance diff runs against `9fa4b43`). `git status --porcelain` was clean at the start.
- **Solution total, measured before any change:** 3167, failed 0, succeeded 3164, skipped 3. It equals Notes item 10.
- **`BrutalUi.Framework.Tests` total, measured before any change:** 356, failed 0, skipped 0.

## Group 2 Close Record (tasks 1.6 to 1.8)

- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3171 total, 0 failed, 3168 succeeded, 3 skipped. The baseline plus the 4 new tests.
- **`BrutalUi.Framework.Tests`:** 360 total, 0 failed, 0 skipped. The baseline plus 4.
- **Demo state:** `BrutalUi.Framework.Tests --filter-class '*DefinitionLoaderDemoTests'` → total 4, `failed: 0`. The four tests are `OneLoadedPanelMountsInItsSlot`, `ThePanelFactoryLoadsItsTemplateAtEachCall`, `TheLayoutTemplateBuildsASlotContainer`, and `TheEntryOrderDoesNotMatter`.
- **Plant 1** (`DefinitionReader.Resolve.cs`, the panel and layout factory caches the first root and returns it at each call): exit 2, 0 `error CS` lines, 1 failed — `ThePanelFactoryLoadsItsTemplateAtEachCall`. Restore: `cmp` reports the file identical to its copy; the re-run gives 3171, 0 failed.
- **Plant 2** (`SlotContainerTemplate.cs`, the factory ignores the value and uses the name `slot`): exit 2, 0 `error CS` lines, 3 failed — `OneLoadedPanelMountsInItsSlot`, `TheLayoutTemplateBuildsASlotContainer`, and `TheEntryOrderDoesNotMatter`. The brief named the first two. The third shares the layout template, so its `RegisterTo` throws `ArgumentException` for the unknown slot `top`. The extra kill is a stronger guard, not a fault of the plant. Restore: `cmp` reports the file identical to its copy.
- **Docs:** section 10, **Panel definitions**, holds the new `DefinitionLoader` bullet. Section 11 holds **### Definition format** before **### Stylesheet format**: the definitions text, the panel template, the layout template, the C# demo, the element table without `actions`, the entry-order rule, and the rules of The Factories. A byte compare of the definitions text against `DefinitionsText` of the demo test gives equal.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- **File sizes:** `DefinitionReader.cs` 361 lines, `DefinitionReader.Elements.cs` 349, `DefinitionReader.Resolve.cs` 228, `DefinitionLoaderDemoTests.cs` 222. Each new source file stays under 400.
- **Verifier (post-phase, round 1):** PASS. Build `0 Warning(s)`, `0 Error(s)`. Solution 3171, 0 failed, 3 skipped; Framework 360, 0 failed. Protected-path diff empty. Section 10 bullet at line 1144; section 11 **### Definition format** at line 1635. Demo command: `*DefinitionLoaderDemoTests` total 4, `failed: 0`. Demo: PASS. UI check: none (headless test proof).

## Phase 2 Group 1 Close Record (tasks 2.1 to 2.3)

- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3176 total, 0 failed, 3173 succeeded, 3 skipped. The Phase 1 close plus the 5 new tests.
- **`BrutalUi.Framework.Tests`:** 365 total, 0 failed, 0 skipped. The Phase 1 close (360) plus 5.
- **Demo state:** `*DefinitionRecordTests` → total 5, `failed: 0`. `*DefinitionLoaderDemoTests` still reads total 4, `failed: 0`. The five tests are `AModeNamesItsActionMap`, `AModeWithNoActionsHasNoMap`, `APanelRefWithNoSlotUsesTheDefaultSlot`, `TheSlotOrderAndThePanelOrderFollowTheText`, and `ABoundPanelTemplateBindsThroughTheAttachedBindings`.
- **Plant 3** (`DefinitionReader.Resolve.cs`, a placement with no slot value takes the first slot of the mode instead of the panel default slot): exit 2, 0 `error CS` lines, 2 failed — `APanelRefWithNoSlotUsesTheDefaultSlot` and `TheSlotOrderAndThePanelOrderFollowTheText`. The brief named the first. The second also places one panel with a direct `PanelRef`, so it fails with the plant: a stronger guard, not a fault of the plant. Restore: `cmp` reports the file identical to its copy; the re-run gives 3176, 0 failed.
- **Plant 4** (`DefinitionReader.Resolve.cs`, `modes.Add(...)` passes `null` as the map): exit 2, 0 `error CS` lines, 1 failed — `AModeNamesItsActionMap`, as the brief expects. Restore: `cmp` reports the file identical to its copy.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- **File sizes:** `DefinitionReader.cs` 366 lines, `DefinitionReader.Elements.cs` 373, `DefinitionReader.Resolve.cs` 263, `DefinitionRecordTests.cs` 365. Each new source file stays under 400.
- **Group 1 changed no doc of `technical-design.md`.** Task 2.6 owns the `actions` row, the direct `PanelRef` rule, and the parity rule in section 11.

## Phase 2 Group 2 Close Record (tasks 2.4 to 2.6)

- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3179 total, 0 failed, 3176 succeeded, 3 skipped. The group 1 close plus the 3 new tests.
- **`BrutalUi.Framework.Tests`:** 368 total, 0 failed, 0 skipped. The group 1 close (365) plus 3.
- **Demo state:** `*DefinitionParityTests` → total 2, `failed: 0`: `TheLoadedRecordsGiveTheSameModeDiffAsTheCodeRecords` and `TheLoadedRecordsHoldTheSameDataAsTheCodeRecords`. `*ModeDiffAllocationTests` → total 4, `failed: 0`. `TheToggleCycleOnLoadedRecordsAllocatesNothing` measures 0 bytes over 100 toggle steps with the loaded records, asserts the sum 1200, and reads the loaded score label equal to the store value.
- **Plant 5** (`UiManager.cs`, `GC.KeepAlive(new object());` as the first line of `UiManager.Apply`): exit 2, 0 `error CS` lines, 3 failed — both toggle gates, `TheToggleCycleOnLoadedRecordsAllocatesNothing` and `TheToggleCycleAllocatesNothingAfterWarmUp` (9600 bytes each), and `ModeDiffTests.SetModeAndTheDiffAllocateNothingAfterWarmUp`. The brief names the first two. The third shares `Apply`, so its kill is a stronger guard, not a fault of the plant. Restore: `cmp` reports the file identical to its copy; the re-run gives 3179, 0 failed.
- **Docs:** section 10, **Mode set**, holds the loaded-record bullet. Section 11, **### Definition format**, holds the `actions` and map row of the `Mode` table, the direct `PanelRef` row with the Q5 rule, the `actions` and Q5 prose, and the **Parity** paragraph. Section 14, the eighteenth-gate bullet, reads 4 tests and names `TheToggleCycleOnLoadedRecordsAllocatesNothing` and plant 5.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output. The diff of `ModeDiffAllocationTests.cs` shows one changed line: the `partial` keyword.
- **File sizes:** `DefinitionSet.cs` 256 lines, `DefinitionParityTests.cs` 178, `ModeDiffAllocationTests.Loaded.cs` 177. Each new file stays under 400.
- **Verifier (post-phase, round 1):** PASS, no fix round. Build `0 Warning(s)`, `0 Error(s)`. Solution 3179, 0 failed, 3 skipped; `BrutalUi.Framework.Tests` 368, 0 failed. `*DefinitionRecordTests` total 5, `*ModeDiffAllocationTests` total 4, each `failed: 0`. Protected-path diff empty; the `ModeDiffAllocationTests.cs` diff shows the one `partial` line; every listed file stays under 400 lines; no reflection use in `Definition*.cs` or `SlotContainerTemplate.cs`; plant 5 left no residue. Section 11 holds the `actions` row and the direct `PanelRef` Q5 rule, section 10 **Mode set** names `DefinitionParityTests` (line 1185), and section 14 names `TheToggleCycleOnLoadedRecordsAllocatesNothing` (line 1980). Demo: PASS. UI check: none (headless test proof). Faults: none.

## Phase 3 Group 1 Close Record (tasks 3.1 to 3.3)

- **Edits:** `DefinitionReader.cs` (the `UnexpectedText` case of the walk, the `UnderSlot` mark of `PlacementEntry`, the scope remarks), `DefinitionReader.Elements.cs` (the `Window` message, the `EmptyValueMessage` table, the `UnderSlot` fill), `DefinitionReader.Resolve.cs` (rewrite: the duplicate check, the fault marks, the check load with the slot walk, the slot check, the completed messages, the follow-on rule), `DefinitionRecordTests.cs` (one line of the shared layout template, Decision 13), and new `tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs` (10 tests).
- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3189 total, 0 failed, 3186 succeeded, 3 skipped. The group start (3179) plus the 10 new tests.
- **`BrutalUi.Framework.Tests`:** 378 total, 0 failed, 0 skipped. The group start (368) plus 10.
- **Demo state:** `*DefinitionErrorTests` → total 10, `failed: 0`. The classes `*DefinitionLoaderDemoTests`, `*DefinitionRecordTests`, `*DefinitionParityTests`, and `*ModeDiffAllocationTests` read 4, 5, 2, and 4 with `failed: 0`.
- **Kind coverage:** all 13 `DefinitionErrorKind` members occur in at least one assert of `DefinitionErrorTests`: `NotWellFormed` and `UnexpectedText` in `TextAndBadXmlGiveTheirKinds`, `UnknownElement` in `AWindowOrAnotherUnknownElementIsAnUnknownElementError`, `UnknownAttribute`, `MissingAttribute`, and `InvalidValue` in `AnAttributeFaultGivesItsKind`, `UnknownTemplate` in `AnUnknownTemplateNameIsAnUnknownTemplateError` and `OneFaultGivesOneErrorAndNoRecord`, `UnknownActionMap` in `AnUnknownActionMapNameIsAnUnknownActionMapError`, `UnknownLayout` and `UnknownPanel` in `AnUnknownLayoutOrPanelNameIsAnError`, `DuplicateName` in `ADuplicateNameIsADuplicateNameError` and `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry`, `UnknownSlot` in `ASlotThatTheLayoutDoesNotHoldIsAnUnknownSlotError`, and `TemplateFault` in `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry`.
- **Plant 6** (`DefinitionReader.Resolve.cs`, the duplicate check skips the modes): exit 2, 0 `error CS` lines, 1 failed — `ADuplicateNameIsADuplicateNameError`, as the brief expects. Restore: `cmp` reports the file identical to its copy; the re-run passes.
- **Plant 7** (`DefinitionReader.Resolve.cs`, the two slot checks never fire): exit 2, 0 `error CS` lines, 1 failed — `ASlotThatTheLayoutDoesNotHoldIsAnUnknownSlotError`, as the brief expects. Restore: `cmp` equal.
- **Plant 8** (`DefinitionReader.Resolve.cs`, a placement whose panel entry has no record gives `UnknownPanel`, the faulty entry leaving its table): exit 2, 0 `error CS` lines, 1 failed — `OneFaultGivesOneErrorAndNoRecord`, as the brief expects. Restore: `cmp` equal; the re-run gives 3189, 0 failed.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- **File sizes:** `DefinitionReader.cs` 387 lines, `DefinitionReader.Elements.cs` 394, `DefinitionReader.Resolve.cs` 396, `DefinitionErrorTests.cs` 398. Each new source file stays under 400.
- **Message notes:** the `UnknownTemplate` and `UnknownActionMap` messages now name `DefinitionLoader.AddTemplate` and `DefinitionLoader.AddActionMap`. The empty-value message keeps the brief wording for `slot` and carries a per-attribute fix sentence for the other names (Decision 14).

## Phase 3 Group 2 Close Record (tasks 3.4 to 3.6)

- **Edits:** `DefinitionLoader.cs` (the four guard-message constants, the empty-name and duplicate checks before each add, the constructor remarks that name the `TemplateLoader.Register` throw, and the end-state rule paragraphs), `DefinitionLoadResult.cs` (the `RegisterTo` remarks that name the `ArgumentException` of a held manager name), new `tests/BrutalUi.Framework.Tests/Templates/DefinitionLoaderApiTests.cs` (4 tests), section 11 of `docs/technical-design.md` (the `DefinitionErrorKind` block, the 13-kind cause table, the position rule, the follow-on rule, the Q3 and A5 check, and the rules of the object), and one `### Added` line in `CHANGELOG.md`.
- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3193 total, 0 failed, 3190 succeeded, 3 skipped. The group start (3189) plus the 4 new tests. The phase closes at Framework +14 in phase 3 and +26 in total: 356 baseline to 382.
- **`BrutalUi.Framework.Tests`:** 382 total, 0 failed, 0 skipped.
- **Demo state:** `*DefinitionLoaderApiTests` → total 4, `failed: 0`. The tests are `TheMethodsCheckTheirArguments`, `ASecondAddForOneNameThrows`, `ASecondDefinitionLoaderOnOneTemplateLoaderThrows`, and `RegisterToChecksTheResultAndTheManager`. `*DefinitionErrorTests` still reads total 10, `failed: 0`. `BrutalUi.Markup.Tests` passes with no edit.
- **Plant 9** (`DefinitionLoadResult.cs`, `RegisterTo` skips the check of a failed result): exit 2, 0 `error CS` lines, 1 failed — `RegisterToChecksTheResultAndTheManager`, as the brief expects. Restore: `cmp` reports the file identical to its copy; the re-run gives 3193, 0 failed.
- **Package run:** `pack`, `package-restore`, and `package` of `scripts/test-linux.sh`, by hand: `package-test: PASS Gauge level 0.5`.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output. The group changed no existing test.
- **File sizes:** `DefinitionLoader.cs` 173 lines, `DefinitionLoadResult.cs` 156, `DefinitionLoaderApiTests.cs` 218. Each file stays under 400.
- **API notes:** each `AddTemplate` and `AddActionMap` guard throws `ArgumentException` with `ParamName` `name` and a constant message; `UiManager.Register` throws with `ParamName` `panel` (the `RegisterTo` clash test reads it). The `SetMode` throw of an unregistered name has `ParamName` `name`. The guards land Decision 4.
- **Verifier (post-phase, round 1):** PASS, no fix round. Build `0 Warning(s)`, `0 Error(s)`. Solution 3193, 0 failed, 3 skipped; `BrutalUi.Framework.Tests` 382, 0 failed. `*DefinitionErrorTests` total 10, `*DefinitionLoaderApiTests` total 4, each `failed: 0`; the Phase 1 and 2 classes read 4, 5, 2, and 4. All 13 kinds asserted (30 kind asserts). Protected-path diff empty; the `DefinitionRecordTests.cs` diff against `02c34e8` holds only the Decision 13 template line and its doc-comment reword (the accepted deviation). Every listed file under 400 lines; no reflection use; no plant residue. Section 11 holds the error block, the 13-kind table, the position rule, the follow-on rule, the Q3 and A5 check, and the rules of the object; `CHANGELOG.md` holds one tagged `### Added` line. Package run by hand: `package-test: PASS Gauge level 0.5`. Demo: PASS. UI check: none (headless test proof). Faults: none.

## Review Fix Close Record (CR-1 to CR-5)

- **Findings:** 5 (0 CRITICAL, 0 HIGH, 3 MEDIUM, 2 LOW), all resolved. `code-review.md` holds each finding, the fix, and its checked box.
- **Edits, review group 1 (CR-2, CR-4, CR-5):** `DefinitionReader.Elements.cs` (the failed-parent check runs before the namespace check, the root message builds from `TemplateLoader.Namespace`, the static tables moved out), `DefinitionReader.cs` (`Load` reads the input span in place through an `UnmanagedMemoryStream` inside a `fixed` block, the remarks name the new partial file), new `DefinitionReader.Tables.cs`, and new `DefinitionErrorTests.FollowOn.cs` with `AChildOutsideTheNamespaceUnderAFailedElementGivesOneError`.
- **Edits, review group 2 (CR-1, CR-3):** `DefinitionReader.Resolve.cs` (steps 2, 3, and 4 mark a faulty entry instead of skipping it; step 3b check-loads each non-faulty layout, Decision 15), new `DefinitionReader.Resolve.Checks.cs` (six helpers), `DefinitionErrorTests.cs` (the word `partial`, the Q3 case moved out), new `DefinitionErrorTests.Layouts.cs` (the flipped Q3 case), `DefinitionErrorTests.FollowOn.cs` (`AFaultyEntryStillGivesItsOtherFaults`), and section 11 of `docs/technical-design.md` (**The check of each layout (Q3, A5)**).
- **Build:** `0 Warning(s)`, `0 Error(s)`. **Solution tests:** 3195 total, 0 failed, 3 skipped (the phase 3 close 3193 plus the 2 review cases). **`BrutalUi.Framework.Tests`:** 384 total, 0 failed, 0 skipped: +28 over the 356 baseline, where the plan expected +26.
- **Demo state:** `*DefinitionErrorTests` → total 12, `failed: 0`. `*DefinitionLoaderDemoTests`, `*DefinitionRecordTests`, `*DefinitionParityTests`, and `*ModeDiffAllocationTests` read 4, 5, 2, and 4, each with `failed: 0`.
- **File sizes:** `DefinitionReader.cs` 397 lines, `DefinitionReader.Elements.cs` 268, `DefinitionReader.Tables.cs` 157, `DefinitionReader.Resolve.cs` 274, `DefinitionReader.Resolve.Checks.cs` 145, `DefinitionErrorTests.cs` 352, `DefinitionErrorTests.FollowOn.cs` 63, `DefinitionErrorTests.Layouts.cs` 64. Each file stays under 400.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output. No existing assertion changed except the named Q3 flip of Decision 15.
- **Verifier (review-fix run, round 1):** PASS, Demo: PASS. UI check: none (headless test proof). No `bug` line and no inbox item reached the run.

## Tool Notes

- Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only.
- The end-of-group pair: `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, 0 failed, and the totals above unchanged. A warning fails the build.
- Per-class run after a build: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`.
- Plants: copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored), change it, run the solution tests into a log, and check the `error CS` count first: it must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect a non-zero exit. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time; run a second time before you treat it as a fault.
- Harness note (dsh sandbox): `/tmp` is private to each shell call. Write a test log and read its totals inside one chained command, or write the log under `obj/`.

## Plan Record (recorded from `implementation.md` by task 1.1)

### Public API (end state)

```csharp
namespace BrutalUi.Framework;

public sealed class DefinitionLoader
{
    public DefinitionLoader(TemplateLoader templates);              // registers the SlotContainer type on templates
    public void AddTemplate(string name, ReadOnlySpan<byte> utf8);  // copies the text
    public void AddActionMap(string name, UiActionMap map);
    public DefinitionLoadResult Load(ReadOnlySpan<byte> utf8, string sourceName = "definitions");
}

public sealed class DefinitionLoadResult
{
    public IReadOnlyList<PanelDefinition> Panels { get; }   // text order. Empty when Errors holds an error
    public IReadOnlyList<LayoutDefinition> Layouts { get; }
    public IReadOnlyList<ModeDefinition> Modes { get; }
    public IReadOnlyList<DefinitionError> Errors { get; }   // sorted by line, then column
    public bool Succeeded { get; }
    public void RegisterTo(UiManager ui);                   // panels, then layouts, then modes (Q6)
}

public enum DefinitionErrorKind : byte
{
    NotWellFormed, UnknownElement, UnknownAttribute, MissingAttribute, InvalidValue, UnexpectedText,
    UnknownTemplate, UnknownActionMap, UnknownLayout, UnknownPanel, DuplicateName, UnknownSlot, TemplateFault,
}

public readonly struct DefinitionError
{
    public DefinitionErrorKind Kind { get; }
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    public string SourceName { get; }
    public override string ToString();   // "<source>(<line>,<column>): <kind>: <message>"
}
```

The names of templates and maps are `string` keys with an ordinal compare, because `XmlReader` gives each value as a `string`. The name has no span overload, so `BRUTALCORE0011` accepts a literal name. The text of `AddTemplate` and of `Load` is a span, so a test passes a `u8` literal. `DefinitionError` has an internal constructor, as `TemplateError` has.

### The Format

```xml
<ui:Definitions xmlns:ui="brutal-ui">
  <ui:Panel name="hud" slot="top" template="hud.panel" />
  <ui:Layout name="game" template="game.layout" />
  <ui:Mode name="play" layout="game" actions="play.actions">
    <ui:Slot name="top">
      <ui:PanelRef name="hud" />
    </ui:Slot>
    <ui:PanelRef name="chat" />
  </ui:Mode>
</ui:Definitions>
```

```xml
<ui:Element pickable="false" xmlns:ui="brutal-ui">
  <ui:SlotContainer slot="top" />
  <ui:SlotContainer slot="bottom" />
</ui:Element>
```

| Element | Parent | Attributes | Record |
|---|---|---|---|
| `Definitions` | none (the root) | none | the result |
| `Panel` | `Definitions` | `name`, `slot`, `template` (all required) | `PanelDefinition(name, slot, factory)` |
| `Layout` | `Definitions` | `name`, `template` (both required) | `LayoutDefinition(name, factory)` |
| `Mode` | `Definitions` | `name`, `layout` (required), `actions` (optional) | `ModeDefinition(name, layout, placements, map)` |
| `Slot` | `Mode` | `name` (required) | a group of placements |
| `PanelRef` | `Slot` or `Mode` | `name` (required) | one `ModePanel`. A direct child of `Mode` takes the `DefaultSlot` of the panel (Q5) |
| `SlotContainer` | a layout template | `slot` (required) | `new SlotContainer(slot)`, a leaf |

The entry order in the text does not matter. A `Mode` can come before its `Panel` and its `Layout`. The placements of a mode keep the text order, and `ModeDefinition` groups them by slot in the order of the first entry of each slot.

### The Two Passes

**The read pass (`DefinitionReader.cs`).** One forward `XmlReader` walk with the settings of `TemplateReader.Load` (DTD prohibited, no resolver, comments, processing instructions, and whitespace ignored, `ConformanceLevel.Document`). Each element goes through these checks, in order: the namespace, the place under its parent, the attributes (a prefixed attribute, an unknown attribute, an empty value), and the required attributes. A frame stack tracks the parent, as the `_frames` stack of `TemplateReader` does. A failed element pushes a failed frame, so its children give no error. Each `Panel`, `Layout`, and `Mode` becomes an entry. Each value is a `DefinitionValue(string Text, int Line, int Column)` with the position of its attribute name. `default` means no value.

**The resolve pass (`DefinitionReader.Resolve.cs`).** It runs after the read pass, also when the read pass found an error, so one load gives each error. It stops only for `NotWellFormed`.
1. **The name tables.** One ordinal table for the panels, one for the layouts, and one for the modes. The first entry of a name wins. A later entry gives `DuplicateName`. The three kinds have three tables, as in `UiManager`.
2. **The panels.** The `template` value must be a registered template, or `UnknownTemplate`. The record gets a factory that loads the template at each call (A4).
3. **The layouts.** The `template` value must be a registered template, or `UnknownTemplate`. A layout that a mode names loads its template one time (Q3). A template error gives `TemplateFault` at the `Layout` element, with each `TemplateError.ToString` line in the message (A5). The walk collects the slot names of each `SlotContainer` in pre-order and does not enter a slot. Two slots of one name give `DuplicateName` at the `Layout` element, because `UiManager.Register(LayoutDefinition)` throws for them. The pass drops the tree and keeps the slot names.
   *(Correction, review group 2, Decision 15: the resolve pass check-loads each layout that is not faulty, also a layout that no mode names, one time per layout.)*
4. **The modes.** The `layout` value must be a `Layout` entry of the text, or `UnknownLayout`. The `actions` value must be a registered map, or `UnknownActionMap`. Each placement names a `Panel` entry of the text, or `UnknownPanel`. A panel two times in one mode gives `DuplicateName` at the second `PanelRef`. The slot of a placement is the `Slot` name, or the `DefaultSlot` of the panel for a direct `PanelRef`. A slot that the layout does not hold gives `UnknownSlot`.
5. **The result.** One error or more gives a sorted error list and no record. No error gives the three lists in text order.

**The position rule.** An error at an element points at the first character of the element name. An error at a value points at the first character of its attribute name. This is the rule of `TemplateError`. `MissingAttribute`, `UnknownElement`, `TemplateFault`, and the duplicate slot of a template sit at the element. Each other kind sits at its attribute.

**The follow-on rule (one fault, one error).** An entry with a name stays in its name table, also when another value of the entry has a fault. Such an entry is faulty. A reference to a faulty entry gives no error. A mode on a faulty layout skips the slot check. A placement with no panel record skips the slot check. So a `Panel` with no `template` gives one `MissingAttribute`, and a `Mode` that names it gives nothing more.

### The Factories

- **A panel factory** captures the `TemplateLoader`, the template text copy, and the template name. Each call runs `templates.Load(text, templateName).GetRootOrThrow()`. The controller calls it one time, at the first mount. A fault of the template throws `InvalidOperationException` at that call, and the error rule of `UiManager` applies: the target stays, and the next `Update` tries again (A4).
- **A layout factory** is the same. `UiManager.Register(LayoutDefinition)` calls it one time. The check of Q3 found no fault in a used layout, so that call does not throw for the text.
  *(Correction, review group 2, Decision 15: the check covers each non-faulty layout, also one that no mode names, so no factory of a successful result throws.)*
- The template name is the source name of each `TemplateError`, so an error line reads `hud.panel(3,5): UnknownBinding: ...`.
- The factory ignores its `PanelContext`. The attached `TemplateBindings` gives the commands and the stores.

### The Rules of the Object

- **One `DefinitionLoader` for each `TemplateLoader`.** The constructor registers the type `SlotContainer`. A second `DefinitionLoader` on the same loader, or a game type with that name, makes `TemplateLoader.Register` throw `ArgumentException` in the constructor (Notes item 4).
- **One name holds one template, and one name holds one map.** A second `AddTemplate` or `AddActionMap` for one name throws `ArgumentException` before the add. `AddTemplate` copies the text, so a later change of the source changes nothing.
- **An add after a load is valid.** The next load reads the tables. An add call and a load call must not run at the same time, as for `TemplateLoader.Register`.
- **The checks of A6.** The load checks duplicates inside one text only. A name that the manager holds makes `UiManager.Register` throw `ArgumentException` in `RegisterTo`, as today. `RegisterTo` is not atomic: the records before the throw stay registered (Notes item 5).
- **A failed result.** `RegisterTo` on a result with an error throws `InvalidOperationException` with one `ToString` line for each error, separated by LF, and registers nothing. This is the form of `GetRootOrThrow`.
- **Load time and run time.** The load, the template copy, the check loads, and each factory call allocate at build time (epic A10). A loaded record holds the same data as a C# record, so the mode diff allocates nothing after warm-up.

### Notes (of the plan)

1. **A reading of A2 and Q5: a direct `PanelRef`.** The plan keeps the A2 shape and the A2 names, and adds one form: a `PanelRef` directly under `Mode` has no slot and takes the `DefaultSlot` of its panel.
2. **Conflict: A9 and the size of `ModeDiffAllocationTests.cs`.** The plan makes the class `partial` and adds `ModeDiffAllocationTests.Loaded.cs` (Decision 9).
3. **Open point: a mode that names a record of C# code.** The plan resolves each layout and panel of a mode against the same text (Decision 6). Options: (a) the same text only (recommended, the plan). (b) Accept an unknown name and leave the fault to `UiManager.Register`. (c) A list of known C# names before the load.
4. **Open point: one `DefinitionLoader` for each `TemplateLoader`.** A second `DefinitionLoader` on one loader throws `ArgumentException`. Options: (a) document and pin the throw (recommended, the plan). (b) Catch it (it hides a game type with the same name). (c) A public `TemplateLoader.IsRegistered` (changes Markup, against A8).
5. **Open point: `RegisterTo` is not atomic.** The records before the throw stay registered. Options: (a) document it (recommended, the plan). (b) A name query on `UiManager` in a later feature.
6. **A reading of Q3: an unused layout.** A bad template of a layout that no mode names gives no load error; `RegisterTo` throws from its factory. The plan follows Q3.
7. **Conflict: sections of `technical-design.md`.** Task 2.6 also edits the eighteenth-gate bullet in section 14.
8. **The whitespace of a value.** Each name value is used as is. Only an empty value gives `InvalidValue`.
9. **A Slot with no PanelRef.** An empty `Slot` adds no placement; the slot check still runs on its name.
10. **The planned totals** come from the binding-attributes close: 3167 with 3 skipped at `9fa4b43`. Task 1.1 records the real baseline above.

### The commands (of Implementation Phases)

Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. Each task group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. The source namespace is `BrutalUi.Framework`, and the test namespace is `BrutalUi.Framework.Tests`.

### The plants (of Implementation Phases)

For each plant, copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored), make the change, and run the solution tests with `dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Check the `error CS` count first. It must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect an exit code that is not 0. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time. Run a second time before you treat it as a fault.

---

## Important Decisions

### Decision 1: The confirmed interview items
**Date:** 2026-11-18
**Context:** Open points 7 and 8 of xml-element-loader left the template registry and the error type to this feature.
**Decision:** A template is the UTF-8 XML text, added by name with `AddTemplate` (Q1, A3). The errors use a new `DefinitionError` struct in Framework (Q4). The full list is in `implementation.md` under `### Confirmed Assumptions`.
**Rationale:** The coordinator chose each recommended option, and the stop check came back "all hold".
**Impact:** Markup stays unchanged. Each factory loads its template through the shared `TemplateLoader`.

### Decision 2: The approach
**Date:** 2026-11-18
**Context:** The loader must give exact positions, and a mode can name a layout or a panel that comes later in the text.
**Decision:** Two passes, in the style of `TemplateReader`: a read pass into an entry list with positions, then a resolve pass.
**Rationale:** The order of entries does not matter, and each error keeps its position.
**Impact:** The plan splits the reader into `DefinitionReader.cs` and `DefinitionReader.Resolve.cs`.

### Decision 3: The SlotContainer factory keeps a defensive error path
**Date:** 2026-11-18
**Context:** The `slot` row is `CreateRequired`, so the loader records `MissingAttribute` before it calls the factory. `args.TryGet("slot", ...)` therefore never gives `false` in a load. A null factory result with no new error, however, makes the loader throw `InvalidOperationException`.
**Decision:** If `TryGet` gives `false`, the factory calls `args.AddError(TemplateErrorKind.MissingAttribute, ...)` and returns `null`. The guard keeps the factory contract in every path.
**Rationale:** The path is unreachable in a load; the cost is three lines.
**Impact:** `SlotContainerTemplate.cs` only.

### Decision 4: The duplicate add guards follow in a later group
**Date:** 2026-11-18
**Context:** The Rules of the Object say a second `AddTemplate` or `AddActionMap` for one name throws `ArgumentException` before the add. Task 1.5 names only the `ArgumentNullException` checks for this group ("Each throws `ArgumentNullException` for a `null` argument in this phase").
**Decision:** Group 1 stores with the dictionary indexer and documents the end-state rule in the class remarks. Task 3.4 owns the guard: it edits `DefinitionLoader.cs` so `AddTemplate` and `AddActionMap` throw `ArgumentException` for a duplicate name.
**Rationale:** The brief pins the checks of this phase, and the plan gives task 3.4 as the owner.
**Impact:** See the record in Gotchas. `AddTemplate` and `AddActionMap` overwrite a name until task 3.4 runs. Task 3.4 ran on 2026-11-18 (phase 3 group 2): the guards now throw before the add.

### Decision 5: The read pass spans two partial files
**Date:** 2026-11-18
**Context:** Task 1.6 with the entry structs, `Load`, the walk, and the BRUTAL doc style reached 703 lines in one file. The rule for a new source file is 400 lines.
**Decision:** Keep `DefinitionReader.cs` for the state, `Load`, `AddError`, the sort, and the entry structs, and move the walk to `DefinitionReader.Elements.cs`. This follows the existing split of `TemplateReader.cs` and `TemplateReader.Elements.cs`. `DefinitionReader.Resolve.cs` stays one file at 228 lines.
**Rationale:** A split on the same seam as Markup keeps each file readable and adds no new kind of file. The plan already notes a partial split for `ModeDiffAllocationTests` (Notes item 2).
**Impact:** Three files instead of two for group 2. Tasks 2.1 and 3.1 edit `DefinitionReader.Elements.cs` for the walk, and `DefinitionReader.cs` for the entries and the value rules. See the record in Gotchas.

### Decision 6: Phase 1 reports the faults that a record constructor would hit
**Date:** 2026-11-18
**Context:** Task 1.6 names `UnknownElement`, `UnknownAttribute`, and `MissingAttribute` for this phase and leaves the other faults to phase 3. An empty `name` or `slot` value, however, reaches a record constructor, which throws `ArgumentException`, and `Load` must never throw for the text. One panel two times in one mode has the same shape: `ModeDefinition` throws.
**Decision:** The read pass reports an empty required value as `InvalidValue` with a short message, and the resolve pass skips an entry or a placement whose required value is absent or empty, so one fault gives one error. The resolve pass also keeps the first placement of one panel in one mode. Phase 3 replaces the message wording and adds `DuplicateName` at the later entry; nothing else changes.
**Rationale:** The public contract of `Load` ("the method never throws for the text") holds from phase 1, and no input gives a silent success with a missing record. Each check is three lines.
**Impact:** `DefinitionReader.Elements.cs` (check 3c) and `DefinitionReader.Resolve.cs` (the skip and the first-placement rule).

### Decision 7: The read split keeps the required names in their own table
**Date:** 2026-11-18
**Context:** Task 2.1 makes `actions` an optional attribute of `Mode`. Phase 1 reads one list per element kind, and check 4 reports `MissingAttribute` for every name in it, so the list cannot hold both a required name and an optional one.
**Decision:** `AllowedNames(kind)` holds the required names plus `actions` for `Mode`, and a new `RequiredNames(kind)` holds only the required ones. Check 3b and the unknown-attribute message read the allowed list; check 4 reads the required list. A mode with no `actions` gives no error, so the value stays absent and the resolve pass gives no map.
**Rationale:** Two small tables keep the order of the checks of `The Two Passes` and need no flag inside the loop. The Phase 3 messages of task 3.1 name both lists already: they list the attributes of the element, `actions` included.
**Impact:** `DefinitionReader.Elements.cs` only.

### Decision 8: The resolve pass keeps the panel records by entry index
**Date:** 2026-11-18
**Context:** Task 2.2 needs the `DefaultSlot` of the panel record for a `PanelRef` directly under `Mode`. The `panelNames` table gives the entry index of the first entry of one name, but a faulty entry (no `template`, an unknown template) is in that table and has no record, and `Load` must not throw for the text.
**Decision:** Step 2 fills `PanelDefinition?[] panelRecords` beside the `panels` list. Step 4 reads the record at the entry index of its name: with a record it builds the `ModePanel` from `panelRecord.DefaultSlot`; with no record it skips the placement, because the fault of that panel entry already has its error.
**Rationale:** One array of the entry count costs no search, keeps the follow-on shape of Decision 6, and gives one fault one error. Phase 3 replaces the skip with the follow-on rule for the same entries.
**Impact:** `DefinitionReader.Resolve.cs` only. The skip of a direct `PanelRef` on a faulty panel is the shape that task 3.2 keeps.

### Decision 11: DefinitionSet returns a nested Records type
**Date:** 2026-11-18
**Context:** Task 2.4 needs the three record lists of both paths in text order. `DefinitionLoadResult.Success` is internal, and `BrutalUi.Framework` keeps no `InternalsVisibleTo` for the test assembly (A10), so `RegisterCode` cannot wrap the C# records in a `DefinitionLoadResult`.
**Decision:** `DefinitionSet.Load` and `DefinitionSet.RegisterCode` each return the nested `internal sealed class DefinitionSet.Records`, which holds the three arrays. `Load` copies the lists of the result into it, and `RegisterCode` builds it from the records it registers.
**Rationale:** One small internal carrier gives both tests one loop shape, adds no public API, and keeps the rule "do not guess on a public API shape". The change needs no new production type.
**Impact:** `DefinitionSet.cs` and `DefinitionParityTests.cs` only.

### Decision 12: The loaded gate scene carries no inline size and no field focus
**Date:** 2026-11-18
**Context:** The C# `BuildScene` sets `Inline` sizes on the field, the add button, and the layout root, and gives the focus to the field. The template format has no style row, so a template cannot set an `Inline` member, and the task 2.5 brief lists no size and no focus step.
**Decision:** `BuildLoadedScene` builds the same trees with no size and no focus, and reuses the existing `ToggleStep` as the measured body. `TreeCompare` reads no size, and the step sum and the 0-byte rule do not depend on the focus. The loaded score label keeps no name: the test reads it by child index, state root child 1, so the two shapes stay equal.
**Rationale:** The overview of task 2.5 pins the build steps, and the parity rule of A9 needs the step body and the tree shape, not the size.
**Impact:** `ModeDiffAllocationTests.Loaded.cs` only.

### Decision 13: The slot check of phase 3 invalidates one phase 2 test text
**Date:** 2026-11-18
**Context:** Task 3.2 and the demo test `ASlotThatTheLayoutDoesNotHoldIsAnUnknownSlotError` require the `UnknownSlot` check on a direct `PanelRef` whose panel default slot the layout does not hold. The phase 2 text `TheSlotOrderAndThePanelOrderFollowTheText` has exactly that shape: panel `dr.p4` has `slot="center"`, and the shared `dr.layout` template held only the slots `top` and `bottom`. The brief asked for all existing tests to pass "with no edit", but no consistent rule can pass both texts, so one edit was unavoidable.
**Decision:** Keep the plan's rule, and add one `<ui:SlotContainer slot="center" />` line to the shared layout template literal of `DefinitionRecordTests.cs`. No test name, no assertion, and no count changed; the class still reads 5 with `failed: 0`.
**Rationale:** Task 3.2, the phase 3 demo, and plant 7 pin the slot check; the phase 2 text predates it. The one-line change keeps the checked template sound for every test that shares it.
**Impact:** `tests/BrutalUi.Framework.Tests/Templates/DefinitionRecordTests.cs` shows a 4-line diff (one template line, two doc lines rewrapped, one deleted) against `9fa4b43`. The protected `src/` paths stay untouched.

### Decision 14: The empty-value message names the fix of each attribute
**Date:** 2026-11-18
**Context:** Task 3.1 pins the `InvalidValue` message word for word only for `slot`: `'slot' needs a value. Write the name of a slot, for example 'top'.` An empty `name`, `template`, `layout`, or `actions` needs a message of the same shape, and "the name of a slot" fits none of them.
**Decision:** `EmptyValueMessage` keeps the pinned `slot` sentence, and gives the other names their own second sentence: `template` names a registered template, `layout` names a layout of this text, `actions` names a registered action map, and any other name gets `Write a name.`
**Rationale:** The first sentence, `'{name}' needs a value.`, stays the pinned shape for every attribute; only the unpinned fix sentence varies. No public API changes.
**Impact:** `DefinitionReader.Elements.cs` only; `DefinitionErrorTests` asserts the pinned `slot` message.

### Decision 15: The review takes the Notes item 6 option for an unused layout
**Date:** 2026-11-18
**Context:** Review CR-3 found that a bad template of a layout that no mode names passed the load and threw from the layout factory inside `RegisterTo`. Notes item 6 of `implementation.md` records the open point and offers the code fix or the doc weakening; the review recommends the fix.
**Decision:** The resolve pass check-loads each layout that is not faulty, also a layout that no mode names, one time per layout. A fault of an unused layout gives `TemplateFault` at the `Layout` element at load time, and the result holds no record.
**Rationale:** A result with no error then holds no factory that throws inside `RegisterTo`, and one load gives each fault. The extra load costs build time only (epic A10 permits load-time allocation).
**Impact:** Step 3b of `DefinitionReader.Resolve.cs`, the flipped Q3 case in the new partial file `DefinitionErrorTests.Layouts.cs`, and the **The check of each layout** paragraph in section 11 of `technical-design.md`. Notes item 6 of `implementation.md` stays as written; this decision replaces it, together with the two correction lines in the Plan Record.

---

## Gotchas & Learnings

- Read the Notes of `implementation.md` before phase 1. Item 4: a second `DefinitionLoader` on one `TemplateLoader` throws from `TemplateLoader.Register`. Item 5: `RegisterTo` is not atomic. Item 6: a bad layout that no mode names gives no error at load time.
- The reader lives in three partial files: `DefinitionReader.cs` (state, `Load`, `AddError`, `HoldsNotWellFormed`, `SortErrors`, and the entry structs), `DefinitionReader.Elements.cs` (the walk: `ReadElement`, `ReadAttributes`, the place and attribute tables, and the frame helpers), and `DefinitionReader.Resolve.cs` (the resolve pass). Tasks 2.1, 2.2, 3.1, and 3.2 name only `DefinitionReader.cs` and `DefinitionReader.Resolve.cs`. The walk edits of tasks 2.1 and 3.1 belong in `DefinitionReader.Elements.cs`; the `DefinitionReader.cs` edits of those tasks are the entries and the value rules.
- `Load` gives one `NotWellFormed` error and stops the resolve pass for it, so a broken text gives no follow-on name error. A text with no root gives `NotWellFormed` at 1,1.
- The resolve pass skips an entry whose required value is absent or empty, because the read pass already named that fault. Phase 3 added the follow-on rule on top: a reference to a skipped (faulty) entry gives no second error, a mode on a faulty layout skips the slot check, and a placement with no panel record skips the slot check.
- A mode with one panel two times: the resolve pass reports `DuplicateName` at the second `PanelRef` and keeps the first placement (task 3.2). `Load` still never throws for the text: the record constructor only sees the kept placements.
- An `actions` attribute and a `PanelRef` directly under `Mode` read as the real forms since task 2.1 and 2.2. A mode with no `actions` gets no map, and a placement with no slot value takes the `DefaultSlot` of its panel record (Q5). The Phase 1 demo text and the section 11 block of `docs/technical-design.md` hold neither form, so no existing test changed. Task 3.1 owns the wording of the unknown-attribute message, which now lists `name, layout, actions` for `Mode`.
- Check 3b and the unknown-attribute message read `AllowedNames(kind)`; check 4 reads `RequiredNames(kind)`. Keep the two tables in step when you add a name (Decision 7). A `Mode` table holds `actions` in the allowed list only.
- `actions=""` gives `InvalidValue` in the read pass and no map in the resolve pass, because `HasName` is false. A direct `PanelRef` whose panel entry has no record gives no placement and no second error (Decision 8, kept by the phase 3 follow-on rule). `PlacementEntry.UnderSlot` tells a `PanelRef` under a `Slot` with a faulty `name` (skip, no second error) from a direct `PanelRef` (default slot path).
- `DefinitionValue.HasName` is the single test that the resolve pass uses for a value. It is false for no value and for an empty value, so no record constructor sees an empty name and `Load` never throws for the text.
- The internal members that group 2 consumes: `DefinitionLoader.Templates`, `TryGetTemplate(string, out byte[]?)`, `TryGetActionMap(string, out UiActionMap?)`, `DefinitionLoadResult.Success`, and `DefinitionLoadResult.Failure`. The `out byte[]?` and `out UiActionMap?` parameters are nullable; do not read them after a false return.
- Group 2 of phase 2 keeps to the public loader API: the test project holds no `InternalsVisibleTo` for `BrutalUi.Framework`, so `DefinitionSet` uses `DefinitionLoadResult.Success` nowhere (Decision 11).
- The diff parity test needs two map instances: one map is active on one context at most, and `Activate` on the second manager's context would clear the slots of the first (the switch rule of `UiActionMap`). The data parity test needs one map instance: it asserts the `ActionMap` reference equality of the two records.
- `DefinitionSet.Load` makes one `TemplateLoader` and one `DefinitionLoader` in each call, so each manager gets a fresh `SlotContainer` registration and no second-loader throw (Notes item 4).
- A rule that a review changes lives in more than one doc. Decision 15 (check-load each non-faulty layout) landed in the **The check of each layout** paragraph of section 11, but the `TemplateFault` row of the kind table in the same section and the `CHANGELOG.md` feature line still read `used layout`. The docs step searched `used layout` under `docs/`, fixed both hits, and left the quality-bar sentence of `implementation.md` as written, because the plan stays as the interview fixed it and the correction lives here.

---

## Testing Approach

- Framework tests in `tests/BrutalUi.Framework.Tests/Templates/` and `Panels/`. The plan adds 26 tests and 9 plants. The cross-phase review adds 2 tests, so the real close is 28: `BrutalUi.Framework.Tests` 356 → 384, solution 3167 → 3195 with 3 skipped.
- `ModeDiffAllocationTests` gets one case on loaded records at 0 bytes after warm-up (A9).
- Each proof runs through the test binaries and the package test. The feature ships no screen, so no browser check applies.

---

## Needs Human Verification

- [ ] Phase 1 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionLoaderDemoTests'` and reads a total of 4 with `failed: 0`. In `OneLoadedPanelMountsInItsSlot`, a test loads one text with one panel, one layout, and one mode, registers them, sets the mode, runs one `Update`, and finds the loaded panel root, with its `score` label, as the one child of the slot `top`.
- [ ] Phase 2 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionParityTests'` and reads a total of 2 with `failed: 0`. After each of five mode steps, with a layout swap and a back step, the layout root of the loaded manager and the layout root of the C# manager compare equal under `TreeCompare`, with the same panels in the same slots.
- [ ] Phase 3 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionErrorTests'` and reads a total of 12 with `failed: 0` (10 from phase 3, 2 from the review fixes). A mode with `<ui:Slot name="left">` on a layout with the slots `top` and `bottom` gives one `UnknownSlot` error at the line and the column of `name`, and the result holds no record.
- [ ] Review fixes: the developer reads three cases in the same run. `AChildOutsideTheNamespaceUnderAFailedElementGivesOneError` gives one `UnknownElement`, not two (CR-2). `AFaultyEntryStillGivesItsOtherFaults` gives `UnknownLayout` and `UnknownPanel` in one load (CR-1). `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry` reports `TemplateFault` for a bad layout that no mode names, the Decision 15 flip (CR-3).

---

## Next Steps

### Immediate
1. A person runs the four lines of `## Needs Human Verification` above.
2. The coordinator merges `feat/markup/definition-loader` into the epic branch, copies the open human tests above to `docs/features/markup/human-testing.md` (the file does not exist yet), and runs `/update-epic markup` and `/update-master`. The docs step of the run is done, and this run made no git write.

### Future
1. `markup/markup-sample` (feature 6 of 6) loads the HelloPanel records through this API: one `TemplateLoader` with its `TemplateBindings` attached, one `DefinitionLoader`, each `AddTemplate` and `AddActionMap` call, one `Load`, and one `RegisterTo`. It then closes Phase 3 of the technical design.
2. A later version may add `WindowDefinition` to the format. The `Window` error message already tells an author that it comes later.

---

## Open Questions

- (none. The review closed each open point of the Notes of `implementation.md`. Decision 15 settles Notes item 6: the resolve pass check-loads each non-faulty layout, also one that no mode names.)

---

## Session Notes

### 2026-11-18 16:40
- The plan interview ran through the Orca coordinator. Each recommended option was chosen, and the assumptions hold.
- The solution-architect agent wrote `implementation.md`. `/start-feature` wrote `tasks.md`, this file, and `inbox.md`.
- The next session starts phase 1.

### 2026-11-18 17:56 (phase 1, group 1)
- Tasks 1.1 to 1.5 done. New files: `DefinitionError.cs`, `SlotContainerTemplate.cs`, `DefinitionLoadResult.cs`, `DefinitionLoader.cs`, and the `DefinitionReader.cs` placeholder. No shared file changed.
- Baseline measured at the group start: solution 3167 (0 failed, 3 skipped), Framework.Tests 356 (0 failed, 0 skipped). Base commit `9f8f14e`, planning reference `9fa4b43`. The recorded plan sections and the tool notes are above.
- End-of-group run: build `0 Warning(s)`, `0 Error(s)`. Solution tests 3167, 0 failed, 3 skipped, unchanged. `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` is empty.
- Blast radius: the `DefinitionLoader` constructor calls `TemplateLoader.Register` (documented throw). `RegisterTo` calls the three `UiManager.Register` overloads. The record constructors and `TemplateLoader.Load` run in group 2 through the reader. Nothing existing reads the new types yet, so the risk is confined to the new files.
- Migration risks and follow-up: the placeholder throws `NotImplementedException`; group 2 replaces it and adds the demo tests. Task 3.4 adds the duplicate-add guards. The `RegisterTo` throw stays non-atomic (Notes item 5).

### 2026-11-18 18:36 (phase 1, group 2)
- Tasks 1.6 to 1.8 done. New: `DefinitionReader.Elements.cs` and `DefinitionReader.Resolve.cs`; replaced: `DefinitionReader.cs`; new test file `DefinitionLoaderDemoTests.cs`; edited `technical-design.md` (section 10 bullet, section 11 **### Definition format**) and these docs.
- Counts, both plants, and the demo state are in the Group 2 Close Record above. Phase 1 acceptance holds, and no git write ran.
- Blast radius, the four integration points that group 2 reached: `TemplateLoader.Load` and `TemplateLoadResult.GetRootOrThrow` inside each record factory; `PanelDefinition`, `LayoutDefinition`, and `ModeDefinition` with `ModePanel`, built by the resolve pass; `UiManager.Register` for the three kinds through `RegisterTo`; `SlotContainer` built by the registered `SlotContainerTemplate` type. Nothing shared changed: the protected-path diff is empty, and no existing test needed an edit.
- Migration risks and follow-up: the reader is now three partial files (Decision 5), so tasks 2.1 and 3.1 must open `DefinitionReader.Elements.cs`. Phase 1 gives `UnknownAttribute` for `actions` and `UnknownElement` for a direct `PanelRef`, and no duplicate, slot, check-load, or follow-on rule yet; tasks 2.1, 2.2, 3.1, and 3.2 own those. `RegisterTo` stays non-atomic (Notes item 5). Task 3.4 still owns the duplicate-add guards.

### 2026-11-18 18:50 (phase 1 close, Orca worker)
- Phase 1 is done: group 1 (tasks 1.1 to 1.5, the public types) and group 2 (tasks 1.6 to 1.8, the reader, the demo, and the docs), both by a `game-dev-advanced` agent. The verifier passed on round 1 with Demo: PASS. No fix round ran. No user message reached the inbox; no inbox line is open.
- The Phase 1 demo line went to Needs Human Verification above. The coordinator copies it to `docs/features/markup/human-testing.md` after the merge into the epic branch, so this run wrote no line there.
- No git write ran in this phase. The verdict file is `.beast-mode/briefs/markup/definition-loader/orca-phase-1.verdict.md`.

### 2026-11-18 19:08 (phase 2, group 1)
- Tasks 2.1 to 2.3 done. Edits: `DefinitionReader.cs` (the `Actions` value of the mode entry, the `PlacementEntry.Slot` note, the scope remarks), `DefinitionReader.Elements.cs` (the `actions` read, the `AllowedNames` and `RequiredNames` split, the `PanelRef` under `Mode` in the place table and the placement read), `DefinitionReader.Resolve.cs` (the `panelRecords` array, the `TryGetActionMap` lookup with `UnknownActionMap` at the attribute, the map pass-through, the default-slot placement rule). New test file: `tests/BrutalUi.Framework.Tests/Templates/DefinitionRecordTests.cs` with 5 tests.
- Counts, both plants, and the demo state are in the Phase 2 Group 1 Close Record above. `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` stays empty: the group changed no shared file, and no existing test needed an edit.
- Blast radius: the group touched the reader only. The new resolve path calls `DefinitionLoader.TryGetActionMap`, `ModeDefinition(name, layout, panels, map)` with the map argument, and reads `PanelDefinition.DefaultSlot`. No record, manager, or Markup type changed.
- Migration risks and follow-up: group 2 (tasks 2.4 to 2.6) owns the parity test, `DefinitionSet.cs`, the loaded allocation case, plant 5, and the section 10, 11, and 14 doc edits. Expect Framework 368 and solution 3179 after it: group 2 adds 3 tests. Phase 3 still owns the duplicate check, the slot check, the check load of a used layout, the follow-on rule, and the message wording.
- No inbox line was open at the group start, and no user message reached this run. No git write ran: no commit, branch, merge, stash, reset, or push.

### 2026-11-18 19:37 (phase 2, group 2)
- Tasks 2.4 to 2.6 done. New files: `tests/BrutalUi.Framework.Tests/Shared/DefinitionSet.cs`, `tests/BrutalUi.Framework.Tests/Templates/DefinitionParityTests.cs` (2 tests), and `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.Loaded.cs` (1 test). Edits: the class line of `ModeDiffAllocationTests.cs` (`partial`, one changed line against `9fa4b43`), sections 10, 11, and 14 of `technical-design.md`, and these docs.
- Counts, plant 5, the demo state, and the protected paths are in the Phase 2 Group 2 Close Record above. Phase 2 acceptance holds: the loaded toggle gate measured 0 bytes on the first run, and the parity classes read 2 and 4 with `failed: 0`.
- Blast radius: tests and docs only. The group changed no `src/` file; the plant restore left `cmp`-equal files and the protected-path diff is empty. The new tests consume the public `DefinitionLoader`, `DefinitionLoadResult.RegisterTo`, `TemplateBindings`, `UiManager`, and the record types. `ModeDiffAllocationTests` became `partial`; no existing test needed an edit.
- Migration risks and follow-up: phase 3 owns the duplicate check, the slot check, the check load of a used layout, the follow-on rule, the message wording, the duplicate-add guards (Decision 4), and `DefinitionLoaderApiTests`. Expect Framework 382 and solution 3193 at the phase 3 close.
- No inbox line was open at the group start, and no user message reached this run. No git write ran: no commit, branch, merge, stash, reset, or push.

### 2026-11-18 19:45 (phase 2 close, Orca worker)
- Phase 2 is done: group 1 (tasks 2.1 to 2.3, the record forms) and group 2 (tasks 2.4 to 2.6, the parity, the loaded gate, and the docs), each by one `game-dev` agent at the STANDARD tier, one agent at a time. The verifier passed on round 1 with Demo: PASS; no fix round ran, and no `bug` line was open, so no fix brief was needed.
- The Phase 2 demo line went to Needs Human Verification above, and only there. This run wrote no line to `docs/features/markup/human-testing.md`: the coordinator copies the open human tests here after it merges the feature into the epic branch.
- `tasks.md`: Phase 2 6/6, progress 14/20, and the four Phase 2 acceptance boxes checked on the verifier evidence.
- No git write ran in this phase. The verdict file is `.beast-mode/briefs/markup/definition-loader/orca-phase-2.verdict.md`.

### 2026-11-18 20:20 (phase 3, group 1)
- Tasks 3.1 to 3.3 done. Edits: `DefinitionReader.cs` (the `UnexpectedText` walk case, the `PlacementEntry.UnderSlot` mark, the scope remarks), `DefinitionReader.Elements.cs` (the `Window` message, `EmptyValueMessage`, the `UnderSlot` fill), `DefinitionReader.Resolve.cs` (rewrite: duplicate check, fault marks, used-layout check load with the pre-order slot walk, slot check, completed messages, follow-on rule), new `DefinitionErrorTests.cs` (10 tests), and the one-line `DefinitionRecordTests.cs` template fix of Decision 13.
- Counts, the three plants, and the demo state are in the Phase 3 Group 1 Close Record above. Group 1 acceptance holds: 10 new tests, all 13 kinds asserted, plants 6 to 8 each exit non-zero with 0 `error CS` lines and `cmp`-equal restores, build `0 Warning(s)`, `0 Error(s)`, solution 3189 (0 failed, 3 skipped), Framework 378, protected-path diff empty.
- Blast radius: the three reader partial files and one test template. No `src/BrutalUi.Markup`, `src/BrutalUi.Core`, or `src/BrutalUi.Framework/Panels` change; no existing assertion changed. The new resolve path calls `TemplateLoader.Load` for each used layout and walks the built tree for `SlotContainer`.
- Open point for the verifier: Decision 13 records the one place where two acceptance lines of the brief contradicted (the slot check of task 3.2 versus "no edit" for `TheSlotOrderAndThePanelOrderFollowTheText`). The plan rule won; the test kept all its asserts.
- Group 2 (tasks 3.4 to 3.6) still owns the duplicate-add guards (Decision 4), `DefinitionLoaderApiTests` with 4 tests, plant 9, the section 11 docs, the changelog line, and the package run. Expect Framework 382 and solution 3193 at the phase close.
- No inbox line was open at the group start, and no user message reached this run. No git write ran: no commit, branch, merge, stash, reset, or push.

### 2026-11-18 20:32 (phase 3, group 2)
- Tasks 3.4 to 3.6 done. Edits: `DefinitionLoader.cs` (guard constants, empty-name and duplicate checks before the add, constructor remarks), `DefinitionLoadResult.cs` (`RegisterTo` remarks), new `DefinitionLoaderApiTests.cs` (4 tests), section 11 of `technical-design.md` (the error block, the kind table, the position rule, the follow-on rule, the Q3 and A5 check, and the rules of the object), and one `CHANGELOG.md` line.
- Counts, plant 9, the demo state, the package run, and the protected paths are in the Phase 3 Group 2 Close Record above. Phase 3 acceptance holds: 14 new phase 3 tests, build `0 Warning(s)`, `0 Error(s)`, solution 3193 (0 failed, 3 skipped), Framework 382, `package-test: PASS Gauge level 0.5`.
- Blast radius: `DefinitionLoader.cs` and `DefinitionLoadResult.cs` only in `src/`. The duplicate-add guard is a behavior change: a game that re-added one name now gets `ArgumentException`. No existing test or sample re-adds one name, so no existing test needed an edit. No `src/BrutalUi.Markup`, `src/BrutalUi.Core`, or `src/BrutalUi.Framework/Panels` change; the protected-path diff is empty.
- No inbox line was open at the group start, and no user message reached this run. No git write ran: no commit, branch, merge, stash, reset, or push. The phase now awaits the close and the verifier pass.

### 2026-11-18 20:44 (phase 3 close, Orca worker)
- Phase 3 is done: group 1 (tasks 3.1 to 3.3, the positioned errors and `DefinitionErrorTests`) and group 2 (tasks 3.4 to 3.6, the argument checks, `DefinitionLoaderApiTests`, the docs, and the package run), each by one `game-dev` agent at the STANDARD tier, one agent at a time. The verifier passed on round 1 with Demo: PASS; no fix round ran, and no `bug` line was open, so no fix brief was needed.
- The one accepted deviation, Decision 13: group 1 added one `<ui:SlotContainer slot="center" />` line to the shared layout template literal of `DefinitionRecordTests.cs`, because the task 3.2 slot check of a direct `PanelRef` rejects the phase 2 scene. No test name, assertion, or count changed; the verifier gate 7 checked the diff.
- The Phase 3 demo line went to Needs Human Verification above, and only there. This run wrote no line to `docs/features/markup/human-testing.md`: the coordinator copies the open human tests there after it merges the feature into the epic branch.
- `tasks.md`: Phase 3 6/6, progress 20/20, and the four Phase 3 acceptance boxes checked on the verifier evidence. The feature build is complete.
- No inbox line was open at the phase start, no `Inbox:` line came from any agent of this phase, and no user message reached this run. No git write ran in this phase. The verdict file is `.beast-mode/briefs/markup/definition-loader/orca-phase-3.verdict.md`.

### 2026-11-18 21:10 (review group 1)
- CR-2, CR-4, and CR-5 of the code review applied. The `CR-<n>` boxes in `code-review.md` stay open for the verifier.
- CR-2: the failed-parent check in `ReadElement` (`DefinitionReader.Elements.cs`) now runs before Check 1, the namespace check. A child outside the namespace under a failed element pushes a failed frame and gives no follow-on error. The repro `<ui:Definitions xmlns:ui="brutal-ui"><ui:Window><Panel /></ui:Window></ui:Definitions>` gives exactly one `UnknownElement`. One new test: `AChildOutsideTheNamespaceUnderAFailedElementGivesOneError` (the choice was a new test, not an extension), in the new partial file `tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.FollowOn.cs`. The split was necessary: `DefinitionErrorTests.cs` reads 398 lines, and any added case would cross the 400-line cap. The class declaration gained only the word `partial`; no existing assertion or count changed.
- CR-4: `PlaceMessage` builds the root fix sentence from `TemplateLoader.Namespace`; the text is otherwise identical, and no `brutal-ui` literal remains in the method.
- CR-5: `AllowedNames` and `RequiredNames` now return `private static readonly string[]` fields, and `Array.Empty<string>()` covers the kinds with no attributes, so no call allocates. The two tables stay in step: the allowed table of `Mode` adds `actions`; the other allowed tables equal their required tables (Decision 7). Check 3b builds `string.Join(", ", allowed)` only inside the unknown-attribute branch. `Load` reads the input span through an `UnmanagedMemoryStream` inside a `fixed` block, so the read pass copies no array. An empty span fixes to a null pointer, and the constructor rejects a null pointer, so `Load` skips the walk for an empty text; the empty text still gives `NotWellFormed` at 1,1 through the `_sawRoot` check.
- Split of the source: the static kind tables and messages moved from `DefinitionReader.Elements.cs` to a new partial file `DefinitionReader.Tables.cs` (`ResolveKind`, `PlaceMessage`, `EmptyValueMessage`, `AllowedNames`, `RequiredNames`, `NameOf`, and the five name arrays), because the fields block took Elements.cs past the 400-line cap. The seam follows the existing partial pattern.
- Docs kept true: the `ReadElement` summary, the class remarks of `DefinitionReader.cs` (the check order now names the failed parent first, and the split names `DefinitionReader.Tables.cs`), and the `Load` summary now note the in-place read. `Load` carries the `unsafe` modifier.
- Counts: build `0 Warning(s)`, `0 Error(s)`. Solution 3194, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 383. `*DefinitionErrorTests` 11, 0 failed. File sizes: `DefinitionReader.cs` 397, `DefinitionReader.Elements.cs` 268, `DefinitionReader.Tables.cs` 157, `DefinitionErrorTests.cs` 398, `DefinitionErrorTests.FollowOn.cs` 31. Each file stays under 400. The protected-path diff against `9fa4b43` is empty. No git write ran: no commit, branch, merge, stash, reset, or push.
- Open questions: none.

### 2026-11-18 21:25 (review group 2)
- CR-1 and CR-3 of the code review applied. The `CR-<n>` boxes in `code-review.md` stay open for the verifier.
- CR-1: the resolve pass steps 2, 3, and 4 mark a faulty entry instead of skipping it. The placement loop of a mode now runs also under an `UnknownLayout`, an `UnknownActionMap`, an absent `name`, an absent `layout`, or a duplicate mode name, so a faulty mode gives its `UnknownPanel` and its duplicate-panel faults in the same load. Steps 2 and 3 look up a present `template` also when another value of the entry has a fault, so a `Panel` with no `slot` and an unknown `template` gives both errors. The record lands only when the entry is sound; the follow-on rule (no second error on a reference to a faulty entry, no slot check on a faulty layout) keeps its shape. The repro `<ui:Mode name="m" layout="gam"><ui:PanelRef name="hdu" /></ui:Mode>` gives two errors. New test: `AFaultyEntryStillGivesItsOtherFaults` in `DefinitionErrorTests.FollowOn.cs`.
- CR-3 (Decision 15): step 3b now check-loads every layout that is not faulty, also one that no mode names, one time per layout; the used-layouts table is gone. A bad template of an unused layout gives `TemplateFault` at the `Layout` element at load time, so a successful result holds no factory that throws inside `RegisterTo`. The flipped case is `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry` in the new partial file `DefinitionErrorTests.Layouts.cs`.
- Splits: the six helper methods (`CheckLayout`, `CollectSlots`, `ReportDuplicates`, `BuildNames`, `CreateFactory`, `IsFirstName`) moved to the new partial file `DefinitionReader.Resolve.Checks.cs`, because the reworked pass grew toward the 400-line cap. The Q3 test moved to `DefinitionErrorTests.Layouts.cs`, because `DefinitionErrorTests.cs` read 398 and the flipped idle assertions crossed the cap; no assertion of the moved test changed except the named Q3 flip.
- Docs: section 11, **The check of each layout** paragraph, states the new rule; Decision 15 and the two Plan Record correction lines are above. `implementation.md` and the interview text stayed unchanged.
- Counts: build `0 Warning(s)`, `0 Error(s)`. Solution 3195, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` 384. `*DefinitionErrorTests` 12, 0 failed. File sizes: `DefinitionReader.Resolve.cs` 274, `DefinitionReader.Resolve.Checks.cs` 145, `DefinitionErrorTests.cs` 352, `DefinitionErrorTests.FollowOn.cs` 63, `DefinitionErrorTests.Layouts.cs` 64. Each file stays under 400. The protected-path diff against `9fa4b43` is empty. No git write ran: no commit, branch, merge, stash, reset, or push.
- Open questions: none.

### 2026-11-18 21:30 (docs step, `/update-feature`, Orca worker)
- `/update-feature markup/definition-loader` ran on the closed build: 20/20 tasks, three phases each verified, the cross-phase review (5 findings, all 5 fixed), and the review-fix verifier PASS. The command changed docs only: `tasks.md`, this file, the header of `implementation.md` (`Ready for Review`, the target date, the timestamp), the `TemplateFault` row of section 11 of `docs/technical-design.md`, and the feature line of `CHANGELOG.md`.
- The last two fixes carry Decision 15 into every doc: the pass check-loads each layout that is not faulty, so neither the kind table nor the changelog line says `used layout` any more. The quality-bar sentence of `implementation.md` stays as the interview fixed it; the correction lives in this file. `git status --porcelain` shows the five docs files and no source file.
- Epic context loaded per section 4 of `.beast-mode/epic-resolution.md`: `epic-overview.md` plus builder-api, xml-element-loader, element-generator, and binding-attributes. This feature settles open points 7 (the template registry) and 8 (the error type) of xml-element-loader. `markup/markup-sample` (feature 6 of 6) is the next feature and the consumer of this API. The epic rollup still reads 4/6 features complete, because `/update-epic markup` belongs to the coordinator.
- Inbox: `inbox.md` holds no line, and no user message reached this run (`orca orchestration check`: no messages). Every task of `tasks.md` is done, so the command promoted no `bug` line to `docs/bugs/` (the folder does not exist yet) and folds no done item into this note. No `note` or `change` line stays open.
- `## Needs Human Verification` holds the three demo lines and the new review-fix line, with the phase 3 count corrected to 12. The coordinator copies them to `docs/features/markup/human-testing.md`, which does not exist yet.
- Ship state: branch `feat/markup/definition-loader`, `HEAD` `fa7dcf3` (the review commit), with these five docs files changed and uncommitted. This run made no git write: no commit, branch, merge, stash, reset, or push.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
