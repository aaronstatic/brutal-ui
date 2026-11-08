using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the slot <see cref="UiContext.OnBeforeUpdate"/> (Q1 and A2 of panels-and-modes): the call point and the
/// <c>deltaTime</c>, the tree change seen in the same update, the re-entry throw, the exception rule, the replace
/// rule of a write, and the 0-byte rule of the slot path.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test interns its class name from the
/// unique test method name. Each test builds its own context with a <see cref="RecordingRenderer"/> and a viewport
/// of 400 by 300.
/// </remarks>
public sealed class BeforeUpdateTests
{
    private const int EscapeKey = 256;
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;

    /// <summary>The background color of the class rule of <see cref="ARootThatTheSlotAddsGetsItsStyleAndLayoutInTheSameUpdate"/>.</summary>
    private static readonly Rgba8 ClassFill = new(16, 128, 64, 255);

    /// <summary>The handler of the allocation gate. The field creates the delegate one time.</summary>
    private static readonly UiBeforeUpdateHandler s_gateHandler = static _ => s_gateCalls++;

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
    /// The slot records the <c>deltaTime</c> of the call, reads the ring before the drain, and runs before the
    /// binding pass: the binding handler records its step after the slot in the shared order counter.
    /// </summary>
    [Fact]
    public void UpdateCallsTheSlotFirstWithTheDeltaTime()
    {
        UiContext context = NewContext();
        Element root = new();
        root.Inline.Width = 100;
        root.Inline.Height = 50;
        OrderLog log = new();
        root.AddBinding(new FakeSource(), ApplyOrder, log);
        context.AddRoot(root);

        float recordedDeltaTime = 0f;
        int recordedCount = 0;
        context.OnBeforeUpdate = dt =>
        {
            recordedDeltaTime = dt;
            recordedCount = context.QueuedEventCount;
            log.SlotStep = ++log.Counter;
        };

        KeyEvent e = KeyDown(EscapeKey);
        context.PushEvent(in e);
        context.Update(0.25f);

        Assert.Equal(0.25f, recordedDeltaTime);
        Assert.Equal(1, recordedCount);
        Assert.Equal(1, log.SlotStep);
        Assert.Equal(2, log.BindingStep);
        Assert.Equal(0, context.QueuedEventCount);
    }

    /// <summary>
    /// A root that the slot adds gets its style and its layout in the same update: <see cref="UiContext.Roots"/>
    /// holds it, its width is 100, and the class rule of the sheet is on it.
    /// </summary>
    [Fact]
    public void ARootThatTheSlotAddsGetsItsStyleAndLayoutInTheSameUpdate()
    {
        UiContext context = NewContext();
        ClassId probe = ClassName.Intern(nameof(ARootThatTheSlotAddsGetsItsStyleAndLayoutInTheSameUpdate) + ".probe");
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Class = probe }),
                new StyleProperties { BackgroundColor = ClassFill })
            .Build();
        context.AddSheet(sheet);

        Element root = new();
        root.Inline.Width = 100;
        root.Inline.Height = 50;
        root.Classes.Add(probe);
        context.OnBeforeUpdate = _ => context.AddRoot(root);

        context.Update(0f);

        Assert.Equal(1, context.Roots.Length);
        Assert.Same(root, context.Roots[0]);
        Assert.Equal(100f, root.Layout.Rect.Z);
        Assert.Equal(ClassFill, root.Style.BackgroundColor);
    }

    /// <summary>
    /// A call of <see cref="UiContext.Update(float)"/> from the slot throws
    /// <see cref="InvalidOperationException"/> and leaves the context valid. The slot calls <c>Update</c> only at
    /// its first call, so a missing guard gives a failed assert and no stack overflow. After the slot is
    /// <see langword="null"/>, the next update drains the ring and works.
    /// </summary>
    [Fact]
    public void AnUpdateFromTheSlotThrowsAndTheContextStaysValid()
    {
        UiContext context = NewContext();
        Element root = new();
        root.Inline.Width = 100;
        root.Inline.Height = 50;
        context.AddRoot(root);

        int slotCalls = 0;
        context.OnBeforeUpdate = _ =>
        {
            slotCalls++;
            if (slotCalls == 1)
            {
                context.Update(0f);
            }
        };

        KeyEvent e = KeyDown(EscapeKey);
        context.PushEvent(in e);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.Update(0f));
        Assert.Contains("before-update", ex.Message, StringComparison.Ordinal);
        Assert.Equal(1, slotCalls);
        Assert.Equal(1, context.QueuedEventCount);

        context.OnBeforeUpdate = null;
        context.Update(0f);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(100f, root.Layout.Rect.Z);
    }

    /// <summary>
    /// An exception of the slot leaves <see cref="UiContext.Update(float)"/> before the input pass: the ring keeps
    /// its entry and a layout change stays unapplied. The next update calls the slot, drains the ring, and lays
    /// out the root.
    /// </summary>
    [Fact]
    public void AnExceptionOfTheSlotLeavesUpdateBeforeTheInputPass()
    {
        UiContext context = NewContext();
        Element root = new();
        root.Inline.Width = 100;
        root.Inline.Height = 50;
        context.AddRoot(root);
        context.Update(0f);
        Assert.Equal(100f, root.Layout.Rect.Z);

        bool thrown = false;
        int slotCalls = 0;
        context.OnBeforeUpdate = _ =>
        {
            slotCalls++;
            if (!thrown)
            {
                thrown = true;
                throw new SlotFailure();
            }
        };

        root.Inline.Width = 200;
        KeyEvent e = KeyDown(EscapeKey);
        context.PushEvent(in e);

        Assert.Throws<SlotFailure>(() => context.Update(0f));
        Assert.Equal(1, slotCalls);
        Assert.Equal(1, context.QueuedEventCount);
        Assert.Equal(100f, root.Layout.Rect.Z);

        context.Update(0f);
        Assert.Equal(2, slotCalls);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(200f, root.Layout.Rect.Z);
    }

    /// <summary>
    /// A write with <c>=</c> replaces the delegate in the slot: the update calls the last write only. A
    /// <see langword="null"/> slot calls nothing.
    /// </summary>
    [Fact]
    public void AWriteReplacesTheDelegateInTheSlot()
    {
        UiContext context = NewContext();
        int callsA = 0;
        int callsB = 0;
        context.OnBeforeUpdate = _ => callsA++;
        context.OnBeforeUpdate = _ => callsB++;

        context.Update(0f);
        Assert.Equal(0, callsA);
        Assert.Equal(1, callsB);

        context.OnBeforeUpdate = null;
        context.Update(0f);
        Assert.Equal(0, callsA);
        Assert.Equal(1, callsB);
    }

    /// <summary>
    /// An update with the slot set allocates 0 bytes after warm-up. The static handler in a
    /// <see langword="static readonly"/> field adds to a static counter, and the counter holds its calls after
    /// the measure.
    /// </summary>
    [Fact]
    public void AnUpdateWithASlotAllocatesNothingAfterWarmUp()
    {
        UiContext context = NewContext();
        Element root = new();
        root.Inline.Width = 100;
        root.Inline.Height = 50;
        context.AddRoot(root);
        context.OnBeforeUpdate = s_gateHandler;

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

    /// <summary>The order log of the slot and the binding handler.</summary>
    private sealed class OrderLog
    {
        /// <summary>The shared step counter.</summary>
        public int Counter;

        /// <summary>The step of the slot call.</summary>
        public int SlotStep;

        /// <summary>The step of the last binding handler call.</summary>
        public int BindingStep;
    }

    /// <summary>A source with a new version 1, as the version of a store.</summary>
    private sealed class FakeSource : IBindingSource
    {
        /// <inheritdoc />
        public ulong Version { get; } = 1;
    }

    /// <summary>The binding handler that records the step after the slot.</summary>
    /// <param name="element">The bound element. The handler writes nothing.</param>
    /// <param name="target">The order log.</param>
    private static void ApplyOrder(Element element, object? target)
    {
        OrderLog log = (OrderLog)target!;
        log.BindingStep = ++log.Counter;
    }

    /// <summary>The exception that the slot throws once.</summary>
    private sealed class SlotFailure : Exception
    {
    }
}
