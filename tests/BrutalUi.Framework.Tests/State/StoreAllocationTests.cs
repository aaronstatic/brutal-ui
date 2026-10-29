using System;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The allocation gate of the store (the first gate of <c>BrutalUi.Framework.Tests</c>). After warm-up, <c>Set</c>, a
/// version read through <see cref="IBindingSource"/>, and <see cref="StoreSubscription{T, TValue}.TryGetChanged"/> must
/// allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene holds a <c>Store&lt;int&gt;</c>, a <c>Store&lt;GameState&gt;</c> (a <c>record struct</c> with
/// <see cref="IEquatable{T}"/>), and a <c>Store&lt;string&gt;</c> with two strings that the scene built. It also holds a
/// subscription on the <c>GameState</c> store with a <see langword="static"/> selector, and an
/// <see cref="IBindingSource"/> reference to each store. The test builds all of it before the measure.
/// </para>
/// <para>
/// The first gate sets a new value and an equal value on each of the three stores in each iteration. The second gate
/// reads <see cref="IBindingSource.Version"/> of each store in each iteration. The third gate sets a new
/// <c>GameState</c> and calls <see cref="StoreSubscription{T, TValue}.TryGetChanged"/> twice in each iteration: the
/// first call gives <see langword="true"/> and the second gives <see langword="false"/>.
/// </para>
/// <para>
/// Each gate calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before the measure. The measured bodies
/// create no lambda, no string, no boxed value, and no Assert. The test adds no warm-up round to hide an allocation.
/// </para>
/// </remarks>
public sealed class StoreAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int Lives = 3;
    private const int SourceCount = 3;

    private readonly record struct GameState(int Score, int Lives);

    /// <summary>The stores, the subscription, and the counters that the measured bodies use.</summary>
    private sealed class Scene
    {
        public required Store<int> IntStore { get; init; }

        public required Store<GameState> StateStore { get; init; }

        public required Store<string> TextStore { get; init; }

        /// <summary>The string that the scene sets on even rounds. It is not equal to <see cref="TextB"/>.</summary>
        public required string TextA { get; init; }

        /// <summary>The string that the scene sets on odd rounds.</summary>
        public required string TextB { get; init; }

        public required IBindingSource[] Sources { get; init; }

        /// <summary>The subscription is a mutable struct. The scene keeps it in a field, so no call works on a copy.</summary>
        public required StoreSubscription<GameState, int> Subscription;

        /// <summary>The round number. The bodies use it as the new value.</summary>
        public int Round;

        /// <summary>The sum of the versions that the bodies read.</summary>
        public ulong VersionSum;

        /// <summary>The count of <see langword="true"/> results of <c>TryGetChanged</c>.</summary>
        public int Hits;

        /// <summary>The count of <see langword="false"/> results of <c>TryGetChanged</c>.</summary>
        public int Misses;

        /// <summary>The last score that <c>TryGetChanged</c> gave.</summary>
        public int LastScore;
    }

    private static Scene BuildScene()
    {
        // The two strings are not literals, so the scene owns them. They differ in content.
        string textA = new('a', 8);
        string textB = new('b', 8);

        Store<int> intStore = new(new Store<int>.CreateInfo { Initial = 0 });
        Store<GameState> stateStore = new(new Store<GameState>.CreateInfo { Initial = new GameState(0, Lives) });
        Store<string> textStore = new(new Store<string>.CreateInfo { Initial = textA });

        IBindingSource[] sources = new IBindingSource[SourceCount];
        sources[0] = intStore;
        sources[1] = stateStore;
        sources[2] = textStore;

        return new Scene
        {
            IntStore = intStore,
            StateStore = stateStore,
            TextStore = textStore,
            TextA = textA,
            TextB = textB,
            Sources = sources,
            Subscription = new StoreSubscription<GameState, int>(stateStore, static s => s.Score),
        };
    }

    /// <summary>
    /// The measured body of the first test. A new value and an equal value on each of the three stores. It uses no
    /// lambda, no boxing, no string, and no Assert.
    /// </summary>
    private static void SetStep(Scene scene)
    {
        int round = ++scene.Round;

        scene.IntStore.Set(round);
        scene.IntStore.Set(round);

        GameState state = new(round, Lives);
        scene.StateStore.Set(state);
        scene.StateStore.Set(state);

        // The odd rounds set TextB and the even rounds set TextA. The start text is TextA, so each round is a change.
        string text = (round & 1) == 1 ? scene.TextB : scene.TextA;
        scene.TextStore.Set(text);
        scene.TextStore.Set(text);
    }

    private static void RunSetSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            SetStep(scene);
        }
    }

    /// <summary>
    /// The measured body of the second test. It adds the version of each store to the sum. It uses no lambda, no boxing,
    /// no string, and no Assert.
    /// </summary>
    private static void ReadStep(Scene scene)
    {
        IBindingSource[] sources = scene.Sources;
        ulong sum = scene.VersionSum;
        for (int i = 0; i < sources.Length; i++)
        {
            sum += sources[i].Version;
        }

        scene.VersionSum = sum;
    }

    private static void RunReadSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            ReadStep(scene);
        }
    }

    /// <summary>
    /// The measured body of the third test. A new <c>GameState</c>, then a read that gives <see langword="true"/>, then
    /// a read that gives <see langword="false"/>. It uses no lambda, no boxing, no string, and no Assert.
    /// </summary>
    private static void ChangedStep(Scene scene)
    {
        int round = ++scene.Round;
        scene.StateStore.Set(new GameState(round, Lives));

        if (scene.Subscription.TryGetChanged(out int score))
        {
            scene.Hits++;
            scene.LastScore = score;
        }
        else
        {
            scene.Misses++;
        }

        if (scene.Subscription.TryGetChanged(out score))
        {
            scene.Hits++;
            scene.LastScore = score;
        }
        else
        {
            scene.Misses++;
        }
    }

    private static void RunChangedSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            ChangedStep(scene);
        }
    }

    /// <summary>
    /// Raises the version of each store to a value that differs from the version of the other stores: 2 for the int
    /// store, 3 for the state store, and 2 for the text store. The sum of the three versions is 7.
    /// </summary>
    private static void RaiseVersions(Scene scene)
    {
        scene.IntStore.Set(10);
        scene.StateStore.Set(new GameState(10, Lives));
        scene.StateStore.Set(new GameState(11, Lives));
        scene.TextStore.Set(scene.TextB);
    }

    /// <summary><c>Set</c> with a new value and with an equal value allocates nothing after warm-up.</summary>
    [Fact]
    public void SetAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunSetSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunSetSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>A version read through <see cref="IBindingSource"/> allocates nothing after warm-up.</summary>
    [Fact]
    public void AVersionReadAllocatesNothing()
    {
        Scene scene = BuildScene();
        RaiseVersions(scene);
        RunReadSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunReadSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // The versions are 2, 3, and 2, so each round adds 7.
        Assert.Equal(7UL * (WarmUpRounds + Iterations), scene.VersionSum);
    }

    /// <summary><c>TryGetChanged</c> with a change and with no change allocates nothing after warm-up.</summary>
    [Fact]
    public void TryGetChangedAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunChangedSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunChangedSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured bodies do real work. After 3 set steps, 1 read step, and 2 changed steps, each store holds the
    /// expected version and the subscription gave the expected count of <see langword="true"/> results. An empty body
    /// cannot pass.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        Assert.Equal(1UL, scene.IntStore.Version);
        Assert.Equal(1UL, scene.StateStore.Version);
        Assert.Equal(1UL, scene.TextStore.Version);

        // Each set step changes each store one time. The equal sets change nothing.
        RunSetSteps(scene, 3);
        Assert.Equal(4UL, scene.IntStore.Version);
        Assert.Equal(4UL, scene.StateStore.Version);
        Assert.Equal(4UL, scene.TextStore.Version);
        Assert.Equal(3, scene.IntStore.Value);
        Assert.Equal(new GameState(3, Lives), scene.StateStore.Value);
        Assert.Same(scene.TextB, scene.TextStore.Value);

        // The read step adds the 3 versions through the interface.
        ReadStep(scene);
        Assert.Equal(12UL, scene.VersionSum);

        // Each changed step sets one new state. The first read of the subscription applies, so each step gives one
        // true and one false. Only the state store changes.
        RunChangedSteps(scene, 2);
        Assert.Equal(2, scene.Hits);
        Assert.Equal(2, scene.Misses);
        Assert.Equal(5, scene.LastScore);
        Assert.Equal(6UL, scene.StateStore.Version);
        Assert.Equal(6UL, scene.Subscription.AppliedVersion);
        Assert.Equal(4UL, scene.IntStore.Version);
        Assert.Equal(4UL, scene.TextStore.Version);
    }
}
