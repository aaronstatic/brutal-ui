using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The result of one <see cref="TemplateLoader"/> load: the root element, or the errors.
/// </summary>
/// <remarks>
/// <para>
/// The result holds a root or one error or more, never both and never neither. A template with one error or more
/// gives no root.
/// </para>
/// <para>
/// The result is immutable. A later load does not change it.
/// </para>
/// </remarks>
public sealed class TemplateLoadResult
{
    private TemplateLoadResult(Element? root, IReadOnlyList<TemplateError> errors)
    {
        Root = root;
        Errors = errors;
    }

    /// <summary>Gets the root element. The value is <see langword="null"/> when <see cref="Errors"/> holds an error.</summary>
    public Element? Root { get; }

    /// <summary>Gets the errors, sorted by line, then by column. The list is empty when the load succeeds.</summary>
    public IReadOnlyList<TemplateError> Errors { get; }

    /// <summary>Gets a value that shows whether the load gives a root. The value is <see langword="true"/> when <see cref="Root"/> is not <see langword="null"/>.</summary>
    public bool Succeeded => Root is not null;

    /// <summary>Gives the root, or throws when the load found an error.</summary>
    /// <remarks>
    /// The exception message holds one line for each error, in the order of the source. Each line is the text of
    /// <see cref="TemplateError.ToString"/>, for example <c>hud.xml(3,59): UnknownAttribute: &lt;message&gt;</c>.
    /// The lines are separated by LF.
    /// </remarks>
    /// <returns>The root element.</returns>
    /// <exception cref="InvalidOperationException">The load found one error or more.</exception>
    public Element GetRootOrThrow()
    {
        if (Root is not null)
        {
            return Root;
        }

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

    /// <summary>Creates the result of a load with no error.</summary>
    /// <param name="root">The root element.</param>
    /// <returns>A result with the root and an empty error list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    internal static TemplateLoadResult Success(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new TemplateLoadResult(root, ReadOnlyCollection<TemplateError>.Empty);
    }

    /// <summary>Creates the result of a load with one error or more.</summary>
    /// <param name="errors">The errors, sorted by line, then by column. The result keeps a copy.</param>
    /// <returns>A result with no root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    internal static TemplateLoadResult Failure(List<TemplateError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failed load holds one error or more.", nameof(errors));
        }

        return new TemplateLoadResult(null, new ReadOnlyCollection<TemplateError>(errors.ToArray()));
    }
}
