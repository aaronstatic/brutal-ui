namespace BrutalUi.Core;

/// <summary>
/// The handler slots of one element. Each event kind has a bubble slot and a capture slot.
/// </summary>
/// <remarks>
/// <para>
/// The slots live in this side object, not in the element (A2). An element with no handler holds only a
/// <see langword="null"/> reference. The first registration on an element creates the side object, at
/// build time. The walk reads the slots and allocates nothing.
/// </para>
/// <para>A slot holds a delegate. A multicast delegate puts more than one handler in one slot.</para>
/// </remarks>
internal sealed class ElementHandlers
{
    /// <summary>The bubble slot for <see cref="UiEventType.PointerMove"/>. The target phase also calls it.</summary>
    internal UiEventHandler<PointerEvent>? PointerMove;

    /// <summary>The capture slot for <see cref="UiEventType.PointerMove"/>.</summary>
    internal UiEventHandler<PointerEvent>? PointerMoveCapture;

    /// <summary>The bubble slot for <see cref="UiEventType.PointerDown"/>. The target phase also calls it.</summary>
    internal UiEventHandler<PointerEvent>? PointerDown;

    /// <summary>The capture slot for <see cref="UiEventType.PointerDown"/>.</summary>
    internal UiEventHandler<PointerEvent>? PointerDownCapture;

    /// <summary>The bubble slot for <see cref="UiEventType.PointerUp"/>. The target phase also calls it.</summary>
    internal UiEventHandler<PointerEvent>? PointerUp;

    /// <summary>The capture slot for <see cref="UiEventType.PointerUp"/>.</summary>
    internal UiEventHandler<PointerEvent>? PointerUpCapture;

    /// <summary>The bubble slot for <see cref="UiEventType.Scroll"/>. The target phase also calls it.</summary>
    internal UiEventHandler<ScrollEvent>? Scroll;

    /// <summary>The capture slot for <see cref="UiEventType.Scroll"/>.</summary>
    internal UiEventHandler<ScrollEvent>? ScrollCapture;

    /// <summary>The bubble slot for <see cref="UiEventType.KeyDown"/>. The target phase also calls it.</summary>
    internal UiEventHandler<KeyEvent>? KeyDown;

    /// <summary>The capture slot for <see cref="UiEventType.KeyDown"/>.</summary>
    internal UiEventHandler<KeyEvent>? KeyDownCapture;

    /// <summary>The bubble slot for <see cref="UiEventType.KeyUp"/>. The target phase also calls it.</summary>
    internal UiEventHandler<KeyEvent>? KeyUp;

    /// <summary>The capture slot for <see cref="UiEventType.KeyUp"/>.</summary>
    internal UiEventHandler<KeyEvent>? KeyUpCapture;

    /// <summary>The bubble slot for <see cref="UiEventType.Char"/>. The target phase also calls it.</summary>
    internal UiEventHandler<CharEvent>? Char;

    /// <summary>The capture slot for <see cref="UiEventType.Char"/>.</summary>
    internal UiEventHandler<CharEvent>? CharCapture;
}
