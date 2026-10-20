using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// Tests of the headless harness: a frame with no backend, the texture status reset, the dump of the background list,
/// and the context lifetime. The tests draw with the raw binding calls, so they need no renderer.
/// </summary>
public sealed unsafe class HeadlessImGuiTests
{
    /// <summary>Two frames run with no backend, and no texture waits for the backend after a frame.</summary>
    [Fact]
    public void RunsFramesWithNoBackend()
    {
        using HeadlessImGui imgui = new();

        for (int frame = 1; frame <= 2; frame++)
        {
            imgui.BeginFrame();
            Assert.Equal(frame, Gui.GetFrameCount());
            Assert.True(imgui.FrameOpen);
            imgui.EndFrame();
            Assert.False(imgui.FrameOpen);

            ref ImVector<ImTextureDataPtr> textures = ref Gui.GetPlatformIO().Textures;
            Assert.True(textures.Count > 0);
            for (int i = 0; i < textures.Count; i++)
            {
                Assert.True(
                    textures[i].Status is ImTextureStatus.OK or ImTextureStatus.Destroyed,
                    $"Texture {i} has the status {textures[i].Status} after frame {frame}.");
            }
        }
    }

    /// <summary>The constructor sets the display size, and the main viewport takes it at the origin 0,0.</summary>
    [Fact]
    public void SetsTheDisplaySize()
    {
        using HeadlessImGui imgui = new(1280, 720);

        imgui.BeginFrame();
        ImGuiViewportPtr viewport = Gui.GetMainViewport();
        float2 position = viewport.Pos;
        float2 size = viewport.Size;
        imgui.EndFrame();

        Assert.Equal(0f, position.X);
        Assert.Equal(0f, position.Y);
        Assert.Equal(1280f, size.X);
        Assert.Equal(720f, size.Y);
    }

    /// <summary>
    /// The dump gives one line for each command: the clip, the atlas texture, the element count, the vertex bounds, and
    /// the distinct colors in first-seen order.
    /// </summary>
    [Fact]
    public void DescribesTheBackgroundList()
    {
        using HeadlessImGui imgui = new();

        imgui.BeginFrame();
        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        Fill(list, new float2(0, 0), new float2(200, 100), 0xFF1E140Au);
        float2 clipMin = new(2, 2);
        float2 clipMax = new(198, 98);
        Gui.PInvoke.ImDrawList_PushClipRect(list.Ptr, &clipMin, &clipMax, true);
        Fill(list, new float2(12, 12), new float2(112, 52), 0xFF0000FFu);
        Fill(list, new float2(212, 12), new float2(312, 52), 0xFF00FF00u);
        Gui.PInvoke.ImDrawList_PopClipRect(list.Ptr);
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex atlas elems 6 verts 0,0-200,100 colors #0A141EFF\n" +
            "cmd 1 clip 2,2,198,98 tex atlas elems 12 verts 12,12-312,52 colors #FF0000FF #00FF00FF",
            dump);
    }

    /// <summary>A command of a texture that the game registered prints its id.</summary>
    [Fact]
    public void DescribesARegisteredTextureById()
    {
        using HeadlessImGui imgui = new();

        imgui.BeginFrame();
        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        ImTextureRef texture = new() { _TexID = new ImTextureID(4660) };
        float2 min = new(20, 20);
        float2 max = new(84, 52);
        float2 uvMin = new(0, 0);
        float2 uvMax = new(1, 1);
        Gui.PInvoke.ImDrawList_AddImage(list.Ptr, texture, &min, &max, &uvMin, &uvMax, 0xFFFFFFFFu);
        imgui.EndFrame();

        Assert.Equal(
            "cmd 0 clip 0,0,400,300 tex id 4660 elems 6 verts 20,20-84,52 colors #FFFFFFFF",
            imgui.DescribeBackgroundList());
    }

    /// <summary>
    /// A frame with no draw call keeps one command with 0 elements: ImGui removes an unused command only when another
    /// command is before it.
    /// </summary>
    [Fact]
    public void DescribesAListWithNoDrawCall()
    {
        using HeadlessImGui imgui = new();

        imgui.BeginFrame();
        _ = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        imgui.EndFrame();

        Assert.Equal("cmd 0 clip 0,0,400,300 tex atlas elems 0 verts none colors", imgui.DescribeBackgroundList());
    }

    /// <summary>The frame calls check the order, and the dump needs a closed frame.</summary>
    [Fact]
    public void ThrowsForAFrameCallOutOfOrder()
    {
        using HeadlessImGui imgui = new();

        Assert.Throws<InvalidOperationException>(imgui.EndFrame);
        imgui.BeginFrame();
        Assert.Throws<InvalidOperationException>(imgui.BeginFrame);
        Assert.Throws<InvalidOperationException>(() => imgui.DescribeBackgroundList());
        imgui.EndFrame();
    }

    /// <summary>
    /// Dispose renders an open frame and destroys the context, so no context is current and a new harness works.
    /// </summary>
    [Fact]
    public void DisposeEndsAnOpenFrameAndDestroysTheContext()
    {
        HeadlessImGui first = new();
        first.BeginFrame();
        first.Dispose();
        first.Dispose();

        Assert.True(Gui.GetCurrentContext().IsNull());
        Assert.Throws<ObjectDisposedException>(first.BeginFrame);

        using HeadlessImGui second = new();
        second.BeginFrame();
        second.EndFrame();
        Assert.Equal(1, Gui.GetFrameCount());
    }

    /// <summary>A second harness while one is alive throws, because ImGui keeps the old context current.</summary>
    [Fact]
    public void ThrowsForASecondContext()
    {
        using HeadlessImGui imgui = new();

        Assert.Throws<InvalidOperationException>(() => new HeadlessImGui());
        Assert.True(imgui.Context.Ptr == Gui.GetCurrentContext().Ptr);
    }

    /// <summary>The color format reads the bytes of an ImU32 in the order R, G, B, A.</summary>
    [Fact]
    public void FormatsTheColorAsRgba()
    {
        Assert.Equal("#0A141EFF", HeadlessImGui.FormatColor(0xFF1E140Au));
        Assert.Equal("0", HeadlessImGui.Format(-0f));
        Assert.Equal("400", HeadlessImGui.Format(400f));
        Assert.Equal("0.5", HeadlessImGui.Format(0.5f));
    }

    private static void Fill(ImDrawListPtr list, float2 min, float2 max, uint color)
    {
        Gui.PInvoke.ImDrawList_AddRectFilled(list.Ptr, &min, &max, color, 0f, ImDrawFlags.RoundCornersNone);
    }
}
