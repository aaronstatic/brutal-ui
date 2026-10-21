using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the sparse style set: the bit rule, the defaults of the getters, the clear, and the apply step.
/// </summary>
public sealed class StylePropertiesTests
{
    [Fact]
    public void ApplyToWritesEachMember()
    {
        StyleProperties properties = AllMembersSet();
        ComputedStyle target = default;

        properties.ApplyTo(ref target, skip: 0);

        Assert.Equal(FlexDirection.Row, target.FlexDirection);
        Assert.Equal(Length.Px(1), target.Width);
        Assert.Equal(Length.Px(2), target.Height);
        AssertEdges(new LengthEdges(3, 4, 5, 6), target.Margin);
        AssertEdges(new LengthEdges(7, 8, 9, 10), target.Padding);
        AssertEdges(new LengthEdges(11, 12, 13, 14), target.BorderWidth);
        Assert.Equal(15f, target.FlexGrow);
        Assert.Equal(16f, target.FlexShrink);
        Assert.Equal(Length.Percent(17), target.FlexBasis);
        Assert.Equal(Length.Px(18), target.MinWidth);
        Assert.Equal(Length.Px(19), target.MinHeight);
        Assert.Equal(Length.Px(20), target.MaxWidth);
        Assert.Equal(Length.Px(21), target.MaxHeight);
        Assert.Equal(Justify.SpaceEvenly, target.JustifyContent);
        Assert.Equal(Align.Center, target.AlignItems);
        Assert.Equal(Align.FlexEnd, target.AlignSelf);
        Assert.Equal(Align.Stretch, target.AlignContent);
        Assert.Equal(FlexWrap.Wrap, target.FlexWrap);
        Assert.Equal(Display.None, target.Display);
        Assert.Equal(PositionType.Absolute, target.Position);
        Assert.Equal(Length.Px(22), target.Left);
        Assert.Equal(Length.Px(23), target.Top);
        Assert.Equal(Length.Px(24), target.Right);
        Assert.Equal(Length.Auto, target.Bottom);
        Assert.Equal(Overflow.Scroll, target.Overflow);
        Assert.Equal(new Rgba8(1, 2, 3, 4), target.BackgroundColor);
        Assert.Equal(new Rgba8(5, 6, 7, 8), target.BorderColor);
        AssertCorners(new Corners(1, 2, 3, 4), target.CornerRadius);
        Assert.Equal(0.25f, target.Opacity);
        Assert.Equal(new Rgba8(9, 10, 11, 12), target.Color);
        Assert.Equal(new FontHandle(25), target.Font);
        Assert.Equal(26f, target.FontSize);
    }

    [Fact]
    public void ApplyToSkipsTheMaskedMembers()
    {
        StyleProperties properties = AllMembersSet();
        ComputedStyle target = default;
        target.Width = 99;
        target.BackgroundColor = new Rgba8(9, 9, 9, 9);
        target.Opacity = 0.5f;
        target.FontSize = 40f;
        ulong skip = Bit(StyleProperty.Width) | Bit(StyleProperty.BackgroundColor) | Bit(StyleProperty.Opacity)
            | Bit(StyleProperty.FontSize);

        properties.ApplyTo(ref target, skip);

        Assert.Equal(Length.Px(99), target.Width);
        Assert.Equal(new Rgba8(9, 9, 9, 9), target.BackgroundColor);
        Assert.Equal(0.5f, target.Opacity);
        Assert.Equal(40f, target.FontSize);
        Assert.Equal(Length.Px(2), target.Height);
        Assert.Equal(new Rgba8(5, 6, 7, 8), target.BorderColor);
        Assert.Equal(new Rgba8(9, 10, 11, 12), target.Color);
        Assert.Equal(new FontHandle(25), target.Font);
    }

    [Fact]
    public void ApplyToWritesNothingWhenTheSkipHoldsEachMember()
    {
        StyleProperties properties = AllMembersSet();
        ComputedStyle target = default;

        properties.ApplyTo(ref target, StyleMasks.All);

        Assert.Equal(default, target);
    }

    [Fact]
    public void ApplyToLeavesTheUnsetMembers()
    {
        StyleProperties properties = default;
        properties.Width = 10;
        ComputedStyle target = default;
        target.Height = 5;
        target.Opacity = 0.5f;

        properties.ApplyTo(ref target, skip: 0);

        Assert.Equal(Length.Px(10), target.Width);
        Assert.Equal(Length.Px(5), target.Height);
        Assert.Equal(0.5f, target.Opacity);
    }

    [Fact]
    public void ASetToTheDefaultValueSetsTheBitAndApplies()
    {
        StyleProperties properties = default;
        properties.Opacity = 1f;
        properties.Width = Length.Undefined;
        ComputedStyle target = default;
        target.Opacity = 0.3f;
        target.Width = 40;

        properties.ApplyTo(ref target, skip: 0);

        Assert.True(properties.IsSet(StyleProperty.Opacity));
        Assert.True(properties.IsSet(StyleProperty.Width));
        Assert.Equal(1f, target.Opacity);
        Assert.Equal(Length.Undefined, target.Width);
    }

    [Fact]
    public void AnUnsetGetterGivesTheDefault()
    {
        StyleProperties properties = default;
        ComputedStyle defaults = default;

        Assert.Equal(1f, properties.Opacity);
        Assert.Equal(defaults.Opacity, properties.Opacity);
        Assert.Equal(defaults.FlexDirection, properties.FlexDirection);
        Assert.Equal(defaults.Width, properties.Width);
        Assert.Equal(defaults.Height, properties.Height);
        AssertEdges(defaults.Margin, properties.Margin);
        AssertEdges(defaults.Padding, properties.Padding);
        AssertEdges(defaults.BorderWidth, properties.BorderWidth);
        Assert.Equal(defaults.FlexGrow, properties.FlexGrow);
        Assert.Equal(defaults.FlexShrink, properties.FlexShrink);
        Assert.Equal(defaults.FlexBasis, properties.FlexBasis);
        Assert.Equal(defaults.MinWidth, properties.MinWidth);
        Assert.Equal(defaults.MinHeight, properties.MinHeight);
        Assert.Equal(defaults.MaxWidth, properties.MaxWidth);
        Assert.Equal(defaults.MaxHeight, properties.MaxHeight);
        Assert.Equal(defaults.JustifyContent, properties.JustifyContent);
        Assert.Equal(defaults.AlignItems, properties.AlignItems);
        Assert.Equal(defaults.AlignSelf, properties.AlignSelf);
        Assert.Equal(defaults.AlignContent, properties.AlignContent);
        Assert.Equal(defaults.FlexWrap, properties.FlexWrap);
        Assert.Equal(defaults.Display, properties.Display);
        Assert.Equal(defaults.Position, properties.Position);
        Assert.Equal(defaults.Left, properties.Left);
        Assert.Equal(defaults.Top, properties.Top);
        Assert.Equal(defaults.Right, properties.Right);
        Assert.Equal(defaults.Bottom, properties.Bottom);
        Assert.Equal(defaults.Overflow, properties.Overflow);
        Assert.Equal(defaults.BackgroundColor, properties.BackgroundColor);
        Assert.Equal(defaults.BorderColor, properties.BorderColor);
        AssertCorners(defaults.CornerRadius, properties.CornerRadius);
        Assert.Equal(defaults.Color, properties.Color);
        Assert.Equal(defaults.Font, properties.Font);
        Assert.Equal(defaults.FontSize, properties.FontSize);
    }

    [Fact]
    public void ClearRemovesTheBitAndTheValue()
    {
        StyleProperties properties = default;
        properties.Width = 10;
        properties.Opacity = 0.5f;
        properties.Margin = LengthEdges.All(4);
        properties.CornerRadius = Corners.All(6);

        properties.Clear(StyleProperty.Width);
        properties.Clear(StyleProperty.Opacity);
        properties.Clear(StyleProperty.Margin);

        Assert.False(properties.IsSet(StyleProperty.Width));
        Assert.False(properties.IsSet(StyleProperty.Opacity));
        Assert.False(properties.IsSet(StyleProperty.Margin));
        Assert.True(properties.IsSet(StyleProperty.CornerRadius));
        Assert.Equal(Length.Undefined, properties.Width);
        Assert.Equal(1f, properties.Opacity);
        AssertEdges(default, properties.Margin);
        Assert.Equal(Bit(StyleProperty.CornerRadius), properties.SetMask);

        properties.Clear(StyleProperty.CornerRadius);

        Assert.True(properties.IsEmpty);
        Assert.Equal(default, properties);
    }

    [Fact]
    public void ClearOfEachMemberGivesAnEmptySet()
    {
        StyleProperties properties = AllMembersSet();

        foreach (StyleProperty property in Enum.GetValues<StyleProperty>())
        {
            properties.Clear(property);
        }

        Assert.True(properties.IsEmpty);
        Assert.Equal(default, properties);
    }

    [Fact]
    public void ClearOfAnUnsetMemberChangesNothing()
    {
        StyleProperties properties = default;
        properties.Width = 10;

        properties.Clear(StyleProperty.Height);

        Assert.True(properties.IsSet(StyleProperty.Width));
        Assert.Equal(Length.Px(10), properties.Width);
        Assert.Equal(Bit(StyleProperty.Width), properties.SetMask);
    }

    [Fact]
    public void IsEmptyForANewSet()
    {
        StyleProperties properties = default;

        Assert.True(properties.IsEmpty);
        Assert.Equal(0UL, properties.SetMask);
        foreach (StyleProperty property in Enum.GetValues<StyleProperty>())
        {
            Assert.False(properties.IsSet(property));
        }

        properties.BackgroundColor = new Rgba8(1, 2, 3);

        Assert.False(properties.IsEmpty);
        Assert.Equal(Bit(StyleProperty.BackgroundColor), properties.SetMask);
    }

    [Fact]
    public void EachSetterSetsTheBitOfItsMember()
    {
        StyleProperties properties = AllMembersSet();

        Assert.Equal(StyleMasks.All, properties.SetMask);
        foreach (StyleProperty property in Enum.GetValues<StyleProperty>())
        {
            Assert.True(properties.IsSet(property));
        }
    }

    [Theory]
    [InlineData(32)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(255)]
    public void IsSetAndClearThrowForAValueThatIsNotAMember(int value)
    {
        StyleProperties properties = AllMembersSet();
        StyleProperty property = (StyleProperty)value;

        Assert.Throws<ArgumentOutOfRangeException>("property", () => properties.IsSet(property));
        Assert.Throws<ArgumentOutOfRangeException>("property", () => properties.Clear(property));
        Assert.Equal(StyleMasks.All, properties.SetMask);
    }

    [Fact]
    public void EachMemberHasOneBitInOneMask()
    {
        StyleProperty[] members = Enum.GetValues<StyleProperty>();

        foreach (StyleProperty property in members)
        {
            Assert.InRange((int)property, 0, 63);
            ulong bit = Bit(property);
            int masks = ((StyleMasks.Layout & bit) != 0 ? 1 : 0)
                + ((StyleMasks.Paint & bit) != 0 ? 1 : 0)
                + ((StyleMasks.Text & bit) != 0 ? 1 : 0);
            Assert.True(masks == 1, $"{property} must be in one mask, but it is in {masks}.");
        }

        Assert.Equal(32, members.Length);
        Assert.Equal(25, BitOperations.PopCount(StyleMasks.Layout));
        Assert.Equal(4, BitOperations.PopCount(StyleMasks.Paint));
        Assert.Equal(3, BitOperations.PopCount(StyleMasks.Text));
        Assert.Equal(0UL, StyleMasks.Layout & StyleMasks.Paint);
        Assert.Equal(0UL, StyleMasks.Layout & StyleMasks.Text);
        Assert.Equal(0UL, StyleMasks.Paint & StyleMasks.Text);
        Assert.Equal(members.Length, BitOperations.PopCount(StyleMasks.All));
        Assert.Equal(StyleMasks.Layout | StyleMasks.Paint | StyleMasks.Text, StyleMasks.All);
        Assert.Equal(0UL, StyleMasks.Layout & Bit(StyleProperty.BackgroundColor));
        Assert.NotEqual(0UL, StyleMasks.Layout & Bit(StyleProperty.Overflow));
        Assert.Equal(StyleMasks.Text, Bit(StyleProperty.Color) | Bit(StyleProperty.Font) | Bit(StyleProperty.FontSize));
        Assert.Equal(29, (int)StyleProperty.Color);
        Assert.Equal(31, (int)StyleProperty.FontSize);
    }

    [Fact]
    public void TheEnumCoversEachComputedStyleMember()
    {
        string[] fields = typeof(ComputedStyle)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(field => field.Name)
            .ToArray();
        string[] properties = typeof(ComputedStyle)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();
        string[] styleMembers = fields.Concat(properties).Order(StringComparer.Ordinal).ToArray();
        string[] enumMembers = Enum.GetNames<StyleProperty>().Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(["Opacity"], properties);
        Assert.True(
            styleMembers.Length == enumMembers.Length,
            $"ComputedStyle has {styleMembers.Length} members and StyleProperty has {enumMembers.Length}. " +
            $"ComputedStyle: {string.Join(", ", styleMembers)}. StyleProperty: {string.Join(", ", enumMembers)}.");
        Assert.Equal(styleMembers, enumMembers);
    }

    [Fact]
    public void EachMemberHasAPropertyOfTheComputedStyleType()
    {
        foreach (string name in Enum.GetNames<StyleProperty>())
        {
            PropertyInfo? setProperty = typeof(StyleProperties).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Type? styleType = typeof(ComputedStyle).GetField(name, BindingFlags.Public | BindingFlags.Instance)?.FieldType
                ?? typeof(ComputedStyle).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.PropertyType;

            Assert.True(setProperty is not null, $"StyleProperties has no property {name}.");
            Assert.True(styleType is not null, $"ComputedStyle has no member {name}.");
            Assert.Equal(styleType, setProperty.PropertyType);
            Assert.True(setProperty.CanRead && setProperty.CanWrite, $"StyleProperties.{name} must have a get and a set.");
        }
    }

    private static ulong Bit(StyleProperty property) => 1UL << (int)property;

    /// <summary>Gives a set with each member set to a value that is not the default. Each value is distinct.</summary>
    private static StyleProperties AllMembersSet()
    {
        StyleProperties properties = default;
        properties.FlexDirection = FlexDirection.Row;
        properties.Width = 1;
        properties.Height = 2;
        properties.Margin = new LengthEdges(3, 4, 5, 6);
        properties.Padding = new LengthEdges(7, 8, 9, 10);
        properties.BorderWidth = new LengthEdges(11, 12, 13, 14);
        properties.FlexGrow = 15;
        properties.FlexShrink = 16;
        properties.FlexBasis = Length.Percent(17);
        properties.MinWidth = 18;
        properties.MinHeight = 19;
        properties.MaxWidth = 20;
        properties.MaxHeight = 21;
        properties.JustifyContent = Justify.SpaceEvenly;
        properties.AlignItems = Align.Center;
        properties.AlignSelf = Align.FlexEnd;
        properties.AlignContent = Align.Stretch;
        properties.FlexWrap = FlexWrap.Wrap;
        properties.Display = Display.None;
        properties.Position = PositionType.Absolute;
        properties.Left = 22;
        properties.Top = 23;
        properties.Right = 24;
        properties.Bottom = Length.Auto;
        properties.Overflow = Overflow.Scroll;
        properties.BackgroundColor = new Rgba8(1, 2, 3, 4);
        properties.BorderColor = new Rgba8(5, 6, 7, 8);
        properties.CornerRadius = new Corners(1, 2, 3, 4);
        properties.Opacity = 0.25f;
        properties.Color = new Rgba8(9, 10, 11, 12);
        properties.Font = new FontHandle(25);
        properties.FontSize = 26;
        return properties;
    }

    private static void AssertEdges(LengthEdges expected, LengthEdges actual)
    {
        Assert.Equal(expected.Left, actual.Left);
        Assert.Equal(expected.Top, actual.Top);
        Assert.Equal(expected.Right, actual.Right);
        Assert.Equal(expected.Bottom, actual.Bottom);
    }

    private static void AssertCorners(Corners expected, Corners actual)
    {
        Assert.Equal(expected.TopLeft, actual.TopLeft);
        Assert.Equal(expected.TopRight, actual.TopRight);
        Assert.Equal(expected.BottomRight, actual.BottomRight);
        Assert.Equal(expected.BottomLeft, actual.BottomLeft);
    }
}
