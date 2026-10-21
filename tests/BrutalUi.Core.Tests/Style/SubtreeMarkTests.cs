using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the subtree mark (A2): a class change and a state change mark the changed element and its full
/// subtree for <see cref="DirtyFlags.Style"/>, so a chain rule of a descendant sees the change of an ancestor.
/// </summary>
/// <remarks>
/// <para>
/// The tree has a depth of 4 (root, panel, row, cell) and a second branch (root, side). Each test reads
/// <see cref="Element.Dirty"/> before the next update, or after <see cref="UiContext.UpdateStyles"/> alone, so the
/// layout pass does not clear the bits first.
/// </para>
/// <para>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class names with its own
/// name as the prefix, and no test asserts a fixed id value.
/// </para>
/// </remarks>
public sealed class SubtreeMarkTests
{
    private const DirtyFlags StyleBits = DirtyFlags.Style | DirtyFlags.DescendantStyle;

    private static readonly Rgba8 RowHover = new(48, 96, 192, 255);
    private static readonly Rgba8 CellHover = new(255, 204, 0, 255);

    [Fact]
    public void AHoverOnAPanelRestylesItsRowAndItsCell()
    {
        Scene scene = Scene.Build(nameof(AHoverOnAPanelRestylesItsRowAndItsCell));
        CompoundSelector panelHover = new() { Class = scene.PanelClass, States = PseudoStates.Hover };
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.Child(panelHover, new CompoundSelector { Class = scene.RowClass }), new StyleProperties { BackgroundColor = RowHover })
            .Add(Selector.Descendant(panelHover, new CompoundSelector { Class = scene.CellClass }), new StyleProperties { BackgroundColor = CellHover })
            .Build();
        scene.Context.AddSheet(sheet);

        scene.Context.Update(0f);
        string idleLine = Describe(scene);

        scene.Panel.States = PseudoStates.Hover;
        scene.Context.Update(0f);
        string hoverLine = Describe(scene);

        scene.Panel.States = PseudoStates.None;
        scene.Context.Update(0f);
        string backLine = Describe(scene);

        TestContext.Current.TestOutputHelper?.WriteLine(idleLine);
        TestContext.Current.TestOutputHelper?.WriteLine(hoverLine);
        TestContext.Current.TestOutputHelper?.WriteLine(backLine);

        Assert.Equal("row #00000000 cell #00000000", idleLine);
        Assert.Equal("row #3060C0FF cell #FFCC00FF", hoverLine);
        Assert.Equal("row #00000000 cell #00000000", backLine);

        // The side branch is not below the panel, so no rule styles it.
        Assert.Equal(Rgba8.Transparent, scene.Side.Style.BackgroundColor);
    }

    [Fact]
    public void AClassAddMarksTheSubtreeAndTheAncestors()
    {
        Scene scene = Scene.BuildClean(nameof(AClassAddMarksTheSubtreeAndTheAncestors));

        Assert.True(scene.Panel.Classes.Add(scene.ExtraClass));

        AssertPanelSubtreeMark(scene);
    }

    [Fact]
    public void AClassRemoveMarksTheSubtree()
    {
        Scene scene = Scene.BuildClean(nameof(AClassRemoveMarksTheSubtree));

        Assert.True(scene.Panel.Classes.Remove(scene.PanelClass));

        AssertPanelSubtreeMark(scene);
    }

    [Fact]
    public void AStateChangeMarksTheSubtree()
    {
        Scene scene = Scene.BuildClean(nameof(AStateChangeMarksTheSubtree));

        scene.Panel.States = PseudoStates.Hover;

        AssertPanelSubtreeMark(scene);

        // The change back is also a change, so it marks the same bits.
        scene.Context.Update(0f);
        AssertAllClean(scene);
        scene.Panel.States = PseudoStates.None;

        AssertPanelSubtreeMark(scene);
    }

    [Fact]
    public void TheSameStateAndADuplicateClassMarkNothing()
    {
        Scene scene = Scene.BuildClean(nameof(TheSameStateAndADuplicateClassMarkNothing));
        ClassId absent = ClassName.Intern(nameof(TheSameStateAndADuplicateClassMarkNothing) + ".absent");

        scene.Panel.States = PseudoStates.None;
        Assert.False(scene.Panel.Classes.Add(scene.PanelClass));
        Assert.False(scene.Panel.Classes.Remove(absent));
        AssertAllClean(scene);

        // A set to a held state that is not None also marks nothing.
        scene.Panel.States = PseudoStates.Hover | PseudoStates.Focus;
        scene.Context.Update(0f);
        AssertAllClean(scene);
        scene.Panel.States = PseudoStates.Hover | PseudoStates.Focus;
        AssertAllClean(scene);
    }

    [Fact]
    public void ALeafChangeMarksTheLeafOnly()
    {
        Scene scene = Scene.BuildClean(nameof(ALeafChangeMarksTheLeafOnly));

        scene.Cell.States = PseudoStates.Active;
        AssertLeafMark(scene);

        // A class change on the leaf gives the same bits.
        scene.Context.Update(0f);
        AssertAllClean(scene);
        Assert.True(scene.Cell.Classes.Add(scene.ExtraClass));
        AssertLeafMark(scene);
    }

    [Fact]
    public void TheMarkKeepsTheInvariant()
    {
        Scene scene = Scene.BuildClean(nameof(TheMarkKeepsTheInvariant));

        scene.Panel.States = PseudoStates.Hover;
        Assert.Equal(5, AssertInvariant(scene.Root));

        scene.Row.Classes.Add(scene.ExtraClass);
        Assert.Equal(5, AssertInvariant(scene.Root));

        scene.Cell.States = PseudoStates.Checked;
        Assert.Equal(5, AssertInvariant(scene.Root));

        scene.Side.Classes.Add(scene.ExtraClass);
        Assert.Equal(5, AssertInvariant(scene.Root));

        scene.Root.States = PseudoStates.Focus;
        Assert.Equal(5, AssertInvariant(scene.Root));

        // A mark on a tree with other bits also keeps the invariant.
        scene.Context.Update(0f);
        AssertAllClean(scene);
        scene.Cell.MarkDirty(DirtyFlags.Paint);
        scene.Row.Classes.Remove(scene.RowClass);
        Assert.Equal(5, AssertInvariant(scene.Root));
        Assert.Equal(DirtyFlags.Style | DirtyFlags.Paint, scene.Cell.Dirty);
        Assert.Equal(StyleBits | DirtyFlags.DescendantPaint, scene.Row.Dirty);
    }

    [Fact]
    public void TheNextUpdateClearsEachStyleBit()
    {
        Scene scene = Scene.BuildClean(nameof(TheNextUpdateClearsEachStyleBit));

        scene.Panel.States = PseudoStates.Hover;
        scene.Side.Classes.Add(scene.ExtraClass);
        scene.Cell.Classes.Remove(scene.CellClass);
        scene.Context.UpdateStyles();

        Element[] elements = [scene.Root, scene.Panel, scene.Row, scene.Cell, scene.Side];
        foreach (Element element in elements)
        {
            Assert.True(
                (element.Dirty & StyleBits) == DirtyFlags.None,
                $"{element.Name} holds {element.Dirty} after the style pass.");
        }
    }

    [Fact]
    public void AChainMatchAndAStateChangeAllocateNothing()
    {
        ChainScene scene = ChainScene.Build();

        // Warm-up: the steps let the JIT compile the mark, the match, and the pass.
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
        Assert.Equal(50, scene.Elements.Length);

        // The steps reached the chain rules: the match is visible in the last state of the tree.
        scene.AssertTheRulesMatch();
    }

    /// <summary>Checks the bits of a mark on the panel. The scene was clean before the mark.</summary>
    private static void AssertPanelSubtreeMark(Scene scene)
    {
        Assert.Equal(StyleBits, scene.Panel.Dirty);
        Assert.Equal(StyleBits, scene.Row.Dirty);
        Assert.Equal(DirtyFlags.Style, scene.Cell.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Root.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Side.Dirty);
    }

    /// <summary>Checks the bits of a mark on the cell. The scene was clean before the mark.</summary>
    private static void AssertLeafMark(Scene scene)
    {
        Assert.Equal(DirtyFlags.Style, scene.Cell.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Row.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Panel.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Root.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Side.Dirty);
    }

    private static void AssertAllClean(Scene scene)
    {
        Assert.Equal(DirtyFlags.None, scene.Root.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Panel.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Row.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Cell.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Side.Dirty);
    }

    /// <summary>
    /// Checks the invariant on the subtree of an element and returns the number of elements in the subtree. When an
    /// element holds a self bit or a descendant bit of a kind, each ancestor holds the descendant bit of that kind.
    /// </summary>
    private static int AssertInvariant(Element element)
    {
        DirtyFlags needed = DirtyBits.ToDescendant(element.Dirty & DirtyBits.SelfMask) | (element.Dirty & DirtyBits.DescendantMask);
        for (Element? ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            Assert.True(
                (ancestor.Dirty & needed) == needed,
                $"{ancestor.Name} ({ancestor.Dirty}) lacks the bits {needed} of {element.Name} ({element.Dirty}).");
        }

        int count = 1;
        foreach (Element child in element.Children)
        {
            count += AssertInvariant(child);
        }

        return count;
    }

    /// <summary>Gives the name and the background color of the row and of the cell.</summary>
    private static string Describe(Scene scene)
    {
        return scene.Row.Name + " " + scene.Row.Style.BackgroundColor + " " + scene.Cell.Name + " " + scene.Cell.Style.BackgroundColor;
    }

    /// <summary>
    /// The tree of the mark tests: root, panel, row, and cell in one branch, and root and side in a second branch.
    /// The panel, the row, and the cell each hold a class. The context has no sheet.
    /// </summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required Element Root { get; init; }

        public required Element Panel { get; init; }

        public required Element Row { get; init; }

        public required Element Cell { get; init; }

        public required Element Side { get; init; }

        public required ClassId PanelClass { get; init; }

        public required ClassId RowClass { get; init; }

        public required ClassId CellClass { get; init; }

        public required ClassId ExtraClass { get; init; }

        /// <summary>Builds the tree and adds the root to a context. The call runs no update.</summary>
        public static Scene Build(string prefix)
        {
            ClassId panelClass = ClassName.Intern(prefix + ".panel");
            ClassId rowClass = ClassName.Intern(prefix + ".row");
            ClassId cellClass = ClassName.Intern(prefix + ".cell");
            ClassId extraClass = ClassName.Intern(prefix + ".extra");

            Element root = new() { Name = "root" };
            Element panel = new() { Name = "panel" };
            Element row = new() { Name = "row" };
            Element cell = new() { Name = "cell" };
            Element side = new() { Name = "side" };
            panel.Classes.Add(panelClass);
            row.Classes.Add(rowClass);
            cell.Classes.Add(cellClass);
            root.Children.Add(panel);
            panel.Children.Add(row);
            row.Children.Add(cell);
            root.Children.Add(side);

            UiContext context = new() { ViewportSize = new float2(400f, 300f) };
            context.AddRoot(root);

            return new Scene
            {
                Context = context,
                Root = root,
                Panel = panel,
                Row = row,
                Cell = cell,
                Side = side,
                PanelClass = panelClass,
                RowClass = rowClass,
                CellClass = cellClass,
                ExtraClass = extraClass,
            };
        }

        /// <summary>Builds the tree, runs one update, and checks that each dirty bit is clear.</summary>
        public static Scene BuildClean(string prefix)
        {
            Scene scene = Build(prefix);
            scene.Context.Update(0f);
            AssertAllClean(scene);
            return scene;
        }
    }

    /// <summary>
    /// The tree of the allocation gate: a root, 7 panels, and 6 items under each panel (50 elements), and a sheet with
    /// a state rule, a child rule, and a descendant rule. Each step toggles the hover of one panel and one class of one
    /// item, and runs one <c>Update</c>.
    /// </summary>
    private sealed class ChainScene
    {
        private const int PanelCount = 7;
        private const int ItemCount = 6;

        private static readonly Rgba8 PanelHover = new(10, 20, 30, 255);
        private static readonly Rgba8 ItemUnderHover = new(40, 50, 60, 255);

        public required UiContext Context { get; init; }

        public required Element[] Elements { get; init; }

        public required Element[] Panels { get; init; }

        public required Element[] Items { get; init; }

        public required ClassId MarkClass { get; init; }

        public int StepCount { get; private set; }

        public static ChainScene Build()
        {
            string prefix = nameof(AChainMatchAndAStateChangeAllocateNothing);
            ClassId rootClass = ClassName.Intern(prefix + ".root");
            ClassId panelClass = ClassName.Intern(prefix + ".panel");
            ClassId itemClass = ClassName.Intern(prefix + ".item");
            ClassId markClass = ClassName.Intern(prefix + ".mark");

            CompoundSelector panelHover = new() { Class = panelClass, States = PseudoStates.Hover };
            StyleSheet sheet = new StyleSheet.Builder()
                .Add(Selector.For(panelHover), new StyleProperties { BackgroundColor = PanelHover })
                .Add(Selector.Child(panelHover, new CompoundSelector { Class = itemClass }), new StyleProperties { BackgroundColor = ItemUnderHover })
                .Add(Selector.Descendant(new CompoundSelector { Class = rootClass }, new CompoundSelector { Class = markClass }), new StyleProperties { Opacity = 0.5f })
                .Build();

            Element[] elements = new Element[1 + PanelCount + (PanelCount * ItemCount)];
            Element[] panels = new Element[PanelCount];
            Element[] items = new Element[PanelCount * ItemCount];
            Element root = new();
            root.Classes.Add(rootClass);
            elements[0] = root;
            int index = 1;
            for (int p = 0; p < PanelCount; p++)
            {
                Element panel = new();
                panel.Classes.Add(panelClass);
                root.Children.Add(panel);
                panels[p] = panel;
                elements[index++] = panel;
                for (int i = 0; i < ItemCount; i++)
                {
                    Element item = new();
                    item.Classes.Add(itemClass);
                    panel.Children.Add(item);
                    items[(p * ItemCount) + i] = item;
                    elements[index++] = item;
                }
            }

            UiContext context = new() { ViewportSize = new float2(1280f, 720f) };
            context.AddRoot(root);
            context.AddSheet(sheet);
            context.Update(0f);

            return new ChainScene { Context = context, Elements = elements, Panels = panels, Items = items, MarkClass = markClass };
        }

        /// <summary>The measured body. It uses no lambda, no boxing, no string, and no Assert.</summary>
        public void Step()
        {
            int step = StepCount++;
            Element panel = Panels[step % Panels.Length];
            panel.States = panel.States == PseudoStates.None ? PseudoStates.Hover : PseudoStates.None;

            ClassList list = Items[step % Items.Length].Classes;
            if (!list.Remove(MarkClass))
            {
                list.Add(MarkClass);
            }

            Context.Update(0f);
        }

        /// <summary>Checks that the style of each panel and of each item follows its state and its classes.</summary>
        public void AssertTheRulesMatch()
        {
            for (int p = 0; p < Panels.Length; p++)
            {
                Element panel = Panels[p];
                bool hover = panel.States == PseudoStates.Hover;
                Assert.Equal(hover ? PanelHover : Rgba8.Transparent, panel.Style.BackgroundColor);
                for (int i = 0; i < ItemCount; i++)
                {
                    Element item = Items[(p * ItemCount) + i];
                    Assert.Equal(hover ? ItemUnderHover : Rgba8.Transparent, item.Style.BackgroundColor);
                    Assert.Equal(item.Classes.Contains(MarkClass) ? 0.5f : 1f, item.Style.Opacity);
                }
            }
        }
    }
}
