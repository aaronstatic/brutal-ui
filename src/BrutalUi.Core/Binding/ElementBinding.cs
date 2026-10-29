namespace BrutalUi.Core;

/// <summary>
/// The apply handler of a binding. It writes the current value of the source into the element.
/// </summary>
/// <remarks>
/// <para>
/// The binding pass of <see cref="UiContext.Update(float)"/> calls the handler one time for each new version of the
/// source, before the style pass. Two new versions before one update give one call. The first update after the add
/// calls the handler when the element is in the tree of a root of the context.
/// </para>
/// <para>
/// Pass a static method in a <c>static readonly</c> field, so the add allocates no delegate. Keep the per-binding
/// state in the target object, not in a closure.
/// </para>
/// <para>
/// The handler writes a value of its element through a setter that marks the dirty bits of the change. It must not
/// add or remove an element, and it must not add or clear a binding.
/// </para>
/// </remarks>
/// <param name="element">The element that holds the binding.</param>
/// <param name="target">The target object of the binding, as the add received it.</param>
public delegate void BindingHandler(Element element, object? target);

/// <summary>
/// One binding of an element: a source, the version that the element applied last, a handler, and a target.
/// </summary>
/// <remarks>
/// The struct names no type of the framework layer. The binding pass reads <see cref="IBindingSource.Version"/> and
/// calls <see cref="Handler"/> on a change only. The struct lives in the binding array of its element.
/// </remarks>
internal struct ElementBinding
{
    /// <summary>The source of the binding. The binding pass reads its version.</summary>
    public IBindingSource Source;

    /// <summary>
    /// The version of <see cref="Source"/> that the binding applied last. The add writes 0, and a source starts at 1,
    /// so the first pass applies the binding.
    /// </summary>
    public ulong AppliedVersion;

    /// <summary>The handler that writes the value of the source into the element.</summary>
    public BindingHandler Handler;

    /// <summary>The target object that the binding pass gives to <see cref="Handler"/>.</summary>
    public object? Target;
}
