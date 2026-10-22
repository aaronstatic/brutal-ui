using System;
using System.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The part of a style rule that picks the elements. A selector holds the subject compound, and it can also hold an
/// ancestor compound and a <see cref="SelectorCombinator"/>. The element must hold each part of the subject: the type,
/// the class, the name, and the pseudo-states.
/// </summary>
/// <remarks>
/// <para>
/// Create a selector with <see cref="For(in CompoundSelector)"/>, or with one of the shortcuts
/// <see cref="ForClass(ClassId)"/>, <see cref="ForName(string)"/>, <see cref="ForType(ElementTypeId)"/>, and
/// <see cref="ForTypeAndClass(ElementTypeId, ClassId)"/>. Create a chain with
/// <see cref="Child(in CompoundSelector, in CompoundSelector)"/> or
/// <see cref="Descendant(in CompoundSelector, in CompoundSelector)"/>. The value <c>default(Selector)</c> holds no
/// part and matches no element. <see cref="StyleSheet.Builder.Add(in Selector, in StyleProperties)"/> rejects it.
/// </para>
/// <para>
/// The type part compares the id of <see cref="Element.TypeId"/> with <see cref="Type"/>. The comparison is exact.
/// <see cref="CompoundSelector"/> tells the rule for a subclass. The name part compares <see cref="Element.Name"/> with
/// an ordinal compare, and <see cref="ForName(string)"/> creates a selector with only a name part.
/// </para>
/// <para>
/// A chain has a fixed bound. A child chain reads the parent only. A descendant chain reads the parent and then the
/// grandparent, and the walk stops there. Thus each chain rule reads at most two ancestors. A chain reads the tree: the
/// type, the classes, the name, and the pseudo-states of the ancestor. It does not read the computed style of the
/// ancestor. Thus the order of the style pass does not change the result.
/// </para>
/// <para>
/// <see cref="Specificity"/> is the one place that holds the weights. Each tier holds one byte of the value: the name
/// tier holds bits 16 to 23, the class tier holds bits 8 to 15, and the type tier holds bits 0 to 7. The name tier
/// counts each compound with a name. The class tier counts each class and each pseudo-state. The type tier counts each
/// compound with a type. Thus one name (65536) wins over the largest chain without a name.
/// </para>
/// <para>
/// A count of a higher tier wins over each count of a lower tier while each count stays below 256. The largest
/// selector of the grammar holds 2 names, 12 units of the class tier (one class and five pseudo-states in each of two
/// compounds), and 2 types. Thus each count stays far below 256, and the value is never negative.
/// </para>
/// <para>
/// Two selectors are equal when the subject, the ancestor, and the combinator are equal. The equality members
/// allocate nothing. <see cref="Equals(object?)"/> boxes only when the caller gives a boxed value. A selector with one
/// compound has an empty ancestor, so two such selectors with equal subjects are equal. <see cref="ToString"/> gives the
/// CSS text of the selector. It allocates and reads the global name tables, so use it for test output and debug output
/// only.
/// </para>
/// </remarks>
public readonly struct Selector : IEquatable<Selector>
{
    /// <summary>The position of the lowest bit of the name tier in <see cref="Specificity"/>.</summary>
    private const int NameShift = 16;

    /// <summary>The position of the lowest bit of the class tier in <see cref="Specificity"/>.</summary>
    private const int ClassShift = 8;

    /// <summary>The position of the lowest bit of the type tier in <see cref="Specificity"/>.</summary>
    private const int TypeShift = 0;

    /// <summary>Each pseudo-state that a compound can hold.</summary>
    private const PseudoStates KnownStates =
        PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus | PseudoStates.Disabled | PseudoStates.Checked;

    /// <summary>The compound that the element itself must match.</summary>
    private readonly CompoundSelector _subject;

    /// <summary>
    /// The compound that an ancestor must match: the parent for <see cref="SelectorCombinator.Child"/>, and the parent
    /// or the grandparent for <see cref="SelectorCombinator.Descendant"/>. Empty when the selector has no chain.
    /// </summary>
    private readonly CompoundSelector _ancestor;

    /// <summary>The link between <see cref="_ancestor"/> and <see cref="_subject"/>.</summary>
    private readonly SelectorCombinator _combinator;

    private Selector(in CompoundSelector subject)
    {
        _subject = subject;
    }

    private Selector(in CompoundSelector ancestor, SelectorCombinator combinator, in CompoundSelector subject)
    {
        _subject = subject;
        _ancestor = ancestor;
        _combinator = combinator;
    }

    /// <summary>
    /// Gets the compound that the element itself must match. <c>default(CompoundSelector)</c> for
    /// <c>default(Selector)</c>.
    /// </summary>
    public CompoundSelector Subject => _subject;

    /// <summary>
    /// Gets the compound that an ancestor must match: the parent for <see cref="SelectorCombinator.Child"/>, and the
    /// parent or the grandparent for <see cref="SelectorCombinator.Descendant"/>. <c>default(CompoundSelector)</c> when
    /// <see cref="Combinator"/> is <see cref="SelectorCombinator.None"/>.
    /// </summary>
    public CompoundSelector Ancestor => _ancestor;

    /// <summary>
    /// Gets the link between <see cref="Ancestor"/> and <see cref="Subject"/>. <see cref="SelectorCombinator.None"/>
    /// for a selector with one compound and for <c>default(Selector)</c>.
    /// </summary>
    public SelectorCombinator Combinator => _combinator;

    /// <summary>
    /// Gets the class that the element must hold. <see cref="ClassId.None"/> when the subject has no class part.
    /// </summary>
    public ClassId Class => _subject.Class;

    /// <summary>
    /// Gets the type that the element must have. <see cref="ElementTypeId.None"/> when the subject has no type part.
    /// </summary>
    public ElementTypeId Type => _subject.Type;

    /// <summary>
    /// Gets the specificity. Each tier holds one byte: <c>(names &lt;&lt; 16) | ((classes + states) &lt;&lt; 8) | types</c>.
    /// Each count is the sum over the subject and the ancestor. <c>default(Selector)</c> gives 0.
    /// </summary>
    /// <remarks>
    /// A rule with a higher specificity wins over a rule with a lower specificity. For example, <c>Label</c> gives 1,
    /// <c>.a</c> and <c>:hover</c> give 256, <c>Label.a</c> gives 257, <c>.a:hover</c> gives 512, and <c>#title</c>
    /// gives 65536. The chain
    /// <c>Label .b</c> gives 257, <c>.a &gt; .b</c> gives 512, and <c>.a:hover .b:focus</c> gives 1024.
    /// </remarks>
    public int Specificity => Tiers(in _subject) + Tiers(in _ancestor);

    /// <summary>Gets a value that shows whether the selector holds no part. Such a selector matches nothing.</summary>
    internal bool IsEmpty => _subject.IsEmpty;

    /// <summary>
    /// Creates a selector from one compound, as <c>Label.button:hover</c> is in CSS. The element must hold each part
    /// of the compound.
    /// </summary>
    /// <param name="subject">The compound that the element must match. Set at least one part.</param>
    /// <returns>The selector.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="subject"/> holds no part, <see cref="CompoundSelector.Name"/> is empty, or
    /// <see cref="CompoundSelector.States"/> holds a bit that is not a member of <see cref="PseudoStates"/>.
    /// </exception>
    public static Selector For(in CompoundSelector subject)
    {
        ThrowIfWrong(in subject, nameof(subject));
        return new Selector(in subject);
    }

    /// <summary>
    /// Creates a child chain, as <c>.a &gt; .b</c> is in CSS. The element must match <paramref name="subject"/>, and
    /// its parent must match <paramref name="parent"/>.
    /// </summary>
    /// <remarks>The match reads the parent only. A root has no parent, so it matches no child chain.</remarks>
    /// <param name="parent">The compound that the parent must match. Set at least one part.</param>
    /// <param name="subject">The compound that the element must match. Set at least one part.</param>
    /// <returns>The selector. Its specificity is the sum of the tiers of both compounds.</returns>
    /// <exception cref="ArgumentException">
    /// A compound holds no part, its <see cref="CompoundSelector.Name"/> is empty, or its
    /// <see cref="CompoundSelector.States"/> holds a bit that is not a member of <see cref="PseudoStates"/>. The
    /// parameter name tells the slot. The method checks <paramref name="parent"/> first.
    /// </exception>
    public static Selector Child(in CompoundSelector parent, in CompoundSelector subject)
    {
        ThrowIfWrong(in parent, nameof(parent));
        ThrowIfWrong(in subject, nameof(subject));
        return new Selector(in parent, SelectorCombinator.Child, in subject);
    }

    /// <summary>
    /// Creates a descendant chain, as <c>.a .b</c> is in CSS. The element must match <paramref name="subject"/>, and
    /// its parent or its grandparent must match <paramref name="ancestor"/>.
    /// </summary>
    /// <remarks>
    /// The walk reads the parent and then the grandparent, and it stops there. An ancestor above the grandparent does
    /// not give a match. A root has no parent, so it matches no descendant chain.
    /// </remarks>
    /// <param name="ancestor">The compound that the parent or the grandparent must match. Set at least one part.</param>
    /// <param name="subject">The compound that the element must match. Set at least one part.</param>
    /// <returns>The selector. Its specificity is the sum of the tiers of both compounds.</returns>
    /// <exception cref="ArgumentException">
    /// A compound holds no part, its <see cref="CompoundSelector.Name"/> is empty, or its
    /// <see cref="CompoundSelector.States"/> holds a bit that is not a member of <see cref="PseudoStates"/>. The
    /// parameter name tells the slot. The method checks <paramref name="ancestor"/> first.
    /// </exception>
    public static Selector Descendant(in CompoundSelector ancestor, in CompoundSelector subject)
    {
        ThrowIfWrong(in ancestor, nameof(ancestor));
        ThrowIfWrong(in subject, nameof(subject));
        return new Selector(in ancestor, SelectorCombinator.Descendant, in subject);
    }

    /// <summary>Creates a selector that matches each element with a class, as <c>.panel</c> does in CSS.</summary>
    /// <param name="id">The class id. Get it from <see cref="ClassName.Intern(string)"/>.</param>
    /// <returns>The selector. Its specificity is 256.</returns>
    /// <exception cref="ArgumentException"><paramref name="id"/> is <see cref="ClassId.None"/>.</exception>
    public static Selector ForClass(ClassId id)
    {
        if (id.IsNone)
        {
            throw new ArgumentException("The id is ClassId.None. Get an id from ClassName.Intern.", nameof(id));
        }

        return new Selector(new CompoundSelector { Class = id });
    }

    /// <summary>Creates a selector that matches each element with a name, as <c>#title</c> does in CSS.</summary>
    /// <remarks>The compare is ordinal and case-sensitive. An element with a <see langword="null"/> name matches no name rule.</remarks>
    /// <param name="name">The name of the element. Give a name that is not empty.</param>
    /// <returns>The selector. Its specificity is 65536.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public static Selector ForName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("The name is null or empty. Give the name of an element.", nameof(name));
        }

        return new Selector(new CompoundSelector { Name = name });
    }

    /// <summary>Creates a selector that matches each element of a type, as <c>Label</c> does in CSS.</summary>
    /// <param name="type">The type id. Get it from <see cref="ElementType.Intern(string)"/> or <see cref="Element.TypeId"/>.</param>
    /// <returns>The selector. Its specificity is 1.</returns>
    /// <exception cref="ArgumentException"><paramref name="type"/> is <see cref="ElementTypeId.None"/>.</exception>
    public static Selector ForType(ElementTypeId type)
    {
        if (type.IsNone)
        {
            throw new ArgumentException("The type is ElementTypeId.None. Get an id from ElementType.Intern.", nameof(type));
        }

        return new Selector(new CompoundSelector { Type = type });
    }

    /// <summary>
    /// Creates a selector that matches each element of a type that also holds a class, as <c>Label.title</c> does in CSS.
    /// </summary>
    /// <remarks>
    /// One part can be none. A none type with a real class gives the same selector as <see cref="ForClass(ClassId)"/>.
    /// A real type with a none class gives the same selector as <see cref="ForType(ElementTypeId)"/>.
    /// </remarks>
    /// <param name="type">The type id.</param>
    /// <param name="id">The class id.</param>
    /// <returns>The selector. Its specificity is 257 when both parts are real.</returns>
    /// <exception cref="ArgumentException">Both <paramref name="type"/> and <paramref name="id"/> are none.</exception>
    public static Selector ForTypeAndClass(ElementTypeId type, ClassId id)
    {
        if (type.IsNone && id.IsNone)
        {
            throw new ArgumentException(
                "The type is ElementTypeId.None and the id is ClassId.None. Give at least one real part.",
                nameof(type));
        }

        return new Selector(new CompoundSelector { Type = type, Class = id });
    }

    /// <summary>Checks whether an element matches the selector. The method reads <see cref="Element.TypeId"/>.</summary>
    /// <param name="element">The element to check.</param>
    /// <returns><see langword="true"/> when the element matches each part. <see langword="false"/> for <c>default(Selector)</c>.</returns>
    internal bool Matches(Element element)
    {
        return Matches(element, element.TypeId);
    }

    /// <summary>Checks whether an element matches the selector.</summary>
    /// <param name="element">The element to check.</param>
    /// <param name="elementType">
    /// The type id of the element. The style pass reads <see cref="Element.TypeId"/> one time for each element and
    /// passes the value to each rule.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the element holds each part of <see cref="Subject"/>, and the chain finds an
    /// ancestor that holds each part of <see cref="Ancestor"/>. <see langword="false"/> for <c>default(Selector)</c>.
    /// </returns>
    /// <remarks>
    /// The method tests the subject first, because most rules miss there. A child chain then reads the parent. A
    /// descendant chain reads the parent and then the grandparent, and the walk stops there. The method reads the tree,
    /// not the computed style, and it makes no allocation.
    /// </remarks>
    internal bool Matches(Element element, ElementTypeId elementType)
    {
        if (IsEmpty)
        {
            return false;
        }

        if (!_subject.Matches(element, elementType))
        {
            return false;
        }

        switch (_combinator)
        {
            case SelectorCombinator.Child:
            {
                Element? parent = element.Parent;
                return parent is not null && _ancestor.Matches(parent);
            }

            case SelectorCombinator.Descendant:
            {
                Element? parent = element.Parent;
                if (parent is null)
                {
                    return false;
                }

                if (_ancestor.Matches(parent))
                {
                    return true;
                }

                // The walk stops at the grandparent.
                Element? grandparent = parent.Parent;
                return grandparent is not null && _ancestor.Matches(grandparent);
            }

            default:
                return true;
        }
    }

    /// <summary>Compares two selectors.</summary>
    /// <param name="left">The first selector.</param>
    /// <param name="right">The second selector.</param>
    /// <returns><see langword="true"/> when the subject, the ancestor, and the combinator are equal.</returns>
    public static bool operator ==(in Selector left, in Selector right) => left.EqualParts(in right);

    /// <summary>Compares two selectors.</summary>
    /// <param name="left">The first selector.</param>
    /// <param name="right">The second selector.</param>
    /// <returns><see langword="true"/> when the subject, the ancestor, or the combinator is not equal.</returns>
    public static bool operator !=(in Selector left, in Selector right) => !left.EqualParts(in right);

    /// <summary>Compares the selector with another selector.</summary>
    /// <remarks>
    /// The method compares <see cref="Subject"/>, <see cref="Ancestor"/>, and <see cref="Combinator"/> with the
    /// equality of <see cref="CompoundSelector"/>. The name compare is ordinal. The method allocates nothing.
    /// </remarks>
    /// <param name="other">The selector to compare.</param>
    /// <returns><see langword="true"/> when the subject, the ancestor, and the combinator are equal.</returns>
    public bool Equals(Selector other) => EqualParts(in other);

    /// <summary>Compares the selector with an object.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is a <see cref="Selector"/> with an equal subject, an equal
    /// ancestor, and an equal combinator.
    /// </returns>
    public override bool Equals(object? obj) => obj is Selector other && EqualParts(in other);

    /// <summary>Gives a hash code from the subject, the ancestor, and the combinator.</summary>
    /// <remarks>Two equal selectors give the same hash code. The method allocates nothing.</remarks>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(_subject.GetHashCode(), _ancestor.GetHashCode(), (int)_combinator);
    }

    /// <summary>
    /// Gives the CSS text of the selector: <c>subject</c>, <c>ancestor &gt; subject</c> for a child chain, or
    /// <c>ancestor subject</c> for a descendant chain. For example, <c>.a:hover &gt; .b</c>.
    /// <c>default(Selector)</c> gives <c>(empty)</c>.
    /// </summary>
    /// <remarks>
    /// The method allocates and reads the global name tables. Use it for test output and debug output only. It never
    /// throws: <see cref="CompoundSelector.ToString"/> tells the text of an id that a table does not hold. The text
    /// uses the grammar of the stylesheet parser, so the parser reads the text back to an equal selector for each
    /// name that the grammar of section 11 can read (an identifier for a type or a class, <c>[A-Za-z0-9_-]+</c> for a
    /// name). Another name gives a text that the parser does not read back.
    /// </remarks>
    /// <returns>The CSS text of the selector.</returns>
    public override string ToString()
    {
        if (IsEmpty)
        {
            return "(empty)";
        }

        return _combinator switch
        {
            SelectorCombinator.Child => _ancestor.ToString() + " > " + _subject.ToString(),
            SelectorCombinator.Descendant => _ancestor.ToString() + " " + _subject.ToString(),
            _ => _subject.ToString(),
        };
    }

    /// <summary>Compares the subject, the ancestor, and the combinator with those of another selector.</summary>
    /// <param name="other">The selector to compare.</param>
    /// <returns><see langword="true"/> when the three parts are equal.</returns>
    private bool EqualParts(in Selector other)
    {
        return _combinator == other._combinator
            && _subject == other._subject
            && _ancestor == other._ancestor;
    }

    /// <summary>
    /// Throws when a compound holds no part, holds an empty name, or holds a state bit that is not a member of
    /// <see cref="PseudoStates"/>.
    /// </summary>
    /// <remarks>
    /// The method checks for an empty compound first, for an empty name second, and for an unknown state bit third.
    /// </remarks>
    /// <param name="compound">The compound to check.</param>
    /// <param name="paramName">The name of the parameter that holds the compound. It tells the slot.</param>
    /// <exception cref="ArgumentException">The compound is wrong.</exception>
    private static void ThrowIfWrong(in CompoundSelector compound, string paramName)
    {
        if (compound.IsEmpty)
        {
            throw new ArgumentException(
                "The compound holds no part, so it matches no element. Set at least one of Type, Class, Name, and States.",
                paramName);
        }

        if (compound.Name is { Length: 0 })
        {
            throw new ArgumentException(
                "The compound holds an empty name, which matches no element. Set Name to null for no name part, or give a real name.",
                paramName);
        }

        if ((compound.States & ~KnownStates) != PseudoStates.None)
        {
            throw new ArgumentException(
                "The compound holds a state bit that is not a member of PseudoStates. Use only Hover, Active, Focus, Disabled, and Checked.",
                paramName);
        }
    }

    /// <summary>Gives the name tier, the class tier, and the type tier of one compound, each in its own byte.</summary>
    /// <remarks>
    /// The tiers of two compounds add with <c>+</c>, because no tier count of the grammar reaches 256. The name tier
    /// counts 1 for a compound with a name part, at <see cref="NameShift"/>.
    /// </remarks>
    /// <param name="compound">The compound to count.</param>
    /// <returns>The tiers of the compound. 0 for an empty compound.</returns>
    private static int Tiers(in CompoundSelector compound)
    {
        int classes = (compound.Class.IsNone ? 0 : 1) + BitOperations.PopCount((uint)compound.States);
        int types = compound.Type.IsNone ? 0 : 1;
        int names = compound.Name is null ? 0 : 1;
        return (names << NameShift) | (classes << ClassShift) | (types << TypeShift);
    }
}
