# imgui-renderer/hello-panel-sample - Context & Decisions

**Last Updated:** 2026-10-21 04:20
**Current Phase:** Complete
**Status:** ✅ Complete

---

## Quick Status

**What's Done:**
- `/start-feature` scaffolded `context.md`, `tasks.md` (27 tasks in 4 phases), and `inbox.md`. The plan status is In Progress.
- Phase 1 is done (tasks 1.1 to 1.8). The sample draws the panel and the label through `ImGuiRenderer`. The smoke run gives `draw commands 1, vertices 124` and `exit=0`.
- Phase 2 is done (tasks 2.1 to 2.7). The sample uploads a 64 x 64 checker texture and shows it as an `Image`. The smoke run gives `draw commands 2, vertices 128` and `exit=0`.
- Phase 3 is done (tasks 3.1 to 3.5). The sample measures the UI region and the frame region after 3 warm-up frames and writes line 3. The smoke run gives `Update and Draw 0, frame <F>, in 117 frames` and `exit=0`. The planted run gives `exit=1`.
- Phase 3 fix 1 and fix 2 are done. The phase 3 verifier returned PASS. See "Phase 3 Verify: PASS".
- Phase 4 is done (tasks 4.1 to 4.7). `TreeDrawBenchmark` gives `Draw` about 21 us with `-` in `Allocated`. `CHANGELOG.md`, `docs/technical-design.md`, and `human-testing.md` hold the record. The phase 4 verifier returned PASS with Demo: PASS.

- The cross-phase review is done. CR-1 to CR-5 are fixed. See "Review Group 1: Results" to "Review Group 3: Results". `scripts/test-linux.sh` prints PASS.

**What's Next:**
- The manual checks of "Needs Human Verification" on Windows and on a display.
- `/review-epic imgui-renderer`.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `samples/HelloPanel/Program.cs` - the create order, the frame order, the teardown order, the output lines, and the allocation gate.
- `samples/HelloPanel/HelloScreen.cs` - the tree of the screen and the checker pixels (new).
- `samples/HelloPanel/SampledTexture.cs` - owns the image, the memory, the view, and the sampler (new, phase 2).
- `samples/HelloPanel/VulkanContext.cs` - `CreateTexture`, the staging upload (phase 2).
- `tests/BrutalUi.Benchmarks/TreeDrawBenchmark.cs` - `Draw` on `ThousandElementTree` into `RecordingRenderer` (new, phase 4).

### Related Files
- `docs/features/imgui-renderer/hello-panel-sample/implementation.md` - the plan.
- `docs/features/imgui-renderer/imgui-draw-list/context.md` and `docs/features/imgui-renderer/label-and-image/context.md` - the hand-offs to this feature.

---

## Important Decisions

### Decision 0: The answers to the open scope questions
**Date:** 2026-10-21
**Context:** The plan held four open scope questions.
**Decision:** The user picked the recommended option of each question in `/autorun-epic`. 1: the `scripts/test-linux.sh` smoke log demo. 2: only the UI region (`ViewportSize`, `Update`, `Draw`) fails the run on an allocation. 3: `TreeDrawBenchmark` with `RecordingRenderer`. 4: the texture upload stays in the sample.
**Impact:** The plan follows each answer with no change.

### Start state
- label-and-image is on `main` as commit `6e2b5a2`. The I1 and I2 checks of the plan use this commit as `<label-and-image commit>`.

---

## Phase 1: Results

### Task 1.1: the start state
- `src/BrutalUi.Core/Elements/Label.cs` and `UiContext.CreateInfo` (in `src/BrutalUi.Core/Tree/UiContext.cs`) exist.
- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 1013, failed 0, succeeded 1013, skipped 0. The total is equal to the expected value.
- The smoke command (start state) prints `HelloPanel: 120 frames rendered.` and `exit=0`.

### Tasks 1.2 to 1.6: the code
- `samples/HelloPanel/HelloScreen.cs` (new) builds the root, the panel, and the label of "The Screen". The label text is in the create-info (`Text = GreetingText`). The analyzer `BRUTALCORE0011` accepts the string in a create-info field, so the code needs no `SetText` call.
- The plan gives no color values. The code uses `PanelColor = (30, 34, 44, 255)`, `BorderColor = (96, 140, 220, 255)`, and `GreetingColor = (255, 255, 255, 255)`. Each size and each color is a named constant.
- `Program.cs`: `RunImGui` creates `fonts`, `textures`, `uiRenderer`, and `ui` before the backends, renames the backends to `glfwBackend` and `vulkanBackend`, and calls `ui.AddRoot(HelloScreen.Build())` after the backends. `RunFrames` has the UI region and reads the two counts after `ImGui.Render`. `RunWindow`, `RunImGui`, and `RunFrames` return `RunResult(FramesRendered, DrawCommandCount, VertexCount)`. `Run` writes line 1 with no change, then line 2.
- `RunFrames` reads `ImGui.GetIO()` one time before the loop. The pointer stays valid for the life of the ImGui context.

### Task 1.7: the checks
- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `--frames abc`, `--frames 0`, `--frames`, and `--bogus`: each prints the `Usage:` line and the reason, and gives `exit=2`.
- The smoke command prints the two lines below and `exit=0`.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 11 s` and `exit=0`. `artifacts/test-linux/smoke.log` holds the two lines.
- `grep -nE "ImGui\.(Begin|Text|End)\(" samples/HelloPanel/*.cs` prints nothing.
- `git diff --quiet -- src Directory.Packages.props scripts samples/WorldSpace samples/HelloPanel/HelloPanel.csproj` gives `exit=0`.
- An extra run with `--frames 1` gives the same counts. Thus the first frame already holds the full layout: `glfwBackend.NewFrame()` sets `io.DisplaySize` before the UI region.

### Task 1.8: the results
- Line 1: `HelloPanel: 120 frames rendered.`
- Line 2: `HelloPanel: last frame: draw commands 1, vertices 124.`
- The draw command count is 1, as the plan expects. The panel and the label share the font atlas texture.
- The vertex count is 124.
- Deviations from "The Create Order": none. Phase 1 runs steps 1, 3 to 7, and 9.
- Deviations from "The Frame Order": none. The UI region holds only the viewport set, `Update`, and `Draw`.
- Note: the teardown drops `ui` with its root, and it calls no `RemoveRoot`. The remarks of `UiContext` ask for `RemoveRoot` before a drop of a context. The rule protects an element that goes into another context. The sample ends the process, so the drop is safe. "The Teardown Order" of the plan has no `RemoveRoot` step.

---

## Phase 2: Results

### Task 2.1: the Vulkan API of "The Texture Upload"

Source: the XML docs of `Brutal.Vulkan` 2026.10.2 and `Brutal.Vulkan.Abstractions` 2026.10.2. The XML docs give no return type and no default value, so a reflection dump of the two assemblies (a scratch tool outside the repository) gave the full signatures. An IL scan of each helper showed the calls inside it. "Raw" means a generated extension of `Brutal.VulkanApi.VkDeviceExtensions` on `Device`. "Helper" means a member of `Brutal.VulkanApi.Abstractions`.

| Step | Call | Kind | Signature |
|------|------|------|-----------|
| 1 | Create the staging buffer | raw | `VkBuffer CreateBuffer(in VkBufferCreateInfo, VkAllocator)` |
| 1, 2 | Allocate the memory | helper | `DeviceExtensions.AllocateMemory(Device, PhysicalDevice, VkBuffer or VkImage, VkMemoryPropertyFlags, VkAllocator) -> VkDeviceMemory` |
| 1, 2 | Bind the memory | raw | `void BindBufferMemory(VkBuffer, VkDeviceMemory, ByteSize64)` and `void BindImageMemory(VkImage, VkDeviceMemory, ByteSize64)` |
| 1 | Write the pixels | helper | `DeviceExtensions.WriteMemory<T>(Device, ReadOnlySpan<T>, VkDeviceMemory, ByteSize inOffset = default, VkMemoryMapFlags inFlags = None)` |
| 2 | Create the image | raw | `VkImage CreateImage(in VkImageCreateInfo, VkAllocator)` |
| 3 | Allocate the command buffer | helper | `DeviceExtensions.AllocateCommandBuffer(Device, VkCommandBufferAllocateInfo) -> CommandBuffer` |
| 3 | Begin the command buffer | helper | `CommandBufferExtensions.Begin(CommandBuffer, VkCommandBufferUsageFlags)` |
| 4 | The barriers | raw | `void PipelineBarrier(VkPipelineStageFlags src, VkPipelineStageFlags dst, VkDependencyFlags, ReadOnlySpan<VkMemoryBarrier>, ReadOnlySpan<VkBufferMemoryBarrier>, ReadOnlySpan<VkImageMemoryBarrier>)` on `CommandBuffer` |
| 4 | The subresource range | helper | `Subresources.Single(VkImageAspectFlags, int mipLevel = 0, int arrayLayer = 0) -> VkImageSubresourceRange` |
| 4 | The subresource layers | helper | `SubresourceExtensions.ToLayers(VkImageSubresourceRange, int mipLevel = 0) -> VkImageSubresourceLayers` |
| 4 | The copy | raw | `void CopyBufferToImage(VkBuffer, VkImage, VkImageLayout, ReadOnlySpan<VkBufferImageCopy>)` on `CommandBuffer` |
| 5 | End the command buffer | raw | `void End()` on `CommandBuffer` |
| 5 | Submit | helper | `QueueExtensions.Submit(Queue, Span<VkSemaphore> wait, Span<VkPipelineStageFlags> waitStages, Span<CommandBuffer>, Span<VkSemaphore> signal, VkFence)` |
| 5 | Wait for the queue | raw | `void Queue.WaitIdle()` |
| 6 | Free the command buffer | helper | `DeviceExtensions.FreeCommandBuffers(Device, VkCommandPool, ReadOnlySpan<CommandBuffer>)` |
| 6 | Destroy the buffer, free the memory | raw | `void DestroyBuffer(VkBuffer, VkAllocator)` and `void FreeMemory(VkDeviceMemory, VkAllocator)` |
| 7 | Create the view | raw | `VkImageView CreateImageView(in VkImageViewCreateInfo, VkAllocator)` |
| 7 | The sampler settings | helper | `Presets.Sampler.SamplerPointClamped` (a static `VkSamplerCreateInfo`) |
| 7 | Create the sampler | raw | `VkSampler CreateSampler(in VkSamplerCreateInfo, VkAllocator)` |
| Dispose | Destroy the sampler, the view, and the image | raw | `DestroySampler(VkSampler, VkAllocator)`, `DestroyImageView(VkImageView, VkAllocator)`, `DestroyImage(VkImage, VkAllocator)` |

The choices:
- `DeviceExtensions.AllocateMemory` covers the memory step. Its IL reads the memory requirements, calls `PhysicalDevice.FindMemoryType`, and calls `vkAllocateMemory`. It does not bind, so the code calls the raw bind.
- `DeviceExtensions.WriteMemory` covers the write. It maps, copies, and unmaps. It does not flush, so the staging memory must be host-coherent (the plan asks for it).
- `Presets.Sampler.SamplerPointClamped` covers the sampler settings. The dump gives: `MagFilter` and `MinFilter` `Nearest`, `MipmapMode` `Nearest`, `AddressModeU`, `V`, and `W` `ClampToEdge`, `AnisotropyEnable` false, `MinLod` 0, `MaxLod` 1000, `CompareEnable` false. That is step 7 of the plan.
- The code does not use `DeviceEx.AllocateImage`, `DeviceEx.AllocateBuffer`, `ImageEx`, or `BufferEx`. These create, allocate, and bind in one call, and `DeviceEx.FreeAllocation` destroys the object and frees the memory in one call. Their IL has throw paths for a null memory and for a foreign allocator. `SampledTexture` must dispose a partly built object one handle at a time, so it keeps four raw handles.
- The queue wait is `Queue.WaitIdle()`, not a fence. The upload runs one time before the first frame, so a fence adds an object and gives no gain.
- `VkImageMemoryBarrier` gives 0 for each queue family index by default. The code sets `VK.QUEUE_FAMILY_IGNORED` on both, because the barrier transfers no ownership.

### Tasks 2.2 to 2.6: the code
- `samples/HelloPanel/SampledTexture.cs` (new): `internal sealed class SampledTexture : IDisposable`. The constructor `SampledTexture(Device, int2)` runs step 2 of "The Texture Upload" (the image, the device-local memory, and the bind). `CreateViewAndSampler()` runs step 7. `Dispose` destroys the sampler, the view, and the image, then frees the memory. It skips each null handle, so a partly built texture is safe to dispose. A second call does nothing.
- `VulkanContext.CreateTexture(ReadOnlySpan<byte> rgba, int2 size)` runs steps 1 to 7 in the order of the plan. An inner `finally` runs step 6 (it frees the command buffer, the staging buffer, and the staging memory) on success and on failure. An outer `catch` disposes the texture on a failure. The class remarks have the new sentence.
- `HelloScreen.FillChecker(Span<byte>, int2)` writes cells of 8 texels. The top-left cell is light gray (230, 232, 238, 255). The other cells are orange (232, 120, 48, 255). `Build(TextureHandle checker)` adds the `Image` after the label: `StretchToFill`, white tint, `Style.Margin.Top = 12`. The name `Image` is not ambiguous in `HelloScreen.cs`, so the code writes `Image` with no namespace.
- `Program.RunWindow` fills a `byte[]` of `64 * 64 * 4` bytes and declares `using SampledTexture checker = vulkan.CreateTexture(pixels, checkerSize);` right after the `vulkan` declaration and before `ImGui.CreateContext()`.
- `Program.RunImGui` declares `TextureHandle handle = default` before the `try` block. The `try` block calls `textures.AddVulkan(...)`, then `ui.AddRoot(HelloScreen.Build(handle))`, then `RunFrames`. The `finally` block calls `vulkan.WaitIdle()`, then `textures.Remove(handle)` when `handle.IsNone` is false. The `using` declarations then dispose `vulkanBackend` and `glfwBackend`.

### Task 2.7: the checks
- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- The smoke command prints the two lines below and `exit=0`.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 12 s` and `exit=0`. `artifacts/test-linux/smoke.log` holds the two lines below.
- An extra run with `--frames 1` gives the same counts.
- `grep -rn "Brutal.Texture" samples Directory.Packages.props` prints nothing. The sample reads no image file.
- `git diff --quiet -- src Directory.Packages.props scripts samples/WorldSpace samples/HelloPanel/HelloPanel.csproj` gives `exit=0`.
- The validation run: no validation layer on this machine. `/usr/share/vulkan/explicit_layer.d/VkLayer_khronos_validation.json` does not exist, and a search of `/usr`, `/etc`, `/opt`, and `~/.local/share/vulkan` finds no `VkLayer_khronos_validation` file. The directory holds only the Intel null-hardware layer and two Mesa layers.

### The results
- Line 1: `HelloPanel: 120 frames rendered.`
- Line 2: `HelloPanel: last frame: draw commands 2, vertices 128.`
- The draw command count is 2, as the plan expects. Command 0 holds the panel and the label (the font atlas). Command 1 holds the image (the checker texture).
- The vertex count is 128: the 124 vertices of phase 1 and 4 vertices for the image quad.

### Deviations from the plan
- `SampledTexture` has two members more than the plan names: the property `Image` (the upload records its barriers and its copy on it) and the method `CreateViewAndSampler()` (step 7). The constructor runs step 2. Thus the texture owns each object from the time of its creation, and the failure path of `CreateTexture` disposes one object.
- `CreateTexture` also throws `ObjectDisposedException` after a dispose of the context, and `ArgumentOutOfRangeException` when a size component is above `VkPhysicalDeviceLimits.MaxImageDimension2D`. A larger image breaks a valid usage rule of `vkCreateImage`, and the driver need not report an error.
- `HelloScreen.FillChecker` checks its arguments with the same exceptions as `CreateTexture`.
- The sampler settings come from `Presets.Sampler.SamplerPointClamped`. The preset also sets `MipmapMode` `Nearest` and `MaxLod` 1000. The image has 1 mip level, so these values change nothing.
- The plan gives no checker colors. The code uses light gray (230, 232, 238, 255) and orange (232, 120, 48, 255).
- The `AddVulkan` call is inside the `try` block, so `Remove` also runs when `AddRoot` or `RunFrames` throws.

---

## Phase 3: Results

### Tasks 3.1 and 3.2: the code
- `Program.cs` has the constant `WarmUpFrames = 3`. `RunResult` has three more members: `UiAllocatedBytes`, `FrameAllocatedBytes` (both `long`), and `MeasuredFrames`.
- `RunFrames` keeps the two sums and the measured frame count in locals. Each frame sets `measured = framesRendered >= WarmUpFrames` at the start of the frame region. The frame region reads `GC.GetAllocatedBytesForCurrentThread()` before `glfwBackend.NewFrame()` and after `framesRendered++`. The UI region reads it right before the viewport set and right after `ui.Draw(uiRenderer)`. The loop adds the two differences only in a measured frame. A skipped frame (a minimized window) is not measured.
- `Run` writes line 3 after line 2. When `UiAllocatedBytes` is above 0, `Run` writes the gate line to standard error and returns `ExitFailure`. The frame sum does not change the exit code (Decision 4).
- The remarks of `RunFrames` state the two regions, the warm-up, and the gate. The sentence "The loop allocates no managed memory after warm-up." is gone.
- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.

### Task 3.3: the 5 smoke runs
One run prints these three lines:
- `HelloPanel: 120 frames rendered.`
- `HelloPanel: last frame: draw commands 2, vertices 128.`
- `HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.`

| Run | Update and Draw | Frame | Measured frames | Exit code |
|-----|-----------------|-------|-----------------|-----------|
| 1 | 0 | 22464 | 117 | 0 |
| 2 | 0 | 22464 | 117 | 0 |
| 3 | 0 | 22224 | 117 | 0 |
| 4 | 0 | 22464 | 117 | 0 |
| 5 | 0 | 22464 | 117 | 0 |

A check run before the 5 runs gave frame 17472 (91 x 192) and `exit=0`. Later runs gave frame 19728 and 22464.

**Finding: the source of the frame number.** The frame number comes from `GlfwBackend.NewFrame()` (BRUTAL `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2). It is not in `src/` and not in the UI region.
- A temporary probe in `RunFrames` read the counter around each call of steps 2, 4, and 5. In each of the 117 measured frames, `glfwBackend.NewFrame()` allocated 192 bytes. `vulkanBackend.NewFrame()`, `ImGui.NewFrame()`, `ImGui.Render()`, the read of the two counts, `vulkan.BeginFrame()`, `vulkanBackend.RenderDrawData(...)`, and `vulkan.EndFrame()` allocated 0 bytes. The probe is gone. `Program.cs` came back from a copy in the session scratch folder.
- 22464 is 117 x 192.
- With `DOTNET_TieredCompilation=0`, the probe run gave `frame 0`. Thus the bytes come from the tier-0 code of the backend, not from the backend logic. The IL of `NewFrame` and of its helpers (`UpdateMouseData`, `UpdateMouseCursor`, `UpdateGamepads`) has `Enum.HasFlag` calls. Tier-0 code boxes the two operands of `Enum.HasFlag` (24 bytes each), and tier-1 code does not. 192 bytes are 8 boxes, thus 4 `HasFlag` calls. This cause is probable, but the probe did not prove it call by call.
- A run of 600 frames shows the stop: the backend allocates 192 bytes in each frame up to about frame 230, then 48 bytes one time, then 0. The tier-up of the backend methods occurs after the 120 frames of the smoke run. The tier-up time changes from run to run, so the frame number changes (17472, 19728, 22224, 22464).
- The frame number does not fail the run (Decision 4). The UI region gave 0 in each run, also with tier-0 code in `src/`.

### Task 3.4: the planted allocation
- The copy is `Label.cs.phase3-plant` in the session scratch folder. The name differs from the `Label.cs.orig` copy of label-and-image.
- The plant is `GC.KeepAlive(new object());` at the start of `Label.PaintContent`. The build gave `0 Warning(s)`, `0 Error(s)`.
- The planted run: standard output gave `HelloPanel: bytes after warm-up: Update and Draw 2808, frame 20856, in 117 frames.`. Standard error gave `HelloPanel: Update and Draw allocated 2808 bytes after warm-up. The limit is 0 bytes.`. The exit code was **1**. 2808 is 117 x 24 bytes: one object for the one label in each measured frame.
- The restore used `cp` from the copy. `diff` of the copy and `Label.cs` gave no output (`diff=0`). `git diff --quiet -- src; echo "exit=$?"` printed `exit=0`.
- The build and the smoke run after the restore gave `Update and Draw 0, frame 19728, in 117 frames.` and exit code **0**.

### Task 3.5: the script and the tests
- The first `scripts/test-linux.sh` run gave `exit=2`. One unit test failed: `BrutalUi.Core.Tests.ElementAllocationTests.UpdateAndDrawAllocateNothingAfterWarmUp` measured 5968 bytes in 100 frames (1012 of 1013 passed). `src/` and `tests/` had no change at that time (`git diff --quiet -- src` gave `exit=0`).
- The next two script runs gave `test-linux: PASS build, test, gpu, smoke (120 frames) in 13 s` and `in 10 s`, each with `exit=0`. `artifacts/test-linux/smoke.log` holds the three lines with `Update and Draw 0, frame 22464, in 117 frames.`
- `dotnet test --solution brutal-ui.sln`: total 1013, failed 0, succeeded 1013, skipped 0. This is the total of task 1.1. 8 more runs with `--no-build` each gave `failed: 0`.
- Thus the test failed 1 time in 12 runs. See "Open problems of phase 3".

### Deviations from the plan
- The summary of `Run` and the remarks of `Program` also name line 3 and the gate exit code. The plan names only the remarks of `RunFrames`. Without the change, the two comments state "two lines" and a wrong exit code contract.
- A temporary probe in `Program.cs` found the source of the frame number. The final `Program.cs` holds no probe.

### Open problems of phase 3
- `ElementAllocationTests.UpdateAndDrawAllocateNothingAfterWarmUp` failed 1 time in 12 runs with 5968 bytes. The test code and `src/` did not change in this phase. A probable cause is the tier-0 code of the runtime, as in the frame number finding: the test measures soon after warm-up, and the tier-up time changes from run to run. A follow-up can run the test with `DOTNET_TieredCompilation=0` or `DOTNET_TieredPGO=0` in a loop to prove the cause. A fix in `tests/` is outside the scope of this phase.

## Phase 3 Fix 1: The Flaky Allocation Gate

**Result:** the cause is the background GC of the runtime, not tier-0 code and not `src/`. A background GC can add the free part of the allocation context of a thread (less than 8 KB) to `GC.GetAllocatedBytesForCurrentThread`, also when the thread allocates nothing. The fix empties the context with a blocking GC of generation 0 right before each measure. The fix is in `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs` only.

### Reproduction
- 30 runs of the class and 30 runs of the full `BrutalUi.Core.Tests` project, one at a time: 0 failures. The machine had no other load.
- The failure needs load. The scratch harness ran the test executable 8 times in parallel (8 cores). 160 runs gave 2 failed runs: `UpdateAndDrawAllocateNothingAfterWarmUp` with 4184 and 3776 bytes. 240 runs with a per-frame probe gave 7 failed runs: 6 times `UpdateAndDrawAllocateNothingAfterWarmUp` (1776 bytes 5 times, 7744 bytes 1 time), and 1 time `RefreshLocalizedTextAllocatesNothing` (6776 bytes). The failure rate is about 3 runs in 100 under load.
- 12 runs of `dotnet test --solution brutal-ui.sln --no-build` gave 0 failures.

### Evidence
- The per-frame probe: each failed run had exactly 1 frame with bytes, in measured frames 0 to 22 of 100. The bytes were in `Update` (11 times) or in `Draw` (2 times), never in the setters. The frame took 13 to 76 ms, in place of 0.4 ms. `JitInfo.GetCompiledMethodCount(currentThread: true)` gave 0 for the frame, so no JIT ran on the thread.
- The extra GC load: temporary diagnostic tests in the same process allocated in a loop, started background GCs, and held an `EventListener` for the GC events. The listener makes the step more frequent. The diagnostic files are gone.
- A diagnostic test read the counter around a loop of `Math.Sqrt` only, under the extra GC load. The counter increased in 17 of 120 runs, each time 1 step of 512 to 7664 bytes. The code in the loop cannot allocate. Thus the bytes do not come from `src/` or from the test.
- Each step was less than 8 KB, which is the size of one allocation context. Each step occurred one time per test, soon after the last allocation of the thread.
- `DOTNET_gcConcurrent=0`: 0 failed runs in 240 plain runs and in 120 runs with an extra GC load. The same extra load without the setting gave 24 failed runs in 120.
- `DOTNET_TieredCompilation=0`: 1 failed run in 240 (1776 bytes). Thus tier-0 code is not the cause. `src/` builds in Debug, so the JIT does not tier the code of `src/`.
- The same loop with a `GC.Collect(0, GCCollectionMode.Forced, blocking: true)` before it: 0 steps in 120 runs, in the same processes that gave the 17 steps.
- A forced GC of each kind (generation 0, 1, 2, background, compacting) from another thread gave 0 steps in 1000 tries. Thus the step needs a background GC under allocation load, not a single GC.
- A probable runtime path: the background GC voids the allocation context of each thread (it sets the pointer and the limit to 0) and does not subtract the free part from the byte count. The probe did not prove this line of the runtime.
- The candidates of the brief: no `Enum.HasFlag` in `src/`. No `ArrayPool`, lock, or static cache in the frame path. `ClassName` takes a lock, but no frame calls it. `LString` reads occur in the setters, which never had bytes. A longer warm-up does not help, because the step does not depend on the code of the frame.
- The stable gates: `ImGuiRendererAllocationTests` runs with `[assembly: Parallelization(Mode = ParallelMode.None)]`, so no other test allocates in parallel. `ThousandElementAllocationTests` uses a longer warm-up and one measure around 100 iterations. It had no failure in these runs, but it has the same exposure.

### Fix
- A private helper `ReleaseAllocationContext()` calls `GC.Collect(0, GCCollectionMode.Forced, blocking: true)`. Each GC empties the context of each thread, so the context of the test thread has no free part after the call.
- `UpdateAndDrawAllocateNothingAfterWarmUp` and `SetTextFromAStringAllocatesNothingAfterWarmUp` call it one time, after the warm-up and right before the first measured frame. The measured frames allocate nothing, so the context stays empty.
- `RefreshLocalizedTextAllocatesNothing` calls it in `MeasureLocaleChange` before each measured refresh, because the test allocates between two refreshes (the messages of the asserts).
- The gate stays strict: the limit is 0 bytes, the warm-up has 3 frames, and the measure has 100 frames. The GC does not change the count of a later allocation.
- No project setting changed. `<ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>` also stops the failure, but it changes each test of the project.

### Proof
- Load harness, 480 runs of the full project, 8 in parallel: 0 failures of `ElementAllocationTests`. Before the fix, 400 runs gave 9 failures.
- 30 runs of `dotnet test --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj --no-build -- --filter-class 'BrutalUi.Core.Tests.ElementAllocationTests'`: 0 failed runs (total 4 each).
- 10 runs of the full `BrutalUi.Core.Tests` project: 0 failed runs (total 768 each).
- The plant `GC.KeepAlive(new object());` at the start of `Label.PaintContent`: `UpdateAndDrawAllocateNothingAfterWarmUp` failed with 122400 bytes (100 frames x 51 labels x 24 bytes). The restore used `cp` from `Label.cs.fix1-plant` in the session scratch folder. `diff` gave no output. `git diff --quiet -- src; echo "exit=$?"` printed `exit=0`.
- `dotnet build brutal-ui.sln`: 0 warnings, 0 errors. `dotnet test --solution brutal-ui.sln`: total 1013, failed 0.

### Open problems of Fix 1
- Fixed: `LayoutAllocationTests`, `TreeAllocationTests`, and `ThousandElementAllocationTests` call the shared helper now. See "Phase 3 Fix 2: The Other Allocation Gates".
- `samples/HelloPanel` measures `Update` and `Draw` with the same counter, and the sample has no parallel test load. The risk is low, but a background GC can still add a step.

## Phase 3 Fix 2: The Other Allocation Gates

**Result:** each 0-byte gate of `BrutalUi.Core.Tests` calls the shared helper `AllocationGate.ReleaseAllocationContext()` right before its measured region. The cause is the background GC of fix 1. 480 runs under load gave 0 failed runs. The two plants made each named gate fail. No file of `src/` changed.

### Files changed
- `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` (new): `internal static class AllocationGate` with `ReleaseAllocationContext()`. Its XML doc comment states the cause in three sentences. `BrutalUi.Benchmarks` and `BrutalUi.ImGui.Tests` link the `Shared` folder, so the file uses no xUnit type.
- `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`: the 3 gates call the shared helper. The private helper of fix 1 is gone. `MeasureLocaleChange` calls the helper before each refresh, because the test allocates the messages of the asserts between two refreshes. The class remarks name the helper.
- `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs`: `LayoutStepAllocatesNothingAfterWarmUp`.
- `tests/BrutalUi.Core.Tests/TreeAllocationTests.cs`: `TreeChangesAndUpdateAllocateNothingAfterWarmUp`.
- `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: `UpdateAllocatesNothingAfterWarmUp` and `DrawAllocatesNothingAfterWarmUp`. The class remarks name the helper.
- Each of these gates measures one region around the full loop, and the loop allocates nothing. Thus one call is enough. `TheProbeSeesAnAllocation` asserts 64 bytes or more, not 0 bytes, so it does not change.
- No limit, warm-up count, measured count, or project setting changed. `src/`, `Directory.Packages.props`, `scripts/`, and `samples/` did not change.

### The choice for `BrutalUi.ImGui.Tests`
- No change. The assembly runs with `[assembly: Parallelization(Mode = ParallelMode.None)]`, so no other test allocates in parallel and starts a background GC.
- No gate there reads the counter after an allocation of the test in the same frame. `HeadlessImGui.BeginFrame` and `HeadlessImGui.EndFrame` make no managed allocation. `DrawFrame`, `DirectFrame`, and `LabelFrame` allocate nothing before their reads.
- Two reads follow an allocation of the test outside a frame. `LabelUpdateAndDrawAllocateNothingAfterWarmUp` reads the counter at the start of a label frame, after the messages of `AssertLabelFrame` of the frame before. `ImGuiTextureServiceTests.LookupsAllocateNothing` reads it after its setup. See "Open problems of Fix 2".
- An earlier record of this fix states 480 runs of the ImGui test executable under the same load, with 0 failed runs (total 243 each). This session did not repeat that run.

### Proof
- Load: the harness `fix2b-load.sh` in the session scratch folder ran 60 rounds. Each round started 8 parallel runs of `dotnet test --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj --no-build` and waited for all 8. The logs are in `runs/FIX2B/`. The count does not use the older logs of `runs/FIX2/`.
- Load result: 480 runs, 480 with `exit=0`, each with total 768 and failed 0. Thus 0 failed runs, and no failed test with a byte count. Before fix 2, 480 runs gave 1 failure of `LayoutAllocationTests` and 3 failures of `TreeAllocationTests`.
- Plant 1: `GC.KeepAlive(new object());` at the start of `Label.PaintContent`, with the copy `Label.cs.fix2-plant` in the session scratch folder. `ElementAllocationTests`: total 4, failed 1, exit code 2. `UpdateAndDrawAllocateNothingAfterWarmUp` failed with 122400 bytes (100 frames x 51 labels x 24 bytes). The 3 other tests do not call `Draw`, so they passed.
- Plant 2: the same line at the start of `LayoutEngine.Calculate` in `src/BrutalUi.Core/Layout/LayoutEngine.cs`, with the copy `LayoutEngine.cs.fix2-plant`. Each `Update` with a dirty root calls it. The three classes: total 14, failed 4, exit code 2. Each gate failed with 2400 bytes (100 steps x 24 bytes): `ThousandElementAllocationTests.UpdateAllocatesNothingAfterWarmUp`, `ThousandElementAllocationTests.DrawAllocatesNothingAfterWarmUp`, `LayoutAllocationTests.LayoutStepAllocatesNothingAfterWarmUp`, and `TreeAllocationTests.TreeChangesAndUpdateAllocateNothingAfterWarmUp`.
- Plant 2 and `ElementAllocationTests`: total 4, failed 2. `SetTextFromAStringAllocatesNothingAfterWarmUp` and `UpdateAndDrawAllocateNothingAfterWarmUp` failed with 2400 bytes each. `RefreshLocalizedTextAllocatesNothing` runs no `Update` in its measure, so it passed.
- The restore used `cp` from each copy. `diff` of each pair gave no output. `git diff --quiet -- src Directory.Packages.props scripts; echo "exit=$?"` printed `exit=0`.
- `dotnet build brutal-ui.sln`: 0 warnings, 0 errors. `dotnet test --solution brutal-ui.sln`: total 1013, failed 0, succeeded 1013, skipped 0.

### Open problems of Fix 2
- The two reads of `BrutalUi.ImGui.Tests` in "The choice" have the exposure of fix 1, but only when a background GC starts during the measure. When the assembly turns on parallel tests, these gates must call `AllocationGate.ReleaseAllocationContext()` before each measure.
- A concurrent writer added an earlier draft of this result to the Fix 1 section and to `tasks.md` during the session. This section replaces that draft.

## Phase 3 Verify: PASS

The verifier of `.beast-mode/briefs/imgui-renderer/hello-panel-sample/phase-3-verify.md` returned PASS after fix 1 and fix 2.
- `dotnet build brutal-ui.sln`: 0 warnings, 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 1013, passed 1013, failed 0.
- 3 smoke runs: each `exit=0` and `Update and Draw 0`.
- 120 parallel load runs of `BrutalUi.Core.Tests`: each passed.
- The diff check of `src/` and the other fixed paths: `exit=0`.
- The 7 zero-byte gates of `BrutalUi.Core.Tests` each call `AllocationGate.ReleaseAllocationContext()` right before the measured region.
- Demo: PASS. `scripts/test-linux.sh` printed the PASS line.
- The two inbox bug lines are done (`phase-3-fix1.md` and `phase-3-fix2.md`).

---

## Phase 4: Results

### Task 4.1: the benchmark

`tests/BrutalUi.Benchmarks/TreeDrawBenchmark.cs` has `[MemoryDiagnoser]` and one `[Benchmark]` method `Draw`. The setup builds `ThousandElementTree`, runs one `Update`, creates one `RecordingRenderer`, and runs `Draw` two times. `RecordingRenderer` comes from the linked `Shared/*.cs` files, so the project file did not change. The time holds the walk and the cost of the recording.

### Task 4.2: the build and the demo

`dotnet build brutal-ui.sln -c Release`: 0 warnings, 0 errors. The demo command (`--filter '*TreeDrawBenchmark*'`) printed `Draw | 21.26 us | 0.401 us | 0.991 us | 20.86 us | -`. The `Allocated` column shows `-`, so the draw walk allocates nothing after warm-up.

### Task 4.3: the full run and the facts

- Date: 2026-10-21.
- Commit: `6e2b5a2`. The working tree holds the uncommitted hello-panel-sample work, so the numbers belong to that tree and not to the commit alone.
- Commands: `--filter '*'` (6 benchmarks, 4 min 27 s) and `--facts`, both with `-c Release`.

Machine block:

```
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 (Resolute Raccoon)
Intel Core i7-6700K CPU 4.00GHz (Max: 0.80GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.113
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
```

The summary table of 6 rows (BenchmarkDotNet prints one table for each class):

| Method                | Mean          | Error        | StdDev       | Allocated |
|---------------------- |--------------:|-------------:|-------------:|----------:|
| FullLayout            | 216,420.29 ns | 3,058.270 ns | 3,521.908 ns |         - |
| DirtySubtree          |  10,814.32 ns |   860.558 ns | 2,455.220 ns |         - |
| CleanUpdate           |      36.19 ns |     0.954 ns |     2.707 ns |         - |
| PaddingChainLeafMark  |       4.919 ms |    0.0948 ms |    0.0886 ms |         - |
| MaxWidthChainLeafMark |       3.812 ms |    0.0519 ms |    0.0460 ms |         - |
| Draw                  |      20.64 us |     0.158 us |     0.140 us |         - |

The `Draw` row of the full run is 20.64 us. The demo run gave 21.26 us. The difference is noise (the demo run had 21 outliers).

Facts lines:

```
facts: tree elements 1000, bytes 746528, bytes per element 746.5
facts: percent padding chain depth 20: 3031 callback calls for one leaf mark (budget 4000)
facts: percent max width chain depth 20: 5674 callback calls for one leaf mark (budget 7400)
facts: plain column chain on a 1 MB stack: 507 levels pass (Release build)
```

All values match the expected values: `bytes per element 746.5` and 507 levels. No value differs. The `Allocated` column shows `-` for all 6 rows, so no draw allocation exists.

### Tasks 4.4 to 4.6: the docs

- `CHANGELOG.md`: one `### Added` line, the new `### Benchmarks` block (6 rows, machine block, 4 facts lines, the sample line, and 3 commands including the smoke command).
- `docs/technical-design.md`: section 3 (HelloPanel comment), section 4 (frame order step 2), section 14 (`TreeDrawBenchmark` and the sample bullet), section 16 (Phase 1 sentence).
- `human-testing.md`: the phase 1 to 3 lines were present. This run added the phase 4 demo line, the manual checks M1 to M5, and the validation layer check.

### The lint (task 4.7)

`ste-lint.py --fail-over 2.5` exit 0 for each file (hits per 100 words): `CHANGELOG.md` 1.70, `docs/technical-design.md` 1.34, `human-testing.md` 1.26, `context.md` 1.61, `implementation.md` 1.52. No file needed a fix. `implementation.md` has no edit.

### Native Dependencies

None. The feature adds no package, no `Directory.Packages.props` line, and no native file.

### Hand-offs

- The style-system epic: `HelloScreen.Build` sets the paint fields and the label values inline. The cascade can replace them. The screen is the first place to show a stylesheet.
- The input epic: the sample owns the `GlfwWindow`. Input can route from its events. The screen has no interactive element yet. Keep the UI region of the frame free of allocation. An input pass inside the region falls under the gate of the sample.
- The world-space epic: `VulkanContext.CreateTexture` shows the upload, the barriers, and the layout for a sampled texture. A render target needs a color attachment usage and its own render pass. Share `ImGuiFontService` and `ImGuiTextureService` between the screen renderer and a world-space renderer.

## Review Group 1: Results

### The change

- CR-1: `RunFrames` in `samples/HelloPanel/Program.cs` now has the local `releasedBytes` (start value -1). In each measured frame, after `ImGui.NewFrame()` and before `uiStartBytes`, the loop compares the counter with `releasedBytes`. When the thread allocated since the last release, the loop calls `GC.Collect(0, GCCollectionMode.Forced, blocking: true)` and stores the new counter value. The release is inside the frame region and outside the UI region. The remarks of `RunFrames` have a new paragraph with the release rule and its cause.
- CR-4: the `<returns>` texts of `RunWindow`, `RunImGui`, and `RunFrames` now read "The result of the run: the frame count, the draw list counts of the last frame, and the allocation sums of the measured frames."
- The release rule of the sample replaces the plain rule of "The Allocation Gate (phase 3)" in `implementation.md`. The plan text has no edit. The UI region keeps only the viewport set, `Update`, and `Draw`. The limit stays 0 bytes, the warm-up stays 3 frames, and the output lines and the exit codes do not change.

### Deviations from the fix text

- The comment of the fix code is three lines above the `if` statement, not one line inside it. The comment names the test helper `AllocationGate` by name only, because the sample does not link the test `Shared` folder.
- A comment on `releasedBytes` states its meaning and its start value.
- The `<returns>` texts wrap to three lines, because the line limit is 120 characters.

### The proof

- `dotnet build brutal-ui.sln`: 0 warnings, 0 errors.
- 5 smoke runs: each gave `120 frames rendered`, `draw commands 2, vertices 128`, `Update and Draw 0`, `in 117 frames`, and `exit=0`. The frame numbers were 21120, 22464, 22464, 22464, and 22464. The frame sum is a report only (Decision 4).
- The plant: a copy of `Label.cs` went to the scratch file `Label.cs.review-g1-plant`. `GC.KeepAlive(new object());` went to the start of `Label.PaintContent`. The planted run gave `Update and Draw 2808, frame 24792, in 117 frames.` on standard output and `HelloPanel: Update and Draw allocated 2808 bytes after warm-up. The limit is 0 bytes.` on standard error. The exit code was **1**. 2808 is 117 x 24 bytes, the same value as the plant of task 3.4. Thus the release does not hide a real allocation of the UI region.
- The restore: `cp` from the copy, then `diff` gave no output (exit 0), and `git diff --quiet -- src` gave `exit=0`. The build and the smoke run after the restore gave `Update and Draw 0, frame 19392, in 117 frames.` and `exit=0`.
- Probe 3 (scratch copy only): the fixed `Program.cs` went into the scratch copy of the review harness, with the GC pressure thread of probe 3 added (`HP_PRESSURE=1`). The scratch build needed `-p:NoWarn=CS0649` for the old probe field `VulkanContext.FailStep`. 72 runs, 4 at a time, of 120 frames: 72 runs with `exit=0`, 0 runs with a UI sum above 0. Each run had about 370 to 430 generation 0 GCs and 65 to 80 generation 2 GCs. Before the fix, the same probe gave 3 failed runs in 48.

### Follow-up

- Review group 3 adds the release rule to the "Sample projects" bullet of section 14 of `docs/technical-design.md` (CR-5).
- The cost of the release is one blocking GC of generation 0 in each measured frame in which the frame code allocated. In the plain sample, that is each measured frame of the smoke run and about the first 230 frames of a long run, then none.

## Review Group 2: Results

The main thread of `/autorun-epic` ran this group, at the request of the user, after the third orchestrator hand-back.

- CR-2: `VulkanContext.CreateTexture` keeps two flags, `submitted` and `copyComplete`. When the queue wait throws after the submit, the `finally` block calls `_device.WaitIdle()` in a `try` block with an empty `catch`, then frees the upload objects. The original exception reaches the caller. The step 6 comment names each path. Deviation: the code uses a second flag `copyComplete`, so the normal path makes no extra device wait.
- CR-3: `HelloScreen.FillChecker` computes the texel count in 64 bits, and it throws `ArgumentOutOfRangeException` when the count is above `int.MaxValue / 4`. The comment of `CreateTexture` now gives the right reason: the device limit check keeps each component at or below `MaxImageDimension2D`.
- Proof: `dotnet build brutal-ui.sln` gave 0 warnings and 0 errors. The smoke run gave `draw commands 2, vertices 128`, `Update and Draw 0, frame 19488, in 117 frames`, and `exit=0`.
- CR-3 probe (scratch project, a delegate on the internal method): `(2147483647, 2147483647)` and `(65536, 65536)` each throw `ArgumentOutOfRangeException` with the text "needs more bytes than a span can hold". No message states a negative byte count. `(4, 4)` with 64 bytes throws nothing. No probe file is in the repository.
- No test can make a real queue wait fail, so the CR-2 path has no run proof.

## Review Group 3: Results

- CR-5: the "Allocation tests" bullet of section 14 of `docs/technical-design.md` now states the release rule of the zero-byte gates, its cause, and the exception of `BrutalUi.ImGui.Tests`.
- The "Sample projects" bullet of section 14 now states the release rule of the sample (from CR-1).
- `CHANGELOG.md` has a new `### Fixed` heading with one line for the gate fix.
- Lint: `docs/technical-design.md` gives 1.35 per 100 words, and `CHANGELOG.md` gives 1.61. Each exits with 0.

---

## Gotchas & Learnings

- The XML docs of BRUTAL give no return type and no default value. A reflection dump of the assembly gives the full signature.
- `ByteSize64` has no implicit conversion from `int` or `long`. Write `new ByteSize64(value)`.
- `VkImageMemoryBarrier` gives 0 for each queue family index by default. Set `VK.QUEUE_FAMILY_IGNORED` when the barrier transfers no ownership.
- `DeviceExtensions.WriteMemory` does not flush. Use it on host-coherent memory only.
- `GlfwBackend.NewFrame()` allocates 192 managed bytes in each frame until the tiered JIT promotes its methods (about frame 230 in this sample). With `DOTNET_TieredCompilation=0`, it allocates 0 bytes. Do not gate a frame region that holds a backend call.
- A background GC can add up to 8 KB to `GC.GetAllocatedBytesForCurrentThread` of a thread that allocates nothing. Empty the allocation context with `AllocationGate.ReleaseAllocationContext()` (in `tests/BrutalUi.Core.Tests/Shared/`) right before an allocation measure. See "Phase 3 Fix 1" and "Phase 3 Fix 2: The Other Allocation Gates".
- An orchestrator agent can hand back while its child agents still run. This happened three times in this feature. Before a new run, wait for each child agent to stop, then read its result in `context.md` and the working tree.
- An internal static method of the sample takes a `Span<byte>`, so reflection `Invoke` cannot call it. A delegate from `MethodInfo.CreateDelegate` can call it from a scratch project.

---

## Testing Approach

### Unit Tests
- No new unit test. The existing gates (`ThousandElementAllocationTests`, `ElementAllocationTests`, and `ImGuiRendererAllocationTests`) cover `Update` and `Draw` with no GPU.

### Integration Tests
- The smoke command and `scripts/test-linux.sh` after each phase. Line 2 proves the draw. Line 3 proves the allocation rule. The exit code proves the contract.

### Manual Testing Checklist
- The manual checks M1 to M5 of the plan, on Windows.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: 120 frames rendered.` and `HelloPanel: last frame: draw commands 1, vertices <V>.` with a value of V above 0.
- [ ] Phase 2 demo: the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: 120 frames rendered.` and `HelloPanel: last frame: draw commands 2, vertices <V>.`
- [ ] Phase 3 demo: the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: bytes after warm-up: Update and Draw 0, frame <F>, in 117 frames.`
- [ ] Phase 4 demo: the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*TreeDrawBenchmark*'` and reads a table with the row `Draw` and `-` in the `Allocated` column.
- [ ] Phase 2 on a display: the developer runs the sample with no `--frames` argument and sees a 64 x 64 checker of light gray and orange cells with sharp edges, 12 pixels under the label, inside the panel.
- [ ] Phase 2 validation: this machine has no Khronos validation layer. The developer runs the smoke command with `VK_INSTANCE_LAYERS=VK_LAYER_KHRONOS_validation` on a machine with the layer, and sees no line with `Validation Error`.

---

## Next Steps

### Immediate (Today/This Session)
1. Commit the feature on `main` (`/autorun-epic` does it).
2. The manual checks of "Needs Human Verification".
3. `/review-epic imgui-renderer`.

---

## Open Questions

- (none)

---

## Session Notes

### 2026-10-21 00:55
- `/autorun-feature` in the called mode of `/autorun-epic` started the feature on the branch `feat/imgui-renderer/hello-panel-sample`.
- Phase 1, group 1: tasks 1.1 to 1.8 are done. The work stays in the working tree with no commit.
- Phase 2, group 1: tasks 2.1 to 2.7 are done. The work stays in the working tree with no commit.
- Phase 3, group 1: tasks 3.1 to 3.5 are done. The work stays in the working tree with no commit. `src/` has no change.
- Phase 3, fix 2: each 0-byte gate of `BrutalUi.Core.Tests` calls the shared helper. 480 runs under load gave 0 failed runs. The work stays in the working tree with no commit. `src/` has no change.

### 2026-10-21 03:26
- A third orchestrator run resumed the feature. The phase 3 verifier returned PASS with Demo: PASS. The two inbox bug lines are done.
- Phase 4, group 1 (`phase-4.md`): tasks 4.1 to 4.3 are done. Group 2 (`phase-4-g2.md`): tasks 4.4 to 4.7 are done.
- The phase 4 verifier (`phase-4-verify.md`) returned PASS: Debug and Release builds with 0 warnings, 1013 tests with 0 failed, `scripts/test-linux.sh` PASS, the diff check `exit=0`, each lint `exit=0`. Demo: PASS (`Draw` 20.95 us, `Allocated` `-`).
- Next: the cross-phase review from the brief `review.md`.

### 2026-10-21 04:20
- The third orchestrator handed back while review group 1 ran. Group 1 finished later: CR-1 and CR-4 are fixed, with 5 smoke runs and 72 load runs that pass.
- At the request of the user, the main thread of `/autorun-epic` ran review groups 2 and 3: CR-2, CR-3, and CR-5 are fixed. Each `CR-N` line of `code-review.md` is checked.
- Proof: 0 warnings, 1013 tests with 0 failed, `scripts/test-linux.sh` PASS, the `src/` diff check `exit=0`.
- Inbox: 2 lines done (the two phase 3 bug lines), 0 lines open. No bug goes to `docs/bugs/`.
- Next: the commit, the manual checks, and `/review-epic imgui-renderer`.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
