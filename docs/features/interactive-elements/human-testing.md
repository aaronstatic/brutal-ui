# Interactive Elements — Human Testing

**Epic:** interactive-elements
**Last Updated:** 2026-10-26

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## button-and-toggle

- [ ] Phase 1 demo: a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `ButtonClickTests.APressAndAReleaseClickTheButtonOnce` pushes a press and a release over a `Button` through `UiContext.PushEvent`, runs `Update`, and counts one `Clicked` call with the button as the sender.
- [ ] Phase 2 demo: a unit test calls `SetFocus(button)`, pushes an Enter key down with no target, and runs `Update`. It counts one `Clicked` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonKeyTests'` and reads a total of 9 with `failed: 0`.
- [ ] Phase 3 demo: a unit test pushes a press and a release over a `Toggle` and runs `Update`. It reads `Checked` as `true`, `PseudoStates.Checked` in `States`, the fill of a `Toggle:checked` rule, and one `CheckedChanged` call that reads the new value. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ToggleTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 4 demo: a unit test creates a `Button` with `Text = "Play"` and a padding of 8, runs `Update` and `Draw` with a `RecordingRenderer`, and reads one `Text` call with the bytes of `Play` at the top-left corner of the content box. The button size is the measured text size plus the padding. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonTextTests'` and reads a total of 9 with `failed: 0`.
- [ ] Phase 5 demo: a unit test sets `Disabled` on the `Button`, pushes a press and a release over it, and runs `Update`. It counts 0 `Clicked` calls, and the root bubble handler gets the release. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetDisabledTests'` and reads a total of 7 with `failed: 0`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetAllocationTests'` and reads a total of 4 with `failed: 0`. A planted allocation at the start of `Button.HandlePointerUp` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
- [ ] Decide the chord rule of CR-1 (open scope question 1 of the button-and-toggle report).
- [ ] `TransitionCurveTests.TheCurvesAllocateNothing` failed one time in a full run (8056 bytes) and passed in each later run. This gate is outside this feature.
