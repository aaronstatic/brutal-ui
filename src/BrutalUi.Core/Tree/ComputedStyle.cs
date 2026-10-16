namespace BrutalUi.Core;

/// <summary>
/// The computed style of an element. The struct lives inline in the element and holds no field yet.
/// </summary>
/// <remarks>
/// The core-tree/flexbox-layout feature adds the layout fields.
/// The style-system epic adds the paint fields.
/// Code writes the fields through the <c>ref</c> that <see cref="Element.Style"/> returns.
/// The struct is not <c>readonly</c>, because those features add mutable fields.
/// </remarks>
public struct ComputedStyle
{
}
