using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the pointer click of <see cref="Button"/>: a left press and a left release on the button call
/// <see cref="Button.Clicked"/> one time, with the button as the sender.
/// </summary>
/// <remarks>
/// Each test drives the button through <see cref="UiContext.PushEvent(in PointerEvent)"/> with no caller target, so
/// the hit test, the press state, and the click focus run. The tree interns no class, so the tests share no class id.
/// </remarks>
public sealed class ButtonClickTests
{
    /// <summary>The fill of the type rule of <see cref="ATypeRuleMatchesTheButton"/>.</summary>
    private static readonly Rgba8 ButtonFill = new(32, 128, 224, 255);

    /// <summary>
    /// The demo of phase 1. A press and a release over the button go through <see cref="UiContext.PushEvent(in PointerEvent)"/>
    /// and one <see cref="UiContext.Update(float)"/>. <see cref="Button.Clicked"/> gets one call with the button as the
    /// sender.
    /// </summary>
    [Fact]
    public void APressAndAReleaseClickTheButtonOnce()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        Button? sender = null;
        tree.Button.Clicked = b =>
        {
            clicks++;
            sender = b;
        };

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press and release over the button, clicks = " + clicks + ", sender = " + sender?.Name);

        Assert.Equal(1, clicks);
        Assert.Same(tree.Button, sender);
        Assert.Equal(PseudoStates.None, tree.Button.States & PseudoStates.Active);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A press on the button and a release over another element give no click. The release walks to the other
    /// element, and the walk does not reach the button.
    /// </summary>
    [Fact]
    public void AReleaseOverAnotherElementDoesNotClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int otherUps = 0;
        tree.Other.OnPointerUp = (ref PointerEvent e) => otherUps++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverOther, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(1, otherUps);
        Assert.Null(tree.Context.ActiveElement);

        // A release over no element gives no click as well.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverNothing, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A press on another element and a release over the button give no click. The button does not hold
    /// <see cref="PseudoStates.Active"/>, so the release does not stop and the root gets its bubble call.
    /// </summary>
    [Fact]
    public void AReleaseAfterAPressElsewhereDoesNotClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootUps = 0;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverOther, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(1, rootUps);

        // A release with no press before it gives no click.
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(2, rootUps);
    }

    /// <summary>
    /// Only the left button clicks (A1). A right press and a right release on the button give no click, and the root
    /// gets the release. A middle press and release give no click.
    /// </summary>
    [Fact]
    public void ARightButtonDoesNotClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootUps = 0;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Right));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(1, rootUps);
        Assert.Null(tree.Context.ActiveElement);

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Middle));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Middle));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(2, rootUps);

        // A left click after the other buttons still clicks.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
        Assert.Equal(2, rootUps);
    }

    /// <summary>
    /// A click stops the walk of the release (A4). The capture handler of the root runs before the button. The bubble
    /// handler of the root gets no call.
    /// </summary>
    [Fact]
    public void AClickStopsTheReleaseWalk()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        int rootCaptures = 0;
        int rootUps = 0;
        int capturesAtClick = -1;
        tree.Root.OnPointerUpCapture = (ref PointerEvent e) => rootCaptures++;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;
        tree.Button.Clicked = _ =>
        {
            clicks++;
            capturesAtClick = rootCaptures;
        };

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
        Assert.Equal(1, rootCaptures);
        Assert.Equal(1, capturesAtClick);
        Assert.Equal(0, rootUps);
    }

    /// <summary>
    /// A button with no <see cref="Button.Clicked"/> delegate does not throw on a click. It still stops the walk, so
    /// the root gets no bubble call.
    /// </summary>
    [Fact]
    public void AButtonWithNoHandlerStopsTheWalkAndDoesNotThrow()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.Null(tree.Button.Clicked);
        Assert.NotNull(tree.Button.OnPointerUp);

        int rootUps = 0;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, rootUps);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>
    /// A button is focusable (A5). A press with no target over the button gives it the keyboard focus.
    /// </summary>
    [Fact]
    public void APressFocusesTheButton()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Button.IsFocusable);
        Assert.Null(tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Button, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Button.States & PseudoStates.Focus);

        // The release keeps the focus on the button.
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Button, tree.Context.FocusedElement);
    }

    /// <summary>
    /// The type id of the button is <c>Button</c> (A6). A rule with that type sets a fill on the button and not on the
    /// plain element.
    /// </summary>
    [Fact]
    public void ATypeRuleMatchesTheButton()
    {
        WidgetTree tree = new();
        ElementTypeId buttonType = ElementType.Intern("Button");
        Assert.Equal(buttonType, tree.Button.TypeId);
        Assert.NotEqual(buttonType, tree.Other.TypeId);

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = buttonType }),
                new StyleProperties { BackgroundColor = ButtonFill })
            .Build();
        tree.Context.AddSheet(sheet);
        tree.Settle();

        Assert.Equal(ButtonFill, tree.Button.Style.BackgroundColor);
        Assert.NotEqual(ButtonFill, tree.Other.Style.BackgroundColor);
        Assert.NotEqual(ButtonFill, tree.Root.Style.BackgroundColor);
    }
}
