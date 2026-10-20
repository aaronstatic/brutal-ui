using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The owner of the root elements. <see cref="Update(float)"/> runs the passes on the dirty elements in frame order.
/// <see cref="Draw(IUiRenderer)"/> then sends the tree to a renderer.
/// </summary>
/// <remarks>
/// <para>The tree is main-thread only. The context and the elements use no locks.</para>
/// <para>
/// <see cref="ViewportSize"/> gives the available size of each root. Set it before the first
/// <see cref="Update(float)"/> and after each resize of the window.
/// </para>
/// <para>
/// An element is a root of one context at most. An element holds no reference to its context.
/// It holds a root flag instead. <see cref="AddRoot(Element)"/> throws for an element with the flag.
/// Remove a root from its context before you add it to another context or to a child list.
/// </para>
/// <para>
/// A root keeps its root flag until <see cref="RemoveRoot(Element)"/>.
/// Call <see cref="RemoveRoot(Element)"/> on each root before you drop a context.
/// </para>
/// </remarks>
public sealed class UiContext
{
    private ElementStorage _roots;
    private float2 _viewportSize;
    private PaintFrame[] _paintStack = new PaintFrame[PaintWalk.InitialStackSize];
    private bool _drawing;

    /// <summary>Creates a context with no roots and a viewport size of (0, 0).</summary>
    public UiContext()
    {
    }

    /// <summary>Gets the root elements in the order of add.</summary>
    /// <remarks>
    /// The span is valid until the next change of the roots.
    /// After a growth, the span can read null or the children of another element.
    /// </remarks>
    public ReadOnlySpan<Element> Roots => _roots.AsSpan();

    /// <summary>
    /// Gets or sets the available size of each root, in pixels. X is the width and Y is the height.
    /// The default is (0, 0).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set the size before the first <see cref="Update(float)"/> and after each resize of the window.
    /// A <c>NaN</c> component means no limit on that axis. Then a root with an auto size on that axis
    /// takes the size of its content.
    /// </para>
    /// <para>
    /// A root with an auto size gets the viewport size on that axis. A root with a style size keeps its style size.
    /// A new value marks no element. The next <see cref="Update(float)"/> uses it.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">A component of the value is negative or infinite.</exception>
    public float2 ViewportSize
    {
        get => _viewportSize;
        set
        {
            if (!IsValidViewportComponent(value.X) || !IsValidViewportComponent(value.Y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Each component of the viewport size must be 0 or more and finite, or NaN for no limit.");
            }

            _viewportSize = value;
        }
    }

    /// <summary>
    /// Adds a root element at the end of the roots. The root keeps its dirty bits.
    /// </summary>
    /// <remarks>
    /// An element that has a parent leaves that parent first. The old parent gets the
    /// <see cref="DirtyFlags.Layout"/> bit.
    /// An element that is a root already throws, also when the root belongs to another context.
    /// </remarks>
    /// <param name="root">The element to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The element is a root of this context or of another context.</exception>
    public void AddRoot(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (root.IsRoot)
        {
            throw new InvalidOperationException("The element is a root already. Remove it from its UiContext first.");
        }

        root.Parent?.Children.Remove(root);
        _roots.Add(root);
        root.IsRoot = true;
    }

    /// <summary>Removes a root element. The element keeps its dirty bits.</summary>
    /// <param name="root">The element to remove.</param>
    /// <returns><see langword="true"/> when the context held the root. Otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    public bool RemoveRoot(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);

        int index = _roots.IndexOf(root);
        if (index < 0)
        {
            return false;
        }

        _roots.RemoveAt(index);
        root.IsRoot = false;
        return true;
    }

    /// <summary>
    /// Runs the passes on each root in frame order. The layout pass lays out each root against
    /// <see cref="ViewportSize"/> and clears the layout bits. The last pass clears each dirty bit that remains.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After the call, <see cref="LayoutNode.Rect"/> of each element in each root holds the new layout.
    /// Set <see cref="ViewportSize"/> before the first call.
    /// </para>
    /// <para>
    /// The layout pass enters only the dirty elements and the elements with new constraints.
    /// A clean root with the same viewport costs one cache check.
    /// A new <see cref="ViewportSize"/> takes effect on the next call with no dirty bit, because the root sees new constraints.
    /// </para>
    /// </remarks>
    /// <param name="deltaTime">The time since the last update, in seconds. No pass reads it yet. The framework and the transitions read it later.</param>
    public void Update(float deltaTime)
    {
        ReadOnlySpan<Element> roots = _roots.AsSpan();

        // Seam: the style pass of the style-system epic goes here.

        // The layout pass. It lays out each root against the viewport size and clears the layout bits.
        float2 viewport = _viewportSize;
        for (int i = 0; i < roots.Length; i++)
        {
            LayoutEngine.Calculate(roots[i], viewport.X, viewport.Y);
        }

        // The clear pass. It clears the bits that no earlier pass cleared.
        for (int i = 0; i < roots.Length; i++)
        {
            ClearVisitor clear = default;
            DirtyWalk.Walk(roots[i], DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint, ref clear);
        }
    }

    /// <summary>
    /// Draws each root and its subtree with a renderer, in tree order. The roots come in the order of add.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="Update(float)"/> first, then <see cref="Draw(IUiRenderer)"/>. The draw reads
    /// <see cref="LayoutNode.Rect"/> of each element, so it uses the layout of the last update.
    /// </para>
    /// <para>
    /// The call is <see cref="IUiRenderer.Begin"/> with <see cref="ViewportSize"/> and a scale of 1, the calls of the
    /// draw walk, and <see cref="IUiRenderer.End"/>. A parent draws before its children. An element with an overflow
    /// other than <see cref="Overflow.Visible"/> clips its content and its children to its padding box and to the clip
    /// of its parent.
    /// </para>
    /// <para>
    /// The draw reads the tree only. It sets and clears no dirty bit. A renderer or a hook must not change the tree
    /// and must not call <see cref="Draw(IUiRenderer)"/> during a draw.
    /// </para>
    /// <para>
    /// After an exception from the renderer, the state of the renderer is undefined. The context stays valid, and
    /// the next call starts a new frame.
    /// </para>
    /// <para>The draw allocates no managed memory after warm-up. The frame stack grows for a deeper tree and never shrinks.</para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the calls.</param>
    /// <exception cref="ArgumentNullException"><paramref name="renderer"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A draw of this context runs already.</exception>
    public void Draw(IUiRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (_drawing)
        {
            throw new InvalidOperationException("A draw of this UiContext runs already. Do not call Draw from a renderer or a hook.");
        }

        _drawing = true;
        try
        {
            renderer.Begin(_viewportSize, 1f);
            PaintWalk.Walk(_roots.AsSpan(), renderer, ref _paintStack);
            renderer.End();
        }
        finally
        {
            _drawing = false;
        }
    }

    /// <summary>
    /// Checks one component of a viewport size. <c>NaN</c> is valid and means no limit.
    /// </summary>
    /// <param name="component">The width or the height.</param>
    /// <returns><see langword="true"/> for <c>NaN</c> and for a finite value of 0 or more.</returns>
    private static bool IsValidViewportComponent(float component)
    {
        return float.IsNaN(component) || (component >= 0f && float.IsFinite(component));
    }
}
