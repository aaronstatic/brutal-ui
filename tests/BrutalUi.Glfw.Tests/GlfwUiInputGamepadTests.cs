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
/// The checks of <see cref="GlfwUiInput.PollGamepad(int)"/> and of the conversion <c>GamepadButtons</c>. Most tests use
/// the test seam with no window and no GLFW call. The last test starts GLFW and skips when no display exists. It
/// measures both polls: <see cref="GlfwUiInput.PollGamepads()"/> and <see cref="GlfwUiInput.PollGamepad(int)"/>.
/// </summary>
/// <remarks>
/// The class is in the collection of <see cref="GlfwUiInputAllocationTests"/>, because the live poll starts and stops
/// GLFW, and GLFW state is global.
/// </remarks>
[Collection(GlfwUiInputAllocationTests.CollectionName)]
public sealed class GlfwUiInputGamepadTests
{
    private const int ButtonCount = 15;
    private const int WarmUpPolls = 2;
    private const int PollCount = 100;

    private static GlfwUiInput CreateSeamAdapter()
    {
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() });
        return new GlfwUiInput(context);
    }

    /// <summary>
    /// The measured body. It calls <see cref="GlfwUiInput.PollGamepads()"/> and then
    /// <see cref="GlfwUiInput.PollGamepad(int)"/> in each iteration. It uses no lambda, no boxing, no string, and no
    /// Assert.
    /// </summary>
    private static UiGamepadButtons Poll(GlfwUiInput input, int count)
    {
        UiGamepadButtons last = UiGamepadButtons.None;
        for (int i = 0; i < count; i++)
        {
            input.PollGamepads();
            last = input.PollGamepad();
        }

        return last;
    }

    [Fact]
    public void EachPressedButtonGivesItsBit()
    {
        byte[] values = new byte[ButtonCount];
        for (int i = 0; i < ButtonCount; i++)
        {
            Array.Clear(values);
            values[i] = (byte)GlfwButtonAction.Press;

            UiGamepadButtons result = GlfwUiInput.GamepadButtons(values);

            Assert.Equal((UiGamepadButtons)(1 << i), result);
        }

        Array.Fill(values, (byte)GlfwButtonAction.Press);
        Assert.Equal((UiGamepadButtons)((1 << ButtonCount) - 1), GlfwUiInput.GamepadButtons(values));
    }

    [Fact]
    public void AReleasedButtonGivesNoBit()
    {
        byte[] values = new byte[ButtonCount];
        Array.Fill(values, (byte)GlfwButtonAction.Release);

        Assert.Equal(UiGamepadButtons.None, GlfwUiInput.GamepadButtons(values));

        // One pressed button and the others released: only the pressed button gives a bit.
        values[GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_START] = (byte)GlfwButtonAction.Press;
        Assert.Equal(UiGamepadButtons.Start, GlfwUiInput.GamepadButtons(values));

        // A short span reads only the values that it holds.
        Assert.Equal(UiGamepadButtons.None, GlfwUiInput.GamepadButtons(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void TheBitsFollowTheGlfwButtonIndices()
    {
        Assert.Equal(UiGamepadButtons.None, (UiGamepadButtons)0);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_A), UiGamepadButtons.A);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_B), UiGamepadButtons.B);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_X), UiGamepadButtons.X);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_Y), UiGamepadButtons.Y);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_LEFT_BUMPER), UiGamepadButtons.LeftBumper);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_RIGHT_BUMPER), UiGamepadButtons.RightBumper);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_BACK), UiGamepadButtons.Back);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_START), UiGamepadButtons.Start);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_GUIDE), UiGamepadButtons.Guide);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_LEFT_THUMB), UiGamepadButtons.LeftThumb);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_RIGHT_THUMB), UiGamepadButtons.RightThumb);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_DPAD_UP), UiGamepadButtons.DpadUp);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_DPAD_RIGHT), UiGamepadButtons.DpadRight);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_DPAD_DOWN), UiGamepadButtons.DpadDown);
        Assert.Equal((UiGamepadButtons)(1 << GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_DPAD_LEFT), UiGamepadButtons.DpadLeft);

        // The enum holds the 15 buttons and None. The last button is the last GLFW index.
        Assert.Equal(ButtonCount, GlfwApi.Constants.GLFW_GAMEPAD_BUTTON_LAST + 1);
        Assert.Equal(ButtonCount + 1, Enum.GetValues<UiGamepadButtons>().Length);

        // PollGamepad reads the fixed buffer with a fixed length. A shorter buffer in a new package must fail here.
        FieldInfo buttons = typeof(GlfwGamepadState).GetField(nameof(GlfwGamepadState.Buttons))!;
        FixedBufferAttribute buffer = Assert.IsType<FixedBufferAttribute>(
            Assert.Single(buttons.GetCustomAttributes(typeof(FixedBufferAttribute), false)));
        Assert.Equal(ButtonCount, buffer.Length);
        Assert.Equal(typeof(byte), buffer.ElementType);
    }

    [Fact]
    public void PollGamepadRejectsAnIdOutsideTheRange()
    {
        GlfwUiInput input = CreateSeamAdapter();

        ArgumentOutOfRangeException below = Assert.Throws<ArgumentOutOfRangeException>(() => input.PollGamepad(-1));
        ArgumentOutOfRangeException above = Assert.Throws<ArgumentOutOfRangeException>(() => input.PollGamepad(16));

        Assert.Equal("gamepadId", below.ParamName);
        Assert.Equal("gamepadId", above.ParamName);
    }

    [Fact]
    public void ADisposedAdapterPollsNoButton()
    {
        GlfwUiInput input = CreateSeamAdapter();
        input.Dispose();

        // The adapter returns before any GLFW call, so this test needs no GLFW start.
        Assert.Equal(UiGamepadButtons.None, input.PollGamepad());
        Assert.Equal(UiGamepadButtons.None, input.PollGamepad(15));
    }

    [Fact]
    public void ALivePollAllocatesNothing()
    {
        GlfwUiInput input = CreateSeamAdapter();
        try
        {
            if (!GlfwApi.Init())
            {
                GlfwErrorCode code = GlfwApi.GetError(out string? description);
                Assert.Skip($"GLFW init failed: {code} ({description}). The machine has no display.");
            }

            // Do not assert a button: the machine can have a gamepad.
            Poll(input, WarmUpPolls);

            AllocationGate.ReleaseAllocationContext();
            long before = GC.GetAllocatedBytesForCurrentThread();
            Poll(input, PollCount);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0L, after - before);

            // The false path of GetGamepadState: an absent joystick gives no button. The assert follows the measure,
            // so it cannot change the measured bytes.
            int absentId = -1;
            for (int id = 0; id <= 15; id++)
            {
                if (!GlfwApi.JoystickPresent(id))
                {
                    absentId = id;
                    break;
                }
            }

            // When all 16 ids are present (not likely), no absent id exists and the assert is skipped.
            if (absentId >= 0)
            {
                Assert.Equal(UiGamepadButtons.None, input.PollGamepad(absentId));

                // The full poll writes the absent slot too: not connected and no button in the raw state.
                input.PollGamepads();
                Assert.False(input.Context.Input.Gamepad(absentId).Connected);
                Assert.Equal(UiGamepadButtons.None, input.Context.Input.Gamepad(absentId).Buttons);
            }
        }
        finally
        {
            input.Dispose();
            GlfwApi.Terminate();
        }
    }
}
