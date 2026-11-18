using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The loaded case of the eighteenth allocation gate: the same toggle step on loaded records allocates 0 bytes
/// after warm-up (A9).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BuildLoadedScene"/> loads the shape of <see cref="BuildScene"/> from XML through
/// <see cref="DefinitionLoader"/>. The templates <c>mda.layout</c>, <c>mda.widgets</c>, <c>mda.state</c>, and
/// <c>mda.info</c> build the same trees as their C# twins: the same elements, the same texts, the same create
/// values, and the same commands. A <see cref="TemplateBindings"/> set gives the loader the commands
/// <c>mda.mode</c> and <c>mda.add</c> and the key <c>mda.score</c> on <c>scene.Score</c> with <c>s_score</c>. The
/// action map <c>mda.map</c> holds the cancel action on Escape, as in the C# scene.
/// </para>
/// <para>
/// A template holds no style and no inline size, so the loaded trees differ from the C# trees in size only.
/// <c>TreeCompare</c> reads no size, and the measured body reads no size. The map, the store, and both mode
/// records come from the load, so the step body runs the diff, the binding, the map switch, and the draw on loaded
/// records.
/// </para>
/// </remarks>
public sealed partial class ModeDiffAllocationTests
{
    /// <summary>Gets the definitions text: three panels, one layout, and two modes on the map <c>mda.map</c>.</summary>
    private static ReadOnlySpan<byte> LoadedDefinitionsText => """
        <ui:Definitions xmlns:ui="brutal-ui">
          <ui:Panel name="mda.widgets" slot="top" template="mda.widgets" />
          <ui:Panel name="mda.state" slot="bottom" template="mda.state" />
          <ui:Panel name="mda.info" slot="bottom" template="mda.info" />
          <ui:Layout name="mda.layout" template="mda.layout" />
          <ui:Mode name="mda.main" layout="mda.layout" actions="mda.map">
            <ui:Slot name="top">
              <ui:PanelRef name="mda.widgets" />
            </ui:Slot>
            <ui:Slot name="bottom">
              <ui:PanelRef name="mda.state" />
            </ui:Slot>
          </ui:Mode>
          <ui:Mode name="mda.detail" layout="mda.layout" actions="mda.map">
            <ui:Slot name="top">
              <ui:PanelRef name="mda.widgets" />
            </ui:Slot>
            <ui:Slot name="bottom">
              <ui:PanelRef name="mda.info" />
            </ui:Slot>
          </ui:Mode>
        </ui:Definitions>
        """u8;

    /// <summary>Gets the layout template: a root that is not pickable, with the slots <c>top</c> and <c>bottom</c>.</summary>
    private static ReadOnlySpan<byte> LoadedLayoutText => """
        <ui:Element pickable="false" xmlns:ui="brutal-ui">
          <ui:SlotContainer slot="top" />
          <ui:SlotContainer slot="bottom" />
        </ui:Element>
        """u8;

    /// <summary>Gets the widget template: a label, a button on <c>mda.mode</c>, and a text field of the capacity 16.</summary>
    private static ReadOnlySpan<byte> LoadedWidgetsText => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Label text="Widgets" />
          <ui:Button text="Mode" command="mda.mode" />
          <ui:TextField capacity="16" />
        </ui:Element>
        """u8;

    /// <summary>Gets the state template: a button on <c>mda.add</c> and a label bound to <c>mda.score</c>.</summary>
    private static ReadOnlySpan<byte> LoadedStateText => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Button text="Add" command="mda.add" />
          <ui:Label bind-text="mda.score" />
        </ui:Element>
        """u8;

    /// <summary>Gets the info template: one label.</summary>
    private static ReadOnlySpan<byte> LoadedInfoText => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Label text="Info" />
        </ui:Element>
        """u8;

    /// <summary>
    /// Builds the loaded scene: the context, the registry, the map, the store, the bindings, the loader, and the
    /// manager. The build registers the records, applies <c>mda.main</c>, runs one update and one draw, reads the
    /// two slots from the layout root, and runs one toggle step, so each root is built, styled, and laid out.
    /// </summary>
    /// <returns>The scene, ready for warm-up and a measure.</returns>
    private static Scene BuildLoadedScene()
    {
        Scene scene = new();
        scene.Renderer = new RecordingRenderer();
        scene.Context = new UiContext(new UiContext.CreateInfo { Measurer = scene.Renderer })
        {
            ViewportSize = new float2(800f, 600f),
        };

        CommandRegistry commands = new();
        commands.Register("mda.mode"u8, s_count, new Counter());
        commands.Register("mda.add"u8, s_count, new Counter());
        commands.Register("mda.cancel"u8, s_count, new Counter());

        scene.Map = new UiActionMap(new UiActionMap.CreateInfo { Commands = commands });
        scene.Map.AddAction("mda.cancel"u8, "mda.cancel"u8);
        scene.Map.BindKey("mda.cancel"u8, EscapeKey);

        scene.Score = new Store<int>(new Store<int>.CreateInfo { Initial = 0 });

        TemplateBindings bindings = new(commands);
        bindings.AddText("mda.score", scene.Score, s_score);
        TemplateLoader templates = new();
        bindings.AttachTo(templates);

        DefinitionLoader definitions = new(templates);
        definitions.AddTemplate("mda.layout", LoadedLayoutText);
        definitions.AddTemplate("mda.widgets", LoadedWidgetsText);
        definitions.AddTemplate("mda.state", LoadedStateText);
        definitions.AddTemplate("mda.info", LoadedInfoText);
        definitions.AddActionMap("mda.map", scene.Map);

        PanelContext panels = new()
        {
            Commands = commands,
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
        scene.Ui = new UiManager(new UiManager.CreateInfo { Context = scene.Context, Panels = panels });

        DefinitionLoadResult result = definitions.Load(LoadedDefinitionsText);
        Assert.True(result.Succeeded);
        result.RegisterTo(scene.Ui);
        scene.Main = result.Modes[0];
        scene.Detail = result.Modes[1];

        // Apply the first mode, run one update and one draw, then read the two slots from the layout root.
        scene.Ui.SetMode("mda.main"u8);
        scene.Context.Update(0.016f);
        scene.Context.Draw(scene.Renderer);
        scene.Top = SlotContainer.Find(scene.Context.Roots[0], "top"u8)!;
        scene.Bottom = SlotContainer.Find(scene.Context.Roots[0], "bottom"u8)!;
        ToggleStep(scene);
        return scene;
    }

    /// <summary>
    /// The 0-byte rule on loaded records: the same toggle step body as the C# gate runs on the records that
    /// <see cref="DefinitionLoader"/> built, and it allocates 0 bytes after warm-up. The test asserts 0 bytes, the
    /// sum <c>Iterations * 12</c>, and the text of the loaded score label equal to the store value.
    /// </summary>
    [Fact]
    public void TheToggleCycleOnLoadedRecordsAllocatesNothing()
    {
        Scene scene = BuildLoadedScene();

        RunSteps(scene, WarmUpRounds, toggle: true);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = RunSteps(scene, Iterations, toggle: true);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
        Assert.Equal(Iterations * 12, sum);

        // The state root is in the bottom slot at the end of each step, and its bound label is its second child.
        Label score = (Label)scene.Bottom.Children[0].Children[1];
        Assert.Equal(scene.Score.Value.ToString(), Encoding.UTF8.GetString(score.Text));
    }
}
