# Dev Ops — Human Testing

**Epic:** dev-ops
**Last Updated:** 2026-10-16 10:20

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## solution-skeleton

- [ ] Phase 1 demo: the user runs `dotnet test --solution brutal-ui.sln` at the repository root and sees 3 passed tests and exit code 0.
- [ ] Phase 2 demo: the user runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120`, sees a window with the text `Hello, brutal-ui`, and the process exits with code 0.
- [ ] Phase 3 demo: the user runs `dotnet pack brutal-ui.sln -c Release` and sees exactly five `.nupkg` files in `artifacts/packages/`.
- [ ] Phase 2 Linux: make `libvulkan.so` available first, with `libvulkan-dev` or a link on `LD_LIBRARY_PATH`. Without it, the sample exits 1 with `DllNotFoundException` for `vulkan`.
- [ ] Phase 2 Windows: do the Phase 2 demo run on Windows. Nobody ran the sample on Windows yet.
- [ ] CR-1 Windows: run `HelloPanel` with no argument. Minimize the window, restore it, and then minimize it again. Close it from the taskbar. Expect exit code 0.
