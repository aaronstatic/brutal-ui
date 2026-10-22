using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The shared test tree of the pointer tests: a root of 400 x 300 in a row, two children of 100 x 100, and one child
/// of 50 x 50 in the second child. It is the shared small scene of the pointer and focus tests. The tree order is
/// <see cref="Root"/>, <see cref="Left"/>, <see cref="Right"/>, and <see cref="Inner"/>.
/// </summary>
/// <remarks>
/// <para>
/// The layout gives these viewport rects: <see cref="Root"/> (0, 0, 400, 300), <see cref="Left"/> (0, 0, 100, 100),
/// <see cref="Right"/> (100, 0, 100, 100), and <see cref="Inner"/> (100, 0, 50, 50). The rects are valid after
/// <see cref="Settle"/>.
/// </para>
/// <para>
/// The class table is global and xUnit runs the test classes in parallel. Each tree interns its class with the name
/// prefix of the caller, so two test classes never share a class id.
/// </para>
/// <para>
/// Do not move this class into <c>Shared/</c>. <c>BrutalUi.ImGui.Tests</c> links <c>Shared/*.cs</c> and has no
/// access to the internals of Core.
/// </para>
/// </remarks>
internal sealed class PointerTree
{
    /// <summary>The background color of the hover rule of <see cref="AddHoverSheet"/>.</summary>
    public static readonly Rgba8 HoverFill = new(48, 96, 192, 255);

    /// <summary>The background color of the active rule of <see cref="AddActiveSheet"/>.</summary>
    public static readonly Rgba8 ActiveFill = new(200, 64, 64, 255);

    /// <summary>The background color of the focus rule of <see cref="AddFocusSheet"/>.</summary>
    public static readonly Rgba8 FocusFill = new(64, 160, 96, 255);

    /// <summary>Creates the tree, adds the root to a new context, and interns the hover class.</summary>
    /// <param name="prefix">The name prefix of the hover class. Use a prefix that is unique to the test.</param>
    public PointerTree(string prefix)
    {
        Context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };

        Hoverable = ClassName.Intern(prefix + ".hoverable");

        Root = new Element { Name = "root" };
        Root.Inline.FlexDirection = FlexDirection.Row;
        Root.Inline.Width = 400;
        Root.Inline.Height = 300;

        Left = new Element { Name = "left" };
        Left.Inline.Width = 100;
        Left.Inline.Height = 100;
        Left.Classes.Add(Hoverable);

        Right = new Element { Name = "right" };
        Right.Inline.Width = 100;
        Right.Inline.Height = 100;
        Right.Classes.Add(Hoverable);

        Inner = new Element { Name = "inner" };
        Inner.Inline.Width = 50;
        Inner.Inline.Height = 50;

        Root.Children.Add(Left);
        Root.Children.Add(Right);
        Right.Children.Add(Inner);
        Context.AddRoot(Root);
    }

    /// <summary>Gets the context. Its viewport is 800 x 600.</summary>
    public UiContext Context { get; }

    /// <summary>Gets the class on <see cref="Left"/> and <see cref="Right"/>.</summary>
    public ClassId Hoverable { get; }

    /// <summary>Gets the root: 400 x 300 at (0, 0), in a row.</summary>
    public Element Root { get; }

    /// <summary>Gets the first child of the root: 100 x 100 at (0, 0).</summary>
    public Element Left { get; }

    /// <summary>Gets the second child of the root: 100 x 100 at (100, 0).</summary>
    public Element Right { get; }

    /// <summary>Gets the child of <see cref="Right"/>: 50 x 50 at (100, 0).</summary>
    public Element Inner { get; }

    /// <summary>Adds a sheet with the rule <c>.hoverable:hover</c>, which sets <see cref="HoverFill"/>.</summary>
    public void AddHoverSheet()
    {
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Class = Hoverable, States = PseudoStates.Hover }),
                new StyleProperties { BackgroundColor = HoverFill })
            .Build();
        Context.AddSheet(sheet);
    }

    /// <summary>Adds a sheet with the rule <c>.hoverable:active</c>, which sets <see cref="ActiveFill"/>.</summary>
    public void AddActiveSheet()
    {
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Class = Hoverable, States = PseudoStates.Active }),
                new StyleProperties { BackgroundColor = ActiveFill })
            .Build();
        Context.AddSheet(sheet);
    }

    /// <summary>Adds a sheet with the rule <c>.hoverable:focus</c>, which sets <see cref="FocusFill"/>.</summary>
    public void AddFocusSheet()
    {
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Class = Hoverable, States = PseudoStates.Focus }),
                new StyleProperties { BackgroundColor = FocusFill })
            .Build();
        Context.AddSheet(sheet);
    }

    /// <summary>Runs one update with a time step of 0, so the layout and the style are current.</summary>
    public void Settle()
    {
        Context.Update(0f);
    }

    /// <summary>Creates a pointer move with no target.</summary>
    /// <param name="p">The position in viewport pixels.</param>
    /// <returns>The event.</returns>
    public static PointerEvent Move(float2 p)
    {
        return Pointer(UiEventType.PointerMove, p, PointerButton.Left);
    }

    /// <summary>Creates a pointer down with no target.</summary>
    /// <param name="p">The position in viewport pixels.</param>
    /// <param name="b">The button.</param>
    /// <returns>The event.</returns>
    public static PointerEvent Down(float2 p, PointerButton b)
    {
        return Pointer(UiEventType.PointerDown, p, b);
    }

    /// <summary>Creates a pointer up with no target.</summary>
    /// <param name="p">The position in viewport pixels.</param>
    /// <param name="b">The button.</param>
    /// <returns>The event.</returns>
    public static PointerEvent Up(float2 p, PointerButton b)
    {
        return Pointer(UiEventType.PointerUp, p, b);
    }

    /// <summary>Creates a scroll with no target.</summary>
    /// <param name="p">The position in viewport pixels.</param>
    /// <param name="delta">The scroll offset.</param>
    /// <returns>The event.</returns>
    public static ScrollEvent Scroll(float2 p, float2 delta)
    {
        return new ScrollEvent
        {
            Header = new UiEventHeader(UiEventType.Scroll, 0d, null),
            Position = p,
            Delta = delta,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>Creates a key event with no target, the scan code 0, and no repeat.</summary>
    /// <param name="type"><see cref="UiEventType.KeyDown"/> or <see cref="UiEventType.KeyUp"/>.</param>
    /// <param name="key">The key code.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <returns>The event.</returns>
    public static KeyEvent Key(UiEventType type, int key, UiModifiers modifiers)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = modifiers,
            IsRepeat = false,
        };
    }

    /// <summary>Creates a Tab key down with no target, the scan code 0, and no repeat.</summary>
    /// <param name="shift"><see langword="true"/> to set <see cref="UiModifiers.Shift"/>.</param>
    /// <returns>The event.</returns>
    public static KeyEvent Tab(bool shift)
    {
        return Key(UiEventType.KeyDown, UiContext.TabKey, shift ? UiModifiers.Shift : UiModifiers.None);
    }

    /// <summary>Creates a char event with no target.</summary>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <returns>The event.</returns>
    public static CharEvent Char(uint codePoint)
    {
        return new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, 0d, null),
            CodePoint = codePoint,
        };
    }

    /// <summary>
    /// Gives the viewport rect of an element: the position is the sum of the rect positions of the element and its
    /// ancestors, and the size is the size of the element.
    /// </summary>
    /// <param name="e">The element.</param>
    /// <returns>X, Y, the width, and the height in viewport pixels.</returns>
    public static float4 ViewportRect(Element e)
    {
        float4 own = e.Layout.Rect;
        float x = 0f;
        float y = 0f;
        for (Element? current = e; current is not null; current = current.Parent)
        {
            float4 r = current.Layout.Rect;
            x += r.X;
            y += r.Y;
        }

        return new float4(x, y, own.Z, own.W);
    }

    private static PointerEvent Pointer(UiEventType type, float2 p, PointerButton b)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Position = p,
            Button = b,
            Modifiers = UiModifiers.None,
        };
    }
}
