using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the element group of <see cref="ElementBuilder"/>: <c>Name</c>, the dirty bits, and the null checks.
/// </summary>
public sealed class ElementBuilderElementTests
{
    /// <summary>A literal name compiles. The method sets the name and returns the same element with its own type.</summary>
    [Fact]
    public void NameSetsTheNameAndReturnsTheSameElementWithItsType()
    {
        Label built = new Label(default(Label.CreateInfo));

        // The literal argument of Name compiles: no span overload exists for the name.
        Label label = new Label(default(Label.CreateInfo)).Name("score");

        Assert.Equal("score", label.Name);
        Assert.True(ReferenceEquals(built, built.Name("score")));
    }

    /// <summary>The same name marks nothing. A null name clears the name and marks the style.</summary>
    [Fact]
    public void NameWithNullClearsTheNameAndTheSameNameMarksNothing()
    {
        UiContext context = new();
        Element root = new Element().Name("score");
        context.AddRoot(root);
        context.Update(0f);
        Assert.Equal(DirtyFlags.None, root.Dirty);

        root.Name("score");

        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal("score", root.Name);

        root.Name((string?)null);

        Assert.Null(root.Name);
        Assert.Equal(DirtyFlags.Style, root.Dirty);

        root.Name("");

        Assert.Equal("", root.Name);
    }

    /// <summary>Pickable and Focusable set the flags and return the same element with its own type.</summary>
    [Fact]
    public void PickableAndFocusableSetTheFlagsAndReturnTheSameElement()
    {
        Element element = new Element();
        Assert.True(element.IsPickable);
        Assert.False(element.IsFocusable);

        Element pickable = element.Pickable(false);
        Element focusable = element.Focusable(true);

        Assert.False(element.IsPickable);
        Assert.True(element.IsFocusable);
        Assert.True(ReferenceEquals(element, pickable));
        Assert.True(ReferenceEquals(element, focusable));
    }

    /// <summary>A null element throws <see cref="ArgumentNullException"/> with the parameter name <c>element</c>.</summary>
    [Fact]
    public void TheElementMethodsThrowForANullElement()
    {
        Element nullElement = null!;

        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Name("score")).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Pickable(false)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Focusable(true)).ParamName);
    }
}
