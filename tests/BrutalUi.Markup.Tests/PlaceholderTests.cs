using System.Reflection;
using Xunit;

namespace BrutalUi.Markup.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void ProjectReferenceResolves()
    {
        Assembly assembly = Assembly.Load(new AssemblyName("BrutalUi.Markup"));

        Assert.NotNull(assembly);
    }
}
