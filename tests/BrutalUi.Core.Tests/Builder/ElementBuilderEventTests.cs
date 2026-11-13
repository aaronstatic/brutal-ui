using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the event group of <see cref="ElementBuilder"/>: the fourteen delegate methods, the slot each one
/// fills, the <c>+=</c> rule, the null checks, and one routed pointer press.
/// </summary>
public sealed class ElementBuilderEventTests
{
    /// <summary>Each of the fourteen methods writes its own slot only, and the other thirteen slots stay null.</summary>
    [Fact]
    public void EachEventMethodAddsTheHandlerToItsSlot()
    {
        UiEventHandler<PointerEvent> pointer = PointerHandler;
        UiEventHandler<ScrollEvent> scroll = ScrollHandler;
        UiEventHandler<KeyEvent> key = KeyHandler;
        UiEventHandler<CharEvent> character = CharHandler;

        Element element = new();
        element.PointerMove(pointer);
        AssertOnlySlotHolds(element, 0, pointer);

        element = new();
        element.PointerMoveCapture(pointer);
        AssertOnlySlotHolds(element, 1, pointer);

        element = new();
        element.PointerDown(pointer);
        AssertOnlySlotHolds(element, 2, pointer);

        element = new();
        element.PointerDownCapture(pointer);
        AssertOnlySlotHolds(element, 3, pointer);

        element = new();
        element.PointerUp(pointer);
        AssertOnlySlotHolds(element, 4, pointer);

        element = new();
        element.PointerUpCapture(pointer);
        AssertOnlySlotHolds(element, 5, pointer);

        element = new();
        element.Scroll(scroll);
        AssertOnlySlotHolds(element, 6, scroll);

        element = new();
        element.ScrollCapture(scroll);
        AssertOnlySlotHolds(element, 7, scroll);

        element = new();
        element.KeyDown(key);
        AssertOnlySlotHolds(element, 8, key);

        element = new();
        element.KeyDownCapture(key);
        AssertOnlySlotHolds(element, 9, key);

        element = new();
        element.KeyUp(key);
        AssertOnlySlotHolds(element, 10, key);

        element = new();
        element.KeyUpCapture(key);
        AssertOnlySlotHolds(element, 11, key);

        element = new();
        element.Char(character);
        AssertOnlySlotHolds(element, 12, character);

        element = new();
        element.CharCapture(character);
        AssertOnlySlotHolds(element, 13, character);
    }

    /// <summary>A method adds with <c>+=</c>, so a handler that the game set with <c>=</c> before stays first.</summary>
    [Fact]
    public void AnEventMethodKeepsAHandlerThatTheGameSetBefore()
    {
        Element element = new();
        UiEventHandler<KeyEvent> game = GameKeyHandler;
        UiEventHandler<KeyEvent> added = KeyHandler;
        element.OnKeyDown = game;

        element.KeyDown(added);

        Delegate[] slots = element.OnKeyDown!.GetInvocationList();
        Assert.Equal(2, slots.Length);
        Assert.Same(game, slots[0]);
        Assert.Same(added, slots[1]);
    }

    /// <summary>Each of the fourteen methods throws for a null element and for a null handler.</summary>
    [Fact]
    public void AnEventMethodThrowsForANullElementOrANullHandler()
    {
        Element nullElement = null!;
        UiEventHandler<PointerEvent> pointer = PointerHandler;
        UiEventHandler<ScrollEvent> scroll = ScrollHandler;
        UiEventHandler<KeyEvent> key = KeyHandler;
        UiEventHandler<CharEvent> character = CharHandler;

        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.PointerMove(pointer)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.PointerMoveCapture(pointer)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.PointerDown(pointer)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.PointerDownCapture(pointer)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.PointerUp(pointer)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.PointerUpCapture(pointer)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Scroll(scroll)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.ScrollCapture(scroll)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.KeyDown(key)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.KeyDownCapture(key)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.KeyUp(key)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.KeyUpCapture(key)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.Char(character)).ParamName);
        Assert.Equal("element", Assert.Throws<ArgumentNullException>(() => nullElement.CharCapture(character)).ParamName);

        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().PointerMove(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().PointerMoveCapture(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().PointerDown(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().PointerDownCapture(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().PointerUp(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().PointerUpCapture(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().Scroll(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().ScrollCapture(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().KeyDown(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().KeyDownCapture(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().KeyUp(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().KeyUpCapture(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().Char(null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => new Element().CharCapture(null!)).ParamName);
    }

    /// <summary>
    /// A handler that the builder adds runs once on a routed pointer press, in the pattern of
    /// <see cref="EventWalkTests"/>.
    /// </summary>
    [Fact]
    public void ABuilderHandlerRunsOnARoutedEvent()
    {
        Element target = new() { Name = "target", IsPickable = true };
        UiContext context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() });
        context.AddRoot(target);

        int calls = 0;
        Element same = target.PointerDown((ref PointerEvent e) => calls++);
        Assert.True(ReferenceEquals(target, same));

        context.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerDown, 1.5, target),
            Position = new float2(10, 20),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
        int walks = context.DrainEvents();

        Assert.Equal(1, walks);
        Assert.Equal(1, calls);
    }

    /// <summary>Checks that slot <paramref name="index"/> holds the handler and the other thirteen stay null.</summary>
    private static void AssertOnlySlotHolds(Element element, int index, Delegate expected)
    {
        Delegate?[] slots =
        [
            element.OnPointerMove,
            element.OnPointerMoveCapture,
            element.OnPointerDown,
            element.OnPointerDownCapture,
            element.OnPointerUp,
            element.OnPointerUpCapture,
            element.OnScroll,
            element.OnScrollCapture,
            element.OnKeyDown,
            element.OnKeyDownCapture,
            element.OnKeyUp,
            element.OnKeyUpCapture,
            element.OnChar,
            element.OnCharCapture,
        ];

        Assert.Equal(14, slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == index)
            {
                Assert.Same(expected, slots[i]);
            }
            else
            {
                Assert.Null(slots[i]);
            }
        }
    }

    private static void PointerHandler(ref PointerEvent e)
    {
    }

    private static void ScrollHandler(ref ScrollEvent e)
    {
    }

    private static void KeyHandler(ref KeyEvent e)
    {
    }

    private static void GameKeyHandler(ref KeyEvent e)
    {
    }

    private static void CharHandler(ref CharEvent e)
    {
    }
}
