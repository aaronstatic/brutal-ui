using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the arrow keys of <see cref="Slider"/> (Decision 8): a Left or Right key down on the focused slider moves the
/// value by one step, or by one percent of the range for a step of 0. A modifier, another key, a key up, and a key from a
/// focusable child give no move.
/// </summary>
/// <remarks>
/// Each test pushes a key event with no caller target, so the key route gives the event to the focus owner. The scene is
/// <see cref="SliderTree"/>. The tree interns no class, so the tests share no class id.
/// </remarks>
public sealed class SliderKeyTests
{
    /// <summary>The GLFW key code of the up arrow key.</summary>
    private const int UpKey = 265;

    /// <summary>The GLFW key code of the down arrow key.</summary>
    private const int DownKey = 264;

    /// <summary>Creates a settled tree with the range 0 to 100, a step, and a start value.</summary>
    private static SliderTree Make(float step, float value)
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = step, Value = value });
        tree.Settle();
        return tree;
    }

    /// <summary>
    /// The demo of phase 6. The slider has the range 0 to 100, the step 10, the value 50, and the focus. A Right key down
    /// and one <see cref="UiContext.Update(float)"/> give the value 60 and one call of
    /// <see cref="Slider.ValueChanged"/>.
    /// </summary>
    [Fact]
    public void ARightArrowMovesTheValueByOneStep()
    {
        // GLFW_KEY_RIGHT and GLFW_KEY_LEFT.
        Assert.Equal(262, WidgetInput.RightKey);
        Assert.Equal(263, WidgetInput.LeftKey);

        SliderTree tree = Make(10f, 50f);
        Assert.True(tree.Context.SetFocus(tree.Slider));

        int calls = 0;
        Slider? sender = null;
        tree.Slider.ValueChanged = s =>
        {
            calls++;
            sender = s;
        };

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None));
        tree.Context.Update(0f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: Right on the focused slider, Value = " + tree.Slider.Value + ", calls = " + calls);

        Assert.Equal(60f, tree.Slider.Value);
        Assert.Equal(1, calls);
        Assert.Same(tree.Slider, sender);
        Assert.Same(tree.Slider, tree.Context.FocusedElement);
    }

    /// <summary>A Left key down moves the value back by one step.</summary>
    [Fact]
    public void ALeftArrowMovesTheValueBack()
    {
        SliderTree tree = Make(10f, 50f);
        Assert.True(tree.Context.SetFocus(tree.Slider));

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(40f, tree.Slider.Value);
        Assert.Equal(1, calls);
    }

    /// <summary>A step of 0 moves the value by one percent of the range.</summary>
    [Fact]
    public void AStepOfZeroMovesOnePercentOfTheRange()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 200f, Step = 0f, Value = 50f });
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Slider));

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(52f, tree.Slider.Value);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(48f, tree.Slider.Value);
        Assert.Equal(3, calls);
    }

    /// <summary>
    /// A Right key at the end of the range gives no call, and the key still stops the walk, because the slider uses it.
    /// </summary>
    [Fact]
    public void TheArrowStopsAtTheEndOfTheRange()
    {
        SliderTree tree = Make(10f, 100f);
        Assert.True(tree.Context.SetFocus(tree.Slider));

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(100f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(0, rootDowns);

        // The same holds at the start of the range for a Left key.
        tree.Slider.Value = 0f;
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(0f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(0, rootDowns);
    }

    /// <summary>A repeat key down moves the value again.</summary>
    [Fact]
    public void ARepeatMovesTheValue()
    {
        SliderTree tree = Make(10f, 50f);
        Assert.True(tree.Context.SetFocus(tree.Slider));

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        KeyEvent first = PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None);
        tree.Context.PushEvent(first);
        KeyEvent repeat = PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None);
        repeat.IsRepeat = true;
        tree.Context.PushEvent(repeat);
        tree.Context.PushEvent(repeat);
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.Equal(80f, tree.Slider.Value);
        Assert.Equal(3, calls);
    }

    /// <summary>
    /// A Control, an Alt, or a Super modifier gives no move, and the key walks on to the root. A Shift modifier moves.
    /// </summary>
    [Fact]
    public void AModifierBlocksTheArrow()
    {
        SliderTree tree = Make(10f, 50f);
        Assert.True(tree.Context.SetFocus(tree.Slider));

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.Control));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.Alt));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.Super));
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(3, rootDowns);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.Shift));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(60f, tree.Slider.Value);
        Assert.Equal(1, calls);
        Assert.Equal(3, rootDowns);
    }

    /// <summary>
    /// Up, Down, Space, Enter, and a key up of Right give no move. Each one walks on to the root.
    /// </summary>
    [Fact]
    public void OtherKeysDoNothing()
    {
        SliderTree tree = Make(10f, 50f);
        Assert.True(tree.Context.SetFocus(tree.Slider));

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootDowns = 0;
        int rootUps = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;
        tree.Root.OnKeyUp = (ref KeyEvent e) => rootUps++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, UpKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, DownKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, WidgetInput.RightKey, UiModifiers.None));
        Assert.Equal(5, tree.Context.DrainEvents());

        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(4, rootDowns);
        Assert.Equal(1, rootUps);

        // A copied key handler in OnKeyUp must still ignore the key up events.
        tree.Slider.OnKeyUp = tree.Slider.OnKeyDown;
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, WidgetInput.RightKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// A focusable child of the slider owns the focus. A Right key down goes to the child, and it bubbles to the slider.
    /// The target is the child, so the slider gives no move, and the key walks on to the root.
    /// </summary>
    [Fact]
    public void AKeyFromAFocusableChildDoesNothing()
    {
        SliderTree tree = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = 10f, Value = 50f });
        Element child = new() { Name = "child", IsFocusable = true };
        child.Inline.Width = 10;
        child.Inline.Height = 10;
        tree.Slider.Children.Add(child);
        tree.Settle();
        Assert.True(tree.Context.SetFocus(child));

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;
        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(50f, tree.Slider.Value);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootDowns);
        Assert.Same(child, tree.Context.FocusedElement);
    }

    /// <summary>
    /// A Tab with no owner focuses the slider, because <see cref="SliderTree.Lead"/> is not focusable. The next Right
    /// key moves the value.
    /// </summary>
    [Fact]
    public void TabThenRightMovesTheValue()
    {
        SliderTree tree = Make(10f, 50f);
        Assert.Null(tree.Context.FocusedElement);

        int calls = 0;
        tree.Slider.ValueChanged = s => calls++;

        tree.Context.PushEvent(PointerTree.Tab(shift: false));
        tree.Context.DrainEvents();
        Assert.Same(tree.Slider, tree.Context.FocusedElement);
        Assert.Equal(0, calls);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(60f, tree.Slider.Value);
        Assert.Equal(1, calls);
        Assert.Same(tree.Slider, tree.Context.FocusedElement);
    }
}
