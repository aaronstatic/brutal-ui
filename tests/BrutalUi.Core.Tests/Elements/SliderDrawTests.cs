using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the draw of <see cref="Slider"/>: the track is the box of the slider, and the thumb is one rect in the
/// content box with the <see cref="ComputedStyle.Color"/> of the slider.
/// </summary>
/// <remarks>
/// The scene is <see cref="SliderTree"/>: the slider is 120 x 20 at (40, 0) with the range 0 to 100. The thumb is
/// 20 x 20, so the value <c>v</c> puts the thumb left edge at <c>40 + v</c>. The tests use the type id <c>Slider</c>,
/// which the slider interns. The tests intern no class.
/// </remarks>
public sealed class SliderDrawTests
{
    /// <summary>The track fill.</summary>
    private static readonly Rgba8 TrackColor = new(16, 32, 48, 255);

    /// <summary>The thumb color of the base rule.</summary>
    private static readonly Rgba8 ThumbColor = new(200, 100, 50, 255);

    /// <summary>The thumb color of the active rule.</summary>
    private static readonly Rgba8 ActiveColor = new(10, 220, 30, 255);

    /// <summary>
    /// The demo of phase 3. A slider with the value 25 draws the track (40, 0, 120, 20) in the background color, and then
    /// the thumb (65, 0, 20, 20) in the color.
    /// </summary>
    [Fact]
    public void TheSliderDrawsTheTrackAndTheThumb()
    {
        SliderTree tree = new();
        tree.Context.AddSheet(BaseSheet());
        tree.Slider.Value = 25f;
        RecordingRenderer renderer = Draw(tree);

        TestContext.Current.TestOutputHelper?.WriteLine(renderer.Describe());

        Assert.Equal(2, CountRects(renderer));
        DrawCall track = RectAt(renderer, 0);
        DrawCall thumb = RectAt(renderer, 1);
        Assert.Equal(new float4(40f, 0f, 120f, 20f), track.Rect);
        Assert.Equal(TrackColor, track.Color);
        Assert.Equal(new float4(65f, 0f, 20f, 20f), thumb.Rect);
        Assert.Equal(ThumbColor, thumb.Color);
        Assert.Equal(0f, thumb.BorderWidth);
        Assert.Equal(Rgba8.Transparent, thumb.BorderColor);
    }

    /// <summary>The thumb moves over the travel: the values 0, 50, and 100 give the x 40, 90, and 140.</summary>
    [Fact]
    public void TheThumbFollowsTheValue()
    {
        SliderTree tree = new();
        tree.Context.AddSheet(BaseSheet());

        float[] values = { 0f, 50f, 100f };
        float[] expected = { 40f, 90f, 140f };
        for (int i = 0; i < values.Length; i++)
        {
            tree.Slider.Value = values[i];
            RecordingRenderer renderer = Draw(tree);
            DrawCall thumb = RectAt(renderer, 1);
            Assert.Equal(expected[i], thumb.Rect.X);
            Assert.Equal(0f, thumb.Rect.Y);
            Assert.Equal(20f, thumb.Rect.Z);
            Assert.Equal(20f, thumb.Rect.W);
        }
    }

    /// <summary>
    /// A padding of 2 and a border of 1 give the content box (43, 3, 114, 14). The thumb is 14 x 14 inside that box, and a
    /// press maps through the same box. The ancestor case puts the slider in a wrapper with a padding of 7 and an offset
    /// from the root: the press, the map, and the draw all use the viewport origin that sums the ancestors.
    /// </summary>
    [Fact]
    public void TheThumbFitsTheContentBox()
    {
        SliderTree tree = new();
        tree.Context.AddSheet(BaseSheet());
        tree.Slider.Inline.Padding = LengthEdges.All(2f);
        tree.Slider.Inline.BorderWidth = LengthEdges.All(1f);

        RecordingRenderer renderer = Draw(tree);
        Assert.Equal(new float4(40f, 0f, 120f, 20f), tree.Slider.Layout.Rect);
        Assert.Equal(new float4(43f, 3f, 14f, 14f), RectAt(renderer, 1).Rect);

        tree.Slider.Value = 100f;
        renderer = Draw(tree);
        Assert.Equal(new float4(143f, 3f, 14f, 14f), RectAt(renderer, 1).Rect);

        // The thumb center sits at 43 + 7 + 100 * r. A press at x 80 gives r = 0.3.
        tree.Context.PushEvent(PointerTree.Down(new float2(80f, 10f), PointerButton.Left));
        tree.Context.Update(0f);
        Assert.Equal(30f, tree.Slider.Value);

        renderer = Draw(tree);
        Assert.Equal(new float4(73f, 3f, 14f, 14f), RectAt(renderer, 1).Rect);

        // The ancestor case: the root holds a lead of 40 x 40 and a wrapper with a padding of 7. The slider sits at
        // (7, 7) in the wrapper, and the wrapper sits at (40, 0) in the root, so the slider origin is (47, 7).
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        Element lead = new() { Name = "lead" };
        lead.Inline.Width = 40;
        lead.Inline.Height = 40;
        Element wrapper = new() { Name = "wrapper" };
        wrapper.Inline.Padding = LengthEdges.All(7f);
        Slider slider = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 0f });
        slider.Inline.Width = 120;
        slider.Inline.Height = 20;
        wrapper.Children.Add(slider);
        root.Children.Add(lead);
        root.Children.Add(wrapper);
        context.AddRoot(root);
        context.AddSheet(BaseSheet());
        context.Update(0f);

        Assert.Equal(new float4(7f, 7f, 120f, 20f), slider.Layout.Rect);
        Assert.Equal(new float4(47f, 7f, 120f, 20f), PointerTree.ViewportRect(slider));

        // The thumb is 20 wide, so its center runs from x 57 to x 157. The value 25 sits at x 82.
        context.PushEvent(PointerTree.Down(new float2(82f, 17f), PointerButton.Left));
        context.Update(0f);
        Assert.Equal(25f, slider.Value);

        RecordingRenderer nested = new();
        context.Draw(nested);
        Assert.Equal(new float4(47f, 7f, 120f, 20f), RectAt(nested, 0).Rect);
        Assert.Equal(new float4(72f, 7f, 20f, 20f), RectAt(nested, 1).Rect);
    }

    /// <summary>
    /// A <c>Slider:active</c> rule with a color shows on the thumb after a press and an update. The base color shows
    /// again after the release.
    /// </summary>
    [Fact]
    public void AnActiveRuleRestylesTheThumb()
    {
        SliderTree tree = new();
        Selector active = Selector.For(new CompoundSelector
        {
            Type = ElementType.Intern("Slider"),
            States = PseudoStates.Active,
        });
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = ElementType.Intern("Slider") }),
                new StyleProperties { BackgroundColor = TrackColor, Color = ThumbColor })
            .Add(active, new StyleProperties { Color = ActiveColor })
            .Build();
        tree.Context.AddSheet(sheet);

        Assert.Equal(ThumbColor, RectAt(Draw(tree), 1).Color);

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(50f), PointerButton.Left));
        tree.Context.Update(0f);
        RecordingRenderer renderer = new();
        tree.Context.Draw(renderer);
        Assert.Equal(PseudoStates.Active, tree.Slider.States & PseudoStates.Active);
        Assert.Equal(ActiveColor, RectAt(renderer, 1).Color);
        Assert.Equal(90f, RectAt(renderer, 1).Rect.X);

        tree.Context.PushEvent(PointerTree.Up(SliderTree.At(50f), PointerButton.Left));
        tree.Context.Update(0f);
        renderer = new RecordingRenderer();
        tree.Context.Draw(renderer);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Active);
        Assert.Equal(ThumbColor, RectAt(renderer, 1).Color);
    }

    /// <summary>An opacity of 0.5 halves the alpha of the thumb. A transparent color draws no thumb rect.</summary>
    [Fact]
    public void TheOpacityScalesTheThumb()
    {
        SliderTree tree = new();
        tree.Context.AddSheet(BaseSheet());
        tree.Slider.Inline.Opacity = 0.5f;

        RecordingRenderer renderer = Draw(tree);
        Assert.Equal(2, CountRects(renderer));
        DrawCall thumb = RectAt(renderer, 1);
        Assert.Equal(new Rgba8(ThumbColor.R, ThumbColor.G, ThumbColor.B, 128), thumb.Color);
        Assert.Equal((byte)128, RectAt(renderer, 0).Color.A);

        tree.Slider.Inline.Opacity = 1f;
        tree.Slider.Inline.Color = Rgba8.Transparent;
        renderer = Draw(tree);
        Assert.Equal(1, CountRects(renderer));
        Assert.Equal(new float4(40f, 0f, 120f, 20f), RectAt(renderer, 0).Rect);
    }

    /// <summary>
    /// A zero range draws the thumb at the start. A slider narrower than tall draws a thumb of the content width. A
    /// slider of 0 x 0 draws no thumb and gives no NaN.
    /// </summary>
    [Fact]
    public void ASmallSliderDrawsASafeThumb()
    {
        SliderTree zero = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 0f, Value = 0f });
        zero.Context.AddSheet(BaseSheet());
        DrawCall zeroThumb = RectAt(Draw(zero), 1);
        Assert.Equal(new float4(40f, 0f, 20f, 20f), zeroThumb.Rect);

        SliderTree narrow = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 100f });
        narrow.Context.AddSheet(BaseSheet());
        narrow.Slider.Inline.Width = 10;
        narrow.Slider.Inline.Height = 20;
        DrawCall narrowThumb = RectAt(Draw(narrow), 1);
        Assert.Equal(new float4(40f, 0f, 10f, 20f), narrowThumb.Rect);

        SliderTree empty = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 50f });
        empty.Context.AddSheet(BaseSheet());
        empty.Slider.Inline.Width = 0;
        empty.Slider.Inline.Height = 0;
        RecordingRenderer renderer = Draw(empty);

        // The paint walk still sends the track rect with no area, and the renderer skips it. The thumb sends no rect.
        Assert.Equal(1, CountRects(renderer));
        Assert.Equal(TrackColor, RectAt(renderer, 0).Color);
        Assert.Equal(new float4(40f, 0f, 0f, 0f), RectAt(renderer, 0).Rect);
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            Assert.False(float.IsNaN(calls[i].Rect.X) || float.IsNaN(calls[i].Rect.Y));
            Assert.False(float.IsNaN(calls[i].Rect.Z) || float.IsNaN(calls[i].Rect.W));
        }
    }

    /// <summary>Builds the sheet with one <c>Slider</c> rule: the track fill and the thumb color.</summary>
    /// <returns>The sheet.</returns>
    private static StyleSheet BaseSheet()
    {
        return new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = ElementType.Intern("Slider") }),
                new StyleProperties { BackgroundColor = TrackColor, Color = ThumbColor })
            .Build();
    }

    /// <summary>Runs one update and one draw of the tree.</summary>
    /// <param name="tree">The tree.</param>
    /// <returns>The renderer with the calls of the frame.</returns>
    private static RecordingRenderer Draw(SliderTree tree)
    {
        RecordingRenderer renderer = new();
        tree.Context.Update(0f);
        tree.Context.Draw(renderer);
        return renderer;
    }

    /// <summary>Counts the rect calls of a frame.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The count.</returns>
    private static int CountRects(RecordingRenderer renderer)
    {
        int count = 0;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Gives one rect call of a frame, by its index among the rect calls.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="index">The index among the rect calls.</param>
    /// <returns>The call.</returns>
    private static DrawCall RectAt(RecordingRenderer renderer, int index)
    {
        int seen = 0;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind != DrawCallKind.Rect)
            {
                continue;
            }

            if (seen == index)
            {
                return calls[i];
            }

            seen++;
        }

        throw new Xunit.Sdk.XunitException("No rect call at index " + index + "; the frame holds " + seen + ".");
    }
}
