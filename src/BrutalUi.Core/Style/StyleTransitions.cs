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

    private const string SlotMessage = "The value is not a slot index. Use a value from 0 to 4.";

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
    /// <remarks>
    /// The slot order is <see cref="StyleProperty.BackgroundColor"/> 0, <see cref="StyleProperty.BorderColor"/> 1,
    /// <see cref="StyleProperty.CornerRadius"/> 2, <see cref="StyleProperty.Opacity"/> 3, and
    /// <see cref="StyleProperty.Color"/> 4. <see cref="PropertyOf(int)"/> gives the inverse map.
    /// </remarks>
    /// <param name="property">The member.</param>
    /// <returns>The slot index from 0 to <see cref="SlotCount"/> - 1, or -1.</returns>
    internal static int SlotOf(StyleProperty property)
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

    /// <summary>The number of slots: one for each member that can transition.</summary>
    internal const int SlotCount = 5;

    /// <summary>Gives the member of a slot. This is the inverse map of <see cref="SlotOf(StyleProperty)"/>.</summary>
    /// <param name="slot">The slot index, from 0 to <see cref="SlotCount"/> - 1.</param>
    /// <returns>The member of the slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is not a slot index.</exception>
    internal static StyleProperty PropertyOf(int slot)
    {
        switch (slot)
        {
            case 0: return StyleProperty.BackgroundColor;
            case 1: return StyleProperty.BorderColor;
            case 2: return StyleProperty.CornerRadius;
            case 3: return StyleProperty.Opacity;
            case 4: return StyleProperty.Color;
            default: throw new ArgumentOutOfRangeException(nameof(slot), slot, SlotMessage);
        }
    }

    /// <summary>Checks whether the set holds a transition in a slot.</summary>
    /// <param name="slot">The slot index.</param>
    /// <returns>
    /// <see langword="true"/> when the bit of the slot is set. <see langword="false"/> for an index outside 0 to
    /// <see cref="SlotCount"/> - 1.
    /// </returns>
    internal readonly bool IsSlotSet(int slot)
    {
        return (uint)slot < SlotCount && (_mask & (1 << slot)) != 0;
    }

    /// <summary>Gets the transition of a slot.</summary>
    /// <param name="slot">The slot index, from 0 to <see cref="SlotCount"/> - 1.</param>
    /// <returns>The transition of the slot, or <c>default(StyleTransition)</c> when the set holds none.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is not a slot index.</exception>
    internal readonly StyleTransition GetSlot(int slot)
    {
        switch (slot)
        {
            case 0: return _backgroundColor;
            case 1: return _borderColor;
            case 2: return _cornerRadius;
            case 3: return _opacity;
            case 4: return _color;
            default: throw new ArgumentOutOfRangeException(nameof(slot), slot, SlotMessage);
        }
    }

    /// <summary>
    /// Copies each slot that <paramref name="later"/> holds into this set and sets its bit. A slot that
    /// <paramref name="later"/> does not hold does not change.
    /// </summary>
    /// <remarks>
    /// The style pass calls this method for each matched rule in the order of the cascade. Thus the last matched rule
    /// that holds a slot wins for that slot (R1). The method allocates nothing.
    /// </remarks>
    /// <param name="later">The set of a later rule in the cascade.</param>
    internal void MergeFrom(in StyleTransitions later)
    {
        byte mask = later._mask;
        if (mask == 0)
        {
            return;
        }

        if ((mask & (1 << 0)) != 0)
        {
            _backgroundColor = later._backgroundColor;
        }

        if ((mask & (1 << 1)) != 0)
        {
            _borderColor = later._borderColor;
        }

        if ((mask & (1 << 2)) != 0)
        {
            _cornerRadius = later._cornerRadius;
        }

        if ((mask & (1 << 3)) != 0)
        {
            _opacity = later._opacity;
        }

        if ((mask & (1 << 4)) != 0)
        {
            _color = later._color;
        }

        _mask |= mask;
    }
}
