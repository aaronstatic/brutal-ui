using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the layout swap: the context root of the new layout, the empty slots of the old layout, the reuse
/// of the first layout root at a swap back, the style and the layout of the new layout in the same update, and the
/// roots of the game.
/// </summary>
/// <remarks>
/// Two layouts stand in each test: <c>wide</c> with the slots <c>left</c> and <c>right</c>, and <c>tall</c> with the
/// slot <c>main</c>. Each test builds its own context with a <see cref="RecordingRenderer"/> and a viewport of 400
/// by 300, and its own panel, layout, and mode names in unique UTF-8 literals.
/// </remarks>
public sealed class LayoutSwapTests
{
    /// <summary>The fill color of the type rule in <see cref="TheNewLayoutIsStyledAndLaidOutInTheSameUpdate"/>.</summary>
    private static readonly Rgba8 SlotFill = new(24, 96, 48, 255);

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

    /// <summary>The factory of the <c>wide</c> layout. Each call counts and holds the two slots it builds.</summary>
    private sealed class WideFactory
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

    /// <summary>The factory of the <c>tall</c> layout. Each call counts and holds the one slot it builds.</summary>
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
    /// The swap rule (A4): a mode with another layout puts the root of the new layout into the context, and the
    /// context holds no other manager root.
    /// </summary>
    [Fact]
    public void AModeWithAnotherLayoutSwapsTheContextRoot()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory p = new();
        WideFactory wide = new();
        TallFactory tall = new();
        ui.Register(new PanelDefinition("ls1.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ls1.wide"u8, wide.Create));
        ui.Register(new LayoutDefinition("ls1.tall"u8, tall.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "ls1.p"u8)];
        ModePanel[] entriesB = [new ModePanel("main"u8, "ls1.p"u8)];
        ui.Register(new ModeDefinition("ls1.a"u8, "ls1.wide"u8, entriesA));
        ui.Register(new ModeDefinition("ls1.b"u8, "ls1.tall"u8, entriesB));
        ui.SetMode("ls1.a"u8);
        context.Update(0f);
        Assert.Equal(1, context.Roots.Length);
        Assert.Same(wide.Last, context.Roots[0]);

        ui.SetMode("ls1.b"u8);
        context.Update(0f);

        Assert.Equal(1, context.Roots.Length);
        Assert.Same(tall.Last, context.Roots[0]);
    }

    /// <summary>
    /// The swap rule for the panels: a swap unmounts each panel from the slots of the old layout. A panel that both
    /// modes hold is the same root, now in <c>main</c>.
    /// </summary>
    [Fact]
    public void TheSlotsOfTheOldLayoutHoldNoPanelAfterASwap()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelController pc = ui.Register(new PanelDefinition("ls2.p"u8, "left"u8, new PanelFactory().Create));
        WideFactory wide = new();
        TallFactory tall = new();
        ui.Register(new LayoutDefinition("ls2.wide"u8, wide.Create));
        ui.Register(new LayoutDefinition("ls2.tall"u8, tall.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "ls2.p"u8)];
        ModePanel[] entriesB = [new ModePanel("main"u8, "ls2.p"u8)];
        ui.Register(new ModeDefinition("ls2.a"u8, "ls2.wide"u8, entriesA));
        ui.Register(new ModeDefinition("ls2.b"u8, "ls2.tall"u8, entriesB));
        ui.SetMode("ls2.a"u8);
        context.Update(0f);

        ui.SetMode("ls2.b"u8);
        context.Update(0f);

        Assert.Equal(0, wide.Left!.Children.Count);
        Assert.Equal(0, wide.Right!.Children.Count);
        Assert.Equal(1, tall.Main!.Children.Count);
        Assert.Same(pc.Instance.Root, tall.Main.Children[0]);
    }

    /// <summary>
    /// The reuse rule: a swap back adds the same layout root again. Each layout factory runs one time, the context
    /// holds the first <c>wide</c> root, and the panels are back in their slots.
    /// </summary>
    [Fact]
    public void ASwapBackReusesTheFirstLayoutRoot()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory p = new();
        WideFactory wide = new();
        TallFactory tall = new();
        ui.Register(new PanelDefinition("ls3.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ls3.wide"u8, wide.Create));
        ui.Register(new LayoutDefinition("ls3.tall"u8, tall.Create));
        Element firstWideRoot = wide.Last!;
        ModePanel[] left = [new ModePanel("left"u8, "ls3.p"u8)];
        ModePanel[] main = [new ModePanel("main"u8, "ls3.p"u8)];
        ui.Register(new ModeDefinition("ls3.a"u8, "ls3.wide"u8, left));
        ui.Register(new ModeDefinition("ls3.b"u8, "ls3.tall"u8, main));
        ui.Register(new ModeDefinition("ls3.c"u8, "ls3.wide"u8, left));

        ui.SetMode("ls3.a"u8);
        context.Update(0f);
        ui.SetMode("ls3.b"u8);
        context.Update(0f);
        ui.SetMode("ls3.c"u8);
        context.Update(0f);

        Assert.Equal(1, wide.Calls);
        Assert.Equal(1, tall.Calls);
        Assert.Equal(1, context.Roots.Length);
        Assert.Same(firstWideRoot, context.Roots[0]);
        Assert.Equal(1, wide.Left!.Children.Count);
        Assert.Same(p.Last, wide.Left.Children[0]);
        Assert.Equal(0, tall.Main!.Children.Count);
    }

    /// <summary>
    /// The restyle rule: the update of the change styles and lays out the new layout. A type rule for
    /// <see cref="SlotContainer"/> colors <c>main</c>, and each mounted root has a layout height above 0.
    /// </summary>
    [Fact]
    public void TheNewLayoutIsStyledAndLaidOutInTheSameUpdate()
    {
        UiContext context = NewContext();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForType(ElementType.Intern("SlotContainer")), new StyleProperties { BackgroundColor = SlotFill })
            .Build();
        context.AddSheet(sheet);
        PanelFactory p = new();
        WideFactory wide = new();
        TallFactory tall = new();
        ui.Register(new PanelDefinition("ls4.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ls4.wide"u8, wide.Create));
        ui.Register(new LayoutDefinition("ls4.tall"u8, tall.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "ls4.p"u8)];
        ModePanel[] entriesB = [new ModePanel("main"u8, "ls4.p"u8)];
        ui.Register(new ModeDefinition("ls4.a"u8, "ls4.wide"u8, entriesA));
        ui.Register(new ModeDefinition("ls4.b"u8, "ls4.tall"u8, entriesB));
        ui.SetMode("ls4.a"u8);
        context.Update(0f);

        ui.SetMode("ls4.b"u8);
        context.Update(0f);

        Assert.Equal(SlotFill, tall.Main!.Style.BackgroundColor);
        Assert.Equal(SlotFill, wide.Left!.Style.BackgroundColor);
        Assert.True(p.Last!.Layout.Rect.W > 0f);
    }

    /// <summary>
    /// The root rule: a root that the game added before the manager stays in <see cref="UiContext.Roots"/> after
    /// two swaps, in the first position.
    /// </summary>
    [Fact]
    public void TheManagerKeepsTheRootsOfTheGame()
    {
        UiContext context = NewContext();
        Element gameRoot = new();
        gameRoot.Inline.Width = 50;
        gameRoot.Inline.Height = 50;
        context.AddRoot(gameRoot);
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = NewPanels() });
        PanelFactory p = new();
        WideFactory wide = new();
        TallFactory tall = new();
        ui.Register(new PanelDefinition("ls5.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ls5.wide"u8, wide.Create));
        ui.Register(new LayoutDefinition("ls5.tall"u8, tall.Create));
        ModePanel[] entriesA = [new ModePanel("left"u8, "ls5.p"u8)];
        ModePanel[] entriesB = [new ModePanel("main"u8, "ls5.p"u8)];
        ui.Register(new ModeDefinition("ls5.a"u8, "ls5.wide"u8, entriesA));
        ui.Register(new ModeDefinition("ls5.b"u8, "ls5.tall"u8, entriesB));
        ui.SetMode("ls5.a"u8);
        context.Update(0f);

        ui.SetMode("ls5.b"u8);
        context.Update(0f);
        Assert.Same(gameRoot, context.Roots[0]);

        ui.SetMode("ls5.a"u8);
        context.Update(0f);

        Assert.Equal(2, context.Roots.Length);
        Assert.Same(gameRoot, context.Roots[0]);
        Assert.Same(wide.Last, context.Roots[1]);
    }
}
