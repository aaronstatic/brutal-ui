using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the remove keys of <see cref="TextField"/> (phase 2): Backspace removes the code point before the caret,
/// Delete removes the code point after the caret, and each remove calls <see cref="TextField.TextChanged"/> one time.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>. Each test pushes key and char events with no caller target, so the key
/// route gives them to the focus owner. The class interns no class name.
/// </remarks>
public sealed class TextFieldDeleteTests
{
    /// <summary>Backspace removes the code point before the caret, moves the caret back, and calls one time.</summary>
    [Fact]
    public void BackspaceRemovesTheCodePointBeforeTheCaret()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        int calls = 0;
        TextField? sender = null;
        string? seen = null;
        tree.Field.TextChanged = f =>
        {
            calls++;
            sender = f;
            seen = System.Text.Encoding.UTF8.GetString(f.Text);
        };

        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        tree.Context.DrainEvents();

        Assert.True(tree.Field.Text.SequenceEqual("ac"u8));
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(1, calls);
        Assert.Same(tree.Field, sender);
        Assert.Equal("ac", seen);
    }

    /// <summary>Delete removes the code point after the caret, and the caret stays.</summary>
    [Fact]
    public void DeleteRemovesTheCodePointAfterTheCaret()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Context.PushEvent(Down(WidgetInput.HomeKey));
        tree.Context.PushEvent(Down(WidgetInput.DeleteKey));
        tree.Context.DrainEvents();

        Assert.True(tree.Field.Text.SequenceEqual("bc"u8));
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// Backspace removes both bytes of an <c>é</c> and all 4 bytes of an emoji, and Delete removes all 3 bytes of a euro
    /// sign.
    /// </summary>
    [Fact]
    public void BackspaceRemovesAWholeCodePoint()
    {
        TextFieldTree accent = new(new TextField.CreateInfo { Capacity = 16, Text = "aé" });
        accent.Focus();
        Assert.Equal(3, accent.Field.Text.Length);
        accent.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        accent.Context.DrainEvents();

        Assert.True(accent.Field.Text.SequenceEqual("a"u8));
        Assert.Equal(1, accent.Field.CaretIndex);

        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "a😀" });
        tree.Focus();
        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        tree.Context.DrainEvents();

        Assert.True(tree.Field.Text.SequenceEqual("a"u8));
        Assert.Equal(1, tree.Field.CaretIndex);

        TextFieldTree euro = new(new TextField.CreateInfo { Capacity = 16, Text = "€b" });
        euro.Focus();
        euro.Context.PushEvent(Down(WidgetInput.HomeKey));
        euro.Context.PushEvent(Down(WidgetInput.DeleteKey));
        euro.Context.DrainEvents();

        Assert.True(euro.Field.Text.SequenceEqual("b"u8));
        Assert.Equal(0, euro.Field.CaretIndex);
    }

    /// <summary>
    /// Backspace at 0 and Delete at the end change nothing, mark nothing, and call nothing. The field still stops the
    /// walk, so the bubble handler of the root gets no key. An empty field gives the same result.
    /// </summary>
    [Fact]
    public void NothingToRemoveCallsNothing()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "ab" });
        tree.Focus();

        int calls = 0;
        int rootKeys = 0;
        tree.Field.TextChanged = f => calls++;
        tree.Root.OnKeyDown += (ref KeyEvent e) => rootKeys++;

        tree.Settle();
        tree.Context.PushEvent(Down(WidgetInput.DeleteKey));
        tree.Context.DrainEvents();
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);
        Assert.Equal(2, tree.Field.CaretIndex);

        tree.Context.PushEvent(Down(WidgetInput.HomeKey));
        tree.Context.DrainEvents();
        tree.Settle();
        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        tree.Context.DrainEvents();
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);
        Assert.Equal(0, tree.Field.CaretIndex);

        Assert.True(tree.Field.Text.SequenceEqual("ab"u8));
        Assert.Equal(0, calls);
        Assert.Equal(0, rootKeys);

        TextFieldTree empty = new();
        empty.Focus();
        int emptyKeys = 0;
        empty.Field.TextChanged = f => calls++;
        empty.Root.OnKeyDown += (ref KeyEvent e) => emptyKeys++;
        empty.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        empty.Context.PushEvent(Down(WidgetInput.DeleteKey));
        empty.Context.DrainEvents();

        Assert.True(empty.Field.Text.IsEmpty);
        Assert.Equal(0, calls);
        Assert.Equal(0, emptyKeys);
    }

    /// <summary>Three Backspace keys, two of them a repeat, remove three code points and give three calls.</summary>
    [Fact]
    public void ARepeatRemovesAgain()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abcd" });
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        KeyEvent repeat = Down(WidgetInput.BackspaceKey);
        repeat.IsRepeat = true;
        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        tree.Context.PushEvent(repeat);
        tree.Context.PushEvent(repeat);
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.True(tree.Field.Text.SequenceEqual("a"u8));
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(3, calls);
    }

    /// <summary>
    /// A full field of 16 bytes loses one byte to Backspace. A euro sign of 3 bytes then does not fit and drops. The char
    /// <c>b</c> then fits and fills the field again.
    /// </summary>
    [Fact]
    public void ARemoveMakesRoomForAnInsert()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "aaaaaaaaaaaaaaaa" });
        Assert.Equal(16, tree.Field.Text.Length);
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        tree.Context.DrainEvents();
        Assert.Equal(15, tree.Field.Text.Length);
        Assert.Equal(1, calls);

        tree.Type("€");
        tree.Context.DrainEvents();
        Assert.Equal(15, tree.Field.Text.Length);
        Assert.Equal(1, calls);

        tree.Type("b");
        tree.Context.DrainEvents();
        Assert.Equal(16, tree.Field.Text.Length);
        Assert.True(tree.Field.Text.SequenceEqual("aaaaaaaaaaaaaaab"u8));
        Assert.Equal(16, tree.Field.CaretIndex);
        Assert.Equal(2, calls);
    }

    /// <summary>An edit in the middle keeps the rest: <c>hello</c>, Left two times, Backspace, and <c>L</c>.</summary>
    [Fact]
    public void AnEditInTheMiddleKeepsTheRest()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        tree.Type("L");
        tree.Context.DrainEvents();

        Assert.True(tree.Field.Text.SequenceEqual("heLlo"u8));
        Assert.Equal(3, tree.Field.CaretIndex);
    }

    /// <summary>The caret rect draws at the new caret after a remove: <c>abc</c> and Backspace give (16, 0, 1, 16).</summary>
    [Fact]
    public void TheCaretDrawsAfterARemove()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        RecordingRenderer renderer = tree.Frame();

        Assert.True(tree.Field.Text.SequenceEqual("ab"u8));
        int text = TextFieldDraws.IndexOfText(renderer, "ab"u8);
        int caret = TextFieldDraws.IndexOfOnlyRect(renderer);
        Assert.Equal(new float4(16f, 0f, 1f, 16f), renderer.Calls[caret].Rect);
        Assert.True(text < caret);
    }

    /// <summary>Creates a key down with no target, no modifier, and no repeat.</summary>
    /// <param name="key">The key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Down(int key)
    {
        return PointerTree.Key(UiEventType.KeyDown, key, UiModifiers.None);
    }
}
