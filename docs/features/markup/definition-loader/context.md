# markup/definition-loader - Context & Decisions

**Epic:** markup (feature 5 of 6)
**Last Updated:** 2026-11-18 16:40
**Current Phase:** Phase 1 - One Panel, One Layout, One Mode
**Status:** 🟡 In Progress

---

## Quick Status

**What's Done:**
- ✅ The plan interview: Q1 to Q7 and A1 to A9 are confirmed. `implementation.md` holds them under `### Confirmed Assumptions`.
- ✅ The plan: 3 slices, 20 tasks, about 190 steps.

**What's Next:**
- 📋 Task 1.1: record the base commit, the solution total, and the Framework test total in this file. Then write the public types of phase 1 group 1.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Templates/DefinitionLoader.cs` - The public loader. It holds the template table and the action map table, and it registers the `SlotContainer` type on the `TemplateLoader`.
- `src/BrutalUi.Framework/Templates/DefinitionReader.cs` - The read pass. It reads each entry with its line and column.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs` - The resolve pass. It checks the names and the slots, and it builds the three records.
- `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs` - The records or the errors, and `RegisterTo(UiManager)`.
- `src/BrutalUi.Framework/Templates/DefinitionError.cs` - `DefinitionError` and `DefinitionErrorKind` (13 kinds).
- `src/BrutalUi.Framework/Templates/SlotContainerTemplate.cs` - The `SlotContainer` element type with the required `slot` attribute.

### Related Files
- `src/BrutalUi.Framework/Panels/PanelDefinition.cs`, `LayoutDefinition.cs`, `ModeDefinition.cs`, `UiManager.cs` - The records and the register calls. This feature does not change them.
- `src/BrutalUi.Markup/Template/TemplateLoader.cs`, `TemplateReader.cs` - The template loader and the read pattern. This feature does not change Markup (A8).
- `src/BrutalUi.Framework/Templates/TemplateBindings.cs` - The bindings that the game attaches to the `TemplateLoader`.
- `docs/features/markup/definition-loader/implementation.md` - The plan.

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

---

## Gotchas & Learnings

- Read the Notes of `implementation.md` before phase 1. Item 4: a second `DefinitionLoader` on one `TemplateLoader` throws from `TemplateLoader.Register`. Item 5: `RegisterTo` is not atomic. Item 6: a bad layout that no mode names gives no error at load time.

---

## Testing Approach

- Framework tests in `tests/BrutalUi.Framework.Tests/Templates/` and `Panels/`. The plan adds 26 tests and 9 plants. The planned solution total is 3193 with 3 skipped, from 3167 at `9fa4b43`.
- `ModeDiffAllocationTests` gets one case on loaded records at 0 bytes after warm-up (A9).

---

## Needs Human Verification

- [ ] (none yet)

---

## Next Steps

### Immediate
1. Run `/proceed` for phase 1 with an ADVANCED (opus) agent.

### Future
1. markup-sample loads the HelloPanel records through this API.

---

## Open Questions

- (none. The Notes of `implementation.md` hold the open points for review.)

---

## Session Notes

### 2026-11-18 16:40
- The plan interview ran through the Orca coordinator. Each recommended option was chosen, and the assumptions hold.
- The solution-architect agent wrote `implementation.md`. `/start-feature` wrote `tasks.md`, this file, and `inbox.md`.
- The next session starts phase 1.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
