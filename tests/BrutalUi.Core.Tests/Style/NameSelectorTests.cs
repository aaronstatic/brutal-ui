using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the name part: the ordinal match, a name in a chain, the subtree mark on a name change, the full order
/// of the specificity (name, then class and state, then type), the order of add at equal specificity, and the factory
/// rules for an empty name.
/// </summary>
/// <remarks>
/// <para>
/// The class table and the type table are global and xUnit runs test classes in parallel. Each test interns its class
/// names with its own name as the prefix. Names are plain strings and need no prefix.
/// </para>
/// <para>
/// Each rule test checks the direct match and the result of the style pass, and both must agree.
/// </para>
/// </remarks>
public sealed class NameSelectorTests
{
    private const PseudoStates AllStates =
        PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus | PseudoStates.Disabled | PseudoStates.Checked;

    private const DirtyFlags StyleBits = DirtyFlags.Style | DirtyFlags.DescendantStyle;

    private static readonly Rgba8 Green = new(0, 255, 0, 255);
    private static readonly Rgba8 Blue = new(48, 96, 192, 255);
    private static readonly Rgba8 Red = new(255, 0, 0, 255);

    [Fact]
    public void ANameRuleWinsOverAClassAndAState()
    {
        ClassId panel = ClassName.Intern(nameof(ANameRuleWinsOverAClassAndAState) + ".panel");
        Element element = new() { Name = "title", States = PseudoStates.Hover };
        element.Classes.Add(panel);

        // The name rule comes first, and the class and state rule comes later.
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForName("title"), new StyleProperties { BackgroundColor = Green })
            .Add(Selector.For(new CompoundSelector { Class = panel, States = PseudoStates.Hover }), new StyleProperties { BackgroundColor = Blue })
            .Build();
        UiContext context = NewContext();
        context.AddRoot(element);
        context.AddSheet(sheet);

        context.Update(0f);
        string firstLine = element.Name + " " + element.Style.BackgroundColor;

        element.Name = "subtitle";
        context.Update(0f);
        string secondLine = element.Name + " " + element.Style.BackgroundColor;

        TestContext.Current.TestOutputHelper?.WriteLine(firstLine);
        TestContext.Current.TestOutputHelper?.WriteLine(secondLine);

        Assert.Equal("title #00FF00FF", firstLine);
        Assert.Equal("subtitle #3060C0FF", secondLine);
    }

    [Fact]
    public void ANameRuleMatchesOnlyThatName()
    {
        Selector selector = Selector.ForName("title");

        Element exact = new() { Name = "title" };
        Element upper = new() { Name = "Title" };
        Element other = new() { Name = "subtitle" };
        Element unnamed = new();

        // An equal name in another string instance also matches (the compare is by value).
        Element copy = new() { Name = string.Concat("ti", "tle") };

        Assert.True(MatchesDirectAndThroughThePass(selector, exact));
        Assert.True(MatchesDirectAndThroughThePass(selector, copy));
        Assert.False(MatchesDirectAndThroughThePass(selector, upper));
        Assert.False(MatchesDirectAndThroughThePass(selector, other));
        Assert.False(MatchesDirectAndThroughThePass(selector, unnamed));
    }

    [Fact]
    public void ANameWorksInACompound()
    {
        ClassId a = ClassName.Intern(nameof(ANameWorksInACompound) + ".a");
        Selector selector = Selector.For(new CompoundSelector { Name = "title", Class = a, States = PseudoStates.Hover });

        Element all = new() { Name = "title", States = PseudoStates.Hover };
        all.Classes.Add(a);
        Element noName = new() { States = PseudoStates.Hover };
        noName.Classes.Add(a);
        Element noClass = new() { Name = "title", States = PseudoStates.Hover };
        Element noState = new() { Name = "title" };
        noState.Classes.Add(a);

        Assert.True(MatchesDirectAndThroughThePass(selector, all));
        Assert.False(MatchesDirectAndThroughThePass(selector, noName));
        Assert.False(MatchesDirectAndThroughThePass(selector, noClass));
        Assert.False(MatchesDirectAndThroughThePass(selector, noState));
        Assert.Equal("title", selector.Subject.Name);
        Assert.Equal((1 << 16) | (2 << 8), selector.Specificity);
    }

    [Fact]
    public void ANameInAChain()
    {
        ClassId row = ClassName.Intern(nameof(ANameInAChain) + ".row");
        CompoundSelector hud = new() { Name = "hud" };
        CompoundSelector rowPart = new() { Class = row };
        Selector child = Selector.Child(hud, rowPart);
        Selector descendant = Selector.Descendant(hud, rowPart);

        // hud > mid > deep, and hud > near. Each of deep and near holds the class row.
        Element root = new() { Name = "hud" };
        Element mid = new() { Name = "mid" };
        Element deep = new() { Name = "deep" };
        Element near = new() { Name = "near" };
        deep.Classes.Add(row);
        near.Classes.Add(row);
        root.Children.Add(mid);
        mid.Children.Add(deep);
        root.Children.Add(near);

        // The element with the class row, but with another parent name, matches neither chain.
        Element otherRoot = new() { Name = "Hud" };
        Element otherRow = new();
        otherRow.Classes.Add(row);
        otherRoot.Children.Add(otherRow);

        Assert.True(MatchesDirectAndThroughThePass(child, near, root));
        Assert.False(MatchesDirectAndThroughThePass(child, deep, root));
        Assert.True(MatchesDirectAndThroughThePass(descendant, near, root));
        Assert.True(MatchesDirectAndThroughThePass(descendant, deep, root));
        Assert.False(MatchesDirectAndThroughThePass(child, otherRow, otherRoot));
        Assert.False(MatchesDirectAndThroughThePass(descendant, otherRow, otherRoot));

        // The name of the subject also works in a chain.
        Selector named = Selector.Child(rowPart, new CompoundSelector { Name = "cell" });
        Element rowElement = new();
        rowElement.Classes.Add(row);
        Element cell = new() { Name = "cell" };
        Element notCell = new() { Name = "Cell" };
        rowElement.Children.Add(cell);
        rowElement.Children.Add(notCell);
        Assert.True(MatchesDirectAndThroughThePass(named, cell, rowElement));
        Assert.False(MatchesDirectAndThroughThePass(named, notCell, rowElement));

        Assert.Equal((1 << 16) | (1 << 8), child.Specificity);
        Assert.Equal(child.Specificity, descendant.Specificity);
        Assert.Equal((1 << 16) | (1 << 8), named.Specificity);
    }

    [Fact]
    public void ANameChangeMarksTheSubtree()
    {
        Scene scene = Scene.BuildClean();

        scene.Panel.Name = "renamed";

        // The panel and the row hold the self bit, the descendant bit, and the leaf holds the self bit only.
        Assert.Equal(StyleBits, scene.Panel.Dirty);
        Assert.Equal(StyleBits, scene.Row.Dirty);
        Assert.Equal(DirtyFlags.Style, scene.Cell.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Root.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Side.Dirty);

        // The next style pass clears each style bit.
        scene.Context.UpdateStyles();
        Element[] elements = [scene.Root, scene.Panel, scene.Row, scene.Cell, scene.Side];
        foreach (Element element in elements)
        {
            Assert.True(
                (element.Dirty & StyleBits) == DirtyFlags.None,
                $"{element.Name} holds {element.Dirty} after the style pass.");
        }

        // A change of a leaf name marks the leaf and the ancestors.
        scene.Context.Update(0f);
        scene.Cell.Name = "leaf";
        Assert.Equal(DirtyFlags.Style, scene.Cell.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Row.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Panel.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, scene.Root.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Side.Dirty);
    }

    [Fact]
    public void ANameChangeRestylesAChainRuleOfADescendant()
    {
        Scene scene = Scene.BuildClean();
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.Descendant(new CompoundSelector { Name = "hud" }, new CompoundSelector { Name = "cell" }), new StyleProperties { BackgroundColor = Red })
            .Build();
        scene.Context.AddSheet(sheet);
        scene.Context.Update(0f);
        Assert.Equal(Rgba8.Transparent, scene.Cell.Style.BackgroundColor);

        // The rule reads the name of the grandparent, so the cell restyles.
        scene.Row.Name = "row";
        scene.Panel.Name = "hud";
        scene.Context.Update(0f);
        Assert.Equal(Red, scene.Cell.Style.BackgroundColor);

        scene.Panel.Name = "panel";
        scene.Context.Update(0f);
        Assert.Equal(Rgba8.Transparent, scene.Cell.Style.BackgroundColor);
    }

    [Fact]
    public void TheSameNameMarksNothing()
    {
        Scene scene = Scene.BuildClean();

        scene.Panel.Name = "panel";
        scene.Panel.Name = string.Concat("pa", "nel");
        scene.Side.Name = "side";

        Assert.Equal(DirtyFlags.None, scene.Root.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Panel.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Row.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Cell.Dirty);
        Assert.Equal(DirtyFlags.None, scene.Side.Dirty);

        // A set of null on a null name also marks nothing.
        Element bare = new();
        scene.Context.AddRoot(bare);
        scene.Context.Update(0f);
        bare.Name = null;
        Assert.Equal(DirtyFlags.None, bare.Dirty);

        // A different case is a different name.
        scene.Panel.Name = "Panel";
        Assert.Equal(StyleBits, scene.Panel.Dirty);
    }

    [Fact]
    public void TheSpecificityOrderIsNameThenClassThenType()
    {
        ClassId a = ClassName.Intern(nameof(TheSpecificityOrderIsNameThenClassThenType) + ".a");
        ElementTypeId plainType = new Element().TypeId;

        // One name (65536) beats the largest chain without a name: 12 units of the class tier (3072).
        CompoundSelector full = new() { Class = a, States = AllStates };
        Selector twelve = Selector.Child(full, full);
        Selector name = Selector.ForName("title");
        Assert.Equal(12 << 8, twelve.Specificity);
        Assert.Equal(1 << 16, name.Specificity);
        Assert.True(name.Specificity > twelve.Specificity);

        // One class (256) beats Label > Label (2 types).
        CompoundSelector type = new() { Type = plainType };
        Selector twoTypes = Selector.Child(type, type);
        Selector oneClass = Selector.ForClass(a);
        Assert.Equal(2, twoTypes.Specificity);
        Assert.Equal(256, oneClass.Specificity);
        Assert.True(oneClass.Specificity > twoTypes.Specificity);

        // The pairs hold in both orders of add, in the sheet and in the style pass.
        AssertWinner(name, twelve, a, plainType);
        AssertWinner(oneClass, twoTypes, a, plainType);
        AssertWinner(name, oneClass, a, plainType);
    }

    [Fact]
    public void ALaterRuleWinsAtEqualSpecificity()
    {
        ClassId a = ClassName.Intern(nameof(ALaterRuleWinsAtEqualSpecificity) + ".a");
        Selector classRule = Selector.ForClass(a);
        Selector stateRule = Selector.For(new CompoundSelector { States = PseudoStates.Hover });
        Assert.Equal(256, classRule.Specificity);
        Assert.Equal(256, stateRule.Specificity);

        Assert.Equal(Blue, StyleOf(classRule, Green, stateRule, Blue, a));
        Assert.Equal(Green, StyleOf(stateRule, Blue, classRule, Green, a));
    }

    [Fact]
    public void ForNameThrowsForNullOrEmpty()
    {
        Assert.Throws<ArgumentException>("name", () => Selector.ForName(null!));
        Assert.Throws<ArgumentException>("name", () => Selector.ForName(string.Empty));
    }

    [Fact]
    public void ForNameGivesTheNameAndTheNameTier()
    {
        Selector selector = Selector.ForName("title");

        Assert.Equal("title", selector.Subject.Name);
        Assert.Equal(SelectorCombinator.None, selector.Combinator);
        Assert.Equal(65536, selector.Specificity);
        Assert.False(selector.IsEmpty);

        // The builder accepts it, and the sheet keeps it.
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(selector, new StyleProperties { BackgroundColor = Green })
            .Build();
        Assert.Equal(1, sheet.Rules.Length);
        Assert.Equal(65536, sheet.Rules[0].Specificity);
    }

    [Fact]
    public void ForThrowsForAnEmptyName()
    {
        ClassId a = ClassName.Intern(nameof(ForThrowsForAnEmptyName) + ".a");
        CompoundSelector emptyName = new() { Name = string.Empty };
        CompoundSelector emptyNameWithClass = new() { Name = string.Empty, Class = a };
        CompoundSelector good = new() { Class = a };

        Assert.Throws<ArgumentException>("subject", () => Selector.For(emptyName));
        Assert.Throws<ArgumentException>("subject", () => Selector.For(emptyNameWithClass));
        Assert.Throws<ArgumentException>("parent", () => Selector.Child(emptyName, good));
        Assert.Throws<ArgumentException>("subject", () => Selector.Child(good, emptyName));
        Assert.Throws<ArgumentException>("ancestor", () => Selector.Descendant(emptyName, good));
        Assert.Throws<ArgumentException>("subject", () => Selector.Descendant(good, emptyName));

        // A null name is no name part, so a compound with only that is empty and still throws.
        Assert.Throws<ArgumentException>("subject", () => Selector.For(new CompoundSelector { Name = null }));
    }

    /// <summary>Checks the selector on an element two times: with the direct match and through the style pass.</summary>
    private static bool MatchesDirectAndThroughThePass(in Selector selector, Element element)
    {
        return MatchesDirectAndThroughThePass(selector, element, element);
    }

    /// <summary>
    /// Checks the selector on the subject two times: one time with the direct match, and one time through a sheet and
    /// the style pass. The background color does not inherit, so only a matching rule sets it.
    /// </summary>
    private static bool MatchesDirectAndThroughThePass(in Selector selector, Element subject, Element root)
    {
        bool direct = selector.Matches(subject);

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(selector, new StyleProperties { BackgroundColor = Red })
            .Build();
        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);
        bool styled = subject.Style.BackgroundColor == Red;

        // The caller can check another selector on the same tree.
        context.RemoveRoot(root);

        Assert.Equal(direct, styled);
        return direct;
    }

    /// <summary>
    /// Checks that the stronger rule wins in both orders of add. The element holds the name <c>title</c>, the class,
    /// all the states, and the plain type, and so does its parent. Thus each rule of the pairs matches the element.
    /// </summary>
    private static void AssertWinner(in Selector strong, in Selector weak, ClassId a, ElementTypeId plainType)
    {
        Assert.True(strong.Specificity > weak.Specificity);
        Assert.Equal(Green, StyleOfPair(strong, Green, weak, Blue, a, plainType));
        Assert.Equal(Green, StyleOfPair(weak, Blue, strong, Green, a, plainType));
    }

    private static Rgba8 StyleOfPair(in Selector first, Rgba8 firstColor, in Selector second, Rgba8 secondColor, ClassId a, ElementTypeId plainType)
    {
        Assert.False(plainType.IsNone);

        Element parent = new() { Name = "hud", States = AllStates };
        parent.Classes.Add(a);
        Element element = new() { Name = "title", States = AllStates };
        element.Classes.Add(a);
        parent.Children.Add(element);

        Assert.True(first.Matches(element));
        Assert.True(second.Matches(element));

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(first, new StyleProperties { BackgroundColor = firstColor })
            .Add(second, new StyleProperties { BackgroundColor = secondColor })
            .Build();
        UiContext context = NewContext();
        context.AddRoot(parent);
        context.AddSheet(sheet);
        context.Update(0f);
        return element.Style.BackgroundColor;
    }

    /// <summary>Gives the background of an element in the class and in the hover state, after two rules in the given order.</summary>
    private static Rgba8 StyleOf(in Selector first, Rgba8 firstColor, in Selector second, Rgba8 secondColor, ClassId a)
    {
        Element element = new() { States = PseudoStates.Hover };
        element.Classes.Add(a);

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(first, new StyleProperties { BackgroundColor = firstColor })
            .Add(second, new StyleProperties { BackgroundColor = secondColor })
            .Build();
        UiContext context = NewContext();
        context.AddRoot(element);
        context.AddSheet(sheet);
        context.Update(0f);
        return element.Style.BackgroundColor;
    }

    private static UiContext NewContext()
    {
        return new UiContext { ViewportSize = new float2(400f, 300f) };
    }

    /// <summary>The tree of the mark tests: root, panel, row, and cell in one branch, and root and side in a second branch.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required Element Root { get; init; }

        public required Element Panel { get; init; }

        public required Element Row { get; init; }

        public required Element Cell { get; init; }

        public required Element Side { get; init; }

        /// <summary>Builds the tree, runs one update, and checks that each dirty bit is clear.</summary>
        public static Scene BuildClean()
        {
            Element root = new() { Name = "root" };
            Element panel = new() { Name = "panel" };
            Element row = new() { Name = "row" };
            Element cell = new() { Name = "cell" };
            Element side = new() { Name = "side" };
            root.Children.Add(panel);
            panel.Children.Add(row);
            row.Children.Add(cell);
            root.Children.Add(side);

            UiContext context = NewContext();
            context.AddRoot(root);
            context.Update(0f);

            Assert.Equal(DirtyFlags.None, root.Dirty);
            Assert.Equal(DirtyFlags.None, cell.Dirty);
            Assert.Equal(DirtyFlags.None, side.Dirty);
            return new Scene { Context = context, Root = root, Panel = panel, Row = row, Cell = cell, Side = side };
        }
    }
}
