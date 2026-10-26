using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of <see cref="Toggle"/>: a left click or a Space key down flips <see cref="Toggle.Checked"/>, the flip calls
/// <see cref="Toggle.CheckedChanged"/>, and a code set calls nothing.
/// </summary>
/// <remarks>
/// Each test drives the toggle through <see cref="UiContext.PushEvent(in PointerEvent)"/> and
/// <see cref="UiContext.PushEvent(in KeyEvent)"/> with no caller target. The tree interns no class, so the tests share
/// no class id.
/// </remarks>
public sealed class ToggleTests
{
    /// <summary>The fill of the <c>:checked</c> rule of <see cref="AClickFlipsTheToggle"/>.</summary>
    private static readonly Rgba8 CheckedFill = new(224, 64, 32, 255);

    /// <summary>The fill of the plain rule of <see cref="AClickFlipsTheToggle"/>.</summary>
    private static readonly Rgba8 PlainFill = new(32, 32, 32, 255);

    /// <summary>
    /// The demo of phase 3. A click on the toggle sets <see cref="Toggle.Checked"/>, calls
    /// <see cref="Toggle.CheckedChanged"/> one time with the new value, and a <c>:checked</c> rule restyles the toggle.
    /// </summary>
    [Fact]
    public void AClickFlipsTheToggle()
    {
        WidgetTree tree = new();
        ElementTypeId toggleType = ElementType.Intern("Toggle");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = toggleType }),
                new StyleProperties { BackgroundColor = PlainFill })
            .Add(
                Selector.For(new CompoundSelector { Type = toggleType, States = PseudoStates.Checked }),
                new StyleProperties { BackgroundColor = CheckedFill })
            .Build();
        tree.Context.AddSheet(sheet);
        tree.Settle();
        Assert.False(tree.Toggle.Checked);
        Assert.Equal(PlainFill, tree.Toggle.Style.BackgroundColor);

        int calls = 0;
        bool seen = false;
        Toggle? sender = null;
        tree.Toggle.CheckedChanged = t =>
        {
            calls++;
            seen = t.Checked;
            sender = t;
        };

        // The capture handler of the root runs before the toggle. The bubble handler of the root runs after it, so the
        // stop of the toggle click must keep the bubble handler silent.
        int rootCaptureUps = 0;
        int rootBubbleUps = 0;
        tree.Root.OnPointerUpCapture = (ref PointerEvent e) => rootCaptureUps++;
        tree.Root.OnPointerUp = (ref PointerEvent e) => rootBubbleUps++;

        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Left));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: click on the toggle, Checked = " + tree.Toggle.Checked + ", calls = " + calls
            + ", fill = " + tree.Toggle.Style.BackgroundColor);

        Assert.True(tree.Toggle.Checked);
        Assert.Equal(PseudoStates.Checked, tree.Toggle.States & PseudoStates.Checked);
        Assert.Equal(1, calls);
        Assert.True(seen);
        Assert.Same(tree.Toggle, sender);
        Assert.Equal(CheckedFill, tree.Toggle.Style.BackgroundColor);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(1, rootCaptureUps);
        Assert.Equal(0, rootBubbleUps);
    }

    /// <summary>A second click clears <see cref="PseudoStates.Checked"/> and calls the delegate with <see langword="false"/>.</summary>
    [Fact]
    public void ASecondClickClearsChecked()
    {
        WidgetTree tree = new();
        tree.Settle();

        int calls = 0;
        bool lastValue = false;
        tree.Toggle.CheckedChanged = t =>
        {
            calls++;
            lastValue = t.Checked;
        };

        for (int i = 0; i < 2; i++)
        {
            tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Left));
            tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Left));
            Assert.Equal(2, tree.Context.DrainEvents());
        }

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(PseudoStates.None, tree.Toggle.States & PseudoStates.Checked);
        Assert.Equal(2, calls);
        Assert.False(lastValue);
    }

    /// <summary>A Space key down on the focused toggle flips it and calls the delegate one time.</summary>
    [Fact]
    public void SpaceFlipsAFocusedToggle()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Toggle));

        int calls = 0;
        Toggle? sender = null;
        tree.Toggle.CheckedChanged = t =>
        {
            calls++;
            sender = t;
        };
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.True(tree.Toggle.Checked);
        Assert.Equal(1, calls);
        Assert.Same(tree.Toggle, sender);
        Assert.Equal(0, rootDowns);
        Assert.Same(tree.Toggle, tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// An Enter key down on the focused toggle does nothing (A3). The key walks on, so the bubble handler of the root
    /// gets it.
    /// </summary>
    [Fact]
    public void EnterDoesNothingOnAFocusedToggle()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Toggle));

        int calls = 0;
        tree.Toggle.CheckedChanged = _ => calls++;
        int rootDowns = 0;
        int rootKey = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) =>
        {
            rootDowns++;
            rootKey = e.Key;
        };

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootDowns);
        Assert.Equal(WidgetInput.EnterKey, rootKey);
    }

    /// <summary>
    /// A repeat Space key down does nothing and walks on. A key down with no repeat after it still flips.
    /// </summary>
    [Fact]
    public void ARepeatSpaceDoesNothing()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Toggle));

        int calls = 0;
        tree.Toggle.CheckedChanged = _ => calls++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        KeyEvent space = PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None);
        space.IsRepeat = true;
        tree.Context.PushEvent(space);
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootDowns);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.True(tree.Toggle.Checked);
        Assert.Equal(1, calls);
        Assert.Equal(1, rootDowns);
    }

    /// <summary>
    /// A set of <see cref="Toggle.Checked"/> from code changes the bit and calls no
    /// <see cref="Toggle.CheckedChanged"/> (Q4).
    /// </summary>
    [Fact]
    public void ACodeSetCallsNoCheckedChanged()
    {
        WidgetTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Toggle.CheckedChanged = _ => calls++;

        tree.Toggle.Checked = true;
        Assert.True(tree.Toggle.Checked);
        Assert.Equal(PseudoStates.Checked, tree.Toggle.States & PseudoStates.Checked);
        Assert.Equal(0, calls);

        tree.Toggle.Checked = false;
        Assert.False(tree.Toggle.Checked);
        Assert.Equal(PseudoStates.None, tree.Toggle.States & PseudoStates.Checked);
        Assert.Equal(0, calls);

        // A set through States calls nothing as well.
        tree.Toggle.States |= PseudoStates.Checked;
        Assert.True(tree.Toggle.Checked);
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// A set to the same value marks nothing: after a settle, a set of <see langword="false"/> on an unchecked toggle
    /// leaves <see cref="DirtyFlags.Style"/> clear. A set to a new value marks it.
    /// </summary>
    [Fact]
    public void ASetToTheSameValueMarksNothing()
    {
        WidgetTree tree = new();
        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.Toggle.Dirty & DirtyFlags.Style);

        tree.Toggle.Checked = false;
        Assert.Equal(DirtyFlags.None, tree.Toggle.Dirty & DirtyFlags.Style);

        tree.Toggle.Checked = true;
        Assert.Equal(DirtyFlags.Style, tree.Toggle.Dirty & DirtyFlags.Style);

        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.Toggle.Dirty & DirtyFlags.Style);

        tree.Toggle.Checked = true;
        Assert.Equal(DirtyFlags.None, tree.Toggle.Dirty & DirtyFlags.Style);
    }

    /// <summary>
    /// The create-info sets the start value (A7). The constructor calls no <see cref="Toggle.CheckedChanged"/>, and
    /// the default create-info gives an unchecked toggle.
    /// </summary>
    [Fact]
    public void TheCreateInfoSetsTheStartValue()
    {
        Toggle on = new(new Toggle.CreateInfo { Checked = true });
        Toggle off = new(new Toggle.CreateInfo());

        Assert.True(on.Checked);
        Assert.Equal(PseudoStates.Checked, on.States & PseudoStates.Checked);
        Assert.Null(on.CheckedChanged);
        Assert.False(off.Checked);
        Assert.Equal(PseudoStates.None, off.States & PseudoStates.Checked);

        // A start value of true does not call a delegate that the caller sets after the constructor.
        int calls = 0;
        on.CheckedChanged = _ => calls++;
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// A release over another element, a release after a press elsewhere, and a right or middle button do not flip
    /// the toggle (A1). A left click after them still flips it.
    /// </summary>
    [Fact]
    public void AReleaseElsewhereOrARightButtonDoesNotFlip()
    {
        WidgetTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Toggle.CheckedChanged = _ => calls++;

        // A press on the toggle and a release over another element.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverOther, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());
        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, calls);

        // A press on another element and a release over the toggle.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverOther, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());
        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, calls);

        // A right and a middle click.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Middle));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Middle));
        Assert.Equal(4, tree.Context.DrainEvents());
        Assert.False(tree.Toggle.Checked);
        Assert.Equal(0, calls);

        // A left click flips.
        tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Left));
        Assert.Equal(2, tree.Context.DrainEvents());
        Assert.True(tree.Toggle.Checked);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// <see cref="Toggle.Checked"/> reads the state bit: a clear of the bit through <see cref="Element.States"/> reads
    /// back as <see langword="false"/>. The type id is <c>Toggle</c>, and the toggle is focusable.
    /// </summary>
    [Fact]
    public void CheckedReadsTheStateBit()
    {
        Toggle toggle = new(new Toggle.CreateInfo { Checked = true });
        Assert.True(toggle.Checked);

        toggle.States &= ~PseudoStates.Checked;
        Assert.False(toggle.Checked);

        toggle.States |= PseudoStates.Checked | PseudoStates.Hover;
        Assert.True(toggle.Checked);

        // A set keeps the other bits.
        toggle.Checked = false;
        Assert.Equal(PseudoStates.Hover, toggle.States);

        Assert.Equal(ElementType.Intern("Toggle"), toggle.TypeId);
        Assert.True(toggle.IsFocusable);
    }
}
