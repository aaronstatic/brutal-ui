using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the drag capture (Decision 3 of slider): while a <see cref="Slider"/> holds the left press with no chord,
/// each pointer move with no target walks to the slider, also outside its rect. <see cref="PseudoStates.Hover"/>,
/// <see cref="UiContext.PointerHit"/>, and the routing gate still follow the hit.
/// </summary>
/// <remarks>
/// Each test drives the scene <see cref="SliderTree"/> through <see cref="UiContext.PushEvent(in PointerEvent)"/> with
/// no caller target, and runs <see cref="UiContext.Update(float)"/>. The value <c>v</c> of the range 0 to 100 sits at
/// the x <c>50 + v</c>. The tests intern no class name.
/// </remarks>
public sealed class DragCaptureTests
{
    /// <summary>
    /// The demo of phase 5. A left press at the value 25 and a move to (300, 10), over the root and outside the slider,
    /// give the value 100. The root holds <see cref="PseudoStates.Hover"/>, and the slider does not.
    /// </summary>
    [Fact]
    public void ADragPastTheRightEndGivesTheMaximum()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.PastRight));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press at 25, move to (300, 10), Value = " + tree.Slider.Value + ", root = " + tree.Root.States
            + ", slider = " + tree.Slider.States);

        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(2, calls);
        Assert.Same(tree.Root, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.Hover, tree.Root.States & PseudoStates.Hover);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Hover);
    }

    /// <summary>A drag to a point over <see cref="SliderTree.Lead"/>, left of the slider, gives the minimum.</summary>
    [Fact]
    public void ADragPastTheLeftEndGivesTheMinimum()
    {
        SliderTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverLead));
        tree.Context.Update(0f);

        Assert.Equal(0f, tree.Slider.Value);
        Assert.Same(tree.Lead, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.Hover, tree.Lead.States & PseudoStates.Hover);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Hover);
    }

    /// <summary>A drag to (100, 60), below the slider and over the root, reads the x only and gives the value 50.</summary>
    [Fact]
    public void ADragAboveOrBelowTheSliderReadsTheXOnly()
    {
        SliderTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(100f, 60f)));
        tree.Context.Update(0f);

        Assert.Equal(50f, tree.Slider.Value);
        Assert.Same(tree.Root, tree.Context.PointerHit);
    }

    /// <summary>
    /// A captured move keeps <see cref="PseudoStates.Hover"/>, <see cref="UiContext.PointerHit"/>, and the routing gate
    /// on the hit. A move over nothing gives a null hit and a gate of <see langword="false"/>, and it still drags.
    /// </summary>
    [Fact]
    public void ACapturedMoveKeepsHoverAndTheGateOnTheHit()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverOther));
        tree.Context.Update(0f);

        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(2, calls);
        Assert.Same(tree.Other, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.Hover, tree.Other.States);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Hover);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);

        // A set from code calls nothing. The next move then shows the drag by a change of the value.
        tree.Slider.Value = 25f;
        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverNothing));
        tree.Context.Update(0f);

        Assert.Null(tree.Context.PointerHit);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(PseudoStates.None, tree.Other.States);
        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(3, calls);
        Assert.Same(tree.Slider, tree.Context.ActiveElement);
    }

    /// <summary>
    /// A captured move targets the slider and walks its path: the capture handler of the root runs, and the bubble
    /// handler of <see cref="SliderTree.Other"/> gets no call.
    /// </summary>
    [Fact]
    public void ACapturedMoveWalksThePathOfTheSlider()
    {
        SliderTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);

        int rootCaptureMoves = 0;
        int otherBubbleMoves = 0;
        Element? target = null;
        tree.Root.OnPointerMoveCapture = (ref PointerEvent e) =>
        {
            rootCaptureMoves++;
            target = e.Header.Target;
        };
        tree.Other.OnPointerMove = (ref PointerEvent e) => otherBubbleMoves++;

        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverOther));
        tree.Context.Update(0f);

        Assert.Same(tree.Slider, target);
        Assert.Equal(1, rootCaptureMoves);
        Assert.Equal(0, otherBubbleMoves);
        Assert.Equal(100f, tree.Slider.Value);
        Assert.Same(tree.Other, tree.Context.PointerHit);
    }

    /// <summary>
    /// The release of the last button ends the capture. After a release over <see cref="SliderTree.Other"/>, a move over
    /// <see cref="SliderTree.Other"/> walks to <see cref="SliderTree.Other"/> and does not change the value.
    /// </summary>
    [Fact]
    public void TheCaptureEndsAtTheRelease()
    {
        SliderTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverOther));
        tree.Context.PushEvent(PointerTree.Up(SliderTree.OverOther, PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Equal(100f, tree.Slider.Value);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Active);

        int otherMoves = 0;
        Element? target = null;
        tree.Other.OnPointerMove = (ref PointerEvent e) =>
        {
            otherMoves++;
            target = e.Header.Target;
        };

        // A set from code calls nothing. A captured move over Other gives 100 again, so the old value shows a capture.
        tree.Slider.Value = 25f;
        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverOther + new float2(5f, 5f)));
        tree.Context.Update(0f);

        Assert.Equal(1, otherMoves);
        Assert.Same(tree.Other, target);
        Assert.Equal(25f, tree.Slider.Value);
    }

    /// <summary>
    /// An element with no capture keeps the hit rule: after a left press on <see cref="SliderTree.Lead"/>, a move over
    /// <see cref="SliderTree.Other"/> walks to <see cref="SliderTree.Other"/>.
    /// </summary>
    [Fact]
    public void AnElementWithNoCaptureKeepsTheHitRule()
    {
        SliderTree tree = new();
        tree.Settle();

        int leadMoves = 0;
        int otherMoves = 0;
        Element? target = null;
        tree.Lead.OnPointerMove = (ref PointerEvent e) => leadMoves++;
        tree.Other.OnPointerMove = (ref PointerEvent e) =>
        {
            otherMoves++;
            target = e.Header.Target;
        };

        tree.Context.PushEvent(PointerTree.Down(SliderTree.OverLead, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverOther));
        tree.Context.Update(0f);

        Assert.Same(tree.Lead, tree.Context.ActiveElement);
        Assert.Equal(0, leadMoves);
        Assert.Equal(1, otherMoves);
        Assert.Same(tree.Other, target);
        Assert.Equal(0f, tree.Slider.Value);
    }

    /// <summary>
    /// A right press on the slider captures no move: a move over <see cref="SliderTree.Other"/> walks to
    /// <see cref="SliderTree.Other"/>, and the value stays.
    /// </summary>
    [Fact]
    public void ARightPressCapturesNoMove()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 25f });
        tree.Settle();

        int otherMoves = 0;
        Element? target = null;
        tree.Other.OnPointerMove = (ref PointerEvent e) =>
        {
            otherMoves++;
            target = e.Header.Target;
        };

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(60f), PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverOther));
        tree.Context.Update(0f);

        Assert.Same(tree.Slider, tree.Context.ActiveElement);
        Assert.Equal(1, otherMoves);
        Assert.Same(tree.Other, target);
        Assert.Equal(25f, tree.Slider.Value);
    }

    /// <summary>
    /// A removed or hidden slider ends the capture. A remove between two updates and a remove from a handler of the
    /// same drain both send the next move to the hit, with no throw. <see cref="Display.None"/> on the slider or on an
    /// ancestor also sends the next move to the hit, and the value stays.
    /// </summary>
    [Fact]
    public void ARemovedSliderEndsTheCapture()
    {
        // The remove between two updates: the stale rule drops Active at the start of the next drain.
        SliderTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);
        Assert.Same(tree.Slider, tree.Context.ActiveElement);

        Assert.True(tree.Root.Children.Remove(tree.Slider));

        int otherMoves = 0;
        Element? target = null;
        tree.Other.OnPointerMove = (ref PointerEvent e) =>
        {
            otherMoves++;
            target = e.Header.Target;
        };

        tree.Context.PushEvent(PointerTree.Move(SliderTree.OverOther));
        tree.Context.Update(0f);

        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(1, otherMoves);
        Assert.Same(tree.Other, target);
        Assert.Equal(25f, tree.Slider.Value);

        // The remove from a handler of the same drain: the press removes the slider, and the next move of that drain
        // walks to the hit. The stale rule runs at the start of the drain only, so the capture branch checks the roots.
        SliderTree inDrain = new();
        inDrain.Settle();
        inDrain.Slider.ValueChanged = s => inDrain.Root.Children.Remove(s);

        int inDrainMoves = 0;
        Element? inDrainTarget = null;
        inDrain.Other.OnPointerMove = (ref PointerEvent e) =>
        {
            inDrainMoves++;
            inDrainTarget = e.Header.Target;
        };

        inDrain.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        inDrain.Context.PushEvent(PointerTree.Move(SliderTree.OverOther));
        inDrain.Context.Update(0f);

        Assert.Null(inDrain.Slider.Parent);
        Assert.Equal(1, inDrainMoves);
        Assert.Same(inDrain.Other, inDrainTarget);
        Assert.Equal(25f, inDrain.Slider.Value);

        // The hidden slider: Display.None on the slider during the press. The capture branch checks the shown rule.
        SliderTree hidden = new();
        hidden.Settle();
        AssertAHiddenSliderEndsTheCapture(hidden, hidden.Slider);

        // The hidden ancestor: the test puts the slider in a panel with no size, at the same index, so the rects stay.
        // Then Display.None goes on the panel only.
        SliderTree inPanel = new();
        Element panel = new() { Name = "panel" };
        Assert.True(inPanel.Root.Children.Remove(inPanel.Slider));
        panel.Children.Add(inPanel.Slider);
        inPanel.Root.Children.Insert(1, panel);
        inPanel.Settle();
        AssertAHiddenSliderEndsTheCapture(inPanel, panel);
    }

    /// <summary>
    /// Presses the slider at the value 25, hides an element, and moves to the x of the value 60. The hidden element
    /// takes no space, so <see cref="SliderTree.Other"/> moves to the x 40 and holds that point. The move must walk to
    /// <see cref="SliderTree.Other"/>, and the value must stay at 25 with 1 call of
    /// <see cref="Slider.ValueChanged"/>. With no shown check, the slider gets the move, finds a travel of 0, and goes
    /// to the minimum.
    /// </summary>
    /// <param name="tree">The settled tree.</param>
    /// <param name="hide">The slider or an ancestor of the slider.</param>
    private static void AssertAHiddenSliderEndsTheCapture(SliderTree tree, Element hide)
    {
        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);
        Assert.Same(tree.Slider, tree.Context.ActiveElement);
        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);

        // The update computes the style and the layout, so the next drain reads the hidden state.
        hide.Inline.Display = Display.None;
        tree.Context.Update(0f);

        int otherMoves = 0;
        Element? target = null;
        tree.Other.OnPointerMove = (ref PointerEvent e) =>
        {
            otherMoves++;
            target = e.Header.Target;
        };

        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Same(tree.Other, tree.Context.PointerHit);
        Assert.Equal(1, otherMoves);
        Assert.Same(tree.Other, target);
        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);
    }
}
