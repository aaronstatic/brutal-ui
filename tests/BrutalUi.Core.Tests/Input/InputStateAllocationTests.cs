using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The nineteenth allocation gate: the input pass with the state, <see cref="UiContext.ClearHeldKeys"/>,
/// <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/>, and each read of
/// <see cref="UiContext.Input"/> and <see cref="UiContext.GameInput"/> must allocate 0 bytes after warm-up.
/// </summary>
/// <remarks>
/// <para>
/// The scene is <see cref="ThousandElementTree"/> after one update. The test builds every event before the measure.
/// The slot point is the top-left corner of grid slot 0; it hits the slot. The outside point is right of the
/// viewport; it hits nothing.
/// </para>
/// <para>
/// Each step pushes nine events: a move to the slot point, a press there, a move outside the root, a release there,
/// a scroll outside the root, a W down, a W repeat, a W up, and a Left Shift down. Then it calls
/// <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> on slot 0 with one of two states, calls
/// <see cref="UiContext.ClearHeldKeys"/> on every second step, and calls <see cref="UiContext.Update(float)"/>. The
/// two gamepad states alternate, so each write gives a real edge. The step ends with reads of both views into a
/// local <see cref="int"/> sum: the delta and the scroll as ints, each button flag, the W flags, and the buttons of
/// slot 0. The drain keeps the state hot, and the writes keep the gamepad edges and the clear request alive in the
/// measured body.
/// </para>
/// <para>
/// The second test sweeps each query: the three key flags of the 349 codes, the three button flags of the eight
/// buttons, and the queries of the 16 slots with their six axes, on both views. The reads are a full sweep, so no
/// read path hides behind a narrow one.
/// </para>
/// </remarks>
public sealed class InputStateAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int W = 87;
    private const int LeftShift = 340;

    /// <summary>The tree, the events, the gamepad states, and the accumulated reads. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required ThousandElementTree Tree { get; init; }

        public required UiContext Context { get; init; }

        public required float2 SlotPoint;
        public required float2 OutsidePoint;

        public required PointerEvent MoveSlot;
        public required PointerEvent DownSlot;
        public required PointerEvent MoveOutside;
        public required PointerEvent UpOutside;
        public required ScrollEvent ScrollOutside;
        public required KeyEvent WDown;
        public required KeyEvent WRepeat;
        public required KeyEvent WUp;
        public required KeyEvent ShiftDown;

        public required UiGamepadState GamepadA;
        public required UiGamepadState GamepadB;

        /// <summary>The sum of the reads of the last round. It keeps the compiler from removing the reads.</summary>
        public int StateSum;
    }

    private static Scene BuildScene()
    {
        ThousandElementTree tree = ThousandElementTree.Build();
        UiContext context = tree.Context;
        context.Update(0f);

        float4 slotRect = PointerTree.ViewportRect(tree.GridSlot);
        float2 slotPoint = new(slotRect.X + 1f, slotRect.Y + 1f);
        float2 outsidePoint = new(ThousandElementTree.ViewportWideWidth + 10f, ThousandElementTree.ViewportHeight * 0.5f);

        return new Scene
        {
            Tree = tree,
            Context = context,
            SlotPoint = slotPoint,
            OutsidePoint = outsidePoint,
            MoveSlot = PointerTree.Move(slotPoint),
            DownSlot = PointerTree.Down(slotPoint, PointerButton.Left),
            MoveOutside = PointerTree.Move(outsidePoint),
            UpOutside = PointerTree.Up(outsidePoint, PointerButton.Left),
            ScrollOutside = PointerTree.Scroll(outsidePoint, new float2(0f, -1f)),
            WDown = PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.None),
            WRepeat = PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.None, repeat: true),
            WUp = PointerTree.Key(UiEventType.KeyUp, W, UiModifiers.None),
            ShiftDown = PointerTree.Key(UiEventType.KeyDown, LeftShift, UiModifiers.Shift),
            GamepadA = new UiGamepadState(UiGamepadButtons.A | UiGamepadButtons.B, [0.25f, -0.5f, 0f, 0f, 0f, 0f]),
            GamepadB = new UiGamepadState(UiGamepadButtons.B | UiGamepadButtons.X, [-0.75f, 0.5f, 0f, 0f, 0f, 0f]),
        };
    }

    /// <summary>Returns 1 when the flag is set, else 0. The measured bodies use it instead of an <c>if</c> block.</summary>
    private static int AsFlag(bool value) => value ? 1 : 0;

    /// <summary>The measured body of the first test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void Step(Scene scene, int index)
    {
        UiContext context = scene.Context;

        context.PushEvent(in scene.MoveSlot);
        context.PushEvent(in scene.DownSlot);
        context.PushEvent(in scene.MoveOutside);
        context.PushEvent(in scene.UpOutside);
        context.PushEvent(in scene.ScrollOutside);
        context.PushEvent(in scene.WDown);
        context.PushEvent(in scene.WRepeat);
        context.PushEvent(in scene.WUp);
        context.PushEvent(in scene.ShiftDown);

        if ((index & 1) == 0)
        {
            context.ClearHeldKeys();
            context.SetGamepad(0, in scene.GamepadA);
        }
        else
        {
            context.SetGamepad(0, in scene.GamepadB);
        }

        context.Update(0.016f);

        ref readonly UiInputState raw = ref context.Input;
        ref readonly UiInputState game = ref context.GameInput;
        int sum = 0;
        sum += (int)raw.Pointer.Delta.X + (int)raw.Pointer.Delta.Y;
        sum += (int)raw.Pointer.ScrollDelta.X + (int)raw.Pointer.ScrollDelta.Y;
        sum += (int)game.Pointer.Delta.X + (int)game.Pointer.Delta.Y;
        sum += (int)game.Pointer.ScrollDelta.X + (int)game.Pointer.ScrollDelta.Y;
        for (int buttonIndex = 0; buttonIndex < UiPointerButtonState.ButtonCount; buttonIndex++)
        {
            PointerButton button = (PointerButton)buttonIndex;
            sum += AsFlag(raw.Buttons.IsDown(button));
            sum += AsFlag(raw.Buttons.WasPressed(button));
            sum += AsFlag(raw.Buttons.WasReleased(button));
            sum += AsFlag(game.Buttons.IsDown(button));
            sum += AsFlag(game.Buttons.WasPressed(button));
            sum += AsFlag(game.Buttons.WasReleased(button));
        }

        sum += AsFlag(raw.Keys.IsDown(W));
        sum += AsFlag(raw.Keys.WasPressed(W));
        sum += AsFlag(raw.Keys.WasReleased(W));
        sum += AsFlag(game.Keys.IsDown(W));
        sum += AsFlag(game.Keys.WasPressed(W));
        sum += AsFlag(game.Keys.WasReleased(W));
        sum += (int)raw.Gamepad(0).Buttons;
        sum += (int)game.Gamepad(0).Buttons;
        scene.StateSum = sum;
    }

    private static void RunSteps(Scene scene, int steps)
    {
        for (int index = 0; index < steps; index++)
        {
            Step(scene, index);
        }
    }

    /// <summary>The measured body of the second test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void ReadRound(Scene scene)
    {
        UiContext context = scene.Context;
        ref readonly UiInputState raw = ref context.Input;
        ref readonly UiInputState game = ref context.GameInput;
        int sum = 0;
        sum = ReadView(in raw, sum);
        sum = ReadView(in game, sum);
        scene.StateSum = sum;
    }

    /// <summary>Reads each query of one view into the sum. It returns the new sum.</summary>
    private static int ReadView(in UiInputState state, int sum)
    {
        for (int key = 0; key < UiKeyState.KeyCount; key++)
        {
            sum += AsFlag(state.Keys.IsDown(key));
            sum += AsFlag(state.Keys.WasPressed(key));
            sum += AsFlag(state.Keys.WasReleased(key));
        }

        sum += (int)state.Keys.Modifiers;
        for (int buttonIndex = 0; buttonIndex < UiPointerButtonState.ButtonCount; buttonIndex++)
        {
            PointerButton button = (PointerButton)buttonIndex;
            sum += AsFlag(state.Buttons.IsDown(button));
            sum += AsFlag(state.Buttons.WasPressed(button));
            sum += AsFlag(state.Buttons.WasReleased(button));
        }

        sum += (int)state.Pointer.Delta.X + (int)state.Pointer.Delta.Y;
        sum += (int)state.Pointer.ScrollDelta.X + (int)state.Pointer.ScrollDelta.Y;
        for (int slot = 0; slot < UiInputState.GamepadCount; slot++)
        {
            ref readonly UiGamepadState gamepad = ref state.Gamepad(slot);
            sum += AsFlag(gamepad.Connected);
            sum += (int)gamepad.Buttons + (int)gamepad.Pressed + (int)gamepad.Released;
            for (int axis = 0; axis < UiGamepadState.AxisCount; axis++)
            {
                sum += (int)(gamepad.Axis(axis) * 100f);
            }
        }

        return sum;
    }

    private static void RunReads(Scene scene, int rounds)
    {
        for (int round = 0; round < rounds; round++)
        {
            ReadRound(scene);
        }
    }

    [Fact]
    public void TheInputPassWithTheStateAllocatesNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        for (int i = 0; i < WarmUpRounds; i++)
        {
            RunSteps(scene, 1);
        }

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void TheReadsOfTheStateAllocateNothing()
    {
        Scene scene = BuildScene();

        for (int i = 0; i < WarmUpRounds; i++)
        {
            RunReads(scene, 1);
        }

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunReads(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        UiContext context = scene.Context;

        Step(scene, 0);
        ref readonly UiInputState raw = ref context.Input;
        ref readonly UiInputState game = ref context.GameInput;

        // The scroll outside the root is the last entry: both views hold the outside point and the same gate-open parts.
        Assert.Equal(scene.OutsidePoint.X, raw.Pointer.Position.X);
        Assert.Equal(scene.OutsidePoint.Y, raw.Pointer.Position.Y);
        Assert.Equal(0f, raw.Pointer.Delta.X);
        Assert.Equal(0f, raw.Pointer.Delta.Y);
        Assert.Equal(-1f, raw.Pointer.ScrollDelta.Y);
        Assert.Equal(raw.Pointer.Position.X, game.Pointer.Position.X);
        Assert.Equal(raw.Pointer.ScrollDelta.Y, game.Pointer.ScrollDelta.Y);

        // The press went to the slot: the raw view holds the pressed and released flags, the game view hides both (A3).
        Assert.True(raw.Buttons.WasPressed(PointerButton.Left));
        Assert.True(raw.Buttons.WasReleased(PointerButton.Left));
        Assert.False(raw.Buttons.IsDown(PointerButton.Left));
        Assert.False(game.Buttons.WasPressed(PointerButton.Left));
        Assert.False(game.Buttons.WasReleased(PointerButton.Left));

        // The clear request ran at the end of the drain: W holds a pressed and a released flag and no down flag, and
        // the Left Shift of the same frame holds no down flag. The modifiers are None. No focus owner gates the key part.
        Assert.False(raw.Keys.IsDown(W));
        Assert.True(raw.Keys.WasPressed(W));
        Assert.True(raw.Keys.WasReleased(W));
        Assert.False(raw.Keys.IsDown(LeftShift));
        Assert.True(raw.Keys.WasPressed(LeftShift));
        Assert.True(raw.Keys.WasReleased(LeftShift));
        Assert.Equal(UiModifiers.None, raw.Keys.Modifiers);
        Assert.True(game.Keys.WasPressed(W));
        Assert.True(game.Keys.WasReleased(W));

        // Slot 0 holds the first state, and its pressed edge is the whole button set. The gate does not touch a slot.
        Assert.True(raw.Gamepad(0).Connected);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.B, raw.Gamepad(0).Buttons);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.B, raw.Gamepad(0).Pressed);
        Assert.Equal(UiGamepadButtons.None, raw.Gamepad(0).Released);
        Assert.Equal(0.25f, raw.Gamepad(0).Axis(0));
        Assert.Equal(-0.5f, raw.Gamepad(0).Axis(1));
        Assert.Equal(raw.Gamepad(0).Pressed, game.Gamepad(0).Pressed);

        // The fixed sum of one step: -2 scroll ints, +2 raw button flags, +4 W flags, +6 buttons of slot 0.
        Assert.Equal(10, scene.StateSum);

        Step(scene, 1);

        // The second state replaces slot 0: the shared button B keeps no edge, A gets released, and X gets pressed.
        Assert.Equal(UiGamepadButtons.B | UiGamepadButtons.X, raw.Gamepad(0).Buttons);
        Assert.Equal(UiGamepadButtons.X, raw.Gamepad(0).Pressed);
        Assert.Equal(UiGamepadButtons.A, raw.Gamepad(0).Released);
        Assert.Equal(-0.75f, raw.Gamepad(0).Axis(0));

        // The second step has no clear request: the Left Shift stays down, and the modifiers hold Shift.
        Assert.True(raw.Keys.IsDown(LeftShift));
        Assert.Equal(UiModifiers.Shift, raw.Keys.Modifiers);
        Assert.Equal(16, scene.StateSum);
    }
}
