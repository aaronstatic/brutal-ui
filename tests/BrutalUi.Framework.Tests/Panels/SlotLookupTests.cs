using System;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="SlotContainer.Find(Element, ReadOnlySpan{byte})"/>: the match by slot name, the walk order,
/// the rule that the walk does not enter a slot, and the 0-byte rule of the mount, the unmount, and the lookup.
/// </summary>
public sealed class SlotLookupTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;

    /// <summary>Builds a layout root with the slot <c>left</c> under a column element and the slot <c>right</c> as a direct child.</summary>
    /// <param name="left">The slot <c>left</c>.</param>
    /// <param name="right">The slot <c>right</c>.</param>
    /// <returns>The layout root.</returns>
    private static Element CreateLayoutRoot(out SlotContainer left, out SlotContainer right)
    {
        Element layoutRoot = new();
        Element column = new();
        left = new SlotContainer("left"u8);
        right = new SlotContainer("right"u8);
        column.Children.Add(left);
        layoutRoot.Children.Add(column);
        layoutRoot.Children.Add(right);
        return layoutRoot;
    }

    /// <summary>
    /// The measured body. It moves each root from the left slot to the right slot, moves the roots back in the reverse
    /// order, moves one held root to index 0, and calls <c>Find</c> three times. It uses no lambda, no string, no array,
    /// and no Assert.
    /// </summary>
    private static int Step(Element layoutRoot, SlotContainer left, SlotContainer right, Element[] roots)
    {
        int sum = 0;
        for (int i = 0; i < roots.Length; i++)
        {
            if (left.Unmount(roots[i]))
            {
                sum++;
            }

            right.Mount(roots[i], i);
        }

        sum += left.Children.Count + right.Children.Count;

        for (int i = roots.Length - 1; i >= 0; i--)
        {
            if (right.Unmount(roots[i]))
            {
                sum++;
            }

            left.Mount(roots[i], roots.Length - 1 - i);
        }

        sum += left.Children.Count + right.Children.Count;

        left.Mount(roots[0], 0);
        sum += left.Children.Count + right.Children.Count;

        if (SlotContainer.Find(layoutRoot, "left"u8) is not null)
        {
            sum++;
        }

        if (SlotContainer.Find(layoutRoot, "right"u8) is not null)
        {
            sum++;
        }

        if (SlotContainer.Find(layoutRoot, "missing"u8) is not null)
        {
            sum += 1000;
        }

        return sum;
    }

    private static int RunSteps(Element layoutRoot, SlotContainer left, SlotContainer right, Element[] roots, int steps)
    {
        int sum = 0;
        for (int i = 0; i < steps; i++)
        {
            sum += Step(layoutRoot, left, right, roots);
        }

        return sum;
    }

    [Fact]
    public void FindReturnsTheSlotOfTheName()
    {
        Element layoutRoot = CreateLayoutRoot(out SlotContainer left, out SlotContainer right);

        Assert.Same(left, SlotContainer.Find(layoutRoot, "left"u8));
        Assert.Same(right, SlotContainer.Find(layoutRoot, "right"u8));
    }

    [Fact]
    public void FindChecksTheRootItself()
    {
        SlotContainer slot = new("left"u8);

        Assert.Same(slot, SlotContainer.Find(slot, "left"u8));
        Assert.Null(SlotContainer.Find(slot, "right"u8));

        // A root slot that matches no name does not walk into the panels that it holds.
        Element panelRoot = new();
        SlotContainer inner = new("inner"u8);
        panelRoot.Children.Add(inner);
        slot.Mount(panelRoot, 0);

        Assert.Null(SlotContainer.Find(slot, "inner"u8));
        Assert.Same(slot, SlotContainer.Find(slot, "left"u8));
    }

    [Fact]
    public void FindReturnsNullForAMissingName()
    {
        Element layoutRoot = CreateLayoutRoot(out _, out _);
        layoutRoot.Children.Add(new Element { Name = "missing" });

        Assert.Null(SlotContainer.Find(layoutRoot, "Left"u8));
        Assert.Null(SlotContainer.Find(layoutRoot, "lef"u8));
        Assert.Null(SlotContainer.Find(layoutRoot, "leftx"u8));
        Assert.Null(SlotContainer.Find(layoutRoot, ReadOnlySpan<byte>.Empty));
        Assert.Null(SlotContainer.Find(layoutRoot, "missing"u8));
    }

    [Fact]
    public void FindReadsTheSlotNameAndNotTheElementName()
    {
        Element layoutRoot = CreateLayoutRoot(out SlotContainer left, out _);
        left.Name = "other";

        Assert.Same(left, SlotContainer.Find(layoutRoot, "left"u8));
        Assert.Null(SlotContainer.Find(layoutRoot, "other"u8));
    }

    [Fact]
    public void FindReturnsTheFirstSlotInTreeOrder()
    {
        Element layoutRoot = new();
        Element branchOne = new();
        Element levelTwo = new();
        SlotContainer deep = new("dup"u8);
        Element branchTwo = new();
        SlotContainer shallow = new("dup"u8);
        levelTwo.Children.Add(deep);
        branchOne.Children.Add(levelTwo);
        layoutRoot.Children.Add(branchOne);
        layoutRoot.Children.Add(shallow);
        layoutRoot.Children.Add(branchTwo);

        // The deep slot is at depth 3 in the first branch. The shallow slot is at depth 1 in a later branch.
        Assert.Same(deep, SlotContainer.Find(layoutRoot, "dup"u8));
    }

    [Fact]
    public void FindDoesNotEnterTheChildrenOfASlot()
    {
        Element layoutRoot = CreateLayoutRoot(out SlotContainer left, out _);
        Element panelRoot = new();
        SlotContainer inner = new("inner"u8);
        panelRoot.Children.Add(inner);
        left.Mount(panelRoot, 0);

        Assert.Null(SlotContainer.Find(layoutRoot, "inner"u8));
        Assert.Same(inner, SlotContainer.Find(panelRoot, "inner"u8));
    }

    [Fact]
    public void FindRejectsANullRoot()
    {
        ArgumentNullException e = Assert.Throws<ArgumentNullException>(() => SlotContainer.Find(null!, "left"u8));

        Assert.Equal("root", e.ParamName);
    }

    [Fact]
    public void MountUnmountAndFindAllocateNothingAfterWarmUp()
    {
        Element layoutRoot = CreateLayoutRoot(out SlotContainer left, out SlotContainer right);
        Element[] roots = [new Element(), new Element(), new Element()];
        left.Mount(roots[0], 0);
        left.Mount(roots[1], 1);
        left.Mount(roots[2], 2);

        int warm = RunSteps(layoutRoot, left, right, roots, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = RunSteps(layoutRoot, left, right, roots, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // The body does real work: each step adds the same sum. It counts 6 unmounts, 9 children, and 2 hits.
        Assert.Equal(17 * WarmUpRounds, warm);
        Assert.Equal(17 * Iterations, sum);
        Assert.Equal(3, left.Children.Count);
        Assert.Equal(0, right.Children.Count);
    }
}
