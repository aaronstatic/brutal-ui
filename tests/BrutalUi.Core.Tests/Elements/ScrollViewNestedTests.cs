using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of a <see cref="ScrollView"/> in a <see cref="ScrollView"/>: the offsets add up in the hit, the draw, and the
/// origin, and an inner view at its end passes the wheel to the outer view.
/// </summary>
/// <remarks>
/// <para>
/// The scene (see <see cref="BuildScene"/>): the outer view is the only root. It is 200 x 100 at (0, 0), in a column,
/// and it holds <c>Top</c> (300 x 40 at y 0), <c>Inner</c> (150 x 60 at (0, 40)), and <c>Bottom</c> (300 x 80 at y 100).
/// The outer content is 300 x 180, so the maximum offset is (100, 80).
/// </para>
/// <para>
/// <c>Inner</c> is a view with a left padding of 10. It holds <c>InnerRow0</c>, <c>InnerRow1</c>, and <c>InnerRow2</c>,
/// each 200 x 40, at x 10 and at y 0, 40, and 80 of the inner view. The inner content is 210 x 120, so the maximum
/// offset is (60, 60). The tests intern no class.
/// </para>
/// </remarks>
public sealed class ScrollViewNestedTests
{
    /// <summary>
    /// The demo of phase 5. An inner view at its end (0, 60) in an outer view at (0, 0) gets a scroll of (0, -1) over it.
    /// The inner offset stays (0, 60), the outer offset is (0, 40), the outer view calls
    /// <see cref="ScrollView.ScrollChanged"/> one time, and the inner view calls it never.
    /// </summary>
    [Fact]
    public void AnInnerViewAtItsEndPassesTheWheelToTheOuterView()
    {
        Scene scene = BuildScene();
        scene.Inner.ScrollOffset = new float2(0f, 60f);
        Assert.Equal(new float2(0f, 60f), scene.Inner.ScrollOffset);

        scene.Context.PushEvent(PointerTree.Scroll(Over, new float2(0f, -1f)));
        scene.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: scroll (0, -1) over an inner view at (0, 60), inner = (" + scene.Inner.ScrollOffset.X + ", "
            + scene.Inner.ScrollOffset.Y + "), outer = (" + scene.Outer.ScrollOffset.X + ", "
            + scene.Outer.ScrollOffset.Y + "), outer calls = " + scene.OuterCalls + ", inner calls = "
            + scene.InnerCalls);

        Assert.Equal(new float2(0f, 60f), scene.Inner.ScrollOffset);
        Assert.Equal(new float2(0f, 40f), scene.Outer.ScrollOffset);
        Assert.Equal(1, scene.OuterCalls);
        Assert.Equal(0, scene.InnerCalls);
    }

    /// <summary>
    /// An inner view with the offset 0 moves to (0, 40) for the scroll of (0, -1). It stops the walk, so the outer view
    /// stays at (0, 0) and calls nothing.
    /// </summary>
    [Fact]
    public void AnInnerViewThatMovesKeepsTheWheel()
    {
        Scene scene = BuildScene();

        scene.Context.PushEvent(PointerTree.Scroll(Over, new float2(0f, -1f)));
        scene.Context.Update(0f);

        Assert.Equal(new float2(0f, 40f), scene.Inner.ScrollOffset);
        Assert.Equal(new float2(0f, 0f), scene.Outer.ScrollOffset);
        Assert.Equal(1, scene.InnerCalls);
        Assert.Equal(0, scene.OuterCalls);
    }

    /// <summary>
    /// The outer offset (0, 20) and the inner offset (0, 30) add up. A move at (50, 35) hits <c>InnerRow1</c>. The draw
    /// puts <c>InnerRow1</c> at (10, 30, 200, 40), and the clip of the inner view is (0, 20, 150, 60).
    /// </summary>
    [Fact]
    public void TheOffsetsAddUpInTheHitAndTheDraw()
    {
        Scene scene = BuildScene();
        scene.Outer.ScrollOffset = new float2(0f, 20f);
        scene.Inner.ScrollOffset = new float2(0f, 30f);
        Assert.Equal(new float2(0f, 20f), scene.Outer.ScrollOffset);
        Assert.Equal(new float2(0f, 30f), scene.Inner.ScrollOffset);

        float2 point = new(50f, 35f);
        Assert.Same(scene.InnerRow1, scene.Context.HitTest(point));
        scene.Context.PushEvent(PointerTree.Move(point));
        scene.Context.Update(0f);
        Assert.Same(scene.InnerRow1, scene.Context.PointerHit);

        RecordingRenderer renderer = new();
        scene.Context.Draw(renderer);

        Assert.Equal(new float4(10f, 30f, 200f, 40f), RectOf(renderer, scene.Fill(1)));
        Assert.Equal(new float4(10f, -10f, 200f, 40f), RectOf(renderer, scene.Fill(0)));
        Assert.True(HasClip(renderer, new float4(0f, 0f, 200f, 100f)));
        Assert.True(HasClip(renderer, new float4(0f, 20f, 150f, 60f)));
        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>
    /// With the same offsets, <c>ViewportOrigin</c> gives (10, 30) for <c>InnerRow1</c> and (0, 20) for <c>Inner</c>. Each
    /// element subtracts the offset of each ancestor view and never its own offset.
    /// </summary>
    [Fact]
    public void TheOriginSubtractsEachAncestorOffset()
    {
        Scene scene = BuildScene();
        scene.Outer.ScrollOffset = new float2(0f, 20f);
        scene.Inner.ScrollOffset = new float2(0f, 30f);

        Assert.Equal(new float2(10f, 30f), WidgetInput.ViewportOrigin(scene.InnerRow1));
        Assert.Equal(new float2(0f, 20f), WidgetInput.ViewportOrigin(scene.Inner));
        Assert.Equal(new float2(0f, 0f), WidgetInput.ViewportOrigin(scene.Outer));
    }

    /// <summary>
    /// A press at (5, 60) hits the padding of the inner view. A move to (5, 30) gives the inner offset (0, 30). The outer
    /// view stays at (0, 0) and calls nothing.
    /// </summary>
    [Fact]
    public void ADragOnTheInnerViewMovesTheInnerViewOnly()
    {
        Scene scene = BuildScene();
        float2 press = new(5f, 60f);
        Assert.Same(scene.Inner, scene.Context.HitTest(press));

        scene.Context.PushEvent(PointerTree.Down(press, PointerButton.Left));
        scene.Context.PushEvent(PointerTree.Move(new float2(5f, 30f)));
        scene.Context.Update(0f);

        Assert.Equal(new float2(0f, 30f), scene.Inner.ScrollOffset);
        Assert.Equal(new float2(0f, 0f), scene.Outer.ScrollOffset);
        Assert.Equal(1, scene.InnerCalls);
        Assert.Equal(0, scene.OuterCalls);
    }

    /// <summary>
    /// A disabled inner view with the offset 0 could move, but it takes no wheel and does not stop the walk. The outer
    /// view moves to (0, 40), and the inner view stays at (0, 0) and calls nothing.
    /// </summary>
    [Fact]
    public void ADisabledInnerViewPassesTheWheelToTheOuterView()
    {
        Scene scene = BuildScene();
        scene.Inner.States |= PseudoStates.Disabled;

        scene.Context.PushEvent(PointerTree.Scroll(Over, new float2(0f, -1f)));
        scene.Context.Update(0f);

        Assert.Equal(new float2(0f, 0f), scene.Inner.ScrollOffset);
        Assert.Equal(new float2(0f, 40f), scene.Outer.ScrollOffset);
        Assert.Equal(0, scene.InnerCalls);
        Assert.Equal(1, scene.OuterCalls);
        Assert.Equal(PseudoStates.Disabled, scene.Inner.States & PseudoStates.Disabled);
    }

    /// <summary>A point inside <c>Inner</c> and over its rows, at the offsets (0, 0), (0, 60) and (0, 0).</summary>
    private static readonly float2 Over = new(50f, 60f);

    /// <summary>
    /// Builds the scene of the class, adds the outer view to a new context, and runs one update so the layout is current.
    /// </summary>
    /// <returns>The scene.</returns>
    private static Scene BuildScene()
    {
        Scene scene = new();
        scene.Context.Update(0f);
        return scene;
    }

    /// <summary>Gives the rect of the only rect call with a fill.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The fill.</param>
    /// <returns>The rect.</returns>
    private static float4 RectOf(RecordingRenderer renderer, Rgba8 color)
    {
        int found = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect && calls[i].Color.Equals(color))
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return calls[found].Rect;
    }

    /// <summary>Tells if the renderer got a clip push with a rect.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="rect">The clip rect.</param>
    /// <returns><see langword="true"/> when one push has the rect.</returns>
    private static bool HasClip(RecordingRenderer renderer, float4 rect)
    {
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.PushClip && calls[i].Rect.Equals(rect))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The elements and the counters of the scene.</summary>
    private sealed class Scene
    {
        /// <summary>The inner rows.</summary>
        private readonly Element[] _rows = new Element[3];

        /// <summary>Creates the scene.</summary>
        public Scene()
        {
            Context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
            {
                ViewportSize = new float2(800f, 600f),
            };

            Outer = new ScrollView(default(ScrollView.CreateInfo)) { Name = "outer" };
            Outer.Inline.FlexDirection = FlexDirection.Column;
            Outer.Inline.Width = 200;
            Outer.Inline.Height = 100;
            Outer.ScrollChanged = _ => OuterCalls++;

            Element top = new() { Name = "top" };
            top.Inline.Width = 300;
            top.Inline.Height = 40;

            Inner = new ScrollView(default(ScrollView.CreateInfo)) { Name = "inner" };
            Inner.Inline.FlexDirection = FlexDirection.Column;
            Inner.Inline.Width = 150;
            Inner.Inline.Height = 60;
            Inner.Inline.Padding = new LengthEdges(10, 0, 0, 0);
            Inner.ScrollChanged = _ => InnerCalls++;

            for (int i = 0; i < _rows.Length; i++)
            {
                Element row = new() { Name = "innerRow" + i };
                row.Inline.Width = 200;
                row.Inline.Height = 40;
                row.Inline.BackgroundColor = Fill(i);
                _rows[i] = row;
                Inner.Children.Add(row);
            }

            Element bottom = new() { Name = "bottom" };
            bottom.Inline.Width = 300;
            bottom.Inline.Height = 80;

            Outer.Children.Add(top);
            Outer.Children.Add(Inner);
            Outer.Children.Add(bottom);
            Context.AddRoot(Outer);
        }

        /// <summary>Gets the context.</summary>
        public UiContext Context { get; }

        /// <summary>Gets the outer view: 200 x 100 at (0, 0).</summary>
        public ScrollView Outer { get; }

        /// <summary>Gets the inner view: 150 x 60 at (0, 40) of the outer view, with a left padding of 10.</summary>
        public ScrollView Inner { get; }

        /// <summary>Gets the second inner row: 200 x 40 at (10, 40) of the inner view.</summary>
        public Element InnerRow1 => _rows[1];

        /// <summary>Gets the number of <see cref="ScrollView.ScrollChanged"/> calls of the outer view.</summary>
        public int OuterCalls { get; private set; }

        /// <summary>Gets the number of <see cref="ScrollView.ScrollChanged"/> calls of the inner view.</summary>
        public int InnerCalls { get; private set; }

        /// <summary>Gives the opaque background color of an inner row.</summary>
        /// <param name="row">The index of the row, 0 to 2.</param>
        /// <returns>The color. Each row has its own color.</returns>
        public Rgba8 Fill(int row)
        {
            return new Rgba8((byte)(60 + (row * 50)), (byte)(180 - (row * 40)), (byte)(30 + (row * 30)), 255);
        }
    }
}
