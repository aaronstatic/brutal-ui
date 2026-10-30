using System;

namespace BrutalUi.Framework;

/// <summary>
/// The three registries that the factory of a panel or a layout reads.
/// </summary>
/// <remarks>
/// <para>
/// The game creates one context at build time and gives it to each controller. The mode-set feature gives it to the
/// manager. The factory of a panel or a layout reads the command registry, the event bus, and the service registry
/// from it.
/// </para>
/// <para>
/// The context holds no other state and owns no registry. Each member is read-only after the initializer, and a
/// <see langword="null"/> value throws <see cref="ArgumentNullException"/>. The context runs on the UI thread.
/// </para>
/// </remarks>
public sealed class PanelContext
{
    /// <summary>The command registry.</summary>
    private readonly CommandRegistry _commands = null!;

    /// <summary>The event bus.</summary>
    private readonly EventBus _events = null!;

    /// <summary>The service registry.</summary>
    private readonly ServiceRegistry _services = null!;

    /// <summary>Gets the command registry that the factory can read.</summary>
    /// <exception cref="ArgumentNullException">The initializer gives <see langword="null"/>.</exception>
    public required CommandRegistry Commands
    {
        get => _commands;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Commands));
            _commands = value;
        }
    }

    /// <summary>Gets the event bus that the factory can read.</summary>
    /// <exception cref="ArgumentNullException">The initializer gives <see langword="null"/>.</exception>
    public required EventBus Events
    {
        get => _events;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Events));
            _events = value;
        }
    }

    /// <summary>Gets the service registry that the factory can read.</summary>
    /// <exception cref="ArgumentNullException">The initializer gives <see langword="null"/>.</exception>
    public required ServiceRegistry Services
    {
        get => _services;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Services));
            _services = value;
        }
    }
}
