# input-routing/keyboard-focus - Code Review

**Reviewed:** 2026-10-22
**Reviewer:** Agent (cross-phase review of phases 1 to 6)
**Rules Applied:** 1. One owner, 2. The exact order, 3. The keys go to the owner, 4. The walk does not change, 5. Zero allocation after warm-up, 6. No recursion and no list, 7. No GLFW in Core, 8. Clean seams and a small public API, 9. No leak, 10. Test quality, 11. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 6 findings, 6 resolved (the verifier of the fixes gave PASS: `review-verify.md`)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 3 | CR-1, CR-2, CR-3 |
| LOW | 3 | CR-4, CR-5, CR-6 |

The feature is in good condition. The one-owner rule, the tree order, the key route, the click focus, the Tab step, the stale rule, and the adapter switch each have a test that kills the mutation of the rule. The public API matches the section "Public API (end state)" of the plan. No public member changed or went away. The internal members stay internal. `EventWalk.cs` keeps the SHA-256 `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`. `BrutalUi.Core` has no GLFW `using` and 2 `PackageReference` items. `FocusWalk.cs` has no recursion, no `List<`, and no LINQ. Each planted allocation fails a gate.

The review found no HIGH problem. Two rules have no test: the GLFW key action and modifier conversion of the real `HandleKey` (CR-1), and the rule that the Tab search does not see a hidden owner (CR-2). A `Focus` bit that code clears through `States` stays cleared while the element keeps the keys (CR-3). Three small gaps remain in `SetFocus`, in the stale rule, and in the docs (CR-4 to CR-6).

**Baseline (repository, before the probes):** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s). `dotnet test --solution brutal-ui.sln` gives total 2271, failed 0, succeeded 2269, skipped 2 (the two GLFW real-window tests, no display).

**Probe method.** The repository went to `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/9cde8ab4-a0c7-4298-9a82-daf2000f39aa/scratchpad/cr-keyboard-focus/repo` with `rsync -a --exclude bin --exclude obj --exclude .git`. The four source files and `Directory.Build.props` went to `../orig/` first. Each mutation sits in the copy behind its own `#if Mnn` symbol, and the copy appends `$(Mut)` to `DefineConstants`. So each run builds exactly one mutation (`-p:Mut=Mnn -p:TreatWarningsAsErrors=false`). Each run executes `BrutalUi.Core.Tests` and, under `xvfb-run -a`, `BrutalUi.Glfw.Tests` (no skip). The control run (`Mut=NONE`) gives Core 1439 tests with 0 failed and Glfw 20 tests with 0 failed and 0 skipped. After the last probe, `cp` restored the five files from `../orig/`.

**Result:** 58 probes. 52 fail one test or more. 6 fail no test: 2 equivalent mutants (M35, M36) and 4 real gaps (M44, M45, M47 for CR-1, M58 for CR-2).

**Probe list** (the failed tests of each mutation; the class prefix shows the file):

1. `SetFocusOwner`
   - M01 no early return for the same element: `FocusOwnerTests.SetFocusMovesTheFocusState`, `FocusStateEdgeTests.SetFocusOnTheOwnerMarksNothing`.
   - M02 no clear of `Focus` on the old owner: 24 Core tests (5 `ClickFocusTests`, 9 `TabNavigationTests`, 7 `FocusStateEdgeTests`, 2 `FocusOwnerTests`, `FocusAllocationTests.TheMeasuredBodyDoesRealWork`) and `GlfwUiInputKeyTests.ATabPressMovesTheFocus`, `GlfwUiInputKeyTests.AKeyAndACharReachTheFocusedElement`.
2. The key target step
   - M03 a key with no owner walks to the first root: 10 Core tests (among them `FocusOwnerTests.AKeyWithNoOwnerGetsNoWalk`, `TabNavigationTests.TabMovesTheFocusInTreeOrderAndWraps`, `EventAllocationTests.TheMeasuredBodyDoesRealWork`) and `GlfwUiInputKeyTests.ATabPressMovesTheFocus`, `GlfwUiInputTests.ARealWindowSubscribesAndUnsubscribes`.
   - M04 a caller target gets the owner in its place: `FocusOwnerTests.AKeyWithACallerTargetKeepsItsTarget`, `TabNavigationTests.ATabWithACallerTargetDoesNotMove`, 6 `EventKindTests`, `EventAllocationTests.TheMeasuredBodyDoesRealWork`.
   - M64 (extra) no write of the owner into `entry.Header.Target`: 5 Core tests and 6 Glfw tests (among them `FocusOwnerTests.AKeyWithNoTargetWalksToTheFocusOwner`).
3. `SetFocus`
   - M05 no `AcceptsFocus` check: `FocusOwnerTests.SetFocusRejectsAnElementThatIsNotFocusable`, `FocusStateEdgeTests.AnOwnerThatGetsDisabledLosesTheFocus`, `FocusStateEdgeTests.ADisabledElementTakesNoFocus`, `FocusStateEdgeTests.AnOwnerThatTurnsUnfocusableLosesTheFocus`.
   - M06 no `IsInRootTree` check: `FocusOwnerTests.SetFocusRejectsAnElementOutsideTheRoots`, `FocusStateEdgeTests.AFocusedElementThatLeavesTheTreeLosesTheFocus`, `FocusStateEdgeTests.ARemovedRootLosesTheFocus`.
   - M07 no `IsShown` check: `FocusStateEdgeTests.AnOwnerUnderAHiddenAncestorLosesTheFocus`.
4. `FocusOnPress`
   - M08 the hit only: `ClickFocusTests.APressOnAChildFocusesTheNearestFocusableAncestor`.
   - M09 the walk passes the first `IsFocusable` element to a higher ancestor that accepts focus: `FocusStateEdgeTests.ADisabledElementTakesNoFocus`.
   - M10 a press over nothing keeps the focus: `ClickFocusTests.APressOverNothingClearsTheFocus`, `FocusAllocationTests.TheMeasuredBodyDoesRealWork`.
   - M11 a caller target also runs the click focus: `ClickFocusTests.APressWithACallerTargetKeepsTheFocus`.
   - M12 the focus moves after the walk: `ClickFocusTests.APressOnAFocusableElementFocusesIt`, `ClickFocusTests.APressOnAChildFocusesTheNearestFocusableAncestor`.
5. `FocusWalk.Find`
   - M13 the roots in reverse order: `TabNavigationTests.TabCrossesTheRootsInTheOrderOfAdd`.
   - M14 the children in reverse order: 6 Core tests (among them `TabNavigationTests.TabMovesTheFocusInTreeOrderAndWraps`) and `GlfwUiInputKeyTests.ATabPressMovesTheFocus`.
   - M15 a parent after its children: 5 `TabNavigationTests` and `GlfwUiInputKeyTests.ATabPressMovesTheFocus`.
   - M16 no wrap (null after the current element): 9 Core tests (among them `TabNavigationTests.TabWithOneOrNoFocusableElement`) and `GlfwUiInputKeyTests.AKeyAndACharReachTheFocusedElement`.
   - M17 no `Display.None` skip: `TabNavigationTests.TabSkipsAHiddenSubtree`, `FocusAllocationTests.TheMeasuredBodyDoesRealWork`.
   - M18 no opacity skip in `Enter`: `TabNavigationTests.TabSkipsAHiddenSubtree`.
   - M19 NaN opacity counts as 0 (the shared clamp): `FocusStateEdgeTests.AnOwnerUnderAHiddenAncestorLosesTheFocus`.
   - M20 `current` does not count as a candidate: `TabNavigationTests.TabSkipsAHiddenSubtree`, `TabNavigationTests.TabWithOneOrNoFocusableElement`, `TabNavigationTests.ATreeDeeperThanTheStackFindsTheLeaf`.
   - M58 (extra, open risk 4) the walk marks a hidden owner as seen: 0 failed (finding CR-2).
6. M21 `FocusWalk.Find` with no clear of the stack in the `finally`: `TabNavigationTests.ATreeDeeperThanTheStackFindsTheLeaf`.
7. The Tab step
   - M22 it runs for a caller target: `TabNavigationTests.ATabWithACallerTargetDoesNotMove`.
   - M23 it runs for a `KeyUp`: `TabNavigationTests.ARepeatTabMovesAndAKeyUpDoesNot`, `GlfwUiInputKeyTests.ATabPressMovesTheFocus`.
   - M24 it ignores the stop flag: `TabNavigationTests.AHandlerThatStopsTheTabKeepsTheFocus`.
   - M25 `Control` does not block: `TabNavigationTests.TabWithControlAltOrSuperDoesNotMove`, `GlfwUiInputKeyTests.ATabPressMovesTheFocus`.
   - M26 `Shift` is ignored: 5 Core tests and `GlfwUiInputKeyTests.ATabPressMovesTheFocus`.
   - M27 a repeat does not move: `TabNavigationTests.ARepeatTabMovesAndAKeyUpDoesNot`, `GlfwUiInputKeyTests.ATabPressMovesTheFocus`.
   - M62 (extra) any key moves the focus: `FocusOwnerTests.AKeyWithNoOwnerGetsNoWalk`, `FocusStateEdgeTests.ASetFocusFromAHandlerRoutesTheNextKey`, 2 `GlfwUiInputKeyTests`.
8. M28 `DispatchQueued` returns `false` always: `TabNavigationTests.AHandlerThatStopsTheTabKeepsTheFocus`.
9. M29 `AcceptsFocus` ignores `Disabled`: `FocusStateEdgeTests.AnOwnerThatGetsDisabledLosesTheFocus`, `FocusStateEdgeTests.ADisabledElementTakesNoFocus`.
10. `IsShown`
    - M30 the element only: `FocusStateEdgeTests.AnOwnerUnderAHiddenAncestorLosesTheFocus`.
    - M31 no opacity check: `FocusStateEdgeTests.AnOwnerUnderAHiddenAncestorLosesTheFocus`.
11. `DropStaleFocus`
    - M32 the call removed: 5 `FocusStateEdgeTests`.
    - M33 no `IsShown` check: `FocusStateEdgeTests.AnOwnerUnderAHiddenAncestorLosesTheFocus`.
    - M34 no `IsInRootTree` check: `FocusStateEdgeTests.AFocusedElementThatLeavesTheTreeLosesTheFocus`, `FocusStateEdgeTests.ARemovedRootLosesTheFocus`.
    - M61 (extra) no `AcceptsFocus` check: `FocusStateEdgeTests.AnOwnerThatGetsDisabledLosesTheFocus`, `FocusStateEdgeTests.AnOwnerThatTurnsUnfocusableLosesTheFocus`.
12. The drop count of the drain
    - M35 the count covers the whole entry, also the Tab move: 0 failed. Equivalent mutant: `FocusNext`, `FocusPrevious`, and `RoutePointer` call no handler and push no event, so `_events.Dropped` cannot change outside the walk.
    - M36 the count moves out of the walk block: 0 failed. Equivalent mutant for the same reason: an entry with no walk runs no handler.
13. `GlfwUiInput`
    - M37 no `KeyboardEnabled` check in `PushKey`: `GlfwUiInputKeyTests.KeyboardEnabledFalseStopsTheKeyAndCharPushes`.
    - M38 no `KeyboardEnabled` check in `PushChar`: `GlfwUiInputKeyTests.KeyboardEnabledFalseStopsTheKeyAndCharPushes`.
    - M39 no `Dispose` guard in `PushKey`: `GlfwUiInputKeyTests.DisposeStopsTheKeyAndCharPushes`.
    - M40 no `Dispose` guard in `PushChar`: `GlfwUiInputKeyTests.DisposeStopsTheKeyAndCharPushes`.
    - M41 no `Rune.IsValid` check: `GlfwUiInputKeyTests.ACharThatIsNotAScalarValueIsDropped`.
    - M42 `PushKey`: a repeat gives `IsRepeat` false: `GlfwUiInputKeyTests.APressARepeatAndAReleaseGiveDownRepeatAndUp`.
    - M50 (extra) `PushKey`: a release keeps the repeat flag: `GlfwUiInputKeyTests.APressARepeatAndAReleaseGiveDownRepeatAndUp`.
    - M43 `PushKey`: a release gives `KeyDown`: 3 `GlfwUiInputKeyTests`.
    - M44 `HandleKey`: `Repeat` gives `IsRepeat` false: 0 failed (finding CR-1).
    - M45 `HandleKey`: `Release` gives `KeyDown`: 0 failed (finding CR-1).
    - M46 no `0x3F` mask in `ButtonModifiers`: `GlfwUiInputTests.APressAndAReleaseGiveDownAndUp`.
    - M47 `HandleKey` passes the GLFW bits with no mask: 0 failed (finding CR-1).
    - M48 `Dispose` does not unsubscribe `OnKey`: `GlfwUiInputTests.ARealWindowSubscribesAndUnsubscribes`.
    - M49 `Dispose` does not unsubscribe `OnChar`: `GlfwUiInputTests.ARealWindowSubscribesAndUnsubscribes`.
14. Planted allocation (`GC.KeepAlive(new object());` as the first line)
    - M52 `DropStaleFocus`: `FocusAllocationTests.KeyboardRoutingAllocatesNothingAfterWarmUp` (12592 bytes), `PointerAllocationTests.PointerRoutingAllocatesNothingAfterWarmUp`, `EventAllocationTests.PushAndDrainAllocateNothingAfterWarmUp`.
    - M51 `FocusOnPress`: `FocusAllocationTests.KeyboardRoutingAllocatesNothingAfterWarmUp` (4800 bytes), `PointerAllocationTests.PointerRoutingAllocatesNothingAfterWarmUp`.
    - M53 `PushKey`: `GlfwUiInputAllocationTests.TheAdapterHandlersAllocateNothing` (24000 bytes), `GlfwUiInputAllocationTests.TheRealHandlersAllocateNothing`.
    - M54 `HandleKey`: `GlfwUiInputAllocationTests.TheRealHandlersAllocateNothing` (24000 bytes). This gate needs a display. With no display it skips, so only `xvfb-run -a` or the Windows run catches this plant.

---

## Findings

### CR-1: No test checks the key action and modifier conversion of the real HandleKey
**Severity:** MEDIUM
**Rule:** 10. Test quality (also 3. The keys go to the owner)
**File:** `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs:369-384` (the code under test: `src/BrutalUi.Glfw/GlfwUiInput.cs:530-539`)
**Problem:** Decision 9 sets the conversion: `Release` gives `KeyUp`, `Press` gives `KeyDown`, `Repeat` gives `KeyDown` with `IsRepeat`, and the modifiers go through `ButtonModifiers` (the mask `0x3F`). `HandleKey` holds this conversion:

```csharp
PushKey(
    (int)key,
    scancode,
    action != GlfwKeyAction.Release,
    action == GlfwKeyAction.Repeat,
    ButtonModifiers(mods),
    GlfwApi.Time);
```

The `GlfwUiInputKeyTests` call `PushKey` with `down` and `repeat` already converted, so they do not reach these lines. The real-window test calls the real `OnKey` delegate with one `Press` and the modifiers 0. The real-handler gate counts bytes and drops only. Three mutations survive all 20 Glfw tests under `xvfb-run -a`: M44 (`Repeat` gives `IsRepeat` false), M45 (`Release` gives `KeyDown`), and M47 (no mask). With M45, each key release of a real window reaches the owner as a second key down. A `Button` of interactive-elements that acts on a key up never acts.
**Fix:** Extend `ARealWindowSubscribesAndUnsubscribes` after the Tab press. Call the real key delegate with `Repeat`, then `Release`, then `Press` with `(GlfwModifier)0xFF`, and drain after each call. Check the type, `IsRepeat`, and `Modifiers` of `recorder.LastKey`.

```csharp
key(window, GlfwKey.A, 30, GlfwKeyAction.Repeat, (GlfwModifier)0);
Assert.Equal(1, tree.Context.DrainEvents());
Assert.Equal(UiEventType.KeyDown, recorder.LastKey.Header.Type);
Assert.True(recorder.LastKey.IsRepeat);

key(window, GlfwKey.A, 30, GlfwKeyAction.Release, (GlfwModifier)0);
Assert.Equal(1, tree.Context.DrainEvents());
Assert.Equal(UiEventType.KeyUp, recorder.LastKey.Header.Type);
Assert.False(recorder.LastKey.IsRepeat);

key(window, GlfwKey.A, 30, GlfwKeyAction.Press, (GlfwModifier)0xFF);
Assert.Equal(1, tree.Context.DrainEvents());
Assert.Equal((UiModifiers)0x3F, recorder.LastKey.Modifiers);
```

This test skips with no display. A test with no window is better: move the conversion into an internal static method (for example `KeyArgs(GlfwKeyAction action, out bool down, out bool repeat)`) and test it in `GlfwUiInputKeyTests`. Then M44 and M45 fail on each machine.
**Effort:** Low

### CR-2: No test shows that the Tab search does not see a hidden owner
**Severity:** MEDIUM
**Rule:** 10. Test quality (also 2. The exact order)
**File:** `tests/BrutalUi.Core.Tests/Input/TabNavigationTests.cs:195-208`
**Problem:** The remarks of `FocusWalk` and of `FocusNext` state a rule: when the walk does not see the current element (hidden or outside the roots), Tab gives the first candidate and Shift+Tab gives the last candidate. M58 changes the walk so that it marks a hidden owner as seen. Then Tab gives the first candidate after the hidden owner, and Shift+Tab gives the last candidate before it. No test fails (open risk 4).

Two causes hide the change. First, the block "A hidden owner" of `TabSkipsAHiddenSubtree` pushes a Tab through the drain. Since phase 5, `DropStaleFocus` clears the hidden owner before the Tab, so the walk starts with no owner. The block now tests the stale rule, not the walk, and its comment is wrong. Second, in the tree of `PointerTree` with `Left`, `Right`, and `Inner` focusable, no shown candidate comes before the hidden owner, so both rules give the same element. The rule matters for a code call of `FocusNext` or `FocusPrevious` between two drains, which is the gamepad slot of the Phase 3 action map.
**Fix:** Add a case that calls `FocusNext` and `FocusPrevious` with no drain, and makes `Root` focusable, so that a candidate comes before the hidden owner:

```csharp
tree.Root.IsFocusable = true;     // the order: Root, Left, Right, Inner
tree.Left.IsFocusable = true;
tree.Right.IsFocusable = true;
tree.Settle();
Assert.True(tree.Context.SetFocus(tree.Left));
tree.Left.StyleStorage.Opacity = 0f;      // hide the owner only, after the update

tree.Context.FocusNext();                 // the walk does not see Left: the first candidate
AssertOwner(tree, tree.Root);             // M58 gives Right
```

Restore the owner and repeat with `FocusPrevious` (expect `Right`, M58 gives `Root`). Change the comment of the drain block to say that the stale rule clears the hidden owner first.
**Effort:** Low

### CR-3: A Focus bit that code clears through States does not come back
**Severity:** MEDIUM
**Rule:** 1. One owner (also 8. Clean seams)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:1317-1324` (`DropStaleFocus`) and `:1510-1528` (`SetFocusOwner`)
**Problem:** `Element.States` is public. Code that writes the whole value clears `Focus` and keeps the element in `FocusedElement` (open risk 1). The feature tests write `States` this way already (`tree.Right.States = PseudoStates.Disabled;` in `ADisabledElementTakesNoFocus`). A `Toggle` of interactive-elements that writes `States = PseudoStates.Checked` on the focused toggle loses its `:focus` look and still gets each key. Nothing repairs the bit: `SetFocus` on the same element returns `true` at once, because `SetFocusOwner` returns early, and the stale rule keeps an owner that accepts focus:

```csharp
Element? old = _focusOwner;
if (ReferenceEquals(old, next))
{
    return;
}
```

`Hover` has the same limit, but the hover owner changes at each pointer event, and the focus owner can stay for minutes. The plan (Phase 1 Results) asks the phase 6 docs to state this limit. No doc states it (CR-6).
**Fix:** Repair the bit when the owner stays. The `States` setter returns at once for an equal value, so the repair marks nothing when the bit is set, and `SetFocusOnTheOwnerMarksNothing` still passes:

```csharp
private void DropStaleFocus()
{
    if (_focusOwner is not { } owner)
    {
        return;
    }

    if (!owner.AcceptsFocus || !IsInRootTree(owner) || !FocusWalk.IsShown(owner))
    {
        SetFocusOwner(null);
    }
    else
    {
        owner.States |= PseudoStates.Focus;   // repair a bit that code cleared through States
    }
}
```

Also set the bit in the early-return path of `SetFocusOwner` when `next` is not null. Add a test: focus `Right`, write `Right.States = PseudoStates.None`, drain, and expect `Focus` on `Right` again. State the rule in the remarks of `FocusedElement`.
**Effort:** Low

### CR-4: SetFocus returns false for an owner that keeps the focus
**Severity:** LOW
**Rule:** 8. Clean seams and a small public API (also 11. The docs)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:769-783`
**Problem:** The `<returns>` doc of `SetFocus` says: "`true` when the element holds the focus after the call". P5-4: when the current owner turned hidden or disabled since the last drain, `SetFocus(owner)` returns `false` and does not clear the owner (open risk 11). The element then holds the focus after a call that returned `false`. A caller that reads the result and the state gets two answers until the next drain.
**Fix:** Pick one rule and make the doc and the code agree. The smaller change: clear the owner when the check fails for the current owner, so the result is true to its doc.

```csharp
if (!element.AcceptsFocus || !IsInRootTree(element) || !FocusWalk.IsShown(element))
{
    if (ReferenceEquals(element, _focusOwner))
    {
        SetFocusOwner(null);   // the stale rule would clear it at the next drain
    }

    return false;
}
```

Add one assert to `AnOwnerThatGetsDisabledLosesTheFocus` before the drain: `Assert.False(SetFocus(Right))` and `Assert.Null(FocusedElement)`. The other option: keep the code and change the `<returns>` text and the remarks.
**Effort:** Low

### CR-5: A key after a handler disables the owner still walks to the disabled owner
**Severity:** LOW
**Rule:** 3. The keys go to the owner
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:1185-1190`
**Problem:** The stale rule runs at the start of a drain only (open risk 9). A handler that sets `Disabled` or clears `IsFocusable` on the owner leaves the owner in place for the later key and char entries of the same drain. Example: a `TextField` that disables itself on Enter (submit) gets the chars that the user typed after the Enter in the same frame. The other two cases in the risk text do not occur. A removed owner gets no walk, because the walk step checks `IsInRootTree`. A handler cannot hide the owner during a drain, because the hidden rule reads the computed style of the last `Update`.
**Fix:** In the key branch of the target step, drop an owner that does not accept the focus. `AcceptsFocus` reads two fields and walks no parent, so the check costs nothing:

```csharp
else if (target is null
    && entry.Header.Type is UiEventType.KeyDown or UiEventType.KeyUp or UiEventType.Char)
{
    if (_focusOwner is { AcceptsFocus: false })
    {
        SetFocusOwner(null);
    }

    target = _focusOwner;
    entry.Header.Target = target;
}
```

Add a test: a `KeyDown` handler on `Left` sets `Disabled` on `Left`. A second key of the same drain gets no walk. Change the limit sentence in the `DrainEvents` remarks. Keep the plan decision (Decision 7) for the hidden and the root checks.
**Effort:** Low

### CR-6: The technical design misstates the stale limit and leaves out two focus limits
**Severity:** LOW
**Rule:** 11. `AGENTS.md` and the docs
**File:** `docs/technical-design.md:333-340` (section 5, "The keyboard step of the drain")
**Problem:** Three gaps in the doc that routing-gate and interactive-elements read:
1. Item 1 says: "When a handler disables, hides, or removes the owner, the later key entries of that drain still go to the owner." A removed owner gets no walk, and a hide takes effect only after the next `Update` (CR-5).
2. The doc does not state the limit of open risk 1: code that clears `Focus` through `States` leaves the element in `FocusedElement`. The plan asked phase 6 to keep this limit in the docs (Phase 1 Results). If CR-3 lands, state the repair rule instead.
3. The doc does not tell a game how to focus a field of a panel that it shows in the same frame. `SetFocus` reads the computed style of the last `Update`. A panel that was `Display.None` and that the game shows now is still hidden for `SetFocus`, so `SetFocus(nameField)` returns `false` until the next `Update`. This is the "Complex" persona flow of the plan (`ui.SetFocus(nameField)` when a dialog opens). The `SetFocus` remarks state the rule, but section 5 does not.
**Fix:** Change item 1 to: "When a handler disables the owner or clears `IsFocusable`, the later key entries of that drain still go to the owner. A removed owner gets no walk. A style change takes effect after the next `Update`." Add one sentence for each of the two limits after the `SetFocus` paragraph, for example: "A panel that the game shows in this frame stays hidden for `SetFocus` until the next `Update`. Call `SetFocus` after that `Update`."
**Effort:** Low

---

## What's Already Good

- **One owner (rule 1).** `SetFocusOwner` is the one path for each change, and M01 and M02 fail 2 and 26 tests. `AssertOwner` in `TabNavigationTests` checks that no other element of the tree holds `Focus` after each move.
- **The exact order (rule 2).** M13, M14, M15, and M16 each fail a test. Each wrap case, the cross-root case, and the single-candidate case have a test.
- **The keys go to the owner (rule 3).** M03, M04, M11, M22, and M64 each fail a test. The event-model tests still guard the caller target.
- **The walk does not change (rule 4).** `sha256sum src/BrutalUi.Core/Input/EventWalk.cs` gives the expected value. `DispatchQueued` returns the stop flag with no change of `EventWalk`.
- **Zero allocation (rule 5).** Each of the four plants of R.4 fails a gate. The measured bodies hold no lambda, no string, and no `Assert`. The handlers are method groups that the scene creates before the measure. No `foreach` over an interface, no boxing, and no closure is on the focus path.
- **No recursion and no list (rule 6).** `Find` calls `Enter` and `Visit` only. `IsShown` is one loop. `FocusWalk.cs` has no `List<` and no LINQ. The stack copies the `HitWalk` pattern, and the comment on the stale `ref` after a growth is correct.
- **No GLFW in Core (rule 7).** No GLFW `using` in `src/BrutalUi.Core`, and 2 `PackageReference` items. `TheCoreTabKeyEqualsTheGlfwTabKey` ties `TabKey` to `GlfwKey.Tab`.
- **The public API (rule 8).** The diff adds 7 public members, the same 7 as the plan: `IsFocusable`, `FocusedElement`, `SetFocus`, `ClearFocus`, `FocusNext`, `FocusPrevious`, and `KeyboardEnabled`. `FocusWalk`, `FocusFrame`, `AcceptsFocus`, `TabKey`, the two stack members, `PushKey`, and `PushChar` are internal. `PushEvent` keeps 4 public overloads.
- **No leak (rule 9).** The `finally` of `Find` clears the used frames (M21 fails `ATreeDeeperThanTheStackFindsTheLeaf`). The stale rule drops a removed owner at the next drain (M32 and M34 fail tests).
- **Demo tests (rule 10).** The six demo tests match their demo lines in `context.md`.
- **Open risk 2 (a press on a disabled focusable element clears the focus).** Not a finding. It is Decision 4 and the accepted answer of Notes Q3 (Decision R1). The remarks of `IsFocusable` and section 5 state it. M09 fails `ADisabledElementTakesNoFocus`, so a test guards the rule.
- **Open risk 3 (a full ring drops a `PointerDown` and its focus move).** Not a finding. It is the same ring limit as `Active`. `DroppedEventCount` shows a drop, and routing-gate drains in each `Update`, so the ring stays below its capacity.
- **Open risk 5 (P3-4, a Tab to an owner outside the roots moves to the first candidate).** Not a finding. The stale rule clears such an owner at the start of the drain, so the case needs a handler of the same drain that removes the owner. The result follows the documented rule for an owner that the search does not see.
- **Open risk 6 (the switch can split a key down and its key up).** Not a finding. The remarks of `KeyboardEnabled` and section 9 item 7 state it. Each focus move splits a pair in the same way (the key up of a Tab goes to the new owner), as in a browser. A key-state reset belongs to interactive-elements.
- **Open risk 7 (a code point above U+FFFF arrives cut).** Not a finding. The defect is in the BRUTAL binding. The adapter takes `uint`, so a fix in BRUTAL needs no change here. `context.md` holds the issue text, and the human-verification list holds the filing.
- **Open risk 8 (`Disabled` does not pass to the children).** Not a finding. Sections 5 and 9 state the limit, and `ADisabledElementTakesNoFocus` shows it. CSS `:disabled` does not pass to the children either. An inherited rule is a later feature.
- **Open risk 10 (P5-2, the two opacity product orders).** Not a finding. Both orders use the same `ClampOpacity`. A different answer needs a product near 1e-45, which no real style gives.
- **Open risks 1, 4, 9, and 11** are findings CR-3, CR-2, CR-5, and CR-4.

---

## Recommended Fix Order

| Order | Finding | Severity | Effort | Fix group (one file) | Why this order |
|-------|---------|----------|--------|----------------------|----------------|
| 1 | CR-1 | MEDIUM | Low | `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs` (or a new internal conversion method and `GlfwUiInputKeyTests.cs`) | A wrong key action reaches every focused widget, and no test guards it. |
| 2 | CR-3 | MEDIUM | Low | `src/BrutalUi.Core/Tree/UiContext.cs` (group A with CR-4 and CR-5) | interactive-elements writes `States` soon. One `if` per method repairs the bit. |
| 3 | CR-2 | MEDIUM | Low | `tests/BrutalUi.Core.Tests/Input/TabNavigationTests.cs` | One new case pins the hidden-owner rule for the gamepad slot. |
| 4 | CR-5 | LOW | Low | `src/BrutalUi.Core/Tree/UiContext.cs` (group A) | One field check in the key branch. |
| 5 | CR-4 | LOW | Low | `src/BrutalUi.Core/Tree/UiContext.cs` (group A) | One `if` in `SetFocus`, one assert. |
| 6 | CR-6 | LOW | Low | `docs/technical-design.md` | Write it after CR-3 and CR-5, so the text states the final rules. |

---

## Progress

- [x] CR-1: No test checks the key action and modifier conversion of the real HandleKey. Fix (`review-g2.md`): the internal `GlfwUiInput.KeyArgs` holds the action conversion, `APressARepeatAndAReleaseGiveDownRepeatAndUp` checks it with no window, and the real-window test checks a repeat, a release, and the `0xFF` mask. A `HandleKey` that skips `ButtonModifiers` still fails only with a display (`xvfb-run -a` or Windows).
- [x] CR-2: No test shows that the Tab search does not see a hidden owner. Fix (`review-g3.md`): `TabSkipsAHiddenSubtree` calls `FocusNext` and `FocusPrevious` with no drain on a hidden owner. M58 now fails it.
- [x] CR-3: A Focus bit that code clears through States does not come back. Fix (`review-g1.md`): `DropStaleFocus` and the early return of `SetFocusOwner` set the bit again. `SetFocusOnTheOwnerMarksNothing` guards both paths.
- [x] CR-4: SetFocus returns false for an owner that keeps the focus. Fix (`review-g1.md`): a failed `SetFocus` on the current owner clears the focus. `AnOwnerThatGetsDisabledLosesTheFocus` guards it.
- [x] CR-5: A key after a handler disables the owner still walks to the disabled owner. Fix (`review-g1.md`): the key and char branch drops an owner with `AcceptsFocus` false. `ASetFocusFromAHandlerRoutesTheNextKey` guards it.
- [x] CR-6: The technical design misstates the stale limit and leaves out two focus limits. Fix (`review-g4.md`): section 5 states the three rules.
