using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The row tests of the eight built-in types: each test reads one row back from the element that the loader made.
/// </summary>
/// <remarks>
/// <para>
/// Each test uses the public API of <see cref="TemplateLoader"/> only, so the tests pass unchanged when
/// element-generator replaces the written table.
/// </para>
/// <para>
/// Each template text is a <c>u8</c> literal, because the BRUTAL analyzer rejects a string literal in
/// <see cref="TemplateLoader.Load(string, string)"/>.
/// </para>
/// </remarks>
public sealed class BuiltInTypeTests
{
    /// <summary>Gets a template with each base row on the root.</summary>
    private static ReadOnlySpan<byte> BaseRowsTemplate => """
        <ui:Element xmlns:ui="brutal-ui" name="root" class="hud panel" pickable="false" focusable="true" />
        """u8;

    /// <summary>Gets a template with a <c>Label</c> that holds a text.</summary>
    private static ReadOnlySpan<byte> LabelTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Label text="Score 12" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <c>Button</c> that holds a text.</summary>
    private static ReadOnlySpan<byte> ButtonTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Button text="Pause" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a checked <c>Toggle</c>.</summary>
    private static ReadOnlySpan<byte> ToggleTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Toggle checked="true" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <c>Slider</c> whose <c>value</c> comes before the range.</summary>
    private static ReadOnlySpan<byte> SliderTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Slider value="7" min="0" max="10" step="1" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <c>Slider</c> whose <c>value</c> is above the range.</summary>
    private static ReadOnlySpan<byte> SliderAboveRangeTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Slider value="15" min="0" max="10" step="1" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <c>TextField</c> that holds a capacity and a text.</summary>
    private static ReadOnlySpan<byte> TextFieldTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:TextField capacity="8" text="abc" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <c>ScrollView</c> that holds a speed and an offset.</summary>
    private static ReadOnlySpan<byte> ScrollViewTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:ScrollView scroll-speed="60" scroll-offset="0 30" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <c>ScrollView</c> that holds no attribute of its own.</summary>
    private static ReadOnlySpan<byte> ScrollViewPlainTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:ScrollView />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <c>ScrollView</c> whose speed is 0.</summary>
    private static ReadOnlySpan<byte> ScrollViewZeroSpeedTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:ScrollView scroll-speed="0" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with an <c>Image</c> that holds each of its four rows.</summary>
    private static ReadOnlySpan<byte> ImageTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Image texture="icon" uv="0 0 0.5 0.5" tint="#ff000080" scale-mode="scale-to-fit" />
        </ui:Element>
        """u8;

    /// <summary>Loads the base rows and checks each value on the root.</summary>
    [Fact]
    public void AnElementTakesNameClassPickableAndFocusable()
    {
        Element root = new TemplateLoader().Load(BaseRowsTemplate, "types.xml").GetRootOrThrow();

        Assert.Equal("root", root.Name);
        Assert.Equal(2, root.Classes.Count);
        Assert.Equal("hud", ClassName.GetName(root.Classes[0]));
        Assert.Equal("panel", ClassName.GetName(root.Classes[1]));
        Assert.False(root.IsPickable);
        Assert.True(root.IsFocusable);
    }

    /// <summary>The <c>text</c> row of a <see cref="Label"/> gives the text bytes.</summary>
    [Fact]
    public void ALabelTakesText()
    {
        Element root = new TemplateLoader().Load(LabelTemplate, "types.xml").GetRootOrThrow();
        Label label = Assert.IsType<Label>(root.Children[0]);

        Assert.True(label.Text.SequenceEqual("Score 12"u8));
    }

    /// <summary>The <c>text</c> row of a <see cref="Button"/> gives the text bytes.</summary>
    [Fact]
    public void AButtonTakesText()
    {
        Element root = new TemplateLoader().Load(ButtonTemplate, "types.xml").GetRootOrThrow();
        Button button = Assert.IsType<Button>(root.Children[0]);

        Assert.True(button.Text.SequenceEqual("Pause"u8));
    }

    /// <summary>
    /// The <c>checked</c> row sets <see cref="PseudoStates.Checked"/>. A handler that the test adds after the load
    /// gets no call, because the row writes the value as code does.
    /// </summary>
    [Fact]
    public void AToggleTakesChecked()
    {
        Element root = new TemplateLoader().Load(ToggleTemplate, "types.xml").GetRootOrThrow();
        Toggle toggle = Assert.IsType<Toggle>(root.Children[0]);

        Assert.True((toggle.States & PseudoStates.Checked) != 0);
        Assert.True(toggle.Checked);

        Counter counter = new();
        toggle.OnCheckedChanged(counter.OnChanged);

        // A set from code calls no handler, so the counter stays 0.
        toggle.Checked = false;
        toggle.Checked = true;
        Assert.Equal(0, counter.Calls);
    }

    /// <summary>The factory makes the range before the <c>value</c> row writes, so <c>value</c> clamps to the range.</summary>
    [Fact]
    public void ASliderTakesTheRangeBeforeTheValue()
    {
        Element root = new TemplateLoader().Load(SliderTemplate, "types.xml").GetRootOrThrow();
        Slider slider = Assert.IsType<Slider>(root.Children[0]);

        Assert.Equal(0f, slider.Minimum);
        Assert.Equal(10f, slider.Maximum);
        Assert.Equal(1f, slider.Step);
        Assert.Equal(7f, slider.Value);

        Element above = new TemplateLoader().Load(SliderAboveRangeTemplate, "types.xml").GetRootOrThrow();
        Assert.Equal(10f, Assert.IsType<Slider>(above.Children[0]).Value);
    }

    /// <summary>The <c>capacity</c> and <c>text</c> rows give the capacity and the text bytes of the field.</summary>
    [Fact]
    public void ATextFieldTakesCapacityAndText()
    {
        Element root = new TemplateLoader().Load(TextFieldTemplate, "types.xml").GetRootOrThrow();
        TextField field = Assert.IsType<TextField>(root.Children[0]);

        Assert.Equal(8, field.Capacity);
        Assert.True(field.Text.SequenceEqual("abc"u8));
    }

    /// <summary>
    /// The <c>scroll-speed</c> row gives the speed. No attribute and a 0 give <see cref="ScrollView.DefaultScrollSpeed"/>.
    /// The <c>scroll-offset</c> row writes the offset, so a new tree reads (0, 0), as the builder method does.
    /// </summary>
    [Fact]
    public void AScrollViewTakesScrollSpeedAndScrollOffset()
    {
        Element root = new TemplateLoader().Load(ScrollViewTemplate, "types.xml").GetRootOrThrow();
        ScrollView view = Assert.IsType<ScrollView>(root.Children[0]);

        Assert.Equal(60f, view.ScrollSpeed);
        Assert.Equal(new float2(0f, 0f), view.ScrollOffset);

        Element plain = new TemplateLoader().Load(ScrollViewPlainTemplate, "types.xml").GetRootOrThrow();
        Assert.Equal(ScrollView.DefaultScrollSpeed, Assert.IsType<ScrollView>(plain.Children[0]).ScrollSpeed);

        Element zero = new TemplateLoader().Load(ScrollViewZeroSpeedTemplate, "types.xml").GetRootOrThrow();
        Assert.Equal(ScrollView.DefaultScrollSpeed, Assert.IsType<ScrollView>(zero.Children[0]).ScrollSpeed);

        ScrollView built = new ScrollView(default).ScrollOffset(new float2(0f, 30f));
        Assert.Equal(new float2(0f, 0f), built.ScrollOffset);
    }

    /// <summary>The four rows of an <see cref="Image"/> give the texture, the uv, the tint, and the scale mode.</summary>
    [Fact]
    public void AnImageTakesTextureUvTintAndScaleMode()
    {
        Dictionary<string, TextureHandle> textures = new(StringComparer.Ordinal)
        {
            ["icon"] = new TextureHandle(7, new int2(16, 16)),
        };
        TemplateLoader loader = new(new TemplateLoader.CreateInfo { Textures = textures });

        Element root = loader.Load(ImageTemplate, "types.xml").GetRootOrThrow();
        Image image = Assert.IsType<Image>(root.Children[0]);

        Assert.Equal(7, image.Texture.Id);
        Assert.Equal(new int2(16, 16), image.Texture.Size);
        Assert.Equal(new float4(0f, 0f, 0.5f, 0.5f), image.Uv);
        Assert.Equal(new Rgba8(0xff, 0x00, 0x00, 0x80), image.Tint);
        Assert.Equal(ImageScaleMode.ScaleToFit, image.ScaleMode);
    }

    /// <summary>The constructor copies the texture map, so a later change of the map changes no load.</summary>
    [Fact]
    public void TheLoaderCopiesTheTextureMap()
    {
        Dictionary<string, TextureHandle> textures = new(StringComparer.Ordinal)
        {
            ["icon"] = new TextureHandle(7, new int2(16, 16)),
        };
        TemplateLoader loader = new(new TemplateLoader.CreateInfo { Textures = textures });

        textures["icon"] = new TextureHandle(9, new int2(8, 8));
        textures["late"] = new TextureHandle(10, new int2(8, 8));

        Element root = loader.Load(ImageTemplate, "types.xml").GetRootOrThrow();
        Assert.Equal(7, Assert.IsType<Image>(root.Children[0]).Texture.Id);

        TemplateLoadResult late = loader.Load(LateTextureTemplate, "types.xml");
        Assert.Null(late.Root);
        TemplateError error = Assert.Single(late.Errors);
        Assert.Equal(TemplateErrorKind.UnknownTexture, error.Kind);
        Assert.Contains("'late'", error.Message);
    }

    /// <summary>Gets a template that reads a name the copied map did not hold at the construction.</summary>
    private static ReadOnlySpan<byte> LateTextureTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Image texture="late" />
        </ui:Element>
        """u8;

    /// <summary>Counts the calls of the checked handler of a toggle.</summary>
    private sealed class Counter
    {
        /// <summary>Gets the count of the calls.</summary>
        public int Calls;

        /// <summary>Counts one call.</summary>
        /// <param name="toggle">The toggle that the change calls.</param>
        public void OnChanged(Toggle toggle)
        {
            _ = toggle;
            Calls++;
        }
    }
}
