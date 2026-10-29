using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the gamepad path of <see cref="UiActionMap"/>: <see cref="UiActionMap.BindGamepadButton"/> and
/// <see cref="UiActionMap.PushGamepad"/>.
/// </summary>
/// <remarks>
/// The tests push the button state by hand. No test reads a gamepad. The focus test builds a context with a
/// <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300.
/// </remarks>
public sealed class GamepadActionTests
{
    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The handler that writes the identifier of its mark into the log of the mark.</summary>
    private static readonly CommandHandler s_mark = static target => ((Mark)target!).Write();

    /// <summary>
    /// The demo of phase 3. <c>Cancel</c> has a command and a bind of <see cref="UiGamepadButtons.B"/>. The first push
    /// of <c>B</c> runs the command one time and returns 1. The second push of <c>B</c> is a held button and returns 0.
    /// </summary>
    [Fact]
    public void ANewPressInvokesItsActionOneTime()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(UiActions.Cancel, "cancel"u8, counter);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);

        int first = map.PushGamepad(UiGamepadButtons.B);
        int second = map.PushGamepad(UiGamepadButtons.B);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A button that stays down for many pushes invokes its action one time.</summary>
    [Fact]
    public void AHeldButtonInvokesNothingAgain()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(UiActions.Confirm, "confirm"u8, counter);
        map.BindGamepadButton(UiActions.Confirm, UiGamepadButtons.A);

        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.A));
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.A));
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.A | UiGamepadButtons.X));
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.A));

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A release clears the bit, so the next press of the same button invokes the action again.</summary>
    [Fact]
    public void AReleaseAndANewPressInvokeAgain()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(UiActions.Cancel, "cancel"u8, counter);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);

        map.PushGamepad(UiGamepadButtons.B);
        map.PushGamepad(UiGamepadButtons.None);
        map.PushGamepad(UiGamepadButtons.B);

        Assert.Equal(2, counter.Calls);
    }

    /// <summary>
    /// <c>B</c> is bound first and <c>A</c> second. One push with both buttons runs the commands in the order of the
    /// bind, not in the order of the bits.
    /// </summary>
    [Fact]
    public void TwoNewPressesInOnePushInvokeInTheOrderOfTheBind()
    {
        Log log = new();
        Mark markA = new(log, 1);
        Mark markB = new(log, 2);
        CommandRegistry registry = new();
        registry.Register("mark-a"u8, s_mark, markA);
        registry.Register("mark-b"u8, s_mark, markB);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(UiActions.Confirm, "mark-a"u8);
        map.AddAction(UiActions.Cancel, "mark-b"u8);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        map.BindGamepadButton(UiActions.Confirm, UiGamepadButtons.A);

        int invoked = map.PushGamepad(UiGamepadButtons.A | UiGamepadButtons.B);

        Assert.Equal(2, invoked);
        Assert.Equal(2, log.Count);
        Assert.Equal(2, log.Entries[0]);
        Assert.Equal(1, log.Entries[1]);
    }

    /// <summary>
    /// <c>DpadDown</c> is bound to <c>NavigateDown</c>, which maps to <see cref="FocusCommands.Next"/>. A push moves
    /// the focus to the first button, and a second press moves it to the second button.
    /// </summary>
    [Fact]
    public void ADpadDownBindingMovesTheFocus()
    {
        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
        Element root = new();
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        root.IsPickable = true;
        Button first = new(new Button.CreateInfo { Text = "One" });
        Button second = new(new Button.CreateInfo { Text = "Two" });
        Button third = new(new Button.CreateInfo { Text = "Three" });
        root.Children.Add(first);
        root.Children.Add(second);
        root.Children.Add(third);
        context.AddRoot(root);
        context.Update(0f);
        CommandRegistry registry = new();
        FocusCommands.Register(registry, context);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(UiActions.NavigateDown, FocusCommands.Next);
        map.BindGamepadButton(UiActions.NavigateDown, UiGamepadButtons.DpadDown);

        Assert.Null(context.FocusedElement);
        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.DpadDown));

        Assert.Same(first, context.FocusedElement);

        map.PushGamepad(UiGamepadButtons.None);
        map.PushGamepad(UiGamepadButtons.DpadDown);

        Assert.Same(second, context.FocusedElement);
    }

    /// <summary>The map never calls <see cref="UiActionMap.Activate"/>, and a push still invokes the action.</summary>
    [Fact]
    public void PushGamepadWorksWithNoActivate()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(UiActions.Confirm, "confirm"u8, counter);
        map.BindGamepadButton(UiActions.Confirm, UiGamepadButtons.Start);

        Assert.False(map.IsActive);
        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.Start));
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A button with no bind invokes nothing and the push returns 0.</summary>
    [Fact]
    public void AnUnboundButtonInvokesNothing()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(UiActions.Cancel, "cancel"u8, counter);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);

        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.X));
        Assert.Equal(0, counter.Calls);
    }

    /// <summary>Creates a map with one action and a counter command of the same target.</summary>
    /// <param name="action">The name of the action.</param>
    /// <param name="command">The name of the command.</param>
    /// <param name="counter">The target of the command.</param>
    /// <returns>The map with no bind.</returns>
    private static UiActionMap CreateMap(ReadOnlySpan<byte> action, ReadOnlySpan<byte> command, Counter counter)
    {
        CommandRegistry registry = new();
        registry.Register(command, s_count, counter);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(action, command);
        return map;
    }

    /// <summary>A target that counts the calls of its command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>A log of the identifiers that the marks wrote, in order.</summary>
    private sealed class Log
    {
        /// <summary>The identifiers in the order of the writes.</summary>
        public readonly int[] Entries = new int[8];

        /// <summary>The count of entries.</summary>
        public int Count;
    }

    /// <summary>A target that writes its identifier into a log.</summary>
    private sealed class Mark
    {
        /// <summary>The log.</summary>
        private readonly Log _log;

        /// <summary>The identifier.</summary>
        private readonly int _id;

        /// <summary>Creates a mark.</summary>
        /// <param name="log">The log.</param>
        /// <param name="id">The identifier.</param>
        public Mark(Log log, int id)
        {
            _log = log;
            _id = id;
        }

        /// <summary>Writes the identifier into the log.</summary>
        public void Write()
        {
            _log.Entries[_log.Count] = _id;
            _log.Count++;
        }
    }
}
