using System;
using Xunit;

namespace BrutalUi.Markup.Tests.Generated;

/// <summary>
/// Pins the <c>ArgumentNullException</c> guard of each named attribute constructor:
/// <see cref="TemplateElementAttribute"/>, <see cref="TemplateCreateRowAttribute"/>, and
/// <see cref="TemplateRowNameAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// The constructors call <see cref="ArgumentNullException.ThrowIfNull(string)"/> on the name. A later refactor
/// that drops the guard breaks this test, not the whole suite.
/// </para>
/// </remarks>
public sealed class TemplateAttributeTests
{
    /// <summary>Each constructor with a name throws for a <see langword="null"/> name.</summary>
    [Fact]
    public void ANullNameThrowsInEachNamedConstructor()
    {
        Assert.Throws<ArgumentNullException>(() => new TemplateElementAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateCreateRowAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRowNameAttribute(null!));
    }
}
