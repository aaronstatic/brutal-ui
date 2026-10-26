using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the disabled rule of the widgets (Decision 6 of button-and-toggle): a <see cref="Button"/> or a
/// <see cref="Toggle"/> that holds <see cref="PseudoStates.Disabled"/> raises no click and no flip, and it does not
/// stop the walk of the event.
/// </summary>
/// <remarks>
/// The drain drops a disabled focus owner, so a key test with a disabled widget pushes the key with the widget as the
/// caller target. The tree interns no class, so the tests share no class id.
/// </remarks>
public sealed class WidgetDisabledTests
{
    /// <summary>
    /// The demo of phase 5. The button holds <see cref="PseudoStates.Disabled"/>. A press and a release over it,
    /// and one <see cref="UiContext.Update(float)"/>, give 0 calls of <see cref="Button.Clicked"/>. The root gets the
    /// release, because the button does not stop the walk.
    /// </summary>
    [Fact]
    public void ADisabledButtonDoesNotClick()
    {
        WidgetTree tree = new();
        tree.Button.States |= PseudoStates.Disabled;
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootUps = 0;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press and release over a disabled button, clicks = " + clicks + ", root releases = " + rootUps);

        Assert.Equal(0, clicks);
        Assert.Equal(1, rootUps);
        Assert.Equal(PseudoStates.None, tree.Button.States & PseudoStates.Active);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.Disabled, tree.Button.States & PseudoStates.Disabled);
    }

    /// <summary>
    /// An Enter key and a Space key with the disabled button as the caller target give no click. The root gets both
    /// keys, because the button does not stop the walk.
    /// </summary>
    [Fact]
    public void ADisabledButtonIgnoresEnterAndSpace()
    {
        WidgetTree tree = new();
        tree.Button.States |= PseudoStates.Disabled;
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(KeyTo(tree.Button, WidgetInput.EnterKey));
        tree.Context.PushEvent(KeyTo(tree.Button, WidgetInput.SpaceKey));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(2, rootDowns);
    }

    /// <summary>
    /// A disable between the press and the release gives no click. The release still clears
    /// <see cref="PseudoStates.Active"/>, and the root gets the release.
    /// </summary>
    [Fact]
    public void ADisableBetweenThePressAndTheReleaseGivesNoClick()
    {
        WidgetTree tree = new();
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootUps = 0;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Button, tree.Context.ActiveElement);

        tree.Button.States |= PseudoStates.Disabled;
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(1, rootUps);
        Assert.Equal(PseudoStates.None, tree.Button.States & PseudoStates.Active);
        Assert.Null(tree.Context.ActiveElement);
    }

    /// <summary>A button that is enabled again clicks again, with a pointer and with a key.</summary>
    [Fact]
    public void AnEnableAgainClicks()
    {
        WidgetTree tree = new();
        tree.Button.States |= PseudoStates.Disabled;
        tree.Settle();

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());
        Assert.Equal(0, clicks);

        tree.Button.States &= ~PseudoStates.Disabled;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());
        Assert.Equal(1, clicks);

        tree.Context.PushEvent(KeyTo(tree.Button, WidgetInput.EnterKey));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(2, clicks);
    }

    /// <summary>
    /// A disabled toggle does not flip on a click or on a Space key. <see cref="Toggle.Checked"/> stays, the count
    /// of <see cref="Toggle.CheckedChanged"/> stays 0, and the root gets both events.
    /// </summary>
    [Fact]
    public void ADisabledToggleDoesNotFlip()
    {
        WidgetTree tree = new();
        tree.Toggle.States |= PseudoStates.Disabled;
        tree.Settle();

        int changes = 0;
        tree.Toggle.CheckedChanged = _ => changes++;
        int rootUps = 0;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, changes);
        Assert.Equal(1, rootUps);

        tree.Context.PushEvent(KeyTo(tree.Toggle, WidgetInput.SpaceKey));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, changes);
        Assert.Equal(1, rootDowns);
    }

    /// <summary>
    /// A set of <see cref="Toggle.Checked"/> from code works on a disabled toggle (Q4), because
    /// <see cref="PseudoStates.Disabled"/> blocks user input only. The set calls no
    /// <see cref="Toggle.CheckedChanged"/>.
    /// </summary>
    [Fact]
    public void ACodeSetOfCheckedWorksOnADisabledToggle()
    {
        WidgetTree tree = new();
        tree.Toggle.States |= PseudoStates.Disabled;
        tree.Settle();

        int changes = 0;
        tree.Toggle.CheckedChanged = _ => changes++;

        tree.Toggle.Checked = true;

        Assert.True(tree.Toggle.Checked);
        Assert.Equal(PseudoStates.Disabled, tree.Toggle.States & PseudoStates.Disabled);
        Assert.Equal(0, changes);

        tree.Toggle.Checked = false;

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(PseudoStates.Disabled, tree.Toggle.States & PseudoStates.Disabled);
        Assert.Equal(0, changes);
    }

    /// <summary>
    /// A press on a disabled button takes no focus (the rule of keyboard-focus holds for a widget). The press clears
    /// the focus of another element, and the button never holds <see cref="PseudoStates.Focus"/>.
    /// </summary>
    [Fact]
    public void APressOnADisabledButtonTakesNoFocus()
    {
        WidgetTree tree = new();
        tree.Button.States |= PseudoStates.Disabled;
        tree.Settle();

        // With no focus owner.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Button.States & PseudoStates.Focus);

        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        // With the toggle as the focus owner.
        Assert.True(tree.Context.SetFocus(tree.Toggle));

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverButton, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Button.States & PseudoStates.Focus);
        Assert.Equal(PseudoStates.None, tree.Toggle.States & PseudoStates.Focus);
    }

    /// <summary>Creates a key down with the target set by the caller.</summary>
    /// <param name="target">The target.</param>
    /// <param name="key">The key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent KeyTo(Element target, int key)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, target),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }
}
