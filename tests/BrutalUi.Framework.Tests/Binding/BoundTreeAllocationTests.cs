using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The allocation gate of the bindings on the 1000-element tree. After warm-up, <c>Update</c> and <c>Draw</c> on a tree
/// with a changing store, 40 bound labels, and three bound widgets must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is <see cref="BoundTree"/>: <see cref="ThousandWidgetTree"/> with a <c>Store</c> of the HUD state. Its 40
/// stat rows hold a bound label each (38 number labels of <c>Score</c> and <c>Lives</c>, one <c>Volume</c> label with
/// the format <c>F1</c>, and one <c>Name</c> label). The toggle, the slider, and the text field are bound. The toggle
/// writes <c>Muted</c> back to the store. The scene has 1000 elements.
/// </para>
/// <para>
/// The first gate sets a new state in each frame, so each slice changes and each binding applies. The second gate
/// changes <c>Lives</c> only: its labels apply, and the labels of the other slices compare the same bytes and mark
/// nothing. The third gate pushes a press and a release at the toggle in each frame. The flip calls the write-back, the
/// version rises, and the binding writes the same value back, which changes nothing.
/// </para>
/// <para>
/// Each gate calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before the measure. The measured bodies
/// create no lambda, no string, no boxed value, and no Assert. The test adds no warm-up round to hide an allocation.
/// </para>
/// </remarks>
public sealed class BoundTreeAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;

    /// <summary>The frames of a warm-up round.</summary>
    private const int FramesPerRound = 9;

    /// <summary>The bound tree, the events, and the round counter that the measured bodies use.</summary>
    private sealed class Scene
    {
        public required BoundTree Bound { get; init; }

        public required PointerEvent TogglePress;
        public required PointerEvent ToggleRelease;

        /// <summary>The round number. The bodies use it as the new value.</summary>
        public int Round;
    }

    private static Scene BuildScene()
    {
        BoundTree bound = BoundTree.Build();
        float2 center = bound.Tree.ToggleCenter;
        return new Scene
        {
            Bound = bound,
            TogglePress = Pointer(UiEventType.PointerDown, center),
            ToggleRelease = Pointer(UiEventType.PointerUp, center),
        };
    }

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
        BoundTree bound = scene.Bound;
        scene.Round++;
        BoundTree.HudState state = BoundTree.Next(scene.Round);
        bound.Store.Set(in state);
        bound.Tree.Context.Update(0.016f);
        bound.Tree.Context.Draw(bound.Tree.Renderer);
    }

    /// <summary>The measured body of the second gate. It changes <c>Lives</c> only, then runs the frame.</summary>
    private static void LivesFrame(Scene scene)
    {
        BoundTree bound = scene.Bound;
        scene.Round++;
        BoundTree.HudState state = bound.Store.Value with { Lives = 10 + (scene.Round % 10) };
        bound.Store.Set(in state);
        bound.Tree.Context.Update(0.016f);
        bound.Tree.Context.Draw(bound.Tree.Renderer);
    }

    /// <summary>The measured body of the third gate. It pushes a click at the toggle, then runs the frame.</summary>
    private static void FlipFrame(Scene scene)
    {
        BoundTree bound = scene.Bound;
        UiContext context = bound.Tree.Context;
        context.PushEvent(in scene.TogglePress);
        context.PushEvent(in scene.ToggleRelease);
        context.Update(0.016f);
        context.Draw(bound.Tree.Renderer);
    }

    private static void RunChanging(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            ChangingFrame(scene);
        }
    }

    private static void RunLives(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            LivesFrame(scene);
        }
    }

    private static void RunFlips(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            FlipFrame(scene);
        }
    }

    /// <summary>
    /// <c>Update</c> and <c>Draw</c> allocate nothing when each frame sets a new state: 40 label bindings, the toggle,
    /// the slider, and the text field apply in each frame.
    /// </summary>
    [Fact]
    public void UpdateAndDrawWithAChangingStoreAllocateNothing()
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
    /// <c>Update</c> and <c>Draw</c> allocate nothing when each frame changes <c>Lives</c> only. Each binding applies.
    /// The labels of the other slices compare the same bytes and mark nothing.
    /// </summary>
    [Fact]
    public void AChangeOfOneSliceAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunLives(scene, WarmUpRounds * FramesPerRound);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunLives(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// A user flip of the toggle with a write-back to the store allocates nothing: the press, the release, the
    /// delegate, the store set, the binding pass, <c>Update</c>, and <c>Draw</c>.
    /// </summary>
    [Fact]
    public void AUserFlipWithAWriteBackAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunFlips(scene, WarmUpRounds * FramesPerRound);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunFlips(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured bodies do real work. After one changing frame, the labels, the toggle, the slider, and the field
    /// hold the slices of the store, and no widget slot but the toggle write-back ran. One lives frame changes the
    /// lives labels only. One flip frame calls <c>CheckedChanged</c> one time and raises the version one time.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        BoundTree bound = scene.Bound;
        ThousandWidgetTree tree = bound.Tree;
        Assert.Equal(BoundTree.LabelCount, bound.Labels.Length);
        Assert.Equal(ThousandElementTree.ElementCount, ThousandWidgetTree.CountSubtree(tree.Root));
        Assert.Equal(1, tree.Toggle.BindingCount);
        Assert.Equal(1, tree.Slider.BindingCount);
        Assert.Equal(1, tree.TextField.BindingCount);
        for (int i = 0; i < bound.Labels.Length; i++)
        {
            Assert.Equal(1, bound.Labels[i].BindingCount);
        }

        // 1. The changing frame: each slice shows the state of round 1.
        ChangingFrame(scene);
        BoundTree.HudState state = BoundTree.Next(1);
        Assert.Equal(state, bound.Store.Value);
        Assert.True(bound.Labels[0].Text.SequenceEqual(Encoding.UTF8.GetBytes((10000 + 1).ToString())));
        Assert.True(bound.Labels[1].Text.SequenceEqual(Encoding.UTF8.GetBytes(state.Lives.ToString())));
        Assert.True(bound.Labels[BoundTree.VolumeRow].Text.SequenceEqual("11.0"u8));
        Assert.True(bound.Labels[BoundTree.NameRow].Text.SequenceEqual(Encoding.UTF8.GetBytes(state.Name)));
        Assert.Equal(state.Muted, tree.Toggle.Checked);
        Assert.Equal(state.Volume, tree.Slider.Value);
        Assert.True(tree.TextField.Text.SequenceEqual(Encoding.UTF8.GetBytes(state.Name)));
        Assert.Equal(0, bound.Counts.Flips);
        Assert.Equal(0, bound.Counts.Values);
        Assert.Equal(0, bound.Counts.TextChanges);

        // 2. The lives frame: the lives labels change, the score labels keep their bytes.
        // The test reads the round and the version before the call. It checks fixed values after the call, because the
        // body changes the round. After step 1, the round is 1, so the lives are 12 after this frame.
        int round = scene.Round;
        ulong version = bound.Store.Version;
        LivesFrame(scene);
        Assert.Equal(round + 1, scene.Round);
        Assert.Equal(version + 1UL, bound.Store.Version);
        Assert.Equal(10 + ((round + 1) % 10), bound.Store.Value.Lives);
        Assert.Equal(12, bound.Store.Value.Lives);
        Assert.True(bound.Labels[1].Text.SequenceEqual("12"u8));
        Assert.True(bound.Labels[0].Text.SequenceEqual("10001"u8));
        Assert.Equal(0, bound.Counts.Flips);

        // 3. The flip frame: one CheckedChanged call, one new version, and the toggle matches the store.
        bool checkedBefore = tree.Toggle.Checked;
        ulong versionBefore = bound.Store.Version;
        FlipFrame(scene);
        Assert.Equal(1, bound.Counts.Flips);
        Assert.Equal(versionBefore + 1UL, bound.Store.Version);
        Assert.Equal(!checkedBefore, tree.Toggle.Checked);
        Assert.Equal(tree.Toggle.Checked, bound.Store.Value.Muted);
        Assert.Equal(0, bound.Counts.Values);
        Assert.Equal(0, bound.Counts.TextChanges);
    }
}
