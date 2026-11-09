# input/input-actions - Code Review

**Status:** Closed - [13] findings, 13 resolved (11 fixed in review groups 1-4, 2 deferred with reason: CR-4 and CR-5 are the scopes of control-schemes and binding-remap)

**Review date:** 2026-11-09

**Reviewer:** Agent (project-rules cross-phase review)

**Branch:** `feat/input/input-actions`, diff from `main` plus the new files.

**Scope:** the Core read slot, the typed `UiActionMap`, its three partial files, and the five test classes. The review also covers the HelloPanel sample, section 4, section 9, section 14, and the changelog.

---

## Findings

### CR-1 [MEDIUM] `IsActive` and the frame read test different slots

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.cs:477`, `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs:204-207` and `:354`

**Problem:** `IsActive` reads only the unhandled slot. The frame read runs from the read slot, and the invoke pass stops only on the read slot. A game that writes one slot by hand, which `UiContext.OnUnhandledInput` invites, gets two broken states.

```csharp
// UiActionMap.cs:477
public bool IsActive => _context is { } c && c.OnUnhandledInput == _onInput;

// UiActionMap.Read.cs:354 — the invoke pass knows only the read slot
if (context.OnInputRead != _onRead)
{
    break;
}
```

Case A: the game replaces `OnUnhandledInput` after `Activate`. The map then reads `false` and zero through every query. But `ReadFrame` still runs and still invokes each gamepad press command. A command fires from a map that the API calls inactive.

Case B: the game sets `OnInputRead = null`. The map still reads `IsActive` as `true`. And each query returns the value of the last frame forever.

The switch rule promises that a map that is not active reads `false` or zero. Neither path holds that promise. `UiManager` writes both slots through `Activate`, so the mode path is safe today. The hole is a hand-written slot. And `UiContext.OnInputRead` is a public settable slot that the remarks tell a game to write with `=`.

**Fix:** start `ReadFrame` with the same test as `IsActive`, one reference compare, no allocation:

```csharp
if (context.OnUnhandledInput != _onInput)
{
    return;
}
```

State the rule in the class remarks and in item 13 of section 9. The read pass follows the unhandled slot. A game that takes that slot also stops the frame read. Add one test in `TypedActionMapTests`. Write `OnUnhandledInput` with a game handler after `Activate`. Then assert that a value action reads zero after an `Update`, and that a gamepad press invokes nothing.

**Effort:** Small (one guard, one remarks line, one test).

---

### CR-2 [MEDIUM] The mouse-button modifier guard breaks the press agreement

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs:208` and `:242-251`, `src/BrutalUi.Framework/Actions/UiActionMap.cs:52-59`

**Problem:** the frame read compares the modifiers of a mouse binding with `input.Keys.Modifiers`. That field holds the modifiers of the last *key* event. The `UiPointerButtonState` type holds no modifiers. So the slot path resolves a mouse press with the modifiers of the *pointer* event. The two paths therefore disagree in both directions for a mouse bind with a modifier.

```csharp
// Read.cs:208
UiModifiers mods = input.Keys.Modifiers & MatchMask;

// Read.cs:244 — the mouse case uses the key modifiers
case InputKind.MouseButton:
{
    if (mods == binding.Modifiers)
```

Case A: `BindMouseButton(Fire, Right, Control)`. A Ctrl+right press runs the command through the slot (the event carries Control), but `Keys.Modifiers` reads `None`, so `WasPressed("Fire")` reads `false`.

Case B: `BindMouseButton(Fire, Right)` with no modifier. A Ctrl+right press runs no command (`Find` compares the event modifiers), but the frame read sets `WasPressed` to `true`. The class remarks state the opposite: "the queries and the commands agree on one press (A4)". Section 9 states the same rule. No test covers a mouse bind with a modifier: `AMouseButtonOverNoElementInvokesOneTimeAndReadsOnePress` uses `UiModifiers.None`, and every `Pointer(...)` helper in the test classes passes `Modifiers = UiModifiers.None`.

**Fix:** the state cannot give the modifiers of a press. The frame read cannot fix the rule without a per-press store. Decision 8 and A4 reject that store.

Do two things. First, state the limit next to the gate limit. Put it in the press rule of the class remarks and in the **The press rule.** bullet of section 9. A mouse binding with modifiers matches on the key modifiers of the frame. So a mouse press with a modifier can run its command with no press flag. Or it can set the flag with no command.

Second, pin both directions in a test in `ActionQueryTests`. Use `BindMouseButton(..., UiModifiers.Control)` and a `Pointer` event that carries Control. That way the next feature cannot read the current behavior as agreement.

If the project wants real agreement, the smaller change ignores modifiers for a mouse bind in the frame read. Say so in the type rule.

**Effort:** Small (remarks plus two asserts), Medium if the guard changes.

---

### CR-3 [MEDIUM] The duplicate check of a composite is position-sensitive

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:323`, `:527`, `:716-734`

**Problem:** `FindValue` compares the four codes by position, so a permuted composite is a new binding. Two bindings of one input set then live in one map, and the frame read sums both.

```csharp
// Bindings.cs:527 — position compare only
if (FindValue(InputKind.KeyAxis, negative, positive, -1, -1) >= 0)

// Bindings.cs:716-734
if (binding.Kind == kind && binding.Code == code && binding.Code1 == code1
    && binding.Code2 == code2 && binding.Code3 == code3)
```

Case A: `BindKeyAxis(Throttle, 83, 87)` then `BindKeyAxis(Throttle, 87, 83)` binds twice. Each frame then sums `+1` and `-1`, so `Throttle` reads 0 whatever the keys. Case B: `BindKeyVector(Move, W, A, S, D)` then `BindKeyVector(Move, W, D, S, A)` binds twice, and the X sum doubles. Nothing throws, and no test pins the case.

The plan states the rule as "the same kind, the same codes, and the same modifiers" (Decision 13). That reads as a set. The binding-remap feature writes and parses one text line per binding. So a line whose codes sit in another order silently becomes a second binding. That hides the conflict that the remap must report.

**Fix:** compare the code set, not the code order. For `KeyAxis`, test both orders. For `KeyVector`, sort the four codes into a small stack-ordered span and compare that. Or reject a bind whose code set intersects an existing bind of the same kind. Keep the scale and the action out of the check, as today. Add two asserts to `TheButtonQueriesCheckTheType` or `BindKeyVectorChecksTheKeys` for the reversed pair and the swapped pair.

**Effort:** Small.

---

### CR-4 [MEDIUM] The bind surface is scheme-blind, which control-schemes must widen

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:114`, `:164`, `:212`, `:279`, `:370`, `:435`, `:503`, `:576`, `:637`, `:674-688`, `:716-734`

**Problem:** `InputBinding` gains one field for a scheme, which is additive. The two duplicate finds and the eleven public binds are the harder part. The control-schemes feature needs the conflict rule per scheme. So `Find` and `FindValue` must compare a scheme, and each public bind must say which scheme it binds into.

No optional parameter can carry that today, because each bind ends with a scale or with a key list. The `modifiers` parameter already sits in the middle of `BindKey` and `BindMouseButton`. A new overload for each of the nine binds, or a breaking parameter, is the likely result.

```csharp
public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers = UiModifiers.None)
// Find(kind, code, modifiers) — map-wide, no scheme
```

**Fix:** settle the shape now, while one review can still cheaply change it. The small option: give `InputBinding` a `Scheme` field. A zero means the default scheme. Give `Find` and `FindValue` a scheme parameter. Add one pattern to the binds: a trailing `UiModifiers modifiers` plus a scheme id on each new overload set. Keep the pattern out of the eleven current signatures.

Then control-schemes adds the scheme overloads and keeps the current ones. Record the choice in the Key Technical Decisions of the plan and in `context.md`. The next planner then inherits it instead of discovering it.

The alternative is to leave it and let control-schemes widen each bind. That costs nine new overloads or a break later.

**Effort:** Small now as a decision, Medium as code.

---

### CR-5 [LOW] Nothing addresses one binding, so binding-remap has no handle to replace

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.cs:619-658`, `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs` (no unbind member)

**Problem:** the shape of one binding holds what one text line needs: kind, four codes, modifiers, scale, action. The API gives no way to reach one binding: `BindingCount` exists, and no member reads, replaces, or removes binding `i`. The `InputBinding.Action` field stores an index. So a remap that rewrites one line must find the index of that binding by its codes. Then it writes the array, all inside the class.

A conflict report also needs the name of the action that already owns the input. The `FindValue` helper throws that name away. It returns the binding index, and each bind discards it and throws a bare `ArgumentException`.

**Fix:** the API needs no public change today. Add two internal hooks in the same file, next to `FindValue`. The binding-remap feature then reads them instead of inventing them. Return `int FindBindingIndex(...)` to the caller. Add one internal member that reads one binding into a small struct. Name the gap in the Integration Points row of the plan for binding-remap, so the next planner budgets it.

**Effort:** Small.

---

### CR-6 [LOW] `BindKey` and `BindMouseButton` accept a code above the state range

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:116-119`, `:166-169`, against `:281-299` and `:505-513`

**Problem:** the two single-input binds check only the low end, while the two composites check `0` to `UiKeyState.KeyCount - 1`. The `UiKeyState.Holds` read guards the range and returns `false` for a large code. So a bad bind lives in the map and never fires.

```csharp
if (key < 0)                                   // BindKey: no upper bound
if ((int)button < 0)                           // BindMouseButton: no upper bound; ButtonCount is 8
if ((uint)up >= UiKeyState.KeyCount)           // BindKeyVector: full range
```

The plan kept the old rule for `BindKey`, so this is not a break. It is a forward risk. The binding-remap feature parses codes out of a text line. So one bad number produces a dead binding instead of a throw.

**Fix:** tighten both to the same rule as the composites: `(uint)key >= UiKeyState.KeyCount` for `BindKey`, and `(int)button < 0 || (int)button >= UiPointerButtonState.ButtonCount` for `BindMouseButton`. Add the two cases to `TheBuildChecksTheActionType` or a sibling test. Note the tightening in the changelog Changed section as a build-rule change.

**Effort:** Small.

---

### CR-7 [LOW] The double-push footgun has no test, and the flagship gate shows the forbidden pattern

**File:** `tests/BrutalUi.Framework.Tests/Actions/ActionMapAllocationTests.cs:377-384`, `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs:29-51`

**Problem:** `GamepadStep` calls `PushGamepad` on a map that `BuildScene` activated, and `TheMeasuredBodyDoesRealWork` asserts exactly one call. The one call holds only because no test writes the `GameInput` pad slot in that step. So `pad.Pressed` reads `None`, and the frame read invokes nothing.

```csharp
scene.Map.PushGamepad(UiGamepadButtons.A | UiGamepadButtons.DpadDown);
scene.Map.PushGamepad(UiGamepadButtons.None);
...
scene.Context.Update(0.016f);
```

A real game runs `PollGamepads`, so the state holds the edge. The same press then runs the command two times. That is the risk that the plan rates Medium and pushes onto the remarks.

A person who copies the gate, the sample pattern a reader trusts, keeps the push. Decision 9 rejects a guard inside `PushGamepad`, and that choice is sound. The missing piece is a test that pins the double invoke. That test makes the behavior a matter of proof, not of reasoning.

**Fix:** add one test to `GamepadValueActionTests`. Start with an active map with `Cancel` on B. In one frame, run `SetGamepad(0, B)` and `PushGamepad(B)`. Assert two calls on the counter. And assert that `WasPressed` reads `true` once. Name that test in the `PushGamepad` remarks as the proof of the warning.

**Effort:** Small.

---

### CR-8 [LOW] Section 14 credits a check to a class that does not hold it

**File:** `docs/technical-design.md:1385`

**Problem:** the **Typed action tests.** bullet says of `ActionMapSwitchTests`: "It checks a mode switch through the manager, a replaced map, and a type check on a map that is not active." The class holds six tests, and none throws a wrong-type query. The guard order that the sentence describes (type check, then `IsActive`) has a pin only in `TypedActionMapTests.ReadVectorChecksTheType`, and that covers `ReadVector` alone. No test checks the wrong type on an inactive map for the other four queries.

**Fix:** move the phrase to the `TypedActionMapTests` sentence. Add one assert per remaining query on the replaced map to `ActionMapSwitchTests.AReplacedMapReadsZero`, for example `Assert.Throws<ArgumentException>(() => mapA.ReadAxis(MoveAction))`. Section 14 then reads true without a doc-only fix.

**Effort:** Trivial.

---

### CR-9 [LOW] The invoke pass never gets checked against `GamepadSlot`

**File:** `tests/BrutalUi.Framework.Tests/Actions/GamepadValueActionTests.cs:172-198`, `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs:207` and `:344-360`

**Problem:** `TheGamepadSlotSelectsThePad` reads values only, and plant 7 (`ReadFrame` reads `input.Gamepad(0)`) failed that one test. The invoke pass shares the same `pad` reference. So the same mistake in the press path has no guard. A future split of the two references also has no guard.

**Fix:** extend the test with `map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B)`, `GamepadSlot = 2`, a `SetGamepad(2, B)` and a `SetGamepad(0, B)`. Assert one call from slot 2 and none from slot 0.

**Effort:** Trivial.

---

### CR-10 [LOW] A modifier press during a held key drops the action, and the latch blocks its return

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Read.cs:229-237`, `implementation.md:453` (Decision 8), `UiActionMap.cs:52-59`

**Problem:** the guard sits outside the down read. A frame whose key-event modifiers differ from the bind contributes nothing to `Down`. With `BindKey(Fire, Space)` (no modifier), Space held and Control pressed gives `mods == Control`. So `Down` reads `false` and `WasReleased` reads `true` while Space is still down.

When Control goes up, `mods` reads `None` and Space is still down. So `Down` reads `true` again. But `press` reads `false`, so `start` reads `false` and `IsHeld` stays `false` until the player releases and presses Space again.

```csharp
case InputKind.Key:
{
    if (mods == binding.Modifiers)   // one modifier event drops the held state
    {
        a.Down |= keys.IsDown(binding.Code);
```

This follows the value table in the plan, so the code is not wrong against the rules. The rules are the risk. A run of the game can show a fire that stops while its key stays down. No test pins it. The test `TheModifierRuleAppliesToTheQueries` releases the key instead.

**Fix:** choose one. Option A, small and inside the frame read: keep the modifier guard on the press edge. And read `IsDown` outside it. Then a held key keeps its hold through a modifier press and gives no new press. Option B: keep the rule and state it in the modifier rule of the class remarks and of section 9. Also add one test that pins the drop and the no-return.

**Effort:** Small for B, Medium for A (one remarks line, one value-table line, and one existing test to grow).

---

### CR-11 [LOW] `CheckActionType` hard-codes the parameter name

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:742-748`

**Problem:** the helper names the parameter of the caller as a string literal. The rule in the plan is "in throws, name the parameter," and every other check in the file uses `nameof`. A rename of the public `action` parameter then leaves a wrong `ParamName` with no compile error. The six tests that assert `"action"` fail as one block for the wrong reason.

```csharp
throw new ArgumentException("The action is not the type that this bind needs.", "action");
```

**Fix:** hold one `private const string ActionParameter = "action";` next to `MatchMask` in `UiActionMap.cs`. Use it in the helper. Also add one line that names it the public parameter name of every bind and query.

**Effort:** Trivial.

---

### CR-12 [LOW] `UiActions.cs` sits in the plan's do-not-change list and changed

**File:** `docs/features/input/input-actions/implementation.md:132`, `src/BrutalUi.Framework/Actions/UiActions.cs:10-13`

**Problem:** the File Tree section names `UiActions.cs` among the files that do not change. The new `AddAction` overload made its `<see cref="UiActionMap.AddAction"/>` ambiguous (CS0419, an error under `TreatWarningsAsErrors`), so one doc line now names both overloads. The `context.md` file records the deviation (phase 1 group 2, decision 10). The plan body still carries the old list, so the next scope gate reads the change as an out-of-scope edit.

**Fix:** add the exception to the do-not-change line: "`UiActions.cs` holds one doc-cref change for the two `AddAction` overloads."

**Effort:** Trivial.

---

### CR-13 [LOW] The two `AddAction` overloads meet at `default`

**File:** `src/BrutalUi.Framework/Actions/UiActionMap.Bindings.cs:22`, `:72`

**Problem:** the overload pair is clear for a name and a type, and for a name and a command. It is ambiguous for a literal `default`: `map.AddAction("x"u8, default)` gives CS0121, because `default` converts to both `ReadOnlySpan{byte}` and `UiActionType`. A game that writes `AddAction(name, default)` for "no command" gets a compile error with no hint toward the typed overload.

**Fix:** state the pair in one sentence in each summary. The first form, `AddAction(action, command)`, adds a Button with a command. The second form, `AddAction(action, type)`, adds an action of that type with no command. The same sentence then names `UiActionType.Button` as the way to add a Button with no command.

**Effort:** Trivial.

---

## What Is Already Good

- **The value rules match the code and the docs**. The `KeyVector` read maps `Code`..`Code3` to up, left, down, and right, exactly as the table says. The finish of a Vector2 clamps the length of the clamped sum, then adds the free sum. The `ClampUnit` and `ClampLength` helpers are small, private, static, and allocate nothing. Section 9 repeats both formulas and the whole table without a drift.

- **The Y signs are right and consistent**. The stick read flips through `new float2(stick.X, -stick.Y)`. The axis read negates only `LeftStickY` and `RightStickY`. A trigger keeps its sign, and the pointer delta keeps its Y-down coordinates. Decision 11 of gamepad-axes leaves the flip to this feature. The flip lands once, in the map.

- **One press runs one command on the paths that the plan pins**. The `ReadFrame` pass invokes only from `pad.Pressed`. And the `HandleInput` path handles only a key down and a pointer down. So a key press and a mouse press have exactly one invoker. Three tests pin the agreement for the three button kinds, and each counts both the command and the flag.

- **The invoke pass keeps its stop rule and its value order**. The finish runs before the pass. And the pass breaks on `context.OnInputRead != _onRead`. So a command that deactivates the map stops the pass. A command that throws still leaves the values of this frame in the map. The `TheInvokePassStopsAndKeepsTheValues` test pins both halves.

- **The switch rule covers both paths**. The `Activate` call captures `wasActive` before any write and resets after the writes. And the `Deactivate` call clears a slot only when that slot holds its own handler. The replaced-map case, which `UiManager` never deactivates, reads `false` and zero through the query guard. The `UiManager.cs` file shows an empty diff, and `AModeSwitchThroughTheManagerGivesNoStaleFlag` builds a real manager.

- **The frame path allocates nothing, and the gates see it**. The `ReadFrame` copies the two arrays and the two counts to locals. It takes `ref readonly` reads of the state, and calls no `new` of any array. That `grep -nE 'new [A-Za-z0-9_]+\[|\block\b|\bevent\b|ToString\(|\(object\)|=>|List<' ` run over `UiActionMap.Read.cs` returns only two expression-bodied static helpers, so Q6 holds. The `ActionMapAllocationTests.QueryStep` drives the read plus all five queries. And the recorded plants 19 and 2 give byte counts, not guesses.

- **The integration contracts hold**. The files `UiInputState.cs`, `InputStateWriter.cs`, `UiGamepadState.cs`, and each `src/BrutalUi.Glfw/` file show no diff. Both `PushGamepad` and `HandleInput` restore identical against `main` on a line-by-line compare, so only the remarks grew. The read uses `GameInput` only. The `HelloPanel` sample drops the push and reads `ReadVector` inside the UI region, where the sample gate measures it.

- **The Core slot change is small and its guard clears**. The `git diff main -- UiContext.cs` output holds the flag, one re-entry check, the slot call between `DrainEvents()` and `UpdateBindings()`, and the remarks. The `finally` clears the flag, and `AnUpdateFromTheSlotThrows` proves that a nested `Update` cannot wedge the context.

- **The blast radius matches the plan**. Every file in the diff appears in the File Tree of the plan. The counts line up with the DoD. The `BrutalUi.Core.Tests` project gains 6, and the `BrutalUi.Framework.Tests` project gains 34 (9, 8, 10, 6, and 1). And the gate scene holds 23 actions and 28 bindings as the docs claim. The `ActionMapSwitchTests` class re-ran green at 6 with `failed: 0` for this review.

- **The throws name their parameters and keep their counts**. Each bind checks its arguments, then the name, then the type, then the duplicate. And no rejected call moves `ActionCount` or `BindingCount`. The `BindKeyVectorChecksTheKeys` test names each key parameter by position. And the `type`, `stick`, `axis`, `scale`, and `value` names each appear in a test.

- **`AddAction(action, type)` keeps every old caller,** and `Invoke` skips an empty command before the registry, so a commandless Button cannot reach `UnknownCommand`. The `ACommandlessButtonInvokesNothing` test pins the skip.

- **Docs fidelity is close to exact**. The gate limit (Notes conflict 2, option A) sits in the class remarks. It also sits in the press rule of section 9. Section 4 names the read slot in item 2 and in the overview diagram, so both agree. The plant record is honest. It keeps the exit 0 of plant 15 as a stated deviation instead of hiding it.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Why here |
|---|---|---|---|---|
| 1 | CR-1 | High | Small | One guard makes `IsActive` the single truth for the read, the values, and the invokes. It closes a live behavior hole. |
| 2 | CR-3 | High | Small | The order-sensitive duplicate check lets one input bind twice and quietly cancels or doubles a value. Fix before binding-remap builds on the line format. |
| 3 | CR-6 | Medium | Small | Same code path as CR-3, one range line each. It removes the dead-binding case that remap parsing will hit. |
| 4 | CR-2 | Medium | Small | A stated limit plus two pinned tests. The frame read cannot recover the press modifiers, so write the rule down before control-schemes reads it as agreement. |
| 5 | CR-4 | Medium | Small (decision) | control-schemes starts next. Settling the scheme shape now avoids nine overloads or a break in one feature. |
| 6 | CR-7 | Medium | Small | One test makes the double-push risk a measured fact instead of a remark. It also protects the reader who copies the gate. |
| 7 | CR-10 | Medium | Small or Medium | A gameplay-visible case. Pick A or B, then pin it. |
| 8 | CR-8 | Low | Trivial | One wrong sentence in section 14, and one assert makes it true. |
| 9 | CR-9 | Low | Trivial | Three lines extend a test that already builds the slot case. |
| 10 | CR-5 | Low | Small | Internal hooks only. A note now and code in binding-remap. |
| 11 | CR-11 | Low | Trivial | One constant, no behavior change. |
| 12 | CR-12 | Low | Trivial | One line in the plan, so the scope gate stops flagging `UiActions.cs`. |
| 13 | CR-13 | Low | Trivial | Two summary sentences. |

CR-1, CR-3, CR-6, CR-9, CR-11, CR-12, and CR-13 change no public signature. CR-2, CR-4, CR-5, and CR-10 need a decision before code. CR-8 and CR-7 add tests only.

---

## Progress

- [x] CR-1: `IsActive` and the frame read test different slots

- [x] CR-2: The mouse-button modifier guard breaks the press agreement

- [x] CR-3: The duplicate check of a composite is position-sensitive

- [x] CR-4: The bind surface is scheme-blind, which control-schemes must widen | done: not fixed here. The widening of eleven binds sits in the scope of control-schemes (requirements, Out of Scope). The plan for control-schemes inherits this note. The binding shape already carries kind, four codes, modifiers, and a scale. And CR-3 made the duplicate rule set-based, which a per-scheme conflict rule can reuse.

- [x] CR-5: Nothing addresses one binding, so binding-remap has no handle to replace | done: not fixed here. The internal binding-index read and the conflict-name hook sit in the scope of binding-remap (requirements, Out of Scope). The plan for binding-remap inherits this note. The text-line format it must write already matches `InputBinding` one to one.

- [x] CR-6: `BindKey` and `BindMouseButton` accept a code above the state range

- [x] CR-7: The double-push footgun has no test, and the flagship gate shows the forbidden pattern

- [x] CR-8: Section 14 credits a check to a class that does not hold it

- [x] CR-9: The invoke pass never gets checked against `GamepadSlot`

- [x] CR-10: A modifier press during a held key drops the action, and the latch blocks its return

- [x] CR-11: `CheckActionType` hard-codes the parameter name

- [x] CR-12: `UiActions.cs` sits in the plan's do-not-change list and changed

- [x] CR-13: The two `AddAction` overloads meet at `default`

---

## Notes on Method

- Read only. No source file changed. `ActionMapSwitchTests` re-ran from the built binary: total 6, failed 0, 558ms.

- `diff` against `git show main:` proved the identity of the `PushGamepad` body and of `HandleInput`, and the empty diff of `UiManager.cs`.

- Plant 15 (a `Deactivate` reset that no public read can observe) is a known item, and `context.md` records it. It appears here only as the pattern behind CR-7 and CR-9: rules that the guards hide from the tests. The review found no further cannot-fail test among the 39 new tests.

- The window-host items stay in Needs Human Verification in `context.md`. They cover the `scripts/test-linux.sh` PASS run, the plants 10 and 20 byte measures, and the `Move: 0.00, 1.00` demo. This review cannot confirm F9.
