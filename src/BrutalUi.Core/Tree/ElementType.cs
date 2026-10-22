using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace BrutalUi.Core;

/// <summary>
/// The global table of element type names. It maps a name to an <see cref="ElementTypeId"/> and back.
/// </summary>
/// <remarks>
/// <para>
/// The table is global to the process. A name keeps its id for the life of the process.
/// The table never removes a name.
/// </para>
/// <para>
/// Both methods take one lock, so any thread can call them. The element tree itself stays main-thread only.
/// An intern of a new name allocates. A call at startup or at load time is the right place for it.
/// </para>
/// </remarks>
public static class ElementType
{
    private static readonly Lock s_lock = new();
    private static readonly Dictionary<string, ElementTypeId> s_ids = new(StringComparer.Ordinal);

    // Index 0 holds a placeholder for ElementTypeId.None. The first name gets the index 1.
    private static readonly List<string> s_names = [string.Empty];

    /// <summary>Returns the id of an element type name. The method adds the name when the table does not hold it.</summary>
    /// <param name="name">The type name. The comparison is ordinal.</param>
    /// <returns>The id of the name. The same name always gives the same id.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static ElementTypeId Intern(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        lock (s_lock)
        {
            if (s_ids.TryGetValue(name, out ElementTypeId existing))
            {
                return existing;
            }

            ElementTypeId id = new(s_names.Count);
            s_names.Add(name);
            s_ids.Add(name, id);
            return id;
        }
    }

    /// <summary>Returns the name of an element type id.</summary>
    /// <param name="id">The id to look up.</param>
    /// <returns>The type name of the id.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The table does not hold the id. <see cref="ElementTypeId.None"/> is not in the table.</exception>
    public static string GetName(ElementTypeId id)
    {
        if (!TryGetName(id, out string? name))
        {
            throw new ArgumentOutOfRangeException(nameof(id), id.Value, "The element type table does not hold this id. Get an id from ElementType.Intern or Element.TypeId.");
        }

        return name;
    }

    /// <summary>Looks up the name of an element type id. The method never throws.</summary>
    /// <param name="id">The id to look up.</param>
    /// <param name="name">The type name of the id, or <see langword="null"/> when the table does not hold the id.</param>
    /// <returns><see langword="true"/> when the table holds the id. <see cref="ElementTypeId.None"/> is not in the table.</returns>
    internal static bool TryGetName(ElementTypeId id, [NotNullWhen(true)] out string? name)
    {
        lock (s_lock)
        {
            if (id.Value < 1 || id.Value >= s_names.Count)
            {
                name = null;
                return false;
            }

            name = s_names[id.Value];
            return true;
        }
    }
}
