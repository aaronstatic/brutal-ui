# Panels and Modes — Human Testing

**Epic:** panels-and-modes
**Last Updated:** 2026-10-30 05:10

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## panel-definition

- [ ] Phase 1 demo: a developer runs `scripts/test-linux.sh` and reads its PASS line, and the new Panels tests pass: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*PanelRecordTests'` reads a total of 14 and `--filter-class '*PanelControllerTests'` reads a total of 10, each with `failed: 0`.
