using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the Tab and the Shift+Tab moves: the tree order, the wrap, the skip rules of the focus walk, the stop of a
/// handler, the modifier rule, the key types, and the explicit stack of the walk.
/// </summary>
/// <remarks>
/// The tree order of <see cref="PointerTree"/> is <see cref="PointerTree.Root"/>, <see cref="PointerTree.Left"/>,
/// <see cref="PointerTree.Right"/>, and <see cref="PointerTree.Inner"/>. The class table is global and xUnit runs test
/// classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class TabNavigationTests
{
    /// <summary>
    /// The demo of phase 3. <see cref="PointerTree.Left"/>, <see cref="PointerTree.Right"/>, and
    /// <see cref="PointerTree.Inner"/> are focusable. Four Tab key downs with no target move the focus to
    /// <see cref="PointerTree.Left"/>, <see cref="PointerTree.Right"/>, <see cref="PointerTree.Inner"/>, and
    /// <see cref="PointerTree.Left"/> again. A Shift+Tab then gives <see cref="PointerTree.Inner"/>.
    /// </summary>
    [Fact]
    public void TabMovesTheFocusInTreeOrderAndWraps()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(TabMovesTheFocusInTreeOrderAndWraps));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Inner.IsFocusable = true;
        tree.Settle();
        Assert.Null(tree.Context.FocusedElement);

        // A Tab with no owner gets no walk, but it still moves the focus.
        Assert.Equal(0, PushTab(tree, shift: false));
        AssertOwner(tree, tree.Left);

        Assert.Equal(1, PushTab(tree, shift: false));
        AssertOwner(tree, tree.Right);

        Assert.Equal(1, PushTab(tree, shift: false));
        AssertOwner(tree, tree.Inner);

        Assert.Equal(1, PushTab(tree, shift: false));
        AssertOwner(tree, tree.Left);

        Assert.Equal(1, PushTab(tree, shift: true));
        AssertOwner(tree, tree.Inner);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "owner " + tree.Context.FocusedElement?.Name + " " + tree.Inner.States);
    }

    /// <summary>
    /// Shift+Tab with no owner gives the last candidate, then moves back in the tree order and wraps to the last
    /// candidate again.
    /// </summary>
    [Fact]
    public void ShiftTabMovesBackAndWraps()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(ShiftTabMovesBackAndWraps));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Inner.IsFocusable = true;
        tree.Settle();

        Assert.Equal(0, PushTab(tree, shift: true));
        AssertOwner(tree, tree.Inner);

        Assert.Equal(1, PushTab(tree, shift: true));
        AssertOwner(tree, tree.Right);

        Assert.Equal(1, PushTab(tree, shift: true));
        AssertOwner(tree, tree.Left);

        Assert.Equal(1, PushTab(tree, shift: true));
        AssertOwner(tree, tree.Inner);
    }

    /// <summary>
    /// An element that is not focusable is not a candidate, but the walk still visits its children. The root and
    /// <see cref="PointerTree.Right"/> are not focusable, so Tab goes from <see cref="PointerTree.Left"/> to
    /// <see cref="PointerTree.Inner"/>.
    /// </summary>
    [Fact]
    public void TabSkipsAnElementThatIsNotFocusable()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(TabSkipsAnElementThatIsNotFocusable));
        tree.Left.IsFocusable = true;
        tree.Inner.IsFocusable = true;
        tree.Settle();

        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Left);

        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Inner);

        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Left);

        PushTab(tree, shift: true);
        AssertOwner(tree, tree.Inner);

        PushTab(tree, shift: true);
        AssertOwner(tree, tree.Left);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Root.States);
    }

    /// <summary>
    /// Two roots. The walk visits the roots in the order of add, so Tab goes from the last candidate of the first root
    /// to the first candidate of the second root, and from the last candidate of the second root back to the first
    /// root.
    /// </summary>
    [Fact]
    public void TabCrossesTheRootsInTheOrderOfAdd()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(TabCrossesTheRootsInTheOrderOfAdd));
        Element second = new() { Name = "second" };
        second.Inline.Width = 100;
        second.Inline.Height = 100;
        Element secondChild = new() { Name = "secondChild" };
        secondChild.Inline.Width = 50;
        secondChild.Inline.Height = 50;
        second.Children.Add(secondChild);
        tree.Context.AddRoot(second);

        tree.Right.IsFocusable = true;
        second.IsFocusable = true;
        secondChild.IsFocusable = true;
        tree.Settle();

        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Right);

        PushTab(tree, shift: false);
        AssertOwner(tree, second);

        PushTab(tree, shift: false);
        AssertOwner(tree, secondChild);

        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Right);

        PushTab(tree, shift: true);
        AssertOwner(tree, secondChild);
        Assert.Equal(PseudoStates.None, tree.Right.States);

        PushTab(tree, shift: true);
        AssertOwner(tree, second);
        Assert.Equal(PseudoStates.None, secondChild.States);
    }

    /// <summary>
    /// A subtree with <see cref="Display.None"/> or an opacity of 0 is not a candidate. The test writes the style after
    /// the update, so the style pass does not overwrite it before the walk. A hidden owner counts as an owner that the
    /// walk does not see. An element that is not pickable stays a candidate.
    /// </summary>
    [Fact]
    public void TabSkipsAHiddenSubtree()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(TabSkipsAHiddenSubtree));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Inner.IsFocusable = true;
        tree.Settle();
        Display shown = tree.Right.StyleStorage.Display;
        Assert.NotEqual(Display.None, shown);

        // Display.None on Right hides Right and Inner. Left is the only candidate, so the owner stays.
        tree.Right.StyleStorage.Display = Display.None;
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Left);
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Left);
        PushTab(tree, shift: true);
        AssertOwner(tree, tree.Left);

        // An opacity of 0 on Right hides the subtree in the same way.
        tree.Right.StyleStorage.Display = shown;
        tree.Right.StyleStorage.Opacity = 0f;
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Left);
        PushTab(tree, shift: true);
        AssertOwner(tree, tree.Left);

        // An opacity of 0 on Inner hides Inner only.
        tree.Right.StyleStorage.Opacity = 1f;
        tree.Inner.StyleStorage.Opacity = 0f;
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Right);
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Left);

        // A hidden owner in the drain: the stale rule clears the hidden owner before the Tab, so these steps test the
        // stale rule, and the walk then starts with no owner (Tab gives the first candidate, Shift+Tab the last).
        tree.Inner.StyleStorage.Opacity = 1f;
        PushTab(tree, shift: true);
        AssertOwner(tree, tree.Inner);
        tree.Right.StyleStorage.Display = Display.None;
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Left);

        tree.Right.StyleStorage.Display = shown;
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Right);
        tree.Right.StyleStorage.Opacity = 0f;
        PushTab(tree, shift: true);
        AssertOwner(tree, tree.Left);

        // The walk reads no IsPickable.
        tree.Right.StyleStorage.Opacity = 1f;
        tree.Right.IsPickable = false;
        PushTab(tree, shift: false);
        AssertOwner(tree, tree.Right);

        // A hidden owner in a code call with no drain (the gamepad slot): the stale rule does not run, so the walk does
        // not see the owner. Tab gives the first candidate and Shift+Tab gives the last candidate. The order is Root,
        // Left, Right, and Inner, and Inner is not focusable here.
        tree.Right.IsPickable = true;
        tree.Inner.IsFocusable = false;
        tree.Root.IsFocusable = true;
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));
        tree.Left.StyleStorage.Opacity = 0f;
        tree.Context.FocusNext();
        AssertOwner(tree, tree.Root);

        tree.Left.StyleStorage.Opacity = 1f;
        Assert.True(tree.Context.SetFocus(tree.Left));
        tree.Left.StyleStorage.Opacity = 0f;
        tree.Context.FocusPrevious();
        AssertOwner(tree, tree.Right);
    }

    /// <summary>
    /// A Tab with no target walks to the old owner first. A handler that stops the walk keeps the focus. A walk that no
    /// handler stops moves the focus after the walk.
    /// </summary>
    [Fact]
    public void AHandlerThatStopsTheTabKeepsTheFocus()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(AHandlerThatStopsTheTabKeepsTheFocus));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Left);
        recorder.Register(tree.Right);

        // The bubble slot of the old owner stops the Tab in the target phase.
        recorder.StopAt(tree.Left, UiEventPhase.Target, captureSlot: false);
        Assert.Equal(1, PushTab(tree, shift: false));
        AssertOwner(tree, tree.Left);

        Assert.False(recorder.Overflow);
        Assert.Equal(3, recorder.Count);
        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[0].Phase);
        Assert.Same(tree.Left, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[1].Phase);
        Assert.True(recorder[1].CaptureSlot);
        Assert.Same(tree.Left, recorder[2].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[2].Phase);
        Assert.False(recorder[2].CaptureSlot);
        for (int i = 0; i < recorder.Count; i++)
        {
            Assert.Equal(UiEventType.KeyDown, recorder[i].Type);
        }

        Assert.Same(tree.Left, recorder.LastKey.Header.Target);
        Assert.Equal(UiContext.TabKey, recorder.LastKey.Key);

        // A stop in the capture phase of the root also keeps the focus. Shift+Tab obeys the same rule.
        recorder.Reset();
        recorder.StopAt(tree.Root, UiEventPhase.Capture, captureSlot: true);
        Assert.Equal(1, PushTab(tree, shift: true));
        AssertOwner(tree, tree.Left);
        Assert.Equal(1, recorder.Count);
        Assert.Same(tree.Root, recorder[0].CurrentTarget);

        // A stop on an element off the path never fires. The Tab walks to the old owner and then moves the focus.
        recorder.Reset();
        recorder.StopAt(tree.Right, UiEventPhase.Target, captureSlot: false);
        Assert.Equal(1, PushTab(tree, shift: false));
        AssertOwner(tree, tree.Right);
        Assert.Equal(4, recorder.Count);
        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Same(tree.Left, recorder[1].CurrentTarget);
        Assert.Same(tree.Left, recorder[2].CurrentTarget);
        Assert.Same(tree.Root, recorder[3].CurrentTarget);
        Assert.Equal(UiEventPhase.Bubble, recorder[3].Phase);
        Assert.Same(tree.Left, recorder.LastKey.Header.Target);
    }

    /// <summary>
    /// A Tab with <see cref="UiModifiers.Control"/>, <see cref="UiModifiers.Alt"/>, or <see cref="UiModifiers.Super"/>
    /// walks to the owner and does not move the focus. <see cref="UiModifiers.CapsLock"/> and
    /// <see cref="UiModifiers.NumLock"/> do not stop a move.
    /// </summary>
    [Fact]
    public void TabWithControlAltOrSuperDoesNotMove()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(TabWithControlAltOrSuperDoesNotMove));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));

        UiModifiers[] blocked =
        [
            UiModifiers.Control,
            UiModifiers.Alt,
            UiModifiers.Super,
            UiModifiers.Shift | UiModifiers.Control,
            UiModifiers.Shift | UiModifiers.Alt,
            UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super,
        ];
        for (int i = 0; i < blocked.Length; i++)
        {
            Assert.Equal(1, Push(tree, PointerTree.Key(UiEventType.KeyDown, UiContext.TabKey, blocked[i])));
            AssertOwner(tree, tree.Left);
        }

        Push(tree, PointerTree.Key(UiEventType.KeyDown, UiContext.TabKey, UiModifiers.CapsLock));
        AssertOwner(tree, tree.Right);

        Push(tree, PointerTree.Key(UiEventType.KeyDown, UiContext.TabKey, UiModifiers.NumLock | UiModifiers.Shift));
        AssertOwner(tree, tree.Left);
    }

    /// <summary>
    /// A repeat Tab key down moves the focus. A Tab key up and a char with the code point of the Tab do not.
    /// </summary>
    [Fact]
    public void ARepeatTabMovesAndAKeyUpDoesNot()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(ARepeatTabMovesAndAKeyUpDoesNot));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));

        Assert.Equal(1, Push(tree, PointerTree.Key(UiEventType.KeyUp, UiContext.TabKey, UiModifiers.None)));
        AssertOwner(tree, tree.Left);

        Assert.Equal(1, Push(tree, PointerTree.Key(UiEventType.KeyUp, UiContext.TabKey, UiModifiers.Shift)));
        AssertOwner(tree, tree.Left);

        tree.Context.PushEvent(PointerTree.Char('\t'));
        Assert.Equal(1, tree.Context.DrainEvents());
        AssertOwner(tree, tree.Left);

        KeyEvent repeat = PointerTree.Tab(shift: false);
        repeat.IsRepeat = true;
        Assert.Equal(1, Push(tree, repeat));
        AssertOwner(tree, tree.Right);

        KeyEvent shiftRepeat = PointerTree.Tab(shift: true);
        shiftRepeat.IsRepeat = true;
        Assert.Equal(1, Push(tree, shiftRepeat));
        AssertOwner(tree, tree.Left);
    }

    /// <summary>
    /// With one candidate, Tab and Shift+Tab give the same element, so the owner stays. With no candidate, the owner
    /// stays <see langword="null"/>.
    /// </summary>
    [Fact]
    public void TabWithOneOrNoFocusableElement()
    {
        PointerTree one = new(nameof(TabNavigationTests) + "." + nameof(TabWithOneOrNoFocusableElement) + ".one");
        one.Right.IsFocusable = true;
        one.Settle();

        PushTab(one, shift: false);
        AssertOwner(one, one.Right);
        PushTab(one, shift: false);
        AssertOwner(one, one.Right);
        PushTab(one, shift: true);
        AssertOwner(one, one.Right);

        PointerTree none = new(nameof(TabNavigationTests) + "." + nameof(TabWithOneOrNoFocusableElement) + ".none");
        none.Settle();

        Assert.Equal(0, PushTab(none, shift: false));
        Assert.Null(none.Context.FocusedElement);
        Assert.Equal(0, PushTab(none, shift: true));
        Assert.Null(none.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, none.Root.States);
        Assert.Equal(PseudoStates.None, none.Left.States);
        Assert.Equal(PseudoStates.None, none.Right.States);
        Assert.Equal(PseudoStates.None, none.Inner.States);
    }

    /// <summary>
    /// <see cref="UiContext.FocusNext"/> and <see cref="UiContext.FocusPrevious"/> move the focus as Tab and
    /// Shift+Tab do, but they push no event and call no handler.
    /// </summary>
    [Fact]
    public void FocusNextAndFocusPreviousMoveWithNoEvent()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(FocusNextAndFocusPreviousMoveWithNoEvent));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Inner.IsFocusable = true;
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Left);
        recorder.Register(tree.Right);
        recorder.Register(tree.Inner);

        tree.Context.FocusNext();
        AssertOwner(tree, tree.Left);
        Assert.Equal(0, tree.Context.QueuedEventCount);

        tree.Context.FocusNext();
        AssertOwner(tree, tree.Right);
        Assert.Equal(0, tree.Context.QueuedEventCount);

        tree.Context.FocusPrevious();
        AssertOwner(tree, tree.Left);
        Assert.Equal(0, tree.Context.QueuedEventCount);

        tree.Context.FocusPrevious();
        AssertOwner(tree, tree.Inner);
        Assert.Equal(0, tree.Context.QueuedEventCount);

        tree.Context.FocusNext();
        AssertOwner(tree, tree.Left);
        Assert.Equal(0, tree.Context.QueuedEventCount);

        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(0, recorder.Count);
    }

    /// <summary>
    /// A Tab with a target from the caller walks to that target and does not move the focus. A caller target outside
    /// the roots gets no walk and does not move the focus.
    /// </summary>
    [Fact]
    public void ATabWithACallerTargetDoesNotMove()
    {
        PointerTree tree = new(nameof(TabNavigationTests) + "." + nameof(ATabWithACallerTargetDoesNotMove));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Left);
        recorder.Register(tree.Right);

        Assert.Equal(1, Push(tree, Targeted(shift: false, tree.Right)));
        AssertOwner(tree, tree.Left);
        Assert.Equal(4, recorder.Count);
        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Same(tree.Right, recorder[1].CurrentTarget);
        Assert.Same(tree.Right, recorder[2].CurrentTarget);
        Assert.Same(tree.Root, recorder[3].CurrentTarget);
        Assert.Same(tree.Right, recorder.LastKey.Header.Target);

        // A caller target that is the owner itself does not move the focus either.
        recorder.Reset();
        Assert.Equal(1, Push(tree, Targeted(shift: true, tree.Left)));
        AssertOwner(tree, tree.Left);
        Assert.Equal(4, recorder.Count);
        Assert.Same(tree.Left, recorder.LastKey.Header.Target);

        // A caller target outside the roots gets no walk and does not move the focus.
        recorder.Reset();
        Element orphan = new() { Name = "orphan" };
        recorder.Register(orphan);
        Assert.Equal(0, Push(tree, Targeted(shift: false, orphan)));
        AssertOwner(tree, tree.Left);
        Assert.Equal(0, recorder.Count);

        // With no owner, a caller target still keeps the focus null.
        tree.Context.ClearFocus();
        Assert.Equal(1, Push(tree, Targeted(shift: false, tree.Right)));
        Assert.Null(tree.Context.FocusedElement);
    }

    /// <summary>
    /// A chain of 41 elements is deeper than the start stack of 32. The walk grows the stack, finds the focusable
    /// leaf, and leaves no element reference in a slot of the stack. An early return deep in the chain and a full pass
    /// both leave the stack clean.
    /// </summary>
    [Fact]
    public void ATreeDeeperThanTheStackFindsTheLeaf()
    {
        const int ancestors = 40;
        const int middle = 20;
        Element[] chain = new Element[ancestors + 1];
        for (int i = 0; i <= ancestors; i++)
        {
            chain[i] = new Element { Name = "level" + i };
            chain[i].Inline.Width = 10;
            chain[i].Inline.Height = 10;
            if (i > 0)
            {
                chain[i - 1].Children.Add(chain[i]);
            }
        }

        chain[ancestors].IsFocusable = true;
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
        context.AddRoot(chain[0]);
        context.Update(0f);

        Assert.True(ancestors + 1 > FocusWalk.InitialStackSize);
        Assert.Equal(FocusWalk.InitialStackSize, context.FocusStackLength);

        // A full pass with no owner reaches the leaf.
        context.PushEvent(PointerTree.Tab(shift: false));
        Assert.Equal(0, context.DrainEvents());
        Assert.Same(chain[ancestors], context.FocusedElement);
        Assert.True(context.FocusStackLength >= ancestors + 1);
        AssertStackClean(context);

        // A single candidate keeps the focus.
        context.PushEvent(PointerTree.Tab(shift: true));
        Assert.Equal(1, context.DrainEvents());
        Assert.Same(chain[ancestors], context.FocusedElement);
        AssertStackClean(context);

        // A second candidate in the middle of the chain. Shift+Tab from the leaf returns early at the leaf.
        chain[middle].IsFocusable = true;
        context.PushEvent(PointerTree.Tab(shift: true));
        Assert.Equal(1, context.DrainEvents());
        Assert.Same(chain[middle], context.FocusedElement);
        AssertStackClean(context);

        // Tab from the middle returns early at the leaf.
        context.FocusNext();
        Assert.Same(chain[ancestors], context.FocusedElement);
        AssertStackClean(context);

        // Tab from the leaf wraps to the middle after a full pass.
        context.FocusNext();
        Assert.Same(chain[middle], context.FocusedElement);
        AssertStackClean(context);
    }

    private static int PushTab(PointerTree tree, bool shift)
    {
        return Push(tree, PointerTree.Tab(shift));
    }

    private static int Push(PointerTree tree, in KeyEvent e)
    {
        tree.Context.PushEvent(in e);
        return tree.Context.DrainEvents();
    }

    private static KeyEvent Targeted(bool shift, Element target)
    {
        KeyEvent e = PointerTree.Tab(shift);
        e.Header = new UiEventHeader(UiEventType.KeyDown, 0d, target);
        return e;
    }

    private static void AssertOwner(PointerTree tree, Element expected)
    {
        Assert.Same(expected, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, expected.States & PseudoStates.Focus);
        AssertNoFocusExcept(tree.Root, expected);
        AssertNoFocusExcept(tree.Left, expected);
        AssertNoFocusExcept(tree.Right, expected);
        AssertNoFocusExcept(tree.Inner, expected);
    }

    private static void AssertNoFocusExcept(Element element, Element owner)
    {
        if (!ReferenceEquals(element, owner))
        {
            Assert.Equal(PseudoStates.None, element.States & PseudoStates.Focus);
        }
    }

    private static void AssertStackClean(UiContext context)
    {
        System.ReadOnlySpan<FocusFrame> slots = context.FocusStackSlots;
        for (int i = 0; i < slots.Length; i++)
        {
            Assert.Null(slots[i].Element);
        }
    }
}
