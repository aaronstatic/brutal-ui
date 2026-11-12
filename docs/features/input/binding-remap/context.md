# input/binding-remap - Context & Decisions

**Last Updated:** 2026-11-10 09:55 UTC
**Current Phase:** all five phases complete (44/44 tasks), and the cross-phase review closed 6 of 6 findings
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Plan written (`implementation.md`, 5 phases, 44 tasks, the last feature of the `input` epic).
- ✅ Feature docs scaffolded (`tasks.md`, `context.md`, `inbox.md`).
- ✅ Feature branch `feat/input/binding-remap` from `main` at `d2e0dcf`. No commit and no push in this run.
- ✅ Phase 1 group 1 (tasks 1.2 to 1.11): `UiScheme.Default`, `UiActionMap.Remap.cs` (the listen, the key capture, the
  commit), the listen branch in `ReadFrame`, the four path guards, the `ResetFrame` close, and `RebindListenTests` (10).
  Solution 2963 with 3 skipped, 0 failed, and a clean build. Plants 1 to 4 each exit 2 with the named tests.
- ✅ Phase 1 group 2 (tasks 1.12 to 1.16): the HelloPanel `Rebind (Up)` button, the `hello.rebind` command, the one
  `OnRebindResult` write in the constructor, `RebindText` with `LongestRebindText`, the `RebindLabel`, and the seventh
  label write inside the UI region. Build `0 Warning(s)` / `0 Error(s)`; solution 2963, 0 failed, 3 skipped; Framework
  301. Plant 5 builds with `0 Error(s)` and restores equal under `cmp`. Phase 1 is complete (16/16).
- ✅ Phase 2 group 1 (tasks 2.1 to 2.5): `TryCaptureCandidate` over the seven capturable shapes in the `InputKind`
   order, the full fit table, `DeviceOf(kind)` in place of `RebindPadLive`, the reject that reports in each frame that
   the candidate stays live, and `RebindCaptureTests` (8). Build `0 Warning(s)` / `0 Error(s)`; solution 2971, 0 failed,
   3 skipped; Framework 309; `RebindListenTests` still 10 with `failed: 0`. Plants 6, 7, and 8 each exit 2 with the
   named tests and restore equal under `cmp`. Phase 2 is complete (5/5).
- ✅ Phase 3 group 1 (tasks 3.1 to 3.5): `_defaults`, `_defaultsCount`, and `_defaultsCaptured` with `CaptureDefaults`
   on the first `Activate`, `ResetBindings` with one `Array.Copy` into the existing array, the conflict and restore
   paragraphs of the class remarks, both `<see cref="ResetBindings"/>` switches, and `RebindConflictTests` (4) with
   `RebindResetTests` (4). Build `0 Warning(s)` / `0 Error(s)`; solution 2979, 0 failed, 3 skipped; Framework 317;
   `RebindListenTests` 10 and `RebindCaptureTests` 8 with `failed: 0`; `ActionMapAllocationTests` 7 with 0 bytes. Plants
   9, 10, and 11 each exit 2 with `0 Error(s)` and restore equal under `cmp`.

- ✅ Phase 3 group 2 (tasks 3.6 and 3.7): the HelloPanel `Restore` button, the `hello.restore` command, and the
   `s_restore` handler that calls `Map.ResetBindings()`, plus the restore paragraph of the class remarks. One sample
   file changed. Build `0 Warning(s)` / `0 Error(s)`; solution 2979, 0 failed, 3 skipped; the four rebind classes still
   read 10, 8, 4, and 4 with `failed: 0`. The smoke run stops at `GLFW init failed: PlatformUnavailable`, as before.
   Phase 3 is complete (7/7).

 - ✅ Phase 4 group 1 (tasks 4.1 to 4.5): the pinned `InputKind` values 0 to 8, `UiActionMap.BindingsText.cs` with
    `BindingText`, `TryReadBinding`, and `WriteBindings` over one shared line writer, and `BindingTextWriterTests` (4).
    Build `0 Warning(s)` / `0 Error(s)`; solution 2983, 0 failed, 3 skipped; Framework 321; the four rebind classes still
    read 10, 8, 4, and 4, and `ActionMapAllocationTests` still reads 7 with 0 bytes. Plants 12 and 13 each exit 2 with
    `0 Error(s)` and restore equal under `cmp`. The carried HelloPanel remarks now name the `Restore` button in all three
    places, prose only.

 - ✅ Phase 4 group 2 (tasks 4.6 to 4.9): `ReadBindings` with the private line parser, the range checks of the binds, the
    skip-and-count rule, and the merge per action and scheme pair, plus `BindingsRoundTripTests` (4) and the reader half
    of the byte layout. The `ResetBindings` remarks now hold a real `<see cref="ReadBindings"/>`; the shipped
    `RemoveBindings` kept its two-argument signature. Build `0 Warning(s)` / `0 Error(s)`; solution 2987, 0 failed, 3
    skipped; Framework 325; `BindingTextWriterTests` 4 and `BindingsRoundTripTests` 4 with `failed: 0`; the four rebind
    classes still read 10, 8, 4, and 4, and `ActionMapAllocationTests` still reads 7 with 0 bytes. Plants 14, 15, and 16
    each exit 2 with `0 Error(s)` and restore equal under `cmp`. Phase 4 is complete (9/9).

 - ✅ Phase 5 group 1 (tasks 5.1 and 5.2): `ActionMapAllocationTests` grew `RebindStep`,
    `ARebindAndAResetAllocateNothing`, the two rebind slots over two static handlers, and the build-time assert that the
    binding array holds 32 bindings at capacity 32. The new gate measures 0 bytes over 2 warm-up rounds and 100 rounds,
    and `TheMeasuredBodyDoesRealWork` holds a fixed increment for the rebind step. Build `0 Warning(s)` / `0 Error(s)`;
    solution 2988, 0 failed, 3 skipped; Framework 326; Core 1801; the gate reads a total of 8 with `failed: 0`; the six
    rebind and text classes hold 10, 8, 4, 4, 4, and 4. Plants 17 and 18 each exit 2 with `0 Error(s)`: the new gate
    fails with 4800 bytes and with 2400 bytes, and no other gate moves; each restore is equal under `cmp`. No `src/`
    file and no sample file changed in this group.

 - ✅ Cross-phase review round: `code-review.md` holds 6 findings (3 MEDIUM, 3 LOW), and all 6 shipped. CR-1 gave
  `CaptureDefaults` build-time slack (`_bindingCount + _actionCount * _schemeCount`), so an empty-group rebind commit cannot
  resize the binding array inside `ReadFrame`; the gate now commits onto the empty default-scheme pair and pins the
  post-`Activate` capacity (32 at 110), with plants 19 and 20 sharp. CR-2 and CR-3 wrote the stored-text contract the code
  keeps (the name rule, and the 15-button cap above the checks of the matching bind). CR-6 made `AddScheme` reject the
  reserved token `-`, a stated deviation outside the plan's File Tree. CR-4 stated both slot call sites, and CR-5 moved the
  sample's idle read onto a `RebindDone` flag, so a mouse-button commit reads `Rebind: code 0`. End state: build
  `0 Warning(s)` / `0 Error(s)`, solution 2988, 0 failed, 3 skipped, Framework 326, Core 1801, and the final gate PASS with
  `Demo: MANUAL`.

- ✅ Phase 5 group 2 (tasks 5.3 to 5.7): section 9 gains item 15 **The binding remap** and item 16 **The binding text
    format**, the `### Action map` gains **The remap rules.** and **The binding text.** with the corrected build rule and
    the grown cost list, section 14 gains **Remap tests.**, and `CHANGELOG.md` gains one line. The prose pass cut
    `implementation.md` from 3.25 to 2.48, in its non-task sections only. Build `0 Warning(s)` / `0 Error(s)`; solution
    2988, 0 failed, 3 skipped; Framework 326; Core 1801; the six class filters read 10, 8, 4, 4, 4, and 4, and the gate
    reads 8, each with `failed: 0`. The lint exits 0 on the four docs: 1.53, 1.51, 1.83, and 2.48. No `src/`, `tests/`,
    `samples/`, or `scripts/` file changed. The feature is complete, and it closes the `input` epic.

**What's Next:**
- 🟢 Nothing in code. The feature is Ready for Review: a person runs the display-host checks below, then commits, pushes,
  and opens the PR. This run made no commit and no push on purpose.
- 📋 The display-host checks in Needs Human Verification: the windowed HelloPanel rebind, cancel, and restore sequence,
  the mouse-commit label read, the smoke PASS line, and the re-measured smoke line 2 counts.
- 📋 Two items for the next pass that may open a frozen file: the `AddAction` separator guard that CR-2 leaves as a named
  follow-up (`UiActionMap.Bindings.cs` is byte-frozen here), and the choice on section 14's historical `(7 tests)` figure.

**Blockers:**
- None for the headless gate. The HelloPanel demo needs a host with a window: the sandbox blocks the X11 socket of
  `scripts/test-linux.sh`, as in every sibling feature.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Actions/UiActionMap.Remap.cs` - new (phase 1, 2, 3): the listen, the capture scan, the fit test, the commit, the conflict report, `RemoveBindings`, `ResetBindings`, and the defaults snapshot.
- `src/BrutalUi.Framework/Actions/UiActionMap.BindingsText.cs` - new (phase 4): `WriteBindings`, `ReadBindings`, the line codec, and the internal `BindingText` handle (review CR-5 of input-actions).
- `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs` - edited (phase 1): the listen branch in `ReadFrame` and the five silence guards.
- `src/BrutalUi.Framework/Actions/UiActionMap.cs` - edited (phase 1, 3, 4): the listen fields, the `ResetFrame` close, the `CaptureDefaults` call in `Activate`, the class remarks, and the explicit `InputKind` values.
- `src/BrutalUi.Framework/Actions/UiScheme.cs` - edited (phase 1): `public const int Default = 0` (deferred review CR-8 of control-schemes).
- `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` - must stay byte-identical (the 18 `Bind*` members and `FindClash`).

### Related Files
- `samples/HelloPanel/HelloState.cs`, `HelloScreen.cs`, `HelloModes.cs`, `Program.cs` - the three buttons, the seventh label, and the write inside the `uiStartBytes`/`uiEndBytes` region.
- `tests/BrutalUi.Framework.Tests/Actions/` - six new classes and the grown `ActionMapAllocationTests`.
- `docs/features/input/binding-remap/implementation.md` - the plan, and the source of the task text in `tasks.md`.
- `docs/features/input/epic-overview.md` - the epic rollup that this feature closes.

---

## Baseline Numbers

Recorded by task 1.1 at the start of the build. See the Phase 1 Group 1 Record for the measured state.

- Base commit: `d2e0dcf` (feat/input/control-schemes), the tip of `main` when the branch started.
- `dotnet test --solution brutal-ui.sln`: 2953 tests, 0 failed, 3 skipped.
- `BrutalUi.Core.Tests`: 1801. `BrutalUi.Framework.Tests`: 291.
- Expected end state: Framework 326 (291 + 35), solution 2988, Core 1801, 3 skipped.

---

## Phase 1 Group 1 Record

Built by the `game-dev-advanced` agent on 2026-11-10, tasks 1.2 to 1.11. Group 2 (the HelloPanel sample, tasks 1.12
to 1.16) did not run: it is outside this brief.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2963, failed 0, succeeded 2960, skipped 3,
  exit 0. The skipped 3 are the display-only Glfw tests, as at the base.
- `BrutalUi.Framework.Tests`: 301 (291 + 10). `BrutalUi.Core.Tests`: 1801. `RebindListenTests` class run: total 10,
  failed 0.
- Each named class passes with no edit, at its base count: `UiActionMapTests` 23, `GamepadActionTests` 7,
  `FocusActionTests` 5, `TypedActionMapTests` 9, `GamepadValueActionTests` 8, `ActionQueryTests` 10,
  `ActionMapSwitchTests` 6, `SchemeGroupTests` 8, `ActiveSchemeTests` 9, `SchemeChangedSlotTests` 7,
  `SchemeLockTests` 7, `ActionMapAllocationTests` 7 (the seventeenth gate, unmoved), `ModeActionMapTests` 5.
- The ten tests: `BeginRebindChecksTheSchemeAndGivesFalseForAnUnknownAction`,
  `AFreshKeyPressAfterTheListenCommitsTheBinding`, `TheListenGivesNoValueAndRunsNoCommandOnEveryPath`,
  `EscapeCancelsTheListenBeforeTheMapsOwnCancelBindCanFire`, `TheCancelKeyIsOneSettablePropertyWithARangeCheck`,
  `TheListenIgnoresTheInputThatReachedItInTheOpenFrame`, `ARebindCapturesAKeyThatTheRoutingGateEmpties`,
  `ASecondBeginRebindMovesTheListenToTheNewTarget`, `ActivateAndDeactivateCloseTheListenWithNoCommitAndNoSlotCall`,
  `TheDeviceTrackAndTheSchemeSlotRunInASuppressedFrame`. All pass.

### Plants

Each plant builds first, and each build reads `0 Warning(s)` and `0 Error(s)`. The full suite ran for each plant.
Logs and the `g1-*.orig.cs` baselines sit in `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored). Each restore
passed `cmp`.

| Plant | Change | Exit | Failed tests |
|---|---|---|---|
| 1 | `RebindScan` reads `context.GameInput` | 2 | `ARebindCapturesAKeyThatTheRoutingGateEmpties` (Q1) |
| 2 | The listen branch falls through to the four passes | 2 | `TheListenGivesNoValueAndRunsNoCommandOnEveryPath`, `AFreshKeyPressAfterTheListenCommitsTheBinding`, `TheDeviceTrackAndTheSchemeSlotRunInASuppressedFrame` (Q9) |
| 3 | `RemoveBindings` runs after `Append` | 2 | `AFreshKeyPressAfterTheListenCommitsTheBinding` at its `BindingCount` assert (Expected 1, Actual 0), plus `TheCancelKeyIsOneSettablePropertyWithARangeCheck`, `ASecondBeginRebindMovesTheListenToTheNewTarget`, `ARebindCapturesAKeyThatTheRoutingGateEmpties`, `ActivateAndDeactivateCloseTheListenWithNoCommitAndNoSlotCall` |
| 4 | `BeginRebind` sets `_rebindSettle = 0` | 2 | `TheListenIgnoresTheInputThatReachedItInTheOpenFrame`, `TheDeviceTrackAndTheSchemeSlotRunInASuppressedFrame` |

Plant 2, 3, and 4 each fail more tests than the brief names. Each extra failure is the same rule seen from another
test, so the plant stays sharp.

### Surface checks (task 1.11)

- `grep -n 'BeginRebind\|_rebindOpen' src/BrutalUi.Framework/Actions/*.cs`: `_rebindOpen` guards four path entries in
  `UiActionMap.Read.cs` (`PushGamepad`, `InvokeAction`, `PressKey`, `PressMouseButton`), the branch in `ReadFrame`, the
  listen members in `UiActionMap.Remap.cs`, and the close in `ResetFrame`. `HandleInput` holds no guard, as planned.
- `git diff -- src/BrutalUi.Core/ src/BrutalUi.Glfw/ src/BrutalUi.ImGui/ src/BrutalUi.Markup/
  src/BrutalUi.Framework/Panels/UiManager.cs` gives no output (A2).
- `git diff src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` gives no output; `grep -c 'public void Bind'` reads
  18. No `.csproj`, `.props`, `.sln`, or `scripts/` change.
- `UiScheme.Default` reads `public const int Default = 0` at `UiScheme.cs:35`.
- The Q6 shape grep of `UiActionMap.Remap.cs` gives no hit: no array creation, no `Array.Resize`, no `Array.Clear`, no
  `TryParse`, no `CultureInfo`, no `lock`, no `event`, no `(object)` cast, no `List<`.

### Demo state

The headless demo is green: `RebindListenTests` reads 10 with `failed: 0`. The HelloPanel seventh-label demo is group 2
work, and `scripts/test-linux.sh` was not run in this group. It stops at `GLFW init failed: PlatformUnavailable` on
this host, as in every sibling feature, so the live click check stays with a person.

### Decisions that the plan did not settle

- **The device track in a no-candidate frame.** Task 1.5 says `device` is the id of the *candidate* kind, which gives no
  device for a frame with no candidate. Task 1.9's `TheDeviceTrackAndTheSchemeSlotRunInASuppressedFrame` names a frame
  with a live stick and trigger and no key edge, and expects `ActiveScheme` to move to the Gamepad id and
  `OnSchemeChanged` to run once. Q9 and the Integration Points row both say the track keeps running in a suppressed
  frame. Settled with one narrow helper: `RebindPadLive(in UiGamepadState)` reads the raw pad slot (a button down, a
  button edge, or an axis that is not exactly 0) and gives the Gamepad id. A captured key still writes the KeyboardMouse
  id, and a frame with nothing live keeps the last report. Phase 2 replaces the helper with `DeviceOf(kind)` over the
  seven shapes, because then a live pad is a candidate.
- **The pressed keys inside the silence test.** Task 1.9 says test 3 pushes "a held W, and a Space press" during the
  listen. On a Button target a fresh key edge is a fitting candidate, so it commits and closes the listen, and the rest
  of the test cannot read. The test pushes the W down and the Space down *before* the listen, and pushes
  `IsRepeat = true` for both inside the three listen frames. A repeat writes no press edge (Decision 2), so the listen
  stays open, and the same shape survives phase 2, where the stick and the trigger become rejected candidates.
- **Test 7 target scheme.** The test calls `BeginRebind(Fire, UiScheme.Default)`, so the map binds `Fire` to Space in
  the default scheme, and the commit replaces that one binding: `BindingCount` stays 1.
- **Test 8 count.** The second `BeginRebind` moves the listen to `Jump` in the Gamepad scheme, whose group already holds
  one binding, so the commit replaces it: `BindingCount` reads 2 before and 2 after, not 3.
- **The `ResetBindings` cref.** Task 1.3 names `ResetBindings` in the re-entry rule of both delegates. The member lands
  in phase 3, and an unresolvable cref is a build error, so the docs hold `<c>ResetBindings</c>` now. A phase 3 edit
  should switch both to `<see cref="ResetBindings"/>`.
- **`RebindNoKey` and `RebindDeltaThreshold`.** `RebindNoKey` (-1) carries the `CancelRebind` code and the
  `RebindCancelKey` range check. `RebindDeltaThreshold` (8f) is unused in phase 1 and gives no warning; phase 2 reads
  it. `RemoveBindings` returns the count of removals, as its declared shape says; phase 4's reader uses the value.

### Prose lint

The DoD Q5 bar (`no score above 2.5`) cannot hold on a whole C# file, because the linter reads a code line as a
sentence: the shipped base files read 6.65 (`UiActionMap.Read.cs`), 2.64 (`UiActionMap.cs`), 13.79
(`ActionQueryTests.cs`), and 11.38 (`SchemeGroupTests.cs`). Measured on the doc-comment prose only, every file of this
group is inside the bar: `UiActionMap.Remap.cs` 0.61, `RebindListenTests.cs` 0.83, `UiScheme.cs` 0.59,
`UiActionMap.cs` 1.25, `UiActionMap.Read.cs` 1.66. On the whole files the group reads 3.58 (new), 5.77 (was 6.65),
2.57 (was 2.64), and 2.41 (`UiScheme.cs`): the additions lowered both edited source files.

### Blast radius and migration risks

- Shared code touched: `ReadFrame` (one branch after its pair test), the four path entries
  (`PushGamepad`, `InvokeAction`, `PressKey`, `PressMouseButton`), and `ResetFrame`. `HandleInput`, the five queries,
  the 18 binds, `FindClash`, `Append`, and the whole scheme table keep their text.
- A wrong placement of the branch breaks `ActionMapSwitchTests` (the `IsActive` pair test) or the seventeenth gate.
  Both run green with no edit, and the gate scene never opens a listen, so the closed-listen cost is one bool test.
- `PushGamepad` now stores its buttons and returns 0 in a listening frame, so an edge read there is lost and not
  queued (the control-schemes CR-7 habit). `UiActionMapTests` (23) and `GamepadActionTests` (7) pin the closed-listen
  path.
- Follow-up for phase 2: replace `TryCaptureKey` with `TryCaptureCandidate`, replace `FitsCapture` with the full table,
  and fold `RebindPadLive` into `DeviceOf(kind)`. Each edit touches one method, as Decision 11 shapes it. (Done: see the
  Phase 2 Group 1 Record.)

---

## Phase 1 Group 2 Record

Built by the `game-dev-advanced` agent on 2026-11-10, tasks 1.12 to 1.16: the HelloPanel sample. Four files changed:
`HelloState.cs`, `HelloScreen.cs`, `HelloModes.cs`, and `Program.cs`. No file of `src/` and no test file changed in
this group.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. The same read
  after the plant 5 restore.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2963, failed 0, succeeded 2960, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 301, `BrutalUi.Core.Tests` 1801. The group adds no test, so the counts equal the
  group 1 end state. The skipped 3 are the display-only Glfw tests, as at the base.

### Smoke line

`MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` passes preflight, build, test, gpu, and loader, then
stops at the smoke step: `test-linux: FAIL at smoke (exit 1)` and
`HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, `exit=1`. The full log sits
in `artifacts/test-linux/smoke.log`. The cause is the sandbox `/tmp` isolation of the X11 socket, as in every sibling
feature. Not a code fault. The live run and every display item are in Needs Human Verification below.

### Plant 5

`GC.KeepAlive(new object());` as the first line of `HelloScreen.RebindText`. The build with the plant read
`0 Warning(s)` and `0 Error(s)` (tool rule: a plant result counts only after `0 Error(s)`). The sample holds no headless
observer: no test runs `RebindText`, so the suite cannot show the bytes. On a host with a window, smoke line 3 (the
frame allocation sum) grows by 24 bytes per planted frame, one `new object()` per label write. Restore with `cp` and
`cmp`: the file equals its baseline `g2-HelloScreen.cs.orig` in `samples/HelloPanel/obj/plantscratch/`, and the
rebuild reads `0 Warning(s)` / `0 Error(s)`.

### Surface checks

- `git diff --name-only`: the four sample files plus the three `src/` edits of group 1. `UiActionMap.Bindings.cs`,
  `src/BrutalUi.Core/`, `src/BrutalUi.Glfw/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Markup/`, `UiManager.cs`, every
  `.csproj`, `.props`, `.sln`, and `scripts/` file keep an empty diff.
- `HelloState.cs`: one `commands.Register("hello.rebind"u8, ...)`, one `Map.OnRebindResult = OnRebind;` write in the
  constructor, one `Rebind (Up)` button, `KeyboardScheme`, `RebindStatus`, `RebindCode`, and the `OnRebind` store.
- `HelloScreen.cs`: `RebindText` and `LongestRebindText` (`"Rebind: rejected 348"u8`, 20 bytes) added; the seventh
  label seeds with the longest text and sits right after the scheme label.
- `HelloModes.cs`: `_rebindLabel` field, `RebindLabel` property, null check, `out _rebindLabel` argument, remarks list.
- `Program.cs`: `rebindScratch` (32 bytes) and one `RebindText` call right after the scheme label write, inside the
  `uiStartBytes`/`uiEndBytes` region, so the gate covers the listen read. `PushSmokeInput` is unchanged: the synthetic
  run opens no listen, so `resets 30` and the six existing smoke lines keep their meaning.
- No stale `six labels` or `six out fields` mention remains in `samples/HelloPanel/`.

### Decisions that the plan did not settle

- **The idle test.** `RebindStatus` starts at 0, and `UiActionMap.RebindBound` is also 0, so `RebindText` treats the
  pair (status 0, code 0) as the start state and writes `Rebind: idle`. A bound result always carries the pressed key
  code, and a real key code is 32 or above, so the pair cannot come from a commit. The rule sits in the `RebindText`
  remarks and on the two properties.
- **The `listening off` tail.** The write of `Rebind: listening off` is the defensive last branch for a status outside
  the three consts. The result slot stores only those three, so the branch is unreachable in the sample and the
  reachable longest text stays `Rebind: rejected 348`, the 20-byte seed. The seed choice of the brief holds.
- **The focus walk.** The class remarks now name `Rebind (Up)` in the focusable list and in the Tab order after
  `Add` (tree order in the state row). No code rule changed.
- **The button text line.** `RebindButton = new Button(...).BindCommand(...)` wraps at 120 columns in the style of the
  `ModeButton` line of `HelloModes.cs`; the text and the command name are word for word from the brief.

### Prose lint

Extracted doc-comment and comment prose, linted with
`python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5`: `g2-HelloState.prose.txt`
0.72, `g2-HelloScreen.prose.txt` 0.98, `g2-HelloModes.prose.txt` 0.87, `g2-Program.prose.txt` 1.48 hits per 100
words. All four are inside the 2.5 bar.

### Blast radius and migration risks

- Shared code: none. The group touches only the four sample files. The listen, the commit, and the frame path stay as
  group 1 shipped them.
- The frame path of the sample grows by one `bool` read, two `int` reads, and one span-format write per frame, all
  inside the measured UI region: 0 bytes on the closed-listen path.
- Phase 3 adds the `Restore` button to the same row, so the smoke line 2 vertex and draw-command counts move again
  after they settle in a human run of phase 1. Record the human baseline numbers in the phase 3 run, not here.
- Open question 3 of group 1 stands: both delegate remarks hold `<c>ResetBindings</c>` as plain text. A phase 3 edit
  switches them to `<see cref="ResetBindings"/>`. Not touched in this group.

---

## Phase 2 Group 1 Record

Built by the `game-dev` agent on 2026-11-10, tasks 2.1 to 2.5: the seven capture shapes, the fit table, and the
mismatch reject. Three files changed: `UiActionMap.Remap.cs` (widened, now 812 lines), `RebindCaptureTests.cs` (new,
714 lines, 8 tests), and `context.md`. `UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, `UiActionMap.cs`, every sample
file, and every project file keep the diff that phase 1 left them with.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. The same read
  after each plant restore.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2971, failed 0, succeeded 2968, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 309 (301 + 8), `BrutalUi.Core.Tests` 1801. The skipped 3 are the display-only Glfw
  tests, as at the base.
- `--filter-class '*RebindCaptureTests'`: total 8, failed 0. `--filter-class '*RebindListenTests'`: total 10,
  failed 0. Each of the twelve existing classes passes with no edit: the whole Framework assembly runs green at 309,
  and phase 1 pinned each class at its base count.
- The eight tests: `AMouseButtonPressCommitsAMouseBinding`, `AGamepadButtonPressCommitsABindingWithNoModifier`,
  `AStickAwayFromRestCommitsAStickBinding`, `ATriggerAboveRestCommitsAnAxisBinding`,
  `APointerDeltaAboveTheThresholdCommitsADeltaBinding`, `ANonZeroScrollCommitsAScrollBinding`,
  `TheScanTakesTheFirstCandidateInTheTableOrder`, `AMismatchedCaptureStaysInTheListenAndReportsTheReason`. All pass.
- The demo holds: `--filter-class '*RebindCaptureTests'` reads a total of 8 with `failed: 0`.

### Plants

Each plant builds first, and each build reads `0 Warning(s)` and `0 Error(s)`. The full suite ran for each plant. The
baseline `p2-UiActionMap.Remap.cs.orig` and the logs `p6run.log`, `p7run.log`, and `p8run.log` sit in
`src/BrutalUi.Framework/obj/plantscratch/` (git-ignored). Each restore passed `cmp`.

| Plant | Change | Exit | Failed tests |
|---|---|---|---|
| 6 | `GamepadAxis` is scanned before `GamepadStick` | 2 | `AStickAwayFromRestCommitsAStickBinding`, the stick half of `TheScanTakesTheFirstCandidateInTheTableOrder`, plus `ATriggerAboveRestCommitsAnAxisBinding` (A5) |
| 7 | `FitsCapture` accepts a `Key` for a Vector2 action | 2 | `AMismatchedCaptureStaysInTheListenAndReportsTheReason` — a dead binding lands, plus `TheScanTakesTheFirstCandidateInTheTableOrder` (Q4) |
| 8 | The delta rule reads `!= (0, 0)` in place of the threshold | 2 | `APointerDeltaAboveTheThresholdCommitsADeltaBinding` only (Decision 2) |

Plant 6 and 7 each fail one more test than the brief names. Each extra failure is the same rule seen from another test,
so the plant stays sharp. The plant-6 run also printed the known `Fatal error. Internal CLR error. (0x80131506)` line;
the totals above come from the log of the same run, which named the three failures.

### The three rules that this phase owns, for section 9

1. **The seven capturable shapes, with the `InputKind` number that each one keeps.** Key (0): the first code in 0 to 348
   with `Keys.WasPressed`, `code = k`, `mods = Keys.Modifiers & MatchMask`. MouseButton (1): the first index in 0 to
   `UiPointerButtonState.ButtonCount - 1` with `Buttons.WasPressed`, `code = (int)button`, `mods` as a key.
   GamepadButton (2): the lowest set bit of `pad.Pressed`, `code = that bit`, `mods = None`. GamepadStick (4):
   `pad.LeftStick` then `pad.RightStick`, not (0, 0), `code = (int)UiGamepadStick`, `mods = None`. GamepadAxis (5):
   `pad.Axis(a) != 0f` for a in 0 to 5, after the sticks, `code = a`, `mods = None`. MouseDelta (7): the length of
   `input.Pointer.Delta` above `RebindDeltaThreshold = 8f`, `code = -1`, `mods = None`. Scroll (8):
   `input.Pointer.ScrollDelta.Y != 0f`, `code = -1`, `mods = None`. Every capture writes `Code1` to `Code3 = -1` and
   `Scale = 1f`. `KeyVector` (3) and `KeyAxis` (6) hold two or four keys, so no rebind captures them.
2. **The fixed scan order, one candidate per frame.** Key, MouseButton, GamepadButton, GamepadStick, GamepadAxis,
   MouseDelta, Scroll: the `InputKind` order, with the two composites skipped. The stick sits before the axis, so a stick
   move never gives a `GamepadAxis` capture of axes 0 to 3. A digital shape needs a fresh press edge; an analog shape is
   live when its stored value is not exactly 0, because the poll baked the deadzone in (A5). The scan takes the first
   live shape and stops, even when the second would fit. The cancel key beats the fit test, and a reject reports once in
   each frame that the candidate stays live (Decision 10). The pointer delta compares `x*x + y*y` against the squared
   threshold with no `sqrt`, and a length that is not finite gives no candidate.
3. **The fit table, and the device table that rides on it.** Button takes `Key`, `MouseButton`, and `GamepadButton`;
   Axis takes `GamepadAxis` and `Scroll`; Vector2 takes `GamepadStick` and `MouseDelta`. A composite rebinds as one
   binding only through the text format, which is Out of Scope of the listen. `DeviceOf(kind)` is the frame read's
   device track: `Key`, `MouseButton`, `MouseDelta`, and `Scroll` (and the two composites) give
   `UiScheme.KeyboardMouse`; `GamepadButton`, `GamepadStick`, and `GamepadAxis` give `UiScheme.Gamepad` (Q9).

### Decisions that the plan did not settle

- **`TrailingZeroCount`, not `Log2`, picks the lowest pad bit.** Task 2.1 names `BitOperations.Log2` to reach the lowest
  set bit of `pad.Pressed`. `Log2` gives the index of the *highest* set bit, so `X | Y` would capture `Y`. The rule that
  the plan and the test both name is "the lowest bit wins", so the shipped line reads
  `1 << BitOperations.TrailingZeroCount((uint)pressed)`, which gives `X` (code 4).
- **The `RebindPadLive` helper is gone.** Phase 1 left it as the stand-in for the pad device in a listen frame. Every pad
  shape is now a candidate, so the helper folds into `DeviceOf(kind)` at the end of `RebindScan`, as the phase 1 record
  planned. One behavior narrows: a frame whose pad holds a button *with no edge* and every axis at rest now keeps the
  last device report instead of moving it to Gamepad, because no shape is live. `TheDeviceTrackAndTheSchemeSlotRunInASuppressedFrame`
  holds a live stick, so it still moves the track to Gamepad: the stick is a rejected candidate for its Button target, and
  `DeviceOf(GamepadStick)` gives the Gamepad id. Section 9
  should state the device rule as "the device of the live candidate, else carry the last report".
- **A stick at rest and a raw axis above rest are the same test.** The brief's fourth test says a raw axis 2 at 0.6 is
  "captured as `GamepadAxis code = 2`". `UiGamepadState.RightStick` is (axis 2, axis 3), so a non-zero axis 2 is a live
  stick, and the stick sits first in the order. The plan of record says the same (`implementation.md:225`), so the test
  pins the order rule instead: axis 2 at 0.6 with the trigger at 0.3 gives a `GamepadStick code = 1` capture, the Axis
  target rejects it, the listen stays open, and the next frame with the stick at rest commits `GamepadAxis code = 4`. A
  `GamepadAxis` capture of codes 2 and 3 is unreachable through the listen, and the record now says so for section 9.
- **The Button latch needs a fresh press after a commit.** The shipped finish pass starts the held latch from
  `oldHeld || press`, and a listen frame writes no flag, so a binding that lands cannot show `IsHeld` in the next frame
  from a button that is merely down. Tests 1 and 2 release and press again in a read frame to prove the kind, the code,
  and the modifier set of the new binding. This is the phase 1 consumed-edge rule seen from the finish pass.
- **Test 2 rebinds in the scheme that holds the old binding.** The group rule of Q5 removes only the bindings whose
  action index *and* scheme id equal the target, so the old Space bind must live in the Gamepad scheme for
  `BindingCount` to stay 1. The first draft bound it in KeyboardMouse and read 2.
- **The non-finite delta guard is reachable only through an overflow.** `UiPointerState.FinishFrame` writes the delta as
  (0, 0) when either position is not finite (`UiInputState.cs:173`), so no event sequence can put a NaN or an Infinity in
  `Delta`. A length of about 1.4e20 still squares past the float range to +Infinity, so the delta test moves the pointer
  to 1e20 to reach the guard. The guard stays: it costs one `float.IsFinite` on a shape that reads the raw view each
  listen frame.
- **No root in the capture contexts.** The scan reads the raw view, so the tests need no pickable root, and with no root
  hit the game view keeps the pointer and key parts, which the post-commit reads depend on.

### Prose lint

Doc-comment prose only, extracted to `obj/plantscratch/p2doc-*.txt` and linted with
`python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5`:
`UiActionMap.Remap.cs` 0.86 hits per 100 words, `RebindCaptureTests.cs` 1.38. Both are inside the bar. On whole files
the linter reads code lines as sentences, and the group reads 3.77 and 12.59, in line with the shipped base files
(`UiActionMap.Read.cs` 6.65, `ActionQueryTests.cs` 13.79).

### Blast radius and migration risks

- Shared code touched: `UiActionMap.Remap.cs` only, inside `TryCaptureCandidate` (new, replaces `TryCaptureKey`),
  `FitsCapture`, `DeviceOf` (new), `RebindScan`, and the remarks of `BeginRebind`, `FindConflictAction`, and
  `CommitRebind`. `RebindPadLive` is deleted. `TryCaptureCandidate` reads the same raw view that phase 1 pinned with
  plant 1, so Q1 needs no new plant.
- The frame path grows only inside a listen: one pad slot read and up to seven shape tests per suppressed frame, 0 bytes
  (`ActionMapAllocationTests` holds the seventeenth gate at 0 bytes with no edit, and the full suite ran green).
- `PushGamepad` still stores then returns 0 in a listen frame, so a pad edge that arrives during a rejected candidate is
  lost and not queued. A player who holds the pad through a long reject sees no press edge after the listen closes, which
  is the CR-7 rule of control-schemes.
- Phase 3 adds `FindConflictAction` report calls and `ResetBindings` on top of the commit that this group widened. The
  `Code1` to `Code3 = -1` write is the field shape that the phase 4 writer reads with `TryReadBinding`.
- Open item from phase 1 stands: both delegate remarks hold `<c>ResetBindings</c>` as plain text, and a phase 3 edit
  switches them to `<see cref="ResetBindings"/>`.

---

## Phase 3 Group 1 Record

Built by the `game-dev-advanced` agent on 2026-11-10, tasks 3.1 to 3.5: the conflict report pinned by tests, the defaults
snapshot, and the restore. Three files changed: `UiActionMap.Remap.cs` (the snapshot fields, `CaptureDefaults`,
`ResetBindings`, the file remark tail, and the two `ResetBindings` crefs of the delegate remarks),
`UiActionMap.cs` (one call in `Activate`, one `Activate` remark sentence, and the conflict and restore paragraphs of the
class remarks), and the new `RebindConflictTests.cs` and `RebindResetTests.cs` (four tests each). Group 2 (tasks 3.6 and
3.7, the HelloPanel `Restore` button) did not run: it is outside this brief. No sample file, no test of a former phase,
and no project file changed in this group.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. The same read
  after each change and after each plant restore.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2979, failed 0, succeeded 2976, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 317 (309 + 8), `BrutalUi.Core.Tests` 1801. The skipped 3 are the display-only Glfw
  tests, as at the base.
- `--filter-class '*RebindConflictTests'`: total 4, failed 0. `--filter-class '*RebindResetTests'`: total 4,
  failed 0. `--filter-class '*RebindListenTests'`: total 10, failed 0. `--filter-class '*RebindCaptureTests'`: total 8,
  failed 0. `--filter-class '*ActionMapAllocationTests'`: total 7, failed 0, so the seventeenth gate keeps 0 bytes.
  Each of the twelve existing classes passes with no edit: the whole Framework assembly runs green at 317, and phases 1
  and 2 pinned each class at its base count.
- The eight tests: `AConflictKeepsBothAndNamesBothActions`, `AConflictAgainstADefaultBindingReports`,
  `TwoDifferentSchemesGiveNoConflict`, `TheConflictSlotTakesOneHandlerWrittenWithEquals`,
  `ResetBindingsRestoresTheBuildDefaultsAfterARebind`, `TheSnapshotComesFromTheFirstActivate`,
  `ResetBindingsRestoresTheBindingsAndNothingElse`, `ARestoreEndsTheListenWithNoCommit`. All pass.
- Carried item done: both nested delegate remarks now hold `<see cref="ResetBindings"/>` in place of
  `<c>ResetBindings</c>`. The member ships in this group, so the cref resolves, and the build reads `0 Warning(s)`.
- The three readings that phase 2 pinned hold with no edit: a gamepad button capture takes the lowest set bit through
  `TrailingZeroCount`, the stick wins the scan order over axes 2 and 3, and the device of a listen frame with no
  candidate carries the last report. `TwoDifferentSchemesGiveNoConflict` re-pins the third one through a live stick.

### Plants

Each plant builds first, and each build reads `0 Warning(s)` and `0 Error(s)`. The full suite ran for each plant. The
baseline `p3-UiActionMap.Remap.cs.orig` and the logs `p9run.log`, `p10run.log`, and `p11run.log` sit in
`src/BrutalUi.Framework/obj/plantscratch/` (git-ignored, confirmed with `git check-ignore`). Each restore passed `cmp`.
All three plants edit `UiActionMap.Remap.cs`, so one baseline covered the three.

| Plant | Change | Exit | Failed tests |
|---|---|---|---|
| 9 | `CommitRebind` calls `FindClash` and throws on a clash | 2 | `AConflictKeepsBothAndNamesBothActions`, `AConflictAgainstADefaultBindingReports`, `TheConflictSlotTakesOneHandlerWrittenWithEquals` (Q8), plus `TheCancelKeyIsOneSettablePropertyWithARangeCheck` and `TheSnapshotComesFromTheFirstActivate` |
| 10 | `CaptureDefaults` runs on every `Activate` | 2 | `TheSnapshotComesFromTheFirstActivate` at its count assert (Expected 1, Actual 2) (A6), plus `ModeDiffTests.SetModeAndTheDiffAllocateNothingAfterWarmUp` and `ModeDiffAllocationTests.TheToggleCycleAllocatesNothingAfterWarmUp` |
| 11 | `ResetBindings` reassigns `_bindings = _defaults` in place of the copy | 2 | `ResetBindingsRestoresTheBuildDefaultsAfterARebind` at its second-restore read assert |

Plant 9 fails two more tests than the brief names, and plant 10 fails two. Each extra failure is the same rule from
another test. The plant-10 extras are the valuable ones: a mode switch calls `Activate` again, so an unguarded
`CaptureDefaults` puts one `InputBinding[]` per activation inside two shipped 0-byte gates.

### Decisions that the plan did not settle

- **Plant 11 shows an alias, not an allocation.** The brief expected a gate to move under the reassignment plant. It does
  not: `_bindings = _defaults` allocates nothing, so no 0-byte gate can see it. The real fault is the shared array,
  because the next `RemoveBindings` and `Append` write into the snapshot, and a second restore then gives the rebound
  bindings. `ResetBindingsRestoresTheBuildDefaultsAfterARebind` therefore holds a second rebind and restore after the
  first one, and its read assert is what plant 11 breaks. The `BindingCount` assert of the same test still holds under
  the plant, as the brief predicts, so the count alone cannot prove the rule. No phase 5 fallback was needed, and the
  0-byte proof of the restore path stays with the phase 5 gate.
- **The once rule needs a bind between two activations.** The brief's test 2 named one activation. A `CaptureDefaults`
  that runs on every `Activate` re-captures the *current* bindings, so the assert only moves when a bind lands after the
  first activation and before the second one. The test now binds `Fire` to A, calls `Activate` again, restores, and
  reads `BindingCount` 1 plus two frame reads: Space drives `Jump`, and A drives nothing.
- **A restore does not consume the snapshot.** A second `ResetBindings` gives the same bindings, because the copy leaves
  `_defaults`, `_defaultsCount`, and `_defaultsCaptured` in place. The scope test and the first test both read it.
- **The last capture of `ARestoreEndsTheListenWithNoCommit` takes a key that no other action holds.** The first draft
  rebound onto Space, which clashes with the build bind of `Jump`, so the conflict slot ran and the "no slot call" claim
  read as a failure. The restore reports nothing of its own, and the clash case sits in `RebindConflictTests`.
- **A throwing conflict handler costs that frame's device report.** `RebindScan` ends with the device store and
  `ReportSchemeChange`, and the slot call happens before the return, so the exception leaves both lines unrun. The
  listen is closed and the binding is in place, which is the exception rule of both delegates. Test 4 pins the state
  after the throw, and no binding or count moves.
- **The placement of the call in `Activate`.** The method calls `CaptureDefaults()` after `_context = context` and
  before the switch-rule `ResetFrame()`, inside the bounds that task 3.2 names. Neither slot write and no line of
  `ResetFrame` touches the snapshot fields, so the guard holds across a mode switch and a second activation.
- **`ReadBindings` stays plain text in the `ResetBindings` remarks.** The member ships in phase 4, and an unresolvable
  cref is a build error, so the rule reads `<c>ReadBindings</c>` now. Phase 4 switches it to a real cref, in the same
  way this group switched `<c>ResetBindings</c>`.
- **The clash rule of a default-scheme target covers both scan directions.** `AConflictAgainstADefaultBindingReports`
  rebinds `Fire` once into `UiScheme.Default` and once into the KeyboardMouse id, against one default bind of `Jump`.
  `SchemeClashes` reports in either order (A4), and the target's own group never counts as a clash.
- **The device rule of a two-scheme clash.** `TwoDifferentSchemesGiveNoConflict` moves the track to the Gamepad id with
  a live stick, then captures a key in another scheme tag, and `ActiveScheme` reads the KeyboardMouse id. The report
  follows the device of the input, not the tag of the binding, as the class remarks already say.
- **The restore scope is proven by the reads that survive it.** `ResetBindingsRestoresTheBindingsAndNothingElse` pins
  `ActionCount`, both type checks through a rejected bind (`ArgumentException`, parameter `action`), `InvokeAction` for
  an action with a command and one without, the four scheme ids with `SchemeName` and one out-of-range id,
  `GamepadSlot`, `LockedScheme` after `SetScheme`, `ActiveScheme` under the lock, both slot references under
  `Assert.Same`, and `RebindCancelKey`. It changes only the bindings, which is requirements line 15.

### Prose lint

Doc-comment prose only, extracted to `obj/plantscratch/p3doc-*.txt` and linted with
`python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5`:
`UiActionMap.Remap.cs` 0.83 hits per 100 words, `UiActionMap.cs` 1.22, `RebindConflictTests.cs` 0.67,
`RebindResetTests.cs` 0.92. All four are inside the bar, and the additions lowered none of them past it.

### Blast radius and migration risks

- Shared code touched: `Activate` (one call and one remark sentence), the class remarks of `UiActionMap.cs` (two new
  paragraphs), and `UiActionMap.Remap.cs`. The commit path, the scan, the fit table, the five silence guards,
  `ResetFrame`, and the whole scheme table keep the text that phases 1 and 2 shipped. `UiActionMap.Bindings.cs` keeps an
  empty diff, as do `src/BrutalUi.Core/`, `src/BrutalUi.Glfw/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Markup/`,
  `UiManager.cs`, `scripts/`, and every `.csproj`, `.props`, and `.sln`.
- The first `Activate` allocates one `InputBinding[]`, so the first activation is a build-time step. A second activation
  allocates nothing, and plant 10 proves that two shipped 0-byte gates see the difference. A game that activates a map
  before it finishes its binds loses those binds from a restore; section 9 of phase 5 states the rule.
- `ResetBindings` never reassigns `_bindings`, so a snapshot cannot alias the live array, and a restore inside a frame
  read leaves any array that a resolve already copied valid. Plant 11 is the guard for that rule.
- Group 2 adds the `Restore` button to the HelloPanel state row, which moves the smoke line 2 vertex and draw-command
  counts again, and puts a `ResetBindings` call inside a command handler. That call runs inside the frame read, so the
  restore lands at the next frame (the re-entry rule of both delegates). `PushSmokeInput` must keep opening no listen.
- Phase 4 reads the same array that the restore copies, so the `Code1` to `Code3 = -1` and `Scale` shape of a captured
  binding stays its contract with the writer. The carried cref of `ReadBindings` belongs to that group.

### Open questions

- None for a public API shape. `ResetBindings` matches the Public API line of the plan (`bool`, no argument), and no new
  top-level type appeared (A10).

---

## Phase 3 Group 2 Record

Built by the `game-dev-advanced` agent on 2026-11-10, tasks 3.6 and 3.7: the HelloPanel `Restore` button. One file
changed: `samples/HelloPanel/HelloState.cs` (the `hello.restore` command, the `s_restore` handler, the `RestoreButton`
property, one row child, and the restore paragraph of the class remarks). No `src/` file, no test file, and no other
sample file changed in this group. No framework defect appeared in the restore path. Phase 3 is complete (7/7).

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2979, failed 0, succeeded 2976, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 317, `BrutalUi.Core.Tests` 1801. The group adds no test, so the counts equal the
  group 1 end state. The skipped 3 are the display-only Glfw tests, as at the base.
- Class filters, each with `failed: 0`: `RebindListenTests` 10, `RebindCaptureTests` 8, `RebindConflictTests` 4,
  `RebindResetTests` 4.
- No plant belongs to this group. The two tasks name no headless rule, and the phase 3 acceptance list holds no plant
  line for the sample. Group 1 shipped and planted the restore that this group calls.

### Smoke line

`MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` passes preflight, build, test, gpu, and loader, then
stops at the smoke step: `test-linux: FAIL at smoke (exit 1)` and
`HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, `exit=1`. The step log
`artifacts/test-linux/smoke.log` holds that one line, and `artifacts/test-linux/test.log` reads total 2979 with
`failed: 0` and `skipped: 3`. The cause is the sandbox `/tmp` isolation of the X11 socket, as in every sibling
feature. Not a code fault. The live sequence and the moved line 2 counts are in Needs Human Verification below.

### Surface checks

- `git status --porcelain` lists no new file and no new `src/` edit: the four sample files of the feature, the three
  edited `src/BrutalUi.Framework/Actions/` files, the new `UiActionMap.Remap.cs`, the four rebind test classes, and the
  three feature docs. `UiActionMap.Bindings.cs`, `src/BrutalUi.Core/`, `src/BrutalUi.Glfw/`, `src/BrutalUi.ImGui/`,
  `src/BrutalUi.Markup/`, `UiManager.cs`, every `.csproj`, `.props`, `.sln`, and `scripts/` file keep an empty diff.
- `HelloState.cs`: one `commands.Register("hello.restore"u8, s_restore, this);` right after the `hello.rebind`
  register, one static `s_restore` handler that calls `state.Map.ResetBindings()`, one `RestoreButton` built next to
  `RebindButton`, `Row.Children.Add(RestoreButton);` between the rebind button and the score label, and the
  `RestoreButton` property.
- The row order is now `Add`, `Rebind (Up)`, `Restore`, the score label. The six existing labels, their texts, and
  `PushSmokeInput` did not change. `Program.cs` still clicks `ViewportCenter(state.AddButton)`, so the smoke click
  point follows the layout of the row each frame and no click lands on `Restore`.
- The class summary of `HelloState` now reads "three commands on the buttons of the row". The old line named one
  command on one button and went stale when phase 1 added the rebind button. Prose only.

### Decisions that the plan did not settle

- **The handler reads no return value.** `ResetBindings` gives `false` when the map holds no snapshot. The snapshot
  comes from the first `Activate`, and the mode diff mounts the panels in step 4 before it activates the map in step 5
  (`UiManager.cs:711-724`). A click needs a mounted row, so the snapshot exists before any click can reach
  `RestoreButton`. The handler ignores the value, the way `s_rebind` ignores the `false` of `BeginRebind` for an
  unknown action. The rule sits in the `s_restore` remarks.
- **The restore gives the whole build back, not only the `Up` key.** `HelloModes` binds `F1` and `F2` in its
  constructor (`HelloModes.cs:157-162`), and the first apply runs later, inside `UiContext.Update(float)`. So the
  snapshot holds every bind of the build, and a click on `Restore` also restores the mode keys. The demo names the
  `Up` key because the rebind touches that one.
- **A `Restore` click during a listen closes it with no report.** The shipped close rule clears the listen and calls
  no slot, so `RebindStatus` and `RebindCode` keep their last values. `RebindText` then drops the `press a key` line,
  because `IsRebinding` is `false`, and shows the last stored result. The sample adds no rule for that case.
- **The row block keeps one shape per button.** The `RestoreButton` line wraps after the `BindCommand` name, in the
  shape that phase 1 chose for `RebindButton`, so the three button lines read alike.

### Prose lint

Doc-comment prose of `HelloState.cs`, extracted with `sed` and linted with
`python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5`:
`samples/HelloPanel/obj/plantscratch/g22-HelloState.prose.txt` reads 0.60 hits per 100 words over 997 words. Inside
the 2.5 bar. The extraction drops the name inside each `<see>` and `<c>` tag, so its word count is not comparable with
the phase 1 file, which kept them.

### Blast radius and migration risks

- Shared code: none. The group touches one sample file, and it calls the shipped `bool ResetBindings()`.
- The call runs inside a command handler, which runs inside `UiContext.Update(float)`, so the restore takes effect at
  the next frame (the re-entry rule of the member remarks). The frame loop needs no extra call.
- The row grows by one focusable button, so the smoke line 2 vertex and draw-command counts move again. A person reads
  them from this tree, because phases 4 and 5 keep it.
- The Tab walk of the state row grows: `Add`, `Rebind (Up)`, `Restore`. `HelloScreen.cs` still names two buttons in
  three remarks. See the open question below.
- No allocation joins the frame path. The button and the handler exist from the constructor, and the static handler
  takes the state as its target.

### Open questions

- None for a public API shape. The group adds no public member and calls none that phase 3 group 1 did not ship.
- **Outside this brief:** `HelloScreen.cs` names the state row in three remarks and now misses the `Restore` button:
  the focusable list (`:33-34`), the Tab walk (`:36-37`), and the row contents (`:59-60`). The file is not in the
  `## Files` list of this brief, so nothing changed there. One short edit in each place closes the gap, and the next
  group that owns that file or a doc pass should take it.

---

## Phase 4 Group 1 Record

Built by the `game-dev` agent on 2026-11-10, tasks 4.1 to 4.5: the pinned `InputKind` values, the internal binding handle,
and `WriteBindings`. The group also carries the Inbox change of the Phase 3 Group 2 Record, three remarks of
`samples/HelloPanel/HelloScreen.cs` that missed the `Restore` button. Six files changed: `UiActionMap.cs` (the nine
explicit `InputKind` values and one enum remark), `UiActionMap.BindingsText.cs` (new, 342 lines: the file remark, the field
spans, `BindingText`, `TryReadBinding`, `WriteBindings`, and one line writer), `BindingTextWriterTests.cs` (new, 327
lines, four tests), `HelloScreen.cs` (three remarks, prose only, no code line), `tasks.md`, and `context.md`. No frame
path, no listen file, and no project file changed. `UiActionMap.Bindings.cs`, `UiActionMap.Remap.cs`, and
`UiActionMap.Read.cs` keep the diff that phase 1 to 3 left them with.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. The same read
  after each change and after each plant restore.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2983, failed 0, succeeded 2980, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 321 (317 + 4), `BrutalUi.Core.Tests` 1801. The skipped 3 are the display-only Glfw
  tests, as at the base.
- The end-state line of the brief reads 2987 and Framework 325. Those are the phase totals with `BindingsRoundTripTests`
  (4) from group 2. This group adds four tests, so the measured read is 2983 and 321. See the open questions.
- `--filter-class '*BindingTextWriterTests'`: total 4, failed 0. The four rebind classes and the gate pass with no edit,
  each at its count: `RebindListenTests` 10, `RebindCaptureTests` 8, `RebindConflictTests` 4, `RebindResetTests` 4, and
  `ActionMapAllocationTests` 7 at 0 bytes. The twelve existing classes hold their counts, as phases 1 to 3 pinned them.
- The four tests: `TheWriterStartsWithTheVersionLineAndWritesOneLinePerBinding`,
  `AShortSpanGivesTheNeededCountAndWritesNothing`, `TheWriterOmitsEachFieldAtItsDefault`, and
  `AllNineKindsWriteTheirDeviceNumbers`. All pass.
- Surface checks: `git diff d2e0dcf -- src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` gives no output, and
  `grep -c 'public void Bind'` reads 18. `grep -n` finds the nine explicit enum values, 0 to 8. `git diff d2e0dcf --stat`
  names no file of `src/BrutalUi.Core/`, `src/BrutalUi.Glfw/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Markup/`,
  `UiManager.cs`, `scripts/`, and no `.csproj`, `.props`, or `.sln`.

### The byte layout that the tests pin

The version line is `version=1\n`, ten bytes, and an empty map returns exactly it. Then one line per binding, in the
order of the binding array. Each line holds its fields in this order, separated by `;`, each as `name=value`, and ends
with `\n`:

```
action=<name>;scheme=<name or ->;device=<0..8>[;code=<n>][;code1=<n>][;code2=<n>][;code3=<n>][;mods=<n>][;scale=<n>]
```

- The three always-present fields are `action`, `scheme`, and `device`. A `code` to `code3` field appears when the value
  is not -1, `mods` when it is not 0, and `scale` when it is not 1.
- `scheme` holds the name bytes of the id, or `-` for id 0, because `SchemeName(0)` is empty (A3).
- `device` is the `InputKind` number: 0 a key, 1 a mouse button, 2 a gamepad button, 3 a key vector, 4 a gamepad stick,
  5 a gamepad axis, 6 a key axis, 7 a pointer delta, and 8 a scroll. The explicit enum values pin them.
- `mods` is the `UiModifiers` mask as a number, inside `MatchMask`: Shift 1, Control 2, Alt 4, Super 8.
- `scale` is the shortest round-trip invariant text, so `2f` writes `scale=2` and `-1f` writes `scale=-1`. The reader of
  group 2 parses the same text.
- A pinned example, one line per field rule:

```
version=1
action=Fire;scheme=-;device=0;code=32
action=Move;scheme=KeyboardMouse;device=3;code=87;code1=65;code2=83;code3=68
action=Zoom;scheme=Gamepad;device=8;scale=2
action=Fire;scheme=-;device=0;code=32;mods=2
action=Zoom;scheme=-;device=6;code=65;code1=68
action=Move;scheme=-;device=7
```

Each line above comes from one of the four tests, so the pinned text and the rules agree.

- Task 4.9 records the reader half of the layout. This section is the writer half, read from the code and the four tests.

### Plants

Each plant builds first, and each build reads `0 Warning(s)` and `0 Error(s)`. The full suite ran for each plant. The
baseline `p4-BindingsText.cs.orig` and the logs `p12run.log`, `p13build.log`, and `p13run2.log` sit in
`src/BrutalUi.Framework/obj/plantscratch/` (git-ignored, confirmed with `git check-ignore`). Plant 13 ran a second time
with the build output captured, because the first plant-13 build died with the known `Fatal error. Internal CLR error.
(0x80131506)` flake. The second build read `0 Warning(s)`, `0 Error(s)`, and no `error CS` line, so the plant result
counts. Each restore passed `cmp`. The final run reads build `0 Warning(s)` / `0 Error(s)`, solution 2983 with 0 failed
and 3 skipped, Framework 321, `BrutalUi.Core.Tests` 1801, and each named class at its count: the writer 4, the four
rebind classes 10, 8, 4, and 4, and `ActionMapAllocationTests` 7.

| Plant | Change | Exit | Failed tests |
|---|---|---|---|
| 12 | The writer omits the `device` field | 2 | `AllNineKindsWriteTheirDeviceNumbers`, `TheWriterOmitsEachFieldAtItsDefault`, plus `TheWriterStartsWithTheVersionLineAndWritesOneLinePerBinding` at its pinned line (the nine numbers) |
| 13 | The measure pass writes `mods` when the value is 0 | 2 | `TheWriterOmitsEachFieldAtItsDefault`, plus `TheWriterStartsWithTheVersionLineAndWritesOneLinePerBinding` and `AllNineKindsWriteTheirDeviceNumbers` (the needed count no longer matches the written count) |

Plant 12 and 13 each fail two more tests than the brief names. Each extra failure is the same rule from another test: the
pinned line text and the two-count agreement, so the plant stays sharp.

### Decisions that the plan did not settle

- **The field spans carry their equals sign.** The brief names eight field-name spans and then lists nine. The file ships
  those nine (`ActionField` to `ScaleField`), each as `name=`, plus `FieldSeparator` and `LineFeed`. One span per name
  keeps the writer to a copy of bytes, so both passes count the same bytes.
- **One stack scratch serves both passes.** `EmitNumber` and `EmitScale` format into a 32-byte `stackalloc` buffer and
  hand that buffer to the same `Emit` call that the field names use. The measure pass adds the length, and the write pass
  copies it. `MaxNumberBytes` covers the eleven bytes of a signed `int` and the shortest text of a finite `float`, so the
  `TryFormat` result cannot be false.
- **`TryReadBinding` is internal, and no test can call it.** `BrutalUi.Framework` holds no `InternalsVisibleTo` item, so
  the Framework test assembly cannot name the type or the member (A10). Task 4.4 asks for three asserts on it inside
  `AllNineKindsWriteTheirDeviceNumbers`. The group keeps the member internal and asserts the same nine numbers from the
  text that the public writer gives. `WriteBindings` calls `TryReadBinding` for each line, so the member is covered by
  all four tests. See the open questions.
- **The writer throws nothing.** Each id and each index comes from the arrays that the map maintains, so `SchemeName`
  cannot go out of range and no bind can leave a code that the writer must reject. A short span is not an error either: it
  returns the needed count.
- **`BindingsVersion` is a const, and `VersionLine` is the literal text.** The reader of group 2 compares the parsed
  number with the const. The writer keeps the literal span, so the version line costs one copy.
- **The HelloPanel remarks are prose only.** The three places now name `Add`, `Rebind (Up)`, and `Restore` in the row
  order of `HelloState.cs`, and the Tab walk ends at `Restore`. No code line of the sample changed, and no other sample
  file changed.

### Prose lint

Doc-comment prose only, extracted to `obj/plantscratch/p41doc-*.txt` and linted with
`python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5`:
`UiActionMap.BindingsText.cs` 1.09 hits per 100 words over 918 words, and `BindingTextWriterTests.cs` 1.08 over 649. Both
are inside the bar. On whole files the linter reads code lines as sentences, and the group reads 4.88 and 11.60, in line
with the shipped base files. `UiActionMap.cs` reads 2.47 whole file after the enum remark, and `HelloScreen.cs` reads
3.89.

### Blast radius and migration risks

- Shared code touched: none. The group adds one new partial file and four tests, and edits only the enum body and the
  sample remarks. `ReadFrame`, the listen, the capture scan, the commit, the two slots, the five silence guards, the 18
  binds, `Append`, `FindClash`, and the whole scheme table keep their text.
- `WriteBindings` reads `_actions[b.Action].Name`, `SchemeName(id)`, and the binding array through `TryReadBinding`. It
  writes no map state, so a call costs no frame change, and it allocates nothing: every number goes to a stack buffer.
- The pinned enum values are a stored contract. A later insert must append a number, never move one, or every saved file
  reads the wrong device. `device` numbers 0 to 8 are the whole table.
- Group 2 reads the same bytes back, so the layout above is its contract too. The `Code1` to `Code3 = -1` and `Scale = 1f`
  shape of a captured binding (phase 2) is what the writer omits, so a rebound binding writes a short line.
- The seventeenth gate does not measure the writer: the writer is a load-time member that owns no frame path. Phase 5
  still grows that gate with the rebind step only.

### Open questions

- **The test reach of `TryReadBinding`.** Task 4.4 names three asserts on the internal member, and A10 keeps it internal
  with no `InternalsVisibleTo`. Both cannot hold. A person chooses: keep the member untested directly (this group), add an
  `InternalsVisibleTo` item to `BrutalUi.Framework.csproj` (a project-file change that A2 forbids today), or drop those
  three asserts from the task text. No public API shape is in question.
- **The end-state counts of the brief.** The brief's end state reads solution 2987 and Framework 325, which are the totals
  after group 2. Group 1 measures 2983 and 321, and its four tests pass. No action needed if the phase total lands in
  group 2.

---

## Phase 4 Group 2 Record

Built by the `game-dev` agent on 2026-11-10, tasks 4.6 to 4.9: `ReadBindings`, its private line parser, and
`BindingsRoundTripTests` (4). The group also carries the one item that phase 3 left: the `ResetBindings` remarks now
name `<see cref="ReadBindings"/>` instead of the plain text. Three files changed:
`src/BrutalUi.Framework/Actions/UiActionMap.BindingsText.cs` (342 to 845 lines: the `VersionField` span, one file
remark line, `GamepadButtonBitLimit`, `ReadBindings`, and the private `TryReadVersion`, `TryReadLine`, `FitsKind`,
`TryParseSignedInt`, `FirstOfPair`, and `ToBinding`), `tests/BrutalUi.Framework.Tests/Actions/BindingsRoundTripTests.cs`
(new, 4 tests), and `UiActionMap.Remap.cs` (one doc paragraph inside `ResetBindings`, prose only, no code line). No
frame path, no listen code, no bind, no sample, and no project file changed.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. The same read
  after each change, after each plant, and after each plant restore.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2987, failed 0, succeeded 2984, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 325 (321 + 4), `BrutalUi.Core.Tests` 1801. The skipped 3 are the display-only Glfw
  tests, as at the base. The brief's end state reads 2987 and Framework 325: this group lands both.
- The first build and the first suite run each stopped with `Fatal error. Internal CLR error. (0x80131506)`, the known
  flake. The second run of each passed, so no fault is in the code.
- `--filter-class '*BindingTextWriterTests'`: total 4, failed 0. `--filter-class '*BindingsRoundTripTests'`: total 4,
  failed 0. The four rebind classes and the gate pass with no edit, each at its count: `RebindListenTests` 10,
  `RebindCaptureTests` 8, `RebindConflictTests` 4, `RebindResetTests` 4, and `ActionMapAllocationTests` 7 at 0 bytes.
  The twelve existing classes hold the counts that phases 1 to 3 pinned.
- The four tests: `AWrittenFileReadsBackIntoEveryKind`, `ReadBindingsMergesTheNamedGroupsAndKeepsTheRest`,
  `ABadLineIsSkippedAndCounted`, and `AnUnknownVersionAppliesNothing`. All pass.
- Surface checks: `git diff d2e0dcf -- src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` gives no output, and
  `grep -c 'public void Bind'` reads 18. `git diff --stat d2e0dcf` names no file of `src/BrutalUi.Core/`,
  `src/BrutalUi.Glfw/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Markup/`, `UiManager.cs`, or `scripts/`, and no `.csproj`,
  `.props`, or `.sln`. `grep -n ReadBindings src/BrutalUi.Framework/Actions/UiActionMap.Remap.cs` reads one line: the
  cref of the `ResetBindings` remarks. `git check-ignore` confirms the plant scratch is ignored.

### The byte layout that the tests pin

Group 1 wrote the writer half above. This section is the reader half, task 4.9, so section 9 of phase 5 states the
format from the code and not from the plan.

The reader accepts the text that the writer gives, byte for byte. The grammar of a text:

```
version=1\n
action=<name>;scheme=<name or ->;device=<0..8>[;code=<n>][;code1=<n>][;code2=<n>][;code3=<n>][;mods=<n>][;scale=<n>]\n
```

- One line feed ends one line. A carriage return before a line feed is not part of a value, so the reader accepts a text
  that a Windows editor saved. A line with no bytes gives no binding and no count. A last line with no line feed is a
  line.
- The first non-empty line must be `version=1`. Each other case applies nothing: `version=2` gives `Applied` 0 and
  `Version` 2; a missing version line, an unreadable number, and a `version=1` line that is not the first non-empty line
  each give `Version` 0. A later `version=` line is one unknown field name, so the skip rule counts it.
- One semicolon separates two fields, and one name and one value make each field, with one equals sign between them. The
  name must be one of the nine: `action`, `scheme`, `device`, `code`, `code1`, `code2`, `code3`, `mods`, `scale`. A name
  that is not one of the nine is a bad line, and a name that appears twice in one line is a bad line. The field order is
  free to a hand edit: the reader looks each name up.
- The three required fields are `action`, `scheme`, and `device`. A line that misses one of them is a bad line.
- The optional fields read at their writer default: a `code` to `code3` that the line omits reads as -1, `mods` reads as
  0, and `scale` reads as 1.
- `action` holds the name bytes of one action of the map. `IndexOfAction` gives the index, and an unknown name is a bad
  line.
- `scheme` holds the name bytes of one scheme, or `-` for id 0 (`UiScheme.Default`, because `SchemeName(0)` is empty).
  `IndexOfScheme` gives the id, and an unknown name is a bad line.
- `device` holds the `InputKind` number, and only 0 to 8 are good: 0 a key, 1 a mouse button, 2 a gamepad button, 3 a
  key vector, 4 a gamepad stick, 5 a gamepad axis, 6 a key axis, 7 a pointer delta, and 8 a scroll.
- Each number reads through `TryParseSignedInt`: an optional sign, then one or more digits, no culture, no allocation,
  and no value outside the `int` range. `mods` must be inside `MatchMask` (Shift 1, Control 2, Alt 4, Super 8). `scale`
  reads through `float.TryParse` with `NumberStyles.Float` and `CultureInfo.InvariantCulture`, and a value that is not
  finite is a bad line, so `NaN` and `Infinity` never land.
- The range rule of each code equals the check that the matching bind runs (review CR-6 of input-actions):

| `device` | Codes that the kind reads | Check |
|---|---|---|
| 0 a key | `code` | below `UiKeyState.KeyCount` |
| 1 a mouse button | `code` | below `UiPointerButtonState.ButtonCount` |
| 2 a gamepad button | `code` | exactly one bit, inside 15 bits |
| 3 a key vector | `code` to `code3` | each below `UiKeyState.KeyCount`, and the four values distinct |
| 4 a gamepad stick | `code` | 0 or 1 |
| 5 a gamepad axis | `code` | below `UiGamepadState.AxisCount` (0 to 5) |
| 6 a key axis | `code` and `code1` | each below `UiKeyState.KeyCount`, and the two values distinct |
| 7 a pointer delta | none | no check |
| 8 a scroll | none | no check |

- The shape rule: the reader sets a code slot that the kind does not read to -1, so one line reads back the nine fields
  that the matching bind writes, and the writer omits those slots again.
- The merge rule (Q6): the reader first holds every good line in a private growable `BindingText[]`, then removes each
  named `(action, scheme)` pair once, then appends each good line in the order of the text. A pair that no line names
  keeps its bindings, and two lines of one pair give two bindings. A re-read of the same text is idempotent.
- The skip rule (A8): one bad line is counted in `Skipped` and never thrown, so a game hands the method a file that a
  person edited. The reader counts a bad line and keeps the good lines of the same text.
- The round trip that `AWrittenFileReadsBackIntoEveryKind` pins: a map with the nine shapes, one modifier bind, and one
  scale of -1 writes its text; a second map with the same action names, the same action types, and the same scheme names
  reads all of it with `Applied` 10, `Skipped` 0, and `Version` 1; and the second map writes the same bytes again, so
  `text.SequenceEqual(again)` holds. No public member reads one binding (A10), so the test proves each shape in a driven
  frame through `WasPressed`, `ReadVector`, `ReadAxis`, `PushGamepad`, and the command that the frame read invoke pass
  runs.
- The `ReadBindings` half of A6: a text read does not move the defaults snapshot, so `ResetBindings` after a read gives
  the bindings of the first `Activate`. `ReadBindingsMergesTheNamedGroupsAndKeepsTheRest` asserts it.

### Plants

Each plant builds first, and each build reads `0 Warning(s)` and `0 Error(s)`. The full suite ran for each plant. The
baseline `g2-BindingsText.cs.orig` and the logs `p14g2run.log`, `p15g2run.log`, and `p16g2run.log` sit in
`src/BrutalUi.Framework/obj/plantscratch/` (git-ignored). Each restore passed `cmp`, and the suite read 2987 with 0
failed after all three.

| Plant | Change | Exit | Failed tests |
|---|---|---|---|
| 14 | The key case of `FitsKind` drops its range check | 2 | `ABadLineIsSkippedAndCounted`: `Applied` reads 4, not 3, because the `code=99999` line lands a dead binding |
| 15 | The reader clears the whole binding list instead of replacing the named pairs | 2 | `ReadBindingsMergesTheNamedGroupsAndKeepsTheRest` (Q6), plus `AnUnknownVersionAppliesNothing` (a `version=1` text with no binding line now wipes the list, so the same rule reads from another test) |
| 16 | `TryReadVersion` accepts any readable number | 2 | `AnUnknownVersionAppliesNothing` (A8) |

### Decisions that the plan did not settle

- **The reader needs no new `RemoveBindings` limit.** Task 4.6 names `RemoveBindings(actionIndex, scheme, limit)`. The
  shipped method takes two arguments (`UiActionMap.Remap.cs:799`), and the reader calls it for every named pair before
  it appends any line. The binding count at the removal is therefore the count at the entry, and the appended region
  starts after it, so no line that this call applied can be removed. The group reused the shipped signature and changed
  no code line of `UiActionMap.Remap.cs`.
- **The three always-present fields are required.** The plan names one skip case: "a missing `action`". The reader also
  skips a line with no `scheme=` or no `device=`: the writer always emits those three fields, and a line with no kind
  cannot bind anything.
- **A code slot that the kind does not read lands as -1.** A hand edit that puts `code1=65` on a key line reads as a key
  binding with no second code, the same shape as `BindKey` writes. The read does not count that line as bad, because the
  number is in range and the writer omits the slot again, so the round trip still holds.
- **`mods` and `scale` read for every kind.** The frame read compares modifiers for a key and a mouse button, and
  multiplies the value of an axis, a delta, and a scroll. A `mods` or `scale` field on another kind is inert, the same
  as on a bind that stores it.
- **A composite does not re-run the distinct-key rule.** `BindKeyVector` rejects two keys with one code, and
  `BindKeyAxis` rejects a pair of one code. The reader runs the range checks that the plan names and compares no code
  with another, because the plan's skip list does not name a duplicate. See the open questions.
- **The reader accepts a carriage return before a line feed.** The writer emits no carriage return, so the round trip is
  unchanged, and an editor on Windows still gives a readable file.
- **`ResetBindings` remarks: one doc paragraph.** The group switched `<c>ReadBindings</c>` to
  `<see cref="ReadBindings"/>` and dropped the clause "<c>ReadBindings</c> lands in a later phase of this feature",
  which the shipped member makes false. No other line of that file changed.
- **A reader call is a load-time call.** The method grows one local array, can grow the binding array through `Append`,
  and reads each `scale` through `Encoding.UTF8.GetString`, the precedent of `src/BrutalUi.Markup/Style/StyleValues.cs:480`.
  The remarks of `ReadBindings` say so, and no frame path calls the method.

### Prose lint

Doc-comment prose only, extracted to `obj/plantscratch/g2doc-*.txt` and linted with
`python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5`:
`UiActionMap.BindingsText.cs` 0.85 hits per 100 words over 1775 words, and `BindingsRoundTripTests.cs` 0.82 over 979.
Both are inside the bar.

### Blast radius and migration risks

- Shared code touched: one doc paragraph of `UiActionMap.Remap.cs`. `ReadFrame`, the listen branch, the capture scan, the
  fit test, the commit, the conflict scan, the two slots, the five silence guards, the 18 binds, `Append`, `FindClash`,
  `RemoveBindings`, `CaptureDefaults`, `ResetBindings`, and the whole scheme table keep their code text.
- No public API shape changed beyond the planned `ReadBindings`: the return is one `ValueTuple` (Decision 8), so no new
  public type appears, and `InputKind` and `BindingText` stay private and internal.
- `device` numbers 0 to 8 remain a stored contract. A later kind must append a number, never move one, or every saved
  file reads the wrong device.
- A game that reads a text into a map that already holds bindings loses the bindings of each pair that the text names,
  and keeps every other binding. A text from a future version applies nothing and reports its number, so a game can tell
  the player why nothing changed.
- The seventeenth gate does not measure the reader: the reader is a load-time member that owns no frame path. Phase 5
  still grows that gate with the rebind step only.

### Open questions

- **The distinct-key rule of a composite.** A hand edit can read `device=3` with two equal codes, or `device=6` with one
  code twice, and the frame read then sums one key twice. `BindKeyVector` and `BindKeyAxis` reject that shape at build
  time. A person chooses: add the compare to `FitsKind`, or leave the reader as the plan states it. No public API shape
  is in question.
- **A line with no `scheme=` or no `device=`.** The plan names only a missing `action`, and the group rejects all three
  always-present fields. A person may prefer a missing `scheme` to read as id 0.
- **The test reach of `TryReadBinding`.** Carried from group 1, unchanged: the member is internal, and the parent ruled
  that no `InternalsVisibleTo` item lands. The bounds rule stays covered through the writer path.
- **The end-state counts of the brief.** Group 1 opened this item, and group 2 closes it: the measured read is 2987 and
  Framework 325, exactly the brief's end state.

### Fix round 1

The parent ruled open question 1: the gap is real, and the Quality Bar of the plan forbids it — "No dead binding can
land, because each value passes the same range checks that the binds run." The reader now runs the distinct-key rule of
the composite kinds. `FitsKind` gained the exact checks that the two composite binds run. The `KeyVector` case now needs
the four codes pairwise distinct, and the `KeyAxis` case needs the two codes different. The change lives inside the
switch of `FitsKind` in `src/BrutalUi.Framework/Actions/UiActionMap.BindingsText.cs` (848 to 860 lines). It adds no
allocation, changes no public member shape, and touches no line of `UiActionMap.Bindings.cs`: the diff of that file
against `d2e0dcf` gives no output. This section replaces the group decision "A composite does not re-run the
distinct-key rule".

The parent ruled open question 2: the reader keeps requiring `scheme=` and `device=` beside `action=`.
`requirements.md:16` names those three fields as the line, and the writer always emits them, so a text that omits one
is not a text this format defines. A missing one is a bad line and skips. The `ReadBindings` remarks now state the rule,
and the distinct-key paragraph above them states the composite rule, so both rules live in the code too.

Test: `ABadLineIsSkippedAndCounted` gained the two new bad lines inside `BadText`: a `KeyVector` line that repeats one
code (`device=3;code=87;code1=87;code2=83;code3=68`) and a `KeyAxis` line with two equal keys
(`device=6;code=65;code1=65`). Each raises `Skipped` by one: the read now gives `Applied` 3 and `Skipped` 14, and the
surviving binds still drive their actions in the driven frame. Every assert stays on the public API: `ReadBindings`,
`WriteBindings`, `BindingCount`, and the five queries.

Measured counts, all in the final state:

- Build: `0 Warning(s)`, `0 Error(s)`, the same read after each change.
- `dotnet test --solution brutal-ui.sln`: total 2987, failed 0, succeeded 2984, skipped 3. `BrutalUi.Framework.Tests`
  325, `BrutalUi.Core.Tests` 1801. The counts do not move: the fix adds two bad lines inside one existing test.
- `--filter-class '*BindingsRoundTripTests'`: total 4, failed 0. `--filter-class '*BindingTextWriterTests'`: total 4,
  failed 0. The four rebind classes pass with no edit: `RebindListenTests` 10, `RebindCaptureTests` 8,
  `RebindConflictTests` 4, `RebindResetTests` 4, each with `failed: 0`.
- Prose lint: the doc-comment lines of `UiActionMap.BindingsText.cs` read 0.74 hits per 100 words over 2421 words,
  inside the 2.5 bar. The extract sits in `src/BrutalUi.Framework/obj/plantscratch/fix1-doc-Reader.txt` (git-ignored).
- `tasks.md`: no new tick. Tasks 4.6 to 4.9 already hold, and no Phase 4 acceptance box names the bad-line rule, so no
  box needed the new case.
- No plant ran in this round, as the brief rules. The test pins the rule from both sides: the read must give `Applied`
  3 and `Skipped` 14, so a dropped distinct compare lets one repeated-code line land, moves `Skipped` to 13, and fails
  the assert.

---

## Phase 5 Group 1 Record

Built by the `game-dev` agent on 2026-11-10, tasks 5.1 and 5.2: the rebind step of the seventeenth gate,
`ARebindAndAResetAllocateNothing`, and plants 17 and 18. One test file changed:
`tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs` (763 to 879 lines). No `src/` file and no sample
file changed in this group: `git status --porcelain src/ samples/` lists the same nine files that phase 4 left.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. The same read
  after task 5.1, after each plant, and after each plant restore.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2988, failed 0, succeeded 2985, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 326 (325 + 1), `BrutalUi.Core.Tests` 1801. The skipped 3 are the display-only Glfw
  tests, as at the base. The solution gains one test, the new gate.
- `--filter-class '*ActionMapAllocationTests'`: total 8, failed 0. `ARebindAndAResetAllocateNothing` measures 0 bytes
  over 2 warm-up rounds and 100 rounds.
- The six rebind and text classes pass with no edit, each at its count: `RebindListenTests` 10,
  `RebindCaptureTests` 8, `RebindConflictTests` 4, `RebindResetTests` 4, `BindingTextWriterTests` 4, and
  `BindingsRoundTripTests` 4, each with `failed: 0`.
- Neither known flake appeared in this group's runs. Each command ran once and its own output is the record.

### The gate that this group grew

- `RebindStep` drives a live listen on the `Kick` action in the KeyboardMouse scheme. Frame 1: `ClearFocus`, the idle
  pad, `BeginRebind`, and the settle `Update`. Frame 2: the F1 down event and `Update` (the capture, the conflict scan,
  and the commit; the F1 command of `act-03` runs no time). Frame 3: the F1 release, the rest pad, `Update` (a normal
  read on the new binding), `ResetBindings`, and one line that adds `map.BindingCount + (map.IsRebinding ? 1 : 0)` to
  `RebindSum`. `Draw` closes the step.
- The scene holds the two `KeyEvent`s for the existing `F1Key` const (290) and the `PadRest` idle pad as fields. The
  map writes `OnRebindResult` and `OnRebindConflict` after `OnSchemeChanged`, over `s_rebindHandler` and
  `s_rebindConflictHandler`, two static fields in the pattern of `s_schemeHandler`, over `s_rebindResults` and
  `s_rebindConflicts`.
- `BuildScene` asserts the binding array holds exactly 32 bindings at capacity 32. `Append` doubles from 8, so 32
  appends land at capacity 32 exactly: a commit that removes one binding and appends one cannot grow the array (A1).
- `TheMeasuredBodyDoesRealWork` ends with the rebind step and a fixed increment for it: `s_rebindResults` +1,
  `s_rebindConflicts` +1, `RebindSum` +32, `counters[3].Calls` unchanged (the F1 command), and `BindingCount` unchanged.

### Plants

Each plant builds first, and each build reads `0 Warning(s)` and `0 Error(s)`. The full suite ran for each plant. The
baseline `p5-UiActionMap.Remap.cs.orig` and the logs `p17build.log`, `p17run.log`, `p18build.log`, and `p18run.log` sit
in `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored, confirmed with `git check-ignore`). Each restore passed
`cmp`, and the clean suite above ran after the plant 18 restore.

| Plant | Change | Exit | Failed tests | Bytes |
|---|---|---|---|---|
| 17 | `GC.KeepAlive(new object());` as the last line of `RebindScan`, after `ReportSchemeChange()` | 2 | `ARebindAndAResetAllocateNothing` only | 4800 |
| 18 | `GC.KeepAlive(new object());` inside `CommitRebind`, after `Append` | 2 | `ARebindAndAResetAllocateNothing` only | 2400 |

Neither plant moved any other gate. `RebindScan` and `CommitRebind` run only inside a listen, and only the rebind step
opens a listen, so the 0-byte guard of the listen path belongs to the new gate alone. The byte counts match the x64
rule of 24 bytes per object over 100 rounds: plant 17 allocates in the settle frame and the capture frame, so 4800;
plant 18 allocates in the one commit, so 2400. Plant 18 is the failure that A1 names: a commit that allocates.

### Decisions that the plan did not settle

- **The capacity assert reads a private field through reflection.** The brief names the capacity-32 assert inside
  `BuildScene`. `InputBinding[] _bindings` is private, and the test assembly holds no `InternalsVisibleTo` (A10), so
  the assert reads the field with `typeof(UiActionMap).GetField("_bindings", ...)`, in the habit of the Glfw tests
  (`GlfwUiInputTests.cs:426`). The read runs at scene build time, and the measured bodies run none. No public API shape
  is in question.
- **The new gate also asserts the two slot counters.** The scheme gate ends with `Assert.True(s_schemeCalls > 0)`. The
  rebind gate ends with the same shape over `s_rebindResults` and `s_rebindConflicts`, so an empty body cannot pass.
- **`RebindSum` counts bindings and the listen flag.** The step line reads
  `map.BindingCount + (map.IsRebinding ? 1 : 0)`. One step adds exactly 32: the commit keeps 32 (one removed, one
  appended at capacity 32), and the restore writes back the 32 build bindings. An open listen at the sum line would add
  1, and a lost or grown binding would move the number, so the +32 assert pins the group rule, the free-room rule, and
  the restore in one number.

### Open questions

- None for a public API shape. The shipped listen path measured 0 bytes over 100 rounds, so this group changed no
  `src/` file and handed no real allocation back.
- The prose lint pass and the section 9, section 14, and changelog edits belong to tasks 5.3 to 5.7 (group 2).

---

## Phase 5 Group 2 Record

Built by the `game-dev` agent on 2026-11-10, tasks 5.3 to 5.7: the docs group that closes the feature and the `input`
epic. Four files changed: `docs/technical-design.md` (section 9 items 15 and 16, the `### Action map` bullets, and the
section 14 bullet **Remap tests.**), `CHANGELOG.md` (one new line under `### Added`), `context.md`, and
`implementation.md` (prose only, in the non-task sections). No file of `src/`, `tests/`, `samples/`, or `scripts/`
changed in this group: `git status` lists the same code files that phase 5 group 1 left.

### Measured counts

- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`.
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: total 2988, failed 0, succeeded 2985, skipped 3,
  exit 0. `BrutalUi.Framework.Tests` 326, `BrutalUi.Core.Tests` 1801. The skipped 3 are the display-only Glfw tests.
- Class filters, each with `failed: 0`: `RebindListenTests` 10, `RebindCaptureTests` 8, `RebindConflictTests` 4,
  `RebindResetTests` 4, `BindingTextWriterTests` 4, `BindingsRoundTripTests` 4, and `ActionMapAllocationTests` 8.
- The doc edits change no test, so these reads equal the phase 5 group 1 end state. Each command ran once, and neither
  known flake appeared.

### Prose lint, final scores

- `docs/technical-design.md`: 613 hits over 40037 words, 1.53 per 100 words. Pass.
- `CHANGELOG.md`: 36 hits over 2378 words, 1.51. Pass.
- `docs/features/input/binding-remap/context.md`: 250 hits over 13667 words, 1.83. Pass.
- `docs/features/input/binding-remap/implementation.md`: 450 hits over 18130 words, 2.48. Pass. The file opened the
  group at 3.25, and the prose pass cut 134 hits.
- `python3 .../ste-lint.py --fail-over 2.5` on all four files in one run exits 0.

### What the group wrote

- Section 9 `### Flow` gains item 15 **The binding remap** and item 16 **The binding text format**, so the run reaches
  16. Item 15 holds the listen, the raw-view rule, the settle frame, the cancel key, one candidate per frame, the live
  test of the seven shapes, the fit table, the reject, the commit, the silence, the conflict report, the restore, the
  switch rule, the two slots, and the cost. Item 16 holds the lines, the three omitted defaults, the `-` token, the nine
  `device` numbers as a table, the four `mods` bits, the scale, `WriteBindings`, `ReadBindings`, the merge, the skip,
  the range, the line rule, and the cost. Items 1 to 14 keep their sentences: `git diff` shows three hunks, and the two
  removed lines are the build-rule bullet and the cost bullet that tasks 5.4 edits.
- Section 9 `### Action map` gains **The remap rules.** and **The binding text.**, the rebind-commit sentences inside
  **The build rules.**, and the grown 0-byte list inside **The cost.** with the load-time statement for the text pair.
- Section 14 gains **Remap tests.** between **Scheme tests.** and **Sample projects.** It names the six classes, the
  grown seventeenth gate, the four new rules that a plant guards, and the counts.
- `CHANGELOG.md` gains one line under `### Added`, tagged `(input/binding-remap)`.

### One line beyond the item that the requirement names

`requirements.md:19` names the tests, HelloPanel, and section 9. It names no section 14 bullet, and no Action map
bullet. This group writes them anyway, in the habit of the sibling features: control-schemes recorded the same reading
("A6 names one item in section 9 and one changelog line, so this bullet sits beyond the letter of A6").

### The four new rules that a plant guards

Section 14 names these four, one per risk area of the feature: the raw-view read of the scan (plant 1), the suppressed
frame with its four passes skipped (plant 2), the commit that keeps both instead of throwing (plant 9), and the 0-byte
cost of the scan and the commit (plants 17 and 18). Eighteen plants ran across the five phases, so the count of the
feature is eighteen.

### Decisions that this plan did not settle

- **`RebindDeltaThreshold = 8f`.** A5 names a pointer-delta threshold and gives no size. The build settled on 8
  device-independent pixels as one private constant, because a raw delta is unfiltered screen movement, so a zero test
  is too weak. `APointerDeltaAboveTheThresholdCommitsADeltaBinding` pins both sides: a move of (40, -30) commits, and a
  move of (2, 0) gives no candidate. Item 15 states the number, the squared-length compare, and the non-finite guard.
  A person who wants another number changes one line and one test.
- **The once-per-frame reject.** The plan says the map reports the reject again while the candidate stays live. The
  shipped scan reports `RebindRejected` in every frame that a rejected candidate is live, so a game that shows the
  reason needs no dedup of its own. Item 15 states this rule, and `AMismatchedCaptureStaysInTheListenAndReportsTheReason`
  pins it.
- **The order change of a committed binding.** The commit removes the group, then appends the new binding at the end of
  the array. A clashing input then resolves to the earlier binding on the press path, while the frame read sums both.
  Item 15 and the **The build rules.** sentence state this with the per-scheme one-input rule, so a game reads the rule
  and the exception together.

### Rules stated from the code, and the differences

- The brief directs the format from the code and the phase 4 records, not from the plan draft. Item 16 states: the nine
  `device` numbers 0 to 8, the field order, the three omitted defaults (`-1`, `0`, `1`), the `-` token for
  `UiScheme.Default`, and that `ReadBindings` requires `action=`, `scheme=`, and `device=` on every line and skips a
  composite line that repeats one code.
- Item 15 states the four readings that the build settled: the lowest set bit of `pad.Pressed` for a gamepad button
  capture, the `GamepadStick` shape before `GamepadAxis` (so a `GamepadAxis` capture of codes 2 and 3 is unreachable
  through the listen), the device of a listen frame as the device of the live candidate else the last report carries,
  and the new binding at the end of the array.
- Item 15 also states the two rules that a plant proved: the restore copies into the existing array, because a
  reassignment would alias the snapshot (plant 11), and `WriteBindings` is the public read of one binding, because no
  public per-binding handle ships (A10).
- **Difference 1, in `src/` and outside this brief:** the class remarks of `UiActionMap.cs` still read "The binding
  text rule: a later phase of this feature adds `WriteBindings` and `ReadBindings`". Both members ship in phase 4.
  Section 9 states the shipped rule, and one short src edit fixes that phrase. No src file is in this brief.
- **Difference 2, in section 14 and left as history:** the older bullets keep their phase-time numbers. **A seventeenth
  allocation gate.** reads `(7 tests)`, and it reads "The scene then holds 23 actions and 28 bindings". The shipped
  gate holds 8 tests over a scene of 26 actions and 32 bindings. Task 5.5 names one added bullet, and the sibling
  features left those same historical counts in place, so the new **Remap tests.** bullet carries the shipped numbers and
  the old bullets stay.
- The shipped `BeginRebind` gives `false` for an unknown action name and throws `ArgumentOutOfRangeException` with the
  parameter name `scheme` for a bad id. Item 15 states both, and no doc contradicts them.

### Epic close

binding-remap is feature 5 of 5 of the `input` epic, and this group closes the epic: input-state, gamepad-axes,
input-actions, control-schemes, and binding-remap are now complete in this tree. `docs/features/input/epic-overview.md`
and the master rollup pick the row up in `/update-epic` and `/update-master`. This run makes no commit, no branch, and
no push, on the branch `feat/input/binding-remap` at base `d2e0dcf`.

### Open questions

- None for a public API shape. Section 9, section 14, and the changelog state only shipped members.
- A later src pass should fix the stale "a later phase" phrase in the `UiActionMap.cs` class remarks.
- A later doc pass may restate the historical gate counts in the two older section 14 bullets. This group left them.
- The display-host checks in Needs Human Verification stay open, as in every sibling feature.

### Fix round 1

Ran on 2026-11-10 by the `game-dev` fix agent, from the brief `phase-5-fix1.md`. Three stale doc-comment paragraphs
named work that this feature ships. The round rewrote all three. It changed no code line, no test, and no other
file. It makes no commit, no branch, and no push.

**Edited lines.**

- `src/BrutalUi.Framework/Actions/UiActionMap.cs:142-144`, the binding text paragraph of the class remarks. The old
  text read "a later phase of this feature adds `WriteBindings` and `ReadBindings`". Both members ship in
  `UiActionMap.BindingsText.cs`. The new paragraph states the shipped rule with two real crefs: `WriteBindings`
  writes the version line and one line per binding, and gives the byte count; a short span gives the needed count
  and writes no byte; `ReadBindings` parses the lines, merges each named action and scheme pair, and skips a bad
  line with a count; both run at load time and may allocate, and the game owns the file. Section 9 item 16 states
  this rule in the same words, so the round followed section 9 for the phrase "may allocate". The member remarks
  add that `WriteBindings` allocates nothing; the phrase is the load-time permission, not a promise of allocation,
  so the comment agrees with the code too.
- `tests/BrutalUi.Framework.Tests/Actions/RebindListenTests.cs:16-17`. The old text read "A later phase widens the
  scan to the seven capture shapes". Phase 2 shipped that widening. The new text reads: each test here rebinds a
  key, because this class tests the listen, and a real cref to `RebindCaptureTests` names the class that tests the
  capture of the seven input shapes.
- `tests/BrutalUi.Framework.Tests/Actions/RebindResetTests.cs:25-27`. The old text read "The `ReadBindings` half of
  A6 lands in a later phase, where that member exists". The member ships, and `BindingsRoundTripTests` pins the
  A6 half in `ReadBindingsMergesTheNamedGroupsAndKeepsTheRest`. The new text names both members with real crefs
  and reads "The `UiActionMap.ReadBindings` half of A6 sits in `BindingsRoundTripTests`".

**Hits left.** The brief's grep ran once over the six feature files. It gave exactly the three lines above, so the
round left no hit. The section 14 numbers stay as history, per Difference 2: **A seventeenth allocation gate.**
keeps its `(7 tests)` and its 23 actions and 28 bindings. The round changed no section 14 line, no
`technical-design.md` line, and no `CHANGELOG.md` line.

**Measured counts.** Build `0 Warning(s)` / `0 Error(s)`. Solution 2988, failed 0, succeeded 2985, skipped 3.
Framework 326, Core 1801. No test moved, so the round re-ran no class filter. The stale-pattern grep now gives no
hit. `git diff d2e0dcf --stat -- src/` lists the same three tracked files as before the round, and the
`UiActionMap.Bindings.cs` diff stays empty. The prose lint exits 0 on the four named docs.

This closes the first open question of the record, the stale "a later phase" phrase in the class remarks.

---

## Plan Shape (recorded for the phase briefs)

The full text lives in `implementation.md`. Each phase brief points at these sections.

- **The Listen State Machine** (`implementation.md:188-207`) - seven states, and the six rules the table cannot carry: settle, one candidate per frame, the fresh press edge for a digital capture, cancel-beats-fit, a reject that stays open, and a commit whose press is consumed.
- **The Capture Table (A5)** (`:209-225`) - the seven capturable shapes in `InputKind` order, what each accepts, the fields each writes, the stick-before-axis order, and `RebindDeltaThreshold = 8f`.
- **The Commit, the Group Rule, and the Conflict** (`:227-254`) - `FindConflictAction` before the commit, `RemoveBindings` before `Append`, the bypass of the throwing `FindClash`, and the new binding at the end of the array.
- **The Defaults Snapshot and the Restore (A6)** (`:256-262`) - `CaptureDefaults` on the first `Activate` only, and a restore that copies back into the existing array.
- **The Text Format** (`:264-293`) - the version line, the `;` and `=` grammar, the nine `device` numbers, the omit-the-default rule, the needed-count return, and the per-pair merge with the skip count.
- **Internal Shape** (`:295-302`) - the fields, the private `CapturedInput` struct, the two new partial files, and the five silence guards.
- **Public API (end state)** (`:146-186`) - the members this feature adds, and the rule that no new top-level public type appears (A10).
- **Notes N1 to N8** (`:716-725`) - the eight requirement-versus-code conflicts, kept as the interview settled them.

---

## Important Decisions

Carried from the plan (`implementation.md:480-579`). Add a new entry here when the build decides something the plan did not settle.

- **Q1/A1:** the listen branch sits inside `ReadFrame` after its pair test and returns early. The scan reads `context.Input`, the raw view.
- **Q9/Decision 9:** five paths go silent during the listen (`PressKey`, `PressMouseButton`, `PushGamepad`, `InvokeAction`, `HandleInput`). `PushGamepad` stores then returns 0, so an edge is lost, not queued. The device track and `OnSchemeChanged` keep running.
- **Q8/A4/Decision 4:** a conflicting rebind commits first, then reports both action names, and keeps both. The commit bypasses `FindClash`.
- **Q5/Q11/Decision 3:** the target is one action inside one scheme. `RemoveBindings` compacts in place, then `Append` writes at the end, so the array length stays flat.
- **A6/Decision 5:** the defaults snapshot comes from the first `Activate`, guarded by `_defaultsCaptured`.
- **A7/Decision 6:** `WriteBindings` returns the needed count for a short span and writes nothing. One private line writer serves the measure pass and the write pass.
- **A3:** `UiScheme.Default` ships here as `public const int Default = 0`, and the writer emits `-` for the empty name of id 0.
- **A10:** no new top-level public type. `BindingText` and `TryReadBinding` stay internal, and every test reaches the feature through the public API.

---

## Gotchas & Learnings

From the siblings, inherited by every brief of this feature:

- Set `MSBUILDDISABLENODEREUSE=1` on every build and test command. Give `-nodeReuse:false` to `dotnet build` only; on `dotnet test` it exits 5 (Gotcha 1 of input-state).
- A test host can exit with 134 (CLR error `0x80131506`), and the Core test `TheCurvesAllocateNothing` fails one time in a while. Run the tests a second time before you treat either as a fault.
- A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read a plant result. Copy to `src/BrutalUi.Framework/obj/plantscratch/`, restore with `cp`, and check with `cmp`.
- `TreatWarningsAsErrors` makes a missing XML doc comment (CS1591) a build error. The projects have no implicit usings, so each new file holds its own `using` lines.
- The sandbox blocks the X11 socket, so `scripts/test-linux.sh` stops at `GLFW init failed: PlatformUnavailable`. Record the exact line and hand the live check to a person.

---

## Testing Approach

- Six new Framework test classes: `RebindListenTests` (10), `RebindCaptureTests` (8), `RebindConflictTests` (4), `RebindResetTests` (4), `BindingTextWriterTests` (4), `BindingsRoundTripTests` (4).
- `ActionMapAllocationTests` grows from 7 to 8 with `RebindStep` and `ARebindAndAResetAllocateNothing`. It is the only existing class this feature edits.
- Eighteen plants across the five phases guard the rules that a green run alone cannot prove.
- Twelve existing Framework Actions classes plus the Panels classes must pass with no edit.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 2 demo: a developer runs the built Framework test binary with `--filter-class '*RebindCaptureTests'` and reads a total of 8 with `failed: 0`. The verifier measured it green, so this line is a convenience re-run, not an open check.

- [ ] Phase 3 group 1 demo: a developer runs the built Framework test binary with `--filter-class '*RebindConflictTests'` and `--filter-class '*RebindResetTests'` and reads 4 and 4 with `failed: 0`. The verifier measured both green, so this line is a convenience re-run. The `Restore` button landed in group 2, so the live `Rebind (Up)` then `Restore` sequence sits in the lines below.

- [ ] Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` outside the sandbox and confirm the smoke PASS line; the
  sandbox stops it at `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`
- [ ] Read the new vertex and draw-command counts of smoke line 2 with the `Rebind (Up)` button and the seventh label
  in the tree, and record them here for the later phases.
- [ ] Confirm the UI allocation sum (smoke line 4) stays 0 with the `RebindText` write inside the UI region.
- [ ] Run HelloPanel on a host with a window and read the seventh label: `Rebind: idle` at the start.
- [ ] Click `Rebind (Up)`, read `Rebind: press a key`, press J, read `Rebind: code 74`, and press J again to move the
  focus up (the Q7 demo).
- [ ] Click `Rebind (Up)` and press Escape: read `Rebind: cancelled`, and confirm the score does not reset (the listen
  consumes the cancel key, A9).
- [ ] Review group 3 check, on a host with a window: click `Rebind (Up)`, then click the left mouse button, and read
  `Rebind: code 0`, not `Rebind: idle`. A mouse-button capture carries code 0 to 7, so the stored code alone cannot
  name the idle state.
- [ ] Measure plant 5 on a host with a window: with `GC.KeepAlive(new object());` as the first line of
  `HelloScreen.RebindText`, smoke line 3 grows by 24 bytes per planted frame; restore the file after the run.
- [ ] Phase 3 group 2 demo, on a host with a window: click `Rebind (Up)`, press J, then click `Restore`. Read the `Up`
  key move the focus up again, and read `F1` and `F2` switch the mode again. The snapshot holds every bind of the
  build, so the restore gives the whole build back.
- [ ] Click `Restore` while the rebind label reads `Rebind: press a key`: the listen closes, no binding lands, and the
  label shows the last stored result, because the close rule calls no slot.
- [ ] Read the vertex and draw-command counts of smoke line 2 from the tree that holds the `Restore` button, and record
  them here. That read supersedes the phase 1 count asked above, because phases 4 and 5 keep this tree.
- [ ] Phase 4 demo: run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class
  '*BindingTextWriterTests'`, then the same binary with `--filter-class '*BindingsRoundTripTests'`, and read 4 and 4 with
  `failed: 0`. The verifier measured both green, so this line is a convenience re-run. A person may also write one
  binding file with `WriteBindings`, edit one line by hand, and read it back with `ReadBindings`.
- [ ] Phase 5 demo: run the same binary with `--filter-class '*ActionMapAllocationTests'` and read a total of 8 with
  `failed: 0`. The verifier measured it green, so this line is a convenience re-run.
- [ ] One doc choice stays open for a person: section 14 of `docs/technical-design.md` keeps the historical figure
  `(7 tests)` in the bullet **A seventeenth allocation gate.**, while the shipped class holds 8 tests. The new bullet
  **Remap tests.** states the shipped number. control-schemes updated its own figure in its review round, so the same
  edit here is a one-line change if you want the history to read the shipped state.

---

## Session Notes

### 2026-11-10 03:40
- `/autorun-feature input/binding-remap` started. Preconditions passed: the plan exists, requirements exist, and the epic resolved to `docs/features/input/binding-remap/`.
- Branch `feat/input/binding-remap` created from `main` at `d2e0dcf`. No commit and no push.
- Scaffolded `tasks.md` (44 tasks, verbatim from the plan), `context.md`, and `inbox.md`. Status flipped to In Progress.
- Next: phase 1 group 1 at the ADVANCED tier.

---

### 2026-11-10 04:20
- Phase 1 group 1 built by the `game-dev-advanced` agent (tasks 1.2 to 1.11). See the Phase 1 Group 1 Record.
- Files: `UiScheme.cs` (the `Default` id), `UiActionMap.Remap.cs` (new, 655 lines), `UiActionMap.Read.cs` (the branch
  and the four guards), `UiActionMap.cs` (four class paragraphs, the `ResetFrame` close), and
  `RebindListenTests.cs` (new, 10 tests).
- `UiActionMap.Bindings.cs`, `src/BrutalUi.Core/`, `src/BrutalUi.Glfw/`, and every project file keep an empty diff.
- Four plants ran, each with a clean build and a non-zero test exit, then restored under `cmp`.
- Two plan gaps settled in the record: the device rule for a no-candidate listen frame, and the repeat-input shape of
  the silence test.

---

### 2026-11-10 04:25
- Phase 1 group 2 built by the `game-dev-advanced` agent (tasks 1.12 to 1.16). See the Phase 1 Group 2 Record.
- Files: `HelloState.cs` (the `hello.rebind` command, the slot write, `RebindButton`, `KeyboardScheme`, `RebindStatus`,
  `RebindCode`), `HelloScreen.cs` (`RebindText`, `LongestRebindText`, the seventh label, remarks to seven labels),
  `HelloModes.cs` (`_rebindLabel`, `RebindLabel`), and `Program.cs` (`rebindScratch`, the write inside the UI region,
  `PushSmokeInput` unchanged).
- Build `0 Warning(s)` / `0 Error(s)`; solution 2963, 0 failed, 3 skipped; Framework 301. Phase 1 is complete.
- `scripts/test-linux.sh` stops at the smoke step with `GLFW init failed: PlatformUnavailable`; the live demo and the
  line 2 counts moved to Needs Human Verification.
- Plant 5 builds with `0 Error(s)`, has no headless observer, and restored equal under `cmp`.

---

### 2026-11-10 05:00
- Phase 2 group 1 built by the `game-dev` agent (tasks 2.1 to 2.5). See the Phase 2 Group 1 Record.
- Files: `UiActionMap.Remap.cs` (`TryCaptureCandidate`, `FitsCapture`, `DeviceOf`, the widened `RebindScan`, the new
  remarks; `TryCaptureKey` and `RebindPadLive` gone) and `RebindCaptureTests.cs` (new, 8 tests). No other `src/` file,
  no sample, and no project file changed in this group.
- Build `0 Warning(s)` / `0 Error(s)`; solution 2971, 0 failed, 3 skipped; Framework 309; `RebindCaptureTests` 8 and
  `RebindListenTests` 10 with `failed: 0`. Phase 2 is complete.
- Plants 6, 7, and 8 each exit 2 with `0 Error(s)` and restore equal under `cmp`.
- Four plan gaps settled in the record: `TrailingZeroCount` for the lowest pad bit, the device rule for a pad with no
  edge, the stick-before-axis reading of the raw-axis clause, and the Button latch that needs a fresh press after a
  commit.
- The run accepts all four. `BitOperations.Log2` names the highest set bit, and the rule in "The Capture Table" reads
  the lowest, so `TrailingZeroCount` is the correct reading of the rule and the plan's method name is the mistake.
- Section 9 of phase 5 states these three readings: the lowest-set-bit rule for a gamepad button capture; the
  stick-before-axis order, with `GamepadAxis` codes 2 and 3 unreachable through the listen; and the device rule of a
  listen frame — the device of the live candidate, else the last report carries.
- Phase 3 still owes one small edit: both delegate remarks switch `<c>ResetBindings</c>` to a real
  `<see cref="ResetBindings"/>` when the member ships.

---

### 2026-11-10 05:30
- Phase 3 group 1 built by the `game-dev-advanced` agent (tasks 3.1 to 3.5). See the Phase 3 Group 1 Record.
- Files: `UiActionMap.Remap.cs` (`_defaults`, `_defaultsCount`, `_defaultsCaptured`, `CaptureDefaults`, `ResetBindings`,
  the file remark tail, and both `ResetBindings` crefs) and `UiActionMap.cs` (the call in `Activate`, one sentence in the
  `Activate` remarks, and the conflict and restore paragraphs of the class remarks). New:
  `RebindConflictTests.cs` (4) and `RebindResetTests.cs` (4). No sample file, no `UiActionMap.Bindings.cs`, no
  `UiActionMap.Read.cs`, and no project file changed in this group.
- Build `0 Warning(s)` / `0 Error(s)`; solution 2979, 0 failed, 3 skipped; Framework 317; conflict 4 and reset 4 with
  `failed: 0`; listen 10 and capture 8 still green; `ActionMapAllocationTests` 7 with 0 bytes.
- Plants 9, 10, and 11 each exit 2 with `0 Error(s)` and restore equal under `cmp`. Plant 9 also breaks
  `TheCancelKeyIsOneSettablePropertyWithARangeCheck` and `TheSnapshotComesFromTheFirstActivate`; plant 10 also breaks two
  shipped 0-byte mode gates, which is the load-time reading of the once rule.
- Four plan gaps settled in the record: plant 11 shows a snapshot alias rather than an allocation, so the first reset
  test holds a second rebind and restore; the once rule needs a bind between two activations; a restore does not consume
  the snapshot; and a throwing conflict handler costs that frame's device report.
- The carried `<c>ReadBindings</c>` cref in the `ResetBindings` remarks is a phase 4 switch. Phase 3 group 2 (tasks 3.6
  and 3.7, the `Restore` button and the smoke run) has not run.

---

### 2026-11-10 05:32
- Phase 3 group 2 built by the `game-dev-advanced` agent (tasks 3.6 and 3.7). See the Phase 3 Group 2 Record.
- File: `samples/HelloPanel/HelloState.cs` (the `hello.restore` command, the `s_restore` handler, `RestoreButton`, one
  row child, the restore paragraph, and the class summary that names three commands). No `src/` file, no test file, and
  no other sample file changed in this group.
- Build `0 Warning(s)` / `0 Error(s)`; solution 2979, 0 failed, 3 skipped; Framework 317; the four rebind classes still
  read 10, 8, 4, and 4 with `failed: 0`. Phase 3 is complete (7/7).
- `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` stops at the smoke step with
  `HelloPanel: GLFW init failed: PlatformUnavailable (X11: Failed to open display :109).`, `exit=1`. The live
  `Rebind (Up)` then `Restore` sequence, the listen close on a `Restore` click, and the moved smoke line 2 counts sit
  in Needs Human Verification.
- No framework defect found. The mode diff mounts the panels in step 4 before it activates the map in step 5, so the
  snapshot exists before any click can reach the `Restore` button.
- One gap sits outside this brief: three remarks in `HelloScreen.cs` still name two buttons in the state row. Handed
  back to the orchestrator.
- Next: phase 4 (tasks 4.1 to 4.9), `WriteBindings`, `ReadBindings`, and the round trip.

### 2026-11-10 05:40
- The orchestrator recorded the run state of the three closed phases. Each gate ran on a verifier subagent that changed
  no file.
- Phase 1 gate: PASS, `Demo: MANUAL` (the sandbox blocks the display socket, so the windowed half sits in Needs Human
  Verification). Phase 2 gate: PASS, `Demo: PASS` (`RebindCaptureTests` 8 with `failed: 0`). Phase 3 gate: PASS,
  `Demo: MANUAL` (the headless half read 4 and 4 with `failed: 0`; the `Rebind (Up)`, J, `Restore` sequence needs a
  window).
- Measured state at the end of phase 3: build `0 Warning(s)` / `0 Error(s)`; solution 2979, 0 failed, 3 skipped;
  Framework 317; Core 1801. The plant and `cmp` records sit in each phase record.
- The run accepted the four readings that phase 2 settled (`TrailingZeroCount` for the lowest gamepad bit, the
  stick-before-axis order with axis codes 2 and 3 unreachable through the listen, the device rule of a frame with no
  candidate, and the fresh press that the Button latch needs), and the three that phase 3 settled (the snapshot alias
  that plant 11 proves, the bind between two activations that pins the once rule, and the lost device report of a
  handler that throws). Section 9 of phase 5 states all of them from the code.
- One open item routes to the phase 5 docs group: three remarks in `samples/HelloPanel/HelloScreen.cs` (`:33-34`,
  `:36-37`, and `:59-60`) still name two buttons in the state row and miss `Restore`. Prose only, no code line.
- Carried item for phase 4 group 2: the `ResetBindings` remarks hold `<c>ReadBindings</c>` as plain text; switch it to
  a real `<see cref="ReadBindings"/>` when the member ships.

### 2026-11-10 07:55
- Phase 5 group 2 built by the `game-dev` agent (tasks 5.3 to 5.7). See the Phase 5 Group 2 Record.
- Files: `docs/technical-design.md` (section 9 items 15 and 16, the `### Action map` bullets, section 14),
  `CHANGELOG.md` (one line), `context.md`, and `implementation.md` (prose only, in the non-task sections). No code file
  changed.
- Build `0 Warning(s)` / `0 Error(s)`; solution 2988, 0 failed, 3 skipped; Framework 326; Core 1801; the six class
  filters 10, 8, 4, 4, 4, and 4; the gate 8; each with `failed: 0`. The prose lint exits 0 on the four docs.
- The feature is complete (44/44 tasks), and it closes the `input` epic. `/update-epic` and `/update-master` pick the
  row up. No commit, no branch, and no push in this run.

### 2026-11-10 10:05
- `/autorun-feature input/binding-remap` ran the feature end to end: five phases, nine implementation groups, one
  verifier subagent per phase, one cross-phase review, and three review fix groups. No phase waited on a person, and no
  gate needed more than its own fix round.
- End state: 44/44 tasks, 5/5 phases, and 6 of 6 review findings fixed. Build `0 Warning(s)` / `0 Error(s)`; solution
  2988 with 0 failed and 3 skipped; Framework 326; Core 1801. Twenty phase plants plus review plants 19 and 20 each
  restored equal under `cmp`.
- Every phase gate returned PASS. The phase 1 and phase 3 demos returned `Demo: MANUAL`, and the phase 2, 4, and 5 demos
  returned `Demo: PASS`. The windowed HelloPanel checks stay for a person, as in every sibling feature, because the
  sandbox blocks the X11 socket.
- The review round changed three things in code, not prose: the build-time slack in `CaptureDefaults` (CR-1), the
  reserved `-` scheme name in `AddScheme` (CR-6), and the `RebindDone` flag that drives the sample's idle label (CR-5).
  `UiActionMap.Schemes.cs` entered the diff as a stated deviation from the plan's File Tree.
- The docs rollups are current: `implementation.md` reads Ready for Review, `tasks.md` reads 44/44, the epic overview
  rolls the epic up as Complete (173/173), and `docs/overview.md` carries one input row plus the v2.4 changelog line.
- No commit, no push, and no branch beyond `feat/input/binding-remap`. A person tests the open Needs Human Verification
  lines, then commits and opens the PR.

## Review Round 1

Built by the `game-dev-advanced` agent on 2026-11-10, cross-phase review group 1, finding CR-1. Branch
`feat/input/binding-remap`, base `d2e0dcf`. No commit, no branch, and no push. Tick no task in `tasks.md`: all 44 hold.

### Review group 1

**Finding CR-1.** A rebind commit onto an action-and-scheme pair that holds no binding frees no slot, so `Append` can
resize the live binding array inside `ReadFrame`. That broke the no-allocation rule on a state a rebind menu ships.

**What changed.** Three files:
- `src/BrutalUi.Framework/Actions/UiActionMap.Remap.cs`: `CaptureDefaults` grows the live array at the first `Activate`
  when `_bindings.Length < _bindingCount + _actionCount * _schemeCount`, with one `new InputBinding[needed]`, an
  `Array.Copy` of the live entries, and `_bindingCount` kept. The snapshot array stays separate, so the plant 11 alias
  proof holds. `CommitRebind` states the two-part free-room rule. The method remarks state the slack rule and the
  scheme-reopen rule.
- `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs`: `RebindStep` commits onto the kick action in the
  default scheme (`UiScheme.Default`), whose group holds no binding, so the empty-group append runs inside the measured
  body. `BuildScene` asserts the post-`Activate` capacity: 32 bindings at capacity 110. The class remarks and the
  `TheMeasuredBodyDoesRealWork` comment now read the empty-group case.
- `docs/technical-design.md`: section 9 item 15 **The cost.** names the empty-group case, the build-time slack, and the
  scheme-reopen rule. Every other sentence of item 15 is unchanged.

**Blast radius.** `CaptureDefaults` and `CommitRebind` are the only two methods touched, both on the private frame path
of the remap partial. No public signature moved. `Append`, `RemoveBindings`, `ResetBindings`, the listen state machine,
and the snapshot separate-array rule are unchanged. `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` is
untouched (the I2 empty-diff check holds, and `grep -c 'public void Bind'` reads 18). The grow runs only inside
`CaptureDefaults`, which runs once on the first `Activate`; `Activate` is a build-time step, so the new array cost lands
at build time, not in a frame.

**Measured counts.** Build `0 Warning(s)` / `0 Error(s)`. Solution total 2988, failed 0, succeeded 2985, skipped 3.
Framework 326, Core 1801. `ActionMapAllocationTests` total 8, failed 0, `ARebindAndAResetAllocateNothing` at 0 bytes.

**Plants.** Plant 19 skips the slack grow. It builds with `0 Error(s)` and fails `ARebindAndAResetAllocateNothing` (and
every class test) at the `BuildScene` capacity assert, `Expected: 110, Actual: 32`. Plant 20 adds `GC.KeepAlive(new
object());` in `CommitRebind` after `Append`. It builds with `0 Error(s)` and fails `ARebindAndAResetAllocateNothing`
only, at the byte assert, with 2400 bytes. Each plant restored from
`src/BrutalUi.Framework/obj/plantscratch/UiActionMap.Remap.cs.g1orig` with `cp` and verified with `cmp`. The clean suite
reads 2988 with 0 failed after both.

**Decision the review did not state.** The brief expected plant 19 to fail `ARebindAndAResetAllocateNothing` with a
recurring byte count that names the resize. A recurring byte delta is not physically available for one net add per round:
`Append` doubles `Array.Resize` once (32 to 64) in a warm-up round, and `ResetBindings` cannot shrink the array, so the
100 measured rounds fit and read 0 bytes. The empty-group leg stays sharp: plant 19 turns the gate red. It reads at the
post-`Activate` capacity assert, which is the direct and honest proof that the grow path runs. Plant 20 carries
the 2400-byte proof that the measured body runs the commit every round. The chosen bound (`_bindingCount +
_actionCount * _schemeCount`) makes the gate scene capacity 110, which the `BuildScene` assert now pins.

**Open questions.** None for a public API shape. A game that adds a scheme after the first `Activate` reopens the grow,
and a rebind menu that adds schemes at runtime should activate again or read bindings through the text; the doc states
this rule, and no code change is needed for the shipped single-scheme-menu case.

### Review group 2

Built by the `game-dev` agent on 2026-11-10, cross-phase review group 2, findings CR-2, CR-3, and CR-6: the
stored-text contract. Branch `feat/input/binding-remap`, base `d2e0dcf`. No commit, no branch, and no push. Tick no
task in `tasks.md`: all 44 hold.

**Findings.** CR-2: a name that holds the field separator or the line separator writes a text that reads back short,
and the loss shows only in `Skipped`. CR-3: item 16 claimed the reader runs "the same range check" as the bind; for
`device=2` the reader is stricter, so a bound bit at or above 15 skips on read-back. CR-6: `AddScheme("-"u8)` interned
the stored token for scheme 0, so one write-and-read moved that scheme's bindings into scheme 0.

**What changed.** Six files:
- `src/BrutalUi.Framework/Actions/UiActionMap.BindingsText.cs`: remarks only. The `WriteBindings` remarks name the
  fence (**The name rule.**), the `ReadBindings` remarks state it from the reader side, and the `ReadBindings` range
  remark and the `GamepadButtonBitLimit` doc now state the 15-button cap for `device=2`. No code line changed.
- `docs/technical-design.md`: section 9 item 16. **The lines.** gains the one name-rule sentence with the consequence
  (`Applied` equal to `BindingCount` after a read proves the whole list). **The range rule.** now reads that the parser
  runs at least the checks that the matching bind runs, and adds the 15-button cap for `device=2`, because the gamepad
  state holds buttons 0 to 14, so a file that holds a wider bound bit skips that line. Item 15 is group 1's and is
  unchanged here.
- `src/BrutalUi.Framework/Actions/UiActionMap.Schemes.cs`: `AddScheme` rejects a name equal to `-` with an
  `ArgumentException` and the parameter name `name`, beside the empty-name check, with the round-trip reason in the
  remarks. No other member changed.
- `tests/BrutalUi.Framework.Tests/Actions/BindingsRoundTripTests.cs`: `ABadLineIsSkippedAndCounted` gains the
  `BadText` line `action=Fire;scheme=Gamepad;device=2;code=65536`, so the expected `Skipped` moves 14 to 15, and a
  public-side name case: `AddAction("attack;heavy")`, one bind, `WriteBindings`, and a read into a second map gives
  `Applied` 0, `Skipped` 1, and version 1.
- `tests/BrutalUi.Framework.Tests/Actions/BindingTextWriterTests.cs`: the first test pins that `AddScheme("-"u8)`
  throws with `ParamName` `name`, and that a scheme named `Default` still interns.
- `docs/features/input/binding-remap/code-review.md`: CR-2, CR-3, and CR-6 ticked, with the group 2 fix-round entry.

**Stated deviation from the plan.** `UiActionMap.Schemes.cs` is not in the plan's File Tree (implementation.md:111),
and Definition-of-Done check I1 does not freeze it, so the one-line `AddScheme` guard plus its remarks are in scope for
the feature that owns the text format: the ruling above says a stored contract must not guess, and the alternative was
a writer that guesses. Record this as a stated widening, not a silent extra. `/update-feature` may add the file to the
File Tree or carry the note.

**Follow-up CR-2 leaves open.** Add a separator guard to `AddAction`: reject an action name that holds `;` or a line
feed, with the parameter name `action`. `UiActionMap.Bindings.cs` is byte-frozen for this feature (Definition-of-Done
check I2), so the guard lands in the first pass that may open that file. The contract ships now as doc and pins.

**Measured counts.** Build `0 Warning(s)` / `0 Error(s)`. Solution total 2988, failed 0, succeeded 2985, skipped 3.
No test method was added, so the total stands. `--filter-class '*BindingTextWriterTests'` total 4, failed 0.
`--filter-class '*BindingsRoundTripTests'` total 4, failed 0, on the raised skip count.
`git diff d2e0dcf -- src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` is empty; `grep -c 'public void Bind'`
reads 18; the Core, Glfw, project-file, and scripts diffs are empty.

**Open questions.** None. The writer and the reader keep their exact behavior; the group changed docs, remarks, and
one door check.

### Review group 3

Built by the `game-dev` agent on 2026-11-10, cross-phase review group 3, findings CR-4 and CR-5: the slot call-site
sentence and the sample idle read. Branch `feat/input/binding-remap`, base `d2e0dcf`. No commit, no branch, and no
push. Tick no task in `tasks.md`: all 44 hold.

**Findings.** CR-4: the slots paragraph said the map calls both slots inside `UiContext.Update(float)`. A
`CancelRebind` call reports at the point of the call, and a game can reach that point between frames. CR-5:
`HelloScreen.RebindText` decided "no result yet" from the stored code. A mouse-button commit carries code 0 to 7, so
the label read `Rebind: idle` forever after such a commit.

**What changed.** Six files:
- `src/BrutalUi.Framework/Actions/UiActionMap.Remap.cs`: remarks only. The `RebindResultHandler` summary drops the
  inside-`Update` claim. Both delegate place rules state the two call sites: the frame read calls the result slot
  for a cancel key, a reject, and a commit, and calls the conflict slot after a commit. A `CancelRebind` call reports
  through the result slot at the point of the call, which may sit outside `UiContext.Update(float)`. The result
  re-entry rule adds the consequence: outside the read the rule does not apply, and an `Update` call from the handler
  does not throw, so a handler that must know the place reads `IsActive` or keeps its own flag. `CancelRebind` gains
  the report rule. No code line changed.
- `docs/technical-design.md`: section 9 item 15 **The two slots.** states the same two call sites and the same
  consequence. **The cost.** stands as group 1 wrote it. Items 1 to 14 are untouched.
- `samples/HelloPanel/HelloState.cs`: `RebindDone` joins the stored result. `OnRebind` sets it beside the
  `RebindStatus` and `RebindCode` stores. The property remarks state that a code alone cannot tell an idle map from a
  mouse-button commit, because a mouse-button capture carries code 0 to 7. The rebind rule paragraph names the flag.
- `samples/HelloPanel/HelloScreen.cs`: `RebindText` takes a `bool done` argument. The idle branch reads from `!done`
  in place of the `code != 0` test, and the bound branch writes its code whatever it is. The "32 or above" claim is
  gone, and the remarks state the true rule. Every text, the `LongestRebindText` seed, and each
  `InvalidOperationException` guard hold.
- `samples/HelloPanel/Program.cs`: the one `RebindText` call inside the `uiStartBytes`/`uiEndBytes` region passes
  `state.RebindDone`. `PushSmokeInput` is unchanged.
- `docs/features/input/binding-remap/code-review.md`: CR-4 and CR-5 ticked, the group 3 fix-round entry added, and
  the status header now reads `Closed - [6] findings, 6 resolved`, the shape of every closed sibling review.

**The replaced decision.** The **Phase 1 Group 2 Record** decided the idle test from the pair (status 0, code 0). Its
reason was "a real key code is 32 or above." That decision was wrong, because the listen captures a mouse button as
well as a key, and a mouse-button capture carries code 0 to 7. This group replaces it with the `RebindDone` flag, as
the CR-5 ruling names. The sentinel-code alternative would have made the sample disagree with the slot contract,
which states that a `RebindBound` result carries the code that landed.

**Blast radius.** Remarks, docs, and the sample only. No test builds the sample, so no test moved.
`UiActionMap.Bindings.cs`, `UiActionMap.Read.cs`, `UiActionMap.cs`, `UiScheme.cs`, `src/BrutalUi.Core/`,
`src/BrutalUi.Glfw/`, and every project file keep an empty diff in this group. The work of groups 1 and 2 stands
unchanged: no file of theirs was touched.

**Measured counts.** Build `0 Warning(s)` / `0 Error(s)`. Solution total 2988, failed 0, succeeded 2985, skipped 3.
Framework 326, Core 1801. The counts equal the state at the start of this group.

**Prose lint.** `ste-lint.py --fail-over 2.5` after this edit: `docs/technical-design.md` 1.53 hits per 100 words,
`context.md` 1.85 hits per 100 words. Both sit at or below the 2.5 bar.

**Follow-up.** The parent ruled the comment into this group's scope. The comment above the `RebindText` call in
`Program.cs` now reads "The reads copy three stored values." No other line of that file changed. No open question
stays in this review round.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
