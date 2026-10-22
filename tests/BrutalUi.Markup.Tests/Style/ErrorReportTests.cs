using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 5 of the parser: the error report. Each error kind at its position, the range checks of the
/// values, the recovery after an error, and the message text.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class names with the
/// prefix <c>nameof(Test) + "-"</c>. A text with an error interns no name (DS5.11), so most rows use short names.
/// </remarks>
public sealed class ErrorReportTests
{
    /// <summary>
    /// The rows of the kind theory: the text, the kind, the line, the column, and the source text at fault that the
    /// message quotes. Each text gives exactly one error.
    /// </summary>
    private static readonly (string Text, StyleErrorKind Kind, int Line, int Column, string Quote)[] Rows =
    [
        ("\u0001", StyleErrorKind.UnexpectedCharacter, 1, 1, "'0x01'"),
        (".a { color: é; }", StyleErrorKind.UnexpectedCharacter, 1, 13, "'é'"),
        (".a { color: #fff; } /* x", StyleErrorKind.UnterminatedComment, 1, 21, "'/*'"),
        ("1 { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 1, "'1'"),
        (".a { color;}", StyleErrorKind.UnexpectedToken, 1, 11, "';'"),
        (".a { color: #fff;", StyleErrorKind.UnexpectedEnd, 1, 18, "the end of the text"),
        (".a", StyleErrorKind.UnexpectedEnd, 1, 3, "the end of the text"),
        ("{ color: #fff; }", StyleErrorKind.InvalidSelector, 1, 1, "'{'"),
        (".a, :root { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 5, "':'"),
        (".a.b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 3, "'.'"),
        ("* { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 1, "'*'"),
        (".a::before { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 4, "':'"),
        (".a:hovr { color: #fff; }", StyleErrorKind.UnknownPseudoState, 1, 4, "'hovr'"),
        (".a { colour: #fff; }", StyleErrorKind.UnknownProperty, 1, 6, "'colour'"),
        (".a {\n  colour: #fff;\n}", StyleErrorKind.UnknownProperty, 2, 3, "'colour'"),
        (".a { width: 12em; }", StyleErrorKind.InvalidValue, 1, 13, "'12em'"),
        (".a { color: #ff; }", StyleErrorKind.InvalidValue, 1, 13, "'#ff'"),
        (".a { font: mono; }", StyleErrorKind.UnknownFont, 1, 12, "'mono'"),
        (".a { color: var(--zz); }", StyleErrorKind.UnknownVariable, 1, 17, "'--zz'"),
        (".a { --x: #fff; }", StyleErrorKind.InvalidVariable, 1, 6, "'--x'"),
        ("@import \"a\";", StyleErrorKind.UnsupportedSyntax, 1, 1, "'@'"),
        ("@media x { .a { color: #fff; } }", StyleErrorKind.UnsupportedSyntax, 1, 1, "'@'"),
        (".a { color: #fff !important; }", StyleErrorKind.UnsupportedSyntax, 1, 18, "'!'"),
        (".a { width: calc(1px); }", StyleErrorKind.UnsupportedSyntax, 1, 13, "'calc'"),
        (":root { --a: calc(1px); }", StyleErrorKind.UnsupportedSyntax, 1, 14, "'calc'"),
    ];

    /// <summary>Gets the rows of the kind theory.</summary>
    public static TheoryData<string, StyleErrorKind, int, int, string> KindRows
    {
        get
        {
            TheoryData<string, StyleErrorKind, int, int, string> data = [];
            foreach ((string text, StyleErrorKind kind, int line, int column, string quote) in Rows)
            {
                data.Add(text, kind, line, column, quote);
            }

            return data;
        }
    }

    [Fact]
    public void ASheetWithThreeMistakesReportsEachOne()
    {
        const string Prefix = nameof(ASheetWithThreeMistakesReportsEachOne) + "-";
        string text = "." + Prefix + "panel {\n  colour: #fff;\n  width: 12em;\n";

        StyleParseResult result = new StyleSheetParser().Parse(text);

        List<string> lines = [];
        foreach (StyleParseError error in result.Errors)
        {
            lines.Add("error " + error.Line + ":" + error.Column + " " + error.Kind);
        }

        lines.Add(result.Sheet is null ? "sheet null" : "sheet " + result.Sheet.Rules.Length + " rules");

        foreach (string line in lines)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(line);
        }

        Assert.Equal(
            ["error 2:3 UnknownProperty", "error 3:10 InvalidValue", "error 4:1 UnexpectedEnd", "sheet null"],
            lines);
    }

    [Theory]
    [MemberData(nameof(KindRows))]
    public void EachErrorKindHasItsPosition(string text, StyleErrorKind kind, int line, int column, string quote)
    {
        _ = quote;
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(kind, error.Kind);
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
    }

    [Fact]
    public void TheKindRowsHoldEachKind()
    {
        // The name table gives a number for a value outside the enum. Thus the enum has no member after UnsupportedSyntax.
        const byte Last = (byte)StyleErrorKind.UnsupportedSyntax;
        Assert.Equal("13", StyleParseError.GetKindName((StyleErrorKind)(Last + 1)));

        for (byte value = 0; value <= Last; value++)
        {
            StyleErrorKind kind = (StyleErrorKind)value;
            Assert.NotEqual(value.ToString(System.Globalization.CultureInfo.InvariantCulture), StyleParseError.GetKindName(kind));
            Assert.Contains(Rows, row => row.Kind == kind);
        }

        // The brief asks for one row for each of the three unsupported forms.
        Assert.Contains(Rows, row => row.Kind == StyleErrorKind.UnsupportedSyntax && row.Text.StartsWith("@import", StringComparison.Ordinal));
        Assert.Contains(Rows, row => row.Kind == StyleErrorKind.UnsupportedSyntax && row.Text.Contains("!important", StringComparison.Ordinal));
        Assert.Contains(Rows, row => row.Kind == StyleErrorKind.UnsupportedSyntax && row.Text.Contains("calc(", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(".a { padding: -1px; }", 15, "'-1px'")]
    [InlineData(".a { border-width: -1px; }", 20, "'-1px'")]
    [InlineData(".a { border-radius: -2px; }", 21, "'-2px'")]
    [InlineData(".a { flex-grow: -1; }", 17, "'-1'")]
    [InlineData(".a { flex-shrink: -1; }", 19, "'-1'")]
    [InlineData(".a { font-size: -12px; }", 17, "'-12px'")]
    [InlineData(".a { opacity: 1.5; }", 15, "'1.5'")]
    [InlineData(".a { opacity: -0.1; }", 15, "'-0.1'")]
    [InlineData(".a { padding: auto; }", 15, "'auto'")]
    [InlineData(".a { border-width: auto; }", 20, "'auto'")]
    [InlineData(".a { border-radius: auto; }", 21, "'auto'")]
    [InlineData(".a { font-size: auto; }", 17, "'auto'")]
    [InlineData(".a { border-width: 10%; }", 20, "'10%'")]
    [InlineData(".a { border-radius: 10%; }", 21, "'10%'")]
    [InlineData(".a { font-size: 10%; }", 17, "'10%'")]
    [InlineData(".a { width: 100000000000000000000000000000000000000000000000000px; }", 13, "'100000000000000000000000000000000000000000000000000px'")]
    [InlineData(".a { width: 12em; }", 13, "'12em'")]
    public void EachRangeCheckGivesAnError(string text, int column, string quote)
    {
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(column, error.Column);
        Assert.Contains(quote, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheParserFindsAnErrorAfterABadSelector()
    {
        // The block of the bad rule is skipped, so its wrong declaration gives no error. The next rule gives its error.
        StyleParseResult result = new StyleSheetParser().Parse(".a:hovr { colour: #fff; }\n.b { colour: #fff; }"u8);

        Assert.Null(result.Sheet);
        Assert.Equal(2, result.Errors.Count);
        AssertError(result.Errors[0], StyleErrorKind.UnknownPseudoState, 1, 4);
        AssertError(result.Errors[1], StyleErrorKind.UnknownProperty, 2, 6);

        // A bad selector in a list, and a bad selector with no block: the parser goes on with the next rule.
        StyleParseResult list = new StyleSheetParser().Parse(".a, * { color: #fff; }\n.b:x { }\n.c { width: 1em; }"u8);
        Assert.Equal(3, list.Errors.Count);
        AssertError(list.Errors[0], StyleErrorKind.UnsupportedSelector, 1, 5);
        AssertError(list.Errors[1], StyleErrorKind.UnknownPseudoState, 2, 4);
        AssertError(list.Errors[2], StyleErrorKind.InvalidValue, 3, 13);
    }

    [Fact]
    public void TheParserFindsAnErrorAfterABadDeclaration()
    {
        // Two bad declarations in one rule, then a bad selector in the next rule.
        StyleParseResult result = new StyleSheetParser().Parse(".a { colour: #fff; width: 12em; }\n.b:hovr { }"u8);

        Assert.Null(result.Sheet);
        Assert.Equal(3, result.Errors.Count);
        AssertError(result.Errors[0], StyleErrorKind.UnknownProperty, 1, 6);
        AssertError(result.Errors[1], StyleErrorKind.InvalidValue, 1, 27);
        AssertError(result.Errors[2], StyleErrorKind.UnknownPseudoState, 2, 4);

        // A bad last declaration with no ';' ends at the '}'. The next rule gives its error.
        StyleParseResult last = new StyleSheetParser().Parse(".a { color: #ff }\n.b { opacity: 2; }"u8);
        Assert.Equal(2, last.Errors.Count);
        AssertError(last.Errors[0], StyleErrorKind.InvalidValue, 1, 13);
        AssertError(last.Errors[1], StyleErrorKind.InvalidValue, 2, 15);
    }

    /// <summary>
    /// The declaration skip counts the nested braces (CR-4). A block in a bad value, with or without a ';' inside,
    /// gives exactly one error, and the next declaration reads as a good one.
    /// </summary>
    [Theory]
    [InlineData(".a { color: { x } ; width: 1px; }")]
    [InlineData(".a { color: { x; y } ; width: 1px; }")]
    public void TheDeclarationSkipCountsTheNestedBraces(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);

        StyleParseResult result = new StyleSheetParser().Parse(bytes);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        AssertError(error, StyleErrorKind.InvalidValue, 1, 13);
    }

    [Fact]
    public void TheErrorTextHasTheSourceAndThePosition()
    {
        const string Prefix = nameof(TheErrorTextHasTheSourceAndThePosition) + "-";
        byte[] text = Encoding.UTF8.GetBytes("." + Prefix + "panel {\n  colour: #fff;\n  width: 12em;\n");

        StyleParseResult result = new StyleSheetParser().Parse(text, "hud.css");

        Assert.Equal(3, result.Errors.Count);
        StyleParseError error = result.Errors[0];
        Assert.Equal("hud.css", error.SourceName);
        Assert.NotEmpty(error.Message);
        Assert.Equal("hud.css(2,3): UnknownProperty: " + error.Message, error.ToString());
        Assert.Equal("hud.css(3,10): InvalidValue: " + result.Errors[1].Message, result.Errors[1].ToString());
        Assert.Equal("hud.css(4,1): UnexpectedEnd: " + result.Errors[2].Message, result.Errors[2].ToString());
    }

    [Theory]
    [MemberData(nameof(KindRows))]
    public void EachMessageQuotesTheSourceText(string text, StyleErrorKind kind, int line, int column, string quote)
    {
        _ = kind;
        _ = line;
        _ = column;
        StyleParseResult result = new StyleSheetParser().Parse(text);

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Contains(quote, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EachMessageQuotesAByteThatIsNotUtf8()
    {
        // A string row cannot hold a byte that is not UTF-8, so this test gives the bytes. The message quotes the byte
        // in hex, as it does for a control byte (DS5.12).
        byte[] text = [(byte)'.', (byte)'a', 0xFF, (byte)' ', (byte)'{', (byte)'}', 0xC3];

        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        Assert.Equal(2, result.Errors.Count);
        AssertError(result.Errors[0], StyleErrorKind.UnexpectedCharacter, 1, 3);
        Assert.Contains("'0xFF'", result.Errors[0].Message, StringComparison.Ordinal);
        AssertError(result.Errors[1], StyleErrorKind.UnexpectedCharacter, 1, 7);
        Assert.Contains("'0xC3'", result.Errors[1].Message, StringComparison.Ordinal);
    }

    private static void AssertError(StyleParseError error, StyleErrorKind kind, int line, int column)
    {
        Assert.Equal(kind, error.Kind);
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
    }
}
