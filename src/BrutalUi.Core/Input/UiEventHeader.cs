namespace BrutalUi.Core;

/// <summary>
/// The fields that each UI event holds: the type, the timestamp, the target, and the state of the walk.
/// </summary>
/// <remarks>
/// <para>
/// The caller can set the target (A4). The drain hit-tests a pointer or scroll event with no target and puts the
/// element under the pointer in <see cref="Target"/>. A key or char event with no target goes to the focus owner
/// (<see cref="UiContext.FocusedElement"/>) at the drain. With no owner, it gets no walk.
/// </para>
/// <para>
/// The walk writes <see cref="Phase"/> and <see cref="CurrentTarget"/> before each handler call. Only the
/// framework can write these two properties.
/// </para>
/// <para>
/// The walk copies the path from the target to the root at the start (A3). A handler that changes
/// <see cref="Target"/> during the walk does not change that path.
/// </para>
/// </remarks>
public struct UiEventHeader
{
    /// <summary>
    /// Creates a header with a type, a timestamp, and a target. The phase is <see cref="UiEventPhase.None"/>
    /// and the current target is <see langword="null"/>.
    /// </summary>
    /// <param name="type">The kind of the event.</param>
    /// <param name="timestamp">The time of the event in seconds, from the caller.</param>
    /// <param name="target">The element that gets the event, or <see langword="null"/>.</param>
    public UiEventHeader(UiEventType type, double timestamp, Element? target)
    {
        Type = type;
        Timestamp = timestamp;
        Target = target;
    }

    /// <summary>The kind of the event.</summary>
    public UiEventType Type;

    /// <summary>
    /// The time of the event in seconds (A6). The caller gives the value. The framework does not read a clock.
    /// </summary>
    public double Timestamp;

    /// <summary>
    /// The element that gets the event (A4), or <see langword="null"/> for an event that the drain routes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pointer event with no target gets the hit element as its target at the drain, not at the push. The hit test
    /// reads the layout of the last <see cref="UiContext.Update(float)"/>. The drain also moves
    /// <see cref="PseudoStates.Hover"/> to the hit element. A pointer event over no element gets no walk.
    /// </para>
    /// <para>
    /// A key or char event with no target gets the focus owner (<see cref="UiContext.FocusedElement"/>) as its target
    /// at the drain, not at the push. With no owner, it gets no walk.
    /// </para>
    /// <para>
    /// A target from the caller skips the hit test and the states (Decision 2). It keeps its target, and a key or char
    /// event with a target changes no focus.
    /// </para>
    /// </remarks>
    public Element? Target;

    /// <summary>Gets the phase of the walk. The walk writes it before each handler call.</summary>
    public UiEventPhase Phase { readonly get; internal set; }

    /// <summary>
    /// Gets the element whose handler the walk calls. The walk writes it before each handler call.
    /// </summary>
    public Element? CurrentTarget { readonly get; internal set; }

    /// <summary>
    /// Gets a value that tells if a handler stopped the walk. The queue sets it to <see langword="false"/> when it
    /// takes an event.
    /// </summary>
    public bool IsPropagationStopped { readonly get; private set; }

    /// <summary>
    /// Stops the walk. The walk calls no later handler, also no later delegate of the same multicast slot.
    /// </summary>
    /// <remarks>
    /// A call through a copy of the event has no effect on the walk. Call this method on the <see langword="ref"/>
    /// parameter of the handler.
    /// </remarks>
    public void StopPropagation()
    {
        IsPropagationStopped = true;
    }

    /// <summary>
    /// Sets the phase to <see cref="UiEventPhase.None"/>, the current target to <see langword="null"/>, and the stop
    /// flag to <see langword="false"/>. A queued event never arrives stopped.
    /// </summary>
    internal void ResetWalkState()
    {
        Phase = UiEventPhase.None;
        CurrentTarget = null;
        IsPropagationStopped = false;
    }
}
