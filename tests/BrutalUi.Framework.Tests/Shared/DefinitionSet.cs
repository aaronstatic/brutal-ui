using System;
using BrutalUi.Core;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The shared definition set: two layouts, four panels, and three modes, as XML texts and as C# records.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Load"/> reads the texts through a <see cref="DefinitionLoader"/> and registers the records on one
/// manager. <see cref="RegisterCode"/> builds the same records in C# and registers them on another manager. Each
/// panel holds one named element with one label and one button, so the loaded tree and the C# tree compare equal
/// under <c>TreeCompare.FirstDifference</c>. <c>DefinitionParityTests</c> runs the same steps on the two managers.
/// </para>
/// <para>
/// The names start with <c>set.</c>. The layout <c>set.column</c> holds the slots <c>top</c> and <c>bottom</c>, and
/// the layout <c>set.overlay</c> holds the slot <c>center</c>. The modes <c>set.main</c> and <c>set.detail</c> name
/// <c>set.column</c> and the map <c>set.map</c>. The mode <c>set.overlay</c> has no <c>actions</c> attribute, so its
/// record holds no map, and one direct <c>PanelRef</c> places <c>set.p4</c> in the default slot of the panel (Q5).
/// A direct <c>PanelRef</c> to <c>set.p3</c> sits under <c>set.main</c>.
/// </para>
/// </remarks>
internal static class DefinitionSet
{
    /// <summary>Gets the definitions text: four panels, two layouts, and three modes.</summary>
    internal static ReadOnlySpan<byte> DefinitionsText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="set.p1" slot="top" template="set.p1" />
          <ui:Panel name="set.p2" slot="bottom" template="set.p2" />
          <ui:Panel name="set.p3" slot="bottom" template="set.p3" />
          <ui:Panel name="set.p4" slot="center" template="set.p4" />
          <ui:Layout name="set.column" template="set.column" />
          <ui:Layout name="set.overlay" template="set.overlay" />
          <ui:Mode name="set.main" layout="set.column" actions="set.map">
            <ui:Slot name="top">
              <ui:PanelRef name="set.p1" />
            </ui:Slot>
            <ui:PanelRef name="set.p3" />
          </ui:Mode>
          <ui:Mode name="set.detail" layout="set.column" actions="set.map">
            <ui:Slot name="bottom">
              <ui:PanelRef name="set.p2" />
            </ui:Slot>
            <ui:Slot name="top">
              <ui:PanelRef name="set.p1" />
            </ui:Slot>
          </ui:Mode>
          <ui:Mode name="set.overlay" layout="set.overlay">
            <ui:PanelRef name="set.p4" />
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the template of the panel <c>set.p1</c>.</summary>
    internal static ReadOnlySpan<byte> Panel1Text => """
        <ui:Element name="set.p1Root" xmlns:ui="brutal-ui">
          <ui:Label text="L1" />
          <ui:Button text="B1" />
        </ui:Element>
        """u8;

    /// <summary>Gets the template of the panel <c>set.p2</c>.</summary>
    internal static ReadOnlySpan<byte> Panel2Text => """
        <ui:Element name="set.p2Root" xmlns:ui="brutal-ui">
          <ui:Label text="L2" />
          <ui:Button text="B2" />
        </ui:Element>
        """u8;

    /// <summary>Gets the template of the panel <c>set.p3</c>.</summary>
    internal static ReadOnlySpan<byte> Panel3Text => """
        <ui:Element name="set.p3Root" xmlns:ui="brutal-ui">
          <ui:Label text="L3" />
          <ui:Button text="B3" />
        </ui:Element>
        """u8;

    /// <summary>Gets the template of the panel <c>set.p4</c>.</summary>
    internal static ReadOnlySpan<byte> Panel4Text => """
        <ui:Element name="set.p4Root" xmlns:ui="brutal-ui">
          <ui:Label text="L4" />
          <ui:Button text="B4" />
        </ui:Element>
        """u8;

    /// <summary>Gets the layout template with the slots <c>top</c> and <c>bottom</c>.</summary>
    internal static ReadOnlySpan<byte> ColumnText => """
        <ui:Element pickable="false" xmlns:ui="brutal-ui">
          <ui:SlotContainer slot="top" />
          <ui:SlotContainer slot="bottom" />
        </ui:Element>
        """u8;

    /// <summary>Gets the layout template with the slot <c>center</c>.</summary>
    internal static ReadOnlySpan<byte> OverlayText => """
        <ui:Element pickable="false" xmlns:ui="brutal-ui">
          <ui:SlotContainer slot="center" />
        </ui:Element>
        """u8;

    /// <summary>
    /// Loads the set and registers the records on one manager. Each call makes its own
    /// <see cref="TemplateLoader"/> and <see cref="DefinitionLoader"/>, so each call gets a fresh registry.
    /// </summary>
    /// <param name="ui">The manager that takes the records.</param>
    /// <param name="map">The action map that the names <c>set.map</c> and that the two mapped modes name.</param>
    /// <returns>The records, in the order of the text.</returns>
    internal static Records Load(UiManager ui, UiActionMap map)
    {
        TemplateLoader templates = new();
        DefinitionLoader loader = new(templates);
        loader.AddTemplate("set.p1", Panel1Text);
        loader.AddTemplate("set.p2", Panel2Text);
        loader.AddTemplate("set.p3", Panel3Text);
        loader.AddTemplate("set.p4", Panel4Text);
        loader.AddTemplate("set.column", ColumnText);
        loader.AddTemplate("set.overlay", OverlayText);
        loader.AddActionMap("set.map", map);

        DefinitionLoadResult result = loader.Load(DefinitionsText);
        Assert.True(result.Succeeded);
        result.RegisterTo(ui);
        return new Records([.. result.Panels], [.. result.Layouts], [.. result.Modes]);
    }

    /// <summary>Registers the same records, built in C#, on one manager. Each call builds its own roots.</summary>
    /// <param name="ui">The manager that takes the records.</param>
    /// <param name="map">The action map that the two mapped modes name.</param>
    /// <returns>The records, in the order of the definitions text of <see cref="Load"/>.</returns>
    internal static Records RegisterCode(UiManager ui, UiActionMap map)
    {
        PanelDefinition[] panels =
        [
            new PanelDefinition("set.p1"u8, "top"u8, static _ => BuildPanel1Root()),
            new PanelDefinition("set.p2"u8, "bottom"u8, static _ => BuildPanel2Root()),
            new PanelDefinition("set.p3"u8, "bottom"u8, static _ => BuildPanel3Root()),
            new PanelDefinition("set.p4"u8, "center"u8, static _ => BuildPanel4Root()),
        ];
        LayoutDefinition[] layouts =
        [
            new LayoutDefinition("set.column"u8, static _ => BuildColumnRoot()),
            new LayoutDefinition("set.overlay"u8, static _ => BuildOverlayRoot()),
        ];
        ModeDefinition[] modes =
        [
            new ModeDefinition(
                "set.main"u8,
                "set.column"u8,
                [new ModePanel("top"u8, "set.p1"u8), new ModePanel("bottom"u8, "set.p3"u8)],
                map),
            new ModeDefinition(
                "set.detail"u8,
                "set.column"u8,
                [new ModePanel("bottom"u8, "set.p2"u8), new ModePanel("top"u8, "set.p1"u8)],
                map),
            new ModeDefinition(
                "set.overlay"u8,
                "set.overlay"u8,
                [new ModePanel("center"u8, "set.p4"u8)]),
        ];

        // Register in the order of DefinitionLoadResult.RegisterTo: panels, then layouts, then modes (Q6).
        foreach (PanelDefinition panel in panels)
        {
            ui.Register(panel);
        }

        foreach (LayoutDefinition layout in layouts)
        {
            ui.Register(layout);
        }

        foreach (ModeDefinition mode in modes)
        {
            ui.Register(mode);
        }

        return new Records(panels, layouts, modes);
    }

    /// <summary>Builds the root of the panel <c>set.p1</c>: one named element with one label and one button.</summary>
    /// <returns>The detached root.</returns>
    private static Element BuildPanel1Root() => BuildPanelRoot("set.p1Root", "L1", "B1");

    /// <summary>Builds the root of the panel <c>set.p2</c>.</summary>
    /// <returns>The detached root.</returns>
    private static Element BuildPanel2Root() => BuildPanelRoot("set.p2Root", "L2", "B2");

    /// <summary>Builds the root of the panel <c>set.p3</c>.</summary>
    /// <returns>The detached root.</returns>
    private static Element BuildPanel3Root() => BuildPanelRoot("set.p3Root", "L3", "B3");

    /// <summary>Builds the root of the panel <c>set.p4</c>.</summary>
    /// <returns>The detached root.</returns>
    private static Element BuildPanel4Root() => BuildPanelRoot("set.p4Root", "L4", "B4");

    /// <summary>Builds one panel root with the builder methods and the create values of <c>BuiltInElements.cs</c>.</summary>
    /// <param name="name">The name of the root.</param>
    /// <param name="label">The text of the label.</param>
    /// <param name="button">The text of the button.</param>
    /// <returns>The detached root.</returns>
    private static Element BuildPanelRoot(string name, string label, string button)
    {
        return new Element()
            .Name(name)
            .Child(new Label(new Label.CreateInfo { Text = label }))
            .Child(new Button(new Button.CreateInfo { Text = button }));
    }

    /// <summary>Builds the column layout root: a root that is not pickable, with the slots <c>top</c> and
    /// <c>bottom</c>.</summary>
    /// <returns>The detached root.</returns>
    private static Element BuildColumnRoot()
    {
        return new Element()
            .Pickable(false)
            .Child(new SlotContainer("top"u8))
            .Child(new SlotContainer("bottom"u8));
    }

    /// <summary>Builds the overlay layout root: a root that is not pickable, with the slot <c>center</c>.</summary>
    /// <returns>The detached root.</returns>
    private static Element BuildOverlayRoot()
    {
        return new Element()
            .Pickable(false)
            .Child(new SlotContainer("center"u8));
    }

    /// <summary>The three record lists of one registration, in the order of the text.</summary>
    internal sealed class Records
    {
        /// <summary>Creates the record set.</summary>
        /// <param name="panels">The panel records.</param>
        /// <param name="layouts">The layout records.</param>
        /// <param name="modes">The mode records.</param>
        internal Records(PanelDefinition[] panels, LayoutDefinition[] layouts, ModeDefinition[] modes)
        {
            Panels = panels;
            Layouts = layouts;
            Modes = modes;
        }

        /// <summary>Gets the panel records.</summary>
        internal PanelDefinition[] Panels { get; }

        /// <summary>Gets the layout records.</summary>
        internal LayoutDefinition[] Layouts { get; }

        /// <summary>Gets the mode records.</summary>
        internal ModeDefinition[] Modes { get; }
    }
}
