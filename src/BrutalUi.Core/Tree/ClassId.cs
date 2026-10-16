using System;

namespace BrutalUi.Core;

/// <summary>
/// An interned class name. The hot path compares ids and never compares strings.
/// </summary>
/// <remarks>
/// Get an id from <see cref="ClassName.Intern(string)"/>. The default value is <see cref="None"/>.
/// The numbers of the ids depend on the order of the first interns. Do not store them between runs.
/// </remarks>
public readonly struct ClassId : IEquatable<ClassId>
{
    /// <summary>Creates an id with a number. Only the class table calls it.</summary>
    /// <param name="value">The number of the id.</param>
    internal ClassId(int value)
    {
        Value = value;
    }

    /// <summary>Gets the id that stands for no class. It is the default value.</summary>
    public static ClassId None => default;

    /// <summary>Gets the number of the id. The number 0 is <see cref="None"/>.</summary>
    public int Value { get; }

    /// <summary>Gets a value that shows whether the id is <see cref="None"/>.</summary>
    public bool IsNone => Value == 0;

    /// <summary>Compares two ids.</summary>
    /// <param name="left">The first id.</param>
    /// <param name="right">The second id.</param>
    /// <returns><see langword="true"/> when both ids have the same number.</returns>
    public static bool operator ==(ClassId left, ClassId right) => left.Value == right.Value;

    /// <summary>Compares two ids.</summary>
    /// <param name="left">The first id.</param>
    /// <param name="right">The second id.</param>
    /// <returns><see langword="true"/> when the ids have different numbers.</returns>
    public static bool operator !=(ClassId left, ClassId right) => left.Value != right.Value;

    /// <inheritdoc />
    public bool Equals(ClassId other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ClassId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value;

    /// <summary>Returns the class name of the id, or <c>(none)</c> for <see cref="None"/>.</summary>
    /// <returns>The class name.</returns>
    public override string ToString() => IsNone ? "(none)" : ClassName.GetName(this);
}
