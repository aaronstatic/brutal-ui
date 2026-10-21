using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the child chain and the descendant chain: the match of the parent and the grandparent, the bound of the
/// walk, the subject check, the root, an ancestor state, an ancestor type, the specificity sums, and the factory rules.
/// </summary>
/// <remarks>
/// <para>
/// The class table and the type table are global and xUnit runs test classes in parallel.
/// Each test uses names with a unique prefix, and no test asserts a fixed id value.
/// </para>
/// <para>
/// Each test sets the classes and the states before the first <see cref="UiContext.Update(float)"/>, so it needs no
/// subtree mark. Each test checks the direct match and the result of the style pass, and both must agree.
/// </para>
/// </remarks>
public sealed class ChainSelectorTests
{
    private static readonly Rgba8 Red = new(255, 0, 0, 255);

    [Fact]
    public void AChildRuleMatchesOnlyTheParent()
    {
        ClassId a = ClassName.Intern(nameof(AChildRuleMatchesOnlyTheParent) + ".a");
        ClassId b = ClassName.Intern(nameof(AChildRuleMatchesOnlyTheParent) + ".b");
        Selector selector = Selector.Child(new CompoundSelector { Class = a }, new CompoundSelector { Class = b });

        // .a on the parent.
        Element parentRoot = new();
        Element parentA = NewElement(a);
        Element subjectOfParent = NewElement(b);
        parentRoot.Children.Add(parentA);
        parentA.Children.Add(subjectOfParent);

        // .a on the grandparent only.
        Element grandparentA = NewElement(a);
        Element plainParent = new();
        Element subjectOfGrandparent = NewElement(b);
        grandparentA.Children.Add(plainParent);
        plainParent.Children.Add(subjectOfGrandparent);

        Assert.True(MatchesDirectAndThroughThePass(selector, parentRoot, subjectOfParent));
        Assert.False(MatchesDirectAndThroughThePass(selector, grandparentA, subjectOfGrandparent));
    }

    [Fact]
    public void ADescendantRuleMatchesTheParentAndTheGrandparent()
    {
        ClassId a = ClassName.Intern(nameof(ADescendantRuleMatchesTheParentAndTheGrandparent) + ".a");
        ClassId b = ClassName.Intern(nameof(ADescendantRuleMatchesTheParentAndTheGrandparent) + ".b");
        Selector selector = Selector.Descendant(new CompoundSelector { Class = a }, new CompoundSelector { Class = b });

        // .a on the parent.
        Element parentA = NewElement(a);
        Element subjectOfParent = NewElement(b);
        parentA.Children.Add(subjectOfParent);

        // .a on the grandparent only.
        Element grandparentA = NewElement(a);
        Element plainParent = new();
        Element subjectOfGrandparent = NewElement(b);
        grandparentA.Children.Add(plainParent);
        plainParent.Children.Add(subjectOfGrandparent);

        // .a on the parent and on the grandparent.
        Element bothGrandparent = NewElement(a);
        Element bothParent = NewElement(a);
        Element subjectOfBoth = NewElement(b);
        bothGrandparent.Children.Add(bothParent);
        bothParent.Children.Add(subjectOfBoth);

        Assert.True(MatchesDirectAndThroughThePass(selector, parentA, subjectOfParent));
        Assert.True(MatchesDirectAndThroughThePass(selector, grandparentA, subjectOfGrandparent));
        Assert.True(MatchesDirectAndThroughThePass(selector, bothGrandparent, subjectOfBoth));

        // The parent with .a has no .b, so the rule does not style it.
        Assert.False(selector.Matches(plainParent));
    }

    [Fact]
    public void ADescendantRuleStopsAtTheGrandparent()
    {
        ClassId a = ClassName.Intern(nameof(ADescendantRuleStopsAtTheGrandparent) + ".a");
        ClassId b = ClassName.Intern(nameof(ADescendantRuleStopsAtTheGrandparent) + ".b");
        Selector selector = Selector.Descendant(new CompoundSelector { Class = a }, new CompoundSelector { Class = b });

        Element greatGrandparentA = NewElement(a);
        Element grandparent = new();
        Element parent = new();
        Element subject = NewElement(b);
        greatGrandparentA.Children.Add(grandparent);
        grandparent.Children.Add(parent);
        parent.Children.Add(subject);

        Assert.False(MatchesDirectAndThroughThePass(selector, greatGrandparentA, subject));
    }

    [Fact]
    public void AChainNeedsTheSubject()
    {
        ClassId a = ClassName.Intern(nameof(AChainNeedsTheSubject) + ".a");
        ClassId b = ClassName.Intern(nameof(AChainNeedsTheSubject) + ".b");
        ClassId c = ClassName.Intern(nameof(AChainNeedsTheSubject) + ".c");
        CompoundSelector ancestor = new() { Class = a };
        CompoundSelector subject = new() { Class = b };

        Element childRoot = NewElement(a);
        Element childOther = NewElement(c);
        childRoot.Children.Add(childOther);

        Element descendantRoot = NewElement(a);
        Element descendantParent = NewElement(a);
        Element descendantOther = NewElement(c);
        descendantRoot.Children.Add(descendantParent);
        descendantParent.Children.Add(descendantOther);

        Assert.False(MatchesDirectAndThroughThePass(Selector.Child(ancestor, subject), childRoot, childOther));
        Assert.False(MatchesDirectAndThroughThePass(Selector.Descendant(ancestor, subject), descendantRoot, descendantOther));
    }

    [Fact]
    public void ARootMatchesNoChain()
    {
        ClassId a = ClassName.Intern(nameof(ARootMatchesNoChain) + ".a");
        ClassId b = ClassName.Intern(nameof(ARootMatchesNoChain) + ".b");
        CompoundSelector ancestor = new() { Class = a };
        CompoundSelector subject = new() { Class = b };

        Element childRoot = new();
        childRoot.Classes.Add(a);
        childRoot.Classes.Add(b);
        Element descendantRoot = new();
        descendantRoot.Classes.Add(a);
        descendantRoot.Classes.Add(b);
        Element controlRoot = NewElement(b);

        Assert.Null(childRoot.Parent);
        Assert.False(MatchesDirectAndThroughThePass(Selector.Child(ancestor, subject), childRoot, childRoot));
        Assert.False(MatchesDirectAndThroughThePass(Selector.Descendant(ancestor, subject), descendantRoot, descendantRoot));

        // The subject alone matches the root, so only the chain misses.
        Assert.True(MatchesDirectAndThroughThePass(Selector.For(subject), controlRoot, controlRoot));
    }

    [Fact]
    public void AnAncestorStateRuleMatchesOnlyInTheState()
    {
        ClassId a = ClassName.Intern(nameof(AnAncestorStateRuleMatchesOnlyInTheState) + ".a");
        ClassId b = ClassName.Intern(nameof(AnAncestorStateRuleMatchesOnlyInTheState) + ".b");
        Selector selector = Selector.Child(
            new CompoundSelector { Class = a, States = PseudoStates.Hover },
            new CompoundSelector { Class = b });

        Element idleParent = NewElement(a);
        Element idleSubject = NewElement(b);
        idleParent.Children.Add(idleSubject);

        Element hoverParent = NewElement(a);
        hoverParent.States = PseudoStates.Hover | PseudoStates.Focus;
        Element hoverSubject = NewElement(b);
        hoverParent.Children.Add(hoverSubject);

        // The subject in the state does not give the state to the parent.
        Element subjectHoverParent = NewElement(a);
        Element subjectHover = NewElement(b);
        subjectHover.States = PseudoStates.Hover;
        subjectHoverParent.Children.Add(subjectHover);

        bool idle = MatchesDirectAndThroughThePass(selector, idleParent, idleSubject);
        bool hover = MatchesDirectAndThroughThePass(selector, hoverParent, hoverSubject);
        bool subjectOnly = MatchesDirectAndThroughThePass(selector, subjectHoverParent, subjectHover);
        TestContext.Current.TestOutputHelper?.WriteLine("idle " + idleSubject.Style.BackgroundColor);
        TestContext.Current.TestOutputHelper?.WriteLine("hover " + hoverSubject.Style.BackgroundColor);

        Assert.False(idle);
        Assert.True(hover);
        Assert.False(subjectOnly);
        Assert.Equal(Red, hoverSubject.Style.BackgroundColor);
        Assert.Equal(default, idleSubject.Style.BackgroundColor);
    }

    [Fact]
    public void ATypeAncestorReadsTheTypeOfTheAncestor()
    {
        ClassId b = ClassName.Intern(nameof(ATypeAncestorReadsTheTypeOfTheAncestor) + ".b");
        Selector selector = Selector.Child(
            new CompoundSelector { Type = TypedLabel.LabelTypeId },
            new CompoundSelector { Class = b });

        TypedLabel labelParent = new();
        Element subjectOfLabel = NewElement(b);
        labelParent.Children.Add(subjectOfLabel);

        Element plainParent = new();
        Element subjectOfPlain = NewElement(b);
        plainParent.Children.Add(subjectOfPlain);

        // The subject has the type, but the parent does not.
        Element typedSubjectParent = new();
        TypedLabel typedSubject = new();
        typedSubject.Classes.Add(b);
        typedSubjectParent.Children.Add(typedSubject);

        Assert.True(MatchesDirectAndThroughThePass(selector, labelParent, subjectOfLabel));
        Assert.False(MatchesDirectAndThroughThePass(selector, plainParent, subjectOfPlain));
        Assert.False(MatchesDirectAndThroughThePass(selector, typedSubjectParent, typedSubject));
    }

    [Fact]
    public void AnAncestorWithNoTypePartReadsNoTypeId()
    {
        ClassId a = ClassName.Intern(nameof(AnAncestorWithNoTypePartReadsNoTypeId) + ".a");
        ClassId b = ClassName.Intern(nameof(AnAncestorWithNoTypePartReadsNoTypeId) + ".b");
        CountingElement grandparent = new();
        CountingElement parent = new();
        Element subject = NewElement(b);
        grandparent.Classes.Add(a);
        grandparent.Children.Add(parent);
        parent.Children.Add(subject);

        Selector classChain = Selector.Descendant(new CompoundSelector { Class = a }, new CompoundSelector { Class = b });
        Selector typeChain = Selector.Descendant(
            new CompoundSelector { Type = grandparent.TypeId },
            new CompoundSelector { Class = b });
        grandparent.Reads = 0;
        parent.Reads = 0;

        Assert.True(classChain.Matches(subject, ElementTypeId.None));
        Assert.Equal(0, parent.Reads + grandparent.Reads);

        Assert.True(typeChain.Matches(subject, ElementTypeId.None));
        Assert.Equal(1, parent.Reads);
        Assert.Equal(0, grandparent.Reads);
    }

    [Fact]
    public void TheChainSpecificitySumsBothCompounds()
    {
        ClassId a = ClassName.Intern(nameof(TheChainSpecificitySumsBothCompounds) + ".a");
        ClassId b = ClassName.Intern(nameof(TheChainSpecificitySumsBothCompounds) + ".b");
        const PseudoStates allStates =
            PseudoStates.Hover | PseudoStates.Active | PseudoStates.Focus | PseudoStates.Disabled | PseudoStates.Checked;

        Selector child = Selector.Child(new CompoundSelector { Class = a }, new CompoundSelector { Class = b });
        Selector stateDescendant = Selector.Descendant(
            new CompoundSelector { Class = a, States = PseudoStates.Hover },
            new CompoundSelector { Class = b, States = PseudoStates.Focus });
        Selector typeDescendant = Selector.Descendant(
            new CompoundSelector { Type = TypedLabel.LabelTypeId },
            new CompoundSelector { Class = b });
        Selector largest = Selector.Descendant(
            new CompoundSelector { Class = a, States = allStates },
            new CompoundSelector { Class = b, States = allStates });
        Selector twoTypes = Selector.Child(
            new CompoundSelector { Type = TypedLabel.LabelTypeId, Class = a },
            new CompoundSelector { Type = TypedLabel.LabelTypeId, Class = b });

        Assert.Equal(512, child.Specificity);
        Assert.Equal(1024, stateDescendant.Specificity);
        Assert.Equal(257, typeDescendant.Specificity);
        Assert.Equal(3072, largest.Specificity);
        Assert.Equal(514, twoTypes.Specificity);

        // The combinator does not change the weight.
        Assert.Equal(
            Selector.Child(new CompoundSelector { Class = a }, new CompoundSelector { Class = b }).Specificity,
            Selector.Descendant(new CompoundSelector { Class = a }, new CompoundSelector { Class = b }).Specificity);
    }

    [Fact]
    public void ChildAndDescendantThrowForAnEmptySlot()
    {
        ClassId a = ClassName.Intern(nameof(ChildAndDescendantThrowForAnEmptySlot) + ".a");
        CompoundSelector real = new() { Class = a };
        CompoundSelector unknownState = new() { Class = a, States = (PseudoStates)32 };

        Assert.Throws<ArgumentException>("parent", () => Selector.Child(default, real));
        Assert.Throws<ArgumentException>("subject", () => Selector.Child(real, default));
        Assert.Throws<ArgumentException>("parent", () => Selector.Child(default, default));
        Assert.Throws<ArgumentException>("ancestor", () => Selector.Descendant(default, real));
        Assert.Throws<ArgumentException>("subject", () => Selector.Descendant(real, default));
        Assert.Throws<ArgumentException>("ancestor", () => Selector.Descendant(default, default));

        // A state bit outside the five members throws in each slot.
        Assert.Throws<ArgumentException>("parent", () => Selector.Child(unknownState, real));
        Assert.Throws<ArgumentException>("subject", () => Selector.Child(real, unknownState));
        Assert.Throws<ArgumentException>("ancestor", () => Selector.Descendant(unknownState, real));
        Assert.Throws<ArgumentException>("subject", () => Selector.Descendant(real, unknownState));
    }

    [Fact]
    public void TheBuilderAcceptsEachChainForm()
    {
        ClassId a = ClassName.Intern(nameof(TheBuilderAcceptsEachChainForm) + ".a");
        ClassId b = ClassName.Intern(nameof(TheBuilderAcceptsEachChainForm) + ".b");
        CompoundSelector ancestor = new() { Class = a, States = PseudoStates.Hover };
        CompoundSelector subject = new() { Class = b };

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.Descendant(ancestor, subject), new StyleProperties { FontSize = 3f })
            .Add(Selector.Child(new CompoundSelector { Class = a }, subject), new StyleProperties { FontSize = 2f })
            .Add(Selector.For(subject), new StyleProperties { FontSize = 1f })
            .Build();

        Assert.Equal(3, sheet.Rules.Length);
        Assert.Equal(SelectorCombinator.None, sheet.Rules[0].Selector.Combinator);
        Assert.Equal(SelectorCombinator.Child, sheet.Rules[1].Selector.Combinator);
        Assert.Equal(SelectorCombinator.Descendant, sheet.Rules[2].Selector.Combinator);
        Assert.Equal(256, sheet.Rules[0].Specificity);
        Assert.Equal(512, sheet.Rules[1].Specificity);
        Assert.Equal(768, sheet.Rules[2].Specificity);

        Selector descendant = sheet.Rules[2].Selector;
        Assert.Equal(a, descendant.Ancestor.Class);
        Assert.Equal(PseudoStates.Hover, descendant.Ancestor.States);
        Assert.Equal(b, descendant.Subject.Class);
        Assert.Equal(b, descendant.Class);
        Assert.Equal(ElementTypeId.None, descendant.Type);

        // A selector with one compound and the default selector have no chain.
        Assert.True(sheet.Rules[0].Selector.Ancestor.IsEmpty);
        Assert.Equal(SelectorCombinator.None, default(Selector).Combinator);
        Assert.True(default(Selector).Ancestor.IsEmpty);
        Assert.False(default(Selector).Matches(new Element()));
    }

    private static Element NewElement(ClassId id)
    {
        Element element = new();
        element.Classes.Add(id);
        return element;
    }

    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
    }

    /// <summary>
    /// Checks the selector on the subject two times: one time with the direct match, and one time through a sheet and
    /// the style pass. The background color does not inherit, so only a matching rule sets it.
    /// </summary>
    private static bool MatchesDirectAndThroughThePass(in Selector selector, Element root, Element subject)
    {
        bool direct = selector.Matches(subject);

        StyleSheet sheet = new StyleSheet.Builder()
            .Add(selector, new StyleProperties { BackgroundColor = Red })
            .Build();
        UiContext context = NewContext();
        context.AddRoot(root);
        context.AddSheet(sheet);
        context.Update(0f);
        bool styled = subject.Style.BackgroundColor == Red;

        Assert.Equal(direct, styled);
        return direct;
    }

    /// <summary>A subclass of <see cref="Element"/> with its own type id, in the role of <c>Label</c>.</summary>
    private sealed class TypedLabel : Element
    {
        public static readonly ElementTypeId LabelTypeId =
            ElementType.Intern(nameof(ATypeAncestorReadsTheTypeOfTheAncestor) + ".Label");

        public override ElementTypeId TypeId => LabelTypeId;
    }

    /// <summary>A subclass of <see cref="Element"/> that counts each read of its type id.</summary>
    private sealed class CountingElement : Element
    {
        private static readonly ElementTypeId s_typeId =
            ElementType.Intern(nameof(AnAncestorWithNoTypePartReadsNoTypeId) + ".Counting");

        public int Reads { get; set; }

        public override ElementTypeId TypeId
        {
            get
            {
                Reads++;
                return s_typeId;
            }
        }
    }
}
