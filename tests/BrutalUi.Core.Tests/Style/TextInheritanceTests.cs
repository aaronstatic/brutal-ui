using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the text members <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>, and
/// <see cref="ComputedStyle.FontSize"/>: the demo, the context defaults, the inheritance order, the child marks of a
/// text change (A3), and the dirty bits of a text change.
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
public sealed class TextInheritanceTests
{
    private static readonly Rgba8 White = new(255, 255, 255, 255);
    private static readonly Rgba8 Gold = new(255, 204, 0, 255);
    private static readonly Rgba8 Red = new(255, 0, 0, 255);
    private static readonly Rgba8 Green = new(0, 255, 0, 255);
    private static readonly Rgba8 Blue = new(0, 0, 255, 255);

    [Fact]
    public void TextColorFlowsDownTheTree()
    {
        ClassId panelClass = ClassName.Intern(nameof(TextColorFlowsDownTheTree) + ".panel");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), new StyleProperties { Color = Gold })
            .Build();

        Element root = new() { Name = "root" };
        Element panel = new() { Name = "panel" };
        Element child = new() { Name = "child" };
        Element badge = new() { Name = "badge" };
        panel.Classes.Add(panelClass);
        root.Children.Add(panel);
        panel.Children.Add(child);
        panel.Children.Add(badge);
        badge.Inline.Color = Red;

        // The create-info sets no default color, so the context uses opaque white.
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        string rootLine = Describe(root);
        string panelLine = Describe(panel);
        string childLine = Describe(child);
        string badgeLine = Describe(badge);
        TestContext.Current.TestOutputHelper?.WriteLine(rootLine);
        TestContext.Current.TestOutputHelper?.WriteLine(panelLine);
        TestContext.Current.TestOutputHelper?.WriteLine(childLine);
        TestContext.Current.TestOutputHelper?.WriteLine(badgeLine);

        panel.Classes.Remove(panelClass);
        context.Update(0f);

        string childAfterLine = Describe(child);
        TestContext.Current.TestOutputHelper?.WriteLine(childAfterLine);

        Assert.Equal("root #FFFFFFFF", rootLine);
        Assert.Equal("panel #FFCC00FF", panelLine);
        Assert.Equal("child #FFCC00FF", childLine);
        Assert.Equal("badge #FF0000FF", badgeLine);
        Assert.Equal("child #FFFFFFFF", childAfterLine);
        Assert.Equal(Red, badge.Style.Color);
        Assert.Equal(White, panel.Style.Color);
    }

    [Fact]
    public void ARootTakesTheContextDefaults()
    {
        Rgba8 color = new(10, 20, 30, 40);
        UiContext context = new(new UiContext.CreateInfo
        {
            Measurer = new RecordingRenderer(),
            DefaultColor = color,
            DefaultFont = new FontHandle(3),
            DefaultFontSize = 18f,
        })
        {
            ViewportSize = new float2(400f, 300f),
        };
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        context.AddRoot(root);

        context.Update(0f);

        Assert.Equal(color, root.Style.Color);
        Assert.Equal(new FontHandle(3), root.Style.Font);
        Assert.Equal(18f, root.Style.FontSize);
        Assert.Equal(color, child.Style.Color);
        Assert.Equal(new FontHandle(3), child.Style.Font);
        Assert.Equal(18f, child.Style.FontSize);
    }

    [Fact]
    public void TheParameterlessContextUsesWhite()
    {
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);

        // Before the first update, the style holds default(ComputedStyle): transparent black.
        Assert.Equal(default, child.Style.Color);

        context.Update(0f);

        Assert.Equal(White, root.Style.Color);
        Assert.Equal(new FontHandle(0), root.Style.Font);
        Assert.Equal(0f, root.Style.FontSize);
        Assert.Equal(White, child.Style.Color);
        Assert.Equal(new FontHandle(0), child.Style.Font);
        Assert.Equal(0f, child.Style.FontSize);
    }

    [Fact]
    public void ARuleWinsOverTheParent()
    {
        ClassId id = ClassName.Intern(nameof(ARuleWinsOverTheParent) + ".a");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(id), new StyleProperties { Color = Green, FontSize = 14f })
            .Build();
        (UiContext context, Element root, Element middle, Element leaf) = BuildChain(sheet);
        root.Inline.Color = Red;
        root.Inline.Font = new FontHandle(2);
        root.Inline.FontSize = 20f;
        middle.Classes.Add(id);

        context.Update(0f);

        Assert.Equal(Red, root.Style.Color);
        Assert.Equal(Green, middle.Style.Color);
        Assert.Equal(new FontHandle(2), middle.Style.Font);
        Assert.Equal(14f, middle.Style.FontSize);
        Assert.Equal(Green, leaf.Style.Color);
        Assert.Equal(new FontHandle(2), leaf.Style.Font);
        Assert.Equal(14f, leaf.Style.FontSize);
    }

    [Fact]
    public void AnInlineValueWinsOverARule()
    {
        ClassId id = ClassName.Intern(nameof(AnInlineValueWinsOverARule) + ".a");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(id), new StyleProperties { Color = Green })
            .Build();
        (UiContext context, Element root, Element middle, Element leaf) = BuildChain(sheet);
        root.Inline.Color = Red;
        middle.Classes.Add(id);
        middle.Inline.Color = Blue;

        context.Update(0f);

        Assert.Equal(Blue, middle.Style.Color);
        Assert.Equal(Blue, leaf.Style.Color);

        // A clear gives the rule value back, and the leaf follows it in the same update.
        middle.Inline.Clear(StyleProperty.Color);
        context.Update(0f);

        Assert.Equal(Green, middle.Style.Color);
        Assert.Equal(Green, leaf.Style.Color);
    }

    [Fact]
    public void AChainOfSixInherits()
    {
        Element[] chain = new Element[6];
        for (int i = 0; i < chain.Length; i++)
        {
            chain[i] = new Element();
            if (i > 0)
            {
                chain[i - 1].Children.Add(chain[i]);
            }
        }

        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(chain[0]);
        chain[0].Inline.Color = Gold;
        chain[0].Inline.Font = new FontHandle(4);
        chain[0].Inline.FontSize = 22f;

        context.Update(0f);

        for (int i = 0; i < chain.Length; i++)
        {
            Assert.Equal(Gold, chain[i].Style.Color);
            Assert.Equal(new FontHandle(4), chain[i].Style.Font);
            Assert.Equal(22f, chain[i].Style.FontSize);
        }

        // A change at the root reaches the sixth level in one update.
        chain[0].Inline.Color = Blue;
        context.Update(0f);

        for (int i = 0; i < chain.Length; i++)
        {
            Assert.Equal(Blue, chain[i].Style.Color);
            Assert.Equal(new FontHandle(4), chain[i].Style.Font);
        }
    }

    [Fact]
    public void AParentChangeReachesAGrandchild()
    {
        (UiContext context, Element _, Element parent, Element child, Element grandchild) = BuildWarmChainOfFour();

        parent.Inline.Color = Red;

        // A3: only the parent holds the self bit before the pass. The pass marks the child and the grandchild.
        Assert.Equal(DirtyFlags.Style, parent.Dirty);
        Assert.Equal(DirtyFlags.None, child.Dirty);
        Assert.Equal(DirtyFlags.None, grandchild.Dirty);

        context.UpdateStyles();

        Assert.Equal(Red, child.Style.Color);
        Assert.Equal(Red, grandchild.Style.Color);
        Assert.Equal(DirtyFlags.Paint | DirtyFlags.DescendantPaint, parent.Dirty);
        Assert.Equal(DirtyFlags.Paint | DirtyFlags.DescendantPaint, child.Dirty);
        Assert.Equal(DirtyFlags.Paint, grandchild.Dirty);

        // A font change and a size change also reach the grandchild.
        context.Update(0f);
        parent.Inline.Font = new FontHandle(5);
        parent.Inline.FontSize = 30f;
        context.Update(0f);

        Assert.Equal(new FontHandle(5), grandchild.Style.Font);
        Assert.Equal(30f, grandchild.Style.FontSize);
        Assert.Equal(DirtyFlags.None, grandchild.Dirty);
    }

    [Fact]
    public void AChildWithItsOwnValueKeepsIt()
    {
        (UiContext context, Element _, Element parent, Element child, Element grandchild) = BuildWarmChainOfFour();
        child.Inline.Color = Blue;
        context.Update(0f);
        Assert.Equal(Blue, grandchild.Style.Color);

        parent.Inline.Color = Red;
        context.UpdateStyles();

        // The pass restyles the child, finds no change, and marks no grandchild.
        Assert.Equal(Red, parent.Style.Color);
        Assert.Equal(Blue, child.Style.Color);
        Assert.Equal(Blue, grandchild.Style.Color);
        Assert.Equal(DirtyFlags.None, child.Dirty);
        Assert.Equal(DirtyFlags.None, grandchild.Dirty);

        // The child sets the color only, so a font change of the parent still reaches the grandchild.
        context.Update(0f);
        parent.Inline.Font = new FontHandle(7);
        context.Update(0f);

        Assert.Equal(new FontHandle(7), child.Style.Font);
        Assert.Equal(new FontHandle(7), grandchild.Style.Font);
        Assert.Equal(Blue, grandchild.Style.Color);
    }

    [Theory]
    [InlineData(StyleProperty.Color)]
    [InlineData(StyleProperty.Font)]
    [InlineData(StyleProperty.FontSize)]
    public void ATextChangeGivesPaint(StyleProperty member)
    {
        (UiContext context, Element root, Element plain) = BuildWarmPair();

        switch (member)
        {
            case StyleProperty.Color: plain.Inline.Color = Red; break;
            case StyleProperty.Font: plain.Inline.Font = new FontHandle(9); break;
            default: plain.Inline.FontSize = 11f; break;
        }

        context.UpdateStyles();

        Assert.Equal(DirtyFlags.Paint, plain.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, root.Dirty);
    }

    [Fact]
    public void TheSameTextValueGivesNoBit()
    {
        (UiContext context, Element root, Element plain) = BuildWarmPair();

        // The inline values equal the inherited values of the context: white, id 0, and size 0.
        plain.Inline.Color = White;
        plain.Inline.Font = new FontHandle(0);
        plain.Inline.FontSize = 0f;
        Assert.Equal(DirtyFlags.Style, plain.Dirty);

        context.UpdateStyles();

        Assert.Equal(DirtyFlags.None, plain.Dirty);
        Assert.Equal(DirtyFlags.None, root.Dirty);

        // A NaN size to a NaN size is no change (float.Equals).
        root.Inline.FontSize = float.NaN;
        context.Update(0f);
        Assert.True(float.IsNaN(root.Style.FontSize));
        root.MarkDirty(DirtyFlags.Style);
        context.UpdateStyles();

        Assert.Equal(DirtyFlags.None, root.Dirty);
    }

    [Fact]
    public void ANewChildTakesTheTextOfItsParent()
    {
        (UiContext context, Element root, Element middle, Element leaf) = BuildChain(sheet: null);
        root.Inline.Color = Gold;
        context.Update(0f);

        // The add marks the new child only. The pass enters the root, the middle, and the leaf through
        // DescendantStyle, so each of them gives its current style to the next level.
        Element added = new();
        leaf.Children.Add(added);
        context.Update(0f);

        Assert.Equal(Gold, added.Style.Color);

        // A move to a parent with another color gives the new color.
        Element other = new();
        other.Inline.Color = Blue;
        root.Children.Add(other);
        other.Children.Add(added);
        context.Update(0f);

        Assert.Equal(Blue, added.Style.Color);
        Assert.Equal(Gold, middle.Style.Color);
    }

    /// <summary>Gives the demo line: the name and the text color as hex.</summary>
    private static string Describe(Element element)
    {
        return element.Name + " " + element.Style.Color;
    }

    /// <summary>Builds a context with a chain of three elements. The method runs no update.</summary>
    private static (UiContext Context, Element Root, Element Middle, Element Leaf) BuildChain(StyleSheet? sheet)
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

        return (context, root, middle, leaf);
    }

    /// <summary>
    /// Builds a context with a root and one plain child, and runs one <c>Update</c>, so each dirty bit is clear.
    /// </summary>
    private static (UiContext Context, Element Root, Element Plain) BuildWarmPair()
    {
        Element root = new();
        Element plain = new();
        root.Children.Add(plain);
        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.Update(0f);

        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, plain.Dirty);
        return (context, root, plain);
    }

    /// <summary>
    /// Builds a context with the chain root, parent, child, and grandchild, and runs one <c>Update</c>, so each dirty
    /// bit is clear.
    /// </summary>
    private static (UiContext Context, Element Root, Element Parent, Element Child, Element Grandchild) BuildWarmChainOfFour()
    {
        Element root = new();
        Element parent = new();
        Element child = new();
        Element grandchild = new();
        root.Children.Add(parent);
        parent.Children.Add(child);
        child.Children.Add(grandchild);
        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.Update(0f);

        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, parent.Dirty);
        Assert.Equal(DirtyFlags.None, child.Dirty);
        Assert.Equal(DirtyFlags.None, grandchild.Dirty);
        Assert.Equal(White, grandchild.Style.Color);
        return (context, root, parent, child, grandchild);
    }
}
