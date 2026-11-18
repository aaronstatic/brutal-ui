using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The rules of the object: the argument checks of the methods, the throw before a duplicate add, the one loader for
/// each template loader, and the checks of <see cref="DefinitionLoadResult.RegisterTo"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each text is a <c>u8</c> literal, because <see cref="DefinitionLoader.Load"/> takes a span. One
/// <see cref="DefinitionLoader"/> serves one <see cref="TemplateLoader"/>, so each test makes its own pair. The
/// names of the records start with <c>api.</c>, so each text reads as one feature sample.
/// </para>
/// </remarks>
public sealed class DefinitionLoaderApiTests
{
    /// <summary>Gets the definitions text of the tests: one panel, one layout, and one mode with a map.</summary>
    private static ReadOnlySpan<byte> DefinitionsText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="api.hud" slot="top" template="api.panel" />
          <ui:Layout name="api.game" template="api.layout" />
          <ui:Mode name="api.play" layout="api.game" actions="api.map">
            <ui:Slot name="top">
              <ui:PanelRef name="api.hud" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets a failed definitions text: the panel names an unknown template, and the mode names an unknown
    /// map.</summary>
    private static ReadOnlySpan<byte> FailedText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="api.hud" slot="top" template="api.nope" />
          <ui:Layout name="api.game" template="api.layout" />
          <ui:Mode name="api.play" layout="api.game" actions="api.nope" />
        </ui:Definitions>
        """u8;

    /// <summary>Gets the panel template with the element name <c>api.aaaa</c>: the first add text, kept as a
    /// changeable source array.</summary>
    private static ReadOnlySpan<byte> FirstSource => """<ui:Element name="api.aaaa" xmlns:ui="brutal-ui" />"""u8;

    /// <summary>Gets a panel template of the same length with the element name <c>api.bbbb</c>: the mutation of the
    /// source array after the add.</summary>
    private static ReadOnlySpan<byte> MutatedSource => """<ui:Element name="api.bbbb" xmlns:ui="brutal-ui" />"""u8;

    /// <summary>Gets a panel template with the element name <c>api.cccc</c>: the rejected second add.</summary>
    private static ReadOnlySpan<byte> SecondTemplate => """<ui:Element name="api.cccc" xmlns:ui="brutal-ui" />"""u8;

    /// <summary>Gets a plain panel template: one element with no child.</summary>
    private static ReadOnlySpan<byte> PanelTemplate => """<ui:Element name="api.root" xmlns:ui="brutal-ui" />"""u8;

    /// <summary>Gets the layout template: a root that is not pickable, with one slot named <c>top</c>.</summary>
    private static ReadOnlySpan<byte> LayoutTemplate => """
        <ui:Element pickable="false" xmlns:ui="brutal-ui">
          <ui:SlotContainer slot="top" />
        </ui:Element>
        """u8;

    /// <summary>
    /// The methods check their arguments: a <see langword="null"/> loader, a <see langword="null"/> or empty name, a
    /// <see langword="null"/> map, a <see langword="null"/> source name, and a <see langword="null"/> manager each
    /// throw with the parameter name.
    /// </summary>
    [Fact]
    public void TheMethodsCheckTheirArguments()
    {
        ArgumentNullException nullLoader = Assert.Throws<ArgumentNullException>(() => new DefinitionLoader(null!));
        Assert.Equal("templates", nullLoader.ParamName);

        DefinitionLoader loader = new(new TemplateLoader());
        ArgumentNullException nullName = Assert.Throws<ArgumentNullException>(() => loader.AddTemplate(null!, PanelTemplate));
        Assert.Equal("name", nullName.ParamName);
        nullName = Assert.Throws<ArgumentNullException>(() => loader.AddActionMap(null!, CreateMap()));
        Assert.Equal("name", nullName.ParamName);
        ArgumentNullException nullMap = Assert.Throws<ArgumentNullException>(() => loader.AddActionMap("api.map", null!));
        Assert.Equal("map", nullMap.ParamName);

        ArgumentException emptyName = Assert.Throws<ArgumentException>(() => loader.AddTemplate(string.Empty, PanelTemplate));
        Assert.Equal("name", emptyName.ParamName);
        emptyName = Assert.Throws<ArgumentException>(() => loader.AddActionMap(string.Empty, CreateMap()));
        Assert.Equal("name", emptyName.ParamName);

        ArgumentNullException nullSource = Assert.Throws<ArgumentNullException>(() => loader.Load(DefinitionsText, null!));
        Assert.Equal("sourceName", nullSource.ParamName);

        loader.AddTemplate("api.panel", PanelTemplate);
        loader.AddTemplate("api.layout", LayoutTemplate);
        loader.AddActionMap("api.map", CreateMap());
        DefinitionLoadResult result = loader.Load(DefinitionsText);
        ArgumentNullException nullManager = Assert.Throws<ArgumentNullException>(() => result.RegisterTo(null!));
        Assert.Equal("ui", nullManager.ParamName);
    }

    /// <summary>
    /// A second <see cref="DefinitionLoader.AddTemplate"/> and a second
    /// <see cref="DefinitionLoader.AddActionMap"/> for one name throw <see cref="ArgumentException"/> before the
    /// add, and the first value stays. A change of the source array after the add changes nothing, because
    /// <see cref="DefinitionLoader.AddTemplate"/> copies the text.
    /// </summary>
    [Fact]
    public void ASecondAddForOneNameThrows()
    {
        DefinitionLoader loader = new(new TemplateLoader());
        byte[] source = FirstSource.ToArray();
        loader.AddTemplate("api.panel", source);
        ArgumentException duplicate = Assert.Throws<ArgumentException>(() => loader.AddTemplate("api.panel", SecondTemplate));
        Assert.Equal("name", duplicate.ParamName);

        MutatedSource.CopyTo(source);

        UiActionMap firstMap = CreateMap();
        loader.AddActionMap("api.map", firstMap);
        Assert.Throws<ArgumentException>(() => loader.AddActionMap("api.map", CreateMap()));

        loader.AddTemplate("api.layout", LayoutTemplate);
        DefinitionLoadResult result = loader.Load(DefinitionsText);

        Assert.True(result.Succeeded);
        Element root = result.Panels[0].CreateRoot(CreatePanels());
        Assert.Equal("api.aaaa", root.Name);
        Assert.Same(firstMap, result.Modes[0].ActionMap);
    }

    /// <summary>
    /// One <see cref="DefinitionLoader"/> serves one <see cref="TemplateLoader"/>: the second constructor throws
    /// <see cref="ArgumentException"/> from <see cref="TemplateLoader.Register(in ElementTypeInfo)"/>, because the
    /// loader holds the <c>SlotContainer</c> name already.
    /// </summary>
    [Fact]
    public void ASecondDefinitionLoaderOnOneTemplateLoaderThrows()
    {
        TemplateLoader templates = new();
        new DefinitionLoader(templates);

        Assert.Throws<ArgumentException>(() => new DefinitionLoader(templates));
    }

    /// <summary>
    /// <see cref="DefinitionLoadResult.RegisterTo"/> checks the result and the manager: a failed result throws
    /// <see cref="InvalidOperationException"/> with each error line and registers nothing, so a later
    /// <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/> of its mode name throws
    /// <see cref="ArgumentException"/>. A manager that holds the panel name throws
    /// <see cref="ArgumentException"/> in the call (A6).
    /// </summary>
    [Fact]
    public void RegisterToChecksTheResultAndTheManager()
    {
        UiContext context = CreateContext();
        UiManager ui = CreateManager(context);
        DefinitionLoader loader = new(new TemplateLoader());
        loader.AddTemplate("api.layout", LayoutTemplate);

        DefinitionLoadResult failed = loader.Load(FailedText);
        Assert.False(failed.Succeeded);
        Assert.Equal(2, failed.Errors.Count);
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => failed.RegisterTo(ui));
        foreach (DefinitionError error in failed.Errors)
        {
            Assert.Contains(error.ToString(), failure.Message, StringComparison.Ordinal);
        }

        ArgumentException unknownMode = Assert.Throws<ArgumentException>(() => ui.SetMode("api.play"u8));
        Assert.Equal("name", unknownMode.ParamName);

        loader.AddTemplate("api.panel", PanelTemplate);
        loader.AddActionMap("api.map", CreateMap());
        DefinitionLoadResult good = loader.Load(DefinitionsText);
        Assert.True(good.Succeeded);
        good.RegisterTo(ui);
        ArgumentException clash = Assert.Throws<ArgumentException>(() => good.RegisterTo(ui));
        Assert.Equal("panel", clash.ParamName);
    }

    /// <summary>Creates an action map with no action.</summary>
    /// <returns>The map.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
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
