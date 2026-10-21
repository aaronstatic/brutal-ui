using System;
using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of <see cref="Image"/>: the natural size, the measure rule, the draw rule of each scale mode, the dirty rules
/// of the setters, and the pure rules <see cref="Image.FitRect"/> and <see cref="Image.CropUv"/>.
/// </summary>
/// <remarks>The demo texture has the id 7 and a size of 64 by 32 texels, so its aspect is 2.</remarks>
public sealed class ImageTests
{
    private const int DemoId = 7;

    private static readonly int2 DemoSize = new(64, 32);
    private static readonly TextureHandle DemoTexture = new(DemoId, DemoSize);
    private static readonly float4 FullUv = new(0f, 0f, 1f, 1f);

    /// <summary>
    /// The demo of phase 3: an image takes the size of its texture, and a fit image in a box of 100 by 100 draws a rect
    /// of 100 by 50 in the center of the box. The context has no measurer, because an image measures no text.
    /// </summary>
    [Fact]
    public void DrawsAnImageAtItsTextureSize()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Image natural = new(new Image.CreateInfo { Texture = DemoTexture });
        Image fit = new(new Image.CreateInfo { Texture = DemoTexture, ScaleMode = ImageScaleMode.ScaleToFit });
        fit.Inline.Width = 100;
        fit.Inline.Height = 100;
        root.Children.Add(natural);
        root.Children.Add(fit);

        context.Update(0f);
        context.Draw(renderer);

        string actual = renderer.Describe();
        TestContext.Current.TestOutputHelper?.WriteLine(actual);

        string[] lines = actual.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Equal("Image 0,0,64,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0", lines[1]);
        Assert.Equal("Image 0,57,100,50 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0", lines[2]);
        Assert.Equal("Begin 400x300 scale 1", lines[0]);
        Assert.Equal("End", lines[3]);
        Assert.Equal("rect 0,32,100,100", RectLine(fit.Layout.Rect));
    }

    /// <summary>The create-info gives the defaults of the public API: the full UV rect, opaque white, and stretch.</summary>
    [Fact]
    public void UsesTheDefaultsOfTheCreateInfo()
    {
        Image image = new(new Image.CreateInfo { Texture = DemoTexture });

        Assert.Equal(DemoId, image.Texture.Id);
        Assert.Equal(FullUv, image.Uv);
        Assert.Equal(new Rgba8(255, 255, 255), image.Tint);
        Assert.Equal(ImageScaleMode.StretchToFill, image.ScaleMode);
        Assert.NotNull(image.Layout.Measure);
    }

    /// <summary>The UV span sets the natural size, so a region of an atlas takes the size of the region.</summary>
    [Fact]
    public void TakesTheSizeOfTheUvRegion()
    {
        Image image = AddImage(CreateContext(out UiContext context), new float4(0f, 0f, 0.5f, 0.5f));

        context.Update(0f);

        Assert.Equal("rect 0,0,32,16", RectLine(image.Layout.Rect));
    }

    /// <summary>A flipped UV rect uses the absolute span for the natural size, and the draw keeps the flip.</summary>
    [Fact]
    public void KeepsTheFlipOfTheUvRect()
    {
        RecordingRenderer renderer = new();
        Image image = AddImage(CreateContext(out UiContext context), new float4(1f, 0.5f, 0.5f, 0f));

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("rect 0,0,32,16", RectLine(image.Layout.Rect));
        Assert.Contains("Image 0,0,32,16 texture 7 uv 1,0.5,0.5,0 tint #FFFFFFFF", renderer.Describe(), StringComparison.Ordinal);
    }

    /// <summary>An image in a parent of width 40 gets the <c>AtMost</c> limit 40 and takes that width.</summary>
    [Fact]
    public void ShrinksToAnAtMostLimit()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        root.Inline.Width = 40;
        Image image = new(new Image.CreateInfo { Texture = DemoTexture });
        root.Children.Add(image);

        context.Update(0f);

        Assert.Equal("rect 0,0,40,32", RectLine(image.Layout.Rect));
    }

    /// <summary>
    /// The contract of <see cref="MeasureCallback"/>: with <see cref="MeasureMode.AtMost"/>, a natural size that fits
    /// gives the size of <see cref="MeasureMode.Undefined"/>. The test checks the callback and the layout pass.
    /// </summary>
    [Fact]
    public void KeepsTheAtMostContract()
    {
        Image probe = new(new Image.CreateInfo { Texture = DemoTexture });
        MeasureCallback measure = probe.Layout.Measure!;
        float2 free = measure(probe, float.NaN, MeasureMode.Undefined, float.NaN, MeasureMode.Undefined);
        float2 atMost = measure(probe, 200f, MeasureMode.AtMost, 300f, MeasureMode.AtMost);
        float2 exactly = measure(probe, 200f, MeasureMode.Exactly, 10f, MeasureMode.Exactly);
        float2 tight = measure(probe, 20f, MeasureMode.AtMost, 10f, MeasureMode.AtMost);

        Assert.Equal(new float2(64, 32), free);
        Assert.Equal(free, atMost);
        Assert.Equal(new float2(200, 10), exactly);
        Assert.Equal(new float2(20, 10), tight);

        Image wide = AddImage(CreateContext(out UiContext wideContext), FullUv);
        wideContext.ViewportSize = new float2(200, 300);
        Image unbounded = AddImage(CreateContext(out UiContext freeContext), FullUv);
        freeContext.ViewportSize = new float2(float.NaN, 300);

        wideContext.Update(0f);
        freeContext.Update(0f);

        Assert.Equal("rect 0,0,64,32", RectLine(wide.Layout.Rect));
        Assert.Equal(RectLine(unbounded.Layout.Rect), RectLine(wide.Layout.Rect));
    }

    /// <summary>A limit that is not finite gives the natural size on its axis.</summary>
    [Fact]
    public void MeasuresTheNaturalSizeForALimitThatIsNotFinite()
    {
        Assert.Equal(64f, Image.MeasureAxis(64f, float.NaN, MeasureMode.Exactly));
        Assert.Equal(64f, Image.MeasureAxis(64f, float.PositiveInfinity, MeasureMode.AtMost));
        Assert.Equal(64f, Image.MeasureAxis(64f, 10f, MeasureMode.Undefined));
        Assert.Equal(0f, Image.MeasureAxis(64f, 0f, MeasureMode.AtMost));
    }

    /// <summary>The none handle and a texture size with a component of 0 give a natural size of (0, 0).</summary>
    [Fact]
    public void TakesNoSpaceForTheNoneHandle()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Image none = new(new Image.CreateInfo { Texture = default });
        Image noneWithSize = new(new Image.CreateInfo { Texture = new TextureHandle(0, DemoSize) });
        Image noSize = new(new Image.CreateInfo { Texture = new TextureHandle(DemoId) });
        Image flat = new(new Image.CreateInfo { Texture = new TextureHandle(DemoId, new int2(64, 0)) });
        root.Children.Add(none);
        root.Children.Add(noneWithSize);
        root.Children.Add(noSize);
        root.Children.Add(flat);

        context.Update(0f);

        Assert.Equal("rect 0,0,0,0", RectLine(none.Layout.Rect));
        Assert.Equal("rect 0,0,0,0", RectLine(noneWithSize.Layout.Rect));
        Assert.Equal("rect 0,0,0,0", RectLine(noSize.Layout.Rect));
        Assert.Equal("rect 0,0,0,0", RectLine(flat.Layout.Rect));
        Assert.Equal(default, Image.NaturalSize(DemoTexture, new float4(0f, 0f, float.NaN, float.PositiveInfinity)));
    }

    /// <summary>The none handle and a texture size of 0 draw nothing, also in a box with a style size.</summary>
    [Fact]
    public void DrawsNothingForTheNoneHandle()
    {
        RecordingRenderer renderer = new();
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Image none = new(new Image.CreateInfo { Texture = new TextureHandle(0, DemoSize) });
        Image noSize = new(new Image.CreateInfo { Texture = new TextureHandle(DemoId) });
        none.Inline.Width = 100;
        none.Inline.Height = 100;
        noSize.Inline.Width = 100;
        noSize.Inline.Height = 100;
        root.Children.Add(none);
        root.Children.Add(noSize);

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());
    }

    /// <summary>The draw scales the alpha of the tint by the opacity. An opacity of 0.5 gives alpha 128.</summary>
    [Fact]
    public void ScalesTheTintByTheOpacity()
    {
        RecordingRenderer renderer = new();
        Image image = AddImage(CreateContext(out UiContext context), FullUv);
        image.Tint = new Rgba8(255, 0, 0);
        image.Inline.Opacity = 0.5f;

        context.Update(0f);
        context.Draw(renderer);

        Assert.Contains("Image 0,0,64,32 texture 7 uv 0,0,1,1 tint #FF000080 radius 0,0,0,0", renderer.Describe(), StringComparison.Ordinal);
    }

    /// <summary>A tint with an alpha of 0 after the scale draws nothing.</summary>
    [Fact]
    public void DrawsNothingForATransparentTint()
    {
        RecordingRenderer renderer = new();
        Image image = AddImage(CreateContext(out UiContext context), FullUv);
        image.Tint = Rgba8.Transparent;

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());
    }

    /// <summary>Each scale mode gives its draw rect and its UV rect for the image of 64 by 32 in a box of 100 by 100.</summary>
    /// <param name="mode">The scale mode.</param>
    /// <param name="expected">The expected image line.</param>
    [Theory]
    [InlineData(ImageScaleMode.StretchToFill, "Image 0,0,100,100 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0")]
    [InlineData(ImageScaleMode.ScaleToFit, "Image 0,25,100,50 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0")]
    [InlineData(ImageScaleMode.ScaleAndCrop, "Image 0,0,100,100 texture 7 uv 0.25,0,0.75,1 tint #FFFFFFFF radius 0,0,0,0")]
    [InlineData((ImageScaleMode)99, "Image 0,0,100,100 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0")]
    public void DrawsEachScaleMode(ImageScaleMode mode, string expected)
    {
        RecordingRenderer renderer = new();
        Image image = AddImage(CreateContext(out UiContext context), FullUv);
        image.ScaleMode = mode;
        image.Inline.Width = 100;
        image.Inline.Height = 100;

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 400x300 scale 1\n" + expected + "\nEnd", renderer.Describe());
    }

    /// <summary>
    /// A UV component that is not finite draws nothing in each scale mode, also in a box with a style size. A set back to
    /// a finite UV rect draws the image again.
    /// </summary>
    /// <param name="mode">The scale mode.</param>
    /// <param name="u0">The first u coordinate.</param>
    /// <param name="v0">The first v coordinate.</param>
    /// <param name="u1">The second u coordinate.</param>
    /// <param name="v1">The second v coordinate.</param>
    [Theory]
    [InlineData(ImageScaleMode.StretchToFill, float.NaN, 0f, 1f, 1f)]
    [InlineData(ImageScaleMode.ScaleToFit, 0f, 0f, float.PositiveInfinity, 1f)]
    [InlineData(ImageScaleMode.ScaleAndCrop, 0f, float.NegativeInfinity, 1f, float.NaN)]
    public void DrawsNothingForAUvThatIsNotFinite(ImageScaleMode mode, float u0, float v0, float u1, float v1)
    {
        RecordingRenderer renderer = new();
        Image image = AddImage(CreateContext(out UiContext context), new float4(u0, v0, u1, v1));
        image.ScaleMode = mode;
        image.Inline.Width = 100;
        image.Inline.Height = 50;

        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());

        RecordingRenderer next = new();
        image.Uv = FullUv;
        context.Update(0f);
        context.Draw(next);

        Assert.Contains("texture 7 uv ", next.Describe(), StringComparison.Ordinal);
    }

    /// <summary>A new id with the same size marks <see cref="DirtyFlags.Paint"/> only.</summary>
    [Fact]
    public void ANewIdWithTheSameSizeMarksPaint()
    {
        Image image = CleanImage();

        image.Texture = new TextureHandle(DemoId + 1, DemoSize);

        Assert.Equal(DirtyFlags.Paint, image.Dirty);
        Assert.Equal(DemoId + 1, image.Texture.Id);
    }

    /// <summary>A new size marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>, and the next update takes it.</summary>
    [Fact]
    public void ANewSizeMarksLayoutAndPaint()
    {
        Image image = CleanImage(out UiContext context);

        image.Texture = new TextureHandle(DemoId, new int2(16, 8));

        Assert.Equal(DirtyFlags.Layout | DirtyFlags.Paint, image.Dirty);
        context.Update(0f);
        Assert.Equal("rect 0,0,16,8", RectLine(image.Layout.Rect));
    }

    /// <summary>
    /// A change from the none handle to a texture with the same size marks <see cref="DirtyFlags.Layout"/>, because
    /// the natural size goes from (0, 0) to the texture size.
    /// </summary>
    [Fact]
    public void AChangeFromTheNoneHandleMarksLayout()
    {
        Image image = CleanImage(out UiContext context);
        image.Texture = new TextureHandle(0, DemoSize);
        context.Update(0f);
        Assert.Equal("rect 0,0,0,0", RectLine(image.Layout.Rect));

        image.Texture = DemoTexture;

        Assert.Equal(DirtyFlags.Layout | DirtyFlags.Paint, image.Dirty);
        context.Update(0f);
        Assert.Equal("rect 0,0,64,32", RectLine(image.Layout.Rect));
    }

    /// <summary>A set to the same id and the same size marks nothing, also with a new handle value.</summary>
    [Fact]
    public void TheSameHandleMarksNothing()
    {
        Image image = CleanImage();

        image.Texture = new TextureHandle(DemoId, new int2(64, 32));

        Assert.Equal(DirtyFlags.None, image.Dirty);
    }

    /// <summary>A new UV rect marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>. The same rect marks nothing.</summary>
    [Fact]
    public void ANewUvMarksLayoutAndPaint()
    {
        Image image = CleanImage(out UiContext context);

        image.Uv = FullUv;
        Assert.Equal(DirtyFlags.None, image.Dirty);

        image.Uv = new float4(0f, 0f, 0.5f, 1f);
        Assert.Equal(DirtyFlags.Layout | DirtyFlags.Paint, image.Dirty);
        context.Update(0f);
        Assert.Equal("rect 0,0,32,32", RectLine(image.Layout.Rect));

        image.Uv = new float4(float.NaN, 0f, 0.5f, 1f);
        context.Update(0f);
        image.Uv = new float4(float.NaN, 0f, 0.5f, 1f);
        Assert.Equal(DirtyFlags.None, image.Dirty);
    }

    /// <summary>A new tint marks <see cref="DirtyFlags.Paint"/>. The same tint marks nothing.</summary>
    [Fact]
    public void ANewTintMarksPaint()
    {
        Image image = CleanImage();

        image.Tint = new Rgba8(255, 255, 255);
        Assert.Equal(DirtyFlags.None, image.Dirty);

        image.Tint = new Rgba8(255, 0, 0);
        Assert.Equal(DirtyFlags.Paint, image.Dirty);
    }

    /// <summary>A new scale mode marks <see cref="DirtyFlags.Paint"/>. The same mode marks nothing.</summary>
    [Fact]
    public void ANewScaleModeMarksPaint()
    {
        Image image = CleanImage();

        image.ScaleMode = ImageScaleMode.StretchToFill;
        Assert.Equal(DirtyFlags.None, image.Dirty);

        image.ScaleMode = ImageScaleMode.ScaleAndCrop;
        Assert.Equal(DirtyFlags.Paint, image.Dirty);
    }

    /// <summary>A wide image fits the width of a square box, and a tall image fits the height. The rect is in the center.</summary>
    [Fact]
    public void FitRectKeepsTheAspectInTheCenter()
    {
        float4 box = new(10f, 20f, 100f, 100f);

        Assert.Equal(new float4(10f, 45f, 100f, 50f), Image.FitRect(box, new float2(64, 32)));
        Assert.Equal(new float4(35f, 20f, 50f, 100f), Image.FitRect(box, new float2(32, 64)));
        Assert.Equal(box, Image.FitRect(box, new float2(50, 50)));
        Assert.Equal(new float4(0f, 50f, 400f, 200f), Image.FitRect(new float4(0f, 0f, 400f, 300f), new float2(2, 1)));
    }

    /// <summary>A wide image in a square box cuts the u span. A tall image in a wide box cuts the v span.</summary>
    [Fact]
    public void CropUvCutsAroundTheCenter()
    {
        Assert.Equal(new float4(0.25f, 0f, 0.75f, 1f), Image.CropUv(FullUv, new float2(64, 32), new float4(0f, 0f, 100f, 100f)));
        Assert.Equal(new float4(0.375f, 0f, 0.625f, 1f), Image.CropUv(FullUv, new float2(64, 32), new float4(0f, 0f, 50f, 100f)));
        Assert.Equal(new float4(0f, 0.375f, 1f, 0.625f), Image.CropUv(FullUv, new float2(32, 64), new float4(0f, 0f, 100f, 50f)));
        Assert.Equal(FullUv, Image.CropUv(FullUv, new float2(64, 32), new float4(0f, 0f, 200f, 100f)));
    }

    /// <summary>The crop scales the signed span, so a flipped UV rect keeps its direction. An atlas region stays inside itself.</summary>
    [Fact]
    public void CropUvKeepsAFlippedUv()
    {
        float4 flipped = new(1f, 1f, 0f, 0f);

        Assert.Equal(new float4(0.75f, 1f, 0.25f, 0f), Image.CropUv(flipped, new float2(64, 32), new float4(0f, 0f, 100f, 100f)));
        Assert.Equal(new float4(1f, 0.625f, 0f, 0.375f), Image.CropUv(flipped, new float2(32, 64), new float4(0f, 0f, 100f, 50f)));
        Assert.Equal(
            new float4(0.125f, 0f, 0.375f, 0.5f),
            Image.CropUv(new float4(0f, 0f, 0.5f, 0.5f), new float2(32, 16), new float4(0f, 0f, 100f, 100f)));
    }

    /// <summary>A natural size with a component of 0, or a box with no area, falls back to the rule of stretch.</summary>
    [Fact]
    public void FallsBackToStretchForASizeOfZero()
    {
        float4 box = new(0f, 0f, 100f, 100f);
        float4 flat = new(5f, 5f, 0f, 40f);

        Assert.Equal(box, Image.FitRect(box, new float2(0, 32)));
        Assert.Equal(box, Image.FitRect(box, new float2(64, 0)));
        Assert.Equal(box, Image.FitRect(box, new float2(float.NaN, 32)));
        Assert.Equal(flat, Image.FitRect(flat, new float2(64, 32)));
        Assert.Equal(FullUv, Image.CropUv(FullUv, new float2(0, 32), box));
        Assert.Equal(FullUv, Image.CropUv(FullUv, new float2(64, 32), flat));
        Assert.Equal(FullUv, Image.CropUv(FullUv, new float2(64, 32), new float4(0f, 0f, float.PositiveInfinity, 10f)));
    }

    /// <summary>A zero UV span with a real texture takes no space, and draws the stretch rule in a box with a style size.</summary>
    [Fact]
    public void DrawsAZeroUvSpanWithTheStretchRule()
    {
        RecordingRenderer renderer = new();
        Image image = AddImage(CreateContext(out UiContext context), new float4(0.5f, 0f, 0.5f, 1f));
        image.ScaleMode = ImageScaleMode.ScaleToFit;

        context.Update(0f);
        Assert.Equal("rect 0,0,0,32", RectLine(image.Layout.Rect));

        image.Inline.Width = 10;
        image.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        context.Draw(renderer);

        Assert.Contains("Image 0,0,10,32 texture 7 uv 0.5,0,0.5,1", renderer.Describe(), StringComparison.Ordinal);
    }

    /// <summary>Creates a context with no measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext { ViewportSize = new float2(400, 300) };
    }

    /// <summary>Creates a context with no measurer, a viewport of 400 by 300, and a root with <see cref="Align.FlexStart"/>.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The root.</returns>
    private static Element CreateContext(out UiContext context)
    {
        context = CreateContext();
        return AddRoot(context);
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

    /// <summary>Adds an image of the demo texture with a UV rect to a root.</summary>
    /// <param name="root">The root.</param>
    /// <param name="uv">The UV rect.</param>
    /// <returns>The image.</returns>
    private static Image AddImage(Element root, float4 uv)
    {
        Image image = new(new Image.CreateInfo { Texture = DemoTexture, Uv = uv });
        root.Children.Add(image);
        return image;
    }

    /// <summary>Makes a clean image of the demo texture: a child of a root after one update.</summary>
    /// <returns>The image.</returns>
    private static Image CleanImage()
    {
        return CleanImage(out _);
    }

    /// <summary>Makes a clean image of the demo texture: a child of a root after one update.</summary>
    /// <param name="context">The context of the image.</param>
    /// <returns>The image.</returns>
    private static Image CleanImage(out UiContext context)
    {
        Image image = AddImage(CreateContext(out context), FullUv);
        context.Update(0f);
        Assert.Equal(DirtyFlags.None, image.Dirty);
        return image;
    }

    /// <summary>Formats a rect as <c>rect x,y,w,h</c> with the invariant culture.</summary>
    /// <param name="rect">The rect.</param>
    /// <returns>The text.</returns>
    private static string RectLine(float4 rect)
    {
        return string.Create(CultureInfo.InvariantCulture, $"rect {rect.X},{rect.Y},{rect.Z},{rect.W}");
    }
}
