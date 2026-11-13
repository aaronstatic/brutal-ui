using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of <see cref="TreeCompare"/>: the equal case, and one case for each kind of difference.
/// </summary>
/// <remarks>
/// Each case builds two trees that differ in one member. The test asserts the path and the member name in the
/// message, not the whole sentence.
/// </remarks>
public sealed class TreeCompareTests
{
    /// <summary>Texture id 3, size 64 by 32, for the image cases.</summary>
    private static readonly TextureHandle Texture = new(3, new int2(64, 32));

    /// <summary>A tree with one element of each of the eight types, two levels deep, gives no difference.</summary>
    [Fact]
    public void TwoEqualTreesGiveNoDifference()
    {
        static Element Build()
        {
            Button button = new Button(new Button.CreateInfo { Text = "Pause" }).Name("tree-compare.button");
            button.Clicked += static _ => { };
            button
                .Child(new Toggle(new Toggle.CreateInfo { Checked = true }).Name("tree-compare.toggle"))
                .Child(new Slider(new Slider.CreateInfo { Minimum = 0f, Maximum = 10f, Step = 1f, Value = 4f }).Name("tree-compare.slider"))
                .Child(new ScrollView(new ScrollView.CreateInfo { ScrollSpeed = 30f }).Name("tree-compare.view"));

            Element root = new Element().Name("tree-compare.root").Class("tree-compare.hud");
            root.OnPointerDown = static (ref PointerEvent e) => { };
            root
                .Child(new Label(new Label.CreateInfo { Text = "0" }).Name("tree-compare.score"))
                .Child(button)
                .Child(new TextField(new TextField.CreateInfo { Capacity = 12, Text = "abc" }).Name("tree-compare.field"))
                .Child(new Image(new Image.CreateInfo { Texture = Texture }).Name("tree-compare.icon"));
            return root;
        }

        Assert.Null(TreeCompare.FirstDifference(Build(), Build()));
    }

    /// <summary>A name difference, a class difference, and a class order difference each give the path and the member.</summary>
    [Fact]
    public void ANameOrAClassDifferenceGivesThePath()
    {
        string? name = TreeCompare.FirstDifference(
            WithChild(new Element().Name("tree-compare.score")),
            WithChild(new Element().Name("tree-compare.scor")));
        Assert.NotNull(name);
        Assert.Contains("root/0", name, StringComparison.Ordinal);
        Assert.Contains("Name", name, StringComparison.Ordinal);

        string? classOfOne = TreeCompare.FirstDifference(
            WithChild(new Element().Class("tree-compare.hud")),
            WithChild(new Element().Class("tree-compare.panel")));
        Assert.NotNull(classOfOne);
        Assert.Contains("root/0", classOfOne, StringComparison.Ordinal);
        Assert.Contains("Class 0", classOfOne, StringComparison.Ordinal);

        string? classOrder = TreeCompare.FirstDifference(
            WithChild(new Element().Class("tree-compare.a").Class("tree-compare.b")),
            WithChild(new Element().Class("tree-compare.b").Class("tree-compare.a")));
        Assert.NotNull(classOrder);
        Assert.Contains("root/0", classOrder, StringComparison.Ordinal);
        Assert.Contains("Class 0", classOrder, StringComparison.Ordinal);
    }

    /// <summary>A runtime type difference and a child count difference each give the path and the member.</summary>
    [Fact]
    public void ATypeOrAChildCountDifferenceGivesThePath()
    {
        string? type = TreeCompare.FirstDifference(
            WithChild(new Element()),
            WithChild(new Label(default(Label.CreateInfo))));
        Assert.NotNull(type);
        Assert.Contains("root/0", type, StringComparison.Ordinal);
        Assert.Contains("Type", type, StringComparison.Ordinal);

        Element two = new Element().Child(new Element()).Child(new Element());
        Element three = new Element().Child(new Element()).Child(new Element()).Child(new Element());
        string? childCount = TreeCompare.FirstDifference(two, three);
        Assert.NotNull(childCount);
        Assert.Contains("root", childCount, StringComparison.Ordinal);
        Assert.Contains("Child count", childCount, StringComparison.Ordinal);
    }

    /// <summary>Each widget value difference gives the path and the member name.</summary>
    [Fact]
    public void AWidgetValueDifferenceGivesThePath()
    {
        AssertOneDifference(
            "Text",
            WithChild(new Label(new Label.CreateInfo { Text = "one" })),
            WithChild(new Label(new Label.CreateInfo { Text = "two" })));

        AssertOneDifference(
            "Value",
            WithChild(new Slider(new Slider.CreateInfo { Maximum = 10f, Value = 4f })),
            WithChild(new Slider(new Slider.CreateInfo { Maximum = 10f, Value = 5f })));

        AssertOneDifference(
            "Tint",
            WithChild(new Image(new Image.CreateInfo { Texture = Texture, Tint = new Rgba8(255, 255, 255) })),
            WithChild(new Image(new Image.CreateInfo { Texture = Texture, Tint = new Rgba8(255, 0, 0) })));

        AssertOneDifference(
            "Texture.Size",
            WithChild(new Image(new Image.CreateInfo { Texture = new TextureHandle(3, new int2(64, 32)) })),
            WithChild(new Image(new Image.CreateInfo { Texture = new TextureHandle(3, new int2(64, 33)) })));

        AssertOneDifference(
            "Capacity",
            WithChild(new TextField(new TextField.CreateInfo { Capacity = 8 })),
            WithChild(new TextField(new TextField.CreateInfo { Capacity = 16 })));

        ScrollViewTree scrolled = new();
        scrolled.Settle();
        scrolled.View.ScrollOffset = new float2(0f, 40f);
        ScrollViewTree top = new();
        top.Settle();
        AssertOneDifference("ScrollOffset", scrolled.Root, top.Root);
    }

    /// <summary>A delegate count difference gives the path and the slot member.</summary>
    [Fact]
    public void ADelegateCountDifferenceGivesThePath()
    {
        Element withHandler = WithChild(new Element());
        withHandler.Children[0].OnKeyDown = static (ref KeyEvent e) => { };
        AssertOneDifference("OnKeyDown count", withHandler, WithChild(new Element()));

        Button one = new Button(default(Button.CreateInfo));
        one.Clicked += static _ => { };
        Button two = new Button(default(Button.CreateInfo));
        two.Clicked += static _ => { };
        two.Clicked += static _ => { };
        AssertOneDifference("Clicked count", WithChild(one), WithChild(two));
    }

    /// <summary>Asserts that the first difference of two trees names one member at <c>root/0</c>.</summary>
    /// <param name="member">The member name that the message must hold.</param>
    /// <param name="expected">The expected tree.</param>
    /// <param name="actual">The actual tree.</param>
    private static void AssertOneDifference(string member, Element expected, Element actual)
    {
        string? message = TreeCompare.FirstDifference(expected, actual);
        Assert.NotNull(message);
        Assert.Contains("root/0", message, StringComparison.Ordinal);
        Assert.Contains(member, message, StringComparison.Ordinal);
    }

    /// <summary>Gives a fresh root with one child.</summary>
    /// <param name="child">The child.</param>
    /// <returns>The root.</returns>
    private static Element WithChild(Element child) => new Element().Child(child);
}
