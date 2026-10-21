using System;
using System.Globalization;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// Tests of <see cref="Label"/> with the real ImGui measure: the measure and the draw of a wrapped label give the same
/// lines.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ImGuiRenderer"/> of a <see cref="HeadlessImGui"/> harness (400 by 300) is the measurer and the
/// renderer. Each test runs <see cref="UiContext.Update(float)"/> before <see cref="HeadlessImGui.BeginFrame"/>, so the
/// measure runs outside the ImGui frame.
/// </para>
/// <para>
/// The default font of the harness is the ImGui default font at size 13. Each glyph is 7 pixels wide. Thus
/// <c>Hello, brutal-ui</c> is 112 by 13 with no wrap. At a wrap width of 70 it has two lines, <c>Hello,</c> and
/// <c>brutal-ui</c>, and the widest line is 63 pixels.
/// </para>
/// <para>
/// The label of the phase 2 tests has no padding and no border, so its content box is its rect. The draw wraps at the
/// width of the rect. The percent padding tests compute the content box from the style values, because the content
/// box of <see cref="LayoutNode"/> is internal.
/// </para>
/// </remarks>
public sealed unsafe class ImGuiLabelTests
{
    /// <summary>The text of the label.</summary>
    private const string Hello = "Hello, brutal-ui";

    /// <summary>The size of the label, the size of the default font of the harness.</summary>
    private const float Size = 13f;

    /// <summary>The glyph count of <see cref="Hello"/>. The blank makes no glyph.</summary>
    private const int HelloGlyphCount = 15;

    /// <summary>The vertex count of one glyph quad.</summary>
    private const int VerticesPerGlyph = 4;

    /// <summary>The float error of the fringe. A bound check allows this distance.</summary>
    private const float Slack = 0.01f;

    /// <summary>The first width of the sweep.</summary>
    private const int FirstWidth = 40;

    /// <summary>The last width of the sweep.</summary>
    private const int LastWidth = 200;

    /// <summary>
    /// The dump of <see cref="Hello"/> at (0, 0) with a wrap width of 63 or 70: the lines <c>Hello,</c> and
    /// <c>brutal-ui</c>.
    /// </summary>
    private const string WrappedList = "cmd 0 clip 0,0,400,300 tex atlas elems 90 verts 1,2-60,24 colors #FFFFFFFF";

    /// <summary>The size of the line count table of the sweep. A count of this value or more goes in the last slot.</summary>
    private const int LineCountSlots = 8;

    /// <summary>The padding of each edge in the percent padding tests, in percent of the root width.</summary>
    private const float PaddingPercent = 5f;

    private static readonly Rgba8 White = new(255, 255, 255);

    /// <summary>
    /// The demo: a label with <see cref="Align.FlexStart"/> in a root of width 70 wraps to two lines and takes the
    /// width of its widest line. Each glyph vertex lies inside the rect, grown by 1 pixel.
    /// </summary>
    [Fact]
    public void WrapsALabelAtItsWidth()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        UiContext context = CreateContext(renderer, 70f);
        Label label = AddLabel(context, Align.FlexStart);

        float4 rect = UpdateAndDraw(imgui, context, renderer, label);
        VertexBounds bounds = ReadVertexBounds();
        string dump = imgui.DescribeBackgroundList();
        Print("label rect " + FormatRect(rect));
        Print(dump);

        Assert.Equal("0,0,63,26", FormatRect(rect));
        Assert.Equal(HelloGlyphCount * VerticesPerGlyph, bounds.Count);
        Assert.Equal(2f, DrawnLines(bounds, rect));
        AssertInsideRect(bounds, rect);
        Assert.Equal(WrappedList, dump);
    }

    /// <summary>
    /// A stretched label in a root of width 70 gets the exact width 70. The measure and the draw both wrap at 70, and
    /// the glyphs lie inside the rect, grown by 1 pixel.
    /// </summary>
    [Fact]
    public void WrapsAStretchedLabelAtTheBoxWidth()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        UiContext context = CreateContext(renderer, 70f);
        Label label = AddLabel(context, Align.Stretch);

        float4 rect = UpdateAndDraw(imgui, context, renderer, label);
        VertexBounds bounds = ReadVertexBounds();
        string dump = imgui.DescribeBackgroundList();
        Print("label rect " + FormatRect(rect));
        Print(dump);

        Assert.Equal("0,0,70,26", FormatRect(rect));
        Assert.Equal(HelloGlyphCount * VerticesPerGlyph, bounds.Count);
        Assert.Equal(2f, DrawnLines(bounds, rect));
        AssertInsideRect(bounds, rect);
        Assert.Equal(WrappedList, dump);
    }

    /// <summary>A text that fits the root of width 200 keeps one line and the width of its text.</summary>
    [Fact]
    public void KeepsOneLineWhenTheTextFits()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        UiContext context = CreateContext(renderer, 200f);
        Label label = AddLabel(context, Align.FlexStart);

        float4 rect = UpdateAndDraw(imgui, context, renderer, label);
        VertexBounds bounds = ReadVertexBounds();
        Print("label rect " + FormatRect(rect));

        Assert.Equal("0,0,112,13", FormatRect(rect));
        Assert.Equal(HelloGlyphCount * VerticesPerGlyph, bounds.Count);
        Assert.Equal(1f, DrawnLines(bounds, rect));
        AssertInsideRect(bounds, rect);
    }

    /// <summary>
    /// For each whole width from 40 to 200, a label with <see cref="Align.FlexStart"/> in a root of that width has a
    /// rect height equal to the vertex height of the draw, rounded up to whole lines. Each glyph vertex also lies
    /// inside the rect, grown by 1 pixel. The test prints the first width that fails, if any.
    /// </summary>
    [Fact]
    public void KeepsTheLinesForEachWidthFromFortyToTwoHundred()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        int firstFailure = -1;
        int failures = 0;
        int checkedWidths = 0;
        int[] widthsPerLineCount = new int[LineCountSlots];

        for (int width = FirstWidth; width <= LastWidth; width++)
        {
            UiContext context = CreateContext(renderer, width);
            Label label = AddLabel(context, Align.FlexStart);

            float4 rect = UpdateAndDraw(imgui, context, renderer, label);
            VertexBounds bounds = ReadVertexBounds();
            float lines = DrawnLines(bounds, rect);
            checkedWidths++;
            widthsPerLineCount[Math.Clamp((int)lines, 0, LineCountSlots - 1)]++;

            if (lines * Size != rect.W
                || bounds.Count != HelloGlyphCount * VerticesPerGlyph
                || !IsInsideRect(bounds, rect))
            {
                failures++;
                if (firstFailure < 0)
                {
                    firstFailure = width;
                }

                Print(string.Create(
                    CultureInfo.InvariantCulture,
                    $"width {width}: rect {FormatRect(rect)}, drawn lines {lines}, verts {bounds.Count} ") +
                    Describe(bounds.Min) + "-" + Describe(bounds.Max));
            }
        }

        Print(firstFailure < 0
            ? string.Create(CultureInfo.InvariantCulture, $"checked {checkedWidths} widths, first failed width none")
            : string.Create(
                CultureInfo.InvariantCulture,
                $"checked {checkedWidths} widths, first failed width {firstFailure}, failed widths {failures}"));
        for (int count = 0; count < LineCountSlots; count++)
        {
            if (widthsPerLineCount[count] > 0)
            {
                Print(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{count} drawn lines: {widthsPerLineCount[count]} widths"));
            }
        }

        // The sweep covers the cut inside a word, the break after a mark or a blank, and one line.
        Assert.True(widthsPerLineCount[1] > 0, "No width gives one line.");
        Assert.True(widthsPerLineCount[2] > 0, "No width gives two lines.");
        Assert.True(widthsPerLineCount[3] > 0, "No width gives three lines.");
        Assert.Equal(LastWidth - FirstWidth + 1, checkedWidths);
        Assert.Equal(-1, firstFailure);
    }

    /// <summary>
    /// A label with a padding of 5 % in a root of width 150 gets 7.5 pixels on each edge. The content box is 112 by 13
    /// at (7.5, 7.5), so the text fits its content width and keeps one line. Each glyph vertex lies inside the content
    /// box, grown by 1 pixel.
    /// </summary>
    [Fact]
    public void WrapsALabelWithAPercentPaddingAtItsWidth()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        UiContext context = CreateContext(renderer, 150f);
        Label label = AddLabel(context, Align.FlexStart, PaddingPercent);

        float4 rect = UpdateAndDraw(imgui, context, renderer, label);
        VertexBounds bounds = ReadVertexBounds();
        float4 content = ContentBox(rect, 150f);
        Print("label rect " + FormatRect(rect) + ", content box " + FormatRect(content));
        Print(imgui.DescribeBackgroundList());

        // 5 % of 150 is 7.5. The border box is the text (112 by 13) plus 15 on each axis.
        Assert.Equal("0,0,127,28", FormatRect(rect));
        Assert.Equal("7.5,7.5,112,13", FormatRect(content));
        Assert.Equal(HelloGlyphCount * VerticesPerGlyph, bounds.Count);
        Assert.Equal(1f, DrawnLines(bounds, content));
        AssertInsideRect(bounds, content);
    }

    /// <summary>
    /// For each whole root width from 40 to 200, a label with a padding of 5 % has a content height equal to the
    /// vertex height of the draw, rounded up to whole lines. Each glyph vertex lies inside the content box, grown by 1
    /// pixel. The padding of most widths is not a whole number.
    /// </summary>
    /// <param name="align">The <see cref="ComputedStyle.AlignItems"/> of the root.</param>
    [Theory]
    [InlineData(Align.FlexStart)]
    [InlineData(Align.Stretch)]
    public void KeepsTheLinesWithAPercentPaddingForEachWidth(Align align)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        int failures = 0;
        int wrappedWidths = 0;

        for (int width = FirstWidth; width <= LastWidth; width++)
        {
            UiContext context = CreateContext(renderer, width);
            Label label = AddLabel(context, align, PaddingPercent);

            float4 rect = UpdateAndDraw(imgui, context, renderer, label);
            VertexBounds bounds = ReadVertexBounds();
            float4 content = ContentBox(rect, width);
            float lines = DrawnLines(bounds, content);
            if (lines > 1f)
            {
                wrappedWidths++;
            }

            if (MathF.Abs((lines * Size) - content.W) > Slack
                || bounds.Count != HelloGlyphCount * VerticesPerGlyph
                || !IsInsideRect(bounds, content))
            {
                failures++;
                Print(string.Create(
                    CultureInfo.InvariantCulture,
                    $"width {width}: content {FormatRect(content)}, drawn lines {lines}, verts {bounds.Count} ") +
                    Describe(bounds.Min) + "-" + Describe(bounds.Max));
            }
        }

        Print(string.Create(
            CultureInfo.InvariantCulture,
            $"{align}: {LastWidth - FirstWidth + 1} widths, {wrappedWidths} wrapped, {failures} failed"));
        Assert.True(wrappedWidths > 0, "No width wraps the text.");
        Assert.Equal(0, failures);
    }

    // ---- Helpers -----------------------------------------------------------------------------------------------

    /// <summary>Creates a renderer with a new font service and a new texture service.</summary>
    /// <returns>The renderer.</returns>
    private static ImGuiRenderer CreateRenderer()
    {
        return new ImGuiRenderer(new ImGuiRenderer.CreateInfo
        {
            Fonts = new ImGuiFontService(),
            Textures = new ImGuiTextureService(),
        });
    }

    /// <summary>Creates a context with the renderer as its measurer and a viewport of the width by 300.</summary>
    /// <param name="measurer">The measurer.</param>
    /// <param name="width">The viewport width, the width of the root.</param>
    /// <returns>The context.</returns>
    private static UiContext CreateContext(ImGuiRenderer measurer, float width)
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = measurer }) { ViewportSize = new float2(width, 300) };
    }

    /// <summary>Adds a root and a white label of size 13 with <see cref="Hello"/> to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="align">The <see cref="ComputedStyle.AlignItems"/> of the root.</param>
    /// <returns>The label.</returns>
    private static Label AddLabel(UiContext context, Align align)
    {
        Element root = new();
        root.Inline.AlignItems = align;
        context.AddRoot(root);
        Label label = new(new Label.CreateInfo { Text = Hello });
        label.Inline.Color = White;
        label.Inline.FontSize = Size;
        root.Children.Add(label);
        return label;
    }

    /// <summary>Adds a root and a white label of size 13 with <see cref="Hello"/> and a percent padding to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="align">The <see cref="ComputedStyle.AlignItems"/> of the root.</param>
    /// <param name="paddingPercent">The padding of each edge, in percent of the root width.</param>
    /// <returns>The label.</returns>
    private static Label AddLabel(UiContext context, Align align, float paddingPercent)
    {
        Label label = AddLabel(context, align);
        label.Inline.Padding = LengthEdges.All(Length.Percent(paddingPercent));
        return label;
    }

    /// <summary>
    /// Gives the content box of a label with a padding of <see cref="PaddingPercent"/> in a root with no padding. The
    /// padding of each edge is the percent of the root width. The content width is the width minus the sum of the
    /// two edges.
    /// </summary>
    /// <param name="rect">The rect of the label.</param>
    /// <param name="rootWidth">The width of the root.</param>
    /// <returns>The content box: x, y, width, and height.</returns>
    private static float4 ContentBox(in float4 rect, float rootWidth)
    {
        float padding = rootWidth * (PaddingPercent / 100f);
        float sum = padding + padding;
        return new float4(rect.X + padding, rect.Y + padding, rect.Z - sum, rect.W - sum);
    }

    /// <summary>
    /// Runs the update outside the ImGui frame, then draws the context in one ImGui frame.
    /// </summary>
    /// <param name="imgui">The harness.</param>
    /// <param name="context">The context.</param>
    /// <param name="renderer">The renderer.</param>
    /// <param name="label">The label.</param>
    /// <returns>The rect of the label.</returns>
    private static float4 UpdateAndDraw(HeadlessImGui imgui, UiContext context, ImGuiRenderer renderer, Label label)
    {
        Assert.False(imgui.FrameOpen);
        context.Update(0f);
        imgui.BeginFrame();
        context.Draw(renderer);
        imgui.EndFrame();
        return label.Layout.Rect;
    }

    /// <summary>
    /// Gives the line count of the draw: the vertex height below the top of the rect, rounded up to whole lines.
    /// </summary>
    /// <param name="bounds">The vertex bounds of the draw.</param>
    /// <param name="rect">The rect of the label.</param>
    /// <returns>The line count. A draw with no vertex gives 0.</returns>
    private static float DrawnLines(VertexBounds bounds, in float4 rect)
    {
        if (bounds.Count == 0)
        {
            return 0f;
        }

        return MathF.Ceiling((bounds.Max.Y - rect.Y - Slack) / Size);
    }

    /// <summary>Tells whether each vertex lies inside the rect, grown by 1 pixel and the slack on each side.</summary>
    /// <param name="bounds">The vertex bounds.</param>
    /// <param name="rect">The rect: x, y, width, and height.</param>
    /// <returns><see langword="true"/> when the list holds a vertex and each vertex lies inside.</returns>
    private static bool IsInsideRect(VertexBounds bounds, in float4 rect)
    {
        const float Grow = 1f + Slack;
        return bounds.Count > 0
            && bounds.Min.X >= rect.X - Grow
            && bounds.Min.Y >= rect.Y - Grow
            && bounds.Max.X <= rect.X + rect.Z + Grow
            && bounds.Max.Y <= rect.Y + rect.W + Grow;
    }

    /// <summary>Asserts that each vertex lies inside the rect, grown by 1 pixel and the slack on each side.</summary>
    /// <param name="bounds">The vertex bounds.</param>
    /// <param name="rect">The rect: x, y, width, and height.</param>
    private static void AssertInsideRect(VertexBounds bounds, in float4 rect)
    {
        Assert.True(
            IsInsideRect(bounds, rect),
            "The vertices " + Describe(bounds.Min) + "-" + Describe(bounds.Max) + " are not inside the rect " +
            FormatRect(rect) + " grown by 1 pixel.");
    }

    /// <summary>
    /// Reads the bounds of each vertex of the raw vertex buffer of the background list, after the ImGui frame.
    /// </summary>
    /// <returns>The count and the bounds of the vertices.</returns>
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

    /// <summary>Formats a rect as <c>x,y,w,h</c> with the invariant culture.</summary>
    /// <param name="rect">The rect.</param>
    /// <returns>The text.</returns>
    private static string FormatRect(in float4 rect)
    {
        return HeadlessImGui.Format(rect.X) + "," + HeadlessImGui.Format(rect.Y) + "," +
            HeadlessImGui.Format(rect.Z) + "," + HeadlessImGui.Format(rect.W);
    }

    /// <summary>Formats a point as <c>x,y</c> with the invariant culture.</summary>
    /// <param name="point">The point.</param>
    /// <returns>The text.</returns>
    private static string Describe(float2 point) =>
        HeadlessImGui.Format(point.X) + "," + HeadlessImGui.Format(point.Y);

    /// <summary>Writes one line to the output of the current test.</summary>
    /// <param name="line">The line.</param>
    private static void Print(string line)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(line);
    }

    /// <summary>The count and the bounds of the vertices of a list.</summary>
    private readonly record struct VertexBounds(int Count, float2 Min, float2 Max);
}
