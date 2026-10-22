using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of phase 2 of the transitions: a turn from the visible value, a restyle with the same target, the
/// transition of the new style, a new target or the same target with no transition, and the inline rule.
/// </summary>
/// <remarks>
/// <para>
/// The class table and the type table are global and xUnit runs test classes in parallel. Each test interns its names
/// with the prefix <c>nameof(Test) + "."</c>, and no test asserts a fixed id value.
/// </para>
/// <para>
/// The durations are 0.2 s and the steps are 0.1 s. <c>0.1f + 0.1f</c> equals <c>0.2f</c> exactly, so a tween ends on
/// its second step.
/// </para>
/// </remarks>
public sealed class TransitionRetargetTests
{
    private const float Duration = 0.2f;
    private const float StepTime = 0.1f;

    private static readonly Rgba8 Idle = new(30, 34, 44, 255);
    private static readonly Rgba8 Hovered = new(48, 96, 192, 255);
    private static readonly Rgba8 Pressed = new(200, 40, 40, 255);

    [Fact]
    public void AHoverOutDuringTheFadeTurnsBack()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AHoverOutDuringTheFadeTurnsBack) + ".button");
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
        context.Update(0f);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        string hover = Line("hover", StepTime, button);

        // The hover out turns the tween. A step of 0 adds no time, so the value stays at the visible value.
        button.States = PseudoStates.None;
        context.Update(0f);
        string leaveStart = Line("leave", 0f, button);

        context.Update(StepTime);
        string leaveHalf = Line("leave", StepTime, button);

        context.Update(StepTime);
        string leaveEnd = Line("leave", Duration, button);

        TestContext.Current.TestOutputHelper?.WriteLine(hover);
        TestContext.Current.TestOutputHelper?.WriteLine(leaveStart);
        TestContext.Current.TestOutputHelper?.WriteLine(leaveHalf);
        TestContext.Current.TestOutputHelper?.WriteLine(leaveEnd);

        Assert.Equal("hover 0.1 #274176FF", hover);
        Assert.Equal("leave 0.0 #274176FF", leaveStart);
        Assert.Equal("leave 0.1 #233251FF", leaveHalf);
        Assert.Equal("leave 0.2 #1E222CFF", leaveEnd);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void ARestyleWithTheSameTargetKeepsTheTween()
    {
        ClassId buttonClass = ClassName.Intern(nameof(ARestyleWithTheSameTargetKeepsTheTween) + ".button");
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
        context.Update(0f);
        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal("#274176FF", button.Style.BackgroundColor.ToString());

        // A state change on the root restyles the whole subtree. The button holds its target, so the tween goes on.
        root.States = PseudoStates.Focus;
        context.Update(StepTime);

        TestContext.Current.TestOutputHelper?.WriteLine("after the restyle " + button.Style.BackgroundColor);
        Assert.Equal(Hovered, button.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void ANewTargetTakesTheFullDuration()
    {
        ClassId buttonClass = ClassName.Intern(nameof(ANewTargetTakesTheFullDuration) + ".button");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(buttonClass),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(buttonClass), new StyleProperties { BackgroundColor = Hovered })
            .Add(
                Selector.For(new CompoundSelector { Class = buttonClass, States = PseudoStates.Active }),
                new StyleProperties { BackgroundColor = Pressed })
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal("#274176FF", button.Style.BackgroundColor.ToString());

        // The third color starts from the visible value (39, 65, 118) toward (200, 40, 40).
        button.States = PseudoStates.Hover | PseudoStates.Active;
        context.Update(0f);
        Assert.Equal("#274176FF", button.Style.BackgroundColor.ToString());

        context.Update(StepTime);
        string half = button.Style.BackgroundColor.ToString();
        Assert.Equal(1, context.Transitions.ActiveCount);

        context.Update(StepTime);
        TestContext.Current.TestOutputHelper?.WriteLine("half " + half + ", end " + button.Style.BackgroundColor);

        // (39, 65, 118) to (200, 40, 40) at 0.5 gives (119.5, 52.5, 79), which rounds to (120, 53, 79).
        Assert.Equal(new Rgba8(120, 53, 79, 255).ToString(), half);
        Assert.Equal(Pressed, button.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void TheTransitionOfTheNewStyleApplies()
    {
        ClassId buttonClass = ClassName.Intern(nameof(TheTransitionOfTheNewStyleApplies) + ".button");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(buttonClass),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, 0.4f))
            .Add(
                Hover(buttonClass),
                new StyleProperties { BackgroundColor = Hovered },
                Fade(StyleProperty.BackgroundColor, 0.1f))
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        // The hover rule gives 0.1 s, so the hover in ends after one step.
        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(Hovered, button.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);

        // The hover out uses the base rule with 0.4 s: 0.1 s in, the value is one quarter of the way.
        button.States = PseudoStates.None;
        context.Update(StepTime);
        Assert.NotEqual(Hovered, button.Style.BackgroundColor);
        Assert.NotEqual(Idle, button.Style.BackgroundColor);
        Assert.Equal(1, context.Transitions.ActiveCount);

        context.Update(StepTime);
        context.Update(StepTime);
        Assert.NotEqual(Idle, button.Style.BackgroundColor);
        Assert.Equal(1, context.Transitions.ActiveCount);

        context.Update(StepTime);
        TestContext.Current.TestOutputHelper?.WriteLine("hover out end " + button.Style.BackgroundColor);
        Assert.Equal(Idle, button.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void ANewTargetWithNoTransitionChangesAtOnce()
    {
        ClassId fading = ClassName.Intern(nameof(ANewTargetWithNoTransitionChangesAtOnce) + ".fading");
        ClassId plain = ClassName.Intern(nameof(ANewTargetWithNoTransitionChangesAtOnce) + ".plain");
        Rgba8 plainColor = new(10, 200, 10, 255);
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(fading),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(fading), new StyleProperties { BackgroundColor = Hovered })
            .Add(Selector.ForClass(plain), new StyleProperties { BackgroundColor = plainColor })
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(fading);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        element.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(1, context.Transitions.ActiveCount);

        // The class change removes the only rule with the slot. The new target applies at once.
        element.Classes.Remove(fading);
        element.Classes.Add(plain);
        context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine("after the class change " + element.Style.BackgroundColor);
        Assert.Equal(plainColor, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);

        context.Update(StepTime);
        Assert.Equal(plainColor, element.Style.BackgroundColor);
    }

    [Fact]
    public void ASameTargetWithNoTransitionEndsTheTween()
    {
        ClassId fading = ClassName.Intern(nameof(ASameTargetWithNoTransitionEndsTheTween) + ".fading");
        ClassId plain = ClassName.Intern(nameof(ASameTargetWithNoTransitionEndsTheTween) + ".plain");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(fading),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(fading), new StyleProperties { BackgroundColor = Hovered })
            .Add(Selector.ForClass(plain), new StyleProperties { BackgroundColor = Hovered })
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(fading);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        element.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal("#274176FF", element.Style.BackgroundColor.ToString());
        Assert.Equal(1, context.Transitions.ActiveCount);

        // Probe 14: the new class holds the target of the tween and no transition. The set of the new style decides
        // (Decision 3), so the tween ends and the target applies at once.
        element.Classes.Remove(fading);
        element.Classes.Add(plain);
        context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine("after the class change " + element.Style.BackgroundColor);
        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);

        context.Update(StepTime);
        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void ASameTargetWithAZeroDurationEndsTheTween()
    {
        ClassId fading = ClassName.Intern(nameof(ASameTargetWithAZeroDurationEndsTheTween) + ".fading");
        ClassId instant = ClassName.Intern(nameof(ASameTargetWithAZeroDurationEndsTheTween) + ".instant");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(fading),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(fading), new StyleProperties { BackgroundColor = Hovered })
            .Add(
                Selector.ForClass(instant),
                new StyleProperties { BackgroundColor = Hovered },
                Fade(StyleProperty.BackgroundColor, 0f))
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(fading);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        element.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal("#274176FF", element.Style.BackgroundColor.ToString());
        Assert.Equal(1, context.Transitions.ActiveCount);

        // Decision R3: the new class holds the target of the tween and a transition of 0 s. A duration of 0 acts as no
        // transition, so the tween ends and the target applies at once.
        element.Classes.Remove(fading);
        element.Classes.Add(instant);
        context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine("after the class change " + element.Style.BackgroundColor);
        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);

        context.Update(StepTime);
        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void AnInlineValueChangesAtOnce()
    {
        ClassId x = ClassName.Intern(nameof(AnInlineValueChangesAtOnce) + ".x");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(x),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(x);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        element.Inline.BackgroundColor = Hovered;
        context.Update(StepTime);

        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(0, context.Transitions.Capacity);
    }

    [Fact]
    public void AnInlineValueEndsARunningTween()
    {
        ClassId x = ClassName.Intern(nameof(AnInlineValueEndsARunningTween) + ".x");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(x),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Add(Hover(x), new StyleProperties { BackgroundColor = Hovered })
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(x);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        element.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(1, context.Transitions.ActiveCount);

        element.Inline.BackgroundColor = Pressed;
        context.Update(0f);

        Assert.Equal(Pressed, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);

        context.Update(StepTime);
        Assert.Equal(Pressed, element.Style.BackgroundColor);
    }

    [Fact]
    public void AClearedInlineValueFadesToTheRuleValue()
    {
        ClassId x = ClassName.Intern(nameof(AClearedInlineValueFadesToTheRuleValue) + ".x");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.ForClass(x),
                new StyleProperties { BackgroundColor = Idle },
                Fade(StyleProperty.BackgroundColor, Duration))
            .Build();

        Element root = new();
        Element element = new();
        element.Classes.Add(x);
        root.Children.Add(element);

        UiContext context = NewContext(root, sheet);
        element.Inline.BackgroundColor = Hovered;
        context.Update(0f);
        Assert.Equal(Hovered, element.Style.BackgroundColor);

        // The member leaves the inline set. It fades from the inline value to the rule value.
        element.Inline.Clear(StyleProperty.BackgroundColor);
        context.Update(0f);
        Assert.Equal(Hovered, element.Style.BackgroundColor);
        Assert.Equal(1, context.Transitions.ActiveCount);

        // (48, 96, 192) to (30, 34, 44) at 0.5 gives (39, 65, 118).
        context.Update(StepTime);
        Assert.Equal("#274176FF", element.Style.BackgroundColor.ToString());

        context.Update(StepTime);
        TestContext.Current.TestOutputHelper?.WriteLine("cleared inline end " + element.Style.BackgroundColor);
        Assert.Equal(Idle, element.Style.BackgroundColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    /// <summary>Gives a set with a linear transition of one duration for a member.</summary>
    private static StyleTransitions Fade(StyleProperty property, float duration)
    {
        StyleTransitions transitions = default;
        transitions.Set(property, new StyleTransition(duration, TransitionEasing.Linear));
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

    /// <summary>Gives a line of the demo: the label, the time, and the background color.</summary>
    private static string Line(string label, float time, Element element)
    {
        return label + " " + time.ToString("F1", CultureInfo.InvariantCulture) + " " + element.Style.BackgroundColor;
    }
}
