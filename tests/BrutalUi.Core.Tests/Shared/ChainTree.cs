using System;
using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>The shape of a <see cref="ChainTree"/>.</summary>
internal enum ChainShape
{
    /// <summary>Each level alternates <c>Row</c> and <c>Column</c> and has a percent padding of 1 on each edge.</summary>
    PercentPadding,

    /// <summary>Each level alternates <c>Row</c> and <c>Column</c> and has a percent maximum width of 99.</summary>
    PercentMaxWidth,

    /// <summary>Each level keeps the default style (a column). This is the chain of the stack probe (CR-10).</summary>
    PlainColumn,
}

/// <summary>
/// A chain of nested elements with a text leaf at the end.
/// </summary>
/// <remarks>
/// <para>
/// The chain holds a root and <c>depth</c> levels below it. The last level is a text leaf with a
/// <see cref="TextMeasure"/> of content width 60 and line height 16. <see cref="Build"/> adds the root to a new
/// <see cref="UiContext"/> with a viewport of 800 by 600. It runs no <see cref="UiContext.Update(float)"/>. Each
/// name is a string literal, so the build creates no name string.
/// </para>
/// <para>
/// The two percent shapes follow probe P2 and the CR-1 test of the flexbox layout. A mark on the leaf dirties each
/// level, and the leaf gets many different constraints, so the callback count grows with the depth.
/// </para>
/// <para>
/// The benchmark project links this file, so the file uses no test framework type and no internal member of
/// <c>BrutalUi.Core</c>.
/// </para>
/// </remarks>
internal sealed class ChainTree
{
    /// <summary>The number of levels below the root that the budget tests and the benchmark use.</summary>
    public const int Depth = 20;

    /// <summary>The budget of callback calls for one leaf mark in the <see cref="ChainShape.PercentPadding"/> chain at <see cref="Depth"/>. The review gave 3031 calls (Assumption A8).</summary>
    public const int PaddingCallBudget = 4000;

    /// <summary>The budget of callback calls for one leaf mark in the <see cref="ChainShape.PercentMaxWidth"/> chain at <see cref="Depth"/>. The review gave 5674 calls (Assumption A8).</summary>
    public const int MaxWidthCallBudget = 7400;

    /// <summary>The width of the viewport, in pixels.</summary>
    public const float ViewportWidth = 800f;

    /// <summary>The height of the viewport, in pixels.</summary>
    public const float ViewportHeight = 600f;

    /// <summary>The content width of the text leaf, in pixels.</summary>
    public const float TextWidth = 60f;

    /// <summary>The line height of the text leaf, in pixels.</summary>
    public const float TextLineHeight = 16f;

    /// <summary>The context that holds the root.</summary>
    public required UiContext Context { get; init; }

    /// <summary>The root of the chain.</summary>
    public required Element Root { get; init; }

    /// <summary>The last level, a text leaf.</summary>
    public required Element Leaf { get; init; }

    /// <summary>The measure callback of the leaf. Its count gives the number of calls.</summary>
    public required TextMeasure Text { get; init; }

    /// <summary>Builds a chain.</summary>
    /// <param name="shape">The shape of the chain.</param>
    /// <param name="depth">The number of levels below the root. The value is 1 or more.</param>
    /// <returns>The chain. It has had no <c>Update</c>.</returns>
    public static ChainTree Build(ChainShape shape, int depth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);

        Element root = new() { Name = "root" };
        Element current = root;
        for (int i = 0; i < depth; i++)
        {
            Element level = new() { Name = "level" };
            current.Children.Add(level);

            if (shape != ChainShape.PlainColumn)
            {
                level.Style.FlexDirection = (i % 2 == 0) ? FlexDirection.Row : FlexDirection.Column;

                if (shape == ChainShape.PercentPadding)
                {
                    level.Style.Padding = LengthEdges.All(Length.Percent(1));
                }
                else
                {
                    level.Style.MaxWidth = Length.Percent(99);
                }
            }

            current = level;
        }

        TextMeasure text = new(TextWidth, TextLineHeight);
        current.Layout.Measure = text.Callback;

        UiContext context = new();
        context.ViewportSize = new float2(ViewportWidth, ViewportHeight);
        context.AddRoot(root);

        return new ChainTree { Context = context, Root = root, Leaf = current, Text = text };
    }
}
