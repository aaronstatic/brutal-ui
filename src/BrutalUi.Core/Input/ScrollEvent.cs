using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// A scroll event: a wheel or a trackpad scroll.
/// </summary>
/// <remarks>
/// <see cref="UiEventHeader.Type"/> is <see cref="UiEventType.Scroll"/>.
/// </remarks>
public struct ScrollEvent : IUiEvent<ScrollEvent>
{
    /// <summary>The type, the timestamp, the target, and the state of the walk.</summary>
    public UiEventHeader Header;

    /// <summary>The pointer position in the coordinates of the context. Pointer routing uses it for the hit test.</summary>
    public float2 Position;

    /// <summary>The scroll amount on the x axis and the y axis.</summary>
    public float2 Delta;

    /// <summary>The modifier keys that are down at the time of the event.</summary>
    public UiModifiers Modifiers;

    /// <inheritdoc/>
    static ref UiEventHeader IUiEvent<ScrollEvent>.HeaderOf(ref ScrollEvent e) => ref e.Header;

    /// <inheritdoc/>
    static bool IUiEvent<ScrollEvent>.Accepts(UiEventType type)
    {
        return type is UiEventType.Scroll;
    }

    /// <inheritdoc/>
    static UiEventHandler<ScrollEvent>? IUiEvent<ScrollEvent>.GetHandler(
        ElementHandlers handlers,
        UiEventType type,
        bool capture)
    {
        return type switch
        {
            UiEventType.Scroll => capture ? handlers.ScrollCapture : handlers.Scroll,
            _ => null,
        };
    }

    /// <inheritdoc/>
    static void IUiEvent<ScrollEvent>.Store(in ScrollEvent e, ref UiEventPayload payload)
    {
        payload.Scroll.Position = e.Position;
        payload.Scroll.Delta = e.Delta;
        payload.Scroll.Modifiers = e.Modifiers;
    }

    /// <inheritdoc/>
    static ScrollEvent IUiEvent<ScrollEvent>.Load(in UiEventHeader header, in UiEventPayload payload)
    {
        return new ScrollEvent
        {
            Header = header,
            Position = payload.Scroll.Position,
            Delta = payload.Scroll.Delta,
            Modifiers = payload.Scroll.Modifiers,
        };
    }
}
