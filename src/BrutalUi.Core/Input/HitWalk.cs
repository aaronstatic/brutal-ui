using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// One frame of the explicit stack of the hit walk. The walk pushes a frame when it enters an element and pops the
/// frame after the element test.
/// </summary>
internal struct HitFrame
{
    /// <summary>The element of the frame.</summary>
    public Element Element;

    /// <summary>The viewport position of the border box of the element, in pixels.</summary>
    public float2 Origin;

    /// <summary>
    /// The index of the next child to visit. The walk counts down from the last child. A value below 0 means that
    /// the walk visited each child, so the element test comes next.
    /// </summary>
    public int NextChild;

    /// <summary>The opacity product of the element and its ancestors, after the clamp. It is above 0.</summary>
    public float Opacity;

    /// <summary>
    /// The clip bounds that apply to the children: min x, min y, max x, and max y. When the overflow is not
    /// <see cref="Overflow.Visible"/>, this is the padding box of the element, intersected with the clip of the parent.
    /// Otherwise it equals the clip of the parent. The frame holds no clip for the element itself: the parent tests
    /// its child clip before it enters a child, so the element is always inside the clip above it.
    /// </summary>
    public float4 ChildClip;
}

/// <summary>
/// The hit walk. It finds the top-most element whose border box holds a point.
/// </summary>
/// <remarks>
/// <para>
/// The order is the reverse of the paint order of <see cref="PaintWalk"/>: the last root first, the last child first,
/// and a descendant before its ancestor. The first element that holds the point is the hit. This element is on top
/// in the draw.
/// </para>
/// <para>
/// The origin of an element is the origin of its parent plus the position of <see cref="LayoutNode.Rect"/>, as in
/// step 3 of the enter step of <see cref="PaintWalk"/>. The walk reads <see cref="LayoutNode.Rect"/> of the last
/// <see cref="UiContext.Update(float)"/>.
/// </para>
/// <para>
/// The rules, in the order of the code:
/// </para>
/// <list type="number">
/// <item><description>
/// The hidden rule copies steps 1 and 2 of the enter step of <see cref="PaintWalk"/>. An element with
/// <see cref="Display.None"/> is not hit, and neither is its subtree. The same holds when the opacity product is 0 or
/// less. A value below 0 counts as 0, a value above 1 counts as 1, and NaN counts as 1.
/// </description></item>
/// <item><description>
/// An element with an overflow other than <see cref="Overflow.Visible"/> clips its children to its padding box,
/// intersected with the clip of its parent. The walk calls <c>PaintWalk.PaddingBounds</c> and
/// <c>PaintWalk.Intersect</c>, so the hit clip equals the draw clip.
/// </description></item>
/// <item><description>
/// <c>Enter</c> tests the point against the child clip when it pushes the frame (step 4). A point outside the child
/// clip sets <c>NextChild</c> to -1, so the walk does not enter the children, but it still tests the
/// element. The border of the element stays hittable. A child enters only when the point is inside each clip above
/// it, so the element test needs no clip test.
/// </description></item>
/// <item><description>
/// The element test reads <see cref="Element.IsPickable"/> first. An element that is not pickable passes the hit to the
/// next element. Its children stay pickable.
/// </description></item>
/// <item><description>
/// The element test reads only the border box, because a child enters only when the point is inside each clip
/// above it. The test is
/// half-open: a point is inside when
/// <c>min &lt;= p &lt; max</c> on each axis, so the right and the bottom edge are outside. A NaN component fails each
/// compare, so a NaN point hits nothing. An infinite clip bound passes each finite point.
/// </description></item>
/// </list>
/// <para>
/// The hit test does not read <see cref="LayoutNode.ContentInsets"/>. It follows the border box and the clip of the
/// draw, not the content box.
/// </para>
/// <para>
/// The walk uses the pattern of <see cref="PaintWalk"/>: an explicit stack of <see cref="HitFrame"/> values and no
/// recursion. No method of this class calls itself. The caller owns the stack. The walk grows it by doubling and
/// never shrinks it, so the walk allocates only during warm-up. The walk clears the used frames before it returns,
/// also after a hit and after an exception, so the stack keeps no element reference.
/// </para>
/// <para>
/// The walk reads the tree only. It changes no state and no dirty bit, and it calls no code of the element.
/// </para>
/// </remarks>
internal static class HitWalk
{
    /// <summary>The start length of the frame stack. A tree deeper than this value grows the stack one time.</summary>
    internal const int InitialStackSize = 32;

    /// <summary>The clip bounds of a root: infinite on each side, which means no clip. It equals the one of the draw.</summary>
    private static readonly float4 NoClip = new(
        float.NegativeInfinity,
        float.NegativeInfinity,
        float.PositiveInfinity,
        float.PositiveInfinity);

    /// <summary>
    /// Finds the top-most element whose border box holds a point.
    /// </summary>
    /// <param name="roots">The roots in the order of add.</param>
    /// <param name="point">The point in viewport pixels.</param>
    /// <param name="stack">The frame stack. The walk replaces it with a larger array when it is full.</param>
    /// <returns>The hit element, or <see langword="null"/> when no element holds the point.</returns>
    internal static Element? Find(ReadOnlySpan<Element> roots, float2 point, ref HitFrame[] stack)
    {
        int depth = 0;
        try
        {
            for (int r = roots.Length - 1; r >= 0; r--)
            {
                Enter(roots[r], default, 1f, in NoClip, point, ref stack, ref depth);

                while (depth > 0)
                {
                    ref HitFrame top = ref stack[depth - 1];

                    if (top.NextChild >= 0)
                    {
                        // Gotcha: Enter can grow the stack, and then the ref is stale. Copy the values and move
                        // NextChild before the call. Do not use the ref after the call.
                        Element child = top.Element.ChildStorage.AsSpan()[top.NextChild];
                        top.NextChild--;
                        float2 origin = top.Origin;
                        float opacity = top.Opacity;
                        float4 clip = top.ChildClip;
                        Enter(child, origin, opacity, in clip, point, ref stack, ref depth);
                    }
                    else
                    {
                        // The walk visited each child, so the element itself comes after its descendants.
                        Element element = top.Element;
                        if (element.IsPickable && Contains(element, top.Origin, point))
                        {
                            // The finally block clears the used frames, also this one.
                            return element;
                        }

                        top = default;
                        depth--;
                    }
                }
            }

            return null;
        }
        finally
        {
            // A miss ends at depth 0. After a hit or an exception, clear the used frames (no stale reference).
            if (depth > 0)
            {
                Array.Clear(stack, 0, depth);
            }
        }
    }

    /// <summary>
    /// Enters one element: it applies the hidden rule, then it pushes a frame with the origin, the opacity product, the
    /// child clip, and the index of the next child. The index is the last child when the point is inside the child clip,
    /// and -1 when it is not. A hidden subtree pushes no frame. The caller enters a child only when the point is inside
    /// the child clip of the parent.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="parentOrigin">The viewport position of the border box of the parent, or (0, 0) for a root.</param>
    /// <param name="opacity">The opacity product of the parent, or 1 for a root.</param>
    /// <param name="parentClip">The child clip of the parent: min x, min y, max x, and max y. A root gets no clip.</param>
    /// <param name="point">The point in viewport pixels.</param>
    /// <param name="stack">The frame stack.</param>
    /// <param name="depth">The count of frames on the stack.</param>
    private static void Enter(
        Element element,
        float2 parentOrigin,
        float opacity,
        in float4 parentClip,
        float2 point,
        ref HitFrame[] stack,
        ref int depth)
    {
        ref readonly ComputedStyle style = ref element.Style;

        // Step 1: Display.None hides the element and its subtree, as in PaintWalk (Decision R1).
        if (style.Display == Display.None)
        {
            return;
        }

        // Step 2: the opacity product, as in PaintWalk. A value below 0 counts as 0, above 1 as 1, and NaN as 1.
        float own = style.Opacity;
        if (float.IsNaN(own) || own > 1f)
        {
            own = 1f;
        }
        else if (own < 0f)
        {
            own = 0f;
        }

        opacity *= own;
        if (opacity <= 0f)
        {
            return;
        }

        // Step 3: the origin, and the clip of the children (the same calls as the draw).
        float4 r = element.Layout.Rect;
        float2 origin = new(parentOrigin.X + r.X, parentOrigin.Y + r.Y);
        float4 childClip = parentClip;
        if (style.Overflow != Overflow.Visible)
        {
            float4 borderBox = new(origin.X, origin.Y, r.Z, r.W);
            childClip = PaintWalk.Intersect(PaintWalk.PaddingBounds(in borderBox, in style), in parentClip);
        }

        // Step 4: grow the stack when it is full, then push the frame. This is the only clip cull (Decision CR-7).
        // A point outside the child clip sets NextChild to -1, so the walk skips each child and tests only the element.
        // The border of the element stays hittable.
        if (depth == stack.Length)
        {
            Array.Resize(ref stack, Math.Max(stack.Length * 2, InitialStackSize));
        }

        stack[depth] = new HitFrame
        {
            Element = element,
            Origin = origin,
            NextChild = InBounds(in childClip, point) ? element.ChildStorage.Count - 1 : -1,
            Opacity = opacity,
            ChildClip = childClip,
        };
        depth++;
    }

    /// <summary>
    /// Finds out whether bounds hold a point. The test is half-open on each axis. A NaN component is never inside.
    /// </summary>
    /// <param name="bounds">Min x, min y, max x, and max y.</param>
    /// <param name="point">The point in viewport pixels.</param>
    /// <returns><see langword="true"/> when the point is inside.</returns>
    private static bool InBounds(in float4 bounds, float2 point)
    {
        return point.X >= bounds.X && point.X < bounds.Z && point.Y >= bounds.Y && point.Y < bounds.W;
    }

    /// <summary>
    /// Finds out whether the border box of an element holds a point. The test is half-open on each axis.
    /// </summary>
    /// <remarks>
    /// The method reads only the border box. The parent tested its child clip before it entered the element (step 4 of
    /// <c>Enter</c>), so the point is inside each clip above the element and this method does not test a clip.
    /// </remarks>
    /// <param name="element">The element.</param>
    /// <param name="origin">The viewport position of the border box of the element.</param>
    /// <param name="point">The point in viewport pixels.</param>
    /// <returns><see langword="true"/> when the point is in the border box.</returns>
    private static bool Contains(Element element, float2 origin, float2 point)
    {
        float4 r = element.Layout.Rect;
        float minX = origin.X;
        float minY = origin.Y;
        float maxX = origin.X + r.Z;
        float maxY = origin.Y + r.W;

        // A NaN component fails each compare, so a NaN point is never inside.
        return point.X >= minX && point.X < maxX && point.Y >= minY && point.Y < maxY;
    }
}
