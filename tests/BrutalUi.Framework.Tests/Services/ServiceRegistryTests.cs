using System;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="ServiceRegistry"/>: the resolve, the exact type, the missing service, the rejects,
/// and the count.
/// </summary>
public sealed class ServiceRegistryTests
{
    /// <summary>The interface that the tests register and resolve.</summary>
    private interface IAudio
    {
    }

    /// <summary>The class of the service.</summary>
    private sealed class Audio : IAudio
    {
    }

    [Fact]
    public void AResolveGivesTheRegisteredService()
    {
        ServiceRegistry registry = new();
        Audio audio = new();

        registry.Register(audio);

        Assert.Same(audio, registry.Resolve<Audio>());
        Assert.True(registry.TryResolve(out Audio? found));
        Assert.Same(audio, found);
    }

    [Fact]
    public void AnInterfaceResolvesTheServiceThatWasRegisteredAsTheInterface()
    {
        ServiceRegistry registry = new();
        Audio audio = new();

        registry.Register<IAudio>(audio);

        Assert.Same(audio, registry.Resolve<IAudio>());
    }

    [Fact]
    public void AMissingServiceGivesNullAndThrowsNothing()
    {
        ServiceRegistry registry = new();

        IAudio? audio = registry.Resolve<IAudio>();

        Assert.Null(audio);
    }

    [Fact]
    public void TryResolveGivesFalseAndNullForAMissingService()
    {
        ServiceRegistry registry = new();

        bool found = registry.TryResolve(out IAudio? audio);

        Assert.False(found);
        Assert.Null(audio);
    }

    [Fact]
    public void TheTypeMustMatchExactly()
    {
        ServiceRegistry registry = new();
        registry.Register(new Audio());

        Assert.Null(registry.Resolve<IAudio>());
        Assert.False(registry.TryResolve(out IAudio? audio));
        Assert.Null(audio);
    }

    [Fact]
    public void RegisterRejectsANullServiceOrADuplicateType()
    {
        ServiceRegistry registry = new();
        Audio first = new();
        registry.Register<IAudio>(first);

        Assert.Throws<ArgumentNullException>(() => registry.Register<Audio>(null!));
        Assert.Throws<ArgumentException>(() => registry.Register<IAudio>(new Audio()));

        Assert.Same(first, registry.Resolve<IAudio>());
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void CountShowsEachRegisteredService()
    {
        ServiceRegistry registry = new();
        Assert.Equal(0, registry.Count);

        registry.Register<IAudio>(new Audio());
        Assert.Equal(1, registry.Count);

        registry.Register(new CommandRegistry());
        registry.Register(new EventBus());
        Assert.Equal(3, registry.Count);
    }
}
