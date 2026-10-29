using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="CommandBindings"/>: the click demo, the Enter key, the order with the handler of the game,
/// the late register, and the argument checks.
/// </summary>
/// <remarks>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer, a root of 400 by 300, and one button of
/// 80 by 24 at the top left corner of the root. The tests build the input events by hand, because the event factories
/// of the Core test project are internal.
/// </remarks>
public sealed class CommandBindingTests
{
    /// <summary>The GLFW key code of the Enter key.</summary>
    private const int EnterKey = 257;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>
    /// The demo. A press and a release at the center of a bound button call the handler one time with its target.
    /// </summary>
    [Fact]
    public void AClickOnABoundButtonInvokesTheCommand()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);
        Button button = CreateButton(root);

        Button returned = button.BindCommand(registry, "save"u8);

        Assert.Same(button, returned);

        context.Update(0f);
        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>An Enter key on the focused bound button invokes the command.</summary>
    [Fact]
    public void AnEnterOnAFocusedBoundButtonInvokesTheCommand()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);
        Button button = CreateButton(root).BindCommand(registry, "save"u8);
        context.Update(0f);
        Assert.True(context.SetFocus(button));

        context.PushEvent(Key(EnterKey));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A handler that the game set before the bind runs first. The command runs second.</summary>
    [Fact]
    public void AClickedHandlerOfTheGameStillRunsBeforeTheCommand()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        List<string> order = new();
        CommandRegistry registry = new();
        registry.Register("save"u8, static target => ((List<string>)target!).Add("command"), order);
        Button button = CreateButton(root);
        button.Clicked = _ => order.Add("game");
        button.BindCommand(registry, "save"u8);
        context.Update(0f);

        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(new[] { "game", "command" }, order);
    }

    /// <summary>A command that the game registers after the bind runs at the next click.</summary>
    [Fact]
    public void ACommandThatIsRegisteredAfterTheBindRuns()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        CommandRegistry registry = new();
        Button button = CreateButton(root).BindCommand(registry, "save"u8);
        context.Update(0f);
        float2 center = new(40f, 12f);

        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Counter counter = new();
        registry.Register("save"u8, s_count, counter);

        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
        Assert.NotNull(button);
    }

    /// <summary>A null button or a null registry throws.</summary>
    [Fact]
    public void BindCommandRejectsANullButtonOrRegistry()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Button button = CreateButton(root);

        ArgumentNullException noButton = Assert.Throws<ArgumentNullException>(
            () => CommandBindings.BindCommand(null!, new CommandRegistry(), "save"u8));
        ArgumentNullException noRegistry = Assert.Throws<ArgumentNullException>(
            () => button.BindCommand(null!, "save"u8));

        Assert.Equal("button", noButton.ParamName);
        Assert.Equal("registry", noRegistry.ParamName);
    }

    /// <summary>
    /// The demo. A click on a button bound to an unknown name reports the name through the slot and calls no handler.
    /// </summary>
    [Fact]
    public void AClickOnAnUnknownNameCallsTheUnknownCommandSlot()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);
        List<byte[]> misses = new();
        registry.UnknownCommand = name => misses.Add(name.ToArray());
        CreateButton(root).BindCommand(registry, "load"u8);
        context.Update(0f);

        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Single(misses);
        Assert.Equal("load"u8.ToArray(), misses[0]);
        Assert.Equal(0, counter.Calls);
    }

    /// <summary>The bind copies the name, so a change of the source array does not change the command.</summary>
    [Fact]
    public void BindCommandCopiesTheName()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("save"u8, s_count, counter);
        byte[] source = "save"u8.ToArray();
        CreateButton(root).BindCommand(registry, source);
        source[0] = (byte)'X';
        context.Update(0f);

        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>An empty name throws <see cref="ArgumentException"/> with the parameter name <c>name</c>.</summary>
    [Fact]
    public void BindCommandRejectsAnEmptyName()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Button button = CreateButton(root);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => button.BindCommand(new CommandRegistry(), ReadOnlySpan<byte>.Empty));

        Assert.Equal("name", exception.ParamName);
    }

    /// <summary>Two binds on one button invoke both commands, in the order of the bind.</summary>
    [Fact]
    public void TwoBindsOnOneButtonInvokeBothCommandsInOrder()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        List<string> order = new();
        CommandRegistry registry = new();
        registry.Register("first"u8, static target => ((List<string>)target!).Add("first"), order);
        registry.Register("second"u8, static target => ((List<string>)target!).Add("second"), order);
        CreateButton(root).BindCommand(registry, "first"u8).BindCommand(registry, "second"u8);
        context.Update(0f);

        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(new[] { "first", "second" }, order);
    }

    /// <summary>
    /// A command that sets a store runs in the input pass. The label shows the value in the binding pass of the same
    /// <see cref="UiContext.Update"/>.
    /// </summary>
    [Fact]
    public void ACommandThatSetsAStoreShowsInTheSameUpdate()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<int> store = new(new Store<int>.CreateInfo { Initial = 0 });
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static value => value);
        CommandRegistry registry = new();
        registry.Register("set"u8, static target => ((Store<int>)target!).Set(7), store);
        CreateButton(root).BindCommand(registry, "set"u8);
        root.Children.Add(label);
        context.Update(0f);
        Assert.Equal("0"u8.ToArray(), label.Text.ToArray());

        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(7, store.Value);
        Assert.Equal("7"u8.ToArray(), label.Text.ToArray());
    }

    /// <summary>The target of the tests: a call counter.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>Creates a context with a measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>Adds a pickable root of 400 by 300 to a context.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The root.</returns>
    private static Element AddRoot(UiContext context)
    {
        Element root = new();
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        root.IsPickable = true;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Adds a button of 80 by 24 to a root.</summary>
    /// <param name="root">The root.</param>
    /// <returns>The button.</returns>
    private static Button CreateButton(Element root)
    {
        Button button = new(default(Button.CreateInfo));
        button.Inline.Width = 80;
        button.Inline.Height = 24;
        root.Children.Add(button);
        return button;
    }

    /// <summary>Creates a left-button pointer event with no target.</summary>
    /// <param name="type"><see cref="UiEventType.PointerDown"/> or <see cref="UiEventType.PointerUp"/>.</param>
    /// <param name="position">The position in viewport pixels.</param>
    /// <returns>The event.</returns>
    private static PointerEvent Pointer(UiEventType type, float2 position)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>Creates a key down event with no target, no modifier, and no repeat.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Key(int key)
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
}
