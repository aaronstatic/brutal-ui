using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of <see cref="UiGamepadDeadzone"/>, <see cref="UiGamepadAxis"/>, and the new reads of
/// <see cref="UiGamepadState"/>. Each test pins one rule with hand-made values: the zero inside the stick zone,
/// the clamp at full tilt, the rescale and the kept direction, the trigger map, the trigger zone, the range check,
/// the deadzone-of-1 and non-finite reads, the state reads, and the enum order.
/// </summary>
/// <remarks>
/// The helper is pure and holds no state, so no test needs a gamepad or a display. Each float compare uses five
/// decimal places. A test never compares a <see cref="float2"/> with the equality operator.
/// </remarks>
public sealed class GamepadDeadzoneTests
{
    /// <summary>A stick at a length up to the deadzone reads (0, 0).</summary>
    [Fact]
    public void AStickInsideTheDeadzoneReadsZero()
    {
        AssertStick(new float2(0f, 0f), UiGamepadDeadzone.ApplyStick(new float2(0.1f, 0.05f), 0.15f));
        AssertStick(new float2(0f, 0f), UiGamepadDeadzone.ApplyStick(new float2(0.15f, 0f), 0.15f));
        AssertStick(new float2(0f, 0f), UiGamepadDeadzone.ApplyStick(new float2(0f, 0f), 0.15f));
    }

    /// <summary>A stick at full tilt reads a length of 1.</summary>
    [Fact]
    public void AStickAtFullTiltReadsLengthOne()
    {
        AssertStick(new float2(1f, 0f), UiGamepadDeadzone.ApplyStick(new float2(1f, 0f), 0.15f));
        AssertStick(new float2(0f, -1f), UiGamepadDeadzone.ApplyStick(new float2(0f, -1f), 0.15f));
        AssertStick(new float2(0.707107f, 0.707107f), UiGamepadDeadzone.ApplyStick(new float2(1f, 1f), 0.15f));

        // A raw length above 1 clamps to 1.
        AssertStick(new float2(1f, 0f), UiGamepadDeadzone.ApplyStick(new float2(2f, 0f), 0f));
    }

    /// <summary>The stick rescales the length and keeps the direction.</summary>
    [Fact]
    public void TheStickRescalesTheLengthAndKeepsTheDirection()
    {
        AssertStick(new float2(0.5f, 0f), UiGamepadDeadzone.ApplyStick(new float2(0.575f, 0f), 0.15f));
        AssertStick(new float2(0.247059f, 0.329412f), UiGamepadDeadzone.ApplyStick(new float2(0.3f, 0.4f), 0.15f));

        // With no deadzone, a length of 1 keeps its value.
        AssertStick(new float2(0.6f, 0.8f), UiGamepadDeadzone.ApplyStick(new float2(0.6f, 0.8f), 0f));
    }

    /// <summary>The trigger maps the GLFW range of -1 to 1 onto 0 to 1.</summary>
    [Fact]
    public void ATriggerMapsTheGlfwRangeToZeroToOne()
    {
        Assert.Equal(0f, UiGamepadDeadzone.ApplyTrigger(-1f, 0f), 5);
        Assert.Equal(0.5f, UiGamepadDeadzone.ApplyTrigger(0f, 0f), 5);
        Assert.Equal(1f, UiGamepadDeadzone.ApplyTrigger(1f, 0f), 5);

        // A value outside the GLFW range clamps first.
        Assert.Equal(1f, UiGamepadDeadzone.ApplyTrigger(2f, 0f), 5);
        Assert.Equal(0f, UiGamepadDeadzone.ApplyTrigger(-2f, 0f), 5);
    }

    /// <summary>The trigger applies the linear deadzone at the low end of the 0 to 1 scale.</summary>
    [Fact]
    public void ATriggerAppliesTheLinearDeadzoneAtTheLowEnd()
    {
        Assert.Equal(0f, UiGamepadDeadzone.ApplyTrigger(-1f, 0.1f), 5);
        Assert.Equal(0f, UiGamepadDeadzone.ApplyTrigger(-0.9f, 0.1f), 5);
        Assert.Equal(0.444444f, UiGamepadDeadzone.ApplyTrigger(0f, 0.1f), 5);
        Assert.Equal(0.722222f, UiGamepadDeadzone.ApplyTrigger(0.5f, 0.1f), 5);
        Assert.Equal(1f, UiGamepadDeadzone.ApplyTrigger(1f, 0.1f), 5);
    }

    /// <summary>Each method rejects a deadzone outside 0 to 1 and accepts the bounds.</summary>
    [Fact]
    public void TheHelperRejectsADeadzoneOutsideTheRange()
    {
        AssertRejects(-0.01f);
        AssertRejects(1.01f);
        AssertRejects(float.NaN);

        // The bounds do not throw.
        _ = UiGamepadDeadzone.ApplyStick(new float2(0.5f, 0.5f), 0f);
        _ = UiGamepadDeadzone.ApplyStick(new float2(0.5f, 0.5f), 1f);
        _ = UiGamepadDeadzone.ApplyTrigger(0.5f, 0f);
        _ = UiGamepadDeadzone.ApplyTrigger(0.5f, 1f);
    }

    /// <summary>A deadzone of 1 and a non-finite input read zero.</summary>
    [Fact]
    public void ADeadzoneOfOneAndANonFiniteInputReadZero()
    {
        // A deadzone of 1 kills each input.
        AssertStick(new float2(0f, 0f), UiGamepadDeadzone.ApplyStick(new float2(1f, 1f), 1f));
        Assert.Equal(0f, UiGamepadDeadzone.ApplyTrigger(1f, 1f), 5);

        // A non-finite input reads zero for a normal deadzone.
        AssertStick(new float2(0f, 0f), UiGamepadDeadzone.ApplyStick(new float2(float.NaN, 0f), 0.15f));
        AssertStick(new float2(0f, 0f), UiGamepadDeadzone.ApplyStick(new float2(float.PositiveInfinity, 0f), 0.15f));
        Assert.Equal(0f, UiGamepadDeadzone.ApplyTrigger(float.NaN, 0.15f), 5);
        Assert.Equal(0f, UiGamepadDeadzone.ApplyTrigger(float.PositiveInfinity, 0.15f), 5);
    }

    /// <summary>The state reads both sticks, each named axis, and the same value as the int read.</summary>
    [Fact]
    public void TheStateReadsTheSticksAndTheNamedAxes()
    {
        UiGamepadState state = new(UiGamepadButtons.None, [0.1f, -0.2f, 0.3f, -0.4f, 0.5f, 0.6f]);

        AssertStick(new float2(0.1f, -0.2f), state.LeftStick);
        AssertStick(new float2(0.3f, -0.4f), state.RightStick);
        Assert.Equal(0.5f, state.Axis(UiGamepadAxis.LeftTrigger), 5);
        Assert.Equal(0.6f, state.Axis(UiGamepadAxis.RightTrigger), 5);

        // Each named axis equals the int read for the same index.
        foreach (UiGamepadAxis axis in Enum.GetValues<UiGamepadAxis>())
        {
            Assert.Equal(state.Axis((int)axis), state.Axis(axis));
        }

        // An enum value outside 0 to 5 throws with the parameter name axis.
        ArgumentOutOfRangeException tooBig = Assert.Throws<ArgumentOutOfRangeException>(
            () => state.Axis((UiGamepadAxis)6));
        Assert.Equal("axis", tooBig.ParamName);
        ArgumentOutOfRangeException tooSmall = Assert.Throws<ArgumentOutOfRangeException>(
            () => state.Axis((UiGamepadAxis)(-1)));
        Assert.Equal("axis", tooSmall.ParamName);

        // A default state reads two zero sticks.
        UiGamepadState empty = default;
        AssertStick(new float2(0f, 0f), empty.LeftStick);
        AssertStick(new float2(0f, 0f), empty.RightStick);
    }

    /// <summary>The enum follows the GLFW axis order, 0 to 5, and matches the axis count.</summary>
    [Fact]
    public void TheAxisEnumFollowsTheGlfwAxisOrder()
    {
        Assert.Equal(0, (int)UiGamepadAxis.LeftStickX);
        Assert.Equal(1, (int)UiGamepadAxis.LeftStickY);
        Assert.Equal(2, (int)UiGamepadAxis.RightStickX);
        Assert.Equal(3, (int)UiGamepadAxis.RightStickY);
        Assert.Equal(4, (int)UiGamepadAxis.LeftTrigger);
        Assert.Equal(5, (int)UiGamepadAxis.RightTrigger);

        Assert.Equal(UiGamepadState.AxisCount, Enum.GetValues<UiGamepadAxis>().Length);
    }

    /// <summary>Asserts that each method rejects the deadzone with the parameter name deadzone.</summary>
    private static void AssertRejects(float deadzone)
    {
        ArgumentOutOfRangeException stickError = Assert.Throws<ArgumentOutOfRangeException>(
            () => UiGamepadDeadzone.ApplyStick(new float2(0.5f, 0.5f), deadzone));
        Assert.Equal("deadzone", stickError.ParamName);

        ArgumentOutOfRangeException triggerError = Assert.Throws<ArgumentOutOfRangeException>(
            () => UiGamepadDeadzone.ApplyTrigger(0.5f, deadzone));
        Assert.Equal("deadzone", triggerError.ParamName);
    }

    /// <summary>Asserts that two sticks match, each component to five decimal places.</summary>
    private static void AssertStick(float2 expected, float2 actual)
    {
        Assert.Equal(expected.X, actual.X, 5);
        Assert.Equal(expected.Y, actual.Y, 5);
    }
}
