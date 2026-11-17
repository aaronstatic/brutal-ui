using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The allocation gate of a loaded bound tree. After warm-up, <c>Update</c> and <c>Draw</c> on the tree that
/// <see cref="BoundTemplate"/> loads allocate 0 bytes, also for a click on its command button.
/// </summary>
/// <remarks>
/// <para>
/// The scene is one loaded tree with six bound elements and one command button: the <c>score</c> label with the format
/// <c>N0</c>, the <c>volume</c> label with the format <c>F1</c>, the <c>player</c> label and the <c>name-field</c> text
/// field on one string key, the <c>mute</c> toggle, the <c>volume-slider</c> slider, and the <c>pause</c> button. The
/// state comes from <see cref="BoundTree.Next(int)"/>, so each text keeps its width and no buffer grows after the
/// warm-up.
/// </para>
/// <para>
/// The first gate sets a new state in each frame, so each binding applies. The second gate pushes a press and a
/// release at <c>pause</c> in each frame, so the click, the command, and the invoke run under the counter. Each gate
/// calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before its measure. The measured bodies create no
/// lambda, no string, no boxed value, and no Assert.
/// </para>
/// <para>
/// The context uses a <see cref="RecordingRenderer"/> measurer and a default font size of 16, so the button holds a
/// real rect and the cached center hits it. The scene caches that center after the first <see cref="UiContext.Update(float)"/>
/// call and builds the two pointer events before the measure.
/// </para>
/// </remarks>
public sealed class TemplateBindingAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int FramesPerRound = 9;
    private const float FrameTime = 0.016f;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The loaded tree, the events, and the round counter that the measured bodies use.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        public required Store<BoundTree.HudState> Store { get; init; }

        public required Element Root { get; init; }

        public required Label Score { get; init; }

        public required Label Volume { get; init; }

        public required Label Player { get; init; }

        public required TextField NameField { get; init; }

        public required Toggle Mute { get; init; }

        public required Slider VolumeSlider { get; init; }

        public required Counter Commands { get; init; }

        /// <summary>The press event. It is a field, so the body passes it by reference.</summary>
        public required PointerEvent Press;

        /// <summary>The release event. It is a field, so the body passes it by reference.</summary>
        public required PointerEvent Release;

        /// <summary>The round number. The bodies use it as the new value.</summary>
        public int Round;
    }

    /// <summary>Loads the template, adds the root to a context, and caches the click target of <c>pause</c>.</summary>
    /// <returns>The scene.</returns>
    private static Scene BuildScene()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("game.pause"u8, s_count, counter);
        Store<BoundTree.HudState> store = new(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = BoundTree.Next(0),
        });

        RecordingRenderer renderer = new();
        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer, DefaultFontSize = 16f })
        {
            ViewportSize = new float2(400, 300),
        };

        Element root = BoundTemplate.Load(store, registry);
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.Width = 400f;
        root.Inline.Height = 300f;
        context.AddRoot(root);

        Button? pause = root.Find<Button>("pause"u8);
        Assert.NotNull(pause);

        Label? score = root.Find<Label>("score"u8);
        Label? volume = root.Find<Label>("volume"u8);
        Label? player = root.Find<Label>("player"u8);
        TextField? nameField = root.Find<TextField>("name-field"u8);
        Toggle? mute = root.Find<Toggle>("mute"u8);
        Slider? volumeSlider = root.Find<Slider>("volume-slider"u8);
        Assert.NotNull(score);
        Assert.NotNull(volume);
        Assert.NotNull(player);
        Assert.NotNull(nameField);
        Assert.NotNull(mute);
        Assert.NotNull(volumeSlider);

        // One frame builds the layout, so the center of the button is a real rect.
        context.Update(FrameTime);
        Assert.True(pause.Layout.Rect.Z > 0f);
        float2 center = ThousandWidgetTree.ViewportCenter(pause);

        return new Scene
        {
            Context = context,
            Renderer = renderer,
            Store = store,
            Root = root,
            Score = score,
            Volume = volume,
            Player = player,
            NameField = nameField,
            Mute = mute,
            VolumeSlider = volumeSlider,
            Commands = counter,
            Press = Pointer(UiEventType.PointerDown, center),
            Release = Pointer(UiEventType.PointerUp, center),
        };
    }

    /// <summary>Creates a left-button pointer event with no target.</summary>
    /// <param name="type"><see cref="UiEventType.PointerDown"/> or <see cref="UiEventType.PointerUp"/>.</param>
    /// <param name="position">The position in viewport pixels.</param>
    /// <returns>The event.</returns>
    private static PointerEvent Pointer(UiEventType type, float2 position)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>The measured body of the first gate. It sets a new state, then runs the frame.</summary>
    private static void ChangingFrame(Scene scene)
    {
        scene.Round++;
        BoundTree.HudState state = BoundTree.Next(scene.Round);
        scene.Store.Set(in state);
        scene.Context.Update(FrameTime);
        scene.Context.Draw(scene.Renderer);
    }

    /// <summary>The measured body of the second gate. It pushes a click at <c>pause</c>, then runs the frame.</summary>
    private static void ClickFrame(Scene scene)
    {
        UiContext context = scene.Context;
        context.PushEvent(in scene.Press);
        context.PushEvent(in scene.Release);
        context.Update(FrameTime);
        context.Draw(scene.Renderer);
    }

    private static void RunChanging(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            ChangingFrame(scene);
        }
    }

    private static void RunClicks(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            ClickFrame(scene);
        }
    }

    /// <summary>
    /// <c>Update</c> and <c>Draw</c> allocate nothing on a loaded bound tree when each frame sets a new state: the two
    /// number labels, the string label, the text field, the toggle, and the slider apply in each frame.
    /// </summary>
    [Fact]
    public void UpdateAndDrawOnALoadedBoundTreeAllocateNothing()
    {
        Scene scene = BuildScene();
        RunChanging(scene, WarmUpRounds * FramesPerRound);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunChanging(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// A click on the loaded command button allocates nothing: the press, the release, the click delegate, the command
    /// invoke, <c>Update</c>, and <c>Draw</c>.
    /// </summary>
    [Fact]
    public void AClickOnALoadedCommandButtonAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunClicks(scene, WarmUpRounds * FramesPerRound);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunClicks(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured bodies do real work. After one changing frame each bound element shows the state of the round.
    /// After one click frame the command counter grows by one.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();

        // 1. The changing frame: each bound element shows the state of round 1.
        ChangingFrame(scene);
        BoundTree.HudState state = BoundTree.Next(1);
        Assert.Equal(state, scene.Store.Value);
        Assert.Equal(1, scene.Score.BindingCount);
        Assert.Equal(1, scene.Volume.BindingCount);
        Assert.Equal(1, scene.Player.BindingCount);
        Assert.Equal(1, scene.NameField.BindingCount);
        Assert.Equal(1, scene.Mute.BindingCount);
        Assert.Equal(1, scene.VolumeSlider.BindingCount);
        Assert.True(scene.Score.Text.SequenceEqual("10,001"u8));
        Assert.True(scene.Volume.Text.SequenceEqual("11.0"u8));
        Assert.True(scene.Player.Text.SequenceEqual(Encoding.UTF8.GetBytes(state.Name)));
        Assert.True(scene.NameField.Text.SequenceEqual(Encoding.UTF8.GetBytes(state.Name)));
        Assert.True(scene.Mute.Checked);
        Assert.Equal(state.Volume, scene.VolumeSlider.Value);

        // 2. The click frame: one call of game.pause.
        int calls = scene.Commands.Calls;
        ClickFrame(scene);
        Assert.Equal(calls + 1, scene.Commands.Calls);
    }

    /// <summary>The target of the command handler: a call counter.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }
}
