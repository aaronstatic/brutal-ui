using System;
using Brutal.Localization;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// An element that clicks. A left press and a left release on the button call <see cref="Clicked"/> one time. The
/// button can show one text.
/// </summary>
/// <remarks>
/// <para>
/// The click rule: the press goes down on the button, and the release walks to the button. A release over another
/// element gives no click. A release after a press on another element gives no click. Only the left button clicks.
/// </para>
/// <para>
/// The limit of a chord: <see cref="PseudoStates.Active"/> marks the element of the first press of a gesture, and it
/// stays until the last button goes up. A left release then clicks a button that holds
/// <see cref="PseudoStates.Active"/>, also when the left press of that release was on another element. Example: a
/// right press on the button, a left press on another element, and a left release over the button give a click.
/// </para>
/// <para>
/// A <c>PointerUp</c> with a caller target does not end a press. When the button holds
/// <see cref="PseudoStates.Active"/>, that event clicks the button, and the real release clicks it again. Push a
/// pointer event with no target to click a button from code.
/// </para>
/// <para>
/// A click stops the walk of the release (A4). The ancestors of the button get no bubble call for that release. The
/// capture handlers of the ancestors run before the button, so they still get the release.
/// </para>
/// <para>
/// <see cref="Clicked"/> gets the button as the sender (Q3). Thus one delegate can serve many buttons.
/// </para>
/// <para>
/// Read <see cref="Clicked"/>, not the slot. The constructor writes the click handler into
/// <see cref="Element.OnPointerUp"/>. A write of <c>=</c> to <see cref="Element.OnPointerUp"/> removes the click. A
/// handler that <c>+=</c> adds to that slot runs after the click handler. The click stops the event, so that handler
/// gets no call for a click. It gets a release that is not a click.
/// </para>
/// <para>
/// A pickable child of the button gets <see cref="PseudoStates.Active"/> in place of the button. A press on that child
/// then gives no click. Set <see cref="Element.IsPickable"/> to <see langword="false"/> on each child of the button.
/// </para>
/// <para>
/// The constructor sets <see cref="Element.IsFocusable"/> to <see langword="true"/> (A5). A press on the button gives
/// it the keyboard focus.
/// </para>
/// <para>
/// The key rule: a focused button clicks on an Enter or a Space key down. The key must have the button as its target,
/// and the key route gives it to the focus owner. A repeat, a key up, another key, the keypad Enter key, and a key with
/// <see cref="UiModifiers.Control"/>, <see cref="UiModifiers.Alt"/>, or <see cref="UiModifiers.Super"/> give no click.
/// <see cref="UiModifiers.Shift"/> does not block the click. A key that bubbles from a focusable child gives no click.
/// A key click stops the walk of the key (A4). A key that the button does not use walks on.
/// </para>
/// <para>
/// The constructor writes the key handler into <see cref="Element.OnKeyDown"/>. A write of <c>=</c> to that slot
/// removes the key click.
/// </para>
/// <para>
/// A button that holds <see cref="PseudoStates.Disabled"/> raises no click, by a pointer or by a key, and it does not
/// stop the walk. The event walks on to the ancestors. A key with the disabled button as the target (a caller target)
/// also gives no click.
/// </para>
/// <para>
/// The text rules are the rules of <see cref="Label"/>. The button measures its text with the
/// <see cref="UiContext.Measurer"/> of its context during <see cref="UiContext.Update(float)"/>. Create the context
/// with <see cref="UiContext(in UiContext.CreateInfo)"/>. A context with no measurer throws
/// <see cref="InvalidOperationException"/> from <see cref="UiContext.Update(float)"/> when the button needs a measure.
/// A button with an empty text and no children also needs the measurer. Only a layout that fixes both the width and
/// the height of the button skips the measure.
/// </para>
/// <para>
/// The text draws at the top-left corner of the content box: the border box minus the padding and the border
/// (Decision 8). The text wraps at the width of the content box. A button with an auto size takes the size of its text
/// plus the padding and the border, so the text shows in the middle of the box. The style system has no text
/// alignment yet. A button with a fixed size larger than its text shows the text at the top-left corner.
/// </para>
/// <para>
/// The engine calls a measure callback for an element with no children only. A button with a child, for example an
/// <see cref="Image"/>, does not measure its text. Its size comes from the children and the style. The button still
/// draws a text that is not empty, and the children paint over that text.
/// </para>
/// <para>
/// The button has no text values of its own. It reads <see cref="ComputedStyle.Color"/>,
/// <see cref="ComputedStyle.Font"/>, and <see cref="ComputedStyle.FontSize"/> of <see cref="Element.Style"/>. The
/// text values come from a rule, the inline set (<see cref="Element.Inline"/>), or the parent. Write
/// <c>button.Inline.Color</c>, <c>button.Inline.Font</c>, or <c>button.Inline.FontSize</c> to set a value on one
/// button.
/// </para>
/// <para>
/// Each <c>SetText</c> overload compares the old text first. A change marks the dirty bits that the change needs. A
/// set to the same text marks nothing. Do not call <see cref="Element.MarkDirty(DirtyFlags)"/> for a button text.
/// </para>
/// <para>
/// A button with a <see cref="LocalizedText"/> reads <see cref="LString.Localized"/> at creation, at
/// <see cref="SetText(LString?)"/>, and at <see cref="UiContext.RefreshLocalizedText"/> only. Call
/// <see cref="UiContext.RefreshLocalizedText"/> after a locale change.
/// </para>
/// </remarks>
public sealed class Button : Element
{
    /// <summary>The measure callback of each button. The constructor sets it one time, so the layout pass allocates nothing.</summary>
    private static readonly MeasureCallback s_measure = MeasureButton;

    /// <summary>The type id of each button.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("Button");

    /// <summary>
    /// The release handler of each button. The delegate has no target, so each button shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerUp = HandlePointerUp;

    /// <summary>
    /// The key down handler of each button. The delegate has no target, so each button shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<KeyEvent> s_keyDown = HandleKeyDown;

    /// <summary>
    /// The owned text. The field has no <see langword="readonly"/> modifier, because each call changes the struct in
    /// place. A copy of the struct loses the change.
    /// </summary>
    private Utf8Text _text;

    /// <summary>Creates a button with the text of the create-info.</summary>
    /// <remarks>
    /// <para>
    /// The constructor sets <see cref="Element.IsFocusable"/> to <see langword="true"/> and writes the click handler
    /// into <see cref="Element.OnPointerUp"/> and the key handler into <see cref="Element.OnKeyDown"/>.
    /// </para>
    /// <para>
    /// The constructor encodes <see cref="CreateInfo.Text"/>, or <see cref="LString.Localized"/> of
    /// <see cref="CreateInfo.LocalizedText"/>, to UTF-8 one time. No text gives a button with an empty text.
    /// </para>
    /// </remarks>
    /// <param name="info">The create-info.</param>
    /// <exception cref="ArgumentException"><see cref="CreateInfo.Text"/> and <see cref="CreateInfo.LocalizedText"/> are both set.</exception>
    public Button(in CreateInfo info)
    {
        if (info.Text is not null && info.LocalizedText is not null)
        {
            throw new ArgumentException(
                "Set Text or LocalizedText of the Button.CreateInfo, not both.",
                nameof(info));
        }

        _text = new Utf8Text(info.Text, info.LocalizedText);
        Layout.Measure = s_measure;
        IsFocusable = true;
        OnPointerUp = s_pointerUp;
        OnKeyDown = s_keyDown;
    }

    /// <summary>Gets the type id of the button: <c>Button</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Gets the owned UTF-8 text.</summary>
    /// <remarks>The span is valid until the next change of the text.</remarks>
    public ReadOnlySpan<byte> Text => _text.Span;

    /// <summary>
    /// Gets the localizable text, or <see langword="null"/> when the text comes from a span or a string.
    /// </summary>
    /// <remarks>
    /// <see cref="SetText(LString?)"/> and <see cref="CreateInfo.LocalizedText"/> set it.
    /// <see cref="SetText(ReadOnlySpan{byte})"/> and <see cref="SetText(string?)"/> clear it.
    /// </remarks>
    public LString? LocalizedText => _text.Localized;

    /// <summary>
    /// Gets or sets the delegate that a click calls. The delegate gets the button as the sender.
    /// </summary>
    /// <remarks>
    /// Only user input calls the delegate. The button stops the walk of the release first, and then calls the
    /// delegate. A button with no delegate still stops the walk.
    /// </remarks>
    public Action<Button>? Clicked { get; set; }

    /// <summary>Copies a UTF-8 text into the owned buffer and clears <see cref="LocalizedText"/>.</summary>
    /// <remarks>
    /// <para>
    /// The method compares the bytes with the current text first. The same bytes mark nothing. Other bytes mark
    /// <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>. The method does not check the bytes for
    /// valid UTF-8.
    /// </para>
    /// <para>
    /// The buffer grows by doubling and never shrinks. A text that fits the buffer allocates nothing. The span can be
    /// a part of <see cref="Text"/>.
    /// </para>
    /// </remarks>
    /// <param name="utf8">The UTF-8 text. An empty span gives an empty text.</param>
    public void SetText(ReadOnlySpan<byte> utf8)
    {
        if (_text.Set(utf8))
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>Encodes a text to UTF-8 into the owned buffer and clears <see cref="LocalizedText"/>.</summary>
    /// <remarks>
    /// <para>
    /// The method compares the text with the current text first (ordinal). The same text marks nothing. Another text
    /// marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>.
    /// </para>
    /// <para>
    /// The buffer grows by doubling and never shrinks. A text that fits the buffer allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    public void SetText(string? text)
    {
        if (_text.Set(text))
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>
    /// Sets the localizable text. The button shows <see cref="LString.Localized"/> as it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method reads <see cref="LString.Localized"/> one time and compares it with the current text (ordinal). The
    /// same text marks nothing. Another text marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>.
    /// </para>
    /// <para>
    /// The button reads the <see cref="LString"/> again at <see cref="UiContext.RefreshLocalizedText"/> only. Call it
    /// after a locale change. An <see cref="LString"/> with replacement variables shows its localized template text.
    /// To show a formatted text, format it and call <see cref="SetText(ReadOnlySpan{byte})"/>.
    /// </para>
    /// </remarks>
    /// <param name="text">The localizable text. <see langword="null"/> clears <see cref="LocalizedText"/> and gives an empty text.</param>
    public void SetText(LString? text)
    {
        if (_text.Set(text))
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>
    /// Reads <see cref="LString.Localized"/> of <see cref="LocalizedText"/> again, and shows the new text when it
    /// changed. <see cref="UiContext.RefreshLocalizedText"/> calls it.
    /// </summary>
    /// <remarks>
    /// A button with no <see cref="LocalizedText"/>, or with the same string reference, does nothing. A new reference
    /// with the same content (ordinal) marks nothing. A new content marks <see cref="DirtyFlags.Layout"/> and
    /// <see cref="DirtyFlags.Paint"/>. The method allocates nothing when the buffer is large enough.
    /// </remarks>
    internal void RefreshLocalized()
    {
        if (_text.Refresh())
        {
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>
    /// Draws the text at the top-left corner of the content box, with the color scaled by the opacity (Decision 8).
    /// The wrap width is <see cref="Utf8Text.DrawWrapWidth(float, float)"/> of the content box and the border box.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The draw reads <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>, and
    /// <see cref="ComputedStyle.FontSize"/> of <see cref="Element.Style"/>.
    /// </para>
    /// <para>An empty text or a color with an alpha of 0 after the scale draws nothing.</para>
    /// <para>
    /// The content box is the border box minus the padding and the border of each edge
    /// (<see cref="LayoutNode.ContentBox(in float4)"/>). The draw rule is the rule of <see cref="Label"/>. The paint
    /// walk draws the fill and the border of the button before this call.
    /// </para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the call.</param>
    /// <param name="borderBox">The border box of the button in viewport pixels: x, y, width, and height.</param>
    /// <param name="opacity">The product of the opacities from the root to the button, from 0 to 1.</param>
    internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
    {
        float4 content = Layout.ContentBox(in borderBox);
        Utf8Text.Paint(renderer, in Style, in content, borderBox.Z, opacity, Text);
    }

    /// <summary>
    /// Measures the text of a button with the measurer of the current <see cref="UiContext.Update(float)"/> call.
    /// </summary>
    /// <remarks>
    /// The callback checks for a measurer first. Then it calls
    /// <see cref="Utf8Text.Measure(IUiRenderer, in ComputedStyle, ReadOnlySpan{byte}, float, MeasureMode)"/>. That
    /// method holds the measure rule. The callback gives a finite size. The engine calls it for a button with no
    /// children only.
    /// </remarks>
    /// <param name="element">The button.</param>
    /// <param name="width">The available inner width in pixels.</param>
    /// <param name="widthMode">The rule for <paramref name="width"/>.</param>
    /// <param name="height">The available inner height in pixels.</param>
    /// <param name="heightMode">The rule for <paramref name="height"/>.</param>
    /// <returns>The size of the text in pixels.</returns>
    /// <exception cref="InvalidOperationException">The current update has no measurer.</exception>
    private static float2 MeasureButton(
        Element element,
        float width,
        MeasureMode widthMode,
        float height,
        MeasureMode heightMode)
    {
        IUiRenderer measurer = UiContext.CurrentMeasurer
            ?? throw new InvalidOperationException(
                "A Button needs a UiContext with a measurer. Create the context with UiContext.CreateInfo.");

        Button button = (Button)element;
        return Utf8Text.Measure(measurer, in button.Style, button.Text, width, widthMode);
    }

    /// <summary>
    /// The click handler. It calls <see cref="Clicked"/> when the release is a click on the button.
    /// </summary>
    /// <param name="e">The release event.</param>
    private static void HandlePointerUp(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not Button button || !WidgetInput.IsClick(button, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        button.Click();
    }

    /// <summary>
    /// The key handler. It calls <see cref="Clicked"/> when the key is an Enter or a Space key down on the button.
    /// </summary>
    /// <param name="e">The key event.</param>
    private static void HandleKeyDown(ref KeyEvent e)
    {
        if (e.Header.CurrentTarget is not Button button
            || !WidgetInput.IsKeyActivation(button, in e, ActivationKeys.Enter | ActivationKeys.Space))
        {
            return;
        }

        e.Header.StopPropagation();
        button.Click();
    }

    /// <summary>The click step that the pointer handler and the key handler share. It calls <see cref="Clicked"/>.</summary>
    private void Click()
    {
        Clicked?.Invoke(this);
    }

    /// <summary>The create-info of a <see cref="Button"/>.</summary>
    /// <remarks>
    /// Each create-info member has a safe default. The create-info has no text values. Set the color,
    /// the font, and the font size with a rule, with <see cref="Element.Inline"/>, or on the parent.
    /// </remarks>
    public struct CreateInfo
    {
        /// <summary>The text. The button encodes it to UTF-8 one time. <see langword="null"/> gives an empty text.</summary>
        /// <remarks>Set <see cref="Text"/> or <see cref="LocalizedText"/>, not both.</remarks>
        public string? Text;

        /// <summary>
        /// The localizable text. The button encodes <see cref="LString.Localized"/> to UTF-8 one time.
        /// <see langword="null"/> gives no localizable text.
        /// </summary>
        /// <remarks>
        /// Set <see cref="Text"/> or <see cref="LocalizedText"/>, not both. Call
        /// <see cref="UiContext.RefreshLocalizedText"/> after a locale change.
        /// </remarks>
        public LString? LocalizedText;
    }
}
