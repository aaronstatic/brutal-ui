using System;
using System.Text;
using Brutal.Localization;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The <c>text-key</c> tests: the loader joins <c>text</c> and <c>text-key</c> into one <see cref="LString"/> by the
/// rule of A4, and a loaded localized text follows a refresh.
/// </summary>
/// <remarks>
/// <para>
/// Each test uses the public API of <see cref="TemplateLoader"/> only. Each template text is a <c>u8</c> literal,
/// because the BRUTAL analyzer rejects a string literal in <see cref="TemplateLoader.Load(string, string)"/>.
/// </para>
/// <para>
/// No test uses the static <c>LStrings</c> class. Each test makes its <see cref="LString"/> objects through the
/// loader or the builder, so parallel tests share no state.
/// </para>
/// </remarks>
public sealed class LocalizedTextTests
{
    /// <summary>Gets a template with a <see cref="Label"/> that holds <c>text-key</c> and <c>text</c>.</summary>
    private static ReadOnlySpan<byte> LabelTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Label text-key="localized-text-tests.score" text="Score" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <see cref="Label"/> that holds <c>text-key</c> and no <c>text</c>.</summary>
    private static ReadOnlySpan<byte> EmptyDefaultTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Label text-key="localized-text-tests.empty" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a <see cref="Button"/> and a <see cref="TextField"/> that hold <c>text-key</c>.</summary>
    private static ReadOnlySpan<byte> ButtonFieldTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Button text-key="localized-text-tests.pause" text="Pause" />
          <ui:TextField capacity="3" text-key="localized-text-tests.cut" text="Score" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with a label and a button that hold <c>text-key</c> and <c>text</c>.</summary>
    private static ReadOnlySpan<byte> TreeTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Label name="score" text-key="localized-text-tests.tree-label" text="Score" />
          <ui:Button name="pause" text-key="localized-text-tests.tree-button" text="Pause" />
        </ui:Element>
        """u8;

    /// <summary>Gets a template with one localized <see cref="Label"/> for the refresh test.</summary>
    private static ReadOnlySpan<byte> RefreshTemplate => """
        <ui:Element xmlns:ui="brutal-ui">
          <ui:Label text-key="localized-text-tests.refresh" text="Score" />
        </ui:Element>
        """u8;

    /// <summary><c>text-key</c> with <c>text</c> gives an <see cref="LString"/> with the key and the default text.</summary>
    [Fact]
    public void TextKeyWithTextGivesALocalizedTextWithTheDefault()
    {
        Element root = new TemplateLoader().Load(LabelTemplate, "localized.xml").GetRootOrThrow();
        Label label = Assert.IsType<Label>(root.Children[0]);

        Assert.NotNull(label.LocalizedText);
        Assert.Equal("localized-text-tests.score", label.LocalizedText.Id.ToString());
        Assert.Equal("Score", label.LocalizedText.Template);
        Assert.True(label.Text.SequenceEqual("Score"u8));
    }

    /// <summary><c>text-key</c> with no <c>text</c> gives the empty template and an empty text.</summary>
    [Fact]
    public void TextKeyWithNoTextGivesAnEmptyDefault()
    {
        Element root = new TemplateLoader().Load(EmptyDefaultTemplate, "localized.xml").GetRootOrThrow();
        Label label = Assert.IsType<Label>(root.Children[0]);

        Assert.NotNull(label.LocalizedText);
        Assert.Equal("localized-text-tests.empty", label.LocalizedText.Id.ToString());
        Assert.Equal(string.Empty, label.LocalizedText.Template);
        Assert.True(label.Text.IsEmpty);
    }

    /// <summary>
    /// <c>text-key</c> works on a <see cref="Button"/> and a <see cref="TextField"/>. The field shows the template
    /// text, cut at the capacity.
    /// </summary>
    [Fact]
    public void TextKeyWorksOnAButtonAndATextField()
    {
        Element root = new TemplateLoader().Load(ButtonFieldTemplate, "localized.xml").GetRootOrThrow();
        Button button = Assert.IsType<Button>(root.Children[0]);
        TextField field = Assert.IsType<TextField>(root.Children[1]);

        Assert.NotNull(button.LocalizedText);
        Assert.Equal("Pause", button.LocalizedText.Template);
        Assert.True(button.Text.SequenceEqual("Pause"u8));

        Assert.Equal(3, field.Capacity);
        Assert.True(field.Text.SequenceEqual("Sco"u8));
    }

    /// <summary>An empty <c>text-key</c> gives one <see cref="TemplateErrorKind.InvalidValue"/> error at the attribute.</summary>
    [Fact]
    public void AnEmptyTextKeyIsAnError()
    {
        string template = """<ui:Element xmlns:ui="brutal-ui"><ui:Label name="a" text-key="" /></ui:Element>""";
        TemplateLoadResult result = new TemplateLoader().Load(Encoding.UTF8.GetBytes(template), "localized.xml");

        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        Assert.Contains("''", error.Message);
        AssertPosition(error, template, template.IndexOf("text-key=", StringComparison.Ordinal));
    }

    /// <summary>A loaded label and button with <c>text-key</c> equal the builder tree with <c>Text(LString)</c>.</summary>
    [Fact]
    public void ALocalizedTemplateEqualsTheBuilderTree()
    {
        Element loaded = new TemplateLoader().Load(TreeTemplate, "localized.xml").GetRootOrThrow();
        Element expected = new Element()
            .Child(new Label(default(Label.CreateInfo)).Name("score").Text(new LString("localized-text-tests.tree-label", "Score")))
            .Child(new Button(default(Button.CreateInfo)).Name("pause").Text(new LString("localized-text-tests.tree-button", "Pause")));

        Assert.Null(TreeCompare.FirstDifference(expected, loaded));
    }

    /// <summary>
    /// A loaded localized text follows <see cref="UiContext.RefreshLocalizedText"/>: after <c>Localize</c> and a
    /// refresh, the label shows the new text.
    /// </summary>
    [Fact]
    public void ALoadedLocalizedTextFollowsARefresh()
    {
        RecordingRenderer renderer = new();
        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer }) { ViewportSize = new float2(400f, 300f) };
        Element root = new TemplateLoader().Load(RefreshTemplate, "localized.xml").GetRootOrThrow();
        Label label = Assert.IsType<Label>(root.Children[0]);
        context.AddRoot(root);
        context.Update(0f);

        label.LocalizedText!.Localize("x");
        context.RefreshLocalizedText();

        Assert.True(label.Text.SequenceEqual("x"u8));
    }

    /// <summary>Checks one position against the index of the fault text in the template.</summary>
    private static void AssertPosition(TemplateError error, string template, int index)
    {
        int line = 1;
        int lineStart = 0;
        for (int i = 0; i < index; i++)
        {
            if (template[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        Assert.Equal(line, error.Line);
        Assert.Equal(index - lineStart + 1, error.Column);
    }
}
