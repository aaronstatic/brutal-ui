namespace BrutalUi.Core;

/// <summary>
/// The layout data of an element. The struct lives inline in the element and holds no field yet.
/// </summary>
/// <remarks>
/// The core-tree/flexbox-layout feature adds the fields, such as the computed rectangle.
/// Code writes the fields through the <c>ref</c> that <see cref="Element.Layout"/> returns.
/// The struct is not <c>readonly</c>, because that feature adds mutable fields.
/// </remarks>
public struct LayoutNode
{
}
