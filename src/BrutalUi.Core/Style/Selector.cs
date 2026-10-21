using System;

namespace BrutalUi.Core;

/// <summary>
/// The part of a style rule that picks the elements. A selector picks each element of one type, each element with one
/// class, or each element of one type that also has one class.
/// </summary>
/// <remarks>
/// <para>
/// Create a selector with <see cref="ForClass(ClassId)"/>, <see cref="ForType(ElementTypeId)"/>, or
/// <see cref="ForTypeAndClass(ElementTypeId, ClassId)"/>. The value <c>default(Selector)</c> holds no part and
/// matches no element. <see cref="StyleSheet.Builder.Add(in Selector, in StyleProperties)"/> rejects it.
/// </para>
/// <para>
/// The type part compares the id of <see cref="Element.TypeId"/> with <see cref="Type"/>. The comparison is exact. A
/// subclass that does not override <see cref="Element.TypeId"/> has the id of its base class and matches the type
/// selector of the base class. A subclass that overrides the id does not match it. The element name, the
/// pseudo-states, and the parent chain join the selector in the selector-matching feature.
/// </para>
/// <para>
/// <see cref="Specificity"/> is the one place that holds the weights: 10 for each class and 1 for each type.
/// </para>
/// <para>
/// The decimal sum keeps the CSS order only while each tier count stays below 10. The selector-matching feature must
/// replace the decimal weights with bit fields, or cap the counts, before it adds the name and the pseudo-states. One
/// selector can then hold 12 units of the class tier (one class and five pseudo-states in each of two compounds), and
/// 12 units of the class tier would win over one name at the weight 100.
/// </para>
/// </remarks>
public readonly struct Selector
{
    /// <summary>The weight of one class in <see cref="Specificity"/>.</summary>
    private const int ClassWeight = 10;

    /// <summary>The weight of one type in <see cref="Specificity"/>.</summary>
    private const int TypeWeight = 1;

    private Selector(ElementTypeId type, ClassId id)
    {
        Type = type;
        Class = id;
    }

    /// <summary>Gets the class that the element must hold. <see cref="ClassId.None"/> when the selector has no class part.</summary>
    public ClassId Class { get; }

    /// <summary>Gets the type that the element must have. <see cref="ElementTypeId.None"/> when the selector has no type part.</summary>
    public ElementTypeId Type { get; }

    /// <summary>
    /// Gets the specificity: the weighted sum of the parts of the selector. A class counts 10 and a type counts 1.
    /// <c>default(Selector)</c> gives 0.
    /// </summary>
    /// <remarks>A rule with a higher specificity wins over a rule with a lower specificity.</remarks>
    public int Specificity => (Class.IsNone ? 0 : ClassWeight) + (Type.IsNone ? 0 : TypeWeight);

    /// <summary>Gets a value that shows whether the selector holds no part. Such a selector matches nothing.</summary>
    internal bool IsEmpty => Class.IsNone && Type.IsNone;

    /// <summary>Creates a selector that matches each element with a class, as <c>.panel</c> does in CSS.</summary>
    /// <param name="id">The class id. Get it from <see cref="ClassName.Intern(string)"/>.</param>
    /// <returns>The selector. Its specificity is 10.</returns>
    /// <exception cref="ArgumentException"><paramref name="id"/> is <see cref="ClassId.None"/>.</exception>
    public static Selector ForClass(ClassId id)
    {
        if (id.IsNone)
        {
            throw new ArgumentException("The id is ClassId.None. Get an id from ClassName.Intern.", nameof(id));
        }

        return new Selector(ElementTypeId.None, id);
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

        return new Selector(type, ClassId.None);
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
    /// <returns>The selector. Its specificity is 11 when both parts are real.</returns>
    /// <exception cref="ArgumentException">Both <paramref name="type"/> and <paramref name="id"/> are none.</exception>
    public static Selector ForTypeAndClass(ElementTypeId type, ClassId id)
    {
        if (type.IsNone && id.IsNone)
        {
            throw new ArgumentException(
                "The type is ElementTypeId.None and the id is ClassId.None. Give at least one real part.",
                nameof(type));
        }

        return new Selector(type, id);
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
    /// <see langword="true"/> when <paramref name="elementType"/> equals <see cref="Type"/> (if the selector has a type)
    /// and the element holds <see cref="Class"/> (if the selector has a class). <see langword="false"/> for
    /// <c>default(Selector)</c>.
    /// </returns>
    internal bool Matches(Element element, ElementTypeId elementType)
    {
        if (IsEmpty)
        {
            return false;
        }

        if (!Type.IsNone && Type != elementType)
        {
            return false;
        }

        return Class.IsNone || element.ClassStorage.Contains(Class);
    }
}
