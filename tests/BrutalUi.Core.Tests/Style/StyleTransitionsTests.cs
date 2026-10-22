using System;
using System.Runtime.CompilerServices;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the transition model: <see cref="TransitionEasing"/>, <see cref="StyleTransition"/>,
/// <see cref="StyleTransitions"/>, the transitions of <see cref="StyleRule"/>, and the <c>Add</c> overload of
/// <see cref="StyleSheet.Builder"/>.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel.
/// Each test uses names with a unique prefix, and no test asserts a fixed id value.
/// </remarks>
public sealed class StyleTransitionsTests
{
    private static readonly Rgba8 Red = new(255, 0, 0, 255);
    private static readonly Rgba8 Blue = new(0, 0, 255, 255);

    /// <summary>The five members that can transition (Q3).</summary>
    public static TheoryData<StyleProperty> TransitionMembers =>
    [
        StyleProperty.BackgroundColor,
        StyleProperty.BorderColor,
        StyleProperty.CornerRadius,
        StyleProperty.Opacity,
        StyleProperty.Color,
    ];

    /// <summary>Each member that cannot transition, and two values that are not members of the enum.</summary>
    public static TheoryData<StyleProperty> OtherMembers =>
    [
        StyleProperty.FlexDirection,
        StyleProperty.Width,
        StyleProperty.Height,
        StyleProperty.Margin,
        StyleProperty.Padding,
        StyleProperty.BorderWidth,
        StyleProperty.FlexGrow,
        StyleProperty.FlexShrink,
        StyleProperty.FlexBasis,
        StyleProperty.MinWidth,
        StyleProperty.MinHeight,
        StyleProperty.MaxWidth,
        StyleProperty.MaxHeight,
        StyleProperty.JustifyContent,
        StyleProperty.AlignItems,
        StyleProperty.AlignSelf,
        StyleProperty.AlignContent,
        StyleProperty.FlexWrap,
        StyleProperty.Display,
        StyleProperty.Position,
        StyleProperty.Left,
        StyleProperty.Top,
        StyleProperty.Right,
        StyleProperty.Bottom,
        StyleProperty.Overflow,
        StyleProperty.Font,
        StyleProperty.FontSize,
        (StyleProperty)32,
        (StyleProperty)255,
    ];

    [Theory]
    [InlineData(StyleProperty.FlexDirection, false)]
    [InlineData(StyleProperty.Width, false)]
    [InlineData(StyleProperty.Height, false)]
    [InlineData(StyleProperty.Margin, false)]
    [InlineData(StyleProperty.Padding, false)]
    [InlineData(StyleProperty.BorderWidth, false)]
    [InlineData(StyleProperty.FlexGrow, false)]
    [InlineData(StyleProperty.FlexShrink, false)]
    [InlineData(StyleProperty.FlexBasis, false)]
    [InlineData(StyleProperty.MinWidth, false)]
    [InlineData(StyleProperty.MinHeight, false)]
    [InlineData(StyleProperty.MaxWidth, false)]
    [InlineData(StyleProperty.MaxHeight, false)]
    [InlineData(StyleProperty.JustifyContent, false)]
    [InlineData(StyleProperty.AlignItems, false)]
    [InlineData(StyleProperty.AlignSelf, false)]
    [InlineData(StyleProperty.AlignContent, false)]
    [InlineData(StyleProperty.FlexWrap, false)]
    [InlineData(StyleProperty.Display, false)]
    [InlineData(StyleProperty.Position, false)]
    [InlineData(StyleProperty.Left, false)]
    [InlineData(StyleProperty.Top, false)]
    [InlineData(StyleProperty.Right, false)]
    [InlineData(StyleProperty.Bottom, false)]
    [InlineData(StyleProperty.Overflow, false)]
    [InlineData(StyleProperty.BackgroundColor, true)]
    [InlineData(StyleProperty.BorderColor, true)]
    [InlineData(StyleProperty.CornerRadius, true)]
    [InlineData(StyleProperty.Opacity, true)]
    [InlineData(StyleProperty.Color, true)]
    [InlineData(StyleProperty.Font, false)]
    [InlineData(StyleProperty.FontSize, false)]
    public void CanTransitionGivesTheFiveMembersOfQ3(StyleProperty property, bool expected)
    {
        Assert.Equal(expected, StyleTransitions.CanTransition(property));
    }

    [Fact]
    public void CanTransitionGivesFalseForAValueThatIsNotAMember()
    {
        Assert.False(StyleTransitions.CanTransition((StyleProperty)32));
        Assert.False(StyleTransitions.CanTransition((StyleProperty)255));
    }

    [Fact]
    public void TheTheoryCoversEachMemberOfTheEnum()
    {
        // The theory above has one row for each member from FlexDirection to FontSize. A new member of the enum
        // changes the last value, and this test then fails until the theory gets a row for it.
        Assert.Equal(31, (int)StyleProperty.FontSize);
    }

    [Fact]
    public void ADefaultSetIsEmpty()
    {
        StyleTransitions transitions = default;

        Assert.True(transitions.IsEmpty);
        Assert.False(transitions.IsSet(StyleProperty.BackgroundColor));
        Assert.False(transitions.IsSet(StyleProperty.Opacity));
    }

    [Theory]
    [MemberData(nameof(TransitionMembers))]
    public void SetWritesTheMemberOnly(StyleProperty property)
    {
        StyleTransitions transitions = default;

        transitions.Set(property, new StyleTransition(0.25f, TransitionEasing.EaseOut));

        Assert.False(transitions.IsEmpty);
        Assert.True(transitions.IsSet(property));
        StyleTransition got = transitions.Get(property);
        Assert.Equal(0.25f, got.Duration);
        Assert.Equal(TransitionEasing.EaseOut, got.Easing);
        foreach (StyleProperty other in AllTransitionMembers())
        {
            if (other != property)
            {
                Assert.False(transitions.IsSet(other));
                Assert.Equal(0f, transitions.Get(other).Duration);
                Assert.Equal(TransitionEasing.Linear, transitions.Get(other).Easing);
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransitionMembers))]
    public void ALaterSetReplacesTheTransition(StyleProperty property)
    {
        StyleTransitions transitions = default;

        transitions.Set(property, new StyleTransition(1f, TransitionEasing.EaseIn));
        transitions.Set(property, new StyleTransition(2f, TransitionEasing.EaseInOut));

        Assert.Equal(2f, transitions.Get(property).Duration);
        Assert.Equal(TransitionEasing.EaseInOut, transitions.Get(property).Easing);
    }

    [Theory]
    [MemberData(nameof(TransitionMembers))]
    public void GetGivesTheDefaultForAnUnsetMember(StyleProperty property)
    {
        StyleTransitions transitions = default;

        StyleTransition got = transitions.Get(property);

        Assert.Equal(0f, got.Duration);
        Assert.Equal(TransitionEasing.Linear, got.Easing);
    }

    [Theory]
    [MemberData(nameof(TransitionMembers))]
    public void ClearRemovesTheMemberOnly(StyleProperty property)
    {
        StyleTransitions transitions = Full();

        transitions.Clear(property);

        Assert.False(transitions.IsSet(property));
        Assert.Equal(0f, transitions.Get(property).Duration);
        Assert.Equal(TransitionEasing.Linear, transitions.Get(property).Easing);
        Assert.False(transitions.IsEmpty);
        foreach (StyleProperty other in AllTransitionMembers())
        {
            if (other != property)
            {
                Assert.True(transitions.IsSet(other));
                Assert.Equal(DurationOf(other), transitions.Get(other).Duration);
            }
        }
    }

    [Fact]
    public void ClearOfEachMemberGivesAnEmptySet()
    {
        StyleTransitions transitions = Full();

        foreach (StyleProperty property in AllTransitionMembers())
        {
            transitions.Clear(property);
        }

        Assert.True(transitions.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(TransitionMembers))]
    public void ClearOfAnUnsetMemberChangesNothing(StyleProperty property)
    {
        StyleTransitions transitions = default;

        transitions.Clear(property);

        Assert.True(transitions.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(OtherMembers))]
    public void SetThrowsForEachOtherMember(StyleProperty property)
    {
        StyleTransitions transitions = default;

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            "property",
            () => transitions.Set(property, new StyleTransition(1f, TransitionEasing.Linear)));

        AssertNamesTheFiveMembers(error.Message);
        Assert.True(transitions.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(OtherMembers))]
    public void GetThrowsForEachOtherMember(StyleProperty property)
    {
        StyleTransitions transitions = Full();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            "property",
            () => transitions.Get(property));

        AssertNamesTheFiveMembers(error.Message);
    }

    [Theory]
    [MemberData(nameof(OtherMembers))]
    public void IsSetGivesFalseForEachOtherMember(StyleProperty property)
    {
        StyleTransitions transitions = Full();

        Assert.False(transitions.IsSet(property));
    }

    [Theory]
    [MemberData(nameof(OtherMembers))]
    public void ClearOfEachOtherMemberChangesNothing(StyleProperty property)
    {
        StyleTransitions transitions = Full();

        transitions.Clear(property);

        foreach (StyleProperty member in AllTransitionMembers())
        {
            Assert.True(transitions.IsSet(member));
            Assert.Equal(DurationOf(member), transitions.Get(member).Duration);
        }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void TheModelKeepsEachDuration(float duration)
    {
        StyleTransitions transitions = default;

        transitions.Set(StyleProperty.Opacity, new StyleTransition(duration, TransitionEasing.Linear));

        Assert.True(transitions.IsSet(StyleProperty.Opacity));
        Assert.Equal(duration, transitions.Get(StyleProperty.Opacity).Duration);
    }

    [Theory]
    [InlineData(TransitionEasing.Linear)]
    [InlineData(TransitionEasing.EaseIn)]
    [InlineData(TransitionEasing.EaseOut)]
    [InlineData(TransitionEasing.EaseInOut)]
    public void ATransitionKeepsItsEasing(TransitionEasing easing)
    {
        StyleTransition transition = new(0.5f, easing);

        Assert.Equal(0.5f, transition.Duration);
        Assert.Equal(easing, transition.Easing);
    }

    [Fact]
    public void ATransitionThrowsForAnEasingThatIsNotAMember()
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            "easing",
            () => new StyleTransition(1f, (TransitionEasing)4));

        Assert.Contains("EaseInOut", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSetHoldsNoHeapObject()
    {
        // Five slots of a float and an easing (8 bytes each with the padding), one mask byte, and the padding.
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<StyleTransitions>());
        Assert.Equal(44, Unsafe.SizeOf<StyleTransitions>());
    }

    [Fact]
    public void TheSetCallsAllocateNothing()
    {
        StyleTransitions transitions = default;
        RunSetCalls(ref transitions);

        long before = GC.GetAllocatedBytesForCurrentThread();
        float sum = 0f;
        for (int i = 0; i < 1000; i++)
        {
            sum += RunSetCalls(ref transitions);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, after - before);
        Assert.True(sum > 0f);
    }

    [Fact]
    public void TheNewConstructorCopiesTheSetIntoTheRule()
    {
        ClassId a = ClassName.Intern(nameof(TheNewConstructorCopiesTheSetIntoTheRule) + "-a");
        Selector selector = Selector.ForClass(a);
        StyleProperties properties = default;
        properties.BackgroundColor = Red;
        StyleTransitions transitions = default;
        transitions.Set(StyleProperty.BackgroundColor, new StyleTransition(0.2f, TransitionEasing.EaseOut));

        StyleRule rule = new(in selector, in properties, in transitions);
        transitions.Set(StyleProperty.BackgroundColor, new StyleTransition(9f, TransitionEasing.EaseIn));
        transitions.Set(StyleProperty.Opacity, new StyleTransition(1f, TransitionEasing.Linear));

        Assert.Equal(selector, rule.Selector);
        Assert.Equal(256, rule.Specificity);
        Assert.Equal(Red, rule.Properties.BackgroundColor);
        Assert.True(rule.Transitions.IsSet(StyleProperty.BackgroundColor));
        Assert.Equal(0.2f, rule.Transitions.Get(StyleProperty.BackgroundColor).Duration);
        Assert.Equal(TransitionEasing.EaseOut, rule.Transitions.Get(StyleProperty.BackgroundColor).Easing);
        Assert.False(rule.Transitions.IsSet(StyleProperty.Opacity));
    }

    [Fact]
    public void TheOldConstructorGivesAnEmptySet()
    {
        ClassId a = ClassName.Intern(nameof(TheOldConstructorGivesAnEmptySet) + "-a");
        Selector selector = Selector.ForClass(a);
        StyleProperties properties = default;
        properties.Opacity = 0.5f;

        StyleRule rule = new(in selector, in properties);

        Assert.True(rule.Transitions.IsEmpty);
        Assert.Equal(256, rule.Specificity);
        Assert.Equal(0.5f, rule.Properties.Opacity);
    }

    [Fact]
    public void TheOldAddGivesAnEmptySet()
    {
        ClassId a = ClassName.Intern(nameof(TheOldAddGivesAnEmptySet) + "-a");

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(a), new StyleProperties { BackgroundColor = Red })
            .Build();

        Assert.True(Assert.Single(sheet.Rules.ToArray()).Transitions.IsEmpty);
    }

    [Fact]
    public void TheNewAddKeepsACopyOfTheSet()
    {
        ClassId a = ClassName.Intern(nameof(TheNewAddKeepsACopyOfTheSet) + "-a");
        StyleTransitions transitions = default;
        transitions.Set(StyleProperty.Color, new StyleTransition(0.3f, TransitionEasing.EaseIn));
        StyleSheet.Builder builder = new();

        builder.Add(Selector.ForClass(a), new StyleProperties { Color = Blue }, transitions);
        transitions.Clear(StyleProperty.Color);
        transitions.Set(StyleProperty.Opacity, new StyleTransition(5f, TransitionEasing.Linear));
        StyleSheet sheet = builder.Build();

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.True(rule.Transitions.IsSet(StyleProperty.Color));
        Assert.Equal(0.3f, rule.Transitions.Get(StyleProperty.Color).Duration);
        Assert.Equal(TransitionEasing.EaseIn, rule.Transitions.Get(StyleProperty.Color).Easing);
        Assert.False(rule.Transitions.IsSet(StyleProperty.Opacity));
    }

    [Fact]
    public void TheNewAddThrowsForADefaultSelector()
    {
        StyleTransitions transitions = default;
        transitions.Set(StyleProperty.Opacity, new StyleTransition(1f, TransitionEasing.Linear));
        StyleSheet.Builder builder = new();

        Assert.Throws<ArgumentException>(
            "selector",
            () => builder.Add(default(Selector), default(StyleProperties), transitions));
        Assert.Equal(0, builder.Build().Rules.Length);
    }

    [Fact]
    public void TheSortKeepsTheSetWithItsRule()
    {
        string prefix = nameof(TheSortKeepsTheSetWithItsRule) + "-";
        ClassId a = ClassName.Intern(prefix + "a");
        ClassId b = ClassName.Intern(prefix + "b");
        CompoundSelector classAndHover = new() { Class = a, States = PseudoStates.Hover };

        // The rules come in an order that the sort changes: the name rule first, the class rules last.
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForName(prefix + "panel"), WidthOf(1), OpacityIn(1f))
            .Add(Selector.For(in classAndHover), WidthOf(2), OpacityIn(2f))
            .Add(Selector.ForClass(a), WidthOf(3), OpacityIn(3f))
            .Add(Selector.ForClass(b), WidthOf(4))
            .Add(Selector.ForClass(a), WidthOf(5), OpacityIn(5f))
            .Build();

        ReadOnlySpan<StyleRule> rules = sheet.Rules;
        Assert.Equal(5, rules.Length);
        int[] expectedOrder = [3, 4, 5, 2, 1];
        for (int i = 0; i < rules.Length; i++)
        {
            int id = expectedOrder[i];
            Assert.Equal(Length.Px(id), rules[i].Properties.Width);
            if (id == 4)
            {
                Assert.True(rules[i].Transitions.IsEmpty);
            }
            else
            {
                Assert.True(rules[i].Transitions.IsSet(StyleProperty.Opacity));
                Assert.Equal(id, rules[i].Transitions.Get(StyleProperty.Opacity).Duration);
            }
        }
    }

    [Fact]
    public void ARuleWithATransitionStylesTheSame()
    {
        string prefix = nameof(ARuleWithATransitionStylesTheSame) + "-";
        ClassId panelClass = ClassName.Intern(prefix + "panel");
        ClassId labelClass = ClassName.Intern(prefix + "label");
        StyleProperties panel = new()
        {
            BackgroundColor = Red,
            BorderColor = Blue,
            CornerRadius = Corners.All(6f),
            Opacity = 0.5f,
            Color = Blue,
            Width = 200f,
            Padding = LengthEdges.All(8f),
        };
        StyleProperties label = new() { Color = Red, Height = 20f };
        StyleTransitions panelTransitions = default;
        panelTransitions.Set(StyleProperty.BackgroundColor, new StyleTransition(0.2f, TransitionEasing.EaseOut));
        panelTransitions.Set(StyleProperty.BorderColor, new StyleTransition(0.3f, TransitionEasing.EaseIn));
        panelTransitions.Set(StyleProperty.CornerRadius, new StyleTransition(0.4f, TransitionEasing.EaseInOut));
        panelTransitions.Set(StyleProperty.Opacity, new StyleTransition(0.5f, TransitionEasing.Linear));
        panelTransitions.Set(StyleProperty.Color, new StyleTransition(0.6f, TransitionEasing.EaseOut));
        StyleTransitions labelTransitions = default;
        labelTransitions.Set(StyleProperty.Color, new StyleTransition(1f, TransitionEasing.Linear));

        StyleSheet withTransitions = new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), panel, panelTransitions)
            .Add(Selector.ForClass(labelClass), label, labelTransitions)
            .Build();
        StyleSheet withNone = new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), panel)
            .Add(Selector.ForClass(labelClass), label)
            .Build();

        (Element rootA, Element panelA, Element labelA, Element plainA) = StyleTree(withTransitions, panelClass, labelClass);
        (Element rootB, Element panelB, Element labelB, Element plainB) = StyleTree(withNone, panelClass, labelClass);

        AssertSameStyle(rootB.Style, rootA.Style, "root");
        AssertSameStyle(panelB.Style, panelA.Style, "panel");
        AssertSameStyle(labelB.Style, labelA.Style, "label");
        AssertSameStyle(plainB.Style, plainA.Style, "plain");
        Assert.Equal(Red, panelA.Style.BackgroundColor);
        Assert.Equal(0.5f, panelA.Style.Opacity);
        Assert.Equal(Red, labelA.Style.Color);
        Assert.Equal(Blue, plainA.Style.Color);
    }

    private static (Element Root, Element Panel, Element Label, Element Plain) StyleTree(
        StyleSheet sheet,
        ClassId panelClass,
        ClassId labelClass)
    {
        Element root = new();
        Element panel = new();
        Element label = new();
        Element plain = new();
        panel.Classes.Add(panelClass);
        label.Classes.Add(labelClass);
        root.Children.Add(panel);
        panel.Children.Add(label);
        panel.Children.Add(plain);

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);
        return (root, panel, label, plain);
    }

    private static void AssertSameStyle(in ComputedStyle expected, in ComputedStyle actual, string element)
    {
        AssertMember(expected.FlexDirection, actual.FlexDirection, element, nameof(ComputedStyle.FlexDirection));
        AssertMember(expected.Width, actual.Width, element, nameof(ComputedStyle.Width));
        AssertMember(expected.Height, actual.Height, element, nameof(ComputedStyle.Height));
        AssertMember(expected.Margin, actual.Margin, element, nameof(ComputedStyle.Margin));
        AssertMember(expected.Padding, actual.Padding, element, nameof(ComputedStyle.Padding));
        AssertMember(expected.BorderWidth, actual.BorderWidth, element, nameof(ComputedStyle.BorderWidth));
        AssertMember(expected.FlexGrow, actual.FlexGrow, element, nameof(ComputedStyle.FlexGrow));
        AssertMember(expected.FlexShrink, actual.FlexShrink, element, nameof(ComputedStyle.FlexShrink));
        AssertMember(expected.FlexBasis, actual.FlexBasis, element, nameof(ComputedStyle.FlexBasis));
        AssertMember(expected.MinWidth, actual.MinWidth, element, nameof(ComputedStyle.MinWidth));
        AssertMember(expected.MinHeight, actual.MinHeight, element, nameof(ComputedStyle.MinHeight));
        AssertMember(expected.MaxWidth, actual.MaxWidth, element, nameof(ComputedStyle.MaxWidth));
        AssertMember(expected.MaxHeight, actual.MaxHeight, element, nameof(ComputedStyle.MaxHeight));
        AssertMember(expected.JustifyContent, actual.JustifyContent, element, nameof(ComputedStyle.JustifyContent));
        AssertMember(expected.AlignItems, actual.AlignItems, element, nameof(ComputedStyle.AlignItems));
        AssertMember(expected.AlignSelf, actual.AlignSelf, element, nameof(ComputedStyle.AlignSelf));
        AssertMember(expected.FlexWrap, actual.FlexWrap, element, nameof(ComputedStyle.FlexWrap));
        AssertMember(expected.AlignContent, actual.AlignContent, element, nameof(ComputedStyle.AlignContent));
        AssertMember(expected.Display, actual.Display, element, nameof(ComputedStyle.Display));
        AssertMember(expected.Position, actual.Position, element, nameof(ComputedStyle.Position));
        AssertMember(expected.Left, actual.Left, element, nameof(ComputedStyle.Left));
        AssertMember(expected.Top, actual.Top, element, nameof(ComputedStyle.Top));
        AssertMember(expected.Right, actual.Right, element, nameof(ComputedStyle.Right));
        AssertMember(expected.Bottom, actual.Bottom, element, nameof(ComputedStyle.Bottom));
        AssertMember(expected.Overflow, actual.Overflow, element, nameof(ComputedStyle.Overflow));
        AssertMember(expected.BackgroundColor, actual.BackgroundColor, element, nameof(ComputedStyle.BackgroundColor));
        AssertMember(expected.BorderColor, actual.BorderColor, element, nameof(ComputedStyle.BorderColor));
        AssertMember(expected.CornerRadius, actual.CornerRadius, element, nameof(ComputedStyle.CornerRadius));
        AssertMember(expected.Color, actual.Color, element, nameof(ComputedStyle.Color));
        AssertMember(expected.Font, actual.Font, element, nameof(ComputedStyle.Font));
        AssertMember(expected.FontSize, actual.FontSize, element, nameof(ComputedStyle.FontSize));
        AssertMember(expected.Opacity, actual.Opacity, element, nameof(ComputedStyle.Opacity));
    }

    private static void AssertMember<T>(T expected, T actual, string element, string member)
    {
        Assert.True(Equals(expected, actual), element + " " + member + ": expected " + expected + ", got " + actual);
    }

    private static void AssertNamesTheFiveMembers(string message)
    {
        Assert.Contains("BackgroundColor", message, StringComparison.Ordinal);
        Assert.Contains("BorderColor", message, StringComparison.Ordinal);
        Assert.Contains("CornerRadius", message, StringComparison.Ordinal);
        Assert.Contains("Opacity", message, StringComparison.Ordinal);
        Assert.Contains("Color", message, StringComparison.Ordinal);
    }

    private static StyleProperty[] AllTransitionMembers()
    {
        return
        [
            StyleProperty.BackgroundColor,
            StyleProperty.BorderColor,
            StyleProperty.CornerRadius,
            StyleProperty.Opacity,
            StyleProperty.Color,
        ];
    }

    /// <summary>Gives a different duration for each of the five members.</summary>
    private static float DurationOf(StyleProperty property)
    {
        return 1f + (int)property;
    }

    /// <summary>Gives a set that holds each of the five members, each with the duration of <see cref="DurationOf"/>.</summary>
    private static StyleTransitions Full()
    {
        StyleTransitions transitions = default;
        foreach (StyleProperty property in AllTransitionMembers())
        {
            transitions.Set(property, new StyleTransition(DurationOf(property), TransitionEasing.EaseInOut));
        }

        return transitions;
    }

    private static float RunSetCalls(ref StyleTransitions transitions)
    {
        transitions.Set(StyleProperty.BackgroundColor, new StyleTransition(0.2f, TransitionEasing.EaseOut));
        transitions.Set(StyleProperty.Opacity, new StyleTransition(0.4f, TransitionEasing.EaseIn));
        float sum = transitions.Get(StyleProperty.BackgroundColor).Duration + transitions.Get(StyleProperty.Opacity).Duration;
        sum += transitions.IsSet(StyleProperty.Width) ? 1f : 0f;
        transitions.Clear(StyleProperty.BackgroundColor);
        transitions.Clear(StyleProperty.Width);
        sum += StyleTransitions.CanTransition(StyleProperty.Color) ? 1f : 0f;
        return sum;
    }

    private static StyleProperties WidthOf(float width)
    {
        StyleProperties properties = default;
        properties.Width = width;
        return properties;
    }

    private static StyleTransitions OpacityIn(float duration)
    {
        StyleTransitions transitions = default;
        transitions.Set(StyleProperty.Opacity, new StyleTransition(duration, TransitionEasing.Linear));
        return transitions;
    }
}
