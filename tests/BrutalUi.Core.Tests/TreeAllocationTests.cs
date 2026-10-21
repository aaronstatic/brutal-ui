using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation check of the tree. After warm-up, a mix of tree changes and one update must allocate 0 bytes.
/// </summary>
public sealed class TreeAllocationTests
{
    private const int ChildCount = 8;
    private const int Iterations = 100;

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required Element Root { get; init; }

        public required Element Spare { get; init; }

        public required Element SpareRoot { get; init; }

        public required Element Leaf { get; init; }

        public required Element OverflowElement { get; init; }

        public required Element InlineElement { get; init; }

        public required ClassId OverflowClass { get; init; }

        public required ClassId InlineClass { get; init; }
    }

    private static Scene BuildScene()
    {
        // Intern each name before the warm-up. Intern takes a lock and can allocate.
        ClassId[] ids = new ClassId[12];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = ClassName.Intern(nameof(TreeAllocationTests) + ".class" + i);
        }

        Element root = new() { Name = "root" };
        Element? leaf = null;
        for (int i = 0; i < ChildCount; i++)
        {
            Element child = new() { Name = "child" + i };
            for (int j = 0; j < ChildCount; j++)
            {
                Element grandchild = new() { Name = "leaf" + i + "-" + j };
                child.Children.Add(grandchild);
                leaf = grandchild;
            }

            root.Children.Add(child);
        }

        // The overflow element holds 12 classes. The inline element holds 4 classes.
        Element overflowElement = root.Children[1].Children[2];
        for (int i = 0; i < 12; i++)
        {
            overflowElement.Classes.Add(ids[i]);
        }

        Element inlineElement = root.Children[4].Children[5];
        for (int i = 0; i < 4; i++)
        {
            inlineElement.Classes.Add(ids[i]);
        }

        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);

        return new Scene
        {
            Context = context,
            Root = root,
            Spare = new Element { Name = "spare" },
            SpareRoot = new Element { Name = "spare-root" },
            Leaf = leaf!,
            OverflowElement = overflowElement,
            InlineElement = inlineElement,
            OverflowClass = ids[11],
            InlineClass = ids[3],
        };
    }

    /// <summary>The measured body. It uses no lambda, no boxing, no string formatting, and no Assert.</summary>
    private static void Step(Scene scene)
    {
        Element root = scene.Root;

        // Remove a child and add it back.
        Element child = root.Children[3];
        root.Children.Remove(child);
        root.Children.Add(child);

        // Reparent a grandchild to another parent and back. Add and Insert get a child that has a parent.
        Element moved = root.Children[2].Children[1];
        root.Children[5].Children.Add(moved);
        root.Children[2].Children.Insert(1, moved);

        // Add a spare root to the context and remove it again.
        scene.Context.AddRoot(scene.SpareRoot);
        scene.Context.RemoveRoot(scene.SpareRoot);

        // Insert at 0 and remove at 0.
        root.Children.Insert(0, scene.Spare);
        root.Children.RemoveAt(0);

        // Move the first child to the last place and back.
        root.Children.Move(0, ChildCount - 1);
        root.Children.Move(ChildCount - 1, 0);

        // Remove one class and add it back on each class element.
        scene.OverflowElement.Classes.Remove(scene.OverflowClass);
        scene.OverflowElement.Classes.Add(scene.OverflowClass);
        scene.InlineElement.Classes.Remove(scene.InlineClass);
        scene.InlineElement.Classes.Add(scene.InlineClass);

        // Set and clear a pseudo-state.
        scene.Leaf.States = PseudoStates.Hover;
        scene.Leaf.States = PseudoStates.None;

        // Mark a leaf for layout, then run the update.
        scene.Leaf.MarkDirty(DirtyFlags.Layout);
        scene.Context.Update(0.016f);
    }

    private static void Run(Scene scene, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            Step(scene);
        }
    }

    [Fact]
    public void TreeChangesAndUpdateAllocateNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        // Warm-up: two runs let the JIT compile the code and let the lists reach their capacity.
        Run(scene, 1);
        Run(scene, 1);

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        Run(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void TheMeasuredBodyKeepsTheTreeShape()
    {
        Scene scene = BuildScene();

        Run(scene, 3);

        Assert.Equal(ChildCount, scene.Root.Children.Count);
        Assert.Equal(ChildCount, scene.Root.Children[2].Children.Count);
        Assert.Equal(ChildCount, scene.Root.Children[5].Children.Count);
        Assert.Same(scene.Root.Children[2], scene.Root.Children[2].Children[1].Parent);
        Assert.Equal(1, scene.Context.Roots.Length);
        Assert.Null(scene.SpareRoot.Parent);
        Assert.Equal(12, scene.OverflowElement.Classes.Count);
        Assert.Equal(4, scene.InlineElement.Classes.Count);
        Assert.Equal(PseudoStates.None, scene.Leaf.States);
        Assert.Equal(DirtyFlags.None, scene.Root.Dirty);
    }
}
