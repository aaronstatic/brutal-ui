using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="FocusCommands"/>: the focus actions of a <see cref="UiActionMap"/> on a root with three
/// focusable buttons.
/// </summary>
/// <remarks>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. The root
/// holds three buttons. One <see cref="UiContext.Update(float)"/> runs the layout before the first focus move. The tests
/// build the input events by hand, because the event factories of the Core test project are internal.
/// </remarks>
public sealed class FocusActionTests
{
    /// <summary>The GLFW key code of the Down arrow.</summary>
    private const int DownKey = 264;

    /// <summary>The GLFW key code of the Up arrow.</summary>
    private const int UpKey = 265;

    /// <summary>
    /// The demo of phase 2. Down is bound to <c>NavigateDown</c>, which maps to <see cref="FocusCommands.Next"/>. With
    /// the focus on the first of three buttons, one press of Down moves the focus to the second button.
    /// </summary>
    [Fact]
    public void NavigateDownMovesTheFocusToTheNextElement()
    {
        Scene scene = CreateScene();
        UiActionMap map = CreateMap(scene.Context);
        map.BindKey(UiActions.NavigateDown, DownKey);
        map.Activate(scene.Context);
        Assert.True(scene.Context.SetFocus(scene.First));

        scene.Context.PushEvent(Key(DownKey));
        scene.Context.Update(0f);

        Assert.Same(scene.Second, scene.Context.FocusedElement);
    }

    /// <summary>Up is bound to <c>NavigateUp</c>. With the focus on the second button, one press moves the focus to the first button.</summary>
    [Fact]
    public void NavigateUpMovesTheFocusToThePreviousElement()
    {
        Scene scene = CreateScene();
        UiActionMap map = CreateMap(scene.Context);
        map.BindKey(UiActions.NavigateUp, UpKey);
        map.Activate(scene.Context);
        Assert.True(scene.Context.SetFocus(scene.Second));

        scene.Context.PushEvent(Key(UpKey));
        scene.Context.Update(0f);

        Assert.Same(scene.First, scene.Context.FocusedElement);

        scene.Context.PushEvent(Key(UpKey));
        scene.Context.Update(0f);

        Assert.Same(scene.Third, scene.Context.FocusedElement);
    }

    /// <summary>
    /// With no owner, Down gives the focus to the first button. Down at the last button wraps the focus to the first
    /// button.
    /// </summary>
    [Fact]
    public void TheFocusActionsStartWithNoOwnerAndWrap()
    {
        Scene scene = CreateScene();
        UiActionMap map = CreateMap(scene.Context);
        map.BindKey(UiActions.NavigateDown, DownKey);
        map.Activate(scene.Context);
        Assert.Null(scene.Context.FocusedElement);

        scene.Context.PushEvent(Key(DownKey));
        scene.Context.Update(0f);

        Assert.Same(scene.First, scene.Context.FocusedElement);

        Assert.True(scene.Context.SetFocus(scene.Third));
        scene.Context.PushEvent(Key(DownKey));
        scene.Context.Update(0f);

        Assert.Same(scene.First, scene.Context.FocusedElement);
    }

    /// <summary>
    /// <see cref="FocusCommands.Register"/> adds two commands. Both names exist, and an invoke with no map moves the
    /// focus, so the context is the target.
    /// </summary>
    [Fact]
    public void RegisterAddsTwoCommandsWithTheContextAsTheTarget()
    {
        Scene scene = CreateScene();
        CommandRegistry registry = new();
        int before = registry.Count;

        FocusCommands.Register(registry, scene.Context);

        Assert.Equal(before + 2, registry.Count);
        Assert.True(registry.Contains(FocusCommands.Next));
        Assert.True(registry.Contains(FocusCommands.Previous));
        Assert.Equal("ui.focus-next"u8.ToArray(), FocusCommands.Next.ToArray());
        Assert.Equal("ui.focus-previous"u8.ToArray(), FocusCommands.Previous.ToArray());

        Assert.True(registry.Invoke(FocusCommands.Next));

        Assert.Same(scene.First, scene.Context.FocusedElement);

        Assert.True(registry.Invoke(FocusCommands.Next));

        Assert.Same(scene.Second, scene.Context.FocusedElement);

        Assert.True(registry.Invoke(FocusCommands.Previous));

        Assert.Same(scene.First, scene.Context.FocusedElement);
    }

    /// <summary><see cref="FocusCommands.Register"/> throws for a <see langword="null"/> registry and for a <see langword="null"/> context.</summary>
    [Fact]
    public void RegisterRejectsANullRegistryOrContext()
    {
        Scene scene = CreateScene();
        CommandRegistry registry = new();

        Assert.Throws<ArgumentNullException>(() => FocusCommands.Register(null!, scene.Context));
        Assert.Throws<ArgumentNullException>(() => FocusCommands.Register(registry, null!));
        Assert.Equal(0, registry.Count);
    }

    /// <summary>Creates a registry with the focus commands and a map with the two focus actions.</summary>
    /// <param name="context">The context that the commands move the focus of.</param>
    /// <returns>The map with no bind.</returns>
    private static UiActionMap CreateMap(UiContext context)
    {
        CommandRegistry registry = new();
        FocusCommands.Register(registry, context);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(UiActions.NavigateDown, FocusCommands.Next);
        map.AddAction(UiActions.NavigateUp, FocusCommands.Previous);
        return map;
    }

    /// <summary>Creates a context with a root that holds three buttons, and runs one update for the layout.</summary>
    /// <returns>The scene.</returns>
    private static Scene CreateScene()
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
        return new Scene { Context = context, First = first, Second = second, Third = third };
    }

    /// <summary>Creates a key down event with no target.</summary>
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

    /// <summary>A context with three focusable buttons.</summary>
    private sealed class Scene
    {
        /// <summary>The context.</summary>
        public required UiContext Context;

        /// <summary>The first button in the paint order.</summary>
        public required Button First;

        /// <summary>The second button.</summary>
        public required Button Second;

        /// <summary>The last button.</summary>
        public required Button Third;
    }
}
