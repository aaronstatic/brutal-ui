using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of phase 5 of the transitions: the lifetime of a tween. An element that leaves the tree drops its tweens,
/// an element that joins a tree starts no tween on its first style, and the pool reuses its records and never shrinks.
/// </summary>
/// <remarks>
/// <para>
/// The class table and the type table are global and xUnit runs test classes in parallel. Each test interns its names
/// with the prefix <c>nameof(Test) + "."</c>, and no test asserts a fixed id value.
/// </para>
/// <para>
/// The sheet is the sheet of phase 1: <c>.panel</c> sets the fill to #1E222C with a linear fill transition of 0.2 s,
/// and <c>.panel:hover</c> sets #3060C0. A step of 0.1 s gives the midpoint #274176.
/// </para>
/// </remarks>
public sealed class TransitionLifetimeTests
{
    private const float Duration = 0.2f;
    private const float StepTime = 0.1f;

    private static readonly Rgba8 Idle = new(30, 34, 44, 255);
    private static readonly Rgba8 Hovered = new(48, 96, 192, 255);
    private static readonly Rgba8 Midpoint = new(39, 65, 118, 255);

    [Fact]
    public void ARemovedPanelEndsItsTweens()
    {
        StyleSheet sheet = PanelSheet(nameof(ARemovedPanelEndsItsTweens), out ClassId panelClass);
        Element root = new();
        Element panel = NewPanel(panelClass);
        root.Children.Add(panel);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        panel.States = PseudoStates.Hover;
        context.Update(StepTime);
        string first = Line(context, panel);

        root.Children.Remove(panel);
        context.Update(StepTime);
        string second = Line(context, panel);

        root.Children.Add(panel);
        context.Update(0f);
        string third = Line(context, panel);

        TestContext.Current.TestOutputHelper?.WriteLine(first);
        TestContext.Current.TestOutputHelper?.WriteLine(second);
        TestContext.Current.TestOutputHelper?.WriteLine(third);

        Assert.Equal("active 1 panel #274176FF", first);
        Assert.Equal("active 0 panel #274176FF", second);
        Assert.Equal("active 0 panel #3060C0FF", third);
    }

    [Fact]
    public void ARemovedAncestorEndsTheTweensOfItsSubtree()
    {
        StyleSheet sheet = PanelSheet(nameof(ARemovedAncestorEndsTheTweensOfItsSubtree), out ClassId panelClass);
        Element root = new();
        Element group = new();
        Element first = NewPanel(panelClass);
        Element second = NewPanel(panelClass);
        Element inner = new();
        Element third = NewPanel(panelClass);
        root.Children.Add(group);
        group.Children.Add(first);
        group.Children.Add(second);
        group.Children.Add(inner);
        inner.Children.Add(third);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        first.States = PseudoStates.Hover;
        second.States = PseudoStates.Hover;
        third.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(3, context.Transitions.ActiveCount);

        // The transition pass alone, so the test reads the paint bits before the clear pass clears them.
        root.Children.Remove(group);
        context.UpdateTransitions(StepTime);

        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(Midpoint, first.Style.BackgroundColor);
        Assert.Equal(Midpoint, second.Style.BackgroundColor);
        Assert.Equal(Midpoint, third.Style.BackgroundColor);
        Assert.Equal(DirtyFlags.None, first.Dirty & DirtyFlags.Paint);
        Assert.Equal(DirtyFlags.None, second.Dirty & DirtyFlags.Paint);
        Assert.Equal(DirtyFlags.None, third.Dirty & DirtyFlags.Paint);
    }

    [Fact]
    public void ARemovedRootEndsItsTweens()
    {
        StyleSheet sheet = PanelSheet(nameof(ARemovedRootEndsItsTweens), out ClassId panelClass);
        Element root = NewPanel(panelClass);
        Element panel = NewPanel(panelClass);
        root.Children.Add(panel);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        root.States = PseudoStates.Hover;
        panel.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(2, context.Transitions.ActiveCount);

        Assert.True(context.RemoveRoot(root));
        context.Update(StepTime);

        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(Midpoint, root.Style.BackgroundColor);
        Assert.Equal(Midpoint, panel.Style.BackgroundColor);
    }

    [Fact]
    public void AMoveInsideTheTreeEndsTheTween()
    {
        StyleSheet sheet = PanelSheet(nameof(AMoveInsideTheTreeEndsTheTween), out ClassId panelClass);
        Element root = new();
        Element left = new();
        Element right = new();
        Element panel = NewPanel(panelClass);
        root.Children.Add(left);
        root.Children.Add(right);
        left.Children.Add(panel);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        panel.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(1, context.Transitions.ActiveCount);
        Assert.Equal(Midpoint, panel.Style.BackgroundColor);

        left.Children.Remove(panel);
        right.Children.Add(panel);
        context.Update(0f);

        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(Hovered, panel.Style.BackgroundColor);
    }

    [Fact]
    public void AMoveInsideOneChildListKeepsTheTween()
    {
        StyleSheet sheet = PanelSheet(nameof(AMoveInsideOneChildListKeepsTheTween), out ClassId panelClass);
        Element root = new();
        Element panel = NewPanel(panelClass);
        Element other = new();
        root.Children.Add(panel);
        root.Children.Add(other);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        panel.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(1, context.Transitions.ActiveCount);
        Assert.Equal(Midpoint, panel.Style.BackgroundColor);

        // Move does not attach, so the tween goes on (the rule of section 7 of the technical design).
        root.Children.Move(0, 1);
        context.Update(0f);

        Assert.Same(panel, root.Children[1]);
        Assert.Equal(1, context.Transitions.ActiveCount);
        Assert.Equal(Midpoint, panel.Style.BackgroundColor);

        context.Update(StepTime);

        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(Hovered, panel.Style.BackgroundColor);
    }

    [Fact]
    public void AMoveIntoAnotherContextStartsNoTween()
    {
        StyleSheet sheet = PanelSheet(nameof(AMoveIntoAnotherContextStartsNoTween), out ClassId panelClass);
        Rgba8 red = new(200, 0, 0, 255);
        StyleSheet otherSheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), new StyleProperties { BackgroundColor = Idle }, Fade(Duration))
            .Add(Hover(panelClass), new StyleProperties { BackgroundColor = red })
            .Build();

        Element firstRoot = new();
        Element panel = NewPanel(panelClass);
        firstRoot.Children.Add(panel);
        UiContext first = NewContext(firstRoot, sheet);
        Element secondRoot = new();
        UiContext second = NewContext(secondRoot, otherSheet);
        first.Update(0f);
        second.Update(0f);

        panel.States = PseudoStates.Hover;
        first.Update(StepTime);
        Assert.Equal(1, first.Transitions.ActiveCount);
        Assert.Equal(Midpoint, panel.Style.BackgroundColor);

        firstRoot.Children.Remove(panel);
        secondRoot.Children.Add(panel);
        second.Update(0f);

        Assert.Equal(0, second.Transitions.ActiveCount);
        Assert.Equal(red, panel.Style.BackgroundColor);

        // Without the tree check, the first context writes the end of its tween (#3060C0) here.
        first.UpdateTransitions(StepTime);

        Assert.Equal(0, first.Transitions.ActiveCount);
        Assert.Equal(red, panel.Style.BackgroundColor);
        Assert.Equal(DirtyFlags.None, panel.Dirty & DirtyFlags.Paint);

        first.Update(StepTime);
        Assert.Equal(red, panel.Style.BackgroundColor);
    }

    [Fact]
    public void AnElementThatBecomesARootStartsNoTween()
    {
        StyleSheet sheet = PanelSheet(nameof(AnElementThatBecomesARootStartsNoTween), out ClassId panelClass);
        Element root = new();
        Element panel = NewPanel(panelClass);
        Element child = NewPanel(panelClass);
        root.Children.Add(panel);
        panel.Children.Add(child);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        panel.States = PseudoStates.Hover;
        child.States = PseudoStates.Hover;
        context.Update(StepTime);
        Assert.Equal(2, context.Transitions.ActiveCount);

        // AddRoot takes the panel from its parent and joins it, with its subtree, as a root of the same context.
        context.AddRoot(panel);
        context.Update(0f);

        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(Hovered, panel.Style.BackgroundColor);
        Assert.Equal(Hovered, child.Style.BackgroundColor);
    }

    [Fact]
    public void AStateChangeAfterAJoinStillFades()
    {
        StyleSheet sheet = PanelSheet(nameof(AStateChangeAfterAJoinStillFades), out ClassId panelClass);
        Element root = new();
        Element panel = NewPanel(panelClass);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        root.Children.Add(panel);
        context.Update(0f);
        Assert.Equal(Idle, panel.Style.BackgroundColor);

        // The join set IsStyled to false, and the restyle set it again. A state change keeps it, so the hover fades.
        panel.States = PseudoStates.Hover;
        context.Update(StepTime);

        Assert.Equal(1, context.Transitions.ActiveCount);
        Assert.Equal(Midpoint, panel.Style.BackgroundColor);
    }

    [Fact]
    public void ThePoolGrowsAndNeverShrinks()
    {
        StyleSheet sheet = PanelSheet(nameof(ThePoolGrowsAndNeverShrinks), out ClassId panelClass);
        Element root = new();
        Element[] panels = NewPanels(root, panelClass, 40);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        Assert.Equal(0, context.Transitions.Capacity);

        SetStates(panels, PseudoStates.Hover);
        context.Update(StepTime);

        int peak = context.Transitions.Capacity;
        Assert.Equal(40, context.Transitions.ActiveCount);
        Assert.True(peak >= 40, "The capacity is " + peak + ".");

        context.Update(StepTime);
        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(peak, context.Transitions.Capacity);

        SetStates(panels, PseudoStates.None);
        context.Update(StepTime);
        Assert.Equal(40, context.Transitions.ActiveCount);
        Assert.Equal(peak, context.Transitions.Capacity);

        // A drop of the tree check also keeps the capacity.
        for (int i = 0; i < panels.Length; i++)
        {
            root.Children.Remove(panels[i]);
        }

        context.Update(StepTime);
        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.Equal(peak, context.Transitions.Capacity);
    }

    [Fact]
    public void ThePoolReusesItsRecords()
    {
        StyleSheet sheet = PanelSheet(nameof(ThePoolReusesItsRecords), out ClassId panelClass);
        Element root = new();
        Element[] panels = NewPanels(root, panelClass, 40);
        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        // The warm-up: two rounds grow the pool to its peak.
        int warmPeak = 0;
        for (int round = 0; round < 2; round++)
        {
            warmPeak = Math.Max(warmPeak, RunRound(context, panels));
        }

        int capacity = context.Transitions.Capacity;
        Assert.Equal(40, warmPeak);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int peak = 0;
        for (int round = 0; round < 2; round++)
        {
            int roundPeak = RunRound(context, panels);
            peak = roundPeak > peak ? roundPeak : peak;
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.True(after - before == 0, "Two rounds allocated " + (after - before) + " bytes.");
        Assert.Equal(40, peak);
        Assert.Equal(capacity, context.Transitions.Capacity);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    /// <summary>
    /// Runs one round: a hover in and a hover out on each panel. Each change runs three steps of 0.016 s and one step
    /// of 1 s, which ends each tween.
    /// </summary>
    /// <returns>The peak count of the running records in the round.</returns>
    private static int RunRound(UiContext context, Element[] panels)
    {
        int peak = 0;
        for (int change = 0; change < 2; change++)
        {
            SetStates(panels, change == 0 ? PseudoStates.Hover : PseudoStates.None);
            for (int step = 0; step < 3; step++)
            {
                context.Update(0.016f);
                int active = context.Transitions.ActiveCount;
                peak = active > peak ? active : peak;
            }

            context.Update(1f);
            if (context.Transitions.ActiveCount != 0)
            {
                return -1;
            }
        }

        return peak;
    }

    /// <summary>Sets the states of each panel.</summary>
    private static void SetStates(Element[] panels, PseudoStates states)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].States = states;
        }
    }

    /// <summary>Adds a count of panels to a root.</summary>
    private static Element[] NewPanels(Element root, ClassId panelClass, int count)
    {
        Element[] panels = new Element[count];
        for (int i = 0; i < count; i++)
        {
            panels[i] = NewPanel(panelClass);
            root.Children.Add(panels[i]);
        }

        return panels;
    }

    /// <summary>Gives a new element with the panel class.</summary>
    private static Element NewPanel(ClassId panelClass)
    {
        Element panel = new();
        panel.Classes.Add(panelClass);
        return panel;
    }

    /// <summary>Gives the sheet of phase 1 with the panel class of a test.</summary>
    private static StyleSheet PanelSheet(string test, out ClassId panelClass)
    {
        panelClass = ClassName.Intern(test + ".panel");
        return new StyleSheet.Builder()
            .Add(Selector.ForClass(panelClass), new StyleProperties { BackgroundColor = Idle }, Fade(Duration))
            .Add(Hover(panelClass), new StyleProperties { BackgroundColor = Hovered })
            .Build();
    }

    /// <summary>Gives a set with a linear fill transition.</summary>
    private static StyleTransitions Fade(float duration)
    {
        StyleTransitions transitions = default;
        transitions.Set(StyleProperty.BackgroundColor, new StyleTransition(duration, TransitionEasing.Linear));
        return transitions;
    }

    /// <summary>Gives the selector of a class with the hover state.</summary>
    private static Selector Hover(ClassId id)
    {
        return Selector.For(new CompoundSelector { Class = id, States = PseudoStates.Hover });
    }

    /// <summary>Gives a context with a measurer, a viewport, the root, and the sheet.</summary>
    private static UiContext NewContext(Element root, StyleSheet sheet)
    {
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
        context.AddRoot(root);
        context.AddSheet(sheet);
        return context;
    }

    /// <summary>Gives the line of the demo: the count of the running records and the fill of the panel.</summary>
    private static string Line(UiContext context, Element panel)
    {
        return "active " + context.Transitions.ActiveCount + " panel " + panel.Style.BackgroundColor;
    }
}
