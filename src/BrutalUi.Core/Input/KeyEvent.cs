namespace BrutalUi.Core;

/// <summary>
/// A key event: a key down or a key up.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UiEventHeader.Type"/> is <see cref="UiEventType.KeyDown"/> or <see cref="UiEventType.KeyUp"/>.
/// </para>
/// <para>
/// The adapter sends a GLFW repeat action as a <see cref="UiEventType.KeyDown"/> with <see cref="IsRepeat"/> set
/// to <see langword="true"/>.
/// </para>
/// </remarks>
public struct KeyEvent : IUiEvent<KeyEvent>
{
    /// <summary>The type, the timestamp, the target, and the state of the walk.</summary>
    public UiEventHeader Header;

    /// <summary>The GLFW key code value of the key.</summary>
    public int Key;

    /// <summary>The platform scan code of the key.</summary>
    public int ScanCode;

    /// <summary>The modifier keys that are down at the time of the event.</summary>
    public UiModifiers Modifiers;

    /// <summary><see langword="true"/> when the event comes from a key repeat.</summary>
    public bool IsRepeat;

    /// <inheritdoc/>
    static ref UiEventHeader IUiEvent<KeyEvent>.HeaderOf(ref KeyEvent e) => ref e.Header;

    /// <inheritdoc/>
    static bool IUiEvent<KeyEvent>.Accepts(UiEventType type)
    {
        return type is UiEventType.KeyDown or UiEventType.KeyUp;
    }

    /// <inheritdoc/>
    static UiEventHandler<KeyEvent>? IUiEvent<KeyEvent>.GetHandler(
        ElementHandlers handlers,
        UiEventType type,
        bool capture)
    {
        return type switch
        {
            UiEventType.KeyDown => capture ? handlers.KeyDownCapture : handlers.KeyDown,
            UiEventType.KeyUp => capture ? handlers.KeyUpCapture : handlers.KeyUp,
            _ => null,
        };
    }

    /// <inheritdoc/>
    static void IUiEvent<KeyEvent>.Store(in KeyEvent e, ref UiEventPayload payload)
    {
        payload.Key.Key = e.Key;
        payload.Key.ScanCode = e.ScanCode;
        payload.Key.Modifiers = e.Modifiers;
        payload.Key.IsRepeat = e.IsRepeat;
    }

    /// <inheritdoc/>
    static KeyEvent IUiEvent<KeyEvent>.Load(in UiEventHeader header, in UiEventPayload payload)
    {
        return new KeyEvent
        {
            Header = header,
            Key = payload.Key.Key,
            ScanCode = payload.Key.ScanCode,
            Modifiers = payload.Key.Modifiers,
            IsRepeat = payload.Key.IsRepeat,
        };
    }
}
