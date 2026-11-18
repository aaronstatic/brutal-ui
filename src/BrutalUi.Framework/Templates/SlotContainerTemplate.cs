using System.Text;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// The table entry of the <c>SlotContainer</c> element type. <see cref="DefinitionLoader"/> registers it on the
/// <see cref="TemplateLoader"/> of its constructor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The leaf rule.</strong> The type is a leaf. A template child of a <c>SlotContainer</c> gives
/// <see cref="TemplateErrorKind.UnexpectedChild"/>, because the children of a slot are the panel roots that the mode
/// switch mounts. They never come from the layout template.
/// </para>
/// <para>
/// <strong>The base row rule.</strong> The base row <c>name</c> changes <see cref="Element.Name"/>, not
/// <see cref="SlotContainer.SlotName"/>. The <see cref="SlotContainer"/> constructor sets both to the
/// <c>slot</c> value, so a template that also writes <c>name</c> gives a slot whose <see cref="Element.Name"/>
/// differs from its <see cref="SlotContainer.SlotName"/>.
/// </para>
/// </remarks>
internal static class SlotContainerTemplate
{
    /// <summary>Creates the type info for <c>SlotContainer</c>.</summary>
    /// <returns>The entry for <see cref="TemplateLoader.Register(in ElementTypeInfo)"/>.</returns>
    internal static ElementTypeInfo Create()
    {
        return new ElementTypeInfo
        {
            Name = "SlotContainer",
            Create = CreateSlotContainer,
            Rows = [new AttributeRow("slot", AttributeRowKind.CreateRequired)],
            IsLeaf = true,
        };
    }

    /// <summary>Makes a slot container from the <c>slot</c> attribute, or <see langword="null"/> after an error.</summary>
    /// <param name="args">The create args of the element. The factory reads <c>slot</c> through
    /// <see cref="ElementCreateArgs.TryGet"/>.</param>
    /// <returns>The new slot container, or <see langword="null"/> after an error.</returns>
    /// <remarks>
    /// The factory uses the value as is. It does not trim spaces. A value of <c>" top"</c> names a slot with a
    /// leading space.
    /// </remarks>
    private static Element? CreateSlotContainer(ElementCreateArgs args)
    {
        if (args.TryGet("slot", out AttributeContext context))
        {
            if (context.Value.Length == 0)
            {
                context.AddError(
                    TemplateErrorKind.InvalidValue,
                    "'slot' needs a value. Write the name of the slot, for example 'top'.");
                return null;
            }

            return new SlotContainer(Encoding.UTF8.GetBytes(context.Value));
        }

        // The loader records MissingAttribute before it calls the factory, so this path does not run in a load.
        // The guard keeps the contract of a null result: one new error.
        args.AddError(
            TemplateErrorKind.MissingAttribute,
            "'slot' is required on SlotContainer. Write the name of the slot, for example 'top'.");
        return null;
    }
}
