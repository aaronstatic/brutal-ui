using System;
using System.Runtime.CompilerServices;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the render value types: <see cref="Rgba8"/>, <see cref="Corners"/>, <see cref="FontHandle"/>, and
/// <see cref="TextureHandle"/>.
/// </summary>
public sealed class RenderValueTests
{
    /// <summary>A color is 4 bytes.</summary>
    [Fact]
    public void Rgba8IsFourBytes()
    {
        Assert.Equal(4, Unsafe.SizeOf<Rgba8>());
    }

    /// <summary>On a little-endian CPU, the bytes read as the <c>IM_COL32</c> value: R in the low byte.</summary>
    [Fact]
    public void Rgba8MatchesTheImCol32Layout()
    {
        Assert.SkipUnless(BitConverter.IsLittleEndian, "The layout check needs a little-endian CPU.");

        Rgba8 color = new(1, 2, 3, 4);
        uint packed = Unsafe.As<Rgba8, uint>(ref color);

        Assert.Equal(0x04030201u, packed);
    }

    /// <summary>The default color is transparent: all four bytes are 0.</summary>
    [Fact]
    public void DefaultRgba8IsTransparent()
    {
        Rgba8 color = default;

        Assert.Equal(Rgba8.Transparent, color);
        Assert.True(color == Rgba8.Transparent);
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(0, color.A);
    }

    /// <summary>The constructor gives an alpha of 255 when the caller gives no alpha.</summary>
    [Fact]
    public void Rgba8ConstructorDefaultsToOpaque()
    {
        Rgba8 color = new(10, 20, 30);

        Assert.Equal(10, color.R);
        Assert.Equal(20, color.G);
        Assert.Equal(30, color.B);
        Assert.Equal(255, color.A);
    }

    /// <summary>The text form is <c>#RRGGBBAA</c> with uppercase hex digits.</summary>
    [Fact]
    public void Rgba8ToStringGivesUppercaseHex()
    {
        Assert.Equal("#0A141EFF", new Rgba8(10, 20, 30, 255).ToString());
    }

    /// <summary>Two colors are equal only when all four bytes are equal.</summary>
    [Fact]
    public void Rgba8EqualityComparesAllFourBytes()
    {
        Rgba8 a = new(1, 2, 3, 4);
        Rgba8 same = new(1, 2, 3, 4);
        Rgba8 otherAlpha = new(1, 2, 3, 5);

        Assert.True(a == same);
        Assert.False(a != same);
        Assert.True(a.Equals(same));
        Assert.True(a.Equals((object)same));
        Assert.Equal(a.GetHashCode(), same.GetHashCode());
        Assert.True(a != otherAlpha);
        Assert.False(a.Equals(otherAlpha));
        Assert.False(a.Equals("#01020304"));
    }

    /// <summary><see cref="Corners.All(float)"/> sets the four radii, and the constructor keeps the clockwise order.</summary>
    [Fact]
    public void CornersSetsFourFields()
    {
        Corners all = Corners.All(4f);
        Assert.Equal(4f, all.TopLeft);
        Assert.Equal(4f, all.TopRight);
        Assert.Equal(4f, all.BottomRight);
        Assert.Equal(4f, all.BottomLeft);

        Corners each = new(1f, 2f, 3f, 4f);
        Assert.Equal(1f, each.TopLeft);
        Assert.Equal(2f, each.TopRight);
        Assert.Equal(3f, each.BottomRight);
        Assert.Equal(4f, each.BottomLeft);

        Corners none = default;
        Assert.Equal(0f, none.TopLeft);
        Assert.Equal(0f, none.TopRight);
        Assert.Equal(0f, none.BottomRight);
        Assert.Equal(0f, none.BottomLeft);
    }

    /// <summary>Two font handles are equal when the ids are equal. The default handle has id 0, the default font.</summary>
    [Fact]
    public void FontHandleComparesTheId()
    {
        FontHandle a = new(3);
        FontHandle same = new(3);
        FontHandle other = new(4);

        Assert.True(a == same);
        Assert.False(a != same);
        Assert.True(a.Equals((object)same));
        Assert.Equal(a.GetHashCode(), same.GetHashCode());
        Assert.True(a != other);
        Assert.False(a.Equals(other));
        Assert.False(a.Equals((object)new TextureHandle(3)));

        Assert.Equal(0, default(FontHandle).Id);
        Assert.Equal(new FontHandle(0), default);
    }

    /// <summary>Two texture handles are equal when the ids are equal. Id 0 (the default) is no texture.</summary>
    [Fact]
    public void TextureHandleComparesTheIdAndZeroIsNone()
    {
        TextureHandle a = new(7);
        TextureHandle same = new(7);
        TextureHandle other = new(8);

        Assert.True(a == same);
        Assert.False(a != same);
        Assert.True(a.Equals((object)same));
        Assert.Equal(a.GetHashCode(), same.GetHashCode());
        Assert.True(a != other);
        Assert.False(a.Equals(other));

        Assert.True(default(TextureHandle).IsNone);
        Assert.True(new TextureHandle(0).IsNone);
        Assert.False(a.IsNone);
    }

    /// <summary>The two-argument constructor keeps the id and the size.</summary>
    [Fact]
    public void TextureHandleSizeReadsBack()
    {
        TextureHandle handle = new(4660, new int2(64, 32));

        Assert.Equal(4660, handle.Id);
        Assert.Equal(64, handle.Size.X);
        Assert.Equal(32, handle.Size.Y);
        Assert.False(handle.IsNone);
    }

    /// <summary>The one-argument constructor and the default handle give the size (0, 0).</summary>
    [Fact]
    public void TextureHandleWithNoSizeHasSizeZero()
    {
        TextureHandle handle = new(7);

        Assert.Equal(0, handle.Size.X);
        Assert.Equal(0, handle.Size.Y);
        Assert.Equal(0, default(TextureHandle).Size.X);
        Assert.Equal(0, default(TextureHandle).Size.Y);
        Assert.True(new TextureHandle(0, new int2(0, 0)).IsNone);
    }

    /// <summary>Equality and the hash code use the id only. Two handles with the same id and another size are equal.</summary>
    [Fact]
    public void TextureHandleEqualityIgnoresTheSize()
    {
        TextureHandle a = new(7, new int2(64, 32));
        TextureHandle otherSize = new(7, new int2(16, 16));
        TextureHandle noSize = new(7);
        TextureHandle otherId = new(8, new int2(64, 32));

        Assert.True(a == otherSize);
        Assert.False(a != otherSize);
        Assert.True(a.Equals(otherSize));
        Assert.True(a.Equals((object)otherSize));
        Assert.Equal(a.GetHashCode(), otherSize.GetHashCode());
        Assert.True(a == noSize);
        Assert.Equal(a.GetHashCode(), noSize.GetHashCode());
        Assert.True(a != otherId);
        Assert.False(a.Equals(otherId));
    }

    /// <summary>The default style and a new element have an opacity of 1, so they are visible.</summary>
    [Fact]
    public void DefaultOpacityIsOne()
    {
        Assert.Equal(1f, default(ComputedStyle).Opacity);
        Assert.Equal(1f, new Element().Style.Opacity);
    }

    /// <summary>A set opacity reads back exactly, also 0.</summary>
    [Fact]
    public void OpacityReadsBackTheSetValue()
    {
        Element element = new();

        element.Inline.Opacity = 0.3f;
        Assert.Equal(0.3f, element.Inline.Opacity);

        element.Inline.Opacity = 0f;
        Assert.Equal(0f, element.Inline.Opacity);
    }

    /// <summary>A factor of 1 keeps the alpha, and the other bytes never change.</summary>
    [Fact]
    public void ScaleAlphaOfOneKeepsTheAlpha()
    {
        Rgba8 color = new(10, 20, 30, 77);

        Assert.Equal(color, color.ScaleAlpha(1f));
        Assert.Equal(new Rgba8(1, 2, 3, 255), new Rgba8(1, 2, 3, 255).ScaleAlpha(1f));
    }

    /// <summary>A factor of 0.5 rounds 127.5 up to 128. A factor of 0 gives an alpha of 0.</summary>
    [Fact]
    public void ScaleAlphaRoundsToTheNearestByte()
    {
        Rgba8 color = new(10, 20, 30, 255);

        Assert.Equal(new Rgba8(10, 20, 30, 128), color.ScaleAlpha(0.5f));
        Assert.Equal(new Rgba8(10, 20, 30, 0), color.ScaleAlpha(0f));
    }
}
