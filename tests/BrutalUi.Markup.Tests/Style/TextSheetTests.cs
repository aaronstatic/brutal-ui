using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 1 of the parser: the demo, the color forms, the three color properties, the whitespace and
/// comment rule, the cascade order, and the error result.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class names with the
/// prefix <c>nameof(Test) + "-"</c>. The prefix uses <c>-</c>, because a <c>.</c> ends a class name in the format.
/// </remarks>
public sealed class TextSheetTests
{
    private const int LastMember = (int)StyleProperty.FontSize;

    [Fact]
    public void ATextSheetPaintsAPanel()
    {
        const string Prefix = nameof(ATextSheetPaintsAPanel) + "-";
        StyleSheet sheet = new StyleSheetParser()
            .Parse("." + Prefix + "panel { background-color: #1e222c; }")
            .GetSheetOrThrow();

        Element root = new() { Name = "root" };
        Element panel = new() { Name = "panel" };
        Element plain = new() { Name = "plain" };
        panel.Classes.Add(ClassName.Intern(Prefix + "panel"));
        root.Children.Add(panel);
        root.Children.Add(plain);

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        string panelLine = "panel " + panel.Style.BackgroundColor;
        string plainLine = "plain " + plain.Style.BackgroundColor;
        TestContext.Current.TestOutputHelper?.WriteLine(panelLine);
        TestContext.Current.TestOutputHelper?.WriteLine(plainLine);

        Assert.Equal("panel #1E222CFF", panelLine);
        Assert.Equal("plain #00000000", plainLine);
    }

    [Theory]
    [InlineData("#fff", "#FFFFFFFF")]
    [InlineData("#FFF", "#FFFFFFFF")]
    [InlineData("#1e222c", "#1E222CFF")]
    [InlineData("#1E222C80", "#1E222C80")]
    [InlineData("#3060c0ff", "#3060C0FF")]
    [InlineData("#a1B", "#AA11BBFF")]
    [InlineData("#00000000", "#00000000")]
    public void EachColorFormGivesTheColor(string value, string expected)
    {
        const string Prefix = nameof(EachColorFormGivesTheColor) + "-";
        StyleSheet sheet = Parse("." + Prefix + "a { background-color: " + value + "; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal(expected, rule.Properties.BackgroundColor.ToString());
    }

    [Theory]
    [InlineData("#ff")]
    [InlineData("#ffff")]
    [InlineData("#fffff")]
    [InlineData("#fffffff")]
    [InlineData("#fffffffff")]
    [InlineData("#ggg")]
    [InlineData("#12_456")]
    [InlineData("#1e-22c")]
    [InlineData("12px")]
    [InlineData("50%")]
    [InlineData("0")]
    [InlineData("red")]
    [InlineData(".")]
    public void EachWrongColorIsAnInvalidValueAtItsToken(string value)
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { color: " + value + "; }");

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(13, error.Column);
        Assert.Contains("'" + value + "'", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("background-color", StyleProperty.BackgroundColor)]
    [InlineData("border-color", StyleProperty.BorderColor)]
    [InlineData("color", StyleProperty.Color)]
    public void EachColorPropertySetsItsOwnMember(string name, StyleProperty member)
    {
        const string Prefix = nameof(EachColorPropertySetsItsOwnMember) + "-";
        StyleSheet sheet = Parse("." + Prefix + "a { " + name + ": #102030; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        for (int i = 0; i <= LastMember; i++)
        {
            Assert.Equal(i == (int)member, rule.Properties.IsSet((StyleProperty)i));
        }

        Rgba8 value = member switch
        {
            StyleProperty.BackgroundColor => rule.Properties.BackgroundColor,
            StyleProperty.BorderColor => rule.Properties.BorderColor,
            _ => rule.Properties.Color,
        };
        Assert.Equal(new Rgba8(0x10, 0x20, 0x30, 255), value);
    }

    [Fact]
    public void CommentsAndWhitespaceChangeNothing()
    {
        const string P = nameof(CommentsAndWhitespaceChangeNothing) + "-";
        string plain = "." + P + "a { background-color: #1e222c; color: #fff; }\n." + P + "b { border-color: #3060c0; }";
        string comments =
            "/*0*/./*1*/" + P + "a/*2*/{/*3*/background-color/*4*/:/*5*/#1e222c/*6*/;/* é */color/**/:/**/#fff/**/;/**/}"
            + "/**/./**/" + P + "b/**/{/**/border-color/**/:/**/#3060c0/**/;/**/}/**/";
        string lineBreaks =
            "\r\n\r\n  ." + P + "a\r\n{\r\n\tbackground-color :\r\n #1e222c ;\r\n color:#fff;\r\n}\r\n."
            + P + "b{border-color:#3060c0;}\r\n";
        string bom = "﻿" + plain;

        StyleSheet expected = Parse(plain);
        Assert.Equal(2, expected.Rules.Length);

        AssertSameSheet(expected, Parse(comments));
        AssertSameSheet(expected, Parse(lineBreaks));
        AssertSameSheet(expected, Parse(bom));
        AssertSameSheet(expected, new StyleSheetParser().Parse(Utf8WithBom(plain), "bom.css").GetSheetOrThrow());
    }

    [Fact]
    public void TheLastSemicolonIsOptional()
    {
        const string P = nameof(TheLastSemicolonIsOptional) + "-";
        StyleSheet sheet = Parse("." + P + "a { color: #fff }\n." + P + "b{background-color:#000;color:#123}");

        StyleRule[] rules = sheet.Rules.ToArray();
        Assert.Equal(2, rules.Length);
        Assert.Equal(new Rgba8(255, 255, 255, 255), rules[0].Properties.Color);
        Assert.Equal(new Rgba8(0, 0, 0, 255), rules[1].Properties.BackgroundColor);
        Assert.Equal(new Rgba8(0x11, 0x22, 0x33, 255), rules[1].Properties.Color);
    }

    [Fact]
    public void ALaterRuleWinsAtEqualSpecificity()
    {
        const string P = nameof(ALaterRuleWinsAtEqualSpecificity) + "-";
        Element first = Styled(
            "." + P + "a { background-color: #ff0000; }\n." + P + "b { background-color: #0000ff; }",
            P + "a",
            P + "b");
        Element second = Styled(
            "." + P + "b { background-color: #0000ff; }\n." + P + "a { background-color: #ff0000; }",
            P + "a",
            P + "b");
        Element sameClass = Styled(
            "." + P + "c { color: #ff0000; }\n." + P + "c { color: #00ff00; }",
            P + "c");

        Assert.Equal(new Rgba8(0, 0, 255, 255), first.Style.BackgroundColor);
        Assert.Equal(new Rgba8(255, 0, 0, 255), second.Style.BackgroundColor);
        Assert.Equal(new Rgba8(0, 255, 0, 255), sameClass.Style.Color);
    }

    [Fact]
    public void AnErrorGivesNoSheet()
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { colour: #fff; }"u8);

        Assert.Null(result.Sheet);
        Assert.False(result.Succeeded);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.UnknownProperty, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(6, error.Column);
        Assert.Equal("stylesheet", error.SourceName);
        Assert.Contains("'colour'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSheetOrThrowNamesTheError()
    {
        StyleParseResult result = new StyleSheetParser().Parse(".a { colour: #fff; }"u8);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.GetSheetOrThrow());

        Assert.Contains("stylesheet(1,6): UnknownProperty:", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSheetOrThrowGivesOneLineForEachError()
    {
        StyleParseResult result = new StyleSheetParser().Parse("é é"u8, "two.css");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.GetSheetOrThrow());

        string[] lines = exception.Message.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("two.css(1,1): UnexpectedCharacter: ", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("two.css(1,3): UnexpectedCharacter: ", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void ASuccessHasASheetAndNoError()
    {
        const string P = nameof(ASuccessHasASheetAndNoError) + "-";
        StyleParseResult result = new StyleSheetParser().Parse("." + P + "a { color: #fff; }");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Sheet);
        Assert.Empty(result.Errors);
        Assert.Same(result.Sheet, result.GetSheetOrThrow());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n\t ")]
    [InlineData("/* only a comment */")]
    [InlineData("﻿")]
    public void AnEmptyTextGivesAnEmptySheet(string text)
    {
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.GetSheetOrThrow().Rules.Length);
    }

    [Fact]
    public void ARuleWithNoDeclarationGivesNoRule()
    {
        const string P = nameof(ARuleWithNoDeclarationGivesNoRule) + "-";
        StyleSheet sheet = Parse("." + P + "a { }\n." + P + "b {}\n." + P + "c { color: #fff; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal(ClassName.Intern(P + "c"), rule.Selector.Class);
    }

    [Fact]
    public void TheTwoOverloadsGiveTheSameSheet()
    {
        const string P = nameof(TheTwoOverloadsGiveTheSameSheet) + "-";
        string text = "." + P + "a { border-color: #3060c080; }";

        StyleSheet fromText = Parse(text);
        StyleSheet fromBytes = new StyleSheetParser(new StyleSheetParser.CreateInfo()).Parse(Encoding.UTF8.GetBytes(text)).GetSheetOrThrow();

        AssertSameSheet(fromText, fromBytes);
    }

    [Theory]
    // Phase 2 reads 'a {' as a type selector and '.a color' as a descendant chain, so these two rows changed in phase 2.
    [InlineData("1 { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 1)]
    [InlineData(". a { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 2)]
    [InlineData(".1 { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 1)]
    [InlineData(".a color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 10)]
    [InlineData(".a { color #fff; }", StyleErrorKind.UnexpectedToken, 1, 12)]
    [InlineData(".a { : #fff; }", StyleErrorKind.UnexpectedToken, 1, 6)]
    [InlineData(".a { color: ; }", StyleErrorKind.InvalidValue, 1, 13)]
    [InlineData(".a { color: }", StyleErrorKind.InvalidValue, 1, 13)]
    [InlineData(".a { color: #fff #000; }", StyleErrorKind.InvalidValue, 1, 18)]
    // Phase 3 knows 'width', so this row uses 'size' (a name that the format never knows).
    [InlineData(".a {\n  size: 12px;\n}", StyleErrorKind.UnknownProperty, 2, 3)]
    [InlineData(".a { color: #fff;", StyleErrorKind.UnexpectedEnd, 1, 18)]
    [InlineData(".a { color: #fff", StyleErrorKind.UnexpectedEnd, 1, 17)]
    [InlineData(".a { color:", StyleErrorKind.UnexpectedEnd, 1, 12)]
    [InlineData(".a", StyleErrorKind.UnexpectedEnd, 1, 3)]
    [InlineData(".", StyleErrorKind.UnexpectedEnd, 1, 2)]
    [InlineData(".a { color: #fff; } }", StyleErrorKind.UnexpectedToken, 1, 21)]
    [InlineData(".a { color: #fff; } é",StyleErrorKind.UnexpectedCharacter, 1, 21)]
    [InlineData(".a { color: #fff; } /* x", StyleErrorKind.UnterminatedComment, 1, 21)]
    public void EachWrongFormGivesOnePositionedError(string text, StyleErrorKind kind, int line, int column)
    {
        StyleParseResult result = new StyleSheetParser().Parse(Encoding.UTF8.GetBytes(text), "wrong.css");

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(kind, error.Kind);
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
        Assert.Equal("wrong.css", error.SourceName);
        Assert.NotEmpty(error.Message);
    }

    [Fact]
    public void ANullArgumentThrows()
    {
        // The analyzer BRUTALCORE0011 rejects a constant string argument of the string overload, so the test uses locals.
        StyleSheetParser parser = new();
        string? noText = null;
        string? noName = null;
        string text = Encoding.UTF8.GetString(".a { color: #fff; }"u8);

        Assert.Throws<ArgumentNullException>(() => parser.Parse(noText!));
        Assert.Throws<ArgumentNullException>(() => parser.Parse(text, noName!));
        Assert.Throws<ArgumentNullException>(() => parser.Parse(".a { color: #fff; }"u8, noName!));
    }

    private static StyleSheet Parse(string text)
    {
        return new StyleSheetParser().Parse(text).GetSheetOrThrow();
    }

    private static byte[] Utf8WithBom(string text)
    {
        byte[] body = Encoding.UTF8.GetBytes(text);
        byte[] bytes = new byte[body.Length + 3];
        bytes[0] = 0xEF;
        bytes[1] = 0xBB;
        bytes[2] = 0xBF;
        body.CopyTo(bytes, 3);
        return bytes;
    }

    /// <summary>Parses a sheet, styles one element with the given classes under a root, and runs <c>Update</c>.</summary>
    private static Element Styled(string text, params string[] classes)
    {
        Element root = new();
        Element element = new();
        foreach (string name in classes)
        {
            element.Classes.Add(ClassName.Intern(name));
        }

        root.Children.Add(element);

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.AddSheet(Parse(text));
        context.Update(0f);
        return element;
    }

    private static void AssertSameSheet(StyleSheet expected, StyleSheet actual)
    {
        StyleRule[] expectedRules = expected.Rules.ToArray();
        StyleRule[] actualRules = actual.Rules.ToArray();
        Assert.Equal(expectedRules.Length, actualRules.Length);

        for (int r = 0; r < expectedRules.Length; r++)
        {
            StyleRule e = expectedRules[r];
            StyleRule a = actualRules[r];
            Assert.Equal(e.Specificity, a.Specificity);
            Assert.Equal(e.Selector.Combinator, a.Selector.Combinator);
            Assert.Equal(e.Selector.Subject.Class, a.Selector.Subject.Class);
            Assert.Equal(e.Selector.Subject.Type, a.Selector.Subject.Type);
            Assert.Equal(e.Selector.Subject.States, a.Selector.Subject.States);
            Assert.Equal(e.Selector.Subject.Name, a.Selector.Subject.Name);
            Assert.Equal(e.Selector.Ancestor.Class, a.Selector.Ancestor.Class);

            for (int i = 0; i <= LastMember; i++)
            {
                Assert.Equal(e.Properties.IsSet((StyleProperty)i), a.Properties.IsSet((StyleProperty)i));
            }

            Assert.Equal(e.Properties.BackgroundColor, a.Properties.BackgroundColor);
            Assert.Equal(e.Properties.BorderColor, a.Properties.BorderColor);
            Assert.Equal(e.Properties.Color, a.Properties.Color);
        }
    }
}
