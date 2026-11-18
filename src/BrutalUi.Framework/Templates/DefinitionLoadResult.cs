using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BrutalUi.Framework;

/// <summary>
/// The result of one <see cref="DefinitionLoader.Load"/> call: the three record lists, or the error list.
/// </summary>
/// <remarks>
/// <para>
/// <strong>All or nothing.</strong> One error or more gives <see langword="false"/> for
/// <see cref="Succeeded"/>, the full sorted <see cref="Errors"/> list, and empty record lists. No error gives the
/// three record lists and an empty <see cref="Errors"/> list. A caller never reads a record from a failed result.
/// </para>
/// <para>
/// <strong>The register order.</strong> <see cref="RegisterTo"/> registers the panels first, then the layouts, then
/// the modes (Q6). This is the order in which the names must exist: a panel layout pair before the mode that names
/// them.
/// </para>
/// <para>
/// <strong>A name that the manager holds.</strong> The load checks duplicate names inside one text only (A6). A name
/// that the <see cref="UiManager"/> holds already makes its <c>Register</c> call throw
/// <see cref="ArgumentException"/> inside <see cref="RegisterTo"/>. <see cref="RegisterTo"/> is not atomic: the
/// records before the throw stay registered.
/// </para>
/// </remarks>
public sealed class DefinitionLoadResult
{
    /// <summary>Creates a result. The lists are the final content; the constructor wraps them.</summary>
    /// <param name="panels">The panel records, in text order.</param>
    /// <param name="layouts">The layout records, in text order.</param>
    /// <param name="modes">The mode records, in text order.</param>
    /// <param name="errors">The errors, sorted by line, then column.</param>
    private DefinitionLoadResult(
        List<PanelDefinition> panels,
        List<LayoutDefinition> layouts,
        List<ModeDefinition> modes,
        List<DefinitionError> errors)
    {
        Panels = new ReadOnlyCollection<PanelDefinition>(panels);
        Layouts = new ReadOnlyCollection<LayoutDefinition>(layouts);
        Modes = new ReadOnlyCollection<ModeDefinition>(modes);
        Errors = new ReadOnlyCollection<DefinitionError>(errors);
    }

    /// <summary>Gets the panel records, in the order of the text. The list is empty when
    /// <see cref="Errors"/> holds one error or more.</summary>
    public IReadOnlyList<PanelDefinition> Panels { get; }

    /// <summary>Gets the layout records, in the order of the text. The list is empty when
    /// <see cref="Errors"/> holds one error or more.</summary>
    public IReadOnlyList<LayoutDefinition> Layouts { get; }

    /// <summary>Gets the mode records, in the order of the text. The list is empty when
    /// <see cref="Errors"/> holds one error or more.</summary>
    public IReadOnlyList<ModeDefinition> Modes { get; }

    /// <summary>Gets the errors, sorted by line, then column. The list is empty when the load succeeded.</summary>
    public IReadOnlyList<DefinitionError> Errors { get; }

    /// <summary>Gets a value that indicates whether the load gave no error.</summary>
    /// <value><see langword="true"/> when <see cref="Errors"/> is empty.</value>
    public bool Succeeded => Errors.Count == 0;

    /// <summary>Creates a success result.</summary>
    /// <param name="panels">The panel records, in text order.</param>
    /// <param name="layouts">The layout records, in text order.</param>
    /// <param name="modes">The mode records, in text order.</param>
    /// <returns>The result.</returns>
    internal static DefinitionLoadResult Success(
        List<PanelDefinition> panels,
        List<LayoutDefinition> layouts,
        List<ModeDefinition> modes)
    {
        return new DefinitionLoadResult(panels, layouts, modes, new List<DefinitionError>());
    }

    /// <summary>Creates a failure result.</summary>
    /// <param name="errors">The errors, sorted by line, then column. The list must not be empty.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty. A result with no error is a success.</exception>
    internal static DefinitionLoadResult Failure(List<DefinitionError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException(
                "A failure result needs one error or more. Call Success when the load gave no error.",
                nameof(errors));
        }

        return new DefinitionLoadResult(
            new List<PanelDefinition>(),
            new List<LayoutDefinition>(),
            new List<ModeDefinition>(),
            errors);
    }

    /// <summary>Registers the records on a manager: the panels, then the layouts, then the modes.</summary>
    /// <remarks>
    /// <para>
    /// The method registers nothing for a failed result. It throws with one <see cref="DefinitionError.ToString"/>
    /// line for each error, separated by LF. This is the form of
    /// <see cref="BrutalUi.Markup.TemplateLoadResult.GetRootOrThrow"/>.
    /// </para>
    /// <para>
    /// The call is not atomic. A name that the <see cref="UiManager"/> holds already makes its <c>Register</c> call
    /// throw <see cref="ArgumentException"/> (A6), and the records before the throw stay registered. A game that
    /// needs a clean manager builds the manager again and calls <see cref="RegisterTo"/> once for each text, or
    /// registers the records itself.
    /// </para>
    /// </remarks>
    /// <param name="ui">The manager that takes the records.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ui"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The result holds one error or more. Nothing registers.</exception>
    /// <exception cref="ArgumentException">The <see cref="UiManager"/> holds the name of a record already. The
    /// records before the clash stay registered.</exception>
    public void RegisterTo(UiManager ui)
    {
        ArgumentNullException.ThrowIfNull(ui);

        if (!Succeeded)
        {
            StringBuilder text = new();
            for (int i = 0; i < Errors.Count; i++)
            {
                if (i > 0)
                {
                    text.Append('\n');
                }

                text.Append(Errors[i].ToString());
            }

            throw new InvalidOperationException(text.ToString());
        }

        foreach (PanelDefinition panel in Panels)
        {
            ui.Register(panel);
        }

        foreach (LayoutDefinition layout in Layouts)
        {
            ui.Register(layout);
        }

        foreach (ModeDefinition mode in Modes)
        {
            ui.Register(mode);
        }
    }
}
