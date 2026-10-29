using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The bind methods of the widgets <see cref="Toggle"/>, <see cref="Slider"/>, and <see cref="TextField"/>. Each method
/// binds the value of a widget to a slice of a <see cref="Store{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// The methods are extension methods because the widgets live in <c>BrutalUi.Core</c>, and Core cannot name
/// <see cref="Store{T}"/>. Each method returns its element, so a build chain can continue after the bind.
/// </para>
/// <para>
/// A binding is one way: the store moves the widget. A user change still calls the delegate of the widget, and the
/// game writes the store there. The epic has no two-way helper. The write raises the version of the store one time,
/// and the next binding pass writes the same value back through the code setter. The setter compares and changes
/// nothing, so there is no loop (the No-Loop Rule). Each new version of the store writes each bound widget again, also
/// for a change of another slice, so a user change that the game does not write back to the store is lost at the next
/// change of the store.
/// </para>
/// <para>
/// Pass a <see langword="static"/> lambda or a cached method group as the selector. The selector must not allocate,
/// because the binding calls it in the update. Each bind allocates the binding at build time.
/// </para>
/// </remarks>
public static class WidgetBindings
{
    /// <summary>
    /// Binds the checked state of a toggle to a <see cref="bool"/> slice of a store.
    /// </summary>
    /// <remarks>
    /// The first <see cref="UiContext.Update(float)"/> after the bind shows the slice. Each later update shows the
    /// slice one time for each new version of the store. The binding writes <see cref="Toggle.Checked"/>. The code
    /// setter calls no delegate, so a store change calls no <see cref="Toggle.CheckedChanged"/>. A flip by the user
    /// still calls the delegate, and the game writes the store there.
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <param name="toggle">The toggle that shows the slice.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the slice from the value of the store.</param>
    /// <returns>The toggle.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toggle"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Toggle BindChecked<T>(this Toggle toggle, Store<T> store, Func<T, bool> selector)
    {
        ArgumentNullException.ThrowIfNull(toggle);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        toggle.AddBinding(store, CheckedHandler<T>.Apply, new StoreSlice<T, bool>(store, selector));
        return toggle;
    }

    /// <summary>
    /// Binds the value of a slider to a <see cref="float"/> slice of a store.
    /// </summary>
    /// <remarks>
    /// The first <see cref="UiContext.Update(float)"/> after the bind shows the slice. Each later update shows the
    /// slice one time for each new version of the store. The binding writes <see cref="Slider.Value"/>. The code
    /// setter calls no delegate, so a store change calls no <see cref="Slider.ValueChanged"/>. A change by the user
    /// still calls the delegate, and the game writes the store there. The slider applies its value rule (a clamp to
    /// the range and a step to the grid), so the slider can show a value that differs from the store.
    /// </remarks>
    /// <param name="slider">The slider that shows the slice.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the slice from the value of the store.</param>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <returns>The slider.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slider"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Slider BindValue<T>(this Slider slider, Store<T> store, Func<T, float> selector)
    {
        ArgumentNullException.ThrowIfNull(slider);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        slider.AddBinding(store, ValueHandler<T>.Apply, new StoreSlice<T, float>(store, selector));
        return slider;
    }

    /// <summary>
    /// Binds the text of a text field to a string slice of a store.
    /// </summary>
    /// <remarks>
    /// The first <see cref="UiContext.Update(float)"/> after the bind shows the slice. Each later update shows the
    /// slice one time for each new version of the store. The binding calls <see cref="TextField.SetText(string?)"/>.
    /// The code setter calls no delegate, so a store change calls no <see cref="TextField.TextChanged"/>. A change by
    /// the user still calls the delegate, and the game writes the store there. A new text from the store puts the
    /// caret of the field at the end and clears the selection. The field cuts a text at its capacity. A
    /// <see langword="null"/> slice gives an empty text.
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <param name="field">The text field that shows the slice.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the slice from the value of the store.</param>
    /// <returns>The text field.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static TextField BindText<T>(this TextField field, Store<T> store, Func<T, string?> selector)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        field.AddBinding(store, TextHandler<T>.Apply, new StoreSlice<T, string?>(store, selector));
        return field;
    }

    /// <summary>The holder of the handler of the toggle bindings of one store type.</summary>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    private static class CheckedHandler<T>
    {
        /// <summary>The handler of each binding of this closed type. The add thus allocates no delegate.</summary>
        internal static readonly BindingHandler Apply = Run;

        private static void Run(Element element, object? target)
        {
            ((Toggle)element).Checked = ((StoreSlice<T, bool>)target!).Read();
        }
    }

    /// <summary>The holder of the handler of the slider bindings of one store type.</summary>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    private static class ValueHandler<T>
    {
        /// <summary>The handler of each binding of this closed type. The add thus allocates no delegate.</summary>
        internal static readonly BindingHandler Apply = Run;

        private static void Run(Element element, object? target)
        {
            ((Slider)element).Value = ((StoreSlice<T, float>)target!).Read();
        }
    }

    /// <summary>The holder of the handler of the text field bindings of one store type.</summary>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    private static class TextHandler<T>
    {
        /// <summary>The handler of each binding of this closed type. The add thus allocates no delegate.</summary>
        internal static readonly BindingHandler Apply = Run;

        private static void Run(Element element, object? target)
        {
            ((TextField)element).SetText(((StoreSlice<T, string?>)target!).Read());
        }
    }
}
