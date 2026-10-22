using Brutal.GlfwApi;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Glfw.Tests;

/// <summary>
/// Tests of the key and char events of <see cref="GlfwUiInput"/>. Each test uses the test seam (no window) and calls
/// the internal methods <c>PushKey</c> and <c>PushChar</c>.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class GlfwUiInputKeyTests
{
    /// <summary>The GLFW key code of the key A.</summary>
    private const int KeyA = 65;

    private static readonly float2 OverRight = new(175f, 75f);

    /// <summary>
    /// The demo of phase 4. A key and a char through the adapter reach the focused element. A Tab press through the
    /// adapter then moves the focus.
    /// </summary>
    [Fact]
    public void AKeyAndACharReachTheFocusedElement()
    {
        PointerTree tree = new(nameof(GlfwUiInputKeyTests) + "." + nameof(AKeyAndACharReachTheFocusedElement));
        tree.AddFocusSheet();
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);
        Assert.True(tree.Context.SetFocus(tree.Right));

        input.PushKey(KeyA, 38, down: true, repeat: false, UiModifiers.None, 1d);
        input.PushChar('a', 1.25d);
        Assert.Equal(2, tree.Context.DrainEvents());
        tree.Settle();

        KeyEvent key = recorder.LastKey;
        CharEvent character = recorder.LastChar;
        TestContext.Current.TestOutputHelper?.WriteLine(
            tree.Right.Name + " " + tree.Right.States + " " + key.Header.Type + " " + key.Key + " "
            + character.Header.Type + " " + character.CodePoint + " " + tree.Right.Style.BackgroundColor);

        Assert.Equal(UiEventType.KeyDown, key.Header.Type);
        Assert.Same(tree.Right, key.Header.Target);
        Assert.Equal(KeyA, key.Key);
        Assert.Equal(UiEventType.Char, character.Header.Type);
        Assert.Same(tree.Right, character.Header.Target);
        Assert.Equal(97u, character.CodePoint);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);

        input.PushKey((int)GlfwKey.Tab, 15, down: true, repeat: false, UiModifiers.None, 2d);
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "after Tab: " + (tree.Context.FocusedElement?.Name ?? "none") + " " + tree.Left.States);

        Assert.Same(tree.Left, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PointerTree.FocusFill, tree.Left.Style.BackgroundColor);
    }

    /// <summary>
    /// A press gives a <see cref="UiEventType.KeyDown"/>, a repeat gives a <see cref="UiEventType.KeyDown"/> with
    /// <see cref="KeyEvent.IsRepeat"/>, and a release gives a <see cref="UiEventType.KeyUp"/>. A release never carries
    /// the repeat flag. The GLFW actions have the values that the adapter expects. The conversion of the real key
    /// handler, <c>KeyArgs</c>, gives the same down and repeat values.
    /// </summary>
    [Fact]
    public void APressARepeatAndAReleaseGiveDownRepeatAndUp()
    {
        PointerTree tree = new(nameof(GlfwUiInputKeyTests) + "." + nameof(APressARepeatAndAReleaseGiveDownRepeatAndUp));
        tree.Right.IsFocusable = true;
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);
        Assert.True(tree.Context.SetFocus(tree.Right));

        Assert.Equal(0, (int)GlfwKeyAction.Release);
        Assert.Equal(1, (int)GlfwKeyAction.Press);
        Assert.Equal(2, (int)GlfwKeyAction.Repeat);

        input.PushKey(KeyA, 0, down: true, repeat: false, UiModifiers.None, 1d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.KeyDown, recorder.LastKey.Header.Type);
        Assert.False(recorder.LastKey.IsRepeat);

        input.PushKey(KeyA, 0, down: true, repeat: true, UiModifiers.None, 2d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.KeyDown, recorder.LastKey.Header.Type);
        Assert.True(recorder.LastKey.IsRepeat);

        input.PushKey(KeyA, 0, down: false, repeat: false, UiModifiers.None, 3d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.KeyUp, recorder.LastKey.Header.Type);
        Assert.False(recorder.LastKey.IsRepeat);

        input.PushKey(KeyA, 0, down: false, repeat: true, UiModifiers.None, 4d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.KeyUp, recorder.LastKey.Header.Type);
        Assert.False(recorder.LastKey.IsRepeat);

        // The conversion of the real key handler. It needs no window.
        GlfwUiInput.KeyArgs(GlfwKeyAction.Press, out bool down, out bool repeat);
        Assert.True(down);
        Assert.False(repeat);

        GlfwUiInput.KeyArgs(GlfwKeyAction.Repeat, out down, out repeat);
        Assert.True(down);
        Assert.True(repeat);

        GlfwUiInput.KeyArgs(GlfwKeyAction.Release, out down, out repeat);
        Assert.False(down);
        Assert.False(repeat);
    }

    /// <summary>
    /// A key carries the key with no table (also <see cref="GlfwKey.Unknown"/>), the scan code, the modifiers, and
    /// the timestamp. A char carries its timestamp. Neither has a target from the adapter.
    /// </summary>
    [Fact]
    public void TheKeyCarriesTheScanCodeTheModifiersAndTheTime()
    {
        PointerTree tree = new(nameof(GlfwUiInputKeyTests) + "." + nameof(TheKeyCarriesTheScanCodeTheModifiersAndTheTime));
        tree.Right.IsFocusable = true;
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);
        Assert.True(tree.Context.SetFocus(tree.Right));

        UiModifiers modifiers = GlfwUiInput.ButtonModifiers(GlfwModifier.Control | GlfwModifier.Shift | GlfwModifier.NumLock);
        Assert.Equal(UiModifiers.Control | UiModifiers.Shift | UiModifiers.NumLock, modifiers);

        input.PushKey(KeyA, 38, down: true, repeat: false, modifiers, 12.5d);
        Assert.Equal(1, tree.Context.DrainEvents());
        KeyEvent key = recorder.LastKey;
        Assert.Same(tree.Right, key.Header.Target);
        Assert.Equal(KeyA, key.Key);
        Assert.Equal(38, key.ScanCode);
        Assert.Equal(modifiers, key.Modifiers);
        Assert.Equal(12.5d, key.Header.Timestamp);

        Assert.Equal(-1, (int)GlfwKey.Unknown);
        input.PushKey((int)GlfwKey.Unknown, 200, down: false, repeat: false, UiModifiers.Alt, 13d);
        Assert.Equal(1, tree.Context.DrainEvents());
        key = recorder.LastKey;
        Assert.Equal(UiEventType.KeyUp, key.Header.Type);
        Assert.Equal(-1, key.Key);
        Assert.Equal(200, key.ScanCode);
        Assert.Equal(UiModifiers.Alt, key.Modifiers);
        Assert.Equal(13d, key.Header.Timestamp);

        input.PushChar(0x00E9u, 14.75d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Right, recorder.LastChar.Header.Target);
        Assert.Equal(0x00E9u, recorder.LastChar.CodePoint);
        Assert.Equal(14.75d, recorder.LastChar.Header.Timestamp);
    }

    /// <summary>
    /// A Tab press through the adapter moves the focus forward, and a Shift+Tab press moves it back. A repeat moves
    /// the focus. A key up and a Tab with Control do not move it.
    /// </summary>
    [Fact]
    public void ATabPressMovesTheFocus()
    {
        PointerTree tree = new(nameof(GlfwUiInputKeyTests) + "." + nameof(ATabPressMovesTheFocus));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Inner.IsFocusable = true;
        tree.Settle();
        using GlfwUiInput input = new(tree.Context);
        int tab = (int)GlfwKey.Tab;
        UiModifiers shift = GlfwUiInput.ButtonModifiers(GlfwModifier.Shift);
        UiModifiers control = GlfwUiInput.ButtonModifiers(GlfwModifier.Control);

        input.PushKey(tab, 15, down: true, repeat: false, UiModifiers.None, 1d);
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.FocusedElement);

        input.PushKey(tab, 15, down: false, repeat: false, UiModifiers.None, 2d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.FocusedElement);

        input.PushKey(tab, 15, down: true, repeat: true, UiModifiers.None, 3d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Right, tree.Context.FocusedElement);

        input.PushKey(tab, 15, down: true, repeat: false, UiModifiers.None, 4d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Inner, tree.Context.FocusedElement);

        input.PushKey(tab, 15, down: true, repeat: false, control, 5d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Inner, tree.Context.FocusedElement);

        input.PushKey(tab, 15, down: true, repeat: false, shift, 6d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Right, tree.Context.FocusedElement);

        input.PushKey(tab, 15, down: true, repeat: false, shift, 7d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
    }

    /// <summary>
    /// The Tab key of Core is the GLFW Tab key, so a Tab press from GLFW needs no table.
    /// </summary>
    [Fact]
    public void TheCoreTabKeyEqualsTheGlfwTabKey()
    {
        Assert.Equal(258, (int)GlfwKey.Tab);
        Assert.Equal(UiContext.TabKey, (int)GlfwKey.Tab);
    }

    /// <summary>
    /// A surrogate and a value above U+10FFFF push nothing. Each Unicode scalar value pushes, also a value above
    /// U+FFFF, so a fix of the char cast in BRUTAL needs no change in the adapter.
    /// </summary>
    [Fact]
    public void ACharThatIsNotAScalarValueIsDropped()
    {
        PointerTree tree = new(nameof(GlfwUiInputKeyTests) + "." + nameof(ACharThatIsNotAScalarValueIsDropped));
        tree.Right.IsFocusable = true;
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);
        Assert.True(tree.Context.SetFocus(tree.Right));

        input.PushChar(0xD800u, 1d);
        input.PushChar(0xDBFFu, 1d);
        input.PushChar(0xDC00u, 1d);
        input.PushChar(0xDFFFu, 1d);
        input.PushChar(0x110000u, 1d);
        input.PushChar(uint.MaxValue, 1d);
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(0, recorder.Count);

        uint[] valid = [0x1F600u, 0xD7FFu, 0xE000u, 0xFFFFu, 0x10FFFFu, 0u];
        foreach (uint codePoint in valid)
        {
            input.PushChar(codePoint, 2d);
            Assert.Equal(1, tree.Context.DrainEvents());
            Assert.Equal(UiEventType.Char, recorder.LastChar.Header.Type);
            Assert.Equal(codePoint, recorder.LastChar.CodePoint);
        }
    }

    /// <summary>
    /// With <see cref="GlfwUiInput.KeyboardEnabled"/> <see langword="false"/>, the adapter pushes no key and no char.
    /// A pointer push still goes. A value of <see langword="true"/> again starts the key and char pushes.
    /// </summary>
    [Fact]
    public void KeyboardEnabledFalseStopsTheKeyAndCharPushes()
    {
        PointerTree tree = new(nameof(GlfwUiInputKeyTests) + "." + nameof(KeyboardEnabledFalseStopsTheKeyAndCharPushes));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);
        Assert.True(input.KeyboardEnabled);
        Assert.True(tree.Context.SetFocus(tree.Right));

        input.KeyboardEnabled = false;
        input.PushKey(KeyA, 38, down: true, repeat: false, UiModifiers.None, 1d);
        input.PushKey((int)GlfwKey.Tab, 15, down: true, repeat: false, UiModifiers.None, 1d);
        input.PushKey(KeyA, 38, down: false, repeat: false, UiModifiers.None, 1d);
        input.PushChar('a', 1d);
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(0, recorder.Count);
        Assert.Same(tree.Right, tree.Context.FocusedElement);

        input.PushCursorMove(OverRight, UiModifiers.None, 2d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.PointerMove, recorder.LastPointer.Header.Type);
        Assert.Same(tree.Right, tree.Context.PointerHit);

        input.KeyboardEnabled = true;
        input.PushKey(KeyA, 38, down: true, repeat: false, UiModifiers.None, 3d);
        input.PushChar('b', 3d);
        Assert.Equal(2, tree.Context.DrainEvents());
        Assert.Equal(KeyA, recorder.LastKey.Key);
        Assert.Equal(98u, recorder.LastChar.CodePoint);
    }

    /// <summary>
    /// After <see cref="GlfwUiInput.Dispose"/>, <c>PushKey</c> and <c>PushChar</c> return at once, also with
    /// <see cref="GlfwUiInput.KeyboardEnabled"/> <see langword="true"/>.
    /// </summary>
    [Fact]
    public void DisposeStopsTheKeyAndCharPushes()
    {
        PointerTree tree = new(nameof(GlfwUiInputKeyTests) + "." + nameof(DisposeStopsTheKeyAndCharPushes));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        GlfwUiInput input = new(tree.Context);
        Assert.True(tree.Context.SetFocus(tree.Right));

        input.Dispose();
        input.KeyboardEnabled = true;
        input.PushKey(KeyA, 38, down: true, repeat: false, UiModifiers.None, 1d);
        input.PushKey((int)GlfwKey.Tab, 15, down: true, repeat: false, UiModifiers.None, 1d);
        input.PushChar('a', 1d);
        input.Dispose();

        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(0, recorder.Count);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
    }
}
