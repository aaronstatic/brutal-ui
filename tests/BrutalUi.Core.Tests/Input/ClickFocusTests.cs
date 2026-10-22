using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the click focus: a press with no target moves the keyboard focus to the hit element or to its nearest
/// focusable ancestor. A press on no such element, or over no element, clears the focus.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class ClickFocusTests
{
    /// <summary>A point inside <see cref="PointerTree.Right"/> and outside <see cref="PointerTree.Inner"/>.</summary>
    private static readonly float2 OverRight = new(175f, 75f);

    /// <summary>A point inside <see cref="PointerTree.Inner"/>.</summary>
    private static readonly float2 OverInner = new(125f, 25f);

    /// <summary>A point inside <see cref="PointerTree.Left"/>.</summary>
    private static readonly float2 OverLeft = new(50f, 50f);

    /// <summary>A point inside the root and outside each child: the gap of the root.</summary>
    private static readonly float2 OverGap = new(300f, 200f);

    /// <summary>A point inside the viewport and outside each root.</summary>
    private static readonly float2 OverNothing = new(700f, 500f);

    /// <summary>
    /// The demo of phase 2. <see cref="PointerTree.Right"/> is focusable. A press with no target over it, outside
    /// <see cref="PointerTree.Inner"/>, gives it the focus before the walk, so its <c>PointerDown</c> handler reads
    /// <see cref="PseudoStates.Focus"/> already. The next update gives it the fill of the focus rule. A press over the
    /// gap of the root then clears the focus.
    /// </summary>
    [Fact]
    public void APressOnAFocusableElementFocusesIt()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(APressOnAFocusableElementFocusesIt));
        tree.Right.IsFocusable = true;
        tree.AddFocusSheet();
        tree.Settle();
        Assert.NotEqual(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);
        Assert.Null(tree.Context.FocusedElement);

        int calls = 0;
        PseudoStates statesInHandler = PseudoStates.None;
        Element? ownerInHandler = null;
        tree.Right.OnPointerDown = (ref PointerEvent e) =>
        {
            calls++;
            statesInHandler = tree.Right.States;
            ownerInHandler = tree.Context.FocusedElement;
        };

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "down " + tree.Right.Name + " " + tree.Right.States + " " + tree.Right.Style.BackgroundColor + " "
            + (tree.Context.FocusedElement?.Name ?? "none"));

        Assert.Equal(1, calls);
        Assert.NotEqual(PseudoStates.None, statesInHandler & PseudoStates.Focus);
        Assert.Same(tree.Right, ownerInHandler);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.NotEqual(PointerTree.FocusFill, tree.Left.Style.BackgroundColor);

        // The release keeps the focus. The press over the gap hits the root, which is not focusable.
        tree.Context.PushEvent(PointerTree.Up(OverRight, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(OverGap, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());
        tree.Settle();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "gap " + tree.Right.Name + " " + tree.Right.States + " " + tree.Right.Style.BackgroundColor + " "
            + (tree.Context.FocusedElement?.Name ?? "none"));

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.NotEqual(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Root.States);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// A press on <see cref="PointerTree.Inner"/>, which is not focusable, focuses <see cref="PointerTree.Right"/>,
    /// its nearest focusable ancestor. The root is focusable too, but it is further up, so it gets nothing. The hover
    /// and active states stay on the hit element.
    /// </summary>
    [Fact]
    public void APressOnAChildFocusesTheNearestFocusableAncestor()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(APressOnAChildFocusesTheNearestFocusableAncestor));
        tree.Root.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();

        Element? ownerInHandler = null;
        tree.Inner.OnPointerDown = (ref PointerEvent e) => ownerInHandler = tree.Context.FocusedElement;

        tree.Context.PushEvent(PointerTree.Down(OverInner, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Right, ownerInHandler);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Inner.States);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
    }

    /// <summary>
    /// A press on <see cref="PointerTree.Inner"/> focuses it when it is focusable itself, also when its parent
    /// <see cref="PointerTree.Right"/> is focusable. A later press on <see cref="PointerTree.Right"/> moves the focus
    /// up to it.
    /// </summary>
    [Fact]
    public void APressOnAFocusableChildFocusesTheChild()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(APressOnAFocusableChildFocusesTheChild));
        tree.Right.IsFocusable = true;
        tree.Inner.IsFocusable = true;
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverInner, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Inner, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus, tree.Inner.States);
        Assert.Equal(PseudoStates.None, tree.Right.States);

        tree.Context.PushEvent(PointerTree.Up(OverInner, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
    }

    /// <summary>
    /// A press on an element with no focusable element on its parent chain clears the focus: a press on
    /// <see cref="PointerTree.Left"/>, and a press over the gap of the root. The press still walks to the hit element.
    /// </summary>
    [Fact]
    public void APressOnNoFocusableElementClearsTheFocus()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(APressOnNoFocusableElementClearsTheFocus));
        tree.Right.IsFocusable = true;
        tree.AddFocusSheet();
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Context.PushEvent(PointerTree.Down(OverLeft, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Settle();
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);

        recorder.Reset();
        tree.Context.PushEvent(PointerTree.Down(OverGap, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.NotEqual(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Root.States);

        // The press walks to the root: the capture slot and the bubble slot of the target phase.
        Assert.Equal(2, recorder.Count);
        Assert.Same(tree.Root, recorder.LastPointer.Header.Target);
        Assert.Equal(UiEventType.PointerDown, recorder.LastPointer.Header.Type);
    }

    /// <summary>
    /// A press over no element clears the focus and gets no walk: a press outside each root, and a press at
    /// (NaN, NaN) after a cursor leave.
    /// </summary>
    [Fact]
    public void APressOverNothingClearsTheFocus()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(APressOverNothingClearsTheFocus));
        tree.Right.IsFocusable = true;
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Context.PushEvent(PointerTree.Down(OverNothing, PointerButton.Left));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Null(tree.Context.ActiveElement);

        tree.Context.PushEvent(PointerTree.Up(OverNothing, PointerButton.Left));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Context.PushEvent(PointerTree.Down(new float2(float.NaN, float.NaN), PointerButton.Left));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
    }

    /// <summary>
    /// A move, a release, and a scroll keep the focus. Each one goes over the focusable <see cref="PointerTree.Left"/>
    /// and then over no element.
    /// </summary>
    [Fact]
    public void AMoveAReleaseAndAScrollKeepTheFocus()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(AMoveAReleaseAndAScrollKeepTheFocus));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Right));

        tree.Context.PushEvent(PointerTree.Move(OverLeft));
        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Scroll(OverLeft, new float2(0f, -1f)));
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        tree.Context.PushEvent(PointerTree.Move(OverNothing));
        tree.Context.PushEvent(PointerTree.Up(OverNothing, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Scroll(OverNothing, new float2(0f, 1f)));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
    }

    /// <summary>
    /// A press with a target from the caller walks to that target and changes no focus (Decision 2 of
    /// pointer-routing). A press to a focusable target sets no owner, and a press to a target that is not focusable
    /// keeps the owner.
    /// </summary>
    [Fact]
    public void APressWithACallerTargetKeepsTheFocus()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(APressWithACallerTargetKeepsTheFocus));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Left);

        PointerEvent toLeft = PointerTree.Down(OverLeft, PointerButton.Left);
        toLeft.Header.Target = tree.Left;
        tree.Context.PushEvent(toLeft);
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(2, recorder.Count);
        Assert.Same(tree.Left, recorder.LastPointer.Header.Target);
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Left.States);

        Assert.True(tree.Context.SetFocus(tree.Right));

        PointerEvent toRoot = PointerTree.Down(OverGap, PointerButton.Left);
        toRoot.Header.Target = tree.Root;
        tree.Context.PushEvent(toRoot);
        tree.Context.PushEvent(toLeft);
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Root.States);
    }

    /// <summary>
    /// Each button runs the click focus (Decision 4). A right press focuses <see cref="PointerTree.Right"/>. A middle
    /// press over <see cref="PointerTree.Left"/> moves the focus while the right button holds
    /// <see cref="PseudoStates.Active"/> on <see cref="PointerTree.Right"/>. A right press over the gap clears it.
    /// </summary>
    [Fact]
    public void ARightButtonPressMovesTheFocus()
    {
        PointerTree tree = new(nameof(ClickFocusTests) + "." + nameof(ARightButtonPressMovesTheFocus));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Right));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus, tree.Right.States);

        // The Active state stays on Right, because a button is down. The focus moves with no regard to it.
        tree.Context.PushEvent(PointerTree.Down(OverLeft, PointerButton.Middle));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Left, tree.Context.FocusedElement);
        Assert.Same(tree.Right, tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.Active, tree.Right.States);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Focus, tree.Left.States);

        tree.Context.PushEvent(PointerTree.Down(OverGap, PointerButton.Right));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.Active, tree.Right.States);
    }
}
