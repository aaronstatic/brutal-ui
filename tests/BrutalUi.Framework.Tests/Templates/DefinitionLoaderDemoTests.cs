using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The phase 1 demo: one definitions text with one panel, one layout, and one mode loads into the three records,
/// registers on a manager, and mounts the loaded panel root in the slot of the loaded layout.
/// </summary>
/// <remarks>
/// <para>
/// The definitions text of <see cref="OneLoadedPanelMountsInItsSlot"/> equals the text of <strong>Definition
/// format</strong> in section 11 of <c>docs/technical-design.md</c>, word for word, with LF line ends. Each text is a
/// <c>u8</c> literal, because <see cref="DefinitionLoader.Load"/> takes a span.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 800 by 600, and each
/// manager with a <see cref="PanelContext"/> of a new <see cref="CommandRegistry"/>, <see cref="EventBus"/>, and
/// <see cref="ServiceRegistry"/>. One <see cref="DefinitionLoader"/> serves one <see cref="TemplateLoader"/>, so each
/// test makes its own pair. The templates hold no binding, so no <see cref="TemplateBindings"/> attaches.
/// </para>
/// </remarks>
public sealed class DefinitionLoaderDemoTests
{
    /// <summary>Gets the definitions text of the demo: one panel, one layout, and one mode.</summary>
    private static ReadOnlySpan<byte> DefinitionsText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="hud" slot="top" template="hud.panel" />
          <ui:Layout name="game" template="game.layout" />
          <ui:Mode name="play" layout="game">
            <ui:Slot name="top">
              <ui:PanelRef name="hud" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the same definitions text with the <c>Mode</c> first, then the <c>Layout</c>, then the
    /// <c>Panel</c>. The entry order makes no difference to the load.</summary>
    private static ReadOnlySpan<byte> ReorderedText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Mode name="play" layout="game">
            <ui:Slot name="top">
              <ui:PanelRef name="hud" />
            </ui:Slot>
          </ui:Mode>
          <ui:Layout name="game" template="game.layout" />
          <ui:Panel name="hud" slot="top" template="hud.panel" />
        </ui:Definitions>
        """u8;

    /// <summary>Gets the panel template: an element named <c>hud</c> with one label named <c>score</c>.</summary>
    private static ReadOnlySpan<byte> HudPanelTemplate => """
        <ui:Element name="hud" xmlns:ui="brutal-ui">
          <ui:Label name="score" text="0" />
        </ui:Element>
        """u8;

    /// <summary>Gets the layout template: a root that is not pickable, with one slot named <c>top</c>.</summary>
    private static ReadOnlySpan<byte> GameLayoutTemplate => """
        <ui:Element pickable="false" xmlns:ui="brutal-ui">
          <ui:SlotContainer slot="top" />
        </ui:Element>
        """u8;

    /// <summary>
    /// The demo. The load gives one record of each kind. After <see cref="DefinitionLoadResult.RegisterTo"/>, the
    /// mode set, and one update, the slot <c>top</c> of the layout root holds one child named <c>hud</c>, and that
    /// child holds the label <c>score</c>.
    /// </summary>
    [Fact]
    public void OneLoadedPanelMountsInItsSlot()
    {
        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);
        DefinitionLoadResult result = LoadDemo(new TemplateLoader(), DefinitionsText);

        Assert.True(result.Succeeded);
        Assert.Single(result.Panels);
        Assert.Single(result.Layouts);
        Assert.Single(result.Modes);

        result.RegisterTo(ui);
        ui.SetMode("play"u8);
        context.Update(0.016f);

        Assert.Equal(1, context.Roots.Length);
        SlotContainer? slot = SlotContainer.Find(context.Roots[0], "top"u8);
        Assert.NotNull(slot);
        Assert.Equal(1, slot.Children.Count);
        Assert.Equal("hud", slot.Children[0].Name);

        Label? score = slot.Children[0].Find<Label>("score"u8);
        Assert.NotNull(score);
    }

    /// <summary>
    /// The panel factory loads its template at each call (A4): two <see cref="PanelDefinition.CreateRoot"/> calls
    /// give two roots that are not the same object and hold the same tree.
    /// </summary>
    [Fact]
    public void ThePanelFactoryLoadsItsTemplateAtEachCall()
    {
        DefinitionLoadResult result = LoadDemo(new TemplateLoader(), DefinitionsText);
        Assert.True(result.Succeeded);

        PanelDefinition panel = result.Panels[0];
        PanelContext panels = CreatePanels();
        Element first = panel.CreateRoot(panels);
        Element second = panel.CreateRoot(panels);

        Assert.NotSame(first, second);
        Assert.Null(TreeCompare.FirstDifference(first, second));
    }

    /// <summary>
    /// The layout template builds a <see cref="SlotContainer"/>: the layout factory gives a root that holds the slot
    /// <c>top</c>, and the slot name and the element name both read <c>top</c> (Q2).
    /// </summary>
    [Fact]
    public void TheLayoutTemplateBuildsASlotContainer()
    {
        DefinitionLoadResult result = LoadDemo(new TemplateLoader(), DefinitionsText);
        Assert.True(result.Succeeded);

        Element root = result.Layouts[0].CreateRoot(CreatePanels());
        SlotContainer? slot = SlotContainer.Find(root, "top"u8);

        Assert.NotNull(slot);
        Assert.True(slot.SlotName.SequenceEqual("top"u8));
        Assert.Equal("top", slot.Name);
    }

    /// <summary>
    /// A text with the <c>Mode</c> first, then the <c>Layout</c>, then the <c>Panel</c> loads with no error and
    /// mounts the same tree as the demo text. The resolve pass reads the entries of the whole text before it checks
    /// the names of a mode.
    /// </summary>
    [Fact]
    public void TheEntryOrderDoesNotMatter()
    {
        UiContext demo = Mount(DefinitionsText, out DefinitionLoadResult demoResult);
        UiContext reordered = Mount(ReorderedText, out DefinitionLoadResult reorderedResult);

        Assert.True(demoResult.Succeeded);
        Assert.True(reorderedResult.Succeeded);
        Assert.Single(reorderedResult.Panels);
        Assert.Single(reorderedResult.Layouts);
        Assert.Single(reorderedResult.Modes);

        Assert.Equal(1, demo.Roots.Length);
        Assert.Equal(1, reordered.Roots.Length);
        Assert.Null(TreeCompare.FirstDifference(demo.Roots[0], reordered.Roots[0]));

        SlotContainer? slot = SlotContainer.Find(reordered.Roots[0], "top"u8);
        Assert.NotNull(slot);
        Assert.Equal(1, slot.Children.Count);
        Assert.Equal("hud", slot.Children[0].Name);
    }

    /// <summary>Loads one definitions text, registers it on a new manager, sets the <c>play</c> mode, and runs one
    /// update.</summary>
    /// <param name="definitions">The definitions text.</param>
    /// <param name="result">The load result.</param>
    /// <returns>The context that holds the mounted layout root.</returns>
    private static UiContext Mount(ReadOnlySpan<byte> definitions, out DefinitionLoadResult result)
    {
        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);
        result = LoadDemo(new TemplateLoader(), definitions);
        result.RegisterTo(ui);
        ui.SetMode("play"u8);
        context.Update(0.016f);
        return context;
    }

    /// <summary>Adds the two demo templates to one template loader and loads one definitions text.</summary>
    /// <param name="templates">The template loader for this load. Each loader takes one definition loader.</param>
    /// <param name="definitions">The definitions text.</param>
    /// <returns>The result.</returns>
    private static DefinitionLoadResult LoadDemo(TemplateLoader templates, ReadOnlySpan<byte> definitions)
    {
        DefinitionLoader loader = new(templates);
        loader.AddTemplate("hud.panel", HudPanelTemplate);
        loader.AddTemplate("game.layout", GameLayoutTemplate);
        return loader.Load(definitions);
    }

    /// <summary>Creates a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 800 by 600.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
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
}
