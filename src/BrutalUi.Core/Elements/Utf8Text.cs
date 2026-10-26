using System;
using System.Buffers;
using System.Text;
using System.Text.Unicode;
using Brutal.Localization;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The owned UTF-8 text of an element that shows one text, with its measure rule and its draw rule.
/// <see cref="Label"/> and <see cref="Button"/> each hold one.
/// </summary>
/// <remarks>
/// <para>
/// The struct holds the UTF-8 bytes, the string of the bytes when it knows it, and the localizable text. The buffer
/// grows by doubling and never shrinks. A text that fits the buffer allocates nothing.
/// </para>
/// <para>
/// Each <c>Set</c> method and <see cref="Refresh"/> return <see langword="true"/> when the bytes changed. The struct
/// sets no dirty bit. The owner marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/> on
/// <see langword="true"/>.
/// </para>
/// <para>
/// The owner holds the struct in a field with no <see langword="readonly"/> modifier, and calls each method on that
/// field. A method call on a copy changes the copy only, and the owner loses the change. A
/// <see langword="readonly"/> field gives a copy for each call of a method that changes the struct.
/// </para>
/// <para>
/// A <see langword="default"/> instance holds an empty text with no buffer. The first text that needs bytes creates
/// the buffer. The owner creates the struct with <see cref="Utf8Text(string?, LString?)"/>.
/// </para>
/// </remarks>
internal struct Utf8Text
{
    /// <summary>The start length of the owned UTF-8 buffer, in bytes.</summary>
    private const int InitialTextCapacity = 32;

    /// <summary>The length of the stack buffer of <see cref="Utf8Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>, in bytes.</summary>
    private const int CompareChunkLength = 256;

    /// <summary>The distance in pixels from a whole number that <see cref="SnapWrapWidth(float)"/> snaps: 1/64 pixel.</summary>
    private const float WrapSnapTolerance = 1f / 64f;

    /// <summary>The owned UTF-8 text. The buffer grows by doubling and never shrinks.</summary>
    private byte[] _utf8 = new byte[InitialTextCapacity];

    /// <summary>The count of the text bytes in <see cref="_utf8"/>.</summary>
    private int _length;

    /// <summary>
    /// The string that <see cref="_utf8"/> holds, or <see langword="null"/> when the struct does not know it (a text
    /// from a span). The string compare of <see cref="ApplyText(string)"/> uses it.
    /// </summary>
    private string? _lastText;

    /// <summary>The localizable text, or <see langword="null"/> for a text from a span or a string.</summary>
    private LString? _localized;

    /// <summary>The value of <see cref="LString.Localized"/> that the struct encoded last.</summary>
    private string? _lastLocalized;

    /// <summary>Creates the owned text from a string or from a localizable text.</summary>
    /// <remarks>
    /// <para>
    /// The constructor encodes <paramref name="text"/>, or <see cref="LString.Localized"/> of
    /// <paramref name="localized"/>, to UTF-8 one time. No text gives an empty text.
    /// </para>
    /// <para>
    /// The owner checks first that the two values are not both set. When both are set, the constructor uses
    /// <paramref name="localized"/>.
    /// </para>
    /// </remarks>
    /// <param name="text">The text, or <see langword="null"/>.</param>
    /// <param name="localized">The localizable text, or <see langword="null"/>.</param>
    internal Utf8Text(string? text, LString? localized)
    {
        if (localized is not null)
        {
            _localized = localized;
            _lastLocalized = localized.Localized;
            EncodeText(_lastLocalized);
        }
        else
        {
            EncodeText(text);
        }
    }

    /// <summary>Gets the owned UTF-8 text.</summary>
    /// <remarks>The span is valid until the next change of the text.</remarks>
    internal readonly ReadOnlySpan<byte> Span => _utf8.AsSpan(0, _length);

    /// <summary>
    /// Gets the localizable text, or <see langword="null"/> when the text comes from a span or a string.
    /// </summary>
    internal readonly LString? Localized => _localized;

    /// <summary>Copies a UTF-8 text into the owned buffer and clears <see cref="Localized"/>.</summary>
    /// <remarks>
    /// <para>
    /// The method compares the bytes with the current text first. The same bytes give <see langword="false"/>.
    /// The method does not check the bytes for valid UTF-8.
    /// </para>
    /// <para>
    /// The buffer grows by doubling and never shrinks. A text that fits the buffer allocates nothing. The span can be
    /// a part of <see cref="Span"/>.
    /// </para>
    /// </remarks>
    /// <param name="utf8">The UTF-8 text. An empty span gives an empty text.</param>
    /// <returns><see langword="true"/> when the bytes changed.</returns>
    internal bool Set(ReadOnlySpan<byte> utf8)
    {
        ClearLocalized();
        if (utf8.SequenceEqual(Span))
        {
            return false;
        }

        EnsureCapacity(utf8.Length);
        utf8.CopyTo(_utf8);
        _length = utf8.Length;
        _lastText = null;
        return true;
    }

    /// <summary>Encodes a text to UTF-8 into the owned buffer and clears <see cref="Localized"/>.</summary>
    /// <remarks>
    /// <para>
    /// The method compares the text with the current text first (ordinal). The same text gives
    /// <see langword="false"/>.
    /// </para>
    /// <para>
    /// The buffer grows by doubling and never shrinks. A text that fits the buffer allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    /// <returns><see langword="true"/> when the bytes changed.</returns>
    internal bool Set(string? text)
    {
        ClearLocalized();
        return ApplyText(text ?? string.Empty);
    }

    /// <summary>
    /// Sets the localizable text. The struct holds <see cref="LString.Localized"/> as it is.
    /// </summary>
    /// <remarks>
    /// The method reads <see cref="LString.Localized"/> one time and compares it with the current text (ordinal). The
    /// same text gives <see langword="false"/>. The struct reads the <see cref="LString"/> again at
    /// <see cref="Refresh"/> only.
    /// </remarks>
    /// <param name="text">The localizable text. <see langword="null"/> clears <see cref="Localized"/> and gives an empty text.</param>
    /// <returns><see langword="true"/> when the bytes changed.</returns>
    internal bool Set(LString? text)
    {
        if (text is null)
        {
            ClearLocalized();
            return ApplyText(string.Empty);
        }

        string localized = text.Localized;
        _localized = text;
        _lastLocalized = localized;
        return ApplyText(localized);
    }

    /// <summary>
    /// Reads <see cref="LString.Localized"/> of <see cref="Localized"/> again, and holds the new text when it
    /// changed.
    /// </summary>
    /// <remarks>
    /// A struct with no <see cref="Localized"/>, or with the same string reference, does nothing. A new reference
    /// with the same content (ordinal) gives <see langword="false"/>. The method allocates nothing when the buffer is
    /// large enough.
    /// </remarks>
    /// <returns><see langword="true"/> when the bytes changed.</returns>
    internal bool Refresh()
    {
        if (_localized is null)
        {
            return false;
        }

        string localized = _localized.Localized;
        if (ReferenceEquals(localized, _lastLocalized))
        {
            return false;
        }

        _lastLocalized = localized;
        return ApplyText(localized);
    }

    /// <summary>
    /// Draws a text at the top-left corner of a content box, with the color scaled by the opacity. The wrap width
    /// is <see cref="DrawWrapWidth(float, float)"/> of the content box and the border box.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The draw reads <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>, and
    /// <see cref="ComputedStyle.FontSize"/> of <paramref name="style"/>.
    /// </para>
    /// <para>An empty text or a color with an alpha of 0 after the scale draws nothing.</para>
    /// <para>
    /// The owner computes <paramref name="content"/> with <see cref="LayoutNode.ContentBox(in float4)"/> of the border
    /// box. Its width is <c>w - (left + right)</c>, in the order of the layout engine for the inner width. Thus the
    /// draw wraps at the width that the measure callback got, or at the measured width of the text.
    /// </para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the call.</param>
    /// <param name="style">The computed style of the owner.</param>
    /// <param name="content">The content box of the owner in viewport pixels: x, y, width, and height.</param>
    /// <param name="borderBoxWidth">The width of the border box of the owner in pixels.</param>
    /// <param name="opacity">The product of the opacities from the root to the owner, from 0 to 1.</param>
    /// <param name="text">The UTF-8 text.</param>
    internal static void Paint(
        IUiRenderer renderer,
        in ComputedStyle style,
        in float4 content,
        float borderBoxWidth,
        float opacity,
        ReadOnlySpan<byte> text)
    {
        Rgba8 color = style.Color.ScaleAlpha(opacity);
        if (text.IsEmpty || color.A == 0)
        {
            return;
        }

        float2 position = new(content.X, content.Y);
        renderer.Text(in position, style.Font, style.FontSize, in color, text, DrawWrapWidth(content.Z, borderBoxWidth));
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
    /// Measures a text with a measurer. The measure callback of the owner calls it after its measurer check.
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
    /// line. The engine gives the owner that width, and the draw wraps at it. A greedy break at the limit takes the
    /// longest prefix that fits the limit. That prefix also fits the widest line, so the draw takes the same prefix.
    /// </para>
    /// <para>
    /// The method calls the measurer one or two times. The layout cache stops a repeat of the same constraint, so
    /// the owner keeps no measure cache of its own.
    /// </para>
    /// <para>
    /// The method gives a finite size. A component of a measurer result that is not finite, or that is below 0,
    /// becomes 0 (<see cref="FiniteSize(float2)"/>).
    /// </para>
    /// </remarks>
    /// <param name="measurer">The measurer of the current update.</param>
    /// <param name="style">The computed style of the owner. The method reads the font and the font size.</param>
    /// <param name="text">The UTF-8 text.</param>
    /// <param name="width">The available inner width in pixels.</param>
    /// <param name="widthMode">The rule for <paramref name="width"/>.</param>
    /// <returns>The size of the text in pixels.</returns>
    internal static float2 Measure(
        IUiRenderer measurer,
        in ComputedStyle style,
        ReadOnlySpan<byte> text,
        float width,
        MeasureMode widthMode)
    {
        float2 free = FiniteSize(measurer.MeasureText(style.Font, style.FontSize, text, 0f));
        if (widthMode == MeasureMode.Undefined)
        {
            return free;
        }

        float limit = SnapWrapWidth(width);
        if (limit <= 0f || free.X <= limit)
        {
            return free;
        }

        return FiniteSize(measurer.MeasureText(style.Font, style.FontSize, text, limit));
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
    /// Gives a size with each component that is not finite, or that is below 0, set to 0.
    /// </summary>
    /// <remarks>
    /// The contract of <see cref="MeasureCallback"/> asks for a finite size. A measurer result that is not finite
    /// would give the owner an infinite or a <c>NaN</c> rect.
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
    /// Holds a string. The method compares it with the current text first, and returns <see langword="true"/> for a
    /// new text only.
    /// </summary>
    /// <remarks>
    /// The compare is ordinal with <see cref="_lastText"/> when the struct knows the string of its buffer. Otherwise
    /// the compare is on the UTF-8 bytes. The method does not change <see cref="_localized"/>.
    /// </remarks>
    /// <param name="text">The text. It must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the bytes changed.</returns>
    private bool ApplyText(string text)
    {
        bool same = (_lastText is not null && string.Equals(_lastText, text, StringComparison.Ordinal))
            || Utf8Equals(Span, text);
        if (same)
        {
            _lastText = text;
            return false;
        }

        EncodeText(text);
        return true;
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
    /// <remarks>
    /// A growth does not copy the old text. The caller writes the new text after the call. A
    /// <see langword="default"/> instance has no buffer. Its first growth starts from
    /// <see cref="InitialTextCapacity"/>.
    /// </remarks>
    /// <param name="needed">The count of bytes.</param>
    private void EnsureCapacity(int needed)
    {
        byte[]? buffer = _utf8;
        if (buffer is not null && needed <= buffer.Length)
        {
            return;
        }

        int length = buffer is null || buffer.Length == 0 ? InitialTextCapacity : buffer.Length;
        while (length < needed)
        {
            length = length > int.MaxValue / 2 ? needed : length * 2;
        }

        _utf8 = new byte[length];
    }

    /// <summary>
    /// Encodes a text to UTF-8 into the owned buffer. The buffer grows by doubling when the text does not fit.
    /// </summary>
    /// <remarks>The method sets <see cref="_lastText"/>. It returns no change result.</remarks>
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
}
