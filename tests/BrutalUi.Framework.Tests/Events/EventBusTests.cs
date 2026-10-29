using System;
using System.Collections.Generic;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="EventBus"/>: the order of the calls, the exact type, the unsubscribe, the snapshot rule,
/// the publish inside a handler, the exception of a subscriber, and the null handler.
/// </summary>
public sealed class EventBusTests
{
    /// <summary>The handler that records the score and the order in its <see cref="Log"/> target.</summary>
    private static readonly EventBusHandler<ScoreChanged> s_record = static (in ScoreChanged e, object? target) =>
    {
        Log log = (Log)target!;
        log.Calls++;
        log.LastScore = e.Score;
        log.Order?.Add(log.Name);
    };

    /// <summary>The handler that records a <see cref="Paused"/> event in its <see cref="Log"/> target.</summary>
    private static readonly EventBusHandler<Paused> s_recordPaused = static (in Paused e, object? target) =>
    {
        Log log = (Log)target!;
        log.Calls++;
        log.PausedOn = e.On;
    };

    /// <summary>The handler that subscribes the late log one time.</summary>
    private static readonly EventBusHandler<ScoreChanged> s_subscribeLate = static (in ScoreChanged e, object? target) =>
    {
        Spawner spawner = (Spawner)target!;
        if (!spawner.Done)
        {
            spawner.Done = true;
            spawner.Bus.Subscribe(s_record, spawner.Late);
        }
    };

    /// <summary>The handler that unsubscribes the victim log.</summary>
    private static readonly EventBusHandler<ScoreChanged> s_unsubscribeVictim = static (in ScoreChanged e, object? target) =>
    {
        Remover remover = (Remover)target!;
        remover.Bus.Unsubscribe(s_record, remover.Victim);
    };

    /// <summary>The handler that publishes a <see cref="Paused"/> event.</summary>
    private static readonly EventBusHandler<ScoreChanged> s_republish = static (in ScoreChanged e, object? target) =>
    {
        Relay relay = (Relay)target!;
        relay.Bus.Publish(new Paused(true));
    };

    /// <summary>The handler that throws one time when it is armed.</summary>
    private static readonly EventBusHandler<ScoreChanged> s_throwOnce = static (in ScoreChanged e, object? target) =>
    {
        Thrower thrower = (Thrower)target!;
        if (thrower.Armed)
        {
            thrower.Armed = false;
            throw new InvalidOperationException("The subscriber fails.");
        }
    };

    /// <summary>A publish calls each subscriber with its target, in the order of the subscribe.</summary>
    [Fact]
    public void APublishCallsEachSubscriberWithItsTargetInOrder()
    {
        EventBus bus = new();
        List<string> order = new();
        Log first = new("first", order);
        Log second = new("second", order);
        Log third = new("third", order);
        bus.Subscribe(s_record, first);
        bus.Subscribe(s_record, second);
        bus.Subscribe(s_record, third);

        bus.Publish(new ScoreChanged(42));

        Assert.Equal(new[] { "first", "second", "third" }, order);
        Assert.Equal(42, first.LastScore);
        Assert.Equal(42, second.LastScore);
        Assert.Equal(42, third.LastScore);
        Assert.Equal(3, bus.SubscriberCount<ScoreChanged>());
    }

    /// <summary>A publish of a type with no subscriber does nothing.</summary>
    [Fact]
    public void APublishWithNoSubscriberDoesNothing()
    {
        EventBus bus = new();

        bus.Publish(new ScoreChanged(1));

        Assert.Equal(0, bus.SubscriberCount<ScoreChanged>());
    }

    /// <summary>A subscriber of another type gets no call.</summary>
    [Fact]
    public void ASubscriberOfAnotherTypeGetsNoCall()
    {
        EventBus bus = new();
        Log paused = new("paused", null);
        Log score = new("score", null);
        bus.Subscribe(s_recordPaused, paused);
        bus.Subscribe(s_record, score);

        bus.Publish(new ScoreChanged(3));

        Assert.Equal(0, paused.Calls);
        Assert.Equal(1, score.Calls);

        bus.Publish(new Paused(true));

        Assert.Equal(1, paused.Calls);
        Assert.True(paused.PausedOn);
        Assert.Equal(1, score.Calls);
    }

    /// <summary>An unsubscribe removes one subscriber and gives <see langword="false"/> for a second try.</summary>
    [Fact]
    public void UnsubscribeRemovesOneSubscriber()
    {
        EventBus bus = new();
        Log first = new("first", null);
        Log second = new("second", null);
        bus.Subscribe(s_record, first);
        bus.Subscribe(s_record, second);

        bool removed = bus.Unsubscribe(s_record, first);
        bool removedAgain = bus.Unsubscribe(s_record, first);
        bus.Publish(new ScoreChanged(9));

        Assert.True(removed);
        Assert.False(removedAgain);
        Assert.Equal(1, bus.SubscriberCount<ScoreChanged>());
        Assert.Equal(0, first.Calls);
        Assert.Equal(1, second.Calls);

        // The removal of the last subscriber of the type leaves a working channel.
        Assert.True(bus.Unsubscribe(s_record, second));
        Assert.Equal(0, bus.SubscriberCount<ScoreChanged>());

        bus.Publish(new ScoreChanged(10));

        Assert.Equal(1, second.Calls);

        bus.Subscribe(s_record, second);
        bus.Publish(new ScoreChanged(11));

        Assert.Equal(1, bus.SubscriberCount<ScoreChanged>());
        Assert.Equal(2, second.Calls);
        Assert.Equal(11, second.LastScore);
    }

    /// <summary>
    /// The same pair twice gets two calls. One unsubscribe removes the first match and leaves one call.
    /// </summary>
    [Fact]
    public void TheSamePairTwiceGetsTwoCalls()
    {
        EventBus bus = new();
        List<string> order = new();
        Log log = new("log", order);
        Log other = new("other", order);
        bus.Subscribe(s_record, log);
        bus.Subscribe(s_record, other);
        bus.Subscribe(s_record, log);

        bus.Publish(new ScoreChanged(1));

        Assert.Equal(2, log.Calls);
        Assert.Equal(new[] { "log", "other", "log" }, order);

        Assert.True(bus.Unsubscribe(s_record, log));
        bus.Publish(new ScoreChanged(2));

        Assert.Equal(3, log.Calls);
        Assert.Equal(2, bus.SubscriberCount<ScoreChanged>());

        // The first match goes, so the order ends with "other", "log". A removal of the last match gives "log", "other".
        Assert.Equal(new[] { "log", "other", "log", "other", "log" }, order);
    }

    /// <summary>A subscribe during a publish gets the next publish only.</summary>
    [Fact]
    public void ASubscribeDuringAPublishGetsTheNextPublishOnly()
    {
        EventBus bus = new();
        Log late = new("late", null);
        Spawner spawner = new(bus, late);
        bus.Subscribe(s_subscribeLate, spawner);

        bus.Publish(new ScoreChanged(1));

        Assert.Equal(0, late.Calls);
        Assert.Equal(2, bus.SubscriberCount<ScoreChanged>());

        bus.Publish(new ScoreChanged(2));

        Assert.Equal(1, late.Calls);
        Assert.Equal(2, late.LastScore);
    }

    /// <summary>An unsubscribe during a publish takes effect at the next publish.</summary>
    [Fact]
    public void AnUnsubscribeDuringAPublishTakesEffectAtTheNextPublish()
    {
        EventBus bus = new();
        Log victim = new("victim", null);
        Remover remover = new(bus, victim);
        bus.Subscribe(s_unsubscribeVictim, remover);
        bus.Subscribe(s_record, victim);

        bus.Publish(new ScoreChanged(1));

        Assert.Equal(1, victim.Calls);
        Assert.Equal(1, bus.SubscriberCount<ScoreChanged>());

        bus.Publish(new ScoreChanged(2));

        Assert.Equal(1, victim.Calls);
        Assert.Equal(1, victim.LastScore);
    }

    /// <summary>A publish inside a handler reaches each subscriber of both types.</summary>
    [Fact]
    public void APublishInsideAHandlerReachesEachSubscriber()
    {
        EventBus bus = new();
        Log score = new("score", null);
        Log paused = new("paused", null);
        bus.Subscribe(s_republish, new Relay(bus));
        bus.Subscribe(s_record, score);
        bus.Subscribe(s_recordPaused, paused);

        bus.Publish(new ScoreChanged(5));

        Assert.Equal(1, paused.Calls);
        Assert.True(paused.PausedOn);
        Assert.Equal(1, score.Calls);
        Assert.Equal(5, score.LastScore);
    }

    /// <summary>
    /// An exception of a subscriber leaves the publish and stops it. The next publish reaches each subscriber.
    /// </summary>
    [Fact]
    public void ASubscriberThatThrowsStopsThePublish()
    {
        EventBus bus = new();
        Log before = new("before", null);
        Log after = new("after", null);
        bus.Subscribe(s_record, before);
        bus.Subscribe(s_throwOnce, new Thrower { Armed = true });
        bus.Subscribe(s_record, after);

        Assert.Throws<InvalidOperationException>(() => bus.Publish(new ScoreChanged(1)));

        Assert.Equal(1, before.Calls);
        Assert.Equal(0, after.Calls);

        bus.Publish(new ScoreChanged(2));

        Assert.Equal(2, before.Calls);
        Assert.Equal(1, after.Calls);
        Assert.Equal(2, after.LastScore);
    }

    /// <summary>A null handler throws and leaves the bus unchanged.</summary>
    [Fact]
    public void SubscribeRejectsANullHandler()
    {
        EventBus bus = new();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => bus.Subscribe<ScoreChanged>(null!, null));

        Assert.Equal("handler", exception.ParamName);
        Assert.Equal(0, bus.SubscriberCount<ScoreChanged>());
    }

    /// <summary>The event of a score change.</summary>
    /// <param name="Score">The new score.</param>
    private readonly record struct ScoreChanged(int Score);

    /// <summary>The event of a pause change.</summary>
    /// <param name="On">Whether the game is paused.</param>
    private readonly record struct Paused(bool On);

    /// <summary>A target that records the calls, the last score, and the order.</summary>
    private sealed class Log(string name, List<string>? order)
    {
        /// <summary>Gets the name of the target.</summary>
        public string Name { get; } = name;

        /// <summary>Gets the list that receives the name at each call, or <see langword="null"/>.</summary>
        public List<string>? Order { get; } = order;

        /// <summary>Gets or sets the count of calls.</summary>
        public int Calls { get; set; }

        /// <summary>Gets or sets the last score.</summary>
        public int LastScore { get; set; }

        /// <summary>Gets or sets the last pause flag.</summary>
        public bool PausedOn { get; set; }
    }

    /// <summary>A target that subscribes a late log during a publish.</summary>
    private sealed class Spawner(EventBus bus, Log late)
    {
        /// <summary>Gets the bus.</summary>
        public EventBus Bus { get; } = bus;

        /// <summary>Gets the log that the handler subscribes.</summary>
        public Log Late { get; } = late;

        /// <summary>Gets or sets a value that shows the handler did its subscribe.</summary>
        public bool Done { get; set; }
    }

    /// <summary>A target that unsubscribes a victim during a publish.</summary>
    private sealed class Remover(EventBus bus, Log victim)
    {
        /// <summary>Gets the bus.</summary>
        public EventBus Bus { get; } = bus;

        /// <summary>Gets the log that the handler unsubscribes.</summary>
        public Log Victim { get; } = victim;
    }

    /// <summary>A target that publishes a second event.</summary>
    private sealed class Relay(EventBus bus)
    {
        /// <summary>Gets the bus.</summary>
        public EventBus Bus { get; } = bus;
    }

    /// <summary>A target that throws one time.</summary>
    private sealed class Thrower
    {
        /// <summary>Gets or sets a value that arms the throw.</summary>
        public bool Armed { get; set; }
    }
}
