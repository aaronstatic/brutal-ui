using System.Text;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of phase 4 of the transitions: the follow walk of an inherited <see cref="ComputedStyle.Color"/>. A color
/// tween writes its visible value into each descendant that inherits the color, and it marks
/// <see cref="DirtyFlags.Paint"/> only (Decision 8).
/// </summary>
/// <remarks>
/// <para>
/// The class table and the type table are global and xUnit runs test classes in parallel. Each test interns its names
/// with the prefix <c>nameof(Test) + "."</c>, and no test asserts a fixed id value.
/// </para>
/// <para>
/// The color goes from #FFFFFF to #FFCC00. At 0.25 the color is #FFF2BFFF, at 0.5 it is #FFE680FF (255, 229.5,
/// 127.5 rounds up), and at 0.75 it is #FFD940FF. The steps are 0.1 s, and <c>0.1f + 0.1f</c> equals <c>0.2f</c>
/// exactly, so a tween of 0.2 s ends on its second step.
/// </para>
/// </remarks>
public sealed class TransitionInheritanceTests
{
    private const float Duration = 0.2f;
    private const float StepTime = 0.1f;

    private static readonly Rgba8 White = new(255, 255, 255, 255);
    private static readonly Rgba8 Gold = new(255, 204, 0, 255);
    private static readonly Rgba8 Blue = new(48, 96, 192, 255);

    [Fact]
    public void TheTextOfALabelFadesWithItsButton()
    {
        ClassId buttonClass = ClassName.Intern(nameof(TheTextOfALabelFadesWithItsButton) + ".button");
        StyleSheet sheet = ButtonSheet(buttonClass, Duration);

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Label label = new(new Label.CreateInfo { Text = "Play" });
        button.Children.Add(label);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        string first = Line(button, label);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        string second = Line(button, label);

        context.Update(StepTime);
        string third = Line(button, label);

        TestContext.Current.TestOutputHelper?.WriteLine(first);
        TestContext.Current.TestOutputHelper?.WriteLine(second);
        TestContext.Current.TestOutputHelper?.WriteLine(third);

        Assert.Equal("button #FFFFFFFF label #FFFFFFFF", first);
        Assert.Equal("button #FFE680FF label #FFE680FF", second);
        Assert.Equal("button #FFCC00FF label #FFCC00FF", third);
        Assert.Equal(0, context.Transitions.ActiveCount);
        Assert.True(label.FollowsColor);
        Assert.False(button.FollowsColor);
    }

    [Fact]
    public void AGrandchildFollowsThroughAnInheritingChild()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AGrandchildFollowsThroughAnInheritingChild) + ".button");
        StyleSheet sheet = ButtonSheet(buttonClass, Duration);

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Element panel = new();
        Label label = new(new Label.CreateInfo { Text = "Play" });
        panel.Children.Add(label);
        button.Children.Add(panel);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        string half = "panel " + panel.Style.Color + " label " + label.Style.Color;

        context.Update(StepTime);
        string end = "panel " + panel.Style.Color + " label " + label.Style.Color;

        TestContext.Current.TestOutputHelper?.WriteLine(half);
        TestContext.Current.TestOutputHelper?.WriteLine(end);
        Assert.Equal("panel #FFE680FF label #FFE680FF", half);
        Assert.Equal("panel #FFCC00FF label #FFCC00FF", end);
    }

    [Fact]
    public void AChildWithItsOwnColorDoesNotFollow()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AChildWithItsOwnColorDoesNotFollow) + ".button");
        ClassId iconClass = ClassName.Intern(nameof(AChildWithItsOwnColorDoesNotFollow) + ".icon");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { Color = White }, Fade(StyleProperty.Color, Duration))
            .Add(Hover(buttonClass), new StyleProperties { Color = Gold })
            .Add(Selector.ForClass(iconClass), new StyleProperties { Color = Blue })
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Element icon = new();
        icon.Classes.Add(iconClass);
        Label glyph = new(new Label.CreateInfo { Text = "+" });
        icon.Children.Add(glyph);
        button.Children.Add(icon);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        string line = "button " + button.Style.Color + " icon " + icon.Style.Color + " glyph " + glyph.Style.Color;
        TestContext.Current.TestOutputHelper?.WriteLine(line);

        Assert.Equal("button #FFE680FF icon #3060C0FF glyph #3060C0FF", line);
        Assert.False(icon.FollowsColor);
        Assert.True(glyph.FollowsColor);
        Assert.Equal(DirtyFlags.None, icon.Dirty);
        Assert.Equal(DirtyFlags.None, glyph.Dirty);

        context.Update(StepTime);
        Assert.Equal(Gold, button.Style.Color);
        Assert.Equal(Blue, icon.Style.Color);
        Assert.Equal(Blue, glyph.Style.Color);
    }

    [Fact]
    public void AnInlineColorStopsTheWalk()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AnInlineColorStopsTheWalk) + ".button");
        StyleSheet sheet = ButtonSheet(buttonClass, Duration);

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Element icon = new();
        icon.Inline.Color = Blue;
        Label glyph = new(new Label.CreateInfo { Text = "+" });
        icon.Children.Add(glyph);
        button.Children.Add(icon);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);

        Assert.Equal("#FFE680FF", button.Style.Color.ToString());
        Assert.False(icon.FollowsColor);
        Assert.Equal(Blue, icon.Style.Color);
        Assert.Equal(Blue, glyph.Style.Color);
    }

    [Fact]
    public void AChildWithItsOwnColorTransitionRunsItsOwnTween()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AChildWithItsOwnColorTransitionRunsItsOwnTween) + ".button");
        ClassId iconClass = ClassName.Intern(nameof(AChildWithItsOwnColorTransitionRunsItsOwnTween) + ".icon");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { Color = White }, Fade(StyleProperty.Color, Duration))
            .Add(Hover(buttonClass), new StyleProperties { Color = Gold })
            .Add(Selector.ForClass(iconClass), default, Fade(StyleProperty.Color, 0.4f))
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Element icon = new();
        icon.Classes.Add(iconClass);
        Label glyph = new(new Label.CreateInfo { Text = "+" });
        icon.Children.Add(glyph);
        button.Children.Add(icon);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        string quarter = Line3(button, icon, glyph);

        context.Update(StepTime);
        string half = Line3(button, icon, glyph);
        int halfCount = context.Transitions.ActiveCount;

        // 0.2f + 0.2f equals 0.4f exactly, so the tween of the icon ends on this step.
        context.Update(0.2f);
        string end = Line3(button, icon, glyph);

        TestContext.Current.TestOutputHelper?.WriteLine(quarter);
        TestContext.Current.TestOutputHelper?.WriteLine(half);
        TestContext.Current.TestOutputHelper?.WriteLine(end);

        // Decision R2: the icon inherits the color, but its own color transition stops the follow.
        Assert.False(icon.FollowsColor);
        Assert.True(glyph.FollowsColor);
        Assert.Equal("button #FFE680FF icon #FFF2BFFF glyph #FFF2BFFF", quarter);
        Assert.Equal("button #FFCC00FF icon #FFE680FF glyph #FFE680FF", half);
        Assert.Equal(1, halfCount);
        Assert.Equal("button #FFCC00FF icon #FFCC00FF glyph #FFCC00FF", end);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void AChildWithAFillTweenStillFollows()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AChildWithAFillTweenStillFollows) + ".button");
        ClassId chipClass = ClassName.Intern(nameof(AChildWithAFillTweenStillFollows) + ".chip");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { Color = White }, Fade(StyleProperty.Color, Duration))
            .Add(Hover(buttonClass), new StyleProperties { Color = Gold })
            .Add(
                Selector.ForClass(chipClass),
                new StyleProperties { BackgroundColor = new Rgba8(0, 0, 0, 255) },
                Fade(StyleProperty.BackgroundColor, 0.4f))
            .Add(Hover(chipClass), new StyleProperties { BackgroundColor = Blue })
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Element chip = new();
        chip.Classes.Add(chipClass);
        button.Children.Add(chip);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        button.States = PseudoStates.Hover;
        chip.States = PseudoStates.Hover;
        context.Update(StepTime);

        Assert.Equal(2, context.Transitions.ActiveCount);
        Assert.Equal("#FFE680FF", button.Style.Color.ToString());
        Assert.Equal("#FFE680FF", chip.Style.Color.ToString());
        Assert.Equal("#0C1830FF", chip.Style.BackgroundColor.ToString());
    }

    /// <summary>
    /// Decision R3: a <see cref="ComputedStyle.Color"/> transition of 0 s starts no tween, so it acts as no transition.
    /// The label keeps <see cref="Element.FollowsColor"/> and shows the fade of its button, as in CSS.
    /// </summary>
    [Fact]
    public void AChildWithAZeroColorTransitionStillFollows()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AChildWithAZeroColorTransitionStillFollows) + ".button");
        ClassId labelClass = ClassName.Intern(nameof(AChildWithAZeroColorTransitionStillFollows) + ".label");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { Color = White }, Fade(StyleProperty.Color, Duration))
            .Add(Hover(buttonClass), new StyleProperties { Color = Gold })
            .Add(Selector.ForClass(labelClass), new StyleProperties(), Fade(StyleProperty.Color, 0f))
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Label label = new(new Label.CreateInfo { Text = "Play" });
        label.Classes.Add(labelClass);
        button.Children.Add(label);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        Assert.True(label.FollowsColor);

        button.States = PseudoStates.Hover;
        context.Update(StepTime);
        string half = Line(button, label);

        context.Update(StepTime);
        string end = Line(button, label);

        TestContext.Current.TestOutputHelper?.WriteLine(half);
        TestContext.Current.TestOutputHelper?.WriteLine(end);
        Assert.Equal("button #FFE680FF label #FFE680FF", half);
        Assert.Equal("button #FFCC00FF label #FFCC00FF", end);
        Assert.True(label.FollowsColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void AChildTweenThatEndsFirstKeepsItsOwnTarget()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AChildTweenThatEndsFirstKeepsItsOwnTarget) + ".button");
        ClassId chipClass = ClassName.Intern(nameof(AChildTweenThatEndsFirstKeepsItsOwnTarget) + ".chip");
        StyleTransitions chipFades = default;
        chipFades.Set(StyleProperty.BackgroundColor, new StyleTransition(0.4f, TransitionEasing.Linear));
        chipFades.Set(StyleProperty.Color, new StyleTransition(StepTime, TransitionEasing.Linear));
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { Color = White }, Fade(StyleProperty.Color, 0.4f))
            .Add(Hover(buttonClass), new StyleProperties { Color = Gold })
            .Add(Selector.ForClass(chipClass), new StyleProperties { BackgroundColor = new Rgba8(0, 0, 0, 255) }, chipFades)
            .Add(Hover(chipClass), new StyleProperties { BackgroundColor = Blue })
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Element chip = new();
        chip.Classes.Add(chipClass);
        Label label = new(new Label.CreateInfo { Text = "chip" });
        chip.Children.Add(label);
        button.Children.Add(chip);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);

        // The fill tween of the chip rents the first record, so the record of the button comes after it. The pool
        // visits the button first, and the result must not depend on that order.
        chip.States = PseudoStates.Hover;
        context.Update(0f);
        button.States = PseudoStates.Hover;
        context.Update(StepTime);

        string line = Line3(button, chip, label);
        TestContext.Current.TestOutputHelper?.WriteLine(line);

        // Decision R2: the color tween of the chip ends on this step. The chip holds its own color transition, so it
        // does not follow the button. It keeps its target, and the label follows the chip.
        Assert.Equal("button #FFF2BFFF icon #FFCC00FF glyph #FFCC00FF", line);
        Assert.False(chip.FollowsColor);
        Assert.Equal(2, context.Transitions.ActiveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AChildWithAShorterOwnColorTweenNeverJumpsBack(bool restyleTheRoot)
    {
        ClassId buttonClass = ClassName.Intern(nameof(AChildWithAShorterOwnColorTweenNeverJumpsBack) + ".button");
        ClassId chipClass = ClassName.Intern(nameof(AChildWithAShorterOwnColorTweenNeverJumpsBack) + ".chip");
        Rgba8 dark = new(0, 0, 0, 255);
        Rgba8 red = new(200, 0, 0, 255);
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { Color = dark }, Fade(StyleProperty.Color, 0.4f))
            .Add(Hover(buttonClass), new StyleProperties { Color = red })
            .Add(Selector.ForClass(chipClass), default, Fade(StyleProperty.Color, StepTime))
            .Build();

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Element chip = new();
        chip.Classes.Add(chipClass);
        Label label = new(new Label.CreateInfo { Text = "chip" });
        chip.Children.Add(label);
        button.Children.Add(chip);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        button.States = PseudoStates.Hover;

        // Probe 13: the chip ends its own tween of 0.1 s while the button runs its tween of 0.4 s. The old rule wrote
        // the visible color of the button over the chip, and the red channel fell from 150 to 50. A state change on
        // the root restyles the full subtree: once during the tween of the chip (the keep branch), and once after it
        // (the old rule started a second own tween from the color of the button, and it fell again from 175 to 150).
        int chipRed = 0;
        int labelRed = 0;
        StringBuilder trace = new();
        for (int i = 0; i < 20; i++)
        {
            if (restyleTheRoot && i == 2)
            {
                root.States = PseudoStates.Focus;
            }

            if (restyleTheRoot && i == 8)
            {
                root.States = PseudoStates.None;
            }

            context.Update(0.025f);
            trace.Append(button.Style.Color.R).Append('/').Append(chip.Style.Color.R).Append('/')
                .Append(label.Style.Color.R).Append(' ');

            Assert.True(chip.Style.Color.R >= chipRed, "The red channel of the chip falls at step " + i + ": " + trace);
            Assert.True(label.Style.Color.R >= labelRed, "The red channel of the label falls at step " + i + ": " + trace);
            chipRed = chip.Style.Color.R;
            labelRed = label.Style.Color.R;

            // The chip reaches its target before the button, and it keeps the target.
            if (i == 6)
            {
                Assert.Equal(red, chip.Style.Color);
                Assert.True(button.Style.Color.R < red.R, "The tween of the button runs at step 6: " + trace);
            }
        }

        TestContext.Current.TestOutputHelper?.WriteLine(trace.ToString());
        Assert.Equal(red, button.Style.Color);
        Assert.Equal(red, chip.Style.Color);
        Assert.Equal(red, label.Style.Color);
        Assert.False(chip.FollowsColor);
        Assert.True(label.FollowsColor);
        Assert.Equal(0, context.Transitions.ActiveCount);
    }

    [Fact]
    public void AFollowMarksPaintOnly()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AFollowMarksPaintOnly) + ".button");
        StyleSheet sheet = ButtonSheet(buttonClass, Duration);

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        Label label = new(new Label.CreateInfo { Text = "Play" });
        button.Children.Add(label);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        button.States = PseudoStates.Hover;
        context.Update(StepTime);

        // The full update cleared each bit, and the tween runs.
        Assert.Equal(DirtyFlags.None, root.Dirty);
        Assert.Equal(DirtyFlags.None, button.Dirty);
        Assert.Equal(DirtyFlags.None, label.Dirty);
        Assert.Equal(1, context.Transitions.ActiveCount);

        context.UpdateTransitions(0.05f);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "root " + root.Dirty + ", button " + button.Dirty + ", label " + label.Dirty);
        Assert.Equal(DirtyFlags.Paint, label.Dirty);
        Assert.Equal(DirtyFlags.Paint | DirtyFlags.DescendantPaint, button.Dirty);
        Assert.Equal(DirtyFlags.DescendantPaint, root.Dirty);
        Assert.Equal("#FFD940FF", button.Style.Color.ToString());
        Assert.Equal("#FFD940FF", label.Style.Color.ToString());
    }

    [Fact]
    public void AChildAddedDuringTheFadeFollows()
    {
        ClassId buttonClass = ClassName.Intern(nameof(AChildAddedDuringTheFadeFollows) + ".button");
        StyleSheet sheet = ButtonSheet(buttonClass, Duration);

        Element root = new();
        Element button = new();
        button.Classes.Add(buttonClass);
        root.Children.Add(button);

        UiContext context = NewContext(root, sheet);
        context.Update(0f);
        button.States = PseudoStates.Hover;
        context.Update(StepTime);

        Label late = new(new Label.CreateInfo { Text = "Late" });
        button.Children.Add(late);
        context.Update(0.05f);

        string line = Line(button, late);
        TestContext.Current.TestOutputHelper?.WriteLine(line);
        Assert.Equal("button #FFD940FF label #FFD940FF", line);
        Assert.True(late.FollowsColor);

        context.Update(StepTime);
        Assert.Equal("button #FFCC00FF label #FFCC00FF", Line(button, late));
    }

    /// <summary>Gives the sheet of the demo: a white button with a color fade, and a gold hover.</summary>
    private static StyleSheet ButtonSheet(ClassId buttonClass, float duration)
    {
        return new StyleSheet.Builder()
            .Add(Selector.ForClass(buttonClass), new StyleProperties { Color = White }, Fade(StyleProperty.Color, duration))
            .Add(Hover(buttonClass), new StyleProperties { Color = Gold })
            .Build();
    }

    /// <summary>Gives a transition set with one linear slot.</summary>
    private static StyleTransitions Fade(StyleProperty property, float duration)
    {
        StyleTransitions transitions = default;
        transitions.Set(property, new StyleTransition(duration, TransitionEasing.Linear));
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

    /// <summary>Gives the line of the demo: the color of the button and the color of the label.</summary>
    private static string Line(Element button, Element label)
    {
        return "button " + button.Style.Color + " label " + label.Style.Color;
    }

    /// <summary>Gives the color of three elements in one line.</summary>
    private static string Line3(Element button, Element icon, Element glyph)
    {
        return "button " + button.Style.Color + " icon " + icon.Style.Color + " glyph " + glyph.Style.Color;
    }
}
