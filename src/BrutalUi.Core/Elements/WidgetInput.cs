using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The shared input checks of the widgets. Each widget calls a check at the start of its handler, and it acts only
/// when the check returns <see langword="true"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Button"/> and the later widgets of the interactive-elements epic derive from <see cref="Element"/>
/// directly. This class holds the checks that they share, so each widget applies the same click rule. There is no
/// abstract base class (Decision 1 of button-and-toggle).
/// </para>
/// <para>
/// <see cref="IsClick(Element, in PointerEvent)"/> is the click rule of each widget that clicks: <see cref="Button"/>
/// and <see cref="Toggle"/> use it. A widget with another rule (a drag, an arrow key, or a char) uses the check of
/// that rule.
/// </para>
/// <para>
/// <see cref="IsDragStart(Element, in PointerEvent)"/> and <see cref="IsDragMove(Element, in PointerEvent)"/> serve
/// each widget with a drag rule.
/// </para>
/// <para>
/// <see cref="IsScroll(Element, in ScrollEvent)"/> serves each widget that takes the wheel. It reads no press target.
/// </para>
/// <para>
/// <see cref="IsCharInput(Element, in CharEvent)"/> and <see cref="IsEditKey(Element, in KeyEvent)"/> serve each
/// widget that takes text. They read no press target.
/// </para>
/// <para>
/// The pointer checks read <see cref="UiContext.CurrentPressTarget"/>, the press target of the current walk
/// (Decision 13 of slider). The drag checks read the same press target, so a chord gives no click and no drag.
/// </para>
/// <para>
/// A check reads the event, the states of the widget, and the press target only. It changes no state, it does not stop
/// the walk, and it allocates nothing.
/// </para>
/// <para>
/// Each check returns <see langword="false"/> for a widget that holds <see cref="PseudoStates.Disabled"/> (Decision 6
/// of button-and-toggle). The handler then returns with no stop, so the event walks on to the ancestors. The check lives
/// here only, and no widget has its own <see cref="PseudoStates.Disabled"/> check.
/// </para>
/// </remarks>
internal static class WidgetInput
{
    /// <summary>
    /// Tells if a <see cref="UiEventType.PointerUp"/> is a click on the widget: the button is
    /// <see cref="PointerButton.Left"/>, <see cref="UiContext.CurrentPressTarget"/> is the widget, and the widget does
    /// not hold <see cref="PseudoStates.Disabled"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check also reads the event type, so a copy of the handler in another slot does nothing. The drain sets the
    /// press target for a press and a move too, and the type check stops a click during the press.
    /// </para>
    /// <para>
    /// The drain sets the press target for the walk of the release that ends the press (Decision 13 of slider). The
    /// press target is the element of the left press with no chord. Thus a click needs a left press on the widget and
    /// a release that walks to the widget.
    /// </para>
    /// <para>
    /// The chord rule: a click needs the left press that set <see cref="PseudoStates.Active"/>, with no other button
    /// down during the gesture. A chord of two buttons gives no click, in each press order and each release order. A
    /// <see cref="UiEventType.PointerUp"/> with a target from the caller gives no click, because the drain gives it no
    /// press target. The check no longer reads <see cref="PseudoStates.Active"/> (this amends Decision 2 of
    /// button-and-toggle).
    /// </para>
    /// <para>
    /// Only the left button clicks (A1). A press or a release of another button gives no click.
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The release event.</param>
    /// <returns><see langword="true"/> when the release is a click on the widget.</returns>
    internal static bool IsClick(Element widget, in PointerEvent e)
    {
        if ((widget.States & PseudoStates.Disabled) != 0)
        {
            return false;
        }

        return e.Header.Type == UiEventType.PointerUp
            && e.Button == PointerButton.Left
            && ReferenceEquals(UiContext.CurrentPressTarget, widget);
    }

    /// <summary>
    /// Tells if a <see cref="UiEventType.PointerDown"/> starts a drag on the widget: the button is
    /// <see cref="PointerButton.Left"/> and <see cref="UiContext.CurrentPressTarget"/> is the widget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The drain sets the press target for the walk of the left press with no chord (Decision 13 of slider). Thus a
    /// right press, a chord press, and a press that hits another element give no drag start.
    /// </para>
    /// <para>
    /// A widget that holds <see cref="PseudoStates.Disabled"/> gets <see langword="false"/> (Decision 11 of slider).
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The press event.</param>
    /// <returns><see langword="true"/> when the press starts a drag on the widget.</returns>
    internal static bool IsDragStart(Element widget, in PointerEvent e)
    {
        return (widget.States & PseudoStates.Disabled) == 0
            && e.Header.Type == UiEventType.PointerDown
            && e.Button == PointerButton.Left
            && ReferenceEquals(UiContext.CurrentPressTarget, widget);
    }

    /// <summary>
    /// Tells if a <see cref="UiEventType.PointerMove"/> is a drag move on the widget: the type is
    /// <see cref="UiEventType.PointerMove"/> and <see cref="UiContext.CurrentPressTarget"/> is the widget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The press target rule: the drain gives the press target to the walk of a move. The press target is the element of
    /// a left press with no chord (Decisions 2 and 13 of slider). Thus a right press gives no drag, and a chord of two
    /// buttons gives no drag until the last button goes up. A move with no press and a move after the release give no
    /// drag.
    /// </para>
    /// <para>
    /// The check does not read <see cref="PointerEvent.Button"/>, because the walk does not set it for a move. A widget
    /// that holds <see cref="PseudoStates.Disabled"/> gets <see langword="false"/> (Decision 11 of slider). A disable
    /// during a drag thus ends the drag at once.
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The move event.</param>
    /// <returns><see langword="true"/> when the move drags the widget.</returns>
    internal static bool IsDragMove(Element widget, in PointerEvent e)
    {
        return (widget.States & PseudoStates.Disabled) == 0
            && e.Header.Type == UiEventType.PointerMove
            && ReferenceEquals(UiContext.CurrentPressTarget, widget);
    }

    /// <summary>
    /// Tells if a <see cref="UiEventType.Scroll"/> is a wheel scroll for the widget: the type is
    /// <see cref="UiEventType.Scroll"/>, and the widget does not hold <see cref="PseudoStates.Disabled"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check reads the type and the disabled bit only. It does not read the target, so a scroll on a child of the
    /// widget counts. The scroll then bubbles from the child to the widget.
    /// </para>
    /// <para>
    /// The check reads no press target, because a scroll is not part of a press.
    /// </para>
    /// <para>
    /// The check also reads the event type, as the other checks do. Each <see cref="ScrollEvent"/> has the type
    /// <see cref="UiEventType.Scroll"/>, so the check does not tell <c>OnScroll</c> from <c>OnScrollCapture</c>.
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The scroll event.</param>
    /// <returns><see langword="true"/> when the scroll is for the widget.</returns>
    internal static bool IsScroll(Element widget, in ScrollEvent e)
    {
        return (widget.States & PseudoStates.Disabled) == 0
            && e.Header.Type == UiEventType.Scroll;
    }

    /// <summary>
    /// Gives the viewport position of the border box of an element: the sum of the rect positions of the element and
    /// each ancestor, minus the applied offset of each ancestor <see cref="ScrollView"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the origin rule of the hit walk and of the draw walk. A <see cref="ScrollView"/> moves its children by
    /// <see cref="ScrollView.AppliedOffset"/>, and not its own border box. Thus the method subtracts the offset of each
    /// ancestor view, and not the offset of the element itself. In a view in a view, the offsets add up. Keep the three
    /// rules the same. <c>ScrollViewSliderTests</c> holds a slider in a scrolled view.
    /// </para>
    /// <para>
    /// The method calls <see cref="ScrollView.AppliedOffset"/> and never reads the stored offset, so it applies the
    /// offset that the walks apply.
    /// </para>
    /// </remarks>
    /// <param name="element">The element.</param>
    /// <returns>The viewport position of the top left corner of the border box.</returns>
    internal static float2 ViewportOrigin(Element element)
    {
        float x = 0f;
        float y = 0f;
        for (Element? current = element; current is not null; current = current.Parent)
        {
            float4 r = current.Layout.Rect;
            x += r.X;
            y += r.Y;

            // An ancestor view moves the element by its offset. The view does not move itself.
            if (!ReferenceEquals(current, element) && current is ScrollView view)
            {
                float2 offset = view.AppliedOffset();
                x -= offset.X;
                y -= offset.Y;
            }
        }

        return new float2(x, y);
    }

    /// <summary>
    /// Tells if a <see cref="UiEventType.KeyDown"/> is a key activation of the widget: the widget is the target of the
    /// event, the event is not a repeat, no <see cref="UiModifiers.Control"/>, <see cref="UiModifiers.Alt"/>, or
    /// <see cref="UiModifiers.Super"/> is down, the key is in <paramref name="keys"/>, and the widget does not hold
    /// <see cref="PseudoStates.Disabled"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The key route gives each key with no target to the focus owner, so the owner is the target (Decision 5 of
    /// button-and-toggle). A key that bubbles from a focusable child has another target, so it does not activate the
    /// widget. The check does not read <see cref="PseudoStates.Focus"/>.
    /// </para>
    /// <para>
    /// <see cref="UiModifiers.Shift"/> does not block the activation. The keypad Enter key does not activate.
    /// </para>
    /// <para>
    /// The check also reads the event type, so a copy of the handler in another slot does nothing.
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The key event.</param>
    /// <param name="keys">The keys that activate the widget.</param>
    /// <returns><see langword="true"/> when the key activates the widget.</returns>
    internal static bool IsKeyActivation(Element widget, in KeyEvent e, ActivationKeys keys)
    {
        if ((widget.States & PseudoStates.Disabled) != 0
            || e.Header.Type != UiEventType.KeyDown
            || e.IsRepeat
            || !ReferenceEquals(e.Header.Target, widget)
            || (e.Modifiers & (UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super)) != 0)
        {
            return false;
        }

        return e.Key switch
        {
            SpaceKey => (keys & ActivationKeys.Space) != 0,
            EnterKey => (keys & ActivationKeys.Enter) != 0,
            _ => false,
        };
    }

    /// <summary>
    /// Tells if a <see cref="UiEventType.KeyDown"/> is a Left or Right arrow key for the widget: the widget is the target
    /// of the event, no <see cref="UiModifiers.Control"/>, <see cref="UiModifiers.Alt"/>, or
    /// <see cref="UiModifiers.Super"/> is down, and the key is <see cref="LeftKey"/> or <see cref="RightKey"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A repeat passes (Decision 8 of slider), so a held key moves the widget again. <see cref="UiModifiers.Shift"/> does
    /// not block the key. The target rule and the modifier rule are the rules of
    /// <see cref="IsKeyActivation(Element, in KeyEvent, ActivationKeys)"/>.
    /// A widget that holds <see cref="PseudoStates.Disabled"/> gets <see langword="false"/> (Decision 11 of slider).
    /// </para>
    /// <para>
    /// The check also reads the event type, so a copy of the handler in another slot does nothing.
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The key event.</param>
    /// <param name="direction">The direction: -1 for the Left key, +1 for the Right key, and 0 when the result is false.</param>
    /// <returns><see langword="true"/> when the key is an arrow key for the widget.</returns>
    internal static bool IsArrowKey(Element widget, in KeyEvent e, out int direction)
    {
        direction = 0;
        if ((widget.States & PseudoStates.Disabled) != 0
            || e.Header.Type != UiEventType.KeyDown
            || !ReferenceEquals(e.Header.Target, widget)
            || (e.Modifiers & (UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super)) != 0)
        {
            return false;
        }

        switch (e.Key)
        {
            case RightKey:
                direction = 1;
                return true;
            case LeftKey:
                direction = -1;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Tells if a <see cref="UiEventType.Char"/> is text input for the widget: the type is
    /// <see cref="UiEventType.Char"/>, the widget is the target of the event, and the widget does not hold
    /// <see cref="PseudoStates.Disabled"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check reads the disabled bit, the type, and the target only. The key route gives each char with no target to
    /// the focus owner, so the owner is the target. A char that bubbles from a focusable child has another target, so
    /// it gives no input to the widget. A char with the widget as the target from the caller counts, also when the widget
    /// does not hold the focus. The check does not read <see cref="PseudoStates.Focus"/>.
    /// </para>
    /// <para>
    /// The check reads no press target and no modifier, because a char has neither. It does not read the code point.
    /// The widget applies its own rule to the code point.
    /// </para>
    /// <para>
    /// The check also reads the event type, so a copy of the handler in another slot does nothing.
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The char event.</param>
    /// <returns><see langword="true"/> when the char is text input for the widget.</returns>
    internal static bool IsCharInput(Element widget, in CharEvent e)
    {
        return (widget.States & PseudoStates.Disabled) == 0
            && e.Header.Type == UiEventType.Char
            && ReferenceEquals(e.Header.Target, widget);
    }

    /// <summary>
    /// Tells if a <see cref="UiEventType.KeyDown"/> is an edit key for the widget: the type is
    /// <see cref="UiEventType.KeyDown"/>, the widget is the target of the event, no <see cref="UiModifiers.Control"/>,
    /// <see cref="UiModifiers.Alt"/>, or <see cref="UiModifiers.Super"/> is down, and the widget does not hold
    /// <see cref="PseudoStates.Disabled"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The target rule: the key route gives each key with no target to the focus owner, so the owner is the target. A
    /// key that bubbles from a focusable child has another target, so it is not an edit key for the widget. A key with
    /// the widget as the target from the caller counts, also when the widget does not hold the focus. The check does not
    /// read <see cref="PseudoStates.Focus"/>.
    /// </para>
    /// <para>
    /// The modifier rule: <see cref="UiModifiers.Control"/>, <see cref="UiModifiers.Alt"/>, and
    /// <see cref="UiModifiers.Super"/> block the key, so a shortcut walks on to the ancestors.
    /// <see cref="UiModifiers.Shift"/>, <see cref="UiModifiers.CapsLock"/>, and <see cref="UiModifiers.NumLock"/> do not
    /// block the key.
    /// </para>
    /// <para>
    /// A repeat passes, so a held key moves the caret or removes text again.
    /// </para>
    /// <para>
    /// The check does not read the key. The widget applies its own rule to the key. The check also reads the event type,
    /// so a copy of the handler in another slot does nothing.
    /// </para>
    /// </remarks>
    /// <param name="widget">The widget that the handler runs on: the current target of the walk.</param>
    /// <param name="e">The key event.</param>
    /// <returns><see langword="true"/> when the key is an edit key for the widget.</returns>
    internal static bool IsEditKey(Element widget, in KeyEvent e)
    {
        return (widget.States & PseudoStates.Disabled) == 0
            && e.Header.Type == UiEventType.KeyDown
            && ReferenceEquals(e.Header.Target, widget)
            && (e.Modifiers & (UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super)) == 0;
    }

    /// <summary>The GLFW key code of the Backspace key (<c>GLFW_KEY_BACKSPACE</c>).</summary>
    internal const int BackspaceKey = 259;

    /// <summary>The GLFW key code of the Delete key (<c>GLFW_KEY_DELETE</c>).</summary>
    internal const int DeleteKey = 261;

    /// <summary>The GLFW key code of the Home key (<c>GLFW_KEY_HOME</c>).</summary>
    internal const int HomeKey = 268;

    /// <summary>The GLFW key code of the End key (<c>GLFW_KEY_END</c>).</summary>
    internal const int EndKey = 269;

    /// <summary>The GLFW key code of the right arrow key (<c>GLFW_KEY_RIGHT</c>).</summary>
    internal const int RightKey = 262;

    /// <summary>The GLFW key code of the left arrow key (<c>GLFW_KEY_LEFT</c>).</summary>
    internal const int LeftKey = 263;

    /// <summary>The GLFW key code of the space key (<c>GLFW_KEY_SPACE</c>).</summary>
    internal const int SpaceKey = 32;

    /// <summary>The GLFW key code of the main Enter key (<c>GLFW_KEY_ENTER</c>). The keypad Enter key is 335.</summary>
    internal const int EnterKey = 257;
}

/// <summary>The keys that activate a widget.</summary>
[Flags]
internal enum ActivationKeys
{
    /// <summary>No key.</summary>
    None = 0,

    /// <summary>The space key.</summary>
    Space = 1,

    /// <summary>The main Enter key.</summary>
    Enter = 2,
}
