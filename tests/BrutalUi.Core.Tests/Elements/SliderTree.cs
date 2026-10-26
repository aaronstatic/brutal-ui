using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The shared test tree of the slider tests: a pickable root of 400 x 300 in a row with three children. The tree order
/// is <see cref="Root"/>, <see cref="Lead"/>, <see cref="Slider"/>, and <see cref="Other"/>.
/// </summary>
/// <remarks>
/// <para>
/// The layout gives these viewport rects: <see cref="Root"/> (0, 0, 400, 300), <see cref="Lead"/> (0, 0, 40, 40),
/// <see cref="Slider"/> (40, 0, 120, 20), and <see cref="Other"/> (160, 0, 100, 100). The rects are valid after
/// <see cref="Settle"/>.
/// </para>
/// <para>
/// The map: the slider has no padding, so its content box is its border box. The thumb is 20 x 20, so the thumb center
/// moves over 100 pixels, from x 50 to x 150. With the range 0 to 100, the value <c>v</c> sits at the x
/// <c>50 + v</c>. <see cref="At(float)"/> gives that point.
/// </para>
/// <para>
/// The tree interns no class. A test that needs a class interns it with a name prefix that is unique to the test,
/// because xUnit runs the test classes in parallel. Use the factories of <see cref="PointerTree"/> for the events.
/// </para>
/// </remarks>
internal sealed class SliderTree
{
    /// <summary>A point inside <see cref="Lead"/>.</summary>
    public static readonly float2 OverLead = new(20f, 10f);

    /// <summary>A point inside <see cref="Other"/>.</summary>
    public static readonly float2 OverOther = new(210f, 50f);

    /// <summary>A point inside the root and right of <see cref="Slider"/> and <see cref="Other"/>.</summary>
    public static readonly float2 PastRight = new(300f, 10f);

    /// <summary>A point inside the viewport and outside the root.</summary>
    public static readonly float2 OverNothing = new(600f, 500f);

    /// <summary>Creates the tree with the range 0 to 100 and the value 0.</summary>
    public SliderTree()
        : this(new Slider.CreateInfo { Minimum = 0f, Maximum = 100f, Value = 0f })
    {
    }

    /// <summary>Creates the tree and adds the root to a new context.</summary>
    /// <param name="info">The create-info of the slider.</param>
    public SliderTree(in Slider.CreateInfo info)
    {
        Context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };

        Root = new Element { Name = "root" };
        Root.Inline.FlexDirection = FlexDirection.Row;
        Root.Inline.Width = 400;
        Root.Inline.Height = 300;
        Root.IsPickable = true;

        Lead = new Element { Name = "lead" };
        Lead.Inline.Width = 40;
        Lead.Inline.Height = 40;

        Slider = new Slider(in info) { Name = "slider" };
        Slider.Inline.Width = 120;
        Slider.Inline.Height = 20;

        Other = new Element { Name = "other" };
        Other.Inline.Width = 100;
        Other.Inline.Height = 100;

        Root.Children.Add(Lead);
        Root.Children.Add(Slider);
        Root.Children.Add(Other);
        Context.AddRoot(Root);
    }

    /// <summary>Gets the context. Its viewport is 800 x 600, and its measurer is a <see cref="RecordingRenderer"/>.</summary>
    public UiContext Context { get; }

    /// <summary>Gets the root: 400 x 300 at (0, 0), in a row, pickable.</summary>
    public Element Root { get; }

    /// <summary>Gets the first child of the root: a plain element of 40 x 40 at (0, 0).</summary>
    public Element Lead { get; }

    /// <summary>Gets the second child of the root: a slider of 120 x 20 at (40, 0).</summary>
    public Slider Slider { get; }

    /// <summary>Gets the third child of the root: a plain element of 100 x 100 at (160, 0).</summary>
    public Element Other { get; }

    /// <summary>Gives the point on the slider where the thumb center sits for a value of the range 0 to 100.</summary>
    /// <param name="value">The value, from 0 to 100.</param>
    /// <returns>The point: x is <c>50 + value</c> and y is 10.</returns>
    public static float2 At(float value)
    {
        return new float2(50f + value, 10f);
    }

    /// <summary>Runs one update with a time step of 0, so the layout and the style are current.</summary>
    public void Settle()
    {
        Context.Update(0f);
    }
}
