using System;
using System.Diagnostics.CodeAnalysis;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui;

/// <summary>
/// An <see cref="IUiRenderer"/> that draws on the background draw list of the ImGui main viewport.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="UiContext.Draw(IUiRenderer)"/> after <c>ImGui.NewFrame</c> and before <c>ImGui.Render</c>. The
/// background list is under all ImGui windows, so a debug window of ImGui shows on top of the UI.
/// </para>
/// <para>
/// Call the renderer on the main thread only. The renderer uses the current ImGui context. Use one ImGui context for
/// each process.
/// </para>
/// <para>
/// A viewport pixel is one ImGui display unit. The renderer adds the position of the main viewport to each position.
/// It does not multiply a position by the <c>scale</c> argument of <see cref="Begin"/>, and it does not use the
/// <c>displaySize</c> argument. The ImGui backend applies the framebuffer scale.
/// </para>
/// <para>
/// The renderer throws <see cref="InvalidOperationException"/> for a draw call before <see cref="Begin"/>, for a
/// <see cref="PopClip"/> with no open clip or with no clip of the renderer on the list, and for a
/// <see cref="Begin"/>, a <see cref="Text"/>, or a
/// <see cref="MeasureText"/> with no current ImGui context. Each check runs before the native call, because a native
/// ImGui assert does not stop the process.
/// </para>
/// <para>
/// <see cref="Text"/> and <see cref="MeasureText"/> give the font and the size to each ImGui call. They use the
/// dynamic fonts of ImGui 1.92, which bake each size on demand. They push no font and use no font stack of the frame,
/// so <see cref="MeasureText"/> works outside a frame.
/// </para>
/// <para>
/// The corner rule: ImGui rounds a rect with one radius and a corner mask. <see cref="Rect"/> and
/// <see cref="Image"/> use the largest radius (limited to half the shorter side) for each corner with a radius above
/// 0. The result is exact when the radii above 0 are equal. Mixed radii above 0 draw with the largest one. An exact
/// path for mixed radii is a later change.
/// </para>
/// <para>
/// <see cref="Begin"/>, the draw calls, <see cref="MeasureText"/>, and <see cref="End"/> allocate no managed memory
/// after warm-up.
/// </para>
/// </remarks>
public sealed unsafe class ImGuiRenderer : IUiRenderer
{
    /// <summary>
    /// The size of the clip stack of the background list with no clip of the renderer. ImGui pushes the viewport clip
    /// when it resets the list at the first access of a frame.
    /// </summary>
    private const int ViewportClipCount = 1;

    /// <summary>The background draw list of the frame. It is unset outside a frame.</summary>
    private ImDrawListPtr _list;

    /// <summary>The position of the main viewport at <see cref="Begin"/>.</summary>
    private float2 _origin;

    /// <summary>The ImGui frame count at <see cref="Begin"/>.</summary>
    private int _frameCount;

    /// <summary>The count of open clips of the frame.</summary>
    private int _clipDepth;

    /// <summary>The services of a renderer.</summary>
    public struct CreateInfo
    {
        /// <summary>The font service. It maps a <see cref="FontHandle"/> to an ImGui font.</summary>
        public required ImGuiFontService Fonts;

        /// <summary>The texture service. It maps a <see cref="TextureHandle"/> to an ImGui texture.</summary>
        public required ImGuiTextureService Textures;
    }

    /// <summary>Creates a renderer with two services.</summary>
    /// <remarks>
    /// The renderer does not own the services. Two renderers can use the same services, so a handle has the same
    /// meaning in each renderer.
    /// </remarks>
    /// <param name="info">The services.</param>
    /// <exception cref="ArgumentNullException">A service of <paramref name="info"/> is <see langword="null"/>.</exception>
    public ImGuiRenderer(in CreateInfo info)
    {
        ArgumentNullException.ThrowIfNull(info.Fonts, "info.Fonts");
        ArgumentNullException.ThrowIfNull(info.Textures, "info.Textures");
        Fonts = info.Fonts;
        Textures = info.Textures;
    }

    /// <summary>Gets the font service of the renderer.</summary>
    public ImGuiFontService Fonts { get; }

    /// <summary>Gets the texture service of the renderer.</summary>
    public ImGuiTextureService Textures { get; }

    /// <summary>Starts a frame on the background draw list of the main viewport.</summary>
    /// <remarks>
    /// <para>
    /// When an earlier frame of the same ImGui frame and the same list has no <see cref="End"/>, the method pops the
    /// clips of that frame first. In a new ImGui frame, ImGui resets the list, so the method pops nothing.
    /// </para>
    /// <para>
    /// The method stores the position of the main viewport as the origin. It does not use
    /// <paramref name="displaySize"/> or <paramref name="scale"/>.
    /// </para>
    /// </remarks>
    /// <param name="displaySize">The size of the viewport in pixels. The renderer does not use it.</param>
    /// <param name="scale">The display scale. The renderer does not use it.</param>
    /// <exception cref="InvalidOperationException">No ImGui context is current.</exception>
    public void Begin(in float2 displaySize, float scale)
    {
        if (Gui.GetCurrentContext().IsNull())
        {
            throw new InvalidOperationException(
                "No ImGui context is current. Call ImGui.CreateContext before the first draw.");
        }

        ImGuiViewportPtr viewport = Gui.GetMainViewport();
        ImDrawListPtr list = Gui.GetBackgroundDrawList(viewport);
        int frameCount = Gui.GetFrameCount();

        // The Begin contract: a frame with no End leaves clips on the list. The same list in the same ImGui frame
        // still holds them. A new ImGui frame resets the list, and a pop on an empty clip stack corrupts it.
        if (_clipDepth > 0 && list.Ptr == _list.Ptr && frameCount == _frameCount)
        {
            PopOpenClips(list, _clipDepth);
        }

        _list = list;
        _origin = viewport.Pos;
        _frameCount = frameCount;
        _clipDepth = 0;
    }

    /// <summary>Pushes a clip rect onto the clip stack of the draw list.</summary>
    /// <remarks>
    /// <para>
    /// ImGui intersects the rect with the current clip rect, so a finite clip stays inside the viewport. A rect with
    /// an area of 0 is valid. Then the list draws nothing until the matching <see cref="PopClip"/>.
    /// </para>
    /// <para>
    /// The non-finite rule: when a bound of the clip is not finite (a <c>NaN</c> or infinite component, or an overflow
    /// of <c>x + width</c>), the method pushes a clip with an area of 0. A coordinate of the top-left point that is
    /// not finite becomes the origin, and the bottom-right point equals the top-left point. The push still happens,
    /// so the stack stays balanced. ImGui keeps a <c>NaN</c> and an infinite min in its intersect, and the backend
    /// casts the clip to an integer scissor.
    /// </para>
    /// </remarks>
    /// <param name="rect">The clip rect: x, y, width, and height in viewport pixels.</param>
    /// <exception cref="InvalidOperationException">No <see cref="Begin"/> started the frame.</exception>
    public void PushClip(in float4 rect)
    {
        ThrowIfNoFrame();

        float2 min = new(_origin.X + rect.X, _origin.Y + rect.Y);
        float2 max = new(_origin.X + (rect.X + rect.Z), _origin.Y + (rect.Y + rect.W));
        if (!ImGuiMapping.HasFiniteBounds(min, max))
        {
            min = new float2(
                float.IsFinite(min.X) ? min.X : _origin.X,
                float.IsFinite(min.Y) ? min.Y : _origin.Y);
            max = min;
        }

        Gui.PInvoke.ImDrawList_PushClipRect(_list.Ptr, &min, &max, true);
        _clipDepth++;
    }

    /// <summary>Removes the last clip rect from the clip stack of the draw list.</summary>
    /// <remarks>
    /// The method reads the size of the clip stack of the list before the native pop. ImGui resets the list in a new
    /// ImGui frame, so the stack can hold only the viewport clip when the renderer has open clips of an earlier frame.
    /// A native pop then writes out of bounds, so the method throws.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="Begin"/> started the frame, no clip of the frame is open, or the clip stack of the list holds no
    /// clip of the renderer.
    /// </exception>
    public void PopClip()
    {
        ThrowIfNoFrame();
        if (_clipDepth == 0)
        {
            throw new InvalidOperationException("No clip is open. Call PushClip before PopClip.");
        }

        // The stack holds the viewport clip that ImGui pushes at the reset of the list. A size of 1 or less means that
        // the list holds no clip of this renderer. Throw before the depth changes, so a throw leaves the depth the same.
        if (_list._ClipRectStack.Count <= ViewportClipCount)
        {
            ThrowNoClipOnList();
        }

        _clipDepth--;
        Gui.PInvoke.ImDrawList_PopClipRect(_list.Ptr);
    }

    /// <summary>Draws a rect with a fill, rounded corners, and a border.</summary>
    /// <remarks>
    /// <para>
    /// A rect with no area, with a <c>NaN</c> or infinite component, or with a bound that is not finite (an overflow
    /// of <c>x + width</c>) draws nothing. The fill draws when the alpha of <paramref name="color"/> is above 0. The
    /// fill comes first.
    /// </para>
    /// <para>
    /// The corner rule: a radius of 0 or less, or <c>NaN</c>, gives a square corner. Each radius is limited to half the
    /// shorter side. ImGui rounds with one radius and a corner mask, so the rect uses the largest radius for each
    /// corner above 0. The result is exact when the radii above 0 are equal. Mixed radii above 0 draw with the largest
    /// one (see <see cref="ImGuiMapping.ResolveCorners"/>).
    /// </para>
    /// <para>
    /// The border draws when <paramref name="borderWidth"/> is above 0 and finite, and the alpha of
    /// <paramref name="borderColor"/> is above 0. The border stays inside the rect. The stroke rect is the rect inset
    /// by <c>borderWidth / 2 - 0.5</c>, and the stroke radius is <c>max(radius - borderWidth / 2, 0)</c>, so the outer
    /// edge of the stroke follows the radius of the fill. A border of half the shorter side or more covers the box. It
    /// draws as one fill in the border color with no stroke. When the border is opaque, the hidden fill is not drawn.
    /// </para>
    /// <para>
    /// A border below 1 pixel draws as 1 pixel, because ImGui draws a thin stroke as 1 pixel. The inset then uses a
    /// width of 1, so the stroke covers the span from 0 to 1 pixel inside the box.
    /// </para>
    /// </remarks>
    /// <param name="rect">The border box: x, y, width, and height in viewport pixels.</param>
    /// <param name="color">The fill color.</param>
    /// <param name="radius">The corner radii in pixels.</param>
    /// <param name="borderWidth">The width of the border in pixels.</param>
    /// <param name="borderColor">The border color.</param>
    /// <exception cref="InvalidOperationException">No <see cref="Begin"/> started the frame.</exception>
    public void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor)
    {
        ThrowIfNoFrame();
        if (!ImGuiMapping.HasArea(rect))
        {
            return;
        }

        float2 min = new(_origin.X + rect.X, _origin.Y + rect.Y);
        float2 max = new(_origin.X + (rect.X + rect.Z), _origin.Y + (rect.Y + rect.W));
        if (!ImGuiMapping.HasFiniteBounds(min, max))
        {
            return;
        }

        ImDrawFlags flags = ImGuiMapping.ResolveCorners(radius, rect.Z, rect.W, out float rounding);
        bool drawBorder = borderWidth > 0f && float.IsFinite(borderWidth) && borderColor.A > 0;
        bool coversBox = drawBorder && ImGuiMapping.BorderCoversBox(borderWidth, rect.Z, rect.W);

        // An opaque border that covers the box hides the fill, so the renderer skips the fill.
        if (color.A > 0 && !(coversBox && borderColor.A == 255))
        {
            Gui.PInvoke.ImDrawList_AddRectFilled(_list.Ptr, &min, &max, ImGuiMapping.ToImU32(color), rounding, flags);
        }

        if (!drawBorder)
        {
            return;
        }

        uint borderValue = ImGuiMapping.ToImU32(borderColor);
        if (coversBox)
        {
            Gui.PInvoke.ImDrawList_AddRectFilled(_list.Ptr, &min, &max, borderValue, rounding, flags);
            return;
        }

        // ImGui draws a stroke below 1 pixel as 1 pixel, so the inset uses a width of at least 1. It is then never below 0.
        float inset = ImGuiMapping.BorderInset(MathF.Max(borderWidth, 1f));
        float2 strokeMin = new(min.X + inset, min.Y + inset);
        float2 strokeMax = new(max.X - inset, max.Y - inset);
        float strokeRounding = ImGuiMapping.BorderRounding(rounding, borderWidth);
        ImDrawFlags strokeFlags = strokeRounding > 0f ? flags : ImDrawFlags.RoundCornersNone;
        Gui.PInvoke.ImDrawList_AddRect(
            _list.Ptr, &strokeMin, &strokeMax, borderValue, strokeRounding, strokeFlags, borderWidth);
    }

    /// <summary>Draws a texture in a rect, with a UV rect and a tint.</summary>
    /// <remarks>
    /// <para>
    /// The order of the checks: (1) the frame check, (2) the none handle draws nothing, (3) the texture of
    /// <see cref="Textures"/>, (4) the skip rules. The texture lookup comes before the skip rules, so a stale handle
    /// throws also for a tint alpha of 0 or a rect with no area. The method draws nothing for a tint alpha of 0, a
    /// rect with no area or with a <c>NaN</c> or infinite component, or a rect with a bound that is not finite (an
    /// overflow of <c>x + width</c>).
    /// </para>
    /// <para>
    /// The point (u0, v0) of <paramref name="uv"/> maps to the top-left corner of the rect, and (u1, v1) maps to the
    /// bottom-right corner. ImGui binds the texture for the image and then binds the texture of the list again.
    /// </para>
    /// <para>
    /// The image uses the corner rule of <see cref="Rect"/>. With no round corner it draws the plain image. Else it
    /// draws a rounded image with the largest radius for each corner above 0 (Decision 4).
    /// </para>
    /// </remarks>
    /// <param name="rect">The rect of the image: x, y, width, and height in viewport pixels.</param>
    /// <param name="texture">The texture. The none handle draws nothing.</param>
    /// <param name="uv">The texture coordinates: u0, v0, u1, and v1.</param>
    /// <param name="tint">The tint. Each texel color is multiplied by it.</param>
    /// <param name="radius">The corner radii in pixels.</param>
    /// <exception cref="InvalidOperationException">No <see cref="Begin"/> started the frame.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The texture service gave no texture with the id of <paramref name="texture"/>, or the texture is removed.
    /// </exception>
    public void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius)
    {
        ThrowIfNoFrame();
        if (texture.IsNone)
        {
            return;
        }

        ImTextureRef textureRef = Textures.GetTexture(texture);
        if (tint.A == 0 || !ImGuiMapping.HasArea(rect))
        {
            return;
        }

        float2 min = new(_origin.X + rect.X, _origin.Y + rect.Y);
        float2 max = new(_origin.X + (rect.X + rect.Z), _origin.Y + (rect.Y + rect.W));
        if (!ImGuiMapping.HasFiniteBounds(min, max))
        {
            return;
        }

        float2 uvMin = new(uv.X, uv.Y);
        float2 uvMax = new(uv.Z, uv.W);
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(radius, rect.Z, rect.W, out float rounding);
        if (rounding > 0f)
        {
            Gui.PInvoke.ImDrawList_AddImageRounded(
                _list.Ptr, textureRef, &min, &max, &uvMin, &uvMax, ImGuiMapping.ToImU32(tint), rounding, flags);
            return;
        }

        Gui.PInvoke.ImDrawList_AddImage(
            _list.Ptr, textureRef, &min, &max, &uvMin, &uvMax, ImGuiMapping.ToImU32(tint));
    }

    /// <summary>Draws UTF-8 text with an explicit font and size on the draw list.</summary>
    /// <remarks>
    /// <para>
    /// The order of the checks: (1) the frame check, (2) the font of <see cref="Fonts"/>, also for an empty text,
    /// (3) the size rule, (4) the skip rules. The method draws nothing for an empty span, an alpha of 0, a resolved
    /// size of 0 or less, or a position with a <c>NaN</c> or infinite component.
    /// </para>
    /// <para>
    /// A size of 0 or less, <c>NaN</c>, or an infinite size gives the size of the font (the size of its
    /// <c>AddFont</c> call). The renderer gives the font and the size to each call, so it uses no font stack of the
    /// ImGui frame and no global font scale.
    /// </para>
    /// <para>
    /// The wrap rule is the rule of <see cref="MeasureText"/>, so the same wrap width gives the same lines. The text
    /// bytes need no zero byte at the end.
    /// </para>
    /// </remarks>
    /// <param name="pos">The top-left corner of the first line in viewport pixels.</param>
    /// <param name="font">The font. Id 0 is the default font of <see cref="Fonts"/>.</param>
    /// <param name="size">The font size in pixels.</param>
    /// <param name="color">The text color.</param>
    /// <param name="utf8">The text as UTF-8 bytes.</param>
    /// <param name="wrapWidth">
    /// The width in viewport pixels at which the text wraps. A value of 0 or less, <c>NaN</c>, or an infinite value
    /// means no wrap.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="Begin"/> started the frame, or no ImGui context is current.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The font service has no font with the id of <paramref name="font"/>.</exception>
    public void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8, float wrapWidth)
    {
        ThrowIfNoFrame();
        ImFontPtr fontPtr = Fonts.GetFont(font);
        float fontSize = ImGuiMapping.FontSize(size, fontPtr.LegacySize);

        // The empty-span check comes before fixed: an empty span pins to a null pointer, and ImGui reads a null
        // text end as a zero-terminated text. A size of 0 draws at the frame font size, and a negative size stops
        // the process in ImGui.
        if (utf8.IsEmpty
            || color.A == 0
            || !IsDrawableSize(fontSize)
            || !float.IsFinite(pos.X)
            || !float.IsFinite(pos.Y))
        {
            return;
        }

        float2 origin = new(_origin.X + pos.X, _origin.Y + pos.Y);
        float wrap = ImGuiMapping.WrapWidth(wrapWidth);
        fixed (byte* begin = utf8)
        {
            Gui.PInvoke.ImDrawList_AddText_1(
                _list.Ptr,
                fontPtr.Ptr,
                fontSize,
                &origin,
                ImGuiMapping.ToImU32(color),
                begin,
                begin + utf8.Length,
                wrap,
                null);
        }
    }

    /// <summary>Measures UTF-8 text with an explicit font and size. The call draws nothing.</summary>
    /// <remarks>
    /// <para>
    /// The method needs no frame and no draw list. It works before the first ImGui frame, inside a frame, and between
    /// frames, and it gives the same result in each case. It uses no font stack of the ImGui frame and no global font
    /// scale.
    /// </para>
    /// <para>
    /// The order of the steps: (1) the context check, (2) the font of <see cref="Fonts"/>, also for an empty text,
    /// (3) the size rule, (4) the skip rules, (5) the measure.
    /// </para>
    /// <para>
    /// A size of 0 or less, <c>NaN</c>, or an infinite size gives the size of the font (the size of its
    /// <c>AddFont</c> call). When that size is also 0 or less, the method gives (0, 0) and makes no measure call.
    /// An empty span gives (0, font size) and makes no measure call.
    /// </para>
    /// <para>
    /// The width rounds up to a whole pixel, as <c>ImGui.CalcTextSize</c> does: <c>floor(width + 0.99999)</c> in
    /// single precision. At 256 and above, a whole width gives the width plus 1, as ImGui gives.
    /// </para>
    /// <para>
    /// The height is the count of lines times the font size. A line feed starts a new line. A line feed at the end of
    /// the text adds no line.
    /// </para>
    /// <para>
    /// With a wrap width, a line breaks after a blank or after one of the marks <c>. , ; ! ? "</c>. A word that is
    /// wider than the wrap width breaks inside the word, so the width stays at or below the wrap width. A wrap width
    /// below the width of one glyph is the exception. Then each line holds one glyph.
    /// </para>
    /// </remarks>
    /// <param name="font">The font. Id 0 is the default font of <see cref="Fonts"/>.</param>
    /// <param name="size">The font size in pixels.</param>
    /// <param name="utf8">The text as UTF-8 bytes.</param>
    /// <param name="wrapWidth">
    /// The width in viewport pixels at which the text wraps. A value of 0 or less, <c>NaN</c>, or an infinite value
    /// means no wrap.
    /// </param>
    /// <returns>The width and the height of the text in viewport pixels.</returns>
    /// <exception cref="InvalidOperationException">No ImGui context is current.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The font service has no font with the id of <paramref name="font"/>.</exception>
    public float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth)
    {
        if (Gui.GetCurrentContext().IsNull())
        {
            throw new InvalidOperationException(
                "No ImGui context is current. Call ImGui.CreateContext before the first measure.");
        }

        ImFontPtr fontPtr = Fonts.GetFont(font);
        float fontSize = ImGuiMapping.FontSize(size, fontPtr.LegacySize);

        // ImGui gives (0, 0) for a size of 0, and a negative size stops the process in ImGui.
        if (!IsDrawableSize(fontSize))
        {
            return default;
        }

        // The empty-span check comes before fixed: an empty span pins to a null pointer, and ImGui reads a null
        // text end as a zero-terminated text.
        if (utf8.IsEmpty)
        {
            return new float2(0f, fontSize);
        }

        float wrap = ImGuiMapping.WrapWidth(wrapWidth);
        float2 measured;
        fixed (byte* begin = utf8)
        {
            measured = Gui.PInvoke.ImFont_CalcTextSizeA(
                fontPtr.Ptr, fontSize, float.MaxValue, wrap, begin, begin + utf8.Length, null);
        }

        return new float2(ImGuiMapping.RoundUpWidth(measured.X), measured.Y);
    }

    /// <summary>Ends the frame. The method pops each clip that is still open, so the ImGui list stays balanced.</summary>
    /// <remarks>
    /// <para>
    /// A call with no open frame does nothing.
    /// </para>
    /// <para>
    /// The method pops only when the current ImGui context, the ImGui frame, and the background list are the same as
    /// at <see cref="Begin"/>. Else ImGui reset or freed the list, and the method only clears the frame state.
    /// </para>
    /// </remarks>
    public void End()
    {
        if (_list.IsNull())
        {
            _clipDepth = 0;
            return;
        }

        if (_clipDepth > 0 && IsTheFrameList())
        {
            PopOpenClips(_list, _clipDepth);
        }

        _list = default;
        _clipDepth = 0;
    }

    /// <summary>Tells whether the stored list is the background list of the current ImGui frame.</summary>
    /// <returns><see langword="true"/> when a pop on the stored list is safe.</returns>
    private bool IsTheFrameList()
    {
        // A context check comes first: the other calls read the current context and fail with no context.
        // The frame count check comes before the list call, because a first list access in a new frame resets it.
        return !Gui.GetCurrentContext().IsNull()
            && Gui.GetFrameCount() == _frameCount
            && Gui.GetBackgroundDrawList(Gui.GetMainViewport()).Ptr == _list.Ptr;
    }

    /// <summary>Pops the open clips of the renderer from a list.</summary>
    /// <remarks>
    /// The count is limited to the size of the clip stack of the list minus the viewport clip. A pop on an empty ImGui
    /// clip stack reads out of bounds, and a native assert does not stop it. The stack holds the viewport clip that
    /// ImGui pushes at the reset of the list. The renderer did not push it, so the method never pops it. This also
    /// holds when the game popped a clip of the list by hand.
    /// </remarks>
    /// <param name="list">The list.</param>
    /// <param name="depth">The count of open clips of the renderer.</param>
    private static void PopOpenClips(ImDrawListPtr list, int depth)
    {
        // Keep the viewport clip: it is not a clip of this renderer. The count is never below 0.
        int count = Math.Max(Math.Min(depth, list._ClipRectStack.Count - ViewportClipCount), 0);
        for (int i = 0; i < count; i++)
        {
            Gui.PInvoke.ImDrawList_PopClipRect(list.Ptr);
        }
    }

    /// <summary>Tells whether ImGui can draw and measure text at a resolved font size.</summary>
    /// <remarks>
    /// The size must be finite and above 0. ImGui measures a size of 0 as (0, 0), but its draw call then uses the font
    /// size of the frame. A negative size stops the process in ImGui. The size rule gives such a size only when the
    /// font itself has a size of 0 or less.
    /// </remarks>
    /// <param name="fontSize">The size of <see cref="ImGuiMapping.FontSize"/>.</param>
    /// <returns><see langword="true"/> when the size is safe for ImGui.</returns>
    private static bool IsDrawableSize(float fontSize)
    {
        return fontSize > 0f && float.IsFinite(fontSize);
    }

    /// <summary>Throws when no <see cref="Begin"/> started the frame.</summary>
    private void ThrowIfNoFrame()
    {
        if (_list.IsNull())
        {
            ThrowNoFrame();
        }
    }

    [DoesNotReturn]
    private static void ThrowNoFrame()
    {
        throw new InvalidOperationException("No frame is open. Call Begin before a draw call.");
    }

    [DoesNotReturn]
    private static void ThrowNoClipOnList()
    {
        throw new InvalidOperationException(
            "The clip stack of the draw list holds no clip of this renderer. Call Begin to start a frame.");
    }
}
