using System;
using System.Globalization;
using BrutalUi.Core;
using BrutalUi.Core.Tests;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The 1000-element tree with 40 bound labels and three bound widgets. The allocation gate of the bindings uses it.
/// </summary>
/// <remarks>
/// <para>
/// The scene is <see cref="ThousandWidgetTree"/> with a <see cref="Store{T}"/> of <see cref="HudState"/>. The build
/// replaces the name leaf of each of the 40 stat rows with a <see cref="Label"/>, and the element count stays 1000. The
/// labels of rows 0 to 37 alternate between the slice <c>Score</c> and the slice <c>Lives</c>. Row 38 shows
/// <c>Volume</c> with the format <c>F1</c> and the invariant culture. Row 39 shows <c>Name</c> through the string
/// overload. The toggle shows <c>Muted</c>, the slider shows <c>Volume</c>, and the text field shows <c>Name</c>.
/// </para>
/// <para>
/// The <see cref="Toggle.CheckedChanged"/> slot writes <c>Muted</c> back to the store, as a game does. The slots
/// <see cref="Slider.ValueChanged"/> and <see cref="TextField.TextChanged"/> only count.
/// </para>
/// <para>
/// The width rule: <see cref="Next(int)"/> gives a score with 5 digits, a count of lives with 2 digits, a volume with
/// 2 digits, and one of two names of 8 bytes. Thus the text of each label keeps its width from round to round. Each
/// label starts with a text of 8 bytes, so its buffer holds each later text. The scratch buffer of a number binding
/// starts at 32 bytes. Thus no round grows a buffer after the warm-up.
/// </para>
/// <para>
/// The two names are strings that the build creates, so <see cref="Next(int)"/> allocates no string. The gate never
/// reads <see cref="ThousandWidgetTree.All"/>, because it holds the old name leaves at the 40 replaced slots.
/// </para>
/// </remarks>
internal sealed class BoundTree
{
    /// <summary>The number of stat rows with a bound label.</summary>
    public const int LabelCount = 40;

    /// <summary>The number of the row that shows <c>Volume</c>.</summary>
    public const int VolumeRow = 38;

    /// <summary>The number of the row that shows <c>Name</c>.</summary>
    public const int NameRow = 39;

    /// <summary>The start text of each bound label. It holds 8 bytes.</summary>
    public const string StartText = "00000000";

    private const int NameWidth = 8;

    /// <summary>The name of an even round. It is not a literal, so the scene owns it.</summary>
    private static readonly string s_nameA = new('a', NameWidth);

    /// <summary>The name of an odd round. It is not a literal, so the scene owns it.</summary>
    private static readonly string s_nameB = new('b', NameWidth);

    /// <summary>The state of the HUD. It is the value of the store.</summary>
    /// <param name="Score">The score. A number slice.</param>
    /// <param name="Lives">The count of lives. A number slice.</param>
    /// <param name="Volume">The volume. A number slice and the slider value.</param>
    /// <param name="Muted">The state of the toggle.</param>
    /// <param name="Name">The name. A string slice and the text of the field.</param>
    internal readonly record struct HudState(int Score, int Lives, float Volume, bool Muted, string Name);

    /// <summary>Gets the tree with the widgets. The gate reads its context, renderer, and widgets.</summary>
    public required ThousandWidgetTree Tree { get; init; }

    /// <summary>Gets the store of the HUD state.</summary>
    public required Store<HudState> Store { get; init; }

    /// <summary>Gets the 40 bound labels, one for each stat row, in tree order.</summary>
    public required Label[] Labels { get; init; }

    /// <summary>Gets the counter of the widget slots.</summary>
    public required Counter Counts { get; init; }

    /// <summary>Counts the calls of the widget slots. <c>OnFlipped</c> writes <c>Muted</c> back to the store.</summary>
    internal sealed class Counter
    {
        private readonly Store<HudState> _store;

        /// <summary>Creates the counter.</summary>
        /// <param name="store">The store that a flip writes to.</param>
        public Counter(Store<HudState> store)
        {
            _store = store;
        }

        /// <summary>Gets the number of <see cref="Toggle.CheckedChanged"/> calls.</summary>
        public int Flips { get; private set; }

        /// <summary>Gets the number of <see cref="Slider.ValueChanged"/> calls.</summary>
        public int Values { get; private set; }

        /// <summary>Gets the number of <see cref="TextField.TextChanged"/> calls.</summary>
        public int TextChanges { get; private set; }

        /// <summary>Counts the flip and writes <c>Muted</c> back to the store.</summary>
        /// <param name="toggle">The toggle.</param>
        public void OnFlipped(Toggle toggle)
        {
            Flips++;
            HudState state = _store.Value with { Muted = toggle.Checked };
            _store.Set(in state);
        }

        /// <summary>Counts the value change.</summary>
        /// <param name="slider">The slider.</param>
        public void OnValue(Slider slider)
        {
            Values++;
        }

        /// <summary>Counts the text change.</summary>
        /// <param name="field">The text field.</param>
        public void OnTextChanged(TextField field)
        {
            TextChanges++;
        }
    }

    /// <summary>Builds the tree, the store, the 40 bound labels, and the 3 bound widgets.</summary>
    /// <returns>The scene.</returns>
    /// <exception cref="InvalidOperationException">
    /// The tree does not hold 1000 elements, or the build did not find 40 stat rows.
    /// </exception>
    public static BoundTree Build()
    {
        ThousandWidgetTree tree = ThousandWidgetTree.Build();
        Element stats = FindByName(tree.All, "stats");
        Element firstRow = stats.Children.Count > 0
            ? stats.Children[0]
            : throw new InvalidOperationException("The stats panel must hold rows.");
        Element firstName = FindByName(firstRow, "stat-name");
        int nameIndex = firstRow.Children.IndexOf(firstName);

        Store<HudState> store = new(new Store<HudState>.CreateInfo { Initial = Next(0) });
        Counter counter = new(store);

        ClassId rowClass = ClassName.Intern("row");
        Label[] labels = new Label[LabelCount];
        int rows = 0;
        for (int i = 0; i < stats.Children.Count; i++)
        {
            Element row = stats.Children[i];
            if (!row.Classes.Contains(rowClass))
            {
                continue;
            }

            if (rows >= LabelCount)
            {
                throw new InvalidOperationException("The stats panel must hold 40 rows.");
            }

            Label label = new(new Label.CreateInfo { Text = StartText });
            label.Inline.FlexGrow = 1f;
            row.Children.RemoveAt(nameIndex);
            row.Children.Insert(nameIndex, label);
            labels[rows] = label;

            if (rows < VolumeRow)
            {
                if (rows % 2 == 0)
                {
                    label.BindText(store, static s => s.Score);
                }
                else
                {
                    label.BindText(store, static s => s.Lives);
                }
            }
            else if (rows == VolumeRow)
            {
                label.BindText(store, static s => s.Volume, "F1", CultureInfo.InvariantCulture);
            }
            else
            {
                label.BindText(store, static s => s.Name);
            }

            rows++;
        }

        if (rows != LabelCount)
        {
            throw new InvalidOperationException("The stats panel must hold 40 rows.");
        }

        if (ThousandWidgetTree.CountSubtree(tree.Root) != ThousandElementTree.ElementCount)
        {
            throw new InvalidOperationException("The bound tree must hold exactly 1000 elements.");
        }

        tree.Toggle.BindChecked(store, static s => s.Muted);
        tree.Slider.BindValue(store, static s => s.Volume);
        tree.TextField.BindText(store, static s => s.Name);
        tree.Toggle.CheckedChanged = counter.OnFlipped;
        tree.Slider.ValueChanged = counter.OnValue;
        tree.TextField.TextChanged = counter.OnTextChanged;

        return new BoundTree
        {
            Tree = tree,
            Store = store,
            Labels = labels,
            Counts = counter,
        };
    }

    /// <summary>Finds the first element with a name in an array.</summary>
    private static Element FindByName(Element[] all, string name)
    {
        for (int i = 0; i < all.Length; i++)
        {
            if (string.Equals(all[i].Name, name, StringComparison.Ordinal))
            {
                return all[i];
            }
        }

        throw new InvalidOperationException("The tree must hold an element with the requested name.");
    }

    /// <summary>Finds the child of a parent with a name.</summary>
    private static Element FindByName(Element parent, string name)
    {
        for (int i = 0; i < parent.Children.Count; i++)
        {
            if (string.Equals(parent.Children[i].Name, name, StringComparison.Ordinal))
            {
                return parent.Children[i];
            }
        }

        throw new InvalidOperationException("The row must hold a child with the requested name.");
    }

    /// <summary>
    /// Gives the state of a round. The score has 5 digits, the lives 2 digits, the volume 2 digits, and the name 8
    /// bytes, so each text keeps its width. The call allocates nothing.
    /// </summary>
    /// <param name="round">The round number. A round of 90000 or more gives a score of 6 digits.</param>
    /// <returns>The state.</returns>
    public static HudState Next(int round)
    {
        return new HudState(
            10000 + round,
            10 + (round % 10),
            10 + (round % 80),
            round % 2 == 1,
            round % 2 == 1 ? s_nameB : s_nameA);
    }
}
