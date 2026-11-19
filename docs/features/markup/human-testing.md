# markup — Human Testing

**Epic:** markup
**Last Updated:** 2026-11-18 21:31

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## definition-loader

- [ ] Phase 1 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionLoaderDemoTests'` and reads a total of 4 with `failed: 0`. In `OneLoadedPanelMountsInItsSlot`, a test loads one text with one panel, one layout, and one mode, registers them, sets the mode, runs one `Update`, and finds the loaded panel root, with its `score` label, as the one child of the slot `top`.
- [ ] Phase 2 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionParityTests'` and reads a total of 2 with `failed: 0`. After each of five mode steps, with a layout swap and a back step, the layout root of the loaded manager and the layout root of the C# manager compare equal under `TreeCompare`, with the same panels in the same slots.
- [ ] Phase 3 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionErrorTests'` and reads a total of 12 with `failed: 0` (10 from phase 3, 2 from the review fixes). A mode with `<ui:Slot name="left">` on a layout with the slots `top` and `bottom` gives one `UnknownSlot` error at the line and the column of `name`, and the result holds no record.
- [ ] Review fixes: the developer reads three cases in the same run. `AChildOutsideTheNamespaceUnderAFailedElementGivesOneError` gives one `UnknownElement`, not two (CR-2). `AFaultyEntryStillGivesItsOtherFaults` gives `UnknownLayout` and `UnknownPanel` in one load (CR-1). `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry` reports `TemplateFault` for a bad layout that no mode names, the Decision 15 flip (CR-3).

## markup-sample

- [ ] Phase 1 demo: the developer runs `HelloPanel --frames 120` on a host with a window and reads an eighth line `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.` Line 6 still shows `adds`, `resets`, and `versions` above 0, and the exit code is 0.
- [ ] Phase 2 demo: the developer changes `bind-text="hello.score"` to `bind-text="hello.scor"` in `samples/HelloPanel/bin/Debug/net10.0/HelloState.xml`, runs `HelloPanel --frames 120`, and reads one line that starts with `HelloState.xml(` and holds `): UnknownBinding: 'hello.scor' is not a bound key.` on standard error, no GLFW line, and exit code 1.
- [ ] Phase 2 windowed half of check 4: on a host with a window, the clean run gives 8 lines and exit code 0.
- [ ] Phase 3 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*SampleMarkupAllocationTests'` and reads a total of 3 with `failed: 0`. `AnAddClickOnTheLoadedStatePanelAllocatesNothing` measures 0 bytes over 100 click frames on the panel that `HelloState.xml` builds.
- [ ] Phase 4 demo: the reader opens `README.md` at `### The same tree in XML` and sees the XML block of `samples/HelloPanel/HelloState.xml`, equal to the file character for character, with the C# load code under it.
- [ ] markup-sample (A10): the XML state panel shows in `hello.main` with the `Add`, `Rebind (Up)`, and `Restore` buttons and the score label.
- [ ] markup-sample (A10): a click on `Add` raises the score.
- [ ] markup-sample (A10): F1 and the `Mode (F1)` button toggle `hello.main` and `hello.detail`, and F2 steps back.
- [ ] markup-sample (A10): the plant of task 1.9 — remove `bind-text="hello.score"` from the output copy of `HelloState.xml` under `bin/Debug/net10.0/`, run `HelloPanel --frames 120`, and read line 8 with `bindings 0`, the line `HelloPanel: the markup panel did not load.` on standard error, and exit code 1. Restore the file and check with `cmp`.
- [ ] markup-sample (A10): line 2 keeps the draw-command and vertex counts of the last measure: the XML state panel builds the same tree as the old C# row.
- [ ] markup-sample (A10): `scripts/test-linux.sh` and `scripts/test-windows.ps1` print their PASS line.
- [ ] Review fix (CR-1, CR-6, window host): the clean run registers before `Glfw.Init`, opens the window, and prints 8 lines with exit code 0. The sample calls `Register` and `Mount` one time each, so the one-time guards show only through the plant of the group 1 close record; a person who adds a second call in the flow reads `Mount runs one time. A second call builds each panel root again.` or `Register runs one time. A second call registers each record twice.`
