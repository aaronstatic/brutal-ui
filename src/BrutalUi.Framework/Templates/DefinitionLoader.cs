using System;
using System.Collections.Generic;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// Loads <c>ui:Definitions</c> texts into <see cref="PanelDefinition"/>, <see cref="LayoutDefinition"/>, and
/// <see cref="ModeDefinition"/> records. The object holds the template table and the action map table that a
/// definition text names by <c>template</c> and <c>actions</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>One loader for each template loader.</strong> The constructor registers the element type
/// <c>SlotContainer</c> on the given <see cref="TemplateLoader"/>. A second <see cref="DefinitionLoader"/> on one
/// <see cref="TemplateLoader"/>, or a game type with that name, makes <see cref="TemplateLoader.Register(in ElementTypeInfo)"/>
/// throw <see cref="ArgumentException"/> in the constructor.
/// </para>
/// <para>
/// <strong>One name holds one template, and one name holds one map.</strong> The tables compare names with an
/// ordinal compare, because <c>XmlReader</c> gives each value as a <see cref="string"/>. A second
/// <see cref="AddTemplate"/> or <see cref="AddActionMap"/> call for one name throws
/// <see cref="ArgumentException"/> before the add. <see cref="AddTemplate"/> copies the text, so a later change of
/// the source changes nothing.
/// </para>
/// <para>
/// <strong>An add after a load is valid.</strong> The next load reads the tables again. An add call and a load call
/// must not run at the same time, as for <see cref="TemplateLoader.Register(in ElementTypeInfo)"/>.
/// </para>
/// <para>
/// <strong>The checks of A6.</strong> The load checks duplicate names inside one text only. A record name that the
/// <see cref="UiManager"/> holds makes its <c>Register</c> call throw <see cref="ArgumentException"/> inside
/// <see cref="DefinitionLoadResult.RegisterTo"/>, and <see cref="DefinitionLoadResult.RegisterTo"/> is not atomic.
/// </para>
/// <para>
/// <strong>Load time and run time.</strong> The load, the template copy, the check loads, and each record factory
/// call allocate at build time. A loaded record holds the same data as a C# record, so the mode diff allocates
/// nothing after warm-up.
/// </para>
/// </remarks>
public sealed class DefinitionLoader
{
    /// <summary>The template loader that reads the templates and builds the trees.</summary>
    private readonly TemplateLoader _templates;

    /// <summary>The template table: name to UTF-8 text copy. The compare is ordinal.</summary>
    private readonly Dictionary<string, byte[]> _templatesByName;

    /// <summary>The action map table: name to map. The compare is ordinal.</summary>
    private readonly Dictionary<string, UiActionMap> _actionMaps;

    /// <summary>Creates a loader and registers the <c>SlotContainer</c> element type on the given template loader.</summary>
    /// <remarks>
    /// A game makes one <see cref="DefinitionLoader"/> for each <see cref="TemplateLoader"/>. See Notes item 4 of the
    /// plan: the second loader on one template loader throws.
    /// </remarks>
    /// <param name="templates">The template loader that the panel and layout factories use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="templates"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The loader holds an element type named <c>SlotContainer</c> already.</exception>
    public DefinitionLoader(TemplateLoader templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        _templates = templates;
        _templatesByName = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        _actionMaps = new Dictionary<string, UiActionMap>(StringComparer.Ordinal);
        templates.Register(SlotContainerTemplate.Create());
    }

    /// <summary>Gets the template loader that this object registered on.</summary>
    internal TemplateLoader Templates => _templates;

    /// <summary>Adds one template text under a name. A definition value names the text with its <c>template</c>
    /// attribute.</summary>
    /// <remarks>
    /// The method copies the text. A later change of the source bytes changes nothing. An add after a load is valid:
    /// the next load reads the tables again.
    /// </remarks>
    /// <param name="name">The name of the template, for example <c>hud.panel</c>. The compare is ordinal.</param>
    /// <param name="utf8">The template text, coded as UTF-8.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public void AddTemplate(string name, ReadOnlySpan<byte> utf8)
    {
        ArgumentNullException.ThrowIfNull(name);

        _templatesByName[name] = utf8.ToArray();
    }

    /// <summary>Adds one action map under a name. A mode value names the map with its <c>actions</c> attribute.</summary>
    /// <remarks>An add after a load is valid: the next load reads the tables again.</remarks>
    /// <param name="name">The name of the map, for example <c>play.actions</c>. The compare is ordinal.</param>
    /// <param name="map">The map.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="map"/> is <see langword="null"/>.</exception>
    public void AddActionMap(string name, UiActionMap map)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(map);

        _actionMaps[name] = map;
    }

    /// <summary>Loads one definitions text into records.</summary>
    /// <remarks>
    /// The method reads the text in a forward walk, then resolves the entries against the tables and each other. One
    /// load gives each error. The result lists are sorted and complete; see <see cref="DefinitionLoadResult"/>.
    /// </remarks>
    /// <param name="utf8">The definitions text, coded as UTF-8.</param>
    /// <param name="sourceName">The name of the source for the error lines. The default is <c>definitions</c>.</param>
    /// <returns>The records, or the errors. The method never throws for the text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceName"/> is <see langword="null"/>.</exception>
    public DefinitionLoadResult Load(ReadOnlySpan<byte> utf8, string sourceName = "definitions")
    {
        ArgumentNullException.ThrowIfNull(sourceName);

        return new DefinitionReader(this, sourceName).Load(utf8);
    }

    /// <summary>Gives the UTF-8 text copy of one template name.</summary>
    /// <param name="name">The template name. The compare is ordinal.</param>
    /// <param name="utf8">The text copy on a success, or <see langword="null"/> on a miss.</param>
    /// <returns><see langword="true"/> when the table holds the name.</returns>
    internal bool TryGetTemplate(string name, out byte[]? utf8)
    {
        return _templatesByName.TryGetValue(name, out utf8);
    }

    /// <summary>Gives one action map by name.</summary>
    /// <param name="name">The map name. The compare is ordinal.</param>
    /// <param name="map">The map on a success, or <see langword="null"/> on a miss.</param>
    /// <returns><see langword="true"/> when the table holds the name.</returns>
    internal bool TryGetActionMap(string name, out UiActionMap? map)
    {
        return _actionMaps.TryGetValue(name, out map);
    }
}
