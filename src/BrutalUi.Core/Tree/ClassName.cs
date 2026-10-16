using System;
using System.Collections.Generic;
using System.Threading;

namespace BrutalUi.Core;

/// <summary>
/// The global table of class names. It maps a name to a <see cref="ClassId"/> and back.
/// </summary>
/// <remarks>
/// <para>
/// The table is global to the process. A name keeps its id for the life of the process.
/// The table never removes a name.
/// </para>
/// <para>
/// Both methods take one lock, so any thread can call them. The element tree itself stays main-thread only.
/// An intern of a new name allocates. A call at startup or at build time is the right place for it.
/// </para>
/// </remarks>
public static class ClassName
{
    private static readonly Lock s_lock = new();
    private static readonly Dictionary<string, ClassId> s_ids = new(StringComparer.Ordinal);

    // Index 0 holds a placeholder for ClassId.None. The first name gets the index 1.
    private static readonly List<string> s_names = [string.Empty];

    /// <summary>Returns the id of a class name. The method adds the name when the table does not hold it.</summary>
    /// <param name="name">The class name. The comparison is ordinal.</param>
    /// <returns>The id of the name. The same name always gives the same id.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static ClassId Intern(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        lock (s_lock)
        {
            if (s_ids.TryGetValue(name, out ClassId existing))
            {
                return existing;
            }

            ClassId id = new(s_names.Count);
            s_names.Add(name);
            s_ids.Add(name, id);
            return id;
        }
    }

    /// <summary>Returns the name of a class id.</summary>
    /// <param name="id">The id to look up.</param>
    /// <returns>The class name of the id.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The table does not hold the id. <see cref="ClassId.None"/> is not in the table.</exception>
    public static string GetName(ClassId id)
    {
        lock (s_lock)
        {
            if (id.Value < 1 || id.Value >= s_names.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id.Value, "The class table does not hold this id.");
            }

            return s_names[id.Value];
        }
    }
}
