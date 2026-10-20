using System.Diagnostics;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// A node of the UI tree. An element has one parent at most, an ordered child list, and dirty bits.
/// </summary>
/// <remarks>
/// The tree is main-thread only. It uses no locks.
/// A change of the tree sets dirty bits on the element and on its ancestors.
/// <see cref="UiContext.Update(float)"/> walks the dirty bits and clears them.
/// </remarks>
public class Element
{
    private ElementStorage _children;
    private ClassSet _classes;
    private DirtyFlags _dirty;
    private Element? _parent;
    private ComputedStyle _style;
    private LayoutNode _layout;
    private PseudoStates _states;

    /// <summary>
    /// Creates an element with no parent and no children. The element starts dirty for
    /// <see cref="DirtyFlags.Style"/>, <see cref="DirtyFlags.Layout"/>, and <see cref="DirtyFlags.Paint"/>.
    /// </summary>
    public Element()
    {
        _dirty = DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint;
    }

    /// <summary>Gets or sets the name of the element. A change of the name sets no dirty bit.</summary>
    public string? Name { get; set; }

    /// <summary>Gets the parent of the element, or <see langword="null"/> when the element has no parent.</summary>
    public Element? Parent => _parent;

    /// <summary>
    /// Gets or sets a value that tells if the element is a root of a <see cref="UiContext"/>.
    /// </summary>
    /// <remarks>
    /// Only <see cref="UiContext.AddRoot(Element)"/> and <see cref="UiContext.RemoveRoot(Element)"/> change it.
    /// A root cannot join a child list. The value is a flag and not a context reference.
    /// </remarks>
    internal bool IsRoot { get; set; }

    /// <summary>Gets the child list of the element. The list is a view. It allocates nothing.</summary>
    public ElementList Children => new(this);

    /// <summary>Gets the class list of the element. The list is a view. It allocates nothing.</summary>
    public ClassList Classes => new(this);

    /// <summary>Gets all the dirty bits of the element: the self bits and the descendant bits.</summary>
    public DirtyFlags Dirty => _dirty;

    /// <summary>
    /// Gets or sets the pseudo-states of the element.
    /// </summary>
    /// <remarks>
    /// A set to a new value marks the element for <see cref="DirtyFlags.Style"/>.
    /// A set to the same value marks nothing.
    /// </remarks>
    public PseudoStates States
    {
        get => _states;
        set
        {
            if (_states == value)
            {
                return;
            }

            _states = value;
            MarkDirty(DirtyFlags.Style);
        }
    }

    /// <summary>
    /// Gets or sets a value that tells if the element can be a target of picking. The default is <see langword="true"/>.
    /// </summary>
    /// <remarks>A change of this value sets no dirty bit.</remarks>
    public bool IsPickable { get; set; } = true;

    /// <summary>
    /// Gets a reference to the computed style of the element. The style lives inline in the element.
    /// </summary>
    /// <remarks>A write through the reference sets no dirty bit. The caller calls <see cref="MarkDirty(DirtyFlags)"/>.</remarks>
    public ref ComputedStyle Style => ref _style;

    /// <summary>
    /// Gets a reference to the layout data of the element. The data lives inline in the element.
    /// </summary>
    /// <remarks>A write through the reference sets no dirty bit. The caller calls <see cref="MarkDirty(DirtyFlags)"/>.</remarks>
    public ref LayoutNode Layout => ref _layout;

    /// <summary>
    /// Gets the class storage. <see cref="ClassList"/> uses it.
    /// </summary>
    /// <remarks>The member returns a <c>ref</c>, so a change goes to the field and not to a copy.</remarks>
    internal ref ClassSet ClassStorage => ref _classes;

    /// <summary>
    /// Gets the child storage. <see cref="ElementList"/> and the dirty walk use it.
    /// </summary>
    /// <remarks>The member returns a <c>ref</c>, so a change goes to the field and not to a copy.</remarks>
    internal ref ElementStorage ChildStorage => ref _children;

    /// <summary>
    /// Marks the element dirty for the given kinds, and gives each ancestor the descendant bits.
    /// </summary>
    /// <remarks>
    /// The method keeps the self bits of <paramref name="kinds"/> and ignores the descendant bits.
    /// The mark stops at the first ancestor that already holds all the descendant bits.
    /// A repeated mark costs one check.
    /// Call this method after a write through a slot that sets no bit.
    /// </remarks>
    /// <param name="kinds">The kinds to mark. Use the self bits: <see cref="DirtyFlags.Style"/>, <see cref="DirtyFlags.Layout"/>, or <see cref="DirtyFlags.Paint"/>.</param>
    public void MarkDirty(DirtyFlags kinds)
    {
        // Step 1: keep the self bits. Return when no bit stays.
        DirtyFlags self = kinds & DirtyBits.SelfMask;
        if (self == DirtyFlags.None)
        {
            return;
        }

        // Step 2: set the self bits on the element.
        _dirty |= self;

        // Step 3: send the descendant bits up the parent chain.
        PropagateToAncestors(DirtyBits.ToDescendant(self));
    }

    /// <summary>
    /// Sets descendant bits on each ancestor, from the parent to the root.
    /// </summary>
    /// <remarks>
    /// For each ancestor, the method computes the bits that the ancestor does not hold.
    /// It stops when that set is empty. Otherwise it sets the missing bits and continues
    /// with the full <paramref name="descendantBits"/>. The stop is safe because of the invariant:
    /// when an element holds a descendant bit, each ancestor also holds it.
    /// <see cref="MarkDirty(DirtyFlags)"/> and the add path both use this method.
    /// </remarks>
    /// <param name="descendantBits">The descendant bits to send up. Each bit is in <see cref="DirtyBits.DescendantMask"/>.</param>
    internal void PropagateToAncestors(DirtyFlags descendantBits)
    {
        Debug.Assert((descendantBits & ~DirtyBits.DescendantMask) == DirtyFlags.None, "Only descendant bits go up the parent chain.");

        for (Element? p = _parent; p is not null; p = p._parent)
        {
            DirtyFlags add = descendantBits & ~p._dirty;
            if (add == DirtyFlags.None)
            {
                return;
            }

            p._dirty |= add;
        }
    }

    /// <summary>
    /// Links the element to a new parent and applies the child part of the add marks.
    /// </summary>
    /// <remarks>
    /// The element gets the <see cref="DirtyFlags.Style"/> self bit, because its cascade changes.
    /// Then all its bits go up the new parent chain as descendant bits, from the new parent.
    /// The caller adds the element to the child storage of the parent first, and marks the parent for layout.
    /// </remarks>
    /// <param name="parent">The new parent.</param>
    internal void AttachToParent(Element parent)
    {
        Debug.Assert(_parent is null, "The element must have no parent before an attach.");

        _parent = parent;
        _dirty |= DirtyFlags.Style;

        DirtyFlags flags = _dirty;
        PropagateToAncestors(DirtyBits.ToDescendant(flags & DirtyBits.SelfMask) | (flags & DirtyBits.DescendantMask));
    }

    /// <summary>
    /// Clears the parent link. The element keeps its dirty bits. A later attach sends them up again.
    /// </summary>
    /// <remarks>The caller removes the element from the child storage of the parent, and marks the parent for layout.</remarks>
    internal void DetachFromParent()
    {
        _parent = null;
    }

    /// <summary>
    /// Clears dirty bits on this element only. The dirty walk calls it. A pass with its own recursion can also call it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pass with its own recursion can call this method. On each element that the pass enters, clear the
    /// self bit and the descendant bit of the walked kind, after the subtree of the element.
    /// Do not clear another kind.
    /// </para>
    /// <para>A clear in another order or of another kind can break the dirty-bit invariant.</para>
    /// </remarks>
    /// <param name="bits">The bits to clear.</param>
    internal void ClearDirty(DirtyFlags bits)
    {
        _dirty &= ~bits;
    }

    /// <summary>
    /// Paints the own content of the element, for example text or an image. The base method does nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The draw walk calls this method for each painted element. The call comes after the walk draws the fill and
    /// pushes the clip, and before the walk visits the first child. Thus the clip of the element also clips its own
    /// content, and the children paint over the content.
    /// </para>
    /// <para>
    /// The walk does not call this method for a hidden element (<see cref="Display.None"/> or an opacity product of 0
    /// or less) or for the subtree of a hidden element.
    /// </para>
    /// <para>
    /// The override must follow these rules: change no part of the tree, set no dirty bit, allocate no managed memory,
    /// and do not call <see cref="UiContext.Draw(IUiRenderer)"/>. The override sends its calls to the renderer only.
    /// </para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the calls.</param>
    /// <param name="borderBox">The border box of the element in viewport pixels: x, y, width, and height.</param>
    /// <param name="opacity">
    /// The product of the opacities from the root to the element, from 0 to 1. The override multiplies its own colors
    /// by this value.
    /// </param>
    internal virtual void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
    {
    }
}
