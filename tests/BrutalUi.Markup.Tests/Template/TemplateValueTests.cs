using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The value-form tests: each fault gives one <see cref="TemplateError"/> with the kind, the line, and the column of
/// the attribute name, and no root.
/// </summary>
/// <remarks>
/// <para>
/// Each test uses the public API of <see cref="TemplateLoader"/> only. Each message check uses <c>Contains</c> on the
/// quoted text, because the plan fixes the words of some messages only.
/// </para>
/// <para>
/// The line and the column come from the template text of the test: an attribute error sits at the first character of
/// the attribute name, and an element error at the first character of the qualified name after the <c>&lt;</c>.
/// </para>
/// </remarks>
public sealed class TemplateValueTests
{
    private static readonly TextureHandle Icon = new(7, new int2(16, 16));

    /// <summary>A bool is <c>true</c> or <c>false</c>, case-sensitive. Each other word is a fault.</summary>
    [Fact]
    public void ABooleanIsTrueOrFalseOnly()
    {
        TemplateLoader loader = new();

        Element on = loader.Load(Bytes("""<ui:Element xmlns:ui="brutal-ui" pickable="true" focusable="true" />"""), "values.xml")
            .GetRootOrThrow();
        Assert.True(on.IsPickable);
        Assert.True(on.IsFocusable);

        Element off = loader.Load(Bytes("""<ui:Element xmlns:ui="brutal-ui" pickable="false" />"""), "values.xml")
            .GetRootOrThrow();
        Assert.False(off.IsPickable);

        foreach (string bad in new[] { "True", "1", "yes" })
        {
            string template = $"""<ui:Element xmlns:ui="brutal-ui" pickable="{bad}" />""";
            TemplateError error = Fault(loader, template);
            AssertAttribute(error, template, "pickable");
            Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
            Assert.Contains($"'{bad}'", error.Message);
        }
    }

    /// <summary>A number reads in the invariant culture, also under a culture with a comma point.</summary>
    /// <remarks>
    /// The test reads <c>scroll-speed</c>, because that row writes one number with no range check. The read is the
    /// only thing under test here.
    /// </remarks>
    [Fact]
    public void ANumberReadsInTheInvariantCulture()
    {
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal("0,5", 0.5f.ToString(CultureInfo.CurrentCulture));

            TemplateLoader loader = new();
            string good = """<ui:ScrollView xmlns:ui="brutal-ui" scroll-speed="0.5" />""";
            ScrollView view = (ScrollView)loader.Load(Bytes(good), "values.xml").GetRootOrThrow();
            Assert.Equal(0.5f, view.ScrollSpeed);

            foreach (string bad in new[] { "0,5", "1e3", "NaN", "12345678901234567890123456789012345678901234567890" })
            {
                string template = $"""<ui:ScrollView xmlns:ui="brutal-ui" scroll-speed="{bad}" />""";
                TemplateError error = Fault(loader, template);
                AssertAttribute(error, template, "scroll-speed");
                Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
                Assert.Contains($"'{bad}'", error.Message);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    /// <summary>A list needs the count of numbers of its row. Whitespace at both ends is valid.</summary>
    [Fact]
    public void AListNeedsTheRightCount()
    {
        TemplateLoader loader = new();

        string good = """<ui:Image xmlns:ui="brutal-ui" uv="  0 0 1 1  " />""";
        Image image = (Image)loader.Load(Bytes(good), "values.xml").GetRootOrThrow();
        Assert.Equal(new float4(0f, 0f, 1f, 1f), image.Uv);

        string shortUv = """<ui:Image xmlns:ui="brutal-ui" uv="0 0 0.5" />""";
        TemplateError uvFault = Fault(loader, shortUv);
        AssertAttribute(uvFault, shortUv, "uv");
        Assert.Equal(TemplateErrorKind.InvalidValue, uvFault.Kind);
        Assert.Contains("'0 0 0.5'", uvFault.Message);

        string longOffset = """<ui:ScrollView xmlns:ui="brutal-ui" scroll-offset="0 30 60" />""";
        TemplateError offsetFault = Fault(loader, longOffset);
        AssertAttribute(offsetFault, longOffset, "scroll-offset");
        Assert.Equal(TemplateErrorKind.InvalidValue, offsetFault.Kind);
        Assert.Contains("'0 30 60'", offsetFault.Message);
    }

    /// <summary>A color takes 3, 6, or 8 hex digits after the <c>#</c>.</summary>
    [Fact]
    public void AColorTakesThreeSixOrEightHexDigits()
    {
        TemplateLoader loader = new();

        Assert.Equal(new Rgba8(0xff, 0xff, 0xff, 0xff), TintOf(loader, "#fff"));
        Assert.Equal(new Rgba8(0x1e, 0x22, 0x2c, 0xff), TintOf(loader, "#1E222C"));
        Assert.Equal(new Rgba8(0x1e, 0x22, 0x2c, 0x80), TintOf(loader, "#1e222c80"));

        foreach (string bad in new[] { "#ffff", "fff", "#ggg" })
        {
            string template = $"""<ui:Image xmlns:ui="brutal-ui" tint="{bad}" />""";
            TemplateError error = Fault(loader, template);
            AssertAttribute(error, template, "tint");
            Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
            Assert.Contains($"'{bad}'", error.Message);
        }
    }

    /// <summary>The three scale-mode keywords read. Each other word is a fault.</summary>
    [Fact]
    public void AScaleModeTakesTheThreeKeywords()
    {
        TemplateLoader loader = new();

        Assert.Equal(ImageScaleMode.StretchToFill, ModeOf(loader, "stretch-to-fill"));
        Assert.Equal(ImageScaleMode.ScaleToFit, ModeOf(loader, "scale-to-fit"));
        Assert.Equal(ImageScaleMode.ScaleAndCrop, ModeOf(loader, "scale-and-crop"));

        foreach (string bad in new[] { "stretch", "ScaleToFit", "" })
        {
            string template = $"""<ui:Image xmlns:ui="brutal-ui" scale-mode="{bad}" />""";
            TemplateError error = Fault(loader, template);
            AssertAttribute(error, template, "scale-mode");
            Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        }
    }

    /// <summary>A capacity is a whole number above 0 and not above the limit 65536. A sign, a point, a 0, a word, and
    /// an over-limit number each fault.</summary>
    [Fact]
    public void ACapacityIsAWholeNumberAboveZero()
    {
        TemplateLoader loader = new();

        foreach (string bad in new[] { "0", "-1", "1.5", "x" })
        {
            string template = $"""<ui:TextField xmlns:ui="brutal-ui" capacity="{bad}" />""";
            TemplateError error = Fault(loader, template);
            AssertAttribute(error, template, "capacity");
            Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
            Assert.Contains($"'{bad}'", error.Message);
        }

        // The factory checks the limit before the constructor, so no OutOfMemoryException leaves the load.
        foreach (string over in new[] { "65537", "100000000", "2147483647" })
        {
            string template = $"""<ui:TextField xmlns:ui="brutal-ui" capacity="{over}" />""";
            TemplateError error = Fault(loader, template);
            AssertAttribute(error, template, "capacity");
            Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
            Assert.Contains($"'{over}'", error.Message);
        }

        string atLimit = """<ui:TextField xmlns:ui="brutal-ui" capacity="65536" />""";
        Assert.True(loader.Load(Bytes(atLimit), "values.xml").Succeeded);
    }

    /// <summary>The range checks report a fault, and no exception leaves <see cref="TemplateLoader.Load(ReadOnlySpan{byte}, string)"/>.</summary>
    [Fact]
    public void ASliderRangeIsChecked()
    {
        TemplateLoader loader = new();

        string below = """<ui:Slider xmlns:ui="brutal-ui" min="5" max="1" />""";
        TemplateError maxFault = Fault(loader, below);
        AssertAttribute(maxFault, below, "max");
        Assert.Equal(TemplateErrorKind.InvalidValue, maxFault.Kind);
        Assert.Contains("'1'", maxFault.Message);

        string negativeStep = """<ui:Slider xmlns:ui="brutal-ui" max="10" step="-1" />""";
        TemplateError stepFault = Fault(loader, negativeStep);
        AssertAttribute(stepFault, negativeStep, "step");
        Assert.Equal(TemplateErrorKind.InvalidValue, stepFault.Kind);
        Assert.Contains("'-1'", stepFault.Message);
    }

    /// <summary>A scroll speed of 0 or more reads. A negative speed is a fault.</summary>
    [Fact]
    public void AScrollSpeedIsZeroOrMore()
    {
        TemplateLoader loader = new();

        string zero = """<ui:ScrollView xmlns:ui="brutal-ui" scroll-speed="0" />""";
        Assert.True(loader.Load(Bytes(zero), "values.xml").Succeeded);

        string negative = """<ui:ScrollView xmlns:ui="brutal-ui" scroll-speed="-1" />""";
        TemplateError error = Fault(loader, negative);
        AssertAttribute(error, negative, "scroll-speed");
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        Assert.Contains("'-1'", error.Message);
    }

    /// <summary>A texture name that the map does not hold gives <see cref="TemplateErrorKind.UnknownTexture"/>.</summary>
    [Fact]
    public void AnUnknownTextureNameIsAnError()
    {
        Dictionary<string, TextureHandle> map = new(StringComparer.Ordinal) { ["icon"] = Icon };
        TemplateLoader loader = new(new TemplateLoader.CreateInfo { Textures = map });

        string missing = """<ui:Image xmlns:ui="brutal-ui" texture="icon2" />""";
        TemplateError error = Fault(loader, missing);
        AssertAttribute(error, missing, "texture");
        Assert.Equal(TemplateErrorKind.UnknownTexture, error.Kind);
        Assert.Contains("'icon2'", error.Message);

        // No map at all: no name is valid.
        string any = """<ui:Image xmlns:ui="brutal-ui" texture="icon" />""";
        TemplateError noMap = Fault(new TemplateLoader(), any);
        AssertAttribute(noMap, any, "texture");
        Assert.Equal(TemplateErrorKind.UnknownTexture, noMap.Kind);
        Assert.Contains("'icon'", noMap.Message);
    }

    /// <summary>A missing required create attribute gives <see cref="TemplateErrorKind.MissingAttribute"/> at the element name.</summary>
    [Fact]
    public void AMissingRequiredAttributeIsAnError()
    {
        TemplateLoader loader = new();

        string field = """<ui:TextField xmlns:ui="brutal-ui" text="abc" />""";
        TemplateError fieldFault = Fault(loader, field);
        AssertElement(fieldFault, field, "ui:TextField");
        Assert.Equal(TemplateErrorKind.MissingAttribute, fieldFault.Kind);
        Assert.Contains("'capacity'", fieldFault.Message);

        string slider = """<ui:Slider xmlns:ui="brutal-ui" min="0" step="1" />""";
        TemplateError sliderFault = Fault(loader, slider);
        AssertElement(sliderFault, slider, "ui:Slider");
        Assert.Equal(TemplateErrorKind.MissingAttribute, sliderFault.Kind);
        Assert.Contains("'max'", sliderFault.Message);
    }

    /// <summary>Loads one bad template and gives its one error. The root is <see langword="null"/>.</summary>
    private static TemplateError Fault(TemplateLoader loader, string template)
    {
        TemplateLoadResult result = loader.Load(Bytes(template), "values.xml");
        Assert.Null(result.Root);
        return Assert.Single(result.Errors);
    }

    /// <summary>Checks the line and the column of an attribute fault against the template text.</summary>
    private static void AssertAttribute(TemplateError error, string template, string attributeName)
    {
        int at = template.IndexOf($"{attributeName}=\"", StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {attributeName} attribute.");
        AssertPosition(error, template, at);
    }

    /// <summary>Checks the line and the column of an element fault against the template text.</summary>
    /// <remarks>The element position is the first character of the qualified name, after the <c>&lt;</c>.</remarks>
    private static void AssertElement(TemplateError error, string template, string qualifiedName)
    {
        int at = template.IndexOf(qualifiedName.AsSpan(), StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {qualifiedName} element.");
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

    /// <summary>Gives the tint of one image template.</summary>
    private static Rgba8 TintOf(TemplateLoader loader, string tint)
    {
        string template = $"""<ui:Image xmlns:ui="brutal-ui" tint="{tint}" />""";
        Image image = (Image)loader.Load(Bytes(template), "values.xml").GetRootOrThrow();
        return image.Tint;
    }

    /// <summary>Gives the scale mode of one image template.</summary>
    private static ImageScaleMode ModeOf(TemplateLoader loader, string mode)
    {
        string template = $"""<ui:Image xmlns:ui="brutal-ui" scale-mode="{mode}" />""";
        Image image = (Image)loader.Load(Bytes(template), "values.xml").GetRootOrThrow();
        return image.ScaleMode;
    }

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Bytes(string template) => Encoding.UTF8.GetBytes(template);
}
