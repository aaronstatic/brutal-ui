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
/// <para>
/// The element holds a binding list. <see cref="AddBinding(IBindingSource, BindingHandler, object?)"/> adds a source,
/// a static handler, and a target at build time. The binding pass of <see cref="UiContext.Update(float)"/> runs after
/// the input pass and before the style pass. It calls the handler of each binding with a new source version. A subtree
/// flag lets the pass skip each branch with no binding, so a tree with no binding costs one flag check for each root.
/// </para>
/// <para>
/// <see cref="Find(ReadOnlySpan{byte})"/> and <see cref="Find{T}(ReadOnlySpan{byte})"/> in <c>Element.Find.cs</c> give
/// the first element with a name.
/// </para>
/// </remarks>
public partial class Element
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

    /// <summary>
    /// The handler slots of the element, or <see langword="null"/> when the element has no handler (A2). The first
    /// registration creates the object.
    /// </summary>
    private ElementHandlers? _handlers;

    /// <summary>
    /// The binding slots of the element, or <see langword="null"/> when the element has no binding. The first add
    /// creates 2 slots, and each later grow doubles the length. The array never shrinks.
    /// </summary>
    private ElementBinding[]? _bindings;

    /// <summary>The count of the used slots of <see cref="_bindings"/>.</summary>
    private int _bindingCount;

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

    /// <summary>
    /// Gets or sets a value that tells if the style pass wrote a style for the element in its current tree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A new element holds <see langword="false"/>. The style pass sets the value to <see langword="true"/> after it
    /// writes the first style of the element.
    /// </para>
    /// <para>
    /// The first-style rule: the first style of an element in a tree starts no tween. The style pass writes the target
    /// at once. Thus a new screen does not fade in from its defaults, as in CSS. <see cref="TransitionPool"/> reads the
    /// value. An attach and <see cref="UiContext.AddRoot(Element)"/> set the value back to <see langword="false"/> on
    /// the full subtree (<see cref="MarkSubtreeJoined"/>), so an element that joins a tree starts no tween and drops
    /// the record of a running tween. A change of a class, a state, a name, or a sheet keeps the value.
    /// </para>
    /// </remarks>
    internal bool IsStyled { get; set; }

    /// <summary>
    /// Gets or sets a value that tells if the element follows a <see cref="ComputedStyle.Color"/> tween of its parent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The style pass writes the value at each restyle. The value is <see langword="true"/> when no matching rule and
    /// no inline value set the color, and no matching rule holds a <see cref="ComputedStyle.Color"/> transition. Then
    /// the element inherits the color of its parent (or the default color of the context for a root), and it runs no
    /// color tween of its own. A new element holds <see langword="false"/> until its first style.
    /// </para>
    /// <para>
    /// The follow rule: a running <see cref="ComputedStyle.Color"/> tween of an ancestor writes its value into each
    /// descendant with this value <see langword="true"/>, and it marks <see cref="DirtyFlags.Paint"/> only. The walk
    /// stops at an element with this value <see langword="false"/>, because the descendants of that element inherit
    /// from it. The walk also stops at an element with a running color tween of its own. <see cref="TransitionPool"/>
    /// reads the value.
    /// </para>
    /// <para>
    /// Decision R2: an element that inherits the color and holds its own <see cref="ComputedStyle.Color"/> transition
    /// runs its own tween to the target of its parent, and it never follows. A follow after the end of its own tween
    /// writes the visible color of the parent, and the color jumps back while the tween of the parent runs.
    /// </para>
    /// </remarks>
    internal bool FollowsColor { get; set; }

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
    /// Gets or sets a value that tells if the element can hold the keyboard focus. The default is
    /// <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A change of this value sets no dirty bit. <see cref="UiContext.SetFocus(Element)"/>, a click, and a Tab read
    /// it. A change does not move the focus at once.
    /// </para>
    /// <para>
    /// A disabled element takes no focus by click, by Tab, or by code. An element is disabled when
    /// <see cref="States"/> holds <see cref="PseudoStates.Disabled"/>. A press on a disabled element with this value
    /// clears the focus. The press does not give the focus to a focusable ancestor.
    /// </para>
    /// <para>
    /// At the start of each drain, the context drops the focus of an owner that turns unfocusable or disabled. The
    /// owner holds the focus until that drain.
    /// </para>
    /// </remarks>
    public bool IsFocusable { get; set; }

    /// <summary>
    /// Gets a value that tells if the element accepts the keyboard focus now: <see cref="IsFocusable"/> is
    /// <see langword="true"/> and <see cref="States"/> does not hold <see cref="PseudoStates.Disabled"/>.
    /// </summary>
    /// <remarks>
    /// Each focus rule reads this member only: <see cref="UiContext.SetFocus(Element)"/>, the click, the Tab search,
    /// and the stale rule. The member reads no style, so the hidden rule is not part of it.
    /// </remarks>
    internal bool AcceptsFocus => IsFocusable && (States & PseudoStates.Disabled) == 0;

    /// <summary>
    /// Gets the handler slots of the element, or <see langword="null"/> when no handler was set on the element.
    /// </summary>
    /// <remarks>
    /// The event walk reads this member one time for each element on the path. It skips an element with no side
    /// object. A set of a <see langword="null"/> handler on an element with no side object creates no side object.
    /// </remarks>
    internal ElementHandlers? Handlers => _handlers;

    /// <summary>
    /// Gets or sets the bubble handler for <see cref="UiEventType.PointerMove"/>. The target phase also calls it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The slots live in a side object (A2). The first registration of a handler on the element creates the side
    /// object. An element with no handler holds one <see langword="null"/> reference. A set of
    /// <see langword="null"/> on an element with no side object does nothing.
    /// </para>
    /// <para>
    /// <c>+=</c> combines delegates into a multicast slot. The walk calls each delegate in the order of add.
    /// Register handlers at build time, not in the frame loop.
    /// </para>
    /// </remarks>
    public UiEventHandler<PointerEvent>? OnPointerMove
    {
        get => _handlers?.PointerMove;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).PointerMove = value;
        }
    }

    /// <summary>
    /// Gets or sets the capture handler for <see cref="UiEventType.PointerMove"/>. The capture phase calls it on an
    /// ancestor of the target. The target phase calls it before the bubble handler.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<PointerEvent>? OnPointerMoveCapture
    {
        get => _handlers?.PointerMoveCapture;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).PointerMoveCapture = value;
        }
    }

    /// <summary>
    /// Gets or sets the bubble handler for <see cref="UiEventType.PointerDown"/>. The target phase also calls it.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<PointerEvent>? OnPointerDown
    {
        get => _handlers?.PointerDown;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).PointerDown = value;
        }
    }

    /// <summary>
    /// Gets or sets the capture handler for <see cref="UiEventType.PointerDown"/>. The capture phase calls it on an
    /// ancestor of the target. The target phase calls it before the bubble handler.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<PointerEvent>? OnPointerDownCapture
    {
        get => _handlers?.PointerDownCapture;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).PointerDownCapture = value;
        }
    }

    /// <summary>
    /// Gets or sets the bubble handler for <see cref="UiEventType.PointerUp"/>. The target phase also calls it.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<PointerEvent>? OnPointerUp
    {
        get => _handlers?.PointerUp;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).PointerUp = value;
        }
    }

    /// <summary>
    /// Gets or sets the capture handler for <see cref="UiEventType.PointerUp"/>. The capture phase calls it on an
    /// ancestor of the target. The target phase calls it before the bubble handler.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<PointerEvent>? OnPointerUpCapture
    {
        get => _handlers?.PointerUpCapture;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).PointerUpCapture = value;
        }
    }

    /// <summary>
    /// Gets or sets the bubble handler for <see cref="UiEventType.Scroll"/>. The target phase also calls it.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<ScrollEvent>? OnScroll
    {
        get => _handlers?.Scroll;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).Scroll = value;
        }
    }

    /// <summary>
    /// Gets or sets the capture handler for <see cref="UiEventType.Scroll"/>. The capture phase calls it on an
    /// ancestor of the target. The target phase calls it before the bubble handler.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<ScrollEvent>? OnScrollCapture
    {
        get => _handlers?.ScrollCapture;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).ScrollCapture = value;
        }
    }

    /// <summary>
    /// Gets or sets the bubble handler for <see cref="UiEventType.KeyDown"/>. The target phase also calls it.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<KeyEvent>? OnKeyDown
    {
        get => _handlers?.KeyDown;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).KeyDown = value;
        }
    }

    /// <summary>
    /// Gets or sets the capture handler for <see cref="UiEventType.KeyDown"/>. The capture phase calls it on an
    /// ancestor of the target. The target phase calls it before the bubble handler.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<KeyEvent>? OnKeyDownCapture
    {
        get => _handlers?.KeyDownCapture;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).KeyDownCapture = value;
        }
    }

    /// <summary>
    /// Gets or sets the bubble handler for <see cref="UiEventType.KeyUp"/>. The target phase also calls it.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<KeyEvent>? OnKeyUp
    {
        get => _handlers?.KeyUp;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).KeyUp = value;
        }
    }

    /// <summary>
    /// Gets or sets the capture handler for <see cref="UiEventType.KeyUp"/>. The capture phase calls it on an
    /// ancestor of the target. The target phase calls it before the bubble handler.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<KeyEvent>? OnKeyUpCapture
    {
        get => _handlers?.KeyUpCapture;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).KeyUpCapture = value;
        }
    }

    /// <summary>
    /// Gets or sets the bubble handler for <see cref="UiEventType.Char"/>. The target phase also calls it.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<CharEvent>? OnChar
    {
        get => _handlers?.Char;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).Char = value;
        }
    }

    /// <summary>
    /// Gets or sets the capture handler for <see cref="UiEventType.Char"/>. The capture phase calls it on an
    /// ancestor of the target. The target phase calls it before the bubble handler.
    /// </summary>
    /// <remarks>
    /// The slot follows the rules of <see cref="OnPointerMove"/>: the first registration creates the side object, and
    /// <c>+=</c> combines delegates in the order of add.
    /// </remarks>
    public UiEventHandler<CharEvent>? OnCharCapture
    {
        get => _handlers?.CharCapture;
        set
        {
            if (value is null && _handlers is null)
            {
                return;
            }

            (_handlers ??= new ElementHandlers()).CharCapture = value;
        }
    }

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

    /// <summary>Gets the count of the bindings of the element.</summary>
    /// <remarks>The count rises by one at each <see cref="AddBinding(IBindingSource, BindingHandler, object?)"/>.</remarks>
    public int BindingCount => _bindingCount;

    /// <summary>Gets the used binding slots of the element, in the order of add. The span allocates nothing.</summary>
    /// <remarks>The span is a view of the current array. A later add can replace the array, so read the span again.</remarks>
    internal ReadOnlySpan<ElementBinding> Bindings => new(_bindings, 0, _bindingCount);

    /// <summary>
    /// Gets or sets a value that tells if the element or a descendant can hold a binding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AddBinding(IBindingSource, BindingHandler, object?)"/> and an attach set the value up the parent
    /// chain, to the first ancestor that holds it. The binding pass skips each element with the value
    /// <see langword="false"/>. After it visits an element, the pass writes the value again from the own binding count
    /// and the values of the children.
    /// </para>
    /// <para>
    /// The value is a hint. A remove leaves a stale <see langword="true"/> on the old ancestors, and the next pass
    /// clears it. The value is never a stale <see langword="false"/>: when an element in a tree holds
    /// <see langword="true"/>, each ancestor also holds it.
    /// </para>
    /// </remarks>
    internal bool HasBoundSubtree { get; set; }

    /// <summary>
    /// Adds a binding to the element: a source, a handler, and a target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Add a binding at build time. The list grows by doubling and never shrinks, so the add can allocate.
    /// </para>
    /// <para>
    /// The first <see cref="UiContext.Update(float)"/> applies the binding, and each later update applies it one time
    /// for each new version of <paramref name="source"/>. The bindings of one element apply in the order of add.
    /// </para>
    /// <para>
    /// The element holds the source, and the source holds no reference to the element. Pass a static handler in a
    /// <c>static readonly</c> field, so the add allocates no delegate.
    /// </para>
    /// </remarks>
    /// <param name="source">The source of the bound value. The binding pass reads its version.</param>
    /// <param name="handler">The handler that writes the value into the element.</param>
    /// <param name="target">The object that the binding pass gives to <paramref name="handler"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public void AddBinding(IBindingSource source, BindingHandler handler, object? target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(handler);

        // Step 1: grow the array when it is full. The first add creates 2 slots.
        if (_bindings is null)
        {
            _bindings = new ElementBinding[2];
        }
        else if (_bindingCount == _bindings.Length)
        {
            Array.Resize(ref _bindings, _bindings.Length * 2);
        }

        // Step 2: write the slot. The applied version 0 makes the first pass apply the binding.
        _bindings[_bindingCount] = new ElementBinding
        {
            Source = source,
            AppliedVersion = 0,
            Handler = handler,
            Target = target,
        };
        _bindingCount++;

        // Step 3: mark the element and the parent chain, so the pass enters the branch.
        MarkBoundChain(this);
    }

    /// <summary>
    /// Drops each binding of the element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method writes <see langword="default"/> into each used slot, so the element drops its references to each
    /// source and each target. It sets <see cref="BindingCount"/> to 0 and keeps the array. It does not change the
    /// bound-subtree flag: the next binding pass clears a stale flag.
    /// </para>
    /// <para>
    /// The leave rule: an element outside the roots of a context applies nothing, because the binding pass walks from
    /// the roots only. The element keeps its bindings, and the first <see cref="UiContext.Update(float)"/> after a join
    /// applies a new version. Call this method to drop the bindings for good. A store holds no reference to an
    /// element, so a dropped element needs no unsubscribe.
    /// </para>
    /// </remarks>
    public void ClearBindings()
    {
        if (_bindings is not null)
        {
            Array.Clear(_bindings, 0, _bindingCount);
        }

        _bindingCount = 0;
    }

    /// <summary>Gets a reference to the binding slot at an index. The binding pass writes the applied version through it.</summary>
    /// <remarks>
    /// The reference points into the current array. A later add can replace the array, so get the reference again
    /// after each handler call.
    /// </remarks>
    /// <param name="index">The index of the slot, from 0 to <see cref="BindingCount"/> minus 1.</param>
    /// <returns>A reference to the slot.</returns>
    internal ref ElementBinding BindingAt(int index)
    {
        Debug.Assert((uint)index < (uint)_bindingCount, "The index must name a used binding slot.");
        return ref _bindings![index];
    }

    /// <summary>
    /// Sets <see cref="HasBoundSubtree"/> on an element and on each ancestor, up to the first element that holds it.
    /// </summary>
    /// <remarks>The stop is safe, because each ancestor of an element with the value also holds it.</remarks>
    /// <param name="start">The first element to mark, or <see langword="null"/> for no mark.</param>
    private static void MarkBoundChain(Element? start)
    {
        for (Element? e = start; e is not null && !e.HasBoundSubtree; e = e._parent)
        {
            e.HasBoundSubtree = true;
        }
    }

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
    /// Marks the element and each descendant for <see cref="DirtyFlags.Style"/> as a join, so the next style pass
    /// restyles the full subtree and starts no tween in it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method sets the style bits as <see cref="MarkSubtreeStyle"/> does, and it sets <see cref="IsStyled"/> to
    /// <see langword="false"/> on each element of the subtree. Then <see cref="DirtyFlags.DescendantStyle"/> goes up
    /// the parent chain from this element.
    /// </para>
    /// <para>
    /// The join rule (Decision 9): the first style of an element in a tree starts no tween, and it drops the record of
    /// a running tween. <see cref="UiContext.AddRoot(Element)"/> calls this method. An attach calls the core walk. A
    /// change of a class, a state, a name, or a sheet calls <see cref="MarkSubtreeStyle"/> and keeps
    /// <see cref="IsStyled"/>. The recursion depth is the depth of the subtree. The method allocates nothing.
    /// </para>
    /// </remarks>
    internal void MarkSubtreeJoined()
    {
        MarkSubtreeJoinedCore(this);
        PropagateToAncestors(DirtyFlags.DescendantStyle);
    }

    /// <summary>
    /// Sets the style bits on one element and on its subtree, in tree order, and sets <see cref="IsStyled"/> to
    /// <see langword="false"/> on each element.
    /// </summary>
    /// <param name="element">The root of the subtree.</param>
    private static void MarkSubtreeJoinedCore(Element element)
    {
        element._dirty |= DirtyFlags.Style;
        element.IsStyled = false;

        ReadOnlySpan<Element> children = element._children.AsSpan();
        if (children.Length == 0)
        {
            return;
        }

        element._dirty |= DirtyFlags.DescendantStyle;
        for (int i = 0; i < children.Length; i++)
        {
            MarkSubtreeJoinedCore(children[i]);
        }
    }

    /// <summary>
    /// Links the element to a new parent and applies the child part of the add marks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The element and each descendant get the <see cref="DirtyFlags.Style"/> self bit, because the cascade of the
    /// full subtree can change. A move into another context changes the sheets of each element of the subtree.
    /// Each element of the subtree with a child also gets <see cref="DirtyFlags.DescendantStyle"/>. Each element of
    /// the subtree gets <see cref="IsStyled"/> <see langword="false"/>, so the next restyle starts no tween and drops
    /// the record of a running tween (the join rule, Decision 9).
    /// </para>
    /// <para>
    /// Then all the bits of the element go up the new parent chain as descendant bits, from the new parent.
    /// The caller adds the element to the child storage of the parent first, and marks the parent for layout.
    /// The subtree mark costs one walk of the subtree and allocates nothing.
    /// </para>
    /// <para>
    /// When the element holds <see cref="HasBoundSubtree"/>, the method also sets it on each new ancestor, up to the
    /// first ancestor that holds it. Thus the binding pass enters the new branch.
    /// </para>
    /// </remarks>
    /// <param name="parent">The new parent.</param>
    internal void AttachToParent(Element parent)
    {
        Debug.Assert(_parent is null, "The element must have no parent before an attach.");

        _parent = parent;
        MarkSubtreeJoinedCore(this);

        DirtyFlags flags = _dirty;
        PropagateToAncestors(DirtyBits.ToDescendant(flags & DirtyBits.SelfMask) | (flags & DirtyBits.DescendantMask));

        // A bound subtree marks the new parent chain, so the binding pass enters the new branch.
        if (HasBoundSubtree)
        {
            MarkBoundChain(parent);
        }
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
