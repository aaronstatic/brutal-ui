namespace BrutalUi.Core;

/// <summary>
/// The rule that puts the texture of an <see cref="Image"/> into the box of the image.
/// </summary>
/// <remarks>
/// The aspect of the image is the width of its natural size divided by the height. A natural size with a component of
/// 0, or a box with no area, uses <see cref="StretchToFill"/>.
/// </remarks>
public enum ImageScaleMode
{
    /// <summary>
    /// The image fills the box. The aspect of the image can change. The draw uses the full UV rect. This value is the
    /// default.
    /// </summary>
    StretchToFill,

    /// <summary>
    /// The image takes the largest rect with its own aspect that fits in the box. The rect is in the center of the box.
    /// The draw uses the full UV rect, so the image shows completely.
    /// </summary>
    ScaleToFit,

    /// <summary>
    /// The image fills the box and keeps its aspect. The draw cuts the UV rect around its center to the aspect of the
    /// box, so the image does not show completely.
    /// </summary>
    ScaleAndCrop,
}
