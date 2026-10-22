using System;
using System.IO;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace HelloPanel;

/// <summary>
/// Builds the screen of the sample: a root, a panel, two labels, and one image. Also fills the pixels of the checker
/// texture of the image, and gives the text of the gate label.
/// </summary>
/// <remarks>
/// <para>
/// The root fills the viewport and puts the panel at its top-left corner, inside the root padding. The panel is a
/// column with a fill, a border, and round corners. The panel takes the size of its content.
/// </para>
/// <para>
/// The screen reacts to the pointer and to Tab. The panel is the only focusable element, so Tab gives the focus to
/// the panel. The two labels and the image are not pickable, so each point inside the panel hits the panel. The root
/// is not pickable (the layout-root rule): a point outside the panel hits no element, and the game gets the pointer.
/// </para>
/// <para>
/// Two state rules of the sheet show the states. The <c>.hello-panel:hover</c> rule changes the fill of the panel,
/// and the <c>.hello-panel:focus</c> rule changes its border color. The two rules set different members, so the
/// hover and the focus show at the same time.
/// </para>
/// <para>
/// The second label is the gate label. It shows the two values of <see cref="UiContext.RoutingGate"/>. The frame
/// loop sets its text with <see cref="GateText"/> after each draw. The label starts with the longest gate text, so its
/// buffer never grows after the first frame.
/// </para>
/// <para>
/// The two labels use font 0, the default font of the renderer. The sample loads no font, so font 0 is the ImGui
/// default font. The panel and the two labels use the font atlas texture, so they share one ImGui draw command. The
/// gate label comes before the image. The image binds the checker texture, so it opens a second draw command.
/// </para>
/// <para>
/// The sizes and the colors of the screen live in <c>HelloScreen.css</c>. <see cref="LoadSheet"/> parses the file,
/// and <see cref="Build"/> sets the classes of the sheet. The tree holds no inline value. The rules
/// <c>.hello-panel &gt; Label</c> and <c>.hello-panel &gt; Image</c> are child rules, so the labels and the image
/// must stay direct children of the panel.
/// </para>
/// <para>
/// Copy this file and <c>HelloScreen.css</c> to start a screen of your game. Change a size or a color in the sheet,
/// not in the code.
/// </para>
/// </remarks>
internal static class HelloScreen
{
    /// <summary>The file name of the sheet. The build copies the file next to the sample assembly.</summary>
    private const string SheetFileName = "HelloScreen.css";

    /// <summary>The text of the label.</summary>
    private const string GreetingText = "Hello, brutal-ui";

    /// <summary>The class of the root: the class of the <c>.hello-root</c> rule of the sheet.</summary>
    private static readonly ClassId RootClass = ClassName.Intern("hello-root");

    /// <summary>The class of the panel: the class of the <c>.hello-panel</c> rules of the sheet.</summary>
    private static readonly ClassId PanelClass = ClassName.Intern("hello-panel");

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

    /// <summary>Builds the tree of the screen.</summary>
    /// <remarks>
    /// <para>
    /// The root has an auto size, so it takes the viewport size of its <see cref="UiContext"/>. Add the result to a
    /// context with <see cref="UiContext.AddRoot(Element)"/>, and add the sheet of <see cref="LoadSheet"/> to the same
    /// context. The tree sets the classes of the sheet and no inline value. The context must have a measurer, because
    /// the tree holds two <see cref="Label"/> elements. The image takes the size of <paramref name="checker"/>, one
    /// pixel for each texel.
    /// </para>
    /// <para>
    /// The root is not pickable, and the panel is focusable. The two labels and the image are not pickable, so a
    /// point inside the panel hits the panel. The gate label starts with the longest text of <see cref="GateText"/>.
    /// </para>
    /// </remarks>
    /// <param name="checker">
    /// The texture of the image. The none handle is valid: the image then takes no space and draws nothing.
    /// </param>
    /// <param name="panel">
    /// The panel. The frame loop of the sample reads its rectangle to aim the synthetic pointer move of the smoke run.
    /// </param>
    /// <param name="gateLabel">The gate label. Set its text with <see cref="GateText"/>.</param>
    /// <returns>The root element of the screen.</returns>
    public static Element Build(TextureHandle checker, out Element panel, out Label gateLabel)
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

        // The child rules read the parent, so the labels and the image are direct children of the panel.
        // The gate label comes before the image, so the panel and the two labels share one draw command.
        // The panel is the only focusable element, so Tab gives the focus to the panel.
        panel = new Element { IsFocusable = true };
        panel.Classes.Add(PanelClass);
        panel.Children.Add(greeting);
        panel.Children.Add(gateLabel);
        panel.Children.Add(checkerImage);

        // The root draws nothing. The rule .hello-root stops the stretch, so the panel keeps the size of its content.
        // The root covers the viewport. It is not pickable (the layout-root rule), so a point outside the panel hits
        // no element, and the gate gives the pointer to the game.
        Element root = new() { IsPickable = false };
        root.Classes.Add(RootClass);
        root.Children.Add(panel);

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
