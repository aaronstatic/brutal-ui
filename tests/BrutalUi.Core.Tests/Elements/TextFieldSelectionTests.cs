using System;
using System.Text;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the selection of <see cref="TextField"/> (phase 5): <c>Shift</c> with Left, Right, Home, or End extends a
/// selection from the anchor, a char replaces the selection, Backspace and Delete remove it, a move with no
/// <c>Shift</c> collapses it, and the focused field draws the selection behind the text.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>. Each test pushes key events with no caller target, so the key route gives
/// them to the focus owner. The class interns no class name.
/// </remarks>
public sealed class TextFieldSelectionTests
{
    /// <summary>The clip of the field: its padding box.</summary>
    private static readonly float4 FieldClip = new(0f, 0f, 81f, 16f);

    /// <summary>
    /// The demo of phase 5. The field holds <c>hello</c> and the focus. Shift with Left two times selects <c>lo</c>, and
    /// the frame draws a selection rect at (24, 0, 16, 16) in the selection color before the text call. The char
    /// <c>p</c> then gives <c>help</c>, one call of <see cref="TextField.TextChanged"/>, the caret at 4, and no
    /// selection rect.
    /// </summary>
    [Fact]
    public void ACharReplacesTheSelection()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(5, tree.Field.AnchorIndex);
        Assert.Equal(3, tree.Field.CaretIndex);
        Rgba8 selectionColor = SelectionColor(tree);
        int push = TextFieldDraws.IndexOfClip(renderer, FieldClip);
        int pop = TextFieldDraws.IndexOfPop(renderer, push);
        int selection = IndexOfRectWithColor(renderer, selectionColor);
        int text = TextFieldDraws.IndexOfText(renderer, "hello"u8);
        Assert.Equal(new float4(24f, 0f, 16f, 16f), renderer.Calls[selection].Rect);
        Assert.True(push < selection);
        Assert.True(selection < text);
        Assert.True(text < pop);
        Assert.Equal(0, calls);

        tree.Context.PushEvent(PointerTree.Char('p'));
        renderer = tree.Frame();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: Shift+Left two times on hello, then p, Text = \"" + Encoding.UTF8.GetString(tree.Field.Text)
            + "\", calls = " + calls + ", caret = " + tree.Field.CaretIndex + ", anchor = " + tree.Field.AnchorIndex);

        Assert.True(tree.Field.Text.SequenceEqual("help"u8));
        Assert.Equal(1, calls);
        Assert.Equal(4, tree.Field.CaretIndex);
        Assert.Equal(4, tree.Field.AnchorIndex);
        Assert.Equal(0, CountRectsWithColor(renderer, selectionColor));
        Assert.Equal(1, TextFieldDraws.CountRects(renderer));
    }

    /// <summary>
    /// Shift with Home selects from the caret to the start. End with no Shift collapses the selection, two Left keys
    /// move the caret to 3, and Shift with End selects from 3 to the end.
    /// </summary>
    [Fact]
    public void ShiftHomeAndShiftEndSelect()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        tree.Context.PushEvent(Down(WidgetInput.HomeKey, UiModifiers.Shift));
        tree.Context.DrainEvents();
        AssertSelection(tree.Field, 0, 5);
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(5, tree.Field.AnchorIndex);

        tree.Context.PushEvent(Down(WidgetInput.EndKey, UiModifiers.None));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(3, tree.Field.AnchorIndex);

        tree.Context.PushEvent(Down(WidgetInput.EndKey, UiModifiers.Shift));
        tree.Context.DrainEvents();
        AssertSelection(tree.Field, 3, 5);
        Assert.Equal(5, tree.Field.CaretIndex);
        Assert.Equal(3, tree.Field.AnchorIndex);

        RecordingRenderer renderer = tree.Frame();
        Assert.Equal(new float4(24f, 0f, 16f, 16f), renderer.Calls[IndexOfRectWithColor(renderer, SelectionColor(tree))].Rect);
    }

    /// <summary>
    /// With the selection from 1 to 3, Left with no Shift puts the caret at 1, and Right puts it at 3. The caret takes no
    /// further step, and the selection becomes empty. A collapse with no caret step still marks the paint.
    /// </summary>
    [Fact]
    public void LeftAndRightCollapseTheSelection()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        // The anchor at 1 and the caret at 3. Left goes to the start of the selection: a step back from 3.
        SelectOneToThree(tree);
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(1, tree.Field.AnchorIndex);

        // The same selection. Right goes to the end of the selection: the caret stays at 3.
        SelectOneToThree(tree);
        tree.Settle();
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(3, tree.Field.AnchorIndex);
        Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & DirtyFlags.Paint);

        // The anchor at 3 and the caret at 1. Left keeps the caret at 1, and the anchor follows.
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        tree.Context.DrainEvents();
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(3, tree.Field.AnchorIndex);
        tree.Settle();
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(1, tree.Field.AnchorIndex);
        Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & DirtyFlags.Paint);

        RecordingRenderer renderer = tree.Frame();
        Assert.Equal(0, CountRectsWithColor(renderer, SelectionColor(tree)));
        Assert.True(tree.Field.Text.SequenceEqual("hello"u8));
    }

    /// <summary>Home and End with no Shift move the caret to 0 or to the end, and the anchor follows.</summary>
    [Fact]
    public void HomeAndEndCollapse()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        SelectOneToThree(tree);
        tree.Context.PushEvent(Down(WidgetInput.HomeKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(0, tree.Field.AnchorIndex);

        SelectOneToThree(tree);
        tree.Context.PushEvent(Down(WidgetInput.EndKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(5, tree.Field.CaretIndex);
        Assert.Equal(5, tree.Field.AnchorIndex);

        RecordingRenderer renderer = tree.Frame();
        Assert.Equal(0, CountRectsWithColor(renderer, SelectionColor(tree)));
    }

    /// <summary>
    /// Backspace with a selection removes the selection with one call, and the caret goes to the start of the selection.
    /// Delete does the same. The caret can be at either end of the selection.
    /// </summary>
    [Fact]
    public void BackspaceAndDeleteRemoveTheSelection()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        // The caret at 3 and the anchor at 5: the caret is at the start of the selection.
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.True(tree.Field.Text.SequenceEqual("hel"u8));
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(3, tree.Field.AnchorIndex);
        Assert.Equal(1, calls);

        // The anchor at 1 and the caret at 3: the caret is at the end of the selection.
        tree.Context.PushEvent(Down(WidgetInput.HomeKey, UiModifiers.None));
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.None));
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.DeleteKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.True(tree.Field.Text.SequenceEqual("h"u8));
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(1, tree.Field.AnchorIndex);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// A full field with the last byte selected drops a char of 3 bytes: no change, no call, and the selection stays.
    /// The walk stops. The char <c>b</c> then fits, because it replaces the selected byte.
    /// </summary>
    [Fact]
    public void ADroppedCharKeepsTheSelection()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "aaaaaaaaaaaaaaaa" });
        Assert.Equal(16, tree.Field.Text.Length);
        tree.Focus();

        int calls = 0;
        int rootChars = 0;
        tree.Field.TextChanged = f => calls++;
        tree.Root.OnChar += (ref CharEvent e) => rootChars++;

        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        tree.Context.PushEvent(PointerTree.Char('€'));
        tree.Context.DrainEvents();
        Assert.True(tree.Field.Text.SequenceEqual("aaaaaaaaaaaaaaaa"u8));
        Assert.Equal(15, tree.Field.CaretIndex);
        Assert.Equal(16, tree.Field.AnchorIndex);
        Assert.Equal(0, calls);
        Assert.Equal(0, rootChars);

        tree.Context.PushEvent(PointerTree.Char('b'));
        tree.Context.DrainEvents();
        Assert.True(tree.Field.Text.SequenceEqual("aaaaaaaaaaaaaaab"u8));
        Assert.Equal(16, tree.Field.CaretIndex);
        Assert.Equal(16, tree.Field.AnchorIndex);
        Assert.Equal(1, calls);
        Assert.Equal(0, rootChars);
    }

    /// <summary>
    /// With the caret at 3, Shift with Right two times and Shift with Left four times move the caret over the anchor.
    /// The anchor stays at 3, the caret goes to 1, and the selection rect is at (8, 0, 16, 16).
    /// </summary>
    [Fact]
    public void TheSelectionFlipsOverTheAnchor()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();
        Assert.Equal(3, tree.Field.CaretIndex);

        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.Shift));
        tree.Context.DrainEvents();
        AssertSelection(tree.Field, 3, 5);

        for (int i = 0; i < 4; i++)
        {
            tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        }

        RecordingRenderer renderer = tree.Frame();
        Assert.Equal(3, tree.Field.AnchorIndex);
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(new float4(8f, 0f, 16f, 16f), renderer.Calls[IndexOfRectWithColor(renderer, SelectionColor(tree))].Rect);
    }

    /// <summary>
    /// The selection rect comes before the text call. With no focus, the frame has no selection rect and no caret rect.
    /// The selection stays, and it draws again at the next focus gain.
    /// </summary>
    [Fact]
    public void TheSelectionDrawsBehindTheTextOnlyWithFocus()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();
        tree.Context.PushEvent(Down(WidgetInput.HomeKey, UiModifiers.Shift));
        RecordingRenderer renderer = tree.Frame();

        Rgba8 selectionColor = SelectionColor(tree);
        int selection = IndexOfRectWithColor(renderer, selectionColor);
        int text = TextFieldDraws.IndexOfText(renderer, "hello"u8);
        Assert.True(selection < text);
        Assert.Equal(new float4(0f, 0f, 40f, 16f), renderer.Calls[selection].Rect);
        Assert.Equal(2, TextFieldDraws.CountRects(renderer));
        Assert.Equal(renderer.Calls[text].Color.A, tree.Field.Style.Color.A);
        Assert.NotEqual(selectionColor.A, renderer.Calls[text].Color.A);

        tree.Context.SetFocus(tree.Other);
        renderer = tree.Frame();
        Assert.Equal(0, TextFieldDraws.CountRects(renderer));
        TextFieldDraws.IndexOfText(renderer, "hello"u8);
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(5, tree.Field.AnchorIndex);

        tree.Context.SetFocus(tree.Field);
        renderer = tree.Frame();
        Assert.Equal(new float4(0f, 0f, 40f, 16f), renderer.Calls[IndexOfRectWithColor(renderer, selectionColor)].Rect);
    }

    /// <summary>
    /// After 12 chars, the text scrolls by 16. Shift with Left two times selects the last two bytes. The selection rect
    /// is at (64, 0, 16, 16), and the text stays at (-16, 0).
    /// </summary>
    [Fact]
    public void TheSelectionFollowsTheScroll()
    {
        TextFieldTree tree = new();
        tree.Focus();
        tree.Type("aaaaaaaaaaaa");
        tree.Frame();
        Assert.Equal(16f, tree.Field.TextScroll);

        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey, UiModifiers.Shift));
        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(10, tree.Field.CaretIndex);
        Assert.Equal(12, tree.Field.AnchorIndex);
        Assert.Equal(16f, tree.Field.TextScroll);
        int text = TextFieldDraws.IndexOfText(renderer, "aaaaaaaaaaaa"u8);
        int selection = IndexOfRectWithColor(renderer, SelectionColor(tree));
        Assert.Equal(new float2(-16f, 0f), renderer.Calls[text].Position);
        Assert.Equal(new float4(64f, 0f, 16f, 16f), renderer.Calls[selection].Rect);
        Assert.True(selection < text);
    }

    /// <summary>
    /// A <c>SetText</c> that changes the text puts the anchor and the caret at the end, so the frame has no selection
    /// rect. It calls nothing.
    /// </summary>
    [Fact]
    public void SetTextClearsTheSelection()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        SelectOneToThree(tree);
        tree.Field.SetText("new"u8);
        RecordingRenderer renderer = tree.Frame();

        Assert.True(tree.Field.Text.SequenceEqual("new"u8));
        Assert.Equal(3, tree.Field.AnchorIndex);
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(0, CountRectsWithColor(renderer, SelectionColor(tree)));
        Assert.Equal(1, TextFieldDraws.CountRects(renderer));
        Assert.Equal(0, calls);
    }

    /// <summary>Creates a key down with no target, the scan code 0, and no repeat.</summary>
    /// <param name="key">The key code.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Down(int key, UiModifiers modifiers)
    {
        return PointerTree.Key(UiEventType.KeyDown, key, modifiers);
    }

    /// <summary>
    /// Makes the selection from 1 to 3 in a field that holds at least 3 bytes: Home, Right, and Shift with Right two
    /// times. The anchor is at 1, and the caret is at 3.
    /// </summary>
    /// <param name="tree">The tree. The field holds the focus.</param>
    private static void SelectOneToThree(TextFieldTree tree)
    {
        tree.Context.PushEvent(Down(WidgetInput.HomeKey, UiModifiers.None));
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.None));
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.Shift));
        tree.Context.PushEvent(Down(WidgetInput.RightKey, UiModifiers.Shift));
        tree.Context.DrainEvents();
        Assert.Equal(1, tree.Field.AnchorIndex);
        Assert.Equal(3, tree.Field.CaretIndex);
    }

    /// <summary>Checks the selection range of a field: the smaller and the larger of the anchor and the caret.</summary>
    /// <param name="field">The field.</param>
    /// <param name="start">The expected start.</param>
    /// <param name="end">The expected end.</param>
    private static void AssertSelection(TextField field, int start, int end)
    {
        Assert.Equal(start, Math.Min(field.AnchorIndex, field.CaretIndex));
        Assert.Equal(end, Math.Max(field.AnchorIndex, field.CaretIndex));
    }

    /// <summary>Gives the selection color of the field at the opacity 1.</summary>
    /// <param name="tree">The tree. The style is current.</param>
    /// <returns>The color.</returns>
    private static Rgba8 SelectionColor(TextFieldTree tree)
    {
        return tree.Field.Style.Color.ScaleAlpha(TextField.SelectionAlpha);
    }

    /// <summary>Gives the count of the rect calls of the last frame with a color.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The color.</param>
    /// <returns>The count.</returns>
    private static int CountRectsWithColor(RecordingRenderer renderer, Rgba8 color)
    {
        int count = 0;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect && calls[i].Color.Equals(color))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Gives the index of the only rect call of the last frame with a color. The search fails for 0 or 2.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The color.</param>
    /// <returns>The index among all calls.</returns>
    private static int IndexOfRectWithColor(RecordingRenderer renderer, Rgba8 color)
    {
        int found = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect && calls[i].Color.Equals(color))
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return found;
    }
}
