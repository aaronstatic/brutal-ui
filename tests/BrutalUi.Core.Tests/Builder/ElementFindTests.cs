using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the query by name: <c>Find</c> and <c>Find&lt;T&gt;</c> on <see cref="Element"/>.
/// </summary>
public sealed class ElementFindTests
{
    /// <summary>The walk checks the element itself first, so a named root wins over a named child.</summary>
    [Fact]
    public void FindChecksTheRootFirst()
    {
        Element root = new Element { Name = "a" };
        root.Children.Add(new Element { Name = "a" });

        Assert.Same(root, root.Find("a"u8));
    }

    /// <summary>
    /// The walk runs in depth-first pre-order, so the first hit is the first element with the name in tree order.
    /// A deeper element in the first branch wins over a shallower element in the second branch.
    /// </summary>
    [Fact]
    public void FindWalksInTreeOrderAndGivesTheFirstHit()
    {
        Element root = new Element();

        Element branchOne = new Element();
        Element branchOneChild = new Element();
        Element deepHit = new Element { Name = "a" };
        branchOneChild.Children.Add(deepHit);
        branchOne.Children.Add(branchOneChild);

        Element branchTwoHit = new Element { Name = "a" };

        root.Children.Add(branchOne);
        root.Children.Add(branchTwoHit);

        Assert.Same(deepHit, root.Find("a"u8));
    }

    /// <summary>
    /// The compare is ordinal on the UTF-8 form of the name, so case and bytes both matter.
    /// </summary>
    [Fact]
    public void FindComparesOrdinal()
    {
        Element upper = new Element { Name = "Score" };
        Assert.Null(upper.Find("score"u8));

        Element umlaut = new Element { Name = "Größe" };
        Assert.Same(umlaut, umlaut.Find("Größe"u8));

        Element shortName = new Element { Name = "a" };
        Assert.Null(shortName.Find("ab"u8));
    }

    /// <summary>A miss, an empty name, and a tree with no names each give <see langword="null"/>.</summary>
    [Fact]
    public void FindGivesNullForAMissAnEmptyNameAndAnUnnamedTree()
    {
        Element root = new Element();
        root.Children.Add(new Element { Name = "score" });
        Assert.Null(root.Find("label"u8));

        Element emptyNameTree = new Element();
        emptyNameTree.Children.Add(new Element { Name = "" });
        Assert.Null(emptyNameTree.Find(""u8));

        Element unnamedTree = new Element();
        unnamedTree.Children.Add(new Element());
        Assert.Null(unnamedTree.Find("a"u8));
    }

    /// <summary>The walk does not look above the element, so a parent and a sibling with the name are out of reach.</summary>
    [Fact]
    public void FindDoesNotLookAboveTheElement()
    {
        Element root = new Element { Name = "a" };
        Element child = new Element();
        Element sibling = new Element { Name = "a" };
        root.Children.Add(child);
        root.Children.Add(sibling);

        Assert.Null(child.Find("a"u8));
    }

    /// <summary><c>Find&lt;T&gt;</c> gives a variable with its own type, with no cast.</summary>
    [Fact]
    public void FindTGivesTheElementWithItsType()
    {
        Element root = new Element();
        Label label = new Label(default(Label.CreateInfo)) { Name = "score" };
        root.Children.Add(label);

        Label? found = root.Find<Label>("score"u8);

        Assert.Same(label, found);
    }

    /// <summary>
    /// <c>Find&lt;T&gt;</c> skips an element of another type and walks on, so the later typed element wins
    /// (Q3). A type with no element in the subtree gives <see langword="null"/>.
    /// </summary>
    [Fact]
    public void FindTSkipsAnElementOfAnotherType()
    {
        Element root = new Element();
        Element plain = new Element { Name = "x" };
        Label label = new Label(default(Label.CreateInfo)) { Name = "x" };
        root.Children.Add(plain);
        root.Children.Add(label);

        Label? typedHit = root.Find<Label>("x"u8);
        Button? noHit = root.Find<Button>("x"u8);

        Assert.Same(label, typedHit);
        Assert.Null(noHit);
    }
}
