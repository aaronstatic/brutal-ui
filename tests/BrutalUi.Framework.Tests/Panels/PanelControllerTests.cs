using System;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="PanelController"/> and <see cref="PanelInstance"/>: the one-time build, the error rules,
/// and the 0-byte rule of a warm call.
/// </summary>
public sealed class PanelControllerTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;

    /// <summary>The factory of the tests. The test passes <see cref="Create"/> as a method group at build time.</summary>
    private sealed class RootFactory
    {
        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The context of the last call.</summary>
        public PanelContext? Seen;

        /// <summary>When <see langword="true"/>, the factory throws <see cref="FactoryFailure"/>.</summary>
        public bool Throw;

        /// <summary>When <see langword="true"/>, the factory returns <see langword="null"/>.</summary>
        public bool ReturnNull;

        /// <summary>The controller that the factory calls <see cref="PanelController.EnsureRoot"/> on, or <see langword="null"/>.</summary>
        public PanelController? Reenter;

        /// <summary>The exception that the call from inside the factory got.</summary>
        public Exception? Caught;

        /// <summary>The command registry that the factory read from the context.</summary>
        public CommandRegistry? Commands;

        /// <summary>The event bus that the factory read from the context.</summary>
        public EventBus? Events;

        /// <summary>The service registry that the factory read from the context.</summary>
        public ServiceRegistry? Services;

        /// <summary>Creates a root.</summary>
        /// <param name="context">The context of the controller.</param>
        /// <returns>A new element, or <see langword="null"/> when <see cref="ReturnNull"/> is set.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            Seen = context;
            Commands = context.Commands;
            Events = context.Events;
            Services = context.Services;

            if (Throw)
            {
                throw new FactoryFailure();
            }

            if (Reenter is not null)
            {
                try
                {
                    Reenter.EnsureRoot();
                }
                catch (InvalidOperationException e)
                {
                    Caught = e;
                }
            }

            return ReturnNull ? null! : new Element();
        }
    }

    /// <summary>The exception that the factory throws.</summary>
    private sealed class FactoryFailure : Exception
    {
    }

    /// <summary>Creates a context with three new registries.</summary>
    /// <returns>The context.</returns>
    private static PanelContext NewContext()
    {
        return new PanelContext
        {
            Commands = new CommandRegistry(),
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
    }

    /// <summary>Creates a definition for a factory.</summary>
    /// <param name="factory">The factory of the test.</param>
    /// <returns>The definition.</returns>
    private static PanelDefinition NewDefinition(RootFactory factory)
    {
        return new PanelDefinition("inventory"u8, "left"u8, factory.Create);
    }

    /// <summary>Creates a mode with three entries in two slots.</summary>
    /// <returns>The mode.</returns>
    private static ModeDefinition NewMode()
    {
        ModePanel[] panels =
        [
            new ModePanel("left"u8, "a"u8),
            new ModePanel("right"u8, "c"u8),
            new ModePanel("left"u8, "b"u8),
        ];
        return new ModeDefinition("explore"u8, "main"u8, panels);
    }

    /// <summary>
    /// The measured body. A warm <c>EnsureRoot</c>, the state reads, each name length, and each mode getter. It uses no
    /// lambda, no string, no array, and no Assert.
    /// </summary>
    private static int ReadStep(PanelController controller, LayoutDefinition layout, ModeDefinition mode, ModePanel placement)
    {
        int sum = 0;
        if (controller.EnsureRoot() is not null)
        {
            sum++;
        }

        if (controller.Instance.IsBuilt)
        {
            sum++;
        }

        if (controller.Instance.Root is not null)
        {
            sum++;
        }

        sum += controller.Definition.Name.Length;
        sum += controller.Definition.DefaultSlot.Length;
        sum += layout.Name.Length;
        sum += mode.Name.Length;
        sum += mode.Layout.Length;
        sum += mode.SlotCount;
        sum += placement.Slot.Length;
        sum += placement.Panel.Length;
        if (mode.ActionMap is null)
        {
            sum++;
        }

        for (int slot = 0; slot < mode.SlotCount; slot++)
        {
            sum += mode.GetSlotName(slot).Length;
            int count = mode.GetPanelCount(slot);
            sum += count;
            for (int index = 0; index < count; index++)
            {
                sum += mode.GetPanelName(slot, index).Length;
            }
        }

        return sum;
    }

    private static int RunReadSteps(
        PanelController controller,
        LayoutDefinition layout,
        ModeDefinition mode,
        ModePanel placement,
        int steps)
    {
        int sum = 0;
        for (int i = 0; i < steps; i++)
        {
            sum += ReadStep(controller, layout, mode, placement);
        }

        return sum;
    }

    [Fact]
    public void EnsureRootBuildsTheRootOneTime()
    {
        RootFactory factory = new();
        PanelController controller = new(NewDefinition(factory), NewContext());

        Element first = controller.EnsureRoot();

        Assert.NotNull(first);
        Assert.Equal(1, factory.Calls);
        Assert.True(controller.Instance.IsBuilt);
        Assert.Same(first, controller.Instance.Root);

        Element second = controller.EnsureRoot();

        Assert.Same(first, second);
        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public void ANewControllerHoldsAnUnbuiltInstance()
    {
        RootFactory factory = new();
        PanelDefinition definition = NewDefinition(factory);
        PanelContext context = NewContext();

        PanelController controller = new(definition, context);

        Assert.False(controller.Instance.IsBuilt);
        Assert.Null(controller.Instance.Root);
        Assert.Same(definition, controller.Definition);
        Assert.Same(definition, controller.Instance.Definition);
        Assert.Same(context, controller.Context);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public void TheFactoryGetsTheContextOfTheController()
    {
        RootFactory factory = new();
        PanelContext context = NewContext();
        PanelController controller = new(NewDefinition(factory), context);

        controller.EnsureRoot();

        Assert.Same(context, factory.Seen);
        Assert.Same(context.Commands, factory.Commands);
        Assert.Same(context.Events, factory.Events);
        Assert.Same(context.Services, factory.Services);
    }

    [Fact]
    public void AFactoryThatThrowsLeavesTheControllerUnbuiltAndTheNextCallTriesAgain()
    {
        RootFactory factory = new() { Throw = true };
        PanelController controller = new(NewDefinition(factory), NewContext());

        Assert.Throws<FactoryFailure>(() => controller.EnsureRoot());

        Assert.False(controller.Instance.IsBuilt);
        Assert.Equal(1, factory.Calls);

        factory.Throw = false;
        Element root = controller.EnsureRoot();

        Assert.True(controller.Instance.IsBuilt);
        Assert.Same(root, controller.Instance.Root);
        Assert.Equal(2, factory.Calls);
    }

    [Fact]
    public void AFactoryThatReturnsNullThrowsAndLeavesTheControllerUnbuilt()
    {
        RootFactory factory = new() { ReturnNull = true };
        PanelController controller = new(NewDefinition(factory), NewContext());

        Assert.Throws<InvalidOperationException>(() => controller.EnsureRoot());

        Assert.False(controller.Instance.IsBuilt);
        Assert.Null(controller.Instance.Root);

        factory.ReturnNull = false;
        Element root = controller.EnsureRoot();

        Assert.Same(root, controller.Instance.Root);
        Assert.Equal(2, factory.Calls);
    }

    [Fact]
    public void ACallFromInsideTheFactoryThrows()
    {
        RootFactory factory = new();
        PanelController controller = new(NewDefinition(factory), NewContext());
        factory.Reenter = controller;

        Element root = controller.EnsureRoot();

        Assert.IsType<InvalidOperationException>(factory.Caught);
        Assert.True(controller.Instance.IsBuilt);
        Assert.Same(root, controller.Instance.Root);
        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public void TwoControllersOfOneDefinitionBuildTwoRoots()
    {
        RootFactory factory = new();
        PanelDefinition definition = NewDefinition(factory);
        PanelContext context = NewContext();
        PanelController one = new(definition, context);
        PanelController two = new(definition, context);

        Element first = one.EnsureRoot();
        Element second = two.EnsureRoot();

        Assert.NotSame(first, second);
        Assert.Equal(2, factory.Calls);
        Assert.NotSame(one.Instance, two.Instance);
    }

    [Fact]
    public void TheRootStaysTheSameAfterADetach()
    {
        RootFactory factory = new();
        PanelController controller = new(NewDefinition(factory), NewContext());
        UiContext ui = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() });

        Element root = controller.EnsureRoot();
        ui.AddRoot(root);
        Assert.True(ui.RemoveRoot(root));

        Assert.Same(root, controller.EnsureRoot());
        Assert.Same(root, controller.Instance.Root);
        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public void TheConstructorRejectsANullDefinitionOrContext()
    {
        RootFactory factory = new();
        PanelDefinition definition = NewDefinition(factory);
        PanelContext context = NewContext();

        ArgumentNullException noDefinition = Assert.Throws<ArgumentNullException>(() => new PanelController(null!, context));
        ArgumentNullException noContext = Assert.Throws<ArgumentNullException>(() => new PanelController(definition, null!));

        Assert.Equal("definition", noDefinition.ParamName);
        Assert.Equal("context", noContext.ParamName);
    }

    [Fact]
    public void EnsureRootAndTheReadsAllocateNothingAfterTheBuild()
    {
        RootFactory factory = new();
        PanelController controller = new(NewDefinition(factory), NewContext());
        LayoutDefinition layout = new("main"u8, factory.Create);
        ModeDefinition mode = NewMode();
        ModePanel placement = new("left"u8, "inventory"u8);
        controller.EnsureRoot();

        int warm = RunReadSteps(controller, layout, mode, placement, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = RunReadSteps(controller, layout, mode, placement, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // The body does real work: each step adds the same positive sum.
        // The sum includes Slot.Length ("left" = 4), Panel.Length ("inventory" = 9), and 1 for a null ActionMap.
        Assert.True(warm > 0);
        Assert.Equal(warm / WarmUpRounds * Iterations, sum);
        Assert.True(sum >= (4 + 9 + 1) * Iterations);
        Assert.Equal(1, factory.Calls);
    }
}
