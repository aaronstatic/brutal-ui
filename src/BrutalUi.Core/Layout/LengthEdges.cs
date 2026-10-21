namespace BrutalUi.Core;

/// <summary>
/// Four lengths, one for each edge of a box: left, top, right, and bottom.
/// </summary>
/// <remarks>
/// <para>
/// <c>default(LengthEdges)</c> holds four <see cref="Length.Undefined"/> values. The layout reads an undefined edge as the
/// default of the property: 0 for a margin, a padding, and a border.
/// </para>
/// <para>
/// The edges are physical. There is no start edge and no end edge, because the layout has no text direction.
/// The fields are public, so a write to a field of a local variable compiles:
/// <c>LengthEdges m = el.Inline.Margin; m.Top = Length.Px(12); el.Inline.Margin = m;</c>.
/// A nested write through a property, such as <c>el.Inline.Margin.Top = 12;</c>, gives a copy and error CS1612.
/// Write the whole value through <c>Element.Inline</c>, or use a <c>with</c> expression.
/// </para>
/// </remarks>
public struct LengthEdges
{
    /// <summary>The length of the left edge. The default is <see cref="Length.Undefined"/>.</summary>
    public Length Left;

    /// <summary>The length of the top edge. The default is <see cref="Length.Undefined"/>.</summary>
    public Length Top;

    /// <summary>The length of the right edge. The default is <see cref="Length.Undefined"/>.</summary>
    public Length Right;

    /// <summary>The length of the bottom edge. The default is <see cref="Length.Undefined"/>.</summary>
    public Length Bottom;

    /// <summary>
    /// Creates edges from four lengths.
    /// </summary>
    /// <param name="left">The length of the left edge.</param>
    /// <param name="top">The length of the top edge.</param>
    /// <param name="right">The length of the right edge.</param>
    /// <param name="bottom">The length of the bottom edge.</param>
    public LengthEdges(Length left, Length top, Length right, Length bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Creates edges that hold the same length on all four edges.
    /// </summary>
    /// <param name="value">The length for each edge.</param>
    /// <returns>The edges.</returns>
    public static LengthEdges All(Length value)
    {
        return new LengthEdges(value, value, value, value);
    }
}
