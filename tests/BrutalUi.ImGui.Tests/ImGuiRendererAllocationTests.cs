using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// The release gate of the ImGui renderer. After warm-up, a frame that draws the 1000-element tree, and a frame of
/// direct renderer calls, must allocate 0 managed bytes.
/// </summary>
/// <remarks>
/// <para>
/// The pattern is the one of <c>ThousandElementAllocationTests</c> in the Core test project: build the scene, run one
/// <c>Update</c>, run warm-up frames, and then measure 100 frames with <see cref="GC.GetAllocatedBytesForCurrentThread"/>.
/// The gate reads the counter right before and right after the measured calls of each frame, so the harness calls
/// (<c>BeginFrame</c> and <c>EndFrame</c>) are outside the measured region.
/// </para>
/// <para>
/// Glyph baking uses native memory. The warm-up frames use each font and each size of the measured frames, so the
/// first bake of a size is not in a measured frame. The gate finds any managed byte.
/// </para>
/// </remarks>
public sealed unsafe class ImGuiRendererAllocationTests
{
    private const int WarmUpFrames = 3;
    private const int MeasuredFrames = 100;
    private const int MinimumVerticesPerRect = 4;

    private static readonly float4 DemoRect = new(20f, 20f, 64f, 32f);
    private static readonly float4 FullUv = new(0f, 0f, 1f, 1f);
    private static readonly Rgba8 Fill = new(40, 80, 160);
    private static readonly Rgba8 Border = new(220, 200, 80, 200);
    private static readonly Rgba8 OpaqueBorder = new(220, 200, 80);
    private static readonly Rgba8 White = new(255, 255, 255);
    private static readonly Rgba8 Transparent = new(255, 255, 255, 0);

    /// <summary>The text of the direct calls. The text is long enough to wrap at a width of 70.</summary>
    private static ReadOnlySpan<byte> Hello => "Hello, brutal-ui"u8;

    /// <summary>The 1000-element tree draws through <c>UiContext.Draw</c> with no managed allocation.</summary>
    [Fact]
    public void DrawAllocatesNothingAfterWarmUp()
    {
        using HeadlessImGui imgui = new(
            ThousandElementTree.ViewportWideWidth, ThousandElementTree.ViewportHeight);
        ImGuiRenderer renderer = CreateRenderer();
        ThousandElementTree tree = ThousandElementTree.Build();
        tree.Context.Update(0.016f);

        for (int i = 0; i < WarmUpFrames; i++)
        {
            DrawFrame(imgui, tree, renderer);
        }

        long bytes = 0;
        for (int i = 0; i < MeasuredFrames; i++)
        {
            bytes += DrawFrame(imgui, tree, renderer);
        }

        Assert.Equal(0L, bytes);
    }

    /// <summary>
    /// Each direct call allocates nothing: the clip, the three kinds of rect, the square and the rounded image, the
    /// text, and the measure with and without a wrap. The skip paths run too: a text with an alpha of 0 or an empty span,
    /// an empty measure, the none image, a rect with no width, a clip with a <c>NaN</c> component, a rect and an image
    /// with an overflow, a border below 1 pixel, and a clip that stays open until <c>End</c>. A second font and a second
    /// size run in the warm-up frames.
    /// </summary>
    [Fact]
    public void DirectCallsAllocateNothingAfterWarmUp()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = CreateRenderer();
        FontHandle wide = renderer.Fonts.Add(AddWideFont(13f));
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(4660), new int2(64, 32));

        for (int i = 0; i < WarmUpFrames; i++)
        {
            DirectFrame(imgui, renderer, wide, texture);
        }

        long bytes = 0;
        for (int i = 0; i < MeasuredFrames; i++)
        {
            bytes += DirectFrame(imgui, renderer, wide, texture);
        }

        Assert.Equal(0L, bytes);
    }

    /// <summary>
    /// The measured draw paints real geometry: the background list holds 4 vertices or more for each
    /// <c>Rect</c> call that the recording renderer sees.
    /// </summary>
    [Fact]
    public void TheMeasuredDrawDoesRealWork()
    {
        ThousandElementTree tree = ThousandElementTree.Build();
        tree.Context.Update(0.016f);
        RecordingRenderer recorder = new();
        tree.Context.Draw(recorder);
        int rectCalls = 0;
        ReadOnlySpan<DrawCall> calls = recorder.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            rectCalls += calls[i].Kind == DrawCallKind.Rect ? 1 : 0;
        }

        Assert.True(rectCalls > 0);

        using HeadlessImGui imgui = new(
            ThousandElementTree.ViewportWideWidth, ThousandElementTree.ViewportHeight);
        ImGuiRenderer renderer = CreateRenderer();
        DrawFrame(imgui, tree, renderer);

        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        int vertexCount = list.VtxBuffer.Count;
        TestContext.Current.TestOutputHelper?.WriteLine(
            "rect calls " + rectCalls + ", vertices " + vertexCount);
        Assert.True(
            vertexCount >= MinimumVerticesPerRect * rectCalls,
            "The list holds " + vertexCount + " vertices for " + rectCalls + " Rect calls.");
    }

    /// <summary>Draws the tree in one frame and gives the managed bytes of the <c>Draw</c> call.</summary>
    private static long DrawFrame(HeadlessImGui imgui, ThousandElementTree tree, ImGuiRenderer renderer)
    {
        imgui.BeginFrame();
        long before = GC.GetAllocatedBytesForCurrentThread();
        tree.Context.Draw(renderer);
        long after = GC.GetAllocatedBytesForCurrentThread();
        imgui.EndFrame();
        return after - before;
    }

    /// <summary>Runs each direct call in one frame and gives the managed bytes of the calls.</summary>
    private static long DirectFrame(
        HeadlessImGui imgui, ImGuiRenderer renderer, FontHandle wide, TextureHandle texture)
    {
        Corners round = Corners.All(6f);
        float4 clip = new(10f, 10f, 200f, 100f);
        float2 pos = new(30f, 30f);
        float4 cover = new(100f, 100f, 20f, 20f);
        float4 zeroWidth = new(20f, 20f, 0f, 32f);
        float4 nanClip = new(float.NaN, 10f, 50f, 50f);
        float4 overflow = new(3e38f, 0f, 3e38f, 10f);

        imgui.BeginFrame();
        long before = GC.GetAllocatedBytesForCurrentThread();
        renderer.Begin(new float2(HeadlessImGui.DefaultWidth, HeadlessImGui.DefaultHeight), 1f);
        renderer.PushClip(clip);
        renderer.Rect(DemoRect, Fill, default, 0f, default);
        renderer.Rect(DemoRect, Fill, round, 2f, Border);
        renderer.Rect(cover, Fill, default, 10f, OpaqueBorder);
        renderer.Rect(cover, Fill, default, 10f, Border);
        renderer.Image(DemoRect, texture, FullUv, White, default);
        renderer.Image(DemoRect, texture, FullUv, White, round);
        renderer.Text(pos, default, 13f, White, Hello, 0f);
        renderer.Text(pos, wide, 13f, White, Hello, 70f);
        renderer.Text(pos, default, 26f, White, Hello, 0f);
        float2 plain = renderer.MeasureText(default, 13f, Hello, 0f);
        float2 wrapped = renderer.MeasureText(wide, 26f, Hello, 70f);
        renderer.PopClip();

        // The skip paths: each call returns early and must allocate nothing.
        renderer.Text(pos, default, 13f, Transparent, Hello, 0f);
        renderer.Text(pos, default, 13f, White, ReadOnlySpan<byte>.Empty, 0f);
        float2 emptyMeasure = renderer.MeasureText(default, 13f, ReadOnlySpan<byte>.Empty, 0f);
        renderer.Image(DemoRect, default, FullUv, White, default);
        renderer.Rect(zeroWidth, Fill, default, 0f, default);

        // The non-finite and overflow paths of the clip, the rect, and the image, and a border below 1 pixel.
        renderer.PushClip(nanClip);
        renderer.PopClip();
        renderer.Rect(overflow, Fill, default, 0f, default);
        renderer.Image(overflow, texture, FullUv, White, default);
        renderer.Rect(cover, Fill, default, 0.5f, Border);
        renderer.Rect(cover, Fill, round, 0.25f, OpaqueBorder);

        // One clip stays open: End pops it.
        renderer.PushClip(clip);
        renderer.End();
        long after = GC.GetAllocatedBytesForCurrentThread();
        imgui.EndFrame();

        // The measure outside a frame runs once more, with a font and a size of the frame.
        long outsideBefore = GC.GetAllocatedBytesForCurrentThread();
        float2 outside = renderer.MeasureText(wide, 13f, Hello, 0f);
        long outsideAfter = GC.GetAllocatedBytesForCurrentThread();

        GC.KeepAlive(plain.X + wrapped.X + outside.X + emptyMeasure.X > 0f ? renderer : null);
        return (after - before) + (outsideAfter - outsideBefore);
    }

    private static ImGuiRenderer CreateRenderer()
    {
        return new ImGuiRenderer(new ImGuiRenderer.CreateInfo
        {
            Fonts = new ImGuiFontService(),
            Textures = new ImGuiTextureService(),
        });
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
}
