# State and Commands — Human Testing

**Epic:** state-and-commands
**Last Updated:** 2026-10-29 06:23

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## store-and-subscription

- [ ] Phase 1 demo: a person runs `scripts/test-linux.sh` and reads its PASS line. Then the person runs `grep -n 'BrutalUi.Framework.Tests' artifacts/test-linux/test.log` and reads the result of the Framework test assembly with 0 failed (Q4).
- [ ] Phase 2 demo: a unit test creates a `Store<GameState>` and a subscription with the static selector `static s => s.Score`. The first `TryGetChanged` gives `true` and the start score, the second gives `false`, and a `Set` of a new score gives `true` and the new score one time. The developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*StoreSubscriptionTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*StoreAllocationTests'` reads a total of 4 with `failed: 0`.
