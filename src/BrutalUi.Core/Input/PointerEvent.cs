using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// A pointer event: a move, a button down, or a button up.
/// </summary>
/// <remarks>
/// <see cref="UiEventHeader.Type"/> is <see cref="UiEventType.PointerMove"/>,
/// <see cref="UiEventType.PointerDown"/>, or <see cref="UiEventType.PointerUp"/>.
/// For a move, the walk does not read <see cref="Button"/>.
/// </remarks>
public struct PointerEvent : IUiEvent<PointerEvent>
{
    /// <summary>The type, the timestamp, the target, and the state of the walk.</summary>
    public UiEventHeader Header;

    /// <summary>The pointer position in the coordinates of the context.</summary>
    public float2 Position;

    /// <summary>The button that goes down or up.</summary>
    public PointerButton Button;

    /// <summary>The modifier keys that are down at the time of the event.</summary>
    public UiModifiers Modifiers;

    /// <inheritdoc/>
    static ref UiEventHeader IUiEvent<PointerEvent>.HeaderOf(ref PointerEvent e) => ref e.Header;

    /// <inheritdoc/>
    static bool IUiEvent<PointerEvent>.Accepts(UiEventType type)
    {
        return type is UiEventType.PointerMove or UiEventType.PointerDown or UiEventType.PointerUp;
    }

    /// <inheritdoc/>
    static UiEventHandler<PointerEvent>? IUiEvent<PointerEvent>.GetHandler(
        ElementHandlers handlers,
        UiEventType type,
        bool capture)
    {
        return type switch
        {
            UiEventType.PointerMove => capture ? handlers.PointerMoveCapture : handlers.PointerMove,
            UiEventType.PointerDown => capture ? handlers.PointerDownCapture : handlers.PointerDown,
            UiEventType.PointerUp => capture ? handlers.PointerUpCapture : handlers.PointerUp,
            _ => null,
        };
    }

    /// <inheritdoc/>
    static void IUiEvent<PointerEvent>.Store(in PointerEvent e, ref UiEventPayload payload)
    {
        payload.Pointer.Position = e.Position;
        payload.Pointer.Button = e.Button;
        payload.Pointer.Modifiers = e.Modifiers;
    }

    /// <inheritdoc/>
    static PointerEvent IUiEvent<PointerEvent>.Load(in UiEventHeader header, in UiEventPayload payload)
    {
        return new PointerEvent
        {
            Header = header,
            Position = payload.Pointer.Position,
            Button = payload.Pointer.Button,
            Modifiers = payload.Pointer.Modifiers,
        };
    }
}
