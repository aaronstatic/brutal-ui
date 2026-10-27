using System;
using System.Text;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the pointer of <see cref="TextField"/> (phase 6): a left press places the caret at the nearest code point
/// boundary, a drag selects from the press point, <c>Shift</c> with a press extends the selection, and the caret pass
/// maps the pointer x with the measurer.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>, unless a test names another scene. Each byte of the text is 8 pixels wide,
/// and the field has no padding, so a text x equals the viewport x plus the text scroll. Each test pushes pointer events
/// with no caller target, so the drain routes them and gives the click focus. Each press has its release, so no test
/// leaves a button down. The class interns no class name.
/// </remarks>
public sealed class TextFieldPointerTests
{
    /// <summary>The y of each pointer event: the middle of the field.</summary>
    private const float FieldY = 8f;

    /// <summary>
    /// The demo of phase 6. The field holds <c>hello</c> and no focus. A left press at (21, 8) and a move to (37, 8) in
    /// one drain give the focus to the field, the anchor at 3, and the caret at 5. The frame draws the selection rect at
    /// (24, 0, 16, 16), and nothing calls <see cref="TextField.TextChanged"/>.
    /// </summary>
    [Fact]
    public void APressAndADragSelect()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Settle();
        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Context.PushEvent(PointerTree.Down(new float2(21f, FieldY), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(37f, FieldY)));
        RecordingRenderer renderer = tree.Frame();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press at 21 and move to 37 on hello, focus = " + tree.Context.FocusedElement?.Name + ", anchor = "
            + tree.Field.AnchorIndex + ", caret = " + tree.Field.CaretIndex + ", calls = " + calls);

        Assert.Same(tree.Field, tree.Context.FocusedElement);
        Assert.Equal(3, tree.Field.AnchorIndex);
        Assert.Equal(5, tree.Field.CaretIndex);
        Assert.Equal(new float4(24f, 0f, 16f, 16f), renderer.Calls[IndexOfRectWithColor(renderer, SelectionColor(tree))].Rect);
        Assert.Equal(0, calls);
        Assert.True(tree.Field.Text.SequenceEqual("hello"u8));

        tree.Context.PushEvent(PointerTree.Up(new float2(37f, FieldY), PointerButton.Left));
        tree.Settle();
    }

    /// <summary>
    /// In <c>hello</c>, the boundaries 1 and 2 sit at x 8 and 16. A press at x 11 gives 1, a press at x 13 gives 2, and
    /// a press at x 12, a tie, gives the lower index 1. Each press collapses the anchor to the caret.
    /// </summary>
    [Fact]
    public void APressPlacesTheCaretAtTheNearestBoundary()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        Click(tree, 11f);
        AssertCaret(tree.Field, 1);

        Click(tree, 13f);
        AssertCaret(tree.Field, 2);

        Click(tree, 12f);
        AssertCaret(tree.Field, 1);
    }

    /// <summary>
    /// In <c>hello</c>, the text ends at x 40. A press at x 80, inside the field and after the text, gives the end 5. A
    /// press at x 0 gives 0.
    /// </summary>
    [Fact]
    public void APressOutsideTheTextGoesToAnEnd()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        Click(tree, 0f);
        AssertCaret(tree.Field, 0);

        Click(tree, 80f);
        AssertCaret(tree.Field, 5);

        Click(tree, 0f);
        AssertCaret(tree.Field, 0);
    }

    /// <summary>
    /// In <c>a€b</c>, the euro sign takes 3 bytes, so the boundaries 0, 1, 4, and 5 sit at x 0, 8, 32, and 40. A press at
    /// x 19 gives 1, and a press at x 21 gives 4. The caret never lands inside the euro sign.
    /// </summary>
    [Fact]
    public void APressStepsOverAWholeCodePoint()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "a€b" });
        tree.Focus();
        Assert.Equal(5, tree.Field.Text.Length);

        Click(tree, 19f);
        AssertCaret(tree.Field, 1);

        Click(tree, 21f);
        AssertCaret(tree.Field, 4);
    }

    /// <summary>
    /// In <c>hello</c>, Home and Right put the caret and the anchor at 1. A press with <c>Shift</c> at x 37 moves the
    /// caret to 5 and keeps the anchor at 1.
    /// </summary>
    [Fact]
    public void ShiftWithAPressExtendsTheSelection()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.HomeKey, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.RightKey, UiModifiers.None));
        tree.Settle();
        AssertCaret(tree.Field, 1);

        float2 p = new(37f, FieldY);
        tree.Context.PushEvent(PointerTree.Down(p, PointerButton.Left) with { Modifiers = UiModifiers.Shift });
        tree.Context.PushEvent(PointerTree.Up(p, PointerButton.Left));
        tree.Settle();

        Assert.Equal(1, tree.Field.AnchorIndex);
        Assert.Equal(5, tree.Field.CaretIndex);
    }

    /// <summary>
    /// A field with 12 bytes and the caret at the end has the text scroll 16. A press at x 3 reads the text x 19, so the
    /// caret goes to 2 (x 16). The caret stays in the view, so the text scroll stays 16.
    /// </summary>
    [Fact]
    public void APressInAScrolledFieldReadsTheScroll()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abcdefghijkl" });
        tree.Focus();
        tree.Settle();
        Assert.Equal(16f, tree.Field.TextScroll);

        Click(tree, 3f);

        AssertCaret(tree.Field, 2);
        Assert.Equal(16f, tree.Field.TextScroll);

        // A padded step: the padding 4 and the border 1 move the content box to x 5. The text is 40 pixels wide, so the
        // field does not scroll. A press at x 30 reads the text x 25, so the caret goes to 3 (x 24). A map from the border
        // box reads the text x 30 and gives 4 (x 32). A press at x 26 would give 3 for both maps, so the test avoids it.
        tree.Field.Inline.Width = 91;
        tree.Field.Inline.Height = 26;
        tree.Field.Inline.Padding = LengthEdges.All(4);
        tree.Field.Inline.BorderWidth = LengthEdges.All(1);
        tree.Field.SetText("hello"u8);
        tree.Settle();
        Assert.Equal(0f, tree.Field.TextScroll);

        Click(tree, 30f);

        AssertCaret(tree.Field, 3);
        Assert.Equal(0f, tree.Field.TextScroll);
    }

    /// <summary>
    /// A field with 12 bytes and the caret at 0 has the text scroll 0. A press at x 2 and a move to x 200, outside the
    /// field, give the anchor 0 and the caret 12. The capture keeps the drag on the field, and the scroll rule scrolls
    /// to the caret: the text scroll is 16.
    /// </summary>
    [Fact]
    public void ADragPastTheRightEdgeScrolls()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abcdefghijkl" });
        tree.Focus();
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.HomeKey, UiModifiers.None));
        tree.Context.Update(0f);
        Assert.Equal(0f, tree.Field.TextScroll);

        tree.Context.PushEvent(PointerTree.Down(new float2(2f, FieldY), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(200f, FieldY)));
        tree.Settle();

        Assert.Equal(0, tree.Field.AnchorIndex);
        Assert.Equal(12, tree.Field.CaretIndex);
        Assert.Equal(16f, tree.Field.TextScroll);

        tree.Context.PushEvent(PointerTree.Up(new float2(200f, FieldY), PointerButton.Left));
        tree.Settle();
        Assert.Equal(0, tree.Field.AnchorIndex);
        Assert.Equal(12, tree.Field.CaretIndex);
    }

    /// <summary>
    /// The field shows a hidden caret, and <see cref="TextFieldTree.Other"/> holds the focus. A press on the field gives
    /// the focus to the field, moves the caret, and shows the caret after the update.
    /// </summary>
    [Fact]
    public void APressFocusesTheFieldAndShowsTheCaret()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();
        tree.Context.Update(TextField.CaretBlinkInterval + 0.1f);
        Assert.False(tree.Field.CaretVisible);
        tree.Context.SetFocus(tree.Other);
        Assert.Same(tree.Other, tree.Context.FocusedElement);

        Click(tree, 11f);

        Assert.Same(tree.Field, tree.Context.FocusedElement);
        AssertCaret(tree.Field, 1);
        Assert.True(tree.Field.CaretVisible);

        // The later input wins: a press, its release, and a Home key in one drain end with the caret at 0.
        float2 p = new(21f, FieldY);
        tree.Context.PushEvent(PointerTree.Down(p, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(p, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.HomeKey, UiModifiers.None));
        tree.Settle();
        AssertCaret(tree.Field, 0);

        // A press that maps to the current caret stays no longer pending. A font change then keeps the caret at 2.
        Click(tree, 16f);
        AssertCaret(tree.Field, 2);
        Click(tree, 16f);
        AssertCaret(tree.Field, 2);
        tree.Field.Inline.FontSize = 32f;
        tree.Settle();
        AssertCaret(tree.Field, 2);
    }

    /// <summary>
    /// A right press gives no press target, so it places no caret. A left press during the right press is a chord, so
    /// it places no caret either, and a move during the chord selects nothing.
    /// </summary>
    [Fact]
    public void ARightPressAndAChordPlaceNothing()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Focus();

        float2 p = new(11f, FieldY);
        tree.Context.PushEvent(PointerTree.Down(p, PointerButton.Right));
        tree.Settle();
        AssertCaret(tree.Field, 5);

        tree.Context.PushEvent(PointerTree.Down(p, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(21f, FieldY)));
        tree.Settle();
        AssertCaret(tree.Field, 5);
        Assert.Same(tree.Field, tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Up(p, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(p, PointerButton.Right));
        tree.Settle();
        AssertCaret(tree.Field, 5);
    }

    /// <summary>
    /// A field in a context with no measurer gets the focus from a press, and the press does not throw. The caret pass
    /// drops the pointer x, so the caret stays at the end.
    /// </summary>
    [Fact]
    public void AContextWithNoMeasurerKeepsTheCaret()
    {
        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        Element root = new() { Name = "root" };
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        TextField field = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" }) { Name = "field" };
        field.Inline.Width = 81;
        field.Inline.Height = 16;
        field.Inline.FontSize = 16;
        root.Children.Add(field);
        context.AddRoot(root);
        context.Update(0f);

        float2 p = new(11f, FieldY);
        context.PushEvent(PointerTree.Down(p, PointerButton.Left));
        context.PushEvent(PointerTree.Move(new float2(21f, FieldY)));
        context.PushEvent(PointerTree.Up(p, PointerButton.Left));
        context.Update(0f);

        Assert.Same(field, context.FocusedElement);
        AssertCaret(field, 5);
        Assert.Equal(0f, field.TextScroll);
    }

    /// <summary>Pushes a left press and its release at an x on the field, and runs one update with a time step of 0.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="x">The x position in viewport pixels.</param>
    private static void Click(TextFieldTree tree, float x)
    {
        float2 p = new(x, FieldY);
        tree.Context.PushEvent(PointerTree.Down(p, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(p, PointerButton.Left));
        tree.Settle();
    }

    /// <summary>Checks the caret of a field and checks that the anchor is at the caret, so there is no selection.</summary>
    /// <param name="field">The field.</param>
    /// <param name="index">The expected byte index of the caret.</param>
    private static void AssertCaret(TextField field, int index)
    {
        Assert.Equal(index, field.CaretIndex);
        Assert.Equal(index, field.AnchorIndex);
    }

    /// <summary>Gives the selection color of the field at the opacity 1.</summary>
    /// <param name="tree">The tree. The style is current.</param>
    /// <returns>The color.</returns>
    private static Rgba8 SelectionColor(TextFieldTree tree)
    {
        return tree.Field.Style.Color.ScaleAlpha(TextField.SelectionAlpha);
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
