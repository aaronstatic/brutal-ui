using System;
using System.Globalization;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The allocation gate of the commands, the events, and the services (the sixteenth gate). After warm-up, an invoke by
/// name, an unknown name, a click on a bound button through <c>Update</c> and <c>Draw</c>, a publish, and a resolve
/// must allocate 0 bytes in the Debug build.
/// </summary>
/// <remarks>
/// <para>
/// The scene holds a <see cref="CommandRegistry"/> with 16 commands <c>cmd-00</c> to <c>cmd-15</c>. Each command has its
/// own counter target and one shared static handler. The scene builds each name as a <c>byte[]</c>, and it builds the
/// unknown name <c>missing</c> as a <c>byte[]</c>. The <see cref="CommandRegistry.UnknownCommand"/> slot counts the
/// misses. The scene has a context with a measurer and a viewport of 400 by 300, a pickable root, and a button of 80 by
/// 24 that is bound to <c>cmd-07</c>.
/// </para>
/// <para>
/// The scene also holds an <see cref="EventBus"/> with 8 subscribers of <c>ScoreChanged</c>, 1 subscriber of
/// <c>Paused</c>, and no subscriber of <c>Unheard</c>. It holds a <see cref="ServiceRegistry"/> with 3 services and the
/// counters for the hits and the misses of a resolve.
/// </para>
/// <para>
/// The rule: a measured body creates no lambda, no string, no boxed value, no array, and no Assert. The scene builds
/// each name, each delegate, and each event before the measure. The test adds no warm-up round to hide an allocation.
/// </para>
/// </remarks>
public sealed class CommandAndEventAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int CommandCount = 16;
    private const int ScoreSubscriberCount = 8;
    private const int ButtonCommand = 7;

    /// <summary>The handler that adds one to the counter target. It sits in a field, so no call creates a delegate.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The handler of <c>ScoreChanged</c>. It records the last score and counts the call.</summary>
    private static readonly EventBusHandler<ScoreChanged> s_onScore = static (in ScoreChanged e, object? target) =>
    {
        ScoreSink sink = (ScoreSink)target!;
        sink.Last = e.Score;
        sink.Count++;
    };

    /// <summary>The handler of <c>Paused</c>. It records the last value and counts the call.</summary>
    private static readonly EventBusHandler<Paused> s_onPaused = static (in Paused e, object? target) =>
    {
        PauseSink sink = (PauseSink)target!;
        sink.On = e.On;
        sink.Count++;
    };

    private readonly record struct ScoreChanged(int Score);

    private readonly record struct Paused(bool On);

    private readonly record struct Unheard(int Value);

    /// <summary>The target of a command: a call counter.</summary>
    private sealed class Counter
    {
        public int Calls;
    }

    /// <summary>The target of a <c>ScoreChanged</c> subscriber.</summary>
    private sealed class ScoreSink
    {
        public int Last;

        public int Count;
    }

    /// <summary>The target of the <c>Paused</c> subscriber.</summary>
    private sealed class PauseSink
    {
        public bool On;

        public int Count;
    }

    private sealed class AudioService
    {
    }

    private sealed class SaveService
    {
    }

    private sealed class InputService
    {
    }

    /// <summary>A type that the registry does not hold.</summary>
    private sealed class MissingService
    {
    }

    /// <summary>The registries, the tree, the events, and the counters that the measured bodies use.</summary>
    private sealed class Scene
    {
        public required CommandRegistry Registry { get; init; }

        public required byte[][] Names { get; init; }

        public required byte[] Missing { get; init; }

        public required Counter[] Counters { get; init; }

        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        /// <summary>The press event. It is a field, so the body passes it by reference.</summary>
        public required PointerEvent Press;

        /// <summary>The release event. It is a field, so the body passes it by reference.</summary>
        public required PointerEvent Release;

        public required EventBus Bus { get; init; }

        public required ScoreSink[] Scores { get; init; }

        public required PauseSink Pause { get; init; }

        public required ServiceRegistry Services { get; init; }

        /// <summary>The round number. The publish body uses it as the score.</summary>
        public int Round;

        /// <summary>The count of unknown names that the slot got.</summary>
        public int CommandMisses;

        /// <summary>The count of resolves that found a service.</summary>
        public int ResolveHits;

        /// <summary>The count of resolves that found no service.</summary>
        public int ResolveMisses;

        /// <summary>The slot of <see cref="CommandRegistry.UnknownCommand"/>. The scene makes the delegate one time.</summary>
        /// <param name="name">The unknown name.</param>
        public void OnUnknown(ReadOnlySpan<byte> name)
        {
            CommandMisses++;
        }
    }

    private static Scene BuildScene()
    {
        CommandRegistry registry = new();
        byte[][] names = new byte[CommandCount][];
        Counter[] counters = new Counter[CommandCount];
        for (int i = 0; i < CommandCount; i++)
        {
            names[i] = Encoding.UTF8.GetBytes("cmd-" + i.ToString("00", CultureInfo.InvariantCulture));
            counters[i] = new Counter();
            registry.Register(names[i], s_count, counters[i]);
        }

        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
        Element root = new();
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        root.IsPickable = true;
        context.AddRoot(root);
        Button button = new(default(Button.CreateInfo));
        button.Inline.Width = 80;
        button.Inline.Height = 24;
        root.Children.Add(button);
        button.BindCommand(registry, names[ButtonCommand]);

        EventBus bus = new();
        ScoreSink[] scores = new ScoreSink[ScoreSubscriberCount];
        for (int i = 0; i < ScoreSubscriberCount; i++)
        {
            scores[i] = new ScoreSink();
            bus.Subscribe(s_onScore, scores[i]);
        }

        PauseSink pause = new();
        bus.Subscribe(s_onPaused, pause);

        ServiceRegistry services = new();
        services.Register(new AudioService());
        services.Register(new SaveService());
        services.Register(new InputService());

        float2 center = new(40f, 12f);
        Scene scene = new()
        {
            Registry = registry,
            Names = names,
            Missing = Encoding.UTF8.GetBytes("missing"),
            Counters = counters,
            Context = context,
            Renderer = new RecordingRenderer(),
            Press = Pointer(UiEventType.PointerDown, center),
            Release = Pointer(UiEventType.PointerUp, center),
            Bus = bus,
            Scores = scores,
            Pause = pause,
            Services = services,
        };
        registry.UnknownCommand = scene.OnUnknown;

        // One frame builds the layout, so the first click hits the button.
        context.Update(0f);
        context.Draw(scene.Renderer);
        return scene;
    }

    /// <summary>Creates a left-button pointer event with no target.</summary>
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

    /// <summary>
    /// The measured body of the first test. An invoke of the first command, of the last command, and of an unknown name.
    /// It uses no lambda, no boxing, no string, and no Assert.
    /// </summary>
    private static void InvokeStep(Scene scene)
    {
        scene.Registry.Invoke(scene.Names[0]);
        scene.Registry.Invoke(scene.Names[CommandCount - 1]);
        scene.Registry.Invoke(scene.Missing);
    }

    /// <summary>
    /// The measured body of the second test. A press and a release at the center of the bound button with no target,
    /// then <c>Update</c> and <c>Draw</c>.
    /// </summary>
    private static void ClickStep(Scene scene)
    {
        UiContext context = scene.Context;
        context.PushEvent(in scene.Press);
        context.PushEvent(in scene.Release);
        context.Update(0.016f);
        context.Draw(scene.Renderer);
    }

    /// <summary>
    /// The measured body of the third test. A publish of <c>ScoreChanged</c> with the round as the score, a publish of
    /// <c>Paused</c>, and a publish of <c>Unheard</c>.
    /// </summary>
    private static void PublishStep(Scene scene)
    {
        int round = ++scene.Round;
        ScoreChanged score = new(round);
        scene.Bus.Publish(in score);
        Paused paused = new((round & 1) == 1);
        scene.Bus.Publish(in paused);
        Unheard unheard = new(round);
        scene.Bus.Publish(in unheard);
    }

    /// <summary>
    /// The measured body of the fourth test. <c>Resolve</c> and <c>TryResolve</c> of two registered types, and
    /// <c>Resolve</c> of one type that the registry does not hold.
    /// </summary>
    private static void ResolveStep(Scene scene)
    {
        ServiceRegistry services = scene.Services;
        int hits = 0;
        int misses = 0;

        if (services.Resolve<AudioService>() is not null)
        {
            hits++;
        }

        if (services.Resolve<SaveService>() is not null)
        {
            hits++;
        }

        if (services.TryResolve(out AudioService? audio))
        {
            hits++;
        }

        if (services.TryResolve(out SaveService? save))
        {
            hits++;
        }

        if (services.Resolve<MissingService>() is null)
        {
            misses++;
        }

        scene.ResolveHits += hits;
        scene.ResolveMisses += misses;
    }

    private static void RunInvokeSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            InvokeStep(scene);
        }
    }

    private static void RunClickSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            ClickStep(scene);
        }
    }

    private static void RunPublishSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            PublishStep(scene);
        }
    }

    private static void RunResolveSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            ResolveStep(scene);
        }
    }

    /// <summary>An invoke by name and an unknown name allocate nothing after warm-up.</summary>
    [Fact]
    public void InvokeAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunInvokeSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunInvokeSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>A click on a bound button, with <c>Update</c> and <c>Draw</c>, allocates nothing after warm-up.</summary>
    [Fact]
    public void AClickOnABoundButtonAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunClickSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunClickSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>A publish with 8 subscribers, with 1 subscriber, and with none allocates nothing after warm-up.</summary>
    [Fact]
    public void PublishAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunPublishSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunPublishSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>A resolve of a found type and of a missing type allocates nothing after warm-up.</summary>
    [Fact]
    public void ResolveAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunResolveSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunResolveSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured bodies do real work. The test reads each count before the bodies run, runs one step of each body,
    /// and asserts a fixed increment. An empty body cannot pass.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        int first = scene.Counters[0].Calls;
        int last = scene.Counters[CommandCount - 1].Calls;
        int button = scene.Counters[ButtonCommand].Calls;
        int other = scene.Counters[3].Calls;
        int commandMisses = scene.CommandMisses;
        int round = scene.Round;
        int pauseCount = scene.Pause.Count;
        int[] scoreCounts = new int[ScoreSubscriberCount];
        for (int i = 0; i < ScoreSubscriberCount; i++)
        {
            scoreCounts[i] = scene.Scores[i].Count;
        }

        int hits = scene.ResolveHits;
        int misses = scene.ResolveMisses;

        InvokeStep(scene);
        ClickStep(scene);
        PublishStep(scene);
        ResolveStep(scene);

        Assert.Equal(first + 1, scene.Counters[0].Calls);
        Assert.Equal(last + 1, scene.Counters[CommandCount - 1].Calls);
        Assert.Equal(commandMisses + 1, scene.CommandMisses);
        Assert.Equal(button + 1, scene.Counters[ButtonCommand].Calls);
        Assert.Equal(other, scene.Counters[3].Calls);

        Assert.Equal(round + 1, scene.Round);
        for (int i = 0; i < ScoreSubscriberCount; i++)
        {
            Assert.Equal(scoreCounts[i] + 1, scene.Scores[i].Count);
            Assert.Equal(round + 1, scene.Scores[i].Last);
        }

        Assert.Equal(pauseCount + 1, scene.Pause.Count);
        Assert.True(scene.Pause.On);

        Assert.Equal(hits + 4, scene.ResolveHits);
        Assert.Equal(misses + 1, scene.ResolveMisses);
    }
}
