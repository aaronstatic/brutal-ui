using System.Text;
using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The shared test tree of the text field tests: a pickable root of 400 x 300 in a column with a text field and a
/// plain element. The tree order is <see cref="Root"/>, <see cref="Field"/>, and <see cref="Other"/>.
/// </summary>
/// <remarks>
/// <para>
/// The layout gives these viewport rects: <see cref="Root"/> (0, 0, 400, 300), <see cref="Field"/> (0, 0, 81, 16), and
/// <see cref="Other"/> (0, 16, 100, 100). The rects are valid after <see cref="Settle"/>.
/// </para>
/// <para>
/// The field has no padding and the inline font size 16. The measurer is a <see cref="RecordingRenderer"/>, so each
/// byte of the text is 8 pixels wide. A text of <c>n</c> bytes ends at x <c>8n</c>. Ten bytes and a caret of 1 pixel
/// fill the field exactly. The inline overflow of the field is hidden, so the draw walk pushes the clip
/// (0, 0, 81, 16) before the text of the field.
/// </para>
/// <para>
/// <see cref="Other"/> is focusable. The tree interns no class. A test that needs a class interns it with a name prefix
/// that is unique to the test class, because xUnit runs the test classes in parallel. Use the factories of
/// <see cref="PointerTree"/> for the events.
/// </para>
/// </remarks>
internal sealed class TextFieldTree
{
    /// <summary>A point inside <see cref="Field"/>.</summary>
    public static readonly float2 OverField = new(40f, 8f);

    /// <summary>A point inside <see cref="Other"/>.</summary>
    public static readonly float2 OverOther = new(50f, 60f);

    /// <summary>A point inside the viewport and outside the root.</summary>
    public static readonly float2 OverNothing = new(600f, 500f);

    /// <summary>Creates the tree with a field of the capacity 16 and no start text.</summary>
    public TextFieldTree()
        : this(new TextField.CreateInfo { Capacity = 16 })
    {
    }

    /// <summary>Creates the tree and adds the root to a new context.</summary>
    /// <param name="info">The create-info of the field.</param>
    public TextFieldTree(in TextField.CreateInfo info)
    {
        Renderer = new RecordingRenderer();
        Context = new UiContext(new UiContext.CreateInfo { Measurer = Renderer })
        {
            ViewportSize = new float2(800f, 600f),
        };

        Root = new Element { Name = "root" };
        Root.Inline.FlexDirection = FlexDirection.Column;
        Root.Inline.Width = 400;
        Root.Inline.Height = 300;
        Root.IsPickable = true;

        Field = new TextField(in info) { Name = "field" };
        Field.Inline.Width = 81;
        Field.Inline.Height = 16;
        Field.Inline.FontSize = 16;

        Other = new Element { Name = "other" };
        Other.Inline.Width = 100;
        Other.Inline.Height = 100;
        Other.IsFocusable = true;

        Root.Children.Add(Field);
        Root.Children.Add(Other);
        Context.AddRoot(Root);
    }

    /// <summary>Gets the context. Its viewport is 800 x 600, and its measurer is <see cref="Renderer"/>.</summary>
    public UiContext Context { get; }

    /// <summary>
    /// Gets the renderer of the tree: the measurer of <see cref="Context"/> and the target of <see cref="Frame"/>. It
    /// holds the calls of the last frame only.
    /// </summary>
    public RecordingRenderer Renderer { get; }

    /// <summary>Gets the root: 400 x 300 at (0, 0), in a column, pickable.</summary>
    public Element Root { get; }

    /// <summary>Gets the first child of the root: a text field of 81 x 16 at (0, 0), with the font size 16.</summary>
    public TextField Field { get; }

    /// <summary>Gets the second child of the root: a focusable plain element of 100 x 100 at (0, 16).</summary>
    public Element Other { get; }

    /// <summary>Runs one update with a time step of 0, so the layout and the style are current.</summary>
    public void Settle()
    {
        Context.Update(0f);
    }

    /// <summary>Settles the tree and gives the focus to <see cref="Field"/>.</summary>
    public void Focus()
    {
        Settle();
        Context.SetFocus(Field);
    }

    /// <summary>Pushes one char event with no target for each rune of a text. The call runs no drain.</summary>
    /// <param name="text">The text. Each rune becomes one char event.</param>
    public void Type(string text)
    {
        foreach (Rune rune in text.EnumerateRunes())
        {
            Context.PushEvent(PointerTree.Char((uint)rune.Value));
        }
    }

    /// <summary>Runs one update with a time step of 0 and one draw into <see cref="Renderer"/>.</summary>
    /// <remarks>The call allocates no renderer, so an allocation test can call it in a measured body.</remarks>
    /// <returns><see cref="Renderer"/>, with the calls of the frame.</returns>
    public RecordingRenderer Frame()
    {
        Context.Update(0f);
        Context.Draw(Renderer);
        return Renderer;
    }
}
