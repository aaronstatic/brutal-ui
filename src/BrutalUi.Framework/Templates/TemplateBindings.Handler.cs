using System.Text;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

// The handler half of the partial class: Handle and the messages of the four binding attributes.
// TemplateBindings.cs holds the state, the public members, and the rules of the object.
public sealed partial class TemplateBindings
{
    /// <summary>Handles one attribute that no row of the loader table holds.</summary>
    /// <remarks>
    /// <para>
    /// <strong>The order of checks.</strong> Each of the four names takes the attribute and adds at most one error, in
    /// this order: the element type, then an empty value, then the key, then the fit of the selector, then the bind.
    /// The element type comes first, because an attribute on the wrong element is wrong for each value. The empty
    /// check comes before the lookup, so an empty key gives <see cref="TemplateErrorKind.InvalidValue"/> and not
    /// <see cref="TemplateErrorKind.UnknownBinding"/>. Another attribute name returns <see langword="false"/>, so the
    /// next handler of the loader still sees it.
    /// </para>
    /// <para>
    /// The compare of the name is ordinal. The value is used as is, with no trim. The <c>command</c> name has no key
    /// check: the registry looks the command up at each click.
    /// </para>
    /// </remarks>
    /// <param name="element">The element that the loader made.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="context">The position of the attribute. Each error sits at the name of the attribute.</param>
    /// <returns><see langword="true"/> for each of the four names; <see langword="false"/> for another name.</returns>
    private bool Handle(Element element, string name, string value, AttributeContext context)
    {
        switch (name)
        {
            case "command":
                if (element is not Button button)
                {
                    context.AddError(TemplateErrorKind.UnknownAttribute,
                        "'command' is not an attribute of this element. Write command on a Button only.");
                    return true;
                }

                if (value.Length == 0)
                {
                    context.AddError(TemplateErrorKind.InvalidValue,
                        "'command' needs a command name. Write the name of a registered command, for example 'game.pause'.");
                    return true;
                }

                button.BindCommand(_commands, Encoding.UTF8.GetBytes(value));
                return true;

            case "bind-text":
                if (element is not (Label or TextField))
                {
                    context.AddError(TemplateErrorKind.UnknownAttribute,
                        "'bind-text' is not an attribute of this element. Write bind-text on a Label or a TextField only.");
                    return true;
                }

                if (value.Length == 0)
                {
                    context.AddError(TemplateErrorKind.InvalidValue,
                        "'bind-text' needs a key. Write a key that the panel code registers, for example 'hud.score'.");
                    return true;
                }

                if (!_entries.TryGetValue(value, out TemplateBindingEntry? textEntry))
                {
                    context.AddError(TemplateErrorKind.UnknownBinding,
                        $"'{value}' is not a bound key. Register it with TemplateBindings.AddText on the "
                        + "TemplateBindings object that this loader attached first, before the load. Another "
                        + "TemplateBindings object on this loader can hold the key: only the first attached object "
                        + "receives the four names, so make one loader for each bindings set.");
                    return true;
                }

                if (element is Label label)
                {
                    if (!textEntry.TryBind(label))
                    {
                        context.AddError(TemplateErrorKind.BindingTypeMismatch,
                            $"'{value}' holds {textEntry.SelectorKind} selector. "
                            + "bind-text on a Label needs a string or a number selector. Register the key with AddText.");
                    }

                    return true;
                }

                if (!textEntry.TryBind((TextField)element))
                {
                    context.AddError(TemplateErrorKind.BindingTypeMismatch,
                        $"'{value}' holds {textEntry.SelectorKind} selector. "
                        + "bind-text on a TextField needs a string selector. Register the key with AddText.");
                }

                return true;

            case "bind-checked":
                if (element is not Toggle toggle)
                {
                    context.AddError(TemplateErrorKind.UnknownAttribute,
                        "'bind-checked' is not an attribute of this element. Write bind-checked on a Toggle only.");
                    return true;
                }

                if (value.Length == 0)
                {
                    context.AddError(TemplateErrorKind.InvalidValue,
                        "'bind-checked' needs a key. Write a key that the panel code registers, for example 'hud.muted'.");
                    return true;
                }

                if (!_entries.TryGetValue(value, out TemplateBindingEntry? checkedEntry))
                {
                    context.AddError(TemplateErrorKind.UnknownBinding,
                        $"'{value}' is not a bound key. Register it with TemplateBindings.AddChecked on the "
                        + "TemplateBindings object that this loader attached first, before the load. Another "
                        + "TemplateBindings object on this loader can hold the key: only the first attached object "
                        + "receives the four names, so make one loader for each bindings set.");
                    return true;
                }

                if (!checkedEntry.TryBind(toggle))
                {
                    context.AddError(TemplateErrorKind.BindingTypeMismatch,
                        $"'{value}' holds {checkedEntry.SelectorKind} selector. "
                        + "bind-checked on a Toggle needs a bool selector. Register the key with AddChecked.");
                }

                return true;

            case "bind-value":
                if (element is not Slider slider)
                {
                    context.AddError(TemplateErrorKind.UnknownAttribute,
                        "'bind-value' is not an attribute of this element. Write bind-value on a Slider only.");
                    return true;
                }

                if (value.Length == 0)
                {
                    context.AddError(TemplateErrorKind.InvalidValue,
                        "'bind-value' needs a key. Write a key that the panel code registers, for example 'hud.volume'.");
                    return true;
                }

                if (!_entries.TryGetValue(value, out TemplateBindingEntry? valueEntry))
                {
                    context.AddError(TemplateErrorKind.UnknownBinding,
                        $"'{value}' is not a bound key. Register it with TemplateBindings.AddValue on the "
                        + "TemplateBindings object that this loader attached first, before the load. Another "
                        + "TemplateBindings object on this loader can hold the key: only the first attached object "
                        + "receives the four names, so make one loader for each bindings set.");
                    return true;
                }

                if (!valueEntry.TryBind(slider))
                {
                    context.AddError(TemplateErrorKind.BindingTypeMismatch,
                        $"'{value}' holds {valueEntry.SelectorKind} selector. "
                        + "bind-value on a Slider needs a float selector. Register the key with AddValue.");
                }

                return true;

            default:
                return false;
        }
    }
}
