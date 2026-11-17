// The CR-1 pins: a property that hides or overrides a base property of the walked chain. The walk takes the read
// kind, the row name, and the ignore of the visible (most-derived) property, and keeps the place of the base
// property. See docs/features/markup/element-generator/code-review.md, finding CR-1.
using System;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Generated;

/// <summary>
/// The walk rules of a hiding or overriding property: the derived property decides the row, the base property
/// keeps the place.
/// </summary>
/// <remarks>
/// <para>
/// The tests use the same generated API as <see cref="GeneratedElementTests"/>:
/// <c>loader.RegisterBrutalUiMarkupTestsElements()</c>. The XML names <c>Hider</c>, <c>HiderName</c>,
/// <c>HiderIgnore</c>, and <c>HiderOverride</c> are unique in this assembly.
/// </para>
/// <para>
/// Review finding CR-1: the walk read the base property, but the generated cast named the target type, so C#
/// member lookup bound the assignment to the derived property. A <c>new</c> hide of another type wrote a
/// <c>CS0266</c> error into the generated file of the consumer, and <c>[TemplateIgnore]</c> or
/// <c>[TemplateRowName]</c> on the hider was lost in silence.
/// </para>
/// </remarks>
public sealed class GeneratedHidingTests
{
    /// <summary>Pin 1: a <c>new</c> hide of another type gives the derived read kind. The row reads an integer,
    /// so a decimal value is a fault, and the assignment compiles against the derived property.</summary>
    [Fact]
    public void AHidingPropertyOfAnotherTypeTakesTheDerivedKind()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string good = """<ui:Hider xmlns:ui="brutal-ui" level="3" />""";
        Hider hider = Assert.IsType<Hider>(loader.Load(Utf8(good), "hiding.xml").GetRootOrThrow());
        Assert.Equal(3, hider.Level);

        string bad = """<ui:Hider xmlns:ui="brutal-ui" level="1.5" />""";
        TemplateError error = Fault(loader, bad);
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        AssertAttribute(error, bad, "level");
        Assert.Contains("is not an integer", error.Message);
    }

    /// <summary>Pin 2: <c>[TemplateRowName]</c> on the hider names the row. The kebab name of the base property
    /// gives an <see cref="TemplateErrorKind.UnknownAttribute"/>.</summary>
    [Fact]
    public void ARenamedHiderTakesTheRowNameOfTheDerivedProperty()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string good = """<ui:HiderName xmlns:ui="brutal-ui" amt="7" />""";
        HiderName hider = Assert.IsType<HiderName>(loader.Load(Utf8(good), "hiding.xml").GetRootOrThrow());
        Assert.Equal(7, hider.Amount);

        string bad = """<ui:HiderName xmlns:ui="brutal-ui" amount="7" />""";
        TemplateError error = Fault(loader, bad);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
        AssertAttribute(error, bad, "amount");
        Assert.Contains("The attributes of HiderName are name, class, pickable, focusable, amt.", error.Message);
    }

    /// <summary>Pin 3: <c>[TemplateIgnore]</c> on the hider removes the row of the base property, even when both
    /// properties share a type.</summary>
    [Fact]
    public void AnIgnoredHiderRemovesTheBaseRow()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string template = """<ui:HiderIgnore xmlns:ui="brutal-ui" zoom="1" />""";
        TemplateError error = Fault(loader, template);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
        AssertAttribute(error, template, "zoom");
        Assert.Contains("The attributes of HiderIgnore are name, class, pickable, focusable.", error.Message);
    }

    /// <summary>Pin 4: an <c>override</c> keeps the place of the base property and gives one row.</summary>
    [Fact]
    public void AnOverrideKeepsTheBasePlaceAndGivesOneRow()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string template = """<ui:HiderOverride xmlns:ui="brutal-ui" probe="1" />""";
        TemplateError error = Fault(loader, template);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
        Assert.Contains(
            "The attributes of HiderOverride are name, class, pickable, focusable, alpha, word, beta.",
            error.Message);

        string good = """<ui:HiderOverride xmlns:ui="brutal-ui" alpha="1.5" word="hi" beta="2.5" />""";
        HiderOverride element = Assert.IsType<HiderOverride>(loader.Load(Utf8(good), "hiding.xml").GetRootOrThrow());
        Assert.Equal(1.5f, element.Alpha);
        Assert.Equal("hi", element.Word);
        Assert.Equal(2.5f, element.Beta);
    }

    /// <summary>Creates a loader and registers the generated types of this assembly.</summary>
    /// <returns>The loader with the built-in types and the generated types.</returns>
    private static TemplateLoader NewLoaderWithGeneratedTypes()
    {
        TemplateLoader loader = new();
        loader.RegisterBrutalUiMarkupTestsElements();
        return loader;
    }

    /// <summary>Loads one bad template and gives its one error. The root is <see langword="null"/>.</summary>
    private static TemplateError Fault(TemplateLoader loader, string template)
    {
        TemplateLoadResult result = loader.Load(Utf8(template), "hiding.xml");
        Assert.Null(result.Root);
        return Assert.Single(result.Errors);
    }

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Utf8(string template) => Encoding.UTF8.GetBytes(template);

    /// <summary>Checks the line and the column of an attribute fault against the template text.</summary>
    private static void AssertAttribute(TemplateError error, string template, string attributeName)
    {
        int at = template.IndexOf($"{attributeName}=\"", StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {attributeName} attribute.");
        AssertPosition(error, template, at);
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

/// <summary>A base element type with one float row that the marked hider replaces.</summary>
internal class HiddenBase : Element
{
    /// <summary>Gets or sets the level.</summary>
    public float Level { get; set; }
}

/// <summary>A marked type that hides <see cref="HiddenBase.Level"/> with an <c>int</c> property: the row reads
/// an integer, and the generated setter binds to this property.</summary>
[TemplateElement("Hider")]
internal sealed class Hider : HiddenBase
{
    /// <summary>Gets or sets the level. The <c>new</c> hides the base <c>float</c> property.</summary>
    public new int Level { get; set; }
}

/// <summary>A base element type with one float row that the marked hider renames.</summary>
internal class RenamedBase : Element
{
    /// <summary>Gets or sets the amount.</summary>
    public float Amount { get; set; }
}

/// <summary>A marked type that hides <see cref="RenamedBase.Amount"/> with an <c>int</c> property and a row
/// name: the row takes the name of the derived property.</summary>
[TemplateElement("HiderName")]
internal sealed class HiderName : RenamedBase
{
    /// <summary>Gets or sets the amount. The attribute names the row, not the base property.</summary>
    [TemplateRowName("amt")]
    public new int Amount { get; set; }
}

/// <summary>A base element type with one float row that the marked hider ignores.</summary>
internal class IgnoredBase : Element
{
    /// <summary>Gets or sets the zoom.</summary>
    public float Zoom { get; set; }
}

/// <summary>A marked type that hides <see cref="IgnoredBase.Zoom"/> with an ignored property: the visible
/// property gives no row, so the name gives no row.</summary>
[TemplateElement("HiderIgnore")]
internal sealed class HiderIgnore : IgnoredBase
{
    /// <summary>Gets or sets the zoom. The <c>[TemplateIgnore]</c> attribute drops the row.</summary>
    [TemplateIgnore]
    public new float Zoom { get; set; }
}

/// <summary>A base element type with one settable property and one virtual property.</summary>
internal class OverridingBase : Element
{
    /// <summary>Gets or sets the alpha value.</summary>
    public float Alpha { get; set; }

    /// <summary>Gets or sets the word.</summary>
    public virtual string? Word { get; set; }
}

/// <summary>A marked type that overrides <see cref="OverridingBase.Word"/> and adds one property: the override
/// keeps the place of the base property and gives one row.</summary>
[TemplateElement("HiderOverride")]
internal sealed class HiderOverride : OverridingBase
{
    /// <summary>Gets or sets the word. The override keeps the row of the base property.</summary>
    public override string? Word { get; set; }

    /// <summary>Gets or sets the beta value.</summary>
    public float Beta { get; set; }
}
