using System;

namespace BrutalUi.Core;

/// <summary>
/// The query by name on an element. <see cref="Find(ReadOnlySpan{byte})"/> and
/// <see cref="Find{T}(ReadOnlySpan{byte})"/> give the first element with a name in the subtree of the element,
/// the element included.
/// </summary>
/// <remarks>
/// <para>
/// The walk is recursive. It reads <see cref="ElementList.Count"/> and the indexer of
/// <see cref="ElementList"/>, so it uses no enumerator and allocates nothing. The compare runs on the internal <see cref="Utf8Text.Utf8Equals"/>, which encodes the
/// <see cref="Name"/> in chunks into a stack buffer. A second compare does not exist.
/// </para>
/// </remarks>
public partial class Element
{
    /// <summary>Gives the first element with a name in the subtree of this element, this element included.</summary>
    /// <remarks>
    /// <para>
    /// The walk checks the element itself first, then the children of each element in order, in depth-first
    /// pre-order. The first hit wins. The walk does not look above this element.
    /// </para>
    /// <para>
    /// The compare is ordinal on the UTF-8 form of <see cref="Name"/>, so <c>Score</c> and <c>score</c> are two
    /// names. An element with no name never matches. An empty name gives <see langword="null"/>.
    /// </para>
    /// <para>
    /// The call allocates 0 bytes, so a game can call it in a frame.
    /// </para>
    /// </remarks>
    /// <param name="name">The UTF-8 name of the element.</param>
    /// <returns>The first element with the name, or <see langword="null"/> when the subtree holds none.</returns>
    public Element? Find(ReadOnlySpan<byte> name)
    {
        return name.IsEmpty ? null : Walk(this, name);
    }

    /// <summary>
    /// Gives the first element with a name and the type <typeparamref name="T"/> in the subtree of this element,
    /// this element included.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The walk checks the element itself first, then the children of each element in order, in depth-first
    /// pre-order. The first hit wins. The walk does not look above this element.
    /// </para>
    /// <para>
    /// The compare is ordinal on the UTF-8 form of <see cref="Name"/>, so <c>Score</c> and <c>score</c> are two
    /// names. An element with no name never matches. An empty name gives <see langword="null"/>.
    /// </para>
    /// <para>
    /// An element with the name but another type does not match, and the walk does not stop there. The method
    /// gives the first element that has the name and the type <typeparamref name="T"/>.
    /// </para>
    /// <para>
    /// The call allocates 0 bytes, so a game can call it in a frame.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The element type that the result must have.</typeparam>
    /// <param name="name">The UTF-8 name of the element.</param>
    /// <returns>The first element with the name and the type, or <see langword="null"/> when none matches.</returns>
    public T? Find<T>(ReadOnlySpan<byte> name)
        where T : Element
    {
        return name.IsEmpty ? null : Walk<T>(this, name);
    }

    /// <summary>Walks a subtree in depth-first pre-order and stops at the first element with the name.</summary>
    /// <param name="element">The element to check.</param>
    /// <param name="name">The UTF-8 name of the element.</param>
    /// <returns>The element with the name, or <see langword="null"/>.</returns>
    private static Element? Walk(Element element, ReadOnlySpan<byte> name)
    {
        if (Matches(element, name))
        {
            return element;
        }

        ElementList children = element.Children;
        int count = children.Count;
        for (int i = 0; i < count; i++)
        {
            if (Walk(children[i], name) is Element hit)
            {
                return hit;
            }
        }

        return null;
    }

    /// <summary>
    /// Walks a subtree in depth-first pre-order and stops at the first element with the name and the type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The element type that the result must have.</typeparam>
    /// <param name="element">The element to check.</param>
    /// <param name="name">The UTF-8 name of the element.</param>
    /// <returns>The element with the name and the type, or <see langword="null"/>.</returns>
    private static T? Walk<T>(Element element, ReadOnlySpan<byte> name)
        where T : Element
    {
        if (Matches(element, name) && element is T hit)
        {
            return hit;
        }

        ElementList children = element.Children;
        int count = children.Count;
        for (int i = 0; i < count; i++)
        {
            if (Walk<T>(children[i], name) is T typedHit)
            {
                return typedHit;
            }
        }

        return null;
    }

    /// <summary>Checks whether an element has the name.</summary>
    /// <remarks>
    /// The length test rejects most misses before the encode, because one UTF-16 unit gives 1 to 3 UTF-8 bytes.
    /// <see cref="Utf8Text.Utf8Equals"/> compares the bytes with no allocation.
    /// </remarks>
    /// <param name="element">The element to check.</param>
    /// <param name="name">The UTF-8 name.</param>
    /// <returns><see langword="true"/> when the element has the name. Otherwise <see langword="false"/>.</returns>
    private static bool Matches(Element element, ReadOnlySpan<byte> name)
    {
        return element.Name is string s
            && s.Length <= name.Length
            && name.Length <= 3 * s.Length
            && Utf8Text.Utf8Equals(name, s);
    }
}
