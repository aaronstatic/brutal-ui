using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the compound with pseudo-states: the hover demo, the match of each state bit, the compound of a type,
/// a class, and states, the order of a state rule and a class rule, the specificity tiers, and the factory rules.
/// </summary>
/// <remarks>
/// The class table and the type table are global and xUnit runs test classes in parallel.
/// Each test uses names with a unique prefix, and no test asserts a fixed id value.
/// </remarks>
public sealed class PseudoStateSelectorTests
{
    private static readonly Rgba8 Idle = new(30, 34, 44, 255);
    private static readonly Rgba8 Hovered = new(48, 96, 192, 255);
    private static readonly Rgba8 Green = new(0, 255, 0, 255);
    private static readonly Rgba8 Red = new(255, 0, 0, 255);

    private static readonly PseudoStates[] EachState =
    [
        PseudoStates.Hover,
        PseudoStates.Active,
        PseudoStates.Focus,
        PseudoStates.Disabled,
        PseudoStates.Checked,
    ];

    [Fact]
    public void AHoverRuleRestylesTheButton()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AHoverRuleRestylesTheButton) + ".button");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { BackgroundColor = Idle })
            .Add(
                Selector.For(new CompoundSelector { Class = buttonClass, States = PseudoStates.Hover }),
                new StyleProperties { BackgroundColor = Hovered })
            .Build();

        Element root = new() { Name = "root" };
        Element button = new() { Name = "button" };
        button.Classes.Add(buttonClass);
        root.Children.Add(button);

        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);
        string idleLine = Describe(button);

        button.States = PseudoStates.Hover;
        context.Update(0f);
        string hoverLine = Describe(button);

        button.States = PseudoStates.None;
        context.Update(0f);
        string backLine = Describe(button);

        TestContext.Current.TestOutputHelper?.WriteLine(idleLine);
        TestContext.Current.TestOutputHelper?.WriteLine(hoverLine);
        TestContext.Current.TestOutputHelper?.WriteLine(backLine);

        Assert.Equal("button #1E222CFF", idleLine);
        Assert.Equal("button hover #3060C0FF", hoverLine);
        Assert.Equal("button #1E222CFF", backLine);
    }

    [Theory]
    [InlineData(PseudoStates.Hover)]
    [InlineData(PseudoStates.Active)]
    [InlineData(PseudoStates.Focus)]
    [InlineData(PseudoStates.Disabled)]
    [InlineData(PseudoStates.Checked)]
    public void EachPseudoStateMatchesItsOwnBit(PseudoStates state)
    {
        ClassId x = ClassName.Intern(nameof(EachPseudoStateMatchesItsOwnBit) + ".x");
        Selector selector = Selector.For(new CompoundSelector { Class = x, States = state });
        Element element = new();
        element.Classes.Add(x);
        Element withoutClass = new() { States = state };

        Assert.False(selector.Matches(element));
        Assert.False(selector.Matches(withoutClass));

        element.States = state;
        Assert.True(selector.Matches(element));

        foreach (PseudoStates other in EachState)
        {
            if (other == state)
            {
                continue;
            }

            element.States = other;
            Assert.False(selector.Matches(element));
        }
    }

    [Fact]
    public void ACompoundNeedsEachState()
    {
        Selector selector = Selector.For(new CompoundSelector { States = PseudoStates.Hover | PseudoStates.Focus });
        Element element = new();

        element.States = PseudoStates.Hover;
        Assert.False(selector.Matches(element));

        element.States = PseudoStates.Focus;
        Assert.False(selector.Matches(element));

        element.States = PseudoStates.Hover | PseudoStates.Focus;
        Assert.True(selector.Matches(element));

        element.States = PseudoStates.Hover | PseudoStates.Focus | PseudoStates.Checked;
        Assert.True(selector.Matches(element));
    }

    [Fact]
    public void ATypeClassAndStateCompoundNeedsEachPart()
    {
        ClassId x = ClassName.Intern(nameof(ATypeClassAndStateCompoundNeedsEachPart) + ".x");
        Selector selector = Selector.For(
            new CompoundSelector { Type = LabelType(), Class = x, States = PseudoStates.Checked });

        Label each = NewLabel("each");
        each.Classes.Add(x);
        each.States = PseudoStates.Checked;

        Label noState = NewLabel("no state");
        noState.Classes.Add(x);

        Label noClass = NewLabel("no class");
        noClass.States = PseudoStates.Checked;

        Element noType = new() { States = PseudoStates.Checked };
        noType.Classes.Add(x);

        Assert.True(selector.Matches(each));
        Assert.False(selector.Matches(noState));
        Assert.False(selector.Matches(noClass));
        Assert.False(selector.Matches(noType));
    }

    [Fact]
    public void AStateOnlyRuleMatchesEachElementInTheState()
    {
        Selector selector = Selector.For(new CompoundSelector { States = PseudoStates.Disabled });
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(selector, new StyleProperties { BackgroundColor = Red })
            .Build();

        Element root = new();
        Element disabledPanel = new() { States = PseudoStates.Disabled };
        Label disabledLabel = NewLabel("label");
        disabledLabel.States = PseudoStates.Disabled | PseudoStates.Hover;
        Element enabledPanel = new() { States = PseudoStates.Hover };
        root.Children.Add(disabledPanel);
        root.Children.Add(disabledLabel);
        root.Children.Add(enabledPanel);

        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);

        // The background color does not inherit, so only a matching rule sets it.
        Assert.Equal(Red, disabledPanel.Style.BackgroundColor);
        Assert.Equal(Red, disabledLabel.Style.BackgroundColor);
        Assert.Equal(default, enabledPanel.Style.BackgroundColor);
        Assert.Equal(default, root.Style.BackgroundColor);
    }

    [Fact]
    public void AStateRuleWinsOverTheClassRuleInEachOrder()
    {
        ClassId a = ClassName.Intern(nameof(AStateRuleWinsOverTheClassRuleInEachOrder) + ".a");
        Selector classRule = Selector.ForClass(a);
        Selector stateRule = Selector.For(new CompoundSelector { Class = a, States = PseudoStates.Hover });
        StyleProperties classValues = new() { BackgroundColor = Red };
        StyleProperties stateValues = new() { BackgroundColor = Green };

        StyleSheet classFirst = new StyleSheet.Builder()
            .Add(classRule, classValues)
            .Add(stateRule, stateValues)
            .Build();
        StyleSheet stateFirst = new StyleSheet.Builder()
            .Add(stateRule, stateValues)
            .Add(classRule, classValues)
            .Build();

        Assert.Equal(Green, StyledBackground(classFirst, a, PseudoStates.Hover));
        Assert.Equal(Green, StyledBackground(stateFirst, a, PseudoStates.Hover));
        Assert.Equal(Red, StyledBackground(classFirst, a, PseudoStates.None));
        Assert.Equal(Red, StyledBackground(stateFirst, a, PseudoStates.None));
    }

    [Fact]
    public void TheSpecificityCountsEachStateInTheClassTier()
    {
        ClassId a = ClassName.Intern(nameof(TheSpecificityCountsEachStateInTheClassTier) + ".a");
        ElementTypeId label = LabelType();
        PseudoStates all = PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus | PseudoStates.Disabled
            | PseudoStates.Checked;

        Assert.Equal(256, Selector.For(new CompoundSelector { States = PseudoStates.Hover }).Specificity);
        Assert.Equal(512, Selector.For(new CompoundSelector { Class = a, States = PseudoStates.Hover }).Specificity);
        Assert.Equal(
            768,
            Selector.For(new CompoundSelector { Class = a, States = PseudoStates.Hover | PseudoStates.Focus }).Specificity);
        Assert.Equal(257, Selector.For(new CompoundSelector { Type = label, States = PseudoStates.Checked }).Specificity);
        Assert.Equal(1537, Selector.For(new CompoundSelector { Type = label, Class = a, States = all }).Specificity);
    }

    [Fact]
    public void ForKeepsEachPartOfTheCompound()
    {
        ClassId a = ClassName.Intern(nameof(ForKeepsEachPartOfTheCompound) + ".a");
        ElementTypeId label = LabelType();

        Selector selector = Selector.For(new CompoundSelector { Type = label, Class = a, States = PseudoStates.Focus });

        Assert.Equal(label, selector.Subject.Type);
        Assert.Equal(a, selector.Subject.Class);
        Assert.Equal(PseudoStates.Focus, selector.Subject.States);
        Assert.Equal(label, selector.Type);
        Assert.Equal(a, selector.Class);
        Assert.False(selector.IsEmpty);
        Assert.Equal(PseudoStates.None, Selector.ForTypeAndClass(label, a).Subject.States);
        Assert.Equal(PseudoStates.None, default(Selector).Subject.States);
    }

    [Fact]
    public void ForThrowsForAWrongCompound()
    {
        Assert.Throws<ArgumentException>("subject", () => Selector.For(default(CompoundSelector)));
        Assert.Throws<ArgumentException>(
            "subject",
            () => Selector.For(new CompoundSelector { States = (PseudoStates)32 }));
        Assert.Throws<ArgumentException>(
            "subject",
            () => Selector.For(new CompoundSelector { States = PseudoStates.Hover | (PseudoStates)32 }));
        Assert.Throws<ArgumentException>(
            "subject",
            () => Selector.For(new CompoundSelector { States = (PseudoStates)(-1) }));
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

    /// <summary>Styles one element with the class and the states, and returns its background color.</summary>
    private static Rgba8 StyledBackground(StyleSheet sheet, ClassId id, PseudoStates states)
    {
        Element root = new();
        Element element = new() { States = states };
        element.Classes.Add(id);
        root.Children.Add(element);
        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);
        return element.Style.BackgroundColor;
    }

    /// <summary>Gives the name, each state in lower case, and the background color of an element.</summary>
    private static string Describe(Element element)
    {
        string states = element.States == PseudoStates.None
            ? string.Empty
            : " " + element.States.ToString().ToLowerInvariant();
        return element.Name + states + " " + element.Style.BackgroundColor;
    }
}
