using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="UiManager"/> for the first mode: the slot write, the defer rule, the layout add, the
/// mount order of the mode, the last target of one frame, the frame with no target, and the unknown name.
/// </summary>
/// <remarks>
/// Each test builds its own context with a <see cref="RecordingRenderer"/> and a viewport of 400 by 300, and its
/// own panel, layout, and mode names in unique UTF-8 literals.
/// </remarks>
public sealed class UiManagerTests
{
    /// <summary>The factory of a panel. Each call counts and records the root it creates.</summary>
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

    /// <summary>The factory of a layout. Each call counts and holds the two slots it builds.</summary>
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

    /// <summary>Creates a panel definition for a factory.</summary>
    /// <param name="name">The unique UTF-8 name of the panel.</param>
    /// <param name="factory">The factory of the test.</param>
    /// <returns>The definition.</returns>
    private static PanelDefinition NewPanel(ReadOnlySpan<byte> name, PanelFactory factory)
    {
        return new PanelDefinition(name, "left"u8, factory.Create);
    }

    /// <summary>Creates a layout definition for a factory.</summary>
    /// <param name="name">The unique UTF-8 name of the layout.</param>
    /// <param name="factory">The factory of the test.</param>
    /// <returns>The definition.</returns>
    private static LayoutDefinition NewLayout(ReadOnlySpan<byte> name, LayoutFactory factory)
    {
        return new LayoutDefinition(name, factory.Create);
    }

    /// <summary>
    /// The slot write rule: the constructor writes <see cref="UiContext.OnBeforeUpdate"/>. A create-info with a
    /// <see langword="null"/> <see cref="UiManager.CreateInfo.Context"/> or <see cref="UiManager.CreateInfo.Panels"/>
    /// throws <see cref="ArgumentNullException"/> with the parameter <c>info</c>.
    /// </summary>
    [Fact]
    public void TheConstructorWritesTheBeforeUpdateSlot()
    {
        UiContext context = NewContext();
        Assert.Null(context.OnBeforeUpdate);
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });

        Assert.NotNull(context.OnBeforeUpdate);
        Assert.Same(ui.Context, context);

        ArgumentNullException e1 = Assert.Throws<ArgumentNullException>(
            () => new UiManager(new UiManager.CreateInfo { Context = null!, Panels = NewPanels() }));
        Assert.Equal("info", e1.ParamName);

        ArgumentNullException e2 = Assert.Throws<ArgumentNullException>(
            () => new UiManager(new UiManager.CreateInfo { Context = context, Panels = null! }));
        Assert.Equal("info", e2.ParamName);
    }

    /// <summary>
    /// The defer rule: <see cref="UiManager.SetMode"/> records the target and changes no tree. No panel factory
    /// runs, <see cref="UiManager.CurrentMode"/> stays <see langword="null"/>, and the context holds no root.
    /// </summary>
    [Fact]
    public void SetModeRecordsTheTargetAndChangesNoTree()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory a = new();
        LayoutFactory layout = new();
        PanelDefinition panel = NewPanel("t2.a"u8, a);
        LayoutDefinition main = NewLayout("t2.main"u8, layout);
        ModePanel[] entries = [new ModePanel("left"u8, "t2.a"u8)];
        ModeDefinition mode = new("t2.mode"u8, "t2.main"u8, entries);
        ui.Register(panel);
        ui.Register(main);
        ui.Register(mode);

        ui.SetMode("t2.mode"u8);

        Assert.Same(mode, ui.TargetMode);
        Assert.Null(ui.CurrentMode);
        Assert.Equal(0, context.Roots.Length);
        Assert.Equal(0, a.Calls);
        Assert.Equal(1, layout.Calls);
    }

    /// <summary>
    /// The next update adds the layout root and mounts each panel in the order of the mode: the context holds only
    /// the layout root, the slot <c>left</c> holds <c>a</c> and <c>b</c> in order, the slot <c>right</c> holds
    /// <c>c</c>, each root is the root of its controller, each root has a layout, and the manager ends with the
    /// mode current and no target.
    /// </summary>
    [Fact]
    public void TheNextUpdateAddsTheLayoutRootAndMountsEachPanelInTheOrderOfTheMode()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory fa = new();
        PanelFactory fb = new();
        PanelFactory fc = new();
        LayoutFactory layout = new();
        PanelController ca = ui.Register(NewPanel("t3.a"u8, fa));
        PanelController cb = ui.Register(NewPanel("t3.b"u8, fb));
        PanelController cc = ui.Register(NewPanel("t3.c"u8, fc));
        LayoutDefinition main = NewLayout("t3.main"u8, layout);
        ui.Register(main);
        ModePanel[] entries =
        [
            new ModePanel("left"u8, "t3.a"u8),
            new ModePanel("right"u8, "t3.c"u8),
            new ModePanel("left"u8, "t3.b"u8),
        ];
        ModeDefinition mode = new("t3.mode"u8, "t3.main"u8, entries);
        ui.Register(mode);
        ui.SetMode("t3.mode"u8);

        context.Update(0f);

        Assert.Equal(1, context.Roots.Length);
        Assert.Same(layout.Last, context.Roots[0]);
        SlotContainer left = layout.Left!;
        SlotContainer right = layout.Right!;
        Assert.Equal(2, left.Children.Count);
        Assert.Same(ca.Instance.Root, left.Children[0]);
        Assert.Same(cb.Instance.Root, left.Children[1]);
        Assert.Equal(1, right.Children.Count);
        Assert.Same(cc.Instance.Root, right.Children[0]);
        Assert.Same(fa.Last, ca.Instance.Root);
        Assert.Same(fb.Last, cb.Instance.Root);
        Assert.Same(fc.Last, cc.Instance.Root);
        Assert.True(ca.Instance.Root!.Layout.Rect.W > 0f);
        Assert.True(cb.Instance.Root!.Layout.Rect.W > 0f);
        Assert.True(cc.Instance.Root!.Layout.Rect.W > 0f);
        Assert.Same(mode, ui.CurrentMode);
        Assert.Null(ui.TargetMode);
    }

    /// <summary>
    /// Two <see cref="UiManager.SetMode"/> calls in one frame apply the last: the update shows the second mode and
    /// the factory of a panel that only the first mode holds never runs.
    /// </summary>
    [Fact]
    public void TwoSetModeCallsInOneFrameApplyTheLast()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory fa = new();
        PanelFactory fb = new();
        PanelFactory fc = new();
        LayoutFactory layout = new();
        PanelController ca = ui.Register(NewPanel("t4.a"u8, fa));
        ui.Register(NewPanel("t4.b"u8, fb));
        PanelController cc = ui.Register(NewPanel("t4.c"u8, fc));
        ui.Register(NewLayout("t4.main"u8, layout));
        ModePanel[] firstEntries =
        [
            new ModePanel("left"u8, "t4.a"u8),
            new ModePanel("right"u8, "t4.b"u8),
        ];
        ModePanel[] secondEntries =
        [
            new ModePanel("left"u8, "t4.a"u8),
            new ModePanel("right"u8, "t4.c"u8),
        ];
        ModeDefinition first = new("t4.first"u8, "t4.main"u8, firstEntries);
        ModeDefinition second = new("t4.second"u8, "t4.main"u8, secondEntries);
        ui.Register(first);
        ui.Register(second);

        ui.SetMode("t4.first"u8);
        ui.SetMode("t4.second"u8);
        context.Update(0f);

        Assert.Same(second, ui.CurrentMode);
        Assert.Null(ui.TargetMode);
        Assert.Equal(0, fb.Calls);
        Assert.Equal(1, fa.Calls);
        Assert.Equal(1, fc.Calls);
        Assert.Same(ca.Instance.Root, layout.Left!.Children[0]);
        Assert.Same(cc.Instance.Root, layout.Right!.Children[0]);
    }

    /// <summary>
    /// A frame with no target changes nothing: the second update keeps the roots, the slot children, the current
    /// mode, and each factory count at 1.
    /// </summary>
    [Fact]
    public void AFrameWithNoTargetChangesNothing()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory fa = new();
        PanelFactory fb = new();
        LayoutFactory layout = new();
        PanelController ca = ui.Register(NewPanel("t5.a"u8, fa));
        PanelController cb = ui.Register(NewPanel("t5.b"u8, fb));
        ui.Register(NewLayout("t5.main"u8, layout));
        ModePanel[] entries =
        [
            new ModePanel("left"u8, "t5.a"u8),
            new ModePanel("right"u8, "t5.b"u8),
        ];
        ModeDefinition mode = new("t5.mode"u8, "t5.main"u8, entries);
        ui.Register(mode);
        ui.SetMode("t5.mode"u8);
        context.Update(0f);

        context.Update(0f);

        Assert.Equal(1, context.Roots.Length);
        Assert.Same(layout.Last, context.Roots[0]);
        Assert.Equal(1, layout.Left!.Children.Count);
        Assert.Same(ca.Instance.Root, layout.Left.Children[0]);
        Assert.Equal(1, layout.Right!.Children.Count);
        Assert.Same(cb.Instance.Root, layout.Right.Children[0]);
        Assert.Same(mode, ui.CurrentMode);
        Assert.Null(ui.TargetMode);
        Assert.Equal(1, fa.Calls);
        Assert.Equal(1, fb.Calls);
    }

    /// <summary>
    /// The unknown name rule: <see cref="UiManager.SetMode"/> throws <see cref="ArgumentException"/> with the
    /// parameter <c>name</c> for a missing name and for an empty name. A target that an earlier call set stays.
    /// </summary>
    [Fact]
    public void SetModeRejectsAnUnknownName()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory fa = new();
        LayoutFactory layout = new();
        ui.Register(NewPanel("t6.a"u8, fa));
        ui.Register(NewLayout("t6.main"u8, layout));
        ModePanel[] entries = [new ModePanel("left"u8, "t6.a"u8)];
        ModeDefinition mode = new("t6.mode"u8, "t6.main"u8, entries);
        ui.Register(mode);
        ui.SetMode("t6.mode"u8);

        ArgumentException e1 = Assert.Throws<ArgumentException>(() => ui.SetMode("missing"u8));
        Assert.Equal("name", e1.ParamName);

        ArgumentException e2 = Assert.Throws<ArgumentException>(() => ui.SetMode(ReadOnlySpan<byte>.Empty));
        Assert.Equal("name", e2.ParamName);

        Assert.Same(mode, ui.TargetMode);
        Assert.Null(ui.CurrentMode);
        Assert.Equal(0, context.Roots.Length);
        Assert.Equal(0, fa.Calls);
    }
}
