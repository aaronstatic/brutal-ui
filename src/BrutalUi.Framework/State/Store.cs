using System;
using System.Collections.Generic;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// A store that holds one value of type <typeparamref name="T"/> and a version counter.
/// </summary>
/// <remarks>
/// <para>
/// The pull model: the store raises <see cref="Version"/> by one for each change. A reader compares the version with
/// the version that it applied last, and it reads <see cref="Value"/> when the two differ. The store holds no event, no
/// subscriber list, and no reference to a reader. Many writes before one read give one apply.
/// </para>
/// <para>
/// The equality rule: <see cref="Set"/> compares the new value with the old value through
/// <see cref="EqualityComparer{T}.Default"/>. An equal value changes nothing. For a struct <typeparamref name="T"/>,
/// use a type with <see cref="IEquatable{T}"/>. A <c>record struct</c> has it. A struct with no
/// <see cref="IEquatable{T}"/> boxes in <see cref="Set"/>. For a class with no <c>Equals</c> override, the rule is the
/// reference: a new instance with the same fields is a change. When you change a reference value in place and then set
/// the same reference, the store sees no change.
/// </para>
/// <para>
/// The version rule: a new store starts at version 1, so a reader that starts at 0 applies at its first read. Each
/// change raises the version by one. A return to an old value is a change.
/// </para>
/// <para>
/// The thread rule: the store runs on the UI thread. It uses no lock.
/// </para>
/// <para>
/// The store keeps no reference to a reader. The store replaces <c>INotifyBindablePropertyChanged</c> of mosaic-ui.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class Store<T> : IBindingSource
{
    private T _value;
    private ulong _version;

    /// <summary>
    /// Creates a store with a start value. The version is 1.
    /// </summary>
    /// <param name="info">The create-info with the start value.</param>
    public Store(in CreateInfo info)
    {
        _value = info.Initial;
        _version = 1;
    }

    /// <summary>
    /// Gets a copy of the current value.
    /// </summary>
    public T Value => _value;

    /// <summary>
    /// Gets the version of the store. It starts at 1 and rises by one for each change.
    /// </summary>
    public ulong Version => _version;

    /// <summary>
    /// Sets the value. A value that is not equal to the current value replaces it and raises <see cref="Version"/> by
    /// one.
    /// </summary>
    /// <remarks>
    /// An equal value changes nothing. The store also keeps the old reference, and the version stays.
    /// </remarks>
    /// <param name="value">The new value.</param>
    public void Set(in T value)
    {
        if (EqualityComparer<T>.Default.Equals(_value, value))
        {
            return;
        }

        _value = value;
        _version++;
    }

    /// <summary>
    /// The create-info of a <see cref="Store{T}"/>.
    /// </summary>
    public struct CreateInfo
    {
        /// <summary>
        /// The start value of the store.
        /// </summary>
        public required T Initial;
    }
}
