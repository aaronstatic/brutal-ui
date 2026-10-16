using System.Reflection;
using Xunit;

namespace BrutalUi.Core.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void ProjectReferenceResolves()
    {
        Assembly assembly = Assembly.Load(new AssemblyName("BrutalUi.Core"));

        Assert.NotNull(assembly);
    }
}
