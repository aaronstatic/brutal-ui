using System;

namespace BrutalUi.Core;

/// <summary>
/// The curve that a transition follows from the old value to the new value.
/// </summary>
/// <remarks>
/// The members follow the CSS keywords <c>linear</c>, <c>ease-in</c>, <c>ease-out</c>, and <c>ease-in-out</c>.
/// The transitions feature gives the curve of each member.
/// </remarks>
public enum TransitionEasing : byte
{
    /// <summary>The value changes at a constant rate. This is the default.</summary>
    Linear,

    /// <summary>The value changes slowly at the start and quickly at the end.</summary>
    EaseIn,

    /// <summary>The value changes quickly at the start and slowly at the end.</summary>
    EaseOut,

    /// <summary>The value changes slowly at the start and at the end, and quickly in the middle.</summary>
    EaseInOut,
}

/// <summary>
/// The transition of one style member: a duration and an easing.
/// </summary>
/// <remarks>
/// <para>
/// The model keeps each duration as it is: 0, a negative value, <c>NaN</c>, and infinity stay in the value.
/// The transitions feature applies its rule to each of them. The parser rejects a negative duration.
/// </para>
/// <para><c>default(StyleTransition)</c> has a duration of 0 and the easing <see cref="TransitionEasing.Linear"/>.</para>
/// </remarks>
public readonly struct StyleTransition
{
    /// <summary>Creates a transition.</summary>
    /// <param name="duration">The duration in seconds. The model keeps each value, also 0, <c>NaN</c>, and infinity.</param>
    /// <param name="easing">The curve of the transition.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="easing"/> is not a member of <see cref="TransitionEasing"/>.</exception>
    public StyleTransition(float duration, TransitionEasing easing)
    {
        if (easing > TransitionEasing.EaseInOut)
        {
            throw new ArgumentOutOfRangeException(
                nameof(easing),
                easing,
                "The value is not a member of TransitionEasing. Use Linear, EaseIn, EaseOut, or EaseInOut.");
        }

        Duration = duration;
        Easing = easing;
    }

    /// <summary>Gets the duration in seconds.</summary>
    /// <value>The duration that the constructor got. The model does not change it.</value>
    public float Duration { get; }

    /// <summary>Gets the curve of the transition.</summary>
    public TransitionEasing Easing { get; }
}

/// <summary>
/// A sparse set of transitions: one slot for each style member that can transition.
/// </summary>
/// <remarks>
/// <para>
/// Five members can transition: <see cref="StyleProperty.BackgroundColor"/>, <see cref="StyleProperty.BorderColor"/>,
/// <see cref="StyleProperty.CornerRadius"/>, <see cref="StyleProperty.Opacity"/>, and <see cref="StyleProperty.Color"/>.
/// These are the colors, the corner radius, and the opacity. A change of one of them changes the draw only, so a tween
/// marks <see cref="DirtyFlags.Paint"/> only. <see cref="CanTransition(StyleProperty)"/> gives these five members.
/// </para>
/// <para>
/// The set holds five fixed slots and a mask with one bit for each slot. <see cref="Set"/> writes a slot and sets its
/// bit. <see cref="Clear"/> clears the bit and puts back <c>default(StyleTransition)</c>. A set of a member that holds
/// a transition replaces the transition.
/// </para>
/// <para>
/// The model keeps each duration as it is: 0, a negative value, <c>NaN</c>, and infinity stay in the slot. The
/// transitions feature applies its rule to each of them.
/// </para>
/// <para>
/// This is a mutable value struct. It holds no heap object, so a copy costs no allocation.
/// <c>default(StyleTransitions)</c> is the empty set.
/// </para>
/// </remarks>
public struct StyleTransitions
{
    private const string MemberMessage =
        "The member cannot transition. Use BackgroundColor, BorderColor, CornerRadius, Opacity, or Color.";

    private StyleTransition _backgroundColor;
    private StyleTransition _borderColor;
    private StyleTransition _cornerRadius;
    private StyleTransition _opacity;
    private StyleTransition _color;
    private byte _mask;

    /// <summary>Gets a value that shows whether the set holds no transition.</summary>
    public readonly bool IsEmpty => _mask == 0;

    /// <summary>Checks whether a member can transition.</summary>
    /// <param name="property">The member to check.</param>
    /// <returns>
    /// <see langword="true"/> for <see cref="StyleProperty.BackgroundColor"/>, <see cref="StyleProperty.BorderColor"/>,
    /// <see cref="StyleProperty.CornerRadius"/>, <see cref="StyleProperty.Opacity"/>, and <see cref="StyleProperty.Color"/>.
    /// <see langword="false"/> for each other value, also for a value that is not a member of <see cref="StyleProperty"/>.
    /// </returns>
    public static bool CanTransition(StyleProperty property)
    {
        return SlotOf(property) >= 0;
    }

    /// <summary>Checks whether the set holds a transition for a member.</summary>
    /// <param name="property">The member to check.</param>
    /// <returns>
    /// <see langword="true"/> when <see cref="Set"/> wrote the member and no <see cref="Clear"/> came after it.
    /// <see langword="false"/> for a member that cannot transition.
    /// </returns>
    public readonly bool IsSet(StyleProperty property)
    {
        int slot = SlotOf(property);
        return slot >= 0 && (_mask & (1 << slot)) != 0;
    }

    /// <summary>Gets the transition of a member.</summary>
    /// <param name="property">The member. It must be one of the five members that can transition.</param>
    /// <returns>The transition of the member, or <c>default(StyleTransition)</c> when the set holds none.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="property"/> cannot transition.</exception>
    public readonly StyleTransition Get(StyleProperty property)
    {
        switch (property)
        {
            case StyleProperty.BackgroundColor: return _backgroundColor;
            case StyleProperty.BorderColor: return _borderColor;
            case StyleProperty.CornerRadius: return _cornerRadius;
            case StyleProperty.Opacity: return _opacity;
            case StyleProperty.Color: return _color;
            default: throw new ArgumentOutOfRangeException(nameof(property), property, MemberMessage);
        }
    }

    /// <summary>Writes the transition of a member. The set keeps a copy.</summary>
    /// <param name="property">The member. It must be one of the five members that can transition.</param>
    /// <param name="transition">The transition. It replaces the transition that the member holds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="property"/> cannot transition.</exception>
    public void Set(StyleProperty property, StyleTransition transition)
    {
        switch (property)
        {
            case StyleProperty.BackgroundColor: _backgroundColor = transition; break;
            case StyleProperty.BorderColor: _borderColor = transition; break;
            case StyleProperty.CornerRadius: _cornerRadius = transition; break;
            case StyleProperty.Opacity: _opacity = transition; break;
            case StyleProperty.Color: _color = transition; break;
            default: throw new ArgumentOutOfRangeException(nameof(property), property, MemberMessage);
        }

        _mask |= (byte)(1 << SlotOf(property));
    }

    /// <summary>
    /// Removes the transition of a member. The bit goes to 0, and <see cref="Get"/> gives <c>default(StyleTransition)</c> again.
    /// </summary>
    /// <param name="property">
    /// The member to clear. A clear of an unset member changes nothing. A clear of a member that cannot transition also
    /// changes nothing, because the set never holds a transition for it.
    /// </param>
    public void Clear(StyleProperty property)
    {
        int slot = SlotOf(property);
        if (slot < 0)
        {
            return;
        }

        _mask &= (byte)~(1 << slot);
        switch (property)
        {
            case StyleProperty.BackgroundColor: _backgroundColor = default; break;
            case StyleProperty.BorderColor: _borderColor = default; break;
            case StyleProperty.CornerRadius: _cornerRadius = default; break;
            case StyleProperty.Opacity: _opacity = default; break;
            case StyleProperty.Color: _color = default; break;
        }
    }

    /// <summary>Gives the slot index of a member, or -1 for a member that cannot transition.</summary>
    private static int SlotOf(StyleProperty property)
    {
        switch (property)
        {
            case StyleProperty.BackgroundColor: return 0;
            case StyleProperty.BorderColor: return 1;
            case StyleProperty.CornerRadius: return 2;
            case StyleProperty.Opacity: return 3;
            case StyleProperty.Color: return 4;
            default: return -1;
        }
    }
}
