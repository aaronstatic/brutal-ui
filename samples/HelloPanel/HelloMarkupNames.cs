using System;

namespace HelloPanel;

/// <summary>
/// The shared names of the sample load flow: the two file names, the template name, the three factory names, the
/// action map name, the score binding key, and the four command keys. <see cref="HelloMarkup.Load"/> and the
/// allocation gate scene <c>SampleMarkupAllocationScene.BuildScene</c> both read this type, so each name has one
/// source across the sample and the test.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The file links into the test.</strong> <c>BrutalUi.Framework.Tests.csproj</c> links this file the way
/// it links <c>HelloRow.cs</c>. The type therefore compiles in both assemblies, and it holds no reference to a
/// sample-only type. Each string constant names one record the sample adds to a loader. Each span property gives
/// one command key, because <c>CommandRegistry.Register</c> takes a span.
/// </para>
/// <para>
/// <strong>The names match the XML.</strong> The factory names match the <c>template</c> attributes of
/// <c>HelloDefinitions.xml</c>, and the template name matches the <c>template</c> attribute of the state panel
/// record. A name change here must match the XML file change. The check load and the definitions load then report
/// the fault in the sample and in the gate at one time.
/// </para>
/// </remarks>
internal static class HelloMarkupNames
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
    internal const string ScoreKey = "hello.score";

    /// <summary>The command key that adds 1 to the score.</summary>
    internal static ReadOnlySpan<byte> AddCommand => "hello.add"u8;

    /// <summary>The command key that sets the score to 0.</summary>
    internal static ReadOnlySpan<byte> ResetCommand => "hello.reset"u8;

    /// <summary>The command key that opens the rebind listen.</summary>
    internal static ReadOnlySpan<byte> RebindCommand => "hello.rebind"u8;

    /// <summary>The command key that restores the build bindings.</summary>
    internal static ReadOnlySpan<byte> RestoreCommand => "hello.restore"u8;
}
