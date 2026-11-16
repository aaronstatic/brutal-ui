using System;
using Brutal.Localization;
using Brutal.Numerics;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The written table of the built-in element types: the base rows and the entry for each type.
/// </summary>
/// <remarks>
/// <para>
/// This file is the written table. The element-generator feature replaces it with a generated one, so each row here
/// holds the shape that the generator writes: a name, a kind, and a <see langword="static"/> setter or factory.
/// </para>
/// <para>
/// The loader copies the base rows before the own rows of each type, and runs them in that order. A game registers
/// its own type with the public <see cref="TemplateLoader.Register"/> call, and gets the base rows the same way.
/// The table holds the eight built-in types and every row of The Written Table, the <c>text-key</c> rows included.
/// </para>
/// </remarks>
internal static class BuiltInElements
{
    /// <summary>Gets the base rows. The loader places them before the own rows of each registered type.</summary>
    internal static readonly AttributeRow[] BaseRows =
    [
        new AttributeRow("name", static (Element element, AttributeContext context) => element.Name = context.Value),
        new AttributeRow("class", static (Element element, AttributeContext context) => AddClasses(element, context)),
        new AttributeRow("pickable", static (Element element, AttributeContext context) => SetPickable(element, context)),
        new AttributeRow("focusable", static (Element element, AttributeContext context) => SetFocusable(element, context)),
    ];

    /// <summary>Registers the built-in types in the loader.</summary>
    /// <remarks>
    /// The loader calls this from each constructor. A game adds its own types after the constructor.
    /// </remarks>
    /// <param name="loader">The loader to fill.</param>
    internal static void RegisterAll(TemplateLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);

        loader.Register(new ElementTypeInfo
        {
            Name = "Element",
            Create = static (ElementCreateArgs _) => new Element(),
        });

        loader.Register(new ElementTypeInfo
        {
            Name = "Label",
            Create = static (ElementCreateArgs args) => CreateLabel(args),
            Rows = [new AttributeRow("text", AttributeRowKind.Create), new AttributeRow("text-key", AttributeRowKind.Create)],
            IsLeaf = true,
        });

        loader.Register(new ElementTypeInfo
        {
            Name = "Button",
            Create = static (ElementCreateArgs args) => CreateButton(args),
            Rows = [new AttributeRow("text", AttributeRowKind.Create), new AttributeRow("text-key", AttributeRowKind.Create)],
        });

        loader.Register(new ElementTypeInfo
        {
            Name = "Toggle",
            Create = static (ElementCreateArgs _) => new Toggle(default),
            Rows = [new AttributeRow("checked", static (Element element, AttributeContext context) => SetChecked((Toggle)element, context))],
        });

        loader.Register(new ElementTypeInfo
        {
            Name = "Slider",
            Create = static (ElementCreateArgs args) => CreateSlider(args),
            Rows =
            [
                new AttributeRow("min", AttributeRowKind.Create),
                new AttributeRow("max", AttributeRowKind.CreateRequired),
                new AttributeRow("step", AttributeRowKind.Create),
                new AttributeRow("value", static (Element element, AttributeContext context) => SetValue((Slider)element, context)),
            ],
        });

        loader.Register(new ElementTypeInfo
        {
            Name = "TextField",
            Create = static (ElementCreateArgs args) => CreateTextField(args),
            Rows =
            [
                new AttributeRow("capacity", AttributeRowKind.CreateRequired),
                new AttributeRow("text", AttributeRowKind.Create),
                new AttributeRow("text-key", AttributeRowKind.Create),
            ],
        });

        loader.Register(new ElementTypeInfo
        {
            Name = "ScrollView",
            Create = static (ElementCreateArgs args) => CreateScrollView(args),
            Rows =
            [
                new AttributeRow("scroll-speed", AttributeRowKind.Create),
                new AttributeRow("scroll-offset", static (Element element, AttributeContext context) => SetScrollOffset((ScrollView)element, context)),
            ],
        });

        loader.Register(new ElementTypeInfo
        {
            Name = "Image",
            Create = static (ElementCreateArgs _) => new Image(new Image.CreateInfo { Texture = default }),
            Rows =
            [
                new AttributeRow("texture", static (Element element, AttributeContext context) => SetTexture((Image)element, context)),
                new AttributeRow("uv", static (Element element, AttributeContext context) => SetUv((Image)element, context)),
                new AttributeRow("tint", static (Element element, AttributeContext context) => SetTint((Image)element, context)),
                new AttributeRow("scale-mode", static (Element element, AttributeContext context) => SetScaleMode((Image)element, context)),
            ],
            IsLeaf = true,
        });
    }

    /// <summary>
    /// Makes a slider from <c>min</c>, <c>max</c>, and <c>step</c>. The method runs the range checks before the
    /// constructor, so no <see cref="ArgumentOutOfRangeException"/> leaves the load.
    /// </summary>
    /// <param name="args">The create args of the element.</param>
    /// <returns>The new slider, or <see langword="null"/> after an error.</returns>
    private static Element? CreateSlider(ElementCreateArgs args)
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

    /// <summary>Makes a text field from <c>capacity</c>, <c>text</c>, and <c>text-key</c>. With <c>text-key</c>, the
    /// method calls <see cref="TextField.SetText(LString?)"/>, as the builder does, and cuts the text at the capacity.</summary>
    /// <param name="args">The create args of the element.</param>
    /// <returns>The new field, or <see langword="null"/> after an error.</returns>
    private static Element? CreateTextField(ElementCreateArgs args)
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

    /// <summary>Makes a scroll view from <c>scroll-speed</c> after the speed check.</summary>
    /// <param name="args">The create args of the element.</param>
    /// <returns>The new view, or <see langword="null"/> after an error.</returns>
    private static Element? CreateScrollView(ElementCreateArgs args)
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

    /// <summary>Writes the <c>checked</c> value of a toggle.</summary>
    /// <param name="toggle">The element to change.</param>
    /// <param name="context">The <c>checked</c> attribute.</param>
    private static void SetChecked(Toggle toggle, AttributeContext context)
    {
        if (context.TryReadBool(out bool value))
        {
            toggle.Checked = value;
        }
    }

    /// <summary>Writes the <c>value</c> of a slider. The loader has made the range already.</summary>
    /// <param name="slider">The element to change.</param>
    /// <param name="context">The <c>value</c> attribute.</param>
    private static void SetValue(Slider slider, AttributeContext context)
    {
        if (context.TryReadNumber(out float value))
        {
            slider.Value = value;
        }
    }

    /// <summary>Writes the <c>scroll-offset</c> of a scroll view, as the builder method does.</summary>
    /// <remarks>
    /// The set clamps with the layout of the last update, so on a new tree the offset stays (0, 0).
    /// </remarks>
    /// <param name="scrollView">The element to change.</param>
    /// <param name="context">The <c>scroll-offset</c> attribute.</param>
    private static void SetScrollOffset(ScrollView scrollView, AttributeContext context)
    {
        if (context.TryReadNumbers(out float2 value))
        {
            scrollView.ScrollOffset = value;
        }
    }

    /// <summary>Writes the <c>texture</c> of an image.</summary>
    /// <param name="image">The element to change.</param>
    /// <param name="context">The <c>texture</c> attribute.</param>
    private static void SetTexture(Image image, AttributeContext context)
    {
        if (context.TryReadTexture(out TextureHandle value))
        {
            image.Texture = value;
        }
    }

    /// <summary>Writes the <c>uv</c> of an image.</summary>
    /// <param name="image">The element to change.</param>
    /// <param name="context">The <c>uv</c> attribute.</param>
    private static void SetUv(Image image, AttributeContext context)
    {
        if (context.TryReadNumbers(out float4 value))
        {
            image.Uv = value;
        }
    }

    /// <summary>Writes the <c>tint</c> of an image.</summary>
    /// <param name="image">The element to change.</param>
    /// <param name="context">The <c>tint</c> attribute.</param>
    private static void SetTint(Image image, AttributeContext context)
    {
        if (context.TryReadColor(out Rgba8 value))
        {
            image.Tint = value;
        }
    }

    /// <summary>Writes the <c>scale-mode</c> of an image from one keyword, through an explicit switch.</summary>
    /// <param name="image">The element to change.</param>
    /// <param name="context">The <c>scale-mode</c> attribute.</param>
    private static void SetScaleMode(Image image, AttributeContext context)
    {
        string mode = TemplateValues.Trim(context.Value).ToString();
        switch (mode)
        {
            case "stretch-to-fill":
                image.ScaleMode = ImageScaleMode.StretchToFill;
                break;
            case "scale-to-fit":
                image.ScaleMode = ImageScaleMode.ScaleToFit;
                break;
            case "scale-and-crop":
                image.ScaleMode = ImageScaleMode.ScaleAndCrop;
                break;
            default:
                context.AddError(
                    TemplateErrorKind.InvalidValue,
                    $"'{mode}' is not a scale mode. Write 'stretch-to-fill', 'scale-to-fit', or 'scale-and-crop'.");
                break;
        }
    }

    /// <summary>Makes a label from <c>text</c> and <c>text-key</c>, or <see langword="null"/> after an error.</summary>
    /// <param name="args">The create args of the element.</param>
    private static Element? CreateLabel(ElementCreateArgs args)
    {
        return TryReadText(args, out string? text, out LString? localized)
            ? new Label(new Label.CreateInfo { Text = text, LocalizedText = localized })
            : null;
    }

    /// <summary>Makes a button from <c>text</c> and <c>text-key</c>, or <see langword="null"/> after an error.</summary>
    /// <param name="args">The create args of the element.</param>
    private static Element? CreateButton(ElementCreateArgs args)
    {
        return TryReadText(args, out string? text, out LString? localized)
            ? new Button(new Button.CreateInfo { Text = text, LocalizedText = localized })
            : null;
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

    /// <summary>Writes the <c>pickable</c> value of an element.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>pickable</c> attribute.</param>
    private static void SetPickable(Element element, AttributeContext context)
    {
        if (context.TryReadBool(out bool value))
        {
            element.IsPickable = value;
        }
    }

    /// <summary>Writes the <c>focusable</c> value of an element.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>focusable</c> attribute.</param>
    private static void SetFocusable(Element element, AttributeContext context)
    {
        if (context.TryReadBool(out bool value))
        {
            element.IsFocusable = value;
        }
    }

    /// <summary>Adds each class name of a class list, in the order of the value.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>class</c> attribute.</param>
    /// <remarks>
    /// The value holds names with XML whitespace between them. An empty value adds no class. A repeated name adds
    /// nothing, as <see cref="ClassList.Add"/> does.
    /// </remarks>
    private static void AddClasses(Element element, AttributeContext context)
    {
        ReadOnlySpan<char> rest = context.Value;
        while (TemplateValues.NextName(ref rest, out ReadOnlySpan<char> name))
        {
            element.Classes.Add(ClassName.Intern(name.ToString()));
        }
    }
}
