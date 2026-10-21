using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of <see cref="ElementTypeId"/>, <see cref="ElementType"/>, and <see cref="Element.TypeId"/>.
/// </summary>
/// <remarks>
/// The type table is global and xUnit runs test classes in parallel.
/// Each test uses names with a unique prefix, and no test asserts a fixed id value.
/// </remarks>
public sealed class ElementTypeTests
{
    [Fact]
    public void TheSameNameGivesTheSameId()
    {
        ElementTypeId first = ElementType.Intern("ett-same");
        ElementTypeId second = ElementType.Intern("ett-same");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.False(first.IsNone);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void TheComparisonIsOrdinal()
    {
        Assert.NotEqual(ElementType.Intern("ett-Case"), ElementType.Intern("ett-case"));
    }

    [Fact]
    public void ElementLabelAndImageHaveThreeIds()
    {
        Label label = new(default(Label.CreateInfo));
        Image image = new(new Image.CreateInfo { Texture = default });
        Element element = new();

        Assert.NotEqual(element.TypeId, label.TypeId);
        Assert.NotEqual(element.TypeId, image.TypeId);
        Assert.NotEqual(label.TypeId, image.TypeId);
        Assert.False(element.TypeId.IsNone);
        Assert.False(label.TypeId.IsNone);
        Assert.False(image.TypeId.IsNone);
    }

    [Fact]
    public void TheBuiltInTypesMatchTheirNames()
    {
        Label label = new(default(Label.CreateInfo));
        Image image = new(new Image.CreateInfo { Texture = default });

        Assert.Equal(ElementType.Intern("Element"), new Element().TypeId);
        Assert.Equal(ElementType.Intern("Label"), label.TypeId);
        Assert.Equal(ElementType.Intern("Image"), image.TypeId);
    }

    [Fact]
    public void ASubclassWithNoOverrideHasTheIdOfElement()
    {
        PlainElement plain = new();

        Assert.Equal(new Element().TypeId, plain.TypeId);
    }

    [Fact]
    public void ASubclassWithAnOverrideHasItsOwnId()
    {
        NamedElement named = new();

        Assert.Equal(ElementType.Intern("ett-named"), named.TypeId);
        Assert.NotEqual(new Element().TypeId, named.TypeId);
    }

    [Fact]
    public void GetNameGivesTheNameBack()
    {
        ElementTypeId id = ElementType.Intern("ett-roundtrip");

        Assert.Equal("ett-roundtrip", ElementType.GetName(id));
        Assert.Equal("ett-roundtrip", id.ToString());
        Assert.Equal("Label", ElementType.GetName(new Label(default(Label.CreateInfo)).TypeId));
    }

    [Fact]
    public void GetNameThrowsForNoneAndForAnUnknownId()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ElementType.GetName(ElementTypeId.None));
        Assert.Equal("(none)", ElementTypeId.None.ToString());
        Assert.True(ElementTypeId.None.IsNone);
        Assert.Equal(default, ElementTypeId.None);
    }

    [Fact]
    public void InternThrowsForNullAndForAnEmptyName()
    {
        Assert.Throws<ArgumentNullException>(() => ElementType.Intern(null!));
        Assert.Throws<ArgumentException>(() => ElementType.Intern(string.Empty));
    }

    private sealed class PlainElement : Element
    {
    }

    private sealed class NamedElement : Element
    {
        private static readonly ElementTypeId s_typeId = ElementType.Intern("ett-named");

        public override ElementTypeId TypeId => s_typeId;
    }
}
