using System;
using System.IO;
using BrutalUi.Framework;
using BrutalUi.Markup;

namespace HelloPanel;

/// <summary>
/// Loads the two markup files of the sample: the state panel template <c>HelloState.xml</c> and the six records of
/// <c>HelloDefinitions.xml</c>. The class holds the names both files use and prints each load error line.
/// </summary>
/// <remarks>
/// <para>
/// <strong>No GPU and no window.</strong> <see cref="Load"/> needs no GPU, no GLFW, and no window. It runs the same
/// managed flow that <see cref="HelloModes"/> mounts later, so a template fault or a definition fault lands on
/// standard error before the sample opens a window (A3, Decision 6). An IO fault throws, as
/// <see cref="HelloScreen.LoadSheet"/> does.
/// </para>
/// <para>
/// <strong>The load order (A4).</strong> The bindings attach to the template loader, the generated register call
/// adds the marked types, the definitions loader takes the template text, the three factory names, and the action
/// map name, and the sample calls <c>DefinitionLoader.Load</c> one time. The flow runs in that order, and a
/// successful call gives the six records.
/// </para>
/// <para>
/// <strong>The check load (Decision 7).</strong> The method loads <c>HelloState.xml</c> through the template loader
/// one time, before the definitions load, and drops the tree. <see cref="DefinitionLoader"/> checks only the name of
/// a panel template, so a fault in the panel text would otherwise show only inside
/// <see cref="HelloModes.Mount"/>, after the window. The method runs both loads, so one run prints each error of
/// each load. It returns <see langword="null"/> when one error or more exists, and prints no prefix before an error
/// line (A3).
/// </para>
/// </remarks>
internal static class HelloMarkup
{
    /// <summary>The file name of the state panel template, read from <see cref="AppContext.BaseDirectory"/>.</summary>
    internal const string StateFileName = "HelloState.xml";

    /// <summary>The file name of the six records, read from <see cref="AppContext.BaseDirectory"/>.</summary>
    internal const string DefinitionsFileName = "HelloDefinitions.xml";

    /// <summary>The template name of the state panel text. It equals the file name.</summary>
    internal const string StateTemplateName = "HelloState.xml";

    /// <summary>The factory name of the widget panel.</summary>
    internal const string WidgetPanelFactoryName = "HelloModes.CreateWidgetPanel";

    /// <summary>The factory name of the info panel.</summary>
    internal const string InfoPanelFactoryName = "HelloModes.CreateInfoPanel";

    /// <summary>The factory name of the layout.</summary>
    internal const string LayoutFactoryName = "HelloModes.CreateLayout";

    /// <summary>The action map name of the state map.</summary>
    internal const string ActionsMapName = "hello.actions";

    /// <summary>The binding key of the score label.</summary>
    private const string ScoreKey = "hello.score";

    /// <summary>The selector of the score binding. It returns the score itself, static, created one time.</summary>
    private static readonly Func<int, int> s_score = static s => s;

    /// <summary>
    /// Reads both files from <see cref="AppContext.BaseDirectory"/>, runs the load flow, and returns the loaded
    /// records. The method prints each <see cref="TemplateError.ToString"/> of the check load and each
    /// <see cref="DefinitionError.ToString"/> of the definitions load to <paramref name="errors"/>, one line each,
    /// with no prefix.
    /// </summary>
    /// <param name="state">The state of the sample. Its command registry, its score store, and its map serve the load.</param>
    /// <param name="modes">The modes of the sample. Its three panel factories join the definitions loader.</param>
    /// <param name="errors">The writer of the error lines. <c>Program.Run</c> passes <see cref="Console.Error"/>.</param>
    /// <returns>
    /// The loaded records, or <see langword="null"/> when the check load or the definitions load gave one error or
    /// more.
    /// </returns>
    /// <exception cref="IOException">One file is missing or unreadable. The call throws, as
    /// <see cref="HelloScreen.LoadSheet"/> does for a missing sheet.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="state"/>, <paramref name="modes"/>, or
    /// <paramref name="errors"/> is <see langword="null"/>.</exception>
    public static DefinitionLoadResult? Load(HelloState state, HelloModes modes, TextWriter errors)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(modes);
        ArgumentNullException.ThrowIfNull(errors);

        byte[] stateText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, StateFileName));
        byte[] definitionsText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, DefinitionsFileName));

        TemplateLoader templates = new();
        TemplateBindings bindings = new(state.Map.Commands);
        bindings.AddText(ScoreKey, state.Score, s_score);
        bindings.AttachTo(templates);
        templates.RegisterHelloPanelElements();

        // The check load of Decision 7: a fault in the panel text lands here, before the window. The tree is dropped.
        TemplateLoadResult check = templates.Load(stateText, StateTemplateName);

        DefinitionLoader definitions = new(templates);
        definitions.AddTemplate(StateTemplateName, stateText);
        definitions.AddFactory(WidgetPanelFactoryName, modes.CreateWidgetPanel);
        definitions.AddFactory(InfoPanelFactoryName, modes.CreateInfoPanel);
        definitions.AddFactory(LayoutFactoryName, modes.CreateLayout);
        definitions.AddActionMap(ActionsMapName, state.Map);

        DefinitionLoadResult result = definitions.Load(definitionsText, DefinitionsFileName);

        // Both loads ran, so one run prints each error of each load. The check load prints first (load order, A3).
        foreach (TemplateError error in check.Errors)
        {
            errors.WriteLine(error.ToString());
        }

        foreach (DefinitionError error in result.Errors)
        {
            errors.WriteLine(error.ToString());
        }

        return check.Succeeded && result.Succeeded ? result : null;
    }
}
