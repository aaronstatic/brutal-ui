using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The bind method that connects a <see cref="Button"/> to a command of a <see cref="CommandRegistry"/>.
/// </summary>
/// <remarks>
/// The method is an extension method because <see cref="Button"/> lives in <c>BrutalUi.Core</c>, and Core cannot name
/// <see cref="CommandRegistry"/>.
/// </remarks>
public static class CommandBindings
{
    /// <summary>
    /// Binds a button to a command name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bind copies the name and adds one delegate to <see cref="Button.Clicked"/> with <c>+=</c>. A handler that the
    /// game set before the bind still runs, before the command. The bind allocates one time, at build time.
    /// </para>
    /// <para>
    /// A click, or an Enter or Space key on the focused button, invokes the command. The name needs no command at the
    /// time of the bind, because the registry looks it up at each click. A later write of <c>=</c> to
    /// <see cref="Button.Clicked"/> removes the command, so use <c>+=</c>. The button holds the registry, and the
    /// registry holds no reference to the button.
    /// </para>
    /// <para>
    /// An unknown name at a click calls <see cref="CommandRegistry.UnknownCommand"/> of the registry. Two binds on one
    /// button invoke both commands, in the order of the bind. An <see cref="CommandRegistry.Unregister"/> of the name
    /// makes each later click a miss.
    /// </para>
    /// </remarks>
    /// <param name="button">The button.</param>
    /// <param name="registry">The registry that holds the command.</param>
    /// <param name="name">The UTF-8 name of the command.</param>
    /// <returns>The button.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="button"/> or <paramref name="registry"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static Button BindCommand(this Button button, CommandRegistry registry, ReadOnlySpan<byte> name)
    {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(registry);

        if (name.IsEmpty)
        {
            throw new ArgumentException("The name of a command must not be empty.", nameof(name));
        }

        ButtonCommand target = new(registry, name.ToArray());
        button.Clicked += target.Run;
        return button;
    }

    /// <summary>The target of the click delegate: the registry and a copy of the name.</summary>
    private sealed class ButtonCommand
    {
        /// <summary>The registry.</summary>
        private readonly CommandRegistry _registry;

        /// <summary>The copy of the UTF-8 name.</summary>
        private readonly byte[] _name;

        /// <summary>Creates the target.</summary>
        /// <param name="registry">The registry.</param>
        /// <param name="name">The copy of the name.</param>
        public ButtonCommand(CommandRegistry registry, byte[] name)
        {
            _registry = registry;
            _name = name;
        }

        /// <summary>Invokes the command. The delegate of <see cref="Button.Clicked"/> calls it.</summary>
        /// <param name="button">The button that was clicked.</param>
        internal void Run(Button button) => _registry.Invoke(_name);
    }
}
