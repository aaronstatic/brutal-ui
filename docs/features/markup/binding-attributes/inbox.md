# markup/binding-attributes - Inbox

> The loop adds one line for each item that reaches a run at any point: a bug, a change, or a note. It acts on the open lines after each phase. A done line stays. Section 12 of `.beast-mode/phase-handoff.md` holds the rules and the line shape.

## Items

- [x] bug 2026-11-17 06:16 | Orchestrator fault: four duplicate Phase-3 group-2 agents ran on the same tree at once (a dispatch error during a garbled orchestrator turn). One left `src/BrutalUi.Framework/Templates/TemplateBindings.cs` in the plant-8 state (md5 `013fe903…`, 391 lines, the `AttachTo` loader check removed). All agents stopped. The clean file is md5 `514499b699b2313b3c0b793b12fb528c`, 398 lines, held by `obj/plantscratch/p8-TemplateBindings.orig.cs`. | done: restored by the delegator from `p8-TemplateBindings.orig.cs` (`cmp` clean); post-restore Phase-3 gate re-run by one verifier agent — PASS on all 14 steps, Demo PASS, and the verifier confirmed all three `p8*` backups hold the clean hash
- [x] bug 2026-11-17 05:12 | Verifier FAIL: Gate 12 — `context.md` records only plant 3; plants 4 and 5 have no session note with the exit code, the 0 `error CS` count, the failed test names, and the `cmp`-clean restore. | done: phase-2-fix1.md (re-verify PASS, log values cross-checked against `artifacts/logs/p2-plant4.log` and `p2-plant5.log`)
