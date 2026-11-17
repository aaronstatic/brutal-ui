using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The rules of the <see cref="TemplateBindings"/> object: one key holds one selector, each method checks its
/// arguments, and a second attach on one loader throws.
/// </summary>
/// <remarks>
/// <para>
/// A check runs before the add, so a rejected call changes nothing: the table keeps the first selector, and the
/// loader keeps the handler of the first attach. The messages are constant, so each test reads the parameter name and
/// one fragment of the text.
/// </para>
/// <para>
/// A handler that the game adds after <see cref="TemplateBindings.AttachTo"/> still takes its own attribute, because
/// the handler of the bindings returns <see langword="false"/> for another name.
/// </para>
/// </remarks>
public sealed class TemplateBindingsApiTests
{
    /// <summary>The template with one label bound to a string key.</summary>
    private static ReadOnlySpan<byte> LabelTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="0" bind-text="hud.name" />
        </ui:Element>
        """u8;

    /// <summary>The template with one bound label and one attribute that the game handler takes.</summary>
    private static ReadOnlySpan<byte> ProbeTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="0" bind-text="hud.name" probe="on" />
        </ui:Element>
        """u8;

    /// <summary>
    /// A second register call for one key throws (A4), also for another method, and the first selector still binds.
    /// </summary>
    [Fact]
    public void ASecondAddForOneKeyThrows()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = CreateStore("Ann");
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.name", store, static s => s.Name);

        ArgumentException sameMethod = Assert.Throws<ArgumentException>(
            () => bindings.AddText("hud.name", store, static s => s.Name));
        ArgumentException numberMethod = Assert.Throws<ArgumentException>(
            () => bindings.AddText("hud.name", store, static s => s.Score));
        ArgumentException otherMethod = Assert.Throws<ArgumentException>(
            () => bindings.AddChecked("hud.name", store, static s => s.Muted));

        Assert.Equal("key", sameMethod.ParamName);
        Assert.Equal("key", numberMethod.ParamName);
        Assert.Equal("key", otherMethod.ParamName);
        Assert.Contains("One binding key holds one selector.", sameMethod.Message, StringComparison.Ordinal);
        Assert.Contains("One binding key holds one selector.", otherMethod.Message, StringComparison.Ordinal);

        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader, LabelTemplate);
        context.Update(0f);

        Label? player = root.Find<Label>("player"u8);
        Assert.NotNull(player);
        Assert.Equal(1, player.BindingCount);
        Assert.True(player.Text.SequenceEqual("Ann"u8));
    }

    /// <summary>
    /// A <see langword="null"/> registry, loader, key, store, or selector throws
    /// <see cref="ArgumentNullException"/> with the parameter name, and an empty key throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void TheMethodsCheckTheirArguments()
    {
        Store<BoundTree.HudState> store = CreateStore("Ann");
        TemplateBindings bindings = new(new CommandRegistry());

        ArgumentNullException nullRegistry = Assert.Throws<ArgumentNullException>(
            () => new TemplateBindings(null!));
        Assert.Equal("commands", nullRegistry.ParamName);

        ArgumentNullException nullLoader = Assert.Throws<ArgumentNullException>(
            () => bindings.AttachTo(null!));
        Assert.Equal("loader", nullLoader.ParamName);

        ArgumentNullException nullKey = Assert.Throws<ArgumentNullException>(
            () => bindings.AddText<BoundTree.HudState>(null!, store, static s => s.Name));
        Assert.Equal("key", nullKey.ParamName);

        ArgumentNullException nullStore = Assert.Throws<ArgumentNullException>(
            () => bindings.AddText<BoundTree.HudState>("hud.name", null!, static s => s.Name));
        Assert.Equal("store", nullStore.ParamName);

        ArgumentNullException nullSelector = Assert.Throws<ArgumentNullException>(
            () => bindings.AddText<BoundTree.HudState>("hud.name", store, null!));
        Assert.Equal("selector", nullSelector.ParamName);

        ArgumentNullException nullNumberSelector = Assert.Throws<ArgumentNullException>(
            () => bindings.AddText<BoundTree.HudState, int>("hud.score", store, null!));
        Assert.Equal("selector", nullNumberSelector.ParamName);

        ArgumentNullException nullCheckedKey = Assert.Throws<ArgumentNullException>(
            () => bindings.AddChecked(null!, store, static s => s.Muted));
        Assert.Equal("key", nullCheckedKey.ParamName);

        ArgumentNullException nullValueStore = Assert.Throws<ArgumentNullException>(
            () => bindings.AddValue<BoundTree.HudState>("hud.volume", null!, static s => s.Volume));
        Assert.Equal("store", nullValueStore.ParamName);

        ArgumentException emptyText = Assert.Throws<ArgumentException>(
            () => bindings.AddText<BoundTree.HudState>(string.Empty, store, static s => s.Name));
        Assert.Equal("key", emptyText.ParamName);
        Assert.Contains("A binding key must not be empty.", emptyText.Message, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => bindings.AddText(string.Empty, store, static s => s.Score));
        Assert.Throws<ArgumentException>(() => bindings.AddChecked(string.Empty, store, static s => s.Muted));
        Assert.Throws<ArgumentException>(() => bindings.AddValue(string.Empty, store, static s => s.Volume));
    }

    /// <summary>
    /// A second <see cref="TemplateBindings.AttachTo"/> on one loader throws and adds no handler, so a later load still
    /// binds the label one time.
    /// </summary>
    [Fact]
    public void ASecondAttachToOnOneLoaderThrows()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = CreateStore("Ann");
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.name", store, static s => s.Name);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);

        ArgumentException second = Assert.Throws<ArgumentException>(() => bindings.AttachTo(loader));
        Assert.Equal("loader", second.ParamName);
        Assert.Contains("One TemplateBindings object attaches to one loader one time.", second.Message,
            StringComparison.Ordinal);

        Element root = AddLoadedRoot(context, loader, LabelTemplate);
        context.Update(0f);

        Label? player = root.Find<Label>("player"u8);
        Assert.NotNull(player);
        Assert.Equal(1, player.BindingCount);
        Assert.True(player.Text.SequenceEqual("Ann"u8));

        // One object can attach to more than one loader.
        TemplateLoader other = new();
        bindings.AttachTo(other);
        Assert.NotNull(other.Load(LabelTemplate, "hud.xml").GetRootOrThrow());
    }

    /// <summary>
    /// A game handler that the game adds after <see cref="TemplateBindings.AttachTo"/> takes its own attribute, and the
    /// load still binds the label and reports no error.
    /// </summary>
    [Fact]
    public void TheHandlerPassesAnotherAttributeToTheNextHandler()
    {
        Store<BoundTree.HudState> store = CreateStore("Ann");
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.name", store, static s => s.Name);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);

        List<string> probes = new();
        loader.AddAttributeHandler(Probe);

        TemplateLoadResult result = loader.Load(ProbeTemplate, "hud.xml");
        Assert.Empty(result.Errors);
        Element root = result.GetRootOrThrow();

        Assert.Single(probes);
        Assert.Equal("on", probes[0]);
        Label? player = root.Find<Label>("player"u8);
        Assert.NotNull(player);
        Assert.Equal(1, player.BindingCount);

        // Takes one attribute by its name, and returns false for every other name.
        bool Probe(Element element, string name, string value, AttributeContext context)
        {
            if (name != "probe")
            {
                return false;
            }

            probes.Add(value);
            return true;
        }
    }

    /// <summary>Loads a template and adds its root, sized to the viewport, to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="loader">The loader with the attached bindings.</param>
    /// <param name="template">The template text.</param>
    /// <returns>The loaded root.</returns>
    private static Element AddLoadedRoot(UiContext context, TemplateLoader loader, ReadOnlySpan<byte> template)
    {
        Element root = loader.Load(template, "hud.xml").GetRootOrThrow();
        root.Inline.Width = 400f;
        root.Inline.Height = 300f;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Creates a context with a measurer, a default font size of 16, and a viewport of 400 by 300.</summary>
    /// <remarks>
    /// <see cref="UiContext.CreateInfo.DefaultFontSize"/> of 16 gives the text a size with no stylesheet, so a loaded
    /// leaf gets a real height.
    /// </remarks>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), DefaultFontSize = 16f })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>Creates a store of the HUD state with the given name.</summary>
    /// <param name="name">The start name.</param>
    /// <returns>The store.</returns>
    private static Store<BoundTree.HudState> CreateStore(string name)
    {
        return new Store<BoundTree.HudState>(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = new BoundTree.HudState(0, 0, 0f, false, name),
        });
    }
}
