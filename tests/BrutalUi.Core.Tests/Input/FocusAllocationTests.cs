using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation check of the keyboard path. After warm-up, the Tab search on a tree of 1000 elements and a full
/// keyboard step (the push, the focus moves, the key and char walks, the click focus, and the update) must allocate 0
/// bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is <see cref="ThousandElementTree"/> after one update. Each child of the grid of
/// <see cref="ThousandElementTree.GridSlot"/> (120 slots) accepts the focus. The first row of the hidden settings
/// panel accepts the focus too, but the panel has <see cref="Display.None"/>, so the Tab search never gives it. The
/// Tab order is then slot 0 to slot 119, and it wraps.
/// </para>
/// <para>
/// The first test calls the search methods: <see cref="UiContext.FocusNext"/> 3 times (slot 0, slot 1, slot 2),
/// <see cref="UiContext.FocusPrevious"/> (slot 1), <see cref="UiContext.ClearFocus"/>,
/// <see cref="UiContext.FocusPrevious"/> with no owner (the wrap to the last slot), and
/// <see cref="UiContext.ClearFocus"/>. Each round ends with no owner.
/// </para>
/// <para>
/// Each step of the second test pushes 10 events: Tab, Tab, Shift+Tab, a key A down, a key A up, a char, a down and an
/// up at the slot point, and a down and an up outside the root. The step calls the drain two times and the update one
/// time. The first drain follows the first Tab. The step then ends with no owner, so the next step starts with no
/// owner. The second drain starts with an owner, so it runs the stale focus check of the drain with an owner. The
/// focus goes to slot 0 (the first Tab), slot 1 (the second Tab), slot 0 (Shift+Tab), slot 0 (the click), and then
/// nowhere (the click outside the root).
/// </para>
/// <para>
/// The focus stack starts at 32 frames, so the 2 warm-up rounds reach the deepest point before the measure.
/// </para>
/// </remarks>
public sealed class FocusAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int LetterA = 65;
    private const uint CharA = 'a';

    /// <summary>Counts the key and char calls. Its methods are the targets of the delegates.</summary>
    private sealed class Counter
    {
        public int KeyDowns;
        public int KeyUps;
        public int Chars;
        public int LetterDowns;
        public int LetterUps;
        public uint LastChar;

        public void CountKeyDown(ref KeyEvent e)
        {
            KeyDowns++;
            if (e.Key == LetterA)
            {
                LetterDowns++;
            }
        }

        public void CountKeyUp(ref KeyEvent e)
        {
            KeyUps++;
            if (e.Key == LetterA)
            {
                LetterUps++;
            }
        }

        public void CountChar(ref CharEvent e)
        {
            Chars++;
            LastChar = e.CodePoint;
        }
    }

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required ThousandElementTree Tree { get; init; }

        public required UiContext Context { get; init; }

        public required Counter Counter { get; init; }

        public required Element Grid { get; init; }

        public required Element FirstSlot { get; init; }

        public required Element SecondSlot { get; init; }

        public required Element LastSlot { get; init; }

        public required Element HiddenFocusable { get; init; }

        public required float2 SlotPoint;
        public required float2 OutsidePoint;

        public required KeyEvent Tab;
        public required KeyEvent ShiftTab;
        public required KeyEvent LetterDown;
        public required KeyEvent LetterUp;
        public required CharEvent Letter;
        public required PointerEvent DownSlot;
        public required PointerEvent UpSlot;
        public required PointerEvent DownOutside;
        public required PointerEvent UpOutside;

        /// <summary>The number of walks of the last drain. It keeps the compiler from removing the drain.</summary>
        public int LastWalks;
    }

    private static Scene BuildScene()
    {
        ThousandElementTree tree = ThousandElementTree.Build();
        UiContext context = tree.Context;
        context.Update(0f);

        Element grid = tree.GridSlot.Parent!;
        Counter counter = new();
        UiEventHandler<KeyEvent> onKeyDown = counter.CountKeyDown;
        UiEventHandler<KeyEvent> onKeyUp = counter.CountKeyUp;
        UiEventHandler<CharEvent> onChar = counter.CountChar;
        for (int i = 0; i < grid.Children.Count; i++)
        {
            Element slot = grid.Children[i];
            slot.IsFocusable = true;
            slot.OnKeyDown = onKeyDown;
            slot.OnKeyUp = onKeyUp;
            slot.OnChar = onChar;
        }

        Element hidden = tree.Settings.Children[0];
        hidden.IsFocusable = true;

        float4 slotRect = PointerTree.ViewportRect(tree.GridSlot);

        return new Scene
        {
            Tree = tree,
            Context = context,
            Counter = counter,
            Grid = grid,
            FirstSlot = grid.Children[0],
            SecondSlot = grid.Children[1],
            LastSlot = grid.Children[grid.Children.Count - 1],
            HiddenFocusable = hidden,
            SlotPoint = new float2(slotRect.X + 1f, slotRect.Y + 1f),
            OutsidePoint = new float2(ThousandElementTree.ViewportWideWidth + 10f, ThousandElementTree.ViewportHeight * 0.5f),
            Tab = PointerTree.Tab(shift: false),
            ShiftTab = PointerTree.Tab(shift: true),
            LetterDown = PointerTree.Key(UiEventType.KeyDown, LetterA, UiModifiers.None),
            LetterUp = PointerTree.Key(UiEventType.KeyUp, LetterA, UiModifiers.None),
            Letter = PointerTree.Char(CharA),
            DownSlot = PointerTree.Down(new float2(slotRect.X + 1f, slotRect.Y + 1f), PointerButton.Left),
            UpSlot = PointerTree.Up(new float2(slotRect.X + 1f, slotRect.Y + 1f), PointerButton.Left),
            DownOutside = PointerTree.Down(
                new float2(ThousandElementTree.ViewportWideWidth + 10f, ThousandElementTree.ViewportHeight * 0.5f),
                PointerButton.Left),
            UpOutside = PointerTree.Up(
                new float2(ThousandElementTree.ViewportWideWidth + 10f, ThousandElementTree.ViewportHeight * 0.5f),
                PointerButton.Left),
        };
    }

    /// <summary>The measured body of the first test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void SearchRound(UiContext context)
    {
        context.FocusNext();
        context.FocusNext();
        context.FocusNext();
        context.FocusPrevious();
        context.ClearFocus();
        context.FocusPrevious();
        context.ClearFocus();
    }

    private static void RunSearches(Scene scene, int rounds)
    {
        for (int i = 0; i < rounds; i++)
        {
            SearchRound(scene.Context);
        }
    }

    /// <summary>The measured body of the second test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void Step(Scene scene)
    {
        UiContext context = scene.Context;

        // The first drain follows the first Tab. The step starts with no owner, and the first Tab gives slot 0.
        context.PushEvent(in scene.Tab);
        int walks = context.DrainEvents();

        // The second drain starts with an owner, so the stale focus check runs with an owner.
        context.PushEvent(in scene.Tab);
        context.PushEvent(in scene.ShiftTab);
        context.PushEvent(in scene.LetterDown);
        context.PushEvent(in scene.LetterUp);
        context.PushEvent(in scene.Letter);
        context.PushEvent(in scene.DownSlot);
        context.PushEvent(in scene.UpSlot);
        context.PushEvent(in scene.DownOutside);
        context.PushEvent(in scene.UpOutside);

        scene.LastWalks = walks + context.DrainEvents();
        context.Update(0.016f);
    }

    private static void RunSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            Step(scene);
        }
    }

    [Fact]
    public void TheTabSearchAllocatesNothingOnAWarmTree()
    {
        Scene scene = BuildScene();

        for (int i = 0; i < WarmUpRounds; i++)
        {
            RunSearches(scene, 1);
        }

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunSearches(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void KeyboardRoutingAllocatesNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        for (int i = 0; i < WarmUpRounds; i++)
        {
            RunSteps(scene, 1);
        }

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        UiContext context = scene.Context;
        Counter counter = scene.Counter;

        // The slot point hits slot 0, so the click of the step focuses it.
        Assert.Same(scene.Tree.GridSlot, scene.FirstSlot);
        Assert.Same(scene.FirstSlot, context.HitTest(scene.SlotPoint));
        Assert.Null(context.HitTest(scene.OutsidePoint));
        Assert.Null(context.FocusedElement);

        // The Tab order: slot 0, slot 1, and slot 0 again.
        context.FocusNext();
        Assert.Same(scene.FirstSlot, context.FocusedElement);
        context.FocusNext();
        Assert.Same(scene.SecondSlot, context.FocusedElement);
        context.FocusPrevious();
        Assert.Same(scene.FirstSlot, context.FocusedElement);

        // With no owner, Shift+Tab gives the last slot of the grid. It never gives the element in the hidden panel.
        context.ClearFocus();
        Assert.Null(context.FocusedElement);
        context.FocusPrevious();
        Assert.Same(scene.LastSlot, context.FocusedElement);
        Assert.NotSame(scene.HiddenFocusable, context.FocusedElement);
        context.FocusNext();
        Assert.Same(scene.FirstSlot, context.FocusedElement);
        context.ClearFocus();
        Assert.Null(context.FocusedElement);
        Assert.True(scene.HiddenFocusable.IsFocusable);
        Assert.True(scene.LastSlot.IsFocusable);

        // One full step. The Tab with no owner gets no walk. The second Tab walks slot 0. Shift+Tab walks slot 1. The
        // key A down and up and the char walk slot 0. The click down walks slot 0. The up walks the same target, and
        // the click outside the root gets no walk. The pointer events have no handler here, so only the key and char
        // handlers count.
        Step(scene);

        // The walks: Tab, Shift+Tab, key A down, key A up, char, pointer down, and pointer up.
        Assert.Equal(7, scene.LastWalks);
        Assert.Equal(3, counter.KeyDowns);
        Assert.Equal(1, counter.LetterDowns);
        Assert.Equal(1, counter.KeyUps);
        Assert.Equal(1, counter.LetterUps);
        Assert.Equal(1, counter.Chars);
        Assert.Equal(CharA, counter.LastChar);

        // The click outside the root clears the focus, so no element holds Focus.
        Assert.Null(context.FocusedElement);
        for (int i = 0; i < scene.Tree.All.Length; i++)
        {
            Assert.Equal(0, (int)(scene.Tree.All[i].States & PseudoStates.Focus));
        }
    }
}
