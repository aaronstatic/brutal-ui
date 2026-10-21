using System;
using System.Diagnostics;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// A node of the UI tree. An element has one parent at most, an ordered child list, and dirty bits.
/// </summary>
/// <remarks>
/// <para>
/// The tree is main-thread only. It uses no locks.
/// A change of the tree sets dirty bits on the element and on its ancestors.
/// <see cref="UiContext.Update(float)"/> walks the dirty bits and clears them.
/// </para>
/// <para>
/// The inline style set lives in the element, next to the computed style (A1). Thus an inline write needs no lookup
/// and no allocation. Each element holds the full set, also when it has no inline value.
/// </para>
/// </remarks>
public class Element
{
    private ElementStorage _children;
    private ClassSet _classes;
    private DirtyFlags _dirty;
    private Element? _parent;
    private ComputedStyle _style;
    private StyleProperties _inline;
    private readonly InlineStyle _inlineView;
    private LayoutNode _layout;
    private PseudoStates _states;
    private string? _name;

    private static readonly ElementTypeId s_typeId = ElementType.Intern("Element");

    /// <summary>
    /// Creates an element with no parent and no children. The element starts dirty for
    /// <see cref="DirtyFlags.Style"/>, <see cref="DirtyFlags.Layout"/>, and <see cref="DirtyFlags.Paint"/>.
    /// </summary>
    public Element()
    {
        _dirty = DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint;
        _inlineView = new InlineStyle(this);
    }

    /// <summary>Gets the type id of the element. A type selector of a style rule matches it.</summary>
    /// <remarks>
    /// A subclass overrides the property with its own name. A subclass with no override has the id of its base class.
    /// The property reads a static field and allocates nothing.
    /// </remarks>
    public virtual ElementTypeId TypeId => s_typeId;

    /// <summary>Gets or sets the name of the element. A name selector of a style rule matches it.</summary>
    /// <remarks>
    /// <para>
    /// A set to a new name marks the element and its full subtree for <see cref="DirtyFlags.Style"/>, because a name
    /// rule of the element, or a chain rule of a descendant, can read the name. The mark is the same as the mark of
    /// <see cref="States"/>. The mark reads no sheet and allocates nothing.
    /// </para>
    /// <para>
    /// The setter compares the old name and the new name with an ordinal compare, so <c>Title</c> and <c>title</c> are
    /// two names. A set to the same name marks nothing. A new element holds <see cref="DirtyFlags.Style"/> already
    /// and has no child, so a name in an object initializer costs one flag write.
    /// </para>
    /// </remarks>
    public string? Name
    {
        get => _name;
        set
        {
            if (string.Equals(_name, value, StringComparison.Ordinal))
            {
                return;
            }

            _name = value;
            MarkSubtreeStyle();
        }
    }

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
    /// <para>
    /// A set to a new value marks the element and its full subtree for <see cref="DirtyFlags.Style"/>, because a chain
    /// rule of a descendant can read the states of this element. Each element of the subtree with a child also gets
    /// <see cref="DirtyFlags.DescendantStyle"/>, and each ancestor gets <see cref="DirtyFlags.DescendantStyle"/>.
    /// The mark reads no sheet. A set to the same value marks nothing.
    /// </para>
    /// <para>
    /// A state change on a high ancestor restyles a large subtree on the next <see cref="UiContext.Update(float)"/>.
    /// The mark allocates nothing.
    /// </para>
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
            MarkSubtreeStyle();
        }
    }

    /// <summary>
    /// Gets or sets a value that tells if the element can be a target of picking. The default is <see langword="true"/>.
    /// </summary>
    /// <remarks>A change of this value sets no dirty bit.</remarks>
    public bool IsPickable { get; set; } = true;

    /// <summary>
    /// Gets a read-only reference to the computed style of the element. The style lives inline in the element.
    /// </summary>
    /// <remarks>
    /// The style pass writes the style in <see cref="UiContext.Update(float)"/>. The game does not write the style.
    /// The game writes <see cref="Inline"/>, and the next update writes the result here. Before the first update, the
    /// style holds the defaults.
    /// </remarks>
    public ref readonly ComputedStyle Style => ref _style;

    /// <summary>
    /// Gets a reference to the computed style for the style pass. The pass writes the result of the cascade through it.
    /// </summary>
    /// <remarks>
    /// <see cref="Style"/> and this member give the same field. Only this member writes the style.
    /// </remarks>
    internal ref ComputedStyle StyleStorage => ref _style;

    /// <summary>
    /// Gets the inline style of the element. An inline value wins over each rule. The view allocates nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A set marks the element for <see cref="DirtyFlags.Style"/>, and the next <see cref="UiContext.Update(float)"/>
    /// writes the result to <see cref="Style"/>. Write a whole <see cref="LengthEdges"/> or <see cref="Corners"/>
    /// value: a write of one part through the view does not compile (CS1612).
    /// </para>
    /// <para>
    /// The member returns a <c>ref readonly</c> to a view that the element creates one time. C# accepts a property
    /// set on a variable only, and a <c>ref</c> return is a variable. Thus <c>element.Inline.Width = 200;</c> compiles.
    /// The <c>readonly</c> stops a write of a new view over the view of the element.
    /// </para>
    /// </remarks>
    public ref readonly InlineStyle Inline => ref _inlineView;

    /// <summary>
    /// Gets the inline set of the element. <see cref="InlineStyle"/> writes it, and the style pass applies it last.
    /// </summary>
    /// <remarks>
    /// The member returns a <c>ref</c>, so a change goes to the field and not to a copy. A write through the
    /// reference sets no dirty bit. Write through <see cref="Inline"/>.
    /// </remarks>
    internal ref StyleProperties InlineStorage => ref _inline;

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
    /// Marks the element and each descendant for <see cref="DirtyFlags.Style"/>, so the next style pass restyles the
    /// full subtree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each element of the subtree gets <see cref="DirtyFlags.Style"/>. Each element of the subtree with a child gets
    /// <see cref="DirtyFlags.DescendantStyle"/>. Then <see cref="DirtyFlags.DescendantStyle"/> goes up the parent
    /// chain from this element. Thus the dirty-bit invariant holds after the call.
    /// </para>
    /// <para>
    /// A sheet change calls this method on each root. A change of <see cref="States"/>, a change of
    /// <see cref="Name"/>, and a change of the <see cref="Classes"/> call it on the changed element. The recursion depth is the depth of the subtree.
    /// The method allocates nothing.
    /// </para>
    /// </remarks>
    internal void MarkSubtreeStyle()
    {
        MarkSubtreeStyleCore(this);
        PropagateToAncestors(DirtyFlags.DescendantStyle);
    }

    /// <summary>Sets the style bits on one element and on its subtree, in tree order.</summary>
    /// <param name="element">The root of the subtree.</param>
    private static void MarkSubtreeStyleCore(Element element)
    {
        element._dirty |= DirtyFlags.Style;

        ReadOnlySpan<Element> children = element._children.AsSpan();
        if (children.Length == 0)
        {
            return;
        }

        element._dirty |= DirtyFlags.DescendantStyle;
        for (int i = 0; i < children.Length; i++)
        {
            MarkSubtreeStyleCore(children[i]);
        }
    }

    /// <summary>
    /// Links the element to a new parent and applies the child part of the add marks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The element and each descendant get the <see cref="DirtyFlags.Style"/> self bit, because the cascade of the
    /// full subtree can change. A move into another context changes the sheets of each element of the subtree.
    /// Each element of the subtree with a child also gets <see cref="DirtyFlags.DescendantStyle"/>.
    /// </para>
    /// <para>
    /// Then all the bits of the element go up the new parent chain as descendant bits, from the new parent.
    /// The caller adds the element to the child storage of the parent first, and marks the parent for layout.
    /// The subtree mark costs one walk of the subtree and allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="parent">The new parent.</param>
    internal void AttachToParent(Element parent)
    {
        Debug.Assert(_parent is null, "The element must have no parent before an attach.");

        _parent = parent;
        MarkSubtreeStyleCore(this);

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
