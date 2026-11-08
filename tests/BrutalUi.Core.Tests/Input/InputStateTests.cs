using System;
using System.Runtime.CompilerServices;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the pointer, button, key, and gamepad parts of <see cref="UiContext.Input"/> and
/// <see cref="UiContext.GameInput"/>: the position, the delta, the scroll, the button flags, the press
/// ownership, the key flags, the modifiers, the key clear, the gamepad slots, and the gamepad edges. Each test
/// runs the real <see cref="UiContext.Update(float)"/> on a <see cref="PointerTree"/>, so it also covers the
/// three hooks of <see cref="UiContext.DrainEvents"/>. A test reads the state after <see cref="Update(float)"/>
/// only.
/// </summary>
/// <remarks>
/// <para>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name
/// prefix. The tree root covers (0, 0, 400, 300) in a viewport of 800 by 600, so (50, 50) is a point over the UI,
/// on <see cref="PointerTree.Left"/>, and (600, 400) is a point over the game.
/// </para>
/// <para>A test never calls the drain directly, because a second drain starts a new frame.</para>
/// </remarks>
public sealed class InputStateTests
{
    /// <summary>
    /// A new context holds no pointer: both components of <see cref="UiPointerState.Position"/> are NaN, and
    /// <see cref="UiPointerState.Delta"/> is (0, 0). An <see cref="UiContext.Update(float)"/> with an empty ring
    /// keeps both.
    /// </summary>
    [Fact]
    public void TheStateStartsWithNoPointerAndAZeroDelta()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheStateStartsWithNoPointerAndAZeroDelta));

        AssertPositionIsNaN(tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.Input.Pointer.Delta);

        tree.Context.Update(0f);

        AssertPositionIsNaN(tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.Input.Pointer.Delta);
        AssertPositionIsNaN(tree.Context.GameInput.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.GameInput.Pointer.Delta);
    }

    /// <summary>
    /// <see cref="UiPointerState.Position"/> holds the position of the last pointer or scroll entry of the frame:
    /// moves to (500, 350) and (600, 400) give (600, 400), and a scroll at (610, 410) after them gives (610, 410).
    /// </summary>
    [Fact]
    public void ThePositionIsTheLastPointerEntryOfTheFrame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(ThePositionIsTheLastPointerEntryOfTheFrame));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(500f, 350f)));
        tree.Context.PushEvent(PointerTree.Move(new float2(600f, 400f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(600f, 400f), tree.Context.Input.Pointer.Position);

        tree.Context.PushEvent(PointerTree.Scroll(new float2(610f, 410f), new float2(0f, 1f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(610f, 410f), tree.Context.Input.Pointer.Position);
    }

    /// <summary>
    /// <see cref="UiPointerState.Delta"/> is the position of this <see cref="UiContext.Update(float)"/> call minus
    /// the position of the last one. A frame with no entry gives (0, 0) and keeps the position.
    /// </summary>
    [Fact]
    public void TheDeltaIsThePositionOfThisUpdateMinusThePositionOfTheLastUpdate()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheDeltaIsThePositionOfThisUpdateMinusThePositionOfTheLastUpdate));
        tree.Settle();

        // Frame 1: the first position arrives. The start is (NaN, NaN), so the delta is (0, 0).
        tree.Context.PushEvent(PointerTree.Move(new float2(500f, 350f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(500f, 350f), tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.Input.Pointer.Delta);

        // Frame 2: two moves. The delta runs from the position of frame 1 to the last move.
        tree.Context.PushEvent(PointerTree.Move(new float2(520f, 340f)));
        tree.Context.PushEvent(PointerTree.Move(new float2(530f, 360f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(530f, 360f), tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(30f, 10f), tree.Context.Input.Pointer.Delta);

        // Frame 3: no entry. The delta is (0, 0) and the position stays.
        tree.Context.Update(0f);
        Assert.Equal(new float2(530f, 360f), tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.Input.Pointer.Delta);
    }

    /// <summary>
    /// A NaN position gives a zero delta (A5): a move to (500, 350) and a move to (NaN, NaN) in one frame give
    /// (0, 0) and a NaN position, and a move to (510, 350) in the next frame gives (0, 0) again.
    /// </summary>
    [Fact]
    public void APointerAtNaNGivesAZeroDelta()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(APointerAtNaNGivesAZeroDelta));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(500f, 350f)));
        tree.Context.PushEvent(PointerTree.Move(new float2(float.NaN, float.NaN)));
        tree.Context.Update(0f);
        AssertPositionIsNaN(tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.Input.Pointer.Delta);

        tree.Context.PushEvent(PointerTree.Move(new float2(510f, 350f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(510f, 350f), tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.Input.Pointer.Delta);
    }

    /// <summary>
    /// The pointer part of <see cref="UiContext.GameInput"/> is empty when the pointer is over the UI: the raw
    /// delta is (-550, -350), and the game view holds the position (NaN, NaN) and the delta (0, 0).
    /// </summary>
    [Fact]
    public void TheGameViewIsEmptyWhenThePointerIsOverTheUi()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheGameViewIsEmptyWhenThePointerIsOverTheUi));
        tree.Settle();

        // Frame 1 over the game: both views agree.
        tree.Context.PushEvent(PointerTree.Move(new float2(600f, 400f)));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);

        // Frame 2 moves to (50, 50), over Left.
        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        tree.Context.Update(0f);

        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(new float2(-550f, -350f), tree.Context.Input.Pointer.Delta);
        Assert.Equal(new float2(0f, 0f), tree.Context.GameInput.Pointer.Delta);
        AssertPositionIsNaN(tree.Context.GameInput.Pointer.Position);
    }

    /// <summary>
    /// Over the game, <see cref="UiContext.GameInput"/> equals <see cref="UiContext.Input"/>: the position and the
    /// delta of both views are equal in two frames.
    /// </summary>
    [Fact]
    public void TheGameViewEqualsTheRawPointerOverTheGame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheGameViewEqualsTheRawPointerOverTheGame));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(600f, 400f)));
        tree.Context.Update(0f);
        Assert.Equal(tree.Context.Input.Pointer.Position, tree.Context.GameInput.Pointer.Position);
        Assert.Equal(tree.Context.Input.Pointer.Delta, tree.Context.GameInput.Pointer.Delta);

        tree.Context.PushEvent(PointerTree.Move(new float2(620f, 420f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(620f, 420f), tree.Context.Input.Pointer.Position);
        Assert.Equal(new float2(20f, 20f), tree.Context.Input.Pointer.Delta);
        Assert.Equal(tree.Context.Input.Pointer.Position, tree.Context.GameInput.Pointer.Position);
        Assert.Equal(tree.Context.Input.Pointer.Delta, tree.Context.GameInput.Pointer.Delta);
    }

    /// <summary>
    /// The gate of the game view comes from the end of the drain (A4): one frame with a move over the UI and then a
    /// move over the game gives an open game view, and the reverse order gives an empty game view.
    /// </summary>
    [Fact]
    public void TheGateOfTheGameViewComesFromTheEndOfTheDrain()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheGateOfTheGameViewComesFromTheEndOfTheDrain));
        tree.Settle();

        // The last entry is over the game, so the gate at the end of the drain is open.
        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        tree.Context.PushEvent(PointerTree.Move(new float2(600f, 400f)));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(new float2(600f, 400f), tree.Context.GameInput.Pointer.Position);
        Assert.Equal(tree.Context.Input.Pointer.Position, tree.Context.GameInput.Pointer.Position);

        // The last entry is over the UI, so the gate at the end of the drain is closed.
        tree.Context.PushEvent(PointerTree.Move(new float2(600f, 400f)));
        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(new float2(50f, 50f), tree.Context.Input.Pointer.Position);
        AssertPositionIsNaN(tree.Context.GameInput.Pointer.Position);
        Assert.Equal(new float2(0f, 0f), tree.Context.GameInput.Pointer.Delta);
    }

    /// <summary>
    /// An entry with a target from the caller moves the raw position: a move to (700, 500) with the target
    /// <see cref="PointerTree.Left"/> sets <see cref="UiContext.Input"/>, and
    /// <see cref="UiContext.PointerHit"/> and <see cref="UiContext.RoutingGate"/> keep their values.
    /// </summary>
    [Fact]
    public void AnEntryWithATargetFromTheCallerMovesTheRawPosition()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(AnEntryWithATargetFromTheCallerMovesTheRawPosition));
        tree.Settle();

        Assert.Null(tree.Context.PointerHit);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);

        tree.Context.PushEvent(TargetedMove(tree.Left, new float2(700f, 500f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(700f, 500f), tree.Context.Input.Pointer.Position);
        Assert.Null(tree.Context.PointerHit);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
    }

    /// <summary>
    /// A press sets the down flag and the pressed flag. Frame 1 of a left press at (600, 400) reads down and
    /// pressed. Frame 2 reads down and not pressed.
    /// </summary>
    [Fact]
    public void APressSetsDownAndPressedForOneFrame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(APressSetsDownAndPressedForOneFrame));
        tree.Settle();

        // Frame 1: the press.
        tree.Context.PushEvent(PointerTree.Down(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
        Assert.True(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.WasReleased(PointerButton.Left));

        // Frame 2: no entry. The button stays down, and the pressed flag clears.
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));
    }

    /// <summary>
    /// A release sets the released flag for one frame and clears the down flag. The up gives released and not down,
    /// and the next frame gives neither. An up of a button that is not down gives no released flag.
    /// </summary>
    [Fact]
    public void AReleaseSetsReleasedForOneFrameAndClearsDown()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(AReleaseSetsReleasedForOneFrameAndClearsDown));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);

        // The up gives released and not down.
        tree.Context.PushEvent(PointerTree.Up(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Buttons.WasReleased(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));

        // The next frame gives neither.
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Buttons.WasReleased(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.IsDown(PointerButton.Left));

        // An up of a button that is not down gives no released flag.
        tree.Context.PushEvent(PointerTree.Up(GamePoint, PointerButton.Right));
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Buttons.WasReleased(PointerButton.Right));
        Assert.False(tree.Context.Input.Buttons.IsDown(PointerButton.Right));
    }

    /// <summary>
    /// A press and a release of one button in one frame set both flags and clear the down flag: pressed, released,
    /// and not down.
    /// </summary>
    [Fact]
    public void APressAndAReleaseInOneFrameSetBothFlags()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(APressAndAReleaseInOneFrameSetBothFlags));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(GamePoint, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);

        Assert.True(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));
        Assert.True(tree.Context.Input.Buttons.WasReleased(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
    }

    /// <summary>
    /// Each button has its own flags. <see cref="PointerButton.Right"/>, <see cref="PointerButton.Middle"/>, and
    /// the button value 7 each set only their own flags. The values 8 and -1 change no mask and read
    /// <see langword="false"/> with no exception.
    /// </summary>
    [Fact]
    public void EachButtonHasItsOwnFlags()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(EachButtonHasItsOwnFlags));
        tree.Settle();
        PointerButton button7 = (PointerButton)7;

        tree.Context.PushEvent(PointerTree.Down(GamePoint, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Down(GamePoint, PointerButton.Middle));
        tree.Context.PushEvent(PointerTree.Down(GamePoint, button7));
        tree.Context.PushEvent(PointerTree.Down(GamePoint, (PointerButton)8));
        tree.Context.PushEvent(PointerTree.Up(GamePoint, (PointerButton)(-1)));
        tree.Context.Update(0f);

        // Each pressed button holds its own flags.
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Right));
        Assert.True(tree.Context.Input.Buttons.WasPressed(PointerButton.Right));
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Middle));
        Assert.True(tree.Context.Input.Buttons.WasPressed(PointerButton.Middle));
        Assert.True(tree.Context.Input.Buttons.IsDown(button7));
        Assert.True(tree.Context.Input.Buttons.WasPressed(button7));

        // The buttons with no entry hold no flag.
        Assert.False(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.IsDown((PointerButton)3));
        Assert.False(tree.Context.Input.Buttons.IsDown((PointerButton)6));
        Assert.False(tree.Context.Input.Buttons.WasReleased(PointerButton.Left));
        Assert.False(tree.Context.Input.Buttons.WasReleased(PointerButton.Right));

        // A value outside 0 to 7 reads false, changes no mask, and throws no exception.
        Assert.False(tree.Context.Input.Buttons.IsDown((PointerButton)8));
        Assert.False(tree.Context.Input.Buttons.WasPressed((PointerButton)8));
        Assert.False(tree.Context.Input.Buttons.WasReleased((PointerButton)8));
        Assert.False(tree.Context.Input.Buttons.IsDown((PointerButton)(-1)));
        Assert.False(tree.Context.Input.Buttons.WasPressed((PointerButton)(-1)));
        Assert.False(tree.Context.Input.Buttons.WasReleased((PointerButton)(-1)));

        // The next frame keeps each down mask and clears each pressed mask.
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Right));
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Middle));
        Assert.True(tree.Context.Input.Buttons.IsDown(button7));
        Assert.False(tree.Context.Input.Buttons.WasPressed(PointerButton.Right));
        Assert.False(tree.Context.Input.Buttons.WasPressed(button7));
    }

    /// <summary>
    /// <see cref="UiPointerState.ScrollDelta"/> is the sum of the scroll entries of the frame: scrolls of (0, 1)
    /// and (0, 2) at (600, 400) give (0, 3). The next frame gives (0, 0).
    /// </summary>
    [Fact]
    public void TheScrollDeltaIsTheSumOfTheFrame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheScrollDeltaIsTheSumOfTheFrame));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Scroll(GamePoint, new float2(0f, 1f)));
        tree.Context.PushEvent(PointerTree.Scroll(GamePoint, new float2(0f, 2f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 3f), tree.Context.Input.Pointer.ScrollDelta);

        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 0f), tree.Context.Input.Pointer.ScrollDelta);
    }

    /// <summary>
    /// The button and scroll parts of <see cref="UiContext.GameInput"/> are empty over the UI: a press and a scroll
    /// at (50, 50) stay in the raw view, and the game view holds neither.
    /// </summary>
    [Fact]
    public void TheButtonAndScrollPartsOfTheGameViewAreEmptyOverTheUi()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheButtonAndScrollPartsOfTheGameViewAreEmptyOverTheUi));
        tree.Settle();
        float2 uiPoint = new(50f, 50f);

        tree.Context.PushEvent(PointerTree.Down(uiPoint, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Scroll(uiPoint, new float2(0f, 2f)));
        tree.Context.Update(0f);

        Assert.True(tree.Context.RoutingGate.PointerOverUi);

        // The raw view holds both.
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
        Assert.True(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));
        Assert.Equal(new float2(0f, 2f), tree.Context.Input.Pointer.ScrollDelta);

        // The game view holds neither.
        Assert.False(tree.Context.GameInput.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.WasPressed(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.WasReleased(PointerButton.Left));
        Assert.Equal(new float2(0f, 0f), tree.Context.GameInput.Pointer.ScrollDelta);
    }

    /// <summary>
    /// A press that goes down over the UI and ends over the game gives no release in the game view (A3): frame 1 is
    /// a press at (50, 50), frame 2 a move to (600, 400), and frame 3 an up at (600, 400).
    /// </summary>
    [Fact]
    public void APressOverTheUiThatEndsOverTheGameGivesNoReleaseInTheGameView()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(APressOverTheUiThatEndsOverTheGameGivesNoReleaseInTheGameView));
        tree.Settle();
        float2 uiPoint = new(50f, 50f);

        // Frame 1: the press goes down over the UI.
        tree.Context.PushEvent(PointerTree.Down(uiPoint, PointerButton.Left));
        tree.Context.Update(0f);

        // Frame 2: the pointer moves over the game. The raw view is down, and the game view is not down.
        tree.Context.PushEvent(PointerTree.Move(GamePoint));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.IsDown(PointerButton.Left));

        // Frame 3: the up over the game. The gate is open, the raw view is released, and the game view is not.
        tree.Context.PushEvent(PointerTree.Up(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.True(tree.Context.Input.Buttons.WasReleased(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.WasReleased(PointerButton.Left));
    }

    /// <summary>
    /// A press that belongs to the game shows all three flags in the game view: a press, a held frame, and an up at
    /// (600, 400) give pressed, then down, then released.
    /// </summary>
    [Fact]
    public void APressOverTheGameBelongsToTheGame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(APressOverTheGameBelongsToTheGame));
        tree.Settle();

        // The press at (600, 400) belongs to the game.
        tree.Context.PushEvent(PointerTree.Down(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);
        Assert.True(tree.Context.GameInput.Buttons.WasPressed(PointerButton.Left));
        Assert.True(tree.Context.GameInput.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.WasReleased(PointerButton.Left));

        // The held frame keeps the down flag.
        tree.Context.Update(0f);
        Assert.True(tree.Context.GameInput.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.WasPressed(PointerButton.Left));

        // The up gives the released flag in the game view.
        tree.Context.PushEvent(PointerTree.Up(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);
        Assert.True(tree.Context.GameInput.Buttons.WasReleased(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.IsDown(PointerButton.Left));
    }

    /// <summary>
    /// A new press over the game takes the button back (Decision 8): frame 1 is a press at (50, 50), and frame 2 an
    /// up and a press at (600, 400). The game view is pressed and down, and not released.
    /// </summary>
    [Fact]
    public void ANewPressOverTheGameTakesTheButtonBack()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(ANewPressOverTheGameTakesTheButtonBack));
        tree.Settle();
        float2 uiPoint = new(50f, 50f);

        // Frame 1: the press goes down over the UI.
        tree.Context.PushEvent(PointerTree.Down(uiPoint, PointerButton.Left));
        tree.Context.Update(0f);

        // Frame 2: an up and a new press at (600, 400). The new press belongs to the game.
        tree.Context.PushEvent(PointerTree.Up(GamePoint, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Down(GamePoint, PointerButton.Left));
        tree.Context.Update(0f);

        Assert.True(tree.Context.GameInput.Buttons.WasPressed(PointerButton.Left));
        Assert.True(tree.Context.GameInput.Buttons.IsDown(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.WasReleased(PointerButton.Left));

        // The raw view holds all three flags.
        Assert.True(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));
        Assert.True(tree.Context.Input.Buttons.IsDown(PointerButton.Left));
        Assert.True(tree.Context.Input.Buttons.WasReleased(PointerButton.Left));
    }

    /// <summary>
    /// A press with a target from the caller belongs to the UI (Decision 5): a left press with the target
    /// <see cref="PointerTree.Left"/> at (600, 400) shows in the raw view and not in the game view, also with the
    /// gate open.
    /// </summary>
    [Fact]
    public void APressWithATargetFromTheCallerBelongsToTheUi()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(APressWithATargetFromTheCallerBelongsToTheUi));
        tree.Settle();

        tree.Context.PushEvent(TargetedDown(tree.Left, GamePoint));
        tree.Context.Update(0f);

        // A target from the caller changes no gate value.
        Assert.False(tree.Context.RoutingGate.PointerOverUi);

        Assert.True(tree.Context.Input.Buttons.WasPressed(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.WasPressed(PointerButton.Left));
        Assert.False(tree.Context.GameInput.Buttons.IsDown(PointerButton.Left));
    }

    /// <summary>
    /// A key down sets the down flag and the pressed flag for one frame: W (87) reads down and pressed in the press
    /// frame, and down and not pressed in the next frame.
    /// </summary>
    [Fact]
    public void AKeyDownSetsDownAndPressedForOneFrame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(AKeyDownSetsDownAndPressedForOneFrame));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.None));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.True(tree.Context.Input.Keys.WasPressed(W));
        Assert.False(tree.Context.Input.Keys.WasReleased(W));

        // The next frame keeps the down flag and clears the pressed flag.
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.False(tree.Context.Input.Keys.WasPressed(W));
    }

    /// <summary>
    /// A key up sets the released flag for one frame and clears the down flag. The next frame gives neither. An up
    /// of a key that is not down gives no released flag.
    /// </summary>
    [Fact]
    public void AKeyUpSetsReleasedAndClearsDown()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(AKeyUpSetsReleasedAndClearsDown));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.None));
        tree.Context.Update(0f);

        // The up gives released and not down.
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, W, UiModifiers.None));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.WasReleased(W));
        Assert.False(tree.Context.Input.Keys.IsDown(W));
        Assert.False(tree.Context.Input.Keys.WasPressed(W));

        // The next frame gives neither.
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.WasReleased(W));
        Assert.False(tree.Context.Input.Keys.IsDown(W));

        // An up of a key that is not down gives no released flag.
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, A, UiModifiers.None));
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.WasReleased(A));
        Assert.False(tree.Context.Input.Keys.IsDown(A));
    }

    /// <summary>
    /// A repeat sets no pressed flag: a down, then a repeat in the next frame, gives down and not pressed. A repeat
    /// of a key that is not down sets the down flag and no pressed flag (Decision 9).
    /// </summary>
    [Fact]
    public void ARepeatSetsNoPressedFlag()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(ARepeatSetsNoPressedFlag));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.None));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.True(tree.Context.Input.Keys.WasPressed(W));

        // The repeat in the next frame: down and not pressed.
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.None, repeat: true));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.False(tree.Context.Input.Keys.WasPressed(W));

        // A repeat of a key that is not down sets the down flag and no pressed flag.
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, A, UiModifiers.None, repeat: true));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(A));
        Assert.False(tree.Context.Input.Keys.WasPressed(A));
    }

    /// <summary>
    /// <see cref="UiKeyState.Modifiers"/> comes from the last key event of the frame and stays through a frame with
    /// no key event (A6).
    /// </summary>
    [Fact]
    public void TheModifiersComeFromTheLastKeyEventOfTheFrame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheModifiersComeFromTheLastKeyEventOfTheFrame));
        tree.Settle();

        const int leftShift = 340;
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, leftShift, UiModifiers.Shift));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.Shift | UiModifiers.Control));
        tree.Context.Update(0f);
        Assert.Equal(UiModifiers.Shift | UiModifiers.Control, tree.Context.Input.Keys.Modifiers);

        // The next frame holds no key event and keeps the value.
        tree.Context.Update(0f);
        Assert.Equal(UiModifiers.Shift | UiModifiers.Control, tree.Context.Input.Keys.Modifiers);
    }

    /// <summary>
    /// A code outside 0 to 348 changes no bit and reads <see langword="false"/> with no exception, and its event
    /// still sets the modifiers (Decision 7). The codes 348 and 32 work.
    /// </summary>
    [Fact]
    public void AKeyOutsideTheRangeChangesNoBit()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(AKeyOutsideTheRangeChangesNoBit));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, -1, UiModifiers.Shift));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, -1, UiModifiers.Shift));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, 349, UiModifiers.Control));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, 1000, UiModifiers.Alt));
        tree.Context.Update(0f);

        // Each outside code reads false on all three flags.
        Assert.False(tree.Context.Input.Keys.IsDown(-1));
        Assert.False(tree.Context.Input.Keys.WasPressed(-1));
        Assert.False(tree.Context.Input.Keys.WasReleased(-1));
        Assert.False(tree.Context.Input.Keys.IsDown(349));
        Assert.False(tree.Context.Input.Keys.WasPressed(349));
        Assert.False(tree.Context.Input.Keys.IsDown(1000));
        Assert.False(tree.Context.Input.Keys.WasPressed(1000));

        // The events of the outside codes set the modifiers: the last one is the Alt entry.
        Assert.Equal(UiModifiers.Alt, tree.Context.Input.Keys.Modifiers);

        // The codes 348 and 32 work.
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, 348, UiModifiers.Alt));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, 32, UiModifiers.Alt));
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(348));
        Assert.True(tree.Context.Input.Keys.WasPressed(348));
        Assert.True(tree.Context.Input.Keys.IsDown(32));
        Assert.True(tree.Context.Input.Keys.WasPressed(32));
    }

    /// <summary>
    /// The key part of <see cref="UiContext.GameInput"/> is empty when the UI holds the keyboard focus: the raw
    /// view is down, and the game view is not down and holds <see cref="UiModifiers.None"/> modifiers. After
    /// <see cref="UiContext.ClearFocus"/> and an <see cref="UiContext.Update(float)"/>, the game view is down.
    /// </summary>
    [Fact]
    public void TheKeyPartOfTheGameViewIsEmptyWhenTheUiHoldsTheFocus()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheKeyPartOfTheGameViewIsEmptyWhenTheUiHoldsTheFocus));
        tree.Settle();
        tree.Left.IsFocusable = true;
        Assert.True(tree.Context.SetFocus(tree.Left));

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.Shift));
        tree.Context.Update(0f);

        Assert.True(tree.Context.RoutingGate.KeyboardFocusInUi);

        // The raw view holds the key.
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.True(tree.Context.Input.Keys.WasPressed(W));
        Assert.Equal(UiModifiers.Shift, tree.Context.Input.Keys.Modifiers);

        // The game view is empty.
        Assert.False(tree.Context.GameInput.Keys.IsDown(W));
        Assert.False(tree.Context.GameInput.Keys.WasPressed(W));
        Assert.Equal(UiModifiers.None, tree.Context.GameInput.Keys.Modifiers);

        // After the focus leaves the UI, the game view shows the held key.
        tree.Context.ClearFocus();
        tree.Context.Update(0f);
        Assert.False(tree.Context.RoutingGate.KeyboardFocusInUi);
        Assert.True(tree.Context.GameInput.Keys.IsDown(W));
    }

    /// <summary>
    /// <see cref="UiContext.ClearHeldKeys"/> releases each held key at the end of the next drain: the raw view
    /// still holds the keys after the call, the next <see cref="UiContext.Update(float)"/> gives not down,
    /// released, and <see cref="UiModifiers.None"/> modifiers, and the <see cref="UiContext.Update(float)"/> after
    /// it gives no released flag (Decision 10).
    /// </summary>
    [Fact]
    public void ClearHeldKeysReleasesEachHeldKeyAtTheNextUpdate()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(ClearHeldKeysReleasesEachHeldKeyAtTheNextUpdate));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.Shift));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, A, UiModifiers.Shift));
        tree.Context.Update(0f);

        tree.Context.ClearHeldKeys();

        // The call sets a request only. The raw view still holds both keys.
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.True(tree.Context.Input.Keys.IsDown(A));

        // The next Update applies the clear.
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.IsDown(W));
        Assert.False(tree.Context.Input.Keys.IsDown(A));
        Assert.True(tree.Context.Input.Keys.WasReleased(W));
        Assert.True(tree.Context.Input.Keys.WasReleased(A));
        Assert.Equal(UiModifiers.None, tree.Context.Input.Keys.Modifiers);

        // The Update after it gives no released flag.
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.WasReleased(W));
        Assert.False(tree.Context.Input.Keys.WasReleased(A));
    }

    /// <summary>
    /// <see cref="UiContext.ClearHeldKeys"/> also clears a key down of the same frame: a pushed W down, the call,
    /// and one <see cref="UiContext.Update(float)"/> give pressed, released, and not down (Decision 10).
    /// </summary>
    [Fact]
    public void ClearHeldKeysClearsAKeyDownOfTheSameFrame()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(ClearHeldKeysClearsAKeyDownOfTheSameFrame));
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, W, UiModifiers.Shift));
        tree.Context.ClearHeldKeys();
        tree.Context.Update(0f);

        Assert.True(tree.Context.Input.Keys.WasPressed(W));
        Assert.True(tree.Context.Input.Keys.WasReleased(W));
        Assert.False(tree.Context.Input.Keys.IsDown(W));
        Assert.Equal(UiModifiers.None, tree.Context.Input.Keys.Modifiers);
    }

    /// <summary>
    /// The 16 slots of both views of a new context are disconnected: no button, no edge, and six axes of 0.
    /// <see cref="UiInputState.Gamepad(int)"/> with -1 and with 16 throws
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void EachGamepadSlotStartsDisconnected()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(EachGamepadSlotStartsDisconnected));

        for (int index = 0; index < UiInputState.GamepadCount; index++)
        {
            AssertDisconnectedGamepad(tree.Context.Input.Gamepad(index));
            AssertDisconnectedGamepad(tree.Context.GameInput.Gamepad(index));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Context.Input.Gamepad(-1).Connected);
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Context.Input.Gamepad(16).Connected);
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Context.GameInput.Gamepad(-1).Connected);
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Context.GameInput.Gamepad(16).Connected);
    }

    /// <summary>
    /// <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> writes one slot and its edges: the first call
    /// with A and B gives connected, A and B down, pressed A and B, and the six axes. The second call with only B
    /// gives pressed none and released A. Index 16 and index -1 throw
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void SetGamepadWritesTheSlotAndTheEdges()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(SetGamepadWritesTheSlotAndTheEdges));
        UiGamepadState first = new(UiGamepadButtons.A | UiGamepadButtons.B, AxisValues);
        UiGamepadState second = new(UiGamepadButtons.B, AxisValues);

        tree.Context.SetGamepad(3, first);
        ref readonly UiGamepadState slot = ref tree.Context.Input.Gamepad(3);
        Assert.True(slot.Connected);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.B, slot.Buttons);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.B, slot.Pressed);
        Assert.Equal(UiGamepadButtons.None, slot.Released);
        for (int axis = 0; axis < UiGamepadState.AxisCount; axis++)
        {
            Assert.Equal(first.Axis(axis), slot.Axis(axis));
        }

        // The second call replaces the edges: B stays down, so no button is pressed and A is released.
        tree.Context.SetGamepad(3, second);
        Assert.Equal(UiGamepadButtons.B, slot.Buttons);
        Assert.Equal(UiGamepadButtons.None, slot.Pressed);
        Assert.Equal(UiGamepadButtons.A, slot.Released);

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Context.SetGamepad(16, first));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Context.SetGamepad(-1, first));
    }

    /// <summary>
    /// The game view copies the slots with no gate: after <see cref="UiContext.Update(float)"/> with the pointer
    /// over the UI and the focus in the UI, <see cref="UiContext.GameInput"/> holds the slot that
    /// <see cref="UiContext.Input"/> holds, and the pointer part of the game view stays empty.
    /// </summary>
    [Fact]
    public void TheGameViewCopiesTheGamepadsWithNoGate()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheGameViewCopiesTheGamepadsWithNoGate));
        tree.Settle();
        tree.Left.IsFocusable = true;
        Assert.True(tree.Context.SetFocus(tree.Left));
        UiGamepadState state = new(UiGamepadButtons.A | UiGamepadButtons.X, AxisValues);

        // The pointer moves over the UI, and the UI holds the keyboard focus.
        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        tree.Context.SetGamepad(3, state);
        tree.Context.Update(0f);

        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.True(tree.Context.RoutingGate.KeyboardFocusInUi);
        Assert.True(float.IsNaN(tree.Context.GameInput.Pointer.Position.X));

        ref readonly UiGamepadState raw = ref tree.Context.Input.Gamepad(3);
        ref readonly UiGamepadState game = ref tree.Context.GameInput.Gamepad(3);
        Assert.True(raw.Connected);
        Assert.True(game.Connected);
        Assert.Equal(raw.Buttons, game.Buttons);
        Assert.Equal(raw.Pressed, game.Pressed);
        Assert.Equal(raw.Released, game.Released);
        for (int axis = 0; axis < UiGamepadState.AxisCount; axis++)
        {
            Assert.Equal(raw.Axis(axis), game.Axis(axis));
        }

        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.X, game.Buttons);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.X, game.Pressed);
    }

    /// <summary>
    /// The drain keeps the gamepad edges (Decision 11): <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/>
    /// and then <see cref="UiContext.Update(float)"/> keep the pressed flag of the new buttons, and the next
    /// <see cref="UiContext.Update(float)"/> keeps it too.
    /// </summary>
    [Fact]
    public void TheDrainKeepsTheGamepadEdges()
    {
        PointerTree tree = new(nameof(InputStateTests) + "." + nameof(TheDrainKeepsTheGamepadEdges));
        tree.Settle();
        UiGamepadState state = new(UiGamepadButtons.A | UiGamepadButtons.B, AxisValues);

        tree.Context.SetGamepad(3, state);

        // The first Update keeps the edges of the raw slot.
        tree.Context.Update(0f);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.B, tree.Context.Input.Gamepad(3).Pressed);
        Assert.Equal(UiGamepadButtons.None, tree.Context.Input.Gamepad(3).Released);

        // The second Update keeps them too: no drain step clears a gamepad edge.
        tree.Context.Update(0f);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.B, tree.Context.Input.Gamepad(3).Pressed);
        Assert.Equal(UiGamepadButtons.A | UiGamepadButtons.B, tree.Context.GameInput.Gamepad(3).Pressed);
    }

    /// <summary>
    /// The constructor copies at most six axes: eight values give the first six, and two values give two values
    /// and four zeros. <c>default(UiGamepadState)</c> is a disconnected slot. <see cref="UiGamepadState.Axis"/>
    /// throws <see cref="ArgumentOutOfRangeException"/> for an index outside 0 to 5.
    /// </summary>
    [Fact]
    public void TheConstructorCopiesUpToSixAxes()
    {
        UiGamepadState eight = new(UiGamepadButtons.None, new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f });
        Assert.True(eight.Connected);
        for (int axis = 0; axis < UiGamepadState.AxisCount; axis++)
        {
            Assert.Equal((float)(axis + 1), eight.Axis(axis));
        }

        UiGamepadState two = new(UiGamepadButtons.None, new float[] { -1f, -2f });
        Assert.Equal(-1f, two.Axis(0));
        Assert.Equal(-2f, two.Axis(1));
        for (int axis = 2; axis < UiGamepadState.AxisCount; axis++)
        {
            Assert.Equal(0f, two.Axis(axis));
        }

        UiGamepadState empty = default;
        Assert.False(empty.Connected);
        Assert.Equal(UiGamepadButtons.None, empty.Buttons);

        // An axis index outside 0 to 5 throws on a connected slot.
        Assert.Throws<ArgumentOutOfRangeException>(() => eight.Axis(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => eight.Axis(6));
    }

    /// <summary>
    /// <see cref="UiInputState"/> stays near one kilobyte: A1 of this feature allows at most 1024 bytes.
    /// </summary>
    [Fact]
    public void TheStateStaysNearOneKilobyte()
    {
        int size = Unsafe.SizeOf<UiInputState>();
        Assert.True(size <= 1024, "UiInputState is " + size + " bytes, and A1 allows at most 1024.");
    }

    /// <summary>The GLFW key code of W.</summary>
    private const int W = 87;

    /// <summary>The GLFW key code of A.</summary>
    private const int A = 65;

    /// <summary>The point over the game, outside the root: (600, 400).</summary>
    private static readonly float2 GamePoint = new(600f, 400f);

    /// <summary>The six axis values that the gamepad tests write.</summary>
    private static ReadOnlySpan<float> AxisValues => new float[] { 1f, -1f, 0.5f, -0.5f, 0.25f, -0.25f };

    /// <summary>Asserts that one gamepad slot is disconnected: no button, no edge, and six axes of 0.</summary>
    private static void AssertDisconnectedGamepad(in UiGamepadState slot)
    {
        Assert.False(slot.Connected);
        Assert.Equal(UiGamepadButtons.None, slot.Buttons);
        Assert.Equal(UiGamepadButtons.None, slot.Pressed);
        Assert.Equal(UiGamepadButtons.None, slot.Released);
        for (int axis = 0; axis < UiGamepadState.AxisCount; axis++)
        {
            Assert.Equal(0f, slot.Axis(axis));
        }
    }

    private static void AssertPositionIsNaN(float2 position)
    {
        Assert.True(float.IsNaN(position.X), "The X component must be NaN, and it is " + position.X + ".");
        Assert.True(float.IsNaN(position.Y), "The Y component must be NaN, and it is " + position.Y + ".");
    }

    private static PointerEvent TargetedMove(Element target, float2 p)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerMove, 0d, target),
            Position = p,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>Creates a left pointer down with a target from the caller.</summary>
    private static PointerEvent TargetedDown(Element target, float2 p)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerDown, 0d, target),
            Position = p,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }
}
