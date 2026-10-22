namespace BrutalUi.Core;

/// <summary>
/// A handler for a UI event. The walk gives the event by reference.
/// </summary>
/// <typeparam name="TEvent">The event struct, for example <see cref="PointerEvent"/>.</typeparam>
/// <param name="e">The event. The handler reads the header and the fields of the kind.</param>
/// <remarks>
/// <para>
/// Register a handler at build time, not in the frame loop. A delegate with a closure allocates when the
/// code creates it, not when the walk calls it.
/// </para>
/// <para>The walk calls the handler with no allocation.</para>
/// </remarks>
public delegate void UiEventHandler<TEvent>(ref TEvent e)
    where TEvent : struct;

/// <summary>
/// The static members that the walk and the ring use for one event struct.
/// </summary>
/// <typeparam name="TSelf">The event struct that implements the interface.</typeparam>
/// <remarks>
/// Each public event struct implements the interface explicitly, so the members do not show in the public
/// API. The walk calls the members through the generic constraint. The JIT compiles one copy of the walk
/// for each struct, so a call boxes nothing.
/// </remarks>
internal interface IUiEvent<TSelf>
    where TSelf : struct, IUiEvent<TSelf>
{
    /// <summary>Gives the header of an event by reference.</summary>
    /// <param name="e">The event.</param>
    /// <returns>A reference to the header field of the event.</returns>
    static abstract ref UiEventHeader HeaderOf(ref TSelf e);

    /// <summary>Tells if the struct carries an event type.</summary>
    /// <param name="type">The event type.</param>
    /// <returns><see langword="true"/> when the struct carries the type.</returns>
    static abstract bool Accepts(UiEventType type);

    /// <summary>Picks the handler slot for an event type and a phase.</summary>
    /// <param name="handlers">The handler slots of an element.</param>
    /// <param name="type">The event type.</param>
    /// <param name="capture"><see langword="true"/> for the capture slot, <see langword="false"/> for the bubble slot.</param>
    /// <returns>The handler of the slot, or <see langword="null"/> when the slot is empty or the struct does not carry the type.</returns>
    static abstract UiEventHandler<TSelf>? GetHandler(ElementHandlers handlers, UiEventType type, bool capture);

    /// <summary>Copies the fields of the kind into the payload of a ring entry.</summary>
    /// <param name="e">The event.</param>
    /// <param name="payload">The payload of the ring entry.</param>
    static abstract void Store(in TSelf e, ref UiEventPayload payload);

    /// <summary>Makes an event from the header and the payload of a ring entry.</summary>
    /// <param name="header">The header of the ring entry.</param>
    /// <param name="payload">The payload of the ring entry.</param>
    /// <returns>The event.</returns>
    static abstract TSelf Load(in UiEventHeader header, in UiEventPayload payload);
}
