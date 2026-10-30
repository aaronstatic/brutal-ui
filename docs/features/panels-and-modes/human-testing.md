# Panels and Modes — Human Testing

**Epic:** panels-and-modes
**Last Updated:** 2026-10-30 05:50

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## panel-definition

- [ ] Phase 1 demo: a developer runs `scripts/test-linux.sh` and reads its PASS line, and the new Panels tests pass: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*PanelRecordTests'` reads a total of 14 and `--filter-class '*PanelControllerTests'` reads a total of 10, each with `failed: 0`.

## slot-container

- [ ] Phase 1 demo: a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*SlotContainerTests'` after a build and reads a total of 10 with `failed: 0`.
- [ ] Phase 2 demo: a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built binary reads a total of 8 for `--filter-class '*SlotLookupTests'` and 5 for `--filter-class '*SlotContainerTreeTests'`, each with `failed: 0`.
