using System;
using System.Collections.Generic;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the binding pass: <see cref="Element.AddBinding(IBindingSource, BindingHandler, object?)"/>, the subtree
/// flag, and the place of the pass in <see cref="UiContext.Update(float)"/>.
/// </summary>
/// <remarks>
/// Each test uses a <see cref="TestSource"/> with a settable version and an <see cref="ApplyLog"/> target. The
/// handlers are static methods in <c>static readonly</c> fields, as a game uses them.
/// </remarks>
public sealed class BindingPassTests
{
    /// <summary>The handler that counts the call and records the name of its log.</summary>
    private static readonly BindingHandler s_record = Record;

    /// <summary>The handler that counts the call and writes the color of its log into the inline color.</summary>
    private static readonly BindingHandler s_writeColor = WriteColor;

    /// <summary>The first update applies each binding of each element one time.</summary>
    [Fact]
    public void TheFirstUpdateAppliesEachBinding()
    {
        UiContext context = new();
        Element root = new();
        Element left = new();
        Element right = new();
        root.Children.Add(left);
        root.Children.Add(right);
        context.AddRoot(root);

        List<string> order = [];
        ApplyLog rootLog = new("root", order);
        ApplyLog leftLog = new("left", order);
        ApplyLog rightLog = new("right", order);
        root.AddBinding(new TestSource(), s_record, rootLog);
        left.AddBinding(new TestSource(), s_record, leftLog);
        right.AddBinding(new TestSource(), s_record, rightLog);
        Assert.Equal(1, root.BindingCount);
        Assert.Equal(0UL, left.Bindings[0].AppliedVersion);

        context.Update(0f);

        Assert.Equal(1, rootLog.Calls);
        Assert.Equal(1, leftLog.Calls);
        Assert.Equal(1, rightLog.Calls);
        Assert.Equal(1UL, left.Bindings[0].AppliedVersion);

        // A parent applies before its children, and the children apply in the order of the child list.
        Assert.Equal(["root", "left", "right"], order);
        Assert.Same(left, leftLog.LastElement);
    }

    /// <summary>An update with no new version of the source calls no handler.</summary>
    [Fact]
    public void AnUpdateWithNoNewVersionAppliesNothing()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        ApplyLog log = new("root", []);
        root.AddBinding(new TestSource(), s_record, log);

        context.Update(0f);
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(1, log.Calls);
    }

    /// <summary>Two new versions before one update give one call with the last version.</summary>
    [Fact]
    public void TwoVersionsBeforeOneUpdateGiveOneApply()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        TestSource source = new();
        ApplyLog log = new("root", []);
        root.AddBinding(source, s_record, log);
        context.Update(0f);
        Assert.Equal(1, log.Calls);

        source.Version++;
        source.Version++;
        context.Update(0f);

        Assert.Equal(2, log.Calls);
        Assert.Equal(3UL, root.Bindings[0].AppliedVersion);
    }

    /// <summary>
    /// The binding pass runs before the style pass: the style pass of the same update reads the inline color that the
    /// handler writes.
    /// </summary>
    [Fact]
    public void TheBindingPassRunsBeforeTheStylePass()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        TestSource source = new();
        ApplyLog log = new("root", []) { Color = new Rgba8(10, 20, 30, 255) };
        root.AddBinding(source, s_writeColor, log);

        context.Update(0f);
        Assert.Equal(new Rgba8(10, 20, 30, 255), root.Style.Color);

        log.Color = new Rgba8(200, 100, 50, 255);
        source.Version++;
        context.Update(0f);

        Assert.Equal(2, log.Calls);
        Assert.Equal(new Rgba8(200, 100, 50, 255), root.Style.Color);
    }

    /// <summary>
    /// A version that an input handler raises reaches the binding pass of the same update, because the input pass
    /// runs first.
    /// </summary>
    [Fact]
    public void AVersionFromAnInputHandlerAppliesInTheSameUpdate()
    {
        UiContext context = new();
        Element root = new();
        Element target = new();
        root.Children.Add(target);
        context.AddRoot(root);
        TestSource source = new();
        ApplyLog log = new("target", []);
        target.AddBinding(source, s_record, log);
        context.Update(0f);
        Assert.Equal(1, log.Calls);

        target.OnPointerDown = (ref PointerEvent e) => source.Version++;
        PointerEvent down = new()
        {
            Header = new UiEventHeader(UiEventType.PointerDown, 0d, target),
            Position = new float2(0f, 0f),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
        context.PushEvent(down);
        context.Update(0f);

        Assert.Equal(2UL, source.Version);
        Assert.Equal(2, log.Calls);
        Assert.Equal(2UL, target.Bindings[0].AppliedVersion);
    }

    /// <summary>
    /// The bindings of one element apply in the order of add. The third add grows the array from 2 slots to 4 slots.
    /// </summary>
    [Fact]
    public void TwoBindingsOnOneElementApplyInTheOrderOfAdd()
    {
        UiContext context = new();
        Element root = new();
        context.AddRoot(root);
        List<string> order = [];
        TestSource firstSource = new();
        TestSource secondSource = new();
        TestSource thirdSource = new();
        root.AddBinding(firstSource, s_record, new ApplyLog("first", order));
        root.AddBinding(secondSource, s_record, new ApplyLog("second", order));
        root.AddBinding(thirdSource, s_record, new ApplyLog("third", order));
        Assert.Equal(3, root.BindingCount);
        Assert.Equal(3, root.Bindings.Length);

        context.Update(0f);
        Assert.Equal(["first", "second", "third"], order);

        // A change of the sources in the reverse order still applies in the order of add.
        order.Clear();
        thirdSource.Version++;
        firstSource.Version++;
        context.Update(0f);

        Assert.Equal(["first", "third"], order);
    }

    /// <summary>The add throws <see cref="ArgumentNullException"/> for a null source or a null handler.</summary>
    [Fact]
    public void AddBindingRejectsANullSourceOrHandler()
    {
        Element element = new();

        ArgumentNullException noSource = Assert.Throws<ArgumentNullException>(
            () => element.AddBinding(null!, s_record, null));
        ArgumentNullException noHandler = Assert.Throws<ArgumentNullException>(
            () => element.AddBinding(new TestSource(), null!, null));

        Assert.Equal("source", noSource.ParamName);
        Assert.Equal("handler", noHandler.ParamName);
        Assert.Equal(0, element.BindingCount);
        Assert.False(element.HasBoundSubtree);
    }

    /// <summary>The add marks the element and each ancestor, and it marks no element of a sibling branch.</summary>
    [Fact]
    public void AddBindingMarksTheBoundPathOnly()
    {
        Element root = new();
        Element branch = new();
        Element leaf = new();
        Element sibling = new();
        Element siblingLeaf = new();
        root.Children.Add(branch);
        branch.Children.Add(leaf);
        root.Children.Add(sibling);
        sibling.Children.Add(siblingLeaf);

        leaf.AddBinding(new TestSource(), s_record, null);

        Assert.True(leaf.HasBoundSubtree);
        Assert.True(branch.HasBoundSubtree);
        Assert.True(root.HasBoundSubtree);
        Assert.False(sibling.HasBoundSubtree);
        Assert.False(siblingLeaf.HasBoundSubtree);
    }

    /// <summary>
    /// An attach of a subtree with a binding marks each new ancestor, so the next update applies the binding.
    /// </summary>
    [Fact]
    public void AnAttachOfABoundSubtreeMarksTheNewAncestors()
    {
        UiContext context = new();
        Element root = new();
        Element host = new();
        Element other = new();
        root.Children.Add(host);
        root.Children.Add(other);
        context.AddRoot(root);
        context.Update(0f);
        Assert.False(root.HasBoundSubtree);

        Element panel = new();
        Element leaf = new();
        panel.Children.Add(leaf);
        ApplyLog log = new("leaf", []);
        leaf.AddBinding(new TestSource(), s_record, log);
        Assert.True(panel.HasBoundSubtree);

        host.Children.Add(panel);

        Assert.True(host.HasBoundSubtree);
        Assert.True(root.HasBoundSubtree);
        Assert.False(other.HasBoundSubtree);

        context.Update(0f);

        Assert.Equal(1, log.Calls);
        Assert.True(root.HasBoundSubtree);
    }

    /// <summary>
    /// A remove leaves a stale flag on the old parent, and the next update clears it. A bound branch keeps its flag.
    /// </summary>
    [Fact]
    public void ABranchWithNoBindingLosesItsMarkAtTheNextUpdate()
    {
        UiContext context = new();
        Element root = new();
        Element parent = new();
        Element child = new();
        Element keeper = new();
        root.Children.Add(parent);
        parent.Children.Add(child);
        root.Children.Add(keeper);
        context.AddRoot(root);
        child.AddBinding(new TestSource(), s_record, null);
        keeper.AddBinding(new TestSource(), s_record, null);
        context.Update(0f);

        Assert.True(parent.Children.Remove(child));
        Assert.True(parent.HasBoundSubtree);

        context.Update(0f);

        Assert.False(parent.HasBoundSubtree);
        Assert.True(keeper.HasBoundSubtree);
        Assert.True(root.HasBoundSubtree);
        Assert.True(child.HasBoundSubtree);
    }

    /// <summary>Counts the call and records the name of the log in the shared order list.</summary>
    /// <param name="element">The bound element.</param>
    /// <param name="target">The <see cref="ApplyLog"/>, or <see langword="null"/> for no record.</param>
    private static void Record(Element element, object? target)
    {
        if (target is ApplyLog log)
        {
            log.Calls++;
            log.LastElement = element;
            log.Order.Add(log.Name);
        }
    }

    /// <summary>Counts the call and writes the color of the log into the inline color of the element.</summary>
    /// <param name="element">The bound element.</param>
    /// <param name="target">The <see cref="ApplyLog"/>.</param>
    private static void WriteColor(Element element, object? target)
    {
        ApplyLog log = (ApplyLog)target!;
        log.Calls++;
        element.Inline.Color = log.Color;
    }

    /// <summary>A source with a settable version. The version starts at 1, as the version of a store.</summary>
    private sealed class TestSource : IBindingSource
    {
        /// <inheritdoc />
        public ulong Version { get; set; } = 1;
    }

    /// <summary>A binding target that counts the calls of its handler and records the order of the calls.</summary>
    /// <param name="name">The name that the record handler adds to the order list.</param>
    /// <param name="order">The order list. The logs of one test can share it.</param>
    private sealed class ApplyLog(string name, List<string> order)
    {
        /// <summary>Gets the name of the log.</summary>
        public string Name { get; } = name;

        /// <summary>Gets the shared order list.</summary>
        public List<string> Order { get; } = order;

        /// <summary>Gets or sets the count of the handler calls.</summary>
        public int Calls { get; set; }

        /// <summary>Gets or sets the element of the last call.</summary>
        public Element? LastElement { get; set; }

        /// <summary>Gets or sets the color that the color handler writes.</summary>
        public Rgba8 Color { get; set; }
    }
}
