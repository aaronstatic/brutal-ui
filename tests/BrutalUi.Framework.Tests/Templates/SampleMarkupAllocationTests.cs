using System;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using HelloPanel;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The twenty-third allocation gate: the sample shape that <c>HelloState.xml</c> and
/// <c>HelloDefinitions.xml</c> build allocates 0 bytes in <c>Update</c> and <c>Draw</c>, also for a click on the
/// loaded <c>Add</c> button and for a mode toggle. A third test reads the tree and proves that the measured bodies
/// do real work.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BuildScene"/> follows the sample: it reads the linked <c>HelloState.xml</c>,
/// <c>HelloDefinitions.xml</c>, and <c>HelloScreen.css</c> from <see cref="AppContext.BaseDirectory"/> and runs the
/// load flow of <c>HelloMarkup.Load</c>. The three C# factories are test twins: a widget panel and an info panel
/// with the panel classes and one label, and a layout with the shape of <c>HelloScreen.BuildLayout</c>. The
/// layout root is not pickable and holds the slots <c>top</c> and <c>bottom</c>. The generated register method
/// <c>RegisterBrutalUiFrameworkTestsElements</c> adds the linked <see cref="HelloRow"/> type.
/// </para>
/// <para>
/// The context uses a <see cref="RecordingRenderer"/> measurer, a default font size of 16, and a viewport of 1280
/// by 720, so the <c>Add</c> button holds a real rect and the cached center hits it. The scene caches that center
/// after one <see cref="UiContext.Update(float)"/> call and builds the two pointer events before the measure.
/// </para>
/// <para>
/// The rule of the measured bodies: they hold no lambda, no string, no boxed value, and no <c>Assert</c>. The
/// command handlers and the store selector live in static fields, so each one builds one time. The click body
/// pushes a press and a release at <c>Add</c>, then runs one update and one draw. The toggle body sets
/// <c>hello.detail</c> and <c>hello.main</c> in turn, then runs one update and one draw.
/// </para>
/// </remarks>
public sealed partial class SampleMarkupAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int FramesPerRound = 9;
    private const float FrameTime = 0.016f;

    /// <summary>The measured body of the click gate: a press and a release at <c>Add</c>, then one frame.</summary>
    /// <param name="scene">The scene.</param>
    private static void ClickFrame(Scene scene)
    {
        UiContext context = scene.Context;
        context.PushEvent(in scene.Press);
        context.PushEvent(in scene.Release);
        context.Update(FrameTime);
        context.Draw(scene.Renderer);
    }

    /// <summary>The measured body of the toggle gate: the two modes in turn, then one frame.</summary>
    /// <param name="scene">The scene.</param>
    private static void ToggleFrame(Scene scene)
    {
        scene.Round++;
        if ((scene.Round & 1) == 1)
        {
            scene.Ui.SetMode(scene.Detail.Name);
        }
        else
        {
            scene.Ui.SetMode(scene.Main.Name);
        }

        scene.Context.Update(FrameTime);
        scene.Context.Draw(scene.Renderer);
    }

    private static void RunClicks(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            ClickFrame(scene);
        }
    }

    private static void RunToggles(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            ToggleFrame(scene);
        }
    }

    /// <summary>Adds one subtree to the three markup counts: the element count, the binding sum, and the command
    /// count. The command count counts each <see cref="Button"/> with a <see cref="Button.Clicked"/> slot that is
    /// not null.</summary>
    /// <param name="element">The subtree root.</param>
    /// <param name="elements">The running element count.</param>
    /// <param name="bindings">The running binding sum.</param>
    /// <param name="commands">The running command count.</param>
    private static void CountTree(Element element, ref int elements, ref int bindings, ref int commands)
    {
        elements++;
        bindings += element.BindingCount;
        if (element is Button button && button.Clicked is not null)
        {
            commands++;
        }

        foreach (Element child in element.Children)
        {
            CountTree(child, ref elements, ref bindings, ref commands);
        }
    }

    /// <summary>
    /// A click on the loaded <c>Add</c> button allocates nothing: the press, the release, the click delegate, the
    /// command, the store version, the binding, <c>Update</c>, and <c>Draw</c> run 100 times for 0 bytes.
    /// </summary>
    [Fact]
    public void AnAddClickOnTheLoadedStatePanelAllocatesNothing()
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
    /// A mode toggle on the loaded sample records allocates nothing: the diff, the map activate, the mount, the
    /// unmount, <c>Update</c>, and <c>Draw</c> run 100 times for 0 bytes.
    /// </summary>
    [Fact]
    public void AModeToggleOnTheLoadedSampleRecordsAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunToggles(scene, WarmUpRounds * FramesPerRound);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunToggles(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured bodies do real work. One click frame adds 1 to the store and the count, and the score label
    /// shows the new value. One toggle frame makes <c>hello.detail</c> current with the info root as the one child
    /// of <c>bottom</c>. The state root holds a <see cref="HelloRow"/> with the class <c>hello-row</c>, 6 elements,
    /// a binding sum of 1, and 3 buttons with a click slot.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();

        // 1. One click frame adds 1 to the store and the count, and the label shows the new value.
        int adds = scene.Count.Adds;
        int value = scene.Store.Value;
        ClickFrame(scene);
        Assert.Equal(adds + 1, scene.Count.Adds);
        Assert.Equal(value + 1, scene.Store.Value);
        Assert.True(scene.ScoreLabel.Text.SequenceEqual("1"u8));

        // 2. One toggle frame makes hello.detail current, with the info root as the one child of bottom.
        ToggleFrame(scene);
        Assert.Same(scene.Detail, scene.Ui.CurrentMode);
        Assert.Equal(1, scene.Bottom.Children.Count);
        Assert.Same(scene.InfoRoot, scene.Bottom.Children[0]);

        // 3. The loaded state shape: a HelloRow with the hello-row class, 6 elements, a binding sum of 1, and
        // 3 buttons with a click slot.
        Element row = scene.StateRoot.Children[0];
        Assert.IsType<HelloRow>(row);
        Assert.True(row.Classes.Contains(RowClass));

        int elements = 0;
        int bindings = 0;
        int commands = 0;
        CountTree(scene.StateRoot, ref elements, ref bindings, ref commands);
        Assert.Equal(6, elements);
        Assert.Equal(1, bindings);
        Assert.Equal(3, commands);
    }
}
