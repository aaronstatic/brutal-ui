using System;
using System.Runtime.CompilerServices;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the style pass and the sheet calls of <see cref="UiContext"/>: the demo, the dirty bits of a restyle,
/// the cascade order, the visit rule, the full restyle after a sheet add, the compare of each member, and the
/// allocation gate.
/// </summary>
/// <remarks>
/// <para>
/// A test that reads the dirty bits calls <c>UiContext.UpdateStyles</c> alone, before the layout pass clears the bits.
/// A test that reads the values calls <c>UiContext.Update</c>.
/// </para>
/// <para>
/// The class table is global and xUnit runs test classes in parallel.
/// Each test uses class names with a unique prefix, and no test asserts a fixed id value.
/// </para>
/// </remarks>
public sealed class StylePassTests
{
    private static readonly Rgba8 Red = new(255, 0, 0, 255);
    private static readonly Rgba8 Blue = new(0, 0, 255, 255);

    [Fact]
    public void PaintsAPanelFromAClassRule()
    {
        ClassId panelClass = ClassName.Intern(nameof(PaintsAPanelFromAClassRule) + ".panel");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), new StyleProperties { BackgroundColor = new Rgba8(30, 34, 44, 255) })
            .Build();

        Element root = new() { Name = "root" };
        Element panel = new() { Name = "panel" };
        Element plain = new() { Name = "plain" };
        panel.Classes.Add(panelClass);
        root.Children.Add(panel);
        root.Children.Add(plain);

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        string panelLine = panel.Name + " BackgroundColor " + panel.Style.BackgroundColor;
        string plainLine = plain.Name + " BackgroundColor " + plain.Style.BackgroundColor;
        TestContext.Current.TestOutputHelper?.WriteLine(panelLine);
        TestContext.Current.TestOutputHelper?.WriteLine(plainLine);

        Assert.Equal("panel BackgroundColor #1E222CFF", panelLine);
        Assert.Equal("plain BackgroundColor #00000000", plainLine);
    }

    [Fact]
    public void ANewPaintValueMarksPaintAndNotLayout()
    {
        ClassId id = ClassName.Intern(nameof(ANewPaintValueMarksPaintAndNotLayout) + ".a");
        (UiContext context, Element root, Element child) = BuildWarm(
            new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { BackgroundColor = Red }).Build());

        child.Classes.Add(id);
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.Paint, child.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, root.Dirty);
        Assert.Equal(Red, child.Style.BackgroundColor);
    }

    [Fact]
    public void ANewWidthMarksLayoutAndNotPaint()
    {
        ClassId id = ClassName.Intern(nameof(ANewWidthMarksLayoutAndNotPaint) + ".a");
        (UiContext context, Element root, Element child) = BuildWarm(
            new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { Width = 120f }).Build());

        child.Classes.Add(id);
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.Layout, child.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, root.Dirty);

        context.Update(0f);
        Assert.Equal(120f, child.Layout.Rect.Z);
    }

    [Fact]
    public void AnOverflowChangeMarksLayoutOnly()
    {
        ClassId id = ClassName.Intern(nameof(AnOverflowChangeMarksLayoutOnly) + ".a");
        (UiContext context, _, Element child) = BuildWarm(
            new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { Overflow = Overflow.Hidden }).Build());

        child.Classes.Add(id);
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.Layout, child.Dirty);
    }

    [Fact]
    public void ARestyleWithTheSameResultMarksNothing()
    {
        ClassId id = ClassName.Intern(nameof(ARestyleWithTheSameResultMarksNothing) + ".a");
        (UiContext context, Element root, Element child) = BuildWarm(
            new StyleSheet.Builder()
                .Add(Selector.ForClass(id), new StyleProperties { BackgroundColor = Red, Width = 40f })
                .Build());
        child.Classes.Add(id);
        context.Update(0f);

        child.MarkDirty(DirtyFlags.Style);
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.None, child.Dirty);
        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(Red, child.Style.BackgroundColor);
        Assert.Equal(Length.Px(40f), child.Style.Width);
    }

    [Fact]
    public void ASetOpacityOfOneIsNoChange()
    {
        ClassId id = ClassName.Intern(nameof(ASetOpacityOfOneIsNoChange) + ".a");
        (UiContext context, _, Element child) = BuildWarm(
            new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { Opacity = 1f }).Build());

        child.Classes.Add(id);
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.None, child.Dirty);
        Assert.Equal(1f, child.Style.Opacity);
    }

    [Fact]
    public void ANaNFloatToNaNIsNoChange()
    {
        ClassId id = ClassName.Intern(nameof(ANaNFloatToNaNIsNoChange) + ".a");
        (UiContext context, _, Element child) = BuildWarm(
            new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { FlexGrow = float.NaN }).Build());
        child.Classes.Add(id);
        context.Update(0f);

        child.MarkDirty(DirtyFlags.Style);
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.None, child.Dirty);
        Assert.True(float.IsNaN(child.Style.FlexGrow));
    }

    [Fact]
    public void ThePassClearsTheStyleBitsOnEachElement()
    {
        ClassId id = ClassName.Intern(nameof(ThePassClearsTheStyleBitsOnEachElement) + ".a");
        StyleSheet sheet = new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { BackgroundColor = Red }).Build();
        Element root = new();
        Element middle = new();
        Element leaf = new();
        Element sibling = new();
        root.Children.Add(middle);
        root.Children.Add(sibling);
        middle.Children.Add(leaf);
        leaf.Classes.Add(id);

        UiContext context = new();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.UpdateStyles();

        const DirtyFlags styleBits = DirtyFlags.Style | DirtyFlags.DescendantStyle;
        Assert.Equal(DirtyFlags.None, root.Dirty & styleBits);
        Assert.Equal(DirtyFlags.None, middle.Dirty & styleBits);
        Assert.Equal(DirtyFlags.None, leaf.Dirty & styleBits);
        Assert.Equal(DirtyFlags.None, sibling.Dirty & styleBits);
        Assert.Equal(Red, leaf.Style.BackgroundColor);

        // The pass marks no style bit and clears no layout bit or paint bit.
        Assert.Equal(DirtyFlags.Layout | DirtyFlags.Paint, leaf.Dirty & DirtyBits.SelfMask);
    }

    [Fact]
    public void ALaterRuleWinsAtEqualSpecificity()
    {
        ClassId a = ClassName.Intern(nameof(ALaterRuleWinsAtEqualSpecificity) + ".a");
        ClassId b = ClassName.Intern(nameof(ALaterRuleWinsAtEqualSpecificity) + ".b");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(a), new StyleProperties { BackgroundColor = Red, Width = 10f })
            .Add(Selector.ForClass(b), new StyleProperties { BackgroundColor = Blue })
            .Build();
        Element element = new();
        element.Classes.Add(b);
        element.Classes.Add(a);

        UiContext context = new();
        context.AddRoot(element);
        context.AddSheet(sheet);
        context.Update(0f);

        Assert.Equal(Blue, element.Style.BackgroundColor);
        Assert.Equal(Length.Px(10f), element.Style.Width);
    }

    [Fact]
    public void ARuleOfALaterSheetWinsAtEqualSpecificity()
    {
        ClassId a = ClassName.Intern(nameof(ARuleOfALaterSheetWinsAtEqualSpecificity) + ".a");
        ClassId b = ClassName.Intern(nameof(ARuleOfALaterSheetWinsAtEqualSpecificity) + ".b");
        StyleSheet first = new StyleSheet.Builder()
            .Add(Selector.ForClass(b), new StyleProperties { BackgroundColor = Red, Height = 30f })
            .Build();
        StyleSheet second = new StyleSheet.Builder()
            .Add(Selector.ForClass(a), new StyleProperties { BackgroundColor = Blue })
            .Build();
        Element element = new();
        element.Classes.Add(a);
        element.Classes.Add(b);

        UiContext context = new();
        context.AddRoot(element);
        context.AddSheet(first);
        context.AddSheet(second);
        context.Update(0f);

        Assert.Equal(Blue, element.Style.BackgroundColor);
        Assert.Equal(Length.Px(30f), element.Style.Height);
    }

    [Fact]
    public void SkipsAnElementWithNoStyleBit()
    {
        ClassId id = ClassName.Intern(nameof(SkipsAnElementWithNoStyleBit) + ".a");
        (UiContext context, _, Element child) = BuildWarm(
            new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { BackgroundColor = Red }).Build());

        Assert.True(child.ClassStorage.Add(id));
        context.UpdateStyles();

        Assert.Equal(Rgba8.Transparent, child.Style.BackgroundColor);
        Assert.Equal(DirtyFlags.None, child.Dirty);
    }

    [Fact]
    public void AddSheetAfterTheFirstUpdateRestylesEachElement()
    {
        ClassId id = ClassName.Intern(nameof(AddSheetAfterTheFirstUpdateRestylesEachElement) + ".a");
        Element root = new();
        Element middle = new();
        Element leaf = new();
        Element lone = new();
        root.Children.Add(middle);
        middle.Children.Add(leaf);
        leaf.Classes.Add(id);
        lone.Classes.Add(id);

        UiContext context = new();
        context.AddRoot(root);
        context.AddRoot(lone);
        context.Update(0f);
        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, leaf.Dirty);

        context.AddSheet(new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { BackgroundColor = Red }).Build());

        Assert.Equal(DirtyFlags.Style | DirtyFlags.DescendantStyle, root.Dirty);
        Assert.Equal(DirtyFlags.Style | DirtyFlags.DescendantStyle, middle.Dirty);
        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        Assert.Equal(DirtyFlags.Style, lone.Dirty);

        context.Update(0f);

        Assert.Equal(Red, leaf.Style.BackgroundColor);
        Assert.Equal(Red, lone.Style.BackgroundColor);
        Assert.Equal(Rgba8.Transparent, middle.Style.BackgroundColor);
        Assert.Equal(DirtyFlags.None, root.Dirty);
    }

    [Fact]
    public void MarkSubtreeStyleSendsTheDescendantBitUp()
    {
        Element root = new();
        Element middle = new();
        Element leaf = new();
        root.Children.Add(middle);
        middle.Children.Add(leaf);
        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);

        leaf.MarkSubtreeStyle();

        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, middle.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, root.Dirty);
    }

    [Fact]
    public void AddSheetThrowsForNull()
    {
        UiContext context = new();

        Assert.Throws<ArgumentNullException>("sheet", () => context.AddSheet(null!));
    }

    [Fact]
    public void AddSheetThrowsForTheSameSheetTwice()
    {
        ClassId id = ClassName.Intern(nameof(AddSheetThrowsForTheSameSheetTwice) + ".a");
        StyleSheet sheet = new StyleSheet.Builder().Add(Selector.ForClass(id), new StyleProperties { Width = 5f }).Build();
        UiContext context = new();
        context.AddSheet(sheet);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => context.AddSheet(sheet));

        Assert.Contains("one time", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyCascadeRestylesAnElementWithNoInlineWrite()
    {
        // The pass owns the computed style. The test puts a stale value in the style through Unsafe.AsRef, because
        // Element.Style is ref readonly. The pass then writes the defaults over the stale value.
        Element element = new();
        UiContext context = new();
        context.AddRoot(element);
        context.AddSheet(new StyleSheet.Builder().Build());
        context.Update(0f);
        Unsafe.AsRef(in element.Style).BackgroundColor = Red;
        element.MarkDirty(DirtyFlags.Style);

        context.Update(0f);

        Assert.Equal(default, element.Style.BackgroundColor);
    }

    [Theory]
    [MemberData(nameof(EachMember))]
    public void CompareGivesTheKindOfEachMember(StyleProperty member)
    {
        StyleProperties properties = default;
        SetToANonDefaultValue(ref properties, member);
        ComputedStyle current = default;
        ComputedStyle next = default;
        properties.ApplyTo(ref next, 0UL);

        DirtyFlags bits = StylePass.Compare(in current, in next, out bool textChanged);

        bool layout = (StyleMasks.Layout & (1UL << (int)member)) != 0;
        bool text = (StyleMasks.Text & (1UL << (int)member)) != 0;
        Assert.Equal(layout ? DirtyFlags.Layout : DirtyFlags.Paint, bits);
        Assert.Equal(text, textChanged);
        Assert.Equal(DirtyFlags.None, StylePass.Compare(in next, in next, out bool noTextChange));
        Assert.False(noTextChange);
    }

    [Fact]
    public void CompareChecksEachEdgeAndEachCorner()
    {
        for (int edge = 0; edge < 4; edge++)
        {
            ComputedStyle margin = default;
            ComputedStyle padding = default;
            ComputedStyle border = default;
            margin.Margin = OneEdge(edge);
            padding.Padding = OneEdge(edge);
            border.BorderWidth = OneEdge(edge);

            Assert.Equal(DirtyFlags.Layout, StylePass.Compare(default(ComputedStyle), in margin));
            Assert.Equal(DirtyFlags.Layout, StylePass.Compare(default(ComputedStyle), in padding));
            Assert.Equal(DirtyFlags.Layout, StylePass.Compare(default(ComputedStyle), in border));
        }

        for (int corner = 0; corner < 4; corner++)
        {
            ComputedStyle style = default;
            style.CornerRadius = new Corners(
                corner == 0 ? 3f : 0f,
                corner == 1 ? 3f : 0f,
                corner == 2 ? 3f : 0f,
                corner == 3 ? 3f : 0f);

            Assert.Equal(DirtyFlags.Paint, StylePass.Compare(default(ComputedStyle), in style));
        }
    }

    [Fact]
    public void TheStylePassAllocatesNothingAfterWarmUp()
    {
        AllocationScene scene = AllocationScene.Build();

        // Warm-up: the steps let the JIT compile the pass and the class sets reach their size.
        for (int i = 0; i < 16; i++)
        {
            scene.Step();
        }

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            scene.Step();
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
        Assert.Equal(116, scene.StepCount);
    }

    /// <summary>Gives each <see cref="StyleProperty"/> member for the theory.</summary>
    public static TheoryData<StyleProperty> EachMember()
    {
        TheoryData<StyleProperty> data = [];
        foreach (StyleProperty member in Enum.GetValues<StyleProperty>())
        {
            data.Add(member);
        }

        return data;
    }

    /// <summary>
    /// Builds a context with one sheet, a root, and one child, and runs one <c>Update</c>, so each dirty bit is clear.
    /// </summary>
    private static (UiContext Context, Element Root, Element Child) BuildWarm(StyleSheet sheet)
    {
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, child.Dirty);
        return (context, root, child);
    }

    private static LengthEdges OneEdge(int edge)
    {
        return new LengthEdges(
            edge == 0 ? 2f : Length.Undefined,
            edge == 1 ? 2f : Length.Undefined,
            edge == 2 ? 2f : Length.Undefined,
            edge == 3 ? 2f : Length.Undefined);
    }

    /// <summary>Sets one member of a set to a value that is not the <see cref="ComputedStyle"/> default.</summary>
    private static void SetToANonDefaultValue(ref StyleProperties properties, StyleProperty member)
    {
        switch (member)
        {
            case StyleProperty.FlexDirection: properties.FlexDirection = FlexDirection.Row; break;
            case StyleProperty.Width: properties.Width = 1f; break;
            case StyleProperty.Height: properties.Height = 1f; break;
            case StyleProperty.Margin: properties.Margin = LengthEdges.All(1f); break;
            case StyleProperty.Padding: properties.Padding = LengthEdges.All(1f); break;
            case StyleProperty.BorderWidth: properties.BorderWidth = LengthEdges.All(1f); break;
            case StyleProperty.FlexGrow: properties.FlexGrow = 1f; break;
            case StyleProperty.FlexShrink: properties.FlexShrink = 1f; break;
            case StyleProperty.FlexBasis: properties.FlexBasis = 1f; break;
            case StyleProperty.MinWidth: properties.MinWidth = 1f; break;
            case StyleProperty.MinHeight: properties.MinHeight = 1f; break;
            case StyleProperty.MaxWidth: properties.MaxWidth = 1f; break;
            case StyleProperty.MaxHeight: properties.MaxHeight = 1f; break;
            case StyleProperty.JustifyContent: properties.JustifyContent = Justify.Center; break;
            case StyleProperty.AlignItems: properties.AlignItems = Align.Center; break;
            case StyleProperty.AlignSelf: properties.AlignSelf = Align.Center; break;
            case StyleProperty.AlignContent: properties.AlignContent = Align.Center; break;
            case StyleProperty.FlexWrap: properties.FlexWrap = FlexWrap.Wrap; break;
            case StyleProperty.Display: properties.Display = Display.None; break;
            case StyleProperty.Position: properties.Position = PositionType.Absolute; break;
            case StyleProperty.Left: properties.Left = 1f; break;
            case StyleProperty.Top: properties.Top = 1f; break;
            case StyleProperty.Right: properties.Right = 1f; break;
            case StyleProperty.Bottom: properties.Bottom = 1f; break;
            case StyleProperty.Overflow: properties.Overflow = Overflow.Hidden; break;
            case StyleProperty.BackgroundColor: properties.BackgroundColor = Red; break;
            case StyleProperty.BorderColor: properties.BorderColor = Red; break;
            case StyleProperty.CornerRadius: properties.CornerRadius = Corners.All(1f); break;
            case StyleProperty.Opacity: properties.Opacity = 0.5f; break;
            case StyleProperty.Color: properties.Color = Red; break;
            case StyleProperty.Font: properties.Font = new FontHandle(1); break;
            case StyleProperty.FontSize: properties.FontSize = 1f; break;
            default: throw new ArgumentOutOfRangeException(nameof(member), member, "The test has no value for the member. Add a case.");
        }
    }

    /// <summary>
    /// The tree of the allocation gate: a root, 7 children, and 6 leaves under each child (50 elements), and a sheet
    /// of 4 rules. Each step toggles one class on one element and runs one <c>Update</c>.
    /// </summary>
    private sealed class AllocationScene
    {
        private const int ChildCount = 7;
        private const int LeafCount = 6;

        public required UiContext Context { get; init; }

        public required Element[] Elements { get; init; }

        public required ClassId[] Classes { get; init; }

        public int StepCount { get; private set; }

        public static AllocationScene Build()
        {
            string prefix = nameof(TheStylePassAllocatesNothingAfterWarmUp);
            ClassId[] classes =
            [
                ClassName.Intern(prefix + ".fill"),
                ClassName.Intern(prefix + ".wide"),
                ClassName.Intern(prefix + ".faded"),
                ClassName.Intern(prefix + ".padded"),
            ];

            StyleSheet sheet = new StyleSheet.Builder()
                .Add(Selector.ForClass(classes[0]), new StyleProperties { BackgroundColor = Red })
                .Add(Selector.ForClass(classes[1]), new StyleProperties { Width = 30f, FlexGrow = 1f })
                .Add(Selector.ForClass(classes[2]), new StyleProperties { Opacity = 0.5f, BorderColor = Blue })
                .Add(Selector.ForClass(classes[3]), new StyleProperties { Padding = LengthEdges.All(4f), CornerRadius = Corners.All(2f) })
                .Build();

            Element[] elements = new Element[1 + ChildCount + (ChildCount * LeafCount)];
            Element root = new();
            elements[0] = root;
            int index = 1;
            for (int c = 0; c < ChildCount; c++)
            {
                Element child = new();
                child.Classes.Add(classes[c % classes.Length]);
                root.Children.Add(child);
                elements[index++] = child;
                for (int l = 0; l < LeafCount; l++)
                {
                    Element leaf = new();
                    leaf.Classes.Add(classes[(c + l) % classes.Length]);
                    child.Children.Add(leaf);
                    elements[index++] = leaf;
                }
            }

            UiContext context = new() { ViewportSize = new float2(1280f, 720f) };
            context.AddRoot(root);
            context.AddSheet(sheet);
            context.Update(0f);

            return new AllocationScene { Context = context, Elements = elements, Classes = classes };
        }

        /// <summary>The measured body. It uses no lambda, no boxing, no string, and no Assert.</summary>
        public void Step()
        {
            int step = StepCount++;
            Element element = Elements[1 + (step % (Elements.Length - 1))];
            ClassId id = Classes[step % Classes.Length];
            ClassList list = element.Classes;
            if (!list.Remove(id))
            {
                list.Add(id);
            }

            Context.Update(0f);
        }
    }
}
