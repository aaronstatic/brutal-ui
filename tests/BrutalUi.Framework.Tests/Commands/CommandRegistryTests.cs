using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="CommandRegistry"/>: the call of a handler with its target, the unknown name, the byte
/// compare, and the null handler.
/// </summary>
public sealed class CommandRegistryTests
{
    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>An invoke calls the handler with its target. The registry then holds one command.</summary>
    [Fact]
    public void AnInvokeCallsTheHandlerWithItsTarget()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);

        bool found = registry.Invoke("save"u8);

        Assert.True(found);
        Assert.Equal(1, counter.Calls);
        Assert.Equal(1, registry.Count);
        Assert.True(registry.Contains("save"u8));
    }

    /// <summary>An invoke of a registered name gives <see langword="true"/>.</summary>
    [Fact]
    public void AnInvokeReturnsTrueForARegisteredName()
    {
        CommandRegistry registry = new();
        registry.Register("open"u8, s_count, new Counter());

        Assert.True(registry.Invoke("open"u8));
    }

    /// <summary>An unknown name gives <see langword="false"/> and calls no handler.</summary>
    [Fact]
    public void AnUnknownNameReturnsFalseAndCallsNoHandler()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);

        bool found = registry.Invoke("load"u8);

        Assert.False(found);
        Assert.Equal(0, counter.Calls);
        Assert.False(registry.Contains("load"u8));
    }

    /// <summary>Each name calls its own handler and its own target.</summary>
    [Fact]
    public void EachNameCallsItsOwnHandler()
    {
        CommandRegistry registry = new();
        Counter first = new();
        Counter second = new();
        registry.Register("first"u8, s_count, first);
        registry.Register("second"u8, s_count, second);

        registry.Invoke("second"u8);
        registry.Invoke("second"u8);
        registry.Invoke("first"u8);

        Assert.Equal(1, first.Calls);
        Assert.Equal(2, second.Calls);
    }

    /// <summary>The names compare by their bytes: a case, a prefix, or a longer name does not match.</summary>
    [Fact]
    public void NamesCompareByTheirBytes()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);

        Assert.False(registry.Invoke("Save"u8));
        Assert.False(registry.Invoke("sav"u8));
        Assert.False(registry.Invoke("saves"u8));
        Assert.Equal(0, counter.Calls);
        Assert.True(registry.Invoke("save"u8));
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A register with a null handler throws and adds no command.</summary>
    [Fact]
    public void RegisterRejectsANullHandler()
    {
        CommandRegistry registry = new();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => registry.Register("save"u8, null!, null));

        Assert.Equal("handler", exception.ParamName);
        Assert.Equal(0, registry.Count);
    }

    /// <summary>An unknown name calls the slot with the bytes of the name. A known name does not.</summary>
    [Fact]
    public void AnUnknownNameCallsTheUnknownCommandSlotWithTheName()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);
        List<byte[]> misses = new();
        registry.UnknownCommand = name => misses.Add(name.ToArray());

        bool unknown = registry.Invoke("load"u8);
        bool known = registry.Invoke("save"u8);

        Assert.False(unknown);
        Assert.True(known);
        Assert.Single(misses);
        Assert.Equal("load"u8.ToArray(), misses[0]);
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>An empty name and a duplicate name throw. The first handler still runs.</summary>
    [Fact]
    public void RegisterRejectsAnEmptyOrDuplicateName()
    {
        CommandRegistry registry = new();
        Counter first = new();
        Counter second = new();
        registry.Register("save"u8, s_count, first);

        ArgumentException duplicate = Assert.Throws<ArgumentException>(
            () => registry.Register("save"u8, s_count, second));
        ArgumentException empty = Assert.Throws<ArgumentException>(
            () => registry.Register(ReadOnlySpan<byte>.Empty, s_count, second));

        Assert.Equal("name", duplicate.ParamName);
        Assert.Equal("name", empty.ParamName);
        Assert.Equal(1, registry.Count);
        Assert.True(registry.Invoke("save"u8));
        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    /// <summary>The register copies the name, so a later change of the source array does not rename the command.</summary>
    [Fact]
    public void RegisterCopiesTheName()
    {
        CommandRegistry registry = new();
        byte[] source = "save"u8.ToArray();
        registry.Register(source, s_count, new Counter());

        source[0] = (byte)'X';

        Assert.True(registry.Invoke("save"u8));
        Assert.False(registry.Invoke("Xave"u8));
    }

    /// <summary>An unregister frees the name and drops the target. The other names still invoke.</summary>
    [Fact]
    public void UnregisterRemovesTheNameAndDropsTheTarget()
    {
        CommandRegistry registry = new();
        Counter first = new();
        Counter second = new();
        Counter third = new();
        registry.Register("first"u8, s_count, first);
        registry.Register("second"u8, s_count, second);
        registry.Register("third"u8, s_count, third);
        WeakReference weak = RegisterTemporary(registry);

        // Four more commands fill the first array (8 of 8 slots). A shift loop that reads one slot too far throws here.
        registry.Register("f0"u8, s_count, null);
        registry.Register("f1"u8, s_count, null);
        registry.Register("f2"u8, s_count, null);
        registry.Register("f3"u8, s_count, null);
        Assert.Equal(8, registry.Count);

        bool removed = registry.Unregister("second"u8);
        bool removedAgain = registry.Unregister("second"u8);
        bool temporaryRemoved = registry.Unregister("temporary"u8);

        Assert.True(removed);
        Assert.False(removedAgain);
        Assert.True(temporaryRemoved);
        Assert.False(registry.Invoke("second"u8));
        Assert.Equal(6, registry.Count);
        Assert.True(registry.Invoke("first"u8));
        Assert.True(registry.Invoke("third"u8));
        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
        Assert.Equal(1, third.Calls);

        // The name is free again.
        registry.Register("second"u8, s_count, second);
        Assert.True(registry.Invoke("second"u8));
        Assert.Equal(1, second.Calls);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.IsAlive);
    }

    /// <summary>A handler can register, unregister, and invoke a command during an invoke.</summary>
    [Fact]
    public void AHandlerCanRegisterUnregisterAndInvokeDuringAnInvoke()
    {
        CommandRegistry registry = new();
        Reentrant target = new(registry);
        registry.Register("a"u8, s_reenter, target);

        // Seven more commands fill the first array (8 of 8 slots), so the register of b in the handler grows it to 16.
        registry.Register("f0"u8, s_count, null);
        registry.Register("f1"u8, s_count, null);
        registry.Register("f2"u8, s_count, null);
        registry.Register("f3"u8, s_count, null);
        registry.Register("f4"u8, s_count, null);
        registry.Register("f5"u8, s_count, null);
        registry.Register("f6"u8, s_count, null);

        bool first = registry.Invoke("a"u8);

        Assert.True(first);
        Assert.Equal(1, target.ACalls);
        Assert.Equal(1, target.BCalls);
        Assert.False(registry.Contains("a"u8));
        Assert.True(registry.Contains("b"u8));

        bool second = registry.Invoke("a"u8);

        Assert.False(second);
        Assert.Equal(1, target.ACalls);
        Assert.Equal(1, target.BCalls);
    }

    /// <summary>An exception of a handler leaves the invoke. The registry stays usable.</summary>
    [Fact]
    public void AHandlerThatThrowsLeavesTheRegistryUsable()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("boom"u8, s_throw, null);
        registry.Register("save"u8, s_count, counter);

        Assert.Throws<InvalidOperationException>(() => registry.Invoke("boom"u8));

        Assert.True(registry.Invoke("save"u8));
        Assert.Equal(1, counter.Calls);
        Assert.Equal(2, registry.Count);
    }

    /// <summary>Registers a command with a target that only the registry holds.</summary>
    /// <param name="registry">The registry.</param>
    /// <returns>A weak reference to the target.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterTemporary(CommandRegistry registry)
    {
        Counter temporary = new();
        registry.Register("temporary"u8, s_count, temporary);
        return new WeakReference(temporary);
    }

    /// <summary>The handler of <c>b</c>.</summary>
    private static readonly CommandHandler s_reenterB = static target => ((Reentrant)target!).BCalls++;

    /// <summary>The handler of <c>a</c>: it registers <c>b</c>, invokes <c>b</c>, and unregisters <c>a</c>.</summary>
    private static readonly CommandHandler s_reenter = static target =>
    {
        Reentrant reentrant = (Reentrant)target!;
        reentrant.ACalls++;
        reentrant.Registry.Register("b"u8, s_reenterB, reentrant);
        reentrant.Registry.Invoke("b"u8);
        reentrant.Registry.Unregister("a"u8);
    };

    /// <summary>The handler that throws.</summary>
    private static readonly CommandHandler s_throw = static _ => throw new InvalidOperationException("boom");

    /// <summary>The target of the tests: a call counter.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>The target of the re-entry test: the registry and two counters.</summary>
    private sealed class Reentrant
    {
        /// <summary>The registry.</summary>
        public readonly CommandRegistry Registry;

        /// <summary>The count of calls of <c>a</c>.</summary>
        public int ACalls;

        /// <summary>The count of calls of <c>b</c>.</summary>
        public int BCalls;

        /// <summary>Creates the target.</summary>
        /// <param name="registry">The registry.</param>
        public Reentrant(CommandRegistry registry)
        {
            Registry = registry;
        }
    }
}
