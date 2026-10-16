# dev-ops/solution-skeleton - Context & Decisions

**Last Updated:** 2026-10-16 10:53
**Current Phase:** Complete (all three phases and the cross-phase review done)
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Implementation plan (`implementation.md`)
- ✅ Dev docs scaffolded (`context.md`, `tasks.md`, `inbox.md`)
- ✅ Phase 1: the solution, the props, the pins, ten projects, and three passing placeholder tests (verifier PASS, Demo PASS)
- ✅ Phase 2: the `HelloPanel` sample, 11 projects. It renders 120 frames under Xvfb on the NVIDIA GPU and exits 0 (with the `libvulkan.so` workaround)
- ✅ Phase 3: `LICENSE`, the pack defaults, `src/Directory.Build.props`, five descriptions. `dotnet pack` writes exactly five `0.1.0-alpha` packages (verifier PASS, Demo PASS)
- ✅ Cross-phase review: 5 findings. CR-1, CR-2, CR-4, and CR-5 fixed. CR-3 declined with a reason. Verifier PASS.

**What's Next:**
- 📋 A person tests the items of `## Needs Human Verification` (the Windows run and the minimize path)
- 📋 `dev-ops/linux-dev-environment` supplies `libvulkan.so` and wraps the smoke run in a timeout

**Blockers:**
- None for this feature. `dev-ops/linux-dev-environment` must supply `libvulkan.so` on Linux (see Native Dependencies).

---

## Key Files

### Core Implementation
- `brutal-ui.sln` - The solution with every project (classic `.sln` format)
- `Directory.Build.props` - Shared build settings and the pack defaults
- `Directory.Packages.props` - Central package pins
- `global.json` - SDK band and the test runner
- `nuget.config` - One package source, nuget.org
- `samples/HelloPanel/` - The GPU smoke sample (phase 2)

### Related Files
- `docs/features/dev-ops/solution-skeleton/implementation.md` - Implementation plan
- `docs/features/dev-ops/epic-overview.md` - Epic overview and build order

---

## Important Decisions

The plan holds decisions 1 to 10 and the confirmed assumptions Q1 to Q4 and A1 to A8. This section records the decisions made during the build.

- Group 1A, tasks 1.1 to 1.6 done. `dotnet --version` prints 10.0.112. `BrutalUi.Core` builds with 0 warnings and 0 errors. The transitive packages of Core hold no `Ahwoo.BRUTAL.ImGui*` id. All `Ahwoo.BRUTAL.*` packages resolve at `2026.10.x`.
- The first `dotnet` run on this machine printed the first-run welcome text and created an HTTPS development certificate in the user profile. It changed nothing in the repository.
- `Microsoft.Extensions.ObjectPool` resolves at `11.0.0-preview.5.26302.115` as a transitive package of `Ahwoo.BRUTAL.*`. The plan does not pin it (Decision 3). No warning appears. Watch it in later groups.
- Group 1B, tasks 1.7 to 1.9 done. The four library projects and `samples/WorldSpace` build with 0 warnings and 0 errors. `WorldSpace` prints one line and exits 0. `Program` and `Main` are `internal` and `private`, because the sample is an executable and CA1515-style rules prefer that. No `.csproj` holds `TargetFramework`, `LangVersion`, `Nullable`, `AllowUnsafeBlocks`, or a `Version` attribute.
- Group 1C (brief g4), tasks 1.11 to 1.13 done. `BrutalUi.Benchmarks` builds with 0 warnings. `BenchmarkDotNet` 0.15.8 raised no analyzer warning, so no `NoWarn` exists. `Program` is an `internal static class` with a `private static int Main`. `brutal-ui.sln` uses the classic format (first non-empty line is `Microsoft Visual Studio Solution File, Format Version 12.00`) and lists 10 projects.
- `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` exits 0 with 3 passed, 0 failed. It runs only the three test projects. The `IsTestProject` fallback was not needed, so the non-test projects keep the default. `dotnet run --project samples/WorldSpace/WorldSpace.csproj` prints one line and exits 0. `git status --porcelain` shows no `bin/` or `obj/` path.
- Phase 2, task 2.3: `HelloPanel.csproj` references `Ahwoo.BRUTAL.Vulkan.Abstractions` directly, because `VulkanBackend.CreateInfo.Device` is a `DeviceEx` from that package. All three new pins (`Glfw` 2026.10.4, `Vulkan` 2026.10.2, `Vulkan.Abstractions` 2026.10.2) are in `Directory.Packages.props`. `HelloPanel` gets `Ahwoo.BRUTAL.ImGui`, `Ahwoo.BRUTAL.ImGui.Backends`, and `Ahwoo.BRUTAL.Numerics` through the project references. It does not reference them directly.
- Phase 2, argument parser: a repeated `--frames` is a usage error (exit 2). The contract does not name this case. One value per run keeps the contract clear. A sign is accepted by the parser, so `--frames -5` reports "below 1" and not "not an integer". `--frames=120` is an unknown argument (exit 2).
- Phase 2, exit codes: a failure inside the window, Vulkan, or ImGui setup throws. The top `catch` in `Main` writes the exception to standard error and returns 1. A failed `Glfw.Init()` writes the GLFW error code and text and returns 1 with no exception.
- Phase 2, `VulkanContext`: two frames in flight. One acquire semaphore and one fence per frame slot. One render-complete semaphore per swapchain image, so a present never reuses a semaphore that is still in use. FIFO present mode, because every driver supports it. A UNORM surface format with the sRGB color space, because ImGui gives sRGB colors. The Vulkan API version is 1.1. The constructor disposes the partial object on a failure. The nested `VulkanContext.CreateInfo` struct follows the BRUTAL pattern of `GlfwWindow.CreateInfo` and `VulkanBackend.CreateInfo`.
- Phase 2, ImGui: `MinImageCount` is the surface minimum plus 1, and at least 2. `ImageCount` is the real swapchain image count. `DescriptorPoolSize` is 8, the minimum that `VulkanBackend` accepts. `io.IniFilename` is null, so no `imgui.ini` file appears in the repository root.
- Phase 2, teardown order: wait for the device, dispose `VulkanBackend`, dispose `GlfwBackend`, destroy the ImGui context, dispose `VulkanContext`, dispose the window, end GLFW, then write the success line. `using` declarations and `try`/`finally` blocks keep this order on the failure path too.
- Phase 2, no suppression: the build has no `NoWarn`. The BRUTAL analyzer `BRUTALCORE0011` asked for a UTF-8 literal in `SupportsExtension`. The code now uses `"VK_KHR_swapchain"u8`.
- Phase 2, demo result (task 2.12): `dotnet build brutal-ui.sln` 0 warnings, 0 errors (Debug and Release). `dotnet test --solution brutal-ui.sln` 3 passed, 0 failed. `dotnet sln list` prints 11 projects. Exit 2 for `--frames abc`, `--frames 0`, `--bogus`, `--frames` (no value), `--frames -5`, `--frames 99999999999`, and `--frames 3 --frames 4`. Exit 2 for `--frames abc` with `--no-build -c Release`. With no display: exit 1, "GLFW init failed: PlatformUnavailable". Under `xvfb-run -a` with the NVIDIA ICD: exit 1 with `DllNotFoundException` for `libvulkan.so` (see the Linux gotcha). Under `xvfb-run -a` with the NVIDIA ICD and a scratchpad `libvulkan.so` link on `LD_LIBRARY_PATH`: exit 0, `HelloPanel: 120 frames rendered.`. The same with the lavapipe ICD: exit 0. Demo: PASS on the GPU path (driver 595.99.02, kernel module 595.99.02, RTX 2060), with the `libvulkan.so` workaround.
- Phase 2, visual check: the Mesa screenshot layer (`VK_LAYER_MESA_screenshot`) captured frame 60 under Xvfb with the NVIDIA ICD. The image shows the `HelloPanel` ImGui window with the text `Hello, brutal-ui` on the dark clear color, at 1280 x 720. The layer writes the frame number `60` to standard output. That line does not come from the sample.
- Phase 3, group 1, tasks 3.1 to 3.4 done. `LICENSE` holds the MIT text. The root `Directory.Build.props` holds the seven pack defaults and keeps `IsPackable` `false`. `src/Directory.Build.props` imports the root file with `GetPathOfFileAbove` and sets `IsPackable` and `GenerateDocumentationFile` to `true`. `.gitignore` holds `artifacts/` under `# .NET`. The pack metadata uses `PackageLicenseExpression` only, with no `PackageLicenseFile`. Group check: `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives 3 passed, 0 failed. The empty `src/` projects raise no documentation warning.
- Phase 3, group 2, tasks 3.5 and 3.6 done. Each `src/` project holds one `PropertyGroup` with one `Description`, placed before the existing `ItemGroup` items. No reference changed. `dotnet pack brutal-ui.sln -c Release` exits 0 with 0 warnings and writes exactly five `.nupkg` files (`BrutalUi.Core`, `ImGui`, `Framework`, `Markup`, `Debugger`, all `0.1.0-alpha`). No test or sample package appears. NuGet prints one informational line per package, "missing a readme". It is not a warning, so no `NoWarn` and no readme file exist. Add a readme when the packages go public. Each `.nuspec` holds `<license type="expression">MIT</license>`, authors `Aaron Static`, and a description. The Core `.nuspec` lists `Ahwoo.BRUTAL.Localization` 2026.10.2 and `Ahwoo.BRUTAL.Numerics` 2026.10.1 only. The Debugger `.nuspec` lists `BrutalUi.Framework` and `BrutalUi.ImGui` at `0.1.0-alpha`. `dotnet pack src/BrutalUi.Core/BrutalUi.Core.csproj -c Release` exits 0 with 0 warnings. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives 3 passed, 0 failed. `git status --porcelain` shows no `artifacts/`, `bin/`, or `obj/` path.
- Review group 1, CR-1 and CR-4 done. After `Glfw.PollEvents()`, `RunFrames` waits in `while (window.IsIconified && !window.ShouldClose) Glfw.WaitEvents();`, and then checks `ShouldClose`. A skipped frame does not count toward `N`. `GlfwWindow.IsIconified` reads a cached field. The native iconify callback of `GlfwWindow` sets that field, and `GlfwBackend` does not replace the callback, because it uses the managed events. The loop allocates nothing new. `GlfwWindow.WaitForWindow()` is not used, because it does not check the close flag. CR-4: the constant `ImGuiDescriptorPoolSize` (8) has a doc comment. `VulkanBackend.CreateDescriptorPool` asserts `DescriptorPoolSize >= 8`. Check: build 0 warnings, 0 errors. The four usage checks exit 2. The GPU smoke run exits 0 with `HelloPanel: 120 frames rendered.`. Xvfb cannot minimize a window, so the minimized path needs the manual Windows run (F8).
- Review group 2, CR-2 done. `WarningsNotAsErrors` in `Directory.Build.props` now holds `NU1900;NU1901;NU1902;NU1903;NU1904`. An unreachable audit source or a new advisory prints a warning but does not fail an unchanged commit (Plan Decision 6). The comment above the property group says so. Check: build 0 warnings, 0 errors. Test 3 passed, 0 failed.

---

## Gotchas & Learnings

- Linux needs the unversioned `libvulkan.so` for `Brutal.Vulkan`. See the Native Dependencies section. Without it, `HelloPanel` exits 1 with `DllNotFoundException: Unable to load shared library 'vulkan'`. Test workaround with no system change: `mkdir <dir> && ln -s /usr/lib/x86_64-linux-gnu/libvulkan.so.1 <dir>/libvulkan.so`, then run with `LD_LIBRARY_PATH=<dir>`.
- `Brutal.Vulkan` calls `NativeLibrary.SetDllImportResolver` in a static constructor. A second resolver for the same assembly throws, so an application cannot add its own Vulkan resolver.
- `VulkanBackend.Dispose` does not wait for the GPU. Call `vkDeviceWaitIdle` first.
- `GlfwBackend` must exist before `VulkanBackend`. `VulkanBackend` asserts that the platform sets `Platform_CreateVkSurface`.
- `ImGui.Text(ImString)` passes no end pointer, so the text must end with a null byte. A `u8` literal does. A slice of a larger buffer does not.
- ImGui writes `imgui.ini` into the working directory by default. Set `io.IniFilename = default`.
- The BRUTAL analyzer `BRUTALCORE0011` runs in the build and prefers a UTF-8 literal overload. With warnings as errors, it fails the build.
- The Khronos validation layer is not on this machine. The sample has not run under validation.
- `dotnet new sln --format sln` writes a UTF-8 BOM and a blank first line. The classic header is the first non-empty line.
- `TreatWarningsAsErrors` also turns NuGet audit warnings into errors. NU1900 (audit source unreachable) failed a full offline restore until CR-2 added it to `WarningsNotAsErrors`.
- The `HelloPanel` frame waits have no time limit (CR-3 declined). A caller that needs a bounded run wraps the sample in `timeout`.

---

## Package API Findings (Phase 2, task 2.1)

The packages hold no BRUTAL sample that sets up GLFW, Vulkan, and ImGui together. The XML doc of `Brutal.ImGui.Backends` holds two type summaries only. The agent decompiled the public surface with `ilspycmd` 11.1 in the session scratchpad. No decompiled file is in the repository. `HelloPanel` follows the pattern of the upstream `imgui_impl_vulkan` example and the window helpers inside `VulkanBackend`.

| Area | API that `HelloPanel` uses |
|---|---|
| GLFW start and end | `Glfw.Init()` returns `bool`. `Glfw.Terminate()`. `Glfw.GetError(out string? description)` returns `GlfwErrorCode`. `Glfw.VulkanSupported()` returns `bool`. |
| Window hints | `Glfw.WindowHint(GlfwWindowHint.ClientApi, Glfw.Constants.GLFW_NO_API)` and `Glfw.WindowHint(GlfwWindowHint.Resizable, Glfw.Constants.GLFW_FALSE)`. |
| Window | `Glfw.CreateWindow(in GlfwWindow.CreateInfo)`. The create-info has `required string Title` and `required int2 Size`. A failure throws `InvalidOperationException` with the GLFW error text. `GlfwWindow` is `IDisposable`. `window.ShouldClose` turns `true` in the close callback. `window.FramebufferSize` is an `int2`. |
| Events | `Glfw.PollEvents()`. It rethrows an exception from a managed GLFW callback. |
| Vulkan surface | `Glfw.GetRequiredInstanceExtensions(out uint count)` returns `byte**`. `Glfw.CreateWindowSurface(nint instance, GlfwWindow window, out ulong surface)` returns a `VkResult` as `int`. Wrap the handle with `new VkSurfaceKHR((nint)surface)`. |
| Vulkan instance | `Vulkan.CreateInstance(Instance.CreateInfo)`. The create-info is a `ref struct` with `AppInfo`, `Flags`, `EnabledExtensions`, and `EnabledLayers`. Both sets must be non-null. `instance.PhysicalDevices` is an array. `Instance` is `IDisposable`. `instance.DestroySurfaceKHR(surface, null)`. |
| Physical device | `GetProperties()`, `SupportsExtension(string)`, `GetQueueFamilyProperties_Count()` and `_Write(span)`, `GetSurfaceSupportKHR(family, surface)`, `GetSurfaceCapabilitiesKHR(surface)`, `GetSurfaceFormatsKHR_Count` and `_Write`, `GetSurfacePresentModesKHR_Count` and `_Write`. |
| Logical device | `Brutal.Vulkan.Abstractions`: `physicalDevice.CreateDevice(DeviceEx.CreateInfo)` returns `DeviceEx`. `VulkanBackend` requires a `DeviceEx`, so the sample references `Ahwoo.BRUTAL.Vulkan.Abstractions` directly. `VulkanHelpers.AddPlatformRequiredExtensions`, `GetPlatformRequiredInstanceCreateFlags`, `AddSurfaceExtensions`, `AddSwapchainExtensions`, and `AddMandatoryIfSupportedDeviceExtensions` add the macOS portability items. `device.GetQueue(family, 0)` returns a `Queue` with a `Family` property. |
| Frame objects | `Device` extension methods: `CreateSwapchainKHR`, `GetSwapchainImagesKHR_Count` and `_Write`, `CreateImageView`, `CreateRenderPass`, `CreateFramebuffer`, `CreateCommandPool`, `CreateSemaphore`, `CreateFence`, and the matching `Destroy*` calls. `Brutal.Vulkan.Abstractions` adds `AllocateCommandBuffers(info, Span<CommandBuffer>)`, `WaitForFence(fence, timeout)`, and `queue.Submit(wait, stages, buffers, signal, fence)`. These helpers use `stackalloc`, not the managed heap. |
| Frame calls | `device.AcquireNextImageKHR(...)` and `queue.PresentKHR(...)` throw `VkResultError` on `VK_ERROR_OUT_OF_DATE_KHR`. They return `VkResult.SuboptimalKHR` as a value. `CommandBuffer` is a struct with the `Reset`, `Begin`, `BeginRenderPass`, `EndRenderPass`, and `End` extension methods. |
| Render pass | `DeviceExtensions.CreateDefaultRenderPass` makes an offscreen pass with the final layout `ShaderReadOnlyOptimal` and the store op `DontCare`. It does not fit a swapchain. `VulkanContext` makes its own pass with the final layout `PresentSrcKHR`. |
| ImGui platform backend | `new GlfwBackend(GlfwWindow window, GlfwBackend.ClientApi.Vulkan)`. It sets `Platform_CreateVkSurface`. Create it before `VulkanBackend`, because `VulkanBackend` asserts that this hook exists. `NewFrame()` reads the window size and the time. `Dispose()`. |
| ImGui renderer backend | `new VulkanBackend(in VulkanBackend.CreateInfo)`. Required fields: `DeviceEx Device`, `Queue GraphicsQueue`, `VkRenderPass RenderPass`, `int MinImageCount` (at least 2), `int ImageCount` (at least `MinImageCount`), `VkSampleCountFlags SampleCount`, and `int DescriptorPoolSize` (at least 8). Optional: `SubPass`, `MinAllocationSize`. `NewFrame()` does nothing. `RenderDrawData(ImDrawDataPtr, CommandBuffer)` uploads the font texture on the first frame with a blocking submit on `GraphicsQueue`, then records the draw commands into the open render pass. `Dispose()` does not wait for the device. The caller waits first. |
| ImGui frame | `ImGui.CreateContext()`, `ImGui.GetIO()`, `ImGui.StyleColorsDark()`, `ImGui.NewFrame()`, `ImGui.Begin(ImString, ImGuiWindowFlags)`, `ImGui.Text(ImString)`, `ImGui.End()`, `ImGui.Render()`, `ImGui.GetDrawData()`, `ImGui.DestroyContext(ctx)`. |
| ImGui text | `ImString` is a `ref struct`. The implicit conversion from `ReadOnlySpan<byte>` keeps the span and does not copy. `ImGui.Text` calls `TextUnformatted(text, null)`, so it relies on the null terminator that the compiler puts after a `u8` literal. Use a `u8` literal only. A sliced span has no terminator. |
| ImGui settings file | `io.IniFilename` is a `ref Ptr<byte>`. The sample sets it to `default`, so ImGui writes no `imgui.ini` into the working directory. |

---

## Native Dependencies

`HelloPanel` loads these native libraries. `AGENTS.md` requires this list.

| Library | Linux | Windows | Source | Notes |
|---|---|---|---|---|
| GLFW | `libglfw.so` (also `.so.3`, `.so.3.5`) | `glfw3.dll` | `Ahwoo.BRUTAL.Glfw` 2026.10.4, `runtimes/<rid>/native/` | Loaded through `DllImport("glfw")`. GLFW also loads the system X11 or Wayland client libraries. |
| Dear ImGui | `libimgui.so` | `imgui.dll` | `Ahwoo.BRUTAL.ImGui` 2026.10.2, `runtimes/<rid>/native/` | Comes through the project reference to `BrutalUi.ImGui`. |
| Vulkan loader | `libvulkan.so` for `Brutal.Vulkan`, `libvulkan.so.1` for GLFW | `vulkan-1.dll` from `System32` | The system. The GPU driver or the OS package installs it. | See the Linux gotcha below. |
| Vulkan driver (ICD) | For example `nvidia_icd.json` | Installed by the driver | The GPU driver | The loader finds it. `VK_ICD_FILENAMES` selects one. |
| VulkanEx | `libVulkanEx.so` | `VulkanEx.dll` | `Ahwoo.BRUTAL.Vulkan` 2026.10.2 | Copied to the output. The static constructor of `Brutal.VulkanApi.Vulkan` loads it on macOS only, so `HelloPanel` does not load it on Linux or Windows. |

Linux gotcha: the static constructor of `Brutal.VulkanApi.Vulkan` calls `NativeLibrary.Load("vulkan", ..., DllImportSearchPath.LegacyBehavior)`. On Linux, .NET probes `libvulkan.so` and not `libvulkan.so.1`. The unversioned `libvulkan.so` comes from the development package (`libvulkan-dev` on Debian and Ubuntu). This machine has only `libvulkan.so.1`. Without `libvulkan.so`, the first Vulkan call throws, and `HelloPanel` exits 1. The sample cannot fix this itself. `Brutal.Vulkan` sets its own `DllImportResolver`, and a second resolver for the same assembly throws. Owner: `dev-ops/linux-dev-environment` (install `libvulkan-dev`, or document a `libvulkan.so` link on `LD_LIBRARY_PATH`).

---

## HelloPanel Command Line Contract

Plan Decision 9. The sibling features `dev-ops/linux-dev-environment` and `dev-ops/windows-test-path` read this table.

| Item | Value |
|---|---|
| Argument form | `--frames <N>`. `N` is a decimal integer from 1 to 2147483647. |
| No argument | The sample runs until the user closes the window. This mode is for the manual look on Windows. |
| Recommended `N` for scripts | 120 |
| Frame | One loop pass: poll events, start an ImGui frame, draw one text label, render through the Vulkan backend, present. |
| Window close before `N` frames | The loop stops. The sample exits 0. |
| Standard output on success | One line: `HelloPanel: <count> frames rendered.` |
| Exit code 0 | The sample rendered `N` frames, or the user closed the window. |
| Exit code 1 | A runtime failure: GLFW init, window creation, Vulkan init, swapchain, or any unhandled exception. The sample writes the error to standard error. |
| Exit code 2 | A usage error: an unknown argument, a missing value, a value that is not an integer, or a value below 1. The sample writes a usage line to standard error. It parses the arguments before it starts GLFW, so this check needs no GPU. |

Shared commands, from the repository root: `dotnet build brutal-ui.sln [-c <Config>]`, `dotnet test --solution brutal-ui.sln [-c <Config>]`, and `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build [-c <Config>] -- --frames <N>`.

---

## Testing Approach

### Unit Tests
**Files:** `tests/*/PlaceholderTests.cs`
- One placeholder fact per test project. Each fact loads the assembly under test by name.

### Integration Tests
- The solution build, `dotnet list package --include-transitive` on `BrutalUi.Core`, and `dotnet pack` with a read of the `.nuspec`.

### Manual Testing Checklist
- [ ] `HelloPanel --frames 120` shows `Hello, brutal-ui` and exits 0 (GPU and display)
- [ ] The Windows build and test give the same result as Linux

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the user runs `dotnet test --solution brutal-ui.sln` at the repository root and sees 3 passed tests and exit code 0.
- [ ] Phase 2 demo: the user runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120`, sees a window with the text `Hello, brutal-ui`, and the process exits with code 0.
- [ ] Phase 2 Linux note: on Linux, `libvulkan.so` must exist before the run (`libvulkan-dev`, or a `libvulkan.so` link on `LD_LIBRARY_PATH`). Without it, the sample exits 1 with `DllNotFoundException` for `vulkan`.
- [ ] Phase 2 Windows: the same run on Windows with `vulkan-1.dll` from the driver. Nobody has run it yet.
- [ ] CR-1 Windows: run `HelloPanel` with no argument, minimize the window, restore it, and see the frames continue. Then minimize it again and close it from the taskbar. The process exits with code 0. Xvfb cannot minimize a window, so no automated check covers this path.
- [ ] Phase 3 demo: the user runs `dotnet pack brutal-ui.sln -c Release` and sees exactly five `.nupkg` files in `artifacts/packages/`.

---

## Next Steps

### Immediate (Today/This Session)
1. The caller (`/autorun-epic`) commits the work and runs `/update-epic dev-ops` and `/update-master`.
2. A person tests the items of `## Needs Human Verification`.

### Short Term (This Week)
1. `dev-ops/linux-dev-environment`: supply `libvulkan.so` on Linux, and wrap the smoke run in `timeout`. Its draft plan exists.
2. `dev-ops/windows-test-path`: run the sample on Windows, including the minimize path of CR-1.

---

## Open Questions

- `dev-ops/linux-dev-environment`: install `libvulkan-dev` as a prerequisite, or document the `libvulkan.so` link on `LD_LIBRARY_PATH`? The sample cannot fix this itself. A report to BRUTAL can ask `Brutal.Vulkan` to try `libvulkan.so.1` on Linux.

## Follow-up Work and Risks

- Blast radius of phase 2: `Directory.Packages.props` (three new pins, no change to the existing pins), `brutal-ui.sln` (one new project), and the new `samples/HelloPanel/` folder. No `src/` project changed. `BrutalUi.Core` still has no ImGui reference.
- Allocation: the `HelloPanel` loop allocates no managed memory in its own code. `GlfwBackend.NewFrame` calls `UpdateMonitors` each frame, and `VulkanBackend.RenderDrawData` maps memory each frame. Nobody has measured whether these BRUTAL calls allocate. The project allocation test must measure the backends too.
- Validation: run the sample once under `VK_LAYER_KHRONOS_validation` on a machine that has the layer.
- Upgrade risk: a new `Ahwoo.BRUTAL.*` version can change the generated Vulkan signatures that `VulkanContext` uses. The build shows such a change.

---

## Session Notes

### 2026-10-16 09:56
- Started the feature from `/autorun-feature` in the called mode of `/autorun-epic`.
- The .NET SDK 10.0.112 exists at `/usr/bin/dotnet` (item A1).

### 2026-10-16 10:20
- Phase 1 done in four task groups (briefs `phase-1-g1.md` to `phase-1-g4.md`), each under the limit of 6 files.
- Verifier `phase-1-verify.md`: PASS. Build 0 warnings, 0 errors. Test 3 passed. 10 projects. Classic `.sln` header after a UTF-8 BOM and a blank line. Demo: PASS.
- Watch item: `Microsoft.Extensions.ObjectPool` `11.0.0-preview.5.26302.115` reaches `BrutalUi.Core` as a transitive package of `Ahwoo.BRUTAL.*`. No gate covers it.
- Phase 2 done, tasks 2.1 to 2.12. The agent read the BRUTAL API with `ilspycmd` in the session scratchpad. It wrote `HelloPanel.csproj`, `Program.cs`, and `VulkanContext.cs`, added three pins, and added the sample to the solution. Build 0 warnings. Test 3 passed. Usage checks exit 2. GPU run exit 0 under Xvfb (NVIDIA and lavapipe) with a scratchpad `libvulkan.so` link. Frame 60 shows `Hello, brutal-ui`.
- Task 1.10 done. The three test projects reference `xunit.v3` only. Restore needed no direct `Microsoft.Testing.Platform` pin, and no `NoWarn` exists. Each project builds with 0 warnings and 0 errors, and each runs 1 passed test with `dotnet test --project <csproj>`.

### 2026-10-16 10:21
- Verifier `phase-2-verify.md`: PASS. Build 0 warnings in Debug and Release. Test 3 passed. 11 projects. Usage checks exit 2, also in the `--no-build -c Release` form. Demo: PASS under `xvfb-run` on the NVIDIA ICD with the scratchpad `libvulkan.so` link. Without the link, the sample exits 1 (`DllNotFoundException` for `vulkan`). The visual check of the window text stays MANUAL.

### 2026-10-16 10:25
- Phase 3 done in two task groups (`phase-3-g1.md`, `phase-3-g2.md`). Verifier `phase-3-verify.md`: PASS. Pack exits 0 with 0 warnings and writes exactly five `0.1.0-alpha` packages. The Core `.nuspec` lists exactly Numerics 2026.10.1 and Localization 2026.10.2. The Debugger `.nuspec` lists Framework and ImGui at 0.1.0-alpha. Each `src/` project packs alone. Build 0 warnings. Test 3 passed. Demo: PASS.
- NuGet prints an information line "missing a readme" for each package. It is not a warning. Add a package readme before a public release.

### 2026-10-16 10:53
- Cross-phase review: `code-review.md` holds 5 findings (2 MEDIUM, 3 LOW). The Vercel React rules of `/review-feature` do not apply to C#, so the review used `AGENTS.md`, technical design section 15, and the plan quality bar.
- Fixed: CR-1 (minimize wait in the frame loop), CR-2 (NU1900 not an error), CR-4 (named descriptor pool size), CR-5 (ignore `BenchmarkDotNet.Artifacts/`). Briefs `review-g1.md` to `review-g3.md`.
- Declined: CR-3 (finite frame wait). A finite wait can stop a manual run with exit 1 when a compositor throttles a hidden window. The sibling scripts wrap the smoke run in a timeout.
- Verifier `review-verify.md`: PASS. Build 0 warnings in Debug and Release. Test 3 passed. Usage checks exit 2. Pack writes five packages. GPU smoke run exit 0 under Xvfb with the `libvulkan.so` link.
- Next feature draft: `docs/features/dev-ops/linux-dev-environment/implementation.md` (3 phases, 18 tasks, 4 open scope questions in Notes).
- Inbox: 0 lines. No item reached the run.
- `implementation.md` status: Ready for Review.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
