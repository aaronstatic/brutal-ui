using System;

namespace BrutalUi.Core;

/// <summary>
/// The event group of the fluent builder: the fourteen delegate methods of the <see cref="Element"/> event slots.
/// </summary>
/// <remarks>
/// <para>
/// Each method toggles the <c>On</c> prefix of its slot: the slot <c>OnPointerDown</c> gives the method
/// <c>PointerDown</c>. A delegate property is invocable, so C# binds <c>element.OnPointerDown(h)</c> to a call of the
/// delegate in the property, and never to a method named after the slot.
/// </para>
/// <para>
/// Each method adds the handler with <c>+=</c>, so a handler that the game set before stays, and two calls add two
/// handlers in the order of the call. The first handler on the element creates the side object of the slots one time,
/// as <c>+=</c> on the property does. Register handlers at build time, and create each delegate one time.
/// </para>
/// <para>
/// A bubble slot (a name without <c>Capture</c>) runs in the target phase and the bubble phase. A capture slot runs
/// in the capture phase on an ancestor of the target, and before the bubble slot in the target phase.
/// </para>
/// </remarks>
public static partial class ElementBuilder
{
    /// <summary>Adds a bubble handler for pointer-move events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnPointerMove"/> with <c>+=</c>, so a handler that the game set before
    /// stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T PointerMove<T>(this T element, UiEventHandler<PointerEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnPointerMove += handler;
        return element;
    }

    /// <summary>Adds a capture handler for pointer-move events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnPointerMoveCapture"/> with <c>+=</c>, so a handler that the game set
    /// before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T PointerMoveCapture<T>(this T element, UiEventHandler<PointerEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnPointerMoveCapture += handler;
        return element;
    }

    /// <summary>Adds a bubble handler for pointer-down events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnPointerDown"/> with <c>+=</c>, so a handler that the game set before
    /// stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T PointerDown<T>(this T element, UiEventHandler<PointerEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnPointerDown += handler;
        return element;
    }

    /// <summary>Adds a capture handler for pointer-down events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnPointerDownCapture"/> with <c>+=</c>, so a handler that the game set
    /// before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T PointerDownCapture<T>(this T element, UiEventHandler<PointerEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnPointerDownCapture += handler;
        return element;
    }

    /// <summary>Adds a bubble handler for pointer-up events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnPointerUp"/> with <c>+=</c>, so a handler that the game set before
    /// stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T PointerUp<T>(this T element, UiEventHandler<PointerEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnPointerUp += handler;
        return element;
    }

    /// <summary>Adds a capture handler for pointer-up events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnPointerUpCapture"/> with <c>+=</c>, so a handler that the game set
    /// before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T PointerUpCapture<T>(this T element, UiEventHandler<PointerEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnPointerUpCapture += handler;
        return element;
    }

    /// <summary>Adds a bubble handler for scroll events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnScroll"/> with <c>+=</c>, so a handler that the game set before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T Scroll<T>(this T element, UiEventHandler<ScrollEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnScroll += handler;
        return element;
    }

    /// <summary>Adds a capture handler for scroll events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnScrollCapture"/> with <c>+=</c>, so a handler that the game set before
    /// stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T ScrollCapture<T>(this T element, UiEventHandler<ScrollEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnScrollCapture += handler;
        return element;
    }

    /// <summary>Adds a bubble handler for key-down events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnKeyDown"/> with <c>+=</c>, so a handler that the game set before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T KeyDown<T>(this T element, UiEventHandler<KeyEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnKeyDown += handler;
        return element;
    }

    /// <summary>Adds a capture handler for key-down events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnKeyDownCapture"/> with <c>+=</c>, so a handler that the game set
    /// before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T KeyDownCapture<T>(this T element, UiEventHandler<KeyEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnKeyDownCapture += handler;
        return element;
    }

    /// <summary>Adds a bubble handler for key-up events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnKeyUp"/> with <c>+=</c>, so a handler that the game set before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T KeyUp<T>(this T element, UiEventHandler<KeyEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnKeyUp += handler;
        return element;
    }

    /// <summary>Adds a capture handler for key-up events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnKeyUpCapture"/> with <c>+=</c>, so a handler that the game set before
    /// stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T KeyUpCapture<T>(this T element, UiEventHandler<KeyEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnKeyUpCapture += handler;
        return element;
    }

    /// <summary>Adds a bubble handler for char events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnChar"/> with <c>+=</c>, so a handler that the game set before stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T Char<T>(this T element, UiEventHandler<CharEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnChar += handler;
        return element;
    }

    /// <summary>Adds a capture handler for char events and returns the element.</summary>
    /// <remarks>
    /// The method adds to <see cref="Element.OnCharCapture"/> with <c>+=</c>, so a handler that the game set before
    /// stays.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="element">The element to change.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The same element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static T CharCapture<T>(this T element, UiEventHandler<CharEvent> handler)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(handler);
        element.OnCharCapture += handler;
        return element;
    }
}
