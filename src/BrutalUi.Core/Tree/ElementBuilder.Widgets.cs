using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The widget group of the fluent builder: the value methods and the delegate methods of the widget types.
/// </summary>
/// <remarks>
/// <para>
/// Each value method checks the receiver, writes one property or calls one setter method, and returns the receiver.
/// A value method adds no rule of its own; the property holds the rules. A create-time value gets no method:
/// <see cref="TextField.Capacity"/> and <see cref="ScrollView.ScrollSpeed"/> stay create-info only.
/// </para>
/// <para>
/// Each delegate method toggles the <c>On</c> prefix of its slot: the slot <c>Clicked</c> gives the method
/// <c>OnClicked</c>. A delegate property is invocable, so C# binds <c>button.Clicked(h)</c> to a call of the delegate
/// in the property, and never to a method named after the slot. Each delegate method adds the handler to the slot with
/// <c>+=</c>: a handler that the game set before stays, and two calls add two handlers in the order of the call.
/// Register handlers at build time, and create each delegate one time.
/// </para>
/// </remarks>
public static partial class ElementBuilder
{
    /// <summary>Sets whether the toggle is checked and returns the toggle.</summary>
    /// <remarks>
    /// The property writes the <see cref="PseudoStates.Checked"/> bit of <see cref="Element.States"/>.
    /// A set from code calls no <see cref="Toggle.CheckedChanged"/>.
    /// </remarks>
    /// <param name="toggle">The toggle to change.</param>
    /// <param name="value">The checked state.</param>
    /// <returns>The same toggle.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toggle"/> is <see langword="null"/>.</exception>
    public static Toggle Checked(this Toggle toggle, bool value)
    {
        ArgumentNullException.ThrowIfNull(toggle);
        toggle.Checked = value;
        return toggle;
    }

    /// <summary>Sets the value of the slider and returns the slider.</summary>
    /// <remarks>
    /// The value rule of the slider applies: a NaN gives <see cref="Slider.Minimum"/>, then the clamp to the range,
    /// then the grid when <see cref="Slider.Step"/> is above 0. A set from code calls no
    /// <see cref="Slider.ValueChanged"/>. <see cref="Value(Slider, float)"/> clamps to the current range, so a chain
    /// sets the range first with <see cref="Range(Slider, float, float, float)"/>.
    /// </remarks>
    /// <param name="slider">The slider to change.</param>
    /// <param name="value">The new value.</param>
    /// <returns>The same slider.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slider"/> is <see langword="null"/>.</exception>
    public static Slider Value(this Slider slider, float value)
    {
        ArgumentNullException.ThrowIfNull(slider);
        slider.Value = value;
        return slider;
    }

    /// <summary>Changes the range and the step of the slider as one, and returns the slider.</summary>
    /// <remarks>
    /// The method calls <see cref="Slider.SetRange(float, float, float)"/>. A bad range throws
    /// <see cref="ArgumentOutOfRangeException"/> and changes nothing. The method applies the value rule to the old
    /// value and calls no <see cref="Slider.ValueChanged"/>. A chain sets the range before the value with
    /// <see cref="Value(Slider, float)"/>, because <see cref="Value(Slider, float)"/> clamps to the current range.
    /// </remarks>
    /// <param name="slider">The slider to change.</param>
    /// <param name="minimum">The lower end of the range. It must be finite.</param>
    /// <param name="maximum">The upper end of the range. It must be finite and not below the minimum.</param>
    /// <param name="step">The step of the grid. It must be finite and 0 or more. A step of 0 means no grid.</param>
    /// <returns>The same slider.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slider"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A bound is not finite, the minimum is above the maximum, the width of the range is not finite, or the step is
    /// negative or not finite. <see cref="Slider.SetRange(float, float, float)"/> names the bad parameter.
    /// </exception>
    public static Slider Range(this Slider slider, float minimum, float maximum, float step)
    {
        ArgumentNullException.ThrowIfNull(slider);
        slider.SetRange(minimum, maximum, step);
        return slider;
    }

    /// <summary>Sets the texture of the image and returns the image.</summary>
    /// <param name="image">The image to change.</param>
    /// <param name="texture">The texture. The none handle is valid: the image then takes no space and draws nothing.</param>
    /// <returns>The same image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is <see langword="null"/>.</exception>
    public static Image Texture(this Image image, TextureHandle texture)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.Texture = texture;
        return image;
    }

    /// <summary>Sets the texture coordinates of the image and returns the image.</summary>
    /// <param name="image">The image to change.</param>
    /// <param name="uv">The texture coordinates: u0, v0, u1, and v1. The default is (0, 0, 1, 1).</param>
    /// <returns>The same image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is <see langword="null"/>.</exception>
    public static Image Uv(this Image image, float4 uv)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.Uv = uv;
        return image;
    }

    /// <summary>Sets the tint of the image and returns the image.</summary>
    /// <param name="image">The image to change.</param>
    /// <param name="tint">The tint. The renderer multiplies each texel by the tint.</param>
    /// <returns>The same image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is <see langword="null"/>.</exception>
    public static Image Tint(this Image image, Rgba8 tint)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.Tint = tint;
        return image;
    }

    /// <summary>Sets the scale mode of the image and returns the image.</summary>
    /// <param name="image">The image to change.</param>
    /// <param name="mode">The rule that puts the texture into the box.</param>
    /// <returns>The same image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is <see langword="null"/>.</exception>
    public static Image ScaleMode(this Image image, ImageScaleMode mode)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.ScaleMode = mode;
        return image;
    }

    /// <summary>Sets the scroll offset of the view and returns the view.</summary>
    /// <remarks>
    /// The property clamps each axis from 0 to the maximum offset of the last layout, and a NaN gives 0 on its axis.
    /// A set from code calls no <see cref="ScrollView.ScrollChanged"/>.
    /// </remarks>
    /// <param name="view">The view to change.</param>
    /// <param name="offset">The scroll offset in pixels: how far the content moves left and up.</param>
    /// <returns>The same view.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="view"/> is <see langword="null"/>.</exception>
    public static ScrollView ScrollOffset(this ScrollView view, float2 offset)
    {
        ArgumentNullException.ThrowIfNull(view);
        view.ScrollOffset = offset;
        return view;
    }

    /// <summary>Adds a click handler to the button and returns the button.</summary>
    /// <remarks>
    /// The method adds the handler to <see cref="Button.Clicked"/> with <c>+=</c>, so a handler that the game set
    /// before stays.
    /// </remarks>
    /// <param name="button">The button to change.</param>
    /// <param name="handler">The handler. The button calls it when a pointer press completes over the button.</param>
    /// <returns>The same button.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="button"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static Button OnClicked(this Button button, Action<Button> handler)
    {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(handler);
        button.Clicked += handler;
        return button;
    }

    /// <summary>Adds a checked-changed handler to the toggle and returns the toggle.</summary>
    /// <remarks>
    /// The method adds the handler to <see cref="Toggle.CheckedChanged"/> with <c>+=</c>, so a handler that the game
    /// set before stays.
    /// </remarks>
    /// <param name="toggle">The toggle to change.</param>
    /// <param name="handler">The handler. The toggle calls it when a pointer press changes the checked state.</param>
    /// <returns>The same toggle.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toggle"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static Toggle OnCheckedChanged(this Toggle toggle, Action<Toggle> handler)
    {
        ArgumentNullException.ThrowIfNull(toggle);
        ArgumentNullException.ThrowIfNull(handler);
        toggle.CheckedChanged += handler;
        return toggle;
    }

    /// <summary>Adds a value-changed handler to the slider and returns the slider.</summary>
    /// <remarks>
    /// The method adds the handler to <see cref="Slider.ValueChanged"/> with <c>+=</c>, so a handler that the game
    /// set before stays.
    /// </remarks>
    /// <param name="slider">The slider to change.</param>
    /// <param name="handler">The handler. The slider calls it when a pointer drag changes the value.</param>
    /// <returns>The same slider.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slider"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static Slider OnValueChanged(this Slider slider, Action<Slider> handler)
    {
        ArgumentNullException.ThrowIfNull(slider);
        ArgumentNullException.ThrowIfNull(handler);
        slider.ValueChanged += handler;
        return slider;
    }

    /// <summary>Adds a text-changed handler to the text field and returns the field.</summary>
    /// <remarks>
    /// The method adds the handler to <see cref="TextField.TextChanged"/> with <c>+=</c>, so a handler that the game
    /// set before stays.
    /// </remarks>
    /// <param name="field">The text field to change.</param>
    /// <param name="handler">The handler. The field calls it when typing changes the text.</param>
    /// <returns>The same field.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static TextField OnTextChanged(this TextField field, Action<TextField> handler)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(handler);
        field.TextChanged += handler;
        return field;
    }

    /// <summary>Adds a submit handler to the text field and returns the field.</summary>
    /// <remarks>
    /// The method adds the handler to <see cref="TextField.Submitted"/> with <c>+=</c>, so a handler that the game
    /// set before stays.
    /// </remarks>
    /// <param name="field">The text field to change.</param>
    /// <param name="handler">The handler. The field calls it when the user presses Enter in the field.</param>
    /// <returns>The same field.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static TextField OnSubmitted(this TextField field, Action<TextField> handler)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(handler);
        field.Submitted += handler;
        return field;
    }

    /// <summary>Adds a scroll-changed handler to the view and returns the view.</summary>
    /// <remarks>
    /// The method adds the handler to <see cref="ScrollView.ScrollChanged"/> with <c>+=</c>, so a handler that the
    /// game set before stays.
    /// </remarks>
    /// <param name="view">The view to change.</param>
    /// <param name="handler">The handler. The view calls it when a wheel or drag changes the scroll offset.</param>
    /// <returns>The same view.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="view"/> or <paramref name="handler"/> is <see langword="null"/>.</exception>
    public static ScrollView OnScrollChanged(this ScrollView view, Action<ScrollView> handler)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(handler);
        view.ScrollChanged += handler;
        return view;
    }
}
