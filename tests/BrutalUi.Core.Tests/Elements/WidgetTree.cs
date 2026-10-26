using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The shared test tree of the widget tests: a root of 400 x 300 in a row, a <see cref="Core.Button"/> of 120 x 40,
/// a plain element of 100 x 100, and a <see cref="Core.Toggle"/> of 40 x 40. The tree order is <see cref="Root"/>,
/// <see cref="Button"/>, <see cref="Other"/>, and <see cref="Toggle"/>.
/// </summary>
/// <remarks>
/// <para>
/// The layout gives these viewport rects: <see cref="Root"/> (0, 0, 400, 300), <see cref="Button"/> (0, 0, 120, 40),
/// <see cref="Other"/> (120, 0, 100, 100), and <see cref="Toggle"/> (220, 0, 40, 40). The rects are valid after
/// <see cref="Settle"/>.
/// </para>
/// <para>
/// A later phase adds a child at the end of the row only, so these rects and points stay. Use the factories of
/// <see cref="PointerTree"/> for the events.
/// </para>
/// <para>
/// The tree interns no class. A test that needs a class interns it with a name prefix that is unique to the test,
/// because xUnit runs the test classes in parallel.
/// </para>
/// </remarks>
internal sealed class WidgetTree
{
    /// <summary>A point inside <see cref="Button"/>.</summary>
    public static readonly float2 OverButton = new(60f, 20f);

    /// <summary>A point inside <see cref="Other"/>.</summary>
    public static readonly float2 OverOther = new(170f, 50f);

    /// <summary>A point inside <see cref="Toggle"/>.</summary>
    public static readonly float2 OverToggle = new(240f, 20f);

    /// <summary>A point inside the viewport and outside the root.</summary>
    public static readonly float2 OverNothing = new(600f, 500f);

    /// <summary>Creates the tree and adds the root to a new context.</summary>
    public WidgetTree()
    {
        Context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };

        Root = new Element { Name = "root" };
        Root.Inline.FlexDirection = FlexDirection.Row;
        Root.Inline.Width = 400;
        Root.Inline.Height = 300;

        Button = new Button(new Button.CreateInfo()) { Name = "button" };
        Button.Inline.Width = 120;
        Button.Inline.Height = 40;

        Other = new Element { Name = "other" };
        Other.Inline.Width = 100;
        Other.Inline.Height = 100;

        Toggle = new Toggle(new Toggle.CreateInfo()) { Name = "toggle" };
        Toggle.Inline.Width = 40;
        Toggle.Inline.Height = 40;

        Root.Children.Add(Button);
        Root.Children.Add(Other);
        Root.Children.Add(Toggle);
        Context.AddRoot(Root);
    }

    /// <summary>Gets the context. Its viewport is 800 x 600, and its measurer is a <see cref="RecordingRenderer"/>.</summary>
    public UiContext Context { get; }

    /// <summary>Gets the root: 400 x 300 at (0, 0), in a row.</summary>
    public Element Root { get; }

    /// <summary>Gets the first child of the root: a button of 120 x 40 at (0, 0).</summary>
    public Button Button { get; }

    /// <summary>Gets the second child of the root: a plain element of 100 x 100 at (120, 0).</summary>
    public Element Other { get; }

    /// <summary>Gets the third child of the root: a toggle of 40 x 40 at (220, 0).</summary>
    public Toggle Toggle { get; }

    /// <summary>Runs one update with a time step of 0, so the layout and the style are current.</summary>
    public void Settle()
    {
        Context.Update(0f);
    }
}
