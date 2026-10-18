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

    /// <summary>Gets the context that holds the root. Its viewport is <see cref="ViewportWideWidth"/> by <see cref="ViewportHeight"/>.</summary>
    public required UiContext Context { get; init; }

    /// <summary>Gets the root of the tree.</summary>
    public required Element Root { get; init; }

    /// <summary>Gets each element of the tree, in build order. The root is at index 0.</summary>
    public required Element[] All { get; init; }

    /// <summary>Gets the value leaf of stat row 0. Its width is a percent of the row.</summary>
    public required Element StatValueLeaf { get; init; }

    /// <summary>Gets slot 0 of the wrapped grid.</summary>
    public required Element GridSlot { get; init; }

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
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.Padding = LengthEdges.All(8f);

        BuildHeader(c, root, labelText);

        // 3. The body: a row that takes the free height. It can shrink, so it fits between the header and the footer.
        Element body = c.Add(root, "body");
        body.Style.FlexDirection = FlexDirection.Row;
        body.Style.FlexGrow = 1f;
        body.Style.FlexShrink = 1f;

        Element sidebar = BuildSidebar(c, body, labelText, out Element sidebarLabel);
        Element gridSlot = BuildGrid(c, body);
        Element statValueLeaf = BuildStats(c, body, labelText, out Element statName);
        BuildFooter(c, root);
        Element tooltip = BuildTooltip(c, root, labelText);
        Element paragraph = BuildModal(c, root, labelText, paragraphText);
        Element settings = BuildSettings(c, root);

        if (c.Count != ElementCount)
        {
            throw new InvalidOperationException("The tree must hold exactly 1000 elements.");
        }

        UiContext context = new();
        context.ViewportSize = new float2(ViewportWideWidth, ViewportHeight);
        context.AddRoot(root);

        return new ThousandElementTree
        {
            Context = context,
            Root = root,
            All = c.All,
            StatValueLeaf = statValueLeaf,
            GridSlot = gridSlot,
            Tooltip = tooltip,
            Settings = settings,
            Sidebar = sidebar,
            SidebarLabel = sidebarLabel,
            StatName = statName,
            Paragraph = paragraph,
            LabelText = labelText,
            ParagraphText = paragraphText,
        };
    }

    /// <summary>2. The header: a row of height 48 with a title and 6 buttons. 14 elements.</summary>
    private static void BuildHeader(Collector c, Element root, TextMeasure labelText)
    {
        Element header = c.Add(root, "header");
        header.Style.FlexDirection = FlexDirection.Row;
        header.Style.Height = 48f;
        header.Style.JustifyContent = Justify.SpaceBetween;
        header.Style.AlignItems = Align.Center;

        Element title = c.Add(header, "header-title");
        title.Style.FlexGrow = 1f;
        title.Layout.Measure = labelText.Callback;

        for (int i = 0; i < 6; i++)
        {
            Element button = c.Add(header, "header-button");
            button.Style.Width = 80f;
            button.Style.Height = 32f;
            button.Style.JustifyContent = Justify.Center;
            button.Style.AlignItems = Align.Center;

            Element label = c.Add(button, "header-button-label");
            label.Layout.Measure = labelText.Callback;
        }
    }

    /// <summary>4. The sidebar: a column of 20 percent with 50 items. 151 elements.</summary>
    private static Element BuildSidebar(Collector c, Element body, TextMeasure labelText, out Element firstLabel)
    {
        Element sidebar = c.Add(body, "sidebar");
        sidebar.Style.FlexDirection = FlexDirection.Column;
        sidebar.Style.Width = Length.Percent(20f);
        sidebar.Style.Padding = LengthEdges.All(Length.Percent(1f));

        firstLabel = null!;
        for (int i = 0; i < 50; i++)
        {
            Element item = c.Add(sidebar, "sidebar-item");
            item.Style.FlexDirection = FlexDirection.Row;
            item.Style.Height = 24f;
            item.Style.AlignItems = Align.Center;

            Element icon = c.Add(item, "sidebar-icon");
            icon.Style.Width = 16f;
            icon.Style.Height = 16f;

            Element label = c.Add(item, "sidebar-label");
            label.Style.FlexGrow = 1f;
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
        grid.Style.FlexDirection = FlexDirection.Row;
        grid.Style.FlexWrap = FlexWrap.Wrap;
        grid.Style.FlexGrow = 1f;
        grid.Style.FlexShrink = 1f;
        grid.Style.AlignContent = Align.SpaceBetween;

        Element firstSlot = null!;
        for (int i = 0; i < 120; i++)
        {
            Element slot = c.Add(grid, "grid-slot");
            slot.Style.Width = SlotNarrowWidth;
            slot.Style.Height = 48f;
            slot.Style.Margin = LengthEdges.All(2f);
            slot.Style.AlignItems = Align.Center;
            slot.Style.JustifyContent = Justify.Center;

            Element icon = c.Add(slot, "grid-icon");
            icon.Style.Width = Length.Percent(80f);
            icon.Style.Height = Length.Percent(80f);

            Element badge = c.Add(slot, "grid-badge");
            badge.Style.Position = PositionType.Absolute;
            badge.Style.Right = 2f;
            badge.Style.Bottom = 2f;
            badge.Style.Width = 12f;
            badge.Style.Height = 12f;
            if (i == 0)
            {
                firstSlot = slot;
            }
        }

        return firstSlot;
    }

    /// <summary>6. The stats panel: a column of 25 percent with 40 rows. 201 elements.</summary>
    private static Element BuildStats(Collector c, Element body, TextMeasure labelText, out Element firstName)
    {
        Element stats = c.Add(body, "stats");
        stats.Style.FlexDirection = FlexDirection.Column;
        stats.Style.Width = Length.Percent(25f);
        stats.Style.MaxWidth = 320f;

        firstName = null!;
        Element firstValue = null!;
        for (int i = 0; i < 40; i++)
        {
            Element row = c.Add(stats, "stat-row");
            row.Style.FlexDirection = FlexDirection.Row;
            row.Style.Height = 20f;
            row.Style.AlignItems = Align.Center;

            Element name = c.Add(row, "stat-name");
            name.Style.FlexGrow = 1f;
            name.Layout.Measure = labelText.Callback;

            Element value = c.Add(row, "stat-value");
            value.Style.Width = Length.Percent(ValueNarrowPercent);
            value.Style.Height = 16f;

            Element bar = c.Add(row, "stat-bar");
            bar.Style.FlexDirection = FlexDirection.Row;
            bar.Style.Width = 40f;
            bar.Style.Height = 6f;

            Element fill = c.Add(bar, "stat-fill");
            fill.Style.Width = Length.Percent(50f);
            fill.Style.Height = Length.Percent(100f);
            if (i == 0)
            {
                firstName = name;
                firstValue = value;
            }
        }

        return firstValue;
    }

    /// <summary>7. The footer: a centered row of height 64 with 10 slots. 31 elements.</summary>
    private static void BuildFooter(Collector c, Element root)
    {
        Element footer = c.Add(root, "footer");
        footer.Style.FlexDirection = FlexDirection.Row;
        footer.Style.Height = 64f;
        footer.Style.JustifyContent = Justify.Center;
        footer.Style.AlignItems = Align.Center;

        for (int i = 0; i < 10; i++)
        {
            Element slot = c.Add(footer, "footer-slot");
            slot.Style.Width = 56f;
            slot.Style.Height = 56f;

            Element icon = c.Add(slot, "footer-icon");
            icon.Style.FlexGrow = 1f;

            Element key = c.Add(slot, "footer-key");
            key.Style.Position = PositionType.Absolute;
            key.Style.Left = 2f;
            key.Style.Top = 2f;
            key.Style.Width = 12f;
            key.Style.Height = 12f;
        }
    }

    /// <summary>8. The tooltip: an absolute column with 6 text lines. 7 elements.</summary>
    private static Element BuildTooltip(Collector c, Element root, TextMeasure labelText)
    {
        Element tooltip = c.Add(root, "tooltip");
        tooltip.Style.Position = PositionType.Absolute;
        tooltip.Style.Left = 40f;
        tooltip.Style.Top = 40f;
        tooltip.Style.Width = TooltipNarrowWidth;
        tooltip.Style.FlexDirection = FlexDirection.Column;
        tooltip.Style.Padding = LengthEdges.All(4f);

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
        modal.Style.Position = PositionType.Absolute;
        modal.Style.Width = Length.Percent(50f);
        modal.Style.Height = Length.Percent(50f);
        modal.Style.Left = Length.Percent(25f);
        modal.Style.Top = Length.Percent(25f);
        modal.Style.FlexDirection = FlexDirection.Column;
        modal.Style.Padding = LengthEdges.All(8f);

        Element titleRow = c.Add(modal, "modal-title-row");
        titleRow.Style.FlexDirection = FlexDirection.Row;
        titleRow.Style.Height = 32f;
        titleRow.Style.AlignItems = Align.Center;

        Element title = c.Add(titleRow, "modal-title");
        title.Style.FlexGrow = 1f;
        title.Layout.Measure = labelText.Callback;

        Element close = c.Add(titleRow, "modal-close");
        close.Style.Width = 24f;
        close.Style.Height = 24f;

        Element modalBody = c.Add(modal, "modal-body");
        modalBody.Style.FlexDirection = FlexDirection.Column;
        modalBody.Style.FlexGrow = 1f;

        Element firstParagraph = null!;
        for (int i = 0; i < 3; i++)
        {
            Element paragraph = c.Add(modalBody, "modal-paragraph");
            paragraph.Style.Margin = LengthEdges.All(4f);
            paragraph.Layout.Measure = paragraphText.Callback;
            if (i == 0)
            {
                firstParagraph = paragraph;
            }
        }

        Element buttonRow = c.Add(modal, "modal-button-row");
        buttonRow.Style.FlexDirection = FlexDirection.Row;
        buttonRow.Style.JustifyContent = Justify.FlexEnd;

        for (int i = 0; i < 2; i++)
        {
            Element button = c.Add(buttonRow, "modal-button");
            button.Style.Width = 80f;
            button.Style.Height = 32f;
            button.Style.AlignItems = Align.Center;
            button.Style.JustifyContent = Justify.Center;

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
        settings.Style.Display = Display.None;
        settings.Style.Position = PositionType.Absolute;
        settings.Style.Left = 0f;
        settings.Style.Top = 0f;
        settings.Style.Width = Length.Percent(100f);
        settings.Style.Height = Length.Percent(100f);
        settings.Style.FlexDirection = FlexDirection.Column;

        for (int i = 0; i < 73; i++)
        {
            Element row = c.Add(settings, "settings-row");
            row.Style.FlexDirection = FlexDirection.Row;

            Element label = c.Add(row, "settings-label");
            label.Style.FlexGrow = 1f;

            Element toggle = c.Add(row, "settings-toggle");
            toggle.Style.Width = 24f;
            toggle.Style.Height = 12f;
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
