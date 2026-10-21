# imgui-renderer/hello-panel-sample - Task Checklist

**Last Updated:** 2026-10-21 04:20
**Progress:** 27/27 tasks complete (100%)
**Advanced phases:** Phases 1 to 3 (ADVANCED, opus). Phase 4 is STANDARD (sonnet).

"The plan" means `implementation.md` of this feature. Its sections "The Screen", "The Create Order", "The Frame Order", "The Teardown Order", "The Texture Upload", "The Output Contract", and "The Allocation Gate" hold the rules that the tasks name. "The smoke command" is the command under "Implementation Phases" of the plan. It runs from the repository root after a build.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` (expect `0 Warning(s)` and `0 Error(s)`) and the smoke command. No file of `src/` changes, except the temporary plant of task 3.4. `Directory.Packages.props`, `scripts/`, and `samples/WorldSpace/` do not change.

---

## Phase 1: A Panel and a Label on the GPU (8/8 complete)

**Demo:** the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: 120 frames rendered.` and `HelloPanel: last frame: draw commands 1, vertices <V>.` with a value of V above 0.
**Needs:** none

- [x] **1.1 Check the start state**
  - Description: Confirm that `src/BrutalUi.Core/Elements/Label.cs` and `UiContext.CreateInfo` exist. Run `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Record the test total in `context.md` (1013 is the expected value). Run the smoke command and record the output and the exit code.
  - File(s): `context.md`
  - Estimate: 5 steps

- [x] **1.2 Write `HelloScreen.cs`**
  - Description: Write `samples/HelloPanel/HelloScreen.cs`: `internal static class HelloScreen` with `public static Element Build()`. Build the root, the panel, and the label of "The Screen". Put each size and each color into a named constant. Set the label text in the create-info (`Text = "Hello, brutal-ui"`). When the analyzer `BRUTALCORE0011` rejects the literal, call `label.SetText("Hello, brutal-ui"u8)` after the constructor. Add XML doc comments.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 6 steps

- [x] **1.3 Add the renderer and the context to `RunImGui`**
  - Description: Edit `RunImGui` in `Program.cs`. Add steps 5 and 6 of "The Create Order" before the backends. Rename the backend locals to `glfwBackend` and `vulkanBackend`. Call `ui.AddRoot(HelloScreen.Build())` after the backends. Pass `ui` and `uiRenderer` to `RunFrames`.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 5 steps

- [x] **1.4 Replace the ImGui window in `RunFrames`**
  - Description: Edit `RunFrames`. Remove the calls `ImGui.Begin`, `ImGui.Text`, and `ImGui.End`. Add steps 3 and 4 of "The Frame Order". Keep the minimize wait and the close check.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 5 steps

- [x] **1.5 Add `RunResult` and output line 2**
  - Description: Replace the `int` result of `RunWindow`, `RunImGui`, and `RunFrames` with a private `readonly record struct RunResult` (the frame count, the draw command count, and the vertex count of the last frame). `Run` writes line 1 with no change, then line 2 of "The Output Contract". The exit codes do not change.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 5 steps

- [x] **1.6 Update the doc comments of `Program`**
  - Description: Update the summary of `Program` and the remarks of `RunFrames`. They state that the sample draws a brutal-ui tree, the frame order, and the output lines. Remove the text "draws one ImGui text label".
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 3 steps

- [x] **1.7 Run the checks**
  - Description: Run the checks in one command: the build (0 warnings), then `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- <args>` with `--frames abc`, `--frames 0`, `--frames`, and `--bogus` (each `exit=2` and a `Usage:` line), then the smoke command (`exit=0` and the two lines), then `scripts/test-linux.sh; echo "exit=$?"` (the PASS line and `exit=0`).
  - File(s): none
  - Estimate: 4 steps

- [x] **1.8 Record the phase 1 results**
  - Description: Record in `context.md`: the two output lines, the draw command count, the vertex count, and each deviation from "The Create Order" or "The Frame Order".
  - File(s): `context.md`
  - Estimate: 2 steps

---

## Phase 2: The Procedural Image (7/7 complete)

**Demo:** the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: 120 frames rendered.` and `HelloPanel: last frame: draw commands 2, vertices <V>.` The second command binds the checker texture.
**Needs:** Phase 1

- [x] **2.1 Read the Vulkan API before the code**
  - Description: Read the XML docs of `Brutal.Vulkan` 2026.10.2 and `Brutal.Vulkan.Abstractions` 2026.10.2 in the NuGet cache (`~/.nuget/packages/ahwoo.brutal.vulkan*/2026.10.2/lib/net10.0/*.xml`). Record in `context.md` the signatures for each step of "The Texture Upload": the create and destroy calls of an image, a buffer, a view, and a sampler, the memory calls (`DeviceExtensions.AllocateMemory` and `WriteMemory`, or `ImageEx` and `BufferEx` on `DeviceEx`), `CommandBuffer.PipelineBarrier`, `CommandBuffer.CopyBufferToImage`, the command buffer allocate and free calls, and the queue wait. Use a helper of `Brutal.Vulkan.Abstractions` when it covers a step. Else use the raw call. Record the choice.
  - File(s): `context.md`
  - Estimate: 8 steps

- [x] **2.2 Write `SampledTexture.cs`**
  - Description: Write `samples/HelloPanel/SampledTexture.cs`: `internal sealed class SampledTexture : IDisposable`. It owns the image, its memory, the image view, and the sampler. It has the properties `View`, `Sampler`, and `Size` (`int2`). `Dispose` destroys the sampler, the view, the image, and the memory, in that order. A second call does nothing. A partly built object is safe to dispose, as in `VulkanContext`.
  - File(s): `samples/HelloPanel/SampledTexture.cs`
  - Estimate: 5 steps

- [x] **2.3 Add `VulkanContext.CreateTexture`**
  - Description: Add `public SampledTexture CreateTexture(ReadOnlySpan<byte> rgba, int2 size)` to `VulkanContext`. The method throws `ArgumentOutOfRangeException` when a size component is 0 or less, and `ArgumentException` when `rgba.Length` is not `size.X * size.Y * 4`. Then it runs steps 1 to 7 of "The Texture Upload". On a failure, it frees each object that it created and throws. Add one sentence to the class remarks: the context also uploads a sampled texture before the first frame.
  - File(s): `samples/HelloPanel/VulkanContext.cs`
  - Estimate: 10 steps

- [x] **2.4 Add the checker and the image to `HelloScreen`**
  - Description: Edit `HelloScreen.cs`. Add `public static void FillChecker(Span<byte> rgba, int2 size)` (cells of 8 texels, two opaque colors). Change `Build()` to `Build(TextureHandle checker)`, and add the `Image` of "The Screen" after the label. Write `BrutalUi.Core.Image` in full when the name is ambiguous.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 4 steps

- [x] **2.5 Create the checker texture in `RunWindow`**
  - Description: Edit `RunWindow`. After the `VulkanContext`, fill a `byte[]` of `64 * 64 * 4` bytes with `FillChecker`. Declare `using SampledTexture checker = vulkan.CreateTexture(pixels, size);` after the `vulkan` declaration. Pass `checker` to `RunImGui`.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 3 steps

- [x] **2.6 Register and remove the texture in `RunImGui`**
  - Description: Edit `RunImGui`. Add step 8 of "The Create Order" after the backends, and pass the handle to `HelloScreen.Build`. Declare the handle as `TextureHandle handle = default` before the `try` block. In the `finally` block, after `vulkan.WaitIdle()`, call `textures.Remove(handle)` when `handle.IsNone` is false.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 4 steps

- [x] **2.7 Run the checks and the validation run**
  - Description: Run the checks: the build, the smoke command (`exit=0` and `draw commands 2`), and `scripts/test-linux.sh` (the PASS line). When `/usr/share/vulkan/explicit_layer.d/VkLayer_khronos_validation.json` exists, run the smoke command again with `VK_INSTANCE_LAYERS=VK_LAYER_KHRONOS_validation` and expect no line with `Validation Error`. Else record "no validation layer on this machine" in `context.md`. Record the two counts in `context.md`.
  - File(s): `context.md`
  - Estimate: 5 steps

---

## Phase 3: The Allocation Gate of the Sample (5/5 complete)

**Demo:** the developer runs `scripts/test-linux.sh` and reads the PASS line. Then the developer runs `cat artifacts/test-linux/smoke.log` and reads `HelloPanel: bytes after warm-up: Update and Draw 0, frame <F>, in 117 frames.`
**Needs:** Phase 2
**Verified:** PASS after fix 1 and fix 2 (2026-10-21). Demo: PASS.

- [x] **3.1 Add the two measured regions to `RunFrames`**
  - Description: Add the two regions of "The Allocation Gate" to `RunFrames`. Add the constant `WarmUpFrames = 3`. Add the UI sum, the frame sum, and the count of measured frames to `RunResult`. The regions use only locals and add no allocation.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 5 steps

- [x] **3.2 Write line 3 and the gate in `Run`**
  - Description: Edit `Run`. Write line 3 of "The Output Contract" after line 2. When the UI sum is above 0, write the gate line to standard error and return `ExitFailure`. Update the remarks of `RunFrames` with the two regions and the gate. Replace the sentence "The loop allocates no managed memory" with the measured rule.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 4 steps

- [x] **3.3 Run the smoke command 5 times**
  - Description: Run the smoke command 5 times. Expect `Update and Draw 0`, `in 117 frames`, and `exit=0` each time. Record the frame number of each run in `context.md`. When the frame number is above 0, find the source (for example a backend call) and record it as a finding. It does not fail the run (Decision 4).
  - File(s): `context.md`
  - Estimate: 5 steps

- [x] **3.4 Prove that the gate blocks**
  - Description: Copy `src/BrutalUi.Core/Elements/Label.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Label.PaintContent`. Build. Run the smoke command. Expect a UI number above 0, the gate line on standard error, and `exit=1`. Restore the file from the copy. Run `diff` on the two files and expect no output. Build and run the smoke command again, and expect `exit=0`. Record the two exit codes and the planted number in `context.md`. Do not use `scripts/test-linux.sh` for the planted run, because the unit tests stop the script first.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs` (temporary, restored), `context.md`
  - Estimate: 8 steps

- [x] **3.5 Run the script and the tests**
  - Description: Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` and expect the total of task 1.1 and 0 failed.
  - File(s): none
  - Estimate: 3 steps

---

## Phase 4: The Draw Benchmark and the Record (7/7 complete)

**Demo:** the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*TreeDrawBenchmark*'` and reads a table with the row `Draw` and `-` in the `Allocated` column.
**Needs:** Phase 3
**Verified:** PASS (2026-10-21). Demo: PASS.

- [x] **4.1 Write `TreeDrawBenchmark.cs`**
  - Description: Write `tests/BrutalUi.Benchmarks/TreeDrawBenchmark.cs` in the form of `TreeLayoutBenchmark`. It has `[MemoryDiagnoser]` and a setup that builds `ThousandElementTree`, runs one `Update`, creates one `RecordingRenderer`, and runs `Draw` two times as warm-up. One `[Benchmark]` method `Draw` calls `_tree.Context.Draw(_renderer)`. The summary states that the time holds the walk and the cost of the recording.
  - File(s): `tests/BrutalUi.Benchmarks/TreeDrawBenchmark.cs`
  - Estimate: 5 steps

- [x] **4.2 Run the Release build and the demo**
  - Description: Run `dotnet build brutal-ui.sln -c Release 2>&1 | tail -n 30`. Run the demo command and expect `-` in the `Allocated` column.
  - File(s): none
  - Estimate: 3 steps

- [x] **4.3 Run the full benchmark and the facts**
  - Description: Run the two commands of `docs/technical-design.md` section 14 in Release (`--filter '*'` and `--facts`). Record in `context.md` the table of 6 rows, the machine block, the 4 facts lines, the date, and the commit. The expected facts are `bytes per element 746.5` and 507 levels. Report each other value and its cause.
  - File(s): `context.md`
  - Estimate: 4 steps

- [x] **4.4 Edit `CHANGELOG.md`**
  - Description: Add one line under `### Added`: the HelloPanel sample shows a static brutal-ui screen with a panel, a label, and a procedural image, with the sample allocation gate and `TreeDrawBenchmark` (imgui-renderer/hello-panel-sample). Replace the `### Benchmarks` block with the run of task 4.3: the date, the commit, the table, the machine block, and the facts lines. Add one line for the sample: `HelloPanel smoke run (120 frames): Update and Draw 0 bytes in 117 frames after warm-up.` Keep the two commands and add the smoke command.
  - File(s): `CHANGELOG.md`
  - Estimate: 5 steps

- [x] **4.5 Edit `docs/technical-design.md`**
  - Description: Section 3: the comment of `HelloPanel/` states the static screen of Phase 1, and that one store and one mode come in Phase 3. Section 4, "Frame order": the game sets `ViewportSize` from `io.DisplaySize` before `Update`. Section 14: add `TreeDrawBenchmark` to the benchmarks bullet, and change the "Sample projects" bullet to the HelloPanel smoke run with its 3 lines and its gate. Section 16, Phase 1: add one sentence that hello-panel-sample closes the phase.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] **4.6 Edit `human-testing.md`**
  - Description: Edit `docs/features/imgui-renderer/human-testing.md`. Add a `## hello-panel-sample` section in the form of the `## label-and-image` section. It holds the 4 phase demo lines and the manual checks M1 to M5 of "Testing Strategy", each as a checkbox.
  - File(s): `docs/features/imgui-renderer/human-testing.md`
  - Estimate: 3 steps

- [x] **4.7 Lint the docs and write the hand-offs**
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.
  - File(s): `context.md`, each changed doc
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- none

**Fixed Bugs:**
- [x] Phase 3 Fix 1: `ElementAllocationTests` failed in about 3 runs of 100 under load. The cause is the background GC, which adds the free part of the allocation context to the byte counter. The test now empties the context before each measure. See `context.md`, "Phase 3 Fix 1".
- [x] Phase 3 Fix 2: `LayoutAllocationTests` and `TreeAllocationTests` failed 1 and 3 times in 480 runs under load, and `ThousandElementAllocationTests` had the same exposure. The cause is the background GC of fix 1. Each 0-byte gate of `BrutalUi.Core.Tests` now calls the shared helper `AllocationGate.ReleaseAllocationContext()` before its measure. 480 runs under load gave 0 failed runs, and two plants made each gate fail. See `context.md`, "Phase 3 Fix 2: The Other Allocation Gates".
- [x] ✅ Cross-phase review CR-1 to CR-5 - Completed 2026-10-21. See `code-review.md` and `context.md`, "Review Group 1: Results" to "Review Group 3: Results".

---

## Technical Debt

- none

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- Small (up to 6 steps)
- Medium (7 to 15 steps)
- Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with [x] immediately when done. Add new tasks as they are discovered.*
