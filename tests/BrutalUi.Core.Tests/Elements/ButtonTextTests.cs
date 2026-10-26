using System;
using System.Globalization;
using System.Text;
using Brutal.Localization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the text of <see cref="Button"/>: the draw, the size, the <c>SetText</c> overloads, the dirty bits of
/// each change, the <see cref="LString"/> members, <see cref="UiContext.RefreshLocalizedText"/>, and a click on the
/// text.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RecordingRenderer"/> measures a text as (bytes * size * 0.5, size). Thus <c>Play</c> at size 16 is 32 by
/// 16 pixels.
/// </para>
/// <para>
/// Each test builds its own tree. No test interns a class or uses the static <c>LStrings</c> class, so parallel tests
/// share no state.
/// </para>
/// </remarks>
public sealed class ButtonTextTests
{
    private const DirtyFlags SelfBits = DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint;
    private const DirtyFlags LayoutAndPaint = DirtyFlags.Layout | DirtyFlags.Paint;

    private static readonly Rgba8 White = new(255, 255, 255);

    /// <summary>
    /// The demo of phase 4. A button with <c>Text = "Play"</c> and a padding of 8 sends one text call with the bytes
    /// of <c>Play</c> at the top-left corner of its content box. The button size is the text size plus the padding.
    /// </summary>
    [Fact]
    public void AButtonDrawsItsText()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Button button = AddButton(context, new Button.CreateInfo { Text = "Play" });
        button.Inline.Padding = LengthEdges.All(8);

        context.Update(0f);
        context.Draw(renderer);

        string rect = RectLine(button);
        string text = TextLine(renderer);
        WriteLine("Demo: " + rect);
        WriteLine("Demo: " + text);

        Assert.Equal("rect 0,0,48,32", rect);
        Assert.Equal("Text 8,8 font 0 size 16 #FFFFFFFF wrap 32 \"Play\"", text);
        Assert.Equal("Play", Encoding.UTF8.GetString(button.Text));
    }

    /// <summary>
    /// A button with an auto size takes the size of its text plus the padding. A narrow parent wraps the text at the
    /// width of the content box. A button with a child does not measure its text.
    /// </summary>
    [Fact]
    public void TheButtonTakesTheSizeOfItsText()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Button plain = AddButton(context, new Button.CreateInfo { Text = "Hello" });
        context.Update(0f);
        Assert.Equal("rect 0,0,40,16", RectLine(plain));

        // A root of 60 pixels leaves 44 pixels for the text: "Hello world" (88 pixels) wraps to two lines of 40.
        RecordingRenderer narrowRenderer = new();
        UiContext narrow = CreateContext(narrowRenderer);
        Button wrapped = AddButton(narrow, new Button.CreateInfo { Text = "Hello world" });
        wrapped.Inline.Padding = LengthEdges.All(8);
        narrow.Roots[0].Inline.Width = 60;
        narrow.Update(0f);
        narrow.Draw(narrowRenderer);
        Assert.Equal("rect 0,0,56,48", RectLine(wrapped));
        Assert.Equal("Text 8,8 font 0 size 16 #FFFFFFFF wrap 40 \"Hello world\"", TextLine(narrowRenderer));

        // Gap 6 of the plan: the engine does not call the measure callback of an element with children.
        RecordingRenderer parentRenderer = new();
        UiContext withChild = CreateContext(parentRenderer);
        Button parent = AddButton(withChild, new Button.CreateInfo { Text = "Hello" });
        Element child = new() { IsPickable = false };
        child.Inline.Width = 20;
        child.Inline.Height = 20;
        parent.Children.Add(child);
        withChild.Update(0f);
        Assert.Equal("rect 0,0,20,20", RectLine(parent));
        Assert.Equal(0, parentRenderer.MeasureCount);

        // A new font size measures the button again, as it does a label. The width doubles from 32 to 64.
        RecordingRenderer sizeRenderer = new();
        UiContext resized = CreateContext(sizeRenderer);
        Button play = AddButton(resized, new Button.CreateInfo { Text = "Play" });
        resized.Update(0f);
        Assert.Equal("rect 0,0,32,16", RectLine(play));
        int measures = sizeRenderer.MeasureCount;

        play.Inline.FontSize = 32;
        resized.Update(0f);
        Assert.True(sizeRenderer.MeasureCount > measures, "The new size measures the button again.");
        Assert.Equal("rect 0,0,64,32", RectLine(play));
    }

    /// <summary>A new span marks the layout and the paint. The next update and draw show the new text.</summary>
    [Fact]
    public void SetTextFromASpanMarksLayoutAndPaint()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Button button = AddButton(context, new Button.CreateInfo { Text = "Hello" });
        context.Update(0f);

        button.SetText("Hello, world"u8);

        Assert.Null(button.LocalizedText);
        Assert.Equal(LayoutAndPaint, button.Dirty & SelfBits);
        context.Update(0f);
        context.Draw(renderer);
        Assert.Equal("rect 0,0,96,16", RectLine(button));
        Assert.Equal("Text 0,0 font 0 size 16 #FFFFFFFF wrap 96 \"Hello, world\"", TextLine(renderer));

        // A part of the text of the button is a valid source.
        button.SetText(button.Text[7..]);
        Assert.Equal("world", Encoding.UTF8.GetString(button.Text));
        Assert.Equal(LayoutAndPaint, button.Dirty & SelfBits);
    }

    /// <summary>
    /// The same text marks nothing: the same span, the text of the button, a new string with the same content, and a
    /// string after a text from a span.
    /// </summary>
    [Fact]
    public void SetTextFromTheSameTextMarksNothing()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Button button = AddButton(context, new Button.CreateInfo { Text = "Hello" });
        context.Update(0f);

        button.SetText("Hello"u8);
        button.SetText(button.Text);
        Assert.Equal(DirtyFlags.None, button.Dirty);

        // A string variable, not a literal: the BRUTAL analyzer asks for a UTF-8 literal at a literal call.
        button.SetText(new string("Hello".AsSpan()));
        Assert.Equal(DirtyFlags.None, button.Dirty);

        button.SetText("Héllo"u8);
        Assert.Equal(LayoutAndPaint, button.Dirty & SelfBits);
        context.Update(0f);
        button.SetText(new string("Héllo".AsSpan()));
        Assert.Equal(DirtyFlags.None, button.Dirty);
        Assert.Equal("Héllo", Encoding.UTF8.GetString(button.Text));

        // A null string gives an empty text one time. The empty forms after it mark nothing.
        button.SetText((string?)null);
        Assert.True(button.Text.IsEmpty);
        Assert.Equal(LayoutAndPaint, button.Dirty & SelfBits);
        context.Update(0f);
        button.SetText((string?)null);
        button.SetText(string.Empty);
        button.SetText(ReadOnlySpan<byte>.Empty);
        Assert.Equal(DirtyFlags.None, button.Dirty);
    }

    /// <summary>
    /// <see cref="Button.SetText(LString?)"/> shows the localized text and marks the layout and the paint. The same
    /// text marks nothing. A text from a string clears the <see cref="LString"/>. A null <see cref="LString"/> clears
    /// the text.
    /// </summary>
    [Fact]
    public void SetTextFromAnLStringShowsTheLocalizedText()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Button button = AddButton(context, new Button.CreateInfo { Text = "Hello" });
        context.Update(0f);

        LString same = new("button-text-tests.same", "Hello");
        button.SetText(same);
        Assert.Same(same, button.LocalizedText);
        Assert.Equal(DirtyFlags.None, button.Dirty);

        LString other = new("button-text-tests.other", "Goodbye");
        other.Localize("Au revoir");
        button.SetText(other);
        Assert.Same(other, button.LocalizedText);
        Assert.Equal(LayoutAndPaint, button.Dirty & SelfBits);
        context.Update(0f);
        context.Draw(renderer);
        Assert.Equal("rect 0,0,72,16", RectLine(button));
        Assert.Equal("Text 0,0 font 0 size 16 #FFFFFFFF wrap 72 \"Au revoir\"", TextLine(renderer));

        string text = "Au revoir";
        button.SetText(text);
        Assert.Null(button.LocalizedText);
        Assert.Equal(DirtyFlags.None, button.Dirty);

        button.SetText(other);
        button.SetText((LString?)null);
        Assert.Null(button.LocalizedText);
        Assert.True(button.Text.IsEmpty);
        Assert.Equal(LayoutAndPaint, button.Dirty & SelfBits);

        LString created = new("button-text-tests.created", "Hello");
        created.Localize("Hallo Welt");
        Button fromInfo = new(new Button.CreateInfo { LocalizedText = created });
        Assert.Same(created, fromInfo.LocalizedText);
        Assert.Equal("Hallo Welt", Encoding.UTF8.GetString(fromInfo.Text));
    }

    /// <summary>
    /// <see cref="UiContext.RefreshLocalizedText"/> reaches a button in the tree and a label inside a button. It marks
    /// each changed text and no other element. The next update measures the new text.
    /// </summary>
    [Fact]
    public void RefreshLocalizedTextReachesAButton()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Element middle = new();
        middle.Inline.AlignItems = Align.FlexStart;
        root.Children.Add(middle);

        LString play = new("button-text-tests.refresh", "Play");
        Button button = new(new Button.CreateInfo { LocalizedText = play });
        SetTextValues(button);
        middle.Children.Add(button);

        Button plain = new(new Button.CreateInfo { Text = "Plain" });
        SetTextValues(plain);
        root.Children.Add(plain);

        LString inner = new("button-text-tests.inner", "Go");
        Button outer = new(new Button.CreateInfo());
        Label label = new(new Label.CreateInfo { LocalizedText = inner }) { IsPickable = false };
        SetTextValues(label);
        outer.Children.Add(label);
        root.Children.Add(outer);
        context.Update(0f);
        Assert.Equal("rect 0,0,32,16", RectLine(button));

        context.RefreshLocalizedText();
        Assert.Equal(DirtyFlags.None, button.Dirty);

        play.Localize("Jouer maintenant");
        inner.Localize("Allez");
        context.RefreshLocalizedText();

        Assert.Equal(LayoutAndPaint, button.Dirty & SelfBits);
        Assert.Equal(LayoutAndPaint, label.Dirty & SelfBits);
        Assert.Equal(DirtyFlags.None, plain.Dirty);
        Assert.Equal(DirtyFlags.None, middle.Dirty & SelfBits);
        Assert.Equal(DirtyFlags.None, outer.Dirty & SelfBits);
        Assert.Equal(DirtyFlags.None, root.Dirty & SelfBits);
        Assert.Equal("Jouer maintenant", Encoding.UTF8.GetString(button.Text));

        context.Update(0f);
        Assert.Equal("rect 0,0,128,16", RectLine(button));
        Assert.Equal("Allez", Encoding.UTF8.GetString(label.Text));
    }

    /// <summary>The constructor throws for a create-info with a text and a localizable text.</summary>
    [Fact]
    public void ThrowsForTwoTexts()
    {
        LString hello = new("button-text-tests.two-texts", "Hello");

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new Button(new Button.CreateInfo { Text = "Hello", LocalizedText = hello }));

        Assert.Equal("info", error.ParamName);
        Assert.StartsWith(
            "Set Text or LocalizedText of the Button.CreateInfo, not both.",
            error.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A button that needs a measure throws from <see cref="UiContext.Update(float)"/> in a context with no measurer.
    /// A button with a fixed width and a fixed height needs no measure.
    /// </summary>
    [Fact]
    public void AButtonNeedsAMeasurer()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };
        AddButton(context, new Button.CreateInfo { Text = "Play" });

        Assert.Null(context.Measurer);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => context.Update(0f));
        Assert.Equal(
            "A Button needs a UiContext with a measurer. Create the context with UiContext.CreateInfo.",
            exception.Message);

        UiContext empty = new() { ViewportSize = new float2(400, 300) };
        AddButton(empty, new Button.CreateInfo());
        Assert.Throws<InvalidOperationException>(() => empty.Update(0f));

        UiContext fixedSize = new() { ViewportSize = new float2(400, 300) };
        Button sized = AddButton(fixedSize, new Button.CreateInfo { Text = "Play" });
        sized.Inline.Width = 120;
        sized.Inline.Height = 40;
        fixedSize.Update(0f);
        Assert.Equal("rect 0,0,120,40", RectLine(sized));
    }

    /// <summary>
    /// A press and a release over the text of a button click the button. The text is not an element, so the hit test
    /// gives the button.
    /// </summary>
    [Fact]
    public void AClickOnTheTextClicksTheButton()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Button button = AddButton(context, new Button.CreateInfo { Text = "Play" });
        button.Inline.Padding = LengthEdges.All(8);
        context.Update(0f);

        int clicks = 0;
        Button? sender = null;
        button.Clicked = b =>
        {
            clicks++;
            sender = b;
        };

        // The text box is (8, 8, 32, 16). The point lies inside it.
        float2 overText = new(20f, 14f);
        context.PushEvent(PointerTree.Down(overText, PointerButton.Left));
        context.PushEvent(PointerTree.Up(overText, PointerButton.Left));
        context.Update(0f);

        Assert.Equal(1, clicks);
        Assert.Same(button, sender);
        Assert.Same(button, context.FocusedElement);
        Assert.Equal("Play", Encoding.UTF8.GetString(button.Text));
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
        root.Inline.AlignItems = Align.FlexStart;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Adds a root and a button to a context. The inline set gives the button white text of size 16.</summary>
    /// <param name="context">The context.</param>
    /// <param name="info">The create-info of the button.</param>
    /// <returns>The button.</returns>
    private static Button AddButton(UiContext context, in Button.CreateInfo info)
    {
        Element root = AddRoot(context);
        Button button = new(in info);
        SetTextValues(button);
        root.Children.Add(button);
        return button;
    }

    /// <summary>Gives an element white text of size 16 through the inline set.</summary>
    /// <param name="element">The element.</param>
    private static void SetTextValues(Element element)
    {
        element.Inline.Color = White;
        element.Inline.FontSize = 16;
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
