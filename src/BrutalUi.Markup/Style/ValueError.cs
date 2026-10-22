namespace BrutalUi.Markup;

/// <summary>
/// An error of a value read: the kind, the index of the token at fault, and the message. The caller makes the
/// <see cref="StyleParseError"/> from the token at that index.
/// </summary>
/// <param name="Kind">The kind of the error.</param>
/// <param name="TokenIndex">The index of the first token at fault in the token list.</param>
/// <param name="Message">The problem and the fix.</param>
internal readonly record struct ValueError(StyleErrorKind Kind, int TokenIndex, string Message);
