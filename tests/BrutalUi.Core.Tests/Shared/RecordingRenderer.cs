using System;
using System.Globalization;
using System.Text;
using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>The kind of a recorded <see cref="DrawCall"/>. Each kind is one member of <see cref="IUiRenderer"/>.</summary>
internal enum DrawCallKind
{
    /// <summary>A <see cref="IUiRenderer.Begin"/> call.</summary>
    Begin,

    /// <summary>A <see cref="IUiRenderer.PushClip"/> call.</summary>
    PushClip,

    /// <summary>A <see cref="IUiRenderer.PopClip"/> call.</summary>
    PopClip,

    /// <summary>A <see cref="IUiRenderer.Rect"/> call.</summary>
    Rect,

    /// <summary>A <see cref="IUiRenderer.Image"/> call.</summary>
    Image,

    /// <summary>A <see cref="IUiRenderer.Text"/> call.</summary>
    Text,

    /// <summary>An <see cref="IUiRenderer.End"/> call.</summary>
    End,
}

/// <summary>
/// One recorded call of <see cref="RecordingRenderer"/>. <see cref="Kind"/> tells which fields hold a value.
/// </summary>
/// <remarks>
/// The fields of a kind that the call does not use stay at their default.
/// </remarks>
internal struct DrawCall
{
    /// <summary>The kind of the call.</summary>
    public DrawCallKind Kind;

    /// <summary>
    /// The rect: x, y, width, and height. <see cref="DrawCallKind.Rect"/>, <see cref="DrawCallKind.Image"/>, and
    /// <see cref="DrawCallKind.PushClip"/> set it. <see cref="DrawCallKind.Begin"/> sets (0, 0, display width,
    /// display height).
    /// </summary>
    public float4 Rect;

    /// <summary>The top-left corner of the text. <see cref="DrawCallKind.Text"/> sets it.</summary>
    public float2 Position;

    /// <summary>The fill color of <see cref="DrawCallKind.Rect"/> or the text color of <see cref="DrawCallKind.Text"/>.</summary>
    public Rgba8 Color;

    /// <summary>The border color. <see cref="DrawCallKind.Rect"/> sets it.</summary>
    public Rgba8 BorderColor;

    /// <summary>The tint. <see cref="DrawCallKind.Image"/> sets it.</summary>
    public Rgba8 Tint;

    /// <summary>The corner radii. <see cref="DrawCallKind.Rect"/> and <see cref="DrawCallKind.Image"/> set them.</summary>
    public Corners Radius;

    /// <summary>The border width. <see cref="DrawCallKind.Rect"/> sets it.</summary>
    public float BorderWidth;

    /// <summary>The font size. <see cref="DrawCallKind.Text"/> sets it.</summary>
    public float Size;

    /// <summary>The wrap width, as the caller gave it. <see cref="DrawCallKind.Text"/> sets it.</summary>
    public float WrapWidth;

    /// <summary>The display scale. <see cref="DrawCallKind.Begin"/> sets it.</summary>
    public float Scale;

    /// <summary>The texture. <see cref="DrawCallKind.Image"/> sets it.</summary>
    public TextureHandle Texture;

    /// <summary>The font. <see cref="DrawCallKind.Text"/> sets it.</summary>
    public FontHandle Font;

    /// <summary>The texture coordinates: u0, v0, u1, and v1. <see cref="DrawCallKind.Image"/> sets them.</summary>
    public float4 Uv;

    /// <summary>The start of the text bytes in the text buffer. <see cref="DrawCallKind.Text"/> sets it.</summary>
    public int TextStart;

    /// <summary>The count of the text bytes. <see cref="DrawCallKind.Text"/> sets it.</summary>
    public int TextLength;
}

/// <summary>
/// A renderer that records each call of one frame, for tests and benchmarks.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IUiRenderer.Begin"/> sets the call count, the text byte count, and the clip depth to 0, so one instance
/// records one frame. The call list starts at 256 calls and the text buffer at 1024 bytes. Both grow by doubling and
/// never shrink, so the renderer allocates nothing after warm-up.
/// </para>
/// <para>
/// <see cref="IUiRenderer.PopClip"/> at a clip depth of 0 and <see cref="IUiRenderer.End"/> at a clip depth above 0
/// throw <see cref="InvalidOperationException"/>. A walk bug then fails the test that finds it.
/// </para>
/// <para>
/// The benchmark project links this file, so the file uses no test framework type and no internal member of
/// <c>BrutalUi.Core</c>.
/// </para>
/// </remarks>
internal sealed class RecordingRenderer : IUiRenderer
{
    private const int InitialCallCapacity = 256;
    private const int InitialTextCapacity = 1024;

    private DrawCall[] _calls = new DrawCall[InitialCallCapacity];
    private byte[] _text = new byte[InitialTextCapacity];
    private int _callCount;
    private int _textCount;

    /// <summary>Gets the calls of the last frame, in call order. The span is valid until the next call.</summary>
    public ReadOnlySpan<DrawCall> Calls => _calls.AsSpan(0, _callCount);

    /// <summary>Gets the count of <see cref="IUiRenderer.MeasureText"/> calls since the creation of the renderer.</summary>
    public int MeasureCount { get; private set; }

    /// <summary>
    /// Gets the wrap width of the last <see cref="MeasureText"/> call with a wrap width above 0, or 0 before such a call.
    /// </summary>
    public float LastMeasureWrapWidth { get; private set; }

    /// <summary>Gets the count of clip pushes with no pop.</summary>
    public int ClipDepth { get; private set; }

    /// <summary>Gets the text bytes of a recorded <see cref="DrawCallKind.Text"/> call.</summary>
    /// <param name="call">The call. A call of another kind gives an empty span.</param>
    /// <returns>The UTF-8 bytes. The span is valid until the next frame.</returns>
    public ReadOnlySpan<byte> GetText(in DrawCall call)
    {
        return _text.AsSpan(call.TextStart, call.TextLength);
    }

    /// <inheritdoc/>
    public void Begin(in float2 displaySize, float scale)
    {
        _callCount = 0;
        _textCount = 0;
        ClipDepth = 0;

        ref DrawCall call = ref Add(DrawCallKind.Begin);
        call.Rect = new float4(0f, 0f, displaySize.X, displaySize.Y);
        call.Scale = scale;
    }

    /// <inheritdoc/>
    public void PushClip(in float4 rect)
    {
        ref DrawCall call = ref Add(DrawCallKind.PushClip);
        call.Rect = rect;
        ClipDepth++;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The clip depth is 0.</exception>
    public void PopClip()
    {
        if (ClipDepth == 0)
        {
            throw new InvalidOperationException("PopClip has no matching PushClip.");
        }

        ClipDepth--;
        Add(DrawCallKind.PopClip);
    }

    /// <inheritdoc/>
    public void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor)
    {
        ref DrawCall call = ref Add(DrawCallKind.Rect);
        call.Rect = rect;
        call.Color = color;
        call.Radius = radius;
        call.BorderWidth = borderWidth;
        call.BorderColor = borderColor;
    }

    /// <inheritdoc/>
    public void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius)
    {
        ref DrawCall call = ref Add(DrawCallKind.Image);
        call.Rect = rect;
        call.Texture = texture;
        call.Uv = uv;
        call.Tint = tint;
        call.Radius = radius;
    }

    /// <inheritdoc/>
    public void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8, float wrapWidth)
    {
        int start = _textCount;
        int needed = start + utf8.Length;
        if (needed > _text.Length)
        {
            int length = _text.Length;
            while (length < needed)
            {
                length *= 2;
            }

            Array.Resize(ref _text, length);
        }

        utf8.CopyTo(_text.AsSpan(start));
        _textCount = needed;

        ref DrawCall call = ref Add(DrawCallKind.Text);
        call.Position = pos;
        call.Font = font;
        call.Size = size;
        call.WrapWidth = wrapWidth;
        call.Color = color;
        call.TextStart = start;
        call.TextLength = utf8.Length;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The call records nothing and allocates nothing. It adds 1 to <see cref="MeasureCount"/>. Each byte is
    /// <c>size * 0.5</c> wide. A wrap width that is 0 or less, <c>NaN</c>, or infinite gives no wrap:
    /// (bytes * size * 0.5, size).
    /// </para>
    /// <para>A finite wrap width above 0 gives the wrap rule:</para>
    /// <list type="bullet">
    /// <item>Words split at the space byte. Two spaces in a row give an empty word with a width of 0.</item>
    /// <item>A word goes on the current line when the line width plus one space plus the word fits at or below the
    /// wrap width. Otherwise it starts a new line. The first word of the text starts the first line.</item>
    /// <item>A word that is wider than the wrap width starts a new line. The rule cuts it into parts of
    /// <c>max(1, floor(wrap / (size * 0.5)))</c> bytes, one part on each line. The next word follows the last part
    /// when it fits.</item>
    /// <item>The width is the widest line. The height is the line count times the size. Empty text gives
    /// (0, size).</item>
    /// </list>
    /// <para>
    /// The rule counts bytes, not glyphs. Use a size of 16 in a test: a byte is then 8 pixels wide, and no float noise
    /// occurs.
    /// </para>
    /// <para>
    /// A call with a wrap width above 0 sets <see cref="LastMeasureWrapWidth"/>, also a call with an infinite wrap
    /// width.
    /// </para>
    /// </remarks>
    public float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth)
    {
        MeasureCount++;
        if (wrapWidth > 0f)
        {
            LastMeasureWrapWidth = wrapWidth;
        }

        if (!(wrapWidth > 0f) || float.IsPositiveInfinity(wrapWidth))
        {
            return new float2(utf8.Length * size * 0.5f, size);
        }

        if (utf8.IsEmpty)
        {
            return new float2(0f, size);
        }

        WrapState state = new() { LineCount = 1 };
        int start = 0;
        while (true)
        {
            ReadOnlySpan<byte> rest = utf8[start..];
            int space = rest.IndexOf((byte)' ');
            int wordBytes = space < 0 ? rest.Length : space;
            PlaceWord(ref state, wordBytes, size, wrapWidth);
            if (space < 0)
            {
                break;
            }

            start += space + 1;
        }

        return new float2(state.WidestBytes * size * 0.5f, state.LineCount * size);
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The clip depth is above 0.</exception>
    public void End()
    {
        if (ClipDepth != 0)
        {
            throw new InvalidOperationException("End comes with a clip depth above 0. A PushClip has no matching PopClip.");
        }

        Add(DrawCallKind.End);
    }

    /// <summary>
    /// Gives the calls of the last frame as text, one line for each call, with the invariant culture.
    /// </summary>
    /// <remarks>
    /// <para>The lines have these forms. A rect is <c>x,y,w,h</c>. A color is <c>#RRGGBBAA</c>.</para>
    /// <list type="bullet">
    /// <item><c>Begin 400x300 scale 1</c></item>
    /// <item><c>PushClip 2,2,196,96</c></item>
    /// <item><c>PopClip</c></item>
    /// <item><c>Rect 0,0,200,100 fill #0A141EFF border 0 #00000000 radius 0,0,0,0</c> (the radius order is top-left, top-right, bottom-right, and bottom-left)</item>
    /// <item><c>Image 0,0,32,32 texture 7 uv 0,0,1,1 tint #FFFFFFFF radius 0,0,0,0</c></item>
    /// <item><c>Text 12,12 font 0 size 16 #FFFFFFFF "hi"</c> (the text is the UTF-8 bytes as a string, with no escape)</item>
    /// <item><c>Text 12,12 font 0 size 16 #FFFFFFFF wrap 70 "hi"</c> (a call with a wrap width above 0 adds <c>wrap</c> and the width before the text)</item>
    /// <item><c>End</c></item>
    /// </list>
    /// <para>A line feed (<c>\n</c>) separates the lines. The text has no line feed at the end.</para>
    /// <para>The method allocates. Do not call it in a measured body.</para>
    /// </remarks>
    /// <returns>The text.</returns>
    public string Describe()
    {
        StringBuilder text = new();
        ReadOnlySpan<DrawCall> calls = Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (i > 0)
            {
                text.Append('\n');
            }

            ref readonly DrawCall call = ref calls[i];
            switch (call.Kind)
            {
                case DrawCallKind.Begin:
                    text.Append("Begin ").Append(F(call.Rect.Z)).Append('x').Append(F(call.Rect.W))
                        .Append(" scale ").Append(F(call.Scale));
                    break;
                case DrawCallKind.PushClip:
                    text.Append("PushClip ").Append(F4(call.Rect));
                    break;
                case DrawCallKind.PopClip:
                    text.Append("PopClip");
                    break;
                case DrawCallKind.Rect:
                    text.Append("Rect ").Append(F4(call.Rect))
                        .Append(" fill ").Append(call.Color.ToString())
                        .Append(" border ").Append(F(call.BorderWidth)).Append(' ').Append(call.BorderColor.ToString())
                        .Append(" radius ").Append(FCorners(call.Radius));
                    break;
                case DrawCallKind.Image:
                    text.Append("Image ").Append(F4(call.Rect))
                        .Append(" texture ").Append(call.Texture.Id.ToString(CultureInfo.InvariantCulture))
                        .Append(" uv ").Append(F4(call.Uv))
                        .Append(" tint ").Append(call.Tint.ToString())
                        .Append(" radius ").Append(FCorners(call.Radius));
                    break;
                case DrawCallKind.Text:
                    text.Append("Text ").Append(F(call.Position.X)).Append(',').Append(F(call.Position.Y))
                        .Append(" font ").Append(call.Font.Id.ToString(CultureInfo.InvariantCulture))
                        .Append(" size ").Append(F(call.Size))
                        .Append(' ').Append(call.Color.ToString());
                    if (call.WrapWidth > 0f)
                    {
                        text.Append(" wrap ").Append(F(call.WrapWidth));
                    }

                    text.Append(" \"").Append(Encoding.UTF8.GetString(GetText(in call))).Append('"');
                    break;
                case DrawCallKind.End:
                    text.Append("End");
                    break;
                default:
                    text.Append("Unknown");
                    break;
            }
        }

        return text.ToString();
    }

    /// <summary>Adds a call at the end of the list. The list grows by doubling when it is full.</summary>
    /// <param name="kind">The kind of the call.</param>
    /// <returns>A reference to the new call. All its other fields are at their default.</returns>
    private ref DrawCall Add(DrawCallKind kind)
    {
        if (_callCount == _calls.Length)
        {
            Array.Resize(ref _calls, _calls.Length * 2);
        }

        ref DrawCall call = ref _calls[_callCount];
        call = default;
        call.Kind = kind;
        _callCount++;
        return ref call;
    }

    /// <summary>Puts one word on the lines of the wrap rule of <see cref="MeasureText"/>.</summary>
    /// <param name="state">The lines so far.</param>
    /// <param name="wordBytes">The byte count of the word. An empty word has 0 bytes.</param>
    /// <param name="size">The font size.</param>
    /// <param name="wrapWidth">The wrap width. It is finite and above 0.</param>
    private static void PlaceWord(ref WrapState state, int wordBytes, float size, float wrapWidth)
    {
        if (wordBytes * size * 0.5f > wrapWidth)
        {
            // A word that is wider than the wrap width starts a new line, and the rule cuts it into parts.
            int partBytes = Math.Max(1, (int)MathF.Floor(wrapWidth / (size * 0.5f)));
            while (partBytes > 1 && partBytes * size * 0.5f > wrapWidth)
            {
                partBytes--;
            }

            int parts = (wordBytes + partBytes - 1) / partBytes;
            state.LineCount += state.LineUsed ? parts : parts - 1;
            state.WidestBytes = Math.Max(state.WidestBytes, partBytes);
            state.LineBytes = wordBytes - ((parts - 1) * partBytes);
            state.LineUsed = true;
        }
        else if (!state.LineUsed)
        {
            state.LineBytes = wordBytes;
            state.LineUsed = true;
        }
        else if ((state.LineBytes + 1 + wordBytes) * size * 0.5f <= wrapWidth)
        {
            state.LineBytes += 1 + wordBytes;
        }
        else
        {
            state.LineCount++;
            state.LineBytes = wordBytes;
        }

        state.WidestBytes = Math.Max(state.WidestBytes, state.LineBytes);
    }

    /// <summary>Formats one float with the invariant culture.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The text.</returns>
    private static string F(float value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Formats four floats as <c>x,y,z,w</c>.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The text.</returns>
    private static string F4(in float4 value)
    {
        return F(value.X) + "," + F(value.Y) + "," + F(value.Z) + "," + F(value.W);
    }

    /// <summary>Formats four radii as <c>top-left,top-right,bottom-right,bottom-left</c>.</summary>
    /// <param name="value">The radii.</param>
    /// <returns>The text.</returns>
    private static string FCorners(in Corners value)
    {
        return F(value.TopLeft) + "," + F(value.TopRight) + "," + F(value.BottomRight) + "," + F(value.BottomLeft);
    }

    /// <summary>The lines of one wrapped measure. The rule counts bytes, so no float noise adds up.</summary>
    private struct WrapState
    {
        /// <summary>The count of the lines. The measure starts it at 1.</summary>
        public int LineCount;

        /// <summary>The byte count of the current line, with the spaces between its words.</summary>
        public int LineBytes;

        /// <summary>The byte count of the widest line.</summary>
        public int WidestBytes;

        /// <summary>The current line holds a word. An empty word counts.</summary>
        public bool LineUsed;
    }
}
