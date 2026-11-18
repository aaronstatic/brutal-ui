# markup — Human Testing

**Epic:** markup
**Last Updated:** 2026-11-18 21:31

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## definition-loader

- [ ] Phase 1 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionLoaderDemoTests'` and reads a total of 4 with `failed: 0`. In `OneLoadedPanelMountsInItsSlot`, a test loads one text with one panel, one layout, and one mode, registers them, sets the mode, runs one `Update`, and finds the loaded panel root, with its `score` label, as the one child of the slot `top`.
- [ ] Phase 2 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionParityTests'` and reads a total of 2 with `failed: 0`. After each of five mode steps, with a layout swap and a back step, the layout root of the loaded manager and the layout root of the C# manager compare equal under `TreeCompare`, with the same panels in the same slots.
- [ ] Phase 3 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionErrorTests'` and reads a total of 12 with `failed: 0` (10 from phase 3, 2 from the review fixes). A mode with `<ui:Slot name="left">` on a layout with the slots `top` and `bottom` gives one `UnknownSlot` error at the line and the column of `name`, and the result holds no record.
- [ ] Review fixes: the developer reads three cases in the same run. `AChildOutsideTheNamespaceUnderAFailedElementGivesOneError` gives one `UnknownElement`, not two (CR-2). `AFaultyEntryStillGivesItsOtherFaults` gives `UnknownLayout` and `UnknownPanel` in one load (CR-1). `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry` reports `TemplateFault` for a bad layout that no mode names, the Decision 15 flip (CR-3).
