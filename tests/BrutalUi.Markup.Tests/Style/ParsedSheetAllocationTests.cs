using System;
using System.IO;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of phase 7 of the parser: the allocation gate of a parsed sheet, and the sheet of the HelloPanel sample.
/// </summary>
/// <remarks>
/// <para>
/// <c>BrutalUi.Core.Tests</c> cannot reference Markup, so the gate of a parsed sheet lives here (Decision 10). The
/// project links <c>AllocationGate.cs</c> from the shared folder of the Core tests.
/// </para>
/// <para>
/// The class table is global and xUnit runs test classes in parallel. The gate interns its names with the prefix
/// <c>nameof(Test) + "-"</c>. <see cref="TheHelloPanelSheetGivesTheOldValues"/> reads the real names of the sample.
/// Each test passes the bytes of the text to <c>Parse</c> (decision R2).
/// </para>
/// <para>
/// The sheet of the gate declares a transition list, so a hover toggle starts a tween on its panel. Each step runs
/// <c>Update</c> with a step time, so the measured steps also run the transition pass and the turns of the tweens.
/// The tweens grow the transition pool during the warm-up only. Before each check of the rules, the gate runs one
/// settle update outside the measure. Its time is more than each duration, so each member holds its target.
/// </para>
/// </remarks>
public sealed class ParsedSheetAllocationTests
{
    [Fact]
    public void AParsedSheetAllocatesNothingInUpdate()
    {
        GateScene scene = GateScene.Build();

        // Warm-up: the steps let the JIT compile the mark, the match, the pass, and the layout.
        for (int i = 0; i < 16; i++)
        {
            scene.Step();
        }

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            scene.Step();
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
        Assert.Equal(116, scene.StepCount);
        Assert.Equal(50, scene.Elements.Length);

        // The steps reached the rules: the match is visible in the last state of the tree. The last measured step
        // (115) sets the name A. One more step sets the name B, so the check reads both name rules. The settle
        // update ends each running tween, so each member holds its target before the check.
        scene.Settle();
        scene.AssertTheRulesMatch();
        Assert.Equal(Length.Px(10f), scene.Named.Style.MinWidth);
        scene.Step();
        scene.Settle();
        scene.AssertTheRulesMatch();
        Assert.Equal(Length.Percent(20f), scene.Named.Style.MinWidth);
    }

    [Fact]
    public void TheHelloPanelSheetGivesTheOldValues()
    {
        // The test reads the copy of samples/HelloPanel/HelloScreen.css that the project links next to the assembly.
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "HelloScreen.css"));
        StyleParseResult result = new StyleSheetParser().Parse(bytes, "HelloScreen.css");

        Assert.Empty(result.Errors);
        Assert.True(result.Succeeded);
        StyleSheet sheet = result.GetSheetOrThrow();
        Assert.Equal(6, sheet.Rules.Length);

        StyleProperties root = FindRule(sheet, ".hello-root").Properties;
        AssertSetMembers(in root, StyleProperty.AlignItems, StyleProperty.Padding);
        Assert.Equal(Align.FlexStart, root.AlignItems);
        AssertEdges(Length.Px(OldValues.RootPadding), root.Padding);

        StyleRule panelRule = FindRule(sheet, ".hello-panel");
        StyleProperties panel = panelRule.Properties;
        AssertSetMembers(
            in panel,
            StyleProperty.FlexDirection,
            StyleProperty.AlignItems,
            StyleProperty.Padding,
            StyleProperty.BorderWidth,
            StyleProperty.CornerRadius,
            StyleProperty.BackgroundColor,
            StyleProperty.BorderColor);
        Assert.Equal(FlexDirection.Column, panel.FlexDirection);
        Assert.Equal(Align.FlexStart, panel.AlignItems);
        AssertEdges(Length.Px(OldValues.PanelPadding), panel.Padding);
        AssertEdges(Length.Px(OldValues.PanelBorderWidth), panel.BorderWidth);
        AssertCorners(OldValues.PanelCornerRadius, OldValues.PanelCornerRadius, panel.CornerRadius);
        Assert.Equal(OldValues.PanelColor, panel.BackgroundColor);
        Assert.Equal(OldValues.BorderColor, panel.BorderColor);
        Assert.True(panelRule.Transitions.IsEmpty);

        // The two state rules of input-routing/routing-gate. The selector text of a parsed state rule is the class
        // text and the state text with no space, for example ".hello-panel:hover". Each rule sets one member only, so
        // the hover and the focus show at the same time.
        StyleRule hoverRule = FindRule(sheet, ".hello-panel:hover");
        StyleProperties hover = hoverRule.Properties;
        AssertSetMembers(in hover, StyleProperty.BackgroundColor);
        Assert.Equal(new Rgba8(0x28, 0x30, 0x44, 255), hover.BackgroundColor);
        Assert.True(hoverRule.Transitions.IsEmpty);

        StyleRule focusRule = FindRule(sheet, ".hello-panel:focus");
        StyleProperties focus = focusRule.Properties;
        AssertSetMembers(in focus, StyleProperty.BorderColor);
        Assert.Equal(new Rgba8(0xf0, 0xc0, 0x40, 255), focus.BorderColor);
        Assert.True(focusRule.Transitions.IsEmpty);

        StyleProperties label = FindRule(sheet, ".hello-panel > Label").Properties;
        AssertSetMembers(in label, StyleProperty.Color, StyleProperty.FontSize);
        Assert.Equal(OldValues.GreetingColor, label.Color);
        Assert.Equal(OldValues.GreetingFontSize, label.FontSize);

        // margin: 12px 0 0 gives 0px on the other three edges. The old inline set left them undefined, and the
        // layout reads an undefined margin as 0.
        StyleProperties image = FindRule(sheet, ".hello-panel > Image").Properties;
        AssertSetMembers(in image, StyleProperty.Margin);
        Assert.Equal(Length.Px(OldValues.ImageTopMargin), image.Margin.Top);
        Assert.Equal(Length.Px(0f), image.Margin.Right);
        Assert.Equal(Length.Px(0f), image.Margin.Bottom);
        Assert.Equal(Length.Px(0f), image.Margin.Left);

        // The tree of HelloScreen.Build with the same classes: each rule reaches its element.
        Label greeting = new(new Label.CreateInfo { Text = "Hello, brutal-ui" });
        Image checker = new(new Image.CreateInfo { Texture = default });
        Element panelElement = new();
        panelElement.Classes.Add(ClassName.Intern("hello-panel"));
        panelElement.Children.Add(greeting);
        panelElement.Children.Add(checker);
        Element rootElement = new();
        rootElement.Classes.Add(ClassName.Intern("hello-root"));
        rootElement.Children.Add(panelElement);

        UiContext context = NewContext();
        context.AddSheet(sheet);
        context.AddRoot(rootElement);
        context.Update(0f);

        Assert.Equal(Align.FlexStart, rootElement.Style.AlignItems);
        AssertEdges(Length.Px(OldValues.RootPadding), rootElement.Style.Padding);
        Assert.Equal(FlexDirection.Column, panelElement.Style.FlexDirection);
        Assert.Equal(OldValues.PanelColor, panelElement.Style.BackgroundColor);
        Assert.Equal(OldValues.BorderColor, panelElement.Style.BorderColor);
        AssertCorners(OldValues.PanelCornerRadius, OldValues.PanelCornerRadius, panelElement.Style.CornerRadius);
        Assert.Equal(OldValues.GreetingColor, greeting.Style.Color);
        Assert.Equal(OldValues.GreetingFontSize, greeting.Style.FontSize);
        Assert.Equal(default(FontHandle), greeting.Style.Font);
        Assert.Equal(Length.Px(OldValues.ImageTopMargin), checker.Style.Margin.Top);
    }

    /// <summary>Gives the one rule of a sheet with the CSS text of a selector.</summary>
    private static StyleRule FindRule(StyleSheet sheet, string selectorText)
    {
        StyleRule? found = null;
        foreach (ref readonly StyleRule rule in sheet.Rules)
        {
            if (rule.Selector.ToString() == selectorText)
            {
                Assert.Null(found);
                found = rule;
            }
        }

        Assert.NotNull(found);
        return found.Value;
    }

    /// <summary>Checks that the set holds each member of the list and no other member.</summary>
    private static void AssertSetMembers(in StyleProperties properties, params StyleProperty[] members)
    {
        for (int i = 0; i <= (int)StyleProperty.FontSize; i++)
        {
            StyleProperty member = (StyleProperty)i;
            Assert.True(
                (Array.IndexOf(members, member) >= 0) == properties.IsSet(member),
                $"The member {member} has the wrong set state.");
        }
    }

    private static void AssertEdges(Length expected, LengthEdges edges)
    {
        Assert.Equal(expected, edges.Top);
        Assert.Equal(expected, edges.Right);
        Assert.Equal(expected, edges.Bottom);
        Assert.Equal(expected, edges.Left);
    }

    /// <summary>Checks the corners. CSS gives the first value to top-left and bottom-right.</summary>
    private static void AssertCorners(float topLeftAndBottomRight, float topRightAndBottomLeft, Corners corners)
    {
        Assert.Equal(topLeftAndBottomRight, corners.TopLeft);
        Assert.Equal(topRightAndBottomLeft, corners.TopRight);
        Assert.Equal(topLeftAndBottomRight, corners.BottomRight);
        Assert.Equal(topRightAndBottomLeft, corners.BottomLeft);
    }

    /// <summary>Makes a context that can lay out a <see cref="Label"/>: a <see cref="Label"/> needs a measurer.</summary>
    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new FixedMeasurer() })
        {
            ViewportSize = new float2(1280f, 720f),
        };
    }

    /// <summary>
    /// The old constants of the sample. Source: <c>samples/HelloPanel/HelloScreen.cs</c> at commit <c>c2af581</c>,
    /// before phase 7 of style-system/style-parser moved each size and each color into <c>HelloScreen.css</c>.
    /// </summary>
    private static class OldValues
    {
        public const float RootPadding = 24f;
        public const float PanelPadding = 16f;
        public const float PanelBorderWidth = 2f;
        public const float PanelCornerRadius = 8f;
        public const float GreetingFontSize = 26f;
        public const float ImageTopMargin = 12f;

        public static readonly Rgba8 PanelColor = new(30, 34, 44, 255);
        public static readonly Rgba8 BorderColor = new(96, 140, 220, 255);
        public static readonly Rgba8 GreetingColor = new(255, 255, 255, 255);
    }

    /// <summary>
    /// The tree of the gate: a root, 7 panels, and 6 items under each panel (50 elements). The first item of each panel
    /// is a <see cref="Label"/>. One parsed text gives the sheet. Each step toggles the hover of one panel, toggles one
    /// class of one item, sets the name of one item to one of two names, and runs one <c>Update</c> with
    /// <see cref="StepTime"/>. <see cref="Settle"/> ends each running tween.
    /// </summary>
    private sealed class GateScene
    {
        private const string Prefix = nameof(AParsedSheetAllocatesNothingInUpdate) + "-";
        private const string NameA = Prefix + "a";
        private const string NameB = Prefix + "b";
        private const int PanelCount = 7;
        private const int ItemCount = 6;
        private const int RuleCount = 10;

        /// <summary>The time of one step in seconds: one frame at about 60 frames each second.</summary>
        private const float StepTime = 0.016f;

        /// <summary>The time of the settle update in seconds. It is more than each duration of the sheet.</summary>
        private const float SettleTime = 1f;

        private static readonly Rgba8 Accent = new(0x30, 0x60, 0xC0, 255);
        private static readonly Rgba8 HoverFill = new(0x10, 0x20, 0x30, 255);
        private static readonly Rgba8 ItemBorder = new(0xFF, 0xCC, 0x00, 255);
        private static readonly Rgba8 LabelText = new(255, 255, 255, 255);

        public required UiContext Context { get; init; }

        public required Element[] Elements { get; init; }

        public required Element[] Panels { get; init; }

        public required Element[] Items { get; init; }

        public required Element Named { get; init; }

        public required Element Root { get; init; }

        public required ClassId MarkClass { get; init; }

        public int StepCount { get; private set; }

        /// <summary>
        /// The text of the gate: a <c>:root</c> block with two variables, each selector form (a class, a list, a
        /// state, a child chain, a descendant chain, a type, a type with a class, and a name in a rule and in a chain),
        /// and each value family (a keyword, a length in px, %, and auto, an edge list, a corner list, a number, a
        /// color, a font, and a transition list).
        /// </summary>
        private static string SheetText()
        {
            const string P = Prefix;
            return
                ":root {\n" +
                "  --accent: #3060c0;\n" +
                "  --gap: 4px;\n" +
                "}\n" +
                "." + P + "root, ." + P + "unused {\n" +
                "  flex-direction: column;\n" +
                "  align-items: flex-start;\n" +
                "  justify-content: space-between;\n" +
                "  padding: 8px 4px;\n" +
                "  width: 100%;\n" +
                "  height: auto;\n" +
                "}\n" +
                "." + P + "panel {\n" +
                "  background-color: var(--accent);\n" +
                "  border-radius: 2px 4px;\n" +
                "  margin: var(--gap) 0 0;\n" +
                "  border-width: 1px 2px 3px 4px;\n" +
                "  transition: background-color 0.2s ease-out, opacity 150ms linear;\n" +
                "}\n" +
                "." + P + "panel:hover {\n" +
                "  background-color: #102030;\n" +
                "  opacity: 0.5;\n" +
                "}\n" +
                "." + P + "panel:hover > ." + P + "item {\n" +
                "  border-color: #ffcc00;\n" +
                "}\n" +
                "." + P + "root ." + P + "mark {\n" +
                "  flex-grow: 2;\n" +
                "}\n" +
                "Label {\n" +
                "  color: #ffffff;\n" +
                "  font: default;\n" +
                "  font-size: 12px;\n" +
                "}\n" +
                "Label." + P + "item {\n" +
                "  max-width: 50%;\n" +
                "}\n" +
                "#" + NameA + " {\n" +
                "  min-width: 10px;\n" +
                "}\n" +
                "." + P + "panel > #" + NameB + " {\n" +
                "  min-width: 20%;\n" +
                "}\n";
        }

        public static GateScene Build()
        {
            StyleParseResult result = new StyleSheetParser().Parse(Encoding.UTF8.GetBytes(SheetText()), "gate.css");
            Assert.Empty(result.Errors);
            StyleSheet sheet = result.GetSheetOrThrow();
            Assert.Equal(RuleCount, sheet.Rules.Length);

            ClassId rootClass = ClassName.Intern(Prefix + "root");
            ClassId panelClass = ClassName.Intern(Prefix + "panel");
            ClassId itemClass = ClassName.Intern(Prefix + "item");
            ClassId markClass = ClassName.Intern(Prefix + "mark");

            Element[] elements = new Element[1 + PanelCount + (PanelCount * ItemCount)];
            Element[] panels = new Element[PanelCount];
            Element[] items = new Element[PanelCount * ItemCount];
            Element root = new();
            root.Classes.Add(rootClass);
            elements[0] = root;
            int index = 1;
            for (int p = 0; p < PanelCount; p++)
            {
                Element panel = new();
                panel.Classes.Add(panelClass);
                root.Children.Add(panel);
                panels[p] = panel;
                elements[index++] = panel;
                for (int i = 0; i < ItemCount; i++)
                {
                    Element item = i == 0 ? new Label(new Label.CreateInfo { Text = "x" }) : new Element();
                    item.Classes.Add(itemClass);
                    panel.Children.Add(item);
                    items[(p * ItemCount) + i] = item;
                    elements[index++] = item;
                }
            }

            // The named item is a plain item of the first panel, so the child chain of the name rule can match.
            Element named = items[1];
            named.Name = NameA;

            UiContext context = NewContext();
            context.AddRoot(root);
            context.AddSheet(sheet);
            context.Update(0f);

            return new GateScene
            {
                Context = context,
                Elements = elements,
                Panels = panels,
                Items = items,
                Named = named,
                Root = root,
                MarkClass = markClass,
            };
        }

        /// <summary>The measured body. It uses no lambda, no boxing, no new string, and no Assert.</summary>
        public void Step()
        {
            int step = StepCount++;
            Element panel = Panels[step % Panels.Length];
            panel.States = panel.States == PseudoStates.None ? PseudoStates.Hover : PseudoStates.None;

            ClassList list = Items[step % Items.Length].Classes;
            if (!list.Remove(MarkClass))
            {
                list.Add(MarkClass);
            }

            Named.Name = (step & 1) == 0 ? NameB : NameA;

            // The step time advances the tweens of the transition list, so the measure covers the transition pass.
            Context.Update(StepTime);
        }

        /// <summary>
        /// Runs one update with <see cref="SettleTime"/> and changes no state. Each running tween ends, so each member
        /// holds its target. Call it outside the measure, before <see cref="AssertTheRulesMatch"/>.
        /// </summary>
        public void Settle()
        {
            Context.Update(SettleTime);
        }

        /// <summary>Checks that the style of each element follows its state, its classes, its name, and its type.</summary>
        public void AssertTheRulesMatch()
        {
            ComputedStyle root = Root.Style;
            Assert.Equal(FlexDirection.Column, root.FlexDirection);
            Assert.Equal(Align.FlexStart, root.AlignItems);
            Assert.Equal(Justify.SpaceBetween, root.JustifyContent);
            Assert.Equal(Length.Px(8f), root.Padding.Top);
            Assert.Equal(Length.Px(4f), root.Padding.Right);
            Assert.Equal(Length.Px(8f), root.Padding.Bottom);
            Assert.Equal(Length.Px(4f), root.Padding.Left);
            Assert.Equal(Length.Percent(100f), root.Width);
            Assert.Equal(Length.Auto, root.Height);

            bool sawHover = false;
            bool sawMark = false;
            for (int p = 0; p < Panels.Length; p++)
            {
                Element panel = Panels[p];
                bool hover = panel.States == PseudoStates.Hover;
                sawHover |= hover;
                ComputedStyle panelStyle = panel.Style;
                Assert.Equal(hover ? HoverFill : Accent, panelStyle.BackgroundColor);
                Assert.Equal(hover ? 0.5f : 1f, panelStyle.Opacity);
                Assert.Equal(2f, panelStyle.CornerRadius.TopLeft);
                Assert.Equal(4f, panelStyle.CornerRadius.TopRight);
                Assert.Equal(2f, panelStyle.CornerRadius.BottomRight);
                Assert.Equal(4f, panelStyle.CornerRadius.BottomLeft);
                Assert.Equal(Length.Px(4f), panelStyle.Margin.Top);
                Assert.Equal(Length.Px(0f), panelStyle.Margin.Right);
                Assert.Equal(Length.Px(0f), panelStyle.Margin.Bottom);
                Assert.Equal(Length.Px(0f), panelStyle.Margin.Left);
                Assert.Equal(Length.Px(1f), panelStyle.BorderWidth.Top);
                Assert.Equal(Length.Px(2f), panelStyle.BorderWidth.Right);
                Assert.Equal(Length.Px(3f), panelStyle.BorderWidth.Bottom);
                Assert.Equal(Length.Px(4f), panelStyle.BorderWidth.Left);

                for (int i = 0; i < ItemCount; i++)
                {
                    Element item = Items[(p * ItemCount) + i];
                    bool mark = item.Classes.Contains(MarkClass);
                    sawMark |= mark;
                    ComputedStyle itemStyle = item.Style;
                    Assert.Equal(hover ? ItemBorder : Rgba8.Transparent, itemStyle.BorderColor);
                    Assert.Equal(mark ? 2f : 0f, itemStyle.FlexGrow);
                    if (item is Label)
                    {
                        Assert.Equal(LabelText, itemStyle.Color);
                        Assert.Equal(12f, itemStyle.FontSize);
                        Assert.Equal(default(FontHandle), itemStyle.Font);
                        Assert.Equal(Length.Percent(50f), itemStyle.MaxWidth);
                    }
                    else
                    {
                        Assert.Equal(Length.Undefined, itemStyle.MaxWidth);
                    }

                    if (!ReferenceEquals(item, Named))
                    {
                        Assert.Equal(Length.Undefined, itemStyle.MinWidth);
                    }
                }
            }

            // The name rule gives 10px for the name A. The child chain with a name gives 20% for the name B.
            bool nameA = string.Equals(NameA, Named.Name, StringComparison.Ordinal);
            Assert.True(nameA || string.Equals(NameB, Named.Name, StringComparison.Ordinal));
            Assert.Equal(nameA ? Length.Px(10f) : Length.Percent(20f), Named.Style.MinWidth);
            Assert.True(sawHover, "No panel holds the hover state at the end.");
            Assert.True(sawMark, "No item holds the mark class at the end.");
        }
    }

    /// <summary>A renderer that draws nothing and measures each text as 10 by 10.</summary>
    private sealed class FixedMeasurer : IUiRenderer
    {
        public void Begin(in float2 displaySize, float scale)
        {
        }

        public void PushClip(in float4 rect)
        {
        }

        public void PopClip()
        {
        }

        public void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor)
        {
        }

        public void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius)
        {
        }

        public void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
        }

        public float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
            return new float2(10f, 10f);
        }

        public void End()
        {
        }
    }
}
