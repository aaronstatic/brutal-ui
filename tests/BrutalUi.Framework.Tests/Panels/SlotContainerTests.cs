using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="SlotContainer"/>: the name, the type, the mount and the unmount of panel roots, and the
/// error rules.
/// </summary>
public sealed class SlotContainerTests
{
    /// <summary>Creates a context with a measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>Creates a slot in a new context and mounts three roots in the order a, b, c.</summary>
    /// <param name="context">The context of the slot.</param>
    /// <param name="a">The first root.</param>
    /// <param name="b">The second root.</param>
    /// <param name="c">The third root.</param>
    /// <returns>The slot.</returns>
    private static SlotContainer CreateSlotWithThreeRoots(out UiContext context, out Element a, out Element b, out Element c)
    {
        context = CreateContext();
        SlotContainer slot = new("left"u8);
        context.AddRoot(slot);
        a = new Element();
        b = new Element();
        c = new Element();
        slot.Mount(a, 0);
        slot.Mount(b, 1);
        slot.Mount(c, 2);
        return slot;
    }

    [Fact]
    public void ANewSlotHoldsACopyOfItsNameAndNoChild()
    {
        byte[] source = "left"u8.ToArray();
        SlotContainer slot = new(source);
        source[0] = (byte)'x';

        Assert.True(slot.SlotName.SequenceEqual("left"u8));
        Assert.Equal(0, slot.Children.Count);
        Assert.Null(slot.Parent);
    }

    [Fact]
    public void TheConstructorRejectsAnEmptyName()
    {
        ArgumentException e = Assert.Throws<ArgumentException>(() => new SlotContainer(ReadOnlySpan<byte>.Empty));
        Assert.Equal("name", e.ParamName);
    }

    [Fact]
    public void TheTypeIsSlotContainerAndTheNameIsTheTextOfTheSlotName()
    {
        SlotContainer slot = new("left"u8);

        Assert.Equal("SlotContainer", slot.TypeId.ToString());
        Assert.NotEqual(new Element().TypeId, slot.TypeId);
        Assert.Equal("left", slot.Name);

        slot.Name = "other";

        Assert.True(slot.SlotName.SequenceEqual("left"u8));
    }

    [Fact]
    public void MountPutsEachRootAtItsIndex()
    {
        SlotContainer slot = new("left"u8);
        Element a = new();
        Element b = new();
        Element c = new();

        slot.Mount(c, 0);
        slot.Mount(a, 0);
        slot.Mount(b, 1);

        Assert.Equal(3, slot.Children.Count);
        Assert.Same(a, slot.Children[0]);
        Assert.Same(b, slot.Children[1]);
        Assert.Same(c, slot.Children[2]);
        Assert.Same(slot, a.Parent);
        Assert.Same(slot, b.Parent);
        Assert.Same(slot, c.Parent);
    }

    [Fact]
    public void AMountOfAHeldRootMovesItWithNoJoin()
    {
        SlotContainer slot = CreateSlotWithThreeRoots(out UiContext context, out Element a, out Element b, out Element c);
        context.Update(0f);

        slot.Mount(c, 0);

        Assert.Equal(3, slot.Children.Count);
        Assert.Same(c, slot.Children[0]);
        Assert.Same(a, slot.Children[1]);
        Assert.Same(b, slot.Children[2]);
        Assert.Equal(DirtyFlags.None, c.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.Layout, slot.Dirty & DirtyFlags.Layout);

        context.Update(0f);
        slot.Mount(c, 0);

        Assert.Equal(DirtyFlags.None, slot.Dirty);

        ArgumentOutOfRangeException badHeldIndex = Assert.Throws<ArgumentOutOfRangeException>(() => slot.Mount(a, 3));
        Assert.Equal("index", badHeldIndex.ParamName);
        Assert.Equal(3, slot.Children.Count);
        Assert.Same(c, slot.Children[0]);
        Assert.Same(a, slot.Children[1]);
        Assert.Same(b, slot.Children[2]);
        Assert.Equal(DirtyFlags.None, slot.Dirty);
    }

    [Fact]
    public void UnmountDetachesTheRootAndKeepsItsState()
    {
        SlotContainer slot = CreateSlotWithThreeRoots(out UiContext context, out Element a, out Element b, out Element c);
        Element root = new() { Name = "panel" };
        Element first = new();
        Element second = new();
        ClassId className = ClassName.Intern(nameof(UnmountDetachesTheRootAndKeepsItsState) + ".slot");
        root.Children.Add(first);
        root.Children.Add(second);
        root.Classes.Add(className);
        root.Inline.Width = 120;
        slot.Mount(root, 1);
        context.Update(0f);

        bool removed = slot.Unmount(root);

        Assert.True(removed);
        Assert.Null(root.Parent);
        Assert.Equal(3, slot.Children.Count);
        Assert.Same(a, slot.Children[0]);
        Assert.Same(b, slot.Children[1]);
        Assert.Same(c, slot.Children[2]);
        Assert.Equal(2, root.Children.Count);
        Assert.Same(first, root.Children[0]);
        Assert.Same(second, root.Children[1]);
        Assert.True(root.Classes.Contains(className));
        Assert.Equal(Length.Px(120), root.Inline.Width);
        Assert.Equal("panel", root.Name);

        slot.Mount(root, 0);

        Assert.Equal(4, slot.Children.Count);
        Assert.Same(root, slot.Children[0]);
        Assert.Same(slot, root.Parent);
        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public void UnmountOfARootThatTheSlotDoesNotHoldReturnsFalse()
    {
        SlotContainer slot = new("left"u8);
        SlotContainer other = new("right"u8);
        Element free = new();
        Element held = new();
        other.Mount(held, 0);

        Assert.False(slot.Unmount(free));
        Assert.False(slot.Unmount(held));

        Assert.Same(other, held.Parent);
        Assert.Equal(1, other.Children.Count);
        Assert.Same(held, other.Children[0]);
    }

    [Fact]
    public void AMountOfARootThatAnotherSlotHoldsThrows()
    {
        SlotContainer slot = new("left"u8);
        SlotContainer other = new("right"u8);
        Element first = new();
        Element root = new();
        other.Mount(first, 0);
        other.Mount(root, 1);

        InvalidOperationException heldByOther = Assert.Throws<InvalidOperationException>(() => slot.Mount(root, 0));
        Assert.Contains("another slot", heldByOther.Message);

        Assert.Equal(0, slot.Children.Count);
        Assert.Equal(2, other.Children.Count);
        Assert.Same(root, other.Children[1]);
        Assert.Same(other, root.Parent);
    }

    [Fact]
    public void AMountOfARootWithAnotherParentThrows()
    {
        SlotContainer slot = new("left"u8);
        Element parent = new();
        Element root = new();
        parent.Children.Add(root);

        InvalidOperationException otherParent = Assert.Throws<InvalidOperationException>(() => slot.Mount(root, 0));
        Assert.Contains("another parent", otherParent.Message);

        Assert.Equal(0, slot.Children.Count);
        Assert.Equal(1, parent.Children.Count);
        Assert.Same(root, parent.Children[0]);
        Assert.Same(parent, root.Parent);
    }

    [Fact]
    public void TheMountRejectsANullRootABadIndexAContextRootAndACycle()
    {
        UiContext context = CreateContext();
        Element ancestor = new();
        SlotContainer slot = new("left"u8);
        ancestor.Children.Add(slot);
        Element held = new();
        slot.Mount(held, 0);
        Element contextRoot = new();
        context.AddRoot(contextRoot);

        ArgumentNullException mountNull = Assert.Throws<ArgumentNullException>(() => slot.Mount(null!, 0));
        Assert.Equal("root", mountNull.ParamName);
        ArgumentNullException unmountNull = Assert.Throws<ArgumentNullException>(() => slot.Unmount(null!));
        Assert.Equal("root", unmountNull.ParamName);

        Element fresh = new();
        ArgumentOutOfRangeException negativeIndex = Assert.Throws<ArgumentOutOfRangeException>(() => slot.Mount(fresh, -1));
        Assert.Equal("index", negativeIndex.ParamName);
        ArgumentOutOfRangeException highIndex =
            Assert.Throws<ArgumentOutOfRangeException>(() => slot.Mount(fresh, slot.Children.Count + 1));
        Assert.Equal("index", highIndex.ParamName);
        Assert.Throws<InvalidOperationException>(() => slot.Mount(contextRoot, 0));
        Assert.Throws<InvalidOperationException>(() => slot.Mount(slot, 0));
        Assert.Throws<InvalidOperationException>(() => slot.Mount(ancestor, 0));

        Assert.Equal(1, slot.Children.Count);
        Assert.Same(held, slot.Children[0]);
        Assert.Null(fresh.Parent);
        Assert.Same(ancestor, slot.Parent);
        Assert.Null(contextRoot.Parent);
    }
}
