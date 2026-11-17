// Cache rule. The incremental pipeline caches every value below between steps.
// A model holds no ISymbol, no SyntaxNode, no Location, and no Compilation: those
// types pin a whole compilation in memory. A model holds plain strings and value
// types only. See implementation.md, "The Generator Pipeline".
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BrutalUi.Generators;

/// <summary>
/// A value-equal wrapper over <see cref="ImmutableArray{T}"/>. The pipeline compares
/// models by element. A bare <see cref="ImmutableArray{T}"/> compares by reference.
/// </summary>
/// <typeparam name="T">The element type. Elements must be value-equal.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
{
    private readonly ImmutableArray<T> _array;

    /// <summary>Wraps an immutable array.</summary>
    /// <param name="array">The elements. A default array reads as empty.</param>
    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array;
    }

    /// <summary>The empty instance.</summary>
    public static EquatableArray<T> Empty => default;

    /// <summary>The number of elements.</summary>
    public int Length => _array.IsDefault ? 0 : _array.Length;

    /// <summary>The element at the index.</summary>
    /// <param name="index">The zero-based index.</param>
    public T this[int index] => _array[index];

    /// <summary>Compares the elements, in order, with <see cref="EqualityComparer{T}"/>.Default.</summary>
    /// <param name="other">The array to compare with.</param>
    /// <returns><see langword="true"/> when each element equals its pair. Otherwise <see langword="false"/>.</returns>
    public bool Equals(EquatableArray<T> other)
    {
        var length = Length;
        if (length != other.Length)
        {
            return false;
        }

        var left = _array.IsDefault ? ImmutableArray<T>.Empty : _array;
        var right = other._array.IsDefault ? ImmutableArray<T>.Empty : other._array;
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < length; i++)
        {
            if (!comparer.Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    /// <summary>Makes the hash by hand. <c>System.HashCode</c> does not exist in <c>netstandard2.0</c>.</summary>
    /// <returns>The hash of the element sequence.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            var length = Length;
            hash = (hash * 31) + length;
            var left = _array.IsDefault ? ImmutableArray<T>.Empty : _array;
            for (var i = 0; i < length; i++)
            {
                var element = left[i];
                hash = (hash * 31) + (element is null ? 0 : element.GetHashCode());
            }

            return hash;
        }
    }

    /// <summary>Enumerates the elements.</summary>
    /// <returns>An enumerator over the elements.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _array.IsDefault ? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator() : ((IEnumerable<T>)_array).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// The kind of read that the writer emits for one set row. The value maps to one
/// <c>AttributeContext</c> read method.
/// </summary>
internal enum ReadKind
{
    /// <summary>Text, read with <c>context.Value</c>.</summary>
    Text,

    /// <summary>A number, read with <c>TryReadNumber</c>.</summary>
    Number,

    /// <summary>A boolean, read with <c>TryReadBool</c>.</summary>
    Bool,

    /// <summary>Two numbers, read with <c>TryReadNumbers</c>.</summary>
    Numbers2,

    /// <summary>Four numbers, read with <c>TryReadNumbers</c>.</summary>
    Numbers4,

    /// <summary>A color, read with <c>TryReadColor</c>.</summary>
    Color,

    /// <summary>A texture name, read with <c>TryReadTexture</c>.</summary>
    Texture,

    /// <summary>An integer, read with <c>TryReadInteger</c>.</summary>
    Integer,

    /// <summary>An enum value, read with <c>TryReadKeyword</c> and written by a <c>switch</c> on the index.</summary>
    Keyword,
}

/// <summary>One keyword of an enum row: the text the game writes and the member it gives.</summary>
/// <param name="Keyword">The kebab-case keyword, for example <c>round-dial</c>.</param>
/// <param name="MemberName">The global name of the enum member, with <c>global::</c>.</param>
internal sealed record KeywordModel(string Keyword, string MemberName);

/// <summary>One create row of one factory: a name the factory reads, and whether the element needs it.</summary>
/// <param name="Name">The attribute name, from <c>[TemplateCreateRow]</c>.</param>
/// <param name="IsRequired"><see langword="true"/> for <c>AttributeRowKind.CreateRequired</c>.</param>
internal sealed record CreateRowModel(string Name, bool IsRequired);

/// <summary>One set row of one element model.</summary>
/// <param name="RowName">The row name: kebab-case, or the <c>[TemplateRowName]</c> value.</param>
/// <param name="PropertyName">The CLR property name that the setter assigns.</param>
/// <param name="Kind">The read kind of the value.</param>
/// <param name="EnumType">The global name of the enum type, with <c>global::</c>. Empty for another kind.</param>
/// <param name="Keywords">The keywords of an enum row, in declaration order. Empty for another kind.</param>
internal sealed record SetRowModel(
    string RowName,
    string PropertyName,
    ReadKind Kind,
    string EnumType = "",
    EquatableArray<KeywordModel> Keywords = default);

/// <summary>One marked element type, cached as a value.</summary>
/// <param name="XmlName">The XML name of the element.</param>
/// <param name="IsLeaf"><see langword="true"/> when the element rejects children.</param>
/// <param name="TargetType">The global name of the target type, with <c>global::</c>.</param>
/// <param name="FactoryCall">The global call of the factory, without arguments, for example
/// <c>global::Ns.Meter.Create</c>. <see langword="null"/> when the generator writes the no-parameter constructor
/// call instead.</param>
/// <param name="CreateRows">The create rows of the factory, in source order. Empty with no factory.</param>
/// <param name="SetRows">The set rows, in output order.</param>
internal sealed record ElementModel(
    string XmlName,
    bool IsLeaf,
    string TargetType,
    string? FactoryCall,
    EquatableArray<CreateRowModel> CreateRows,
    EquatableArray<SetRowModel> SetRows);
