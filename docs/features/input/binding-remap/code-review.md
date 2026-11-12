# input/binding-remap - Code Review

**Status:** Closed - [6] findings, 6 resolved

**Review date:** 2026-11-10

**Reviewer:** Independent cross-phase review agent

**Branch:** `feat/input/binding-remap`, base `d2e0dcf`. The work sits in the tree uncommitted; this review read the tree.

**Scope:** `UiActionMap.Remap.cs`, `UiActionMap.BindingsText.cs`, the `UiActionMap.Read.cs` and `UiActionMap.cs` diffs, `UiScheme.cs`, the four HelloPanel files, the six new test classes and the grown `ActionMapAllocationTests`, section 9 items 15 and 16, section 14, and the changelog line.

**Method note:** one finding per class the brief names, stated only with a file, a line, and a concrete consequence. The gates already measured this tree; no build, test, or plant ran here.

---

## Summary

Six findings: three MEDIUM, three LOW. No CRITICAL. The state machine, the silence on the five paths, the snapshot discipline, and the shipped surface all hold against section 9 line by line, and the inherited items (CR-5 handle, CR-6 range checks, `UiScheme.Default`) ship. The misses are of the kind this epic has produced before: a grow the gate's own comment reasons about but does not bound, a round-trip loss class the quality bar forbids but the format does not fence, a reader range stricter than the bind it claims to copy, and three name or sentence choices a game can read wrong.

Counts by severity: 0 CRITICAL, 3 MEDIUM, 3 LOW.

## Findings

### CR-1 [MEDIUM] An empty-group commit on a capacity-exact array grows the binding array inside the frame read

**Rule:** AGENTS.md "No managed allocation in update or draw after warm-up"; section 9 item 15 "The cost." ("the commit ... allocate nothing after warm-up"); the free-room rule in the code's own remark.

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Remap.cs:743-745` (the claim), `:753-777` (`CommitRebind`); `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:1276-1281` (the grow); `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs:314-319` (the gate scene).

**Problem:** the commit removes the target group, then appends. The remark reasons the grow cannot happen:

```csharp
// UiActionMap.Remap.cs:743-744
/// The free-room rule: <see cref="RemoveBindings"/> runs before <see cref="Append"/>, so the frame path does not grow
/// the array (A1).
```

That holds only when the group holds at least one binding. When the pair is empty, the removal frees nothing and the append raises the count. `Append` then resizes:

```csharp
// UiActionMap.Bindings.cs:1278-1280
if (_bindingCount == _bindings.Length)
{
    Array.Resize(ref _bindings, _bindings.Length == 0 ? InitialCapacity : _bindings.Length * 2);
```

`BeginRebind` opens a listen on any known action, including one with no binding in that scheme — the empty row of a rebind menu, which a game ships. When the live count equals the capacity at that moment, the commit allocates inside `ReadFrame`, in the pass the gate claims at 0 bytes. The gate scene is exactly such an array: the build-time assert pins 32 bindings at capacity 32 (`Assert.Equal(32, bindings.Length)`), and `RebindStep` drives only the remove-one-and-append-one shape. No test names the empty-group commit on a full array. `RebindCaptureTests` does commit an empty group (`ANonZeroScrollCommitsAScrollBinding`, lines 397-431, count 0 to 1), but on a fresh array with room, and outside any allocation measure — so the grow path itself passes unmeasured today.

**Fix:** bound the risk at build time. In `CaptureDefaults` (first `Activate`, allocation permitted), grow the live array when `_bindingCount + _actionCount * _schemeCount > _bindings.Length`, so the capacity covers one net add for every action-and-scheme pair the table can name; a commit can then never resize in a frame while the scheme table stands still. One line of state plus a copy at Activate (a few KB for a real map). Then: add one leg to `RebindStep` that commits to an unbound action in a scheme (so the gate measures the empty-group commit at the pinned 32-at-32 capacity), and correct the free-room sentence to name the empty-group case and the Activate slack. Say plainly in the doc that schemes added after the first `Activate` reopen the grow, the same way binds after the first `Activate` reopen the snapshot loss.

**Effort:** Medium

### CR-2 [MEDIUM] A name that holds a semicolon or a line feed writes a text that does not read back, and the loss is silent

**Rule:** the plan Quality Bar "No silent data loss on a round trip"; section 9 item 16 "The lines" and "The skip rule".

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.BindingsText.cs:371-372` and `:375-376` (raw name bytes), `:549-551` (one split on each semicolon), `:287-290` (one split on each line feed); `docs/technical-design.md:955` (the grammar).

**Problem:** the writer emits the action name and the scheme name bytes unchanged:

```csharp
// UiActionMap.BindingsText.cs:371-372
Emit(ActionField, destination, ref at, measure);
Emit(_actions[b.Action].Name, destination, ref at, measure);
```

`AddAction` and `AddScheme` reject an empty name and nothing else, so an action named `attack;heavy` or a name that holds a line feed is legal and reaches the text. The line then breaks the grammar: a `;` in a name splits one binding into two fields and the reader counts the line `Skipped` (an action value that stops at the `;` names no action); a `\n` in a name fabricates an extra line, which is also a bad line. The read throws nothing and reports the loss only inside the `Skipped` count, so `Applied < BindingCount` is the only sign, and a game that does not compare loses a stored binding silently. The format grammar in item 16 states no rule about name bytes, so the quality-bar promise does not hold for names a shipped build call accepts.

**Fix:** state the fence where the contract lives: add to item 16 and to the `WriteBindings` remarks that an action name and a scheme name must not hold `;` or a line feed, because such a name gives a line the reader counts in `Skipped`. The real validation belongs in `AddAction`, and `UiActionMap.Bindings.cs` is byte-frozen for this feature (I2), so the cheap honest step is the doc fence now plus a named follow-up check in the next pass that may open that file. Cost: two doc sentences now; one guard and one test in the follow-up.

**Effort:** Low

### CR-3 [MEDIUM] The reader's gamepad-button range is stricter than the bind, so a bound bit at or above 15 is lost on read-back

**Rule:** section 9 item 16 "The range rule" — "each parsed value passes the same range check that the matching bind runs (CR-6 of input-actions)"; the Quality Bar round-trip clause.

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.BindingsText.cs:82-86` (`GamepadButtonBitLimit`), `:711-720` (the kind case), `:797-815` (the magnitude cap of the number parse); `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:318-323` (the shipped bind).

**Problem:** `BindGamepadButton` checks one thing:

```csharp
// UiActionMap.Bindings.cs:320-323
if (BitOperations.PopCount((uint)button) != 1)
{
    throw new ArgumentException("The gamepad button must be exactly one button.", nameof(button));
}
```

It accepts any single bit, so a build of `BindGamepadButton(action, (UiGamepadButtons)(1 << 16))` succeeds. The reader then runs a different check:

```csharp
// UiActionMap.BindingsText.cs:713-716
if ((uint)code >= GamepadButtonBitLimit || BitOperations.PopCount((uint)code) != 1)
{
    return false;
}
```

So the writer emits `device=2;code=65536` for that binding, and `ReadBindings` counts the line bad and skips it: the same map does not read back, silently, and the item-16 sentence "the same range check that the matching bind runs" is false for device 2. A bit 31 case is worse: the binding code is the negative int `-2147483648`, `TryParseSignedInt` caps the magnitude at `int.MaxValue` and fails the parse, so even a widened kind check could not read the line back. The mismatch is a range statement that a public call (the frozen bind) can step outside.

**Fix:** keep the strict reader — a bit at or above 15 is GLFW-dead (the state holds buttons 0 to 14), and the input-actions CR-6 intent is "no dead binding lands" — and correct the sentence instead: item 16 should say the reader runs at least the checks of the matching bind, and adds the 15-button cap for device 2, so a file written from a map with a wider bind line skips that line. Add one `BadText` case with `device=2;code=65536` to `ABadLineIsSkippedAndCounted` so the behavior is pinned from the public side. Cost: one doc sentence and one test line. (Widening the parser to negative codes is the wrong direction: it lands dead bindings.)

**Effort:** Low

### CR-4 [LOW] "The map calls both inside `UiContext.Update(float)`" is contradicted by the synchronous report of `CancelRebind`

**Rule:** section 9 item 15 "The two slots" (and the same sentence in the handler remarks).

**File:** `docs/technical-design.md:952`; `src/BrutalUi.Framework/Actions/UiActionMap.Remap.cs:88-90` (the place rule), `:321-332` (`CancelRebind`).

**Problem:** the slots paragraph reads: "The map calls both inside `UiContext.Update(float)`, in the frame read, in the frame that closes or rejects the listen." But a game call closes the listen at the point of the call:

```csharp
// UiActionMap.Remap.cs:329-330
byte[] name = _actions[_rebindAction].Name;
CloseRebind();
OnRebindResult?.Invoke(this, name, RebindCancelled, RebindNoKey);
```

A menu button that runs a command inside the drain calls it inside `Update`, and the sentence holds by accident; game code that calls `CancelRebind` between frames calls the handler outside `Update` outright. A game that writes its handler to the stated place gets the wrong model there: the re-entry rule ("a bind in it takes effect at the next frame") does not apply outside the read, and the rule that an `Update` call from the handler throws does not fire.

**Fix:** state the two call sites once, in item 15 and in both remarks: the frame read calls the slots for a cancel key, a reject, and a commit; `CancelRebind` calls the result slot at the point of the call, which may be outside `Update`. A handler that must know the place can read `IsActive` or keep its own flag; no code change. Cost: three doc sentences.

**Effort:** Low

### CR-5 [LOW] The sample maps `RebindBound` with code 0 to "idle", and the remark's claim that a bound code is "32 or above" is false

**Rule:** section 9 item 15 "The sample rule"; the capture table (a mouse-button capture carries code 0 to 7, a gamepad-button capture carries a power of two).

**File:** `samples/HelloPanel/HelloScreen.cs:834-836` (the claim), `:897` and `:913` (the heuristic); `samples/HelloPanel/HelloState.cs:206-211` (the default 0/0 state).

**Problem:** the label decides "no result yet" from the two stored values:

```csharp
// HelloScreen.cs:897
if (status == UiActionMap.RebindBound && code != 0)
...
// HelloScreen.cs:913
ReadOnlySpan<byte> text = status == UiActionMap.RebindBound ? RebindIdleText : RebindOffText;
```

and the remark justifies it: "a bound result carries the pressed key code, and that code is 32 or above." The listen captures more shapes than keys: a mouse-button capture gives code 0 to 7. A player who clicks `Rebind (Up)` and then clicks the left mouse button commits `NavigateUp` on button 0, the slot stores status 0 with code 0, and the label reads `Rebind: idle` forever, as if no listen ever ran. The manual sequence in Needs Human Verification presses J and Escape only, so no windowed step covers a mouse commit either. A game that copies this sample pattern ships the same wrong read.

**Fix:** track the fact instead of the code: set one `bool RebindDone` in `HelloState.OnRebind`, pass it to `RebindText`, and pick idle from `!RebindDone`; drop the "32 or above" sentence from the remark. Cost: one bool, two lines, one doc correction; add one mouse-click line to the pending windowed sequence.

**Effort:** Low

### CR-6 [LOW] A scheme literally named `-` aliases the default token, and a write-and-read moves its bindings into scheme 0

**Rule:** section 9 item 16 "The scheme token"; the round-trip clause of the Quality Bar.

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.BindingsText.cs:376` (the writer), `:565` (the reader); `src/BrutalUi.Framework/Actions/UiActionMap.Schemes.cs:81` (`AddScheme`, which interns any non-empty name).

**Problem:** the writer special-cases only id 0:

```csharp
// UiActionMap.BindingsText.cs:376
Emit(b.Scheme == 0 ? DefaultSchemeToken : SchemeName(b.Scheme), destination, ref at, measure);
```

`AddScheme("-"u8)` is legal and gives an id at or above 1. A binding of that scheme writes `scheme=-`, and the reader resolves the token first:

```csharp
// UiActionMap.BindingsText.cs:565
scheme = name.SequenceEqual(DefaultSchemeToken) ? 0 : IndexOfScheme(name);
```

so the binding lands in the default scheme after one write-and-read: it then reads under every lock and clashes with every scheme — a silent behavior change for the game's data. No test names a scheme that is literally the token.

**Fix:** reject the reserved token in `AddScheme`, next to the empty-name check (a `SequenceEqual(DefaultSchemeToken)` test that throws `ArgumentException` with the parameter name `name`), plus one assert in `SchemeGroupTests`. Note the file fact plainly: `UiActionMap.Schemes.cs` is not in this feature's edited list, so the run must rule the one-line widening; the fallback for this feature alone is one sentence in item 16 that names `-` as reserved bytes for scheme names.

**Effort:** Low

## What Is Already Good

- **The state machine matches item 15 line by line.** The settle frame consumes without reading (`Remap.cs:655-658`), the cancel edge runs before the fit test (`:663`), the reject keeps the listen open and re-reports while the analog candidate stays live, the second `BeginRebind` moves the target with a fresh settle, and the inactive rule holds because only `ReadFrame` scans. Plants 1, 2, and 6 to 8 guard the rules the gate can see.
- **The silence is complete.** The five paths read exactly as the doc states: `ReadFrame` branches to the scan after the pair test and returns, `PressKey` and `PressMouseButton` give `false` first, `InvokeAction` gives `false` before the name read, `PushGamepad` stores then returns 0, and `HandleInput` keeps its guard and repeat rule above the two calls. No dead path lets a command through during a listen.
- **The commit order is right.** The clash index comes before the compaction, the listen closes before both slot calls, and the scan sets the device store after the slots run — so a throwing report handler costs the frame's device report, the shipped behavior item 15 states. No throw can leave a half-open listen.
- **The snapshot discipline holds.** One copy on the first `Activate` only, `ResetBindings` copies into the existing array (plant 11 guards the alias trap), a never-activated map gives `false`, `ReadBindings` does not move the snapshot, and the restore closes the listen with no commit and no slot call.
- **The shipped surface did not move.** `UiActionMap.Bindings.cs` is byte-identical to the base, the 18 `Bind*` signatures and the five queries stand, the scheme API changed no line, `InputKind` pins the nine numbers with a stored-contract remark, and no new top-level public type exists. The inherited items ship: `BindingText` and `TryReadBinding` (CR-5 of input-actions) and `UiScheme.Default` (CR-8 of control-schemes).
- **The writer and reader hold the load-time contract.** One line writer serves both passes, so the needed count and the written count agree exactly; a short span writes nothing; the empty list gives the 10 version bytes; each number goes to a stack buffer; the scan passes the raw state by `ref readonly` with no hidden 804-byte copy; the reader counts every bad-line class the doc names and throws nothing, including the fix-round distinct-key rule.
- **Project rules hold.** File-scoped namespaces, UTF-8 literals, `Brutal.Numerics`, XML docs on every new member with real crefs, and prose that follows the sibling shape. The settled readings (lowest gamepad bit, stick before axis, the `-1` reject code, `PushGamepad` store-then-return) are each stated and each pinned.

## Recommended Fix Order

| # | Finding | Severity | Impact | Effort | Why here |
|---|---------|----------|--------|--------|----------|
| 1 | CR-1 grow at capacity-exact empty-group commit | MEDIUM | High | Medium | It breaks the zero-allocation promise of the shipped cost rule on a normal rebind-menu state; fix before the epic close is trusted. |
| 2 | CR-2 name bytes break the round trip | MEDIUM | Medium | Low | Silent data loss is the class the quality bar names; the doc fence is two sentences and the follow-up guard is one line. |
| 3 | CR-3 reader stricter than the bind it cites | MEDIUM | Medium | Low | One sentence and one test line remove a false doc claim about the stored contract. |
| 4 | CR-4 `CancelRebind` call-site sentence | LOW | Low | Low | Three doc sentences; a game builds its handler model from this paragraph. |
| 5 | CR-5 sample "idle" heuristic | LOW | Low | Low | Sample-only, but the sample is the doc a game copies; fix with the pending windowed sequence. |
| 6 | CR-6 reserved `-` scheme name | LOW | Low | Low | Exotic name, needs a ruling on the Schemes.cs line; the item-16 sentence is the fallback. |

## Progress

- [x] CR-1: An empty-group commit on a capacity-exact array grows the binding array inside the frame read
- [x] CR-2: A name that holds a semicolon or a line feed writes a text that does not read back, and the loss is silent
- [x] CR-3: The reader's gamepad-button range is stricter than the bind, so a bound bit at or above 15 is lost on read-back
- [x] CR-4: "The map calls both inside `UiContext.Update(float)`" is contradicted by the synchronous report of `CancelRebind`
- [x] CR-5: The sample maps `RebindBound` with code 0 to "idle", and the remark's claim that a bound code is "32 or above" is false
- [x] CR-6: A scheme literally named `-` aliases the default token, and a write-and-read moves its bindings into scheme 0

## Fix Round

**Group 1 (CR-1), review round, 2026-11-10.** Fixed by the `game-dev-advanced` agent on branch `feat/input/binding-remap`, base `d2e0dcf`. No commit, no branch, no push.

**What changed.** `CaptureDefaults` now grows the live binding array at the first `Activate`, after the snapshot copy, when `_bindings.Length < _bindingCount + _actionCount * _schemeCount`. It replaces `_bindings` with one `new InputBinding[needed]`, copies the live entries with `Array.Copy`, and keeps `_bindingCount` unchanged. The snapshot array stays separate. `ResetBindings` still copies into the existing array. `CommitRebind` states the two-part free-room rule. `ActionMapAllocationTests.RebindStep` commits onto the kick action in the default scheme, whose group holds no binding, so the empty-group append runs inside the measured body. `BuildScene` asserts the post-`Activate` capacity, 32 bindings at capacity 110. `docs/technical-design.md` section 9 item 15 **The cost.** names the empty-group case, the build-time slack, and the scheme-reopen rule.

**Measured counts.** `dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. `dotnet test --solution brutal-ui.sln`: total 2988, failed 0, succeeded 2985, skipped 3. `--filter-class '*ActionMapAllocationTests'`: total 8, failed 0, `ARebindAndAResetAllocateNothing` at 0 bytes. `git diff d2e0dcf -- UiActionMap.Bindings.cs` is empty, and `grep -c 'public void Bind'` reads 18.

**Plant 19 (slack grow skipped in `CaptureDefaults`).** Builds with `0 Error(s)`. It fails `ARebindAndAResetAllocateNothing` and every other class test at the `BuildScene` capacity assert: `Expected: 110, Actual: 32`. The missing grow is exactly why the live array stands at 32. The assert therefore names the grow path. A recurring byte delta is not physically available for one net add per round: `Append` doubles `Array.Resize` once (32 to 64) in a warm-up round, and `ResetBindings` cannot shrink the array, so the 100 measured rounds fit in the 64 slot and read 0 bytes. The leg stays sharp: plant 19 turns the gate red. It reads at the capacity assert, not at a byte count. This corrects the brief's byte-count wording.

**Plant 20 (`GC.KeepAlive(new object());` in `CommitRebind`, after `Append`).** Builds with `0 Error(s)`. It fails `ARebindAndAResetAllocateNothing` and no other test, at the `Assert.Equal(0L, …)` byte assert, with **2400 bytes** (24 per object over 100 rounds). This proves the measured body still runs the commit on every round.

**Restore.** Each plant file was copied to `src/BrutalUi.Framework/obj/plantscratch/UiActionMap.Remap.cs.g1orig` first, restored with `cp`, and verified equal with `cmp`. The clean suite reads 2988, failed 0, after both plants.

**Group 2 (CR-2, CR-3, CR-6), review round, 2026-11-10.** Fixed by the `game-dev` agent on branch `feat/input/binding-remap`, base `d2e0dcf`. No commit, no branch, no push.

**What changed.** CR-2 ships as a doc fence plus a public-side pin. `UiActionMap.BindingsText.cs`: the `WriteBindings` remarks add **The name rule.**, which states that an action name and a scheme name hold no semicolon and no line feed, because the writer emits the name bytes unchanged. The `ReadBindings` remarks state the same rule from the reader side. No code line of that file changed for CR-2. `docs/technical-design.md` item 16 **The lines.** adds the one sentence with the consequence: a name that holds a separator gives a line that skips, so a game keeps `Applied` equal to `BindingCount` after a read. `BindingsRoundTripTests.ABadLineIsSkippedAndCounted` adds a public-side case: `AddAction` with the name `attack;heavy`, one bind, `WriteBindings`, and a read into a second map that gives `Applied` 0 and `Skipped` 1. The test remarks state that the guard belongs to `AddAction`, and that Definition-of-Done check I2 freezes `UiActionMap.Bindings.cs`, so the rule ships as a named contract, not as a throw; the follow-up is recorded in `context.md`. CR-3 keeps the strict reader and corrects the sentence: item 16 **The range rule.**, the `ReadBindings` range remark, and the `GamepadButtonBitLimit` doc now read that the parser runs at least the checks that the matching bind runs, and adds the 15-button cap for `device=2`, because the gamepad state holds buttons 0 to 14, so a file that holds a wider bound bit skips that line. `ABadLineIsSkippedAndCounted` adds the `BadText` line `action=Fire;scheme=Gamepad;device=2;code=65536`, and its expected `Skipped` moves from 14 to 15. CR-6 rejects the reserved name: `AddScheme` in `UiActionMap.Schemes.cs` now throws `ArgumentException` with parameter name `name` for a name equal to `-`, beside the empty-name check, with the reason in the remarks. `BindingTextWriterTests.TheWriterStartsWithTheVersionLineAndWritesOneLinePerBinding` pins the throw and shows that a scheme named `Default` still interns. The one-line widening of `Schemes.cs` is a stated deviation from the plan's File Tree, recorded in `context.md`.

**Measured counts.** `dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. `dotnet test --solution brutal-ui.sln`: total 2988, failed 0, succeeded 2985, skipped 3. `--filter-class '*BindingTextWriterTests'`: total 4, failed 0. `--filter-class '*BindingsRoundTripTests'`: total 4, failed 0, on the raised counts. `git diff d2e0dcf -- UiActionMap.Bindings.cs` is empty, and `grep -c 'public void Bind'` reads 18.

**Group 3 (CR-4, CR-5), review round, 2026-11-10.** Fixed by the `game-dev` agent on branch `feat/input/binding-remap`, base `d2e0dcf`. No commit, no branch, no push.

**What changed.** CR-4 ships as a doc fix, with no code line changed. `UiActionMap.Remap.cs`: the `RebindResultHandler` summary drops the inside-`Update` claim, and both delegate place rules now state the two call sites: the frame read calls the result slot for a cancel key, a reject, and a commit, and calls the conflict slot after a commit; a `CancelRebind` call reports through the result slot at the point of the call, which may sit outside `UiContext.Update(float)`. The result re-entry rule adds the consequence: outside the read the rule does not apply, and an `Update` call from the handler does not throw, so a handler that must know the place reads `IsActive` or keeps its own flag. `CancelRebind` gains the report rule that states the same point. `docs/technical-design.md` section 9 item 15 **The two slots.** states the same two call sites and the same consequence. **The cost.** stays as group 1 wrote it. CR-5 fixes the sample read. `HelloState.cs`: `public bool RebindDone { get; private set; }` joins the state, set in `OnRebind` beside the `RebindStatus` and `RebindCode` stores; the property remarks state that a code alone cannot tell an idle map from a mouse-button commit, because a mouse-button capture carries code 0 to 7. `HelloScreen.cs`: `RebindText` takes a `bool done` argument, the idle branch reads from `!done` in place of the `code != 0` test, the bound branch writes its code whatever it is, and the "32 or above" claim is gone; every text, the `LongestRebindText` seed, and each `InvalidOperationException` guard hold. `Program.cs`: the one `RebindText` call inside the `uiStartBytes`/`uiEndBytes` region passes `state.RebindDone`; `PushSmokeInput` is unchanged. `context.md` gains the windowed mouse-commit check in Needs Human Verification and records the group.

**Measured counts.** `dotnet build brutal-ui.sln -nodeReuse:false`: `0 Warning(s)`, `0 Error(s)`. `dotnet test --solution brutal-ui.sln`: total 2988, failed 0, succeeded 2985, skipped 3. Framework 326, Core 1801. No test moved, because no test builds the sample. `git diff d2e0dcf -- UiActionMap.Bindings.cs` is empty.

## Notes on Method

I read, in order: the review brief, `AGENTS.md` (session context), `docs/mission-statement.md`, `docs/technical-design.md` section 9 items 13 to 16 with the `### Action map` bullets and the frame order, section 14, the epic overview, the feature requirements, the plan Quality Bar, Public API, state machine, capture table, commit and conflict pseudocode, snapshot section, text format, Definition of Done F1 to F11, Q1 to Q6, I1 to I6, the Verification Method, and the Notes, `context.md` Quick Status, both `### Fix round 1` sections, the phase 5 "Rules stated from the code" and "Open questions", and Needs Human Verification, then the shipped code: `UiActionMap.Remap.cs` and `UiActionMap.BindingsText.cs` whole, the `UiActionMap.Read.cs`, `UiActionMap.cs`, `UiScheme.cs`, HelloPanel, and `ActionMapAllocationTests` diffs, `BindGamepadButton` and `Append` in `UiActionMap.Bindings.cs`, the capture and reset test setups, the siblings digest, and the control-schemes review for the finding shape. I checked each item-15 and item-16 sentence against the code, and each hunt class the brief names; I filed nothing the settled Decisions list already rules, and nothing the display-host tracking already holds. I ran no build, no test, and no plant. Where the fix reaches a file outside this feature's edited list (CR-6, and the CR-2 follow-up), the finding says so plainly instead of pretending the scope covers it.
