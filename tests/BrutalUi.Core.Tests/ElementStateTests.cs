using System.Runtime.CompilerServices;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the element state: the pseudo-states, the picking flag, the inline slots, and the fluent builder.
/// </summary>
public sealed class ElementStateTests
{
    /// <summary>Makes a clean element: a root of a new context after one update.</summary>
    private static Element CleanElement()
    {
        UiContext context = new();
        Element element = new();
        context.AddRoot(element);
        context.Update(0f);
        Assert.Equal(DirtyFlags.None, element.Dirty);
        return element;
    }

    [Fact]
    public void NewElementHasNoStatesAndIsPickable()
    {
        Element element = new();

        Assert.Equal(PseudoStates.None, element.States);
        Assert.True(element.IsPickable);
    }

    [Fact]
    public void SetOfANewStateMarksStyleAfterACleanUpdate()
    {
        Element element = CleanElement();

        element.States |= PseudoStates.Hover;

        Assert.Equal(PseudoStates.Hover, element.States);
        Assert.Equal(DirtyFlags.Style, element.Dirty);
    }

    [Fact]
    public void SetOfANewStateOnAChildGivesTheAncestorsTheDescendantStyleBit()
    {
        UiContext context = new();
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        context.AddRoot(root);
        context.Update(0f);

        child.States = PseudoStates.Focus | PseudoStates.Active;

        Assert.Equal(DirtyFlags.Style, child.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, root.Dirty);
    }

    [Fact]
    public void SetToTheSameValueMarksNothing()
    {
        Element element = CleanElement();
        element.States = PseudoStates.Disabled;
        element.ClearDirty(DirtyFlags.Style | DirtyFlags.DescendantStyle);
        Assert.Equal(DirtyFlags.None, element.Dirty);

        element.States = PseudoStates.Disabled;
        element.States |= PseudoStates.Disabled;

        Assert.Equal(DirtyFlags.None, element.Dirty);
    }

    [Fact]
    public void SetToNoneRemovesTheStatesAndMarksStyle()
    {
        Element element = CleanElement();
        element.States = PseudoStates.Hover | PseudoStates.Checked;
        element.ClearDirty(DirtyFlags.Style);

        element.States = PseudoStates.None;

        Assert.Equal(PseudoStates.None, element.States);
        Assert.Equal(DirtyFlags.Style, element.Dirty);
    }

    [Fact]
    public void IsPickableSetMarksNothing()
    {
        Element element = CleanElement();

        element.IsPickable = false;

        Assert.False(element.IsPickable);
        Assert.Equal(DirtyFlags.None, element.Dirty);
    }

    [Fact]
    public void StyleReturnsARefToTheSameField()
    {
        Element first = new();
        Element second = new();

        Assert.True(Unsafe.AreSame(ref Unsafe.AsRef(in first.Style), ref Unsafe.AsRef(in first.Style)));
        Assert.False(Unsafe.AreSame(ref Unsafe.AsRef(in first.Style), ref Unsafe.AsRef(in second.Style)));
    }

    [Fact]
    public void StyleIsReadOnly()
    {
        object[] attributes = typeof(Element).GetProperty("Style")!.GetMethod!.ReturnParameter
            .GetCustomAttributes(false);

        Assert.Contains(attributes, attribute => attribute.GetType().Name == "IsReadOnlyAttribute");
    }

    [Fact]
    public void LayoutReturnsARefToTheSameField()
    {
        Element first = new();
        Element second = new();

        Assert.True(Unsafe.AreSame(ref first.Layout, ref first.Layout));
        Assert.False(Unsafe.AreSame(ref first.Layout, ref second.Layout));
    }

    [Fact]
    public void WriteThroughLayoutMarksNothing()
    {
        Element element = CleanElement();

        element.Layout = default;

        Assert.Equal(DirtyFlags.None, element.Dirty);
    }

    [Fact]
    public void BuilderExpressionBuildsAThreeLevelTree()
    {
        Element leaf = new();
        Element second = new();
        Element mid = new Element().Class("a").Child(leaf);
        Element root = new Element { Name = "root" }.Class("a").Child(mid).Child(second);

        Assert.Equal("root", root.Name);
        Assert.Null(root.Parent);
        Assert.Equal(2, root.Children.Count);
        Assert.Same(mid, root.Children[0]);
        Assert.Same(second, root.Children[1]);
        Assert.Same(root, mid.Parent);
        Assert.Same(root, second.Parent);
        Assert.Equal(1, mid.Children.Count);
        Assert.Same(leaf, mid.Children[0]);
        Assert.Same(mid, leaf.Parent);
        Assert.Equal(0, leaf.Children.Count);
        Assert.Equal(0, second.Children.Count);
        Assert.Equal(1, root.Classes.Count);
        Assert.Equal(ClassName.Intern("a"), root.Classes[0]);
        Assert.Equal(1, mid.Classes.Count);
        Assert.Equal(0, leaf.Classes.Count);
    }

    [Fact]
    public void BuilderOneExpressionFromTheTaskGivesTheExpectedTree()
    {
        Element root = new Element { Name = "root" }.Class("a").Child(new Element().Class("b").Child(new Element())).Child(new Element());

        Assert.Equal("root", root.Name);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal(ClassName.Intern("a"), root.Classes[0]);
        Assert.Equal(ClassName.Intern("b"), root.Children[0].Classes[0]);
        Assert.Equal(1, root.Children[0].Children.Count);
        Assert.Equal(0, root.Children[1].Children.Count);
        Assert.Same(root, root.Children[0].Parent);
        Assert.Same(root.Children[0], root.Children[0].Children[0].Parent);
    }

    [Fact]
    public void BuilderClassWithAnIdAddsTheClass()
    {
        ClassId id = ClassName.Intern(nameof(BuilderClassWithAnIdAddsTheClass) + ".x");

        Element element = new Element().Class(id);

        Assert.True(element.Classes.Contains(id));
        Assert.Equal(1, element.Classes.Count);
    }

    [Fact]
    public void BuilderKeepsTheTypeOfADerivedElementInAChain()
    {
        MarkerElement marker = new MarkerElement().Class(nameof(BuilderKeepsTheTypeOfADerivedElementInAChain) + ".x").Child(new Element());

        Assert.Equal(1, marker.Children.Count);
        Assert.Equal(1, marker.Classes.Count);
        Assert.Equal("marker", marker.Tag);
    }

    /// <summary>A derived element for the type check of the builder.</summary>
    private sealed class MarkerElement : Element
    {
        /// <summary>Gets a fixed tag.</summary>
        public string Tag => "marker";
    }
}
