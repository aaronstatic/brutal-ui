using System;
using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of <see cref="Label"/> and of the measurer slot of <see cref="UiContext"/>: the measure route through the
/// thread-static scope of <see cref="UiContext.Update(float)"/>, and the draw rule of the label.
/// </summary>
/// <remarks>
/// <see cref="RecordingRenderer"/> measures a text as (bytes * size * 0.5, size). Thus <c>Hello</c> at size 16 is
/// 40 by 16 pixels.
/// </remarks>
public sealed class LabelTests
{
    private static readonly Rgba8 White = new(255, 255, 255);

    /// <summary>
    /// The demo of phase 1: a label in a root with <see cref="Align.FlexStart"/> takes the size of its text, and the
    /// draw sends one text call at the top-left corner with the width of the label as the wrap width.
    /// </summary>
    [Fact]
    public void DrawsAOneLineLabel()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");

        context.Update(0f);
        context.Draw(renderer);

        float4 rect = label.Layout.Rect;
        string rectLine = string.Create(CultureInfo.InvariantCulture, $"rect {rect.X},{rect.Y},{rect.Z},{rect.W}");
        string actual = renderer.Describe();
        TestContext.Current.TestOutputHelper?.WriteLine(rectLine);
        TestContext.Current.TestOutputHelper?.WriteLine(actual);

        Assert.Equal("rect 0,0,40,16", rectLine);
        string expected = string.Join(
            "\n",
            "Begin 400x300 scale 1",
            "Text 0,0 font 0 size 16 #FFFFFFFF wrap 40 \"Hello\"",
            "End");
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// The demo of phase 8: a label with no text value of its own takes the color and the size of its parent from a
    /// sheet rule. A new size on the parent flows down, and the label measures its text again.
    /// </summary>
    [Fact]
    public void ALabelDrawsInTheInheritedTextStyle()
    {
        Rgba8 gold = new(255, 204, 0, 255);
        ClassId panelClass = ClassName.Intern("panel");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), new StyleProperties { Color = gold, FontSize = 16 })
            .Build();

        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        context.AddSheet(sheet);
        Element root = AddRoot(context);
        Element panel = new();
        panel.Classes.Add(panelClass);
        Label label = new(new Label.CreateInfo { Text = "Hello" });
        panel.Children.Add(label);
        root.Children.Add(panel);

        context.Update(0f);
        context.Draw(renderer);

        string firstRect = DescribeRect(label);
        string firstText = DescribeText(renderer);
        TestContext.Current.TestOutputHelper?.WriteLine(firstRect);
        TestContext.Current.TestOutputHelper?.WriteLine(firstText);
        int firstMeasures = renderer.MeasureCount;

        panel.Inline.FontSize = 32;
        context.Update(0f);
        context.Draw(renderer);

        string secondRect = DescribeRect(label);
        string secondText = DescribeText(renderer);
        TestContext.Current.TestOutputHelper?.WriteLine(secondRect);
        TestContext.Current.TestOutputHelper?.WriteLine(secondText);

        Assert.Equal("rect 0,0,40,16", firstRect);
        Assert.Equal("Text 0,0 font 0 size 16 #FFCC00FF wrap 40 \"Hello\"", firstText);
        Assert.Equal("rect 0,0,80,32", secondRect);
        Assert.Equal("Text 0,0 font 0 size 32 #FFCC00FF wrap 80 \"Hello\"", secondText);
        Assert.True(renderer.MeasureCount > firstMeasures, "The new size measures the label again.");
        Assert.Equal(0UL, label.InlineStorage.SetMask);
    }

    /// <summary>The label measures through the measurer of the context, during <see cref="UiContext.Update(float)"/> only.</summary>
    [Fact]
    public void MeasuresThroughTheContextMeasurer()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        AddLabel(context, "Hello");

        Assert.Same(renderer, context.Measurer);
        Assert.Equal(0, renderer.MeasureCount);

        context.Update(0f);
        int afterUpdate = renderer.MeasureCount;
        Assert.True(afterUpdate > 0, "The update measures the label.");

        context.Draw(renderer);
        Assert.Equal(afterUpdate, renderer.MeasureCount);
    }

    /// <summary>A second update of a clean tree hits the layout cache and calls no measure.</summary>
    [Fact]
    public void ASecondUpdateOfACleanTreeMeasuresNothing()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        AddLabel(context, "Hello");

        context.Update(0f);
        int afterFirst = renderer.MeasureCount;
        context.Update(0f);

        Assert.Equal(afterFirst, renderer.MeasureCount);
    }

    /// <summary>A label in a context with no measurer throws from <see cref="UiContext.Update(float)"/>.</summary>
    [Fact]
    public void ThrowsWithNoMeasurer()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };
        AddLabel(context, "Hello");

        Assert.Null(context.Measurer);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => context.Update(0f));
        Assert.Equal(
            "A Label needs a UiContext with a measurer. Create the context with UiContext.CreateInfo.",
            exception.Message);
    }

    /// <summary>The create-info constructor throws for a null measurer, also for a default create-info.</summary>
    [Fact]
    public void ThrowsForANullMeasurer()
    {
        ArgumentNullException fromNull = Assert.Throws<ArgumentNullException>(
            () => new UiContext(new UiContext.CreateInfo { Measurer = null! }));
        Assert.Equal("info", fromNull.ParamName);

        ArgumentNullException fromDefault = Assert.Throws<ArgumentNullException>(
            () => new UiContext(default(UiContext.CreateInfo)));
        Assert.Equal("info", fromDefault.ParamName);
    }

    /// <summary>
    /// The scope holds the measurer of the context during the measure. It is null before and after
    /// <see cref="UiContext.Update(float)"/>, also after an exception in a context with no measurer.
    /// </summary>
    /// <remarks>
    /// The exception case of this test passes with no <c>finally</c> too, because the context has no measurer.
    /// <see cref="TheScopeIsEmptyAfterAnExceptionWithAMeasurer"/> guards the <c>finally</c>.
    /// </remarks>
    [Fact]
    public void TheScopeIsEmptyAfterUpdate()
    {
        Assert.Null(UiContext.CurrentMeasurer);

        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        ScopeProbe probe = AddProbe(context);

        context.Update(0f);

        Assert.Same(renderer, probe.SeenBefore);
        Assert.Null(UiContext.CurrentMeasurer);

        UiContext noMeasurer = new() { ViewportSize = new float2(400, 300) };
        AddLabel(noMeasurer, "Hello");

        Assert.Throws<InvalidOperationException>(() => noMeasurer.Update(0f));
        Assert.Null(UiContext.CurrentMeasurer);
    }

    /// <summary>
    /// An update inside a measure callback uses its own measurer, and the outer scope comes back after the inner
    /// update.
    /// </summary>
    [Fact]
    public void ANestedUpdateKeepsTheOuterScope()
    {
        RecordingRenderer outerRenderer = new();
        RecordingRenderer innerRenderer = new();
        UiContext outer = CreateContext(outerRenderer);
        UiContext inner = CreateContext(innerRenderer);
        AddLabel(inner, "Hello");

        ScopeProbe probe = AddProbe(outer);
        probe.Inner = inner;

        outer.Update(0f);

        Assert.Same(outerRenderer, probe.SeenBefore);
        Assert.Same(outerRenderer, probe.SeenAfter);
        Assert.True(innerRenderer.MeasureCount > 0, "The inner update measures with the inner measurer.");
        Assert.Equal(0, outerRenderer.MeasureCount);
        Assert.Null(UiContext.CurrentMeasurer);
    }

    /// <summary>
    /// A measure callback that throws in a context with a measurer leaves the scope null after
    /// <see cref="UiContext.Update(float)"/>. The test fails when the scope has no restore in a <c>finally</c>.
    /// </summary>
    [Fact]
    public void TheScopeIsEmptyAfterAnExceptionWithAMeasurer()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Element root = AddRoot(context);
        root.Children.Add(new ThrowingProbe());

        Assert.Null(UiContext.CurrentMeasurer);
        Assert.Throws<MeasureProbeException>(() => context.Update(0f));
        Assert.Null(UiContext.CurrentMeasurer);
    }

    /// <summary>
    /// An inner update throws inside a measure callback of an outer update, and the callback catches the exception.
    /// The outer scope comes back after the catch, and a later label of the outer tree measures with the outer
    /// measurer. The test fails when the scope has no restore in a <c>finally</c>.
    /// </summary>
    [Fact]
    public void ANestedUpdateThatThrowsKeepsTheOuterScope()
    {
        RecordingRenderer outerRenderer = new();
        RecordingRenderer innerRenderer = new();
        UiContext outer = CreateContext(outerRenderer);
        UiContext inner = CreateContext(innerRenderer);
        Element innerRoot = AddRoot(inner);
        innerRoot.Children.Add(new ThrowingProbe());

        Element outerRoot = AddRoot(outer);
        CatchingProbe probe = new() { Inner = inner };
        outerRoot.Children.Add(probe);
        Label label = new(new Label.CreateInfo { Text = "After" });
        label.Inline.Color = White;
        label.Inline.FontSize = 16;
        outerRoot.Children.Add(label);

        outer.Update(0f);

        Assert.True(probe.Caught, "The inner update throws, and the probe catches the exception.");
        Assert.Same(outerRenderer, probe.SeenAfter);
        Assert.True(outerRenderer.MeasureCount > 0, "The later label measures with the outer measurer.");
        Assert.Equal(0, innerRenderer.MeasureCount);
        Assert.Equal(40f, label.Layout.Rect.Z);
        Assert.Null(UiContext.CurrentMeasurer);
    }

    /// <summary>Two contexts measure each label with the measurer of its own context.</summary>
    [Fact]
    public void TwoContextsUseTheirOwnMeasurer()
    {
        RecordingRenderer first = new();
        RecordingRenderer second = new();
        UiContext firstContext = CreateContext(first);
        UiContext secondContext = CreateContext(second);
        AddLabel(firstContext, "Hello");
        AddLabel(secondContext, "World");

        firstContext.Update(0f);
        int firstCount = first.MeasureCount;
        Assert.True(firstCount > 0, "The first update measures with the first measurer.");
        Assert.Equal(0, second.MeasureCount);

        secondContext.Update(0f);
        Assert.True(second.MeasureCount > 0, "The second update measures with the second measurer.");
        Assert.Equal(firstCount, first.MeasureCount);
    }

    /// <summary>The label scales the alpha of its color by the opacity: 255 * 0.5 + 0.5 = 128.</summary>
    [Fact]
    public void ScalesTheColorByTheOpacity()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, "Hello");
        label.Inline.Opacity = 0.5f;

        context.Update(0f);
        context.Draw(renderer);

        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        Assert.Equal(3, calls.Length);
        Assert.Equal(DrawCallKind.Text, calls[1].Kind);
        Assert.Equal(new Rgba8(255, 255, 255, 128), calls[1].Color);
    }

    /// <summary>A label with no text, or with an empty text, sends no text call.</summary>
    /// <param name="text">The text of the label.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DrawsNothingForEmptyText(string? text)
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Label label = AddLabel(context, text);

        context.Update(0f);
        context.Draw(renderer);

        Assert.True(label.Text.IsEmpty);
        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());
    }

    /// <summary>A label with a transparent color sends no text call.</summary>
    [Fact]
    public void DrawsNothingForATransparentColor()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext(renderer);
        Element root = AddRoot(context);
        Label label = new(new Label.CreateInfo { Text = "Hello" });
        label.Inline.Color = Rgba8.Transparent;
        label.Inline.FontSize = 16;
        root.Children.Add(label);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());
    }

    /// <summary>
    /// The label owns a UTF-8 copy of its text. A text longer than the start buffer and a text with multi-byte
    /// characters keep each byte.
    /// </summary>
    [Fact]
    public void EncodesTheTextToUtf8()
    {
        string longText = new('x', 100);
        Label longLabel = new(new Label.CreateInfo { Text = longText });
        Assert.Equal(100, longLabel.Text.Length);

        Label accented = new(new Label.CreateInfo { Text = "hé" });
        Assert.Equal(new byte[] { 0x68, 0xC3, 0xA9 }, accented.Text.ToArray());

        // The create-info has no text values, so a new label sets no inline value.
        Label defaults = new(default(Label.CreateInfo));
        Assert.Equal(0UL, defaults.InlineStorage.SetMask);
        Assert.True(defaults.Text.IsEmpty);
    }

    /// <summary>
    /// A measurer result that is not finite, or that is below 0, gives the label a finite rect. The callback sets each
    /// bad component to 0, for the measure with no wrap and for the wrapped measure.
    /// </summary>
    /// <param name="viewportWidth">The viewport width. <c>NaN</c> gives an undefined width.</param>
    /// <param name="freeX">The width of the measure with no wrap.</param>
    /// <param name="freeY">The height of the measure with no wrap.</param>
    /// <param name="wrappedX">The width of the wrapped measure.</param>
    /// <param name="wrappedY">The height of the wrapped measure.</param>
    /// <param name="expected">The rect of the label as <c>x,y,w,h</c>.</param>
    [Theory]
    [InlineData(400f, float.PositiveInfinity, float.NaN, 10f, 10f, "0,0,0,0")]
    [InlineData(float.NaN, float.PositiveInfinity, float.NaN, 10f, 10f, "0,0,0,0")]
    [InlineData(400f, float.NaN, float.NegativeInfinity, 10f, 10f, "0,0,0,0")]
    [InlineData(400f, -5f, 16f, 10f, 10f, "0,0,0,16")]
    [InlineData(400f, 1000f, 16f, float.PositiveInfinity, float.NaN, "0,0,0,0")]
    [InlineData(400f, 1000f, 16f, -3f, 32f, "0,0,0,32")]
    public void GivesAFiniteSizeForABadMeasurerResult(
        float viewportWidth,
        float freeX,
        float freeY,
        float wrappedX,
        float wrappedY,
        string expected)
    {
        FixedMeasurer measurer = new(new float2(freeX, freeY), new float2(wrappedX, wrappedY));
        UiContext context = new(new UiContext.CreateInfo { Measurer = measurer })
        {
            ViewportSize = new float2(viewportWidth, 300),
        };
        Element root = AddRoot(context);
        Label label = new(new Label.CreateInfo { Text = "Hello" });
        label.Inline.Color = White;
        label.Inline.FontSize = 16;
        root.Children.Add(label);

        context.Update(0f);

        float4 rect = label.Layout.Rect;
        Assert.True(
            float.IsFinite(rect.X) && float.IsFinite(rect.Y) && float.IsFinite(rect.Z) && float.IsFinite(rect.W),
            "The rect is finite.");
        Assert.Equal(expected, string.Create(CultureInfo.InvariantCulture, $"{rect.X},{rect.Y},{rect.Z},{rect.W}"));
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

    /// <summary>Adds a root and a white label of size 16 to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="text">The text of the label.</param>
    /// <returns>The label.</returns>
    private static Label AddLabel(UiContext context, string? text)
    {
        Element root = AddRoot(context);
        Label label = new(new Label.CreateInfo { Text = text });
        label.Inline.Color = White;
        label.Inline.FontSize = 16;
        root.Children.Add(label);
        return label;
    }

    /// <summary>Describes the rect of an element as <c>rect x,y,w,h</c>.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The line.</returns>
    private static string DescribeRect(Element element)
    {
        float4 rect = element.Layout.Rect;
        return string.Create(CultureInfo.InvariantCulture, $"rect {rect.X},{rect.Y},{rect.Z},{rect.W}");
    }

    /// <summary>Gives the one text line of the last draw of a renderer, or an empty string when it has none.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The text line.</returns>
    private static string DescribeText(RecordingRenderer renderer)
    {
        string[] lines = renderer.Describe().Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("Text ", StringComparison.Ordinal))
            {
                return lines[i];
            }
        }

        return string.Empty;
    }

    /// <summary>Adds a root and a scope probe to a context.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The probe.</returns>
    private static ScopeProbe AddProbe(UiContext context)
    {
        Element root = AddRoot(context);
        ScopeProbe probe = new();
        root.Children.Add(probe);
        return probe;
    }

    /// <summary>
    /// A leaf whose measure callback records the scope. When <see cref="Inner"/> is set, the callback runs an update of
    /// that context between the two reads.
    /// </summary>
    private sealed class ScopeProbe : Element
    {
        /// <summary>Creates a probe with the static measure callback.</summary>
        public ScopeProbe()
        {
            Layout.Measure = static (element, width, widthMode, height, heightMode) =>
            {
                ScopeProbe probe = (ScopeProbe)element;
                probe.SeenBefore = UiContext.CurrentMeasurer;
                probe.Inner?.Update(0f);
                probe.SeenAfter = UiContext.CurrentMeasurer;
                return new float2(10, 10);
            };
        }

        /// <summary>Gets or sets a context that the callback updates.</summary>
        public UiContext? Inner { get; set; }

        /// <summary>Gets the scope at the start of the last measure.</summary>
        public IUiRenderer? SeenBefore { get; private set; }

        /// <summary>Gets the scope at the end of the last measure.</summary>
        public IUiRenderer? SeenAfter { get; private set; }
    }

    /// <summary>The exception of <see cref="ThrowingProbe"/>. No other code throws it.</summary>
    private sealed class MeasureProbeException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public MeasureProbeException()
            : base("The measure probe throws.")
        {
        }
    }

    /// <summary>A leaf whose static measure callback throws a <see cref="MeasureProbeException"/>.</summary>
    private sealed class ThrowingProbe : Element
    {
        /// <summary>Creates a probe with the static measure callback.</summary>
        public ThrowingProbe()
        {
            Layout.Measure = static (element, width, widthMode, height, heightMode) => throw new MeasureProbeException();
        }
    }

    /// <summary>
    /// A leaf whose static measure callback runs an update of <see cref="Inner"/> in a <c>try</c> block, catches a
    /// <see cref="MeasureProbeException"/>, and then records the scope.
    /// </summary>
    private sealed class CatchingProbe : Element
    {
        /// <summary>Creates a probe with the static measure callback.</summary>
        public CatchingProbe()
        {
            Layout.Measure = static (element, width, widthMode, height, heightMode) =>
            {
                CatchingProbe probe = (CatchingProbe)element;
                try
                {
                    probe.Inner?.Update(0f);
                }
                catch (MeasureProbeException)
                {
                    probe.Caught = true;
                }

                probe.SeenAfter = UiContext.CurrentMeasurer;
                return new float2(10, 10);
            };
        }

        /// <summary>Gets or sets the context that the callback updates.</summary>
        public UiContext? Inner { get; set; }

        /// <summary>Gets a value that is <see langword="true"/> when the callback caught the exception of the inner update.</summary>
        public bool Caught { get; private set; }

        /// <summary>Gets the scope after the inner update.</summary>
        public IUiRenderer? SeenAfter { get; private set; }
    }

    /// <summary>
    /// A measurer that gives one fixed size for a measure with no wrap and another fixed size for a wrapped measure.
    /// The draw members do nothing.
    /// </summary>
    /// <param name="free">The size for a wrap width of 0 or less.</param>
    /// <param name="wrapped">The size for a wrap width above 0.</param>
    private sealed class FixedMeasurer(float2 free, float2 wrapped) : IUiRenderer
    {
        /// <inheritdoc/>
        public void Begin(in float2 displaySize, float scale)
        {
        }

        /// <inheritdoc/>
        public void PushClip(in float4 rect)
        {
        }

        /// <inheritdoc/>
        public void PopClip()
        {
        }

        /// <inheritdoc/>
        public void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor)
        {
        }

        /// <inheritdoc/>
        public void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius)
        {
        }

        /// <inheritdoc/>
        public void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
        }

        /// <inheritdoc/>
        public float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
            return wrapWidth > 0f ? wrapped : free;
        }

        /// <inheritdoc/>
        public void End()
        {
        }
    }
}
