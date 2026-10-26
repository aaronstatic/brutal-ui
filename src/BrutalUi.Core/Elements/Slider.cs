using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// An element that holds a number in a range. A left press on the slider moves <see cref="Value"/> to the pointer and
/// calls <see cref="ValueChanged"/> one time when the value changes.
/// </summary>
/// <remarks>
/// <para>
/// The press rule: a left press with no chord (Decision 13) moves the value. The rule reads the press target of the
/// context through <c>WidgetInput.IsDragStart</c>. A right press, a chord press, and a press on a child of the slider
/// give no move.
/// </para>
/// <para>
/// The map of the thumb center (Decision 6): the thumb is as wide as the smaller of the content height and the content
/// width, and as tall as the content box. It is square when the content box is at least as wide as tall. A slider
/// narrower than tall has no travel, so a press and a drag give <see cref="Minimum"/>. The center of the thumb follows
/// the pointer. The ratio is
/// <c>(x - content.X - thumbWidth / 2) / (content.Width - thumbWidth)</c>, clamped to 0 and 1. The ratio is 0 when the
/// travel is 0 or less. The value is <see cref="Minimum"/> plus the ratio times the range. A press on the thumb maps
/// in the same way, with no grab offset.
/// </para>
/// <para>
/// A move stops the walk of the event (A4 of button-and-toggle). The stop also skips a later delegate of the same slot.
/// The slider calls <see cref="ValueChanged"/> after the stop and after the change, and it passes itself as the sender.
/// </para>
/// <para>
/// The drag rule: a pointer move while the slider holds the left press moves the value to the pointer. The rule reads
/// the press target of the context through <c>WidgetInput.IsDragMove</c>, and the slider holds no drag flag
/// (Decision 2). Each drag move stops the walk, also when the value does not change, because the slider uses the move.
/// A move with no press, a move after the release, and a move after a press on another element give no drag, and the
/// move walks on.
/// </para>
/// <para>
/// The press button rule: only a left press with no other button down starts a drag. A right press sets
/// <see cref="PseudoStates.Active"/> but gives no press target, so a later move gives no drag. A second button during a
/// drag ends the drag until the last button goes up. A new left press then starts a new drag.
/// </para>
/// <para>
/// The capture rule (Decision 3): the slider implements <see cref="IDragCapture"/>. While the slider holds the left
/// press with no chord, each pointer move with no target walks to the slider, also when the pointer leaves its rect.
/// A drag past an end thus gives the minimum or the maximum, and a drag above or below the slider reads the x only.
/// <see cref="PseudoStates.Hover"/>, <see cref="UiContext.PointerHit"/>, and the routing gate still follow the hit.
/// The release of the last button ends the capture. A right press and a chord give no capture.
/// </para>
/// <para>
/// Read <see cref="ValueChanged"/>, not the slot. A handler that a game adds with <c>+=</c> to
/// <see cref="Element.OnPointerDown"/> of a slider gets no call for a press on the slider. Read
/// <see cref="ValueChanged"/>, or use a capture handler on an ancestor.
/// </para>
/// <para>
/// A write of <c>=</c> to <see cref="Element.OnPointerDown"/> removes the press. The slider then ignores the pointer.
/// </para>
/// <para>
/// Only user input calls <see cref="ValueChanged"/> (Decision 5). A set of <see cref="Value"/> from code, the start
/// value of the create-info, and <see cref="SetRange"/> apply the value rule and call nothing.
/// </para>
/// <para>
/// The value rule (Decision 4): each path that writes the value uses it. A NaN gives <see cref="Minimum"/>. The number is
/// clamped to the range. When <see cref="Step"/> is above 0, the number is rounded to the nearest grid point
/// <c>Minimum + k * Step</c>, and a midpoint rounds away from zero. The number is then clamped again. The code set, the
/// create-info, <see cref="SetRange"/>, the press, the drag, and the keys follow the same rule.
/// </para>
/// <para>
/// The grid starts at <see cref="Minimum"/>, as in an HTML range input. A range that is not a multiple of the step
/// reaches <see cref="Maximum"/> only when the nearest grid point is at or past it. The range 0 to 10 with the step 3
/// stops at 9. Use a range that is a multiple of the step.
/// </para>
/// <para>
/// The range is read-only. <see cref="SetRange"/> changes the range and the step as one. The constructor and
/// <see cref="SetRange"/> throw <see cref="ArgumentOutOfRangeException"/> for a bound that is not finite, a minimum above
/// the maximum, a range with a width that is not finite (<c>maximum - minimum</c> overflows, as in -3e38 to 3e38), and a
/// step that is negative or not finite. A range of zero width is valid, and the value is then <see cref="Minimum"/>.
/// </para>
/// <para>
/// The key rule (Decision 8): a Left or Right arrow key down moves the value by one <see cref="Step"/>, or by one
/// percent of the range when the step is 0. The slider must be the target of the key, so a key that bubbles from a
/// focusable child gives no move. <see cref="UiModifiers.Control"/>, <see cref="UiModifiers.Alt"/>, and
/// <see cref="UiModifiers.Super"/> block the key. <see cref="UiModifiers.Shift"/> does not. A repeat key moves the value
/// again. A key that the slider uses stops the walk, also at the end of the range, where the value does not change and
/// <see cref="ValueChanged"/> gets no call. The slider ignores Up, Down, Space, Enter, and a key up, and they walk on.
/// </para>
/// <para>
/// A write of <c>=</c> to <see cref="Element.OnKeyDown"/> removes the keys.
/// </para>
/// <para>
/// The disabled rule (Decision 11): a slider that holds <see cref="PseudoStates.Disabled"/> takes no press, no drag,
/// and no key. It does not stop the walk, so the event goes on to the ancestors. It captures no move. A disable during
/// a drag ends the drag at once, and the next move walks to the hit. A set of <see cref="Value"/> from code and
/// <see cref="SetRange"/> still work. The checks live in <c>WidgetInput</c>, and the slider handlers have none.
/// </para>
/// <para>
/// The slider has no own size (Decision 10). The size comes from a style rule or from <see cref="Element.Inline"/>. The
/// slider has no measure callback. A slider with no size rule takes no space and catches no press.
/// </para>
/// <para>
/// The constructor sets <see cref="Element.IsFocusable"/> to <see langword="true"/>, so a press focuses the slider.
/// </para>
/// <para>
/// The draw (Decision 7): the track is the box of the slider, so the background, the border, and the corner radius of
/// the slider paint the track. The thumb is one rect in the content box. It uses the <c>color</c> of the style, which
/// is opaque white by default. The rules <c>Slider:hover</c>, <c>Slider:active</c>, <c>Slider:focus</c>, and
/// <c>:disabled</c> style the thumb through <c>color</c>. A change of the value marks <see cref="DirtyFlags.Paint"/>
/// only, so the layout does not change.
/// </para>
/// </remarks>
public sealed class Slider : Element, IDragCapture
{
    /// <summary>The type id of each slider.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("Slider");

    /// <summary>The name of the constructor parameter. A bad create-info range throws with this name.</summary>
    private const string InfoName = "info";

    /// <summary>
    /// The press handler of each slider. The delegate has no target, so each slider shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerDown = HandlePointerDown;

    /// <summary>
    /// The move handler of each slider. The delegate has no target, so each slider shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerMove = HandlePointerMove;

    /// <summary>
    /// The key handler of each slider. The delegate has no target, so each slider shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<KeyEvent> s_keyDown = HandleKeyDown;

    /// <summary>The lower end of the range.</summary>
    private float _minimum;

    /// <summary>The upper end of the range.</summary>
    private float _maximum;

    /// <summary>The step of the grid. A step of 0 means no grid.</summary>
    private float _step;

    /// <summary>The current value. It is always inside the range.</summary>
    private float _value;

    /// <summary>Creates a slider.</summary>
    /// <remarks>
    /// The constructor checks the range first (Decision 9). It sets <see cref="Element.IsFocusable"/> to
    /// <see langword="true"/>, writes the handlers, and writes the start value through the same rule as
    /// <see cref="Value"/>. It calls <see cref="ValueChanged"/> never.
    /// </remarks>
    /// <param name="info">The create-info.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A bound is not finite, the minimum is above the maximum, the width of the range is not finite, or the step is
    /// negative or not finite. The <see cref="ArgumentException.ParamName"/> is <c>info</c>, and the message names the
    /// bad field of the create-info.
    /// </exception>
    public Slider(in CreateInfo info)
    {
        ValidateRange(info.Minimum, info.Maximum, info.Step, fromCreateInfo: true);
        _minimum = info.Minimum;
        _maximum = info.Maximum;
        _step = info.Step;
        IsFocusable = true;
        OnPointerDown = s_pointerDown;
        OnPointerMove = s_pointerMove;
        OnKeyDown = s_keyDown;
        Apply(info.Value);
    }

    /// <summary>Gets the type id of the slider: <c>Slider</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Gets the lower end of the range. Use <see cref="SetRange"/> to change it.</summary>
    public float Minimum => _minimum;

    /// <summary>Gets the upper end of the range. Use <see cref="SetRange"/> to change it.</summary>
    public float Maximum => _maximum;

    /// <summary>Gets the step of the grid, or 0 for no grid. Use <see cref="SetRange"/> to change it.</summary>
    public float Step => _step;

    /// <summary>Gets or sets the value of the slider.</summary>
    /// <remarks>
    /// A set follows the value rule: a NaN gives <see cref="Minimum"/>, the number is clamped to the range, and then
    /// rounded to the grid when <see cref="Step"/> is above 0. A set to a new value marks
    /// <see cref="DirtyFlags.Paint"/>. A set to the same value marks nothing. A set from code calls no
    /// <see cref="ValueChanged"/> (Decision 5).
    /// </remarks>
    public float Value
    {
        get => _value;
        set => Apply(value);
    }

    /// <summary>
    /// Gets or sets the delegate that a change by the user calls. The delegate gets the slider as the sender.
    /// </summary>
    /// <remarks>
    /// Only user input calls the delegate, and only when the value changes. The slider stops the walk of the event,
    /// writes the value, and then calls the delegate. The delegate reads the new value. A slider with no delegate still
    /// moves and stops the walk.
    /// </remarks>
    public Action<Slider>? ValueChanged { get; set; }

    /// <summary>Changes the range and the step as one, and applies the value rule to the old value.</summary>
    /// <remarks>
    /// <para>
    /// The method checks the three numbers first (Decision 9). On a bad number it throws and changes nothing. Else it
    /// writes the range and applies the value rule to the old value, so the value stays in the range and on the grid.
    /// </para>
    /// <para>
    /// The method marks <see cref="DirtyFlags.Paint"/> when the range or the value changes. The same range with the same
    /// value marks nothing. The method calls no <see cref="ValueChanged"/> (Decision 5).
    /// </para>
    /// </remarks>
    /// <param name="minimum">The lower end of the range. It must be finite.</param>
    /// <param name="maximum">The upper end of the range. It must be finite and not below the minimum.</param>
    /// <param name="step">The step of the grid. It must be finite and 0 or more. A step of 0 means no grid.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A bound is not finite, the minimum is above the maximum, the width of the range (<c>maximum - minimum</c>) is not
    /// finite, or the step is negative or not finite. The exception names the bad parameter.
    /// </exception>
    public void SetRange(float minimum, float maximum, float step)
    {
        ValidateRange(minimum, maximum, step, fromCreateInfo: false);
        bool rangeChanged = minimum != _minimum || maximum != _maximum || step != _step;
        _minimum = minimum;
        _maximum = maximum;
        _step = step;
        bool valueChanged = Apply(_value);
        if (rangeChanged && !valueChanged)
        {
            MarkDirty(DirtyFlags.Paint);
        }
    }

    /// <summary>Gets a value that tells if the slider takes the moves of its drag: always <see langword="true"/>.</summary>
    /// <remarks>
    /// A slider that holds <see cref="PseudoStates.Disabled"/> captures no move (Decision 11). The move then walks to the
    /// hit. This is the only <see cref="PseudoStates.Disabled"/> check in this file.
    /// </remarks>
    bool IDragCapture.CapturesMoves => (States & PseudoStates.Disabled) == 0;

    /// <summary>Gives the width of the thumb: the smaller of the content height and the content width.</summary>
    /// <param name="content">The content box: x, y, width, and height.</param>
    /// <returns>The width of the thumb in pixels.</returns>
    internal static float ThumbWidth(in float4 content)
    {
        return MathF.Min(content.W, content.Z);
    }

    /// <summary>Gives the position of a value in the range, from 0 to 1.</summary>
    /// <param name="value">The value.</param>
    /// <param name="minimum">The lower end of the range.</param>
    /// <param name="maximum">The upper end of the range.</param>
    /// <returns>The ratio, clamped to 0 and 1. The ratio is 0 for a range of zero.</returns>
    internal static float Ratio(float value, float minimum, float maximum)
    {
        float range = maximum - minimum;
        if (range <= 0f)
        {
            return 0f;
        }

        return Math.Clamp((value - minimum) / range, 0f, 1f);
    }

    /// <summary>
    /// Draws the thumb in the content box, in the <see cref="ComputedStyle.Color"/> of the slider scaled by the opacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The thumb has the width of <c>ThumbWidth</c> and the height of the content box. Its x is the content x plus the
    /// ratio of the value times the travel (the content width minus the thumb width). A zero range gives the ratio 0, so
    /// the thumb sits at the start. The thumb uses the corner radius of the slider and has no border.
    /// </para>
    /// <para>
    /// The method draws nothing when the content width or the content height is 0 or less, or when the color has an alpha
    /// of 0 after the scale. It changes no tree state and allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the call.</param>
    /// <param name="borderBox">The border box of the slider in viewport pixels: x, y, width, and height.</param>
    /// <param name="opacity">The product of the opacities from the root to the slider, from 0 to 1.</param>
    internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
    {
        float4 content = Layout.ContentBox(in borderBox);
        if (!(content.Z > 0f) || !(content.W > 0f))
        {
            return;
        }

        Rgba8 color = Style.Color.ScaleAlpha(opacity);
        if (color.A == 0)
        {
            return;
        }

        float thumb = ThumbWidth(in content);
        float travel = content.Z - thumb;
        float x = content.X + (Ratio(_value, _minimum, _maximum) * travel);
        float4 rect = new(x, content.Y, thumb, content.W);
        Corners radius = Style.CornerRadius;
        renderer.Rect(in rect, in color, in radius, 0f, Rgba8.Transparent);
    }

    /// <summary>
    /// The press handler. It moves the value to the pointer when the press starts a drag on the slider.
    /// </summary>
    /// <param name="e">The press event.</param>
    private static void HandlePointerDown(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not Slider slider || !WidgetInput.IsDragStart(slider, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        slider.UserSet(slider.ValueAtPointer(e.Position.X));
    }

    /// <summary>
    /// The move handler. It moves the value to the pointer when the slider holds the left press.
    /// </summary>
    /// <param name="e">The move event.</param>
    private static void HandlePointerMove(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not Slider slider || !WidgetInput.IsDragMove(slider, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        slider.UserSet(slider.ValueAtPointer(e.Position.X));
    }

    /// <summary>
    /// The key handler. It moves the value by one step on a Left or Right arrow key down on the focused slider.
    /// </summary>
    /// <param name="e">The key event.</param>
    private static void HandleKeyDown(ref KeyEvent e)
    {
        if (e.Header.CurrentTarget is not Slider slider || !WidgetInput.IsArrowKey(slider, in e, out int direction))
        {
            return;
        }

        e.Header.StopPropagation();
        float delta = slider._step > 0f ? slider._step : (slider._maximum - slider._minimum) / 100f;
        slider.UserSet(slider._value + (direction * delta));
    }

    /// <summary>Writes a value from user input and calls <see cref="ValueChanged"/> when the value changes.</summary>
    /// <param name="value">The new value.</param>
    private void UserSet(float value)
    {
        if (Apply(value))
        {
            ValueChanged?.Invoke(this);
        }
    }

    /// <summary>Gives the value that sits at a viewport x position, by the map of the thumb center.</summary>
    /// <param name="x">The x position in viewport pixels.</param>
    /// <returns>The value for the position. The caller clamps it.</returns>
    private float ValueAtPointer(float x)
    {
        float2 origin = WidgetInput.ViewportOrigin(this);
        float4 rect = Layout.Rect;
        float4 borderBox = new(origin.X, origin.Y, rect.Z, rect.W);
        float4 content = Layout.ContentBox(in borderBox);
        float thumb = ThumbWidth(in content);
        float travel = content.Z - thumb;
        if (!(travel > 0f))
        {
            return _minimum;
        }

        // The map runs in double and rounds one time to float, so a pixel center gives the exact value.
        double offset = Math.Clamp((double)x - content.X - (thumb / 2d), 0d, travel);
        return (float)(_minimum + (offset * ((double)_maximum - _minimum) / travel));
    }

    /// <summary>Checks a range and a step (Decision 9).</summary>
    /// <remarks>
    /// The checks run in this order: the minimum is finite, the maximum is finite, the minimum is not above the maximum,
    /// the width of the range (<c>maximum - minimum</c>) is finite, and the step is finite and 0 or more. A range such as
    /// -3e38 to 3e38 has bounds that are finite, but its width is not, so the map, the draw, and the keys break. The check
    /// rejects it. The good path allocates nothing.
    /// </remarks>
    /// <param name="minimum">The lower end of the range.</param>
    /// <param name="maximum">The upper end of the range.</param>
    /// <param name="step">The step of the grid.</param>
    /// <param name="fromCreateInfo">
    /// <see langword="true"/> when the constructor calls the method. The exception then names <c>info</c>, the parameter
    /// of the constructor, and the message names the field of <see cref="CreateInfo"/>. Else the exception names
    /// <c>minimum</c>, <c>maximum</c>, or <c>step</c>, the parameters of <see cref="SetRange"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">A number is bad. The exception names the parameter.</exception>
    private static void ValidateRange(float minimum, float maximum, float step, bool fromCreateInfo)
    {
        if (!float.IsFinite(minimum))
        {
            throw fromCreateInfo
                ? new ArgumentOutOfRangeException(InfoName, minimum, "CreateInfo.Minimum must be finite.")
                : new ArgumentOutOfRangeException(nameof(minimum), minimum, "The minimum must be finite.");
        }

        if (!float.IsFinite(maximum))
        {
            throw fromCreateInfo
                ? new ArgumentOutOfRangeException(InfoName, maximum, "CreateInfo.Maximum must be finite.")
                : new ArgumentOutOfRangeException(nameof(maximum), maximum, "The maximum must be finite.");
        }

        if (minimum > maximum)
        {
            throw fromCreateInfo
                ? new ArgumentOutOfRangeException(
                    InfoName, minimum, "CreateInfo.Minimum must not be above CreateInfo.Maximum.")
                : new ArgumentOutOfRangeException(nameof(minimum), minimum, "The minimum must not be above the maximum.");
        }

        if (!float.IsFinite(maximum - minimum))
        {
            throw fromCreateInfo
                ? new ArgumentOutOfRangeException(
                    InfoName, maximum, "The width of the range (CreateInfo.Maximum - CreateInfo.Minimum) must be finite.")
                : new ArgumentOutOfRangeException(nameof(maximum), maximum, "The width of the range must be finite.");
        }

        if (!float.IsFinite(step) || step < 0f)
        {
            throw fromCreateInfo
                ? new ArgumentOutOfRangeException(InfoName, step, "CreateInfo.Step must be finite and 0 or more.")
                : new ArgumentOutOfRangeException(nameof(step), step, "The step must be finite and 0 or more.");
        }
    }

    /// <summary>Applies the value rule (Decision 4) to a number.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The value in the range, on the grid when <see cref="Step"/> is above 0.</returns>
    private float Normalize(float value)
    {
        if (float.IsNaN(value))
        {
            return _minimum;
        }

        float next = Math.Clamp(value, _minimum, _maximum);
        if (_step > 0f)
        {
            float steps = MathF.Round((next - _minimum) / _step, MidpointRounding.AwayFromZero);
            next = Math.Clamp(_minimum + (steps * _step), _minimum, _maximum);
        }

        return next;
    }

    /// <summary>Normalizes a value, compares it, writes it, and marks the paint.</summary>
    /// <param name="value">The new value.</param>
    /// <returns><see langword="true"/> when the value changed.</returns>
    private bool Apply(float value)
    {
        float next = Normalize(value);
        if (next == _value)
        {
            return false;
        }

        _value = next;
        MarkDirty(DirtyFlags.Paint);
        return true;
    }

    /// <summary>The create-info of a <see cref="Slider"/>.</summary>
    public struct CreateInfo
    {
        /// <summary>The lower end of the range. The default is 0.</summary>
        public float Minimum;

        /// <summary>The upper end of the range.</summary>
        public required float Maximum;

        /// <summary>
        /// The step of the grid. The grid starts at <see cref="Minimum"/>. A step of 0 means no grid. The default is 0.
        /// </summary>
        public float Step;

        /// <summary>The start value. The slider applies the value rule to it. The default is 0.</summary>
        public float Value;
    }
}
