using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// One frame of the explicit stack of the draw walk. The walk pushes a frame when it enters an element and pops the
/// frame after the last child of the element.
/// </summary>
internal struct PaintFrame
{
    /// <summary>The element of the frame.</summary>
    public Element Element;

    /// <summary>The viewport position of the border box of the element, in pixels.</summary>
    public float2 Origin;

    /// <summary>
    /// The product of the opacities from the root to the element, each clamped from 0 to 1.
    /// </summary>
    public float Opacity;

    /// <summary>
    /// The clip for the children: min x, min y, max x, and max y in viewport pixels (not x, y, width, and height).
    /// Infinite bounds mean no clip.
    /// </summary>
    public float4 ClipBounds;

    /// <summary>True when the walk pushed a clip for the element. The walk then pops it after the subtree.</summary>
    public bool PushedClip;

    /// <summary>The index of the next child to visit.</summary>
    public int NextChild;
}

/// <summary>
/// The draw walk. It visits the elements in tree order and sends the paint calls to an <see cref="IUiRenderer"/>.
/// </summary>
/// <remarks>
/// <para>
/// The paint order is the tree order: the parent first, then the children in child order (A1). The roots come in the
/// order of add.
/// </para>
/// <para>
/// The walk uses an explicit stack of <see cref="PaintFrame"/> values and no recursion (A8). No method of this class
/// calls itself. The caller owns the stack. The walk grows it by doubling and never shrinks it, so the walk allocates
/// only during warm-up.
/// </para>
/// <para>
/// The walk reads the tree only. It changes no dirty bit (A2). It reads <see cref="LayoutNode.Rect"/>, so the caller
/// runs <see cref="UiContext.Update(float)"/> first.
/// </para>
/// </remarks>
internal static class PaintWalk
{
    /// <summary>The start length of the frame stack. A tree deeper than this value grows the stack one time.</summary>
    internal const int InitialStackSize = 32;

    /// <summary>The clip bounds of a root: infinite on each side, which means no clip.</summary>
    private static readonly float4 NoClip = new(
        float.NegativeInfinity,
        float.NegativeInfinity,
        float.PositiveInfinity,
        float.PositiveInfinity);

    /// <summary>
    /// Walks each root and its subtree in tree order and calls the renderer for each painted element.
    /// </summary>
    /// <remarks>
    /// The method does not call <see cref="IUiRenderer.Begin"/> or <see cref="IUiRenderer.End"/>.
    /// <see cref="UiContext.Draw(IUiRenderer)"/> calls them. Each clip push gets its pop before the walk returns.
    /// After an exception, the walk clears the used frames, so the stack keeps no element reference.
    /// </remarks>
    /// <param name="roots">The roots in the order of add.</param>
    /// <param name="renderer">The renderer that gets the calls.</param>
    /// <param name="stack">The frame stack. The walk replaces it with a larger array when it is full.</param>
    internal static void Walk(ReadOnlySpan<Element> roots, IUiRenderer renderer, ref PaintFrame[] stack)
    {
        int depth = 0;
        try
        {
            for (int i = 0; i < roots.Length; i++)
            {
                Enter(roots[i], default, 1f, NoClip, renderer, ref stack, ref depth);

                while (depth > 0)
                {
                    ref PaintFrame top = ref stack[depth - 1];
                    ReadOnlySpan<Element> children = top.Element.ChildStorage.AsSpan();

                    if (top.NextChild < children.Length)
                    {
                        // Gotcha: Enter can grow the stack, and then the ref is stale. Copy the values and move
                        // NextChild before the call. Do not use the ref after the call.
                        Element child = children[top.NextChild];
                        top.NextChild++;
                        float2 origin = top.Origin;
                        float opacity = top.Opacity;
                        float4 clip = top.ClipBounds;
                        Enter(child, origin, opacity, clip, renderer, ref stack, ref depth);
                    }
                    else
                    {
                        if (top.PushedClip)
                        {
                            renderer.PopClip();
                        }

                        top = default;
                        depth--;
                    }
                }
            }
        }
        finally
        {
            // The normal path ends at depth 0. After an exception, clear the used frames (no stale reference).
            if (depth > 0)
            {
                Array.Clear(stack, 0, depth);
            }
        }
    }

    /// <summary>
    /// Enters one element: it draws the fill, pushes the clip, and pushes a frame for the children.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="parentOrigin">The viewport position of the border box of the parent, or (0, 0) for a root.</param>
    /// <param name="opacity">The opacity product of the parent, or 1 for a root.</param>
    /// <param name="parentClip">The clip bounds of the parent: min x, min y, max x, and max y.</param>
    /// <param name="renderer">The renderer.</param>
    /// <param name="stack">The frame stack.</param>
    /// <param name="depth">The count of frames on the stack.</param>
    private static void Enter(
        Element element,
        float2 parentOrigin,
        float opacity,
        in float4 parentClip,
        IUiRenderer renderer,
        ref PaintFrame[] stack,
        ref int depth)
    {
        ref readonly ComputedStyle style = ref element.Style;

        // Step 1: Display.None hides the element and its subtree (Q2, Decision 7).
        if (style.Display == Display.None)
        {
            return;
        }

        // Step 2: the opacity product (Q3). A value below 0 counts as 0, above 1 as 1, and NaN as 1. At 0 or less, hide (Q2).
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

        // Step 3: the border box in viewport pixels (Q1).
        float4 r = element.Layout.Rect;
        float2 origin = new(parentOrigin.X + r.X, parentOrigin.Y + r.Y);
        float4 borderBox = new(origin.X, origin.Y, r.Z, r.W);

        // Step 4: the fill and the stroke with the opacity product (Q3), and the border rule (A4, Decision 4).
        Rgba8 fill = style.BackgroundColor.ScaleAlpha(opacity);
        Rgba8 stroke = style.BorderColor.ScaleAlpha(opacity);
        float borderWidth = LayoutMath.LeadingBorder(in style, FlexDirection.Row);
        bool borderVisible = borderWidth > 0f && stroke.A > 0;

        // Step 5: the rect, for a visible fill or a visible border.
        if (fill.A > 0 || borderVisible)
        {
            Rgba8 drawStroke = borderVisible ? stroke : Rgba8.Transparent;
            renderer.Rect(in borderBox, in fill, in style.CornerRadius, borderVisible ? borderWidth : 0f, in drawStroke);
        }

        // Step 6: the clip of the padding box (Q4, A7, Decision 6). Hidden and Scroll both clip.
        float4 childClip = parentClip;
        bool pushedClip = false;
        if (style.Overflow != Overflow.Visible)
        {
            childClip = Intersect(PaddingBounds(in borderBox, in style), in parentClip);
            renderer.PushClip(ToRect(in childClip));
            pushedClip = true;
        }

        // Step 7: the paint hook of the element. It runs after the clip push, so the clip also clips the content.
        // The walk holds no ref PaintFrame here. The hook receives the border box and the opacity product.
        element.PaintContent(renderer, in borderBox, opacity);

        // Step 8: push the frame. Grow the stack first when it is full.
        if (depth == stack.Length)
        {
            Array.Resize(ref stack, Math.Max(stack.Length * 2, InitialStackSize));
        }

        stack[depth] = new PaintFrame
        {
            Element = element,
            Origin = origin,
            Opacity = opacity,
            ClipBounds = childClip,
            PushedClip = pushedClip,
            NextChild = 0,
        };
        depth++;
    }

    /// <summary>
    /// Gives the bounds of the padding box: the border box less the four resolved border widths (A7).
    /// </summary>
    /// <param name="borderBox">The border box: x, y, width, and height.</param>
    /// <param name="style">The style of the element.</param>
    /// <returns>Min x, min y, max x, and max y. The max can be less than the min when the borders are wider than the box.</returns>
    /// <remarks><see cref="HitWalk"/> shares this method, so the hit clip equals the draw clip.</remarks>
    internal static float4 PaddingBounds(in float4 borderBox, in ComputedStyle style)
    {
        float left = LayoutMath.LeadingBorder(in style, FlexDirection.Row);
        float right = LayoutMath.TrailingBorder(in style, FlexDirection.Row);
        float top = LayoutMath.LeadingBorder(in style, FlexDirection.Column);
        float bottom = LayoutMath.TrailingBorder(in style, FlexDirection.Column);

        return new float4(
            borderBox.X + left,
            borderBox.Y + top,
            borderBox.X + borderBox.Z - right,
            borderBox.Y + borderBox.W - bottom);
    }

    /// <summary>
    /// Intersects two bounds. An empty result gets a max equal to its min, so the size is 0 and not negative.
    /// </summary>
    /// <param name="a">The first bounds: min x, min y, max x, and max y.</param>
    /// <param name="b">The second bounds: min x, min y, max x, and max y.</param>
    /// <returns>The intersection.</returns>
    /// <remarks><see cref="HitWalk"/> shares this method, so the hit clip equals the draw clip.</remarks>
    internal static float4 Intersect(in float4 a, in float4 b)
    {
        float minX = Math.Max(a.X, b.X);
        float minY = Math.Max(a.Y, b.Y);
        float maxX = Math.Min(a.Z, b.Z);
        float maxY = Math.Min(a.W, b.W);

        if (maxX < minX)
        {
            maxX = minX;
        }

        if (maxY < minY)
        {
            maxY = minY;
        }

        return new float4(minX, minY, maxX, maxY);
    }

    /// <summary>Converts bounds to a rect.</summary>
    /// <param name="bounds">Min x, min y, max x, and max y.</param>
    /// <returns>X, y, width, and height.</returns>
    private static float4 ToRect(in float4 bounds)
    {
        return new float4(bounds.X, bounds.Y, bounds.Z - bounds.X, bounds.W - bounds.Y);
    }
}
