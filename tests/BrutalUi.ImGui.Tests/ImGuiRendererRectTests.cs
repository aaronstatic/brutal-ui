using System;
using System.Globalization;
using System.Text;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// Tests of the frame, the clips, and the square fills of <see cref="ImGuiRenderer"/> on a headless ImGui context.
/// </summary>
/// <remarks>
/// Each test reads the background list of the main viewport after the ImGui frame. A second pass in the same ImGui
/// frame adds to the list, so these tests assert on the last command.
/// </remarks>
public sealed class ImGuiRendererRectTests
{
    private const string EmptyList = "cmd 0 clip 0,0,400,300 tex atlas elems 0 verts none colors";
    private const string RedRectUnderTheViewportClip =
        "cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 10,10-30,30 colors #FF0000FF";

    /// <summary>
    /// The size of the clip stack of a background list with no open clip of the renderer. ImGui pushes the viewport
    /// clip when it resets the list at the first access of a frame.
    /// </summary>
    private const int ViewportClipCount = 1;

    private static readonly Rgba8 Red = new(255, 0, 0);
    private static readonly Rgba8 Green = new(0, 255, 0);
    private static readonly Rgba8 PanelFill = new(10, 20, 30);

    /// <summary>The RGB part of a vertex color (R in the low byte), for <c>PanelFill</c>.</summary>
    private const uint PanelFillRgb = 0x1E140Au;

    /// <summary>The RGB part of a vertex color (R in the low byte), for <c>Red</c>.</summary>
    private const uint RedRgb = 0x0000FFu;

    /// <summary>
    /// The demo of phase 1: the tree of the renderer-interface demo through <see cref="UiContext.Draw(IUiRenderer)"/>.
    /// The root fill draws under the viewport clip. The two child fills draw under the padding box clip.
    /// </summary>
    [Fact]
    public void DrawsThreeElementTreeWithOneClip()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        UiContext context = new() { ViewportSize = new float2(400, 300) };
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.Width = 200;
        root.Style.Height = 100;
        root.Style.Padding = LengthEdges.All(10);
        root.Style.BorderWidth = LengthEdges.All(2);
        root.Style.Overflow = Overflow.Hidden;
        root.Style.BackgroundColor = new Rgba8(10, 20, 30);
        context.AddRoot(root);
        AddBox(root, 50, 20, Red);
        AddBox(root, 300, 20, Green);

        context.Update(0f);
        imgui.BeginFrame();
        context.Draw(renderer);
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 0,0-200,100 colors #0A141EFF\n" +
            "cmd 1 clip 2,2,198,98 tex atlas elems 12 verts 12,12-312,52 colors #FF0000FF #00FF00FF",
            dump);
    }

    /// <summary>The renderer adds the position of the main viewport to each clip and each fill.</summary>
    [Fact]
    public void AddsTheMainViewportPosition()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        Gui.GetMainViewport().Pos = new float2(100, 50);
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 196, 96));
        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.PopClip();
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        Assert.Equal("cmd 0 clip 102,52,298,148 tex atlas elems 6 verts 110,60-130,80 colors #FF0000FF", dump);
    }

    /// <summary>Each draw call before <see cref="ImGuiRenderer.Begin"/> and after the end of a frame throws.</summary>
    [Fact]
    public void ThrowsForADrawCallBeforeBegin()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        AssertEachDrawCallThrows(renderer);

        renderer.Begin(new float2(400, 300), 1f);
        renderer.End();
        AssertEachDrawCallThrows(renderer);
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>A pop with no open clip throws before the native call, so the list stays balanced.</summary>
    [Fact]
    public void ThrowsForAPopClipWithNoPush()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        Assert.Throws<InvalidOperationException>(renderer.PopClip);

        renderer.PushClip(new float4(2, 2, 50, 50));
        renderer.PopClip();
        Assert.Throws<InvalidOperationException>(renderer.PopClip);

        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.End();
        imgui.EndFrame();

        Assert.EndsWith(RedRectUnderTheViewportClip, imgui.DescribeBackgroundList());
    }

    /// <summary>A null service throws <see cref="ArgumentNullException"/>.</summary>
    [Fact]
    public void ThrowsForANullService()
    {
        Assert.Throws<ArgumentNullException>(() => new ImGuiRenderer(new ImGuiRenderer.CreateInfo
        {
            Fonts = null!,
            Textures = new ImGuiTextureService(),
        }));
        Assert.Throws<ArgumentNullException>(() => new ImGuiRenderer(new ImGuiRenderer.CreateInfo
        {
            Fonts = new ImGuiFontService(),
            Textures = null!,
        }));

        ImGuiFontService fonts = new();
        ImGuiTextureService textures = new();
        ImGuiRenderer renderer = new(new ImGuiRenderer.CreateInfo { Fonts = fonts, Textures = textures });
        Assert.Same(fonts, renderer.Fonts);
        Assert.Same(textures, renderer.Textures);
    }

    /// <summary>
    /// <see cref="ImGuiRenderer.End"/> pops a clip with no <see cref="ImGuiRenderer.PopClip"/>, so a later frame in
    /// the same ImGui frame draws under the viewport clip.
    /// </summary>
    [Fact]
    public void EndPopsAnOpenClip()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 50, 50));
        renderer.End();
        Assert.Equal(ViewportClipCount, ClipStackCount());

        renderer.Begin(new float2(400, 300), 1f);
        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);
        Assert.EndsWith(RedRectUnderTheViewportClip, dump);
    }

    /// <summary>
    /// <see cref="ImGuiRenderer.Begin"/> pops the clips of an earlier frame with no <see cref="ImGuiRenderer.End"/> in
    /// the same ImGui frame.
    /// </summary>
    [Fact]
    public void BeginClearsTheClipsOfAnAbortedFrame()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 50, 50));
        renderer.PushClip(new float4(4, 4, 20, 20));

        renderer.Begin(new float2(400, 300), 1f);
        Assert.Equal(ViewportClipCount, ClipStackCount());
        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);
        Assert.EndsWith(RedRectUnderTheViewportClip, dump);
    }

    /// <summary>
    /// A frame with no <see cref="ImGuiRenderer.End"/> in an earlier ImGui frame leaves nothing to pop: ImGui resets
    /// the list in the new frame. <see cref="ImGuiRenderer.Begin"/> pops nothing, so the list stays valid.
    /// </summary>
    [Fact]
    public void BeginInANewImGuiFramePopsNothing()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 50, 50));
        imgui.EndFrame();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        Assert.Equal(ViewportClipCount, ClipStackCount());
        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(RedRectUnderTheViewportClip, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// An <see cref="ImGuiRenderer.End"/> in a later ImGui frame pops nothing, because ImGui reset the list.
    /// </summary>
    [Fact]
    public void EndInALaterImGuiFramePopsNothing()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 50, 50));
        imgui.EndFrame();

        imgui.BeginFrame();
        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        renderer.End();
        int clipCount = list._ClipRectStack.Count;
        Assert.Equal(ViewportClipCount, clipCount);
        Assert.Throws<InvalidOperationException>(() => DrawRect(renderer, new float4(10, 10, 20, 20), Red));

        renderer.Begin(new float2(400, 300), 1f);
        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(RedRectUnderTheViewportClip, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// A <see cref="ImGuiRenderer.PopClip"/> in a later ImGui frame with no <see cref="ImGuiRenderer.Begin"/> throws
    /// before the native call. ImGui reset the list, so the clip stack holds only the viewport clip, and a native pop
    /// writes out of bounds. The clip stack keeps its size.
    /// </summary>
    [Fact]
    public void PopClipInALaterImGuiFrameThrows()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 50, 50));
        renderer.PushClip(new float4(4, 4, 20, 20));
        imgui.EndFrame();

        imgui.BeginFrame();
        // The first access of the frame resets the list, as a draw of ImGui or of the game does.
        Assert.Equal(ViewportClipCount, ClipStackCount());
        Assert.Throws<InvalidOperationException>(renderer.PopClip);
        Assert.Equal(ViewportClipCount, ClipStackCount());

        // The throw comes before the renderer lowers its depth, so a second pop also meets the guard.
        Assert.Throws<InvalidOperationException>(renderer.PopClip);
        Assert.Equal(ViewportClipCount, ClipStackCount());

        renderer.Begin(new float2(400, 300), 1f);
        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(RedRectUnderTheViewportClip, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// The game pops the clip of the list by hand inside a frame. <see cref="ImGuiRenderer.End"/> then pops nothing
    /// and keeps the viewport clip, which the renderer did not push.
    /// </summary>
    [Fact]
    public unsafe void EndKeepsTheViewportClipWhenTheGamePoppedTheClipByHand()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 50, 50));
        Assert.Equal(ViewportClipCount + 1, ClipStackCount());

        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        Gui.PInvoke.ImDrawList_PopClipRect(list.Ptr);
        Assert.Equal(ViewportClipCount, ClipStackCount());

        renderer.End();
        Assert.Equal(ViewportClipCount, ClipStackCount());
        imgui.EndFrame();
    }

    /// <summary>
    /// The game pops the clip of the list by hand inside a frame. <see cref="ImGuiRenderer.Begin"/> then pops nothing
    /// and keeps the viewport clip, because the renderer did not push it. The list stays valid for a new frame.
    /// </summary>
    [Fact]
    public unsafe void BeginKeepsTheViewportClipWhenTheGamePoppedTheClipByHand()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(2, 2, 50, 50));
        Assert.Equal(ViewportClipCount + 1, ClipStackCount());

        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        Gui.PInvoke.ImDrawList_PopClipRect(list.Ptr);
        Assert.Equal(ViewportClipCount, ClipStackCount());

        renderer.Begin(new float2(400, 300), 1f);
        Assert.Equal(ViewportClipCount, ClipStackCount());
        DrawRect(renderer, new float4(10, 10, 20, 20), Red);
        renderer.End();
        Assert.Equal(ViewportClipCount, ClipStackCount());
        imgui.EndFrame();

        Assert.Equal(RedRectUnderTheViewportClip, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// A clip stays inside the parent clip. A clip that is larger than the viewport gives the viewport clip, and a
    /// clip that is larger than the first clip gives the first clip.
    /// </summary>
    [Fact]
    public void PushClipStaysInsideTheParentClip()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);

        // A clip that is larger than the viewport.
        renderer.PushClip(new float4(-50, -50, 1000, 1000));
        DrawRect(renderer, new float4(20, 20, 20, 20), Red);
        renderer.PopClip();

        // A second clip that is larger than the first clip.
        renderer.PushClip(new float4(10, 10, 100, 100));
        renderer.PushClip(new float4(0, 0, 300, 300));
        Assert.Equal(ViewportClipCount + 2, ClipStackCount());
        DrawRect(renderer, new float4(20, 20, 20, 20), Red);
        renderer.PopClip();
        renderer.PopClip();
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);
        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 20,20-40,40 colors #FF0000FF\n" +
            "cmd 1 clip 10,10,110,110 tex atlas elems 6 verts 20,20-40,40 colors #FF0000FF",
            dump);
    }

    /// <summary>
    /// A clip rect with a <c>NaN</c> component gives a clip with an area of 0 and no <c>NaN</c>. The push still
    /// happens, so the stack stays balanced. A <c>NaN</c> coordinate of the min point becomes the origin.
    /// </summary>
    [Theory]
    [InlineData(float.NaN, 10f, 50f, 50f, "0,10,0,10")]
    [InlineData(10f, float.NaN, 50f, 50f, "10,0,10,0")]
    [InlineData(10f, 10f, float.NaN, 50f, "10,10,10,10")]
    [InlineData(10f, 10f, 50f, float.NaN, "10,10,10,10")]
    public void PushClipWithANaNComponentClipsEverything(
        float x, float y, float width, float height, string expectedClip)
    {
        AssertClipHasNoArea(new float4(x, y, width, height), expectedClip);
    }

    /// <summary>
    /// A clip rect with an infinite component, or with an overflow of <c>x + width</c>, gives a clip with an area of
    /// 0. ImGui keeps an infinite min in its intersect, and <c>-Infinity + Infinity</c> gives <c>NaN</c>.
    /// </summary>
    [Theory]
    [InlineData(float.PositiveInfinity, 10f, 50f, 50f, "0,10,0,10")]
    [InlineData(10f, float.PositiveInfinity, 50f, 50f, "10,0,10,0")]
    [InlineData(float.NegativeInfinity, 10f, 50f, 50f, "0,10,0,10")]
    [InlineData(10f, float.NegativeInfinity, 50f, 50f, "10,0,10,0")]
    [InlineData(10f, 10f, float.PositiveInfinity, 50f, "10,10,10,10")]
    [InlineData(10f, 10f, 50f, float.PositiveInfinity, "10,10,10,10")]
    [InlineData(10f, 10f, float.NegativeInfinity, 50f, "10,10,10,10")]
    [InlineData(float.NegativeInfinity, 10f, float.PositiveInfinity, 50f, "0,10,0,10")]
    [InlineData(3e38f, 0f, 3e38f, 10f, "3E+38,0,3E+38,0")]
    public void PushClipWithANonFiniteBoundClipsEverything(
        float x, float y, float width, float height, string expectedClip)
    {
        AssertClipHasNoArea(new float4(x, y, width, height), expectedClip);
    }

    /// <summary>
    /// A rect with no area, with a <c>NaN</c> or infinite component, or with an overflow of <c>x + width</c> draws
    /// nothing.
    /// </summary>
    [Theory]
    [InlineData(10f, 10f, 0f, 20f)]
    [InlineData(10f, 10f, 20f, 0f)]
    [InlineData(10f, 10f, -5f, 20f)]
    [InlineData(float.NaN, 10f, 20f, 20f)]
    [InlineData(10f, float.NaN, 20f, 20f)]
    [InlineData(10f, 10f, float.NaN, 20f)]
    [InlineData(10f, 10f, 20f, float.NaN)]
    [InlineData(10f, 10f, float.PositiveInfinity, 20f)]
    [InlineData(3e38f, 0f, 3e38f, 10f)]
    public void SkipsAnEmptyRect(float x, float y, float width, float height)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        DrawRect(renderer, new float4(x, y, width, height), Red);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>A fill with an alpha of 0 draws nothing.</summary>
    [Fact]
    public void SkipsATransparentFill()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        DrawRect(renderer, new float4(10, 10, 20, 20), new Rgba8(255, 0, 0, 0));
        DrawRect(renderer, new float4(10, 10, 20, 20), Rgba8.Transparent);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    // ---- Corners and borders ------------------------------------------------------------------------------------

    /// <summary>
    /// The demo of phase 4: a 200 by 100 rect with the top corners rounded by 8 and a border of 2. The test prints the
    /// resolved rules and the dump. Each vertex lies inside the rect grown by 1 pixel for the anti-alias fringe.
    /// </summary>
    [Fact]
    public void DrawsARoundedPanelWithABorder()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        Corners radius = new(8f, 8f, 0f, 0f);
        const float borderWidth = 2f;

        ImDrawFlags flags = ImGuiMapping.ResolveCorners(radius, 200f, 100f, out float rounding);
        TestContext.Current.TestOutputHelper?.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"corners {radius.TopLeft},{radius.TopRight},{radius.BottomRight},{radius.BottomLeft} -> rounding {rounding} flags {FormatFlags(flags)}"));
        TestContext.Current.TestOutputHelper?.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"border {borderWidth} -> inset {ImGuiMapping.BorderInset(borderWidth)} rounding {ImGuiMapping.BorderRounding(rounding, borderWidth)}"));

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(0, 0, 200, 100), PanelFill, radius, borderWidth, Red);
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);

        Assert.Equal(8f, rounding);
        Assert.Equal(ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersTopRight, flags);
        Assert.Equal(0.5f, ImGuiMapping.BorderInset(borderWidth));
        Assert.Equal(7f, ImGuiMapping.BorderRounding(rounding, borderWidth));
        Assert.StartsWith("cmd 0 clip 0,0,400,300 tex atlas elems ", dump);
        Assert.DoesNotContain('\n', dump);
        Assert.Contains("#0A141EFF", dump);
        Assert.Contains("#FF0000FF", dump);
        AssertColorInside(PanelFillRgb, new float4(-1, -1, 202, 102));
        AssertColorInside(RedRgb, new float4(-1, -1, 202, 102));
    }

    /// <summary>The vertices of the fill come before the vertices of the border in the vertex buffer.</summary>
    [Fact]
    public void DrawsTheBorderAfterTheFill()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(10, 10, 100, 60), PanelFill, Corners.All(6f), 3f, Red);
        renderer.End();
        imgui.EndFrame();

        int lastFill = LastIndexOfColor(PanelFillRgb);
        int firstBorder = FirstIndexOfColor(RedRgb);
        Assert.True(lastFill >= 0, "The list holds no fill vertex.");
        Assert.True(firstBorder > lastFill, "The first border vertex comes before the last fill vertex.");
    }

    /// <summary>
    /// Each border vertex lies inside the rect grown by 1 pixel for the anti-alias fringe, for a square and a rounded
    /// rect. The test prints the real bounds.
    /// </summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(8f)]
    public void KeepsTheBorderInsideTheRect(float cornerRadius)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        float4 rect = new(10, 10, 60, 60);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(rect, PanelFill, Corners.All(cornerRadius), 3f, Red);
        renderer.End();
        imgui.EndFrame();

        (float minX, float minY, float maxX, float maxY) = BoundsOfColor(RedRgb);
        TestContext.Current.TestOutputHelper?.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"radius {cornerRadius} border verts {minX},{minY}-{maxX},{maxY} rect 10,10-70,70"));
        AssertColorInside(RedRgb, new float4(9, 9, 62, 62));
        AssertColorInside(PanelFillRgb, new float4(9, 9, 62, 62));
    }

    /// <summary>
    /// A border below 1 pixel draws as 1 pixel (ImGui draws the thin stroke as 1 pixel). Each border vertex then lies
    /// inside the rect grown by 1 pixel, for a square and a rounded rect. The test prints the real bounds.
    /// </summary>
    [Theory]
    [InlineData(0.25f, 0f)]
    [InlineData(0.5f, 0f)]
    [InlineData(0.99f, 0f)]
    [InlineData(0.25f, 8f)]
    [InlineData(0.5f, 8f)]
    [InlineData(0.99f, 8f)]
    public void KeepsAThinBorderInsideTheRectGrownByOnePixel(float borderWidth, float cornerRadius)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        float4 rect = new(10, 10, 40, 20);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(rect, PanelFill, Corners.All(cornerRadius), borderWidth, Red);
        renderer.End();
        imgui.EndFrame();

        (float minX, float minY, float maxX, float maxY) = BoundsOfColor(RedRgb);
        TestContext.Current.TestOutputHelper?.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"border {borderWidth} radius {cornerRadius} verts {minX},{minY}-{maxX},{maxY} rect 10,10-50,30"));
        AssertColorInside(RedRgb, new float4(9, 9, 42, 22));
        AssertColorInside(PanelFillRgb, new float4(9, 9, 42, 22));
    }

    /// <summary>A border of half the shorter side or more draws one fill in the border color and no stroke.</summary>
    [Fact]
    public void FillsTheBoxForAWideBorder()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(10, 10, 20, 10), Green, default, 5f, Red);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 10,10-30,20 colors #FF0000FF",
            imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// A wide border with an alpha below 255 does not hide the fill, so the renderer draws the fill and then the border
    /// fill.
    /// </summary>
    [Fact]
    public void KeepsTheFillUnderATranslucentWideBorder()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(10, 10, 20, 10), Green, default, 5f, new Rgba8(255, 0, 0, 128));
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex atlas elems 12 verts 10,10-30,20 colors #00FF00FF #FF000080",
            imgui.DescribeBackgroundList());
    }

    /// <summary>A <c>NaN</c> radius is a square corner, so the rect keeps 6 elements as in phase 1.</summary>
    [Fact]
    public void DrawsASquareRectForANaNRadius()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(10, 10, 20, 20), Red, Corners.All(float.NaN), 0f, Rgba8.Transparent);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(RedRectUnderTheViewportClip, imgui.DescribeBackgroundList());
    }

    /// <summary>A rounded fill adds vertices, and the corner flags limit the rounding to the named corners.</summary>
    [Fact]
    public void RoundsOnlyTheNamedCorners()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(10, 10, 100, 60), Red, new Corners(10f, 0f, 0f, 0f), 0f, Rgba8.Transparent);
        renderer.End();
        imgui.EndFrame();

        Assert.True(ElementCount(0) > 6, "The rounded fill holds only 6 elements.");
        // The square corners keep a vertex within the fringe of the corner point. The round corner has none.
        Assert.True(HasVertexNear(110f, 10f));
        Assert.True(HasVertexNear(110f, 70f));
        Assert.True(HasVertexNear(10f, 70f));
        Assert.False(HasVertexNear(10f, 10f));
    }

    /// <summary>A rect with no fill and a border draws the stroke only.</summary>
    [Fact]
    public void DrawsABorderOnlyRect()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(10, 10, 60, 40), Rgba8.Transparent, default, 2f, Red);
        renderer.End();
        imgui.EndFrame();

        Assert.True(ElementCount(0) > 0, "The list holds no stroke.");
        Assert.Equal(1, CommandCount());
        (float minX, float minY, float maxX, float maxY) = BoundsOfColor(RedRgb);
        AssertColorInside(RedRgb, new float4(9, 9, 62, 42));
        Assert.Equal(AllVertexCount(), CountOfColor(RedRgb));
    }

    /// <summary>A border width of 0, below 0, <c>NaN</c>, or infinite, or a border alpha of 0, draws no border.</summary>
    [Theory]
    [InlineData(0f, 255)]
    [InlineData(-2f, 255)]
    [InlineData(float.NaN, 255)]
    [InlineData(float.PositiveInfinity, 255)]
    [InlineData(2f, 0)]
    public void DrawsNoBorderForAnInvalidBorder(float borderWidth, byte alpha)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Rect(new float4(10, 10, 20, 20), Green, default, borderWidth, new Rgba8(255, 0, 0, alpha));
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 10,10-30,30 colors #00FF00FF",
            imgui.DescribeBackgroundList());
    }

    // ---- Edge rules ----------------------------------------------------------------------------------------------

    /// <summary>With no current context, <see cref="ImGuiRenderer.Begin"/> throws before a native call.</summary>
    /// <remarks><c>MeasureThrowsWithNoContext</c> in <c>ImGuiRendererTextTests</c> covers the measure.</remarks>
    [Fact]
    public void BeginThrowsWithNoContext()
    {
        Assert.True(Gui.GetCurrentContext().IsNull());
        ImGuiRenderer renderer = CreateRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.Begin(new float2(400, 300), 1f));
        Assert.Throws<InvalidOperationException>(() => DrawRect(renderer, new float4(0, 0, 10, 10), Red));
    }

    /// <summary>An <see cref="ImGuiRenderer.End"/> with no open frame does nothing.</summary>
    [Fact]
    public void EndWithNoFrameDoesNothing()
    {
        ImGuiRenderer renderer = CreateRenderer();
        renderer.End();

        using HeadlessImGui imgui = new();
        imgui.BeginFrame();
        renderer.End();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.End();
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    private static ImGuiRenderer CreateRenderer()
    {
        return new ImGuiRenderer(new ImGuiRenderer.CreateInfo
        {
            Fonts = new ImGuiFontService(),
            Textures = new ImGuiTextureService(),
        });
    }

    private static void DrawRect(ImGuiRenderer renderer, float4 rect, Rgba8 color)
    {
        renderer.Rect(rect, color, default, 0f, Rgba8.Transparent);
    }

    private static void AssertEachDrawCallThrows(ImGuiRenderer renderer)
    {
        Assert.Throws<InvalidOperationException>(() => renderer.PushClip(new float4(0, 0, 10, 10)));
        Assert.Throws<InvalidOperationException>(renderer.PopClip);
        Assert.Throws<InvalidOperationException>(() => DrawRect(renderer, new float4(0, 0, 10, 10), Red));
        Assert.Throws<InvalidOperationException>(() => DrawRect(renderer, new float4(0, 0, 0, 0), Red));
    }

    /// <summary>
    /// Pushes a clip, draws a rect under it, and asserts that the command of the rect has the expected clip. The clip
    /// holds no <c>NaN</c> and no infinity, and its width or its height is 0.
    /// </summary>
    private static void AssertClipHasNoArea(float4 clipRect, string expectedClip)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(clipRect);
        Assert.Equal(ViewportClipCount + 1, ClipStackCount());
        DrawRect(renderer, new float4(20, 20, 20, 20), Red);
        renderer.PopClip();
        Assert.Equal(ViewportClipCount, ClipStackCount());
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);
        Assert.Equal($"cmd 0 clip {expectedClip} tex atlas elems 6 verts 20,20-40,40 colors #FF0000FF", dump);

        float4 clip = BackgroundList().CmdBuffer[0].ClipRect;
        Assert.True(
            float.IsFinite(clip.X) && float.IsFinite(clip.Y) && float.IsFinite(clip.Z) && float.IsFinite(clip.W),
            "The clip holds a value that is not finite.");
        Assert.True(clip.Z == clip.X || clip.W == clip.Y, "The clip has an area above 0.");
    }

    private static int ClipStackCount()
    {
        return Gui.GetBackgroundDrawList(Gui.GetMainViewport())._ClipRectStack.Count;
    }

    /// <summary>Gives the flag names in the form of the rule line, for example <c>TopLeft|TopRight</c>.</summary>
    private static string FormatFlags(ImDrawFlags flags)
    {
        if ((flags & (ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersTopRight
            | ImDrawFlags.RoundCornersBottomRight | ImDrawFlags.RoundCornersBottomLeft)) == 0)
        {
            return "None";
        }

        StringBuilder text = new();
        AppendFlag(text, flags, ImDrawFlags.RoundCornersTopLeft, "TopLeft");
        AppendFlag(text, flags, ImDrawFlags.RoundCornersTopRight, "TopRight");
        AppendFlag(text, flags, ImDrawFlags.RoundCornersBottomRight, "BottomRight");
        AppendFlag(text, flags, ImDrawFlags.RoundCornersBottomLeft, "BottomLeft");
        return text.ToString();
    }

    private static void AppendFlag(StringBuilder text, ImDrawFlags flags, ImDrawFlags flag, string name)
    {
        if ((flags & flag) == flag)
        {
            text.Append(text.Length > 0 ? "|" : string.Empty).Append(name);
        }
    }

    private static ImDrawListPtr BackgroundList()
    {
        return Gui.GetBackgroundDrawList(Gui.GetMainViewport());
    }

    private static int CommandCount()
    {
        return BackgroundList().CmdBuffer.Count;
    }

    private static uint ElementCount(int command)
    {
        return BackgroundList().CmdBuffer[command].ElemCount;
    }

    private static int AllVertexCount()
    {
        return BackgroundList().VtxBuffer.Count;
    }

    private static bool SameRgb(uint color, uint rgb)
    {
        return (color & 0xFFFFFFu) == rgb;
    }

    /// <summary>Gives the count of vertices with a color, ignoring the alpha of the fringe.</summary>
    private static int CountOfColor(uint rgb)
    {
        ref ImVector<ImDrawVert> vertices = ref BackgroundList().VtxBuffer;
        int count = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (SameRgb(vertices[i].col, rgb))
            {
                count++;
            }
        }

        return count;
    }

    private static int FirstIndexOfColor(uint rgb)
    {
        ref ImVector<ImDrawVert> vertices = ref BackgroundList().VtxBuffer;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (SameRgb(vertices[i].col, rgb))
            {
                return i;
            }
        }

        return -1;
    }

    private static int LastIndexOfColor(uint rgb)
    {
        ref ImVector<ImDrawVert> vertices = ref BackgroundList().VtxBuffer;
        for (int i = vertices.Count - 1; i >= 0; i--)
        {
            if (SameRgb(vertices[i].col, rgb))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Tells whether a vertex lies within 0.6 pixel of a point on each axis (the anti-alias fringe).</summary>
    private static bool HasVertexNear(float x, float y)
    {
        ref ImVector<ImDrawVert> vertices = ref BackgroundList().VtxBuffer;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (MathF.Abs(vertices[i].pos.X - x) <= 0.6f && MathF.Abs(vertices[i].pos.Y - y) <= 0.6f)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Gives the bounds of the vertices with a color. The method fails when no vertex has the color.</summary>
    private static (float MinX, float MinY, float MaxX, float MaxY) BoundsOfColor(uint rgb, int alpha = -1)
    {
        ref ImVector<ImDrawVert> vertices = ref BackgroundList().VtxBuffer;
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        int count = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            ImDrawVert vertex = vertices[i];
            if (!SameRgb(vertex.col, rgb) || (alpha >= 0 && vertex.col >> 24 != (uint)alpha))
            {
                continue;
            }

            count++;
            minX = MathF.Min(minX, vertex.pos.X);
            minY = MathF.Min(minY, vertex.pos.Y);
            maxX = MathF.Max(maxX, vertex.pos.X);
            maxY = MathF.Max(maxY, vertex.pos.Y);
        }

        Assert.True(count > 0, "The list holds no vertex of the color.");
        return (minX, minY, maxX, maxY);
    }

    /// <summary>The slack for the float error of the ImGui fringe math: ImGui puts a vertex 1.0003 pixels away.</summary>
    private const float FloatSlack = 0.01f;

    /// <summary>Asserts that each vertex with a color lies inside a rect (x, y, width, height).</summary>
    private static void AssertColorInside(uint rgb, float4 area)
    {
        (float minX, float minY, float maxX, float maxY) = BoundsOfColor(rgb);
        Assert.True(
            minX >= area.X - FloatSlack && minY >= area.Y - FloatSlack
                && maxX <= area.X + area.Z + FloatSlack && maxY <= area.Y + area.W + FloatSlack,
            string.Create(CultureInfo.InvariantCulture, $"Vertices {minX},{minY}-{maxX},{maxY} lie outside the area."));
    }

    private static Element AddBox(Element parent, float width, float height, Rgba8 color)
    {
        Element child = new();
        child.Style.Width = width;
        child.Style.Height = height;
        child.Style.BackgroundColor = color;
        parent.Children.Add(child);
        return child;
    }
}
