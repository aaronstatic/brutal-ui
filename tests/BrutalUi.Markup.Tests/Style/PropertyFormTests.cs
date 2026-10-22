using System;
using System.Collections.Generic;
using System.Globalization;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 3 of the parser: the demo, the 32 property names, the keyword tables, the length forms, the
/// number read, the font map, and the order of the declarations.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class names with the
/// prefix <c>nameof(Test) + "-"</c>. The prefix uses <c>-</c>, because a <c>.</c> ends a class name in the format.
/// </remarks>
public sealed class PropertyFormTests
{
    private const int MemberCount = (int)StyleProperty.FontSize + 1;

    /// <summary>The rows of <see cref="EachPropertyNameSetsItsOwnMember"/>: one row for each member.</summary>
    public static readonly TheoryData<string, string, StyleProperty, string> PropertyRows = new()
    {
        { "flex-direction", "row-reverse", StyleProperty.FlexDirection, "RowReverse" },
        { "width", "12px", StyleProperty.Width, "Pixel:12" },
        { "height", "50%", StyleProperty.Height, "Percent:50" },
        { "margin", "1px 2px 3px 4px", StyleProperty.Margin, "Pixel:4,Pixel:1,Pixel:2,Pixel:3" },
        { "padding", "5px", StyleProperty.Padding, "Pixel:5,Pixel:5,Pixel:5,Pixel:5" },
        { "border-width", "2px", StyleProperty.BorderWidth, "Pixel:2,Pixel:2,Pixel:2,Pixel:2" },
        { "flex-grow", "2", StyleProperty.FlexGrow, "2" },
        { "flex-shrink", "3", StyleProperty.FlexShrink, "3" },
        { "flex-basis", "30px", StyleProperty.FlexBasis, "Pixel:30" },
        { "min-width", "10px", StyleProperty.MinWidth, "Pixel:10" },
        { "min-height", "11px", StyleProperty.MinHeight, "Pixel:11" },
        { "max-width", "90%", StyleProperty.MaxWidth, "Percent:90" },
        { "max-height", "91%", StyleProperty.MaxHeight, "Percent:91" },
        { "justify-content", "center", StyleProperty.JustifyContent, "Center" },
        { "align-items", "flex-end", StyleProperty.AlignItems, "FlexEnd" },
        { "align-self", "center", StyleProperty.AlignSelf, "Center" },
        { "align-content", "space-evenly", StyleProperty.AlignContent, "SpaceEvenly" },
        { "flex-wrap", "wrap", StyleProperty.FlexWrap, "Wrap" },
        { "display", "none", StyleProperty.Display, "None" },
        { "position", "absolute", StyleProperty.Position, "Absolute" },
        { "left", "1px", StyleProperty.Left, "Pixel:1" },
        { "top", "2px", StyleProperty.Top, "Pixel:2" },
        { "right", "3px", StyleProperty.Right, "Pixel:3" },
        { "bottom", "4px", StyleProperty.Bottom, "Pixel:4" },
        { "overflow", "hidden", StyleProperty.Overflow, "Hidden" },
        { "background-color", "#102030", StyleProperty.BackgroundColor, "#102030FF" },
        { "border-color", "#405060", StyleProperty.BorderColor, "#405060FF" },
        { "border-radius", "3px 4px 5px 6px", StyleProperty.CornerRadius, "3,4,5,6" },
        { "opacity", "0.5", StyleProperty.Opacity, "0.5" },
        { "color", "#708090", StyleProperty.Color, "#708090FF" },
        { "font", "title", StyleProperty.Font, "7" },
        { "font-size", "18px", StyleProperty.FontSize, "18" },
    };

    /// <summary>The rows of <see cref="EachKeywordGivesItsMember"/>: each keyword of each enum table.</summary>
    public static readonly TheoryData<string, string, StyleProperty, string> KeywordRows = new()
    {
        { "flex-direction", "column", StyleProperty.FlexDirection, "Column" },
        { "flex-direction", "column-reverse", StyleProperty.FlexDirection, "ColumnReverse" },
        { "flex-direction", "row", StyleProperty.FlexDirection, "Row" },
        { "flex-direction", "row-reverse", StyleProperty.FlexDirection, "RowReverse" },
        { "justify-content", "flex-start", StyleProperty.JustifyContent, "FlexStart" },
        { "justify-content", "center", StyleProperty.JustifyContent, "Center" },
        { "justify-content", "flex-end", StyleProperty.JustifyContent, "FlexEnd" },
        { "justify-content", "space-between", StyleProperty.JustifyContent, "SpaceBetween" },
        { "justify-content", "space-around", StyleProperty.JustifyContent, "SpaceAround" },
        { "justify-content", "space-evenly", StyleProperty.JustifyContent, "SpaceEvenly" },
        { "align-items", "auto", StyleProperty.AlignItems, "Auto" },
        { "align-items", "flex-start", StyleProperty.AlignItems, "FlexStart" },
        { "align-items", "center", StyleProperty.AlignItems, "Center" },
        { "align-items", "flex-end", StyleProperty.AlignItems, "FlexEnd" },
        { "align-items", "stretch", StyleProperty.AlignItems, "Stretch" },
        { "align-items", "space-between", StyleProperty.AlignItems, "SpaceBetween" },
        { "align-items", "space-around", StyleProperty.AlignItems, "SpaceAround" },
        { "align-items", "space-evenly", StyleProperty.AlignItems, "SpaceEvenly" },
        { "flex-wrap", "nowrap", StyleProperty.FlexWrap, "NoWrap" },
        { "flex-wrap", "wrap", StyleProperty.FlexWrap, "Wrap" },
        { "flex-wrap", "wrap-reverse", StyleProperty.FlexWrap, "WrapReverse" },
        { "display", "flex", StyleProperty.Display, "Flex" },
        { "display", "none", StyleProperty.Display, "None" },
        { "position", "relative", StyleProperty.Position, "Relative" },
        { "position", "absolute", StyleProperty.Position, "Absolute" },
        { "overflow", "visible", StyleProperty.Overflow, "Visible" },
        { "overflow", "hidden", StyleProperty.Overflow, "Hidden" },
        { "overflow", "scroll", StyleProperty.Overflow, "Scroll" },
    };

    [Fact]
    public void ATextSheetLaysOutAPanel()
    {
        const string Prefix = nameof(ATextSheetLaysOutAPanel) + "-";
        StyleSheet sheet = Parse("." + Prefix + "panel { width: 50%; height: 40px; margin: 10px 20px; }");

        Element root = new() { Name = "root" };
        Element panel = new() { Name = "panel" };
        panel.Classes.Add(ClassName.Intern(Prefix + "panel"));
        root.Children.Add(panel);

        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        float4 rect = panel.Layout.Rect;
        string line = "panel "
            + rect.X.ToString(CultureInfo.InvariantCulture) + ","
            + rect.Y.ToString(CultureInfo.InvariantCulture) + ","
            + rect.Z.ToString(CultureInfo.InvariantCulture) + ","
            + rect.W.ToString(CultureInfo.InvariantCulture);
        TestContext.Current.TestOutputHelper?.WriteLine(line);

        Assert.Equal("panel 20,10,200,40", line);
    }

    [Fact]
    public void EachMemberHasOneRowAndOneName()
    {
        Assert.Equal(32, MemberCount);
        Assert.Equal(MemberCount, PropertyTable.Entries.Length);

        bool[] seen = new bool[MemberCount];
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (PropertyEntry entry in PropertyTable.Entries)
        {
            Assert.False(seen[(int)entry.Property], entry.Name);
            seen[(int)entry.Property] = true;
            Assert.True(names.Add(entry.Name), entry.Name);
        }

        Assert.DoesNotContain(false, seen);

        foreach (PropertyEntry entry in PropertyTable.Entries)
        {
            Assert.True(PropertyTable.TryGet(System.Text.Encoding.UTF8.GetBytes(entry.Name), out PropertyEntry found));
            Assert.Equal(entry, found);
        }

        Assert.False(PropertyTable.TryGet("Width"u8, out _));
    }

    [Theory]
    [MemberData(nameof(PropertyRows))]
    public void EachPropertyNameSetsItsOwnMember(string name, string value, StyleProperty member, string expected)
    {
        const string P = nameof(EachPropertyNameSetsItsOwnMember) + "-";
        StyleSheet sheet = ParseWithFonts("." + P + "a { " + name + ": " + value + "; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        for (int i = 0; i < MemberCount; i++)
        {
            Assert.Equal(i == (int)member, rule.Properties.IsSet((StyleProperty)i));
        }

        Assert.Equal(expected, Render(in rule.Properties, member));
    }

    [Theory]
    [MemberData(nameof(KeywordRows))]
    public void EachKeywordGivesItsMember(string name, string word, StyleProperty member, string expected)
    {
        const string P = nameof(EachKeywordGivesItsMember) + "-";
        StyleSheet sheet = Parse("." + P + "a { " + name + ": " + word + "; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.True(rule.Properties.IsSet(member));
        Assert.Equal(expected, Render(in rule.Properties, member));
    }

    [Fact]
    public void TheKeywordTablesHaveTheirWords()
    {
        Assert.Equal(4, PropertyTable.FlexDirectionKeywords.Length);
        Assert.Equal(6, PropertyTable.JustifyKeywords.Length);
        Assert.Equal(8, PropertyTable.AlignKeywords.Length);
        Assert.Equal(3, PropertyTable.FlexWrapKeywords.Length);
        Assert.Equal(2, PropertyTable.DisplayKeywords.Length);
        Assert.Equal(2, PropertyTable.PositionKeywords.Length);
        Assert.Equal(3, PropertyTable.OverflowKeywords.Length);
    }

    [Theory]
    [InlineData("align-self")]
    [InlineData("align-content")]
    public void AlignSelfAndAlignContentTakeTheAlignWords(string name)
    {
        const string P = nameof(AlignSelfAndAlignContentTakeTheAlignWords) + "-";
        foreach (Keyword<Align> keyword in PropertyTable.AlignKeywords)
        {
            StyleSheet sheet = Parse("." + P + "a { " + name + ": " + keyword.Text + "; }");
            StyleRule rule = Assert.Single(sheet.Rules.ToArray());
            Align actual = name == "align-self" ? rule.Properties.AlignSelf : rule.Properties.AlignContent;
            Assert.Equal(keyword.Value, actual);
        }
    }

    [Theory]
    [InlineData("12px", LengthUnit.Pixel, 12f)]
    [InlineData("-4px", LengthUnit.Pixel, -4f)]
    [InlineData(".5px", LengthUnit.Pixel, 0.5f)]
    [InlineData("50%", LengthUnit.Percent, 50f)]
    [InlineData("1.25%", LengthUnit.Percent, 1.25f)]
    [InlineData("auto", LengthUnit.Auto, 0f)]
    [InlineData("0", LengthUnit.Pixel, 0f)]
    public void EachLengthFormGivesItsLength(string text, LengthUnit unit, float value)
    {
        const string P = nameof(EachLengthFormGivesItsLength) + "-";
        StyleSheet sheet = Parse("." + P + "a { width: " + text + "; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal(unit, rule.Properties.Width.Unit);
        Assert.Equal(value, rule.Properties.Width.Value);
    }

    [Fact]
    public void ABareNumberForALengthIsAnError()
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { width: 12; }"u8);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(13, error.Column);
        Assert.Contains("'12'", error.Message, StringComparison.Ordinal);
        Assert.Contains("px", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NumbersReadInTheInvariantCulture()
    {
        const string P = nameof(NumbersReadInTheInvariantCulture) + "-";
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal("0,5", 0.5f.ToString(CultureInfo.CurrentCulture));

            StyleSheet sheet = Parse("." + P + "a { opacity: 0.5; width: 1.5px; flex-grow: 2.25; }");

            StyleRule rule = Assert.Single(sheet.Rules.ToArray());
            Assert.Equal(0.5f, rule.Properties.Opacity);
            Assert.Equal(1.5f, rule.Properties.Width.Value);
            Assert.Equal(2.25f, rule.Properties.FlexGrow);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void AFontNameReadsTheFontMap()
    {
        const string P = nameof(AFontNameReadsTheFontMap) + "-";
        Dictionary<string, FontHandle> map = new() { ["title"] = new FontHandle(3), ["body-2"] = new FontHandle(4) };
        StyleSheetParser parser = new(new StyleSheetParser.CreateInfo { Fonts = map });

        // A later change of the caller map does not change the parser.
        map["title"] = new FontHandle(99);
        map["other"] = new FontHandle(5);

        StyleSheet sheet = parser.Parse("." + P + "a { font: title; }\n." + P + "b { font: body-2; }").GetSheetOrThrow();
        StyleRule[] rules = sheet.Rules.ToArray();
        Assert.Equal(2, rules.Length);
        Assert.Equal(new FontHandle(3), rules[0].Properties.Font);
        Assert.Equal(new FontHandle(4), rules[1].Properties.Font);

        StyleParseResult copied = parser.Parse(".a { font: other; }"u8);
        Assert.Equal(StyleErrorKind.UnknownFont, Assert.Single(copied.Errors).Kind);
    }

    [Fact]
    public void DefaultGivesTheFontIdZero()
    {
        const string P = nameof(DefaultGivesTheFontIdZero) + "-";

        StyleSheet plain = Parse("." + P + "a { font: default; }");
        StyleRule rule = Assert.Single(plain.Rules.ToArray());
        Assert.True(rule.Properties.IsSet(StyleProperty.Font));
        Assert.Equal(new FontHandle(0), rule.Properties.Font);

        // A key named default in the map wins over the built-in id 0.
        Dictionary<string, FontHandle> map = new() { ["default"] = new FontHandle(6) };
        StyleSheet mapped = new StyleSheetParser(new StyleSheetParser.CreateInfo { Fonts = map })
            .Parse("." + P + "b { font: default; }")
            .GetSheetOrThrow();
        Assert.Equal(new FontHandle(6), Assert.Single(mapped.Rules.ToArray()).Properties.Font);
    }

    [Fact]
    public void AnUnknownFontIsAnError()
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { font: missing; }"u8);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.UnknownFont, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(12, error.Column);
        Assert.Contains("'missing'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALaterDeclarationWins()
    {
        const string P = nameof(ALaterDeclarationWins) + "-";
        StyleSheet sheet = Parse("." + P + "a { width: 10px; margin: 1px; width: 20px; margin: 2px 3px; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal(20f, rule.Properties.Width.Value);
        Assert.Equal(3f, rule.Properties.Margin.Left.Value);
        Assert.Equal(2f, rule.Properties.Margin.Top.Value);
    }

    [Fact]
    public void AColorRuleLetsTheFontInherit()
    {
        const string P = nameof(AColorRuleLetsTheFontInherit) + "-";
        StyleSheet sheet = Parse("." + P + "parent { font-size: 24px; }\n." + P + "child { color: #ffcc00; }");

        Element root = new() { Name = "root" };
        Element parent = new() { Name = "parent" };
        Element child = new() { Name = "child" };
        parent.Classes.Add(ClassName.Intern(P + "parent"));
        child.Classes.Add(ClassName.Intern(P + "child"));
        root.Children.Add(parent);
        parent.Children.Add(child);

        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        Assert.Equal(24f, parent.Style.FontSize);
        Assert.Equal(24f, child.Style.FontSize);
        Assert.Equal(new Rgba8(0xFF, 0xCC, 0x00, 255), child.Style.Color);
    }

    [Theory]
    [InlineData("flex-direction", "sideways", 0)]
    [InlineData("flex-direction", "Row", 0)]
    [InlineData("flex-direction", "12px", 0)]
    [InlineData("justify-content", "stretch", 0)]
    [InlineData("display", "block", 0)]
    [InlineData("width", "12em", 0)]
    [InlineData("width", "#fff", 0)]
    [InlineData("width", "red", 0)]
    [InlineData("flex-basis", "1.5", 0)]
    [InlineData("border-width", "5%", 0)]
    [InlineData("border-width", "auto", 0)]
    [InlineData("border-radius", "10%", 0)]
    [InlineData("border-radius", "8", 0)]
    [InlineData("flex-grow", "1px", 0)]
    [InlineData("flex-grow", "auto", 0)]
    [InlineData("opacity", "half", 0)]
    [InlineData("opacity", "50%", 0)]
    [InlineData("font-size", "auto", 0)]
    [InlineData("font-size", "50%", 0)]
    [InlineData("font-size", "12", 0)]
    [InlineData("font", "12px", 0)]
    [InlineData("margin", "1px 2px 12", 8)]
    [InlineData("margin", "1px 2px3px", 4)]
    public void EachWrongFormIsAnInvalidValueAtTheFirstTokenAtFault(string name, string value, int offset)
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { " + name + ": " + value + "; }");

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(name.Length + 8 + offset, error.Column);
    }

    [Fact]
    public void ADeclarationWithNoValueIsAnError()
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { width: ; }"u8);

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(13, error.Column);
    }

    [Fact]
    public void AnUnknownPropertyIsStillAnError()
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { margin-top: 4px; }"u8);

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.UnknownProperty, error.Kind);
        Assert.Equal(6, error.Column);
    }

    [Fact]
    public void ThePropertyNamesAreCaseSensitive()
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { Width: 4px; }"u8);

        Assert.Equal(StyleErrorKind.UnknownProperty, Assert.Single(result.Errors).Kind);
    }

    private static StyleSheet Parse(string text)
    {
        return new StyleSheetParser().Parse(text).GetSheetOrThrow();
    }

    private static StyleSheet ParseWithFonts(string text)
    {
        Dictionary<string, FontHandle> map = new() { ["title"] = new FontHandle(7) };
        return new StyleSheetParser(new StyleSheetParser.CreateInfo { Fonts = map }).Parse(text).GetSheetOrThrow();
    }

    /// <summary>Gives the text of the value of one member, so a row can compare it with a text.</summary>
    private static string Render(in StyleProperties p, StyleProperty member)
    {
        return member switch
        {
            StyleProperty.FlexDirection => p.FlexDirection.ToString(),
            StyleProperty.Width => Render(p.Width),
            StyleProperty.Height => Render(p.Height),
            StyleProperty.Margin => Render(p.Margin),
            StyleProperty.Padding => Render(p.Padding),
            StyleProperty.BorderWidth => Render(p.BorderWidth),
            StyleProperty.FlexGrow => Render(p.FlexGrow),
            StyleProperty.FlexShrink => Render(p.FlexShrink),
            StyleProperty.FlexBasis => Render(p.FlexBasis),
            StyleProperty.MinWidth => Render(p.MinWidth),
            StyleProperty.MinHeight => Render(p.MinHeight),
            StyleProperty.MaxWidth => Render(p.MaxWidth),
            StyleProperty.MaxHeight => Render(p.MaxHeight),
            StyleProperty.JustifyContent => p.JustifyContent.ToString(),
            StyleProperty.AlignItems => p.AlignItems.ToString(),
            StyleProperty.AlignSelf => p.AlignSelf.ToString(),
            StyleProperty.AlignContent => p.AlignContent.ToString(),
            StyleProperty.FlexWrap => p.FlexWrap.ToString(),
            StyleProperty.Display => p.Display.ToString(),
            StyleProperty.Position => p.Position.ToString(),
            StyleProperty.Left => Render(p.Left),
            StyleProperty.Top => Render(p.Top),
            StyleProperty.Right => Render(p.Right),
            StyleProperty.Bottom => Render(p.Bottom),
            StyleProperty.Overflow => p.Overflow.ToString(),
            StyleProperty.BackgroundColor => p.BackgroundColor.ToString(),
            StyleProperty.BorderColor => p.BorderColor.ToString(),
            StyleProperty.CornerRadius => Render(p.CornerRadius.TopLeft) + "," + Render(p.CornerRadius.TopRight) + ","
                + Render(p.CornerRadius.BottomRight) + "," + Render(p.CornerRadius.BottomLeft),
            StyleProperty.Opacity => Render(p.Opacity),
            StyleProperty.Color => p.Color.ToString(),
            StyleProperty.Font => p.Font.Id.ToString(CultureInfo.InvariantCulture),
            StyleProperty.FontSize => Render(p.FontSize),
            _ => throw new ArgumentOutOfRangeException(nameof(member)),
        };
    }

    private static string Render(float value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Render(Length length)
    {
        return length.Unit + ":" + Render(length.Value);
    }

    private static string Render(LengthEdges edges)
    {
        return Render(edges.Left) + "," + Render(edges.Top) + "," + Render(edges.Right) + "," + Render(edges.Bottom);
    }
}
