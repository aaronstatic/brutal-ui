using System;
using System.Globalization;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// Tests of <see cref="ImGuiRenderer.Text"/> and <see cref="ImGuiRenderer.MeasureText"/> on a headless ImGui context.
/// </summary>
/// <remarks>
/// The default font of the harness is the ImGui default font with a size of 13. Each glyph of it is 7 pixels wide at
/// size 13. So <c>Hello, brutal-ui</c> measures 112 by 13 at size 13.
/// </remarks>
public sealed unsafe class ImGuiRendererTextTests
{
    private const string EmptyList = "cmd 0 clip 0,0,400,300 tex atlas elems 0 verts none colors";

    /// <summary>The size of the default font of the harness.</summary>
    private const float DefaultSize = 13f;

    /// <summary>The glyph count of <see cref="Hello"/>. The blank makes no glyph.</summary>
    private const int HelloGlyphCount = 15;

    /// <summary>The index count of one glyph quad.</summary>
    private const int IndicesPerGlyph = 6;

    private static readonly float2 One = new(1f, 1f);
    private static readonly Rgba8 White = new(255, 255, 255);

    /// <summary>The text of the demo.</summary>
    private static ReadOnlySpan<byte> Hello => "Hello, brutal-ui"u8;

    // ---- MeasureText -------------------------------------------------------------------------------------------

    /// <summary>One line of text has the font size as its height.</summary>
    [Fact]
    public void MeasuresOneLineWithTheSizeAsHeight()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        float2 size = renderer.MeasureText(default, DefaultSize, Hello, 0f);

        Assert.Equal(new float2(112, 13), size);
    }

    /// <summary>Twice the size gives twice the width and twice the height.</summary>
    [Fact]
    public void DoublesTheHeightForTwiceTheSize()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        float2 small = renderer.MeasureText(default, 13f, Hello, 0f);
        float2 large = renderer.MeasureText(default, 26f, Hello, 0f);

        Assert.Equal(26f, large.Y);
        Assert.Equal(2f * small.Y, large.Y);
        Assert.Equal(new float2(224, 26), large);
    }

    /// <summary>
    /// A wrap width of 70 gives two lines: <c>Hello,</c> and <c>brutal-ui</c>. The mark <c>-</c> is no break point.
    /// </summary>
    [Fact]
    public void WrapsAtANarrowWidth()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        float2 size = renderer.MeasureText(default, DefaultSize, Hello, 70f);

        Assert.Equal(new float2(63, 26), size);
        Assert.True(size.X <= 70f);
    }

    /// <summary>A wrap width of 0 or less, <c>NaN</c>, or an infinite width gives the no-wrap result.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    [InlineData(-0.5f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void TreatsAZeroNegativeOrNaNWrapAsNoWrap(float wrapWidth)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        float2 noWrap = renderer.MeasureText(default, DefaultSize, Hello, 0f);
        float2 size = renderer.MeasureText(default, DefaultSize, Hello, wrapWidth);

        Assert.Equal(noWrap, size);
        Assert.Equal(new float2(112, 13), size);
    }

    /// <summary>
    /// <c>brutal-ui</c> (63 pixels) at a wrap width of 40 gives more than one line, and each line stays at or below the
    /// wrap width. ImGui cuts the word inside the word.
    /// </summary>
    [Fact]
    public void CutsAWordThatIsWiderThanTheWrapWidth()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        float2 noWrap = renderer.MeasureText(default, DefaultSize, "brutal-ui"u8, 0f);
        float2 size = renderer.MeasureText(default, DefaultSize, "brutal-ui"u8, 40f);
        TestContext.Current.TestOutputHelper?.WriteLine(
            "brutal-ui wrap 40 -> " + HeadlessImGui.Format(size.X) + "x" + HeadlessImGui.Format(size.Y));

        Assert.Equal(new float2(63, 13), noWrap);
        Assert.True(size.Y > DefaultSize);
        Assert.Equal(0f, size.Y % DefaultSize);
        Assert.True(size.X <= 40f);
        Assert.Equal(new float2(35, 26), size);
    }

    /// <summary>
    /// A line breaks after a blank or after a mark of <c>. , ; ! ? "</c>. The blank at a break does not count in the
    /// width, and the mark stays on the first line. The mark <c>-</c> is no break point, so a word with <c>-</c>
    /// breaks only where it does not fit.
    /// </summary>
    [Theory]
    [InlineData("aaaa bbbb", 28f)]
    [InlineData("aaaa.bbbb", 35f)]
    [InlineData("aaaa,bbbb", 35f)]
    [InlineData("aaaa;bbbb", 35f)]
    [InlineData("aaaa!bbbb", 35f)]
    [InlineData("aaaa?bbbb", 35f)]
    [InlineData("aaaa\"bbbb", 35f)]
    [InlineData("aaaa-bbbb", 49f)]
    public void BreaksAfterABlankOrAMark(string text, float expectedWidth)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(text);

        float2 size = renderer.MeasureText(default, DefaultSize, utf8, 50f);

        Assert.Equal(new float2(expectedWidth, 26), size);
    }

    /// <summary>A line feed starts a new line. A line feed at the end of the text adds no line.</summary>
    [Fact]
    public void CountsALineFeedAsANewLine()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        Assert.Equal(new float2(7, 26), renderer.MeasureText(default, DefaultSize, "a\nb"u8, 0f));
        Assert.Equal(new float2(7, 13), renderer.MeasureText(default, DefaultSize, "a\n"u8, 0f));
        Assert.Equal(new float2(0, 13), renderer.MeasureText(default, DefaultSize, "\n"u8, 0f));
    }

    /// <summary>The width rounds up to a whole pixel, as <c>ImGui.CalcTextSize</c> does.</summary>
    [Fact]
    public void RoundsTheWidthUpToAWholePixel()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        ImFontPtr font = renderer.Fonts.GetFont(default);

        float2 raw;
        fixed (byte* begin = Hello)
        {
            raw = Gui.PInvoke.ImFont_CalcTextSizeA(
                font.Ptr, 13.5f, float.MaxValue, 0f, begin, begin + Hello.Length, null);
        }

        float2 size = renderer.MeasureText(default, 13.5f, Hello, 0f);

        Assert.NotEqual(MathF.Floor(raw.X), raw.X);
        Assert.Equal(MathF.Ceiling(raw.X), size.X);
        Assert.Equal(raw.Y, size.Y);
        Assert.Equal(13.5f, size.Y);
    }

    /// <summary>An empty text gives a width of 0 and the height of one line, also with a wrap width.</summary>
    [Fact]
    public void MeasuresEmptyTextAsZeroWidthAndOneLine()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        Assert.Equal(new float2(0, 13), renderer.MeasureText(default, DefaultSize, ReadOnlySpan<byte>.Empty, 0f));
        Assert.Equal(new float2(0, 26), renderer.MeasureText(default, 26f, ReadOnlySpan<byte>.Empty, 0f));
        Assert.Equal(new float2(0, 13), renderer.MeasureText(default, DefaultSize, ReadOnlySpan<byte>.Empty, 70f));
        Assert.Equal(new float2(0, 13), renderer.MeasureText(default, 0f, ReadOnlySpan<byte>.Empty, 0f));
    }

    /// <summary>A size of 0 or less, <c>NaN</c>, or an infinite size uses the size of the font.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void UsesTheFontSizeForASizeOfZero(float size)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        float2 measured = renderer.MeasureText(default, size, Hello, 0f);

        Assert.Equal(renderer.MeasureText(default, DefaultSize, Hello, 0f), measured);
        Assert.Equal(new float2(112, 13), measured);
        Assert.Equal(new float2(63, 26), renderer.MeasureText(default, size, Hello, 70f));
    }

    /// <summary>
    /// A resolved size of 0 or less, or <c>NaN</c>, gives a measure of (0, 0) and no draw, with no native text call. A
    /// positive call size still uses the call size.
    /// </summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    [InlineData(float.NaN)]
    public void UsesNoNativeCallForAResolvedSizeOfZero(float legacySize)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        ImFontPtr font = renderer.Fonts.GetFont(default);
        float saved = font.LegacySize;

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        try
        {
            font.LegacySize = legacySize;

            Assert.Equal(default, renderer.MeasureText(default, 0f, Hello, 0f));
            Assert.Equal(default, renderer.MeasureText(default, 0f, Hello, 70f));
            Assert.Equal(default, renderer.MeasureText(default, 0f, ReadOnlySpan<byte>.Empty, 0f));
            Assert.Equal(new float2(112, 13), renderer.MeasureText(default, DefaultSize, Hello, 0f));

            // ImGui draws a size of 0 at the font size of the frame, so a native call shows as vertices.
            renderer.Text(new float2(10, 10), default, 0f, White, Hello, 0f);
            renderer.Text(new float2(10, 10), default, -3f, White, Hello, 70f);
        }
        finally
        {
            font.LegacySize = saved;
        }

        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// The measure gives the same result before the first frame, inside a frame, after <c>ImGui.Render</c>, and
    /// between frames.
    /// </summary>
    [Fact]
    public void MeasuresTheSameBeforeInsideAndBetweenFrames()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        Assert.Equal(0, Gui.GetFrameCount());
        float2[] before = MeasureThree(renderer);

        imgui.BeginFrame();
        float2[] inside = MeasureThree(renderer);
        renderer.Begin(new float2(400, 300), 1f);
        float2[] insideBegin = MeasureThree(renderer);
        renderer.End();
        imgui.EndFrame();
        float2[] afterRender = MeasureThree(renderer);

        imgui.BeginFrame();
        imgui.EndFrame();
        imgui.BeginFrame();
        float2[] laterFrame = MeasureThree(renderer);
        imgui.EndFrame();
        float2[] between = MeasureThree(renderer);

        float2[] expected = [new float2(112, 13), new float2(224, 26), new float2(63, 26)];
        Assert.Equal(expected, before);
        Assert.Equal(expected, inside);
        Assert.Equal(expected, insideBegin);
        Assert.Equal(expected, afterRender);
        Assert.Equal(expected, laterFrame);
        Assert.Equal(expected, between);
    }

    /// <summary>A measure inside a frame adds no command, no index, and no vertex to the list.</summary>
    [Fact]
    public void MeasureDrawsNothing()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        int commands = list.CmdBuffer.Count;
        int indices = list.IdxBuffer.Count;
        int vertices = list.VtxBuffer.Count;

        _ = renderer.MeasureText(default, DefaultSize, Hello, 0f);
        _ = renderer.MeasureText(default, 26f, Hello, 70f);
        _ = renderer.MeasureText(default, 40f, "brutal-ui"u8, 40f);

        Assert.Equal(commands, list.CmdBuffer.Count);
        Assert.Equal(indices, list.IdxBuffer.Count);
        Assert.Equal(vertices, list.VtxBuffer.Count);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// A second font measures with its own glyphs, and a size of 0 uses its own size. The default font does not
    /// change.
    /// </summary>
    [Fact]
    public void MeasuresWithASecondFont()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        _ = renderer.Fonts.GetFont(default);
        FontHandle second = renderer.Fonts.Add(AddWideFont(20f));

        float2 secondAtZero = renderer.MeasureText(second, 0f, Hello, 0f);
        float2 secondAt13 = renderer.MeasureText(second, DefaultSize, Hello, 0f);
        float2 defaultAt13 = renderer.MeasureText(default, DefaultSize, Hello, 0f);
        TestContext.Current.TestOutputHelper?.WriteLine(
            "second 0 -> " + Describe(secondAtZero) + ", second 13 -> " + Describe(secondAt13));

        Assert.Equal(1, second.Id);
        Assert.Equal(20f, secondAtZero.Y);
        Assert.Equal(13f, secondAt13.Y);
        Assert.True(secondAt13.X > defaultAt13.X);
        Assert.Equal(new float2(128, 13), secondAt13);
        Assert.Equal(new float2(189, 20), secondAtZero);
        Assert.Equal(new float2(112, 13), defaultAt13);
    }

    /// <summary>An unknown font id throws, also for an empty text.</summary>
    [Fact]
    public void ThrowsForAnUnknownFont()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.MeasureText(new FontHandle(1), 13f, Hello, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.MeasureText(new FontHandle(1), 13f, ReadOnlySpan<byte>.Empty, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.MeasureText(new FontHandle(-1), 13f, Hello, 0f));

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.Text(new float2(10, 10), new FontHandle(1), 13f, White, Hello, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.Text(new float2(10, 10), new FontHandle(1), 13f, White, ReadOnlySpan<byte>.Empty, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.Text(new float2(10, 10), new FontHandle(1), 13f, Rgba8.Transparent, Hello, 0f));
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>With no current context, the measure throws before a native call.</summary>
    [Fact]
    public void MeasureThrowsWithNoContext()
    {
        Assert.True(Gui.GetCurrentContext().IsNull());
        ImGuiRenderer renderer = CreateRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.MeasureText(default, 13f, Hello, 0f));
        Assert.Throws<InvalidOperationException>(
            () => renderer.MeasureText(default, 13f, ReadOnlySpan<byte>.Empty, 0f));
    }

    // ---- Text --------------------------------------------------------------------------------------------------

    /// <summary>A text call before <see cref="ImGuiRenderer.Begin"/> or after the end of a frame throws.</summary>
    [Fact]
    public void ThrowsForATextCallBeforeBegin()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        Assert.Throws<InvalidOperationException>(
            () => renderer.Text(new float2(10, 10), default, 13f, White, Hello, 0f));
        Assert.Throws<InvalidOperationException>(
            () => renderer.Text(new float2(10, 10), default, 13f, White, ReadOnlySpan<byte>.Empty, 0f));

        renderer.Begin(new float2(400, 300), 1f);
        renderer.End();
        Assert.Throws<InvalidOperationException>(
            () => renderer.Text(new float2(10, 10), default, 13f, White, Hello, 0f));
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>Each glyph vertex lies inside the measured box, grown by 1 pixel, at two sizes.</summary>
    [Theory]
    [InlineData(13f)]
    [InlineData(26f)]
    public void DrawsGlyphsInsideTheMeasuredBox(float size)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        float2 pos = new(10, 10);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        float2 measured = renderer.MeasureText(default, size, Hello, 0f);
        renderer.Text(pos, default, size, White, Hello, 0f);
        renderer.End();
        imgui.EndFrame();

        VertexBounds bounds = ReadVertexBounds();
        TestContext.Current.TestOutputHelper?.WriteLine(imgui.DescribeBackgroundList());

        Assert.Equal(HelloGlyphCount * 4, bounds.Count);
        AssertInsideBox(bounds, pos, measured);
    }

    /// <summary>
    /// The wrapped text draws inside the wrapped box, grown by 1 pixel, and its vertices span two lines. So
    /// <see cref="ImGuiRenderer.Text"/> passes the wrap width to ImGui.
    /// </summary>
    [Fact]
    public void DrawsWrappedTextInsideTheWrappedBox()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        float2 pos = new(10, 10);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        float2 measured = renderer.MeasureText(default, DefaultSize, Hello, 70f);
        renderer.Text(pos, default, DefaultSize, White, Hello, 70f);
        renderer.End();
        imgui.EndFrame();

        VertexBounds bounds = ReadVertexBounds();
        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);

        Assert.Equal(new float2(63, 26), measured);
        AssertInsideBox(bounds, pos, measured);
        Assert.True(bounds.Max.Y > pos.Y + DefaultSize);
        Assert.True(bounds.Min.Y < pos.Y + DefaultSize);
        Assert.Equal("cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 11,12-70,34 colors #FFFFFFFF", dump);
    }

    /// <summary>Each glyph vertex has the text color.</summary>
    [Fact]
    public void DrawsTheTextColor()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Text(new float2(10, 10), default, DefaultSize, new Rgba8(255, 128, 0), Hello, 0f);
        renderer.Text(new float2(10, 40), default, DefaultSize, new Rgba8(16, 32, 64, 128), Hello, 0f);
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        Assert.StartsWith("cmd 0 clip 0,0,400,300 tex atlas elems 180 verts ", dump);
        Assert.EndsWith(" colors #FF8000FF #10204080", dump);
    }

    /// <summary>An empty text draws nothing.</summary>
    [Fact]
    public void DrawsNothingForEmptyText()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Text(new float2(10, 10), default, DefaultSize, White, ReadOnlySpan<byte>.Empty, 0f);
        renderer.Text(new float2(10, 10), default, DefaultSize, White, ReadOnlySpan<byte>.Empty, 70f);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>A text color with an alpha of 0 draws nothing.</summary>
    [Fact]
    public void DrawsNothingForAZeroAlpha()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Text(new float2(10, 10), default, DefaultSize, new Rgba8(255, 255, 255, 0), Hello, 0f);
        renderer.Text(new float2(10, 10), default, DefaultSize, Rgba8.Transparent, Hello, 70f);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>A position with a <c>NaN</c> or infinite component draws nothing.</summary>
    [Theory]
    [InlineData(float.NaN, 10f)]
    [InlineData(10f, float.NaN)]
    [InlineData(float.PositiveInfinity, 10f)]
    [InlineData(10f, float.NegativeInfinity)]
    public void SkipsANonFinitePosition(float x, float y)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Text(new float2(x, y), default, DefaultSize, White, Hello, 0f);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// The text command carries the pushed clip. ImGui also drops each glyph that lies fully outside the clip on the
    /// CPU, so only the 6 glyphs of <c>Hello,</c> that touch the clip from x 5 to 55 draw. The blank makes no glyph.
    /// </summary>
    [Fact]
    public void ClipsTextUnderAPushedClip()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(5, 5, 50, 20));
        renderer.Text(new float2(10, 10), default, DefaultSize, White, Hello, 0f);
        renderer.PopClip();
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);
        string[] lines = dump.Split('\n');
        string last = lines[^1];

        Assert.StartsWith("cmd ", last);
        Assert.Contains(
            " clip 5,5,55,25 tex atlas elems " +
            (6 * IndicesPerGlyph).ToString(CultureInfo.InvariantCulture) + " verts 11,12-48,23 colors #FFFFFFFF",
            last);
    }

    // ---- A span with no zero byte ------------------------------------------------------------------------------

    /// <summary>
    /// A <c>u8</c> literal has a zero byte at its end, so ImGui finds the end of a literal with <c>strlen</c>. A slice
    /// of the literal has the byte <c>,</c> after it. The renderer gives the end of the span to ImGui, so the measure
    /// and the draw cover the slice only.
    /// </summary>
    [Fact]
    public void MeasuresAndDrawsASpanWithNoZeroByte()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        float2 pos = new(10, 10);

        float2 sliced = renderer.MeasureText(default, 13f, "Hello, brutal-ui"u8[..5], 0f);
        float2 literal = renderer.MeasureText(default, 13f, "Hello"u8, 0f);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Text(pos, default, 13f, White, "Hello, brutal-ui"u8[..5], 0f);
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(Describe(sliced) + " " + dump);
        Assert.Equal(literal, sliced);
        Assert.Equal(new float2(35, 13), sliced);
        Assert.StartsWith("cmd 0 clip 0,0,400,300 tex atlas elems " +
            (5 * IndicesPerGlyph).ToString(CultureInfo.InvariantCulture) + " ", dump);
    }

    // ---- The demo ----------------------------------------------------------------------------------------------

    /// <summary>
    /// The demo of phase 2: three measures of <c>Hello, brutal-ui</c>, then the draw of the 13 px text at (10, 10).
    /// </summary>
    [Fact]
    public void MeasuresAndDrawsText()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        float2 pos = new(10, 10);

        float2 small = renderer.MeasureText(default, 13f, Hello, 0f);
        float2 large = renderer.MeasureText(default, 26f, Hello, 0f);
        float2 wrapped = renderer.MeasureText(default, 13f, Hello, 70f);
        PrintMeasure(13f, 0f, small);
        PrintMeasure(26f, 0f, large);
        PrintMeasure(13f, 70f, wrapped);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Text(pos, default, 13f, White, Hello, 0f);
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);
        VertexBounds bounds = ReadVertexBounds();

        Assert.Equal(13f, small.Y);
        Assert.Equal(26f, large.Y);
        Assert.Equal(26f, wrapped.Y);
        Assert.True(large.X > small.X);
        Assert.True(wrapped.X <= 70f);
        Assert.DoesNotContain('\n', dump);
        Assert.Equal("cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 11,12-119,23 colors #FFFFFFFF", dump);
        AssertInsideBox(bounds, pos, small);
    }

    // ---- Helpers -----------------------------------------------------------------------------------------------

    private static ImGuiRenderer CreateRenderer()
    {
        return new ImGuiRenderer(new ImGuiRenderer.CreateInfo
        {
            Fonts = new ImGuiFontService(),
            Textures = new ImGuiTextureService(),
        });
    }

    /// <summary>Measures the text of the demo at 13, at 26, and at 13 with a wrap width of 70.</summary>
    private static float2[] MeasureThree(ImGuiRenderer renderer)
    {
        return
        [
            renderer.MeasureText(default, 13f, Hello, 0f),
            renderer.MeasureText(default, 26f, Hello, 0f),
            renderer.MeasureText(default, 13f, Hello, 70f),
        ];
    }

    /// <summary>
    /// Adds the ImGui default font with a size and one extra pixel of advance for each glyph. It sets the defaults of
    /// the native <c>ImFontConfig</c> constructor, because the binding has no constructor call.
    /// </summary>
    private static ImFontPtr AddWideFont(float sizePixels)
    {
        ImFontConfig config = default;
        config.FontDataOwnedByAtlas = true;
        config.GlyphMaxAdvanceX = float.MaxValue;
        config.RasterizerMultiply = 1f;
        config.RasterizerDensity = 1f;
        config.SizePixels = sizePixels;
        config.GlyphExtraAdvanceX = 1f;

        ImFont* font = Gui.PInvoke.ImFontAtlas_AddFontDefault(Gui.GetIO().Fonts.Ptr, &config);
        Assert.True(font != null);
        return new ImFontPtr(font);
    }

    private static void PrintMeasure(float size, float wrapWidth, float2 measured)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(
            "measure " + HeadlessImGui.Format(size) + " wrap " + HeadlessImGui.Format(wrapWidth) + " -> " +
            Describe(measured));
    }

    private static string Describe(float2 size) =>
        HeadlessImGui.Format(size.X) + "x" + HeadlessImGui.Format(size.Y);

    /// <summary>
    /// Asserts that each vertex lies inside the box from <paramref name="pos"/> to <paramref name="pos"/> plus
    /// <paramref name="measured"/>, grown by 1 pixel on each side.
    /// </summary>
    private static void AssertInsideBox(VertexBounds bounds, float2 pos, float2 measured)
    {
        Assert.True(bounds.Count > 0, "The list holds no vertex.");
        float2 min = pos - One;
        float2 max = pos + measured + One;
        Assert.True(
            bounds.Min.X >= min.X && bounds.Min.Y >= min.Y && bounds.Max.X <= max.X && bounds.Max.Y <= max.Y,
            "The vertices " + Describe(bounds.Min) + "-" + Describe(bounds.Max) + " are not inside the box " +
            Describe(min) + "-" + Describe(max) + ".");
    }

    /// <summary>
    /// Reads the bounds of each vertex of the raw vertex buffer of the background list, after the ImGui frame.
    /// </summary>
    private static VertexBounds ReadVertexBounds()
    {
        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        ref ImVector<ImDrawVert> vertices = ref list.VtxBuffer;
        float2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        float2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < vertices.Count; i++)
        {
            float2 p = vertices[i].pos;
            min = new float2(MathF.Min(min.X, p.X), MathF.Min(min.Y, p.Y));
            max = new float2(MathF.Max(max.X, p.X), MathF.Max(max.Y, p.Y));
        }

        return new VertexBounds(vertices.Count, min, max);
    }

    /// <summary>The count and the bounds of the vertices of a list.</summary>
    private readonly record struct VertexBounds(int Count, float2 Min, float2 Max);
}
