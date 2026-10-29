using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The two focus commands of a <see cref="UiContext"/>: move the focus to the next element and to the previous
/// element.
/// </summary>
/// <remarks>
/// <para>
/// The commands call <see cref="UiContext.FocusNext"/> and <see cref="UiContext.FocusPrevious"/>. These methods are the
/// gamepad slots of Decision 8 of keyboard-focus, so their rules apply: the order is the paint order, the focus wraps,
/// and with no owner the focus goes to the first or to the last element.
/// </para>
/// <para>
/// A game maps <see cref="UiActions.NavigateDown"/> to <see cref="Next"/> and <see cref="UiActions.NavigateUp"/> to
/// <see cref="Previous"/>. Thus "down" is the next element in the tree. A spatial search is out of scope.
/// </para>
/// <para>
/// The registry holds the context until <see cref="CommandRegistry.Unregister"/> of both names. Each handler is one
/// <see langword="static"/> delegate, so a call allocates nothing.
/// </para>
/// </remarks>
public static class FocusCommands
{
    /// <summary>The handler of <see cref="Next"/>. The target is the context.</summary>
    private static readonly CommandHandler s_next = static target => ((UiContext)target!).FocusNext();

    /// <summary>The handler of <see cref="Previous"/>. The target is the context.</summary>
    private static readonly CommandHandler s_previous = static target => ((UiContext)target!).FocusPrevious();

    /// <summary>Gets the UTF-8 name of the command that moves the focus to the next element: <c>ui.focus-next</c>.</summary>
    public static ReadOnlySpan<byte> Next => "ui.focus-next"u8;

    /// <summary>Gets the UTF-8 name of the command that moves the focus to the previous element: <c>ui.focus-previous</c>.</summary>
    public static ReadOnlySpan<byte> Previous => "ui.focus-previous"u8;

    /// <summary>
    /// Registers both focus commands in a registry, with the context as the target.
    /// </summary>
    /// <remarks>
    /// The registry throws when it already holds one of the names. Call the method at build time.
    /// </remarks>
    /// <param name="registry">The registry.</param>
    /// <param name="context">The context that the commands move the focus of.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    public static void Register(CommandRegistry registry, UiContext context)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(context);

        registry.Register(Next, s_next, context);
        registry.Register(Previous, s_previous, context);
    }
}
