using System;

namespace BrutalUi.Framework;

/// <summary>
/// The handler of a command.
/// </summary>
/// <remarks>
/// <see cref="CommandRegistry.Invoke(System.ReadOnlySpan{byte})"/> calls the handler with the target of the register.
/// Pass a static method in a <see langword="static"/> <see langword="readonly"/> field, so the register allocates no
/// delegate (as <c>Toggle.s_pointerUp</c> does). The handler runs on the UI thread, inside the input pass for a click.
/// </remarks>
/// <param name="target">The target that the game gave to <see cref="CommandRegistry.Register"/>.</param>
public delegate void CommandHandler(object? target);

/// <summary>
/// The slot of a <see cref="CommandRegistry"/> for a name that no command holds.
/// </summary>
/// <remarks>
/// The span is valid during the call only. Copy it to keep it. The slot follows <c>UiContext.OnFileDrop</c>: one
/// delegate that the game sets one time.
/// </remarks>
/// <param name="name">The UTF-8 name that no command holds.</param>
public delegate void UnknownCommandHandler(ReadOnlySpan<byte> name);
