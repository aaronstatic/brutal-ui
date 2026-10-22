using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the focus owner and of the key route: code sets a focus owner, the owner gets
/// <see cref="PseudoStates.Focus"/>, and a key or char event with no target walks to the owner.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class FocusOwnerTests
{
    /// <summary>The key code of the tests. The value is the GLFW code of the key A.</summary>
    private const int KeyA = 65;

    /// <summary>
    /// The demo of phase 1. <see cref="PointerTree.Right"/> is focusable and gets the focus from code. A key down with
    /// no target walks to it: root capture, <see cref="PointerTree.Right"/> target capture,
    /// <see cref="PointerTree.Right"/> target bubble, and root bubble. The next update gives it the fill of the focus
    /// rule.
    /// </summary>
    [Fact]
    public void AKeyWithNoTargetWalksToTheFocusOwner()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(AKeyWithNoTargetWalksToTheFocusOwner));
        tree.Right.IsFocusable = true;
        tree.AddFocusSheet();
        tree.Settle();
        Assert.NotEqual(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Right);

        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.Same(tree.Right, tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        TestContext.Current.TestOutputHelper?.WriteLine(
            tree.Right.Name + " " + tree.Right.States + " " + tree.Right.Style.BackgroundColor);

        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.NotEqual(PointerTree.FocusFill, tree.Left.Style.BackgroundColor);

        // Capture: root. Target: Right in the capture slot and in the bubble slot. Bubble: root.
        Assert.False(recorder.Overflow);
        Assert.Equal(4, recorder.Count);
        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[0].Phase);
        Assert.True(recorder[0].CaptureSlot);
        Assert.Same(tree.Right, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[1].Phase);
        Assert.True(recorder[1].CaptureSlot);
        Assert.Same(tree.Right, recorder[2].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[2].Phase);
        Assert.False(recorder[2].CaptureSlot);
        Assert.Same(tree.Root, recorder[3].CurrentTarget);
        Assert.Equal(UiEventPhase.Bubble, recorder[3].Phase);
        Assert.False(recorder[3].CaptureSlot);
        for (int i = 0; i < recorder.Count; i++)
        {
            Assert.Equal(UiEventType.KeyDown, recorder[i].Type);
        }

        Assert.Same(tree.Right, recorder.LastKey.Header.Target);
        Assert.Equal(KeyA, recorder.LastKey.Key);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
    }

    /// <summary>A char with no target walks to the focus owner, and the code point survives the ring.</summary>
    [Fact]
    public void ACharWithNoTargetWalksToTheFocusOwner()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(ACharWithNoTargetWalksToTheFocusOwner));
        tree.Right.IsFocusable = true;
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Left);
        recorder.Register(tree.Right);

        Assert.True(tree.Context.SetFocus(tree.Right));

        // U+00E9, a code point above the ASCII range.
        tree.Context.PushEvent(PointerTree.Char(0xE9u));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(4, recorder.Count);
        for (int i = 0; i < recorder.Count; i++)
        {
            Assert.Equal(UiEventType.Char, recorder[i].Type);
            Assert.NotSame(tree.Left, recorder[i].CurrentTarget);
        }

        Assert.Same(tree.Right, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[1].Phase);
        Assert.Same(tree.Right, recorder.LastChar.Header.Target);
        Assert.Equal(0xE9u, recorder.LastChar.CodePoint);
    }

    /// <summary>
    /// With no focus owner, a key down, a key up, and a char with no target get no walk. The drain changes no state.
    /// </summary>
    [Fact]
    public void AKeyWithNoOwnerGetsNoWalk()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(AKeyWithNoOwnerGetsNoWalk));
        tree.Right.IsFocusable = true;
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Left);
        recorder.Register(tree.Right);
        recorder.Register(tree.Inner);

        Assert.Null(tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, KeyA, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Char('a'));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(0, recorder.Count);
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.Equal(0L, tree.Context.DroppedEventCount);
    }

    /// <summary>
    /// A key with a target from the caller walks to that target and not to the focus owner. The owner keeps the
    /// focus, and the target gets no focus (Decision 3).
    /// </summary>
    [Fact]
    public void AKeyWithACallerTargetKeepsItsTarget()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(AKeyWithACallerTargetKeepsItsTarget));
        tree.Right.IsFocusable = true;
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Left);
        recorder.Register(tree.Right);

        Assert.True(tree.Context.SetFocus(tree.Right));

        KeyEvent e = PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.Shift);
        e.Header.Target = tree.Left;
        tree.Context.PushEvent(e);
        Assert.Equal(1, tree.Context.DrainEvents());

        // Capture: root. Target: Left in the capture slot and in the bubble slot. Bubble: root.
        Assert.Equal(4, recorder.Count);
        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Same(tree.Left, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[1].Phase);
        Assert.Same(tree.Left, recorder[2].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[2].Phase);
        Assert.Same(tree.Root, recorder[3].CurrentTarget);
        for (int i = 0; i < recorder.Count; i++)
        {
            Assert.NotSame(tree.Right, recorder[i].CurrentTarget);
        }

        Assert.Same(tree.Left, recorder.LastKey.Header.Target);
        Assert.Equal(UiModifiers.Shift, recorder.LastKey.Modifiers);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
    }

    /// <summary>
    /// A second <see cref="UiContext.SetFocus(Element)"/> moves the state: <see cref="PointerTree.Left"/> loses
    /// <see cref="PseudoStates.Focus"/> and the fill, and <see cref="PointerTree.Right"/> gets them. A repeated set
    /// returns <see langword="true"/> and marks nothing.
    /// </summary>
    [Fact]
    public void SetFocusMovesTheFocusState()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(SetFocusMovesTheFocusState));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.AddFocusSheet();
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Left));
        tree.Settle();
        Assert.Same(tree.Left, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Left.States);
        Assert.Equal(PointerTree.FocusFill, tree.Left.Style.BackgroundColor);

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Settle();
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.NotEqual(PointerTree.FocusFill, tree.Left.Style.BackgroundColor);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.Equal(PseudoStates.None, tree.Root.States);

        // A repeated set keeps the owner and marks no subtree.
        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(DirtyFlags.None, tree.Root.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.None, tree.Right.Dirty & DirtyFlags.Style);
    }

    /// <summary>
    /// <see cref="UiContext.ClearFocus"/> clears the owner, the state, and the fill. A key then gets no walk. A second
    /// call with no owner changes nothing.
    /// </summary>
    [Fact]
    public void ClearFocusClearsTheState()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(ClearFocusClearsTheState));
        tree.Right.IsFocusable = true;
        tree.AddFocusSheet();
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Settle();
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);

        tree.Context.ClearFocus();
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        tree.Settle();
        Assert.NotEqual(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);

        tree.Context.ClearFocus();
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(DirtyFlags.None, tree.Right.Dirty & DirtyFlags.Style);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        Assert.Equal(0, tree.Context.DrainEvents());
    }

    /// <summary>
    /// The default of <see cref="Element.IsFocusable"/> is <see langword="false"/>. A set to an element that is not
    /// focusable returns <see langword="false"/>, and the owner stays. A null element throws.
    /// </summary>
    [Fact]
    public void SetFocusRejectsAnElementThatIsNotFocusable()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(SetFocusRejectsAnElementThatIsNotFocusable));
        Assert.False(new Element().IsFocusable);
        Assert.False(tree.Left.IsFocusable);

        tree.Right.IsFocusable = true;
        tree.Settle();
        Assert.False(tree.Context.SetFocus(tree.Left));
        Assert.Null(tree.Context.FocusedElement);

        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.False(tree.Context.SetFocus(tree.Left));
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);

        Assert.Throws<ArgumentNullException>(() => tree.Context.SetFocus(null!));
        Assert.Same(tree.Right, tree.Context.FocusedElement);
    }

    /// <summary>
    /// A set to an element outside the roots of the context returns <see langword="false"/> and changes nothing: an
    /// orphan, a child of an orphan, a root of another context, and a child of that root.
    /// </summary>
    [Fact]
    public void SetFocusRejectsAnElementOutsideTheRoots()
    {
        PointerTree tree = new(nameof(FocusOwnerTests) + "." + nameof(SetFocusRejectsAnElementOutsideTheRoots));
        PointerTree other = new(nameof(FocusOwnerTests) + "." + nameof(SetFocusRejectsAnElementOutsideTheRoots) + ".other");
        tree.Right.IsFocusable = true;
        tree.Settle();
        other.Settle();

        Element orphan = new() { Name = "orphan", IsFocusable = true };
        Element orphanChild = new() { Name = "orphan-child", IsFocusable = true };
        orphan.Children.Add(orphanChild);
        other.Root.IsFocusable = true;
        other.Right.IsFocusable = true;

        Assert.False(tree.Context.SetFocus(orphan));
        Assert.False(tree.Context.SetFocus(orphanChild));
        Assert.False(tree.Context.SetFocus(other.Root));
        Assert.False(tree.Context.SetFocus(other.Right));
        Assert.Null(tree.Context.FocusedElement);
        Assert.Null(other.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, orphan.States);
        Assert.Equal(PseudoStates.None, orphanChild.States);
        Assert.Equal(PseudoStates.None, other.Root.States);
        Assert.Equal(PseudoStates.None, other.Right.States);

        // A rejected set keeps the owner that a valid set stored.
        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.False(tree.Context.SetFocus(other.Right));
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
    }
}
