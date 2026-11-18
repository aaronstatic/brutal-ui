using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The positioned definition errors of phase 3: each kind with its line, column, and message, the duplicate check,
/// the slot check, the check load of each sound layout (Q3, A5, Decision 15), the follow-on rule, the sort, and the
/// text form. Each test loads with the source name <c>defs.xml</c> and checks each position against the text, the
/// way <c>TemplateErrorTests</c> does in Markup. One <see cref="DefinitionLoader"/> serves one
/// <see cref="TemplateLoader"/>, so each load makes its own pair.
/// </summary>
public sealed partial class DefinitionErrorTests
{
    /// <summary>Gets the panel template: one element with no child.</summary>
    private static ReadOnlySpan<byte> PanelTemplate => "<ui:Element name=\"de.hudRoot\" xmlns:ui=\"brutal-ui\" />"u8;

    /// <summary>Gets the layout template: a root with the slots <c>top</c> and <c>bottom</c>.</summary>
    private static ReadOnlySpan<byte> LayoutTemplate => """
        <ui:Element pickable="false" xmlns:ui="brutal-ui"><ui:SlotContainer slot="top" /><ui:SlotContainer slot="bottom" /></ui:Element>
        """u8;

    /// <summary>Two entries that name an unregistered template give two <c>UnknownTemplate</c> errors at the two
    /// <c>template</c> attributes, with the add-it message.</summary>
    [Fact]
    public void AnUnknownTemplateNameIsAnUnknownTemplateError()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" template="hud.panl" /><ui:Layout name="de.game" template="game.layout" />
            </ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> errors = LoadErrors(CreateLoader(), Text);

        Assert.Equal(2, errors.Count);
        Assert.Equal(DefinitionErrorKind.UnknownTemplate, errors[0].Kind);
        AssertAt(errors[0], Text, "template=\"hud.panl\"");
        Assert.Equal(
            "'hud.panl' is not a registered template. Add it with DefinitionLoader.AddTemplate before the load.",
            errors[0].Message);
        Assert.Equal(DefinitionErrorKind.UnknownTemplate, errors[1].Kind);
        AssertAt(errors[1], Text, "template=\"game.layout\"");
    }

    /// <summary>A mode that names an unregistered map gives one <c>UnknownActionMap</c> error at the
    /// <c>actions</c> attribute.</summary>
    [Fact]
    public void AnUnknownActionMapNameIsAnUnknownActionMapError()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" template="de.panel" /><ui:Layout name="de.game" template="de.layout" />
            <ui:Mode name="de.play" layout="de.game" actions="play.map"><ui:Slot name="top"><ui:PanelRef name="de.hud" /></ui:Slot></ui:Mode>
            </ui:Definitions>
            """;
        DefinitionError error = Assert.Single(LoadErrors(CreateLoader(), Text));

        Assert.Equal(DefinitionErrorKind.UnknownActionMap, error.Kind);
        AssertAt(error, Text, "actions=\"play.map\"");
        Assert.Equal(
            "'play.map' is not a registered action map. Add it with DefinitionLoader.AddActionMap before the load.",
            error.Message);
    }

    /// <summary>A mode on an unknown layout gives <c>UnknownLayout</c> at the <c>layout</c> attribute, and a mode
    /// that places an unknown panel gives <c>UnknownPanel</c> at the <c>PanelRef</c> name.</summary>
    [Fact]
    public void AnUnknownLayoutOrPanelNameIsAnError()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" template="de.panel" /><ui:Layout name="de.game" template="de.layout" />
            <ui:Mode name="de.a" layout="gam" /><ui:Mode name="de.b" layout="de.game"><ui:PanelRef name="hdu" /></ui:Mode>
            </ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> errors = LoadErrors(CreateLoader(), Text);

        Assert.Equal(2, errors.Count);
        Assert.Equal(DefinitionErrorKind.UnknownLayout, errors[0].Kind);
        AssertAt(errors[0], Text, "layout=\"gam\"");
        Assert.Equal("'gam' is not a layout of this text. Add a Layout entry with this name.", errors[0].Message);
        Assert.Equal(DefinitionErrorKind.UnknownPanel, errors[1].Kind);
        AssertAt(errors[1], Text, "name=\"hdu\"");
        Assert.Equal("'hdu' is not a panel of this text. Add a Panel entry with this name.", errors[1].Message);
    }

    /// <summary>Two panels, two layouts, and two modes of one name, and one panel placed two times in one mode,
    /// give four <c>DuplicateName</c> errors in source order, each at the name of the later entry.</summary>
    [Fact]
    public void ADuplicateNameIsADuplicateNameError()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" template="de.panel" />
            <ui:Panel name="de.hud" slot="bottom" template="de.panel" />
            <ui:Layout name="de.game" template="de.layout" /><ui:Layout name="de.game" template="de.layout" />
            <ui:Mode name="de.play" layout="de.game"><ui:PanelRef name="de.hud" /></ui:Mode>
            <ui:Mode name="de.play" layout="de.game" />
            <ui:Mode name="de.two" layout="de.game"><ui:PanelRef name="de.hud" /><ui:PanelRef name="de.hud" /></ui:Mode>
            </ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> errors = LoadErrors(CreateLoader(), Text);

        Assert.Equal(4, errors.Count);
        Assert.Equal(DefinitionErrorKind.DuplicateName, errors[0].Kind);
        AssertAt(errors[0], Text, "name=\"de.hud\"", 2);
        Assert.Equal("'de.hud' names a panel already. Give each panel one name.", errors[0].Message);
        Assert.Equal(DefinitionErrorKind.DuplicateName, errors[1].Kind);
        AssertAt(errors[1], Text, "name=\"de.game\"", 2);
        Assert.Equal("'de.game' names a layout already. Give each layout one name.", errors[1].Message);
        Assert.Equal(DefinitionErrorKind.DuplicateName, errors[2].Kind);
        AssertAt(errors[2], Text, "name=\"de.play\"", 2);
        Assert.Equal("'de.play' names a mode already. Give each mode one name.", errors[2].Message);
        Assert.Equal(DefinitionErrorKind.DuplicateName, errors[3].Kind);
        AssertAt(errors[3], Text, "name=\"de.hud\"", 5);
        Assert.Equal("'de.hud' is in this mode already. A mode holds a panel one time.", errors[3].Message);
    }

    /// <summary>The demo case: a <c>Slot name="left"</c> and a direct <c>PanelRef</c> to a panel with
    /// <c>slot="left"</c> on a layout with the slots <c>top</c> and <c>bottom</c> give two <c>UnknownSlot</c>
    /// errors, and the result holds no record.</summary>
    [Fact]
    public void ASlotThatTheLayoutDoesNotHoldIsAnUnknownSlotError()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" template="de.panel" />
            <ui:Panel name="de.chat" slot="left" template="de.panel" />
            <ui:Layout name="de.game" template="de.layout" />
            <ui:Mode name="de.play" layout="de.game">
            <ui:Slot name="left"><ui:PanelRef name="de.hud" /></ui:Slot><ui:PanelRef name="de.chat" />
            </ui:Mode>
            </ui:Definitions>
            """;
        DefinitionLoadResult result = Load(CreateLoader(), Text);
        IReadOnlyList<DefinitionError> errors = result.Errors;

        Assert.Equal(2, errors.Count);
        Assert.Equal(DefinitionErrorKind.UnknownSlot, errors[0].Kind);
        AssertAt(errors[0], Text, "name=\"left\"");
        Assert.Equal("'left' is not a slot of the layout 'de.game'. The slots are top, bottom.", errors[0].Message);
        Assert.Equal(DefinitionErrorKind.UnknownSlot, errors[1].Kind);
        AssertAt(errors[1], Text, "name=\"de.chat\"", 2);
        Assert.Equal(
            "The panel 'de.chat' has the default slot 'left', and the layout 'de.game' holds no such slot. "
                + "Put the PanelRef in a Slot, or change the slot of the panel.",
            errors[1].Message);
        Assert.Empty(result.Panels);
        Assert.Empty(result.Layouts);
        Assert.Empty(result.Modes);
    }

    /// <summary><c>Window</c>, a misplaced <c>Slot</c>, a <c>PanelRef</c> under <c>Panel</c>, an element in another
    /// namespace, and a root that is not <c>Definitions</c> each give <c>UnknownElement</c>.</summary>
    [Fact]
    public void AWindowOrAnotherUnknownElementIsAnUnknownElementError()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui" xmlns:p="urn:other">
            <ui:Window name="de.main" /><ui:Slot name="top" /><p:Thing />
            </ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> errors = LoadErrors(CreateLoader(), Text);

        Assert.Equal(3, errors.Count);
        Assert.Equal(DefinitionErrorKind.UnknownElement, errors[0].Kind);
        AssertAt(errors[0], Text, "ui:Window");
        Assert.Equal(
            "'Window' is not a definition. WindowDefinition comes in a later version. "
                + "The definitions are Panel, Layout, and Mode.",
            errors[0].Message);
        Assert.Equal(DefinitionErrorKind.UnknownElement, errors[1].Kind);
        Assert.Equal("'Slot' is not a definition. The definitions are Panel, Layout, and Mode.", errors[1].Message);
        Assert.Equal(DefinitionErrorKind.UnknownElement, errors[2].Kind);
        Assert.Equal("'p:Thing' is not in the namespace 'brutal-ui'. Add xmlns:ui=\"brutal-ui\" and write <ui:Thing>.", errors[2].Message);

        const string ChildText = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Panel name="de.hud" slot="top" template="de.panel"><ui:PanelRef name="de.hud" /></ui:Panel></ui:Definitions>
            """;
        DefinitionError child = Assert.Single(LoadErrors(CreateLoader(), ChildText));
        Assert.Equal(DefinitionErrorKind.UnknownElement, child.Kind);
        Assert.Equal("'PanelRef' cannot be a child of Panel. Remove it.", child.Message);

        const string RootText = """
            <ui:Panel xmlns:ui="brutal-ui" name="de.hud" slot="top" template="de.panel" />
            """;
        DefinitionError root = Assert.Single(LoadErrors(CreateLoader(), RootText));
        Assert.Equal(DefinitionErrorKind.UnknownElement, root.Kind);
        AssertAt(root, RootText, "ui:Panel");
        Assert.Equal("'Panel' is not the root of a definition text. Write <ui:Definitions xmlns:ui=\"brutal-ui\"> as the root.", root.Message);
    }

    /// <summary>A <c>colour</c> attribute and a prefixed attribute give <c>UnknownAttribute</c>; a <c>Panel</c>
    /// with no <c>template</c> gives <c>MissingAttribute</c> at the element; and <c>slot=""</c> gives
    /// <c>InvalidValue</c>.</summary>
    [Fact]
    public void AnAttributeFaultGivesItsKind()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui" xmlns:p="urn:other">
            <ui:Panel name="de.hud" slot="top" colour="red" p:name="x" template="de.panel" />
            <ui:Panel name="de.a" slot="top" /><ui:Panel name="de.b" slot="" template="de.panel" />
            </ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> errors = LoadErrors(CreateLoader(), Text);

        Assert.Equal(4, errors.Count);
        Assert.Equal(DefinitionErrorKind.UnknownAttribute, errors[0].Kind);
        AssertAt(errors[0], Text, "colour=\"red\"");
        Assert.Equal("'colour' is not an attribute of Panel. The attributes of Panel are name, slot, template.", errors[0].Message);
        Assert.Equal(DefinitionErrorKind.UnknownAttribute, errors[1].Kind);
        AssertAt(errors[1], Text, "p:name=\"x\"");
        Assert.Equal("'p:name' is not an attribute of Panel. Write the attribute with no prefix.", errors[1].Message);
        Assert.Equal(DefinitionErrorKind.MissingAttribute, errors[2].Kind);
        AssertAt(errors[2], Text, "ui:Panel name=\"de.a\"");
        Assert.Equal("Panel needs the attribute 'template'.", errors[2].Message);
        Assert.Equal(DefinitionErrorKind.InvalidValue, errors[3].Kind);
        AssertAt(errors[3], Text, "slot=\"\"");
        Assert.Equal("'slot' needs a value. Write the name of a slot, for example 'top'.", errors[3].Message);
    }

    /// <summary>Text inside <c>Mode</c> gives <c>UnexpectedText</c>; a missing end tag gives one
    /// <c>NotWellFormed</c>; an empty text gives <c>NotWellFormed</c> at 1,1.</summary>
    [Fact]
    public void TextAndBadXmlGiveTheirKinds()
    {
        const string TextText = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Layout name="de.game" template="de.layout" /><ui:Mode name="de.play" layout="de.game">Hello</ui:Mode></ui:Definitions>
            """;
        DefinitionError text = Assert.Single(LoadErrors(CreateLoader(), TextText));
        Assert.Equal(DefinitionErrorKind.UnexpectedText, text.Kind);
        AssertAt(text, TextText, "Hello");
        Assert.Equal("Text is not valid in a definition text. Remove the text.", text.Message);

        const string BrokenText = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Panel name="de.hud" slot="top" template="de.panel" />
            """;
        DefinitionError broken = Assert.Single(LoadErrors(CreateLoader(), BrokenText));
        Assert.Equal(DefinitionErrorKind.NotWellFormed, broken.Kind);

        DefinitionError empty = Assert.Single(LoadErrors(CreateLoader(), string.Empty));
        Assert.Equal(DefinitionErrorKind.NotWellFormed, empty.Kind);
        Assert.Equal(1, empty.Line);
        Assert.Equal(1, empty.Column);
    }

    /// <summary>One fault gives one error and no record (the follow-on rule): a <c>Panel</c> with no
    /// <c>template</c> and a <c>Mode</c> that names it give one error, and a <c>Layout</c> with an unknown template
    /// and a <c>Mode</c> with a <c>Slot</c> on it give one error. The errors of both passes sort by line, then
    /// column.</summary>
    [Fact]
    public void OneFaultGivesOneErrorAndNoRecord()
    {
        const string PanelText = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" />
            <ui:Layout name="de.game" template="de.layout" />
            <ui:Mode name="de.play" layout="de.game"><ui:Slot name="top"><ui:PanelRef name="de.hud" /></ui:Slot></ui:Mode>
            </ui:Definitions>
            """;
        DefinitionLoadResult panelResult = Load(CreateLoader(), PanelText);
        DefinitionError missing = Assert.Single(panelResult.Errors);
        Assert.Equal(DefinitionErrorKind.MissingAttribute, missing.Kind);
        Assert.Empty(panelResult.Panels);
        Assert.Empty(panelResult.Layouts);
        Assert.Empty(panelResult.Modes);

        const string LayoutText = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" template="de.panel" />
            <ui:Layout name="de.game" template="no.such" />
            <ui:Mode name="de.play" layout="de.game"><ui:Slot name="top"><ui:PanelRef name="de.hud" /></ui:Slot></ui:Mode>
            </ui:Definitions>
            """;
        DefinitionLoadResult layoutResult = Load(CreateLoader(), LayoutText);
        DefinitionError unknown = Assert.Single(layoutResult.Errors);
        Assert.Equal(DefinitionErrorKind.UnknownTemplate, unknown.Kind);
        Assert.Empty(layoutResult.Modes);

        const string SortText = """
            <ui:Definitions xmlns:ui="brutal-ui">
            <ui:Panel name="de.hud" slot="top" template="no.such" />
            <ui:Panel name="de.other" colour="red" slot="top" template="de.panel" />
            </ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> errors = LoadErrors(CreateLoader(), SortText);
        Assert.Equal(DefinitionErrorKind.UnknownTemplate, errors[0].Kind);
        Assert.Equal(DefinitionErrorKind.UnknownAttribute, errors[1].Kind);
        Assert.Equal(
            $"defs.xml({errors[0].Line},{errors[0].Column}): UnknownTemplate: "
                + "'no.such' is not a registered template. Add it with DefinitionLoader.AddTemplate before the load.",
            errors[0].ToString());
    }

    /// <summary>Creates a loader with the panel template and the layout template on a new template loader.</summary>
    private static DefinitionLoader CreateLoader()
    {
        DefinitionLoader loader = new(new TemplateLoader());
        loader.AddTemplate("de.panel", PanelTemplate);
        loader.AddTemplate("de.layout", LayoutTemplate);
        return loader;
    }

    /// <summary>Creates a loader with one extra template on a new template loader.</summary>
    private static DefinitionLoader LoaderWith(string name, string text)
    {
        DefinitionLoader loader = CreateLoader();
        loader.AddTemplate(name, Encoding.UTF8.GetBytes(text));
        return loader;
    }

    /// <summary>Loads one definitions text (with the format argument, when given) and gives its error list.</summary>
    private static IReadOnlyList<DefinitionError> LoadErrors(DefinitionLoader loader, string text, string? templateName = null)
    {
        return Load(loader, templateName is null ? text : string.Format(text, templateName)).Errors;
    }

    /// <summary>Loads one definitions text with the source name <c>defs.xml</c>.</summary>
    private static DefinitionLoadResult Load(DefinitionLoader loader, string text)
    {
        return loader.Load(Encoding.UTF8.GetBytes(text), "defs.xml");
    }

    /// <summary>Checks the line and the column of one error against the nth occurrence (from 1) of the fault text
    /// in the source: the element name start for an element fault, the attribute name start for a value fault.</summary>
    private static void AssertAt(DefinitionError error, string text, string at, int occurrence = 1)
    {
        int index = -1;
        for (int n = 0; n < occurrence; n++)
        {
            index = text.IndexOf(at, index + 1, StringComparison.Ordinal);
            Assert.True(index >= 0, $"The text holds no occurrence {occurrence} of '{at}'.");
        }

        int line = 1;
        int lineStart = 0;
        for (int i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        Assert.Equal(line, error.Line);
        Assert.Equal(index - lineStart + 1, error.Column);
    }
}
