using System.Reflection;
using Xunit;

namespace BrutalUi.Framework.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void ProjectReferenceResolves()
    {
        Assembly assembly = Assembly.Load(new AssemblyName("BrutalUi.Framework"));

        Assert.NotNull(assembly);
    }
}
