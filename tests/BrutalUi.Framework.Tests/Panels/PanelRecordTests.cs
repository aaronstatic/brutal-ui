using System;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the panel data model: <see cref="PanelContext"/>, <see cref="PanelDefinition"/>,
/// <see cref="LayoutDefinition"/>, <see cref="ModePanel"/>, and <see cref="ModeDefinition"/>.
/// </summary>
public sealed class PanelRecordTests
{
    /// <summary>The factory that the tests pass to a definition.</summary>
    private static readonly Func<PanelContext, Element> RootFactory = NewRoot;

    /// <summary>Creates a new, detached element.</summary>
    /// <param name="context">The context. The factory ignores it.</param>
    /// <returns>A new element.</returns>
    private static Element NewRoot(PanelContext context) => new Element();

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

    /// <summary>Gets the bytes of a text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The UTF-8 bytes.</returns>
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// Creates a mode with the entries right:c, left:b, right:a. A sort by name gives a different order for the
    /// slots and for the panels of the slot right.
    /// </summary>
    /// <returns>The mode.</returns>
    private static ModeDefinition NewMode()
    {
        ModePanel[] panels =
        [
            new ModePanel("right"u8, "c"u8),
            new ModePanel("left"u8, "b"u8),
            new ModePanel("right"u8, "a"u8),
        ];
        return new ModeDefinition("explore"u8, "main"u8, panels);
    }

    [Fact]
    public void PanelContextHoldsTheThreeRegistries()
    {
        CommandRegistry commands = new();
        EventBus events = new();
        ServiceRegistry services = new();

        PanelContext context = new() { Commands = commands, Events = events, Services = services };

        Assert.Same(commands, context.Commands);
        Assert.Same(events, context.Events);
        Assert.Same(services, context.Services);
    }

    [Fact]
    public void PanelContextRejectsANullRegistry()
    {
        Assert.Throws<ArgumentNullException>(() => new PanelContext
        {
            Commands = null!,
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        });
        Assert.Throws<ArgumentNullException>(() => new PanelContext
        {
            Commands = new CommandRegistry(),
            Events = null!,
            Services = new ServiceRegistry(),
        });
        Assert.Throws<ArgumentNullException>(() => new PanelContext
        {
            Commands = new CommandRegistry(),
            Events = new EventBus(),
            Services = null!,
        });
    }

    [Fact]
    public void APanelDefinitionHoldsItsNamesAndItsFactory()
    {
        PanelDefinition definition = new("inventory"u8, "left"u8, RootFactory);

        Assert.True(definition.Name.SequenceEqual("inventory"u8));
        Assert.True(definition.DefaultSlot.SequenceEqual("left"u8));
        Assert.Same(RootFactory, definition.CreateRoot);

        PanelDefinition other = new("inventory"u8, "left"u8, RootFactory);
        PanelDefinition copy = definition with { };

        Assert.NotEqual(definition, other);
        Assert.Equal(definition, copy);
        Assert.True(copy.Name.SequenceEqual("inventory"u8));
        Assert.True(copy.DefaultSlot.SequenceEqual("left"u8));
    }

    [Fact]
    public void APanelDefinitionCopiesItsNames()
    {
        byte[] name = Bytes("inventory");
        byte[] slot = Bytes("left");

        PanelDefinition definition = new(name, slot, RootFactory);
        name[0] = (byte)'X';
        slot[0] = (byte)'Y';

        Assert.True(definition.Name.SequenceEqual("inventory"u8));
        Assert.True(definition.DefaultSlot.SequenceEqual("left"u8));
    }

    [Fact]
    public void APanelDefinitionRejectsAnEmptyNameAnEmptySlotAndANullFactory()
    {
        ArgumentException emptyName = Assert.Throws<ArgumentException>(
            () => new PanelDefinition(ReadOnlySpan<byte>.Empty, "left"u8, RootFactory));
        Assert.Equal("name", emptyName.ParamName);

        ArgumentException emptySlot = Assert.Throws<ArgumentException>(
            () => new PanelDefinition("inventory"u8, ReadOnlySpan<byte>.Empty, RootFactory));
        Assert.Equal("defaultSlot", emptySlot.ParamName);

        ArgumentNullException nullFactory = Assert.Throws<ArgumentNullException>(
            () => new PanelDefinition("inventory"u8, "left"u8, null!));
        Assert.Equal("createRoot", nullFactory.ParamName);
    }

    [Fact]
    public void ALayoutDefinitionHoldsACopyOfItsNameAndItsFactory()
    {
        byte[] name = Bytes("main");

        LayoutDefinition definition = new(name, RootFactory);
        name[0] = (byte)'X';

        Assert.True(definition.Name.SequenceEqual("main"u8));
        Assert.Same(RootFactory, definition.CreateRoot);

        LayoutDefinition other = new("main"u8, RootFactory);
        LayoutDefinition copy = definition with { };

        Assert.NotEqual(definition, other);
        Assert.Equal(definition, copy);
        Assert.True(copy.Name.SequenceEqual("main"u8));
    }

    [Fact]
    public void ALayoutDefinitionRejectsAnEmptyNameAndANullFactory()
    {
        ArgumentException emptyName = Assert.Throws<ArgumentException>(
            () => new LayoutDefinition(ReadOnlySpan<byte>.Empty, RootFactory));
        Assert.Equal("name", emptyName.ParamName);

        ArgumentNullException nullFactory = Assert.Throws<ArgumentNullException>(
            () => new LayoutDefinition("main"u8, null!));
        Assert.Equal("createRoot", nullFactory.ParamName);
    }

    [Fact]
    public void AModeDefinitionGroupsThePanelsBySlotInTheOrderOfTheList()
    {
        ModeDefinition mode = NewMode();

        Assert.Equal(2, mode.SlotCount);
        Assert.True(mode.GetSlotName(0).SequenceEqual("right"u8));
        Assert.Equal(2, mode.GetPanelCount(0));
        Assert.True(mode.GetPanelName(0, 0).SequenceEqual("c"u8));
        Assert.True(mode.GetPanelName(0, 1).SequenceEqual("a"u8));
        Assert.True(mode.GetSlotName(1).SequenceEqual("left"u8));
        Assert.Equal(1, mode.GetPanelCount(1));
        Assert.True(mode.GetPanelName(1, 0).SequenceEqual("b"u8));
    }

    [Fact]
    public void AModeDefinitionHoldsItsLayoutAndItsOptionalActionMap()
    {
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });

        ModeDefinition withMap = new("explore"u8, "main"u8, ReadOnlySpan<ModePanel>.Empty, map);
        ModeDefinition withoutMap = NewMode();

        Assert.True(withMap.Name.SequenceEqual("explore"u8));
        Assert.True(withMap.Layout.SequenceEqual("main"u8));
        Assert.Same(map, withMap.ActionMap);
        Assert.Null(withoutMap.ActionMap);

        ModeDefinition other = new("explore"u8, "main"u8, ReadOnlySpan<ModePanel>.Empty, map);
        ModeDefinition copy = withMap with { };

        Assert.NotEqual(withMap, other);
        Assert.Equal(withMap, copy);
        Assert.True(copy.Name.SequenceEqual("explore"u8));
        Assert.True(copy.Layout.SequenceEqual("main"u8));
        Assert.Same(map, copy.ActionMap);
    }

    [Fact]
    public void AModeDefinitionCopiesItsNames()
    {
        byte[] name = Bytes("explore");
        byte[] layout = Bytes("main");
        byte[] slot = Bytes("left");
        byte[] panel = Bytes("a");
        ModePanel[] panels = [new ModePanel(slot, panel)];

        ModePanel entry = panels[0];
        Assert.True(entry.Slot.SequenceEqual("left"u8));
        Assert.True(entry.Panel.SequenceEqual("a"u8));
        ModePanel none = default;
        Assert.True(none.Slot.IsEmpty);
        Assert.True(none.Panel.IsEmpty);

        ModeDefinition mode = new(name, layout, panels);
        name[0] = (byte)'X';
        layout[0] = (byte)'X';
        slot[0] = (byte)'X';
        panel[0] = (byte)'X';

        Assert.True(mode.Name.SequenceEqual("explore"u8));
        Assert.True(mode.Layout.SequenceEqual("main"u8));
        Assert.True(mode.GetSlotName(0).SequenceEqual("left"u8));
        Assert.True(mode.GetPanelName(0, 0).SequenceEqual("a"u8));
    }

    [Fact]
    public void AModeDefinitionWithNoPanelHoldsNoSlot()
    {
        ModeDefinition mode = new("empty"u8, "main"u8, ReadOnlySpan<ModePanel>.Empty);

        Assert.Equal(0, mode.SlotCount);
    }

    [Fact]
    public void AModeDefinitionRejectsAnEmptyNameAnEmptyLayoutAnEmptyEntryAndAPanelThatItListsTwoTimes()
    {
        ModePanel[] one = [new ModePanel("left"u8, "a"u8)];

        ArgumentException emptyName = Assert.Throws<ArgumentException>(
            () => new ModeDefinition(ReadOnlySpan<byte>.Empty, "main"u8, one));
        Assert.Equal("name", emptyName.ParamName);

        ArgumentException emptyLayout = Assert.Throws<ArgumentException>(
            () => new ModeDefinition("explore"u8, ReadOnlySpan<byte>.Empty, one));
        Assert.Equal("layout", emptyLayout.ParamName);

        ArgumentException emptySlot = Assert.Throws<ArgumentException>(
            () => new ModePanel(ReadOnlySpan<byte>.Empty, "a"u8));
        Assert.Equal("slot", emptySlot.ParamName);

        ArgumentException emptyPanel = Assert.Throws<ArgumentException>(
            () => new ModePanel("left"u8, ReadOnlySpan<byte>.Empty));
        Assert.Equal("panel", emptyPanel.ParamName);

        ModePanel[] emptyEntry = [new ModePanel("left"u8, "a"u8), default];
        ArgumentException emptyEntryError = Assert.Throws<ArgumentException>(
            () => new ModeDefinition("explore"u8, "main"u8, emptyEntry));
        Assert.Equal("panels", emptyEntryError.ParamName);

        ModePanel[] sameSlot = [new ModePanel("left"u8, "a"u8), new ModePanel("left"u8, "a"u8)];
        ArgumentException sameSlotError = Assert.Throws<ArgumentException>(
            () => new ModeDefinition("explore"u8, "main"u8, sameSlot));
        Assert.Equal("panels", sameSlotError.ParamName);

        ModePanel[] twoSlots = [new ModePanel("left"u8, "a"u8), new ModePanel("right"u8, "a"u8)];
        ArgumentException twoSlotsError = Assert.Throws<ArgumentException>(
            () => new ModeDefinition("explore"u8, "main"u8, twoSlots));
        Assert.Equal("panels", twoSlotsError.ParamName);
    }

    [Fact]
    public void TheModeGettersRejectAnIndexOutsideTheRange()
    {
        ModeDefinition mode = NewMode();

        ArgumentOutOfRangeException negativeSlot = Assert.Throws<ArgumentOutOfRangeException>(() => mode.GetSlotName(-1));
        Assert.Equal("slot", negativeSlot.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => mode.GetSlotName(mode.SlotCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => mode.GetPanelCount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => mode.GetPanelCount(mode.SlotCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => mode.GetPanelName(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => mode.GetPanelName(mode.SlotCount, 0));

        ArgumentOutOfRangeException negativeIndex = Assert.Throws<ArgumentOutOfRangeException>(() => mode.GetPanelName(0, -1));
        Assert.Equal("index", negativeIndex.ParamName);
        ArgumentOutOfRangeException countIndex = Assert.Throws<ArgumentOutOfRangeException>(
            () => mode.GetPanelName(0, mode.GetPanelCount(0)));
        Assert.Equal("index", countIndex.ParamName);
    }

    [Fact]
    public void ToStringShowsTheNamesAsText()
    {
        PanelDefinition panel = new("inventory"u8, "left"u8, RootFactory);
        LayoutDefinition layout = new("main"u8, RootFactory);
        ModeDefinition mode = NewMode();

        Assert.Equal("PanelDefinition { Name = inventory, DefaultSlot = left }", panel.ToString());
        Assert.Equal("LayoutDefinition { Name = main }", layout.ToString());
        Assert.Equal("ModeDefinition { Name = explore, Layout = main, SlotCount = 2 }", mode.ToString());
    }
}
