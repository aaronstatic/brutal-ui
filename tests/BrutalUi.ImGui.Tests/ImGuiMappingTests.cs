using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.ImGui.Tests;

/// <summary>Tests of the pure rules of <see cref="ImGuiMapping"/>. No test calls ImGui.</summary>
public sealed class ImGuiMappingTests
{
    /// <summary>The color reads as <c>IM_COL32(r, g, b, a)</c>: R in the low byte and A in the high byte.</summary>
    [Fact]
    public void ToImU32GivesTheImCol32Value()
    {
        Assert.SkipUnless(BitConverter.IsLittleEndian, "The ImU32 value check needs a little-endian CPU.");

        Assert.Equal(0x04030201u, ImGuiMapping.ToImU32(new Rgba8(1, 2, 3, 4)));
        Assert.Equal(0xFF1E140Au, ImGuiMapping.ToImU32(new Rgba8(10, 20, 30)));
        Assert.Equal(0u, ImGuiMapping.ToImU32(Rgba8.Transparent));
    }

    /// <summary>A rect with a finite position and a width and a height above 0 has an area.</summary>
    [Theory]
    [InlineData(0f, 0f, 1f, 1f)]
    [InlineData(-50f, -20f, 200f, 100f)]
    [InlineData(10f, 10f, 0.5f, 0.25f)]
    public void HasAreaIsTrueForAPositiveSize(float x, float y, float width, float height)
    {
        Assert.True(ImGuiMapping.HasArea(new float4(x, y, width, height)));
    }

    /// <summary>Finite bounds give true.</summary>
    [Theory]
    [InlineData(0f, 0f, 0f, 0f)]
    [InlineData(10f, 10f, 50f, 30f)]
    [InlineData(-5f, -5f, 3e38f, 3e38f)]
    public void HasFiniteBoundsIsTrueForFiniteBounds(float minX, float minY, float maxX, float maxY)
    {
        Assert.True(ImGuiMapping.HasFiniteBounds(new float2(minX, minY), new float2(maxX, maxY)));
    }

    /// <summary>A <c>NaN</c> or an infinite component in the min point or in the max point gives false.</summary>
    [Theory]
    [InlineData(float.NaN, 0f, 10f, 10f)]
    [InlineData(0f, float.NaN, 10f, 10f)]
    [InlineData(0f, 0f, float.NaN, 10f)]
    [InlineData(0f, 0f, 10f, float.NaN)]
    [InlineData(float.PositiveInfinity, 0f, 10f, 10f)]
    [InlineData(0f, float.NegativeInfinity, 10f, 10f)]
    [InlineData(0f, 0f, float.PositiveInfinity, 10f)]
    [InlineData(0f, 0f, 10f, float.NegativeInfinity)]
    public void HasFiniteBoundsIsFalseForANonFiniteComponent(float minX, float minY, float maxX, float maxY)
    {
        Assert.False(ImGuiMapping.HasFiniteBounds(new float2(minX, minY), new float2(maxX, maxY)));
    }

    /// <summary>
    /// A width or a height of 0, a negative size, a <c>NaN</c> in each component, and an infinite component give no
    /// area.
    /// </summary>
    [Theory]
    [InlineData(10f, 10f, 0f, 20f)]
    [InlineData(10f, 10f, 20f, 0f)]
    [InlineData(10f, 10f, -5f, 20f)]
    [InlineData(10f, 10f, 20f, -5f)]
    [InlineData(float.NaN, 10f, 20f, 20f)]
    [InlineData(10f, float.NaN, 20f, 20f)]
    [InlineData(10f, 10f, float.NaN, 20f)]
    [InlineData(10f, 10f, 20f, float.NaN)]
    [InlineData(float.PositiveInfinity, 10f, 20f, 20f)]
    [InlineData(10f, float.NegativeInfinity, 20f, 20f)]
    [InlineData(10f, 10f, float.PositiveInfinity, 20f)]
    [InlineData(10f, 10f, 20f, float.PositiveInfinity)]
    public void HasAreaIsFalseForNoArea(float x, float y, float width, float height)
    {
        Assert.False(ImGuiMapping.HasArea(new float4(x, y, width, height)));
    }

    /// <summary>A size of 0, a negative size, <c>NaN</c>, and an infinite size give the default size.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void FontSizeGivesTheDefaultForNoSize(float size)
    {
        Assert.Equal(13f, ImGuiMapping.FontSize(size, 13f));
    }

    /// <summary>A size above 0 that is finite stays.</summary>
    [Theory]
    [InlineData(20f)]
    [InlineData(0.5f)]
    [InlineData(13f)]
    public void FontSizeKeepsAPositiveSize(float size)
    {
        Assert.Equal(size, ImGuiMapping.FontSize(size, 13f));
    }

    /// <summary>A wrap width of 0, a negative width, <c>NaN</c>, and positive infinity give 0 (no wrap).</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void WrapWidthGivesZeroForNoWrap(float wrapWidth)
    {
        Assert.Equal(0f, ImGuiMapping.WrapWidth(wrapWidth));
    }

    /// <summary>A wrap width above 0 that is finite stays.</summary>
    [Theory]
    [InlineData(70f)]
    [InlineData(0.25f)]
    public void WrapWidthKeepsAPositiveWidth(float wrapWidth)
    {
        Assert.Equal(wrapWidth, ImGuiMapping.WrapWidth(wrapWidth));
    }

    /// <summary>A whole width stays, and a width with a fraction goes up to the next whole pixel.</summary>
    [Theory]
    [InlineData(112f, 112f)]
    [InlineData(112.2f, 113f)]
    [InlineData(0f, 0f)]
    [InlineData(0.5f, 1f)]
    [InlineData(255f, 255f)]
    public void RoundUpWidthGivesTheNextWholePixel(float width, float expected)
    {
        Assert.Equal(expected, ImGuiMapping.RoundUpWidth(width));
    }

    /// <summary>
    /// At 256 and above, the single-precision sum of the ImGui rule rounds to the next whole number. The rule then
    /// gives the width plus 1, as <c>ImGui::CalcTextSize</c> gives.
    /// </summary>
    [Fact]
    public void RoundUpWidthMatchesImGuiForAWideWholeWidth()
    {
        Assert.Equal(257f, ImGuiMapping.RoundUpWidth(256f));
        Assert.Equal(128f, ImGuiMapping.RoundUpWidth(128f));
    }

    private const ImDrawFlags AllCorners =
        ImDrawFlags.RoundCornersTopLeft
        | ImDrawFlags.RoundCornersTopRight
        | ImDrawFlags.RoundCornersBottomLeft
        | ImDrawFlags.RoundCornersBottomRight;

    /// <summary>Four radii of 0 give no rounding and the none flag, because ImGui reads no corner bit as all corners.</summary>
    [Fact]
    public void ResolveCornersGivesNoneForZeroRadii()
    {
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(default, 100f, 100f, out float rounding);

        Assert.Equal(0f, rounding);
        Assert.Equal(ImDrawFlags.RoundCornersNone, flags);
    }

    /// <summary>A <c>NaN</c> radius and a negative radius count as 0.</summary>
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(-3f)]
    [InlineData(float.NegativeInfinity)]
    public void ResolveCornersTreatsAnInvalidRadiusAsZero(float radius)
    {
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(Corners.All(radius), 100f, 100f, out float rounding);

        Assert.Equal(0f, rounding);
        Assert.Equal(ImDrawFlags.RoundCornersNone, flags);
    }

    /// <summary>Four equal radii give that radius with the flags of all four corners.</summary>
    [Fact]
    public void ResolveCornersNamesAllFourCorners()
    {
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(Corners.All(4f), 100f, 100f, out float rounding);

        Assert.Equal(4f, rounding);
        Assert.Equal(AllCorners, flags);
    }

    /// <summary>Two corners with a radius give the flags of those two corners.</summary>
    [Fact]
    public void ResolveCornersNamesTheTopCorners()
    {
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(new Corners(4f, 4f, 0f, 0f), 100f, 100f, out float rounding);

        Assert.Equal(4f, rounding);
        Assert.Equal(ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersTopRight, flags);
    }

    /// <summary>One corner with a radius gives the flag of that corner only.</summary>
    [Fact]
    public void ResolveCornersNamesTheBottomCorners()
    {
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(new Corners(0f, 0f, 5f, 5f), 100f, 100f, out float rounding);
        Assert.Equal(5f, rounding);
        Assert.Equal(ImDrawFlags.RoundCornersBottomRight | ImDrawFlags.RoundCornersBottomLeft, flags);

        flags = ImGuiMapping.ResolveCorners(new Corners(0f, 0f, 0f, 3f), 100f, 100f, out rounding);
        Assert.Equal(3f, rounding);
        Assert.Equal(ImDrawFlags.RoundCornersBottomLeft, flags);
    }

    /// <summary>Mixed radii above 0 give the largest radius with the flags of the corners above 0 (Decision 4).</summary>
    [Fact]
    public void ResolveCornersTakesTheLargestRadius()
    {
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(new Corners(2f, 6f, 0f, 0f), 100f, 100f, out float rounding);

        Assert.Equal(6f, rounding);
        Assert.Equal(ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersTopRight, flags);
    }

    /// <summary>A radius above half the shorter side goes down to half the shorter side.</summary>
    [Fact]
    public void ResolveCornersLimitsTheRadiusToHalfTheShorterSide()
    {
        ImDrawFlags flags = ImGuiMapping.ResolveCorners(Corners.All(100f), 40f, 20f, out float rounding);

        Assert.Equal(10f, rounding);
        Assert.Equal(AllCorners, flags);

        flags = ImGuiMapping.ResolveCorners(Corners.All(float.PositiveInfinity), 40f, 20f, out rounding);
        Assert.Equal(10f, rounding);
        Assert.Equal(AllCorners, flags);
    }

    /// <summary>The inset puts the stroke on the span from 0 to the border width inside the box.</summary>
    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(2f, 0.5f)]
    [InlineData(4f, 1.5f)]
    public void BorderInsetGivesHalfTheWidthMinusHalf(float borderWidth, float expected)
    {
        Assert.Equal(expected, ImGuiMapping.BorderInset(borderWidth));
    }

    /// <summary>The stroke rounding is the fill rounding minus half the border width, and never below 0.</summary>
    [Theory]
    [InlineData(8f, 2f, 7f)]
    [InlineData(1f, 4f, 0f)]
    [InlineData(0f, 2f, 0f)]
    public void BorderRoundingFollowsTheFillRadius(float rounding, float borderWidth, float expected)
    {
        Assert.Equal(expected, ImGuiMapping.BorderRounding(rounding, borderWidth));
    }

    /// <summary>A border of half the shorter side or more covers the box.</summary>
    [Theory]
    [InlineData(2f, 4f, 10f, true)]
    [InlineData(1f, 4f, 10f, false)]
    [InlineData(3f, 4f, 10f, true)]
    [InlineData(5f, 10f, 4f, true)]
    public void BorderCoversBoxForHalfTheShorterSide(float borderWidth, float width, float height, bool expected)
    {
        Assert.Equal(expected, ImGuiMapping.BorderCoversBox(borderWidth, width, height));
    }
}
