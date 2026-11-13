using System;

namespace BrutalUi.Core;

/// <summary>
/// Fluent extension methods that build a tree in one expression.
/// </summary>
/// <remarks>
/// Each method returns the element of the call, with its own type, so a derived element keeps its type in a chain.
/// The builder has one file for each group: the element group, the text group, the widget group, and the event group.
/// A value method writes one property or calls one setter method.
/// A delegate method adds one handler to one slot with <c>+=</c>.
/// </remarks>
public static partial class ElementBuilder
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

    /// <summary>Sets the name of the element.</summary>
    /// <remarks>
    /// A <see langword="null"/> name clears the name. An empty name is valid, and no name selector matches it.
    /// The set marks the element and its full subtree for style, as the <see cref="Element.Name"/> property does.
    /// A set to the same name marks nothing.
    /// </remarks>
    /// <typeparam name="T">The type of the element.</typeparam>
    /// <param name="element">The element to name.</param>
    /// <param name="name">The name, or <see langword="null"/> for no name.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public static T Name<T>(this T element, string? name)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        element.Name = name;
        return element;
    }

    /// <summary>Sets whether the element can be a target of picking.</summary>
    /// <remarks>
    /// The default of <see cref="Element.IsPickable"/> is <see langword="true"/>.
    /// A change sets no dirty bit, as the property states.
    /// </remarks>
    /// <typeparam name="T">The type of the element.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="value">The new value of <see cref="Element.IsPickable"/>.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public static T Pickable<T>(this T element, bool value)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        element.IsPickable = value;
        return element;
    }

    /// <summary>Sets whether the element can hold the keyboard focus.</summary>
    /// <remarks>
    /// The default of <see cref="Element.IsFocusable"/> is <see langword="false"/>.
    /// A change sets no dirty bit, as the property states.
    /// </remarks>
    /// <typeparam name="T">The type of the element.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="value">The new value of <see cref="Element.IsFocusable"/>.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public static T Focusable<T>(this T element, bool value)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        element.IsFocusable = value;
        return element;
    }
}
