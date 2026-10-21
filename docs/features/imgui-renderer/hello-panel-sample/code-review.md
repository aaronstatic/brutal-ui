# imgui-renderer/hello-panel-sample - Code Review

**Reviewed:** 2026-10-21
**Reviewer:** Agent (cross-phase review of phases 1 to 4)
**Rules Applied:** 1. The create, frame, and teardown orders, 2. The texture upload, 3. The allocation gate, 4. Zero allocation and the hot path, 5. The test gates, 6. The benchmark, 7. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Open - 5 findings, 0 resolved

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 1 |
| LOW | 4 |

The sample follows "The Create Order", "The Frame Order", and "The Teardown Order" of the plan, with the documented changes of `context.md`. The upload has the right barriers, layouts, access masks, stages, and memory types. Each failure step of `CreateTexture` frees its objects and gives the original exception. The 7 zero-byte gates of `BrutalUi.Core.Tests` and the sample gate fail with a planted allocation. `TreeDrawBenchmark` has the form of `TreeLayoutBenchmark`, and its memory does not grow.

The MEDIUM finding is the open point of the orchestrator. The sample gate reads the counter with no release of the allocation context first. A background GC can then add 8008 bytes to the UI number with no allocation in the UI region (CR-1). The exposure is low today, because the plain sample runs no GC in 120 frames. The LOW findings are one failure path after a submit, one error message, and three doc gaps.

All probes ran in a scratch copy of the repository at `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/3877ec01-edbe-4549-8fe2-adfb5451e35e/scratchpad/cr-hello-panel-sample/repo`. A harness script (`harness.sh` in the parent folder) runs the sample N times, M at a time, each on its own Xvfb server, and counts the lines. The probe code in the copy reads environment variables, so one build covers each probe. No file of the repository changed, except this file.

1. **Baseline.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 1013, failed 0. The smoke command gives `HelloPanel: 120 frames rendered.`, `HelloPanel: last frame: draw commands 2, vertices 128.`, `HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.`, and `exit=0`.
2. **The warm-up edge.** `--frames 1` and `--frames 3` each give `Update and Draw 0, frame 0, in 0 frames.` and `exit=0`. `--frames 4` gives `Update and Draw 0, frame 192, in 1 frames.` and `exit=0`. Line 2 gives `draw commands 2, vertices 128` in each run. Line 3 follows "The Output Contract" with 0 and with 1 measured frame.
3. **The open point: can the sample gate fail with no real allocation?** Each row is one group of runs of 120 frames, unless the row states 600.

   | Group | Runs (parallel) | Extra setting | UI above 0 | UI bytes | GCs in the loop |
   |-------|-----------------|---------------|------------|----------|-----------------|
   | Plain sample | 24 (24) | none | 0 | - | not read |
   | Small generation 0 | 48 (24) | `DOTNET_GCgen0size=0x10000` | 0 | - | not read |
   | Small generation 0, 600 frames | 24 (24) | `DOTNET_GCgen0size=0x10000` | 0 | - | not read |
   | Probe build, no pressure | 8 (8) | none | 0 | - | 0 of each generation |
   | Probe build, GC pressure | 48 (24) | a pressure thread | 3 (exit 1) | 8008 each | about 390 generation 0, 72 generation 2 |
   | Pressure, one release after warm-up | 72 (24) | `once` | 2 (exit 1) | 8008 each | about 400 generation 0 |
   | Pressure, a release before each UI region | 72 (24) | `each` | 0 | - | 117 releases, 25 to 33 ms each under pressure |
   | No pressure, a release before each UI region | 4 (1) | `each` | 0 | - | 117 releases, 115 to 124 us each |
   | Pressure, a release when the thread allocated | 72 (24) | `dirty` | 0 | - | 64 to 71 releases |
   | No pressure, a release when the thread allocated, 600 frames | 3 (1) | `dirty` | 0 | - | 133 to 141 releases, 94 to 108 us each |
   | Pressure, `DOTNET_gcConcurrent=0` | 72 (24) | none | 0 | - | about 60 generation 0, 7 generation 2 |

   The pressure thread lives in the probe build only. It allocates byte arrays of 32 to 16160 bytes in a loop, keeps the last 4096, and starts a non-blocking GC of generation 2 after each 50000 arrays. It never touches the UI tree. The probe build records each measured frame with UI bytes. Each failed run had exactly 1 such frame. The frames were the rendered frames 9, 12, and 33 (with no release) and 34 and 64 (with one release after warm-up), counted from 0. Each step was 8008 bytes. Thus the step is the free part of one allocation context, as in "Phase 3 Fix 1". See CR-1.
4. **A failure inside `VulkanContext.CreateTexture`.** A size of `(32769, 1)` gives `ArgumentOutOfRangeException` with "32768 or less on this device". A pixel span of 10 bytes for 64 by 64 gives `ArgumentException` with "needs 16384 bytes". A size of `(int.MaxValue, int.MaxValue)` gives the limit exception first. A probe build throws an `InvalidOperationException` at one of 10 points: after the staging buffer, after the write, after the image, after the begin, after the record, after the queue wait, before the view, after the sampler, and at two points in the constructor of `SampledTexture` (before and after the memory bind). Each point ran 4500 times. Each call gave the injected exception and no other exception. The device limit `maxSamplerAllocationCount` is 4000, so a leaked sampler at the last point fails before call 4500. The driver gives `maxMemoryAllocationCount` 4294967295, so this probe cannot see a leak of memory. The code frees each memory object on each path. A loop of 4500 successful calls with two `Dispose` calls each gave no exception.
5. **`SampledTexture.Dispose` on a partly built object.** 4500 textures from the constructor only (an image and its memory, no view, no sampler) each took two `Dispose` calls with no exception. `CreateViewAndSampler` after `Dispose` gives `ObjectDisposedException`. A second `CreateViewAndSampler` gives `InvalidOperationException`.
6. **The size arithmetic.** Each component is at most `int.MaxValue`, so `(long)size.X * size.Y` is below 2^62, and the product with 4 is below 2^64. A wrapped result is at 2^63 or more, which is a negative `long`. A span length is 0 or more, so a wrapped value never passes the length check. In `CreateTexture`, the limit check comes first, so the product is at most 4 GiB and cannot wrap. `HelloScreen.FillChecker` has no limit check. `(int.MaxValue, int.MaxValue)` gives the message "needs -17179869180 bytes". See CR-3.
7. **`TreeDrawBenchmark`.** `UiContext.Draw` calls `renderer.Begin`, and `RecordingRenderer.Begin` sets the call count and the text count to 0. The call array and the text buffer grow by doubling to the peak of one frame and never shrink. Thus memory and time do not grow over the iterations. The setup has the order of `TreeLayoutBenchmark`: build, one `Update`, then two warm-up calls of each body. The phase 4 run gives `Draw` 20.64 us with a standard deviation of 0.140 us and `-` in the `Allocated` column.
8. **The gates with planted allocations.** `GC.KeepAlive(new object())` at the start of `Label.PaintContent` and of `LayoutEngine.Calculate`, in one build. `BrutalUi.Core.Tests`: each of the 7 zero-byte gates fails. `ElementAllocationTests.UpdateAndDrawAllocateNothingAfterWarmUp` gives 124800 bytes, and the 6 other gates give 2400 bytes each. The sample gives `Update and Draw 5616` (117 frames of 48 bytes), the gate line on standard error, and `exit=1`. Thus the release of the tests hides no real allocation. The copy of `src/` came back from the repository, and `diff` of the two `src/` folders without `bin` and `obj` gave no output.
9. **Rule checks.** In the repository, `git diff --quiet -- src Directory.Packages.props scripts samples/WorldSpace samples/HelloPanel/HelloPanel.csproj tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj` gives `exit=0`. The feature adds no package and no native file. `BrutalUi.Benchmarks` and `BrutalUi.ImGui.Tests` link the `Shared` folder, and both build with `AllocationGate.cs` with 0 warnings. Each new file uses a file-scoped namespace, `Brutal.Numerics` types, and XML doc comments on each type and member. `grep -nE "ImGui\.(Begin|Text|End)\(" samples/HelloPanel/*.cs` prints nothing.

### Known items from the build

1. **The frame region reports 17472 to 22464 bytes in 117 frames.** The source is `GlfwBackend.NewFrame()` of BRUTAL (192 bytes in each frame of tier-0 code). The frame number is a report only (Decision 4). Probe 3 adds a new angle: this backend allocation fills the allocation context of the thread in each frame before the UI region. Thus one release after warm-up does not protect the gate. See CR-1.
2. **This machine has no Khronos validation layer.** The validation run is a manual check. Not reported.
3. **The window has a fixed size, and the sample does not create the swapchain again (plan gap 1).** Not reported.
4. **No pixel test.** The visual check is manual (M2 and M3). Not reported.
5. **The teardown drops `ui` with its root and calls no `RemoveRoot`.** The sample ends the process. Not reported.
6. **The checker colors and the panel colors are choices of the build.** Not reported.
7. **At a display scale above 100 %, the nearest filter can give cells of uneven width (plan gap 6).** Not reported.
8. **`BrutalUi.ImGui.Tests` has two reads that follow an allocation of the test outside a frame.** The assembly runs with no parallel tests. Not reported.
9. **Line 1 and line 3 write "1 frames" for a count of 1.** Line 1 has no change from the old contract, and line 3 uses the same fixed form. Not reported.

---

## Findings

### CR-1: The sample gate can fail with no allocation in the UI region
**Severity:** MEDIUM
**Rule:** 3. The allocation gate
**File:** `samples/HelloPanel/Program.cs:387-443` (the loop), `samples/HelloPanel/Program.cs:350-355` (the remarks), `docs/features/imgui-renderer/hello-panel-sample/implementation.md:171-175`, `docs/technical-design.md:629`
**Problem:** The UI region reads the counter with no release of the allocation context first:

```csharp
glfwBackend.NewFrame();          // tier-0 code: 192 managed bytes in each frame up to about frame 230
vulkanBackend.NewFrame();
ImGui.NewFrame();

// The UI region. Put no other call inside it.
long uiStartBytes = GC.GetAllocatedBytesForCurrentThread();
```

`glfwBackend.NewFrame()` allocates on the same thread right before the region, so the allocation context of the thread has a free part when the region starts. "Phase 3 Fix 1" shows that a background GC can add that free part to `GC.GetAllocatedBytesForCurrentThread()`. Probe 3 shows it in the sample. With a pressure thread that never touches the UI tree, 3 of 48 runs gave `Update and Draw 8008` and `exit=1`. Each run had one frame with 8008 bytes. Rule 3 states that the gate cannot fail with no real allocation of the UI region. The exposure today is low: the plain sample ran 0 GCs in 120 frames. 96 runs with no pressure thread (24 plain runs and 72 runs with a small generation 0) gave 0 failures. The exposure grows with each allocation outside the UI region. Phase 3 of the project adds a store and a mode to the sample, and the input epic adds events. A game that copies the loop also has other threads that allocate. Then the smoke step of `scripts/test-linux.sh` and `scripts/test-windows.ps1`, and the manual check M4, can fail at random.

The fix option of the orchestrator (one blocking GC of generation 0 after the warm-up, outside the measured region) is not enough. The backend allocates again in each frame before the UI region, so the context fills again. Probe 3 gave 2 failed runs in 72 with this option.
**Fix:** Release the context before the UI region in each measured frame in which the thread allocated since the last release. Keep the release outside the UI region and inside the frame region:

```csharp
// Before the loop.
long releasedBytes = -1;

// In a measured frame, after ImGui.NewFrame() and before uiStartBytes.
if (measured && GC.GetAllocatedBytesForCurrentThread() != releasedBytes)
{
    // A background GC can add the free part of the allocation context to the counter. See AllocationGate.
    GC.Collect(0, GCCollectionMode.Forced, blocking: true);
    releasedBytes = GC.GetAllocatedBytesForCurrentThread();
}
```

Probe 3 gave 0 failed runs in 72 under pressure with this option. The cost is one GC of generation 0 of about 100 us in each frame in which the frame code allocated. In the plain sample, that is each measured frame of the smoke run (117) and about 140 frames of a long run, then 0. Update the remarks of `RunFrames`, "The Allocation Gate" of the plan (or a note in `context.md`), and the "Sample projects" bullet of section 14 with the release rule. A plant in `Label.PaintContent` must still give `exit=1`.

The other options and their cost:
- A release before each measured UI region with no condition: 0 failed runs in 72. It costs one GC of about 120 us in each frame for the full run, also after the tier-up.
- `<ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>` in `HelloPanel.csproj`: 0 failed runs in 72, with no cost in each frame. It changes the GC mode of the sample. A game that copies the project then gets blocking full GCs. The fix of the tests did not use it for the same reason.
- One release after warm-up: not enough (2 failed runs in 72).
**Effort:** Low

### CR-2: A failed queue wait frees objects that the GPU can still use
**Severity:** LOW
**Rule:** 2. The texture upload
**File:** `samples/HelloPanel/VulkanContext.cs:352-373`, `samples/HelloPanel/VulkanContext.cs:379-383`
**Problem:** The inner `finally` frees the command buffer, the staging buffer, and the staging memory on each path. Its comment states the condition:

```csharp
// Step 6: the copy is complete, or the submit did not occur. Free the upload objects.
```

When `GraphicsQueue.Submit` succeeds and `GraphicsQueue.WaitIdle()` throws (`VK_ERROR_OUT_OF_HOST_MEMORY`, `VK_ERROR_OUT_OF_DEVICE_MEMORY`, or `VK_ERROR_DEVICE_LOST`), the copy can still be pending. The comment is then false. `vkFreeCommandBuffers` on a pending command buffer and `vkDestroyBuffer` on a buffer in use break valid usage rules. The outer `catch` then destroys the image that the pending copy writes. The probe build cannot make a real queue wait fail, so the probe did not show a fault. The path occurs only after a device fault, and the sample then ends with exit code 1.
**Fix:** Keep a flag `submitted` that the code sets after `Submit`. When the flag is set and the queue wait throws, call `_device.WaitIdle()` in a `try` block with an empty `catch` before the frees. Or skip the frees on that path and state the leak in a comment. Change the step 6 comment to name the path.
**Effort:** Low

### CR-3: The size check of `FillChecker` writes a wrapped byte count
**Severity:** LOW
**Rule:** 2. The texture upload (the size and length checks)
**File:** `samples/HelloPanel/HelloScreen.cs:87-93`, `samples/HelloPanel/VulkanContext.cs:300-301`
**Problem:** Both methods compute the byte count in 64 bits:

```csharp
long byteCount = (long)size.X * size.Y * BytesPerTexel;
```

A wrapped value is negative, so no overflow passes the length check (probe 6). `FillChecker` has no limit check. For `(int.MaxValue, int.MaxValue)`, the product wraps, and the exception message states "needs -17179869180 bytes". The comment of `CreateTexture` states "The product uses 64 bits, so a large size cannot overflow". The product cannot overflow there only because the limit check of the device comes first. The comment gives the wrong reason.
**Fix:** In `FillChecker`, compute the count with `Math.BigMul(size.X, size.Y)` and a multiplication by 4 in `checked` context, or compare `size.X * size.Y` with `int.MaxValue / 4` first and throw the size exception. In `CreateTexture`, change the comment: the limit check keeps each component at or below `MaxImageDimension2D`, so the product fits in 64 bits.
**Effort:** Low

### CR-4: The `<returns>` texts of the run methods name only the counts of line 2
**Severity:** LOW
**Rule:** 7. `AGENTS.md` and the docs
**File:** `samples/HelloPanel/Program.cs:222`, `samples/HelloPanel/Program.cs:277`, `samples/HelloPanel/Program.cs:372`
**Problem:** Phase 3 added `UiAllocatedBytes`, `FrameAllocatedBytes`, and `MeasuredFrames` to `RunResult`. The three methods still state the phase 1 result:

```csharp
/// <returns>The frame count and the draw list counts of the last frame.</returns>
```

**Fix:** Change each text to "The result of the run: the frame count, the draw list counts of the last frame, and the allocation sums of the measured frames." Or refer to `RunResult`.
**Effort:** Low

### CR-5: The docs do not record the release rule of the test gates
**Severity:** LOW
**Rule:** 7. `AGENTS.md` and the docs
**File:** `CHANGELOG.md:7-15`, `docs/technical-design.md:621`, `docs/technical-design.md:623`, `docs/technical-design.md:626`
**Problem:** Fix 1 and Fix 2 of phase 3 changed 7 gates of `BrutalUi.Core.Tests`. Each gate now calls `AllocationGate.ReleaseAllocationContext()` right before its measured region. Section 14 describes each gate in detail ("assert zero allocated bytes after warm-up"), but it does not state the release or its cause. `CHANGELOG.md` has no line for the new helper or for the fix of the flaky gate. A developer who adds a gate in a later epic reads section 14 and does not find the rule. The doc comment of `AllocationGate` and `context.md` hold the rule, but the plan does not send a reader there.
**Fix:** Add one sentence to the "Allocation tests" bullet of section 14: each zero-byte gate calls `AllocationGate.ReleaseAllocationContext()` right before its measured region, because a background GC can add the free part of the allocation context to the counter. Name the exception of `BrutalUi.ImGui.Tests` (no parallel tests). Add one line to `CHANGELOG.md`, for example under `### Fixed`: the zero-byte gates of `BrutalUi.Core.Tests` release the allocation context before each measure, so a background GC no longer fails them (imgui-renderer/hello-panel-sample). When CR-1 adds a release to the sample, add the same rule to the "Sample projects" bullet.
**Effort:** Low

---

## What's Already Good

- The create order and the teardown order follow the plan. `checker` is declared after `vulkan` and outlives the backends. `WaitIdle` runs before `textures.Remove`, the none handle has a guard, and the `AddVulkan` call inside the `try` block also removes the texture when `AddRoot` or `RunFrames` throws.
- The upload has the barriers, the stages, and the access masks of "The Texture Upload". Both barriers set `QUEUE_FAMILY_IGNORED`. The staging memory is host-visible and host-coherent, so `WriteMemory` needs no flush, and the image memory is device-local.
- Each of 10 failure points of the upload gave the injected exception and no second exception in 4500 calls (probe 4). `SampledTexture` is safe to dispose two times and in a partly built state (probe 5).
- The UI region holds only the viewport set, `Update`, and `Draw`. The clamp guards the setter of `ViewportSize` against the first `io.DisplaySize` of (-1, -1). The loop uses only locals, with no LINQ, no lambda, no boxing, and no string format.
- Line 3 is right with 0 and with 1 measured frame (probe 2). The gate reads the UI sum only, and a plant gives `exit=1` and the gate line (probe 8).
- The 7 zero-byte gates of the tests call the release right before the measure, and each gate still fails with a plant (probe 8). `AllocationGate` uses no test framework type, so the linked projects build.
- `TreeDrawBenchmark` follows the form of `TreeLayoutBenchmark`. Its memory and its time do not grow over the iterations (probe 7).
- `CHANGELOG.md`, sections 3, 4, 14, and 16 of `docs/technical-design.md`, and the `## hello-panel-sample` section of `human-testing.md` match the source and the smoke output, except for the gaps of CR-4 and CR-5.

---

## Recommended Fix Order

| Order | Finding | File | Severity | Effort |
|-------|---------|------|----------|--------|
| 1 | CR-1: The sample gate and the background GC | `samples/HelloPanel/Program.cs` | MEDIUM | Low |
| 2 | CR-4: The `<returns>` texts of the run methods | `samples/HelloPanel/Program.cs` | LOW | Low |
| 3 | CR-2: The failed queue wait | `samples/HelloPanel/VulkanContext.cs` | LOW | Low |
| 4 | CR-3: The wrapped byte count | `samples/HelloPanel/HelloScreen.cs` | LOW | Low |
| 5 | CR-5: The release rule in the docs | `docs/technical-design.md` | LOW | Low |

CR-1 comes first, because it changes the loop and the remarks of `RunFrames`. CR-4 changes the doc comments of the same file, so one fix agent can do both. CR-2 and CR-3 change the upload and the size checks, so one fix agent can do both. CR-5 comes last, because it also records the release rule of CR-1 in section 14. After CR-1, run the smoke command 5 times and the plant of task 3.4 one time.

---

## Progress

- [x] CR-1: The sample gate can fail with no allocation in the UI region
- [x] CR-2: A failed queue wait frees objects that the GPU can still use
- [x] CR-3: The size check of `FillChecker` writes a wrapped byte count
- [x] CR-4: The `<returns>` texts of the run methods name only the counts of line 2
- [x] CR-5: The docs do not record the release rule of the test gates
