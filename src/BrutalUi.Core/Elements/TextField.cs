using System;
using System.Buffers;
using System.Text;
using System.Text.Unicode;
using Brutal.Localization;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// An element that holds one line of UTF-8 text that the user types. A char on the focused field inserts the UTF-8
/// form of the code point at the caret and calls <see cref="TextChanged"/> one time. The Left, Right, Home, and End keys
/// move the caret, and with <c>Shift</c> they select. The Backspace and Delete keys remove the selection or one code
/// point. A char replaces the selection. A left press places the caret under the pointer, and a drag selects.
/// </summary>
/// <remarks>
/// <para>
/// The buffer rule (A5): the field holds one fixed <see cref="byte"/> array of <see cref="Capacity"/> bytes. The
/// constructor allocates it one time, and the buffer never grows. <see cref="Text"/> is the start of the buffer, and the
/// caret sits on a code point boundary of it. The capacity counts bytes, not code points. A code point takes 1 to 4
/// bytes.
/// </para>
/// <para>
/// The char rule (Decision 4): the field must be the target of the char. The key route gives each char with no target
/// to the focus owner, so a char goes to the focused field. A char with the field as the target from the caller also
/// counts. A code point that is not a Unicode scalar value, or that is a control character (C0, DEL, or C1), is not
/// text. The field ignores it, and the char walks on to the ancestors. The field inserts each other code point at the
/// caret and moves the caret after it.
/// </para>
/// <para>
/// The drop rule: the fit check comes before each change. The new length is the length minus the selection plus the
/// UTF-8 form of the code point. When the new length is more than <see cref="Capacity"/>, the field drops the char.
/// Nothing changes, the selection stays, and nothing gets a call. Thus the player loses no selected text. A code point
/// never splits.
/// </para>
/// <para>
/// The selection rule (Edit Rule 1): the field holds an anchor and the caret. Both are byte indexes on code point
/// boundaries. The selection is the range from the smaller index to the larger index. It is empty when the two indexes
/// are equal. A move with <c>Shift</c> moves the caret and keeps the anchor, so the selection grows or shrinks, and it
/// can flip over the anchor. The scroll follows the caret, not the anchor.
/// </para>
/// <para>
/// The replace rule (Decision 4): a char that fits removes the selection, inserts the code point at the start of the
/// selection, puts the caret after it, collapses the anchor to the caret, and calls <see cref="TextChanged"/> one time.
/// Backspace and Delete with a selection remove the selection, put the caret and the anchor at its start, and call
/// <see cref="TextChanged"/> one time.
/// </para>
/// <para>
/// The collapse rule: Left with a selection and no <c>Shift</c> puts the caret at the start of the selection, and Right
/// puts it at the end. The caret does not step further. Home and End with no <c>Shift</c> put the caret at 0 or at the
/// end. In each move with no <c>Shift</c>, the anchor follows the caret, so the selection becomes empty. A collapse
/// with no caret step still changes the anchor, so it marks <see cref="DirtyFlags.Paint"/> and restarts the blink.
/// </para>
/// <para>
/// The selection color (Conflicts and Gaps item 6): the style has no selection color. The field draws the selection in
/// its <see cref="ComputedStyle.Color"/>, at <see cref="SelectionAlpha"/> (35 %) of the alpha after the opacity scale.
/// A rule for <c>TextField:focus</c> thus also sets the selection color. A later style member can replace this rule.
/// </para>
/// <para>
/// The selection after a focus loss: the selection stays, and it draws again at the next focus gain. The field draws
/// no selection and no caret when it does not hold <see cref="PseudoStates.Focus"/>.
/// </para>
/// <para>
/// The stop rule: the field stops the walk of each char that is text, also when it drops the char. Thus a typed letter
/// never reaches a game shortcut on an ancestor. The stop also skips a later delegate of the same slot.
/// </para>
/// <para>
/// The sender: the field calls <see cref="TextChanged"/> after the stop and after the change, and it passes itself as
/// the sender. Only user input calls the delegate (Decision 7). The start text of the create-info calls nothing.
/// </para>
/// <para>
/// Read <see cref="TextChanged"/>, not the slot. A handler that a game adds with <c>+=</c> to
/// <see cref="Element.OnChar"/> of a field gets no call for a char that the field uses. Read
/// <see cref="TextChanged"/>, or use a capture handler on an ancestor.
/// </para>
/// <para>
/// A write of <c>=</c> to <see cref="Element.OnChar"/> removes the typing. The field then ignores each char.
/// </para>
/// <para>
/// The key rule (Decision 5): the field must be the target of a <see cref="UiEventType.KeyDown"/>, and no
/// <c>Control</c>, <c>Alt</c>, or <c>Super</c> modifier may be down. A repeat counts, so a held key acts again.
/// <c>Shift</c> does not block a key. Left and Right move the caret one code point. Home moves the caret to the start of
/// the text, and End moves it to the end. With <c>Shift</c>, these four keys keep the anchor, so they extend the
/// selection. Backspace removes the selection, or else the code point before the caret. Delete removes the selection, or
/// else the code point after the caret. A remove calls <see cref="TextChanged"/> one time. A caret move and a selection
/// change call nothing.
/// </para>
/// <para>
/// The stop rule at an end: the field stops the walk of each of these six keys, also when the key changes nothing. Thus
/// a Left at the start or a Backspace in an empty field never reaches a game handler on an ancestor.
/// </para>
/// <para>
/// The keys that walk on: the field does not stop Tab, so the focus moves. Up, Down, Escape, the keypad Enter key, a
/// repeat of the main Enter key, each letter key, each key up, and each key with <c>Control</c>, <c>Alt</c>, or
/// <c>Super</c> also walk on to the ancestors.
/// </para>
/// <para>
/// The Enter rule (Decision 6): the main Enter key with no repeat stops the walk and calls <see cref="Submitted"/> with
/// the field as the sender. <c>Shift</c> does not block it. The field keeps the focus. The game decides what a submit
/// does, for example a <see cref="UiContext.ClearFocus"/>. A field with no delegate still stops the walk.
/// </para>
/// <para>
/// The call rule (Decision 7): only user input calls <see cref="TextChanged"/>. A <c>SetText</c> from code, the start
/// text, and a caret move call nothing, so a store binding does not echo its own set.
/// </para>
/// <para>
/// The text from code (Decision 8): <see cref="GetText(Span{byte})"/> copies the text into a span of the game. The
/// three <c>SetText</c> overloads copy the longest prefix that fits <see cref="Capacity"/> and ends on a code point
/// boundary (the cut rule). A new text puts the caret and the anchor at the end, so it clears the selection. It sets
/// the text scroll to 0, restarts the blink, and marks <see cref="DirtyFlags.Paint"/>. The same text changes nothing
/// and marks nothing, so the caret and the selection stay. No call allocates. The span of <see cref="Text"/> is valid
/// until the next change, so copy it to keep it. A string literal needs the <c>u8</c> form for the span overload:
/// <c>SetText("x"u8)</c>. The field shows an <see cref="LString"/> as it is at the call only, and a later locale change
/// does not change it. A <c>SetText</c> and the start text do not filter a control character (Decision 3). Only the
/// typed char path filters. A line feed or another control character in a text from code breaks the caret position and
/// the scroll. Remove it before the call.
/// </para>
/// <para>
/// A write of <c>=</c> to <see cref="Element.OnKeyDown"/> removes the keys. The field then ignores each key, and only
/// the typing stays. A handler that a game adds with <c>+=</c> to <see cref="Element.OnKeyDown"/> gets no call for a key
/// that the field uses.
/// </para>
/// <para>
/// The code point rule (Decision 3): the caret is a byte index on a code point boundary, and each step is one code
/// point, not one grapheme. A letter with a combining mark, an emoji with a skin tone, and a flag take more than one
/// step and more than one Backspace. An invalid byte sequence in the text is one step of at least one byte, so the caret
/// never sticks.
/// </para>
/// <para>
/// The draw order (Decision 13): the selection, the text, and the caret. The selection draws behind the text, so the
/// glyphs stay readable. When the field holds <see cref="PseudoStates.Focus"/> and the caret is in the shown
/// phase of the blink, the field draws the caret after the text. The caret is a rect of <see cref="CaretWidth"/> pixel
/// by the line height, at the end of the text before the caret, in the <see cref="ComputedStyle.Color"/> of the field
/// scaled by the opacity.
/// </para>
/// <para>
/// The caret pass (Decision 10): <see cref="UiContext.Update(float)"/> runs the caret pass after the layout pass and
/// before the clear pass, for the focus owner only, and only when the focus owner is a text field. The pass advances
/// the blink and writes the text scroll. The draw reads the scroll and writes nothing.
/// </para>
/// <para>
/// The scroll rule (Decision 12): the text moves left by the text scroll, so the caret stays in the content box. The
/// pass moves the text by the smallest distance that keeps the caret and its <see cref="CaretWidth"/> inside the
/// content width. A caret move inside the view thus does not move the text. The scroll stays from 0 to the text width
/// plus <see cref="CaretWidth"/> minus the content width, so a remove at the end pulls the text back and no empty space
/// shows after the text. The pass measures the text with the measurer of the context. A context with no measurer keeps
/// the scroll.
/// </para>
/// <para>
/// The blink (Decision 11): the caret shows for <see cref="CaretBlinkInterval"/> seconds and hides for the same time.
/// The caret pass advances the timer by the time step of <see cref="UiContext.Update(float)"/>. A time step of 0 or
/// less, or one that is not finite, advances nothing. Each change of the text, the caret, or the anchor, and each focus
/// gain, restarts the timer, so the caret shows at once. A key that changes nothing, for example Left at the start, does not
/// restart the timer.
/// </para>
/// <para>
/// A field that is not the focus owner keeps its scroll and its timer, because the caret pass does not run for it. A
/// resize or a style change of such a field thus shows the old scroll until the field gets the focus again. A focus
/// owner with a content width below the caret width also keeps its scroll. Thus a hide or a width of 0 on the focused
/// field does not move the text, and the text shows again as before when the field shows again. A
/// <c>SetText</c> that changes the text sets the scroll to 0 at once, so the text shows from its start. The caret goes
/// to the end, so the next <see cref="UiContext.Update(float)"/> with the focus scrolls to the caret.
/// </para>
/// <para>
/// The clip (Decision 9): the constructor sets the inline overflow to <see cref="Overflow.Hidden"/>. The draw walk thus
/// clips the text to the padding box of the field. An inline value wins over each rule, so a rule with
/// <c>overflow: visible</c> does not remove the clip. A write of <c>Inline.Overflow</c> from code replaces the value of
/// the constructor. The text then draws outside the field.
/// </para>
/// <para>
/// The field has no own size (Decision 15). The size comes from a style rule or from <see cref="Element.Inline"/>. The
/// field has no measure callback, so a context with no measurer can hold a field. Give the field a width and a height
/// of at least one line of text.
/// </para>
/// <para>
/// The press rule (Decision 16): a left press with no chord on the field puts the caret at the code point boundary
/// nearest to the pointer x, and the anchor follows, so the selection becomes empty. The rule reads the press target of
/// the context through <c>WidgetInput.IsDragStart</c>, so a right press, a chord press, and a press on a child place
/// nothing. A tie between two boundaries goes to the lower index. A press before the text gives 0, and a press after
/// the text gives the end. The click focus of the context gives the field the focus before the walk of the press, so
/// the caret shows at once. The press stops the walk.
/// </para>
/// <para>
/// The drag rule: a pointer move while the field holds the left press moves the caret to the boundary nearest to the
/// pointer and keeps the anchor, so the drag selects. The rule reads <c>WidgetInput.IsDragMove</c>, and the field holds
/// no drag flag. Each drag move stops the walk. A drag past an edge of the field moves the caret to an end of the text,
/// and the scroll rule then scrolls to the caret. The field has no scroll timer, so a pointer that stays outside the
/// field does not scroll further.
/// </para>
/// <para>
/// The Shift rule: a left press with <c>Shift</c> moves the caret and keeps the anchor, so it extends the selection
/// from the old anchor. A drag after it extends the same selection.
/// </para>
/// <para>
/// The pending map: a handler has no measurer during the drain. Thus the press and the move store the pointer x in text
/// space: the pointer x minus the x of the content box plus the text scroll of the last frame. A press with no
/// <c>Shift</c> also stores the x of the anchor. The caret pass maps each stored x with the measurer, before the scroll
/// rule, in the same <see cref="UiContext.Update(float)"/>. A press and a move in one drain thus still select from the
/// press point. The caret pass runs for the focus owner only. A change of the text or the caret after the press in the
/// same drain, and each focus gain, drop the stored x, so the later input wins.
/// </para>
/// <para>
/// The capture: the field implements <c>IDragCapture</c> explicitly. While the field holds the left press with no
/// chord, each pointer move with no target walks to the field, also when the pointer leaves its rect. A disabled field
/// captures no move. <see cref="PseudoStates.Hover"/>, <see cref="UiContext.PointerHit"/>, and the routing gate still
/// follow the hit.
/// </para>
/// <para>
/// A write of <c>=</c> to <see cref="Element.OnPointerDown"/> removes the press. The field then places no caret for a
/// press, and a drag selects nothing. A write of <c>=</c> to <see cref="Element.OnPointerMove"/> removes the drag only.
/// </para>
/// <para>
/// The no-measurer rule: a context with no measurer cannot map a pointer x. The caret pass drops the stored x and keeps
/// the caret and the anchor. A press does not throw.
/// </para>
/// <para>
/// The constructor sets <see cref="Element.IsFocusable"/> to <see langword="true"/>.
/// </para>
/// </remarks>
public sealed class TextField : Element, IDragCapture
{
    /// <summary>The type id of each text field.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("TextField");

    /// <summary>The name of the constructor parameter. A bad create-info throws with this name.</summary>
    private const string InfoName = "info";

    /// <summary>The length of the stack buffer of <see cref="EqualsText"/>, in bytes.</summary>
    private const int CompareChunkLength = 64;

    /// <summary>
    /// The char handler of each text field. The delegate has no target, so each field shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<CharEvent> s_char = HandleChar;

    /// <summary>
    /// The key down handler of each text field. The delegate has no target, so each field shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<KeyEvent> s_keyDown = HandleKeyDown;

    /// <summary>
    /// The press handler of each text field. The delegate has no target, so each field shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerDown = HandlePointerDown;

    /// <summary>
    /// The move handler of each text field. The delegate has no target, so each field shares it, and the constructor
    /// allocates no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerMove = HandlePointerMove;

    /// <summary>The fixed UTF-8 buffer. <c>_buffer[0.._length]</c> holds the text.</summary>
    private readonly byte[] _buffer;

    /// <summary>The count of the text bytes, from 0 to <see cref="Capacity"/>.</summary>
    private int _length;

    /// <summary>The byte index of the caret, from 0 to <see cref="_length"/>. It sits on a code point boundary.</summary>
    private int _caret;

    /// <summary>
    /// The byte index of the anchor, from 0 to <see cref="_length"/>. It sits on a code point boundary. The selection is
    /// the range between the anchor and <see cref="_caret"/>.
    /// </summary>
    private int _anchor;

    /// <summary>
    /// The text scroll in pixels: the distance that the text moves left in the content box. The caret pass writes it.
    /// It is 0 or more, and it is finite.
    /// </summary>
    private float _scroll;

    /// <summary>
    /// The blink timer in seconds, from 0 to below two times <see cref="CaretBlinkInterval"/>. The caret shows while
    /// the timer is below <see cref="CaretBlinkInterval"/>.
    /// </summary>
    private float _blink;

    /// <summary>
    /// The pending x of the caret in text space, or <see cref="float.NaN"/> when no press or drag move waits. The press
    /// handler and the move handler write it. The caret pass maps it to a boundary and keeps the anchor.
    /// </summary>
    /// <remarks>
    /// Text space is the x from the start of the text: the pointer x minus the x of the content box plus
    /// <see cref="_scroll"/>.
    /// </remarks>
    private float _pendingX = float.NaN;

    /// <summary>
    /// The pending x of the anchor in text space, or <see cref="float.NaN"/> when the anchor stays. A left press with no
    /// <c>Shift</c> writes it. The caret pass maps it to a boundary and collapses the anchor and the caret there before it
    /// maps <see cref="_pendingX"/>. Thus a press and a drag move in one drain still select from the press point.
    /// </summary>
    private float _pendingAnchorX = float.NaN;

    /// <summary>Creates a text field.</summary>
    /// <remarks>
    /// The constructor checks the capacity first. It allocates the buffer one time, and it encodes the start text into
    /// the buffer. A start text that does not fit is cut at the capacity, on a code point boundary. An invalid surrogate
    /// in the start text becomes U+FFFD. The caret and the anchor go to the end of the text, so the field has no
    /// selection. The constructor sets <see cref="Element.IsFocusable"/> to <see langword="true"/>, sets the inline
    /// overflow to <see cref="Overflow.Hidden"/>, and writes the four handlers (char, key down, press, and move). It
    /// calls no <see cref="TextChanged"/>.
    /// </remarks>
    /// <param name="info">The create-info.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="CreateInfo.Capacity"/> is 0 or less. The <see cref="ArgumentException.ParamName"/> is <c>info</c>, and
    /// the message names <c>CreateInfo.Capacity</c>.
    /// </exception>
    public TextField(in CreateInfo info)
    {
        if (info.Capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(InfoName, info.Capacity, "CreateInfo.Capacity must be above 0.");
        }

        _buffer = new byte[info.Capacity];
        _length = EncodeUtf16(info.Text, _buffer);
        _caret = _length;
        _anchor = _length;
        IsFocusable = true;
        Inline.Overflow = Overflow.Hidden;
        OnChar = s_char;
        OnKeyDown = s_keyDown;
        OnPointerDown = s_pointerDown;
        OnPointerMove = s_pointerMove;
    }

    /// <summary>Gets the type id of the text field: <c>TextField</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Gets the size of the buffer in bytes. The text never holds more bytes.</summary>
    public int Capacity => _buffer.Length;

    /// <summary>Gets the UTF-8 text of the field.</summary>
    /// <remarks>
    /// The span points into the buffer of the field, so the read allocates nothing. The span is valid until the next
    /// change of the text. Copy the bytes to keep them.
    /// </remarks>
    public ReadOnlySpan<byte> Text => _buffer.AsSpan(0, _length);

    /// <summary>
    /// Gets or sets the delegate that a change of the text by the user calls. The delegate gets the field as the sender.
    /// </summary>
    /// <remarks>
    /// Only user input calls the delegate, and only when the bytes change. The field stops the walk of the event,
    /// writes the text, and then calls the delegate. The delegate reads the new <see cref="Text"/>. A dropped char calls
    /// nothing. A field with no delegate still takes the text and stops the walk.
    /// </remarks>
    public Action<TextField>? TextChanged { get; set; }

    /// <summary>
    /// Gets or sets the delegate that the Enter key calls. The delegate gets the field as the sender.
    /// </summary>
    /// <remarks>
    /// The main Enter key with no repeat stops the walk and then calls the delegate (Decision 6). The keypad Enter key
    /// and a repeat of Enter walk on and call nothing. <c>Shift</c> does not block the key. A field with no delegate
    /// still stops the walk. The field keeps the focus, and the game decides what a submit does.
    /// </remarks>
    public Action<TextField>? Submitted { get; set; }

    /// <summary>
    /// Copies the UTF-8 text into a destination span, cut at a code point boundary when the span is too short.
    /// </summary>
    /// <remarks>
    /// The method copies the longest prefix that fits the span and ends on a code point boundary, so it never writes
    /// half a code point. It writes no byte after the prefix. It allocates nothing and changes nothing in the field.
    /// </remarks>
    /// <param name="destination">The span that gets the bytes.</param>
    /// <returns>The count of the bytes that the method wrote.</returns>
    public int GetText(Span<byte> destination)
    {
        int count = CutAtBoundary(Text, destination.Length);
        Text[..count].CopyTo(destination);
        return count;
    }

    /// <summary>Copies a UTF-8 text into the buffer, cut at the capacity on a code point boundary.</summary>
    /// <remarks>
    /// <para>
    /// The method cuts the bytes first (see <see cref="CutAtBoundary(ReadOnlySpan{byte}, int)"/>) and compares the
    /// prefix with <see cref="Text"/>. The same bytes change nothing and mark nothing. Other bytes go to the buffer, the
    /// caret and the anchor go to the end, the text scroll goes to 0, and the field marks
    /// <see cref="DirtyFlags.Paint"/>. The span can be a part of
    /// <see cref="Text"/>. The method allocates nothing, and it calls no delegate (Decision 7).
    /// </para>
    /// <para>
    /// The method does not check the bytes for valid UTF-8, and it does not filter a control character
    /// (Decision 3). Only the typed char path filters. An invalid byte is one step for the caret.
    /// </para>
    /// <para>
    /// A line feed or another control character in a text from code breaks the caret position and the scroll. Remove it
    /// before the call.
    /// </para>
    /// </remarks>
    /// <param name="utf8">The UTF-8 text. An empty span gives an empty text.</param>
    public void SetText(ReadOnlySpan<byte> utf8)
    {
        ReadOnlySpan<byte> prefix = utf8[..CutAtBoundary(utf8, _buffer.Length)];
        if (prefix.SequenceEqual(Text))
        {
            return;
        }

        prefix.CopyTo(_buffer);
        WriteLength(prefix.Length);
    }

    /// <summary>Encodes a text to UTF-8 into the buffer, cut at the capacity on a code point boundary.</summary>
    /// <remarks>
    /// <para>
    /// The method compares the encoded text with <see cref="Text"/> first, and it allocates nothing. The same text
    /// changes nothing and marks nothing. Another text goes to the buffer, the caret and the anchor go to the end, the
    /// text scroll goes to 0, and the field marks <see cref="DirtyFlags.Paint"/>. The method calls no delegate. An
    /// invalid surrogate becomes U+FFFD, and a control character stays in the text (Decision 3).
    /// </para>
    /// <para>
    /// A call with a string literal needs the <c>u8</c> form to reach the span overload, for example
    /// <c>SetText("x"u8)</c>. The analyzer <c>BRUTALCORE0011</c> rejects <c>SetText("x")</c>. A bare
    /// <c>SetText(null)</c> is ambiguous. Write <c>SetText((string?)null)</c>.
    /// </para>
    /// </remarks>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    public void SetText(string? text)
    {
        ReadOnlySpan<char> chars = text;
        if (EqualsText(chars))
        {
            return;
        }

        WriteLength(EncodeUtf16(chars, _buffer));
    }

    /// <summary>
    /// Sets the text to <see cref="LString.Localized"/> as it is at the call. The field keeps no <see cref="LString"/>.
    /// </summary>
    /// <remarks>
    /// The method reads <see cref="LString.Localized"/> one time and sets it as <see cref="SetText(string?)"/> does. A
    /// later locale change does not change the field, and <see cref="UiContext.RefreshLocalizedText"/> does not know
    /// the field, because a refresh would overwrite the edits of the player. Call this method again to show the new
    /// locale.
    /// </remarks>
    /// <param name="text">The localizable text. <see langword="null"/> gives an empty text.</param>
    public void SetText(LString? text)
    {
        SetText(text?.Localized);
    }

    /// <summary>The width of the caret rect in pixels.</summary>
    internal const float CaretWidth = 1f;

    /// <summary>
    /// The time in seconds for which the caret shows, and then the time for which it hides. One blink cycle is two
    /// times this value.
    /// </summary>
    internal const float CaretBlinkInterval = 0.5f;

    /// <summary>
    /// The factor of the selection alpha: the selection draws in <see cref="ComputedStyle.Color"/> at this part of the
    /// alpha after the opacity scale.
    /// </summary>
    internal const float SelectionAlpha = 0.35f;

    /// <summary>Gets the byte index of the caret in <see cref="Text"/>.</summary>
    internal int CaretIndex => _caret;

    /// <summary>
    /// Gets the byte index of the anchor in <see cref="Text"/>. The selection is the range between the anchor and
    /// <see cref="CaretIndex"/>. It is empty when the two are equal.
    /// </summary>
    internal int AnchorIndex => _anchor;

    /// <summary>Gets the text scroll in pixels: the distance that the text moves left in the content box.</summary>
    internal float TextScroll => _scroll;

    /// <summary>
    /// Gets a value that tells if the caret is in the shown phase of the blink. The draw shows the caret only with
    /// <see cref="PseudoStates.Focus"/> too.
    /// </summary>
    internal bool CaretVisible => _blink < CaretBlinkInterval;

    /// <summary>The caret pass of <see cref="UiContext.Update(float)"/>: it advances the blink and writes the text scroll.</summary>
    /// <remarks>
    /// <para>
    /// The context calls the method after the layout pass, on the focus owner only. A time step that is finite and above
    /// 0 advances the blink timer in a cycle of two times <see cref="CaretBlinkInterval"/>. A flip of the shown phase
    /// marks <see cref="DirtyFlags.Paint"/>. Each other time step advances nothing.
    /// </para>
    /// <para>
    /// With a measurer, the method then applies the scroll rule (Decision 12). It measures the text before the caret and
    /// the full text, and it moves the scroll by the smallest distance that keeps the caret in
    /// <c>[0, width - CaretWidth]</c> of the content box. It clamps the scroll to
    /// <c>[0, max(0, textWidth + CaretWidth - width)]</c>. A value that is not finite gives 0. A new scroll marks
    /// <see cref="DirtyFlags.Paint"/>. With no measurer, the scroll stays. A content width below
    /// <see cref="CaretWidth"/> also keeps the scroll, so a hide or a width of 0 does not move the text.
    /// </para>
    /// <para>
    /// The pending map (Decision 16): after the blink step and before the scroll rule, the method maps the pending press
    /// and drag x with <see cref="IndexAtX(ReadOnlySpan{byte}, float, IUiRenderer, FontHandle, float)"/>. A pending anchor
    /// x moves the caret there and collapses the anchor. A pending caret x then moves the caret there and keeps the
    /// anchor. Each move goes through the caret move, so a change restarts the blink and marks
    /// <see cref="DirtyFlags.Paint"/>. The scroll rule then scrolls to the new caret in the same pass. The method clears
    /// both pending values before the first measure, so a measurer that throws does not map the same x again. A field
    /// that holds <see cref="PseudoStates.Disabled"/> drops the pending values and maps no press, also when a handler of
    /// the same drain disables the field after the press.
    /// </para>
    /// <para>
    /// With no measurer, the method drops the pending values, keeps the caret, and keeps the scroll.
    /// </para>
    /// <para>The method allocates nothing. A measurer that throws stops the pass after the blink step.</para>
    /// </remarks>
    /// <param name="deltaTime">The time step of the update, in seconds.</param>
    /// <param name="measurer">The measurer of the context, or <see langword="null"/>.</param>
    internal void AdvanceCaret(float deltaTime, IUiRenderer? measurer)
    {
        if (float.IsFinite(deltaTime) && deltaTime > 0f)
        {
            bool wasVisible = CaretVisible;
            float next = (_blink + deltaTime) % (2f * CaretBlinkInterval);
            _blink = float.IsFinite(next) && next >= 0f ? next : 0f;
            if (CaretVisible != wasVisible)
            {
                MarkDirty(DirtyFlags.Paint);
            }
        }

        // Read the pending values and clear them first. The caret move below also clears them, and a throw of the
        // measurer must not map the same x again in the next pass.
        float anchorX = _pendingAnchorX;
        float caretX = _pendingX;
        DropPending();
        if (measurer is null)
        {
            return;
        }

        // A handler of the same drain can disable the field after the press. A disabled field maps no press.
        if ((States & PseudoStates.Disabled) == 0)
        {
            if (!float.IsNaN(anchorX))
            {
                MoveCaret(IndexAtX(Text, anchorX, measurer, Style.Font, Style.FontSize), false);
            }

            if (!float.IsNaN(caretX))
            {
                MoveCaret(IndexAtX(Text, caretX, measurer, Style.Font, Style.FontSize), true);
            }
        }

        ApplyScrollRule(measurer);
    }

    /// <summary>Applies the scroll rule (Decision 12) with a measurer.</summary>
    /// <remarks>
    /// A content width below <see cref="CaretWidth"/>, or one that is not a number, keeps the scroll. The style pass gives
    /// a hidden field a rect of 0, and the draw shows nothing for a content width of 0. The old scroll thus stays, and
    /// the text shows again as before when the field shows again.
    /// </remarks>
    /// <param name="measurer">The measurer of the context.</param>
    private void ApplyScrollRule(IUiRenderer measurer)
    {
        float4 rect = Layout.Rect;
        float4 size = new(0f, 0f, rect.Z, rect.W);
        float width = Layout.ContentBox(in size).Z;
        if (!(width >= CaretWidth))
        {
            // A hidden field or a field with no room for the caret keeps its scroll, so the text shows again as before.
            return;
        }

        float caretX = measurer.MeasureText(Style.Font, Style.FontSize, Text[.._caret], 0f).X;
        float textWidth = measurer.MeasureText(Style.Font, Style.FontSize, Text, 0f).X;

        float scroll = _scroll;
        if (caretX - scroll > width - CaretWidth)
        {
            scroll = caretX - (width - CaretWidth);
        }

        if (caretX - scroll < 0f)
        {
            scroll = caretX;
        }

        float max = MathF.Max(0f, textWidth + CaretWidth - width);
        if (scroll > max)
        {
            scroll = max;
        }

        if (!float.IsFinite(scroll) || scroll < 0f)
        {
            scroll = 0f;
        }

        if (scroll != _scroll)
        {
            _scroll = scroll;
            MarkDirty(DirtyFlags.Paint);
        }
    }

    /// <summary>Sets the blink timer to 0, so the caret shows at once.</summary>
    /// <remarks>
    /// <para>
    /// Each change of the text or the caret calls the method, and <see cref="UiContext"/> calls it at each focus gain of
    /// the field (Edit Rule 6). A hidden caret that becomes shown marks <see cref="DirtyFlags.Paint"/>.
    /// </para>
    /// <para>
    /// The method also drops the pending press and drag x. Thus a later change of the text or the caret wins over an
    /// earlier press of the same drain, and a focus gain never maps a press from an old focus. The click focus runs
    /// before the walk of the press, so the press of the focus gain writes its x after this call.
    /// </para>
    /// </remarks>
    internal void RestartCaretBlink()
    {
        if (!CaretVisible)
        {
            MarkDirty(DirtyFlags.Paint);
        }

        _blink = 0f;
        DropPending();
    }

    /// <summary>Gets a value that tells if the field takes the moves of its drag.</summary>
    /// <remarks>
    /// A field that holds <see cref="PseudoStates.Disabled"/> captures no move. The move then walks to the hit.
    /// </remarks>
    bool IDragCapture.CapturesMoves => (States & PseudoStates.Disabled) == 0;

    /// <summary>
    /// Gives the code point boundary of a text with the measured prefix width nearest to an x in text space.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method steps from 0 with <see cref="NextBoundary(ReadOnlySpan{byte}, int)"/> and measures the prefix up to
    /// each boundary, with no wrap. It stops at the first boundary with a width at or past <paramref name="x"/>, and it
    /// compares that boundary with the boundary before it. A tie goes to the lower index. An x at or below 0, and an x
    /// that is not a number, give 0. An x past the width of the text gives the length.
    /// </para>
    /// <para>
    /// The method measures slices of the span, so it allocates nothing. The cost is one measure for each code point up
    /// to <paramref name="x"/>, so the capacity limits it.
    /// </para>
    /// </remarks>
    /// <param name="text">The UTF-8 text.</param>
    /// <param name="x">The x in text space: 0 is the start of the text.</param>
    /// <param name="measurer">The measurer.</param>
    /// <param name="font">The font of the field.</param>
    /// <param name="fontSize">The font size of the field.</param>
    /// <returns>The byte index of the nearest boundary, from 0 to the length of the text.</returns>
    internal static int IndexAtX(ReadOnlySpan<byte> text, float x, IUiRenderer measurer, FontHandle font, float fontSize)
    {
        if (!(x > 0f))
        {
            return 0;
        }

        int index = 0;
        float width = 0f;
        while (index < text.Length)
        {
            int next = NextBoundary(text, index);
            float nextWidth = measurer.MeasureText(font, fontSize, text[..next], 0f).X;
            if (nextWidth >= x)
            {
                // The tie goes to the lower index.
                return x - width <= nextWidth - x ? index : next;
            }

            index = next;
            width = nextWidth;
        }

        return text.Length;
    }

    /// <summary>Drops the pending press and drag x, so the next caret pass maps nothing.</summary>
    private void DropPending()
    {
        _pendingX = float.NaN;
        _pendingAnchorX = float.NaN;
    }

    /// <summary>Gives the x of a viewport x position in text space: the distance from the start of the text.</summary>
    /// <remarks>
    /// The method reads the rect of the last layout and the current scroll, so it uses the frame that the player saw.
    /// </remarks>
    /// <param name="x">The x position in viewport pixels.</param>
    /// <returns>The x in text space.</returns>
    private float TextSpaceX(float x)
    {
        float2 origin = WidgetInput.ViewportOrigin(this);
        float4 rect = Layout.Rect;
        float4 borderBox = new(origin.X, origin.Y, rect.Z, rect.W);
        float4 content = Layout.ContentBox(in borderBox);
        return x - content.X + _scroll;
    }

    /// <summary>
    /// Draws the text in the content box, in the <see cref="ComputedStyle.Color"/> of the field scaled by the opacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The walk pushes the clip of the padding box before it calls this method, because the inline overflow is
    /// <see cref="Overflow.Hidden"/>. The text starts at the top left corner of the content box, moved left by
    /// <see cref="TextScroll"/>, and does not wrap. The clip cuts the text at the edges of the padding box.
    /// </para>
    /// <para>
    /// When the field holds <see cref="PseudoStates.Focus"/> and has a selection, the method draws the selection before
    /// the text: a rect from the x of the selection start to the x of the selection end, by the line height. The color is
    /// <see cref="ComputedStyle.Color"/> with the alpha scaled by the opacity times <see cref="SelectionAlpha"/>. The
    /// method gets each x from one measure of the text before the index. A selection alpha of 0 draws no rect.
    /// </para>
    /// <para>
    /// When the field holds <see cref="PseudoStates.Focus"/> and <see cref="CaretVisible"/> is
    /// <see langword="true"/>, the method draws the caret after the text: a rect of
    /// <see cref="CaretWidth"/> by the line height, at the x of the end of the text before the caret. The method gets the
    /// x and the line height from one measure of the text before the caret, so an empty text also gives a caret of the
    /// line height.
    /// </para>
    /// <para>
    /// The method draws nothing when the content width or the content height is 0 or less. It draws no text for an
    /// empty text, and it draws no text and no caret for a color with an alpha of 0 after the scale. It changes no tree
    /// state, marks nothing, and allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the call.</param>
    /// <param name="borderBox">The border box of the field in viewport pixels: x, y, width, and height.</param>
    /// <param name="opacity">The product of the opacities from the root to the field, from 0 to 1.</param>
    internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
    {
        float4 content = Layout.ContentBox(in borderBox);
        if (!(content.Z > 0f) || !(content.W > 0f))
        {
            return;
        }

        float x0 = content.X - _scroll;
        bool focused = (States & PseudoStates.Focus) != 0;
        Corners none = default;
        Rgba8 border = default;

        // The selection comes before the text, so the glyphs draw over it and stay readable.
        if (focused && _anchor != _caret)
        {
            Rgba8 selectionColor = Style.Color.ScaleAlpha(opacity * SelectionAlpha);
            if (selectionColor.A != 0)
            {
                int start = Math.Min(_anchor, _caret);
                int end = Math.Max(_anchor, _caret);
                float a = renderer.MeasureText(Style.Font, Style.FontSize, Text[..start], 0f).X;
                float2 b = renderer.MeasureText(Style.Font, Style.FontSize, Text[..end], 0f);
                float4 selection = new(x0 + a, content.Y, b.X - a, b.Y);
                renderer.Rect(in selection, in selectionColor, in none, 0f, in border);
            }
        }

        Rgba8 color = Style.Color.ScaleAlpha(opacity);
        if (_length > 0 && color.A != 0)
        {
            float2 position = new(x0, content.Y);
            renderer.Text(in position, Style.Font, Style.FontSize, in color, Text, 0f);
        }

        // The caret comes after the text, so it draws over the glyphs.
        if (focused && CaretVisible && color.A != 0)
        {
            float2 m = renderer.MeasureText(Style.Font, Style.FontSize, Text[.._caret], 0f);
            float4 caret = new(x0 + m.X, content.Y, CaretWidth, m.Y);
            renderer.Rect(in caret, in color, in none, 0f, in border);
        }
    }

    /// <summary>
    /// The char handler. It replaces the selection with the UTF-8 form of a code point that is text, or it inserts the
    /// code point at the caret when the selection is empty.
    /// </summary>
    /// <remarks>
    /// The fit check uses the length minus the selection plus the new bytes, and it comes before each change. A char that
    /// does not fit changes nothing, keeps the selection, and calls nothing. The handler still stops the walk.
    /// </remarks>
    /// <param name="e">The char event.</param>
    private static void HandleChar(ref CharEvent e)
    {
        if (e.Header.CurrentTarget is not TextField field || !WidgetInput.IsCharInput(field, in e))
        {
            return;
        }

        // A code point that is not a scalar value, or a control character, is not text. It walks on.
        if (!Rune.TryCreate(e.CodePoint, out Rune rune) || Rune.IsControl(rune))
        {
            return;
        }

        e.Header.StopPropagation();
        Span<byte> utf8 = stackalloc byte[4];
        int count = rune.EncodeToUtf8(utf8);

        // The fit check comes before the remove, so a dropped char keeps the selection (Decision 4).
        int start = Math.Min(field._anchor, field._caret);
        int end = Math.Max(field._anchor, field._caret);
        if (field._length - (end - start) + count > field._buffer.Length)
        {
            return;
        }

        if (end > start)
        {
            field.Remove(start, end);
        }

        if (field.Insert(utf8[..count]))
        {
            field.TextChanged?.Invoke(field);
        }
    }

    /// <summary>
    /// The key down handler. It moves the caret for Left, Right, Home, and End, and with <c>Shift</c> it extends the
    /// selection. It removes the selection or one code point for Backspace and Delete, and it calls
    /// <see cref="Submitted"/> for Enter.
    /// </summary>
    /// <remarks>
    /// Left and Right with a selection and no <c>Shift</c> collapse the selection to its start or its end, with no
    /// step. The handler stops the walk of each of the six edit keys, also when the key changes nothing. The main Enter key
    /// with no repeat stops the walk too, also when <see cref="Submitted"/> has no delegate. Each other key walks on. A
    /// remove calls <see cref="TextChanged"/> after the stop and after the change. Enter calls <see cref="Submitted"/>
    /// after the stop.
    /// </remarks>
    /// <param name="e">The key event.</param>
    private static void HandleKeyDown(ref KeyEvent e)
    {
        if (e.Header.CurrentTarget is not TextField field || !WidgetInput.IsEditKey(field, in e))
        {
            return;
        }

        int caret = field._caret;
        int length = field._length;
        int start = Math.Min(field._anchor, caret);
        int end = Math.Max(field._anchor, caret);
        bool selected = end > start;
        bool extend = (e.Modifiers & UiModifiers.Shift) != 0;
        switch (e.Key)
        {
            case WidgetInput.LeftKey:
                e.Header.StopPropagation();
                field.MoveCaret(selected && !extend ? start : PreviousBoundary(field.Text, caret), extend);
                return;
            case WidgetInput.RightKey:
                e.Header.StopPropagation();
                field.MoveCaret(selected && !extend ? end : NextBoundary(field.Text, caret), extend);
                return;
            case WidgetInput.HomeKey:
                e.Header.StopPropagation();
                field.MoveCaret(0, extend);
                return;
            case WidgetInput.EndKey:
                e.Header.StopPropagation();
                field.MoveCaret(length, extend);
                return;
            case WidgetInput.BackspaceKey:
                e.Header.StopPropagation();
                if (selected)
                {
                    field.Remove(start, end);
                    field.TextChanged?.Invoke(field);
                }
                else if (caret > 0)
                {
                    field.Remove(PreviousBoundary(field.Text, caret), caret);
                    field.TextChanged?.Invoke(field);
                }

                return;
            case WidgetInput.DeleteKey:
                e.Header.StopPropagation();
                if (selected)
                {
                    field.Remove(start, end);
                    field.TextChanged?.Invoke(field);
                }
                else if (caret < length)
                {
                    field.Remove(caret, NextBoundary(field.Text, caret));
                    field.TextChanged?.Invoke(field);
                }

                return;
            case WidgetInput.EnterKey:
                // A repeat of Enter walks on, as the repeat rule of the button does. The keypad Enter key is another
                // key code, so it takes the default branch.
                if (e.IsRepeat)
                {
                    return;
                }

                e.Header.StopPropagation();
                field.Submitted?.Invoke(field);
                return;
            default:
                return;
        }
    }

    /// <summary>
    /// The press handler. It stores the pointer x in text space when the press starts a drag on the field.
    /// </summary>
    /// <remarks>
    /// The handler has no measurer, so the caret pass maps the x later (Decision 16). A press with no <c>Shift</c> also
    /// stores the x as the pending anchor, so the anchor collapses to the press point. A press with <c>Shift</c> keeps
    /// the anchor, so it extends the selection. The handler stops the walk.
    /// </remarks>
    /// <param name="e">The press event.</param>
    private static void HandlePointerDown(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not TextField field || !WidgetInput.IsDragStart(field, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        float x = field.TextSpaceX(e.Position.X);
        field._pendingX = x;
        if ((e.Modifiers & UiModifiers.Shift) == 0)
        {
            field._pendingAnchorX = x;
        }
    }

    /// <summary>
    /// The move handler. It stores the pointer x in text space when the field holds the left press.
    /// </summary>
    /// <remarks>
    /// A drag move always extends, so it writes the pending caret x only and keeps the pending anchor of the press. The
    /// handler stops the walk of each drag move.
    /// </remarks>
    /// <param name="e">The move event.</param>
    private static void HandlePointerMove(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not TextField field || !WidgetInput.IsDragMove(field, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        field._pendingX = field.TextSpaceX(e.Position.X);
    }

    /// <summary>Gives the byte index of the next code point boundary after an index.</summary>
    /// <remarks>
    /// The step decodes one code point with <see cref="Rune.DecodeFromUtf8(ReadOnlySpan{byte}, out Rune, out int)"/>. An
    /// invalid sequence is one step of at least one byte, so the step is never 0. An index at or after the end gives the
    /// length of the text, and an index below 0 gives the first step from 0.
    /// </remarks>
    /// <param name="text">The UTF-8 text.</param>
    /// <param name="index">The byte index.</param>
    /// <returns>The byte index of the next boundary, from 0 to the length of the text.</returns>
    internal static int NextBoundary(ReadOnlySpan<byte> text, int index)
    {
        if (index >= text.Length)
        {
            return text.Length;
        }

        int start = Math.Max(0, index);
        Rune.DecodeFromUtf8(text[start..], out _, out int consumed);
        return Math.Min(text.Length, start + Math.Max(1, consumed));
    }

    /// <summary>Gives the byte index of the code point boundary before an index.</summary>
    /// <remarks>
    /// The step decodes one code point with
    /// <see cref="Rune.DecodeLastFromUtf8(ReadOnlySpan{byte}, out Rune, out int)"/>. An invalid sequence is one step of
    /// at least one byte, so the step is never 0. An index at or below 0 gives 0, and an index after the end gives the
    /// first step back from the end.
    /// </remarks>
    /// <param name="text">The UTF-8 text.</param>
    /// <param name="index">The byte index.</param>
    /// <returns>The byte index of the boundary before the index, from 0 to the length of the text.</returns>
    internal static int PreviousBoundary(ReadOnlySpan<byte> text, int index)
    {
        if (index <= 0)
        {
            return 0;
        }

        int end = Math.Min(text.Length, index);
        Rune.DecodeLastFromUtf8(text[..end], out _, out int consumed);
        return Math.Max(0, end - Math.Max(1, consumed));
    }

    /// <summary>Gives the length of the longest prefix of a text that has at most a count of bytes and ends on a boundary.</summary>
    /// <remarks>
    /// The method steps from the start with <see cref="NextBoundary(ReadOnlySpan{byte}, int)"/>, so it uses the same
    /// boundaries as the caret. A code point that does not fit whole is not in the prefix. The step count is at most
    /// the count of bytes, so the cost has the bound of the capacity. The method allocates nothing.
    /// </remarks>
    /// <param name="text">The UTF-8 text.</param>
    /// <param name="maxBytes">The most bytes in the prefix. A value of 0 or less gives 0.</param>
    /// <returns>The length of the prefix, from 0 to the smaller of the length of the text and <paramref name="maxBytes"/>.</returns>
    internal static int CutAtBoundary(ReadOnlySpan<byte> text, int maxBytes)
    {
        if (maxBytes <= 0)
        {
            return 0;
        }

        if (maxBytes >= text.Length)
        {
            return text.Length;
        }

        int index = 0;
        while (true)
        {
            int next = NextBoundary(text, index);
            if (next > maxBytes)
            {
                return index;
            }

            index = next;
        }
    }

    /// <summary>Encodes a UTF-16 text into a buffer, cut at the end of the buffer on a code point boundary.</summary>
    /// <remarks>
    /// <see cref="Utf8.FromUtf16(ReadOnlySpan{char}, Span{byte}, out int, out int, bool, bool)"/> writes whole scalar
    /// values only. On <see cref="OperationStatus.DestinationTooSmall"/>, the bytes that it wrote end on a boundary.
    /// The constructor and <see cref="SetText(string?)"/> share this method.
    /// </remarks>
    /// <param name="text">The text. An empty text gives 0.</param>
    /// <param name="buffer">The buffer of the field.</param>
    /// <returns>The count of the bytes that the method wrote.</returns>
    private static int EncodeUtf16(ReadOnlySpan<char> text, Span<byte> buffer)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        Utf8.FromUtf16(text, buffer, out _, out int written, replaceInvalidSequences: true, isFinalBlock: true);
        return written;
    }

    /// <summary>
    /// Tells if the UTF-8 form of a UTF-16 text, cut at the capacity, has the same bytes as <see cref="Text"/>.
    /// </summary>
    /// <remarks>
    /// The method encodes the text in chunks into a stack buffer, as <see cref="Utf8Text.Utf8Equals"/> does. It limits
    /// each chunk to the free room of the capacity, so a text that is too long compares as its cut form. Thus the same
    /// long text twice marks nothing. It allocates nothing.
    /// </remarks>
    /// <param name="text">The UTF-16 text.</param>
    /// <returns><see langword="true"/> when a set of the text would change no byte.</returns>
    private bool EqualsText(ReadOnlySpan<char> text)
    {
        Span<byte> chunk = stackalloc byte[CompareChunkLength];
        ReadOnlySpan<byte> current = Text;
        int offset = 0;
        while (!text.IsEmpty)
        {
            Span<byte> destination = chunk[..Math.Min(chunk.Length, _buffer.Length - offset)];
            if (destination.IsEmpty)
            {
                break;
            }

            Utf8.FromUtf16(text, destination, out int read, out int written, replaceInvalidSequences: true, isFinalBlock: true);
            if (written == 0)
            {
                break;
            }

            if (offset + written > current.Length || !destination[..written].SequenceEqual(current.Slice(offset, written)))
            {
                return false;
            }

            offset += written;
            text = text[read..];
        }

        return offset == current.Length;
    }

    /// <summary>
    /// Sets the length after a write to the buffer, puts the caret and the anchor at the end, sets the scroll to 0,
    /// restarts the blink, and marks the paint.
    /// </summary>
    /// <remarks>
    /// The anchor at the caret clears the selection. The scroll of 0 shows the start of the new text at once, also on a
    /// field that is not the focus owner. The next caret pass with the focus scrolls to the caret at the end.
    /// </remarks>
    /// <param name="length">The new count of the text bytes. The bytes end on a code point boundary.</param>
    private void WriteLength(int length)
    {
        _length = length;
        _caret = length;
        _anchor = length;
        _scroll = 0f;
        RestartCaretBlink();
        MarkDirty(DirtyFlags.Paint);
    }

    /// <summary>
    /// Inserts UTF-8 bytes at the caret, moves the caret after them, and collapses the anchor to the caret, when the
    /// bytes fit in the buffer.
    /// </summary>
    /// <remarks>
    /// The caller removes the selection first. The fit check comes first, so a dropped insert changes no byte. The tail
    /// of the text moves right with <see cref="Span{T}.CopyTo(Span{T})"/>, which handles the overlap of the two ranges.
    /// A change restarts the blink and marks <see cref="DirtyFlags.Paint"/> only, because the field has no own size.
    /// </remarks>
    /// <param name="utf8">The bytes. They hold whole code points.</param>
    /// <returns><see langword="true"/> when the text changed.</returns>
    private bool Insert(ReadOnlySpan<byte> utf8)
    {
        if (utf8.IsEmpty || utf8.Length > _buffer.Length - _length)
        {
            return false;
        }

        _buffer.AsSpan(_caret, _length - _caret).CopyTo(_buffer.AsSpan(_caret + utf8.Length));
        utf8.CopyTo(_buffer.AsSpan(_caret));
        _length += utf8.Length;
        _caret += utf8.Length;
        _anchor = _caret;
        RestartCaretBlink();
        MarkDirty(DirtyFlags.Paint);
        return true;
    }

    /// <summary>
    /// Removes the bytes from a start index to an end index, and moves the caret and the anchor to the start index.
    /// </summary>
    /// <remarks>
    /// The caller gives a range on code point boundaries with <c>0 &lt;= start &lt; end &lt;= length</c>. The tail of
    /// the text moves left with <see cref="Span{T}.CopyTo(Span{T})"/>, which handles the overlap of the two ranges. The
    /// remove restarts the blink and marks <see cref="DirtyFlags.Paint"/> only, because the field has no own size. It
    /// calls nothing.
    /// </remarks>
    /// <param name="start">The byte index of the first byte to remove.</param>
    /// <param name="end">The byte index after the last byte to remove.</param>
    private void Remove(int start, int end)
    {
        _buffer.AsSpan(end, _length - end).CopyTo(_buffer.AsSpan(start));
        _length -= end - start;
        _caret = start;
        _anchor = start;
        RestartCaretBlink();
        MarkDirty(DirtyFlags.Paint);
    }

    /// <summary>
    /// Moves the caret to a byte index. With <paramref name="extend"/>, the anchor stays. Else the anchor follows the
    /// caret, so the selection becomes empty.
    /// </summary>
    /// <remarks>
    /// The caller gives an index on a code point boundary, from 0 to the length. A move that changes neither the caret
    /// nor the anchor changes nothing and marks nothing. Each other move counts as a caret move: it restarts the blink and
    /// marks <see cref="DirtyFlags.Paint"/> only, because the caret changes no size. Thus a collapse with no caret step
    /// also restarts the blink and marks the paint. It calls nothing.
    /// </remarks>
    /// <param name="index">The new byte index of the caret.</param>
    /// <param name="extend"><see langword="true"/> to keep the anchor and extend the selection.</param>
    private void MoveCaret(int index, bool extend)
    {
        int anchor = extend ? _anchor : index;
        if (index == _caret && anchor == _anchor)
        {
            return;
        }

        _caret = index;
        _anchor = anchor;
        RestartCaretBlink();
        MarkDirty(DirtyFlags.Paint);
    }

    /// <summary>The create-info of a <see cref="TextField"/>.</summary>
    public struct CreateInfo
    {
        /// <summary>
        /// The size of the buffer in bytes. It must be above 0. There is no default (A5), because a silent limit is a
        /// limit that a game finds late.
        /// </summary>
        public required int Capacity;

        /// <summary>
        /// The start text, or <see langword="null"/> for an empty text. The field cuts it at <see cref="Capacity"/>, on
        /// a code point boundary. The default is <see langword="null"/>.
        /// </summary>
        public string? Text;
    }
}
