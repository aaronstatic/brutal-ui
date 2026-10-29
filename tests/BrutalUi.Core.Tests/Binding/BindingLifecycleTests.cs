using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the binding lifecycle: the leave rule (Decision 6), <see cref="Element.ClearBindings"/>, and the error
/// rule (Decision 7).
/// </summary>
/// <remarks>
/// Each test uses a <see cref="TestSource"/> with a settable version and an <see cref="ApplyLog"/> target. The
/// handlers are static methods in <c>static readonly</c> fields, as a game uses them.
/// </remarks>
public sealed class BindingLifecycleTests
{
    /// <summary>The handler that counts the call.</summary>
    private static readonly BindingHandler s_record = Record;

    /// <summary>The handler that counts the call and throws.</summary>
    private static readonly BindingHandler s_throw = Throw;

    /// <summary>The handler that counts the call and calls <see cref="UiContext.Update(float)"/>.</summary>
    private static readonly BindingHandler s_callUpdate = CallUpdate;

    /// <summary>A removed element gets no apply, also after a new version.</summary>
    [Fact]
    public void ARemovedElementGetsNoApply()
    {
        UiContext context = new();
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        context.AddRoot(root);
        TestSource source = new();
        ApplyLog log = new();
        child.AddBinding(source, s_record, log);
        context.Update(0f);
        Assert.Equal(1, log.Calls);

        Assert.True(root.Children.Remove(child));
        source.Version++;
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(1, log.Calls);
        Assert.Equal(1, child.BindingCount);
    }

    /// <summary>An element that joins again applies the newest version at the next update.</summary>
    [Fact]
    public void AnElementThatJoinsAgainAppliesTheNewVersion()
    {
        UiContext context = new();
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        context.AddRoot(root);
        TestSource source = new();
        ApplyLog log = new();
        child.AddBinding(source, s_record, log);
        context.Update(0f);

        Assert.True(root.Children.Remove(child));
        source.Version++;
        source.Version++;
        context.Update(0f);
        Assert.Equal(1, log.Calls);

        root.Children.Add(child);
        context.Update(0f);

        Assert.Equal(2, log.Calls);
        Assert.Equal(3UL, child.Bindings[0].AppliedVersion);
    }

    /// <summary>An element that joins again with no new version applies nothing.</summary>
    [Fact]
    public void AnElementThatJoinsAgainWithNoNewVersionAppliesNothing()
    {
        UiContext context = new();
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        context.AddRoot(root);
        ApplyLog log = new();
        child.AddBinding(new TestSource(), s_record, log);
        context.Update(0f);

        Assert.True(root.Children.Remove(child));
        context.Update(0f);
        root.Children.Add(child);
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(1, log.Calls);
    }

    /// <summary>A root that leaves the context gets no apply, and it applies again after the add.</summary>
    [Fact]
    public void ARemovedRootGetsNoApply()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        TestSource source = new();
        ApplyLog log = new();
        root.AddBinding(source, s_record, log);
        context.Update(0f);
        Assert.Equal(1, log.Calls);

        Assert.True(context.RemoveRoot(root));
        source.Version++;
        context.Update(0f);
        Assert.Equal(1, log.Calls);

        context.AddRoot(root);
        context.Update(0f);
        Assert.Equal(2, log.Calls);
    }

    /// <summary>An element that moves to another context applies there, and the old context applies nothing.</summary>
    [Fact]
    public void AMoveToAnotherContextAppliesInTheNewContext()
    {
        UiContext first = new();
        UiContext second = new();
        Element firstRoot = new();
        Element secondRoot = new();
        Element child = new();
        firstRoot.Children.Add(child);
        first.AddRoot(firstRoot);
        second.AddRoot(secondRoot);
        TestSource source = new();
        ApplyLog log = new();
        child.AddBinding(source, s_record, log);
        first.Update(0f);
        Assert.Equal(1, log.Calls);

        Assert.True(firstRoot.Children.Remove(child));
        secondRoot.Children.Add(child);
        source.Version++;
        first.Update(0f);
        Assert.Equal(1, log.Calls);

        second.Update(0f);

        Assert.Equal(2, log.Calls);
        Assert.Same(child, log.LastElement);
    }

    /// <summary>
    /// <see cref="Element.ClearBindings"/> writes <see langword="default"/> into each used slot, sets the count to 0,
    /// and stops each apply.
    /// </summary>
    [Fact]
    public void ClearBindingsDropsEachBindingAndItsReferences()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        TestSource first = new();
        TestSource second = new();
        ApplyLog log = new();
        root.AddBinding(first, s_record, log);
        root.AddBinding(second, s_record, log);
        context.Update(0f);
        Assert.Equal(2, log.Calls);

        // The span is a view of the array, so it shows the slots after the clear.
        ReadOnlySpan<ElementBinding> slots = root.Bindings;
        Assert.Equal(2, slots.Length);

        root.ClearBindings();

        Assert.Equal(0, root.BindingCount);
        Assert.Equal(0, root.Bindings.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            Assert.Null(slots[i].Source);
            Assert.Null(slots[i].Handler);
            Assert.Null(slots[i].Target);
            Assert.Equal(0UL, slots[i].AppliedVersion);
        }

        first.Version++;
        second.Version++;
        context.Update(0f);

        Assert.Equal(2, log.Calls);

        // A new binding after the clear works.
        root.AddBinding(first, s_record, log);
        context.Update(0f);
        Assert.Equal(3, log.Calls);
    }

    /// <summary>
    /// A handler that throws leaves <see cref="UiContext.Update(float)"/> before the style pass. The binding keeps the
    /// new version, so the next update does not call that handler again. It applies the bindings after it.
    /// </summary>
    [Fact]
    public void AHandlerThatThrowsUsesUpItsChangeAndStopsTheUpdate()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        ApplyLog throwLog = new();
        ApplyLog laterLog = new();
        root.AddBinding(new TestSource(), s_throw, throwLog);
        root.AddBinding(new TestSource(), s_record, laterLog);

        Assert.Throws<ApplyFailure>(() => context.Update(0f));

        Assert.Equal(1, throwLog.Calls);
        Assert.Equal(0, laterLog.Calls);
        Assert.Equal(1UL, root.Bindings[0].AppliedVersion);
        Assert.Equal(0UL, root.Bindings[1].AppliedVersion);
        Assert.True((root.Dirty & DirtyFlags.Style) != 0);

        context.Update(0f);

        Assert.Equal(1, throwLog.Calls);
        Assert.Equal(1, laterLog.Calls);
        Assert.True((root.Dirty & DirtyFlags.Style) == 0);
    }

    /// <summary>A handler that calls <see cref="UiContext.Update(float)"/> gets an exception, and the context stays valid.</summary>
    [Fact]
    public void AHandlerThatCallsUpdateGetsAnException()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        ApplyLog log = new() { Context = context };
        root.AddBinding(new TestSource(), s_callUpdate, log);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => context.Update(0f));

        Assert.Equal("A binding handler must not call Update.", exception.Message);
        Assert.Equal(1, log.Calls);

        // The flag is clear: the next update runs and the used-up version does not throw again.
        context.Update(0f);

        Assert.Equal(1, log.Calls);
    }

    /// <summary>Counts the call and records the element.</summary>
    /// <param name="element">The bound element.</param>
    /// <param name="target">The <see cref="ApplyLog"/>.</param>
    private static void Record(Element element, object? target)
    {
        ApplyLog log = (ApplyLog)target!;
        log.Calls++;
        log.LastElement = element;
    }

    /// <summary>Counts the call and throws <see cref="ApplyFailure"/>.</summary>
    /// <param name="element">The bound element.</param>
    /// <param name="target">The <see cref="ApplyLog"/>.</param>
    private static void Throw(Element element, object? target)
    {
        ((ApplyLog)target!).Calls++;
        throw new ApplyFailure();
    }

    /// <summary>Counts the call and calls <see cref="UiContext.Update(float)"/> of the context of the log.</summary>
    /// <param name="element">The bound element.</param>
    /// <param name="target">The <see cref="ApplyLog"/>.</param>
    private static void CallUpdate(Element element, object? target)
    {
        ApplyLog log = (ApplyLog)target!;
        log.Calls++;
        log.Context!.Update(0f);
    }

    /// <summary>A source with a settable version. The version starts at 1, as the version of a store.</summary>
    private sealed class TestSource : IBindingSource
    {
        /// <inheritdoc />
        public ulong Version { get; set; } = 1;
    }

    /// <summary>The exception that the throw handler raises.</summary>
    private sealed class ApplyFailure : Exception;

    /// <summary>A binding target that counts the calls of its handler.</summary>
    private sealed class ApplyLog
    {
        /// <summary>Gets or sets the count of the handler calls.</summary>
        public int Calls { get; set; }

        /// <summary>Gets or sets the element of the last call.</summary>
        public Element? LastElement { get; set; }

        /// <summary>Gets or sets the context that the update handler calls.</summary>
        public UiContext? Context { get; set; }
    }
}
