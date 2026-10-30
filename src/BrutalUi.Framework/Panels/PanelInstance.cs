using System.Diagnostics.CodeAnalysis;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The one live instance of a panel: its definition and its root element.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="PanelController"/> creates the one instance and writes <see cref="Root"/> one time. <see cref="Root"/>
/// is <see langword="null"/> until the first <see cref="PanelController.EnsureRoot"/>. After that, it is the same
/// <see cref="Element"/> for the life of the controller, also while the root is detached from each slot.
/// </para>
/// <para>
/// The slot-container feature mounts <see cref="Root"/>. The instance runs on the UI thread and uses no lock.
/// </para>
/// </remarks>
public sealed class PanelInstance
{
    /// <summary>Creates an unbuilt instance. Only <see cref="PanelController"/> calls this constructor.</summary>
    /// <param name="definition">The definition of the panel.</param>
    internal PanelInstance(PanelDefinition definition)
    {
        Definition = definition;
    }

    /// <summary>Gets the definition of the panel.</summary>
    public PanelDefinition Definition { get; }

    /// <summary>Gets the root element, or <see langword="null"/> until the first <see cref="PanelController.EnsureRoot"/>.</summary>
    public Element? Root { get; private set; }

    /// <summary>Gets a value that tells whether the controller built the root.</summary>
    [MemberNotNullWhen(true, nameof(Root))]
    public bool IsBuilt => Root is not null;

    /// <summary>Writes the root. Only <see cref="PanelController"/> calls this method, one time.</summary>
    /// <param name="root">The root element that the factory made.</param>
    internal void SetRoot(Element root)
    {
        Root = root;
    }
}
