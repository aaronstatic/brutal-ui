using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the hit walk in a <see cref="ScrollView"/>: the walk moves the children of the view by the applied offset,
/// so a pointer event hits the element that the draw shows under the pointer. The border box and the clip of the view
/// do not move.
/// </summary>
/// <remarks>
/// The scene is <see cref="ScrollViewTree"/>: the view is 200 x 100 at (0, 0), the content is 300 x 180, and the maximum
/// offset is (100, 80). <see cref="PointerTree.ViewportRect(Element)"/> ignores the offset, so each expected point comes
/// from a hand computation: the drawn y of a child is its layout y minus the offset y. The tests intern no class.
/// </remarks>
public sealed class ScrollViewHitTests
{
    /// <summary>The thumb color of <see cref="TheHitAndTheDrawAgree"/>. It differs from each row color.</summary>
    private static readonly Rgba8 ThumbFill = new(250, 240, 230, 255);

    /// <summary>The fill of the grandchild of <see cref="TheHitAndTheDrawAgree"/>. It differs from each row color.</summary>
    private static readonly Rgba8 GrandchildFill = new(11, 22, 33, 255);

    /// <summary>The clip of the view in viewport bounds: min x, min y, max x, and max y.</summary>
    private static readonly float4 ViewClip = new(0f, 0f, 200f, 100f);

    /// <summary>
    /// At the offset (0, 40), a move at (100, 30) is at the content point (100, 70), inside <see cref="ScrollViewTree.Row2"/>.
    /// <see cref="UiContext.PointerHit"/> and <see cref="PseudoStates.Hover"/> are on <see cref="ScrollViewTree.Row2"/>.
    /// The walk with no offset gives <see cref="ScrollViewTree.Row0"/>.
    /// </summary>
    [Fact]
    public void AHitLandsOnTheElementThatTheUserSees()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = new float2(0f, 40f);

        Move(tree, new float2(100f, 30f));

        Assert.Same(tree.Row2, tree.Context.PointerHit);
        Assert.NotEqual(PseudoStates.None, tree.Row2.States & PseudoStates.Hover);
        Assert.Equal(PseudoStates.None, tree.Row0.States & PseudoStates.Hover);
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
    }

    /// <summary>
    /// At the offset (60, 0), <see cref="ScrollViewTree.Row0"/> draws from x -60 to x 240, and the clip of the view ends
    /// at x 200. A move at (220, 20) is outside the clip, so it hits the root. A move at (190, 20) hits
    /// <see cref="ScrollViewTree.Row0"/>.
    /// </summary>
    [Fact]
    public void TheClipOfTheViewDoesNotMove()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = new float2(60f, 0f);

        Move(tree, new float2(220f, 20f));
        Assert.Same(tree.Root, tree.Context.PointerHit);

        Move(tree, new float2(190f, 20f));
        Assert.Same(tree.Row0, tree.Context.PointerHit);
    }

    /// <summary>
    /// At the offset 0, <see cref="ScrollViewTree.OverGap"/> hits the view. At the offset (0, 80), a move at (100, 50) is
    /// at the content point (100, 130), inside <see cref="ScrollViewTree.Row3"/>. The row is not pickable, so the hit
    /// falls through to the view. The walk with no offset gives the slider there.
    /// </summary>
    [Fact]
    public void TheGapAndANonPickableChildHitTheView()
    {
        ScrollViewTree tree = new();
        tree.Settle();

        Move(tree, ScrollViewTree.OverGap);
        Assert.Same(tree.View, tree.Context.PointerHit);

        tree.View.ScrollOffset = new float2(0f, 80f);
        Move(tree, new float2(100f, 50f));
        Assert.Same(tree.View, tree.Context.PointerHit);
        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
    }

    /// <summary>
    /// At the offset (0, 40), a scroll at (100, 30) has the target <see cref="ScrollViewTree.Row2"/>. The scroll bubbles
    /// to the view, and the delta (0, -1) moves the offset to (0, 80).
    /// </summary>
    [Fact]
    public void AScrollWalksThePathOfTheShiftedChild()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = new float2(0f, 40f);
        Element? target = null;
        int rowCalls = 0;
        tree.Root.OnScrollCapture += (ref ScrollEvent e) => target = e.Header.Target;
        tree.Row2.OnScroll += (ref ScrollEvent e) => rowCalls++;
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.Context.PushEvent(PointerTree.Scroll(new float2(100f, 30f), new float2(0f, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Row2, target);
        Assert.Equal(1, rowCalls);
        Assert.Same(tree.Row2, tree.Context.PointerHit);
        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// At the maximum offset (100, 80), <see cref="ScrollViewTree.OverOutside"/> hits
    /// <see cref="ScrollViewTree.Outside"/>, and <see cref="ScrollViewTree.PastRight"/> hits the root.
    /// </summary>
    [Fact]
    public void AnElementOutsideTheViewKeepsItsHit()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = new float2(100f, 80f);
        Assert.Equal(new float2(100f, 80f), tree.View.ScrollOffset);

        Move(tree, ScrollViewTree.OverOutside);
        Assert.Same(tree.Outside, tree.Context.PointerHit);

        Move(tree, ScrollViewTree.PastRight);
        Assert.Same(tree.Root, tree.Context.PointerHit);
    }

    /// <summary>
    /// At the offset (37, 23), the draw gives rects to <see cref="ScrollViewTree.Row0"/>,
    /// <see cref="ScrollViewTree.Row2"/>, a grandchild of the view (a child of <see cref="ScrollViewTree.Row2"/>), and the
    /// thumb of the slider at the value 50. For each rect, a move at the center
    /// of the visible part of the rect inside the clip of the view hits the element that drew the rect.
    /// </summary>
    [Fact]
    public void TheHitAndTheDrawAgree()
    {
        ScrollViewTree tree = new();
        tree.Slider.Value = 50f;
        tree.Slider.Inline.Color = ThumbFill;

        // A grandchild of the view: a walk that applies the offset to each element below the view would move it twice.
        Element grandchild = new() { Name = "grandchild" };
        grandchild.Inline.Width = 50;
        grandchild.Inline.Height = 20;
        grandchild.Inline.BackgroundColor = GrandchildFill;
        tree.Row2.Children.Add(grandchild);
        tree.Settle();
        tree.View.ScrollOffset = new float2(37f, 23f);
        Assert.Equal(new float2(37f, 23f), tree.View.ScrollOffset);

        RecordingRenderer renderer = new();
        tree.Context.Update(0f);
        tree.Context.Draw(renderer);
        TestContext.Current.TestOutputHelper?.WriteLine(renderer.Describe());

        float4 row0 = RectOf(renderer, ScrollViewTree.RowFill(0));
        float4 row2 = RectOf(renderer, ScrollViewTree.RowFill(2));
        float4 thumb = RectOf(renderer, ThumbFill);
        float4 child = RectOf(renderer, GrandchildFill);

        // The hand computation: the drawn position is the layout position minus the offset.
        Assert.Equal(new float4(-37f, -23f, 300f, 40f), row0);
        Assert.Equal(new float4(-37f, 37f, 300f, 40f), row2);
        Assert.Equal(new float4(13f, 17f, 20f, 20f), thumb);
        Assert.Equal(new float4(-37f, 37f, 50f, 20f), child);

        AssertCenterHits(tree, row0, tree.Row0);
        AssertCenterHits(tree, row2, tree.Row2);
        AssertCenterHits(tree, child, grandchild);
        AssertCenterHits(tree, thumb, tree.Slider);
        Assert.Equal(50f, tree.Slider.Value);
    }

    /// <summary>Pushes a move with no target and runs one update.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="p">The position in viewport pixels.</param>
    private static void Move(ScrollViewTree tree, float2 p)
    {
        tree.Context.PushEvent(PointerTree.Move(p));
        tree.Context.Update(0f);
    }

    /// <summary>
    /// Intersects a drawn rect with the clip of the view, checks that the visible part is not empty, moves the pointer
    /// to its center, and checks the hit.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="rect">The drawn rect: x, y, width, and height.</param>
    /// <param name="expected">The element that drew the rect.</param>
    private static void AssertCenterHits(ScrollViewTree tree, float4 rect, Element expected)
    {
        float minX = Math.Max(rect.X, ViewClip.X);
        float minY = Math.Max(rect.Y, ViewClip.Y);
        float maxX = Math.Min(rect.X + rect.Z, ViewClip.Z);
        float maxY = Math.Min(rect.Y + rect.W, ViewClip.W);

        // An empty visible part would give no point, and the test would pass for the wrong reason.
        Assert.True(maxX > minX && maxY > minY, "The visible part of " + expected.Name + " is empty.");

        float2 center = new((minX + maxX) / 2f, (minY + maxY) / 2f);
        Move(tree, center);
        Assert.Same(expected, tree.Context.PointerHit);
    }

    /// <summary>Gives the rect of the only rect call with a fill.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The fill.</param>
    /// <returns>The rect: x, y, width, and height.</returns>
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
}
