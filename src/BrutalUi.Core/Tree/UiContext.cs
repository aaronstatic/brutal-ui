using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The owner of the root elements. <see cref="Update(float)"/> runs the input pass, then the other passes on the dirty
/// elements in frame order. <see cref="Draw(IUiRenderer)"/> then sends the tree to a renderer.
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
/// <para>
/// A <see cref="Label"/> measures its text with the <see cref="Measurer"/> of its context during
/// <see cref="Update(float)"/>. Create the context with <see cref="UiContext(in CreateInfo)"/> when the tree holds
/// a label. A context with no measurer throws from <see cref="Update(float)"/> when a label needs a measure.
/// </para>
/// <para>
/// The measurer and the renderer of <see cref="Draw(IUiRenderer)"/> must use the same font ids. Pass the renderer
/// that draws, or a renderer that shares its font service. Otherwise the layout and the draw use different fonts.
/// </para>
/// </remarks>
public sealed class UiContext
{
    /// <summary>The create-info of a <see cref="UiContext"/>.</summary>
    public struct CreateInfo
    {
        /// <summary>
        /// The renderer that measures text for the layout pass. It must not be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The measurer and the renderer of <see cref="Draw(IUiRenderer)"/> must use the same font ids. Pass the
        /// renderer that draws, or a renderer that shares its font service.
        /// </remarks>
        public required IUiRenderer Measurer;

        /// <summary>
        /// The text color of each root with no rule and no inline color. <see langword="null"/> gives opaque white
        /// (255, 255, 255, 255).
        /// </summary>
        /// <remarks>
        /// The descendants inherit the color. <c>default(Rgba8)</c> is transparent and draws no text, so the default
        /// of this member is <see langword="null"/> and not <c>default(Rgba8)</c>.
        /// </remarks>
        public Rgba8? DefaultColor;

        /// <summary>
        /// The font of each root with no rule and no inline font. The default is the id 0, the default font of the
        /// renderer. The descendants inherit the font.
        /// </summary>
        public FontHandle DefaultFont;

        /// <summary>
        /// The font size in pixels of each root with no rule and no inline size. The default is 0, the default size
        /// of the font. The descendants inherit the size.
        /// </summary>
        public float DefaultFontSize;

        /// <summary>
        /// The number of entries of the event ring. The default is 0, which gives
        /// <see cref="DefaultEventQueueCapacity"/>.
        /// </summary>
        /// <remarks>
        /// The constructor creates the ring with this capacity, so a push never allocates. A full ring drops the
        /// oldest entry.
        /// </remarks>
        public int EventQueueCapacity;
    }

    /// <summary>The number of entries of the event ring when the create-info gives 0, or for <see cref="UiContext()"/>.</summary>
    public const int DefaultEventQueueCapacity = 256;

    /// <summary>
    /// The measurer of the <see cref="Update(float)"/> call that runs on this thread. It is <see langword="null"/>
    /// outside <see cref="Update(float)"/>.
    /// </summary>
    /// <remarks>
    /// The field is thread-static because tests lay out trees on more than one thread at the same time.
    /// </remarks>
    [ThreadStatic]
    private static IUiRenderer? t_measurer;

    private readonly IUiRenderer? _measurer;
    private ElementStorage _roots;
    private float2 _viewportSize;
    private PaintFrame[] _paintStack = new PaintFrame[PaintWalk.InitialStackSize];

    /// <summary>
    /// The frame stack of the hit walk. The walk grows it by doubling and never shrinks it (Decision 3).
    /// </summary>
    private HitFrame[] _hitStack = new HitFrame[HitWalk.InitialStackSize];

    /// <summary>
    /// The frame stack of the focus walk. The walk grows it by doubling and never shrinks it (Decision 6 of
    /// keyboard-focus). It is not the stack of the hit walk.
    /// </summary>
    private FocusFrame[] _focusStack = new FocusFrame[FocusWalk.InitialStackSize];

    /// <summary>
    /// The result of the last hit test of a pointer entry. It is also the element with <see cref="PseudoStates.Hover"/>
    /// from this context (Decision 7).
    /// </summary>
    private Element? _pointerHit;

    /// <summary>The position of the last hit-tested pointer entry, in viewport pixels.</summary>
    private float2 _pointerPosition;

    /// <summary>True after the first hit-tested pointer entry.</summary>
    private bool _hasPointerPosition;

    /// <summary>
    /// The element with <see cref="PseudoStates.Active"/> from this context, or <see langword="null"/>. A press sets
    /// it and the release of the last button clears it after the walk of the release (Decision 8, and Decision 2 of
    /// button-and-toggle). <see cref="_activeButton"/> and <see cref="_activeChord"/> tell which press set it
    /// (Decision 13 of slider).
    /// </summary>
    private Element? _activeElement;

    /// <summary>
    /// True from the release of the last button to the end of the walk of that release. Then
    /// <see cref="FinishRelease"/> clears <see cref="PseudoStates.Active"/> on <see cref="_activeElement"/> (Decision 2
    /// of button-and-toggle).
    /// </summary>
    private bool _releasePending;

    /// <summary>
    /// The button of the press that set <see cref="PseudoStates.Active"/> on <see cref="_activeElement"/> (Decision 13
    /// of slider). <see cref="PressButton"/> writes it when it sets the state.
    /// </summary>
    /// <remarks>
    /// The value is valid only while <see cref="_activeElement"/> is not <see langword="null"/>. The release does not
    /// clear it, because <see cref="LeftPressElement"/> reads <see cref="_activeElement"/> first.
    /// </remarks>
    private PointerButton _activeButton;

    /// <summary>
    /// True when the gesture of <see cref="_activeElement"/> is a chord (Decision 13 of slider): another button was
    /// down at the press that set <see cref="PseudoStates.Active"/>, or a button went down while an element held
    /// the state.
    /// </summary>
    /// <remarks>
    /// <see cref="PressButton"/> writes the flag when it sets the state, and it sets the flag for each later press of
    /// the gesture. The value is valid only while <see cref="_activeElement"/> is not <see langword="null"/>.
    /// </remarks>
    private bool _activeChord;

    /// <summary>
    /// The press target of the walk that runs on this thread (Decision 13 of slider), or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="DrainEvents"/> sets the value after the target step of each entry and writes the old value back
    /// after the walk step and in its <c>finally</c> block, as <see cref="Update(float)"/> does with
    /// <see cref="t_measurer"/>. Thus no value stays between drains, a nested drain of another context does not change
    /// the value of the outer walk, and two contexts on two threads do not share it.
    /// </remarks>
    [ThreadStatic]
    private static Element? t_pressTarget;

    /// <summary>
    /// The element with <see cref="PseudoStates.Focus"/> from this context, or <see langword="null"/>. Each change
    /// goes through <see cref="SetFocusOwner(Element?)"/> (Decision 1 of keyboard-focus).
    /// </summary>
    private Element? _focusOwner;

    /// <summary>
    /// The buttons that are down. The bit <c>1 &lt;&lt; (int)button</c> is set from the press to the release of a
    /// button.
    /// </summary>
    private int _pressedButtons;

    private bool _drawing;
    private bool _draining;

    /// <summary>Tells if the binding pass runs. <see cref="Update(float)"/> throws when a binding handler calls it.</summary>
    private bool _binding;

    /// <summary>
    /// Tells if the before-update handler runs. <see cref="Update(float)"/> throws when the handler calls it.
    /// </summary>
    private bool _beforeUpdate;

    /// <summary>
    /// The ring of queued events. Both constructors create it, so a push never allocates (Decision 8).
    /// </summary>
    /// <remarks>The ring is a mutable struct. Change it only through this field.</remarks>
    private EventQueue _events;

    /// <summary>
    /// The writer of the input state. Both constructors create it, and the three hooks of
    /// <see cref="DrainEvents"/> call it (Decision 2 of input-state).
    /// </summary>
    /// <remarks>The writer is a mutable struct. Change it only through this field.</remarks>
    private InputStateWriter _input;

    /// <summary>
    /// The path buffer of the event walk. The walk grows it by doubling and never shrinks it (A7).
    /// </summary>
    private Element[] _eventPath = new Element[EventWalk.InitialPathSize];

    /// <summary>The sheets in the order of add.</summary>
    private readonly List<StyleSheet> _sheets = [];

    /// <summary>
    /// The rules of each sheet, sorted by the key (specificity, sheet index, rule index). The style pass applies them
    /// in this order. <see cref="AddSheet(StyleSheet)"/> and <see cref="RemoveSheet(StyleSheet)"/> build the array again.
    /// </summary>
    private StyleRule[] _cascade = [];

    /// <summary>
    /// The running tweens of the context. The style pass starts, keeps, turns, and ends them. The transition pass
    /// advances them.
    /// </summary>
    /// <remarks>The field initializer serves both constructors.</remarks>
    private readonly TransitionPool _transitions = new();

    /// <summary>Gets the running tweens of the context.</summary>
    /// <remarks>Only the tests of <c>BrutalUi.Core.Tests</c> read this member. They read the count and the capacity.</remarks>
    internal TransitionPool Transitions => _transitions;

    /// <summary>
    /// The rules of the cascade in the order that the style pass applies them: by specificity, then by sheet index,
    /// then by rule index.
    /// </summary>
    /// <remarks>Only the tests of <c>BrutalUi.Core.Tests</c> read this member. It proves the tie-break of the key.</remarks>
    internal ReadOnlySpan<StyleRule> Cascade => _cascade;

    /// <summary>
    /// The resolved text defaults. Only <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>, and
    /// <see cref="ComputedStyle.FontSize"/> hold a value. The style pass gives them to each root as the parent text.
    /// </summary>
    private readonly ComputedStyle _textDefaults;

    /// <summary>
    /// Creates a context with no roots, a viewport size of (0, 0), no measurer, and the text defaults opaque white,
    /// the font id 0, and the font size 0. The event ring has <see cref="DefaultEventQueueCapacity"/> entries.
    /// </summary>
    /// <remarks>
    /// A context with no measurer cannot lay out a <see cref="Label"/>. Use <see cref="UiContext(in CreateInfo)"/>
    /// for a tree with text.
    /// </remarks>
    public UiContext()
    {
        _textDefaults = CreateTextDefaults(null, default, 0f);
        _events = new EventQueue(DefaultEventQueueCapacity);
        _input = InputStateWriter.Create();
    }

    /// <summary>
    /// Creates a context with no roots, a viewport size of (0, 0), and the measurer, the text defaults, and the event
    /// ring capacity of the create-info.
    /// </summary>
    /// <param name="info">The create-info.</param>
    /// <exception cref="ArgumentNullException">The measurer of <paramref name="info"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The <see cref="CreateInfo.EventQueueCapacity"/> of <paramref name="info"/> is negative.
    /// </exception>
    public UiContext(in CreateInfo info)
    {
        if (info.Measurer is null)
        {
            throw new ArgumentNullException(nameof(info), "The Measurer of the UiContext.CreateInfo must not be null.");
        }

        if (info.EventQueueCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(info),
                info.EventQueueCapacity,
                "The CreateInfo.EventQueueCapacity must not be negative. Use 0 for the default of 256.");
        }

        _measurer = info.Measurer;
        _textDefaults = CreateTextDefaults(info.DefaultColor, info.DefaultFont, info.DefaultFontSize);

        // The ring rejects a capacity of 0, so map 0 to the default first (P1-1).
        int capacity = info.EventQueueCapacity == 0 ? DefaultEventQueueCapacity : info.EventQueueCapacity;
        _events = new EventQueue(capacity);
        _input = InputStateWriter.Create();
    }

    /// <summary>
    /// Gets the renderer that measures text for the layout pass. It is <see langword="null"/> for a context from
    /// <see cref="UiContext()"/>.
    /// </summary>
    /// <remarks>The value is set at creation only.</remarks>
    public IUiRenderer? Measurer => _measurer;

    /// <summary>
    /// Gets the measurer of the <see cref="Update(float)"/> call that runs on this thread. It is
    /// <see langword="null"/> outside <see cref="Update(float)"/> and during an update of a context with no measurer.
    /// </summary>
    internal static IUiRenderer? CurrentMeasurer => t_measurer;

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
    /// Adds a root element at the end of the roots. The root keeps its dirty bits and gets the style bits of a full
    /// restyle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An element that has a parent leaves that parent first. The old parent gets the
    /// <see cref="DirtyFlags.Layout"/> bit.
    /// An element that is a root already throws, also when the root belongs to another context.
    /// </para>
    /// <para>
    /// The call marks the root and each descendant for <see cref="DirtyFlags.Style"/>. Thus the next
    /// <see cref="Update(float)"/> restyles the full subtree with the sheets and the text defaults of this context.
    /// </para>
    /// <para>
    /// The join rule: the call also marks the subtree as not styled, so the next <see cref="Update(float)"/> starts
    /// no tween in it. Each element gets the target of the cascade at once, and a running tween of a moved element
    /// ends with no fade. A record of the element in the pool of another context drops at the next update of that
    /// context with no write.
    /// </para>
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

        // A root from another context or from a parent needs the sheets and the text defaults of this context. The
        // join mark also starts no tween in the subtree (Decision 9).
        root.MarkSubtreeJoined();
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
    /// Adds a style sheet at the end of the sheets. The next <see cref="Update(float)"/> restyles each element of each root.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call allocates: it builds the cascade of the context again. Call it in load code, not in the frame loop.
    /// </para>
    /// <para>
    /// The cascade holds the rules of each sheet, sorted by the specificity, then by the order of the sheets, then by
    /// the order of the rules in the sheet. Thus a rule of a later sheet wins over a rule of an earlier sheet at equal
    /// specificity.
    /// </para>
    /// <para>
    /// The call marks each element of each root that the context holds now for <see cref="DirtyFlags.Style"/>.
    /// A root that the context gets later is marked by <see cref="AddRoot(Element)"/>, so no root misses the sheet.
    /// </para>
    /// </remarks>
    /// <param name="sheet">The sheet to add. Build it with <see cref="StyleSheet.Builder.Build"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sheet"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The context holds the sheet already.</exception>
    public void AddSheet(StyleSheet sheet)
    {
        if (sheet is null)
        {
            throw new ArgumentNullException(nameof(sheet), "The sheet is null. Build a sheet with StyleSheet.Builder.Build.");
        }

        if (_sheets.Contains(sheet))
        {
            throw new InvalidOperationException(
                "The UiContext holds this sheet already. Add each sheet one time, or build a new sheet.");
        }

        _sheets.Add(sheet);
        RebuildCascade();

        // A8: a sheet change can change the style of each element.
        ReadOnlySpan<Element> roots = _roots.AsSpan();
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i].MarkSubtreeStyle();
        }
    }

    /// <summary>
    /// Removes a style sheet. The next <see cref="Update(float)"/> restyles each element of each root.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call allocates: it builds the cascade of the context again. Call it in load code, not in the frame loop.
    /// </para>
    /// <para>
    /// The other sheets keep their order of add. The call marks each element of each root for
    /// <see cref="DirtyFlags.Style"/>. With no sheet left, the pass still restyles each element from the defaults and
    /// the inline set.
    /// </para>
    /// </remarks>
    /// <param name="sheet">The sheet to remove.</param>
    /// <returns><see langword="true"/> when the context held the sheet. Otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sheet"/> is <see langword="null"/>.</exception>
    public bool RemoveSheet(StyleSheet sheet)
    {
        if (sheet is null)
        {
            throw new ArgumentNullException(nameof(sheet), "The sheet is null. Pass a sheet that AddSheet added.");
        }

        if (!_sheets.Remove(sheet))
        {
            return false;
        }

        RebuildCascade();

        // A8: a sheet change can change the style of each element.
        ReadOnlySpan<Element> roots = _roots.AsSpan();
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i].MarkSubtreeStyle();
        }

        return true;
    }

    /// <summary>
    /// Runs the binding pass alone on each root. <see cref="Update(float)"/> calls it after the input pass and before
    /// the style pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pass calls the handler of each binding whose source holds a new version. It skips each branch with no
    /// binding, so a tree with no binding costs one flag check for each root. Only <c>Update</c> calls this method.
    /// </para>
    /// <para>The pass allocates nothing.</para>
    /// </remarks>
    internal void UpdateBindings()
    {
        _binding = true;
        try
        {
            BindingPass.Run(_roots.AsSpan());
        }
        finally
        {
            _binding = false;
        }
    }

    /// <summary>
    /// Runs the style pass alone on each root. <see cref="Update(float)"/> calls it before the layout pass.
    /// </summary>
    /// <remarks>
    /// Tests call this method to read the dirty bits of the style pass before the layout pass clears them.
    /// </remarks>
    internal void UpdateStyles()
    {
        // The pass runs also with an empty cascade, so an inline value reaches the style with no sheet.
        // Each root gets the text defaults of the context as its parent text.
        StylePass.Run(_roots.AsSpan(), _cascade, in _textDefaults, _transitions);
    }

    /// <summary>
    /// Runs the transition pass alone. <see cref="Update(float)"/> calls it after the style pass and before the layout
    /// pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pass advances each running tween by <paramref name="deltaTime"/>, writes its value over the target that
    /// the style pass wrote, and marks <see cref="DirtyFlags.Paint"/> only. Tests call this method to read the dirty
    /// bits of the pass before the clear pass clears them.
    /// </para>
    /// <para>The pass allocates nothing. With no running tween, it costs one count check.</para>
    /// </remarks>
    /// <param name="deltaTime">The time since the last update, in seconds.</param>
    internal void UpdateTransitions(float deltaTime)
    {
        if (_transitions.ActiveCount > 0)
        {
            _transitions.Advance(deltaTime, _roots.AsSpan());
        }
    }

    /// <summary>Gives a style that holds the resolved text defaults. The other members keep their defaults.</summary>
    /// <param name="color">The default color. <see langword="null"/> gives opaque white.</param>
    /// <param name="font">The default font.</param>
    /// <param name="fontSize">The default font size in pixels.</param>
    /// <returns>The style with the three text members set.</returns>
    private static ComputedStyle CreateTextDefaults(Rgba8? color, FontHandle font, float fontSize)
    {
        ComputedStyle defaults = default;
        defaults.Color = color ?? new Rgba8(255, 255, 255, 255);
        defaults.Font = font;
        defaults.FontSize = fontSize;
        return defaults;
    }

    /// <summary>
    /// Builds <see cref="_cascade"/> again from the sheets: the rules of each sheet in the order of add, sorted with
    /// the unique key (specificity, sheet index, rule index).
    /// </summary>
    /// <remarks>The method allocates. Only the sheet calls use it.</remarks>
    private void RebuildCascade()
    {
        int count = 0;
        for (int s = 0; s < _sheets.Count; s++)
        {
            count += _sheets[s].Rules.Length;
        }

        StyleRule[] rules = new StyleRule[count];
        long[] keys = new long[count];
        int index = 0;
        for (int s = 0; s < _sheets.Count; s++)
        {
            ReadOnlySpan<StyleRule> sheetRules = _sheets[s].Rules;
            for (int r = 0; r < sheetRules.Length; r++)
            {
                // The index of the rule in the order of the sheets gives the sheet index and the rule index in one
                // value. The specificity is never negative, so the shift keeps the order, and the key is unique.
                rules[index] = sheetRules[r];
                keys[index] = ((long)sheetRules[r].Specificity << 32) | (uint)index;
                index++;
            }
        }

        Array.Sort(keys, rules);
        _cascade = rules;
    }

    /// <summary>
    /// Runs the input pass, then the other passes on each root in frame order. The layout pass lays out each root
    /// against <see cref="ViewportSize"/> and clears the layout bits. The last pass clears each dirty bit that remains.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After the call, <see cref="LayoutNode.Rect"/> of each element in each root holds the new layout.
    /// Set <see cref="ViewportSize"/> before the first call.
    /// </para>
    /// <para>
    /// The before-update slot of <see cref="OnBeforeUpdate"/> runs first. The handler can change the tree, and the
    /// passes of the same call see the change. The hit test of the input pass reads the layout of the last
    /// <see cref="Update(float)"/>, so a root that the slot adds gets no hit until its first layout.
    /// </para>
    /// <para>
    /// The input pass runs first (A5). It drains each entry that the event ring holds at the start of the call, and
    /// it runs the stale rules also when the ring is empty. The hit test reads the layout of the last
    /// <see cref="Update(float)"/>. An event that a handler pushes waits for the next <see cref="Update(float)"/>. A
    /// handler that calls <see cref="Update(float)"/> gets <see cref="InvalidOperationException"/>. An exception from
    /// a handler stops the call before the style pass. The entries after it stay in the ring, and the next
    /// <see cref="Update(float)"/> drains them. Read <see cref="RoutingGate"/> after the call.
    /// </para>
    /// <para>
    /// The input pass drains the ring each frame. The ring holds <see cref="DefaultEventQueueCapacity"/> entries by
    /// default and drops its oldest entry when it is full (Decision 4). A game with long hitches sets
    /// <see cref="CreateInfo.EventQueueCapacity"/> to a larger value. <see cref="DroppedEventCount"/> shows a drop.
    /// </para>
    /// <para>
    /// Limit of the stale rules (CR-8 of pointer-routing): the input pass of one context can clear
    /// <see cref="PseudoStates.Hover"/>, <see cref="PseudoStates.Active"/>, or <see cref="PseudoStates.Focus"/> that
    /// another context set. When you move an element to another context, call <see cref="Update(float)"/> on the old
    /// context before the new context updates. Otherwise <see cref="RoutingGate"/> of the new context can read
    /// <see langword="true"/> while the element has no <see cref="PseudoStates.Hover"/>.
    /// </para>
    /// <para>
    /// The style pass runs after the input pass. It restyles each element with <see cref="DirtyFlags.Style"/> from the
    /// rules of the sheets of <see cref="AddSheet(StyleSheet)"/> and from the inline set of
    /// <see cref="Element.Inline"/>. An inline value wins over each rule. The pass marks the elements with a new style
    /// for the layout and the paint. Each element with the style bit gets a new style from the defaults, the matching
    /// rules, the inline set, and the parent text. An element with no rule and no inline value gets the defaults.
    /// </para>
    /// <para>
    /// The frame order is: the input pass, the binding pass, the style pass, the transition pass, the layout pass, the
    /// caret pass, and the clear pass.
    /// The input pass changes the states, so the style pass of the same call reads the new states. The binding pass
    /// calls the handler of each binding of <see cref="Element.AddBinding(IBindingSource, BindingHandler, object?)"/>
    /// whose source holds a new version. A value that an input handler writes reaches the binding pass of the same
    /// call. A tree with no binding costs one flag check for each root. An exception from a binding handler stops the
    /// call before the style pass. That binding keeps the new version, so it does not apply again until the next new
    /// version. The bindings after it apply at the next <see cref="Update(float)"/>. A binding handler that calls
    /// <see cref="Update(float)"/> gets <see cref="InvalidOperationException"/>. The style pass
    /// writes the target of each member and starts, keeps, turns, or ends the tweens of the transitions of the rules.
    /// The first style of an element starts no tween. The transition pass then advances each running tween by
    /// <paramref name="deltaTime"/>, also the tween that the style pass started in this call. It writes the value of
    /// the tween over the target and marks <see cref="DirtyFlags.Paint"/> only. A tween changes only the colors, the
    /// corner radius, and the opacity, so the layout pass reads no value of a tween.
    /// </para>
    /// <para>
    /// The layout pass enters only the dirty elements and the elements with new constraints.
    /// A clean root with the same viewport costs one cache check.
    /// A new <see cref="ViewportSize"/> takes effect on the next call with no dirty bit, because the root sees new constraints.
    /// </para>
    /// <para>
    /// A <see cref="Label"/> measures its text with <see cref="Measurer"/> during this call only. The call puts the
    /// measurer into a scope of the current thread for the layout pass, and it restores the old scope at the end,
    /// also after an exception. A nested call thus keeps the scope of the outer call.
    /// </para>
    /// <para>
    /// The caret pass runs after the layout pass, so the text, the style, and the size of the frame are final. It runs
    /// only when <see cref="FocusedElement"/> is a <see cref="TextField"/>. It advances the blink of the caret by
    /// <paramref name="deltaTime"/> and writes the text scroll of the field, so the caret stays visible. The pass
    /// measures the text with <see cref="Measurer"/> directly, not through the measure scope. A context with no
    /// <see cref="Measurer"/> still advances the blink and keeps the scroll. An exception of the measurer stops the
    /// call before the clear pass, as an exception of the layout pass does.
    /// </para>
    /// </remarks>
    /// <param name="deltaTime">
    /// The time since the last update, in seconds. The transition pass reads it: each running tween advances by this
    /// time. A value of 0 or less, also <c>NaN</c>, advances no tween. An infinite value ends each tween at its target.
    /// The caret pass reads it with the rule of the transition pass for a bad value: a value of 0 or less, also
    /// <c>NaN</c>, advances no blink. An infinite value also advances no blink, so the caret does not hide for good.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// A <see cref="Label"/> needs a measure and the context has no <see cref="Measurer"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// An event handler of the input pass calls this method. The input pass of this context runs already.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A binding handler calls this method. The binding pass of this context runs already.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The before-update slot calls this method. The before-update of this context runs already.
    /// </exception>
    public void Update(float deltaTime)
    {
        // A binding handler runs inside the binding pass. A nested call would run the passes again.
        if (_binding)
        {
            throw new InvalidOperationException("A binding handler must not call Update.");
        }

        // A before-update handler runs at the start of this call. A nested call would run the passes again.
        if (_beforeUpdate)
        {
            throw new InvalidOperationException("A before-update handler must not call Update.");
        }

        // The before-update slot runs first (Q1 of panels-and-modes). The passes of this call read the roots after
        // the slot, so they see a tree that the handler changed.
        UiBeforeUpdateHandler? beforeUpdate = OnBeforeUpdate;
        if (beforeUpdate is not null)
        {
            _beforeUpdate = true;
            try
            {
                beforeUpdate(deltaTime);
            }
            finally
            {
                _beforeUpdate = false;
            }
        }

        // The input pass (A5): the drain walks the queued events and changes the states before the style pass
        // reads them.
        DrainEvents();

        // The binding pass. It applies each new store version before the style pass reads the elements.
        UpdateBindings();

        ReadOnlySpan<Element> roots = _roots.AsSpan();

        // The style pass. It restyles each element with the style bit from the cascade and clears the style bits.
        UpdateStyles();

        // The transition pass. It writes the value of each running tween over the target and marks Paint only.
        UpdateTransitions(deltaTime);

        // The layout pass. It lays out each root against the viewport size and clears the layout bits.
        // The measure scope gives the measurer of this context to the measure callbacks of the labels.
        float2 viewport = _viewportSize;
        IUiRenderer? previousMeasurer = t_measurer;
        t_measurer = _measurer;
        try
        {
            for (int i = 0; i < roots.Length; i++)
            {
                LayoutEngine.Calculate(roots[i], viewport.X, viewport.Y);
            }
        }
        finally
        {
            t_measurer = previousMeasurer;
        }

        // The caret pass. It advances the blink of the focused text field and writes its text scroll.
        if (_focusOwner is TextField field)
        {
            field.AdvanceCaret(deltaTime, _measurer);
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

    /// <summary>Gets the number of events in the event ring. The input pass of the next <see cref="Update(float)"/> walks them.</summary>
    public int QueuedEventCount => _events.Count;

    /// <summary>Gets the number of entries that the event ring holds at most.</summary>
    public int EventQueueCapacity => _events.Capacity;

    /// <summary>Gets the length of the path buffer of the event walk. This member serves the tests.</summary>
    internal int EventPathLength => _eventPath.Length;

    /// <summary>Gets the slots of the path buffer of the event walk. Each slot is null after a walk.</summary>
    /// <remarks>Only the tests of <c>BrutalUi.Core.Tests</c> read this member.</remarks>
    internal ReadOnlySpan<Element?> EventPathSlots => _eventPath;

    /// <summary>Gets the slots of the event ring. A popped slot holds no element reference.</summary>
    /// <remarks>Only the tests of <c>BrutalUi.Core.Tests</c> read this member.</remarks>
    internal ReadOnlySpan<UiQueuedEvent> EventRingSlots => _events.Slots;

    /// <summary>Gets the length of the frame stack of the hit walk. This member serves the tests.</summary>
    internal int HitStackLength => _hitStack.Length;

    /// <summary>Gets the slots of the frame stack of the hit walk. Each slot holds no element after a hit test.</summary>
    /// <remarks>Only the tests of <c>BrutalUi.Core.Tests</c> read this member.</remarks>
    internal ReadOnlySpan<HitFrame> HitStackSlots => _hitStack;

    /// <summary>
    /// The key code of the Tab key. The drain compares <see cref="KeyEvent.Key"/> with this value for the Tab move.
    /// </summary>
    /// <remarks>
    /// The value is the GLFW value of <c>GLFW_KEY_TAB</c>. Core has no GLFW reference, so it holds the number. A test
    /// of <c>BrutalUi.Glfw.Tests</c> checks the value against the GLFW enum.
    /// </remarks>
    internal const int TabKey = 258;

    /// <summary>Gets the length of the frame stack of the focus walk. This member serves the tests.</summary>
    internal int FocusStackLength => _focusStack.Length;

    /// <summary>
    /// Gets the slots of the frame stack of the focus walk. Each slot holds no element after a focus move.
    /// </summary>
    /// <remarks>Only the tests of <c>BrutalUi.Core.Tests</c> read this member.</remarks>
    internal ReadOnlySpan<FocusFrame> FocusStackSlots => _focusStack;

    /// <summary>
    /// Gets the result of the hit test of the last pointer entry with no target, or <see langword="null"/> when that
    /// entry hit nothing. This element also holds <see cref="PseudoStates.Hover"/> from this context.
    /// </summary>
    /// <remarks>
    /// <see cref="RoutingGate"/> reads this member (A6). The value follows the pointer events only. An entry with a
    /// target from the caller does not change it. The value changes only during a drain: a pointer entry with no
    /// target sets it, and the start of a drain sets it to <see langword="null"/> when the element left the roots
    /// (Decision 10). A layout change under a still pointer waits for the next pointer event.
    /// </remarks>
    internal Element? PointerHit => _pointerHit;

    /// <summary>
    /// Gets the position of the last pointer entry with no target, in viewport pixels. The value is (0, 0) before the
    /// first such entry. A cursor leave gives (NaN, NaN).
    /// </summary>
    /// <remarks>The value follows the pointer events only. The tests of <c>BrutalUi.Core.Tests</c> read this member.</remarks>
    internal float2 PointerPosition => _pointerPosition;

    /// <summary>
    /// Gets a value that tells if a pointer entry with no target went through a drain. Then
    /// <see cref="PointerPosition"/> holds its position.
    /// </summary>
    /// <remarks>The value follows the pointer events only. The position can be NaN after a cursor leave.</remarks>
    internal bool HasPointerPosition => _hasPointerPosition;

    /// <summary>
    /// Gets the element with <see cref="PseudoStates.Active"/> from this context, or <see langword="null"/> when a
    /// press set no state or the last button went up.
    /// </summary>
    /// <remarks>
    /// Only the tests of <c>BrutalUi.Core.Tests</c> read this member. The value changes only during a drain.
    /// A <see cref="UiEventType.PointerUp"/> handler of the last button reads the pressed element here, because the
    /// drain clears the value after the walk of the release (Decision 2 of button-and-toggle). This amends Decision 9
    /// of pointer-routing for the release only.
    /// <c>Active</c> marks the element of the first press of a gesture, with each button. It is not the click target.
    /// The press target (<see cref="CurrentPressTarget"/>) holds the element of a left press with no chord only
    /// (Decision 13 of slider). This amends Decision 2 of button-and-toggle: a widget reads the press target, not
    /// <c>Active</c>, to find a click or a drag.
    /// The drag capture (Decision 3 of slider) reads the same element of the left press with no chord. While that
    /// element implements <see cref="IDragCapture"/> and captures its moves, each move with no target walks to it. The
    /// capture thus ends when <c>Active</c> clears. This amends Decision 8 of pointer-routing for such a move only.
    /// Limit: the ring drops its oldest entry when it is full. A dropped <c>PointerUp</c> leaves the button bit set, so
    /// <c>Active</c> stays until the next press and release of that button. Call <see cref="Update(float)"/> each frame to keep the ring below its capacity.
    /// </remarks>
    internal Element? ActiveElement => _activeElement;

    /// <summary>
    /// Gets the element of the left press with no chord that set <see cref="PseudoStates.Active"/>, or
    /// <see langword="null"/> (Decision 13 of slider).
    /// </summary>
    /// <remarks>
    /// The value is <see cref="_activeElement"/> when <see cref="_activeButton"/> is <see cref="PointerButton.Left"/>
    /// and <see cref="_activeChord"/> is <see langword="false"/>. Else it is <see langword="null"/>.
    /// </remarks>
    private Element? LeftPressElement =>
        _activeElement is not null && _activeButton == PointerButton.Left && !_activeChord ? _activeElement : null;

    /// <summary>
    /// Gets the press target of the walk that runs on this thread: the element of the left press with no chord that
    /// the current pointer gesture holds, or <see langword="null"/> (Decision 13 of slider).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The drain sets the value for the walk of each entry that it routes. A <see cref="UiEventType.PointerDown"/> and
    /// a <see cref="UiEventType.PointerMove"/> get the element of the left press with no chord. A
    /// <see cref="UiEventType.PointerUp"/> gets that element only when the release ends the press (no button stays
    /// down). Each other entry gets <see langword="null"/>: an entry with a target from the caller, a scroll, a key,
    /// and a char.
    /// </para>
    /// <para>
    /// For the walk of a release, the press target is the click target. <see cref="WidgetInput.IsClick"/> compares
    /// the widget with this value, and the drag checks of the later widgets read the same value.
    /// </para>
    /// <para>
    /// The value is <see langword="null"/> outside a walk. It is thread-static and lives for one walk only, so a
    /// nested drain of another context does not change it, and two contexts on two threads do not share it. It
    /// allocates nothing.
    /// </para>
    /// </remarks>
    internal static Element? CurrentPressTarget => t_pressTarget;

    /// <summary>
    /// Gets the element that holds the keyboard focus of this context, or <see langword="null"/> when no element
    /// holds it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="RoutingGate"/> reads this member (A6). The owner holds <see cref="PseudoStates.Focus"/> from this
    /// context. The state goes on the owner only, not on its ancestors.
    /// </para>
    /// <para>
    /// The value changes at a code call (<see cref="SetFocus(Element)"/>, <see cref="ClearFocus"/>,
    /// <see cref="FocusNext"/>, or <see cref="FocusPrevious"/>) or during a drain. A key or char event with no target
    /// walks to this element at the drain. A Tab key down with no target moves the value after its walk.
    /// </para>
    /// <para>
    /// The click focus: a <see cref="UiEventType.PointerDown"/> with no target sets this value at the drain, before
    /// its walk. The value becomes the hit element or its nearest ancestor with <see cref="Element.IsFocusable"/>. A
    /// press on no such element, or over no element, sets it to <see langword="null"/>. A press with a target from the
    /// caller does not change it.
    /// </para>
    /// <para>
    /// The stale focus rule: at the start of each drain, the value becomes <see langword="null"/> when the owner is not
    /// focusable, holds <see cref="PseudoStates.Disabled"/>, left the roots of this context, or is hidden
    /// (<see cref="Display.None"/> or an opacity product of 0 or less on the owner or an ancestor). The style of the
    /// last <see cref="Update(float)"/> decides. Between two drains, the value can hold an owner that turned stale.
    /// A handler that disables the owner or clears <see cref="Element.IsFocusable"/> drops the owner at the next key
    /// or char entry of the same drain.
    /// </para>
    /// <para>
    /// The repair rule: code can clear <see cref="PseudoStates.Focus"/> on the owner through
    /// <see cref="Element.States"/>. The owner then stays in this value. A drain and a
    /// <see cref="SetFocus(Element)"/> call on the owner set the bit again. The repair touches the owner only. It marks
    /// nothing when the bit is set already.
    /// </para>
    /// </remarks>
    public Element? FocusedElement => _focusOwner;

    /// <summary>
    /// Gets the routing gate: the two values that tell the game when the UI owns the pointer or the keyboard. Read it
    /// after <see cref="Update(float)"/> and before the game handles its own input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property reads the current state at each read and gives a new copy (Decision 3).
    /// <see cref="UiRoutingGate.PointerOverUi"/> comes from the hit of the last pointer or scroll entry with no target,
    /// the element with <see cref="PseudoStates.Hover"/>. <see cref="UiRoutingGate.KeyboardFocusInUi"/> comes from
    /// <see cref="FocusedElement"/>. The value changes during
    /// a drain, at <see cref="SetFocus(Element)"/>, <see cref="ClearFocus"/>, <see cref="FocusNext"/>, and
    /// <see cref="FocusPrevious"/>, and at the stale rules of the next <see cref="Update(float)"/>.
    /// </para>
    /// <para>
    /// The pointer value follows the pointer events only. A layout change under a still pointer, for example a panel
    /// that closes, keeps the value until the next pointer event or the next stale rule. A press, a release, a move,
    /// and a scroll each hit-test again before the game reads the gate. An entry with a target from the caller
    /// changes no value.
    /// </para>
    /// <para>
    /// The read allocates nothing. <see cref="GameInput"/> applies this value at the end of the drain (A4 of
    /// input-state).
    /// </para>
    /// </remarks>
    public UiRoutingGate RoutingGate => new(_pointerHit is not null, _focusOwner is not null);

    /// <summary>
    /// Gets the raw input state of the last <see cref="Update(float)"/> call: the values that the drain recorded,
    /// before the routing gate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The frame rule: the drain writes the state during <see cref="Update(float)"/>, entry by entry, and writes
    /// the delta at the end of the drain. Read this property after the call, when the frame values are complete.
    /// </para>
    /// <para>
    /// Q2 of the input epic: the value is the state after the drain of the last <see cref="Update(float)"/> call. It
    /// is fixed until the next call, so two reads in one frame agree.
    /// </para>
    /// <para>The read copies nothing and allocates nothing.</para>
    /// </remarks>
    public ref readonly UiInputState Input => ref _input.Raw;

    /// <summary>
    /// Gets the game view of the input state: the copy of <see cref="Input"/> that the routing gate filters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate rule (A4): the end of the drain applies the gate one time, from the value that
    /// <see cref="RoutingGate"/> gives at that point. <see cref="RoutingGate"/> gives the same value after
    /// <see cref="Update(float)"/>. A focus change after <see cref="Update(float)"/> changes
    /// <see cref="RoutingGate"/> and not this view until the next <see cref="Update(float)"/> call.
    /// </para>
    /// <para>
    /// The pointer part is empty when <see cref="UiRoutingGate.PointerOverUi"/> was <see langword="true"/> at the
    /// end of the last drain: the position holds (NaN, NaN), the delta is (0, 0), the scroll is (0, 0), and the
    /// button masks are zero. The game then sees no pointer input from the UI. The key part is empty when
    /// <see cref="UiRoutingGate.KeyboardFocusInUi"/> was <see langword="true"/> at that point: no key bits and the
    /// <see cref="UiModifiers.None"/> modifiers. The gamepad slots get no gate.
    /// </para>
    /// <para>The read copies nothing and allocates nothing.</para>
    /// </remarks>
    public ref readonly UiInputState GameInput => ref _input.Game;

    /// <summary>
    /// Releases each key that <see cref="Input"/> holds down, at the end of the next drain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Q2 of the input epic: the call sets a request only. The end of the next drain applies it, after the last
    /// entry, so a key down of the same frame that came before the call also clears (Decision 10). A key down after
    /// the call in the same frame also clears.
    /// </para>
    /// <para>
    /// Each cleared key gets the released flag for that frame, and the modifiers become
    /// <see cref="UiModifiers.None"/>. The clear runs before the game view is written, so both views agree.
    /// </para>
    /// <para>
    /// The call does not touch the elements: an element that got a key down gets no key up.
    /// <c>GlfwUiInput</c> calls it on a focus loss and on a <c>KeyboardEnabled</c> switch to
    /// <see langword="false"/>.
    /// </para>
    /// <para>The call allocates nothing.</para>
    /// </remarks>
    public void ClearHeldKeys() => _input.RequestKeyClear();

    /// <summary>
    /// Writes one gamepad slot of <see cref="Input"/> and computes its pressed and released edges from the old
    /// buttons of the slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the write seam of the gamepad-axes feature (the input epic, Notes question 1). Call it one time for
    /// each slot in each frame, after <c>Glfw.PollEvents</c> and before <see cref="Update(float)"/>. A second call
    /// for the same slot in one frame replaces the edges of the first call.
    /// </para>
    /// <para>
    /// The method computes <c>Pressed = new &amp; ~old</c> and <c>Released = old &amp; ~new</c>, over the buttons
    /// of the old raw slot and of <paramref name="state"/>. It writes the raw slot at once. The drain keeps the
    /// gamepad edges, and the end of the next drain copies the slots to the game view.
    /// </para>
    /// <para>
    /// The gate does not apply to a gamepad: <see cref="GameInput"/> holds the same slots as <see cref="Input"/>,
    /// also when the pointer is over the UI or the UI holds the keyboard focus.
    /// </para>
    /// <para>The call allocates nothing.</para>
    /// </remarks>
    /// <param name="index">The slot index, 0 to 15.</param>
    /// <param name="state">
    /// The new state of the slot. Build it with <see cref="UiGamepadState(UiGamepadButtons, ReadOnlySpan{float})"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is outside 0 to 15. The parameter name is <c>index</c>.
    /// </exception>
    public void SetGamepad(int index, in UiGamepadState state)
    {
        if ((uint)index >= (uint)UiInputState.GamepadCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _input.SetGamepad(index, in state);
    }

    /// <summary>
    /// Moves the keyboard focus of this context to an element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call returns <see langword="false"/> when the element does not accept the focus
    /// (<see cref="Element.IsFocusable"/> is <see langword="false"/>, or the element holds
    /// <see cref="PseudoStates.Disabled"/>), when the top of its parent chain is not a root of this context, or when
    /// the element is hidden. Hidden means <see cref="Display.None"/> or an opacity product of 0 or less on the element
    /// or an ancestor. The style of the last <see cref="Update(float)"/> decides, as for the hit test. A style change
    /// that shows or hides the element counts for this call after the next update.
    /// </para>
    /// <para>
    /// A failed call for another element changes nothing. A failed call for the current owner clears the focus, so
    /// no element holds it after the call. A call for the current owner that passes the checks returns
    /// <see langword="true"/>. It sets <see cref="PseudoStates.Focus"/> again when code cleared the bit through
    /// <see cref="Element.States"/>. It marks nothing when the bit is set already.
    /// </para>
    /// <para>
    /// The call changes the owner and <see cref="PseudoStates.Focus"/> at once, with no queue (Decision 12). This is
    /// also true outside a drain and from an event handler. A key or char event of the current drain that did not walk
    /// yet goes to the new owner. The next <see cref="Update(float)"/> restyles the subtrees of the old and the new
    /// owner. The call allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="element">The element that gets the focus.</param>
    /// <returns>
    /// <see langword="true"/> only when the element holds the focus after the call. On <see langword="false"/>, the
    /// element does not hold the focus.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public bool SetFocus(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!element.AcceptsFocus || !IsInRootTree(element) || !FocusWalk.IsShown(element))
        {
            // A failed call on the owner clears the focus, so the result and the state agree (CR-4).
            if (ReferenceEquals(element, _focusOwner))
            {
                SetFocusOwner(null);
            }

            return false;
        }

        SetFocusOwner(element);
        return true;
    }

    /// <summary>
    /// Removes the keyboard focus from the owner of this context. After the call, no element holds it.
    /// </summary>
    /// <remarks>
    /// The call clears <see cref="PseudoStates.Focus"/> on the old owner at once, also outside a drain and from an
    /// event handler (Decision 12). The next <see cref="Update(float)"/> restyles the subtree of the old owner. A call
    /// with no owner changes nothing. A key or char event with no target then gets no walk.
    /// </remarks>
    public void ClearFocus()
    {
        SetFocusOwner(null);
    }

    /// <summary>
    /// Moves the keyboard focus of this context to the next element that accepts the focus, in tree order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is the Tab move. It is also the slot for gamepad navigation: an action map can bind a button or a
    /// key to it (Decision 8 of keyboard-focus). This feature binds no gamepad button and no arrow key.
    /// </para>
    /// <para>
    /// The order is the paint order: the roots in the order of add, and a parent before its children. The search
    /// skips an element with <see cref="Display.None"/> and an element with an opacity product of 0 or less, and
    /// their subtrees. It reads no clip, so a clipped element can take the focus. After the last element, the focus
    /// wraps to the first element. With no owner, or with an owner that the search does not see, the focus goes to
    /// the first element. When the owner is the only element that accepts the focus, the owner keeps it. With no
    /// element that accepts the focus, the owner clears.
    /// </para>
    /// <para>
    /// The call changes the owner and <see cref="PseudoStates.Focus"/> at once, with no queue (Decision 12). This is
    /// also true outside a drain and from an event handler. The call allocates nothing after warm-up. A tree deeper
    /// than the frame stack grows the stack one time.
    /// </para>
    /// </remarks>
    public void FocusNext()
    {
        SetFocusOwner(FocusWalk.Find(_roots.AsSpan(), _focusOwner, false, ref _focusStack));
    }

    /// <summary>
    /// Moves the keyboard focus of this context to the previous element that accepts the focus, in tree order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is the Shift+Tab move. It is also the slot for gamepad navigation: an action map can bind a button
    /// or a key to it (Decision 8 of keyboard-focus). This feature binds no gamepad button and no arrow key.
    /// </para>
    /// <para>
    /// The rules of <see cref="FocusNext"/> apply in the reverse direction. Before the first element, the focus wraps
    /// to the last element. With no owner, or with an owner that the search does not see, the focus goes to the last
    /// element. With no element that accepts the focus, the owner clears.
    /// </para>
    /// </remarks>
    public void FocusPrevious()
    {
        SetFocusOwner(FocusWalk.Find(_roots.AsSpan(), _focusOwner, true, ref _focusStack));
    }

    /// <summary>
    /// Finds the top-most element whose border box holds a position: the last root first, the last child first, and a
    /// descendant before its ancestor.
    /// </summary>
    /// <remarks>
    /// The hit test reads <see cref="LayoutNode.Rect"/> of the last <see cref="Update(float)"/>. An element that
    /// arrives after that update has no layout yet. The test is half-open, so the right and the bottom edge are
    /// outside. A NaN component hits nothing. The method changes no state and allocates only when a deep tree grows
    /// the frame stack.
    /// </remarks>
    /// <param name="position">The position in viewport pixels.</param>
    /// <returns>The hit element, or <see langword="null"/> when no element holds the position.</returns>
    internal Element? HitTest(float2 position)
    {
        return HitWalk.Find(_roots.AsSpan(), position, ref _hitStack);
    }

    /// <summary>
    /// Gets the number of events that a push into a full ring dropped. The ring drops its oldest event in this case.
    /// </summary>
    public long DroppedEventCount => _events.Dropped;

    /// <summary>
    /// Adds a pointer event at the end of the event ring. The input pass of the next <see cref="Update(float)"/> sends
    /// it along the path of its target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The push only copies the event. The ring keeps the type, the timestamp, the target, and the pointer fields.
    /// It does not keep
    /// <see cref="UiEventHeader.Phase"/> or <see cref="UiEventHeader.CurrentTarget"/>: the walk writes them.
    /// </para>
    /// <para>
    /// A pointer event with no <see cref="UiEventHeader.Target"/> gets the hit element as its target at the drain,
    /// not at the push. The hit test reads the layout of the last <see cref="Update(float)"/>. The drain also moves
    /// <see cref="PseudoStates.Hover"/> to the hit element and stores the hit. A pointer event over no element gets no
    /// walk, but it clears <see cref="PseudoStates.Hover"/>.
    /// </para>
    /// <para>
    /// A <see cref="UiEventType.PointerDown"/> with no target also moves the keyboard focus at the drain, before its
    /// walk: to the hit element, or to its nearest ancestor with <see cref="Element.IsFocusable"/>. A press on no such
    /// element, or over no element, clears the focus (Decision 4 of keyboard-focus). Each button runs this rule. A
    /// press over no element then goes to <see cref="OnUnhandledInput"/>, after the focus clears.
    /// </para>
    /// <para>
    /// A target from the caller skips the hit test, the states, the click focus, and the store of the hit
    /// (Decision 2). The hit test applies to the pointer types and the scroll only. A key or char event with no target
    /// goes to the focus owner.
    /// </para>
    /// <para>
    /// A full ring drops its oldest event. The push allocates nothing, because the constructor creates the ring.
    /// </para>
    /// </remarks>
    /// <param name="e">The event. The call copies it.</param>
    /// <exception cref="ArgumentException">The type of the event is not a pointer type.</exception>
    public void PushEvent(in PointerEvent e)
    {
        Enqueue(in e);
    }

    /// <summary>
    /// Adds a scroll event at the end of the event ring. The input pass of the next <see cref="Update(float)"/> sends
    /// it along the path of its target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rules of <see cref="PushEvent(in PointerEvent)"/> apply. The ring keeps the type, the timestamp, the
    /// target, the position, the delta, and the modifiers.
    /// </para>
    /// <para>
    /// The scroll rule: a scroll with no <see cref="UiEventHeader.Target"/> gets the element under its position as
    /// its target at the drain. It goes through the same hit test as a pointer event, so it also moves
    /// <see cref="PseudoStates.Hover"/> to the hit element and stores the hit. It changes
    /// <see cref="PseudoStates.Active"/> never. A scroll over no element gets no walk. A scroll with a target from the
    /// caller skips the hit test and the states.
    /// </para>
    /// </remarks>
    /// <param name="e">The event. The call copies it.</param>
    /// <exception cref="ArgumentException">The type of the event is not <see cref="UiEventType.Scroll"/>.</exception>
    public void PushEvent(in ScrollEvent e)
    {
        Enqueue(in e);
    }

    /// <summary>
    /// Adds a key event at the end of the event ring. The input pass of the next <see cref="Update(float)"/> sends it
    /// along the path of its target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The push only copies the event. The ring keeps the type, the timestamp, the target, the key, the scan code, the
    /// modifiers, and the repeat flag. A full ring drops its oldest event. The push allocates nothing.
    /// </para>
    /// <para>
    /// The focus rule: a key event with no <see cref="UiEventHeader.Target"/> goes to the focus owner
    /// (<see cref="FocusedElement"/>) at the drain, not at the push. With no owner, it gets no walk. A target from the
    /// caller keeps its target and changes no focus (Decision 3 of keyboard-focus).
    /// </para>
    /// <para>
    /// The Tab rule: a <see cref="UiEventType.KeyDown"/> of the Tab key with no target moves the focus after its walk
    /// (Decision 5 of keyboard-focus). Tab calls <see cref="FocusNext"/>, and Shift+Tab calls
    /// <see cref="FocusPrevious"/>. A handler that stops the walk keeps the focus. A handler that calls
    /// <see cref="SetFocus(Element)"/> during the walk must also stop the walk to keep its choice. A Tab with no owner
    /// still moves the focus. A repeat moves the focus, and a key up does not. A Tab with Control, Alt, or Super down,
    /// or with a target from the caller, does not move the focus. A key down with no target that no handler stopped
    /// and that is not a Tab move goes to <see cref="OnUnhandledInput"/> after its walk.
    /// </para>
    /// </remarks>
    /// <param name="e">The event. The call copies it.</param>
    /// <exception cref="ArgumentException">The type of the event is not a key type.</exception>
    public void PushEvent(in KeyEvent e)
    {
        Enqueue(in e);
    }

    /// <summary>
    /// Adds a char event at the end of the event ring. The input pass of the next <see cref="Update(float)"/> sends it
    /// along the path of its target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The push only copies the event. The ring keeps the type, the timestamp, the target, and the code point. A full
    /// ring drops its oldest event. The push allocates nothing.
    /// </para>
    /// <para>
    /// The focus rule: a char event with no <see cref="UiEventHeader.Target"/> goes to the focus owner
    /// (<see cref="FocusedElement"/>) at the drain, not at the push. With no owner, it gets no walk. A target from the
    /// caller keeps its target and changes no focus (Decision 3 of keyboard-focus).
    /// </para>
    /// </remarks>
    /// <param name="e">The event. The call copies it.</param>
    /// <exception cref="ArgumentException">The type of the event is not <see cref="UiEventType.Char"/>.</exception>
    public void PushEvent(in CharEvent e)
    {
        Enqueue(in e);
    }

    /// <summary>
    /// Gets or sets the handler for a file drop on the window. <see cref="RaiseFileDrop(ReadOnlySpan{string})"/>
    /// calls it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A4: a window event gets no per-element routing. The slot is on the context, not on an element. A null slot
    /// ignores the drop.
    /// </para>
    /// <para>
    /// Set the slot at build time, not in the frame loop. <c>+=</c> combines delegates in the order of add.
    /// </para>
    /// </remarks>
    public UiFileDropHandler? OnFileDrop { get; set; }

    /// <summary>
    /// Gets or sets the handler for a change of the content scale of the window.
    /// <see cref="RaiseContentScale(float2)"/> calls it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A4: a window event gets no per-element routing. The slot is on the context, not on an element. A null slot
    /// ignores the change.
    /// </para>
    /// <para>
    /// The context does not apply the scale. The handler applies it. Set the slot at build time, not in the frame
    /// loop.
    /// </para>
    /// </remarks>
    public UiContentScaleHandler? OnContentScale { get; set; }

    /// <summary>
    /// Gets or sets the handler for an input that no element took. The drain of the input pass calls it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A6 of state-and-commands: the slot is the seam of the action map. The map lives in a higher layer, and Core
    /// names no type of that layer. <c>UiActionMap.Activate</c> writes the slot. The context holds one slot, so one
    /// handler is active at a time. A null slot does nothing.
    /// </para>
    /// <para>
    /// The drain calls the slot for a <see cref="UiEventType.KeyDown"/> with no target from the caller, when no
    /// handler stopped its walk and the key is not a Tab move (the unhandled input rule of <see cref="DrainEvents"/>).
    /// It also calls the slot for a <see cref="UiEventType.PointerDown"/> with no target from the caller that hit no
    /// element.
    /// </para>
    /// <para>
    /// Set the slot at build time, not in the frame loop. A write replaces the delegate in the slot. Do not use
    /// <c>+=</c>: the action map reads the slot to know if it is active.
    /// </para>
    /// </remarks>
    public UiUnhandledInputHandler? OnUnhandledInput { get; set; }

    /// <summary>
    /// Gets or sets the handler that <see cref="Update(float)"/> calls first, before the input pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Q1 and A2 of panels-and-modes: the slot is the seam of the mode diff of the framework layer. The manager
    /// lives in that layer, and Core names no type of it. <c>UiManager</c> writes the slot in its constructor. A
    /// write with <c>=</c> replaces the delegate. Do not use <c>+=</c>. A null slot does nothing.
    /// </para>
    /// <para>
    /// Set the slot at build time, not in the frame loop. Create the delegate one time.
    /// <see cref="UiBeforeUpdateHandler"/> states the call rules.
    /// </para>
    /// </remarks>
    public UiBeforeUpdateHandler? OnBeforeUpdate { get; set; }

    /// <summary>
    /// Calls <see cref="OnFileDrop"/> at once with the paths of a file drop. A null slot makes the call do nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call the method on the main thread. The input adapter calls it. A test or another platform layer can call it
    /// too.
    /// </para>
    /// <para>
    /// The method queues no entry, walks no element, and changes no state of the context (Decision 11). The span is
    /// valid during the call only. An exception from the handler goes to the caller.
    /// </para>
    /// <para>The method allocates nothing. The handler can allocate.</para>
    /// </remarks>
    /// <param name="paths">The paths of the dropped files. An empty span still calls the slot.</param>
    public void RaiseFileDrop(ReadOnlySpan<string> paths)
    {
        OnFileDrop?.Invoke(paths);
    }

    /// <summary>
    /// Calls <see cref="OnContentScale"/> at once with the new content scale. A null slot makes the call do nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call the method on the main thread. The input adapter calls it. A test or another platform layer can call it
    /// too.
    /// </para>
    /// <para>
    /// The method queues no entry, walks no element, and changes no state of the context (Decision 11). It does not
    /// check the value. It does not change <see cref="ViewportSize"/>. An exception from the handler goes to the
    /// caller.
    /// </para>
    /// </remarks>
    /// <param name="scale">The new content scale of the window.</param>
    public void RaiseContentScale(float2 scale)
    {
        OnContentScale?.Invoke(scale);
    }

    /// <summary>
    /// Pops each event that the ring holds at the start of the call, and sends each event with a target along the
    /// path of its target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A5: <see cref="Update(float)"/> calls this method first. The tests also call it directly.
    /// </para>
    /// <para>
    /// The method reads the count one time at the start (Decision 6). It pops each entry into a local before the walk,
    /// so the free slot of the ring holds no element reference during the walk. An event that a handler pushes waits for the next
    /// drain. An exception from a handler stops the drain. The entries that the drain did not pop stay in the ring.
    /// </para>
    /// <para>
    /// A handler can push into a full ring. The push drops the oldest entry, and this entry can be an entry of the
    /// current drain that did not walk yet. The method reduces the count of waiting entries by each drop that a walk
    /// causes. The drain then never pops an event that a handler pushed. A skipped entry runs no handler, so it causes no drop.
    /// </para>
    /// <para>
    /// A pointer entry or a scroll entry with no target gets the hit element as its target in the target step,
    /// before the walk (Decision 1 of pointer-routing). The hit test reads the layout of the last
    /// <see cref="Update(float)"/>. The step moves <see cref="PseudoStates.Hover"/> to the hit element and stores the
    /// hit before the walk runs, so a handler reads the new state. An entry over no element clears
    /// <see cref="PseudoStates.Hover"/> and gets no walk. An entry with a target from the caller skips the hit test
    /// and changes no state and no focus.
    /// </para>
    /// <para>
    /// The focus rule (Decision 3 of keyboard-focus): a <see cref="UiEventType.KeyDown"/>,
    /// <see cref="UiEventType.KeyUp"/>, or <see cref="UiEventType.Char"/> entry with no target gets the focus owner
    /// (<see cref="FocusedElement"/>) as its target in the target step. With no owner, it gets no walk. An entry with
    /// a target from the caller keeps its target and changes no focus. A handler that moves the focus changes the
    /// target of each later key or char entry of the same drain.
    /// </para>
    /// <para>
    /// The Active rule: a <see cref="UiEventType.PointerDown"/> sets <see cref="PseudoStates.Active"/> on its hit
    /// element when no element holds the state. The context counts the buttons that are down. The
    /// <see cref="UiEventType.PointerUp"/> of the last button clears the state on the element that got it. The release
    /// walks to the element under the pointer, which can be another element. A move and a scroll leave the state.
    /// </para>
    /// <para>
    /// The move rule (Decision 3 of slider): a <see cref="UiEventType.PointerMove"/> with no target walks to the hit
    /// element. During a captured drag, it walks to the capture element: the element of the left press with no chord
    /// (Decision 13 of slider) that implements <see cref="IDragCapture"/> with <see cref="IDragCapture.CapturesMoves"/>
    /// set, when that element is in the roots of this context and is shown. Shown means no <see cref="Display.None"/>
    /// and no opacity product of 0 or less on the element or an ancestor, as for the focus. A removed or hidden capture
    /// element gets no captured move: the move walks to the hit. The move then reaches the capture element also
    /// outside its rect. The hit test, <see cref="PseudoStates.Hover"/>, <see cref="PointerHit"/>, and the routing
    /// gate still follow the hit. This amends Decision 8 of pointer-routing for a move during a captured drag only.
    /// The release of the last button ends the capture.
    /// </para>
    /// <para>
    /// The release order (Decision 2 of button-and-toggle): the release clears its button bit before the walk, but
    /// it clears <see cref="PseudoStates.Active"/> after its walk. Thus a <see cref="UiEventType.PointerUp"/> handler
    /// reads <see cref="PseudoStates.Active"/> on the pressed element. This amends Decision 9 of pointer-routing ("The
    /// state changes before the walk") for the release only. A release over no element gets no walk, and the state
    /// still clears after the walk step. The <c>finally</c> block clears the state when a handler throws. After the
    /// drain, the states are the same as before the amendment.
    /// </para>
    /// <para>
    /// The press target (Decision 13 of slider): after the target step of each entry, the method sets
    /// <see cref="CurrentPressTarget"/> for the walk of that entry. A <see cref="UiEventType.PointerDown"/> and a
    /// <see cref="UiEventType.PointerMove"/> that the drain routes get the element of the left press with no chord. A
    /// routed <see cref="UiEventType.PointerUp"/> gets that element only when the release ends the press. Each other
    /// entry gets <see langword="null"/>. The method saves the old value at its start, and it writes the old value
    /// back after each walk step and in its <c>finally</c> block. Thus a chord gives no click, and a release with a
    /// target from the caller gives no click. <see cref="PseudoStates.Active"/> keeps its life. This amends
    /// Decision 2 of button-and-toggle: a widget reads the press target, not <see cref="PseudoStates.Active"/>.
    /// </para>
    /// <para>
    /// The click focus rule (Decision 4 of keyboard-focus): a <see cref="UiEventType.PointerDown"/> with no target
    /// moves the focus after the Active step and before the walk. The focus goes to the hit element, or to its nearest
    /// ancestor with <see cref="Element.IsFocusable"/>. When that element does not accept the focus, or when the press
    /// hit no such element or no element at all, the focus clears. Each button runs the rule. A move, a release, and a
    /// scroll keep the focus. A press with a target from the caller keeps the focus. A press moves the target of each
    /// later key or char entry of the same drain.
    /// </para>
    /// <para>
    /// The Tab rule (Decision 5 of keyboard-focus): a <see cref="UiEventType.KeyDown"/> entry with no target and the
    /// key <c>TabKey</c> (the GLFW Tab key) first walks to the focus owner. After the walk, the method calls
    /// <see cref="FocusPrevious"/> when <see cref="UiModifiers.Shift"/> is down and <see cref="FocusNext"/> otherwise.
    /// A handler that stops the walk keeps the focus: the move does not occur. A handler that calls
    /// <see cref="SetFocus(Element)"/> during the walk must also stop the walk to keep its choice. Otherwise the move
    /// starts from the new owner. A Tab with no owner gets no walk, but it still moves the focus. A repeat moves the
    /// focus. A <see cref="UiEventType.KeyUp"/> does not. A Tab with <see cref="UiModifiers.Control"/>,
    /// <see cref="UiModifiers.Alt"/>, or <see cref="UiModifiers.Super"/> down does not move the focus. A Tab with a
    /// target from the caller walks to that target and never moves the focus.
    /// </para>
    /// <para>
    /// The unhandled input rule (A6 of state-and-commands): after the Tab move, the method reads
    /// <see cref="OnUnhandledInput"/> one time for the entry. It calls the slot for a
    /// <see cref="UiEventType.KeyDown"/> entry with no target from the caller, when no handler stopped the walk and the
    /// entry is not a Tab move. The call gets the key code, the modifiers, and the repeat flag. A key down with no
    /// focus owner gets no walk, and it reaches the slot. The call runs at the position of the entry in the ring, after
    /// the walk, after the end of a release, and after the Tab move. Thus the slot reads the focus of that point. A
    /// Control+Tab is not a Tab move, so it reaches the slot. The method also calls the slot for a
    /// <see cref="UiEventType.PointerDown"/> entry with no target from the caller when the press hit no element. The
    /// call gets the button as its GLFW value, the modifiers, and <see langword="false"/> for the repeat flag. A press
    /// on an element never reaches the slot, also when no handler stops it: a hit takes a press, as a stopped walk
    /// takes a key. The click focus rule runs in the target step, so the press clears the focus before the call. A key
    /// up, a char, a pointer move, a release, a scroll, and an entry with a target from the caller never reach the
    /// slot. An exception of the slot stops the drain, as an exception of a handler does. A null slot skips the step.
    /// </para>
    /// <para>
    /// The method skips the walk of an entry with no target after the target step (A4). It skips an entry when the top of the
    /// parent chain of the target is not a root of this context (Decision 7): the target left the tree, or it belongs
    /// to another context.
    /// It also skips an entry with a type that no event struct carries. <see cref="PushEvent(in PointerEvent)"/> and
    /// the other push methods reject such a type, so the skip is a guard only.
    /// </para>
    /// <para>
    /// The stale rule (Decision 10): at the start of each drain, the method clears <see cref="PseudoStates.Hover"/> and
    /// <see cref="PseudoStates.Active"/> on a stored element that left the roots, and drops the reference. It keeps
    /// the button mask, so a later release still clears its bit.
    /// </para>
    /// <para>
    /// The stale focus rule (Decision 7 of keyboard-focus): right after the stale rule, the method clears the focus
    /// when the owner does not accept the focus (it is not focusable, or it holds <see cref="PseudoStates.Disabled"/>),
    /// is not in the roots of this context, or is hidden. Hidden means <see cref="Display.None"/> or an opacity product
    /// of 0 or less on the owner or an ancestor. The style of the last <see cref="Update(float)"/> decides, as for the
    /// hit test. After the drain starts, <see cref="FocusedElement"/> is <see langword="null"/> or an element that can
    /// hold the focus. When the owner stays, the method sets <see cref="PseudoStates.Focus"/> on it again, in case
    /// code cleared the bit through <see cref="Element.States"/>.
    /// </para>
    /// <para>
    /// Limit of the stale focus rule during a drain: a handler that disables the owner or clears
    /// <see cref="Element.IsFocusable"/> drops the owner at the next key or char entry of the same drain. That entry
    /// gets no walk. A removed owner gets no walk, because the walk step checks the roots. A style change that hides
    /// the owner takes effect after the next <see cref="Update(float)"/>.
    /// </para>
    /// <para>
    /// The states follow the pointer events only. A layout change under a still pointer, or a change that hides an
    /// element, makes it transparent, or makes it unpickable, leaves the states until the next pointer event. An entry
    /// with a target from the caller changes no state.
    /// </para>
    /// <para>
    /// Limit of the ring: the ring drops its oldest entry when it is full. A dropped <c>PointerUp</c> leaves the button
    /// bit set, so <see cref="PseudoStates.Active"/> stays until the next press and release of that button. Call
    /// <see cref="Update(float)"/> each frame to keep the ring below its capacity. <see cref="DroppedEventCount"/> shows a drop.
    /// </para>
    /// <para>
    /// Limit of the stale rules: the check of one context can clear a state that another context set. This is true
    /// for <see cref="PseudoStates.Hover"/>, <see cref="PseudoStates.Active"/>, and <see cref="PseudoStates.Focus"/>.
    /// Call <see cref="Update(float)"/> on the old context after you remove an element and before you add it to another context.
    /// </para>
    /// <para>
    /// A call from a handler of the drain throws. The flag is cleared in a <c>finally</c> block, so the context stays
    /// valid after an exception from a handler.
    /// </para>
    /// <para>
    /// The state write (input-state). Three hooks wrap the loop: the first hook clears the frame values of the input
    /// state and keeps the start position, the second hook writes each entry into the raw state after the target
    /// step, and the third hook writes the delta and the game view in the <c>finally</c> block. The third hook reads
    /// the value that <see cref="RoutingGate"/> gives at that point. An exception of a handler still runs the third
    /// hook, so the state holds each entry up to the throw. Read the result through <see cref="Input"/> or
    /// <see cref="GameInput"/> after <see cref="Update(float)"/>.
    /// </para>
    /// </remarks>
    /// <returns>The number of walks: the entries that the method sent to the walk.</returns>
    /// <exception cref="InvalidOperationException">A drain of this context runs already.</exception>
    internal int DrainEvents()
    {
        if (_draining)
        {
            throw new InvalidOperationException("A drain of this UiContext runs already. Do not call Update or the drain from an event handler.");
        }

        // The press target of an outer walk (Decision 13 of slider). The drain writes it back after each walk step and
        // in the finally block, as Update does with t_measurer.
        Element? previousPressTarget = t_pressTarget;

        _draining = true;
        try
        {
            // The stale rule (Decision 10): a stored element that left its roots loses its state before any entry runs.
            DropStalePointerState();

            // The stale focus rule (Decision 7 of keyboard-focus): an owner that cannot take the focus loses it here.
            DropStaleFocus();

            // Hook 1 of input-state: clear the frame values of the input state and keep the start position.
            _input.BeginFrame();

            // Read the count one time. An event that a handler pushes waits for the next drain (Decision 6).
            int remaining = _events.Count;
            int walks = 0;

            while (remaining > 0 && _events.TryPop(out UiQueuedEvent entry))
            {
                remaining--;

                // The target step. A pointer entry with no target gets the hit element here, before the walk
                // (Decision 1 of pointer-routing). A key or char entry with no target gets the focus owner here
                // (Decision 3 of keyboard-focus). A target from the caller keeps its target and changes no focus. An
                // entry with no target after this step, or with a target outside the roots of this context, gets no walk.
                Element? target = entry.Header.Target;

                // True when the drain gives the target. Only such an entry can move the focus after its walk.
                bool routed = target is null;
                if (target is null
                    && entry.Header.Type is UiEventType.PointerMove or UiEventType.PointerDown or UiEventType.PointerUp
                        or UiEventType.Scroll)
                {
                    target = RoutePointer(ref entry);
                }
                else if (target is null
                    && entry.Header.Type is UiEventType.KeyDown or UiEventType.KeyUp or UiEventType.Char)
                {
                    // A handler of an earlier entry can disable the owner or clear IsFocusable. Drop such an owner
                    // before the entry walks to it (CR-5). The check reads two fields and walks no parent.
                    if (_focusOwner is { AcceptsFocus: false })
                    {
                        SetFocusOwner(null);
                    }

                    target = _focusOwner;
                    entry.Header.Target = target;
                }

                // Hook 2 of input-state: write this entry into the raw input state.
                _input.Record(in entry, target is not null);

                // The press target (Decision 13 of slider). A press and a move that the drain routes get the element of
                // the left press with no chord. A routed release gets it only when the release ends the press. An entry
                // with a target from the caller, a scroll, a key, and a char get null.
                t_pressTarget = !routed
                    ? null
                    : entry.Header.Type switch
                    {
                        UiEventType.PointerDown or UiEventType.PointerMove => LeftPressElement,
                        UiEventType.PointerUp when _releasePending => LeftPressElement,
                        _ => null,
                    };

                // The walk step. An entry with no target, or with a target outside the roots, gets no walk. The step
                // after the walk still runs for it.
                bool stopped = false;
                if (target is not null && IsInRootTree(target))
                {
                    long droppedBefore = _events.Dropped;

                    switch (entry.Header.Type)
                    {
                        case UiEventType.PointerMove:
                        case UiEventType.PointerDown:
                        case UiEventType.PointerUp:
                            stopped = DispatchQueued<PointerEvent>(in entry);
                            walks++;
                            break;

                        case UiEventType.Scroll:
                            stopped = DispatchQueued<ScrollEvent>(in entry);
                            walks++;
                            break;

                        case UiEventType.KeyDown:
                        case UiEventType.KeyUp:
                            stopped = DispatchQueued<KeyEvent>(in entry);
                            walks++;
                            break;

                        case UiEventType.Char:
                            stopped = DispatchQueued<CharEvent>(in entry);
                            walks++;
                            break;

                        default:
                            // No event struct carries the type, so the entry gets no walk. The push rejects such a type.
                            break;
                    }

                    // A push from a handler into a full ring drops the oldest entry. While entries of this drain wait,
                    // that entry belongs to this drain, so the drain must not pop one more entry in its place.
                    remaining -= (int)Math.Min(remaining, _events.Dropped - droppedBefore);
                }

                // The press target lives for the walk of this entry only (Decision 13 of slider).
                t_pressTarget = previousPressTarget;

                // The end of a release (Decision 2 of button-and-toggle): Active stays on the pressed element through
                // the walk of the release, and goes off here. A release with no walk also clears Active here.
                FinishRelease();

                // The Tab move (Decision 5 of keyboard-focus). It runs after the walk, so a handler that stops the
                // Tab keeps the focus. A Tab with no owner got no walk, but it still moves the focus.
                bool tabMove = routed
                    && !stopped
                    && entry.Header.Type == UiEventType.KeyDown
                    && entry.Payload.Key.Key == TabKey
                    && (entry.Payload.Key.Modifiers & (UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super)) == 0;
                if (tabMove)
                {
                    if ((entry.Payload.Key.Modifiers & UiModifiers.Shift) != 0)
                    {
                        FocusPrevious();
                    }
                    else
                    {
                        FocusNext();
                    }
                }

                // The unhandled input step (A6 of state-and-commands). It runs after the walk, the end of a release,
                // and the Tab move, so the slot reads the focus of this point in the ring. The drain reads the slot
                // one time for each entry. Only an entry that the drain routes and that no handler stopped can reach it.
                UiUnhandledInputHandler? handler = OnUnhandledInput;
                if (handler is not null && routed && !stopped)
                {
                    // A key down that is not a Tab move. A Tab move is a built-in action, so it is a handled key.
                    if (entry.Header.Type == UiEventType.KeyDown && !tabMove)
                    {
                        handler(
                            UiEventType.KeyDown,
                            entry.Payload.Key.Key,
                            entry.Payload.Key.Modifiers,
                            entry.Payload.Key.IsRepeat);
                    }
                    else if (entry.Header.Type == UiEventType.PointerDown && target is null)
                    {
                        // A press that hit no element (case 2). A hit takes a press, also when no handler stops it, so
                        // a click on a button never reaches the slot. The click focus rule ran in the target step.
                        handler(
                            UiEventType.PointerDown,
                            (int)entry.Payload.Pointer.Button,
                            entry.Payload.Pointer.Modifiers,
                            false);
                    }
                }
            }

            return walks;
        }
        finally
        {
            // A handler that throws in the walk of a release skips the call after the walk step. This call clears
            // Active then (Decision 2 of button-and-toggle). The flag clears first, so the context stays valid.
            // A handler that throws also skips the write-back after the walk step, so the press target goes back here
            // (Decision 13 of slider).
            _draining = false;
            t_pressTarget = previousPressTarget;
            FinishRelease();

            // Hook 3 of input-state: write the delta and the game view from the gate at this point.
            _input.EndFrame(RoutingGate);
        }
    }

    /// <summary>
    /// Clears <see cref="PseudoStates.Hover"/> and <see cref="PseudoStates.Active"/> on a stored element that left the
    /// roots of this context, and drops the references to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method runs at the start of each drain (Decision 10). It costs one parent walk for each of the two fields and
    /// allocates nothing. A closed panel then does not keep a hover look when it comes back.
    /// </para>
    /// <para>
    /// The method keeps the button mask. The release of a button that went down before the removal still clears its
    /// bit (Decision 10). The release then finds no stored element, so it clears no state.
    /// </para>
    /// <para>
    /// Limit: each context keeps its own stored elements, and the states are bits on the element. An element that is
    /// not in the roots of this context can be in the roots of another context, and that context can have set its
    /// state. This method then clears the state that the other context set. Call <see cref="Update(float)"/> on the old
    /// context after you remove an element and before you add it to another context.
    /// </para>
    /// </remarks>
    private void DropStalePointerState()
    {
        if (_pointerHit is { } hovered && !IsInRootTree(hovered))
        {
            SetHover(null);
        }

        if (_activeElement is { } active && !IsInRootTree(active))
        {
            active.States &= ~PseudoStates.Active;
            _activeElement = null;
        }
    }

    /// <summary>
    /// Clears the keyboard focus when the owner cannot hold it now: the owner does not accept the focus, is not in the
    /// roots of this context, or is hidden.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method runs at the start of each drain, after <see cref="DropStalePointerState"/> (Decision 7 of
    /// keyboard-focus). "Does not accept" means that <see cref="Element.IsFocusable"/> is <see langword="false"/> or
    /// that the owner holds <see cref="PseudoStates.Disabled"/>. "Hidden" means <see cref="Display.None"/> or an
    /// opacity product of 0 or less on the owner or an ancestor, in the style of the last <see cref="Update(float)"/>.
    /// <see cref="SetFocus(Element)"/> checks the same rule.
    /// </para>
    /// <para>
    /// The repair rule: when the owner stays, the method sets <see cref="PseudoStates.Focus"/> on it again. Code can
    /// clear the bit through <see cref="Element.States"/>. The <see cref="Element.States"/> setter returns at once for
    /// an equal value, so the repair marks nothing when the bit is set already.
    /// </para>
    /// <para>
    /// The method costs two parent walks at most and allocates nothing. It creates no delegate, no closure, and no
    /// boxed value. With no owner, it does nothing.
    /// </para>
    /// <para>
    /// Limit: the state is a bit on the element. When the owner is in the roots of another context, that context can
    /// have set the bit too. This method then clears it. Call <see cref="Update(float)"/> on the old context after you
    /// remove an element and before you add it to another context.
    /// </para>
    /// <para>
    /// Limit: the method runs at the start of the drain only. A handler that disables the owner or clears
    /// <see cref="Element.IsFocusable"/> drops the owner at the next key or char entry of the same drain. A removed
    /// owner gets no walk. A style change that hides the owner takes effect after the next
    /// <see cref="Update(float)"/>.
    /// </para>
    /// </remarks>
    private void DropStaleFocus()
    {
        if (_focusOwner is not { } owner)
        {
            return;
        }

        if (!owner.AcceptsFocus || !IsInRootTree(owner) || !FocusWalk.IsShown(owner))
        {
            SetFocusOwner(null);
        }
        else
        {
            // The repair rule (CR-3): code can clear the bit through States while the owner stays.
            owner.States |= PseudoStates.Focus;
        }
    }

    /// <summary>
    /// Finds out whether the top of the parent chain of an element is a root of this context. The method uses a loop and
    /// allocates nothing.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true"/> when the top element is a root of this context.</returns>
    private bool IsInRootTree(Element element)
    {
        Element top = element;
        while (top.Parent is { } parent)
        {
            top = parent;
        }

        return top.IsRoot && _roots.IndexOf(top) >= 0;
    }

    /// <summary>
    /// Hit-tests the position of a pointer or scroll entry with no target. The method writes the hit into the target
    /// of the entry, stores the hit and the position, and moves <see cref="PseudoStates.Hover"/> to the hit. A move
    /// during a captured drag gets the capture element as its target in place of the hit. A press sets
    /// <see cref="PseudoStates.Active"/> and moves the keyboard focus. A release of the last button marks the clear of
    /// <see cref="PseudoStates.Active"/> as pending until the end of its walk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method runs in the target step of the drain, before the walk (Decision 9), so a handler reads the new
    /// state and the new focus owner. The release is the one exception (Decision 2 of button-and-toggle): its
    /// handler still reads <see cref="PseudoStates.Active"/> on the pressed element. It creates no delegate, no
    /// closure, and no boxed value.
    /// </para>
    /// <para>
    /// The payloads of a pointer and of a scroll overlap in memory, so the method branches on the type before it reads
    /// the position. A press sets <see cref="PseudoStates.Active"/> on the hit when no element holds it. The release of
    /// the last button clears its bit here, and <see cref="FinishRelease"/> clears the state on the stored element
    /// after the walk (Decision 8, and Decision 2 of button-and-toggle). The release walks to the element under the
    /// pointer and not to the stored element.
    /// </para>
    /// <para>
    /// The click focus (Decision 4 of keyboard-focus): after the Active step, a press of each button moves the focus
    /// to the hit element or to its nearest ancestor with <see cref="Element.IsFocusable"/>. When that element does not
    /// accept the focus, or when the press hit no such element or no element at all, the focus clears. A move, a
    /// release, and a scroll keep the focus. The drain calls this method for an entry with no target only, so a target
    /// from the caller skips the click focus, as it skips the states.
    /// </para>
    /// <para>
    /// The drag capture (Decision 3 of slider): after the hover step, a <see cref="UiEventType.PointerMove"/> goes to
    /// the element of the left press with no chord (Decision 13 of slider) when that element implements
    /// <see cref="IDragCapture"/>, its <see cref="IDragCapture.CapturesMoves"/> is <see langword="true"/>, it is
    /// in the roots of this context, and it is shown. The method writes that element into the target of the entry and
    /// returns it. This amends Decision 8 of pointer-routing for a move during a captured drag only: a move and a
    /// scroll still leave <see cref="PseudoStates.Active"/>. The hit test, <see cref="PseudoStates.Hover"/>,
    /// <see cref="PointerHit"/>, and the routing gate still follow the hit. A press, a release, and a scroll keep the
    /// route of the hit. The root check sends the move to the hit when a handler of the same drain removed the capture
    /// element. The shown rule is the rule of the focus: no <see cref="Display.None"/> and no opacity product of 0 or
    /// less on the element or an ancestor. The style of the last <see cref="Update(float)"/> decides. A hidden element
    /// keeps <see cref="PseudoStates.Active"/>, but its moves walk to the hit, so a hidden slider does not go to its
    /// minimum. The type pattern and the shown check allocate nothing.
    /// </para>
    /// </remarks>
    /// <param name="entry">The ring entry. Its type is a pointer type or <see cref="UiEventType.Scroll"/>, and its target is <see langword="null"/>.</param>
    /// <returns>
    /// The capture element for a move during a captured drag. Else the hit element, or <see langword="null"/> when no
    /// element holds the position.
    /// </returns>
    private Element? RoutePointer(ref UiQueuedEvent entry)
    {
        UiEventType type = entry.Header.Type;
        float2 position = type == UiEventType.Scroll
            ? entry.Payload.Scroll.Position
            : entry.Payload.Pointer.Position;
        Element? hit = HitTest(position);

        entry.Header.Target = hit;
        _pointerPosition = position;
        _hasPointerPosition = true;

        // One field is both the hover owner and the last hit (Decision 7). SetHover reads the old value of the field
        // and then stores the hit in it, so the store of the hit happens there.
        SetHover(hit);

        // The drag capture (Decision 3 of slider): a move goes to the element of the left press with no chord
        // (Decision 13 of slider) when that element captures its moves. The hit test, Hover, and PointerHit above
        // stay on the hit. The root check sends the move to the hit when a handler of this drain removed the element.
        // The shown check sends the move to the hit when a style change hid the element or an ancestor. The layout
        // gives a hidden element a rect of 0, so a captured move would set the minimum.
        if (type == UiEventType.PointerMove
            && LeftPressElement is IDragCapture { CapturesMoves: true } and Element capture
            && IsInRootTree(capture)
            && FocusWalk.IsShown(capture))
        {
            entry.Header.Target = capture;
            return capture;
        }

        // The state of Active follows the press and the release only (Decision 8). A move and a scroll leave it.
        if (type == UiEventType.PointerDown)
        {
            PressButton(entry.Payload.Pointer.Button, hit);

            // The click focus (Decision 4 of keyboard-focus): a press of each button moves the focus to the hit or to
            // its nearest focusable ancestor. A press on no such element, or over nothing, clears the focus. The move
            // runs before the walk, so a handler of this press reads the new owner.
            FocusOnPress(hit);
        }
        else if (type == UiEventType.PointerUp)
        {
            ReleaseButton(entry.Payload.Pointer.Button);
        }

        return hit;
    }

    /// <summary>
    /// Moves the keyboard focus for a press: to the hit element, or to its nearest ancestor with
    /// <see cref="Element.IsFocusable"/>. A press on no such element, or over nothing, clears the focus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method stops at the first element with <see cref="Element.IsFocusable"/> (Notes Q3 of keyboard-focus).
    /// When that element does not accept the focus, the focus clears. The method does not look past it to a higher
    /// ancestor. The hit element comes from the hit test, so it is in the roots and needs no root check (P1-3).
    /// </para>
    /// <para>
    /// The parent walk is a loop. It creates no delegate, no closure, and no boxed value.
    /// </para>
    /// </remarks>
    /// <param name="hit">The element under the pointer, or <see langword="null"/>.</param>
    private void FocusOnPress(Element? hit)
    {
        Element? candidate = hit;
        while (candidate is not null && !candidate.IsFocusable)
        {
            candidate = candidate.Parent;
        }

        SetFocusOwner(candidate is not null && candidate.AcceptsFocus ? candidate : null);
    }

    /// <summary>
    /// Records a button as down. When no element holds <see cref="PseudoStates.Active"/> and the press hit an
    /// element, the method sets the state on that element and stores it. It also stores the button of that press and
    /// the chord flag (Decision 13 of slider).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method reads the other buttons of the mask before the bit of this press goes in. When this press sets
    /// <see cref="PseudoStates.Active"/>, it writes <see cref="_activeButton"/>, and it writes
    /// <see cref="_activeChord"/> as <see langword="true"/> when another button is down. A press over no element sets
    /// no state, but its bit stays in the mask, so a later press that sets the state is a chord.
    /// </para>
    /// <para>
    /// When an element holds <see cref="PseudoStates.Active"/> already, the method sets <see cref="_activeChord"/>.
    /// This is true for a press of each button, also over no element, and also for a second press of the same button
    /// after a dropped release. The state stays on the element of the first press (Decision 2 of button-and-toggle).
    /// </para>
    /// </remarks>
    /// <param name="button">The button that goes down.</param>
    /// <param name="hit">The element under the pointer, or <see langword="null"/>.</param>
    private void PressButton(PointerButton button, Element? hit)
    {
        int bit = (uint)button < 32u ? 1 << (int)button : 0;
        bool otherDown = (_pressedButtons & ~bit) != 0;
        _pressedButtons |= bit;

        if (_activeElement is not null)
        {
            // A button goes down while an element holds Active: the gesture is a chord (Decision 13 of slider).
            _activeChord = true;
        }
        else if (hit is not null)
        {
            hit.States |= PseudoStates.Active;
            _activeElement = hit;
            _activeButton = button;
            _activeChord = otherDown;
        }
    }

    /// <summary>
    /// Records a button as up. When no button is down after that and an element holds
    /// <see cref="PseudoStates.Active"/>, the method marks the release as pending. The state stays on the stored
    /// element through the walk of the release. <see cref="FinishRelease"/> then clears it (Decision 2 of
    /// button-and-toggle).
    /// </summary>
    /// <remarks>
    /// The method clears the bit of the button at once, before the walk, as Decision 9 of pointer-routing tells. Only
    /// the clear of <see cref="PseudoStates.Active"/> waits for the end of the walk.
    /// </remarks>
    /// <param name="button">The button that goes up.</param>
    private void ReleaseButton(PointerButton button)
    {
        if ((uint)button < 32u)
        {
            _pressedButtons &= ~(1 << (int)button);
        }

        if (_pressedButtons == 0 && _activeElement is not null)
        {
            _releasePending = true;
        }
    }

    /// <summary>
    /// Clears <see cref="PseudoStates.Active"/> on the stored element and drops it, when a release of the last button
    /// is pending. Else the method does nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The drain calls the method after the walk step of each entry, before the Tab step. A release over no element
    /// gets no walk, and this call still clears the state. The <c>finally</c> block of the drain calls the method one
    /// more time, so a handler that throws does not leave <see cref="PseudoStates.Active"/> on an element (Decision 2
    /// of button-and-toggle).
    /// </para>
    /// <para>
    /// A handler of the release can remove the stored element from the tree. The method then clears the state on the
    /// removed element, as the stale rule (Decision 10) does at the start of the next drain.
    /// </para>
    /// </remarks>
    private void FinishRelease()
    {
        if (!_releasePending)
        {
            return;
        }

        _releasePending = false;
        if (_activeElement is { } active)
        {
            _activeElement = null;
            active.States &= ~PseudoStates.Active;
        }
    }

    /// <summary>
    /// Moves <see cref="PseudoStates.Hover"/> from the hover owner of this context to another element, and stores
    /// that element in <see cref="PointerHit"/>.
    /// </summary>
    /// <remarks>
    /// The call returns at once for the same element, so it marks nothing. Else it clears
    /// <see cref="PseudoStates.Hover"/> on the old owner and sets it on the new element. Each change marks one
    /// subtree for <see cref="DirtyFlags.Style"/>. The state goes on the element only, not on its ancestors
    /// (Decision R1).
    /// </remarks>
    /// <param name="hit">The new hover owner, or <see langword="null"/> for no owner.</param>
    private void SetHover(Element? hit)
    {
        Element? old = _pointerHit;
        if (ReferenceEquals(old, hit))
        {
            return;
        }

        if (old is not null)
        {
            old.States &= ~PseudoStates.Hover;
        }

        _pointerHit = hit;
        if (hit is not null)
        {
            hit.States |= PseudoStates.Hover;
        }
    }

    /// <summary>
    /// Moves <see cref="PseudoStates.Focus"/> from the focus owner of this context to another element, and stores
    /// that element in <see cref="FocusedElement"/>.
    /// </summary>
    /// <remarks>
    /// The method copies the pattern of <see cref="SetHover(Element?)"/>. For the same element, the call sets
    /// <see cref="PseudoStates.Focus"/> on it again and returns, so a repeated set marks nothing when the bit is set
    /// (the repair rule). Else it clears <see cref="PseudoStates.Focus"/> on the old owner and sets it on the new
    /// element. Each change marks one subtree for <see cref="DirtyFlags.Style"/>. The state goes on
    /// the element only (Decision 1 of keyboard-focus). The method does not check the accept rule. The caller checks it.
    /// A change to a new owner that is a <see cref="TextField"/> restarts the blink of its caret, so the caret shows at
    /// once (Decision 11 of text-field). The repair branch restarts nothing.
    /// </remarks>
    /// <param name="next">The new focus owner, or <see langword="null"/> for no owner.</param>
    private void SetFocusOwner(Element? next)
    {
        Element? old = _focusOwner;
        if (ReferenceEquals(old, next))
        {
            // The repair rule (CR-3): code can clear the bit through States while the owner stays.
            if (next is not null)
            {
                next.States |= PseudoStates.Focus;
            }

            return;
        }

        if (old is not null)
        {
            old.States &= ~PseudoStates.Focus;
        }

        _focusOwner = next;
        if (next is not null)
        {
            next.States |= PseudoStates.Focus;
            if (next is TextField field)
            {
                field.RestartCaretBlink();
            }
        }
    }

    /// <summary>
    /// Copies an event into a ring entry and pushes the entry. The entry header gets the phase
    /// <see cref="UiEventPhase.None"/> and no current target.
    /// </summary>
    /// <typeparam name="T">The event struct.</typeparam>
    /// <param name="e">The event.</param>
    private void Enqueue<T>(in T e)
        where T : struct, IUiEvent<T>
    {
        // HeaderOf takes a ref, so read the header through a copy. The copy is a struct on the stack.
        T copy = e;
        ref UiEventHeader header = ref T.HeaderOf(ref copy);

        // A type that the struct does not carry would read the wrong payload fields. Reject it before it enters the ring.
        if (!T.Accepts(header.Type))
        {
            ThrowTypeMismatch<T>(header.Type);
        }

        // Start from a default payload. Store writes only the fields of its kind (P1-7).
        UiQueuedEvent entry = default;
        entry.Header = new UiEventHeader(header.Type, header.Timestamp, header.Target);

        // A queued entry never arrives stopped, also when the caller reuses a stopped struct (P2).
        entry.Header.ResetWalkState();
        T.Store(in copy, ref entry.Payload);
        _events.Push(in entry);
    }

    /// <summary>Throws the exception for an event type that the struct does not carry. It allocates on this path only.</summary>
    /// <typeparam name="T">The event struct.</typeparam>
    /// <param name="type">The rejected type.</param>
    /// <exception cref="ArgumentException">Always.</exception>
    [DoesNotReturn]
    private static void ThrowTypeMismatch<T>(UiEventType type)
        where T : struct, IUiEvent<T>
    {
        string hint = type switch
        {
            UiEventType.PointerMove or UiEventType.PointerDown or UiEventType.PointerUp => "Use a PointerEvent.",
            UiEventType.Scroll => "Use a ScrollEvent.",
            UiEventType.KeyDown or UiEventType.KeyUp => "Use a KeyEvent.",
            UiEventType.Char => "Use a CharEvent.",
            _ => "Set a valid type in the event header.",
        };

        throw new ArgumentException($"A {typeof(T).Name} cannot carry the type {type}. {hint}", "e");
    }

    /// <summary>Makes an event from a ring entry and sends it along the path of its target.</summary>
    /// <typeparam name="T">The event struct of the entry type.</typeparam>
    /// <param name="entry">The ring entry.</param>
    /// <returns><see langword="true"/> when a handler stopped the walk.</returns>
    private bool DispatchQueued<T>(in UiQueuedEvent entry)
        where T : struct, IUiEvent<T>
    {
        T e = T.Load(in entry.Header, in entry.Payload);
        EventWalk.Dispatch(ref e, ref _eventPath);
        return T.HeaderOf(ref e).IsPropagationStopped;
    }

    /// <summary>
    /// Reads the localized text of each <see cref="Label"/> with a <see cref="Label.LocalizedText"/> and of each
    /// <see cref="Button"/> with a <see cref="Button.LocalizedText"/> again, in each root and its subtree. A label or a
    /// button with a new text gets <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Brutal.Localization</c> has no change event. Call this method after a locale change: after
    /// <c>LocalizedStrings.SetLocalizedStringProvider</c>, <c>LStrings.SetLocalizedStringProvider</c>,
    /// <see cref="Brutal.Localization.LString.Localize(string)"/>, or
    /// <see cref="Brutal.Localization.LString.RemoveLocalization"/>. Do not call it in each frame.
    /// <see cref="Update(float)"/> does not call it.
    /// </para>
    /// <para>
    /// The method walks each element of each root. It marks a label or a button only when the content of its
    /// localized text changed (ordinal). A new string with the same content marks nothing. The method marks no other
    /// element.
    /// </para>
    /// <para>
    /// The method allocates nothing when the buffer of each label and each button is large enough. A longer text grows
    /// the buffer of its element one time. The method needs no <see cref="Measurer"/>. The next
    /// <see cref="Update(float)"/> measures the new text.
    /// </para>
    /// </remarks>
    public void RefreshLocalizedText()
    {
        ReadOnlySpan<Element> roots = _roots.AsSpan();
        for (int i = 0; i < roots.Length; i++)
        {
            RefreshSubtree(roots[i]);
        }
    }

    /// <summary>
    /// Calls <see cref="Label.RefreshLocalized"/> on each label and <see cref="Button.RefreshLocalized"/> on each
    /// button in a subtree, in tree order.
    /// </summary>
    /// <remarks>
    /// The recursion depth is the depth of the tree. A refresh changes no child list. The method checks the two types
    /// one after the other (Decision 7 of interactive-elements/button-and-toggle).
    /// </remarks>
    /// <param name="element">The root of the subtree.</param>
    private static void RefreshSubtree(Element element)
    {
        if (element is Label label)
        {
            label.RefreshLocalized();
        }
        else if (element is Button button)
        {
            button.RefreshLocalized();
        }

        ReadOnlySpan<Element> children = element.ChildStorage.AsSpan();
        for (int i = 0; i < children.Length; i++)
        {
            RefreshSubtree(children[i]);
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
