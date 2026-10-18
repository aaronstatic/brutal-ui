using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Hand-written tests of the margin rules with fixed sizes.
/// </summary>
/// <remarks>
/// These tests are not Yoga fixtures.
/// Each one is a hand-checked case of a margin rule that the Yoga fixtures cover only with grow, justify, or a start or end edge.
/// The expected values follow the Yoga rules by hand.
/// The absolute percent margin tests follow CSS: a percent margin resolves against the width of the containing block on
/// all four edges. Yoga uses the size of the axis in a position from an offset. No Yoga fixture covers that case.
/// The wrapped container tests follow CSS: each item puts its margin box in its line. Yoga drops the cross margin of a
/// flex-start item, centers the border box of a center item, and adds the wrong margins to the height of a stretch item
/// in a column. No Yoga fixture covers these cases.
/// A stretch item in a wrapped container with space-between, space-around, or space-evenly lines fills its line only,
/// as in CSS. Yoga also fills the space after the line. No Yoga fixture covers this case.
/// A row in which the first pass of the flex step freezes each growing item gives each item its bound flex basis.
/// Yoga divides by a total factor of 0 and gives an infinite width. No Yoga fixture covers this case.
/// A row in which the first pass freezes each shrinking item gives each item its bound flex basis, also when the float
/// subtractions leave a residual. A grow or shrink factor that is negative, infinite, or <c>NaN</c> counts as 0.
/// The measure tests check the callback of a leaf with <see cref="MeasureStub"/>: the size bounds, the available width,
/// and a callback on an element with children.
/// The scroll tests follow the Yoga rule for <see cref="Overflow.Scroll"/>, because no Yoga fixture that is in scope sets it.
/// </remarks>
public sealed class BoxModelTests
{
    /// <summary>It is the LTR half of margin_start with the left edge.</summary>
    [Fact]
    public void MarginLeftInRow()
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = 10;
        child0.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 10, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A right margin on the main axis of a row moves the next sibling.</summary>
    [Fact]
    public void MarginRightMovesSibling()
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Right = 10;
        child0.Style.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 20, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>The top and bottom margins on the main axis of a column add to the position of the next sibling.</summary>
    [Fact]
    public void MarginTopAndBottomMoveSibling()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = 5;
        child0.Style.Margin.Bottom = 5;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 5, 100, 10);
        LayoutFixture.AssertRect(child1, 0, 20, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>The auto height of a column holds the margins of its children.</summary>
    [Fact]
    public void MarginBottomCountsInAutoHeight()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Bottom = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 20);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// It is margin_and_stretch_column with a fixed height in place of grow.
    /// The stretch width on the cross axis is the width of the container less both margins.
    /// </summary>
    [Fact]
    public void MarginLeftAndRightShrinkStretch()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = 10;
        child0.Style.Margin.Right = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 10, 0, 80, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A percent top margin of an absolute child with a top offset resolves against the width of the containing block, as in CSS.
    /// Yoga uses the height here and gives Y 10.
    /// </summary>
    [Fact]
    public void AbsolutePercentMarginTopResolvesAgainstWidth()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element child0 = AddAbsoluteChild(root, "root_child0");
        child0.Style.Top = 0;
        child0.Style.Left = 0;
        child0.Style.Margin.Top = Length.Percent(10);

        LayoutFixture.Calculate(root);

        // Margin top = 10% of the width 200 = 20. Y = top 0 + border 0 + margin 20 = 20. X = left 0.
        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 0, 20, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A percent left margin of an absolute child with a left offset resolves against the width of the containing block.</summary>
    [Fact]
    public void AbsolutePercentMarginLeftResolvesAgainstWidth()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element child0 = AddAbsoluteChild(root, "root_child0");
        child0.Style.Top = 0;
        child0.Style.Left = 0;
        child0.Style.Margin.Left = Length.Percent(10);

        LayoutFixture.Calculate(root);

        // Margin left = 10% of the width 200 = 20. X = left 0 + border 0 + margin 20 = 20. Y = top 0.
        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 20, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A percent bottom margin of an absolute child with a bottom offset resolves against the width of the containing block.
    /// Yoga uses the height here and gives Y 80.
    /// </summary>
    [Fact]
    public void AbsolutePercentMarginBottomWithBottomOffsetResolvesAgainstWidth()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element child0 = AddAbsoluteChild(root, "root_child0");
        child0.Style.Bottom = 0;
        child0.Style.Left = 0;
        child0.Style.Margin.Bottom = Length.Percent(10);

        LayoutFixture.Calculate(root);

        // Margin bottom = 10% of the width 200 = 20.
        // Y = parent height 100 - child height 10 - border 0 - margin 20 - bottom 0 = 70. X = left 0.
        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 0, 70, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// On a reverse main axis, a percent top margin of an absolute child with a top offset also resolves against the width.
    /// </summary>
    [Fact]
    public void AbsolutePercentMarginTopInColumnReverseResolvesAgainstWidth()
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.ColumnReverse;
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element child0 = AddAbsoluteChild(root, "root_child0");
        child0.Style.Top = 0;
        child0.Style.Left = 0;
        child0.Style.Margin.Top = Length.Percent(10);

        LayoutFixture.Calculate(root);

        // Margin top = 10% of the width 200 = 20. From the top edge: 0 + 0 + 20 = 20.
        // From the flex-start (bottom) edge: 100 - 10 - 20 = 70. The conversion back gives Y = 100 - 10 - 70 = 20.
        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 0, 20, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// The containing block is the padding box of the parent: the border reduces the width for a percent margin, and the
    /// padding does not.
    /// </summary>
    [Fact]
    public void AbsolutePercentMarginResolvesAgainstPaddingBoxWidth()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 200;
        root.Style.Height = 100;
        root.Style.BorderWidth.Left = 10;
        root.Style.BorderWidth.Top = 10;
        root.Style.BorderWidth.Right = 10;
        root.Style.BorderWidth.Bottom = 10;
        root.Style.Padding.Left = 5;
        root.Style.Padding.Top = 5;
        root.Style.Padding.Right = 5;
        root.Style.Padding.Bottom = 5;

        Element child0 = AddAbsoluteChild(root, "root_child0");
        child0.Style.Top = 0;
        child0.Style.Left = 0;
        child0.Style.Margin.Top = Length.Percent(10);

        LayoutFixture.Calculate(root);

        // Padding box width = 200 - border 10 - border 10 = 180. Margin top = 10% of 180 = 18.
        // Y = top 0 + border 10 + margin 18 = 28. X = left 0 + border 10 + margin 0 = 10.
        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 10, 28, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A flex-start item in a wrapped row puts its margin box at the start of its line.</summary>
    [Fact]
    public void WrappedRowFlexStartAddsTopMarginToLineStart()
    {
        Element root = BuildWrappedRow(Align.FlexStart, 20, out Element child0, out Element child1, out Element child2);
        child2.Style.Margin.Top = 10;

        LayoutFixture.Calculate(root);

        // Line 0 holds child0 and child1 (50 + 50 = 100, not more than 100). Line 1 holds child2.
        // Line 0 height = max(20, 20) = 20. Line 1 starts at 20.
        // child2: Y = line start 20 + margin top 10 = 30. Yoga gives 20, with no margin.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 20);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 20);
        LayoutFixture.AssertRect(child2, 0, 30, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A center item in a wrapped row centers its margin box in its line.</summary>
    [Fact]
    public void WrappedRowCenterCentersMarginBox()
    {
        Element root = BuildWrappedRow(Align.Center, 40, out Element child0, out Element child1, out Element child2);
        child1.Style.Margin.Top = 10;

        LayoutFixture.Calculate(root);

        // Line 0 height = max(child0 40, child1 20 + margin 10) = 40.
        // child0: Y = (40 - 40) / 2 = 0.
        // child1: margin box = 20 + 10 = 30. Y = (40 - 30) / 2 + margin top 10 = 15. Yoga gives (40 - 20) / 2 = 10.
        // Line 1 starts at 40 and has a height of 20. child2: Y = 40 + (20 - 20) / 2 = 40.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 40);
        LayoutFixture.AssertRect(child1, 50, 15, 50, 20);
        LayoutFixture.AssertRect(child2, 0, 40, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A flex-end item in a wrapped row ends its margin box at the end of its line.</summary>
    [Fact]
    public void WrappedRowFlexEndEndsMarginBoxAtLineEnd()
    {
        Element root = BuildWrappedRow(Align.FlexEnd, 40, out Element child0, out Element child1, out Element child2);
        child1.Style.Margin.Bottom = 10;

        LayoutFixture.Calculate(root);

        // Line 0 height = max(child0 40, child1 20 + margin 10) = 40.
        // child0: Y = 40 - 40 = 0.
        // child1: Y = line end 40 - margin bottom 10 - height 20 = 10. The margin box ends at 10 + 20 + 10 = 40.
        // Line 1 starts at 40 and has a height of 20. child2: Y = 40 + 20 - 20 = 40.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 40);
        LayoutFixture.AssertRect(child1, 50, 10, 50, 20);
        LayoutFixture.AssertRect(child2, 0, 40, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A stretch item with an auto width in a wrapped column fills its line with its margin box, and keeps the height
    /// of its style.
    /// </summary>
    [Fact]
    public void WrappedColumnStretchKeepsHeightWithMargins()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.AlignContent = Align.FlexStart;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 40;
        child0.Style.Margin.Left = 5;
        child0.Style.Margin.Top = 3;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 20;
        child2.Style.Height = 40;

        LayoutFixture.Calculate(root);

        // Main axis (column): line 0 holds child0 (3 + 40 = 43) and child1 (43 + 40 = 83). child2 gives 123, more
        // than 100, so it starts line 1.
        // Before step 8, child0 has its content width 0. Line 0 width = max(0 + margin 5, 30) = 30.
        // Line 1 width = 20.
        // child0: X = 0 + margin left 5 = 5. Width = 30 - 5 = 25. Y = margin top 3. Height = 40 + 3 - 3 = 40.
        // Yoga adds the horizontal margins in place of the vertical margins: height = 40 + 5 - 3 = 42.
        // child1: X = 0, Y = 43. child2: X = line 1 start 30, Y = 0.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 5, 3, 25, 40);
        LayoutFixture.AssertRect(child1, 0, 43, 30, 40);
        LayoutFixture.AssertRect(child2, 30, 0, 20, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A stretch item with an auto height in a wrapped row fills its line with its margin box, and keeps the width of
    /// its style.
    /// </summary>
    [Fact]
    public void WrappedRowStretchKeepsWidthWithMargins()
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.AlignContent = Align.FlexStart;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 40;
        child0.Style.Margin.Left = 5;
        child0.Style.Margin.Top = 3;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;
        child1.Style.Height = 30;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;
        child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        // Main axis (row): line 0 holds child0 (5 + 40 = 45) and child1 (45 + 40 = 85). child2 gives 125, more than
        // 100, so it starts line 1.
        // Before step 8, child0 has its content height 0. Line 0 height = max(0 + margin 3, 30) = 30.
        // Line 1 height = 20.
        // child0: Y = 0 + margin top 3 = 3. Height = 30 - 3 = 27. X = margin left 5. Width = 40 + 5 - 5 = 40.
        // child1: X = 45, Y = 0. child2: X = 0, Y = line 1 start 30.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 5, 3, 40, 27);
        LayoutFixture.AssertRect(child1, 45, 0, 40, 30);
        LayoutFixture.AssertRect(child2, 0, 30, 40, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A stretch item in a wrapped row with space-between lines fills its line only. The space between the lines
    /// stays empty.
    /// </summary>
    [Fact]
    public void WrappedRowSpaceBetweenStretchFillsLineOnly()
    {
        Element root = BuildWrappedStretchRow(
            Align.SpaceBetween,
            out Element child0,
            out Element child1,
            out Element child2,
            out Element child3);

        LayoutFixture.Calculate(root);

        // Main axis (row): line 0 holds child0 (40) and child1 (80). child2 gives 120, more than 100, so it starts
        // line 1 with child3. Each item has the content height 0 and the minimum height 20, so each line height = 20.
        // Free space = 100 - (20 + 20) = 60. Space between the 2 lines = 60 / (2 - 1) = 60.
        // Line 0: Y = 0, height 20. Line 1: Y = 0 + 20 + 60 = 80, height 20.
        // Each item height = line height 20. Yoga gives 60 + 20 = 80, so line 1 overflows the root.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 20);
        LayoutFixture.AssertRect(child1, 40, 0, 40, 20);
        LayoutFixture.AssertRect(child2, 0, 80, 40, 20);
        LayoutFixture.AssertRect(child3, 40, 80, 40, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A stretch item in a wrapped row with space-around lines fills its line only.</summary>
    [Fact]
    public void WrappedRowSpaceAroundStretchFillsLineOnly()
    {
        Element root = BuildWrappedStretchRow(
            Align.SpaceAround,
            out Element child0,
            out Element child1,
            out Element child2,
            out Element child3);

        LayoutFixture.Calculate(root);

        // Two lines of height 20 (see the space-between test). Free space = 60.
        // Space per line = 60 / 2 = 30. The first line starts at 30 / 2 = 15.
        // Line 0: Y = 15, height 20. Line 1: Y = 15 + 20 + 30 = 65, height 20.
        // Each item height = line height 20. Yoga gives 30 + 20 = 50.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 15, 40, 20);
        LayoutFixture.AssertRect(child1, 40, 15, 40, 20);
        LayoutFixture.AssertRect(child2, 0, 65, 40, 20);
        LayoutFixture.AssertRect(child3, 40, 65, 40, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A stretch item in a wrapped row with space-evenly lines fills its line only.</summary>
    [Fact]
    public void WrappedRowSpaceEvenlyStretchFillsLineOnly()
    {
        Element root = BuildWrappedStretchRow(
            Align.SpaceEvenly,
            out Element child0,
            out Element child1,
            out Element child2,
            out Element child3);

        LayoutFixture.Calculate(root);

        // Two lines of height 20 (see the space-between test). Free space = 60.
        // Space = 60 / (2 + 1) = 20, before, between, and after the lines.
        // Line 0: Y = 20, height 20. Line 1: Y = 20 + 20 + 20 = 60, height 20.
        // Each item height = line height 20. Yoga gives 20 + 20 = 40.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 20, 40, 20);
        LayoutFixture.AssertRect(child1, 40, 20, 40, 20);
        LayoutFixture.AssertRect(child2, 0, 60, 40, 20);
        LayoutFixture.AssertRect(child3, 40, 60, 40, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A stretch item in a wrapped row with stretched lines fills its grown line. Each line grows by an equal part of
    /// the free space.
    /// </summary>
    [Fact]
    public void WrappedRowStretchLinesGrowStretchItems()
    {
        Element root = BuildWrappedStretchRow(
            Align.Stretch,
            out Element child0,
            out Element child1,
            out Element child2,
            out Element child3);

        LayoutFixture.Calculate(root);

        // Two lines of height 20 (see the space-between test). Free space = 60.
        // Each line grows by 60 / 2 = 30, so each line height = 20 + 30 = 50.
        // Line 0: Y = 0, height 50. Line 1: Y = 0 + 50 = 50, height 50.
        // Each item height = line height 50. Yoga gives the same result.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 50);
        LayoutFixture.AssertRect(child1, 40, 0, 40, 50);
        LayoutFixture.AssertRect(child2, 0, 50, 40, 50);
        LayoutFixture.AssertRect(child3, 40, 50, 40, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A stretch item in a wrapped column with space-between lines fills its line only. The test covers the width
    /// branch of the stretch size.
    /// </summary>
    [Fact]
    public void WrappedColumnSpaceBetweenStretchFillsLineOnly()
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.AlignContent = Align.SpaceBetween;
        root.Style.AlignItems = Align.Stretch;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 40;
        child0.Style.MinWidth = 20;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 40;
        child1.Style.MinWidth = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 40;
        child2.Style.MinWidth = 20;

        LayoutFixture.Calculate(root);

        // Main axis (column): line 0 holds child0 (40) and child1 (80). child2 gives 120, more than 100, so it starts
        // line 1. Each item has the content width 0 and the minimum width 20, so each line width = 20.
        // Free space = 100 - (20 + 20) = 60. Space between the 2 lines = 60 / (2 - 1) = 60.
        // Line 0: X = 0, width 20. Line 1: X = 0 + 20 + 60 = 80, width 20.
        // Each item width = line width 20. Yoga gives 60 + 20 = 80.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 20, 40);
        LayoutFixture.AssertRect(child1, 0, 40, 20, 40);
        LayoutFixture.AssertRect(child2, 80, 0, 20, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// The first pass freezes each growing item of a row, and free space stays. Each item then keeps its bound flex
    /// basis. Yoga divides the free space by a total factor of 0 and gives an infinite width. The tree has the shape
    /// of the review probe tree of CR-3, with round values.
    /// </summary>
    [Fact]
    public void GrowWithEachItemFrozenGivesFiniteRects()
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        // The row is wider than the root, so its owner width (100) and its inner width (200) are different.
        Element row = AddChild(root, "row");
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.Width = 200;
        row.Style.Height = 50;

        Element first = AddChild(row, "row_child0");
        first.Style.FlexBasis = 10;
        first.Style.MinWidth = Length.Percent(75);
        first.Style.FlexGrow = 0.5f;

        Element second = AddChild(row, "row_child1");
        second.Style.Width = 30;
        second.Style.MaxWidth = 20;
        second.Style.FlexGrow = 1;
        second.Style.Margin.Left = Length.Auto;

        LayoutFixture.Calculate(root);

        // Line: the flex basis of each item gets the bounds with the owner width 100 of the row.
        // first = max(10, 75% of 100 = 75) = 75. second = min(30, 20) = 20. The auto margin counts 0.
        // Free space = 200 - (75 + 20) = 105. Total grow factor = 0.5 + 1 = 1.5.
        // First pass: the minimum of first uses the inner width 200: 75% of 200 = 150.
        // first: 75 + 105 / 1.5 * 0.5 = 110, less than 150, so it freezes at 150. Delta = 150 - 75 = 75.
        // The total = 1.5 - 0.5 = 1.
        // second: 20 + 105 / 1 * 1 = 125, more than 20, so it freezes at 20. Delta = 75 + 0 = 75. The total = 0.
        // Free space = 105 - 75 = 30.
        // Second pass: the total is 0, so each item gets its bound flex basis. first = 150. second = 20.
        // Yoga gives first 75 + 30 / 0 * 0.5 = infinity.
        // Free space after the flex step = 105 - (150 - 75) - (20 - 20) = 30. The auto margin of second takes it.
        // first: X 0, width 150. second: X 150 + 30 = 180, width 20. Each item stretches to the height 50.
        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(row, 0, 0, 200, 50);
        LayoutFixture.AssertRect(first, 0, 0, 150, 50);
        LayoutFixture.AssertRect(second, 180, 0, 20, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// The first pass freezes each growing item of a row, but the float subtractions leave a small residual in place
    /// of a total grow factor of 0. The engine sets the residual to 0, so each item keeps its bound flex basis.
    /// Yoga divides by the residual: 0.3, 0.3, and 0.4 give the width 0 to the second and the third item, and 1.1,
    /// 2.2, and 3.3 give a width of about 92 million to the first item.
    /// </summary>
    /// <param name="grow0">The grow factor of the first item.</param>
    /// <param name="grow1">The grow factor of the second item.</param>
    /// <param name="grow2">The grow factor of the third item.</param>
    [Theory]
    [InlineData(0.3f, 0.3f, 0.4f)]
    [InlineData(1.1f, 2.2f, 3.3f)]
    public void GrowWithEachItemFrozenAndRoundingResidualGivesBoundBasis(float grow0, float grow1, float grow2)
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element row = AddChild(root, "row");
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.Width = 200;
        row.Style.Height = 50;

        Element first = AddChild(row, "row_child0");
        first.Style.FlexBasis = 10;
        first.Style.MinWidth = Length.Percent(75);
        first.Style.FlexGrow = grow0;

        Element second = AddChild(row, "row_child1");
        second.Style.Width = 30;
        second.Style.MaxWidth = 5;
        second.Style.FlexGrow = grow1;

        Element third = AddChild(row, "row_child2");
        third.Style.Width = 30;
        third.Style.MaxWidth = 5;
        third.Style.FlexGrow = grow2;

        LayoutFixture.Calculate(root);

        // Line: first = max(10, 75% of the owner width 100) = 75. second = third = min(30, 5) = 5.
        // Free space = 200 - (75 + 5 + 5) = 115.
        // First pass: the minimum of first uses the inner width 200: 75% of 200 = 150.
        // first with 0.3: 75 + 115 / 1 * 0.3 = 109.5. first with 1.1: 75 + 115 / 6.6 * 1.1 = 94.2 (rounded).
        // Each value is less than 150, so first freezes at 150. Delta = 150 - 75 = 75.
        // second and third get more than 5, so each freezes at 5. Delta = 75. The total is a residual near 0.
        // The engine sets the total to 0. Free space = 115 - 75 = 40.
        // Second pass: each item gets its bound flex basis. first = 150, second = 5, third = 5.
        // first: X 0. second: X 150. third: X 150 + 5 = 155. Each item stretches to the height 50.
        LayoutFixture.AssertRect(row, 0, 0, 200, 50);
        LayoutFixture.AssertRect(first, 0, 0, 150, 50);
        LayoutFixture.AssertRect(second, 150, 0, 5, 50);
        LayoutFixture.AssertRect(third, 155, 0, 5, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// The first pass freezes each shrinking item of a row at its minimum width, but the float subtractions leave a
    /// small residual in place of a total scaled shrink factor of 0. The engine sets the residual to 0, so each item
    /// keeps its bound flex basis. Yoga divides by the residual and gives widths of about 100 million or more.
    /// </summary>
    /// <param name="shrink0">The shrink factor of the first item.</param>
    /// <param name="shrink1">The shrink factor of the second item.</param>
    /// <param name="shrink2">The shrink factor of the third item.</param>
    [Theory]
    [InlineData(0.1f, 1.2f, 0.1f)]
    [InlineData(0.1f, 0.6f, 2.1f)]
    public void ShrinkWithEachItemFrozenAndRoundingResidualGivesBoundBasis(float shrink0, float shrink1, float shrink2)
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element row = AddChild(root, "row");
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.Width = 50;
        row.Style.Height = 50;

        Element first = AddChild(row, "row_child0");
        first.Style.Width = 40;
        first.Style.MinWidth = 40;
        first.Style.FlexShrink = shrink0;

        Element second = AddChild(row, "row_child1");
        second.Style.Width = 50;
        second.Style.MinWidth = 50;
        second.Style.FlexShrink = shrink1;

        Element third = AddChild(row, "row_child2");
        third.Style.Width = 60;
        third.Style.MinWidth = 60;
        third.Style.FlexShrink = shrink2;

        LayoutFixture.Calculate(root);

        // Line: the bases are 40, 50, and 60. Free space = 50 - 150 = -100.
        // First pass: each item shrinks to less than its minimum, so each item freezes at its minimum.
        // Delta = 0 for each item. The sums of the factors times the bases are 70 (0.1, 1.2, 0.1) and 160 (0.1, 0.6, 2.1).
        // The subtractions leave a residual of about 4e-6 or -8e-6 in place of 0. The engine sets the total to 0.
        // Second pass: each item gets its bound flex basis. first = 40, second = 50, third = 60.
        // first: X 0. second: X 40. third: X 90. Each item stretches to the height 50.
        LayoutFixture.AssertRect(row, 0, 0, 50, 50);
        LayoutFixture.AssertRect(first, 0, 0, 40, 50);
        LayoutFixture.AssertRect(second, 40, 0, 50, 50);
        LayoutFixture.AssertRect(third, 90, 0, 60, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A grow factor that is negative, infinite, or <c>NaN</c> counts as 0. CSS rejects such a value. Yoga gives a
    /// wrong rect: it gives a width of 0 to the item with an infinite factor and the whole free space to the item
    /// with a negative factor.
    /// </summary>
    /// <param name="grow">The grow factor of the first item.</param>
    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void InvalidGrowFactorCountsAsZero(float grow)
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element row = AddChild(root, "row");
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.Width = 100;
        row.Style.Height = 50;

        Element first = AddChild(row, "row_child0");
        first.Style.Width = 20;
        first.Style.FlexGrow = grow;

        Element second = AddChild(row, "row_child1");
        second.Style.Width = 20;
        second.Style.FlexGrow = 1;

        LayoutFixture.Calculate(root);

        // first does not grow: it keeps its width of 20. Free space = 100 - 40 = 60.
        // second is the only growing item: width = 20 + 60 = 80, at X 20.
        LayoutFixture.AssertRect(row, 0, 0, 100, 50);
        LayoutFixture.AssertRect(first, 0, 0, 20, 50);
        LayoutFixture.AssertRect(second, 20, 0, 80, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A shrink factor that is negative, infinite, or <c>NaN</c> counts as 0. CSS rejects such a value. A negative
    /// factor gives a positive scaled factor in Yoga, and the total of the line is then wrong.
    /// </summary>
    /// <param name="shrink">The shrink factor of the first item.</param>
    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void InvalidShrinkFactorCountsAsZero(float shrink)
    {
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element row = AddChild(root, "row");
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.Width = 100;
        row.Style.Height = 50;

        Element first = AddChild(row, "row_child0");
        first.Style.Width = 80;
        first.Style.FlexShrink = shrink;

        Element second = AddChild(row, "row_child1");
        second.Style.Width = 80;
        second.Style.FlexShrink = 1;

        LayoutFixture.Calculate(root);

        // first does not shrink: it keeps its width of 80. Free space = 100 - 160 = -60.
        // second is the only shrinking item: width = 80 + (-60 / -80 * -80) = 20, at X 80.
        LayoutFixture.AssertRect(row, 0, 0, 100, 50);
        LayoutFixture.AssertRect(first, 0, 0, 80, 50);
        LayoutFixture.AssertRect(second, 80, 0, 20, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// Builds a wrapped row root of 100 by 100 pixels with stretch items and four children of 40 pixels in width.
    /// Two children fill one line. Each child has an auto height and a minimum height of 20, so each line has a height
    /// of 20.
    /// </summary>
    /// <param name="alignContent">The <see cref="ComputedStyle.AlignContent"/> value of the root.</param>
    /// <param name="child0">The first child.</param>
    /// <param name="child1">The second child.</param>
    /// <param name="child2">The third child.</param>
    /// <param name="child3">The fourth child.</param>
    /// <returns>The root.</returns>
    private static Element BuildWrappedStretchRow(
        Align alignContent,
        out Element child0,
        out Element child1,
        out Element child2,
        out Element child3)
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.AlignContent = alignContent;
        root.Style.AlignItems = Align.Stretch;

        child0 = AddStretchChild(root, "root_child0");
        child1 = AddStretchChild(root, "root_child1");
        child2 = AddStretchChild(root, "root_child2");
        child3 = AddStretchChild(root, "root_child3");
        return root;
    }

    /// <summary>
    /// Creates a named child of 40 pixels in width with an auto height and a minimum height of 20, and adds it at the
    /// end of the children of <paramref name="parent"/>.
    /// </summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child.</param>
    /// <returns>The new child.</returns>
    private static Element AddStretchChild(Element parent, string name)
    {
        Element child = AddChild(parent, name);
        child.Style.Width = 40;
        child.Style.MinHeight = 20;
        return child;
    }

    /// <summary>
    /// Builds a wrapped row root of 100 by 100 pixels with flex-start lines and three children of 50 pixels in width.
    /// Two children fill one line. The first child has the height <paramref name="firstHeight"/>, and the other two
    /// children have a height of 20.
    /// </summary>
    /// <param name="alignItems">The <see cref="ComputedStyle.AlignItems"/> value of the root.</param>
    /// <param name="firstHeight">The height of the first child.</param>
    /// <param name="child0">The first child.</param>
    /// <param name="child1">The second child.</param>
    /// <param name="child2">The third child.</param>
    /// <returns>The root.</returns>
    private static Element BuildWrappedRow(
        Align alignItems,
        float firstHeight,
        out Element child0,
        out Element child1,
        out Element child2)
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.AlignContent = Align.FlexStart;
        root.Style.AlignItems = alignItems;

        child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = firstHeight;

        child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 20;

        child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 20;

        return root;
    }

    /// <summary>A leaf with a measure callback gets the size that the callback returns.</summary>
    [Fact]
    public void MeasureLeafGetsCallbackSize()
    {
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddMeasuredChild(root, "root_child0", MeasureStub.Size40By30);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 30);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>The min and max of a leaf with a measure callback bound the callback size.</summary>
    [Fact]
    public void MeasureLeafSizeIsBoundedByMinAndMax()
    {
        // The callback returns 40 by 30. MaxWidth 25 cuts the width to 25. MinHeight 50 grows the height to 50.
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddMeasuredChild(root, "root_child0", MeasureStub.Size40By30);
        child0.Style.MaxWidth = 25;
        child0.Style.MinHeight = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(child0, 0, 0, 25, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A stretched leaf with a measure callback gets the available width, and the callback gives the height.</summary>
    [Fact]
    public void MeasureLeafStretchedWidthUsesAvailableWidth()
    {
        // The root is a column of 100 pixels width. The child stretches to 100. The callback gets the width 100.
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddMeasuredChild(root, "root_child0", MeasureStub.AvailableWidthBy20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(child0, 0, 0, 100, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>The engine does not call the callback of an element with children, and the children give the size.</summary>
    [Fact]
    public void MeasureCallbackOnElementWithChildrenIsNotCalled()
    {
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;

        MeasureStub stub = new(10, 10);
        Element child0 = AddMeasuredChild(root, "root_child0", stub.Callback);

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 30;
        child0Child0.Style.Height = 40;

        LayoutFixture.Calculate(root);

        Assert.Equal(0, stub.Count);
        LayoutFixture.AssertRect(child0, 0, 0, 30, 40);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 30, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A column with <see cref="Overflow.Scroll"/> in a column parent with flex-start alignment takes the width
    /// limit of the parent and the height of its content.
    /// </summary>
    [Fact]
    public void ScrollColumnLimitsCrossAxisToAvailableWidth()
    {
        // The Yoga rule (step 9 of calculateLayoutImpl): a scroll container with an at-most mode on an axis takes
        // max(min(availableInner + paddingAndBorder, content), paddingAndBorder) on that axis.
        // The parent is a column with align-items flex-start. Thus the width of the child is at most 100, and
        // the scroll column takes 100. The height has no limit in a column, so it takes the content height 300.
        // The child of the scroll column keeps 300 by 300.
        Element root = BuildScrollTree(Overflow.Scroll, FlexDirection.Column, FlexDirection.Column, out Element scroll, out Element content);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(scroll, 0, 0, 100, 300);
        LayoutFixture.AssertRect(content, 0, 0, 300, 300);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>The same tree with <see cref="Overflow.Hidden"/> grows to the content width, so the scroll rule makes the difference.</summary>
    [Fact]
    public void HiddenColumnTakesContentSizeInTheSameTree()
    {
        // The hidden column has no scroll rule. The at-most width 100 does not bound the content. The column takes
        // the content size 300 by 300.
        Element root = BuildScrollTree(Overflow.Hidden, FlexDirection.Column, FlexDirection.Column, out Element scroll, out Element content);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(scroll, 0, 0, 300, 300);
        LayoutFixture.AssertRect(content, 0, 0, 300, 300);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// A row with <see cref="Overflow.Scroll"/> in a row parent with flex-start alignment takes the content width
    /// and the height limit of the parent.
    /// </summary>
    [Fact]
    public void ScrollRowTakesContentWidthAndLimitedHeight()
    {
        // The parent is a row with align-items flex-start. The scroll row measures with the at-most mode 100 on
        // both axes. Its main axis is the width. With no flexible child, step 5 sets the inner main size to the size
        // of the line (300), so the width is min(300, 300) = 300 (the same as Yoga). The height is the cross axis.
        // The scroll rule gives min(100, 300) = 100.
        Element root = BuildScrollTree(Overflow.Scroll, FlexDirection.Row, FlexDirection.Row, out Element scroll, out Element content);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(scroll, 0, 0, 300, 100);
        LayoutFixture.AssertRect(content, 0, 0, 300, 300);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>The padding of a scroll container stays inside the limit: the border box takes the limit, not the content plus the padding.</summary>
    [Fact]
    public void ScrollColumnWithPaddingKeepsPaddingInsideLimit()
    {
        // The available outer width is 100, so availableInner + paddingAndBorder is 100.
        // The content is 300 + 20 = 320 wide. min(100, 320) = 100, and 100 is not below the padding of 20.
        // The height has no limit in the column: 300 + 20 = 320.
        Element root = BuildScrollTree(Overflow.Scroll, FlexDirection.Column, FlexDirection.Column, out Element scroll, out Element content);
        scroll.Style.Padding = LengthEdges.All(10);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(scroll, 0, 0, 100, 320);
        LayoutFixture.AssertRect(content, 10, 10, 300, 300);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>A scroll container with a fixed size keeps the size, and the content overflows.</summary>
    [Fact]
    public void ScrollContainerWithFixedSizeKeepsSize()
    {
        Element root = BuildScrollTree(Overflow.Scroll, FlexDirection.Column, FlexDirection.Column, out Element scroll, out Element content);
        scroll.Style.Width = 60;
        scroll.Style.Height = 70;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(scroll, 0, 0, 60, 70);
        LayoutFixture.AssertRect(content, 0, 0, 300, 300);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>Creates a named leaf with a measure callback and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child.</param>
    /// <param name="measure">The measure callback.</param>
    /// <returns>The new child.</returns>
    private static Element AddMeasuredChild(Element parent, string name, MeasureCallback measure)
    {
        Element child = AddChild(parent, name);
        child.Layout.Measure = measure;
        return child;
    }

    /// <summary>
    /// Builds a 100 by 100 parent with flex-start alignment, one auto-size child with the given overflow, and a
    /// 300 by 300 child in that child.
    /// </summary>
    /// <param name="overflow">The overflow of the middle element.</param>
    /// <param name="parentDirection">The flex direction of the root.</param>
    /// <param name="scrollDirection">The flex direction of the middle element.</param>
    /// <param name="scroll">The middle element.</param>
    /// <param name="content">The 300 by 300 element.</param>
    /// <returns>The root.</returns>
    private static Element BuildScrollTree(
        Overflow overflow,
        FlexDirection parentDirection,
        FlexDirection scrollDirection,
        out Element scroll,
        out Element content)
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = parentDirection;
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;

        scroll = AddChild(root, "root_child0");
        scroll.Style.FlexDirection = scrollDirection;
        scroll.Style.Overflow = overflow;

        content = AddChild(scroll, "root_child0_child0");
        content.Style.Width = 300;
        content.Style.Height = 300;

        return root;
    }

    /// <summary>Creates a named absolute child of 10 by 10 pixels and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child.</param>
    /// <returns>The new child.</returns>
    private static Element AddAbsoluteChild(Element parent, string name)
    {
        Element child = AddChild(parent, name);
        child.Style.Position = PositionType.Absolute;
        child.Style.Width = 10;
        child.Style.Height = 10;
        return child;
    }

    /// <summary>Creates a named child and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child.</param>
    /// <returns>The new child.</returns>
    private static Element AddChild(Element parent, string name)
    {
        Element child = new() { Name = name };
        parent.Children.Add(child);
        return child;
    }
}
