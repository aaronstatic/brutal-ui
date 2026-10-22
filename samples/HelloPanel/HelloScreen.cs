using System;
using System.IO;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace HelloPanel;

/// <summary>
/// Builds the static screen of the sample: a root, a panel, one label, and one image. Also fills the pixels of the
/// checker texture of the image.
/// </summary>
/// <remarks>
/// <para>
/// The root fills the viewport and puts the panel at its top-left corner, inside the root padding. The panel is a
/// column with a fill, a border, and round corners. The panel takes the size of its content.
/// </para>
/// <para>
/// The label uses font 0, the default font of the renderer. The sample loads no font, so font 0 is the ImGui default
/// font. The panel and the label use the font atlas texture, so they share one ImGui draw command. The image binds the
/// checker texture, so it opens a second draw command.
/// </para>
/// <para>
/// The sizes and the colors of the screen live in <c>HelloScreen.css</c>. <see cref="LoadSheet"/> parses the file,
/// and <see cref="Build"/> sets the classes of the sheet. The tree holds no inline value. The rules
/// <c>.hello-panel &gt; Label</c> and <c>.hello-panel &gt; Image</c> are child rules, so the label and the image must
/// stay direct children of the panel.
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
    /// The root has an auto size, so it takes the viewport size of its <see cref="UiContext"/>. Add the result to a
    /// context with <see cref="UiContext.AddRoot(Element)"/>, and add the sheet of <see cref="LoadSheet"/> to the same
    /// context. The tree sets the classes of the sheet and no inline value. The context must have a measurer, because
    /// the tree holds a <see cref="Label"/>. The image takes the size of <paramref name="checker"/>, one pixel for each
    /// texel.
    /// </remarks>
    /// <param name="checker">
    /// The texture of the image. The none handle is valid: the image then takes no space and draws nothing.
    /// </param>
    /// <returns>The root element of the screen.</returns>
    public static Element Build(TextureHandle checker)
    {
        // The rule .hello-panel > Label gives the color and the size. The label keeps font 0, the default font.
        Label greeting = new(new Label.CreateInfo { Text = GreetingText });

        // The rule .hello-panel > Image gives the top margin. The tint is a value of the image, not of the style.
        Image checkerImage = new(new Image.CreateInfo
        {
            Texture = checker,
            Tint = ImageTint,
            ScaleMode = ImageScaleMode.StretchToFill,
        });

        // The two child rules read the parent, so the label and the image are direct children of the panel.
        Element panel = new();
        panel.Classes.Add(PanelClass);
        panel.Children.Add(greeting);
        panel.Children.Add(checkerImage);

        // The root draws nothing. The rule .hello-root stops the stretch, so the panel keeps the size of its content.
        Element root = new();
        root.Classes.Add(RootClass);
        root.Children.Add(panel);

        return root;
    }
}
