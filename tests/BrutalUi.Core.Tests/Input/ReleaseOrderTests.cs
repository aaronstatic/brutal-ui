using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the release order (Decision 2 of button-and-toggle): <see cref="PseudoStates.Active"/> stays on the
/// pressed element through the walk of the release, and the drain clears it after that walk.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class ReleaseOrderTests
{
    private static readonly float2 OverRight = new(175f, 75f);
    private static readonly float2 OverLeft = new(50f, 50f);
    private static readonly float2 OverNothing = new(600f, 500f);

    /// <summary>
    /// A <see cref="UiEventType.PointerUp"/> handler on the pressed element reads <see cref="PseudoStates.Active"/> on
    /// that element and finds it in <see cref="UiContext.ActiveElement"/>.
    /// </summary>
    [Fact]
    public void AReleaseHandlerOfThePressedElementReadsActive()
    {
        PointerTree tree = new(nameof(ReleaseOrderTests) + "." + nameof(AReleaseHandlerOfThePressedElementReadsActive));
        tree.Settle();

        PseudoStates seenStates = PseudoStates.None;
        Element? seenActive = null;
        int calls = 0;
        tree.Right.OnPointerUp = (ref PointerEvent e) =>
        {
            calls++;
            seenStates = tree.Right.States;
            seenActive = tree.Context.ActiveElement;
        };

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Right, tree.Context.ActiveElement);

        tree.Context.PushEvent(PointerTree.Up(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(1, calls);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, seenStates);
        Assert.Same(tree.Right, seenActive);
    }

    /// <summary>
    /// After the drain of a release, no element holds <see cref="PseudoStates.Active"/> and
    /// <see cref="UiContext.ActiveElement"/> is <see langword="null"/>. A release over no element gets no walk, and
    /// the drain still clears the state.
    /// </summary>
    [Fact]
    public void TheDrainClearsActiveAfterTheReleaseWalk()
    {
        PointerTree tree = new(nameof(ReleaseOrderTests) + "." + nameof(TheDrainClearsActiveAfterTheReleaseWalk));
        tree.Settle();

        // The press and the release go through one drain. The release walk ends, and then Active goes off.
        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(OverRight, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Null(tree.Context.ActiveElement);

        // A release over nothing gets no walk. The drain still clears Active on the pressed element.
        tree.Context.PushEvent(PointerTree.Down(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.ActiveElement);

        tree.Context.PushEvent(PointerTree.Up(OverNothing, PointerButton.Left));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Null(tree.Context.PointerHit);
    }

    /// <summary>
    /// A release over another element walks to that element. Its handler reads <see cref="PseudoStates.Active"/> on
    /// the pressed element and not on the element under the pointer.
    /// </summary>
    [Fact]
    public void AReleaseOverAnotherElementSeesActiveOnThePressedElement()
    {
        PointerTree tree = new(nameof(ReleaseOrderTests) + "." + nameof(AReleaseOverAnotherElementSeesActiveOnThePressedElement));
        tree.Settle();

        PseudoStates seenRight = PseudoStates.None;
        PseudoStates seenLeft = PseudoStates.None;
        int calls = 0;
        tree.Left.OnPointerUp = (ref PointerEvent e) =>
        {
            calls++;
            seenRight = tree.Right.States;
            seenLeft = tree.Left.States;
        };

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(1, calls);
        Assert.Equal(PseudoStates.Active, seenRight & PseudoStates.Active);
        Assert.Equal(PseudoStates.None, seenLeft & PseudoStates.Active);

        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A release handler that throws stops the drain. The <c>finally</c> block of the drain clears
    /// <see cref="PseudoStates.Active"/>, and the next drain runs.
    /// </summary>
    [Fact]
    public void AThrowingReleaseHandlerStillClearsActive()
    {
        PointerTree tree = new(nameof(ReleaseOrderTests) + "." + nameof(AThrowingReleaseHandlerStillClearsActive));
        tree.Settle();

        tree.Right.OnPointerUp = (ref PointerEvent e) => throw new InvalidOperationException("The release handler fails.");

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(OverRight, PointerButton.Left));
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tree.Context.DrainEvents());
        Assert.Equal("The release handler fails.", exception.Message);

        Assert.Equal(PseudoStates.None, tree.Root.States & PseudoStates.Active);
        Assert.Equal(PseudoStates.None, tree.Left.States & PseudoStates.Active);
        Assert.Equal(PseudoStates.None, tree.Right.States & PseudoStates.Active);
        Assert.Equal(PseudoStates.None, tree.Inner.States & PseudoStates.Active);
        Assert.Null(tree.Context.ActiveElement);

        // The next drain runs. A new press sets Active again, so the release left no stuck state.
        tree.Right.OnPointerUp = null;
        tree.Context.PushEvent(PointerTree.Down(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.ActiveElement);

        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Null(tree.Context.ActiveElement);
    }
}
