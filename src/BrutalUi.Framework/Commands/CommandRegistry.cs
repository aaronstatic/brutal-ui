using System;

namespace BrutalUi.Framework;

/// <summary>
/// A register of named commands. A command maps a UTF-8 name to a static handler and a target.
/// </summary>
/// <remarks>
/// <para>
/// The names compare by their bytes, so <c>Save</c> and <c>save</c> are two names. <see cref="Register"/> copies the
/// name and can grow the array, so register at build time.
/// </para>
/// <para>
/// <see cref="Invoke"/> scans the names in the order of the register. It allocates nothing and throws nothing of its
/// own. A miss calls <see cref="UnknownCommand"/> with the name and gives <see langword="false"/>. An exception of a
/// handler or of the slot leaves <see cref="Invoke"/>, and the registry stays usable.
/// </para>
/// <para>
/// A handler can register, unregister, and invoke a command during an invoke, because <see cref="Invoke"/> copies the
/// handler and the target before the call.
/// </para>
/// <para>
/// The registry holds each target until <see cref="Unregister"/> frees the name or the game drops the registry. Call
/// <see cref="Unregister"/> when the target of a command closes. The registry runs on the UI thread and uses no
/// lock.
/// </para>
/// </remarks>
public sealed class CommandRegistry
{
    /// <summary>The first size of the entry array.</summary>
    private const int InitialCapacity = 8;

    /// <summary>The entries in the order of the register. The array is <see langword="null"/> until the first register.</summary>
    private CommandEntry[]? _entries;

    /// <summary>The count of used entries.</summary>
    private int _count;

    /// <summary>Creates an empty registry.</summary>
    public CommandRegistry()
    {
    }

    /// <summary>Gets the count of registered commands.</summary>
    public int Count => _count;

    /// <summary>
    /// Gets or sets the slot that <see cref="Invoke"/> calls for a name that no command holds.
    /// </summary>
    /// <remarks>
    /// The slot is <see langword="null"/> at the start. The game sets it one time, as <c>UiContext.OnFileDrop</c>.
    /// The span of the slot is valid during the call only.
    /// </remarks>
    public UnknownCommandHandler? UnknownCommand { get; set; }

    /// <summary>
    /// Registers a command.
    /// </summary>
    /// <remarks>
    /// The registry copies the name. The registry holds the handler and the target until <see cref="Unregister"/> frees
    /// the name or the game drops the registry. A name must not be empty, and a name that the registry holds is a
    /// duplicate. Call <see cref="Unregister"/> first to replace a command.
    /// </remarks>
    /// <param name="name">The UTF-8 name of the command.</param>
    /// <param name="handler">The handler. Pass a static method in a <see langword="static"/> field.</param>
    /// <param name="target">The object that the registry gives to the handler. It can be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty, or the registry holds the name.</exception>
    public void Register(ReadOnlySpan<byte> name, CommandHandler handler, object? target)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (name.IsEmpty)
        {
            throw new ArgumentException("The name of a command must not be empty.", nameof(name));
        }

        if (IndexOf(name) >= 0)
        {
            throw new ArgumentException("The registry already holds a command with this name.", nameof(name));
        }

        if (_entries is null)
        {
            _entries = new CommandEntry[InitialCapacity];
        }
        else if (_count == _entries.Length)
        {
            Array.Resize(ref _entries, _entries.Length * 2);
        }

        _entries[_count] = new CommandEntry { Name = name.ToArray(), Handler = handler, Target = target };
        _count++;
    }

    /// <summary>
    /// Tests whether a command with a name exists.
    /// </summary>
    /// <param name="name">The UTF-8 name of the command.</param>
    /// <returns><see langword="true"/> when the registry holds the name.</returns>
    public bool Contains(ReadOnlySpan<byte> name)
    {
        return IndexOf(name) >= 0;
    }

    /// <summary>
    /// Calls the handler of a command with its target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An unknown name calls no handler. The method calls <see cref="UnknownCommand"/> with the name and gives
    /// <see langword="false"/>. A <see langword="null"/> slot reports through the return value only. The method
    /// allocates nothing.
    /// </para>
    /// <para>
    /// The method copies the handler and the target before the call, so a handler can register, unregister, and invoke
    /// a command. A register in a handler can grow the array and allocate. An exception of the handler or of the slot
    /// leaves the method, and the registry stays usable.
    /// </para>
    /// </remarks>
    /// <param name="name">The UTF-8 name of the command.</param>
    /// <returns><see langword="true"/> when the registry found the name and called the handler.</returns>
    public bool Invoke(ReadOnlySpan<byte> name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            UnknownCommand?.Invoke(name);
            return false;
        }

        CommandEntry entry = _entries![index];
        CommandHandler handler = entry.Handler;
        object? target = entry.Target;
        handler(target);
        return true;
    }

    /// <summary>
    /// Removes the command of a name.
    /// </summary>
    /// <remarks>
    /// The registry drops the handler, the target, and the copy of the name. Call this method when the target of a
    /// command closes, so the registry drops the target. The later commands keep their order.
    /// </remarks>
    /// <param name="name">The UTF-8 name of the command.</param>
    /// <returns><see langword="true"/> when the registry held the name. <see langword="false"/> for an unknown name.</returns>
    public bool Unregister(ReadOnlySpan<byte> name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            return false;
        }

        CommandEntry[] entries = _entries!;
        for (int i = index; i < _count - 1; i++)
        {
            entries[i] = entries[i + 1];
        }

        _count--;
        entries[_count] = default;
        return true;
    }

    /// <summary>Finds the entry of a name.</summary>
    /// <param name="name">The UTF-8 name.</param>
    /// <returns>The index of the entry, or -1 when no entry matches.</returns>
    private int IndexOf(ReadOnlySpan<byte> name)
    {
        CommandEntry[]? entries = _entries;
        for (int i = 0; i < _count; i++)
        {
            if (entries![i].Name.AsSpan().SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>One registered command.</summary>
    private struct CommandEntry
    {
        /// <summary>The copy of the name.</summary>
        public byte[] Name;

        /// <summary>The handler.</summary>
        public CommandHandler Handler;

        /// <summary>The target that the registry gives to the handler.</summary>
        public object? Target;
    }
}
