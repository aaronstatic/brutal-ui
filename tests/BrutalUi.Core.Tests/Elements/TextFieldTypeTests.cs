using System;
using System.Text;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the char path of <see cref="TextField"/> (phase 1): a char on the focused field inserts the UTF-8 form of the
/// code point at the caret and calls <see cref="TextField.TextChanged"/>. A char that does not fit drops, a control
/// character walks on, and the draw shows the text inside the clip of the field.
/// </summary>
/// <remarks>
/// The scene is <see cref="TextFieldTree"/>. Each test pushes char events with no caller target, so the key route gives
/// them to the focus owner, unless the test names a target. The class interns no class name.
/// </remarks>
public sealed class TextFieldTypeTests
{
    /// <summary>The fill of the type rule of <see cref="ATypeRuleMatchesTheTextField"/>.</summary>
    private static readonly Rgba8 FieldFill = new(30, 90, 150, 255);

    /// <summary>The clip of the field: its padding box.</summary>
    private static readonly float4 FieldClip = new(0f, 0f, 81f, 16f);

    /// <summary>
    /// The demo of phase 1. The field has the focus. The chars <c>H</c> and <c>i</c>, one update, and one draw give the
    /// text <c>Hi</c>, two calls of <see cref="TextField.TextChanged"/> with the field as the sender, and a text call
    /// <c>Hi</c> at (0, 0) with the size 16 inside the clip of the field.
    /// </summary>
    [Fact]
    public void ACharShowsInTheField()
    {
        TextFieldTree tree = new();
        tree.Focus();
        Assert.Same(tree.Field, tree.Context.FocusedElement);

        int calls = 0;
        TextField? sender = null;
        string? seen = null;
        tree.Field.TextChanged = f =>
        {
            calls++;
            sender = f;
            seen = Encoding.UTF8.GetString(f.Text);
        };

        tree.Context.PushEvent(PointerTree.Char('H'));
        tree.Context.PushEvent(PointerTree.Char('i'));
        RecordingRenderer renderer = tree.Frame();

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: H and i on the focused field, Text = \"" + Encoding.UTF8.GetString(tree.Field.Text) + "\", calls = "
            + calls);

        Assert.True(tree.Field.Text.SequenceEqual("Hi"u8));
        Assert.Equal(2, tree.Field.CaretIndex);
        Assert.Equal(2, calls);
        Assert.Same(tree.Field, sender);

        // The delegate reads the text of the last call: the call comes after the insert.
        Assert.Equal("Hi", seen);

        int push = IndexOfClip(renderer, FieldClip);
        int pop = IndexOfPop(renderer, push);
        int text = IndexOfText(renderer, "Hi"u8);
        DrawCall call = renderer.Calls[text];
        Assert.Equal(new float2(0f, 0f), call.Position);
        Assert.Equal(16f, call.Size);
        Assert.Equal(0f, call.WrapWidth);
        Assert.True(push < text);
        Assert.True(text < pop);
    }

    /// <summary>A start text comes first, and a char goes in after it, because the caret starts at the end.</summary>
    [Fact]
    public void TheStartTextComesFirst()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "ab" });
        Assert.Equal(2, tree.Field.CaretIndex);
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Type("c");
        tree.Context.Update(0f);

        Assert.True(tree.Field.Text.SequenceEqual("abc"u8));
        Assert.Equal(3, tree.Field.CaretIndex);
        Assert.Equal(1, calls);
    }

    /// <summary>A code point of two bytes and a code point of four bytes go in as UTF-8, and the caret moves by bytes.</summary>
    [Fact]
    public void AMultiByteCodePointGoesInAsUtf8()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Type("é\U0001F600");
        tree.Context.Update(0f);

        byte[] expected = [0xC3, 0xA9, 0xF0, 0x9F, 0x98, 0x80];
        Assert.Equal(expected, tree.Field.Text.ToArray());
        Assert.Equal(6, tree.Field.CaretIndex);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// A char past the capacity drops: the text stays, and nothing gets a call. The field still stops the walk, so a
    /// bubble handler on the root gets no char.
    /// </summary>
    [Fact]
    public void AnInsertPastTheCapacityDrops()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;
        int rootChars = 0;
        tree.Root.OnChar = (ref CharEvent e) => rootChars++;

        tree.Type(new string('a', 16));
        tree.Context.Update(0f);
        Assert.Equal(16, tree.Field.Text.Length);
        Assert.Equal(16, calls);

        tree.Type("b");
        tree.Context.Update(0f);

        Assert.True(tree.Field.Text.SequenceEqual("aaaaaaaaaaaaaaaa"u8));
        Assert.Equal(16, tree.Field.CaretIndex);
        Assert.Equal(16, calls);
        Assert.Equal(0, rootChars);
    }

    /// <summary>
    /// A code point of three bytes with one free byte drops, and it does not split. A char of one byte then fits.
    /// </summary>
    [Fact]
    public void ACodePointThatDoesNotFitDrops()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;
        int rootChars = 0;
        tree.Root.OnChar = (ref CharEvent e) => rootChars++;

        tree.Type(new string('a', 15));
        tree.Context.Update(0f);
        Assert.Equal(15, calls);

        tree.Type("€");
        tree.Context.Update(0f);
        Assert.True(tree.Field.Text.SequenceEqual("aaaaaaaaaaaaaaa"u8));
        Assert.Equal(15, tree.Field.CaretIndex);
        Assert.Equal(15, calls);

        tree.Type("b");
        tree.Context.Update(0f);
        Assert.True(tree.Field.Text.SequenceEqual("aaaaaaaaaaaaaaab"u8));
        Assert.Equal(16, tree.Field.CaretIndex);
        Assert.Equal(16, calls);
        Assert.Equal(0, rootChars);
    }

    /// <summary>
    /// A control character (C0, DEL, and C1), a surrogate, and a value above U+10FFFF are not text. The field changes
    /// nothing, calls nothing, and the chars walk on to the root.
    /// </summary>
    [Fact]
    public void AControlCharWalksOn()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int calls = 0;
        tree.Field.TextChanged = f => calls++;
        int rootChars = 0;
        tree.Root.OnChar = (ref CharEvent e) => rootChars++;

        uint[] codePoints = [0x0008u, 0x007Fu, 0x0085u, 0xD800u, 0x110000u];
        foreach (uint codePoint in codePoints)
        {
            tree.Context.PushEvent(PointerTree.Char(codePoint));
        }

        tree.Context.Update(0f);

        Assert.True(tree.Field.Text.IsEmpty);
        Assert.Equal(0, tree.Field.CaretIndex);
        Assert.Equal(0, calls);
        Assert.Equal(5, rootChars);
    }

    /// <summary>A char that the field uses stops the walk: the capture handler of the root runs, and its bubble handler does not.</summary>
    [Fact]
    public void ACharStopsTheWalk()
    {
        TextFieldTree tree = new();
        tree.Focus();

        int captures = 0;
        int bubbles = 0;
        tree.Root.OnCharCapture = (ref CharEvent e) => captures++;
        tree.Root.OnChar = (ref CharEvent e) => bubbles++;

        tree.Type("x");
        tree.Context.Update(0f);

        Assert.True(tree.Field.Text.SequenceEqual("x"u8));
        Assert.Equal(1, captures);
        Assert.Equal(0, bubbles);
    }

    /// <summary>
    /// A char with the field as the caller target inserts with no focus owner, and it changes no focus. A char with no
    /// target and no focus owner gets no walk, so it changes nothing. A char and a Left key with a focusable child of
    /// the field as the target bubble to the field, and the field does not edit: the target rule of the char check and
    /// the edit key check. A root bubble handler gets both entries.
    /// </summary>
    [Fact]
    public void ACharWithACallerTargetInserts()
    {
        TextFieldTree tree = new();
        Element child = new() { Name = "child", IsFocusable = true };
        tree.Field.Children.Add(child);
        tree.Settle();
        Assert.Null(tree.Context.FocusedElement);

        int calls = 0;
        tree.Field.TextChanged = f => calls++;

        tree.Context.PushEvent(new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, 0d, tree.Field),
            CodePoint = 'q',
        });
        tree.Context.Update(0f);

        Assert.True(tree.Field.Text.SequenceEqual("q"u8));
        Assert.Equal(1, calls);
        Assert.Null(tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Char('r'));
        tree.Context.Update(0f);

        Assert.True(tree.Field.Text.SequenceEqual("q"u8));
        Assert.Equal(1, calls);

        // The target is a child of the field. The events bubble through the field, and the field must not edit.
        int rootChars = 0;
        int rootKeys = 0;
        tree.Root.OnChar = (ref CharEvent e) => rootChars++;
        tree.Root.OnKeyDown = (ref KeyEvent e) => rootKeys++;

        tree.Context.PushEvent(new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, 0d, child),
            CodePoint = 'z',
        });
        tree.Context.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, child),
            Key = WidgetInput.LeftKey,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        });
        tree.Context.Update(0f);

        Assert.True(tree.Field.Text.SequenceEqual("q"u8));
        Assert.Equal(1, tree.Field.CaretIndex);
        Assert.Equal(1, calls);
        Assert.Equal(1, rootChars);
        Assert.Equal(1, rootKeys);
    }

    /// <summary>
    /// The type id, the focus flag, the overflow, the capacity, the capacity check, and the cut of the start text.
    /// </summary>
    [Fact]
    public void TheCreateInfoTheTypeAndTheOverflow()
    {
        TextFieldTree tree = new();
        tree.Settle();

        Assert.Equal(ElementType.Intern("TextField"), tree.Field.TypeId);
        Assert.True(tree.Field.IsFocusable);
        Assert.Equal(Overflow.Hidden, tree.Field.Inline.Overflow);
        Assert.Equal(Overflow.Hidden, tree.Field.Style.Overflow);
        Assert.Equal(16, tree.Field.Capacity);

        int[] bad = [0, -1];
        foreach (int capacity in bad)
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new TextField(new TextField.CreateInfo { Capacity = capacity }));
            Assert.Equal("info", ex.ParamName);
            Assert.Contains("CreateInfo.Capacity", ex.Message, StringComparison.Ordinal);
        }

        TextField cut = new(new TextField.CreateInfo { Capacity = 4, Text = "abc€" });
        Assert.True(cut.Text.SequenceEqual("abc"u8));
        Assert.Equal(3, cut.CaretIndex);
        Assert.Equal(4, cut.Capacity);

        TextField empty = new(new TextField.CreateInfo { Capacity = 8, Text = null });
        Assert.True(empty.Text.IsEmpty);
        Assert.Equal(0, empty.CaretIndex);
    }

    /// <summary>
    /// A rule with the type <c>TextField</c> sets a fill on the field and not on <see cref="TextFieldTree.Other"/>. The
    /// same rule sets <c>overflow: visible</c>, and the inline value of the field wins, so the clip stays.
    /// </summary>
    [Fact]
    public void ATypeRuleMatchesTheTextField()
    {
        TextFieldTree tree = new(new TextField.CreateInfo { Capacity = 16, Text = "ab" });
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = ElementType.Intern("TextField") }),
                new StyleProperties { BackgroundColor = FieldFill, Overflow = Overflow.Visible })
            .Build();
        tree.Context.AddSheet(sheet);

        RecordingRenderer renderer = tree.Frame();

        Assert.Equal(FieldFill, tree.Field.Style.BackgroundColor);
        Assert.NotEqual(FieldFill, tree.Other.Style.BackgroundColor);
        Assert.NotEqual(FieldFill, tree.Root.Style.BackgroundColor);
        Assert.Equal(Overflow.Hidden, tree.Field.Style.Overflow);

        int fill = IndexOfFill(renderer, FieldFill);
        int push = IndexOfClip(renderer, FieldClip);
        int text = IndexOfText(renderer, "ab"u8);
        Assert.Equal(FieldClip, renderer.Calls[fill].Rect);
        Assert.True(fill < push);
        Assert.True(push < text);
        Assert.True(text < IndexOfPop(renderer, push));
    }

    /// <summary>Gives the index of the only rect call with a fill.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The fill.</param>
    /// <returns>The index among all calls.</returns>
    private static int IndexOfFill(RecordingRenderer renderer, Rgba8 color)
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

    /// <summary>Gives the index of the only clip push with a rect.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="rect">The clip rect.</param>
    /// <returns>The index among all calls.</returns>
    private static int IndexOfClip(RecordingRenderer renderer, float4 rect)
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
    private static int IndexOfText(RecordingRenderer renderer, ReadOnlySpan<byte> utf8)
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
    private static int IndexOfPop(RecordingRenderer renderer, int push)
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
