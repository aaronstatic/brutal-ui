using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the key click of <see cref="Button"/>: a focused button clicks on an Enter or a Space key down. A repeat, a
/// key up, another key, and a key with <see cref="UiModifiers.Control"/>, <see cref="UiModifiers.Alt"/>, or
/// <see cref="UiModifiers.Super"/> give no click.
/// </summary>
/// <remarks>
/// Each test pushes a key event with no caller target, so the key route gives the event to the focus owner. The tree
/// interns no class, so the tests share no class id.
/// </remarks>
public sealed class ButtonKeyTests
{
    /// <summary>The GLFW key code of the key A.</summary>
    private const int KeyA = 65;

    /// <summary>
    /// The demo of phase 2. The button has the focus. An Enter key down through
    /// <see cref="UiContext.PushEvent(in KeyEvent)"/> and one <see cref="UiContext.Update(float)"/> calls
    /// <see cref="Button.Clicked"/> one time, with the button as the sender.
    /// </summary>
    [Fact]
    public void AFocusedButtonClicksOnEnter()
    {
        // GLFW_KEY_ENTER and GLFW_KEY_SPACE.
        Assert.Equal(257, WidgetInput.EnterKey);
        Assert.Equal(32, WidgetInput.SpaceKey);

        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Button));

        int clicks = 0;
        Button? sender = null;
        tree.Button.Clicked = b =>
        {
            clicks++;
            sender = b;
        };

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: Enter on the focused button, clicks = " + clicks + ", sender = " + sender?.Name);

        Assert.Equal(1, clicks);
        Assert.Same(tree.Button, sender);
        Assert.Same(tree.Button, tree.Context.FocusedElement);
    }

    /// <summary>A Space key down on the focused button gives one click.</summary>
    [Fact]
    public void AFocusedButtonClicksOnSpace()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Button));

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
    }

    /// <summary>
    /// A repeat key down gives no click. The button does not stop the event, so the root gets the bubble call.
    /// </summary>
    [Fact]
    public void ARepeatDoesNotClick()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Button));

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        KeyEvent enter = PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None);
        enter.IsRepeat = true;
        tree.Context.PushEvent(enter);
        KeyEvent space = PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None);
        space.IsRepeat = true;
        tree.Context.PushEvent(space);
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(2, rootDowns);

        // A key down with no repeat after the repeats still clicks.
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
        Assert.Equal(2, rootDowns);
    }

    /// <summary>
    /// A key up of Enter or Space gives no click. A copy of the key handler in <see cref="Element.OnKeyUp"/> gives no
    /// click as well, because the handler checks the event type.
    /// </summary>
    [Fact]
    public void AKeyUpDoesNotClick()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Button));

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, WidgetInput.SpaceKey, UiModifiers.None));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);

        // A copied key handler in OnKeyUp must still ignore the key up events.
        tree.Button.OnKeyUp = tree.Button.OnKeyDown;
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, WidgetInput.SpaceKey, UiModifiers.None));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
    }

    /// <summary>
    /// Another key gives no click, and the key walks on: the key A reaches the bubble handler of the root. The keypad
    /// Enter key (335) gives no click as well.
    /// </summary>
    [Fact]
    public void AnotherKeyDoesNotClickAndWalksOn()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Button));

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootDowns = 0;
        int rootKey = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) =>
        {
            rootDowns++;
            rootKey = e.Key;
        };

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(1, rootDowns);
        Assert.Equal(KeyA, rootKey);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, 335, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(2, rootDowns);
    }

    /// <summary>
    /// A Control, an Alt, or a Super modifier stops the activation. A Shift modifier does not. A modifier that is not
    /// listed, such as the caps lock, does not stop it.
    /// </summary>
    [Fact]
    public void AModifierStopsTheActivation()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Button));

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.Control));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.Alt));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.Super));
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(3, rootDowns);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.Shift));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
        Assert.Equal(3, rootDowns);

        tree.Context.PushEvent(
            PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.CapsLock | UiModifiers.NumLock));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(2, clicks);
    }

    /// <summary>
    /// A key activation stops the walk (A4). The capture handler of the root runs before the button, so it has run for
    /// each key at the time of the click. The bubble handler of the root gets no call.
    /// </summary>
    [Fact]
    public void AKeyActivationStopsTheWalk()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Button));

        int clicks = 0;
        int rootCaptures = 0;
        int rootDowns = 0;
        int capturesAtClick = -1;
        tree.Root.OnKeyDownCapture = (ref KeyEvent e) => rootCaptures++;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;
        tree.Button.Clicked = _ =>
        {
            clicks++;
            capturesAtClick = rootCaptures;
        };

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(2, clicks);
        Assert.Equal(2, rootCaptures);
        Assert.Equal(2, capturesAtClick);
        Assert.Equal(0, rootDowns);
    }

    /// <summary>
    /// A Tab with no owner focuses the button, and the next Enter clicks. The click does not move the focus.
    /// </summary>
    [Fact]
    public void TabThenEnterClicks()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.Null(tree.Context.FocusedElement);

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;

        tree.Context.PushEvent(PointerTree.Tab(shift: false));
        tree.Context.DrainEvents();
        Assert.Same(tree.Button, tree.Context.FocusedElement);
        Assert.Equal(0, clicks);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(1, clicks);
        Assert.Same(tree.Button, tree.Context.FocusedElement);
    }

    /// <summary>
    /// A focusable child of the button owns the focus. An Enter key down goes to the child, and it bubbles to the
    /// button. The target is the child, so the button gives no click (Decision 5).
    /// </summary>
    [Fact]
    public void AKeyFromAFocusableChildDoesNotClick()
    {
        WidgetTree tree = new();
        Element child = new() { Name = "child", IsFocusable = true };
        child.Inline.Width = 20;
        child.Inline.Height = 20;
        tree.Button.Children.Add(child);
        tree.Settle();
        Assert.True(tree.Context.SetFocus(child));

        int clicks = 0;
        tree.Button.Clicked = _ => clicks++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0, clicks);
        Assert.Equal(1, rootDowns);
        Assert.Same(child, tree.Context.FocusedElement);
    }
}
