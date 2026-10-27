using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the caret blink of <see cref="TextField"/> (phase 4): the caret pass of
/// <see cref="UiContext.Update(float)"/> advances the blink timer, and an edit, a caret move, and a focus gain show the
/// caret at once.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>, except for the last test. <see cref="TextField.CaretBlinkInterval"/> is
/// 0.5 s, so the caret shows from 0 s to below 0.5 s and hides from 0.5 s to below 1 s. The class interns no class
/// name.
/// </remarks>
public sealed class TextFieldBlinkTests
{
    /// <summary>The caret shows, hides after 0.5 s, and shows again after 1 s.</summary>
    [Fact]
    public void TheCaretBlinks()
    {
        TextFieldTree tree = new();
        tree.Focus();

        Frame(tree, 0f);
        Assert.True(tree.Field.CaretVisible);
        Assert.Equal(1, TextFieldDraws.CountRects(tree.Renderer));

        Frame(tree, 0.5f);
        Assert.False(tree.Field.CaretVisible);
        Assert.Equal(0, TextFieldDraws.CountRects(tree.Renderer));

        // The caret pass runs before the clear pass, so the flip leaves no Paint bit after Update.
        Assert.Equal(DirtyFlags.None, tree.Field.Dirty & DirtyFlags.Paint);

        Frame(tree, 0.5f);
        Assert.True(tree.Field.CaretVisible);
        Assert.Equal(1, TextFieldDraws.CountRects(tree.Renderer));
    }

    /// <summary>A char on a field with a hidden caret shows the caret in the same frame.</summary>
    [Fact]
    public void AnEditRestartsTheBlink()
    {
        TextFieldTree tree = new();
        tree.Focus();
        Frame(tree, 0.5f);
        Assert.False(tree.Field.CaretVisible);

        tree.Type("a");
        Frame(tree, 0f);

        Assert.True(tree.Field.Text.SequenceEqual("a"u8));
        Assert.True(tree.Field.CaretVisible);
        int caret = TextFieldDraws.IndexOfOnlyRect(tree.Renderer);
        Assert.Equal(new float4(8f, 0f, 1f, 16f), tree.Renderer.Calls[caret].Rect);
    }

    /// <summary>A Left key on a field with a hidden caret shows the caret in the same frame.</summary>
    [Fact]
    public void ACaretMoveRestartsTheBlink()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();
        Frame(tree, 0.5f);
        Assert.False(tree.Field.CaretVisible);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None));
        Frame(tree, 0f);

        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.True(tree.Field.CaretVisible);
        int caret = TextFieldDraws.IndexOfOnlyRect(tree.Renderer);
        Assert.Equal(new float4(16f, 0f, 1f, 16f), tree.Renderer.Calls[caret].Rect);
    }

    /// <summary>
    /// A focus gain from code and a focus gain from Tab each show the caret at once, also when the timer of the field
    /// stopped in the hidden phase.
    /// </summary>
    [Fact]
    public void AFocusGainRestartsTheBlink()
    {
        TextFieldTree tree = new();
        tree.Focus();
        Frame(tree, 0.7f);
        Assert.False(tree.Field.CaretVisible);

        tree.Context.ClearFocus();
        Assert.True(tree.Context.SetFocus(tree.Field));
        Frame(tree, 0f);
        Assert.True(tree.Field.CaretVisible);
        Assert.Equal(1, TextFieldDraws.CountRects(tree.Renderer));

        // Hide the caret again, move the focus to Other, and Tab back to the field. The timer of a field that is not
        // the focus owner does not advance, so the field keeps the hidden phase until the focus gain.
        Frame(tree, 0.7f);
        Assert.False(tree.Field.CaretVisible);
        Assert.True(tree.Context.SetFocus(tree.Other));
        Frame(tree, 0f);
        Assert.False(tree.Field.CaretVisible);

        tree.Context.PushEvent(PointerTree.Tab(false));
        Frame(tree, 0f);
        Assert.Same(tree.Field, tree.Context.FocusedElement);
        Assert.True(tree.Field.CaretVisible);
        Assert.Equal(1, TextFieldDraws.CountRects(tree.Renderer));

        // The repair branch restarts nothing: SetFocus on the focused field keeps the hidden caret.
        Frame(tree, 0.7f);
        Assert.False(tree.Field.CaretVisible);
        Assert.True(tree.Context.SetFocus(tree.Field));
        Frame(tree, 0f);
        Assert.Same(tree.Field, tree.Context.FocusedElement);
        Assert.False(tree.Field.CaretVisible);
    }

    /// <summary>A time step of NaN, of -1, or of positive infinity advances no blink.</summary>
    [Fact]
    public void ABadDeltaAdvancesNothing()
    {
        TextFieldTree tree = new();
        tree.Focus();
        Frame(tree, 0f);
        Assert.True(tree.Field.CaretVisible);

        Frame(tree, float.NaN);
        Assert.True(tree.Field.CaretVisible);

        Frame(tree, -1f);
        Assert.True(tree.Field.CaretVisible);

        Frame(tree, float.PositiveInfinity);
        Assert.True(tree.Field.CaretVisible);
        Assert.Equal(1, TextFieldDraws.CountRects(tree.Renderer));

        // The timer still runs after the bad steps.
        Frame(tree, 0.5f);
        Assert.False(tree.Field.CaretVisible);

        // An infinite step in the hidden phase keeps the caret hidden.
        Frame(tree, float.PositiveInfinity);
        Assert.False(tree.Field.CaretVisible);
        Assert.Equal(0, TextFieldDraws.CountRects(tree.Renderer));
    }

    /// <summary>
    /// A field in a context with no measurer updates with no exception. The scroll stays 0 after 12 chars, and the blink
    /// still flips.
    /// </summary>
    [Fact]
    public void AContextWithNoMeasurerStillBlinks()
    {
        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        Element root = new() { Name = "root" };
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        TextField field = new(new TextField.CreateInfo { Capacity = 16 }) { Name = "field" };
        field.Inline.Width = 81;
        field.Inline.Height = 16;
        field.Inline.FontSize = 16;
        root.Children.Add(field);
        context.AddRoot(root);
        context.Update(0f);
        Assert.True(context.SetFocus(field));

        for (int i = 0; i < 12; i++)
        {
            context.PushEvent(PointerTree.Char('a'));
        }

        context.Update(0f);
        Assert.Equal(12, field.Text.Length);
        Assert.Equal(0f, field.TextScroll);
        Assert.True(field.CaretVisible);

        context.Update(0.5f);
        Assert.False(field.CaretVisible);
        Assert.Equal(0f, field.TextScroll);

        context.Update(0.5f);
        Assert.True(field.CaretVisible);
        Assert.Equal(0f, field.TextScroll);
    }

    /// <summary>Runs one update with a time step and one draw into the renderer of the tree.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="deltaTime">The time step in seconds.</param>
    private static void Frame(TextFieldTree tree, float deltaTime)
    {
        tree.Context.Update(deltaTime);
        tree.Context.Draw(tree.Renderer);
    }
}
