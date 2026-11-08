using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Glfw.Tests;

/// <summary>
/// Tests of the held-key clear rules of <see cref="GlfwUiInput"/>: the focus loss, the focus gain, the
/// <see cref="GlfwUiInput.KeyboardEnabled"/> switch, the edge rule of the setter, and <see cref="GlfwUiInput.Dispose"/>.
/// Each test uses the test seam (no window) and calls the internal methods <c>PushKey</c> and <c>PushFocus</c>.
/// </summary>
/// <remarks>
/// <para>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </para>
/// <para>
/// A test reads <see cref="UiContext.Input"/> after <see cref="UiContext.Update(float)"/>. The clear of
/// <see cref="UiContext.ClearHeldKeys"/> applies at the end of the next drain (Decision 10), so a test counts one
/// <see cref="UiContext.Update(float)"/> after the clear request.
/// </para>
/// </remarks>
public sealed class GlfwUiInputFocusTests
{
    /// <summary>
    /// W down, one update, a focus loss, and one update leave no key down: the key reads not down and released, and
    /// the modifiers become <see cref="UiModifiers.None"/>.
    /// </summary>
    [Fact]
    public void AFocusLossClearsTheHeldKeys()
    {
        PointerTree tree = new(nameof(GlfwUiInputFocusTests) + "." + nameof(AFocusLossClearsTheHeldKeys));
        tree.Settle();
        using GlfwUiInput input = new(tree.Context);

        input.PushKey(W, 0, down: true, repeat: false, UiModifiers.Shift, 1d);
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));

        input.PushFocus(false);
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.IsDown(W));
        Assert.True(tree.Context.Input.Keys.WasReleased(W));
        Assert.Equal(UiModifiers.None, tree.Context.Input.Keys.Modifiers);
    }

    /// <summary>
    /// A focus gain changes nothing: <c>PushFocus(true)</c> keeps W down through the next update.
    /// </summary>
    [Fact]
    public void AFocusGainChangesNothing()
    {
        PointerTree tree = new(nameof(GlfwUiInputFocusTests) + "." + nameof(AFocusGainChangesNothing));
        tree.Settle();
        using GlfwUiInput input = new(tree.Context);

        input.PushKey(W, 0, down: true, repeat: false, UiModifiers.None, 1d);
        tree.Context.Update(0f);

        input.PushFocus(true);
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.False(tree.Context.Input.Keys.WasReleased(W));
    }

    /// <summary>
    /// <see cref="GlfwUiInput.KeyboardEnabled"/> set to <see langword="false"/> clears the held keys: W down, one
    /// update, the switch, and one update leave W not down.
    /// </summary>
    [Fact]
    public void KeyboardEnabledFalseClearsTheHeldKeys()
    {
        PointerTree tree = new(nameof(GlfwUiInputFocusTests) + "." + nameof(KeyboardEnabledFalseClearsTheHeldKeys));
        tree.Settle();
        using GlfwUiInput input = new(tree.Context);

        input.PushKey(W, 0, down: true, repeat: false, UiModifiers.None, 1d);
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));

        input.KeyboardEnabled = false;
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.IsDown(W));
        Assert.True(tree.Context.Input.Keys.WasReleased(W));
    }

    /// <summary>
    /// Only the switch from <see langword="true"/> to <see langword="false"/> clears: a write of
    /// <see langword="true"/> while the value is <see langword="true"/> keeps W down, and a second write of
    /// <see langword="true"/> after a new W down keeps W down too.
    /// </summary>
    [Fact]
    public void OnlyTheSwitchFromTrueToFalseClears()
    {
        PointerTree tree = new(nameof(GlfwUiInputFocusTests) + "." + nameof(OnlyTheSwitchFromTrueToFalseClears));
        tree.Settle();
        using GlfwUiInput input = new(tree.Context);

        input.PushKey(W, 0, down: true, repeat: false, UiModifiers.None, 1d);
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));

        // A write of the same value does nothing.
        input.KeyboardEnabled = true;
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));

        // The switch to false clears.
        input.KeyboardEnabled = false;
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.IsDown(W));

        // The switch back, a new W down, and one more write of the same value keep W down.
        input.KeyboardEnabled = true;
        input.PushKey(W, 0, down: true, repeat: false, UiModifiers.None, 2d);
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));

        input.KeyboardEnabled = true;
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));
    }

    /// <summary>
    /// After <see cref="GlfwUiInput.Dispose"/>, <c>PushFocus(false)</c> clears nothing: W stays down. The
    /// <see cref="GlfwUiInput.KeyboardEnabled"/> setter still clears: the switch to <see langword="false"/>
    /// releases the held W at the next update, even after <see cref="GlfwUiInput.Dispose"/>.
    /// </summary>
    [Fact]
    public void DisposeStopsTheFocusClear()
    {
        PointerTree tree = new(nameof(GlfwUiInputFocusTests) + "." + nameof(DisposeStopsTheFocusClear));
        tree.Settle();
        GlfwUiInput input = new(tree.Context);

        input.PushKey(W, 0, down: true, repeat: false, UiModifiers.None, 1d);
        tree.Context.Update(0f);

        input.Dispose();
        input.PushFocus(false);
        tree.Context.Update(0f);
        Assert.True(tree.Context.Input.Keys.IsDown(W));
        Assert.False(tree.Context.Input.Keys.WasReleased(W));

        // The setter still clears after Dispose: the call only sets a flag.
        input.KeyboardEnabled = false;
        input.PushKey(W, 0, down: true, repeat: false, UiModifiers.None, 2d);
        tree.Context.Update(0f);
        Assert.False(tree.Context.Input.Keys.IsDown(W));
        Assert.True(tree.Context.Input.Keys.WasReleased(W));
    }

    /// <summary>The GLFW key code of W.</summary>
    private const int W = 87;
}
