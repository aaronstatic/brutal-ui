using System;

namespace BrutalUi.Core;

/// <summary>
/// Fluent extension methods that build a tree in one expression.
/// </summary>
/// <remarks>
/// Each method returns the element of the call, with its own type, so a derived element keeps its type in a chain.
/// The builder holds <c>Class</c> and <c>Child</c> only.
/// </remarks>
public static class ElementBuilder
{
    /// <summary>Adds a class to the element. The method interns the name first.</summary>
    /// <typeparam name="T">The type of the element.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="name">The class name.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static T Class<T>(this T element, string name)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        element.Classes.Add(ClassName.Intern(name));
        return element;
    }

    /// <summary>Adds a class to the element.</summary>
    /// <typeparam name="T">The type of the element.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="id">The class id.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is <see cref="ClassId.None"/>.</exception>
    public static T Class<T>(this T element, ClassId id)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        element.Classes.Add(id);
        return element;
    }

    /// <summary>Adds a child at the end of the child list of the element.</summary>
    /// <typeparam name="T">The type of the element.</typeparam>
    /// <param name="element">The parent element.</param>
    /// <param name="child">The child to add.</param>
    /// <returns>The parent element, not the child.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The child is a context root, or the add makes a cycle.</exception>
    public static T Child<T>(this T element, Element child)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        element.Children.Add(child);
        return element;
    }
}
