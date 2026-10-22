using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The result of one <see cref="StyleSheetParser"/> parse: the sheet, or the errors.
/// </summary>
/// <remarks>
/// <para>
/// The result holds a sheet or one error or more, never both and never neither.
/// A text with one error or more gives no sheet.
/// </para>
/// <para>
/// The result is immutable. A later parse does not change it.
/// </para>
/// </remarks>
public sealed class StyleParseResult
{
    private StyleParseResult(StyleSheet? sheet, IReadOnlyList<StyleParseError> errors)
    {
        Sheet = sheet;
        Errors = errors;
    }

    /// <summary>Gets the sheet. The value is <see langword="null"/> when <see cref="Errors"/> holds an error.</summary>
    public StyleSheet? Sheet { get; }

    /// <summary>Gets the errors in the order of the source. The list is empty when the parse succeeds.</summary>
    public IReadOnlyList<StyleParseError> Errors { get; }

    /// <summary>Gets a value that shows whether the parse gives a sheet. The value is <see langword="true"/> when <see cref="Sheet"/> is not <see langword="null"/>.</summary>
    public bool Succeeded => Sheet is not null;

    /// <summary>Gives the sheet, or throws when the parse found an error.</summary>
    /// <remarks>
    /// The exception message holds one line for each error, in the order of the source.
    /// Each line is the text of <see cref="StyleParseError.ToString"/>, for example
    /// <c>HelloScreen.css(3,10): InvalidValue: &lt;message&gt;</c>. The lines are separated by LF.
    /// </remarks>
    /// <returns>The sheet.</returns>
    /// <exception cref="InvalidOperationException">The parse found one error or more.</exception>
    public StyleSheet GetSheetOrThrow()
    {
        if (Sheet is not null)
        {
            return Sheet;
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

    /// <summary>Creates the result of a parse with no error.</summary>
    /// <param name="sheet">The sheet.</param>
    /// <returns>A result with the sheet and an empty error list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sheet"/> is <see langword="null"/>.</exception>
    internal static StyleParseResult Success(StyleSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        return new StyleParseResult(sheet, ReadOnlyCollection<StyleParseError>.Empty);
    }

    /// <summary>Creates the result of a parse with one error or more.</summary>
    /// <param name="errors">The errors in the order of the source. The result keeps a copy.</param>
    /// <returns>A result with no sheet.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    internal static StyleParseResult Failure(List<StyleParseError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failed parse holds one error or more.", nameof(errors));
        }

        return new StyleParseResult(null, new ReadOnlyCollection<StyleParseError>(errors.ToArray()));
    }
}
