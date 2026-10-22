using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the edge rules of the pointer states: a stored element that left the tree loses its state at the start of
/// the next drain, an entry with a target from the caller changes no state, and a move over the same element marks
/// nothing.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class PointerStateEdgeTests
{
    private static readonly float2 OverRight = new(175f, 75f);
    private static readonly float2 OverLeft = new(50f, 50f);
    private static readonly float2 OverNewChild = new(250f, 50f);

    /// <summary>
    /// The demo of phase 5. A hovered element that leaves the tree loses <see cref="PseudoStates.Hover"/> at the next
    /// drain, and the context drops the hit.
    /// </summary>
    [Fact]
    public void AHoveredElementThatLeavesTheTreeLosesHover()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(AHoveredElementThatLeavesTheTreeLosesHover));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(OverRight));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.PointerHit);

        Assert.True(tree.Root.Children.Remove(tree.Right));
        Assert.Equal(0, tree.Context.DrainEvents());

        TestContext.Current.TestOutputHelper?.WriteLine(
            tree.Right.Name + " " + tree.Right.States + " " + (tree.Context.PointerHit?.Name ?? "none"));

        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Null(tree.Context.PointerHit);
    }

    /// <summary>A root that the caller removes from the context takes the hover of its descendants away.</summary>
    [Fact]
    public void ARemovedRootLosesHover()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(ARemovedRootLosesHover));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(OverRight));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover, tree.Right.States);

        Assert.True(tree.Context.RemoveRoot(tree.Root));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Null(tree.Context.PointerHit);
    }

    /// <summary>
    /// An active element that leaves the tree loses <see cref="PseudoStates.Active"/> at the next drain. The release
    /// of the button still routes to the element under the pointer.
    /// </summary>
    [Fact]
    public void AnActiveElementThatLeavesTheTreeLosesActive()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(AnActiveElementThatLeavesTheTreeLosesActive));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.ActiveElement);

        Assert.True(tree.Root.Children.Remove(tree.Right));

        // An empty drain runs the stale rule alone. A release in the same drain would clear the state and hide the rule.
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Null(tree.Context.ActiveElement);

        Element? seenTarget = null;
        tree.Root.OnPointerUp = (ref PointerEvent e) => seenTarget = e.Header.Target;

        // The release is over Left now. The drain routes the release to Left.
        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Same(tree.Left, seenTarget);
        Assert.Same(tree.Left, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        // The button mask stayed, so the release cleared its bit. A new press sets the state again.
        tree.Context.PushEvent(PointerTree.Down(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.ActiveElement);
    }

    /// <summary>
    /// The stale rule for <see cref="PseudoStates.Active"/> keeps the button mask. A button that is still down keeps
    /// <see cref="PseudoStates.Active"/> on the next pressed element until the last button goes up.
    /// </summary>
    [Fact]
    public void TheStaleRuleKeepsTheButtonMask()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(TheStaleRuleKeepsTheButtonMask));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Right, tree.Context.ActiveElement);

        Assert.True(tree.Root.Children.Remove(tree.Right));
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.ActiveElement);

        // The left button is still down. The right button goes down over Left, and Left becomes active.
        tree.Context.PushEvent(PointerTree.Down(OverLeft, PointerButton.Right));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.ActiveElement);

        // The right button goes up. The left bit stays in the mask, so Left keeps Active.
        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Right));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Left.States);

        // The left button goes up. No button is down now, so Left loses Active.
        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
    }

    /// <summary>
    /// An entry with a target from the caller skips the hit test. It changes no state, no hit, and no position.
    /// </summary>
    [Fact]
    public void ACallerTargetChangesNoState()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(ACallerTargetChangesNoState));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(OverRight));
        Assert.Equal(1, tree.Context.DrainEvents());
        float2 positionBefore = tree.Context.PointerPosition;

        PointerEvent withTarget = new()
        {
            Header = new UiEventHeader(UiEventType.PointerDown, 0d, tree.Left),
            Position = new float2(300f, 250f),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };

        Element? seenTarget = null;
        tree.Left.OnPointerDown = (ref PointerEvent e) => seenTarget = e.Header.Target;

        tree.Context.PushEvent(withTarget);
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Left, seenTarget);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.PointerHit);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(positionBefore, tree.Context.PointerPosition);
    }

    /// <summary>A second move over the same element marks no element for <see cref="DirtyFlags.Style"/>.</summary>
    [Fact]
    public void AMoveOverTheSameElementMarksNothing()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(AMoveOverTheSameElementMarksNothing));
        tree.AddHoverSheet();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(OverRight));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(DirtyFlags.None, tree.Root.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.None, tree.Left.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.None, tree.Right.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.None, tree.Inner.Dirty & DirtyFlags.Style);
        Assert.Same(tree.Right, tree.Context.PointerHit);
    }

    /// <summary>
    /// An element that the caller adds after <see cref="UiContext.Update(float)"/> has no layout yet, so the hit test
    /// skips it. After the next update it is hit.
    /// </summary>
    [Fact]
    public void AnElementAddedAfterUpdateIsNotHitUntilTheNextUpdate()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(AnElementAddedAfterUpdateIsNotHitUntilTheNextUpdate));
        tree.Settle();

        Element added = new() { Name = "added" };
        added.Inline.Width = 100;
        added.Inline.Height = 100;
        tree.Root.Children.Add(added);

        tree.Context.PushEvent(PointerTree.Move(OverNewChild));
        tree.Context.DrainEvents();
        Assert.NotSame(added, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.None, added.States);

        tree.Settle();
        tree.Context.PushEvent(PointerTree.Move(new float2(251f, 51f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(added, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.Hover, added.States);
    }

    /// <summary>A drain with no pointer entry keeps a hover that is still valid.</summary>
    [Fact]
    public void ADrainWithNoPointerEventKeepsAValidHover()
    {
        PointerTree tree = new(nameof(PointerStateEdgeTests) + "." + nameof(ADrainWithNoPointerEventKeepsAValidHover));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(OverRight));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0, tree.Context.DrainEvents());
        tree.Settle();
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.PointerHit);
    }
}
