using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The helpers of the layout tests. The fixture tests use them in place of the Yoga test calls.
/// </summary>
internal static class LayoutFixture
{
    /// <summary>The tolerance of each compare of <see cref="AssertRect"/>, in pixels.</summary>
    public const float Tolerance = 0.001f;

    /// <summary>
    /// Lays out the tree below <paramref name="root"/> with no size limit.
    /// </summary>
    /// <remarks>
    /// The method adds the root to a new <see cref="UiContext"/> with a viewport size of (<c>NaN</c>, <c>NaN</c>),
    /// calls <see cref="UiContext.Update(float)"/>, and removes the root again.
    /// This matches <c>YGNodeCalculateLayout(root, YGUndefined, YGUndefined, YGDirectionLTR)</c>.
    /// </remarks>
    /// <param name="root">The root of the tree. It must not be a root of a context.</param>
    public static void Calculate(Element root)
    {
        UiContext context = new()
        {
            ViewportSize = new float2(float.NaN, float.NaN),
        };

        context.AddRoot(root);
        context.Update(0f);
        context.RemoveRoot(root);
    }

    /// <summary>
    /// Checks the rect of the last layout pass of <paramref name="element"/>.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <param name="left">The expected X, relative to the parent. Yoga: <c>YGNodeLayoutGetLeft</c>.</param>
    /// <param name="top">The expected Y, relative to the parent. Yoga: <c>YGNodeLayoutGetTop</c>.</param>
    /// <param name="width">The expected width. Yoga: <c>YGNodeLayoutGetWidth</c>.</param>
    /// <param name="height">The expected height. Yoga: <c>YGNodeLayoutGetHeight</c>.</param>
    public static void AssertRect(Element element, float left, float top, float width, float height)
    {
        float4 rect = element.Layout.Rect;
        string name = element.Name ?? "(no name)";

        AssertValue(name, "left", left, rect.X);
        AssertValue(name, "top", top, rect.Y);
        AssertValue(name, "width", width, rect.Z);
        AssertValue(name, "height", height, rect.W);
    }

    /// <summary>
    /// Checks that no element of the tree below <paramref name="root"/> holds a layout bit.
    /// </summary>
    /// <remarks>The check includes the root, and it reads the self bit and the descendant bit.</remarks>
    /// <param name="root">The root of the tree.</param>
    public static void AssertNoLayoutBits(Element root)
    {
        DirtyFlags layoutBits = root.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout);
        Assert.True(
            layoutBits == DirtyFlags.None,
            $"The element '{root.Name ?? "(no name)"}' holds the layout bits {layoutBits}.");

        foreach (Element child in root.Children)
        {
            AssertNoLayoutBits(child);
        }
    }

    /// <summary>Compares one value with the tolerance, and names the element and the value on a failure.</summary>
    /// <param name="name">The name of the element.</param>
    /// <param name="what">The name of the value.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    private static void AssertValue(string name, string what, float expected, float actual)
    {
        Assert.True(
            MathF.Abs(expected - actual) <= Tolerance,
            $"The {what} of '{name}' is {actual}. The expected value is {expected}.");
    }
}
