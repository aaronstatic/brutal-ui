using System;
using System.Globalization;
using BrutalUi.Core;

namespace HelloPanel;

/// <summary>
/// Builds the widget section of the screen: one row for each widget, with the widget and a status label. Also holds
/// the change counts and the delegates of the widgets.
/// </summary>
/// <remarks>
/// <para>
/// The section is a column, and each row is a row. The section and the rows are plain elements. They are pickable,
/// so a press in a gap focuses the panel, the nearest focusable ancestor. Each status label is not pickable, so the
/// pointer hits the row behind it.
/// </para>
/// <para>
/// Row 1 holds the <c>Click me</c> button and its status label. A click adds 1 to <see cref="Clicks"/> and shows the
/// new count in the label.
/// </para>
/// <para>
/// The status rule: the object owns one scratch buffer of 64 bytes. Each delegate writes its text into the scratch
/// buffer and calls <see cref="Label.SetText(ReadOnlySpan{byte})"/>. A count text is a <c>u8</c> prefix and then the
/// count from <see cref="int.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>. The delegate
/// makes no string and boxes no value, so it allocates nothing.
/// </para>
/// <para>
/// The longest text rule: each status label gets its longest text first and then its start text. The label buffer
/// grows by doubling and never shrinks. Thus the buffer reaches its peak size at build time, and a later text never
/// grows it. The counts stop at <see cref="int.MaxValue"/>, so a count text is never longer than the longest text.
/// </para>
/// <para>
/// One delegate object, created at build time: the constructor creates each delegate one time from an instance
/// method and keeps it in a field. The frame loop creates no delegate.
/// </para>
/// <para>
/// The sizes and the colors of the widgets live in <c>HelloScreen.css</c>. The rules <c>.hello-widgets</c>,
/// <c>.hello-row</c>, <c>.hello-row &gt; Label</c>, and <c>.hello-status &gt; Label</c> give the layout. The type
/// rules (for example <c>Button</c> and <c>Button:hover</c>) give the look of each widget.
/// </para>
/// <para>
/// Row 1 also holds a disabled button. It holds <see cref="PseudoStates.Disabled"/> from the build and has no delegate,
/// so the sheet shows the <c>:disabled</c> rule. A press on it raises no click and clears the keyboard focus. Row 2 holds a <see cref="Toggle"/> and
/// its status label. A flip adds 1 to <see cref="Flips"/> and shows <c>Toggle: on</c> or <c>Toggle: off</c>. Row 3
/// holds a <see cref="Slider"/> (0 to 100, step 1, start 50) and its status label. A change adds 1 to
/// <see cref="Values"/> and shows <c>Slider value: &lt;n&gt;</c>. The toggle and the slider have no own size, so the
/// sheet gives their size.
/// </para>
/// <para>
/// Row 4 holds a <see cref="ScrollView"/> of 240 by 80 pixels with 8 item labels (<c>Item 1</c> to <c>Item 8</c>) and
/// a status label. The content is taller than the view, so the view has an offset range to scroll. The item labels are
/// not pickable, so a press on an item hits the view itself and drags the list, and a wheel turn over an item hits the
/// view. A scroll that changes the offset adds 1 to <see cref="Scrolls"/> and shows
/// <c>Scroll offset: &lt;n&gt; px</c>. The view takes no focus, so Tab skips it. The sheet gives the view a fixed size.
/// </para>
/// <para>
/// Row 5 holds a <see cref="TextField"/> and a column (class <c>hello-status</c>) with two status labels. A typed char
/// or a remove adds 1 to <see cref="Edits"/> and shows <c>Text: &lt;text&gt;</c>. The main Enter key adds 1 to
/// <see cref="Submits"/> and shows <c>Submitted: &lt;text&gt;</c>. The field takes the focus, so Tab goes from the
/// slider to the field. The sheet gives the field its size.
/// </para>
/// <para>
/// The capacity: the field holds a fixed buffer of <see cref="TextCapacity"/> (32) bytes. The field drops a char that
/// does not fit, so the text is never longer than <see cref="TextCapacity"/> bytes. The capacity counts bytes, not
/// letters. A letter outside ASCII takes 2 to 4 bytes.
/// </para>
/// <para>
/// The text copy: each text delegate reads <see cref="TextField.Text"/>, a span of the buffer of the field, and copies
/// it after a <c>u8</c> prefix into the scratch buffer. The copy makes no string, so it allocates nothing. The longest
/// text of each text label is its prefix and <see cref="TextCapacity"/> bytes of <c>W</c>. The constructor fills a
/// stack buffer with <c>W</c> for it. The span of <see cref="TextField.Text"/> changes with the next edit, so the label
/// keeps a copy, not the span.
/// </para>
/// <para>
/// The keyboard switch: ImGui and the input adapter both get each key and each char of the window.
/// <c>Program.RunFrames</c> sets <c>GlfwUiInput.KeyboardEnabled</c> to <c>!io.WantCaptureKeyboard</c> before the poll.
/// Thus the field gets no key and no char while an ImGui window wants the keyboard. A screen with no ImGui window can
/// leave the switch on.
/// </para>
/// <para>
/// Copy this file with <c>HelloScreen.cs</c> and <c>HelloScreen.css</c> to start a screen of your game.
/// </para>
/// </remarks>
internal sealed class HelloWidgets
{
    /// <summary>The capacity of the text field in bytes.</summary>
    /// <remarks>
    /// The longest text label is the 11 bytes of <c>Submitted: </c> and this capacity (43 bytes), so it fits the
    /// scratch buffer.
    /// </remarks>
    internal const int TextCapacity = 32;

    /// <summary>The size of the scratch buffer in bytes. Each status text fits in it.</summary>
    private const int ScratchSize = 64;

    /// <summary>The class of the section: the class of the <c>.hello-widgets</c> rule of the sheet.</summary>
    private static readonly ClassId SectionClass = ClassName.Intern("hello-widgets");

    /// <summary>The class of each row: the class of the <c>.hello-row</c> rules of the sheet.</summary>
    private static readonly ClassId RowClass = ClassName.Intern("hello-row");

    /// <summary>The class of the status column of the text field: the class of the <c>.hello-status</c> rule.</summary>
    private static readonly ClassId StatusClass = ClassName.Intern("hello-status");

    /// <summary>The scratch buffer. Each delegate writes its status text here.</summary>
    private readonly byte[] _scratch;

    /// <summary>The status label of the <c>Click me</c> button.</summary>
    private readonly Label _clickStatus;

    /// <summary>The click delegate. The constructor creates it one time from <see cref="OnClicked"/>.</summary>
    private readonly Action<Button> _onClicked;

    /// <summary>The status label of the toggle.</summary>
    private readonly Label _toggleStatus;

    /// <summary>The flip delegate. The constructor creates it one time from <see cref="OnCheckedChanged"/>.</summary>
    private readonly Action<Toggle> _onCheckedChanged;

    /// <summary>The status label of the slider.</summary>
    private readonly Label _sliderStatus;

    /// <summary>The value delegate. The constructor creates it one time from <see cref="OnValueChanged"/>.</summary>
    private readonly Action<Slider> _onValueChanged;

    /// <summary>The status label of the scroll view.</summary>
    private readonly Label _scrollStatus;

    /// <summary>The scroll delegate. The constructor creates it one time from <see cref="OnScrollChanged"/>.</summary>
    private readonly Action<ScrollView> _onScrollChanged;

    /// <summary>The status label that shows the text of the field.</summary>
    private readonly Label _textStatus;

    /// <summary>The status label that shows the text of the last submit.</summary>
    private readonly Label _submitStatus;

    /// <summary>The text delegate. The constructor creates it one time from <see cref="OnTextChanged"/>.</summary>
    private readonly Action<TextField> _onTextChanged;

    /// <summary>The submit delegate. The constructor creates it one time from <see cref="OnSubmitted"/>.</summary>
    private readonly Action<TextField> _onSubmitted;

    /// <summary>Builds the section, the rows, the widgets, and the status labels.</summary>
    /// <remarks>
    /// Add <see cref="Section"/> to the panel. The section needs a context with a measurer, because it holds a
    /// <see cref="Button"/> and <see cref="Label"/> elements.
    /// </remarks>
    public HelloWidgets()
    {
        _scratch = new byte[ScratchSize];
        _onClicked = OnClicked;
        _onCheckedChanged = OnCheckedChanged;
        _onValueChanged = OnValueChanged;
        _onScrollChanged = OnScrollChanged;
        _onTextChanged = OnTextChanged;
        _onSubmitted = OnSubmitted;

        // Row 1: the Click me button and its status label.
        ClickButton = new Button(new Button.CreateInfo { Text = "Click me" })
        {
            Clicked = _onClicked,
        };

        // The label gets the longest text first, so its buffer never grows after the build.
        _clickStatus = new Label(new Label.CreateInfo()) { IsPickable = false };
        _clickStatus.SetText(ClickStatusLongest);
        WriteClickStatus();

        Element clickRow = new();
        clickRow.Classes.Add(RowClass);
        clickRow.Children.Add(ClickButton);
        clickRow.Children.Add(_clickStatus);

        // The disabled button holds the disabled state from the build and has no delegate. The game sets that bit, not
        // the widget, so the sample sets it here to show the :disabled rule.
        DisabledButton = new Button(new Button.CreateInfo { Text = "Disabled" });
        DisabledButton.States |= PseudoStates.Disabled;
        clickRow.Children.Add(DisabledButton);

        // Row 2: the toggle and its status label. The toggle starts unchecked.
        Toggle = new Toggle(new Toggle.CreateInfo()) { CheckedChanged = _onCheckedChanged };
        _toggleStatus = new Label(new Label.CreateInfo()) { IsPickable = false };
        _toggleStatus.SetText(ToggleStatusLongest);
        WriteToggleStatus();

        Element toggleRow = new();
        toggleRow.Classes.Add(RowClass);
        toggleRow.Children.Add(Toggle);
        toggleRow.Children.Add(_toggleStatus);

        // Row 3: the slider and its status label. The slider starts at 50 in the range 0 to 100 with a step of 1.
        Slider = new Slider(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = 1f, Value = 50f })
        {
            ValueChanged = _onValueChanged,
        };
        _sliderStatus = new Label(new Label.CreateInfo()) { IsPickable = false };
        _sliderStatus.SetText(SliderStatusLongest);
        WriteSliderStatus();

        Element sliderRow = new();
        sliderRow.Classes.Add(RowClass);
        sliderRow.Children.Add(Slider);
        sliderRow.Children.Add(_sliderStatus);

        // Row 4: the scroll view and its status label. The sheet gives the view its fixed size. The 8 items are taller
        // than the view together, and they are not pickable, so a press on an item drags the list.
        ScrollView = new ScrollView(default(ScrollView.CreateInfo)) { ScrollChanged = _onScrollChanged };
        AddItem("Item 1"u8);
        AddItem("Item 2"u8);
        AddItem("Item 3"u8);
        AddItem("Item 4"u8);
        AddItem("Item 5"u8);
        AddItem("Item 6"u8);
        AddItem("Item 7"u8);
        AddItem("Item 8"u8);
        _scrollStatus = new Label(new Label.CreateInfo()) { IsPickable = false };
        _scrollStatus.SetText(ScrollStatusLongest);
        WriteScrollStatus();

        Element scrollRow = new();
        scrollRow.Classes.Add(RowClass);
        scrollRow.Children.Add(ScrollView);
        scrollRow.Children.Add(_scrollStatus);

        // Row 5: the text field and a column with two status labels. The field starts empty. The sheet gives the field
        // its size, because the field has no own size.
        TextField = new TextField(new TextField.CreateInfo { Capacity = TextCapacity })
        {
            TextChanged = _onTextChanged,
            Submitted = _onSubmitted,
        };

        // The longest text of each label is the prefix and TextCapacity bytes of W. Each label gets it first, then
        // the start text: the prefix and the empty text.
        Span<byte> longest = stackalloc byte[TextCapacity];
        longest.Fill((byte)'W');
        _textStatus = new Label(new Label.CreateInfo()) { IsPickable = false };
        WriteText(_textStatus, TextStatusPrefix, longest);
        WriteText(_textStatus, TextStatusPrefix, TextField.Text);
        _submitStatus = new Label(new Label.CreateInfo()) { IsPickable = false };
        WriteText(_submitStatus, SubmitStatusPrefix, longest);
        WriteText(_submitStatus, SubmitStatusPrefix, ReadOnlySpan<byte>.Empty);

        // The column has no rule of its own. The default direction of an element is a column.
        Element statusColumn = new();
        statusColumn.Classes.Add(StatusClass);
        statusColumn.Children.Add(_textStatus);
        statusColumn.Children.Add(_submitStatus);

        Element textRow = new();
        textRow.Classes.Add(RowClass);
        textRow.Children.Add(TextField);
        textRow.Children.Add(statusColumn);

        Section = new Element();
        Section.Classes.Add(SectionClass);
        Section.Children.Add(clickRow);
        Section.Children.Add(toggleRow);
        Section.Children.Add(sliderRow);
        Section.Children.Add(scrollRow);
        Section.Children.Add(textRow);
    }

    /// <summary>Gets the section: the column that holds each row. Add it as the last child of the panel.</summary>
    public Element Section { get; }

    /// <summary>Gets the <c>Click me</c> button.</summary>
    /// <remarks>The smoke run aims a press and a release at the center of this button in each frame.</remarks>
    public Button ClickButton { get; }

    /// <summary>Gets the number of clicks of <see cref="ClickButton"/> since the build.</summary>
    /// <remarks>The count stops at <see cref="int.MaxValue"/>.</remarks>
    public int Clicks { get; private set; }

    /// <summary>Gets the disabled button. It holds <see cref="PseudoStates.Disabled"/> and has no delegate.</summary>
    /// <remarks>A press on this button raises no click and clears the keyboard focus. The sheet styles it with the <c>Button:disabled</c> rule.</remarks>
    public Button DisabledButton { get; }

    /// <summary>Gets the toggle.</summary>
    /// <remarks>The smoke run aims a press and a release at the center of this toggle in each frame.</remarks>
    public Toggle Toggle { get; }

    /// <summary>Gets the number of flips of <see cref="Toggle"/> by the user since the build.</summary>
    /// <remarks>The count stops at <see cref="int.MaxValue"/>.</remarks>
    public int Flips { get; private set; }

    /// <summary>Gets the slider.</summary>
    /// <remarks>The smoke run aims a press and a release at a point of this slider in each frame.</remarks>
    public Slider Slider { get; }

    /// <summary>Gets the number of value changes of <see cref="Slider"/> by the user since the build.</summary>
    /// <remarks>The count stops at <see cref="int.MaxValue"/>.</remarks>
    public int Values { get; private set; }

    /// <summary>Gets the scroll view.</summary>
    /// <remarks>
    /// The smoke run aims a scroll event at the center of this view in each frame. The view is not inside another
    /// scroll view, so its center needs no offset.
    /// </remarks>
    public ScrollView ScrollView { get; }

    /// <summary>Gets the number of offset changes of <see cref="ScrollView"/> by the user since the build.</summary>
    /// <remarks>The count stops at <see cref="int.MaxValue"/>.</remarks>
    public int Scrolls { get; private set; }

    /// <summary>Gets the text field. Its capacity is <see cref="TextCapacity"/> bytes.</summary>
    /// <remarks>
    /// The smoke run aims a press and a release at the center of this field in each frame. It also pushes a char or a
    /// Backspace key and a main Enter key with the field as the target.
    /// </remarks>
    public TextField TextField { get; }

    /// <summary>Gets the number of text changes of <see cref="TextField"/> by the user since the build.</summary>
    /// <remarks>A typed char and a remove each add 1. The count stops at <see cref="int.MaxValue"/>.</remarks>
    public int Edits { get; private set; }

    /// <summary>Gets the number of submits of <see cref="TextField"/> since the build.</summary>
    /// <remarks>Each main Enter key with no repeat adds 1. The count stops at <see cref="int.MaxValue"/>.</remarks>
    public int Submits { get; private set; }

    /// <summary>The longest text of the click status label: the prefix and <see cref="int.MaxValue"/>.</summary>
    private static ReadOnlySpan<byte> ClickStatusLongest => "Button clicks: 2147483647"u8;

    /// <summary>The prefix of the click status text.</summary>
    private static ReadOnlySpan<byte> ClickStatusPrefix => "Button clicks: "u8;

    /// <summary>The longest text of the toggle status label.</summary>
    private static ReadOnlySpan<byte> ToggleStatusLongest => "Toggle: off"u8;

    /// <summary>The text of the toggle status label while the toggle is checked.</summary>
    private static ReadOnlySpan<byte> ToggleStatusOn => "Toggle: on"u8;

    /// <summary>The longest text of the slider status label: the prefix and the value 100.</summary>
    private static ReadOnlySpan<byte> SliderStatusLongest => "Slider value: 100"u8;

    /// <summary>The prefix of the slider status text.</summary>
    private static ReadOnlySpan<byte> SliderStatusPrefix => "Slider value: "u8;

    /// <summary>The longest text of the scroll status label: the prefix, <see cref="int.MaxValue"/>, and the suffix.</summary>
    private static ReadOnlySpan<byte> ScrollStatusLongest => "Scroll offset: 2147483647 px"u8;

    /// <summary>The prefix of the scroll status text.</summary>
    private static ReadOnlySpan<byte> ScrollStatusPrefix => "Scroll offset: "u8;

    /// <summary>The suffix of the scroll status text: the unit of the offset.</summary>
    private static ReadOnlySpan<byte> ScrollStatusSuffix => " px"u8;

    /// <summary>The prefix of the text status text. The text of the field follows it.</summary>
    private static ReadOnlySpan<byte> TextStatusPrefix => "Text: "u8;

    /// <summary>The prefix of the submit status text. The text of the field at the submit follows it.</summary>
    private static ReadOnlySpan<byte> SubmitStatusPrefix => "Submitted: "u8;

    /// <summary>The click delegate of <see cref="ClickButton"/>. It counts the click and shows the new count.</summary>
    /// <param name="sender">The button that clicked.</param>
    private void OnClicked(Button sender)
    {
        if (Clicks < int.MaxValue)
        {
            Clicks++;
        }

        WriteClickStatus();
    }

    /// <summary>The flip delegate of <see cref="Toggle"/>. It counts the flip and shows the new state.</summary>
    /// <param name="sender">The toggle that flipped.</param>
    private void OnCheckedChanged(Toggle sender)
    {
        if (Flips < int.MaxValue)
        {
            Flips++;
        }

        WriteToggleStatus();
    }

    /// <summary>The value delegate of <see cref="Slider"/>. It counts the change and shows the new value.</summary>
    /// <param name="sender">The slider that changed.</param>
    private void OnValueChanged(Slider sender)
    {
        if (Values < int.MaxValue)
        {
            Values++;
        }

        WriteSliderStatus();
    }

    /// <summary>The scroll delegate of <see cref="ScrollView"/>. It counts the change and shows the new offset.</summary>
    /// <param name="sender">The scroll view that moved.</param>
    private void OnScrollChanged(ScrollView sender)
    {
        if (Scrolls < int.MaxValue)
        {
            Scrolls++;
        }

        WriteScrollStatus();
    }

    /// <summary>The text delegate of <see cref="TextField"/>. It counts the change and shows the new text.</summary>
    /// <param name="sender">The text field that changed.</param>
    private void OnTextChanged(TextField sender)
    {
        if (Edits < int.MaxValue)
        {
            Edits++;
        }

        WriteText(_textStatus, TextStatusPrefix, sender.Text);
    }

    /// <summary>The submit delegate of <see cref="TextField"/>. It counts the submit and shows the text.</summary>
    /// <param name="sender">The text field that got the Enter key.</param>
    private void OnSubmitted(TextField sender)
    {
        if (Submits < int.MaxValue)
        {
            Submits++;
        }

        WriteText(_submitStatus, SubmitStatusPrefix, sender.Text);
    }

    /// <summary>Adds a not pickable item label to the scroll view.</summary>
    /// <param name="text">The UTF-8 text of the item.</param>
    private void AddItem(ReadOnlySpan<byte> text)
    {
        Label item = new(new Label.CreateInfo()) { IsPickable = false };
        item.SetText(text);
        ScrollView.Children.Add(item);
    }

    /// <summary>Writes <c>Scroll offset: &lt;n&gt; px</c> into the scroll status label, with no allocation.</summary>
    /// <remarks>The offset is a whole number of pixels with the default speed. The method truncates it to an <see cref="int"/>.</remarks>
    private void WriteScrollStatus()
    {
        WriteCount(_scrollStatus, ScrollStatusPrefix, (int)ScrollView.ScrollOffset.Y, ScrollStatusSuffix);
    }

    /// <summary>Writes <c>Toggle: on</c> or <c>Toggle: off</c> into the toggle status label, with no allocation.</summary>
    private void WriteToggleStatus()
    {
        _toggleStatus.SetText(Toggle.Checked ? ToggleStatusOn : ToggleStatusLongest);
    }

    /// <summary>Writes <c>Slider value: &lt;n&gt;</c> into the slider status label, with no allocation.</summary>
    /// <remarks>The step is 1, so the value is a whole number. The method rounds it to an <see cref="int"/>.</remarks>
    private void WriteSliderStatus()
    {
        WriteCount(_sliderStatus, SliderStatusPrefix, (int)MathF.Round(Slider.Value));
    }

    /// <summary>Writes <c>Button clicks: &lt;n&gt;</c> into the click status label, with no allocation.</summary>
    private void WriteClickStatus()
    {
        WriteCount(_clickStatus, ClickStatusPrefix, Clicks);
    }

    /// <summary>
    /// Writes a <c>u8</c> prefix and a UTF-8 text into the scratch buffer, and gives the result to a label. The method
    /// allocates nothing.
    /// </summary>
    /// <remarks>
    /// The label copies the bytes, so a later change of <paramref name="text"/> does not change the label. The text of
    /// the field is at most <see cref="TextCapacity"/> bytes, so each text label fits the scratch buffer.
    /// </remarks>
    /// <param name="label">The status label.</param>
    /// <param name="prefix">The UTF-8 prefix.</param>
    /// <param name="text">The UTF-8 text after the prefix.</param>
    /// <exception cref="InvalidOperationException">The text does not fit in the scratch buffer.</exception>
    private void WriteText(Label label, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> text)
    {
        Span<byte> scratch = _scratch;
        if (!prefix.TryCopyTo(scratch) || !text.TryCopyTo(scratch[prefix.Length..]))
        {
            throw new InvalidOperationException("The status text does not fit in the scratch buffer.");
        }

        label.SetText(scratch[..(prefix.Length + text.Length)]);
    }

    /// <summary>
    /// Writes a <c>u8</c> prefix, a count, and a <c>u8</c> suffix into the scratch buffer, and gives the result to a
    /// label. The method allocates nothing.
    /// </summary>
    /// <param name="label">The status label.</param>
    /// <param name="prefix">The UTF-8 prefix.</param>
    /// <param name="count">The count. The invariant culture formats it.</param>
    /// <param name="suffix">The UTF-8 suffix after the count. The default is no suffix.</param>
    /// <exception cref="InvalidOperationException">The text does not fit in the scratch buffer.</exception>
    private void WriteCount(Label label, ReadOnlySpan<byte> prefix, int count, ReadOnlySpan<byte> suffix = default)
    {
        Span<byte> scratch = _scratch;
        if (!prefix.TryCopyTo(scratch))
        {
            throw new InvalidOperationException("The status text does not fit in the scratch buffer.");
        }

        if (!count.TryFormat(scratch[prefix.Length..], out int written, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("The status text does not fit in the scratch buffer.");
        }

        int length = prefix.Length + written;
        if (!suffix.TryCopyTo(scratch[length..]))
        {
            throw new InvalidOperationException("The status text does not fit in the scratch buffer.");
        }

        label.SetText(scratch[..(length + suffix.Length)]);
    }
}
