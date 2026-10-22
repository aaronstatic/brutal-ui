using System;
using System.Globalization;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 3 of the parser for the edge sets (<c>margin</c>, <c>padding</c>, and <c>border-width</c>) and
/// the corner set (<c>border-radius</c>): the CSS order of one to four values, the fifth value, and mixed units.
/// </summary>
/// <remarks>
/// The CSS order is top, right, bottom, left for an edge set, and top-left, top-right, bottom-right, bottom-left for
/// the corner set. The fields of <see cref="LengthEdges"/> use another order, so the tests read each edge by name.
/// </remarks>
public sealed class EdgeAndCornerTests
{
    /// <summary>The rows of <see cref="EachEdgeCountFollowsTheCssOrder"/>: three names and four counts.</summary>
    public static readonly TheoryData<string, int> EdgeRows = BuildRows("margin", "padding", "border-width");

    /// <summary>The rows of <see cref="EachCornerCountFollowsTheCssOrder"/>: one name and four counts.</summary>
    public static readonly TheoryData<string, int> CornerRows = BuildRows("border-radius");

    [Theory]
    [MemberData(nameof(EdgeRows))]
    public void EachEdgeCountFollowsTheCssOrder(string name, int count)
    {
        const string P = nameof(EachEdgeCountFollowsTheCssOrder) + "-";
        StyleSheet sheet = Parse("." + P + "a { " + name + ": " + Values(count) + "; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        LengthEdges edges = name switch
        {
            "margin" => rule.Properties.Margin,
            "padding" => rule.Properties.Padding,
            _ => rule.Properties.BorderWidth,
        };

        // The CSS order of the values a b c d is top, right, bottom, left. Missing values copy as in CSS.
        float[] expected = count switch
        {
            1 => [1f, 1f, 1f, 1f],
            2 => [1f, 2f, 1f, 2f],
            3 => [1f, 2f, 3f, 2f],
            _ => [1f, 2f, 3f, 4f],
        };
        Assert.Equal(LengthUnit.Pixel, edges.Top.Unit);
        Assert.Equal(expected[0], edges.Top.Value);
        Assert.Equal(expected[1], edges.Right.Value);
        Assert.Equal(expected[2], edges.Bottom.Value);
        Assert.Equal(expected[3], edges.Left.Value);
        Assert.Equal(LengthUnit.Pixel, edges.Left.Unit);
        Assert.Equal(LengthUnit.Pixel, edges.Right.Unit);
        Assert.Equal(LengthUnit.Pixel, edges.Bottom.Unit);
    }

    [Theory]
    [MemberData(nameof(CornerRows))]
    public void EachCornerCountFollowsTheCssOrder(string name, int count)
    {
        const string P = nameof(EachCornerCountFollowsTheCssOrder) + "-";
        StyleSheet sheet = Parse("." + P + "a { " + name + ": " + Values(count) + "; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Corners corners = rule.Properties.CornerRadius;

        // The CSS order of the values a b c d is top-left, top-right, bottom-right, bottom-left.
        float[] expected = count switch
        {
            1 => [1f, 1f, 1f, 1f],
            2 => [1f, 2f, 1f, 2f],
            3 => [1f, 2f, 3f, 2f],
            _ => [1f, 2f, 3f, 4f],
        };
        Assert.Equal(expected[0], corners.TopLeft);
        Assert.Equal(expected[1], corners.TopRight);
        Assert.Equal(expected[2], corners.BottomRight);
        Assert.Equal(expected[3], corners.BottomLeft);
    }

    [Theory]
    [InlineData("margin", "1px 2px 3px 4px 5px")]
    [InlineData("padding", "1px 2px 3px 4px 5px 6px")]
    [InlineData("border-width", "1px 2px 3px 4px 5px")]
    [InlineData("border-radius", "1px 2px 3px 4px 5px")]
    [InlineData("margin", "1px 2px 3px 4px  /* c */ 5px")]
    public void AFifthValueIsAnError(string name, string values)
    {
        // The error points at the fifth value, which is the first "5px" of the text.
        string text = ".a { " + name + ": " + values + "; }";
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(1, error.Line);
        int fifth = text.IndexOf("5px", StringComparison.Ordinal);
        Assert.Equal(fifth + 1, error.Column);
        Assert.Contains("'5px'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEdgeSetWithMixedUnits()
    {
        const string P = nameof(AnEdgeSetWithMixedUnits) + "-";
        StyleSheet sheet = Parse("." + P + "a { margin: 4px 10% auto 0; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        LengthEdges margin = rule.Properties.Margin;
        Assert.Equal(Length.Px(4f), margin.Top);
        Assert.Equal(Length.Percent(10f), margin.Right);
        Assert.Equal(Length.Auto, margin.Bottom);
        Assert.Equal(Length.Px(0f), margin.Left);
    }

    [Theory]
    [InlineData("margin: 1px\n\t2px /* c */ 3px")]
    [InlineData("margin:1px 2px 3px")]
    public void TheValuesAreSeparatedByWhitespaceAndComments(string declaration)
    {
        const string P = nameof(TheValuesAreSeparatedByWhitespaceAndComments) + "-";
        StyleSheet sheet = Parse("." + P + "a { " + declaration + " }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal(1f, rule.Properties.Margin.Top.Value);
        Assert.Equal(2f, rule.Properties.Margin.Right.Value);
        Assert.Equal(3f, rule.Properties.Margin.Bottom.Value);
        Assert.Equal(2f, rule.Properties.Margin.Left.Value);
    }

    [Fact]
    public void AnEdgeSetSetsAllFourEdgesAtOneTime()
    {
        const string P = nameof(AnEdgeSetSetsAllFourEdgesAtOneTime) + "-";
        StyleSheet sheet = Parse("." + P + "a { padding: 7px; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal(LengthEdges.All(Length.Px(7f)), rule.Properties.Padding);
        Assert.Equal(Corners.All(0f), rule.Properties.CornerRadius);
        Assert.False(rule.Properties.IsSet(StyleProperty.Margin));
        Assert.False(rule.Properties.IsSet(StyleProperty.BorderWidth));
    }

    [Theory]
    [InlineData("margin: 1px 2px 3px 4px 5px", "5px")]
    [InlineData("border-width: 1px 5%", "5%")]
    [InlineData("border-radius: 1px auto", "auto")]
    [InlineData("padding: 1px  4", "4")]
    [InlineData("margin: 1px/**/2px", "2px")]
    public void AWrongValueInAListPointsAtThatValue(string declaration, string token)
    {
        string text = ".a { " + declaration + "; }";
        StyleParseResult result = new StyleSheetParser().Parse(text);

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(text.LastIndexOf(token, StringComparison.Ordinal) + 1, error.Column);
    }

    private static TheoryData<string, int> BuildRows(params string[] names)
    {
        TheoryData<string, int> rows = [];
        foreach (string name in names)
        {
            for (int count = 1; count <= 4; count++)
            {
                rows.Add(name, count);
            }
        }

        return rows;
    }

    /// <summary>Gives the first <paramref name="count"/> of the values <c>1px 2px 3px 4px</c>.</summary>
    private static string Values(int count)
    {
        string[] all = ["1px", "2px", "3px", "4px"];
        return string.Join(' ', all, 0, count);
    }

    private static StyleSheet Parse(string text)
    {
        return new StyleSheetParser().Parse(text).GetSheetOrThrow();
    }
}
