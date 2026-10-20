using System;
using System.Text;
using Brutal.Localization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation gate of the label and the image. After warm-up, a frame of label and image changes, one
/// <c>Update</c>, and one <c>Draw</c> must allocate 0 bytes. The same holds for a text change from a string and for
/// <see cref="UiContext.RefreshLocalizedText"/> after a locale change.
/// </summary>
/// <remarks>
/// <para>
/// The pattern is the one of <see cref="ThousandElementAllocationTests"/>: the test builds the scene, runs
/// <see cref="WarmUpFrames"/> warm-up frames, and then measures <see cref="MeasuredFrames"/> frames. Each frame reads
/// <see cref="GC.GetAllocatedBytesForCurrentThread"/> right before and right after the measured body, and the test adds
/// the differences.
/// </para>
/// <para>
/// The scene: a <see cref="RecordingRenderer"/> is the measurer and the renderer. A root column holds
/// <see cref="LabelCount"/> labels, <see cref="ImageCount"/> images, and one label with an <see cref="LString"/>. Each
/// label has a padding of 4 and a width of 120, so its text wraps in a content box of 112 pixels (14 bytes at size 16,
/// 16 bytes at size 14). Each image has a width of 32, so its height comes from the image measure.
/// </para>
/// <para>
/// The measured body changes <see cref="ChangedCount"/> labels, <see cref="ChangedCount"/> images, and the label with
/// the <see cref="LString"/> in each frame. On a label, it sets the text, the color, the size, and the font. On an
/// image, it sets the tint, the UV rect, the texture, and the scale mode. On the last label, it sets one of two
/// <see cref="LString"/> instances. The frames take the first value and the second value in turn, so each frame is a
/// real change. A new UV span runs the image measure, and a new size or font runs the label measure. The warm-up uses
/// both values, so no buffer grows in a measured frame.
/// </para>
/// </remarks>
public sealed class ElementAllocationTests
{
    private const int LabelCount = 50;
    private const int ImageCount = 50;
    private const int ChangedCount = 10;
    private const int WarmUpFrames = 3;
    private const int MeasuredFrames = 100;
    private const float LabelWidth = 120f;
    private const float LabelPadding = 4f;
    private const float WrapWidth = 112f;
    private const float FirstSize = 16f;
    private const float SecondSize = 14f;
    private const float ImageWidth = 32f;
    private const int FirstTextureId = 7;
    private const int SecondTextureId = 8;

    /// <summary>The text of each label at creation.</summary>
    private const string InitialText = "A label that wraps in its box";

    private static readonly int2 TextureSize = new(16, 16);
    private static readonly Rgba8 White = new(255, 255, 255);
    private static readonly Rgba8 FirstTint = new(255, 200, 100);
    private static readonly Rgba8 SecondTint = new(100, 200, 255);
    private static readonly Rgba8 FirstColor = new(255, 240, 200);
    private static readonly Rgba8 SecondColor = new(200, 240, 255);
    private static readonly FontHandle FirstFont = default;
    private static readonly FontHandle SecondFont = new(1);
    private static readonly TextureHandle FirstTexture = new(FirstTextureId, TextureSize);
    private static readonly TextureHandle SecondTexture = new(SecondTextureId, TextureSize);

    /// <summary>The UV rect of the even frames: the full texture, a natural size of 16 by 16.</summary>
    private static readonly float4 FirstUv = new(0f, 0f, 1f, 1f);

    /// <summary>The UV rect of the odd frames: the top half of the texture, a natural size of 16 by 8.</summary>
    private static readonly float4 SecondUv = new(0f, 0f, 1f, 0.5f);

    /// <summary>
    /// The UV rect that <see cref="ImageScaleMode.ScaleAndCrop"/> draws for <see cref="SecondUv"/> in a box of 32 by 8.
    /// The box is wider than the image, so the cut keeps the center half of the v span.
    /// </summary>
    private static readonly float4 SecondCroppedUv = new(0f, 0.125f, 1f, 0.375f);

    /// <summary>The text of the even frames: 25 bytes, two lines at a wrap width of 112 in both sizes.</summary>
    private static ReadOnlySpan<byte> FirstText => "The first text of a label"u8;

    /// <summary>The text of the odd frames: 24 bytes, two lines at a wrap width of 112 in both sizes.</summary>
    private static ReadOnlySpan<byte> SecondText => "A second text that wraps"u8;

    /// <summary>The default text of <see cref="Scene.LocalizedText"/>.</summary>
    private static ReadOnlySpan<byte> FirstLocalizedText => "Hello"u8;

    /// <summary>The default text of <see cref="Scene.OtherLocalizedText"/>. It has the length of the first text.</summary>
    private static ReadOnlySpan<byte> SecondLocalizedText => "World"u8;

    /// <summary>The context, the elements, and the frame counter. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        /// <summary>The renderer that measures the text and records the calls of <c>Draw</c>.</summary>
        public required RecordingRenderer Renderer { get; init; }

        /// <summary>The labels with a plain text, in tree order.</summary>
        public required Label[] Labels { get; init; }

        /// <summary>The images, in tree order.</summary>
        public required Image[] Images { get; init; }

        /// <summary>The label with an <see cref="LString"/>. It is the last child of the root.</summary>
        public required Label Localized { get; init; }

        /// <summary>The <see cref="LString"/> of <see cref="Localized"/> at creation, and of the even frames.</summary>
        public required LString LocalizedText { get; init; }

        /// <summary>The <see cref="LString"/> of the odd frames.</summary>
        public required LString OtherLocalizedText { get; init; }

        /// <summary>The number of frames that ran. An even number takes the first value, an odd number the second.</summary>
        public int FrameCount { get; set; }
    }

    /// <summary>
    /// The label changes (a text from a span, the color, the size, the font, and an <see cref="LString"/>), the image
    /// changes (the tint, the UV rect, the texture, and the scale mode), <c>Update</c>, and <c>Draw</c> allocate nothing
    /// after warm-up.
    /// </summary>
    [Fact]
    public void UpdateAndDrawAllocateNothingAfterWarmUp()
    {
        Scene scene = BuildScene();
        for (int i = 0; i < WarmUpFrames; i++)
        {
            MeasureFrame(scene);
        }

        int measuresBefore = scene.Renderer.MeasureCount;
        long bytes = 0;
        for (int i = 0; i < MeasuredFrames; i++)
        {
            bytes += MeasureFrame(scene);
        }

        WriteLine("frames " + MeasuredFrames + ", bytes " + bytes + ", measures "
            + (scene.Renderer.MeasureCount - measuresBefore));
        Assert.Equal(0L, bytes);
    }

    /// <summary>A text change from a string and <c>Update</c> allocate nothing after warm-up.</summary>
    [Fact]
    public void SetTextFromAStringAllocatesNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        // The strings exist before the loop. The decode makes two strings with the bytes of the span texts.
        string first = Encoding.UTF8.GetString(FirstText);
        string second = Encoding.UTF8.GetString(SecondText);

        for (int i = 0; i < WarmUpFrames; i++)
        {
            MeasureStringFrame(scene, first, second);
        }

        int measuresBefore = scene.Renderer.MeasureCount;
        long bytes = 0;
        for (int i = 0; i < MeasuredFrames; i++)
        {
            bytes += MeasureStringFrame(scene, first, second);
        }

        // The body is a real change: each frame measures the changed labels again.
        Assert.True(scene.Renderer.MeasureCount - measuresBefore >= MeasuredFrames * ChangedCount);
        Assert.Equal(0L, bytes);
    }

    /// <summary>
    /// <see cref="UiContext.RefreshLocalizedText"/> after a locale change allocates nothing. Each measured refresh finds
    /// a new string reference with a new content, so it takes the slow path: it encodes the text and marks the label.
    /// </summary>
    [Fact]
    public void RefreshLocalizedTextAllocatesNothing()
    {
        Scene scene = BuildScene();
        scene.Context.Update(0.016f);

        // The two locale strings exist before the loop. They have the same length, so the buffer of the label does
        // not grow in a measured refresh.
        string first = Encoding.UTF8.GetString("Bonjour"u8);
        string second = Encoding.UTF8.GetString("Bonsoir"u8);

        for (int i = 0; i < WarmUpFrames; i++)
        {
            MeasureLocaleChange(scene, (i & 1) == 0 ? first : second);
        }

        long bytes = 0;
        for (int i = 0; i < MeasuredFrames; i++)
        {
            bool odd = ((WarmUpFrames + i) & 1) == 1;
            bytes += MeasureLocaleChange(scene, odd ? second : first);

            // The refresh took the slow path: the label shows the new text, and the layout pass measures it again.
            Assert.True(
                scene.Localized.Text.SequenceEqual(odd ? "Bonsoir"u8 : "Bonjour"u8),
                "Refresh " + i + " shows the old text.");
            int measuresBefore = scene.Renderer.MeasureCount;
            scene.Context.Update(0.016f);
            Assert.True(scene.Renderer.MeasureCount > measuresBefore, "Refresh " + i + " marks no layout.");
        }

        Assert.Equal(0L, bytes);
    }

    /// <summary>
    /// The measured body of <see cref="UpdateAndDrawAllocateNothingAfterWarmUp"/> does real work. Each frame measures
    /// the changed labels again, runs the image measure, and draws the new values. The dump of a frame holds 51
    /// <c>Text</c> lines and 50 <c>Image</c> lines.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        for (int i = 0; i < WarmUpFrames; i++)
        {
            MeasureFrame(scene);
        }

        for (int i = 0; i < MeasuredFrames; i++)
        {
            bool odd = (scene.FrameCount & 1) == 1;
            int before = scene.Renderer.MeasureCount;
            MeasureFrame(scene);

            // Each changed label is dirty, so the layout pass calls its measure callback one time or more. The label
            // with the LString changes too.
            Assert.True(
                scene.Renderer.MeasureCount >= before + ChangedCount + 1,
                "Frame " + i + " measures " + (scene.Renderer.MeasureCount - before) + " times.");
            AssertFrame(scene, odd, i);
        }

        string[] lines = scene.Renderer.Describe().Split('\n');
        int texts = 0;
        int images = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            texts += lines[i].StartsWith("Text ", StringComparison.Ordinal) ? 1 : 0;
            images += lines[i].StartsWith("Image ", StringComparison.Ordinal) ? 1 : 0;
        }

        WriteLine("text lines " + texts + ", image lines " + images);
        Assert.Equal(LabelCount + 1, texts);
        Assert.Equal(ImageCount, images);

        // The changed label wraps in its content box: 4 pixels in, at a width of 112. The last frame number gives
        // the text, the font, the size, and the color.
        bool lastOdd = ((scene.FrameCount - 1) & 1) == 1;
        string expected = lastOdd
            ? "Text 4,4 font 1 size 14 " + SecondColor + " wrap 112 \"A second text that wraps\""
            : "Text 4,4 font 0 size 16 " + FirstColor + " wrap 112 \"The first text of a label\"";
        Assert.Equal(expected, lines[1]);
    }

    /// <summary>Builds the scene. The scene runs no <c>Update</c>.</summary>
    private static Scene BuildScene()
    {
        RecordingRenderer renderer = new();
        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer })
        {
            ViewportSize = new float2(400, 3000),
        };

        Element root = new();
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.AlignItems = Align.FlexStart;
        context.AddRoot(root);

        Label[] labels = new Label[LabelCount];
        for (int i = 0; i < labels.Length; i++)
        {
            Label label = new(new Label.CreateInfo { Color = White, Size = FirstSize, Text = InitialText });
            label.Style.Width = LabelWidth;
            label.Style.Padding = LengthEdges.All(LabelPadding);
            root.Children.Add(label);
            labels[i] = label;
        }

        Image[] images = new Image[ImageCount];
        for (int i = 0; i < images.Length; i++)
        {
            Image image = new(new Image.CreateInfo { Texture = FirstTexture });
            image.Style.Width = ImageWidth;
            root.Children.Add(image);
            images[i] = image;
        }

        LString hello = new("element-allocation-tests.localized", "Hello");
        LString world = new("element-allocation-tests.other-localized", "World");
        Label localized = new(new Label.CreateInfo { Color = White, Size = FirstSize, LocalizedText = hello });
        root.Children.Add(localized);

        return new Scene
        {
            Context = context,
            Renderer = renderer,
            Labels = labels,
            Images = images,
            Localized = localized,
            LocalizedText = hello,
            OtherLocalizedText = world,
        };
    }

    /// <summary>Runs one frame of the span body and gives the managed bytes of the body.</summary>
    private static long MeasureFrame(Scene scene)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        Frame(scene);
        long after = GC.GetAllocatedBytesForCurrentThread();
        return after - before;
    }

    /// <summary>
    /// The measured body of the span test: new values on <see cref="ChangedCount"/> labels, on
    /// <see cref="ChangedCount"/> images, and on the label with the <see cref="LString"/>, then <c>Update</c> and
    /// <c>Draw</c>. It uses no lambda, no boxing, and no Assert.
    /// </summary>
    private static void Frame(Scene scene)
    {
        bool odd = (scene.FrameCount++ & 1) == 1;
        ReadOnlySpan<byte> text = odd ? SecondText : FirstText;
        Rgba8 color = odd ? SecondColor : FirstColor;
        float size = odd ? SecondSize : FirstSize;
        FontHandle font = odd ? SecondFont : FirstFont;
        Rgba8 tint = odd ? SecondTint : FirstTint;
        float4 uv = odd ? SecondUv : FirstUv;
        TextureHandle texture = odd ? SecondTexture : FirstTexture;
        ImageScaleMode mode = odd ? ImageScaleMode.ScaleAndCrop : ImageScaleMode.ScaleToFit;
        Label[] labels = scene.Labels;
        Image[] images = scene.Images;
        for (int i = 0; i < ChangedCount; i++)
        {
            Label label = labels[i];
            label.SetText(text);
            label.Color = color;
            label.Size = size;
            label.Font = font;

            Image image = images[i];
            image.Tint = tint;
            image.Uv = uv;
            image.Texture = texture;
            image.ScaleMode = mode;
        }

        scene.Localized.SetText(odd ? scene.OtherLocalizedText : scene.LocalizedText);
        scene.Context.Update(0.016f);
        scene.Context.Draw(scene.Renderer);
    }

    /// <summary>
    /// Runs one frame of the string body: a new text from a string on <see cref="ChangedCount"/> labels, and
    /// <c>Update</c>. It gives the managed bytes of the body.
    /// </summary>
    private static long MeasureStringFrame(Scene scene, string first, string second)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        bool odd = (scene.FrameCount++ & 1) == 1;
        string text = odd ? second : first;
        Label[] labels = scene.Labels;
        for (int i = 0; i < ChangedCount; i++)
        {
            labels[i].SetText(text);
        }

        scene.Context.Update(0.016f);
        long after = GC.GetAllocatedBytesForCurrentThread();
        return after - before;
    }

    /// <summary>
    /// Changes the locale string of the <see cref="LString"/> outside the measured region, then runs one
    /// <see cref="UiContext.RefreshLocalizedText"/>. It gives the managed bytes of the refresh.
    /// </summary>
    private static long MeasureLocaleChange(Scene scene, string localized)
    {
        scene.LocalizedText.Localize(localized);
        long before = GC.GetAllocatedBytesForCurrentThread();
        scene.Context.RefreshLocalizedText();
        long after = GC.GetAllocatedBytesForCurrentThread();
        return after - before;
    }

    /// <summary>
    /// Checks the draw calls and the layout of one frame. The paint walk goes in tree order, so the first text call
    /// belongs to the first label, the first image call to the first image, and the last text call to the label with
    /// the <see cref="LString"/>.
    /// </summary>
    /// <param name="scene">The scene after the frame.</param>
    /// <param name="odd">Whether the frame took the second values.</param>
    /// <param name="frame">The number of the measured frame, for the message.</param>
    private static void AssertFrame(Scene scene, bool odd, int frame)
    {
        RecordingRenderer renderer = scene.Renderer;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        int firstText = -1;
        int lastText = -1;
        int firstImage = -1;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Text)
            {
                firstText = firstText < 0 ? i : firstText;
                lastText = i;
            }

            if (firstImage < 0 && calls[i].Kind == DrawCallKind.Image)
            {
                firstImage = i;
            }
        }

        Assert.True(firstText >= 0 && firstImage >= 0, "Frame " + frame + " draws no text or no image.");

        // The label setters: the text, the color, the size, and the font reach the draw.
        ref readonly DrawCall text = ref calls[firstText];
        float size = odd ? SecondSize : FirstSize;
        Assert.True(renderer.GetText(in text).SequenceEqual(odd ? SecondText : FirstText));
        Assert.Equal(odd ? SecondColor : FirstColor, text.Color);
        Assert.Equal(size, text.Size);
        Assert.Equal((odd ? SecondFont : FirstFont).Id, text.Font.Id);
        Assert.Equal(WrapWidth, text.WrapWidth);

        // The label measure ran with the new size: the text wraps to two lines in both sizes.
        float4 labelRect = scene.Labels[0].Layout.Rect;
        Assert.Equal(LabelWidth, labelRect.Z);
        Assert.Equal((2 * size) + (2 * LabelPadding), labelRect.W);

        // SetText(LString): the last label shows the default text of the LString of the frame.
        Assert.True(renderer.GetText(in calls[lastText])
            .SequenceEqual(odd ? SecondLocalizedText : FirstLocalizedText));

        // The image setters: the tint, the texture, the UV rect, and the scale mode reach the draw.
        ref readonly DrawCall image = ref calls[firstImage];
        float4 imageRect = scene.Images[0].Layout.Rect;
        Assert.Equal(odd ? SecondTint : FirstTint, image.Tint);
        Assert.Equal(odd ? SecondTextureId : FirstTextureId, image.Texture.Id);

        // The image measure ran with the new UV span: the height is the natural height, 16 or 8.
        Assert.Equal(ImageWidth, imageRect.Z);
        Assert.Equal(odd ? 8f : 16f, imageRect.W);
        if (odd)
        {
            // ScaleAndCrop fills the box of 32 by 8 and cuts the v span.
            Assert.Equal(new float4(imageRect.X, imageRect.Y, ImageWidth, 8f), image.Rect);
            Assert.Equal(SecondCroppedUv, image.Uv);
        }
        else
        {
            // ScaleToFit puts a rect of 16 by 16 in the center of the box of 32 by 16.
            Assert.Equal(new float4(imageRect.X + 8f, imageRect.Y, 16f, 16f), image.Rect);
            Assert.Equal(FirstUv, image.Uv);
        }
    }

    /// <summary>Writes a line to the live output of the test.</summary>
    private static void WriteLine(string line)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(line);
    }
}
