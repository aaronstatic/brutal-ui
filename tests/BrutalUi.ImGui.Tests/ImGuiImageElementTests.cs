using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// Tests of the <see cref="BrutalUi.Core.Image"/> element with <see cref="ImGuiRenderer"/> on a headless ImGui context.
/// </summary>
/// <remarks>
/// The texture is a reference with a texture id and no texture data, so the harness dump prints <c>tex id N</c> for
/// it. The service registers the reference only, so the test needs no GPU.
/// </remarks>
public sealed class ImGuiImageElementTests
{
    /// <summary>The texture id of the reference.</summary>
    private const int ReferenceId = 4660;

    private static readonly int2 ReferenceSize = new(64, 32);

    /// <summary>
    /// An image element with a handle of the texture service takes the size of the texture, and the draw binds the
    /// texture of the handle in a quad of 64 by 32 at the top-left corner.
    /// </summary>
    [Fact]
    public void DrawsTheImageElementWithItsTexture()
    {
        using HeadlessImGui imgui = new();
        ImGuiRenderer renderer = new(new ImGuiRenderer.CreateInfo
        {
            Fonts = new ImGuiFontService(),
            Textures = new ImGuiTextureService(),
        });
        TextureHandle texture = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(ReferenceId), ReferenceSize);

        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer }) { ViewportSize = new float2(400, 300) };
        Element root = new();
        root.Inline.AlignItems = Align.FlexStart;
        context.AddRoot(root);
        BrutalUi.Core.Image image = new(new BrutalUi.Core.Image.CreateInfo { Texture = texture });
        root.Children.Add(image);

        context.Update(0f);
        imgui.BeginFrame();
        context.Draw(renderer);
        imgui.EndFrame();

        string dump = imgui.DescribeBackgroundList();
        TestContext.Current.TestOutputHelper?.WriteLine(dump);

        Assert.Equal(new float4(0f, 0f, 64f, 32f), image.Layout.Rect);
        Assert.Contains("tex id 4660", dump, System.StringComparison.Ordinal);
        Assert.Contains("verts 0,0-64,32", dump, System.StringComparison.Ordinal);
        Assert.Equal("cmd 0 clip 0,0,400,300 tex id 4660 elems 6 verts 0,0-64,32 colors #FFFFFFFF", dump);
        int commandCount = Gui.GetBackgroundDrawList(Gui.GetMainViewport()).CmdBuffer.Count;
        Assert.Equal(1, commandCount);
    }
}
