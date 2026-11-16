using System;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The attribute-hook tests: the hook runs after the rows of the element, the handlers run in the order of the
/// add, and a handler reports a fault with the position of its attribute.
/// </summary>
/// <remarks>
/// <para>
/// Each test uses the public API of <see cref="TemplateLoader"/> only. The line and the column of an error come
/// from the template text of the test: an attribute error sits at the first character of the attribute name.
/// </para>
/// </remarks>
public sealed class AttributeHookTests
{
    /// <summary>The hook runs after the set rows, so a handler reads the name, the classes, and the value that the rows set.</summary>
    [Fact]
    public void TheHookRunsAfterTheRowsOfTheElement()
    {
        string? seenName = null;
        int seenClasses = 0;
        float seenValue = 0f;

        TemplateLoader loader = new();
        loader.AddAttributeHandler(
            (Element element, string name, string value, AttributeContext context) =>
            {
                _ = value;
                _ = context;
                if (name != "probe")
                {
                    return false;
                }

                Slider slider = (Slider)element;
                seenName = slider.Name;
                seenClasses = slider.Classes.Count;
                seenValue = slider.Value;
                return true;
            });

        Element root = loader
            .Load("""<ui:Slider xmlns:ui="brutal-ui" name="s" class="hud" max="10" value="7" probe="x" />"""u8, "hook.xml")
            .GetRootOrThrow();

        Assert.IsType<Slider>(root);
        Assert.Equal("s", seenName);
        Assert.Equal(1, seenClasses);
        Assert.Equal(7f, seenValue);
    }

    /// <summary>The handlers run in the order of the add, and the first <see langword="true"/> takes the attribute.</summary>
    [Fact]
    public void TheHandlersRunInTheOrderOfTheAdd()
    {
        int[] calls = new int[3];
        TemplateLoader loader = new();
        loader.AddAttributeHandler(
            (Element element, string name, string value, AttributeContext context) =>
            {
                _ = element;
                _ = name;
                _ = value;
                _ = context;
                calls[0]++;
                return false;
            });
        loader.AddAttributeHandler(
            (Element element, string name, string value, AttributeContext context) =>
            {
                _ = element;
                _ = name;
                _ = value;
                _ = context;
                calls[1]++;
                return true;
            });
        loader.AddAttributeHandler(
            (Element element, string name, string value, AttributeContext context) =>
            {
                _ = element;
                _ = name;
                _ = value;
                _ = context;
                calls[2]++;
                return true;
            });

        loader.Load("""<ui:Element xmlns:ui="brutal-ui" probe="1" />"""u8, "hook.xml").GetRootOrThrow();

        Assert.Equal(1, calls[0]);
        Assert.Equal(1, calls[1]);
        Assert.Equal(0, calls[2]);
    }

    /// <summary>A handler error sits at the position of its attribute, and one error gives no root.</summary>
    [Fact]
    public void AHandlerErrorHasThePositionOfTheAttribute()
    {
        TemplateLoader loader = new();
        loader.AddAttributeHandler(
            (Element element, string name, string value, AttributeContext context) =>
            {
                _ = element;
                _ = value;
                if (name != "oops")
                {
                    return false;
                }

                context.AddError(TemplateErrorKind.InvalidValue, "'oops' needs a bound source. Remove the attribute.");
                return true;
            });

        string template = """<ui:Element xmlns:ui="brutal-ui" oops="1" />""";
        TemplateLoadResult result = loader.Load(Bytes(template), "hook.xml");

        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        AssertAttribute(error, template, "oops");
    }

    /// <summary>An attribute with a row never reaches a handler.</summary>
    [Fact]
    public void TheHookNeverSeesAnAttributeWithARow()
    {
        int calls = 0;
        TemplateLoader loader = new();
        loader.AddAttributeHandler(
            (Element element, string name, string value, AttributeContext context) =>
            {
                _ = element;
                _ = name;
                _ = value;
                _ = context;
                calls++;
                return false;
            });

        TemplateLoadResult result = loader
            .Load("""<ui:Label xmlns:ui="brutal-ui" name="l" text="hi" />"""u8, "hook.xml");

        Assert.True(result.Succeeded);
        Assert.Equal(0, calls);
    }

    /// <summary>A <see langword="null"/> handler gives <see cref="ArgumentNullException"/> with the name of the parameter.</summary>
    [Fact]
    public void AddAttributeHandlerChecksNull()
    {
        TemplateLoader loader = new();
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => loader.AddAttributeHandler(null!));
        Assert.Equal("handler", error.ParamName);
    }

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

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Bytes(string template) => Encoding.UTF8.GetBytes(template);
}
