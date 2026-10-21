using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of a context with more than one sheet: the demo, the order of the sheets (A4), the restyle after a
/// sheet change (A8), <see cref="UiContext.RemoveSheet(StyleSheet)"/>, and the full restyle of a new root.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel.
/// Each test uses class names with a unique prefix, and no test asserts a fixed id value.
/// </remarks>
public sealed class MultiSheetTests
{
    private static readonly Rgba8 Base = new(30, 34, 44, 255);
    private static readonly Rgba8 Theme = new(48, 96, 192, 255);
    private static readonly Rgba8 Red = new(255, 0, 0, 255);
    private static readonly Rgba8 Green = new(0, 255, 0, 255);
    private static readonly Rgba8 Blue = new(0, 0, 255, 255);
    private static readonly Rgba8 Gold = new(255, 204, 0, 255);

    /// <summary>The two style bits: the self bit and the descendant bit.</summary>
    private const DirtyFlags StyleBits = DirtyFlags.Style | DirtyFlags.DescendantStyle;

    /// <summary>The number of sheets in the cascade-order tests. More than 16 rules defeat the insertion sort.</summary>
    private const int SheetCount = 3;

    /// <summary>The number of rules in each sheet of the cascade-order tests.</summary>
    private const int RulesPerSheet = 100;

    [Fact]
    public void ALaterSheetWinsAndARemoveRestyles()
    {
        ClassId panelClass = ClassName.Intern(nameof(ALaterSheetWinsAndARemoveRestyles) + ".panel");
        StyleSheet baseSheet = Sheet(panelClass, new StyleProperties { BackgroundColor = Base });
        StyleSheet themeSheet = Sheet(panelClass, new StyleProperties { BackgroundColor = Theme });

        Element panel = new() { Name = "panel" };
        panel.Classes.Add(panelClass);
        UiContext context = NewContext();
        context.AddRoot(panel);
        context.AddSheet(baseSheet);
        context.AddSheet(themeSheet);
        context.Update(0f);

        string withTheme = Describe(panel);
        TestContext.Current.TestOutputHelper?.WriteLine(withTheme);

        Assert.True(context.RemoveSheet(themeSheet));
        context.Update(0f);

        string withoutTheme = Describe(panel);
        TestContext.Current.TestOutputHelper?.WriteLine(withoutTheme);

        Assert.Equal("panel #3060C0FF", withTheme);
        Assert.Equal("panel #1E222CFF", withoutTheme);
    }

    [Fact]
    public void AHigherSpecificityInTheFirstSheetWins()
    {
        ClassId panelClass = ClassName.Intern(nameof(AHigherSpecificityInTheFirstSheetWins) + ".panel");
        ElementTypeId type = new Element().TypeId;

        // The type and class rule has the specificity 11. The class rule has 10. The order of the sheets decides
        // only at equal specificity.
        StyleSheet strong = new StyleSheet.Builder()
            .Add(Selector.ForTypeAndClass(type, panelClass), new StyleProperties { BackgroundColor = Red })
            .Build();
        StyleSheet weak = Sheet(panelClass, new StyleProperties { BackgroundColor = Blue });

        Assert.Equal(Red, StyledBackground(panelClass, strong, weak));
        Assert.Equal(Red, StyledBackground(panelClass, weak, strong));
    }

    [Fact]
    public void ThreeSheetsMergeInTheOrderOfAdd()
    {
        ClassId panelClass = ClassName.Intern(nameof(ThreeSheetsMergeInTheOrderOfAdd) + ".panel");
        StyleSheet first = Sheet(
            panelClass,
            new StyleProperties { BackgroundColor = Red, BorderColor = Red, Color = Gold });
        StyleSheet second = Sheet(
            panelClass,
            new StyleProperties { BackgroundColor = Green, BorderColor = Green });
        StyleSheet third = Sheet(panelClass, new StyleProperties { BackgroundColor = Blue });

        Element panel = new();
        panel.Classes.Add(panelClass);
        UiContext context = NewContext();
        context.AddRoot(panel);
        context.AddSheet(first);
        context.AddSheet(second);
        context.AddSheet(third);
        context.Update(0f);

        // Each member comes from the last sheet that sets it.
        Assert.Equal(Blue, panel.Style.BackgroundColor);
        Assert.Equal(Green, panel.Style.BorderColor);
        Assert.Equal(Gold, panel.Style.Color);

        // A removal of the middle sheet keeps the order of the other two.
        Assert.True(context.RemoveSheet(second));
        context.Update(0f);

        Assert.Equal(Blue, panel.Style.BackgroundColor);
        Assert.Equal(Red, panel.Style.BorderColor);
        Assert.Equal(Gold, panel.Style.Color);
    }

    [Fact]
    public void RemoveSheetGivesFalseForAnAbsentSheet()
    {
        ClassId panelClass = ClassName.Intern(nameof(RemoveSheetGivesFalseForAnAbsentSheet) + ".panel");
        StyleSheet held = Sheet(panelClass, new StyleProperties { BackgroundColor = Red });
        StyleSheet absent = Sheet(panelClass, new StyleProperties { BackgroundColor = Blue });

        Element panel = new();
        panel.Classes.Add(panelClass);
        UiContext context = NewContext();
        context.AddRoot(panel);
        context.AddSheet(held);
        context.Update(0f);

        Assert.False(context.RemoveSheet(absent));
        Assert.Equal(DirtyFlags.None, panel.Dirty);
        Assert.Throws<ArgumentNullException>(() => context.RemoveSheet(null!));

        Assert.True(context.RemoveSheet(held));
        Assert.False(context.RemoveSheet(held));
        Assert.False(new UiContext().RemoveSheet(held));
    }

    [Fact]
    public void RemoveTheLastSheetKeepsTheInlineValues()
    {
        ClassId panelClass = ClassName.Intern(nameof(RemoveTheLastSheetKeepsTheInlineValues) + ".panel");
        StyleSheet sheet = Sheet(panelClass, new StyleProperties { BackgroundColor = Red, BorderColor = Green });

        Element panel = new();
        panel.Classes.Add(panelClass);
        panel.Inline.BackgroundColor = Blue;
        UiContext context = NewContext();
        context.AddRoot(panel);
        context.AddSheet(sheet);
        context.Update(0f);

        Assert.Equal(Blue, panel.Style.BackgroundColor);
        Assert.Equal(Green, panel.Style.BorderColor);

        Assert.True(context.RemoveSheet(sheet));
        context.Update(0f);

        // The pass runs with no sheet. The inline value stays, and the rule value goes back to the default.
        Assert.Equal(Blue, panel.Style.BackgroundColor);
        Assert.Equal(default, panel.Style.BorderColor);
    }

    [Fact]
    public void AddSheetAfterARemoveRestyles()
    {
        ClassId panelClass = ClassName.Intern(nameof(AddSheetAfterARemoveRestyles) + ".panel");
        StyleSheet first = Sheet(panelClass, new StyleProperties { BackgroundColor = Red });
        StyleSheet second = Sheet(panelClass, new StyleProperties { BackgroundColor = Green });

        Element panel = new();
        Element child = new();
        child.Classes.Add(panelClass);
        panel.Children.Add(child);
        UiContext context = NewContext();
        context.AddRoot(panel);
        context.AddSheet(first);
        context.Update(0f);
        Assert.Equal(Red, child.Style.BackgroundColor);

        Assert.True(context.RemoveSheet(first));
        context.Update(0f);
        Assert.Equal(default(Rgba8), child.Style.BackgroundColor);

        // The removed sheet can come back, and a new sheet takes its place in the order.
        context.AddSheet(second);
        context.Update(0f);
        Assert.Equal(Green, child.Style.BackgroundColor);

        context.AddSheet(first);
        context.Update(0f);
        Assert.Equal(Red, child.Style.BackgroundColor);
    }

    [Fact]
    public void ARootFromAnotherContextGetsTheNewSheets()
    {
        ClassId panelClass = ClassName.Intern(nameof(ARootFromAnotherContextGetsTheNewSheets) + ".panel");
        StyleSheet oldSheet = Sheet(panelClass, new StyleProperties { BackgroundColor = Red });
        StyleSheet newSheet = Sheet(panelClass, new StyleProperties { BackgroundColor = Blue });

        Element root = new();
        Element child = new();
        child.Classes.Add(panelClass);
        root.Children.Add(child);

        UiContext oldContext = NewContext();
        oldContext.AddRoot(root);
        oldContext.AddSheet(oldSheet);
        oldContext.Update(0f);
        Assert.Equal(Red, child.Style.BackgroundColor);
        Assert.Equal(DirtyFlags.None, root.Dirty);

        UiContext newContext = NewContext();
        newContext.AddSheet(newSheet);
        Assert.True(oldContext.RemoveRoot(root));
        newContext.AddRoot(root);

        // The add marks the full subtree, so the next update restyles the child.
        Assert.Equal(DirtyFlags.Style, root.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.Style, child.Dirty & DirtyFlags.Style);
        newContext.Update(0f);

        Assert.Equal(Blue, child.Style.BackgroundColor);
    }

    [Fact]
    public void ASubtreeThatMovesIntoAContextWithNoSheetTakesTheDefaults()
    {
        ClassId xClass = ClassName.Intern(nameof(ASubtreeThatMovesIntoAContextWithNoSheetTakesTheDefaults) + ".x");
        StyleSheet sheet = Sheet(xClass, new StyleProperties { BackgroundColor = Red });

        // The moved subtree has three levels. Each element has the class.
        Element oldRoot = new();
        Element moved = new();
        Element child = new();
        Element grandchild = new();
        moved.Classes.Add(xClass);
        child.Classes.Add(xClass);
        grandchild.Classes.Add(xClass);
        child.Children.Add(grandchild);
        moved.Children.Add(child);
        oldRoot.Children.Add(moved);

        UiContext oldContext = NewContext();
        oldContext.AddRoot(oldRoot);
        oldContext.AddSheet(sheet);
        oldContext.Update(0f);
        Assert.Equal(Red, moved.Style.BackgroundColor);
        Assert.Equal(Red, child.Style.BackgroundColor);
        Assert.Equal(Red, grandchild.Style.BackgroundColor);

        // The subtree moves under a root of a context with no sheet.
        Element newRoot = new();
        UiContext newContext = NewContext();
        newContext.AddRoot(newRoot);
        newContext.Update(0f);
        newRoot.Children.Add(moved);

        // The attach marks the full subtree.
        Assert.Equal(DirtyFlags.Style | DirtyFlags.DescendantStyle, moved.Dirty & StyleBits);
        Assert.Equal(DirtyFlags.Style | DirtyFlags.DescendantStyle, child.Dirty & StyleBits);
        Assert.Equal(DirtyFlags.Style, grandchild.Dirty & StyleBits);
        newContext.Update(0f);

        Assert.Equal(default(Rgba8), moved.Style.BackgroundColor);
        Assert.Equal(default(Rgba8), child.Style.BackgroundColor);
        Assert.Equal(default(Rgba8), grandchild.Style.BackgroundColor);
    }

    [Fact]
    public void ADetachedChildThatBecomesARootTakesTheContextDefaults()
    {
        Element parent = new();
        Element child = new();
        parent.Children.Add(child);
        parent.Inline.Color = Gold;

        UiContext first = NewContext();
        first.AddRoot(parent);
        first.Update(0f);
        Assert.Equal(Gold, child.Style.Color);

        // The child leaves its parent and becomes a root of a context with a green text default.
        UiContext second = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), DefaultColor = Green })
        {
            ViewportSize = new float2(800f, 600f),
        };
        second.AddRoot(child);
        second.Update(0f);

        Assert.Same(child, second.Roots[0]);
        Assert.Equal(Green, child.Style.Color);
    }

    [Fact]
    public void TheCascadeKeepsTheOrderOfSheetAndRuleAtEqualSpecificity()
    {
        ClassId panelClass = ClassName.Intern(nameof(TheCascadeKeepsTheOrderOfSheetAndRuleAtEqualSpecificity) + ".panel");
        UiContext context = NewContext();

        // Three sheets of 100 rules with one selector. The width of a rule is its index in the order of add.
        for (int s = 0; s < SheetCount; s++)
        {
            StyleSheet.Builder builder = new();
            for (int r = 0; r < RulesPerSheet; r++)
            {
                builder.Add(Selector.ForClass(panelClass), WidthOf((s * RulesPerSheet) + r));
            }

            context.AddSheet(builder.Build());
        }

        ReadOnlySpan<StyleRule> cascade = context.Cascade;

        Assert.Equal(SheetCount * RulesPerSheet, cascade.Length);
        for (int i = 0; i < cascade.Length; i++)
        {
            Assert.Equal(Length.Px(i), cascade[i].Properties.Width);
        }
    }

    [Fact]
    public void TheCascadeSortsBySpecificityThenBySheetThenByRule()
    {
        string prefix = nameof(TheCascadeSortsBySpecificityThenBySheetThenByRule);
        ClassId panelClass = ClassName.Intern(prefix + ".panel");
        ElementTypeId type = new Element().TypeId;
        UiContext context = NewContext();

        // The rule r of a sheet has the specificity 1 (type), 10 (class), or 11 (both) by r % 3. The width of a rule
        // is its index in the order of add.
        for (int s = 0; s < SheetCount; s++)
        {
            StyleSheet.Builder builder = new();
            for (int r = 0; r < RulesPerSheet; r++)
            {
                Selector selector = (r % 3) switch
                {
                    0 => Selector.ForType(type),
                    1 => Selector.ForClass(panelClass),
                    _ => Selector.ForTypeAndClass(type, panelClass),
                };
                builder.Add(selector, WidthOf((s * RulesPerSheet) + r));
            }

            context.AddSheet(builder.Build());
        }

        ReadOnlySpan<StyleRule> cascade = context.Cascade;

        Assert.Equal(SheetCount * RulesPerSheet, cascade.Length);
        int position = 0;
        for (int group = 0; group < 3; group++)
        {
            for (int s = 0; s < SheetCount; s++)
            {
                for (int r = group; r < RulesPerSheet; r += 3)
                {
                    Assert.Equal(Length.Px((s * RulesPerSheet) + r), cascade[position].Properties.Width);
                    position++;
                }
            }
        }

        Assert.Equal(cascade.Length, position);
    }

    /// <summary>Builds the properties of a rule with one width.</summary>
    private static StyleProperties WidthOf(int pixels)
    {
        return new StyleProperties { Width = Length.Px(pixels) };
    }

    /// <summary>Builds a sheet with one rule for a class.</summary>
    private static StyleSheet Sheet(ClassId id, in StyleProperties properties)
    {
        return new StyleSheet.Builder().Add(Selector.ForClass(id), in properties).Build();
    }

    /// <summary>Styles one element with the class and the sheets in the given order. Returns its background.</summary>
    private static Rgba8 StyledBackground(ClassId id, StyleSheet first, StyleSheet second)
    {
        Element element = new();
        element.Classes.Add(id);
        UiContext context = NewContext();
        context.AddRoot(element);
        context.AddSheet(first);
        context.AddSheet(second);
        context.Update(0f);
        return element.Style.BackgroundColor;
    }

    /// <summary>Gives the demo line: the name and the background color as hex.</summary>
    private static string Describe(Element element)
    {
        return element.Name + " " + element.Style.BackgroundColor;
    }

    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
    }
}
