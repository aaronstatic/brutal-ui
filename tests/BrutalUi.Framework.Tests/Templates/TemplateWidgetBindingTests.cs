using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The phase 2 widget bindings: <c>bind-text</c> on a <see cref="Label"/> and a <see cref="TextField"/> with one
/// string key, <c>bind-checked</c> on a <see cref="Toggle"/>, and <c>bind-value</c> on a <see cref="Slider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each test loads a small template through a <see cref="TemplateBindings"/> object, runs one update, and writes the
/// store. The templates are <c>u8</c> literals, because the BRUTAL analyzer rejects a string literal in
/// <see cref="TemplateLoader.Load(string, string)"/>.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a default font size of 16, so a
/// loaded tree lays out at a real text size. A store change writes each bound widget through the code setter, so no
/// widget slot runs.
/// </para>
/// </remarks>
public sealed class TemplateWidgetBindingTests
{
    /// <summary>The template with one label bound to a string key.</summary>
    private static ReadOnlySpan<byte> LabelTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="0" bind-text="hud.name" />
        </ui:Element>
        """u8;

    /// <summary>The template with one label and one text field on the same string key.</summary>
    private static ReadOnlySpan<byte> LabelAndFieldTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="0" bind-text="hud.name" />
          <ui:TextField name="name-field" capacity="16" text="0" bind-text="hud.name" />
        </ui:Element>
        """u8;

    /// <summary>The template with one toggle bound to a bool key.</summary>
    private static ReadOnlySpan<byte> ToggleTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Toggle name="mute" bind-checked="hud.muted" />
        </ui:Element>
        """u8;

    /// <summary>The template with one slider bound to a float key.</summary>
    private static ReadOnlySpan<byte> SliderTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Slider name="volume" max="100" bind-value="hud.volume" />
        </ui:Element>
        """u8;

    /// <summary>
    /// A string key binds the text of a label. The label shows the name after the first update, and the new name
    /// after a set.
    /// </summary>
    [Fact]
    public void AStringKeyBindsALabel()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = CreateStore("Ann", 0f, false);
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.name", store, static s => s.Name);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader, LabelTemplate);

        context.Update(0f);
        Label? player = root.Find<Label>("player"u8);
        Assert.NotNull(player);
        Assert.Equal(1, player.BindingCount);
        Assert.True(player.Text.SequenceEqual("Ann"u8));

        BoundTree.HudState state = store.Value with { Name = "Bo" };
        store.Set(in state);
        context.Update(0f);
        Assert.True(player.Text.SequenceEqual("Bo"u8));
    }

    /// <summary>
    /// One string key serves a label and a text field (A5). Both show the name, and each holds one binding.
    /// </summary>
    [Fact]
    public void OneStringKeyServesALabelAndATextField()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = CreateStore("Ann", 0f, false);
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.name", store, static s => s.Name);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader, LabelAndFieldTemplate);

        context.Update(0f);
        Label? player = root.Find<Label>("player"u8);
        Assert.NotNull(player);
        Assert.Equal(1, player.BindingCount);
        Assert.True(player.Text.SequenceEqual("Ann"u8));

        TextField? field = root.Find<TextField>("name-field"u8);
        Assert.NotNull(field);
        Assert.Equal(1, field.BindingCount);
        Assert.True(field.Text.SequenceEqual("Ann"u8));

        BoundTree.HudState state = store.Value with { Name = "Bo" };
        store.Set(in state);
        context.Update(0f);
        Assert.True(player.Text.SequenceEqual("Bo"u8));
        Assert.True(field.Text.SequenceEqual("Bo"u8));
    }

    /// <summary>
    /// A bool key binds the checked state of a toggle. A set flips <see cref="Toggle.Checked"/>, and
    /// <see cref="Toggle.CheckedChanged"/> gets no call.
    /// </summary>
    [Fact]
    public void ACheckedKeyBindsAToggle()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = CreateStore(string.Empty, 0f, false);
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddChecked("hud.muted", store, static s => s.Muted);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader, ToggleTemplate);

        context.Update(0f);
        Toggle? mute = root.Find<Toggle>("mute"u8);
        Assert.NotNull(mute);
        Assert.Equal(1, mute.BindingCount);
        Assert.False(mute.Checked);

        SlotCounter counter = new();
        mute.CheckedChanged = counter.OnFlip;

        BoundTree.HudState state = store.Value with { Muted = true };
        store.Set(in state);
        context.Update(0f);

        Assert.True(mute.Checked);
        Assert.Equal(0, counter.Flips);
    }

    /// <summary>
    /// A float key binds the value of a slider. A set writes <see cref="Slider.Value"/>, and
    /// <see cref="Slider.ValueChanged"/> gets no call.
    /// </summary>
    [Fact]
    public void AValueKeyBindsASlider()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = CreateStore(string.Empty, 10f, false);
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddValue("hud.volume", store, static s => s.Volume);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader, SliderTemplate);

        context.Update(0f);
        Slider? volume = root.Find<Slider>("volume"u8);
        Assert.NotNull(volume);
        Assert.Equal(1, volume.BindingCount);
        Assert.Equal(10f, volume.Value);

        SlotCounter counter = new();
        volume.ValueChanged = counter.OnValue;

        BoundTree.HudState state = store.Value with { Volume = 42f };
        store.Set(in state);
        context.Update(0f);

        Assert.Equal(42f, volume.Value);
        Assert.Equal(0, counter.Values);
    }

    /// <summary>Creates a store of the HUD state with the given name, volume, and muted state.</summary>
    /// <param name="name">The start name.</param>
    /// <param name="volume">The start volume.</param>
    /// <param name="muted">The start muted state.</param>
    /// <returns>The store.</returns>
    private static Store<BoundTree.HudState> CreateStore(string name, float volume, bool muted)
    {
        return new Store<BoundTree.HudState>(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = new BoundTree.HudState(0, 0, volume, muted, name),
        });
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
    /// <see cref="UiContext.CreateInfo.DefaultFontSize"/> of 16 gives the text a size with no stylesheet, so a
    /// loaded leaf gets a real height.
    /// </remarks>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), DefaultFontSize = 16f })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>The target of the widget slots: a counter of calls.</summary>
    private sealed class SlotCounter
    {
        /// <summary>The count of <see cref="Toggle.CheckedChanged"/> calls.</summary>
        public int Flips;

        /// <summary>The count of <see cref="Slider.ValueChanged"/> calls.</summary>
        public int Values;

        /// <summary>Counts one flip.</summary>
        /// <param name="toggle">The toggle that the user flipped.</param>
        public void OnFlip(Toggle toggle) => Flips++;

        /// <summary>Counts one value change.</summary>
        /// <param name="slider">The slider that the user moved.</param>
        public void OnValue(Slider slider) => Values++;
    }
}
