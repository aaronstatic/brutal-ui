using System;
using System.IO;
using Brutal.Numerics;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The tests of the sheet of the HelloPanel sample: the rule count, the widget rules, and the label rules
/// (Decision 7 of interactive-elements/widget-sample).
/// </summary>
/// <remarks>
/// <para>
/// Each test parses the copy of <c>samples/HelloPanel/HelloScreen.css</c> that the project links next to the
/// assembly. A parse test needs no GPU, so a headless verifier checks the sheet.
/// </para>
/// <para>
/// The tests read the real sheet, so they intern the real class names of the sample (<c>hello-slot-root</c>,
/// <c>hello-panel</c>, <c>hello-panel-root</c>, <c>hello-widgets</c>, <c>hello-row</c>, and <c>hello-status</c>).
/// Each expected value comes from the sheet.
/// </para>
/// </remarks>
public sealed class HelloPanelSheetTests
{
    /// <summary>The number of rules of the sheet after phase 1 of panels-and-modes/panel-sample.</summary>
    private const int RuleCount = 33;

    /// <summary>The width of one byte of text in pixels, from the measurer of the tests.</summary>
    private const float ByteWidth = 8f;

    /// <summary>The height of one line of text in pixels, from the measurer of the tests.</summary>
    private const float LineHeight = 16f;

    /// <summary>The fill of a button with no state: <c>--widget-fill</c>.</summary>
    private static readonly Rgba8 WidgetFill = new(0x2c, 0x34, 0x46, 255);

    /// <summary>The border color of a button with no focus: <c>--widget-border</c>.</summary>
    private static readonly Rgba8 WidgetBorder = new(0x4a, 0x56, 0x72, 255);

    /// <summary>The fill of a button under the pointer: <c>--widget-hover</c>.</summary>
    private static readonly Rgba8 WidgetHover = new(0x3c, 0x48, 0x62, 255);

    /// <summary>The fill of a pressed button: <c>--widget-active</c>.</summary>
    private static readonly Rgba8 WidgetActive = new(0x18, 0x20, 0x30, 255);

    /// <summary>The border color of the focus owner: <c>--widget-focus</c>.</summary>
    private static readonly Rgba8 WidgetFocus = new(0xf0, 0xc0, 0x40, 255);

    /// <summary>The text color: <c>--text</c>.</summary>
    private static readonly Rgba8 TextColor = new(0xff, 0xff, 0xff, 255);

    /// <summary>The fill of a disabled button: <c>--disabled-fill</c>.</summary>
    private static readonly Rgba8 DisabledFill = new(0x1a, 0x1e, 0x26, 255);

    /// <summary>The text color of a disabled button: <c>--disabled-text</c>.</summary>
    private static readonly Rgba8 DisabledText = new(0x6a, 0x72, 0x84, 255);

    /// <summary>The fill of a checked toggle: <c>--toggle-checked</c>.</summary>
    private static readonly Rgba8 ToggleChecked = new(0x3c, 0x8c, 0x5a, 255);

    /// <summary>The fill of the slider track: <c>--slider-track</c>.</summary>
    private static readonly Rgba8 SliderTrack = new(0x14, 0x1a, 0x26, 255);

    /// <summary>The thumb color of a slider with no state: <c>--slider-thumb</c>.</summary>
    private static readonly Rgba8 SliderThumb = new(0xb4, 0xc0, 0xd8, 255);

    /// <summary>The thumb color of a slider under the pointer: <c>--slider-thumb-hover</c>.</summary>
    private static readonly Rgba8 SliderThumbHover = new(0xdc, 0xe4, 0xf4, 255);

    /// <summary>The thumb color of a pressed slider: <c>--slider-thumb-active</c>.</summary>
    private static readonly Rgba8 SliderThumbActive = new(0x70, 0xa0, 0xf0, 255);

    /// <summary>The border color of a scroll view under the pointer: <c>--scroll-hover-border</c>.</summary>
    private static readonly Rgba8 ScrollHoverBorder = new(0x7a, 0x94, 0xc8, 255);

    /// <summary>The selector text of each rule of the sheet, in the order of the sheet.</summary>
    private static readonly string[] SelectorTexts =
    [
        ".hello-slot-root",
        "SlotContainer",
        ".hello-panel-root",
        ".hello-panel",
        ".hello-panel:hover",
        ".hello-panel:focus",
        ".hello-panel > Label",
        ".hello-panel > Image",
        ".hello-widgets",
        ".hello-row",
        ".hello-row > Label",
        "Button",
        "Button:hover",
        "Button:active",
        "Button:focus",
        "Button:disabled",
        "Toggle",
        "Toggle:hover",
        "Toggle:active",
        "Toggle:focus",
        "Toggle:checked",
        "Slider",
        "Slider:hover",
        "Slider:active",
        "Slider:focus",
        "ScrollView",
        "ScrollView:hover",
        "ScrollView:active",
        "ScrollView > Label",
        "TextField",
        "TextField:hover",
        "TextField:focus",
        ".hello-status > Label",
    ];

    /// <summary>The sheet parses with no error, holds 33 rules, and holds each selector text one time.</summary>
    [Fact]
    public void TheSheetHoldsEachRule()
    {
        StyleSheet sheet = ParseSheet();

        // When a phase adds a rule, update the count and record the cause in context.md.
        Assert.Equal(RuleCount, sheet.Rules.Length);
        Assert.Equal(RuleCount, SelectorTexts.Length);

        foreach (string selectorText in SelectorTexts)
        {
            int count = 0;
            foreach (ref readonly StyleRule rule in sheet.Rules)
            {
                if (rule.Selector.ToString() == selectorText)
                {
                    count++;
                }
            }

            Assert.True(count == 1, $"The selector {selectorText} occurs {count} times. The test expects 1.");
        }
    }

    /// <summary>
    /// The type rules of <see cref="Button"/> style a button in a row of the widget section. A hover rule and an
    /// active rule set the fill only. A focus rule sets the border color only, so the states combine.
    /// </summary>
    [Fact]
    public void TheButtonRulesStyleTheButton()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        Button button = tree.Button;

        // No state: the base rule gives each member.
        ComputedStyle style = button.Style;
        Assert.Equal(WidgetFill, style.BackgroundColor);
        Assert.Equal(WidgetBorder, style.BorderColor);
        Assert.Equal(TextColor, style.Color);
        Assert.Equal(16f, style.FontSize);
        Assert.Equal(Length.Px(6f), style.Padding.Top);
        Assert.Equal(Length.Px(12f), style.Padding.Right);
        Assert.Equal(Length.Px(6f), style.Padding.Bottom);
        Assert.Equal(Length.Px(12f), style.Padding.Left);
        Assert.Equal(Length.Px(2f), style.BorderWidth.Top);
        Assert.Equal(4f, style.CornerRadius.TopLeft);

        // Hover: the hover fill and the base border color.
        tree.SetStates(PseudoStates.Hover);
        Assert.Equal(WidgetHover, button.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, button.Style.BorderColor);

        // Active: the active fill. Button:active comes after Button:hover, so it wins while both apply.
        tree.SetStates(PseudoStates.Active);
        Assert.Equal(WidgetActive, button.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, button.Style.BorderColor);
        tree.SetStates(PseudoStates.Hover | PseudoStates.Active);
        Assert.Equal(WidgetActive, button.Style.BackgroundColor);

        // Focus: the focus border color and the base fill.
        tree.SetStates(PseudoStates.Focus);
        Assert.Equal(WidgetFill, button.Style.BackgroundColor);
        Assert.Equal(WidgetFocus, button.Style.BorderColor);

        // Hover and focus: the hover fill and the focus border color at the same time.
        tree.SetStates(PseudoStates.Hover | PseudoStates.Focus);
        Assert.Equal(WidgetHover, button.Style.BackgroundColor);
        Assert.Equal(WidgetFocus, button.Style.BorderColor);

        // Back to no state: the base values return.
        tree.SetStates(PseudoStates.None);
        Assert.Equal(WidgetFill, button.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, button.Style.BorderColor);
        Assert.Equal(TextColor, button.Style.Color);
    }

    /// <summary>
    /// The type rules of <see cref="Toggle"/> give the size and the paint. A hover rule and an active rule set the
    /// fill, the checked rule sets the fill and wins over both, and the focus rule sets the border color.
    /// </summary>
    [Fact]
    public void TheToggleRulesStyleTheToggle()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        Toggle toggle = tree.Toggle;

        // The size comes from the sheet, because the toggle has no own size.
        float4 rect = toggle.Layout.Rect;
        Assert.Equal(20f, rect.Z);
        Assert.Equal(20f, rect.W);

        Assert.Equal(WidgetFill, toggle.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, toggle.Style.BorderColor);
        Assert.Equal(Length.Px(2f), toggle.Style.BorderWidth.Top);
        Assert.Equal(4f, toggle.Style.CornerRadius.TopLeft);

        // Hover and active: the fill follows the state, and Toggle:active comes after Toggle:hover.
        tree.SetStates(toggle, PseudoStates.Hover);
        Assert.Equal(WidgetHover, toggle.Style.BackgroundColor);
        tree.SetStates(toggle, PseudoStates.Active);
        Assert.Equal(WidgetActive, toggle.Style.BackgroundColor);

        // Checked: the checked fill.
        toggle.Checked = true;
        tree.Context.Update(0f);
        Assert.Equal(ToggleChecked, toggle.Style.BackgroundColor);

        // Checked with hover and with active: the checked rule comes after both, so the checked fill stays.
        toggle.States = PseudoStates.Checked | PseudoStates.Hover;
        tree.Context.Update(0f);
        Assert.Equal(ToggleChecked, toggle.Style.BackgroundColor);
        toggle.States = PseudoStates.Checked | PseudoStates.Active;
        tree.Context.Update(0f);
        Assert.Equal(ToggleChecked, toggle.Style.BackgroundColor);

        // Focus: the focus border color, with the checked fill kept.
        toggle.States = PseudoStates.Checked | PseudoStates.Focus;
        tree.Context.Update(0f);
        Assert.Equal(ToggleChecked, toggle.Style.BackgroundColor);
        Assert.Equal(WidgetFocus, toggle.Style.BorderColor);

        // Focus on an unchecked toggle: the focus border color and the base fill.
        tree.SetStates(toggle, PseudoStates.Focus);
        Assert.Equal(WidgetFill, toggle.Style.BackgroundColor);
        Assert.Equal(WidgetFocus, toggle.Style.BorderColor);
    }

    /// <summary>
    /// The type rules of <see cref="Slider"/> give the size, the track, and the thumb color. A hover rule and an
    /// active rule set the thumb color, and the focus rule sets the border color.
    /// </summary>
    [Fact]
    public void TheSliderRulesStyleTheSlider()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        Slider slider = tree.Slider;

        float4 rect = slider.Layout.Rect;
        Assert.Equal(200f, rect.Z);
        Assert.Equal(20f, rect.W);

        // No state: the track fill, the thumb color, and the base border color.
        Assert.Equal(SliderTrack, slider.Style.BackgroundColor);
        Assert.Equal(SliderThumb, slider.Style.Color);
        Assert.Equal(WidgetBorder, slider.Style.BorderColor);
        Assert.Equal(4f, slider.Style.CornerRadius.TopLeft);

        tree.SetStates(slider, PseudoStates.Hover);
        Assert.Equal(SliderThumbHover, slider.Style.Color);
        Assert.Equal(SliderTrack, slider.Style.BackgroundColor);

        tree.SetStates(slider, PseudoStates.Active);
        Assert.Equal(SliderThumbActive, slider.Style.Color);

        // Hover and active: Slider:active comes after Slider:hover, so the active color wins.
        tree.SetStates(slider, PseudoStates.Hover | PseudoStates.Active);
        Assert.Equal(SliderThumbActive, slider.Style.Color);

        // Focus: the focus border color and the base thumb color.
        tree.SetStates(slider, PseudoStates.Focus);
        Assert.Equal(WidgetFocus, slider.Style.BorderColor);
        Assert.Equal(SliderThumb, slider.Style.Color);
    }

    /// <summary>
    /// The type rules of <see cref="ScrollView"/> give the size, the paint, and the item labels. A hover rule sets the
    /// border color, and an active rule sets the fill.
    /// </summary>
    [Fact]
    public void TheScrollViewRulesStyleTheView()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        ScrollView view = tree.ScrollView;

        // The size comes from the sheet, because the view has no own size.
        float4 rect = view.Layout.Rect;
        Assert.Equal(240f, rect.Z);
        Assert.Equal(80f, rect.W);

        // No state: the base fill and the base border color.
        Assert.Equal(WidgetFill, view.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, view.Style.BorderColor);
        Assert.Equal(FlexDirection.Column, view.Style.FlexDirection);
        Assert.Equal(Length.Px(4f), view.Style.Padding.Top);
        Assert.Equal(Length.Px(2f), view.Style.BorderWidth.Top);
        Assert.Equal(4f, view.Style.CornerRadius.TopLeft);

        // Hover: the hover border color and the base fill.
        tree.SetStates(view, PseudoStates.Hover);
        Assert.Equal(ScrollHoverBorder, view.Style.BorderColor);
        Assert.Equal(WidgetFill, view.Style.BackgroundColor);

        // Active: the active fill. The hover border color stays while both apply.
        tree.SetStates(view, PseudoStates.Hover | PseudoStates.Active);
        Assert.Equal(WidgetActive, view.Style.BackgroundColor);
        Assert.Equal(ScrollHoverBorder, view.Style.BorderColor);

        // Back to no state: the base values return.
        tree.SetStates(view, PseudoStates.None);
        Assert.Equal(WidgetFill, view.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, view.Style.BorderColor);

        // An item label: the text color and 16 pixels. The rule .hello-row > Label does not reach it.
        ComputedStyle item = view.Children[0].Style;
        Assert.Equal(16f, item.FontSize);
        Assert.Equal(TextColor, item.Color);
        Assert.Equal(Length.Undefined, item.Margin.Left);
    }

    /// <summary>
    /// The 8 item labels are taller than the 80 pixel view, so the view has a range to scroll. A set of a large offset
    /// clamps to the end of the content: above 0 and below the set value.
    /// </summary>
    [Fact]
    public void TheScrollContentIsTallerThanTheView()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        ScrollView view = tree.ScrollView;

        // The view keeps its fixed height, and it holds 8 items.
        Assert.Equal(8, view.Children.Count);
        Assert.Equal(80f, view.Layout.Rect.W);

        view.ScrollOffset = new float2(0f, 1000f);
        float offsetY = view.ScrollOffset.Y;
        Assert.True(offsetY > 0f, $"The offset Y is {offsetY}. The content must be taller than the view.");
        Assert.True(offsetY < 1000f, $"The offset Y is {offsetY}. The clamp must stop it below 1000.");

        // The content has no range on the x axis, because each item fits the width of the view.
        Assert.Equal(0f, view.ScrollOffset.X);
    }

    /// <summary>
    /// The type rules of <see cref="TextField"/> give the size and the paint. The hover rule sets the fill, and the
    /// focus rule sets the border color, so the states combine. The rule <c>.hello-status &gt; Label</c> styles the two
    /// status labels of the field.
    /// </summary>
    [Fact]
    public void TheTextFieldRulesStyleTheField()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        TextField field = tree.TextField;

        // The size comes from the sheet, because the field has no own size.
        float4 rect = field.Layout.Rect;
        Assert.Equal(240f, rect.Z);
        Assert.Equal(32f, rect.W);

        // No state: the base rule gives each member.
        ComputedStyle style = field.Style;
        Assert.Equal(WidgetFill, style.BackgroundColor);
        Assert.Equal(WidgetBorder, style.BorderColor);
        Assert.Equal(TextColor, style.Color);
        Assert.Equal(16f, style.FontSize);
        Assert.Equal(Length.Px(4f), style.Padding.Top);
        Assert.Equal(Length.Px(8f), style.Padding.Right);
        Assert.Equal(Length.Px(4f), style.Padding.Bottom);
        Assert.Equal(Length.Px(8f), style.Padding.Left);
        Assert.Equal(Length.Px(2f), style.BorderWidth.Top);
        Assert.Equal(4f, style.CornerRadius.TopLeft);

        // Hover: the hover fill and the base border color.
        tree.SetStates(field, PseudoStates.Hover);
        Assert.Equal(WidgetHover, field.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, field.Style.BorderColor);
        Assert.Equal(TextColor, field.Style.Color);

        // Focus: the focus border color and the base fill.
        tree.SetStates(field, PseudoStates.Focus);
        Assert.Equal(WidgetFill, field.Style.BackgroundColor);
        Assert.Equal(WidgetFocus, field.Style.BorderColor);

        // Hover and focus: the hover fill and the focus border color at the same time.
        tree.SetStates(field, PseudoStates.Hover | PseudoStates.Focus);
        Assert.Equal(WidgetHover, field.Style.BackgroundColor);
        Assert.Equal(WidgetFocus, field.Style.BorderColor);

        // Back to no state: the base values return.
        tree.SetStates(field, PseudoStates.None);
        Assert.Equal(WidgetFill, field.Style.BackgroundColor);
        Assert.Equal(WidgetBorder, field.Style.BorderColor);

        // The status labels: the text color, 16 pixels, and the left margin. The rule .hello-row > Label does not reach
        // them, because the parent is the status column.
        foreach (Label label in new[] { tree.TextLabel, tree.SubmitLabel })
        {
            Assert.Equal(16f, label.Style.FontSize);
            Assert.Equal(TextColor, label.Style.Color);
            Assert.Equal(Length.Px(12f), label.Style.Margin.Left);
        }

        // The status column has no rule. It keeps the default direction, a column.
        Assert.Equal(FlexDirection.Column, tree.StatusColumn.Style.FlexDirection);
        Assert.Equal(Length.Undefined, tree.StatusColumn.Style.Margin.Left);
    }

    /// <summary>
    /// After an update, the content box of the text field is at least as tall as one line of text at its font size, so
    /// the text and the caret fit. The content height is the height minus the padding and the border.
    /// </summary>
    [Fact]
    public void TheTextFieldHasRoomForOneLine()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        TextField field = tree.TextField;
        ComputedStyle style = field.Style;

        float contentHeight = field.Layout.Rect.W
            - style.Padding.Top.Value
            - style.Padding.Bottom.Value
            - style.BorderWidth.Top.Value
            - style.BorderWidth.Bottom.Value;
        float lineHeight = new ByteMeasurer().MeasureText(style.Font, style.FontSize, "W"u8, 0f).Y;

        Assert.True(
            contentHeight >= lineHeight,
            $"The content height is {contentHeight}. The line height is {lineHeight}.");

        // The line height of a font is at least its font size, so the content box must also hold the font size.
        Assert.True(
            contentHeight >= style.FontSize,
            $"The content height is {contentHeight}. The font size is {style.FontSize}.");
    }

    /// <summary>
    /// The rule <c>Button:disabled</c> comes after <c>Button:hover</c> and <c>Button:active</c>, so a disabled button
    /// keeps the disabled fill and the disabled text color under the pointer and under a press.
    /// </summary>
    [Fact]
    public void TheDisabledRuleWinsOverHover()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());
        Button button = tree.DisabledButton;

        tree.SetStates(button, PseudoStates.Disabled);
        Assert.Equal(DisabledFill, button.Style.BackgroundColor);
        Assert.Equal(DisabledText, button.Style.Color);

        tree.SetStates(button, PseudoStates.Disabled | PseudoStates.Hover);
        Assert.Equal(DisabledFill, button.Style.BackgroundColor);
        Assert.Equal(DisabledText, button.Style.Color);

        tree.SetStates(button, PseudoStates.Disabled | PseudoStates.Active);
        Assert.Equal(DisabledFill, button.Style.BackgroundColor);
        Assert.Equal(DisabledText, button.Style.Color);

        // An enabled button keeps the text color and the base fill.
        tree.SetStates(button, PseudoStates.None);
        Assert.Equal(WidgetFill, button.Style.BackgroundColor);
        Assert.Equal(TextColor, button.Style.Color);
    }

    /// <summary>
    /// The rule <c>.hello-row &gt; Label</c> styles a status label. The rule <c>.hello-panel &gt; Label</c> still
    /// styles a label that is a direct child of the panel.
    /// </summary>
    [Fact]
    public void TheRowLabelRuleKeepsThePanelLabelRule()
    {
        WidgetTree tree = WidgetTree.Build(ParseSheet());

        ComputedStyle status = tree.RowLabel.Style;
        Assert.Equal(16f, status.FontSize);
        Assert.Equal(TextColor, status.Color);
        Assert.Equal(Length.Px(0f), status.Margin.Top);
        Assert.Equal(Length.Px(12f), status.Margin.Left);

        ComputedStyle greeting = tree.PanelLabel.Style;
        Assert.Equal(26f, greeting.FontSize);
        Assert.Equal(TextColor, greeting.Color);

        // The rule .hello-row > Label does not reach the greeting, so the left margin stays undefined.
        Assert.Equal(Length.Undefined, greeting.Margin.Left);

        // The layout rules of the section and the row.
        Assert.Equal(FlexDirection.Column, tree.Section.Style.FlexDirection);
        Assert.Equal(Length.Px(12f), tree.Section.Style.Margin.Top);
        Assert.Equal(FlexDirection.Row, tree.Row.Style.FlexDirection);
        Assert.Equal(Align.Center, tree.Row.Style.AlignItems);
        Assert.Equal(Length.Px(8f), tree.Row.Style.Margin.Top);
    }

    /// <summary>Parses the linked sheet of the sample and checks that the parse has no error.</summary>
    private static StyleSheet ParseSheet()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "HelloScreen.css"));
        StyleParseResult result = new StyleSheetParser().Parse(bytes, "HelloScreen.css");

        Assert.Empty(result.Errors);
        Assert.True(result.Succeeded);
        return result.GetSheetOrThrow();
    }

    /// <summary>
    /// The tree of the tests: a root <c>.hello-slot-root</c>, a panel <c>.hello-panel</c> with a label, a section
    /// <c>.hello-widgets</c>, and a row <c>.hello-row</c> with a <see cref="Button"/> and a status label. Rows 2 to 5
    /// hold the toggle, the slider, the scroll view, and the text field with its status column <c>.hello-status</c>.
    /// </summary>
    private sealed class WidgetTree
    {
        public required UiContext Context { get; init; }

        public required Label PanelLabel { get; init; }

        public required Element Section { get; init; }

        public required Element Row { get; init; }

        public required Button Button { get; init; }

        public required Label RowLabel { get; init; }

        public required Button DisabledButton { get; init; }

        public required Toggle Toggle { get; init; }

        public required Slider Slider { get; init; }

        public required ScrollView ScrollView { get; init; }

        public required TextField TextField { get; init; }

        public required Element StatusColumn { get; init; }

        public required Label TextLabel { get; init; }

        public required Label SubmitLabel { get; init; }

        /// <summary>Builds the tree, adds the sheet, and runs one update.</summary>
        public static WidgetTree Build(StyleSheet sheet)
        {
            Button button = new(new Button.CreateInfo { Text = "Click me" });
            Button disabledButton = new(new Button.CreateInfo { Text = "Disabled" });
            Label rowLabel = new(new Label.CreateInfo { Text = "Button clicks: 0" }) { IsPickable = false };

            Element row = new();
            row.Classes.Add(ClassName.Intern("hello-row"));
            row.Children.Add(button);
            row.Children.Add(rowLabel);
            row.Children.Add(disabledButton);

            Toggle toggle = new(new Toggle.CreateInfo());
            Element toggleRow = new();
            toggleRow.Classes.Add(ClassName.Intern("hello-row"));
            toggleRow.Children.Add(toggle);

            Slider slider = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = 1f, Value = 50f });
            Element sliderRow = new();
            sliderRow.Classes.Add(ClassName.Intern("hello-row"));
            sliderRow.Children.Add(slider);

            ScrollView scrollView = new(default(ScrollView.CreateInfo));
            for (int i = 1; i <= 8; i++)
            {
                scrollView.Children.Add(new Label(new Label.CreateInfo { Text = "Item " + i }) { IsPickable = false });
            }

            Label scrollLabel = new(new Label.CreateInfo { Text = "Scroll offset: 0 px" }) { IsPickable = false };
            Element scrollRow = new();
            scrollRow.Classes.Add(ClassName.Intern("hello-row"));
            scrollRow.Children.Add(scrollView);
            scrollRow.Children.Add(scrollLabel);

            TextField textField = new(new TextField.CreateInfo { Capacity = 32 });
            Label textLabel = new(new Label.CreateInfo { Text = "Text: " }) { IsPickable = false };
            Label submitLabel = new(new Label.CreateInfo { Text = "Submitted: " }) { IsPickable = false };
            Element statusColumn = new();
            statusColumn.Classes.Add(ClassName.Intern("hello-status"));
            statusColumn.Children.Add(textLabel);
            statusColumn.Children.Add(submitLabel);
            Element textRow = new();
            textRow.Classes.Add(ClassName.Intern("hello-row"));
            textRow.Children.Add(textField);
            textRow.Children.Add(statusColumn);

            Element section = new();
            section.Classes.Add(ClassName.Intern("hello-widgets"));
            section.Children.Add(row);
            section.Children.Add(toggleRow);
            section.Children.Add(sliderRow);
            section.Children.Add(scrollRow);
            section.Children.Add(textRow);

            Label panelLabel = new(new Label.CreateInfo { Text = "Hello, brutal-ui" });
            Element panel = new();
            panel.Classes.Add(ClassName.Intern("hello-panel"));
            panel.Children.Add(panelLabel);
            panel.Children.Add(section);

            Element root = new();
            root.Classes.Add(ClassName.Intern("hello-slot-root"));
            root.Children.Add(panel);

            UiContext context = new(new UiContext.CreateInfo { Measurer = new ByteMeasurer() })
            {
                ViewportSize = new float2(1280f, 720f),
            };
            context.AddSheet(sheet);
            context.AddRoot(root);
            context.Update(0f);

            return new WidgetTree
            {
                Context = context,
                PanelLabel = panelLabel,
                Section = section,
                Row = row,
                Button = button,
                RowLabel = rowLabel,
                DisabledButton = disabledButton,
                Toggle = toggle,
                Slider = slider,
                ScrollView = scrollView,
                TextField = textField,
                StatusColumn = statusColumn,
                TextLabel = textLabel,
                SubmitLabel = submitLabel,
            };
        }

        /// <summary>Sets the states of the button and runs one update. The sheet has no transition.</summary>
        public void SetStates(PseudoStates states)
        {
            SetStates(Button, states);
        }

        /// <summary>Sets the states of an element and runs one update. The sheet has no transition.</summary>
        public void SetStates(Element element, PseudoStates states)
        {
            element.States = states;
            Context.Update(0f);
        }
    }

    /// <summary>A renderer that draws nothing. It measures 8 pixels for each byte and a line of 16 pixels.</summary>
    private sealed class ByteMeasurer : IUiRenderer
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
            return new float2(ByteWidth * utf8.Length, LineHeight);
        }

        public void End()
        {
        }
    }
}
