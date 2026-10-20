# imgui-renderer/renderer-interface - Context & Decisions

**Last Updated:** 2026-10-20 04:25
**Current Phase:** Complete (all four phases and the cross-phase review done)
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Implementation plan (`implementation.md`), accepted.
- ✅ Dev docs scaffolded (`context.md`, `tasks.md`, `inbox.md`)
- ✅ Phase 1 (tasks 1.1 to 1.11): the contract, the walk, `UiContext.Draw`, the recording renderer, and 18 new tests. Verifier `phase-1-verify.md`: PASS (592 tests, 0 failed, 0 warnings). Demo: PASS.
- ✅ Phase 2 (tasks 2.1 to 2.8): the paint members, the hidden and opacity rules, the border rule, the radius, the scroll clip, and 20 new tests. Verifier `phase-2-verify.md`: PASS (612 tests, 0 failed, 0 warnings). Demo: PASS.
- ✅ Phase 3 (tasks 3.1 to 3.6): paint members on the 1000-element tree, `DrawAllocatesNothingAfterWarmUp` (0 bytes in 5 runs), and `TheDrawStepEmitsEachPaintedElement`. A planted allocation fails the gate (exit 2). Verifier `phase-3-verify.md`: PASS (614 tests, 0 failed, 0 warnings, `scripts/test-linux.sh` exit 0). Demo: PASS.
- ✅ Phase 4 (tasks 4.1 to 4.7): the internal `PaintContent` hook, `PaintContentTests` (5 tests), sections 4, 5, 7, 8, and 14 of the technical design, the `CHANGELOG.md` Added line, and the human-testing lines. Verifier `phase-4-verify.md`: PASS (619 tests, 0 failed, 0 warnings, `scripts/test-linux.sh` exit 0, lint below 2.5). Demo: PASS.
- ✅ Cross-phase review (`code-review.md`): 11 findings (0 HIGH, 2 MEDIUM, 9 LOW). Review groups 1 to 4 fixed CR-1 (the doc contract), CR-3 to CR-9. The orchestrator fixed CR-11. CR-2 goes to a user decision. CR-10 goes to the label-and-image hand-off. Verifier `review-verify.md`: PASS (623 tests, 0 failed, 0 warnings, `scripts/test-linux.sh` exit 0).
- ✅ The draft plan of imgui-draw-list: `docs/features/imgui-renderer/imgui-draw-list/implementation.md` (5 phases, 40 tasks).

**What's Next:**
- 📋 A person runs the items of `## Needs Human Verification` (the Windows run first).
- 📋 The caller (`/autorun-epic`) commits the work and runs `/update-epic imgui-renderer` and `/update-master`.
- 📋 The user answers the two Open Questions (CR-1 route, CR-2 texture size) before imgui-draw-list builds its texture service and before label-and-image starts.

**Blockers:**
- none

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Rendering/` - `IUiRenderer`, `Rgba8`, `Corners`, `RenderHandles`, and `PaintWalk` (new folder, namespace `BrutalUi.Core`)
- `src/BrutalUi.Core/Tree/ComputedStyle.cs` - The paint members
- `src/BrutalUi.Core/Tree/UiContext.cs` - `Draw` and the frame stack
- `src/BrutalUi.Core/Tree/Element.cs` - The `PaintContent` hook (phase 4)
- `tests/BrutalUi.Core.Tests/Shared/RecordingRenderer.cs` - The recording renderer. The test project compiles it, and the benchmark project links it.

### Related Files
- `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` - The allocation gate (phase 3 adds the draw)
- `docs/features/imgui-renderer/renderer-interface/implementation.md` - Implementation plan

---

## Important Decisions

The plan holds decisions 1 to 10 and the assumptions Q1 to Q4 and A1 to A9. This section records the decisions made during the build.

- The run is the called mode of `/autorun-feature`, from `/autorun-epic`. The orchestrator makes no git write. The caller commits.
- Phase 1, group 1 (tasks 1.1 to 1.4): the four contract files are in `src/BrutalUi.Core/Rendering/`, namespace `BrutalUi.Core`. The build gives 0 warnings and 0 errors. `BrutalUi.Core.csproj` still holds 2 `PackageReference` items.
- `Rgba8` equality and the hash code use one packed `uint`: R in the low byte, A in the high byte. This value is the same as `IM_COL32(r, g, b, a)`. The pack helper is private. Phase 2 adds `internal Rgba8 ScaleAlpha(float factor)` next to it.
- `Rgba8` is not premultiplied. The summary states it, so the ImGui renderer passes the bytes with no conversion.
- `FontHandle` and `TextureHandle` have a public constructor, as the plan states. `ClassId` has an internal constructor. The handles differ on purpose, because a renderer in another assembly creates them.
- Blast radius: none. No existing file in `src/`, `tests/`, or `samples/` names `IUiRenderer`, `Rgba8`, `Corners`, `FontHandle`, or `TextureHandle`. The four files add new types only.
- Phase 1, group 2 (tasks 1.5 to 1.11). Baseline before the first edit: `dotnet test --solution brutal-ui.sln` gave total 574, failed 0. After the group: total 592 (9 in `RenderValueTests`, 9 in `PaintWalkTests`), failed 0. The build gives 0 warnings and 0 errors. The benchmark project builds with the linked `RecordingRenderer.cs`.
- Blast radius of group 2: `ComputedStyle` gets one field at the end (`BackgroundColor`, 4 bytes). The layout reads no paint field, and the 574 old tests pass with no change. `UiContext` gets two private fields and one public method. `Update` does not change. `Element` does not change. The walk reads `Element.ChildStorage.AsSpan()`, `Element.Style`, and `Element.Layout.Rect` only.
- `PaintWalk.Walk` holds a `try`/`finally`. After an exception from the renderer, the `finally` clears the used frames (`Array.Clear(stack, 0, depth)`), so the stack keeps no element reference. The normal path ends at depth 0 and clears nothing. The brief does not name this case.
- The stack growth uses `Math.Max(stack.Length * 2, InitialStackSize)`, so an empty array also grows. `UiContext` never passes an empty array.
- The loop order on exit is the order of the brief: `PopClip` when `PushedClip` is set, then `default` into the slot, then the depth goes down.
- `RecordingRenderer.Begin` also sets `ClipDepth` to 0, so a frame after an exception starts clean. `Begin` stores the display size in `DrawCall.Rect` as (0, 0, width, height). The text buffer starts at 1024 bytes.
- `RecordingRenderer.Describe` joins the lines with `\n` and adds no line feed at the end. The forms of `Image` and `Text` are `Image x,y,w,h texture <id> uv u0,v0,u1,v1 tint #RRGGBBAA radius tl,tr,br,bl` and `Text x,y font <id> size <s> #RRGGBBAA "<text>"`, with no escape of the text.
- `ThrowsForADrawDuringADraw` also checks that the context draws again after the throw, so the `finally` resets `_drawing`.
- `Rgba8MatchesTheImCol32Layout` uses `Assert.SkipUnless(BitConverter.IsLittleEndian, ...)`. It skips on a big-endian CPU. On x64, it runs.
- Review triage (2026-10-20). The review agent ran in a scratch copy of the repository, so no probe stayed in the tree. The orchestrator did not apply CR-2, because each option changes a public value type or the interface of the accepted plan, and the called mode keeps the plan. CR-10 needs a change of the layout pass and of the internal hook, and only `Label` needs it, so it goes to the label-and-image hand-off.
- Review group 1 (CR-1, CR-9) changed `///` lines of `IUiRenderer.cs` only. No signature changed. `MeasureText` can run outside a frame. `Begin` starts a clean frame. The rects are in viewport pixels, and `scale` converts them to framebuffer pixels. A radius of 0 or less, or `NaN`, gives a square corner.
- Review group 2 (CR-3 to CR-5): the 5000-level test draws on a thread with a 64 KB stack, so a recursive walk stops the test host. New tests: `ClampsAnOpacityAboveOne`, `RecoversAfterTheRendererThrowsInTheWalk`, `RecoversAfterAHookThrows`, and `ReleasesTheElementsAfterADraw`.
- Review group 4 (CR-8) also fixed the doc of the `Overflow` enum in `src/BrutalUi.Core/Layout/LayoutEnums.cs`, which named a future epic.

---

## Gotchas & Learnings

- Section 8 of `docs/technical-design.md` and the interface block of the phase 1 brief are the same. The eight signatures match word for word. No change was necessary.
- The demo command form `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` is valid and prints the list, one `OUTPUT: [BrutalUi.Core.Tests.PaintWalkTests.DrawsThreeElementTreeWithOneClip]` prefix for each line. The form `dotnet test --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` runs the test and passes, but it prints no output line. Use the `dotnet run` form for each demo.
- The printed list of the phase 1 demo (the prefix removed):

```
Begin 400x300 scale 1
Rect 0,0,200,100 fill #0A141EFF border 0 #00000000 radius 0,0,0,0
PushClip 2,2,196,96
Rect 12,12,50,20 fill #FF0000FF border 0 #00000000 radius 0,0,0,0
Rect 12,32,300,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0
PopClip
End
```

- Phase 2 (tasks 2.1 to 2.8). `ComputedStyle` gets `BorderColor` and `CornerRadius` (fields) and `Opacity` (property with a private `_opacity` and `_hasOpacity`, `readonly get`). The class remarks name `Opacity` as the one property. `Rgba8.ScaleAlpha` is internal and sits before `Pack`. `PaintWalk.Enter` gets steps 1 and 2, the stroke and border rule, the radius, and `Overflow != Visible` for the clip. The walk runs a clamp by branches (`NaN` or above 1 gives 1, below 0 gives 0) and then `opacity *= own`.
- Blast radius of phase 2: `ComputedStyle` grows by about 28 bytes (4 for `BorderColor`, 16 for `CornerRadius`, and 8 for the opacity float and flag, with padding). No layout code reads a paint member. The 592 older tests pass with no change.
- Test count: 592 before the phase, 612 after (4 in `RenderValueTests`, 16 in `PaintWalkTests`), failed 0. `KeepsTheOrderWhenTheStackGrows` passes, so the growth path works (a chain of 100 levels grows the stack from 32 to 128).
- The demo list of phase 2 (printed by `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsHiddenAndFadedTree' --show-live-output on`, prefix removed):

```
Begin 400x300 scale 1
Rect 0,0,200,200 fill #0000FFFF border 0 #00000000 radius 0,0,0,0
Rect 0,0,100,100 fill #00FF0080 border 0 #00000000 radius 0,0,0,0
Rect 0,0,40,40 fill #FF000040 border 0 #00000000 radius 0,0,0,0
End
```

---

## API Integration

- `IUiRenderer` is the contract that imgui-draw-list implements. `UiContext.Draw(IUiRenderer)` is the call that hello-panel-sample makes each frame.

---

## State Management

- `UiContext` owns the paint frame stack (`PaintFrame[]`). It grows by doubling and never shrinks. `Draw` reads the tree only and changes no dirty bit (A2).

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Rendering/*.cs`
- `RenderValueTests`, `PaintWalkTests`, and `PaintContentTests` against `RecordingRenderer`.

### Integration Tests
**Files:** `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
- `DrawAllocatesNothingAfterWarmUp` on the 1000-element tree.

### Manual Testing Checklist
- [ ] (MANUAL) `scripts\test-windows.ps1` on Windows.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsThreeElementTreeWithOneClip' --show-live-output on` and reads 7 call lines: `Begin`, the root Rect, PushClip 2,2,196,96, two child Rects, PopClip, and End.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsHiddenAndFadedTree' --show-live-output on` and reads a call list with no Rect for the hidden child, alpha `80` (128) in the fill of the half-opacity panel, and alpha `40` (64) in the fill of its half-opacity child.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsContentBetweenTheBackgroundAndTheChildren' --show-live-output on` and reads, for the probe element, the Rect line, the PushClip line, a `Text` line, the Rect line of its child, and the PopClip line, in that order.
- [ ] Windows run (Q7): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- `Draw` allocates nothing after warm-up. The frame stack and the recording renderer reach their peak size in the warm-up.

---

## Next Steps

### Immediate (Today/This Session)
1. Run the items of `## Needs Human Verification`, with the Windows run of `scripts\test-windows.ps1` first.
2. Answer the two Open Questions (CR-1 route, CR-2 texture size).
3. Commit the feature (the caller does it), then build imgui-draw-list from its draft plan.

### Short Term (This Week)
1. imgui-draw-list, then label-and-image, then hello-panel-sample (the epic build order).

---

## Open Questions

- [ ] **Q (CR-1, the measure route):** How does the measure callback of `Label` reach `IUiRenderer.MeasureText` during `UiContext.Update`? `MeasureCallback` gets the `Element` only, and `Update(float)` takes no renderer. Options: (a) a renderer slot on `UiContext` that the label reads through its root, (b) a measurer reference on the label at creation, (c) a text measure service in `BrutalUi.Core` that the renderer implements. This feature exposes the interface only. label-and-image owns the answer.
      **A:** open (a user decision before label-and-image starts)

- [ ] **Q (CR-2, the texture size):** How does `Image` get the natural size of its texture? `TextureHandle` holds one `int` id, and `IUiRenderer` has no size query. Options: (a) `TextureHandle` gets an `int2 Size`, and the texture service of imgui-draw-list returns a handle with its size, (b) `Image` takes an explicit natural size from the caller, and the handle does not change, (c) `IUiRenderer` gets `int2 GetTextureSize(TextureHandle texture)`, with the same route problem as CR-1. Option (a) changes a public value type of this plan, so the run did not apply it.
      **A:** open (a user decision before imgui-draw-list builds its texture service)

---

## Session Notes

### 2026-10-20 04:25 (the review and the close)
- The cross-phase review gave 11 findings (0 HIGH, 2 MEDIUM, 9 LOW). No probe found a bug in the walk. The gate is real: a planted allocation for the settings rows fails it with 85,848 bytes.
- Review groups 1 to 4 fixed 8 findings. The orchestrator fixed CR-11. CR-2 and CR-10 carry a reason in `code-review.md`. Verifier `review-verify.md`: PASS, 623 tests, 0 failed, 0 warnings, `scripts/test-linux.sh` exit 0.
- The solution-architect wrote the draft plan of imgui-draw-list (5 phases, 40 tasks). It assumes option (a) of CR-2 and a test project with no GPU, and it lists five open scope questions in its Notes.
- The inbox holds no line. No user message reached the run.
- Next: the human checks, the user decisions on CR-1 and CR-2, and the commit by the caller.

### 2026-10-20 (phase 4, group 1)
- Tasks 4.1 to 4.3 done. The hook is `internal virtual void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` on `Element` (Decision 8). The walk calls it in step 7, after the clip push and before the frame push. The `internal override` in the test probe compiles and loads, so the risk fallback (`protected internal`) is not needed.
- The probe draws `"hi"u8` at the top-left of the border box, font 0, size 16, white scaled by the opacity.
- Build: 0 warnings, 0 errors. Solution tests: total 619 (614 before, 5 new), failed 0. `ThousandElementAllocationTests`: total 8, failed 0 (the draw gate still reads 0 bytes with the hook call).
- Demo, `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*DrawsContentBetweenTheBackgroundAndTheChildren' --show-live-output on`, prints:
  ```
  Begin 400x300 scale 1
  Rect 0,0,100,50 fill #0A141EFF border 0 #00000000 radius 0,0,0,0
  PushClip 0,0,100,50
  Text 0,0 font 0 size 16 #FFFFFFFF "hi"
  Rect 0,0,20,10 fill #00FF00FF border 0 #00000000 radius 0,0,0,0
  PopClip
  End
  ```

### 2026-10-20 (phase 3)
- Tasks 3.1 to 3.6 done. The solution test total is 614, with 0 failed. The build gives 0 warnings and 0 errors.
- Task 3.1: the builder holds 73 settings rows, so the count of 74 in task 3.4 is right (the panel plus 73 rows). The builder has a constant `SettingsRowCount` now. Fills: 299 elements. The tree has no `BorderWidth` outside the 10 footer keys. Each existing assert of `TheMeasuredBodyChangesEachPart` passes with no change of a constant, so no border moved.
- Task 3.4: `CountPainted` in the test mirrors the walk rules with public members and `ScaleAlpha`. The Rect count equals the count of `CountPainted` in the hidden frame and in the visible frame.
- Task 3.5: the demo class gives `total: 8` and `failed: 0` in each of 5 runs (about 3.0 s each, including the build check). The gate asserts `after - before == 0`, and the asserts pass, so the byte result is 0 in each of 5 runs. The test prints no byte value. `scripts/test-linux.sh` prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 13 s`, and `exit=0`.
- Task 3.6: with `GC.KeepAlive(new object());` after `Draw` in `Frame`, `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` gives `exit=2`. After the restore, the same command gives `exit=0`. The `diff` of the restored file and the copy (`/tmp/claude-1000/brutalui-gate/nKTPVy/copy.cs`) gives no difference. The restore reversed the edit with the Edit tool, in place of a file copy.

### 2026-10-20 (phase 1, group 2)
- Tasks 1.5 to 1.11 done. The demo prints the 7 golden lines. The test total goes from 574 to 592, with 0 failed.
- Follow-up for phase 2: task 2.7 (`KeepsTheOrderWhenTheStackGrows`) is the first test of the stack growth. Phase 1 has no tree deeper than 32 levels.

### 2026-10-20 03:10
- `/start-feature` scaffolded `context.md`, `tasks.md` (32 tasks in 4 phases), and `inbox.md`. The plan status is In Progress.

## Hand-offs

Phase 4, group 2 (2026-10-20). Tasks 4.4 to 4.7 done.

**Results**
- Lint (`ste-lint.py --fail-over 2.5`), exit 0 for each file: `docs/technical-design.md` 1.15 per 100 words, `CHANGELOG.md` 1.61, `docs/features/imgui-renderer/human-testing.md` after the fix (the title em dash became a hyphen), `docs/features/imgui-renderer/renderer-interface/implementation.md` 1.47 (no edit needed).
- Build: 0 Warning(s), 0 Error(s). Tests: 619 total, 0 failed, 619 succeeded.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 9 s`, `exit=0`.
- `Directory.Packages.props` and `scripts/` are unchanged.

**imgui-draw-list**
- `Begin` can get a `NaN` component (a viewport with no limit).
- `PushClip` gets an intersected rect, so `PushClipRect(min, max, false)` or `true` both work.
- `Rgba8` casts to `ImU32` with `Unsafe.As`.
- `Corners` maps to four rounding values. ImGui rounds all corners with one radius, so the renderer must pick a rule (for example the largest radius with the corner flags of the non-zero corners).
- `MeasureText` treats a wrap width of 0 or less, or `NaN`, as no wrap.

**label-and-image**
- Override `PaintContent` and scale each color with the opacity argument.
- The hook gets the border box only, so the label resolves its own padding and border for the text position.
- Limit (CR-10): a percent padding resolves against the inner width of the parent, and for a root against the viewport. An element holds no context reference, and `LayoutNode` stores `Rect` only. Thus a root `Label` with a percent padding cannot find its content box in the hook. Options: the layout pass stores the resolved padding and border in `LayoutNode`, and the hook gets the content box as a second argument (the hook is internal, so the change is cheap), or label-and-image documents the limit.
- Open: the way a measure callback reaches `MeasureText` during `Update`. Options: a measurer reference on the label at creation, or a renderer slot on `UiContext` that the label reads through its root. This feature exposes the interface only.
- `RecordingRenderer.MeasureText` returns `(length * size * 0.5, size)` and can change for the wrap tests.

**hello-panel-sample**
- Record the new memory facts line (the paint members grow `ComputedStyle`).
- Decide on a draw benchmark. `RecordingRenderer` is already linked into the benchmark project, so a `Draw` benchmark on `ThousandElementTree` needs one new class.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
