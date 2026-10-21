using System;
using Brutal.Numerics;
using BrutalUi.Core;

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
/// Copy this file to start a screen of your game. Each size and each color is a named constant.
/// </para>
/// </remarks>
internal static class HelloScreen
{
    /// <summary>The text of the label.</summary>
    private const string GreetingText = "Hello, brutal-ui";

    /// <summary>The space between the edge of the viewport and the panel, in pixels.</summary>
    private const float RootPadding = 24f;

    /// <summary>The space between the border of the panel and its content, in pixels.</summary>
    private const float PanelPadding = 16f;

    /// <summary>The width of the panel border, in pixels.</summary>
    private const float PanelBorderWidth = 2f;

    /// <summary>The radius of each corner of the panel, in pixels.</summary>
    private const float PanelCornerRadius = 8f;

    /// <summary>The font size of the label, in pixels.</summary>
    private const float GreetingFontSize = 26f;

    /// <summary>The fill color of the panel: an opaque dark blue-gray.</summary>
    private static readonly Rgba8 PanelColor = new(30, 34, 44, 255);

    /// <summary>The border color of the panel: an opaque light blue.</summary>
    private static readonly Rgba8 BorderColor = new(96, 140, 220, 255);

    /// <summary>The color of the label text: opaque white.</summary>
    private static readonly Rgba8 GreetingColor = new(255, 255, 255, 255);

    /// <summary>The space between the label and the image, in pixels.</summary>
    private const float ImageTopMargin = 12f;

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

    /// <summary>Builds the tree of the screen.</summary>
    /// <remarks>
    /// The root has an auto size, so it takes the viewport size of its <see cref="UiContext"/>. Add the result to a
    /// context with <see cref="UiContext.AddRoot(Element)"/>. The context must have a measurer, because the tree holds a
    /// <see cref="Label"/>. The image takes the size of <paramref name="checker"/>, one pixel for each texel.
    /// </remarks>
    /// <param name="checker">
    /// The texture of the image. The none handle is valid: the image then takes no space and draws nothing.
    /// </param>
    /// <returns>The root element of the screen.</returns>
    public static Element Build(TextureHandle checker)
    {
        Label greeting = new(new Label.CreateInfo { Text = GreetingText });
        // The label keeps font 0, the default font of the renderer. The inline set gives the color and the size.
        greeting.Inline.Color = GreetingColor;
        greeting.Inline.FontSize = GreetingFontSize;

        Image checkerImage = new(new Image.CreateInfo
        {
            Texture = checker,
            Tint = ImageTint,
            ScaleMode = ImageScaleMode.StretchToFill,
        });
        // The view gives a copy of the edges, so the write changes the top edge of a whole value.
        checkerImage.Inline.Margin = checkerImage.Inline.Margin with { Top = ImageTopMargin };

        Element panel = new();
        panel.Inline.FlexDirection = FlexDirection.Column;
        panel.Inline.AlignItems = Align.FlexStart;
        panel.Inline.Padding = LengthEdges.All(PanelPadding);
        panel.Inline.BorderWidth = LengthEdges.All(PanelBorderWidth);
        panel.Inline.CornerRadius = Corners.All(PanelCornerRadius);
        panel.Inline.BackgroundColor = PanelColor;
        panel.Inline.BorderColor = BorderColor;
        panel.Children.Add(greeting);
        panel.Children.Add(checkerImage);

        // The root draws nothing. FlexStart stops the stretch, so the panel keeps the size of its content.
        Element root = new();
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Padding = LengthEdges.All(RootPadding);
        root.Children.Add(panel);

        return root;
    }
}
