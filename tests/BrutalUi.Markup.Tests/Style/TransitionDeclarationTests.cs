using System;
using System.Globalization;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 6 of the parser: the <c>transition</c> declaration, its five members, its four easings, the
/// duration forms, the comma list, the cascade of two declarations, a variable in the value, and the errors.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class names with the
/// prefix <c>nameof(Test) + "-"</c>. Each test passes the bytes of the text to <c>Parse</c> (decision R2).
/// </remarks>
public sealed class TransitionDeclarationTests
{
    private const int LastMember = (int)StyleProperty.FontSize;

    [Fact]
    public void ATransitionDeclarationIsStoredInTheRule()
    {
        const string Prefix = nameof(ATransitionDeclarationIsStoredInTheRule) + "-";
        StyleRule rule = ParseOneRule(
            "." + Prefix + "button { background-color: #1e222c; transition: background-color 0.2s ease-out; }");

        StyleTransition background = rule.Transitions.Get(StyleProperty.BackgroundColor);
        string backgroundLine = "BackgroundColor "
            + background.Duration.ToString(CultureInfo.InvariantCulture) + "s "
            + EasingName(background.Easing);
        string opacityLine = rule.Transitions.IsSet(StyleProperty.Opacity) ? "Opacity set" : "Opacity none";
        TestContext.Current.TestOutputHelper?.WriteLine(backgroundLine);
        TestContext.Current.TestOutputHelper?.WriteLine(opacityLine);

        Assert.Equal("BackgroundColor 0.2s EaseOut", backgroundLine);
        Assert.Equal("Opacity none", opacityLine);
        Assert.Equal("#1E222CFF", rule.Properties.BackgroundColor.ToString());
    }

    [Theory]
    [InlineData("background-color", StyleProperty.BackgroundColor)]
    [InlineData("border-color", StyleProperty.BorderColor)]
    [InlineData("color", StyleProperty.Color)]
    [InlineData("border-radius", StyleProperty.CornerRadius)]
    [InlineData("opacity", StyleProperty.Opacity)]
    public void EachMemberCanTransition(string name, StyleProperty member)
    {
        const string Prefix = nameof(EachMemberCanTransition) + "-";
        StyleRule rule = ParseOneRule("." + Prefix + "a { transition: " + name + " 1s ease-in; }");

        for (int i = 0; i <= LastMember; i++)
        {
            Assert.Equal(i == (int)member, rule.Transitions.IsSet((StyleProperty)i));
        }

        StyleTransition transition = rule.Transitions.Get(member);
        Assert.Equal(1f, transition.Duration);
        Assert.Equal(TransitionEasing.EaseIn, transition.Easing);

        // A rule with only a transition gives a rule, and the declaration sets no style member.
        Assert.True(rule.Properties.IsEmpty);
    }

    [Theory]
    [InlineData("linear", TransitionEasing.Linear)]
    [InlineData("ease-in", TransitionEasing.EaseIn)]
    [InlineData("ease-out", TransitionEasing.EaseOut)]
    [InlineData("ease-in-out", TransitionEasing.EaseInOut)]
    public void EachEasingKeywordGivesItsMember(string word, TransitionEasing easing)
    {
        const string Prefix = nameof(EachEasingKeywordGivesItsMember) + "-";
        StyleRule rule = ParseOneRule("." + Prefix + "a { transition: opacity 0.5s " + word + "; }");

        StyleTransition transition = rule.Transitions.Get(StyleProperty.Opacity);
        Assert.Equal(easing, transition.Easing);
        Assert.Equal(0.5f, transition.Duration);
    }

    [Theory]
    [InlineData("200ms", 0.2f)]
    [InlineData("1500ms", 1.5f)]
    [InlineData("0ms", 0f)]
    [InlineData("0.5ms", 0.0005f)]
    public void MillisecondsGiveSeconds(string duration, float seconds)
    {
        const string Prefix = nameof(MillisecondsGiveSeconds) + "-";
        StyleRule rule = ParseOneRule("." + Prefix + "a { transition: opacity " + duration + " linear; }");

        Assert.Equal(seconds, rule.Transitions.Get(StyleProperty.Opacity).Duration);
    }

    [Theory]
    [InlineData("0.2s", 0.2f)]
    [InlineData("2s", 2f)]
    [InlineData("0s", 0f)]
    [InlineData("0", 0f)]
    [InlineData("0.0", 0f)]
    [InlineData("-0s", 0f)]
    [InlineData("-0", 0f)]
    public void EachDurationFormGivesItsSeconds(string duration, float seconds)
    {
        const string Prefix = nameof(EachDurationFormGivesItsSeconds) + "-";
        StyleRule rule = ParseOneRule("." + Prefix + "a { transition: opacity " + duration + " linear; }");

        float actual = rule.Transitions.Get(StyleProperty.Opacity).Duration;
        Assert.Equal(seconds, actual);

        // '-0s' gives +0, so the duration prints as 0.
        Assert.False(float.IsNegative(actual));
    }

    [Fact]
    public void ACommaListSetsEachMember()
    {
        const string Prefix = nameof(ACommaListSetsEachMember) + "-";

        // The whitespace around a comma is optional.
        StyleRule rule = ParseOneRule(
            "." + Prefix + "a { transition: background-color 0.2s ease-out, opacity 150ms linear,color 1s ease-in ,"
            + "\n    border-color 0s ease-in-out; }");

        AssertTransition(in rule, StyleProperty.BackgroundColor, 0.2f, TransitionEasing.EaseOut);
        AssertTransition(in rule, StyleProperty.Opacity, 0.15f, TransitionEasing.Linear);
        AssertTransition(in rule, StyleProperty.Color, 1f, TransitionEasing.EaseIn);
        AssertTransition(in rule, StyleProperty.BorderColor, 0f, TransitionEasing.EaseInOut);
        Assert.False(rule.Transitions.IsSet(StyleProperty.CornerRadius));
    }

    [Fact]
    public void ALaterDeclarationWinsForTheSameMember()
    {
        const string Prefix = nameof(ALaterDeclarationWinsForTheSameMember) + "-";

        // A later declaration replaces only the slots of its own members.
        StyleRule rule = ParseOneRule(
            "." + Prefix + "a { transition: opacity 1s linear, color 2s ease-in; transition: opacity 3s ease-out; }");

        AssertTransition(in rule, StyleProperty.Opacity, 3f, TransitionEasing.EaseOut);
        AssertTransition(in rule, StyleProperty.Color, 2f, TransitionEasing.EaseIn);

        // Within one list, a later item for the same member wins.
        StyleRule list = ParseOneRule(
            "." + Prefix + "b { transition: opacity 1s linear, opacity 2s ease-in-out; }");

        AssertTransition(in list, StyleProperty.Opacity, 2f, TransitionEasing.EaseInOut);
    }

    [Fact]
    public void AVariableCanHoldTheDuration()
    {
        const string Prefix = nameof(AVariableCanHoldTheDuration) + "-";
        StyleRule rule = ParseOneRule(
            ":root { --fast: 0.2s; } ." + Prefix + "a { transition: opacity var(--fast) linear; }");

        AssertTransition(in rule, StyleProperty.Opacity, 0.2f, TransitionEasing.Linear);

        // A variable can also hold a full list.
        StyleRule list = ParseOneRule(
            "." + Prefix + "b { transition: var(--both); } :root { --both: color 1s ease-in, opacity 2s ease-out; }");

        AssertTransition(in list, StyleProperty.Color, 1f, TransitionEasing.EaseIn);
        AssertTransition(in list, StyleProperty.Opacity, 2f, TransitionEasing.EaseOut);
    }

    [Fact]
    public void TheRulesOfAListGetTheSameTransitions()
    {
        const string Prefix = nameof(TheRulesOfAListGetTheSameTransitions) + "-";
        StyleSheet sheet = Parse("." + Prefix + "a, ." + Prefix + "b:hover { transition: color 1s linear; }");

        StyleRule[] rules = sheet.Rules.ToArray();
        Assert.Equal(2, rules.Length);
        foreach (StyleRule rule in rules)
        {
            AssertTransition(in rule, StyleProperty.Color, 1f, TransitionEasing.Linear);
        }
    }

    [Fact]
    public void ARuleWithNoTransitionHasAnEmptySet()
    {
        const string Prefix = nameof(ARuleWithNoTransitionHasAnEmptySet) + "-";
        StyleRule rule = ParseOneRule("." + Prefix + "a { color: #fff; }");

        Assert.True(rule.Transitions.IsEmpty);
    }

    [Theory]
    [InlineData("width 1s linear", 1)]
    [InlineData("opacity -1s linear", 9)]
    [InlineData("opacity 1px linear", 9)]
    [InlineData("opacity 1s ease", 12)]
    [InlineData("opacity 1s", 11)]
    [InlineData("opacity", 8)]
    [InlineData("none", 1)]
    [InlineData("all 1s linear", 1)]
    [InlineData("Opacity 1s linear", 1)]
    [InlineData("#fff 1s linear", 1)]
    [InlineData("opacity 1 linear", 9)]
    [InlineData("opacity 1S linear", 9)]
    [InlineData("opacity 50% linear", 9)]
    [InlineData("opacity fast linear", 9)]
    [InlineData("opacity 1s EaseOut", 12)]
    [InlineData("opacity 1s 2s", 12)]
    [InlineData("opacity 1s linear 0.5s", 19)]
    [InlineData("opacity 1s linear opacity", 19)]
    [InlineData("opacity 1s linear,", 19)]
    [InlineData("opacity 1s linear, ,color 1s linear", 20)]
    [InlineData("opacity 1s linear, width 1s linear", 20)]
    [InlineData("opacity 1s, color 1s linear", 11)]
    [InlineData("opacity,color 1s linear", 8)]
    [InlineData("opacity 1slinear", 9)]
    [InlineData("opacity 1s linear#fff", 18)]
    [InlineData("opacity 99999999999999999999999999999999999999999999999999s linear", 9)]
    public void EachWrongTransitionGivesAPositionedError(string value, int offset)
    {
        const string Prefix = nameof(EachWrongTransitionGivesAPositionedError) + "-";
        string head = "." + Prefix + "a { transition: ";
        StyleParseResult result = new StyleSheetParser().Parse(Encoding.UTF8.GetBytes(head + value + "; }"), "wrong.css");

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        TestContext.Current.TestOutputHelper?.WriteLine(error.ToString());
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(head.Length + offset, error.Column);
        Assert.NotEmpty(error.Message);
    }

    [Fact]
    public void AMemberOutsideTheFiveGivesAMessageThatNamesTheFive()
    {
        const string Prefix = nameof(AMemberOutsideTheFiveGivesAMessageThatNamesTheFive) + "-";
        StyleParseResult result = new StyleSheetParser().Parse(
            Encoding.UTF8.GetBytes("." + Prefix + "a { transition: width 1s linear; }"),
            "wrong.css");

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Contains("'width'", error.Message, StringComparison.Ordinal);
        foreach (string name in new[] { "'background-color'", "'border-color'", "'color'", "'border-radius'", "'opacity'" })
        {
            Assert.Contains(name, error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AMissingPartBeforeTheBracePointsAtTheBrace()
    {
        const string Prefix = nameof(AMissingPartBeforeTheBracePointsAtTheBrace) + "-";
        string head = "." + Prefix + "a { transition: opacity 1s ";
        StyleParseResult result = new StyleSheetParser().Parse(Encoding.UTF8.GetBytes(head + "}"), "wrong.css");

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(head.Length + 1, error.Column);
    }

    [Fact]
    public void AWrongDurationFromAVariablePointsAtTheUse()
    {
        const string Prefix = nameof(AWrongDurationFromAVariablePointsAtTheUse) + "-";
        string head = ":root { --slow: 2px; } ." + Prefix + "a { transition: opacity ";
        StyleParseResult result = new StyleSheetParser().Parse(
            Encoding.UTF8.GetBytes(head + "var(--slow) linear; }"),
            "wrong.css");

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(head.Length + 1, error.Column);
        Assert.Contains("var(--slow)", error.Message, StringComparison.Ordinal);
    }

    private static StyleSheet Parse(string text)
    {
        return new StyleSheetParser().Parse(Encoding.UTF8.GetBytes(text), "transition.css").GetSheetOrThrow();
    }

    private static StyleRule ParseOneRule(string text)
    {
        return Assert.Single(Parse(text).Rules.ToArray());
    }

    private static void AssertTransition(in StyleRule rule, StyleProperty member, float duration, TransitionEasing easing)
    {
        Assert.True(rule.Transitions.IsSet(member), member.ToString());
        StyleTransition transition = rule.Transitions.Get(member);
        Assert.Equal(duration, transition.Duration);
        Assert.Equal(easing, transition.Easing);
    }

    private static string EasingName(TransitionEasing easing)
    {
        return easing switch
        {
            TransitionEasing.Linear => "Linear",
            TransitionEasing.EaseIn => "EaseIn",
            TransitionEasing.EaseOut => "EaseOut",
            TransitionEasing.EaseInOut => "EaseInOut",
            _ => "<easing " + ((int)easing).ToString(CultureInfo.InvariantCulture) + ">",
        };
    }
}
