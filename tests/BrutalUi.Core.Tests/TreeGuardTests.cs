using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the guards: the cycle guard, the root guard, the argument checks, and the rule that a failed call changes nothing.
/// </summary>
public sealed class TreeGuardTests
{
    /// <summary>
    /// A tree of a root, a middle element, a branch, and a leaf at depth 3, with a second child on the root.
    /// The tree is clean and holds no context root, so the cycle guard runs and not the root guard.
    /// </summary>
    private sealed class Tree
    {
        private Element[] _all = [];
        private DirtyFlags[] _flags = [];
        private Element?[] _parents = [];
        private int[] _counts = [];

        public Tree()
        {
            Root = new Element { Name = "root" };
            Middle = new Element { Name = "middle" };
            Branch = new Element { Name = "branch" };
            Leaf = new Element { Name = "leaf" };
            Other = new Element { Name = "other" };
            Root.Children.Add(Middle);
            Middle.Children.Add(Branch);
            Branch.Children.Add(Leaf);
            Root.Children.Add(Other);

            // The context cleans the bits. The remove clears the root flag and keeps the clean bits.
            UiContext context = new();
            context.AddRoot(Root);
            context.Update(0f);
            context.RemoveRoot(Root);

            Capture();
        }

        public Element Root { get; }

        public Element Middle { get; }

        public Element Branch { get; }

        public Element Leaf { get; }

        public Element Other { get; }

        public void AssertUnchanged()
        {
            for (int i = 0; i < _all.Length; i++)
            {
                Element element = _all[i];
                Assert.Equal(DirtyFlags.None, element.Dirty);
                Assert.Equal(_flags[i], element.Dirty);
                Assert.Same(_parents[i], element.Parent);
                Assert.Equal(_counts[i], element.Children.Count);
            }

            Assert.Same(Root, Middle.Parent);
            Assert.Same(Middle, Branch.Parent);
            Assert.Same(Branch, Leaf.Parent);
            Assert.Same(Root, Other.Parent);
            Assert.Null(Root.Parent);
            Assert.Same(Middle, Root.Children[0]);
            Assert.Same(Other, Root.Children[1]);
            Assert.Same(Branch, Middle.Children[0]);
            Assert.Same(Leaf, Branch.Children[0]);
        }

        private void Capture()
        {
            _all = [Root, Middle, Branch, Leaf, Other];
            _flags = new DirtyFlags[_all.Length];
            _parents = new Element?[_all.Length];
            _counts = new int[_all.Length];
            for (int i = 0; i < _all.Length; i++)
            {
                _flags[i] = _all[i].Dirty;
                _parents[i] = _all[i].Parent;
                _counts[i] = _all[i].Children.Count;
            }
        }
    }

    // The cycle guard.

    [Fact]
    public void AddOfAnElementToItselfThrowsAndChangesNothing()
    {
        Tree tree = new();

        Assert.Throws<InvalidOperationException>(() => tree.Middle.Children.Add(tree.Middle));

        tree.AssertUnchanged();
    }

    [Fact]
    public void AddOfTheParentToItsChildThrowsAndChangesNothing()
    {
        Tree tree = new();

        Assert.Throws<InvalidOperationException>(() => tree.Middle.Children.Add(tree.Root));

        tree.AssertUnchanged();
    }

    [Fact]
    public void AddOfTheRootToTheLeafAtDepthThreeThrowsAndChangesNothing()
    {
        Tree tree = new();

        Assert.Throws<InvalidOperationException>(() => tree.Leaf.Children.Add(tree.Root));

        tree.AssertUnchanged();
    }

    [Fact]
    public void AddOfAnAncestorBelowTheRootToTheLeafThrowsAndChangesNothing()
    {
        Tree tree = new();

        Assert.Throws<InvalidOperationException>(() => tree.Leaf.Children.Add(tree.Middle));

        tree.AssertUnchanged();
    }

    [Fact]
    public void InsertOfTheOwnerOrAnAncestorThrowsAndChangesNothing()
    {
        Tree tree = new();

        Assert.Throws<InvalidOperationException>(() => tree.Leaf.Children.Insert(0, tree.Leaf));
        Assert.Throws<InvalidOperationException>(() => tree.Leaf.Children.Insert(0, tree.Root));
        Assert.Throws<InvalidOperationException>(() => tree.Branch.Children.Insert(1, tree.Middle));

        tree.AssertUnchanged();
    }

    [Fact]
    public void ChildOfTheBuilderThrowsForACycleAndChangesNothing()
    {
        Tree tree = new();

        Assert.Throws<InvalidOperationException>(() => tree.Leaf.Child(tree.Root));

        tree.AssertUnchanged();
    }

    [Fact]
    public void AddOfASiblingBranchIsNoCycleAndSucceeds()
    {
        Tree tree = new();

        tree.Leaf.Children.Add(tree.Other);

        Assert.Same(tree.Leaf, tree.Other.Parent);
        Assert.Equal(1, tree.Root.Children.Count);
    }

    // The root guard.

    [Fact]
    public void AddOfARootToAChildListThrowsAndChangesNothing()
    {
        Tree tree = new();
        UiContext context = new();
        Element root = new() { Name = "context-root" };
        context.AddRoot(root);
        context.Update(0f);

        Assert.Throws<InvalidOperationException>(() => tree.Other.Children.Add(root));

        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.Other.Dirty);
        Assert.Null(root.Parent);
        Assert.Equal(0, tree.Other.Children.Count);
        Assert.Equal(1, context.Roots.Length);
        Assert.Same(root, context.Roots[0]);
        tree.AssertUnchanged();
    }

    [Fact]
    public void InsertOfARootIntoAChildListThrowsAndChangesNothing()
    {
        Tree tree = new();
        UiContext context = new();
        Element root = new() { Name = "context-root" };
        context.AddRoot(root);
        context.Update(0f);

        Assert.Throws<InvalidOperationException>(() => tree.Other.Children.Insert(0, root));

        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.Other.Dirty);
        Assert.Null(root.Parent);
        Assert.Equal(0, tree.Other.Children.Count);
        Assert.Equal(1, context.Roots.Length);
        Assert.Same(root, context.Roots[0]);
        tree.AssertUnchanged();
    }

    [Fact]
    public void ChildOfTheBuilderThrowsForARoot()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        Element parent = new();

        Assert.Throws<InvalidOperationException>(() => parent.Child(root));

        Assert.Equal(0, parent.Children.Count);
        Assert.Null(root.Parent);
    }

    [Fact]
    public void DuplicateAddRootThrowsAndChangesNothing()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        context.Update(0f);

        Assert.Throws<InvalidOperationException>(() => context.AddRoot(root));

        Assert.Equal(1, context.Roots.Length);
        Assert.Same(root, context.Roots[0]);
        Assert.Equal(DirtyFlags.None, root.Dirty);
    }

    [Fact]
    public void AddRootOfARootOfASecondContextThrowsAndChangesNothing()
    {
        UiContext first = new();
        UiContext second = new();
        Element root = new();
        first.AddRoot(root);
        first.Update(0f);

        Assert.Throws<InvalidOperationException>(() => second.AddRoot(root));

        Assert.Equal(1, first.Roots.Length);
        Assert.Equal(0, second.Roots.Length);
        Assert.Equal(DirtyFlags.None, root.Dirty);
    }

    [Fact]
    public void RemoveRootClearsTheRootFlagSoTheElementCanBeAChildOrARootAgain()
    {
        UiContext context = new();
        Element root = new();
        Element parent = new();
        context.AddRoot(root);

        Assert.True(context.RemoveRoot(root));
        parent.Children.Add(root);

        Assert.Same(parent, root.Parent);

        parent.Children.Remove(root);
        context.AddRoot(root);

        Assert.Equal(1, context.Roots.Length);
        Assert.Same(root, context.Roots[0]);
    }

    [Fact]
    public void RemoveRootOnAContextThatDoesNotHoldTheRootKeepsTheRootFlag()
    {
        UiContext first = new();
        UiContext second = new();
        Element root = new();
        first.AddRoot(root);

        Assert.False(second.RemoveRoot(root));

        Assert.Throws<InvalidOperationException>(() => second.AddRoot(root));
        Assert.Equal(1, first.Roots.Length);
    }

    [Fact]
    public void AddRootOfAChildStillMovesItOutOfItsParent()
    {
        Tree tree = new();
        UiContext context = new();

        context.AddRoot(tree.Other);

        Assert.Null(tree.Other.Parent);
        Assert.Equal(1, tree.Root.Children.Count);
        Assert.Same(tree.Other, context.Roots[0]);
    }

    // The null checks.

    [Fact]
    public void NullElementArgumentsOfTheListThrowArgumentNullExceptionWithTheParameterName()
    {
        Element owner = new();
        ElementList children = owner.Children;

        Assert.Equal("child", Assert.Throws<ArgumentNullException>(() => owner.Children.Add(null!)).ParamName);
        Assert.Equal("child", Assert.Throws<ArgumentNullException>(() => owner.Children.Insert(0, null!)).ParamName);
        Assert.Equal("child", Assert.Throws<ArgumentNullException>(() => owner.Children.Remove(null!)).ParamName);
        Assert.Equal("child", Assert.Throws<ArgumentNullException>(() => owner.Children.IndexOf(null!)).ParamName);
        Assert.Equal("child", Assert.Throws<ArgumentNullException>(() => owner.Children.Contains(null!)).ParamName);
        Assert.Equal("child", Assert.Throws<ArgumentNullException>(() => owner.Child(null!)).ParamName);
        Assert.Equal(0, children.Count);
        Assert.Equal(DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint, owner.Dirty);
    }

    [Fact]
    public void NullElementArgumentsOfTheContextThrowArgumentNullExceptionWithTheParameterName()
    {
        UiContext context = new();

        Assert.Equal("root", Assert.Throws<ArgumentNullException>(() => context.AddRoot(null!)).ParamName);
        Assert.Equal("root", Assert.Throws<ArgumentNullException>(() => context.RemoveRoot(null!)).ParamName);
        Assert.Equal(0, context.Roots.Length);
    }

    [Fact]
    public void NullElementOfTheBuilderThrowsArgumentNullExceptionWithTheParameterName()
    {
        Element nullElement = null!;
        ClassId id = ClassName.Intern("guard-test-builder-class");

        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Class("guard-test-builder-class")).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Class(id)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Child(new Element())).ParamName);
    }

    [Fact]
    public void InternOfNullThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => ClassName.Intern(null!));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void InternOfAnEmptyNameThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => ClassName.Intern(string.Empty));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void ClassListAddOfNoneThrowsAndChangesNothing()
    {
        Tree tree = new();
        Element element = tree.Other;
        element.Classes.Add(ClassName.Intern("guard-test-none-class"));
        UiContext context = new();
        context.AddRoot(tree.Root);
        context.Update(0f);
        context.RemoveRoot(tree.Root);
        DirtyFlags before = element.Dirty;

        ArgumentException ex = Assert.Throws<ArgumentException>(() => element.Classes.Add(ClassId.None));
        Assert.Equal("id", ex.ParamName);
        Assert.Equal("id", Assert.Throws<ArgumentException>(() => element.Class(ClassId.None)).ParamName);

        Assert.Equal(1, element.Classes.Count);
        Assert.Equal(before, element.Dirty);
        Assert.Equal(DirtyFlags.None, element.Dirty);
        Assert.False(element.Classes.Contains(ClassId.None));
        tree.AssertUnchanged();
    }

    // The index checks.

    [Fact]
    public void IndexerWithABadIndexThrowsArgumentOutOfRangeExceptionAndNamesIndex()
    {
        Tree tree = new();

        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children[-1]).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children[2]).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Leaf.Children[0]).ParamName);
        Assert.Same(tree.Other, tree.Root.Children[1]);
    }

    [Fact]
    public void RemoveAtWithABadIndexThrowsAndChangesNothing()
    {
        Tree tree = new();

        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.RemoveAt(-1)).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.RemoveAt(2)).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Leaf.Children.RemoveAt(0)).ParamName);

        tree.AssertUnchanged();
    }

    [Fact]
    public void InsertWithABadIndexThrowsAndChangesNothing()
    {
        Tree tree = new();
        Element extra = new();

        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.Insert(-1, extra)).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.Insert(3, extra)).ParamName);

        // A child of the same owner: the valid range is 0 to Count minus 1.
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.Insert(2, tree.Other)).ParamName);

        Assert.Null(extra.Parent);
        tree.AssertUnchanged();
    }

    [Fact]
    public void MoveWithABadIndexThrowsAndNamesTheBadParameter()
    {
        Tree tree = new();

        Assert.Equal("fromIndex", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.Move(-1, 0)).ParamName);
        Assert.Equal("fromIndex", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.Move(2, 0)).ParamName);
        Assert.Equal("toIndex", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.Move(0, -1)).ParamName);
        Assert.Equal("toIndex", Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.Children.Move(0, 2)).ParamName);

        tree.AssertUnchanged();
    }

    [Fact]
    public void ClassListIndexerWithABadIndexNamesIndex()
    {
        Element element = new();
        element.Classes.Add(ClassName.Intern("guard-test-class"));

        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => element.Classes[-1]).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => element.Classes[1]).ParamName);
    }
}
