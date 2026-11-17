using System;
using Brutal.Localization;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The declarations and the factories of the eight built-in element types.
/// </summary>
/// <remarks>
/// <para>
/// Each nested class marks one Core type with <c>TemplateElement</c> and a <c>Target</c>, and holds the factory with
/// its create rows. The generator writes the set rows from the public settable properties of the target, and writes
/// the register call for the eight types. See the element-generator plan, "The Rules" and "The Generated Source".
/// </para>
/// <para>
/// The loader runs the factory before the set rows, so the order of the attributes in the source does not change the
/// tree. Each factory checks its own range before it calls the constructor, so no exception leaves the load.
/// </para>
/// <para>
/// The base rows live in <see cref="BaseRows"/>. A game registers its own type with the public
/// <see cref="TemplateLoader.Register"/> call, and gets the base rows the same way.
/// </para>
/// </remarks>
internal static class BuiltInElements
{
    /// <summary>The declaration of <see cref="Element"/>: no own row, and a plain constructor.</summary>
    [TemplateElement("Element", Target = typeof(Element))]
    internal static class ElementDeclaration
    {
        /// <summary>Makes an empty element.</summary>
        /// <param name="args">The create args of the element. The factory reads no attribute.</param>
        /// <returns>The new element.</returns>
        [TemplateFactory]
        internal static Element? Create(ElementCreateArgs args)
        {
            return new Element();
        }
    }

    /// <summary>The declaration of <see cref="Label"/>: a leaf with the two text create rows.</summary>
    [TemplateElement("Label", Target = typeof(Label), IsLeaf = true)]
    internal static class LabelDeclaration
    {
        /// <summary>Makes a label from <c>text</c> and <c>text-key</c>, or <see langword="null"/> after an error.</summary>
        /// <param name="args">The create args of the element.</param>
        /// <returns>The new label, or <see langword="null"/> after an error.</returns>
        [TemplateFactory]
        [TemplateCreateRow("text")]
        [TemplateCreateRow("text-key")]
        internal static Element? Create(ElementCreateArgs args)
        {
            return TryReadText(args, out string? text, out LString? localized)
                ? new Label(new Label.CreateInfo { Text = text, LocalizedText = localized })
                : null;
        }
    }

    /// <summary>The declaration of <see cref="Button"/>: the two text create rows, and no set row.</summary>
    [TemplateElement("Button", Target = typeof(Button))]
    internal static class ButtonDeclaration
    {
        /// <summary>Makes a button from <c>text</c> and <c>text-key</c>, or <see langword="null"/> after an error.</summary>
        /// <param name="args">The create args of the element.</param>
        /// <returns>The new button, or <see langword="null"/> after an error.</returns>
        [TemplateFactory]
        [TemplateCreateRow("text")]
        [TemplateCreateRow("text-key")]
        internal static Element? Create(ElementCreateArgs args)
        {
            return TryReadText(args, out string? text, out LString? localized)
                ? new Button(new Button.CreateInfo { Text = text, LocalizedText = localized })
                : null;
        }
    }

    /// <summary>The declaration of <see cref="Toggle"/>: the set row <c>checked</c> from the property.</summary>
    [TemplateElement("Toggle", Target = typeof(Toggle))]
    internal static class ToggleDeclaration
    {
        /// <summary>Makes a toggle with the default create-info.</summary>
        /// <param name="args">The create args of the element. The factory reads no attribute.</param>
        /// <returns>The new toggle.</returns>
        /// <remarks>
        /// The loader writes <c>checked</c> after the factory, through the set row of <see cref="Toggle.Checked"/>.
        /// </remarks>
        [TemplateFactory]
        internal static Element? Create(ElementCreateArgs args)
        {
            return new Toggle(default);
        }
    }

    /// <summary>The declaration of <see cref="Slider"/>: three create rows and the set row <c>value</c>.</summary>
    [TemplateElement("Slider", Target = typeof(Slider))]
    internal static class SliderDeclaration
    {
        /// <summary>
        /// Makes a slider from <c>min</c>, <c>max</c>, and <c>step</c>. The method runs the range checks before the
        /// constructor, so no <see cref="ArgumentOutOfRangeException"/> leaves the load.
        /// </summary>
        /// <param name="args">The create args of the element.</param>
        /// <returns>The new slider, or <see langword="null"/> after an error.</returns>
        [TemplateFactory]
        [TemplateCreateRow("min")]
        [TemplateCreateRow("max", IsRequired = true)]
        [TemplateCreateRow("step")]
        internal static Element? Create(ElementCreateArgs args)
        {
            float min = 0f;
            if (args.TryGet("min", out AttributeContext minContext) && !minContext.TryReadNumber(out min))
            {
                return null;
            }

            if (!args.TryGet("max", out AttributeContext maxContext) || !maxContext.TryReadNumber(out float max))
            {
                return null;
            }

            float step = 0f;
            if (args.TryGet("step", out AttributeContext stepContext) && !stepContext.TryReadNumber(out step))
            {
                return null;
            }

            if (max < min)
            {
                maxContext.AddError(
                    TemplateErrorKind.InvalidValue,
                    $"'{maxContext.Value}' is below the min. Write a max that is not below the min.");
                return null;
            }

            if (!float.IsFinite(max - min))
            {
                maxContext.AddError(
                    TemplateErrorKind.InvalidValue,
                    $"'{maxContext.Value}' makes the width of the range infinite. Write a max nearer to the min.");
                return null;
            }

            if (step < 0f)
            {
                stepContext.AddError(
                    TemplateErrorKind.InvalidValue,
                    $"'{stepContext.Value}' is a negative step. Write a step of 0 or more.");
                return null;
            }

            return new Slider(new Slider.CreateInfo { Minimum = min, Maximum = max, Step = step });
        }
    }

    /// <summary>The declaration of <see cref="TextField"/>: one required create row and two text create rows.</summary>
    [TemplateElement("TextField", Target = typeof(TextField))]
    internal static class TextFieldDeclaration
    {
        /// <summary>Makes a text field from <c>capacity</c>, <c>text</c>, and <c>text-key</c>. With <c>text-key</c>, the
        /// method calls <see cref="TextField.SetText(LString?)"/>, as the builder does, and cuts the text at the capacity.</summary>
        /// <param name="args">The create args of the element.</param>
        /// <returns>The new field, or <see langword="null"/> after an error.</returns>
        /// <remarks>
        /// The method checks the capacity ceiling before the constructor allocates the buffer (review CR-1).
        /// </remarks>
        [TemplateFactory]
        [TemplateCreateRow("capacity", IsRequired = true)]
        [TemplateCreateRow("text")]
        [TemplateCreateRow("text-key")]
        internal static Element? Create(ElementCreateArgs args)
        {
            if (!args.TryGet("capacity", out AttributeContext capacityContext) || !capacityContext.TryReadWholeNumber(out int capacity)
                || !TemplateValues.CheckCapacity(capacityContext, capacity))
            {
                return null;
            }

            if (!TryReadText(args, out string? text, out LString? localized))
            {
                return null;
            }

            TextField field = new(new TextField.CreateInfo { Capacity = capacity, Text = text });
            if (localized is not null)
            {
                field.SetText(localized);
            }

            return field;
        }
    }

    /// <summary>The declaration of <see cref="ScrollView"/>: one create row and the set row <c>scroll-offset</c>.</summary>
    [TemplateElement("ScrollView", Target = typeof(ScrollView))]
    internal static class ScrollViewDeclaration
    {
        /// <summary>Makes a scroll view from <c>scroll-speed</c> after the speed check.</summary>
        /// <param name="args">The create args of the element.</param>
        /// <returns>The new view, or <see langword="null"/> after an error.</returns>
        [TemplateFactory]
        [TemplateCreateRow("scroll-speed")]
        internal static Element? Create(ElementCreateArgs args)
        {
            float speed = 0f;
            if (args.TryGet("scroll-speed", out AttributeContext speedContext))
            {
                if (!speedContext.TryReadNumber(out speed))
                {
                    return null;
                }

                if (speed < 0f)
                {
                    speedContext.AddError(
                        TemplateErrorKind.InvalidValue,
                        $"'{speedContext.Value}' is a negative speed. Write a scroll-speed of 0 or more.");
                    return null;
                }
            }

            return new ScrollView(new ScrollView.CreateInfo { ScrollSpeed = speed });
        }
    }

    /// <summary>The declaration of <see cref="Image"/>: a leaf whose four set rows come from its properties.</summary>
    [TemplateElement("Image", Target = typeof(Image), IsLeaf = true)]
    internal static class ImageDeclaration
    {
        /// <summary>Makes an image with the none handle.</summary>
        /// <param name="args">The create args of the element. The factory reads no attribute.</param>
        /// <returns>The new image.</returns>
        /// <remarks>
        /// The none handle is valid: the image takes no space and draws nothing. The loader writes <c>texture</c>,
        /// <c>uv</c>, <c>tint</c>, and <c>scale-mode</c> after the factory, through the set rows of the properties.
        /// </remarks>
        [TemplateFactory]
        internal static Element? Create(ElementCreateArgs args)
        {
            return new Image(new Image.CreateInfo { Texture = default });
        }
    }

    /// <summary>Reads <c>text</c> and <c>text-key</c> from the create args by the rule of A4.</summary>
    /// <remarks>
    /// <c>text-key</c> wins: the out values then hold the <see cref="LString"/> and a <see langword="null"/> text.
    /// A missing <c>text</c> gives the empty template. An empty <c>text-key</c> gives
    /// <see cref="TemplateErrorKind.InvalidValue"/> at the attribute, because <see cref="LString"/> rejects an empty id.
    /// </remarks>
    /// <returns><see langword="true"/> when the read gives no error.</returns>
    private static bool TryReadText(ElementCreateArgs args, out string? text, out LString? localizedText)
    {
        text = args.TryGet("text", out AttributeContext textContext) ? textContext.Value : null;
        localizedText = null;
        if (args.TryGet("text-key", out AttributeContext keyContext))
        {
            if (keyContext.Value.Length == 0)
            {
                keyContext.AddError(TemplateErrorKind.InvalidValue, "'' is an empty text key. Write a key with one or more characters.");
                return false;
            }

            localizedText = new LString(keyContext.Value, text ?? string.Empty);
            text = null;
        }

        return true;
    }
}
