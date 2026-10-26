using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the press path of <see cref="Slider"/>: a left press on the slider moves <see cref="Slider.Value"/> to the
/// pointer and calls <see cref="Slider.ValueChanged"/> one time, and a code set calls nothing.
/// </summary>
/// <remarks>
/// Each test drives the slider through <see cref="UiContext.PushEvent(in PointerEvent)"/> with no caller target. The
/// scene is <see cref="SliderTree"/>: the value <c>v</c> of the range 0 to 100 sits at the x <c>50 + v</c>.
/// </remarks>
public sealed class SliderPressTests
{
    /// <summary>The fill of the type rule of <see cref="ATypeRuleMatchesTheSlider"/>.</summary>
    private static readonly Rgba8 SliderFill = new(32, 160, 224, 255);

    /// <summary>
    /// The demo of phase 2. A left press at x 75 over a slider of 0 to 100 sets the value 25 and calls
    /// <see cref="Slider.ValueChanged"/> one time with the slider as the sender.
    /// </summary>
    [Fact]
    public void APressMovesTheValueToThePointer()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        float seen = -1f;
        Slider? sender = null;
        tree.Slider.ValueChanged = s =>
        {
            calls++;
            seen = s.Value;
            sender = s;
        };

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press at x " + SliderTree.At(25f).X + ", Value = " + tree.Slider.Value + ", calls = " + calls);

        Assert.Equal(75f, SliderTree.At(25f).X);
        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);
        Assert.Equal(25f, seen);
        Assert.Same(tree.Slider, sender);
    }

    /// <summary>A press left of the thumb travel gives the minimum.</summary>
    [Fact]
    public void APressBeforeTheTrackGivesTheMinimum()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 50f });
        tree.Settle();
        Assert.Equal(50f, tree.Slider.Value);

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(new float2(45f, 10f), PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Equal(0f, tree.Slider.Value);
        Assert.Equal(1, calls);
    }

    /// <summary>A press right of the thumb travel gives the maximum.</summary>
    [Fact]
    public void APressPastTheTrackGivesTheMaximum()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(new float2(155f, 10f), PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(1, calls);
    }

    /// <summary>A press that maps to the current value calls nothing and marks no paint.</summary>
    [Fact]
    public void APressAtTheSameValueCallsNothing()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 40f });
        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.Slider.Dirty & DirtyFlags.Paint);

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(40f), PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Equal(40f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(DirtyFlags.None, tree.Slider.Dirty & DirtyFlags.Paint);
    }

    /// <summary>
    /// A press that is not a left press with no chord on the slider itself does nothing, and the press walks on to the
    /// root (Decision 13). Four cases: a right press, a left press after a right press on the slider (a chord), a left
    /// press on a pickable child of the slider, and a left press with the slider as the caller target. Each case gives
    /// no move and no call, and the root bubble handler gets the press.
    /// </summary>
    [Fact]
    public void ARightButtonPressDoesNothing()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootBubbleDowns = 0;
        tree.Root.OnPointerDown = (ref PointerEvent e) => rootBubbleDowns++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Right));
        tree.Context.Update(0f);

        Assert.Equal(0f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootBubbleDowns);

        // A chord: the left press follows a right press on the slider, so it has no press target.
        SliderTree chord = new();
        chord.Settle();
        int chordCalls = 0;
        chord.Slider.ValueChanged = s => chordCalls++;
        int chordBubbleDowns = 0;
        chord.Root.OnPointerDown = (ref PointerEvent e) => chordBubbleDowns++;

        chord.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Right));
        chord.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        Assert.Equal(2, chord.Context.DrainEvents());

        Assert.Equal(0f, chord.Slider.Value);
        Assert.Equal(0, chordCalls);
        Assert.Equal(2, chordBubbleDowns);

        // A press on a pickable child of the slider: the child is the press target, not the slider.
        SliderTree child = new();
        Element inner = new() { Name = "inner", IsPickable = true };
        inner.Inline.Width = 100;
        inner.Inline.Height = 20;
        child.Slider.Children.Add(inner);
        child.Settle();
        Assert.Equal(new float4(40f, 0f, 100f, 20f), PointerTree.ViewportRect(inner));
        int childCalls = 0;
        child.Slider.ValueChanged = s => childCalls++;
        int childBubbleDowns = 0;
        child.Root.OnPointerDown = (ref PointerEvent e) => childBubbleDowns++;

        child.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        Assert.Equal(1, child.Context.DrainEvents());

        Assert.Equal(0f, child.Slider.Value);
        Assert.Equal(0, childCalls);
        Assert.Equal(1, childBubbleDowns);

        // A left press with the slider as the caller target: the drain gives it no press target.
        SliderTree caller = new();
        caller.Settle();
        int callerCalls = 0;
        caller.Slider.ValueChanged = s => callerCalls++;
        int callerBubbleDowns = 0;
        caller.Root.OnPointerDown = (ref PointerEvent e) => callerBubbleDowns++;

        caller.Context.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerDown, 0d, caller.Slider),
            Position = SliderTree.At(25f),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
        Assert.Equal(1, caller.Context.DrainEvents());

        Assert.Equal(0f, caller.Slider.Value);
        Assert.Equal(0, callerCalls);
        Assert.Equal(1, callerBubbleDowns);
    }

    /// <summary>A moving press stops the walk: the capture handler of the root runs, and the bubble handler does not.</summary>
    [Fact]
    public void APressStopsTheWalk()
    {
        SliderTree tree = new();
        tree.Settle();

        int rootCaptureDowns = 0;
        int rootBubbleDowns = 0;
        tree.Root.OnPointerDownCapture = (ref PointerEvent e) => rootCaptureDowns++;
        tree.Root.OnPointerDown = (ref PointerEvent e) => rootBubbleDowns++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, rootCaptureDowns);
        Assert.Equal(0, rootBubbleDowns);
    }

    /// <summary>A left press focuses the slider.</summary>
    [Fact]
    public void APressFocusesTheSlider()
    {
        SliderTree tree = new();
        tree.Settle();
        Assert.True(tree.Slider.IsFocusable);
        Assert.Null(tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Same(tree.Slider, tree.Context.FocusedElement);
    }

    /// <summary>A set from code clamps the number, marks the paint on a change, and calls nothing.</summary>
    [Fact]
    public void ACodeSetClampsAndCallsNothing()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Slider.Value = 150f;
        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(DirtyFlags.Paint, tree.Slider.Dirty & DirtyFlags.Paint);
        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.Slider.Dirty & DirtyFlags.Paint);

        tree.Slider.Value = 150f;
        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(DirtyFlags.None, tree.Slider.Dirty & DirtyFlags.Paint);

        tree.Slider.Value = -5f;
        Assert.Equal(0f, tree.Slider.Value);
        Assert.Equal(DirtyFlags.Paint, tree.Slider.Dirty & DirtyFlags.Paint);
        tree.Settle();

        Assert.Equal(0, calls);
    }

    /// <summary>The create-info sets the range and the start value, clamps a start value, and calls nothing.</summary>
    [Fact]
    public void TheCreateInfoSetsTheRangeAndTheStartValue()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 10f, Maximum = 20f, Value = 50f });
        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        tree.Settle();

        Assert.Equal(10f, tree.Slider.Minimum);
        Assert.Equal(20f, tree.Slider.Maximum);
        Assert.Equal(20f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(ElementType.Intern("Slider"), tree.Slider.TypeId);

        Slider low = new(new Slider.CreateInfo { Minimum = 10f, Maximum = 20f, Value = -3f });
        Assert.Equal(10f, low.Value);

        Slider plain = new(new Slider.CreateInfo { Maximum = 8f });
        Assert.Equal(0f, plain.Minimum);
        Assert.Equal(0f, plain.Value);
    }

    /// <summary>A style rule with the type <c>Slider</c> matches the slider and not another element.</summary>
    [Fact]
    public void ATypeRuleMatchesTheSlider()
    {
        SliderTree tree = new();
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = ElementType.Intern("Slider") }),
                new StyleProperties { BackgroundColor = SliderFill })
            .Build();
        tree.Context.AddSheet(sheet);
        tree.Settle();

        Assert.Equal(SliderFill, tree.Slider.Style.BackgroundColor);
        Assert.NotEqual(SliderFill, tree.Other.Style.BackgroundColor);
    }
}
