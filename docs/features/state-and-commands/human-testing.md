# State and Commands — Human Testing

**Epic:** state-and-commands
**Last Updated:** 2026-10-29 06:23

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
