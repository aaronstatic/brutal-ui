using System;

namespace BrutalUi.Core;

/// <summary>
/// The dirty bits of an element. Each kind has a self bit and a descendant bit.
/// </summary>
/// <remarks>
/// A self bit tells that the element itself needs the pass of that kind.
/// A descendant bit tells that an element below it has the self bit of that kind.
/// The descendant bit of a kind is the self bit of the kind shifted left by 3.
/// When an element has a bit of a kind, each ancestor has the descendant bit of that kind.
/// The update walk uses the descendant bits to skip clean subtrees.
/// </remarks>
[Flags]
public enum DirtyFlags
{
    /// <summary>No bit is set. The element and its subtree are clean.</summary>
    None = 0,

    /// <summary>The style of the element needs a new cascade.</summary>
    Style = 1,

    /// <summary>The layout of the element needs a new calculation.</summary>
    Layout = 2,

    /// <summary>The element needs a new paint.</summary>
    Paint = 4,

    /// <summary>An element below this element has the <see cref="Style"/> bit.</summary>
    DescendantStyle = 8,

    /// <summary>An element below this element has the <see cref="Layout"/> bit.</summary>
    DescendantLayout = 16,

    /// <summary>An element below this element has the <see cref="Paint"/> bit.</summary>
    DescendantPaint = 32,
}

/// <summary>
/// The masks and the bit conversion for <see cref="DirtyFlags"/>.
/// </summary>
internal static class DirtyBits
{
    /// <summary>The self bits of all kinds.</summary>
    public const DirtyFlags SelfMask = DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint;

    /// <summary>The descendant bits of all kinds.</summary>
    public const DirtyFlags DescendantMask =
        DirtyFlags.DescendantStyle | DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint;

    /// <summary>
    /// Converts self bits to the descendant bits of the same kinds.
    /// </summary>
    /// <param name="self">The self bits. The method ignores each bit outside <see cref="SelfMask"/>.</param>
    /// <returns>The descendant bits of the kinds in <paramref name="self"/>.</returns>
    public static DirtyFlags ToDescendant(DirtyFlags self)
    {
        return (DirtyFlags)((int)(self & SelfMask) << 3);
    }
}
