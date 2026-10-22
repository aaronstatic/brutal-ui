using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

public sealed class StyleTokenizerTests
{
    private const string Source = "test.css";

    [Theory]
    [InlineData("abc", nameof(StyleTokenKind.Ident))]
    [InlineData("--accent", nameof(StyleTokenKind.Ident))]
    [InlineData("flex-start", nameof(StyleTokenKind.Ident))]
    [InlineData("-x", nameof(StyleTokenKind.Ident))]
    [InlineData("_a1-b", nameof(StyleTokenKind.Ident))]
    [InlineData("--", nameof(StyleTokenKind.Ident))]
    [InlineData("#1e222c", nameof(StyleTokenKind.Hash))]
    [InlineData("#title", nameof(StyleTokenKind.Hash))]
    [InlineData("0", nameof(StyleTokenKind.Number))]
    [InlineData("12", nameof(StyleTokenKind.Number))]
    [InlineData(".5", nameof(StyleTokenKind.Number))]
    [InlineData("-4", nameof(StyleTokenKind.Number))]
    [InlineData("-.5", nameof(StyleTokenKind.Number))]
    [InlineData("1.25", nameof(StyleTokenKind.Number))]
    [InlineData("-4px", nameof(StyleTokenKind.Dimension))]
    [InlineData("0.2s", nameof(StyleTokenKind.Dimension))]
    [InlineData("12px", nameof(StyleTokenKind.Dimension))]
    [InlineData("50%", nameof(StyleTokenKind.Percentage))]
    [InlineData("-1.5%", nameof(StyleTokenKind.Percentage))]
    [InlineData(":", nameof(StyleTokenKind.Colon))]
    [InlineData(";", nameof(StyleTokenKind.Semicolon))]
    [InlineData(",", nameof(StyleTokenKind.Comma))]
    [InlineData(".", nameof(StyleTokenKind.Dot))]
    [InlineData(">", nameof(StyleTokenKind.Greater))]
    [InlineData("{", nameof(StyleTokenKind.LeftBrace))]
    [InlineData("}", nameof(StyleTokenKind.RightBrace))]
    [InlineData("(", nameof(StyleTokenKind.LeftParen))]
    [InlineData(")", nameof(StyleTokenKind.RightParen))]
    [InlineData(" \t\r\n\f", nameof(StyleTokenKind.Whitespace))]
    [InlineData("*", nameof(StyleTokenKind.Delim))]
    [InlineData("[", nameof(StyleTokenKind.Delim))]
    [InlineData("+", nameof(StyleTokenKind.Delim))]
    [InlineData("~", nameof(StyleTokenKind.Delim))]
    [InlineData("!", nameof(StyleTokenKind.Delim))]
    [InlineData("@", nameof(StyleTokenKind.Delim))]
    [InlineData("\"", nameof(StyleTokenKind.Delim))]
    [InlineData("#", nameof(StyleTokenKind.Delim))]
    [InlineData("-", nameof(StyleTokenKind.Delim))]
    [InlineData("/", nameof(StyleTokenKind.Delim))]
    [InlineData("", nameof(StyleTokenKind.End))]
    public void EachTokenKindHasItsText(string text, string kind)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

        Assert.Empty(errors);
        Assert.Equal(kind, tokens[0].Kind.ToString());
        Assert.Equal(text, Encoding.UTF8.GetString(tokens[0].GetText(utf8)));
        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
        Assert.Equal(StyleTokenKind.End, tokens[^1].Kind);
        Assert.Equal(text.Length == 0 ? 1 : 2, tokens.Count);
    }

    [Theory]
    [InlineData("12px", "12", "px")]
    [InlineData("-4px", "-4", "px")]
    [InlineData("0.2s", "0.2", "s")]
    [InlineData(".5em", ".5", "em")]
    [InlineData("50%", "50", "%")]
    [InlineData("7", "7", "")]
    [InlineData("1--x", "1", "--x")]
    public void ANumericTokenSplitsTheNumberAndTheUnit(string text, string number, string unit)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        (List<StyleToken> tokens, _) = Tokenize(utf8);

        Assert.Equal(number, Encoding.UTF8.GetString(tokens[0].GetNumber(utf8)));
        Assert.Equal(unit, Encoding.UTF8.GetString(tokens[0].GetUnit(utf8)));
    }

    [Theory]
    [InlineData(".a", "Dot Ident")]
    [InlineData("1.", "Number Dot")]
    [InlineData("1.5.5", "Number Number")]
    [InlineData("1-2", "Number Number")]
    [InlineData("- x", "Delim Whitespace Ident")]
    [InlineData("12px%", "Dimension Delim")]
    [InlineData("a>b", "Ident Greater Ident")]
    [InlineData("#fff;", "Hash Semicolon")]
    [InlineData("var(--x)", "Ident LeftParen Ident RightParen")]
    [InlineData(".a:hover > .b { color: #fff; }", "Dot Ident Colon Ident Whitespace Greater Whitespace Dot Ident Whitespace LeftBrace Whitespace Ident Colon Whitespace Hash Semicolon Whitespace RightBrace")]
    public void EachTextGivesItsTokenList(string text, string kinds)
    {
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(Encoding.UTF8.GetBytes(text));

        Assert.Empty(errors);
        Assert.Equal(kinds, Kinds(tokens));
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\r\nb")]
    [InlineData("a\rb")]
    public void ALineBreakStartsANewLine(string text)
    {
        (List<StyleToken> tokens, _) = Tokenize(Encoding.UTF8.GetBytes(text));

        Assert.Equal("Ident Whitespace Ident", Kinds(tokens));
        Assert.Equal(2, tokens[2].Line);
        Assert.Equal(1, tokens[2].Column);
    }

    [Theory]
    [InlineData("a\n\nb", 3)]
    [InlineData("a\r\n\r\nb", 3)]
    [InlineData("a\r\rb", 3)]
    [InlineData("a\n\rb", 3)]
    [InlineData("a\f b", 1)]
    public void EachLineBreakCountsOneTime(string text, int line)
    {
        (List<StyleToken> tokens, _) = Tokenize(Encoding.UTF8.GetBytes(text));

        Assert.Equal(line, tokens[2].Line);
    }

    [Fact]
    public void AColumnCountsCodePoints()
    {
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(Encoding.UTF8.GetBytes("/* é */ .a"));

        Assert.Empty(errors);
        Assert.Equal("Whitespace Dot Ident", Kinds(tokens));
        Assert.Equal(9, tokens[1].Column);
        Assert.Equal(9, tokens[1].Offset);
        TestContext.Current.TestOutputHelper?.WriteLine($"dot {tokens[1].Line}:{tokens[1].Column} offset {tokens[1].Offset}");

        (tokens, _) = Tokenize(Encoding.UTF8.GetBytes("/* \U0001F600 */ .a"));
        Assert.Equal(9, tokens[1].Column);

        (tokens, _) = Tokenize(Encoding.UTF8.GetBytes("\t\t.a"));
        Assert.Equal(3, tokens[1].Column);
    }

    [Fact]
    public void ABomAddsNoColumn()
    {
        byte[] utf8 = [0xEF, 0xBB, 0xBF, (byte)'.', (byte)'a'];
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

        Assert.Empty(errors);
        Assert.Equal("Dot Ident", Kinds(tokens));
        Assert.Equal(3, tokens[0].Offset);
        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
        Assert.Equal(2, tokens[1].Column);
    }

    [Fact]
    public void ABomAfterTheStartIsAnError()
    {
        byte[] utf8 = [(byte)'a', 0xEF, 0xBB, 0xBF];
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

        StyleParseError error = Assert.Single(errors);
        Assert.Equal(StyleErrorKind.UnexpectedCharacter, error.Kind);
        Assert.Equal(2, error.Column);
        Assert.Equal("Ident", Kinds(tokens));
    }

    [Fact]
    public void ACommentJoinsTheWhitespace()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("a /* c */\n/* d */ b");
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

        Assert.Empty(errors);
        Assert.Equal("Ident Whitespace Ident", Kinds(tokens));
        Assert.Equal(" /* c */\n/* d */ ", Encoding.UTF8.GetString(tokens[1].GetText(utf8)));
        Assert.Equal(2, tokens[2].Line);
        Assert.Equal(9, tokens[2].Column);

        // A comment that touches the whitespace on one side joins the run, and the run starts at the comment.
        utf8 = Encoding.UTF8.GetBytes("a/* c */ b");
        (tokens, _) = Tokenize(utf8);
        Assert.Equal("Ident Whitespace Ident", Kinds(tokens));
        Assert.Equal(1, tokens[1].Offset);
    }

    [Theory]
    [InlineData("./**/a", "Dot Ident")]
    [InlineData("a/* c */b", "Ident Ident")]
    [InlineData("/* only */", "")]
    [InlineData("/**/", "")]
    [InlineData("/***/.a", "Dot Ident")]
    public void ACommentAloneGivesNoToken(string text, string kinds)
    {
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(Encoding.UTF8.GetBytes(text));

        Assert.Empty(errors);
        Assert.Equal(kinds, Kinds(tokens));
    }

    [Fact]
    public void ANonAsciiByteOutsideACommentIsAnError()
    {
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(Encoding.UTF8.GetBytes("a é b"));

        StyleParseError error = Assert.Single(errors);
        TestContext.Current.TestOutputHelper?.WriteLine(error.ToString());
        Assert.Equal(StyleErrorKind.UnexpectedCharacter, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(3, error.Column);
        Assert.Contains("'é'", error.Message, StringComparison.Ordinal);
        Assert.Equal(Source, error.SourceName);

        // The tokenizer skips the full code point and gives no token for it.
        Assert.Equal("Ident Whitespace Whitespace Ident", Kinds(tokens));
        Assert.Equal(5, tokens[3].Column);
    }

    // Each rejected sequence counts one column (CR-3). Row 1: 0xFF, the stray 0x80, and the cut code point 0xC3 at the
    // end each take one column, so 'b' is at column 4. Row 2: the stray 0x80 takes column 1, so the error of 'é' at
    // column 2 is not a copy, and the merge keeps it. Row 3 adds 'x' after the two errors.
    [Theory]
    [InlineData(new byte[] { (byte)'a', 0xFF, 0x80, (byte)'b', 0xC3 }, new[] { 2, 3, 5 }, "0xFF", "Ident Ident", 6)]
    [InlineData(new byte[] { 0x80, 0xC3, 0xA9 }, new[] { 1, 2 }, "0x80", "", 3)]
    [InlineData(new byte[] { 0x80, 0xC3, 0xA9, (byte)'x' }, new[] { 1, 2 }, "0x80", "Ident", 4)]
    public void AnInvalidUtf8ByteIsAnError(byte[] utf8, int[] columns, string firstQuote, string kinds, int endColumn)
    {
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

        Assert.All(errors, e => Assert.Equal(StyleErrorKind.UnexpectedCharacter, e.Kind));
        Assert.All(errors, e => Assert.Equal(1, e.Line));
        Assert.Equal(columns, errors.Select(e => e.Column));
        Assert.Contains(firstQuote, errors[0].Message, StringComparison.Ordinal);
        Assert.Equal(kinds, Kinds(tokens));
        Assert.Equal(utf8.Length, tokens[^1].Offset);
        Assert.Equal(endColumn, tokens[^1].Column);

        // The merge of the parser keeps each token error, because no two token errors share a column. Row 3 also gives
        // a selector error at the End token, a real second fault.
        StyleParseResult result = new StyleSheetParser().Parse(utf8, Source);
        Assert.Equal(columns, result.Errors.Where(e => e.Kind == StyleErrorKind.UnexpectedCharacter).Select(e => e.Column));
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x0B)]
    [InlineData(0x1F)]
    [InlineData(0x7F)]
    public void AControlByteOutsideACommentIsAnError(int value)
    {
        byte[] utf8 = [(byte)'a', (byte)value, (byte)'b'];
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

        StyleParseError error = Assert.Single(errors);
        Assert.Equal(StyleErrorKind.UnexpectedCharacter, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(2, error.Column);
        Assert.Equal("Ident Ident", Kinds(tokens));
        Assert.Equal(3, tokens[1].Column);
    }

    [Fact]
    public void ACommentCanHoldEachByte()
    {
        byte[] utf8 = [(byte)'/', (byte)'*', 0x00, 0x7F, 0xFF, 0xC3, 0xA9, (byte)'*', (byte)'/', (byte)'.', (byte)'a'];
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

        Assert.Empty(errors);
        Assert.Equal("Dot Ident", Kinds(tokens));
    }

    [Fact]
    public void AnUnterminatedCommentIsAnError()
    {
        (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(Encoding.UTF8.GetBytes(".a\n  /* open"));

        StyleParseError error = Assert.Single(errors);
        TestContext.Current.TestOutputHelper?.WriteLine(error.ToString());
        Assert.Equal(StyleErrorKind.UnterminatedComment, error.Kind);
        Assert.Equal(2, error.Line);
        Assert.Equal(3, error.Column);
        Assert.Equal("Dot Ident Whitespace", Kinds(tokens));
        Assert.Equal(2, tokens[^1].Line);
        Assert.Equal(10, tokens[^1].Column);

        (tokens, errors) = Tokenize(Encoding.UTF8.GetBytes("/*/"));
        error = Assert.Single(errors);
        Assert.Equal(StyleErrorKind.UnterminatedComment, error.Kind);
        Assert.Equal(1, error.Column);
        Assert.Equal("", Kinds(tokens));
    }

    [Theory]
    [InlineData("", 0, 1, 1)]
    [InlineData(".a", 2, 1, 3)]
    [InlineData(".a\n", 3, 2, 1)]
    [InlineData(".a\r\n", 4, 2, 1)]
    [InlineData(".a\r", 3, 2, 1)]
    [InlineData("é", 2, 1, 2)]
    public void TheEndTokenHoldsThePositionAfterTheText(string text, int offset, int line, int column)
    {
        (List<StyleToken> tokens, _) = Tokenize(Encoding.UTF8.GetBytes(text));

        StyleToken end = tokens[^1];
        Assert.Equal(StyleTokenKind.End, end.Kind);
        Assert.Equal(offset, end.Offset);
        Assert.Equal(0, end.Length);
        Assert.Equal(line, end.Line);
        Assert.Equal(column, end.Column);
    }

    [Fact]
    public void NoInputMakesTheTokenizerThrow()
    {
        Random random = new(1234);
        byte[] alphabet = Encoding.UTF8.GetBytes("ab-_.#0123456789%:;,>{}()/*! \t\r\n\f\"@");

        for (int i = 0; i < 2000; i++)
        {
            byte[] utf8 = new byte[random.Next(0, 64)];
            for (int j = 0; j < utf8.Length; j++)
            {
                utf8[j] = i % 2 == 0 ? (byte)random.Next(0, 256) : alphabet[random.Next(alphabet.Length)];
            }

            (List<StyleToken> tokens, List<StyleParseError> errors) = Tokenize(utf8);

            Assert.Equal(StyleTokenKind.End, tokens[^1].Kind);
            Assert.Equal(utf8.Length, tokens[^1].Offset);

            int next = 0;
            foreach (StyleToken token in tokens)
            {
                Assert.True(token.Offset >= next, "The tokens overlap.");
                Assert.True(token.Offset + token.Length <= utf8.Length, "A token ends after the text.");
                Assert.True(token.Line >= 1 && token.Column >= 1, "A token has no position.");
                Assert.True(token.Kind == StyleTokenKind.End || token.Length > 0, "A token is empty.");
                next = token.Offset + token.Length;
            }

            Assert.All(errors, e => Assert.True(e.Line >= 1 && e.Column >= 1));
        }
    }

    [Fact]
    public void AnErrorTextHasTheSourceAndThePosition()
    {
        List<StyleParseError> errors = [];
        StyleTokenizer.Tokenize(Encoding.UTF8.GetBytes("a\n é"), "hud.css", errors);

        StyleParseError error = Assert.Single(errors);
        Assert.StartsWith("hud.css(2,2): UnexpectedCharacter: ", error.ToString(), StringComparison.Ordinal);
        Assert.EndsWith(error.Message, error.ToString(), StringComparison.Ordinal);

        StyleParseError empty = default;
        Assert.Equal("(0,0): UnexpectedCharacter: ", empty.ToString());
    }

    [Fact]
    public void EachErrorKindHasItsName()
    {
        string[] names =
        [
            "UnexpectedCharacter", "UnterminatedComment", "UnexpectedToken", "UnexpectedEnd",
            "InvalidSelector", "UnsupportedSelector", "UnknownPseudoState",
            "UnknownProperty", "InvalidValue", "UnknownFont",
            "UnknownVariable", "InvalidVariable", "UnsupportedSyntax",
        ];

        for (int i = 0; i < names.Length; i++)
        {
            Assert.Equal(names[i], StyleParseError.GetKindName((StyleErrorKind)i));
            Assert.Equal(names[i], ((StyleErrorKind)i).ToString());
        }

        Assert.Equal("13", StyleParseError.GetKindName((StyleErrorKind)13));
    }

    private static (List<StyleToken> Tokens, List<StyleParseError> Errors) Tokenize(byte[] utf8)
    {
        List<StyleParseError> errors = [];
        List<StyleToken> tokens = StyleTokenizer.Tokenize(utf8, Source, errors);
        return (tokens, errors);
    }

    /// <summary>Gives the kinds of the tokens without the End token, joined by a space.</summary>
    private static string Kinds(List<StyleToken> tokens)
    {
        return string.Join(" ", tokens.Where(t => t.Kind != StyleTokenKind.End).Select(t => t.Kind.ToString()));
    }
}
