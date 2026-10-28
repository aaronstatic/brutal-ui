using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation gate of the 1000-element tree with each widget type (the thirteenth gate). After warm-up,
/// <c>Update</c> and <c>Draw</c> on that tree, and the full input path of each widget, must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The tree is <see cref="ThousandWidgetTree"/>: the 1000-element tree with a button, a disabled button, a toggle, a
/// slider, a text field, and a scroll view in the footer. The context has a measurer, and it holds the sheet of the base
/// tree and the sheet of the 7 widget state rules.
/// </para>
/// <para>
/// The frame gate gives the focus to the text field. Each frame marks <see cref="DirtyFlags.Style"/> on each element of
/// <see cref="ThousandWidgetTree.All"/> and runs <c>Update</c> and <c>Draw</c>. Thus the style pass restyles 1000
/// elements against both sheets, the caret pass runs, and the blink of the caret turns in the measure.
/// </para>
/// <para>
/// Each input step sets the offset of the scroll view to (0, 0) from code. Then it pushes the events of 7 groups, in
/// this order: a click, a scroll, a char, and an Enter at the button. The same 4 events at the disabled button. The same
/// at the toggle, with a Space in place of the Enter. A drag of the slider from 25 % to 75 % of its width, a scroll, a
/// char, and a Right. A scroll at the view and a drag of 20 pixels up. A Tab and an Enter. Last, a click at the text
/// field, a scroll, a char, a Backspace, an Enter, and a click inside the text. Each event has no target, so each key
/// and each char goes to the focus owner that the press of its group set. Then the step runs <c>Update</c> and
/// <c>Draw</c>. The step leaves the text, the toggle, and the caret as they were, so each step starts from the same
/// state.
/// </para>
/// <para>
/// The text field comes last, so it holds the focus and a pending press x at the caret pass of the step. Thus the
/// caret pass maps the press with <c>TextField.IndexAtX</c> in the measure. The footer has a press handler. The presses
/// at the button, at the disabled button, and at the toggle walk on to it, so a lost group changes its count.
/// </para>
/// <para>
/// Each gate calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before the measure. The measured bodies
/// create no lambda, no string, no boxed value, and no Assert. The test adds no warm-up round to hide an allocation.
/// </para>
/// </remarks>
public sealed class ThousandWidgetAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int FramesPerRound = 9;
    private const float SliderPressRatio = 0.25f;
    private const float SliderMoveRatio = 0.75f;
    private const float DragDistance = 20f;
    private const uint CharA = 'a';

    /// <summary>The caret index that the press inside the text gives: the boundary after <c>fi</c> of <c>field</c>.</summary>
    private const int FieldTextIndex = 2;

    /// <summary>The part of the next glyph that the press inside the text adds to the x of the boundary.</summary>
    private const float FieldTextBias = 0.25f;

    private static readonly float2 ScrollDown = new(0f, -1f);

    /// <summary>
    /// Counts the widget calls and the footer presses. Its methods are the targets of the sender delegates and of the
    /// press slot of the footer.
    /// </summary>
    private sealed class Counter
    {
        public int Clicks;
        public int FooterPresses;
        public int DisabledClicks;
        public int Flips;
        public int Values;
        public int Scrolls;
        public int TextChanges;
        public int Submits;

        public void OnClicked(Button button)
        {
            Clicks++;
        }

        public void OnDisabledClicked(Button button)
        {
            DisabledClicks++;
        }

        public void OnFlipped(Toggle toggle)
        {
            Flips++;
        }

        public void OnValue(Slider slider)
        {
            Values++;
        }

        public void OnScrolled(ScrollView view)
        {
            Scrolls++;
        }

        public void OnTextChanged(TextField field)
        {
            TextChanges++;
        }

        public void OnSubmitted(TextField field)
        {
            Submits++;
        }

        /// <summary>
        /// The press handler of the footer. A press that no widget stops walks on to it, for example a press on the
        /// disabled button.
        /// </summary>
        /// <param name="e">The press event.</param>
        public void OnFooterPressed(ref PointerEvent e)
        {
            FooterPresses++;
        }
    }

    /// <summary>The tree and the events that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required ThousandWidgetTree Tree { get; init; }

        public required Counter Counter { get; init; }

        public required CharEvent CharA;
        public required KeyEvent EnterDown;
        public required KeyEvent SpaceDown;
        public required KeyEvent RightDown;
        public required KeyEvent BackspaceDown;
        public required KeyEvent TabDown;

        public required PointerEvent ButtonPress;
        public required PointerEvent ButtonRelease;
        public required ScrollEvent ButtonScroll;

        public required PointerEvent DisabledPress;
        public required PointerEvent DisabledRelease;
        public required ScrollEvent DisabledScroll;

        public required PointerEvent TogglePress;
        public required PointerEvent ToggleRelease;
        public required ScrollEvent ToggleScroll;

        public required PointerEvent SliderPress;
        public required PointerEvent SliderMove;
        public required PointerEvent SliderRelease;
        public required ScrollEvent SliderScroll;

        public required PointerEvent FieldPress;
        public required PointerEvent FieldRelease;
        public required ScrollEvent FieldScroll;
        public required PointerEvent FieldTextPress;
        public required PointerEvent FieldTextRelease;

        public required ScrollEvent ViewScroll;
        public required PointerEvent ViewPress;
        public required PointerEvent ViewMove;
        public required PointerEvent ViewRelease;
    }

    private static Scene BuildScene()
    {
        ThousandWidgetTree tree = ThousandWidgetTree.Build();

        Counter counter = new();
        tree.Button.Clicked = counter.OnClicked;
        tree.DisabledButton.Clicked = counter.OnDisabledClicked;
        tree.Toggle.CheckedChanged = counter.OnFlipped;
        tree.Slider.ValueChanged = counter.OnValue;
        tree.ScrollView.ScrollChanged = counter.OnScrolled;
        tree.TextField.TextChanged = counter.OnTextChanged;
        tree.TextField.Submitted = counter.OnSubmitted;

        // The footer holds each widget. A press that no widget stops walks on to its press slot.
        Element footer = tree.Button.Parent
            ?? throw new InvalidOperationException("The button of the widget tree must have a parent.");
        footer.OnPointerDown = counter.OnFooterPressed;

        // The press inside the text: a quarter glyph after the boundary of FieldTextIndex, so IndexAtX gives that index.
        TextField field = tree.TextField;
        float4 fieldContent = field.Layout.ContentBox(ThousandWidgetTree.ViewportRect(field));
        ReadOnlySpan<byte> fieldText = field.Text;
        float2 prefix = tree.Renderer.MeasureText(field.Style.Font, field.Style.FontSize, fieldText[..FieldTextIndex], 0f);
        float2 glyph = tree.Renderer.MeasureText(
            field.Style.Font,
            field.Style.FontSize,
            fieldText[FieldTextIndex..(FieldTextIndex + 1)],
            0f);
        float2 fieldTextPoint = new(fieldContent.X + prefix.X + (glyph.X * FieldTextBias), tree.TextFieldCenter.Y);

        float4 slider = ThousandWidgetTree.ViewportRect(tree.Slider);
        float sliderY = tree.SliderCenter.Y;
        float2 sliderPress = new(slider.X + (slider.Z * SliderPressRatio), sliderY);
        float2 sliderMove = new(slider.X + (slider.Z * SliderMoveRatio), sliderY);
        float2 viewMove = new(tree.ScrollViewCenter.X, tree.ScrollViewCenter.Y - DragDistance);

        return new Scene
        {
            Tree = tree,
            Counter = counter,
            CharA = PointerTree.Char(CharA),
            EnterDown = PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None),
            SpaceDown = PointerTree.Key(UiEventType.KeyDown, WidgetInput.SpaceKey, UiModifiers.None),
            RightDown = PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None),
            BackspaceDown = PointerTree.Key(UiEventType.KeyDown, WidgetInput.BackspaceKey, UiModifiers.None),
            TabDown = PointerTree.Tab(shift: false),
            ButtonPress = PointerTree.Down(tree.ButtonCenter, PointerButton.Left),
            ButtonRelease = PointerTree.Up(tree.ButtonCenter, PointerButton.Left),
            ButtonScroll = PointerTree.Scroll(tree.ButtonCenter, ScrollDown),
            DisabledPress = PointerTree.Down(tree.DisabledButtonCenter, PointerButton.Left),
            DisabledRelease = PointerTree.Up(tree.DisabledButtonCenter, PointerButton.Left),
            DisabledScroll = PointerTree.Scroll(tree.DisabledButtonCenter, ScrollDown),
            TogglePress = PointerTree.Down(tree.ToggleCenter, PointerButton.Left),
            ToggleRelease = PointerTree.Up(tree.ToggleCenter, PointerButton.Left),
            ToggleScroll = PointerTree.Scroll(tree.ToggleCenter, ScrollDown),
            SliderPress = PointerTree.Down(sliderPress, PointerButton.Left),
            SliderMove = PointerTree.Move(sliderMove),
            SliderRelease = PointerTree.Up(sliderMove, PointerButton.Left),
            SliderScroll = PointerTree.Scroll(tree.SliderCenter, ScrollDown),
            FieldPress = PointerTree.Down(tree.TextFieldCenter, PointerButton.Left),
            FieldRelease = PointerTree.Up(tree.TextFieldCenter, PointerButton.Left),
            FieldScroll = PointerTree.Scroll(tree.TextFieldCenter, ScrollDown),
            FieldTextPress = PointerTree.Down(fieldTextPoint, PointerButton.Left),
            FieldTextRelease = PointerTree.Up(fieldTextPoint, PointerButton.Left),
            ViewScroll = PointerTree.Scroll(tree.ScrollViewCenter, ScrollDown),
            ViewPress = PointerTree.Down(tree.ScrollViewCenter, PointerButton.Left),
            ViewMove = PointerTree.Move(viewMove),
            ViewRelease = PointerTree.Up(viewMove, PointerButton.Left),
        };
    }

    /// <summary>The measured body of the frame gate. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void Frame(Scene scene)
    {
        ThousandWidgetTree tree = scene.Tree;
        Element[] all = tree.All;
        for (int i = 0; i < all.Length; i++)
        {
            all[i].MarkDirty(DirtyFlags.Style);
        }

        tree.Context.Update(0.016f);
        tree.Context.Draw(tree.Renderer);
    }

    private static void RunFrames(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            Frame(scene);
        }
    }

    /// <summary>The measured body of the input gate. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void InputStep(Scene scene)
    {
        ThousandWidgetTree tree = scene.Tree;
        UiContext context = tree.Context;

        // Each step starts from the offset (0, 0), so the scroll and the drag of group 6 change the offset each time.
        tree.ScrollView.ScrollOffset = default;

        // 1. The button: a click, a scroll, a char, and an Enter. The press gives the focus to the button.
        context.PushEvent(in scene.ButtonPress);
        context.PushEvent(in scene.ButtonRelease);
        context.PushEvent(in scene.ButtonScroll);
        context.PushEvent(in scene.CharA);
        context.PushEvent(in scene.EnterDown);

        // 2. The disabled button: the same 4 events. The press clears the focus, so the char and the key get no walk.
        context.PushEvent(in scene.DisabledPress);
        context.PushEvent(in scene.DisabledRelease);
        context.PushEvent(in scene.DisabledScroll);
        context.PushEvent(in scene.CharA);
        context.PushEvent(in scene.EnterDown);

        // 3. The toggle: a click, a scroll, a char, and a Space. The click and the Space flip it back.
        context.PushEvent(in scene.TogglePress);
        context.PushEvent(in scene.ToggleRelease);
        context.PushEvent(in scene.ToggleScroll);
        context.PushEvent(in scene.CharA);
        context.PushEvent(in scene.SpaceDown);

        // 4. The slider: a drag from 25 % to 75 % of the width, a scroll, a char, and a Right.
        context.PushEvent(in scene.SliderPress);
        context.PushEvent(in scene.SliderMove);
        context.PushEvent(in scene.SliderRelease);
        context.PushEvent(in scene.SliderScroll);
        context.PushEvent(in scene.CharA);
        context.PushEvent(in scene.RightDown);

        // 6. The scroll view: a scroll down and a drag of 20 pixels up. The press clears the focus.
        context.PushEvent(in scene.ViewScroll);
        context.PushEvent(in scene.ViewPress);
        context.PushEvent(in scene.ViewMove);
        context.PushEvent(in scene.ViewRelease);

        // 7. A Tab with no owner moves the focus to the first focusable element, the button. The Enter clicks it.
        context.PushEvent(in scene.TabDown);
        context.PushEvent(in scene.EnterDown);

        // 5. The text field, last: a click, a scroll, a char, a Backspace of that char, and an Enter. Then a click inside
        // the text. The field keeps the focus and the press x, so the caret pass of this step maps the x.
        context.PushEvent(in scene.FieldPress);
        context.PushEvent(in scene.FieldRelease);
        context.PushEvent(in scene.FieldScroll);
        context.PushEvent(in scene.CharA);
        context.PushEvent(in scene.BackspaceDown);
        context.PushEvent(in scene.EnterDown);
        context.PushEvent(in scene.FieldTextPress);
        context.PushEvent(in scene.FieldTextRelease);

        context.Update(0.016f);
        context.Draw(tree.Renderer);
    }

    private static void RunInputSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            InputStep(scene);
        }
    }

    /// <summary>
    /// The tree holds 1000 elements with one element of each widget type and one disabled button. Each widget center
    /// hits its widget, and the content of the scroll view is taller than the view.
    /// </summary>
    [Fact]
    public void TheTreeHoldsEachWidget()
    {
        ThousandWidgetTree tree = ThousandWidgetTree.Build();

        Assert.Equal(ThousandElementTree.ElementCount, tree.All.Length);
        Assert.Equal(ThousandElementTree.ElementCount, ThousandWidgetTree.CountSubtree(tree.Root));
        Assert.Same(tree.Root, tree.All[0]);

        int buttons = 0;
        int disabledButtons = 0;
        int toggles = 0;
        int sliders = 0;
        int fields = 0;
        int views = 0;
        for (int i = 0; i < tree.All.Length; i++)
        {
            Element element = tree.All[i];
            Assert.NotNull(element);
            switch (element)
            {
                case Button:
                    buttons++;
                    disabledButtons += (element.States & PseudoStates.Disabled) != 0 ? 1 : 0;
                    break;
                case Toggle:
                    toggles++;
                    break;
                case Slider:
                    sliders++;
                    break;
                case TextField:
                    fields++;
                    break;
                case ScrollView:
                    views++;
                    break;
                default:
                    break;
            }
        }

        Assert.Equal(2, buttons);
        Assert.Equal(1, disabledButtons);
        Assert.Equal(1, toggles);
        Assert.Equal(1, sliders);
        Assert.Equal(1, fields);
        Assert.Equal(1, views);
        Assert.Equal(PseudoStates.Disabled, tree.DisabledButton.States & PseudoStates.Disabled);

        UiContext context = tree.Context;
        Assert.Same(tree.Button, context.HitTest(tree.ButtonCenter));
        Assert.Same(tree.DisabledButton, context.HitTest(tree.DisabledButtonCenter));
        Assert.Same(tree.Toggle, context.HitTest(tree.ToggleCenter));
        Assert.Same(tree.Slider, context.HitTest(tree.SliderCenter));
        Assert.Same(tree.TextField, context.HitTest(tree.TextFieldCenter));
        Assert.Same(tree.ScrollView, context.HitTest(tree.ScrollViewCenter));

        Assert.Equal(ThousandWidgetTree.ViewHeight, tree.ScrollView.Layout.Rect.W);
        Assert.Equal(new float2(0f, ThousandWidgetTree.ViewMaxOffset), tree.ScrollView.MaxOffset());
    }

    /// <summary>
    /// <c>Update</c> and <c>Draw</c> on the tree with each widget allocate nothing after warm-up. The text field holds
    /// the focus, and each frame restyles each element.
    /// </summary>
    [Fact]
    public void UpdateAndDrawAllocateNothingWithEachWidget()
    {
        Scene scene = BuildScene();
        Assert.True(scene.Tree.Context.SetFocus(scene.Tree.TextField));
        for (int round = 0; round < WarmUpRounds; round++)
        {
            RunFrames(scene, FramesPerRound);
        }

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunFrames(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
        Assert.Same(scene.Tree.TextField, scene.Tree.Context.FocusedElement);
    }

    /// <summary>
    /// The input path of each widget allocates nothing after warm-up: the pointer, scroll, key, and char events of the
    /// 7 groups, the drain, <c>Update</c>, and <c>Draw</c>.
    /// </summary>
    [Fact]
    public void TheWidgetInputPathAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunInputSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunInputSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured body does real work. One input step clicks the button three times and the disabled button never,
    /// flips the toggle two times, changes the slider value three times, scrolls the view two times, changes the text
    /// two times, and submits it one time. Three presses walk on to the footer. The text stays <c>field</c>, the text
    /// field holds the focus, the caret pass maps the press inside the text, the view offset is (0, 60), and the frame
    /// clips the view and the text field.
    /// </summary>
    /// <remarks>
    /// The third click comes from the Enter after the Tab, so a lost Tab loses it. The footer presses come from the
    /// button, the disabled button, and the toggle. The slider, the view, and the field stop their presses. Thus a lost
    /// group 2 gives 2 footer presses. The caret starts at the end of the text, and only the press map moves it to
    /// <see cref="FieldTextIndex"/>.
    /// </remarks>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        ThousandWidgetTree tree = scene.Tree;

        // The caret starts at the end of the text, so a caret at FieldTextIndex comes from the press map only.
        Assert.Equal(tree.TextField.Text.Length, tree.TextField.CaretIndex);
        Assert.NotEqual(FieldTextIndex, tree.TextField.CaretIndex);

        InputStep(scene);

        Counter counter = scene.Counter;
        Assert.Equal(3, counter.Clicks);
        Assert.Equal(3, counter.FooterPresses);
        Assert.Equal(0, counter.DisabledClicks);
        Assert.Equal(2, counter.Flips);
        Assert.Equal(3, counter.Values);
        Assert.Equal(2, counter.Scrolls);
        Assert.Equal(2, counter.TextChanges);
        Assert.Equal(1, counter.Submits);
        Assert.False(tree.Toggle.Checked);
        Assert.True(tree.TextField.Text.SequenceEqual("field"u8));
        Assert.Same(tree.TextField, tree.Context.FocusedElement);
        Assert.Equal(FieldTextIndex, tree.TextField.CaretIndex);
        Assert.Equal(FieldTextIndex, tree.TextField.AnchorIndex);
        Assert.Equal(new float2(0f, 60f), tree.ScrollView.ScrollOffset);

        float4 viewRect = ThousandWidgetTree.ViewportRect(tree.ScrollView);
        float4 fieldRect = ThousandWidgetTree.ViewportRect(tree.TextField);
        bool viewClip = false;
        bool fieldClip = false;
        ReadOnlySpan<DrawCall> calls = tree.Renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind != DrawCallKind.PushClip)
            {
                continue;
            }

            viewClip |= calls[i].Rect.Equals(viewRect);
            fieldClip |= calls[i].Rect.Equals(fieldRect);
        }

        Assert.True(viewClip);
        Assert.True(fieldClip);
    }
}
