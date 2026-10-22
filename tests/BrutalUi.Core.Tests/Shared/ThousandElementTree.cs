using System;
using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The shared tree of 1000 elements: a game screen with a header, a sidebar, a grid, a stats panel, a footer, a
/// tooltip, a modal, and a hidden settings panel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Build"/> creates the tree, adds the root to a new <see cref="UiContext"/>, and sets the viewport. It
/// runs no <see cref="UiContext.Update(float)"/>, so a caller can measure the build alone. Each name is a string
/// literal, so the build creates no name string.
/// </para>
/// <para>
/// The sections, in build order: the root (1), the header (14), the body (1), the sidebar (151), the grid (361), the
/// stats panel (201), the footer (31), the tooltip (7), the modal (13), and the settings panel (220). The body holds
/// the sidebar, the grid, and the stats panel. The root holds the other sections. The settings panel is an absolute
/// overlay of the full screen, so it takes no space in the flow when it shows. The 109 text leaves use
/// <see cref="LabelText"/> or <see cref="ParagraphText"/>.
/// </para>
/// <para>
/// The build also sets the paint members, so the draw pass has work to do: a background on the root, the header, the
/// body, each sidebar item, each grid slot, each stat row, each footer slot, the tooltip, the modal, the settings panel,
/// and each settings row (so 1 + 1 + 1 + 50 + 120 + 40 + 10 + 1 + 1 + 1 + 73 = 299 fills). The sidebar, the grid, and
/// the modal clip with <see cref="Overflow.Hidden"/>. The tooltip has an opacity of <see cref="TooltipOpacity"/>, and
/// the modal has an opacity of <see cref="ModalOpacity"/>. Each footer key has a border of 1 pixel, and each grid slot
/// has a corner radius of <see cref="SlotCornerRadius"/>. The paint members change no layout. The overflow rule
/// <see cref="Overflow.Hidden"/> gives the same layout as <see cref="Overflow.Visible"/>, and the border is on a leaf
/// of fixed size (12 by 12), so its rect does not change. No other element has a border.
/// </para>
/// <para>
/// The build adds <see cref="Sheet"/> to the context. The sheet holds <see cref="RuleCount"/> rules: type rules,
/// class rules, class-on-type rules, a state rule, a child rule, a descendant rule, and a name rule. The root has the
/// class <c>root</c>, each sidebar item has <c>item</c>, each stat row and each settings row has <c>row</c>, and each
/// grid slot has <c>slot</c>. The fill of each grid slot comes from the <c>slot</c> rule, not from the inline set, so
/// the <see cref="SelectedClass"/> rule can change it. The always-on rules set only a fill that the inline set had
/// before, a border color with no border width, and text values (<see cref="ComputedStyle.Color"/>,
/// <see cref="ComputedStyle.FontSize"/>). The tree holds no label, so a text value changes no rect and no draw call.
/// Only the <see cref="SelectedClass"/> rule sets layout values, and the build puts no element in that class. The
/// <see cref="MutedClass"/> rule sets a text color. No always-on rule sets a text color on the sidebar or on an element
/// below it, so the sidebar labels inherit the color of the sidebar.
/// </para>
/// <para>
/// The last 4 rules use each selector form of the selector-matching feature. Each one sets only a border color on an
/// element with no border width, so it changes no rect and no draw call. The state rule <c>.slot:hover</c> and the
/// child rule <c>.slot:hover &gt; Element</c> match when <see cref="GridSlot"/> is in the state
/// <see cref="PseudoStates.Hover"/>. The descendant rule <c>.row:hover Element</c> matches when <see cref="StatRow"/> is
/// in that state. The name rule <c>#header</c> matches <see cref="Header"/> when its name is <see cref="HeaderName"/>.
/// The build gives the header that name, and no element gets the name <see cref="OtherHeaderName"/>. The build puts no
/// element in a state.
/// </para>
/// <para>
/// The benchmark project links this file, so the file uses no test framework type and no internal member of
/// <c>BrutalUi.Core</c>.
/// </para>
/// </remarks>
internal sealed class ThousandElementTree
{
    /// <summary>The number of elements in the tree.</summary>
    public const int ElementCount = 1000;

    /// <summary>The number of leaves with a measure callback.</summary>
    public const int TextLeafCount = 109;

    /// <summary>The width of the viewport after the build, in pixels.</summary>
    public const float ViewportWideWidth = 1280f;

    /// <summary>The second width of the viewport that the viewport change sets, in pixels.</summary>
    public const float ViewportNarrowWidth = 1024f;

    /// <summary>The height of the viewport, in pixels. No change sets another height.</summary>
    public const float ViewportHeight = 720f;

    /// <summary>The width of the value leaf of stat row 0 after the build, in percent of the row.</summary>
    public const float ValueNarrowPercent = 30f;

    /// <summary>The second width of the value leaf of stat row 0, in percent of the row.</summary>
    public const float ValueWidePercent = 40f;

    /// <summary>The width of grid slot 0 after the build, in pixels.</summary>
    public const float SlotNarrowWidth = 48f;

    /// <summary>The second width of grid slot 0, in pixels.</summary>
    public const float SlotWideWidth = 100f;

    /// <summary>The width of the tooltip after the build, in pixels.</summary>
    public const float TooltipNarrowWidth = 200f;

    /// <summary>The second width of the tooltip, in pixels.</summary>
    public const float TooltipWideWidth = 240f;

    /// <summary>The content width of each label text, in pixels.</summary>
    public const float LabelContentWidth = 60f;

    /// <summary>The content width of each paragraph text, in pixels.</summary>
    public const float ParagraphContentWidth = 300f;

    /// <summary>The line height of each text, in pixels.</summary>
    public const float LineHeight = 16f;

    /// <summary>The opacity of the tooltip.</summary>
    public const float TooltipOpacity = 0.5f;

    /// <summary>The opacity of the modal.</summary>
    public const float ModalOpacity = 0.9f;

    /// <summary>The corner radius of each grid slot, in pixels.</summary>
    public const float SlotCornerRadius = 4f;

    /// <summary>The border width of each footer key, in pixels.</summary>
    public const float KeyBorderWidth = 1f;

    /// <summary>The number of rows in the settings panel.</summary>
    public const int SettingsRowCount = 73;

    private static readonly Rgba8 RootColor = new(18, 20, 28);
    private static readonly Rgba8 HeaderColor = new(32, 36, 48);
    private static readonly Rgba8 BodyColor = new(24, 27, 36);
    private static readonly Rgba8 SidebarItemColor = new(40, 44, 58);
    private static readonly Rgba8 StatRowColor = new(36, 40, 52);
    private static readonly Rgba8 FooterSlotColor = new(44, 48, 64);
    private static readonly Rgba8 TooltipColor = new(10, 10, 14, 230);
    private static readonly Rgba8 ModalColor = new(30, 34, 46);
    private static readonly Rgba8 SettingsColor = new(20, 22, 30, 240);
    private static readonly Rgba8 SettingsRowColor = new(38, 42, 56);
    private static readonly Rgba8 KeyBorderColor = new(220, 200, 80);
    private static readonly Rgba8 ItemBorderColor = new(60, 66, 84);
    private static readonly Rgba8 RowBorderColor = new(56, 62, 80);
    private static readonly Rgba8 SlotBorderColor = new(72, 80, 104);
    private static readonly Rgba8 RowTextColor = new(200, 204, 214);
    private static readonly Rgba8 LabelTextColor = new(236, 238, 244);
    private static readonly Rgba8 SelectedLabelColor = new(255, 220, 120);

    /// <summary>The number of rules in <see cref="Sheet"/>.</summary>
    public const int RuleCount = 16;

    /// <summary>The name of <see cref="Header"/>. The name rule of <see cref="Sheet"/> selects it.</summary>
    public const string HeaderName = "header";

    /// <summary>
    /// The second name of <see cref="Header"/>. The name rule of <see cref="Sheet"/> does not select it. The build gives
    /// no element this name.
    /// </summary>
    /// <remarks>It is a constant, so a change of the header name creates no string.</remarks>
    public const string OtherHeaderName = "header-renamed";

    /// <summary>The font size that the type rule of <see cref="Element"/> sets on each element.</summary>
    public const float ElementFontSize = 14f;

    /// <summary>The minimum width that the <see cref="SelectedClass"/> rule sets, in pixels.</summary>
    /// <remarks>It is equal to <see cref="SlotNarrowWidth"/>, so it changes no rect of grid slot 0.</remarks>
    public const float SelectedMinWidth = SlotNarrowWidth;

    /// <summary>The text color that the <c>root</c> rule sets. Each element with no text color rule inherits it.</summary>
    public static readonly Rgba8 RootTextColor = new(220, 224, 232);

    /// <summary>The fill of each grid slot. The <c>slot</c> rule sets it.</summary>
    public static readonly Rgba8 SlotColor = new(52, 58, 76);

    /// <summary>The fill that the <see cref="SelectedClass"/> rule sets.</summary>
    public static readonly Rgba8 SelectedSlotColor = new(90, 120, 200);

    /// <summary>The text color that the <see cref="MutedClass"/> rule sets.</summary>
    public static readonly Rgba8 MutedTextColor = new(120, 124, 136);

    /// <summary>
    /// The border color that the state rule <c>.slot:hover</c> sets on a hovered grid slot. The slot has no border width.
    /// </summary>
    public static readonly Rgba8 SlotHoverBorderColor = new(140, 170, 240);

    /// <summary>
    /// The border color that the child rule <c>.slot:hover &gt; Element</c> sets on each child of a hovered grid slot.
    /// No child has a border width.
    /// </summary>
    public static readonly Rgba8 SlotChildBorderColor = new(110, 150, 230);

    /// <summary>
    /// The border color that the descendant rule <c>.row:hover Element</c> sets on each child and each grandchild of a
    /// hovered row. No element in a row has a border width.
    /// </summary>
    public static readonly Rgba8 RowDescendantBorderColor = new(230, 180, 90);

    /// <summary>The border color that the name rule <c>#header</c> sets on <see cref="Header"/>. The header has no border width.</summary>
    public static readonly Rgba8 HeaderBorderColor = new(96, 104, 132);

    /// <summary>Gets the context that holds the root. Its viewport is <see cref="ViewportWideWidth"/> by <see cref="ViewportHeight"/>.</summary>
    public required UiContext Context { get; init; }

    /// <summary>Gets the root of the tree.</summary>
    public required Element Root { get; init; }

    /// <summary>Gets each element of the tree, in build order. The root is at index 0.</summary>
    public required Element[] All { get; init; }

    /// <summary>Gets the value leaf of stat row 0. Its width is a percent of the row.</summary>
    public required Element StatValueLeaf { get; init; }

    /// <summary>
    /// Gets stat row 0. The descendant rule <c>.row:hover Element</c> reads its state at the parent and at the
    /// grandparent of an element.
    /// </summary>
    public required Element StatRow { get; init; }

    /// <summary>
    /// Gets the fill leaf of stat row 0. Its grandparent is <see cref="StatRow"/>, so the descendant rule matches it at
    /// the grandparent.
    /// </summary>
    public required Element StatFill { get; init; }

    /// <summary>Gets slot 0 of the wrapped grid.</summary>
    public required Element GridSlot { get; init; }

    /// <summary>Gets the icon of grid slot 0. It is a child of <see cref="GridSlot"/>.</summary>
    public required Element GridIcon { get; init; }

    /// <summary>
    /// Gets the header. After the build, its name is <see cref="HeaderName"/>, and no other element has that name.
    /// </summary>
    public required Element Header { get; init; }

    /// <summary>Gets the absolute tooltip.</summary>
    public required Element Tooltip { get; init; }

    /// <summary>Gets the settings panel. Its <see cref="Display"/> is <see cref="Display.None"/> after the build.</summary>
    public required Element Settings { get; init; }

    /// <summary>Gets the sidebar column that holds the 50 items.</summary>
    public required Element Sidebar { get; init; }

    /// <summary>Gets the text label of sidebar item 0.</summary>
    public required Element SidebarLabel { get; init; }

    /// <summary>Gets the name text leaf of stat row 0.</summary>
    public required Element StatName { get; init; }

    /// <summary>Gets the first paragraph leaf of the modal.</summary>
    public required Element Paragraph { get; init; }

    /// <summary>Gets the callback of each label text leaf. Its content width is <see cref="LabelContentWidth"/>.</summary>
    public required TextMeasure LabelText { get; init; }

    /// <summary>Gets the callback of each paragraph leaf. Its content width is <see cref="ParagraphContentWidth"/>.</summary>
    public required TextMeasure ParagraphText { get; init; }

    /// <summary>Gets the sheet that the build adds to <see cref="Context"/>. It holds <see cref="RuleCount"/> rules.</summary>
    public required StyleSheet Sheet { get; init; }

    /// <summary>
    /// Gets the id of the class <c>selected</c>. Its rule sets <see cref="SelectedSlotColor"/>, a width of
    /// <see cref="SlotWideWidth"/>, and a minimum width of <see cref="SelectedMinWidth"/>. The build puts no element in
    /// the class.
    /// </summary>
    public required ClassId SelectedClass { get; init; }

    /// <summary>
    /// Gets the id of the class <c>muted</c>. Its rule sets the text color <see cref="MutedTextColor"/>. The build puts
    /// no element in the class.
    /// </summary>
    public required ClassId MutedClass { get; init; }

    /// <summary>
    /// Builds the tree, adds the root to a new context, and sets the viewport. It runs no update.
    /// </summary>
    /// <returns>The tree and the handles of the parts that a test changes.</returns>
    /// <exception cref="InvalidOperationException">The tree does not hold <see cref="ElementCount"/> elements.</exception>
    public static ThousandElementTree Build()
    {
        Collector c = new(new Element[ElementCount]);
        TextMeasure labelText = new(LabelContentWidth, LineHeight);
        TextMeasure paragraphText = new(ParagraphContentWidth, LineHeight);

        // 1. The root: a column with a padding of 8.
        Element root = c.Make("root");
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.Padding = LengthEdges.All(8f);
        root.Inline.BackgroundColor = RootColor;
        root.Classes.Add(ClassName.Intern("root"));

        Element header = BuildHeader(c, root, labelText);

        // 3. The body: a row that takes the free height. It can shrink, so it fits between the header and the footer.
        Element body = c.Add(root, "body");
        body.Inline.FlexDirection = FlexDirection.Row;
        body.Inline.FlexGrow = 1f;
        body.Inline.FlexShrink = 1f;
        body.Inline.BackgroundColor = BodyColor;

        Element sidebar = BuildSidebar(c, body, labelText, out Element sidebarLabel);
        Element gridSlot = BuildGrid(c, body);
        Element statValueLeaf = BuildStats(c, body, labelText, out Element statName, out Element statFill);
        BuildFooter(c, root);
        Element tooltip = BuildTooltip(c, root, labelText);
        Element paragraph = BuildModal(c, root, labelText, paragraphText);
        Element settings = BuildSettings(c, root);

        if (c.Count != ElementCount)
        {
            throw new InvalidOperationException("The tree must hold exactly 1000 elements.");
        }

        ClassId selected = ClassName.Intern("selected");
        ClassId muted = ClassName.Intern("muted");
        StyleSheet sheet = BuildSheet(selected, muted);

        UiContext context = new();
        context.ViewportSize = new float2(ViewportWideWidth, ViewportHeight);
        context.AddRoot(root);
        context.AddSheet(sheet);

        return new ThousandElementTree
        {
            Context = context,
            Root = root,
            All = c.All,
            StatValueLeaf = statValueLeaf,
            StatRow = statValueLeaf.Parent!,
            StatFill = statFill,
            GridSlot = gridSlot,
            GridIcon = gridSlot.Children[0],
            Header = header,
            Tooltip = tooltip,
            Settings = settings,
            Sidebar = sidebar,
            SidebarLabel = sidebarLabel,
            StatName = statName,
            Paragraph = paragraph,
            LabelText = labelText,
            ParagraphText = paragraphText,
            Sheet = sheet,
            SelectedClass = selected,
            MutedClass = muted,
        };
    }

    /// <summary>
    /// Builds a second sheet with three rules that hold only transitions. A test adds it to <see cref="Context"/>, so the
    /// style step of the allocation gate runs tweens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rules set no property value, so each target stays the value of <see cref="Sheet"/>. <see cref="Sheet"/> keeps
    /// its <see cref="RuleCount"/> rules. The build does not add this sheet. The rules, in add order:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <c>.slot</c>: <see cref="StyleProperty.BackgroundColor"/> 0.1 s <see cref="TransitionEasing.EaseOut"/> and
    /// <see cref="StyleProperty.BorderColor"/> 0.5 s <see cref="TransitionEasing.EaseInOut"/>. The toggle of the class
    /// <c>selected</c> fades the fill of grid slot 0. The fill tween ends before the next style step, because 9 steps of
    /// 0.016 s (0.144 s) lie between two style steps. The toggle of the hover state fades the border color of grid slot
    /// 0. The border tween runs longer than 0.144 s, so the next style step turns it.
    /// </description></item>
    /// <item><description>
    /// <c>.muted</c>: <see cref="StyleProperty.Color"/> 0.1 s <see cref="TransitionEasing.Linear"/>. The toggle of the
    /// class <c>muted</c> fades the text color of the sidebar, and the follow walk of the transition pass writes the
    /// color into each sidebar item and each sidebar label. When the class goes, the rule does not match, so the color
    /// changes at once.
    /// </description></item>
    /// <item><description>
    /// <c>#header</c>: <see cref="StyleProperty.BorderColor"/> 0.3 s <see cref="TransitionEasing.EaseIn"/>. The name
    /// change back to <see cref="HeaderName"/> fades the border color of <see cref="Header"/>. The next name change
    /// comes after 0.144 s, while the tween runs. The rule then does not match, so the style step ends the tween.
    /// </description></item>
    /// </list>
    /// </remarks>
    /// <returns>A new sheet with the three rules.</returns>
    public static StyleSheet BuildTransitionSheet()
    {
        ClassId slotClass = ClassName.Intern("slot");
        ClassId mutedClass = ClassName.Intern("muted");
        StyleProperties none = default;
        StyleSheet.Builder builder = new();

        // 1. Each grid slot: the fill and the border color.
        StyleTransitions transitions = default;
        transitions.Set(StyleProperty.BackgroundColor, new StyleTransition(0.1f, TransitionEasing.EaseOut));
        transitions.Set(StyleProperty.BorderColor, new StyleTransition(0.5f, TransitionEasing.EaseInOut));
        builder.Add(Selector.ForClass(slotClass), in none, in transitions);

        // 2. A muted element: the text color.
        transitions = default;
        transitions.Set(StyleProperty.Color, new StyleTransition(0.1f, TransitionEasing.Linear));
        builder.Add(Selector.ForClass(mutedClass), in none, in transitions);

        // 3. The header by its name: the border color.
        transitions = default;
        transitions.Set(StyleProperty.BorderColor, new StyleTransition(0.3f, TransitionEasing.EaseIn));
        builder.Add(Selector.ForName(HeaderName), in none, in transitions);

        return builder.Build();
    }

    /// <summary>
    /// Builds the sheet of <see cref="RuleCount"/> rules: 2 type rules, 6 class rules, 4 class-on-type rules, 1 state
    /// rule, 1 child rule, 1 descendant rule, and 1 name rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>selected</c> rule and the <c>slot</c> rule have the same specificity. The <c>selected</c> rule comes later,
    /// so its fill wins on a grid slot in both classes. The inline width of grid slot 0 wins over the width of the
    /// <c>selected</c> rule, so only the minimum width reaches the computed style. That value is
    /// <see cref="SlotNarrowWidth"/>, so it changes no rect.
    /// </para>
    /// <para>
    /// The two rules of the type <c>Label</c> match no element of this tree. They add work to the match loop only.
    /// </para>
    /// <para>
    /// Rules 13 to 16 set only a border color, and no element that they match has a border width. The state rule
    /// <c>.slot:hover</c> (specificity 512) wins over the border color of rule 11 (257). The subject of the child rule and
    /// of the descendant rule is the type <c>Element</c>, so each element of the tree tests the parent part, and the
    /// descendant rule also tests the grandparent part when the parent part misses. The name rule selects
    /// <see cref="Header"/>, which has no other border color rule.
    /// </para>
    /// </remarks>
    private static StyleSheet BuildSheet(ClassId selected, ClassId muted)
    {
        ElementTypeId elementType = ElementType.Intern("Element");
        ElementTypeId labelType = ElementType.Intern("Label");
        ClassId rootClass = ClassName.Intern("root");
        ClassId itemClass = ClassName.Intern("item");
        ClassId rowClass = ClassName.Intern("row");
        ClassId slotClass = ClassName.Intern("slot");
        StyleSheet.Builder builder = new();

        // 1. Each element: a font size. No element of the tree is a label, so no measure reads it.
        StyleProperties properties = default;
        properties.FontSize = ElementFontSize;
        builder.Add(Selector.ForType(elementType), in properties);

        // 2. Each label: a color and a font size. The tree holds no label.
        properties = default;
        properties.Color = LabelTextColor;
        properties.FontSize = 16f;
        builder.Add(Selector.ForType(labelType), in properties);

        // 3. The root: the text color that each element inherits.
        properties = default;
        properties.Color = RootTextColor;
        builder.Add(Selector.ForClass(rootClass), in properties);

        // 4. Each sidebar item: a border color with no border width, so the draw does not change.
        properties = default;
        properties.BorderColor = ItemBorderColor;
        builder.Add(Selector.ForClass(itemClass), in properties);

        // 5. Each stat row and each settings row: a text color. No row is in the sidebar.
        properties = default;
        properties.Color = RowTextColor;
        builder.Add(Selector.ForClass(rowClass), in properties);

        // 6. Each grid slot: the fill. The inline set of the slot holds no fill.
        properties = default;
        properties.BackgroundColor = SlotColor;
        builder.Add(Selector.ForClass(slotClass), in properties);

        // 7. The selected slot: a fill and two width values. It comes after rule 6, so its fill wins.
        properties = default;
        properties.BackgroundColor = SelectedSlotColor;
        properties.Width = SlotWideWidth;
        properties.MinWidth = SelectedMinWidth;
        builder.Add(Selector.ForClass(selected), in properties);

        // 8. A muted element: a text color that each descendant inherits.
        properties = default;
        properties.Color = MutedTextColor;
        builder.Add(Selector.ForClass(muted), in properties);

        // 9. Each sidebar item as an element: a font size.
        properties = default;
        properties.FontSize = 13f;
        builder.Add(Selector.ForTypeAndClass(elementType, itemClass), in properties);

        // 10. Each row as an element: a font size and a border color with no border width.
        properties = default;
        properties.FontSize = 12f;
        properties.BorderColor = RowBorderColor;
        builder.Add(Selector.ForTypeAndClass(elementType, rowClass), in properties);

        // 11. Each grid slot as an element: a border color with no border width. It sets no fill, so rule 7 still wins.
        properties = default;
        properties.BorderColor = SlotBorderColor;
        builder.Add(Selector.ForTypeAndClass(elementType, slotClass), in properties);

        // 12. A selected label: a color. The tree holds no label.
        properties = default;
        properties.Color = SelectedLabelColor;
        builder.Add(Selector.ForTypeAndClass(labelType, selected), in properties);

        // 13. A hovered grid slot: a border color with no border width. The state rule wins over rule 11.
        CompoundSelector hoveredSlot = new() { Class = slotClass, States = PseudoStates.Hover };
        properties = default;
        properties.BorderColor = SlotHoverBorderColor;
        builder.Add(Selector.For(in hoveredSlot), in properties);

        // 14. Each child of a hovered grid slot: a border color. The icon and the badge have no border width.
        CompoundSelector anyElement = new() { Type = elementType };
        properties = default;
        properties.BorderColor = SlotChildBorderColor;
        builder.Add(Selector.Child(in hoveredSlot, in anyElement), in properties);

        // 15. Each child and each grandchild of a hovered row: a border color. No element in a row has a border width.
        CompoundSelector hoveredRow = new() { Class = rowClass, States = PseudoStates.Hover };
        properties = default;
        properties.BorderColor = RowDescendantBorderColor;
        builder.Add(Selector.Descendant(in hoveredRow, in anyElement), in properties);

        // 16. The header by its name: a border color with no border width.
        properties = default;
        properties.BorderColor = HeaderBorderColor;
        builder.Add(Selector.ForName(HeaderName), in properties);

        return builder.Build();
    }

    /// <summary>2. The header: a row of height 48 with a title and 6 buttons. 14 elements.</summary>
    /// <returns>The header. Its name is <see cref="HeaderName"/>.</returns>
    private static Element BuildHeader(Collector c, Element root, TextMeasure labelText)
    {
        Element header = c.Add(root, HeaderName);
        header.Inline.FlexDirection = FlexDirection.Row;
        header.Inline.Height = 48f;
        header.Inline.JustifyContent = Justify.SpaceBetween;
        header.Inline.AlignItems = Align.Center;
        header.Inline.BackgroundColor = HeaderColor;

        Element title = c.Add(header, "header-title");
        title.Inline.FlexGrow = 1f;
        title.Layout.Measure = labelText.Callback;

        for (int i = 0; i < 6; i++)
        {
            Element button = c.Add(header, "header-button");
            button.Inline.Width = 80f;
            button.Inline.Height = 32f;
            button.Inline.JustifyContent = Justify.Center;
            button.Inline.AlignItems = Align.Center;

            Element label = c.Add(button, "header-button-label");
            label.Layout.Measure = labelText.Callback;
        }

        return header;
    }

    /// <summary>4. The sidebar: a column of 20 percent with 50 items. 151 elements.</summary>
    private static Element BuildSidebar(Collector c, Element body, TextMeasure labelText, out Element firstLabel)
    {
        Element sidebar = c.Add(body, "sidebar");
        sidebar.Inline.FlexDirection = FlexDirection.Column;
        sidebar.Inline.Width = Length.Percent(20f);
        sidebar.Inline.Padding = LengthEdges.All(Length.Percent(1f));
        sidebar.Inline.Overflow = Overflow.Hidden;

        ClassId itemClass = ClassName.Intern("item");
        firstLabel = null!;
        for (int i = 0; i < 50; i++)
        {
            Element item = c.Add(sidebar, "sidebar-item");
            item.Classes.Add(itemClass);
            item.Inline.FlexDirection = FlexDirection.Row;
            item.Inline.Height = 24f;
            item.Inline.AlignItems = Align.Center;
            item.Inline.BackgroundColor = SidebarItemColor;

            Element icon = c.Add(item, "sidebar-icon");
            icon.Inline.Width = 16f;
            icon.Inline.Height = 16f;

            Element label = c.Add(item, "sidebar-label");
            label.Inline.FlexGrow = 1f;
            label.Layout.Measure = labelText.Callback;
            if (i == 0)
            {
                firstLabel = label;
            }
        }

        return sidebar;
    }

    /// <summary>
    /// 5. The grid: a wrapped row with 120 slots. Each slot holds an icon and an absolute badge. 361 elements.
    /// </summary>
    /// <remarks>The grid can shrink. Its content basis is wider than the free width of the body.</remarks>
    private static Element BuildGrid(Collector c, Element body)
    {
        Element grid = c.Add(body, "grid");
        grid.Inline.FlexDirection = FlexDirection.Row;
        grid.Inline.FlexWrap = FlexWrap.Wrap;
        grid.Inline.FlexGrow = 1f;
        grid.Inline.FlexShrink = 1f;
        grid.Inline.AlignContent = Align.SpaceBetween;
        grid.Inline.Overflow = Overflow.Hidden;

        ClassId slotClass = ClassName.Intern("slot");
        Element firstSlot = null!;
        for (int i = 0; i < 120; i++)
        {
            // The fill comes from the slot rule of the sheet.
            Element slot = c.Add(grid, "grid-slot");
            slot.Classes.Add(slotClass);
            slot.Inline.Width = SlotNarrowWidth;
            slot.Inline.Height = 48f;
            slot.Inline.Margin = LengthEdges.All(2f);
            slot.Inline.AlignItems = Align.Center;
            slot.Inline.JustifyContent = Justify.Center;
            slot.Inline.CornerRadius = Corners.All(SlotCornerRadius);

            Element icon = c.Add(slot, "grid-icon");
            icon.Inline.Width = Length.Percent(80f);
            icon.Inline.Height = Length.Percent(80f);

            Element badge = c.Add(slot, "grid-badge");
            badge.Inline.Position = PositionType.Absolute;
            badge.Inline.Right = 2f;
            badge.Inline.Bottom = 2f;
            badge.Inline.Width = 12f;
            badge.Inline.Height = 12f;
            if (i == 0)
            {
                firstSlot = slot;
            }
        }

        return firstSlot;
    }

    /// <summary>6. The stats panel: a column of 25 percent with 40 rows. 201 elements.</summary>
    /// <returns>The value leaf of stat row 0.</returns>
    private static Element BuildStats(
        Collector c,
        Element body,
        TextMeasure labelText,
        out Element firstName,
        out Element firstFill)
    {
        Element stats = c.Add(body, "stats");
        stats.Inline.FlexDirection = FlexDirection.Column;
        stats.Inline.Width = Length.Percent(25f);
        stats.Inline.MaxWidth = 320f;

        ClassId rowClass = ClassName.Intern("row");
        firstName = null!;
        firstFill = null!;
        Element firstValue = null!;
        for (int i = 0; i < 40; i++)
        {
            Element row = c.Add(stats, "stat-row");
            row.Classes.Add(rowClass);
            row.Inline.FlexDirection = FlexDirection.Row;
            row.Inline.Height = 20f;
            row.Inline.AlignItems = Align.Center;
            row.Inline.BackgroundColor = StatRowColor;

            Element name = c.Add(row, "stat-name");
            name.Inline.FlexGrow = 1f;
            name.Layout.Measure = labelText.Callback;

            Element value = c.Add(row, "stat-value");
            value.Inline.Width = Length.Percent(ValueNarrowPercent);
            value.Inline.Height = 16f;

            Element bar = c.Add(row, "stat-bar");
            bar.Inline.FlexDirection = FlexDirection.Row;
            bar.Inline.Width = 40f;
            bar.Inline.Height = 6f;

            Element fill = c.Add(bar, "stat-fill");
            fill.Inline.Width = Length.Percent(50f);
            fill.Inline.Height = Length.Percent(100f);
            if (i == 0)
            {
                firstName = name;
                firstFill = fill;
                firstValue = value;
            }
        }

        return firstValue;
    }

    /// <summary>7. The footer: a centered row of height 64 with 10 slots. 31 elements.</summary>
    private static void BuildFooter(Collector c, Element root)
    {
        Element footer = c.Add(root, "footer");
        footer.Inline.FlexDirection = FlexDirection.Row;
        footer.Inline.Height = 64f;
        footer.Inline.JustifyContent = Justify.Center;
        footer.Inline.AlignItems = Align.Center;

        for (int i = 0; i < 10; i++)
        {
            Element slot = c.Add(footer, "footer-slot");
            slot.Inline.Width = 56f;
            slot.Inline.Height = 56f;
            slot.Inline.BackgroundColor = FooterSlotColor;

            Element icon = c.Add(slot, "footer-icon");
            icon.Inline.FlexGrow = 1f;

            Element key = c.Add(slot, "footer-key");
            key.Inline.Position = PositionType.Absolute;
            key.Inline.Left = 2f;
            key.Inline.Top = 2f;
            key.Inline.Width = 12f;
            key.Inline.Height = 12f;
            key.Inline.BorderWidth = LengthEdges.All(KeyBorderWidth);
            key.Inline.BorderColor = KeyBorderColor;
        }
    }

    /// <summary>8. The tooltip: an absolute column with 6 text lines. 7 elements.</summary>
    private static Element BuildTooltip(Collector c, Element root, TextMeasure labelText)
    {
        Element tooltip = c.Add(root, "tooltip");
        tooltip.Inline.Position = PositionType.Absolute;
        tooltip.Inline.Left = 40f;
        tooltip.Inline.Top = 40f;
        tooltip.Inline.Width = TooltipNarrowWidth;
        tooltip.Inline.FlexDirection = FlexDirection.Column;
        tooltip.Inline.Padding = LengthEdges.All(4f);
        tooltip.Inline.BackgroundColor = TooltipColor;
        tooltip.Inline.Opacity = TooltipOpacity;

        for (int i = 0; i < 6; i++)
        {
            Element line = c.Add(tooltip, "tooltip-line");
            line.Layout.Measure = labelText.Callback;
        }

        return tooltip;
    }

    /// <summary>
    /// 9. The modal: an absolute column of 50 percent with a title row, a body of 3 paragraphs, and a button row.
    /// 13 elements.
    /// </summary>
    private static Element BuildModal(Collector c, Element root, TextMeasure labelText, TextMeasure paragraphText)
    {
        Element modal = c.Add(root, "modal");
        modal.Inline.Position = PositionType.Absolute;
        modal.Inline.Width = Length.Percent(50f);
        modal.Inline.Height = Length.Percent(50f);
        modal.Inline.Left = Length.Percent(25f);
        modal.Inline.Top = Length.Percent(25f);
        modal.Inline.FlexDirection = FlexDirection.Column;
        modal.Inline.Padding = LengthEdges.All(8f);
        modal.Inline.BackgroundColor = ModalColor;
        modal.Inline.Overflow = Overflow.Hidden;
        modal.Inline.Opacity = ModalOpacity;

        Element titleRow = c.Add(modal, "modal-title-row");
        titleRow.Inline.FlexDirection = FlexDirection.Row;
        titleRow.Inline.Height = 32f;
        titleRow.Inline.AlignItems = Align.Center;

        Element title = c.Add(titleRow, "modal-title");
        title.Inline.FlexGrow = 1f;
        title.Layout.Measure = labelText.Callback;

        Element close = c.Add(titleRow, "modal-close");
        close.Inline.Width = 24f;
        close.Inline.Height = 24f;

        Element modalBody = c.Add(modal, "modal-body");
        modalBody.Inline.FlexDirection = FlexDirection.Column;
        modalBody.Inline.FlexGrow = 1f;

        Element firstParagraph = null!;
        for (int i = 0; i < 3; i++)
        {
            Element paragraph = c.Add(modalBody, "modal-paragraph");
            paragraph.Inline.Margin = LengthEdges.All(4f);
            paragraph.Layout.Measure = paragraphText.Callback;
            if (i == 0)
            {
                firstParagraph = paragraph;
            }
        }

        Element buttonRow = c.Add(modal, "modal-button-row");
        buttonRow.Inline.FlexDirection = FlexDirection.Row;
        buttonRow.Inline.JustifyContent = Justify.FlexEnd;

        for (int i = 0; i < 2; i++)
        {
            Element button = c.Add(buttonRow, "modal-button");
            button.Inline.Width = 80f;
            button.Inline.Height = 32f;
            button.Inline.AlignItems = Align.Center;
            button.Inline.JustifyContent = Justify.Center;

            Element label = c.Add(button, "modal-button-label");
            label.Layout.Measure = labelText.Callback;
        }

        return firstParagraph;
    }

    /// <summary>
    /// 10. The settings panel: a hidden full-screen overlay with 73 rows. The labels have no callback. 220 elements.
    /// </summary>
    /// <remarks>The overlay is absolute, so the panel takes no space in the flow when it shows.</remarks>
    private static Element BuildSettings(Collector c, Element root)
    {
        Element settings = c.Add(root, "settings");
        settings.Inline.Display = Display.None;
        settings.Inline.Position = PositionType.Absolute;
        settings.Inline.Left = 0f;
        settings.Inline.Top = 0f;
        settings.Inline.Width = Length.Percent(100f);
        settings.Inline.Height = Length.Percent(100f);
        settings.Inline.FlexDirection = FlexDirection.Column;
        settings.Inline.BackgroundColor = SettingsColor;

        ClassId rowClass = ClassName.Intern("row");
        for (int i = 0; i < SettingsRowCount; i++)
        {
            Element row = c.Add(settings, "settings-row");
            row.Classes.Add(rowClass);
            row.Inline.FlexDirection = FlexDirection.Row;
            row.Inline.BackgroundColor = SettingsRowColor;

            Element label = c.Add(row, "settings-label");
            label.Inline.FlexGrow = 1f;

            Element toggle = c.Add(row, "settings-toggle");
            toggle.Inline.Width = 24f;
            toggle.Inline.Height = 12f;
        }

        return settings;
    }

    /// <summary>Creates the elements and records each one in build order.</summary>
    private sealed class Collector(Element[] all)
    {
        /// <summary>Gets the array of the elements, in build order.</summary>
        public Element[] All { get; } = all;

        /// <summary>Gets the number of elements that the collector created.</summary>
        public int Count { get; private set; }

        /// <summary>Creates an element with no parent and records it.</summary>
        /// <param name="name">The name of the element. Give a string literal.</param>
        /// <returns>The new element.</returns>
        /// <exception cref="InvalidOperationException">The collector already holds each element of the array.</exception>
        public Element Make(string name)
        {
            if (Count == All.Length)
            {
                throw new InvalidOperationException("The tree must hold exactly 1000 elements.");
            }

            Element element = new() { Name = name };
            All[Count] = element;
            Count++;
            return element;
        }

        /// <summary>Creates an element, adds it at the end of the children of the parent, and records it.</summary>
        /// <param name="parent">The parent of the new element.</param>
        /// <param name="name">The name of the element. Give a string literal.</param>
        /// <returns>The new element.</returns>
        public Element Add(Element parent, string name)
        {
            Element element = Make(name);
            parent.Children.Add(element);
            return element;
        }
    }
}
