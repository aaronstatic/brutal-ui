using System;
using System.Collections.Generic;
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
    }

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
    private bool _drawing;

    /// <summary>The sheets in the order of add.</summary>
    private readonly List<StyleSheet> _sheets = [];

    /// <summary>
    /// The rules of each sheet, sorted by the key (specificity, sheet index, rule index). The style pass applies them
    /// in this order. <see cref="AddSheet(StyleSheet)"/> and <see cref="RemoveSheet(StyleSheet)"/> build the array again.
    /// </summary>
    private StyleRule[] _cascade = [];

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
    /// the font id 0, and the font size 0.
    /// </summary>
    /// <remarks>
    /// A context with no measurer cannot lay out a <see cref="Label"/>. Use <see cref="UiContext(in CreateInfo)"/>
    /// for a tree with text.
    /// </remarks>
    public UiContext()
    {
        _textDefaults = CreateTextDefaults(null, default, 0f);
    }

    /// <summary>
    /// Creates a context with no roots, a viewport size of (0, 0), and the measurer and the text defaults of the
    /// create-info.
    /// </summary>
    /// <param name="info">The create-info.</param>
    /// <exception cref="ArgumentNullException">The measurer of <paramref name="info"/> is <see langword="null"/>.</exception>
    public UiContext(in CreateInfo info)
    {
        if (info.Measurer is null)
        {
            throw new ArgumentNullException(nameof(info), "The Measurer of the UiContext.CreateInfo must not be null.");
        }

        _measurer = info.Measurer;
        _textDefaults = CreateTextDefaults(info.DefaultColor, info.DefaultFont, info.DefaultFontSize);
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

        // A root from another context or from a parent needs the sheets and the text defaults of this context.
        root.MarkSubtreeStyle();
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
    /// Runs the style pass alone on each root. <see cref="Update(float)"/> calls it before the layout pass.
    /// </summary>
    /// <remarks>
    /// Tests call this method to read the dirty bits of the style pass before the layout pass clears them.
    /// </remarks>
    internal void UpdateStyles()
    {
        // The pass runs also with an empty cascade, so an inline value reaches the style with no sheet.
        // Each root gets the text defaults of the context as its parent text.
        StylePass.Run(_roots.AsSpan(), _cascade, in _textDefaults);
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
    /// Runs the passes on each root in frame order. The layout pass lays out each root against
    /// <see cref="ViewportSize"/> and clears the layout bits. The last pass clears each dirty bit that remains.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After the call, <see cref="LayoutNode.Rect"/> of each element in each root holds the new layout.
    /// Set <see cref="ViewportSize"/> before the first call.
    /// </para>
    /// <para>
    /// The style pass runs first. It restyles each element with <see cref="DirtyFlags.Style"/> from the rules of the
    /// sheets of <see cref="AddSheet(StyleSheet)"/> and from the inline set of <see cref="Element.Inline"/>. An inline
    /// value wins over each rule. The pass marks the elements with a new style for the layout and the paint. Each
    /// element with the style bit gets a new style from the defaults, the matching rules, the inline set, and the
    /// parent text. An element with no rule and no inline value gets the defaults.
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
    /// </remarks>
    /// <param name="deltaTime">The time since the last update, in seconds. No pass reads it yet. The framework and the transitions read it later.</param>
    /// <exception cref="InvalidOperationException">
    /// A <see cref="Label"/> needs a measure and the context has no <see cref="Measurer"/>.
    /// </exception>
    public void Update(float deltaTime)
    {
        ReadOnlySpan<Element> roots = _roots.AsSpan();

        // The style pass. It restyles each element with the style bit from the cascade and clears the style bits.
        UpdateStyles();

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
    /// Reads the localized text of each <see cref="Label"/> with a <see cref="Label.LocalizedText"/> again, in each
    /// root and its subtree. A label with a new text gets <see cref="DirtyFlags.Layout"/> and
    /// <see cref="DirtyFlags.Paint"/>.
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
    /// The method walks each element of each root. It marks a label only when the content of its localized text
    /// changed (ordinal). A new string with the same content marks nothing. The method marks no other element.
    /// </para>
    /// <para>
    /// The method allocates nothing when the buffer of each label is large enough. A longer text grows the buffer of
    /// its label one time. The method needs no <see cref="Measurer"/>. The next <see cref="Update(float)"/> measures
    /// the new text.
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

    /// <summary>Calls <see cref="Label.RefreshLocalized"/> on each label in a subtree, in tree order.</summary>
    /// <remarks>The recursion depth is the depth of the tree. A refresh changes no child list.</remarks>
    /// <param name="element">The root of the subtree.</param>
    private static void RefreshSubtree(Element element)
    {
        if (element is Label label)
        {
            label.RefreshLocalized();
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
