using System;
using System.Globalization;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The template tree and its C# twin: one HUD with six bound elements and one command button.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Load"/> reads the template text of <see cref="Template"/> through a <see cref="TemplateBindings"/> set.
/// <see cref="BuildCode"/> builds the same tree with the builder methods and the C# bind methods. The two forms hold
/// the same element names, the same create values as the factories of <c>BuiltInElements.cs</c>, and the same bind
/// methods, so <c>TemplateParityTests</c> can compare them and <c>TemplateBindingAllocationTests</c> can measure the
/// loaded form.
/// </para>
/// <para>
/// The keys: <c>hud.score</c> is a number key with the format <c>N0</c> and the invariant culture, <c>hud.volume-text</c>
/// is a number key with the format <c>F1</c> and the invariant culture, <c>hud.name</c> is one string key for a label
/// and a text field, <c>hud.muted</c> is a bool key, and <c>hud.volume</c> is a float key. <c>game.pause</c> goes to the
/// <see cref="CommandRegistry"/> of the caller.
/// </para>
/// <para>
/// The state comes from <see cref="BoundTree.Next(int)"/>, so each text keeps its width from round to round and no
/// buffer grows after the warm-up of the allocation gate.
/// </para>
/// </remarks>
internal static class BoundTemplate
{
    /// <summary>Gets the template text of the bound HUD.</summary>
    internal static ReadOnlySpan<byte> Template => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="score" class="hud-value" text="0" bind-text="hud.score" />
          <ui:Label name="volume" class="hud-value" text="0" bind-text="hud.volume-text" />
          <ui:Label name="player" text="0" bind-text="hud.name" />
          <ui:TextField name="name-field" capacity="16" text="0" bind-text="hud.name" />
          <ui:Toggle name="mute" bind-checked="hud.muted" />
          <ui:Slider name="volume-slider" max="100" bind-value="hud.volume" />
          <ui:Button name="pause" class="hud-button" text="Pause" command="game.pause" />
        </ui:Element>
        """u8;

    /// <summary>Loads the template through a new <see cref="TemplateBindings"/> set and a new loader.</summary>
    /// <param name="store">The store of the HUD state. The method registers one key per slice it binds.</param>
    /// <param name="commands">The registry that holds <c>game.pause</c>.</param>
    /// <returns>The loaded root.</returns>
    internal static Element Load(Store<BoundTree.HudState> store, CommandRegistry commands)
    {
        TemplateBindings bindings = new(commands);
        AddKeys(bindings, store);

        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        return loader.Load(Template, "hud.xml").GetRootOrThrow();
    }

    /// <summary>Builds the same tree in C# with the builder methods and the C# bind methods.</summary>
    /// <param name="store">The store of the HUD state.</param>
    /// <param name="commands">The registry that holds <c>game.pause</c>.</param>
    /// <returns>The root of the twin.</returns>
    internal static Element BuildCode(Store<BoundTree.HudState> store, CommandRegistry commands)
    {
        return new Element()
            .Class("hud")
            .Child(
                new Label(new Label.CreateInfo { Text = "0" })
                    .Name("score")
                    .Class("hud-value")
                    .BindText(store, static s => s.Score, "N0", CultureInfo.InvariantCulture))
            .Child(
                new Label(new Label.CreateInfo { Text = "0" })
                    .Name("volume")
                    .Class("hud-value")
                    .BindText(store, static s => s.Volume, "F1", CultureInfo.InvariantCulture))
            .Child(
                new Label(new Label.CreateInfo { Text = "0" })
                    .Name("player")
                    .BindText(store, static s => s.Name))
            .Child(
                new TextField(new TextField.CreateInfo { Capacity = 16, Text = "0" })
                    .Name("name-field")
                    .BindText(store, static s => s.Name))
            .Child(
                new Toggle(default)
                    .Name("mute")
                    .BindChecked(store, static s => s.Muted))
            .Child(
                new Slider(new Slider.CreateInfo { Maximum = 100f })
                    .Name("volume-slider")
                    .BindValue(store, static s => s.Volume))
            .Child(
                new Button(new Button.CreateInfo { Text = "Pause" })
                    .Name("pause")
                    .Class("hud-button")
                    .BindCommand(commands, "game.pause"u8));
    }

    /// <summary>Registers the five keys of the template on one bindings set.</summary>
    /// <param name="bindings">The bindings set of a load.</param>
    /// <param name="store">The store of the HUD state.</param>
    private static void AddKeys(TemplateBindings bindings, Store<BoundTree.HudState> store)
    {
        bindings.AddText("hud.score", store, static s => s.Score, "N0", CultureInfo.InvariantCulture);
        bindings.AddText("hud.volume-text", store, static s => s.Volume, "F1", CultureInfo.InvariantCulture);
        bindings.AddText("hud.name", store, static s => s.Name);
        bindings.AddChecked("hud.muted", store, static s => s.Muted);
        bindings.AddValue("hud.volume", store, static s => s.Volume);
    }
}
