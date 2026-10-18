using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The seeded differential test of the layout cache. It compares the incremental layout of a tree after each change
/// with a fresh layout of a copy of the tree.
/// </summary>
/// <remarks>
/// <para>
/// A fresh copy is a new tree of new <see cref="Element"/> objects with the same <see cref="Element.Style"/>, the same
/// <see cref="LayoutNode.Measure"/>, and the same child order. A new <see cref="UiContext"/> with the same
/// <see cref="UiContext.ViewportSize"/> lays it out. Its elements have an empty cache, so the fresh rects are the
/// reference. A difference is a stale cache entry or a stale dirty bit.
/// </para>
/// <para>
/// The measure functions of the random leaves keep the callback contract of the cache: a fixed size, or a text that
/// gives its natural size when it fits. <see cref="MeasureStub.AvailableWidthBy20"/> breaks the contract and is not
/// in the set.
/// </para>
/// <para>
/// The tree index selects a <see cref="TreeKind"/>, so each kind gets the same share of the trees. Each kind other
/// than <see cref="TreeKind.Mixed"/> builds the case of one cache rule often, so that 200 trees find a fault in each
/// rule. A mutation check in a scratch copy confirms it (see the session notes of phase 9 group 3).
/// </para>
/// <para>
/// The change steps also move laid-out subtrees with a warm cache: a reparent of a subtree to an element outside the
/// subtree, a promote of a child to a new root, and a demote of a root into the tree of another root. A context can
/// thus hold up to <see cref="MaxRoots"/> roots, and each step compares each root with a fresh layout. A promote
/// can hide the new root. A root ignores <see cref="Display.None"/>, so a later demote moves a hidden element with a
/// visible layout into a tree (see CR-2 of the review).
/// </para>
/// </remarks>
public sealed class LayoutDifferentialTests
{
    /// <summary>The seed of the test. The test method name holds the same value.</summary>
    private const int Seed = 20261017;

    /// <summary>The number of random trees.</summary>
    private const int TreeCount = 200;

    /// <summary>The number of random changes on each tree.</summary>
    private const int ChangeCount = 20;

    /// <summary>The number of values of <see cref="TreeKind"/>.</summary>
    private const int KindCount = 5;

    /// <summary>The largest number of roots in the context of a tree. A promote adds a root, and a demote removes one.</summary>
    private const int MaxRoots = 4;

    /// <summary>The finite viewport sizes of the test. A change step can select a new one.</summary>
    private static readonly float2[] s_viewports =
    [
        new float2(400f, 300f),
        new float2(640f, 480f),
        new float2(320f, 568f),
        new float2(250f, 250f),
        new float2(1024f, 200f),
        new float2(0f, 120f),
        new float2(333f, 217f),
        new float2(517f, 391f),
        new float2(291f, 433f),
        new float2(777f, 155f),
        new float2(123f, 321f),
    ];

    /// <summary>The measure functions of the random leaves. Each one keeps the callback contract of the cache.</summary>
    private static readonly MeasureCallback[] s_measures =
    [
        MeasureStub.Ten,
        MeasureStub.WrappingText,
        MeasureStub.ClampedText,
        MeasureStub.Size40By30,
    ];

    /// <summary>
    /// The measure functions whose result depends on the limit. <see cref="MeasureStub.ClampedText"/> gives a size up
    /// to an <see cref="MeasureMode.AtMost"/> limit on each axis, so it shows a stale limit most clearly.
    /// </summary>
    private static readonly MeasureCallback[] s_textMeasures =
    [
        MeasureStub.ClampedText,
        MeasureStub.ClampedText,
        MeasureStub.WrappingText,
    ];

    /// <summary>The cross alignments that do not stretch. A child with one of them gets an <see cref="MeasureMode.AtMost"/> cross limit.</summary>
    private static readonly Align[] s_noStretch = [Align.FlexStart, Align.Center, Align.FlexEnd];

    /// <summary>The grow and shrink factors of the random styles.</summary>
    private static readonly float[] s_factors = [0f, 0f, 0.5f, 1f, 2f];

    /// <summary>The kinds of random trees. The index of a tree modulo <see cref="KindCount"/> selects the kind.</summary>
    private enum TreeKind
    {
        /// <summary>The general random set: each enum value and each length kind of the supported fields.</summary>
        Mixed,

        /// <summary>
        /// A root with a fixed pixel size and a percent offset, padding, or margin. Half of the steps change the
        /// viewport. A root with a percent offset keeps no percent in its own size, margin, and padding, and in the
        /// styles of its children, so a viewport change gives a cache hit at the root.
        /// </summary>
        PercentRoot,

        /// <summary>
        /// Text leaves with a pixel margin on each edge, also <c>Margin.Top</c> and <c>Margin.Bottom</c>, in small
        /// fixed-size containers. The leaves get <see cref="MeasureMode.AtMost"/> limits, and many steps change the
        /// size of a container by a few pixels.
        /// </summary>
        MarginLeaves,

        /// <summary>
        /// Callback leaves with a pixel minimum or maximum, most of them text leaves with a maximum width below the
        /// natural text width, and half of them absolute. Many steps change the size, the direction, or the overflow
        /// of a container, so a clean leaf goes from no width limit to a limit.
        /// </summary>
        LimitedLeaves,

        /// <summary>
        /// Containers with a fixed height, a width of 100 percent, and two side paddings that add up to 100 percent.
        /// The inner width is 0 or a float noise above 0, and it changes with the viewport. The children and the
        /// grandchildren hold no percent, so the children keep their cache when the inner width changes by the noise
        /// only. Each child has content, so a limit of 0 and a limit above 0 give two sizes.
        /// </summary>
        NoiseInnerSize,
    }

    [Fact]
    public void IncrementalLayoutMatchesFreshLayoutSeed20261017()
    {
        Stopwatch watch = Stopwatch.StartNew();
        int comparisons = 0;
        int rootComparisons = 0;
        int[] structuralCounts = new int[3];

        for (int tree = 0; tree < TreeCount; tree++)
        {
            TreeKind kind = (TreeKind)(tree % KindCount);
            Random random = new(Seed + tree);
            int nextName = 0;
            Element root = BuildTree(random, kind, ref nextName);
            float2 viewport = s_viewports[random.Next(s_viewports.Length)];

            UiContext context = new() { ViewportSize = viewport };
            context.AddRoot(root);
            context.Update(0f);
            rootComparisons += CheckStep(context, tree, kind, "the first layout");
            comparisons++;

            List<Element> elements = [];
            for (int change = 0; change < ChangeCount; change++)
            {
                elements.Clear();
                foreach (Element each in context.Roots)
                {
                    Collect(each, elements);
                }

                string what = ApplyRandomChange(random, kind, context, elements, structuralCounts, ref nextName);

                context.Update(0f);
                rootComparisons += CheckStep(context, tree, kind, $"change {change}: {what}");
                comparisons++;
            }

            // A root keeps its root flag until RemoveRoot, so each root leaves the context.
            while (context.Roots.Length > 0)
            {
                context.RemoveRoot(context.Roots[0]);
            }
        }

        watch.Stop();
        Assert.Equal(TreeCount * (ChangeCount + 1), comparisons);
        TestContext.Current.SendDiagnosticMessage(
            $"Seed {Seed}: {TreeCount} trees, {comparisons} steps, {rootComparisons} root comparisons, "
            + $"{structuralCounts[0]} reparents, {structuralCounts[1]} promotes, {structuralCounts[2]} demotes, "
            + $"no difference, {watch.Elapsed.TotalSeconds:F2} s.");
    }

    /// <summary>
    /// Checks that the incremental layout of <paramref name="root"/> equals a fresh layout of a copy of the tree.
    /// </summary>
    /// <param name="root">The root of the tree. A context laid it out with <paramref name="viewport"/>.</param>
    /// <param name="viewport">The viewport size of that layout.</param>
    /// <param name="where">A text that names the case in the failure message.</param>
    internal static void AssertMatchesFreshLayout(Element root, float2 viewport, string where)
    {
        string? difference = FindDifferenceWithFreshLayout(root, viewport);
        Assert.True(difference is null, $"{where}: {difference}");
    }

    /// <summary>
    /// Lays out a fresh copy of the tree and compares each rect with the tolerance of <see cref="LayoutFixture"/>.
    /// </summary>
    /// <param name="root">The root of the laid-out tree.</param>
    /// <param name="viewport">The viewport size of the layout.</param>
    /// <returns><see langword="null"/> when all rects match, else a text with the count, the first element path, and the two rects.</returns>
    internal static string? FindDifferenceWithFreshLayout(Element root, float2 viewport)
    {
        Element fresh = Clone(root);
        UiContext context = new() { ViewportSize = viewport };
        context.AddRoot(fresh);
        context.Update(0f);
        context.RemoveRoot(fresh);

        string? first = null;
        int count = 0;
        Compare(root, fresh, ref first, ref count);
        return first is null ? null : $"{count} rects differ. The first is {first}";
    }

    /// <summary>Copies the tree: new elements with the same name, style, measure callback, and child order.</summary>
    /// <param name="source">The root of the tree to copy.</param>
    /// <returns>The root of the copy. It holds the start dirty bits of a new element and an empty cache.</returns>
    internal static Element Clone(Element source)
    {
        Element copy = new() { Name = source.Name };
        copy.Style = source.Style;
        copy.Layout.Measure = source.Layout.Measure;
        foreach (Element child in source.Children)
        {
            copy.Children.Add(Clone(child));
        }

        return copy;
    }

    /// <summary>Gives the path of the names from the root to the element, for example <c>n0/n4/n9</c>.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The path.</returns>
    internal static string PathOf(Element element)
    {
        string path = element.Name ?? "(no name)";
        for (Element? parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            path = (parent.Name ?? "(no name)") + "/" + path;
        }

        return path;
    }

    /// <summary>Compares the rects of two trees with the same shape, and counts each element with a difference.</summary>
    private static void Compare(Element actual, Element fresh, ref string? first, ref int count)
    {
        float4 a = actual.Layout.Rect;
        float4 f = fresh.Layout.Rect;
        if (!Near(a.X, f.X) || !Near(a.Y, f.Y) || !Near(a.Z, f.Z) || !Near(a.W, f.W))
        {
            count++;
            first ??= $"'{PathOf(actual)}': incremental ({a.X}, {a.Y}, {a.Z}, {a.W}), fresh ({f.X}, {f.Y}, {f.Z}, {f.W}).";
        }

        ReadOnlySpan<Element> actualChildren = actual.ChildStorage.AsSpan();
        ReadOnlySpan<Element> freshChildren = fresh.ChildStorage.AsSpan();
        for (int i = 0; i < actualChildren.Length; i++)
        {
            Compare(actualChildren[i], freshChildren[i], ref first, ref count);
        }
    }

    /// <summary>Compares two values with the tolerance. Two <c>NaN</c> values are equal.</summary>
    private static bool Near(float a, float b)
    {
        return (float.IsNaN(a) && float.IsNaN(b)) || MathF.Abs(a - b) <= LayoutFixture.Tolerance;
    }

    /// <summary>
    /// Checks the bits and the rects of each root of the context after one <see cref="UiContext.Update(float)"/>.
    /// </summary>
    /// <returns>The number of roots that the step compares.</returns>
    private static int CheckStep(UiContext context, int tree, TreeKind kind, string what)
    {
        float2 viewport = context.ViewportSize;
        ReadOnlySpan<Element> roots = context.Roots;
        for (int i = 0; i < roots.Length; i++)
        {
            Element root = roots[i];
            LayoutFixture.AssertNoLayoutBits(root);
            string? difference = FindDifferenceWithFreshLayout(root, viewport);
            Assert.True(
                difference is null,
                $"Seed {Seed}, tree {tree} (tree seed {Seed + tree}, kind {kind}), after {what}, viewport ({viewport.X}, {viewport.Y}), "
                + $"root {i} of {roots.Length} ('{root.Name}'): {difference}");
        }

        return roots.Length;
    }

    /// <summary>Builds a random tree of 20 to 40 elements of one kind. Some leaves get a measure function.</summary>
    private static Element BuildTree(Random random, TreeKind kind, ref int nextName)
    {
        Element root = NewElement(random, ref nextName);
        List<Element> elements = [root];
        int count = random.Next(20, 41);
        for (int i = 1; i < count; i++)
        {
            Element element = NewElement(random, ref nextName);
            elements[random.Next(elements.Count)].Children.Add(element);
            elements.Add(element);
        }

        foreach (Element element in elements)
        {
            if (element.Children.Count == 0 && random.Next(2) == 0)
            {
                element.Layout.Measure = s_measures[random.Next(s_measures.Length)];
            }
        }

        switch (kind)
        {
            case TreeKind.PercentRoot:
                ShapePercentRoot(random, root);
                break;

            case TreeKind.MarginLeaves:
                foreach (Element element in elements)
                {
                    ShapeMarginLeaves(random, element);
                }

                break;

            case TreeKind.LimitedLeaves:
                foreach (Element element in elements)
                {
                    ShapeLimitedLeaves(random, element);
                }

                break;

            case TreeKind.NoiseInnerSize:
                ShapeNoiseInnerSize(random, elements, ref nextName);
                break;
        }

        return root;
    }

    /// <summary>
    /// Gives the root a fixed pixel size and one percent offset, padding, or margin (<see cref="TreeKind.PercentRoot"/>).
    /// </summary>
    private static void ShapePercentRoot(Random random, Element root)
    {
        ref ComputedStyle style = ref root.Style;
        RemovePercents(ref style);
        style.Display = Display.Flex;
        style.Width = Length.Px(random.Next(40, 301));
        style.Height = Length.Px(random.Next(40, 301));
        style.Left = Length.Undefined;
        style.Top = Length.Undefined;
        style.Right = Length.Undefined;
        style.Bottom = Length.Undefined;

        switch (random.Next(3))
        {
            case 0:
                // A percent offset is not a percent that the owner size test reads. With no other percent on the root
                // and its children, a new viewport gives a cache hit at the root, and only the root position changes.
                if (random.Next(2) == 0)
                {
                    style.Left = Length.Percent(random.Next(5, 51));
                }
                else
                {
                    style.Right = Length.Percent(random.Next(5, 51));
                }

                if (random.Next(2) == 0)
                {
                    style.Top = Length.Percent(random.Next(5, 51));
                }
                else
                {
                    style.Bottom = Length.Percent(random.Next(5, 51));
                }

                foreach (Element child in root.Children)
                {
                    RemovePercents(ref child.Style);
                }

                break;

            case 1:
                style.Padding.Left = Length.Percent(random.Next(1, 31));
                style.Padding.Top = Length.Percent(random.Next(1, 31));
                break;

            default:
                style.Margin.Left = Length.Percent(random.Next(1, 31));
                style.Margin.Top = Length.Percent(random.Next(1, 31));
                break;
        }
    }

    /// <summary>
    /// Gives most leaves a text callback with a pixel margin on each edge, and gives most containers a small pixel
    /// size (<see cref="TreeKind.MarginLeaves"/>).
    /// </summary>
    private static void ShapeMarginLeaves(Random random, Element element)
    {
        ref ComputedStyle style = ref element.Style;
        if (element.Children.Count > 0)
        {
            style.Width = random.Next(3) == 0 ? Length.Undefined : Length.Px(random.Next(20, 121));
            style.Height = random.Next(3) == 0 ? Length.Undefined : Length.Px(random.Next(20, 121));
            if (random.Next(2) == 0)
            {
                style.AlignItems = s_noStretch[random.Next(s_noStretch.Length)];
            }

            return;
        }

        if (random.Next(4) != 0)
        {
            element.Layout.Measure = s_textMeasures[random.Next(s_textMeasures.Length)];
        }

        if (element.Layout.Measure is null)
        {
            return;
        }

        // The relaxed rules of the cache apply to a callback leaf with no minimum and no maximum, and with a size
        // that the callback gives.
        style.Display = Display.Flex;
        style.Width = Length.Undefined;
        style.Height = Length.Undefined;
        style.FlexBasis = Length.Undefined;
        style.MinWidth = Length.Undefined;
        style.MinHeight = Length.Undefined;
        style.MaxWidth = Length.Undefined;
        style.MaxHeight = Length.Undefined;
        style.Margin = new LengthEdges(
            Length.Px(random.Next(1, 13)),
            Length.Px(random.Next(1, 13)),
            Length.Px(random.Next(1, 13)),
            Length.Px(random.Next(1, 13)));
        style.AlignSelf = random.Next(2) == 0 ? s_noStretch[random.Next(s_noStretch.Length)] : Align.Auto;
    }

    /// <summary>
    /// Gives most leaves a callback with a pixel minimum or maximum, and gives some containers a pixel size
    /// (<see cref="TreeKind.LimitedLeaves"/>).
    /// </summary>
    private static void ShapeLimitedLeaves(Random random, Element element)
    {
        ref ComputedStyle style = ref element.Style;
        if (element.Children.Count > 0)
        {
            // A width below the natural text width makes the text wrap.
            style.Width = random.Next(4) == 0 ? Length.Undefined : Length.Px(random.Next(20, 91));
            style.Height = random.Next(2) == 0 ? Length.Undefined : Length.Px(random.Next(20, 161));
            return;
        }

        if (random.Next(4) != 0)
        {
            element.Layout.Measure = random.Next(4) != 0
                ? s_textMeasures[random.Next(s_textMeasures.Length)]
                : s_measures[random.Next(s_measures.Length)];
        }

        if (element.Layout.Measure is null)
        {
            return;
        }

        // A maximum width below the natural text width (60 or 68 pixels) clamps the stored width, and the stored
        // height comes from a wider text. An absolute leaf with no offset gets no width limit in a row parent and a
        // limit in a column parent, so a new parent direction gives new constraints to a clean leaf.
        style.Display = Display.Flex;
        style.Width = Length.Undefined;
        style.Height = Length.Undefined;
        style.FlexBasis = Length.Undefined;
        style.MinWidth = random.Next(4) == 0 ? Length.Px(random.Next(0, 81)) : Length.Undefined;
        style.MinHeight = random.Next(4) == 0 ? Length.Px(random.Next(0, 81)) : Length.Undefined;
        style.MaxWidth = random.Next(4) != 0 ? Length.Px(random.Next(0, 46)) : Length.Undefined;
        style.MaxHeight = random.Next(4) == 0 ? Length.Px(random.Next(0, 81)) : Length.Undefined;
        if (style.MinWidth.Unit == LengthUnit.Undefined
            && style.MinHeight.Unit == LengthUnit.Undefined
            && style.MaxWidth.Unit == LengthUnit.Undefined
            && style.MaxHeight.Unit == LengthUnit.Undefined)
        {
            style.MinHeight = Length.Px(random.Next(0, 81));
        }

        if (random.Next(2) == 0)
        {
            style.Position = PositionType.Absolute;
            style.Left = Length.Undefined;
            style.Top = Length.Undefined;
            style.Right = Length.Undefined;
            style.Bottom = Length.Undefined;
        }

        if (random.Next(2) == 0)
        {
            style.AlignSelf = s_noStretch[random.Next(s_noStretch.Length)];
        }
    }

    /// <summary>
    /// Gives some containers a fixed height, a width of 100 percent, and two side paddings that add up to 100
    /// percent. Removes each percent from the styles of their children and grandchildren, and gives each empty child
    /// a content element (<see cref="TreeKind.NoiseInnerSize"/>).
    /// </summary>
    private static void ShapeNoiseInnerSize(Random random, List<Element> elements, ref int nextName)
    {
        // The root takes the viewport width, so a new viewport gives a new owner width to each noise container.
        ref ComputedStyle rootStyle = ref elements[0].Style;
        rootStyle.Display = Display.Flex;
        rootStyle.Width = random.Next(2) == 0 ? Length.Undefined : Length.Percent(random.Next(40, 101));
        rootStyle.MinWidth = Length.Undefined;
        rootStyle.MaxWidth = Length.Undefined;

        List<Element> noisy = [];
        foreach (Element element in elements)
        {
            if (element.Children.Count == 0)
            {
                continue;
            }

            // A child with its own children has a content size. The fixed-size path of the engine for a limit of 0
            // ignores the content, so a stale hit at the wrong side of 0 shows.
            bool hasGrandchildren = false;
            foreach (Element child in element.Children)
            {
                hasGrandchildren |= child.Children.Count > 0;
            }

            if (random.Next(4) >= (hasGrandchildren ? 3 : 1))
            {
                continue;
            }

            // The only percents are the width and the two side paddings, so the inner height of the container does
            // not change with the owner width.
            ref ComputedStyle style = ref element.Style;
            RemovePercents(ref style);
            int left = random.Next(1, 100);
            style.Display = Display.Flex;
            style.FlexDirection = (FlexDirection)random.Next(4);
            style.FlexWrap = FlexWrap.NoWrap;
            style.Width = Length.Percent(100);
            style.Height = Length.Px(random.Next(20, 201));
            style.MinWidth = Length.Undefined;
            style.MaxWidth = Length.Undefined;
            style.Padding.Left = Length.Percent(left);
            style.Padding.Right = Length.Percent(100 - left);
            style.BorderWidth.Left = Length.Undefined;
            style.BorderWidth.Right = Length.Undefined;
            style.Margin.Left = Length.Undefined;
            style.Margin.Right = Length.Undefined;
            noisy.Add(element);
        }

        foreach (Element element in noisy)
        {
            foreach (Element child in element.Children)
            {
                if (noisy.Contains(child))
                {
                    continue;
                }

                // With no width and no shrink, the child gets the inner width of the container as a limit, and its
                // content keeps its size. The child and its children hold no percent, so a new owner width does not
                // reset the cache of the child.
                ref ComputedStyle childStyle = ref child.Style;
                RemovePercents(ref childStyle);
                foreach (Element grandchild in child.Children)
                {
                    if (!noisy.Contains(grandchild))
                    {
                        RemovePercents(ref grandchild.Style);
                    }
                }

                // A leaf child gets the leaf path of the engine, which has no test against 0. A child with content
                // gets the fixed-size path for a limit of 0 and the full computation for a limit above 0.
                if (child.Children.Count == 0 && child.Layout.Measure is null)
                {
                    Element content = new() { Name = "n" + nextName++ };
                    content.Style.Width = Length.Px(random.Next(10, 61));
                    content.Style.Height = Length.Px(random.Next(10, 41));
                    child.Children.Add(content);
                }

                if (random.Next(4) != 0)
                {
                    childStyle.Width = Length.Undefined;
                    childStyle.FlexBasis = Length.Undefined;
                    childStyle.FlexShrink = 0f;
                }

                if (random.Next(2) == 0)
                {
                    childStyle.AlignSelf = s_noStretch[random.Next(s_noStretch.Length)];
                }
            }
        }
    }

    /// <summary>Replaces each percent length of a style with an undefined length.</summary>
    private static void RemovePercents(ref ComputedStyle style)
    {
        style.Width = WithoutPercent(style.Width);
        style.Height = WithoutPercent(style.Height);
        style.MinWidth = WithoutPercent(style.MinWidth);
        style.MinHeight = WithoutPercent(style.MinHeight);
        style.MaxWidth = WithoutPercent(style.MaxWidth);
        style.MaxHeight = WithoutPercent(style.MaxHeight);
        style.FlexBasis = WithoutPercent(style.FlexBasis);
        style.Left = WithoutPercent(style.Left);
        style.Top = WithoutPercent(style.Top);
        style.Right = WithoutPercent(style.Right);
        style.Bottom = WithoutPercent(style.Bottom);
        RemovePercents(ref style.Margin);
        RemovePercents(ref style.Padding);
    }

    /// <summary>Replaces each percent length of four edges with an undefined length.</summary>
    private static void RemovePercents(ref LengthEdges edges)
    {
        edges.Left = WithoutPercent(edges.Left);
        edges.Top = WithoutPercent(edges.Top);
        edges.Right = WithoutPercent(edges.Right);
        edges.Bottom = WithoutPercent(edges.Bottom);
    }

    /// <summary>Gives an undefined length for a percent length, else the same length.</summary>
    private static Length WithoutPercent(Length length)
    {
        return length.Unit == LengthUnit.Percent ? Length.Undefined : length;
    }

    /// <summary>Creates an element with a random style and a new name.</summary>
    private static Element NewElement(Random random, ref int nextName)
    {
        Element element = new() { Name = "n" + nextName++ };
        element.Style = RandomStyle(random);
        return element;
    }

    /// <summary>Collects the elements of the tree in depth-first order.</summary>
    private static void Collect(Element element, List<Element> elements)
    {
        elements.Add(element);
        foreach (Element child in element.Children)
        {
            Collect(child, elements);
        }
    }

    /// <summary>Gives the percent of the steps that change the viewport, for a kind of tree.</summary>
    private static int ViewportShare(TreeKind kind)
    {
        return kind switch
        {
            TreeKind.PercentRoot => 50,
            TreeKind.NoiseInnerSize => 60,
            TreeKind.Mixed => 10,
            _ => 15,
        };
    }

    /// <summary>Gives the percent of the steps that apply <see cref="ChangeContainer"/>, for a kind of tree.</summary>
    private static int ContainerShare(TreeKind kind)
    {
        return kind switch
        {
            TreeKind.MarginLeaves => 40,
            TreeKind.LimitedLeaves => 40,
            _ => 0,
        };
    }

    /// <summary>Applies one random change to the trees or to the context, and marks the dirty bits that it needs.</summary>
    /// <param name="random">The random source of the tree.</param>
    /// <param name="kind">The kind of the tree.</param>
    /// <param name="context">The context that holds the roots.</param>
    /// <param name="elements">The elements of each root of the context, in depth-first order.</param>
    /// <param name="structuralCounts">The counts of the reparents, the promotes, and the demotes. The method adds to them.</param>
    /// <param name="nextName">The number of the next element name.</param>
    /// <returns>A text that names the change.</returns>
    private static string ApplyRandomChange(
        Random random,
        TreeKind kind,
        UiContext context,
        List<Element> elements,
        int[] structuralCounts,
        ref int nextName)
    {
        Element target = elements[random.Next(elements.Count)];
        int roll = random.Next(100);
        if (roll < ViewportShare(kind))
        {
            return ChangeViewport(random, context);
        }

        if (roll < ViewportShare(kind) + ContainerShare(kind))
        {
            return ChangeContainer(random, kind, target);
        }

        int changeKind = random.Next(13);
        switch (changeKind)
        {
            case 0:
            {
                Element added = NewElement(random, ref nextName);
                if (random.Next(2) == 0)
                {
                    added.Layout.Measure = s_measures[random.Next(s_measures.Length)];
                }

                int index = random.Next(target.Children.Count + 1);
                target.Children.Insert(index, added);
                return $"add '{added.Name}' to '{PathOf(target)}' at {index}";
            }

            case 1:
            {
                if (target.Parent is null)
                {
                    return $"no change (remove of the root '{target.Name}')";
                }

                string path = PathOf(target);
                target.Parent!.Children.Remove(target);
                return $"remove '{path}'";
            }

            case 2:
            {
                int count = target.Children.Count;
                if (count < 2)
                {
                    return $"no change (move in '{PathOf(target)}' with {count} children)";
                }

                int from = random.Next(count);
                int to = random.Next(count);
                target.Children.Move(from, to);
                return $"move in '{PathOf(target)}' from {from} to {to}";
            }

            case 3:
                return ChangeViewport(random, context);

            case 4:
            {
                MeasureCallback? measure = random.Next(3) == 0 ? null : s_measures[random.Next(s_measures.Length)];
                target.Layout.Measure = measure;
                target.MarkDirty(DirtyFlags.Layout);
                return $"measure of '{PathOf(target)}' set to {(measure is null ? "none" : measure.Method.Name)}";
            }

            case 5:
                structuralCounts[0]++;
                return Reparent(random, elements);

            case 6:
            case 7:
            {
                // Case 6 prefers a promote and case 7 prefers a demote. A context with one root gets a promote, and a
                // context with MaxRoots roots gets a demote.
                int rootCount = context.Roots.Length;
                bool promote = rootCount < 2 || (changeKind == 6 && rootCount < MaxRoots);
                if (promote)
                {
                    structuralCounts[1]++;
                    return Promote(random, context, elements);
                }

                structuralCounts[2]++;
                return Demote(random, context, elements);
            }

            default:
            {
                string field = ChangeOneField(random, ref target.Style);
                target.MarkDirty(DirtyFlags.Layout);
                return $"field {field} of '{PathOf(target)}'";
            }
        }
    }

    /// <summary>
    /// Moves a laid-out subtree to a new parent outside the subtree. The new parent can be in the tree of another root.
    /// <see cref="ElementList.Insert(int, Element)"/> marks the old parent and the new parent.
    /// </summary>
    /// <remarks>Half of the moves select a subtree with children, when the trees hold one.</remarks>
    /// <param name="random">The random source of the tree.</param>
    /// <param name="elements">The elements of each root of the context.</param>
    /// <returns>A text that names the change.</returns>
    private static string Reparent(Random random, List<Element> elements)
    {
        bool withChildren = random.Next(2) == 0;
        Element? moving = PickElement(random, elements, e => e.Parent is not null && (!withChildren || e.Children.Count > 0))
            ?? PickElement(random, elements, e => e.Parent is not null);
        if (moving is null)
        {
            return "no change (reparent with no child in the trees)";
        }

        Element? newParent = PickElement(random, elements, e => !ReferenceEquals(e, moving.Parent) && !IsInSubtree(e, moving));
        if (newParent is null)
        {
            return $"no change (reparent of '{PathOf(moving)}' with no parent outside the subtree)";
        }

        string oldPath = PathOf(moving);
        int index = random.Next(newParent.Children.Count + 1);
        newParent.Children.Insert(index, moving);
        return $"reparent '{oldPath}' to '{PathOf(newParent)}' at {index}";
    }

    /// <summary>
    /// Makes a laid-out child a new root of the context. One promote in three also hides the new root. A root ignores
    /// <see cref="Display.None"/>, so the hidden root gets a visible layout.
    /// </summary>
    /// <param name="random">The random source of the tree.</param>
    /// <param name="context">The context that holds the roots.</param>
    /// <param name="elements">The elements of each root of the context.</param>
    /// <returns>A text that names the change.</returns>
    private static string Promote(Random random, UiContext context, List<Element> elements)
    {
        Element? child = PickElement(random, elements, e => e.Parent is not null);
        if (child is null)
        {
            return "no change (promote with no child in the trees)";
        }

        string oldPath = PathOf(child);
        child.Parent!.Children.Remove(child);
        context.AddRoot(child);
        if (random.Next(3) == 0 && child.Style.Display != Display.None)
        {
            child.Style.Display = Display.None;
            child.MarkDirty(DirtyFlags.Layout);
            return $"promote '{oldPath}' to root {context.Roots.Length - 1}, and hide it";
        }

        return $"promote '{oldPath}' to root {context.Roots.Length - 1}";
    }

    /// <summary>
    /// Moves a laid-out root into the tree of another root. The root leaves the context first, because an element with
    /// the root flag cannot be a child.
    /// </summary>
    /// <param name="random">The random source of the tree.</param>
    /// <param name="context">The context that holds the roots. It holds two roots or more.</param>
    /// <param name="elements">The elements of each root of the context.</param>
    /// <returns>A text that names the change.</returns>
    private static string Demote(Random random, UiContext context, List<Element> elements)
    {
        ReadOnlySpan<Element> roots = context.Roots;
        int rootIndex = random.Next(roots.Length);
        Element root = roots[rootIndex];
        Element? newParent = PickElement(random, elements, e => !IsInSubtree(e, root));
        if (newParent is null)
        {
            return $"no change (demote of root {rootIndex} with no other tree)";
        }

        bool isHidden = root.Style.Display == Display.None;
        context.RemoveRoot(root);
        int index = random.Next(newParent.Children.Count + 1);
        newParent.Children.Insert(index, root);
        return $"demote root {rootIndex}{(isHidden ? " (hidden)" : string.Empty)} to '{PathOf(root)}' at {index}";
    }

    /// <summary>Selects a random element that matches a condition.</summary>
    /// <param name="random">The random source of the tree.</param>
    /// <param name="elements">The elements to select from.</param>
    /// <param name="match">The condition.</param>
    /// <returns>An element that matches, or <see langword="null"/> when no element matches.</returns>
    private static Element? PickElement(Random random, List<Element> elements, Predicate<Element> match)
    {
        int count = 0;
        foreach (Element element in elements)
        {
            if (match(element))
            {
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        int pick = random.Next(count);
        foreach (Element element in elements)
        {
            if (match(element) && pick-- == 0)
            {
                return element;
            }
        }

        return null;
    }

    /// <summary>Tests whether an element is <paramref name="ancestor"/> or a descendant of it.</summary>
    /// <param name="element">The element to test.</param>
    /// <param name="ancestor">The root of the subtree.</param>
    /// <returns><see langword="true"/> when the element is in the subtree.</returns>
    private static bool IsInSubtree(Element element, Element ancestor)
    {
        for (Element? p = element; p is not null; p = p.Parent)
        {
            if (ReferenceEquals(p, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Selects a random viewport size. The context needs no dirty bit for it.</summary>
    /// <returns>A text that names the change.</returns>
    private static string ChangeViewport(Random random, UiContext context)
    {
        float2 viewport = s_viewports[random.Next(s_viewports.Length)];
        context.ViewportSize = viewport;
        return $"viewport ({viewport.X}, {viewport.Y})";
    }

    /// <summary>
    /// Changes one value of a container and marks it. The container is the target, or the parent of a leaf target,
    /// so its children stay clean and get new constraints.
    /// </summary>
    /// <remarks>
    /// A <see cref="TreeKind.MarginLeaves"/> tree gets a small size change. A <see cref="TreeKind.LimitedLeaves"/>
    /// tree gets a small size change, a new direction, or a new overflow, so that a clean leaf goes from no limit to
    /// a limit.
    /// </remarks>
    /// <returns>A text that names the change.</returns>
    private static string ChangeContainer(Random random, TreeKind kind, Element target)
    {
        Element container = target.Children.Count > 0 || target.Parent is null ? target : target.Parent;
        ref ComputedStyle style = ref container.Style;
        int what = kind == TreeKind.LimitedLeaves ? random.Next(4) : 0;
        if (what is 1 or 3)
        {
            // Row and column swap, and the reverse flag stays.
            FlexDirection old = style.FlexDirection;
            style.FlexDirection = (FlexDirection)((int)old ^ 2);
            container.MarkDirty(DirtyFlags.Layout);
            return $"FlexDirection of '{PathOf(container)}' from {old} to {style.FlexDirection}";
        }

        if (what == 2)
        {
            Overflow old = style.Overflow;
            style.Overflow = old == Overflow.Scroll ? Overflow.Visible : Overflow.Scroll;
            container.MarkDirty(DirtyFlags.Layout);
            return $"Overflow of '{PathOf(container)}' from {old} to {style.Overflow}";
        }

        return NudgeSize(random, container);
    }

    /// <summary>
    /// Changes the width or the height of a container by 1 to 8 pixels, or gives it a pixel size, and marks it.
    /// A small change of a limit is the case of rule 4 of the cache.
    /// </summary>
    /// <returns>A text that names the change.</returns>
    private static string NudgeSize(Random random, Element container)
    {
        ref ComputedStyle style = ref container.Style;
        bool isWidth = random.Next(2) == 0;
        ref Length length = ref isWidth ? ref style.Width : ref style.Height;
        Length old = length;
        if (old.Unit == LengthUnit.Pixel)
        {
            int step = random.Next(1, 9);
            length = Length.Px(MathF.Max(0f, old.Value + (random.Next(2) == 0 ? -step : step)));
        }
        else
        {
            length = Length.Px(random.Next(20, 121));
        }

        container.MarkDirty(DirtyFlags.Layout);
        return $"{(isWidth ? "Width" : "Height")} of '{PathOf(container)}' from {old} to {length}";
    }

    /// <summary>Copies one field from a new random style into <paramref name="style"/>.</summary>
    /// <returns>A text that names the field and the new value.</returns>
    private static string ChangeOneField(Random random, ref ComputedStyle style)
    {
        ComputedStyle source = RandomStyle(random);
        switch (random.Next(30))
        {
            case 0: style.FlexDirection = source.FlexDirection; return $"FlexDirection = {style.FlexDirection}";
            case 1: style.FlexWrap = source.FlexWrap; return $"FlexWrap = {style.FlexWrap}";
            case 2: style.JustifyContent = source.JustifyContent; return $"JustifyContent = {style.JustifyContent}";
            case 3: style.AlignItems = source.AlignItems; return $"AlignItems = {style.AlignItems}";
            case 4: style.AlignSelf = source.AlignSelf; return $"AlignSelf = {style.AlignSelf}";
            case 5: style.AlignContent = source.AlignContent; return $"AlignContent = {style.AlignContent}";
            case 6: style.Display = style.Display == Display.None ? Display.Flex : Display.None; return $"Display = {style.Display}";
            case 7: style.Position = source.Position; return $"Position = {style.Position}";
            case 8: style.Overflow = source.Overflow; return $"Overflow = {style.Overflow}";
            case 9: style.Width = source.Width; return $"Width = {style.Width}";
            case 10: style.Height = source.Height; return $"Height = {style.Height}";
            case 11: style.MinWidth = source.MinWidth; return $"MinWidth = {style.MinWidth}";
            case 12: style.MinHeight = source.MinHeight; return $"MinHeight = {style.MinHeight}";
            case 13: style.MaxWidth = source.MaxWidth; return $"MaxWidth = {style.MaxWidth}";
            case 14: style.MaxHeight = source.MaxHeight; return $"MaxHeight = {style.MaxHeight}";
            case 15: style.FlexBasis = source.FlexBasis; return $"FlexBasis = {style.FlexBasis}";
            case 16: style.FlexGrow = source.FlexGrow; return $"FlexGrow = {style.FlexGrow}";
            case 17: style.FlexShrink = source.FlexShrink; return $"FlexShrink = {style.FlexShrink}";
            case 18: style.Margin.Left = RandomMargin(random); return $"Margin.Left = {style.Margin.Left}";
            case 19: style.Margin.Top = RandomMargin(random); return $"Margin.Top = {style.Margin.Top}";
            case 20: style.Margin.Right = RandomMargin(random); return $"Margin.Right = {style.Margin.Right}";
            case 21: style.Margin.Bottom = RandomMargin(random); return $"Margin.Bottom = {style.Margin.Bottom}";
            case 22: style.Padding = source.Padding; return $"Padding = {Describe(style.Padding)}";
            case 23: style.BorderWidth = source.BorderWidth; return $"BorderWidth = {Describe(style.BorderWidth)}";
            case 24: style.Left = source.Left; return $"Left = {style.Left}";
            case 25: style.Top = source.Top; return $"Top = {style.Top}";
            case 26: style.Right = source.Right; return $"Right = {style.Right}";
            case 27: style.Bottom = source.Bottom; return $"Bottom = {style.Bottom}";
            case 28: style.Margin = source.Margin; return $"Margin = {Describe(style.Margin)}";
            default: style = source; return "the whole style";
        }
    }

    /// <summary>Gives a text for the four edges of a <see cref="LengthEdges"/> value.</summary>
    private static string Describe(LengthEdges edges)
    {
        StringBuilder text = new();
        text.Append('(').Append(edges.Left).Append(", ").Append(edges.Top).Append(", ")
            .Append(edges.Right).Append(", ").Append(edges.Bottom).Append(')');
        return text.ToString();
    }

    /// <summary>Gives a random style. Each enum value and each length kind of the supported fields can occur.</summary>
    private static ComputedStyle RandomStyle(Random random)
    {
        ComputedStyle style = default;
        style.FlexDirection = (FlexDirection)random.Next(4);
        style.FlexWrap = random.Next(3) == 0 ? (FlexWrap)random.Next(3) : FlexWrap.NoWrap;
        style.JustifyContent = (Justify)random.Next(6);
        style.AlignItems = (Align)random.Next(8);
        style.AlignSelf = random.Next(3) == 0 ? (Align)random.Next(8) : Align.Auto;
        style.AlignContent = (Align)random.Next(8);
        style.Display = random.Next(12) == 0 ? Display.None : Display.Flex;
        style.Position = random.Next(7) == 0 ? PositionType.Absolute : PositionType.Relative;
        style.Overflow = random.Next(5) == 0 ? (Overflow)random.Next(3) : Overflow.Visible;

        style.Width = RandomSize(random, 200);
        style.Height = RandomSize(random, 200);
        style.MinWidth = random.Next(4) == 0 ? RandomSize(random, 80) : Length.Undefined;
        style.MinHeight = random.Next(4) == 0 ? RandomSize(random, 80) : Length.Undefined;
        style.MaxWidth = random.Next(4) == 0 ? RandomSize(random, 250) : Length.Undefined;
        style.MaxHeight = random.Next(4) == 0 ? RandomSize(random, 250) : Length.Undefined;
        style.FlexBasis = random.Next(3) == 0 ? RandomSize(random, 100) : Length.Undefined;
        style.FlexGrow = random.Next(3) == 0 ? s_factors[random.Next(s_factors.Length)] : 0f;
        style.FlexShrink = random.Next(3) == 0 ? s_factors[random.Next(s_factors.Length)] : 0f;

        style.Margin = new LengthEdges(RandomMargin(random), RandomMargin(random), RandomMargin(random), RandomMargin(random));
        style.Padding = new LengthEdges(RandomEdge(random), RandomEdge(random), RandomEdge(random), RandomEdge(random));
        style.BorderWidth = new LengthEdges(RandomBorder(random), RandomBorder(random), RandomBorder(random), RandomBorder(random));

        style.Left = random.Next(4) == 0 ? RandomSize(random, 30) : Length.Undefined;
        style.Top = random.Next(4) == 0 ? RandomSize(random, 30) : Length.Undefined;
        style.Right = random.Next(6) == 0 ? RandomSize(random, 30) : Length.Undefined;
        style.Bottom = random.Next(6) == 0 ? RandomSize(random, 30) : Length.Undefined;
        return style;
    }

    /// <summary>Gives an undefined, auto, percent, or pixel length for a size, a limit, a basis, or an offset.</summary>
    private static Length RandomSize(Random random, int maxPixels)
    {
        int kind = random.Next(10);
        return kind switch
        {
            < 4 => Length.Undefined,
            4 => Length.Auto,
            5 or 6 => Length.Percent(random.Next(5, 101)),
            _ => Length.Px(random.Next(0, maxPixels)),
        };
    }

    /// <summary>Gives an undefined, auto, percent, or pixel margin. A pixel margin can be negative.</summary>
    private static Length RandomMargin(Random random)
    {
        int kind = random.Next(12);
        return kind switch
        {
            < 6 => Length.Undefined,
            6 => Length.Auto,
            7 => Length.Percent(random.Next(1, 20)),
            8 => Length.Px(-random.Next(1, 8)),
            _ => Length.Px(random.Next(0, 12)),
        };
    }

    /// <summary>Gives an undefined, percent, or pixel padding.</summary>
    private static Length RandomEdge(Random random)
    {
        int kind = random.Next(10);
        return kind switch
        {
            < 6 => Length.Undefined,
            6 => Length.Percent(random.Next(1, 20)),
            _ => Length.Px(random.Next(0, 12)),
        };
    }

    /// <summary>Gives an undefined or pixel border width.</summary>
    private static Length RandomBorder(Random random)
    {
        return random.Next(3) == 0 ? Length.Px(random.Next(0, 6)) : Length.Undefined;
    }
}
