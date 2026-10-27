using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of a <see cref="Slider"/> inside a scrolled <see cref="ScrollView"/>: the hit walk and
/// <see cref="WidgetInput.ViewportOrigin(Element)"/> apply the offset of the view, so a press and a drag map the pointer
/// to the value under the pointer in the draw.
/// </summary>
/// <remarks>
/// The scene is <see cref="ScrollViewTree"/>: the slider is 120 x 20 at (0, 40) of the view, with the range 0 to 100 and
/// a thumb of 20 x 20. <see cref="ScrollViewTree.SliderAt(float, float2)"/> gives the point of a value at an offset. The
/// slider map computes in <c>double</c>, so each test asserts the value with no tolerance. The tests intern no class.
/// </remarks>
public sealed class ScrollViewSliderTests
{
    /// <summary>The offset of most tests.</summary>
    private static readonly float2 Offset = new(20f, 20f);

    /// <summary>
    /// The demo of phase 2. At the offset (20, 20), a left press at (15, 30), the point of the value 25, gives the value
    /// 25 and one <see cref="Slider.ValueChanged"/> call. The offset stays (20, 20), and
    /// <see cref="ScrollView.ScrollChanged"/> gets no call.
    /// </summary>
    [Fact]
    public void APressOnASliderInAScrolledViewMapsThroughTheOffset()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = Offset;
        int valueCalls = 0;
        tree.Slider.ValueChanged = _ => valueCalls++;
        int scrollCalls = 0;
        tree.View.ScrollChanged = _ => scrollCalls++;

        float2 press = ScrollViewTree.SliderAt(25f, Offset);
        Assert.Equal(new float2(15f, 30f), press);
        tree.Context.PushEvent(PointerTree.Down(press, PointerButton.Left));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: offset (20, 20), press at (15, 30), Value = " + tree.Slider.Value + ", calls = " + valueCalls
            + ", offset = (" + tree.View.ScrollOffset.X + ", " + tree.View.ScrollOffset.Y + "), scroll calls = "
            + scrollCalls);

        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, valueCalls);
        Assert.Equal(Offset, tree.View.ScrollOffset);
        Assert.Equal(0, scrollCalls);
        Assert.Same(tree.Slider, tree.Context.PointerHit);
    }

    /// <summary>
    /// At the offset (20, 20), a press at the value 25 and a move to the point of the value 60 give the value 60 and two
    /// calls.
    /// </summary>
    [Fact]
    public void ADragOfASliderInAScrolledViewFollowsThePointer()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = Offset;
        int calls = 0;
        tree.Slider.ValueChanged = _ => calls++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.SliderAt(25f, Offset), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(ScrollViewTree.SliderAt(60f, Offset)));
        tree.Context.Update(0f);

        Assert.Equal(60f, tree.Slider.Value);
        Assert.Equal(2, calls);
        Assert.Equal(Offset, tree.View.ScrollOffset);
    }

    /// <summary>
    /// At the offset (20, 20), a press at the value 25 and a move to (300, 30), past the right edge of the view, give the
    /// value 100. The drag capture keeps the move on the slider. The offset stays (20, 20), and
    /// <see cref="PseudoStates.Hover"/> is on the root.
    /// </summary>
    [Fact]
    public void ASliderDragPastTheViewEdgeStaysOnTheSlider()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = Offset;
        int scrollCalls = 0;
        tree.View.ScrollChanged = _ => scrollCalls++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.SliderAt(25f, Offset), PointerButton.Left));
        tree.Context.Update(0f);
        Assert.Equal(25f, tree.Slider.Value);

        tree.Context.PushEvent(PointerTree.Move(new float2(300f, 30f)));
        tree.Context.Update(0f);

        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(Offset, tree.View.ScrollOffset);
        Assert.Equal(0, scrollCalls);
        Assert.Same(tree.Root, tree.Context.PointerHit);
        Assert.NotEqual(PseudoStates.None, tree.Root.States & PseudoStates.Hover);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Hover);
    }

    /// <summary>
    /// At the offset (20, 0), a scroll with the delta (0, -0.5) over the thumb of the slider gives the offset (20, 20).
    /// The slider takes no wheel, so its value stays and it calls nothing.
    /// </summary>
    [Fact]
    public void AWheelOverTheSliderScrollsTheView()
    {
        ScrollViewTree tree = new();
        tree.Slider.Value = 25f;
        tree.Settle();
        float2 start = new(20f, 0f);
        tree.View.ScrollOffset = start;
        int valueCalls = 0;
        tree.Slider.ValueChanged = _ => valueCalls++;
        int scrollCalls = 0;
        tree.View.ScrollChanged = _ => scrollCalls++;

        float2 over = ScrollViewTree.SliderAt(25f, start);
        Assert.Same(tree.Slider, tree.Context.HitTest(over));
        tree.Context.PushEvent(PointerTree.Scroll(over, new float2(0f, -0.5f)));
        tree.Context.Update(0f);

        Assert.Equal(Offset, tree.View.ScrollOffset);
        Assert.Equal(1, scrollCalls);
        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(0, valueCalls);
    }

    /// <summary>
    /// At the offset 0, a scroll of (-0.5, 0) over <see cref="ScrollViewTree.Row0"/> and a left press at (15, 50) in one
    /// update give the offset (20, 0) and the value 25. The press reads the new offset. The old offset gives the value 5.
    /// </summary>
    [Fact]
    public void AScrollAndAPressInOneDrainUseTheNewOffset()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int valueCalls = 0;
        tree.Slider.ValueChanged = _ => valueCalls++;
        int scrollCalls = 0;
        tree.View.ScrollChanged = _ => scrollCalls++;

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(-0.5f, 0f)));
        tree.Context.PushEvent(PointerTree.Down(new float2(15f, 50f), PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Equal(new float2(20f, 0f), tree.View.ScrollOffset);
        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, scrollCalls);
        Assert.Equal(1, valueCalls);
    }

    /// <summary>
    /// At the offset (20, 20), <see cref="WidgetInput.ViewportOrigin(Element)"/> gives (-20, 20) for the slider, (0, 0)
    /// for the view, and (-20, -20) for <see cref="ScrollViewTree.Row0"/>. The view does not subtract its own offset.
    /// </summary>
    [Fact]
    public void TheOriginSubtractsTheAncestorOffsetOnly()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = Offset;

        Assert.Equal(new float2(-20f, 20f), WidgetInput.ViewportOrigin(tree.Slider));
        Assert.Equal(new float2(0f, 0f), WidgetInput.ViewportOrigin(tree.View));
        Assert.Equal(new float2(-20f, -20f), WidgetInput.ViewportOrigin(tree.Row0));
        Assert.Equal(new float2(0f, 0f), WidgetInput.ViewportOrigin(tree.Root));
        Assert.Equal(new float2(0f, 100f), WidgetInput.ViewportOrigin(tree.Outside));
    }
}
