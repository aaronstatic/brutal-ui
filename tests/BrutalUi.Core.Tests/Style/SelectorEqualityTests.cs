using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the equality members and the CSS text form of <see cref="CompoundSelector"/> and
/// <see cref="Selector"/>: equal and not equal for each part and each combinator, the ordinal name compare, the hash
/// codes, the operators, the text of each form of the specificity table, the fallback text, and the allocation gate of
/// the equality.
/// </summary>
/// <remarks>
/// The class table and the type table are global and xUnit runs test classes in parallel. Each test interns its names
/// with the prefix <c>nameof(Test) + "-"</c>. The type <c>Label</c> is the real type name of the element, and an intern
/// of it gives the same id in each test.
/// </remarks>
public sealed class SelectorEqualityTests
{
    /// <summary>The value of a state bit that is not a member of <see cref="PseudoStates"/>.</summary>
    private const PseudoStates UnknownState = (PseudoStates)32;

    [Fact]
    public void CompoundsWithEqualPartsAreEqual()
    {
        const string Prefix = nameof(CompoundsWithEqualPartsAreEqual) + "-";
        ElementTypeId type = ElementType.Intern(Prefix + "Type");
        ClassId id = ClassName.Intern(Prefix + "a");

        CompoundSelector left = new() { Type = type, Class = id, States = PseudoStates.Hover | PseudoStates.Checked, Name = "title" };
        CompoundSelector right = new() { Type = type, Class = id, States = PseudoStates.Checked | PseudoStates.Hover, Name = "title" };

        Assert.True(left.Equals(right));
        Assert.True(right.Equals(left));
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Theory]
    [InlineData("type")]
    [InlineData("type-none")]
    [InlineData("class")]
    [InlineData("class-none")]
    [InlineData("states")]
    [InlineData("states-none")]
    [InlineData("name")]
    [InlineData("name-case")]
    [InlineData("name-null")]
    public void EachPartMakesTwoCompoundsDiffer(string part)
    {
        const string Prefix = nameof(EachPartMakesTwoCompoundsDiffer) + "-";
        ElementTypeId type = ElementType.Intern(Prefix + "Type");
        ElementTypeId otherType = ElementType.Intern(Prefix + "Other");
        ClassId id = ClassName.Intern(Prefix + "a");
        ClassId otherId = ClassName.Intern(Prefix + "b");

        CompoundSelector left = new() { Type = type, Class = id, States = PseudoStates.Hover, Name = "title" };
        CompoundSelector right = part switch
        {
            "type" => left with { Type = otherType },
            "type-none" => left with { Type = ElementTypeId.None },
            "class" => left with { Class = otherId },
            "class-none" => left with { Class = ClassId.None },
            "states" => left with { States = PseudoStates.Hover | PseudoStates.Focus },
            "states-none" => left with { States = PseudoStates.None },
            "name" => left with { Name = "score" },
            "name-case" => left with { Name = "Title" },
            "name-null" => left with { Name = null },
            _ => throw new ArgumentOutOfRangeException(nameof(part), part, "The test has no such part."),
        };

        Assert.False(left.Equals(right));
        Assert.False(right.Equals(left));
        Assert.False(left == right);
        Assert.True(left != right);
        Assert.NotEqual(left, right);

        // The selectors of the two compounds differ also.
        Assert.False(Selector.For(left) == Selector.For(right));
        Assert.NotEqual(Selector.For(left), Selector.For(right));
    }

    [Fact]
    public void TwoNameInstancesWithTheSameTextAreEqual()
    {
        string first = "title";
        string second = new(['t', 'i', 't', 'l', 'e']);
        Assert.False(ReferenceEquals(first, second));

        CompoundSelector left = new() { Name = first };
        CompoundSelector right = new() { Name = second };

        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.True(Selector.ForName(first) == Selector.ForName(second));
        Assert.Equal(Selector.ForName(first).GetHashCode(), Selector.ForName(second).GetHashCode());
    }

    [Fact]
    public void EmptyCompoundsAreEqual()
    {
        Assert.True(default(CompoundSelector) == new CompoundSelector());
        Assert.Equal(default(CompoundSelector).GetHashCode(), new CompoundSelector().GetHashCode());
        Assert.False(default(CompoundSelector) == new CompoundSelector { Name = "x" });
    }

    [Fact]
    public void SelectorsWithEqualPartsAreEqual()
    {
        const string Prefix = nameof(SelectorsWithEqualPartsAreEqual) + "-";
        ClassId a = ClassName.Intern(Prefix + "a");
        ClassId b = ClassName.Intern(Prefix + "b");
        CompoundSelector ancestor = new() { Class = a, States = PseudoStates.Hover };
        CompoundSelector subject = new() { Class = b, Name = "cell" };

        Selector[] lefts =
        [
            Selector.For(subject),
            Selector.Child(ancestor, subject),
            Selector.Descendant(ancestor, subject),
            Selector.ForClass(a),
            default,
        ];
        Selector[] rights =
        [
            Selector.For(new CompoundSelector { Class = b, Name = "cell" }),
            Selector.Child(new CompoundSelector { Class = a, States = PseudoStates.Hover }, subject),
            Selector.Descendant(ancestor, new CompoundSelector { Class = b, Name = "cell" }),
            Selector.For(new CompoundSelector { Class = a }),
            default,
        ];

        for (int i = 0; i < lefts.Length; i++)
        {
            Assert.True(lefts[i].Equals(rights[i]));
            Assert.True(lefts[i] == rights[i]);
            Assert.False(lefts[i] != rights[i]);
            Assert.Equal(lefts[i], rights[i]);
            Assert.Equal(lefts[i].GetHashCode(), rights[i].GetHashCode());
        }
    }

    [Fact]
    public void TheFactoryShortcutsGiveEqualSelectors()
    {
        const string Prefix = nameof(TheFactoryShortcutsGiveEqualSelectors) + "-";
        ElementTypeId type = ElementType.Intern(Prefix + "Type");
        ClassId id = ClassName.Intern(Prefix + "a");

        Assert.Equal(Selector.For(new CompoundSelector { Class = id }), Selector.ForClass(id));
        Assert.Equal(Selector.For(new CompoundSelector { Type = type }), Selector.ForType(type));
        Assert.Equal(Selector.For(new CompoundSelector { Name = "title" }), Selector.ForName("title"));
        Assert.Equal(Selector.For(new CompoundSelector { Type = type, Class = id }), Selector.ForTypeAndClass(type, id));
        Assert.Equal(Selector.ForClass(id), Selector.ForTypeAndClass(ElementTypeId.None, id));
    }

    [Theory]
    [InlineData("one-child")]
    [InlineData("one-descendant")]
    [InlineData("child-descendant")]
    [InlineData("child-ancestor")]
    [InlineData("child-subject")]
    [InlineData("descendant-ancestor")]
    [InlineData("descendant-subject")]
    [InlineData("swap")]
    [InlineData("default-one")]
    public void EachCombinatorAndEachCompoundMakesTwoSelectorsDiffer(string pair)
    {
        const string Prefix = nameof(EachCombinatorAndEachCompoundMakesTwoSelectorsDiffer) + "-";
        CompoundSelector a = new() { Class = ClassName.Intern(Prefix + "a") };
        CompoundSelector b = new() { Class = ClassName.Intern(Prefix + "b") };
        CompoundSelector c = new() { Class = ClassName.Intern(Prefix + "c") };

        (Selector left, Selector right) = pair switch
        {
            "one-child" => (Selector.For(b), Selector.Child(a, b)),
            "one-descendant" => (Selector.For(b), Selector.Descendant(a, b)),
            "child-descendant" => (Selector.Child(a, b), Selector.Descendant(a, b)),
            "child-ancestor" => (Selector.Child(a, b), Selector.Child(c, b)),
            "child-subject" => (Selector.Child(a, b), Selector.Child(a, c)),
            "descendant-ancestor" => (Selector.Descendant(a, b), Selector.Descendant(c, b)),
            "descendant-subject" => (Selector.Descendant(a, b), Selector.Descendant(a, c)),
            "swap" => (Selector.Child(a, b), Selector.Child(b, a)),
            "default-one" => (default(Selector), Selector.For(a)),
            _ => throw new ArgumentOutOfRangeException(nameof(pair), pair, "The test has no such pair."),
        };

        Assert.False(left.Equals(right));
        Assert.False(right.Equals(left));
        Assert.False(left == right);
        Assert.True(left != right);
        Assert.NotEqual(left, right);
    }

    [Fact]
    public void EqualsWithAnObjectChecksTheType()
    {
        const string Prefix = nameof(EqualsWithAnObjectChecksTheType) + "-";
        ClassId id = ClassName.Intern(Prefix + "a");
        Selector selector = Selector.ForClass(id);
        CompoundSelector compound = new() { Class = id };

        Assert.True(selector.Equals((object)Selector.ForClass(id)));
        Assert.False(selector.Equals((object)Selector.ForName("title")));
        Assert.False(selector.Equals((object)compound));
        Assert.False(selector.Equals(null));
        Assert.True(compound.Equals((object)new CompoundSelector { Class = id }));
        Assert.False(compound.Equals((object)selector));
        Assert.False(compound.Equals(null));
    }

    [Theory]
    [InlineData("Label", "Label", 1)]
    [InlineData(".a", ".{p}a", 256)]
    [InlineData(":hover", ":hover", 256)]
    [InlineData("Label.a", "Label.{p}a", 257)]
    [InlineData(".a:hover", ".{p}a:hover", 512)]
    [InlineData("#title", "#title", 65536)]
    [InlineData(".a > .b", ".{p}a > .{p}b", 512)]
    [InlineData(".a .b", ".{p}a .{p}b", 512)]
    [InlineData("Label .b", "Label .{p}b", 257)]
    [InlineData(".a:hover .b:focus", ".{p}a:hover .{p}b:focus", 1024)]
    [InlineData(".a:hover > .b", ".{p}a:hover > .{p}b", 768)]
    [InlineData("Label.title#score:hover", "Label.{p}title#score:hover", 66049)]
    [InlineData("all-states", ":hover:active:focus:disabled:checked", 1280)]
    [InlineData("all-parts", "Label.{p}a#title:hover:active:focus:disabled:checked", 67073)]
    [InlineData("(empty)", "(empty)", 0)]
    public void ToStringGivesTheCssTextOfEachForm(string form, string expectedTemplate, int specificity)
    {
        const string Prefix = nameof(ToStringGivesTheCssTextOfEachForm) + "-";
        ElementTypeId label = ElementType.Intern("Label");
        ClassId a = ClassName.Intern(Prefix + "a");
        ClassId b = ClassName.Intern(Prefix + "b");
        ClassId title = ClassName.Intern(Prefix + "title");
        const PseudoStates AllStates =
            PseudoStates.Checked | PseudoStates.Disabled | PseudoStates.Focus | PseudoStates.Active | PseudoStates.Hover;

        Selector selector = form switch
        {
            "Label" => Selector.ForType(label),
            ".a" => Selector.ForClass(a),
            ":hover" => Selector.For(new CompoundSelector { States = PseudoStates.Hover }),
            "Label.a" => Selector.ForTypeAndClass(label, a),
            ".a:hover" => Selector.For(new CompoundSelector { Class = a, States = PseudoStates.Hover }),
            "#title" => Selector.ForName("title"),
            ".a > .b" => Selector.Child(new CompoundSelector { Class = a }, new CompoundSelector { Class = b }),
            ".a .b" => Selector.Descendant(new CompoundSelector { Class = a }, new CompoundSelector { Class = b }),
            "Label .b" => Selector.Descendant(new CompoundSelector { Type = label }, new CompoundSelector { Class = b }),
            ".a:hover .b:focus" => Selector.Descendant(
                new CompoundSelector { Class = a, States = PseudoStates.Hover },
                new CompoundSelector { Class = b, States = PseudoStates.Focus }),
            ".a:hover > .b" => Selector.Child(
                new CompoundSelector { Class = a, States = PseudoStates.Hover },
                new CompoundSelector { Class = b }),
            "Label.title#score:hover" => Selector.For(
                new CompoundSelector { Type = label, Class = title, Name = "score", States = PseudoStates.Hover }),
            "all-states" => Selector.For(new CompoundSelector { States = AllStates }),
            "all-parts" => Selector.For(new CompoundSelector { Type = label, Class = a, Name = "title", States = AllStates }),
            "(empty)" => default,
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, "The test has no such form."),
        };

        string expected = expectedTemplate.Replace("{p}", Prefix, StringComparison.Ordinal);
        TestContext.Current.TestOutputHelper?.WriteLine(selector.ToString());

        Assert.Equal(expected, selector.ToString());
        Assert.Equal(specificity, selector.Specificity);
    }

    [Fact]
    public void ACompoundGivesItsOwnText()
    {
        const string Prefix = nameof(ACompoundGivesItsOwnText) + "-";
        ClassId a = ClassName.Intern(Prefix + "a");

        Assert.Equal(string.Empty, default(CompoundSelector).ToString());
        Assert.Equal("." + Prefix + "a:focus", new CompoundSelector { Class = a, States = PseudoStates.Focus }.ToString());
        Assert.Equal("#Title", new CompoundSelector { Name = "Title" }.ToString());
    }

    [Fact]
    public void AnIdOutsideTheTableGivesTheFallbackText()
    {
        const string Prefix = nameof(AnIdOutsideTheTableGivesTheFallbackText) + "-";
        ClassId b = ClassName.Intern(Prefix + "b");
        ElementTypeId unknownType = new(int.MaxValue);
        ClassId unknownClass = new(int.MaxValue);

        CompoundSelector typeOnly = new() { Type = unknownType };
        CompoundSelector both = new() { Type = unknownType, Class = unknownClass, States = PseudoStates.Hover };

        Assert.Equal("<type 2147483647>", typeOnly.ToString());
        Assert.Equal(".<class 2147483647>", new CompoundSelector { Class = unknownClass }.ToString());
        Assert.Equal("<type 2147483647>.<class 2147483647>:hover", both.ToString());
        Assert.Equal(
            "<type 2147483647>.<class 2147483647>:hover > ." + Prefix + "b",
            Selector.Child(both, new CompoundSelector { Class = b }).ToString());
    }

    [Fact]
    public void AStateBitOutsideTheEnumGivesTheFallbackText()
    {
        const string Prefix = nameof(AStateBitOutsideTheEnumGivesTheFallbackText) + "-";
        ClassId a = ClassName.Intern(Prefix + "a");

        CompoundSelector unknownOnly = new() { Class = a, States = UnknownState };
        CompoundSelector mixed = new() { Class = a, States = UnknownState | PseudoStates.Checked | PseudoStates.Hover };

        Assert.Equal("." + Prefix + "a:<state 32>", unknownOnly.ToString());
        Assert.Equal("." + Prefix + "a:hover:checked:<state 32>", mixed.ToString());
    }

    [Fact]
    public void EqualsAllocatesNothing()
    {
        const string Prefix = nameof(EqualsAllocatesNothing) + "-";
        ClassId a = ClassName.Intern(Prefix + "a");
        ClassId b = ClassName.Intern(Prefix + "b");

        // Two string instances with the same text, so the ordinal compare reads each character.
        string name = "score";
        string sameName = new(['s', 'c', 'o', 'r', 'e']);
        Selector left = Selector.Child(
            new CompoundSelector { Class = a, States = PseudoStates.Hover, Name = "panel" },
            new CompoundSelector { Class = b, Name = name });
        Selector right = Selector.Child(
            new CompoundSelector { Class = a, States = PseudoStates.Hover, Name = new string(['p', 'a', 'n', 'e', 'l']) },
            new CompoundSelector { Class = b, Name = sameName });
        Selector other = Selector.Descendant(
            new CompoundSelector { Class = a, States = PseudoStates.Hover, Name = "panel" },
            new CompoundSelector { Class = b, Name = name });

        // Warm-up: the calls let the JIT compile each member.
        int count = Compare(in left, in right, in other, 16);
        Assert.Equal(16 * 4, count);

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        count = Compare(in left, in right, in other, 1000);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
        Assert.Equal(1000 * 4, count);
    }

    /// <summary>Calls each equality member a number of times and counts the expected results.</summary>
    /// <param name="left">A selector.</param>
    /// <param name="right">A selector equal to <paramref name="left"/>, with other string instances.</param>
    /// <param name="other">A selector not equal to <paramref name="left"/>.</param>
    /// <param name="calls">The number of calls of each member.</param>
    /// <returns>Four times <paramref name="calls"/> when each result is right.</returns>
    private static int Compare(in Selector left, in Selector right, in Selector other, int calls)
    {
        int count = 0;
        int hash = left.GetHashCode();
        for (int i = 0; i < calls; i++)
        {
            if (left.Equals(right) && left.Subject.Equals(right.Subject))
            {
                count++;
            }

            if (left == right && !(left == other))
            {
                count++;
            }

            if (left != other && !(left != right))
            {
                count++;
            }

            if (right.GetHashCode() == hash)
            {
                count++;
            }
        }

        return count;
    }
}
