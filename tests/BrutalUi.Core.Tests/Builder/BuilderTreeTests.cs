using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The whole-tree tests of the builder: the eight-type tree from the fluent methods equals the tree from
/// initializers, and the slider chain applies the value rule to the range that stands before the value.
/// </summary>
public sealed class BuilderTreeTests
{
    /// <summary>A texture for the image case: id 3, size 64 by 32.</summary>
    private static readonly TextureHandle Texture = new(3, new int2(64, 32));

    /// <summary>
    /// A tree with one element of each of the eight types, built with the fluent methods, equals the same tree
    /// built with create-info fields, object initializers, and direct property writes.
    /// </summary>
    [Fact]
    public void AnEightTypeTreeFromTheFluentMethodsEqualsTheInitializerTree()
    {
        Element fluent = new Element()
            .Name("root")
            .Class("hud")
            .Class("panel")
            .Pickable(false)
            .Focusable(true)
            .Child(new Label(default).Name("score").Text("0"u8))
            .Child(new Button(default).Name("pause").Text("Pause"u8))
            .Child(new Toggle(default).Name("switch").Checked(true))
            .Child(new Slider(default).Name("volume").Range(0f, 10f, 1f).Value(5f))
            .Child(new TextField(new TextField.CreateInfo { Capacity = 16 }).Name("name").Text("Ada"u8))
            .Child(new Image(default)
                .Name("icon")
                .Texture(Texture)
                .Uv(new float4(0.25f, 0.25f, 0.75f, 0.75f))
                .Tint(new Rgba8(255, 0, 0))
                .ScaleMode(ImageScaleMode.ScaleAndCrop))
            .Child(new ScrollView(default).Name("log").ScrollOffset(new float2(0f, 40f)));

        Element reference = new() { Name = "root", IsPickable = false, IsFocusable = true };
        reference.Classes.Add(ClassName.Intern("hud"));
        reference.Classes.Add(ClassName.Intern("panel"));
        Label score = new Label(new Label.CreateInfo { Text = "0" }) { Name = "score" };
        Button pause = new Button(new Button.CreateInfo { Text = "Pause" }) { Name = "pause" };
        Toggle toggle = new Toggle(new Toggle.CreateInfo { Checked = true }) { Name = "switch" };
        Slider slider = new Slider(new Slider.CreateInfo { Minimum = 0f, Maximum = 10f, Step = 1f, Value = 5f }) { Name = "volume" };
        TextField field = new TextField(new TextField.CreateInfo { Capacity = 16, Text = "Ada" }) { Name = "name" };
        Image image = new Image(new Image.CreateInfo
        {
            Texture = Texture,
            Uv = new float4(0.25f, 0.25f, 0.75f, 0.75f),
            Tint = new Rgba8(255, 0, 0),
            ScaleMode = ImageScaleMode.ScaleAndCrop,
        })
        { Name = "icon" };
        ScrollView view = new ScrollView(new ScrollView.CreateInfo()) { Name = "log", ScrollOffset = new float2(0f, 40f) };
        reference.Children.Add(score);
        reference.Children.Add(pause);
        reference.Children.Add(toggle);
        reference.Children.Add(slider);
        reference.Children.Add(field);
        reference.Children.Add(image);
        reference.Children.Add(view);

        Assert.Null(TreeCompare.FirstDifference(reference, fluent));
    }

    /// <summary>
    /// <c>Value</c> clamps to the current range. A chain that sets the value before the range keeps the clamp of
    /// the old range, and a chain that sets the range first gets the value.
    /// </summary>
    [Fact]
    public void ValueBeforeRangeClampsToTheOldRange()
    {
        Slider valueFirst = new Slider(new Slider.CreateInfo { Maximum = 1f }).Value(5f).Range(0f, 10f, 0f);

        Assert.Equal(1f, valueFirst.Value);

        Slider rangeFirst = new Slider(new Slider.CreateInfo { Maximum = 1f }).Range(0f, 10f, 0f).Value(5f);

        Assert.Equal(5f, rangeFirst.Value);
    }

    /// <summary>
    /// The eight-type tree with one handler on each of the six widget slots and on three event slots equals the
    /// initializer tree that sets the same static handlers with <c>=</c> and <c>+=</c>.
    /// </summary>
    [Fact]
    public void ATreeWithHandlersFromTheFluentMethodsEqualsTheInitializerTree()
    {
        Element fluent = new Element()
            .Name("root")
            .Class("hud")
            .Class("panel")
            .Pickable(false)
            .Focusable(true)
            .PointerDown(PointerDownHandler)
            .KeyDown(KeyDownHandler)
            .Char(CharHandler)
            .Child(new Label(default).Name("score").Text("0"u8))
            .Child(new Button(default).Name("pause").Text("Pause"u8).OnClicked(ClickHandler))
            .Child(new Toggle(default).Name("switch").Checked(true).OnCheckedChanged(CheckedChangedHandler))
            .Child(new Slider(default).Name("volume").Range(0f, 10f, 1f).Value(5f).OnValueChanged(ValueChangedHandler))
            .Child(new TextField(new TextField.CreateInfo { Capacity = 16 })
                .Name("name")
                .Text("Ada"u8)
                .OnTextChanged(TextChangedHandler)
                .OnSubmitted(SubmittedHandler))
            .Child(new Image(default)
                .Name("icon")
                .Texture(Texture)
                .Uv(new float4(0.25f, 0.25f, 0.75f, 0.75f))
                .Tint(new Rgba8(255, 0, 0))
                .ScaleMode(ImageScaleMode.ScaleAndCrop))
            .Child(new ScrollView(default).Name("log").ScrollOffset(new float2(0f, 40f)).OnScrollChanged(ScrollChangedHandler));

        Element reference = new() { Name = "root", IsPickable = false, IsFocusable = true };
        reference.Classes.Add(ClassName.Intern("hud"));
        reference.Classes.Add(ClassName.Intern("panel"));
        reference.OnPointerDown = PointerDownHandler;
        reference.OnKeyDown += KeyDownHandler;
        reference.OnChar += CharHandler;
        Label score = new Label(new Label.CreateInfo { Text = "0" }) { Name = "score" };
        Button pause = new Button(new Button.CreateInfo { Text = "Pause" }) { Name = "pause" };
        pause.Clicked = ClickHandler;
        Toggle toggle = new Toggle(new Toggle.CreateInfo { Checked = true }) { Name = "switch" };
        toggle.CheckedChanged += CheckedChangedHandler;
        Slider slider = new Slider(new Slider.CreateInfo { Minimum = 0f, Maximum = 10f, Step = 1f, Value = 5f }) { Name = "volume" };
        slider.ValueChanged = ValueChangedHandler;
        TextField field = new TextField(new TextField.CreateInfo { Capacity = 16, Text = "Ada" }) { Name = "name" };
        field.TextChanged += TextChangedHandler;
        field.Submitted = SubmittedHandler;
        Image image = new Image(new Image.CreateInfo
        {
            Texture = Texture,
            Uv = new float4(0.25f, 0.25f, 0.75f, 0.75f),
            Tint = new Rgba8(255, 0, 0),
            ScaleMode = ImageScaleMode.ScaleAndCrop,
        })
        { Name = "icon" };
        ScrollView view = new ScrollView(new ScrollView.CreateInfo()) { Name = "log", ScrollOffset = new float2(0f, 40f) };
        view.ScrollChanged += ScrollChangedHandler;
        reference.Children.Add(score);
        reference.Children.Add(pause);
        reference.Children.Add(toggle);
        reference.Children.Add(slider);
        reference.Children.Add(field);
        reference.Children.Add(image);
        reference.Children.Add(view);

        Assert.Null(TreeCompare.FirstDifference(reference, fluent));
    }

    private static void PointerDownHandler(ref PointerEvent e)
    {
    }

    private static void KeyDownHandler(ref KeyEvent e)
    {
    }

    private static void CharHandler(ref CharEvent e)
    {
    }

    private static void ClickHandler(Button button)
    {
    }

    private static void CheckedChangedHandler(Toggle toggle)
    {
    }

    private static void ValueChangedHandler(Slider slider)
    {
    }

    private static void TextChangedHandler(TextField field)
    {
    }

    private static void SubmittedHandler(TextField field)
    {
    }

    private static void ScrollChangedHandler(ScrollView view)
    {
    }
}
