using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the drag path of <see cref="Slider"/>: a pointer move while the slider holds the left press moves
/// <see cref="Slider.Value"/> to the pointer. A move with no press, after the release, after a press on another
/// element, after a right press, or after a second button gives no drag.
/// </summary>
/// <remarks>
/// Each test drives the slider through <see cref="UiContext.PushEvent(in PointerEvent)"/> with no caller target. The
/// scene is <see cref="SliderTree"/>: the value <c>v</c> of the range 0 to 100 sits at the x <c>50 + v</c>. Each move
/// goes over the slider. <c>DragCaptureTests</c> holds the moves outside the slider.
/// </remarks>
public sealed class SliderDragTests
{
    /// <summary>
    /// The demo of phase 4. A left press at the value 25 and a move to the value 60 give the value 60 and two calls of
    /// <see cref="Slider.ValueChanged"/>.
    /// </summary>
    [Fact]
    public void AMoveAfterAPressDragsTheValue()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press at 25, move to 60, Value = " + tree.Slider.Value + ", calls = " + calls);

        Assert.Equal(60f, tree.Slider.Value);
        Assert.Equal(2, calls);
    }

    /// <summary>A drag move to the same value calls nothing, and the move still stops the walk.</summary>
    [Fact]
    public void AMoveToTheSameValueCallsNothing()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootBubbleMoves = 0;
        tree.Root.OnPointerMove = (ref PointerEvent e) => rootBubbleMoves++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);
        Assert.Equal(1, calls);

        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(25f)));
        tree.Context.Update(0f);

        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);
        Assert.Equal(0, rootBubbleMoves);
    }

    /// <summary>A hover move with no press changes nothing, and the move walks on to the root.</summary>
    [Fact]
    public void AMoveWithNoPressDoesNothingAndWalksOn()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootBubbleMoves = 0;
        tree.Root.OnPointerMove = (ref PointerEvent e) => rootBubbleMoves++;

        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Equal(0f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootBubbleMoves);
    }

    /// <summary>A move after the release of the left button changes nothing.</summary>
    [Fact]
    public void AMoveAfterTheReleaseDoesNothing()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// A move that the left press of the slider does not route does nothing. The move follows a press elsewhere, or
    /// the move has the slider as the caller target.
    /// </summary>
    [Fact]
    public void APressElsewhereThenAMoveOverTheSliderDoesNothing()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.OverLead, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Equal(0f, tree.Slider.Value);
        Assert.Equal(0, calls);

        // A fresh tree: the slider holds the left press, and the move has the slider as the caller target. The drain
        // gives a null press target to such an entry, so the slider does not drag.
        SliderTree targeted = new();
        targeted.Settle();

        int targetedCalls = 0;
        targeted.Slider.ValueChanged = s => targetedCalls++;

        targeted.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        targeted.Context.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerMove, 0d, targeted.Slider),
            Position = SliderTree.At(60f),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
        targeted.Context.Update(0f);

        Assert.Equal(25f, targeted.Slider.Value);
        Assert.Equal(1, targetedCalls);
    }

    /// <summary>A right press gives no drag, and a move over the slider walks on to the root.</summary>
    [Fact]
    public void ARightPressThenAMoveDoesNotDrag()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootBubbleMoves = 0;
        tree.Root.OnPointerMove = (ref PointerEvent e) => rootBubbleMoves++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Equal(0f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootBubbleMoves);
    }

    /// <summary>
    /// A second button ends the drag until the last release. A new left press then starts a new drag.
    /// </summary>
    [Fact]
    public void ASecondButtonEndsTheDrag()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);

        tree.Context.PushEvent(PointerTree.Up(SliderTree.At(60f), PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(SliderTree.At(60f), PointerButton.Left));
        tree.Context.Update(0f);

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(10f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Equal(60f, tree.Slider.Value);
        Assert.Equal(3, calls);
    }

    /// <summary>A drag move stops the walk: the capture handler of the root runs, and the bubble handler does not.</summary>
    [Fact]
    public void ADragMoveStopsTheWalk()
    {
        SliderTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);

        int rootCaptureMoves = 0;
        int rootBubbleMoves = 0;
        tree.Root.OnPointerMoveCapture = (ref PointerEvent e) => rootCaptureMoves++;
        tree.Root.OnPointerMove = (ref PointerEvent e) => rootBubbleMoves++;

        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.Update(0f);

        Assert.Equal(60f, tree.Slider.Value);
        Assert.Equal(1, rootCaptureMoves);
        Assert.Equal(0, rootBubbleMoves);
    }

    /// <summary>The delegate reads the new value in each call, and it gets the slider as the sender.</summary>
    [Fact]
    public void TheDelegateReadsTheNewValue()
    {
        SliderTree tree = new();
        tree.Settle();

        float[] seen = new float[4];
        int calls = 0;
        Slider? sender = null;
        tree.Slider.ValueChanged = s =>
        {
            seen[calls++] = s.Value;
            sender = s;
        };

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(80f)));
        tree.Context.Update(0f);

        Assert.Equal(3, calls);
        Assert.Equal(25f, seen[0]);
        Assert.Equal(60f, seen[1]);
        Assert.Equal(80f, seen[2]);
        Assert.Same(tree.Slider, sender);
    }
}
