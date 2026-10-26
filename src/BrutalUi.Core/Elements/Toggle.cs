using System;

namespace BrutalUi.Core;

/// <summary>
/// An element that holds a checked state. A left click on the toggle, or a Space key down with the focus on it, flips
/// <see cref="Checked"/> and calls <see cref="CheckedChanged"/> one time.
/// </summary>
/// <remarks>
/// <para>
/// The toggle has no text (Q5). The game puts a <see cref="Label"/> next to it.
/// </para>
/// <para>
/// The toggle has no own size (Decision 9). The size comes from a style rule or from <see cref="Element.Inline"/>. The
/// constructor sets no inline size, because an inline value wins over each rule. A toggle with no size rule takes no
/// space and catches no click. The toggle has no measure callback and no paint override. The paint walk draws its fill
/// and its border from the style. A rule with <c>:checked</c> restyles the toggle when <see cref="Checked"/> is
/// <see langword="true"/>.
/// </para>
/// <para>
/// <see cref="Checked"/> is the <see cref="PseudoStates.Checked"/> bit of <see cref="Element.States"/> (Decision 4). The
/// toggle holds no second copy of the value.
/// </para>
/// <para>
/// Only user input calls <see cref="CheckedChanged"/> (Q4). A set of <see cref="Checked"/> from code changes the bit
/// and calls nothing.
/// </para>
/// <para>
/// The click rule is the rule of <see cref="Button"/>: a left press and a left release on the toggle. A flip stops the
/// walk of the event (A4). The key rule: a focused toggle flips on a Space key down (A3). An Enter key does nothing and
/// walks on. The same limits as for a button apply to a repeat, a modifier, and a key from a child.
/// </para>
/// <para>
/// A toggle that holds <see cref="PseudoStates.Disabled"/> does not flip, by a pointer or by a key, and it does not stop
/// the walk. The event walks on to the ancestors. <see cref="PseudoStates.Disabled"/> blocks user input only: a set of
/// <see cref="Checked"/> from code still works on a disabled toggle.
/// </para>
/// <para>
/// The constructor sets <see cref="Element.IsFocusable"/> to <see langword="true"/> (A5). It writes the handlers into
/// <see cref="Element.OnPointerUp"/> and <see cref="Element.OnKeyDown"/>. A write of <c>=</c> to a slot removes that
/// input.
/// </para>
/// <para>
/// Read <see cref="CheckedChanged"/>, not the slot. A flip stops the walk, and the stop also skips a later delegate of
/// the same slot. A handler that a game adds with <c>+=</c> to <see cref="Element.OnPointerUp"/> or
/// <see cref="Element.OnKeyDown"/> of a toggle gets no call for a flip. Read <see cref="CheckedChanged"/>, or use a
/// capture handler on an ancestor.
/// </para>
/// <para>
/// A pickable child of the toggle gets <see cref="PseudoStates.Active"/> in place of the toggle. A press on that child
/// then gives no flip. Set <see cref="Element.IsPickable"/> to <see langword="false"/> on each child of the toggle.
/// </para>
/// <para>
/// A <c>PointerUp</c> with a caller target does not end a press. When the toggle holds
/// <see cref="PseudoStates.Active"/>, that event flips the toggle, and the real release flips it again. Push a pointer
/// event with no target to flip a toggle from code.
/// </para>
/// <para>
/// The limit of a chord: <see cref="PseudoStates.Active"/> marks the element of the first press of a gesture, and it
/// stays until the last button goes up. A left release then flips a toggle that holds
/// <see cref="PseudoStates.Active"/>, also when the left press of that release was on another element.
/// </para>
/// </remarks>
public sealed class Toggle : Element
{
    /// <summary>The type id of each toggle.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("Toggle");

    /// <summary>
    /// The release handler of each toggle. The delegate has no target, so each toggle shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerUp = HandlePointerUp;

    /// <summary>
    /// The key down handler of each toggle. The delegate has no target, so each toggle shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<KeyEvent> s_keyDown = HandleKeyDown;

    /// <summary>Creates a toggle.</summary>
    /// <remarks>
    /// The constructor sets <see cref="Element.IsFocusable"/> to <see langword="true"/>, writes the handlers, and
    /// writes the start value through <see cref="Element.States"/>. It calls <see cref="CheckedChanged"/> never.
    /// </remarks>
    /// <param name="info">The create-info.</param>
    public Toggle(in CreateInfo info)
    {
        IsFocusable = true;
        OnPointerUp = s_pointerUp;
        OnKeyDown = s_keyDown;
        Checked = info.Checked;
    }

    /// <summary>Gets the type id of the toggle: <c>Toggle</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>
    /// Gets or sets a value that tells if the toggle is checked. The value is the <see cref="PseudoStates.Checked"/>
    /// bit of <see cref="Element.States"/>.
    /// </summary>
    /// <remarks>
    /// A set from code calls no <see cref="CheckedChanged"/> (Q4). A set to a new value restyles the toggle through
    /// <see cref="Element.States"/>. A set to the same value marks nothing.
    /// </remarks>
    public bool Checked
    {
        get => (States & PseudoStates.Checked) != 0;
        set => States = value ? States | PseudoStates.Checked : States & ~PseudoStates.Checked;
    }

    /// <summary>
    /// Gets or sets the delegate that a flip by the user calls. The delegate gets the toggle as the sender.
    /// </summary>
    /// <remarks>
    /// Only user input calls the delegate. The toggle stops the walk of the event, flips <see cref="Checked"/>, and then
    /// calls the delegate. The delegate reads the new value. A toggle with no delegate still flips and stops the walk.
    /// </remarks>
    public Action<Toggle>? CheckedChanged { get; set; }

    /// <summary>
    /// The click handler. It flips the toggle when the release is a click on the toggle.
    /// </summary>
    /// <param name="e">The release event.</param>
    private static void HandlePointerUp(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not Toggle toggle || !WidgetInput.IsClick(toggle, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        toggle.Flip();
    }

    /// <summary>
    /// The key handler. It flips the toggle when the key is a Space key down on the toggle.
    /// </summary>
    /// <param name="e">The key event.</param>
    private static void HandleKeyDown(ref KeyEvent e)
    {
        if (e.Header.CurrentTarget is not Toggle toggle
            || !WidgetInput.IsKeyActivation(toggle, in e, ActivationKeys.Space))
        {
            return;
        }

        e.Header.StopPropagation();
        toggle.Flip();
    }

    /// <summary>The flip step that the pointer handler and the key handler share. It calls <see cref="CheckedChanged"/>.</summary>
    private void Flip()
    {
        Checked = !Checked;
        CheckedChanged?.Invoke(this);
    }

    /// <summary>The create-info of a <see cref="Toggle"/>.</summary>
    public struct CreateInfo
    {
        /// <summary>The start value of <see cref="Toggle.Checked"/>. The default is <see langword="false"/>.</summary>
        public bool Checked;
    }
}
