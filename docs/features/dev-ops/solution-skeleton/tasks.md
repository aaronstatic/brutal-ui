# dev-ops/solution-skeleton - Task Checklist

**Last Updated:** 2026-10-16 10:53
**Progress:** 31/31 tasks complete (100%)
**Advanced phases:** Phase 2 (advanced agent, opus)

---

## Phase 1: Solution, Props, Pins, and Test Projects (13/13 complete)

**Demo:** the user runs `dotnet test --solution brutal-ui.sln` at the repository root and sees 3 passed tests and exit code 0.
**Needs:** none

### Task Group 1A: Root Configuration, Libraries, and WorldSpace

- [x] ✅ **1.1 Write `global.json`** - Completed 2026-10-16
  - Description: Set `sdk.version` to `10.0.100` and `sdk.rollForward` to `latestFeature`. Set `test.runner` to `Microsoft.Testing.Platform`. Run `dotnet --version` at the root and confirm a `10.0.1xx` version (10.0.112 on this machine).
  - File(s): `global.json`
  - Estimate: 4 steps

- [x] ✅ **1.2 Write `nuget.config`** - Completed 2026-10-16
  - Description: Add `<clear />` and one source, `https://api.nuget.org/v3/index.json`, with the key `nuget.org`.
  - File(s): `nuget.config`
  - Estimate: 3 steps

- [x] ✅ **1.3 Write `Directory.Build.props`** - Completed 2026-10-16
  - Description: `TargetFramework` `net10.0`, `LangVersion` `14`, `Nullable` `enable`, `AllowUnsafeBlocks` `true`, `ImplicitUsings` `disable`, `TreatWarningsAsErrors` `true`, `WarningsNotAsErrors` `NU1901;NU1902;NU1903;NU1904`, `EnforceCodeStyleInBuild` `true`, `IsPackable` `false`, `UseMicrosoftTestingPlatformRunner` `true`. One short ASD-STE100 comment for each group of properties. No pack metadata (phase 3 adds it).
  - File(s): `Directory.Build.props`
  - Estimate: 4 steps

- [x] ✅ **1.4 Write `Directory.Packages.props`** - Completed 2026-10-16
  - Description: `ManagePackageVersionsCentrally` `true`. One `PackageVersion` per phase 1 pin: `Ahwoo.BRUTAL.Numerics` 2026.10.1, `Ahwoo.BRUTAL.Localization` 2026.10.2, `Ahwoo.BRUTAL.ImGui` 2026.10.2, `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2, `xunit.v3` 4.0.2, `BenchmarkDotNet` 0.15.8. Exact nuget.org id spelling. Plain version strings, not bracket ranges.
  - File(s): `Directory.Packages.props`
  - Estimate: 4 steps

- [x] ✅ **1.5 Write `.editorconfig`** - Completed 2026-10-16
  - Description: `root = true`. All files: `charset = utf-8`, `indent_style = space`, `indent_size = 4`, `insert_final_newline = true`, `trim_trailing_whitespace = true`. `*.{csproj,props,targets,json,config,xml}`: `indent_size = 2`. `*.sln`: `indent_style = tab`. `*.cs`: `csharp_style_namespace_declarations = file_scoped:warning`, `dotnet_diagnostic.IDE0161.severity = warning`, `csharp_using_directive_placement = outside_namespace:warning`, `dotnet_sort_system_directives_first = true`, `csharp_new_line_before_open_brace = all`. Do not set `end_of_line`.
  - File(s): `.editorconfig`
  - Estimate: 3 steps

- [x] ✅ **1.6 Write `src/BrutalUi.Core/BrutalUi.Core.csproj`** - Completed 2026-10-16
  - Description: `Sdk="Microsoft.NET.Sdk"`. `PackageReference` items for `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization` with no `Version` attribute. No project reference. No source file.
  - File(s): `src/BrutalUi.Core/BrutalUi.Core.csproj`
  - Estimate: 3 steps

- [x] ✅ **1.7 Write the four other library project files** - Completed 2026-10-16
  - Description: `BrutalUi.ImGui` (Core, `Ahwoo.BRUTAL.ImGui`, `Ahwoo.BRUTAL.ImGui.Backends`), `BrutalUi.Framework` (Core), `BrutalUi.Markup` (Core), `BrutalUi.Debugger` (Framework, ImGui). Forward slashes in `ProjectReference` paths, exact file name case. No source file.
  - File(s): `src/BrutalUi.ImGui/BrutalUi.ImGui.csproj`, `src/BrutalUi.Framework/BrutalUi.Framework.csproj`, `src/BrutalUi.Markup/BrutalUi.Markup.csproj`, `src/BrutalUi.Debugger/BrutalUi.Debugger.csproj`
  - Estimate: 6 steps

- [x] ✅ **1.8 Write the `WorldSpace` sample** - Completed 2026-10-16
  - Description: `WorldSpace.csproj` (`OutputType` `Exe`, project references to Core and ImGui). `Program.cs`: file-scoped namespace `WorldSpace`, `static class Program`, `static int Main(string[] args)`, writes `WorldSpace: this sample has no scene yet.` to standard output, returns 0.
  - File(s): `samples/WorldSpace/WorldSpace.csproj`, `samples/WorldSpace/Program.cs`
  - Estimate: 4 steps

- [x] ✅ **1.9 Build the group 1A projects** - Completed 2026-10-16
  - Description: In one shell command, build `src/BrutalUi.Debugger/BrutalUi.Debugger.csproj` and `samples/WorldSpace/WorldSpace.csproj`. Confirm 0 warnings and 0 errors. Run `WorldSpace` and confirm exit 0. Run `dotnet list src/BrutalUi.Core/BrutalUi.Core.csproj package --include-transitive` and confirm no package id that starts with `Ahwoo.BRUTAL.ImGui`.
  - File(s): none (verification)
  - Estimate: 6 steps

### Task Group 1B: Test Projects, Benchmarks, and the Solution

- [x] ✅ **1.10 Write the three test projects and placeholder tests** - Completed 2026-10-16
  - Description: Each `.csproj` sets `OutputType` `Exe`, references `xunit.v3` with no version, and holds one `ProjectReference` to its project under test. Each `PlaceholderTests.cs` uses a file-scoped namespace equal to the project name, holds one `public sealed class PlaceholderTests` with one `[Fact]` that loads the assembly under test by name (for example `Assembly.Load(new AssemblyName("BrutalUi.Core"))`) and asserts not null. Explicit `using` directives.
  - File(s): `tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj`, `tests/BrutalUi.Core.Tests/PlaceholderTests.cs`, `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`, `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs`, `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`, `tests/BrutalUi.Markup.Tests/PlaceholderTests.cs`
  - Estimate: 8 steps

- [x] ✅ **1.11 Write the benchmark project** - Completed 2026-10-16
  - Description: `BrutalUi.Benchmarks.csproj` (`OutputType` `Exe`, `PackageReference` `BenchmarkDotNet`, `ProjectReference` Core). `Program.cs` with `static int Main(string[] args)` that calls `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)` and returns 0. `PlaceholderBenchmark.cs` with one unsealed `public class PlaceholderBenchmark`, `[MemoryDiagnoser]`, and one empty `[Benchmark]` method.
  - File(s): `tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj`, `tests/BrutalUi.Benchmarks/Program.cs`, `tests/BrutalUi.Benchmarks/PlaceholderBenchmark.cs`
  - Estimate: 5 steps

- [x] ✅ **1.12 Create the solution** - Completed 2026-10-16
  - Description: In one shell command: `dotnet new sln --name brutal-ui --format sln`, then `dotnet sln brutal-ui.sln add` with the five `src/` projects, the four `tests/` projects, and `samples/WorldSpace/WorldSpace.csproj`. Confirm that `dotnet sln brutal-ui.sln list` prints 10 projects.
  - File(s): `brutal-ui.sln`
  - Estimate: 4 steps

- [x] ✅ **1.13 Run the phase demo** - Completed 2026-10-16
  - Description: In one shell command: `dotnet build brutal-ui.sln`, then `dotnet test --solution brutal-ui.sln`. Confirm `0 Warning(s)`, `0 Error(s)`, 3 passed tests, 0 failed, exit 0. Confirm that `dotnet test` does not run `BrutalUi.Benchmarks` or `WorldSpace`. Confirm that `git status --porcelain` shows no `bin/` or `obj/` path.
  - File(s): none (verification)
  - Estimate: 6 steps

---

## Phase 2: HelloPanel Runs N Frames (12/12 complete)

**Demo:** the user runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120`, sees a window with the text `Hello, brutal-ui`, and the process exits with code 0.
**Needs:** Phase 1

- [x] ✅ **2.1 Read the package API** - Completed 2026-10-16
  - Description: Read the XML docs of `Ahwoo.BRUTAL.ImGui.Backends`, `Ahwoo.BRUTAL.Glfw`, `Ahwoo.BRUTAL.Vulkan`, and `Ahwoo.BRUTAL.Vulkan.Abstractions` under `~/.nuget/packages/`. When the docs are not sufficient, install `ilspycmd` into the scratchpad (not the repository) and decompile the public surface. Find the `GlfwBackend` and `VulkanBackend` create-info, the per-frame calls, the window and surface creation, the Vulkan helpers, and the ImGui text call form. Follow a BRUTAL example when one exists.
  - File(s): none (research)
  - Estimate: 15 steps

- [x] ✅ **2.2 Record the API findings and the native libraries** - Completed 2026-10-16
  - Description: Write the API calls, every native library the sample loads (GLFW, ImGui, the system Vulkan loader), and the HelloPanel command line contract into `context.md`.
  - File(s): `docs/features/dev-ops/solution-skeleton/context.md`
  - Estimate: 3 steps

- [x] ✅ **2.3 Pin the sample packages** - Completed 2026-10-16
  - Description: Add `Ahwoo.BRUTAL.Glfw` 2026.10.4, `Ahwoo.BRUTAL.Vulkan` 2026.10.2, and `Ahwoo.BRUTAL.Vulkan.Abstractions` 2026.10.2 (only when the sample uses it) to `Directory.Packages.props`.
  - File(s): `Directory.Packages.props`
  - Estimate: 2 steps

- [x] ✅ **2.4 Write `HelloPanel.csproj`** - Completed 2026-10-16
  - Description: `OutputType` `Exe`, project references to Core and ImGui, the package references of task 2.3 with no version.
  - File(s): `samples/HelloPanel/HelloPanel.csproj`
  - Estimate: 2 steps

- [x] ✅ **2.5 Write the argument parser** - Completed 2026-10-16
  - Description: `static int Main(string[] args)` parses `--frames <N>` per the contract before any GLFW call. On a usage error, write `Usage: HelloPanel [--frames <N>]` and the reason to standard error and return 2.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 4 steps

- [x] ✅ **2.6 Write the window setup** - Completed 2026-10-16
  - Description: Start GLFW, set the no-client-API hint, make the window not resizable, open a 1280 x 720 window titled `HelloPanel`. On a failure, write the error to standard error and return 1.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 4 steps

- [x] ✅ **2.7 Write the Vulkan device setup** - Completed 2026-10-16
  - Description: Instance with the GLFW extensions, surface, physical device with graphics and present queues, logical device. Use `Brutal.Vulkan.Abstractions` helpers when they exist. `required` create-info struct for the `VulkanContext` constructor.
  - File(s): `samples/HelloPanel/VulkanContext.cs`
  - Estimate: 8 steps

- [x] ✅ **2.8 Write the frame resources** - Completed 2026-10-16
  - Description: Swapchain, render pass, framebuffers, command pool, command buffers, semaphores, and fences, all inside `VulkanContext`. No swapchain re-creation. On `VK_ERROR_OUT_OF_DATE_KHR`, throw, so the sample exits 1.
  - File(s): `samples/HelloPanel/VulkanContext.cs`
  - Estimate: 10 steps

- [x] ✅ **2.9 Write the ImGui setup** - Completed 2026-10-16
  - Description: Create the ImGui context, the `GlfwBackend`, and the `VulkanBackend` with the render pass of `VulkanContext`. Default font.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 4 steps

- [x] ✅ **2.10 Write the frame loop and the teardown** - Completed 2026-10-16
  - Description: Each pass polls events, calls both backend `NewFrame` methods, starts the ImGui frame, opens one ImGui window, draws `Hello, brutal-ui` from a UTF-8 literal, renders, records the command buffer, calls `RenderDrawData`, submits, and presents. Stop at `N` frames or on window close. No managed allocation in the loop. After the loop: wait idle, dispose in reverse order, end GLFW, write `HelloPanel: <count> frames rendered.`, return 0. Catch every exception at the top of `Main`, write it to standard error, return 1.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 8 steps

- [x] ✅ **2.11 Add the sample to the solution** - Completed 2026-10-16
  - Description: `dotnet sln brutal-ui.sln add samples/HelloPanel/HelloPanel.csproj`. Confirm that `dotnet sln brutal-ui.sln list` prints 11 projects.
  - File(s): `brutal-ui.sln`
  - Estimate: 2 steps

- [x] ✅ **2.12 Verify the phase** - Completed 2026-10-16
  - Description: In one shell command: build with 0 warnings, test with 3 passed tests, run the sample with `--frames abc`, `--frames 0`, and `--bogus` and confirm exit 2 for each. When a GPU and a display are ready, run `--frames 120` and confirm exit 0 and the output line. Otherwise record `Demo: MANUAL` and the reason in `context.md`.
  - File(s): none (verification)
  - Estimate: 6 steps

---

## Phase 3: Pack Metadata and `dotnet pack` (6/6 complete)

**Demo:** the user runs `dotnet pack brutal-ui.sln -c Release` and sees exactly five `.nupkg` files in `artifacts/packages/`.
**Needs:** Phase 1

### Task Group 3A: Shared Pack Settings

- [x] ✅ **3.1 Write `LICENSE`** - Completed 2026-10-16
  - Description: The standard MIT licence text with the line `Copyright (c) 2026 Aaron Static`.
  - File(s): `LICENSE`
  - Estimate: 2 steps

- [x] ✅ **3.2 Add the pack defaults to `Directory.Build.props`** - Completed 2026-10-16
  - Description: `Version` `0.1.0-alpha`, `Authors` `Aaron Static`, `Copyright` `Copyright (c) 2026 Aaron Static`, `PackageLicenseExpression` `MIT`, `RepositoryUrl` `https://github.com/aaronstatic/brutal-ui`, `RepositoryType` `git`, `PackageOutputPath` `$(MSBuildThisFileDirectory)artifacts/packages/`. Keep `IsPackable` `false` as the default.
  - File(s): `Directory.Build.props`
  - Estimate: 3 steps

- [x] ✅ **3.3 Write `src/Directory.Build.props`** - Completed 2026-10-16
  - Description: Import the root file with `GetPathOfFileAbove`. Set `IsPackable` `true` and `GenerateDocumentationFile` `true`.
  - File(s): `src/Directory.Build.props`
  - Estimate: 3 steps

- [x] ✅ **3.4 Ignore the pack output** - Completed 2026-10-16
  - Description: Add the line `artifacts/` under the `# .NET` group of `.gitignore`.
  - File(s): `.gitignore`
  - Estimate: 2 steps

### Task Group 3B: Descriptions and Verification

- [x] ✅ **3.5 Add the package descriptions** - Completed 2026-10-16
  - Description: One `Description` per library project, from `docs/technical-design.md` section 3 (the exact strings are in task 3.5 of `implementation.md`).
  - File(s): the five `src/*/*.csproj` files
  - Estimate: 6 steps

- [x] ✅ **3.6 Verify the pack** - Completed 2026-10-16
  - Description: Delete `artifacts/packages/`, run `dotnet pack brutal-ui.sln -c Release` with 0 warnings, confirm exactly the five `0.1.0-alpha` `.nupkg` files, read the `.nuspec` of Core and Debugger, pack Core alone, re-run build and test, and confirm that `git status --porcelain` shows no `artifacts/` path.
  - File(s): none (verification)
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- none

**Fixed Bugs:**
- [x] ✅ 🐛 **CR-1: a minimized window stops the manual run with exit code 1 on Windows** - Fixed 2026-10-16
  - Fix: the frame loop waits on events while the window is minimized, and it stops when the user closes the window.
- [x] ✅ 🐛 **CR-2: NU1900 fails a full restore when nuget.org is not reachable** - Fixed 2026-10-16
  - Fix: `WarningsNotAsErrors` holds NU1900 too.

---

## Technical Debt

- [ ] 💳 **No package readme** - Priority: Low
  - Description: NuGet prints "missing a readme" for each of the five packages.
  - Reason: no public release exists yet.
  - Effort: Low. Add a readme before the first public release.
- [ ] 💳 **No validation layer run** - Priority: Low
  - Description: nobody has run `HelloPanel` under `VK_LAYER_KHRONOS_validation`. The layer is not on this machine.
  - Reason: the layer is not installed.
  - Effort: Low.
- [ ] 💳 **BRUTAL backend allocation per frame is not measured** - Priority: Medium
  - Description: `GlfwBackend.NewFrame` and `VulkanBackend.RenderDrawData` run each frame. Nobody has measured whether they allocate.
  - Reason: the allocation test does not exist yet.
  - Effort: Medium. The first feature with a frame loop in `BrutalUi.Core` adds the allocation test.
- [x] ✅ 💳 **CR-4: a magic descriptor pool size** - Resolved 2026-10-16
  - Resolution: the named constant `ImGuiDescriptorPoolSize` with a doc comment.
- [x] ✅ 💳 **CR-5: BenchmarkDotNet output not ignored** - Resolved 2026-10-16
  - Resolution: `.gitignore` ignores `BenchmarkDotNet.Artifacts/`.

---

## Progress Tracking

### Completed This Session (2026-10-16)
- ✅ Phase 1: solution, props, pins, and test projects (13 tasks)
- ✅ Phase 2: `HelloPanel` runs N frames (12 tasks)
- ✅ Phase 3: pack metadata and `dotnet pack` (6 tasks)
- ✅ Cross-phase review: CR-1, CR-2, CR-4, and CR-5 fixed, CR-3 declined with a reason

### Discovered New Tasks
- none for this feature. The `libvulkan.so` gap goes to `dev-ops/linux-dev-environment`.

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
