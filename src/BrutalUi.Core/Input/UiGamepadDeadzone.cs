using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// Applies a deadzone to one gamepad axis. The helper is pure, holds no state, and allocates nothing.
/// </summary>
/// <remarks>
/// <para>
/// The poll of <c>BrutalUi.Glfw</c> applies this helper, and <see cref="UiGamepadState"/> stores the result
/// (Q1). Each reader then sees one value, and a zero always means no input above the zone.
/// </para>
/// <para>
/// A <c>deadzone</c> that is <see cref="float.NaN"/> or is outside 0 to 1 throws
/// <see cref="ArgumentOutOfRangeException"/>. Both methods check it first.
/// </para>
/// </remarks>
public static class UiGamepadDeadzone
{
    /// <summary>
    /// Applies the radial deadzone to one stick and returns the stored value.
    /// </summary>
    /// <param name="stick">
    /// The raw stick value, in the GLFW range. The X component is <see cref="UiGamepadAxis.LeftStickX"/> or
    /// <see cref="UiGamepadAxis.RightStickX"/>, and the Y component is the GLFW Y value.
    /// </param>
    /// <param name="deadzone">The deadzone, from 0 to 1.</param>
    /// <returns>The stick value after the deadzone, with a length from 0 to 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="deadzone"/> is <see cref="float.NaN"/> or is outside 0 to 1. The parameter name is
    /// <c>deadzone</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The rule is radial. A length up to <paramref name="deadzone"/> gives (0, 0). A length above it keeps its
    /// direction and rescales its length to <c>(len - deadzone) / (1 - deadzone)</c>. The result clamps to a
    /// length of 1, because a GLFW stick with a square gate gives a length up to about 1.414 on a diagonal.
    /// </para>
    /// <para>
    /// A <paramref name="deadzone"/> of 1 gives (0, 0) for each input. A component that is not finite gives
    /// (0, 0).
    /// </para>
    /// </remarks>
    public static float2 ApplyStick(float2 stick, float deadzone)
    {
        if (!(deadzone >= 0f && deadzone <= 1f))
        {
            throw new ArgumentOutOfRangeException(nameof(deadzone), deadzone, "The deadzone must be from 0 to 1.");
        }

        float x = stick.X;
        float y = stick.Y;
        if (!float.IsFinite(x) || !float.IsFinite(y))
        {
            return new float2(0f, 0f);
        }

        float len = MathF.Sqrt(x * x + y * y);
        if (!(len > deadzone) || deadzone >= 1f)
        {
            return new float2(0f, 0f);
        }

        float scale = MathF.Min((len - deadzone) / (1f - deadzone), 1f);
        float k = scale / len;
        return new float2(x * k, y * k);
    }

    /// <summary>
    /// Maps one GLFW trigger to 0 to 1 and applies the linear deadzone.
    /// </summary>
    /// <param name="value">The raw trigger value, in the GLFW range of -1 to 1.</param>
    /// <param name="deadzone">The deadzone on the 0 to 1 scale, from 0 to 1.</param>
    /// <returns>The trigger value after the deadzone, from 0 to 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="deadzone"/> is <see cref="float.NaN"/> or is outside 0 to 1. The parameter name is
    /// <c>deadzone</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The method first maps the GLFW range of -1 to 1 onto 0 to 1 with <c>(value + 1) / 2</c>, clamped to 0 to 1.
    /// The deadzone then sits at the low end of that 0 to 1 scale (Q2): a value up to
    /// <paramref name="deadzone"/> gives 0, and a value above it rescales to <c>(t - deadzone) / (1 - deadzone)</c>.
    /// </para>
    /// <para>
    /// A <paramref name="deadzone"/> of 1 gives 0 for each input. A value that is not finite gives 0.
    /// </para>
    /// </remarks>
    public static float ApplyTrigger(float value, float deadzone)
    {
        if (!(deadzone >= 0f && deadzone <= 1f))
        {
            throw new ArgumentOutOfRangeException(nameof(deadzone), deadzone, "The deadzone must be from 0 to 1.");
        }

        if (!float.IsFinite(value))
        {
            return 0f;
        }

        float t = MathF.Min(MathF.Max((value + 1f) * 0.5f, 0f), 1f);
        if (!(t > deadzone) || deadzone >= 1f)
        {
            return 0f;
        }

        return (t - deadzone) / (1f - deadzone);
    }
}
