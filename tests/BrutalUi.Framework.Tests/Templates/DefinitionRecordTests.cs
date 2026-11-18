using System;
using System.Globalization;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The record forms of phase 2: a mode names its action map with <c>actions</c>, a <c>PanelRef</c> directly under
/// <c>Mode</c> takes the default slot of its panel (Q5), the slot order and the panel order follow the text, and a
/// bound panel template binds through the <see cref="TemplateBindings"/> that the game attaches to the loader.
/// </summary>
/// <remarks>
/// <para>
/// Each text is a <c>u8</c> literal, because <see cref="DefinitionLoader.Load"/> takes a span. Each test builds a
/// context with a <see cref="RecordingRenderer"/> measurer, a default font size of 16, and a viewport of 800 by 600,
/// and each manager with a <see cref="PanelContext"/> of a new <see cref="CommandRegistry"/>, <see cref="EventBus"/>,
/// and <see cref="ServiceRegistry"/>. One <see cref="DefinitionLoader"/> serves one <see cref="TemplateLoader"/>, so
/// each test makes its own pair.
/// </para>
/// <para>
/// The names of the records start with <c>dr.</c>, so each text reads as one feature sample.
/// </para>
/// </remarks>
public sealed class DefinitionRecordTests
{
    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>Gets the definitions text with one map: one panel, one layout, and one mode with
    /// <c>actions="dr.map"</c>.</summary>
    private static ReadOnlySpan<byte> WithActionsText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="dr.hud" slot="top" template="dr.panel" />
          <ui:Layout name="dr.game" template="dr.layout" />
          <ui:Mode name="dr.play" layout="dr.game" actions="dr.map">
            <ui:Slot name="top">
              <ui:PanelRef name="dr.hud" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the same text with no <c>actions</c> attribute.</summary>
    private static ReadOnlySpan<byte> NoActionsText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="dr.hud" slot="top" template="dr.panel" />
          <ui:Layout name="dr.game" template="dr.layout" />
          <ui:Mode name="dr.play" layout="dr.game">
            <ui:Slot name="top">
              <ui:PanelRef name="dr.hud" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the text with a direct <c>PanelRef</c>: the <c>dr.chat</c> panel names the slot <c>bottom</c>,
    /// and its <c>PanelRef</c> sits directly under the mode.</summary>
    private static ReadOnlySpan<byte> DefaultSlotText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="dr.hud" slot="top" template="dr.panel" />
          <ui:Panel name="dr.chat" slot="bottom" template="dr.chat" />
          <ui:Layout name="dr.game" template="dr.layout" />
          <ui:Mode name="dr.play" layout="dr.game">
            <ui:Slot name="top">
              <ui:PanelRef name="dr.hud" />
            </ui:Slot>
            <ui:PanelRef name="dr.chat" />
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the text for the order of the slots and the panels: two <c>Slot</c> elements and one direct
    /// <c>PanelRef</c>.</summary>
    private static ReadOnlySpan<byte> OrderText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="dr.p1" slot="center" template="dr.panel" />
          <ui:Panel name="dr.p2" slot="center" template="dr.panel" />
          <ui:Panel name="dr.p3" slot="center" template="dr.panel" />
          <ui:Panel name="dr.p4" slot="center" template="dr.panel" />
          <ui:Layout name="dr.game" template="dr.layout" />
          <ui:Mode name="dr.play" layout="dr.game">
            <ui:Slot name="bottom">
              <ui:PanelRef name="dr.p1" />
              <ui:PanelRef name="dr.p2" />
            </ui:Slot>
            <ui:Slot name="top">
              <ui:PanelRef name="dr.p3" />
            </ui:Slot>
            <ui:PanelRef name="dr.p4" />
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the definitions text of the bound panel: one panel with a bound template, one layout, and one
    /// mode.</summary>
    private static ReadOnlySpan<byte> BoundText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="dr.hud" slot="top" template="dr.bound" />
          <ui:Layout name="dr.game" template="dr.layout" />
          <ui:Mode name="dr.play" layout="dr.game">
            <ui:Slot name="top">
              <ui:PanelRef name="dr.hud" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the panel template: one element with no child.</summary>
    private static ReadOnlySpan<byte> PanelTemplate => """
        <ui:Element name="dr.hudRoot" xmlns:ui="brutal-ui" />
        """u8;

    /// <summary>Gets the chat panel template: one element named <c>dr.chatRoot</c>.</summary>
    private static ReadOnlySpan<byte> ChatTemplate => """
        <ui:Element name="dr.chatRoot" xmlns:ui="brutal-ui" />
        """u8;

    /// <summary>Gets the layout template: a root that is not pickable, with the slots <c>top</c>, <c>bottom</c>,
    /// and <c>center</c>. The <c>center</c> slot holds the default-slot placement of <c>OrderText</c>, which the
    /// phase 3 slot check needs.</summary>
    private static ReadOnlySpan<byte> LayoutTemplate => """
        <ui:Element pickable="false" xmlns:ui="brutal-ui">
          <ui:SlotContainer slot="top" />
          <ui:SlotContainer slot="bottom" />
          <ui:SlotContainer slot="center" />
        </ui:Element>
        """u8;

    /// <summary>Gets the bound panel template: one label with <c>bind-text="dr.score"</c> and one button with
    /// <c>command="dr.add"</c>.</summary>
    private static ReadOnlySpan<byte> BoundTemplate => """
        <ui:Element name="dr.boundRoot" xmlns:ui="brutal-ui">
          <ui:Label name="score" text="0" bind-text="dr.score" />
          <ui:Button name="add" text="Add" command="dr.add" />
        </ui:Element>
        """u8;

    /// <summary>
    /// A mode names its action map with <c>actions</c>: the record holds the map instance that the game added with
    /// <see cref="DefinitionLoader.AddActionMap"/>, and after the mode set and one update the map reads active.
    /// </summary>
    [Fact]
    public void AModeNamesItsActionMap()
    {
        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);
        UiActionMap map = CreateMap();

        DefinitionLoader loader = CreateLoader(new TemplateLoader());
        loader.AddActionMap("dr.map", map);
        DefinitionLoadResult result = loader.Load(WithActionsText);

        Assert.True(result.Succeeded);
        Assert.Same(map, result.Modes[0].ActionMap);

        result.RegisterTo(ui);
        ui.SetMode("dr.play"u8);
        context.Update(0.016f);

        Assert.True(map.IsActive);
    }

    /// <summary>
    /// A mode with no <c>actions</c> attribute gets no map: the record holds <see langword="null"/>. An absent value
    /// and no value read the same, because <c>DefinitionValue.HasName</c> is the single test of the resolve pass.
    /// </summary>
    [Fact]
    public void AModeWithNoActionsHasNoMap()
    {
        DefinitionLoader loader = CreateLoader(new TemplateLoader());
        loader.AddActionMap("dr.map", CreateMap());
        DefinitionLoadResult result = loader.Load(NoActionsText);

        Assert.True(result.Succeeded);
        Assert.Null(result.Modes[0].ActionMap);
    }

    /// <summary>
    /// A <c>PanelRef</c> directly under <c>Mode</c> takes the <see cref="PanelDefinition.DefaultSlot"/> of its panel
    /// (Q5): the record places <c>dr.chat</c> in the slot <c>bottom</c>, and after the mode set and one update the
    /// chat root mounts in that slot.
    /// </summary>
    [Fact]
    public void APanelRefWithNoSlotUsesTheDefaultSlot()
    {
        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);

        DefinitionLoader loader = CreateLoader(new TemplateLoader());
        loader.AddTemplate("dr.chat", ChatTemplate);
        DefinitionLoadResult result = loader.Load(DefaultSlotText);

        Assert.True(result.Succeeded);
        ModeDefinition mode = result.Modes[0];
        Assert.Equal(2, mode.SlotCount);
        Assert.True(mode.GetSlotName(1).SequenceEqual("bottom"u8));
        Assert.Equal(1, mode.GetPanelCount(1));
        Assert.True(mode.GetPanelName(1, 0).SequenceEqual("dr.chat"u8));

        result.RegisterTo(ui);
        ui.SetMode("dr.play"u8);
        context.Update(0.016f);

        SlotContainer? bottom = SlotContainer.Find(context.Roots[0], "bottom"u8);
        Assert.NotNull(bottom);
        Assert.Equal(1, bottom.Children.Count);
        Assert.Equal("dr.chatRoot", bottom.Children[0].Name);
    }

    /// <summary>
    /// The slot order and the panel order follow the text: two <c>Slot</c> elements and one direct <c>PanelRef</c>
    /// give the slots <c>bottom</c>, <c>top</c>, and <c>center</c> in the order of the first entry of each slot, and
    /// the panels of one slot in the order of the text.
    /// </summary>
    [Fact]
    public void TheSlotOrderAndThePanelOrderFollowTheText()
    {
        DefinitionLoader loader = CreateLoader(new TemplateLoader());
        DefinitionLoadResult result = loader.Load(OrderText);

        Assert.True(result.Succeeded);
        ModeDefinition mode = result.Modes[0];

        Assert.Equal(3, mode.SlotCount);
        Assert.True(mode.GetSlotName(0).SequenceEqual("bottom"u8));
        Assert.True(mode.GetSlotName(1).SequenceEqual("top"u8));
        Assert.True(mode.GetSlotName(2).SequenceEqual("center"u8));

        Assert.Equal(2, mode.GetPanelCount(0));
        Assert.True(mode.GetPanelName(0, 0).SequenceEqual("dr.p1"u8));
        Assert.True(mode.GetPanelName(0, 1).SequenceEqual("dr.p2"u8));
        Assert.Equal(1, mode.GetPanelCount(1));
        Assert.True(mode.GetPanelName(1, 0).SequenceEqual("dr.p3"u8));
        Assert.Equal(1, mode.GetPanelCount(2));
        Assert.True(mode.GetPanelName(2, 0).SequenceEqual("dr.p4"u8));
    }

    /// <summary>
    /// A bound panel template binds through the attached <see cref="TemplateBindings"/>: after the mount the label
    /// shows the store value, a store set and one update change it, and a click on the button runs the command one
    /// time.
    /// </summary>
    [Fact]
    public void ABoundPanelTemplateBindsThroughTheAttachedBindings()
    {
        UiContext context = CreateContext();
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("dr.add"u8, s_count, counter);
        Store<int> store = new(new Store<int>.CreateInfo { Initial = 5 });

        TemplateBindings bindings = new(registry);
        bindings.AddText("dr.score", store, static value => value, null, CultureInfo.InvariantCulture);
        TemplateLoader templates = new();
        bindings.AttachTo(templates);

        DefinitionLoader loader = new(templates);
        loader.AddTemplate("dr.bound", BoundTemplate);
        loader.AddTemplate("dr.layout", LayoutTemplate);
        UiManager ui = CreateManager(context);
        DefinitionLoadResult result = loader.Load(BoundText);

        Assert.True(result.Succeeded);
        result.RegisterTo(ui);
        ui.SetMode("dr.play"u8);
        context.Update(0.016f);

        // The layout factory builds the root at the first mount. The template holds no size attribute, so the test
        // sizes the root here, which gives each child its measured rect for the click.
        context.Roots[0].Inline.Width = 800f;
        context.Roots[0].Inline.Height = 600f;
        context.Update(0.016f);

        SlotContainer? slot = SlotContainer.Find(context.Roots[0], "top"u8);
        Assert.NotNull(slot);
        Assert.Equal(1, slot.Children.Count);
        Label? score = slot.Children[0].Find<Label>("score"u8);
        Button? add = slot.Children[0].Find<Button>("add"u8);
        Assert.NotNull(score);
        Assert.NotNull(add);
        Assert.True(score.Text.SequenceEqual("5"u8));

        store.Set(9);
        context.Update(0.016f);
        Assert.True(score.Text.SequenceEqual("9"u8));

        float2 center = ThousandWidgetTree.ViewportCenter(add);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0.016f);
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>Creates the loader for one test and adds the panel and the layout template.</summary>
    /// <param name="templates">The template loader that the definition loader registers on.</param>
    /// <returns>The loader.</returns>
    private static DefinitionLoader CreateLoader(TemplateLoader templates)
    {
        DefinitionLoader loader = new(templates);
        loader.AddTemplate("dr.panel", PanelTemplate);
        loader.AddTemplate("dr.layout", LayoutTemplate);
        return loader;
    }

    /// <summary>Creates an action map with no action.</summary>
    /// <returns>The map.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
    }

    /// <summary>Creates a context with a measurer, a default font size of 16, and a viewport of 800 by 600.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), DefaultFontSize = 16f })
        {
            ViewportSize = new float2(800f, 600f),
        };
    }

    /// <summary>Creates a manager on one context, with a fresh panel context.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The manager.</returns>
    private static UiManager CreateManager(UiContext context)
    {
        return new UiManager(new UiManager.CreateInfo { Context = context, Panels = CreatePanels() });
    }

    /// <summary>Creates a panel context with a new command registry, event bus, and service registry.</summary>
    /// <returns>The panel context.</returns>
    private static PanelContext CreatePanels()
    {
        return new PanelContext
        {
            Commands = new CommandRegistry(),
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
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

    /// <summary>The target of the command handler: a call counter.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }
}
