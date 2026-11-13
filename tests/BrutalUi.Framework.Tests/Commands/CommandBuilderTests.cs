using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="CommandBindings.Command"/>: the click, the order with the handler of the game, the argument
/// checks, the typed return, and the section 11 example.
/// </summary>
/// <remarks>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer, a root of 400 by 300, and one button of
/// 80 by 24 at the top left corner of the root. The tests build the input events by hand, because the event factories
/// of the Core test project are internal.
/// </remarks>
public sealed class CommandBuilderTests
{
    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>One click on a button built with <c>Command</c> invokes the registered command one time.</summary>
    [Fact]
    public void CommandInvokesTheCommandOnAClick()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("game.pause"u8, s_count, counter);
        Button button = CreateButton(root);

        button.Command(registry, "game.pause"u8);

        context.Update(0f);
        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A handler that the game set before the bind runs first. The command runs second.</summary>
    [Fact]
    public void CommandKeepsAHandlerThatTheGameSetBefore()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        List<string> order = new();
        CommandRegistry registry = new();
        registry.Register("game.pause"u8, static target => ((List<string>)target!).Add("command"), order);
        Button button = CreateButton(root);
        button.Clicked = _ => order.Add("game");

        button.Command(registry, "game.pause"u8);

        context.Update(0f);
        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(new[] { "game", "command" }, order);
    }

    /// <summary>A null button, a null registry, and an empty name each throw with their parameter name.</summary>
    [Fact]
    public void CommandChecksTheButtonTheRegistryAndTheName()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Button button = CreateButton(root);

        ArgumentNullException noButton = Assert.Throws<ArgumentNullException>(
            () => CommandBindings.Command(null!, new CommandRegistry(), "game.pause"u8));
        ArgumentNullException noRegistry = Assert.Throws<ArgumentNullException>(
            () => button.Command(null!, "game.pause"u8));
        ArgumentException noName = Assert.Throws<ArgumentException>(
            () => button.Command(new CommandRegistry(), ReadOnlySpan<byte>.Empty));

        Assert.Equal("button", noButton.ParamName);
        Assert.Equal("registry", noRegistry.ParamName);
        Assert.Equal("name", noName.ParamName);
    }

    /// <summary>The method returns the same button.</summary>
    [Fact]
    public void CommandReturnsTheSameButton()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Button button = CreateButton(root);
        CommandRegistry registry = new();

        Button returned = button.Command(registry, "game.pause"u8);

        Assert.True(ReferenceEquals(button, returned));
    }

    /// <summary>
    /// <c>OnClicked</c> and <c>Command</c> each keep their delegate in each order. One click runs the handler and the
    /// command, in the order of the chain.
    /// </summary>
    [Fact]
    public void CommandAndOnClickedKeepBothDelegatesInEachOrder()
    {
        List<string> handlerFirstOrder = new();
        UiContext handlerFirstContext = CreateContext();
        Element handlerFirstRoot = AddRoot(handlerFirstContext);
        CommandRegistry handlerFirstRegistry = new();
        handlerFirstRegistry.Register("game.pause"u8, static target => ((List<string>)target!).Add("command"), handlerFirstOrder);
        Button handlerFirst = CreateButton(handlerFirstRoot);

        handlerFirst.OnClicked(_ => handlerFirstOrder.Add("handler")).Command(handlerFirstRegistry, "game.pause"u8);

        Assert.Equal(2, handlerFirst.Clicked!.GetInvocationList().Length);

        Click(handlerFirstContext);

        Assert.Equal(new[] { "handler", "command" }, handlerFirstOrder);

        List<string> commandFirstOrder = new();
        UiContext commandFirstContext = CreateContext();
        Element commandFirstRoot = AddRoot(commandFirstContext);
        CommandRegistry commandFirstRegistry = new();
        commandFirstRegistry.Register("game.pause"u8, static target => ((List<string>)target!).Add("command"), commandFirstOrder);
        Button commandFirst = CreateButton(commandFirstRoot);

        commandFirst.Command(commandFirstRegistry, "game.pause"u8).OnClicked(_ => commandFirstOrder.Add("handler"));

        Assert.Equal(2, commandFirst.Clicked!.GetInvocationList().Length);

        Click(commandFirstContext);

        Assert.Equal(new[] { "command", "handler" }, commandFirstOrder);
    }

    /// <summary>
    /// The demo. The section 11 example of <c>docs/technical-design.md</c> builds the same tree as the initializer
    /// form, and one click on its <c>pause</c> button invokes <c>game.pause</c> one time.
    /// </summary>
    [Fact]
    public void TheSectionElevenExampleBuildsTheSameTreeAsInitializers()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("game.pause"u8, s_count, counter);

        var panel = new Element().Class("hud")
            .Child(new Label(default).Name("score").Class("hud-value").Text("0"u8))
            .Child(new Button(default).Name("pause").Class("hud-button").Text("Pause"u8).Command(registry, "game.pause"u8));

        Element reference = new();
        reference.Classes.Add(ClassName.Intern("hud"));
        Label score = new Label(new Label.CreateInfo { Text = "0" }) { Name = "score" };
        score.Classes.Add(ClassName.Intern("hud-value"));
        Button pause = new Button(new Button.CreateInfo { Text = "Pause" }) { Name = "pause" };
        pause.Classes.Add(ClassName.Intern("hud-button"));
        pause.BindCommand(registry, "game.pause"u8);
        reference.Children.Add(score);
        reference.Children.Add(pause);

        Assert.Null(TreeCompare.FirstDifference(reference, panel));

        panel.Inline.FlexDirection = FlexDirection.Column;
        panel.Inline.AlignItems = Align.FlexStart;
        panel.Inline.Width = 400;
        panel.Inline.Height = 300;
        panel.Children[0].Inline.Width = 100;
        panel.Children[0].Inline.Height = 20;
        panel.Children[1].Inline.Width = 80;
        panel.Children[1].Inline.Height = 24;
        UiContext context = CreateContext();
        context.AddRoot(panel);
        context.Update(0f);

        float2 center = new(40f, 32f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
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

    /// <summary>Pushes one left click at the center of the test button of a context, then updates.</summary>
    /// <param name="context">The context with the root and the button.</param>
    private static void Click(UiContext context)
    {
        context.Update(0f);
        float2 center = new(40f, 12f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);
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
}
