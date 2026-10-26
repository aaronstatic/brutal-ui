using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation gate of the slider (the tenth gate). After warm-up, <c>Update</c> and <c>Draw</c> on a tree of 20
/// sliders and the full slider input path must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is a column root of 400 x 400 with 20 <see cref="Slider"/> elements of 200 x 20. Each slider has the range
/// 0 to 100 and the step 0. A sheet holds the rules <c>Slider</c>, <c>Slider:hover</c>, <c>Slider:active</c>,
/// <c>Slider:focus</c>, and <c>:disabled</c>. The last slider holds <see cref="PseudoStates.Disabled"/>, so the
/// <c>:disabled</c> rule matches. The test sends no input to it.
/// </para>
/// <para>
/// Each step of the input test pushes 6 events to the first slider: a press at 25, a move to 60, a move past the right
/// end of the root, a release, a Left key, and a Right key. The move past the root walks to the slider through the
/// capture, so the gate also measures the capture branch of the pointer step. The release past the root has no target
/// and no walk, but it ends the press. The step then runs the drain,
/// <c>Update</c>, and <c>Draw</c>. Each step calls <see cref="Slider.ValueChanged"/> 5 times.
/// </para>
/// <para>
/// Each gate calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before the measure. The measured bodies
/// create no lambda, no string, no boxed value, and no Assert. The test adds no warm-up round to hide an allocation.
/// </para>
/// </remarks>
public sealed class SliderAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int SliderCount = 20;
    private const float ViewportWidth = 800f;
    private const float ViewportHeight = 600f;
    private const float RootSize = 400f;
    private const float SliderWidth = 200f;
    private const float SliderHeight = 20f;
    private const float PressX = 25f;
    private const float MoveX = 60f;
    private const float PastRootX = 500f;
    private const float RowY = 10f;

    private static readonly Rgba8 White = new(255, 255, 255, 255);
    private static readonly Rgba8 TrackFill = new(32, 32, 32, 255);
    private static readonly Rgba8 PlainThumb = new(220, 220, 220, 255);
    private static readonly Rgba8 HoverThumb = new(48, 96, 192, 255);
    private static readonly Rgba8 ActiveThumb = new(200, 64, 64, 255);
    private static readonly Rgba8 FocusThumb = new(64, 160, 96, 255);
    private static readonly Rgba8 DisabledThumb = new(96, 96, 96, 255);

    /// <summary>Counts the changes. Its method is the target of the sender delegate.</summary>
    private sealed class Counter
    {
        public int Changes;

        public void OnChanged(Slider slider)
        {
            Changes++;
        }
    }

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        public required Counter Counter { get; init; }

        public required Slider[] Sliders { get; init; }

        public required PointerEvent Press;
        public required PointerEvent MoveInside;
        public required PointerEvent MovePast;
        public required PointerEvent Release;
        public required KeyEvent LeftDown;
        public required KeyEvent RightDown;

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

        ElementTypeId sliderType = ElementType.Intern("Slider");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = sliderType }),
                new StyleProperties { BackgroundColor = TrackFill, Color = PlainThumb })
            .Add(
                Selector.For(new CompoundSelector { Type = sliderType, States = PseudoStates.Hover }),
                new StyleProperties { Color = HoverThumb })
            .Add(
                Selector.For(new CompoundSelector { Type = sliderType, States = PseudoStates.Active }),
                new StyleProperties { Color = ActiveThumb })
            .Add(
                Selector.For(new CompoundSelector { Type = sliderType, States = PseudoStates.Focus }),
                new StyleProperties { Color = FocusThumb })
            .Add(
                Selector.For(new CompoundSelector { States = PseudoStates.Disabled }),
                new StyleProperties { Color = DisabledThumb })
            .Build();
        context.AddSheet(sheet);

        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = RootSize;
        root.Inline.Height = RootSize;
        root.Inline.Color = White;
        root.IsPickable = true;
        context.AddRoot(root);

        Counter counter = new();
        Action<Slider> onChanged = counter.OnChanged;

        Slider[] sliders = new Slider[SliderCount];
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = 0f, Value = 0f });
            slider.Inline.Width = SliderWidth;
            slider.Inline.Height = SliderHeight;
            slider.ValueChanged = onChanged;
            root.Children.Add(slider);
            sliders[i] = slider;
        }

        // The disabled rule matches the last slider. The step sends no input to it.
        sliders[SliderCount - 1].States |= PseudoStates.Disabled;

        context.Update(0f);

        float2 pastRoot = new(PastRootX, RowY);
        return new Scene
        {
            Context = context,
            Renderer = renderer,
            Counter = counter,
            Sliders = sliders,
            Press = PointerTree.Down(new float2(PressX, RowY), PointerButton.Left),
            MoveInside = PointerTree.Move(new float2(MoveX, RowY)),
            MovePast = PointerTree.Move(pastRoot),
            Release = PointerTree.Up(pastRoot, PointerButton.Left),
            LeftDown = PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None),
            RightDown = PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None),
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

        // The press focuses the first slider, so the keys go to it.
        context.PushEvent(in scene.Press);
        context.PushEvent(in scene.MoveInside);
        context.PushEvent(in scene.MovePast);
        context.PushEvent(in scene.Release);
        context.PushEvent(in scene.LeftDown);
        context.PushEvent(in scene.RightDown);

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

    /// <summary><c>Update</c> and <c>Draw</c> on a tree of sliders allocate nothing after warm-up.</summary>
    [Fact]
    public void UpdateAndDrawAllocateNothingWithSliders()
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
    /// The full slider input path allocates nothing after warm-up: a press, a move, a captured move past the root, a
    /// release, a Left key, a Right key, the drain, <c>Update</c>, and <c>Draw</c>.
    /// </summary>
    [Fact]
    public void TheSliderInputPathAllocatesNothing()
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
    /// The measured body does real work. One input step gives 5 <see cref="Slider.ValueChanged"/> calls, the value 100,
    /// and one thumb rect for each slider in the frame.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        Assert.True(scene.Sliders[0].IsFocusable);
        Assert.Equal(PseudoStates.Disabled, scene.Sliders[SliderCount - 1].States & PseudoStates.Disabled);
        Assert.Same(scene.Sliders[0], scene.Context.HitTest(new float2(PressX, RowY)));

        InputStep(scene);

        // The walks: the press, 2 moves, and 2 keys. The release past the root has no target, so it has no walk. It
        // still ends the press.
        Assert.Equal(5, scene.LastWalks);
        Assert.Equal(5, scene.Counter.Changes);
        Assert.Equal(100f, scene.Sliders[0].Value);
        Assert.Same(scene.Sliders[0], scene.Context.FocusedElement);
        Assert.Equal(0f, scene.Sliders[1].Value);

        // The thumb is a square of the slider height. The track is wider.
        int thumbs = 0;
        ReadOnlySpan<DrawCall> calls = scene.Renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            bool isThumb = calls[i].Kind == DrawCallKind.Rect
                && calls[i].Rect.Z == SliderHeight
                && calls[i].Rect.W == SliderHeight;
            thumbs += isThumb ? 1 : 0;
        }

        Assert.Equal(SliderCount, thumbs);
    }
}
