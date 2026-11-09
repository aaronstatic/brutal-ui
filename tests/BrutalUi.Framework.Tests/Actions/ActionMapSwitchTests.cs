using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the switch rule of <see cref="UiActionMap"/>: the release of <see cref="UiActionMap.Deactivate"/>, the
/// reset of a fresh <see cref="UiActionMap.Activate"/>, the reads of a map that is not active, and the values across a
/// mode switch.
/// </summary>
/// <remarks>
/// <para>
/// The rule (Decision 11): <see cref="UiActionMap.Deactivate"/> releases each action. A fresh
/// <see cref="UiActionMap.Activate"/> starts with no value. A map that another map replaced reads
/// <see langword="false"/> and zero. The same map in two modes keeps its values. A Button needs a new press in the
/// next map (the latch), and an Axis or a Vector2 reads a held input in the next map at once (Decision 12).
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300, and pushes
/// hand-built key events. GLFW codes: Space 32, W 87, A 65, S 83, D 68. The two manager tests build a real
/// <see cref="UiManager"/> with two modes and two maps, in the style of <c>ModeActionMapTests</c>. Each float assert
/// compares one component with a precision of five decimals.
/// </para>
/// </remarks>
public sealed class ActionMapSwitchTests
{
    /// <summary>The GLFW key code of Space.</summary>
    private const int SpaceKey = 32;

    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the D key.</summary>
    private const int DKey = 68;

    /// <summary>The name of the Button action that the tests fire with.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action of the next map.</summary>
    private static ReadOnlySpan<byte> JumpAction => "Jump"u8;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>
    /// The release rule (A6): <see cref="UiActionMap.Deactivate"/> releases each held action. A Button reads no held
    /// flag and no released flag, a Vector2 reads (0, 0), and more <see cref="UiContext.Update(float)"/> calls keep
    /// that state.
    /// </summary>
    [Fact]
    public void DeactivateReleasesEachHeldAction()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKey(FireAction, SpaceKey);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.Activate(context);

        context.PushEvent(Key(SpaceKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.IsHeld(FireAction));
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);

        map.Deactivate();
        Assert.False(map.IsHeld(FireAction));
        Assert.False(map.WasReleased(FireAction));
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);

        context.Update(0f);
        context.Update(0f);
        Assert.False(map.IsHeld(FireAction));
        Assert.False(map.WasReleased(FireAction));
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);
    }

    /// <summary>
    /// The fresh-map rule: a new map that is activated while Space is held sees no stale flag. It reads no held flag
    /// and no pressed flag, a release gives no released flag, and a new press gives a press.
    /// </summary>
    [Fact]
    public void ANewMapMidHoldSeesNoStaleFlag()
    {
        UiContext context = CreateContext();
        UiActionMap mapA = CreateMap();
        mapA.AddAction(FireAction, UiActionType.Button);
        mapA.BindKey(FireAction, SpaceKey);
        UiActionMap mapB = CreateMap();
        mapB.AddAction(JumpAction, UiActionType.Button);
        mapB.BindKey(JumpAction, SpaceKey);
        mapA.Activate(context);

        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(mapA.IsHeld(FireAction));

        mapA.Deactivate();
        mapB.Activate(context);
        context.Update(0f);
        Assert.False(mapB.IsHeld(JumpAction));
        Assert.False(mapB.WasPressed(JumpAction));

        context.PushEvent(KeyUp(SpaceKey));
        context.Update(0f);
        Assert.False(mapB.WasReleased(JumpAction));

        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(mapB.WasPressed(JumpAction));
    }

    /// <summary>
    /// The manager rule: a mode switch through <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/> with Space held
    /// gives no stale flag. The old map reads no held flag, the new map reads no held flag, and a release in the new
    /// map gives no released flag.
    /// </summary>
    [Fact]
    public void AModeSwitchThroughTheManagerGivesNoStaleFlag()
    {
        UiContext context = CreateContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        PanelFactory p = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("ms3.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ms3.main"u8, layout.Create));
        UiActionMap mapA = CreateMap();
        mapA.AddAction(FireAction, UiActionType.Button);
        mapA.BindKey(FireAction, SpaceKey);
        UiActionMap mapB = CreateMap();
        mapB.AddAction(JumpAction, UiActionType.Button);
        mapB.BindKey(JumpAction, SpaceKey);
        ModePanel[] entries = [new ModePanel("left"u8, "ms3.p"u8)];
        ui.Register(new ModeDefinition("ms3.a"u8, "ms3.main"u8, entries, mapA));
        ui.Register(new ModeDefinition("ms3.b"u8, "ms3.main"u8, entries, mapB));
        ui.SetMode("ms3.a"u8);
        context.Update(0f);

        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(mapA.IsHeld(FireAction));

        ui.SetMode("ms3.b"u8);
        context.Update(0f);
        Assert.False(mapA.IsHeld(FireAction));
        Assert.False(mapB.IsHeld(JumpAction));

        context.PushEvent(KeyUp(SpaceKey));
        context.Update(0f);
        Assert.False(mapB.WasReleased(JumpAction));
    }

    /// <summary>
    /// The keep rule: the same map in two modes keeps its values. The manager activates the map of the new mode, and a
    /// second <see cref="UiActionMap.Activate"/> on the same context keeps each value, so a held action stays held with
    /// no released flag.
    /// </summary>
    [Fact]
    public void TheSameMapInTwoModesKeepsAHeldAction()
    {
        UiContext context = CreateContext();
        PanelContext panels = NewPanels();
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });
        PanelFactory p = new();
        LayoutFactory layout = new();
        ui.Register(new PanelDefinition("ms4.p"u8, "left"u8, p.Create));
        ui.Register(new LayoutDefinition("ms4.main"u8, layout.Create));
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey);
        ModePanel[] entries = [new ModePanel("left"u8, "ms4.p"u8)];
        ui.Register(new ModeDefinition("ms4.a"u8, "ms4.main"u8, entries, map));
        ui.Register(new ModeDefinition("ms4.b"u8, "ms4.main"u8, entries, map));
        ui.SetMode("ms4.a"u8);
        context.Update(0f);

        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.IsHeld(FireAction));

        ui.SetMode("ms4.b"u8);
        context.Update(0f);
        Assert.True(map.IsHeld(FireAction));
        Assert.False(map.WasReleased(FireAction));
    }

    /// <summary>
    /// The replace rule: a map that another map replaced reads false and zero, with no <see cref="UiActionMap.Deactivate"/>
    /// call. A fresh <see cref="UiActionMap.Activate"/> of it starts with no value, so a Button that is still down needs
    /// a new press. The pair rule: a game that clears <see cref="UiContext.OnInputRead"/> by hand, and keeps the
    /// unhandled slot, reads the same false and zero.
    /// </summary>
    [Fact]
    public void AReplacedMapReadsZero()
    {
        UiContext context = CreateContext();
        UiActionMap mapA = CreateMap();
        mapA.AddAction(FireAction, UiActionType.Button);
        mapA.AddAction(MoveAction, UiActionType.Vector2);
        mapA.BindKey(FireAction, SpaceKey);
        mapA.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        UiActionMap mapB = CreateMap();
        mapB.AddAction(JumpAction, UiActionType.Button);
        mapB.BindKey(JumpAction, SpaceKey);
        mapA.Activate(context);

        context.PushEvent(Key(SpaceKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(mapA.IsHeld(FireAction));
        AssertVector(mapA.ReadVector(MoveAction), 0f, 1f);

        mapB.Activate(context);
        Assert.False(mapA.IsHeld(FireAction));
        Assert.False(mapA.WasPressed(FireAction));
        Assert.False(mapA.WasReleased(FireAction));
        AssertVector(mapA.ReadVector(MoveAction), 0f, 0f);

        context.Update(0f);
        Assert.False(mapA.IsHeld(FireAction));
        AssertVector(mapA.ReadVector(MoveAction), 0f, 0f);

        mapA.Activate(context);
        context.Update(0f);
        Assert.False(mapA.IsHeld(FireAction));

        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(mapA.IsHeld(FireAction));

        // The pair rule: the map keeps its unhandled slot, and a hand-written game clears the read slot.
        Assert.True(mapA.IsActive);
        context.OnInputRead = null;
        Assert.False(mapA.IsActive);
        Assert.False(mapA.IsHeld(FireAction));
        Assert.False(mapA.WasPressed(FireAction));
        Assert.False(mapA.WasReleased(FireAction));
        AssertVector(mapA.ReadVector(MoveAction), 0f, 0f);

        context.Update(0f);
        Assert.False(mapA.IsHeld(FireAction));
        AssertVector(mapA.ReadVector(MoveAction), 0f, 0f);
    }

    /// <summary>
    /// The value rule (Decision 12): a Vector2 action reads a held key in the first frame of the next map at once.
    /// </summary>
    [Fact]
    public void AValueActionReadsAHeldInputInTheNextMap()
    {
        UiContext context = CreateContext();
        UiActionMap mapA = CreateMap();
        mapA.AddAction(MoveAction, UiActionType.Vector2);
        mapA.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        UiActionMap mapB = CreateMap();
        mapB.AddAction(MoveAction, UiActionType.Vector2);
        mapB.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        mapA.Activate(context);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        AssertVector(mapA.ReadVector(MoveAction), 0f, 1f);

        mapB.Activate(context);
        context.Update(0f);
        AssertVector(mapB.ReadVector(MoveAction), 0f, 1f);
    }

    /// <summary>Creates a context with a measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>Creates a map on a new empty registry.</summary>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
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

    /// <summary>Asserts the two components of a vector with a precision of five decimals.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="x">The expected X component.</param>
    /// <param name="y">The expected Y component.</param>
    private static void AssertVector(float2 value, float x, float y)
    {
        Assert.Equal(x, value.X, 5);
        Assert.Equal(y, value.Y, 5);
    }

    /// <summary>Creates a key down event with no target.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Key(int key)
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

    /// <summary>Creates a key up event with no target.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent KeyUp(int key)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyUp, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }

    /// <summary>The factory of a panel. Each call records the root it creates.</summary>
    private sealed class PanelFactory
    {
        /// <summary>The root of the last call, or <see langword="null"/> before the first call.</summary>
        public Element? Last;

        /// <summary>Creates a root of 100 by 50.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Element root = new();
            root.Inline.Width = 100;
            root.Inline.Height = 50;
            Last = root;
            return root;
        }
    }

    /// <summary>The factory of a layout. Each call holds the two slots it builds.</summary>
    private sealed class LayoutFactory
    {
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
}
