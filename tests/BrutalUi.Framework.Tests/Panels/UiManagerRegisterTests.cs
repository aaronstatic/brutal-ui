using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the register checks, the applying guard, and the error rule of <see cref="UiManager"/>: each check
/// comes before a change, a call from inside the diff throws, and a factory exception leaves the update with the
/// target intact.
/// </summary>
/// <remarks>
/// Each test builds its own context with a <see cref="RecordingRenderer"/> and a viewport of 400 by 300, and its own
/// panel, layout, and mode names in unique UTF-8 literals. To read the tree between the diff and the passes, a test
/// calls <see cref="UiContext.OnBeforeUpdate"/> directly, then runs the update.
/// </remarks>
public sealed class UiManagerRegisterTests
{
    /// <summary>The message of the fault of <see cref="FaultyFactory"/>.</summary>
    private const string FaultMessage = "The panel factory fails on purpose.";

    /// <summary>The factory of a plain panel. Each call counts and records the root it creates.</summary>
    private sealed class PanelFactory
    {
        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The root of the last call, or <see langword="null"/> before the first call.</summary>
        public Element? Last;

        /// <summary>Creates a root of 100 by 50.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            Element root = new();
            root.Inline.Width = 100;
            root.Inline.Height = 50;
            Last = root;
            return root;
        }
    }

    /// <summary>The factory of a panel that throws while <see cref="Fails"/> is <see langword="true"/>.</summary>
    private sealed class FaultyFactory
    {
        /// <summary><see langword="true"/> makes each call throw. The test clears it to let the next call succeed.</summary>
        public bool Fails = true;

        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The root of the last call that returned, or <see langword="null"/> before such a call.</summary>
        public Element? Last;

        /// <summary>Creates a root of 100 by 50, or throws.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        /// <exception cref="NotSupportedException">The call throws when <see cref="Fails"/> is <see langword="true"/>.</exception>
        public Element Create(PanelContext context)
        {
            Calls++;
            if (Fails)
            {
                throw new NotSupportedException(FaultMessage);
            }

            Element root = new();
            root.Inline.Width = 100;
            root.Inline.Height = 50;
            Last = root;
            return root;
        }
    }

    /// <summary>The factory of the layout with the slots <c>left</c> and <c>right</c>. Each call counts.</summary>
    private sealed class LayoutFactory
    {
        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The root of the last call, or <see langword="null"/> before the first call.</summary>
        public Element? Last;

        /// <summary>The slot <c>left</c> of the root of the last call.</summary>
        public SlotContainer? Left;

        /// <summary>The slot <c>right</c> of the root of the last call.</summary>
        public SlotContainer? Right;

        /// <summary>Creates a root of 400 by 300 with the slots <c>left</c> and <c>right</c>.</summary>
        /// <param name="context">The context of the manager. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            Left = new SlotContainer("left"u8);
            Right = new SlotContainer("right"u8);
            Element root = new();
            root.Inline.Width = 400;
            root.Inline.Height = 300;
            root.IsPickable = false;
            root.Children.Add(Left);
            root.Children.Add(Right);
            Last = root;
            return root;
        }
    }

    /// <summary>The layout factory of one detached root element with no slot, used after a rejected register.</summary>
    /// <param name="context">The context of the manager. The factory reads nothing from it.</param>
    /// <returns>A new root element.</returns>
    private static Element EmptyRoot(PanelContext context)
    {
        Element root = new();
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        root.IsPickable = false;
        return root;
    }

    /// <summary>The layout factory that returns <see langword="null"/>.</summary>
    /// <param name="context">The context of the manager. The factory reads nothing from it.</param>
    /// <returns><see langword="null"/> always.</returns>
    private static Element NullRoot(PanelContext context)
    {
        return null!;
    }

    /// <summary>
    /// The panel factory of the diff probe: it calls <see cref="UiManager.SetMode"/>,
    /// <see cref="ModeHistory.Back"/>, and <see cref="UiManager.Register(PanelDefinition)"/> from inside the diff,
    /// catches each exception, and records its type.
    /// </summary>
    private sealed class DiffProbeFactory
    {
        /// <summary>The manager under test. Set it before the diff runs.</summary>
        public UiManager Manager = null!;

        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The type of the exception of the <see cref="UiManager.SetMode"/> call.</summary>
        public Type? SetModeFault;

        /// <summary>The type of the exception of the <see cref="ModeHistory.Back"/> call.</summary>
        public Type? BackFault;

        /// <summary>The type of the exception of the <see cref="UiManager.Register(PanelDefinition)"/> call.</summary>
        public Type? RegisterFault;

        /// <summary>The root of the last call, or <see langword="null"/> before the first call.</summary>
        public Element? Last;

        /// <summary>Runs the three probe calls, then creates a root of 100 by 50.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            try
            {
                Manager.SetMode("rg6.b"u8);
            }
            catch (Exception e)
            {
                SetModeFault = e.GetType();
            }

            try
            {
                Manager.History.Back();
            }
            catch (Exception e)
            {
                BackFault = e.GetType();
            }

            try
            {
                Manager.Register(new PanelDefinition("rg6.late"u8, "left"u8, EmptyPanel));
            }
            catch (Exception e)
            {
                RegisterFault = e.GetType();
            }

            Element root = new();
            root.Inline.Width = 100;
            root.Inline.Height = 50;
            Last = root;
            return root;
        }

        /// <summary>The factory of the panel that the probe tries to register during the diff.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        private static Element EmptyPanel(PanelContext context)
        {
            return new Element();
        }
    }

    /// <summary>The layout factory with two slots named <c>left</c> at different depths.</summary>
    /// <param name="context">The context of the manager. The factory reads nothing from it.</param>
    /// <returns>A new root with one slot under a column element and one slot as a direct child.</returns>
    private static Element DuplicateSlotRoot(PanelContext context)
    {
        Element root = new();
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        root.IsPickable = false;
        Element column = new();
        column.Children.Add(new SlotContainer("left"u8));
        root.Children.Add(column);
        root.Children.Add(new SlotContainer("left"u8));
        return root;
    }

    /// <summary>Creates a context with a recording measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400f, 300f),
        };
    }

    /// <summary>Creates a panel context with three new registries.</summary>
    /// <returns>The context.</returns>
    private static PanelContext NewPanels()
    {
        return new PanelContext
        {
            Commands = new CommandRegistry(),
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
    }

    /// <summary>
    /// The null rule: each <see cref="UiManager.Register(PanelDefinition)"/> overload throws
    /// <see cref="ArgumentNullException"/> for a <see langword="null"/> definition, and the manager stays empty: a
    /// later register of a real definition works.
    /// </summary>
    [Fact]
    public void RegisterRejectsANullDefinition()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });

        ArgumentNullException e1 = Assert.Throws<ArgumentNullException>(
            () => ui.Register((PanelDefinition)null!));
        Assert.Equal("panel", e1.ParamName);

        ArgumentNullException e2 = Assert.Throws<ArgumentNullException>(
            () => ui.Register((LayoutDefinition)null!));
        Assert.Equal("layout", e2.ParamName);

        ArgumentNullException e3 = Assert.Throws<ArgumentNullException>(
            () => ui.Register((ModeDefinition)null!));
        Assert.Equal("mode", e3.ParamName);

        PanelFactory panel = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("rg1.a"u8, "left"u8, panel.Create));
        ui.Register(new LayoutDefinition("rg1.main"u8, layout.Create));
        ModePanel[] entries = [new ModePanel("left"u8, "rg1.a"u8)];
        ui.Register(new ModeDefinition("rg1.mode"u8, "rg1.main"u8, entries));
        ui.SetMode("rg1.mode"u8);
        context.Update(0f);

        Assert.Equal(1, panel.Calls);
        Assert.Same(panel.Last, layout.Left!.Children[0]);
    }

    /// <summary>
    /// The duplicate rule: a second register of a panel name, a layout name, or a mode name throws
    /// <see cref="ArgumentException"/>, the first record stays, and the second factory never runs.
    /// </summary>
    [Fact]
    public void RegisterRejectsADuplicateName()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory first = new();
        PanelFactory second = new();
        LayoutFactory firstLayout = new();
        LayoutFactory secondLayout = new();
        ui.Register(new PanelDefinition("rg2.a"u8, "left"u8, first.Create));
        ui.Register(new LayoutDefinition("rg2.main"u8, firstLayout.Create));

        ArgumentException panelFault = Assert.Throws<ArgumentException>(
            () => ui.Register(new PanelDefinition("rg2.a"u8, "left"u8, second.Create)));
        Assert.Equal("panel", panelFault.ParamName);

        ArgumentException layoutFault = Assert.Throws<ArgumentException>(
            () => ui.Register(new LayoutDefinition("rg2.main"u8, secondLayout.Create)));
        Assert.Equal("layout", layoutFault.ParamName);

        ModePanel[] firstEntries = [new ModePanel("left"u8, "rg2.a"u8)];
        ModeDefinition firstMode = new("rg2.mode"u8, "rg2.main"u8, firstEntries);
        ui.Register(firstMode);
        ModePanel[] secondEntries = [new ModePanel("right"u8, "rg2.a"u8)];
        ArgumentException modeFault = Assert.Throws<ArgumentException>(
            () => ui.Register(new ModeDefinition("rg2.mode"u8, "rg2.main"u8, secondEntries)));
        Assert.Equal("mode", modeFault.ParamName);

        ui.SetMode("rg2.mode"u8);
        context.Update(0f);

        Assert.Equal(0, second.Calls);
        Assert.Equal(0, secondLayout.Calls);
        Assert.Same(firstMode, ui.CurrentMode);
        Assert.Same(firstLayout.Last, context.Roots[0]);
        Assert.Same(first.Last, firstLayout.Left!.Children[0]);
        Assert.Equal(0, firstLayout.Right!.Children.Count);
    }

    /// <summary>
    /// The layout build rule: the factory runs one time at the register, the root is not in the context yet, a
    /// factory that returns <see langword="null"/>, and a factory that returns an element with a parent, throw
    /// <see cref="InvalidOperationException"/>. A rejected register leaves the name free.
    /// </summary>
    [Fact]
    public void RegisterOfALayoutBuildsItsRootOneTimeAndChecksIt()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory good = new();
        ui.Register(new LayoutDefinition("rg3.main"u8, good.Create));

        Assert.Equal(1, good.Calls);
        Assert.Equal(0, context.Roots.Length);

        InvalidOperationException nullFault = Assert.Throws<InvalidOperationException>(
            () => ui.Register(new LayoutDefinition("rg3.nil"u8, NullRoot)));
        Assert.Contains("null", nullFault.Message);
        ui.Register(new LayoutDefinition("rg3.nil"u8, EmptyRoot));

        Element holder = new();
        Element attached = new();
        holder.Children.Add(attached);
        InvalidOperationException parentFault = Assert.Throws<InvalidOperationException>(
            () => ui.Register(new LayoutDefinition("rg3.held"u8, ctx => attached)));
        Assert.Contains("parent", parentFault.Message);
        ui.Register(new LayoutDefinition("rg3.held"u8, EmptyRoot));

        PanelFactory panel = new();
        ui.Register(new PanelDefinition("rg3.a"u8, "left"u8, panel.Create));
        ModePanel[] entries = [new ModePanel("left"u8, "rg3.a"u8)];
        ui.Register(new ModeDefinition("rg3.mode"u8, "rg3.main"u8, entries));
        ui.SetMode("rg3.mode"u8);
        context.Update(0f);

        Assert.Equal(1, good.Calls);
        Assert.Same(panel.Last, good.Left!.Children[0]);
    }

    /// <summary>
    /// The slot name rule: two slots with one name in a layout root throw
    /// <see cref="ArgumentException"/> with the parameter <c>layout</c>, also at different depths. The name stays
    /// free, and the root is not in the context.
    /// </summary>
    [Fact]
    public void RegisterOfALayoutRejectsTwoSlotsWithOneName()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });

        ArgumentException fault = Assert.Throws<ArgumentException>(
            () => ui.Register(new LayoutDefinition("rg4.dup"u8, DuplicateSlotRoot)));
        Assert.Equal("layout", fault.ParamName);

        ui.Register(new LayoutDefinition("rg4.dup"u8, EmptyRoot));
        Assert.Equal(0, context.Roots.Length);
    }

    /// <summary>
    /// The mode name rule: a mode that names an unknown layout, an unknown slot, or an unknown panel throws
    /// <see cref="ArgumentException"/> with the parameter <c>mode</c>, and <see cref="UiManager.SetMode"/> of that
    /// mode name throws <see cref="ArgumentException"/> too.
    /// </summary>
    [Fact]
    public void RegisterOfAModeRejectsAnUnknownLayoutPanelOrSlot()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory panel = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("rg5.a"u8, "left"u8, panel.Create));
        ui.Register(new LayoutDefinition("rg5.main"u8, layout.Create));

        ModePanel[] unknownLayout = [new ModePanel("left"u8, "rg5.a"u8)];
        ArgumentException layoutFault = Assert.Throws<ArgumentException>(
            () => ui.Register(new ModeDefinition("rg5.mode"u8, "rg5.gone"u8, unknownLayout)));
        Assert.Equal("mode", layoutFault.ParamName);

        ModePanel[] unknownSlot = [new ModePanel("middle"u8, "rg5.a"u8)];
        ArgumentException slotFault = Assert.Throws<ArgumentException>(
            () => ui.Register(new ModeDefinition("rg5.mode"u8, "rg5.main"u8, unknownSlot)));
        Assert.Equal("mode", slotFault.ParamName);

        ModePanel[] unknownPanel = [new ModePanel("left"u8, "rg5.gone"u8)];
        ArgumentException panelFault = Assert.Throws<ArgumentException>(
            () => ui.Register(new ModeDefinition("rg5.mode"u8, "rg5.main"u8, unknownPanel)));
        Assert.Equal("mode", panelFault.ParamName);

        ArgumentException setFault = Assert.Throws<ArgumentException>(() => ui.SetMode("rg5.mode"u8));
        Assert.Equal("name", setFault.ParamName);
        Assert.Equal(0, panel.Calls);
        Assert.Equal(0, context.Roots.Length);
    }

    /// <summary>
    /// The applying rule: a <see cref="UiManager.SetMode"/> call, a <see cref="ModeHistory.Back"/> call, and a
    /// <see cref="UiManager.Register(PanelDefinition)"/> call from a panel factory inside the diff each throw
    /// <see cref="InvalidOperationException"/>, and the diff ends.
    /// </summary>
    [Fact]
    public void SetModeBackAndRegisterFromTheDiffThrow()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelFactory plain = new();
        DiffProbeFactory probe = new() { Manager = ui };
        ui.Register(new PanelDefinition("rg6.a"u8, "left"u8, plain.Create));
        ui.Register(new PanelDefinition("rg6.b"u8, "left"u8, probe.Create));
        ui.Register(new LayoutDefinition("rg6.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "rg6.a"u8)];
        ModePanel[] entriesB = [new ModePanel("right"u8, "rg6.b"u8)];
        ModeDefinition modeB = new("rg6.b"u8, "rg6.main"u8, entriesB);
        ui.Register(new ModeDefinition("rg6.a"u8, "rg6.main"u8, entriesA));
        ui.Register(modeB);
        ui.SetMode("rg6.a"u8);
        context.Update(0f);

        ui.SetMode("rg6.b"u8);
        context.Update(0f);

        Assert.Equal(1, probe.Calls);
        Assert.Equal(typeof(InvalidOperationException), probe.SetModeFault);
        Assert.Equal(typeof(InvalidOperationException), probe.BackFault);
        Assert.Equal(typeof(InvalidOperationException), probe.RegisterFault);
        Assert.Same(modeB, ui.CurrentMode);
        Assert.Null(ui.TargetMode);
        Assert.Same(probe.Last, layout.Right!.Children[0]);

        ui.Register(new PanelDefinition("rg6.late"u8, "left"u8, plain.Create));
        Assert.Equal(1, probe.Calls);
    }

    /// <summary>
    /// The error rule: an exception of a factory leaves <see cref="UiContext.Update(float)"/> before the input pass.
    /// The current mode stays the old mode, the target stays, and the next update finishes the diff with each slot in
    /// the order of the new mode.
    /// </summary>
    [Fact]
    public void AFactoryExceptionLeavesUpdateAndTheNextUpdateFinishesTheDiff()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelFactory leave = new();
        PanelFactory stay = new();
        FaultyFactory faulty = new();
        ui.Register(new PanelDefinition("rg7.a"u8, "left"u8, leave.Create));
        PanelController first = ui.Register(new PanelDefinition("rg7.b1"u8, "left"u8, stay.Create));
        ui.Register(new PanelDefinition("rg7.b2"u8, "left"u8, faulty.Create));
        ui.Register(new LayoutDefinition("rg7.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "rg7.a"u8)];
        ModePanel[] entriesB = [new ModePanel("left"u8, "rg7.b1"u8), new ModePanel("left"u8, "rg7.b2"u8)];
        ModeDefinition modeA = new("rg7.a"u8, "rg7.main"u8, entriesA);
        ModeDefinition modeB = new("rg7.b"u8, "rg7.main"u8, entriesB);
        ui.Register(modeA);
        ui.Register(modeB);
        ui.SetMode("rg7.a"u8);
        context.Update(0f);

        ui.SetMode("rg7.b"u8);
        NotSupportedException fault = Assert.Throws<NotSupportedException>(() => context.Update(0f));
        Assert.Equal(FaultMessage, fault.Message);

        Assert.Same(modeA, ui.CurrentMode);
        Assert.Same(modeB, ui.TargetMode);
        Assert.Equal(1, faulty.Calls);

        faulty.Fails = false;
        context.Update(0f);

        Assert.Same(modeB, ui.CurrentMode);
        Assert.Null(ui.TargetMode);
        Assert.Equal(2, faulty.Calls);
        SlotContainer left = layout.Left!;
        Assert.Equal(2, left.Children.Count);
        Assert.Same(first.Instance.Root, left.Children[0]);
        Assert.Same(faulty.Last, left.Children[1]);
        Assert.Null(leave.Last!.Parent);
        Assert.Same(stay.Last, first.Instance.Root);
        Assert.Equal(1, stay.Calls);
    }

    /// <summary>
    /// The recovery rule: a diff that fails after it mounts one panel of the new target leaves that panel in its
    /// slot. A new target in the next update leaves no stale panel: the slot of the failed target holds only the
    /// panels of the new mode, and the panel of the failed target has no parent.
    /// </summary>
    [Fact]
    public void ANewTargetAfterAFailedDiffShowsOnlyItsOwnPanels()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelFactory leave = new();
        PanelFactory stale = new();
        PanelFactory fresh = new();
        FaultyFactory faulty = new();
        ui.Register(new PanelDefinition("rg8.a"u8, "left"u8, leave.Create));
        PanelController mounted = ui.Register(new PanelDefinition("rg8.b1"u8, "left"u8, stale.Create));
        ui.Register(new PanelDefinition("rg8.b2"u8, "right"u8, faulty.Create));
        ui.Register(new PanelDefinition("rg8.c1"u8, "right"u8, fresh.Create));
        ui.Register(new LayoutDefinition("rg8.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "rg8.a"u8)];
        ModePanel[] entriesB = [new ModePanel("left"u8, "rg8.b1"u8), new ModePanel("right"u8, "rg8.b2"u8)];
        ModePanel[] entriesC = [new ModePanel("right"u8, "rg8.c1"u8)];
        ModeDefinition modeC = new("rg8.c"u8, "rg8.main"u8, entriesC);
        ui.Register(new ModeDefinition("rg8.a"u8, "rg8.main"u8, entriesA));
        ui.Register(new ModeDefinition("rg8.b"u8, "rg8.main"u8, entriesB));
        ui.Register(modeC);
        ui.SetMode("rg8.a"u8);
        context.Update(0f);

        ui.SetMode("rg8.b"u8);
        Assert.Throws<NotSupportedException>(() => context.Update(0f));
        Assert.Same(mounted.Instance.Root, layout.Left!.Children[0]);

        ui.SetMode("rg8.c"u8);
        context.Update(0f);

        Assert.Same(modeC, ui.CurrentMode);
        Assert.Null(ui.TargetMode);
        Assert.Equal(0, layout.Left!.Children.Count);
        Assert.Equal(1, layout.Right!.Children.Count);
        Assert.Same(fresh.Last, layout.Right.Children[0]);
        Assert.Null(mounted.Instance.Root!.Parent);
        Assert.Null(leave.Last!.Parent);
        Assert.Equal(1, stale.Calls);
        Assert.Equal(1, faulty.Calls);
    }
}
