using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the text scroll of <see cref="TextField"/> (phase 4): the caret pass of <see cref="UiContext.Update(float)"/>
/// moves the text by the smallest distance that keeps the caret visible, and it clamps the scroll to the text.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>. Each byte is 8 pixels wide, and the content box of the field is 81 pixels
/// wide, so the caret stays in [0, 80] of the content box. Each frame runs <see cref="TextFieldTree.Frame"/>, so the
/// time step is 0 and the caret stays in the shown phase. The class interns no class name.
/// </remarks>
public sealed class TextFieldScrollTests
{
    /// <summary>The clip of the field: its padding box.</summary>
    private static readonly float4 FieldClip = new(0f, 0f, 81f, 16f);

    /// <summary>
    /// The demo of phase 4. The focused field gets 12 chars <c>a</c>. The text of 96 pixels scrolls by 16, so the text
    /// call is at (-16, 0) with 12 bytes, and the caret rect is at (80, 0, 1, 16). Both come after the push of the clip
    /// (0, 0, 81, 16) and before its pop.
    /// </summary>
    [Fact]
    public void LongTextScrollsUnderTheCaret()
    {
        TextFieldTree tree = TypedTree();
        RecordingRenderer renderer = tree.Frame();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: 12 chars, TextScroll = " + tree.Field.TextScroll + ", caret = " + tree.Field.CaretIndex);

        Assert.Equal(12, tree.Field.Text.Length);
        Assert.Equal(16f, tree.Field.TextScroll);

        int push = TextFieldDraws.IndexOfClip(renderer, FieldClip);
        int pop = TextFieldDraws.IndexOfPop(renderer, push);
        int text = TextFieldDraws.IndexOfText(renderer, "aaaaaaaaaaaa"u8);
        int caret = TextFieldDraws.IndexOfOnlyRect(renderer);
        Assert.Equal(new float2(-16f, 0f), renderer.Calls[text].Position);
        Assert.Equal(16f, renderer.Calls[text].Size);
        Assert.Equal(new float4(80f, 0f, 1f, 16f), renderer.Calls[caret].Rect);
        Assert.True(push < text);
        Assert.True(text < caret);
        Assert.True(caret < pop);
    }

    /// <summary>Home on the scrolled field moves the text back to its start and the caret to x 0.</summary>
    [Fact]
    public void HomeScrollsBackToTheStart()
    {
        TextFieldTree tree = TypedTree();
        tree.Frame();

        tree.Context.PushEvent(Down(WidgetInput.HomeKey));
        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(0f, tree.Field.TextScroll);
        AssertTextAndCaret(renderer, new float2(0f, 0f), new float4(0f, 0f, 1f, 16f));
    }

    /// <summary>A caret move that stays inside the view does not move the text.</summary>
    [Fact]
    public void AMoveInsideTheViewDoesNotScroll()
    {
        TextFieldTree tree = TypedTree();
        tree.Frame();

        for (int i = 0; i < 3; i++)
        {
            tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        }

        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(9, tree.Field.CaretIndex);
        Assert.Equal(16f, tree.Field.TextScroll);
        AssertTextAndCaret(renderer, new float2(-16f, 0f), new float4(56f, 0f, 1f, 16f));
    }

    /// <summary>
    /// A caret move past the left edge of the view moves the text by the smallest distance: the caret sits at x 0.
    /// </summary>
    [Fact]
    public void AMovePastTheLeftEdgeScrolls()
    {
        TextFieldTree tree = TypedTree();
        tree.Frame();

        tree.Context.PushEvent(Down(WidgetInput.HomeKey));
        tree.Frame();
        Assert.Equal(0f, tree.Field.TextScroll);

        tree.Context.PushEvent(Down(WidgetInput.EndKey));
        tree.Frame();
        Assert.Equal(16f, tree.Field.TextScroll);

        for (int i = 0; i < 11; i++)
        {
            tree.Context.PushEvent(Down(WidgetInput.LeftKey));
        }

        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(8f, tree.Field.TextScroll);
        AssertTextAndCaret(renderer, new float2(-8f, 0f), new float4(0f, 0f, 1f, 16f));

        // A move right inside a long text moves the text by the smallest distance: the caret sits at x 80, not at the
        // left edge and not at the upper bound. Fourteen chars give the upper bound 32. Eleven steps right put the caret
        // at x 88, so the scroll is 88 - 80 = 8.
        tree.Field.SetText("aaaaaaaaaaaaaa"u8);
        tree.Frame();
        Assert.Equal(32f, tree.Field.TextScroll);

        tree.Context.PushEvent(Down(WidgetInput.HomeKey));
        tree.Frame();
        Assert.Equal(0f, tree.Field.TextScroll);

        for (int i = 0; i < 11; i++)
        {
            tree.Context.PushEvent(Down(WidgetInput.RightKey));
        }

        renderer = tree.Frame();

        Assert.Equal(11, tree.Field.CaretIndex);
        Assert.Equal(8f, tree.Field.TextScroll);
        AssertTextAndCaret(renderer, new float2(-8f, 0f), new float4(80f, 0f, 1f, 16f));
    }

    /// <summary>
    /// A Backspace at the end of the scrolled text pulls the text back, so no empty space shows after the text.
    /// </summary>
    [Fact]
    public void ARemoveAtTheEndPullsTheTextBack()
    {
        TextFieldTree tree = TypedTree();
        tree.Frame();

        tree.Context.PushEvent(Down(WidgetInput.BackspaceKey));
        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(11, tree.Field.Text.Length);
        Assert.Equal(8f, tree.Field.TextScroll);
        AssertTextAndCaret(renderer, new float2(-8f, 0f), new float4(80f, 0f, 1f, 16f));
    }

    /// <summary>A text of 10 bytes and the caret fill the field exactly, so the text does not scroll.</summary>
    [Fact]
    public void TextThatFitsDoesNotScroll()
    {
        TextFieldTree tree = new();
        tree.Focus();
        tree.Type("aaaaaaaaaa");
        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(10, tree.Field.Text.Length);
        Assert.Equal(0f, tree.Field.TextScroll);
        AssertTextAndCaret(renderer, new float2(0f, 0f), new float4(80f, 0f, 1f, 16f));
    }

    /// <summary>
    /// A resize of the field runs the scroll rule against the new content width in the same update, so the caret stays
    /// visible.
    /// </summary>
    [Fact]
    public void AResizeKeepsTheCaretVisible()
    {
        TextFieldTree tree = TypedTree();
        tree.Frame();
        Assert.Equal(16f, tree.Field.TextScroll);

        tree.Field.Inline.Width = 41;
        tree.Context.Update(0f);
        tree.Context.Draw(tree.Renderer);

        Assert.Equal(41f, tree.Field.Layout.Rect.Z);
        Assert.Equal(56f, tree.Field.TextScroll);
        int caret = TextFieldDraws.IndexOfOnlyRect(tree.Renderer);
        Assert.Equal(new float4(40f, 0f, 1f, 16f), tree.Renderer.Calls[caret].Rect);

        // A hide of the focused field gives a content width of 0. The field keeps its scroll, and the focus drops at the
        // next drain. A show thus draws the text with the old scroll.
        tree.Field.Inline.Display = Display.None;
        tree.Frame();
        tree.Frame();
        Assert.Null(tree.Context.FocusedElement);
        tree.Field.Inline.Display = Display.Flex;
        tree.Frame();
        Assert.Equal(41f, tree.Field.Layout.Rect.Z);
        Assert.Equal(56f, tree.Field.TextScroll);
    }

    /// <summary>
    /// A <c>SetText</c> that changes the text sets the scroll to 0 at once. A field that is not the focus owner keeps its
    /// scroll through an update. The next update with the focus scrolls to the caret at the end.
    /// </summary>
    [Fact]
    public void SetTextAndTheFocusOwnerRule()
    {
        TextFieldTree tree = TypedTree();
        tree.Frame();
        Assert.Equal(16f, tree.Field.TextScroll);

        // A SetText on the focused field gives the scroll 0 at once, before any update.
        tree.Field.SetText("bbbbbbbbbbbb"u8);
        Assert.Equal(0f, tree.Field.TextScroll);
        Assert.Equal(12, tree.Field.CaretIndex);

        // The next update with the focus scrolls to the caret at the end.
        tree.Frame();
        Assert.Equal(16f, tree.Field.TextScroll);

        // A field that is not the focus owner keeps its scroll through an update, also after a resize.
        tree.Context.ClearFocus();
        tree.Field.Inline.Width = 41;
        tree.Frame();
        Assert.Equal(16f, tree.Field.TextScroll);

        // A SetText on a field that is not the focus owner gives the scroll 0, and an update keeps it.
        tree.Field.SetText("cccccccccccc"u8);
        Assert.Equal(0f, tree.Field.TextScroll);
        tree.Frame();
        Assert.Equal(0f, tree.Field.TextScroll);

        // The next update with the focus scrolls to the caret at the end: 96 - (41 - 1).
        Assert.True(tree.Context.SetFocus(tree.Field));
        RecordingRenderer renderer = tree.Frame();
        Assert.Equal(56f, tree.Field.TextScroll);
        int caret = TextFieldDraws.IndexOfOnlyRect(renderer);
        Assert.Equal(new float4(40f, 0f, 1f, 16f), renderer.Calls[caret].Rect);
    }

    /// <summary>
    /// The padding and the border move the content box. A field of 91 x 26 with a padding of 4 and a border of 1 has a
    /// content box of 81 x 16 at (5, 5) and the clip (1, 1, 89, 24). Twelve chars scroll by 16, so the text is at
    /// (-11, 5) and the caret at (85, 5, 1, 16).
    /// </summary>
    [Fact]
    public void ThePaddingAndTheBorderCount()
    {
        TextFieldTree tree = new();
        tree.Field.Inline.Width = 91;
        tree.Field.Inline.Height = 26;
        tree.Field.Inline.Padding = LengthEdges.All(4);
        tree.Field.Inline.BorderWidth = LengthEdges.All(1);
        tree.Focus();
        tree.Type("aaaaaaaaaaaa");
        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(16f, tree.Field.TextScroll);
        int push = TextFieldDraws.IndexOfClip(renderer, new float4(1f, 1f, 89f, 24f));
        int pop = TextFieldDraws.IndexOfPop(renderer, push);
        int text = TextFieldDraws.IndexOfText(renderer, "aaaaaaaaaaaa"u8);
        int caret = TextFieldDraws.IndexOfOnlyRect(renderer);
        Assert.Equal(new float2(-11f, 5f), renderer.Calls[text].Position);
        Assert.Equal(new float4(85f, 5f, 1f, 16f), renderer.Calls[caret].Rect);
        Assert.True(push < text);
        Assert.True(text < caret);
        Assert.True(caret < pop);
    }

    /// <summary>Creates the tree, focuses the field, and queues 12 chars <c>a</c>. The call runs no frame after the type.</summary>
    /// <returns>The tree.</returns>
    private static TextFieldTree TypedTree()
    {
        TextFieldTree tree = new();
        tree.Focus();
        tree.Type("aaaaaaaaaaaa");
        return tree;
    }

    /// <summary>Checks the position of the only text call and the rect of the only caret rect of the last frame.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="text">The expected top left corner of the text.</param>
    /// <param name="caret">The expected caret rect.</param>
    private static void AssertTextAndCaret(RecordingRenderer renderer, float2 text, float4 caret)
    {
        int textIndex = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Text)
            {
                Assert.Equal(-1, textIndex);
                textIndex = i;
            }
        }

        Assert.NotEqual(-1, textIndex);
        Assert.Equal(text, calls[textIndex].Position);
        int caretIndex = TextFieldDraws.IndexOfOnlyRect(renderer);
        Assert.Equal(caret, calls[caretIndex].Rect);
    }

    /// <summary>Gives a key down event with no modifier and no caller target.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Down(int key)
    {
        return PointerTree.Key(UiEventType.KeyDown, key, UiModifiers.None);
    }
}
