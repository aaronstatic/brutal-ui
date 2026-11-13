using System;
using System.Text;
using Brutal.Localization;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the text group of <see cref="ElementBuilder"/>: the three <c>Text</c> overloads of <see cref="Label"/>,
/// <see cref="Button"/>, and <see cref="TextField"/>, the typed return, and the null checks.
/// </summary>
/// <remarks>
/// No test uses the static <c>LStrings</c> class. Each test makes its own <see cref="LString"/>, so parallel tests
/// share no state. A test passes a string local, because <c>BRUTALCORE0011</c> rejects a string literal at the
/// <c>string?</c> overload.
/// </remarks>
public sealed class ElementBuilderTextTests
{
    /// <summary>Each overload sets the bytes of a label. The string overload clears the localizable text.</summary>
    [Fact]
    public void TextOfALabelSetsTheBytesOfEachOverload()
    {
        Label label = new Label(default(Label.CreateInfo));

        label.Text("Hello"u8);
        Assert.Equal("Hello", Encoding.UTF8.GetString(label.Text));
        Assert.Null(label.LocalizedText);

        LString hello = new("builder-text.label", "Hello");
        hello.Localize("Bonjour");
        label.Text(hello);
        Assert.Same(hello, label.LocalizedText);
        Assert.Equal("Bonjour", Encoding.UTF8.GetString(label.Text));

        label.Text((string?)null);
        Assert.True(label.Text.IsEmpty);
        Assert.Null(label.LocalizedText);
        label.Text(hello);          // restore for the next assert when needed
        label.Text((LString?)null);
        Assert.True(label.Text.IsEmpty);
        Assert.Null(label.LocalizedText);

        string text = "World";
        label.Text(text);
        Assert.Equal("World", Encoding.UTF8.GetString(label.Text));
        Assert.Null(label.LocalizedText);
    }

    /// <summary>Each overload sets the bytes of a button. The string overload clears the localizable text.</summary>
    [Fact]
    public void TextOfAButtonSetsTheBytesOfEachOverload()
    {
        Button button = new Button(default(Button.CreateInfo));

        button.Text("Go"u8);
        Assert.Equal("Go", Encoding.UTF8.GetString(button.Text));
        Assert.Null(button.LocalizedText);

        LString go = new("builder-text.button", "Go");
        go.Localize("Aller");
        button.Text(go);
        Assert.Same(go, button.LocalizedText);
        Assert.Equal("Aller", Encoding.UTF8.GetString(button.Text));

        button.Text((string?)null);
        Assert.True(button.Text.IsEmpty);
        Assert.Null(button.LocalizedText);
        button.Text(go);          // restore for the next assert when needed
        button.Text((LString?)null);
        Assert.True(button.Text.IsEmpty);
        Assert.Null(button.LocalizedText);

        string text = "Stop";
        button.Text(text);
        Assert.Equal("Stop", Encoding.UTF8.GetString(button.Text));
        Assert.Null(button.LocalizedText);
    }

    /// <summary>Each overload sets the bytes of a field. A long text gets the capacity cut of <c>SetText</c>.</summary>
    [Fact]
    public void TextOfATextFieldSetsTheBytesOfEachOverload()
    {
        TextField field = new TextField(new TextField.CreateInfo { Capacity = 16 });

        field.Text("Hello"u8);
        Assert.Equal("Hello", Encoding.UTF8.GetString(field.Text));

        string text = "World";
        field.Text(text);
        Assert.Equal("World", Encoding.UTF8.GetString(field.Text));

        LString hi = new("builder-text.field", "Hi");
        hi.Localize("Bonjour");
        field.Text(hi);
        Assert.Equal("Bonjour", Encoding.UTF8.GetString(field.Text));

        field.Text((string?)null);
        Assert.True(field.Text.IsEmpty);
        field.Text(hi);          // restore for the next assert when needed
        field.Text((LString?)null);
        Assert.True(field.Text.IsEmpty);

        TextField small = new TextField(new TextField.CreateInfo { Capacity = 8 });
        small.Text("0123456789"u8);
        Assert.Equal("01234567", Encoding.UTF8.GetString(small.Text));
    }

    /// <summary>A chain keeps the widget type, and each call returns the same element.</summary>
    [Fact]
    public void TextReturnsTheSameElementWithItsType()
    {
        Button button = new Button(default(Button.CreateInfo));

        Button chained = button.Text("Go"u8).Class("builder-text.b");

        Assert.True(ReferenceEquals(button, chained));
        Assert.Equal("Go"u8.ToArray(), chained.Text.ToArray());
        Assert.Equal(1, chained.Classes.Count);
    }

    /// <summary>Each overload throws <see cref="ArgumentNullException"/> for a null receiver.</summary>
    [Fact]
    public void TheTextMethodsThrowForANullElement()
    {
        Label nullLabel = null!;
        Button nullButton = null!;
        TextField nullField = null!;

        Assert.Equal("label", Assert.Throws<ArgumentNullException>(() => nullLabel.Text("a"u8)).ParamName);
        Assert.Equal("label", Assert.Throws<ArgumentNullException>(() => nullLabel.Text((string?)null)).ParamName);
        Assert.Equal("label", Assert.Throws<ArgumentNullException>(() => nullLabel.Text((LString?)null)).ParamName);
        Assert.Equal("button", Assert.Throws<ArgumentNullException>(() => nullButton.Text("a"u8)).ParamName);
        Assert.Equal("button", Assert.Throws<ArgumentNullException>(() => nullButton.Text((string?)null)).ParamName);
        Assert.Equal("button", Assert.Throws<ArgumentNullException>(() => nullButton.Text((LString?)null)).ParamName);
        Assert.Equal("field", Assert.Throws<ArgumentNullException>(() => nullField.Text("a"u8)).ParamName);
        Assert.Equal("field", Assert.Throws<ArgumentNullException>(() => nullField.Text((string?)null)).ParamName);
        Assert.Equal("field", Assert.Throws<ArgumentNullException>(() => nullField.Text((LString?)null)).ParamName);
    }
}
