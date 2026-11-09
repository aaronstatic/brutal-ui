using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Brutal.GlfwApi;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;
using GlfwApi = Brutal.GlfwApi.Glfw;

namespace BrutalUi.Glfw.Tests;

/// <summary>
/// Tests of <see cref="GlfwUiInput.PollGamepads()"/>, of the conversion <c>GamepadState</c>, and of the two deadzone
/// settings. Most tests use the test seam with no window and no GLFW call. The last test starts GLFW on the null
/// platform, needs no display, and skips when the platform is not supported or the init fails.
/// </summary>
/// <remarks>
/// <para>
/// The class is in the collection of <see cref="GlfwUiInputAllocationTests"/>, because the null-platform test starts
/// and stops GLFW and sets a global init hint, and GLFW state is global.
/// </para>
/// <para>
/// Each float compare uses five decimal places. The measured bodies use no lambda, no string, no array creation, and
/// no <c>Assert</c>.
/// </para>
/// </remarks>
[Collection(GlfwUiInputAllocationTests.CollectionName)]
public sealed class GlfwUiInputGamepadPollTests
{
    private const int ButtonCount = 15;
    private const int AxisCount = 6;
    private const int GamepadCount = 16;
    private const int WarmUpPolls = 2;
    private const int PollCount = 100;

    private static GlfwUiInput CreateSeamAdapter()
    {
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() });
        return new GlfwUiInput(context);
    }

    /// <summary>The measured body of the poll tests. It uses no lambda, no string, no array creation, and no Assert.</summary>
    private static void PollGamepads(GlfwUiInput input, int count)
    {
        for (int i = 0; i < count; i++)
        {
            input.PollGamepads();
        }
    }

    /// <summary>
    /// The measured body of the conversion test: convert, write the slot, and sum three rounded reads. It uses no
    /// lambda, no string, no array creation, and no Assert.
    /// </summary>
    private static int ConversionBody(GlfwUiInput input, byte[] buttons, float[] axes, int count)
    {
        int sum = 0;
        for (int round = 0; round < count; round++)
        {
            UiGamepadState state = GlfwUiInput.GamepadState(
                buttons, axes, input.StickDeadzone, input.TriggerDeadzone);
            input.Context.SetGamepad(round % GamepadCount, in state);
            sum += (int)MathF.Round(state.LeftStick.X)
                + (int)MathF.Round(state.RightStick.X)
                + (int)MathF.Round(state.Axis(UiGamepadAxis.LeftTrigger));
        }

        return sum;
    }

    [Fact]
    public void TheAxisBufferAndTheAxisConstantsFollowGlfw()
    {
        // PollGamepads reads the fixed buffer with a fixed length. A shorter buffer in a new package must fail here.
        FieldInfo axesField = typeof(GlfwGamepadState).GetField(nameof(GlfwGamepadState.Axes))!;
        FixedBufferAttribute buffer = Assert.IsType<FixedBufferAttribute>(
            Assert.Single(axesField.GetCustomAttributes(typeof(FixedBufferAttribute), false)));
        Assert.Equal(AxisCount, buffer.Length);
        Assert.Equal(typeof(float), buffer.ElementType);

        Assert.Equal(UiGamepadAxis.LeftStickX, (UiGamepadAxis)GlfwApi.Constants.GLFW_GAMEPAD_AXIS_LEFT_X);
        Assert.Equal(UiGamepadAxis.LeftStickY, (UiGamepadAxis)GlfwApi.Constants.GLFW_GAMEPAD_AXIS_LEFT_Y);
        Assert.Equal(UiGamepadAxis.RightStickX, (UiGamepadAxis)GlfwApi.Constants.GLFW_GAMEPAD_AXIS_RIGHT_X);
        Assert.Equal(UiGamepadAxis.RightStickY, (UiGamepadAxis)GlfwApi.Constants.GLFW_GAMEPAD_AXIS_RIGHT_Y);
        Assert.Equal(UiGamepadAxis.LeftTrigger, (UiGamepadAxis)GlfwApi.Constants.GLFW_GAMEPAD_AXIS_LEFT_TRIGGER);
        Assert.Equal(UiGamepadAxis.RightTrigger, (UiGamepadAxis)GlfwApi.Constants.GLFW_GAMEPAD_AXIS_RIGHT_TRIGGER);

        Assert.Equal(AxisCount, GlfwApi.Constants.GLFW_GAMEPAD_AXIS_LAST + 1);
    }

    [Fact]
    public void TheConversionMapsEachGlfwAxis()
    {
        byte[] buttons = new byte[ButtonCount];
        buttons[GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_A] = (byte)GlfwButtonAction.Press;
        buttons[GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_START] = (byte)GlfwButtonAction.Press;

        float[] axes = [0.5f, -0.25f, -0.75f, 1f, -1f, 1f];
        UiGamepadState state = GlfwUiInput.GamepadState(buttons, axes, 0f, 0f);

        Assert.True(state.Connected);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.Start, state.Buttons);
        Assert.Equal(0.5f, state.LeftStick.X, 5);
        Assert.Equal(-0.25f, state.LeftStick.Y, 5);

        // The plan text names (-0.75, 1) here, but the Phase-1 helper clamps the length to 1 also with a deadzone
        // of 0 (see AStickAtFullTiltReadsLengthOne): (−0.75, 1) has length 1.25, so it reads (−0.6, 0.8).
        Assert.Equal(-0.6f, state.RightStick.X, 5);
        Assert.Equal(0.8f, state.RightStick.Y, 5);
        Assert.Equal(0f, state.Axis(UiGamepadAxis.LeftTrigger), 5);
        Assert.Equal(1f, state.Axis(UiGamepadAxis.RightTrigger), 5);

        // The trigger map runs on each trigger: axis 4 of 0 reads 0.5 and axis 5 of -1 reads 0.
        float[] triggers = [0f, 0f, 0f, 0f, 0f, -1f];
        UiGamepadState second = GlfwUiInput.GamepadState(buttons, triggers, 0f, 0f);
        Assert.Equal(0.5f, second.Axis(UiGamepadAxis.LeftTrigger), 5);
        Assert.Equal(0f, second.Axis(UiGamepadAxis.RightTrigger), 5);
    }

    [Fact]
    public void TheConversionAppliesTheDeadzoneSettings()
    {
        float[] axes = [0.1f, 0.05f, 0.575f, 0f, 0f, -1f];
        UiGamepadState state = GlfwUiInput.GamepadState(ReadOnlySpan<byte>.Empty, axes, 0.15f, 0.1f);

        Assert.Equal(0f, state.LeftStick.X, 5);
        Assert.Equal(0f, state.LeftStick.Y, 5);
        Assert.Equal(0.5f, state.RightStick.X, 5);
        Assert.Equal(0f, state.RightStick.Y, 5);
        Assert.Equal(0.444444f, state.Axis(UiGamepadAxis.LeftTrigger), 5);
        Assert.Equal(0f, state.Axis(UiGamepadAxis.RightTrigger), 5);

        // The pin for the poll wiring. Read the two settings through the properties of a live adapter, the same
        // values that PollGamepads passes to the conversion. The properties equal the literals above, so the two
        // conversions must agree on every stored value. A swapped getter, a cross-wired setter, or a cached
        // default breaks a compare here.
        GlfwUiInput input = CreateSeamAdapter();
        input.StickDeadzone = 0.15f;
        input.TriggerDeadzone = 0.1f;
        UiGamepadState viaProps = GlfwUiInput.GamepadState(
            ReadOnlySpan<byte>.Empty, axes, input.StickDeadzone, input.TriggerDeadzone);

        Assert.Equal(state.LeftStick.X, viaProps.LeftStick.X, 5);
        Assert.Equal(state.LeftStick.Y, viaProps.LeftStick.Y, 5);
        Assert.Equal(state.RightStick.X, viaProps.RightStick.X, 5);
        Assert.Equal(state.RightStick.Y, viaProps.RightStick.Y, 5);
        Assert.Equal(state.Axis(UiGamepadAxis.LeftTrigger), viaProps.Axis(UiGamepadAxis.LeftTrigger), 5);
        Assert.Equal(state.Axis(UiGamepadAxis.RightTrigger), viaProps.Axis(UiGamepadAxis.RightTrigger), 5);

        // Five axes give an ArgumentException with the parameter name axes.
        float[] shortAxes = new float[AxisCount - 1];
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => GlfwUiInput.GamepadState(ReadOnlySpan<byte>.Empty, shortAxes, 0.15f, 0.1f));
        Assert.Equal("axes", error.ParamName);
    }

    [Fact]
    public void TheDeadzoneSettingsCheckTheRange()
    {
        GlfwUiInput input = CreateSeamAdapter();

        Assert.Equal(0.15f, input.StickDeadzone, 5);
        Assert.Equal(0.1f, input.TriggerDeadzone, 5);

        float[] badValues = [-0.01f, 1.01f, float.NaN];
        foreach (float value in badValues)
        {
            ArgumentOutOfRangeException stick = Assert.Throws<ArgumentOutOfRangeException>(
                () => input.StickDeadzone = value);
            Assert.Equal("value", stick.ParamName);
            Assert.Equal(0.15f, input.StickDeadzone, 5);

            ArgumentOutOfRangeException trigger = Assert.Throws<ArgumentOutOfRangeException>(
                () => input.TriggerDeadzone = value);
            Assert.Equal("value", trigger.ParamName);
            Assert.Equal(0.1f, input.TriggerDeadzone, 5);
        }

        input.StickDeadzone = 0f;
        Assert.Equal(0f, input.StickDeadzone, 5);
        input.StickDeadzone = 1f;
        Assert.Equal(1f, input.StickDeadzone, 5);
        input.TriggerDeadzone = 0f;
        Assert.Equal(0f, input.TriggerDeadzone, 5);
        input.TriggerDeadzone = 1f;
        Assert.Equal(1f, input.TriggerDeadzone, 5);

        // The setter also works after Dispose, because it only sets a field.
        input.Dispose();
        input.StickDeadzone = 0.2f;
        Assert.Equal(0.2f, input.StickDeadzone, 5);
        input.TriggerDeadzone = 0.3f;
        Assert.Equal(0.3f, input.TriggerDeadzone, 5);
    }

    [Fact]
    public void ADisposedAdapterWritesDisconnectedSlots()
    {
        GlfwUiInput input = CreateSeamAdapter();
        input.Context.SetGamepad(3, new UiGamepadState(UiGamepadButtons.A, [0.5f]));
        input.Context.SetGamepad(15, new UiGamepadState(UiGamepadButtons.A, [0.5f]));

        input.Dispose();

        // The disposed path makes no GLFW call, so this test needs no GLFW start.
        input.PollGamepads();

        for (int id = 0; id < GamepadCount; id++)
        {
            ref readonly UiGamepadState slot = ref input.Context.Input.Gamepad(id);
            Assert.False(slot.Connected);
            Assert.Equal(UiGamepadButtons.None, slot.Buttons);
            for (int axis = 0; axis < AxisCount; axis++)
            {
                Assert.Equal(0f, slot.Axis(axis), 5);
            }
        }

        // A held button gets its released edge in the first poll after Dispose.
        Assert.Equal(UiGamepadButtons.A, input.Context.Input.Gamepad(3).Released);

        // The next poll starts from no button, so it gives no edge.
        input.PollGamepads();
        Assert.Equal(UiGamepadButtons.None, input.Context.Input.Gamepad(3).Released);
    }

    [Fact]
    public void ADisposedPollAllocatesNothing()
    {
        GlfwUiInput input = CreateSeamAdapter();

        // Seed slot 0 with a held button before the dispose. Each poll after Dispose must write the default
        // state into it, so the measured polls cannot pass empty.
        input.Context.SetGamepad(0, new UiGamepadState(UiGamepadButtons.A, [0.5f]));
        input.Dispose();

        PollGamepads(input, WarmUpPolls);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        PollGamepads(input, PollCount);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // The asserts follow the measure, so they cannot change the measured bytes. A no-op poll leaves the
        // seed connected with its button. The released edge itself is transient: each later SetGamepad write
        // clears it, as ADisposedAdapterWritesDisconnectedSlots shows.
        Assert.False(input.Context.Input.Gamepad(0).Connected);
        Assert.Equal(UiGamepadButtons.None, input.Context.Input.Gamepad(0).Buttons);
        Assert.Equal(0f, input.Context.Input.Gamepad(0).Axis(0), 5);
    }

    [Fact]
    public void TheConversionAllocatesNothing()
    {
        GlfwUiInput input = CreateSeamAdapter();
        byte[] buttons = new byte[ButtonCount];
        buttons[GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_A] = (byte)GlfwButtonAction.Press;
        float[] axes = [1f, 0f, 1f, 0f, 1f, -1f];

        ConversionBody(input, buttons, axes, WarmUpPolls);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = ConversionBody(input, buttons, axes, PollCount);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // Each round adds three ones: the left stick X, the right stick X, and the left trigger.
        Assert.Equal(3 * PollCount, sum);
    }

    [Fact]
    public void TheNullPlatformPollWritesSixteenAbsentSlots()
    {
        if (!GlfwApi.IsPlatformSupported(GlfwPlatform.Null))
        {
            Assert.Skip("The GLFW build does not support the null platform.");
        }

        GlfwUiInput input = CreateSeamAdapter();
        try
        {
            GlfwApi.SetInitHint(
                (GlfwInitHint)GlfwApi.Constants.GLFW_PLATFORM, GlfwApi.Constants.GLFW_PLATFORM_NULL);

            if (!GlfwApi.Init())
            {
                GlfwErrorCode code = GlfwApi.GetError(out string? description);
                Assert.Skip($"GLFW null-platform init failed: {code} ({description}).");
            }

            // A present button first, so the poll must clear it through the absent path.
            input.Context.SetGamepad(0, new UiGamepadState(UiGamepadButtons.A, [0.5f]));

            PollGamepads(input, WarmUpPolls);

            AllocationGate.ReleaseAllocationContext();
            long before = GC.GetAllocatedBytesForCurrentThread();
            PollGamepads(input, PollCount);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0L, after - before);

            // The asserts follow the measure, so they cannot change the measured bytes. The null platform has no
            // joystick, so each slot is absent.
            for (int id = 0; id < GamepadCount; id++)
            {
                ref readonly UiGamepadState slot = ref input.Context.Input.Gamepad(id);
                Assert.False(slot.Connected);
                Assert.Equal(UiGamepadButtons.None, slot.Buttons);
                for (int axis = 0; axis < AxisCount; axis++)
                {
                    Assert.Equal(0f, slot.Axis(axis), 5);
                }

                Assert.Equal(UiGamepadButtons.None, input.PollGamepad(id));
            }
        }
        finally
        {
            input.Dispose();
            GlfwApi.Terminate();

            // Put the hint back, so the later GLFW tests of the collection start on the normal platform.
            GlfwApi.SetInitHint(
                (GlfwInitHint)GlfwApi.Constants.GLFW_PLATFORM, GlfwApi.Constants.GLFW_ANY_PLATFORM);
        }
    }
}
