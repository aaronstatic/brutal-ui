using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the rules of the hit walk that follow the draw: the clip of an overflow, the hidden rule, and
/// <see cref="Element.IsPickable"/>.
/// </summary>
public sealed class HitTestRuleTests
{
    /// <summary>
    /// The demo of phase 3. A child lies half outside a parent with <see cref="Overflow.Hidden"/>. A sibling that comes
    /// before the parent in the tree lies under the clipped half. A point in the visible half hits the child. A point
    /// in the clipped half hits the sibling, as the draw clips the child and shows the sibling there.
    /// </summary>
    [Fact]
    public void AnOverflowHiddenParentClipsItsChild()
    {
        Element root = NewRoot();
        Element sibling = NewBox("sibling", 150, 50, 100, 100, Overflow.Visible);
        Element parent = NewBox("parent", 50, 50, 100, 100, Overflow.Hidden);
        Element child = NewBox("child", 0, 0, 200, 200, Overflow.Visible);
        root.Children.Add(sibling);
        root.Children.Add(parent);
        parent.Children.Add(child);
        UiContext context = Settle(root);

        Assert.Equal(new float4(150f, 50f, 100f, 100f), PointerTree.ViewportRect(sibling));
        Assert.Equal(new float4(50f, 50f, 100f, 100f), PointerTree.ViewportRect(parent));
        Assert.Equal(new float4(50f, 50f, 200f, 200f), PointerTree.ViewportRect(child));

        // The visible half: x from 50 to 150. The child is above the sibling and hit first.
        Assert.Same(child, context.HitTest(new float2(120f, 120f)));

        // The clipped half: the child lies there but the clip hides it, so the sibling under it takes the hit.
        Assert.Same(sibling, context.HitTest(new float2(200f, 120f)));

        // The clipped half where the sibling is not: the hit falls through to the root.
        Assert.Same(root, context.HitTest(new float2(120f, 200f)));
    }

    /// <summary>
    /// A parent with <see cref="Overflow.Scroll"/> clips its child, as <see cref="Overflow.Hidden"/> does.
    /// </summary>
    [Fact]
    public void AnOverflowScrollParentClipsItsChild()
    {
        ClipScene scene = new(Overflow.Scroll);

        Assert.Same(scene.Child, scene.Context.HitTest(new float2(120f, 120f)));
        Assert.Same(scene.Root, scene.Context.HitTest(new float2(200f, 120f)));
        Assert.Same(scene.Root, scene.Context.HitTest(new float2(120f, 200f)));
    }

    /// <summary>
    /// Two nested clips intersect. A point inside the inner clip but outside the outer clip hits the root.
    /// </summary>
    [Fact]
    public void NestedClipsIntersect()
    {
        Element root = NewRoot();
        Element outer = NewBox("outer", 50, 50, 100, 100, Overflow.Hidden);
        Element middle = NewBox("middle", 50, 50, 200, 200, Overflow.Hidden);
        Element leaf = NewBox("leaf", 0, 0, 300, 300, Overflow.Visible);
        root.Children.Add(outer);
        outer.Children.Add(middle);
        middle.Children.Add(leaf);
        UiContext context = Settle(root);

        Assert.Equal(new float4(100f, 100f, 200f, 200f), PointerTree.ViewportRect(middle));

        // Inside both clips: x and y from 100 to 150.
        Assert.Same(leaf, context.HitTest(new float2(120f, 120f)));
        Assert.Same(leaf, context.HitTest(new float2(100f, 100f)));

        // Inside the middle clip, outside the outer clip.
        Assert.Same(root, context.HitTest(new float2(160f, 120f)));
        Assert.Same(root, context.HitTest(new float2(120f, 160f)));

        // Inside the outer clip, before the middle box starts.
        Assert.Same(outer, context.HitTest(new float2(70f, 70f)));
    }

    /// <summary>
    /// A parent with <see cref="Overflow.Visible"/> does not clip. A point in the child but outside the parent hits the
    /// child.
    /// </summary>
    [Fact]
    public void AVisibleOverflowLetsAChildOutsideTheParentBeHit()
    {
        ClipScene scene = new(Overflow.Visible);

        Assert.Same(scene.Child, scene.Context.HitTest(new float2(200f, 120f)));
        Assert.Same(scene.Child, scene.Context.HitTest(new float2(120f, 200f)));
        Assert.Same(scene.Root, scene.Context.HitTest(new float2(260f, 120f)));
    }

    /// <summary>
    /// The clip is the padding box. A point on the border of a clip parent, over a child that extends under the border,
    /// hits the parent. The border box of the parent is not clipped by its own clip.
    /// </summary>
    [Fact]
    public void ThePaddingBoxClipKeepsTheBorderForTheParent()
    {
        Element root = NewRoot();
        Element parent = NewBox("parent", 50, 50, 100, 100, Overflow.Hidden);
        parent.Inline.BorderWidth = LengthEdges.All(10);
        Element child = NewBox("child", -10, -10, 200, 200, Overflow.Visible);
        root.Children.Add(parent);
        parent.Children.Add(child);
        UiContext context = Settle(root);

        float4 childRect = PointerTree.ViewportRect(child);
        float2 onBorder = new(55f, 100f);
        Assert.True(onBorder.X >= childRect.X && onBorder.X < childRect.X + childRect.Z);
        Assert.True(onBorder.Y >= childRect.Y && onBorder.Y < childRect.Y + childRect.W);

        Assert.Same(parent, context.HitTest(onBorder));
        Assert.Same(parent, context.HitTest(new float2(100f, 145f)));
        Assert.Same(child, context.HitTest(new float2(100f, 100f)));
        Assert.Same(root, context.HitTest(new float2(150f, 100f)));
    }

    /// <summary>An unpickable leaf passes the hit to its parent.</summary>
    [Fact]
    public void AnUnpickableLeafPassesTheHitToItsParent()
    {
        PointerTree tree = new(nameof(HitTestRuleTests) + "." + nameof(AnUnpickableLeafPassesTheHitToItsParent));
        tree.Settle();
        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(125f, 25f)));

        tree.Inner.IsPickable = false;

        Assert.Same(tree.Right, tree.Context.HitTest(new float2(125f, 25f)));
    }

    /// <summary>
    /// An unpickable parent keeps its children pickable. A point in the gap of the parent passes to the grandparent.
    /// </summary>
    [Fact]
    public void AnUnpickableParentKeepsItsChildrenPickable()
    {
        PointerTree tree = new(nameof(HitTestRuleTests) + "." + nameof(AnUnpickableParentKeepsItsChildrenPickable));
        tree.Settle();

        tree.Right.IsPickable = false;

        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(125f, 25f)));
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(175f, 75f)));
        Assert.Same(tree.Left, tree.Context.HitTest(new float2(50f, 50f)));
    }

    /// <summary>
    /// A subtree with <see cref="Display.None"/> is not hit. The test sets the style after the layout, so the rects stay
    /// valid and the hidden rule alone decides.
    /// </summary>
    [Fact]
    public void ADisplayNoneSubtreeIsNotHit()
    {
        PointerTree tree = new(nameof(HitTestRuleTests) + "." + nameof(ADisplayNoneSubtreeIsNotHit));
        tree.Settle();
        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(125f, 25f)));

        tree.Right.StyleStorage.Display = Display.None;

        Assert.Equal(new float4(100f, 0f, 100f, 100f), PointerTree.ViewportRect(tree.Right));
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(125f, 25f)));
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(175f, 75f)));
        Assert.Same(tree.Left, tree.Context.HitTest(new float2(50f, 50f)));
    }

    /// <summary>
    /// A subtree with an opacity product of 0 is not hit. A value below 0 counts as 0. NaN counts as 1, so the element
    /// stays hittable.
    /// </summary>
    [Fact]
    public void AZeroOpacitySubtreeIsNotHit()
    {
        PointerTree tree = new(nameof(HitTestRuleTests) + "." + nameof(AZeroOpacitySubtreeIsNotHit));
        tree.Settle();

        tree.Right.StyleStorage.Opacity = 0f;
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(125f, 25f)));
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(175f, 75f)));

        tree.Right.StyleStorage.Opacity = -3f;
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(125f, 25f)));

        tree.Right.StyleStorage.Opacity = float.NaN;
        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(125f, 25f)));

        tree.Right.StyleStorage.Opacity = 7f;
        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(125f, 25f)));

        // The product counts: a child of 0.5 under a parent of 0.5 stays hittable.
        tree.Right.StyleStorage.Opacity = 0.5f;
        tree.Inner.StyleStorage.Opacity = 0.5f;
        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(125f, 25f)));

        // A child of 0 under a visible parent hides only the child.
        tree.Right.StyleStorage.Opacity = 1f;
        tree.Inner.StyleStorage.Opacity = 0f;
        Assert.Same(tree.Right, tree.Context.HitTest(new float2(125f, 25f)));
    }

    /// <summary>
    /// The test is half-open. The right and the bottom edge of a clip and of a border box are outside, and the left and
    /// the top edge are inside.
    /// </summary>
    [Fact]
    public void TheRightAndBottomEdgesAreOutside()
    {
        Element root = NewRoot();
        Element parent = NewBox("parent", 50, 50, 100, 100, Overflow.Hidden);
        Element child = NewBox("child", 0, 0, 200, 200, Overflow.Visible);
        Element neighbor = NewBox("neighbor", 150, 50, 50, 50, Overflow.Visible);
        root.Children.Add(parent);
        parent.Children.Add(child);
        root.Children.Add(neighbor);
        UiContext context = Settle(root);

        // The clip of the parent ends at x = 150 and y = 150. The neighbor starts at x = 150.
        Assert.Same(child, context.HitTest(new float2(149.5f, 100f)));
        Assert.Same(neighbor, context.HitTest(new float2(150f, 75f)));
        Assert.Same(root, context.HitTest(new float2(150f, 100f)));
        Assert.Same(child, context.HitTest(new float2(100f, 149.5f)));
        Assert.Same(root, context.HitTest(new float2(100f, 150f)));

        // The left and the top edge of the clip are inside.
        Assert.Same(child, context.HitTest(new float2(50f, 50f)));
        Assert.Same(root, context.HitTest(new float2(49.5f, 100f)));

        // The right edge of the neighbor is outside.
        Assert.Same(neighbor, context.HitTest(new float2(199.5f, 75f)));
        Assert.Same(root, context.HitTest(new float2(200f, 75f)));
    }

    /// <summary>A point with a NaN component hits nothing, also under a clip. Infinite points hit nothing.</summary>
    [Fact]
    public void ANaNPointHitsNothing()
    {
        ClipScene scene = new(Overflow.Hidden);

        Assert.Null(scene.Context.HitTest(new float2(float.NaN, float.NaN)));
        Assert.Null(scene.Context.HitTest(new float2(float.NaN, 120f)));
        Assert.Null(scene.Context.HitTest(new float2(120f, float.NaN)));
        Assert.Null(scene.Context.HitTest(new float2(float.PositiveInfinity, 120f)));
        Assert.Null(scene.Context.HitTest(new float2(float.NegativeInfinity, 120f)));
        Assert.Same(scene.Child, scene.Context.HitTest(new float2(120f, 120f)));
    }

    private static Element NewRoot()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        return root;
    }

    private static Element NewBox(string name, float left, float top, float width, float height, Overflow overflow)
    {
        Element box = new() { Name = name };
        box.Inline.Position = PositionType.Absolute;
        box.Inline.Left = left;
        box.Inline.Top = top;
        box.Inline.Width = width;
        box.Inline.Height = height;
        box.Inline.Overflow = overflow;
        return box;
    }

    private static UiContext Settle(Element root)
    {
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
        context.AddRoot(root);
        context.Update(0f);
        return context;
    }

    /// <summary>A root of 400 x 300, a parent of 100 x 100 at (50, 50), and a child of 200 x 200 in the parent.</summary>
    private sealed class ClipScene
    {
        public ClipScene(Overflow overflow)
        {
            Root = NewRoot();
            Parent = NewBox("parent", 50, 50, 100, 100, overflow);
            Child = NewBox("child", 0, 0, 200, 200, Overflow.Visible);
            Root.Children.Add(Parent);
            Parent.Children.Add(Child);
            Context = Settle(Root);
        }

        public UiContext Context { get; }

        public Element Root { get; }

        public Element Parent { get; }

        public Element Child { get; }
    }
}
