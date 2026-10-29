using System;

namespace BrutalUi.Framework;

/// <summary>
/// The target of a widget binding: a store and the selector of one slice of the store.
/// </summary>
/// <remarks>
/// The bind methods create one instance for each binding, at build time. The binding pass gives the instance to a
/// static handler, and the handler calls <see cref="Read"/>. The class holds no state that changes, so the read
/// allocates nothing.
/// </remarks>
/// <typeparam name="T">The type of the value of the store.</typeparam>
/// <typeparam name="TValue">The type of the slice.</typeparam>
internal sealed class StoreSlice<T, TValue>
{
    private readonly Store<T> _store;
    private readonly Func<T, TValue> _selector;

    /// <summary>Creates the slice.</summary>
    /// <param name="store">The store.</param>
    /// <param name="selector">The function that reads the slice from the value of the store.</param>
    public StoreSlice(Store<T> store, Func<T, TValue> selector)
    {
        _store = store;
        _selector = selector;
    }

    /// <summary>Gets the store of the slice.</summary>
    public Store<T> Store => _store;

    /// <summary>Gets the selector of the slice.</summary>
    public Func<T, TValue> Selector => _selector;

    /// <summary>Reads the slice from the current value of the store.</summary>
    /// <returns>The slice.</returns>
    public TValue Read() => _selector(_store.Value);
}
