using System;
using System.Collections.Generic;
using System.Reflection;
using Brutal.GlfwApi;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;
using GlfwApi = Brutal.GlfwApi.Glfw;

namespace BrutalUi.Glfw.Tests;

/// <summary>
/// Tests of <see cref="GlfwUiInput"/>. Each test except the last uses the test seam (no window) and calls the
/// internal push methods. The last test uses a real hidden window and skips when GLFW has no display.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class GlfwUiInputTests
{
    private static readonly float2 OverRight = new(175f, 75f);
    private static readonly float2 OverLeft = new(50f, 50f);

    /// <summary>The names of the six window events that the adapter subscribes to.</summary>
    private static readonly string[] EventNames =
    [
        nameof(GlfwWindow.OnCursorPos),
        nameof(GlfwWindow.OnMouseButton),
        nameof(GlfwWindow.OnScroll),
        nameof(GlfwWindow.OnCursorEnter),
        nameof(GlfwWindow.OnDrop),
        nameof(GlfwWindow.OnContentScale),
    ];

    /// <summary>
    /// The demo of phase 4. A cursor move and a press through the adapter, over the second child, set
    /// <see cref="PseudoStates.Hover"/> and <see cref="PseudoStates.Active"/> on it.
    /// </summary>
    [Fact]
    public void ACursorMoveAndAPressHoverAndActivateTheChild()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(ACursorMoveAndAPressHoverAndActivateTheChild));
        tree.AddHoverSheet();
        tree.AddActiveSheet();
        tree.Settle();
        using GlfwUiInput input = new(tree.Context);

        input.PushCursorMove(OverRight, UiModifiers.None, 1d);
        input.PushButton(0, press: true, UiModifiers.None, 1.25d);
        Assert.Equal(2, tree.Context.DrainEvents());
        tree.Settle();

        TestContext.Current.TestOutputHelper?.WriteLine(
            tree.Right.Name + " " + tree.Right.States + " " + tree.Right.Style.BackgroundColor + " "
            + (tree.Context.ActiveElement?.Name ?? "none"));

        Assert.Equal(PseudoStates.Hover | PseudoStates.Active, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.PointerHit);
        Assert.Same(tree.Right, tree.Context.ActiveElement);
        Assert.Equal(PointerTree.ActiveFill, tree.Right.Style.BackgroundColor);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Root.States);
    }

    /// <summary>
    /// A move carries the position with no scale, the modifiers, and the timestamp. It has no target from the caller,
    /// so the drain hits the element under the position.
    /// </summary>
    [Fact]
    public void AMoveCarriesThePositionTheModifiersAndTheTime()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(AMoveCarriesThePositionTheModifiersAndTheTime));
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);

        UiModifiers modifiers = UiModifiers.Shift | UiModifiers.Super;
        input.PushCursorMove(OverRight, modifiers, 12.5d);
        Assert.Equal(1, tree.Context.DrainEvents());

        PointerEvent e = recorder.LastPointer;
        Assert.Equal(UiEventType.PointerMove, e.Header.Type);
        Assert.Same(tree.Right, e.Header.Target);
        Assert.Equal(12.5d, e.Header.Timestamp);
        Assert.Equal(OverRight, e.Position);
        Assert.Equal(modifiers, e.Modifiers);
        Assert.Equal(PointerButton.Left, e.Button);
        Assert.Equal(OverRight, tree.Context.PointerPosition);
    }

    /// <summary>
    /// Each GLFW button value from 0 to 7 casts to <see cref="PointerButton"/> with no table and no filter.
    /// </summary>
    [Fact]
    public void EachButtonCastsWithNoTable()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(EachButtonCastsWithNoTable));
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);
        input.PushCursorMove(OverRight, UiModifiers.None, 0d);
        Assert.Equal(1, tree.Context.DrainEvents());

        for (int button = 0; button <= 7; button++)
        {
            input.PushButton(button, press: true, UiModifiers.None, button);
            Assert.Equal(1, tree.Context.DrainEvents());
            Assert.Equal(UiEventType.PointerDown, recorder.LastPointer.Header.Type);
            Assert.Equal((PointerButton)button, recorder.LastPointer.Button);
            Assert.Equal(button, (int)recorder.LastPointer.Button);

            input.PushButton(button, press: false, UiModifiers.None, button);
            Assert.Equal(1, tree.Context.DrainEvents());
            Assert.Equal(UiEventType.PointerUp, recorder.LastPointer.Header.Type);
            Assert.Equal(button, (int)recorder.LastPointer.Button);
        }

        Assert.Equal(PointerButton.Left, (PointerButton)(int)GlfwMouseButton.Left);
        Assert.Equal(PointerButton.Right, (PointerButton)(int)GlfwMouseButton.Right);
        Assert.Equal(PointerButton.Middle, (PointerButton)(int)GlfwMouseButton.Middle);
        Assert.Equal(7, (int)GlfwMouseButton.Last);
    }

    /// <summary>
    /// A press gives <see cref="UiEventType.PointerDown"/> and a release gives <see cref="UiEventType.PointerUp"/>, at
    /// the last cursor position. The button modifiers keep the GLFW bits <c>0x3F</c>.
    /// </summary>
    [Fact]
    public void APressAndAReleaseGiveDownAndUp()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(APressAndAReleaseGiveDownAndUp));
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);
        input.PushCursorMove(OverRight, UiModifiers.None, 0d);
        Assert.Equal(1, tree.Context.DrainEvents());

        UiModifiers modifiers = GlfwUiInput.ButtonModifiers(GlfwModifier.Control | GlfwModifier.CapsLock);
        Assert.Equal(UiModifiers.Control | UiModifiers.CapsLock, modifiers);
        Assert.Equal((UiModifiers)0x3F, GlfwUiInput.ButtonModifiers((GlfwModifier)0xFF));
        Assert.Equal(UiModifiers.None, GlfwUiInput.ButtonModifiers((GlfwModifier)0x40));

        input.PushButton((int)GlfwMouseButton.Right, press: true, modifiers, 2d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.PointerDown, recorder.LastPointer.Header.Type);
        Assert.Equal(PointerButton.Right, recorder.LastPointer.Button);
        Assert.Equal(OverRight, recorder.LastPointer.Position);
        Assert.Equal(modifiers, recorder.LastPointer.Modifiers);
        Assert.Equal(2d, recorder.LastPointer.Header.Timestamp);
        Assert.Same(tree.Right, tree.Context.ActiveElement);

        input.PushButton((int)GlfwMouseButton.Right, press: false, UiModifiers.None, 3d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.PointerUp, recorder.LastPointer.Header.Type);
        Assert.Equal(PointerButton.Right, recorder.LastPointer.Button);
        Assert.Equal(OverRight, recorder.LastPointer.Position);
        Assert.Equal(3d, recorder.LastPointer.Header.Timestamp);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
    }

    /// <summary>
    /// A scroll has no position of its own. It takes the last cursor position: (0, 0) for the test seam before any
    /// move, and then the position of the last move.
    /// </summary>
    [Fact]
    public void AScrollUsesTheLastCursorPosition()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(AScrollUsesTheLastCursorPosition));
        tree.Settle();
        EventRecorder recorder = new();
        recorder.Register(tree.Left);
        recorder.Register(tree.Right);
        using GlfwUiInput input = new(tree.Context);

        input.PushScroll(new float2(0f, 1f), UiModifiers.None, 1d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, recorder.LastScroll.Header.Target);
        Assert.Equal(float2.Zero, recorder.LastScroll.Position);

        input.PushCursorMove(OverRight, UiModifiers.None, 2d);
        input.PushScroll(new float2(-0.5f, -2f), UiModifiers.Alt, 3d);
        Assert.Equal(2, tree.Context.DrainEvents());

        ScrollEvent e = recorder.LastScroll;
        Assert.Equal(UiEventType.Scroll, e.Header.Type);
        Assert.Same(tree.Right, e.Header.Target);
        Assert.Equal(OverRight, e.Position);
        Assert.Equal(new float2(-0.5f, -2f), e.Delta);
        Assert.Equal(UiModifiers.Alt, e.Modifiers);
        Assert.Equal(3d, e.Header.Timestamp);
    }

    /// <summary>
    /// A cursor leave pushes a move at (NaN, NaN). The move hits no element and clears the hover. A later scroll
    /// takes the NaN position and also hits no element.
    /// </summary>
    [Fact]
    public void ACursorLeaveClearsHover()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(ACursorLeaveClearsHover));
        tree.Settle();
        using GlfwUiInput input = new(tree.Context);

        input.PushCursorMove(OverLeft, UiModifiers.None, 1d);
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        input.PushCursorLeave(2d);
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Null(tree.Context.PointerHit);
        Assert.True(tree.Context.HasPointerPosition);
        Assert.True(float.IsNaN(tree.Context.PointerPosition.X));
        Assert.True(float.IsNaN(tree.Context.PointerPosition.Y));

        input.PushScroll(new float2(0f, 1f), UiModifiers.None, 3d);
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.PointerHit);
    }

    /// <summary>
    /// A drop calls <see cref="UiContext.OnFileDrop"/> at once with the paths, in order. It needs no drain.
    /// </summary>
    [Fact]
    public void ADropReachesTheContextCallback()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(ADropReachesTheContextCallback));
        using GlfwUiInput input = new(tree.Context);
        List<string> seen = [];
        int calls = 0;
        tree.Context.OnFileDrop = paths =>
        {
            calls++;
            foreach (string path in paths)
            {
                seen.Add(path);
            }
        };

        input.PushFileDrop(["/tmp/a.txt", "/tmp/b dir"]);

        Assert.Equal(1, calls);
        Assert.Equal(new[] { "/tmp/a.txt", "/tmp/b dir" }, seen);
        Assert.Equal(0, tree.Context.DrainEvents());
    }

    /// <summary>
    /// A content-scale event calls <see cref="UiContext.OnContentScale"/> at once with the scale. The context does
    /// not change its viewport size.
    /// </summary>
    [Fact]
    public void AContentScaleReachesTheContextCallback()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(AContentScaleReachesTheContextCallback));
        using GlfwUiInput input = new(tree.Context);
        float2 seen = float2.Zero;
        int calls = 0;
        tree.Context.OnContentScale = scale =>
        {
            calls++;
            seen = scale;
        };

        input.PushContentScale(new float2(1.5f, 2f));

        Assert.Equal(1, calls);
        Assert.Equal(new float2(1.5f, 2f), seen);
        Assert.Equal(new float2(800f, 600f), tree.Context.ViewportSize);
    }

    /// <summary>
    /// After <see cref="GlfwUiInput.Dispose"/>, each push method returns at once. A second dispose does nothing.
    /// </summary>
    [Fact]
    public void DisposeStopsThePushes()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(DisposeStopsThePushes));
        tree.Settle();
        GlfwUiInput input = new(tree.Context);
        int calls = 0;
        tree.Context.OnFileDrop = _ => calls++;
        tree.Context.OnContentScale = _ => calls++;

        input.Dispose();
        input.PushCursorMove(OverRight, UiModifiers.None, 1d);
        input.PushButton(0, press: true, UiModifiers.None, 2d);
        input.PushScroll(new float2(0f, 1f), UiModifiers.None, 3d);
        input.PushCursorLeave(4d);
        input.PushFileDrop(["/tmp/a.txt"]);
        input.PushContentScale(new float2(2f, 2f));
        input.Dispose();

        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(0, calls);
        Assert.False(tree.Context.HasPointerPosition);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Same(tree.Context, input.Context);
    }

    /// <summary>
    /// A real hidden window: the constructor adds one handler to each of the six events, seeds the last cursor
    /// position from the window, and converts a cursor event. <see cref="GlfwUiInput.Dispose"/> removes the handlers.
    /// The test skips when GLFW cannot start or cannot create a window (no display). The constructor checks for null
    /// arguments run first, because they need no display.
    /// </summary>
    [Fact]
    public void ARealWindowSubscribesAndUnsubscribes()
    {
        PointerTree tree = new(nameof(GlfwUiInputTests) + "." + nameof(ARealWindowSubscribesAndUnsubscribes));
        tree.Settle();

        Assert.Throws<ArgumentNullException>(() => new GlfwUiInput(null!));
        Assert.Throws<ArgumentNullException>(
            () => new GlfwUiInput(new GlfwUiInput.CreateInfo { Window = null!, Context = tree.Context }));

        GlfwWindow? window = null;
        GlfwUiInput? input = null;
        try
        {
            if (!GlfwApi.Init())
            {
                GlfwErrorCode code = GlfwApi.GetError(out string? description);
                Assert.Skip($"GLFW init failed: {code} ({description}). The machine has no display.");
            }

            GlfwApi.DefaultWindowHints();
            GlfwApi.WindowHint(GlfwWindowHint.ClientApi, GlfwApi.Constants.GLFW_NO_API);
            GlfwApi.WindowHint(GlfwWindowHint.Visible, GlfwApi.Constants.GLFW_FALSE);
            try
            {
                window = GlfwApi.CreateWindow(new GlfwWindow.CreateInfo
                {
                    Title = "GlfwUiInputTests",
                    Size = new int2(320, 240),
                });
            }
            catch (InvalidOperationException exception)
            {
                Assert.Skip($"GLFW could not create a hidden window: {exception.Message}");
            }

            Assert.Throws<ArgumentNullException>(
                () => new GlfwUiInput(new GlfwUiInput.CreateInfo { Window = window, Context = null! }));

            input = new GlfwUiInput(new GlfwUiInput.CreateInfo { Window = window, Context = tree.Context });
            Assert.Same(tree.Context, input.Context);
            foreach (string name in EventNames)
            {
                Assert.Equal(1, CountHandlers(window, name, input));
            }

            float2 seed = new((float)window.CursorPos.X, (float)window.CursorPos.Y);
            input.PushScroll(new float2(0f, 1f), UiModifiers.None, 0d);
            tree.Context.DrainEvents();
            Assert.Equal(seed, tree.Context.PointerPosition);

            GlfwCursorPosCallback cursorPos = (GlfwCursorPosCallback)ReadEvent(window, nameof(GlfwWindow.OnCursorPos))!;
            cursorPos(window, new double2(175d, 75d));
            Assert.Equal(1, tree.Context.DrainEvents());
            Assert.Same(tree.Right, tree.Context.PointerHit);
            Assert.Equal(OverRight, tree.Context.PointerPosition);

            input.Dispose();
            foreach (string name in EventNames)
            {
                Assert.Equal(0, CountHandlers(window, name, input));
            }
        }
        finally
        {
            input?.Dispose();
            window?.Dispose();
            GlfwApi.Terminate();
        }
    }

    /// <summary>Reads the delegate behind a field-like event of the window.</summary>
    /// <param name="window">The window.</param>
    /// <param name="name">The event name. The compiler gives the backing field the same name.</param>
    /// <returns>The delegate, or <see langword="null"/> when the event has no handler.</returns>
    private static Delegate? ReadEvent(GlfwWindow window, string name)
    {
        FieldInfo? field = typeof(GlfwWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (Delegate?)field.GetValue(window);
    }

    /// <summary>Counts the handlers of one window event that belong to the adapter.</summary>
    /// <param name="window">The window.</param>
    /// <param name="name">The event name.</param>
    /// <param name="input">The adapter.</param>
    /// <returns>The number of handlers with the adapter as the target.</returns>
    private static int CountHandlers(GlfwWindow window, string name, GlfwUiInput input)
    {
        Delegate? handlers = ReadEvent(window, name);
        if (handlers is null)
        {
            return 0;
        }

        int count = 0;
        foreach (Delegate handler in handlers.GetInvocationList())
        {
            if (ReferenceEquals(handler.Target, input))
            {
                count++;
            }
        }

        return count;
    }
}
