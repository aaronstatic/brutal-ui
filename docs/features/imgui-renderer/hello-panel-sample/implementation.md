# Hello Panel Sample - Implementation Plan

**Epic:** imgui-renderer (feature 4 of 4)
**Status:** In Progress
**Started:** 2026-10-21
**Target Completion:** TBD
**Last Updated:** 2026-10-21 00:55

---

## Quality Bar

**Target quality:** A sample and a GPU integration gate. HelloPanel is the smoke run of both test scripts, and it is the first screen that a game developer reads. It is the only code that runs the full path from `Element` to the Vulkan backend. A wrong create order, frame order, or teardown order fails on a GPU only, and no unit test sees it.

- **The exit code contract stays:** The sample keeps `--frames N` and the exit codes 0, 1, and 2. The argument check runs before the first GLFW call, so a usage error needs no GPU. Line 1 of standard output stays `HelloPanel: <count> frames rendered.`
- **The GPU order is safe:** The sample follows "The Create Order", "The Frame Order", and "The Teardown Order" of this plan. When the Khronos validation layer exists on the machine, a run with the layer gives no validation error.
- **Zero allocation:** `ViewportSize`, `Update`, and `Draw` allocate 0 managed bytes after warm-up in the sample. The sample measures the number in each run and exits with code 1 when it is above 0. A planted allocation proves the gate.
- **No change outside the sample, the benchmark, and the docs:** No file of `src/` changes. `Directory.Packages.props` does not change. `samples/WorldSpace/` does not change (A8). No native dependency comes in (A9).
- **A readable sample:** The screen lives in one short file, `HelloScreen.cs`, with named constants and XML doc comments. A game developer can copy it.

---

## Executive Summary

This feature turns the HelloPanel sample into a static brutal-ui screen and closes Phase 1 of the technical design. The ImGui text window goes (Q5). In its place, the sample builds a tree with a panel, a `Label`, and an `Image`, and `ImGuiRenderer` draws the tree on the background draw list. The Vulkan backend renders the list as before.

The image is a checker texture of 64 by 64 texels that the sample fills in code (Q4). `VulkanContext` gets one method that uploads the pixels through a staging buffer and gives a `SampledTexture` (the image, the memory, the view, and the sampler). The sample registers the texture with `ImGuiTextureService.AddVulkan` (Q6) and removes it after `vulkan.WaitIdle()` and before the backend dispose.

The frame loop calls `UiContext.Update` and `UiContext.Draw` after `ImGui.NewFrame()`. The sample measures the managed bytes of that region after 3 warm-up frames, prints the number, and fails with exit code 1 when the number is above 0. Both test scripts run the sample with no change, so the smoke run of each script becomes a GPU allocation gate. A new `TreeDrawBenchmark` measures the draw walk, and `CHANGELOG.md` records a new benchmark run, the facts lines, and the sample number. The WorldSpace sample stays a stub (A8).

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (reads the sample to learn the create order, the frame order, the texture registration, and the teardown order)
- [x] **The maintainer** (runs `scripts/test-linux.sh` and `scripts/test-windows.ps1` and reads one PASS line, and reads the allocation number in the smoke log)
- [x] **The later epics** (the style-system and input epics show their results on this screen. The world-space epic reuses the upload path for its render target)
- [ ] **The markup author** (not served. The markup comes in a later epic)

**User Subsets Served:**
- [x] **Simple** - Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj` and see a panel with text and an image.
- [x] **Complex** - Read `Program.cs` for the full life cycle of the services, the context, the backends, and a GPU texture. Read the smoke log for the allocation number.

**UX Approach:**
- The window shows only the brutal-ui tree. No ImGui window covers it.
- A usage error writes the usage line and the reason to standard error and exits with code 2, with no window.
- An allocation in the UI region after warm-up writes one line to standard error that names the byte count and the limit, and the exit code is 1.

---

## Goals

### Primary Goals
1. Show the full path from `Element` to the Vulkan backend on screen: a panel, a label, and an image (Q4, Q5).
2. Keep the `--frames N` contract, so both scripts run the sample as the GPU smoke run with no script change.
3. Prove in the GPU sample that `Update` and `Draw` allocate nothing after warm-up.
4. Record the benchmark numbers and the draw allocation number, and close Phase 1.

### Success Criteria
- [ ] The smoke run prints `HelloPanel: 120 frames rendered.`, `HelloPanel: last frame: draw commands 2, vertices <V>.` with V above 0, and a bytes line with `Update and Draw 0` and `in 117 frames`. The exit code is 0.
- [ ] `scripts/test-linux.sh` prints its PASS line with no script change.
- [ ] A planted allocation in `Label.PaintContent` makes the smoke run exit with code 1.
- [ ] `TreeDrawBenchmark` shows `-` in the `Allocated` column.
- [ ] `CHANGELOG.md` holds the new benchmark table, the 4 facts lines, and the sample line.
- [ ] On Windows, a person sees the panel, the text, and the checker image, and no ImGui window (MANUAL).

---

## Architecture Overview

### File Tree (end state)

```
samples/HelloPanel/
├── HelloPanel.csproj        # no change (BrutalUi.Core, BrutalUi.ImGui, Glfw, Vulkan, Vulkan.Abstractions)
├── Program.cs               # edit: create order, frame order, teardown order, output lines, gate
├── HelloScreen.cs           # new: the tree of the screen and the checker pixels
├── SampledTexture.cs        # new: owns the image, the memory, the view, and the sampler
└── VulkanContext.cs         # edit: CreateTexture (staging upload)
samples/WorldSpace/          # no change (A8)
tests/BrutalUi.Benchmarks/
└── TreeDrawBenchmark.cs     # new: Draw on ThousandElementTree into RecordingRenderer
CHANGELOG.md                 # edit: Added line, new Benchmarks block
docs/technical-design.md     # edit: sections 3, 4, 14, 16
docs/features/imgui-renderer/human-testing.md   # edit: the hello-panel-sample section
```

No file of `src/` changes. The plant of task 3.4 changes `Label.cs` for one run only, and a copy restores it.

### The Screen

```
root            auto size = the viewport, padding 24 px, AlignItems FlexStart, no paint
└── panel       column, AlignItems FlexStart, padding 16 px, border 2 px, radius 8,
    │           fill PanelColor, border BorderColor (both alpha 255)
    ├── Label   "Hello, brutal-ui", font 0 (the ImGui default font), size 26, white
    └── Image   the checker texture, 64 x 64, top margin 12 px, StretchToFill, white tint
```

With the default font, the text is 224 by 26 pixels (16 glyphs of 14 pixels). The panel is then 260 by 138 pixels at (24, 24). The paint order is the panel, the label, and the image. The panel and the label use the atlas texture, so they share draw command 0. The image binds the checker texture, so it opens draw command 1. Phase 1 has no image, so it gives 1 draw command.

`HelloScreen.FillChecker` writes 64 by 64 RGBA texels in cells of 8 texels with two opaque colors. The sampler uses the nearest filter and clamp to edge, so each cell has sharp edges.

### The Create Order

1. `Glfw.Init`, the window, and `VulkanContext` (no change).
2. Fill the checker pixels into a `byte[]`. Call `vulkan.CreateTexture(pixels, new int2(64, 64))`. The texture is ready in `ShaderReadOnlyOptimal` when the call returns.
3. `ImGui.CreateContext()`, `io.IniFilename = default`, and `ImGui.StyleColorsDark()` (no change).
4. No font load. Font 0 is the ImGui default font (Q3).
5. `ImGuiFontService`, `ImGuiTextureService`, and `ImGuiRenderer` (named `uiRenderer`).
6. `UiContext ui = new(new UiContext.CreateInfo { Measurer = uiRenderer })`.
7. `GlfwBackend` (named `glfwBackend`), then `VulkanBackend` (named `vulkanBackend`, descriptor pool 8).
8. `textures.AddVulkan(vulkanBackend, checker.Sampler, checker.View, VkImageLayout.ShaderReadOnlyOptimal, checker.Size)`. The pool now holds 2 of 8 sets: the font atlas and the checker.
9. `ui.AddRoot(HelloScreen.Build(handle))`.

Phase 1 runs steps 1, 3 to 7, and 9 (with no image). Phase 2 adds steps 2 and 8.

### The Frame Order

1. `Glfw.PollEvents()`, the wait while the window is minimized, and the close check (no change).
2. `glfwBackend.NewFrame()`, `vulkanBackend.NewFrame()`, and `ImGui.NewFrame()`.
3. The UI region: `ui.ViewportSize = new float2(MathF.Max(size.X, 0f), MathF.Max(size.Y, 0f))` with `size = io.DisplaySize`, then `ui.Update(io.DeltaTime)`, then `ui.Draw(uiRenderer)`.
4. `ImGui.Render()`. Read `CmdBuffer.Count` and `VtxBuffer.Count` of `ImGui.GetBackgroundDrawList(ImGui.GetMainViewport())` into two locals, as the ImGui tests do.
5. `vulkan.BeginFrame()`, `vulkanBackend.RenderDrawData(ImGui.GetDrawData(), commandBuffer)`, and `vulkan.EndFrame()` (no change).
6. `framesRendered++`.

The order follows "Frame order" of `docs/technical-design.md` section 4. It also meets both sibling hand-offs: `Draw` runs between `ImGui.NewFrame` and `ImGui.Render`, and `Update` runs with a current ImGui context. The clamp guards the setter of `ViewportSize`, which throws for a negative value. ImGui starts `io.DisplaySize` at (-1, -1) until the first platform frame.

### The Teardown Order

1. The loop ends.
2. `vulkan.WaitIdle()` in the `finally` block of `RunImGui` (no change).
3. `textures.Remove(handle)` when the handle is not the none handle. The backend frees the descriptor set.
4. The `using` declarations of `RunImGui` dispose `vulkanBackend`, then `glfwBackend`.
5. `ImGui.DestroyContext` in the `finally` block of `RunWindow` (no change).
6. The `using` declarations of `RunWindow` dispose `checker` (the sampler, the view, the image, and the memory), then `vulkan`, then the window.
7. `Glfw.Terminate()` (no change).

`checker` is declared after `vulkan` in `RunWindow`, so it disposes first and the device is still alive. It outlives the backend, so no descriptor set names a destroyed view while the backend lives.

### The Texture Upload (`VulkanContext.CreateTexture`)

1. Create a staging buffer of `size.X * size.Y * 4` bytes with `TransferSrc` usage in host-visible, host-coherent memory. Write the pixels into it.
2. Create the image: 2D, `R8G8B8A8UNorm`, 1 mip level, 1 layer, 1 sample, optimal tiling, usage `TransferDst | Sampled`, exclusive sharing, initial layout `Undefined`, in device-local memory.
3. Allocate one primary command buffer from the command pool of the context. Begin it with `OneTimeSubmit`.
4. Record a barrier from `Undefined` to `TransferDstOptimal` (source stage `TopOfPipe`, no access, destination stage `Transfer`, `TransferWrite`). Record `CopyBufferToImage` for the full extent. Record a barrier from `TransferDstOptimal` to `ShaderReadOnlyOptimal` (source stage `Transfer`, `TransferWrite`, destination stage `FragmentShader`, `ShaderRead`).
5. End the command buffer, submit it to `GraphicsQueue`, and wait for the queue (`WaitIdle` or a fence).
6. Free the command buffer. Destroy the staging buffer and free its memory.
7. Create the image view (2D, the same format, the color aspect) and the sampler (nearest filter for min and mag, clamp to edge on U, V, and W, no anisotropy).

The format is UNORM, not sRGB. The swapchain is UNORM, and ImGui gives its colors as sRGB bytes (the remark of `SelectSurfaceFormat`). A UNORM texture keeps the bytes, so the checker colors match the `Rgba8` colors of the panel. `R8G8B8A8UNorm` with optimal tiling supports `Sampled` and `TransferDst` on each Vulkan device (a format that the specification requires).

### The Output Contract

| Case | Exit code | Standard output | Standard error |
|------|-----------|-----------------|----------------|
| Success | 0 | Line 1, line 2, line 3 | Nothing |
| The UI region allocates after warm-up | 1 | Line 1, line 2, line 3 | `HelloPanel: Update and Draw allocated <B> bytes after warm-up. The limit is 0 bytes.` |
| A runtime failure (no change) | 1 | Nothing | `HelloPanel: <exception>` |
| A usage error (no change) | 2 | Nothing | The usage line and the reason |

- Line 1: `HelloPanel: <count> frames rendered.` (no change)
- Line 2: `HelloPanel: last frame: draw commands <C>, vertices <V>.` (phase 1)
- Line 3: `HelloPanel: bytes after warm-up: Update and Draw <B>, frame <F>, in <M> frames.` (phase 3)

`<M>` is the frame count minus 3, and 0 for a run of 3 frames or less. The scripts read the exit code only, so they need no change.

### The Allocation Gate (phase 3)

- `WarmUpFrames` is 3. The first 3 rendered frames are not measured. The paint stack of `UiContext` and the glyph bake of ImGui settle in them.
- The UI region is step 3 of "The Frame Order". The frame region is steps 2 to 6. Each region reads `GC.GetAllocatedBytesForCurrentThread()` right before and right after, and adds the difference to a `long` sum.
- The gate reads the UI sum only (Decision 4). The frame sum is a report.

### Data Flow

```
HelloScreen.Build ──► UiContext (root) ──► Update ──► LayoutEngine ──► Label.MeasureLabel ──► ImGuiRenderer.MeasureText
                                      └──► Draw ──► PaintWalk ──► ImGuiRenderer (Rect, Text, Image) ──► background ImDrawList
VulkanContext.CreateTexture ──► SampledTexture ──► ImGuiTextureService.AddVulkan ──► TextureHandle ──► Image
ImGui.Render ──► ImDrawData ──► VulkanBackend.RenderDrawData ──► swapchain image
```

---

## Implementation Phases

The phases follow the slice rules of `.beast-mode/phase-handoff.md` section 11. Each phase runs end to end on the GPU path and ends with the smoke run.

**The smoke command** (the line of `scripts/test-linux.sh` with no time limit, from the repository root, after a build):

```
env -u WAYLAND_DISPLAY -u VK_DRIVER_FILES -u VK_ADD_DRIVER_FILES XDG_SESSION_TYPE=x11 VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames 120; echo "exit=$?"
```

---

### Phase 1: A Panel and a Label on the GPU
**Goal:** The sample draws a brutal-ui panel with one label through `ImGuiRenderer` in place of the ImGui text window, and it keeps the `--frames N` contract.
**Demo:** the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: 120 frames rendered.` and `HelloPanel: last frame: draw commands 1, vertices <V>.` with a value of V above 0.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** The create order, the frame order, and the exit code contract are load-bearing. A wrong order fails on the GPU only. Run the smoke command after each change of `Program.cs`. When the draw command count is not 1, find the cause before a change of the demo line.

**Tasks:**

Group A (3 files):
- [ ] 1.1 Check the start state. Confirm that `src/BrutalUi.Core/Elements/Label.cs` and `UiContext.CreateInfo` exist. Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Record the test total in `context.md` (1013 is the expected value). Run the smoke command and record the output and the exit code.
- [ ] 1.2 Write `samples/HelloPanel/HelloScreen.cs`: `internal static class HelloScreen` with `public static Element Build()`. Build the root, the panel, and the label of "The Screen". Put each size and each color into a named constant. Set the label text in the create-info (`Text = "Hello, brutal-ui"`). When the analyzer `BRUTALCORE0011` rejects the literal, call `label.SetText("Hello, brutal-ui"u8)` after the constructor. Add XML doc comments.
- [ ] 1.3 Edit `RunImGui` in `Program.cs`. Add steps 5 and 6 of "The Create Order" before the backends. Rename the backend locals to `glfwBackend` and `vulkanBackend`. Call `ui.AddRoot(HelloScreen.Build())` after the backends. Pass `ui` and `uiRenderer` to `RunFrames`.
- [ ] 1.4 Edit `RunFrames`. Remove the calls `ImGui.Begin`, `ImGui.Text`, and `ImGui.End`. Add steps 3 and 4 of "The Frame Order". Keep the minimize wait and the close check.
- [ ] 1.5 Replace the `int` result of `RunWindow`, `RunImGui`, and `RunFrames` with a private `readonly record struct RunResult` (the frame count, the draw command count, and the vertex count of the last frame). `Run` writes line 1 with no change, then line 2 of "The Output Contract". The exit codes do not change.
- [ ] 1.6 Update the summary of `Program` and the remarks of `RunFrames`. They state that the sample draws a brutal-ui tree, the frame order, and the output lines. Remove the text "draws one ImGui text label".
- [ ] 1.7 Run the checks in one command: the build (0 warnings), then `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- <args>` with `--frames abc`, `--frames 0`, `--frames`, and `--bogus` (each `exit=2` and a `Usage:` line), then the smoke command (`exit=0` and the two lines), then `scripts/test-linux.sh; echo "exit=$?"` (the PASS line and `exit=0`).
- [ ] 1.8 Record in `context.md`: the two output lines, the draw command count, the vertex count, and each deviation from "The Create Order" or "The Frame Order".

**Estimated Steps:** about 40 (8 tasks, about five steps for one task)

**Files (3):** `samples/HelloPanel/Program.cs`, `samples/HelloPanel/HelloScreen.cs`, `context.md`.

**Acceptance Criteria:**
- [ ] `grep -nE "ImGui\.(Begin|Text|End)\(" samples/HelloPanel/*.cs` prints nothing.
- [ ] The smoke command prints the two lines with `draw commands 1` and a vertex count above 0, and `exit=0`.
- [ ] Each of the 4 usage cases gives `exit=2`.
- [ ] `scripts/test-linux.sh` prints the PASS line.

---

### Phase 2: The Procedural Image
**Goal:** The sample fills a checker texture in code, uploads it through `VulkanContext`, registers it with `AddVulkan`, and shows it as an `Image` in the panel. The teardown frees each GPU object in a safe order.
**Demo:** the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: 120 frames rendered.` and `HelloPanel: last frame: draw commands 2, vertices <V>.` The second command binds the checker texture.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** The GPU resource order and the Vulkan synchronization are load-bearing. A wrong layout or a wrong destroy order can pass on the NVIDIA driver and fail on Windows. Follow "The Texture Upload" and "The Teardown Order" step by step. When the validation layer exists, a validation error stops the phase.

**Tasks:**

Group A (5 files):
- [ ] 2.1 Read the API before the code. Read the XML docs of `Brutal.Vulkan` 2026.10.2 and `Brutal.Vulkan.Abstractions` 2026.10.2 in the NuGet cache (`~/.nuget/packages/ahwoo.brutal.vulkan*/2026.10.2/lib/net10.0/*.xml`). Record in `context.md` the signatures for each step of "The Texture Upload": the create and destroy calls of an image, a buffer, a view, and a sampler, the memory calls (`DeviceExtensions.AllocateMemory` and `WriteMemory`, or `ImageEx` and `BufferEx` on `DeviceEx`), `CommandBuffer.PipelineBarrier`, `CommandBuffer.CopyBufferToImage`, the command buffer allocate and free calls, and the queue wait. Use a helper of `Brutal.Vulkan.Abstractions` when it covers a step. Else use the raw call. Record the choice.
- [ ] 2.2 Write `samples/HelloPanel/SampledTexture.cs`: `internal sealed class SampledTexture : IDisposable`. It owns the image, its memory, the image view, and the sampler. It has the properties `View`, `Sampler`, and `Size` (`int2`). `Dispose` destroys the sampler, the view, the image, and the memory, in that order. A second call does nothing. A partly built object is safe to dispose, as in `VulkanContext`.
- [ ] 2.3 Add `public SampledTexture CreateTexture(ReadOnlySpan<byte> rgba, int2 size)` to `VulkanContext`. The method throws `ArgumentOutOfRangeException` when a size component is 0 or less, and `ArgumentException` when `rgba.Length` is not `size.X * size.Y * 4`. Then it runs steps 1 to 7 of "The Texture Upload". On a failure, it frees each object that it created and throws. Add one sentence to the class remarks: the context also uploads a sampled texture before the first frame.
- [ ] 2.4 Edit `HelloScreen.cs`. Add `public static void FillChecker(Span<byte> rgba, int2 size)` (cells of 8 texels, two opaque colors). Change `Build()` to `Build(TextureHandle checker)`, and add the `Image` of "The Screen" after the label. Write `BrutalUi.Core.Image` in full when the name is ambiguous.
- [ ] 2.5 Edit `RunWindow`. After the `VulkanContext`, fill a `byte[]` of `64 * 64 * 4` bytes with `FillChecker`. Declare `using SampledTexture checker = vulkan.CreateTexture(pixels, size);` after the `vulkan` declaration. Pass `checker` to `RunImGui`.
- [ ] 2.6 Edit `RunImGui`. Add step 8 of "The Create Order" after the backends, and pass the handle to `HelloScreen.Build`. Declare the handle as `TextureHandle handle = default` before the `try` block. In the `finally` block, after `vulkan.WaitIdle()`, call `textures.Remove(handle)` when `handle.IsNone` is false.
- [ ] 2.7 Run the checks: the build, the smoke command (`exit=0` and `draw commands 2`), and `scripts/test-linux.sh` (the PASS line). When `/usr/share/vulkan/explicit_layer.d/VkLayer_khronos_validation.json` exists, run the smoke command again with `VK_INSTANCE_LAYERS=VK_LAYER_KHRONOS_validation` and expect no line with `Validation Error`. Else record "no validation layer on this machine" in `context.md`. Record the two counts in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (5):** `samples/HelloPanel/VulkanContext.cs`, `samples/HelloPanel/SampledTexture.cs`, `samples/HelloPanel/HelloScreen.cs`, `samples/HelloPanel/Program.cs`, `context.md`.

**Acceptance Criteria:**
- [ ] The smoke command prints `draw commands 2` and `exit=0`.
- [ ] `grep -rn "Brutal.Texture" samples Directory.Packages.props` prints nothing, and the sample reads no image file.
- [ ] `textures.Remove` comes after `vulkan.WaitIdle()` and before the backend dispose. `checker` disposes after the ImGui context and before `vulkan`.
- [ ] The validation run gives no `Validation Error` line, or `context.md` records that no layer exists.

---

### Phase 3: The Allocation Gate of the Sample
**Goal:** The sample measures the managed bytes of `Update` and `Draw` after warm-up, prints the number, and exits with code 1 when the number is above 0. A planted allocation proves the gate.
**Demo:** the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: bytes after warm-up: Update and Draw 0, frame <F>, in 117 frames.`
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)

**Tier note:** The zero-allocation rule is load-bearing. When the UI number is above 0, find the cause in `src/` or in the sample before a change of the gate. Do not move a call out of the UI region to pass the gate.

**Tasks:**

Group A (3 files):
- [ ] 3.1 Add the two regions of "The Allocation Gate" to `RunFrames`. Add the constant `WarmUpFrames = 3`. Add the UI sum, the frame sum, and the count of measured frames to `RunResult`. The regions use only locals and add no allocation.
- [ ] 3.2 Edit `Run`. Write line 3 of "The Output Contract" after line 2. When the UI sum is above 0, write the gate line to standard error and return `ExitFailure`. Update the remarks of `RunFrames` with the two regions and the gate. Replace the sentence "The loop allocates no managed memory" with the measured rule.
- [ ] 3.3 Run the smoke command 5 times. Expect `Update and Draw 0`, `in 117 frames`, and `exit=0` each time. Record the frame number of each run in `context.md`. When the frame number is above 0, find the source (for example a backend call) and record it as a finding. It does not fail the run (Decision 4).
- [ ] 3.4 Prove that the gate blocks. Copy `src/BrutalUi.Core/Elements/Label.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Label.PaintContent`. Build. Run the smoke command. Expect a UI number above 0, the gate line on standard error, and `exit=1`. Restore the file from the copy. Run `diff` on the two files and expect no output. Build and run the smoke command again, and expect `exit=0`. Record the two exit codes and the planted number in `context.md`. Do not use `scripts/test-linux.sh` for the planted run, because the unit tests stop the script first.
- [ ] 3.5 Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` and expect the total of task 1.1 and 0 failed.

**Estimated Steps:** about 30 (5 tasks, about six steps for one task)

**Files (3):** `samples/HelloPanel/Program.cs`, `context.md`, and a temporary edit of `src/BrutalUi.Core/Elements/Label.cs` (restored).

**Acceptance Criteria:**
- [ ] 5 smoke runs give `Update and Draw 0` and `exit=0`.
- [ ] The planted run gives `exit=1` and the gate line. The restored `Label.cs` is the same as the copy.
- [ ] `git diff --quiet -- src; echo "exit=$?"` prints `exit=0` after the restore.

---

### Phase 4: The Draw Benchmark and the Record
**Goal:** A benchmark measures the draw walk on the 1000-element tree. `CHANGELOG.md` records the new benchmark run, the facts lines, and the sample number. The docs close Phase 1.
**Demo:** the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*TreeDrawBenchmark*'` and reads a table with the row `Draw` and `-` in the `Allocated` column.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tier note:** This phase adds one benchmark class and docs. When the `Allocated` column shows a value, stop and report it. That value is a draw allocation, and the tier note of phase 3 applies.

**Tasks:**

Group A (2 files):
- [ ] 4.1 Write `tests/BrutalUi.Benchmarks/TreeDrawBenchmark.cs` in the form of `TreeLayoutBenchmark`. It has `[MemoryDiagnoser]` and a setup that builds `ThousandElementTree`, runs one `Update`, creates one `RecordingRenderer`, and runs `Draw` two times as warm-up. One `[Benchmark]` method `Draw` calls `_tree.Context.Draw(_renderer)`. The summary states that the time holds the walk and the cost of the recording.
- [ ] 4.2 Run `dotnet build brutal-ui.sln -c Release 2>&1 | tail -n 30`. Run the demo command and expect `-` in the `Allocated` column.
- [ ] 4.3 Run the two commands of `docs/technical-design.md` section 14 in Release (`--filter '*'` and `--facts`). Record in `context.md` the table of 6 rows, the machine block, the 4 facts lines, the date, and the commit. The expected facts are `bytes per element 746.5` and 507 levels. Report each other value and its cause.

Group B (4 files):
- [ ] 4.4 Edit `CHANGELOG.md`. Add one line under `### Added`: the HelloPanel sample shows a static brutal-ui screen with a panel, a label, and a procedural image, with the sample allocation gate and `TreeDrawBenchmark` (imgui-renderer/hello-panel-sample). Replace the `### Benchmarks` block with the run of task 4.3: the date, the commit, the table, the machine block, and the facts lines. Add one line for the sample: `HelloPanel smoke run (120 frames): Update and Draw 0 bytes in 117 frames after warm-up.` Keep the two commands and add the smoke command.
- [ ] 4.5 Edit `docs/technical-design.md`. Section 3: the comment of `HelloPanel/` states the static screen of Phase 1, and that one store and one mode come in Phase 3. Section 4, "Frame order": the game sets `ViewportSize` from `io.DisplaySize` before `Update`. Section 14: add `TreeDrawBenchmark` to the benchmarks bullet, and change the "Sample projects" bullet to the HelloPanel smoke run with its 3 lines and its gate. Section 16, Phase 1: add one sentence that hello-panel-sample closes the phase.
- [ ] 4.6 Edit `docs/features/imgui-renderer/human-testing.md`. Add a `## hello-panel-sample` section in the form of the `## label-and-image` section. It holds the 4 phase demo lines and the manual checks M1 to M5 of "Testing Strategy", each as a checkbox.
- [ ] 4.7 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.

**Estimated Steps:** about 40 (7 tasks, about six steps for one task)

**Files (5):** Group A: `TreeDrawBenchmark.cs`, `context.md`. Group B: `CHANGELOG.md`, `docs/technical-design.md`, `docs/features/imgui-renderer/human-testing.md`, `context.md`.

**Acceptance Criteria:**
- [ ] The demo table shows `-` in the `Allocated` column.
- [ ] `CHANGELOG.md` holds 6 benchmark rows, 4 facts lines, and the sample line.
- [ ] The lint exits with 0 for each changed doc.

---

## Key Technical Decisions

### Confirmed Assumptions

No feature interview ran for this draft. The epic interview confirmed these items, and they apply word for word from `docs/features/imgui-renderer/epic-requirements.md` and `epic-overview.md`:
- Q4: the image is a procedural texture that the sample fills in code. No `Brutal.Texture` pin.
- Q5: the brutal-ui tree only. The ImGui text window goes.
- Q6: the texture service registers with the Vulkan backend only.
- A8: the WorldSpace sample stays a stub.
- A9: no new native dependency and no new package pin.

### Decision 1: The sample makes no ImGui widget call
**Choice:** The sample removes `ImGui.Begin`, `ImGui.Text`, and `ImGui.End`. Only `ImGuiRenderer` writes to ImGui, on the background draw list.
**Rationale:** Q5 removes the text window. With no window, the background list holds each vertex of the frame. Thus line 2 of the output proves that brutal-ui drew, with no pixel test.
**Alternatives:** Keep a small ImGui debug window for the frame numbers: against Q5, and it hides the proof of line 2.

### Decision 2: `Update` and `Draw` run after `ImGui.NewFrame`, in one region
**Choice:** Step 3 of "The Frame Order": the viewport size, `Update`, and `Draw`, one after the other, after `ImGui.NewFrame()`.
**Rationale:** The order is the frame order of the technical design (section 4). It meets the imgui-draw-list hand-off (both calls between `NewFrame` and `Render`) and the label-and-image hand-off (`Draw` in the frame, `Update` with a context). The three calls form one region, so one measure covers them.
**Alternatives:** `Update` before `ImGui.NewFrame`, as `LabelUpdateAndDrawAllocateNothingAfterWarmUp` does. It is also valid, but it needs two measured regions and differs from the design doc.

### Decision 3: The upload lives in the sample (`VulkanContext.CreateTexture` and `SampledTexture`)
**Choice:** `VulkanContext` gets one method that uploads RGBA pixels and gives a `SampledTexture`. The sample owns the GPU objects. `BrutalUi.ImGui` gets no upload API.
**Rationale:** The requirement names the upload "through its `VulkanContext`". `VulkanContext` holds the physical device, the queue, and the command pool, which the upload needs. The texture service doc says that the game owns the image, the view, and the sampler. A library upload API has no second user yet (YAGNI). See open scope question 4.
**Alternatives:** (a) A public upload helper in `BrutalUi.ImGui`. (b) The ImGui 1.92 texture data path, where the backend uploads the pixels: no Vulkan code, but against Q6 and the requirement.

### Decision 4: The gate reads the UI region, and the frame region is a report
**Choice:** The sample fails with exit code 1 when `ViewportSize`, `Update`, and `Draw` allocate after warm-up. It prints the bytes of the full frame and does not gate them.
**Rationale:** brutal-ui owns the UI region. The rest of the frame is GLFW and BRUTAL backend code. On Windows, the smoke window is visible, so a mouse event can call a GLFW callback in a measured frame. A gate on the full frame can then fail for code that this project does not own. See open scope question 2.
**Alternatives:** Gate the full frame, or print both numbers with no gate.

### Decision 5: Two new lines on standard output, and line 1 stays
**Choice:** Lines 2 and 3 of "The Output Contract", in a fixed `key value` form, after the unchanged line 1.
**Rationale:** The scripts read the exit code only. The smoke log then holds the proof of the draw and the allocation number for `CHANGELOG.md`. Line 1 stays, so a reader of the old contract still finds it first.
**Alternatives:** Keep one line and print the facts on a flag such as `--report`: a new argument and a change of the parser. See Conflict 1.

### Decision 6: The draw benchmark uses `RecordingRenderer`
**Choice:** `TreeDrawBenchmark` with one method, `Draw`, on `ThousandElementTree` into `RecordingRenderer`.
**Rationale:** The renderer-interface hand-off names this path. `RecordingRenderer` is linked into the benchmark project already, so the project keeps its single reference to `BrutalUi.Core` and loads no native library. See open scope question 3.
**Alternatives:** A draw through `ImGuiRenderer` on a headless ImGui context: it measures the real renderer, but it adds a reference to `BrutalUi.ImGui` and the native ImGui library to the benchmark project.

### Decision 7: The texture is created before the ImGui context and disposed after it
**Choice:** Steps 2 and 6 of "The Create Order" and "The Teardown Order".
**Rationale:** The upload needs no ImGui object. A texture that lives from the `VulkanContext` to the end of `RunWindow` outlives the backend and each descriptor set. The `using` declarations of C# then give the right order with no extra code.
**Alternatives:** Create the texture after the backends in `RunImGui`: the `using` order then disposes the texture before the backend.

### Decision 8: The default font at 26 pixels, and no font load
**Choice:** The label uses font 0 (the ImGui default font) at size 26.
**Rationale:** Q3 of the epic keeps the ImGui default loader, and a TTF file adds an asset to the sample. The dynamic fonts of ImGui 1.92 bake each size on demand, and 26 is two times the default size of 13, so the glyphs stay sharp.
**Alternatives:** Load a TTF file of the game: an asset and a font-load step with no gain for the proof.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The `ViewportSize` setter throws for a negative value, and ImGui starts `io.DisplaySize` at (-1, -1). | Exit code 1 at the first frame. | Step 3 of "The Frame Order" runs after `glfwBackend.NewFrame()` and clamps each component at 0. |
| 2 | A wrong layout or a missing barrier in the upload. | Garbage texels, a validation error, or a lost device on another driver. | The barriers of "The Texture Upload", a wait for the queue before the first frame, the validation run of task 2.7, and the manual check M2 on Windows. |
| 3 | A wrong destroy order at the end. | A crash or a validation error at exit, so the exit code is not 0. | "The Teardown Order", `WaitIdle` before `Remove`, the guard for the none handle, and `checker` after the backend (Decision 7). |
| 4 | GLFW or a BRUTAL backend allocates in the frame. | The frame number is above 0. | The gate reads the UI region only (Decision 4). Task 3.3 records the source as a finding. |
| 5 | The draw command count differs from the plan (1, then 2). | The demo line does not match. | Task 1.8 records the real count. The tier note asks for the cause before a change of the demo line. |
| 6 | A name clash: `Image` of `BrutalUi.Core`, or two locals with the name `renderer`. | A build error, or a call on the wrong object. | Write `BrutalUi.Core.Image` in full when needed. Use the names `uiRenderer`, `glfwBackend`, and `vulkanBackend`. |
| 7 | `BRUTALCORE0011` rejects the string literal of the label. | A build error. | Task 1.2 falls back to `SetText("Hello, brutal-ui"u8)`. |
| 8 | The planted allocation stays in `Label.cs`. | A wrong gate result for each later run. | The copy, the `diff` check, and the `git diff --quiet -- src` check of phase 3. |
| 9 | The benchmark times vary between runs. | The `CHANGELOG.md` numbers do not repeat. | No test asserts a time. The record holds the date, the commit, and the machine block. |
| 10 | The descriptor pool runs out. | `AddVulkan` fails. | The pool holds 8 sets, and the sample uses 2 (the atlas and the checker). |

---

## Dependencies

**Internal (done):**
- renderer-interface: `IUiRenderer`, `UiContext.Draw`, `PaintWalk`, the paint fields of `ComputedStyle`, and `RecordingRenderer`.
- imgui-draw-list: `ImGuiRenderer`, `ImGuiFontService`, `ImGuiTextureService.AddVulkan` and `Remove`, and `TextureHandle.Size`.
- label-and-image: `Label`, `Image`, `UiContext.CreateInfo`, and `LayoutNode.ContentInsets`. This work is on the branch `feat/imgui-renderer/label-and-image` and is not committed yet. Commit it before phase 1 (task 1.1 checks it).

**External (pinned, no change):**
- `Ahwoo.BRUTAL.ImGui` 2026.10.2 and `Ahwoo.BRUTAL.ImGui.Backends` 2026.10.2 (Dear ImGui 1.92.2, `GlfwBackend`, `VulkanBackend`).
- `Ahwoo.BRUTAL.Glfw` 2026.10.4, `Ahwoo.BRUTAL.Vulkan` 2026.10.2, and `Ahwoo.BRUTAL.Vulkan.Abstractions` 2026.10.2.
- `BenchmarkDotNet` 0.15.8.

**System (no change, from the dev-ops epic):**
- Linux: `libvulkan-dev`, the NVIDIA driver and its ICD file, and `xvfb`. Windows: `vulkan-1.dll` from the GPU driver.
- Optional for task 2.7: the Khronos validation layer. It is a check of the developer, not a dependency of the project. The plan installs nothing.

**Downstream:**
- The style-system and input epics show their results on this screen.
- The world-space epic can reuse the upload path of `VulkanContext` for its render target.

---

## Definition of Done

### Functional
- [ ] F1. `grep -nE "ImGui\.(Begin|Text|End)\(" samples/HelloPanel/*.cs` prints nothing.
- [ ] F2. `Program.cs` creates `ImGuiRenderer` before `UiContext`, and passes it as `Measurer` and to `Draw`.
- [ ] F3. The smoke command prints line 1 `HelloPanel: 120 frames rendered.`, line 2 with `draw commands 2` and a vertex count above 0, and line 3 with `Update and Draw 0` and `in 117 frames`. It prints `exit=0`.
- [ ] F4. `--frames abc`, `--frames 0`, `--frames`, and `--bogus` each give a `Usage:` line and `exit=2`, with no window.
- [ ] F5. The planted allocation of task 3.4 gives the gate line on standard error and `exit=1`.
- [ ] F6. `grep -rn "Brutal.Texture" samples Directory.Packages.props` prints nothing. `HelloScreen.FillChecker` makes the pixels.
- [ ] F7. `textures.Remove` runs after `vulkan.WaitIdle()` and before the backend dispose. `checker` disposes after `ImGui.DestroyContext` and before `vulkan`.
- [ ] F8. `TreeDrawBenchmark` shows `-` in the `Allocated` column.

### Quality
- [ ] Q1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30` prints `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` prints 0 failed, with the total of task 1.1.
- [ ] Q3. 5 smoke runs give `Update and Draw 0` (task 3.3).
- [ ] Q4. The validation run of task 2.7 gives no `Validation Error` line, or `context.md` records that the layer does not exist.
- [ ] Q5. Each new type and member of the sample and the benchmark has an XML doc comment.
- [ ] Q6. `ste-lint.py --fail-over 2.5` exits with 0 for each changed doc.

### Integration
- [ ] I1. `scripts/test-linux.sh; echo "exit=$?"` prints the PASS line and `exit=0`. `git diff --quiet <label-and-image commit> -- scripts; echo "exit=$?"` prints `exit=0`.
- [ ] I2. `git diff --quiet <label-and-image commit> -- src Directory.Packages.props samples/WorldSpace; echo "exit=$?"` prints `exit=0`.
- [ ] I3. `CHANGELOG.md` holds the new `### Benchmarks` block with 6 rows, 4 facts lines, and the sample line.
- [ ] I4. (MANUAL) On Windows, `.\scripts\test-windows.ps1` prints the PASS line, and `$LASTEXITCODE` is 0.

### Verification Method
1. Run Q1 and Q2.
2. Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames abc; echo "exit=$?"` and repeat with `--frames 0`, `--frames`, and `--bogus` (F4).
3. Run the smoke command and compare the three lines with F3.
4. Run `scripts/test-linux.sh; echo "exit=$?"` and `cat artifacts/test-linux/smoke.log` (I1).
5. Run the greps of F1 and F6, and the `git diff` checks of I1 and I2.
6. Read `Program.cs` for F2 and F7 against "The Create Order" and "The Teardown Order".
7. Read `context.md` for the exit codes of task 3.4 (F5), the 5 runs of task 3.3 (Q3), and the validation result of task 2.7 (Q4).
8. Run the phase 4 demo command (F8), and read `CHANGELOG.md` (I3).
9. On Windows, run the manual checks M1 to M5 (I4).

---

## Testing Strategy

- **Command checks of the GPU path (Linux):** the smoke command and `scripts/test-linux.sh` after each phase. Line 2 proves the draw (the count of commands and vertices of the background list). Line 3 proves the allocation rule. The exit code proves the contract.
- **Usage checks:** the 4 usage cases need no GPU and no display.
- **The gate proof:** the planted allocation in `Label.PaintContent` (task 3.4).
- **The validation run:** the smoke command with the Khronos validation layer, when the layer exists (task 2.7).
- **The unit tests:** no new test. The existing gates (`ThousandElementAllocationTests`, `ElementAllocationTests`, and `ImGuiRendererAllocationTests`) still cover `Update` and `Draw` with no GPU. The total of the solution stays the total of task 1.1.
- **The benchmark:** `TreeDrawBenchmark` and a full run for `CHANGELOG.md` (phase 4).
- **Manual on Windows (a person with a display):**
  - M1. After `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0. `artifacts\test-windows\smoke.log` holds the 3 lines with `Update and Draw 0`.
  - M2. Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj` with no argument. Expect the window `HelloPanel` with a dark clear color and no ImGui window. Near the top-left corner, expect a panel with rounded corners and a border. In the panel, expect the text `Hello, brutal-ui` in white, and the checker image under it.
  - M3. At a display scale of 100 %, the checker is a square of 64 by 64 pixels with 8 by 8 cells and sharp edges. The text and the image lie inside the panel, and the border lies inside the panel edge.
  - M4. Minimize the window for 5 seconds, then restore it. Expect the same screen. Close the window. Expect the 3 lines in the console and `$LASTEXITCODE` 0.
  - M5. Run the sample with `--frames abc`. Expect the `Usage:` line and `$LASTEXITCODE` 2.

---

## Related Documentation

- `docs/features/imgui-renderer/hello-panel-sample/requirements.md`
- `docs/features/imgui-renderer/epic-overview.md` and `epic-requirements.md`
- `docs/features/imgui-renderer/renderer-interface/context.md` (hand-offs: the draw benchmark)
- `docs/features/imgui-renderer/imgui-draw-list/context.md` (hand-offs: the create order, `AddVulkan`, the descriptor pool, `Remove`)
- `docs/features/imgui-renderer/label-and-image/context.md` (hand-offs: the measurer, the frame rule, `BRUTALCORE0011`, the facts lines)
- `docs/features/dev-ops/solution-skeleton/implementation.md` (the `--frames N` contract)
- `docs/technical-design.md` sections 3, 4, 8, 14, and 16
- `docs/feasibility.md` (the `Brutal.Vulkan.Abstractions` helpers)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

### Open Scope Questions

**Answered 2026-10-21 by the user in `/autorun-epic`:** each of the four questions takes its recommended option. Question 1: the `scripts/test-linux.sh` smoke log demo. Question 2: only the UI region fails the run on an allocation. Question 3: `TreeDrawBenchmark` with `RecordingRenderer`. Question 4: the texture upload stays in the sample. The plan follows each answer.

**Question 1: What is the demo of phase 1? (rule 12)**
- **Recommended:** `**Demo:** the developer runs scripts/test-linux.sh and reads the PASS line. Then the developer runs cat artifacts/test-linux/smoke.log and reads HelloPanel: 120 frames rendered. and HelloPanel: last frame: draw commands 1, vertices <V>. with a value of V above 0.` Reason: a command checks the GPU path, and no person must look at a window.
- Option B: `**Demo:** a person runs the sample on Windows and sees the panel with the label, and no ImGui window.` Reason: it checks the pixels, but it needs a person and a display.
- Option C: `**Demo:** a headless ImGui test builds HelloScreen and prints the background list.` Reason: no GPU, but it does not run the sample, and the test project must compile a file of the sample.

**Question 2: What does the allocation gate of the sample read?**
- **Recommended:** the UI region (`ViewportSize`, `Update`, and `Draw`) fails the run with exit code 1. The full frame is a report. Reason: brutal-ui owns the UI region, and an input event on Windows can call GLFW code in the full frame.
- Option B: the full frame fails the run. Reason: it matches the words "the frame loop allocates no managed memory", but code of GLFW and BRUTAL can fail it.
- Option C: both numbers are a report with no gate. Reason: the exit code keeps its old meaning, but no script catches a new allocation in the draw on the GPU path.

**Question 3: Which draw benchmark comes in?**
- **Recommended:** `TreeDrawBenchmark` on `ThousandElementTree` into `RecordingRenderer`. Reason: one class, no new reference, and the path of the renderer-interface hand-off.
- Option B: a draw benchmark through `ImGuiRenderer` on a headless ImGui context. Reason: it measures the real renderer, but the benchmark project then references `BrutalUi.ImGui` and loads the native ImGui library.
- Option C: no new benchmark. Phase 4 runs the existing benchmarks only. Reason: less work, but `CHANGELOG.md` gets no draw time.

**Question 4: Where does the texture upload live?**
- **Recommended:** `VulkanContext.CreateTexture` and `SampledTexture` in the sample. Reason: the requirement names the `VulkanContext` of the sample, and no library user needs an upload API yet.
- Option B: a public upload helper in `BrutalUi.ImGui` (for example a method that uploads pixels and calls `AddVulkan`). Reason: a game can reuse it, but it is a new public API with Vulkan types, and the service then owns GPU objects.

### Conflicts

1. **The output contract of the dev-ops epic.** `docs/features/dev-ops/solution-skeleton/implementation.md` states one line on standard output on success. This plan adds lines 2 and 3 (Decision 5). Line 1 stays the same, and both scripts read the exit code only. The plan does not edit the done dev-ops docs. Task 4.5 records the new lines in `docs/technical-design.md`.
2. **The epic names "the draw allocation test" for this feature.** The draw gates exist already (`ThousandElementAllocationTests`, `ImGuiRendererAllocationTests`, and `ElementAllocationTests`). This plan adds the gate inside the GPU sample (phase 3) and no new unit test.
3. **The requirement "the frame loop allocates no managed memory".** The recommended gate reads the UI region only, and the full frame is a report (Decision 4, question 2). When the full frame number is 0 in each run of task 3.3, the requirement holds as written.
4. **The two hand-offs on the frame order.** imgui-draw-list asks for `Update` and `Draw` between `ImGui.NewFrame` and `ImGui.Render`. label-and-image allows `Update` before `ImGui.NewFrame`. The plan runs both after `ImGui.NewFrame`, which meets both (Decision 2).
5. **`docs/technical-design.md` section 3** names HelloPanel "one panel, one store, one mode". Stores and modes come in Phase 3. Task 4.5 states the Phase 1 state and keeps the later goal.

The Confirmed Assumptions agree with the codebase. `ImGuiTextureService.AddVulkan` is the only registration path that the sample uses (Q6), and no `Brutal.Texture` pin exists (Q4).

### Gaps (not in this feature)

1. The window has a fixed size, and the sample does not create the swapchain again. A resize needs that work first.
2. No pixel test. The visual check on the GPU is manual (M2 and M3).
3. The static screen does not call `SetText`, `RefreshLocalizedText`, or a setter in the loop. The unit gates cover these paths.
4. The sample uses the ImGui default font only. A TTF font of the game needs the font load step before the first `GetFont(0)` call.
5. The full frame is not gated (question 2, option A). A later change can gate it when the BRUTAL code allocates nothing.
6. At a display scale above 100 %, the 64 by 64 image fills more than 64 physical pixels, and the nearest filter can give cells of uneven width.

### Hand-offs (for `context.md` at the end of phase 4)

**The style-system epic**
- `HelloScreen.Build` sets the paint fields and the label values inline. The cascade can replace them. The screen is the first place to show a stylesheet.

**The input epic**
- The sample owns the `GlfwWindow`. Input can route from its events. The screen has no interactive element yet.
- Keep the UI region of "The Frame Order" free of allocation. An input pass inside the region falls under the gate of the sample.

**The world-space epic**
- `VulkanContext.CreateTexture` shows the upload, the barriers, and the layout for a sampled texture. A render target needs a color attachment usage and its own render pass.
- Share `ImGuiFontService` and `ImGuiTextureService` between the screen renderer and a world-space renderer.
