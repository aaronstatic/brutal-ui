using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the mode diff on one layout with the slots <c>left</c> and <c>right</c>: the leave with the state
/// kept, the enter with the root built one time, the stay with no join, the move, the slot order, the binding of a
/// detached panel, and the deferred apply from a command.
/// </summary>
/// <remarks>
/// Each test builds its own context with a <see cref="RecordingRenderer"/> and a viewport of 400 by 300, and its
/// own panel, layout, and mode names in unique UTF-8 literals. To read the tree between the diff and the passes, a
/// test calls <see cref="UiContext.OnBeforeUpdate"/> directly, then runs the update.
/// </remarks>
public sealed class ModeDiffTests
{
    /// <summary>The count of measured steps in each 0-byte test.</summary>
    private const int Iterations = 100;

    /// <summary>The count of steps that warm up the tree, the arrays, and the JIT before a measure.</summary>
    private const int WarmUpRounds = 2;

    /// <summary>The count that one full cycle of the diff gate adds. The cycle adds 10 slot children, 4 mode hits, and 3 active-map hits.</summary>
    private const int CycleSum = 17;

    /// <summary>The count that one frame with no target adds: the children of the two slots of the shown layout.</summary>
    private const int IdleFrameSum = 3;

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

    /// <summary>The factory of the bound panel: a root with one label bound to the store of the holder.</summary>
    private sealed class BoundFactory
    {
        /// <summary>The store that the label shows.</summary>
        public Store<int> Store = new(new Store<int>.CreateInfo { Initial = 1 });

        /// <summary>The label of the last root, or <see langword="null"/> before the first call.</summary>
        public Label? Label;

        /// <summary>Creates a root of 100 by 50 with one bound label.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Label = new Label(default(Label.CreateInfo)).BindText(Store, static value => value);
            Element root = new();
            root.Inline.Width = 100;
            root.Inline.Height = 50;
            root.Children.Add(Label);
            return root;
        }
    }

    /// <summary>The factory of the click panel: a button that binds the switch command.</summary>
    private sealed class ButtonFactory
    {
        /// <summary>The registry that holds the switch command. Set it before the first call.</summary>
        public CommandRegistry Registry = null!;

        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>Creates a button root of 80 by 24 with the bound command.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new button.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            Button root = new(default(Button.CreateInfo));
            root.Inline.Width = 80;
            root.Inline.Height = 24;
            root.BindCommand(Registry, "md7.to"u8);
            return root;
        }
    }

    /// <summary>The target of the switch command: the manager and the UTF-8 name of the next mode.</summary>
    private sealed class Switcher
    {
        /// <summary>The manager of the test.</summary>
        public UiManager Manager = null!;

        /// <summary>The name of the target mode.</summary>
        public byte[] Name = [];

        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>The switch command: it calls <see cref="UiManager.SetMode"/> for the recorded name.</summary>
    /// <param name="target">The <see cref="Switcher"/> of the test.</param>
    private static void SwitchToTarget(object? target)
    {
        Switcher s = (Switcher)target!;
        s.Calls++;
        s.Manager.SetMode(s.Name);
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

    /// <summary>The factory of the <c>tall</c> layout of the two 0-byte gates. Each call counts and holds the slot it builds.</summary>
    private sealed class TallFactory
    {
        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The root of the last call, or <see langword="null"/> before the first call.</summary>
        public Element? Last;

        /// <summary>The slot <c>main</c> of the root of the last call.</summary>
        public SlotContainer? Main;

        /// <summary>Creates a root of 400 by 300 with the slot <c>main</c>.</summary>
        /// <param name="context">The context of the manager. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            Main = new SlotContainer("main"u8);
            Element root = new();
            root.Inline.Width = 400;
            root.Inline.Height = 300;
            root.IsPickable = false;
            root.Children.Add(Main);
            Last = root;
            return root;
        }
    }

    /// <summary>
    /// The scene of the two 0-byte gates: the layouts <c>wide</c> (<c>left</c>, <c>right</c>) and <c>tall</c>
    /// (<c>main</c>), four panels, two maps with one action each, and three modes. Mode A sits on <c>wide</c>
    /// (<c>left [p1, p2]</c>, <c>right [p3]</c>, map 1), mode B sits on <c>wide</c> (<c>left [p2, p4]</c>,
    /// <c>right [p1]</c>, map 2), and mode C sits on <c>tall</c> (<c>main [p2]</c>, no map).
    /// </summary>
    private sealed class Gate
    {
        /// <summary>The context of the tree.</summary>
        public UiContext Context = null!;

        /// <summary>The manager under test.</summary>
        public UiManager Ui = null!;

        /// <summary>The factory of the <c>wide</c> layout.</summary>
        public LayoutFactory Wide = new();

        /// <summary>The factory of the <c>tall</c> layout.</summary>
        public TallFactory Tall = new();

        /// <summary>The action map of mode A.</summary>
        public UiActionMap Map1 = null!;

        /// <summary>The action map of mode B.</summary>
        public UiActionMap Map2 = null!;

        /// <summary>The definition of mode A.</summary>
        public ModeDefinition A = null!;

        /// <summary>The definition of mode B.</summary>
        public ModeDefinition B = null!;

        /// <summary>The definition of mode C.</summary>
        public ModeDefinition C = null!;
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

    /// <summary>Runs the diff, then the passes of one update.</summary>
    /// <param name="context">The context.</param>
    private static void ApplyAndUpdate(UiContext context)
    {
        context.OnBeforeUpdate!(0f);
        context.Update(0f);
    }

    /// <summary>Creates a left-button pointer event with no target.</summary>
    /// <param name="type"><see cref="UiEventType.PointerDown"/> or <see cref="UiEventType.PointerUp"/>.</param>
    /// <param name="position">The position in viewport pixels.</param>
    /// <returns>The event.</returns>
    private static PointerEvent Pointer(UiEventType type, float2 position)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>Computes the center of an element in viewport pixels. The walk adds the rect of each ancestor.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The center position.</returns>
    private static float2 CenterInView(Element element)
    {
        float x = 0f;
        float y = 0f;
        for (Element? e = element; e is not null; e = e.Parent)
        {
            x += e.Layout.Rect.X;
            y += e.Layout.Rect.Y;
        }

        return new float2(x + (element.Layout.Rect.Z * 0.5f), y + (element.Layout.Rect.W * 0.5f));
    }

    /// <summary>
    /// The leave rule: a panel that only the old mode holds unmounts and keeps its root, its children, its class,
    /// and its inline width. The controller keeps the same root instance.
    /// </summary>
    [Fact]
    public void APanelThatOnlyTheOldModeHoldsUnmountsAndKeepsItsState()
    {
        ClassId probe = ClassName.Intern(nameof(APanelThatOnlyTheOldModeHoldsUnmountsAndKeepsItsState) + ".probe");
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelController leaving = ui.Register(new PanelDefinition("md1.out"u8, "left"u8, ctx =>
        {
            Element root = new();
            root.Inline.Width = 100;
            root.Inline.Height = 50;
            root.Classes.Add(probe);
            root.Children.Add(new Element());
            return root;
        }));
        PanelFactory stay = new();
        ui.Register(new PanelDefinition("md1.stay"u8, "left"u8, stay.Create));
        ui.Register(new LayoutDefinition("md1.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "md1.out"u8), new ModePanel("left"u8, "md1.stay"u8)];
        ModePanel[] entriesB = [new ModePanel("left"u8, "md1.stay"u8)];
        ui.Register(new ModeDefinition("md1.a"u8, "md1.main"u8, entriesA));
        ui.Register(new ModeDefinition("md1.b"u8, "md1.main"u8, entriesB));
        ui.SetMode("md1.a"u8);
        context.Update(0f);

        Element root = leaving.Instance.Root!;
        ui.SetMode("md1.b"u8);
        context.OnBeforeUpdate!(0f);

        Assert.Null(root.Parent);
        Assert.Equal(1, root.Children.Count);
        Assert.True(root.Classes.Contains(probe));
        Assert.Equal(100f, root.Inline.Width);
        Assert.Same(root, leaving.Instance.Root);
        Assert.Equal(1, layout.Left!.Children.Count);
        Assert.Same(stay.Last, layout.Left.Children[0]);
    }

    /// <summary>
    /// The enter rule (Q3): a panel that only the new mode holds mounts, and its controller builds the root one
    /// time. A, B, A, B give one factory call and the same root at each mount.
    /// </summary>
    [Fact]
    public void APanelThatOnlyTheNewModeHoldsMountsAndItsRootIsBuiltOneTime()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelFactory plain = new();
        PanelFactory once = new();
        PanelController controller = ui.Register(new PanelDefinition("md2.enter"u8, "left"u8, once.Create));
        ui.Register(new PanelDefinition("md2.plain"u8, "left"u8, plain.Create));
        ui.Register(new LayoutDefinition("md2.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "md2.plain"u8)];
        ModePanel[] entriesB = [new ModePanel("left"u8, "md2.plain"u8), new ModePanel("right"u8, "md2.enter"u8)];
        ui.Register(new ModeDefinition("md2.a"u8, "md2.main"u8, entriesA));
        ui.Register(new ModeDefinition("md2.b"u8, "md2.main"u8, entriesB));

        ui.SetMode("md2.a"u8);
        context.Update(0f);
        Assert.Equal(0, once.Calls);

        ui.SetMode("md2.b"u8);
        context.Update(0f);
        Element first = controller.Instance.Root!;
        Assert.Equal(1, once.Calls);
        Assert.Same(first, layout.Right!.Children[0]);

        ui.SetMode("md2.a"u8);
        context.Update(0f);
        Assert.Equal(1, once.Calls);
        Assert.Null(first.Parent);

        ui.SetMode("md2.b"u8);
        context.Update(0f);
        Assert.Equal(1, once.Calls);
        Assert.Same(first, layout.Right!.Children[0]);
        Assert.Same(first, controller.Instance.Root);
    }

    /// <summary>
    /// The stay rule (A5): a panel that both modes hold in the same slot stays with no join. After the diff, the
    /// staying root keeps its parent and holds no <see cref="DirtyFlags.Style"/> bit. A root that enters holds the
    /// bit.
    /// </summary>
    [Fact]
    public void APanelInBothModesInTheSameSlotStaysWithNoJoin()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelFactory stay = new();
        PanelFactory leave = new();
        PanelFactory enter = new();
        ui.Register(new PanelDefinition("md3.stay"u8, "left"u8, stay.Create));
        ui.Register(new PanelDefinition("md3.out"u8, "right"u8, leave.Create));
        ui.Register(new PanelDefinition("md3.in"u8, "right"u8, enter.Create));
        ui.Register(new LayoutDefinition("md3.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "md3.stay"u8), new ModePanel("right"u8, "md3.out"u8)];
        ModePanel[] entriesB = [new ModePanel("left"u8, "md3.stay"u8), new ModePanel("right"u8, "md3.in"u8)];
        ui.Register(new ModeDefinition("md3.a"u8, "md3.main"u8, entriesA));
        ui.Register(new ModeDefinition("md3.b"u8, "md3.main"u8, entriesB));
        ui.SetMode("md3.a"u8);
        context.Update(0f);

        ui.SetMode("md3.b"u8);
        context.OnBeforeUpdate!(0f);

        Element stayRoot = stay.Last!;
        Assert.Same(layout.Left, stayRoot.Parent);
        Assert.False((stayRoot.Dirty & DirtyFlags.Style) != 0);
        Assert.True((enter.Last!.Dirty & DirtyFlags.Style) != 0);
        Assert.Null(leave.Last!.Parent);
    }

    /// <summary>
    /// The move rule: a panel that moves to another slot leaves and enters. After the diff, <c>left</c> holds no
    /// root, <c>right</c> holds the root, and the root carries the <see cref="DirtyFlags.Style"/> bit of the join.
    /// </summary>
    [Fact]
    public void APanelThatMovesToAnotherSlotLeavesAndEnters()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelFactory mover = new();
        ui.Register(new PanelDefinition("md4.move"u8, "left"u8, mover.Create));
        ui.Register(new LayoutDefinition("md4.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "md4.move"u8)];
        ModePanel[] entriesB = [new ModePanel("right"u8, "md4.move"u8)];
        ui.Register(new ModeDefinition("md4.a"u8, "md4.main"u8, entriesA));
        ui.Register(new ModeDefinition("md4.b"u8, "md4.main"u8, entriesB));
        ui.SetMode("md4.a"u8);
        context.Update(0f);

        ui.SetMode("md4.b"u8);
        context.OnBeforeUpdate!(0f);

        Element root = mover.Last!;
        Assert.False(layout.Left!.Children.Contains(root));
        Assert.True(layout.Right!.Children.Contains(root));
        Assert.Same(layout.Right, root.Parent);
        Assert.True((root.Dirty & DirtyFlags.Style) != 0);
    }

    /// <summary>
    /// The order rule (A4): each slot takes the order of the new mode. A change from <c>left [x, y, z]</c> to
    /// <c>left [z, w, x]</c> gives <c>z</c>, <c>w</c>, <c>x</c>, and <c>y</c> has no parent.
    /// </summary>
    [Fact]
    public void EachSlotTakesTheOrderOfTheNewMode()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        LayoutFactory layout = new();
        PanelFactory fx = new();
        PanelFactory fy = new();
        PanelFactory fz = new();
        PanelFactory fw = new();
        ui.Register(new PanelDefinition("md5.x"u8, "left"u8, fx.Create));
        ui.Register(new PanelDefinition("md5.y"u8, "left"u8, fy.Create));
        ui.Register(new PanelDefinition("md5.z"u8, "left"u8, fz.Create));
        ui.Register(new PanelDefinition("md5.w"u8, "left"u8, fw.Create));
        ui.Register(new LayoutDefinition("md5.main"u8, layout.Create));
        ModePanel[] entriesA =
        [
            new ModePanel("left"u8, "md5.x"u8),
            new ModePanel("left"u8, "md5.y"u8),
            new ModePanel("left"u8, "md5.z"u8),
        ];
        ModePanel[] entriesB =
        [
            new ModePanel("left"u8, "md5.z"u8),
            new ModePanel("left"u8, "md5.w"u8),
            new ModePanel("left"u8, "md5.x"u8),
        ];
        ui.Register(new ModeDefinition("md5.a"u8, "md5.main"u8, entriesA));
        ui.Register(new ModeDefinition("md5.b"u8, "md5.main"u8, entriesB));
        ui.SetMode("md5.a"u8);
        context.Update(0f);

        ui.SetMode("md5.b"u8);
        context.OnBeforeUpdate!(0f);

        SlotContainer left = layout.Left!;
        Assert.Equal(3, left.Children.Count);
        Assert.Same(fz.Last, left.Children[0]);
        Assert.Same(fw.Last, left.Children[1]);
        Assert.Same(fx.Last, left.Children[2]);
        Assert.Null(fy.Last!.Parent);
    }

    /// <summary>
    /// The binding rule: an unmounted panel keeps its binding and shows the new value when it returns. The A, B,
    /// set, A path gives the text <c>2</c> and a binding count of 1.
    /// </summary>
    [Fact]
    public void AnUnmountedPanelKeepsItsBindingAndShowsTheNewValueWhenItReturns()
    {
        UiContext context = NewContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        LayoutFactory layout = new();
        BoundFactory bound = new();
        PanelFactory plain = new();
        ui.Register(new PanelDefinition("md6.bound"u8, "left"u8, bound.Create));
        ui.Register(new PanelDefinition("md6.plain"u8, "left"u8, plain.Create));
        ui.Register(new LayoutDefinition("md6.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "md6.bound"u8)];
        ModePanel[] entriesB = [new ModePanel("left"u8, "md6.plain"u8)];
        ui.Register(new ModeDefinition("md6.a"u8, "md6.main"u8, entriesA));
        ui.Register(new ModeDefinition("md6.b"u8, "md6.main"u8, entriesB));

        ui.SetMode("md6.a"u8);
        context.Update(0f);
        Label label = bound.Label!;
        Assert.Equal("1"u8.ToArray(), label.Text.ToArray());

        ui.SetMode("md6.b"u8);
        context.Update(0f);
        Element panelRoot = label.Parent!;
        Assert.Null(panelRoot.Parent);
        bound.Store.Set(2);
        context.Update(0f);

        ui.SetMode("md6.a"u8);
        context.Update(0f);

        Assert.Equal("2"u8.ToArray(), label.Text.ToArray());
        Assert.Equal(1, label.BindingCount);
    }

    /// <summary>
    /// The defer rule with a command (Q1): a command handler calls <see cref="UiManager.SetMode"/> in the input
    /// pass. The update of the click keeps the old mode with the new mode as the target. The next update applies
    /// the new mode.
    /// </summary>
    [Fact]
    public void ASetModeFromACommandAppliesAtTheStartOfTheNextUpdate()
    {
        UiContext context = NewContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        LayoutFactory layout = new();
        ButtonFactory button = new();
        button.Registry = panels.Commands;
        PanelFactory plain = new();
        ui.Register(new PanelDefinition("md7.click"u8, "left"u8, button.Create));
        ui.Register(new PanelDefinition("md7.plain"u8, "left"u8, plain.Create));
        ui.Register(new LayoutDefinition("md7.main"u8, layout.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "md7.click"u8)];
        ModePanel[] entriesB = [new ModePanel("left"u8, "md7.plain"u8)];
        ModeDefinition modeB = new("md7.b"u8, "md7.main"u8, entriesB);
        ui.Register(new ModeDefinition("md7.a"u8, "md7.main"u8, entriesA));
        ui.Register(modeB);
        Switcher switcher = new() { Manager = ui, Name = "md7.b"u8.ToArray() };
        panels.Commands.Register("md7.to"u8, SwitchToTarget, switcher);
        ui.SetMode("md7.a"u8);
        context.Update(0f);

        float2 center = CenterInView(layout.Left!.Children[0]);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(1, switcher.Calls);
        Assert.Equal("md7.a"u8.ToArray(), ui.CurrentMode!.Name.ToArray());
        Assert.Same(modeB, ui.TargetMode);

        context.Update(0f);

        Assert.Same(modeB, ui.CurrentMode);
        Assert.Null(ui.TargetMode);
        Assert.Same(plain.Last, layout.Left!.Children[0]);
    }

    /// <summary>Builds the gate scene: two layouts, four panels, two maps, three modes, and mode A applied.</summary>
    /// <returns>The gate, ready for a measure.</returns>
    private static Gate NewGate()
    {
        Gate gate = new();
        gate.Context = NewContext();
        PanelContext panels = NewPanels();
        gate.Ui = new UiManager(new UiManager.CreateInfo { Context = gate.Context, Panels = panels });
        gate.Map1 = new UiActionMap(new UiActionMap.CreateInfo { Commands = panels.Commands });
        gate.Map1.AddAction("md8.back"u8, "md8.cmd1"u8);
        gate.Map1.BindKey("md8.back"u8, 256);
        gate.Map2 = new UiActionMap(new UiActionMap.CreateInfo { Commands = panels.Commands });
        gate.Map2.AddAction("md8.close"u8, "md8.cmd2"u8);
        gate.Map2.BindKey("md8.close"u8, 257);
        PanelFactory p1 = new();
        PanelFactory p2 = new();
        PanelFactory p3 = new();
        PanelFactory p4 = new();
        gate.Ui.Register(new PanelDefinition("md8.p1"u8, "left"u8, p1.Create));
        gate.Ui.Register(new PanelDefinition("md8.p2"u8, "left"u8, p2.Create));
        gate.Ui.Register(new PanelDefinition("md8.p3"u8, "left"u8, p3.Create));
        gate.Ui.Register(new PanelDefinition("md8.p4"u8, "left"u8, p4.Create));
        gate.Ui.Register(new LayoutDefinition("md8.wide"u8, gate.Wide.Create));
        gate.Ui.Register(new LayoutDefinition("md8.tall"u8, gate.Tall.Create));
        ModePanel[] entriesA =
        [
            new ModePanel("left"u8, "md8.p1"u8),
            new ModePanel("left"u8, "md8.p2"u8),
            new ModePanel("right"u8, "md8.p3"u8),
        ];
        ModePanel[] entriesB =
        [
            new ModePanel("left"u8, "md8.p2"u8),
            new ModePanel("left"u8, "md8.p4"u8),
            new ModePanel("right"u8, "md8.p1"u8),
        ];
        ModePanel[] entriesC = [new ModePanel("main"u8, "md8.p2"u8)];
        gate.A = new ModeDefinition("md8.a"u8, "md8.wide"u8, entriesA, gate.Map1);
        gate.B = new ModeDefinition("md8.b"u8, "md8.wide"u8, entriesB, gate.Map2);
        gate.C = new ModeDefinition("md8.c"u8, "md8.tall"u8, entriesC);
        gate.Ui.Register(gate.A);
        gate.Ui.Register(gate.B);
        gate.Ui.Register(gate.C);
        gate.Ui.SetMode("md8.a"u8);
        gate.Context.Update(0f);
        return gate;
    }

    /// <summary>
    /// The measured body of the diff gate: <c>SetMode(B)</c>, one update, <c>SetMode(C)</c>, one update, a back step
    /// to B, one update, <c>SetMode(A)</c>, and one update. One cycle holds a leave, an enter, a stay with a new
    /// index, a move, a layout swap, a map switch, a deactivate, and a back step. The body adds the child count of
    /// each slot, one for each current-mode hit, and one for each active map. It uses no lambda, no string, no array,
    /// and no <c>Assert</c>.
    /// </summary>
    /// <param name="gate">The gate scene.</param>
    /// <returns>The count of the work that the cycle did.</returns>
    private static int RunCycle(Gate gate)
    {
        int sum = 0;

        gate.Ui.SetMode("md8.b"u8);
        gate.Context.Update(0f);
        sum += gate.Wide.Left!.Children.Count + gate.Wide.Right!.Children.Count + gate.Tall.Main!.Children.Count;
        if (ReferenceEquals(gate.Ui.CurrentMode, gate.B))
        {
            sum++;
        }

        if (gate.Map1.IsActive)
        {
            sum++;
        }

        if (gate.Map2.IsActive)
        {
            sum++;
        }

        gate.Ui.SetMode("md8.c"u8);
        gate.Context.Update(0f);
        sum += gate.Wide.Left!.Children.Count + gate.Wide.Right!.Children.Count + gate.Tall.Main!.Children.Count;
        if (ReferenceEquals(gate.Ui.CurrentMode, gate.C))
        {
            sum++;
        }

        if (gate.Map1.IsActive)
        {
            sum++;
        }

        if (gate.Map2.IsActive)
        {
            sum++;
        }

        gate.Ui.History.Back();
        gate.Context.Update(0f);
        sum += gate.Wide.Left!.Children.Count + gate.Wide.Right!.Children.Count + gate.Tall.Main!.Children.Count;
        if (ReferenceEquals(gate.Ui.CurrentMode, gate.B))
        {
            sum++;
        }

        if (gate.Map1.IsActive)
        {
            sum++;
        }

        if (gate.Map2.IsActive)
        {
            sum++;
        }

        gate.Ui.SetMode("md8.a"u8);
        gate.Context.Update(0f);
        sum += gate.Wide.Left!.Children.Count + gate.Wide.Right!.Children.Count + gate.Tall.Main!.Children.Count;
        if (ReferenceEquals(gate.Ui.CurrentMode, gate.A))
        {
            sum++;
        }

        if (gate.Map1.IsActive)
        {
            sum++;
        }

        if (gate.Map2.IsActive)
        {
            sum++;
        }

        return sum;
    }

    /// <summary>The measured body of the idle gate: one update with no target, plus the child count of each slot.</summary>
    /// <param name="gate">The gate scene, with mode A applied and no target.</param>
    /// <returns>The count of the children in the three slots.</returns>
    private static int RunIdleFrame(Gate gate)
    {
        gate.Context.Update(0f);
        return gate.Wide.Left!.Children.Count + gate.Wide.Right!.Children.Count + gate.Tall.Main!.Children.Count;
    }

    /// <summary>Runs one body a number of times and adds the results.</summary>
    /// <param name="gate">The gate scene.</param>
    /// <param name="count">The count of runs.</param>
    /// <param name="cycle"><see langword="true"/> runs the diff cycle; <see langword="false"/> runs one idle frame.</param>
    /// <returns>The sum of the counts.</returns>
    private static int RunBody(Gate gate, int count, bool cycle)
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += cycle ? RunCycle(gate) : RunIdleFrame(gate);
        }

        return sum;
    }

    /// <summary>
    /// The 0-byte rule of the diff (Decision 10): <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/>, the diff, and
    /// the back step allocate nothing after warm-up. The history ring fills during the measure, and a full ring
    /// allocates nothing.
    /// </summary>
    [Fact]
    public void SetModeAndTheDiffAllocateNothingAfterWarmUp()
    {
        Gate gate = NewGate();

        int warm = RunBody(gate, WarmUpRounds, cycle: true);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = RunBody(gate, Iterations, cycle: true);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // The body does real work: each cycle adds 10 slot children, 4 current-mode hits, and 3 active-map hits.
        Assert.Equal(CycleSum * WarmUpRounds, warm);
        Assert.Equal(CycleSum * Iterations, sum);
        Assert.Same(gate.A, gate.Ui.CurrentMode);
        Assert.Equal(UiManager.DefaultHistoryCapacity, gate.Ui.History.Count);
        Assert.True(gate.Map1.IsActive);
        Assert.False(gate.Map2.IsActive);
    }

    /// <summary>
    /// The 0-byte rule of a frame with no target: <see cref="UiContext.Update(float)"/> allocates nothing and changes
    /// no tree.
    /// </summary>
    [Fact]
    public void AFrameWithNoTargetAllocatesNothing()
    {
        Gate gate = NewGate();

        int warm = RunBody(gate, WarmUpRounds, cycle: false);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = RunBody(gate, Iterations, cycle: false);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // Each idle frame adds the children of the two slots of the shown layout: 2 and 1.
        Assert.Equal(IdleFrameSum * WarmUpRounds, warm);
        Assert.Equal(IdleFrameSum * Iterations, sum);
        Assert.Equal(2, gate.Wide.Left!.Children.Count);
        Assert.Equal(1, gate.Wide.Right!.Children.Count);
        Assert.Equal(0, gate.Tall.Main!.Children.Count);
    }
}
