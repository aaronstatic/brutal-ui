using System;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The phase 1 demo: the section 11 template loads, and the tree equals the builder tree. The tests also cover the
/// attribute hook, the class list, XML that is not well formed, and the <see cref="string"/> overload.
/// </summary>
/// <remarks>
/// <para>
/// Each test uses the public API of <see cref="TemplateLoader"/> and <see cref="TreeCompare"/> only, so the tests
/// pass unchanged when element-generator replaces the written table.
/// </para>
/// <para>
/// Each template text is a <c>u8</c> literal, because the BRUTAL analyzer rejects a string literal in
/// <see cref="TemplateLoader.Load(string, string)"/>. The test of that overload passes a local string.
/// </para>
/// </remarks>
public sealed class SectionElevenTemplateTests
{
    /// <summary>Gets the section 11 template of <c>docs/technical-design.md</c>, word for word, with LF line ends.</summary>
    private static ReadOnlySpan<byte> SectionElevenTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="score" class="hud-value" text="0" />
          <ui:Button name="pause" class="hud-button" text="Pause" command="game.pause" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with no end tag for the root element, and with no attribute that needs a handler.</summary>
    private static ReadOnlySpan<byte> NoEndTagTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="score" class="hud-value" text="0" />
        """u8;

    /// <summary>Loads the section 11 template with a handler for <c>command</c>, and compares the tree with the builder tree.</summary>
    [Fact]
    public void TheSectionElevenTemplateEqualsTheBuilderTree()
    {
        TemplateLoader loader = new();
        loader.AddAttributeHandler(HandleCommand);
        Element actual = loader.Load(SectionElevenTemplate, "hud.xml").GetRootOrThrow();

        Element expected = new Element()
            .Class("hud")
            .Child(new Label(default).Name("score").Class("hud-value").Text("0"u8))
            .Child(new Button(default).Name("pause").Class("hud-button").Text("Pause"u8).OnClicked(Clicked));

        Assert.Null(TreeCompare.FirstDifference(expected, actual));
    }

    /// <summary>The hook gets the element, the attribute name, the attribute value, and the position of the name.</summary>
    [Fact]
    public void TheHookGetsTheElementTheNameAndTheValue()
    {
        Element? seenElement = null;
        string? seenName = null;
        string? seenValue = null;
        int seenLine = 0;
        int seenColumn = 0;

        TemplateLoader loader = new();
        loader.AddAttributeHandler(
            (Element element, string name, string value, AttributeContext context) =>
            {
                seenElement = element;
                seenName = name;
                seenValue = value;
                seenLine = context.Line;
                seenColumn = context.Column;
                return name == "command";
            });

        TemplateLoadResult result = loader.Load(SectionElevenTemplate, "hud.xml");

        Assert.True(result.Succeeded);
        Assert.IsType<Button>(seenElement);
        Assert.Equal("command", seenName);
        Assert.Equal("game.pause", seenValue);
        Assert.Equal(3, seenLine);
        Assert.Equal(59, seenColumn);
    }

    /// <summary>An attribute with no row and no handler that takes it gives one <see cref="TemplateErrorKind.UnknownAttribute"/> error.</summary>
    [Fact]
    public void AnAttributeThatNoHandlerTakesIsAnUnknownAttributeError()
    {
        TemplateLoadResult result = new TemplateLoader().Load(SectionElevenTemplate, "hud.xml");

        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
        Assert.Equal(3, error.Line);
        Assert.Equal(59, error.Column);
        Assert.Equal("hud.xml", error.SourceName);
        Assert.Contains("'command'", error.Message);
    }

    /// <summary>The class list holds each name of the value, in the order of the value. An empty value adds none.</summary>
    [Fact]
    public void AClassListGivesEachNameInOrder()
    {
        Element root = new TemplateLoader()
            .Load("""<ui:Element xmlns:ui="brutal-ui" class="a  b&#9;c" />"""u8, "classes.xml")
            .GetRootOrThrow();

        Assert.Equal(3, root.Classes.Count);
        Assert.Equal("a", ClassName.GetName(root.Classes[0]));
        Assert.Equal("b", ClassName.GetName(root.Classes[1]));
        Assert.Equal("c", ClassName.GetName(root.Classes[2]));

        Element empty = new TemplateLoader()
            .Load("""<ui:Element xmlns:ui="brutal-ui" class="" />"""u8, "classes.xml")
            .GetRootOrThrow();

        Assert.Equal(0, empty.Classes.Count);
    }

    /// <summary>XML with no end tag gives one <see cref="TemplateErrorKind.NotWellFormed"/> error and no root.</summary>
    [Fact]
    public void XmlThatIsNotWellFormedGivesOneErrorAndNoRoot()
    {
        TemplateLoadResult result = new TemplateLoader().Load(NoEndTagTemplate, "broken.xml");

        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.NotWellFormed, error.Kind);
        Assert.True(error.Line >= 1);
        Assert.True(error.Column >= 1);
    }

    /// <summary>The <see cref="string"/> overload and the span overload give the same tree.</summary>
    /// <remarks>
    /// The test passes a local string, and calls with the default source name, because the BRUTAL analyzer rejects a
    /// string literal argument for this overload.
    /// </remarks>
    [Fact]
    public void TheStringOverloadGivesTheSameTree()
    {
        string text = Encoding.UTF8.GetString(SectionElevenTemplate);

        TemplateLoader fromText = new();
        fromText.AddAttributeHandler(HandleCommand);
        Element loadedFromText = fromText.Load(text).GetRootOrThrow();

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        TemplateLoader fromBytes = new();
        fromBytes.AddAttributeHandler(HandleCommand);
        Element loadedFromBytes = fromBytes.Load(bytes).GetRootOrThrow();

        Assert.Null(TreeCompare.FirstDifference(loadedFromText, loadedFromBytes));
    }

    /// <summary>
    /// Takes the <c>command</c> attribute of a button, and adds the click handler that the builder tree adds. The
    /// binding-attributes feature registers this attribute later.
    /// </summary>
    /// <param name="element">The element that the factory made.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="context">The position of the attribute. The handler needs no error here.</param>
    /// <returns><see langword="true"/> for <c>command="game.pause"</c> on a button.</returns>
    private static bool HandleCommand(Element element, string name, string value, AttributeContext context)
    {
        _ = context;
        if (name != "command" || value != "game.pause" || element is not Button button)
        {
            return false;
        }

        button.OnClicked(Clicked);
        return true;
    }

    /// <summary>The click handler that both trees add.</summary>
    /// <param name="button">The button that the click calls.</param>
    private static void Clicked(Button button)
    {
        _ = button;
    }
}
