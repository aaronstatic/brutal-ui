namespace BrutalUi.Core;

/// <summary>
/// A source of bound state with a version counter.
/// </summary>
/// <remarks>
/// <para>
/// A reader keeps the version that it applied last. The reader reads the source again when <see cref="Version"/>
/// differs from that version.
/// </para>
/// <para>
/// The version never goes down. A new source starts at 1, so a reader that starts at 0 applies at its first read.
/// </para>
/// <para>
/// The source raises no event. A read of <see cref="Version"/> allocates nothing.
/// </para>
/// <para>
/// The interface names no type of the framework layer, so the binding pass of Core reads it.
/// </para>
/// </remarks>
public interface IBindingSource
{
    /// <summary>
    /// Gets the version of the source. The version rises when the state changes, and it never goes down.
    /// </summary>
    ulong Version { get; }
}
