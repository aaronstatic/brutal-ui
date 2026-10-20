using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// The release gate of the ImGui renderer. After warm-up, a frame that draws the 1000-element tree, a frame of direct
/// renderer calls, and a frame that changes, measures, and draws labels and images must allocate 0 managed bytes.
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

    /// <summary>The label count and the image count of the label scene.</summary>
    private const int LabelSceneCount = 50;

    /// <summary>The count of labels and of images that the label frame changes.</summary>
    private const int LabelSceneChangedCount = 10;

    /// <summary>The width of a label of the label scene. The padding of 4 leaves 112 pixels: 16 glyphs at size 13.</summary>
    private const float LabelSceneLabelWidth = 120f;

    /// <summary>The padding of a label of the label scene.</summary>
    private const float LabelScenePadding = 4f;

    /// <summary>The font size of the label scene and of the even label frames, the size of the default font of the harness.</summary>
    private const float LabelSceneFontSize = 13f;

    /// <summary>The font size of the odd label frames. Both texts wrap to two lines at this size too.</summary>
    private const float LabelSceneSecondFontSize = 12f;

    /// <summary>The width of an image of the label scene. The height comes from the image measure.</summary>
    private const float LabelSceneImageWidth = 32f;

    /// <summary>The display width of the label scene.</summary>
    private const float LabelSceneWidth = 400f;

    /// <summary>The display height of the label scene. The column of 101 elements is about 2520 pixels high.</summary>
    private const float LabelSceneHeight = 2600f;

    private static readonly int2 LabelSceneTextureSize = new(16, 16);
    private static readonly Rgba8 FirstTint = new(255, 200, 100);
    private static readonly Rgba8 SecondTint = new(100, 200, 255);
    private static readonly Rgba8 FirstColor = new(255, 240, 200);
    private static readonly Rgba8 SecondColor = new(200, 240, 255);

    /// <summary>The UV rect of the odd label frames: the top half of the texture, a natural size of 16 by 8.</summary>
    private static readonly float4 HalfUv = new(0f, 0f, 1f, 0.5f);

    /// <summary>The text of the direct calls. The text is long enough to wrap at a width of 70.</summary>
    private static ReadOnlySpan<byte> Hello => "Hello, brutal-ui"u8;

    /// <summary>The text of the even label frames: 25 glyphs, two lines at a wrap width of 112.</summary>
    private static ReadOnlySpan<byte> FirstLabelText => "The first text of a label"u8;

    /// <summary>The text of the odd label frames: 24 glyphs, two lines at a wrap width of 112.</summary>
    private static ReadOnlySpan<byte> SecondLabelText => "A second text that wraps"u8;

    /// <summary>
    /// The label scene: the context, the renderer, the changed labels and images, and the frame counter.
    /// </summary>
    private sealed class LabelScene
    {
        public required UiContext Context { get; init; }

        /// <summary>The renderer that measures the text and draws the frame.</summary>
        public required ImGuiRenderer Renderer { get; init; }

        /// <summary>The labels with a plain text, in tree order.</summary>
        public required Label[] Labels { get; init; }

        /// <summary>The images, in tree order.</summary>
        public required BrutalUi.Core.Image[] Images { get; init; }

        /// <summary>The texture of the even frames.</summary>
        public required TextureHandle FirstTexture { get; init; }

        /// <summary>The texture of the odd frames. It has the size of <see cref="FirstTexture"/> and another id.</summary>
        public required TextureHandle SecondTexture { get; init; }

        /// <summary>The label with an <see cref="Brutal.Localization.LString"/>. It is the last child of the root.</summary>
        public required Label Localized { get; init; }

        /// <summary>The <see cref="Brutal.Localization.LString"/> of the even frames: "Hello".</summary>
        public required Brutal.Localization.LString FirstLocalized { get; init; }

        /// <summary>The <see cref="Brutal.Localization.LString"/> of the odd frames: "World".</summary>
        public required Brutal.Localization.LString SecondLocalized { get; init; }

        /// <summary>The number of frames that ran. An even number takes the first value, an odd number the second.</summary>
        public int FrameCount { get; set; }
    }

    /// <summary>
    /// The body of the Core <c>ElementAllocationTests</c> allocates nothing with the real ImGui measure and draw. A frame
    /// sets a new text, color, and size on 10 labels, a new tint, UV rect, texture, and scale mode on 10 images, and a
    /// new <see cref="Brutal.Localization.LString"/> on the last label. It runs <c>Update</c> outside the ImGui frame.
    /// Then it runs <c>Draw</c> between <see cref="HeadlessImGui.BeginFrame"/> and <see cref="HeadlessImGui.EndFrame"/>.
    /// </summary>
    /// <remarks>
    /// The harness has one font, so the frame does not change the font. The warm-up frames use both texts and both font
    /// sizes, so the first bake of each glyph is not in a measured frame.
    /// </remarks>
    [Fact]
    public void LabelUpdateAndDrawAllocateNothingAfterWarmUp()
    {
        using HeadlessImGui imgui = new(LabelSceneWidth, LabelSceneHeight);
        ImGuiRenderer renderer = CreateRenderer();
        TextureHandle first = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(4660), LabelSceneTextureSize);
        TextureHandle second = renderer.Textures.Add(ImGuiTextureServiceTests.TestTexture(4661), LabelSceneTextureSize);
        LabelScene scene = BuildLabelScene(renderer, first, second);

        for (int i = 0; i < WarmUpFrames; i++)
        {
            LabelFrame(imgui, scene);
        }

        long bytes = 0;
        for (int i = 0; i < MeasuredFrames; i++)
        {
            bytes += LabelFrame(imgui, scene);

            // The body is a real change: each element shows the values of the frame. The checks run outside the
            // measured region.
            AssertLabelFrame(scene, ((scene.FrameCount - 1) & 1) == 1, i);
        }

        TestContext.Current.TestOutputHelper?.WriteLine("frames " + MeasuredFrames + ", bytes " + bytes);
        Assert.Equal(0L, bytes);
    }

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

    /// <summary>
    /// Builds the label scene: a root column with 50 labels (a width of 120 and a padding of 4), 50 images (a width of
    /// 32), and one label with an <see cref="Brutal.Localization.LString"/>. The renderer is the measurer. The scene runs
    /// no <c>Update</c>.
    /// </summary>
    private static LabelScene BuildLabelScene(ImGuiRenderer renderer, TextureHandle texture, TextureHandle second)
    {
        Rgba8 white = new(255, 255, 255);
        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer })
        {
            ViewportSize = new float2(LabelSceneWidth, LabelSceneHeight),
        };

        Element root = new();
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.AlignItems = Align.FlexStart;
        context.AddRoot(root);

        Label[] labels = new Label[LabelSceneCount];
        for (int i = 0; i < labels.Length; i++)
        {
            Label label = new(new Label.CreateInfo
            {
                Color = white,
                Size = LabelSceneFontSize,
                Text = "A label that wraps in its box",
            });
            label.Style.Width = LabelSceneLabelWidth;
            label.Style.Padding = LengthEdges.All(LabelScenePadding);
            root.Children.Add(label);
            labels[i] = label;
        }

        BrutalUi.Core.Image[] images = new BrutalUi.Core.Image[LabelSceneCount];
        for (int i = 0; i < images.Length; i++)
        {
            BrutalUi.Core.Image image = new(new BrutalUi.Core.Image.CreateInfo { Texture = texture });
            image.Style.Width = LabelSceneImageWidth;
            root.Children.Add(image);
            images[i] = image;
        }

        Brutal.Localization.LString hello = new("imgui-renderer-allocation-tests.localized", "Hello");
        Brutal.Localization.LString world = new("imgui-renderer-allocation-tests.other-localized", "World");
        Label localized = new(new Label.CreateInfo
        {
            Color = white,
            Size = LabelSceneFontSize,
            LocalizedText = hello,
        });
        root.Children.Add(localized);

        return new LabelScene
        {
            Context = context,
            Renderer = renderer,
            Labels = labels,
            Images = images,
            FirstTexture = texture,
            SecondTexture = second,
            Localized = localized,
            FirstLocalized = hello,
            SecondLocalized = world,
        };
    }

    /// <summary>
    /// Runs one label frame and gives the managed bytes of the measured calls: the changes and <c>Update</c> outside
    /// the ImGui frame, and <c>Draw</c> inside it. The harness calls are outside the measured regions.
    /// </summary>
    private static long LabelFrame(HeadlessImGui imgui, LabelScene scene)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        bool odd = (scene.FrameCount++ & 1) == 1;
        ReadOnlySpan<byte> text = odd ? SecondLabelText : FirstLabelText;
        Rgba8 color = odd ? SecondColor : FirstColor;
        float size = odd ? LabelSceneSecondFontSize : LabelSceneFontSize;
        Rgba8 tint = odd ? SecondTint : FirstTint;
        float4 uv = odd ? HalfUv : FullUv;
        TextureHandle texture = odd ? scene.SecondTexture : scene.FirstTexture;
        ImageScaleMode mode = odd ? ImageScaleMode.ScaleAndCrop : ImageScaleMode.ScaleToFit;
        Label[] labels = scene.Labels;
        BrutalUi.Core.Image[] images = scene.Images;
        for (int i = 0; i < LabelSceneChangedCount; i++)
        {
            Label label = labels[i];
            label.SetText(text);
            label.Color = color;
            label.Size = size;

            BrutalUi.Core.Image image = images[i];
            image.Tint = tint;
            image.Uv = uv;
            image.Texture = texture;
            image.ScaleMode = mode;
        }

        scene.Localized.SetText(odd ? scene.SecondLocalized : scene.FirstLocalized);
        scene.Context.Update(0.016f);
        long after = GC.GetAllocatedBytesForCurrentThread();

        imgui.BeginFrame();
        long drawBefore = GC.GetAllocatedBytesForCurrentThread();
        scene.Context.Draw(scene.Renderer);
        long drawAfter = GC.GetAllocatedBytesForCurrentThread();
        imgui.EndFrame();

        return (after - before) + (drawAfter - drawBefore);
    }

    /// <summary>
    /// Checks the elements and the layout of one label frame. The label measure ran with the new size, and the image
    /// measure ran with the new UV span.
    /// </summary>
    /// <param name="scene">The scene after the frame.</param>
    /// <param name="odd">Whether the frame took the second values.</param>
    /// <param name="frame">The number of the measured frame, for the message.</param>
    private static void AssertLabelFrame(LabelScene scene, bool odd, int frame)
    {
        Label label = scene.Labels[0];
        float size = odd ? LabelSceneSecondFontSize : LabelSceneFontSize;
        Assert.True(
            label.Text.SequenceEqual(odd ? SecondLabelText : FirstLabelText),
            "Frame " + frame + " shows the old text.");
        Assert.Equal(odd ? SecondColor : FirstColor, label.Color);
        Assert.Equal(size, label.Size);

        // The text wraps to two lines in both sizes.
        Assert.Equal((2 * size) + (2 * LabelScenePadding), label.Layout.Rect.W);

        BrutalUi.Core.Image image = scene.Images[0];
        Assert.Equal(odd ? SecondTint : FirstTint, image.Tint);
        Assert.Equal((odd ? scene.SecondTexture : scene.FirstTexture).Id, image.Texture.Id);
        Assert.Equal(odd ? ImageScaleMode.ScaleAndCrop : ImageScaleMode.ScaleToFit, image.ScaleMode);

        // The image measure gives the natural height of the UV rect: 16 for the full rect, 8 for the half rect.
        Assert.Equal(LabelSceneImageWidth, image.Layout.Rect.Z);
        Assert.Equal(odd ? 8f : 16f, image.Layout.Rect.W);

        Assert.True(scene.Localized.Text.SequenceEqual(odd ? "World"u8 : "Hello"u8));
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
