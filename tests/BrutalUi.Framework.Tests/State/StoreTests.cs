using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="Store{T}"/>: the start value, the version rule, and the equality rule.
/// </summary>
public sealed class StoreTests
{
    private readonly record struct Pair(int A, int B);

    private sealed class Box
    {
        public int Field;
    }

    [Fact]
    public void ANewStoreHoldsTheInitialValueAtVersionOne()
    {
        var store = new Store<int>(new Store<int>.CreateInfo { Initial = 7 });

        Assert.Equal(7, store.Value);
        Assert.Equal(1UL, store.Version);
    }

    [Fact]
    public void ASetOfANewValueRaisesTheVersionByOne()
    {
        var store = new Store<int>(new Store<int>.CreateInfo { Initial = 0 });

        store.Set(10);

        Assert.Equal(10, store.Value);
        Assert.Equal(2UL, store.Version);
    }

    [Fact]
    public void ASetOfAnEqualValueKeepsTheVersion()
    {
        var store = new Store<int>(new Store<int>.CreateInfo { Initial = 0 });
        store.Set(10);

        store.Set(10);

        Assert.Equal(10, store.Value);
        Assert.Equal(2UL, store.Version);
    }

    [Fact]
    public void EachChangeRaisesTheVersionByOne()
    {
        var store = new Store<int>(new Store<int>.CreateInfo { Initial = 1 });
        Assert.Equal(1UL, store.Version);

        store.Set(2);
        Assert.Equal(2UL, store.Version);

        store.Set(2);
        Assert.Equal(2UL, store.Version);

        // A return to an old value is a change.
        store.Set(1);
        Assert.Equal(3UL, store.Version);
    }

    [Fact]
    public void ARecordStructComparesByValue()
    {
        var store = new Store<Pair>(new Store<Pair>.CreateInfo { Initial = new Pair(1, 2) });

        store.Set(new Pair(1, 2));
        Assert.Equal(1UL, store.Version);

        store.Set(new Pair(1, 3));
        Assert.Equal(2UL, store.Version);
        Assert.Equal(new Pair(1, 3), store.Value);
    }

    [Fact]
    public void AClassWithNoEqualsComparesByReference()
    {
        var first = new Box { Field = 5 };
        var store = new Store<Box>(new Store<Box>.CreateInfo { Initial = first });

        store.Set(first);
        Assert.Equal(1UL, store.Version);

        store.Set(new Box { Field = 5 });
        Assert.Equal(2UL, store.Version);
    }

    [Fact]
    public void AnEqualStringKeepsTheVersionAndTheOldReference()
    {
        string old = "abc";
        var store = new Store<string>(new Store<string>.CreateInfo { Initial = old });
        string equal = new string('a', 1) + "bc";
        Assert.NotSame(old, equal);

        store.Set(equal);

        Assert.Equal(1UL, store.Version);
        Assert.Same(old, store.Value);
    }

    [Fact]
    public void ANullValueComparesSafely()
    {
        var store = new Store<string?>(new Store<string?>.CreateInfo { Initial = null });

        store.Set(null);
        Assert.Equal(1UL, store.Version);

        store.Set("a");
        Assert.Equal(2UL, store.Version);

        store.Set(null);
        Assert.Equal(3UL, store.Version);
        Assert.Null(store.Value);
    }

    [Fact]
    public void TheStoreIsABindingSource()
    {
        var store = new Store<int>(new Store<int>.CreateInfo { Initial = 0 });
        IBindingSource source = store;
        Assert.Equal(store.Version, source.Version);

        store.Set(1);
        Assert.Equal(store.Version, source.Version);
        Assert.Equal(2UL, source.Version);

        store.Set(2);
        Assert.Equal(store.Version, source.Version);
        Assert.Equal(3UL, source.Version);
    }
}
