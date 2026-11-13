using System;
using System.Collections.Generic;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the widget group of <see cref="ElementBuilder"/>: the eight value methods, the six delegate
/// methods, the value rules they pass through, the delegate silence, and the null checks.
/// </summary>
public sealed class ElementBuilderWidgetTests
{
    /// <summary>A texture for the image cases: id 3, size 64 by 32.</summary>
    private static readonly TextureHandle Texture = new(3, new int2(64, 32));

    /// <summary>A second texture for the <c>Texture</c> method.</summary>
    private static readonly TextureHandle SecondTexture = new(7, new int2(32, 16));

    /// <summary>Checked(true) sets the checked bit. A set from code calls no CheckedChanged.</summary>
    [Fact]
    public void CheckedSetsTheStateAndCallsNoDelegate()
    {
        Toggle toggle = new Toggle(default(Toggle.CreateInfo));
        int calls = 0;
        toggle.CheckedChanged = _ => calls++;

        Toggle same = toggle.Checked(true);

        Assert.True(toggle.Checked);
        Assert.NotEqual(PseudoStates.None, toggle.States & PseudoStates.Checked);
        Assert.Equal(0, calls);
        Assert.True(ReferenceEquals(toggle, same));
    }

    /// <summary>Value applies the value rule of the slider. A set from code calls no ValueChanged.</summary>
    [Fact]
    public void ValueAppliesTheValueRuleAndCallsNoDelegate()
    {
        Slider slider = new Slider(new Slider.CreateInfo { Minimum = 0f, Maximum = 10f, Step = 2f });
        int calls = 0;
        slider.ValueChanged = _ => calls++;

        slider.Value(20f);
        Assert.Equal(10f, slider.Value);

        slider.Value(float.NaN);
        Assert.Equal(0f, slider.Value);

        Slider same = slider.Value(3.4f);
        Assert.Equal(4f, slider.Value);
        Assert.True(ReferenceEquals(slider, same));

        Assert.Equal(0, calls);
    }

    /// <summary>Range calls SetRange. A bad range throws and leaves the old range.</summary>
    [Fact]
    public void RangeCallsSetRangeAndThrowsForABadRange()
    {
        Slider slider = new Slider(new Slider.CreateInfo { Maximum = 2f });

        Slider same = slider.Range(0f, 10f, 1f);

        Assert.Equal(0f, slider.Minimum);
        Assert.Equal(10f, slider.Maximum);
        Assert.Equal(1f, slider.Step);
        Assert.True(ReferenceEquals(slider, same));

        Assert.Throws<ArgumentOutOfRangeException>(() => slider.Range(5f, 1f, 0f));
        Assert.Equal(0f, slider.Minimum);
        Assert.Equal(10f, slider.Maximum);
        Assert.Equal(1f, slider.Step);
    }

    /// <summary>The four image methods set the texture, the uv rect, the tint, and the scale mode.</summary>
    [Fact]
    public void TheImageMethodsSetTextureUvTintAndScaleMode()
    {
        Image image = new Image(new Image.CreateInfo { Texture = Texture });

        Image chained = image
            .Texture(SecondTexture)
            .Uv(new float4(0.25f, 0.25f, 0.75f, 0.75f))
            .Tint(new Rgba8(255, 0, 0))
            .ScaleMode(ImageScaleMode.ScaleAndCrop);

        Assert.True(ReferenceEquals(image, chained));
        Assert.Equal(SecondTexture, image.Texture);
        Assert.Equal(new float4(0.25f, 0.25f, 0.75f, 0.75f), image.Uv);
        Assert.Equal(new Rgba8(255, 0, 0), image.Tint);
        Assert.Equal(ImageScaleMode.ScaleAndCrop, image.ScaleMode);
    }

    /// <summary>ScrollOffset writes the offset, and the property clamps with the layout of the last update.</summary>
    [Fact]
    public void ScrollOffsetSetsTheOffset()
    {
        ScrollViewTree tree = new ScrollViewTree();
        tree.Settle();

        ScrollView same = tree.View.ScrollOffset(new float2(0f, 40f));

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.True(ReferenceEquals(tree.View, same));

        tree.View.ScrollOffset(new float2(500f, 500f));
        Assert.Equal(new float2(100f, 80f), tree.View.ScrollOffset);
    }

    /// <summary>Each value method throws <see cref="ArgumentNullException"/> for a null receiver.</summary>
    [Fact]
    public void EachWidgetValueMethodThrowsForANullElement()
    {
        Toggle nullToggle = null!;
        Slider nullSlider = null!;
        Image nullImage = null!;
        ScrollView nullView = null!;

        Assert.Equal("toggle", Assert.Throws<ArgumentNullException>(() => nullToggle.Checked(true)).ParamName);
        Assert.Equal("slider", Assert.Throws<ArgumentNullException>(() => nullSlider.Value(1f)).ParamName);
        Assert.Equal("slider", Assert.Throws<ArgumentNullException>(() => nullSlider.Range(0f, 1f, 0f)).ParamName);
        Assert.Equal("image", Assert.Throws<ArgumentNullException>(() => nullImage.Texture(default)).ParamName);
        Assert.Equal("image", Assert.Throws<ArgumentNullException>(() => nullImage.Uv(default)).ParamName);
        Assert.Equal("image", Assert.Throws<ArgumentNullException>(() => nullImage.Tint(default)).ParamName);
        Assert.Equal("image", Assert.Throws<ArgumentNullException>(() => nullImage.ScaleMode(default)).ParamName);
        Assert.Equal("view", Assert.Throws<ArgumentNullException>(() => nullView.ScrollOffset(default)).ParamName);
    }

    /// <summary>
    /// Each delegate method adds with <c>+=</c>: a handler that the test set with <c>=</c> first stays, and two
    /// builder calls add two handlers, so the slot holds three calls in the order of add.
    /// </summary>
    [Fact]
    public void EachWidgetSlotMethodAddsTheHandlerWithPlusEquals()
    {
        List<string> order = new();

        Button button = new Button(default(Button.CreateInfo));
        button.Clicked = _ => order.Add("button game");
        Button sameButton = button.OnClicked(_ => order.Add("button one")).OnClicked(_ => order.Add("button two"));
        Assert.True(ReferenceEquals(button, sameButton));
        Assert.Equal(3, button.Clicked!.GetInvocationList().Length);
        button.Clicked!(button);

        Toggle toggle = new Toggle(default(Toggle.CreateInfo));
        toggle.CheckedChanged = _ => order.Add("toggle game");
        Toggle sameToggle = toggle.OnCheckedChanged(_ => order.Add("toggle one"))
            .OnCheckedChanged(_ => order.Add("toggle two"));
        Assert.True(ReferenceEquals(toggle, sameToggle));
        Assert.Equal(3, toggle.CheckedChanged!.GetInvocationList().Length);
        toggle.CheckedChanged!(toggle);

        Slider slider = new Slider(default(Slider.CreateInfo));
        slider.ValueChanged = _ => order.Add("slider game");
        Slider sameSlider = slider.OnValueChanged(_ => order.Add("slider one")).OnValueChanged(_ => order.Add("slider two"));
        Assert.True(ReferenceEquals(slider, sameSlider));
        Assert.Equal(3, slider.ValueChanged!.GetInvocationList().Length);
        slider.ValueChanged!(slider);

        TextField field = new TextField(new TextField.CreateInfo { Capacity = 16 });
        field.TextChanged = _ => order.Add("text game");
        TextField sameField = field.OnTextChanged(_ => order.Add("text one")).OnTextChanged(_ => order.Add("text two"));
        Assert.True(ReferenceEquals(field, sameField));
        Assert.Equal(3, field.TextChanged!.GetInvocationList().Length);
        field.TextChanged!(field);

        field.Submitted = _ => order.Add("submit game");
        TextField sameField2 = field.OnSubmitted(_ => order.Add("submit one")).OnSubmitted(_ => order.Add("submit two"));
        Assert.True(ReferenceEquals(field, sameField2));
        Assert.Equal(3, field.Submitted!.GetInvocationList().Length);
        field.Submitted!(field);

        ScrollView view = new ScrollView(default(ScrollView.CreateInfo));
        view.ScrollChanged = _ => order.Add("scroll game");
        ScrollView sameView = view.OnScrollChanged(_ => order.Add("scroll one")).OnScrollChanged(_ => order.Add("scroll two"));
        Assert.True(ReferenceEquals(view, sameView));
        Assert.Equal(3, view.ScrollChanged!.GetInvocationList().Length);
        view.ScrollChanged!(view);

        Assert.Equal(
            new[]
            {
                "button game", "button one", "button two",
                "toggle game", "toggle one", "toggle two",
                "slider game", "slider one", "slider two",
                "text game", "text one", "text two",
                "submit game", "submit one", "submit two",
                "scroll game", "scroll one", "scroll two",
            },
            order);
    }

    /// <summary>Each delegate method throws for a null receiver and for a null handler.</summary>
    [Fact]
    public void AWidgetSlotMethodThrowsForANullReceiverOrANullHandler()
    {
        Button nullButton = null!;
        Toggle nullToggle = null!;
        Slider nullSlider = null!;
        TextField nullField = null!;
        ScrollView nullView = null!;

        Assert.Equal("button", Assert.Throws<ArgumentNullException>(() => nullButton.OnClicked(_ => { })).ParamName);
        Assert.Equal("toggle", Assert.Throws<ArgumentNullException>(() => nullToggle.OnCheckedChanged(_ => { })).ParamName);
        Assert.Equal("slider", Assert.Throws<ArgumentNullException>(() => nullSlider.OnValueChanged(_ => { })).ParamName);
        Assert.Equal("field", Assert.Throws<ArgumentNullException>(() => nullField.OnTextChanged(_ => { })).ParamName);
        Assert.Equal("field", Assert.Throws<ArgumentNullException>(() => nullField.OnSubmitted(_ => { })).ParamName);
        Assert.Equal("view", Assert.Throws<ArgumentNullException>(() => nullView.OnScrollChanged(_ => { })).ParamName);

        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Button(default(Button.CreateInfo)).OnClicked(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Toggle(default(Toggle.CreateInfo)).OnCheckedChanged(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Slider(default(Slider.CreateInfo)).OnValueChanged(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new TextField(new TextField.CreateInfo { Capacity = 16 }).OnTextChanged(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new TextField(new TextField.CreateInfo { Capacity = 16 }).OnSubmitted(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new ScrollView(default(ScrollView.CreateInfo)).OnScrollChanged(null!)).ParamName);
    }
}
