using System;
using System.Globalization;
using BrutalUi.Core;
using BrutalUi.Markup;

// The package test: a game outside the solution references the packed BrutalUi.Markup and gets
// the generated rows with no extra line. The analyzer in analyzers/dotnet/cs runs the generator
// on the Gauge below. A PASS prints the line and exits 0; any other result prints a FAIL to
// standard error and exits 1.

TemplateLoader loader = new();
loader.RegisterBrutalUiPackageTestElements();

TemplateLoadResult result = loader.Load(
    """<ui:Gauge xmlns:ui="brutal-ui" level="0.5" />"""u8, "gauge.xml");

if (result.Errors.Count > 0)
{
    Console.Error.WriteLine($"package-test: FAIL {result.Errors[0].Kind} at line {result.Errors[0].Line}: {result.Errors[0].Message}");
    return 1;
}

if (result.Root is not Gauge gauge)
{
    Console.Error.WriteLine($"package-test: FAIL expected a Gauge root, got {result.Root?.GetType().Name ?? "null"}");
    return 1;
}

if (gauge.Level != 0.5f)
{
    Console.Error.WriteLine($"package-test: FAIL expected level 0.5, got {gauge.Level.ToString(CultureInfo.InvariantCulture)}");
    return 1;
}

Console.WriteLine("package-test: PASS Gauge level 0.5");
return 0;

/// <summary>A marked element type: the generator in the package writes its rows and the register call.</summary>
[TemplateElement("Gauge")]
internal sealed class Gauge : Element
{
    /// <summary>Gets or sets the level.</summary>
    public float Level { get; set; }
}
