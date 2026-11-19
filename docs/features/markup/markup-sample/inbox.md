# markup/markup-sample - Inbox

> The loop adds one line for each item that reaches a run at any point: a bug, a change, or a note. It acts on the open lines after each phase. A done line stays. Section 12 of `.beast-mode/phase-handoff.md` holds the rules and the line shape.

## Items
- [x] bug 2026-11-19 10:24 | Verifier FAIL: tests/BrutalUi.Framework.Tests/Templates/SampleMarkupAllocationTests.cs is 472 lines; the 400-line cap for a new source file fails (gate step 10 of phase-3-verify). All other gate steps and the demo passed. | done: phase-3-fix1.md
- [x] note 2026-11-19 11:12 | Message that reached the phase 4 verifier tab: "1) Did you verify that the README csharp block matches HelloMarkup.Load's A4 flow (the abridged form task 4.1 intends)? 2) Did you check the UnknownTemplate message quote verbatim, character for character, against DefinitionReader.Resolve.cs and context.md? Noted: the coordinator decides README line 7 stays as is — it belongs to /update-feature. That does not change your verdict; reply only if your checks leave either question open." | done: no brief follows (phase 4 is the last phase). The two questions got their answers in the verifier result (the flow matches in the abridged form of task 4.1; the message quote is verbatim equal). The guidance restates the recorded decision in the phase 4 close record, so it wrote no plan change.
