using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the selector, the rule, the sheet, and the builder: the specificity, the match, the order of the
/// rules, and the copy at the build.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel.
/// Each test uses names with a unique prefix, and no test asserts a fixed id value.
/// </remarks>
public sealed class StyleSheetTests
{
    [Fact]
    public void ForClassGivesTheClassAndTheSpecificityOfOneClass()
    {
        ClassId id = ClassName.Intern(nameof(ForClassGivesTheClassAndTheSpecificityOfOneClass) + ".a");

        Selector selector = Selector.ForClass(id);

        Assert.Equal(id, selector.Class);
        Assert.Equal(256, selector.Specificity);
        Assert.False(selector.IsEmpty);
    }

    [Fact]
    public void ForClassThrowsForNone()
    {
        Assert.Throws<ArgumentException>("id", () => Selector.ForClass(ClassId.None));
    }

    [Fact]
    public void ADefaultSelectorIsEmptyAndHasNoSpecificity()
    {
        Selector selector = default;

        Assert.True(selector.IsEmpty);
        Assert.Equal(ClassId.None, selector.Class);
        Assert.Equal(0, selector.Specificity);
    }

    [Fact]
    public void ASelectorMatchesAnElementWithTheClass()
    {
        ClassId a = ClassName.Intern(nameof(ASelectorMatchesAnElementWithTheClass) + ".a");
        ClassId b = ClassName.Intern(nameof(ASelectorMatchesAnElementWithTheClass) + ".b");
        Element withA = new();
        withA.Classes.Add(b);
        withA.Classes.Add(a);
        Element withB = new();
        withB.Classes.Add(b);
        Element plain = new();

        Selector selector = Selector.ForClass(a);

        Assert.True(selector.Matches(withA));
        Assert.False(selector.Matches(withB));
        Assert.False(selector.Matches(plain));
    }

    [Fact]
    public void ADefaultSelectorMatchesNothing()
    {
        ClassId a = ClassName.Intern(nameof(ADefaultSelectorMatchesNothing) + ".a");
        Element element = new();
        element.Classes.Add(a);
        Selector selector = default;

        Assert.False(selector.Matches(element));
        Assert.False(selector.Matches(new Element()));
    }

    [Fact]
    public void ARuleTakesTheSpecificityOfItsSelector()
    {
        ClassId a = ClassName.Intern(nameof(ARuleTakesTheSpecificityOfItsSelector) + ".a");
        Selector selector = Selector.ForClass(a);
        StyleProperties properties = default;
        properties.Width = 12;

        StyleRule rule = new(in selector, in properties);

        Assert.Equal(selector, rule.Selector);
        Assert.Equal(256, rule.Specificity);
        Assert.Equal(Length.Px(12), rule.Properties.Width);
        Assert.Equal(properties.SetMask, rule.Properties.SetMask);
    }

    [Fact]
    public void ARuleKeepsACopyOfTheProperties()
    {
        ClassId a = ClassName.Intern(nameof(ARuleKeepsACopyOfTheProperties) + ".a");
        Selector selector = Selector.ForClass(a);
        StyleProperties properties = default;
        properties.Width = 12;

        StyleRule rule = new(in selector, in properties);
        properties.Width = 99;
        properties.Opacity = 0.5f;

        Assert.Equal(Length.Px(12), rule.Properties.Width);
        Assert.False(rule.Properties.IsSet(StyleProperty.Opacity));
    }

    [Fact]
    public void TheRulesKeepTheOrderOfAddAtEqualSpecificity()
    {
        ClassId a = ClassName.Intern(nameof(TheRulesKeepTheOrderOfAddAtEqualSpecificity) + ".a");
        ClassId b = ClassName.Intern(nameof(TheRulesKeepTheOrderOfAddAtEqualSpecificity) + ".b");
        ClassId c = ClassName.Intern(nameof(TheRulesKeepTheOrderOfAddAtEqualSpecificity) + ".c");

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(c), WidthOf(1))
            .Add(Selector.ForClass(a), WidthOf(2))
            .Add(Selector.ForClass(b), WidthOf(3))
            .Add(Selector.ForClass(a), WidthOf(4))
            .Build();

        ReadOnlySpan<StyleRule> rules = sheet.Rules;
        Assert.Equal(4, rules.Length);
        Assert.Equal(c, rules[0].Selector.Class);
        Assert.Equal(a, rules[1].Selector.Class);
        Assert.Equal(b, rules[2].Selector.Class);
        Assert.Equal(a, rules[3].Selector.Class);
        Assert.Equal(Length.Px(1), rules[0].Properties.Width);
        Assert.Equal(Length.Px(2), rules[1].Properties.Width);
        Assert.Equal(Length.Px(3), rules[2].Properties.Width);
        Assert.Equal(Length.Px(4), rules[3].Properties.Width);
    }

    [Fact]
    public void TheOrderOfAddHoldsForManyRules()
    {
        ClassId a = ClassName.Intern(nameof(TheOrderOfAddHoldsForManyRules) + ".a");
        StyleSheet.Builder builder = new();
        for (int i = 0; i < 200; i++)
        {
            builder.Add(Selector.ForClass(a), WidthOf(i));
        }

        StyleSheet sheet = builder.Build();

        ReadOnlySpan<StyleRule> rules = sheet.Rules;
        Assert.Equal(200, rules.Length);
        for (int i = 0; i < rules.Length; i++)
        {
            Assert.Equal(Length.Px(i), rules[i].Properties.Width);
        }
    }

    [Fact]
    public void AddThrowsForADefaultSelector()
    {
        StyleSheet.Builder builder = new();

        ArgumentException error = Assert.Throws<ArgumentException>("selector", () => builder.Add(default, WidthOf(1)));

        Assert.Contains("Selector.ForClass", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, builder.Build().Rules.Length);
    }

    [Fact]
    public void AnEmptyBuilderGivesAnEmptySheet()
    {
        StyleSheet sheet = new StyleSheet.Builder().Build();

        Assert.True(sheet.Rules.IsEmpty);
    }

    [Fact]
    public void TwoBuildCallsGiveTwoSheetsWithEqualRules()
    {
        ClassId a = ClassName.Intern(nameof(TwoBuildCallsGiveTwoSheetsWithEqualRules) + ".a");
        ClassId b = ClassName.Intern(nameof(TwoBuildCallsGiveTwoSheetsWithEqualRules) + ".b");
        StyleProperties paint = default;
        paint.BackgroundColor = new Rgba8(30, 34, 44);
        StyleSheet.Builder builder = new StyleSheet.Builder()
            .Add(Selector.ForClass(a), WidthOf(5))
            .Add(Selector.ForClass(b), paint);

        StyleSheet first = builder.Build();
        StyleSheet second = builder.Build();

        Assert.NotSame(first, second);
        Assert.Equal(first.Rules.ToArray(), second.Rules.ToArray());
        Assert.Equal(new Rgba8(30, 34, 44), second.Rules[1].Properties.BackgroundColor);
    }

    [Fact]
    public void AnAddAfterBuildDoesNotChangeTheBuiltSheet()
    {
        ClassId a = ClassName.Intern(nameof(AnAddAfterBuildDoesNotChangeTheBuiltSheet) + ".a");
        ClassId b = ClassName.Intern(nameof(AnAddAfterBuildDoesNotChangeTheBuiltSheet) + ".b");
        StyleSheet.Builder builder = new StyleSheet.Builder().Add(Selector.ForClass(a), WidthOf(5));
        StyleSheet sheet = builder.Build();

        builder.Add(Selector.ForClass(b), WidthOf(6));
        StyleSheet next = builder.Build();

        Assert.Equal(1, sheet.Rules.Length);
        Assert.Equal(a, sheet.Rules[0].Selector.Class);
        Assert.Equal(2, next.Rules.Length);
        Assert.Equal(b, next.Rules[1].Selector.Class);
    }

    private static StyleProperties WidthOf(float width)
    {
        StyleProperties properties = default;
        properties.Width = width;
        return properties;
    }
}
