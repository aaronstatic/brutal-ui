using System;
using Brutal.Localization;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation gate of the text field (the twelfth gate). After warm-up, <c>Update</c> and <c>Draw</c> on a tree of 20
/// text fields and the full text field input path must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is a column root of 400 x 400 with 20 <see cref="TextField"/> elements of 81 x 16, the font size 16, the
/// capacity 32, and the start text <c>field</c>. A sheet holds the rules <c>TextField</c>, <c>TextField:focus</c>, and
/// <c>:disabled</c>. The last field holds <see cref="PseudoStates.Disabled"/>, so the <c>:disabled</c> rule matches. The
/// test sends no input to it. The first field holds the focus, so the caret pass and the blink run in each frame.
/// </para>
/// <para>
/// Each step of the input test works on the first field. The step sets the text <c>seed</c> from code and pushes the
/// chars <c>a</c>, <c>b</c>, and the euro sign, Left, Shift+Left, the char <c>c</c> (it replaces the selection),
/// Backspace, and Delete. The text is then <c>seeda</c>. The step pushes a left press, a drag move, and a release, and it
/// runs one <c>Update</c>, so the caret pass maps the press and the drag with real measure calls. The step then pushes
/// Home, Shift+End, Delete (it removes the selection and leaves an empty text), End, Enter, and a char past the capacity
/// to the second field, which holds a full text from <c>SetText</c>. The step also sets two strings and one
/// <see cref="LString"/> on the fourth field, so the string overload and the <see cref="LString"/> overload run with a
/// same text (the compare) and a new text (the encode). The step ends with <c>GetText</c> into a stack span,
/// <c>Update</c>, and <c>Draw</c>. Each step calls <see cref="TextField.TextChanged"/> 7 times and
/// <see cref="TextField.Submitted"/> 1 time.
/// </para>
/// <para>
/// Each gate calls <see cref="AllocationGate.ReleaseAllocationContext"/> right before the measure. The measured bodies
/// create no lambda, no string, no boxed value, and no Assert. The test adds no warm-up round to hide an allocation.
/// </para>
/// </remarks>
public sealed class TextFieldAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int FieldCount = 20;
    private const int Capacity = 32;
    private const float ViewportWidth = 800f;
    private const float ViewportHeight = 600f;
    private const float RootWidth = 400f;
    private const float RootHeight = 400f;
    private const float FieldWidth = 81f;
    private const float FieldHeight = 16f;
    private const float FontSize = 16f;
    private const uint EuroSign = 0x20AC;

    private static readonly Rgba8 White = new(255, 255, 255, 255);
    private static readonly Rgba8 FieldFill = new(32, 32, 32, 255);
    private static readonly Rgba8 FieldText = new(220, 220, 220, 255);
    private static readonly Rgba8 FocusText = new(64, 160, 96, 255);
    private static readonly Rgba8 DisabledText = new(96, 96, 96, 255);

    // The first field is at x 0 to 81 and y 0 to 16. Eight pixels are one byte of the text.
    private static readonly float2 PressPoint = new(21f, 8f);
    private static readonly float2 DragPoint = new(37f, 8f);

    // The texts that the input step sets on the fourth field through the string overload and the LString overload.
    // The test builds them one time, so the measured body creates no string.
    private static readonly string StringTextA = "alpha";
    private static readonly string StringTextB = "b€ta";

    /// <summary>Counts the calls. Its methods are the targets of the sender delegates.</summary>
    private sealed class Counter
    {
        public int Changes;
        public int Submits;

        public void OnChanged(TextField field)
        {
            Changes++;
        }

        public void OnSubmitted(TextField field)
        {
            Submits++;
        }
    }

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        public required Counter Counter { get; init; }

        public required TextField[] Fields { get; init; }

        public required LString LocalizedText { get; init; }

        public required CharEvent CharA;
        public required CharEvent CharB;
        public required CharEvent CharEuro;
        public required CharEvent CharC;
        public required CharEvent CharPastCapacity;
        public required KeyEvent Left;
        public required KeyEvent ShiftLeft;
        public required KeyEvent Backspace;
        public required KeyEvent Delete;
        public required KeyEvent Home;
        public required KeyEvent ShiftEnd;
        public required KeyEvent End;
        public required KeyEvent Enter;
        public required PointerEvent Press;
        public required PointerEvent Drag;
        public required PointerEvent Release;

        /// <summary>The number of bytes that the last <c>GetText</c> copied. It keeps the compiler from removing the call.</summary>
        public int LastLength;
    }

    /// <summary>The 32 bytes of a full text. A <c>u8</c> literal is static data, so the property allocates nothing.</summary>
    private static ReadOnlySpan<byte> FullText => "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"u8;

    private static Scene BuildScene()
    {
        RecordingRenderer renderer = new();
        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer })
        {
            ViewportSize = new float2(ViewportWidth, ViewportHeight),
        };

        ElementTypeId fieldType = ElementType.Intern("TextField");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = fieldType }),
                new StyleProperties { BackgroundColor = FieldFill, Color = FieldText })
            .Add(
                Selector.For(new CompoundSelector { Type = fieldType, States = PseudoStates.Focus }),
                new StyleProperties { Color = FocusText })
            .Add(
                Selector.For(new CompoundSelector { States = PseudoStates.Disabled }),
                new StyleProperties { Color = DisabledText })
            .Build();
        context.AddSheet(sheet);

        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = RootWidth;
        root.Inline.Height = RootHeight;
        root.Inline.Color = White;
        root.IsPickable = true;
        context.AddRoot(root);

        Counter counter = new();
        Action<TextField> onChanged = counter.OnChanged;
        Action<TextField> onSubmitted = counter.OnSubmitted;

        TextField[] fields = new TextField[FieldCount];
        for (int i = 0; i < fields.Length; i++)
        {
            TextField field = new(new TextField.CreateInfo { Capacity = Capacity, Text = "field" });
            field.Inline.Width = FieldWidth;
            field.Inline.Height = FieldHeight;
            field.Inline.FontSize = FontSize;
            field.TextChanged = onChanged;
            field.Submitted = onSubmitted;
            root.Children.Add(field);
            fields[i] = field;
        }

        // The disabled rule matches the last field. The step sends no input to it.
        fields[FieldCount - 1].States |= PseudoStates.Disabled;

        context.Update(0f);
        context.SetFocus(fields[0]);

        return new Scene
        {
            Context = context,
            Renderer = renderer,
            Counter = counter,
            Fields = fields,
            LocalizedText = new LString("text-field-allocation-tests.text", "gamma"),
            CharA = PointerTree.Char('a'),
            CharB = PointerTree.Char('b'),
            CharEuro = PointerTree.Char(EuroSign),
            CharC = PointerTree.Char('c'),
            CharPastCapacity = new CharEvent
            {
                Header = new UiEventHeader(UiEventType.Char, 0d, fields[1]),
                CodePoint = 'z',
            },
            Left = PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.None),
            ShiftLeft = PointerTree.Key(UiEventType.KeyDown, WidgetInput.LeftKey, UiModifiers.Shift),
            Backspace = PointerTree.Key(UiEventType.KeyDown, WidgetInput.BackspaceKey, UiModifiers.None),
            Delete = PointerTree.Key(UiEventType.KeyDown, WidgetInput.DeleteKey, UiModifiers.None),
            Home = PointerTree.Key(UiEventType.KeyDown, WidgetInput.HomeKey, UiModifiers.None),
            ShiftEnd = PointerTree.Key(UiEventType.KeyDown, WidgetInput.EndKey, UiModifiers.Shift),
            End = PointerTree.Key(UiEventType.KeyDown, WidgetInput.EndKey, UiModifiers.None),
            Enter = PointerTree.Key(UiEventType.KeyDown, WidgetInput.EnterKey, UiModifiers.None),
            Press = PointerTree.Down(PressPoint, PointerButton.Left),
            Drag = PointerTree.Move(DragPoint),
            Release = PointerTree.Up(DragPoint, PointerButton.Left),
        };
    }

    /// <summary>The measured body of the first test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void Frame(Scene scene)
    {
        scene.Context.Update(0.016f);
        scene.Context.Draw(scene.Renderer);
    }

    private static void RunFrames(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            Frame(scene);
        }
    }

    /// <summary>The measured body of the second test. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void InputStep(Scene scene)
    {
        UiContext context = scene.Context;
        TextField first = scene.Fields[0];

        first.SetText("seed"u8);
        context.PushEvent(in scene.CharA);
        context.PushEvent(in scene.CharB);
        context.PushEvent(in scene.CharEuro);
        context.PushEvent(in scene.Left);
        context.PushEvent(in scene.ShiftLeft);
        context.PushEvent(in scene.CharC);
        context.PushEvent(in scene.Backspace);
        context.PushEvent(in scene.Delete);

        // The press, the drag, and the release come before one Update, so the caret pass maps both x values.
        context.PushEvent(in scene.Press);
        context.PushEvent(in scene.Drag);
        context.PushEvent(in scene.Release);
        context.Update(0.016f);

        scene.Fields[1].SetText(FullText);

        // The fourth field takes the string overload and the LString overload. Each text differs from the text before it,
        // except the repeats, so both the compare and the encode run. The calls raise no TextChanged.
        TextField fourth = scene.Fields[3];
        fourth.SetText(StringTextA);
        fourth.SetText(StringTextA);
        fourth.SetText(StringTextB);
        fourth.SetText(scene.LocalizedText);
        fourth.SetText(scene.LocalizedText);
        context.PushEvent(in scene.Home);
        context.PushEvent(in scene.ShiftEnd);
        context.PushEvent(in scene.Delete);
        context.PushEvent(in scene.End);
        context.PushEvent(in scene.Enter);
        context.PushEvent(in scene.CharPastCapacity);

        Span<byte> copy = stackalloc byte[Capacity];
        context.DrainEvents();
        scene.LastLength = first.GetText(copy);
        context.Update(0.016f);
        context.Draw(scene.Renderer);
    }

    private static void RunInputSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            InputStep(scene);
        }
    }

    /// <summary><c>Update</c> and <c>Draw</c> on a tree of text fields allocate nothing after warm-up.</summary>
    [Fact]
    public void UpdateAndDrawAllocateNothingWithTextFields()
    {
        Scene scene = BuildScene();
        RunFrames(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunFrames(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The full text field input path allocates nothing after warm-up: <c>SetText</c>, the chars, the keys, the
    /// selection, a press, a drag, a release, Enter, a dropped char, <c>GetText</c>, <c>Update</c>, and <c>Draw</c>.
    /// </summary>
    [Fact]
    public void TheTextFieldInputPathAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunInputSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunInputSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured body does real work. One input step calls <see cref="TextField.TextChanged"/> 7 times and
    /// <see cref="TextField.Submitted"/> one time, leaves the first field empty, drops the char on the full field, and
    /// draws the text of 19 fields and the caret rect of the blink phase.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        TextField first = scene.Fields[0];
        Assert.Same(first, scene.Context.FocusedElement);
        Assert.Equal(PseudoStates.Disabled, scene.Fields[FieldCount - 1].States & PseudoStates.Disabled);
        Assert.Same(first, scene.Context.HitTest(PressPoint));

        InputStep(scene);

        Assert.Equal(7, scene.Counter.Changes);
        Assert.Equal(1, scene.Counter.Submits);
        Assert.True(first.Text.SequenceEqual(""u8));
        Assert.Equal(0, scene.LastLength);
        Assert.Equal(0, first.CaretIndex);
        Assert.True(scene.Fields[1].Text.SequenceEqual(FullText));
        Assert.True(scene.Fields[2].Text.SequenceEqual("field"u8));

        int texts = 0;
        int rects = 0;
        ReadOnlySpan<DrawCall> calls = scene.Renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            texts += calls[i].Kind == DrawCallKind.Text ? 1 : 0;
            rects += calls[i].Kind == DrawCallKind.Rect ? 1 : 0;
        }

        Assert.Equal(FieldCount - 1, texts);

        // The field has a fill, so each field draws one rect for its background. The caret adds one rect in its shown phase.
        Assert.Equal(FieldCount + (first.CaretVisible ? 1 : 0), rects);
    }
}
