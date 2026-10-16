# Solution Skeleton - Implementation Plan

**Epic:** dev-ops (feature 1 of 3)
**Status:** Ready for Review
**Started:** 2026-10-16
**Target Completion:** 2026-10-16
**Last Updated:** 2026-10-16 10:53

---

## Quality Bar

**Target quality:** Production-grade build infrastructure. Every later epic builds on this skeleton. A defect here costs time in every later feature.

- **Reproducible builds:** The same commit restores the same package versions on Linux and on Windows. One package source. Exact pins. One SDK band.
- **Exact pins:** Every package version lives in `Directory.Packages.props`. No `.csproj` file holds a version.
- **No warnings:** `TreatWarningsAsErrors` is on. A clean build prints `0 Warning(s)`.
- **One place per rule:** Each shared setting lives in one props file. Each `.csproj` file holds only its name, its output type, its references, and its description.
- **Same result on both operating systems:** The build uses no shell-specific step, no absolute path, and no case-sensitive path mismatch.

---

## Executive Summary

This feature creates `brutal-ui.sln`, the shared props files, the package pins, and the eleven projects from `docs/technical-design.md` section 3. Each library project builds empty. Each test project holds one passing placeholder test. The `HelloPanel` sample opens a GLFW window, draws one ImGui text label per frame through the Vulkan backend, and exits after N frames. That sample is the GPU smoke contract for the two sibling features. The five `src/` projects pack into `.nupkg` files with MIT licence metadata at version `0.1.0-alpha`.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from the epic docs.

**Target Users:**
- [x] **The author on Windows** (pulls `main`, builds, tests, and looks at the sample window)
- [x] **The agent on Linux** (builds and tests with no display and no user input, and runs the sample under Xvfb)
- [x] **A later package consumer** (adds a `BrutalUi.*` package to a BRUTAL game)

**User Subsets Served:**
- [x] **Hands-off** - The agent runs one command for each step and reads one exit code.
- [x] **Simple** - The author runs the same `dotnet` commands on Windows with no Visual Studio.

**UX Approach:**
- The build uses only `dotnet build`, `dotnet test`, `dotnet run`, and `dotnet pack`. No other tool is necessary.
- The sample has one argument and three exit codes. A script reads the exit code only.
- A package consumer gets correct metadata: id, version, licence, description, and dependencies.

---

## Goals

### Primary Goals
1. Give every later feature a solution that builds and tests on Linux and on Windows.
2. Give the sibling features one GPU smoke sample with a fixed command line contract.
3. Make each `src/` project packable with correct metadata.

### Success Criteria
- [ ] `dotnet build brutal-ui.sln` exits 0 with 0 warnings.
- [ ] `dotnet test --solution brutal-ui.sln` exits 0 and reports 3 passed tests.
- [ ] `HelloPanel --frames 120` exits 0 on a machine with a working Vulkan driver.
- [ ] `dotnet pack brutal-ui.sln -c Release` writes exactly five `.nupkg` files.
- [ ] `BrutalUi.Core` resolves no `Ahwoo.BRUTAL.ImGui*` package, direct or transitive.

---

## Architecture Overview

The build has three layers. `global.json` and `nuget.config` fix the SDK band and the package source. `Directory.Build.props` and `Directory.Packages.props` hold every shared setting and every version. Each `.csproj` file holds only what differs.

### File Tree (end state)

```
brutal-ui/
├── .editorconfig                    # phase 1
├── .gitignore                       # existing, phase 3 adds artifacts/
├── Directory.Build.props            # phase 1, phase 3 adds pack defaults
├── Directory.Packages.props         # phase 1, phase 2 adds sample pins
├── LICENSE                          # phase 3
├── brutal-ui.sln                    # phase 1, phase 2 adds HelloPanel
├── global.json                      # phase 1
├── nuget.config                     # phase 1
├── src/
│   ├── Directory.Build.props        # phase 3 (imports the root props)
│   ├── BrutalUi.Core/BrutalUi.Core.csproj
│   ├── BrutalUi.ImGui/BrutalUi.ImGui.csproj
│   ├── BrutalUi.Framework/BrutalUi.Framework.csproj
│   ├── BrutalUi.Markup/BrutalUi.Markup.csproj
│   └── BrutalUi.Debugger/BrutalUi.Debugger.csproj
├── tests/
│   ├── BrutalUi.Core.Tests/{BrutalUi.Core.Tests.csproj, PlaceholderTests.cs}
│   ├── BrutalUi.Framework.Tests/{BrutalUi.Framework.Tests.csproj, PlaceholderTests.cs}
│   ├── BrutalUi.Markup.Tests/{BrutalUi.Markup.Tests.csproj, PlaceholderTests.cs}
│   └── BrutalUi.Benchmarks/{BrutalUi.Benchmarks.csproj, Program.cs, PlaceholderBenchmark.cs}
├── samples/
│   ├── HelloPanel/{HelloPanel.csproj, Program.cs, VulkanContext.cs}   # phase 2
│   └── WorldSpace/{WorldSpace.csproj, Program.cs}
└── artifacts/packages/              # pack output, ignored by git
```

### Project Reference Graph

```
BrutalUi.Core  <──  BrutalUi.ImGui  <──┐
      ^  ^  ^                          ├── BrutalUi.Debugger
      │  │  └── BrutalUi.Framework <───┘
      │  └───── BrutalUi.Markup
      │
tests:   Core.Tests -> Core    Framework.Tests -> Framework    Markup.Tests -> Markup
         Benchmarks -> Core
samples: WorldSpace -> Core, ImGui    HelloPanel -> Core, ImGui
```

`BrutalUi.Core` has no project reference. No arrow points from Core to ImGui. The compiler enforces the rule of `docs/technical-design.md` section 15, principle 2.

### Package Reference per Project

| Project | Output | Project references | Direct package references |
|---|---|---|---|
| `BrutalUi.Core` | Library | none | `Ahwoo.BRUTAL.Numerics`, `Ahwoo.BRUTAL.Localization` |
| `BrutalUi.ImGui` | Library | Core | `Ahwoo.BRUTAL.ImGui`, `Ahwoo.BRUTAL.ImGui.Backends` |
| `BrutalUi.Framework` | Library | Core | none |
| `BrutalUi.Markup` | Library | Core | none |
| `BrutalUi.Debugger` | Library | Framework, ImGui | none |
| `BrutalUi.Core.Tests` | Exe | Core | `xunit.v3` |
| `BrutalUi.Framework.Tests` | Exe | Framework | `xunit.v3` |
| `BrutalUi.Markup.Tests` | Exe | Markup | `xunit.v3` |
| `BrutalUi.Benchmarks` | Exe | Core | `BenchmarkDotNet` |
| `WorldSpace` | Exe | Core, ImGui | none |
| `HelloPanel` | Exe | Core, ImGui | `Ahwoo.BRUTAL.Glfw`, `Ahwoo.BRUTAL.Vulkan`, and `Ahwoo.BRUTAL.Vulkan.Abstractions` when the code uses it (task 2.1 decides) |

### Pins in `Directory.Packages.props`

| Package | Version | Phase |
|---|---|---|
| `Ahwoo.BRUTAL.Numerics` | 2026.10.1 | 1 |
| `Ahwoo.BRUTAL.Localization` | 2026.10.2 | 1 |
| `Ahwoo.BRUTAL.ImGui` | 2026.10.2 | 1 |
| `Ahwoo.BRUTAL.ImGui.Backends` | 2026.10.2 | 1 |
| `xunit.v3` | 4.0.2 | 1 |
| `BenchmarkDotNet` | 0.15.8 | 1 |
| `Ahwoo.BRUTAL.Glfw` | 2026.10.4 | 2 |
| `Ahwoo.BRUTAL.Vulkan` | 2026.10.2 | 2 |
| `Ahwoo.BRUTAL.Vulkan.Abstractions` | 2026.10.2 | 2, only when `HelloPanel` references it |

### The Shared Contract with the Sibling Features

The sibling features `dev-ops/linux-dev-environment` and `dev-ops/windows-test-path` call these commands from the repository root. This feature guarantees each one.

| Step | Command | Success |
|---|---|---|
| Build | `dotnet build brutal-ui.sln [-c <Config>]` | exit 0 |
| Test | `dotnet test --solution brutal-ui.sln [-c <Config>]` | exit 0 |
| Smoke run | `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build [-c <Config>] -- --frames <N>` | exit 0 |
| Pack | `dotnet pack brutal-ui.sln -c Release` | exit 0, five files in `artifacts/packages/` |

The test step uses `--solution`. The .NET 10 SDK runs `dotnet test` in Microsoft.Testing.Platform mode when `global.json` selects that runner. In that mode, `dotnet test` accepts no positional path. A script that uses the smoke run with `--no-build` passes the same `-c` value to the build step and to the run step.

**HelloPanel command line contract:**

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

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Item A8 of the Confirmed Assumptions fixes the three phases.

### Phase 1: Solution, Props, Pins, and Test Projects
**Goal:** Create the full solution with every project except `HelloPanel`. Prove that it restores, builds with no warning, and passes its tests.
**Demo:** the user runs `dotnet test --solution brutal-ui.sln` at the repository root and sees 3 passed tests and exit code 0.
**Needs:** none

**Size note:** This phase is large. Item A8 fixes its scope. It holds 13 tasks and about 25 small files. Most files are near-identical project files with fewer than 15 lines. The plan splits it into two task groups (1A and 1B) at a file boundary. Each group exceeds the limit of 6 files. When the delegator applies the file limit strictly, it can split further at the marked sub-boundaries. No file appears in two groups. The solution file appears in task 1.12 only.

#### Task Group 1A: Root Configuration, Libraries, and WorldSpace

Sub-boundary 1A-i: the root files. Sub-boundary 1A-ii: the project files.

**Tasks:**
- [ ] 1.1 Write `global.json`. Set `sdk.version` to `10.0.100` and `sdk.rollForward` to `latestFeature`. Set `test.runner` to `Microsoft.Testing.Platform`. Run `dotnet --version` at the root and confirm that it prints a `10.0.1xx` version (10.0.112 on this machine).
- [ ] 1.2 Write `nuget.config`. Add `<clear />` and one source, `https://api.nuget.org/v3/index.json`, with the key `nuget.org`. This stops machine-level sources from changing the restore result and from raising warning NU1507.
- [ ] 1.3 Write `Directory.Build.props` with these properties: `TargetFramework` `net10.0`, `LangVersion` `14`, `Nullable` `enable`, `AllowUnsafeBlocks` `true`, `ImplicitUsings` `disable`, `TreatWarningsAsErrors` `true`, `WarningsNotAsErrors` `NU1901;NU1902;NU1903;NU1904`, `EnforceCodeStyleInBuild` `true`, `IsPackable` `false`, `UseMicrosoftTestingPlatformRunner` `true`. Add one short comment for each group of properties. Write the comments in ASD-STE100 prose. Do not add pack metadata here. Phase 3 adds it.
- [ ] 1.4 Write `Directory.Packages.props`. Set `ManagePackageVersionsCentrally` to `true`. Add one `PackageVersion` item for each phase 1 row in the pin table: `Ahwoo.BRUTAL.Numerics` 2026.10.1, `Ahwoo.BRUTAL.Localization` 2026.10.2, `Ahwoo.BRUTAL.ImGui` 2026.10.2, `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2, `xunit.v3` 4.0.2, `BenchmarkDotNet` 0.15.8. Use the exact id spelling of nuget.org. Use plain version strings, not bracket ranges.
- [ ] 1.5 Write `.editorconfig`. Set `root = true`. For all files: `charset = utf-8`, `indent_style = space`, `indent_size = 4`, `insert_final_newline = true`, `trim_trailing_whitespace = true`. For `*.{csproj,props,targets,json,config,xml}`: `indent_size = 2`. For `*.sln`: `indent_style = tab`. For `*.cs`: `csharp_style_namespace_declarations = file_scoped:warning`, `dotnet_diagnostic.IDE0161.severity = warning`, `csharp_using_directive_placement = outside_namespace:warning`, `dotnet_sort_system_directives_first = true`, `csharp_new_line_before_open_brace = all`. Do not set `end_of_line`. The `.gitattributes` file of `dev-ops/windows-test-path` owns line endings.
- [ ] 1.6 Write `src/BrutalUi.Core/BrutalUi.Core.csproj`. Use `Sdk="Microsoft.NET.Sdk"`. Add `PackageReference` items for `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization` with no `Version` attribute. Add no project reference. Add no source file.
- [ ] 1.7 Write the four other library project files: `src/BrutalUi.ImGui/BrutalUi.ImGui.csproj`, `src/BrutalUi.Framework/BrutalUi.Framework.csproj`, `src/BrutalUi.Markup/BrutalUi.Markup.csproj`, `src/BrutalUi.Debugger/BrutalUi.Debugger.csproj`. Use the references of the package reference table. Use forward slashes in `ProjectReference` paths, and match the case of each file name exactly. Add no source file.
- [ ] 1.8 Write `samples/WorldSpace/WorldSpace.csproj` (`OutputType` `Exe`, project references to Core and ImGui) and `samples/WorldSpace/Program.cs`. `Program.cs` uses the file-scoped namespace `WorldSpace`, holds a `static class Program` with `static int Main(string[] args)`, writes the line `WorldSpace: this sample has no scene yet.` to standard output, and returns 0.
- [ ] 1.9 Run `dotnet build src/BrutalUi.Debugger/BrutalUi.Debugger.csproj` and `dotnet build samples/WorldSpace/WorldSpace.csproj` in one shell command. Filter the output for `warning` and `error`. Confirm 0 warnings and 0 errors. Run `dotnet run --project samples/WorldSpace/WorldSpace.csproj` and confirm exit code 0. Run `dotnet list src/BrutalUi.Core/BrutalUi.Core.csproj package --include-transitive` and confirm that no package id starts with `Ahwoo.BRUTAL.ImGui`.

#### Task Group 1B: Test Projects, Benchmarks, and the Solution

**Tasks:**
- [ ] 1.10 Write the three test project files: `tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj`, `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`, `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj`. Each one sets `OutputType` `Exe`, references `xunit.v3` with no version, and holds one `ProjectReference` to its project under test. Write the three placeholder test files: `tests/BrutalUi.Core.Tests/PlaceholderTests.cs`, `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs`, `tests/BrutalUi.Markup.Tests/PlaceholderTests.cs`. Each file uses a file-scoped namespace equal to the project name and holds one `public sealed class PlaceholderTests` with one `[Fact]`. The fact loads the assembly under test by name, for example `Assembly.Load(new AssemblyName("BrutalUi.Core"))`, and asserts that the result is not null. This proves that the project reference resolves. Use explicit `using` directives, because `ImplicitUsings` is off.
- [ ] 1.11 Write `tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj` (`OutputType` `Exe`, `PackageReference` `BenchmarkDotNet`, `ProjectReference` Core). Write `tests/BrutalUi.Benchmarks/Program.cs` with `static int Main(string[] args)` that calls `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)` and returns 0. Write `tests/BrutalUi.Benchmarks/PlaceholderBenchmark.cs` with one `public class PlaceholderBenchmark`, the `[MemoryDiagnoser]` attribute, and one empty `[Benchmark]` method. Do not seal the class. BenchmarkDotNet generates a subclass of it.
- [ ] 1.12 Create the solution and add every phase 1 project in one shell command: `dotnet new sln --name brutal-ui --format sln`, then `dotnet sln brutal-ui.sln add` with the five `src/` projects, the four `tests/` projects, and `samples/WorldSpace/WorldSpace.csproj`. The `--format sln` option is required. The .NET 10 SDK writes `.slnx` by default. Confirm that `dotnet sln brutal-ui.sln list` prints 10 projects.
- [ ] 1.13 Run the phase demo in one shell command: `dotnet build brutal-ui.sln`, then `dotnet test --solution brutal-ui.sln`. Print the last 30 lines. Confirm `0 Warning(s)`, `0 Error(s)`, 3 passed tests, 0 failed tests, and exit code 0. Confirm that `dotnet test` does not run `BrutalUi.Benchmarks` or `WorldSpace`. Run `git status --porcelain` and confirm that no `bin/` or `obj/` path appears.

**Estimated Steps:** about 78 (13 tasks, about six steps for one task)

**Acceptance Criteria:**
- [ ] `dotnet build brutal-ui.sln` exits 0 with 0 warnings and 0 errors.
- [ ] `dotnet test --solution brutal-ui.sln` exits 0 and reports 3 passed tests.
- [ ] `dotnet sln brutal-ui.sln list` prints 10 projects.
- [ ] `brutal-ui.sln` starts with the classic header `Microsoft Visual Studio Solution File, Format Version 12.00`.
- [ ] No `.csproj` file holds `TargetFramework`, `LangVersion`, `Nullable`, `AllowUnsafeBlocks`, or a `Version` attribute on a `PackageReference`.
- [ ] `BrutalUi.Core.csproj` holds no `ProjectReference` and no ImGui package.
- [ ] `dotnet run --project samples/WorldSpace/WorldSpace.csproj` prints one line and exits 0.

---

### Phase 2: HelloPanel Runs N Frames
**Goal:** Add the `HelloPanel` sample. It opens a GLFW window, draws one ImGui text label per frame through the Vulkan backend, and exits with the codes of the contract.
**Demo:** the user runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120`, sees a window with the text `Hello, brutal-ui`, and the process exits with code 0.
**Needs:** Phase 1
**Dev agent:** advanced (opus)

**Reason for the advanced agent:** The API of `Ahwoo.BRUTAL.ImGui.Backends`, `Ahwoo.BRUTAL.Glfw`, and `Ahwoo.BRUTAL.Vulkan` is unknown to this plan. The sample also needs a full Vulkan setup: instance, surface, device, swapchain, render pass, framebuffers, command buffers, and synchronization. A mistake there shows up as a driver hang or a validation error, not as a compiler error.

**GPU note:** A GPU run on this Linux machine needs a reboot first. The NVIDIA kernel module is 595.91 and the user space library is 595.99. The agent has no `DISPLAY`. When the GPU is not ready, the verifier of this phase returns `Demo: MANUAL`. The author then runs the demo on Windows or on the Linux desktop after the reboot. The exit code 2 checks of task 2.12 need no GPU. The verifier runs them in all cases.

**Tasks:**
- [ ] 2.1 Read the package API before you write any sample code. Phase 1 restored `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2 into `~/.nuget/packages/ahwoo.brutal.imgui.backends/2026.10.2/lib/net10.0/`. Read the XML doc file next to each DLL, when it exists. If the docs are not sufficient, install `ilspycmd` with `dotnet tool install --tool-path <scratchpad>` (not in the repository) and decompile the public surface of `Brutal.ImGui.Backends`, `Brutal.Glfw`, `Brutal.Vulkan`, and `Brutal.Vulkan.Abstractions`. Find these items: the constructor and the create-info of `GlfwBackend` and `VulkanBackend`, the per-frame calls (`NewFrame`, `RenderDrawData`), the GLFW window creation and the Vulkan surface creation, the Vulkan instance and device helpers, and the form of the ImGui text call (UTF-8 span, `ImString`, or other). Also look for a BRUTAL example that sets up GLFW, Vulkan, and ImGui together, and follow it when it exists.
- [ ] 2.2 Write the findings of task 2.1 into `docs/features/dev-ops/solution-skeleton/context.md`: the API calls that the sample uses, and every native library that the sample loads (the GLFW binary from `Ahwoo.BRUTAL.Glfw`, the ImGui binary from `Ahwoo.BRUTAL.ImGui`, and the system Vulkan loader: `libvulkan.so.1` on Linux, `vulkan-1.dll` on Windows). `AGENTS.md` requires this note for each native dependency. Also copy the HelloPanel command line contract of this plan into `context.md`.
- [ ] 2.3 Add the pins for the packages that `HelloPanel` references directly to `Directory.Packages.props`: `Ahwoo.BRUTAL.Glfw` 2026.10.4, `Ahwoo.BRUTAL.Vulkan` 2026.10.2, and `Ahwoo.BRUTAL.Vulkan.Abstractions` 2026.10.2 only when task 2.1 shows that the sample code uses it.
- [ ] 2.4 Write `samples/HelloPanel/HelloPanel.csproj`: `OutputType` `Exe`, project references to Core and ImGui, and the package references of task 2.3 with no version.
- [ ] 2.5 Write the argument parser in `samples/HelloPanel/Program.cs`. `static int Main(string[] args)` parses `--frames <N>` per the contract before any GLFW call. On a usage error, it writes `Usage: HelloPanel [--frames <N>]` and the reason to standard error and returns 2.
- [ ] 2.6 Write the window setup in `samples/HelloPanel/Program.cs`. Start GLFW, set the no-client-API hint for Vulkan, make the window not resizable, and open a 1280 x 720 window with the title `HelloPanel`. On a failure, write the error to standard error and return 1.
- [ ] 2.7 Write the Vulkan device setup in `samples/HelloPanel/VulkanContext.cs`: the instance with the extensions that GLFW requires, the surface, the physical device with a graphics queue and a present queue, and the logical device. Use the helpers of `Brutal.Vulkan.Abstractions` when they exist. Use a `required` create-info struct for the `VulkanContext` constructor, per the BRUTAL style.
- [ ] 2.8 Write the frame resources in `samples/HelloPanel/VulkanContext.cs`: the swapchain, the render pass, the framebuffers, the command pool, the command buffers, and the semaphores and fences. Keep all swapchain code inside `VulkanContext`. A later change can then add an offscreen target with no change to `Program.cs`. Do not write swapchain re-creation. The window is not resizable. On `VK_ERROR_OUT_OF_DATE_KHR`, throw an exception, so the sample exits 1.
- [ ] 2.9 Write the ImGui setup in `samples/HelloPanel/Program.cs`: create the ImGui context, the `GlfwBackend`, and the `VulkanBackend` with the render pass of `VulkanContext`. Use the default font.
- [ ] 2.10 Write the frame loop and the teardown in `samples/HelloPanel/Program.cs`. Each pass polls events, calls the `NewFrame` methods of both backends, starts the ImGui frame, opens one ImGui window, draws the text `Hello, brutal-ui` from a UTF-8 literal, renders, records the command buffer, calls `RenderDrawData`, submits, and presents. The loop stops at `N` frames or when the window closes. Do not allocate managed memory in the loop. After the loop, wait for the device to go idle, dispose every object in reverse order, end GLFW, write `HelloPanel: <count> frames rendered.` to standard output, and return 0. Catch every exception at the top of `Main`, write it to standard error, and return 1.
- [ ] 2.11 Add the sample to the solution: `dotnet sln brutal-ui.sln add samples/HelloPanel/HelloPanel.csproj`. Confirm that `dotnet sln brutal-ui.sln list` prints 11 projects.
- [ ] 2.12 Verify in one shell command. Run `dotnet build brutal-ui.sln` and confirm 0 warnings. Run `dotnet test --solution brutal-ui.sln` and confirm 3 passed tests. Run the sample with `--frames abc`, with `--frames 0`, and with `--bogus`, and confirm exit code 2 for each. These need no GPU. When a GPU and a display are ready, run `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120` and confirm exit code 0 and the output line. When the GPU is not ready, record `Demo: MANUAL` and the reason in `context.md`.

**Estimated Steps:** about 90 (12 tasks, about six steps for one task, plus extra steps for the API reading of task 2.1)

**Files (6):** `samples/HelloPanel/HelloPanel.csproj`, `samples/HelloPanel/Program.cs`, `samples/HelloPanel/VulkanContext.cs`, `Directory.Packages.props`, `brutal-ui.sln`, `docs/features/dev-ops/solution-skeleton/context.md`

**Acceptance Criteria:**
- [ ] `dotnet build brutal-ui.sln` exits 0 with 0 warnings.
- [ ] `HelloPanel --frames abc`, `--frames 0`, and `--bogus` each exit with code 2 and write a usage line to standard error.
- [ ] `HelloPanel --frames 120` exits 0 and prints `HelloPanel: 120 frames rendered.` on a machine with a working Vulkan driver and a display (MANUAL when the GPU is not ready).
- [ ] The window shows the text `Hello, brutal-ui` (MANUAL, a person looks at it).
- [ ] `context.md` lists each native library that the sample loads and the command line contract.
- [ ] `Directory.Packages.props` pins each package that `HelloPanel.csproj` references directly.

---

### Phase 3: Pack Metadata and `dotnet pack`
**Goal:** Make the five `src/` projects packable with correct metadata. Keep the tests and the samples out of the pack output.
**Demo:** the user runs `dotnet pack brutal-ui.sln -c Release` and sees exactly five `.nupkg` files in `artifacts/packages/`.
**Needs:** Phase 1

#### Task Group 3A: Shared Pack Settings

**Tasks:**
- [ ] 3.1 Write `LICENSE` at the repository root with the standard MIT licence text and the line `Copyright (c) 2026 Aaron Static`.
- [ ] 3.2 Edit `Directory.Build.props`. Add the pack defaults: `Version` `0.1.0-alpha`, `Authors` `Aaron Static`, `Copyright` `Copyright (c) 2026 Aaron Static`, `PackageLicenseExpression` `MIT`, `RepositoryUrl` `https://github.com/aaronstatic/brutal-ui`, `RepositoryType` `git`, `PackageOutputPath` `$(MSBuildThisFileDirectory)artifacts/packages/`. Keep `IsPackable` `false` as the default.
- [ ] 3.3 Write `src/Directory.Build.props`. Import the root file with `<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`. Set `IsPackable` `true` and `GenerateDocumentationFile` `true`. With `TreatWarningsAsErrors`, a later public member with no XML doc comment then breaks the build. This enforces principle 4 of `docs/technical-design.md` section 15.
- [ ] 3.4 Edit `.gitignore`. Add the line `artifacts/` under the `# .NET` group.

#### Task Group 3B: Descriptions and Verification

**Tasks:**
- [ ] 3.5 Edit the five library project files. Add one `Description` to each, from `docs/technical-design.md` section 3. `BrutalUi.Core`: "Element tree, layout, style, input, and the renderer interface for brutal-ui. No ImGui reference." `BrutalUi.ImGui`: "The brutal-ui renderer on the ImGui draw list, with fonts and textures." `BrutalUi.Framework`: "Stores, panels, modes, windows, commands, and events for brutal-ui." `BrutalUi.Markup`: "The XML element loader and the stylesheet parser for brutal-ui." `BrutalUi.Debugger`: "The state, composition, and event inspector window for brutal-ui."
- [ ] 3.6 Verify in one shell command. Delete `artifacts/packages/` when it exists. Run `dotnet pack brutal-ui.sln -c Release` and confirm 0 warnings. List `artifacts/packages/` and confirm exactly these five files: `BrutalUi.Core.0.1.0-alpha.nupkg`, `BrutalUi.ImGui.0.1.0-alpha.nupkg`, `BrutalUi.Framework.0.1.0-alpha.nupkg`, `BrutalUi.Markup.0.1.0-alpha.nupkg`, `BrutalUi.Debugger.0.1.0-alpha.nupkg`. Print the `.nuspec` of `BrutalUi.Core` and of `BrutalUi.Debugger` with `unzip -p <file> '*.nuspec'`. Confirm the licence expression, the version, the authors, and the dependencies. Run `dotnet pack src/BrutalUi.Core/BrutalUi.Core.csproj -c Release` and confirm exit code 0. Run `dotnet build brutal-ui.sln` and `dotnet test --solution brutal-ui.sln` again, and confirm no regression. Run `git status --porcelain` and confirm that no `artifacts/` path appears.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Acceptance Criteria:**
- [ ] `dotnet pack brutal-ui.sln -c Release` exits 0 with 0 warnings.
- [ ] `artifacts/packages/` holds exactly the five named `.nupkg` files and no test or sample package.
- [ ] Each `.nuspec` holds `<license type="expression">MIT</license>`, version `0.1.0-alpha`, authors `Aaron Static`, and a non-empty description.
- [ ] The `BrutalUi.Core` `.nuspec` lists exactly two dependencies: `Ahwoo.BRUTAL.Numerics` 2026.10.1 and `Ahwoo.BRUTAL.Localization` 2026.10.2.
- [ ] The `BrutalUi.Debugger` `.nuspec` lists `BrutalUi.Framework` 0.1.0-alpha and `BrutalUi.ImGui` 0.1.0-alpha.
- [ ] `LICENSE` holds the MIT text and `Copyright (c) 2026 Aaron Static`.
- [ ] `git status --porcelain` shows no `artifacts/` path after a pack.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Package ids and version: `BrutalUi.*` at version `0.1.0-alpha`.
- Q2 Licence: MIT, with a LICENSE file at the root and the SPDX expression in the pack metadata.
- Q3 xUnit line: test projects pin `xunit.v3` with Microsoft.Testing.Platform.
- Q4 Demo of phase 1: `dotnet test` passes on the full solution.
- A1 The .NET SDK 10.0.112 exists at `/usr/bin/dotnet` today, so this feature builds now. The SDK install of `linux-dev-environment` becomes a check only.
- A2 `global.json` pins `10.0.100` with `rollForward: latestFeature`.
- A3 `BrutalUi.Debugger` references `BrutalUi.Framework` and `BrutalUi.ImGui`. `BrutalUi.Benchmarks` references `BrutalUi.Core` only.
- A4 The plan uses the exact package id spelling from nuget.org. The docs disagree today: `Ahwoo.Brutal.*` in the technical design, `Ahwoo.BRUTAL.*` in the epic docs.
- A5 `WorldSpace` is a console app that references `BrutalUi.Core` and `BrutalUi.ImGui`, prints one line, and exits 0. No window.
- A6 `HelloPanel` draws one ImGui text label per frame, because no brutal-ui element exists yet. It uses the GLFW backend and the Vulkan backend.
- A7 `BrutalUi.Benchmarks` holds one empty BenchmarkDotNet benchmark. `dotnet test` skips it because it is not a test project.
- A8 The phases are: 1 solution, props, pins, and test projects. 2 `HelloPanel` runs N frames. 3 pack metadata and `dotnet pack`.

### Decision 1: Hand-written project files with shared props
**Context:** Eleven projects share the same target, language, and analysis settings.
**Decision:** Each `.csproj` holds only its SDK, its output type, its references, and (in `src/`) its description. `Directory.Build.props` holds every shared setting. `src/Directory.Build.props` imports the root file and adds the package-only settings.
**Rationale:** Small files, one place per rule. This matches `docs/technical-design.md` section 3.
**Alternatives Considered:** `dotnet new` templates, then a trim. Rejected: the templates leave settings that the props already set, and the trim is churn.

### Decision 2: Classic `.sln` format
**Context:** The requirements name `brutal-ui.sln`. The .NET 10 SDK writes `.slnx` by default.
**Decision:** Create the solution with `dotnet new sln --name brutal-ui --format sln`.
**Rationale:** The sibling scripts call `brutal-ui.sln`. The classic format also works in older tools on Windows.
**Alternatives Considered:** `.slnx`. Rejected: it breaks the required file name.

### Decision 3: Central package management with plain version strings
**Context:** `AGENTS.md` and the epic require one file that pins every `Ahwoo.BRUTAL.*` version.
**Decision:** `Directory.Packages.props` sets `ManagePackageVersionsCentrally` and holds one `PackageVersion` item for each package that a project references directly. The versions come from the Research Findings (nuget.org search and registration APIs, https://api.nuget.org/v3/, verified on 2026-10-16). The file uses plain version strings such as `2026.10.2`. Transitive pinning stays off.
**Rationale:** NuGet picks the lowest version that satisfies a range. A plain version string therefore resolves to that exact version when it exists on the feed. A bracket range such as `[2026.10.2]` adds downgrade errors and gives no extra safety. The transitive `Ahwoo.BRUTAL.*` packages (Core, Logging, Memory, Strings, OpenGl) resolve to the exact minimum versions that the direct packages declare. The Research Findings direct this rule: pin a transitive package only when a project references it directly.
**Alternatives Considered:** Lock files (`packages.lock.json`). Rejected for now: they add eleven generated files, and the plain pins already give one result. Pins for every package in the technical design stack (`Ahwoo.BRUTAL.ImGui.FreeType`, `Ahwoo.BRUTAL.Texture`). Rejected: no project references them yet (YAGNI). The feature that first references a package adds its pin.

### Decision 4: Package id spelling `Ahwoo.BRUTAL.*`
**Context:** `docs/technical-design.md` and `AGENTS.md` write `Ahwoo.Brutal.*`. nuget.org lists `Ahwoo.BRUTAL.*`.
**Decision:** Every project file and `Directory.Packages.props` use `Ahwoo.BRUTAL.*`. The code namespaces stay `Brutal.*`.
**Rationale:** Item A4. NuGet ids are not case-sensitive, so both spellings restore. The nuget.org spelling avoids a mismatch in lock tools and in package listings.

### Decision 5: xunit.v3 on Microsoft.Testing.Platform, selected in `global.json`
**Context:** Item Q3 selects xunit.v3 with Microsoft.Testing.Platform. The xUnit.net 4.0.0 release notes state that Microsoft.Testing.Platform v2 is the default and that support for v1 ends. On the .NET 10 SDK, `dotnet test` uses that platform only when `global.json` selects it.
**Decision:** `global.json` holds `"test": { "runner": "Microsoft.Testing.Platform" }`. `Directory.Build.props` sets `UseMicrosoftTestingPlatformRunner` to `true`. Test projects are `Exe` projects and reference `xunit.v3` only. The plan does not reference `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or `Microsoft.Testing.Platform` directly, and it does not pin them.
**Rationale:** `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` serve the VSTest runner only. `xunit.v3` brings the platform version that it needs. A direct pin of `Microsoft.Testing.Platform` 2.5.1 can conflict with the version that `xunit.v3` 4.0.2 needs. When restore shows that a direct reference is necessary, the dev agent adds the pin at 2.5.1 and records the reason in `context.md`.
**Consequence:** The test command is `dotnet test --solution brutal-ui.sln`. In this mode `dotnet test` accepts no positional path. The sibling scripts use this exact form.
**Sources:** https://xunit.net/releases/v3/4.0.0, https://xunit.net/docs/getting-started/v3/microsoft-testing-platform, https://learn.microsoft.com/dotnet/core/testing/migrating-vstest-microsoft-testing-platform

### Decision 6: Warnings are errors, with one exception
**Context:** The quality bar requires a build with no warnings.
**Decision:** `TreatWarningsAsErrors` is `true`. `WarningsNotAsErrors` holds `NU1901;NU1902;NU1903;NU1904`.
**Rationale:** A new advisory on nuget.org must not break the build of an unchanged commit. The advisory still prints as a warning, and a person sees it.

### Decision 7: A repository `nuget.config` with one source
**Context:** The Windows machine and the Linux machine can have different user-level NuGet sources.
**Decision:** `nuget.config` clears every source and adds nuget.org only.
**Rationale:** The same commit restores from the same source on both machines. Central package management also raises warning NU1507 when more than one source exists. With `TreatWarningsAsErrors`, that warning breaks the build.

### Decision 8: Package-only settings in `src/Directory.Build.props`
**Context:** `GenerateDocumentationFile` and `IsPackable` must apply to the five libraries only. A root props file cannot read a property that the project file sets later.
**Decision:** The root file sets `IsPackable` to `false` and holds the pack metadata defaults (version, authors, licence, repository). `src/Directory.Build.props` imports the root file and sets `IsPackable` and `GenerateDocumentationFile` to `true`. Each library project sets its own `Description`.
**Rationale:** The requirement places the pack defaults in `Directory.Build.props`. The nested file keeps the package-only switches in one place. `dotnet pack brutal-ui.sln` then skips the tests and the samples.
**Alternatives Considered:** A path condition in the root file. Rejected: it depends on path separators and is harder to read.

### Decision 9: The HelloPanel smoke contract
**Context:** Both sibling script sets run the same sample and read its exit code.
**Decision:** The contract table in the Architecture Overview: `--frames <N>`, no argument means run until the window closes, exit codes 0, 1, and 2.
**Rationale:** Exit code 2 separates a script error from a GPU failure. The argument check runs before GLFW starts, so a script can test its own call with no GPU. The no-argument mode serves the manual look in the Windows runbook.

### Decision 10: Explicit usings
**Context:** The SDK can add global usings.
**Decision:** `ImplicitUsings` is `disable`.
**Rationale:** Each file shows its dependencies. This helps the rule that `BrutalUi.Core` uses no ImGui namespace.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| The API of `GlfwBackend`, `VulkanBackend`, and the Vulkan helpers is unknown. The sample code can take many attempts. | High | High | Task 2.1 reads the API first and task 2.2 records it. Phase 2 uses the advanced dev agent. |
| The GPU on this Linux machine does not work before a reboot (kernel module 595.91, user space 595.99). | High | Medium | The verifier returns `Demo: MANUAL` for the GPU run. The exit code 2 checks need no GPU. The author runs the GPU demo on Windows or after the reboot. |
| The NVIDIA Vulkan driver reports no present support for an Xvfb surface. Then the Linux smoke run of the sibling feature fails even with a healthy GPU. | Medium | High | `VulkanContext` holds all swapchain code, so a later change can add an offscreen target. `dev-ops/linux-dev-environment` tests this first and opens a follow-up when it fails. This feature does not build an offscreen mode (YAGNI). |
| `dotnet test` in Microsoft.Testing.Platform mode fails on a solution that holds non-test projects (`BrutalUi.Benchmarks`, the samples). | Low | Medium | Task 1.13 confirms the behaviour. When it fails, set `IsTestProject` `false` in the non-test projects and record it in `context.md`. |
| `xunit.v3` 4.0.2 needs a direct `Microsoft.Testing.Platform` reference, or the `UseMicrosoftTestingPlatformRunner` property has no effect in 4.x. | Low | Low | Task 1.13 shows the result. Add the pin at 2.5.1 only when necessary, and record the reason. |
| An analyzer warning in an empty project, in BenchmarkDotNet, or in xUnit breaks the build because warnings are errors. | Medium | Low | Fix the cause. Do not add a `NoWarn`. When a suppression is unavoidable, scope it to one project and record the reason in `context.md`. |
| `dotnet new sln` writes `.slnx` on the .NET 10 SDK. | High | Medium | Task 1.12 passes `--format sln` and checks the file header. |
| A path case mismatch builds on Windows and fails on Linux. | Medium | Medium | Task 1.7 requires an exact case match. Phase 1 builds on Linux, which is case-sensitive. |
| The GLFW native binary of `Ahwoo.BRUTAL.Glfw` is missing for `win-x64` or `linux-x64`. | Low | High | Task 2.1 lists the `runtimes/` folders of the package. Task 2.2 records them. The Windows run of `dev-ops/windows-test-path` confirms the Windows binary. |
| The solution file gets mixed line endings between the two machines. | Medium | Low | `dev-ops/windows-test-path` owns `.gitattributes`. This feature does not set `end_of_line`. |

---

## Dependencies

### Internal Dependencies
- None. This feature is number 1 of 3 in the dev-ops epic. `dev-ops/linux-dev-environment` and `dev-ops/windows-test-path` depend on it.

### External Dependencies
- The .NET SDK 10.0.112 at `/usr/bin/dotnet` (item A1).
- nuget.org packages, verified on 2026-10-16 through https://api.nuget.org/v3/:
  - `Ahwoo.BRUTAL.Numerics` 2026.10.1, `Ahwoo.BRUTAL.Localization` 2026.10.2, `Ahwoo.BRUTAL.ImGui` 2026.10.2, `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2, `Ahwoo.BRUTAL.Glfw` 2026.10.4, `Ahwoo.BRUTAL.Vulkan` 2026.10.2, `Ahwoo.BRUTAL.Vulkan.Abstractions` 2026.10.2. Every package targets `net10.0` only.
  - `xunit.v3` 4.0.2, `BenchmarkDotNet` 0.15.8.
- Network access to nuget.org for the first restore.
- For the phase 2 GPU demo only: a Vulkan driver, a display, and (on this Linux machine) a reboot.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs them from the repository root. "MANUAL" marks a criterion that needs a GPU, a display, or the Windows machine.

### Functional Criteria
- [ ] F1. `dotnet --version` at the root prints a version that starts with `10.0.`.
- [ ] F2. `dotnet build brutal-ui.sln` exits 0. The output holds `0 Warning(s)` and `0 Error(s)`.
- [ ] F3. `dotnet sln brutal-ui.sln list` prints exactly these 11 projects: the five `src/` projects, the four `tests/` projects, `samples/HelloPanel`, and `samples/WorldSpace`.
- [ ] F4. `dotnet test --solution brutal-ui.sln` exits 0 and reports 3 passed tests, 0 failed tests, and 0 skipped tests, one test from each of `BrutalUi.Core.Tests`, `BrutalUi.Framework.Tests`, and `BrutalUi.Markup.Tests`.
- [ ] F5. `dotnet run --project samples/WorldSpace/WorldSpace.csproj` prints one line and exits 0. It opens no window.
- [ ] F6. `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames abc` exits 2 and writes a line that starts with `Usage:` to standard error. The same applies to `--frames 0`, to `--frames` with no value, and to `--bogus`.
- [ ] F7. (MANUAL) `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120` shows a window with the text `Hello, brutal-ui`, prints `HelloPanel: 120 frames rendered.`, and exits 0.
- [ ] F8. (MANUAL) `dotnet run --project samples/HelloPanel/HelloPanel.csproj` with no argument shows the window until the user closes it, then exits 0.
- [ ] F9. `dotnet pack brutal-ui.sln -c Release` exits 0 with 0 warnings. `artifacts/packages/` then holds exactly five files: `BrutalUi.Core.0.1.0-alpha.nupkg`, `BrutalUi.ImGui.0.1.0-alpha.nupkg`, `BrutalUi.Framework.0.1.0-alpha.nupkg`, `BrutalUi.Markup.0.1.0-alpha.nupkg`, `BrutalUi.Debugger.0.1.0-alpha.nupkg`.
- [ ] F10. `dotnet pack src/<Project>/<Project>.csproj -c Release` exits 0 for each of the five `src/` projects.

### Quality Criteria
- [ ] Q1. `global.json` holds `"version": "10.0.100"`, `"rollForward": "latestFeature"`, and `"runner": "Microsoft.Testing.Platform"`.
- [ ] Q2. `Directory.Build.props` sets `net10.0`, `LangVersion` 14, `Nullable` `enable`, `AllowUnsafeBlocks` `true`, `TreatWarningsAsErrors` `true`, and the pack defaults (`Version` `0.1.0-alpha`, `Authors`, `PackageLicenseExpression` `MIT`, `RepositoryUrl`).
- [ ] Q3. `Directory.Packages.props` sets `ManagePackageVersionsCentrally` to `true`. Each `PackageVersion` matches the pin table of this plan exactly.
- [ ] Q4. A search for `Version=` on `PackageReference` lines in `**/*.csproj` finds no match. A search for `TargetFramework`, `LangVersion`, `Nullable`, and `AllowUnsafeBlocks` in `**/*.csproj` finds no match.
- [ ] Q5. `src/BrutalUi.Core/BrutalUi.Core.csproj` holds no `ProjectReference` and references only `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization`.
- [ ] Q6. `dotnet list src/BrutalUi.Core/BrutalUi.Core.csproj package --include-transitive` lists no package id that starts with `Ahwoo.BRUTAL.ImGui`.
- [ ] Q7. `dotnet list brutal-ui.sln package --include-transitive` shows every `Ahwoo.BRUTAL.*` package at a `2026.10.x` version.
- [ ] Q8. Every `.cs` file in the repository uses a file-scoped namespace. `.editorconfig` holds `csharp_style_namespace_declarations = file_scoped:warning`.
- [ ] Q9. `LICENSE` holds the MIT licence text and `Copyright (c) 2026 Aaron Static`.
- [ ] Q10. The `BrutalUi.Core` `.nuspec` holds `<license type="expression">MIT</license>`, `<version>0.1.0-alpha</version>`, and exactly two dependencies: `Ahwoo.BRUTAL.Numerics` 2026.10.1 and `Ahwoo.BRUTAL.Localization` 2026.10.2.
- [ ] Q11. After build, test, and pack, `git status --porcelain` shows no path under `bin/`, `obj/`, or `artifacts/`.
- [ ] Q12. The feature adds no `.gitattributes` file and no file under `scripts/`.
- [ ] Q13. `docs/features/dev-ops/solution-skeleton/context.md` lists each native library that `HelloPanel` loads.

### Integration Criteria
- [ ] I1. `brutal-ui.sln` sits at the repository root. Its first non-empty line is `Microsoft Visual Studio Solution File, Format Version 12.00`.
- [ ] I2. The commands of the shared contract table work from the repository root with no other setup: `dotnet build brutal-ui.sln`, `dotnet test --solution brutal-ui.sln`, `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames <N>`.
- [ ] I3. `dotnet build brutal-ui.sln -c Release` followed by `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -c Release -- --frames abc` exits 2. This proves the `--no-build` form that the sibling scripts use.
- [ ] I4. `context.md` holds the HelloPanel command line contract, so the sibling features read one source.
- [ ] I5. (MANUAL, the author on Windows) `dotnet build brutal-ui.sln` and `dotnet test --solution brutal-ui.sln` give the same result as on Linux. This check can run with the first run of `dev-ops/windows-test-path`.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui`. Filter long output to the last 30 lines.

1. `dotnet --version`. Expect `10.0.1xx`.
2. `dotnet sln brutal-ui.sln list`. Expect 11 project paths.
3. `head -c 200 brutal-ui.sln`. Expect the classic header (I1).
4. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, and exit 0 (F2).
5. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 3 passed tests, 0 failed tests, and exit 0 (F4).
6. `dotnet run --project samples/WorldSpace/WorldSpace.csproj; echo "exit=$?"`. Expect one line and `exit=0` (F5).
7. `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames abc; echo "exit=$?"`. Expect a `Usage:` line and `exit=2`. Repeat with `--frames 0`, `--frames`, and `--bogus` (F6).
8. `dotnet build brutal-ui.sln -c Release && dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -c Release -- --frames abc; echo "exit=$?"`. Expect `exit=2` (I3).
9. `dotnet list src/BrutalUi.Core/BrutalUi.Core.csproj package --include-transitive`. Expect no `Ahwoo.BRUTAL.ImGui` line (Q6).
10. `dotnet list brutal-ui.sln package --include-transitive`. Expect each `Ahwoo.BRUTAL.*` line at `2026.10.x` (Q7).
11. Search `**/*.csproj` for `Version=`, `TargetFramework`, `LangVersion`, `Nullable`, and `AllowUnsafeBlocks`. Expect no match (Q4).
12. Read `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, and `LICENSE`. Compare them with Q1, Q2, Q3, Q8, and Q9.
13. `rm -rf artifacts/packages && dotnet pack brutal-ui.sln -c Release 2>&1 | tail -n 30 && ls artifacts/packages`. Expect exit 0, 0 warnings, and the five file names of F9.
14. `unzip -p artifacts/packages/BrutalUi.Core.0.1.0-alpha.nupkg '*.nuspec'`. Compare it with Q10. Repeat for `BrutalUi.Debugger` and expect dependencies on `BrutalUi.Framework` and `BrutalUi.ImGui` at `0.1.0-alpha`.
15. `git status --porcelain`. Expect no `bin/`, `obj/`, or `artifacts/` path (Q11).
16. Read `docs/features/dev-ops/solution-skeleton/context.md`. Confirm the native library list and the contract (Q13, I4).
17. (MANUAL) On a machine with a display and a working Vulkan driver: `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"`. Expect the window with `Hello, brutal-ui`, the line `HelloPanel: 120 frames rendered.`, and `exit=0` (F7). On this Linux machine, do this step only after the reboot. Otherwise report `Demo: MANUAL`.
18. (Optional) Add a file with a block-scoped namespace to `src/BrutalUi.Core/`, run `dotnet build src/BrutalUi.Core/BrutalUi.Core.csproj`, and expect error IDE0161. Delete the file after the check.

---

## Testing Strategy

### Unit Tests
- Three placeholder facts, one in each test project. Each fact loads the assembly under test by name. This proves the project reference and the test runner. Later features replace them with real tests.

### Integration Tests
- The solution build is the main integration test. It proves the props, the pins, the project graph, and the analyzer rules together.
- `dotnet list package --include-transitive` on `BrutalUi.Core` proves the no-ImGui rule at package level. The project graph proves it at compile level.
- `dotnet pack` and a read of the `.nuspec` prove the pack metadata and the dependency graph that a consumer sees.

### Manual Testing
- The `HelloPanel` GPU run (F7, F8). Checklist: the window opens, the title is `HelloPanel`, the text `Hello, brutal-ui` shows in an ImGui window, the process exits with code 0 after `N` frames.
- The Windows build and test run (I5). It runs with the first run of `dev-ops/windows-test-path`.

### Not Tested Here
- The Linux script under Xvfb and the GPU health check. `dev-ops/linux-dev-environment` owns them.
- The Windows scripts and the runbook. `dev-ops/windows-test-path` owns them.
- The allocation guard. The first feature with a frame loop in `BrutalUi.Core` adds it.

---

## Related Documentation

- **Requirements:** `docs/features/dev-ops/solution-skeleton/requirements.md`
- **Epic:** `docs/features/dev-ops/epic-overview.md`, `docs/features/dev-ops/epic-requirements.md`
- **Siblings:** `docs/features/dev-ops/linux-dev-environment/requirements.md`, `docs/features/dev-ops/windows-test-path/requirements.md`
- **Design:** `docs/technical-design.md` sections 1, 3, and 15. `docs/feasibility.md`, section "Package layout".
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **External:** https://api.nuget.org/v3/ (pins), https://xunit.net/releases/v3/4.0.0, https://xunit.net/docs/getting-started/v3/microsoft-testing-platform, https://learn.microsoft.com/dotnet/core/testing/migrating-vstest-microsoft-testing-platform

---

## Notes

1. **Phase 1 size.** Item A8 fixes the scope of phase 1. It holds 13 tasks in two task groups, but about 25 files. That exceeds the limit of 6 files for each group in section 1 of `.beast-mode/phase-handoff.md`. The files are near-identical and short. The delegator can split groups 1A and 1B further at the marked sub-boundaries. Task 1.12 is the only task that touches `brutal-ui.sln` in phase 1.
2. **Contract for the sibling scripts.** The siblings must call `dotnet test --solution brutal-ui.sln`, not `dotnet test brutal-ui.sln`. The positional form fails in Microsoft.Testing.Platform mode. The epic docs say "dotnet test on brutal-ui.sln". The plans of `dev-ops/linux-dev-environment` and `dev-ops/windows-test-path` must use the `--solution` form.
3. **SDK install conflict (item A1).** `requirements.md` says that `linux-dev-environment` installs the SDK before the first build. Item A1 says the SDK exists today. This plan follows item A1. The sibling plan turns the install into a check.
4. **Package id spelling (item A4).** `AGENTS.md` and `docs/technical-design.md` write `Ahwoo.Brutal.*`. This plan writes `Ahwoo.BRUTAL.*` per nuget.org. NuGet ids are not case-sensitive, so both restore. A later docs change can align the spelling. This feature does not edit those docs.
5. **"Pin every package" scope.** The requirement says that `Directory.Packages.props` pins every `Ahwoo.BRUTAL.*` package. This plan reads it as every package that a project references directly. The Research Findings state the same rule. `Ahwoo.BRUTAL.ImGui.FreeType` and `Ahwoo.BRUTAL.Texture` get pins in the feature that first references them.
6. **Research lists unused test packages.** The Research Findings list `Microsoft.Testing.Platform` 2.5.1, `xunit.runner.visualstudio` 4.0.1, and `Microsoft.NET.Test.Sdk` 18.11.0. Decision 5 explains why the plan does not reference them.
7. **BRUTAL style rules.** No BRUTAL `.editorconfig` is available to this plan. Task 1.5 sets file-scoped namespaces at warning level, plus a short set of layout rules. If the author supplies the BRUTAL `.editorconfig`, a later change aligns the rules. Keep new rules at `suggestion` level until each one is checked against the existing code.
8. **Xvfb presentation risk.** The NVIDIA Vulkan driver can refuse to present to an Xvfb window. This risk belongs to `dev-ops/linux-dev-environment`, but it can force an offscreen mode in `HelloPanel`. The main thread can run a short research task on "NVIDIA Vulkan present support on Xvfb" before the sibling plan.
9. **No user personas file.** `docs/user-personas.md` does not exist. The personas of this plan come from the epic docs.
10. **Repository URL.** `RepositoryUrl` uses the git remote `https://github.com/aaronstatic/brutal-ui`. The repository is private. No publish step exists.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
