// The two name rules of the plan: "The kebab rule" and "The register call".
using System.Text;

namespace BrutalUi.Generators;

/// <summary>
/// The name rules that the transform and the writer share.
/// </summary>
internal static class NameRules
{
    /// <summary>
    /// Converts a property name or an enum member name to a kebab-case row name.
    /// A hyphen goes before an upper-case letter that follows a lower-case letter or a
    /// digit, and before an upper-case letter that follows an upper-case letter and
    /// precedes a lower-case letter. Then each letter becomes lower case.
    /// </summary>
    /// <param name="name">The source name. For example <c>ScrollOffset</c>.</param>
    /// <returns>The row name. For example <c>scroll-offset</c>.</returns>
    public static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                var prev = name[i - 1];
                var after = i + 1 < name.Length ? name[i + 1] : '\0';
                var insert = char.IsLower(prev) || char.IsDigit(prev)
                    || (char.IsUpper(prev) && char.IsLower(after));
                if (insert)
                {
                    builder.Append('-');
                }
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds the assembly id of the register call. The id keeps the ASCII letters, the
    /// digits, and <c>_</c> of the assembly name, and drops each other character. An id
    /// that starts with a digit gets a <c>_</c> before it. A <see langword="null"/> or
    /// empty name gives <c>Assembly</c>.
    /// </summary>
    /// <param name="assemblyName">The assembly name, for example <c>BrutalUi.Markup.Tests</c>.</param>
    /// <returns>The id, for example <c>BrutalUiMarkupTests</c>.</returns>
    public static string ToIdentifier(string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            return "Assembly";
        }

        // netstandard2.0 lacks the NotNullWhen attribute on IsNullOrEmpty, so the
        // compiler keeps the null flow. The check above rules it out.
        var name = assemblyName!;
        var builder = new StringBuilder(name.Length + 1);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            var keep = c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';
            if (keep)
            {
                builder.Append(c);
            }
        }

        // An empty id cannot follow a name with characters. Guard it as an empty name.
        if (builder.Length == 0)
        {
            return "Assembly";
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }
}
