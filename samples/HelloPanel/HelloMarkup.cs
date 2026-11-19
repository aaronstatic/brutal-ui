using System;
using System.IO;
using BrutalUi.Framework;
using BrutalUi.Markup;

namespace HelloPanel;

/// <summary>
/// Loads the two markup files of the sample: the state panel template <c>HelloState.xml</c> and the six records of
/// <c>HelloDefinitions.xml</c>. The class prints each load error line. <see cref="HelloMarkupNames"/> holds the
/// names both the sample and the allocation gate share.
/// </summary>
/// <remarks>
/// <para>
/// <strong>No GPU and no window.</strong> <see cref="Load"/> needs no GPU, no GLFW, and no window. It runs the
/// same managed flow that <see cref="HelloModes"/> mounts later: the two loads and the register step
/// (<see cref="HelloModes.Register"/>). A template fault, a definition fault, and a mode slot fault therefore
/// land on standard error before the sample opens a window (A3, Decision 3, Decision 6). The register step needs
/// no GPU: the manager defers each panel root build and <see cref="HelloScreen.BuildLayout"/> reads no texture.
/// The panel factories stay deferred to <see cref="HelloModes.Mount"/>, after the window (Decision 5). An IO
/// fault throws, as <see cref="HelloScreen.LoadSheet"/> does.
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
    /// <summary>The selector of the score binding. It returns the score itself, static, created one time.</summary>
    private static readonly Func<int, int> s_score = static s => s;

    /// <summary>
    /// Reads both files from <see cref="AppContext.BaseDirectory"/>, runs the load flow, registers the records
    /// with <see cref="HelloModes.Manager"/> when both loads pass, and returns the loaded records. The method
    /// prints each <see cref="TemplateError.ToString"/> of the check load, each
    /// <see cref="DefinitionError.ToString"/> of the definitions load, and the message of a register
    /// <see cref="ArgumentException"/> to <paramref name="errors"/>, one line each, with no prefix.
    /// </summary>
    /// <param name="state">The state of the sample. Its command registry, its score store, and its map serve the load.</param>
    /// <param name="modes">The modes of the sample. Its three panel factories join the definitions loader, and it
    /// takes the records in the register step.</param>
    /// <param name="errors">The writer of the error lines. <c>Program.Run</c> passes <see cref="Console.Error"/>.</param>
    /// <returns>
    /// The loaded records, or <see langword="null"/> when the check load or the definitions load gave one error or
    /// more, or when the register step threw.
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

        byte[] stateText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, HelloMarkupNames.StateFileName));
        byte[] definitionsText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, HelloMarkupNames.DefinitionsFileName));

        TemplateLoader templates = new();
        TemplateBindings bindings = new(state.Map.Commands);
        bindings.AddText(HelloMarkupNames.ScoreKey, state.Score, s_score);
        bindings.AttachTo(templates);
        templates.RegisterHelloPanelElements();

        // The check load of Decision 7: a fault in the panel text lands here, before the window. The tree is dropped.
        TemplateLoadResult check = templates.Load(stateText, HelloMarkupNames.StateTemplateName);

        DefinitionLoader definitions = new(templates);
        definitions.AddTemplate(HelloMarkupNames.StateTemplateName, stateText);
        definitions.AddFactory(HelloMarkupNames.WidgetPanelFactoryName, modes.CreateWidgetPanel);
        definitions.AddFactory(HelloMarkupNames.InfoPanelFactoryName, modes.CreateInfoPanel);
        definitions.AddFactory(HelloMarkupNames.LayoutFactoryName, modes.CreateLayout);
        definitions.AddActionMap(HelloMarkupNames.ActionsMapName, state.Map);

        DefinitionLoadResult result = definitions.Load(definitionsText, HelloMarkupNames.DefinitionsFileName);

        // Both loads ran, so one run prints each error of each load. The check load prints first (load order, A3).
        foreach (TemplateError error in check.Errors)
        {
            errors.WriteLine(error.ToString());
        }

        foreach (DefinitionError error in result.Errors)
        {
            errors.WriteLine(error.ToString());
        }

        if (!check.Succeeded || !result.Succeeded)
        {
            return null;
        }

        // The register step of HelloModes runs here, before the window (A3, Decision 6). The resolve pass skips
        // the slot check of a mode on a factory layout (Decision 3), and UiManager.Register(ModeDefinition)
        // checks each slot inside RegisterTo. The sample owns when it registers, so a slot typo in
        // HelloDefinitions.xml prints one line here and the run exits 1 before the first GLFW call.
        try
        {
            modes.Register(result);
        }
        catch (ArgumentException exception)
        {
            errors.WriteLine($"{HelloMarkupNames.DefinitionsFileName}: {exception.Message}");
            return null;
        }

        return result;
    }
}
