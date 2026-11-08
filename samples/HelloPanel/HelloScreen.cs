using System;
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
/// <c>Mode (F1)</c> button of the mode row. The <c>Add</c> button of the state row takes the focus too. The disabled
/// button and the scroll view take no focus. In <c>hello.main</c>, Tab walks the focusable elements in tree order:
/// the panel, <c>Click me</c>, the toggle, the slider, the text field, <c>Mode (F1)</c>, and <c>Add</c>. A press on
/// one of these widgets gives the focus to the widget. A press on the disabled button clears the focus: the button is
/// focusable but disabled, and the press does not go on to the panel. A press on the scroll view gives the focus to
/// the panel, the nearest focusable ancestor, because the scroll view is not focusable. The two labels and the image
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
/// <c>Add</c> button and a score label. <see cref="BuildStatePanel"/> puts that row in the state panel, so the row is
/// no longer a child of the widget panel.
/// </para>
/// <para>
/// The second label is the gate label. It shows the two values of <see cref="UiContext.RoutingGate"/>. The frame
/// loop sets its text with <see cref="GateText"/> after each draw. The label starts with the longest gate text, so its
/// buffer never grows after the first frame.
/// </para>
/// <para>
/// The labels and the widgets use font 0, the default font of the renderer. The sample loads no font, so font 0 is
/// the ImGui default font. The panel and the two labels use the font atlas texture, so they share one ImGui draw
/// command. The gate label comes before the image. The image binds the checker texture, so it opens a second draw
/// command. The widget section comes after the image and uses the font atlas texture again, so it opens a third draw
/// command. The mode row follows the widget section, and its button keeps the same command.
/// </para>
/// <para>
/// The sizes and the colors of the screen live in <c>HelloScreen.css</c>. <see cref="LoadSheet"/> parses the file,
/// and the build methods set the classes of the sheet. The class names of the sheet are <c>hello-slot-root</c>,
/// <c>hello-panel</c>, <c>hello-panel-root</c>, <c>hello-widgets</c>, <c>hello-row</c>, and <c>hello-status</c>. The
/// sheet also holds the type rule <c>SlotContainer</c> for the two slots. The widget rules use the
/// type names of the widgets, for example <c>Button</c>. The tree holds no inline value. The rules
/// <c>.hello-panel &gt; Label</c> and <c>.hello-panel &gt; Image</c> are child rules, so the two labels and the image
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

    /// <summary>Builds the widget panel: the two labels, the image, the widget section, and the mode row.</summary>
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
    /// <paramref name="modeButton"/>. The disabled button and the scroll view take no focus. The two labels and the
    /// image are not pickable, so a point on them hits the panel. The gate label starts with the longest text of
    /// <see cref="GateText"/>.
    /// </para>
    /// <para>
    /// The two labels and the image are direct children of the panel, because the rules
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
    /// <param name="widgets">
    /// The widget section, its widgets, its status labels, and its change counts. The smoke run aims its widget events
    /// at the widgets and reads the counts.
    /// </param>
    /// <returns>The root element of the widget panel.</returns>
    public static Element BuildWidgetPanel(
        TextureHandle checker, Button modeButton, out Label gateLabel, out HelloWidgets widgets)
    {
        // The rule .hello-panel > Label gives the color and the size. The label keeps font 0, the default font.
        // A label is not pickable, so the pointer hits the panel behind it.
        Label greeting = new(new Label.CreateInfo { Text = GreetingText }) { IsPickable = false };

        // The gate label starts with the longest gate text, so its buffer holds each gate text with no growth.
        gateLabel = new Label(new Label.CreateInfo()) { IsPickable = false };
        gateLabel.SetText(PointerYesFocusYes);

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
        // The gate label comes before the image, so the panel and the two labels share one draw command.
        // The panel is focusable. So are Click me, the toggle, the slider, the text field, and Mode (F1). The
        // disabled button and the scroll view take no focus.
        Element panel = new() { IsFocusable = true };
        panel.Classes.Add(PanelClass);
        panel.Classes.Add(PanelRootClass);
        panel.Children.Add(greeting);
        panel.Children.Add(gateLabel);
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
}
