using System;

namespace BrutalUi.Core;

/// <summary>
/// A handler for an input that no element took. <see cref="UiContext"/> calls it from the drain of the input pass.
/// </summary>
/// <param name="type">
/// The type of the input. The value is <see cref="UiEventType.KeyDown"/> or <see cref="UiEventType.PointerDown"/>.
/// </param>
/// <param name="code">
/// The GLFW key code of a key down. For a press, the GLFW mouse button value (<c>(int)</c> of
/// <see cref="PointerButton"/>).
/// </param>
/// <param name="modifiers">The modifier keys that are down at the time of the input.</param>
/// <param name="isRepeat"><see langword="true"/> for a key repeat. A press always gives <see langword="false"/>.</param>
/// <remarks>
/// <para>
/// A6 of state-and-commands: the action map reads its input here. The map lives in a higher layer, and Core names no
/// type of that layer. Thus the seam is one delegate slot on the context: <see cref="UiContext.OnUnhandledInput"/>.
/// </para>
/// <para>
/// The drain call: the drain calls the handler for a <see cref="UiEventType.KeyDown"/> entry with no target from the
/// caller, when no handler stopped its walk and the key is not a Tab move. A key down with no focus owner gets no
/// walk, and it reaches the handler. The call runs inside the drain, at the position of the entry in the ring. It
/// runs after the walk, after the end of a release, and after the Tab move. Thus the handler reads the focus of that
/// point in the ring. A Tab or a Shift+Tab that moves the focus is a handled key. A Control+Tab is not a Tab move, so
/// it reaches the handler.
/// </para>
/// <para>
/// The press call: the drain calls the handler for a <see cref="UiEventType.PointerDown"/> entry with no target from
/// the caller, only when the press hit no element. The routing gate also reads such a press as a game pointer: it
/// gives <see langword="false"/> for <see cref="UiRoutingGate.PointerOverUi"/>. A press on an element never
/// goes to the handler, also when no handler of the element stops it. A hit takes a press, as a stopped walk takes a
/// key. The click focus rule runs first, so the press clears the focus before the handler call. Each button runs the
/// rule. The call gets the button, the modifiers, and <see langword="false"/> for the repeat flag.
/// </para>
/// <para>
/// The slot rules: the handler gets no key up, no char, no pointer move, no release, no scroll, and no entry with a
/// target from the caller. An exception of the handler leaves <see cref="UiContext.Update(float)"/>, as an exception
/// of an element handler does. The entries after it stay in the ring for the next update. The handler runs while the
/// drain runs, so a call of <see cref="UiContext.Update(float)"/> from the handler throws
/// <see cref="InvalidOperationException"/>. A call of <see cref="UiContext.FocusNext"/>,
/// <see cref="UiContext.FocusPrevious"/>, or <see cref="UiContext.SetFocus(Element)"/> from the handler is valid. An
/// event that the handler pushes waits for the next drain.
/// </para>
/// <para>
/// The build-time rule: create the delegate one time, and set the slot at build time, not in the frame loop. A new
/// delegate in each frame allocates.
/// </para>
/// </remarks>
public delegate void UiUnhandledInputHandler(UiEventType type, int code, UiModifiers modifiers, bool isRepeat);
