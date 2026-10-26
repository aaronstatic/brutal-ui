namespace BrutalUi.Core;

/// <summary>
/// An element that takes each pointer move of its drag, also when the pointer leaves its rect (Decision 3 of slider).
/// </summary>
/// <remarks>
/// <para>
/// The capture rule: when the element of the left press implements this interface and <see cref="CapturesMoves"/> is
/// <see langword="true"/>, the drain sends each <see cref="UiEventType.PointerMove"/> with no target to that element.
/// The walk then goes from that element up to its root, and not from the hit element. This amends Decision 8 of
/// pointer-routing for a move during a captured drag only. A move and a scroll still leave
/// <see cref="PseudoStates.Active"/>.
/// </para>
/// <para>
/// The press button rule (Decision 13 of slider): only a left press with no chord starts a capture. A right press, a
/// middle press, and a chord give no capture, so each move of such a gesture walks to the hit element.
/// </para>
/// <para>
/// The end of the capture: the release of the last button clears <see cref="PseudoStates.Active"/>, and the capture
/// ends with it. A second button during the drag makes the gesture a chord, and the capture ends at once. The stale
/// rule ends the capture when the element leaves the roots of the context.
/// </para>
/// <para>
/// The shown rule: the element must be shown. Shown means no <see cref="Display.None"/> and no opacity product of 0 or
/// less on the element or an ancestor, as for the focus. The style of the last <see cref="UiContext.Update(float)"/>
/// decides. A slider that a style change hides gets no captured move, and the move walks to the hit element. The
/// hidden element keeps <see cref="PseudoStates.Active"/>, so its value does not go to the minimum.
/// </para>
/// <para>
/// The limits: the hit test still runs for a captured move. <see cref="PseudoStates.Hover"/>,
/// <see cref="UiContext.PointerHit"/>, and the routing gate (<see cref="UiContext.RoutingGate"/>) follow the hit, not
/// the capture element. A drag over the game world thus gives a <see cref="UiRoutingGate.PointerOverUi"/> of
/// <see langword="false"/>. A move with a target from the caller keeps its target. A press, a release, and a scroll
/// keep the route of the hit.
/// </para>
/// <para>
/// Limit of the ring: the ring drops its oldest entry when it is full. A dropped <see cref="UiEventType.PointerUp"/>
/// keeps <see cref="PseudoStates.Active"/> and the button bit, so the capture goes on with no button down. Each move
/// then drags the element. The next press makes a chord and ends the capture, and the release after it clears
/// <see cref="PseudoStates.Active"/>. Call <see cref="UiContext.Update(float)"/> each frame to keep the ring below its capacity.
/// </para>
/// <para>
/// Implement the interface explicitly, so it adds no public member to the element. The check of the drain is a type
/// pattern and a property read. It allocates nothing.
/// </para>
/// </remarks>
internal interface IDragCapture
{
    /// <summary>
    /// Gets a value that tells if the element takes the moves of its drag now.
    /// </summary>
    /// <remarks>
    /// The drain reads the value for each move with no target while the element holds the left press. Return
    /// <see langword="false"/> to give the moves back to the hit, for example for a disabled element.
    /// </remarks>
    bool CapturesMoves { get; }
}
