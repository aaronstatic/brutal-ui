using System;
using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of phase 3 of the transitions: the four easing curves, the duration rule, and the rule of the delta time.
/// </summary>
/// <remarks>
/// <para>
/// The class table and the type table are global and xUnit runs test classes in parallel. Each test interns its names
/// with the prefix <c>nameof(Test) + "."</c>, and no test asserts a fixed id value.
/// </para>
/// <para>
/// The scene of the pass tests is one button with an opacity of 0 and a hover rule with an opacity of 1. The opacity
/// is the easiest member to read, because it holds the part of the change.
/// </para>
/// </remarks>
public sealed class TransitionCurveTests
{
    private const float Duration = 0.2f;
    private const float StepTime = 0.1f;

    private static readonly TransitionEasing[] Easings =
    [
        TransitionEasing.Linear,
        TransitionEasing.EaseIn,
        TransitionEasing.EaseOut,
        TransitionEasing.EaseInOut,
    ];

    [Fact]
    public void EachEasingShapesTheFade()
    {
        StyleSheet.Builder builder = new();
        Element root = new() { Name = "root" };
        Element[] buttons = new Element[Easings.Length];
        for (int i = 0; i < Easings.Length; i++)
        {
            ClassId id = ClassName.Intern(nameof(EachEasingShapesTheFade) + "." + NameOf(Easings[i]));
            builder.Add(
                Selector.ForClass(id),
                new StyleProperties { Opacity = 0f },
                Fade(Duration, Easings[i]));
            builder.Add(Hover(id), new StyleProperties { Opacity = 1f });

            buttons[i] = new Element { Name = NameOf(Easings[i]) };
            buttons[i].Classes.Add(id);
            root.Children.Add(buttons[i]);
        }

        UiContext context = NewContext(root, builder.Build());
        context.Update(0f);
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].States = PseudoStates.Hover;
        }

        context.Update(StepTime);

        string[] lines = new string[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            lines[i] = NameOf(Easings[i]) + " " + buttons[i].Style.Opacity.ToString("F3", CultureInfo.InvariantCulture);
            TestContext.Current.TestOutputHelper?.WriteLine(lines[i]);
        }

        Assert.Equal("linear 0.500", lines[0]);
        Assert.Equal("ease-in 0.315", lines[1]);
        Assert.Equal("ease-out 0.685", lines[2]);
        Assert.Equal("ease-in-out 0.500", lines[3]);
    }

    [Theory]
    [InlineData(TransitionEasing.Linear, 0f, 0f)]
    [InlineData(TransitionEasing.Linear, 0.5f, 0.5f)]
    [InlineData(TransitionEasing.Linear, 1f, 1f)]
    [InlineData(TransitionEasing.EaseIn, 0f, 0f)]
    [InlineData(TransitionEasing.EaseIn, 0.5f, 0.31536f)]
    [InlineData(TransitionEasing.EaseIn, 1f, 1f)]
    [InlineData(TransitionEasing.EaseOut, 0f, 0f)]
    [InlineData(TransitionEasing.EaseOut, 0.5f, 0.68464f)]
    [InlineData(TransitionEasing.EaseOut, 1f, 1f)]
    [InlineData(TransitionEasing.EaseInOut, 0f, 0f)]
    [InlineData(TransitionEasing.EaseInOut, 0.5f, 0.5f)]
    [InlineData(TransitionEasing.EaseInOut, 1f, 1f)]
    public void EachEasingAtZeroHalfAndOne(TransitionEasing easing, float progress, float expected)
    {
        float value = TransitionCurves.Evaluate(easing, progress);

        TestContext.Current.TestOutputHelper?.WriteLine(NameOf(easing) + " " + progress + " " + value);
        if (progress == 0.5f)
        {
            Assert.Equal(expected, value, 1e-4f);
        }
        else
        {
            // The ends are exact.
            Assert.Equal(expected, value);
        }
    }

    [Fact]
    public void EachCurveRisesAndStaysInTheRange()
    {
        for (int e = 0; e < Easings.Length; e++)
        {
            float previous = 0f;
            for (int i = 0; i <= 1000; i++)
            {
                float value = TransitionCurves.Evaluate(Easings[e], i / 1000f);
                Assert.InRange(value, 0f, 1f);
                Assert.True(value >= previous, NameOf(Easings[e]) + " falls at sample " + i);
                previous = value;
            }

            Assert.Equal(1f, previous);
        }
    }

    [Theory]
    [InlineData(TransitionEasing.EaseIn, 0.42f, 1f)]
    [InlineData(TransitionEasing.EaseOut, 0f, 0.58f)]
    [InlineData(TransitionEasing.EaseInOut, 0.42f, 0.58f)]
    public void TheSolverFindsTheParameterForEachProgress(TransitionEasing easing, float x1, float x2)
    {
        // Newton alone misses the tolerance near the flat end of EaseIn and EaseOut. The bisection must catch these values.
        const int count = 100000;
        float worst = 0f;
        float worstProgress = 0f;
        for (int i = 1; i < count; i++)
        {
            float progress = i / (float)count;
            float t = TransitionCurves.SolveParameter(x1, x2, progress);
            Assert.InRange(t, 0f, 1f);

            float error = MathF.Abs(TransitionCurves.Sample(x1, x2, t) - progress);
            if (error > worst)
            {
                worst = error;
                worstProgress = progress;
            }
        }

        TestContext.Current.TestOutputHelper?.WriteLine(
            NameOf(easing) + " worst x error " + worst.ToString("E3", CultureInfo.InvariantCulture)
            + " at " + worstProgress.ToString("F5", CultureInfo.InvariantCulture));
        Assert.True(worst < 1e-6f, NameOf(easing) + " misses the progress " + worstProgress + " by " + worst);
    }

    [Fact]
    public void AProgressOutsideTheRangeIsClamped()
    {
        for (int e = 0; e < Easings.Length; e++)
        {
            Assert.Equal(0f, TransitionCurves.Evaluate(Easings[e], -1f));
            Assert.Equal(1f, TransitionCurves.Evaluate(Easings[e], 2f));
            Assert.Equal(0f, TransitionCurves.Evaluate(Easings[e], float.NaN));
            Assert.Equal(1f, TransitionCurves.Evaluate(Easings[e], float.PositiveInfinity));
        }
    }

    /// <summary>Gives the durations that are not valid. The analyzer of InlineData treats 0 and -0 as one row.</summary>
    public static TheoryData<float> BadDurations { get; } = new()
    {
        0f,
        -0f,
        -1f,
        float.NaN,
        float.PositiveInfinity,
        float.NegativeInfinity,
    };

    [Theory]
    [MemberData(nameof(BadDurations))]
    public void EachBadDurationWritesTheTargetAtOnce(float duration)
    {
        (UiContext context, Element element) = HoverScene(nameof(EachBadDurationWritesTheTargetAtOnce), duration);

        element.States = PseudoStates.Hover;
        context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            duration.ToString(CultureInfo.InvariantCulture) + " " + element.Style.Opacity.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(1f, element.Style.Opacity);
        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(0, context.Transitions.Capacity);
    }

    [Fact]
    public void AnEpsilonDurationStartsATween()
    {
        (UiContext context, Element element) = HoverScene(nameof(AnEpsilonDurationStartsATween), float.Epsilon);

        element.States = PseudoStates.Hover;
        context.Update(0f);

        // The control row of the duration theory: a valid duration rents a record, and a delta time of 0 shows the start.
        Assert.Equal(0f, element.Style.Opacity);
        Assert.Equal(1, context.Transitions.ActiveCount);

        context.Update(StepTime);
        Assert.Equal(1f, element.Style.Opacity);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.NegativeInfinity)]
    public void ADeltaTimeOfZeroOrLessAdvancesNothing(float deltaTime)
    {
        (UiContext context, Element element) = HoverScene(nameof(ADeltaTimeOfZeroOrLessAdvancesNothing), Duration);

        element.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(0.5f, element.Style.Opacity, 1e-5f);

        context.Update(deltaTime);
        Assert.Equal(0.5f, element.Style.Opacity, 1e-5f);
        Assert.Equal(1, context.Transitions.ActiveCount);

        // The elapsed time stayed at 0.1 s, so one more step of 0.1 s ends the tween.
        context.Update(StepTime);
        Assert.Equal(1f, element.Style.Opacity);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void AnInfiniteDeltaTimeEndsEachTween()
    {
        (UiContext context, Element element) = HoverScene(nameof(AnInfiniteDeltaTimeEndsEachTween), Duration);

        element.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(1, context.Transitions.ActiveCount);

        context.Update(float.PositiveInfinity);

        Assert.Equal(1f, element.Style.Opacity);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void TheCurvesAllocateNothing()
    {
        float sink = 0f;
        for (int e = 0; e < Easings.Length; e++)
        {
            sink += TransitionCurves.Evaluate(Easings[e], 0.5f);
        }

        AllocationGate.ReleaseAllocationContext();

        for (int e = 0; e < Easings.Length; e++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                sink += TransitionCurves.Evaluate(Easings[e], i / 10000f);
            }

            long after = GC.GetAllocatedBytesForCurrentThread();
            Assert.True(after - before == 0, NameOf(Easings[e]) + " allocated " + (after - before) + " bytes");
        }

        Assert.True(sink > 0f);
    }

    /// <summary>
    /// Gives a context with one styled button. The button holds an opacity of 0, and a hover gives it an opacity of 1
    /// through a linear transition of one duration.
    /// </summary>
    private static (UiContext Context, Element Button) HoverScene(string test, float duration)
    {
        ClassId id = ClassName.Intern(test + ".button");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(id), new StyleProperties { Opacity = 0f }, Fade(duration, TransitionEasing.Linear))
            .Add(Hover(id), new StyleProperties { Opacity = 1f })
            .Build();

        Element root = new() { Name = "root" };
        Element button = new() { Name = "button" };
        button.Classes.Add(id);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        Assert.Equal(0f, button.Style.Opacity);
        return (context, button);
    }

    /// <summary>Gives the name of an easing as the CSS text.</summary>
    private static string NameOf(TransitionEasing easing)
    {
        return easing switch
        {
            TransitionEasing.Linear => "linear",
            TransitionEasing.EaseIn => "ease-in",
            TransitionEasing.EaseOut => "ease-out",
            TransitionEasing.EaseInOut => "ease-in-out",
            _ => throw new ArgumentOutOfRangeException(nameof(easing), easing, "The easing is not a member."),
        };
    }

    /// <summary>Gives a set with an opacity transition.</summary>
    private static StyleTransitions Fade(float duration, TransitionEasing easing)
    {
        StyleTransitions transitions = default;
        transitions.Set(StyleProperty.Opacity, new StyleTransition(duration, easing));
        return transitions;
    }

    /// <summary>Gives the selector of a class with the hover state.</summary>
    private static Selector Hover(ClassId id)
    {
        return Selector.For(new CompoundSelector { Class = id, States = PseudoStates.Hover });
    }

    /// <summary>Gives a context with a measurer, a viewport, the root, and the sheet.</summary>
    private static UiContext NewContext(Element root, StyleSheet sheet)
    {
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
        context.AddRoot(root);
        context.AddSheet(sheet);
        return context;
    }
}
