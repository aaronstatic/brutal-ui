using System;
using Brutal.GlfwApi;
using Brutal.Numerics;
using BrutalUi.Core;
using GlfwApi = Brutal.GlfwApi.Glfw;

namespace BrutalUi.Glfw;

/// <summary>
/// The GLFW input adapter. It converts the pointer, scroll, drop, and content-scale events of a
/// <see cref="GlfwWindow"/> into the events of <see cref="UiContext"/> and pushes them.
/// </summary>
/// <remarks>
/// <para>
/// Create the adapter on the main thread. GLFW calls the window events on the main thread only, from
/// <c>Glfw.PollEvents</c> or <c>Glfw.WaitEvents</c>. The adapter has no lock.
/// </para>
/// <para>
/// The creation order: <see cref="GlfwWindow"/> sets all of its native GLFW callbacks one time, in its constructor.
/// Its events are managed multicast events. The ImGui <c>GlfwBackend</c> and this adapter only add handlers to these
/// events and set no native callback, so neither replaces the handlers of the other, in each order. The handlers of
/// one event run in the order of the subscriptions. The recommended order is: create the window, then
/// <c>GlfwBackend</c>, then the adapter. Then ImGui gets each event first. Dispose in the opposite order: the adapter,
/// then <c>GlfwBackend</c>, then the window.
/// </para>
/// <para>
/// Dispose the adapter before the window. <see cref="Dispose"/> removes the handlers. A move or a scroll reads the key
/// state of the window, and a destroyed window has no key state.
/// </para>
/// <para>
/// Use one context for each adapter. The adapter keeps the last cursor position of its window. Two adapters on one
/// context push two copies of each event.
/// </para>
/// <para>
/// The position space: the adapter casts the GLFW cursor position to <see cref="float2"/> with no scale. The value is
/// in the screen coordinates of the window, relative to the top-left corner of the content area. This is the display
/// space of ImGui, so the context and ImGui use the same coordinates.
/// </para>
/// <para>
/// The adapter only pushes the events into the ring of the context. <see cref="UiContext.Update(float)"/> does not
/// drain the ring until the routing-gate feature. Until then, the drain is internal. A full ring drops its oldest
/// event.
/// </para>
/// <para>
/// The window catches an exception from a handler and gives it to the GLFW exception aggregator. The exception stops
/// the handlers of the same event that come after it. An exception from a handler of
/// <see cref="UiContext.OnFileDrop"/> or <see cref="UiContext.OnContentScale"/> goes this way.
/// </para>
/// </remarks>
public sealed class GlfwUiInput : IDisposable
{
    /// <summary>
    /// The data to create a <see cref="GlfwUiInput"/>.
    /// </summary>
    public struct CreateInfo
    {
        /// <summary>The window that sends the events. The adapter adds its handlers to the events of this window.</summary>
        public required GlfwWindow Window;

        /// <summary>The context that gets the events.</summary>
        public required UiContext Context;
    }

    /// <summary>The GLFW modifier bits that a button event keeps: Shift, Control, Alt, Super, Caps Lock, and Num Lock.</summary>
    private const int ButtonModifierMask = 0x3F;

    /// <summary>The position of a cursor leave. A move to this position hits no element and clears the hover.</summary>
    private static readonly float2 LeavePosition = new(float.NaN, float.NaN);

    /// <summary>The window of the adapter, or <see langword="null"/> for the test seam.</summary>
    private readonly GlfwWindow? _window;

    /// <summary>The handler of <see cref="GlfwWindow.OnCursorPos"/>. The adapter creates it one time.</summary>
    private readonly GlfwCursorPosCallback _onCursorPos;

    /// <summary>The handler of <see cref="GlfwWindow.OnMouseButton"/>. The adapter creates it one time.</summary>
    private readonly GlfwMouseButtonCallback _onMouseButton;

    /// <summary>The handler of <see cref="GlfwWindow.OnScroll"/>. The adapter creates it one time.</summary>
    private readonly GlfwScrollCallback _onScroll;

    /// <summary>The handler of <see cref="GlfwWindow.OnCursorEnter"/>. The adapter creates it one time.</summary>
    private readonly GlfwCursorEnterCallback _onCursorEnter;

    /// <summary>The handler of <see cref="GlfwWindow.OnDrop"/>. The adapter creates it one time.</summary>
    private readonly GlfwDropCallback _onDrop;

    /// <summary>The handler of <see cref="GlfwWindow.OnContentScale"/>. The adapter creates it one time.</summary>
    private readonly GlfwWindowContentScaleCallback _onContentScale;

    /// <summary>The last cursor position. A button event and a scroll event use it.</summary>
    private float2 _lastCursorPosition;

    /// <summary><see langword="true"/> after <see cref="Dispose"/>.</summary>
    private bool _disposed;

    /// <summary>
    /// Creates the adapter and adds its handlers to the events of the window.
    /// </summary>
    /// <param name="info">The window and the context.</param>
    /// <exception cref="ArgumentNullException">The window or the context of <paramref name="info"/> is null.</exception>
    /// <remarks>
    /// The constructor reads <see cref="GlfwWindow.CursorPos"/> as the first last cursor position. A scroll or a
    /// button event before the first cursor move uses this position.
    /// </remarks>
    public GlfwUiInput(in CreateInfo info)
        : this(RequireContext(in info))
    {
        GlfwWindow window = info.Window;
        _window = window;
        _lastCursorPosition = ToFloat2(window.CursorPos);

        window.OnCursorPos += _onCursorPos;
        window.OnMouseButton += _onMouseButton;
        window.OnScroll += _onScroll;
        window.OnCursorEnter += _onCursorEnter;
        window.OnDrop += _onDrop;
        window.OnContentScale += _onContentScale;
    }

    /// <summary>
    /// The test seam. Creates an adapter with no window. It adds no handler and starts at the position (0, 0).
    /// </summary>
    /// <param name="context">The context that gets the events.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    internal GlfwUiInput(UiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        _onCursorPos = HandleCursorPos;
        _onMouseButton = HandleMouseButton;
        _onScroll = HandleScroll;
        _onCursorEnter = HandleCursorEnter;
        _onDrop = HandleDrop;
        _onContentScale = HandleContentScale;
    }

    /// <summary>
    /// Gets the context that gets the events.
    /// </summary>
    public UiContext Context { get; }

    /// <summary>
    /// Removes the handlers from the events of the window. After this call, the adapter pushes no event. A second
    /// call does nothing.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GlfwWindow? window = _window;
        if (window is null)
        {
            return;
        }

        window.OnCursorPos -= _onCursorPos;
        window.OnMouseButton -= _onMouseButton;
        window.OnScroll -= _onScroll;
        window.OnCursorEnter -= _onCursorEnter;
        window.OnDrop -= _onDrop;
        window.OnContentScale -= _onContentScale;
    }

    /// <summary>
    /// Pushes a <see cref="UiEventType.PointerMove"/> with no target and stores the position as the last cursor
    /// position.
    /// </summary>
    /// <param name="position">The cursor position in the coordinates of the context.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <param name="timestamp">The time of the event, in seconds.</param>
    internal void PushCursorMove(float2 position, UiModifiers modifiers, double timestamp)
    {
        if (_disposed)
        {
            return;
        }

        _lastCursorPosition = position;
        Context.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerMove, timestamp, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = modifiers,
        });
    }

    /// <summary>
    /// Pushes a <see cref="UiEventType.PointerDown"/> for a press or a <see cref="UiEventType.PointerUp"/> for a
    /// release, with no target, at the last cursor position.
    /// </summary>
    /// <param name="button">The GLFW button value. The method casts it to <see cref="PointerButton"/> with no table.</param>
    /// <param name="press"><see langword="true"/> for a press, <see langword="false"/> for a release.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <param name="timestamp">The time of the event, in seconds.</param>
    internal void PushButton(int button, bool press, UiModifiers modifiers, double timestamp)
    {
        if (_disposed)
        {
            return;
        }

        UiEventType type = press ? UiEventType.PointerDown : UiEventType.PointerUp;
        Context.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(type, timestamp, null),
            Position = _lastCursorPosition,
            Button = (PointerButton)button,
            Modifiers = modifiers,
        });
    }

    /// <summary>
    /// Pushes a <see cref="UiEventType.Scroll"/> with no target at the last cursor position.
    /// </summary>
    /// <param name="delta">The scroll amount on the x axis and the y axis, as GLFW gives it.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <param name="timestamp">The time of the event, in seconds.</param>
    internal void PushScroll(float2 delta, UiModifiers modifiers, double timestamp)
    {
        if (_disposed)
        {
            return;
        }

        Context.PushEvent(new ScrollEvent
        {
            Header = new UiEventHeader(UiEventType.Scroll, timestamp, null),
            Position = _lastCursorPosition,
            Delta = delta,
            Modifiers = modifiers,
        });
    }

    /// <summary>
    /// Pushes a <see cref="UiEventType.PointerMove"/> at (NaN, NaN) for a cursor leave. The move hits no element, so
    /// the drain clears the hover. The last cursor position becomes (NaN, NaN).
    /// </summary>
    /// <param name="timestamp">The time of the event, in seconds.</param>
    internal void PushCursorLeave(double timestamp)
    {
        PushCursorMove(LeavePosition, UiModifiers.None, timestamp);
    }

    /// <summary>
    /// Calls <see cref="UiContext.RaiseFileDrop(ReadOnlySpan{string})"/> at once with the paths of a drop.
    /// </summary>
    /// <param name="paths">The paths of the dropped files.</param>
    internal void PushFileDrop(ReadOnlySpan<string> paths)
    {
        if (_disposed)
        {
            return;
        }

        Context.RaiseFileDrop(paths);
    }

    /// <summary>
    /// Calls <see cref="UiContext.RaiseContentScale(float2)"/> at once with the new content scale.
    /// </summary>
    /// <param name="scale">The new content scale of the window.</param>
    internal void PushContentScale(float2 scale)
    {
        if (_disposed)
        {
            return;
        }

        Context.RaiseContentScale(scale);
    }

    /// <summary>Checks the window and the context of the create info and returns the context.</summary>
    /// <param name="info">The create info.</param>
    /// <returns>The context of <paramref name="info"/>.</returns>
    /// <exception cref="ArgumentNullException">The window or the context is null.</exception>
    private static UiContext RequireContext(in CreateInfo info)
    {
        if (info.Window is null)
        {
            throw new ArgumentNullException(nameof(info), "The window of the create info is null.");
        }

        if (info.Context is null)
        {
            throw new ArgumentNullException(nameof(info), "The context of the create info is null.");
        }

        return info.Context;
    }

    /// <summary>Casts a GLFW position or offset to <see cref="float2"/> with no scale.</summary>
    /// <param name="value">The GLFW value.</param>
    /// <returns>The value as <see cref="float2"/>.</returns>
    private static float2 ToFloat2(double2 value)
    {
        return new float2((float)value.X, (float)value.Y);
    }

    /// <summary>
    /// Reads the modifier keys from the key state of the window: the left and right Shift, Control, Alt, and Super
    /// keys. The key state has no lock state, so the result never holds Caps Lock or Num Lock.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <returns>The modifier keys that are down.</returns>
    private static UiModifiers ReadModifiers(GlfwWindow window)
    {
        UiModifiers modifiers = UiModifiers.None;
        if (IsDown(window, GlfwKey.LeftShift) || IsDown(window, GlfwKey.RightShift))
        {
            modifiers |= UiModifiers.Shift;
        }

        if (IsDown(window, GlfwKey.LeftControl) || IsDown(window, GlfwKey.RightControl))
        {
            modifiers |= UiModifiers.Control;
        }

        if (IsDown(window, GlfwKey.LeftAlt) || IsDown(window, GlfwKey.RightAlt))
        {
            modifiers |= UiModifiers.Alt;
        }

        if (IsDown(window, GlfwKey.LeftSuper) || IsDown(window, GlfwKey.RightSuper))
        {
            modifiers |= UiModifiers.Super;
        }

        return modifiers;
    }

    /// <summary>Returns <see langword="true"/> when the key is down in the window.</summary>
    /// <param name="window">The window.</param>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when GLFW reports a press for the key.</returns>
    private static bool IsDown(GlfwWindow window, GlfwKey key)
    {
        return window.GetKey(key) == GlfwKeyAction.Press;
    }

    /// <summary>Converts a cursor move and pushes it.</summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="pos">The cursor position in screen coordinates.</param>
    private void HandleCursorPos(GlfwWindow window, double2 pos)
    {
        PushCursorMove(ToFloat2(pos), ReadModifiers(window), GlfwApi.Time);
    }

    /// <summary>Converts a button press or release and pushes it.</summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="button">The GLFW button.</param>
    /// <param name="action">The press or the release.</param>
    /// <param name="mods">The GLFW modifier bits.</param>
    private void HandleMouseButton(GlfwWindow window, GlfwMouseButton button, GlfwButtonAction action, GlfwModifier mods)
    {
        PushButton((int)button, action == GlfwButtonAction.Press, ButtonModifiers(mods), GlfwApi.Time);
    }

    /// <summary>
    /// Converts the GLFW modifier bits of a button event: the cast keeps the bits <c>0x3F</c> and uses no table.
    /// </summary>
    /// <param name="mods">The GLFW modifier bits.</param>
    /// <returns>The modifier keys.</returns>
    internal static UiModifiers ButtonModifiers(GlfwModifier mods)
    {
        return (UiModifiers)((int)mods & ButtonModifierMask);
    }

    /// <summary>Converts a scroll and pushes it.</summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="offset">The GLFW scroll offset.</param>
    private void HandleScroll(GlfwWindow window, double2 offset)
    {
        PushScroll(ToFloat2(offset), ReadModifiers(window), GlfwApi.Time);
    }

    /// <summary>Pushes a cursor leave when the cursor leaves the content area. An enter does nothing.</summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="entered"><see langword="true"/> for an enter, <see langword="false"/> for a leave.</param>
    private void HandleCursorEnter(GlfwWindow window, bool entered)
    {
        if (!entered)
        {
            PushCursorLeave(GlfwApi.Time);
        }
    }

    /// <summary>Gives the paths of a drop to the context.</summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="count">The number of paths.</param>
    /// <param name="paths">The paths. The window creates a new array for each drop.</param>
    private void HandleDrop(GlfwWindow window, int count, string[] paths)
    {
        int length = Math.Clamp(count, 0, paths.Length);
        PushFileDrop(new ReadOnlySpan<string>(paths, 0, length));
    }

    /// <summary>Gives a new content scale to the context.</summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="scale">The new content scale.</param>
    private void HandleContentScale(GlfwWindow window, float2 scale)
    {
        PushContentScale(scale);
    }
}
