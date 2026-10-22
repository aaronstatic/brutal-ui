using System.Runtime.CompilerServices;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The tween of one style member: the start value, the target, the elapsed time, the duration, and the curve.
/// </summary>
/// <remarks>
/// <para>
/// A value goes into four floats. A color holds R, G, B, and A, each from 0 to 255. The corners hold top-left,
/// top-right, bottom-right, and bottom-left. The opacity holds its value in the first float, and the other floats
/// hold 0. <see cref="TransitionPool"/> holds the map from a member to the four floats.
/// </para>
/// <para>The slot holds no heap object.</para>
/// </remarks>
internal struct TweenSlot
{
    /// <summary>The visible value at the start of the tween, as four floats.</summary>
    internal float4 From;

    /// <summary>The target of the cascade, as four floats. The tween ends on this exact value.</summary>
    internal float4 To;

    /// <summary>The seconds since the start of the tween.</summary>
    internal float Elapsed;

    /// <summary>The duration in seconds. The value is finite and more than 0, so the progress never divides by 0.</summary>
    internal float Duration;

    /// <summary>The curve of the tween.</summary>
    internal TransitionEasing Easing;
}

/// <summary>
/// The inline buffer of the tweens of one element: one slot for each member that can transition.
/// </summary>
/// <remarks>
/// The slot order is the slot order of <see cref="StyleTransitions"/>: <see cref="StyleProperty.BackgroundColor"/> 0,
/// <see cref="StyleProperty.BorderColor"/> 1, <see cref="StyleProperty.CornerRadius"/> 2,
/// <see cref="StyleProperty.Opacity"/> 3, and <see cref="StyleProperty.Color"/> 4. The buffer lives inline in
/// <see cref="TransitionRecord"/>. Code reads a slot by index.
/// </remarks>
[InlineArray(StyleTransitions.SlotCount)]
internal struct TweenSlots
{
    /// <summary>The first slot. The compiler gives the other slots.</summary>
    private TweenSlot _element0;
}

/// <summary>
/// The running tweens of one element. <see cref="TransitionPool"/> holds one record for each element with a running
/// tween.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Mask"/> holds one bit for each running slot, in the slot order of <see cref="StyleTransitions"/>. A slot
/// with no bit holds stale data, and the pool does not read it. The pool releases the record when no bit stays.
/// </para>
/// <para>
/// <see cref="Follow"/> is a mark of <see cref="TransitionPool.Advance"/> only. Outside that method, the field holds
/// <see langword="false"/>. The field fits in the padding after <see cref="Mask"/>, so the record does not grow.
/// </para>
/// </remarks>
internal struct TransitionRecord
{
    /// <summary>The element of the record. A released record holds <see langword="null"/>.</summary>
    internal Element Element;

    /// <summary>One bit for each running slot. Bit <c>n</c> is slot <c>n</c>. No other bit has a meaning.</summary>
    internal byte Mask;

    /// <summary>
    /// The follow mark: the record ran a <see cref="ComputedStyle.Color"/> step in this call of
    /// <see cref="TransitionPool.Advance"/>. The first loop of the pass sets it. The second loop reads it, runs the
    /// follow walk, and clears it.
    /// </summary>
    internal bool Follow;

    /// <summary>The tween of each slot. Only a slot with a bit in <see cref="Mask"/> runs.</summary>
    internal TweenSlots Slots;
}
