using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace BrutalUi.Core;

/// <summary>
/// A color with four bytes in the order red, green, blue, and alpha.
/// </summary>
/// <remarks>
/// <para>
/// The memory layout is 4 bytes: R at offset 0, G at offset 1, B at offset 2, and A at offset 3.
/// On a little-endian CPU, this layout is the same as the <c>ImU32</c> value of <c>IM_COL32(r, g, b, a)</c>.
/// A renderer can read the color as a 32-bit value with no conversion.
/// </para>
/// <para>
/// <c>default(Rgba8)</c> is <see cref="Transparent"/>: all four bytes are 0.
/// The color is not premultiplied. The alpha does not change the red, the green, and the blue bytes.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Rgba8 : IEquatable<Rgba8>
{
    /// <summary>The red byte, from 0 to 255. The default is 0.</summary>
    public readonly byte R;

    /// <summary>The green byte, from 0 to 255. The default is 0.</summary>
    public readonly byte G;

    /// <summary>The blue byte, from 0 to 255. The default is 0.</summary>
    public readonly byte B;

    /// <summary>The alpha byte, from 0 (transparent) to 255 (opaque). The default is 0.</summary>
    public readonly byte A;

    /// <summary>
    /// Creates a color from four bytes.
    /// </summary>
    /// <param name="r">The red byte.</param>
    /// <param name="g">The green byte.</param>
    /// <param name="b">The blue byte.</param>
    /// <param name="a">The alpha byte. The default is 255 (opaque).</param>
    public Rgba8(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>Gets the transparent color. All four bytes are 0. It is the default value.</summary>
    public static Rgba8 Transparent => default;

    /// <summary>Compares two colors.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when the four bytes are equal.</returns>
    public static bool operator ==(Rgba8 left, Rgba8 right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two colors.</summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns><see langword="true"/> when one or more bytes are not equal.</returns>
    public static bool operator !=(Rgba8 left, Rgba8 right)
    {
        return !left.Equals(right);
    }

    /// <summary>Compares this color with another color.</summary>
    /// <param name="other">The other color.</param>
    /// <returns><see langword="true"/> when the four bytes are equal.</returns>
    public bool Equals(Rgba8 other)
    {
        return Pack() == other.Pack();
    }

    /// <summary>Compares this color with an object.</summary>
    /// <param name="obj">The object.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an equal <see cref="Rgba8"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Rgba8 other && Equals(other);
    }

    /// <summary>Gets a hash code from the four bytes.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return (int)Pack();
    }

    /// <summary>
    /// Gives the color as text in the form <c>#RRGGBBAA</c>, with uppercase hex digits.
    /// </summary>
    /// <remarks>The method allocates. Use it for test output and debug output only, not in the draw pass.</remarks>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        return "#"
            + R.ToString("X2", CultureInfo.InvariantCulture)
            + G.ToString("X2", CultureInfo.InvariantCulture)
            + B.ToString("X2", CultureInfo.InvariantCulture)
            + A.ToString("X2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gives the color with the alpha byte scaled by a factor. The red, green, and blue bytes do not change.
    /// </summary>
    /// <param name="factor">The factor, from 0 to 1. A factor of 1 keeps the alpha exactly.</param>
    /// <returns>The scaled color. The alpha is rounded to the nearest byte.</returns>
    internal Rgba8 ScaleAlpha(float factor)
    {
        return new Rgba8(R, G, B, (byte)(A * factor + 0.5f));
    }

    /// <summary>Packs the four bytes into one value: R in the low byte and A in the high byte.</summary>
    /// <returns>The packed value.</returns>
    private uint Pack()
    {
        return R | ((uint)G << 8) | ((uint)B << 16) | ((uint)A << 24);
    }
}
