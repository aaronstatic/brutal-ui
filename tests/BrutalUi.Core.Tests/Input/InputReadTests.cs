using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the slot <see cref="UiContext.OnInputRead"/> (Q1 of input-actions): the one call after the drain, the
/// frame read inside the call, the order after the before-update slot and the drain slot, the null slot, the
/// re-entry throw, the exception rule, and the 0-byte rule of the slot path.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test builds its own context with a
/// <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. Each test pushes hand-built events: the
/// Core event factories are internal. GLFW key W is 87.
/// </remarks>
public sealed class InputReadTests
{
    private const int WKey = 87;
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;

    /// <summary>The handler of the allocation gate. The field creates the delegate one time.</summary>
    private static readonly UiInputReadHandler s_gateHandler = static _ => s_gateCalls++;

    /// <summary>The counter of <see cref="s_gateHandler"/>.</summary>
    private static int s_gateCalls;

    /// <summary>Creates a context with a recording measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400f, 300f),
        };
    }

    /// <summary>Creates a key down with no target.</summary>
    /// <param name="key">The key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent KeyDown(int key)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }

    /// <summary>
    /// A key down of W with no target: <see cref="UiContext.Update(float)"/> calls the slot one time, the slot gets
    /// the same context, and the slot reads <see cref="UiKeyState.WasPressed(int)"/> of W as
    /// <see langword="true"/> inside the call, after the drain. A second update with no event calls the slot again,
    /// and the read gives <see langword="false"/>, so the drain cleared the pressed bit of the frame.
    /// </summary>
    [Fact]
    public void TheSlotRunsOneTimeAfterTheDrain()
    {
        UiContext context = NewContext();
        int calls = 0;
        UiContext? gotContext = null;
        bool pressedInside = false;
        context.OnInputRead = c =>
        {
            calls++;
            gotContext = c;
            pressedInside = c.GameInput.Keys.WasPressed(WKey);
        };

        KeyEvent e = KeyDown(WKey);
        context.PushEvent(in e);
        context.Update(0f);

        Assert.Equal(1, calls);
        Assert.Same(context, gotContext);
        Assert.True(pressedInside);

        context.Update(0f);
        Assert.Equal(2, calls);
        Assert.False(pressedInside);
    }

    /// <summary>
    /// The order of the three slots in one update: <see cref="UiContext.OnBeforeUpdate"/> writes 0,
    /// <see cref="UiContext.OnUnhandledInput"/> writes 1 for the W key down in the drain, and
    /// <see cref="UiContext.OnInputRead"/> writes 2 after the drain.
    /// </summary>
    [Fact]
    public void TheSlotRunsAfterTheDrainSlotAndTheBeforeUpdateSlot()
    {
        UiContext context = NewContext();
        int[] log = new int[3];
        int step = 0;
        context.OnBeforeUpdate = _ => log[step++] = 0;
        context.OnUnhandledInput = (_, _, _, _) => log[step++] = 1;
        context.OnInputRead = _ => log[step++] = 2;

        KeyEvent e = KeyDown(WKey);
        context.PushEvent(in e);
        context.Update(0f);

        Assert.Equal(3, step);
        Assert.Equal(0, log[0]);
        Assert.Equal(1, log[1]);
        Assert.Equal(2, log[2]);
    }

    /// <summary>
    /// An update with no slot works. A slot that is set to <see langword="null"/> after one call gets no second
    /// call.
    /// </summary>
    [Fact]
    public void ANullSlotDoesNothing()
    {
        UiContext context = NewContext();
        context.Update(0f);

        int calls = 0;
        context.OnInputRead = _ => calls++;
        context.Update(0f);
        Assert.Equal(1, calls);

        context.OnInputRead = null;
        context.Update(0f);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// A call of <see cref="UiContext.Update(float)"/> from the slot throws
    /// <see cref="InvalidOperationException"/>. The next update with a counting handler works, so the flag cleared
    /// in the <c>finally</c>.
    /// </summary>
    [Fact]
    public void AnUpdateFromTheSlotThrows()
    {
        UiContext context = NewContext();
        context.OnInputRead = c => c.Update(0f);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.Update(0f));
        Assert.Contains("input-read", ex.Message, StringComparison.Ordinal);

        int calls = 0;
        context.OnInputRead = _ => calls++;
        context.Update(0f);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// An exception of the slot leaves <see cref="UiContext.Update(float)"/>: the state of the frame stays, so
    /// <see cref="UiKeyState.WasPressed(int)"/> of W reads <see langword="true"/> after the throw. The next update
    /// calls the handler again.
    /// </summary>
    [Fact]
    public void AnExceptionOfTheSlotLeavesUpdate()
    {
        UiContext context = NewContext();
        bool thrown = false;
        int calls = 0;
        context.OnInputRead = _ =>
        {
            calls++;
            if (!thrown)
            {
                thrown = true;
                throw new SlotFailure();
            }
        };

        KeyEvent e = KeyDown(WKey);
        context.PushEvent(in e);

        Assert.Throws<SlotFailure>(() => context.Update(0f));
        Assert.Equal(1, calls);
        Assert.True(context.GameInput.Keys.WasPressed(WKey));

        context.Update(0f);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// An update with the slot set allocates 0 bytes after warm-up. The static handler in a
    /// <see langword="static readonly"/> field adds to a static counter, and the counter holds its calls after
    /// the measure.
    /// </summary>
    [Fact]
    public void TheSlotAllocatesNothing()
    {
        UiContext context = NewContext();
        Element root = new();
        root.Inline.Width = 100;
        root.Inline.Height = 50;
        context.AddRoot(root);
        context.OnInputRead = s_gateHandler;

        for (int i = 0; i < WarmUpRounds; i++)
        {
            context.Update(0f);
        }

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++)
        {
            context.Update(0f);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, after - before);
        Assert.Equal(WarmUpRounds + Iterations, s_gateCalls);
        s_gateCalls = 0;
    }

    /// <summary>The exception that the slot throws once.</summary>
    private sealed class SlotFailure : Exception
    {
    }
}
