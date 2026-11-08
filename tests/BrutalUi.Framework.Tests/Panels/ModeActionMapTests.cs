using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the action map switch: the activate of a mode map, the replace of an old map, the deactivate for a
/// mode with no map, the slot of the game between two maps with no mode map, and the key of the frame of the
/// change.
/// </summary>
/// <remarks>
/// Each map binds the Escape key (256) to a counted command. Each test builds its own context with a
/// <see cref="RecordingRenderer"/> and a viewport of 400 by 300, and its own panel, layout, and mode names in
/// unique UTF-8 literals.
/// </remarks>
public sealed class ModeActionMapTests
{
    /// <summary>The GLFW key code of Escape.</summary>
    private const int EscapeKey = 256;

    /// <summary>A call counter for a command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>The handler of a counted command.</summary>
    /// <param name="target">The <see cref="Counter"/> of the test.</param>
    private static void CountCall(object? target)
    {
        ((Counter)target!).Calls++;
    }

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

    /// <summary>Creates a map that binds Escape to a counted command of one name.</summary>
    /// <param name="registry">The registry that holds the command.</param>
    /// <param name="command">The UTF-8 name of the command.</param>
    /// <param name="counter">The counter that the command raises.</param>
    /// <returns>The map.</returns>
    private static UiActionMap NewEscapeMap(CommandRegistry registry, ReadOnlySpan<byte> command, Counter counter)
    {
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction("esc"u8, command);
        map.BindKey("esc"u8, EscapeKey);
        registry.Register(command, CountCall, counter);
        return map;
    }

    /// <summary>Creates a key down event with no target, no modifier, and no repeat.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent KeyDown(int key)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }

    /// <summary>Pushes one Escape key down.</summary>
    /// <param name="context">The context.</param>
    private static void PushEscape(UiContext context)
    {
        KeyEvent e = KeyDown(EscapeKey);
        context.PushEvent(in e);
    }

    /// <summary>
    /// The activate rule: a mode with a map activates it. <see cref="UiActionMap.IsActive"/> is
    /// <see langword="true"/>, and a pushed Escape runs the command of the map.
    /// </summary>
    [Fact]
    public void AModeWithAMapActivatesIt()
    {
        UiContext context = NewContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        PanelFactory p = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("ma1.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ma1.main"u8, layout.Create));
        Counter counter = new();
        UiActionMap map = NewEscapeMap(panels.Commands, "ma1.cmd"u8, counter);
        ModePanel[] entries = [new ModePanel("left"u8, "ma1.p"u8)];
        ui.Register(new ModeDefinition("ma1.a"u8, "ma1.main"u8, entries, map));
        ui.SetMode("ma1.a"u8);
        context.Update(0f);

        Assert.True(map.IsActive);

        PushEscape(context);
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>
    /// The replace rule: a mode with another map replaces the old map. The old map reads
    /// <see langword="false"/> for <see cref="UiActionMap.IsActive"/>, and Escape runs only the new command.
    /// </summary>
    [Fact]
    public void AModeWithAnotherMapReplacesTheOldMap()
    {
        UiContext context = NewContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        PanelFactory p = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("ma2.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ma2.main"u8, layout.Create));
        Counter counterA = new();
        Counter counterB = new();
        UiActionMap mapA = NewEscapeMap(panels.Commands, "ma2.cmdA"u8, counterA);
        UiActionMap mapB = NewEscapeMap(panels.Commands, "ma2.cmdB"u8, counterB);
        ModePanel[] entries = [new ModePanel("left"u8, "ma2.p"u8)];
        ui.Register(new ModeDefinition("ma2.a"u8, "ma2.main"u8, entries, mapA));
        ui.Register(new ModeDefinition("ma2.b"u8, "ma2.main"u8, entries, mapB));
        ui.SetMode("ma2.a"u8);
        context.Update(0f);

        ui.SetMode("ma2.b"u8);
        context.Update(0f);

        Assert.False(mapA.IsActive);
        Assert.True(mapB.IsActive);

        PushEscape(context);
        context.Update(0f);

        Assert.Equal(0, counterA.Calls);
        Assert.Equal(1, counterB.Calls);
    }

    /// <summary>
    /// The deactivate rule (A6): a mode with no map deactivates the old map.
    /// <see cref="UiContext.OnUnhandledInput"/> is <see langword="null"/> after the change.
    /// </summary>
    [Fact]
    public void AModeWithNoMapDeactivatesTheOldMap()
    {
        UiContext context = NewContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        PanelFactory p = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("ma3.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ma3.main"u8, layout.Create));
        Counter counter = new();
        UiActionMap mapA = NewEscapeMap(panels.Commands, "ma3.cmd"u8, counter);
        ModePanel[] entries = [new ModePanel("left"u8, "ma3.p"u8)];
        ui.Register(new ModeDefinition("ma3.a"u8, "ma3.main"u8, entries, mapA));
        ui.Register(new ModeDefinition("ma3.b"u8, "ma3.main"u8, entries));
        ui.SetMode("ma3.a"u8);
        context.Update(0f);
        Assert.NotNull(context.OnUnhandledInput);

        ui.SetMode("ma3.b"u8);
        context.Update(0f);

        Assert.Null(context.OnUnhandledInput);
        Assert.False(mapA.IsActive);
    }

    /// <summary>
    /// The keep rule: two modes with no map leave <see cref="UiContext.OnUnhandledInput"/> as the game set it.
    /// </summary>
    [Fact]
    public void TwoModesWithNoMapKeepTheSlotOfTheGame()
    {
        UiContext context = NewContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        PanelFactory p = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("ma4.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ma4.main"u8, layout.Create));
        UiUnhandledInputHandler? gameSlot = null;
        gameSlot = (_, _, _, _) => { };
        context.OnUnhandledInput = gameSlot;
        ModePanel[] entries = [new ModePanel("left"u8, "ma4.p"u8)];
        ui.Register(new ModeDefinition("ma4.a"u8, "ma4.main"u8, entries));
        ui.Register(new ModeDefinition("ma4.b"u8, "ma4.main"u8, entries));

        ui.SetMode("ma4.a"u8);
        context.Update(0f);
        Assert.Equal(gameSlot, context.OnUnhandledInput);

        ui.SetMode("ma4.b"u8);
        context.Update(0f);

        Assert.Equal(gameSlot, context.OnUnhandledInput);
    }

    /// <summary>
    /// The order rule: the map switch comes before the input pass. <see cref="UiManager.SetMode"/>, a pushed
    /// Escape, and one update run the command of the new map and not the command of the old map.
    /// </summary>
    [Fact]
    public void AKeyInTheFrameOfTheChangeReachesTheNewMap()
    {
        UiContext context = NewContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        PanelFactory p = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("ma5.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ma5.main"u8, layout.Create));
        Counter counterA = new();
        Counter counterB = new();
        UiActionMap mapA = NewEscapeMap(panels.Commands, "ma5.cmdA"u8, counterA);
        UiActionMap mapB = NewEscapeMap(panels.Commands, "ma5.cmdB"u8, counterB);
        ModePanel[] entries = [new ModePanel("left"u8, "ma5.p"u8)];
        ui.Register(new ModeDefinition("ma5.a"u8, "ma5.main"u8, entries, mapA));
        ui.Register(new ModeDefinition("ma5.b"u8, "ma5.main"u8, entries, mapB));
        ui.SetMode("ma5.a"u8);
        context.Update(0f);

        ui.SetMode("ma5.b"u8);
        PushEscape(context);
        context.Update(0f);

        Assert.Equal(0, counterA.Calls);
        Assert.Equal(1, counterB.Calls);
    }
}
