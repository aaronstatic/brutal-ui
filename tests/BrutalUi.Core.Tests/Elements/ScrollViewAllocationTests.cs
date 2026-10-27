using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation gate of the scroll view (the eleventh gate). After warm-up, <c>Update</c> and <c>Draw</c> on a tree of
/// 10 scroll views and the full scroll view input path must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is a column root of 400 x 1000 with 10 <see cref="ScrollView"/> elements of 200 x 100. Each view holds 20
/// rows of 300 x 40. The odd rows are not pickable. The first view also holds a <see cref="Slider"/> of 120 x 20 as its
/// second child, so the rows of that view start at y 0, 60, 100, and so on. A sheet holds the rules
/// <c>ScrollView</c>, <c>ScrollView:hover</c>, <c>ScrollView:active</c>, and <c>:disabled</c>. The last view holds
/// <see cref="PseudoStates.Disabled"/>, so the <c>:disabled</c> rule matches. The test sends no input to it.
/// </para>
/// <para>
/// Each step of the input test sets the offset of the first view to (0, 0) from code, so each step starts from the same
/// state. The step then pushes 9 events to the first view: a scroll down, a scroll up, a press on a row that is not
/// pickable (it starts a drag), a move, a move outside the view (the capture walks it to the view), a release, a press
/// on the slider at the scrolled origin, a move, and a release. The step then runs the drain, <c>Update</c>, and
/// <c>Draw</c>. Each step calls <see cref="ScrollView.ScrollChanged"/> 4 times and <see cref="Slider.ValueChanged"/> 2
/// times.
/// </para>
/// <para>
/// Each gate calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before the measure. The measured bodies
/// create no lambda, no string, no boxed value, and no Assert. The test adds no warm-up round to hide an allocation.
/// </para>
/// </remarks>
public sealed class ScrollViewAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int ViewCount = 10;
    private const int RowCount = 20;
    private const float ViewportWidth = 800f;
    private const float ViewportHeight = 1200f;
    private const float RootWidth = 400f;
    private const float RootHeight = 1000f;
    private const float ViewWidth = 200f;
    private const float ViewHeight = 100f;
    private const float RowWidth = 300f;
    private const float RowHeight = 40f;
    private const float SliderWidth = 120f;
    private const float SliderHeight = 20f;

    private static readonly Rgba8 White = new(255, 255, 255, 255);
    private static readonly Rgba8 ViewFill = new(32, 32, 32, 255);
    private static readonly Rgba8 HoverFill = new(48, 96, 192, 255);
    private static readonly Rgba8 ActiveFill = new(200, 64, 64, 255);
    private static readonly Rgba8 DisabledFill = new(96, 96, 96, 255);
    private static readonly Rgba8 RowFill = new(64, 160, 96, 255);

    // Row 1 of the first view is at y 60 to 100 at the offset (0, 0), and it is not pickable.
    private static readonly float2 OverRow0 = new(100f, 20f);
    private static readonly float2 OverRow1 = new(100f, 80f);
    private static readonly float2 DragMove = new(100f, 60f);
    private static readonly float2 PastView = new(300f, 40f);

    // After the drag the offset is (0, 40). The slider is at x 0 to 120 and y 0 to 20.
    private static readonly float2 SliderPress = new(5f, 10f);
    private static readonly float2 SliderMove = new(15f, 10f);

    /// <summary>Counts the changes. Its methods are the targets of the sender delegates.</summary>
    private sealed class Counter
    {
        public int Scrolls;
        public int Values;

        public void OnScrolled(ScrollView view)
        {
            Scrolls++;
        }

        public void OnValue(Slider slider)
        {
            Values++;
        }
    }

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        public required Counter Counter { get; init; }

        public required ScrollView[] Views { get; init; }

        public required Slider Slider { get; init; }

        public required ScrollEvent ScrollDown;
        public required ScrollEvent ScrollUp;
        public required PointerEvent PressRow;
        public required PointerEvent MoveInside;
        public required PointerEvent MovePast;
        public required PointerEvent ReleasePast;
        public required PointerEvent PressSlider;
        public required PointerEvent MoveSlider;
        public required PointerEvent ReleaseSlider;

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

        ElementTypeId viewType = ElementType.Intern("ScrollView");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = viewType }),
                new StyleProperties { BackgroundColor = ViewFill })
            .Add(
                Selector.For(new CompoundSelector { Type = viewType, States = PseudoStates.Hover }),
                new StyleProperties { BackgroundColor = HoverFill })
            .Add(
                Selector.For(new CompoundSelector { Type = viewType, States = PseudoStates.Active }),
                new StyleProperties { BackgroundColor = ActiveFill })
            .Add(
                Selector.For(new CompoundSelector { States = PseudoStates.Disabled }),
                new StyleProperties { BackgroundColor = DisabledFill })
            .Build();
        context.AddSheet(sheet);

        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = RootWidth;
        root.Inline.Height = RootHeight;
        root.Inline.Color = White;
        root.IsPickable = true;
        context.AddRoot(root);

        Counter counter = new();
        Action<ScrollView> onScrolled = counter.OnScrolled;
        Action<Slider> onValue = counter.OnValue;

        Slider slider = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = 0f, Value = 0f });
        slider.Inline.Width = SliderWidth;
        slider.Inline.Height = SliderHeight;
        slider.ValueChanged = onValue;

        ScrollView[] views = new ScrollView[ViewCount];
        for (int v = 0; v < views.Length; v++)
        {
            ScrollView view = new(default(ScrollView.CreateInfo));
            view.Inline.FlexDirection = FlexDirection.Column;
            view.Inline.Width = ViewWidth;
            view.Inline.Height = ViewHeight;
            view.ScrollChanged = onScrolled;
            root.Children.Add(view);
            views[v] = view;

            for (int r = 0; r < RowCount; r++)
            {
                Element row = new();
                row.Inline.Width = RowWidth;
                row.Inline.Height = RowHeight;
                row.Inline.BackgroundColor = RowFill;
                row.IsPickable = (r & 1) == 0;
                view.Children.Add(row);

                if (v == 0 && r == 0)
                {
                    view.Children.Add(slider);
                }
            }
        }

        // The disabled rule matches the last view. The step sends no input to it.
        views[ViewCount - 1].States |= PseudoStates.Disabled;

        context.Update(0f);

        return new Scene
        {
            Context = context,
            Renderer = renderer,
            Counter = counter,
            Views = views,
            Slider = slider,
            ScrollDown = PointerTree.Scroll(OverRow0, new float2(0f, -1f)),
            ScrollUp = PointerTree.Scroll(OverRow0, new float2(0f, 1f)),
            PressRow = PointerTree.Down(OverRow1, PointerButton.Left),
            MoveInside = PointerTree.Move(DragMove),
            MovePast = PointerTree.Move(PastView),
            ReleasePast = PointerTree.Up(PastView, PointerButton.Left),
            PressSlider = PointerTree.Down(SliderPress, PointerButton.Left),
            MoveSlider = PointerTree.Move(SliderMove),
            ReleaseSlider = PointerTree.Up(SliderMove, PointerButton.Left),
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

        // Each step starts from the offset (0, 0), so the points below hit the same elements in each step.
        scene.Views[0].ScrollOffset = default;

        context.PushEvent(in scene.ScrollDown);
        context.PushEvent(in scene.ScrollUp);
        context.PushEvent(in scene.PressRow);
        context.PushEvent(in scene.MoveInside);
        context.PushEvent(in scene.MovePast);
        context.PushEvent(in scene.ReleasePast);
        context.PushEvent(in scene.PressSlider);
        context.PushEvent(in scene.MoveSlider);
        context.PushEvent(in scene.ReleaseSlider);

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

    /// <summary><c>Update</c> and <c>Draw</c> on a tree of scroll views allocate nothing after warm-up.</summary>
    [Fact]
    public void UpdateAndDrawAllocateNothingWithScrollViews()
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
    /// The full scroll view input path allocates nothing after warm-up: two scrolls, a drag press on a row that is not
    /// pickable, a move, a captured move outside the view, a release, a slider press and move at a scrolled origin, a
    /// release, the drain, <c>Update</c>, and <c>Draw</c>.
    /// </summary>
    [Fact]
    public void TheScrollViewInputPathAllocatesNothing()
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
    /// The measured body does real work. One input step calls <see cref="ScrollView.ScrollChanged"/> and
    /// <see cref="Slider.ValueChanged"/>, leaves the first view at an offset above 0, and draws one clip for each view.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        Assert.Equal(PseudoStates.Disabled, scene.Views[ViewCount - 1].States & PseudoStates.Disabled);
        Assert.Same(scene.Views[0], scene.Context.HitTest(OverRow1));
        Assert.Same(scene.Slider, scene.Views[0].Children[1]);

        InputStep(scene);

        Assert.True(scene.LastWalks > 0);
        Assert.Equal(4, scene.Counter.Scrolls);
        Assert.True(scene.Counter.Values >= 1);
        Assert.Equal(new float2(0f, 40f), scene.Views[0].ScrollOffset);
        Assert.Equal(0f, scene.Views[1].ScrollOffset.X);
        Assert.Equal(0f, scene.Views[1].ScrollOffset.Y);

        int clips = 0;
        ReadOnlySpan<DrawCall> calls = scene.Renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            clips += calls[i].Kind == DrawCallKind.PushClip ? 1 : 0;
        }

        Assert.Equal(ViewCount, clips);
    }
}
