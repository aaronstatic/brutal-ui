using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The rules of <see cref="DefinitionLoader.AddFactory"/>: a factory panel and a factory layout mount, the load
/// calls no factory, the names of texts and factories share one table, the arguments have their checks, and
/// <see cref="DefinitionLoadResult.RegisterTo"/> gives the panel controllers in the order of the text.
/// </summary>
/// <remarks>
/// <para>
/// Each text is a <c>u8</c> literal, because <see cref="DefinitionLoader.Load"/> takes a span. One
/// <see cref="DefinitionLoader"/> serves one <see cref="TemplateLoader"/>, so each test makes its own pair. The
/// names of the records start with <c>fct.</c>. The C# factory builders follow the roots of
/// <see cref="DefinitionSet"/>.
/// </para>
/// </remarks>
public sealed class DefinitionFactoryTests
{
    /// <summary>Gets the definitions text of the mount test: a factory panel in <c>top</c>, a template panel in
    /// <c>bottom</c>, a factory layout, and one mode.</summary>
    private static ReadOnlySpan<byte> MountText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="fct.widgets" slot="top" template="fct.CreateWidgets" />
          <ui:Panel name="fct.state" slot="bottom" template="fct.state" />
          <ui:Layout name="fct.game" template="fct.CreateLayout" />
          <ui:Mode name="fct.main" layout="fct.game">
            <ui:Slot name="top">
              <ui:PanelRef name="fct.widgets" />
            </ui:Slot>
            <ui:Slot name="bottom">
              <ui:PanelRef name="fct.state" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the definitions text of the skipped-check tests: one panel and one mode on a factory layout
    /// with a slot name that no slot of the built root holds.</summary>
    private static ReadOnlySpan<byte> BadSlotText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="fct.hud" slot="top" template="fct.hud" />
          <ui:Layout name="fct.game" template="fct.CreateLayout" />
          <ui:Mode name="fct.play" layout="fct.game">
            <ui:Slot name="left">
              <ui:PanelRef name="fct.hud" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the definitions text of the name-table tests: one panel that names the shared template name
    /// <c>fct.name</c>.</summary>
    private static ReadOnlySpan<byte> OnePanelText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="fct.hud" slot="top" template="fct.name" />
        </ui:Definitions>
        """u8;

    /// <summary>Gets the definitions text of the controller tests: two template panels, no layout, and no
    /// mode.</summary>
    private static ReadOnlySpan<byte> TwoPanelsText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="fct.p1" slot="top" template="fct.t1" />
          <ui:Panel name="fct.p2" slot="bottom" template="fct.t2" />
        </ui:Definitions>
        """u8;

    /// <summary>Gets the panel template with the root name <c>fct.state-root</c>.</summary>
    private static ReadOnlySpan<byte> StatePanelText => """<ui:Element name="fct.state-root" xmlns:ui="brutal-ui" />"""u8;

    /// <summary>Gets the panel template with the root name <c>fct.hud-root</c>.</summary>
    private static ReadOnlySpan<byte> HudPanelText => """<ui:Element name="fct.hud-root" xmlns:ui="brutal-ui" />"""u8;

    /// <summary>Gets the panel template with the root name <c>fct.text-root</c>: the text of the shared-name
    /// tests.</summary>
    private static ReadOnlySpan<byte> TextFirstPanel => """<ui:Element name="fct.text-root" xmlns:ui="brutal-ui" />"""u8;

    /// <summary>
    /// A factory panel and a factory layout load and mount: the load succeeds, the record of the factory panel
    /// holds the added delegate as is (Decision 2), and after <see cref="DefinitionLoadResult.RegisterTo"/>, a
    /// mode set, and one update, the slot <c>top</c> holds the factory root and the slot <c>bottom</c> holds the
    /// template root.
    /// </summary>
    [Fact]
    public void AFactoryPanelAndAFactoryLayoutLoadAndMount()
    {
        Element? widgetsRoot = null;
        Func<PanelContext, Element> widgetsFactory = _ =>
        {
            widgetsRoot = new Element().Name("fct.widgets-root");
            return widgetsRoot;
        };
        Func<PanelContext, Element> layoutFactory = _ => BuildColumnRoot();

        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);
        DefinitionLoader loader = new(new TemplateLoader());
        loader.AddTemplate("fct.state", StatePanelText);
        loader.AddFactory("fct.CreateWidgets", widgetsFactory);
        loader.AddFactory("fct.CreateLayout", layoutFactory);

        DefinitionLoadResult result = loader.Load(MountText);
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Panels.Count);
        Assert.Single(result.Layouts);
        Assert.Single(result.Modes);
        Assert.Same(widgetsFactory, result.Panels[0].CreateRoot);

        result.RegisterTo(ui);
        ui.SetMode("fct.main"u8);
        context.Update(0.016f);

        Assert.Equal(1, context.Roots.Length);
        SlotContainer? top = SlotContainer.Find(context.Roots[0], "top"u8);
        SlotContainer? bottom = SlotContainer.Find(context.Roots[0], "bottom"u8);
        Assert.NotNull(top);
        Assert.NotNull(bottom);
        Assert.Equal(1, top.Children.Count);
        Assert.Equal(1, bottom.Children.Count);
        Assert.NotNull(widgetsRoot);
        Assert.Same(widgetsRoot, top.Children[0]);
        Assert.Equal("fct.state-root", bottom.Children[0].Name);
    }

    /// <summary>
    /// A factory layout gets no check load and its modes skip the slot check (Decision 3): the load calls the
    /// factory no time, a mode with an unknown slot name loads with no error, and
    /// <see cref="DefinitionLoadResult.RegisterTo"/> calls the factory one time and throws
    /// <see cref="ArgumentException"/> from <see cref="UiManager.Register(ModeDefinition)"/>.
    /// </summary>
    [Fact]
    public void AFactoryLayoutGetsNoCheckLoadAndItsModesSkipTheSlotCheck()
    {
        int calls = 0;
        Func<PanelContext, Element> layoutFactory = _ =>
        {
            calls++;
            return BuildColumnRoot();
        };

        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);
        DefinitionLoader loader = new(new TemplateLoader());
        loader.AddTemplate("fct.hud", HudPanelText);
        loader.AddFactory("fct.CreateLayout", layoutFactory);

        DefinitionLoadResult result = loader.Load(BadSlotText);
        Assert.True(result.Succeeded);
        Assert.Equal(0, calls);

        ArgumentException slotFault = Assert.Throws<ArgumentException>(() => result.RegisterTo(ui));
        Assert.Equal("mode", slotFault.ParamName);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// <see cref="DefinitionLoader.AddTemplate"/> and <see cref="DefinitionLoader.AddFactory"/> share one name
    /// table (Decision 1): a second add of one name throws <see cref="ArgumentException"/> with
    /// <see cref="ArgumentException.ParamName"/> <c>name</c> in each order, and the first entry stays: the loaded
    /// panel uses the first text or the first factory.
    /// </summary>
    [Fact]
    public void AddTemplateAndAddFactoryShareOneNameTable()
    {
        Func<PanelContext, Element> altFactory = static _ => new Element().Name("fct.factory-root");

        DefinitionLoader textFirst = new(new TemplateLoader());
        textFirst.AddTemplate("fct.name", TextFirstPanel);
        ArgumentException afterText = Assert.Throws<ArgumentException>(
            () => textFirst.AddFactory("fct.name", altFactory));
        Assert.Equal("name", afterText.ParamName);

        DefinitionLoadResult textKept = textFirst.Load(OnePanelText);
        Assert.True(textKept.Succeeded);
        Assert.NotSame(altFactory, textKept.Panels[0].CreateRoot);
        Assert.Equal("fct.text-root", textKept.Panels[0].CreateRoot(CreatePanels()).Name);

        Func<PanelContext, Element> firstFactory = static _ => new Element().Name("fct.factory-root");
        DefinitionLoader factoryFirst = new(new TemplateLoader());
        factoryFirst.AddFactory("fct.name", firstFactory);
        ArgumentException afterFactory = Assert.Throws<ArgumentException>(
            () => factoryFirst.AddTemplate("fct.name", TextFirstPanel));
        Assert.Equal("name", afterFactory.ParamName);

        DefinitionLoadResult factoryKept = factoryFirst.Load(OnePanelText);
        Assert.True(factoryKept.Succeeded);
        Assert.Same(firstFactory, factoryKept.Panels[0].CreateRoot);
    }

    /// <summary>
    /// <see cref="DefinitionLoader.AddFactory"/> checks its arguments: a <see langword="null"/> name throws
    /// <see cref="ArgumentNullException"/> (<c>name</c>), a <see langword="null"/> factory throws
    /// <see cref="ArgumentNullException"/> (<c>factory</c>), and an empty name throws
    /// <see cref="ArgumentException"/> (<c>name</c>).
    /// </summary>
    [Fact]
    public void AddFactoryChecksItsArguments()
    {
        DefinitionLoader loader = new(new TemplateLoader());
        Func<PanelContext, Element> factory = static _ => new Element();

        ArgumentNullException nullName = Assert.Throws<ArgumentNullException>(
            () => loader.AddFactory(null!, factory));
        Assert.Equal("name", nullName.ParamName);

        ArgumentNullException nullFactory = Assert.Throws<ArgumentNullException>(
            () => loader.AddFactory("fct.any", null!));
        Assert.Equal("factory", nullFactory.ParamName);

        ArgumentException emptyName = Assert.Throws<ArgumentException>(
            () => loader.AddFactory(string.Empty, factory));
        Assert.Equal("name", emptyName.ParamName);
    }

    /// <summary>
    /// <see cref="DefinitionLoadResult.RegisterTo"/> gives the panel controllers in the order of the text: a list
    /// of two, each <see cref="PanelController.Definition"/> is the same object as its record, and
    /// <see cref="PanelController.EnsureRoot"/> gives the root of each template.
    /// </summary>
    [Fact]
    public void RegisterToGivesThePanelControllersInTextOrder()
    {
        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);
        DefinitionLoader loader = new(new TemplateLoader());
        loader.AddTemplate("fct.t1", """<ui:Element name="fct.r1" xmlns:ui="brutal-ui" />"""u8);
        loader.AddTemplate("fct.t2", """<ui:Element name="fct.r2" xmlns:ui="brutal-ui" />"""u8);

        DefinitionLoadResult result = loader.Load(TwoPanelsText);
        Assert.True(result.Succeeded);

        IReadOnlyList<PanelController> controllers = result.RegisterTo(ui);
        Assert.Equal(2, controllers.Count);
        Assert.Same(result.Panels[0], controllers[0].Definition);
        Assert.Same(result.Panels[1], controllers[1].Definition);
        Assert.Equal("fct.r1", controllers[0].EnsureRoot().Name);
        Assert.Equal("fct.r2", controllers[1].EnsureRoot().Name);
    }

    /// <summary>Builds the factory layout root: a root that is not pickable, with the slots <c>top</c> and
    /// <c>bottom</c>. A factory layout has no slot list at load time, so the built root answers the slot check of
    /// <see cref="UiManager.Register(ModeDefinition)"/>.</summary>
    /// <returns>The detached root.</returns>
    private static Element BuildColumnRoot()
    {
        return new Element()
            .Pickable(false)
            .Child(new SlotContainer("top"u8))
            .Child(new SlotContainer("bottom"u8));
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
