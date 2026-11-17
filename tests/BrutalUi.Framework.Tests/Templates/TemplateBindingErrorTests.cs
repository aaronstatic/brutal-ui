using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The binding errors: each fault of a binding attribute gives one error with its kind, its line, and its column, and
/// the load result holds no root.
/// </summary>
/// <remarks>
/// <para>
/// The handler checks each attribute in this order: the element type, then an empty value, then the key, then the fit
/// of the selector. The element type comes first, so a key that no register call holds still gives
/// <see cref="TemplateErrorKind.UnknownAttribute"/> on the wrong element.
/// </para>
/// <para>
/// No test needs a <see cref="UiContext"/>: a fault gives no root, so the load alone reports each error. An attribute
/// error sits at the first character of its name, so each test computes its line and its column from the text of its
/// own template, as <c>AttributeHookTests</c> does.
/// </para>
/// </remarks>
public sealed class TemplateBindingErrorTests
{
    /// <summary>The template with an unknown key on each bind attribute.</summary>
    private const string UnknownKeyTemplate = """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="Ann" bind-text="hud.nme" />
          <ui:Toggle name="mute" bind-checked="hud.muted" />
          <ui:Slider name="volume" max="100" bind-value="hud.volume" />
        </ui:Element>
        """;

    /// <summary>The template with one selector of the wrong type on each bind attribute.</summary>
    private const string MismatchTemplate = """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="Ann" bind-text="hud.muted" />
          <ui:TextField name="name-field" capacity="16" text="Ann" bind-text="hud.score" />
          <ui:Slider name="volume" max="100" bind-value="hud.name" />
          <ui:Toggle name="mute" bind-checked="hud.volume" />
        </ui:Element>
        """;

    /// <summary>The template with each attribute on an element type that it does not serve.</summary>
    private const string WrongElementTemplate = """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="Ann" command="game.pause" />
          <ui:Button name="pause" text="Pause" bind-text="hud.nme" />
          <ui:Slider name="volume" max="100" bind-checked="hud.nme" />
          <ui:Toggle name="mute" bind-value="hud.nme" />
        </ui:Element>
        """;

    /// <summary>The template with an empty value on each of the four attributes.</summary>
    private const string EmptyValueTemplate = """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Button name="pause" text="Pause" command="" />
          <ui:Label name="player" text="Ann" bind-text="" />
          <ui:Toggle name="mute" bind-checked="" />
          <ui:Slider name="volume" max="100" bind-value="" />
        </ui:Element>
        """;

    /// <summary>The template with spaces around a key that the register call holds with no spaces.</summary>
    private const string SpacedValueTemplate = """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="player" text="Ann" bind-text=" hud.name " />
        </ui:Element>
        """;

    /// <summary>The template with each attribute on the element type that it serves.</summary>
    private const string EachAttributeTemplate = """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Button name="pause" text="Pause" command="game.pause" />
          <ui:Label name="player" text="Ann" bind-text="hud.name" />
          <ui:Toggle name="mute" bind-checked="hud.muted" />
          <ui:Slider name="volume" max="100" bind-value="hud.volume" />
        </ui:Element>
        """;

    /// <summary>
    /// A key that no register call holds gives <see cref="TemplateErrorKind.UnknownBinding"/> on each bind attribute,
    /// with the line and the column of the attribute name.
    /// </summary>
    [Fact]
    public void AnUnknownKeyIsAnUnknownBindingError()
    {
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.score", CreateStore(), static s => s.Score);

        IReadOnlyList<TemplateError> errors = LoadErrors(UnknownKeyTemplate, bindings);

        Assert.Equal(3, errors.Count);
        Assert.Equal(TemplateErrorKind.UnknownBinding, errors[0].Kind);
        Assert.Equal(TemplateErrorKind.UnknownBinding, errors[1].Kind);
        Assert.Equal(TemplateErrorKind.UnknownBinding, errors[2].Kind);
        AssertAttribute(errors[0], UnknownKeyTemplate, "bind-text", "hud.nme");
        AssertAttribute(errors[1], UnknownKeyTemplate, "bind-checked", "hud.muted");
        AssertAttribute(errors[2], UnknownKeyTemplate, "bind-value", "hud.volume");
        Assert.Contains("'hud.nme'", errors[0].Message);
        Assert.Contains("'hud.muted'", errors[1].Message);
        Assert.Contains("'hud.volume'", errors[2].Message);
    }

    /// <summary>
    /// A selector of the wrong type gives <see cref="TemplateErrorKind.BindingTypeMismatch"/>: a bool key on a label,
    /// a number key on a text field (A5), a string key on <c>bind-value</c>, and a float key on
    /// <c>bind-checked</c>.
    /// </summary>
    [Fact]
    public void ASelectorOfTheWrongTypeIsABindingTypeMismatchError()
    {
        Store<BoundTree.HudState> store = CreateStore();
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddChecked("hud.muted", store, static s => s.Muted);
        bindings.AddText("hud.score", store, static s => s.Score);
        bindings.AddText("hud.name", store, static s => s.Name);
        bindings.AddValue("hud.volume", store, static s => s.Volume);

        IReadOnlyList<TemplateError> errors = LoadErrors(MismatchTemplate, bindings);

        Assert.Equal(4, errors.Count);
        Assert.Equal(TemplateErrorKind.BindingTypeMismatch, errors[0].Kind);
        Assert.Equal(TemplateErrorKind.BindingTypeMismatch, errors[1].Kind);
        Assert.Equal(TemplateErrorKind.BindingTypeMismatch, errors[2].Kind);
        Assert.Equal(TemplateErrorKind.BindingTypeMismatch, errors[3].Kind);
        AssertAttribute(errors[0], MismatchTemplate, "bind-text", "hud.muted");
        AssertAttribute(errors[1], MismatchTemplate, "bind-text", "hud.score");
        AssertAttribute(errors[2], MismatchTemplate, "bind-value", "hud.name");
        AssertAttribute(errors[3], MismatchTemplate, "bind-checked", "hud.volume");
        Assert.Contains("'hud.muted'", errors[0].Message);
        Assert.Contains("'hud.score'", errors[1].Message);
        Assert.Contains("'hud.name'", errors[2].Message);
        Assert.Contains("'hud.volume'", errors[3].Message);
        Assert.Contains("holds a bool selector.", errors[0].Message);
        Assert.Contains("holds a number selector.", errors[1].Message);
        Assert.Contains("holds a string selector.", errors[2].Message);
        Assert.Contains("holds a float selector.", errors[3].Message);
    }

    /// <summary>
    /// An attribute on the wrong element type gives <see cref="TemplateErrorKind.UnknownAttribute"/> (A3), even for a
    /// key that no register call holds, so the element check comes before the key check.
    /// </summary>
    [Fact]
    public void AnAttributeOnTheWrongElementIsAnUnknownAttributeError()
    {
        TemplateBindings bindings = new(new CommandRegistry());

        IReadOnlyList<TemplateError> errors = LoadErrors(WrongElementTemplate, bindings);

        Assert.Equal(4, errors.Count);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[0].Kind);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[1].Kind);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[2].Kind);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[3].Kind);
        AssertAttribute(errors[0], WrongElementTemplate, "command", "game.pause");
        AssertAttribute(errors[1], WrongElementTemplate, "bind-text", "hud.nme");
        AssertAttribute(errors[2], WrongElementTemplate, "bind-checked", "hud.nme");
        AssertAttribute(errors[3], WrongElementTemplate, "bind-value", "hud.nme");
        Assert.Contains("'command'", errors[0].Message);
        Assert.Contains("'bind-text'", errors[1].Message);
        Assert.Contains("'bind-checked'", errors[2].Message);
        Assert.Contains("'bind-value'", errors[3].Message);
    }

    /// <summary>An empty value gives <see cref="TemplateErrorKind.InvalidValue"/> (A2), not an unknown key, on each
    /// of the four attributes.</summary>
    [Fact]
    public void AnEmptyValueIsAnInvalidValueError()
    {
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.name", CreateStore(), static s => s.Name);

        IReadOnlyList<TemplateError> errors = LoadErrors(EmptyValueTemplate, bindings);

        Assert.Equal(4, errors.Count);
        Assert.Equal(TemplateErrorKind.InvalidValue, errors[0].Kind);
        Assert.Equal(TemplateErrorKind.InvalidValue, errors[1].Kind);
        Assert.Equal(TemplateErrorKind.InvalidValue, errors[2].Kind);
        Assert.Equal(TemplateErrorKind.InvalidValue, errors[3].Kind);
        AssertAttribute(errors[0], EmptyValueTemplate, "command", string.Empty);
        AssertAttribute(errors[1], EmptyValueTemplate, "bind-text", string.Empty);
        AssertAttribute(errors[2], EmptyValueTemplate, "bind-checked", string.Empty);
        AssertAttribute(errors[3], EmptyValueTemplate, "bind-value", string.Empty);
        Assert.Contains("'command'", errors[0].Message);
        Assert.Contains("'bind-text'", errors[1].Message);
        Assert.Contains("'bind-checked'", errors[2].Message);
        Assert.Contains("'bind-value'", errors[3].Message);
    }

    /// <summary>
    /// The handler uses the attribute value as is, with no trim (Decision 5 and Notes item 6). A spaced value is a
    /// different key, so it gives <see cref="TemplateErrorKind.UnknownBinding"/> with the spaces inside the quote.
    /// </summary>
    /// <remarks>
    /// The rest of Markup trims: <c>AttributeContext.TryRead*</c> calls <c>TemplateValues.Trim</c> on each value read.
    /// The four binding attributes are the one exception, and this test pins the rule. A later change that adds a
    /// <c>Trim</c> must update the plan, the docs, and this test.
    /// </remarks>
    [Fact]
    public void ASpacedValueIsUsedAsIsWithNoTrim()
    {
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.name", CreateStore(), static s => s.Name);

        IReadOnlyList<TemplateError> errors = LoadErrors(SpacedValueTemplate, bindings);

        Assert.Single(errors);
        Assert.Equal(TemplateErrorKind.UnknownBinding, errors[0].Kind);
        AssertAttribute(errors[0], SpacedValueTemplate, "bind-text", " hud.name ");
        Assert.Contains("' hud.name '", errors[0].Message);
    }

    /// <summary>
    /// A loader with no attached bindings gives <see cref="TemplateErrorKind.UnknownAttribute"/> for each of the four
    /// names (A6), as it does today.
    /// </summary>
    [Fact]
    public void ALoaderWithNoBindingsRejectsEachAttribute()
    {
        TemplateLoader loader = new();
        TemplateLoadResult result = loader.Load(Bytes(EachAttributeTemplate), "hud.xml");

        Assert.Null(result.Root);
        IReadOnlyList<TemplateError> errors = result.Errors;
        Assert.Equal(4, errors.Count);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[0].Kind);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[1].Kind);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[2].Kind);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, errors[3].Kind);
        AssertAttribute(errors[0], EachAttributeTemplate, "command", "game.pause");
        AssertAttribute(errors[1], EachAttributeTemplate, "bind-text", "hud.name");
        AssertAttribute(errors[2], EachAttributeTemplate, "bind-checked", "hud.muted");
        AssertAttribute(errors[3], EachAttributeTemplate, "bind-value", "hud.volume");
    }

    /// <summary>The text of each new kind names the kind after the position.</summary>
    [Fact]
    public void TheErrorTextNamesTheNewKinds()
    {
        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddChecked("hud.muted", CreateStore(), static s => s.Muted);

        IReadOnlyList<TemplateError> unknown = LoadErrors(UnknownKeyTemplate, bindings);
        string unknownText = unknown[0].ToString();
        Assert.Contains("hud.xml(", unknownText, StringComparison.Ordinal);
        Assert.Contains("UnknownBinding: ", unknownText, StringComparison.Ordinal);

        Store<BoundTree.HudState> store = CreateStore();
        TemplateBindings mismatchBindings = new(new CommandRegistry());
        mismatchBindings.AddChecked("hud.muted", store, static s => s.Muted);
        IReadOnlyList<TemplateError> mismatch = LoadErrors(MismatchTemplate, mismatchBindings);
        Assert.Contains("BindingTypeMismatch: ", mismatch[0].ToString(), StringComparison.Ordinal);
    }

    /// <summary>Attaches the bindings to a new loader, loads the template, and returns the errors of the load.</summary>
    /// <param name="template">The template text.</param>
    /// <param name="bindings">The bindings to attach. The handler takes each of the four names.</param>
    /// <returns>The errors, in the order of the source.</returns>
    private static IReadOnlyList<TemplateError> LoadErrors(string template, TemplateBindings bindings)
    {
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        TemplateLoadResult result = loader.Load(Bytes(template), "hud.xml");
        Assert.Null(result.Root);
        return result.Errors;
    }

    /// <summary>Checks one error against the position of its attribute name in the template text.</summary>
    /// <param name="error">The error from the load.</param>
    /// <param name="template">The template text.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value of the attribute, or an empty text.</param>
    private static void AssertAttribute(TemplateError error, string template, string attributeName, string value)
    {
        string needle = attributeName + "=\"" + value + "\"";
        int at = template.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {needle} attribute.");

        int line = 1;
        int lineStart = 0;
        for (int i = 0; i < at; i++)
        {
            if (template[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        Assert.Equal(line, error.Line);
        Assert.Equal(at - lineStart + 1, error.Column);
    }

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    /// <param name="template">The template text.</param>
    /// <returns>The bytes.</returns>
    private static byte[] Bytes(string template) => Encoding.UTF8.GetBytes(template);

    /// <summary>Creates a store of the HUD state with a start value for each slice.</summary>
    /// <returns>The store.</returns>
    private static Store<BoundTree.HudState> CreateStore()
    {
        return new Store<BoundTree.HudState>(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = new BoundTree.HudState(0, 0, 0f, false, "Ann"),
        });
    }
}
