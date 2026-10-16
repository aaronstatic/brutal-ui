using System;
using System.Collections.Generic;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// A test visitor that records each element and each flag set that the dirty walk gives it.
/// </summary>
/// <remarks>Test code can allocate. The lists grow during the walk.</remarks>
internal struct RecordingVisitor : IDirtyVisitor
{
    /// <summary>Creates a visitor with empty lists.</summary>
    public RecordingVisitor()
    {
        Elements = [];
        Flags = [];
    }

    /// <summary>Gets the visited elements in the order of the visits.</summary>
    public List<Element> Elements { get; }

    /// <summary>Gets the flags of each visit, at the index of the element.</summary>
    public List<DirtyFlags> Flags { get; }

    /// <inheritdoc />
    public readonly void Visit(Element element, DirtyFlags flags)
    {
        Elements.Add(element);
        Flags.Add(flags);
    }
}

/// <summary>
/// A test visitor that marks <see cref="DirtyFlags.Paint"/> on the visited element and on its first child.
/// It stands for a pass that marks another kind than the kind that its walk clears.
/// </summary>
internal readonly struct MarkPaintVisitor : IDirtyVisitor
{
    /// <inheritdoc />
    public void Visit(Element element, DirtyFlags flags)
    {
        element.MarkDirty(DirtyFlags.Paint);
        if (element.Children.Count > 0)
        {
            element.Children[0].MarkDirty(DirtyFlags.Paint);
        }
    }
}

/// <summary>
/// The tests of the dirty bits: the marks, the propagation to the root, the structural marks, the walk, and the clear.
/// </summary>
public sealed class DirtyTrackingTests
{
    private const DirtyFlags AllSelf = DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint;

    [Fact]
    public void NewElementIsDirtyForStyleLayoutAndPaint()
    {
        Element element = new();

        Assert.Equal(AllSelf, element.Dirty);
    }

    [Fact]
    public void MarkOnALeafAtDepthFourSetsTheSelfBitAndTheDescendantBitOnEachAncestor()
    {
        Chain chain = Chain.Build();
        chain.Clean();

        chain.Leaf.MarkDirty(DirtyFlags.Layout);

        Assert.Equal(DirtyFlags.Layout, chain.Leaf.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, chain.Parent.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, chain.Grandparent.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, chain.Top.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, chain.Root.Dirty);
        Assert.Equal(DirtyFlags.None, chain.SiblingLeaf.Dirty);
    }

    [Fact]
    public void MarkOnASiblingLeafKeepsTheFlagsOfTheSharedParentAndTheRoot()
    {
        Chain chain = Chain.Build();
        chain.Clean();
        chain.Leaf.MarkDirty(DirtyFlags.Layout);
        DirtyFlags rootBefore = chain.Root.Dirty;

        chain.SiblingLeaf.MarkDirty(DirtyFlags.Layout);

        Assert.Equal(DirtyFlags.Layout, chain.SiblingLeaf.Dirty);
        Assert.Equal(DirtyFlags.Layout, chain.Leaf.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, chain.Parent.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, chain.Grandparent.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, chain.Top.Dirty);
        Assert.Equal(rootBefore, chain.Root.Dirty);
    }

    [Fact]
    public void MarkOnASiblingLeafStopsAtTheSharedParent()
    {
        Chain chain = Chain.Build();
        chain.Clean();
        chain.Leaf.MarkDirty(DirtyFlags.Layout);

        // The test breaks the invariant on purpose: the root loses its descendant bit.
        // A mark that goes past the shared parent puts the bit back. A mark that stops at the shared parent does not.
        chain.Root.ClearDirty(DirtyFlags.DescendantLayout);

        chain.SiblingLeaf.MarkDirty(DirtyFlags.Layout);

        Assert.Equal(DirtyFlags.DescendantLayout, chain.Parent.Dirty);
        Assert.Equal(DirtyFlags.None, chain.Root.Dirty);
    }

    [Fact]
    public void MarkOfAnotherKindOnASiblingLeafGoesUpToTheRoot()
    {
        Chain chain = Chain.Build();
        chain.Clean();
        chain.Leaf.MarkDirty(DirtyFlags.Layout);

        chain.SiblingLeaf.MarkDirty(DirtyFlags.Paint);

        const DirtyFlags both = DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint;
        Assert.Equal(DirtyFlags.Paint, chain.SiblingLeaf.Dirty);
        Assert.Equal(both, chain.Parent.Dirty);
        Assert.Equal(both, chain.Grandparent.Dirty);
        Assert.Equal(both, chain.Top.Dirty);
        Assert.Equal(both, chain.Root.Dirty);
    }

    [Fact]
    public void MarkWithNoSelfBitChangesNothing()
    {
        Chain chain = Chain.Build();
        chain.Clean();

        chain.Leaf.MarkDirty(DirtyFlags.None);
        chain.Leaf.MarkDirty(DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint);

        Assert.Equal(DirtyFlags.None, chain.Leaf.Dirty);
        Assert.Equal(DirtyFlags.None, chain.Parent.Dirty);
        Assert.Equal(DirtyFlags.None, chain.Root.Dirty);
    }

    [Fact]
    public void UpdateClearsEachBitOfEachElement()
    {
        Chain chain = Chain.Build();
        UiContext context = new();
        context.AddRoot(chain.Root);

        context.Update(0f);

        foreach (Element element in chain.All())
        {
            Assert.Equal(DirtyFlags.None, element.Dirty);
        }

        chain.Leaf.MarkDirty(AllSelf);
        chain.SiblingLeaf.MarkDirty(DirtyFlags.Style);
        chain.Top.MarkDirty(DirtyFlags.Paint);

        context.Update(0f);

        foreach (Element element in chain.All())
        {
            Assert.Equal(DirtyFlags.None, element.Dirty);
        }
    }

    [Fact]
    public void UpdateClearsEachRoot()
    {
        Element first = new();
        Element second = new();
        Element child = new();
        second.Children.Add(child);
        UiContext context = new();
        context.AddRoot(first);
        context.AddRoot(second);

        context.Update(0f);

        Assert.Equal(DirtyFlags.None, first.Dirty);
        Assert.Equal(DirtyFlags.None, second.Dirty);
        Assert.Equal(DirtyFlags.None, child.Dirty);
    }

    [Fact]
    public void AddRootKeepsTheBitsAndRemoveRootTakesTheRootOutOfTheUpdate()
    {
        Element kept = new();
        Element removed = new();
        UiContext context = new();
        context.AddRoot(kept);
        context.AddRoot(removed);

        Assert.Equal(AllSelf, kept.Dirty);
        Assert.Equal(2, context.Roots.Length);

        Assert.True(context.RemoveRoot(removed));
        Assert.False(context.RemoveRoot(removed));
        Assert.False(context.RemoveRoot(new Element()));
        context.Update(0f);

        Assert.Equal(1, context.Roots.Length);
        Assert.Same(kept, context.Roots[0]);
        Assert.Equal(DirtyFlags.None, kept.Dirty);
        Assert.Equal(AllSelf, removed.Dirty);
    }

    [Fact]
    public void WalkVisitsTheDirtyLeafAndNoElementOfACleanSiblingSubtree()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        tree.ALeaf.MarkDirty(DirtyFlags.Layout);

        RecordingVisitor recorder = new();
        DirtyWalk.Walk(tree.Root, DirtyFlags.Layout, ref recorder);

        // The path elements hold only a descendant bit. The visitor sees self bits only, so the list holds the leaf only.
        Assert.Equal([tree.ALeaf], recorder.Elements);
        Assert.Equal([DirtyFlags.Layout], recorder.Flags);
        foreach (Element element in tree.SubtreeB())
        {
            Assert.DoesNotContain(element, recorder.Elements);
        }

        // The walk clears the leaf and its path.
        Assert.Equal(DirtyFlags.None, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.None, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.None, tree.Root.Dirty);
    }

    [Fact]
    public void WalkVisitsTheDirtyPathInDepthFirstOrder()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        tree.ALeaf.MarkDirty(DirtyFlags.Layout);
        tree.ATop.MarkDirty(DirtyFlags.Layout);
        tree.Root.MarkDirty(DirtyFlags.Layout);

        RecordingVisitor recorder = new();
        DirtyWalk.Walk(tree.Root, DirtyFlags.Layout, ref recorder);

        Assert.Equal([tree.Root, tree.ATop, tree.ALeaf], recorder.Elements);
        foreach (Element element in tree.SubtreeB())
        {
            Assert.DoesNotContain(element, recorder.Elements);
        }
    }

    [Fact]
    public void WalkDoesNotEnterTheChildrenOfAnElementWithNoDescendantBit()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        tree.ALeaf.MarkDirty(DirtyFlags.Layout);
        tree.BLeaf.MarkDirty(DirtyFlags.Layout);

        // The test breaks the invariant on purpose: the top of subtree B loses its descendant bit.
        // A walk that ignores the descendant bits visits the hidden leaf. A walk that reads them does not.
        tree.BTop.ClearDirty(DirtyFlags.DescendantLayout);

        RecordingVisitor recorder = new();
        DirtyWalk.Walk(tree.Root, DirtyFlags.Layout, ref recorder);

        Assert.Equal([tree.ALeaf], recorder.Elements);
        Assert.Equal(DirtyFlags.Layout, tree.BLeaf.Dirty);
    }

    [Fact]
    public void WalkKeepsTheBitsOfTheOtherKinds()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        tree.ALeaf.MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        tree.BLeaf.MarkDirty(DirtyFlags.Paint);

        RecordingVisitor recorder = new();
        DirtyWalk.Walk(tree.Root, DirtyFlags.Layout, ref recorder);

        Assert.Equal([tree.ALeaf], recorder.Elements);
        Assert.Equal([DirtyFlags.Layout], recorder.Flags);
        Assert.Equal(DirtyFlags.Paint, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.Paint, tree.BLeaf.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, tree.BTop.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, tree.Root.Dirty);
    }

    [Fact]
    public void WalkOnACleanTreeVisitsNothing()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();

        RecordingVisitor recorder = new();
        DirtyWalk.Walk(tree.Root, AllSelf, ref recorder);

        Assert.Empty(recorder.Elements);
    }

    [Fact]
    public void VisitorThatMarksPaintDuringALayoutWalkKeepsTheInvariantAndTheWalkClearsTheLayoutBits()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        tree.ATop.MarkDirty(DirtyFlags.Layout);
        tree.ALeaf.MarkDirty(DirtyFlags.Layout);

        MarkPaintVisitor visitor = default;
        DirtyWalk.Walk(tree.Root, DirtyFlags.Layout, ref visitor);

        // The visit of the top marks the top and its first child, the leaf. The visit of the leaf marks the leaf.
        Assert.Equal(DirtyFlags.Paint, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.Paint | DirtyFlags.DescendantPaint, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, tree.Root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.AOther.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BTop.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BLeaf.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BOther.Dirty);
        Assert.Equal(7, AssertInvariant(tree.Root));
    }

    [Fact]
    public void VisitorThatMarksPaintOnAChildThatTheWalkSkipsKeepsTheInvariant()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        tree.ATop.MarkDirty(DirtyFlags.Layout);

        MarkPaintVisitor visitor = default;
        DirtyWalk.Walk(tree.Root, DirtyFlags.Layout, ref visitor);

        // The first child has no layout bit, so the walk does not enter it. The mark of the visitor stays.
        Assert.Equal(DirtyFlags.Paint, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.Paint | DirtyFlags.DescendantPaint, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, tree.Root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.AOther.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BTop.Dirty);
        Assert.Equal(7, AssertInvariant(tree.Root));
    }

    [Fact]
    public void TenThousandSeededRandomCallsKeepTheInvariantAfterEachCall()
    {
        const int seed = 20260416;
        const int elementCount = 24;
        const int contextCount = 2;
        const int callCount = 10_000;

        Random random = new(seed);
        ClassId[] classes = [ClassName.Intern("seed-a"), ClassName.Intern("seed-b"), ClassName.Intern("seed-c")];
        Element[] pool = new Element[elementCount];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = new Element { Name = "e" + i };
        }

        UiContext[] contexts = [new UiContext(), new UiContext()];
        Assert.Equal(contextCount, contexts.Length);
        int rejected = 0;
        int accepted = 0;

        for (int step = 0; step < callCount; step++)
        {
            Element owner = pool[random.Next(elementCount)];
            Element other = pool[random.Next(elementCount)];
            UiContext context = contexts[random.Next(contextCount)];
            int kind = random.Next(14);
            string call = "?";
            bool updated = false;

            try
            {
                switch (kind)
                {
                    case 0:
                        call = $"{owner.Name}.Children.Add({other.Name})";
                        owner.Children.Add(other);
                        break;
                    case 1:
                        int insertIndex = random.Next(-1, owner.Children.Count + 2);
                        call = $"{owner.Name}.Children.Insert({insertIndex}, {other.Name})";
                        owner.Children.Insert(insertIndex, other);
                        break;
                    case 2:
                        call = $"{owner.Name}.Children.Remove({other.Name})";
                        owner.Children.Remove(other);
                        break;
                    case 3:
                        int removeIndex = random.Next(-1, owner.Children.Count + 1);
                        call = $"{owner.Name}.Children.RemoveAt({removeIndex})";
                        owner.Children.RemoveAt(removeIndex);
                        break;
                    case 4:
                        int from = random.Next(-1, owner.Children.Count + 1);
                        int to = random.Next(-1, owner.Children.Count + 1);
                        call = $"{owner.Name}.Children.Move({from}, {to})";
                        owner.Children.Move(from, to);
                        break;
                    case 5:
                        call = $"contexts[{Array.IndexOf(contexts, context)}].AddRoot({owner.Name})";
                        context.AddRoot(owner);
                        break;
                    case 6:
                        call = $"contexts[{Array.IndexOf(contexts, context)}].RemoveRoot({owner.Name})";
                        context.RemoveRoot(owner);
                        break;
                    case 7:
                        call = $"{owner.Name}.Classes.Add";
                        owner.Classes.Add(classes[random.Next(classes.Length)]);
                        break;
                    case 8:
                        call = $"{owner.Name}.Classes.Remove";
                        owner.Classes.Remove(classes[random.Next(classes.Length)]);
                        break;
                    case 9:
                        call = $"{owner.Name}.States";
                        owner.States = (PseudoStates)random.Next(0, 32);
                        break;
                    case 10:
                    case 11:
                        DirtyFlags mark = (DirtyFlags)random.Next(0, 64);
                        call = $"{owner.Name}.MarkDirty({mark})";
                        owner.MarkDirty(mark);
                        break;
                    default:
                        call = $"contexts[{Array.IndexOf(contexts, context)}].Update";
                        context.Update(0f);
                        updated = true;
                        break;
                }

                accepted++;
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                rejected++;
            }

            string where = $"seed {seed}, step {step}, call {call}";

            // Each element has a top: an element with no parent. The check of each top covers the whole pool.
            int visited = 0;
            int rootFlags = 0;
            foreach (Element element in pool)
            {
                if (element.IsRoot)
                {
                    rootFlags++;
                }

                if (element.Parent is null)
                {
                    visited += AssertInvariant(element, where);
                }
            }

            Assert.True(visited == elementCount, $"A cycle or a lost element. Visited {visited}. {where}");
            Assert.True(rootFlags == contexts[0].Roots.Length + contexts[1].Roots.Length, $"The root flags and the roots differ. {where}");
            foreach (UiContext each in contexts)
            {
                foreach (Element root in each.Roots)
                {
                    Assert.True(root.IsRoot && root.Parent is null, $"A root has a bad flag or a parent. {where}");
                }
            }

            if (updated)
            {
                foreach (Element root in context.Roots)
                {
                    AssertClean(root, where);
                }
            }
        }

        // The guards must reject some calls, and the others must pass. Else the sequence tests too little.
        Assert.True(rejected > 100, $"Only {rejected} calls were rejected.");
        Assert.True(accepted > 100, $"Only {accepted} calls were accepted.");
    }

    /// <summary>
    /// Checks the invariant on the subtree of an element and returns the number of elements in the subtree.
    /// </summary>
    /// <remarks>
    /// When an element has the self bit or the descendant bit of a kind, each ancestor has the descendant bit of that kind.
    /// Each child lists its parent. Each parent lists each child one time.
    /// </remarks>
    private static int AssertInvariant(Element root, string where = "")
    {
        return AssertSubtree(root, 0, where);
    }

    private static int AssertSubtree(Element element, int depth, string where)
    {
        Assert.True(depth < 1000, $"The tree is too deep. A cycle is likely. {where}");

        // Each ancestor holds the descendant bit of each kind of this element.
        DirtyFlags needed = DirtyBits.ToDescendant(element.Dirty & DirtyBits.SelfMask) | (element.Dirty & DirtyBits.DescendantMask);
        int hops = 0;
        for (Element? ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            Assert.True(++hops < 1000, $"The parent chain is too long. A cycle is likely. {where}");
            Assert.True(
                (ancestor.Dirty & needed) == needed,
                $"{ancestor.Name} ({ancestor.Dirty}) lacks the bits {needed} of {element.Name} ({element.Dirty}). {where}");
        }

        int count = 1;
        ElementList children = element.Children;
        for (int i = 0; i < children.Count; i++)
        {
            Element child = children[i];
            Assert.True(ReferenceEquals(child.Parent, element), $"{child.Name} does not list {element.Name} as its parent. {where}");
            for (int j = i + 1; j < children.Count; j++)
            {
                Assert.True(!ReferenceEquals(children[j], child), $"{element.Name} lists {child.Name} two times. {where}");
            }

            count += AssertSubtree(child, depth + 1, where);
        }

        return count;
    }

    private static void AssertClean(Element element, string where)
    {
        Assert.True(element.Dirty == DirtyFlags.None, $"{element.Name} is dirty ({element.Dirty}) after an update. {where}");
        foreach (Element child in element.Children)
        {
            AssertClean(child, where);
        }
    }

    [Fact]
    public void AddMarksTheParentForLayoutTheChildForStyleAndSendsTheBitsOfTheChildUp()
    {
        Element root = new();
        Element parent = new();
        root.Children.Add(parent);
        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);

        // A clean subtree with a dirty grandchild. A second context cleans it.
        Element child = new();
        Element grandchild = new();
        child.Children.Add(grandchild);
        UiContext other = new();
        other.AddRoot(child);
        other.Update(0f);
        other.RemoveRoot(child);
        grandchild.MarkDirty(DirtyFlags.Paint);

        parent.Children.Add(child);

        Assert.Equal(DirtyFlags.Paint, grandchild.Dirty);
        Assert.Equal(DirtyFlags.Style | DirtyFlags.DescendantPaint, child.Dirty);
        Assert.Equal(DirtyFlags.Layout | DirtyFlags.DescendantStyle | DirtyFlags.DescendantPaint, parent.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle | DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint, root.Dirty);
    }

    [Fact]
    public void RemoveMarksTheOldParentForLayout()
    {
        Element root = new();
        Element parent = new();
        Element child = new();
        root.Children.Add(parent);
        parent.Children.Add(child);
        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);

        parent.Children.Remove(child);

        Assert.Equal(DirtyFlags.Layout, parent.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, root.Dirty);
        Assert.Equal(DirtyFlags.None, child.Dirty);
    }

    [Fact]
    public void RemoveAtMarksTheOldParentForLayout()
    {
        Element root = new();
        Element parent = new();
        root.Children.Add(parent);
        parent.Children.Add(new Element());
        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);

        parent.Children.RemoveAt(0);

        Assert.Equal(DirtyFlags.Layout, parent.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, root.Dirty);
    }

    [Fact]
    public void RemovedChildKeepsItsBits()
    {
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);
        child.MarkDirty(DirtyFlags.Paint);

        root.Children.Remove(child);
        context.Update(0f);

        Assert.Equal(DirtyFlags.Paint, child.Dirty);
        Assert.Equal(DirtyFlags.None, root.Dirty);
    }

    [Fact]
    public void MarkInARemovedSubtreeDoesNotReachTheOldParent()
    {
        Element root = new();
        Element parent = new();
        Element subtree = new();
        Element leaf = new();
        root.Children.Add(parent);
        parent.Children.Add(subtree);
        subtree.Children.Add(leaf);
        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);

        parent.Children.Remove(subtree);
        context.Update(0f);
        leaf.MarkDirty(DirtyFlags.Layout);

        Assert.Equal(DirtyFlags.Layout, leaf.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, subtree.Dirty);
        Assert.Equal(DirtyFlags.None, parent.Dirty);
        Assert.Equal(DirtyFlags.None, root.Dirty);
    }

    [Fact]
    public void ReparentMarksBothParentsForLayoutTheChildForStyleAndTheAncestorsForDescendantLayout()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();

        tree.BTop.Children.Add(tree.ALeaf);

        Assert.Equal(DirtyFlags.Style, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.Layout, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.Layout | DirtyFlags.DescendantStyle, tree.BTop.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout | DirtyFlags.DescendantStyle, tree.Root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.AOther.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BLeaf.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BOther.Dirty);
    }

    [Fact]
    public void ReparentWithInsertMarksBothParentsForLayoutAndTheChildForStyle()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();

        tree.BTop.Children.Insert(0, tree.ALeaf);

        Assert.Equal(DirtyFlags.Style, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.Layout, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.Layout | DirtyFlags.DescendantStyle, tree.BTop.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout | DirtyFlags.DescendantStyle, tree.Root.Dirty);
    }

    [Fact]
    public void ReparentInTheSameOwnerMarksTheOwnerForLayoutAndTheChildForStyle()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();

        tree.ATop.Children.Add(tree.ALeaf);

        Assert.Equal(DirtyFlags.Style, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.Layout | DirtyFlags.DescendantStyle, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout | DirtyFlags.DescendantStyle, tree.Root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.AOther.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BTop.Dirty);
    }

    [Fact]
    public void DirtySubtreeThatJoinsACleanTreeGivesTheNewAncestorsItsDescendantBits()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        Element joinTop = new() { Name = "join-top" };
        Element joinLeaf = new() { Name = "join-leaf" };
        joinTop.Children.Add(joinLeaf);
        const DirtyFlags allDescendant = DirtyFlags.DescendantStyle | DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint;

        tree.ATop.Children.Add(joinTop);

        Assert.Equal(AllSelf, joinLeaf.Dirty);
        Assert.Equal(AllSelf | allDescendant, joinTop.Dirty);
        Assert.Equal(DirtyFlags.Layout | allDescendant, tree.ATop.Dirty);
        Assert.Equal(allDescendant, tree.Root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BTop.Dirty);
    }

    [Fact]
    public void MoveMarksTheOwnerForLayoutOnly()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();

        tree.ATop.Children.Move(1, 0);

        Assert.Equal(DirtyFlags.Layout, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.DescendantLayout, tree.Root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.ALeaf.Dirty);
        Assert.Equal(DirtyFlags.None, tree.AOther.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BTop.Dirty);
    }

    [Fact]
    public void MoveToTheSameIndexMarksNothing()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();

        tree.ATop.Children.Move(1, 1);

        Assert.Equal(DirtyFlags.None, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.None, tree.Root.Dirty);
    }

    [Fact]
    public void InsertMarksTheOwnerForLayoutAndTheNewChildForStyle()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        Element fresh = new() { Name = "fresh" };

        tree.ATop.Children.Insert(1, fresh);

        Assert.Equal(AllSelf, fresh.Dirty);
        Assert.Equal(DirtyFlags.Layout | DirtyFlags.DescendantStyle | DirtyFlags.DescendantLayout | DirtyFlags.DescendantPaint, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.None, tree.ALeaf.Dirty);
    }

    [Fact]
    public void AddRootOfAChildMarksTheOldParentForLayout()
    {
        TwoSubtrees tree = TwoSubtrees.Build();
        tree.Clean();
        UiContext context = new();

        context.AddRoot(tree.BTop);

        Assert.Equal(DirtyFlags.Layout, tree.Root.Dirty);
        Assert.Equal(DirtyFlags.None, tree.ATop.Dirty);
        Assert.Equal(DirtyFlags.None, tree.BTop.Dirty);
    }

    /// <summary>
    /// A chain of depth 4: root, top, grandparent, parent, and two leaves under the parent.
    /// </summary>
    private sealed class Chain
    {
        private Chain(Element root, Element top, Element grandparent, Element parent, Element leaf, Element siblingLeaf)
        {
            Root = root;
            Top = top;
            Grandparent = grandparent;
            Parent = parent;
            Leaf = leaf;
            SiblingLeaf = siblingLeaf;
        }

        public Element Root { get; }

        public Element Top { get; }

        public Element Grandparent { get; }

        public Element Parent { get; }

        /// <summary>Gets the leaf at depth 4.</summary>
        public Element Leaf { get; }

        /// <summary>Gets the second leaf at depth 4. It shares the parent with <see cref="Leaf"/>.</summary>
        public Element SiblingLeaf { get; }

        public static Chain Build()
        {
            Element root = new() { Name = "root" };
            Element top = new() { Name = "top" };
            Element grandparent = new() { Name = "grandparent" };
            Element parent = new() { Name = "parent" };
            Element leaf = new() { Name = "leaf" };
            Element siblingLeaf = new() { Name = "sibling-leaf" };
            root.Children.Add(top);
            top.Children.Add(grandparent);
            grandparent.Children.Add(parent);
            parent.Children.Add(leaf);
            parent.Children.Add(siblingLeaf);
            return new Chain(root, top, grandparent, parent, leaf, siblingLeaf);
        }

        /// <summary>Clears each bit with an update of a new context, and then removes the root from it.</summary>
        public void Clean()
        {
            UiContext context = new();
            context.AddRoot(Root);
            context.Update(0f);
            context.RemoveRoot(Root);
            foreach (Element element in All())
            {
                Assert.Equal(DirtyFlags.None, element.Dirty);
            }
        }

        public Element[] All() => [Root, Top, Grandparent, Parent, Leaf, SiblingLeaf];
    }

    /// <summary>
    /// A root with two subtrees. Subtree A is a top with a leaf and a second leaf. Subtree B is a top with a leaf and a second leaf.
    /// </summary>
    private sealed class TwoSubtrees
    {
        private TwoSubtrees(Element root, Element aTop, Element aLeaf, Element aOther, Element bTop, Element bLeaf, Element bOther)
        {
            Root = root;
            ATop = aTop;
            ALeaf = aLeaf;
            AOther = aOther;
            BTop = bTop;
            BLeaf = bLeaf;
            BOther = bOther;
        }

        public Element Root { get; }

        public Element ATop { get; }

        public Element ALeaf { get; }

        public Element AOther { get; }

        public Element BTop { get; }

        public Element BLeaf { get; }

        public Element BOther { get; }

        public static TwoSubtrees Build()
        {
            Element root = new() { Name = "root" };
            Element aTop = new() { Name = "a-top" };
            Element aLeaf = new() { Name = "a-leaf" };
            Element aOther = new() { Name = "a-other" };
            Element bTop = new() { Name = "b-top" };
            Element bLeaf = new() { Name = "b-leaf" };
            Element bOther = new() { Name = "b-other" };
            root.Children.Add(aTop);
            root.Children.Add(bTop);
            aTop.Children.Add(aLeaf);
            aTop.Children.Add(aOther);
            bTop.Children.Add(bLeaf);
            bTop.Children.Add(bOther);
            return new TwoSubtrees(root, aTop, aLeaf, aOther, bTop, bLeaf, bOther);
        }

        /// <summary>Clears each bit with an update of a new context, and then removes the root from it.</summary>
        public void Clean()
        {
            UiContext context = new();
            context.AddRoot(Root);
            context.Update(0f);
            context.RemoveRoot(Root);
            foreach (Element element in new[] { Root, ATop, ALeaf, AOther, BTop, BLeaf, BOther })
            {
                Assert.Equal(DirtyFlags.None, element.Dirty);
            }
        }

        public Element[] SubtreeB() => [BTop, BLeaf, BOther];
    }
}
