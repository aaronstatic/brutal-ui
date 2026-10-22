using System;
using System.Reflection;
using Brutal.GlfwApi;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;
using GlfwApi = Brutal.GlfwApi.Glfw;

namespace BrutalUi.Glfw.Tests;

/// <summary>
/// The allocation checks of the adapter handlers. After warm-up, the push methods and the real window handlers of
/// <see cref="GlfwUiInput"/> must allocate 0 bytes, also when the event ring is full and each push drops the oldest
/// event.
/// </summary>
/// <remarks>
/// <para>
/// The first gate calls the internal push methods of the test seam (no window): <c>PushCursorMove</c>,
/// <c>PushButton</c>, <c>PushScroll</c>, and <c>PushCursorLeave</c>.
/// </para>
/// <para>
/// The second gate measures the real handlers with a hidden window. It calls the delegates behind
/// <see cref="GlfwWindow.OnCursorPos"/>, <see cref="GlfwWindow.OnScroll"/>, and <see cref="GlfwWindow.OnMouseButton"/>.
/// This path also calls <c>GlfwWindow.GetKey</c> and <c>Glfw.Time</c> in the BRUTAL binding. The gate skips with the
/// reason when <c>Glfw.Init</c> or the window creation fails, for example on a machine with no display. Use
/// <c>xvfb-run -a</c> or <c>scripts/test-windows.ps1</c> to run it.
/// </para>
/// <para>
/// Both gates leave out the drop and the content-scale events. A drop allocates inside the binding and in the path
/// strings.
/// </para>
/// <para>
/// The ring has a capacity of 8 and is full before the measure, so every push in the measured region drops one event.
/// </para>
/// <para>
/// The class is in a collection with no parallel run. The second gate starts and stops GLFW, and GLFW state is
/// global, so no other test that uses GLFW can run at the same time.
/// </para>
/// </remarks>
[Collection(GlfwUiInputAllocationTests.CollectionName)]
public sealed class GlfwUiInputAllocationTests
{
    /// <summary>The name of the collection with no parallel run.</summary>
    public const string CollectionName = "GlfwUiInputAllocation";

    private const int QueueCapacity = 8;
    private const int PushCount = 1000;

    private static readonly float2 MovePosition = new(175f, 75f);
    private static readonly float2 ScrollDelta = new(0f, -1f);
    private static readonly double2 RealCursorPosition = new(175d, 75d);
    private static readonly double2 RealScrollOffset = new(0d, -1d);

    /// <summary>The context, the adapter, and the counters of the measured body.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required GlfwUiInput Input { get; init; }
    }

    /// <summary>The delegates behind the window events. The test reads them before the measure.</summary>
    private sealed class Handlers
    {
        public required GlfwCursorPosCallback CursorPos { get; init; }

        public required GlfwScrollCallback Scroll { get; init; }

        public required GlfwMouseButtonCallback MouseButton { get; init; }
    }

    private static Scene BuildScene()
    {
        UiContext context = new(new UiContext.CreateInfo
        {
            Measurer = new RecordingRenderer(),
            EventQueueCapacity = QueueCapacity,
        });
        GlfwUiInput input = new(context);
        return new Scene { Context = context, Input = input };
    }

    /// <summary>The measured body. It uses no lambda, no boxing, no string, and no Assert.</summary>
    private static void Push(GlfwUiInput input, int count)
    {
        for (int i = 0; i < count; i++)
        {
            input.PushCursorMove(MovePosition, UiModifiers.None, 1d);
        }

        for (int i = 0; i < count; i++)
        {
            input.PushButton(0, press: true, UiModifiers.None, 1d);
        }

        for (int i = 0; i < count; i++)
        {
            input.PushScroll(ScrollDelta, UiModifiers.None, 1d);
        }

        for (int i = 0; i < count; i++)
        {
            input.PushCursorLeave(1d);
        }
    }

    /// <summary>
    /// The measured body of the real handlers. It uses no lambda, no boxing, no string, and no Assert.
    /// </summary>
    private static void CallHandlers(Handlers handlers, GlfwWindow window, int count)
    {
        GlfwCursorPosCallback cursorPos = handlers.CursorPos;
        GlfwScrollCallback scroll = handlers.Scroll;
        GlfwMouseButtonCallback mouseButton = handlers.MouseButton;

        for (int i = 0; i < count; i++)
        {
            cursorPos(window, RealCursorPosition);
        }

        for (int i = 0; i < count; i++)
        {
            scroll(window, RealScrollOffset);
        }

        for (int i = 0; i < count; i++)
        {
            mouseButton(window, GlfwMouseButton.Left, GlfwButtonAction.Press, default);
        }
    }

    /// <summary>Reads the delegate behind a field-like event of the window.</summary>
    /// <typeparam name="T">The delegate type of the event.</typeparam>
    /// <param name="window">The window.</param>
    /// <param name="name">The event name. The compiler gives the backing field the same name.</param>
    /// <returns>The delegate.</returns>
    private static T ReadEvent<T>(GlfwWindow window, string name)
        where T : Delegate
    {
        FieldInfo? field = typeof(GlfwWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        object? value = field.GetValue(window);
        Assert.NotNull(value);
        return Assert.IsType<T>(value);
    }

    [Fact]
    public void TheAdapterHandlersAllocateNothing()
    {
        Scene scene = BuildScene();
        UiContext context = scene.Context;
        GlfwUiInput input = scene.Input;

        // Fill the ring, then warm up with full-ring pushes.
        Push(input, QueueCapacity);
        Push(input, QueueCapacity);
        Assert.Equal(QueueCapacity, context.QueuedEventCount);

        long dropsBefore = context.DroppedEventCount;
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        Push(input, PushCount);
        long after = GC.GetAllocatedBytesForCurrentThread();
        long drops = context.DroppedEventCount - dropsBefore;

        Assert.Equal(0L, after - before);
        Assert.Equal(4L * PushCount, drops);
        Assert.Equal(QueueCapacity, context.QueuedEventCount);
    }

    [Fact]
    public void TheRealHandlersAllocateNothing()
    {
        UiContext context = new(new UiContext.CreateInfo
        {
            Measurer = new RecordingRenderer(),
            EventQueueCapacity = QueueCapacity,
        });

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
                    Title = "GlfwUiInputAllocationTests",
                    Size = new int2(320, 240),
                });
            }
            catch (InvalidOperationException exception)
            {
                Assert.Skip($"GLFW could not create a hidden window: {exception.Message}");
            }

            input = new GlfwUiInput(new GlfwUiInput.CreateInfo { Window = window, Context = context });
            Handlers handlers = new()
            {
                CursorPos = ReadEvent<GlfwCursorPosCallback>(window, nameof(GlfwWindow.OnCursorPos)),
                Scroll = ReadEvent<GlfwScrollCallback>(window, nameof(GlfwWindow.OnScroll)),
                MouseButton = ReadEvent<GlfwMouseButtonCallback>(window, nameof(GlfwWindow.OnMouseButton)),
            };

            // Fill the ring, then warm up with full-ring calls.
            CallHandlers(handlers, window, QueueCapacity);
            CallHandlers(handlers, window, QueueCapacity);
            Assert.Equal(QueueCapacity, context.QueuedEventCount);

            long dropsBefore = context.DroppedEventCount;
            AllocationGate.ReleaseAllocationContext();
            long before = GC.GetAllocatedBytesForCurrentThread();
            CallHandlers(handlers, window, PushCount);
            long after = GC.GetAllocatedBytesForCurrentThread();
            long drops = context.DroppedEventCount - dropsBefore;

            Assert.Equal(0L, after - before);
            Assert.Equal(3L * PushCount, drops);
            Assert.Equal(QueueCapacity, context.QueuedEventCount);
        }
        finally
        {
            input?.Dispose();
            window?.Dispose();
            GlfwApi.Terminate();
        }
    }
}

/// <summary>
/// The collection of <see cref="GlfwUiInputAllocationTests"/>. It runs after the parallel collections and alone, so
/// its GLFW start and stop do not overlap the GLFW test of <see cref="GlfwUiInputTests"/>.
/// </summary>
[CollectionDefinition(GlfwUiInputAllocationTests.CollectionName, DisableParallelization = true)]
public sealed class GlfwUiInputAllocationCollection
{
}
