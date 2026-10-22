namespace BrutalUi.Core;

/// <summary>
/// A text input event: one Unicode character.
/// </summary>
/// <remarks>
/// <para><see cref="UiEventHeader.Type"/> is <see cref="UiEventType.Char"/>.</para>
/// <para>GLFW gives no modifiers with a char, so this struct has none.</para>
/// </remarks>
public struct CharEvent : IUiEvent<CharEvent>
{
    /// <summary>The type, the timestamp, the target, and the state of the walk.</summary>
    public UiEventHeader Header;

    /// <summary>The Unicode scalar value of the character.</summary>
    public uint CodePoint;

    /// <inheritdoc/>
    static ref UiEventHeader IUiEvent<CharEvent>.HeaderOf(ref CharEvent e) => ref e.Header;

    /// <inheritdoc/>
    static bool IUiEvent<CharEvent>.Accepts(UiEventType type)
    {
        return type is UiEventType.Char;
    }

    /// <inheritdoc/>
    static UiEventHandler<CharEvent>? IUiEvent<CharEvent>.GetHandler(
        ElementHandlers handlers,
        UiEventType type,
        bool capture)
    {
        return type switch
        {
            UiEventType.Char => capture ? handlers.CharCapture : handlers.Char,
            _ => null,
        };
    }

    /// <inheritdoc/>
    static void IUiEvent<CharEvent>.Store(in CharEvent e, ref UiEventPayload payload)
    {
        payload.Char.CodePoint = e.CodePoint;
    }

    /// <inheritdoc/>
    static CharEvent IUiEvent<CharEvent>.Load(in UiEventHeader header, in UiEventPayload payload)
    {
        return new CharEvent
        {
            Header = header,
            CodePoint = payload.Char.CodePoint,
        };
    }
}
