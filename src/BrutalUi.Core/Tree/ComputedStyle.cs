namespace BrutalUi.Core;

/// <summary>
/// The computed style of an element. The struct lives inline in the element and holds the layout inputs, the paint
/// members, and the text members.
/// </summary>
/// <remarks>
/// <para>
/// <c>default(ComputedStyle)</c> gives the Yoga defaults: a column direction and auto sizes.
/// Each field doc gives the default and the units. A new <see cref="Element"/> holds the default style until the
/// first <see cref="UiContext.Update(float)"/>.
/// </para>
/// <para>
/// The style pass writes the fields. <see cref="Element.Style"/> gives a read-only reference. The pass computes each
/// member from the rules of the sheets of the context and from the inline set of the element. An inline value wins
/// over each rule. The game writes the inline set through <see cref="Element.Inline"/>, for example
/// <c>element.Inline.Width = 120;</c>, and the next <see cref="UiContext.Update(float)"/> writes the result here.
/// </para>
/// <para>
/// A write marks the dirty bits itself. An inline write marks the element with <see cref="DirtyFlags.Style"/>. The
/// style pass then compares the old style and the new style. A changed layout member marks
/// <see cref="DirtyFlags.Layout"/>. A changed paint member or text member marks <see cref="DirtyFlags.Paint"/>.
/// Do not call <see cref="Element.MarkDirty(DirtyFlags)"/> for a style change.
/// </para>
/// <para>
/// The text members <see cref="Color"/>, <see cref="Font"/>, and <see cref="FontSize"/> inherit. When no rule and no
/// inline value set a text member, the element gets the value of its parent. A root gets the value of the text
/// defaults of its context (<see cref="UiContext.CreateInfo"/>). The other members do not inherit.
/// </para>
/// <para>
/// The members are public fields and not properties, so a read of a nested value, such as the top margin, reads the
/// field with no copy. The one exception is <see cref="Opacity"/>, which is a property because its default is 1.
/// </para>
/// </remarks>
public struct ComputedStyle
{
    /// <summary>
    /// The direction of the main axis of the children. The default is <see cref="FlexDirection.Column"/>.
    /// </summary>
    public FlexDirection FlexDirection;

    /// <summary>
    /// The width of the border box, in pixels or in percent of the width of the parent content box.
    /// The default is <see cref="Length.Undefined"/>, which means auto: the flex rules and the content give the width.
    /// </summary>
    public Length Width;

    /// <summary>
    /// The height of the border box, in pixels or in percent of the height of the parent content box.
    /// The default is <see cref="Length.Undefined"/>, which means auto: the flex rules and the content give the height.
    /// </summary>
    public Length Height;

    /// <summary>
    /// The space outside the border box, for each edge, in pixels or in percent of the width of the parent content box.
    /// A percent on all four edges uses the width of the parent, as in Yoga. The default is
    /// <see cref="LengthEdges"/> with <see cref="Length.Undefined"/> on each edge, which counts as 0.
    /// A negative pixel value is valid.
    /// </summary>
    /// <remarks>
    /// <see cref="Length.Auto"/> on an edge takes free space, as in CSS and Yoga. On the main axis of the parent, the
    /// auto margins of a line share the positive free space in equal parts, and
    /// <see cref="JustifyContent"/> of the parent then gives no space. On the cross axis, an auto margin stops the
    /// stretch. Two auto margins center the element, and one auto margin pushes the element to the other edge.
    /// An auto margin takes no negative space. The margin of an absolute element counts auto as 0.
    /// </remarks>
    public LengthEdges Margin;

    /// <summary>
    /// The space between the border and the content, for each edge, in pixels or in percent of the width of the
    /// parent content box. A percent on all four edges uses the width of the parent, as in Yoga.
    /// The default is <see cref="Length.Undefined"/> on each edge, which counts as 0.
    /// A negative value and <see cref="Length.Auto"/> count as 0.
    /// </summary>
    public LengthEdges Padding;

    /// <summary>
    /// The width of the border, for each edge, in pixels. The border lies inside the border box, between the edge and the padding.
    /// The default is <see cref="Length.Undefined"/> on each edge, which counts as 0.
    /// A percent, <see cref="Length.Auto"/>, and a negative value count as 0.
    /// </summary>
    public LengthEdges BorderWidth;

    /// <summary>
    /// The part of the free space on the main axis of the parent that the element takes, as a factor with no unit.
    /// The default is 0: the element does not grow. A root element does not grow.
    /// A negative, infinite, or <c>NaN</c> value counts as 0.
    /// </summary>
    /// <remarks>
    /// The engine gives each growing child a part of the free space in the ratio of the factors.
    /// When the sum of the factors of a line is more than 0 and less than 1, the engine uses a sum of 1.
    /// </remarks>
    public float FlexGrow;

    /// <summary>
    /// The part of the negative free space on the main axis of the parent that the element gives up, as a factor
    /// with no unit. The engine multiplies the factor by the flex basis of the element.
    /// The default is 0: the element does not shrink. This is the Yoga default, not the CSS default of 1.
    /// A root element does not shrink. A negative, infinite, or <c>NaN</c> value counts as 0.
    /// </summary>
    public float FlexShrink;

    /// <summary>
    /// The main size of the element before the engine distributes the free space, in pixels or in percent of the
    /// main size of the parent content box. The default is <see cref="Length.Undefined"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Length.Undefined"/> and <see cref="Length.Auto"/> both mean auto: a definite
    /// <see cref="Width"/> or <see cref="Height"/> on the main axis gives the basis, else the content gives it.
    /// A percent of an undefined parent size acts as auto. A basis less than the padding and border sum
    /// gives that sum.
    /// </remarks>
    public Length FlexBasis;

    /// <summary>
    /// The smallest width of the border box, in pixels or in percent of the width of the parent content box.
    /// The default is <see cref="Length.Undefined"/>, which means no limit.
    /// </summary>
    /// <remarks>
    /// The minimum overrides the maximum, as in Yoga. A minimum equal to the maximum also gives the
    /// <see cref="Width"/>.
    /// </remarks>
    public Length MinWidth;

    /// <summary>
    /// The smallest height of the border box, in pixels or in percent of the height of the parent content box.
    /// The default is <see cref="Length.Undefined"/>, which means no limit.
    /// </summary>
    /// <remarks>
    /// The minimum overrides the maximum, as in Yoga. A minimum equal to the maximum also gives the
    /// <see cref="Height"/>.
    /// </remarks>
    public Length MinHeight;

    /// <summary>
    /// The largest width of the border box, in pixels or in percent of the width of the parent content box.
    /// The default is <see cref="Length.Undefined"/>, which means no limit.
    /// </summary>
    /// <remarks><see cref="MinWidth"/> overrides this value.</remarks>
    public Length MaxWidth;

    /// <summary>
    /// The largest height of the border box, in pixels or in percent of the height of the parent content box.
    /// The default is <see cref="Length.Undefined"/>, which means no limit.
    /// </summary>
    /// <remarks><see cref="MinHeight"/> overrides this value.</remarks>
    public Length MaxHeight;

    /// <summary>
    /// The position of the children on the main axis when the line has free space.
    /// The default is <see cref="Justify.FlexStart"/>.
    /// </summary>
    public Justify JustifyContent;

    /// <summary>
    /// The position of the children on the cross axis of their line. A child with an
    /// <see cref="AlignSelf"/> value that is not <see cref="Align.Auto"/> overrides this value.
    /// The default is <see cref="Align.Auto"/>, which means <see cref="Align.Stretch"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Align.SpaceBetween"/>, <see cref="Align.SpaceAround"/>, and <see cref="Align.SpaceEvenly"/>
    /// act as <see cref="Align.FlexStart"/>.
    /// </remarks>
    public Align AlignItems;

    /// <summary>
    /// The position of the element on the cross axis of its line. The default is <see cref="Align.Auto"/>, which
    /// means the <see cref="AlignItems"/> value of the parent.
    /// </summary>
    /// <remarks>
    /// <see cref="Align.SpaceBetween"/>, <see cref="Align.SpaceAround"/>, and <see cref="Align.SpaceEvenly"/>
    /// act as <see cref="Align.FlexStart"/>. A root element has no parent, so this value has no effect on a root.
    /// </remarks>
    public Align AlignSelf;

    /// <summary>
    /// The rule for the children that do not fit on one line. The default is <see cref="FlexWrap.NoWrap"/>.
    /// </summary>
    /// <remarks>
    /// With <see cref="FlexWrap.Wrap"/> or <see cref="FlexWrap.WrapReverse"/>, a child that goes past the inner main
    /// size of the element starts a new line. A line holds one child or more. The cross size of each line is the
    /// largest cross size of its children, with the margins. A relative offset on the cross axis moves a child in its
    /// physical direction, also with <see cref="FlexWrap.WrapReverse"/>.
    /// </remarks>
    public FlexWrap FlexWrap;

    /// <summary>
    /// The position of the lines on the cross axis of the element. The default is <see cref="Align.Auto"/>, which
    /// means <see cref="Align.FlexStart"/>, the Yoga default.
    /// </summary>
    /// <remarks>
    /// The value applies to a wrapped element only (<see cref="FlexWrap"/> is not <see cref="FlexWrap.NoWrap"/>).
    /// Then it applies also when the element has one line. When the lines need more space than the element has,
    /// <see cref="Align.Stretch"/>, <see cref="Align.SpaceBetween"/>, <see cref="Align.SpaceAround"/>, and
    /// <see cref="Align.SpaceEvenly"/> act as <see cref="Align.FlexStart"/>, as in Yoga.
    /// <see cref="Align.Stretch"/> gives each line an equal part of the free space. With each value, a child with the
    /// stretch alignment and no definite cross size fills the cross size of its line. The CSS default is
    /// <c>normal</c>, which acts as stretch.
    /// </remarks>
    public Align AlignContent;

    /// <summary>
    /// The display type of the element. The default is <see cref="Display.Flex"/>.
    /// </summary>
    /// <remarks>
    /// With <see cref="Display.None"/>, the element and its subtree take no space, and the layout pass sets each
    /// rect of the subtree to zeros. A change of this value marks <see cref="DirtyFlags.Layout"/> in the style pass,
    /// so the layout pass sees the change. The value has no effect on a root element.
    /// </remarks>
    public Display Display;

    /// <summary>
    /// The position type of the element. The default is <see cref="PositionType.Relative"/>.
    /// </summary>
    /// <remarks>
    /// A relative element is in the flex flow. <see cref="Left"/>, <see cref="Top"/>, <see cref="Right"/>, and
    /// <see cref="Bottom"/> move it from its flex position. An absolute element is out of the flex flow and takes
    /// no space in the line of its parent. The offsets place it in the padding box of the parent.
    /// </remarks>
    public PositionType Position;

    /// <summary>
    /// The offset from the left edge, in pixels or in percent of the width of the parent.
    /// The default is <see cref="Length.Undefined"/>, which means "not set". <see cref="Length.Auto"/> also means "not set".
    /// </summary>
    /// <remarks>
    /// On a relative element, a positive value moves the element to the right from its flex position. When
    /// <see cref="Left"/> and <see cref="Right"/> are both set, <see cref="Left"/> applies. The move is physical, as
    /// in CSS: it goes to the right in each flex direction, also in a parent with <see cref="FlexDirection.RowReverse"/>.
    /// On an absolute element, the value is the space between the left edge of the padding box of the parent and the
    /// left margin edge of the element. With <see cref="Right"/> also set and an auto <see cref="ComputedStyle.Width"/>,
    /// the two offsets give the width.
    /// </remarks>
    public Length Left;

    /// <summary>
    /// The offset from the top edge, in pixels or in percent of the height of the parent.
    /// The default is <see cref="Length.Undefined"/>, which means "not set". <see cref="Length.Auto"/> also means "not set".
    /// </summary>
    /// <remarks>
    /// On a relative element, a positive value moves the element down from its flex position. When
    /// <see cref="Top"/> and <see cref="Bottom"/> are both set, <see cref="Top"/> applies. The move is physical, as
    /// in CSS: it goes down in each flex direction, also in a parent with <see cref="FlexDirection.ColumnReverse"/>.
    /// On an absolute element, the value is the space between the top edge of the padding box of the parent and the
    /// top margin edge of the element. With <see cref="Bottom"/> also set and an auto <see cref="ComputedStyle.Height"/>,
    /// the two offsets give the height.
    /// </remarks>
    public Length Top;

    /// <summary>
    /// The offset from the right edge, in pixels or in percent of the width of the parent.
    /// The default is <see cref="Length.Undefined"/>, which means "not set". <see cref="Length.Auto"/> also means "not set".
    /// </summary>
    /// <remarks>
    /// On a relative element, a positive value moves the element to the left from its flex position. The value has
    /// no effect when <see cref="Left"/> is set. The move is physical, as in CSS: it goes to the left in each flex
    /// direction, also in a parent with <see cref="FlexDirection.RowReverse"/>.
    /// On an absolute element, the value is the space between the right edge of the padding box of the parent and the
    /// right margin edge of the element. It applies only when <see cref="Left"/> is not set.
    /// </remarks>
    public Length Right;

    /// <summary>
    /// The offset from the bottom edge, in pixels or in percent of the height of the parent.
    /// The default is <see cref="Length.Undefined"/>, which means "not set". <see cref="Length.Auto"/> also means "not set".
    /// </summary>
    /// <remarks>
    /// On a relative element, a positive value moves the element up from its flex position. The value has no
    /// effect when <see cref="Top"/> is set. The move is physical, as in CSS: it goes up in each flex direction,
    /// also in a parent with <see cref="FlexDirection.ColumnReverse"/>.
    /// On an absolute element, the value is the space between the bottom edge of the padding box of the parent and the
    /// bottom margin edge of the element. It applies only when <see cref="Top"/> is not set.
    /// </remarks>
    public Length Bottom;

    /// <summary>
    /// The rule for content that is larger than the element. The default is <see cref="Overflow.Visible"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Overflow.Visible"/> and <see cref="Overflow.Hidden"/> give the same layout.
    /// <see cref="Overflow.Scroll"/> changes the measure limits of the children and the final size of the element.
    /// The draw walk clips the content of <see cref="Overflow.Hidden"/> and <see cref="Overflow.Scroll"/> to the padding box.
    /// </remarks>
    public Overflow Overflow;

    /// <summary>
    /// The fill color of the border box. The default is <see cref="Rgba8.Transparent"/>, which draws no fill.
    /// </summary>
    /// <remarks>
    /// The value is a paint field. It has no effect on the layout. A change marks <see cref="DirtyFlags.Paint"/>.
    /// </remarks>
    public Rgba8 BackgroundColor;

    /// <summary>
    /// The color of the border. The default is <see cref="Rgba8.Transparent"/>, which draws no border.
    /// </summary>
    /// <remarks>
    /// The draw walk uses the left edge of <see cref="BorderWidth"/> as the width of the border (pixels only).
    /// The value is a paint field. It has no effect on the layout. A change marks <see cref="DirtyFlags.Paint"/>.
    /// </remarks>
    public Rgba8 BorderColor;

    /// <summary>
    /// The radius of each corner of the fill and the border, in pixels. The default is 0 on each corner.
    /// </summary>
    /// <remarks>
    /// The value is a paint field. It has no effect on the layout. A change marks <see cref="DirtyFlags.Paint"/>.
    /// </remarks>
    public Corners CornerRadius;

    /// <summary>
    /// The text color. The value inherits: with no rule and no inline value, the element gets the color of its parent,
    /// and a root gets <see cref="UiContext.CreateInfo.DefaultColor"/>. The context default is opaque white.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Rgba8.Transparent"/> draws no text. <c>default(ComputedStyle)</c> holds transparent black, but the
    /// style pass gives each element a value, so an element with no rule gets the color of the context.
    /// </para>
    /// <para>
    /// The value is a text field. A change marks <see cref="DirtyFlags.Paint"/> and restyles each child, so the new
    /// value flows down to each descendant with no own value. A <see cref="Label"/> draws its text in this color.
    /// </para>
    /// </remarks>
    public Rgba8 Color;

    /// <summary>
    /// The font, as an id of the renderer. The id 0 is the default font of the renderer. The value inherits: with no
    /// rule and no inline value, the element gets the font of its parent, and a root gets
    /// <see cref="UiContext.CreateInfo.DefaultFont"/>. The context default is the id 0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The measurer of the context and the renderer of <see cref="UiContext.Draw(IUiRenderer)"/> must use the same
    /// font ids. The ids have a meaning for one renderer only.
    /// </para>
    /// <para>
    /// The value is a text field. A change marks <see cref="DirtyFlags.Paint"/> and restyles each child. A
    /// <see cref="Label"/> measures and draws its text in this font, so a change on a label also marks
    /// <see cref="DirtyFlags.Layout"/>.
    /// </para>
    /// </remarks>
    public FontHandle Font;

    /// <summary>
    /// The font size, in pixels. A size of 0 means the default size of the font. The value inherits: with no rule and
    /// no inline value, the element gets the size of its parent, and a root gets
    /// <see cref="UiContext.CreateInfo.DefaultFontSize"/>. The context default is 0.
    /// </summary>
    /// <remarks>
    /// The value is a text field. A change marks <see cref="DirtyFlags.Paint"/> and restyles each child. The compare
    /// uses <see cref="float.Equals(float)"/>, so <c>NaN</c> to <c>NaN</c> is no change. A <see cref="Label"/>
    /// measures and draws its text at this size, so a change on a label also marks <see cref="DirtyFlags.Layout"/>.
    /// </remarks>
    public float FontSize;

    private float _opacity;
    private bool _hasOpacity;

    /// <summary>
    /// The opacity of the element and its subtree, from 0 (hidden) to 1 (opaque). The default is 1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This member is a property and not a field. A float field gives 0 for <c>default(ComputedStyle)</c>, and an
    /// opacity of 0 hides the element. Thus a new element would draw nothing. The property keeps a flag that tells
    /// whether code set a value. The getter gives 1 when code set no value. A set value reads back exactly.
    /// </para>
    /// <para>
    /// The draw walk clamps a value below 0 to 0 and a value above 1 to 1. <c>NaN</c> counts as 1.
    /// An opacity of 0 hides the element and its subtree: the walk draws nothing for them.
    /// The walk multiplies the opacity down the subtree, and the product scales the alpha of each color.
    /// The value has no effect on the layout. A change marks <see cref="DirtyFlags.Paint"/>.
    /// </para>
    /// </remarks>
    public float Opacity
    {
        readonly get => _hasOpacity ? _opacity : 1f;
        set
        {
            _opacity = value;
            _hasOpacity = true;
        }
    }
}
