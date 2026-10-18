using System;
using System.Globalization;

namespace BrutalUi.Core;

/// <summary>
/// The unit of a <see cref="Length"/>.
/// </summary>
public enum LengthUnit : byte
{
    /// <summary>
    /// The length has no value. The layout uses the default of the property: auto for a size and a basis,
    /// no limit for a minimum or a maximum, 0 for a margin, a padding, and a border, and "not set" for an offset.
    /// </summary>
    Undefined = 0,

    /// <summary>The value is in pixels.</summary>
    Pixel,

    /// <summary>The value is a percent of the size of the owner. 50 means half of the owner size.</summary>
    Percent,

    /// <summary>The layout computes the length from the content and the flex rules.</summary>
    Auto,
}

/// <summary>
/// A length for a layout property: a value and a unit.
/// </summary>
/// <remarks>
/// <para>
/// <c>default(Length)</c> is <see cref="Undefined"/>. Thus a new <see cref="ComputedStyle"/> gives the default of each
/// property with no initial-value table. <see cref="Auto"/> is a separate value.
/// </para>
/// <para>
/// A <see cref="float"/> converts to a length in pixels: <c>style.Width = 120;</c>.
/// A <c>NaN</c> or an infinite value gives <see cref="Undefined"/>, as in Yoga.
/// </para>
/// </remarks>
public readonly struct Length : IEquatable<Length>
{
    private Length(float value, LengthUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Gets the value. The unit gives its meaning. The value is 0 for <see cref="LengthUnit.Undefined"/> and
    /// <see cref="LengthUnit.Auto"/>.
    /// </summary>
    public float Value { get; }

    /// <summary>Gets the unit of the value.</summary>
    public LengthUnit Unit { get; }

    /// <summary>Gets the length with no value. It is the default of the struct.</summary>
    public static Length Undefined => default;

    /// <summary>Gets the auto length.</summary>
    public static Length Auto => new(0f, LengthUnit.Auto);

    /// <summary>
    /// Creates a length in pixels.
    /// </summary>
    /// <param name="value">The value in pixels.</param>
    /// <returns>The length, or <see cref="Undefined"/> when <paramref name="value"/> is <c>NaN</c> or infinite.</returns>
    public static Length Px(float value)
    {
        return float.IsFinite(value) ? new Length(value, LengthUnit.Pixel) : default;
    }

    /// <summary>
    /// Creates a length as a percent of the owner size.
    /// </summary>
    /// <param name="value">The percent. 50 means half of the owner size.</param>
    /// <returns>The length, or <see cref="Undefined"/> when <paramref name="value"/> is <c>NaN</c> or infinite.</returns>
    public static Length Percent(float value)
    {
        return float.IsFinite(value) ? new Length(value, LengthUnit.Percent) : default;
    }

    /// <summary>Converts a value in pixels to a length. The conversion is the same as <see cref="Px(float)"/>.</summary>
    /// <param name="pixels">The value in pixels.</param>
    public static implicit operator Length(float pixels)
    {
        return Px(pixels);
    }

    /// <summary>Compares two lengths. Two lengths are equal when the unit and the value are equal.</summary>
    /// <param name="left">The first length.</param>
    /// <param name="right">The second length.</param>
    /// <returns><see langword="true"/> when the two lengths are equal.</returns>
    public static bool operator ==(Length left, Length right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two lengths. Two lengths are equal when the unit and the value are equal.</summary>
    /// <param name="left">The first length.</param>
    /// <param name="right">The second length.</param>
    /// <returns><see langword="true"/> when the two lengths are not equal.</returns>
    public static bool operator !=(Length left, Length right)
    {
        return !left.Equals(right);
    }

    /// <summary>Compares this length with another length.</summary>
    /// <param name="other">The other length.</param>
    /// <returns><see langword="true"/> when the unit and the value are equal.</returns>
    public bool Equals(Length other)
    {
        // The factories keep the value at 0 for Undefined and Auto, and never store NaN.
        return Unit == other.Unit && Value.Equals(other.Value);
    }

    /// <summary>Compares this length with an object.</summary>
    /// <param name="obj">The object.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an equal <see cref="Length"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Length other && Equals(other);
    }

    /// <summary>Gets a hash code from the unit and the value.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine((int)Unit, Value);
    }

    /// <summary>
    /// Gives the length as text: <c>"undefined"</c>, <c>"auto"</c>, <c>"10px"</c>, or <c>"50%"</c>.
    /// </summary>
    /// <remarks>The method allocates. Use it for test output and debug output only, not in the layout pass.</remarks>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        return Unit switch
        {
            LengthUnit.Pixel => Value.ToString(CultureInfo.InvariantCulture) + "px",
            LengthUnit.Percent => Value.ToString(CultureInfo.InvariantCulture) + "%",
            LengthUnit.Auto => "auto",
            _ => "undefined",
        };
    }

    /// <summary>
    /// Resolves the length to pixels against the size of the owner, as <c>resolve</c> of the Yoga style length does.
    /// </summary>
    /// <param name="ownerSize">The size of the owner on the same axis, in pixels. <c>NaN</c> means an undefined size.</param>
    /// <returns>
    /// The value in pixels. <c>NaN</c> for <see cref="LengthUnit.Undefined"/>, for <see cref="LengthUnit.Auto"/>,
    /// and for a percent of an undefined owner size.
    /// </returns>
    internal float Resolve(float ownerSize)
    {
        return Unit switch
        {
            LengthUnit.Pixel => Value,
            LengthUnit.Percent => Value * ownerSize * 0.01f,
            _ => float.NaN,
        };
    }
}
