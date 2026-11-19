using System;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The check load of the layouts (Q3, A5, Decision 15): the resolve pass check-loads each layout that is not faulty,
/// also one that no mode names, so a fault of an unused layout lands at load time.
/// </summary>
public sealed partial class DefinitionErrorTests
{
    /// <summary>A layout whose template does not load gives one <c>TemplateFault</c> at the <c>Layout</c>
    /// element, with the <c>TemplateError.ToString</c> lines in the message (A5). A template with two <c>top</c>
    /// slots gives <c>DuplicateName</c> at the element, an empty <c>slot</c> gives <c>TemplateFault</c> with
    /// <c>InvalidValue</c>, and a bad layout that no mode names gives the same <c>TemplateFault</c> at load
    /// time with no record (Decision 15).</summary>
    [Fact]
    public void ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Layout name="de.game" template="{0}" /><ui:Mode name="de.play" layout="de.game" /></ui:Definitions>
            """;
        const string BadTemplate = """
            <ui:Element xmlns:ui="brutal-ui"><ui:Nope /></ui:Element>
            """;
        const string TwiceTemplate = """
            <ui:Element pickable="false" xmlns:ui="brutal-ui"><ui:SlotContainer slot="top" /><ui:SlotContainer slot="top" /></ui:Element>
            """;
        const string EmptySlotTemplate = """
            <ui:Element pickable="false" xmlns:ui="brutal-ui"><ui:SlotContainer slot="" /></ui:Element>
            """;

        DefinitionError fault = Assert.Single(LoadErrors(LoaderWith("de.bad", BadTemplate), Text, "de.bad"));
        Assert.Equal(DefinitionErrorKind.TemplateFault, fault.Kind);
        AssertAt(fault, Text, "ui:Layout");
        Assert.Equal("The template 'de.bad' of the layout 'de.game' does not load.", fault.Message[..fault.Message.IndexOf('\n')]);
        Assert.Contains("de.bad(", fault.Message, StringComparison.Ordinal);
        Assert.Contains("UnknownElement", fault.Message, StringComparison.Ordinal);

        DefinitionError twice = Assert.Single(LoadErrors(LoaderWith("de.twice", TwiceTemplate), Text, "de.twice"));
        Assert.Equal(DefinitionErrorKind.DuplicateName, twice.Kind);
        AssertAt(twice, Text, "ui:Layout");
        Assert.Equal(
            "The template 'de.twice' holds two slots with the name 'top'. Give each SlotContainer one slot name.",
            twice.Message);

        DefinitionError empty = Assert.Single(LoadErrors(LoaderWith("de.empty", EmptySlotTemplate), Text, "de.empty"));
        Assert.Equal(DefinitionErrorKind.TemplateFault, empty.Kind);
        Assert.Contains("InvalidValue", empty.Message, StringComparison.Ordinal);

        const string IdleText = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Layout name="de.idle" template="de.bad" /></ui:Definitions>
            """;
        DefinitionLoadResult idle = Load(LoaderWith("de.bad", BadTemplate), IdleText);
        Assert.False(idle.Succeeded);
        DefinitionError idleFault = Assert.Single(idle.Errors);
        Assert.Equal(DefinitionErrorKind.TemplateFault, idleFault.Kind);
        AssertAt(idleFault, IdleText, "ui:Layout");
        Assert.Equal("The template 'de.bad' of the layout 'de.idle' does not load.", idleFault.Message[..idleFault.Message.IndexOf('\n')]);
        Assert.Empty(idle.Panels);
        Assert.Empty(idle.Layouts);
        Assert.Empty(idle.Modes);
    }
}
