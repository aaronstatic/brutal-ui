using System;
using System.Text;
using Brutal.GlfwApi;
using Brutal.Numerics;
using BrutalUi.Core;
using GlfwApi = Brutal.GlfwApi.Glfw;

namespace BrutalUi.Glfw;

/// <summary>
/// The GLFW input adapter. It converts the pointer, scroll, key, char, drop, and content-scale events of a
/// <see cref="GlfwWindow"/> into the events of <see cref="UiContext"/> and pushes them.
/// </summary>
/// <remarks>
/// <para>
/// The adapter adds one handler to each of eight window events: <see cref="GlfwWindow.OnCursorPos"/>,
/// <see cref="GlfwWindow.OnMouseButton"/>, <see cref="GlfwWindow.OnScroll"/>, <see cref="GlfwWindow.OnCursorEnter"/>,
/// <see cref="GlfwWindow.OnDrop"/>, <see cref="GlfwWindow.OnContentScale"/>, <see cref="GlfwWindow.OnKey"/>, and
/// <see cref="GlfwWindow.OnChar"/>. It does not use <see cref="GlfwWindow.OnCharMods"/>. A key event and a char event
/// have no target. The drain sends them to the focus owner of the context.
/// </para>
/// <para>
/// The ImGui rule: the ImGui <c>GlfwBackend</c> and this adapter both get each key event and each char event. The
/// binding has no consume rule. To give the keyboard to ImGui, set <see cref="KeyboardEnabled"/> to
/// <see langword="false"/> while ImGui wants the keyboard. The pointer events keep no switch.
/// </para>
/// <para>
/// The char limit of the binding: the native char callback of <see cref="GlfwWindow"/> casts the code point to
/// <see cref="ushort"/>. A code point above U+FFFF therefore arrives cut to its low 16 bits, and the adapter cannot
/// restore it. The adapter pushes the code point that it gets and drops a value that is not a Unicode scalar value.
/// </para>
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
/// The adapter only pushes the events into the ring of the context. <see cref="UiContext.Update(float)"/> drains the
/// ring at its start, before the style pass. Poll the events before the update. A full ring drops its oldest event.
/// </para>
/// <para>
/// The window catches an exception from a handler and gives it to the GLFW exception aggregator. The exception stops
/// the handlers of the same event that come after it. An exception from a handler of
/// <see cref="UiContext.OnFileDrop"/> or <see cref="UiContext.OnContentScale"/> goes this way.
/// </para>
/// <para>
/// The gamepad: the adapter reads one gamepad when the game calls <see cref="PollGamepad(int)"/>, and it adds no
/// handler for it. Call the method one time in each frame, after <c>Glfw.PollEvents</c>. Give the result to
/// <c>UiActionMap.PushGamepad</c> of <c>BrutalUi.Framework</c>. The adapter reads no axis and applies no deadzone. The
/// system can hold the Guide button, so the button can stay up. GLFW joystick state is global, so the poll needs no
/// window.
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

    /// <summary>The GLFW modifier bits that a button event and a key event keep: Shift, Control, Alt, Super, Caps Lock, and Num Lock.</summary>
    private const int ButtonModifierMask = 0x3F;

    /// <summary>The last GLFW joystick id (<c>GLFW_JOYSTICK_LAST</c>).</summary>
    private const int GamepadIdLast = 15;

    /// <summary>The number of GLFW gamepad buttons (<c>GLFW_GAMEPAD_BUTTON_LAST</c> plus 1).</summary>
    private const int GamepadButtonCount = 15;

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

    /// <summary>The handler of <see cref="GlfwWindow.OnKey"/>. The adapter creates it one time.</summary>
    private readonly GlfwKeyCallback _onKey;

    /// <summary>The handler of <see cref="GlfwWindow.OnChar"/>. The adapter creates it one time.</summary>
    private readonly GlfwCharCallback _onChar;

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
        window.OnKey += _onKey;
        window.OnChar += _onChar;
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
        _onKey = HandleKey;
        _onChar = HandleChar;
    }

    /// <summary>
    /// Gets the context that gets the events.
    /// </summary>
    public UiContext Context { get; }

    /// <summary>
    /// Gets or sets a value that tells the adapter to push the key events and the char events. The default is
    /// <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// With <see langword="false"/>, the adapter pushes no key event and no char event. The pointer, scroll, cursor
    /// leave, drop, and content-scale events still go.
    /// </para>
    /// <para>
    /// Use this switch to give the keyboard to ImGui. Set the value before <c>Glfw.PollEvents()</c>, for example
    /// <c>input.KeyboardEnabled = !io.WantCaptureKeyboard;</c>. The adapter reads the value when GLFW calls the
    /// handler.
    /// </para>
    /// <para>
    /// The switch does not keep the key state. A key down before the switch and a key up after it give an element a
    /// key down with no key up. A key down while the value is <see langword="false"/> and a key up after the value
    /// becomes <see langword="true"/> again give a key up with no key down.
    /// </para>
    /// </remarks>
    public bool KeyboardEnabled { get; set; } = true;

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
        window.OnKey -= _onKey;
        window.OnChar -= _onChar;
    }

    /// <summary>
    /// Reads the buttons of one gamepad and returns them as <see cref="UiGamepadButtons"/>. The game gives the result
    /// to <c>UiActionMap.PushGamepad</c>.
    /// </summary>
    /// <param name="gamepadId">The GLFW joystick id, from 0 (<c>GLFW_JOYSTICK_1</c>) to 15 (<c>GLFW_JOYSTICK_LAST</c>).</param>
    /// <returns>
    /// The buttons that are down. The result is <see cref="UiGamepadButtons.None"/> after <see cref="Dispose"/>, and
    /// when the joystick is absent or has no gamepad mapping.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="gamepadId"/> is below 0 or above 15.</exception>
    /// <remarks>
    /// The method checks the id first, then <see cref="Dispose"/>. An id outside the range throws also after
    /// <see cref="Dispose"/>. The method reads no axis and applies no deadzone. Call it on the main thread, after
    /// <c>Glfw.PollEvents</c>.
    /// </remarks>
    public UiGamepadButtons PollGamepad(int gamepadId = 0)
    {
        if (gamepadId < 0 || gamepadId > GamepadIdLast)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gamepadId),
                gamepadId,
                "The gamepad id must be from 0 to 15.");
        }

        if (_disposed)
        {
            return UiGamepadButtons.None;
        }

        if (!GlfwApi.GetGamepadState(gamepadId, out GlfwGamepadState state))
        {
            return UiGamepadButtons.None;
        }

        // The binding declares Buttons as a fixed buffer of 15 bytes. The local state is a fixed variable, so no pin is needed.
        unsafe
        {
            return GamepadButtons(new ReadOnlySpan<byte>(state.Buttons, GamepadButtonCount));
        }
    }

    /// <summary>
    /// Converts the button values of a GLFW gamepad state. A button with the value
    /// <see cref="GlfwButtonAction.Press"/> sets the bit <c>1 &lt;&lt; i</c> for its index <c>i</c>.
    /// </summary>
    /// <param name="buttons">
    /// The button values, one for each GLFW gamepad button, in the order of the GLFW button indices. The method reads
    /// the first 15 values and ignores the others.
    /// </param>
    /// <returns>The buttons that are down.</returns>
    internal static UiGamepadButtons GamepadButtons(ReadOnlySpan<byte> buttons)
    {
        int count = Math.Min(buttons.Length, GamepadButtonCount);
        int bits = 0;
        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == (byte)GlfwButtonAction.Press)
            {
                bits |= 1 << i;
            }
        }

        return (UiGamepadButtons)bits;
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

    /// <summary>
    /// Pushes a <see cref="UiEventType.KeyDown"/> for a press or a repeat, or a <see cref="UiEventType.KeyUp"/> for a
    /// release, with no target. The drain sends it to the focus owner.
    /// </summary>
    /// <remarks>
    /// The method returns at once after <see cref="Dispose"/> or when <see cref="KeyboardEnabled"/> is
    /// <see langword="false"/>. <see cref="KeyEvent.IsRepeat"/> is <see langword="true"/> only for a key down with
    /// <paramref name="repeat"/>.
    /// </remarks>
    /// <param name="key">The GLFW key value. The method keeps it with no table, also for the unknown key (-1).</param>
    /// <param name="scanCode">The system-specific scan code of the key.</param>
    /// <param name="down"><see langword="true"/> for a press or a repeat, <see langword="false"/> for a release.</param>
    /// <param name="repeat"><see langword="true"/> for a repeat.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <param name="timestamp">The time of the event, in seconds.</param>
    internal void PushKey(int key, int scanCode, bool down, bool repeat, UiModifiers modifiers, double timestamp)
    {
        if (_disposed || !KeyboardEnabled)
        {
            return;
        }

        UiEventType type = down ? UiEventType.KeyDown : UiEventType.KeyUp;
        Context.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(type, timestamp, null),
            Key = key,
            ScanCode = scanCode,
            Modifiers = modifiers,
            IsRepeat = down && repeat,
        });
    }

    /// <summary>
    /// Pushes a <see cref="UiEventType.Char"/> with no target. The drain sends it to the focus owner.
    /// </summary>
    /// <remarks>
    /// The method returns at once after <see cref="Dispose"/>, when <see cref="KeyboardEnabled"/> is
    /// <see langword="false"/>, or when <paramref name="codePoint"/> is not a Unicode scalar value: a surrogate
    /// (U+D800 to U+DFFF) or a value above U+10FFFF.
    /// </remarks>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="timestamp">The time of the event, in seconds.</param>
    internal void PushChar(uint codePoint, double timestamp)
    {
        if (_disposed || !KeyboardEnabled || !Rune.IsValid(codePoint))
        {
            return;
        }

        Context.PushEvent(new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, timestamp, null),
            CodePoint = codePoint,
        });
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
    /// Converts the GLFW modifier bits of a button event or a key event: the cast keeps the bits <c>0x3F</c> and uses
    /// no table.
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

    /// <summary>
    /// Converts a key press, repeat, or release and pushes it. The key casts to <see cref="int"/> with no table, also
    /// for <see cref="GlfwKey.Unknown"/>.
    /// </summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="key">The GLFW key.</param>
    /// <param name="scancode">The system-specific scan code.</param>
    /// <param name="action">The press, the repeat, or the release.</param>
    /// <param name="mods">The GLFW modifier bits.</param>
    private void HandleKey(GlfwWindow window, GlfwKey key, int scancode, GlfwKeyAction action, GlfwModifier mods)
    {
        KeyArgs(action, out bool down, out bool repeat);
        PushKey((int)key, scancode, down, repeat, ButtonModifiers(mods), GlfwApi.Time);
    }

    /// <summary>
    /// Converts a GLFW key action: <see cref="GlfwKeyAction.Release"/> gives a key up,
    /// <see cref="GlfwKeyAction.Press"/> gives a key down, and <see cref="GlfwKeyAction.Repeat"/> gives a key down with
    /// the repeat flag. The method allocates nothing.
    /// </summary>
    /// <param name="action">The press, the repeat, or the release.</param>
    /// <param name="down"><see langword="true"/> for a key down, <see langword="false"/> for a key up.</param>
    /// <param name="repeat"><see langword="true"/> when the system repeats a held key.</param>
    internal static void KeyArgs(GlfwKeyAction action, out bool down, out bool repeat)
    {
        down = action != GlfwKeyAction.Release;
        repeat = action == GlfwKeyAction.Repeat;
    }

    /// <summary>Converts a char and pushes it.</summary>
    /// <param name="window">The window that sends the event.</param>
    /// <param name="codepoint">The code point. The binding cuts a value above U+FFFF to 16 bits.</param>
    private void HandleChar(GlfwWindow window, uint codepoint)
    {
        PushChar(codepoint, GlfwApi.Time);
    }
}
