using System;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="StoreSubscription{T, TValue}"/>: the first read, the pull rule, and the struct copy rule.
/// </summary>
public sealed class StoreSubscriptionTests
{
    private readonly record struct GameState(int Score, int Lives);

    private static Store<GameState> CreateStore(int score = 5, int lives = 3)
    {
        return new Store<GameState>(new Store<GameState>.CreateInfo { Initial = new GameState(score, lives) });
    }

    [Fact]
    public void TheFirstReadAppliesTheCurrentValue()
    {
        var store = CreateStore(score: 5);
        var subscription = new StoreSubscription<GameState, int>(store, static s => s.Score);
        Assert.Equal(0UL, subscription.AppliedVersion);

        bool changed = subscription.TryGetChanged(out int score);

        Assert.True(changed);
        Assert.Equal(5, score);
        Assert.Equal(1UL, subscription.AppliedVersion);
        Assert.Same(store, subscription.Store);
    }

    [Fact]
    public void ASecondReadWithNoChangeGivesNothing()
    {
        var subscription = new StoreSubscription<GameState, int>(CreateStore(score: 5), static s => s.Score);
        Assert.True(subscription.TryGetChanged(out _));

        bool changed = subscription.TryGetChanged(out int score);

        Assert.False(changed);
        Assert.Equal(default, score);
    }

    [Fact]
    public void ASetOfANewSliceGivesOneChange()
    {
        var store = CreateStore(score: 5);
        var subscription = new StoreSubscription<GameState, int>(store, static s => s.Score);
        Assert.True(subscription.TryGetChanged(out _));

        store.Set(new GameState(9, 3));

        Assert.True(subscription.TryGetChanged(out int score));
        Assert.Equal(9, score);
        Assert.False(subscription.TryGetChanged(out _));
    }

    [Fact]
    public void AnEqualSetGivesNoChange()
    {
        var store = CreateStore(score: 5, lives: 3);
        var subscription = new StoreSubscription<GameState, int>(store, static s => s.Score);
        Assert.True(subscription.TryGetChanged(out _));

        store.Set(new GameState(5, 3));

        Assert.False(subscription.TryGetChanged(out _));
        Assert.Equal(1UL, subscription.AppliedVersion);
    }

    [Fact]
    public void TwoSetsBeforeOneReadGiveOneChangeWithTheLastValue()
    {
        var store = CreateStore(score: 5);
        var subscription = new StoreSubscription<GameState, int>(store, static s => s.Score);
        Assert.True(subscription.TryGetChanged(out _));

        store.Set(new GameState(6, 3));
        store.Set(new GameState(7, 3));

        Assert.True(subscription.TryGetChanged(out int score));
        Assert.Equal(7, score);
        Assert.False(subscription.TryGetChanged(out _));
        Assert.Equal(3UL, subscription.AppliedVersion);
    }

    [Fact]
    public void AChangeOfAnotherSliceGivesAChangeWithTheSameSlice()
    {
        var store = CreateStore(score: 5, lives: 3);
        var subscription = new StoreSubscription<GameState, int>(store, static s => s.Score);
        Assert.True(subscription.TryGetChanged(out _));

        // Decision 4: the subscription does not store the last slice.
        store.Set(new GameState(5, 2));

        Assert.True(subscription.TryGetChanged(out int score));
        Assert.Equal(5, score);
    }

    [Fact]
    public void TwoSubscriptionsOnOneStoreKeepTheirOwnVersion()
    {
        var store = CreateStore(score: 5, lives: 3);
        var scoreSubscription = new StoreSubscription<GameState, int>(store, static s => s.Score);
        var livesSubscription = new StoreSubscription<GameState, int>(store, static s => s.Lives);

        Assert.True(scoreSubscription.TryGetChanged(out int score));
        Assert.Equal(5, score);

        store.Set(new GameState(5, 2));

        Assert.True(livesSubscription.TryGetChanged(out int lives));
        Assert.Equal(2, lives);
        Assert.True(scoreSubscription.TryGetChanged(out _));
        Assert.False(scoreSubscription.TryGetChanged(out _));
        Assert.False(livesSubscription.TryGetChanged(out _));
    }

    [Fact]
    public void ASubscriptionInAnArraySlotKeepsItsVersion()
    {
        var store = CreateStore(score: 5);
        var slots = new StoreSubscription<GameState, int>[1];
        slots[0] = new StoreSubscription<GameState, int>(store, static s => s.Score);
        var copy = slots[0];

        Assert.True(slots[0].TryGetChanged(out int score));

        Assert.Equal(5, score);
        Assert.Equal(1UL, slots[0].AppliedVersion);
        Assert.False(slots[0].TryGetChanged(out _));

        // Decision 5: a copy that the call did not touch keeps the old version.
        Assert.Equal(0UL, copy.AppliedVersion);
    }

    [Fact]
    public void TheConstructorRejectsANullStoreOrSelector()
    {
        var store = CreateStore();

        var storeException = Assert.Throws<ArgumentNullException>(
            () => new StoreSubscription<GameState, int>(null!, static s => s.Score));
        Assert.Equal("store", storeException.ParamName);

        var selectorException = Assert.Throws<ArgumentNullException>(
            () => new StoreSubscription<GameState, int>(store, null!));
        Assert.Equal("selector", selectorException.ParamName);
    }

    [Fact]
    public void ADefaultSubscriptionThrows()
    {
        var subscription = default(StoreSubscription<GameState, int>);

        Assert.Throws<InvalidOperationException>(() => subscription.TryGetChanged(out _));
    }
}
