using System;
using System.Text;
using Brutal.Localization;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the text from code of <see cref="TextField"/> (phase 3): the three <c>SetText</c> overloads, the cut rule
/// at the capacity, and <see cref="TextField.GetText(Span{byte})"/>.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>. The class interns no class name. The <see cref="LString"/> keys start with
/// <c>text-field-text-tests</c>, so a parallel class cannot clash with them.
/// </remarks>
public sealed class TextFieldTextTests
{
    /// <summary>
    /// <c>SetText(ReadOnlySpan&lt;byte&gt;)</c> copies the bytes, puts the caret at the end, marks the paint, and calls
    /// no delegate.
    /// </summary>
    [Fact]
    public void SetTextCopiesTheBytes()
    {
        TextFieldTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.Field.TextChanged = f => calls++;
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Field.SetText("name"u8);

        Assert.True(tree.Field.Text.SequenceEqual("name"u8));
        Assert.Equal(4, tree.Field.CaretIndex);
        Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & DirtyFlags.Paint);
        Assert.Equal(0, calls);
    }

    /// <summary>A string is encoded to UTF-8: <c>héllo</c> takes 6 bytes.</summary>
    [Fact]
    public void SetTextEncodesAString()
    {
        TextFieldTree tree = new();
        tree.Settle();

        tree.Field.SetText((string?)"héllo");

        Assert.Equal(6, tree.Field.Text.Length);
        Assert.Equal("héllo", Encoding.UTF8.GetString(tree.Field.Text));
        Assert.Equal(6, tree.Field.CaretIndex);
        Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & DirtyFlags.Paint);
    }

    /// <summary>
    /// An <see cref="LString"/> gives its localized text at the call. A later locale change does not change the field,
    /// also after <see cref="UiContext.RefreshLocalizedText"/>.
    /// </summary>
    [Fact]
    public void SetTextReadsTheLocalizedText()
    {
        TextFieldTree tree = new();
        tree.Settle();

        LString name = new("text-field-text-tests.name", "Name");
        name.Localize("Nom");
        tree.Field.SetText(name);
        Assert.Equal("Nom", Encoding.UTF8.GetString(tree.Field.Text));

        name.Localize("Namen");
        tree.Context.RefreshLocalizedText();
        tree.Settle();

        Assert.Equal("Nom", Encoding.UTF8.GetString(tree.Field.Text));
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Field.SetText((LString?)null);
        Assert.True(tree.Field.Text.IsEmpty);
    }

    /// <summary>After a settle, the same span and the same string mark no paint and keep the caret.</summary>
    [Fact]
    public void TheSameTextMarksNothing()
    {
        TextFieldTree tree = new();
        tree.Field.SetText("abc"u8);
        tree.Focus();
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();
        tree.Settle();
        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Field.SetText("abc"u8);
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Field.SetText((string?)"abc");
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Field.SetText(new LString("text-field-text-tests.same", "abc"));
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);
        Assert.Equal(2, tree.Field.CaretIndex);

        tree.Field.SetText("abd"u8);
        Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & DirtyFlags.Paint);
        Assert.Equal(3, tree.Field.CaretIndex);
    }

    /// <summary>
    /// A set past the capacity cuts at a code point: with the capacity 4, <c>abc€</c> gives <c>abc</c> and
    /// <c>ab😀</c> gives <c>ab</c>. The same long text again marks nothing.
    /// </summary>
    [Fact]
    public void ASetPastTheCapacityCutsAtACodePoint()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 4 });
        tree.Settle();

        tree.Field.SetText("abc€"u8);
        Assert.True(tree.Field.Text.SequenceEqual("abc"u8));
        Assert.Equal(3, tree.Field.CaretIndex);

        tree.Field.SetText((string?)"ab😀");
        Assert.True(tree.Field.Text.SequenceEqual("ab"u8));
        Assert.Equal(2, tree.Field.CaretIndex);

        tree.Field.SetText((string?)"abc€");
        Assert.True(tree.Field.Text.SequenceEqual("abc"u8));
        tree.Settle();
        tree.Field.SetText((string?)"abc€");
        tree.Field.SetText("abc€"u8);
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Field.SetText("abcdefgh"u8);
        Assert.True(tree.Field.Text.SequenceEqual("abcd"u8));
        Assert.Equal(4, tree.Field.CaretIndex);
    }

    /// <summary>An empty span and a null string give 0 bytes and the caret at 0.</summary>
    [Fact]
    public void AnEmptySetClearsTheText()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Settle();

        tree.Field.SetText(ReadOnlySpan<byte>.Empty);
        Assert.Equal(0, tree.Field.Text.Length);
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Field.SetText("xyz"u8);
        tree.Settle();
        tree.Field.SetText((string?)null);
        Assert.Equal(0, tree.Field.Text.Length);
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Settle();
        tree.Field.SetText((string?)null);
        tree.Field.SetText(ReadOnlySpan<byte>.Empty);
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);
    }

    /// <summary>
    /// <see cref="TextField.GetText(Span{byte})"/> gives the count and the bytes. A span of 16 bytes for 2 bytes keeps
    /// the rest as it was.
    /// </summary>
    [Fact]
    public void GetTextCopiesTheText()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hi" });
        Span<byte> destination = stackalloc byte[16];
        destination.Fill(0x7F);

        int count = tree.Field.GetText(destination);

        Assert.Equal(2, count);
        Assert.True(destination[..2].SequenceEqual("hi"u8));
        for (int i = 2; i < destination.Length; i++)
        {
            Assert.Equal(0x7F, destination[i]);
        }

        Assert.Equal(0, tree.Field.GetText(Span<byte>.Empty));
    }

    /// <summary>A destination of 3 bytes gets <c>a</c> only from <c>a€</c>, because the euro sign needs 3 bytes.</summary>
    [Fact]
    public void GetTextIntoAShortSpanCutsAtACodePoint()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "a€" });
        Span<byte> destination = stackalloc byte[3];
        destination.Fill(0x7F);

        int count = tree.Field.GetText(destination);

        Assert.Equal(1, count);
        Assert.Equal((byte)'a', destination[0]);
        Assert.Equal(0x7F, destination[1]);
        Assert.Equal(0x7F, destination[2]);
        Assert.Equal(4, tree.Field.Text.Length);
    }

    /// <summary>After a set, Left, Backspace, and a char edit the new text.</summary>
    [Fact]
    public void TheTextIsUsableAfterASet()
    {
        TextFieldTree tree = new();
        tree.Focus();
        tree.Field.SetText("abc"u8);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.BackspaceKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.True(tree.Field.Text.SequenceEqual("ac"u8));
        Assert.Equal(1, tree.Field.CaretIndex);

        tree.Type("X");
        tree.Context.DrainEvents();
        Assert.True(tree.Field.Text.SequenceEqual("aXc"u8));
        Assert.Equal(2, tree.Field.CaretIndex);
    }

    /// <summary>
    /// An invalid byte is one step. With the bytes <c>a</c>, <c>0xFF</c>, and <c>b</c>, Left from 3 gives 2 and then
    /// 1, Right gives 2, and Backspace removes the byte <c>0xFF</c>.
    /// </summary>
    [Fact]
    public void AnInvalidByteStepsAsOne()
    {
        TextFieldTree tree = new();
        tree.Focus();
        tree.Field.SetText(new byte[] { 0x61, 0xFF, 0x62 });
        Assert.Equal(3, tree.Field.CaretIndex);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(2, tree.Field.CaretIndex);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(1, tree.Field.CaretIndex);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.BackspaceKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.True(tree.Field.Text.SequenceEqual(new byte[] { 0x61, 0x62 }));
        Assert.Equal(1, tree.Field.CaretIndex);
    }
}
