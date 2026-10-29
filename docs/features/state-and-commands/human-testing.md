# State and Commands — Human Testing

**Epic:** state-and-commands
**Last Updated:** 2026-10-29 15:20

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## store-and-subscription

- [ ] Phase 1 demo: a person runs `scripts/test-linux.sh` and reads its PASS line. Then the person runs `grep -n 'BrutalUi.Framework.Tests' artifacts/test-linux/test.log` and reads the result of the Framework test assembly with 0 failed (Q4).
- [ ] Phase 2 demo: a unit test creates a `Store<GameState>` and a subscription with the static selector `static s => s.Score`. The first `TryGetChanged` gives `true` and the start score, the second gives `false`, and a `Set` of a new score gives `true` and the new score one time. The developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*StoreSubscriptionTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*StoreAllocationTests'` reads a total of 4 with `failed: 0`.

## element-bindings

- [ ] Phase 1 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*LabelBindingTests'` and reads a total of 6 with `failed: 0`. The first test binds a label to a store score of 42 and reads the text `42` after one `Update`.
- [ ] Phase 2 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*WidgetBindingTests'` and reads a total of 10 with `failed: 0`. The test `AUserFlipWritesTheStoreOneTimeWithNoLoop` shows one delegate call and one new store version for one click.
- [ ] Phase 3 demo: a developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*BindingLifecycleTests'` and reads a total of 8 with `failed: 0`. The same command on the Framework test project with `'*LabelBindingTests'` reads a total of 12 with `failed: 0`.
- [ ] Phase 4 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*BoundTreeAllocationTests'` and reads a total of 4 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
- [ ] Windows run: run `scripts/test-windows.ps1` on a Windows machine and read its PASS line. No verifier had a Windows machine.

## commands-and-events

- [ ] Phase 1 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 5 with `failed: 0`. The first test clicks a button bound to `save` and reads one call of the save handler with its target.
- [ ] Phase 2 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandBindingTests'` and reads a total of 10 with `failed: 0`. The test `AClickOnAnUnknownNameCallsTheUnknownCommandSlot` reads the name `load` in the slot and no handler call.
- [ ] Phase 3 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*EventBusTests'` and reads a total of 10 with `failed: 0`. The first test publishes a `ScoreChanged` event to three subscribers and reads the score in each target, in the order of the subscribe.
- [ ] Phase 4 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ServiceRegistryTests'` and reads a total of 7 with `failed: 0`. The test `AMissingServiceGivesNullAndThrowsNothing` reads `null` for a type that no one registered.
- [ ] Phase 5 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*CommandAndEventAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
- [ ] Windows run: run `scripts/test-windows.ps1` on a Windows machine and read its PASS line. It checks the sixteenth gate on the Windows JIT. No verifier had a Windows machine.

## action-map

- [ ] Phase 1 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 8 with `failed: 0`. The first test pushes an Escape key down with no focus owner, runs `Update`, and reads one call of the command of `Cancel`.
- [ ] Phase 2 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*FocusActionTests'` and reads a total of 5 with `failed: 0`. The first test binds Down (264) to `NavigateDown`, presses it one time with the focus on the first of three buttons, and reads the focus on the second button.
- [ ] Phase 3 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*GamepadActionTests'` and reads a total of 7 with `failed: 0`. The first test pushes `UiGamepadButtons.B` two times and reads one call of the command of `Cancel` and the return values 1 and 0.
- [ ] Phase 4 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 15 with `failed: 0`. The test `AMouseButtonOverNoElementInvokesItsAction` presses the right button outside the panel and reads one call of the command of `Cancel`.
- [ ] Phase 5 demo: a developer runs `xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"` and reads line 6, `HelloPanel: state: adds <A>, resets <R>, versions <V>.`, with three counts above 0 and `exit=0`.
- [ ] Phase 6 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*UiActionMapTests'` and reads a total of 23 with `failed: 0`. The test `TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus` presses W (87) with the field focused and reads no call of the bound action and the char in the field.
- [ ] Phase 7 demo: a developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*ActionMapAllocationTests'` and reads a total of 5 with `failed: 0`. Then the developer runs `scripts/test-linux.sh` and reads its PASS line.
- [ ] HelloPanel by hand: run `dotnet run --project samples/HelloPanel/HelloPanel.csproj` with no argument. Press Escape: the score label shows 0. Click `Add`: the score rises. Press Up and Down: the focus moves. Type a letter into the text field: no action fires.
- [ ] Real gamepad: connect a gamepad and run HelloPanel. B resets the score, and the D-pad moves the focus. No verifier had a gamepad.
- [ ] Windows run: run `scripts/test-windows.ps1` on a Windows machine and read its PASS line. It checks the seventeenth gate on the Windows JIT. No verifier had a Windows machine.
