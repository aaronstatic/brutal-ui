using System;
using System.Collections.Generic;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the chord rule (Decision 13 of slider): a click needs the left press that set
/// <see cref="PseudoStates.Active"/>, with no other button down during the gesture. A chord gives no click, and a
/// release with a caller target gives no click.
/// </summary>
/// <remarks>
/// Each test drives the widgets of <see cref="WidgetTree"/> through <see cref="UiContext.PushEvent(in PointerEvent)"/>
/// and <see cref="UiContext.DrainEvents"/>. Only <see cref="AReleaseWithACallerTargetGivesNoClick"/> pushes an event with
/// a caller target. The tree interns no class, so the tests share no class id.
/// </remarks>
public sealed class ChordClickTests
{
    /// <summary>
    /// The demo of phase 1 (order 1 of CR-1). A right press on the button sets <see cref="PseudoStates.Active"/>, a
    /// left press goes down on another element, the right button goes up, and the left release walks to the button.
    /// The base code gives 1 click. The chord rule gives 0 clicks, and the root gets the left release in its bubble
    /// handler.
    /// </summary>
    [Fact]
    public void ARightPressOnTheButtonThenALeftPressElsewhereGivesNoClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootLeftUps = 0;
        tree.Root.OnPointerUp = (ref PointerEvent e) =>
        {
            if (e.Button == PointerButton.Left)
            {
                rootLeftUps++;
            }
        };

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverOther, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(4, tree.Context.DrainEvents());

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: right press on the button, left press on Other, right release, left release on the button, clicks = "
            + clicks);

        Assert.Equal(0, clicks);
        Assert.Equal(1, rootLeftUps);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Button.States & PseudoStates.Active);
    }

    /// <summary>
    /// Order 2 of CR-1. A left press on the button, a right press, a left release over another element, a left press
    /// over that element, a left release over the button, and a right release give no click. The left release over
    /// the button does not end the press, because the right button is still down.
    /// </summary>
    [Fact]
    public void ALeftReleaseAfterALeftPressElsewhereWhileRightIsHeldGivesNoClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverOther, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverOther, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Right));
        Assert.Equal(6, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A left press on the button, a right press, a right release, and a left release over the button give no click.
    /// The right press during the gesture makes a chord.
    /// </summary>
    [Fact]
    public void AChordOnTheButtonGivesNoClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        PushChordOnTheButton(tree);
        Assert.Equal(4, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A right press over no element sets no <see cref="PseudoStates.Active"/>, but its bit stays in the mask. A left
    /// press on the button then sets the state with another button down, so the gesture is a chord. The right release
    /// and the left release over the button give no click.
    /// </summary>
    [Fact]
    public void AButtonDownBeforeTheLeftPressMakesAChord()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverNothing, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));

        // The press over no element gets no walk.
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// Order 1 of CR-1 on the toggle: a right press on the toggle, a left press on another element, a right release,
    /// and a left release over the toggle. The toggle does not flip.
    /// </summary>
    [Fact]
    public void AToggleChordGivesNoFlip()
    {
        WidgetTree tree = new();
        tree.Settle();

        int changes = 0;
        tree.Toggle.CheckedChanged = _ => changes++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverOther, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Left));
        Assert.Equal(4, tree.Context.DrainEvents());

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, changes);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A left <c>PointerUp</c> with the button as the caller target gives no click, and it does not end the press. The
    /// real release over the button then clicks one time (CR-6).
    /// </summary>
    [Fact]
    public void AReleaseWithACallerTargetGivesNoClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerUp, 0d, tree.Button),
            Position = WidgetTree.OverButton,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Same(tree.Button, tree.Context.ActiveElement);

        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A chord leaves no state behind. After the chord of <see cref="AChordOnTheButtonGivesNoClick"/>, a left press
    /// and a left release over the button click one time.
    /// </summary>
    [Fact]
    public void APlainClickAfterAChordClicksOnce()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        PushChordOnTheButton(tree);
        Assert.Equal(4, tree.Context.DrainEvents());
        Assert.Equal(0, clicks);

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A capture handler on the root reads <see cref="UiContext.CurrentPressTarget"/>. The value is the button for a
    /// left press, a move, and the release over the button. It is <see langword="null"/> for a right press and its
    /// release, and after each drain, also after a release handler that throws. A last case copies the click handler
    /// of the button into <see cref="Element.OnPointerDown"/> of a fresh tree. The press gives no click, because the
    /// check reads the event type (CR-9), and the release gives 1 click.
    /// </summary>
    [Fact]
    public void ThePressTargetFollowsTheLeftPress()
    {
        WidgetTree tree = new();
        tree.Settle();

        List<Element?> seen = [];
        tree.Root.OnPointerDownCapture = (ref PointerEvent e) => seen.Add(UiContext.CurrentPressTarget);
        tree.Root.OnPointerMoveCapture = (ref PointerEvent e) => seen.Add(UiContext.CurrentPressTarget);
        tree.Root.OnPointerUpCapture = (ref PointerEvent e) => seen.Add(UiContext.CurrentPressTarget);

        Assert.Null(UiContext.CurrentPressTarget);

        // A left press, a move, and the release over the button.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(WidgetTree.OverButton + new float2(4f, 2f)));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.Equal(3, seen.Count);
        Assert.Same(tree.Button, seen[0]);
        Assert.Same(tree.Button, seen[1]);
        Assert.Same(tree.Button, seen[2]);
        Assert.Null(UiContext.CurrentPressTarget);

        // A right press and its release over the button.
        seen.Clear();
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Right));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(2, seen.Count);
        Assert.Null(seen[0]);
        Assert.Null(seen[1]);
        Assert.Null(UiContext.CurrentPressTarget);

        // A release handler that throws. The handler reads the press target, and the drain writes the old value back.
        seen.Clear();
        Element? seenByThrower = null;
        tree.Other.OnPointerUp = (ref PointerEvent e) =>
        {
            seenByThrower = UiContext.CurrentPressTarget;
            throw new InvalidOperationException("The test handler throws.");
        };

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverOther, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverOther, PointerButton.Left));
        Assert.Throws<InvalidOperationException>(() => tree.Context.DrainEvents());

        Assert.Same(tree.Other, seenByThrower);
        Assert.Equal(2, seen.Count);
        Assert.Same(tree.Other, seen[0]);
        Assert.Same(tree.Other, seen[1]);
        Assert.Null(UiContext.CurrentPressTarget);
        Assert.Null(tree.Context.ActiveElement);

        // CR-9: the click handler of the button in the press slot of a fresh tree. The press target is the button
        // during the press, but the press is no release, so it gives no click. The release gives 1 click.
        WidgetTree copyTree = new();
        copyTree.Settle();

        int copyClicks = 0;
        copyTree.Button.Clicked = _ => copyClicks++;
        copyTree.Button.OnPointerDown = copyTree.Button.OnPointerUp;

        copyTree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, copyTree.Context.DrainEvents());
        Assert.Equal(0, copyClicks);

        copyTree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, copyTree.Context.DrainEvents());
        Assert.Equal(1, copyClicks);
    }

    /// <summary>
    /// Pushes the chord of <see cref="AChordOnTheButtonGivesNoClick"/>: a left press on the button, a right press, a
    /// right release, and a left release over the button.
    /// </summary>
    /// <param name="tree">The tree.</param>
    private static void PushChordOnTheButton(WidgetTree tree)
    {
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
    }
}
