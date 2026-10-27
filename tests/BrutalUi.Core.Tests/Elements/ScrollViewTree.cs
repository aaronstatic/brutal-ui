using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The shared test tree of the scroll view tests: a pickable root of 400 x 300 in a column with a scroll view and a
/// plain element. The tree order is <see cref="Root"/>, <see cref="View"/>, <see cref="Row0"/>, <see cref="Slider"/>,
/// <see cref="Row2"/>, <see cref="Row3"/>, <see cref="Row4"/>, and <see cref="Outside"/>.
/// </summary>
/// <remarks>
/// <para>
/// The layout gives these rects at the offset (0, 0), in viewport pixels: <see cref="Root"/> (0, 0, 400, 300),
/// <see cref="View"/> (0, 0, 200, 100), <see cref="Row0"/> (0, 0, 300, 40), <see cref="Slider"/> (0, 40, 120, 20),
/// <see cref="Row2"/> (0, 60, 300, 40), <see cref="Row3"/> (0, 100, 300, 40), <see cref="Row4"/> (0, 140, 300, 40), and
/// <see cref="Outside"/> (0, 100, 100, 100). The rects are valid after <see cref="Settle"/>. The view moves the draw of
/// each child up and left by its offset.
/// </para>
/// <para>
/// The content of the view is 300 x 180, so the maximum offset is (100, 80). The rows keep their width of 300, because
/// the flex shrink is 0 by default.
/// </para>
/// <para>
/// The map: the slider has the range 0 to 100 and no padding. Its thumb is 20 x 20, so the thumb center moves over 100
/// pixels. <see cref="SliderAt(float, float2)"/> gives the point of a value at an offset of the view.
/// </para>
/// <para>
/// <see cref="Row3"/> and <see cref="Row4"/> are not pickable, so a hit there goes to the view. Each row has its own
/// opaque inline background color, <see cref="RowFill(int)"/>, so a test finds the rect of a row by its color.
/// </para>
/// <para>
/// <see cref="PointerTree.ViewportRect(Element)"/> ignores the offset. A test computes each expected point by hand. The
/// tree interns no class. A test that needs a class interns it with a name prefix that is unique to the test class.
/// Use the factories of <see cref="PointerTree"/> for the events.
/// </para>
/// </remarks>
internal sealed class ScrollViewTree
{
    /// <summary>A point inside <see cref="Row0"/> at the offset (0, 0).</summary>
    public static readonly float2 OverRow0 = new(100f, 20f);

    /// <summary>A point inside the view and right of <see cref="Slider"/> at the offset (0, 0).</summary>
    public static readonly float2 OverGap = new(160f, 50f);

    /// <summary>A point inside <see cref="Outside"/>.</summary>
    public static readonly float2 OverOutside = new(50f, 150f);

    /// <summary>A point inside the root and right of the view.</summary>
    public static readonly float2 PastRight = new(300f, 50f);

    /// <summary>A point inside the viewport and outside the root.</summary>
    public static readonly float2 OverNothing = new(600f, 500f);

    /// <summary>Creates the tree with the default create-info of the view.</summary>
    public ScrollViewTree()
        : this(default(ScrollView.CreateInfo))
    {
    }

    /// <summary>Creates the tree and adds the root to a new context.</summary>
    /// <param name="info">The create-info of the view.</param>
    public ScrollViewTree(in ScrollView.CreateInfo info)
    {
        Context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };

        Root = new Element { Name = "root" };
        Root.Inline.FlexDirection = FlexDirection.Column;
        Root.Inline.Width = 400;
        Root.Inline.Height = 300;
        Root.IsPickable = true;

        View = new ScrollView(in info) { Name = "view" };
        View.Inline.FlexDirection = FlexDirection.Column;
        View.Inline.Width = 200;
        View.Inline.Height = 100;

        Row0 = CreateRow("row0", 0);

        Slider = new Slider(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 0f }) { Name = "slider" };
        Slider.Inline.Width = 120;
        Slider.Inline.Height = 20;

        Row2 = CreateRow("row2", 2);
        Row3 = CreateRow("row3", 3);
        Row3.IsPickable = false;
        Row4 = CreateRow("row4", 4);
        Row4.IsPickable = false;

        Outside = new Element { Name = "outside" };
        Outside.Inline.Width = 100;
        Outside.Inline.Height = 100;

        View.Children.Add(Row0);
        View.Children.Add(Slider);
        View.Children.Add(Row2);
        View.Children.Add(Row3);
        View.Children.Add(Row4);
        Root.Children.Add(View);
        Root.Children.Add(Outside);
        Context.AddRoot(Root);
    }

    /// <summary>Gets the context. Its viewport is 800 x 600, and its measurer is a <see cref="RecordingRenderer"/>.</summary>
    public UiContext Context { get; }

    /// <summary>Gets the root: 400 x 300 at (0, 0), in a column, pickable.</summary>
    public Element Root { get; }

    /// <summary>Gets the scroll view: 200 x 100 at (0, 0), in a column.</summary>
    public ScrollView View { get; }

    /// <summary>Gets the first child of the view: 300 x 40 at (0, 0) of the view.</summary>
    public Element Row0 { get; }

    /// <summary>Gets the second child of the view: a slider of 120 x 20 at (0, 40) of the view, the range 0 to 100.</summary>
    public Slider Slider { get; }

    /// <summary>Gets the third child of the view: 300 x 40 at (0, 60) of the view.</summary>
    public Element Row2 { get; }

    /// <summary>Gets the fourth child of the view: 300 x 40 at (0, 100) of the view, not pickable.</summary>
    public Element Row3 { get; }

    /// <summary>Gets the fifth child of the view: 300 x 40 at (0, 140) of the view, not pickable.</summary>
    public Element Row4 { get; }

    /// <summary>Gets the second child of the root: a plain element of 100 x 100 at (0, 100).</summary>
    public Element Outside { get; }

    /// <summary>
    /// Gives the viewport point of the thumb center of <see cref="Slider"/> for a value, at an offset of the view.
    /// </summary>
    /// <param name="value">The value, from 0 to 100.</param>
    /// <param name="offset">The scroll offset of the view.</param>
    /// <returns>The point: x is <c>10 + value - offset.X</c> and y is <c>50 - offset.Y</c>.</returns>
    public static float2 SliderAt(float value, float2 offset)
    {
        return new float2(10f + value - offset.X, 50f - offset.Y);
    }

    /// <summary>Gives the opaque inline background color of a row.</summary>
    /// <param name="row">The index of the row: 0, 2, 3, or 4.</param>
    /// <returns>The color. Each row has its own color.</returns>
    public static Rgba8 RowFill(int row)
    {
        return new Rgba8((byte)(40 + (row * 40)), (byte)(200 - (row * 30)), (byte)(10 + (row * 20)), 255);
    }

    /// <summary>Runs one update with a time step of 0, so the layout and the style are current.</summary>
    public void Settle()
    {
        Context.Update(0f);
    }

    /// <summary>Creates a row of 300 x 40 with its own background color.</summary>
    /// <param name="name">The name of the row.</param>
    /// <param name="index">The index of the row.</param>
    /// <returns>The row.</returns>
    private static Element CreateRow(string name, int index)
    {
        Element row = new() { Name = name };
        row.Inline.Width = 300;
        row.Inline.Height = 40;
        row.Inline.BackgroundColor = RowFill(index);
        return row;
    }
}
