using System;
using System.Diagnostics.CodeAnalysis;

namespace BrutalUi.Framework;

/// <summary>
/// A reader of one slice of a <see cref="Store{T}"/>. The reader pulls each change one time.
/// </summary>
/// <remarks>
/// <para>
/// The subscription keeps the store version that it applied last. <see cref="TryGetChanged"/> compares that version with
/// the store version. The first read applies, because a new store starts at version 1 and a new subscription starts at 0.
/// Then the method gives <see langword="true"/> one time for each version change, with the slice of the newest value.
/// Many writes before one read give one <see langword="true"/>.
/// </para>
/// <para>
/// The subscription does not store the last slice. A change of another slice of the same store also gives
/// <see langword="true"/>, with a slice that is equal to the old slice.
/// </para>
/// <para>
/// The struct is mutable. Call <see cref="TryGetChanged"/> on a variable, an array slot, or a <see langword="ref"/>.
/// Never call it on a copy or on a <see langword="readonly"/> field. A call on a copy loses the applied version.
/// </para>
/// <para>
/// Pass a <see langword="static"/> lambda or a cached method group as the selector. The selector must not allocate.
/// The constructor checks the selector for <see langword="null"/> only. When the selector throws, the subscription
/// still applies the version.
/// </para>
/// <para>
/// A <see langword="default"/> value holds no store. <see cref="TryGetChanged"/> throws on it. The subscription runs on
/// the UI thread only. It uses no lock.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value of the store.</typeparam>
/// <typeparam name="TValue">The type of the slice that the selector gives.</typeparam>
public struct StoreSubscription<T, TValue>
{
    private readonly Store<T> _store;
    private readonly Func<T, TValue> _selector;
    private ulong _applied;

    /// <summary>
    /// Creates a subscription. The applied version is 0, so the first read applies.
    /// </summary>
    /// <param name="store">The store to read.</param>
    /// <param name="selector">The function that selects the slice. Use a <see langword="static"/> lambda.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="store"/> or the <paramref name="selector"/> is
    /// <see langword="null"/>.</exception>
    public StoreSubscription(Store<T> store, Func<T, TValue> selector)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        _store = store;
        _selector = selector;
        _applied = 0;
    }

    /// <summary>
    /// Gets the store that the subscription reads. A <see langword="default"/> value gives <see langword="null"/>.
    /// </summary>
    public readonly Store<T> Store => _store;

    /// <summary>
    /// Gets the store version that the subscription applied last. It is 0 before the first read.
    /// </summary>
    public readonly ulong AppliedVersion => _applied;

    /// <summary>
    /// Gives the slice of the newest value when the store version differs from the applied version.
    /// </summary>
    /// <param name="value">The slice of the newest value when the method gives <see langword="true"/>. Else the
    /// default value.</param>
    /// <returns><see langword="true"/> when the store version differs from the applied version. The first read of a new
    /// subscription always gives <see langword="true"/>.</returns>
    /// <exception cref="InvalidOperationException">The subscription is a <see langword="default"/> value.</exception>
    public bool TryGetChanged([MaybeNullWhen(false)] out TValue value)
    {
        if (_store is null)
        {
            throw new InvalidOperationException("The subscription is a default value and holds no store.");
        }

        ulong version = _store.Version;
        if (version == _applied)
        {
            value = default;
            return false;
        }

        _applied = version;
        value = _selector(_store.Value);
        return true;
    }
}
