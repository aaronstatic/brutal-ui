namespace BrutalUi.Core;

/// <summary>
/// The link between the ancestor compound and the subject compound of a <see cref="Selector"/>.
/// </summary>
/// <remarks>
/// A chain reads the tree: the type, the classes, the name, and the pseudo-states of the ancestor. It does not read the computed
/// style of the ancestor. Thus the order of the style pass does not change the result.
/// </remarks>
public enum SelectorCombinator : byte
{
    /// <summary>
    /// The selector holds one compound, as <c>.b</c> is in CSS. <see cref="Selector.Ancestor"/> holds no part.
    /// </summary>
    None = 0,

    /// <summary>
    /// The child chain, as <c>.a &gt; .b</c> is in CSS. The parent of the element must match the ancestor compound.
    /// </summary>
    Child = 1,

    /// <summary>
    /// The descendant chain, as <c>.a .b</c> is in CSS. The parent or the grandparent of the element must match the
    /// ancestor compound. The walk stops at the grandparent.
    /// </summary>
    Descendant = 2,
}
