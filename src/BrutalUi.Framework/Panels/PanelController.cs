using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The owner of one panel: it builds the root element one time and keeps it.
/// </summary>
/// <remarks>
/// <para>
/// The controller owns one <see cref="PanelInstance"/> and reaches the command registry, the event bus, and the
/// service registry through <see cref="Context"/>. The slot-container and mode-set features call
/// <see cref="EnsureRoot"/> at the mount.
/// </para>
/// <para>
/// <see cref="EnsureRoot"/> calls the factory of the definition one time, at the first call, and gives the same root
/// after that. The warm call reads one field and allocates 0 bytes. The controller keeps the root for its life and
/// never detaches or replaces it.
/// </para>
/// <para>
/// A factory exception leaves <see cref="EnsureRoot"/> with no wrap. The controller stays unbuilt, and the next call
/// calls the factory again. A factory that returns <see langword="null"/> makes <see cref="EnsureRoot"/> throw
/// <see cref="InvalidOperationException"/>, and the controller stays unbuilt. A call from inside the factory throws
/// <see cref="InvalidOperationException"/>. Only the inner call throws. The outer call goes on, and it keeps the root
/// that the factory returns.
/// </para>
/// <para>
/// The controller does not bind the root to a store. The factory does that. The controller does not check the parent of
/// the root. The slot-container feature owns the mount rule. The controller runs on the UI thread and uses no lock.
/// </para>
/// </remarks>
public sealed class PanelController
{
    /// <summary>The message of a call from inside the factory.</summary>
    private const string ReentryMessage = "EnsureRoot must not be called from inside the factory of the same panel.";

    /// <summary>The message of a factory that returns <see langword="null"/>.</summary>
    private const string NullRootMessage = "The factory of a panel must not return null.";

    /// <summary>Tells whether the factory runs now.</summary>
    private bool _building;

    /// <summary>Creates a controller with an unbuilt instance.</summary>
    /// <param name="definition">The definition of the panel.</param>
    /// <param name="context">The context that the factory reads.</param>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    public PanelController(PanelDefinition definition, PanelContext context)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        Definition = definition;
        Context = context;
        Instance = new PanelInstance(definition);
    }

    /// <summary>Gets the definition of the panel.</summary>
    public PanelDefinition Definition { get; }

    /// <summary>Gets the context that the factory reads.</summary>
    public PanelContext Context { get; }

    /// <summary>Gets the one instance of the panel.</summary>
    public PanelInstance Instance { get; }

    /// <summary>
    /// Gives the root element. The first call runs the factory and keeps the root. Each later call gives the same root.
    /// </summary>
    /// <returns>The root element.</returns>
    /// <exception cref="InvalidOperationException">
    /// The factory returned <see langword="null"/>, or the factory called this method on the same controller.
    /// </exception>
    public Element EnsureRoot()
    {
        Element? root = Instance.Root;
        if (root is not null)
        {
            return root;
        }

        if (_building)
        {
            throw new InvalidOperationException(ReentryMessage);
        }

        _building = true;
        try
        {
            root = Definition.CreateRoot(Context);
        }
        finally
        {
            _building = false;
        }

        if (root is null)
        {
            throw new InvalidOperationException(NullRootMessage);
        }

        Instance.SetRoot(root);
        return root;
    }
}
