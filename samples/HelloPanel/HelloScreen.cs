using System;
using System.Globalization;
using System.IO;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Framework;
using BrutalUi.Markup;

namespace HelloPanel;

/// <summary>
/// Builds the trees of the sample: the widget panel, the state panel, the info panel, and the layout with the two
/// slots. Also fills the pixels of the checker texture of the image, and gives the text of the gate label.
/// </summary>
/// <remarks>
/// <para>
/// The mode model lives in <see cref="HelloModes"/>. That class registers the panels, the layout, and the two modes,
/// and it owns the two commands, the two keys, and the mode button. This class builds trees only. The
/// <see cref="UiManager"/> adds the layout root of <see cref="BuildLayout"/> to the context and mounts the panel
/// roots into the slots.
/// </para>
/// <para>
/// The tree: the layout root holds two slot containers, <c>top</c> and <c>bottom</c>. The widget panel
/// (<see cref="BuildWidgetPanel"/>) sits in <c>top</c> in both modes. The state panel
/// (<see cref="BuildStatePanel"/>) sits in <c>bottom</c> in <c>hello.main</c>, and the info panel
/// (<see cref="BuildInfoPanel"/>) sits in <c>bottom</c> in <c>hello.detail</c>. The layout root is a column with
/// padding, a slot is a column of its panels, and each panel is a column with a fill, a border, and round corners.
/// A panel takes the size of its content.
/// </para>
/// <para>
/// The screen reacts to the pointer and to Tab. The widget panel is not the only focusable element: five widgets take
/// the focus too. These are the <c>Click me</c> button, the toggle, the slider, the text field, and the
/// <c>Mode (F1)</c> button of the mode row. The <c>Add</c> button, the <c>Rebind (Up)</c> button, and the
/// <c>Restore</c> button of the state row take the focus too. The disabled
/// button and the scroll view take no focus. In <c>hello.main</c>, Tab walks the focusable elements in tree order:
/// the panel, <c>Click me</c>, the toggle, the slider, the text field, <c>Mode (F1)</c>, <c>Add</c>,
/// <c>Rebind (Up)</c>, and <c>Restore</c>. A press on
/// one of these widgets gives the focus to the widget. A press on the disabled button clears the focus: the button is
/// focusable but disabled, and the press does not go on to the panel. A press on the scroll view gives the focus to
/// the panel, the nearest focusable ancestor, because the scroll view is not focusable. The seven labels and the image
/// are not pickable, so a point on them hits the panel. The widget section and its rows are pickable, so a press in a
/// gap between the widgets gives the focus to the panel, the nearest focusable ancestor. The layout root and the two
/// slots are not pickable (the layout-root rule and the slot rule), so a point that hits no panel hits no element, and
/// the game gets the pointer.
/// </para>
/// <para>
/// Two state rules of the sheet show the states of the panel. The <c>.hello-panel:hover</c> rule changes the fill of
/// the panel, and the <c>.hello-panel:focus</c> rule changes its border color. The two rules set different members,
/// so the hover and the focus show at the same time. The hover state goes to the hit element only, so the panel
/// loses its hover fill while the pointer is over the widget section. The widget rules use the same pattern: a hover
/// or an active rule sets the fill, and a focus rule sets the border color.
/// </para>
/// <para>
/// The widget section is the child of the widget panel after the image. <see cref="HelloWidgets"/> builds it, holds
/// the status labels and the change counts, and gives the delegates of the widgets. The mode row is the last child,
/// after the widget section. It is a plain element with the <c>hello-row</c> class, and it holds the
/// <c>Mode (F1)</c> button of <see cref="HelloModes"/>. A click on the button runs <c>hello.mode</c>, and the manager
/// applies the other mode at the start of the next update. <see cref="HelloState"/> builds the state row: an
/// <c>Add</c> button, a <c>Rebind (Up)</c> button, a <c>Restore</c> button, and a score label.
/// <see cref="BuildStatePanel"/> puts that row in the state panel, so the row is no longer a child of the widget panel.
/// </para>
/// <para>
/// The second label is the gate label. It shows the two values of <see cref="UiContext.RoutingGate"/>. The frame
/// loop sets its text with <see cref="GateText"/> after each draw. The label starts with the longest gate text, so its
/// buffer never grows after the first frame.
/// </para>
/// <para>
/// The third label is the input label. It shows the pointer delta and one held key of
/// <see cref="UiContext.GameInput"/>, the game view of the input state. The frame loop sets its text with
/// <see cref="InputText"/> after each draw. The gate rule of <see cref="UiContext.GameInput"/> empties the pointer
/// part over the UI, so the label reads 0 over the panel and a non-zero delta over the game area. The rule empties
/// the key part when the UI holds the keyboard focus, so the label then reads <c>Held key: none</c>, and a held W
/// over the game area shows <c>Held key: W</c>. The label starts with the longest input text, so its buffer never
/// grows after the first frame.
/// </para>
/// <para>
/// The fourth label is the stick label. It shows the left stick of gamepad slot 0 of
/// <see cref="UiContext.GameInput"/>, the game view of the input state. The frame loop sets its text with
/// <see cref="StickText"/> after each draw. The gamepad poll stores the stick after the deadzone, so the length is
/// 1 or less. A stick at rest gives <c>Left stick: 0.00, 0.00</c>, and a small tilt inside the deadzone gives the
/// same text. The label starts with the longest stick text, so its buffer never grows after the first frame.
/// </para>
/// <para>
/// The fifth label is the move label. It shows the value of the <c>Move</c> Vector2 action of
/// <see cref="UiActionMap"/>, which the frame loop reads with <see cref="UiActionMap.ReadVector"/>. The frame loop
/// sets its text with <see cref="MoveText"/> after each draw. The action holds a key composite and a stick bind,
/// so the value has a length of 1 or less. Holding W gives <c>Move: 0.00, 1.00</c>, and holding W and D gives
/// <c>Move: 0.71, 0.71</c>. The label starts with the longest move text, so its buffer never grows after the
/// first frame.
/// </para>
/// <para>
/// The sixth label is the scheme label. It shows the control scheme that <see cref="UiActionMap.ActiveScheme"/>
/// reports. The frame loop sets its text with <see cref="SchemeText"/> after each draw, from the name that
/// <see cref="UiActionMap.SchemeName"/> gives back. The report follows the device of the last binding that gave a
/// value, so a key, a mouse button, a key composite, the pointer delta, and a scroll give
/// <c>Scheme: Keyboard / Mouse</c>, and a stick, a gamepad axis, and a gamepad button give <c>Scheme: Gamepad</c>.
/// A value of exactly 0 gives no value, so a stick at rest keeps the last text. The label starts with the longest
/// scheme text, so its buffer never grows after the first frame.
/// </para>
/// <para>
/// The seventh label is the rebind label. It shows the listen state of <see cref="UiActionMap"/> and the last
/// rebind result. The frame loop sets its text with <see cref="RebindText"/> after each draw, from
/// <see cref="UiActionMap.IsRebinding"/> and the result that the slot stores on <see cref="HelloState"/>. A click on
/// the <c>Rebind (Up)</c> button opens the listen, and the label reads <c>Rebind: press a key</c>. A key press then
/// closes the listen, and the label reads <c>Rebind: code &lt;n&gt;</c>. Escape during the listen cancels it, and the
/// label reads <c>Rebind: cancelled</c>. The sample rebinds the <c>NavigateUp</c> key, so a J press moves the focus
/// up in the next frame. The label starts with the longest rebind text, so its buffer never grows after the first
/// frame.
/// </para>
/// <para>
/// The labels and the widgets use font 0, the default font of the renderer. The sample loads no font, so font 0 is
/// the ImGui default font. The panel and the seven labels use the font atlas texture, so they share one ImGui draw
/// command. The seven labels come before the image. The image binds the checker texture, so it opens a second draw
/// command. The widget section comes after the image and uses the font atlas texture again, so it opens a third draw
/// command. The mode row follows the widget section, and its button keeps the same command.
/// </para>
/// <para>
/// The sizes and the colors of the screen live in <c>HelloScreen.css</c>. <see cref="LoadSheet"/> parses the file,
/// and the build methods set the classes of the sheet. The class names of the sheet are <c>hello-slot-root</c>,
/// <c>hello-panel</c>, <c>hello-panel-root</c>, <c>hello-widgets</c>, <c>hello-row</c>, and <c>hello-status</c>. The
/// sheet also holds the type rule <c>SlotContainer</c> for the two slots. The widget rules use the
/// type names of the widgets, for example <c>Button</c>. The tree holds no inline value. The rules
/// <c>.hello-panel &gt; Label</c> and <c>.hello-panel &gt; Image</c> are child rules, so the seven labels and the image
/// must stay direct children of the widget panel. Each status label is a child of a row, so the rule
/// <c>.hello-row &gt; Label</c> gives its values, and the rule <c>.hello-panel &gt; Label</c> does not apply to it.
/// </para>
/// <para>
/// Copy this file, <c>HelloModes.cs</c>, <c>HelloWidgets.cs</c>, and <c>HelloScreen.css</c> to start a screen with two
/// modes of your game. Change a size or a color in the sheet, not in the code.
/// </para>
/// </remarks>
internal static class HelloScreen
{
    /// <summary>The file name of the sheet. The build copies the file next to the sample assembly.</summary>
    private const string SheetFileName = "HelloScreen.css";

    /// <summary>The text of the label.</summary>
    private const string GreetingText = "Hello, brutal-ui";

    /// <summary>The text of the label of the info panel.</summary>
    private const string InfoText = "Detail mode. Press F2 to go back.";

    /// <summary>The class of the layout root: the class of the <c>.hello-slot-root</c> rule of the sheet.</summary>
    private static readonly ClassId SlotRootClass = ClassName.Intern("hello-slot-root");

    /// <summary>The class of the panel: the class of the <c>.hello-panel</c> rules of the sheet.</summary>
    private static readonly ClassId PanelClass = ClassName.Intern("hello-panel");

    /// <summary>The class of each panel root: the class of the <c>.hello-panel-root</c> rule of the sheet.</summary>
    private static readonly ClassId PanelRootClass = ClassName.Intern("hello-panel-root");

    /// <summary>The class of a row: the class of the <c>.hello-row</c> rules of the sheet.</summary>
    private static readonly ClassId RowClass = ClassName.Intern("hello-row");

    /// <summary>The tint of the image: opaque white, so the texels keep their colors.</summary>
    private static readonly Rgba8 ImageTint = new(255, 255, 255, 255);

    /// <summary>The number of bytes of one RGBA texel.</summary>
    private const int BytesPerTexel = 4;

    /// <summary>The clamp limit of one component of the input text.</summary>
    private const float DeltaClampLimit = 9999f;

    /// <summary>The clamp limit of one component of the stick text.</summary>
    private const float StickClampLimit = 1f;

    /// <summary>The first key code that the input text writes as one ASCII character.</summary>
    private const int HeldKeyAsciiFirst = 32;

    /// <summary>The last key code that the input text writes as one ASCII character.</summary>
    private const int HeldKeyAsciiLast = 96;

    /// <summary>The width and the height of one checker cell, in texels.</summary>
    private const int CheckerCellSize = 8;

    /// <summary>The color of the checker cell at the top-left corner: an opaque light gray.</summary>
    private static readonly Rgba8 CheckerLightColor = new(230, 232, 238, 255);

    /// <summary>The color of the other checker cells: an opaque orange.</summary>
    private static readonly Rgba8 CheckerDarkColor = new(232, 120, 48, 255);

    /// <summary>
    /// Fills a checker pattern of two opaque colors, in cells of 8 by 8 texels. The cell at the top-left corner has the
    /// light color.
    /// </summary>
    /// <param name="rgba">The destination: <c>size.X * size.Y</c> texels of 4 bytes (R, G, B, A), row by row from the top.</param>
    /// <param name="size">The size of the texture in texels.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component of <paramref name="size"/> is 0 or less.</exception>
    /// <exception cref="ArgumentException">The length of <paramref name="rgba"/> is not <c>size.X * size.Y * 4</c>.</exception>
    public static void FillChecker(Span<byte> rgba, int2 size)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Each component of the size must be 1 or more.");
        }

        // The texel count of two int components fits in 64 bits. A span holds int.MaxValue bytes or fewer,
        // so the check of the texel count comes before the multiplication by the texel size.
        long texelCount = (long)size.X * size.Y;
        if (texelCount > int.MaxValue / BytesPerTexel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"A texture of {size.X} x {size.Y} needs more bytes than a span can hold.");
        }

        long byteCount = texelCount * BytesPerTexel;
        if (rgba.Length != byteCount)
        {
            throw new ArgumentException(
                $"The span has {rgba.Length} bytes. A texture of {size.X} x {size.Y} needs {byteCount} bytes.",
                nameof(rgba));
        }

        int offset = 0;
        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                bool light = ((x / CheckerCellSize) + (y / CheckerCellSize)) % 2 == 0;
                Rgba8 color = light ? CheckerLightColor : CheckerDarkColor;
                rgba[offset] = color.R;
                rgba[offset + 1] = color.G;
                rgba[offset + 2] = color.B;
                rgba[offset + 3] = color.A;
                offset += BytesPerTexel;
            }
        }
    }

    /// <summary>Reads and parses <c>HelloScreen.css</c> from the folder of the sample assembly.</summary>
    /// <remarks>
    /// The method reads the bytes of the file and passes them to the UTF-8 overload of
    /// <see cref="StyleSheetParser.Parse(ReadOnlySpan{byte}, string)"/>. Each error of the parse names the file, the
    /// line, and the column. The method needs no GPU and no window, so call it before the window opens.
    /// </remarks>
    /// <returns>The sheet of the screen. Add it to the context with <see cref="UiContext.AddSheet(StyleSheet)"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The sheet has one error or more. The message holds one <c>HelloScreen.css(line,column): kind: text</c> line for
    /// each error.
    /// </exception>
    /// <exception cref="System.IO.IOException">The sample cannot read the file.</exception>
    public static StyleSheet LoadSheet()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, SheetFileName));
        return new StyleSheetParser().Parse(bytes, SheetFileName).GetSheetOrThrow();
    }

    /// <summary>Builds the widget panel: the seven labels, the image, the widget section, and the mode row.</summary>
    /// <remarks>
    /// <para>
    /// The factory of <c>hello.widgets</c> calls this method (see <see cref="HelloModes"/>). The panel is a column
    /// with a fill, a border, and round corners, and it takes the size of its content. The tree sets the classes
    /// <c>hello-panel</c> and <c>hello-panel-root</c> and no inline value. The context must have a measurer, because
    /// the tree holds <see cref="Label"/> elements and two <see cref="Button"/> elements. The image takes the size of
    /// <paramref name="checker"/>, one pixel for each texel.
    /// </para>
    /// <para>
    /// The panel is focusable. So are <c>Click me</c>, the toggle, the slider, the text field, and
    /// <paramref name="modeButton"/>. The disabled button and the scroll view take no focus. The seven labels and the
    /// image are not pickable, so a point on them hits the panel. The gate label starts with the longest text of
    /// <see cref="GateText"/>. The input label comes right after the gate label and starts with the longest text of
    /// <see cref="InputText"/>. The stick label comes right after the input label and starts with the longest text
    /// of <see cref="StickText"/>. The move label comes right after the stick label and starts with the longest
    /// text of <see cref="MoveText"/>. The scheme label comes right after the move label and starts with the longest
    /// text of <see cref="SchemeText"/>. The rebind label comes right after the scheme label and starts with the
    /// longest text of <see cref="RebindText"/>.
    /// </para>
    /// <para>
    /// The seven labels and the image are direct children of the panel, because the rules
    /// <c>.hello-panel &gt; Label</c> and <c>.hello-panel &gt; Image</c> are child rules. The widget section of
    /// <paramref name="widgets"/> follows the image. Its status labels are children of a row, so the rule
    /// <c>.hello-row &gt; Label</c> styles them. The mode row is the last child. It carries the <c>hello-row</c>
    /// class and holds <paramref name="modeButton"/>.
    /// </para>
    /// </remarks>
    /// <param name="checker">
    /// The texture of the image. The none handle is valid: the image then takes no space and draws nothing.
    /// </param>
    /// <param name="modeButton">The <c>Mode (F1)</c> button. A click runs <c>hello.mode</c>.</param>
    /// <param name="gateLabel">The gate label. Set its text with <see cref="GateText"/>.</param>
    /// <param name="inputLabel">The input label. Set its text with <see cref="InputText"/>.</param>
    /// <param name="stickLabel">The stick label. Set its text with <see cref="StickText"/>.</param>
    /// <param name="moveLabel">The move label. Set its text with <see cref="MoveText"/>.</param>
    /// <param name="schemeLabel">The scheme label. Set its text with <see cref="SchemeText"/>.</param>
    /// <param name="rebindLabel">The rebind label. Set its text with <see cref="RebindText"/>.</param>
    /// <param name="widgets">
    /// The widget section, its widgets, its status labels, and its change counts. The smoke run aims its widget events
    /// at the widgets and reads the counts.
    /// </param>
    /// <returns>The root element of the widget panel.</returns>
    public static Element BuildWidgetPanel(
        TextureHandle checker, Button modeButton, out Label gateLabel, out Label inputLabel, out Label stickLabel,
        out Label moveLabel, out Label schemeLabel, out Label rebindLabel, out HelloWidgets widgets)
    {
        // The rule .hello-panel > Label gives the color and the size. The label keeps font 0, the default font.
        // A label is not pickable, so the pointer hits the panel behind it.
        Label greeting = new(new Label.CreateInfo { Text = GreetingText }) { IsPickable = false };

        // The gate label starts with the longest gate text, so its buffer holds each gate text with no growth.
        gateLabel = new Label(new Label.CreateInfo()) { IsPickable = false };
        gateLabel.SetText(PointerYesFocusYes);

        // The input label starts with the longest input text, so its buffer holds each input text with no growth.
        inputLabel = new Label(new Label.CreateInfo()) { IsPickable = false };
        inputLabel.SetText(LongestInputText);

        // The stick label starts with the longest stick text, so its buffer holds each stick text with no growth.
        stickLabel = new Label(new Label.CreateInfo()) { IsPickable = false };
        stickLabel.SetText(LongestStickText);

        // The move label starts with the longest move text, so its buffer holds each move text with no growth.
        moveLabel = new Label(new Label.CreateInfo()) { IsPickable = false };
        moveLabel.SetText(LongestMoveText);

        // The scheme label starts with the longest scheme text, so its buffer holds each scheme text with no growth.
        schemeLabel = new Label(new Label.CreateInfo()) { IsPickable = false };
        schemeLabel.SetText(LongestSchemeText);

        // The rebind label starts with the longest rebind text, so its buffer holds each rebind text with no growth.
        rebindLabel = new Label(new Label.CreateInfo()) { IsPickable = false };
        rebindLabel.SetText(LongestRebindText);

        // The rule .hello-panel > Image gives the top margin. The tint is a value of the image, not of the style.
        Image checkerImage = new(new Image.CreateInfo
        {
            Texture = checker,
            Tint = ImageTint,
            ScaleMode = ImageScaleMode.StretchToFill,
        })
        {
            IsPickable = false,
        };

        // The widget section: a column of rows. Each row holds a widget and its status label.
        widgets = new HelloWidgets();

        // The mode row comes last. Its button runs hello.mode, so Tab reaches it after the text field.
        Element modeRow = new();
        modeRow.Classes.Add(RowClass);
        modeRow.Children.Add(modeButton);

        // The child rules read the parent, so the labels and the image are direct children of the panel.
        // The seven labels come before the image, so the panel and the seven labels share one draw command.
        // The panel is focusable. So are Click me, the toggle, the slider, the text field, and Mode (F1). The
        // disabled button and the scroll view take no focus.
        Element panel = new() { IsFocusable = true };
        panel.Classes.Add(PanelClass);
        panel.Classes.Add(PanelRootClass);
        panel.Children.Add(greeting);
        panel.Children.Add(gateLabel);
        panel.Children.Add(inputLabel);
        panel.Children.Add(stickLabel);
        panel.Children.Add(moveLabel);
        panel.Children.Add(schemeLabel);
        panel.Children.Add(rebindLabel);
        panel.Children.Add(checkerImage);
        panel.Children.Add(widgets.Section);
        panel.Children.Add(modeRow);

        return panel;
    }

    /// <summary>Builds the state panel: the row of <see cref="HelloState"/>.</summary>
    /// <remarks>
    /// <para>
    /// The factory of <c>hello.state</c> calls this method (see <see cref="HelloModes"/>). The panel holds the row and
    /// is not focusable; the <c>Add</c> button of the row takes the focus. The panel sets the classes
    /// <c>hello-panel</c> and <c>hello-panel-root</c>, so it shows with the fill, the border, and the round corners of
    /// the sheet, and the margin of the root rule keeps it apart from the panel above it.
    /// </para>
    /// <para>
    /// The score label is a child of the row, so the rule <c>.hello-row &gt; Label</c> styles it. The mode keeps the
    /// panel: the switch to <c>hello.detail</c> unmounts this root, and a back step mounts the same root with its
    /// score.
    /// </para>
    /// </remarks>
    /// <param name="state">The state of the sample. Its row is the only child of the panel.</param>
    /// <returns>The root element of the state panel.</returns>
    public static Element BuildStatePanel(HelloState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        Element panel = new();
        panel.Classes.Add(PanelClass);
        panel.Classes.Add(PanelRootClass);
        panel.Children.Add(state.Row);
        return panel;
    }

    /// <summary>Builds the info panel: one label.</summary>
    /// <remarks>
    /// The factory of <c>hello.info</c> calls this method (see <see cref="HelloModes"/>). The mode
    /// <c>hello.detail</c> shows the panel in the <c>bottom</c> slot, in place of the state panel. The label is a
    /// direct child, so the rule <c>.hello-panel &gt; Label</c> gives its color and its size, and it is not pickable.
    /// The panel is not focusable.
    /// </remarks>
    /// <returns>The root element of the info panel.</returns>
    public static Element BuildInfoPanel()
    {
        Label info = new(new Label.CreateInfo { Text = InfoText }) { IsPickable = false };

        Element panel = new();
        panel.Classes.Add(PanelClass);
        panel.Classes.Add(PanelRootClass);
        panel.Children.Add(info);
        return panel;
    }

    /// <summary>Builds the layout: the root and the two slots.</summary>
    /// <remarks>
    /// <para>
    /// The factory of <c>hello.layout</c> calls this method (see <see cref="HelloModes"/>). The manager adds the root
    /// to the context; the game does not add it (the root rule of <see cref="UiManager"/>). The root fills the viewport
    /// and puts each slot at its top-left corner, inside the root padding. The root is a column, so the <c>top</c>
    /// slot stands above the <c>bottom</c> slot, and a slot is a column of its panels.
    /// </para>
    /// <para>
    /// The root draws nothing, and it is not pickable (the layout-root rule). A slot is not pickable too (the slot
    /// rule): a point between two panels hits no element, and the game gets the pointer. Each mounted panel keeps its
    /// own pick.
    /// </para>
    /// </remarks>
    /// <returns>The root element of the layout.</returns>
    public static Element BuildLayout()
    {
        SlotContainer top = new("top"u8) { IsPickable = false };
        SlotContainer bottom = new("bottom"u8) { IsPickable = false };

        // The rule .hello-slot-root stops the stretch, so each slot keeps the size of its content.
        Element root = new() { IsPickable = false };
        root.Classes.Add(SlotRootClass);
        root.Children.Add(top);
        root.Children.Add(bottom);
        return root;
    }

    /// <summary>Gives the text of the gate label for a value of the routing gate.</summary>
    /// <remarks>
    /// The method returns one of four UTF-8 literals. The literals live in the data of the assembly, so the call
    /// allocates nothing. Pass the result to <see cref="Label.SetText(ReadOnlySpan{byte})"/>.
    /// </remarks>
    /// <param name="gate">The value of <see cref="UiContext.RoutingGate"/>.</param>
    /// <returns>The UTF-8 text that names the two values of <paramref name="gate"/>.</returns>
    public static ReadOnlySpan<byte> GateText(UiRoutingGate gate)
    {
        if (gate.PointerOverUi)
        {
            return gate.KeyboardFocusInUi ? PointerYesFocusYes : PointerYesFocusNo;
        }

        return gate.KeyboardFocusInUi ? PointerNoFocusYes : PointerNoFocusNo;
    }

    /// <summary>The gate text when the pointer is not over the UI and the UI has no keyboard focus.</summary>
    private static ReadOnlySpan<byte> PointerNoFocusNo => "Pointer over UI: no. Keyboard focus in UI: no."u8;

    /// <summary>The gate text when the pointer is not over the UI and the UI has the keyboard focus.</summary>
    private static ReadOnlySpan<byte> PointerNoFocusYes => "Pointer over UI: no. Keyboard focus in UI: yes."u8;

    /// <summary>The gate text when the pointer is over the UI and the UI has no keyboard focus.</summary>
    private static ReadOnlySpan<byte> PointerYesFocusNo => "Pointer over UI: yes. Keyboard focus in UI: no."u8;

    /// <summary>
    /// The gate text when the pointer is over the UI and the UI has the keyboard focus. This is the longest gate text.
    /// </summary>
    private static ReadOnlySpan<byte> PointerYesFocusYes => "Pointer over UI: yes. Keyboard focus in UI: yes."u8;

    /// <summary>
    /// Gives the text of the input label for the game view of the input state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method writes <c>Game pointer delta: &lt;x&gt;, &lt;y&gt;. Held key: &lt;key&gt;</c> into
    /// <paramref name="scratch"/> and returns the written part. It clamps each component of
    /// <see cref="UiPointerState.Delta"/> to -9999 to 9999, rounds the value to an <see cref="int"/>, and writes it.
    /// <see cref="int.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)"/> writes each count with
    /// the invariant culture, so the method makes no string and boxes no value.
    /// </para>
    /// <para>
    /// The held-key part names the lowest key code that <see cref="UiKeyState.IsDown(int)"/> reports down. A code from
    /// 32 to 96 writes as one ASCII character, and another code writes as its number. With no key down, the part
    /// writes <c>none</c>. The longest text needs 48 bytes, so a scratch buffer of 64 bytes always holds one input
    /// text.
    /// </para>
    /// <para>
    /// Pass the result to <see cref="Label.SetText(ReadOnlySpan{byte})"/>. Read <see cref="UiContext.GameInput"/> after
    /// <see cref="UiContext.Update(float)"/>. The gate rule empties the pointer part of that view over the UI, so the
    /// delta is (0, 0) there, and the label reads 0. The rule empties the key part when the UI holds the keyboard
    /// focus, so the label then reads <c>Held key: none</c>.
    /// </para>
    /// </remarks>
    /// <param name="game">The game view of the input state: <see cref="UiContext.GameInput"/>.</param>
    /// <param name="scratch">The destination buffer. Give it 64 bytes.</param>
    /// <returns>The part of <paramref name="scratch"/> that holds the text.</returns>
    /// <exception cref="InvalidOperationException">The text does not fit in <paramref name="scratch"/>.</exception>
    public static ReadOnlySpan<byte> InputText(in UiInputState game, Span<byte> scratch)
    {
        // The write pattern of HelloWidgets.WriteCount: one copy or format for each part, and no allocation.
        if (!InputPrefix.TryCopyTo(scratch))
        {
            throw new InvalidOperationException("The input text does not fit in the scratch buffer.");
        }

        int length = InputPrefix.Length;
        int x = DeltaComponent(game.Pointer.Delta.X);
        if (!x.TryFormat(scratch[length..], out int written, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("The input text does not fit in the scratch buffer.");
        }

        length += written;
        if (!InputSeparator.TryCopyTo(scratch[length..]))
        {
            throw new InvalidOperationException("The input text does not fit in the scratch buffer.");
        }

        length += InputSeparator.Length;
        int y = DeltaComponent(game.Pointer.Delta.Y);
        if (!y.TryFormat(scratch[length..], out written, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("The input text does not fit in the scratch buffer.");
        }

        length += written;
        if (!HeldKeyPrefix.TryCopyTo(scratch[length..]))
        {
            throw new InvalidOperationException("The input text does not fit in the scratch buffer.");
        }

        length += HeldKeyPrefix.Length;
        int heldKey = LowestHeldKey(in game);
        if (heldKey < 0)
        {
            if (!HeldKeyNone.TryCopyTo(scratch[length..]))
            {
                throw new InvalidOperationException("The input text does not fit in the scratch buffer.");
            }

            length += HeldKeyNone.Length;
        }
        else if (heldKey >= HeldKeyAsciiFirst && heldKey <= HeldKeyAsciiLast)
        {
            scratch[length] = (byte)heldKey;
            length++;
        }
        else
        {
            if (!heldKey.TryFormat(scratch[length..], out written, default, CultureInfo.InvariantCulture))
            {
                throw new InvalidOperationException("The input text does not fit in the scratch buffer.");
            }

            length += written;
        }

        return scratch[..length];
    }

    /// <summary>The prefix of the input text.</summary>
    private static ReadOnlySpan<byte> InputPrefix => "Game pointer delta: "u8;

    /// <summary>The text between the two components of the input text.</summary>
    private static ReadOnlySpan<byte> InputSeparator => ", "u8;

    /// <summary>The text between the pointer delta and the held key of the input text.</summary>
    private static ReadOnlySpan<byte> HeldKeyPrefix => ". Held key: "u8;

    /// <summary>The held-key value that the input text writes when no key is down.</summary>
    private static ReadOnlySpan<byte> HeldKeyNone => "none"u8;

    /// <summary>
    /// The longest input text: the prefix, the separator, two clamped components of five bytes, and the held-key part
    /// with <c>none</c>. The held-key part of <c>none</c> is longer than each code part. The input label starts with
    /// it, so its buffer never grows after the first frame.
    /// </summary>
    private static ReadOnlySpan<byte> LongestInputText => "Game pointer delta: -9999, -9999. Held key: none"u8;

    /// <summary>Clamps one component of the pointer delta and rounds it to an <see cref="int"/>.</summary>
    /// <remarks>
    /// The method clamps the <see cref="float"/> before the cast, so the cast always sees a value inside the range of
    /// the result. A component that is not finite gives 0. Phase 1 writes a finite delta for every frame, so the check
    /// guards a later change.
    /// </remarks>
    /// <param name="value">One component of <see cref="UiPointerState.Delta"/>.</param>
    /// <returns>The rounded value, from -9999 to 9999.</returns>
    private static int DeltaComponent(float value)
    {
        float clamped = float.IsFinite(value) ? Math.Clamp(value, -DeltaClampLimit, DeltaClampLimit) : 0f;
        return (int)MathF.Round(clamped);
    }

    /// <summary>Finds the lowest key code that the game view reports down.</summary>
    /// <remarks>
    /// The method walks the codes from 0 to <see cref="UiKeyState.KeyCount"/> minus 1 and stops at the first
    /// <see cref="UiKeyState.IsDown(int)"/> answer of <see langword="true"/>. The read of the key part copies nothing,
    /// and the loop allocates nothing.
    /// </remarks>
    /// <param name="game">The game view of the input state: <see cref="UiContext.GameInput"/>.</param>
    /// <returns>The lowest held code, or -1 when no key is down.</returns>
    private static int LowestHeldKey(in UiInputState game)
    {
        for (int key = 0; key < UiKeyState.KeyCount; key++)
        {
            if (game.Keys.IsDown(key))
            {
                return key;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gives the text of the stick label for the left stick of one gamepad slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method writes <c>Left stick: &lt;x&gt;, &lt;y&gt;</c> into <paramref name="scratch"/> and returns the
    /// written part. Each component goes through <see cref="StickComponent"/>, so the value is -1 to 1 with two
    /// decimals. <see cref="float.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)"/> writes
    /// each component with the <c>F2</c> format and the invariant culture, so the method makes no string and boxes
    /// no value.
    /// </para>
    /// <para>
    /// The longest text needs 24 bytes, so a scratch buffer of 32 bytes always holds one stick text. Pass the
    /// result to <see cref="Label.SetText(ReadOnlySpan{byte})"/>. Read <see cref="UiContext.GameInput"/> after
    /// <see cref="UiContext.Update(float)"/>. The poll stores the stick of slot 0 after the deadzone, so a stick at
    /// rest reads <c>Left stick: 0.00, 0.00</c>, and a small tilt inside the deadzone gives the same text.
    /// </para>
    /// </remarks>
    /// <param name="pad">One gamepad slot: <see cref="UiInputState.Gamepad(int)"/>.</param>
    /// <param name="scratch">The destination buffer. Give it 32 bytes.</param>
    /// <returns>The part of <paramref name="scratch"/> that holds the text.</returns>
    /// <exception cref="InvalidOperationException">The text does not fit in <paramref name="scratch"/>.</exception>
    public static ReadOnlySpan<byte> StickText(in UiGamepadState pad, Span<byte> scratch)
    {
        // The write pattern of InputText: one copy or format for each part, and no allocation.
        if (!StickPrefix.TryCopyTo(scratch))
        {
            throw new InvalidOperationException("The stick text does not fit in the scratch buffer.");
        }

        int length = StickPrefix.Length;
        float x = StickComponent(pad.LeftStick.X);
        if (!x.TryFormat(scratch[length..], out int written, "F2", CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("The stick text does not fit in the scratch buffer.");
        }

        length += written;
        if (!StickSeparator.TryCopyTo(scratch[length..]))
        {
            throw new InvalidOperationException("The stick text does not fit in the scratch buffer.");
        }

        length += StickSeparator.Length;
        float y = StickComponent(pad.LeftStick.Y);
        if (!y.TryFormat(scratch[length..], out written, "F2", CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("The stick text does not fit in the scratch buffer.");
        }

        length += written;
        return scratch[..length];
    }

    /// <summary>The prefix of the stick text.</summary>
    private static ReadOnlySpan<byte> StickPrefix => "Left stick: "u8;

    /// <summary>The text between the two components of the stick text.</summary>
    private static ReadOnlySpan<byte> StickSeparator => ", "u8;

    /// <summary>
    /// The longest stick text: the prefix, the separator, and two clamped components of five bytes. The text is 24
    /// bytes. The stick label starts with it, so its buffer never grows after the first frame.
    /// </summary>
    private static ReadOnlySpan<byte> LongestStickText => "Left stick: -1.00, -1.00"u8;

    /// <summary>Clamps one stick component and rounds it to two decimals.</summary>
    /// <remarks>
    /// A component that is not finite gives 0. The method clamps the value to -1 to 1, then rounds it to two
    /// decimals. The <c>+ 0f</c> turns -0 into +0, so the label never shows <c>-0.00</c>. The poll stores a stick
    /// with a length of 1 or less, so each component is already -1 to 1; the clamp guards a hand-made state.
    /// </remarks>
    /// <param name="value">One component of <see cref="UiGamepadState.LeftStick"/>.</param>
    /// <returns>The clamped and rounded value, from -1 to 1.</returns>
    private static float StickComponent(float value)
    {
        float clamped = float.IsFinite(value) ? Math.Clamp(value, -StickClampLimit, StickClampLimit) : 0f;
        return MathF.Round(clamped, 2) + 0f;
    }

    /// <summary>
    /// Gives the text of the move label for the value of a Vector2 action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method writes <c>Move: &lt;x&gt;, &lt;y&gt;</c> into <paramref name="scratch"/> and returns the written
    /// part. Each component goes through <see cref="StickComponent"/>, so the value is -1 to 1 with two decimals.
    /// The <c>Move</c> action of the sample holds a key composite and a stick bind, and both groups clamp, so its
    /// value is already inside that range. A bigger component renders as <c>1.00</c> or <c>-1.00</c>.
    /// <see cref="float.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)"/> writes each
    /// component with the <c>F2</c> format and the invariant culture, so the method makes no string and boxes no
    /// value.
    /// </para>
    /// <para>
    /// The longest text needs 18 bytes, so a scratch buffer of 32 bytes always holds one move text. Pass the
    /// result to <see cref="Label.SetText(ReadOnlySpan{byte})"/>. Read the value with
    /// <see cref="UiActionMap.ReadVector"/> after <see cref="UiContext.Update(float)"/>, while the map is active.
    /// </para>
    /// </remarks>
    /// <param name="value">The value of a Vector2 action.</param>
    /// <param name="scratch">The destination buffer. Give it 32 bytes.</param>
    /// <returns>The part of <paramref name="scratch"/> that holds the text.</returns>
    /// <exception cref="InvalidOperationException">The text does not fit in <paramref name="scratch"/>.</exception>
    public static ReadOnlySpan<byte> MoveText(float2 value, Span<byte> scratch)
    {
        // The write pattern of StickText: one copy or format for each part, and no allocation.
        if (!MovePrefix.TryCopyTo(scratch))
        {
            throw new InvalidOperationException("The move text does not fit in the scratch buffer.");
        }

        int length = MovePrefix.Length;
        float x = StickComponent(value.X);
        if (!x.TryFormat(scratch[length..], out int written, "F2", CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("The move text does not fit in the scratch buffer.");
        }

        length += written;
        if (!MoveSeparator.TryCopyTo(scratch[length..]))
        {
            throw new InvalidOperationException("The move text does not fit in the scratch buffer.");
        }

        length += MoveSeparator.Length;
        float y = StickComponent(value.Y);
        if (!y.TryFormat(scratch[length..], out written, "F2", CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("The move text does not fit in the scratch buffer.");
        }

        length += written;
        return scratch[..length];
    }

    /// <summary>The prefix of the move text.</summary>
    private static ReadOnlySpan<byte> MovePrefix => "Move: "u8;

    /// <summary>The text between the two components of the move text.</summary>
    private static ReadOnlySpan<byte> MoveSeparator => ", "u8;

    /// <summary>
    /// The longest move text: the prefix, the separator, and two clamped components of five bytes. The text is 18
    /// bytes. The move label starts with it, so its buffer never grows after the first frame.
    /// </summary>
    private static ReadOnlySpan<byte> LongestMoveText => "Move: -1.00, -1.00"u8;

    /// <summary>
    /// Writes the text of the scheme label: the name of the active control scheme of
    /// <see cref="UiActionMap"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method writes <c>Scheme: &lt;scheme&gt;</c> into <paramref name="scratch"/> and returns the written part.
    /// It copies <c>Keyboard / Mouse</c> for the <see cref="UiScheme.KeyboardMouse"/> name and <c>Gamepad</c> for the
    /// <see cref="UiScheme.Gamepad"/> name. For any other name it copies the name bytes. The sample owns these words,
    /// because a scheme to art map is the game's. A prompt glyph, a sprite, and a font icon live in the game, not in
    /// the framework.
    /// </para>
    /// <para>
    /// The method compares the bytes of the name, so it needs no string and allocates nothing. The longest text needs
    /// 24 bytes, so a scratch buffer of 32 bytes holds each standard name. A custom name longer than 24 bytes does not
    /// fit, and the method throws, like the other formatters here. Pass the result to
    /// <see cref="Label.SetText(ReadOnlySpan{byte})"/>. Read the name with <see cref="UiActionMap.SchemeName"/> of
    /// <see cref="UiActionMap.ActiveScheme"/> after <see cref="UiContext.Update(float)"/>.
    /// </para>
    /// </remarks>
    /// <param name="scheme">
    /// The name bytes of the active scheme. Give the result of <see cref="UiActionMap.SchemeName"/>.
    /// </param>
    /// <param name="scratch">The destination buffer. Give it 32 bytes.</param>
    /// <returns>The part of <paramref name="scratch"/> that holds the text.</returns>
    /// <exception cref="InvalidOperationException">The text does not fit in <paramref name="scratch"/>.</exception>
    public static ReadOnlySpan<byte> SchemeText(ReadOnlySpan<byte> scheme, Span<byte> scratch)
    {
        // The write pattern of MoveText: one copy for each part, and no allocation.
        if (!SchemePrefix.TryCopyTo(scratch))
        {
            throw new InvalidOperationException("The scheme text does not fit in the scratch buffer.");
        }

        int length = SchemePrefix.Length;
        ReadOnlySpan<byte> text = scheme.SequenceEqual(UiScheme.KeyboardMouse)
            ? KeyboardMouseText
            : scheme.SequenceEqual(UiScheme.Gamepad)
                ? GamepadText
                : scheme;
        if (!text.TryCopyTo(scratch[length..]))
        {
            throw new InvalidOperationException("The scheme text does not fit in the scratch buffer.");
        }

        return scratch[..(length + text.Length)];
    }

    /// <summary>The prefix of the scheme text.</summary>
    private static ReadOnlySpan<byte> SchemePrefix => "Scheme: "u8;

    /// <summary>The display text of the keyboard and mouse scheme name.</summary>
    private static ReadOnlySpan<byte> KeyboardMouseText => "Keyboard / Mouse"u8;

    /// <summary>The display text of the gamepad scheme name.</summary>
    private static ReadOnlySpan<byte> GamepadText => "Gamepad"u8;

    /// <summary>
    /// The longest scheme text: the prefix and the keyboard and mouse text. The text is 24 bytes. The scheme label
    /// starts with it, so its buffer never grows after the first frame.
    /// </summary>
    private static ReadOnlySpan<byte> LongestSchemeText => "Scheme: Keyboard / Mouse"u8;

    /// <summary>
    /// Writes the text of the rebind label: the listen state of <see cref="UiActionMap"/> and its last result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method writes <c>Rebind: &lt;text&gt;</c> into <paramref name="scratch"/> and returns the written part.
    /// While <paramref name="listening"/> is <see langword="true"/>, it writes <c>press a key</c>. When
    /// <paramref name="done"/> is <see langword="false"/>, it writes <c>idle</c>: the result slot has stored no
    /// result yet. When the listen is closed and one result stands, it writes one text per status:
    /// <c>code &lt;n&gt;</c> for <see cref="UiActionMap.RebindBound"/>, <c>cancelled</c> for
    /// <see cref="UiActionMap.RebindCancelled"/>, and <c>rejected &lt;n&gt;</c> for
    /// <see cref="UiActionMap.RebindRejected"/>. The <paramref name="done"/> flag carries the idle test, not the
    /// code: a mouse-button capture stores a code from 0 to 7. For any other status the method writes
    /// <c>listening off</c>.
    /// </para>
    /// <para>
    /// <see cref="int.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)"/> writes each code with
    /// the invariant culture, so the method makes no string and boxes no value. The longest text needs 20 bytes, so
    /// a scratch buffer of 32 bytes holds each rebind text. Pass the result to
    /// <see cref="Label.SetText(ReadOnlySpan{byte})"/>. Read <see cref="UiActionMap.IsRebinding"/> after
    /// <see cref="UiContext.Update(float)"/>, and read the done flag, the status, and the code that the result slot
    /// stores.
    /// </para>
    /// </remarks>
    /// <param name="listening">The value of <see cref="UiActionMap.IsRebinding"/> of the active map.</param>
    /// <param name="done">A value that tells if the result slot has stored one result.</param>
    /// <param name="status">The status of the last result, or <c>0</c> before the first result.</param>
    /// <param name="code">The code of the last result, or <c>0</c> before the first result.</param>
    /// <param name="scratch">The destination buffer. Give it 32 bytes.</param>
    /// <returns>The part of <paramref name="scratch"/> that holds the text.</returns>
    /// <exception cref="InvalidOperationException">The text does not fit in <paramref name="scratch"/>.</exception>
    public static ReadOnlySpan<byte> RebindText(bool listening, bool done, int status, int code, Span<byte> scratch)
    {
        // The write pattern of SchemeText: one copy or format for each part, and no allocation.
        if (!RebindPrefix.TryCopyTo(scratch))
        {
            throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
        }

        int length = RebindPrefix.Length;
        if (listening)
        {
            if (!RebindListeningText.TryCopyTo(scratch[length..]))
            {
                throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
            }

            return scratch[..(length + RebindListeningText.Length)];
        }

        if (!done)
        {
            if (!RebindIdleText.TryCopyTo(scratch[length..]))
            {
                throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
            }

            return scratch[..(length + RebindIdleText.Length)];
        }

        if (status == UiActionMap.RebindCancelled)
        {
            if (!RebindCancelledText.TryCopyTo(scratch[length..]))
            {
                throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
            }

            return scratch[..(length + RebindCancelledText.Length)];
        }

        if (status == UiActionMap.RebindRejected)
        {
            if (!RebindRejectedText.TryCopyTo(scratch[length..]))
            {
                throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
            }

            length += RebindRejectedText.Length;
            if (!code.TryFormat(scratch[length..], out int written, default, CultureInfo.InvariantCulture))
            {
                throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
            }

            return scratch[..(length + written)];
        }

        if (status == UiActionMap.RebindBound)
        {
            if (!RebindCodeText.TryCopyTo(scratch[length..]))
            {
                throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
            }

            length += RebindCodeText.Length;
            if (!code.TryFormat(scratch[length..], out int formatted, default, CultureInfo.InvariantCulture))
            {
                throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
            }

            return scratch[..(length + formatted)];
        }

        if (!RebindOffText.TryCopyTo(scratch[length..]))
        {
            throw new InvalidOperationException("The rebind text does not fit in the scratch buffer.");
        }

        return scratch[..(length + RebindOffText.Length)];
    }

    /// <summary>The prefix of the rebind text.</summary>
    private static ReadOnlySpan<byte> RebindPrefix => "Rebind: "u8;

    /// <summary>The rebind text while the listen is open.</summary>
    private static ReadOnlySpan<byte> RebindListeningText => "press a key"u8;

    /// <summary>The rebind text before the first result.</summary>
    private static ReadOnlySpan<byte> RebindIdleText => "idle"u8;

    /// <summary>The text before the code of the bound rebind text.</summary>
    private static ReadOnlySpan<byte> RebindCodeText => "code "u8;

    /// <summary>The rebind text of a cancelled listen.</summary>
    private static ReadOnlySpan<byte> RebindCancelledText => "cancelled"u8;

    /// <summary>The text before the code of the rejected rebind text.</summary>
    private static ReadOnlySpan<byte> RebindRejectedText => "rejected "u8;

    /// <summary>The rebind text of a status outside the three result consts.</summary>
    private static ReadOnlySpan<byte> RebindOffText => "listening off"u8;

    /// <summary>
    /// The longest rebind text: the prefix, the rejected text, and the biggest key code 348. The text is 20 bytes.
    /// The rebind label starts with it, so its buffer never grows after the first frame.
    /// </summary>
    private static ReadOnlySpan<byte> LongestRebindText => "Rebind: rejected 348"u8;
}
