using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The bind methods of a <see cref="Label"/>. Each method binds the text of the label to a slice of a
/// <see cref="Store{T}"/>.
/// </summary>
/// <remarks>
/// The methods are extension methods because <see cref="Label"/> lives in <c>BrutalUi.Core</c>, and Core cannot name
/// <see cref="Store{T}"/>. Each method returns its label, so a build chain can continue after the bind.
/// </remarks>
public static class LabelBindings
{
    /// <summary>
    /// Binds the text of a label to a number slice of a store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first <see cref="UiContext.Update(float)"/> after the bind shows the slice. Each later update shows the slice
    /// one time for each new version of the store. Two sets before one update show the last value.
    /// </para>
    /// <para>
    /// The binding formats the slice with <see cref="IUtf8SpanFormattable.TryFormat"/> into a buffer of the binding.
    /// It runs no <c>ToString</c>. A <see langword="null"/> <paramref name="provider"/> gives the current culture, as
    /// <c>ToString</c> does. The buffer starts at 32 bytes. It doubles on each <see langword="false"/> of
    /// <c>TryFormat</c> and never shrinks. A text that is longer than each earlier text allocates one array for each
    /// doubling, in that update only. A text over 4096 bytes throws
    /// <see cref="InvalidOperationException"/> in the update. A <see langword="null"/> slice gives an empty text.
    /// </para>
    /// <para>
    /// Pass a <see langword="static"/> lambda or a cached method group as <paramref name="selector"/>. The selector
    /// must not allocate, because the binding calls it in the update.
    /// </para>
    /// <para>
    /// The binding writes through <see cref="Label.SetText(ReadOnlySpan{byte})"/>. The same bytes mark nothing. The
    /// write clears <see cref="Label.LocalizedText"/>. A <see cref="Label.SetText(string?)"/> from code on a bound
    /// label shows until the next new version of the store.
    /// </para>
    /// <para>
    /// The bind allocates the binding at build time. The label holds the store, and the store holds no reference to
    /// the label.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <typeparam name="TValue">The type of the slice. <see cref="int"/>, <see cref="long"/>, <see cref="float"/>,
    /// <see cref="double"/>, and <see cref="decimal"/> implement <see cref="IUtf8SpanFormattable"/>.</typeparam>
    /// <param name="label">The label that shows the slice.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the slice from the value of the store.</param>
    /// <param name="format">The format string of the slice, for example <c>"N0"</c>. <see langword="null"/> gives the
    /// default format.</param>
    /// <param name="provider">The format provider. <see langword="null"/> gives the current culture.</param>
    /// <returns>The label.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="label"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Label BindText<T, TValue>(
        this Label label,
        Store<T> store,
        Func<T, TValue> selector,
        string? format = null,
        IFormatProvider? provider = null)
        where TValue : IUtf8SpanFormattable
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        NumberText<T, TValue> target = new(store, selector, format, provider);
        target.AttachTo(label);
        return label;
    }

    /// <summary>
    /// Binds the text of a label to a string slice of a store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first <see cref="UiContext.Update(float)"/> after the bind shows the slice. Each later update shows the slice
    /// one time for each new version of the store. A <see langword="null"/> slice gives an empty text.
    /// </para>
    /// <para>
    /// The binding calls <see cref="Label.SetText(string?)"/>. The call encodes the string into the buffer of the
    /// label and allocates nothing when the buffer is large enough. The same text marks nothing.
    /// </para>
    /// <para>
    /// Pass a <see langword="static"/> lambda or a cached method group as <paramref name="selector"/>. The selector
    /// must not allocate, because the binding calls it in the update. The bind allocates the binding at build time.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <param name="label">The label that shows the slice.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the slice from the value of the store.</param>
    /// <returns>The label.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="label"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Label BindText<T>(this Label label, Store<T> store, Func<T, string?> selector)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        label.AddBinding(store, StringHandler<T>.Apply, new StoreSlice<T, string?>(store, selector));
        return label;
    }

    /// <summary>The holder of the handler of the string bindings of one store type.</summary>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    private static class StringHandler<T>
    {
        /// <summary>The handler of each binding of this closed type. The add thus allocates no delegate.</summary>
        internal static readonly BindingHandler Apply = Run;

        private static void Run(Element element, object? target)
        {
            ((Label)element).SetText(((StoreSlice<T, string?>)target!).Read());
        }
    }

    /// <summary>
    /// The target of a number binding: the store, the selector, the format, the provider, and the scratch buffer.
    /// </summary>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <typeparam name="TValue">The type of the slice.</typeparam>
    private sealed class NumberText<T, TValue>
        where TValue : IUtf8SpanFormattable
    {
        /// <summary>The start size of the scratch buffer, in bytes.</summary>
        private const int InitialScratchSize = 32;

        /// <summary>The largest size of the scratch buffer, in bytes.</summary>
        private const int MaxScratchSize = 4096;

        /// <summary>The handler of each binding of this closed type. The add thus allocates no delegate.</summary>
        private static readonly BindingHandler s_apply = Apply;

        /// <summary><see langword="true"/> when <typeparamref name="TValue"/> is a reference type.</summary>
        /// <remarks>
        /// The null check of <see cref="Apply"/> reads this field first. The C# compiler emits a <c>box</c> of
        /// <typeparamref name="TValue"/> for <c>value is null</c>. The optimizing JIT removes the box for a struct, but
        /// the JIT of a Debug build keeps it, and a boxed <see cref="int"/> is 24 bytes. When this field is
        /// <see langword="false"/>, the <c>&amp;&amp;</c> skips the box, so a struct slice allocates nothing in each
        /// configuration.
        /// </remarks>
        private static readonly bool s_isReferenceType = !typeof(TValue).IsValueType;

        private readonly Store<T> _store;
        private readonly Func<T, TValue> _selector;
        private readonly string? _format;
        private readonly IFormatProvider? _provider;
        private byte[] _scratch = new byte[InitialScratchSize];

        /// <summary>Creates the target.</summary>
        /// <param name="store">The store.</param>
        /// <param name="selector">The selector of the slice.</param>
        /// <param name="format">The format string, or <see langword="null"/>.</param>
        /// <param name="provider">The format provider, or <see langword="null"/>.</param>
        public NumberText(Store<T> store, Func<T, TValue> selector, string? format, IFormatProvider? provider)
        {
            _store = store;
            _selector = selector;
            _format = format;
            _provider = provider;
        }

        /// <summary>Adds the binding of this target to a label.</summary>
        /// <param name="label">The label.</param>
        public void AttachTo(Label label)
        {
            label.AddBinding(_store, s_apply, this);
        }

        /// <summary>Reads the slice, formats it into the scratch buffer, and writes the bytes into the label.</summary>
        /// <param name="element">The bound label.</param>
        /// <param name="target">The <see cref="NumberText{T, TValue}"/> of the binding.</param>
        /// <exception cref="InvalidOperationException">The text needs more than 4096 bytes.</exception>
        private static void Apply(Element element, object? target)
        {
            var self = (NumberText<T, TValue>)target!;
            var label = (Label)element;
            TValue value = self._selector(self._store.Value);

            // A null slice of a reference type gives an empty text. The guard comes first, so that a struct slice never
            // runs the box of the null check. A Debug build keeps that box (see s_isReferenceType).
            if (s_isReferenceType && value is null)
            {
                label.SetText(ReadOnlySpan<byte>.Empty);
                return;
            }

            // The constrained call on TValue boxes nothing. A false return means that the buffer is too small.
            int written;
            while (!value.TryFormat(self._scratch, out written, self._format, self._provider))
            {
                int next = self._scratch.Length * 2;
                if (next > MaxScratchSize)
                {
                    throw new InvalidOperationException("The bound text of the label needs more than 4096 bytes.");
                }

                self._scratch = new byte[next];
            }

            label.SetText(self._scratch.AsSpan(0, written));
        }
    }
}
