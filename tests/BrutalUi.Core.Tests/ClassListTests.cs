using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the class table and the class list: the intern, the inline set, the pooled overflow, and the style mark.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel.
/// Each test uses names with a unique prefix, and no test asserts a fixed id value.
/// </remarks>
public sealed class ClassListTests
{
    [Fact]
    public void InternOfTheSameNameGivesTheSameId()
    {
        ClassId first = ClassName.Intern(nameof(InternOfTheSameNameGivesTheSameId) + ".a");
        ClassId second = ClassName.Intern(nameof(InternOfTheSameNameGivesTheSameId) + ".a");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void InternOfTwoNamesGivesTwoIds()
    {
        ClassId first = ClassName.Intern(nameof(InternOfTwoNamesGivesTwoIds) + ".a");
        ClassId second = ClassName.Intern(nameof(InternOfTwoNamesGivesTwoIds) + ".b");

        Assert.NotEqual(first, second);
        Assert.False(first.IsNone);
        Assert.False(second.IsNone);
    }

    [Fact]
    public void InternComparesNamesWithTheOrdinalRule()
    {
        ClassId lower = ClassName.Intern(nameof(InternComparesNamesWithTheOrdinalRule) + ".abc");
        ClassId upper = ClassName.Intern(nameof(InternComparesNamesWithTheOrdinalRule) + ".ABC");

        Assert.NotEqual(lower, upper);
    }

    [Fact]
    public void GetNameReturnsTheInternedName()
    {
        string name = nameof(GetNameReturnsTheInternedName) + ".card";
        ClassId id = ClassName.Intern(name);

        Assert.Equal(name, ClassName.GetName(id));
        Assert.Equal(name, id.ToString());
    }

    [Fact]
    public void GetNameThrowsForAnIdThatTheTableDoesNotHold()
    {
        ArgumentOutOfRangeException none = Assert.Throws<ArgumentOutOfRangeException>(() => ClassName.GetName(ClassId.None));
        ArgumentOutOfRangeException max = Assert.Throws<ArgumentOutOfRangeException>(() => ClassName.GetName(new ClassId(int.MaxValue)));
        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => ClassName.GetName(new ClassId(-1)));

        Assert.Equal("id", none.ParamName);
        Assert.Equal("id", max.ParamName);
        Assert.Equal("id", negative.ParamName);
    }

    [Fact]
    public void NoneIsTheDefaultValue()
    {
        ClassId none = ClassId.None;

        Assert.Equal(default, none);
        Assert.True(none.IsNone);
        Assert.Equal(0, none.Value);
        Assert.Equal("(none)", none.ToString());
        Assert.False(ClassName.Intern(nameof(NoneIsTheDefaultValue) + ".x").IsNone);
    }

    [Fact]
    public async Task FourThreadsThatInternTheSameNamesGetOneIdForEachName()
    {
        const int threadCount = 4;
        const int nameCount = 100;
        string prefix = nameof(FourThreadsThatInternTheSameNamesGetOneIdForEachName) + ".";
        ClassId[][] results = new ClassId[threadCount][];
        using Barrier barrier = new(threadCount);
        Task[] tasks = new Task[threadCount];
        CancellationToken token = TestContext.Current.CancellationToken;

        for (int t = 0; t < threadCount; t++)
        {
            int thread = t;
            results[thread] = new ClassId[nameCount];
            tasks[thread] = Task.Factory.StartNew(
                () =>
                {
                    barrier.SignalAndWait(token);
                    for (int i = 0; i < nameCount; i++)
                    {
                        results[thread][i] = ClassName.Intern(prefix + i);
                    }
                },
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        await Task.WhenAll(tasks);

        for (int i = 0; i < nameCount; i++)
        {
            for (int t = 1; t < threadCount; t++)
            {
                Assert.Equal(results[0][i], results[t][i]);
            }

            Assert.Equal(prefix + i, ClassName.GetName(results[0][i]));
        }

        // The 100 names must give 100 different ids.
        System.Collections.Generic.HashSet<ClassId> distinct = [.. results[0]];
        Assert.Equal(nameCount, distinct.Count);
    }

    [Fact]
    public void AddContainsAndRemove()
    {
        Element element = new();
        ClassId a = Id(nameof(AddContainsAndRemove), "a");
        ClassId b = Id(nameof(AddContainsAndRemove), "b");

        Assert.Equal(0, element.Classes.Count);
        Assert.False(element.Classes.Contains(a));

        Assert.True(element.Classes.Add(a));
        Assert.True(element.Classes.Add(b));

        Assert.Equal(2, element.Classes.Count);
        Assert.True(element.Classes.Contains(a));
        Assert.True(element.Classes.Contains(b));
        Assert.Equal(a, element.Classes[0]);
        Assert.Equal(b, element.Classes[1]);

        Assert.True(element.Classes.Remove(a));

        Assert.Equal(1, element.Classes.Count);
        Assert.False(element.Classes.Contains(a));
        Assert.True(element.Classes.Contains(b));
        Assert.Equal(b, element.Classes[0]);
    }

    [Fact]
    public void AddOfADuplicateReturnsFalseAndMarksNothing()
    {
        Element element = new();
        ClassId a = Id(nameof(AddOfADuplicateReturnsFalseAndMarksNothing), "a");
        Assert.True(element.Classes.Add(a));
        CleanTree(element);

        bool changed = element.Classes.Add(a);

        Assert.False(changed);
        Assert.Equal(1, element.Classes.Count);
        Assert.Equal(DirtyFlags.None, element.Dirty);
    }

    [Fact]
    public void AddMarksTheOwnerForStyleAndTheAncestorsForDescendantStyle()
    {
        Element root = new();
        Element child = new();
        root.Children.Add(child);
        CleanTree(root);

        child.Classes.Add(Id(nameof(AddMarksTheOwnerForStyleAndTheAncestorsForDescendantStyle), "a"));

        Assert.Equal(DirtyFlags.Style, child.Dirty);
        Assert.Equal(DirtyFlags.DescendantStyle, root.Dirty);
    }

    [Fact]
    public void RemoveMarksTheOwnerForStyle()
    {
        Element element = new();
        ClassId a = Id(nameof(RemoveMarksTheOwnerForStyle), "a");
        element.Classes.Add(a);
        CleanTree(element);

        bool changed = element.Classes.Remove(a);

        Assert.True(changed);
        Assert.Equal(DirtyFlags.Style, element.Dirty);
    }

    [Fact]
    public void RemoveOfAnAbsentIdReturnsFalseAndMarksNothing()
    {
        Element element = new();
        ClassId a = Id(nameof(RemoveOfAnAbsentIdReturnsFalseAndMarksNothing), "a");
        ClassId b = Id(nameof(RemoveOfAnAbsentIdReturnsFalseAndMarksNothing), "b");
        element.Classes.Add(a);
        CleanTree(element);

        Assert.False(element.Classes.Remove(b));
        Assert.False(element.Classes.Remove(ClassId.None));

        Assert.Equal(1, element.Classes.Count);
        Assert.Equal(DirtyFlags.None, element.Dirty);
    }

    [Fact]
    public void RemoveFromAnEmptyListReturnsFalse()
    {
        Element element = new();

        Assert.False(element.Classes.Remove(Id(nameof(RemoveFromAnEmptyListReturnsFalse), "a")));
        Assert.Equal(0, element.Classes.Count);
    }

    [Fact]
    public void EightAddsStayInlineAndKeepTheOrder()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(EightAddsStayInlineAndKeepTheOrder), 8);

        foreach (ClassId id in ids)
        {
            Assert.True(element.Classes.Add(id));
        }

        AssertOrder(element, ids);
    }

    [Fact]
    public void NineAddsKeepAllNineInOrder()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(NineAddsKeepAllNineInOrder), 9);

        foreach (ClassId id in ids)
        {
            Assert.True(element.Classes.Add(id));
        }

        AssertOrder(element, ids);
        foreach (ClassId id in ids)
        {
            Assert.True(element.Classes.Contains(id));
        }
    }

    [Fact]
    public void ADuplicateAddInTheOverflowReturnsFalse()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(ADuplicateAddInTheOverflowReturnsFalse), 10);
        foreach (ClassId id in ids)
        {
            element.Classes.Add(id);
        }

        Assert.False(element.Classes.Add(ids[0]));
        Assert.False(element.Classes.Add(ids[9]));

        AssertOrder(element, ids);
    }

    [Fact]
    public void FortyAddsGrowTheArrayTwiceAndKeepTheOrder()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(FortyAddsGrowTheArrayTwiceAndKeepTheOrder), 40);

        foreach (ClassId id in ids)
        {
            Assert.True(element.Classes.Add(id));
        }

        AssertOrder(element, ids);
    }

    [Fact]
    public void RemoveFromTheOverflowKeepsTheOrder()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(RemoveFromTheOverflowKeepsTheOrder), 12);
        foreach (ClassId id in ids)
        {
            element.Classes.Add(id);
        }

        // Remove the first, a middle id, and the last id.
        Assert.True(element.Classes.Remove(ids[0]));
        Assert.True(element.Classes.Remove(ids[6]));
        Assert.True(element.Classes.Remove(ids[11]));

        ClassId[] expected =
        [
            ids[1], ids[2], ids[3], ids[4], ids[5], ids[7], ids[8], ids[9], ids[10],
        ];
        AssertOrder(element, expected);
        Assert.False(element.Classes.Contains(ids[6]));
    }

    [Fact]
    public void RemoveFromTheInlineBufferKeepsTheOrder()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(RemoveFromTheInlineBufferKeepsTheOrder), 6);
        foreach (ClassId id in ids)
        {
            element.Classes.Add(id);
        }

        Assert.True(element.Classes.Remove(ids[2]));
        Assert.True(element.Classes.Remove(ids[5]));

        AssertOrder(element, [ids[0], ids[1], ids[3], ids[4]]);
    }

    [Fact]
    public void RemoveDownToThreeAndAddBackToTwelveKeepsTheOrder()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(RemoveDownToThreeAndAddBackToTwelveKeepsTheOrder), 20);
        for (int i = 0; i < 12; i++)
        {
            element.Classes.Add(ids[i]);
        }

        // Remove the ids 3 to 11, so the ids 0, 1, and 2 stay. The set is now below 8 on the array.
        for (int i = 11; i >= 3; i--)
        {
            Assert.True(element.Classes.Remove(ids[i]));
        }

        AssertOrder(element, [ids[0], ids[1], ids[2]]);

        // Add 9 new ids to reach 12 again.
        for (int i = 12; i < 20; i++)
        {
            Assert.True(element.Classes.Add(ids[i]));
        }

        Assert.True(element.Classes.Add(ids[3]));

        ClassId[] expected =
        [
            ids[0], ids[1], ids[2], ids[12], ids[13], ids[14], ids[15], ids[16], ids[17], ids[18], ids[19], ids[3],
        ];
        AssertOrder(element, expected);
    }

    [Fact]
    public void ARemoveAndAnAddBackMoveTheIdToTheEnd()
    {
        Element element = new();
        ClassId[] ids = Ids(nameof(ARemoveAndAnAddBackMoveTheIdToTheEnd), 10);
        foreach (ClassId id in ids)
        {
            element.Classes.Add(id);
        }

        Assert.True(element.Classes.Remove(ids[4]));
        Assert.True(element.Classes.Add(ids[4]));

        AssertOrder(element, [ids[0], ids[1], ids[2], ids[3], ids[5], ids[6], ids[7], ids[8], ids[9], ids[4]]);
    }

    [Fact]
    public void ClassesOfTwoElementsStaySeparate()
    {
        Element first = new();
        Element second = new();
        ClassId a = Id(nameof(ClassesOfTwoElementsStaySeparate), "a");
        ClassId b = Id(nameof(ClassesOfTwoElementsStaySeparate), "b");

        first.Classes.Add(a);
        second.Classes.Add(b);

        Assert.True(first.Classes.Contains(a));
        Assert.False(first.Classes.Contains(b));
        Assert.True(second.Classes.Contains(b));
        Assert.False(second.Classes.Contains(a));
    }

    [Fact]
    public void ADefaultClassIdIsEqualToNone()
    {
        ClassId id = default;

        Assert.True(id == ClassId.None);
        Assert.True(id.Equals(ClassId.None));
        Assert.True(id.Equals((object)ClassId.None));
        Assert.False(id.Equals((object)"none"));
    }

    private static ClassId Id(string test, string suffix)
    {
        return ClassName.Intern(test + "." + suffix);
    }

    private static ClassId[] Ids(string test, int count)
    {
        ClassId[] ids = new ClassId[count];
        for (int i = 0; i < count; i++)
        {
            ids[i] = ClassName.Intern(test + "." + i);
        }

        return ids;
    }

    private static void AssertOrder(Element element, ClassId[] expected)
    {
        Assert.Equal(expected.Length, element.Classes.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], element.Classes[i]);
        }
    }

    /// <summary>Clears each bit with an update of a new context, and then removes the root from it.</summary>
    private static void CleanTree(Element root)
    {
        UiContext context = new();
        context.AddRoot(root);
        context.Update(0f);
        context.RemoveRoot(root);
        Assert.Equal(DirtyFlags.None, root.Dirty);
    }
}
