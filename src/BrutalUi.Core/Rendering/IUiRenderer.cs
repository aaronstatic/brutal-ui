using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The contract between the element tree and a renderer. The draw walk calls it to turn the tree into pixels.
/// </summary>
/// <remarks>
/// <para>
/// The members take spans and structs only. No member takes a string or an array.
/// </para>
/// <para>
/// All positions and sizes are in viewport pixels. The origin is the top-left corner of the viewport.
/// A rect is a <see cref="float4"/> with x, y, width, and height, in this order.
/// </para>
/// <para>
/// The caller calls the members on the main thread only. An implementation needs no lock.
/// </para>
/// <para>
/// An implementation allocates no managed memory after warm-up. <c>ThousandElementAllocationTests</c> guards the walk
/// with <c>RecordingRenderer</c>. Each other implementation needs its own check.
/// </para>
/// <para>
/// One frame is a <see cref="Begin"/> call, draw calls, and an <see cref="End"/> call.
/// <see cref="MeasureText"/> is the exception. It does not belong to a frame.
/// </para>
/// </remarks>
public interface IUiRenderer
{
    /// <summary>
    /// Starts a frame. One <see cref="Begin"/> call comes before each draw call of a frame.
    /// <see cref="MeasureText"/> is the exception.
    /// </summary>
    /// <remarks>
    /// <see cref="Begin"/> starts a clean frame. It clears each clip of an earlier frame that had no <see cref="End"/>.
    /// </remarks>
    /// <param name="displaySize">
    /// The size of the viewport in pixels. It is <see cref="UiContext.ViewportSize"/>. A <c>NaN</c> component means no limit.
    /// </param>
    /// <param name="scale">
    /// The display scale. The value is 1 in this release.
    /// The rects are in viewport pixels, the unit of <see cref="UiContext.ViewportSize"/>.
    /// <paramref name="scale"/> is the ratio of framebuffer pixels to viewport pixels.
    /// A renderer that draws in framebuffer pixels multiplies each rect by <paramref name="scale"/>.
    /// </param>
    void Begin(in float2 displaySize, float scale);

    /// <summary>
    /// Pushes a clip rect onto the clip stack. The renderer draws nothing outside the last clip rect.
    /// </summary>
    /// <remarks>
    /// The caller intersects the rect with the current clip rect before the call.
    /// The width or the height can be 0. Then the renderer draws nothing until the matching <see cref="PopClip"/>.
    /// </remarks>
    /// <param name="rect">The clip rect: x, y, width, and height in viewport pixels.</param>
    void PushClip(in float4 rect);

    /// <summary>
    /// Removes the last clip rect from the clip stack. Each <see cref="PushClip"/> call gets one <see cref="PopClip"/> call before <see cref="End"/>.
    /// </summary>
    void PopClip();

    /// <summary>
    /// Draws a rect with a fill and a border.
    /// </summary>
    /// <remarks>
    /// The renderer fills the rect with <paramref name="color"/> when the alpha of <paramref name="color"/> is above 0.
    /// The renderer draws the border inside the rect when <paramref name="borderWidth"/> is above 0 and the alpha of
    /// <paramref name="borderColor"/> is above 0. <paramref name="radius"/> applies to the fill and to the border.
    /// </remarks>
    /// <param name="rect">The border box: x, y, width, and height in viewport pixels.</param>
    /// <param name="color">The fill color.</param>
    /// <param name="radius">
    /// The corner radii in pixels. A value of 0 or less, or <c>NaN</c>, gives a square corner.
    /// The renderer limits a radius to half the shorter side of the rect.
    /// </param>
    /// <param name="borderWidth">The width of the border in pixels.</param>
    /// <param name="borderColor">The border color.</param>
    void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor);

    /// <summary>
    /// Draws a texture in a rect.
    /// </summary>
    /// <remarks>
    /// A <see cref="TextureHandle"/> with id 0 (<see cref="TextureHandle.IsNone"/>) draws nothing.
    /// </remarks>
    /// <param name="rect">The rect of the image: x, y, width, and height in viewport pixels.</param>
    /// <param name="texture">The texture.</param>
    /// <param name="uv">The texture coordinates: u0, v0, u1, and v1.</param>
    /// <param name="tint">The tint. The renderer multiplies each texel by the tint.</param>
    /// <param name="radius">
    /// The corner radii in pixels. A value of 0 or less, or <c>NaN</c>, gives a square corner.
    /// The renderer limits a radius to half the shorter side of the rect.
    /// </param>
    void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius);

    /// <summary>
    /// Draws UTF-8 text.
    /// </summary>
    /// <remarks>
    /// An empty span draws nothing. A <paramref name="color"/> with an alpha of 0 draws nothing.
    /// </remarks>
    /// <param name="pos">The top-left corner of the first line in viewport pixels.</param>
    /// <param name="font">The font. Id 0 is the default font of the renderer.</param>
    /// <param name="size">The font size in pixels.</param>
    /// <param name="color">The text color.</param>
    /// <param name="utf8">The text as UTF-8 bytes. The span is valid for the call only. Do not keep it.</param>
    /// <param name="wrapWidth">
    /// The width in viewport pixels at which the text wraps. A value of 0 or less, or <c>NaN</c>, means no wrap.
    /// The same value given to <see cref="MeasureText"/> gives the same lines.
    /// </param>
    void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8, float wrapWidth);

    /// <summary>
    /// Measures UTF-8 text. The call draws nothing.
    /// </summary>
    /// <remarks>
    /// The caller can call this method at any time on the main thread, also outside a <see cref="Begin"/> and <see cref="End"/> pair,
    /// for example from a measure callback during <c>UiContext.Update</c>. The call does not depend on the clip stack.
    /// </remarks>
    /// <param name="font">The font. Id 0 is the default font of the renderer.</param>
    /// <param name="size">The font size in pixels.</param>
    /// <param name="utf8">The text as UTF-8 bytes. The span is valid for the call only. Do not keep it.</param>
    /// <param name="wrapWidth">The wrap width in pixels. A value of 0 or less, or <c>NaN</c>, means no wrap.</param>
    /// <returns>The width and the height of the text in pixels.</returns>
    float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth);

    /// <summary>
    /// Ends the frame.
    /// </summary>
    void End();
}
