using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the Enter key of <see cref="TextField"/> (phase 3): the main Enter key with no repeat stops the walk and
/// calls <see cref="TextField.Submitted"/>, and each other form of Enter walks on.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>. Each test pushes key and char events with no caller target, so the key
/// route gives them to the focus owner. The class interns no class name.
/// </remarks>
public sealed class TextFieldSubmitTests
{
    /// <summary>The GLFW key code of the keypad Enter key (<c>GLFW_KEY_KP_ENTER</c>).</summary>
    private const int KeypadEnterKey = 335;

    /// <summary>
    /// The demo: Enter calls <see cref="TextField.Submitted"/> one time with the field as the sender. The two typed
    /// chars called <see cref="TextField.TextChanged"/> two times, and <see cref="TextField.GetText(Span{byte})"/> reads
    /// the text.
    /// </summary>
    [Fact]
    public void EnterCallsSubmitted()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int submits = 0;
        int changes = 0;
        TextField? sender = null;
        tree.Field.Submitted = f =>
        {
            submits++;
            sender = f;
        };
        tree.Field.TextChanged = f => changes++;

        tree.Type("ok");
        tree.Context.PushEvent(Down(WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.Update(0f);

        Assert.Equal(1, submits);
        Assert.Same(tree.Field, sender);
        Assert.Equal(2, changes);

        Span<byte> buffer = stackalloc byte[16];
        int count = tree.Field.GetText(buffer);
        Assert.Equal(2, count);
        Assert.True(buffer[..count].SequenceEqual("ok"u8));
    }

    /// <summary>Enter stops the walk, so a bubble handler on the root gets no Enter. The text does not change.</summary>
    [Fact]
    public void EnterStopsTheWalk()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "ok" });
        tree.Focus();

        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;
        int submits = 0;
        tree.Field.Submitted = f => submits++;

        tree.Context.PushEvent(Down(WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.DrainEvents();

        Assert.Equal(1, submits);
        Assert.Equal(0, rootDowns);
        Assert.True(tree.Field.Text.SequenceEqual("ok"u8));
        Assert.Equal(2, tree.Field.CaretIndex);
    }

    /// <summary>A repeat of Enter calls nothing and walks on to the root.</summary>
    [Fact]
    public void ARepeatOfEnterWalksOn()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;
        int submits = 0;
        tree.Field.Submitted = f => submits++;

        KeyEvent repeat = PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None);
        repeat.IsRepeat = true;
        tree.Context.PushEvent(repeat);
        tree.Context.DrainEvents();

        Assert.Equal(0, submits);
        Assert.Equal(1, rootDowns);
    }

    /// <summary>The keypad Enter key (335) calls nothing and walks on to the root.</summary>
    [Fact]
    public void TheKeypadEnterWalksOn()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;
        int submits = 0;
        tree.Field.Submitted = f => submits++;

        tree.Context.PushEvent(Down(KeypadEnterKey, UiModifiers.None));
        tree.Context.DrainEvents();

        Assert.Equal(0, submits);
        Assert.Equal(1, rootDowns);
    }

    /// <summary>
    /// <c>Control</c> with Enter walks on and calls nothing. <c>Shift</c> with Enter calls
    /// <see cref="TextField.Submitted"/> and stops the walk.
    /// </summary>
    [Fact]
    public void TheModifiersAndEnter()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;
        int submits = 0;
        tree.Field.Submitted = f => submits++;

        tree.Context.PushEvent(Down(WidgetInput.EnterKey, UiModifiers.Control));
        tree.Context.DrainEvents();

        Assert.Equal(0, submits);
        Assert.Equal(1, rootDowns);

        tree.Context.PushEvent(Down(WidgetInput.EnterKey, UiModifiers.Shift));
        tree.Context.DrainEvents();

        Assert.Equal(1, submits);
        Assert.Equal(1, rootDowns);
    }

    /// <summary>A field with no <see cref="TextField.Submitted"/> delegate still stops the Enter walk.</summary>
    [Fact]
    public void EnterWithNoDelegateStillStops()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int rootDowns = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootDowns++;

        Assert.Null(tree.Field.Submitted);
        tree.Context.PushEvent(Down(WidgetInput.EnterKey, UiModifiers.None));
        tree.Context.DrainEvents();

        Assert.Equal(0, rootDowns);
    }

    /// <summary>Creates a key down event with no target and no repeat.</summary>
    /// <param name="key">The key code.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Down(int key, UiModifiers modifiers)
    {
        return PointerTree.Key(UiEventType.KeyDown, key, modifiers);
    }
}
