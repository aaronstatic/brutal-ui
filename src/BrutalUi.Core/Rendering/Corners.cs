namespace BrutalUi.Core;

/// <summary>
/// Four corner radii in pixels, one for each corner of a box: top-left, top-right, bottom-right, and bottom-left.
/// </summary>
/// <remarks>
/// <para>
/// <c>default(Corners)</c> holds four radii of 0. A radius of 0 gives a square corner.
/// </para>
/// <para>
/// The order of the fields goes clockwise from the top-left corner.
/// The fields are public, so a nested write such as <c>element.Style.CornerRadius.TopLeft = 4f;</c> goes to the field and compiles.
/// A property gives a copy and error CS1612.
/// </para>
/// </remarks>
public struct Corners
{
    /// <summary>The radius of the top-left corner in pixels. The default is 0.</summary>
    public float TopLeft;

    /// <summary>The radius of the top-right corner in pixels. The default is 0.</summary>
    public float TopRight;

    /// <summary>The radius of the bottom-right corner in pixels. The default is 0.</summary>
    public float BottomRight;

    /// <summary>The radius of the bottom-left corner in pixels. The default is 0.</summary>
    public float BottomLeft;

    /// <summary>
    /// Creates corners from four radii.
    /// </summary>
    /// <param name="topLeft">The radius of the top-left corner in pixels.</param>
    /// <param name="topRight">The radius of the top-right corner in pixels.</param>
    /// <param name="bottomRight">The radius of the bottom-right corner in pixels.</param>
    /// <param name="bottomLeft">The radius of the bottom-left corner in pixels.</param>
    public Corners(float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    /// <summary>
    /// Creates corners that hold the same radius on all four corners.
    /// </summary>
    /// <param name="radius">The radius for each corner in pixels.</param>
    /// <returns>The corners.</returns>
    public static Corners All(float radius)
    {
        return new Corners(radius, radius, radius, radius);
    }
}
