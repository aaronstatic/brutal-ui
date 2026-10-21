using System;

namespace BrutalUi.Core;

/// <summary>
/// A member of <see cref="ComputedStyle"/> that a <see cref="StyleProperties"/> set can hold.
/// </summary>
/// <remarks>
/// <para>
/// The numeric value of each member is its bit index in the set mask of <see cref="StyleProperties"/>.
/// The bit of a member is <c>1UL &lt;&lt; (int)member</c>. The values stay below 64.
/// </para>
/// <para>
/// The first 25 members are layout members. A change of one of them changes the layout.
/// The next 4 members are paint members. A change of one of them changes the draw only.
/// <see cref="Overflow"/> is a layout member.
/// </para>
/// <para>
/// The last 3 members are text members (bits 29 to 31). They inherit: an element with no value gets the value of
/// its parent. A change of one of them changes the draw. A change of <see cref="StyleProperty.Font"/> or <see cref="StyleProperty.FontSize"/> also changes the layout of a <see cref="Label"/>.
/// </para>
/// </remarks>
public enum StyleProperty : byte
{
    /// <summary>The member <see cref="ComputedStyle.FlexDirection"/>. A layout member.</summary>
    FlexDirection,

    /// <summary>The member <see cref="ComputedStyle.Width"/>. A layout member.</summary>
    Width,

    /// <summary>The member <see cref="ComputedStyle.Height"/>. A layout member.</summary>
    Height,

    /// <summary>The member <see cref="ComputedStyle.Margin"/>. A layout member.</summary>
    Margin,

    /// <summary>The member <see cref="ComputedStyle.Padding"/>. A layout member.</summary>
    Padding,

    /// <summary>The member <see cref="ComputedStyle.BorderWidth"/>. A layout member.</summary>
    BorderWidth,

    /// <summary>The member <see cref="ComputedStyle.FlexGrow"/>. A layout member.</summary>
    FlexGrow,

    /// <summary>The member <see cref="ComputedStyle.FlexShrink"/>. A layout member.</summary>
    FlexShrink,

    /// <summary>The member <see cref="ComputedStyle.FlexBasis"/>. A layout member.</summary>
    FlexBasis,

    /// <summary>The member <see cref="ComputedStyle.MinWidth"/>. A layout member.</summary>
    MinWidth,

    /// <summary>The member <see cref="ComputedStyle.MinHeight"/>. A layout member.</summary>
    MinHeight,

    /// <summary>The member <see cref="ComputedStyle.MaxWidth"/>. A layout member.</summary>
    MaxWidth,

    /// <summary>The member <see cref="ComputedStyle.MaxHeight"/>. A layout member.</summary>
    MaxHeight,

    /// <summary>The member <see cref="ComputedStyle.JustifyContent"/>. A layout member.</summary>
    JustifyContent,

    /// <summary>The member <see cref="ComputedStyle.AlignItems"/>. A layout member.</summary>
    AlignItems,

    /// <summary>The member <see cref="ComputedStyle.AlignSelf"/>. A layout member.</summary>
    AlignSelf,

    /// <summary>The member <see cref="ComputedStyle.AlignContent"/>. A layout member.</summary>
    AlignContent,

    /// <summary>The member <see cref="ComputedStyle.FlexWrap"/>. A layout member.</summary>
    FlexWrap,

    /// <summary>The member <see cref="ComputedStyle.Display"/>. A layout member.</summary>
    Display,

    /// <summary>The member <see cref="ComputedStyle.Position"/>. A layout member.</summary>
    Position,

    /// <summary>The member <see cref="ComputedStyle.Left"/>. A layout member.</summary>
    Left,

    /// <summary>The member <see cref="ComputedStyle.Top"/>. A layout member.</summary>
    Top,

    /// <summary>The member <see cref="ComputedStyle.Right"/>. A layout member.</summary>
    Right,

    /// <summary>The member <see cref="ComputedStyle.Bottom"/>. A layout member.</summary>
    Bottom,

    /// <summary>The member <see cref="ComputedStyle.Overflow"/>. A layout member.</summary>
    Overflow,

    /// <summary>The member <see cref="ComputedStyle.BackgroundColor"/>. A paint member.</summary>
    BackgroundColor,

    /// <summary>The member <see cref="ComputedStyle.BorderColor"/>. A paint member.</summary>
    BorderColor,

    /// <summary>The member <see cref="ComputedStyle.CornerRadius"/>. A paint member.</summary>
    CornerRadius,

    /// <summary>The member <see cref="ComputedStyle.Opacity"/>. A paint member.</summary>
    Opacity,

    /// <summary>The member <see cref="ComputedStyle.Color"/>. A text member. It inherits.</summary>
    Color,

    /// <summary>The member <see cref="ComputedStyle.Font"/>. A text member. It inherits.</summary>
    Font,

    /// <summary>The member <see cref="ComputedStyle.FontSize"/>. A text member. It inherits.</summary>
    FontSize,
}

/// <summary>
/// The bit masks of the <see cref="StyleProperty"/> groups. Each member has its bit in one mask only.
/// </summary>
internal static class StyleMasks
{
    /// <summary>The bits of the 25 layout members. A change of one of them changes the layout.</summary>
    internal const ulong Layout =
        (1UL << (int)StyleProperty.FlexDirection)
        | (1UL << (int)StyleProperty.Width)
        | (1UL << (int)StyleProperty.Height)
        | (1UL << (int)StyleProperty.Margin)
        | (1UL << (int)StyleProperty.Padding)
        | (1UL << (int)StyleProperty.BorderWidth)
        | (1UL << (int)StyleProperty.FlexGrow)
        | (1UL << (int)StyleProperty.FlexShrink)
        | (1UL << (int)StyleProperty.FlexBasis)
        | (1UL << (int)StyleProperty.MinWidth)
        | (1UL << (int)StyleProperty.MinHeight)
        | (1UL << (int)StyleProperty.MaxWidth)
        | (1UL << (int)StyleProperty.MaxHeight)
        | (1UL << (int)StyleProperty.JustifyContent)
        | (1UL << (int)StyleProperty.AlignItems)
        | (1UL << (int)StyleProperty.AlignSelf)
        | (1UL << (int)StyleProperty.AlignContent)
        | (1UL << (int)StyleProperty.FlexWrap)
        | (1UL << (int)StyleProperty.Display)
        | (1UL << (int)StyleProperty.Position)
        | (1UL << (int)StyleProperty.Left)
        | (1UL << (int)StyleProperty.Top)
        | (1UL << (int)StyleProperty.Right)
        | (1UL << (int)StyleProperty.Bottom)
        | (1UL << (int)StyleProperty.Overflow);

    /// <summary>The bits of the 4 paint members. A change of one of them changes the draw only.</summary>
    internal const ulong Paint =
        (1UL << (int)StyleProperty.BackgroundColor)
        | (1UL << (int)StyleProperty.BorderColor)
        | (1UL << (int)StyleProperty.CornerRadius)
        | (1UL << (int)StyleProperty.Opacity);

    /// <summary>
    /// The bits of the 3 text members. The members inherit. A change of one of them changes the draw.
    /// </summary>
    internal const ulong Text =
        (1UL << (int)StyleProperty.Color)
        | (1UL << (int)StyleProperty.Font)
        | (1UL << (int)StyleProperty.FontSize);

    /// <summary>The bits of each member. A bit outside this mask is not a member.</summary>
    internal const ulong All = Layout | Paint | Text;
}

/// <summary>
/// A sparse set of style values. The set holds a value for some members of <see cref="ComputedStyle"/> and no value
/// for the others.
/// </summary>
/// <remarks>
/// <para>
/// The bit rule: the set keeps one bit for each <see cref="StyleProperty"/> member in a 64-bit mask. The bit of a
/// member is <c>1UL &lt;&lt; (int)member</c>. A setter writes the value and sets the bit. <see cref="Clear"/> clears
/// the bit and puts back the default value. A set to the default value also sets the bit, so the set holds the
/// default as a value.
/// </para>
/// <para>
/// The getter of an unset member gives the value of <c>default(ComputedStyle)</c> for that member. For example,
/// <see cref="Opacity"/> gives 1 and <see cref="Width"/> gives <see cref="Length.Undefined"/>.
/// Use <see cref="IsSet(StyleProperty)"/> to tell an unset member from a set default.
/// </para>
/// <para>
/// This is a mutable value struct. It holds no heap object, so a copy costs no allocation.
/// A nested write such as <c>properties.Margin.Top = 12;</c> does not compile (CS1612), because the getter gives a
/// copy. Write the whole value: <c>properties.Margin = LengthEdges.All(12);</c>.
/// </para>
/// </remarks>
public struct StyleProperties
{
    // The fields are in the order of size and not in the order of the enum, so the struct has little padding.
    private ulong _mask;

    private LengthEdges _margin;
    private LengthEdges _padding;
    private LengthEdges _borderWidth;
    private Corners _cornerRadius;

    private Length _width;
    private Length _height;
    private Length _flexBasis;
    private Length _minWidth;
    private Length _minHeight;
    private Length _maxWidth;
    private Length _maxHeight;
    private Length _left;
    private Length _top;
    private Length _right;
    private Length _bottom;

    private float _flexGrow;
    private float _flexShrink;
    private float _opacity;
    private float _fontSize;

    private Rgba8 _backgroundColor;
    private Rgba8 _borderColor;
    private Rgba8 _color;
    private FontHandle _font;

    private FlexDirection _flexDirection;
    private Justify _justifyContent;
    private Align _alignItems;
    private Align _alignSelf;
    private Align _alignContent;
    private FlexWrap _flexWrap;
    private Display _display;
    private PositionType _position;
    private Overflow _overflow;

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.FlexDirection"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Core.FlexDirection.Column"/>.</value>
    public FlexDirection FlexDirection
    {
        readonly get => _flexDirection;
        set
        {
            _flexDirection = value;
            _mask |= 1UL << (int)StyleProperty.FlexDirection;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Width"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length Width
    {
        readonly get => _width;
        set
        {
            _width = value;
            _mask |= 1UL << (int)StyleProperty.Width;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Height"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length Height
    {
        readonly get => _height;
        set
        {
            _height = value;
            _mask |= 1UL << (int)StyleProperty.Height;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Margin"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/> on each edge.</value>
    public LengthEdges Margin
    {
        readonly get => _margin;
        set
        {
            _margin = value;
            _mask |= 1UL << (int)StyleProperty.Margin;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Padding"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/> on each edge.</value>
    public LengthEdges Padding
    {
        readonly get => _padding;
        set
        {
            _padding = value;
            _mask |= 1UL << (int)StyleProperty.Padding;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.BorderWidth"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/> on each edge.</value>
    public LengthEdges BorderWidth
    {
        readonly get => _borderWidth;
        set
        {
            _borderWidth = value;
            _mask |= 1UL << (int)StyleProperty.BorderWidth;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.FlexGrow"/>.</summary>
    /// <value>The value. An unset member gives 0.</value>
    public float FlexGrow
    {
        readonly get => _flexGrow;
        set
        {
            _flexGrow = value;
            _mask |= 1UL << (int)StyleProperty.FlexGrow;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.FlexShrink"/>.</summary>
    /// <value>The value. An unset member gives 0.</value>
    public float FlexShrink
    {
        readonly get => _flexShrink;
        set
        {
            _flexShrink = value;
            _mask |= 1UL << (int)StyleProperty.FlexShrink;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.FlexBasis"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length FlexBasis
    {
        readonly get => _flexBasis;
        set
        {
            _flexBasis = value;
            _mask |= 1UL << (int)StyleProperty.FlexBasis;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.MinWidth"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length MinWidth
    {
        readonly get => _minWidth;
        set
        {
            _minWidth = value;
            _mask |= 1UL << (int)StyleProperty.MinWidth;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.MinHeight"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length MinHeight
    {
        readonly get => _minHeight;
        set
        {
            _minHeight = value;
            _mask |= 1UL << (int)StyleProperty.MinHeight;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.MaxWidth"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length MaxWidth
    {
        readonly get => _maxWidth;
        set
        {
            _maxWidth = value;
            _mask |= 1UL << (int)StyleProperty.MaxWidth;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.MaxHeight"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length MaxHeight
    {
        readonly get => _maxHeight;
        set
        {
            _maxHeight = value;
            _mask |= 1UL << (int)StyleProperty.MaxHeight;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.JustifyContent"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Justify.FlexStart"/>.</value>
    public Justify JustifyContent
    {
        readonly get => _justifyContent;
        set
        {
            _justifyContent = value;
            _mask |= 1UL << (int)StyleProperty.JustifyContent;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.AlignItems"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Align.Auto"/>.</value>
    public Align AlignItems
    {
        readonly get => _alignItems;
        set
        {
            _alignItems = value;
            _mask |= 1UL << (int)StyleProperty.AlignItems;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.AlignSelf"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Align.Auto"/>.</value>
    public Align AlignSelf
    {
        readonly get => _alignSelf;
        set
        {
            _alignSelf = value;
            _mask |= 1UL << (int)StyleProperty.AlignSelf;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.AlignContent"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Align.Auto"/>.</value>
    public Align AlignContent
    {
        readonly get => _alignContent;
        set
        {
            _alignContent = value;
            _mask |= 1UL << (int)StyleProperty.AlignContent;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.FlexWrap"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Core.FlexWrap.NoWrap"/>.</value>
    public FlexWrap FlexWrap
    {
        readonly get => _flexWrap;
        set
        {
            _flexWrap = value;
            _mask |= 1UL << (int)StyleProperty.FlexWrap;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Display"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Core.Display.Flex"/>.</value>
    public Display Display
    {
        readonly get => _display;
        set
        {
            _display = value;
            _mask |= 1UL << (int)StyleProperty.Display;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Position"/>.</summary>
    /// <value>The value. An unset member gives <see cref="PositionType.Relative"/>.</value>
    public PositionType Position
    {
        readonly get => _position;
        set
        {
            _position = value;
            _mask |= 1UL << (int)StyleProperty.Position;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Left"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length Left
    {
        readonly get => _left;
        set
        {
            _left = value;
            _mask |= 1UL << (int)StyleProperty.Left;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Top"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length Top
    {
        readonly get => _top;
        set
        {
            _top = value;
            _mask |= 1UL << (int)StyleProperty.Top;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Right"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length Right
    {
        readonly get => _right;
        set
        {
            _right = value;
            _mask |= 1UL << (int)StyleProperty.Right;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Bottom"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Length.Undefined"/>.</value>
    public Length Bottom
    {
        readonly get => _bottom;
        set
        {
            _bottom = value;
            _mask |= 1UL << (int)StyleProperty.Bottom;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Overflow"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Core.Overflow.Visible"/>.</value>
    public Overflow Overflow
    {
        readonly get => _overflow;
        set
        {
            _overflow = value;
            _mask |= 1UL << (int)StyleProperty.Overflow;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.BackgroundColor"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Rgba8.Transparent"/>.</value>
    public Rgba8 BackgroundColor
    {
        readonly get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            _mask |= 1UL << (int)StyleProperty.BackgroundColor;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.BorderColor"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Rgba8.Transparent"/>.</value>
    public Rgba8 BorderColor
    {
        readonly get => _borderColor;
        set
        {
            _borderColor = value;
            _mask |= 1UL << (int)StyleProperty.BorderColor;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.CornerRadius"/>.</summary>
    /// <value>The value. An unset member gives 0 on each corner.</value>
    public Corners CornerRadius
    {
        readonly get => _cornerRadius;
        set
        {
            _cornerRadius = value;
            _mask |= 1UL << (int)StyleProperty.CornerRadius;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Opacity"/>.</summary>
    /// <value>The value. An unset member gives 1. A set value reads back exactly.</value>
    public float Opacity
    {
        readonly get => (_mask & (1UL << (int)StyleProperty.Opacity)) != 0 ? _opacity : 1f;
        set
        {
            _opacity = value;
            _mask |= 1UL << (int)StyleProperty.Opacity;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Color"/>.</summary>
    /// <value>The value. An unset member gives <see cref="Rgba8.Transparent"/>. The style pass gives the parent value to a member that no set holds.</value>
    public Rgba8 Color
    {
        readonly get => _color;
        set
        {
            _color = value;
            _mask |= 1UL << (int)StyleProperty.Color;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.Font"/>.</summary>
    /// <value>The value. An unset member gives the id 0. The style pass gives the parent value to a member that no set holds.</value>
    public FontHandle Font
    {
        readonly get => _font;
        set
        {
            _font = value;
            _mask |= 1UL << (int)StyleProperty.Font;
        }
    }

    /// <summary>Gets or sets the value for <see cref="ComputedStyle.FontSize"/>.</summary>
    /// <value>The value. An unset member gives 0. The style pass gives the parent value to a member that no set holds.</value>
    public float FontSize
    {
        readonly get => _fontSize;
        set
        {
            _fontSize = value;
            _mask |= 1UL << (int)StyleProperty.FontSize;
        }
    }

    /// <summary>Gets a value that shows whether the set holds no value.</summary>
    public readonly bool IsEmpty => _mask == 0;

    /// <summary>Gets the set mask: one bit for each member that holds a value.</summary>
    internal readonly ulong SetMask => _mask;

    /// <summary>Checks whether the set holds a value for a member.</summary>
    /// <param name="property">The member to check.</param>
    /// <returns><see langword="true"/> when a setter wrote the member and no <see cref="Clear"/> came after it.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="property"/> is not a member of <see cref="StyleProperty"/>.</exception>
    public readonly bool IsSet(StyleProperty property)
    {
        return (_mask & BitOf(property)) != 0;
    }

    /// <summary>Removes the value of a member. The bit goes to 0, and the getter gives the default again.</summary>
    /// <param name="property">The member to clear. A clear of an unset member changes nothing.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="property"/> is not a member of <see cref="StyleProperty"/>.</exception>
    public void Clear(StyleProperty property)
    {
        _mask &= ~BitOf(property);

        switch (property)
        {
            case StyleProperty.FlexDirection: _flexDirection = default; break;
            case StyleProperty.Width: _width = default; break;
            case StyleProperty.Height: _height = default; break;
            case StyleProperty.Margin: _margin = default; break;
            case StyleProperty.Padding: _padding = default; break;
            case StyleProperty.BorderWidth: _borderWidth = default; break;
            case StyleProperty.FlexGrow: _flexGrow = default; break;
            case StyleProperty.FlexShrink: _flexShrink = default; break;
            case StyleProperty.FlexBasis: _flexBasis = default; break;
            case StyleProperty.MinWidth: _minWidth = default; break;
            case StyleProperty.MinHeight: _minHeight = default; break;
            case StyleProperty.MaxWidth: _maxWidth = default; break;
            case StyleProperty.MaxHeight: _maxHeight = default; break;
            case StyleProperty.JustifyContent: _justifyContent = default; break;
            case StyleProperty.AlignItems: _alignItems = default; break;
            case StyleProperty.AlignSelf: _alignSelf = default; break;
            case StyleProperty.AlignContent: _alignContent = default; break;
            case StyleProperty.FlexWrap: _flexWrap = default; break;
            case StyleProperty.Display: _display = default; break;
            case StyleProperty.Position: _position = default; break;
            case StyleProperty.Left: _left = default; break;
            case StyleProperty.Top: _top = default; break;
            case StyleProperty.Right: _right = default; break;
            case StyleProperty.Bottom: _bottom = default; break;
            case StyleProperty.Overflow: _overflow = default; break;
            case StyleProperty.BackgroundColor: _backgroundColor = default; break;
            case StyleProperty.BorderColor: _borderColor = default; break;
            case StyleProperty.CornerRadius: _cornerRadius = default; break;
            case StyleProperty.Opacity: _opacity = default; break;
            case StyleProperty.Color: _color = default; break;
            case StyleProperty.Font: _font = default; break;
            case StyleProperty.FontSize: _fontSize = default; break;
        }
    }

    /// <summary>Writes each set member that <paramref name="skip"/> does not hold to a computed style.</summary>
    /// <param name="target">The style to write. The method does not touch a member with no write.</param>
    /// <param name="skip">The bits of the members to skip. Use 0 to write each set member.</param>
    internal readonly void ApplyTo(ref ComputedStyle target, ulong skip)
    {
        ulong apply = _mask & ~skip;
        if (apply == 0)
        {
            return;
        }

        if ((apply & (1UL << (int)StyleProperty.FlexDirection)) != 0)
        {
            target.FlexDirection = _flexDirection;
        }

        if ((apply & (1UL << (int)StyleProperty.Width)) != 0)
        {
            target.Width = _width;
        }

        if ((apply & (1UL << (int)StyleProperty.Height)) != 0)
        {
            target.Height = _height;
        }

        if ((apply & (1UL << (int)StyleProperty.Margin)) != 0)
        {
            target.Margin = _margin;
        }

        if ((apply & (1UL << (int)StyleProperty.Padding)) != 0)
        {
            target.Padding = _padding;
        }

        if ((apply & (1UL << (int)StyleProperty.BorderWidth)) != 0)
        {
            target.BorderWidth = _borderWidth;
        }

        if ((apply & (1UL << (int)StyleProperty.FlexGrow)) != 0)
        {
            target.FlexGrow = _flexGrow;
        }

        if ((apply & (1UL << (int)StyleProperty.FlexShrink)) != 0)
        {
            target.FlexShrink = _flexShrink;
        }

        if ((apply & (1UL << (int)StyleProperty.FlexBasis)) != 0)
        {
            target.FlexBasis = _flexBasis;
        }

        if ((apply & (1UL << (int)StyleProperty.MinWidth)) != 0)
        {
            target.MinWidth = _minWidth;
        }

        if ((apply & (1UL << (int)StyleProperty.MinHeight)) != 0)
        {
            target.MinHeight = _minHeight;
        }

        if ((apply & (1UL << (int)StyleProperty.MaxWidth)) != 0)
        {
            target.MaxWidth = _maxWidth;
        }

        if ((apply & (1UL << (int)StyleProperty.MaxHeight)) != 0)
        {
            target.MaxHeight = _maxHeight;
        }

        if ((apply & (1UL << (int)StyleProperty.JustifyContent)) != 0)
        {
            target.JustifyContent = _justifyContent;
        }

        if ((apply & (1UL << (int)StyleProperty.AlignItems)) != 0)
        {
            target.AlignItems = _alignItems;
        }

        if ((apply & (1UL << (int)StyleProperty.AlignSelf)) != 0)
        {
            target.AlignSelf = _alignSelf;
        }

        if ((apply & (1UL << (int)StyleProperty.AlignContent)) != 0)
        {
            target.AlignContent = _alignContent;
        }

        if ((apply & (1UL << (int)StyleProperty.FlexWrap)) != 0)
        {
            target.FlexWrap = _flexWrap;
        }

        if ((apply & (1UL << (int)StyleProperty.Display)) != 0)
        {
            target.Display = _display;
        }

        if ((apply & (1UL << (int)StyleProperty.Position)) != 0)
        {
            target.Position = _position;
        }

        if ((apply & (1UL << (int)StyleProperty.Left)) != 0)
        {
            target.Left = _left;
        }

        if ((apply & (1UL << (int)StyleProperty.Top)) != 0)
        {
            target.Top = _top;
        }

        if ((apply & (1UL << (int)StyleProperty.Right)) != 0)
        {
            target.Right = _right;
        }

        if ((apply & (1UL << (int)StyleProperty.Bottom)) != 0)
        {
            target.Bottom = _bottom;
        }

        if ((apply & (1UL << (int)StyleProperty.Overflow)) != 0)
        {
            target.Overflow = _overflow;
        }

        if ((apply & (1UL << (int)StyleProperty.BackgroundColor)) != 0)
        {
            target.BackgroundColor = _backgroundColor;
        }

        if ((apply & (1UL << (int)StyleProperty.BorderColor)) != 0)
        {
            target.BorderColor = _borderColor;
        }

        if ((apply & (1UL << (int)StyleProperty.CornerRadius)) != 0)
        {
            target.CornerRadius = _cornerRadius;
        }

        if ((apply & (1UL << (int)StyleProperty.Opacity)) != 0)
        {
            target.Opacity = _opacity;
        }

        if ((apply & (1UL << (int)StyleProperty.Color)) != 0)
        {
            target.Color = _color;
        }

        if ((apply & (1UL << (int)StyleProperty.Font)) != 0)
        {
            target.Font = _font;
        }

        if ((apply & (1UL << (int)StyleProperty.FontSize)) != 0)
        {
            target.FontSize = _fontSize;
        }
    }

    /// <summary>Gives the mask bit of a member, and throws for a value that is not a member.</summary>
    private static ulong BitOf(StyleProperty property)
    {
        int index = (int)property;
        ulong bit = index < 64 ? 1UL << index : 0UL;
        if ((bit & StyleMasks.All) == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(property), property, "The value is not a member of StyleProperty. Use a named member of StyleProperty, from FlexDirection to FontSize.");
        }

        return bit;
    }
}
