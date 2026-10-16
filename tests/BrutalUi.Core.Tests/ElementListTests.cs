using System;
using System.Collections.Generic;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the child list: add, remove, the order, the growth, the enumerator, and the search.
/// </summary>
public sealed class ElementListTests
{
    [Fact]
    public void NewElementHasNoChildren()
    {
        Element owner = new();

        Assert.Equal(0, owner.Children.Count);
        Assert.Null(owner.Parent);
    }

    [Fact]
    public void AddOneChildSetsTheCountTheIndexAndTheParent()
    {
        Element owner = new();
        Element child = new();

        owner.Children.Add(child);

        Assert.Equal(1, owner.Children.Count);
        Assert.Same(child, owner.Children[0]);
        Assert.Equal(0, owner.Children.IndexOf(child));
        Assert.Same(owner, child.Parent);
    }

    [Fact]
    public void AddThreeChildrenKeepsTheOrderOfAdd()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();

        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        Assert.Equal(3, owner.Children.Count);
        Assert.Same(a, owner.Children[0]);
        Assert.Same(b, owner.Children[1]);
        Assert.Same(c, owner.Children[2]);
    }

    [Fact]
    public void RemoveByReferenceClearsTheParentAndKeepsTheOrderOfTheRest()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        bool removed = owner.Children.Remove(b);

        Assert.True(removed);
        Assert.Equal(2, owner.Children.Count);
        Assert.Null(b.Parent);
        Assert.Same(a, owner.Children[0]);
        Assert.Same(c, owner.Children[1]);
        Assert.Same(owner, a.Parent);
        Assert.Same(owner, c.Parent);
    }

    [Fact]
    public void RemoveAtRemovesTheChildAtTheIndex()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        owner.Children.RemoveAt(0);

        Assert.Equal(2, owner.Children.Count);
        Assert.Null(a.Parent);
        Assert.Same(b, owner.Children[0]);
        Assert.Same(c, owner.Children[1]);
    }

    [Fact]
    public void RemoveAtOfTheLastIndexKeepsTheOtherChildren()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        owner.Children.Add(a);
        owner.Children.Add(b);

        owner.Children.RemoveAt(1);

        Assert.Equal(1, owner.Children.Count);
        Assert.Null(b.Parent);
        Assert.Same(a, owner.Children[0]);
    }

    [Fact]
    public void RemoveOfANonChildReturnsFalseAndChangesNothing()
    {
        Element owner = new();
        Element child = new();
        Element otherOwner = new();
        Element otherChild = new();
        owner.Children.Add(child);
        otherOwner.Children.Add(otherChild);

        bool removedStranger = owner.Children.Remove(new Element());
        bool removedOtherChild = owner.Children.Remove(otherChild);

        Assert.False(removedStranger);
        Assert.False(removedOtherChild);
        Assert.Equal(1, owner.Children.Count);
        Assert.Same(child, owner.Children[0]);
        Assert.Same(otherOwner, otherChild.Parent);
        Assert.Equal(1, otherOwner.Children.Count);
    }

    [Fact]
    public void RemoveOfTheOnlyChildEmptiesTheListAndAnAddUsesTheListAgain()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        owner.Children.Add(a);

        Assert.True(owner.Children.Remove(a));
        Assert.Equal(0, owner.Children.Count);

        owner.Children.Add(b);

        Assert.Equal(1, owner.Children.Count);
        Assert.Same(b, owner.Children[0]);
    }

    [Fact]
    public void FortyAddsKeepTheOrderPastTwoGrowths()
    {
        // The pool gives 16 slots on the first add. Forty adds make the list grow to 32 and then to 64.
        const int count = 40;
        Element owner = new();
        Element[] children = new Element[count];
        for (int i = 0; i < count; i++)
        {
            children[i] = new Element();
            owner.Children.Add(children[i]);
        }

        Assert.Equal(count, owner.Children.Count);
        for (int i = 0; i < count; i++)
        {
            Assert.Same(children[i], owner.Children[i]);
            Assert.Same(owner, children[i].Parent);
        }
    }

    [Fact]
    public void ForeachVisitsEachChildInOrder()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        List<Element> visited = [];
        foreach (Element child in owner.Children)
        {
            visited.Add(child);
        }

        Assert.Equal([a, b, c], visited);
    }

    [Fact]
    public void ForeachOnAnEmptyListVisitsNothing()
    {
        Element owner = new();

        int visits = 0;
        foreach (Element child in owner.Children)
        {
            Assert.NotNull(child);
            visits++;
        }

        Assert.Equal(0, visits);
    }

    [Fact]
    public void IndexOfAndContainsFindOnlyTheChildren()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element stranger = new();
        owner.Children.Add(a);
        owner.Children.Add(b);

        Assert.Equal(0, owner.Children.IndexOf(a));
        Assert.Equal(1, owner.Children.IndexOf(b));
        Assert.Equal(-1, owner.Children.IndexOf(stranger));
        Assert.True(owner.Children.Contains(a));
        Assert.True(owner.Children.Contains(b));
        Assert.False(owner.Children.Contains(stranger));

        owner.Children.Remove(a);

        Assert.Equal(-1, owner.Children.IndexOf(a));
        Assert.False(owner.Children.Contains(a));
        Assert.Equal(0, owner.Children.IndexOf(b));
    }

    [Fact]
    public void InsertAtZeroPutsTheChildFirst()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);

        owner.Children.Insert(0, c);

        AssertOrder(owner, c, a, b);
        Assert.Same(owner, c.Parent);
    }

    [Fact]
    public void InsertInTheMiddleShiftsTheTail()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        Element d = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        owner.Children.Insert(1, d);

        AssertOrder(owner, a, d, b, c);
        Assert.Same(owner, d.Parent);
    }

    [Fact]
    public void InsertAtCountAddsTheChildAtTheEnd()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);

        owner.Children.Insert(2, c);

        AssertOrder(owner, a, b, c);
    }

    [Fact]
    public void InsertInAnEmptyListAtZeroAddsTheChild()
    {
        Element owner = new();
        Element a = new();

        owner.Children.Insert(0, a);

        AssertOrder(owner, a);
        Assert.Same(owner, a.Parent);
    }

    [Fact]
    public void InsertKeepsTheOrderAcrossGrowth()
    {
        Element owner = new();
        Element[] children = new Element[40];
        for (int i = 0; i < children.Length; i++)
        {
            children[i] = new Element();
            owner.Children.Insert(0, children[i]);
        }

        Assert.Equal(40, owner.Children.Count);
        for (int i = 0; i < children.Length; i++)
        {
            Assert.Same(children[children.Length - 1 - i], owner.Children[i]);
        }
    }

    [Fact]
    public void InsertWithABadIndexThrowsAndChangesNothing()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        owner.Children.Add(a);

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => owner.Children.Insert(-1, b));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => owner.Children.Insert(2, b));

        AssertOrder(owner, a);
        Assert.Null(b.Parent);
    }

    [Fact]
    public void InsertOfAChildOfTheSameOwnerWithABadIndexThrowsAndChangesNothing()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        owner.Children.Add(a);
        owner.Children.Add(b);

        // After the remove the list holds one child, so the valid range is 0 to 1.
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => owner.Children.Insert(2, a));

        AssertOrder(owner, a, b);
        Assert.Same(owner, a.Parent);
    }

    [Fact]
    public void MoveDownShiftsTheChildrenInBetweenUp()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        Element d = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);
        owner.Children.Add(d);

        owner.Children.Move(0, 2);

        AssertOrder(owner, b, c, a, d);
    }

    [Fact]
    public void MoveUpShiftsTheChildrenInBetweenDown()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        Element d = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);
        owner.Children.Add(d);

        owner.Children.Move(3, 1);

        AssertOrder(owner, a, d, b, c);
    }

    [Fact]
    public void MoveFirstToLastAndBackRestoresTheOrder()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        owner.Children.Move(0, 2);
        AssertOrder(owner, b, c, a);
        owner.Children.Move(2, 0);

        AssertOrder(owner, a, b, c);
    }

    [Fact]
    public void MoveToTheSameIndexChangesNothing()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        owner.Children.Add(a);
        owner.Children.Add(b);

        owner.Children.Move(1, 1);

        AssertOrder(owner, a, b);
        Assert.Same(owner, a.Parent);
        Assert.Same(owner, b.Parent);
    }

    [Fact]
    public void MoveWithABadIndexThrowsAndChangesNothing()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        owner.Children.Add(a);
        owner.Children.Add(b);

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => owner.Children.Move(-1, 0));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => owner.Children.Move(0, 2));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => owner.Children.Move(2, 0));

        AssertOrder(owner, a, b);
    }

    [Fact]
    public void AddOfAChildOfAnotherParentMovesItToTheNewParent()
    {
        Element parentA = new();
        Element parentB = new();
        Element x = new();
        Element y = new();
        Element child = new();
        parentA.Children.Add(x);
        parentA.Children.Add(child);
        parentB.Children.Add(y);

        parentB.Children.Add(child);

        Assert.Equal(1, parentA.Children.Count);
        Assert.Equal(2, parentB.Children.Count);
        Assert.Same(parentB, child.Parent);
        Assert.False(parentA.Children.Contains(child));
        AssertOrder(parentA, x);
        AssertOrder(parentB, y, child);
    }

    [Fact]
    public void InsertOfAChildOfAnotherParentMovesItToTheNewParent()
    {
        Element parentA = new();
        Element parentB = new();
        Element y = new();
        Element child = new();
        parentA.Children.Add(child);
        parentB.Children.Add(y);

        parentB.Children.Insert(0, child);

        Assert.Equal(0, parentA.Children.Count);
        Assert.Same(parentB, child.Parent);
        Assert.False(parentA.Children.Contains(child));
        AssertOrder(parentB, child, y);
    }

    [Fact]
    public void AddOfAChildOfTheSameOwnerMovesItToTheEnd()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        owner.Children.Add(a);

        AssertOrder(owner, b, c, a);
        Assert.Same(owner, a.Parent);
    }

    [Fact]
    public void InsertOfAChildOfTheSameOwnerAtZeroMovesItToTheFront()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        owner.Children.Insert(0, c);

        AssertOrder(owner, c, a, b);
        Assert.Same(owner, c.Parent);
    }

    [Fact]
    public void InsertOfAChildOfTheSameOwnerAtTheIndexAfterTheRemoveEndsAtTheEnd()
    {
        Element owner = new();
        Element a = new();
        Element b = new();
        Element c = new();
        owner.Children.Add(a);
        owner.Children.Add(b);
        owner.Children.Add(c);

        // After the remove the list holds b and c, so index 2 is the end.
        owner.Children.Insert(2, a);

        AssertOrder(owner, b, c, a);
    }

    [Fact]
    public void AddRootOfAChildRemovesItFromItsParent()
    {
        Element parent = new();
        Element other = new();
        Element child = new();
        parent.Children.Add(other);
        parent.Children.Add(child);
        UiContext context = new();

        context.AddRoot(child);

        Assert.Null(child.Parent);
        Assert.False(parent.Children.Contains(child));
        AssertOrder(parent, other);
        Assert.Equal(1, context.Roots.Length);
        Assert.Same(child, context.Roots[0]);
    }

    private static void AssertOrder(Element owner, params Element[] expected)
    {
        Assert.Equal(expected.Length, owner.Children.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Same(expected[i], owner.Children[i]);
            Assert.Same(owner, expected[i].Parent);
        }
    }
}
