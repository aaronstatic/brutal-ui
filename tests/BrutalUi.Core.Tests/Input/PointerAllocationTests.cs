using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation check of the pointer path. After warm-up, the hit test of a tree of 1000 elements and a full pointer
/// step (the push, the hit, the hover and active states, the walk, and the update) must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is <see cref="ThousandElementTree"/> after one update. The 8 points of the hit test, with the expected hit:
/// </para>
/// <list type="number">
/// <item>The slot point: the top-left corner of grid slot 0, outside its icon and its badge. It hits the slot.</item>
/// <item>The icon point: the center of the icon of the slot. It hits the icon.</item>
/// <item>The badge point: the center of the absolute badge of the slot. It hits the badge.</item>
/// <item>The modal point: 2 pixels inside the top-left corner of the modal, in its padding. It hits the modal.</item>
/// <item>
/// The grid gap point: a point of the grid, right of the last slot of the last row. No slot holds it, so the walk tests
/// the 120 slots under the clip of the grid, and it hits the grid.
/// </item>
/// <item>The tooltip point: 2 pixels inside the top-left corner of the tooltip, in its padding. It hits the tooltip.</item>
/// <item>
/// The settings point: the center of stat row 0, where the hidden settings panel would lie. The walk skips the
/// panel, so the point hits a leaf of the stat row.
/// </item>
/// <item>The outside point: right of the viewport. It hits nothing.</item>
/// </list>
/// <para>
/// Each step of the second test pushes 6 events: a move to the slot point, a down, a move to the modal point, an up, a
/// scroll at the slot point, and a move outside the root. The hover goes to the slot, then to the modal, and then to
/// nothing. The down sets the active state on the slot, and the up clears it. The slot then holds the hover rule border
/// color for one update, so the style step runs the selector match each time.
/// </para>
/// <para>
/// The hit stack starts at 32 frames, so the 2 warm-up rounds reach the deepest point before the measure.
/// </para>
/// </remarks>
public sealed class PointerAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int PointCount = 8;

    /// <summary>Counts the handler calls. Its methods are the targets of the delegates.</summary>
    private sealed class Counter
    {
        public int PointerCalls;
        public int ScrollCalls;
        public int Moves;
        public int Downs;
        public int Ups;

        public void CountPointer(ref PointerEvent e)
        {
            PointerCalls++;
            switch (e.Header.Type)
            {
                case UiEventType.PointerMove:
                    Moves++;
                    break;
                case UiEventType.PointerDown:
                    Downs++;
                    break;
                case UiEventType.PointerUp:
                    Ups++;
                    break;
            }
        }

        public void CountScroll(ref ScrollEvent e) => ScrollCalls++;
    }

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required ThousandElementTree Tree { get; init; }

        public required UiContext Context { get; init; }

        public required Counter SlotCounter { get; init; }

        public required Counter ModalCounter { get; init; }

        public required Element Modal { get; init; }

        public required Element Badge { get; init; }

        public required Element Grid { get; init; }

        public required Element StatRow { get; init; }

        public required float2 SlotPoint;
        public required float2 IconPoint;
        public required float2 BadgePoint;
        public required float2 ModalPoint;
        public required float2 GridGapPoint;
        public required float2 TooltipPoint;
        public required float2 SettingsPoint;
        public required float2 OutsidePoint;

        public required PointerEvent MoveSlot;
        public required PointerEvent DownSlot;
        public required PointerEvent MoveModal;
        public required PointerEvent UpModal;
        public required ScrollEvent ScrollSlot;
        public required PointerEvent MoveOutside;

        /// <summary>The sum of the hits of the last round. It keeps the compiler from removing the hit tests.</summary>
        public int HitSum;

        /// <summary>The number of walks of the last drain.</summary>
        public int LastWalks;
    }

    private static Scene BuildScene()
    {
        ThousandElementTree tree = ThousandElementTree.Build();
        UiContext context = tree.Context;
        context.Update(0f);

        Element slot = tree.GridSlot;
        Element grid = slot.Parent!;
        Element modal = tree.Paragraph.Parent!.Parent!;
        Element badge = slot.Children[1];
        Element lastSlot = grid.Children[grid.Children.Count - 1];

        float4 slotRect = PointerTree.ViewportRect(slot);
        float4 iconRect = PointerTree.ViewportRect(tree.GridIcon);
        float4 badgeRect = PointerTree.ViewportRect(badge);
        float4 modalRect = PointerTree.ViewportRect(modal);
        float4 lastSlotRect = PointerTree.ViewportRect(lastSlot);
        float4 tooltipRect = PointerTree.ViewportRect(tree.Tooltip);
        float4 rowRect = PointerTree.ViewportRect(tree.StatRow);

        float2 slotPoint = new(slotRect.X + 1f, slotRect.Y + 1f);
        float2 modalPoint = new(modalRect.X + 2f, modalRect.Y + 2f);
        float2 outsidePoint = new(ThousandElementTree.ViewportWideWidth + 10f, ThousandElementTree.ViewportHeight * 0.5f);

        Counter slotCounter = new();
        Counter modalCounter = new();
        slot.OnPointerMove = slotCounter.CountPointer;
        slot.OnPointerDown = slotCounter.CountPointer;
        slot.OnScroll = slotCounter.CountScroll;
        modal.OnPointerMove = modalCounter.CountPointer;
        modal.OnPointerUp = modalCounter.CountPointer;

        return new Scene
        {
            Tree = tree,
            Context = context,
            SlotCounter = slotCounter,
            ModalCounter = modalCounter,
            Modal = modal,
            Badge = badge,
            Grid = grid,
            StatRow = tree.StatRow,
            SlotPoint = slotPoint,
            IconPoint = new float2(iconRect.X + iconRect.Z * 0.5f, iconRect.Y + iconRect.W * 0.5f),
            BadgePoint = new float2(badgeRect.X + badgeRect.Z * 0.5f, badgeRect.Y + badgeRect.W * 0.5f),
            ModalPoint = modalPoint,
            GridGapPoint = new float2(lastSlotRect.X + lastSlotRect.Z + 20f, lastSlotRect.Y + lastSlotRect.W * 0.5f),
            TooltipPoint = new float2(tooltipRect.X + 2f, tooltipRect.Y + 2f),
            SettingsPoint = new float2(rowRect.X + rowRect.Z * 0.5f, rowRect.Y + rowRect.W * 0.5f),
            OutsidePoint = outsidePoint,
            MoveSlot = PointerTree.Move(slotPoint),
            DownSlot = PointerTree.Down(slotPoint, PointerButton.Left),
            MoveModal = PointerTree.Move(modalPoint),
            UpModal = PointerTree.Up(modalPoint, PointerButton.Left),
            ScrollSlot = PointerTree.Scroll(slotPoint, new float2(0f, -1f)),
            MoveOutside = PointerTree.Move(outsidePoint),
        };
    }

    /// <summary>The measured body of the first test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void HitRound(Scene scene)
    {
        UiContext context = scene.Context;
        int sum = 0;
        sum += context.HitTest(scene.SlotPoint) is null ? 0 : 1;
        sum += context.HitTest(scene.IconPoint) is null ? 0 : 1;
        sum += context.HitTest(scene.BadgePoint) is null ? 0 : 1;
        sum += context.HitTest(scene.ModalPoint) is null ? 0 : 1;
        sum += context.HitTest(scene.GridGapPoint) is null ? 0 : 1;
        sum += context.HitTest(scene.TooltipPoint) is null ? 0 : 1;
        sum += context.HitTest(scene.SettingsPoint) is null ? 0 : 1;
        sum += context.HitTest(scene.OutsidePoint) is null ? 0 : 1;
        scene.HitSum = sum;
    }

    private static void RunHits(Scene scene, int rounds)
    {
        for (int i = 0; i < rounds; i++)
        {
            HitRound(scene);
        }
    }

    /// <summary>The measured body of the second test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void Step(Scene scene)
    {
        UiContext context = scene.Context;

        context.PushEvent(in scene.MoveSlot);
        context.PushEvent(in scene.DownSlot);
        context.PushEvent(in scene.MoveModal);
        context.PushEvent(in scene.UpModal);
        context.PushEvent(in scene.ScrollSlot);
        context.PushEvent(in scene.MoveOutside);

        scene.LastWalks = context.DrainEvents();
        context.Update(0.016f);
    }

    private static void RunSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            Step(scene);
        }
    }

    private static bool IsInside(Element? element, Element ancestor)
    {
        for (Element? current = element; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void TheHitTestAllocatesNothingOnAWarmTree()
    {
        Scene scene = BuildScene();

        for (int i = 0; i < WarmUpRounds; i++)
        {
            RunHits(scene, 1);
        }

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunHits(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void PointerRoutingAllocatesNothingAfterWarmUp()
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
        ThousandElementTree tree = scene.Tree;
        UiContext context = scene.Context;
        Element slot = tree.GridSlot;

        // The 8 points hit what the remarks of the class say.
        Assert.Same(slot, context.HitTest(scene.SlotPoint));
        Assert.Same(tree.GridIcon, context.HitTest(scene.IconPoint));
        Assert.Same(scene.Badge, context.HitTest(scene.BadgePoint));
        Assert.Same(scene.Modal, context.HitTest(scene.ModalPoint));
        Assert.Same(scene.Grid, context.HitTest(scene.GridGapPoint));
        Assert.Same(tree.Tooltip, context.HitTest(scene.TooltipPoint));
        Element? settingsHit = context.HitTest(scene.SettingsPoint);
        Assert.NotNull(settingsHit);
        Assert.True(IsInside(settingsHit, scene.StatRow));
        Assert.False(IsInside(settingsHit, tree.Settings));
        Assert.Null(context.HitTest(scene.OutsidePoint));

        RunHits(scene, 1);
        Assert.Equal(PointCount - 1, scene.HitSum);

        // A move to the slot point and an update: the slot holds Hover and the hover border color.
        context.PushEvent(in scene.MoveSlot);
        Assert.Equal(1, context.DrainEvents());
        context.Update(0.016f);
        Assert.Equal(PseudoStates.Hover, slot.States);
        Assert.Same(slot, context.PointerHit);
        Assert.Equal(ThousandElementTree.SlotHoverBorderColor, slot.Style.BorderColor);

        // One full step: the pointer leaves the root, so no element holds Hover or Active.
        Counter slotCounter = scene.SlotCounter;
        Counter modalCounter = scene.ModalCounter;
        Step(scene);

        // The move outside the root has no target, so it gets no walk: 5 walks from 6 events.
        Assert.Equal(5, scene.LastWalks);
        Assert.Equal(PseudoStates.None, slot.States);
        Assert.Equal(PseudoStates.None, scene.Modal.States);
        Assert.Null(context.PointerHit);
        Assert.Null(context.ActiveElement);
        Assert.NotEqual(ThousandElementTree.SlotHoverBorderColor, slot.Style.BorderColor);
        for (int i = 0; i < tree.All.Length; i++)
        {
            Assert.Equal(PseudoStates.None, tree.All[i].States);
        }

        // The slot got the move before the update above and the whole step now: 2 moves, 1 down, 1 scroll. The
        // earlier move counts too, so the slot saw 1 + 1 move and 1 down.
        Assert.Equal(2, slotCounter.Moves);
        Assert.Equal(1, slotCounter.Downs);
        Assert.Equal(0, slotCounter.Ups);
        Assert.Equal(3, slotCounter.PointerCalls);
        Assert.Equal(1, slotCounter.ScrollCalls);

        // The modal got one move and one up in the step.
        Assert.Equal(1, modalCounter.Moves);
        Assert.Equal(0, modalCounter.Downs);
        Assert.Equal(1, modalCounter.Ups);
        Assert.Equal(2, modalCounter.PointerCalls);
        Assert.Equal(0, modalCounter.ScrollCalls);
    }
}
