using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the input pass of <see cref="UiContext.Update(float)"/> and of <see cref="UiContext.RoutingGate"/>. Each
/// test calls <see cref="UiContext.Update(float)"/> and never calls the drain directly.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class RoutingGateTests
{
    /// <summary>
    /// The demo of phase 1. A move over <see cref="PointerTree.Right"/> and a Tab key down go into the ring. One
    /// <see cref="UiContext.Update(float)"/> drains both before the style pass: <see cref="PointerTree.Right"/> holds
    /// <see cref="PseudoStates.Hover"/> and <see cref="PseudoStates.Focus"/>, it has the hover fill after the same
    /// call, and the gate gives two <see langword="true"/> values.
    /// </summary>
    [Fact]
    public void UpdateRunsTheInputPassBeforeTheStylePass()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(UpdateRunsTheInputPassBeforeTheStylePass));
        tree.Right.IsFocusable = true;
        tree.AddHoverSheet();

        // The hit test reads the layout of the last update, so one update with an empty ring comes first.
        tree.Settle();
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.NotEqual(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);

        tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));
        tree.Context.PushEvent(PointerTree.Tab(false));
        Assert.Equal(2, tree.Context.QueuedEventCount);

        tree.Context.Update(0f);

        UiRoutingGate gate = tree.Context.RoutingGate;
        TestContext.Current.TestOutputHelper?.WriteLine(
            tree.Right.Name + " " + tree.Right.States + " " + tree.Right.Style.BackgroundColor + " pointer="
            + gate.PointerOverUi + " keyboard=" + gate.KeyboardFocusInUi);

        Assert.Equal(0, tree.Context.QueuedEventCount);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.True(gate.PointerOverUi);
        Assert.True(gate.KeyboardFocusInUi);
    }

    /// <summary>With no input, the gate gives two <see langword="false"/> values. The default value does too.</summary>
    [Fact]
    public void TheGateIsFalseWithNoInput()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(TheGateIsFalseWithNoInput));
        tree.Right.IsFocusable = true;
        tree.Settle();

        UiRoutingGate gate = tree.Context.RoutingGate;
        Assert.False(gate.PointerOverUi);
        Assert.False(gate.KeyboardFocusInUi);

        UiRoutingGate empty = default;
        Assert.False(empty.PointerOverUi);
        Assert.False(empty.KeyboardFocusInUi);
    }

    /// <summary>
    /// <see cref="UiRoutingGate.PointerOverUi"/> follows the hit of the last pointer entry with no target. A move
    /// outside the root and a cursor leave give <see langword="false"/>. A move with a target from the caller keeps
    /// the value.
    /// </summary>
    [Fact]
    public void PointerOverUiFollowsTheLastHit()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(PointerOverUiFollowsTheLastHit));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Same(tree.Right, tree.Context.PointerHit);

        // The root is 400 x 300 in a viewport of 800 x 600, so (500, 500) hits no element.
        tree.Context.PushEvent(PointerTree.Move(new float2(500f, 500f)));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.Null(tree.Context.PointerHit);

        // The cursor leave gives (NaN, NaN). It clears a value that is true.
        tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        tree.Context.PushEvent(PointerTree.Move(new float2(float.NaN, float.NaN)));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);

        // A target from the caller skips the hit test. Its position hits Left, and the value stays false.
        tree.Context.PushEvent(TargetedMove(tree.Left, new float2(50f, 50f)));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.Null(tree.Context.PointerHit);

        // Its position hits no element, and a value that is true stays true.
        tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));
        tree.Context.Update(0f);
        tree.Context.PushEvent(TargetedMove(tree.Left, new float2(500f, 500f)));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Same(tree.Right, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(0, tree.Context.QueuedEventCount);
    }

    /// <summary>
    /// The layout-root rule (P6-8 of pointer-routing). A pickable root makes the pointer over its gap a UI pointer.
    /// A root with <see cref="Element.IsPickable"/> set to <see langword="false"/> passes the pointer over its gap to
    /// the game, and its children still get the hit.
    /// </summary>
    [Fact]
    public void ANonPickableRootPassesThePointerToTheGame()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(ANonPickableRootPassesThePointerToTheGame));
        tree.Settle();

        // (300, 200) is in the root and in no child.
        tree.Context.PushEvent(PointerTree.Move(new float2(300f, 200f)));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Same(tree.Root, tree.Context.PointerHit);

        tree.Root.IsPickable = false;
        tree.Context.PushEvent(PointerTree.Move(new float2(300f, 200f)));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.Null(tree.Context.PointerHit);

        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Same(tree.Left, tree.Context.PointerHit);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
    }

    /// <summary>
    /// <see cref="UiRoutingGate.KeyboardFocusInUi"/> follows the focus owner. A Tab and a code call set it. A press
    /// on no focusable element and <see cref="UiContext.ClearFocus"/> clear it. A code call shows at once, with no
    /// update.
    /// </summary>
    [Fact]
    public void KeyboardFocusInUiFollowsTheFocusOwner()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(KeyboardFocusInUiFollowsTheFocusOwner));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        Assert.False(tree.Context.RoutingGate.KeyboardFocusInUi);

        tree.Context.PushEvent(PointerTree.Tab(false));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.KeyboardFocusInUi);
        Assert.Same(tree.Left, tree.Context.FocusedElement);

        // The press hits the gap of the root. The root and its ancestors are not focusable, so the focus clears.
        tree.Context.PushEvent(PointerTree.Down(new float2(300f, 200f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(new float2(300f, 200f), PointerButton.Left));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.KeyboardFocusInUi);
        Assert.Null(tree.Context.FocusedElement);

        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.True(tree.Context.RoutingGate.KeyboardFocusInUi);

        tree.Context.ClearFocus();
        Assert.False(tree.Context.RoutingGate.KeyboardFocusInUi);
    }

    /// <summary>
    /// The stale rules of the input pass run also with an empty ring. After <see cref="PointerTree.Right"/> leaves
    /// the root, the next update clears the hover, the focus, and both values of the gate.
    /// </summary>
    [Fact]
    public void TheStaleRulesClearTheGateAtTheNextUpdate()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(TheStaleRulesClearTheGateAtTheNextUpdate));
        tree.Right.IsFocusable = true;
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));
        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.True(tree.Context.RoutingGate.KeyboardFocusInUi);
        Assert.Equal(PseudoStates.Hover | PseudoStates.Focus, tree.Right.States);

        Assert.True(tree.Root.Children.Remove(tree.Right));

        // The gate is a live read. It keeps the old values until the stale rules of the next update run.
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.True(tree.Context.RoutingGate.KeyboardFocusInUi);

        Assert.Equal(0, tree.Context.QueuedEventCount);
        tree.Context.Update(0f);

        UiRoutingGate gate = tree.Context.RoutingGate;
        Assert.False(gate.PointerOverUi);
        Assert.False(gate.KeyboardFocusInUi);
        Assert.Null(tree.Context.PointerHit);
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States & (PseudoStates.Hover | PseudoStates.Focus));
    }

    /// <summary>
    /// A handler that calls <see cref="UiContext.Update(float)"/> gets <see cref="InvalidOperationException"/>. The
    /// outer update stops before the style pass, and the entry after the thrown entry stays in the ring. The next
    /// update runs, drains that entry, and runs the style pass.
    /// </summary>
    [Fact]
    public void AnUpdateFromAHandlerThrowsAndTheContextStaysValid()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(AnUpdateFromAHandlerThrowsAndTheContextStaysValid));
        tree.AddHoverSheet();
        tree.Settle();

        tree.Left.OnPointerMove = (ref PointerEvent e) => tree.Context.Update(0f);
        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => tree.Context.Update(0f));
        Assert.Contains("Update", ex.Message, StringComparison.Ordinal);

        // The target step ran before the walk, so Left holds Hover. The style pass did not run.
        Assert.Equal(1, tree.Context.QueuedEventCount);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
        Assert.NotEqual(PointerTree.HoverFill, tree.Left.Style.BackgroundColor);

        tree.Left.OnPointerMove = null;
        tree.Context.Update(0f);

        Assert.Equal(0, tree.Context.QueuedEventCount);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Equal(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
    }

    /// <summary>
    /// An event that a handler pushes during the input pass waits for the next update. The first update keeps it in
    /// the ring, and the second update drains it. Two more blocks check the other effects of a handler during the
    /// drain: a handler that removes a root and a handler that adds a root. <see cref="UiContext.Update(float)"/>
    /// reads the roots span after the drain, so the layout pass runs on the new roots.
    /// </summary>
    [Fact]
    public void AnEventThatAHandlerPushesWaitsForTheNextUpdate()
    {
        PointerTree tree = new(nameof(RoutingGateTests) + "." + nameof(AnEventThatAHandlerPushesWaitsForTheNextUpdate));
        tree.Settle();

        bool pushed = false;
        tree.Left.OnPointerMove = (ref PointerEvent e) =>
        {
            if (!pushed)
            {
                pushed = true;
                tree.Context.PushEvent(PointerTree.Move(new float2(180f, 80f)));
            }
        };

        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        tree.Context.Update(0f);

        Assert.True(pushed);
        Assert.Equal(1, tree.Context.QueuedEventCount);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Right.States);

        tree.Context.Update(0f);

        Assert.Equal(0, tree.Context.QueuedEventCount);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.PointerHit);

        // The roots span of Update comes after the drain. A handler that removes a root must not leave a stale span.
        // A stale span holds a null in the last slot, and the layout pass throws NullReferenceException.
        PointerTree removeTree = new(
            nameof(RoutingGateTests) + "." + nameof(AnEventThatAHandlerPushesWaitsForTheNextUpdate) + ".remove");
        Element second = new();
        second.Inline.Width = 40;
        second.Inline.Height = 40;
        removeTree.Context.AddRoot(second);
        removeTree.Settle();

        removeTree.Left.OnPointerMove = (ref PointerEvent e) => removeTree.Context.RemoveRoot(second);
        removeTree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        removeTree.Context.Update(0f);

        Assert.False(second.IsRoot);
        Assert.Equal(1, removeTree.Context.Roots.Length);

        // The roots span of Update comes after the drain. A handler that adds a root gets its layout in the same
        // Update. A stale span skips the new root, and its rect stays 0.
        PointerTree addTree = new(
            nameof(RoutingGateTests) + "." + nameof(AnEventThatAHandlerPushesWaitsForTheNextUpdate) + ".add");
        Element added = new();
        added.Inline.Width = 40;
        added.Inline.Height = 40;
        addTree.Settle();

        addTree.Left.OnPointerMove = (ref PointerEvent e) =>
        {
            if (!added.IsRoot)
            {
                addTree.Context.AddRoot(added);
            }
        };
        addTree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        addTree.Context.Update(0f);

        Assert.True(added.IsRoot);
        Assert.Equal(40f, added.Layout.Rect.Z);
    }

    /// <summary>Creates a pointer move with a target from the caller.</summary>
    /// <param name="target">The target.</param>
    /// <param name="p">The position in viewport pixels. The drain does not hit-test it.</param>
    /// <returns>The event.</returns>
    private static PointerEvent TargetedMove(Element target, float2 p)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerMove, 0d, target),
            Position = p,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }
}
