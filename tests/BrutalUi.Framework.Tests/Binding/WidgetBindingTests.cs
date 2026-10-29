using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="WidgetBindings"/>: the first apply, the apply after a store change, the No-Loop Rule, and
/// the argument checks.
/// </summary>
/// <remarks>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer, a root of 400 by 300, and one widget
/// with an inline size. The widget sits at the top left corner of the root. Each widget gets a counter delegate, so a
/// test can count the calls. Each selector is a <see langword="static"/> lambda, as a game uses it.
/// </para>
/// <para>
/// The tests build the input events by hand, because the event factories of the Core test project are internal.
/// </para>
/// </remarks>
public sealed class WidgetBindingTests
{
    /// <summary>The GLFW key code of the Right key.</summary>
    private const int RightKey = 262;

    /// <summary>The GLFW key code of the Left key.</summary>
    private const int LeftKey = 263;

    /// <summary>The state of the test form: one slice for each widget.</summary>
    /// <param name="Muted">The state of the toggle.</param>
    /// <param name="Volume">The value of the slider.</param>
    /// <param name="Name">The text of the field.</param>
    private readonly record struct FormState(bool Muted, float Volume, string Name);

    /// <summary>A bound toggle shows the slice at the first update, not before it.</summary>
    [Fact]
    public void ABoundToggleTakesTheSliceAtTheFirstUpdate()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(true, 0f, "");
        Toggle toggle = CreateToggle(root);

        Toggle returned = toggle.BindChecked(store, static s => s.Muted);

        Assert.Same(toggle, returned);
        Assert.Equal(1, toggle.BindingCount);
        Assert.False(toggle.Checked);

        context.Update(0f);

        Assert.True(toggle.Checked);
    }

    /// <summary>A store change flips the toggle at the next update and calls no delegate.</summary>
    [Fact]
    public void AStoreChangeFlipsTheToggleAndCallsNoDelegate()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(true, 0f, "");
        Toggle toggle = CreateToggle(root).BindChecked(store, static s => s.Muted);
        int calls = 0;
        toggle.CheckedChanged = _ => calls++;
        context.Update(0f);

        store.Set(store.Value with { Muted = false });

        Assert.True(toggle.Checked);

        context.Update(0f);

        Assert.False(toggle.Checked);
        Assert.Equal(0, calls);
    }

    /// <summary>A bound slider takes the slice through its value rule: the step 1 rounds 50.4 to 50.</summary>
    [Fact]
    public void ABoundSliderTakesTheSliceThroughTheValueRule()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(false, 50.4f, "");
        Slider slider = CreateSlider(root);

        Slider returned = slider.BindValue(store, static s => s.Volume);

        Assert.Same(slider, returned);
        Assert.Equal(1, slider.BindingCount);

        context.Update(0f);

        Assert.Equal(50f, slider.Value);
    }

    /// <summary>A store change moves the slider at the next update and calls no delegate.</summary>
    [Fact]
    public void AStoreChangeMovesTheSliderAndCallsNoDelegate()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(false, 20f, "");
        Slider slider = CreateSlider(root).BindValue(store, static s => s.Volume);
        int calls = 0;
        slider.ValueChanged = _ => calls++;
        context.Update(0f);

        store.Set(store.Value with { Volume = 70f });

        Assert.Equal(20f, slider.Value);

        context.Update(0f);

        Assert.Equal(70f, slider.Value);
        Assert.Equal(0, calls);
    }

    /// <summary>A bound text field takes the string slice at the first update.</summary>
    [Fact]
    public void ABoundTextFieldTakesTheStringSlice()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(false, 0f, "Ada");
        TextField field = CreateField(root);

        TextField returned = field.BindText(store, static s => s.Name);

        Assert.Same(field, returned);
        Assert.Equal(1, field.BindingCount);
        Assert.True(field.Text.IsEmpty);

        context.Update(0f);

        Assert.Equal("Ada", Encoding.UTF8.GetString(field.Text));
    }

    /// <summary>A store change sets the text of the field at the next update and calls no delegate.</summary>
    [Fact]
    public void AStoreChangeSetsTheFieldTextAndCallsNoDelegate()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(false, 0f, "Ada");
        TextField field = CreateField(root).BindText(store, static s => s.Name);
        int calls = 0;
        field.TextChanged = _ => calls++;
        context.Update(0f);

        store.Set(store.Value with { Name = "Grace" });
        context.Update(0f);

        Assert.Equal("Grace", Encoding.UTF8.GetString(field.Text));
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// The demo of the No-Loop Rule. A press and a release at the center of the toggle flip it. The delegate writes the
    /// flip to the store. After two updates the delegate has one call, the store has one new version, and the toggle is
    /// checked.
    /// </summary>
    [Fact]
    public void AUserFlipWritesTheStoreOneTimeWithNoLoop()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(false, 0f, "");
        Toggle toggle = CreateToggle(root).BindChecked(store, static s => s.Muted);
        int calls = 0;
        toggle.CheckedChanged = t =>
        {
            calls++;
            store.Set(store.Value with { Muted = t.Checked });
        };
        context.Update(0f);
        ulong version = store.Version;

        float2 center = new(10f, 10f);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(1, calls);
        Assert.Equal(version + 1, store.Version);
        Assert.True(toggle.Checked);
        Assert.True(store.Value.Muted);
    }

    /// <summary>A Right key on the focused slider moves it one step. The delegate writes the store one time.</summary>
    [Fact]
    public void AUserKeyOnTheSliderWritesTheStoreOneTimeWithNoLoop()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(false, 50f, "");
        Slider slider = CreateSlider(root).BindValue(store, static s => s.Volume);
        int calls = 0;
        slider.ValueChanged = s =>
        {
            calls++;
            store.Set(store.Value with { Volume = s.Value });
        };
        context.Update(0f);
        Assert.True(context.SetFocus(slider));
        ulong version = store.Version;

        context.PushEvent(Key(RightKey));
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(1, calls);
        Assert.Equal(version + 1, store.Version);
        Assert.Equal(51f, slider.Value);
        Assert.Equal(51f, store.Value.Volume);
    }

    /// <summary>
    /// A char in the focused field inserts the char. The delegate writes the text to the store one time. The binding
    /// then sets the same text and keeps the caret, so a second char follows the first. A char typed in the middle of the
    /// text stays in the middle after the binding pass.
    /// </summary>
    [Fact]
    public void AUserCharInTheFieldWritesTheStoreOneTimeWithNoLoop()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Store<FormState> store = CreateStore(false, 0f, "");
        TextField field = CreateField(root).BindText(store, static s => s.Name);
        int calls = 0;
        field.TextChanged = f =>
        {
            calls++;
            store.Set(store.Value with { Name = Encoding.UTF8.GetString(f.Text) });
        };
        context.Update(0f);
        Assert.True(context.SetFocus(field));
        ulong version = store.Version;

        context.PushEvent(Char('a'));
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(1, calls);
        Assert.Equal(version + 1, store.Version);
        Assert.Equal("a", Encoding.UTF8.GetString(field.Text));
        Assert.Equal("a", store.Value.Name);

        // The caret stays at the end: the next char goes after the first char.
        context.PushEvent(Char('b'));
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(2, calls);
        Assert.Equal("ab", Encoding.UTF8.GetString(field.Text));
        Assert.Equal("ab", store.Value.Name);

        // The caret moves into the middle: the Left key puts it between a and b. A caret reset by the binding would
        // put it at the end again, so the next char would follow the b.
        context.PushEvent(Key(LeftKey));
        context.PushEvent(Char('x'));
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(3, calls);
        Assert.Equal("axb", Encoding.UTF8.GetString(field.Text));
        Assert.Equal("axb", store.Value.Name);

        context.PushEvent(Char('y'));
        context.Update(0f);
        context.Update(0f);

        Assert.Equal(4, calls);
        Assert.Equal("axyb", Encoding.UTF8.GetString(field.Text));
        Assert.Equal("axyb", store.Value.Name);
    }

    /// <summary>Each widget bind method rejects a null store or a null selector, and the widget then holds no binding.</summary>
    [Fact]
    public void EachWidgetBindMethodRejectsANullStoreOrSelector()
    {
        Store<FormState> store = CreateStore(false, 0f, "");
        Toggle toggle = new(default(Toggle.CreateInfo));
        Slider slider = new(new Slider.CreateInfo { Maximum = 100f });
        TextField field = new(new TextField.CreateInfo { Capacity = 16 });

        Assert.Throws<ArgumentNullException>("store", () => toggle.BindChecked<FormState>(null!, static s => s.Muted));
        Assert.Throws<ArgumentNullException>("selector", () => toggle.BindChecked(store, null!));
        Assert.Throws<ArgumentNullException>("store", () => slider.BindValue<FormState>(null!, static s => s.Volume));
        Assert.Throws<ArgumentNullException>("selector", () => slider.BindValue(store, null!));
        Assert.Throws<ArgumentNullException>("store", () => field.BindText<FormState>(null!, static s => s.Name));
        Assert.Throws<ArgumentNullException>("selector", () => field.BindText(store, null!));
        Assert.Equal(0, toggle.BindingCount);
        Assert.Equal(0, slider.BindingCount);
        Assert.Equal(0, field.BindingCount);
    }

    /// <summary>Creates a store with the three slices.</summary>
    /// <param name="muted">The start state of the toggle.</param>
    /// <param name="volume">The start value of the slider.</param>
    /// <param name="name">The start text of the field.</param>
    /// <returns>The store at version 1.</returns>
    private static Store<FormState> CreateStore(bool muted, float volume, string name)
    {
        return new Store<FormState>(new Store<FormState>.CreateInfo { Initial = new FormState(muted, volume, name) });
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

    /// <summary>Adds a toggle of 20 by 20 to a root.</summary>
    /// <param name="root">The root.</param>
    /// <returns>The toggle.</returns>
    private static Toggle CreateToggle(Element root)
    {
        Toggle toggle = new(default(Toggle.CreateInfo));
        toggle.Inline.Width = 20;
        toggle.Inline.Height = 20;
        root.Children.Add(toggle);
        return toggle;
    }

    /// <summary>Adds a slider of 120 by 20 with the range 0 to 100, the step 1, and the value 0 to a root.</summary>
    /// <param name="root">The root.</param>
    /// <returns>The slider.</returns>
    private static Slider CreateSlider(Element root)
    {
        Slider slider = new(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Step = 1f, Value = 0f });
        slider.Inline.Width = 120;
        slider.Inline.Height = 20;
        root.Children.Add(slider);
        return slider;
    }

    /// <summary>Adds an empty text field of the capacity 16 and the size 120 by 20 to a root.</summary>
    /// <param name="root">The root.</param>
    /// <returns>The field.</returns>
    private static TextField CreateField(Element root)
    {
        TextField field = new(new TextField.CreateInfo { Capacity = 16 });
        field.Inline.Width = 120;
        field.Inline.Height = 20;
        field.Inline.FontSize = 16;
        root.Children.Add(field);
        return field;
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

    /// <summary>Creates a char event with no target.</summary>
    /// <param name="c">The char.</param>
    /// <returns>The event.</returns>
    private static CharEvent Char(char c)
    {
        return new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, 0d, null),
            CodePoint = c,
        };
    }
}
