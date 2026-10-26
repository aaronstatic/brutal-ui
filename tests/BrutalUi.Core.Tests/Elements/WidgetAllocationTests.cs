using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation gate of the widgets. After warm-up, <c>Update</c> and <c>Draw</c> on a tree of 20 buttons and 20
/// toggles, the full widget input path, and a text change on the buttons must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is a column root with 20 <see cref="Button"/> elements that have a text, and 20 <see cref="Toggle"/>
/// elements. A sheet holds the rules <c>Button:hover</c>, <c>Button:active</c>, <c>Toggle:checked</c>, and
/// <c>:disabled</c>. The last button and the last toggle hold <see cref="PseudoStates.Disabled"/>, so the
/// <c>:disabled</c> rule matches. The test sends no input to them.
/// </para>
/// <para>
/// Each step of the input test pushes 6 events: a press and a release over the first button, an Enter key down to the
/// focused button, a press and a release over the first toggle, and a Space key down to the focused toggle. The step
/// then runs <c>Update</c> and <c>Draw</c>. The step clicks the button 2 times and flips the toggle 2 times, so the
/// toggle ends unchecked.
/// </para>
/// <para>
/// Each gate calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before the measure. The measured bodies
/// create no lambda, no string, no boxed value, and no Assert. <c>TextStep</c> reads a string that the scene built.
/// </para>
/// </remarks>
public sealed class WidgetAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int WidgetCount = 20;
    private const int EnterKey = 257;
    private const int SpaceKey = 32;
    private const float ViewportWidth = 800f;
    private const float ViewportHeight = 3000f;
    private const float ToggleSize = 20f;

    private static readonly Rgba8 White = new(255, 255, 255, 255);
    private static readonly Rgba8 PlainFill = new(32, 32, 32, 255);
    private static readonly Rgba8 HoverFill = new(48, 96, 192, 255);
    private static readonly Rgba8 ActiveFill = new(200, 64, 64, 255);
    private static readonly Rgba8 CheckedFill = new(224, 64, 32, 255);
    private static readonly Rgba8 DisabledFill = new(96, 96, 96, 255);

    private static ReadOnlySpan<byte> FirstText => "Open it"u8;

    private static ReadOnlySpan<byte> SecondText => "Close x"u8;

    /// <summary>Counts the clicks and the flips. Its methods are the targets of the sender delegates.</summary>
    private sealed class Counter
    {
        public int Clicks;
        public int Flips;

        public void OnClicked(Button button)
        {
            Clicks++;
        }

        public void OnChanged(Toggle toggle)
        {
            Flips++;
        }
    }

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        public required Counter Counter { get; init; }

        public required Button[] Buttons { get; init; }

        public required Toggle[] Toggles { get; init; }

        public required string FirstString { get; init; }

        public required string SecondString { get; init; }

        public required PointerEvent DownButton;
        public required PointerEvent UpButton;
        public required PointerEvent DownToggle;
        public required PointerEvent UpToggle;
        public required KeyEvent EnterDown;
        public required KeyEvent SpaceDown;

        /// <summary>The number of text steps that ran. An even number takes the first text.</summary>
        public int TextSteps;

        /// <summary>The number of walks of the last step. It keeps the compiler from removing the drain.</summary>
        public int LastWalks;
    }

    private static Scene BuildScene()
    {
        RecordingRenderer renderer = new();
        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer })
        {
            ViewportSize = new float2(ViewportWidth, ViewportHeight),
        };

        ElementTypeId buttonType = ElementType.Intern("Button");
        ElementTypeId toggleType = ElementType.Intern("Toggle");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = buttonType }),
                new StyleProperties { BackgroundColor = PlainFill })
            .Add(
                Selector.For(new CompoundSelector { Type = buttonType, States = PseudoStates.Hover }),
                new StyleProperties { BackgroundColor = HoverFill })
            .Add(
                Selector.For(new CompoundSelector { Type = buttonType, States = PseudoStates.Active }),
                new StyleProperties { BackgroundColor = ActiveFill })
            .Add(
                Selector.For(new CompoundSelector { Type = toggleType }),
                new StyleProperties { BackgroundColor = PlainFill })
            .Add(
                Selector.For(new CompoundSelector { Type = toggleType, States = PseudoStates.Checked }),
                new StyleProperties { BackgroundColor = CheckedFill })
            .Add(
                Selector.For(new CompoundSelector { States = PseudoStates.Disabled }),
                new StyleProperties { BackgroundColor = DisabledFill })
            .Build();
        context.AddSheet(sheet);

        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Color = White;
        root.Inline.FontSize = 16;
        context.AddRoot(root);

        Counter counter = new();
        Action<Button> onClicked = counter.OnClicked;
        Action<Toggle> onChanged = counter.OnChanged;

        Button[] buttons = new Button[WidgetCount];
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = new(new Button.CreateInfo { Text = System.Text.Encoding.UTF8.GetString(FirstText) });
            button.Inline.Padding = LengthEdges.All(4);
            button.Clicked = onClicked;
            root.Children.Add(button);
            buttons[i] = button;
        }

        Toggle[] toggles = new Toggle[WidgetCount];
        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = new(new Toggle.CreateInfo());
            toggle.Inline.Width = ToggleSize;
            toggle.Inline.Height = ToggleSize;
            toggle.CheckedChanged = onChanged;
            root.Children.Add(toggle);
            toggles[i] = toggle;
        }

        // The disabled rule matches the last widgets. The step sends no input to them.
        buttons[WidgetCount - 1].States |= PseudoStates.Disabled;
        toggles[WidgetCount - 1].States |= PseudoStates.Disabled;

        context.Update(0f);

        float4 buttonRect = PointerTree.ViewportRect(buttons[0]);
        float4 toggleRect = PointerTree.ViewportRect(toggles[0]);
        float2 overButton = new(buttonRect.X + 2f, buttonRect.Y + 2f);
        float2 overToggle = new(toggleRect.X + 2f, toggleRect.Y + 2f);

        return new Scene
        {
            Context = context,
            Renderer = renderer,
            Counter = counter,
            Buttons = buttons,
            Toggles = toggles,
            FirstString = System.Text.Encoding.UTF8.GetString(FirstText),
            SecondString = System.Text.Encoding.UTF8.GetString(SecondText),
            DownButton = PointerTree.Down(overButton, PointerButton.Left),
            UpButton = PointerTree.Up(overButton, PointerButton.Left),
            DownToggle = PointerTree.Down(overToggle, PointerButton.Left),
            UpToggle = PointerTree.Up(overToggle, PointerButton.Left),
            EnterDown = PointerTree.Key(UiEventType.KeyDown, EnterKey, UiModifiers.None),
            SpaceDown = PointerTree.Key(UiEventType.KeyDown, SpaceKey, UiModifiers.None),
        };
    }

    /// <summary>The measured body of the first test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void Frame(Scene scene)
    {
        scene.Context.Update(0.016f);
        scene.Context.Draw(scene.Renderer);
    }

    private static void RunFrames(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            Frame(scene);
        }
    }

    /// <summary>The measured body of the second test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void InputStep(Scene scene)
    {
        UiContext context = scene.Context;

        // The press focuses the button, so the Enter key goes to it.
        context.PushEvent(in scene.DownButton);
        context.PushEvent(in scene.UpButton);
        context.PushEvent(in scene.EnterDown);

        // The press focuses the toggle, so the Space key goes to it.
        context.PushEvent(in scene.DownToggle);
        context.PushEvent(in scene.UpToggle);
        context.PushEvent(in scene.SpaceDown);

        scene.LastWalks = context.DrainEvents();
        context.Update(0.016f);
        context.Draw(scene.Renderer);
    }

    private static void RunInputSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            InputStep(scene);
        }
    }

    /// <summary>The measured body of the third test. It uses no lambda, no boxing, and no Assert.</summary>
    private static void TextStep(Scene scene)
    {
        string text = (scene.TextSteps++ & 1) == 0 ? scene.SecondString : scene.FirstString;
        Button[] buttons = scene.Buttons;
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].SetText(text);
        }

        scene.Context.Update(0.016f);
        scene.Context.Draw(scene.Renderer);
    }

    private static void RunTextSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            TextStep(scene);
        }
    }

    /// <summary><c>Update</c> and <c>Draw</c> on a tree of widgets allocate nothing after warm-up.</summary>
    [Fact]
    public void UpdateAndDrawAllocateNothingWithWidgets()
    {
        Scene scene = BuildScene();
        RunFrames(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunFrames(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The full widget input path allocates nothing after warm-up: the press and release over a button, an Enter key,
    /// the press and release over a toggle, a Space key, the drain, <c>Update</c>, and <c>Draw</c>.
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
    /// <see cref="Button.SetText(string?)"/> with two texts of the same length, in turn, allocates nothing after
    /// warm-up.
    /// </summary>
    [Fact]
    public void SetTextOnAButtonAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunTextSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunTextSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured bodies do real work. One input step gives 2 clicks and 2 flips, and a frame draws one text for
    /// each button. One text step changes the texts and measures the buttons again.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        Assert.Equal(FirstText.Length, SecondText.Length);
        Assert.True(scene.Buttons[0].IsFocusable);
        Assert.Equal(PseudoStates.Disabled, scene.Buttons[WidgetCount - 1].States & PseudoStates.Disabled);
        Assert.Equal(PseudoStates.Disabled, scene.Toggles[WidgetCount - 1].States & PseudoStates.Disabled);
        Assert.Same(scene.Buttons[0], scene.Context.HitTest(new float2(
            PointerTree.ViewportRect(scene.Buttons[0]).X + 2f,
            PointerTree.ViewportRect(scene.Buttons[0]).Y + 2f)));
        Assert.Same(scene.Toggles[0], scene.Context.HitTest(new float2(
            PointerTree.ViewportRect(scene.Toggles[0]).X + 2f,
            PointerTree.ViewportRect(scene.Toggles[0]).Y + 2f)));

        InputStep(scene);

        // The walks: 3 pointer and key events for the button, and 3 for the toggle.
        Assert.Equal(6, scene.LastWalks);
        Assert.Equal(2, scene.Counter.Clicks);
        Assert.Equal(2, scene.Counter.Flips);
        Assert.False(scene.Toggles[0].Checked);
        Assert.Same(scene.Toggles[0], scene.Context.FocusedElement);

        int texts = 0;
        ReadOnlySpan<DrawCall> calls = scene.Renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            texts += calls[i].Kind == DrawCallKind.Text ? 1 : 0;
        }

        Assert.Equal(WidgetCount, texts);

        int measuresBefore = scene.Renderer.MeasureCount;
        TextStep(scene);
        Assert.True(scene.Renderer.MeasureCount >= measuresBefore + WidgetCount);
        Assert.True(scene.Buttons[0].Text.SequenceEqual(SecondText));
        Assert.True(scene.Buttons[WidgetCount - 1].Text.SequenceEqual(SecondText));
    }
}
