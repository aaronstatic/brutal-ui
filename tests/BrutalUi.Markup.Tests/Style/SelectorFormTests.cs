using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 2 of the parser: the demo, each selector form of selector-matching from text, the selector
/// list, the round trip of the text form, the reject list with positions, the case rule, and the specificity.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class names with the
/// prefix <c>nameof(Test) + "-"</c>. A row of a theory writes <c>$</c> in place of the prefix. A type name in a row
/// is a built-in type (<c>Label</c>) or a prefixed name.
/// </remarks>
public sealed class SelectorFormTests
{
    /// <summary>The color <c>#3060c0</c>.</summary>
    private static readonly Rgba8 Blue = new(0x30, 0x60, 0xC0);
    /// <summary>The rows of <see cref="EachSelectorFormGivesTheFactorySelector"/> and <see cref="TheTextFormRoundTrips"/>.</summary>
    public static readonly TheoryData<string> Forms =
    [
        "Label",
        ".$a",
        "#title",
        ":hover",
        ":active",
        ":focus",
        ":disabled",
        ":checked",
        "Label.$a#title:hover:checked",
        ".$a > .$b",
        ".$a>.$b",
        ".$a\n>\t.$b",
        ".$a .$b",
        ".$a  /* c */ .$b",
        "Label:hover > #title",
        ":focus .$b",
        ":hover:hover",
        "#title.$a:checked",
    ];

    [Fact]
    public void AHoverOnAPanelRestylesItsRowAndItsCellFromText()
    {
        const string Prefix = nameof(AHoverOnAPanelRestylesItsRowAndItsCellFromText) + "-";
        string text =
            "." + Prefix + "panel:hover > ." + Prefix + "row { background-color: #3060c0; }\n"
            + "." + Prefix + "panel:hover ." + Prefix + "cell { background-color: #ffcc00; }\n";
        StyleSheet sheet = new StyleSheetParser().Parse(text).GetSheetOrThrow();

        Element root = new() { Name = "root" };
        Element panel = new() { Name = "panel" };
        Element row = new() { Name = "row" };
        Element cell = new() { Name = "cell" };
        panel.Classes.Add(ClassName.Intern(Prefix + "panel"));
        row.Classes.Add(ClassName.Intern(Prefix + "row"));
        cell.Classes.Add(ClassName.Intern(Prefix + "cell"));
        root.Children.Add(panel);
        panel.Children.Add(row);
        row.Children.Add(cell);

        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.AddSheet(sheet);

        context.Update(0f);
        string idleLine = Describe(row, cell);

        panel.States = PseudoStates.Hover;
        context.Update(0f);
        string hoverLine = Describe(row, cell);

        TestContext.Current.TestOutputHelper?.WriteLine(idleLine);
        TestContext.Current.TestOutputHelper?.WriteLine(hoverLine);

        Assert.Equal("row #00000000 cell #00000000", idleLine);
        Assert.Equal("row #3060C0FF cell #FFCC00FF", hoverLine);

        // The hover goes away, and the rules stop to match.
        panel.States = PseudoStates.None;
        context.Update(0f);
        Assert.Equal("row #00000000 cell #00000000", Describe(row, cell));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EachSelectorFormGivesTheFactorySelector(string form)
    {
        const string Prefix = nameof(EachSelectorFormGivesTheFactorySelector) + "-";
        Selector expected = Expected(form, Prefix);

        Selector parsed = ParseOne(form.Replace("$", Prefix, StringComparison.Ordinal));

        Assert.Equal(expected, parsed);
        Assert.Equal(expected.Specificity, parsed.Specificity);
    }

    [Fact]
    public void ASelectorListGivesOneRuleForEachSelector()
    {
        const string P = nameof(ASelectorListGivesOneRuleForEachSelector) + "-";
        string text = "." + P + "a, ." + P + "b > ." + P + "c, #d { background-color: #3060c0; color: #ffcc00 }";

        StyleRule[] rules = Parse(text).Rules.ToArray();

        Assert.Equal(3, rules.Length);
        CompoundSelector b = new() { Class = ClassName.Intern(P + "b") };
        CompoundSelector c = new() { Class = ClassName.Intern(P + "c") };
        Assert.Equal(Selector.ForClass(ClassName.Intern(P + "a")), rules[0].Selector);
        Assert.Equal(Selector.Child(b, c), rules[1].Selector);
        Assert.Equal(Selector.ForName("d"), rules[2].Selector);

        foreach (StyleRule rule in rules)
        {
            Assert.Equal(Blue, rule.Properties.BackgroundColor);
            Assert.Equal(new Rgba8(0xFF, 0xCC, 0x00), rule.Properties.Color);
            Assert.True(rule.Properties.IsSet(StyleProperty.BackgroundColor));
            Assert.True(rule.Properties.IsSet(StyleProperty.Color));
            Assert.False(rule.Properties.IsSet(StyleProperty.BorderColor));
        }
    }

    [Fact]
    public void AListKeepsTheOrderOfTheSourceAtEqualSpecificity()
    {
        const string P = nameof(AListKeepsTheOrderOfTheSourceAtEqualSpecificity) + "-";
        string text = "." + P + "z,." + P + "x ,\n ." + P + "y{color:#fff}";

        StyleRule[] rules = Parse(text).Rules.ToArray();

        Assert.Equal(3, rules.Length);
        Assert.Equal(ClassName.Intern(P + "z"), rules[0].Selector.Class);
        Assert.Equal(ClassName.Intern(P + "x"), rules[1].Selector.Class);
        Assert.Equal(ClassName.Intern(P + "y"), rules[2].Selector.Class);
    }

    [Fact]
    public void AListWithNoDeclarationGivesNoRule()
    {
        const string P = nameof(AListWithNoDeclarationGivesNoRule) + "-";
        string text = "." + P + "a, #b > Label { }";

        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Sheet);
        Assert.Equal(0, result.Sheet.Rules.Length);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void TheTextFormRoundTrips(string form)
    {
        const string Prefix = nameof(TheTextFormRoundTrips) + "-";
        Selector parsed = ParseOne(form.Replace("$", Prefix, StringComparison.Ordinal));

        string printed = parsed.ToString();
        Selector again = ParseOne(printed);

        Assert.Equal(parsed, again);
        Assert.Equal(printed, again.ToString());
        Assert.Equal(Expected(form, Prefix), again);
    }

    [Theory]
    // A second class or a second name.
    [InlineData(".a.b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 3)]
    [InlineData("#a#b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 3)]
    [InlineData(".a/**/.b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 7)]
    [InlineData("Label.a:hover.b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 14)]
    // A third compound.
    [InlineData(".a .b .c { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 7)]
    [InlineData(".a > .b > .c { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 11)]
    [InlineData(".a > .b .c { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 9)]
    [InlineData(".a,\n  .b .c #d { color: #fff; }", StyleErrorKind.UnsupportedSelector, 2, 9)]
    // A CSS form outside the format.
    [InlineData("* { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 1)]
    [InlineData("[x] { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 1)]
    [InlineData(".a + .b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 4)]
    [InlineData(".a ~ .b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 4)]
    [InlineData("::before { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 2)]
    [InlineData(":not(.a) { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 2)]
    [InlineData(".a+.b { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 3)]
    [InlineData(".a[x] { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 3)]
    [InlineData(".a::before { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 4)]
    [InlineData(".a .b * { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 7)]
    [InlineData(":hover(x) { color: #fff; }", StyleErrorKind.UnsupportedSelector, 1, 2)]
    // An unknown state. The compare is case-sensitive.
    [InlineData(":first-child { color: #fff; }", StyleErrorKind.UnknownPseudoState, 1, 2)]
    [InlineData(".a:Hover { color: #fff; }", StyleErrorKind.UnknownPseudoState, 1, 4)]
    // An empty compound.
    [InlineData("> .a { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 1)]
    [InlineData(".a > { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 6)]
    [InlineData(".a, { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 5)]
    [InlineData("{ color: #fff; }", StyleErrorKind.InvalidSelector, 1, 1)]
    [InlineData(".a, , .b { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 5)]
    [InlineData(".a > > .b { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 6)]
    [InlineData(".a .b > { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 9)]
    // ':root' in a chain, in a list, or in a compound.
    [InlineData(":root .a { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 1)]
    [InlineData(".a, :root { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 5)]
    [InlineData(":root, .a { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 1)]
    [InlineData(".a :root { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 4)]
    [InlineData(".a:root { color: #fff; }", StyleErrorKind.InvalidSelector, 1, 3)]
    // ':root' alone starts a block. The block holds variables only, so a property gives InvalidVariable at its name.
    [InlineData(":root { color: #fff; }", StyleErrorKind.InvalidVariable, 1, 9)]
    [InlineData(":root/* x */{ color: #fff; }", StyleErrorKind.InvalidVariable, 1, 15)]
    // The parts of a compound must touch.
    [InlineData(". a { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 2)]
    [InlineData(": hover { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 2)]
    [InlineData(".a:#b { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 4)]
    // Each other token in a selector.
    [InlineData(".a ; { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 4)]
    [InlineData("12px { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 1)]
    [InlineData("# a { color: #fff; }", StyleErrorKind.UnexpectedToken, 1, 1)]
    [InlineData(".a )", StyleErrorKind.UnexpectedToken, 1, 4)]
    // The text ends in a selector.
    [InlineData(".a >", StyleErrorKind.UnexpectedEnd, 1, 5)]
    [InlineData(".a,", StyleErrorKind.UnexpectedEnd, 1, 4)]
    [InlineData(".a:", StyleErrorKind.UnexpectedEnd, 1, 4)]
    [InlineData(".a .b ", StyleErrorKind.UnexpectedEnd, 1, 7)]
    public void EachRejectedFormGivesAPositionedError(string text, StyleErrorKind kind, int line, int column)
    {
        StyleParseResult result = new StyleSheetParser().Parse(Encoding.UTF8.GetBytes(text), "selector.css");

        Assert.Null(result.Sheet);
        StyleParseError error = Assert.Single(result.Errors);
        Assert.Equal(kind, error.Kind);
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
        Assert.Equal("selector.css", error.SourceName);
        Assert.NotEmpty(error.Message);
    }

    [Fact]
    public void ATypeNameIsCaseSensitive()
    {
        Label lower = StyledLabel("label { background-color: #3060c0; }");
        Label upper = StyledLabel("Label { background-color: #3060c0; }");

        Assert.Equal(Rgba8.Transparent, lower.Style.BackgroundColor);
        Assert.Equal(Blue, upper.Style.BackgroundColor);
    }

    [Fact]
    public void AnUnknownTypeMatchesNothingAndIsNoError()
    {
        // The type name is the name of Widget. The parse runs before the first Widget exists.
        string text = Widget.TypeName + " { background-color: #3060c0; }";
        StyleParseResult result = new StyleSheetParser().Parse(text);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Sheet);
        StyleRule rule = Assert.Single(result.Sheet.Rules.ToArray());
        Assert.Equal(ElementType.Intern(Widget.TypeName), rule.Selector.Type);
        Assert.Equal(1, rule.Specificity);

        Element root = new();
        Element plain = new();
        Label label = new(default(Label.CreateInfo));
        Widget widget = new();
        root.Children.Add(plain);
        root.Children.Add(label);
        root.Children.Add(widget);

        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(result.Sheet);
        context.Update(0f);

        Assert.Equal(Rgba8.Transparent, plain.Style.BackgroundColor);
        Assert.Equal(Rgba8.Transparent, label.Style.BackgroundColor);
        Assert.Equal(Blue, widget.Style.BackgroundColor);
    }

    [Fact]
    public void ANameKeepsItsCase()
    {
        Selector selector = ParseOne("#Title");
        Assert.Equal("Title", selector.Subject.Name);

        Element root = new();
        Element upper = new() { Name = "Title" };
        Element lower = new() { Name = "title" };
        root.Children.Add(upper);
        root.Children.Add(lower);

        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.AddSheet(Parse("#Title { background-color: #3060c0; }"));
        context.Update(0f);

        Assert.Equal(Blue, upper.Style.BackgroundColor);
        Assert.Equal(Rgba8.Transparent, lower.Style.BackgroundColor);
    }

    [Theory]
    [InlineData("Label", 1)]
    [InlineData(".$a", 256)]
    [InlineData(":hover", 256)]
    [InlineData("Label.$a", 257)]
    [InlineData(".$a:hover", 512)]
    [InlineData("#title", 65536)]
    [InlineData("Label .$b", 257)]
    [InlineData(".$a > .$b", 512)]
    [InlineData(".$a:hover .$b:focus", 1024)]
    [InlineData(":hover:hover", 256)]
    [InlineData("Label.$a#title:hover:active:focus:disabled:checked", 65536 + (6 << 8) + 1)]
    [InlineData("#a > #b", 2 << 16)]
    public void TheSpecificityComesFromTheSelector(string form, int specificity)
    {
        const string Prefix = nameof(TheSpecificityComesFromTheSelector) + "-";
        string text = form.Replace("$", Prefix, StringComparison.Ordinal) + " { color: #fff; }";

        StyleRule rule = Assert.Single(Parse(text).Rules.ToArray());

        Assert.Equal(specificity, rule.Specificity);
        Assert.Equal(specificity, rule.Selector.Specificity);
    }

    /// <summary>Gives the selector of the factories for one row of <see cref="Forms"/>.</summary>
    private static Selector Expected(string form, string prefix)
    {
        ElementTypeId label = ElementType.Intern("Label");
        ClassId a = ClassName.Intern(prefix + "a");
        ClassId b = ClassName.Intern(prefix + "b");
        CompoundSelector classA = new() { Class = a };
        CompoundSelector classB = new() { Class = b };

        return form switch
        {
            "Label" => Selector.ForType(label),
            ".$a" => Selector.ForClass(a),
            "#title" => Selector.ForName("title"),
            ":hover" => Selector.For(new CompoundSelector { States = PseudoStates.Hover }),
            ":active" => Selector.For(new CompoundSelector { States = PseudoStates.Active }),
            ":focus" => Selector.For(new CompoundSelector { States = PseudoStates.Focus }),
            ":disabled" => Selector.For(new CompoundSelector { States = PseudoStates.Disabled }),
            ":checked" => Selector.For(new CompoundSelector { States = PseudoStates.Checked }),
            "Label.$a#title:hover:checked" => Selector.For(new CompoundSelector
            {
                Type = label,
                Class = a,
                Name = "title",
                States = PseudoStates.Hover | PseudoStates.Checked,
            }),
            ".$a > .$b" or ".$a>.$b" or ".$a\n>\t.$b" => Selector.Child(classA, classB),
            ".$a .$b" or ".$a  /* c */ .$b" => Selector.Descendant(classA, classB),
            "Label:hover > #title" => Selector.Child(
                new CompoundSelector { Type = label, States = PseudoStates.Hover },
                new CompoundSelector { Name = "title" }),
            ":focus .$b" => Selector.Descendant(new CompoundSelector { States = PseudoStates.Focus }, classB),
            ":hover:hover" => Selector.For(new CompoundSelector { States = PseudoStates.Hover }),
            "#title.$a:checked" => Selector.For(new CompoundSelector { Class = a, Name = "title", States = PseudoStates.Checked }),
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, "The row has no expected selector."),
        };
    }

    /// <summary>Parses one selector with one declaration, and gives the selector of the one rule.</summary>
    private static Selector ParseOne(string selector)
    {
        StyleRule rule = Assert.Single(Parse(selector + " { color: #fff; }").Rules.ToArray());
        return rule.Selector;
    }

    private static StyleSheet Parse(string text)
    {
        return new StyleSheetParser().Parse(Encoding.UTF8.GetBytes(text), "selector.css").GetSheetOrThrow();
    }

    private static string Describe(Element row, Element cell)
    {
        return row.Name + " " + row.Style.BackgroundColor + " " + cell.Name + " " + cell.Style.BackgroundColor;
    }

    /// <summary>Parses a sheet, styles one <see cref="Label"/> under a root, and runs <c>Update</c>.</summary>
    private static Label StyledLabel(string text)
    {
        Element root = new();
        Label label = new(default(Label.CreateInfo));
        root.Children.Add(label);

        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(Parse(text));
        context.Update(0f);
        return label;
    }

    /// <summary>Makes a context that can lay out a <see cref="Label"/>: a <see cref="Label"/> needs a measurer.</summary>
    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new FixedMeasurer() })
        {
            ViewportSize = new float2(400f, 300f),
        };
    }

    /// <summary>A renderer that draws nothing and measures each text as 10 by 10.</summary>
    private sealed class FixedMeasurer : IUiRenderer
    {
        public void Begin(in float2 displaySize, float scale)
        {
        }

        public void PushClip(in float4 rect)
        {
        }

        public void PopClip()
        {
        }

        public void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor)
        {
        }

        public void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius)
        {
        }

        public void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
        }

        public float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
            return new float2(10f, 10f);
        }

        public void End()
        {
        }
    }

    /// <summary>An element type that no other test knows. Its type name is prefixed.</summary>
    private sealed class Widget : Element
    {
        public const string TypeName = nameof(AnUnknownTypeMatchesNothingAndIsNoError) + "-Widget";

        private static readonly ElementTypeId s_typeId = ElementType.Intern(TypeName);

        public override ElementTypeId TypeId => s_typeId;
    }
}
