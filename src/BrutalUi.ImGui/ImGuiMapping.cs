using System;
using System.Runtime.CompilerServices;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;

namespace BrutalUi.ImGui;

/// <summary>
/// The pure rules that turn the values of <see cref="IUiRenderer"/> into ImGui values. No rule calls ImGui.
/// </summary>
/// <remarks>Each rule allocates no managed memory.</remarks>
internal static class ImGuiMapping
{
    /// <summary>Gives the <c>ImU32</c> value of a color.</summary>
    /// <remarks>
    /// The method reads the 4 bytes of <see cref="Rgba8"/> as one 32-bit value. On a little-endian CPU, the value
    /// equals <c>IM_COL32(r, g, b, a)</c>.
    /// </remarks>
    /// <param name="color">The color.</param>
    /// <returns>The <c>ImU32</c> value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToImU32(in Rgba8 color)
    {
        return Unsafe.As<Rgba8, uint>(ref Unsafe.AsRef(in color));
    }

    /// <summary>Tells whether a rect has an area that a renderer can draw.</summary>
    /// <remarks>
    /// The four components are finite, and the width and the height are above 0. A <c>NaN</c> component fails each
    /// compare. An infinite width or height gives <see langword="false"/>, because ImGui does not cut the vertices at
    /// the clip rect.
    /// </remarks>
    /// <param name="rect">The rect: x, y, width, and height.</param>
    /// <returns><see langword="true"/> when the rect has an area.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasArea(in float4 rect)
    {
        return float.IsFinite(rect.X)
            && float.IsFinite(rect.Y)
            && rect.Z > 0f
            && rect.W > 0f
            && float.IsFinite(rect.Z)
            && float.IsFinite(rect.W);
    }

    /// <summary>Tells whether the four bounds of a rect in viewport pixels are finite.</summary>
    /// <remarks>
    /// The renderer adds the origin and the size to each position. Two finite values can add up to infinity, for
    /// example <c>3e38 + 3e38</c>, so a rect that passes <see cref="HasArea"/> can still give an infinite bound.
    /// </remarks>
    /// <param name="min">The top-left point.</param>
    /// <param name="max">The bottom-right point.</param>
    /// <returns><see langword="true"/> when each coordinate of the two points is finite.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFiniteBounds(in float2 min, in float2 max)
    {
        return float.IsFinite(min.X)
            && float.IsFinite(min.Y)
            && float.IsFinite(max.X)
            && float.IsFinite(max.Y);
    }

    /// <summary>Gives the font size in pixels for a text call.</summary>
    /// <remarks>
    /// A size above 0 that is finite stays. Each other size (0, a negative size, <c>NaN</c>, or an infinite size)
    /// gives <paramref name="defaultSize"/>.
    /// </remarks>
    /// <param name="size">The size of the call.</param>
    /// <param name="defaultSize">The size of the font: its <c>LegacySize</c>, the size of its <c>AddFont</c> call.</param>
    /// <returns>The size in pixels.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FontSize(float size, float defaultSize)
    {
        return size > 0f && float.IsFinite(size) ? size : defaultSize;
    }

    /// <summary>Gives the ImGui wrap width for a text call.</summary>
    /// <remarks>
    /// A width above 0 that is finite stays. Each other width (0, a negative width, <c>NaN</c>, or positive infinity)
    /// gives 0, the ImGui value for no wrap. An infinite width wraps no line, so 0 gives the same lines.
    /// </remarks>
    /// <param name="wrapWidth">The wrap width of the call, in pixels.</param>
    /// <returns>The wrap width for ImGui. The value 0 means no wrap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float WrapWidth(float wrapWidth)
    {
        return wrapWidth > 0f && float.IsFinite(wrapWidth) ? wrapWidth : 0f;
    }

    /// <summary>Rounds a text width up to a whole pixel, as <c>ImGui::CalcTextSize</c> does.</summary>
    /// <remarks>
    /// The rule is <c>floor(width + 0.99999)</c> in single precision, the same rule as ImGui. A width within 0.00001 of
    /// a whole number below it stays at that whole number. At 256 and above, the float sum rounds to the next whole
    /// number, so a whole width of 256 or more gives the width plus 1, as ImGui gives.
    /// </remarks>
    /// <param name="width">The width in pixels.</param>
    /// <returns>The width, rounded up.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RoundUpWidth(float width)
    {
        return MathF.Floor(width + 0.99999f);
    }

    /// <summary>Resolves the four corner radii of a box into one ImGui rounding value and a corner mask.</summary>
    /// <remarks>
    /// <para>
    /// The rules: (1) A radius of 0 or less, or <c>NaN</c>, becomes 0. (2) Each radius is limited to half the shorter
    /// side. (3) <paramref name="rounding"/> is the largest radius. (4) The flags name each corner with a radius above
    /// 0. (5) With no corner above 0, <paramref name="rounding"/> is 0 and the flags are
    /// <see cref="ImDrawFlags.RoundCornersNone"/>. ImGui reads flags with no corner bit as "all corners", so rule 5
    /// matters.
    /// </para>
    /// <para>
    /// ImGui rounds with one radius and a corner mask. The result is exact when the radii above 0 are equal. Mixed
    /// radii above 0 draw with the largest one.
    /// </para>
    /// </remarks>
    /// <param name="radius">The four radii in pixels.</param>
    /// <param name="width">The width of the box in pixels.</param>
    /// <param name="height">The height of the box in pixels.</param>
    /// <param name="rounding">The ImGui rounding in pixels. It is 0 when no corner is round.</param>
    /// <returns>The corner flags for ImGui.</returns>
    public static ImDrawFlags ResolveCorners(in Corners radius, float width, float height, out float rounding)
    {
        float half = MathF.Min(width, height) * 0.5f;
        float topLeft = LimitRadius(radius.TopLeft, half);
        float topRight = LimitRadius(radius.TopRight, half);
        float bottomRight = LimitRadius(radius.BottomRight, half);
        float bottomLeft = LimitRadius(radius.BottomLeft, half);

        rounding = MathF.Max(MathF.Max(topLeft, topRight), MathF.Max(bottomRight, bottomLeft));
        if (rounding <= 0f)
        {
            rounding = 0f;
            return ImDrawFlags.RoundCornersNone;
        }

        ImDrawFlags flags = ImDrawFlags.None;
        if (topLeft > 0f)
        {
            flags |= ImDrawFlags.RoundCornersTopLeft;
        }

        if (topRight > 0f)
        {
            flags |= ImDrawFlags.RoundCornersTopRight;
        }

        if (bottomRight > 0f)
        {
            flags |= ImDrawFlags.RoundCornersBottomRight;
        }

        if (bottomLeft > 0f)
        {
            flags |= ImDrawFlags.RoundCornersBottomLeft;
        }

        return flags;
    }

    /// <summary>Gives the inset of the stroke rect that puts a border inside the box.</summary>
    /// <remarks>
    /// <c>AddRect</c> insets its path by 0.5 and strokes on the center of the path. The inset
    /// <c>borderWidth / 2 - 0.5</c> puts the stroke on the span from 0 to <paramref name="borderWidth"/> inside the
    /// box.
    /// </remarks>
    /// <param name="borderWidth">The border width in pixels.</param>
    /// <returns>The inset for each side of the stroke rect, in pixels.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float BorderInset(float borderWidth)
    {
        return borderWidth * 0.5f - 0.5f;
    }

    /// <summary>Gives the rounding of the stroke path of a border.</summary>
    /// <remarks>
    /// The rule is <c>max(rounding - borderWidth / 2, 0)</c>. The outer edge of the stroke then follows the radius of
    /// the fill. A result of 0 calls for <see cref="ImDrawFlags.RoundCornersNone"/>.
    /// </remarks>
    /// <param name="rounding">The rounding of the fill in pixels.</param>
    /// <param name="borderWidth">The border width in pixels.</param>
    /// <returns>The rounding of the stroke path in pixels.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float BorderRounding(float rounding, float borderWidth)
    {
        return MathF.Max(rounding - borderWidth * 0.5f, 0f);
    }

    /// <summary>Tells whether a border fills the whole box.</summary>
    /// <remarks>
    /// The rule is <c>borderWidth * 2 &gt;= min(width, height)</c>. The renderer then draws one fill in the border
    /// color and no stroke.
    /// </remarks>
    /// <param name="borderWidth">The border width in pixels.</param>
    /// <param name="width">The width of the box in pixels.</param>
    /// <param name="height">The height of the box in pixels.</param>
    /// <returns><see langword="true"/> when the border covers the box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BorderCoversBox(float borderWidth, float width, float height)
    {
        return borderWidth * 2f >= MathF.Min(width, height);
    }

    private static float LimitRadius(float radius, float half)
    {
        return radius > 0f ? MathF.Min(radius, half) : 0f;
    }
}
