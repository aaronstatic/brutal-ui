using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the value rule of <see cref="Slider"/> (Decision 4): a NaN gives the minimum, the number is clamped, and a
/// step above 0 rounds it to the grid that starts at the minimum. Tests of <see cref="Slider.SetRange"/> and of the range
/// check (Decision 9) are here too.
/// </summary>
/// <remarks>
/// The tests write the value through the code set, the create-info, the press, and the drag. The scene is
/// <see cref="SliderTree"/>: the value <c>v</c> of the range 0 to 100 sits at the x <c>50 + v</c>.
/// </remarks>
public sealed class SliderStepTests
{
    /// <summary>The tolerance of a value on a fractional grid. The grid point is <c>k * step</c> in float.</summary>
    private const float GridTolerance = 1e-6f;

    /// <summary>Creates a slider with a range, a step, and a start value.</summary>
    private static Slider Make(float minimum, float maximum, float step, float value = 0f)
    {
        return new Slider(new Slider.CreateInfo { Minimum = minimum, Maximum = maximum, Step = step, Value = value });
    }

    /// <summary>A set rounds to the nearest step, and a midpoint rounds up.</summary>
    [Fact]
    public void ASetRoundsToTheNearestStep()
    {
        Slider slider = Make(0f, 100f, 10f);
        Assert.Equal(10f, slider.Step);

        slider.Value = 23f;
        Assert.Equal(20f, slider.Value);

        slider.Value = 25f;
        Assert.Equal(30f, slider.Value);

        slider.Value = 27f;
        Assert.Equal(30f, slider.Value);

        // The create-info follows the same rule.
        Assert.Equal(20f, Make(0f, 100f, 10f, 22f).Value);
    }

    /// <summary>The grid starts at the minimum, not at 0.</summary>
    [Fact]
    public void TheGridStartsAtTheMinimum()
    {
        Slider slider = Make(5f, 105f, 10f);

        slider.Value = 12f;
        Assert.Equal(15f, slider.Value);

        slider.Value = 9f;
        Assert.Equal(5f, slider.Value);
    }

    /// <summary>A step of 0 gives no grid, so the value stays as set.</summary>
    [Fact]
    public void AStepOfZeroKeepsTheValue()
    {
        Slider slider = Make(0f, 100f, 0f);

        slider.Value = 33.3f;

        Assert.Equal(33.3f, slider.Value);
    }

    /// <summary>A NaN gives the minimum, with or without a step.</summary>
    [Fact]
    public void ANaNValueGivesTheMinimum()
    {
        Slider plain = Make(5f, 105f, 0f, 50f);
        plain.Value = float.NaN;
        Assert.Equal(5f, plain.Value);

        Slider stepped = Make(5f, 105f, 10f, 55f);
        stepped.Value = float.NaN;
        Assert.Equal(5f, stepped.Value);

        Assert.Equal(5f, Make(5f, 105f, 10f, float.NaN).Value);
    }

    /// <summary>
    /// A range that is no multiple of the step reaches the maximum only when the nearest grid point is at or past it.
    /// </summary>
    [Fact]
    public void ARangeThatIsNoMultipleOfTheStep()
    {
        Slider three = Make(0f, 10f, 3f);
        three.Value = 10f;
        Assert.Equal(9f, three.Value);

        Slider four = Make(0f, 10f, 4f);
        four.Value = 10f;
        Assert.Equal(10f, four.Value);
    }

    /// <summary>A fractional step gives the grid, and the last grid point is the maximum.</summary>
    [Fact]
    public void AFractionalStepGivesTheGrid()
    {
        Slider slider = Make(0f, 1f, 0.1f);

        slider.Value = 0.34f;
        Assert.Equal(0.3f, slider.Value, GridTolerance);

        slider.Value = 1f;
        Assert.Equal(1f, slider.Value, GridTolerance);
    }

    /// <summary>
    /// A press and a drag round to the step. A press at 23 gives 20 and one call. A move to 24 gives the same value and
    /// no call.
    /// </summary>
    [Fact]
    public void APressAndADragRoundToTheStep()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = 10f });
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(23f), PointerButton.Left));
        tree.Context.Update(0f);

        Assert.Equal(20f, tree.Slider.Value);
        Assert.Equal(1, calls);

        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(24f)));
        tree.Context.Update(0f);

        Assert.Equal(20f, tree.Slider.Value);
        Assert.Equal(1, calls);

        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(36f)));
        tree.Context.Update(0f);

        Assert.Equal(40f, tree.Slider.Value);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// <see cref="Slider.SetRange"/> clamps and rounds the old value, marks the paint on a change, and calls nothing.
    /// The same range again marks nothing.
    /// </summary>
    [Fact]
    public void SetRangeClampsAndRoundsAndCallsNothing()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        tree.Slider.Value = 73f;
        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.Slider.Dirty & DirtyFlags.Paint);

        tree.Slider.SetRange(0f, 50f, 5f);

        Assert.Equal(0f, tree.Slider.Minimum);
        Assert.Equal(50f, tree.Slider.Maximum);
        Assert.Equal(5f, tree.Slider.Step);
        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(DirtyFlags.Paint, tree.Slider.Dirty & DirtyFlags.Paint);
        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.Slider.Dirty & DirtyFlags.Paint);

        tree.Slider.SetRange(0f, 50f, 5f);
        Assert.Equal(DirtyFlags.None, tree.Slider.Dirty & DirtyFlags.Paint);

        // A new range with the same value still marks the paint, because the thumb position changes.
        tree.Slider.SetRange(0f, 200f, 5f);
        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(DirtyFlags.Paint, tree.Slider.Dirty & DirtyFlags.Paint);
        tree.Settle();

        Assert.Equal(0, calls);
    }

    /// <summary>
    /// A bad range throws <see cref="ArgumentOutOfRangeException"/> with the name of the bad parameter. After a throw of
    /// <see cref="Slider.SetRange"/>, the range and the value stay.
    /// </summary>
    [Fact]
    public void ABadRangeThrowsAndKeepsTheState()
    {
        Assert.Equal("info", ConstructorThrows(10f, 5f, 0f));
        Assert.Equal("info", ConstructorThrows(float.NaN, 5f, 0f));
        Assert.Equal("info", ConstructorThrows(float.NegativeInfinity, 5f, 0f));
        Assert.Equal("info", ConstructorThrows(0f, float.NaN, 0f));
        Assert.Equal("info", ConstructorThrows(0f, float.PositiveInfinity, 0f));
        Assert.Equal("info", ConstructorThrows(0f, 10f, -1f));
        Assert.Equal("info", ConstructorThrows(0f, 10f, float.NaN));
        Assert.Equal("info", ConstructorThrows(0f, 10f, float.PositiveInfinity));
        Assert.Equal("info", ConstructorThrows(-3e38f, 3e38f, 0f));

        Slider slider = Make(0f, 100f, 10f, 40f);
        Assert.Equal("minimum", SetRangeThrows(slider, 10f, 5f, 0f));
        Assert.Equal("minimum", SetRangeThrows(slider, float.NaN, 5f, 0f));
        Assert.Equal("minimum", SetRangeThrows(slider, float.NegativeInfinity, 5f, 0f));
        Assert.Equal("maximum", SetRangeThrows(slider, 0f, float.NaN, 0f));
        Assert.Equal("maximum", SetRangeThrows(slider, 0f, float.PositiveInfinity, 0f));
        Assert.Equal("step", SetRangeThrows(slider, 0f, 10f, -1f));
        Assert.Equal("step", SetRangeThrows(slider, 0f, 10f, float.NaN));
        Assert.Equal("step", SetRangeThrows(slider, 0f, 10f, float.PositiveInfinity));
        Assert.Equal("maximum", SetRangeThrows(slider, -3e38f, 3e38f, 0f));

        Assert.Equal(0f, slider.Minimum);
        Assert.Equal(100f, slider.Maximum);
        Assert.Equal(10f, slider.Step);
        Assert.Equal(40f, slider.Value);

        // A zero range is valid, and the value is the minimum.
        slider.SetRange(7f, 7f, 0f);
        Assert.Equal(7f, slider.Value);
    }

    /// <summary>Runs the constructor with a bad range and gives the name of the bad parameter.</summary>
    private static string? ConstructorThrows(float minimum, float maximum, float step)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => Make(minimum, maximum, step));
        return ex.ParamName;
    }

    /// <summary>Runs <see cref="Slider.SetRange"/> with a bad range and gives the name of the bad parameter.</summary>
    private static string? SetRangeThrows(Slider slider, float minimum, float maximum, float step)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => slider.SetRange(minimum, maximum, step));
        return ex.ParamName;
    }
}
