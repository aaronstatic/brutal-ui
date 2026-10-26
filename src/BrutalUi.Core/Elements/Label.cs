using System;
using Brutal.Localization;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// An element that shows one text. The layout pass gives the label the size of its text, and the draw sends one
/// <see cref="IUiRenderer.Text"/> call.
/// </summary>
/// <remarks>
/// <para>
/// A label is a leaf. Do not add children to a label. The engine calls a measure callback for an element with no
/// children only, so a label with children loses the size of its text.
/// </para>
/// <para>
/// The label measures its text with the <see cref="UiContext.Measurer"/> of its context during
/// <see cref="UiContext.Update(float)"/>. Create the context with <see cref="UiContext(in UiContext.CreateInfo)"/>.
/// A context with no measurer throws <see cref="InvalidOperationException"/> from <see cref="UiContext.Update(float)"/>
/// when the label needs a measure.
/// </para>
/// <para>
/// The label wraps its text at the available width. A text that fits takes the width of its text. A text that does
/// not fit wraps at the available width and takes the width of its widest line. The measure and the draw wrap at the
/// same lines.
/// </para>
/// <para>
/// The rule has two limits for a measurer that gives widths that are not whole numbers. (1) The draw wraps at a
/// fractional width plus a small float noise. A line that fills the width to within this noise can break at a
/// different place. (2) A measured width within 1/64 pixel above a whole number snaps down to that whole number in
/// the draw. The widest line can then wrap in the draw. A measurer that gives whole-pixel widths has no such limit.
/// <c>ImGuiRenderer</c> rounds each width up to a whole pixel.
/// </para>
/// <para>
/// The rule has one more limit. A minimum width above the available width gives the box the lines of the available
/// width. The draw wraps at the box width and can use fewer lines.
/// </para>
/// <para>
/// The label has no text values of its own. It reads <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>,
/// and <see cref="ComputedStyle.FontSize"/> of <see cref="Element.Style"/>. The text values come from a rule, the inline
/// set (<see cref="Element.Inline"/>), or the parent. A root takes the text defaults of its context. Write
/// <c>label.Inline.Color</c>, <c>label.Inline.Font</c>, or <c>label.Inline.FontSize</c> to set a value on one label.
/// </para>
/// <para>
/// A change of the font or of the font size marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>
/// in the style pass, so the label measures its text again. A change of the color marks
/// <see cref="DirtyFlags.Paint"/> only.
/// </para>
/// <para>
/// <see cref="ComputedStyle.Font"/> is an id of the renderer. The measurer and the renderer of
/// <see cref="UiContext.Draw(IUiRenderer)"/> must use the same font ids. The id 0 is the default font of the renderer.
/// </para>
/// <para>
/// The label draws its text at the top-left corner of its content box: the border box minus the padding and the
/// border. The text wraps at the width of the content box.
/// </para>
/// <para>
/// Each <c>SetText</c> overload compares the old text first. A change marks the dirty bits that the change needs. A
/// set to the same text marks nothing. Do not call <see cref="Element.MarkDirty(DirtyFlags)"/> for a label text.
/// </para>
/// <para>
/// A label with a <see cref="LocalizedText"/> reads <see cref="LString.Localized"/> at creation, at
/// <see cref="SetText(LString?)"/>, and at <see cref="UiContext.RefreshLocalizedText"/> only. Call
/// <see cref="UiContext.RefreshLocalizedText"/> after a locale change.
/// </para>
/// </remarks>
public sealed class Label : Element
{
    /// <summary>The measure callback of each label. The constructor sets it one time, so the layout pass allocates nothing.</summary>
    private static readonly MeasureCallback s_measure = MeasureLabel;

    /// <summary>The type id of each label.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("Label");

    /// <summary>
    /// The owned text. The field has no <see langword="readonly"/> modifier, because each call changes the struct in
    /// place. A copy of the struct loses the change.
    /// </summary>
    private Utf8Text _text;

    /// <summary>Creates a label with the text of the create-info.</summary>
    /// <remarks>
    /// The constructor encodes <see cref="CreateInfo.Text"/>, or <see cref="LString.Localized"/> of
    /// <see cref="CreateInfo.LocalizedText"/>, to UTF-8 one time. No text gives an empty label.
    /// </remarks>
    /// <param name="info">The create-info.</param>
    /// <exception cref="ArgumentException"><see cref="CreateInfo.Text"/> and <see cref="CreateInfo.LocalizedText"/> are both set.</exception>
    public Label(in CreateInfo info)
    {
        if (info.Text is not null && info.LocalizedText is not null)
        {
            throw new ArgumentException(
                "Set Text or LocalizedText of the Label.CreateInfo, not both.",
                nameof(info));
        }

        _text = new Utf8Text(info.Text, info.LocalizedText);
        Layout.Measure = s_measure;
    }

    /// <summary>Gets the type id of the label: <c>Label</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Gets the owned UTF-8 text.</summary>
    /// <remarks>The span is valid until the next change of the text.</remarks>
    public ReadOnlySpan<byte> Text => _text.Span;

    /// <summary>
    /// Gets the localizable text, or <see langword="null"/> when the text comes from a span or a string.
    /// </summary>
    /// <remarks>
    /// <see cref="SetText(LString?)"/> and <see cref="CreateInfo.LocalizedText"/> set it.
    /// <see cref="SetText(ReadOnlySpan{byte})"/> and <see cref="SetText(string?)"/> clear it.
    /// </remarks>
    public LString? LocalizedText => _text.Localized;

    /// <summary>Copies a UTF-8 text into the owned buffer and clears <see cref="LocalizedText"/>.</summary>
    /// <remarks>
    /// <para>
    /// The method compares the bytes with the current text first. The same bytes mark nothing. Other bytes mark
    /// <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>. The method does not check the bytes for
    /// valid UTF-8.
    /// </para>
    /// <para>
    /// The buffer grows by doubling and never shrinks. A text that fits the buffer allocates nothing. The span can be
    /// a part of <see cref="Text"/>.
    /// </para>
    /// </remarks>
    /// <param name="utf8">The UTF-8 text. An empty span gives an empty text.</param>
    public void SetText(ReadOnlySpan<byte> utf8)
    {
        if (_text.Set(utf8))
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>Encodes a text to UTF-8 into the owned buffer and clears <see cref="LocalizedText"/>.</summary>
    /// <remarks>
    /// <para>
    /// The method compares the text with the current text first (ordinal). The same text marks nothing. Another text
    /// marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>.
    /// </para>
    /// <para>
    /// The buffer grows by doubling and never shrinks. A text that fits the buffer allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    public void SetText(string? text)
    {
        if (_text.Set(text))
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>
    /// Sets the localizable text. The label shows <see cref="LString.Localized"/> as it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method reads <see cref="LString.Localized"/> one time and compares it with the current text (ordinal). The
    /// same text marks nothing. Another text marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>.
    /// </para>
    /// <para>
    /// The label reads the <see cref="LString"/> again at <see cref="UiContext.RefreshLocalizedText"/> only. Call it
    /// after a locale change. An <see cref="LString"/> with replacement variables shows its localized template text.
    /// To show a formatted text, format it and call <see cref="SetText(ReadOnlySpan{byte})"/>.
    /// </para>
    /// </remarks>
    /// <param name="text">The localizable text. <see langword="null"/> clears <see cref="LocalizedText"/> and gives an empty text.</param>
    public void SetText(LString? text)
    {
        if (_text.Set(text))
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>
    /// Reads <see cref="LString.Localized"/> of <see cref="LocalizedText"/> again, and shows the new text when it
    /// changed. <see cref="UiContext.RefreshLocalizedText"/> calls it.
    /// </summary>
    /// <remarks>
    /// A label with no <see cref="LocalizedText"/>, or with the same string reference, does nothing. A new reference
    /// with the same content (ordinal) marks nothing. A new content marks <see cref="DirtyFlags.Layout"/> and
    /// <see cref="DirtyFlags.Paint"/>. The method allocates nothing when the buffer is large enough.
    /// </remarks>
    internal void RefreshLocalized()
    {
        if (_text.Refresh())
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>
    /// Draws the text at the top-left corner of the content box, with the color scaled by the opacity. The wrap width
    /// is <see cref="DrawWrapWidth(float, float)"/> of the content box and the border box.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The draw reads <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>, and
    /// <see cref="ComputedStyle.FontSize"/> of <see cref="Element.Style"/>.
    /// </para>
    /// <para>An empty text or a color with an alpha of 0 after the scale draws nothing.</para>
    /// <para>
    /// The content box is the border box minus the padding and the border of each edge
    /// (<see cref="LayoutNode.ContentBox(in float4)"/>). Its width is <c>w - (left + right)</c>, in the order of the
    /// layout engine for the inner width. Thus the draw wraps at the width that the measure callback got, or at the
    /// measured width of the text.
    /// </para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the call.</param>
    /// <param name="borderBox">The border box of the label in viewport pixels: x, y, width, and height.</param>
    /// <param name="opacity">The product of the opacities from the root to the label, from 0 to 1.</param>
    internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
    {
        float4 content = Layout.ContentBox(in borderBox);
        Utf8Text.Paint(renderer, in Style, in content, borderBox.Z, opacity, Text);
    }

    /// <summary>
    /// Gives the wrap width of the draw for the width of the content box and the width of the border box.
    /// </summary>
    /// <remarks>The method calls <see cref="Utf8Text.DrawWrapWidth(float, float)"/>. That method holds the rule.</remarks>
    /// <param name="contentWidth">The width of the content box in pixels.</param>
    /// <param name="borderBoxWidth">The width of the border box in pixels.</param>
    /// <returns>The wrap width in pixels. 0 means no wrap.</returns>
    internal static float DrawWrapWidth(float contentWidth, float borderBoxWidth)
    {
        return Utf8Text.DrawWrapWidth(contentWidth, borderBoxWidth);
    }

    /// <summary>
    /// Gives the wrap width that the measure sends to the renderer for an available width. The draw starts from the
    /// same value (see <see cref="DrawWrapWidth(float, float)"/>).
    /// </summary>
    /// <remarks>The method calls <see cref="Utf8Text.SnapWrapWidth(float)"/>. That method holds the rule.</remarks>
    /// <param name="width">The available width in pixels.</param>
    /// <returns>The wrap width in pixels. 0 means no wrap.</returns>
    internal static float SnapWrapWidth(float width)
    {
        return Utf8Text.SnapWrapWidth(width);
    }

    /// <summary>
    /// Measures the text of a label with the measurer of the current <see cref="UiContext.Update(float)"/> call.
    /// </summary>
    /// <remarks>
    /// The callback checks for a measurer first. Then it calls
    /// <see cref="Utf8Text.Measure(IUiRenderer, in ComputedStyle, ReadOnlySpan{byte}, float, MeasureMode)"/>. That
    /// method holds the measure rule. The callback gives a finite size.
    /// </remarks>
    /// <param name="element">The label.</param>
    /// <param name="width">The available inner width in pixels.</param>
    /// <param name="widthMode">The rule for <paramref name="width"/>.</param>
    /// <param name="height">The available inner height in pixels.</param>
    /// <param name="heightMode">The rule for <paramref name="height"/>.</param>
    /// <returns>The size of the text in pixels.</returns>
    /// <exception cref="InvalidOperationException">The current update has no measurer.</exception>
    private static float2 MeasureLabel(
        Element element,
        float width,
        MeasureMode widthMode,
        float height,
        MeasureMode heightMode)
    {
        IUiRenderer measurer = UiContext.CurrentMeasurer
            ?? throw new InvalidOperationException(
                "A Label needs a UiContext with a measurer. Create the context with UiContext.CreateInfo.");

        Label label = (Label)element;
        return Utf8Text.Measure(measurer, in label.Style, label.Text, width, widthMode);
    }

    /// <summary>
    /// Compares a UTF-8 text with the UTF-8 form of a UTF-16 text, with no allocation.
    /// </summary>
    /// <remarks>
    /// The method calls <see cref="Utf8Text.Utf8Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>. That method holds
    /// the rule.
    /// </remarks>
    /// <param name="utf8">The UTF-8 text.</param>
    /// <param name="text">The UTF-16 text.</param>
    /// <returns><see langword="true"/> when the UTF-8 form of <paramref name="text"/> has the same bytes as <paramref name="utf8"/>.</returns>
    internal static bool Utf8Equals(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> text)
    {
        return Utf8Text.Utf8Equals(utf8, text);
    }

    /// <summary>The create-info of a <see cref="Label"/>.</summary>
    /// <remarks>
    /// The create-info has no required member: each member has a safe default. It has no text values. Set the color,
    /// the font, and the font size with a rule, with <see cref="Element.Inline"/>, or on the parent.
    /// </remarks>
    public struct CreateInfo
    {
        /// <summary>The text. The label encodes it to UTF-8 one time. <see langword="null"/> gives an empty text.</summary>
        /// <remarks>Set <see cref="Text"/> or <see cref="LocalizedText"/>, not both.</remarks>
        public string? Text;

        /// <summary>
        /// The localizable text. The label encodes <see cref="LString.Localized"/> to UTF-8 one time.
        /// <see langword="null"/> gives no localizable text.
        /// </summary>
        /// <remarks>
        /// Set <see cref="Text"/> or <see cref="LocalizedText"/>, not both. Call
        /// <see cref="UiContext.RefreshLocalizedText"/> after a locale change.
        /// </remarks>
        public LString? LocalizedText;
    }
}
