using System;
using System.Collections.Generic;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the window callbacks of <see cref="UiContext"/>: <see cref="UiContext.RaiseFileDrop(ReadOnlySpan{string})"/>
/// and <see cref="UiContext.RaiseContentScale(float2)"/> call their slot at once and route to no element (A4).
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class WindowCallbackTests
{
    /// <summary>A drop and a scale change with null slots do nothing and do not throw.</summary>
    [Fact]
    public void ADropWithNoCallbackDoesNothing()
    {
        UiContext context = new();
        Assert.Null(context.OnFileDrop);
        Assert.Null(context.OnContentScale);

        context.RaiseFileDrop(["/tmp/a.txt"]);
        context.RaiseFileDrop(ReadOnlySpan<string>.Empty);
        context.RaiseContentScale(new float2(2f, 2f));

        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(0L, context.DroppedEventCount);
    }

    /// <summary>A drop calls the slot one time, and the slot gets each path in the order of the call.</summary>
    [Fact]
    public void ADropPassesEachPathInOrder()
    {
        UiContext context = new();
        List<string> received = [];
        int calls = 0;
        context.OnFileDrop = paths =>
        {
            calls++;
            foreach (string path in paths)
            {
                received.Add(path);
            }
        };

        string[] dropped = ["/home/user/first.png", "/home/user/second.json", "/home/user/third dir/ü.txt"];
        context.RaiseFileDrop(dropped);

        TestContext.Current.TestOutputHelper?.WriteLine(string.Join(" | ", received));

        Assert.Equal(1, calls);
        Assert.Equal(dropped, received);

        context.RaiseFileDrop(ReadOnlySpan<string>.Empty);
        Assert.Equal(2, calls);
        Assert.Equal(3, received.Count);
    }

    /// <summary>A scale change calls the slot one time with the exact scale. The viewport does not change.</summary>
    [Fact]
    public void AContentScalePassesTheScale()
    {
        UiContext context = new();
        context.ViewportSize = new float2(800f, 600f);
        List<float2> received = [];
        context.OnContentScale = scale => received.Add(scale);

        context.RaiseContentScale(new float2(1.5f, 1.25f));

        TestContext.Current.TestOutputHelper?.WriteLine(received[0].X + " " + received[0].Y);

        float2 only = Assert.Single(received);
        Assert.Equal(1.5f, only.X);
        Assert.Equal(1.25f, only.Y);
        Assert.Equal(800f, context.ViewportSize.X);
        Assert.Equal(600f, context.ViewportSize.Y);
    }

    /// <summary>
    /// A drop and a scale change over a settled tree queue no event, store no hit, set no state, and set no dirty bit.
    /// </summary>
    [Fact]
    public void ACallbackQueuesNoEventAndChangesNoState()
    {
        PointerTree tree = new(nameof(WindowCallbackTests) + "." + nameof(ACallbackQueuesNoEventAndChangesNoState));
        tree.AddHoverSheet();
        tree.Settle();

        Element[] elements = [tree.Root, tree.Left, tree.Right, tree.Inner];
        DirtyFlags[] dirtyBefore = new DirtyFlags[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            dirtyBefore[i] = elements[i].Dirty;
        }

        int drops = 0;
        int scales = 0;
        tree.Context.OnFileDrop = _ => drops++;
        tree.Context.OnContentScale = _ => scales++;

        tree.Context.RaiseFileDrop(["/tmp/over-the-right-child.txt"]);
        tree.Context.RaiseContentScale(new float2(2f, 2f));

        Assert.Equal(1, drops);
        Assert.Equal(1, scales);
        Assert.Equal(0, tree.Context.QueuedEventCount);
        Assert.Null(tree.Context.PointerHit);
        Assert.Null(tree.Context.ActiveElement);
        Assert.False(tree.Context.HasPointerPosition);
        for (int i = 0; i < elements.Length; i++)
        {
            Assert.Equal(dirtyBefore[i], elements[i].Dirty);
            Assert.Equal(PseudoStates.None, elements[i].States);
        }

        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.PointerHit);
    }
}
