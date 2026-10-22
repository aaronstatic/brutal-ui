using System;

namespace BrutalUi.Core;

/// <summary>
/// The easing curves of the transitions. A curve maps the progress of a tween to the part of the change that the
/// tween shows.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TransitionEasing.Linear"/> gives the progress. The three other members are the CSS cubic Bezier curves.
/// A curve starts at (0, 0), ends at (1, 1), and has two control points (x1, y1) and (x2, y2):
/// </para>
/// <list type="bullet">
/// <item><description><see cref="TransitionEasing.EaseIn"/>: (0.42, 0) and (1, 1).</description></item>
/// <item><description><see cref="TransitionEasing.EaseOut"/>: (0, 0) and (0.58, 1).</description></item>
/// <item><description><see cref="TransitionEasing.EaseInOut"/>: (0.42, 0) and (0.58, 1).</description></item>
/// </list>
/// <para>
/// The reference values at a progress of 0.5: <see cref="TransitionEasing.EaseIn"/> gives 0.31536,
/// <see cref="TransitionEasing.EaseOut"/> gives 0.68464, and <see cref="TransitionEasing.EaseInOut"/> gives 0.5.
/// </para>
/// <para>
/// The solver finds the curve parameter <c>t</c> where the x value of the curve equals the progress. It takes up to 8
/// Newton steps and stops at an error below 1e-6. When Newton fails (the slope is nearly 0 or a step leaves the range
/// 0 to 1), it takes up to 32 bisection steps on 0 to 1. Then it gives the y value of the curve at <c>t</c>. The class
/// holds no state and no table, and it allocates nothing.
/// </para>
/// </remarks>
internal static class TransitionCurves
{
    /// <summary>The largest x error at which the solver stops.</summary>
    private const float Tolerance = 1e-6f;

    /// <summary>The slope of the x value below which a Newton step is not safe.</summary>
    private const float MinSlope = 1e-6f;

    /// <summary>The number of Newton steps.</summary>
    private const int NewtonSteps = 8;

    /// <summary>The number of bisection steps.</summary>
    private const int BisectionSteps = 32;

    /// <summary>Gives the value of a curve at a progress.</summary>
    /// <param name="easing">The curve. It must be a member of <see cref="TransitionEasing"/>.</param>
    /// <param name="progress">
    /// The progress of the tween: the elapsed time divided by the duration. The method clamps it to 0 to 1. A value of
    /// 0 or less gives 0, also <c>NaN</c>. A value of 1 or more gives 1, also infinity. Thus the ends are exact.
    /// </param>
    /// <returns>The part of the change, from 0 to 1.</returns>
    internal static float Evaluate(TransitionEasing easing, float progress)
    {
        // The negated compare also catches NaN, so a NaN progress never reaches the solver.
        if (!(progress > 0f))
        {
            return 0f;
        }

        if (progress >= 1f)
        {
            return 1f;
        }

        // The constructor of StyleTransition rejects each other value, so the last branch is EaseInOut.
        if (easing == TransitionEasing.Linear)
        {
            return progress;
        }

        if (easing == TransitionEasing.EaseIn)
        {
            return Solve(0.42f, 0f, 1f, 1f, progress);
        }

        if (easing == TransitionEasing.EaseOut)
        {
            return Solve(0f, 0f, 0.58f, 1f, progress);
        }

        return Solve(0.42f, 0f, 0.58f, 1f, progress);
    }

    /// <summary>Gives the y value of a cubic Bezier curve at the x value of a progress.</summary>
    /// <param name="x1">The x value of the first control point.</param>
    /// <param name="y1">The y value of the first control point.</param>
    /// <param name="x2">The x value of the second control point.</param>
    /// <param name="y2">The y value of the second control point.</param>
    /// <param name="x">The progress, more than 0 and less than 1.</param>
    /// <returns>The y value.</returns>
    private static float Solve(float x1, float y1, float x2, float y2, float x)
    {
        float t = SolveParameter(x1, x2, x);
        return Sample(y1, y2, t);
    }

    /// <summary>Finds the curve parameter where the x value equals a progress.</summary>
    /// <param name="x1">The x value of the first control point.</param>
    /// <param name="x2">The x value of the second control point.</param>
    /// <param name="x">The progress, more than 0 and less than 1.</param>
    /// <returns>The parameter, from 0 to 1.</returns>
    internal static float SolveParameter(float x1, float x2, float x)
    {
        // Newton: the start guess t = x is exact for the linear case and close for the three curves.
        float t = x;
        for (int i = 0; i < NewtonSteps; i++)
        {
            float error = Sample(x1, x2, t) - x;
            if (MathF.Abs(error) < Tolerance)
            {
                return t;
            }

            float slope = Slope(x1, x2, t);
            if (!(MathF.Abs(slope) >= MinSlope))
            {
                break;
            }

            t -= error / slope;
            if (!(t >= 0f && t <= 1f))
            {
                break;
            }
        }

        // Bisection: the x value of the curve rises with t, so the interval keeps the answer.
        float low = 0f;
        float high = 1f;
        t = 0.5f;
        for (int i = 0; i < BisectionSteps; i++)
        {
            float value = Sample(x1, x2, t);
            if (MathF.Abs(value - x) < Tolerance)
            {
                break;
            }

            if (value < x)
            {
                low = t;
            }
            else
            {
                high = t;
            }

            t = (low + high) * 0.5f;
        }

        return t;
    }

    /// <summary>Gives one coordinate of the curve at a parameter, with the end points 0 and 1.</summary>
    /// <param name="p1">The coordinate of the first control point.</param>
    /// <param name="p2">The coordinate of the second control point.</param>
    /// <param name="t">The parameter.</param>
    /// <returns><c>3(1-t)^2 t p1 + 3(1-t) t^2 p2 + t^3</c>.</returns>
    internal static float Sample(float p1, float p2, float t)
    {
        float u = 1f - t;
        return (3f * u * u * t * p1) + (3f * u * t * t * p2) + (t * t * t);
    }

    /// <summary>Gives the slope of one coordinate of the curve at a parameter.</summary>
    /// <param name="p1">The coordinate of the first control point.</param>
    /// <param name="p2">The coordinate of the second control point.</param>
    /// <param name="t">The parameter.</param>
    /// <returns>The derivative of <see cref="Sample"/> by <c>t</c>.</returns>
    private static float Slope(float p1, float p2, float t)
    {
        float u = 1f - t;
        return (3f * u * u * p1) + (6f * u * t * (p2 - p1)) + (3f * t * t * (1f - p2));
    }
}
