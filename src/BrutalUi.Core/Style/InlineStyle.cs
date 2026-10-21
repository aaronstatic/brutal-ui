using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BrutalUi.Core;

/// <summary>
/// The inline style of an element. The view writes the inline set of its owner. An inline value wins over each rule
/// of each sheet.
/// </summary>
/// <remarks>
/// <para>
/// A set to a new value, or a set of a member with no inline value, writes the value, sets the bit of the member, and
/// marks the owner with <see cref="DirtyFlags.Style"/>. A set to the default value also sets the bit. A set to the
/// same value on a member with an inline value marks nothing. <see cref="Clear(StyleProperty)"/> is the only way back
/// to the rule value. The next <see cref="UiContext.Update(float)"/> writes the result to <see cref="Element.Style"/>.
/// </para>
/// <para>
/// The equality of a set: a float uses <see cref="float.Equals(float)"/>, so <c>NaN</c> equals <c>NaN</c>.
/// <see cref="Length"/>, <see cref="Rgba8"/>, <see cref="FontHandle"/>, and the enums use their own equality. <see cref="LengthEdges"/> and
/// <see cref="Corners"/> compare each part.
/// </para>
/// <para>
/// The nested write: <c>element.Inline.Margin.Top = 12;</c> does not compile (CS1612), because the getter gives a
/// copy of the edges. Write the whole value: <c>element.Inline.Margin = element.Inline.Margin with { Top = 12 };</c>,
/// <c>new LengthEdges(left, top, right, bottom)</c>, or <c>LengthEdges.All(value)</c>. The same rule applies to
/// <see cref="CornerRadius"/>.
/// </para>
/// <para>
/// Get a view from <see cref="Element.Inline"/>. The view allocates nothing. A default instance has no owner. Each
/// member of it throws <see cref="InvalidOperationException"/>.
/// </para>
/// </remarks>
public readonly struct InlineStyle
{
    private readonly Element? _owner;

    /// <summary>Creates a view on the inline set of an element.</summary>
    /// <param name="owner">The element that owns the inline set.</param>
    internal InlineStyle(Element owner)
    {
        _owner = owner;
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.FlexDirection"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Core.FlexDirection.Column"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public FlexDirection FlexDirection
    {
        get => Owner.InlineStorage.FlexDirection;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.FlexDirection) && set.FlexDirection == value)
            {
                return;
            }

            set.FlexDirection = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Width"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length Width
    {
        get => Owner.InlineStorage.Width;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Width) && set.Width.Equals(value))
            {
                return;
            }

            set.Width = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Height"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length Height
    {
        get => Owner.InlineStorage.Height;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Height) && set.Height.Equals(value))
            {
                return;
            }

            set.Height = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Margin"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/> on each edge.</value>
    /// <remarks>Write the whole value. A write of one edge does not compile (CS1612).</remarks>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public LengthEdges Margin
    {
        get => Owner.InlineStorage.Margin;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Margin) && StylePass.EdgesEqual(set.Margin, value))
            {
                return;
            }

            set.Margin = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Padding"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/> on each edge.</value>
    /// <remarks>Write the whole value. A write of one edge does not compile (CS1612).</remarks>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public LengthEdges Padding
    {
        get => Owner.InlineStorage.Padding;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Padding) && StylePass.EdgesEqual(set.Padding, value))
            {
                return;
            }

            set.Padding = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.BorderWidth"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/> on each edge.</value>
    /// <remarks>Write the whole value. A write of one edge does not compile (CS1612).</remarks>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public LengthEdges BorderWidth
    {
        get => Owner.InlineStorage.BorderWidth;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.BorderWidth) && StylePass.EdgesEqual(set.BorderWidth, value))
            {
                return;
            }

            set.BorderWidth = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.FlexGrow"/>.</summary>
    /// <value>The inline value. A member with no inline value gives 0.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public float FlexGrow
    {
        get => Owner.InlineStorage.FlexGrow;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.FlexGrow) && set.FlexGrow.Equals(value))
            {
                return;
            }

            set.FlexGrow = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.FlexShrink"/>.</summary>
    /// <value>The inline value. A member with no inline value gives 0.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public float FlexShrink
    {
        get => Owner.InlineStorage.FlexShrink;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.FlexShrink) && set.FlexShrink.Equals(value))
            {
                return;
            }

            set.FlexShrink = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.FlexBasis"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length FlexBasis
    {
        get => Owner.InlineStorage.FlexBasis;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.FlexBasis) && set.FlexBasis.Equals(value))
            {
                return;
            }

            set.FlexBasis = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.MinWidth"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length MinWidth
    {
        get => Owner.InlineStorage.MinWidth;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.MinWidth) && set.MinWidth.Equals(value))
            {
                return;
            }

            set.MinWidth = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.MinHeight"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length MinHeight
    {
        get => Owner.InlineStorage.MinHeight;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.MinHeight) && set.MinHeight.Equals(value))
            {
                return;
            }

            set.MinHeight = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.MaxWidth"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length MaxWidth
    {
        get => Owner.InlineStorage.MaxWidth;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.MaxWidth) && set.MaxWidth.Equals(value))
            {
                return;
            }

            set.MaxWidth = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.MaxHeight"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length MaxHeight
    {
        get => Owner.InlineStorage.MaxHeight;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.MaxHeight) && set.MaxHeight.Equals(value))
            {
                return;
            }

            set.MaxHeight = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.JustifyContent"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="Justify"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Justify JustifyContent
    {
        get => Owner.InlineStorage.JustifyContent;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.JustifyContent) && set.JustifyContent == value)
            {
                return;
            }

            set.JustifyContent = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.AlignItems"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="Align"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Align AlignItems
    {
        get => Owner.InlineStorage.AlignItems;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.AlignItems) && set.AlignItems == value)
            {
                return;
            }

            set.AlignItems = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.AlignSelf"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="Align"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Align AlignSelf
    {
        get => Owner.InlineStorage.AlignSelf;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.AlignSelf) && set.AlignSelf == value)
            {
                return;
            }

            set.AlignSelf = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.AlignContent"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="Align"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Align AlignContent
    {
        get => Owner.InlineStorage.AlignContent;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.AlignContent) && set.AlignContent == value)
            {
                return;
            }

            set.AlignContent = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.FlexWrap"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="Core.FlexWrap"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public FlexWrap FlexWrap
    {
        get => Owner.InlineStorage.FlexWrap;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.FlexWrap) && set.FlexWrap == value)
            {
                return;
            }

            set.FlexWrap = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Display"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="Core.Display"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Display Display
    {
        get => Owner.InlineStorage.Display;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Display) && set.Display == value)
            {
                return;
            }

            set.Display = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Position"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="PositionType"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public PositionType Position
    {
        get => Owner.InlineStorage.Position;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Position) && set.Position == value)
            {
                return;
            }

            set.Position = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Left"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length Left
    {
        get => Owner.InlineStorage.Left;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Left) && set.Left.Equals(value))
            {
                return;
            }

            set.Left = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Top"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length Top
    {
        get => Owner.InlineStorage.Top;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Top) && set.Top.Equals(value))
            {
                return;
            }

            set.Top = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Right"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length Right
    {
        get => Owner.InlineStorage.Right;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Right) && set.Right.Equals(value))
            {
                return;
            }

            set.Right = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Bottom"/>.</summary>
    /// <value>The inline value. A member with no inline value gives <see cref="Length.Undefined"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Length Bottom
    {
        get => Owner.InlineStorage.Bottom;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Bottom) && set.Bottom.Equals(value))
            {
                return;
            }

            set.Bottom = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Overflow"/>.</summary>
    /// <value>The inline value. A member with no inline value gives the first member of <see cref="Core.Overflow"/>.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Overflow Overflow
    {
        get => Owner.InlineStorage.Overflow;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Overflow) && set.Overflow == value)
            {
                return;
            }

            set.Overflow = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.BackgroundColor"/>.</summary>
    /// <value>The inline value. A member with no inline value gives transparent black.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Rgba8 BackgroundColor
    {
        get => Owner.InlineStorage.BackgroundColor;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.BackgroundColor) && set.BackgroundColor.Equals(value))
            {
                return;
            }

            set.BackgroundColor = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.BorderColor"/>.</summary>
    /// <value>The inline value. A member with no inline value gives transparent black.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Rgba8 BorderColor
    {
        get => Owner.InlineStorage.BorderColor;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.BorderColor) && set.BorderColor.Equals(value))
            {
                return;
            }

            set.BorderColor = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.CornerRadius"/>.</summary>
    /// <value>The inline value. A member with no inline value gives 0 on each corner.</value>
    /// <remarks>Write the whole value. A write of one corner does not compile (CS1612).</remarks>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Corners CornerRadius
    {
        get => Owner.InlineStorage.CornerRadius;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.CornerRadius) && StylePass.CornersEqual(set.CornerRadius, value))
            {
                return;
            }

            set.CornerRadius = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Opacity"/>.</summary>
    /// <value>The inline value. A member with no inline value gives 1.</value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public float Opacity
    {
        get => Owner.InlineStorage.Opacity;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Opacity) && set.Opacity.Equals(value))
            {
                return;
            }

            set.Opacity = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Color"/>, the text color.</summary>
    /// <value>
    /// The inline value. A member with no inline value gives transparent black. The style pass then gives the element
    /// the rule value or the color of its parent.
    /// </value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public Rgba8 Color
    {
        get => Owner.InlineStorage.Color;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Color) && set.Color.Equals(value))
            {
                return;
            }

            set.Color = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.Font"/>.</summary>
    /// <value>
    /// The inline value. A member with no inline value gives the id 0. The style pass then gives the element the rule
    /// value or the font of its parent.
    /// </value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public FontHandle Font
    {
        get => Owner.InlineStorage.Font;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.Font) && set.Font.Equals(value))
            {
                return;
            }

            set.Font = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Gets or sets the inline value of <see cref="ComputedStyle.FontSize"/>, in pixels.</summary>
    /// <value>
    /// The inline value. A member with no inline value gives 0. The style pass then gives the element the rule value
    /// or the size of its parent.
    /// </value>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public float FontSize
    {
        get => Owner.InlineStorage.FontSize;
        set
        {
            Element owner = Owner;
            ref StyleProperties set = ref owner.InlineStorage;
            if (Has(set.SetMask, StyleProperty.FontSize) && set.FontSize.Equals(value))
            {
                return;
            }

            set.FontSize = value;
            MarkWritten(owner);
        }
    }

    /// <summary>Checks whether the element holds an inline value for a member.</summary>
    /// <param name="property">The member to check.</param>
    /// <returns><see langword="true"/> when a setter wrote the member and no <see cref="Clear(StyleProperty)"/> came after it.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="property"/> is not a member of <see cref="StyleProperty"/>.</exception>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public bool IsSet(StyleProperty property) => Owner.InlineStorage.IsSet(property);

    /// <summary>
    /// Removes the inline value of a member. The next <see cref="UiContext.Update(float)"/> gives the rule value again,
    /// or the default value when no rule sets the member. A text member with no rule gets the value of the parent.
    /// </summary>
    /// <remarks>A clear of a member with an inline value marks the owner with <see cref="DirtyFlags.Style"/>. A clear of a member with no inline value marks nothing.</remarks>
    /// <param name="property">The member to clear.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="property"/> is not a member of <see cref="StyleProperty"/>.</exception>
    /// <exception cref="InvalidOperationException">The view has no owner.</exception>
    public void Clear(StyleProperty property)
    {
        Element owner = Owner;
        ref StyleProperties set = ref owner.InlineStorage;

        // IsSet also rejects a value that is not a member.
        if (!set.IsSet(property))
        {
            return;
        }

        set.Clear(property);
        owner.MarkDirty(DirtyFlags.Style);
    }

    /// <summary>Gets the owner of the view.</summary>
    /// <exception cref="InvalidOperationException">The view is a default instance with no owner.</exception>
    private Element Owner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Element? owner = _owner;
            if (owner is null)
            {
                ThrowNoOwner();
            }

            return owner;
        }
    }

    /// <summary>Checks the bit of a member in a set mask.</summary>
    /// <param name="mask">The set mask.</param>
    /// <param name="property">The member.</param>
    /// <returns><see langword="true"/> when the mask holds the bit of the member.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Has(ulong mask, StyleProperty property) => (mask & (1UL << (int)property)) != 0;

    /// <summary>Marks the owner of an inline write with <see cref="DirtyFlags.Style"/>.</summary>
    /// <param name="owner">The owner of the view.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MarkWritten(Element owner)
    {
        owner.MarkDirty(DirtyFlags.Style);
    }

    /// <summary>Throws the error of a default instance.</summary>
    /// <exception cref="InvalidOperationException">Always.</exception>
    [DoesNotReturn]
    private static void ThrowNoOwner()
    {
        throw new InvalidOperationException("The InlineStyle has no owner. Use Element.Inline to get a view.");
    }
}
