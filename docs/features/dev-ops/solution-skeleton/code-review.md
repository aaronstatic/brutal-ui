# dev-ops/solution-skeleton - Code Review

**Reviewed:** 2026-10-16
**Reviewer:** Agent (project rules review: AGENTS.md, technical design section 15, plan quality bar)
**Skills Applied:** none. The Vercel React rules of `/review-feature` do not apply to a C# solution.
**Status:** Resolved - 5 findings, 5 resolved (4 fixed, 1 won't fix with a reason). Verifier `review-verify.md`: PASS.

---

## Summary

The skeleton meets the plan. `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives 3 passed and 0 failed. The HelloPanel parser returns exit code 2 for each usage error that the review tried. The Vulkan frame synchronization is correct. The teardown order is correct on the success path and on each failure path.

The review found no CRITICAL finding and no HIGH finding.

| Severity | Count |
|---|---|
| CRITICAL | 0 |
| HIGH | 0 |
| MEDIUM | 2 |
| LOW | 3 |

The two MEDIUM findings are:
- A minimized window stops the manual run on Windows with exit code 1.
- The audit warning NU1900 fails a full restore when nuget.org is not reachable.

The review ran these read-only checks:
- `dotnet build brutal-ui.sln`: exit 0, 0 warnings, 0 errors.
- `dotnet test --solution brutal-ui.sln`: exit 0, 3 passed, 0 failed.
- `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- <args>` with `--frames abc`, `--frames 0`, `--frames`, `--bogus`, `--frames +7 --frames 3`, `--frames=5`, and `--frames -0`: exit 2 and a `Usage:` line each time. With no display: exit 1 and `GLFW init failed: PlatformUnavailable`.
- A scratchpad copy of the build configuration. The build reports IDE0161 and IDE0065 as errors. A restore against an unreachable source with a warm package cache fails with `error NU1900`.

The review did not run the sample on Windows or under a validation layer.

---

## Findings

### CR-1: A minimized window stops the manual run with exit code 1 on Windows
**Severity:** MEDIUM
**Rule:** `correctness-window`
**File:** `samples/HelloPanel/Program.cs:228-253`, `samples/HelloPanel/VulkanContext.cs:170`, `samples/HelloPanel/VulkanContext.cs:230`
**Problem:** The frame loop draws a frame on each pass, also when the window is minimized. On Windows, the surface of a minimized window has a current extent of 0 x 0 (`VK_KHR_win32_surface`). The swapchain then does not match the surface. Common drivers return `VK_ERROR_OUT_OF_DATE_KHR` from `vkAcquireNextImageKHR` or from `vkQueuePresentKHR`. The BRUTAL wrapper throws `VkResultError`, and the sample exits 1 with a stack trace.

The no-argument mode exists for the manual look of the author on Windows (F8). A normal user action (minimize, or Win+D) stops it with a runtime failure and not with exit code 0. Task 2.8 asks for exit 1 on an out-of-date swapchain. Task 2.8 thinks about a resize only, and the window is not resizable. It does not think about a minimize. The scripted `--frames` run under Xvfb does not minimize the window, so the sibling smoke run is not affected.

```csharp
Glfw.PollEvents();
if (window.ShouldClose)
{
    break;
}

platform.NewFrame();   // This runs also when the window is minimized.
```

**Fix:** Skip the frame while the window is minimized. Block on events, so the loop does not use the CPU. Check `ShouldClose` inside the wait, because the user can close a minimized window from the taskbar. Do not use `GlfwWindow.WaitForWindow()`. It does not check the close flag, so it can wait with no end. `GlfwWindow.IsIconified` changes in the iconify callback of GLFW.

```csharp
Glfw.PollEvents();
while (window.IsIconified && !window.ShouldClose)
{
    Glfw.WaitEvents();
}

if (window.ShouldClose)
{
    break;
}
```

**Effort:** Low

### CR-2: The audit warning NU1900 fails a full restore when nuget.org is not reachable
**Severity:** MEDIUM
**Rule:** `build-warnings`
**File:** `Directory.Build.props:15`
**Problem:** `TreatWarningsAsErrors` turns every NuGet warning into an error. `WarningsNotAsErrors` keeps NU1901 to NU1904 (the advisory warnings) as warnings. It does not keep NU1900. NuGet raises NU1900 when it cannot get the vulnerability data. A full restore then fails, also when every package is in the local cache. A full restore occurs after a fresh clone, in a new worktree, after `git clean`, with `--force`, and after a change to a project file or a props file. The review confirmed the failure with a copy of the configuration and an unreachable source:

```text
error NU1900: Warning As Error: Error occurred while getting package vulnerability data: Unable to load the service index for source https://127.0.0.1:9/v3/index.json.
```

Decision 6 says that the audit must not break the build of an unchanged commit. An offline machine or a nuget.org outage breaks it now.

**Fix:** Add NU1900 to the list. The warning still shows, so a person sees it.

```xml
<WarningsNotAsErrors>NU1900;NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
```

Change the comment above the group and Decision 6 in `implementation.md` to match.
**Effort:** Low

### CR-3: The frame loop waits with no time limit, so a GPU stall gives no exit code
**Severity:** LOW
**Rule:** `contract-exit-code`
**File:** `samples/HelloPanel/VulkanContext.cs:40-41`, `samples/HelloPanel/VulkanContext.cs:168`, `samples/HelloPanel/VulkanContext.cs:170`
**Problem:** The fence wait and the image acquire use `NoTimeout`. The value `-1` is `UINT64_MAX` in Vulkan. If the presentation engine or the driver stops, the sample waits with no end. The agent on Linux runs the sample with no user input and reads one exit code. The contract gives exit code 1 for a runtime failure, but a stall gives no exit code. Only an outer timeout of the caller stops the process. The code already handles a finite limit: `CheckResult` throws on `VkResult.Timeout`, and the acquire check throws on each result other than `Success` and `SuboptimalKHR`. Only the constant prevents it.

```csharp
private const nint NoTimeout = -1;
...
CheckResult(_device.WaitForFence(fence, NoTimeout), "vkWaitForFences");
VkResult acquire = _device.AcquireNextImageKHR(_swapchain, NoTimeout, _imageAcquired[_frameSlot], default, out _imageIndex);
```

**Fix:** Use a finite limit for the two frame waits. Ten seconds is much more than the time of one frame. The `vkDeviceWaitIdle` calls of the teardown have no timeout parameter, so this fix does not cover them.

```csharp
/// <summary>The longest wait for a frame fence or a swapchain image, in nanoseconds. A longer wait is a failure.</summary>
private const nint FrameWaitTimeout = 10_000_000_000;
```

**Effort:** Low

### CR-4: The descriptor pool size 8 has no name and no reason in the code
**Severity:** LOW
**Rule:** `maint-magic-number`
**File:** `samples/HelloPanel/Program.cs:202`
**Problem:** `DescriptorPoolSize = 8` is the minimum that `VulkanBackend` accepts. The backend asserts `DescriptorPoolSize >= 8` when it creates its pool. Only `context.md` gives this reason. A reader who makes the value smaller gets an assert at run time and not a build error.

```csharp
DescriptorPoolSize = 8,
```

**Fix:** Give the value a name and a doc comment, next to the other constants of `Program`.

```csharp
/// <summary>The descriptor pool size of the ImGui renderer. 8 is the minimum that <see cref="VulkanBackend"/> accepts.</summary>
private const int ImGuiDescriptorPoolSize = 8;
```

**Effort:** Low

### CR-5: `.gitignore` does not ignore the BenchmarkDotNet output folder
**Severity:** LOW
**Rule:** `build-clean-tree`
**File:** `.gitignore:6-12`
**Problem:** BenchmarkDotNet writes its results to `BenchmarkDotNet.Artifacts/` in the current directory. A run of `BrutalUi.Benchmarks` from the repository root puts that folder into `git status`. `git check-ignore BenchmarkDotNet.Artifacts/x` finds no rule. Item Q11 of the plan asks for a clean `git status` after the standard steps. A benchmark run is not a standard step today. The first feature that adds a real benchmark meets this problem.
**Fix:** Add one line under `# .NET`.

```gitignore
BenchmarkDotNet.Artifacts/
```

**Effort:** Low

---

## What's Already Good

- **Frame synchronization.** Each frame slot has one acquire semaphore and one fence. Each swapchain image has one render-complete semaphore, so a present never uses a semaphore that is still in use. Khronos recommends this pattern. The fence reset comes after a successful acquire, so a failed acquire cannot leave an unsignaled fence. The subpass dependency waits for the acquire at `COLOR_ATTACHMENT_OUTPUT`. Two frames in flight stay inside the vertex buffer ring of `VulkanBackend`, which has `ImageCount` slots.
- **Teardown order.** The order is: wait for the device, dispose `VulkanBackend`, dispose `GlfwBackend`, destroy the ImGui context, dispose `VulkanContext`, dispose the window, end GLFW, write the success line. `using` declarations and `try`/`finally` blocks keep this order on each failure path. The surface goes before the instance and before the window. The constructor of `VulkanContext` disposes a partial object, and each destroy call accepts a null handle.
- **Exit codes.** The parser runs before GLFW and uses the invariant culture. It rejects a repeated `--frames`, `--frames=5`, `-0`, and an overflow. A failed `Glfw.Init()` writes the GLFW error and returns 1. The top `catch` turns each other failure into exit 1. Standard output holds one line on success. The recorded demo output confirms it.
- **The window-close path.** `GlfwWindow` sets its close flag before it calls the `OnClose` event. `ShouldClose` is therefore correct after `PollEvents`, also with `GlfwBackend` attached.
- **Windows native loading.** `Brutal.Glfw` loads `glfw3` on Windows. `Brutal.Vulkan` loads `vulkan-1` from `System32`. The Windows smoke run has no gap like the Linux `libvulkan.so` gap.
- **One place per rule.** Each `.csproj` holds only its SDK, its output type, its references, and (in `src/`) its description. No `.csproj` holds a version, a target, or a `NoWarn`. The nine pins match the plan table exactly. `BrutalUi.Core` references only `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization`.
- **Style enforcement.** A scratchpad probe confirms that the build reports IDE0161 (a block namespace) and IDE0065 (a `using` inside a namespace) as errors.
- **The sibling contract.** All paths use the exact case of the files. Project references use forward slashes. No build step depends on the shell or on the operating system. `UseMicrosoftTestingPlatformRunner` is a live property in `xunit.v3` 4.0.2.
- **BRUTAL style.** The code uses file-scoped namespaces, a `required` create-info struct, `int2` and `float4`, and `u8` literals for all ImGui text. The `u8` literals give the null terminator that `ImGui.Text` needs. `io.IniFilename` is null, so no `imgui.ini` file appears.
- **Comments.** The comments follow ASD-STE100. The only error is the `-ing` subject at `VulkanContext.cs:280` ("Destroying the pool also frees its command buffers."). It is too small for a finding.
- **Pack metadata.** The five packages carry the version, the authors, the MIT expression, the repository, and a description. NuGet reports a missing package readme as information only. Add a readme before a public release.

---

## Recommended Fix Order

| Order | Finding | Severity | Effort | Reason |
|---|---|---|---|---|
| 1 | CR-2 | MEDIUM | Low | One token in one file. It removes a build break on an offline machine and in a new worktree. |
| 2 | CR-1 | MEDIUM | Low | Five lines in `RunFrames`. It makes the manual Windows run (F8) safe before `dev-ops/windows-test-path` uses it. |
| 3 | CR-5 | LOW | Low | One line. |
| 4 | CR-4 | LOW | Low | One constant. |
| 5 | CR-3 | LOW | Low | One constant and its comment. Do it together with CR-1, because both change the frame loop. |

---

## Progress

- [x] CR-1: A minimized window stops the manual run with exit code 1 on Windows
- [x] CR-2: The audit warning NU1900 fails a full restore when nuget.org is not reachable
- [x] CR-3: The frame loop waits with no time limit, so a GPU stall gives no exit code. Won't fix: a finite wait can stop a manual run with exit 1 when a compositor throttles a hidden window. The sibling scripts (`dev-ops/linux-dev-environment`, `dev-ops/windows-test-path`) wrap the smoke run in a timeout, so a stall still gives a non-zero exit code.
- [x] CR-4: The descriptor pool size 8 has no name and no reason in the code
- [x] CR-5: `.gitignore` does not ignore the BenchmarkDotNet output folder
