using System;
using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of phase 1 of the transitions: the hover demo, the linear step of each member, the first-style rule, the
/// paint mark of the transition pass, a member with no transition, the merge of the matched sets, the premultiplied
/// color step, and a sheet with no transition.
/// </summary>
/// <remarks>
/// <para>
/// The class table and the type table are global and xUnit runs test classes in parallel. Each test interns its names
/// with the prefix <c>nameof(Test) + "."</c>, and no test asserts a fixed id value.
/// </para>
/// <para>
/// The durations are 0.2 s and the steps are 0.1 s. <c>0.1f + 0.1f</c> equals <c>0.2f</c> exactly, so a tween ends on
/// its second step (Decision 6).
/// </para>
/// </remarks>
public sealed class TransitionPassTests
{
    private const float Duration = 0.2f;
    private const float StepTime = 0.1f;

    private static readonly Rgba8 Idle = new(30, 34, 44, 255);
    private static readonly Rgba8 Hovered = new(48, 96, 192, 255);
    private static readonly Rgba8 Midpoint = new(39, 65, 118, 255);

    [Fact]
    public void AHoverFadesTheFillOfAButton()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AHoverFadesTheFillOfAButton) + ".button");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(buttonClass),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(buttonClass), new StyleProperties { BackgroundColor = Hovered })
            .Build();

        Element root = new() { Name = "root" };
        Element button = new() { Name = "button" };
        button.Classes.Add(buttonClass);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        float time = 0f;
        context.Update(0f);
        string first = Line(time, button);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        time += StepTime;
        string second = Line(time, button);

        context.Update(StepTime);
        time += StepTime;
        string third = Line(time, button);

        TestContext.Current.TestOutputHelper?.WriteLine(first);
        TestContext.Current.TestOutputHelper?.WriteLine(second);
        TestContext.Current.TestOutputHelper?.WriteLine(third);

        Assert.Equal("t 0.0 #1E222CFF", first);
        Assert.Equal("t 0.1 #274176FF", second);
        Assert.Equal("t 0.2 #3060C0FF", third);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Theory]
    [InlineData(StyleProperty.BackgroundColor, "#643219FF", "#C86432FF")]
    [InlineData(StyleProperty.BorderColor, "#14283CFF", "#1E3C5AFF")]
    [InlineData(StyleProperty.CornerRadius, "4 2 1 3", "8 4 2 6")]
    [InlineData(StyleProperty.Opacity, "0.750", "0.500")]
    [InlineData(StyleProperty.Color, "#9BCDFFFF", "#379BFFFF")]
    public void EachMemberFadesToItsTarget(StyleProperty property, string half, string end)
    {
        ClassId x = ClassName.Intern(nameof(EachMemberFadesToItsTarget) + "." + property + ".x");
        StyleProperties start = default;
        StyleProperties target = default;
        switch (property)
        {
            case StyleProperty.BackgroundColor:
                start.BackgroundColor = new Rgba8(0, 0, 0, 255);
                target.BackgroundColor = new Rgba8(200, 100, 50, 255);
                break;
            case StyleProperty.BorderColor:
                start.BorderColor = new Rgba8(10, 20, 30, 255);
                target.BorderColor = new Rgba8(30, 60, 90, 255);
                break;
            case StyleProperty.CornerRadius:
                start.CornerRadius = new Corners(0f, 0f, 0f, 0f);
                target.CornerRadius = new Corners(8f, 4f, 2f, 6f);
                break;
            case StyleProperty.Opacity:
                start.Opacity = 1f;
                target.Opacity = 0.5f;
                break;
            case StyleProperty.Color:
                start.Color = new Rgba8(255, 255, 255, 255);
                target.Color = new Rgba8(55, 155, 255, 255);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, "The member cannot transition.");
        }

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(x), start, Fade(property, Duration))
            .Add(Hover(x), target)
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(x);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        element.States = PseudoStates.Hover;
        context.Update(StepTime);
        string halfValue = Describe(property, element.Style);
        int halfCount = context.Transitions.ActiveCount;

        context.Update(StepTime);
        string endValue = Describe(property, element.Style);

        TestContext.Current.TestOutputHelper?.WriteLine(property + " " + halfValue + " " + endValue);
        Assert.Equal(half, halfValue);
        Assert.Equal(1, halfCount);
        Assert.Equal(end, endValue);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void TheFirstStyleStartsNoTween()
    {
        ClassId x = ClassName.Intern(nameof(TheFirstStyleStartsNoTween) + ".x");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(x),
                new StyleProperties { BackgroundColor = Hovered, Opacity = 0.5f },
                Fade(StyleProperty.BackgroundColor, Duration, StyleProperty.Opacity))
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(x);
        root.Children.Add(element);
        Assert.False(element.IsStyled);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        Assert.True(element.IsStyled);
        Assert.True(root.IsStyled);
        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(0.5f, element.Style.Opacity);
        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(0, context.Transitions.Capacity);
    }

    [Fact]
    public void ATweenMarksPaintOnly()
    {
        ClassId buttonClass = ClassName.Intern(nameof(ATweenMarksPaintOnly) + ".button");
        ClassId chipClass = ClassName.Intern(nameof(ATweenMarksPaintOnly) + ".chip");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(buttonClass),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(buttonClass), new StyleProperties { BackgroundColor = Hovered })
            .Add(
                Selector.ForClass(chipClass),
                new StyleProperties { CornerRadius = new Corners(0f, 0f, 0f, 0f) },
                Fade(StyleProperty.CornerRadius, Duration))
            .Add(Hover(chipClass), new StyleProperties { CornerRadius = new Corners(8f, 8f, 8f, 8f) })
            .Build();

        Element root = new() { Name = "root" };
        Element button = new() { Name = "button" };
        button.Classes.Add(buttonClass);
        Label chip = new(new Label.CreateInfo { Text = "chip" }) { Name = "chip" };
        chip.Classes.Add(chipClass);
        root.Children.Add(button);
        root.Children.Add(chip);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        button.States = PseudoStates.Hover;
        chip.States = PseudoStates.Hover;
        context.Update(StepTime);

        // The full update cleared each bit, and both tweens run.
        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, button.Dirty);
        Assert.Equal(DirtyFlags.None, chip.Dirty);
        Assert.Equal(2, context.Transitions.ActiveCount);

        context.UpdateTransitions(0.05f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "root " + root.Dirty + ", button " + button.Dirty + ", chip " + chip.Dirty);
        Assert.Equal(DirtyFlags.Paint, button.Dirty);
        Assert.Equal(DirtyFlags.Paint, chip.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, root.Dirty);
        Assert.NotEqual(Midpoint, button.Style.BackgroundColor);
        Assert.Equal(6f, chip.Style.CornerRadius.TopLeft, 0.001f);
    }

    [Fact]
    public void AMemberWithNoTransitionChangesAtOnce()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AMemberWithNoTransitionChangesAtOnce) + ".button");
        Rgba8 idleBorder = new(10, 10, 10, 255);
        Rgba8 hoverBorder = new(250, 200, 0, 255);
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(buttonClass),
                new StyleProperties { BackgroundColor = Idle, BorderColor = idleBorder },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(buttonClass), new StyleProperties { BackgroundColor = Hovered, BorderColor = hoverBorder })
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        button.States = PseudoStates.Hover;
        context.Update(StepTime);

        Assert.Equal(hoverBorder, button.Style.BorderColor);
        Assert.Equal(Midpoint, button.Style.BackgroundColor);
        Assert.Equal(1, context.Transitions.ActiveCount);
    }

    [Fact]
    public void ALaterMatchedRuleWinsForEachSlot()
    {
        ClassId a = ClassName.Intern(nameof(ALaterMatchedRuleWinsForEachSlot) + ".a");
        ClassId b = ClassName.Intern(nameof(ALaterMatchedRuleWinsForEachSlot) + ".b");

        // The rule .a declares two slots. The later rule .b (same specificity, later in the sheet) declares the opacity
        // slot again with twice the duration. The hover rule declares no slot.
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(a),
                new StyleProperties { BackgroundColor = Idle, Opacity = 1f },
                Fade(StyleProperty.BackgroundColor, Duration, StyleProperty.Opacity))
            .Add(Selector.ForClass(b), default(StyleProperties), Fade(StyleProperty.Opacity, 2f * Duration))
            .Add(Hover(a), new StyleProperties { BackgroundColor = Hovered, Opacity = 0f })
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(a);
        element.Classes.Add(b);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        element.States = PseudoStates.Hover;

        context.Update(StepTime);
        Assert.Equal(Midpoint, element.Style.BackgroundColor);
        Assert.Equal(0.75f, element.Style.Opacity, 0.0001f);

        context.Update(StepTime);
        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(0.5f, element.Style.Opacity, 0.0001f);
        Assert.Equal(1, context.Transitions.ActiveCount);

        context.Update(2f * StepTime);
        Assert.Equal(0f, element.Style.Opacity);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void AColorFadesInPremultipliedAlpha()
    {
        ClassId x = ClassName.Intern(nameof(AColorFadesInPremultipliedAlpha) + ".x");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(x),
                new StyleProperties { BackgroundColor = Rgba8.Transparent },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(x), new StyleProperties { BackgroundColor = new Rgba8(255, 0, 0, 255) })
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(x);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        element.States = PseudoStates.Hover;
        context.Update(StepTime);

        // A straight step would give #80000080, a dark red. The premultiplied step keeps the red of the target.
        Assert.Equal("#FF000080", element.Style.BackgroundColor.ToString());
    }

    [Fact]
    public void ASheetWithNoTransitionRentsNoRecord()
    {
        ClassId buttonClass = ClassName.Intern(nameof(ASheetWithNoTransitionRentsNoRecord) + ".button");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { BackgroundColor = Idle })
            .Add(Hover(buttonClass), new StyleProperties { BackgroundColor = Hovered })
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        button.States = PseudoStates.Hover;
        context.Update(StepTime);

        Assert.Equal(Hovered, button.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(0, context.Transitions.Capacity);
    }

    /// <summary>Gives a set with a linear transition of one duration for each member.</summary>
    private static StyleTransitions Fade(StyleProperty property, float duration, StyleProperty? other = null)
    {
        StyleTransitions transitions = default;
        transitions.Set(property, new StyleTransition(duration, TransitionEasing.Linear));
        if (other is StyleProperty second)
        {
            transitions.Set(second, new StyleTransition(duration, TransitionEasing.Linear));
        }

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

    /// <summary>Gives the line of the demo: the time and the background color.</summary>
    private static string Line(float time, Element element)
    {
        return "t " + time.ToString("F1", CultureInfo.InvariantCulture) + " " + element.Style.BackgroundColor;
    }

    /// <summary>Gives the value of one member as text: a color as #RRGGBBAA, the corners, or the opacity.</summary>
    private static string Describe(StyleProperty property, in ComputedStyle style)
    {
        switch (property)
        {
            case StyleProperty.BackgroundColor: return style.BackgroundColor.ToString();
            case StyleProperty.BorderColor: return style.BorderColor.ToString();
            case StyleProperty.Color: return style.Color.ToString();
            case StyleProperty.CornerRadius:
                Corners corners = style.CornerRadius;
                return string.Join(
                    ' ',
                    corners.TopLeft.ToString(CultureInfo.InvariantCulture),
                    corners.TopRight.ToString(CultureInfo.InvariantCulture),
                    corners.BottomRight.ToString(CultureInfo.InvariantCulture),
                    corners.BottomLeft.ToString(CultureInfo.InvariantCulture));
            case StyleProperty.Opacity: return style.Opacity.ToString("F3", CultureInfo.InvariantCulture);
            default: throw new ArgumentOutOfRangeException(nameof(property), property, "The member cannot transition.");
        }
    }
}
