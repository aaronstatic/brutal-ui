using System;
using System.Globalization;
using System.Text;

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
/// <para>
/// Two compounds are equal when each of the four parts is equal. The name compare is ordinal. The equality members
/// allocate nothing, so a test or a cache can compare compounds. <see cref="Equals(object?)"/> boxes only when the
/// caller gives a boxed value. <see cref="ToString"/> gives the CSS text of the compound. It allocates and reads the
/// global name tables, so use it for test output and debug output only.
/// </para>
/// </remarks>
public readonly struct CompoundSelector : IEquatable<CompoundSelector>
{
    /// <summary>
    /// The state bits and their CSS text, in the order of the text form. <see cref="ToString"/> writes each state in
    /// this order.
    /// </summary>
    private static readonly (PseudoStates State, string Text)[] StateTexts =
    [
        (PseudoStates.Hover, ":hover"),
        (PseudoStates.Active, ":active"),
        (PseudoStates.Focus, ":focus"),
        (PseudoStates.Disabled, ":disabled"),
        (PseudoStates.Checked, ":checked"),
    ];

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

    /// <summary>Compares two compounds.</summary>
    /// <param name="left">The first compound.</param>
    /// <param name="right">The second compound.</param>
    /// <returns><see langword="true"/> when each part of the two compounds is equal.</returns>
    public static bool operator ==(in CompoundSelector left, in CompoundSelector right) => left.EqualParts(in right);

    /// <summary>Compares two compounds.</summary>
    /// <param name="left">The first compound.</param>
    /// <param name="right">The second compound.</param>
    /// <returns><see langword="true"/> when one part or more of the two compounds is not equal.</returns>
    public static bool operator !=(in CompoundSelector left, in CompoundSelector right) => !left.EqualParts(in right);

    /// <summary>Compares the compound with another compound.</summary>
    /// <remarks>
    /// The method compares <see cref="Type"/>, <see cref="Class"/>, <see cref="States"/>, and <see cref="Name"/>. The
    /// name compare is ordinal, so two string instances with the same text are equal. The method allocates nothing.
    /// </remarks>
    /// <param name="other">The compound to compare.</param>
    /// <returns><see langword="true"/> when each part of the two compounds is equal.</returns>
    public bool Equals(CompoundSelector other) => EqualParts(in other);

    /// <summary>Compares the compound with an object.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is a <see cref="CompoundSelector"/> and each part is equal.
    /// </returns>
    public override bool Equals(object? obj) => obj is CompoundSelector other && EqualParts(in other);

    /// <summary>Gives a hash code from the four parts.</summary>
    /// <remarks>
    /// Two equal compounds give the same hash code. The name part uses the ordinal hash, and a
    /// <see langword="null"/> name gives 0. The method allocates nothing.
    /// </remarks>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        int nameHash = Name is null ? 0 : StringComparer.Ordinal.GetHashCode(Name);
        return HashCode.Combine(Type.Value, Class.Value, (int)States, nameHash);
    }

    /// <summary>
    /// Gives the CSS text of the compound: the type name, then <c>.class</c>, then <c>#name</c>, then each state in
    /// the order <c>:hover:active:focus:disabled:checked</c>. For example, <c>Label.title#score:hover</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method allocates and reads the global name tables. Use it for test output and debug output only. The text
    /// uses the grammar of the stylesheet parser, so the parser reads the text back to an equal compound for each
    /// name that the grammar of section 11 can read (an identifier for a type or a class, <c>[A-Za-z0-9_-]+</c> for a
    /// name). Another name gives a text that the parser does not read back.
    /// </para>
    /// <para>
    /// The method never throws. A type id that the type table does not hold gives <c>&lt;type N&gt;</c>, and a class
    /// id that the class table does not hold gives <c>.&lt;class N&gt;</c>, with the number of the id for N. A state
    /// bit that is not a member of <see cref="PseudoStates"/> gives <c>:&lt;state N&gt;</c>, with the value of each
    /// such bit for N. The name goes into the text with no escape. An empty compound gives the empty text.
    /// </para>
    /// </remarks>
    /// <returns>The CSS text of the compound.</returns>
    public override string ToString()
    {
        StringBuilder text = new();
        if (!Type.IsNone)
        {
            text.Append(TypeText(Type));
        }

        if (!Class.IsNone)
        {
            text.Append('.').Append(ClassText(Class));
        }

        if (Name is not null)
        {
            text.Append('#').Append(Name);
        }

        PseudoStates rest = States;
        foreach ((PseudoStates state, string stateText) in StateTexts)
        {
            if ((States & state) != PseudoStates.None)
            {
                text.Append(stateText);
                rest &= ~state;
            }
        }

        if (rest != PseudoStates.None)
        {
            text.Append(":<state ").Append(((int)rest).ToString(CultureInfo.InvariantCulture)).Append('>');
        }

        return text.ToString();
    }

    /// <summary>Compares each part of the compound with each part of another compound.</summary>
    /// <param name="other">The compound to compare.</param>
    /// <returns><see langword="true"/> when each part is equal.</returns>
    private bool EqualParts(in CompoundSelector other)
    {
        return Type == other.Type
            && Class == other.Class
            && States == other.States
            && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    /// <summary>Gives the name of a type id, or <c>&lt;type N&gt;</c> when the type table does not hold the id.</summary>
    /// <param name="type">The type id. It is not <see cref="ElementTypeId.None"/>.</param>
    /// <returns>The type name or the fallback text.</returns>
    private static string TypeText(ElementTypeId type)
    {
        return ElementType.TryGetName(type, out string? name)
            ? name
            : "<type " + type.Value.ToString(CultureInfo.InvariantCulture) + ">";
    }

    /// <summary>Gives the name of a class id, or <c>&lt;class N&gt;</c> when the class table does not hold the id.</summary>
    /// <param name="id">The class id. It is not <see cref="ClassId.None"/>.</param>
    /// <returns>The class name or the fallback text, with no dot.</returns>
    private static string ClassText(ClassId id)
    {
        return ClassName.TryGetName(id, out string? name)
            ? name
            : "<class " + id.Value.ToString(CultureInfo.InvariantCulture) + ">";
    }
}
