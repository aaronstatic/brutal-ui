using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Brutal.Localization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the text changes of <see cref="Label"/>: the setters, the <c>SetText</c> overloads, the dirty bits of each
/// change, the <see cref="LString"/> members, and <see cref="UiContext.RefreshLocalizedText"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RecordingRenderer"/> measures a text as (bytes * size * 0.5, size). Thus <c>Hello</c> at size 16 is
/// 40 by 16 pixels.
/// </para>
/// <para>
/// No test uses the static <c>LStrings</c> class. Each test makes its own <see cref="LString"/> or its own
/// <see cref="LocalizedStrings"/>, so parallel tests share no state.
/// </para>
/// </remarks>
public sealed class LabelTextTests
{
    private const DirtyFlags SelfBits = DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint;
    private const DirtyFlags LayoutAndPaint = DirtyFlags.Layout | DirtyFlags.Paint;

    private static readonly Rgba8 White = new(255, 255, 255);

    /// <summary>
    /// The demo of phase 4: a label with an <see cref="LString"/> shows the new locale after
    /// <see cref="LString.Localize(string)"/> and <see cref="UiContext.RefreshLocalizedText"/>.
    /// </summary>
    [Fact]
    public void ShowsTheNewLocaleAfterARefresh()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        LString hello = new("label-text-tests.demo", "Hello");
        Label label = AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });

        context.Update(0f);
        string before = RectLine(label);
        WriteLine(before);

        hello.Localize("Bonjour le monde");
        context.RefreshLocalizedText();
        string dirty = string.Create(CultureInfo.InvariantCulture, $"dirty {label.Dirty & SelfBits}");
        WriteLine(dirty);

        context.Update(0f);
        context.Draw(renderer);
        string after = RectLine(label);
        string text = TextLine(renderer);
        WriteLine(after);
        WriteLine(text);

        Assert.Equal("rect 0,0,40,16", before);
        Assert.Equal("dirty Layout, Paint", dirty);
        Assert.Equal("rect 0,0,128,16", after);
        Assert.Equal("Text 0,0 font 0 size 16 #FFFFFFFF wrap 128 \"Bonjour le monde\"", text);
    }

    /// <summary>A new span marks the layout and the paint. The next update and draw show the new text.</summary>
    [Fact]
    public void SetTextFromASpanMarksLayoutAndPaint()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.SetText("Hello, world"u8);

        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        context.Update(0f);
        context.Draw(renderer);
        Assert.Equal("rect 0,0,96,16", RectLine(label));
        Assert.Equal("Text 0,0 font 0 size 16 #FFFFFFFF wrap 96 \"Hello, world\"", TextLine(renderer));
    }

    /// <summary>The same bytes mark nothing, also from a part of the text of the label.</summary>
    [Fact]
    public void SetTextFromTheSameSpanMarksNothing()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.SetText("Hello"u8);
        label.SetText(label.Text);

        Assert.Equal(DirtyFlags.None, label.Dirty);
        Assert.Equal("Hello", Encoding.UTF8.GetString(label.Text));
    }

    /// <summary>A part of the text of the label is a valid source for a new text.</summary>
    [Fact]
    public void SetTextFromAPartOfTheTextCopiesTheBytes()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.SetText(label.Text[1..4]);

        Assert.Equal("ell", Encoding.UTF8.GetString(label.Text));
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
    }

    /// <summary>A new string marks the layout and the paint. The next update and draw show the new text.</summary>
    [Fact]
    public void SetTextFromAStringMarksLayoutAndPaint()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        // A string variable, not a literal: the BRUTAL analyzer asks for a UTF-8 literal at a literal call.
        string text = "Hi";
        label.SetText(text);

        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        context.Update(0f);
        context.Draw(renderer);
        Assert.Equal("rect 0,0,16,16", RectLine(label));
        Assert.Equal("Text 0,0 font 0 size 16 #FFFFFFFF wrap 16 \"Hi\"", TextLine(renderer));
    }

    /// <summary>A new string with the same content marks nothing, also after a text from a span.</summary>
    [Fact]
    public void SetTextFromTheSameStringMarksNothing()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.SetText(new string("Hello".AsSpan()));
        Assert.Equal(DirtyFlags.None, label.Dirty);

        label.SetText("Héllo"u8);
        context.Update(0f);
        label.SetText(new string("Héllo".AsSpan()));
        Assert.Equal(DirtyFlags.None, label.Dirty);
    }

    /// <summary>A null string gives an empty text. A null string on an empty label marks nothing.</summary>
    [Fact]
    public void SetTextFromANullStringGivesAnEmptyText()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.SetText((string?)null);
        Assert.True(label.Text.IsEmpty);
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);

        context.Update(0f);
        label.SetText((string?)null);
        label.SetText(string.Empty);
        label.SetText(ReadOnlySpan<byte>.Empty);
        Assert.Equal(DirtyFlags.None, label.Dirty);

        context.Draw(renderer);
        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());
    }

    /// <summary>A new font marks the layout and the paint. The draw sends the new font.</summary>
    [Fact]
    public void SettingTheFontMarksLayoutAndPaint()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.Font = new FontHandle(3);

        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        context.Update(0f);
        context.Draw(renderer);
        Assert.Equal(3, label.Font.Id);
        Assert.Equal("rect 0,0,40,16", RectLine(label));
        Assert.Equal("Text 0,0 font 3 size 16 #FFFFFFFF wrap 40 \"Hello\"", TextLine(renderer));
    }

    /// <summary>The same font id marks nothing.</summary>
    [Fact]
    public void SettingTheSameFontMarksNothing()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.Font = new FontHandle(0);

        Assert.Equal(DirtyFlags.None, label.Dirty);
    }

    /// <summary>A new size marks the layout and the paint. The next update gives the new size.</summary>
    [Fact]
    public void SettingTheSizeMarksLayoutAndPaint()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.Size = 32;

        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        context.Update(0f);
        context.Draw(renderer);
        Assert.Equal("rect 0,0,80,32", RectLine(label));
        Assert.Equal("Text 0,0 font 0 size 32 #FFFFFFFF wrap 80 \"Hello\"", TextLine(renderer));
    }

    /// <summary>The same size marks nothing. <c>NaN</c> to <c>NaN</c> also marks nothing.</summary>
    [Fact]
    public void SettingTheSameSizeMarksNothing()
    {
        Label label = new(new Label.CreateInfo { Color = White, Size = 16, Text = "Hello" });
        Settle(label);

        label.Size = 16;
        Assert.Equal(DirtyFlags.None, label.Dirty);

        label.Size = float.NaN;
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        Settle(label);
        label.Size = float.NaN;
        Assert.Equal(DirtyFlags.None, label.Dirty);
    }

    /// <summary>A new color marks the paint only. The draw sends the new color.</summary>
    [Fact]
    public void SettingTheColorMarksPaint()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.Color = new Rgba8(255, 0, 0);

        Assert.Equal(DirtyFlags.Paint, label.Dirty & SelfBits);
        context.Update(0f);
        context.Draw(renderer);
        Assert.Equal("rect 0,0,40,16", RectLine(label));
        Assert.Equal("Text 0,0 font 0 size 16 #FF0000FF wrap 40 \"Hello\"", TextLine(renderer));
    }

    /// <summary>The same color marks nothing.</summary>
    [Fact]
    public void SettingTheSameColorMarksNothing()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        label.Color = new Rgba8(255, 255, 255, 255);

        Assert.Equal(DirtyFlags.None, label.Dirty);
    }

    /// <summary>A label with an <see cref="LString"/> in the create-info shows its localized text.</summary>
    [Fact]
    public void TheConstructorReadsTheLocalizedText()
    {
        LString hello = new("label-text-tests.constructor", "Hello");
        hello.Localize("Hallo Welt");

        Label label = new(new Label.CreateInfo { Color = White, LocalizedText = hello });

        Assert.Same(hello, label.LocalizedText);
        Assert.Equal("Hallo Welt", Encoding.UTF8.GetString(label.Text));
    }

    /// <summary>A text from a span clears the <see cref="LString"/>. A later refresh does not change the text.</summary>
    [Fact]
    public void SetTextFromASpanClearsTheLocalizedText()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        LString hello = new("label-text-tests.span-clears", "Hello");
        Label label = AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });

        label.SetText("Hi"u8);
        context.Update(0f);
        hello.Localize("Bonjour");
        context.RefreshLocalizedText();

        Assert.Null(label.LocalizedText);
        Assert.Equal(DirtyFlags.None, label.Dirty);
        Assert.Equal("Hi", Encoding.UTF8.GetString(label.Text));
    }

    /// <summary>A text from a string clears the <see cref="LString"/>, also when the text stays the same.</summary>
    [Fact]
    public void SetTextFromAStringClearsTheLocalizedText()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        LString hello = new("label-text-tests.string-clears", "Hello");
        Label label = AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });
        context.Update(0f);

        string text = "Hello";
        label.SetText(text);
        Assert.Null(label.LocalizedText);
        Assert.Equal(DirtyFlags.None, label.Dirty);

        hello.Localize("Bonjour");
        context.RefreshLocalizedText();
        Assert.Equal(DirtyFlags.None, label.Dirty);
        Assert.Equal("Hello", Encoding.UTF8.GetString(label.Text));
    }

    /// <summary>
    /// <see cref="Label.SetText(LString?)"/> shows the localized text and marks the layout and the paint. The same
    /// text marks nothing. A null <see cref="LString"/> clears the text.
    /// </summary>
    [Fact]
    public void SetTextFromAnLStringShowsTheLocalizedText()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Label label = AddLabel(context, "Hello");
        context.Update(0f);

        LString same = new("label-text-tests.same", "Hello");
        label.SetText(same);
        Assert.Same(same, label.LocalizedText);
        Assert.Equal(DirtyFlags.None, label.Dirty);

        LString other = new("label-text-tests.other", "Goodbye");
        other.Localize("Au revoir");
        label.SetText(other);
        Assert.Same(other, label.LocalizedText);
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        context.Update(0f);
        Assert.Equal("rect 0,0,72,16", RectLine(label));

        label.SetText((LString?)null);
        Assert.Null(label.LocalizedText);
        Assert.True(label.Text.IsEmpty);
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
    }

    /// <summary>
    /// A refresh marks nothing when the localized text has a new reference with the same content, and when nothing
    /// changed.
    /// </summary>
    [Fact]
    public void RefreshMarksNothingWhenTheTextIsTheSame()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        LString hello = new("label-text-tests.same-content", "Hello");
        Label label = AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });
        context.Update(0f);

        context.RefreshLocalizedText();
        Assert.Equal(DirtyFlags.None, label.Dirty);

        string copy = new("Hello".AsSpan());
        hello.Localize(copy);
        Assert.Same(copy, hello.Localized);
        context.RefreshLocalizedText();
        Assert.Equal(DirtyFlags.None, label.Dirty);
        Assert.Equal(DirtyFlags.None, context.Roots[0].Dirty);
    }

    /// <summary>
    /// A refresh reaches a label at the end of a deep chain. It marks the label and sends the descendant bits up. It
    /// marks no other element.
    /// </summary>
    [Fact]
    public void RefreshReachesADeepLabel()
    {
        const int Depth = 50;
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Element[] chain = new Element[Depth];
        Element parent = root;
        for (int i = 0; i < Depth; i++)
        {
            Element child = new();
            child.Style.AlignItems = Align.FlexStart;
            parent.Children.Add(child);
            chain[i] = child;
            parent = child;
        }

        LString hello = new("label-text-tests.deep", "Hello");
        Label label = new(new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });
        parent.Children.Add(label);
        Label plain = new(new Label.CreateInfo { Color = White, Size = 16, Text = "Plain" });
        root.Children.Add(plain);
        context.Update(0f);

        hello.Localize("Bonjour le monde");
        context.RefreshLocalizedText();

        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        Assert.Equal(DirtyFlags.None, plain.Dirty);
        Assert.Equal(DirtyFlags.None, root.Dirty & SelfBits);
        Assert.Equal(DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint, root.Dirty);
        for (int i = 0; i < Depth; i++)
        {
            Assert.Equal(DirtyFlags.None, chain[i].Dirty & SelfBits);
        }

        context.Update(0f);
        Assert.Equal("rect 0,0,128,16", RectLine(label));
    }

    /// <summary>A refresh needs no measurer. A context from <see cref="UiContext()"/> refreshes its labels.</summary>
    [Fact]
    public void RefreshWorksWithNoMeasurer()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };
        LString hello = new("label-text-tests.no-measurer", "Hello");
        Label label = AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });
        Settle(label);

        hello.Localize("Bonjour");
        context.RefreshLocalizedText();

        Assert.Equal("Bonjour", Encoding.UTF8.GetString(label.Text));
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
    }

    /// <summary>A refresh of a context with no roots does nothing.</summary>
    [Fact]
    public void RefreshOfAnEmptyContextDoesNothing()
    {
        UiContext context = new();

        context.RefreshLocalizedText();

        Assert.Equal(0, context.Roots.Length);
    }

    /// <summary>The constructor throws for a create-info with a text and a localizable text.</summary>
    [Fact]
    public void ThrowsForTwoTexts()
    {
        LString hello = new("label-text-tests.two-texts", "Hello");

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new Label(new Label.CreateInfo { Color = White, Text = "Hello", LocalizedText = hello }));

        Assert.Equal("info", error.ParamName);
    }

    /// <summary>After <see cref="LString.RemoveLocalization"/> and a refresh, the label shows the template.</summary>
    [Fact]
    public void RemoveLocalizationShowsTheTemplate()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        LString hello = new("label-text-tests.remove", "Hello");
        Label label = AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });
        hello.Localize("Bonjour le monde");
        context.RefreshLocalizedText();
        context.Update(0f);
        Assert.Equal("rect 0,0,128,16", RectLine(label));

        hello.RemoveLocalization();
        context.RefreshLocalizedText();

        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        Assert.Equal("Hello", Encoding.UTF8.GetString(label.Text));
        context.Update(0f);
        Assert.Equal("rect 0,0,40,16", RectLine(label));
    }

    /// <summary>
    /// A provider on a private <see cref="LocalizedStrings"/> localizes its <see cref="LString"/> instances. A refresh
    /// then shows the new text.
    /// </summary>
    [Fact]
    public void RefreshShowsTheTextOfANewProvider()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        LocalizedStrings strings = new(null);
        LString hello = strings.Create("label-text-tests.provider", "Hello");
        Label label = AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, LocalizedText = hello });
        context.Update(0f);

        strings.SetLocalizedStringProvider(static (LString _, [NotNullWhen(true)] out string? localized) =>
        {
            localized = "Hallo";
            return true;
        });
        context.RefreshLocalizedText();

        Assert.Equal("Hallo", Encoding.UTF8.GetString(label.Text));
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
    }

    /// <summary>A text of 100 bytes, then of 10, then of 100: the text reads back each time.</summary>
    [Fact]
    public void GrowsTheBufferForALongText()
    {
        string longText = new('a', 100);
        string shortText = new('b', 10);
        string otherLong = new('c', 100);
        Label label = new(new Label.CreateInfo { Color = White, Size = 16 });

        label.SetText(longText);
        Assert.Equal(longText, Encoding.UTF8.GetString(label.Text));

        label.SetText(shortText);
        Assert.Equal(shortText, Encoding.UTF8.GetString(label.Text));

        label.SetText(Encoding.UTF8.GetBytes(otherLong));
        Assert.Equal(otherLong, Encoding.UTF8.GetString(label.Text));

        string longer = new('d', 300);
        label.SetText(Encoding.UTF8.GetBytes(longer));
        Assert.Equal(longer, Encoding.UTF8.GetString(label.Text));
    }

    /// <summary>The byte compare gives the result of a compare with <see cref="Encoding.UTF8"/>.</summary>
    /// <param name="utf8Source">The text that gives the UTF-8 bytes.</param>
    /// <param name="text">The UTF-16 text to compare.</param>
    [Theory]
    [InlineData("", "")]
    [InlineData("Hello", "Hello")]
    [InlineData("Hello", "Hellp")]
    [InlineData("Hello", "Hell")]
    [InlineData("Hell", "Hello")]
    [InlineData("Héllo wörld", "Héllo wörld")]
    [InlineData("😀 smile", "😀 smile")]
    [InlineData("\uD800 lone", "\uDBFF lone")]
    public void Utf8EqualsMatchesTheEncoder(string utf8Source, string text)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(utf8Source);
        bool expected = utf8.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(text));

        Assert.Equal(expected, Label.Utf8Equals(utf8, text));
    }

    /// <summary>The byte compare works across the chunks of its stack buffer, with multi-byte characters.</summary>
    [Fact]
    public void Utf8EqualsComparesALongText()
    {
        StringBuilder builder = new();
        for (int i = 0; i < 200; i++)
        {
            builder.Append("é😀a");
        }

        string text = builder.ToString();
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        Assert.True(Label.Utf8Equals(utf8, text));

        byte[] changed = (byte[])utf8.Clone();
        changed[^1] = (byte)'b';
        Assert.False(Label.Utf8Equals(changed, text));
        Assert.False(Label.Utf8Equals(utf8.AsSpan(0, utf8.Length - 1), text));
    }

    /// <summary>Creates a context with a measurer and a viewport of 400 by 300.</summary>
    /// <param name="measurer">The measurer.</param>
    /// <returns>The context.</returns>
    private static UiContext CreateContext(RecordingRenderer measurer)
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = measurer }) { ViewportSize = new float2(400, 300) };
    }

    /// <summary>
    /// Adds a root with <see cref="Align.FlexStart"/> to a context. A child then takes the width of its content.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The root.</returns>
    private static Element AddRoot(UiContext context)
    {
        Element root = new();
        root.Style.AlignItems = Align.FlexStart;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Adds a root and a white label of size 16 to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="text">The text of the label.</param>
    /// <returns>The label.</returns>
    private static Label AddLabel(UiContext context, string text)
    {
        return AddLabel(context, new Label.CreateInfo { Color = White, Size = 16, Text = text });
    }

    /// <summary>Adds a root and a label to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="info">The create-info of the label.</param>
    /// <returns>The label.</returns>
    private static Label AddLabel(UiContext context, in Label.CreateInfo info)
    {
        Element root = AddRoot(context);
        Label label = new(in info);
        root.Children.Add(label);
        return label;
    }

    /// <summary>Clears each dirty bit of an element with no context.</summary>
    /// <param name="element">The element.</param>
    private static void Settle(Element element)
    {
        element.ClearDirty(DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint
            | DirtyFlags.DescendantStyle | DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint);
    }

    /// <summary>Gives the rect line of an element: <c>rect x,y,width,height</c>.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The line.</returns>
    private static string RectLine(Element element)
    {
        float4 rect = element.Layout.Rect;
        return string.Create(CultureInfo.InvariantCulture, $"rect {rect.X},{rect.Y},{rect.Z},{rect.W}");
    }

    /// <summary>Gives the one text line of the dump of a renderer.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The text line.</returns>
    private static string TextLine(RecordingRenderer renderer)
    {
        string[] lines = renderer.Describe().Split('\n');
        return Assert.Single(lines, static line => line.StartsWith("Text ", StringComparison.Ordinal));
    }

    /// <summary>Writes a line to the live output of the test.</summary>
    /// <param name="line">The line.</param>
    private static void WriteLine(string line)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(line);
    }
}
