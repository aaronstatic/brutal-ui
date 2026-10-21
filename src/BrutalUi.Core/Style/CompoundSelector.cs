using System;

namespace BrutalUi.Core;

/// <summary>
/// One compound of a selector, as <c>Label.title:hover:focus</c> is in CSS. The element must hold each part of the
/// compound.
/// </summary>
/// <remarks>
/// <para>
/// A compound has four parts. Each part is optional, but a compound needs at least one part.
/// <see cref="Type"/> holds the type of the element, <see cref="Class"/> holds one class, <see cref="States"/>
/// holds each pseudo-state that the element must hold, and <see cref="Name"/> holds the name of the element. Set the parts with an object initializer, and then give the
/// compound to <see cref="Selector.For(in CompoundSelector)"/>. The factory checks the parts.
/// </para>
/// <para>
/// The type part compares the id of <see cref="Element.TypeId"/> with <see cref="Type"/>. The comparison is exact. A
/// subclass that does not override <see cref="Element.TypeId"/> has the id of its base class and matches the type part
/// of the base class. A subclass that overrides the id does not match it.
/// </para>
/// <para>
/// The state part compares the bits of <see cref="Element.States"/> with <see cref="States"/>. The element must hold
/// each bit of the part, and it can hold more bits. A compound with only a state part, as <c>:hover</c> is in CSS,
/// matches each element in the state.
/// </para>
/// <para>
/// The name part compares <see cref="Element.Name"/> with <see cref="Name"/>, as <c>#title</c> does in CSS. The
/// compare is ordinal and case-sensitive. A <see langword="null"/> <see cref="Name"/> means no name part.
/// </para>
/// </remarks>
public readonly struct CompoundSelector
{
    /// <summary>
    /// Gets the type that the element must have. <see cref="ElementTypeId.None"/> when the compound has no type part.
    /// </summary>
    /// <remarks>Get the id from <see cref="ElementType.Intern(string)"/> or <see cref="Element.TypeId"/>.</remarks>
    public ElementTypeId Type { get; init; }

    /// <summary>
    /// Gets the class that the element must hold. <see cref="ClassId.None"/> when the compound has no class part.
    /// </summary>
    /// <remarks>Get the id from <see cref="ClassName.Intern(string)"/>.</remarks>
    public ClassId Class { get; init; }

    /// <summary>
    /// Gets each pseudo-state that the element must hold. <see cref="PseudoStates.None"/> when the compound has no
    /// state part.
    /// </summary>
    /// <remarks>Each bit counts one time in the class tier of <see cref="Selector.Specificity"/>.</remarks>
    public PseudoStates States { get; init; }

    /// <summary>
    /// Gets the name that the element must have. <see langword="null"/> when the compound has no name part.
    /// </summary>
    /// <remarks>
    /// The compare is ordinal, so it is case-sensitive: <c>Title</c> and <c>title</c> are two names. An element with
    /// a <see langword="null"/> name matches no name part. <see cref="Selector"/> rejects the empty name.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>Gets a value that shows whether the compound holds no part.</summary>
    internal bool IsEmpty => Type.IsNone && Class.IsNone && States == PseudoStates.None && Name is null;

    /// <summary>Checks whether an element holds each part of the compound.</summary>
    /// <remarks>
    /// The method tests the cheapest part first: the state bits, then the type id, then the class list, then the name.
    /// The name compare checks the reference first and allocates nothing. An empty
    /// compound gives <see langword="true"/>. <see cref="Selector"/> rejects an empty compound before the call.
    /// </remarks>
    /// <param name="element">The element to check.</param>
    /// <param name="elementType">The type id of the element. The style pass reads it one time for each element.</param>
    /// <returns><see langword="true"/> when the element holds each part.</returns>
    internal bool Matches(Element element, ElementTypeId elementType)
    {
        if ((element.States & States) != States)
        {
            return false;
        }

        if (!Type.IsNone && Type != elementType)
        {
            return false;
        }

        if (!Class.IsNone && !element.ClassStorage.Contains(Class))
        {
            return false;
        }

        return Name is null || string.Equals(element.Name, Name, StringComparison.Ordinal);
    }

    /// <summary>Checks whether an ancestor of the subject holds each part of the compound.</summary>
    /// <remarks>
    /// The method reads <see cref="Element.TypeId"/> only when the compound has a type part. Thus a compound with no
    /// type part makes no virtual call. The method gives the same result as
    /// <see cref="Matches(Element, ElementTypeId)"/> with the type id of the element.
    /// </remarks>
    /// <param name="element">The ancestor to check.</param>
    /// <returns><see langword="true"/> when the element holds each part.</returns>
    internal bool Matches(Element element)
    {
        return Matches(element, Type.IsNone ? ElementTypeId.None : element.TypeId);
    }
}
