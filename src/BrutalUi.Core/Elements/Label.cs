using System;
using System.Buffers;
using System.Text;
using System.Text.Unicode;
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
    /// <summary>The start length of the owned UTF-8 buffer, in bytes.</summary>
    private const int InitialTextCapacity = 32;

    /// <summary>The length of the stack buffer of <see cref="Utf8Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>, in bytes.</summary>
    private const int CompareChunkLength = 256;

    /// <summary>The distance in pixels from a whole number that <see cref="SnapWrapWidth(float)"/> snaps: 1/64 pixel.</summary>
    private const float WrapSnapTolerance = 1f / 64f;

    /// <summary>The measure callback of each label. The constructor sets it one time, so the layout pass allocates nothing.</summary>
    private static readonly MeasureCallback s_measure = MeasureLabel;

    /// <summary>The type id of each label.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("Label");

    /// <summary>The owned UTF-8 text. The buffer grows by doubling and never shrinks.</summary>
    private byte[] _utf8 = new byte[InitialTextCapacity];

    /// <summary>The count of the text bytes in <see cref="_utf8"/>.</summary>
    private int _length;

    /// <summary>
    /// The string that <see cref="_utf8"/> holds, or <see langword="null"/> when the label does not know it (a text
    /// from a span). The string compare of <see cref="ApplyText(string)"/> uses it.
    /// </summary>
    private string? _lastText;

    /// <summary>The localizable text, or <see langword="null"/> for a text from a span or a string.</summary>
    private LString? _localized;

    /// <summary>The value of <see cref="LString.Localized"/> that the label encoded last.</summary>
    private string? _lastLocalized;

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

        if (info.LocalizedText is not null)
        {
            _localized = info.LocalizedText;
            _lastLocalized = info.LocalizedText.Localized;
            EncodeText(_lastLocalized);
        }
        else
        {
            EncodeText(info.Text);
        }

        Layout.Measure = s_measure;
    }

    /// <summary>Gets the type id of the label: <c>Label</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Gets the owned UTF-8 text.</summary>
    /// <remarks>The span is valid until the next change of the text.</remarks>
    public ReadOnlySpan<byte> Text => _utf8.AsSpan(0, _length);

    /// <summary>
    /// Gets the localizable text, or <see langword="null"/> when the text comes from a span or a string.
    /// </summary>
    /// <remarks>
    /// <see cref="SetText(LString?)"/> and <see cref="CreateInfo.LocalizedText"/> set it.
    /// <see cref="SetText(ReadOnlySpan{byte})"/> and <see cref="SetText(string?)"/> clear it.
    /// </remarks>
    public LString? LocalizedText => _localized;

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
        ClearLocalized();
        if (utf8.SequenceEqual(Text))
        {
            return;
        }

        EnsureCapacity(utf8.Length);
        utf8.CopyTo(_utf8);
        _length = utf8.Length;
        _lastText = null;
        MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
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
        ClearLocalized();
        ApplyText(text ?? string.Empty);
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
        if (text is null)
        {
            ClearLocalized();
            ApplyText(string.Empty);
            return;
        }

        string localized = text.Localized;
        _localized = text;
        _lastLocalized = localized;
        ApplyText(localized);
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
        if (_localized is null)
        {
            return;
        }

        string localized = _localized.Localized;
        if (ReferenceEquals(localized, _lastLocalized))
        {
            return;
        }

        _lastLocalized = localized;
        ApplyText(localized);
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
        ref readonly ComputedStyle style = ref Style;
        float4 content = Layout.ContentBox(in borderBox);
        Rgba8 color = style.Color.ScaleAlpha(opacity);
        ReadOnlySpan<byte> text = Text;
        if (text.IsEmpty || color.A == 0)
        {
            return;
        }

        float2 position = new(content.X, content.Y);
        renderer.Text(in position, style.Font, style.FontSize, in color, text, DrawWrapWidth(content.Z, borderBox.Z));
    }

    /// <summary>
    /// Gives the wrap width of the draw for the width of the content box and the width of the border box.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method snaps the content width with <see cref="SnapWrapWidth(float)"/>. A result of 0 or a whole number
    /// stays the same. A fractional result gets two units in the last place of the border box width on top.
    /// </para>
    /// <para>
    /// Under <see cref="MeasureMode.AtMost"/>, the box width is the measured width <c>w</c> plus the padding and the
    /// border, and the draw takes them off again. The float noise of the sum and of the subtraction is at most one
    /// unit in the last place of the box width. Thus the raise keeps the draw width at <c>w</c> or above, and the widest
    /// line does not wrap. Under <see cref="MeasureMode.Exactly"/>, the raise puts the draw width above the measure
    /// limit by the same noise. A break can differ only when a line fills the limit to within this noise.
    /// </para>
    /// <para>
    /// A whole number needs no raise: the snap already removes the noise there. A raise that is not finite gives the
    /// snapped width.
    /// </para>
    /// </remarks>
    /// <param name="contentWidth">The width of the content box in pixels.</param>
    /// <param name="borderBoxWidth">The width of the border box in pixels.</param>
    /// <returns>The wrap width in pixels. 0 means no wrap.</returns>
    internal static float DrawWrapWidth(float contentWidth, float borderBoxWidth)
    {
        float wrap = SnapWrapWidth(contentWidth);
        if (wrap <= 0f || wrap == MathF.Round(wrap))
        {
            return wrap;
        }

        float raised = wrap + (2f * (MathF.BitIncrement(borderBoxWidth) - borderBoxWidth));
        return float.IsFinite(raised) ? raised : wrap;
    }

    /// <summary>
    /// Gives the wrap width that the measure sends to the renderer for an available width. The draw starts from the
    /// same value (see <see cref="DrawWrapWidth(float, float)"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A value that is not finite, or 0 or less, gives 0 (no wrap). A value within 1/64 pixel of a whole number gives
    /// that whole number. Another value stays the same.
    /// </para>
    /// <para>
    /// The snap removes float noise near a whole number. The layout pass adds the padding and the border to the
    /// measured width, and the draw takes them off again. The result can differ from the measured width by one unit
    /// in the last place. A line that fits the measured width exactly can then wrap in the draw. For a whole-pixel
    /// measured width, the snap gives the same value on both sides. For a fractional measured width, the snap keeps
    /// the noise, and <see cref="DrawWrapWidth(float, float)"/> raises the draw width above it.
    /// </para>
    /// <para>
    /// The limit: a measured width within 1/64 pixel above a whole number snaps down in the draw. The widest line can
    /// then wrap in the draw. A measurer that gives whole-pixel widths does not meet this limit.
    /// </para>
    /// </remarks>
    /// <param name="width">The available width in pixels.</param>
    /// <returns>The wrap width in pixels. 0 means no wrap.</returns>
    internal static float SnapWrapWidth(float width)
    {
        if (!float.IsFinite(width) || width <= 0f)
        {
            return 0f;
        }

        float whole = MathF.Round(width);
        return MathF.Abs(width - whole) <= WrapSnapTolerance ? whole : width;
    }

    /// <summary>
    /// Measures the text of a label with the measurer of the current <see cref="UiContext.Update(float)"/> call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rule: measure the text with no wrap first. An undefined width gives that size. Otherwise the limit is
    /// <see cref="SnapWrapWidth(float)"/> of the width. A text that fits the limit, or a limit of 0, gives the size with
    /// no wrap. Another text wraps at the limit.
    /// </para>
    /// <para>
    /// The rule keeps the contract of <see cref="MeasureCallback"/>: with <see cref="MeasureMode.AtMost"/>, a text
    /// that fits gives the size of <see cref="MeasureMode.Undefined"/>. The callback gives the content size only, and
    /// it does not clamp to the limit. The engine adds the padding and the border, and it applies
    /// <see cref="MeasureMode.Exactly"/>, the minimum, and the maximum.
    /// </para>
    /// <para>
    /// The draw wraps at the same lines. With <see cref="MeasureMode.Exactly"/>, the measure and the draw both wrap at
    /// the snapped width. With <see cref="MeasureMode.AtMost"/>, the measure wraps at the limit and gives the widest
    /// line. The engine gives the label that width, and the draw wraps at it. A greedy break at the limit takes the
    /// longest prefix that fits the limit. That prefix also fits the widest line, so the draw takes the same prefix.
    /// </para>
    /// <para>
    /// The callback calls the measurer one or two times. The layout cache stops a repeat of the same constraint, so
    /// the label keeps no measure cache of its own.
    /// </para>
    /// <para>
    /// The callback gives a finite size. A component of a measurer result that is not finite, or that is below 0,
    /// becomes 0 (<see cref="FiniteSize(float2)"/>).
    /// </para>
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
        ref readonly ComputedStyle style = ref label.Style;
        float2 free = FiniteSize(measurer.MeasureText(style.Font, style.FontSize, label.Text, 0f));
        if (widthMode == MeasureMode.Undefined)
        {
            return free;
        }

        float limit = SnapWrapWidth(width);
        if (limit <= 0f || free.X <= limit)
        {
            return free;
        }

        return FiniteSize(measurer.MeasureText(style.Font, style.FontSize, label.Text, limit));
    }

    /// <summary>
    /// Gives a size with each component that is not finite, or that is below 0, set to 0.
    /// </summary>
    /// <remarks>
    /// The contract of <see cref="MeasureCallback"/> asks for a finite size. A measurer result that is not finite
    /// would give the label an infinite or a <c>NaN</c> rect.
    /// </remarks>
    /// <param name="size">The size from the measurer.</param>
    /// <returns>The size with each component finite and 0 or more.</returns>
    private static float2 FiniteSize(float2 size)
    {
        return new float2(FiniteLength(size.X), FiniteLength(size.Y));
    }

    /// <summary>Gives a length, or 0 for a length that is not finite or that is below 0.</summary>
    /// <param name="length">The length.</param>
    /// <returns>The length, or 0.</returns>
    private static float FiniteLength(float length)
    {
        return float.IsFinite(length) && length > 0f ? length : 0f;
    }

    /// <summary>
    /// Compares a UTF-8 text with the UTF-8 form of a UTF-16 text, with no allocation.
    /// </summary>
    /// <remarks>
    /// The method encodes the UTF-16 text in chunks into a stack buffer. It replaces an invalid surrogate with U+FFFD,
    /// as <see cref="Encoding.UTF8"/> does.
    /// </remarks>
    /// <param name="utf8">The UTF-8 text.</param>
    /// <param name="text">The UTF-16 text.</param>
    /// <returns><see langword="true"/> when the UTF-8 form of <paramref name="text"/> has the same bytes as <paramref name="utf8"/>.</returns>
    internal static bool Utf8Equals(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> text)
    {
        Span<byte> chunk = stackalloc byte[CompareChunkLength];
        while (!text.IsEmpty)
        {
            OperationStatus status = Utf8.FromUtf16(
                text,
                chunk,
                out int read,
                out int written,
                replaceInvalidSequences: true,
                isFinalBlock: true);
            if (read == 0 || (status != OperationStatus.Done && status != OperationStatus.DestinationTooSmall))
            {
                return false;
            }

            if (written > utf8.Length || !chunk[..written].SequenceEqual(utf8[..written]))
            {
                return false;
            }

            utf8 = utf8[written..];
            text = text[read..];
        }

        return utf8.IsEmpty;
    }

    /// <summary>
    /// Shows a string. The method compares it with the current text first, and marks
    /// <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/> for a new text only.
    /// </summary>
    /// <remarks>
    /// The compare is ordinal with <see cref="_lastText"/> when the label knows the string of its buffer. Otherwise
    /// the compare is on the UTF-8 bytes. The method does not change <see cref="_localized"/>.
    /// </remarks>
    /// <param name="text">The text. It must not be <see langword="null"/>.</param>
    private void ApplyText(string text)
    {
        bool same = (_lastText is not null && string.Equals(_lastText, text, StringComparison.Ordinal))
            || Utf8Equals(Text, text);
        if (same)
        {
            _lastText = text;
            return;
        }

        EncodeText(text);
        MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
    }

    /// <summary>Clears <see cref="_localized"/> and <see cref="_lastLocalized"/>. The text does not change.</summary>
    private void ClearLocalized()
    {
        _localized = null;
        _lastLocalized = null;
    }

    /// <summary>
    /// Makes the owned buffer hold at least a count of bytes. The buffer grows by doubling and never shrinks.
    /// </summary>
    /// <remarks>A growth does not copy the old text. The caller writes the new text after the call.</remarks>
    /// <param name="needed">The count of bytes.</param>
    private void EnsureCapacity(int needed)
    {
        if (needed <= _utf8.Length)
        {
            return;
        }

        int length = _utf8.Length;
        while (length < needed)
        {
            length = length > int.MaxValue / 2 ? needed : length * 2;
        }

        _utf8 = new byte[length];
    }

    /// <summary>
    /// Encodes a text to UTF-8 into the owned buffer. The buffer grows by doubling when the text does not fit.
    /// </summary>
    /// <remarks>The method sets <see cref="_lastText"/>. It sets no dirty bit.</remarks>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    private void EncodeText(string? text)
    {
        _lastText = text ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            _length = 0;
            return;
        }

        EnsureCapacity(Encoding.UTF8.GetByteCount(text));
        _length = Encoding.UTF8.GetBytes(text, _utf8.AsSpan());
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
