using System;
using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the content box in the draw of <see cref="Label"/> and <see cref="Image"/>: each element draws inside its
/// padding and its border, also with a percent padding.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RecordingRenderer"/> measures a text as (bytes * size * 0.5, size). Thus <c>Hello</c> at size 16 is
/// 40 by 16 pixels.
/// </para>
/// <para>
/// The demo texture has the id 7 and a size of 64 by 32 texels, so its aspect is 2.
/// </para>
/// </remarks>
public sealed class ContentBoxTests
{
    private const int DemoId = 7;

    /// <summary>A text of 43 bytes, 344 pixels wide at size 16. It wraps in each box of the tests.</summary>
    private const string LongText = "The quick brown fox jumps over the lazy dog";

    private static readonly Rgba8 White = new(255, 255, 255);
    private static readonly TextureHandle DemoTexture = new(DemoId, new int2(64, 32));

    /// <summary>
    /// The demo of phase 5. Context 1: a label with a padding of 10 pixels and a border of 2 pixels in a root with
    /// <see cref="Align.FlexStart"/> takes the size of its text plus the insets, and draws its text at the corner of
    /// its content box. Context 2: a root label with a padding of 10 % in a viewport of 400 by 300 resolves the padding
    /// against the viewport width (40 pixels on each edge), and wraps at the width of its content box.
    /// </summary>
    [Fact]
    public void DrawsTheLabelInsideItsPadding()
    {
        RecordingRenderer first = new();
        UiContext firstContext = CreateContext(first);
        Element root = AddRoot(firstContext, Align.FlexStart);
        Label padded = AddLabel(root, "Hello");
        padded.Style.Padding = LengthEdges.All(10);
        padded.Style.BorderWidth = LengthEdges.All(2);

        firstContext.Update(0f);
        firstContext.Draw(first);

        RecordingRenderer second = new();
        UiContext secondContext = CreateContext(second);
        Label rootLabel = CreateLabel("Hello");
        rootLabel.Style.Padding = LengthEdges.All(Length.Percent(10));
        secondContext.AddRoot(rootLabel);

        secondContext.Update(0f);
        secondContext.Draw(second);

        string rectLine = RectLine(padded.Layout.Rect);
        string firstText = DescribeOnlyCall(first);
        string secondText = DescribeOnlyCall(second);
        Print(rectLine);
        Print(firstText);
        Print(secondText);

        Assert.Equal("rect 0,0,64,40", rectLine);
        Assert.Equal("Text 12,12 font 0 size 16 #FFFFFFFF wrap 40 \"Hello\"", firstText);
        Assert.Equal("Text 40,40 font 0 size 16 #FFFFFFFF wrap 320 \"Hello\"", secondText);

        // The root label has the exact size of the viewport on both axes, so the engine skips its measure callback.
        // The draw still wraps at the width of the content box.
        Assert.Equal("rect 0,0,400,300", RectLine(rootLabel.Layout.Rect));
        Assert.Equal(0, second.MeasureCount);
    }

    /// <summary>
    /// A stretched label with a padding of 7.3 % in a root of width 333 gets a content width that is not a whole
    /// number. The draw wraps at the width of the last measure plus the float noise of the subtraction: never below
    /// that width, and at most two units in the last place of the box width above it.
    /// </summary>
    [Fact]
    public void KeepsTheSameWrapWidthWithAPercentPadding()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer, 333f);
        Element root = AddRoot(context, Align.Stretch);
        Label label = AddLabel(root, LongText);
        label.Style.Padding = LengthEdges.All(Length.Percent(7.3f));

        context.Update(0f);
        context.Draw(renderer);

        DrawCall text = FindTextCall(renderer);
        float4 rect = label.Layout.Rect;
        float4 insets = label.Layout.ContentInsets;
        Print(string.Create(
            CultureInfo.InvariantCulture,
            $"measure wrap {renderer.LastMeasureWrapWidth:R}, draw wrap {text.WrapWidth:R}"));

        float noise = 2f * (MathF.BitIncrement(rect.Z) - rect.Z);
        float measureWrap = renderer.LastMeasureWrapWidth;
        Assert.Equal(333f, rect.Z);
        Assert.True(measureWrap > 0f, "The label measures with a wrap width.");
        Assert.Equal(rect.Z - (insets.X + insets.Z), measureWrap);
        Assert.InRange(text.WrapWidth, measureWrap, measureWrap + noise);
        Assert.True(text.WrapWidth > measureWrap, "The draw raises a fractional wrap width.");
        Assert.NotEqual(MathF.Round(text.WrapWidth), text.WrapWidth);
        Assert.Equal(new float2(insets.X, insets.Y), text.Position);

        float2 drawn = renderer.MeasureText(text.Font, text.Size, label.Text, text.WrapWidth);
        Assert.Equal(rect.W - (insets.Y + insets.W), drawn.Y, 0.001f);
    }

    /// <summary>
    /// For each whole root width from 20 to 400, a label with a padding of 7.3 % draws the lines of its measure: the
    /// text at the draw wrap width has the height of the content box and fits its width.
    /// </summary>
    /// <param name="align">The <see cref="ComputedStyle.AlignItems"/> of the root.</param>
    [Theory]
    [InlineData(Align.FlexStart)]
    [InlineData(Align.Stretch)]
    public void KeepsTheLinesWithAPercentPaddingForEachWidth(Align align)
    {
        RecordingRenderer renderer = new();
        int failures = 0;
        for (int width = 20; width <= 400; width++)
        {
            UiContext context = CreateContext(renderer, width);
            Element root = AddRoot(context, align);
            Label label = AddLabel(root, LongText);
            label.Style.Padding = LengthEdges.All(Length.Percent(7.3f));

            context.Update(0f);
            context.Draw(renderer);

            DrawCall text = FindTextCall(renderer);
            float4 content = label.Layout.ContentBox(label.Layout.Rect);
            float2 drawn = renderer.MeasureText(text.Font, text.Size, label.Text, text.WrapWidth);
            if (MathF.Abs(drawn.Y - content.W) > 0.001f || drawn.X > content.Z + 0.001f)
            {
                failures++;
                Print(string.Create(
                    CultureInfo.InvariantCulture,
                    $"width {width}: content {content.Z:R} by {content.W:R}, drawn {drawn.X} by {drawn.Y}"));
            }
        }

        Assert.Equal(0, failures);
    }

    /// <summary>
    /// For each whole root width from 20 to 400, a label of size 13.5 with a padding of 7.3 % draws the line count of
    /// its box. <see cref="RecordingRenderer"/> gives each byte a width of 6.75, so the measured widths are not whole
    /// numbers, and the snap does not remove the float noise of the padding sum.
    /// </summary>
    /// <param name="align">The <see cref="ComputedStyle.AlignItems"/> of the root.</param>
    [Theory]
    [InlineData(Align.FlexStart)]
    [InlineData(Align.Stretch)]
    public void KeepsTheLinesWithAFractionalMeasuredWidth(Align align)
    {
        const float FractionalSize = 13.5f;
        RecordingRenderer renderer = new();
        int failures = 0;
        int wrapped = 0;
        for (int width = 20; width <= 400; width++)
        {
            UiContext context = CreateContext(renderer, width);
            Element root = AddRoot(context, align);
            Label label = new(new Label.CreateInfo { Color = White, Size = FractionalSize, Text = "Hello brutal world of the text" });
            label.Style.Padding = LengthEdges.All(Length.Percent(7.3f));
            root.Children.Add(label);

            context.Update(0f);
            context.Draw(renderer);

            DrawCall text = FindTextCall(renderer);
            float4 content = label.Layout.ContentBox(label.Layout.Rect);
            float2 drawn = renderer.MeasureText(text.Font, text.Size, label.Text, text.WrapWidth);
            if (content.W > FractionalSize * 1.5f)
            {
                wrapped++;
            }

            if (MathF.Abs(drawn.Y - content.W) > 0.001f)
            {
                failures++;
                Print(string.Create(
                    CultureInfo.InvariantCulture,
                    $"width {width}: content {content.Z:R} by {content.W:R}, draw wrap {text.WrapWidth:R}, drawn {drawn.X:R} by {drawn.Y:R}"));
            }
        }

        Print(string.Create(CultureInfo.InvariantCulture, $"{align}: wrapped {wrapped}, failures {failures}"));
        Assert.True(wrapped > 0, "The sweep has boxes with more than one line.");
        Assert.Equal(0, failures);
    }

    /// <summary>
    /// The draw wrap width: 0 and a whole number stay the same, and a fractional width gets two units in the last place
    /// of the border box width on top.
    /// </summary>
    [Fact]
    public void RaisesAFractionalDrawWrapWidthOnly()
    {
        Assert.Equal(0f, Label.DrawWrapWidth(0f, 20f));
        Assert.Equal(0f, Label.DrawWrapWidth(float.NaN, float.NaN));
        Assert.Equal(40f, Label.DrawWrapWidth(40f, 64f));
        Assert.Equal(96f, Label.DrawWrapWidth(MathF.BitDecrement(96f), 130.5f));
        Assert.Equal(96f, Label.DrawWrapWidth(MathF.BitIncrement(96f), 130.5f));

        float content = 284.382f;
        float box = 333f;
        float noise = 2f * (MathF.BitIncrement(box) - box);
        Assert.Equal(content + noise, Label.DrawWrapWidth(content, box));
        Assert.Equal(content, Label.DrawWrapWidth(content, float.MaxValue));
    }

    /// <summary>An image with a border of 2 pixels takes its texture size plus the border and draws inside the border.</summary>
    [Fact]
    public void DrawsTheImageInsideItsBorder()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Element root = AddRoot(context, Align.FlexStart);
        Image image = AddImage(root, ImageScaleMode.StretchToFill);
        image.Style.BorderWidth = LengthEdges.All(2);

        context.Update(0f);
        context.Draw(renderer);

        string imageLine = DescribeOnlyCall(renderer);
        Print(imageLine);

        Assert.Equal("rect 0,0,68,36", RectLine(image.Layout.Rect));
        Assert.Equal("Image 2,2,64,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0", imageLine);
    }

    /// <summary>A fit image of 100 by 100 with a padding of 10 fits the texture in the content box of 80 by 80.</summary>
    [Fact]
    public void FitsTheImageInsideItsPadding()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Element root = AddRoot(context, Align.FlexStart);
        Image image = AddImage(root, ImageScaleMode.ScaleToFit);
        image.Style.Width = 100;
        image.Style.Height = 100;
        image.Style.Padding = LengthEdges.All(10);

        context.Update(0f);
        context.Draw(renderer);

        string imageLine = DescribeOnlyCall(renderer);
        Print(imageLine);

        Assert.Equal("rect 0,0,100,100", RectLine(image.Layout.Rect));
        Assert.Equal("Image 10,30,80,40 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0", imageLine);
    }

    /// <summary>
    /// Each scale mode uses the content box of a padding with a different value on each edge (10, 20, 30, and 40):
    /// a content box of 60 by 40 at (10, 20).
    /// </summary>
    /// <param name="mode">The scale mode.</param>
    /// <param name="expected">The expected image line.</param>
    [Theory]
    [InlineData(ImageScaleMode.StretchToFill, "Image 10,20,60,40 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0")]
    [InlineData(ImageScaleMode.ScaleToFit, "Image 10,25,60,30 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0")]
    [InlineData(ImageScaleMode.ScaleAndCrop, "Image 10,20,60,40 texture 7 uv 0.125,0,0.875,1 tint #FFFFFFFF radius 0,0,0,0")]
    public void DrawsEachScaleModeInsideItsPadding(ImageScaleMode mode, string expected)
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Element root = AddRoot(context, Align.FlexStart);
        Image image = AddImage(root, mode);
        image.Style.Width = 100;
        image.Style.Height = 100;
        image.Style.Padding = new LengthEdges(10, 20, 30, 40);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(expected, DescribeOnlyCall(renderer));
    }

    /// <summary>
    /// A padding larger than the style size gives a content box with no area. Each scale mode then uses the rule of
    /// <see cref="ImageScaleMode.StretchToFill"/>: the empty content box and the full UV rect.
    /// </summary>
    /// <param name="mode">The scale mode.</param>
    [Theory]
    [InlineData(ImageScaleMode.StretchToFill)]
    [InlineData(ImageScaleMode.ScaleToFit)]
    [InlineData(ImageScaleMode.ScaleAndCrop)]
    public void DrawsAnEmptyContentBoxWithTheStretchRule(ImageScaleMode mode)
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Element root = AddRoot(context, Align.FlexStart);
        Image image = AddImage(root, mode);
        image.Style.Width = 10;
        image.Style.Height = 10;
        image.Style.Padding = LengthEdges.All(8);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("rect 0,0,16,16", RectLine(image.Layout.Rect));
        Assert.Equal("Image 8,8,0,0 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0", DescribeOnlyCall(renderer));
    }

    /// <summary>The content box moves with the parent: a label in a padded root draws at the sum of the two insets.</summary>
    [Fact]
    public void AddsTheInsetsOfTheParentAndTheLabel()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Element root = AddRoot(context, Align.FlexStart);
        root.Style.Padding = LengthEdges.All(5);
        root.Style.BorderWidth = LengthEdges.All(1);
        Label label = AddLabel(root, "Hello");
        label.Style.Padding = new LengthEdges(3, 4, 0, 0);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("rect 6,6,43,20", RectLine(label.Layout.Rect));
        Assert.Equal("Text 9,10 font 0 size 16 #FFFFFFFF wrap 40 \"Hello\"", DescribeOnlyCall(renderer));
    }

    // ---- Helpers -----------------------------------------------------------------------------------------------

    /// <summary>Creates a context with the renderer as its measurer and a viewport of the width by 300.</summary>
    /// <param name="measurer">The measurer.</param>
    /// <param name="width">The viewport width.</param>
    /// <returns>The context.</returns>
    private static UiContext CreateContext(RecordingRenderer measurer, float width = 400f)
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = measurer }) { ViewportSize = new float2(width, 300) };
    }

    /// <summary>Adds a root with an <see cref="ComputedStyle.AlignItems"/> value to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="align">The <see cref="ComputedStyle.AlignItems"/> of the root.</param>
    /// <returns>The root.</returns>
    private static Element AddRoot(UiContext context, Align align)
    {
        Element root = new();
        root.Style.AlignItems = align;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Creates a white label of size 16.</summary>
    /// <param name="text">The text of the label.</param>
    /// <returns>The label.</returns>
    private static Label CreateLabel(string text)
    {
        return new Label(new Label.CreateInfo { Color = White, Size = 16, Text = text });
    }

    /// <summary>Adds a white label of size 16 to a parent.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="text">The text of the label.</param>
    /// <returns>The label.</returns>
    private static Label AddLabel(Element parent, string text)
    {
        Label label = CreateLabel(text);
        parent.Children.Add(label);
        return label;
    }

    /// <summary>Adds an image of the demo texture to a parent.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="mode">The scale mode.</param>
    /// <returns>The image.</returns>
    private static Image AddImage(Element parent, ImageScaleMode mode)
    {
        Image image = new(new Image.CreateInfo { Texture = DemoTexture, ScaleMode = mode });
        parent.Children.Add(image);
        return image;
    }

    /// <summary>
    /// Gives the one draw call between <c>Begin</c> and <c>End</c> of the last frame. The method asserts that the frame
    /// holds exactly three calls.
    /// </summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The line of the call.</returns>
    private static string DescribeOnlyCall(RecordingRenderer renderer)
    {
        string[] lines = renderer.Describe().Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("Begin 400x300 scale 1", lines[0]);
        Assert.Equal("End", lines[2]);
        return lines[1];
    }

    /// <summary>Gives the one text call of the last frame.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The text call.</returns>
    private static DrawCall FindTextCall(RecordingRenderer renderer)
    {
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        DrawCall found = default;
        int count = 0;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Text)
            {
                found = calls[i];
                count++;
            }
        }

        Assert.Equal(1, count);
        return found;
    }

    /// <summary>Formats a rect as <c>rect x,y,w,h</c> with the invariant culture.</summary>
    /// <param name="rect">The rect.</param>
    /// <returns>The text.</returns>
    private static string RectLine(in float4 rect)
    {
        return string.Create(CultureInfo.InvariantCulture, $"rect {rect.X},{rect.Y},{rect.Z},{rect.W}");
    }

    /// <summary>Writes one line to the output of the current test.</summary>
    /// <param name="line">The line.</param>
    private static void Print(string line)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(line);
    }
}
