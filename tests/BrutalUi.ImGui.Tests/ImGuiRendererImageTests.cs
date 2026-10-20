using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>Tests of <see cref="ImGuiRenderer.Image"/> on a headless ImGui context.</summary>
/// <remarks>
/// Each texture of these tests is a reference with a texture id and no texture data, so the harness dump prints
/// <c>tex id N</c> for it. The service registers the reference only, so the tests need no GPU.
/// </remarks>
public sealed class ImGuiRendererImageTests
{
    private const string EmptyList = "cmd 0 clip 0,0,400,300 tex atlas elems 0 verts none colors";

    /// <summary>The texture id of the demo.</summary>
    private const int DemoId = 4660;

    private static readonly int2 DemoSize = new(64, 32);
    private static readonly float4 FullUv = new(0f, 0f, 1f, 1f);
    private static readonly float4 DemoRect = new(20f, 20f, 64f, 32f);
    private static readonly Rgba8 White = new(255, 255, 255);
    private static readonly Rgba8 Red = new(255, 0, 0);

    /// <summary>
    /// The demo of phase 3: an image with its texture, then a rect. The image command binds the texture of the
    /// handle, and the rect after it binds the font atlas again.
    /// </summary>
    [Fact]
    public void DrawsAnImageWithItsTexture()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);
        TestContext.Current.TestOutputHelper?.WriteLine($"size {texture.Size.X}x{texture.Size.Y}");

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, texture, FullUv, White, default);
        renderer.Rect(new float4(100, 20, 30, 30), Red, default, 0f, Rgba8.Transparent);
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);

        Assert.Equal(64, texture.Size.X);
        Assert.Equal(32, texture.Size.Y);
        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex id 4660 elems 6 verts 20,20-84,52 colors #FFFFFFFF\n" +
            "cmd 1 clip 0,0,400,300 tex atlas elems 6 verts 100,20-130,50 colors #FF0000FF",
            dump);
    }

    /// <summary>
    /// A rounded image draws more than the 6 elements of a square image, binds the texture of the handle, and keeps
    /// the UV rule: the UV of a vertex follows its position in the rect.
    /// </summary>
    [Fact]
    public void DrawsARoundedImage()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, texture, FullUv, White, Corners.All(8f));
        renderer.End();
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);

        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        ImDrawCmd command = list.CmdBuffer[0];
        int commandCount = list.CmdBuffer.Count;
        Assert.Equal(1, commandCount);
        Assert.True(command.ElemCount > 6, "The rounded image holds only 6 elements.");
        Assert.StartsWith("cmd 0 clip 0,0,400,300 tex id 4660 elems ", dump);

        // The rounded image has the anti-alias fringe of ImGui: each vertex lies within 0.5 pixel of the rect. The corner
        // point has no vertex, because the corner is round. The UV of a vertex follows its position in the rect.
        ref ImVector<ImDrawVert> vertices = ref list.VtxBuffer;
        for (int i = 0; i < vertices.Count; i++)
        {
            ImDrawVert vertex = vertices[i];
            Assert.InRange(vertex.pos.X, 19.49f, 84.51f);
            Assert.InRange(vertex.pos.Y, 19.49f, 52.51f);
            Assert.False(
                MathF.Abs(vertex.pos.X - 20f) < 1f && MathF.Abs(vertex.pos.Y - 20f) < 1f,
                "A vertex lies near the round corner.");
            // ImGui clamps the UV of a fringe vertex to the UV rect.
            Assert.Equal(Math.Clamp((vertex.pos.X - 20f) / 64f, 0f, 1f), vertex.uv.X, 0.001f);
            Assert.Equal(Math.Clamp((vertex.pos.Y - 20f) / 32f, 0f, 1f), vertex.uv.Y, 0.001f);
        }
    }

    /// <summary>An image with a radius of 0 or <c>NaN</c> keeps the 6 elements of the square image.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    [InlineData(-4f)]
    public void DrawsASquareImageForAnInvalidRadius(float radius)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, texture, FullUv, White, Corners.All(radius));
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex id 4660 elems 6 verts 20,20-84,52 colors #FFFFFFFF",
            imgui.DescribeBackgroundList());
    }

    /// <summary>The point (u0, v0) maps to the top-left vertex, and (u1, v1) maps to the bottom-right vertex.</summary>
    [Fact]
    public void MapsTheUvRectToTheCorners()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, texture, new float4(0.25f, 0.5f, 0.75f, 1f), White, default);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(new float2(0.25f, 0.5f), FindVertex(new float2(20, 20)).uv);
        Assert.Equal(new float2(0.75f, 1f), FindVertex(new float2(84, 52)).uv);
        Assert.Equal(new float2(0.75f, 0.5f), FindVertex(new float2(84, 20)).uv);
        Assert.Equal(new float2(0.25f, 1f), FindVertex(new float2(20, 52)).uv);
    }

    /// <summary>A flipped UV rect stays as the caller gave it, so the image draws mirrored.</summary>
    [Fact]
    public void KeepsAFlippedUvRect()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, texture, new float4(1f, 1f, 0f, 0f), White, default);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(new float2(1f, 1f), FindVertex(new float2(20, 20)).uv);
        Assert.Equal(new float2(0f, 0f), FindVertex(new float2(84, 52)).uv);
    }

    /// <summary>The tint is the color of each vertex of the image.</summary>
    [Fact]
    public void DrawsTheTint()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, texture, FullUv, new Rgba8(10, 20, 30, 128), default);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex id 4660 elems 6 verts 20,20-84,52 colors #0A141E80",
            imgui.DescribeBackgroundList());
    }

    /// <summary>The none handle draws nothing and does not throw, also with a service that holds no texture.</summary>
    [Fact]
    public void DrawsNothingForTheNoneHandle()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, default, FullUv, White, default);
        renderer.Image(DemoRect, new TextureHandle(0, DemoSize), FullUv, White, default);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>A tint alpha of 0 draws nothing.</summary>
    [Fact]
    public void DrawsNothingForAZeroTint()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(DemoRect, texture, FullUv, new Rgba8(255, 255, 255, 0), default);
        renderer.Image(DemoRect, texture, FullUv, Rgba8.Transparent, default);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// A rect with no area, with a <c>NaN</c> or infinite component, or with an overflow of <c>x + width</c> draws
    /// nothing.
    /// </summary>
    [Theory]
    [InlineData(20f, 20f, 0f, 32f)]
    [InlineData(20f, 20f, 64f, 0f)]
    [InlineData(20f, 20f, -64f, 32f)]
    [InlineData(float.NaN, 20f, 64f, 32f)]
    [InlineData(20f, float.NaN, 64f, 32f)]
    [InlineData(20f, 20f, float.NaN, 32f)]
    [InlineData(20f, 20f, 64f, float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity, 20f, 64f, 32f)]
    [InlineData(3e38f, 0f, 3e38f, 10f)]
    public void DrawsNothingForARectWithNoArea(float x, float y, float width, float height)
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        renderer.Image(new float4(x, y, width, height), texture, FullUv, White, default);
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// A removed or unknown texture throws, also for a tint alpha of 0 and a rect with no area. The list stays empty.
    /// </summary>
    [Fact]
    public void ThrowsForARemovedTexture()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle removed = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);
        renderer.Textures.Remove(removed);
        TextureHandle unknown = new(5, DemoSize);

        imgui.BeginFrame();
        renderer.Begin(new float2(400, 300), 1f);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.Image(DemoRect, removed, FullUv, White, default));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.Image(DemoRect, removed, FullUv, Rgba8.Transparent, default));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.Image(new float4(0, 0, 0, 0), removed, FullUv, White, default));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => renderer.Image(DemoRect, unknown, FullUv, White, default));
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>An image call before <see cref="ImGuiRenderer.Begin"/> throws, also for the none handle.</summary>
    [Fact]
    public void ThrowsForAnImageCallBeforeBegin()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        Assert.Throws<InvalidOperationException>(() => renderer.Image(DemoRect, texture, FullUv, White, default));
        Assert.Throws<InvalidOperationException>(() => renderer.Image(DemoRect, default, FullUv, White, default));
        Assert.Throws<InvalidOperationException>(
            () => renderer.Image(DemoRect, new TextureHandle(9), FullUv, White, default));
        imgui.EndFrame();

        Assert.Equal(EmptyList, imgui.DescribeBackgroundList());
    }

    /// <summary>The image draws under a pushed clip, and the renderer adds the main viewport position.</summary>
    [Fact]
    public void DrawsUnderTheClipAtTheViewportPosition()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(DemoId), DemoSize);

        imgui.BeginFrame();
        Gui.GetMainViewport().Pos = new float2(100, 50);
        renderer.Begin(new float2(400, 300), 1f);
        renderer.PushClip(new float4(10, 10, 40, 40));
        renderer.Image(DemoRect, texture, FullUv, White, default);
        renderer.PopClip();
        renderer.End();
        imgui.EndFrame();

        Assert.Equal(
            "cmd 0 clip 110,60,150,100 tex id 4660 elems 6 verts 120,70-184,102 colors #FFFFFFFF",
            imgui.DescribeBackgroundList());
    }

    private static ImGuiRenderer CreateRenderer()
    {
        return new ImGuiRenderer(new ImGuiRenderer.CreateInfo
        {
            Fonts = new ImGuiFontService(),
            Textures = new ImGuiTextureService(),
        });
    }

    /// <summary>Gives the one vertex of the background list at a position. Call it after the frame.</summary>
    private static ImDrawVert FindVertex(float2 pos)
    {
        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        ref ImVector<ImDrawVert> vertices = ref list.VtxBuffer;
        int found = -1;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (vertices[i].pos.X == pos.X && vertices[i].pos.Y == pos.Y)
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return vertices[found];
    }
}
