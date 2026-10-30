using System;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The definition of a layout: a UTF-8 name and a factory for the root element.
/// </summary>
/// <remarks>
/// <para>
/// The root of the layout holds the <c>SlotContainer</c> elements of the slot-container feature, and the manager
/// finds each one by name (Q4 of the epic). The layout root is the one <see cref="UiContext"/> root (A4 of the
/// epic). The factory gets the same <see cref="PanelContext"/> as the panels.
/// </para>
/// <para>
/// The record copies the name at build time, and a name read allocates nothing. The record keeps the equality that
/// the compiler generates. It compares the name copy by reference, so two records from two constructor calls are not
/// equal. Compare the names with <c>Name.SequenceEqual(other)</c>. The record has no <see langword="init"/> member, so
/// a <see langword="with"/> expression cannot skip the validation.
/// </para>
/// <para>
/// <see cref="object.ToString"/> shows the name as text. It allocates, so use it in a debugger and a log, not in a
/// frame. The record runs on the UI thread.
/// </para>
/// </remarks>
public sealed record LayoutDefinition
{
    /// <summary>The copy of the name.</summary>
    private readonly byte[] _name;

    /// <summary>The factory of the root element.</summary>
    private readonly Func<PanelContext, Element> _createRoot;

    /// <summary>
    /// Creates a definition. The constructor copies the name.
    /// </summary>
    /// <param name="name">The UTF-8 name of the layout.</param>
    /// <param name="createRoot">The factory. It returns a new, detached element.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="createRoot"/> is <see langword="null"/>.</exception>
    public LayoutDefinition(ReadOnlySpan<byte> name, Func<PanelContext, Element> createRoot)
    {
        if (name.IsEmpty)
        {
            throw new ArgumentException("The name of a layout must not be empty.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(createRoot);

        _name = name.ToArray();
        _createRoot = createRoot;
    }

    /// <summary>Gets the UTF-8 name of the layout.</summary>
    public ReadOnlySpan<byte> Name => _name;

    /// <summary>
    /// Gets the factory of the root element.
    /// </summary>
    /// <remarks>
    /// The factory returns a new, detached element on each call.
    /// </remarks>
    public Func<PanelContext, Element> CreateRoot => _createRoot;

    /// <summary>Writes the name as text for <see cref="object.ToString"/>.</summary>
    /// <param name="builder">The builder of the text.</param>
    /// <returns><see langword="true"/> always, because the method writes members.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Name = ");
        builder.Append(Encoding.UTF8.GetString(_name));
        return true;
    }
}
