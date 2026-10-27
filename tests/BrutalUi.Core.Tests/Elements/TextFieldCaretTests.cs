using System;
using System.Text;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the caret keys of <see cref="TextField"/> (phase 2): Left, Right, Home, and End move the caret by code point,
/// a char goes in at the caret, each used key stops the walk, the other keys walk on, and the focused field draws the
/// caret.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>. Each test pushes key events with no caller target, so the key route gives
/// them to the focus owner. The class interns no class name.
/// </remarks>
public sealed class TextFieldCaretTests
{
    /// <summary>The GLFW key code of the up arrow key (<c>GLFW_KEY_UP</c>).</summary>
    private const int UpKey = 265;

    /// <summary>The GLFW key code of the down arrow key (<c>GLFW_KEY_DOWN</c>).</summary>
    private const int DownKey = 264;

    /// <summary>The GLFW key code of the Escape key (<c>GLFW_KEY_ESCAPE</c>).</summary>
    private const int EscapeKey = 256;

    /// <summary>The GLFW key code of the keypad Enter key (<c>GLFW_KEY_KP_ENTER</c>).</summary>
    private const int KeypadEnterKey = 335;

    /// <summary>The GLFW key code of the key A (<c>GLFW_KEY_A</c>).</summary>
    private const int KeyA = 65;

    /// <summary>The clip of the field: its padding box.</summary>
    private static readonly float4 FieldClip = new(0f, 0f, 81f, 16f);

    /// <summary>
    /// The demo of phase 2. The field holds <c>abc</c> and the focus. Left, Left, and the char <c>X</c> give the text
    /// <c>aXbc</c>, one call of <see cref="TextField.TextChanged"/>, and a caret rect at (16, 0, 1, 16) in the text color.
    /// The caret rect comes after the text call and before the pop of the clip.
    /// </summary>
    [Fact]
    public void ACharGoesInAtTheCaret()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.PushEvent(PointerTree.Char('X'));
        RecordingRenderer renderer = tree.Frame();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: Left, Left, X on abc, Text = \"" + Encoding.UTF8.GetString(tree.Field.Text) + "\", calls = " + calls
            + ", caret = " + tree.Field.CaretIndex);

        Assert.True(tree.Field.Text.SequenceEqual("aXbc"u8));
        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.Equal(1, calls);

        int push = TextFieldDraws.IndexOfClip(renderer, FieldClip);
        int pop = TextFieldDraws.IndexOfPop(renderer, push);
        int text = TextFieldDraws.IndexOfText(renderer, "aXbc"u8);
        int caret = TextFieldDraws.IndexOfOnlyRect(renderer);
        DrawCall rect = renderer.Calls[caret];
        Assert.Equal(new float4(16f, 0f, 1f, 16f), rect.Rect);
        Assert.Equal(renderer.Calls[text].Color, rect.Color);
        Assert.True(push < text);
        Assert.True(text < caret);
        Assert.True(caret < pop);
    }

    /// <summary>Home moves the caret to 0, and End moves it to the end. The caret rect follows the caret.</summary>
    [Fact]
    public void HomeAndEndMoveTheCaret()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        tree.Context.PushEvent(Down(WidgetInput.HomeKey));
        RecordingRenderer renderer = tree.Frame();
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(new float4(0f, 0f, 1f, 16f), renderer.Calls[TextFieldDraws.IndexOfOnlyRect(renderer)].Rect);

        tree.Context.PushEvent(Down(WidgetInput.EndKey));
        renderer = tree.Frame();
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(new float4(24f, 0f, 1f, 16f), renderer.Calls[TextFieldDraws.IndexOfOnlyRect(renderer)].Rect);
    }

    /// <summary>
    /// The caret steps over one code point for each key: a code point of 1, 2, 3, and 4 bytes. Left from 10 gives 6, 3,
    /// 1, and 0. Right gives the reverse.
    /// </summary>
    [Fact]
    public void TheCaretStepsOverEachCodePoint()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "aé€😀" });
        Assert.Equal(10, tree.Field.Text.Length);
        Assert.Equal(10, tree.Field.CaretIndex);
        tree.Focus();

        int[] left = [6, 3, 1, 0];
        foreach (int expected in left)
        {
            tree.Context.PushEvent(Down(WidgetInput.LeftKey));
            Assert.Equal(1, tree.Context.DrainEvents());
            Assert.Equal(expected, tree.Field.CaretIndex);
        }

        int[] right = [1, 3, 6, 10];
        foreach (int expected in right)
        {
            tree.Context.PushEvent(Down(WidgetInput.RightKey));
            Assert.Equal(1, tree.Context.DrainEvents());
            Assert.Equal(expected, tree.Field.CaretIndex);
        }

        Assert.True(tree.Field.Text.SequenceEqual("aé€😀"u8));
    }

    /// <summary>
    /// Left at 0 and Right at the end change nothing, mark nothing, and call nothing. The field still stops the walk, so
    /// the bubble handler of the root gets no key.
    /// </summary>
    [Fact]
    public void TheCaretStopsAtEachEnd()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "ab" });
        tree.Focus();
        tree.Settle();

        int calls = 0;
        int rootKeys = 0;
        tree.Field.TextChanged = f => calls++;
        tree.Root.OnKeyDown += (ref KeyEvent e) => rootKeys++;

        tree.Context.PushEvent(Down(WidgetInput.RightKey));
        tree.Context.DrainEvents();
        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        tree.Context.PushEvent(Down(WidgetInput.HomeKey));
        tree.Context.DrainEvents();
        tree.Settle();
        Assert.Equal(0, tree.Field.CaretIndex);

        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.DrainEvents();
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        Assert.True(tree.Field.Text.SequenceEqual("ab"u8));
        Assert.Equal(0, calls);
        Assert.Equal(0, rootKeys);
    }

    /// <summary>
    /// Up, Down, Escape, the keypad Enter key, and the key A walk on to the root and change nothing. A key up walks on.
    /// Tab walks on and moves the focus to <c>Other</c>.
    /// </summary>
    [Fact]
    public void TheOtherKeysWalkOn()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        int calls = 0;
        int rootDowns = 0;
        int rootUps = 0;
        tree.Field.TextChanged = f => calls++;
        tree.Root.OnKeyDown += (ref KeyEvent e) => rootDowns++;
        tree.Root.OnKeyUp += (ref KeyEvent e) => rootUps++;

        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.DrainEvents();
        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.Equal(0, rootDowns);

        int[] keys = [UpKey, DownKey, EscapeKey, KeypadEnterKey, KeyA];
        foreach (int key in keys)
        {
            tree.Context.PushEvent(Down(key));
        }

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyUp, WidgetInput.LeftKey, UiModifiers.None));
        tree.Context.DrainEvents();

        Assert.Equal(keys.Length, rootDowns);
        Assert.Equal(1, rootUps);
        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.True(tree.Field.Text.SequenceEqual("abc"u8));
        Assert.Same(tree.Field, tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Tab(false));
        tree.Context.DrainEvents();

        Assert.Equal(keys.Length + 1, rootDowns);
        Assert.Same(tree.Other, tree.Context.FocusedElement);
        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// <c>Control</c>, <c>Alt</c>, and <c>Super</c> block Left: the caret stays, and the key walks on to the root.
    /// <c>CapsLock</c> and <c>NumLock</c> do not block Left.
    /// </summary>
    [Fact]
    public void TheModifiersBlockTheKey()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        int rootKeys = 0;
        tree.Root.OnKeyDown += (ref KeyEvent e) => rootKeys++;

        UiModifiers[] blocking = [UiModifiers.Control, UiModifiers.Alt, UiModifiers.Super];
        foreach (UiModifiers modifiers in blocking)
        {
            tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, modifiers));
            tree.Context.DrainEvents();
            Assert.Equal(3, tree.Field.CaretIndex);
        }

        Assert.Equal(blocking.Length, rootKeys);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.CapsLock));
        tree.Context.DrainEvents();
        Assert.Equal(2, tree.Field.CaretIndex);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.NumLock));
        tree.Context.DrainEvents();
        Assert.Equal(1, tree.Field.CaretIndex);

        Assert.Equal(blocking.Length, rootKeys);
    }

    /// <summary>A repeat of Left moves the caret again: two Left keys, the second a repeat, give two steps.</summary>
    [Fact]
    public void ARepeatMovesAgain()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();

        KeyEvent repeat = Down(WidgetInput.LeftKey);
        repeat.IsRepeat = true;
        tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        tree.Context.PushEvent(repeat);
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.Equal(1, tree.Field.CaretIndex);
    }

    /// <summary>
    /// A caret move calls no <see cref="TextField.TextChanged"/> and marks <see cref="DirtyFlags.Paint"/>. It marks no
    /// <see cref="DirtyFlags.Layout"/>.
    /// </summary>
    [Fact]
    public void ACaretMoveCallsNothing()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();
        tree.Settle();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        int[] keys = [WidgetInput.LeftKey, WidgetInput.HomeKey, WidgetInput.RightKey, WidgetInput.EndKey];
        int[] carets = [2, 0, 1, 3];
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.Equal(DirtyFlags.None, tree.Field.Dirty & (DirtyFlags.Paint | DirtyFlags.Layout));
            tree.Context.PushEvent(Down(keys[i]));
            tree.Context.DrainEvents();
            Assert.Equal(carets[i], tree.Field.CaretIndex);
            Assert.Equal(DirtyFlags.Paint, tree.Field.Dirty & (DirtyFlags.Paint | DirtyFlags.Layout));
            tree.Settle();
        }

        Assert.Equal(0, calls);
    }

    /// <summary>
    /// The field draws no caret with no focus. With the focus, it draws one rect of width 1 and height 16 in the color
    /// of the style. With the opacity 0.5, the caret alpha is the alpha of the color scaled by 0.5.
    /// </summary>
    [Fact]
    public void TheCaretDrawsOnlyWithFocus()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });

        RecordingRenderer renderer = tree.Frame();
        Assert.Equal(0, TextFieldDraws.CountRects(renderer));

        tree.Context.SetFocus(tree.Field);
        renderer = tree.Frame();
        DrawCall caret = renderer.Calls[TextFieldDraws.IndexOfOnlyRect(renderer)];
        Assert.Equal(1f, caret.Rect.Z);
        Assert.Equal(16f, caret.Rect.W);
        Assert.Equal(tree.Field.Style.Color, caret.Color);

        tree.Field.Inline.Opacity = 0.5f;
        renderer = tree.Frame();
        caret = renderer.Calls[TextFieldDraws.IndexOfOnlyRect(renderer)];
        Rgba8 half = tree.Field.Style.Color.ScaleAlpha(0.5f);
        Assert.NotEqual(tree.Field.Style.Color.A, half.A);
        Assert.Equal(half.A, caret.Color.A);

        tree.Context.ClearFocus();
        renderer = tree.Frame();
        Assert.Equal(0, TextFieldDraws.CountRects(renderer));
    }

    /// <summary>The four new key codes are the GLFW values.</summary>
    [Fact]
    public void TheKeyCodesAreTheGlfwValues()
    {
        Assert.Equal(259, WidgetInput.BackspaceKey);
        Assert.Equal(261, WidgetInput.DeleteKey);
        Assert.Equal(268, WidgetInput.HomeKey);
        Assert.Equal(269, WidgetInput.EndKey);
    }

    /// <summary>Creates a key down with no target, no modifier, and no repeat.</summary>
    /// <param name="key">The key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Down(int key)
    {
        return PointerTree.Key(UiEventType.KeyDown, key, UiModifiers.None);
    }
}

/// <summary>
/// The draw call searches of the phase 2 text field tests. <see cref="TextFieldCaretTests"/> and
/// <see cref="TextFieldDeleteTests"/> share them.
/// </summary>
/// <remarks>
/// <see cref="TextFieldTree"/> has no fill on the root, the field, or <c>Other</c>, so each rect call of a frame is a
/// caret rect.
/// </remarks>
internal static class TextFieldDraws
{
    /// <summary>Gives the count of the rect calls of the last frame.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The count.</returns>
    public static int CountRects(RecordingRenderer renderer)
    {
        int count = 0;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Gives the index of the only rect call of the last frame. The search fails for 0 or 2 rects.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The index among all calls.</returns>
    public static int IndexOfOnlyRect(RecordingRenderer renderer)
    {
        int found = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect)
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return found;
    }

    /// <summary>Gives the index of the only clip push with a rect.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="rect">The clip rect.</param>
    /// <returns>The index among all calls.</returns>
    public static int IndexOfClip(RecordingRenderer renderer, float4 rect)
    {
        int found = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.PushClip && calls[i].Rect.Equals(rect))
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return found;
    }

    /// <summary>Gives the index of the only text call with a text.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="utf8">The UTF-8 text.</param>
    /// <returns>The index among all calls.</returns>
    public static int IndexOfText(RecordingRenderer renderer, ReadOnlySpan<byte> utf8)
    {
        int found = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Text && renderer.GetText(in calls[i]).SequenceEqual(utf8))
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return found;
    }

    /// <summary>Gives the index of the pop that matches a clip push.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="push">The index of the push.</param>
    /// <returns>The index among all calls.</returns>
    public static int IndexOfPop(RecordingRenderer renderer, int push)
    {
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        int depth = 0;
        for (int i = push; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.PushClip)
            {
                depth++;
            }
            else if (calls[i].Kind == DrawCallKind.PopClip)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        Assert.Fail("The clip push has no pop.");
        return -1;
    }
}
