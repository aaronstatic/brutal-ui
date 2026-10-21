using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the type selector: the demo, the specificity, the order of a type rule and a class rule, the sort of
/// the builder, the exact match of the type id, and the factory rules.
/// </summary>
/// <remarks>
/// The class table and the type table are global and xUnit runs test classes in parallel.
/// Each test uses names with a unique prefix, and no test asserts a fixed id value.
/// </remarks>
public sealed class TypeSelectorTests
{
    private static readonly Rgba8 Gold = new(255, 204, 0, 255);
    private static readonly Rgba8 Green = new(0, 255, 0, 255);
    private static readonly Rgba8 Red = new(255, 0, 0, 255);

    [Fact]
    public void ATypeSelectorStylesEachLabel()
    {
        ClassId title = ClassName.Intern(nameof(ATypeSelectorStylesEachLabel) + ".title");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForType(LabelType()), new StyleProperties { Color = Gold })
            .Add(Selector.ForClass(title), new StyleProperties { Color = Green })
            .Add(Selector.ForTypeAndClass(LabelType(), title), new StyleProperties { FontSize = 24f })
            .Build();

        Element root = new() { Name = "root" };
        Label plainLabel = NewLabel("label");
        Label titleLabel = NewLabel("title label");
        Element titlePanel = new() { Name = "title panel" };
        titleLabel.Classes.Add(title);
        titlePanel.Classes.Add(title);
        root.Children.Add(plainLabel);
        root.Children.Add(titleLabel);
        root.Children.Add(titlePanel);

        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        string plainLine = Describe(plainLabel);
        string titleLine = Describe(titleLabel);
        string panelLine = Describe(titlePanel);
        TestContext.Current.TestOutputHelper?.WriteLine(plainLine);
        TestContext.Current.TestOutputHelper?.WriteLine(titleLine);
        TestContext.Current.TestOutputHelper?.WriteLine(panelLine);

        Assert.Equal("label #FFCC00FF size 0", plainLine);
        Assert.Equal("title label #00FF00FF size 24", titleLine);
        Assert.Equal("title panel #00FF00FF size 0", panelLine);
    }

    [Fact]
    public void TheSpecificityPutsEachTierInItsOwnByte()
    {
        ClassId id = ClassName.Intern(nameof(TheSpecificityPutsEachTierInItsOwnByte) + ".a");
        ElementTypeId type = LabelType();

        Assert.Equal(1, Selector.ForType(type).Specificity);
        Assert.Equal(256, Selector.ForClass(id).Specificity);
        Assert.Equal(257, Selector.ForTypeAndClass(type, id).Specificity);
        Assert.Equal(0, default(Selector).Specificity);
        Assert.Equal(type, Selector.ForTypeAndClass(type, id).Type);
        Assert.Equal(id, Selector.ForTypeAndClass(type, id).Class);
        Assert.Equal(ElementTypeId.None, Selector.ForClass(id).Type);
        Assert.Equal(ClassId.None, Selector.ForType(type).Class);
    }

    [Fact]
    public void AClassRuleBeatsATypeRuleInEachOrder()
    {
        ClassId id = ClassName.Intern(nameof(AClassRuleBeatsATypeRuleInEachOrder) + ".a");
        StyleProperties typeRule = new() { Color = Red };
        StyleProperties classRule = new() { Color = Green };

        StyleSheet typeFirst = new StyleSheet.Builder()
            .Add(Selector.ForType(LabelType()), typeRule)
            .Add(Selector.ForClass(id), classRule)
            .Build();
        StyleSheet classFirst = new StyleSheet.Builder()
            .Add(Selector.ForClass(id), classRule)
            .Add(Selector.ForType(LabelType()), typeRule)
            .Build();

        Assert.Equal(Green, StyledColor(typeFirst, id));
        Assert.Equal(Green, StyledColor(classFirst, id));
    }

    [Fact]
    public void TheBuilderSortsATypeRuleFirst()
    {
        ClassId id = ClassName.Intern(nameof(TheBuilderSortsATypeRuleFirst) + ".a");
        ElementTypeId type = LabelType();

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForTypeAndClass(type, id), new StyleProperties { FontSize = 3f })
            .Add(Selector.ForClass(id), new StyleProperties { FontSize = 2f })
            .Add(Selector.ForType(type), new StyleProperties { FontSize = 1f })
            .Build();

        Assert.Equal(3, sheet.Rules.Length);
        Assert.Equal(1, sheet.Rules[0].Specificity);
        Assert.Equal(type, sheet.Rules[0].Selector.Type);
        Assert.Equal(256, sheet.Rules[1].Specificity);
        Assert.Equal(257, sheet.Rules[2].Specificity);
    }

    [Fact]
    public void ATypeRuleDoesNotMatchASubclassWithItsOwnId()
    {
        Selector elementRule = Selector.ForType(new Element().TypeId);
        Selector badgeRule = Selector.ForType(ElementType.Intern("tst-" + nameof(TypedBadge)));

        Element plain = new();
        Element sameId = new SameIdElement();
        Element badge = new TypedBadge();

        Assert.True(elementRule.Matches(plain));
        Assert.True(elementRule.Matches(sameId));
        Assert.False(elementRule.Matches(badge));
        Assert.True(badgeRule.Matches(badge));
        Assert.False(badgeRule.Matches(plain));

        // The pass gives the same result through a sheet.
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(elementRule, new StyleProperties { BackgroundColor = Red })
            .Build();
        Element root = new();
        root.Children.Add(sameId);
        root.Children.Add(badge);
        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        // The background color does not inherit, so only a matching rule sets it.
        Assert.Equal(Red, root.Style.BackgroundColor);
        Assert.Equal(Red, sameId.Style.BackgroundColor);
        Assert.Equal(default, badge.Style.BackgroundColor);
    }

    [Fact]
    public void ATypeRuleDoesNotMatchAnotherType()
    {
        Label label = NewLabel("label");
        Image image = new(default(Image.CreateInfo));
        Selector labelRule = Selector.ForType(LabelType());

        Assert.True(labelRule.Matches(label));
        Assert.False(labelRule.Matches(image));
        Assert.False(labelRule.Matches(new Element()));
    }

    [Fact]
    public void ATypeAndClassRuleNeedsBothParts()
    {
        ClassId id = ClassName.Intern(nameof(ATypeAndClassRuleNeedsBothParts) + ".a");
        Selector selector = Selector.ForTypeAndClass(LabelType(), id);
        Label withClass = NewLabel("a");
        Label withoutClass = NewLabel("b");
        Element panelWithClass = new();
        withClass.Classes.Add(id);
        panelWithClass.Classes.Add(id);

        Assert.True(selector.Matches(withClass));
        Assert.False(selector.Matches(withoutClass));
        Assert.False(selector.Matches(panelWithClass));
    }

    [Fact]
    public void ForTypeThrowsForNone()
    {
        Assert.Throws<ArgumentException>("type", () => Selector.ForType(ElementTypeId.None));
    }

    [Fact]
    public void ForTypeAndClassThrowsForTwoNones()
    {
        Assert.Throws<ArgumentException>(() => Selector.ForTypeAndClass(ElementTypeId.None, ClassId.None));
    }

    [Fact]
    public void ForTypeAndClassWithOneNoneGivesTheOtherSelector()
    {
        ClassId id = ClassName.Intern(nameof(ForTypeAndClassWithOneNoneGivesTheOtherSelector) + ".a");
        ElementTypeId type = LabelType();

        Selector classOnly = Selector.ForTypeAndClass(ElementTypeId.None, id);
        Selector typeOnly = Selector.ForTypeAndClass(type, ClassId.None);

        Assert.Equal(256, classOnly.Specificity);
        Assert.Equal(id, classOnly.Class);
        Assert.Equal(ElementTypeId.None, classOnly.Type);
        Assert.Equal(1, typeOnly.Specificity);
        Assert.Equal(type, typeOnly.Type);
        Assert.Equal(ClassId.None, typeOnly.Class);
    }

    [Fact]
    public void TheBuilderAcceptsATypeSelectorAndRejectsADefaultSelector()
    {
        StyleSheet.Builder builder = new();

        builder.Add(Selector.ForType(LabelType()), new StyleProperties { Color = Red });

        Assert.False(Selector.ForType(LabelType()).IsEmpty);
        Assert.Throws<ArgumentException>("selector", () => builder.Add(default, new StyleProperties()));
    }

    private static ElementTypeId LabelType()
    {
        return NewLabel("probe").TypeId;
    }

    private static Label NewLabel(string name)
    {
        return new Label(default(Label.CreateInfo)) { Name = name };
    }

    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
    }

    /// <summary>Styles one label with the class and returns its color.</summary>
    private static Rgba8 StyledColor(StyleSheet sheet, ClassId id)
    {
        Element root = new();
        Label label = NewLabel("label");
        label.Classes.Add(id);
        root.Children.Add(label);
        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);
        return label.Style.Color;
    }

    private static string Describe(Element element)
    {
        return element.Name + " " + element.Style.Color + " size " + element.Style.FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>A subclass with its own type id.</summary>
    private sealed class TypedBadge : Element
    {
        private static readonly ElementTypeId s_typeId = ElementType.Intern("tst-" + nameof(TypedBadge));

        public override ElementTypeId TypeId => s_typeId;
    }

    /// <summary>A subclass that keeps the type id of <see cref="Element"/>.</summary>
    private sealed class SameIdElement : Element
    {
    }
}
