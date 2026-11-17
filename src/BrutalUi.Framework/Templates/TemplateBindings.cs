using System;
using System.Collections.Generic;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// The binding attributes of a template: <c>command</c>, <c>bind-text</c>, <c>bind-checked</c>, and <c>bind-value</c>.
/// One object holds the <see cref="CommandRegistry"/> of the game and a table of typed selectors by key.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The capture.</strong> <see cref="AttachTo"/> adds the instance method <c>Handle</c> to the attribute hook
/// of a <see cref="TemplateLoader"/> as one <see cref="AttributeHandler"/>. The delegate captures this object, so the
/// handler needs no state argument, and the hook contract of the loader does not change.
/// </para>
/// <para>
/// <strong>Load time.</strong> The handler binds each element during the load, and the load allocates: the UTF-8 copy
/// of a command name and one binding object per bound element. A loaded tree allocates nothing in
/// <c>UiContext.Update</c> and <c>Draw</c>, exactly as an element bound through the C# bind methods does.
/// </para>
/// <para>
/// <strong>One bindings set per loader.</strong> The first attached handler takes each of the four names, so a second
/// <see cref="TemplateBindings"/> attached to the same loader never sees them. A game makes one loader for each
/// bindings set.
/// </para>
/// <para>
/// <strong>The two setup faults.</strong> A loader that no <see cref="AttachTo"/> call took gives the loader's own
/// <see cref="TemplateErrorKind.UnknownAttribute"/> for each of the four names, with the attribute list of the
/// element. That message never names bindings, and the fix is one <see cref="AttachTo"/> call on that loader, before
/// the load. A second <see cref="TemplateBindings"/> object on one loader gives
/// <see cref="TemplateErrorKind.UnknownBinding"/> for a key that this object does register, because the first
/// attached object takes the four names. The fix is one loader for each bindings set.
/// </para>
/// <para>
/// <strong>The object keeps each loader.</strong> <see cref="AttachTo"/> holds a reference to every loader that it
/// takes, and no detach method exists. One <see cref="TemplateBindings"/> object therefore keeps each of its loaders
/// alive for the life of the object. A game makes one <see cref="TemplateBindings"/> object for one set of loaders
/// and releases the two together.
/// </para>
/// <para>
/// <strong>An add after <see cref="AttachTo"/> is valid.</strong> The handler reads the key table at each load, so a
/// key that the panel code adds after the attach serves the next <see cref="TemplateLoader.Load(ReadOnlySpan{byte}, string)"/>
/// call. An <c>Add*</c> call and a load call must not run at the same time, as for
/// <see cref="TemplateLoader.Register"/>.
/// </para>
/// <para>
/// <strong>One key holds one selector.</strong> A second <c>Add*</c> call for one key throws
/// <see cref="ArgumentException"/>, also for another <c>Add*</c> method, and the table keeps the first selector. An
/// empty key throws <see cref="ArgumentException"/>. A second <see cref="AttachTo"/> of one object on one loader
/// throws <see cref="ArgumentException"/> and adds no handler.
/// </para>
/// </remarks>
public sealed partial class TemplateBindings
{
    /// <summary>The registry that <c>command</c> binds.</summary>
    private readonly CommandRegistry _commands;

    /// <summary>The key table: one selector entry per key, compared ordinal.</summary>
    private readonly Dictionary<string, TemplateBindingEntry> _entries = new(StringComparer.Ordinal);

    /// <summary>The loaders that <see cref="AttachTo"/> took, for the check of a second attach.</summary>
    /// <remarks>
    /// The list only grows, and no detach method exists. The object therefore keeps a reference to each loader that it
    /// attaches to, for the life of the object.
    /// </remarks>
    private readonly List<TemplateLoader> _loaders = new();

    /// <summary>Creates the bindings for one registry and an empty key table.</summary>
    /// <param name="commands">The registry that a <c>command</c> attribute reads. The registry looks the command up at
    /// each click, so the game can register a name after the load.</param>
    /// <exception cref="ArgumentNullException"><paramref name="commands"/> is <see langword="null"/>.</exception>
    public TemplateBindings(CommandRegistry commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        _commands = commands;
    }

    /// <summary>
    /// Registers a string key for <c>bind-text</c> on a <see cref="Label"/> or a <see cref="TextField"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A template element with <c>bind-text="<paramref name="key"/>"</c> binds its text to the slice. One string key
    /// serves a label and a text field: each element gets one binding that reads the same slice.
    /// </para>
    /// <para>
    /// Pass a <see langword="static"/> lambda or a cached method group as <paramref name="selector"/>, because the
    /// binding calls it in each update.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <param name="key">The key that the <c>bind-text</c> attribute names. The compare is ordinal, and the value is
    /// used as is.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the string slice from the value of the store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty, or the table holds the key already.</exception>
    public void AddText<T>(string key, Store<T> store, Func<T, string?> selector)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        CheckKey(key);
        _entries.Add(key, new StringTextEntry<T>(store, selector));
    }

    /// <summary>
    /// Registers a number key for <c>bind-text</c> on a <see cref="Label"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A template label with <c>bind-text="<paramref name="key"/>"</c> binds its text to the slice. The format and the
    /// provider come from this call, not from the template. The entry serves a label only: a text field with the same
    /// key is a type mismatch.
    /// </para>
    /// <para>
    /// Pass a <see langword="static"/> lambda or a cached method group as <paramref name="selector"/>, because the
    /// binding calls it in each update.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <typeparam name="TValue">The type of the slice. It implements <see cref="IUtf8SpanFormattable"/>.</typeparam>
    /// <param name="key">The key that the <c>bind-text</c> attribute names. The compare is ordinal, and the value is
    /// used as is.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the slice from the value of the store.</param>
    /// <param name="format">The format string of the slice, for example <c>"N0"</c>. <see langword="null"/> gives the
    /// default format.</param>
    /// <param name="provider">The format provider. <see langword="null"/> gives the current culture.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty, or the table holds the key already.</exception>
    public void AddText<T, TValue>(string key, Store<T> store, Func<T, TValue> selector,
        string? format = null, IFormatProvider? provider = null)
        where TValue : IUtf8SpanFormattable
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        CheckKey(key);
        _entries.Add(key, new NumberTextEntry<T, TValue>(store, selector, format, provider));
    }

    /// <summary>
    /// Registers a bool key for <c>bind-checked</c> on a <see cref="Toggle"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A template toggle with <c>bind-checked="<paramref name="key"/>"</c> binds its checked state to the slice. The
    /// binding writes <see cref="Toggle.Checked"/>, so a store change calls no
    /// <see cref="Toggle.CheckedChanged"/>. A flip by the user still calls that delegate, and the game writes the
    /// store there.
    /// </para>
    /// <para>
    /// Pass a <see langword="static"/> lambda or a cached method group as <paramref name="selector"/>, because the
    /// binding calls it in each update.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <param name="key">The key that the <c>bind-checked</c> attribute names. The compare is ordinal, and the value
    /// is used as is.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the bool slice from the value of the store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty, or the table holds the key already.</exception>
    public void AddChecked<T>(string key, Store<T> store, Func<T, bool> selector)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        CheckKey(key);
        _entries.Add(key, new CheckedEntry<T>(store, selector));
    }

    /// <summary>
    /// Registers a float key for <c>bind-value</c> on a <see cref="Slider"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A template slider with <c>bind-value="<paramref name="key"/>"</c> binds its value to the slice. The binding
    /// writes <see cref="Slider.Value"/>, so a store change calls no <see cref="Slider.ValueChanged"/>. The slider
    /// applies its range rule, so it can show a value that differs from the store. A drag by the user still calls
    /// that delegate, and the game writes the store there.
    /// </para>
    /// <para>
    /// Pass a <see langword="static"/> lambda or a cached method group as <paramref name="selector"/>, because the
    /// binding calls it in each update.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value of the store.</typeparam>
    /// <param name="key">The key that the <c>bind-value</c> attribute names. The compare is ordinal, and the value
    /// is used as is.</param>
    /// <param name="store">The store that holds the value.</param>
    /// <param name="selector">The function that reads the float slice from the value of the store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="store"/>, or
    /// <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty, or the table holds the key already.</exception>
    public void AddValue<T>(string key, Store<T> store, Func<T, float> selector)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        CheckKey(key);
        _entries.Add(key, new ValueEntry<T>(store, selector));
    }

    /// <summary>Adds the handler of the four binding attributes to one loader.</summary>
    /// <remarks>
    /// <para>
    /// The method adds one handler at the end of the hook list, so a game handler that the game added before this
    /// call still sees each attribute first. The handler reads the key table at each load, so an <c>Add*</c> call
    /// after this call serves the next load.
    /// </para>
    /// <para>
    /// <strong>A loader needs one call.</strong> A loader that no <see cref="AttachTo"/> call took knows none of the
    /// four names. It gives its own <see cref="TemplateErrorKind.UnknownAttribute"/> for each of them, and that
    /// message lists the attributes of the element and never names a binding. The fix is one <see cref="AttachTo"/>
    /// call on that loader, before the load.
    /// </para>
    /// <para>
    /// <strong>The object keeps the loader.</strong> This object holds a reference to each loader that it attaches
    /// to, and no detach method exists. Make one <see cref="TemplateBindings"/> object for one set of loaders, and
    /// release the two together.
    /// </para>
    /// </remarks>
    /// <param name="loader">The loader to attach to. A loader holds one bindings set: the first attached handler
    /// takes each of the four names.</param>
    /// <exception cref="ArgumentNullException"><paramref name="loader"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The object attached to <paramref name="loader"/> already.</exception>
    public void AttachTo(TemplateLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);

        if (_loaders.Contains(loader))
        {
            throw new ArgumentException(
                "One TemplateBindings object attaches to one loader one time. Make another loader for another bindings set.",
                nameof(loader));
        }

        _loaders.Add(loader);
        loader.AddAttributeHandler(Handle);
    }

    /// <summary>Checks one key before the table takes it.</summary>
    /// <remarks>
    /// One key holds one selector, so a second <c>Add*</c> call for one key throws, also for another <c>Add*</c>
    /// method. The check runs before the add, so the table keeps the first selector. The messages are constant.
    /// </remarks>
    /// <param name="key">The key that an <c>Add*</c> call names.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty, or the table holds it already.</exception>
    private void CheckKey(string key)
    {
        if (key.Length == 0)
        {
            throw new ArgumentException(
                "A binding key must not be empty. Write a key that the panel code registers, for example 'hud.score'.",
                nameof(key));
        }

        if (_entries.ContainsKey(key))
        {
            throw new ArgumentException(
                "One binding key holds one selector. Use another key, or register the key on another TemplateBindings object.",
                nameof(key));
        }
    }
}
