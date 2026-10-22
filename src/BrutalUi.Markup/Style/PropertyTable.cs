using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The value form of a property name. The form tells <see cref="StyleValues"/> how to read the value tokens.
/// </summary>
/// <remarks>
/// The <see cref="Transition"/> form writes no <see cref="StyleProperty"/> member. Its entry ignores
/// <see cref="PropertyEntry.Property"/>.
/// </remarks>
internal enum ValueForm : byte
{
    /// <summary>One keyword of <see cref="PropertyTable.FlexDirectionKeywords"/>.</summary>
    FlexDirection,

    /// <summary>One length: <c>px</c>, <c>%</c>, <c>auto</c>, or a bare <c>0</c>.</summary>
    Length,

    /// <summary>One to four lengths in the CSS order of a margin.</summary>
    Margin,

    /// <summary>One to four lengths in the CSS order of a padding.</summary>
    Padding,

    /// <summary>One to four lengths in <c>px</c> or a bare <c>0</c>.</summary>
    BorderWidth,

    /// <summary>One number.</summary>
    Number,

    /// <summary>One keyword of <see cref="PropertyTable.JustifyKeywords"/>.</summary>
    Justify,

    /// <summary>One keyword of <see cref="PropertyTable.AlignKeywords"/>.</summary>
    Align,

    /// <summary>One keyword of <see cref="PropertyTable.FlexWrapKeywords"/>.</summary>
    FlexWrap,

    /// <summary>One keyword of <see cref="PropertyTable.DisplayKeywords"/>.</summary>
    Display,

    /// <summary>One keyword of <see cref="PropertyTable.PositionKeywords"/>.</summary>
    Position,

    /// <summary>One keyword of <see cref="PropertyTable.OverflowKeywords"/>.</summary>
    Overflow,

    /// <summary>One color.</summary>
    Color,

    /// <summary>One to four values in <c>px</c> or a bare <c>0</c>, in the CSS order of a corner set.</summary>
    Corners,

    /// <summary>One number from 0 to 1.</summary>
    Opacity,

    /// <summary>One name of the font map, or <c>default</c>.</summary>
    Font,

    /// <summary>One value in <c>px</c> or a bare <c>0</c>.</summary>
    FontSize,

    /// <summary>
    /// One or more items <c>&lt;property&gt; &lt;duration&gt; &lt;easing&gt;</c>, separated by commas. The value goes
    /// into the <see cref="StyleTransitions"/> set of the rule, not into a <see cref="StyleProperty"/> member.
    /// </summary>
    Transition,
}

/// <summary>One row of the property table: the CSS name, the style member, and the value form.</summary>
/// <param name="Name">The CSS name.</param>
/// <param name="Property">The member that the declaration writes.</param>
/// <param name="Form">The value form.</param>
internal readonly record struct PropertyEntry(string Name, StyleProperty Property, ValueForm Form);

/// <summary>One word of a keyword table and its enum member.</summary>
/// <typeparam name="T">The enum type.</typeparam>
/// <param name="Text">The CSS word.</param>
/// <param name="Value">The member.</param>
internal readonly record struct Keyword<T>(string Text, T Value);

/// <summary>
/// The one table of the property names, the keyword tables of the seven layout enums, and the two word tables of
/// <c>transition</c> (the members and the easings). The table is the only name map of the parser.
/// </summary>
/// <remarks>
/// The tables are built one time, at the first use. The compare of a name is ordinal, so a name is case-sensitive.
/// The code uses no reflection: each keyword is a row of an explicit table.
/// </remarks>
internal static class PropertyTable
{
    /// <summary>The keywords of <c>flex-direction</c>.</summary>
    public static readonly Keyword<FlexDirection>[] FlexDirectionKeywords =
    [
        new("column", FlexDirection.Column),
        new("column-reverse", FlexDirection.ColumnReverse),
        new("row", FlexDirection.Row),
        new("row-reverse", FlexDirection.RowReverse),
    ];

    /// <summary>The keywords of <c>justify-content</c>.</summary>
    public static readonly Keyword<Justify>[] JustifyKeywords =
    [
        new("flex-start", Justify.FlexStart),
        new("center", Justify.Center),
        new("flex-end", Justify.FlexEnd),
        new("space-between", Justify.SpaceBetween),
        new("space-around", Justify.SpaceAround),
        new("space-evenly", Justify.SpaceEvenly),
    ];

    /// <summary>The keywords of <c>align-items</c>, <c>align-self</c>, and <c>align-content</c>.</summary>
    public static readonly Keyword<Align>[] AlignKeywords =
    [
        new("auto", Align.Auto),
        new("flex-start", Align.FlexStart),
        new("center", Align.Center),
        new("flex-end", Align.FlexEnd),
        new("stretch", Align.Stretch),
        new("space-between", Align.SpaceBetween),
        new("space-around", Align.SpaceAround),
        new("space-evenly", Align.SpaceEvenly),
    ];

    /// <summary>The keywords of <c>flex-wrap</c>.</summary>
    public static readonly Keyword<FlexWrap>[] FlexWrapKeywords =
    [
        new("nowrap", FlexWrap.NoWrap),
        new("wrap", FlexWrap.Wrap),
        new("wrap-reverse", FlexWrap.WrapReverse),
    ];

    /// <summary>The keywords of <c>display</c>.</summary>
    public static readonly Keyword<Display>[] DisplayKeywords =
    [
        new("flex", Display.Flex),
        new("none", Display.None),
    ];

    /// <summary>The keywords of <c>position</c>.</summary>
    public static readonly Keyword<PositionType>[] PositionKeywords =
    [
        new("relative", PositionType.Relative),
        new("absolute", PositionType.Absolute),
    ];

    /// <summary>The keywords of <c>overflow</c>.</summary>
    public static readonly Keyword<Overflow>[] OverflowKeywords =
    [
        new("visible", Overflow.Visible),
        new("hidden", Overflow.Hidden),
        new("scroll", Overflow.Scroll),
    ];

    /// <summary>The 32 rows, one for each <see cref="StyleProperty"/> member.</summary>
    public static readonly PropertyEntry[] Entries =
    [
        new("flex-direction", StyleProperty.FlexDirection, ValueForm.FlexDirection),
        new("width", StyleProperty.Width, ValueForm.Length),
        new("height", StyleProperty.Height, ValueForm.Length),
        new("margin", StyleProperty.Margin, ValueForm.Margin),
        new("padding", StyleProperty.Padding, ValueForm.Padding),
        new("border-width", StyleProperty.BorderWidth, ValueForm.BorderWidth),
        new("flex-grow", StyleProperty.FlexGrow, ValueForm.Number),
        new("flex-shrink", StyleProperty.FlexShrink, ValueForm.Number),
        new("flex-basis", StyleProperty.FlexBasis, ValueForm.Length),
        new("min-width", StyleProperty.MinWidth, ValueForm.Length),
        new("min-height", StyleProperty.MinHeight, ValueForm.Length),
        new("max-width", StyleProperty.MaxWidth, ValueForm.Length),
        new("max-height", StyleProperty.MaxHeight, ValueForm.Length),
        new("justify-content", StyleProperty.JustifyContent, ValueForm.Justify),
        new("align-items", StyleProperty.AlignItems, ValueForm.Align),
        new("align-self", StyleProperty.AlignSelf, ValueForm.Align),
        new("align-content", StyleProperty.AlignContent, ValueForm.Align),
        new("flex-wrap", StyleProperty.FlexWrap, ValueForm.FlexWrap),
        new("display", StyleProperty.Display, ValueForm.Display),
        new("position", StyleProperty.Position, ValueForm.Position),
        new("left", StyleProperty.Left, ValueForm.Length),
        new("top", StyleProperty.Top, ValueForm.Length),
        new("right", StyleProperty.Right, ValueForm.Length),
        new("bottom", StyleProperty.Bottom, ValueForm.Length),
        new("overflow", StyleProperty.Overflow, ValueForm.Overflow),
        new("background-color", StyleProperty.BackgroundColor, ValueForm.Color),
        new("border-color", StyleProperty.BorderColor, ValueForm.Color),
        new("border-radius", StyleProperty.CornerRadius, ValueForm.Corners),
        new("opacity", StyleProperty.Opacity, ValueForm.Opacity),
        new("color", StyleProperty.Color, ValueForm.Color),
        new("font", StyleProperty.Font, ValueForm.Font),
        new("font-size", StyleProperty.FontSize, ValueForm.FontSize),
    ];

    /// <summary>
    /// The row of the <c>transition</c> declaration. It writes no <see cref="StyleProperty"/> member, so it is not a
    /// row of <see cref="Entries"/>. <see cref="TryGet"/> finds it as each other name. The row ignores
    /// <see cref="PropertyEntry.Property"/>.
    /// </summary>
    public static readonly PropertyEntry TransitionEntry = new("transition", default, ValueForm.Transition);

    /// <summary>
    /// The five member names of a <c>transition</c> item, one for each member that
    /// <see cref="StyleTransitions.CanTransition(StyleProperty)"/> accepts.
    /// </summary>
    public static readonly Keyword<StyleProperty>[] TransitionMemberKeywords =
    [
        new("background-color", StyleProperty.BackgroundColor),
        new("border-color", StyleProperty.BorderColor),
        new("color", StyleProperty.Color),
        new("border-radius", StyleProperty.CornerRadius),
        new("opacity", StyleProperty.Opacity),
    ];

    /// <summary>The easing keywords of a <c>transition</c> item. The CSS keyword <c>ease</c> is not in the format.</summary>
    public static readonly Keyword<TransitionEasing>[] EasingKeywords =
    [
        new("linear", TransitionEasing.Linear),
        new("ease-in", TransitionEasing.EaseIn),
        new("ease-out", TransitionEasing.EaseOut),
        new("ease-in-out", TransitionEasing.EaseInOut),
    ];

    private static readonly Dictionary<string, PropertyEntry> ByName = BuildIndex();

    /// <summary>Finds the row of a property name.</summary>
    /// <param name="name">The bytes of the name. The compare is ordinal.</param>
    /// <param name="entry">The row, or <c>default</c> for an unknown name.</param>
    /// <returns><see langword="true"/> when the format knows the name.</returns>
    public static bool TryGet(ReadOnlySpan<byte> name, out PropertyEntry entry)
    {
        // A name token holds ASCII bytes only, so the string is the name. The parser can allocate.
        return ByName.TryGetValue(Encoding.UTF8.GetString(name), out entry);
    }

    /// <summary>Finds a keyword in a table.</summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="table">The table.</param>
    /// <param name="word">The bytes of the word. The compare is ordinal.</param>
    /// <param name="value">The member, or <c>default</c> when the table has no such word.</param>
    /// <returns><see langword="true"/> when the table holds the word.</returns>
    public static bool TryFind<T>(Keyword<T>[] table, ReadOnlySpan<byte> word, out T value)
    {
        string text = Encoding.UTF8.GetString(word);
        for (int i = 0; i < table.Length; i++)
        {
            if (string.Equals(table[i].Text, text, StringComparison.Ordinal))
            {
                value = table[i].Value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    /// <summary>Gives the words of a table for a message, for example <c>'row', 'row-reverse'</c>.</summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="table">The table.</param>
    /// <returns>The words in single quotes, separated by a comma and a space.</returns>
    public static string List<T>(Keyword<T>[] table)
    {
        StringBuilder builder = new();
        for (int i = 0; i < table.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append('\'').Append(table[i].Text).Append('\'');
        }

        return builder.ToString();
    }

    private static Dictionary<string, PropertyEntry> BuildIndex()
    {
        Dictionary<string, PropertyEntry> index = new(Entries.Length + 1, StringComparer.Ordinal);
        foreach (PropertyEntry entry in Entries)
        {
            index.Add(entry.Name, entry);
        }

        index.Add(TransitionEntry.Name, TransitionEntry);
        return index;
    }
}
