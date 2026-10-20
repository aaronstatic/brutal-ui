using System;
using System.Text;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the paint hook <c>Element.PaintContent</c> in the draw walk. A private probe element overrides the hook
/// and sends one <c>Text</c> call.
/// </summary>
/// <remarks>
/// The hook comes after the Rect and the clip push of the element, and before its children. The test assembly sees
/// the internal member through <c>InternalsVisibleTo</c>.
/// </remarks>
public sealed class PaintContentTests
{
    private static readonly Rgba8 Background = new(10, 20, 30);
    private static readonly Rgba8 Green = new(0, 255, 0);

    /// <summary>
    /// The demo of phase 4: a clipped probe with a child. The order is the Rect, the clip push, the text of the hook,
    /// the Rect of the child, and the clip pop.
    /// </summary>
    [Fact]
    public void DrawsContentBetweenTheBackgroundAndTheChildren()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        ContentProbe probe = new() { Name = "probe" };
        probe.Style.Width = 100;
        probe.Style.Height = 50;
        probe.Style.Overflow = Overflow.Hidden;
        probe.Style.BackgroundColor = Background;
        context.AddRoot(probe);

        Element child = new();
        child.Style.Width = 20;
        child.Style.Height = 10;
        child.Style.BackgroundColor = Green;
        probe.Children.Add(child);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        string actual = renderer.Describe();
        TestContext.Current.TestOutputHelper?.WriteLine(actual);

        string expected = string.Join(
            "\n",
            "Begin 400x300 scale 1",
            "Rect 0,0,100,50 fill #0A141EFF border 0 #00000000 radius 0,0,0,0",
            "PushClip 0,0,100,50",
            "Text 0,0 font 0 size 16 #FFFFFFFF \"hi\"",
            "Rect 0,0,20,10 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
            "PopClip",
            "End");
        Assert.Equal(expected, actual);
        Assert.Equal(1, probe.CallCount);
    }

    /// <summary>The hook gets the border box in viewport pixels: the sum of the parent positions and its own.</summary>
    [Fact]
    public void CallsTheHookWithTheViewportBorderBox()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new();
        root.Style.Width = 200;
        root.Style.Height = 100;
        root.Style.Padding = LengthEdges.All(10);
        context.AddRoot(root);

        ContentProbe probe = new();
        probe.Style.Width = 40;
        probe.Style.Height = 20;
        probe.Style.Margin.Left = 2;
        probe.Style.Margin.Top = 3;
        root.Children.Add(probe);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(1, probe.CallCount);
        Assert.Equal(new float4(12, 13, 40, 20), probe.LastBorderBox);
    }

    /// <summary>The hook gets the product of the opacities down the tree, and the probe scales its color with it.</summary>
    [Fact]
    public void PassesTheOpacityProduct()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new();
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.Opacity = 0.5f;
        context.AddRoot(root);

        ContentProbe probe = new();
        probe.Style.Width = 40;
        probe.Style.Height = 20;
        probe.Style.Opacity = 0.5f;
        root.Children.Add(probe);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(0.25f, probe.LastOpacity);

        // 255 * 0.25 + 0.5 = 64.25, which truncates to 64 (the same rule as the fill).
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        Assert.Equal(3, calls.Length);
        Assert.Equal(DrawCallKind.Text, calls[1].Kind);
        Assert.Equal(new Rgba8(255, 255, 255, 64), calls[1].Color);
    }

    /// <summary>The walk does not call the hook for a hidden element or for a hidden subtree.</summary>
    [Fact]
    public void CallsNoHookForAHiddenElement()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new();
        root.Style.Width = 100;
        root.Style.Height = 100;
        context.AddRoot(root);

        ContentProbe hidden = new();
        hidden.Style.Width = 40;
        hidden.Style.Height = 20;
        hidden.Style.Display = Display.None;
        root.Children.Add(hidden);

        ContentProbe faded = new();
        faded.Style.Width = 40;
        faded.Style.Height = 20;
        faded.Style.Opacity = 0f;
        root.Children.Add(faded);

        Element hiddenParent = new();
        hiddenParent.Style.Display = Display.None;
        root.Children.Add(hiddenParent);

        ContentProbe inHidden = new();
        inHidden.Style.Width = 40;
        inHidden.Style.Height = 20;
        hiddenParent.Children.Add(inHidden);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(0, hidden.CallCount);
        Assert.Equal(0, faded.CallCount);
        Assert.Equal(0, inHidden.CallCount);
        Assert.Equal(2, renderer.Calls.Length);
    }

    /// <summary>The recorder keeps the UTF-8 bytes of the text call, and <c>GetText</c> returns them.</summary>
    [Fact]
    public void RecordsTheTextBytes()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        ContentProbe probe = new();
        probe.Style.Width = 100;
        probe.Style.Height = 50;
        probe.Style.BackgroundColor = Background;
        context.AddRoot(probe);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        Assert.Equal(4, calls.Length);
        Assert.Equal(DrawCallKind.Text, calls[2].Kind);

        ReadOnlySpan<byte> text = renderer.GetText(in calls[2]);
        Assert.True(text.SequenceEqual("hi"u8));
        Assert.Equal(2, text.Length);
        Assert.Equal("hi", Encoding.UTF8.GetString(text));
    }

    /// <summary>
    /// The recorder keeps the wrap width of the text call. The description shows a wrap width above 0 before the text.
    /// </summary>
    [Fact]
    public void RecordsTheWrapWidth()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        ContentProbe probe = new() { WrapWidth = 70f };
        probe.Style.Width = 100;
        probe.Style.Height = 50;
        context.AddRoot(probe);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        Assert.Equal(3, calls.Length);
        Assert.Equal(DrawCallKind.Text, calls[1].Kind);
        Assert.Equal(70f, calls[1].WrapWidth);

        string expected = string.Join(
            "\n",
            "Begin 400x300 scale 1",
            "Text 0,0 font 0 size 16 #FFFFFFFF wrap 70 \"hi\"",
            "End");
        Assert.Equal(expected, renderer.Describe());
    }

    /// <summary>
    /// A recorded wrap width of 0 or less, or <c>NaN</c>, means no wrap. The description of such a call has no wrap
    /// part, so the golden lines of the earlier tests stay the same.
    /// </summary>
    /// <param name="wrapWidth">The wrap width of the hook.</param>
    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    [InlineData(float.NaN)]
    public void DescribesNoWrapForAWidthOfZeroOrLess(float wrapWidth)
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        ContentProbe probe = new() { WrapWidth = wrapWidth };
        probe.Style.Width = 100;
        probe.Style.Height = 50;
        context.AddRoot(probe);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        // A compare of the bits also holds for NaN.
        Assert.Equal(BitConverter.SingleToInt32Bits(wrapWidth), BitConverter.SingleToInt32Bits(renderer.Calls[1].WrapWidth));
        Assert.Contains("Text 0,0 font 0 size 16 #FFFFFFFF \"hi\"", renderer.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A probe element. The hook records its arguments and draws the text "hi" at the top-left corner of the border
    /// box, with font 0, size 16, a white color scaled by the opacity, and the wrap width of <see cref="WrapWidth"/>.
    /// </summary>
    private sealed class ContentProbe : Element
    {
        private static readonly Rgba8 White = new(255, 255, 255);

        /// <summary>Gets the count of hook calls.</summary>
        public int CallCount { get; private set; }

        /// <summary>Gets the border box of the last hook call.</summary>
        public float4 LastBorderBox { get; private set; }

        /// <summary>Gets the opacity of the last hook call.</summary>
        public float LastOpacity { get; private set; }

        /// <summary>Gets or sets the wrap width that the hook gives to the text call. The default is 0, no wrap.</summary>
        public float WrapWidth { get; set; }

        /// <inheritdoc/>
        internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
        {
            CallCount++;
            LastBorderBox = borderBox;
            LastOpacity = opacity;

            float2 position = new(borderBox.X, borderBox.Y);
            Rgba8 color = White.ScaleAlpha(opacity);
            renderer.Text(in position, new FontHandle(0), 16f, in color, "hi"u8, WrapWidth);
        }
    }
}
