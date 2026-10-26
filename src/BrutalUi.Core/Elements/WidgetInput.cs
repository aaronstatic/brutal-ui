using System;

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
/// and <see cref="Toggle"/> use it. A widget with another rule (a drag, an arrow key, or a char) writes its own
/// check.
/// </para>
/// <para>
/// A check reads the event and the states of the widget only. It changes no state, it does not stop the walk, and it
/// allocates nothing.
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
    /// <see cref="PointerButton.Left"/>, and <see cref="Element.States"/> of the widget holds
    /// <see cref="PseudoStates.Active"/>, and the widget does not hold <see cref="PseudoStates.Disabled"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The release clears <see cref="PseudoStates.Active"/> after its walk (Decision 2 of button-and-toggle), so a
    /// release handler reads <see cref="PseudoStates.Active"/> on the pressed element. Thus a click needs a press on
    /// the widget and a release that walks to the widget.
    /// </para>
    /// <para>
    /// <see cref="PseudoStates.Active"/> marks the first press of a gesture, so a chord of two buttons can give a click
    /// after a left press on another element.
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

        return e.Button == PointerButton.Left && (widget.States & PseudoStates.Active) != 0;
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
