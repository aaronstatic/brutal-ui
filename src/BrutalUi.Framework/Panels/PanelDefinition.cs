using System;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The definition of a panel: a UTF-8 name, a default slot, and a factory for the root element.
/// </summary>
/// <remarks>
/// <para>
/// The record copies each name at build time, so a change of the source bytes changes nothing. A name property
/// returns a span over the copy and allocates nothing.
/// </para>
/// <para>
/// The factory runs one time for each controller, at the first mount, so a closure costs one build-time allocation
/// only. The factory can bind the root to a store (A7). <see cref="DefaultSlot"/> names the slot of the panel when a
/// mode does not name one. The mode-set feature uses it.
/// </para>
/// <para>
/// The record keeps the equality that the compiler generates. It compares the name copies by reference, so two
/// records from two constructor calls are not equal, and a <see langword="with"/> copy equals its source. Compare the
/// names with <c>Name.SequenceEqual(other)</c>. The record has no <see langword="init"/> member, so a
/// <see langword="with"/> expression cannot change a member and cannot skip the validation.
/// </para>
/// <para>
/// <see cref="object.ToString"/> shows the names as text. It allocates, so use it in a debugger and a log, not in a
/// frame. The record runs on the UI thread.
/// </para>
/// </remarks>
public sealed record PanelDefinition
{
    /// <summary>The copy of the name.</summary>
    private readonly byte[] _name;

    /// <summary>The copy of the default slot name.</summary>
    private readonly byte[] _defaultSlot;

    /// <summary>The factory of the root element.</summary>
    private readonly Func<PanelContext, Element> _createRoot;

    /// <summary>
    /// Creates a definition. The constructor copies both names.
    /// </summary>
    /// <param name="name">The UTF-8 name of the panel.</param>
    /// <param name="defaultSlot">The UTF-8 name of the slot that holds the panel when a mode names no slot.</param>
    /// <param name="createRoot">The factory. It returns a new, detached element.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="defaultSlot"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="createRoot"/> is <see langword="null"/>.</exception>
    public PanelDefinition(ReadOnlySpan<byte> name, ReadOnlySpan<byte> defaultSlot, Func<PanelContext, Element> createRoot)
    {
        if (name.IsEmpty)
        {
            throw new ArgumentException("The name of a panel must not be empty.", nameof(name));
        }

        if (defaultSlot.IsEmpty)
        {
            throw new ArgumentException("The default slot of a panel must not be empty.", nameof(defaultSlot));
        }

        ArgumentNullException.ThrowIfNull(createRoot);

        _name = name.ToArray();
        _defaultSlot = defaultSlot.ToArray();
        _createRoot = createRoot;
    }

    /// <summary>Gets the UTF-8 name of the panel.</summary>
    public ReadOnlySpan<byte> Name => _name;

    /// <summary>Gets the UTF-8 name of the slot that holds the panel when a mode names no slot.</summary>
    public ReadOnlySpan<byte> DefaultSlot => _defaultSlot;

    /// <summary>
    /// Gets the factory of the root element.
    /// </summary>
    /// <remarks>
    /// The factory returns a new, detached element on each call. The controller calls it one time.
    /// </remarks>
    public Func<PanelContext, Element> CreateRoot => _createRoot;

    /// <summary>Writes the names as text for <see cref="object.ToString"/>.</summary>
    /// <param name="builder">The builder of the text.</param>
    /// <returns><see langword="true"/> always, because the method writes members.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Name = ");
        builder.Append(Encoding.UTF8.GetString(_name));
        builder.Append(", DefaultSlot = ");
        builder.Append(Encoding.UTF8.GetString(_defaultSlot));
        return true;
    }
}
