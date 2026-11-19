using System.Collections.Generic;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The follow-on rule in both directions. Silence under a failed element (review group 1, CR-2): the failed-parent
/// check precedes the namespace check, so a child of a failed element gives no error in any namespace. Visibility of
/// the faults inside a faulty entry (review group 2, CR-1): a fault of one value of an entry hides no other fault of
/// the same entry. One fault gives one error, and one load gives each fault.
/// </summary>
public sealed partial class DefinitionErrorTests
{
    /// <summary>A <c>Panel</c> outside the namespace under a failed <c>Window</c> gives one <c>UnknownElement</c>
    /// error, at the <c>Window</c>. The old order reported a second error at the child.</summary>
    [Fact]
    public void AChildOutsideTheNamespaceUnderAFailedElementGivesOneError()
    {
        const string Text = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Window><Panel /></ui:Window></ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> errors = LoadErrors(CreateLoader(), Text);

        DefinitionError error = Assert.Single(errors);
        Assert.Equal(DefinitionErrorKind.UnknownElement, error.Kind);
        AssertAt(error, Text, "ui:Window");
        Assert.Equal(
            "'Window' is not a definition. WindowDefinition comes in a later version. "
                + "The definitions are Panel, Layout, and Mode.",
            error.Message);
    }

    /// <summary>A faulty mode hides no placement fault: a mode with an unknown layout and an unknown panel gives
    /// <c>UnknownLayout</c> at the <c>layout</c> attribute and <c>UnknownPanel</c> at the <c>PanelRef</c> name. A
    /// <c>Panel</c> with no <c>slot</c> and an unknown <c>template</c> gives <c>MissingAttribute</c> and
    /// <c>UnknownTemplate</c> in the same load.</summary>
    [Fact]
    public void AFaultyEntryStillGivesItsOtherFaults()
    {
        const string ModeText = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Mode name="m" layout="gam"><ui:PanelRef name="hdu" /></ui:Mode></ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> modeErrors = LoadErrors(CreateLoader(), ModeText);

        Assert.Equal(2, modeErrors.Count);
        Assert.Equal(DefinitionErrorKind.UnknownLayout, modeErrors[0].Kind);
        AssertAt(modeErrors[0], ModeText, "layout=\"gam\"");
        Assert.Equal(DefinitionErrorKind.UnknownPanel, modeErrors[1].Kind);
        AssertAt(modeErrors[1], ModeText, "name=\"hdu\"");

        const string PanelText = """
            <ui:Definitions xmlns:ui="brutal-ui"><ui:Panel name="de.hud" template="no.such" /></ui:Definitions>
            """;
        IReadOnlyList<DefinitionError> panelErrors = LoadErrors(CreateLoader(), PanelText);

        Assert.Equal(2, panelErrors.Count);
        Assert.Equal(DefinitionErrorKind.MissingAttribute, panelErrors[0].Kind);
        AssertAt(panelErrors[0], PanelText, "ui:Panel");
        Assert.Equal(DefinitionErrorKind.UnknownTemplate, panelErrors[1].Kind);
        AssertAt(panelErrors[1], PanelText, "template=\"no.such\"");
    }
}
