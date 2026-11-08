# panels-and-modes/panel-sample - Code Review

**Reviewed:** 2026-11-08
**Reviewer:** Senior C# engine reviewer (cross-phase review of phases 1 to 3)
**Rules Applied:** 1. Decision 1/2/3 fidelity in `HelloModes.cs` (one owner file, one registry, build-time roots, register order, static handlers, allocation-free handler path), 2. Decision 4/5/6 fidelity in `Program.cs` (toggle source, key parity, cached `Add` point, allocation-free frame lines, counts after `Update`), 3. The line-7 print and the mode check in `Run`, 4. Gate integrity (clean measured bodies, fixed sums, real-work shape, release before measure, plants 3 to 5), 5. The tree and style contract (Decisions 7 and 8), 6. Integration with siblings (mode-set, panel-definition, slot-container, state-and-commands), 7. API clarity and docs (XML docs, remark topics, ASD-STE100), 8. Test strength (a rule pinned by a test AND a plant), 9. Scope discipline, 10. The carried open item (`HelloState state` in `PushSmokeInput`). The Vercel React rules do not apply to C#.
**Status:** Resolved - 3 findings, 3 resolved (2026-11-08: CR-1 five prose lines reworded to clear prohibitions; CR-2 unused `HelloState state` parameter dropped from `PushSmokeInput`; CR-3 stale smoke counts in section 14 marked pending a windowed re-measure, with a Needs Human Verification line. Review-fix verifier: PASS, Demo PASS, build 0/0, solution 2824 (3 skipped) 0 failed, `src/` snapshot holds.)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 1 | CR-3 |
| LOW | 2 | CR-1, CR-2 |

The code does what the plan asks. Rules 1, 2, 3, 4, 5, 6, 8, and 9 pass with no finding.

Rule 1 passes. `HelloModes.cs` owns the whole mode model in one file: the `PanelContext`, the `UiManager`, the three panel registers, the layout register, the two modes, the two commands, the two actions, the F1 and F2 keys, and the `Mode (F1)` button (Decision 1). The two commands join `state.Map.Commands`, so one registry serves the map, the buttons, and the panel context, and `HelloState` changes no code line (Decision 2). The constructor calls `EnsureRoot()` on each of the three panel controllers before it registers the layout and the modes, so every root exists at build time and no factory runs in a frame (Decision 3). The register order is panels, layout, then modes (line 148 to 177), which `Register(ModeDefinition)` needs. `s_mode` and `s_back` are `private static readonly CommandHandler` fields with `this` as the target (Q4). The handler path holds only a `??` on two stored references, `ReferenceEquals`, a `ReadOnlySpan<byte>` ternary, and two `int` increments, so it allocates nothing.

Rule 2 passes. `s_mode` reads `TargetMode ?? CurrentMode` (Decision 4) and counts a back step only when `Back()` returns `true`. `PushSmokeInput` computes the key with `(frameIndex - WarmUpFrames) % 2` and runs in every frame, warm-up included (Decision 5). The C# remainder keeps its sign, so frame 0 and frame 2 give F2 and frame 1 gives F1, as the plan states. `RunFrames` reads `modes.Manager.CurrentMode` into a local right after `ui.Update` (line 839), caches `addPoint` only when that local is `modes.Main` (line 843 to 846), and counts `mainFrames` and `detailFrames` from the same local inside `if (measured)` (line 895 to 903), as Decision 6 and task 2.4 require. The mode read, the cache line, and the two counts allocate nothing: a reference read, a reference compare, a `float2` store, and two increments. `PushSmokeInput` keeps the order rule: the mode keys come after the Escape block and before the text burst, the field press, and the move.

Rule 3 passes by code reading. The print at line 401 to 403 gives exactly `HelloPanel: modes: switches 60, backs 59, main frames 59, detail frames 58.` for the documented counts, and the mode check at line 455 to 458 writes exactly `HelloPanel: the synthetic input did not reach the mode switch.` and returns `ExitFailure` when any of the four counts is 0. The check sits inside `frameLimit != NoFrameLimit && result.MeasuredFrames >= 2`, after the state check. A frame trace of the code reproduces the four counts: F1 runs in frames 1, 3, …, 119 (60 switches); F2 runs in frames 2, 4, …, 118 (59 backs, because the F2 of frame 0 meets an empty history and the handler does not count it); the measured frames that start in `hello.main` are 3, 5, …, 119 (59); the rest are 58.

Rule 4 passes. The measured bodies are `ToggleStep`, `IdleStep`, and `RunSteps`: no `=>` lambda, no string literal, no `u8` literal, no boxed value, no `new` array, and no `Assert`. The mode names come from the registered `ModeDefinition.Name` spans, so no literal sits in a measured body; this is cleaner than what Q4 allows. The two 0-byte tests assert `Assert.Equal(0L, after - before)` and a fixed measured sum (1200 and 300), plus a warm sum (`WarmUpRounds * 12` and `WarmUpRounds * 3`), so an empty body cannot pass. Both call `AllocationGate.ReleaseAllocationContext()` right before the `before` read. `TheMeasuredBodyDoesRealWork` matches the shape that Decision 9 and task 3.2 name: sum 12, `CurrentMode` is Main, `TargetMode` null, `bottom` holds only the state root, the info root has no parent, the widget root sits in `top`, the label text equals the store value, `History.Count` grows by 2, each factory ran one time, the map `IsActive`, and one idle step gives 3 with no history growth. Plants 3 to 5 map to the recorded outcomes: an allocation in `Apply` fails only the toggle test (4 applies per step, 9600 bytes) and spares the idle test (no target, no apply); an allocation in the handler before the target check fails both (9600 and 2400); an empty `ToggleStep` that returns 12 passes both 0-byte tests and fails only the real-work test, at the history assert. The code makes each outcome possible, and the sums match the byte counts in `context.md`.

Rule 5 passes. `BuildLayout` sets `IsPickable = false` on the root and on both slots (Decision 7), so a point in the gap reaches the game and line 4 keeps its off-panel frames. The sheet holds `.hello-slot-root` with `flex-direction: column`, the `SlotContainer` type rule with no padding and no margin, and `.hello-panel-root` with `margin: 0 0 12px` (Decision 8), and no `.hello-root` remains. Each rule has its one-line comment, and the header now names 33 rules; `grep -c` over the sheet gives 33. The mode row carries `hello-row` and holds the `Mode (F1)` button as the last child of the widget panel, so the Tab order in the remarks (panel, `Click me`, toggle, slider, field, `Mode (F1)`, `Add`) matches the tree. The class names in the remarks (`hello-slot-root`, `hello-panel`, `hello-panel-root`, `hello-widgets`, `hello-row`, `hello-status`) all appear in the sheet.

Rule 6 passes. `SetMode` runs only from a command handler and from `RunImGui` before the loop, so each switch defers to `OnBeforeUpdate`. `Program.cs` holds no `AddRoot(` call and no `Map.Activate(` call: the I2 grep prints only `new HelloModes(` and `SetMode(HelloModes.MainName)`, with no comment false positive. Both modes name `HelloState.Map`, and the `HelloModes` remarks state the same-map `Activate` rule. `ViewportCenter` stays behind the cached point. Every registered name is a `u8` literal.

Rule 8 passes with two notes. The gate rules are pinned by tests and by plants 3 to 5. The smoke rules are pinned by line 7 and the mode check, and plants 1 and 2 guard them; both need a window, so they sit under Needs Human Verification (A8). The focused-field switch path (DoD F1) has no automated pin of its own. The smoke covers it indirectly: the field holds the focus from the last press of the frame before, so line 7 with `switches 60` and `backs 59` proves that a focused field passed F1 and F2 to the map. Two sample rules have no automated exercise. First, the `TargetMode` half of the toggle never runs in the smoke, because the script pushes one mode key per frame, so `TargetMode` is always null at the drain; only the manual demo covers two presses in one frame. Second, the gate scene registers `mda.mode`, `mda.add`, and `mda.cancel` with three `Counter` targets, binds `mda.cancel` to Escape, and never runs a dispatch: no step pushes a click or a key, and no assert reads `Counter.Calls`. The plan prescribes that scene shape, so the finding list records no change, but the command path is decoration in this gate, not proof.

Rule 9 passes. `git status` shows only the planned files: the five sample and test paths, `docs/overview.md`, `docs/technical-design.md`, `CHANGELOG.md`, `implementation.md` (checkboxes and status), and the feature's own `context.md`, `tasks.md`, and `inbox.md`. The two Markup pins are the approved Decision 8 consequence, and their content is right: the selector list now names `.hello-slot-root`, `SlotContainer`, and `.hello-panel-root` in sheet order, `RuleCount` is 33, and `ParsedSheetAllocationTests` reads the `.hello-slot-root` rule with `FlexDirection: column`. `ModeDiffAllocationTests.cs` is the only new test file. `git diff HEAD -- src/` prints nothing, and both `src/` snapshot hashes equal the task 1.1 value `e3b0c442…b855`.

Rule 10 gives CR-2. Rule 7 gives CR-1. The docs accuracy check gives CR-3.

### Baseline (observed, not copied from context.md)

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2824, failed 0, succeeded 2821, skipped 3 (the three GLFW display tests). No flake, no rerun needed.
- `BrutalUi.Framework.Tests` full run: total 225, failed 0, skipped 0.
- Class run `--filter-class '*ModeDiffAllocationTests'`: total 3, failed 0, succeeded 3.
- `tests/BrutalUi.Markup.Tests` passed, which confirms the two updated pins against the real sheet.
- I1 scope: `git diff HEAD -- src/ | wc -l` is 0; both `src/` snapshot hashes are `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- I2 grep (`AddRoot(|Map.Activate(|new HelloModes(|SetMode(`): two hits, `new HelloModes(` at line 600 and `SetMode(HelloModes.MainName)` at line 601.
- F6 grep: no `hello-root` in the sheet; `.hello-slot-root` at line 48, `SlotContainer` at line 55, `.hello-panel-root` at line 61. Rule count in the sheet: 33.
- STE lint (`--fail-over 2.5`): `technical-design.md` 1.53, `overview.md` 2.36, `CHANGELOG.md` 1.62, `context.md` 1.53, `implementation.md` 1.76. All pass.
- Smoke run: not observable here. `dotnet run` stops at `GLFW init failed: PlatformUnavailable` (A8), so line 7 and plants 1 and 2 stay human items. This review judged their code by reading.

---

## Findings

### CR-1: Five new doc lines state a prohibition with a broken negation, so they read as permission

**Severity:** LOW
**Rule:** 7 (API clarity and docs) — ASD-STE100 sentence structure
**File:** `samples/HelloPanel/HelloModes.cs` line 28 to 30; `samples/HelloPanel/HelloState.cs` line 64 to 65 and line 100 to 101; `samples/HelloPanel/HelloScreen.cs` line 320 to 321; `samples/HelloPanel/Program.cs` line 596 to 599

**Problem:** Each sentence wants to say that a caller must not call `Activate` or `AddRoot`. Each one puts `not` after the verb phrase, so the plain reading says the caller does call it. The map rule in `HelloModes.cs` reads:

```csharp
/// diff calls <see cref="UiActionMap.Activate(UiContext)"/> on the map of each apply, and the call changes nothing for
/// the same map. The first apply activates the map before the input pass of frame 0, so the game calls
/// <see cref="UiActionMap.Activate(UiContext)"/> not itself.
```

A reader of the reference sample takes "the game calls `Activate` not itself" as a statement that the game calls `Activate`. The same shape appears in `HelloState.cs` twice ("so the caller calls `Activate` not", "call `Activate` not"), in `HelloScreen.cs` ("the game adds it not"), and in the `RunImGui` comment ("so the sample calls `UiActionMap.Activate` not"). The negation is also the rule that DoD I2 checks with a grep, so the prose must not invert it. The pre-feature text of `HelloState.cs` was correct ("The caller calls `Activate` after the context holds the row"), so this feature introduced the wording. The STE lint does not catch it, because the lint scores readability, not negation. The src/ precedent ("moves its children and not itself") is a grammatical coordination and is not the same shape.

**Fix:** Use an explicit negative in all five places. Prose only; no code line changes.

```csharp
// HelloModes.cs
/// diff calls <see cref="UiActionMap.Activate(UiContext)"/> on the map of each apply, and the call changes nothing for
/// the same map. The first apply activates the map before the input pass of frame 0, so the game does not call
/// <see cref="UiActionMap.Activate(UiContext)"/> itself.

// HelloState.cs, constructor remark
/// The constructor does not activate the map. The first apply of the mode that names the map activates it, so the
/// caller does not call <see cref="UiActionMap.Activate(UiContext)"/> (see <see cref="HelloModes"/>).

// HelloState.cs, Map summary
/// <summary>Gets the action map. The first apply of the mode that names the map activates it; the caller does not
/// call <see cref="UiActionMap.Activate(UiContext)"/> (see <see cref="HelloModes"/>).</summary>

// HelloScreen.cs, BuildLayout remark
/// The factory of <c>hello.layout</c> calls this method (see <see cref="HelloModes"/>). The manager adds the root
/// to the context; the game does not add it (the root rule of <see cref="UiManager"/>).

// Program.cs, RunImGui comment
// … and activates the map before the input pass of frame 0, so the sample does not call
// UiActionMap.Activate.
```

**Effort:** Low

### CR-2: `PushSmokeInput` still takes `HelloState state`, but its body no longer uses it

**Severity:** LOW
**Rule:** 10 (the carried open item); API clarity
**File:** `samples/HelloPanel/Program.cs` line 1021 to 1022 (signature), line 1016 to 1017 (`<param name="state">`), line 828 (the call)

**Problem:** Phase 2 moved the `Add` click from `ViewportCenter(state.AddButton)` to the cached `addPoint`. The body now reads `ui`, `panel`, `widgets`, `addPoint`, and `frameIndex`, and never names `state`:

```csharp
private static void PushSmokeInput(
    UiContext ui, Element panel, HelloWidgets widgets, HelloState state, float2 addPoint, int frameIndex)
```

The parameter survives only as a doc anchor ("<param name="state">The state of the sample. The click at `addPoint` runs its `hello.add` command.</param>"). `context.md` carries it as the one open question of Phase 2. The reader of a reference smoke script should not have to find out whether an unused argument matters.

**Fix:** Drop the parameter, and move the `Add` explanation into the `addPoint` doc. Then the call site at line 828 becomes:

```csharp
PushSmokeInput(ui, panel, widgets, addPoint, framesRendered);
```

```csharp
/// <param name="addPoint">The cached center of the <c>Add</c> button of <see cref="HelloState"/> from the last
/// frame with <c>hello.main</c> as the current mode (Decision 6). The click aims at it, because that button has
/// no parent in a <c>hello.detail</c> frame.</param>
```

**Effort:** Low

### CR-3: The technical-design smoke bullet still prints the line-2 and line-6 results that this feature makes unreachable

**Severity:** MEDIUM
**Rule:** Docs accuracy (review task R.3); Decision 6 and Decision 8 consequences
**File:** `docs/technical-design.md`, section 14, the `**Sample projects**` bullet

**Problem:** Phase 3 rewrote that bullet: it now prints 7 lines and explains the mode script, the cached `Add` point, the mode check, and plants 1 and 2. It kept two result numbers from the run before the modes, and both are now wrong.

1. Line 6 keeps `HelloPanel: state: adds 119, resets 30, versions 149.` Under the per-frame switch, the `Add` click can only hit in a frame that starts in `hello.main`, because the unmounted state panel is not in the tree at the drain. The code caches `addPoint` after a `hello.main` frame and misses in the `hello.detail` frames (the bullet's own mode script says so). So `adds` lands near 60, not 119, and `versions` is `adds + resets`, about 90, not 149.
2. Line 2 keeps `draw commands 7, vertices 1172.` Phase 1 added the mode row with the `Mode (F1)` button to the widget panel, so the last frame now draws one more button: its fill, its border, and its text add vertices. The bullet also still says "The last frame is odd, so the text of the field is empty in it", which the mode script does not change, while the counts around it do change.

A maintainer who reads the doc and then runs the smoke on a windowed host will read a mismatch and treat it as a fault, exactly the confusion Risk 1 and Risk 4 were written to avoid. `context.md` records the numbers only for the build, the tests, and the gate, so the doc overstates what was observed.

**Fix:** Mark the two lines as pending a re-measure, and add the human item. In the bullet, replace:

```markdown
`HelloPanel: last frame: draw commands 7, vertices 1172.`
```

with:

```markdown
`HelloPanel: last frame: draw commands <D>, vertices <V>.` A person re-measures both
counts on a host with a window: the mode row of `Mode (F1)` adds one button to the tree, so the pre-mode counts
of 7 commands and 1172 vertices no longer hold.
```

and replace the line-6 quote `adds 119, resets 30, versions 149.` with `adds <A>, resets 30, versions <A+30>.`, then add one sentence:

```markdown
The mode switch unmounts the state panel in every other frame, so the `Add` click hits in about half of the
frames and `adds` falls near 60. The `resets` count of 30 stays, because the Escape key reaches the map in both
modes.
```

Add one Needs Human Verification line to `context.md` for a re-measure of lines 2 and 6 of the smoke run on a host with a window.

**Effort:** Medium (doc reword plus one human item; no code change)

---

## What's Already Good

- The reference use reads as one file: `HelloModes.cs` holds the context, the manager, the registers, the roots built at build time, the two modes, the two commands, the two actions, the two keys, and the button, and the class remarks state the register order, the build rule, the map rule, the toggle rule, the back rule, the frame rule, and the command rule. A game developer can copy the file, as the Quality Bar asks.
- The handler shape is the allocation-safe one: `private static readonly CommandHandler` fields with the instance as the target, a `TargetMode ?? CurrentMode` read with `ReferenceEquals`, and `ReadOnlySpan<byte>` name properties. Nothing in the path can allocate, and plant 2 shows that the smoke gate would catch it if it did.
- Decision 3 pays off in the smoke: `EnsureRoot()` at build time plus the F1 in frame 1 moves the first style pass and the first layout of the info panel into warm-up, so the first measured frame runs only a plain diff. The frame trace confirms the four documented counts (60/59/59/58) from the code alone, with the C# signed remainder handled the way Decision 5 says (F2 in frames 0 and 2, F1 in frame 1).
- The gate is a true 0-byte proof. `ToggleStep` returns a work sum (4 mode hits plus 8 slot children), so `Assert.Equal(Iterations * 12, sum)` blocks an empty body, and `TheMeasuredBodyDoesRealWork` blocks a body that fakes the sum: plant 5 fails it at the history assert while both 0-byte tests pass. The measured bodies hold no `u8` literal at all, because the mode names come from the registered `ModeDefinition` — stricter than Q4 asks.
- The real-work test pins the integration contract in one place: the same-map `Activate` (`Map.IsActive`), the build-time roots (each factory ran exactly once), the stay-with-no-join (the widget root keeps `top` as its parent), the leave (the info root has no parent), the enter (the state root back in `bottom`), and the store binding through an unmount (`ScoreLabel.Text` equals the value of the store set while the panel was away).
- `BuildScene` asserts `SetFocus(field)` is true, so a silent focus loss cannot lighten the measured body below the shape Decision 9 names.
- Scope discipline is clean: `git diff HEAD -- src/` is empty, both snapshot hashes equal the task 1.1 value, no `.csproj`, `.props`, `.sln`, or `scripts/` file changed, and the only new test file is `ModeDiffAllocationTests.cs`. The two Markup pins are value-only updates of existing tests, and their content matches the new sheet (selector order, 33 rules, and the `.hello-slot-root` rule with `FlexDirection: column`), which the Markup run confirms.
- `HelloScreen.cs` keeps the child-rule contract honest: the remarks restate why the two labels and the image stay direct children, why the score label now takes `.hello-row > Label`, and why the layout root and the slots are not pickable. The `hello-panel-root` margin sits on the panel root only, so the fill, border, hover, and focus rules of `hello-panel` stay untouched.
- `docs/overview.md` holds exactly one added line, the Phase 3 modes contract, and `CHANGELOG.md` holds the three Added lines and the one Changed line, with the feature tag on each. The `**Mode tests.**` bullet now reads in the past, and the new eighteenth-gate bullet follows the shape of the earlier gate bullets with the sums, the bytes, and the plant outcomes.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-3 | Medium | Medium | The doc states smoke numbers the shipped code cannot produce. Fix it before a person runs the smoke on a windowed host, or the run looks like a fault. |
| 2 | CR-1 | Low | Low | Five prose lines in the reference sample teach a false rule. Prose only; it does not touch code. |
| 3 | CR-2 | Low | Low | One dead parameter in a sample helper. Take it out with the next edit of `Program.cs`, and keep the call site and the doc in step. |

---

## Progress

- [x] CR-1: Five new doc lines state a prohibition with a broken negation, so they read as permission (HelloModes.cs 28 to 30; HelloState.cs 64 to 65 and 100 to 101; HelloScreen.cs 320 to 321; Program.cs 596 to 599) — fixed 2026-11-08 (review group 2): all five sentences now use "does not call / does not add"; the flattened-negation grep prints no line; the verifier confirmed each location.
- [x] CR-2: `PushSmokeInput` still takes `HelloState state`, but its body no longer uses it (Program.cs 1021 to 1022) — fixed 2026-11-08 (review group 2): parameter dropped, note folded into the `addPoint` doc, call site updated; build 0/0 and solution 2824 (3 skipped), 0 failed.
- [x] CR-3: The technical-design smoke bullet keeps the line-2 and line-6 results that this feature makes unreachable (docs/technical-design.md, section 14) — fixed 2026-11-08 (review group 1): lines 2 and 6 marked pending a re-measure with the two causes named (mode row vertices; `Add` hits only in `hello.main` frames); Needs Human Verification line added to `context.md`; STE lint 1.54.
