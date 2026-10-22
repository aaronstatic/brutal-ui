using System;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 4 of the parser: the theme variables of the <c>:root</c> blocks, the <c>var(--name)</c> read,
/// and the variable errors.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class names with the
/// prefix <c>nameof(Test) + "-"</c>. The prefix uses <c>-</c>, because a <c>.</c> ends a class name in the format.
/// </remarks>
public sealed class ThemeVariableTests
{
    [Fact]
    public void AThemeVariableColorsTheButton()
    {
        const string Prefix = nameof(AThemeVariableColorsTheButton) + "-";
        StyleSheet sheet = new StyleSheetParser()
            .Parse(":root { --accent: #3060c0; }\n." + Prefix + "button { background-color: var(--accent); }")
            .GetSheetOrThrow();

        Element root = new() { Name = "root" };
        Element button = new() { Name = "button" };
        button.Classes.Add(ClassName.Intern(Prefix + "button"));
        root.Children.Add(button);

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        string colorLine = "button " + button.Style.BackgroundColor;

        // The second sheet reads a variable that no ':root' block defines.
        StyleParseResult result = new StyleSheetParser()
            .Parse("." + Prefix + "button {\n  background-color: var(--missing);\n}");
        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        string errorLine = "error " + error.Line + ":" + error.Column + " " + error.Kind;

        TestContext.Current.TestOutputHelper?.WriteLine(colorLine);
        TestContext.Current.TestOutputHelper?.WriteLine(errorLine);

        Assert.Equal("button #3060C0FF", colorLine);
        Assert.Equal("error 2:25 UnknownVariable", errorLine);
    }

    [Fact]
    public void AVariableBeforeItsDefinitionResolves()
    {
        const string Prefix = nameof(AVariableBeforeItsDefinitionResolves) + "-";
        StyleSheet sheet = Parse("." + Prefix + "a { color: var(--ink); }\n:root { --ink: #102030; }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal("#102030FF", rule.Properties.Color.ToString());
    }

    [Fact]
    public void ALaterDefinitionWins()
    {
        const string Prefix = nameof(ALaterDefinitionWins) + "-";

        // The later definition in one block wins.
        StyleSheet inBlock = Parse(":root { --ink: #111111; --ink: #3060c0; }\n." + Prefix + "a { color: var(--ink); }");
        Assert.Equal("#3060C0FF", Assert.Single(inBlock.Rules.ToArray()).Properties.Color.ToString());

        // The later definition in a later block wins, also when a rule comes between the blocks.
        StyleSheet inBlocks = Parse(
            ":root { --ink: #111111; }\n." + Prefix + "a { color: var(--ink); }\n:root { --ink: #ffcc00; }");
        Assert.Equal("#FFCC00FF", Assert.Single(inBlocks.Rules.ToArray()).Properties.Color.ToString());
    }

    [Fact]
    public void AVariableHoldsMoreThanOneValue()
    {
        const string Prefix = nameof(AVariableHoldsMoreThanOneValue) + "-";
        StyleSheet sheet = Parse(":root { --pad: 4px 8px; }\n." + Prefix + "a { padding: var(--pad); }");

        LengthEdges padding = Assert.Single(sheet.Rules.ToArray()).Properties.Padding;
        Assert.Equal(Length.Px(4f), padding.Top);
        Assert.Equal(Length.Px(8f), padding.Right);
        Assert.Equal(Length.Px(4f), padding.Bottom);
        Assert.Equal(Length.Px(8f), padding.Left);
    }

    [Fact]
    public void AVariableMixesWithOtherValues()
    {
        const string Prefix = nameof(AVariableMixesWithOtherValues) + "-";
        StyleSheet sheet = Parse(":root { --gap: 4px; }\n." + Prefix + "a { padding: var(--gap) 8px; }");

        LengthEdges padding = Assert.Single(sheet.Rules.ToArray()).Properties.Padding;
        Assert.Equal(Length.Px(4f), padding.Top);
        Assert.Equal(Length.Px(8f), padding.Right);
        Assert.Equal(Length.Px(4f), padding.Bottom);
        Assert.Equal(Length.Px(8f), padding.Left);

        // A variable can come second, and a value can use more than one variable.
        StyleSheet second = Parse(
            ":root { --gap: 4px; --wide: 12px; }\n." + Prefix + "b { padding: 1px var(--gap) var(--wide); }");
        LengthEdges edges = Assert.Single(second.Rules.ToArray()).Properties.Padding;
        Assert.Equal(Length.Px(1f), edges.Top);
        Assert.Equal(Length.Px(4f), edges.Right);
        Assert.Equal(Length.Px(12f), edges.Bottom);
        Assert.Equal(Length.Px(4f), edges.Left);
    }

    [Fact]
    public void AVariableIsLocalToItsSheet()
    {
        const string Prefix = nameof(AVariableIsLocalToItsSheet) + "-";
        StyleSheetParser parser = new();

        StyleParseResult first = parser.Parse(":root { --ink: #3060c0; }\n." + Prefix + "a { color: var(--ink); }");
        Assert.NotNull(first.Sheet);

        // The same parser object reads a second text. The second text does not see the variable of the first text.
        StyleParseResult second = parser.Parse("." + Prefix + "a { color: var(--ink); }");
        Assert.Null(second.Sheet);
        StyleParseError error = Assert.Single(second.Errors);
        Assert.Equal(StyleErrorKind.UnknownVariable, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(Prefix.Length + 17, error.Column);
    }

    [Fact]
    public void VariableNamesAreCaseSensitive()
    {
        StyleParseResult result = new StyleSheetParser().Parse(":root { --Accent: #fff; }\n.a { color: var(--accent); }"u8);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.UnknownVariable, error.Kind);
        Assert.Equal(2, error.Line);
        Assert.Equal(17, error.Column);
        Assert.Contains("--accent", error.Message);

        StyleSheet sheet = Parse(":root { --Accent: #fff; --accent: #000; }\n.a { color: var(--Accent); }");
        Assert.Equal("#FFFFFFFF", Assert.Single(sheet.Rules.ToArray()).Properties.Color.ToString());
    }

    [Theory]
    [InlineData(".a { --x: #fff; }", StyleErrorKind.InvalidVariable, 1, 6)]
    [InlineData(":root { color: #fff; }", StyleErrorKind.InvalidVariable, 1, 9)]
    [InlineData(":root { --a: ; }", StyleErrorKind.InvalidVariable, 1, 14)]
    [InlineData(":root { --a: }", StyleErrorKind.InvalidVariable, 1, 14)]
    [InlineData(":root { --a: var(--b); }", StyleErrorKind.InvalidVariable, 1, 14)]
    [InlineData(":root { --a: 4px var(--b); }", StyleErrorKind.InvalidVariable, 1, 18)]
    [InlineData(":root { --a: #fff; }\n.a { color: var(--a, #000); }", StyleErrorKind.InvalidVariable, 2, 20)]
    [InlineData(":root { --a: #fff; }\n.a { color: var(); }", StyleErrorKind.InvalidVariable, 2, 17)]
    [InlineData(":root { --a: #fff; }\n.a { color: var(a); }", StyleErrorKind.InvalidVariable, 2, 17)]
    [InlineData(":root { --a: #fff; }\n.a { color: var(--a }", StyleErrorKind.InvalidVariable, 2, 21)]
    [InlineData(":root .a { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 1)]
    [InlineData(".a, :root { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 5)]
    [InlineData(".a { color: var(--zz); }", StyleErrorKind.UnknownVariable, 1, 17)]
    [InlineData(".a { color: var( --zz ); }", StyleErrorKind.UnknownVariable, 1, 18)]
    [InlineData(":root { 12px: #fff; }", StyleErrorKind.InvalidVariable, 1, 9)]
    [InlineData(":root { --a #fff; }", StyleErrorKind.UnexpectedToken, 1, 13)]
    [InlineData(":root { --a: 1px", StyleErrorKind.UnexpectedEnd, 1, 17)]
    [InlineData(":root {", StyleErrorKind.UnexpectedEnd, 1, 8)]
    public void EachVariableErrorHasItsPosition(string text, StyleErrorKind kind, int line, int column)
    {
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(kind, error.Kind);
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
        Assert.NotEmpty(error.Message);
    }

    // CR-2: a definition with an error gives one error at the definition. A use of the name gives no second
    // UnknownVariable, because the definition error names the fault.
    [Theory]
    [InlineData(":root { --a: calc(1px); }\n.a { width: var(--a); }", StyleErrorKind.UnsupportedSyntax, 1, 14)]
    [InlineData(":root { --a: ; }\n.a { width: var(--a); }", StyleErrorKind.InvalidVariable, 1, 14)]
    [InlineData(":root { --a: var(--b); }\n.a { width: var(--a); }", StyleErrorKind.InvalidVariable, 1, 14)]
    [InlineData(":root { --a: calc(1px); }\n.a { width: var(--a); height: var(--a); }\n.b { margin: var(--a) 4px; }", StyleErrorKind.UnsupportedSyntax, 1, 14)]
    [InlineData(":root { --a #fff; }\n.a { color: var(--a); }", StyleErrorKind.UnexpectedToken, 1, 13)]
    [InlineData(".a { color: var(--a); }\n:root { --a: #fff", StyleErrorKind.UnexpectedEnd, 2, 18)]
    [InlineData(".a { color: var(--a); }\n:root { --a", StyleErrorKind.UnexpectedEnd, 2, 12)]
    [InlineData(":root { --a: ; --a: #fff; }\n.a { color: var(--a); }", StyleErrorKind.InvalidVariable, 1, 14)]
    public void ADefinitionErrorGivesNoErrorAtTheUse(string text, StyleErrorKind kind, int line, int column)
    {
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(kind, error.Kind);
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
    }

    // CR-2: the declaration rule of DS5.5 applies to pass 1 too. A token error inside a declaration of a ':root' block
    // drops each pass 1 error of that declaration, and the definition is rejected.
    [Theory]
    [InlineData(":root{--é:1px;}", 1, 9)]
    [InlineData(":root { --a: 1px; --é: 2px; }\n.a { width: var(--a); }", 1, 21)]
    [InlineData(":root { --a: #ffé; }\n.a { color: var(--a); }", 1, 17)]
    [InlineData(":root { --a: calé(1px); }\n.a { width: var(--a); }", 1, 17)]
    public void ATokenErrorInADefinitionGivesOneError(string text, int line, int column)
    {
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.UnexpectedCharacter, error.Kind);
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
    }

    [Fact]
    public void ATokenErrorInADefinitionKeepsTheErrorOfTheBlockEnd()
    {
        // The token error drops the UnexpectedEnd of the declaration. The block has no '}', so the block gives its own
        // UnexpectedEnd, as the rule loop of pass 2 does.
        StyleParseResult result = new StyleSheetParser().Parse(":root { --a: 1pé"u8);

        Assert.Null(result.Sheet);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(StyleErrorKind.UnexpectedCharacter, result.Errors[0].Kind);
        Assert.Equal(16, result.Errors[0].Column);
        Assert.Equal(StyleErrorKind.UnexpectedEnd, result.Errors[1].Kind);
        Assert.Equal(17, result.Errors[1].Column);
        Assert.Contains("block", result.Errors[1].Message);
    }

    [Fact]
    public void AVariableThatNoRuleReadsGetsNoValueCheck()
    {
        // CR-6: the parser checks the value of a variable at each use, with the form of the property that reads it.
        // A variable that no rule reads gets no value check, as in CSS.
        const string Prefix = nameof(AVariableThatNoRuleReadsGetsNoValueCheck) + "-";
        StyleSheet sheet = Parse(":root { --a: banana; } ." + Prefix + "x { color: #fff; }");

        Assert.Equal("#FFFFFFFF", Assert.Single(sheet.Rules.ToArray()).Properties.Color.ToString());

        // The same value gives an error when a rule reads it.
        StyleParseResult read = new StyleSheetParser().Parse(":root { --a: banana; } .x { color: var(--a); }"u8);
        Assert.Null(read.Sheet);
        StyleParseError error = Assert.Single(read.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(36, error.Column);
    }

    [Fact]
    public void AValueErrorFromAVariablePointsAtTheUse()
    {
        StyleParseResult result = new StyleSheetParser().Parse(":root { --w: 12; }\n.a { width: var(--w); }"u8);

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);

        // The error points at the 'var' token of the use, not at the '12' in the ':root' block.
        Assert.Equal(2, error.Line);
        Assert.Equal(13, error.Column);
        Assert.Contains("--w", error.Message);
    }

    [Fact]
    public void AValueErrorInAMixedValuePointsAtTheUse()
    {
        // The second value is wrong and comes from the variable. The first value is correct.
        StyleParseResult result = new StyleSheetParser().Parse(":root { --b: red; }\n.a { margin: 4px var(--b); }"u8);

        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(StyleErrorKind.InvalidValue, error.Kind);
        Assert.Equal(2, error.Line);
        Assert.Equal(18, error.Column);
        Assert.Contains("--b", error.Message);
    }

    [Fact]
    public void ARootBlockGivesNoRule()
    {
        StyleSheet sheet = Parse(":root { --a: #fff; }");
        Assert.Empty(sheet.Rules.ToArray());

        StyleSheet empty = Parse(":root { }\n:root/* c */{ --a: #fff; }");
        Assert.Empty(empty.Rules.ToArray());
    }

    [Fact]
    public void TheLastSemicolonOfAVariableIsOptional()
    {
        const string Prefix = nameof(TheLastSemicolonOfAVariableIsOptional) + "-";
        StyleSheet sheet = Parse(":root { --ink: #3060c0 }\n." + Prefix + "a { color: var( --ink ); }");

        Assert.Equal("#3060C0FF", Assert.Single(sheet.Rules.ToArray()).Properties.Color.ToString());
    }

    [Fact]
    public void AVariableValueKeepsItsKeywordAndNumberForms()
    {
        const string Prefix = nameof(AVariableValueKeepsItsKeywordAndNumberForms) + "-";
        StyleSheet sheet = Parse(
            ":root { --dir: column; --o: 0.5; --w: 50%; }\n"
            + "." + Prefix + "a { flex-direction: var(--dir); opacity: var(--o); width: var(--w); }");

        StyleRule rule = Assert.Single(sheet.Rules.ToArray());
        Assert.Equal(FlexDirection.Column, rule.Properties.FlexDirection);
        Assert.Equal(0.5f, rule.Properties.Opacity);
        Assert.Equal(Length.Percent(50f), rule.Properties.Width);
    }

    // Phase 5 (DS5.4): the report holds the errors of pass 1 and of pass 2, merged in the order of the source. In
    // phase 4 this test kept the first error only (DS4.7).
    [Theory]
    [InlineData(".a { colour: #fff; }\n:root { color: #fff; }", StyleErrorKind.UnknownProperty, 1, 6, StyleErrorKind.InvalidVariable, 2, 9)]
    [InlineData(":root { color: #fff; }\n.a { colour: #fff; }", StyleErrorKind.InvalidVariable, 1, 9, StyleErrorKind.UnknownProperty, 2, 6)]
    public void TheErrorsOfBothPassesKeepTheOrderOfTheSource(
        string text,
        StyleErrorKind firstKind,
        int firstLine,
        int firstColumn,
        StyleErrorKind secondKind,
        int secondLine,
        int secondColumn)
    {
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Null(result.Sheet);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(firstKind, result.Errors[0].Kind);
        Assert.Equal(firstLine, result.Errors[0].Line);
        Assert.Equal(firstColumn, result.Errors[0].Column);
        Assert.Equal(secondKind, result.Errors[1].Kind);
        Assert.Equal(secondLine, result.Errors[1].Line);
        Assert.Equal(secondColumn, result.Errors[1].Column);
    }

    private static StyleSheet Parse(string text)
    {
        return new StyleSheetParser().Parse(text).GetSheetOrThrow();
    }
}
