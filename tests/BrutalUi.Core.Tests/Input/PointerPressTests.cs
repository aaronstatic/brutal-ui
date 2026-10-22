using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the press and release steps of the drain: a press sets <see cref="PseudoStates.Active"/> on the hit
/// element, and the release of the last button clears it.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class PointerPressTests
{
    private static readonly float2 OverRight = new(175f, 75f);
    private static readonly float2 OverLeft = new(50f, 50f);
    private static readonly float2 OverNothing = new(700f, 500f);

    /// <summary>
    /// The demo of phase 2. A press over the second child sets <see cref="PseudoStates.Active"/> on it, and the
    /// release clears it.
    /// </summary>
    [Fact]
    public void APressSetsActiveAndTheReleaseClearsIt()
    {
        PointerTree tree = new(nameof(PointerPressTests) + "." + nameof(APressSetsActiveAndTheReleaseClearsIt));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        TestContext.Current.TestOutputHelper?.WriteLine(
            "down " + tree.Right.Name + " " + tree.Right.States + " " + (tree.Context.ActiveElement?.Name ?? "none"));

        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);

        tree.Context.PushEvent(PointerTree.Up(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        TestContext.Current.TestOutputHelper?.WriteLine(
            "up " + tree.Right.Name + " " + tree.Right.States + " " + (tree.Context.ActiveElement?.Name ?? "none"));

        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A press over the second child, outside its own child, walks from the second child to the root. The root
    /// handler reads the second child as the target and the root as the current target. The state is set before the
    /// handler runs.
    /// </summary>
    [Fact]
    public void APressBubblesFromTheHitElement()
    {
        PointerTree tree = new(nameof(PointerPressTests) + "." + nameof(APressBubblesFromTheHitElement));
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Right);
        recorder.Register(tree.Inner);

        Element? seenTarget = null;
        Element? seenCurrent = null;
        PseudoStates rightStatesInHandler = PseudoStates.None;
        UiEventPhase seenPhase = UiEventPhase.None;
        tree.Root.OnPointerDown = (ref PointerEvent e) =>
        {
            seenTarget = e.Header.Target;
            seenCurrent = e.Header.CurrentTarget;
            seenPhase = e.Header.Phase;
            rightStatesInHandler = tree.Right.States;
        };

        // OverRight is inside Right and outside Inner (Inner covers x 100 to 150 and y 0 to 50).
        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Right, seenTarget);
        Assert.Same(tree.Root, seenCurrent);
        Assert.Equal(UiEventPhase.Bubble, seenPhase);
        Assert.True((rightStatesInHandler & PseudoStates.Active) != 0);

        // The recorder holds the capture call of the root and the two target calls of Right (the capture slot, then
        // the bubble slot). The test replaced the bubble handler of the root. Inner is not on the path.
        Assert.False(recorder.Overflow);
        Assert.Equal(3, recorder.Count);
        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[0].Phase);
        Assert.Same(tree.Right, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[1].Phase);
        Assert.True(recorder[1].CaptureSlot);
        Assert.Equal(UiEventType.PointerDown, recorder[1].Type);
        Assert.Same(tree.Right, recorder[2].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[2].Phase);
        Assert.False(recorder[2].CaptureSlot);

        // The state goes on the hit element only (Decision R1).
        Assert.True((tree.Right.States & PseudoStates.Active) != 0);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.Equal(PseudoStates.None, tree.Root.States);
    }

    /// <summary>
    /// A release over another element walks to that element. The first element loses
    /// <see cref="PseudoStates.Active"/>, and the other element never gets it.
    /// </summary>
    [Fact]
    public void AReleaseOverAnotherElementClearsTheFirst()
    {
        PointerTree tree = new(nameof(PointerPressTests) + "." + nameof(AReleaseOverAnotherElementClearsTheFirst));
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Left);

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Right.States);
        Assert.Equal(0, recorder.Count);

        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
        Assert.Null(tree.Context.ActiveElement);

        // The up walks to the element under the pointer, not to the pressed element.
        Assert.False(recorder.Overflow);
        Assert.Equal(2, recorder.Count);
        Assert.Same(tree.Left, recorder[0].CurrentTarget);
        Assert.Equal(UiEventType.PointerUp, recorder[0].Type);
        Assert.Same(tree.Left, recorder.LastPointer.Header.Target);
    }

    /// <summary>
    /// The state stays on the first element while one button is down. A second button over another element does not
    /// move it. The release of the last button clears it.
    /// </summary>
    [Fact]
    public void ASecondButtonKeepsActiveUntilTheLastRelease()
    {
        PointerTree tree = new(nameof(PointerPressTests) + "." + nameof(ASecondButtonKeepsActiveUntilTheLastRelease));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(OverLeft, PointerButton.Right));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Same(tree.Right, tree.Context.ActiveElement);
        Assert.True((tree.Right.States & PseudoStates.Active) != 0);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        // The first release leaves one button down, so the state stays.
        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Right, tree.Context.ActiveElement);
        Assert.True((tree.Right.States & PseudoStates.Active) != 0);

        tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Right));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
    }

    /// <summary>
    /// A release over no element still clears <see cref="PseudoStates.Active"/>. The release can land outside each
    /// root, or at NaN after a cursor leave. The release gets no walk.
    /// </summary>
    /// <param name="x">The x position of the release.</param>
    /// <param name="y">The y position of the release.</param>
    [Theory]
    [InlineData(500f, 500f)]
    [InlineData(float.NaN, float.NaN)]
    public void AReleaseOverNothingClearsActive(float x, float y)
    {
        PointerTree tree = new(nameof(PointerPressTests) + "." + nameof(AReleaseOverNothingClearsActive)
            + "." + (float.IsNaN(x) ? "nan" : "outside"));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Right, tree.Context.ActiveElement);
        Assert.NotEqual(PseudoStates.None, tree.Right.States & PseudoStates.Active);

        tree.Context.PushEvent(PointerTree.Up(new float2(x, y), PointerButton.Left));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Right.States & PseudoStates.Active);
    }

    /// <summary>A press over no element sets no state, gets no walk, and holds no active element.</summary>
    [Fact]
    public void APressOverNothingSetsNoActive()
    {
        PointerTree tree = new(nameof(PointerPressTests) + "." + nameof(APressOverNothingSetsNoActive));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(OverNothing, PointerButton.Left));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);

        // A move over an element after the press is no press. It sets no Active.
        tree.Context.PushEvent(PointerTree.Move(OverRight));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
    }

    /// <summary>
    /// After the press, the next update gives the element the fill of the <c>:active</c> rule. After the release,
    /// the next update takes the fill away.
    /// </summary>
    [Fact]
    public void AnActiveRuleShowsAfterUpdate()
    {
        PointerTree tree = new(nameof(PointerPressTests) + "." + nameof(AnActiveRuleShowsAfterUpdate));
        tree.AddActiveSheet();
        tree.Settle();
        Assert.NotEqual(PointerTree.ActiveFill, tree.Right.Style.BackgroundColor);

        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        Assert.Equal(PointerTree.ActiveFill, tree.Right.Style.BackgroundColor);
        Assert.NotEqual(PointerTree.ActiveFill, tree.Left.Style.BackgroundColor);

        tree.Context.PushEvent(PointerTree.Up(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        Assert.NotEqual(PointerTree.ActiveFill, tree.Right.Style.BackgroundColor);
    }
}
