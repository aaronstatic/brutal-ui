using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the disabled rule of <see cref="Slider"/> (Decision 11): a slider that holds
/// <see cref="PseudoStates.Disabled"/> takes no press, no drag, no key, and no capture, and it does not stop the walk.
/// A set from code still works.
/// </summary>
/// <remarks>
/// The drain drops a disabled focus owner, so the key test pushes the key with the slider as the caller target. The
/// scene is <see cref="SliderTree"/>: the value <c>v</c> of the range 0 to 100 sits at the x <c>50 + v</c>. The tests
/// intern no class, so they share no class id.
/// </remarks>
public sealed class SliderDisabledTests
{
    /// <summary>
    /// The demo of phase 7. A disabled slider holds the value 50. A left press at the value 25 and one
    /// <see cref="UiContext.Update(float)"/> give the old value and 0 calls of <see cref="Slider.ValueChanged"/>. The
    /// root gets the press, because the slider does not stop the walk.
    /// </summary>
    [Fact]
    public void ADisabledSliderIgnoresAPress()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 50f });
        tree.Slider.States |= PseudoStates.Disabled;
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootDowns = 0;
        tree.Root.OnPointerDown = (ref PointerEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press at 25 on a disabled slider, Value = " + tree.Slider.Value + ", calls = " + calls
            + ", root presses = " + rootDowns);

        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootDowns);
        Assert.Equal(PseudoStates.Disabled, tree.Slider.States & PseudoStates.Disabled);
    }

    /// <summary>
    /// A disable during a drag ends the drag: a move over the slider changes nothing and walks on to the root. An enable
    /// during the same press drags again.
    /// </summary>
    [Fact]
    public void ADisableDuringADragStopsTheDrag()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootMoves = 0;
        tree.Root.OnPointerMove = (ref PointerEvent e) => rootMoves++;

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);

        tree.Slider.States |= PseudoStates.Disabled;
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(60f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);
        Assert.Equal(1, rootMoves);

        tree.Slider.States &= ~PseudoStates.Disabled;
        tree.Context.PushEvent(PointerTree.Move(SliderTree.At(70f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(70f, tree.Slider.Value);
        Assert.Equal(2, calls);
        Assert.Equal(1, rootMoves);
    }

    /// <summary>
    /// A Left key and a Right key with the disabled slider as the caller target change nothing. The root gets both keys,
    /// because the slider does not stop the walk.
    /// </summary>
    [Fact]
    public void ADisabledSliderIgnoresTheArrows()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 50f });
        tree.Slider.States |= PseudoStates.Disabled;
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(KeyTo(tree.Slider, WidgetInput.RightKey));
        tree.Context.PushEvent(KeyTo(tree.Slider, WidgetInput.LeftKey));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(2, rootDowns);
    }

    /// <summary>
    /// A disable during a drag, then a move to <see cref="SliderTree.PastRight"/>: the slider takes no capture, so the
    /// move walks to the root, and the value stays.
    /// </summary>
    [Fact]
    public void ADisabledSliderCapturesNoMove()
    {
        SliderTree tree = new();
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootMoves = 0;
        Element? moveTarget = null;
        tree.Root.OnPointerMove = (ref PointerEvent e) =>
        {
            rootMoves++;
            moveTarget = e.Header.Target;
        };

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(1, calls);

        // With the capture, a move past the right end gives the maximum (see SliderDragTests).
        tree.Slider.States |= PseudoStates.Disabled;
        tree.Context.PushEvent(PointerTree.Move(SliderTree.PastRight));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(25f, tree.Slider.Value);
        Assert.Equal(1, calls);
        Assert.Equal(1, rootMoves);
        Assert.Same(tree.Root, moveTarget);
    }

    /// <summary>
    /// A set of <see cref="Slider.Value"/> and a call of <see cref="Slider.SetRange(float, float, float)"/> work on a
    /// disabled slider, because <see cref="PseudoStates.Disabled"/> blocks user input only. Both call no
    /// <see cref="Slider.ValueChanged"/>.
    /// </summary>
    [Fact]
    public void ACodeSetWorksOnADisabledSlider()
    {
        SliderTree tree = new();
        tree.Slider.States |= PseudoStates.Disabled;
        tree.Settle();

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Slider.Value = 40f;

        Assert.Equal(40f, tree.Slider.Value);
        Assert.Equal(0, calls);

        tree.Slider.SetRange(0f, 30f, 10f);

        Assert.Equal(0f, tree.Slider.Minimum);
        Assert.Equal(30f, tree.Slider.Maximum);
        Assert.Equal(10f, tree.Slider.Step);
        Assert.Equal(30f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(PseudoStates.Disabled, tree.Slider.States & PseudoStates.Disabled);
    }

    /// <summary>
    /// A press on a disabled slider takes no focus (the rule of keyboard-focus holds for a widget). The press clears the
    /// focus of another element, and the slider never holds <see cref="PseudoStates.Focus"/>.
    /// </summary>
    [Fact]
    public void APressOnADisabledSliderTakesNoFocus()
    {
        SliderTree tree = new();
        tree.Other.IsFocusable = true;
        tree.Slider.States |= PseudoStates.Disabled;
        tree.Settle();

        // With no focus owner.
        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Focus);

        tree.Context.PushEvent(PointerTree.Up(SliderTree.At(25f), PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        // With another element as the focus owner.
        Assert.True(tree.Context.SetFocus(tree.Other));

        tree.Context.PushEvent(PointerTree.Down(SliderTree.At(25f), PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Slider.States & PseudoStates.Focus);
        Assert.Equal(PseudoStates.None, tree.Other.States & PseudoStates.Focus);
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
