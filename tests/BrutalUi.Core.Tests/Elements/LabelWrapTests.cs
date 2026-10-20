using System;
using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the wrap rule of <see cref="Label"/>: the wrap rule of <see cref="RecordingRenderer"/>,
/// <see cref="Label.SnapWrapWidth(float)"/>, the measure rule, and the draw rule.
/// </summary>
/// <remarks>
/// <see cref="RecordingRenderer"/> gives each byte a width of <c>size * 0.5</c>. At size 16, a byte is 8 pixels wide.
/// Thus <c>Hello brutal world</c> (18 bytes) is 144 by 16 pixels with no wrap.
/// </remarks>
public sealed class LabelWrapTests
{
    private const string Sentence = "Hello brutal world";
    private const float Size = 16f;
    private static readonly Rgba8 White = new(255, 255, 255);

    /// <summary>The wrap rule of the recording renderer gives the lines of a greedy break at the space byte.</summary>
    [Fact]
    public void TheRecordingRuleWrapsAtTheSpaces()
    {
        RecordingRenderer renderer = new();
        ReadOnlySpan<byte> sentence = "Hello brutal world"u8;

        Assert.Equal(0f, renderer.LastMeasureWrapWidth);
        Assert.Equal(new float2(144, 16), renderer.MeasureText(default, Size, sentence, 0f));
        Assert.Equal(new float2(144, 16), renderer.MeasureText(default, Size, sentence, float.NaN));
        Assert.Equal(0f, renderer.LastMeasureWrapWidth);

        Assert.Equal(new float2(96, 32), renderer.MeasureText(default, Size, sentence, 100f));
        Assert.Equal(100f, renderer.LastMeasureWrapWidth);
        Assert.Equal(new float2(144, 16), renderer.MeasureText(default, Size, sentence, 200f));
        Assert.Equal(200f, renderer.LastMeasureWrapWidth);
        Assert.Equal(new float2(144, 16), renderer.MeasureText(default, Size, sentence, 144f));
        Assert.Equal(new float2(48, 48), renderer.MeasureText(default, Size, sentence, 60f));

        Assert.Equal(new float2(40, 32), renderer.MeasureText(default, Size, "brutalui"u8, 40f));
    }

    /// <summary>The edge cases of the recording rule: empty text, an infinite width, long words, and spaces.</summary>
    [Fact]
    public void TheRecordingRuleHandlesTheEdgeCases()
    {
        RecordingRenderer renderer = new();

        Assert.Equal(new float2(0, 16), renderer.MeasureText(default, Size, ReadOnlySpan<byte>.Empty, 100f));
        Assert.Equal(new float2(144, 16), renderer.MeasureText(default, Size, "Hello brutal world"u8, float.PositiveInfinity));
        Assert.Equal(float.PositiveInfinity, renderer.LastMeasureWrapWidth);

        // A long word after a word starts a new line. The next word follows the last part when it fits.
        Assert.Equal(new float2(40, 48), renderer.MeasureText(default, Size, "Hi brutalui"u8, 40f));
        Assert.Equal(new float2(40, 32), renderer.MeasureText(default, Size, "brutalui a"u8, 40f));
        Assert.Equal(new float2(40, 48), renderer.MeasureText(default, Size, "brutalui ab"u8, 40f));

        // A wrap width below one byte cuts a word into parts of one byte.
        Assert.Equal(new float2(8, 32), renderer.MeasureText(default, Size, "ab"u8, 4f));

        // Two spaces in a row give an empty word. The text that fits keeps the width with no wrap.
        Assert.Equal(new float2(32, 16), renderer.MeasureText(default, Size, "a  b"u8, 100f));
        Assert.Equal(new float2(16, 16), renderer.MeasureText(default, Size, " a"u8, 100f));
    }

    /// <summary>The snap rule gives 0 for no wrap, snaps a value near a whole number, and keeps another value.</summary>
    /// <param name="width">The available width.</param>
    /// <param name="expected">The wrap width.</param>
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(-1f, 0f)]
    [InlineData(float.NaN, 0f)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(float.NegativeInfinity, 0f)]
    [InlineData(95.999f, 96f)]
    [InlineData(96.01f, 96f)]
    [InlineData(96.02f, 96.02f)]
    [InlineData(70.3f, 70.3f)]
    [InlineData(100f, 100f)]
    public void SnapWrapWidth(float width, float expected)
    {
        Assert.Equal(expected, Label.SnapWrapWidth(width));
    }

    /// <summary>
    /// A label with <see cref="Align.FlexStart"/> in a root of width 100 measures at the limit 100. It takes the width
    /// of its widest line, and the draw wraps at that width.
    /// </summary>
    [Fact]
    public void WrapsAtTheAtMostLimit()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, 100f);
        Label label = AddLabel(context, Align.FlexStart);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("0,0,96,32", FormatRect(label.Layout.Rect));
        Assert.Equal(100f, renderer.LastMeasureWrapWidth);
        Assert.Equal(96f, TextCall(renderer).WrapWidth);
    }

    /// <summary>
    /// A stretched label in a root of width 100 gets the exact width 100. The measure and the draw both wrap at 100.
    /// </summary>
    [Fact]
    public void WrapsAtTheExactWidth()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, 100f);
        Label label = AddLabel(context, Align.Stretch);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("0,0,100,32", FormatRect(label.Layout.Rect));
        Assert.Equal(100f, TextCall(renderer).WrapWidth);
        Assert.Equal(TextCall(renderer).WrapWidth, renderer.LastMeasureWrapWidth);
    }

    /// <summary>
    /// A text that fits the limit gives the size with no wrap, as the contract of <see cref="MeasureCallback"/> states.
    /// The label sends no wrapped measure.
    /// </summary>
    [Fact]
    public void KeepsTheAtMostContract()
    {
        RecordingRenderer limited = new();
        UiContext limitedContext = CreateContext(limited, 200f);
        Label limitedLabel = AddLabel(limitedContext, Align.FlexStart);

        RecordingRenderer free = new();
        UiContext freeContext = CreateContext(free, float.NaN);
        Label freeLabel = AddLabel(freeContext, Align.FlexStart);

        limitedContext.Update(0f);
        freeContext.Update(0f);

        Assert.Equal("0,0,144,16", FormatRect(limitedLabel.Layout.Rect));
        Assert.Equal(freeLabel.Layout.Rect, limitedLabel.Layout.Rect);
        Assert.Equal(0f, limited.LastMeasureWrapWidth);
    }

    /// <summary>
    /// A text that fits the <see cref="MeasureMode.AtMost"/> limit exactly (144 pixels in a root of width 144) gives
    /// the size with no wrap. Each callback sends one measure with no wrap and no wrapped measure.
    /// </summary>
    [Fact]
    public void KeepsTheAtMostContractAtAnExactFit()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, 144f);
        Label label = AddLabel(context, Align.FlexStart);
        MeasureCallback inner = label.Layout.Measure!;
        int callbacks = 0;
        bool sawExactFit = false;
        label.Layout.Measure = (element, width, widthMode, height, heightMode) =>
        {
            callbacks++;
            sawExactFit |= widthMode == MeasureMode.AtMost && width == 144f;
            return inner(element, width, widthMode, height, heightMode);
        };

        context.Update(0f);

        Assert.Equal("0,0,144,16", FormatRect(label.Layout.Rect));
        Assert.True(sawExactFit, "The engine measures the label with the limit 144 under AtMost.");
        Assert.Equal(0f, renderer.LastMeasureWrapWidth);
        Assert.True(callbacks > 0, "The engine calls the measure callback.");
        Assert.Equal(callbacks, renderer.MeasureCount);
    }

    /// <summary>
    /// Pins a limit of the rule. A minimum width above the available width gives the box the lines of the available
    /// width, and the draw wraps at the wider box and uses fewer lines. A later fix sees the change in this test.
    /// </summary>
    [Fact]
    public void PinsTheMinimumWidthAboveTheAvailableWidth()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, 100f);
        Label label = AddLabel(context, Align.FlexStart);
        label.Style.MinWidth = 150;

        context.Update(0f);
        context.Draw(renderer);

        // The measure wraps at the limit 100 and gives two lines. The engine then raises the width to 150.
        Assert.Equal("0,0,150,32", FormatRect(label.Layout.Rect));
        Assert.Equal(100f, renderer.LastMeasureWrapWidth);

        // The draw wraps at the box width 150 and gives one line.
        float drawWrap = TextCall(renderer).WrapWidth;
        Assert.Equal(150f, drawWrap);
        Assert.Equal(new float2(144, 16), renderer.MeasureText(default, Size, label.Text, drawWrap));
    }

    /// <summary>A viewport with an undefined width gives the label an undefined width, and the label does not wrap.</summary>
    [Fact]
    public void DoesNotWrapWithAnUndefinedWidth()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, float.NaN);
        Label label = AddLabel(context, Align.FlexStart);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("0,0,144,16", FormatRect(label.Layout.Rect));
        Assert.Equal(0f, renderer.LastMeasureWrapWidth);
        Assert.Equal(144f, TextCall(renderer).WrapWidth);
    }

    /// <summary>
    /// A root of width 60 gives three lines: <c>Hello</c>, <c>brutal</c>, and <c>world</c>. The widest line is
    /// <c>brutal</c> at 48 pixels.
    /// </summary>
    [Fact]
    public void MeasuresATallerLabelInANarrowerBox()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, 60f);
        Label label = AddLabel(context, Align.FlexStart);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("0,0,48,48", FormatRect(label.Layout.Rect));
        Assert.Equal(60f, renderer.LastMeasureWrapWidth);
        float drawWrap = TextCall(renderer).WrapWidth;
        Assert.Equal(48f, drawWrap);
        Assert.Equal(new float2(48, 48), renderer.MeasureText(default, Size, label.Text, drawWrap));
    }

    /// <summary>The callback gives the content size only. The engine applies the minimum height of the label.</summary>
    [Fact]
    public void ReturnsTheContentSizeOnly()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, 100f);
        Label label = AddLabel(context, Align.FlexStart);
        label.Style.MinHeight = 50;

        context.Update(0f);

        Assert.Equal("0,0,96,50", FormatRect(label.Layout.Rect));
    }

    /// <summary>
    /// For each whole width from 1 to 200, the draw wraps at the same lines as the measure: a measure at the wrap
    /// width of the draw gives the size of the rect. The test checks a label with <see cref="Align.FlexStart"/> and a
    /// stretched label.
    /// </summary>
    /// <param name="align">The alignment of the label in its root.</param>
    [Theory]
    [InlineData(Align.FlexStart)]
    [InlineData(Align.Stretch)]
    public void KeepsTheLinesForEachWidth(Align align)
    {
        for (int width = 1; width <= 200; width++)
        {
            RecordingRenderer renderer = new();
            UiContext context = CreateContext(renderer, width);
            Label label = AddLabel(context, align);

            context.Update(0f);
            context.Draw(renderer);

            float4 rect = label.Layout.Rect;
            float drawWrap = TextCall(renderer).WrapWidth;
            float2 drawn = renderer.MeasureText(default, Size, label.Text, drawWrap);
            if (drawn.Y != rect.W || (width >= Size * 0.5f && drawn.X > rect.Z))
            {
                TestContext.Current.TestOutputHelper?.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"width {width}: rect {FormatRect(rect)}, draw wrap {drawWrap}, drawn {drawn.X}x{drawn.Y}"));
            }

            // The height checks the line count. A stretched box narrower than one byte (8 pixels) cannot hold a
            // glyph, so the width check starts at one byte.
            Assert.Equal(rect.W, drawn.Y);
            Assert.True(
                width < Size * 0.5f || drawn.X <= rect.Z,
                "The drawn lines fit the rect at width " + width + ".");
        }
    }

    /// <summary>Creates a context with a measurer and a viewport of the width by 300.</summary>
    /// <param name="measurer">The measurer.</param>
    /// <param name="width">The viewport width. <c>NaN</c> gives an undefined width.</param>
    /// <returns>The context.</returns>
    private static UiContext CreateContext(RecordingRenderer measurer, float width)
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = measurer }) { ViewportSize = new float2(width, 300) };
    }

    /// <summary>Adds a root and a white label of size 16 with <see cref="Sentence"/> to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="align">The <see cref="ComputedStyle.AlignItems"/> of the root.</param>
    /// <returns>The label.</returns>
    private static Label AddLabel(UiContext context, Align align)
    {
        Element root = new();
        root.Style.AlignItems = align;
        context.AddRoot(root);
        Label label = new(new Label.CreateInfo { Color = White, Size = Size, Text = Sentence });
        root.Children.Add(label);
        return label;
    }

    /// <summary>Gives the one text call of the last frame.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The call.</returns>
    private static DrawCall TextCall(RecordingRenderer renderer)
    {
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        Assert.Equal(3, calls.Length);
        Assert.Equal(DrawCallKind.Text, calls[1].Kind);
        return calls[1];
    }

    /// <summary>Formats a rect as <c>x,y,w,h</c> with the invariant culture.</summary>
    /// <param name="rect">The rect.</param>
    /// <returns>The text.</returns>
    private static string FormatRect(in float4 rect)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{rect.X},{rect.Y},{rect.Z},{rect.W}");
    }
}
