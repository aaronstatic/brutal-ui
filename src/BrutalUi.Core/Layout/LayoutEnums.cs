namespace BrutalUi.Core;

/// <summary>
/// The direction of the main axis of a flex container. The children go along the main axis.
/// </summary>
/// <remarks>
/// <see cref="Column"/> is the default, as in Yoga. The CSS default is <c>row</c>.
/// The cross axis is perpendicular to the main axis. A row gives a column cross axis, and a column gives a row cross axis.
/// </remarks>
public enum FlexDirection : byte
{
    /// <summary>The children go from the top to the bottom. This is the default.</summary>
    Column = 0,

    /// <summary>The children go from the bottom to the top.</summary>
    ColumnReverse,

    /// <summary>The children go from the left to the right.</summary>
    Row,

    /// <summary>The children go from the right to the left.</summary>
    RowReverse,
}

/// <summary>
/// The rule for the children of a flex container when they do not fit on one line.
/// </summary>
/// <remarks>
/// <see cref="NoWrap"/> is the default, as in Yoga and CSS. With <see cref="Wrap"/> or <see cref="WrapReverse"/>,
/// a child that goes past the main size of the container starts a new line. Each line keeps the order of the
/// children. <see cref="ComputedStyle.AlignContent"/> sets the positions of the lines on the cross axis.
/// </remarks>
public enum FlexWrap : byte
{
    /// <summary>The children stay on one line, also when they do not fit. This is the default.</summary>
    NoWrap = 0,

    /// <summary>The children go on more lines. The lines go from the flex-start edge of the cross axis.</summary>
    Wrap,

    /// <summary>
    /// The children go on more lines. The lines go from the flex-end edge of the cross axis: in a row, the first
    /// line is at the bottom, and in a column, the first line is at the right.
    /// </summary>
    WrapReverse,
}

/// <summary>
/// The position of the children of a flex container on the main axis, when the line has free space.
/// </summary>
/// <remarks>
/// <see cref="FlexStart"/> is the default, as in Yoga. When the children use more space than the line has,
/// the free space is negative. Then <see cref="SpaceBetween"/>, <see cref="SpaceAround"/>, and
/// <see cref="SpaceEvenly"/> act as <see cref="FlexStart"/>, as in Yoga.
/// </remarks>
public enum Justify : byte
{
    /// <summary>The children go to the flex-start edge of the main axis. This is the default.</summary>
    FlexStart = 0,

    /// <summary>The children go to the center of the main axis. The free space is equal on the two edges.</summary>
    Center,

    /// <summary>The children go to the flex-end edge of the main axis.</summary>
    FlexEnd,

    /// <summary>
    /// The first child goes to the flex-start edge and the last child goes to the flex-end edge.
    /// The free space is equal between two children. A line with one child uses <see cref="FlexStart"/>.
    /// </summary>
    SpaceBetween,

    /// <summary>
    /// Each child gets an equal part of the free space, half before the child and half after it.
    /// The space at the edges is half the space between two children.
    /// </summary>
    SpaceAround,

    /// <summary>The free space is equal at the two edges and between two children.</summary>
    SpaceEvenly,
}

/// <summary>
/// The position of an element on the cross axis of its line, or of the lines on the cross axis of a container.
/// <see cref="ComputedStyle.AlignItems"/>, <see cref="ComputedStyle.AlignSelf"/>, and
/// <see cref="ComputedStyle.AlignContent"/> use this enum.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Auto"/> is the default. On <see cref="ComputedStyle.AlignItems"/>, <see cref="Auto"/> means
/// <see cref="Stretch"/>. On <see cref="ComputedStyle.AlignSelf"/>, <see cref="Auto"/> means the
/// <see cref="ComputedStyle.AlignItems"/> value of the parent.
/// </para>
/// <para>
/// <see cref="SpaceBetween"/>, <see cref="SpaceAround"/>, and <see cref="SpaceEvenly"/> are for the alignment of
/// the lines. On <see cref="ComputedStyle.AlignItems"/> and <see cref="ComputedStyle.AlignSelf"/>, they act as
/// <see cref="FlexStart"/>. The enum has no baseline value.
/// </para>
/// <para>
/// On <see cref="ComputedStyle.AlignContent"/>, <see cref="Auto"/> means <see cref="FlexStart"/>, and each value
/// sets the positions of the lines of a wrapped container.
/// </para>
/// </remarks>
public enum Align : byte
{
    /// <summary>The default. The value comes from the parent or from the default rule of the field.</summary>
    Auto = 0,

    /// <summary>The element goes to the flex-start edge of the cross axis.</summary>
    FlexStart,

    /// <summary>The element goes to the center of the cross axis. The free space is equal on the two edges.</summary>
    Center,

    /// <summary>The element goes to the flex-end edge of the cross axis.</summary>
    FlexEnd,

    /// <summary>
    /// The element fills the cross size of its line. An element with a definite cross size keeps that size
    /// and goes to the flex-start edge. On <see cref="ComputedStyle.AlignContent"/>, each line gets an equal part of
    /// the free space on the cross axis.
    /// </summary>
    Stretch,

    /// <summary>
    /// The first line goes to the flex-start edge and the last line goes to the flex-end edge. On an element,
    /// this value acts as <see cref="FlexStart"/>.
    /// </summary>
    SpaceBetween,

    /// <summary>
    /// Each line gets an equal part of the free space, half before the line and half after it. On an element,
    /// this value acts as <see cref="FlexStart"/>.
    /// </summary>
    SpaceAround,

    /// <summary>
    /// The free space is equal at the two edges and between two lines. On an element, this value acts as
    /// <see cref="FlexStart"/>.
    /// </summary>
    SpaceEvenly,
}

/// <summary>
/// The display type of an element for the layout.
/// </summary>
/// <remarks>
/// <see cref="Flex"/> is the default, as in Yoga. An element with <see cref="None"/> and its subtree take no space,
/// and each of them gets a rect of zeros. A change of the value needs a call of
/// <see cref="Element.MarkDirty(DirtyFlags)"/> with <see cref="DirtyFlags.Layout"/>.
/// </remarks>
public enum Display : byte
{
    /// <summary>The element is a flex item of its parent and a flex container of its children. This is the default.</summary>
    Flex = 0,

    /// <summary>
    /// The element and its subtree take no space and get a rect of zeros. The value has no effect on a root element.
    /// </summary>
    None,
}

/// <summary>
/// The position type of an element: in the flex flow of its parent, or out of the flow.
/// </summary>
/// <remarks>
/// <see cref="Relative"/> is the default, as in Yoga. The engine has no static position type.
/// </remarks>
public enum PositionType : byte
{
    /// <summary>
    /// The element is in the flex flow of its parent. The offsets move it from its flex position and do not
    /// change the position of a sibling. This is the default.
    /// </summary>
    Relative = 0,

    /// <summary>
    /// The element is out of the flex flow and takes no space in the line of its parent. The offsets place it in
    /// the padding box of the parent. With no offset on an axis, the justification and the alignment of the parent
    /// place it.
    /// </summary>
    Absolute,
}

/// <summary>
/// The rule for the content of an element that is larger than the element.
/// </summary>
/// <remarks>
/// <see cref="Visible"/> is the default, as in Yoga and CSS. <see cref="Visible"/> and <see cref="Hidden"/> give the
/// same layout. The renderer epic clips the content of <see cref="Hidden"/> and <see cref="Scroll"/>.
/// <see cref="Scroll"/> changes the layout: the size of the element does not grow to the size of its content, and
/// the children get no limit on the main axis of the element.
/// </remarks>
public enum Overflow : byte
{
    /// <summary>The content can go outside the element. The layout is the same as for <see cref="Hidden"/>. This is the default.</summary>
    Visible = 0,

    /// <summary>The layout is the same as for <see cref="Visible"/>. The renderer clips the content.</summary>
    Hidden,

    /// <summary>
    /// The element is a scroll container. When the available size is a maximum, the element takes that size and not
    /// the size of its content. A child gets no limit on the main axis of the element. The renderer clips the content.
    /// </summary>
    Scroll,
}

/// <summary>
/// The rule for an available size that the layout gives to an element or to a measure callback.
/// </summary>
/// <remarks>
/// Yoga 3 uses a sizing mode inside the algorithm and this measure mode at the measure callback.
/// The engine uses the measure mode in both places. <see cref="Exactly"/> is the Yoga stretch-fit mode,
/// <see cref="AtMost"/> is the fit-content mode, and <see cref="Undefined"/> is the max-content mode.
/// </remarks>
public enum MeasureMode : byte
{
    /// <summary>The size has no limit. The available size is <c>NaN</c>. The element takes the size of its content.</summary>
    Undefined = 0,

    /// <summary>The element takes the available size exactly.</summary>
    Exactly,

    /// <summary>The element takes the size of its content, but not more than the available size.</summary>
    AtMost,
}
