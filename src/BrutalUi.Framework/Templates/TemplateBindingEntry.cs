using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// One registered selector of a <see cref="TemplateBindings"/> key table. An entry binds the element that its selector
/// fits.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The entry rule.</strong> The generic types close at the <c>Add*</c> call of
/// <see cref="TemplateBindings"/>, so a sealed entry holds a concrete selector and the handler needs no reflection and
/// no type test of a selector. A sealed entry overrides <c>TryBind</c> for the element types that its selector fits,
/// and calls the public bind method of the element. The base returns <see langword="false"/> for every target, so a
/// <see langword="false"/> result is the type-mismatch case.
/// </para>
/// <para>
/// <strong>The selector kind.</strong> Each entry names its selector through <c>SelectorKind</c>, in the words of a
/// type-mismatch message: <c>a string</c>, <c>a number</c>, <c>a bool</c>, or <c>a float</c>.
/// </para>
/// <para>
/// The entries are internal. <see cref="TemplateBindings"/> is the public surface.
/// </para>
/// </remarks>
internal abstract class TemplateBindingEntry
{
    /// <summary>Gets the kind of the selector, in the words of a type-mismatch message.</summary>
    /// <remarks>An example is <c>a bool</c>. The handler of <see cref="TemplateBindings"/> puts the value in the
    /// message that names the selector of a key.</remarks>
    public abstract string SelectorKind { get; }

    /// <summary>Tries to bind the text of a label.</summary>
    /// <param name="label">The label that the loader made.</param>
    /// <returns><see langword="true"/> when this entry holds a selector that fits a label.</returns>
    internal virtual bool TryBind(Label label) => false;

    /// <summary>Tries to bind the text of a text field.</summary>
    /// <param name="field">The text field that the loader made.</param>
    /// <returns><see langword="true"/> when this entry holds a selector that fits a text field.</returns>
    internal virtual bool TryBind(TextField field) => false;

    /// <summary>Tries to bind the checked state of a toggle.</summary>
    /// <param name="toggle">The toggle that the loader made.</param>
    /// <returns><see langword="true"/> when this entry holds a selector that fits a toggle.</returns>
    internal virtual bool TryBind(Toggle toggle) => false;

    /// <summary>Tries to bind the value of a slider.</summary>
    /// <param name="slider">The slider that the loader made.</param>
    /// <returns><see langword="true"/> when this entry holds a selector that fits a slider.</returns>
    internal virtual bool TryBind(Slider slider) => false;
}

/// <summary>
/// A number key of a <see cref="TemplateBindings"/> table. The entry binds the text of a <see cref="Label"/> through
/// the number overload of <see cref="LabelBindings.BindText{T, TValue}(Label, Store{T}, Func{T, TValue}, string?, IFormatProvider?)"/>.
/// </summary>
/// <remarks>
/// <para>
/// The entry serves a label only. A number key on a text field is the mismatch case: the entry does not override
/// <see cref="TemplateBindingEntry.TryBind(TextField)"/>, and the base gives <see langword="false"/>.
/// </para>
/// <para>
/// The format and the provider come from the register call, not from the template. The bind copies nothing that a
/// frame later rereads: the binding object holds these four fields, as <c>LabelBindings.NumberText</c> does.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value of the store.</typeparam>
/// <typeparam name="TValue">The type of the slice. It implements <see cref="IUtf8SpanFormattable"/>.</typeparam>
internal sealed class NumberTextEntry<T, TValue> : TemplateBindingEntry
    where TValue : IUtf8SpanFormattable
{
    /// <summary>The store.</summary>
    private readonly Store<T> _store;

    /// <summary>The selector of the number slice.</summary>
    private readonly Func<T, TValue> _selector;

    /// <summary>The format string, or <see langword="null"/>.</summary>
    private readonly string? _format;

    /// <summary>The format provider, or <see langword="null"/>.</summary>
    private readonly IFormatProvider? _provider;

    /// <summary>Creates the entry.</summary>
    /// <param name="store">The store.</param>
    /// <param name="selector">The selector of the slice.</param>
    /// <param name="format">The format string, or <see langword="null"/>.</param>
    /// <param name="provider">The format provider, or <see langword="null"/>.</param>
    internal NumberTextEntry(Store<T> store, Func<T, TValue> selector, string? format, IFormatProvider? provider)
    {
        _store = store;
        _selector = selector;
        _format = format;
        _provider = provider;
    }

    /// <summary>Gets the kind of the selector: <c>a number</c>.</summary>
    public override string SelectorKind => "a number";

    /// <summary>Binds the text of the label to the slice, with the format and the provider of the register call.</summary>
    /// <param name="label">The label that the loader made.</param>
    /// <returns>Always <see langword="true"/>: the entry holds a selector that fits a label.</returns>
    internal override bool TryBind(Label label)
    {
        label.BindText(_store, _selector, _format, _provider);
        return true;
    }
}

/// <summary>
/// A string key of a <see cref="TemplateBindings"/> table. The entry binds the text of a <see cref="Label"/> and the
/// text of a <see cref="TextField"/> through the string overload of
/// <see cref="LabelBindings.BindText{T}(Label, Store{T}, Func{T, string?})"/> and the text bind of
/// <see cref="WidgetBindings.BindText{T}(TextField, Store{T}, Func{T, string?})"/>.
/// </summary>
/// <remarks>
/// <para>
/// One string key serves a label and a text field. The entry holds no format and no provider: the slice is a
/// <see cref="string"/>. A number key, by contrast, serves a label only.
/// </para>
/// <para>
/// The bind adds one binding to each element, so a label and a field with one key each hold one binding.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value of the store.</typeparam>
internal sealed class StringTextEntry<T> : TemplateBindingEntry
{
    /// <summary>The store.</summary>
    private readonly Store<T> _store;

    /// <summary>The selector of the string slice.</summary>
    private readonly Func<T, string?> _selector;

    /// <summary>Creates the entry.</summary>
    /// <param name="store">The store.</param>
    /// <param name="selector">The selector of the slice.</param>
    internal StringTextEntry(Store<T> store, Func<T, string?> selector)
    {
        _store = store;
        _selector = selector;
    }

    /// <summary>Gets the kind of the selector: <c>a string</c>.</summary>
    public override string SelectorKind => "a string";

    /// <summary>Binds the text of the label to the string slice.</summary>
    /// <param name="label">The label that the loader made.</param>
    /// <returns>Always <see langword="true"/>: the entry holds a selector that fits a label.</returns>
    internal override bool TryBind(Label label)
    {
        label.BindText(_store, _selector);
        return true;
    }

    /// <summary>Binds the text of the text field to the string slice.</summary>
    /// <param name="field">The text field that the loader made.</param>
    /// <returns>Always <see langword="true"/>: the entry holds a selector that fits a text field.</returns>
    internal override bool TryBind(TextField field)
    {
        field.BindText(_store, _selector);
        return true;
    }
}

/// <summary>
/// A bool key of a <see cref="TemplateBindings"/> table. The entry binds the checked state of a
/// <see cref="Toggle"/> through <see cref="WidgetBindings.BindChecked{T}(Toggle, Store{T}, Func{T, bool})"/>.
/// </summary>
/// <remarks>
/// <para>
/// The entry serves a toggle only. The binding writes <see cref="Toggle.Checked"/> through the code setter, so a
/// store change calls no <see cref="Toggle.CheckedChanged"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value of the store.</typeparam>
internal sealed class CheckedEntry<T> : TemplateBindingEntry
{
    /// <summary>The store.</summary>
    private readonly Store<T> _store;

    /// <summary>The selector of the bool slice.</summary>
    private readonly Func<T, bool> _selector;

    /// <summary>Creates the entry.</summary>
    /// <param name="store">The store.</param>
    /// <param name="selector">The selector of the slice.</param>
    internal CheckedEntry(Store<T> store, Func<T, bool> selector)
    {
        _store = store;
        _selector = selector;
    }

    /// <summary>Gets the kind of the selector: <c>a bool</c>.</summary>
    public override string SelectorKind => "a bool";

    /// <summary>Binds the checked state of the toggle to the bool slice.</summary>
    /// <param name="toggle">The toggle that the loader made.</param>
    /// <returns>Always <see langword="true"/>: the entry holds a selector that fits a toggle.</returns>
    internal override bool TryBind(Toggle toggle)
    {
        toggle.BindChecked(_store, _selector);
        return true;
    }
}

/// <summary>
/// A float key of a <see cref="TemplateBindings"/> table. The entry binds the value of a <see cref="Slider"/>
/// through <see cref="WidgetBindings.BindValue{T}(Slider, Store{T}, Func{T, float})"/>.
/// </summary>
/// <remarks>
/// <para>
/// The entry serves a slider only. The binding writes <see cref="Slider.Value"/> through the code setter, so a
/// store change calls no <see cref="Slider.ValueChanged"/>. The slider applies its range rule, so it can show a
/// value that differs from the store.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value of the store.</typeparam>
internal sealed class ValueEntry<T> : TemplateBindingEntry
{
    /// <summary>The store.</summary>
    private readonly Store<T> _store;

    /// <summary>The selector of the float slice.</summary>
    private readonly Func<T, float> _selector;

    /// <summary>Creates the entry.</summary>
    /// <param name="store">The store.</param>
    /// <param name="selector">The selector of the slice.</param>
    internal ValueEntry(Store<T> store, Func<T, float> selector)
    {
        _store = store;
        _selector = selector;
    }

    /// <summary>Gets the kind of the selector: <c>a float</c>.</summary>
    public override string SelectorKind => "a float";

    /// <summary>Binds the value of the slider to the float slice.</summary>
    /// <param name="slider">The slider that the loader made.</param>
    /// <returns>Always <see langword="true"/>: the entry holds a selector that fits a slider.</returns>
    internal override bool TryBind(Slider slider)
    {
        slider.BindValue(_store, _selector);
        return true;
    }
}
