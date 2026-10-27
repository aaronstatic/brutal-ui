using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the disabled rule of <see cref="TextField"/> (Decision 14): a field that holds
/// <see cref="PseudoStates.Disabled"/> takes no char, no key, and no press, it does not stop the walk, it loses the
/// focus at the next update, and it draws no caret. A set from code still works.
/// </summary>
/// <remarks>
/// The drain drops a disabled focus owner, so the char and key tests push their events with the field as the caller
/// target. The scene is <see cref="TextFieldTree"/>. The tests intern no class, so they share no class id.
/// </remarks>
public sealed class TextFieldDisabledTests
{
    /// <summary>
    /// A char with the disabled field as the caller target changes nothing and calls nothing. The root gets the char,
    /// because the field does not stop the walk.
    /// </summary>
    [Fact]
    public void ADisabledFieldTakesNoChar()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Field.States |= PseudoStates.Disabled;
        tree.Settle();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;
        int rootChars = 0;
        tree.Root.OnChar = (ref CharEvent e) => rootChars++;

        tree.Context.PushEvent(CharTo(tree.Field, 'X'));
        Assert.Equal(1, tree.Context.DrainEvents());

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: char X on a disabled field, length = " + tree.Field.Text.Length + ", calls = " + calls
            + ", root chars = " + rootChars);

        Assert.True(tree.Field.Text.SequenceEqual("abc"u8));
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootChars);
    }

    /// <summary>
    /// Left, Backspace, and Enter with the disabled field as the caller target change nothing and call nothing. The root
    /// gets each key, because the field does not stop the walk.
    /// </summary>
    [Fact]
    public void ADisabledFieldTakesNoKey()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Field.States |= PseudoStates.Disabled;
        tree.Settle();

        int changes = 0;
        int submits = 0;
        tree.Field.TextChanged = f => changes++;
        tree.Field.Submitted = f => submits++;
        int rootKeys = 0;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootKeys++;

        tree.Context.PushEvent(KeyTo(tree.Field, WidgetInput.LeftKey));
        tree.Context.PushEvent(KeyTo(tree.Field, WidgetInput.BackspaceKey));
        tree.Context.PushEvent(KeyTo(tree.Field, WidgetInput.EnterKey));
        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.True(tree.Field.Text.SequenceEqual("abc"u8));
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(3, tree.Field.AnchorIndex);
        Assert.Equal(0, changes);
        Assert.Equal(0, submits);
        Assert.Equal(3, rootKeys);
    }

    /// <summary>
    /// A disable drops the focus at the next update: <see cref="UiContext.FocusedElement"/> is <see langword="null"/>, and
    /// the frame holds the text but no caret rect. The caret returns with the focus.
    /// </summary>
    [Fact]
    public void ADisableDropsTheFocusAndTheCaret()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Focus();
        Assert.Same(tree.Field, tree.Context.FocusedElement);
        Assert.Equal(1, TextFieldDraws.CountRects(tree.Frame()));

        tree.Field.States |= PseudoStates.Disabled;
        tree.Context.Update(0f);

        Assert.Null(tree.Context.FocusedElement);

        tree.Context.Draw(tree.Renderer);
        Assert.Equal(0, TextFieldDraws.CountRects(tree.Renderer));
        Assert.True(TextFieldDraws.IndexOfText(tree.Renderer, "abc"u8) >= 0);

        tree.Field.States &= ~PseudoStates.Disabled;
        tree.Context.SetFocus(tree.Field);
        Assert.Equal(1, TextFieldDraws.CountRects(tree.Frame()));
    }

    /// <summary>
    /// A left press, a drag move, and a release on a disabled field give no focus, no caret move, and no selection. The
    /// root gets the press, because the field does not stop the walk.
    /// </summary>
    [Fact]
    public void APressOnADisabledFieldGivesNoFocus()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "hello" });
        tree.Field.States |= PseudoStates.Disabled;
        tree.Settle();
        Assert.Equal(5, tree.Field.CaretIndex);

        int rootDowns = 0;
        tree.Root.OnPointerDown = (ref PointerEvent e) => rootDowns++;
        int otherMoves = 0;
        tree.Other.OnPointerMove = (ref PointerEvent e) => otherMoves++;

        // The press sets Active on the disabled field, but the field does not capture moves. The move over the field
        // does not reach Other. The move over Other does reach Other, because no capture holds the move.
        tree.Context.PushEvent(PointerTree.Down(new float2(21f, 8f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(37f, 8f)));
        tree.Context.PushEvent(PointerTree.Move(TextFieldTree.OverOther));
        tree.Context.PushEvent(PointerTree.Up(TextFieldTree.OverOther, PointerButton.Left));
        RecordingRenderer renderer = tree.Frame();

        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(5, tree.Field.CaretIndex);
        Assert.Equal(5, tree.Field.AnchorIndex);
        Assert.Equal(0, TextFieldDraws.CountRects(renderer));
        Assert.Equal(1, rootDowns);
        Assert.Equal(1, otherMoves);

        // An enabled field takes a press, and a root release handler of the same drain disables the field. The field
        // has no release handler, so the release bubbles to the root. The caret pass maps no press of a disabled field.
        tree.Field.States &= ~PseudoStates.Disabled;
        tree.Settle();
        tree.Root.OnPointerUp = (ref PointerEvent e) => tree.Field.States |= PseudoStates.Disabled;
        tree.Context.PushEvent(PointerTree.Down(new float2(21f, 8f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Up(new float2(21f, 8f), PointerButton.Left));
        tree.Settle();

        Assert.True((tree.Field.States & PseudoStates.Disabled) != 0);
        Assert.Equal(5, tree.Field.CaretIndex);
        Assert.Equal(5, tree.Field.AnchorIndex);
    }

    /// <summary>
    /// <see cref="TextField.SetText(ReadOnlySpan{byte})"/> on a disabled field changes the text and the caret, and it
    /// calls <see cref="TextField.TextChanged"/> not at all.
    /// </summary>
    [Fact]
    public void ACodeSetStillWorks()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "abc" });
        tree.Field.States |= PseudoStates.Disabled;
        tree.Settle();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Field.SetText("hello"u8);

        Assert.True(tree.Field.Text.SequenceEqual("hello"u8));
        Assert.Equal(5, tree.Field.CaretIndex);
        Assert.Equal(0, calls);
        Assert.Equal(PseudoStates.Disabled, tree.Field.States & PseudoStates.Disabled);

        RecordingRenderer renderer = tree.Frame();
        Assert.True(TextFieldDraws.IndexOfText(renderer, "hello"u8) >= 0);
    }

    private static CharEvent CharTo(Element target, uint codePoint)
    {
        return new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, 0d, target),
            CodePoint = codePoint,
        };
    }

    private static KeyEvent KeyTo(Element target, int key)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, target),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }
}
