using System;

namespace BrutalUi.Core;

/// <summary>
/// The kind of a UI event. The header of each event holds one value.
/// </summary>
/// <remarks>
/// The set has no enter event and no leave event (A1). The hover feature makes the hover state from the
/// pointer moves.
/// </remarks>
public enum UiEventType
{
    /// <summary>No event. A default header has this type.</summary>
    None = 0,

    /// <summary>The pointer moves. <see cref="PointerEvent"/> carries it.</summary>
    PointerMove,

    /// <summary>A pointer button goes down. <see cref="PointerEvent"/> carries it.</summary>
    PointerDown,

    /// <summary>A pointer button goes up. <see cref="PointerEvent"/> carries it.</summary>
    PointerUp,

    /// <summary>The scroll wheel or the touch pad scrolls.</summary>
    Scroll,

    /// <summary>A key goes down, or the key repeats.</summary>
    KeyDown,

    /// <summary>A key goes up.</summary>
    KeyUp,

    /// <summary>The text input gives one Unicode code point.</summary>
    Char,
}

/// <summary>
/// The phase of the walk at the time of a handler call.
/// </summary>
/// <remarks>
/// The walk calls the capture slots from the root down to the parent of the target. Then it calls the
/// capture slot and the bubble slot of the target, both with the phase <c>Target</c>. Then it calls the
/// bubble slots from the parent of the target up to the root.
/// </remarks>
public enum UiEventPhase
{
    /// <summary>No walk runs. A queued event has this phase.</summary>
    None = 0,

    /// <summary>The walk goes from the root down to the parent of the target.</summary>
    Capture,

    /// <summary>The walk is at the target.</summary>
    Target,

    /// <summary>The walk goes from the parent of the target up to the root.</summary>
    Bubble,
}

/// <summary>
/// A pointer button.
/// </summary>
/// <remarks>
/// The values equal the GLFW mouse button values. The input adapter casts the GLFW value to this type and
/// uses no table.
/// </remarks>
public enum PointerButton
{
    /// <summary>The left button. GLFW value 0.</summary>
    Left = 0,

    /// <summary>The right button. GLFW value 1.</summary>
    Right = 1,

    /// <summary>The middle button. GLFW value 2.</summary>
    Middle = 2,
}

/// <summary>
/// The modifier keys that are down at the time of an event.
/// </summary>
/// <remarks>
/// The values equal the GLFW modifier bits. The input adapter casts the GLFW bits to this type and uses
/// no table.
/// </remarks>
[Flags]
public enum UiModifiers
{
    /// <summary>No modifier key is down.</summary>
    None = 0,

    /// <summary>A shift key is down. GLFW value 0x1.</summary>
    Shift = 1,

    /// <summary>A control key is down. GLFW value 0x2.</summary>
    Control = 2,

    /// <summary>An alt key is down. GLFW value 0x4.</summary>
    Alt = 4,

    /// <summary>A super key is down. GLFW value 0x8.</summary>
    Super = 8,

    /// <summary>The caps lock is on. GLFW value 0x10.</summary>
    CapsLock = 16,

    /// <summary>The num lock is on. GLFW value 0x20.</summary>
    NumLock = 32,
}
