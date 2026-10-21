using System;
using System.Globalization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of <see cref="InlineStyle"/> and of the inline step of the style pass: the demo, the marks of a set and
/// of a clear, the win over each rule, the path with no sheet, the default view, and each member of the view.
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
public sealed class InlineStyleTests
{
    private static readonly Rgba8 PanelColor = new(30, 34, 44, 255);
    private static readonly Rgba8 InlineColor = new(48, 96, 192, 255);
    private static readonly Rgba8 Red = new(255, 0, 0, 255);
    private static readonly Rgba8 Green = new(0, 255, 0, 255);

    [Fact]
    public void AnInlineValueWinsOverARule()
    {
        ClassId panelClass = ClassName.Intern(nameof(AnInlineValueWinsOverARule) + ".panel");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), new StyleProperties { BackgroundColor = PanelColor, Width = 200f })
            .Build();

        Element root = new();
        Element panel = new();
        panel.Classes.Add(panelClass);
        root.Children.Add(panel);

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.AddSheet(sheet);

        panel.Inline.BackgroundColor = InlineColor;
        context.Update(0f);
        string inlineLine = Describe(panel);
        TestContext.Current.TestOutputHelper?.WriteLine(inlineLine);

        panel.Inline.Clear(StyleProperty.BackgroundColor);
        context.Update(0f);
        string ruleLine = Describe(panel);
        TestContext.Current.TestOutputHelper?.WriteLine(ruleLine);

        Assert.Equal("BackgroundColor #3060C0FF width 200", inlineLine);
        Assert.Equal("BackgroundColor #1E222CFF width 200", ruleLine);
    }

    [Fact]
    public void ASetMarksStyleAndTheAncestors()
    {
        (UiContext _, Element root, Element middle, Element leaf) = BuildWarmChain(sheet: null);

        leaf.Inline.Width = 40f;

        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, middle.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, root.Dirty);
        Assert.True(leaf.Inline.IsSet(StyleProperty.Width));
    }

    [Fact]
    public void ASetToTheSameValueMarksNothing()
    {
        (UiContext context, Element _, Element _, Element leaf) = BuildWarmChain(sheet: null);
        leaf.Inline.Width = 40f;
        leaf.Inline.Margin = new LengthEdges(1f, 2f, 3f, 4f);
        leaf.Inline.CornerRadius = new Corners(1f, 2f, 3f, 4f);
        leaf.Inline.FlexGrow = float.NaN;
        leaf.Inline.BackgroundColor = Red;
        leaf.Inline.Display = Display.None;
        leaf.Inline.Opacity = 1f;
        context.Update(0f);
        Assert.Equal(DirtyFlags.None, leaf.Dirty);

        leaf.Inline.Width = 40f;
        leaf.Inline.Margin = new LengthEdges(1f, 2f, 3f, 4f);
        leaf.Inline.CornerRadius = new Corners(1f, 2f, 3f, 4f);
        leaf.Inline.FlexGrow = float.NaN;
        leaf.Inline.BackgroundColor = Red;
        leaf.Inline.Display = Display.None;
        leaf.Inline.Opacity = 1f;

        Assert.Equal(DirtyFlags.None, leaf.Dirty);
    }

    [Fact]
    public void ASetOfTheDefaultOnAnUnsetMemberMarksStyle()
    {
        (UiContext _, Element _, Element _, Element leaf) = BuildWarmChain(sheet: null);

        leaf.Inline.Opacity = 1f;

        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        Assert.True(leaf.Inline.IsSet(StyleProperty.Opacity));
    }

    [Fact]
    public void AClearOfAnUnsetMemberMarksNothing()
    {
        (UiContext _, Element root, Element _, Element leaf) = BuildWarmChain(sheet: null);

        leaf.Inline.Clear(StyleProperty.Width);

        Assert.Equal(DirtyFlags.None, leaf.Dirty);
        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.False(leaf.Inline.IsSet(StyleProperty.Width));
    }

    [Fact]
    public void AClearOfASetMemberMarksStyle()
    {
        (UiContext context, Element _, Element _, Element leaf) = BuildWarmChain(sheet: null);
        leaf.Inline.Width = 40f;
        context.Update(0f);

        leaf.Inline.Clear(StyleProperty.Width);

        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        Assert.False(leaf.Inline.IsSet(StyleProperty.Width));
    }

    [Fact]
    public void AnInlineValueReachesTheStyleWithNoSheet()
    {
        Element root = new();
        Element panel = new();
        root.Children.Add(panel);
        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);

        panel.Inline.Width = 120f;
        panel.Inline.BackgroundColor = InlineColor;
        context.Update(0f);

        Assert.Equal(Length.Px(120f), panel.Style.Width);
        Assert.Equal(InlineColor, panel.Style.BackgroundColor);
        Assert.Equal(120f, panel.Layout.Rect.Z);
    }

    [Fact]
    public void AClearOfTheLastInlineValueRestylesWithNoSheet()
    {
        Element root = new();
        Element panel = new();
        root.Children.Add(panel);
        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        panel.Inline.BackgroundColor = InlineColor;
        context.Update(0f);

        panel.Inline.Clear(StyleProperty.BackgroundColor);
        context.Update(0f);

        Assert.Equal(default, panel.Style.BackgroundColor);
    }

    [Fact]
    public void AnInlineSetWithNoViewWriteRestylesWithNoSheet()
    {
        // The test writes the inline set directly, not through the view. The pass has no gate, so it applies the set
        // also with no sheet.
        Element root = new();
        Element panel = new();
        root.Children.Add(panel);
        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);

        panel.InlineStorage = new StyleProperties { BackgroundColor = Red };
        panel.MarkDirty(DirtyFlags.Style);
        context.Update(0f);

        Assert.Equal(Red, panel.Style.BackgroundColor);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AnInlineValueWinsOverEachRuleOrder(bool reverse, bool twoSheets)
    {
        string prefix = nameof(AnInlineValueWinsOverEachRuleOrder) + "." + reverse + "." + twoSheets;
        ClassId first = ClassName.Intern(prefix + ".first");
        ClassId second = ClassName.Intern(prefix + ".second");
        StyleProperties firstSet = new() { BackgroundColor = Red, Width = 10f };
        StyleProperties secondSet = new() { BackgroundColor = Green, Height = 20f };
        (ClassId Id, StyleProperties Set) a = reverse ? (second, secondSet) : (first, firstSet);
        (ClassId Id, StyleProperties Set) b = reverse ? (first, firstSet) : (second, secondSet);

        Element root = new();
        Element panel = new();
        panel.Classes.Add(first);
        panel.Classes.Add(second);
        root.Children.Add(panel);
        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        if (twoSheets)
        {
            context.AddSheet(new StyleSheet.Builder().Add(Selector.ForClass(a.Id), a.Set).Build());
            context.AddSheet(new StyleSheet.Builder().Add(Selector.ForClass(b.Id), b.Set).Build());
        }
        else
        {
            context.AddSheet(new StyleSheet.Builder()
                .Add(Selector.ForClass(a.Id), a.Set)
                .Add(Selector.ForClass(b.Id), b.Set)
                .Build());
        }

        panel.Inline.BackgroundColor = InlineColor;
        context.Update(0f);

        Assert.Equal(InlineColor, panel.Style.BackgroundColor);
        Assert.Equal(Length.Px(10f), panel.Style.Width);
        Assert.Equal(Length.Px(20f), panel.Style.Height);
    }

    [Fact]
    public void ADefaultViewThrows()
    {
        InlineStyle view = default;

        InvalidOperationException get = Assert.Throws<InvalidOperationException>(() => view.Width);
        Assert.Contains("Element.Inline", get.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => { view.BackgroundColor = Red; });
        Assert.Throws<InvalidOperationException>(() => view.IsSet(StyleProperty.Width));
        Assert.Throws<InvalidOperationException>(() => view.Clear(StyleProperty.Width));
    }

    [Fact]
    public void AValueThatIsNotAMemberThrows()
    {
        Element element = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => element.Inline.IsSet((StyleProperty)64));
        Assert.Throws<ArgumentOutOfRangeException>(() => element.Inline.Clear((StyleProperty)32));
    }

    [Fact]
    public void AnInlineLayoutChangeSetsLayoutAfterThePass()
    {
        (UiContext context, Element _, Element _, Element leaf) = BuildWarmChain(sheet: null);

        leaf.Inline.Width = 50f;
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.Layout, leaf.Dirty & (DirtyFlags.Layout | DirtyFlags.Paint));
        Assert.Equal(DirtyFlags.None, leaf.Dirty & (DirtyFlags.Style | DirtyFlags.DescendantStyle));
    }

    [Fact]
    public void AnInlinePaintChangeSetsPaintAndNotLayoutAfterThePass()
    {
        (UiContext context, Element _, Element _, Element leaf) = BuildWarmChain(sheet: null);

        leaf.Inline.BackgroundColor = Red;
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.Paint, leaf.Dirty & (DirtyFlags.Layout | DirtyFlags.Paint));
    }

    [Theory]
    [MemberData(nameof(EachMember))]
    public void EachMemberOfTheViewWritesItsOwnMember(StyleProperty member)
    {
        (UiContext context, Element _, Element _, Element leaf) = BuildWarmChain(sheet: null);
        DirtyFlags kind = (StyleMasks.Layout & (1UL << (int)member)) != 0 ? DirtyFlags.Layout : DirtyFlags.Paint;

        // The warm style holds the defaults and the inherited text of the context (opaque white, id 0, size 0).
        ComputedStyle warm = leaf.Style;

        // A set writes the bit of the member only, and marks Style.
        SetThroughTheView(leaf.Inline, member, alternate: false);
        Assert.Equal(1UL << (int)member, leaf.InlineStorage.SetMask);
        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        context.Update(0f);
        Assert.Equal(kind, StylePass.Compare(in warm, in leaf.Style));

        // A set to the same value marks nothing. A set to a new value marks Style.
        SetThroughTheView(leaf.Inline, member, alternate: false);
        Assert.Equal(DirtyFlags.None, leaf.Dirty);
        SetThroughTheView(leaf.Inline, member, alternate: true);
        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        context.Update(0f);

        // A clear gives back the default value, or the inherited value for a text member.
        leaf.Inline.Clear(member);
        Assert.Equal(DirtyFlags.Style, leaf.Dirty);
        Assert.Equal(0UL, leaf.InlineStorage.SetMask);
        context.Update(0f);
        Assert.Equal(DirtyFlags.None, StylePass.Compare(in warm, in leaf.Style));
    }

    [Fact]
    public void AWholeValueWriteChangesOneEdge()
    {
        Element element = new();
        element.Inline.Margin = LengthEdges.All(4f);

        element.Inline.Margin = element.Inline.Margin with { Top = 12f };

        LengthEdges margin = element.Inline.Margin;
        Assert.Equal(Length.Px(4f), margin.Left);
        Assert.Equal(Length.Px(12f), margin.Top);
        Assert.Equal(Length.Px(4f), margin.Right);
        Assert.Equal(Length.Px(4f), margin.Bottom);
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

    /// <summary>Gives the demo line: the background color as hex and the width as an integer.</summary>
    private static string Describe(Element element)
    {
        int width = (int)element.Style.Width.Value;
        return "BackgroundColor " + element.Style.BackgroundColor + " width " + width.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Builds a context with a chain of three elements and runs one <c>Update</c>, so each dirty bit is clear.
    /// </summary>
    private static (UiContext Context, Element Root, Element Middle, Element Leaf) BuildWarmChain(StyleSheet? sheet)
    {
        Element root = new();
        Element middle = new();
        Element leaf = new();
        root.Children.Add(middle);
        middle.Children.Add(leaf);
        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        if (sheet is not null)
        {
            context.AddSheet(sheet);
        }

        context.Update(0f);

        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, middle.Dirty);
        Assert.Equal(DirtyFlags.None, leaf.Dirty);
        return (context, root, middle, leaf);
    }

    /// <summary>
    /// Sets one member through the view to a value that is not the <see cref="ComputedStyle"/> default. The alternate
    /// value is a second value. <see cref="Display"/> and <see cref="PositionType"/> have two members only, so their
    /// alternate value is the default.
    /// </summary>
    private static void SetThroughTheView(InlineStyle view, StyleProperty member, bool alternate)
    {
        float number = alternate ? 2f : 1f;
        switch (member)
        {
            case StyleProperty.FlexDirection: view.FlexDirection = alternate ? FlexDirection.RowReverse : FlexDirection.Row; break;
            case StyleProperty.Width: view.Width = number; break;
            case StyleProperty.Height: view.Height = number; break;
            case StyleProperty.Margin: view.Margin = LengthEdges.All(number); break;
            case StyleProperty.Padding: view.Padding = LengthEdges.All(number); break;
            case StyleProperty.BorderWidth: view.BorderWidth = LengthEdges.All(number); break;
            case StyleProperty.FlexGrow: view.FlexGrow = number; break;
            case StyleProperty.FlexShrink: view.FlexShrink = number; break;
            case StyleProperty.FlexBasis: view.FlexBasis = number; break;
            case StyleProperty.MinWidth: view.MinWidth = number; break;
            case StyleProperty.MinHeight: view.MinHeight = number; break;
            case StyleProperty.MaxWidth: view.MaxWidth = number; break;
            case StyleProperty.MaxHeight: view.MaxHeight = number; break;
            case StyleProperty.JustifyContent: view.JustifyContent = alternate ? Justify.FlexEnd : Justify.Center; break;
            case StyleProperty.AlignItems: view.AlignItems = alternate ? Align.FlexEnd : Align.Center; break;
            case StyleProperty.AlignSelf: view.AlignSelf = alternate ? Align.FlexEnd : Align.Center; break;
            case StyleProperty.AlignContent: view.AlignContent = alternate ? Align.FlexEnd : Align.Center; break;
            case StyleProperty.FlexWrap: view.FlexWrap = alternate ? FlexWrap.WrapReverse : FlexWrap.Wrap; break;
            case StyleProperty.Display: view.Display = alternate ? default : Display.None; break;
            case StyleProperty.Position: view.Position = alternate ? default : PositionType.Absolute; break;
            case StyleProperty.Left: view.Left = number; break;
            case StyleProperty.Top: view.Top = number; break;
            case StyleProperty.Right: view.Right = number; break;
            case StyleProperty.Bottom: view.Bottom = number; break;
            case StyleProperty.Overflow: view.Overflow = alternate ? Overflow.Scroll : Overflow.Hidden; break;
            case StyleProperty.BackgroundColor: view.BackgroundColor = alternate ? Green : Red; break;
            case StyleProperty.BorderColor: view.BorderColor = alternate ? Green : Red; break;
            case StyleProperty.CornerRadius: view.CornerRadius = Corners.All(number); break;
            case StyleProperty.Opacity: view.Opacity = alternate ? 0.25f : 0.5f; break;
            case StyleProperty.Color: view.Color = alternate ? Green : Red; break;
            case StyleProperty.Font: view.Font = new FontHandle(alternate ? 2 : 1); break;
            case StyleProperty.FontSize: view.FontSize = alternate ? 16f : 12f; break;
            default: throw new ArgumentOutOfRangeException(nameof(member), member, "The test has no value for the member. Add a case.");
        }
    }
}
